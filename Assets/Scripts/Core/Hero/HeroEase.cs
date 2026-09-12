using System;

namespace Forge.Core.Hero
{
    /// <summary>
    /// 정본 `prochar.js` ProChar 의 이징 6종 — 관절 기본은 smoothstep, 키의 3번째 원소(`EASES` 이름)가 그 키로 «들어오는 구간» 의 이징을 고른다.
    /// 1930s 카툰 이징(back 오버슈트 · bounce 착지 · snap 급가속) 포함. 값은 정본 코드 상수(T6 벡터 `ease` 와 대조).
    /// </summary>
    public static class HeroEase
    {
        /// <summary>smoothstep — 관절 기본(`ease`).</summary>
        public static double Smooth(double t) { return t * t * (3 - 2 * t); }
        /// <summary>빠른 시작(타격 스윙 · `easeOut`).</summary>
        public static double Out(double t) { return 1 - (1 - t) * (1 - t); }
        /// <summary>느린 시작(와인드업 · `easeIn`).</summary>
        public static double In(double t) { return t * t; }
        /// <summary>오버슈트 후 안착(`easeBack`).</summary>
        public static double Back(double t)
        {
            const double c = 1.70158;
            return 1 + (c + 1) * Math.Pow(t - 1, 3) + c * Math.Pow(t - 1, 2);
        }
        /// <summary>착지·정착 — 점점 작아지는 3번 튐(`easeBounce`).</summary>
        public static double Bounce(double t)
        {
            const double n = 7.5625, d = 2.75;
            if (t < 1 / d) return n * t * t;
            if (t < 2 / d) return n * (t -= 1.5 / d) * t + 0.75;
            if (t < 2.5 / d) return n * (t -= 2.25 / d) * t + 0.9375;
            return n * (t -= 2.625 / d) * t + 0.984375;
        }
        /// <summary>예비동작에서 튀어나가는 급가속 종료(`easeSnap`).</summary>
        public static double Snap(double t) { return 1 - Math.Pow(1 - t, 4); }

        /// <summary>정본 `EASES` 표: back · bounce · snap · out · in. 이름이 없거나(null) 모르는 이름이면 smoothstep(정본 `fn ? this[fn](u) : this.ease(u)`).</summary>
        public static double ByName(string name, double u)
        {
            switch (name)
            {
                case "back": return Back(u);
                case "bounce": return Bounce(u);
                case "snap": return Snap(u);
                case "out": return Out(u);
                case "in": return In(u);
                default: return Smooth(u);
            }
        }

        /// <summary>정본 `EASES` 의 이름 → 함수 이름(벡터 대조용).</summary>
        public static readonly string[][] Names =
        {
            new[] { "back", "easeBack" }, new[] { "bounce", "easeBounce" }, new[] { "snap", "easeSnap" }, new[] { "out", "easeOut" }, new[] { "in", "easeIn" },
        };
    }
}
