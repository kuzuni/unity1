using System;

namespace Forge.Core.CraftFx
{
    /// <summary>
    /// CSS 타이밍 함수 `cubic-bezier(x1, y1, x2, y2)` — 정본 제작 연출 중 **linear 가 아닌 것**들이 쓴다
    /// (`afexit .18s cubic-bezier(.2,.62,.5,1)` · `afring .2s cubic-bezier(.1,.82,.28,1)` · `afbloom`·`afflash` …).
    /// 브라우저와 같은 뜻으로 푼다: 진행 t(0~1)에 대해 x(u)=t 인 u 를 찾아(뉴턴 + 이분) y(u) 를 돌려준다. 엔진 참조 0.
    /// </summary>
    public sealed class CssEase
    {
        private readonly double x1, y1, x2, y2;
        private readonly bool linear;

        /// <summary>`linear` — 정본 키프레임 대부분(모루·빌릿·시트)은 이것이다.</summary>
        public static readonly CssEase Linear = new CssEase();

        private CssEase()
        {
            linear = true;
        }

        /// <param name="x1">제어점 1의 x(0~1).</param>
        /// <param name="y1">제어점 1의 y.</param>
        /// <param name="x2">제어점 2의 x(0~1).</param>
        /// <param name="y2">제어점 2의 y.</param>
        public CssEase(double x1, double y1, double x2, double y2)
        {
            if (x1 < 0 || x1 > 1 || x2 < 0 || x2 > 1) throw new ArgumentException("cubic-bezier 의 x 는 0~1 이어야 한다(CSS 규격)");
            this.x1 = x1; this.y1 = y1; this.x2 = x2; this.y2 = y2;
        }

        /// <summary>진행 t(0~1)를 이징한 값으로. 범위 밖은 끝으로 자른다.</summary>
        public double Ease(double t)
        {
            if (t <= 0) return 0;
            if (t >= 1) return 1;
            if (linear) return t;
            return BezY(SolveU(t));
        }

        private double BezX(double u)
        {
            double m = 1 - u;
            return 3 * m * m * u * x1 + 3 * m * u * u * x2 + u * u * u;
        }

        private double BezY(double u)
        {
            double m = 1 - u;
            return 3 * m * m * u * y1 + 3 * m * u * u * y2 + u * u * u;
        }

        private double DBezX(double u)
        {
            double m = 1 - u;
            return 3 * m * m * x1 + 6 * m * u * (x2 - x1) + 3 * u * u * (1 - x2);
        }

        /// <summary>x(u) = t 인 u — 뉴턴 여덟 번(브라우저와 같은 수)으로 못 잡으면 이분법으로 마무리한다.</summary>
        private double SolveU(double t)
        {
            double u = t;
            for (int i = 0; i < 8; i++)
            {
                double x = BezX(u) - t;
                if (Math.Abs(x) < 1e-7) return u;
                double d = DBezX(u);
                if (Math.Abs(d) < 1e-9) break;
                u -= x / d;
            }
            double lo = 0, hi = 1;
            u = t;
            for (int i = 0; i < 64; i++)
            {
                double x = BezX(u);
                if (Math.Abs(x - t) < 1e-9) break;
                if (x < t) lo = u; else hi = u;
                u = (lo + hi) * 0.5;
            }
            return u;
        }
    }
}
