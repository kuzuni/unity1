using System;
using NUnit.Framework;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>
    /// T472 — 둥근 사각 점선 테의 셈(<see cref="DashedFrameRules"/>). 정본 `style.css` 1830 `.cmp-card.empty { border-style: dashed }` ·
    /// 1826 `.cmp-card { border: var(--ol3) solid var(--pp-line); border-radius: .8rem }`. 값은 표가 쥐고 여기는 기하만 잰다(유니티 없이).
    /// </summary>
    public class DashedFrameRulesTests
    {
        const double W = 200, H = 100, R = 20, T = 6;

        [Test]
        public void 둘레는_직선_넷과_원_하나의_합이고_반지름은_반_변에서_잘린다()
        {
            Assert.AreEqual(2 * (W - 2 * R) + 2 * (H - 2 * R) + 2 * Math.PI * R, DashedFrameRules.Perimeter(W, H, R), 1e-9);
            Assert.AreEqual(2 * W + 2 * H, DashedFrameRules.Perimeter(W, H, 0), 1e-9, "반지름 0 = 직사각 둘레");
            Assert.AreEqual(2 * (W - H) + Math.PI * H, DashedFrameRules.Perimeter(W, H, 999), 1e-9, "반지름이 반 변을 넘으면 반 변(알약) — 직선은 W−H 둘 + 원 하나");
            Assert.AreEqual(H * 0.5, DashedFrameRules.ClampRadius(W, H, 999), 1e-9);
        }

        [Test]
        public void 거리는_경계_0_안쪽_음수_바깥_양수이고_모서리는_원호를_따른다()
        {
            Assert.AreEqual(0, DashedFrameRules.Distance(W * 0.5, 0, W, H, R), 1e-9, "위 변 한가운데는 경계");
            Assert.AreEqual(-3, DashedFrameRules.Distance(W * 0.5, 3, W, H, R), 1e-9, "3 안쪽");
            Assert.AreEqual(2, DashedFrameRules.Distance(W * 0.5, -2, W, H, R), 1e-9, "2 바깥");
            double c = R - R / Math.Sqrt(2.0);                                              // 왼쪽 위 사분원의 45° 점
            Assert.AreEqual(0, DashedFrameRules.Distance(c, c, W, H, R), 1e-9, "모서리 원호 위는 경계");
            Assert.Greater(DashedFrameRules.Distance(0, 0, W, H, R), 0, "네모 꼭짓점은 둥근 테 밖");
        }

        [Test]
        public void 둘레_자리는_위_변_왼쪽에서_시계_방향으로_이어지고_모서리를_지나며_끊기지_않는다()
        {
            double sw = W - 2 * R, sh = H - 2 * R, q = Math.PI * R * 0.5;
            Assert.AreEqual(0, DashedFrameRules.ArcPos(R, 0, W, H, R), 1e-9, "시작점");
            Assert.AreEqual(50, DashedFrameRules.ArcPos(R + 50, 2, W, H, R), 1e-9, "위 변: 띠 안 깊이는 자리를 안 바꾼다");
            Assert.AreEqual(sw + q, DashedFrameRules.ArcPos(W, R, W, H, R), 1e-6, "오른쪽 위 모서리 끝 = 직선 + 사분원");
            Assert.AreEqual(sw + q + 10, DashedFrameRules.ArcPos(W - 1, R + 10, W, H, R), 1e-9, "오른 변 ↓");
            Assert.AreEqual(sw + q + sh + q + 10, DashedFrameRules.ArcPos(W - R - 10, H, W, H, R), 1e-9, "아래 변 ←");
            Assert.AreEqual(2 * sw + sh + 3 * q + 10, DashedFrameRules.ArcPos(0, H - R - 10, W, H, R), 1e-9, "왼 변 ↑");
            double end = DashedFrameRules.ArcPos(R - 1e-6, 0.5, W, H, R);                   // 왼쪽 위 사분원의 끝(각도 −π/2 직전)
            Assert.AreEqual(DashedFrameRules.Perimeter(W, H, R), end, 0.1, "왼쪽 위 모서리 끝은 둘레 전체(시작점 직전)");
            // 모서리 안에서 각도를 따라 단조 증가
            double prev = -1;
            for (int i = 0; i <= 20; i++)
            {
                double a = -Math.PI * 0.5 + (Math.PI * 0.5) * i / 20.0;                     // 오른쪽 위 사분원 −π/2 → 0
                double x = (W - R) + Math.Cos(a) * (R - 1), y = R + Math.Sin(a) * (R - 1);
                double s = DashedFrameRules.ArcPos(x, y, W, H, R);
                Assert.Greater(s, prev, "모서리 안 단조 증가 " + i);
                prev = s;
            }
        }

        [Test]
        public void 주기는_둘레에_정수_개가_들어가게_맞춰지고_대시_비율은_지킨다()
        {
            double per = DashedFrameRules.Perimeter(W, H, R);
            double dashFit;
            double period = DashedFrameRules.FitPeriod(per, 18, 18, out dashFit);
            double n = per / period;
            Assert.AreEqual(Math.Round(n), n, 1e-9, "정수 개");
            Assert.AreEqual(Math.Round(per / 36.0), n, 1e-9, "가장 가까운 정수");
            Assert.AreEqual(period * 0.5, dashFit, 1e-9, "대시:틈 = 1:1 그대로");
            Assert.AreEqual(36, DashedFrameRules.FitPeriod(0, 18, 18, out dashFit), 1e-9, "둘레 0 이면 그대로");
        }

        [Test]
        public void 잉크는_띠_안에서만_대시와_틈이_번갈아_들고_한가운데와_바깥은_비어_있다()
        {
            double dash = 3 * T, gap = 3 * T;
            Assert.IsFalse(DashedFrameRules.IsInk(W * 0.5, H * 0.5, W, H, R, T, dash, gap), "한가운데는 비어 있다(면 없음)");
            Assert.IsFalse(DashedFrameRules.IsInk(W * 0.5, -1, W, H, R, T, dash, gap), "바깥은 비어 있다");
            Assert.IsFalse(DashedFrameRules.IsInk(W * 0.5, T + 1, W, H, R, T, dash, gap), "띠 아래(안쪽)는 비어 있다");
            // 위 변 띠 가운데 줄을 따라 잉크가 끊겼다 이어졌다 한다
            int flips = 0; bool prev = DashedFrameRules.IsInk(R, T * 0.5, W, H, R, T, dash, gap);
            Assert.IsTrue(prev, "시작점은 대시");
            for (double x = R; x <= W - R; x += 0.5)
            {
                bool ink = DashedFrameRules.IsInk(x, T * 0.5, W, H, R, T, dash, gap);
                if (ink != prev) flips++;
                prev = ink;
            }
            Assert.GreaterOrEqual(flips, 4, "위 변 160px 에 주기 ≈36px 이면 적어도 두 번은 끊긴다");
            // 대시 한 토막의 길이 = 맞춘 주기의 절반
            double dashFit; double period = DashedFrameRules.FitPeriod(DashedFrameRules.Perimeter(W, H, R), dash, gap, out dashFit);
            double run = 0;
            for (double x = R; x <= W - R && DashedFrameRules.IsInk(x, T * 0.5, W, H, R, T, dash, gap); x += 0.25) run += 0.25;
            Assert.AreEqual(dashFit, run, 0.6, "첫 대시 길이");
            Assert.IsTrue(DashedFrameRules.IsInk(W * 0.5, T * 0.5, W, H, R, T, dash, 0), "틈 0 이면 실선");
        }
    }
}
