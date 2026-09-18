using System;

namespace Forge.Core.Ui
{
    /// <summary>
    /// T472 — 정본 `border-style: dashed` 를 **둥근 사각 테** 위에서 셈하는 자. `UnityEngine` 참조 0(§1).
    ///
    /// CSS 의 점선 테는 «상자 안쪽으로 굵기 <c>thick</c> 만큼 두른 띠» 를 둘레를 따라 «대시 <c>dash</c> · 틈 <c>gap</c>» 으로 끊은 것이다.
    /// 브라우저(Blink)는 대시·틈을 굵기의 3배로 잡고 둘레에 **정수 개**가 딱 맞게 주기를 조금 늘이거나 줄인다(모서리에서 반 토막이 안 남게) —
    /// 그 두 셈이 <see cref="FitPeriod"/> 다. 정본 캡처(shot-043224)는 두 슬롯이 다 장착이라 빈 카드가 안 찍혀 배율은 표가 쥔다(`cmp_empty_dash_ratio`·`cmp_empty_gap_ratio`).
    ///
    /// 좌표는 CSS 뜻 그대로 **왼쪽 위가 (0,0) · y 는 아래가 +** 이고, 둘레 자리 <see cref="ArcPos"/> 는 위 변의 왼쪽 직선 시작점(x = r, y = 0)에서
    /// 시계 방향(위 → 오른쪽 → 아래 → 왼쪽)으로 잰다.
    /// </summary>
    public static class DashedFrameRules
    {
        /// <summary>반지름을 상자에 맞게 자른다(CSS 도 `border-radius` 가 반 변을 넘으면 그만큼 줄인다).</summary>
        public static double ClampRadius(double w, double h, double r)
        {
            double m = Math.Min(w, h) * 0.5;
            if (r < 0) r = 0;
            return r > m ? m : r;
        }

        /// <summary>둥근 사각의 둘레 = 직선 넷 + 사분원 넷(= 원 하나).</summary>
        public static double Perimeter(double w, double h, double r)
        {
            r = ClampRadius(w, h, r);
            return 2.0 * (w - 2.0 * r) + 2.0 * (h - 2.0 * r) + 2.0 * Math.PI * r;
        }

        /// <summary>
        /// 둥근 사각 경계까지의 **부호 있는 거리**(안쪽 음수 · 경계 0 · 바깥 양수). 상자는 (0,0)~(w,h).
        /// </summary>
        public static double Distance(double x, double y, double w, double h, double r)
        {
            r = ClampRadius(w, h, r);
            double hx = w * 0.5 - r, hy = h * 0.5 - r;
            double qx = Math.Abs(x - w * 0.5) - hx, qy = Math.Abs(y - h * 0.5) - hy;
            double ox = qx > 0 ? qx : 0, oy = qy > 0 ? qy : 0;
            double outside = Math.Sqrt(ox * ox + oy * oy);
            double inside = Math.Min(Math.Max(qx, qy), 0.0);
            return outside + inside - r;
        }

