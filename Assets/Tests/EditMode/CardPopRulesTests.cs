using System;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>
    /// T135 ⓑ — 모달 열림 카드 팝의 셈이 정본 `style.css` 1758~1759 와 같은가: `.25s ease-out` · scale .7→1 · α 0→1 · 한 번만 · 재호출 창 300ms.
    /// </summary>
    public class CardPopRulesTests
    {
        static string File_()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            return Path.Combine(root, "Assets", "Forge", "Resources", "CardPopUi.json");
        }
        static CardPopSpec S() { return CardPopSpec.From(MiniJson.ParseObject(File.ReadAllText(File_()))); }

        [Test]
        public void 길이와_창과_양끝_값이_정본_그대로다()
        {
            var s = S();
            Assert.AreEqual(250, s.DurationMs, 1e-9, "css 1758 `cardpop .25s`");
            Assert.AreEqual(300, s.HoldMs, 1e-9, "ui.js 1247 `remove('opening')` 300ms");
            Assert.AreEqual(0.7, s.ScaleAt(0), 1e-9, "from scale(.7)");
            Assert.AreEqual(0.0, s.AlphaAt(0), 1e-9, "from opacity 0");
            Assert.AreEqual(1.0, s.ScaleAt(250), 1e-9, "to transform: none");
            Assert.AreEqual(1.0, s.AlphaAt(250), 1e-9, "to opacity 1");
            Assert.AreEqual(1.0, s.ScaleAt(10000), 1e-9, "한 번만 돈다 — 끝난 뒤는 원래 모습");
            Assert.IsFalse(s.Done(249)); Assert.IsTrue(s.Done(250));
            Assert.IsTrue(s.Opening(299)); Assert.IsFalse(s.Opening(300));
        }

        [Test]
        public void ease_out_이라_앞이_빠르고_단조롭게_커진다()
        {
            var s = S();
            double mid = s.ScaleAt(125);
            Assert.Greater(mid, 0.7 + (1.0 - 0.7) * 0.5, "ease-out 은 절반 시각에 절반보다 더 가 있다(cubic-bezier(0,0,.58,1))");
            Assert.Less(mid, 1.0);
            double prevS = -1, prevA = -1;
            for (int ms = 0; ms <= 250; ms += 5)
            {
                double sc = s.ScaleAt(ms), a = s.AlphaAt(ms);
                Assert.GreaterOrEqual(sc, prevS, "scale 단조 " + ms); Assert.GreaterOrEqual(a, prevA, "alpha 단조 " + ms);
                Assert.AreEqual((sc - 0.7) / 0.3, a, 1e-9, "scale 과 alpha 는 같은 진행도를 탄다(같은 구간 · 같은 이징) " + ms);
                prevS = sc; prevA = a;
            }
        }

        [Test]
        public void 고장_주입_창이_길이보다_짧거나_키프레임이_하나면_막힌다()
        {
            string txt = File.ReadAllText(File_());
            Assert.Throws<FormatException>(() => CardPopSpec.From(MiniJson.ParseObject(txt.Replace("\"opening_hold_ms\": 300", "\"opening_hold_ms\": 100"))), "창 < 길이");
            string one = txt.Replace("{ \"at\": 0,   \"scale\": 0.7, \"alpha\": 0, \"ease\": \"ease-out\" },\n    { \"at\": 100, \"scale\": 1.0, \"alpha\": 1 }", "{ \"at\": 0, \"scale\": 0.7, \"alpha\": 0 }");
            Assert.AreNotEqual(txt, one, "고장 주입이 실제로 표를 바꿨다");
            Assert.Throws<FormatException>(() => CardPopSpec.From(MiniJson.ParseObject(one)), "키프레임 하나");
        }
    }
}
