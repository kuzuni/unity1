using NUnit.Framework;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>
    /// T104 — 정본 `-webkit-text-stroke` → TMP SDF 재질 값 환산(<see cref="OutlineSdf"/>). 값은 TMP `CreateFontAsset(Font)` 기본 애셋
    /// (패딩 9 · 90pt → `_GradientScale` 10 · `_ScaleRatioA` .9 = 배포된 LiberationSans SDF.asset 의 값)과 등재 실측(17.66 CSS px 글자 ·
    /// 정본 2px 키라인 · 촬영 배율 2.164)이다. 실제 화면 두께는 PlayMode `OutlineTests` 가 픽셀로 잰다.
    /// (T121: 게임 글꼴 자체는 `UiFontBake.json` 의 54pt · 패딩 9 로 굽는다 — 여기 상수는 식의 단위 검산용 TMP 기본값이다 · 실제 애셋 값은 PlayMode `FontBakeTests` 가 본다.)
    /// </summary>
    public class OutlineSdfTests
    {
        const double G = 10, R = 0.9, Pt = 90;

        [Test]
        public void 단위_검산_W_1_이면_바깥_띠가_패딩_px_다()
        {
            // TMP 문서 «효과는 패딩(9/90 = 글자의 10%)까지» — W = D = 1 의 바깥 띠가 정확히 그 값이어야 식이 맞다
            double fs = 100;
            Assert.AreEqual(10.0, OutlineSdf.UnitPx(fs, G, R, Pt), 1e-9);
            OutlineSdf o = OutlineSdf.FromStroke(20, fs, G, R, Pt);   // 바깥 10 = 패딩 전부
            Assert.AreEqual(1.0, o.Width01, 1e-9);
            Assert.AreEqual(1.0, o.Dilate, 1e-9);
            Assert.IsFalse(o.Clipped);
            Assert.AreEqual(10.0, o.VisiblePx, 1e-9);
        }

        [Test]
        public void 정본_2px_키라인은_17_66px_글자에서_여백_안이고_보이는_띠는_획의_절반이다()
        {
            double fs = 17.66 * 2.164, stroke = 2 * 2.164;   // 기준 캔버스 px(T87 결정 222 규약)
            OutlineSdf o = OutlineSdf.FromStroke(stroke, fs, G, R, Pt);
            Assert.IsFalse(o.Clipped, "2px 키라인은 패딩 10% 안이다(획 11.3% 의 절반)");
            Assert.AreEqual(o.Width01, o.Dilate, 1e-12, "채움을 지키려면 가장자리 이동 = 띠 반폭");
            Assert.AreEqual(stroke * 0.5, o.VisiblePx, 1e-9, "paint-order: stroke fill = 바깥 N/2 만 보인다");
            Assert.AreEqual(stroke * 0.5, o.WantedPx, 1e-12);
            Assert.AreEqual(0.5665, o.Width01, 1e-3);
        }

        [Test]
        public void 옛_갈래_0_25_는_같은_글자에서_1px_미만이다_등재_진단()
        {
            double fs = 17.66 * 2.164;
            double seen = OutlineSdf.LegacyVisiblePx(0.25, fs, G, R, Pt);
            Assert.Less(seen, 1.0, "런 191 이름 줄 어두운 픽셀 0 의 이유");
            Assert.AreEqual(0.4778, seen, 1e-3);
            // px 갈래는 같은 글자에서 4.5배 두껍다
            Assert.Greater(OutlineSdf.FromStroke(2 * 2.164, fs, G, R, Pt).VisiblePx / seen, 4.0);
        }

        [Test]
        public void 여백을_넘는_획은_1_로_잘리고_표식이_선다()
        {
            OutlineSdf o = OutlineSdf.FromStroke(8, 20, G, R, Pt);   // 원한 바깥 4 · 패딩 2
            Assert.IsTrue(o.Clipped);
            Assert.AreEqual(1.0, o.Width01, 1e-12);
            Assert.AreEqual(1.0, o.Dilate, 1e-12);
            Assert.AreEqual(2.0, o.VisiblePx, 1e-9);
            Assert.AreEqual(4.0, o.WantedPx, 1e-12);
        }

        [Test]
        public void 빈_값은_0_이다()
        {
            OutlineSdf o = OutlineSdf.FromStroke(0, 30, G, R, Pt);
            Assert.AreEqual(0, o.Width01); Assert.AreEqual(0, o.Dilate); Assert.AreEqual(0, o.VisiblePx); Assert.IsFalse(o.Clipped);
            Assert.AreEqual(0, OutlineSdf.FromStroke(4, 30, 0, R, Pt).Width01, "G 0");
            Assert.AreEqual(0, OutlineSdf.FromStroke(4, 30, G, 0, Pt).Width01, "R 0");
            Assert.AreEqual(0, OutlineSdf.FromStroke(4, 0, G, R, Pt).Width01, "글자 0");
            Assert.AreEqual(0, OutlineSdf.LegacyVisiblePx(0, 30, G, R, Pt));
            Assert.AreEqual(0, OutlineSdf.AlphaPx(30, G, 0));
        }
    }
}
