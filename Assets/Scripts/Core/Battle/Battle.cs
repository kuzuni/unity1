using System;
using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Battle
{
    public enum BattlePhase { Idle, Fight, WaveDelay, StageDelay, BossWarn, DungeonClear }

    /// <summary>
    /// 원작 `web/js/combat.js` 의 <c>Combat</c> — 100ms 고정 틱 전투 엔진(T7). 렌더링과 분리 · UnityEngine 참조 0.
    /// 함수 이름·순서·난수 호출 순서를 원작과 같게 두었다: `tools/sim/sim_combat.js` 가 정본을 같은 시드로 돌린 기대값을
    /// `Assets/Tests/EditMode/BattleTests.cs` 가 판 단위로 대조한다. 여기서 순서를 바꾸면 그 대조가 깨진다(그것이 의도다).
    /// 바깥(장면·UI·SFX)은 <see cref="Events"/> 로만 안다 — 장면이 규칙을 계산하지 않는다.
    /// 시각(ms)은 호출자가 준다(`Tick(dt, nowMs)`) — 원작 `U.now()` 자리. 사망 연출·버프 만료는 벽시계, 나머지는 틱 누적.
    /// </summary>
    public sealed class Battle
    {
        sealed class Pending { public double T; public Action Fn; }
        public sealed class Buff { public string Id; public Big AtkFlat; public double Until; }
        public sealed class Hot { public string Id; public Big Per, Remain, Acc; public double Until, AccT; }
        public sealed class HeroState
        {
            public Big Hp = Big.One, MaxHp = Big.One;
            public double AtkTimer;
            /// <summary>마지막 `RecalcHero` 결과(버프 가산 포함). 예약된 타격은 시전 시점의 이 객체를 붙든다(원작 클로저와 같다).</summary>
            public HeroStats Stats;
        }

        readonly BattleContext _c;
        readonly Rng _rng;
        double _nowMs;
        int _tick;
        int _enemySeq;
        List<Pending> _pending = new List<Pending>();

        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly HeroState Hero = new HeroState();
        public readonly List<Buff> Buffs = new List<Buff>();
        public readonly List<Hot> Hots = new List<Hot>();
        public readonly Dictionary<string, double> Cooldowns = new Dictionary<string, double>();
        public int Wave;
        public BattlePhase Phase = BattlePhase.Idle;
        public double PhaseTimer;
        /// <summary>사망 연출 구간만 벽시계(ms) — 0 이면 사망 중이 아님.</summary>
        public double DownUntil, RiseUntil;
        /// <summary>`Scene3D.walking` — 무한맵 행군 여부(전투 중이 아니거나 적이 없으면 걷는다).</summary>
        public bool Walking;
        /// <summary>장면·UI 가 틱마다 비우며 그린다(비우지 않으면 쌓인다 — 테스트는 그대로 둔다).</summary>
        public readonly List<BattleEvent> Events = new List<BattleEvent>();

        public BattleContext Context { get { return _c; } }
        public int TickCount { get { return _tick; } }
        public double NowMs { get { return _nowMs; } }

        public Battle(BattleContext ctx, Rng rng)
        {
            if (ctx == null) throw new ArgumentNullException("ctx");
            if (rng == null) throw new ArgumentNullException("rng");
            if (ctx.HeroStats == null) throw new ArgumentException("BattleContext.HeroStats 가 없다 — 영웅 스탯 없이는 전투가 없다");
            _c = ctx;
            _rng = rng;
        }

        // ── 이벤트 ──
        void Emit(string kind, int id = 0, Big? value = null, double num = 0, bool flag = false, string tag = "")
        {
            Events.Add(new BattleEvent { Tick = _tick, Kind = kind, Id = id, Value = value, Num = num, Flag = flag, Tag = tag ?? "" });
        }

        // ── 시작 ──
        public void Start(double nowMs = 0)
        {
            _nowMs = nowMs;
            RecalcHero();
            Hero.Hp = Hero.MaxHp;
            SetupStage();
        }

        /// <summary>`recalcHero` — 비율은 Number 로 뽑는다(체력이 아무리 커도 0~1). 스탯 = 바깥(버프 제외) + 살아 있는 버프 `atkFlat` 합.</summary>
        public void RecalcHero()
        {
            double ratio = (!Hero.MaxHp.IsZero) ? Hero.Hp.RatioTo(Hero.MaxHp) : 1;
            HeroStats st = _c.HeroStats().Clone();
            Big buffAtkFlat = Big.Zero;
            for (int i = 0; i < Buffs.Count; i++) if (!Buffs[i].AtkFlat.IsZero) buffAtkFlat = buffAtkFlat.Add(Buffs[i].AtkFlat);
            st.Atk = st.Atk.Add(buffAtkFlat);
            Hero.Stats = st;
            Hero.MaxHp = st.Hp;
            Hero.Hp = Hero.MaxHp.Mul(Math.Min(1, Math.Max(0, ratio)));
        }

        /// <summary>종합 전투력(Big) — 상단바 표시·PvP 매칭 공용.</summary>
        public Big CombatPower()
        {
            HeroStats st = Hero.Stats;
            if (st == null) return Big.Zero;
            return st.Atk.Mul(st.AttacksPerSec * (1 + st.CritCh / 100 * st.CritDmg / 100)).Add(st.Hp.Div(8));
        }

        // ── 스테이지/웨이브 ──
        /// <summary>몬스터 기본 HP(Big) — 던전은 `Dungeons.monsterHp` · 본대는 절대 챕터 곡선.</summary>
        public Big MonsterBaseHp()
        {
            if (_c.Dungeon != null) return Big.Of(_c.Dungeon.MonsterHp);
            return Big.Of(BattleRules.MonsterHpBase).Mul(Math.Pow(BattleRules.MonsterHpPerChapter, _c.Progress.CurAbsChapter - 1))
                .Mul(Big.Of(BattleRules.MonsterHpPerStage).Pow(_c.Progress.Stage - 1));
        }

        /// <summary>1장 초반 보스 완화 계수 — 맨몸 영웅 전용이라 기본 순환 1장에서만.</summary>
        public double BossEase()
        {
            if (_c.Dungeon != null || _c.Progress.CurAbsChapter > 1) return 1;
            return Math.Min(1, BattleRules.BossEaseBase + BattleRules.BossEasePerStage * (_c.Progress.Stage - 1));
        }

        /// <summary>스테이지 한 판의 총 웨이브 수 — 마지막 웨이브가 보스. 메인 5 · 던전 1~3(run.waves).</summary>
        public int TotalWaves()
        {
            if (_c.Dungeon != null) return _c.Dungeon.Waves > 0 ? _c.Dungeon.Waves : BattleRules.DungeonDefaultWaves;
            return BattleRules.MainWaves;
        }

        /// <summary>스킬 쿨 전체 리셋 — 첫 진입과 같은 1~3초 스태거.</summary>
        public void ResetCooldowns()
        {
            Cooldowns.Clear();
            for (int i = 0; i < _c.EquippedSkills.Count; i++)
                Cooldowns[_c.EquippedSkills[i]] = BattleRules.CooldownStaggerBase + _rng.Random() * BattleRules.CooldownStaggerSpan;
        }

        public void SetupStage()
        {
            Wave = 0;
            Enemies.Clear();
            _pending = new List<Pending>();
            ResetCooldowns();
            Hero.Hp = Hero.MaxHp;
            DownUntil = 0; RiseUntil = 0;
            Emit(BattleEventKind.HeroRevive);
            Emit(BattleEventKind.Music, tag: _c.Dungeon != null ? "dungeon" : "normal");
            Emit(BattleEventKind.ClearEnemies);
            if (_c.Dungeon != null) Emit(BattleEventKind.Theme, tag: _c.Dungeon.Theme);
            else Emit(BattleEventKind.Theme, tag: "ch:" + _c.Progress.Chapter);
            Emit(BattleEventKind.StageLabel, tag: _c.Progress.StageName());
            NextWave();
        }

        public void NextWave()
        {
            Wave++;
            if (Wave == TotalWaves())
            {
                Phase = BattlePhase.BossWarn;
                PhaseTimer = BattleRules.BossImpact;
                Emit(BattleEventKind.BossEntrance);
                Emit(BattleEventKind.Music, tag: "boss");
                Emit(BattleEventKind.WavePips, num: Wave);
                return;
            }
            SpawnWave();
        }

        public void SpawnWave()
        {
            bool isBossWave = Wave == TotalWaves();
            Big baseHp = MonsterBaseHp().Mul(1 + BattleRules.WaveHpStep * (Wave - 1));
            int count = isBossWave ? 1 : (Wave <= 2 ? 2 : 3);
            double bossMult = BattleRules.BossHpMult * BossEase();
            for (int i = 0; i < count; i++)
            {
                Big hp = baseHp.Mul(isBossWave ? bossMult : 1);
                var e = new Enemy
                {
                    Id = ++_enemySeq,
                    Hp = hp, MaxHp = hp,
                    Atk = hp.Div(isBossWave ? BattleRules.BossAtkDiv : BattleRules.MobAtkDiv),
                    X = isBossWave ? BattleRules.BossSpawnX : BattleRules.SpawnX + i * BattleRules.SpawnGap + _rng.Rand(0, BattleRules.SpawnJitter),
                    Speed = _rng.Rand(BattleRules.SpeedMin, BattleRules.SpeedMax),
                    AtkTimer = _rng.Rand(BattleRules.FirstAtkMin, BattleRules.FirstAtkMax),
                    IsBoss = isBossWave,
                    Alive = true,
                };
                Enemies.Add(e);
                Emit(BattleEventKind.Spawn, e.Id, e.Hp, e.X, e.IsBoss);
            }
            RestackMelee();
            Phase = BattlePhase.Fight;
            Emit(BattleEventKind.WavePips, num: Wave);
        }

        /// <summary>근접 대열 편성 — 스폰·처치 때만. 자리만 벌리고 판정은 그대로(뒷줄도 제 자리에서 때린다).</summary>
        public void RestackMelee()
        {
            List<Enemy> alive = AliveEnemies();
            var q = new List<Enemy>();
            for (int i = 0; i < alive.Count; i++) if (!alive[i].IsBoss) q.Add(alive[i]);
            StableSortByX(q);
            double edge = BattleRules.MeleeX;
            for (int i = 0; i < q.Count; i++)
            {
                Enemy e = q[i];
                double hw = _c.EnemyHalfW != null ? _c.EnemyHalfW(e.Id) : BattleRules.DefaultEnemyHalfW;
                if (hw == 0) hw = BattleRules.DefaultEnemyHalfW;
                e.StopX = i == 0 ? BattleRules.MeleeX : edge + hw * BattleRules.MeleePack;
                edge = e.StopX.Value + hw * BattleRules.MeleePack + BattleRules.MeleeGap;
                e.Z = BattleRules.MeleeLaneZ[i % BattleRules.MeleeLaneZ.Length];
            }
            for (int i = 0; i < alive.Count; i++) if (alive[i].IsBoss) { alive[i].StopX = BattleRules.MeleeX; alive[i].Z = 0; }
        }

        /// <summary>JS `Array.sort` 는 안정 정렬 — `List.Sort` 는 아니라 삽입 정렬로.</summary>
        static void StableSortByX(List<Enemy> list)
        {
            for (int i = 1; i < list.Count; i++)
            {
                Enemy k = list[i];
                int j = i - 1;
                while (j >= 0 && list[j].X > k.X) { list[j + 1] = list[j]; j--; }
                list[j + 1] = k;
            }
        }

        public double StopXOf(Enemy e) { return e.StopX ?? BattleRules.MeleeX; }

        public List<Enemy> AliveEnemies()
        {
            var r = new List<Enemy>();
            for (int i = 0; i < Enemies.Count; i++) if (Enemies[i].Alive) r.Add(Enemies[i]);
            return r;
        }

        public Enemy FrontEnemy()
        {
            Enemy best = null;
            for (int i = 0; i < Enemies.Count; i++)
            {
                Enemy e = Enemies[i];
                if (!e.Alive) continue;
                if (best == null || e.X < best.X) best = e;
            }
            return best;
        }

        /// <summary>단일기 우선 타겟: 보스 &gt; 최전방.</summary>
        public Enemy PriorityTarget()
        {
            for (int i = 0; i < Enemies.Count; i++) if (Enemies[i].Alive && Enemies[i].IsBoss) return Enemies[i];
            return FrontEnemy();
        }

        // ── 메인 틱 ──
        public void Tick(double dt, double nowMs)
        {
            _tick++;
            _nowMs = nowMs;
            Walking = Phase != BattlePhase.BossWarn && Phase != BattlePhase.DungeonClear && (Phase != BattlePhase.Fight || AliveEnemies().Count == 0);

            // 지연 큐 — 콜백이 큐를 통째로 비울 수 있다(사망·던전 이탈) → 엔트리를 매번 다시 집어 없으면 그대로 끝낸다.
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                Pending p = i < _pending.Count ? _pending[i] : null;
                if (p == null) continue;
                p.T -= dt;
                if (p.T <= 0) { _pending.RemoveAt(i); p.Fn(); }
            }

            // 버프 만료
            double now = _nowMs;
            int beforeBuffs = Buffs.Count;
            for (int i = Buffs.Count - 1; i >= 0; i--) if (!(Buffs[i].Until > now)) Buffs.RemoveAt(i);
            if (Buffs.Count != beforeBuffs) RecalcHero();

            // 지속 회복(HoT) — 만료 틱에는 남은 몫을 통째로(캐치업에서 회복량이 증발하지 않게).
            for (int i = Hots.Count - 1; i >= 0; i--)
            {
                Hot h = Hots[i];
                bool done = h.Until <= now;
                Big amt = done ? h.Remain : h.Per.Mul(dt).Min(h.Remain);
                h.Remain = h.Remain.Sub(amt);
                Big before = Hero.Hp;
                Hero.Hp = Hero.Hp.Add(amt).Min(Hero.MaxHp);
                h.Acc = h.Acc.Add(Hero.Hp.Sub(before));
                h.AccT += dt;
                if (done || h.AccT >= BattleRules.HotFloatEvery)
                {
                    if (h.Acc.IsPos) Emit(BattleEventKind.Float, tag: "+" + NumFmt.Fmt(h.Acc));
                    h.Acc = Big.Zero; h.AccT = 0;
                }
                if (done) Hots.RemoveAt(i);
            }

            // 체력 자연 회복: 기본 1%/s + 서브스탯 '체력 재생'
            double regenPct = BattleRules.BaseRegenPerSec + (Hero.Stats != null ? Hero.Stats.HpRegen / 100 : 0);
            Hero.Hp = Hero.Hp.Add(Hero.MaxHp.Mul(regenPct * dt)).Min(Hero.MaxHp);

            // 스킬 쿨타임 감소 — 전투 중이 아니어도 흐른다(발동은 fight 에서만).
            for (int i = 0; i < _c.EquippedSkills.Count; i++)
            {
                string id = _c.EquippedSkills[i];
                double cd;
                if (!Cooldowns.TryGetValue(id, out cd)) cd = BattleRules.CooldownStaggerBase + _rng.Random() * BattleRules.CooldownStaggerSpan;
                Cooldowns[id] = Math.Max(0, cd - dt);
            }

            if (Phase == BattlePhase.WaveDelay || Phase == BattlePhase.StageDelay || Phase == BattlePhase.BossWarn)
            {
                if (DownUntil != 0)
                {
                    if (now < DownUntil) return;
                    DownUntil = 0;
                    RiseUntil = now + BattleRules.DeathRiseMs;
                    Emit(BattleEventKind.HeroRevive);
                    return;
                }
                if (RiseUntil != 0)
                {
                    if (now < RiseUntil) return;
                    RiseUntil = 0;
                }
                PhaseTimer -= dt;
                if (PhaseTimer <= 0)
                {
                    if (Phase == BattlePhase.WaveDelay) NextWave();
                    else if (Phase == BattlePhase.BossWarn) SpawnWave();
                    else SetupStage();
                }
                return;
            }
            if (Phase != BattlePhase.Fight) return;

            // 적 이동/공격
            List<Enemy> aliveNow = AliveEnemies();
            for (int i = 0; i < aliveNow.Count; i++)
            {
                Enemy e = aliveNow[i];
                double stop = StopXOf(e);
                if (e.X > stop)
                {
                    e.X -= e.Speed * dt;
                    if (e.X < stop) e.X = stop;
                }
                else
                {
                    e.AtkTimer -= dt;
                    if (e.AtkTimer <= 0)
                    {
                        e.AtkTimer = BattleRules.EnemyAtkPeriod;
                        Emit(BattleEventKind.EnemyAttack, e.Id);
                        Big atk = e.Atk;
                        _pending.Add(new Pending { T = BattleRules.EnemyHitDelay, Fn = () => DamageHero(atk) });
                    }
                }
            }

            // 영웅 자동 공격(무기 타입별 사거리·타격 시점)
            HeroStats st = Hero.Stats;
            Hero.AtkTimer -= dt;
            Enemy target = FrontEnemy();
            WeaponType wt = WeaponOf(_c.WeaponType);
            bool ranged = wt.Kind == "ranged";
            double atkRange = ranged ? BattleRules.RangedRange : BattleRules.MeleeRange;
            if (target != null && target.X < atkRange && Hero.AtkTimer <= 0)
            {
                Hero.AtkTimer = 1 / st.AttacksPerSec;
                int hits = _rng.Chance(st.DblAtk / 100) ? 2 : 1;
                double weaponDmgBonus = 1 + (ranged ? st.RangedDmg : st.MeleeDmg) / 100;
                Emit(BattleEventKind.HeroAttack, target.Id);
                for (int h = 0; h < hits; h++)
                {
                    Enemy tgt = target;
                    HeroStats stAt = st;
                    _pending.Add(new Pending
                    {
                        T = wt.Impact + h * BattleRules.DoubleHitGap,
                        Fn = () =>
                        {
                            if (!tgt.Alive) return;
                            bool crit = _rng.Chance(stAt.CritCh / 100);
                            Big dmg = stAt.Atk.Mul(_rng.Rand(BattleRules.DmgJitterMin, BattleRules.DmgJitterMax) * (crit ? stAt.CritDmg / 100 + 1 : 1) * weaponDmgBonus);
                            DamageEnemy(tgt, dmg, crit, null);
                            if (crit) Emit(BattleEventKind.Shake, num: BattleRules.CritShake);
                        },
                    });
                }
            }

            // 스킬 자동 발동
            if (_c.AutoCast)
                for (int i = 0; i < _c.EquippedSkills.Count; i++)
                {
                    string id = _c.EquippedSkills[i];
                    double cd;
                    if (Cooldowns.TryGetValue(id, out cd) && cd <= 0) TryCast(id, false);
                }
        }

        WeaponType WeaponOf(string key)
        {
            WeaponType wt = null;
            if (_c.Defs != null && _c.Defs.WeaponTypes != null)
            {
                if (!string.IsNullOrEmpty(key)) wt = _c.Defs.WeaponTypes.Get(key, null);
                if (wt == null) wt = _c.Defs.WeaponTypes.Get("sword", null);
            }
            if (wt == null) throw new InvalidOperationException("WEAPON_TYPES 에 sword 가 없다 — gamedata.json 을 확인");
            return wt;
        }

        SkillSpec SkillOf(string id)
        {
            if (_c.Skill == null) throw new InvalidOperationException("BattleContext.Skill 이 없다 — 장착 스킬 " + id);
            SkillSpec d = _c.Skill(id);
            if (d == null) throw new InvalidOperationException("없는 스킬 " + id);
            return d;
        }

        /// <summary>`tryCast` — 수동은 UI 버튼(사거리 밖이면 false · 쿨 안 돈다).</summary>
        public bool TryCast(string id, bool manual)
        {
            double cd0;
            if ((Cooldowns.TryGetValue(id, out cd0) ? cd0 : 0) > 0) return false;
            SkillSpec d = SkillOf(id);
            HeroStats st = Hero.Stats;
            if (d.Type == "heal")
            {
                Big total = d.HealAmt;
                double dur = d.Dur != 0 ? d.Dur : 1;
                for (int i = Hots.Count - 1; i >= 0; i--) if (Hots[i].Id == id) Hots.RemoveAt(i);
                Hots.Add(new Hot { Id = id, Per = total.Div(dur), Remain = total, Until = _nowMs + dur * 1000, Acc = Big.Zero, AccT = 0 });
                Emit(BattleEventKind.SkillEffect, num: 0, tag: string.IsNullOrEmpty(d.Fx) ? "heal" : d.Fx);
                Emit(BattleEventKind.SkillCutin, tag: d.Id);
            }
            else if (d.Type == "buff")
            {
                for (int i = Buffs.Count - 1; i >= 0; i--) if (Buffs[i].Id == id) Buffs.RemoveAt(i);
                Buffs.Add(new Buff { Id = id, AtkFlat = d.BuffAtk, Until = _nowMs + d.Dur * 1000 });
                RecalcHero();
                Emit(BattleEventKind.SkillEffect, num: 0, tag: string.IsNullOrEmpty(d.Fx) ? "aura" : d.Fx);
                Emit(BattleEventKind.SkillCutin, tag: d.Id);
            }
            else
            {
                var alive = new List<Enemy>();
                for (int i = 0; i < Enemies.Count; i++) if (Enemies[i].Alive && Enemies[i].X < BattleRules.SkillRange) alive.Add(Enemies[i]);
                if (alive.Count == 0) { if (manual) Emit(BattleEventKind.Toast, tag: "사거리 안에 적이 없습니다"); return false; }
                double techMult = _c.SkillDmgMult != null ? _c.SkillDmgMult() : 1;
                Big dmg = d.Dmg.Mul((1 + st.SkillDmg / 100) * techMult);
                Emit(BattleEventKind.SkillCutin, tag: d.Id);
                Emit(BattleEventKind.SkillFlash, tag: d.Color);
                if (d.Type == "aoe")
                {
                    Emit(BattleEventKind.SkillEffect, num: alive.Count, tag: d.Fx);
                    double impT = d.ImpactAt ?? BattleRules.AoeImpactDefault;
                    _pending.Add(new Pending { T = impT, Fn = () => Emit(BattleEventKind.Shake, num: BattleRules.AoeShake) });
                    for (int i = 0; i < alive.Count; i++)
                    {
                        Enemy e = alive[i];
                        _pending.Add(new Pending { T = impT, Fn = () => { if (e.Alive) DamageEnemy(e, dmg.Mul(_rng.Rand(BattleRules.DmgJitterMin, BattleRules.DmgJitterMax)), false, "skill"); } });
                    }
                }
                else
                {
                    Enemy t = PriorityTarget();
                    if (t == null) return false;
                    Emit(BattleEventKind.SkillEffect, num: 1, tag: d.Fx);
                    double impT = d.ImpactAt ?? BattleRules.SingleImpactDefault;
                    _pending.Add(new Pending { T = impT, Fn = () => { Emit(BattleEventKind.Shake, num: BattleRules.SingleShake); if (t.Alive) DamageEnemy(t, dmg, true, "skill"); } });
                }
            }
            Cooldowns[id] = d.Cd * (1 - st.SkillCd / 100);
            return true;
        }

        public void DamageEnemy(Enemy e, Big dmg, bool crit, string kind)
        {
            if (!e.Alive) return;
            e.Hp = e.Hp.Sub(dmg);
            bool kill = !e.Hp.IsPos;
            Emit(BattleEventKind.Hit, e.Id, dmg, kill ? 1 : 0, crit, kind ?? "");
            HeroStats st = Hero.Stats;
            if (st != null && st.Lifesteal != 0) Hero.Hp = Hero.Hp.Add(dmg.Mul(st.Lifesteal / 100)).Min(Hero.MaxHp);
            if (kill)
            {
                e.Alive = false;
                OnKill(e);
                Emit(BattleEventKind.Kill, e.Id, flag: e.IsBoss);
                RestackMelee();
                if (e.IsBoss) { Emit(BattleEventKind.Shake, num: BattleRules.BossKillShake); Emit(BattleEventKind.Music, tag: _c.Dungeon != null ? "dungeon" : "normal"); }
                if (AliveEnemies().Count == 0)
                {
                    if (Wave >= TotalWaves()) StageClear();
                    else { Phase = BattlePhase.WaveDelay; PhaseTimer = BattleRules.WaveDelay; }
                }
            }
        }

        public void DamageHero(Big dmg)
        {
            if (Phase != BattlePhase.Fight) return;
            HeroStats st = Hero.Stats;
            if (st != null && _rng.Chance(st.Block / 100))
            {
                Emit(BattleEventKind.Float, tag: "BLOCK");
                return;
            }
            Hero.Hp = Hero.Hp.Sub(dmg);
            Emit(BattleEventKind.HeroHit, value: dmg, num: Hero.MaxHp.IsZero ? 0.12 : dmg.RatioTo(Hero.MaxHp));
            if (!Hero.Hp.IsPos) OnDefeat();
        }

        // ── 보상 ──
        public void OnKill(Enemy e)
        {
            _c.Kills++;
            if (_c.Dungeon != null) return;
            int ac = _c.Progress.CurAbsChapter;
            double coins = Math.Ceiling(BattleRules.CoinBase * Math.Pow(BattleRules.CoinPerChapter, ac - 1) * Math.Pow(BattleRules.CoinPerStage, _c.Progress.Stage - 1)) * (e.IsBoss ? BattleRules.BossCoinMult : 1);
            _c.Coins += coins;
            Emit(BattleEventKind.Loot, tag: "🪙 +" + NumFmt.Fmt(coins));
            double hammerAmt = 1 + Math.Floor(ac / BattleRules.HammerChapterDiv);
            if (e.IsBoss)
            {
                _c.Hammers += hammerAmt * BattleRules.BossHammerMult;
                Emit(BattleEventKind.Loot, tag: "🔨 +" + JsNum.ToString(hammerAmt * BattleRules.BossHammerMult));
            }
            else if (_rng.Chance(BattleRules.HammerChance))
            {
                _c.Hammers += hammerAmt;
            }
        }

        public void StageClear()
        {
            if (_c.Dungeon != null)
            {
                _c.Dungeon = null;
                if (_c.OnDungeonClear != null) _c.OnDungeonClear();
                Emit(BattleEventKind.DungeonClear);
                Phase = BattlePhase.DungeonClear;
                PhaseTimer = 0;
                return;
            }
            Progress p = _c.Progress;
            string key = p.StageKey();
            bool firstClear = !_c.ClearedBosses.Contains(key);
            if (firstClear)
            {
                _c.ClearedBosses.Add(key);
                double bonus = Math.Ceiling(BattleRules.FirstClearBonusBase * Math.Pow(BattleRules.CoinPerChapter, p.CurAbsChapter - 1) * Math.Pow(BattleRules.CoinPerStage, p.Stage - 1));
                _c.Coins += bonus;
                Emit(BattleEventKind.Toast, tag: "🏆 " + p.StageName() + " 첫 클리어! 🪙+" + NumFmt.Fmt(bonus));
            }
            bool tierUp = false;
            if (p.Stage >= p.StagesPerChapter)
            {
                if (p.Chapter < p.ChaptersPerCycle) { p.Chapter++; p.Stage = 1; }
                else if (p.Difficulty < p.MaxDifficulty) { p.Difficulty++; p.Chapter = 1; p.Stage = 1; tierUp = true; }
            }
            else p.Stage++;
            if (tierUp) Emit(BattleEventKind.Toast, tag: "🔥 난이도 상승! " + p.StageName() + "부터 다시 도전합니다");
            if (p.CurRank > p.BestRank) p.RecordBest();
            if (_c.Save != null) _c.Save();
            Emit(BattleEventKind.Save);
            Phase = BattlePhase.StageDelay;
            PhaseTimer = BattleRules.StageDelay;
        }

        /// <summary>던전에서 튕겨 나온 순간 = 본대 복귀 순간. 전제: <see cref="BattleContext.Dungeon"/> 은 이미 null.</summary>
        public void LeaveDungeon()
        {
            Enemies.Clear();
            _pending = new List<Pending>();
            Emit(BattleEventKind.ClearEnemies);
            Emit(BattleEventKind.Music, tag: "normal");
            Emit(BattleEventKind.SceneCut);
            Emit(BattleEventKind.Theme, tag: "ch:" + _c.Progress.Chapter);
            Emit(BattleEventKind.StageLabel, tag: _c.Progress.StageName());
            Emit(BattleEventKind.WavePips, num: 0);
        }

        /// <summary>던전 클리어 팝업의 [보상 수령] 연출이 끝난 뒤 — 본대 복귀의 나머지 절반.</summary>
        public void FinishDungeonClear()
        {
            if (Phase != BattlePhase.DungeonClear) return;
            LeaveDungeon();
            Phase = BattlePhase.StageDelay;
            PhaseTimer = BattleRules.DungeonReturnDelay;
        }

        public void OnDefeat()
        {
            Emit(BattleEventKind.HeroDown);
            Progress p = _c.Progress;
            if (_c.Dungeon != null)
            {
                _c.Dungeon = null;
                if (_c.OnDungeonFail != null) _c.OnDungeonFail();
                Emit(BattleEventKind.DungeonFail);
                LeaveDungeon();
                Emit(BattleEventKind.DeathFade, tag: "본대로 복귀합니다");
            }
            else
            {
                bool back = p.Stage > 1;
                if (back) p.Stage--;
                Emit(BattleEventKind.DeathFade, tag: back ? p.StageName() + " 스테이지로 이동합니다" : "회복 후 다시 도전합니다");
                Enemies.Clear();
                _pending = new List<Pending>();
                Emit(BattleEventKind.DeathWipe);
                Emit(BattleEventKind.StageLabel, tag: p.StageName());
            }
            if (_c.Save != null) _c.Save();
            Emit(BattleEventKind.Save);
            Hero.Hp = Hero.MaxHp;
            Phase = BattlePhase.StageDelay;
            PhaseTimer = BattleRules.DeathMarchS;
            DownUntil = _nowMs + BattleRules.DeathDownMs;
            RiseUntil = 0;
        }
    }
}
