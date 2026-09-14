using System;

namespace Forge.Core.Ui
{
    /// <summary>
    /// T333 — 정본 `text-shadow: dx dy blur color`(한 겹)를 TMP SDF 재질의 언더레이(`_Underlay*`)로 옮기는 식. 셰이더는 기본 폰트 재질이 문
    /// 모바일 SDF(`Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader` 175~179 · 197 · 219~220):
    /// <code>
    ///   layerScale /= 1 + (_UnderlaySoftness × R_C × layerScale)              // 램프를 softness 알파만큼 넓힌다(흐림)
    ///   layerBias   = (.5 − weight) × layerScale − .5 − (_UnderlayDilate × R_C × ½ × layerScale)
    ///   uv 이동     = −(_UnderlayOffsetX × R_C) × G / TexW  (y 도 같이)         // 텍스처 좌표 이동 = 그림자가 +Offset 쪽(오른쪽·위)으로
    ///   1 알파      = G 텍셀 · 1 텍셀 = fontSize / samplingPointSize px            // T104·T121 과 같은 단위(G 는 UiFont 가 실측 램프로 세운다)
    /// </code>
    /// 그러니 Offset 1.0 = R_C × G 텍셀 = «단위 px»(<see cref="UnitPx"/>) 만큼 이동이고, CSS 의 y(아래가 +)는 TMP 의 y(위가 +)와 부호가 반대다.
    /// 흐림은 근사다: CSS blur 반지름 b(가우시안 σ ≈ b/2 · 눈에 보이는 폭 ≈ b)를 «램프 폭 b» 로 — softness = b / 단위 px. 정본 겹이 여럿(글로우·4방 링)이면
    /// TMP 한 겹으로는 못 내니 호출자가 한 겹만 고른다(등재 절 ⓒ·ⓓ). UnityEngine 0 — 재질·폰트 값은 호출자(`UiKit.TextShadow`)가 읽어 넘긴다.
    /// </summary>
    public struct UnderlaySdf
    {
        /// <summary>TMP `_UnderlayOffsetX`(−1~1).</summary>
        public double OffsetX01;
        /// <summary>TMP `_UnderlayOffsetY`(−1~1 · 위가 +).</summary>
        public double OffsetY01;
        /// <summary>TMP `_UnderlaySoftness`(0~1).</summary>
        public double Softness01;
        /// <summary>단위 px — Offset 1.0 · softness 1.0 이 몇 캔버스 px 인가 = R_C × G × fontSize / sampling.</summary>
        public double UnitPx;
        /// <summary>여백이 모자라 −1~1 / 0~1 로 잘렸다.</summary>
        public bool Clipped;

        public static double Unit(double fontSize, double gradientScale, double scaleRatioC, double samplingPointSize)
        {
            if (fontSize <= 0 || gradientScale <= 0 || scaleRatioC <= 0 || samplingPointSize <= 0) return 0;
            return scaleRatioC * gradientScale * fontSize / samplingPointSize;
        }

        /// <summary>정본 한 겹(dx·dy = CSS 방향 · 아래가 + · 캔버스 px · blur = 반지름 px)을 재질 값으로.</summary>
        public static UnderlaySdf FromPx(double dxPx, double dyPx, double blurPx, double fontSize, double gradientScale, double scaleRatioC, double samplingPointSize)
        {
            UnderlaySdf r = new UnderlaySdf();
            double unit = Unit(fontSize, gradientScale, scaleRatioC, samplingPointSize);
            r.UnitPx = unit;
            if (unit <= 0) return r;
            double x = dxPx / unit, y = -dyPx / unit, s = blurPx > 0 ? blurPx / unit : 0;
            if (x > 1 || x < -1) { x = Math.Max(-1, Math.Min(1, x)); r.Clipped = true; }
            if (y > 1 || y < -1) { y = Math.Max(-1, Math.Min(1, y)); r.Clipped = true; }
            if (s > 1) { s = 1; r.Clipped = true; }
            r.OffsetX01 = x; r.OffsetY01 = y; r.Softness01 = s;
            return r;
        }

        /// <summary>재질 값에서 되짚은 캔버스 px(dx · dy 는 CSS 방향 · blur) — 자가 «표대로 걸렸다» 를 잴 때.</summary>
        public static void ToPx(double offsetX01, double offsetY01, double softness01, double unitPx, out double dxPx, out double dyPx, out double blurPx)
        {
            dxPx = offsetX01 * unitPx; dyPx = -offsetY01 * unitPx; blurPx = softness01 * unitPx;
        }
    }
}
