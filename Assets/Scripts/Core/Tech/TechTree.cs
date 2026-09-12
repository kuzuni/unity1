using System;
using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Tech
{
    /// <summary>트리 한 행(그리기 골격) — `{ids, tier, first, lastOfTier}`.</summary>
    public sealed class TechRow
    {
        public string[] Ids;
        public int Tier;
        public bool First, LastOfTier;

        public bool Has(string id) { return IndexOf(id) >= 0; }
        public int IndexOf(string id)
        {
            for (int i = 0; i < Ids.Length; i++) if (Ids[i] == id) return i;
            return -1;
        }
    }

    /// <summary>'총 보너스' 팝업 한 줄 `{id, label, text}` — id 는 타입.</summary>
    public sealed class TechBonusLine
    {
        public string Id, Label, Text;
    }

    /// <summary>
    /// 기술 트리(대장간 연구) — 정본 `web/js/techtree.js` 의 규칙을 그대로. 수치는 전부 <see cref="TechTable"/>(tech.json).
    /// 분기의 모든 타입이 매 단계마다 반복된다(노드 id = `타입@단계`) · 노드 하나는 최대 5레벨 · 보너스는 그 타입의 5개 단계 노드 레벨을 전부 합산 ·
    /// 비용·시간은 단계·레벨로 뛴다 · 연구는 물약 선결제 + 실시간 타이머(전체 트리 통틀어 동시 1건) · 취소 없음 · 젬 건너뛰기 · [완료] 를 눌러야 레벨이 오른다.
    /// 상태(<see cref="State"/>)는 세이브 조각이고 «지금» 은 절대시각 ms 로 받는다(원작 `U.now()`) — 순수 계산.
    /// </summary>
    public sealed class TechTree
    {
        public readonly TechTable Table;
        readonly GameDefs _defs;
        readonly Dictionary<string, List<TechRow>> _rowsCache = new Dictionary<string, List<TechRow>>();

        /// <summary>원작 `S.tech`·`S.techResearch`. 바꿔 끼울 수 있다(세이브 로드).</summary>
        public TechState State;

        /// <param name="defs">`gamedata.json`(SLOTS·SLOT_MAIN·SLOT_KR — 총 보너스 줄의 부위 펼침).</param>
        public TechTree(TechTable table, GameDefs defs, TechState state)
        {
            if (table == null) throw new ArgumentNullException("table");
            if (defs == null) throw new ArgumentNullException("defs");
            Table = table; _defs = defs; State = state ?? new TechState();
        }

        // ===== 노드 인스턴스 = 타입@단계 =====
        public string Nid(string type, int tier) { return type + "@" + tier; }
        public string TypeOf(string id) { int i = id.IndexOf('@'); return i < 0 ? id : id.Substring(0, i); }
        public int TierOf(string id)
        {
            int i = id.IndexOf('@');
            if (i < 0) return 1;
            // 원작 `+String(id).split('@')[1]` — 정수가 아니거나 범위 밖이면 1.
            int t;
            if (!int.TryParse(id.Substring(i + 1), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out t)) return 1;
            return t >= 1 && t <= Table.Tiers ? t : 1;
        }
        public TechNodeDef Def(string id) { return Table.Node(TypeOf(id)); }

        /// <summary>분기의 전 노드(그리는 순서 = 1단계 타입 전부 → 2단계 …). 없는 분기면 빈 목록.</summary>
        public List<string> NodesOf(string branchId)
        {
            var b = Table.Branch(branchId);
            var outList = new List<string>();
            if (b == null) return outList;
            for (int t = 1; t <= Table.Tiers; t++) for (int i = 0; i < b.Types.Length; i++) outList.Add(Nid(b.Types[i], t));
            return outList;
        }
        public int Level(string id) { return State.Level(id); }
        public bool IsMax(string id) { return Level(id) >= Table.MaxLevel; }
        public TechBranch BranchOf(string id)
        {
            string ty = TypeOf(id);
            for (int i = 0; i < Table.Branches.Count; i++) if (Table.Branches[i].HasType(ty)) return Table.Branches[i];
            return null;
        }
        public List<string> TierNodes(string branchId, int tier)
        {
            var b = Table.Branch(branchId);
            var outList = new List<string>();
            if (b == null) return outList;
            for (int i = 0; i < b.Types.Length; i++) outList.Add(Nid(b.Types[i], tier));
            return outList;
        }

        // ===== 해금 = 바로 위 행에 «실제로 그어진 선» 을 따라 =====
        // 폭이 같은 두 행 → 열마다 세로 레일 한 가닥(부모 = 같은 열 하나) · 폭이 다른 두 행 → fork/converge(부모 = 위 행 전부).
        public List<TechRow> RowsCached(string branchId)
        {
            List<TechRow> rows;
            if (!_rowsCache.TryGetValue(branchId, out rows)) { rows = Rows(branchId); _rowsCache[branchId] = rows; }
            return rows;
        }
        public List<string> ParentsOf(string id)
        {
            var outList = new List<string>();
            var b = BranchOf(id);
            if (b == null) return outList;
            var rows = RowsCached(b.Id);
            int i = -1;
            for (int k = 0; k < rows.Count; k++) if (rows[k].Has(id)) { i = k; break; }
            if (i <= 0) return outList;
            string[] prev = rows[i - 1].Ids, cur = rows[i].Ids;
            if (prev.Length == cur.Length) { outList.Add(prev[rows[i].IndexOf(id)]); return outList; }
            outList.AddRange(prev);
            return outList;
        }
        public bool IsUnlocked(string id)
        {
            var ps = ParentsOf(id);
            for (int i = 0; i < ps.Count; i++) if (Level(ps[i]) < 1) return false;
            return true;
        }
        /// <summary>해금 대기 중인 노드가 '무엇을 올려야 열리는지' — 아직 0레벨인 부모 노드 id 목록.</summary>
        public List<string> LockedBy(string id)
        {
            var outList = new List<string>();
            var ps = ParentsOf(id);
            for (int i = 0; i < ps.Count; i++) if (Level(ps[i]) < 1) outList.Add(ps[i]);
            return outList;
        }

        // ===== 그리기 골격 =====
        // 단계가 가로 블록 · 모든 단계가 같은 행 패턴('첫 타입 단독 → 나머지 2개씩') · 마지막 단계에서 꼬리 2노드 행은 한 노드씩 두 행으로 쪼개 맨 아래가 노드 1개로 수렴.
        List<string[]> Chunk2(string[] types, int offset, int tier)
        {
            var g = new List<string[]>();
            for (int i = offset; i < types.Length; i += 2)
            {
                int n = Math.Min(2, types.Length - i);
                var ids = new string[n];
                for (int k = 0; k < n; k++) ids[k] = Nid(types[i + k], tier);
                g.Add(ids);
            }
            return g;
        }
        public List<TechRow> Rows(string branchId)
        {
            var b = Table.Branch(branchId);
            var outList = new List<TechRow>();
            if (b == null) return outList;
            for (int t = 1; t <= Table.Tiers; t++)
            {
                string[] ty = b.Types;
                List<string[]> groups;
                if (ty.Length > 1)
                {
                    groups = new List<string[]> { new[] { Nid(ty[0], t) } };
                    groups.AddRange(Chunk2(ty, 1, t));
                }
                else groups = Chunk2(ty, 0, t);
                var tail = groups[groups.Count - 1];
                if (t == Table.Tiers && tail.Length == 2)
                {
                    groups.RemoveAt(groups.Count - 1);
                    groups.Add(new[] { tail[0] });
                    groups.Add(new[] { tail[1] });
                }
                for (int i = 0; i < groups.Count; i++)
                    outList.Add(new TechRow { Ids = groups[i], Tier = t, First = i == 0, LastOfTier = i == groups.Count - 1 });
            }
            return outList;
        }
        /// <summary>연결선은 단계와 단계 사이에만(같은 단계 안의 행들은 서로 부모-자식이 아니다).</summary>
        public bool TierBreak(TechRow upper, TechRow lower) { return upper != null && lower != null && upper.Tier != lower.Tier; }

        public string Roman(int tier) { return tier >= 1 && tier <= Table.Roman.Length ? Table.Roman[tier - 1] : Table.Roman[0]; }
        public string TierLabel(string id) { return Roman(TierOf(id)); }

        /// <summary>분기 진행률(%): 분기 내 모든 노드 레벨 합 ÷ (노드 수 × MAX_LEVEL).</summary>
        public double BranchProgress(string branchId)
        {
            var ids = NodesOf(branchId);
            if (ids.Count == 0) return 0;
            double sum = 0;
            for (int i = 0; i < ids.Count; i++) sum += Level(ids[i]);
            return sum / (ids.Count * Table.MaxLevel) * 100;
        }

        // ===== 비용·시간 =====
        /// <summary>레벨 하나(1-based)를 구매하는 데 드는 물약 — '기술 연구 비용' 노드로 할인. 폐기된 id 면 null.</summary>
        public double? Cost(string id, int level)
        {
            var def = Def(id);
            if (def == null) return null;
            double raw = def.Base * Math.Pow(Table.LvMult, level - 1) * Math.Pow(Table.TierMult, TierOf(id) - 1);
            return Math.Max(1, Math.Ceiling(raw * TechCostMult()));
        }
        /// <summary>레벨 하나를 연구하는 데 걸리는 실시간(초) — '기술 연구 타이머' 노드로 단축.</summary>
        public double? Time(string id, int level)
        {
            if (Def(id) == null) return null;
            double raw = Table.TimeBase * Math.Pow(Table.TimeLvMult, level - 1) * Math.Pow(Table.TimeTierMult, TierOf(id) - 1);
            return Math.Max(1, Math.Ceiling(raw * TechTimeMult()));
        }
        public double? NextCost(string id)
        {
            if (Def(id) == null) return null;
            int lv = Level(id);
            return lv >= Table.MaxLevel ? (double?)null : Cost(id, lv + 1);
        }
        public double? NextTime(string id)
        {
            if (Def(id) == null) return null;
            int lv = Level(id);
            return lv >= Table.MaxLevel ? (double?)null : Time(id, lv + 1);
        }

        // ===== 연구 흐름 =====
        /// <summary>현재 연구 중인 노드 id(없으면 null).</summary>
        public string ResearchingId() { return State.Research != null ? State.Research.Id : null; }

        public bool CanStart(string id, IWallet wallet)
        {
            if (State.Research != null) return false;
            if (!IsUnlocked(id)) return false;
            double? c = NextCost(id);
            return c != null && wallet.Potions >= c.Value;
        }

        /// <summary>물약을 선결제하고 타이머를 건다(endsAt = now + time × 1000).</summary>
        public bool Start(string id, IWallet wallet, double nowMs)
        {
            if (!CanStart(id, wallet)) return false;
            wallet.Potions -= NextCost(id).Value;
            State.Research = new TechResearch(id, nowMs + NextTime(id).Value * 1000);
            return true;
        }

        /// <summary>연구 취소 없음(주인 지시) — 항상 거절 · 환불 없음.</summary>
        public bool Cancel() { return false; }

        /// <summary>젬 건너뛰기 비용 — 남은 시간 10분당 젬 1(대장간과 같은 규칙 · 원작 `Math.ceil(remainMin / 10)`).</summary>
        public double GemSkipCost(double nowMs)
        {
            if (State.Research == null) return 0;
            double remainMin = Math.Max(0, (State.Research.EndsAt - nowMs) / 60000);
            return Math.Ceiling(remainMin / GemSkipMinutesPerGem);
        }
        /// <summary>원작 `gemSkipCost` 의 «10분당 젬 1» — 표가 아니라 규칙 안의 나눗수라 코드에 둔다(대장간 `forge.js` 도 같은 값).</summary>
        public const double GemSkipMinutesPerGem = 10;

        public bool GemSkip(IWallet wallet, double nowMs)
        {
            double cost = GemSkipCost(nowMs);
            if (State.Research == null || wallet.Gems < cost) return false;
            wallet.Gems -= cost;
            State.Research.EndsAt = nowMs - 1;
            return true;
        }

        /// <summary>연구 시간은 끝났지만 아직 [완료] 를 안 눌러 레벨이 안 오른 상태 — 이때도 «동시 1건» 가드가 걸린다.</summary>
        public bool IsDone(double nowMs) { return State.Research != null && nowMs >= State.Research.EndsAt; }

        /// <summary>[완료] — 여기서만 레벨이 오른다. 성공 시 오른 노드 id 를 준다(호출자가 퀘스트·토스트·스탯 재계산).</summary>
        public bool Claim(double nowMs, out string claimedId)
        {
            claimedId = null;
            if (!IsDone(nowMs)) return false;
            string id = State.Research.Id;
            State.Tech[id] = Level(id) + 1;
            State.Research = null;
            claimedId = id;
            return true;
        }
        public bool Claim(double nowMs) { string id; return Claim(nowMs, out id); }

        // ===== 세이브 이관 =====
        /// <summary>
        /// 구세이브 이관: 폐기 노드(LEGACY_MAP) → 살아남은 타입 · 타입 이름 키 → `타입@1` · 전 분기×전 단계 인스턴스를 채우고 목록에 없는 키는 지운다 ·
        /// 진행 중이던 연구가 사라진 노드면 취소하고 선결제한 물약을 돌려준다(같은 타입 1단계 기준).
        /// </summary>
        public void Ensure(IWallet wallet)
        {
            if (State.Tech == null) State.Tech = new Dictionary<string, int>();
            var tech = State.Tech;
            foreach (var kv in Table.LegacyMap)
            {
                string oldId = kv.Key;
                if (Table.Node(oldId) != null || !tech.ContainsKey(oldId)) continue;
                int lv = tech[oldId];
                for (int i = 0; i < kv.Value.Length; i++)
                {
                    string newId = kv.Value[i];
                    if (Table.Node(newId) != null) tech[newId] = Math.Max(Level(newId), lv);
                }
                tech.Remove(oldId);
            }
            foreach (string id in new List<string>(tech.Keys))
            {
                if (id.IndexOf('@') >= 0) continue;
                int lv = Clamp(tech[id], 0, Table.MaxLevel);
                if (Table.Node(id) != null)
                {
                    string first = Nid(id, 1);
                    tech[first] = Math.Max(Level(first), lv);
                }
                tech.Remove(id);
            }
            var valid = new HashSet<string>();
            for (int b = 0; b < Table.Branches.Count; b++)
            {
                var ids = NodesOf(Table.Branches[b].Id);
                for (int i = 0; i < ids.Count; i++) { valid.Add(ids[i]); tech[ids[i]] = Clamp(Level(ids[i]), 0, Table.MaxLevel); }
            }
            foreach (string id in new List<string>(tech.Keys)) if (!valid.Contains(id)) tech.Remove(id);
            if (State.Research != null && !valid.Contains(State.Research.Id))
            {
                string type = TypeOf(State.Research.Id);
                string heir = Table.Node(type) != null ? Nid(type, 1) : null;
                if (heir != null && wallet != null) wallet.Potions += Cost(heir, Math.Min(Level(heir) + 1, Table.MaxLevel)).Value;
                State.Research = null;
            }
        }

        static int Clamp(int v, int a, int b) { return Math.Min(b, Math.Max(a, v)); }

        // ===== 효과 =====
        /// <summary>타입 총 레벨 = 그 타입의 5개 단계 노드 레벨 합.</summary>
        public int TypeLevel(string type)
        {
            int sum = 0;
            for (int t = 1; t <= Table.Tiers; t++) sum += Level(Nid(type, t));
            return sum;
        }
        /// <summary>포인트당 수치 × 총 레벨 = 그 타입의 총 효과(%). 노드 id 를 줘도 타입으로 본다 · 없는 타입은 0.</summary>
        public double Pct(string idOrType)
        {
            string type = TypeOf(idOrType);
            var d = Table.Node(type);
            return d != null ? TypeLevel(type) * d.Per : 0;
        }
        /// <summary>노드 하나가 지금 내고 있는 몫.</summary>
        public double NodeTotal(string id) { var d = Def(id); return d != null ? Level(id) * d.Per : 0; }
        public double TotalOf(string idOrType) { return Pct(idOrType); }
        public string UnitOf(string idOrType) { var d = Table.Node(TypeOf(idOrType)); return d != null && d.Unit != null ? d.Unit : "%"; }
        public string GainNote() { return "레벨당"; }

        /// <summary>현재 연구 상태에서 실제로 붙어 있는 누적 보너스만 한 줄씩(값 0 제외) — 분기 정의 순서 · expand 노드는 부위 수만큼 펼친다.</summary>
        public List<TechBonusLine> TotalBonuses()
        {
            var outList = new List<TechBonusLine>();
            for (int b = 0; b < Table.Branches.Count; b++)
            {
                var types = Table.Branches[b].Types;
                for (int i = 0; i < types.Length; i++)
                {
                    string id = types[i];
                    TechBonusMeta meta;
                    if (!Table.Bonus.TryGet(id, out meta)) continue;
                    double value = TotalOf(id);
                    if (value == 0) continue;
                    string unit = UnitOf(id) == "%" ? "%" : "";
                    string text = "+" + NumFmt.Fmt(value) + unit;
                    if (meta.Expand != null)
                    {
                        foreach (string slot in ExpandSlots(meta.Expand))
                            outList.Add(new TechBonusLine { Id = id, Label = _defs.SlotKr[slot] + " " + meta.Label, Text = text });
                    }
                    else outList.Add(new TechBonusLine { Id = id, Label = meta.Label, Text = text });
                }
            }
            return outList;
        }
        /// <summary>'atk'(주스탯 atk 부위) · 'hp' · 'all'(8부위) — 원작 `EXPAND_SLOTS`.</summary>
        public List<string> ExpandSlots(string expand)
        {
            var outList = new List<string>();
            for (int i = 0; i < _defs.Slots.Length; i++)
            {
                string s = _defs.Slots[i];
                if (expand == "all" || _defs.SlotMain[s] == expand) outList.Add(s);
            }
            return outList;
        }

        // ===== 다른 모듈에서 참조하는 효과 배율 =====
        // 증가형은 (1 + %/100) · 시간 단축은 ÷(1+%) · 비용 감소는 ×(1-%)(하한 0.1).
        // ① 힘 · 탈것
        public double GearAtkMult() { return 1 + Pct("weaponMastery") / 100; }
        public double GearHpMult() { return 1 + Pct("armorMastery") / 100; }
        /// <summary>장비 최대 강화 레벨 +N(레벨 수 · % 아님).</summary>
        public double GearMaxLevelBonus() { return Pct("gearMaxLevel"); }
        public double MountDmgMult() { return 1 + Pct("mountDmg") / 100; }
        public double MountHpMult() { return 1 + Pct("mountHp") / 100; }
        public double MountCostMult() { return Math.Max(0.1, 1 - Pct("mountCost") / 100); }
        public double ExtraMountChance() { return Pct("extraMount") / 100; }
        // ② 대장간
        public double ForgeTimeMult() { return 1 / (1 + Pct("forgeTimer") / 100); }
        public double ForgeCostMult() { return Math.Max(0.1, 1 - Pct("forgeCost") / 100); }
        public double SellPriceMult() { return 1 + Pct("sellPrice") / 100; }
        public double ThiefHammerMult() { return 1 + Pct("thiefHammer") / 100; }
        public double ThiefCoinMult() { return 1 + Pct("thiefCoin") / 100; }
        public double AutoForgeSlotBonus() { return TotalOf("autoForgeSlot"); }
        public double FreeForgeChance() { return Pct("freeForge") / 100; }
        public double OfflineCapMult() { return 1 + Pct("offlineCap") / 100; }
        public double OfflineCoinMult() { return 1 + Pct("offlineCoin") / 100; }
        public double OfflineHammerMult() { return 1 + Pct("offlineHammer") / 100; }
        // ③ 스킬 · 펫 & 기술
        public double TechTimeMult() { return 1 / (1 + Pct("techTimer") / 100); }
        public double TechCostMult() { return Math.Max(0.1, 1 - Pct("techCost") / 100); }
        public double SkillDmgMult() { return 1 + Pct("skillDmg") / 100; }
        public double SkillPassiveDmgMult() { return 1 + Pct("skillPassiveDmg") / 100; }
        public double SkillPassiveHpMult() { return 1 + Pct("skillPassiveHp") / 100; }
        public double PetDmgMult() { return 1 + Pct("petDmg") / 100; }
        public double PetHpMult() { return 1 + Pct("petHp") / 100; }
        public double SkillSummonCostMult() { return Math.Max(0.1, 1 - Pct("skillSummonCost") / 100); }
        public double HatchSpeedMult() { return 1 / (1 + Pct("hatchTimer") / 100); }
        public double ExtraEggChance() { return Pct("extraEgg") / 100; }
        public double DungeonTicketMult() { return 1 + Pct("dungeonTicket") / 100; }
        public double DungeonPotionMult() { return 1 + Pct("dungeonPotion") / 100; }
    }
}
