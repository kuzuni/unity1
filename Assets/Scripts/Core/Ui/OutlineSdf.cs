using System;

namespace Forge.Core.Ui
{
    /// <summary>
    /// T104 — 정본 `-webkit-text-stroke: N` + `paint-order: stroke fill`(획 N 을 윤곽 **중심**에 긋고 채움을 위에 칠한다 → 보이는 것은
    /// 바깥 N/2 뿐이고 채움은 그대로)을 TMP SDF 재질 값으로 옮기는 식. 셰이더는 기본 폰트 재질이 문 **모바일 SDF**
    /// (`Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader` 148~155 · 210~215행):
    /// <code>
    ///   weight  = _FaceDilate   × R × ½      // 채움 가장자리를 바깥으로 weight 알파만큼
    ///   outline = _OutlineWidth × R × ½      // 그 가장자리 **양쪽**으로 outline 알파만큼 띠
    ///   1 알파  = _GradientScale(G) 텍셀 · 1 텍셀 = fontSize / samplingPointSize px
    /// </code>
    /// 그러니 TMP 의 띠는 가장자리 중심이라 옛 갈래(`Dilate` 0)는 띠의 절반이 채움을 먹고, 보이는 두께도 `width01 × R × ½ × G × px/텍셀`
    /// 뿐이다(17px 글자 · 0.25 → 1px 미만 — T104 등재 실측). 정본과 같은 그림은 **띠의 안쪽 끝을 원래 윤곽에 붙이는 것**:
    /// 가장자리 이동(D×R×½) = 띠 반폭(W×R×½) ⇒ D = W · 바깥 띠 = W × R × G × px/텍셀 = N/2.
    /// 단위 검산: TMP 기본 애셋(패딩 9 · 90pt → G 10 · R .9)에서 W = 1 이면 바깥 띠 = 0.1 × fontSize = 패딩 px — TMP 문서의
    /// «효과는 패딩까지» 와 같다. 여백이 모자라면(W &gt; 1) 1 로 잘리고 <see cref="Clipped"/> 가 선다.
    /// T121: 게임 글꼴은 그 기본이 아니라 `Resources/UiFontBake.json`(90pt · 패딩 15 → G 16)으로 굽는다 — 정본 최대 획 .2em(바깥 .1em)이
    /// 기본 패딩(글자의 10%)에서는 정확히 천장(W = 1)이라 링이 «면» 이 됐다. 표 값이면 36px 글자의 최대 바깥 띠가 6px(W .6).
    /// UnityEngine 0 — 재질·폰트 값은 호출자(`UiKit.OutlinePx`)가 읽어 넘긴다.
    /// </summary>
    public struct OutlineSdf
    {
        /// <summary>TMP `outlineWidth`(_OutlineWidth · 0~1).</summary>
        public double Width01;
        /// <summary>재질 `_FaceDilate`(0~1) — 채움을 바깥으로 밀어 띠가 채움을 안 먹게.</summary>
        public double Dilate;
        /// <summary>실제로 보이는 바깥 띠(px · 잘렸으면 원한 값보다 작다).</summary>
        public double VisiblePx;
        /// <summary>원한 바깥 띠 = 획/2 (px).</summary>
        public double WantedPx;
        /// <summary>패딩이 모자라 1 로 잘렸다.</summary>
        public bool Clipped;

        /// <summary>1 알파가 몇 px 인가 = G × fontSize / samplingPointSize.</summary>
        public static double AlphaPx(double fontSize, double gradientScale, double samplingPointSize)
        {
            if (fontSize <= 0 || gradientScale <= 0 || samplingPointSize <= 0) return 0;
            return gradientScale * fontSize / samplingPointSize;
        }

        /// <summary>W = 1 · D = 1 일 때의 바깥 띠(px) = R × G × px/텍셀 — 이 애셋에서 낼 수 있는 최대 키라인.</summary>
        public static double UnitPx(double fontSize, double gradientScale, double scaleRatioA, double samplingPointSize)
        {
            if (scaleRatioA <= 0) return 0;
            return scaleRatioA * AlphaPx(fontSize, gradientScale, samplingPointSize);
        }

        /// <summary>정본 획 <paramref name="strokePx"/>(윤곽 중심 · px)를 «바깥 N/2 · 채움 그대로» 로 내는 재질 값.</summary>
        public static OutlineSdf FromStroke(double strokePx, double fontSize, double gradientScale, double scaleRatioA, double samplingPointSize)
        {
            OutlineSdf r = new OutlineSdf();
            r.WantedPx = strokePx > 0 ? strokePx * 0.5 : 0;
            double unit = UnitPx(fontSize, gradientScale, scaleRatioA, samplingPointSize);
            if (strokePx <= 0 || unit <= 0) return r;
            double w = strokePx / (2.0 * unit);
            if (w > 1.0) { w = 1.0; r.Clipped = true; }
            r.Width01 = w;
            r.Dilate = w;
            r.VisiblePx = w * unit;
            return r;
        }

        /// <summary>옛 갈래(`Dilate` 0 · 호출자 상수 width01)가 실제로 보이던 바깥 띠(px) = width01 × R × ½ × G × px/텍셀 — 안쪽 같은 두께는 채움을 먹는다.</summary>
        public static double LegacyVisiblePx(double width01, double fontSize, double gradientScale, double scaleRatioA, double samplingPointSize)
        {
            if (width01 <= 0) return 0;
            return Math.Min(width01, 1.0) * 0.5 * UnitPx(fontSize, gradientScale, scaleRatioA, samplingPointSize);
        }
    }
}
