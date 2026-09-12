using System;
using System.Collections.Generic;
using Forge.Core.Data;
using Forge.Core.Pets;

namespace Forge.Core.Mounts
{
    /// <summary>소환 한 번의 결과 — 원작 `Mounts.summon` 반환 `{results:[{name, rarity, isNew, level}]}` + 효과음용 최고 등급.</summary>
    public sealed class MountSummonResult
    {
        public sealed class Item { public string Name; public string Rarity; public bool IsNew; public int Level; }
        public List<Item> Results = new List<Item>();
        /// <summary>`SFX.gacha(bestRarity)` 에 줄 등급.</summary>
        public string BestRarity;
    }

    /// <summary>
    /// 원작 `web/js/mounts.js` 의 `Mounts` 를 그대로 옮긴 순수 C# — 소환 레벨(`mountSummonRates` needed 표 · MAX 50) · 태엽 비용 ·
    /// 보관 250 · 장착 1마리 · 경험치 흡수(내림차순 · 인덱스 보정) · 기여(같은 등급 장비 8부위 합 × 레벨 × 승천 × 기술트리) · 구세이브 정리.
    /// 수치는 <see cref="GameData"/>(T2 JSON)와 <see cref="MountRules"/>(원작 코드 상수)에서만 온다. 난수는 <see cref="Rng"/> 로 받아 원작과
    /// **같은 순서로 같은 횟수** 소비한다(EditMode 가 `mount_vectors.json` 과 대조). UI·퀘스트·저장·씬 갱신은 호출자가 결과로 한다
    /// (원작의 `Quests.bump`·`SFX.gacha`·`Combat.recalcHero`·`Scene3D.refreshMount`·`saveGame` 자리).
    /// </summary>
    public sealed class MountSystem
    {
        readonly GameData _data;
        readonly MountRules _rules;
        readonly IMountHost _host;
        readonly Rng _rng;

        public MountState State;

        public MountSystem(GameData data, MountRules rules, IMountHost host, Rng rng, MountState state = null)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (host == null) throw new ArgumentNullException("host");
            if (rng == null) throw new ArgumentNullException("rng");
            _data = data; _rules = rules ?? MountRules.Original(); _host = host; _rng = rng;
            State = state ?? new MountState();
        }

        public MountRules Rules { get { return _rules; } }
        string[] Rarities { get { return _data.Defs.Rarities; } }
        int RarityIndex(string rarity) { return Array.IndexOf(Rarities, rarity); }
        MountTable Table { get { return _data.Balance.Mounts; } }

        // ===== ensure · 이관 =====

        /// <summary>
        /// 원작 `ensure()` — 인벤 상한 자르기 · 폐기 종 이름 이관 · 장착 목록 정리(없는 개체·중복을 걷고 1마리로). 구세이브 «맵 → 배열» 이관은
        /// 세이브 코덱(<see cref="MountSave"/>)이 JSON 단계에서 한다(원작 `migrateInventory` · 이 상태는 이미 배열이다).
        /// </summary>
        public void Ensure()
        {
            ClampInventory();
            MigrateSpecies();
            var seen = new HashSet<int>();
            var next = new List<int>();
            for (int k = 0; k < State.ActiveMounts.Count; k++)
            {
                int i = State.ActiveMounts[k];
                if (Inst(i) == null || seen.Contains(i)) continue;
                seen.Add(i);
                next.Add(i);
            }
            if (next.Count > _rules.MaxActiveMounts) next.RemoveRange(_rules.MaxActiveMounts, next.Count - _rules.MaxActiveMounts);
            State.ActiveMounts = next;
        }

        /// <summary>원작 `migrateSpecies()` — 폐기된 종을 새 종으로 이름만 갈아 끼운다. 반환 = 옮긴 개체 수.</summary>
        public int MigrateSpecies()
        {
            int moved = 0;
            for (int i = 0; i < State.Mounts.Count; i++)
            {
                Mount m = State.Mounts[i];
                string to;
                if (m != null && m.Name != null && _rules.LegacySpecies.TryGet(m.Name, out to)) { m.Name = to; moved++; }
            }
            return moved;
        }

        /// <summary>상한 초과분은 뒤에서 자른다(손상 세이브 방어).</summary>
        public void ClampInventory()
        {
            if (State.Mounts.Count > _rules.InvCap) State.Mounts.RemoveRange(_rules.InvCap, State.Mounts.Count - _rules.InvCap);
        }

        // ===== 보관 · 장착 조회 =====

        public int Count() { return State.Mounts.Count; }
        public int Space() { return Math.Max(0, _rules.InvCap - Count()); }
        /// <summary>없는 인덱스는 null(원작 `inst`).</summary>
        public Mount Inst(int idx) { return idx >= 0 && idx < State.Mounts.Count ? State.Mounts[idx] : null; }