        /// <summary>
        /// 점이 놓인 자리를 둘레 위의 길이(0 ~ <see cref="Perimeter"/>)로 — 위 변 왼쪽 직선 시작(x = r · y = 0)에서 시계 방향.
        /// 직선 구간은 축 좌표로, 모서리 구간은 그 사분원 중심에서 본 각도로 잰다(띠 안 어느 깊이의 점이든 같은 자리로 접힌다).
        /// </summary>
        public static double ArcPos(double x, double y, double w, double h, double r)
        {
            r = ClampRadius(w, h, r);
            double sw = w - 2.0 * r, sh = h - 2.0 * r, q = Math.PI * r * 0.5;
            bool midX = x >= r && x <= w - r, midY = y >= r && y <= h - r;
            if (midX && !midY)
            {
                if (y < h * 0.5) return Clamp(x - r, 0, sw);                              // 위 변 →
                return sw + q + sh + q + Clamp(w - r - x, 0, sw);                          // 아래 변 ←
            }
            if (midY && !midX)
            {
                if (x >= w * 0.5) return sw + q + Clamp(y - r, 0, sh);                     // 오른 변 ↓
                return sw + q + sh + q + sw + q + Clamp(h - r - y, 0, sh);                 // 왼 변 ↑
            }
            if (midX && midY)
            {
                // 상자 한가운데 직사각 안쪽 — 가장 가까운 변으로 접는다(띠 밖이라 잉크 판정엔 안 쓰이지만 값은 연속이어야 한다)
                double dt = y, db = h - y, dl = x, dr = w - x;
                double m = Math.Min(Math.Min(dt, db), Math.Min(dl, dr));
                if (m == dt) return Clamp(x - r, 0, sw);
                if (m == dr) return sw + q + Clamp(y - r, 0, sh);
                if (m == db) return sw + q + sh + q + Clamp(w - r - x, 0, sw);
                return sw + q + sh + q + sw + q + Clamp(h - r - y, 0, sh);
            }
            if (r <= 0) return 0;
            // 모서리 사분원 — 중심에서 본 각도(atan2 · y 아래가 +)
            if (x > w - r && y < r)                                                        // 오른쪽 위: −π/2 → 0
            {
                double a = Math.Atan2(y - r, x - (w - r));
                return sw + r * Clamp(a + Math.PI * 0.5, 0, Math.PI * 0.5);
            }
            if (x > w - r)                                                                 // 오른쪽 아래: 0 → π/2
            {
                double a = Math.Atan2(y - (h - r), x - (w - r));
                return sw + q + sh + r * Clamp(a, 0, Math.PI * 0.5);
            }
            if (y > h - r)                                                                 // 왼쪽 아래: π/2 → π
            {
                double a = Math.Atan2(y - (h - r), x - r);
                return sw + q + sh + q + sw + r * Clamp(a - Math.PI * 0.5, 0, Math.PI * 0.5);
            }
            {                                                                              // 왼쪽 위: −π → −π/2
                double a = Math.Atan2(y - r, x - r);
                if (a > 0) a -= 2.0 * Math.PI;                                             // atan2 가 +π 를 주는 경계(y == r)를 −π 로
                return sw + q + sh + q + sw + q + sh + r * Clamp(a + Math.PI, 0, Math.PI * 0.5);   // 왼 변(sh)을 다 지난 뒤
            }
        }

        /// <summary>
        /// 둘레에 «대시 + 틈» 이 **정수 개** 들어가게 주기를 맞춘다(Blink 의 dash fitting) — 돌려주는 값은 맞춘 주기, <paramref name="dashFit"/> 은 그 주기 안 대시 길이(비율 유지).
        /// </summary>
        public static double FitPeriod(double perimeter, double dash, double gap, out double dashFit)
        {
            double p = dash + gap;
            if (perimeter <= 0 || p <= 0) { dashFit = dash; return p; }
            int n = (int)Math.Round(perimeter / p);
            if (n < 1) n = 1;
            double period = perimeter / n;
            dashFit = period * (dash / p);
            return period;
        }

        /// <summary>
        /// 화소 (x,y) 가 점선 테의 **잉크**인가 — 띠(경계에서 안쪽으로 <paramref name="thick"/>) 안이고 둘레 자리가 대시 구간이면 참.
        /// </summary>
        public static bool IsInk(double x, double y, double w, double h, double r, double thick, double dash, double gap)
        {
            if (thick <= 0) return false;
            double d = Distance(x, y, w, h, r);
            if (d > 0 || d < -thick) return false;
            if (gap <= 0) return true;
            double dashFit;
            double period = FitPeriod(Perimeter(w, h, r), dash, gap, out dashFit);
            double s = ArcPos(x, y, w, h, r) % period;
            if (s < 0) s += period;
            return s < dashFit;
        }

        static double Clamp(double v, double lo, double hi) { return v < lo ? lo : (v > hi ? hi : v); }
    }
}
