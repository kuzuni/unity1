using System;
using System.Collections.Generic;
using System.Globalization;

namespace Forge.Core.Ui
{
    /// <summary>
    /// T98 1회차 — 정본 SVG 의 `d` 문자열을 **글자 그대로 들고 와** 꼭짓점으로 펴는 자. 엔진 참조 0.
    ///
    /// 왜 문자열을 그대로 두는가: 모루 그림(정본 `ui.js` 2387 `ANVIL_SVG`)은 주석이 «좌표는 감으로 찍지 말 것 —
    /// 상판 면을 매개변수로 풀어서 뽑았다» 라고 못 박은 자리다. 꼭짓점을 손으로 옮겨 적으면 그 대조가 끊긴다.
    /// 그래서 표(`Assets/Forge/Resources/AnvilArtUi.json`)에는 정본 `d` 를 **한 글자도 안 바꾸고** 담고,
    /// 클론 쪽에서 그것을 편다 — 정본이 바뀌면 문자열 대조 하나로 드러난다.
    ///
    /// 다루는 명령은 정본 모루가 쓰는 **절대 명령 넷**뿐이다: `M`(시작) · `L`(직선) · `Q`(2차 베지에) · `Z`(닫기).
    /// 상대 명령(`m`·`l`·`q`)·`C`·`A` 는 이 그림에 없으므로 만나면 **던진다** — 조용히 건너뛰면 도형이 소리 없이 뭉개진다.
    /// </summary>
    public static class SvgPath
    {
        /// <summary>곡선 하나를 몇 조각으로 펼 것인가(기본값) — 모루 뿔·받침 라운드가 132×86 viewBox 에서 계단으로 안 보이는 최소값.</summary>
        public const int DefaultCurveSamples = 8;

        /// <summary>
        /// `d` 를 꼭짓점 목록으로 편다(각 원소는 <c>{x, y}</c>). `Z` 는 «첫 점으로 닫는다» 는 뜻이라 점을 더하지 않는다 —
        /// 폴리곤을 굽는 쪽(`CraftFxPoly.Bake`)이 이미 닫힌 것으로 다루기 때문이다.
        /// </summary>
        /// <param name="d">정본 `d` 문자열(절대 M·L·Q·Z).</param>
        /// <param name="curveSamples">`Q` 하나를 펴는 조각 수(1 이상 · 끝점 포함).</param>
        public static double[][] Flatten(string d, int curveSamples)
        {
            if (d == null) throw new ArgumentNullException("d");
            if (curveSamples < 1) throw new ArgumentOutOfRangeException("curveSamples", "곡선 조각은 1 이상이다");
            List<double[]> pts = new List<double[]>();
            int i = 0;
            char cmd = '\0';
            double cx = 0, cy = 0;
            while (true)
            {
                SkipSep(d, ref i);
                if (i >= d.Length) break;
                char c = d[i];
                if (c == 'M' || c == 'L' || c == 'Q' || c == 'Z' || c == 'z') { cmd = c; i++; }
                else if (cmd == '\0') throw new FormatException("`d` 가 명령 없이 수로 시작한다: " + d);
                // 같은 명령이 수만 이어지는 꼴(`L12 26 95 25`)은 앞 명령을 되쓴다 — SVG 규칙 그대로다.

                if (cmd == 'Z' || cmd == 'z') { cmd = '\0'; continue; }
                if (cmd == 'M' || cmd == 'L')
                {
                    cx = Num(d, ref i); cy = Num(d, ref i);
                    pts.Add(new double[] { cx, cy });
                }
                else if (cmd == 'Q')
                {
                    double qx = Num(d, ref i), qy = Num(d, ref i);
                    double ex = Num(d, ref i), ey = Num(d, ref i);
                    for (int k = 1; k <= curveSamples; k++)
                    {
                        double t = (double)k / curveSamples, u = 1 - t;
                        pts.Add(new double[] { u * u * cx + 2 * u * t * qx + t * t * ex,
                                               u * u * cy + 2 * u * t * qy + t * t * ey });
                    }
                    cx = ex; cy = ey;
                }
                else throw new FormatException("이 그림에 없는 SVG 명령이다(상대 명령·C·A 는 안 다룬다): " + cmd);
            }
            if (pts.Count < 3) throw new FormatException("꼭짓점이 셋보다 적다 — 면이 아니다: " + d);
            return pts.ToArray();
        }

        /// <summary>기본 조각 수로 편다.</summary>
        public static double[][] Flatten(string d) { return Flatten(d, DefaultCurveSamples); }

        /// <summary>편 점들의 상자 — <c>{minX, minY, maxX, maxY}</c>.</summary>
        public static double[] Bounds(double[][] pts)
        {
            if (pts == null || pts.Length == 0) throw new ArgumentException("점이 없다", "pts");
            double x0 = pts[0][0], y0 = pts[0][1], x1 = x0, y1 = y0;
            for (int i = 1; i < pts.Length; i++)
            {
                if (pts[i][0] < x0) x0 = pts[i][0];
                if (pts[i][0] > x1) x1 = pts[i][0];
                if (pts[i][1] < y0) y0 = pts[i][1];
                if (pts[i][1] > y1) y1 = pts[i][1];
            }
            return new double[] { x0, y0, x1, y1 };
        }

        private static void SkipSep(string d, ref int i)
        {
            while (i < d.Length && (d[i] == ' ' || d[i] == ',' || d[i] == '\t' || d[i] == '\n' || d[i] == '\r')) i++;
        }

        private static double Num(string d, ref int i)
        {
            SkipSep(d, ref i);
            int s = i;
            if (i < d.Length && (d[i] == '-' || d[i] == '+')) i++;
            while (i < d.Length && ((d[i] >= '0' && d[i] <= '9') || d[i] == '.')) i++;
            if (i == s) throw new FormatException("수가 와야 할 자리에 «" + (i < d.Length ? d[i].ToString() : "끝") + "» 이 있다: " + d);
            return double.Parse(d.Substring(s, i - s), CultureInfo.InvariantCulture);
        }
    }
}