        /// <summary>영웅이 실제로 올라타는 탈것 = 장착 목록의 첫 번째(없으면 null).</summary>
        public int? RiddenIdx() { return State.ActiveMounts.Count > 0 ? (int?)State.ActiveMounts[0] : null; }
        public Mount RiddenInst() { int? i = RiddenIdx(); return i.HasValue ? Inst(i.Value) : null; }
        /// <summary>원작 `ridden()` 은 **이름**을 돌려준다(3D 메시 선택·표시 경로). 개체는 <see cref="RiddenInst"/>.</summary>
        public string Ridden() { Mount m = RiddenInst(); return m != null ? m.Name : null; }
        public bool IsActive(int idx) { return State.ActiveMounts.IndexOf(idx) >= 0; }

        // ===== 소환 레벨 · 확률표 · 비용 =====

        /// <summary>누적 오픈 수로부터 소환 레벨 — needed 표를 1부터 올라가며 `MAX` 또는 미달에서 멈춘다.</summary>
        public int Level()
        {
            int lvl = 1;
            for (int l = 1; l < _rules.MaxLevel; l++)
            {
                MountSummonRow row = Table.SummonAt(l);
                if (row.NeededIsMax || State.MountOpens < row.Needed) break;
                lvl = l + 1;
            }
            return lvl;
        }

        /// <summary>다음 레벨까지 필요한 누적 오픈 수(MAX 면 null).</summary>
        public double? NextNeeded()
        {
            MountSummonRow row = Table.SummonAt(Level());
            return row.NeededIsMax ? (double?)null : row.Needed;
        }

        /// <summary>현재 레벨 시작 시점의 누적 오픈 수(레벨 1 이면 0) — 게이지 기준점.</summary>
        public double PrevNeeded()
        {
            int lvl = Level();
            return lvl > 1 ? Table.SummonAt(lvl - 1).Needed : 0;
        }

        /// <summary>현재 레벨의 등급 확률표(needed 제외 · 키 순서 = 추첨 순서).</summary>
        public OrderedMap<double> Rates() { return Table.SummonAt(Math.Min(_rules.MaxLevel, Level())).Rates; }

        /// <summary>기술트리 «탈것 소환 비용» 반영 실제 태엽 비용 = max(1, ceil(WINDERS_PER_SUMMON × count × mountCostMult)).</summary>
        public double WinderCost(double count = 1) { return Math.Max(1, Math.Ceiling(Table.WindersPerSummon * count * _host.MountCostMult())); }
        public bool CanSummon(double count = 1) { return _host.Winders >= WinderCost(count); }

        /// <summary>옵션 2개 — 장비·펫과 같은 풀에서.</summary>
        public List<Substat> RollSubs() { return SubstatRoll.Roll(_data.Defs, _rng, 2); }

        // ===== 기여 =====

        /// <summary>레벨 배율 — 장비·펫과 같은 커브(`Forge.levelMult`).</summary>
        public double LevelMult(Mount m) { return _host.LevelMult(m.Level); }

        /// <summary>장착 슬롯 1칸 = 장비 8부위 등가 → 같은 등급 장비 8부위 합 전체.</summary>
        public AtkHp BaseStat(string rarity) { return new AtkHp { Atk = _host.GearSumAtkAt(rarity), Hp = _host.GearSumHpAt(rarity) }; }

        /// <summary>장착 시 1마리 기여 = 등급 기준치 × 레벨 배율 × 승천 배율 × 기술트리(곱하는 순서도 원작과 같다).</summary>
        public void MountPower(Mount m, out Big atk, out Big hp)
        {
            AtkHp b = BaseStat(m.Rarity);
            Big mult = _host.StarMult(m.Stars).Mul(Big.Of(LevelMult(m)));
            atk = mult.Mul(Big.Of(b.Atk * _host.MountDmgMult()));
            hp = mult.Mul(Big.Of(b.Hp * _host.MountHpMult()));
        }

        /// <summary>장착 탈것의 고정 공격력·체력 + 서브스탯(없으면 전부 0). 장착은 1마리라 사실상 0~1회 도는 루프다.</summary>
        public void ActiveBonus(out Big atk, out Big hp, List<Substat> subs)
        {
            atk = Big.Zero; hp = Big.Zero;
            for (int k = 0; k < State.ActiveMounts.Count; k++)
            {
                Mount m = Inst(State.ActiveMounts[k]);
                if (m == null) continue;
                Big a, h;
                MountPower(m, out a, out h);
                atk = atk.Add(a); hp = hp.Add(h);
                if (subs != null && m.Subs != null) subs.AddRange(m.Subs);
            }
        }

        // ===== 경험치 =====

        /// <summary>레벨업 필요 경험치 = floor(80 × level^1.6).</summary>
        public double XpNeeded(int level) { return Math.Floor(80 * Math.Pow(level, 1.6)); }
        /// <summary>재료 경험치 = 30 × 3^등급 인덱스.</summary>
        public double XpValue(string rarity) { return 30 * Math.Pow(3, RarityIndex(rarity)); }

        public void AddXp(int idx, double amount)
        {
            Mount m = Inst(idx);
            if (m == null) return;
            m.Xp += amount;
            while (m.Level < _rules.IndivMaxLevel && m.Xp >= XpNeeded(m.Level))
            {
                m.Xp -= XpNeeded(m.Level);
                m.Level++;
            }
            if (m.Level >= _rules.IndivMaxLevel) m.Xp = 0;
        }

