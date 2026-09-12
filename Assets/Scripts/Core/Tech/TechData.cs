using Forge.Core.Ascend;
using Forge.Core.Data;

namespace Forge.Core.Tech
{
    /// <summary>
    /// `tech.json`(T24 · `tools/export_data.js` 가 정본 `techtree.js`·`ascension.js` 의 표 칸만 뽑는다) 의 뿌리.
    /// <see cref="GameData"/> 의 7파일과 별도로 읽는다 — 파일 읽기는 호출자 몫(<see cref="GameData"/> 와 같은 규약).
    /// </summary>
    public sealed class TechData
    {
        public const string File = "tech.json";

        public TechTable Tech;
        public AscensionTable Ascension;

        public static TechData Load(string json)
        {
            var o = MiniJson.ParseObject(json);
            return new TechData
            {
                Tech = TechTable.From(J.Obj(J.Require(o, "TechTree"))),
                Ascension = AscensionTable.From(J.Obj(J.Require(o, "Ascension")))
            };
        }
    }
}
