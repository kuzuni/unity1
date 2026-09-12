using System;
using System.Collections.Generic;
using Forge.Core.Battle;
using Forge.Core.Data;

namespace Forge.Core.Skills
{
    /// <summary>소환 한 번의 결과 — 원작 `Skills.summon` 반환 `{results, count}` + 효과음용 최고 등급.</summary>
    public sealed class SkillSummonResult
    {
        public sealed class Item { public SkillDef Def; public bool IsNew; public int Level; }
        public List<Item> Results = new List<Item>();
        public int Count;
        /// <summary>`SFX.gacha(bestRarity)` 에 줄 등급 — 마지막 굴림이 아니라 배치 중 가장 높은 등급.</summary>
        public string BestRarity;
        /// <summary>신규 스킬 수 — 원작은 그때마다 `Combat.recalcHero()` 를 불렀다(보유 패시브가 바뀐다).</summary>
        public int NewCount;
    }

    /// <summary>`passiveOf`·`ownedPassive` 의 `{atk, hp}` — 승천이 곱해지면 Number 를 넘으므로 Big.</summary>
    public struct PassiveBonus { public Big Atk, Hp; }

    /// <summary>
    /// 원작 `web/js/skills.js` 의 `Skills` 를 그대로 옮긴 순수 C# — 소환(`skillRatesData` · 등급 추첨 → 같은 등급 풀에서 균등) · 조각 적립 →
    /// 수동 업그레이드 · 장착 3슬롯(토글·빠른 장착) · 고정 피해/회복/버프(등급 기준치 × mult × 레벨 배율 × 승천) · 보유 패시브(장착 무관).
    /// 수치는 <see cref="GameData"/>(T2 JSON)와 <see cref="SkillRules"/>(원작 코드 상수)에서만 온다. 난수는 <see cref="Rng"/> 로 받아
    /// 원작과 **같은 순서로 같은 횟수** 소비한다(EditMode 가 `skill_vectors.json` 과 대조). UI·저장·효과음·퀘스트는 호출자가 결과로 한다.
    /// 전투(T7)에는 <see cref="Spec"/> 이 <see cref="SkillSpec"/> 을 준다(`BattleContext.Skill` 훅).
    /// </summary>
    public sealed class SkillSystem
    {
        readonly GameData _g;
        readonly SkillRules _r;
        readonly ISkillHost _h;
        readonly Rng _rng;

        public SkillState State { get; private set; }
        /// <summary>원작이 `Combat.recalcHero()` 를 부른 횟수(신규 획득·업그레이드·장착 변경) — 호출자가 영웅 스탯을 다시 계산할 신호.</summary>
        public int RecalcRequests { get; private set; }
        public SkillRules Rules { get { return _r; } }

        public SkillSystem(GameData data, SkillRules rules, ISkillHost host, Rng rng, SkillState state = null)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (host == null) throw new ArgumentNullException("host");
            if (rng == null) throw new ArgumentNullException("rng");
            _g = data; _r = rules ?? SkillRules.Original(); _h = host; _rng = rng;
            State = state ?? new SkillState();
        }

        // ── 소환 ────────────────────────────────────────────────────────────────

        /// <summary>`ticketCost(count)` = max(1, ceil(32 × count × 기술트리 배율)) — 표시·차감 양쪽이 이것을 쓴다.</summary>
        public double TicketCost(double count = 1)
        {
            return Math.Max(1, Math.Ceiling(_r.SummonTicketCost * count * _h.SkillSummonCostMult()));
        }

        /// <summary>`summonLevel()` = min(100, floor(summonCount / 5) + 1).</summary>
        public int SummonLevel()
        {
            return Math.Min(_r.SummonLevelCap, (int)Math.Floor(State.SummonCount / (double)_r.SummonsPerLevel) + 1);
        }

        /// <summary>`rates(level)` — 등급 → % (원작 `{common, rare, …}` 순서 그대로 · level 0 이면 현재 소환 레벨 · 1~100 으로 클램프).</summary>
        public OrderedMap<double> Rates(int level = 0)
        {
            int l = level != 0 ? level : SummonLevel();
            l = Math.Min(100, Math.Max(1, l));
            double[] row = _g.Balance.Skills.RatesAt(l);
            var m = new OrderedMap<double>();
            string[] rar = _g.Defs.Rarities;
            for (int i = 0; i < rar.Length; i++) m.Add(rar[i], i < row.Length ? row[i] : 0);
            return m;
        }

        /// <summary>`canSummon(useGems, count)` — 버튼 비활성 표시용.</summary>
        public bool CanSummon(bool useGems, double count = 1)
        {
            return useGems ? _h.Gems >= _r.SummonGemCost * count : _h.Tickets >= TicketCost(count);
        }