        /// <summary>
        /// 재료 탈것들을 흡수해 대상에 경험치로 — 재료는 **내림차순**으로 지우고 그때마다 대상·장착 인덱스를 보정한다(원작 🔑).
        /// <paramref name="consumed"/> &gt; 0 이면 호출자가 원작처럼 `Quests.bump('mountMerge')` 한다. 대상이 없으면 false.
        /// </summary>
        public bool AbsorbMaterials(int targetIdx, IList<int> materialIdxs, out int consumed)
        {
            consumed = 0;
            if (Inst(targetIdx) == null) return false;
            var idxs = new List<int>();
            if (materialIdxs != null)
                for (int k = 0; k < materialIdxs.Count; k++)
                {
                    int i = materialIdxs[k];
                    if (i == targetIdx || Inst(i) == null || idxs.Contains(i)) continue;
                    idxs.Add(i);
                }
            idxs.Sort((a, b) => b.CompareTo(a));
            double totalXp = 0;
            for (int k = 0; k < idxs.Count; k++)
            {
                int idx = idxs[k];
                Mount m = Inst(idx);
                totalXp += XpValue(m.Rarity) * LevelMult(m);
                consumed++;
                State.Mounts.RemoveAt(idx);
                var next = new List<int>(State.ActiveMounts.Count);
                for (int j = 0; j < State.ActiveMounts.Count; j++)
                {
                    int a = State.ActiveMounts[j];
                    if (a == idx) continue;
                    next.Add(a > idx ? a - 1 : a);
                }
                State.ActiveMounts = next;
                if (idx < targetIdx) targetIdx--;
            }
            AddXp(targetIdx, totalXp);
            return true;
        }

        // ===== 소환 =====

        /// <summary>실제로 소환될 개수 = min(max(1, floor(count) || 1), 보관함 여유) — 여유가 0 이면 0.</summary>
        public int SummonCount(double count = 1)
        {
            double f = Math.Floor(count);
            int c = double.IsNaN(f) || f == 0 ? 1 : (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, f));
            return Math.Min(Math.Max(1, c), Space());
        }

        /// <summary>count번 소환 — 태엽 선결제 · 기술트리 보너스 마리(비용 없음 · 상한 안에서) · 장착이 비어 있을 때만 자동 장착. 못 하면 null.</summary>
        public MountSummonResult Summon(double count = 1)
        {
            Ensure();
            int n = SummonCount(count);
            if (n < 1 || !CanSummon(n)) return null;
            _host.Winders -= WinderCost(n);
            var res = new MountSummonResult();
            int rolls = n;
            // 원작 `U.chance(...)` 는 확률이 0 이어도 난수를 하나 먹는다 — 순서를 지킨다.
            for (int i = 0; i < n; i++) if (_rng.Chance(_host.ExtraMountChance())) rolls++;
            for (int i = 0; i < rolls; i++)
            {
                if (Space() <= 0) break;
                State.MountOpens++;
                string rarity = _rng.WeightedPick(Rates());
                string name = _rng.Choice(Table.Names[rarity]);
                var m = new Mount { Name = name, Rarity = rarity, Level = 1, Xp = 0, Stars = _host.MountAscendCount(), Subs = RollSubs() };
                State.Mounts.Add(m);
                int idx = State.Mounts.Count - 1;
                bool isNew = true;
                for (int k = 0; k < State.Mounts.Count; k++) if (k != idx && State.Mounts[k].Name == name) { isNew = false; break; }
                if (State.ActiveMounts.Count == 0) Equip(idx);
                res.Results.Add(new MountSummonResult.Item { Name = name, Rarity = rarity, IsNew = isNew, Level = 1 });
            }
            if (res.Results.Count == 0) return null;
            string best = res.Results[0].Rarity;
            for (int i = 0; i < res.Results.Count; i++) if (RarityIndex(res.Results[i].Rarity) > RarityIndex(best)) best = res.Results[i].Rarity;
            res.BestRarity = best;
            return res;
        }

        // ===== 장착 =====

        /// <summary>장착/해제 토글 — 장착은 1마리(다른 탈것을 장착하면 이전 것은 자동 해제 = 단일 슬롯 교체). 없는 개체는 false.</summary>
        public bool Equip(int idx)
        {
            Ensure();
            if (Inst(idx) == null) return false;
            int pos = State.ActiveMounts.IndexOf(idx);
            if (pos >= 0) State.ActiveMounts.RemoveAt(pos);
            else State.ActiveMounts = new List<int> { idx };
            return true;
        }

        /// <summary>타고 있는 탈것을 이 탈것으로 바꾼다(토글 아님 — 상세 팝업 «타기»).</summary>
        public bool SetRidden(int idx)
        {
            Ensure();
            if (Inst(idx) == null) return false;
            State.ActiveMounts = new List<int> { idx };
            return true;
        }
    }
}
