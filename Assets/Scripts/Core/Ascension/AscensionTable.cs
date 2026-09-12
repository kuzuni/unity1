using Forge.Core.Data;

namespace Forge.Core.Ascend
{
    /// <summary>승천 **표**(T24) — `tech.json` 의 `Ascension` 칸(정본 `web/js/ascension.js` 의 STAR_MULT·LINES·LINE_KR·LINE_ICON·FORGE_LEVEL).</summary>
    public sealed class AscensionTable
    {
        /// <summary>별 1개당 능력치 배율(1e9 · 승천 2회차부터 2^53 을 넘어 스탯은 <see cref="Big"/>).</summary>
        public double StarMult;
        /// <summary>라인 4: forge · skill · pet · mount.</summary>
        public string[] Lines;
        public OrderedMap<string> LineKr, LineIcon;
        /// <summary>대장간 승천 도달 레벨.</summary>
        public int ForgeLevel;
        public JsonObject Raw;

        public bool HasLine(string line)
        {
            for (int i = 0; i < Lines.Length; i++) if (Lines[i] == line) return true;
            return false;
        }

        public static AscensionTable From(JsonObject o)
        {
            return new AscensionTable
            {
                StarMult = J.Num(J.Require(o, "STAR_MULT")),
                Lines = J.StrArr(J.Require(o, "LINES")),
                LineKr = J.StrMap(J.Require(o, "LINE_KR")),
                LineIcon = J.StrMap(J.Require(o, "LINE_ICON")),
                ForgeLevel = J.Int(J.Require(o, "FORGE_LEVEL")),
                Raw = o
            };
        }
    }
}
