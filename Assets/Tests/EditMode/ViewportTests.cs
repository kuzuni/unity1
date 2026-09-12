using NUnit.Framework;
using Forge.Core;

namespace Forge.Tests
{
    /// <summary>T1 — 9:16 레터박스 계산(Core.Viewport). 순수 C# 이라 dotnet 하니스와 유니티 EditMode 둘 다에서 돈다.</summary>
    public class ViewportTests
    {
        const float A = 9f / 16f;

        [Test]
        public void 정확히_9대16이면_전체화면()
        {
            var r = Viewport.Letterbox(1080, 1920, A);
            Assert.AreEqual(0f, r.X, 1e-6f); Assert.AreEqual(0f, r.Y, 1e-6f);
            Assert.AreEqual(1f, r.W, 1e-6f); Assert.AreEqual(1f, r.H, 1e-6f);
        }

        [Test]
        public void 가로화면이면_좌우를_비운다()
        {
            var r = Viewport.Letterbox(1920, 1080, A);
            Assert.AreEqual(1f, r.H, 1e-6f); Assert.AreEqual(0f, r.Y, 1e-6f);
            Assert.AreEqual(A / (1920f / 1080f), r.W, 1e-5f);
            Assert.AreEqual((1f - r.W) * 0.5f, r.X, 1e-6f);
            Assert.AreEqual(A, (r.W * 1920f) / (r.H * 1080f), 1e-4f);
        }

        [Test]
        public void 더_긴_폰이면_위아래를_비운다()
        {
            var r = Viewport.Letterbox(390, 844, A);
            Assert.AreEqual(1f, r.W, 1e-6f); Assert.AreEqual(0f, r.X, 1e-6f);
            Assert.AreEqual((390f / 844f) / A, r.H, 1e-5f);
            Assert.AreEqual((1f - r.H) * 0.5f, r.Y, 1e-6f);
            Assert.AreEqual(A, (r.W * 390f) / (r.H * 844f), 1e-4f);
        }

        [Test]
        public void 잘못된_입력은_전체화면으로_되돌린다()
        {
            var r = Viewport.Letterbox(0, 0, A);
            Assert.AreEqual(1f, r.W, 1e-6f); Assert.AreEqual(1f, r.H, 1e-6f);
            r = Viewport.Letterbox(100, 100, 0f);
            Assert.AreEqual(1f, r.W, 1e-6f); Assert.AreEqual(1f, r.H, 1e-6f);
        }
    }
}
