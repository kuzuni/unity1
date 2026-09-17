using System.IO;
using NUnit.Framework;
using Forge.Core.CraftFx;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>T454 — 정본 `transition` 표(`Resources/TransitionUi.json`)의 왕복과 셈(진행도 · 남은 dy · 끝).</summary>
    public class TransitionRulesTests
    {
        static TransitionTable T()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            string file = Path.Combine(root, "Assets", "Forge", "Resources", "TransitionUi.json");
            return TransitionTable.From(MiniJson.ParseObject(File.ReadAllText(file)));
        }

        [Test]
        public void 표는_정본_세_자리를_그_길이와_타이밍으로_쥔다()
        {
            var t = T();
            // style.css 1936 .toast { transition: all .25s } · translateY(-.5rem)
            var toast = t.Get("toast_in");
            Assert.AreEqual(250, toast.Ms); Assert.AreEqual(-0.5, toast.DyRem, 1e-9, "위 .5rem 에서 내려온다(아래가 +)");
            Assert.AreEqual(new CssEase(0.25, 0.1, 0.25, 1).Ease(0.5), toast.Ease.Ease(0.5), 1e-9, "timing 을 안 적은 자리 = CSS 기본 ease");
            // 3113 .settings-toggle::after { transition: left .15s }
            var tg = t.Get("toggle");
            Assert.AreEqual(150, tg.Ms); Assert.AreEqual(0, tg.DyRem, 1e-9);
            // 4992 .af-toggle .knob { transition: left .15s ease-out }
            var af = t.Get("af-toggle");
            Assert.AreEqual(150, af.Ms);
            Assert.AreEqual(new CssEase(0, 0, 0.58, 1).Ease(0.5), af.Ease.Ease(0.5), 1e-9, "ease-out");
            Assert.AreNotEqual(af.Ease.Ease(0.5), tg.Ease.Ease(0.5), "두 토글의 타이밍은 다른 곡선이다(3113 기본 ease · 4992 ease-out — CSS 정의값 ease 가 절반 시각에 더 앞선다 .80 ↔ .68)");
        }

        [Test]
        public void 진행도는_0에서_1로_단조롭고_길이를_지나면_끝이다()
        {
            var t = T();
            var s = t.Get("toast_in");
            Assert.AreEqual(0, TransitionRules.Progress(s, 0), 1e-9);
            Assert.AreEqual(0, TransitionRules.Progress(s, -5), 1e-9, "음수 시간은 시작");
            double prev = 0;
            for (int ms = 10; ms <= 250; ms += 10)
            {
                double p = TransitionRules.Progress(s, ms);
                Assert.GreaterOrEqual(p, prev - 1e-9, "단조 증가 @" + ms);
                prev = p;
            }
            Assert.AreEqual(1, TransitionRules.Progress(s, 250), 1e-9);
            Assert.AreEqual(1, TransitionRules.Progress(s, 9999), 1e-9);
            Assert.IsFalse(TransitionRules.Done(s, 249)); Assert.IsTrue(TransitionRules.Done(s, 250));
            // 남은 dy: 시작 −.5rem · 끝 0 · 중간은 그 사이
            Assert.AreEqual(-0.5, TransitionRules.DyRem(s, 0), 1e-9);
            Assert.AreEqual(0, TransitionRules.DyRem(s, 1), 1e-9);
            double mid = TransitionRules.DyRem(s, TransitionRules.Progress(s, 125));
            Assert.Greater(mid, -0.5); Assert.Less(mid, 0);
            // 손잡이 자리: 앞 닻 0 → 새 닻 1
            Assert.AreEqual(0, TransitionRules.Lerp(0, 1, 0), 1e-9); Assert.AreEqual(1, TransitionRules.Lerp(0, 1, 1), 1e-9);
            Assert.AreEqual(1, TransitionRules.Lerp(0, 1, 7), 1e-9, "진행도는 1 을 안 넘는다");
        }
    }
}
