using System;

namespace Forge.Core.Ui
{
    /// <summary>
    /// T385 — 소환 결과 구슬 위 아이콘의 «조명 통합» 셈. 정본 `web/css/style.css` 6509~6547 이 처방을 주석으로 적어 둔 자리다
    /// (⑴ 접지 그림자 ⑵ 약 4% 배럴 스케일 ⑶ 구체 스페큘러를 아이콘 위에 한 겹 더).
    /// 여기는 **셈만** 쥔다 — 값은 표(`Resources/OrbIconUi.json`), 그림은 굽는 층 몫이다. UnityEngine 참조 0.
    ///
    /// 풀어야 할 것 하나: 정본의 겹은 `.sr-ico::after` 라 **아이콘 한 변**을 기준으로 놓였는데(`left/top −60%` · `220%` 정사각),
    /// 빛나야 할 자리는 **구체의 하이라이트와 같은 곳**이다. 아이콘이 구체에서 차지하는 비율이 정본과 다르면
    /// 같은 수를 그대로 써도 빛이 다른 데서 난다 — 그래서 «아이콘 기준 → 구체 기준» 환산(<see cref="ToOrbFrac"/>)이 필요하다.
    /// </summary>
    public static class OrbIconRules
    {
        /// <summary>겹 상자 — 부모(아이콘) 한 변을 1 로 본 비율. 정본은 left/top −0.6 · 한 변 2.2.</summary>
        public struct OverlayBox
        {
            public double Left, Top, W, H;
            public OverlayBox(double left, double top, double w, double h) { Left = left; Top = top; W = w; H = h; }
        }

        /// <summary>`radial-gradient(… at X% Y%)` 의 중심을 **부모 기준 비율**로. 0 미만이면 부모 상자 위/왼쪽 바깥이다.</summary>
        public static double CentreX(OverlayBox b, double atXFrac) { return b.Left + b.W * atXFrac; }
        /// <summary>같은 것의 세로 — CSS 와 같이 **위가 0** 이다.</summary>
        public static double CentreY(OverlayBox b, double atYFrac) { return b.Top + b.H * atYFrac; }

        /// <summary>`radial-gradient(RX% RY% …)` 의 반지름을 부모 기준 비율로.</summary>
        public static double RadiusX(OverlayBox b, double rXFrac) { return b.W * rXFrac; }
        public static double RadiusY(OverlayBox b, double rYFrac) { return b.H * rYFrac; }

        /// <summary>타원 거리 — 1 이면 반지름 위, 0 이면 중심. CSS 방사형은 이 t 로 정지 위치를 잰다.</summary>
        public static double EllipseT(double x, double y, double cx, double cy, double rx, double ry)
        {
            if (rx <= 0 || ry <= 0) throw new ArgumentOutOfRangeException("rx", "반지름은 0보다 커야 한다");
            double dx = (x - cx) / rx, dy = (y - cy) / ry;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 정본 두 겹은 «색 → 투명 <c>end</c>%» 두 정지뿐이라 감쇠가 한 줄이다.
        /// t 가 0 이면 1(제 색 그대로) · <paramref name="endFrac"/> 이상이면 0.
        /// </summary>
        public static double Falloff(double t, double endFrac)
        {
            if (endFrac <= 0) throw new ArgumentOutOfRangeException("endFrac", "마지막 정지는 0보다 커야 한다");
            if (t <= 0) return 1.0;
            if (t >= endFrac) return 0.0;
            return 1.0 - t / endFrac;
        }

        /// <summary>
        /// 아이콘이 구체 한가운데 서고 구체 한 변의 <paramref name="iconFracOfOrb"/> 만큼일 때,
        /// «아이콘 기준 비율» 을 «구체 기준 비율» 로 옮긴다. 정본은 `.sr-orb` 2.4rem 안의 1em 아이콘이라 1/2.4 다.
        /// </summary>
        public static double ToOrbFrac(double iconFracOfOrb, double iconRelFrac)
        {
            if (iconFracOfOrb <= 0) throw new ArgumentOutOfRangeException("iconFracOfOrb", "아이콘 비율은 0보다 커야 한다");
            return (0.5 - iconFracOfOrb * 0.5) + iconRelFrac * iconFracOfOrb;
        }

        /// <summary>
        /// CSS `mix-blend-mode: screen` — `1 − (1−a)(1−b)`. UGUI 엔 이 합성이 없어 굽는 층이 더하기로 근사하는데,
        /// **얼마나 밝게 어긋나는지**를 이 식으로 잴 수 있어야 해서 여기 둔다(어두운 바탕일수록 둘이 가깝다).
        /// </summary>
        public static double ScreenBlend(double a, double b) { return a + b - a * b; }
    }
}
