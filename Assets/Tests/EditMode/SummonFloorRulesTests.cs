using System.IO;
using NUnit.Framework;
using Forge.Core.Data;

namespace Forge.Tests
{
    /// <summary>T449 — 소환진(`.sr-floor`) 폭·비율 네 칸이 표(`SummonFxUi.json` layout)에 정본 값으로 있다.
    /// 정본 style.css 5800 `.sr-body.stage:not(.one) .sr-floor { width: 88%; aspect-ratio: 2.5/1 }` · 5771 `.stage.one .sr-floor { width: 64%; aspect-ratio: 2.6/1 }`.
    /// 캐노피(`canopy_*`)와 같은 꼴(`w_f` · `aspect`)이라 읽는 줄도 같다.</summary>
    public class SummonFloorRulesTests
    {
        static string Root_()
        {
            return Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
        }

        static JsonObject Layout_()
        {
            JsonObject root = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Root_(), "Assets", "Forge", "Resources", "SummonFxUi.json")));
            return J.Obj(J.Require(root, "layout"));
        }

        [Test]
        public void 소환진_네_칸은_정본_5800_5771_그대로다()
        {
            JsonObject l = Layout_();
            Assert.AreEqual(0.88, J.Num(J.Require(l, "floor_w_f")), 1e-9, "5800 .stage:not(.one) .sr-floor width 88%");
            Assert.AreEqual(2.5, J.Num(J.Require(l, "floor_aspect")), 1e-9, "5800 aspect-ratio 2.5/1");
            Assert.AreEqual(0.64, J.Num(J.Require(l, "floor_one_w_f")), 1e-9, "5771 .stage.one .sr-floor width 64%");
            Assert.AreEqual(2.6, J.Num(J.Require(l, "floor_one_aspect")), 1e-9, "5771 aspect-ratio 2.6/1");
        }

        [Test]
        public void 소환진은_캐노피와_같은_꼴이라_키_짝이_나란하다()
        {
            JsonObject l = Layout_();
            foreach (string k in new[] { "canopy_", "canopy_one_", "floor_", "floor_one_" })
            {
                Assert.IsTrue(l.Has(k + "w_f"), k + "w_f");
                Assert.IsTrue(l.Has(k + "aspect"), k + "aspect");
                Assert.Greater(J.Num(l[k + "w_f"]), 0.0); Assert.LessOrEqual(J.Num(l[k + "w_f"]), 1.0, "폭은 그리드 폭 비율");
                Assert.Greater(J.Num(l[k + "aspect"]), 1.0, "가로로 긴 타원");
            }
        }
    }
}
