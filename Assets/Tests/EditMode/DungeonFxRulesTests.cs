using System;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>
    /// T335 ⓐⓒ — 던전 클리어 연출 셋과 «연구 완료» 맥동의 셈이 정본 `style.css` 5390~5398 · 4622~4626 과 같은가:
    /// 칸 팝 .38s(칸마다 .09s 늦게 · 시작 전엔 from) · 가라앉기 .45s ease-in .12s(끝값에 머문다) · 딤 .55s · 맥동 1.1s alternate.
    /// </summary>
    public class DungeonFxRulesTests
    {
        static string File_()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            return Path.Combine(root, "Assets", "Forge", "Resources", "DungeonFxUi.json");
        }
        static DungeonFxSpec S() { return DungeonFxSpec.From(MiniJson.ParseObject(File.ReadAllText(File_()))); }

        [Test]
        public void 표값이_정본_그대로다()
        {
            var s = S();
            Assert.AreEqual(380, s.PopMs, 1e-9, "css 5389 dgc-pop .38s");
            Assert.AreEqual(90, s.PopStaggerMs, 1e-9, "css 5390 nth-child(2) .09s");
            Assert.AreEqual(120, s.SinkDelayMs, 1e-9, "css 5396 dgclear-sink 지연 .12s");
            Assert.AreEqual(450, s.SinkMs, 1e-9, "css 5396 .45s");
            Assert.AreEqual(550, s.DimOutMs, 1e-9, "css 5398 transition .55s");
            Assert.AreEqual(1100, s.ReadyMs, 1e-9, "css 4622 tt-ready 1.1s");
            Assert.AreEqual("tech_node_border", s.ReadyFromKey, "from border-color #7d3920");
            Assert.AreEqual("pp_green", s.ReadyToKey, "to var(--pp-green)");
        }

        [Test]
        public void 칸_팝은_backwards_라_시작_전에도_from_이고_칸마다_늦게_뛴다()
        {
            var s = S();
            Assert.AreEqual(0.3, s.PopScaleAt(0, 0), 1e-9, "from scale(.3)");
            Assert.AreEqual(0.0, s.PopAlphaAt(0, 0), 1e-9, "from opacity 0");
            Assert.AreEqual(0.3, s.PopScaleAt(80, 1), 1e-9, "둘째 칸은 .09s 전엔 아직 from(backwards)");
            Assert.AreEqual(0.3, s.PopScaleAt(170, 2), 1e-9, "셋째 칸은 .18s 전엔 아직 from");
            Assert.AreEqual(1.0, s.PopScaleAt(380, 0), 1e-9, "to transform: none");
            Assert.AreEqual(1.0, s.PopAlphaAt(380, 0), 1e-9, "to opacity 1");
            Assert.Greater(s.PopScaleAt(380 + 90, 1), 0.999, "둘째 칸은 .09s 뒤에 끝난다");
            Assert.IsFalse(s.PopDone(379 + 180, 3)); Assert.IsTrue(s.PopDone(380 + 180, 3), "셋이면 .18s + .38s");
            Assert.IsTrue(s.PopDone(380, 1), "하나면 .38s");
            // cubic-bezier(.34,1.56,.64,1) 은 1 을 넘었다가 돌아온다(통통) — 중간 어딘가에서 scale > 1
            double peak = 0;
            for (int ms = 0; ms <= 380; ms += 5) peak = Math.Max(peak, s.PopScaleAt(ms, 0));
            Assert.Greater(peak, 1.0, "오버슈트가 있어야 «통통 튄다»");
        }

        [Test]
        public void 가라앉기는_지연_동안_원래_모습이고_끝값에_머물며_딤은_같이_걷힌다()
        {
            var s = S();
            Assert.AreEqual(1.0, s.SinkScaleAt(0), 1e-9, "지연 .12s 동안 원래 모습");
            Assert.AreEqual(1.0, s.SinkAlphaAt(119), 1e-9);
            Assert.AreEqual(0.9, s.SinkScaleAt(120 + 450), 1e-9, "to scale(.9)");
            Assert.AreEqual(0.0, s.SinkAlphaAt(120 + 450), 1e-9, "to opacity 0");
            Assert.AreEqual(0.9, s.SinkScaleAt(5000), 1e-9, "forwards — 끝값에 머문다");
            double mid = s.SinkAlphaAt(120 + 225);
            Assert.Greater(mid, 0.5, "ease-in 이라 앞이 느리다(중간에 아직 반 넘게 남는다)");
            Assert.AreEqual(1.0, s.DimFactorAt(0), 1e-9);
            Assert.AreEqual(0.0, s.DimFactorAt(550), 1e-9, "딤 .55s 뒤 0");
            Assert.IsFalse(s.LeaveDone(569)); Assert.IsTrue(s.LeaveDone(570), "카드(.12+.45)와 딤(.55) 둘 다 끝나야 뿌리를 걷는다");
        }

        [Test]
        public void 맥동은_반_주기마다_왕복하고_0과_1_사이를_벗어나지_않는다()
        {
            var s = S();
            Assert.AreEqual(0.0, s.ReadyT(0), 1e-9, "from 색에서 시작");
            Assert.AreEqual(1.0, s.ReadyT(1100), 1e-9, "1.1s 에 to 색");
            Assert.AreEqual(0.0, s.ReadyT(2200), 1e-9, "alternate — 2.2s 에 다시 from");
            Assert.AreEqual(s.ReadyT(300), s.ReadyT(2200 + 300), 1e-9, "infinite — 주기가 같다");
            Assert.AreEqual(s.ReadyT(300), s.ReadyT(2200 - 300), 1e-9, "alternate — 거울");
            for (int ms = 0; ms <= 4400; ms += 25) { double t = s.ReadyT(ms); Assert.GreaterOrEqual(t, 0); Assert.LessOrEqual(t, 1); }
            Assert.Less(s.ReadyT(110), 0.1, "ease-in-out 이라 앞이 느리다");
        }
    }
}
