namespace Forge.Core.Ui
{
    /// <summary>T178 48회차 — 소환 결과 구슬의 런타임 색 셋(정본 `ui.js` 188~209 · UnityEngine 0).
    /// `--rc` 는 등급색 그대로 · `--rc-lite` 는 등급마다 **목표 휘도**(SR_HILITE_LUMA)로 흰색 쪽으로 당긴 값(srHilite) · `--rc-deep` 은 `srShade(rc, −.62)`.
    /// 정본 주석: «이걸 상수 배합(예전 srShade(rc, .5))으로 두면 위계가 뒤집힌다 — 하이라이트 휘도가 등급색 자체의 밝기에 끌려가서».</summary>
    public static class SummonOrbRules
    {
        /// <summary>정본 `srHilite` 의 휘도(0~255 채널 · `.299 .587 .114`).</summary>
        public static double Luma255(double r, double g, double b) { return r * .299 + g * .587 + b * .114; }

        /// <summary>목표 휘도까지 흰색 쪽으로 당기는 비율 — `clamp((want − luma) / max(1, 255 − luma), 0, 1)`.</summary>
        public static double HiliteAmt(double luma255, double want255)
        {
            double d = (want255 - luma255) / System.Math.Max(1.0, 255.0 - luma255);
            return d < 0 ? 0 : d > 1 ? 1 : d;
        }

        /// <summary>정본 `srShade(hex, amt)` 의 채널 하나 — amt &gt; 0 이면 `c + (255 − c) × amt`(흰색 쪽) · amt &lt; 0 이면 `c × (1 + amt)`(검정 쪽). 0~255 로 자른다.</summary>
        public static double Shade255(double c255, double amt)
        {
            double v = amt > 0 ? c255 + (255.0 - c255) * amt : c255 * (1.0 + amt);
            return v < 0 ? 0 : v > 255 ? 255 : v;
        }
    }
}