        /// <summary>
        /// `summon(useGems, count)` — 비용은 선결제로 한 번에 확인·차감(부족하면 null · 상태 불변). 굴림마다 `summonCount++` →
        /// `weightedPick(rates())` 로 등급 → 그 등급 `SKILL_DEFS` 풀에서 `choice`. 신규면 `{level 1, dupes 0, stars = 스킬 승천 수}` 로 넣고
        /// 장착 슬롯이 비면 자동 장착(재계산 신호) · 이미 있으면 조각 +1.
        /// count ≤ 0 은 원작이 `results[0].def` 에서 TypeError 를 내는 갈래(UI 가 부르지 않는다) — 여기서는 차감 없이 null.
        /// </summary>
        public SkillSummonResult Summon(bool useGems, int count = 1)
        {
            if (count <= 0) return null;
            double cost = useGems ? _r.SummonGemCost * (double)count : TicketCost(count);
            if (useGems)
            {
                if (_h.Gems < cost) return null;
                _h.Gems -= cost;
            }
            else
            {
                if (_h.Tickets < cost) return null;
                _h.Tickets -= cost;
            }
            var res = new SkillSummonResult { Count = count };
            var defs = _g.Defs.SkillDefs;
            for (int i = 0; i < count; i++)
            {
                State.SummonCount++;
                string rarity = _rng.WeightedPick(Rates());
                var pool = new List<SkillDef>();
                for (int k = 0; k < defs.Count; k++) if (defs[k].Rarity == rarity) pool.Add(defs[k]);
                SkillDef def = _rng.Choice(pool);
                SkillEntry cur = State.Get(def.Id);
                bool isNew = cur == null;
                if (!isNew)
                {
                    cur.Dupes++;
                }
                else
                {
                    cur = new SkillEntry { Level = 1, Dupes = 0, Stars = _h.SkillAscendCount() };
                    State.Skills.Add(def.Id, cur);
                    if (State.Equipped.Count < _r.MaxActive) State.Equipped.Add(def.Id);
                    RecalcRequests++;
                    res.NewCount++;
                }
                res.Results.Add(new SkillSummonResult.Item { Def = def, IsNew = isNew, Level = cur.Level });
            }
            // 배치 중 가장 높은 등급(마지막 굴림이 아니라) — 효과음 기준
            string best = res.Results[0].Def.Rarity;
            for (int i = 1; i < res.Results.Count; i++)
            {
                string r = res.Results[i].Def.Rarity;
                if (RarityIndex(r) > RarityIndex(best)) best = r;
            }
            res.BestRarity = best;
            return res;
        }

        int RarityIndex(string r) { return Array.IndexOf(_g.Defs.Rarities, r); }

        // ── 조회 · 값 ───────────────────────────────────────────────────────────

        /// <summary>`def(id)` — 없으면 null.</summary>
        public SkillDef Def(string id) { return id == null ? null : _g.Defs.Skill(id); }

        /// <summary>`level(id)` — 미보유 0.</summary>
        public int Level(string id) { SkillEntry e = State.Get(id); return e != null ? e.Level : 0; }

        /// <summary>`levelMult(id)` = 1 + 0.15 × (level − 1) — 미보유면 0.85(원작 그대로).</summary>
        public double LevelMult(string id) { return 1 + _r.LevelMultStep * (Level(id) - 1); }

        int StarsOf(string id) { SkillEntry e = State.Get(id); return e != null ? e.Stars : 0; }

        Big Scaled(string id, OrderedMap<double> baseTable)
        {
            SkillDef d = Def(id);
            if (d == null) throw new ArgumentException("unknown skill: " + id);
            double b = baseTable.Get(d.Rarity, 0);
            return _h.StarMult(StarsOf(id)).Mul(Big.Of(b * d.Mult * LevelMult(id)));
        }

        /// <summary>`dmg(id)` = 승천 배율 × (등급 기준 피해 × mult × 레벨 배율) — 영웅 공격력에 비례하지 않는 고정 피해.</summary>
        public Big Dmg(string id) { return Scaled(id, _g.Defs.SkillBaseDmg); }
        /// <summary>`healAmt(id)` — 지속시간 동안의 **총** 회복량(같은 규약).</summary>
        public Big HealAmt(string id) { return Scaled(id, _g.Defs.SkillBaseHeal); }
        /// <summary>`buffAtk(id)` — 지속시간 동안 더해지는 **고정** 공격력(같은 규약).</summary>
        public Big BuffAtk(string id) { return Scaled(id, _g.Defs.SkillBaseBuffAtk); }

        /// <summary>전투(T7)가 받는 «해석된 스킬» — `BattleContext.Skill = sys.Spec`. 모르는 id 면 null.</summary>
        public SkillSpec Spec(string id)
        {
            SkillDef d = Def(id);
            return d == null ? null : SkillSpec.From(d, Dmg(id), HealAmt(id), BuffAtk(id));
        }

