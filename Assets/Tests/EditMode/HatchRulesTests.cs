using System;
using NUnit.Framework;
using Forge.Core.Ui;

namespace Forge.Tests.EditMode
{
    /// <summary>
    /// T178 15회차 — 교차 해칭(정본 828·1063·1131 `repeating-linear-gradient(±45deg, rgba(0,0,0,.13) 0 2px, transparent 2px 12px)`)의 셈(<see cref="StripeRules.HatchLayers"/>·<see cref="StripeRules.HatchTile"/>).
    /// 재는 것: ⓐ 겹 수는 0·1·2 이고 원점은 두 겹이 다 덮는다 ⓑ 덮이는 넓이 = 1 − (1 − d/p)² ⓒ 한 타일(p÷|sin|·p÷|cos|)을 상하좌우로 이어도 이음새가 없다.
    /// </summary>
    public class HatchRulesTests
    {
        const double P = 12, D = 2, A = 45;

        [Test]
        public void 원점은_두_겹이_다_덮고_겹_수는_0에서_2다()
        {
            Assert.AreEqual(2, StripeRules.HatchLayers(0, 0, A, P, D), "원점은 두 축 위 자리가 다 0 이라 둘 다 대시 안");
            Assert.AreEqual(1, StripeRules.HatchLayers(0.5, 0.5, A, P, D), "(.5,.5) 는 +45 축 자리 0 · −45 축 자리 −.71(주기 끝) → 한 겹");
            int min = 9, max = -1;
            for (int y = 0; y < 60; y++) for (int x = 0; x < 60; x++) { int n = StripeRules.HatchLayers(x + 0.5, y + 0.5, A, P, D); if (n < min) min = n; if (n > max) max = n; }
            Assert.AreEqual(0, min, "빈 자리가 있다"); Assert.AreEqual(2, max, "격자점이 있다");
        }

        [Test]
        public void 덮이는_넓이는_한_겹_비율의_여집합_제곱을_뺀_것이다()
        {
            // 한 겹이 d/p 를 덮고 둘이 독립이라 1 − (1 − d/p)² · 촘촘히 재면 그 수에 붙는다
            double w, h; StripeRules.HatchTile(A, P, P * 64, out w, out h);
            int n = 0, tot = 0, step = 8;
            for (int y = 0; y < (int)(h * step); y++) for (int x = 0; x < (int)(w * step); x++) { tot++; if (StripeRules.HatchLayers((x + 0.5) / step, (y + 0.5) / step, A, P, D) > 0) n++; }
            double want = 1 - Math.Pow(1 - D / P, 2);
            Assert.AreEqual(want, (double)n / tot, 0.02, "덮이는 비율(정본 2/12 두 겹 ≈ 30.6%)");
        }

        [Test]
        public void 타일은_p_나누기_sin_과_cos_이고_이어_붙여도_이음새가_없다()
        {
            double w, h; StripeRules.HatchTile(A, P, P * 64, out w, out h);
            Assert.AreEqual(P * Math.Sqrt(2), w, 1e-9, "45° 가로 주기 = p√2(정본 .bw-hazard 의 1.556rem 꼴)");
            Assert.AreEqual(P * Math.Sqrt(2), h, 1e-9, "45° 세로 주기도 p√2");
            for (int i = 0; i < 200; i++)
            {
                double x = (i * 7.31) % w, y = (i * 3.17) % h;
                int n = StripeRules.HatchLayers(x, y, A, P, D);
                Assert.AreEqual(n, StripeRules.HatchLayers(x + w, y, A, P, D), "가로로 한 타일 옮겨도 같다");
                Assert.AreEqual(n, StripeRules.HatchLayers(x, y + h, A, P, D), "세로로 한 타일 옮겨도 같다");
            }
            StripeRules.HatchTile(90, P, P * 64, out w, out h);
            Assert.AreEqual(P, w, 1e-9, "가로 줄무늬는 가로 주기 = p"); Assert.AreEqual(P * 64, h, 1e-9, "세로 주기는 무한 → cap");
        }
    }
}
