using NUnit.Framework;
using Forge.Core.Ui;

namespace Forge.Tests.EditMode
{
    /// <summary>T333 — 정본 `text-shadow` 한 겹 → TMP 언더레이 환산(<see cref="UnderlaySdf"/>). 단위는 T121 실측 램프 꼴(54pt · G 19.6 · R_C 1).</summary>
    public class UnderlaySdfTests
    {
        const double G = 19.6, Rc = 1.0, Pt = 54;

        [Test]
        public void 단위_px_는_Rc_G_글자_샘플링이다()
        {
            // 36px 글자: 1 알파 = 19.6 텍셀 · 1 텍셀 = 36/54 px → 13.07px
            Assert.AreEqual(19.6 * 36 / 54, UnderlaySdf.Unit(36, G, Rc, Pt), 1e-9);
            Assert.AreEqual(0, UnderlaySdf.Unit(0, G, Rc, Pt)); Assert.AreEqual(0, UnderlaySdf.Unit(36, 0, Rc, Pt));
            Assert.AreEqual(0, UnderlaySdf.Unit(36, G, 0, Pt)); Assert.AreEqual(0, UnderlaySdf.Unit(36, G, Rc, 0));
        }

        [Test]
        public void 정본_0_1px_1px_는_아래로_한_칸_흐림_한_칸이고_y_부호가_뒤집힌다()
        {
            double fs = 44, dy = 2.164, blur = 2.164;   // CSS 1px × css_px 2.164
            UnderlaySdf u = UnderlaySdf.FromPx(0, dy, blur, fs, G, Rc, Pt);
            double unit = UnderlaySdf.Unit(fs, G, Rc, Pt);
            Assert.AreEqual(0, u.OffsetX01, 1e-12);
            Assert.AreEqual(-dy / unit, u.OffsetY01, 1e-12, "CSS 아래(+) = TMP 위(+)의 반대");
            Assert.Less(u.OffsetY01, 0);
            Assert.AreEqual(blur / unit, u.Softness01, 1e-12);
            Assert.IsFalse(u.Clipped);
            double bx, by, bb;
            UnderlaySdf.ToPx(u.OffsetX01, u.OffsetY01, u.Softness01, u.UnitPx, out bx, out by, out bb);
            Assert.AreEqual(0, bx, 1e-9); Assert.AreEqual(dy, by, 1e-9); Assert.AreEqual(blur, bb, 1e-9);
        }

        [Test]
        public void 흐림_0_은_softness_0_이고_단위를_넘으면_잘린다()
        {
            UnderlaySdf u = UnderlaySdf.FromPx(0, 1, 0, 40, G, Rc, Pt);
            Assert.AreEqual(0, u.Softness01); Assert.IsFalse(u.Clipped);
            UnderlaySdf c = UnderlaySdf.FromPx(200, -200, 200, 20, G, Rc, Pt);   // 단위 7.26px 를 훌쩍 넘는다
            Assert.IsTrue(c.Clipped);
            Assert.AreEqual(1, c.OffsetX01); Assert.AreEqual(1, c.OffsetY01); Assert.AreEqual(1, c.Softness01);
        }

        [Test]
        public void 값이_비면_0_이다()
        {
            UnderlaySdf u = UnderlaySdf.FromPx(1, 1, 1, 30, 0, Rc, Pt);
            Assert.AreEqual(0, u.UnitPx); Assert.AreEqual(0, u.OffsetX01); Assert.AreEqual(0, u.OffsetY01); Assert.AreEqual(0, u.Softness01); Assert.IsFalse(u.Clipped);
        }
    }
}
