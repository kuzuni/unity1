using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>
    /// T459 — 소환 결과의 «도는 겹» 둘의 표(`SummonFxUi.json` `enter`·`hipulse`)가 정본 수 그대로이고 셈이 CSS 의 뜻과 같다.
    /// ⓧ `srshake`(5657·6148·5666): 열림 .21s 뒤 .25s · 16% 에 (.5%, −.9%) · 배율 1.012 · 양 끝 제자리.
    /// ⓨ `srpulse`(6674·6675): 1.6s 주기 · 지연 .45s + i×.17s · 광채 두 겹 1.1/2.3 → 1.7/3.4rem(50%).
    /// </summary>
    public class SummonEnterPulseSpecTests
    {
        static JsonObject Root()
        {
            string root = Directory.GetCurrentDirectory();
            while (!File.Exists(Path.Combine(root, "Assets", "Forge", "Resources", "SummonFxUi.json"))) root = Path.GetDirectoryName(root);
            return MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "Assets", "Forge", "Resources", "SummonFxUi.json")));
        }

        [Test]
        public void 입장_셰이크는_열림_210ms_뒤_250ms_이고_16퍼센트에_정본_치우침이다()
        {
            SummonEnterSpec s = SummonEnterSpec.From(Root());
            Assert.AreEqual(250.0, s.ShakeMs, 1e-9);
            Assert.AreEqual(210.0, s.DelayMs, 1e-9);
            double tx, ty, sc;
            s.At(0, out tx, out ty, out sc);
            Assert.AreEqual(0.0, tx, 1e-9); Assert.AreEqual(0.0, ty, 1e-9); Assert.AreEqual(1.0, sc, 1e-9);
            Assert.IsFalse(s.Shaking(100), "지연 안은 제자리");
            s.At(210 + 250 * 0.16, out tx, out ty, out sc);
            Assert.AreEqual(0.005, tx, 1e-6, "16% 키 translate3d(.5%, …)");
            Assert.AreEqual(-0.009, ty, 1e-6, "16% 키 …, −.9% (CSS 부호 · 음수 = 위)");
            Assert.AreEqual(1.012, sc, 1e-6, "16% 키 scale(1.012)");
            Assert.IsTrue(s.Shaking(300) && !s.Done(300));
            Assert.IsTrue(s.Done(460), "210 + 250 = 460ms 에 끝난다");
            s.At(460, out tx, out ty, out sc);
            Assert.AreEqual(0.0, tx, 1e-9); Assert.AreEqual(0.0, ty, 1e-9); Assert.AreEqual(1.0, sc, 1e-9);
        }

        [Test]
        public void 광채_맥동은_1600ms_주기에_셀마다_450_더하기_170i_지연이고_정점은_두_겹_비율의_평균이다()
        {
            SummonHiPulseSpec s = SummonHiPulseSpec.From(Root());
            Assert.AreEqual(1600.0, s.PeriodMs, 1e-9);
            Assert.AreEqual(450.0, s.DelayMs(0), 1e-9);
            Assert.AreEqual(450.0 + 170.0 * 3, s.DelayMs(3), 1e-9);
            double peak = ((1.7 / 1.1) + (3.4 / 2.3)) * 0.5;
            Assert.AreEqual(peak, s.PeakF, 1e-9, "정본 1.1→1.7 · 2.3→3.4 의 비율 평균");
            Assert.AreEqual(1.0, s.ScaleAt(100, 0), 1e-9, "지연 안은 기본");
            Assert.AreEqual(1.0, s.ScaleAt(450, 0), 1e-9, "0% = 기본");
            Assert.AreEqual(peak, s.ScaleAt(450 + 800, 0), 1e-6, "50% = 정점");
            Assert.AreEqual(1.0, s.ScaleAt(450 + 1600, 0), 1e-6, "100% = 기본 · 무한 반복");
            Assert.AreEqual(peak, s.ScaleAt(450 + 1600 + 800, 0), 1e-6, "둘째 주기의 정점");
            double q = s.ScaleAt(450 + 400, 0);
            Assert.Greater(q, 1.0); Assert.Less(q, peak);
            Assert.AreEqual(s.ScaleAt(450 + 400, 0), s.ScaleAt(450 + 1200, 0), 1e-6, "ease-in-out 은 좌우 대칭");
            Assert.AreEqual(1.0, s.ScaleAt(450 + 170 * 2 + 0, 2), 1e-9, "셀 2 는 제 지연 뒤에 0% 다");
        }

        [Test]
        public void 표가_틀리면_거부한다()
        {
            JsonObject r = Root();
            JsonObject e = J.Obj(r["enter"]);
            double keep = J.Num(e["shake_delay_ms"]);
            e["shake_delay_ms"] = -1.0;
            Assert.Throws<System.FormatException>(() => SummonEnterSpec.From(r));
            e["shake_delay_ms"] = keep;
            JsonObject h = J.Obj(r["hipulse"]);
            object keep2 = h["inner_rem"];
            h["inner_rem"] = new System.Collections.Generic.List<object> { 1.7, 1.1 };
            Assert.Throws<System.FormatException>(() => SummonHiPulseSpec.From(r));
            h["inner_rem"] = keep2;
            Assert.DoesNotThrow(() => SummonHiPulseSpec.From(r));
        }
    }
}
