using System;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>
    /// CSS `filter` 함수의 **셈** — 정본 `web/css/style.css` 가 화면 곳곳에 거는 `grayscale`·`brightness`·`saturate`·`opacity`·`blur`.
    /// 자리마다의 **값**은 표(`Resources/FilterUi.json`)가 쥔다 — 여기 있는 상수는 게임 수치가 아니라 **CSS Filter Effects 명세의 정의 자체**다(π 와 같은 자리).
    /// UnityEngine 참조 0.
    ///
    /// 색 셈은 명세대로 **sRGB 값 그대로**(선형화하지 않는다) 한다 — 브라우저 `filter` 단축 함수의 기본 작업 공간이 sRGB 다.
    /// 순서도 명세대로 **적은 순서**다: `grayscale(1) brightness(1.75) opacity(.52)` 는 «회색 → 밝기 → 합성 알파» 순이다.
    /// </summary>
    public static class FilterRules
    {
        // feColorMatrix type="saturate" 의 휘도 계수(명세 표에 적힌 수 그대로).
        public const double SatLumR = 0.213, SatLumG = 0.715, SatLumB = 0.072;
        // grayscale(amount) 행렬은 명세가 따로 적어 두는데 계수가 한 자리 더 길다.
        public const double GrayLumR = 0.2126, GrayLumG = 0.7152, GrayLumB = 0.0722;

        public static double Clamp01(double v) { return v < 0 ? 0 : (v > 1 ? 1 : v); }

        /// <summary>`saturate(s)` — s=1 은 그대로, s=0 은 완전 회색, s&gt;1 은 채도 올림.</summary>
        public static void Saturate(double s, ref double r, ref double g, ref double b)
        {
            double r0 = r, g0 = g, b0 = b;
            r = (SatLumR + (1 - SatLumR) * s) * r0 + (SatLumG - SatLumG * s) * g0 + (SatLumB - SatLumB * s) * b0;
            g = (SatLumR - SatLumR * s) * r0 + (SatLumG + (1 - SatLumG) * s) * g0 + (SatLumB - SatLumB * s) * b0;
            b = (SatLumR - SatLumR * s) * r0 + (SatLumG - SatLumG * s) * g0 + (SatLumB + (1 - SatLumB) * s) * b0;
        }

        /// <summary>`grayscale(a)` — a=1 이면 휘도 한 값으로 눕는다. 명세상 `saturate(1-a)` 와 같은 꼴이지만 계수가 grayscale 표의 것이다.</summary>
        public static void Grayscale(double a, ref double r, ref double g, ref double b)
        {
            double s = 1 - a, r0 = r, g0 = g, b0 = b;
            r = (GrayLumR + (1 - GrayLumR) * s) * r0 + (GrayLumG - GrayLumG * s) * g0 + (GrayLumB - GrayLumB * s) * b0;
            g = (GrayLumR - GrayLumR * s) * r0 + (GrayLumG + (1 - GrayLumG) * s) * g0 + (GrayLumB - GrayLumB * s) * b0;
            b = (GrayLumR - GrayLumR * s) * r0 + (GrayLumG - GrayLumG * s) * g0 + (GrayLumB + (1 - GrayLumB) * s) * b0;
        }

        /// <summary>`brightness(k)` — 채널마다 기울기 k 의 선형 전달(feComponentTransfer linear · intercept 0). 1 을 넘으면 자른다.</summary>
        public static void Brightness(double k, ref double r, ref double g, ref double b)
        {
            r = Clamp01(r * k); g = Clamp01(g * k); b = Clamp01(b * k);
        }

        /// <summary>표가 적은 대로 차례차례 건다(그레이 → 채도 → 밝기). 알파는 `opacity` 가 따로 곱한다.</summary>
        public static void Apply(FilterSpec f, ref double r, ref double g, ref double b)
        {
            if (f.HasGrayscale) Grayscale(f.Grayscale, ref r, ref g, ref b);
            if (f.HasSaturate) Saturate(f.Saturate, ref r, ref g, ref b);
            if (f.HasBrightness) Brightness(f.Brightness, ref r, ref g, ref b);
            r = Clamp01(r); g = Clamp01(g); b = Clamp01(b);
        }

        // ── blur ────────────────────────────────────────────────────────────
        // 명세: `blur(r)` = feGaussianBlur stdDeviation = r. «반지름» 이 아니라 **표준편차**다 —
        // 1.2px 를 반지름으로 읽으면 훨씬 덜 번진다.

        /// <summary>커널 반경 — 가우시안을 자르는 자리(3σ 면 꼬리 0.3% 다). 최소 1.</summary>
        public static int KernelRadius(double sigma)
        {
            if (sigma <= 0) return 0;
            int r = (int)Math.Ceiling(sigma * 3.0);
            return r < 1 ? 1 : r;
        }

        /// <summary>1차원 가우시안 커널(합 1) — 2차원은 이것을 가로·세로 두 번 건다(분리 가능).</summary>
        public static double[] GaussianKernel(double sigma)
        {
            int r = KernelRadius(sigma);
            if (r == 0) return new double[] { 1.0 };
            var k = new double[2 * r + 1];
            double two = 2.0 * sigma * sigma, sum = 0;
            for (int i = -r; i <= r; i++) { double v = Math.Exp(-(i * i) / two); k[i + r] = v; sum += v; }
            for (int i = 0; i < k.Length; i++) k[i] /= sum;
            return k;
        }

        /// <summary>
        /// 정본이 말한 `blur(N CSS px)` 를 **굽는 그림의 화소**로 옮긴다.
        /// 도형을 정규 상자에 굽고 칸 크기로 늘려 쓰는 자리(<c>PetHatchCone</c>)에서는 늘림 배율만큼 번짐도 늘어난다 —
        /// 그러니 구울 때의 σ 는 «구운 화소수 / 화면에 설 화소수» 를 곱한 값이어야 화면에서 N px 가 된다.
        /// </summary>
        public static double BakeSigmaPx(double cssPx, double bakedPx, double displayPx)
        {
            if (cssPx <= 0 || bakedPx <= 0 || displayPx <= 0) return 0;
            return cssPx * (bakedPx / displayPx);
        }
    }

    /// <summary>한 자리의 `filter` 선언 — 표(`Resources/FilterUi.json`)의 한 줄.</summary>
    public sealed class FilterSpec
    {
        public string Key;
        /// <summary>정본 CSS 줄 번호(출처 · 사람이 되짚는 자리).</summary>
        public int Line;
        public double Grayscale, Saturate, Brightness, Opacity, BlurPx;
        public bool HasGrayscale, HasSaturate, HasBrightness, HasOpacity, HasBlur;

        public static FilterSpec From(string key, JsonObject o)
        {
            var f = new FilterSpec { Key = key };
            f.Line = (int)J.Num(J.Require(o, "line"));
            f.HasGrayscale = Pick(o, "grayscale", out f.Grayscale);
            f.HasSaturate = Pick(o, "saturate", out f.Saturate);
            f.HasBrightness = Pick(o, "brightness", out f.Brightness);
            f.HasOpacity = Pick(o, "opacity", out f.Opacity);
            f.HasBlur = Pick(o, "blur_px", out f.BlurPx);
            if (!(f.HasGrayscale || f.HasSaturate || f.HasBrightness || f.HasOpacity || f.HasBlur))
                throw new FormatException("FilterUi 줄에 filter 함수가 하나도 없다: " + key);
            if (f.HasOpacity && (f.Opacity < 0 || f.Opacity > 1))
                throw new FormatException("FilterUi opacity 는 0~1 이다: " + key);
            if (f.HasGrayscale && (f.Grayscale < 0 || f.Grayscale > 1))
                throw new FormatException("FilterUi grayscale 은 0~1 이다: " + key);
            if ((f.HasBrightness && f.Brightness < 0) || (f.HasSaturate && f.Saturate < 0) || (f.HasBlur && f.BlurPx < 0))
                throw new FormatException("FilterUi brightness·saturate·blur_px 는 음수가 아니다: " + key);
            return f;
        }

        static bool Pick(JsonObject o, string k, out double v)
        {
            v = 0;
            object raw = o == null ? null : o[k];
            if (raw == null) return false;
            v = J.Num(raw);
            return true;
        }
    }

    /// <summary>표 전체 — `Resources/FilterUi.json`.</summary>
    public sealed class FilterTable
    {
        readonly System.Collections.Generic.Dictionary<string, FilterSpec> map =
            new System.Collections.Generic.Dictionary<string, FilterSpec>(StringComparer.Ordinal);

        public int Count { get { return map.Count; } }

        public FilterSpec Get(string key)
        {
            FilterSpec f;
            if (!map.TryGetValue(key, out f)) throw new FormatException("FilterUi 에 없는 자리다: " + key);
            return f;
        }

        public bool Has(string key) { return map.ContainsKey(key); }

        public static FilterTable From(JsonObject root)
        {
            var sites = J.Obj(J.Require(root, "sites"));
            var t = new FilterTable();
            foreach (var k in sites.Keys)
            {
                if (k.Length > 0 && k[0] == '_') continue;      // `_` 로 시작하는 칸은 설명이다
                t.map[k] = FilterSpec.From(k, J.Obj(sites[k]));
            }
            if (t.map.Count == 0) throw new FormatException("FilterUi 에 자리가 하나도 없다");
            return t;
        }
    }
}
