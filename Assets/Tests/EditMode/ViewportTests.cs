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

        // T54 — 3D 캔버스 상자(`#game-area`): 원작 `topbar_h`·`sheet_top`(카탈로그 판독표)
        const float Top = 0.065f, Bottom = 0.5516f;

        [Test]
        public void 게임영역은_상단바_밑에서_시트_위까지의_띠다()
        {
            var app = Viewport.Letterbox(540, 960, A);              // 정확히 9:16 — 앱 상자가 화면 전체
            var g = Viewport.GameArea(app, Top, Bottom);
            Assert.AreEqual(app.X, g.X, 1e-6f);
            Assert.AreEqual(app.W, g.W, 1e-6f);
            Assert.AreEqual(Bottom - Top, g.H, 1e-6f);              // 띠 높이 = 시트 위 − 상단바 밑
            Assert.AreEqual(1f - Bottom, g.Y, 1e-6f);               // 뷰포트 y 는 아래에서 위로
            // 띠 한가운데(= 카메라가 비추는 자리)가 화면 위에서 30.8% — 원작 shot-042120 의 전투선과 같다.
            float centerFromTop = 1f - (g.Y + g.H * 0.5f);
            Assert.AreEqual(0.3083f, centerFromTop, 5e-4f);
        }

        [Test]
        public void 게임영역은_레터박스_앱_상자_안에서만_자른다()
        {
            var app = Viewport.Letterbox(1200, 800, A);             // 가로가 넓다 → 좌우 필러박스
            var g = Viewport.GameArea(app, Top, Bottom);
            Assert.AreEqual(app.X, g.X, 1e-6f);
            Assert.AreEqual(app.W, g.W, 1e-6f);
            Assert.IsTrue(g.Y >= app.Y - 1e-6f && g.Y + g.H <= app.Y + app.H + 1e-6f, "띠는 앱 상자 밖으로 안 나간다");
            Assert.AreEqual(app.H * (Bottom - Top), g.H, 1e-6f);
        }

        [Test]
        public void 표가_비었거나_뒤집혔으면_앱_상자_그대로다()
        {
            var app = Viewport.Letterbox(540, 960, A);
            foreach (var bad in new[] { new[] { 0f, 0f }, new[] { 0.6f, 0.2f }, new[] { -0.1f, 0.5f }, new[] { 0.1f, 1.5f } })
            {
                var g = Viewport.GameArea(app, bad[0], bad[1]);
                Assert.AreEqual(app.Y, g.Y, 1e-6f); Assert.AreEqual(app.H, g.H, 1e-6f);
            }
        }
    }
}
