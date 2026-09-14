using NUnit.Framework;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>
    /// T98 1회차 — 정본 SVG `d` 를 펴는 자(<see cref="SvgPath"/>)와, 표에 옮겨 적은 모루 좌표가
    /// **정본 주석의 매개변수 식과 맞는가**. 정본(`ui.js` 2387)은 상판 위 구멍들을 «감으로 찍지 말고
    /// 상판 면을 매개변수로 풀어서» 뽑았다고 못 박아 뒀으니, 옮겨 적은 수가 그 식을 벗어나면 오타다.
    /// </summary>
    public class SvgPathTests
    {
        private const double Eps = 1e-9;

        /// <summary>정본 상판 윗면 — `M23 4 L90 3 L95 25 L12 26 Z`.</summary>
        private const string Top = "M23 4 L90 3 L95 25 L12 26 Z";

        /// <summary>정본 뿔 — `M90 3 Q112 6 121 16 Q112 23 95 25 Z`.</summary>
        private const string Horn = "M90 3 Q112 6 121 16 Q112 23 95 25 Z";

        [Test]
        public void 직선만_있는_면은_꼭짓점_수가_그대로다()
        {
            double[][] p = SvgPath.Flatten(Top);
            Assert.AreEqual(4, p.Length, "Z 는 «첫 점으로 닫는다» 라 점을 더하지 않는다");
            Assert.AreEqual(23, p[0][0], Eps); Assert.AreEqual(4, p[0][1], Eps);
            Assert.AreEqual(12, p[3][0], Eps); Assert.AreEqual(26, p[3][1], Eps);
        }

        [Test]
        public void 곡선은_조각_수만큼_펴지고_끝점에_정확히_닿는다()
        {
            double[][] p = SvgPath.Flatten(Horn, 8);
            Assert.AreEqual(1 + 8 + 8, p.Length, "시작점 + Q 둘 × 8조각");
            Assert.AreEqual(90, p[0][0], Eps); Assert.AreEqual(3, p[0][1], Eps);
            Assert.AreEqual(121, p[8][0], 1e-9, "첫 Q 의 끝점");
            Assert.AreEqual(16, p[8][1], 1e-9);
            Assert.AreEqual(95, p[16][0], 1e-9, "둘째 Q 의 끝점");
            Assert.AreEqual(25, p[16][1], 1e-9);
            // 2차 베지에는 제어점 쪽으로 **부풀지만 넘지 않는다** — 뿔이 둥근 총알로 읽히는 까닭이다.
            for (int i = 1; i < 8; i++) Assert.Less(p[i][0], 121.0000001, "곡선은 끝점 x 를 넘지 않는다");
            Assert.Greater(p[4][1], 3, "가운데는 시작점보다 아래로 휜다");
        }

        [Test]
        public void 조각_수를_늘리면_점이_늘고_상자는_그대로다()
        {
            double[] b8 = SvgPath.Bounds(SvgPath.Flatten(Horn, 8));
            double[] b32 = SvgPath.Bounds(SvgPath.Flatten(Horn, 32));
            Assert.AreEqual(90, b8[0], Eps, "왼쪽 끝은 시작점");
            Assert.AreEqual(121, b8[2], Eps, "오른쪽 끝은 곡선 끝점");
            Assert.AreEqual(b8[0], b32[0], 1e-6);
            Assert.AreEqual(b8[2], b32[2], 1e-6);
            Assert.Less(System.Math.Abs(b8[3] - b32[3]), 0.35, "조각을 늘려도 아래 끝이 0.35유닛 넘게 안 달라진다(8조각이면 충분하다)");
        }

        [Test]
        public void 이_그림에_없는_명령은_조용히_넘어가지_않고_던진다()
        {
            Assert.Throws<System.FormatException>(() => SvgPath.Flatten("M0 0 C1 1 2 2 3 3 Z"), "C 는 안 다룬다");
            Assert.Throws<System.FormatException>(() => SvgPath.Flatten("m0 0 l1 1 l2 2 Z"), "상대 명령은 안 다룬다");
            Assert.Throws<System.FormatException>(() => SvgPath.Flatten("M0 0 L1 1 Z"), "점 둘은 면이 아니다");
        }

        [Test]
        public void 상판은_기운_사다리꼴이고_정본_매개변수_식과_맞는다()
        {
            double[][] p = SvgPath.Flatten(Top);
            // 정본 주석: 뒤 모서리 back(u) = (23 + 67u, 4 − u) · 앞 모서리 front(u) = (12 + 83u, 26 − u).
            Assert.AreEqual(23 + 67 * 0, p[0][0], Eps); Assert.AreEqual(4 - 0, p[0][1], Eps);   // back(0)
            Assert.AreEqual(23 + 67 * 1, p[1][0], Eps); Assert.AreEqual(4 - 1, p[1][1], Eps);   // back(1)
            Assert.AreEqual(12 + 83 * 1, p[2][0], Eps); Assert.AreEqual(26 - 1, p[2][1], Eps);  // front(1)
            Assert.AreEqual(12 + 83 * 0, p[3][0], Eps); Assert.AreEqual(26 - 0, p[3][1], Eps);  // front(0)
            // «기울었다» = 뒤 모서리가 앞 모서리보다 짧고(67 < 83) 오른쪽 끝이 더 위에 있다.
            Assert.Less(p[1][0] - p[0][0], p[2][0] - p[3][0], "뒤 모서리가 앞 모서리보다 짧다(원근)");
            Assert.Less(p[1][1], p[0][1], "오른쪽이 살짝 위로 기운다");
        }

        [Test]
        public void 하디_홀은_상판_면_위에_같은_기울기로_누워_있다()
        {
            // 정본 주석: 하디 홀은 u 0.06~0.19 · v 0.30~0.62 의 P(u,v) = lerp(back(u), front(u), v) 다.
            double[][] h = SvgPath.Flatten("M24.01 10.54 L33.34 10.41 L30.80 17.45 L20.80 17.58 Z");
            double[][] want =
            {
                P(0.06, 0.30), P(0.19, 0.30), P(0.19, 0.62), P(0.06, 0.62),
            };
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(want[i][0], h[i][0], 0.02, i + "번째 꼭짓점 x 가 상판 면 식과 다르다");
                Assert.AreEqual(want[i][1], h[i][1], 0.02, i + "번째 꼭짓점 y 가 상판 면 식과 다르다");
            }
        }

        /// <summary>정본 주석의 상판 면 매개변수 — P(u,v) = lerp(back(u), front(u), v).</summary>
        private static double[] P(double u, double v)
        {
            double bx = 23 + 67 * u, by = 4 - u;
            double fx = 12 + 83 * u, fy = 26 - u;
            return new double[] { bx + (fx - bx) * v, by + (fy - by) * v };
        }
    }
}
