using System;

namespace Forge.Core.Ui
{
    /// <summary>
    /// T357 — **정본의 알파 겹은 sRGB(감마) 공간에서 섞인다.** 브라우저는 `linear-gradient(… rgba(255,255,255,.16) …)` 을 화면 바이트 위에서
    /// 그대로 `dst*(1−a) + src*a` 로 합성한다. 유니티는 이 프로젝트가 **Linear 색공간**(`ProjectSettings` `m_ActiveColorSpace: 1`)이라
    /// 같은 알파를 **선형 값 위에서** 섞는다 — 어두운 바탕일수록 결과가 훨씬 밝아진다.
    ///
    /// 실측(2026-09-15 · 런 537 `screen_main.png` 탭바 · 바탕 `#0e111b` 위 정본 `style.css` 8317 의 180° 겹):
    /// | 겹 안 위치 t | 정본(sRGB 합성) | 클론 실측 | 선형 합성 예측 |
    /// |---|---|---|---|
    /// | .098 | 42 | **98** | 98 |
    /// | .231 | 28 | **71** | 72 |
    /// | .385 | 25 | **53** | 54 |
    /// | .590 | 17 | **19** | 19 |
    /// | .897 | 10 | **9**  | 10 |
    /// 다섯 점이 전부 «선형 합성» 과 맞는다 — 즉 클론이 밝은 것은 굽는 값이 틀려서가 아니라 **섞는 공간이 달라서**다.
    ///
    /// 그래서 CSS 겹을 유니티에 옮길 때는 ⓐ 바탕색을 아는 자리(탭바·패널처럼 표에 든 단색 위)면 **여기서 sRGB 로 합성한 값을 불투명하게 구워** 얹고
    /// ⓑ 바탕을 모르면 그대로 알파로 얹되 «정본보다 밝게 보인다» 를 기록에 남긴다. 이 자는 UnityEngine 참조가 0이라 EditMode 자가 수로 지킨다.
    /// </summary>
    public static class SurfaceBlendRules
    {
        /// <summary>sRGB 바이트(0~255) → 선형 0~1 (IEC 61966-2-1).</summary>
        public static double ToLinear(double srgb01)
        {
            if (srgb01 <= 0.04045) return srgb01 / 12.92;
            return Math.Pow((srgb01 + 0.055) / 1.055, 2.4);
        }

        /// <summary>선형 0~1 → sRGB 0~1.</summary>
        public static double ToSrgb(double linear01)
        {
            if (linear01 <= 0.0031308) return linear01 * 12.92;
            return 1.055 * Math.Pow(linear01, 1.0 / 2.4) - 0.055;
        }

        /// <summary>정본(브라우저)이 섞는 길 — 바이트 위에서 바로. `a` 는 0~1.</summary>
        public static byte OverSrgb(byte dst, byte src, double a)
        {
            if (a <= 0.0) return dst;
            if (a >= 1.0) return src;
            double v = dst * (1.0 - a) + src * a;
            return Clamp(v);
        }

        /// <summary>유니티 Linear 색공간이 섞는 길 — 선형으로 풀어 섞고 다시 sRGB 로. 정본과 다른 값이 나온다(이 자의 이유).</summary>
        public static byte OverLinear(byte dst, byte src, double a)
        {
            if (a <= 0.0) return dst;
            if (a >= 1.0) return src;
            double d = ToLinear(dst / 255.0), s = ToLinear(src / 255.0);
            return Clamp(ToSrgb(d * (1.0 - a) + s * a) * 255.0);
        }

        /// <summary>
        /// CSS `linear-gradient` 의 한 점 — 정지점 둘 사이를 색·알파 **둘 다 선형 보간**한다(브라우저 규칙).
        /// <paramref name="pos"/> 는 오름차순 0~1 · <paramref name="rgba"/> 는 정지점마다 (r, g, b, a) 네 값.
        /// </summary>
        public static void Sample(double[] pos, double[][] rgba, double t, out double r, out double g, out double b, out double a)
        {
            if (pos == null || rgba == null || pos.Length == 0 || pos.Length != rgba.Length)
                throw new ArgumentException("정지점과 색 수가 같아야 한다");
            if (t <= pos[0]) { r = rgba[0][0]; g = rgba[0][1]; b = rgba[0][2]; a = rgba[0][3]; return; }
            int last = pos.Length - 1;
            if (t >= pos[last]) { r = rgba[last][0]; g = rgba[last][1]; b = rgba[last][2]; a = rgba[last][3]; return; }
            int i = 0;
            while (i < last && t > pos[i + 1]) i++;
            double span = pos[i + 1] - pos[i];
            double f = span <= 0.0 ? 0.0 : (t - pos[i]) / span;
            r = rgba[i][0] + (rgba[i + 1][0] - rgba[i][0]) * f;
            g = rgba[i][1] + (rgba[i + 1][1] - rgba[i][1]) * f;
            b = rgba[i][2] + (rgba[i + 1][2] - rgba[i][2]) * f;
            a = rgba[i][3] + (rgba[i + 1][3] - rgba[i][3]) * f;
        }

        /// <summary>정본이 그리는 최종 바이트 — 바탕색 위에 그 겹을 **sRGB 로** 얹은 값. 아는 바탕 위라면 이 값을 «불투명하게» 구워 얹으면 정본과 같아진다.</summary>
        public static void CompositeSrgb(byte[] baseRgb, double[] pos, double[][] rgba, double t, byte[] outRgb)
        {
            double r, g, b, a;
            Sample(pos, rgba, t, out r, out g, out b, out a);
            outRgb[0] = OverSrgb(baseRgb[0], Clamp(r), a);
            outRgb[1] = OverSrgb(baseRgb[1], Clamp(g), a);
            outRgb[2] = OverSrgb(baseRgb[2], Clamp(b), a);
        }

        /// <summary>지금 클론이 그리는 최종 바이트 — 같은 겹을 **선형으로** 얹은 값(회귀 자가 «얼마나 다른가» 를 수로 쥐게).</summary>
        public static void CompositeLinear(byte[] baseRgb, double[] pos, double[][] rgba, double t, byte[] outRgb)
        {
            double r, g, b, a;
            Sample(pos, rgba, t, out r, out g, out b, out a);
            outRgb[0] = OverLinear(baseRgb[0], Clamp(r), a);
            outRgb[1] = OverLinear(baseRgb[1], Clamp(g), a);
            outRgb[2] = OverLinear(baseRgb[2], Clamp(b), a);
        }

        static byte Clamp(double v)
        {
            if (v <= 0.0) return 0;
            if (v >= 255.0) return 255;
            return (byte)(v + 0.5);
        }
    }
}
