using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Tech
{
    /// <summary>
    /// 기술 트리 **표**(T24) — `tech.json` 의 `TechTree` 칸(정본 `web/js/techtree.js` 의 TIERS·MAX_LEVEL·BRANCHES·NODES·BONUS·LEGACY_MAP·배수 다섯·ROMAN).
    /// 수치는 전부 여기서 온다 — <see cref="TechTree"/> 는 규칙만 든다.
    /// </summary>
    public sealed class TechTable
    {
        /// <summary>트리 단계 I~V.</summary>
        public int Tiers;
        /// <summary>노드당 최대 레벨(원본 'N/5').</summary>
        public int MaxLevel;
        /// <summary>분기 3(힘·탈것 / 대장간 / 스킬·펫&amp;기술) — 정의 순서가 곧 그리는 순서·보너스 줄 순서.</summary>
        public List<TechBranch> Branches;
        /// <summary>타입 → 노드 정의(per·base·unit). 같은 타입이 1~5단계에 반복돼 노드 인스턴스(`타입@단계`)가 된다.</summary>
        public OrderedMap<TechNodeDef> Nodes;
        /// <summary>'총 보너스' 팝업 줄 메타(label · expand).</summary>
        public OrderedMap<TechBonusMeta> Bonus;
        /// <summary>구세이브 폐기 노드 id → 새 노드 id 목록.</summary>
        public OrderedMap<string[]> LegacyMap;
        /// <summary>노드 안 1레벨당 비용 증가율.</summary>
        public double LvMult;
        /// <summary>트리 단계가 하나 내려갈 때 붙는 비용 배수.</summary>
        public double TierMult;
        /// <summary>연구 시간 기준(초) · 레벨 배수 · 단계 배수.</summary>
        public double TimeBase, TimeLvMult, TimeTierMult;
        /// <summary>단계 표기 I~V.</summary>
        public string[] Roman;
        public JsonObject Raw;

        public TechBranch Branch(string id)
        {
            for (int i = 0; i < Branches.Count; i++) if (Branches[i].Id == id) return Branches[i];
            return null;
        }

        public TechNodeDef Node(string type) { TechNodeDef d; return Nodes.TryGet(type, out d) ? d : null; }

        public static TechTable From(JsonObject o)
        {
            return new TechTable
            {
                Tiers = J.Int(J.Require(o, "TIERS")),
                MaxLevel = J.Int(J.Require(o, "MAX_LEVEL")),
                Branches = J.List(J.Require(o, "BRANCHES"), TechBranch.From),
                Nodes = J.Map(J.Require(o, "NODES"), TechNodeDef.From),
                Bonus = J.Map(J.Require(o, "BONUS"), TechBonusMeta.From),
                LegacyMap = J.StrArrMap(J.Require(o, "LEGACY_MAP")),
                LvMult = J.Num(J.Require(o, "LV_MULT")),
                TierMult = J.Num(J.Require(o, "TIER_MULT")),
                TimeBase = J.Num(J.Require(o, "TIME_BASE")),
                TimeLvMult = J.Num(J.Require(o, "TIME_LV_MULT")),
                TimeTierMult = J.Num(J.Require(o, "TIME_TIER_MULT")),
                Roman = J.StrArr(J.Require(o, "ROMAN")),
                Raw = o
            };
        }
    }

    /// <summary>분기 `{id, name, icon, types}`.</summary>
    public sealed class TechBranch
    {
        public string Id, Name, Icon;
        public string[] Types;

        public bool HasType(string type)
        {
            for (int i = 0; i < Types.Length; i++) if (Types[i] == type) return true;
            return false;
        }

        public static TechBranch From(object v)
        {
            var o = J.Obj(v);
            return new TechBranch { Id = J.Str(o["id"]), Name = J.Str(o["name"]), Icon = J.Str(o["icon"]), Types = J.StrArr(o["types"]) };
        }
    }

    /// <summary>노드 정의 `{name, desc, icon, per, base, unit?}` — per = 1업당 수치(단위는 unit · 없으면 %).</summary>
    public sealed class TechNodeDef
    {
        public string Name, Desc, Icon;
        public double Per, Base;
        /// <summary>절대 수치 노드의 단위('레벨'·'개') · 없으면 null(= %).</summary>
        public string Unit;

        public static TechNodeDef From(object v)
        {
            var o = J.Obj(v);
            return new TechNodeDef
            {
                Name = J.Str(o["name"]), Desc = J.Str(o["desc"]), Icon = J.Str(o["icon"]),
                Per = J.Num(o["per"]), Base = J.Num(o["base"]),
                Unit = o.Has("unit") ? J.Str(o["unit"]) : null
            };
        }
    }

    /// <summary>총 보너스 줄 메타 `{label, expand?}` — expand: 'atk'(공격 4부위) · 'hp'(체력 4부위) · 'all'(8부위) · 없으면 한 줄.</summary>
    public sealed class TechBonusMeta
    {
        public string Label;
        public string Expand;

        public static TechBonusMeta From(object v)
        {
            var o = J.Obj(v);
            return new TechBonusMeta { Label = J.Str(o["label"]), Expand = o.Has("expand") ? J.Str(o["expand"]) : null };
        }
    }
}