        // ── 조각 · 업그레이드 ────────────────────────────────────────────────────

        public int ShardsRequired(int level) { return _r.ShardsRequired(level); }

        /// <summary>`canUpgrade(id)` — 보유 · 만렙 미만 · 조각 충분.</summary>
        public bool CanUpgrade(string id)
        {
            SkillEntry sk = State.Get(id);
            return sk != null && sk.Level < _r.MaxLevel && sk.Dupes >= _r.ShardsRequired(sk.Level);
        }

        /// <summary>`upgrade(id)` — 조각을 쓰고 레벨 +1(재계산 신호 · 미장착도 패시브가 바뀐다).</summary>
        public bool Upgrade(string id)
        {
            SkillEntry sk = State.Get(id);
            if (sk == null || !CanUpgrade(id)) return false;
            sk.Dupes -= _r.ShardsRequired(sk.Level);
            sk.Level++;
            RecalcRequests++;
            return true;
        }

        /// <summary>`upgradeAll()` — 보유 순서대로 조각이 허용하는 한 끝까지 · 올린 횟수.</summary>
        public int UpgradeAll()
        {
            int count = 0;
            var keys = new List<string>(State.Skills.Keys);
            foreach (string id in keys) while (Upgrade(id)) count++;
            return count;
        }

        // ── 장착 ────────────────────────────────────────────────────────────────

        /// <summary>`quickEquip()` — 보유 스킬을 등급 내림 → 레벨 내림(동률은 보유 순서 · 원작 sort 는 안정 정렬)으로 MAX_ACTIVE 개 장착.</summary>
        public void QuickEquip()
        {
            var owned = new List<string>(State.Skills.Keys);
            // 안정 삽입 정렬 — List.Sort 는 불안정이라 동률의 순서가 원작(V8 TimSort · 안정)과 달라진다.
            for (int i = 1; i < owned.Count; i++)
            {
                string x = owned[i];
                int j = i - 1;
                while (j >= 0 && Before(x, owned[j])) { owned[j + 1] = owned[j]; j--; }
                owned[j + 1] = x;
            }
            State.Equipped = owned.GetRange(0, Math.Min(_r.MaxActive, owned.Count));
            RecalcRequests++;
        }

        // «x 가 y 보다 앞에 와야 하는가» — 등급 높은 쪽 · 같으면 레벨 높은 쪽. 같으면 false(안정).
        bool Before(string x, string y)
        {
            int rx = RarityIndex(Def(x).Rarity), ry = RarityIndex(Def(y).Rarity);
            if (rx != ry) return rx > ry;
            return State.Get(x).Level > State.Get(y).Level;
        }

        /// <summary>`toggleEquip(id)` — 장착 중이면 해제 · 아니면 빈 슬롯에 장착 · 슬롯이 차 있으면 false.</summary>
        public bool ToggleEquip(string id)
        {
            int pos = State.Equipped.IndexOf(id);
            if (pos >= 0) State.Equipped.RemoveAt(pos);
            else if (State.Equipped.Count < _r.MaxActive) State.Equipped.Add(id);
            else return false;
            RecalcRequests++;
            return true;
        }

        // ── 보유 패시브(장착 무관) ──────────────────────────────────────────────

        /// <summary>`passiveOf(id)` — 등급 기준 패시브 × 승천 × 레벨 배율 × 기술트리. 미보유·모르는 id 면 0.</summary>
        public PassiveBonus PassiveOf(string id)
        {
            SkillDef d = Def(id); SkillEntry sk = State.Get(id);
            if (d == null || sk == null) return new PassiveBonus { Atk = Big.Zero, Hp = Big.Zero };
            AtkHp b = _g.Defs.SkillBasePassive[d.Rarity];
            Big mult = _h.StarMult(sk.Stars).Mul(Big.Of(LevelMult(id)));
            return new PassiveBonus
            {
                Atk = mult.Mul(Big.Of(b.Atk * _h.SkillPassiveDmgMult())),
                Hp = mult.Mul(Big.Of(b.Hp * _h.SkillPassiveHpMult())),
            };
        }

        /// <summary>`ownedPassive()` — 보유 전부의 합(장착 슬롯과 무관 · 원작 «보유 효과»).</summary>
        public PassiveBonus OwnedPassive()
        {
            Big atk = Big.Zero, hp = Big.Zero;
            for (int i = 0; i < State.Skills.Count; i++)
            {
                PassiveBonus p = PassiveOf(State.Skills.KeyAt(i));
                atk = atk.Add(p.Atk);
                hp = hp.Add(p.Hp);
            }
            return new PassiveBonus { Atk = atk, Hp = hp };
        }
    }
}
