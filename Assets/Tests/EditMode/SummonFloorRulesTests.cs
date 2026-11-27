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

        static double[] Ease_(string key)
        {
            double[] e = J.NumArr(J.Require(Layout_(), key));
            Assert.IsNotNull(e, key + " 는 cubic-bezier 넷이어야 한다");
            Assert.AreEqual(4, e.Length, key);
            return e;
        }

        /// <summary>T499 — «들어오는 세 겹» 의 이징 셋이 표에 정본 값으로 있다(와이프 6193 · 천개 5839 · 소환진 5769).</summary>
        [Test]
        public void 들어오는_세_겹의_이징은_정본_6193_5839_5769_그대로다()
        {
            double[] w = Ease_("wipe_ease");
            Assert.AreEqual(0.2, w[0], 1e-9); Assert.AreEqual(0.7, w[1], 1e-9);
            Assert.AreEqual(0.35, w[2], 1e-9); Assert.AreEqual(1.0, w[3], 1e-9);
            foreach (string k in new[] { "canopy_in_ease", "floor_in_ease" })
            {
                double[] e = Ease_(k);
                Assert.AreEqual(0.2, e[0], 1e-9, k); Assert.AreEqual(0.9, e[1], 1e-9, k);
                Assert.AreEqual(0.3, e[2], 1e-9, k); Assert.AreEqual(1.0, e[3], 1e-9, k);
            }
        }

        /// <summary>T499 ⓒ — 소환진 도입 여섯 칸(정본 5767~5770 `srfloor .43s … .08s forwards` · 5801~5804 알파 0→1 · 배율 .74→1).</summary>
        [Test]
        public void 소환진_도입_여섯_칸은_정본_5767_5804_그대로다()
        {
            JsonObject l = Layout_();
            Assert.AreEqual(430.0, J.Num(J.Require(l, "floor_in_ms")), 1e-9, "5769 .43s");
            Assert.AreEqual(80.0, J.Num(J.Require(l, "floor_in_delay_ms")), 1e-9, "5769 .08s 지연");
            Assert.AreEqual(0.74, J.Num(J.Require(l, "floor_in_scale")), 1e-9, "5802 scale(.74)");
            Assert.AreEqual(0.0, J.Num(J.Require(l, "floor_in_a0")), 1e-9, "5802 opacity 0");
            Assert.AreEqual(1.0, J.Num(J.Require(l, "floor_in_a1")), 1e-9, "5803 opacity 1");
        }

        /// <summary>
        /// T499 — 그 이징이 **실제로 갈라지는** 자리를 못 박는다. 셋이 빈 자리였던 동안 클론이 쓰던 두 대체물은
        /// ⓐ 생 비율(선형)과 ⓑ 손으로 쓴 <c>1 − (1−k)³</c> 였다 — 그 둘과의 어긋남이 T33 77회차의 등재 근거다.
        /// 값이 다시 선형·세제곱으로 돌아가면 이 자가 빨개진다.
        /// </summary>
        [Test]
        public void 와이프_이징은_선형보다_k_0_3_에서_0_42_앞선다()
        {
            double[] w = Ease_("wipe_ease");
            var ez = new Forge.Core.CraftFx.CssEase(w[0], w[1], w[2], w[3]);
            Assert.AreEqual(0.7247, ez.Ease(0.3), 5e-4, "정본 곡선의 k=.3");
            Assert.Greater(ez.Ease(0.3) - 0.3, 0.42, "선형(생 비율)과의 어긋남 — 이 축에서 가장 컸다");
            Assert.AreEqual(0.0, ez.Ease(0.0), 1e-9); Assert.AreEqual(1.0, ez.Ease(1.0), 1e-9);
        }

        [Test]
        public void 천개_소환진_이징은_세제곱_근사보다_k_0_2_에서_0_18_앞선다()
        {
            double[] e = Ease_("canopy_in_ease");
            var ez = new Forge.Core.CraftFx.CssEase(e[0], e[1], e[2], e[3]);
            double approx = 1.0 - System.Math.Pow(1.0 - 0.2, 3.0);   // 종전 클론이 쓰던 손 근사
            Assert.AreEqual(0.6745, ez.Ease(0.2), 5e-4, "정본 곡선의 k=.2");
            Assert.AreEqual(0.488, approx, 5e-4, "손 근사의 같은 자리");
            Assert.Greater(ez.Ease(0.2) - approx, 0.18, "둘의 어긋남 — T33 77회차가 잰 +0.19");
            double prev = -1.0;
            for (int i = 0; i <= 20; i++) { double y = ez.Ease(i / 20.0); Assert.GreaterOrEqual(y, prev - 1e-12, "단조"); prev = y; }
        }
    }
}
