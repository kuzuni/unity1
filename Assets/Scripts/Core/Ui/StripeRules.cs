using System;

namespace Forge.Core.Ui
{
    /// <summary>
    /// T368 — 정본이 `repeating-linear-gradient` 로 까는 **되풀이 줄무늬**의 셈. `UnityEngine` 참조 0(§1).
    ///
    /// 정본의 `background-image: repeating-linear-gradient(θ, A 0 d, B d p)` 는 «각도 θ 축을 따라 주기 p 마다
    /// 앞 d 만큼은 A, 나머지는 B» 다. 한 겹짜리 <see cref="SurfaceRules"/>(정지점 하나로 끝나는 그라디언트)와 달리
    /// **끝없이 되풀이**되므로 판을 «한 주기» 만 굽고 타일링하면 된다 — 그것이 이 자의 셈이다.
    ///
    /// 각도는 CSS 뜻 그대로: 0deg 가 위로, 90deg 가 오른쪽. 축 방향은 (sinθ, −cosθ)(y 아래가 +)이고
    /// 어느 화소의 축 위 자리는 그 벡터와의 내적이다. −45deg 면 (−√2/2, −√2/2) 라 **오른쪽 위로 올라가는 띠**가 된다.
    ///
    /// ⚠ **가로 치수에 rem 을 쓰지 않는다**(정본 2547 주석 «가로 치수라 rem 금지 — 루트 폰트가 높이 기준이다» · T364 가 수로 잡은 그 함정).
    /// 그래서 표는 주기·대시를 **앱 폭 비율**(`period_w`·`dash_w`)로 담고, 여기서는 이미 **캔버스 px 로 바뀐 값**만 받는다.
    /// </summary>
    public static class StripeRules
    {
        /// <summary>CSS 각도 → 축 단위벡터(x 오른쪽 · y 아래가 +). 0deg = 위(0,−1) · 90deg = 오른쪽(1,0).</summary>
        public static void Axis(double angleDeg, out double ax, out double ay)
        {
            double r = angleDeg * Math.PI / 180.0;
            ax = Math.Sin(r);
            ay = -Math.Cos(r);
        }

        /// <summary>
        /// 그 화소가 «앞 색»(대시) 인가. <paramref name="phase"/> 는 정본 `background-position` 의 축 방향 당김(캔버스 px).
        ///
        /// ⚠ **0 의 부호에 걸린다**: `Math.Cos(π/2)` 는 정확히 0 이 아니라 6.1e−17 이라, 가로 줄무늬(90deg)에서 y 가 큰 화소의 축 자리가
        /// **아주 작은 음수**가 된다. 그것을 그대로 주기로 나누면 `−1e−15 % p` → `+p − 1e−15` 로 **한 주기 끝**으로 튀어 색이 뒤집힌다
        /// (자기 검사가 «가로 줄무늬는 y 를 안 탄다»·«왼쪽 아래도 같은 띠» 두 칸에서 실제로 잡았다). 그래서 **주기 위끝을 0 으로 당겨** 맞춘다.
        /// </summary>
        public static bool IsInk(double x, double y, double angleDeg, double period, double dash, double phase)
        {
            if (period <= 0) return false;
            double ax, ay;
            Axis(angleDeg, out ax, out ay);
            double t = x * ax + y * ay - phase;
            double m = t % period;
            if (m < 0) m += period;
            if (m >= period - Eps * period) m = 0;      // 위끝은 곧 아래끝이다(부동소수 되돌림)
            return m < dash;
        }

        /// <summary>주기에 견준 되돌림 오차 한계 — 각도 코사인의 0 이 정확히 0 이 아닌 만큼만 본다.</summary>
        const double Eps = 1e-12;

        /// <summary>
        /// 한 주기를 굽는 판의 가로 크기(캔버스 px). 축이 기울면 **가로축으로 환산한 주기**가 늘어난다 —
        /// 정본이 `.bw-hazard` 에 `background-size: 1.556rem`(= 1.1 × √2)을 적어 둔 그 수다(주기 ÷ |sinθ|).
        /// 가로 줄무늬(θ = 90deg)면 주기 그대로, 세로에 가까우면(|sinθ| → 0) 타일이 무한히 넓어지므로 <paramref name="cap"/> 에서 멎는다.
        /// </summary>
        public static double TileWidth(double angleDeg, double period, double cap)
        {
            double ax, ay;
            Axis(angleDeg, out ax, out ay);
            double s = Math.Abs(ax);
            if (s < 1e-6) return cap;
            double w = period / s;
            return w > cap ? cap : w;
        }

        /// <summary>한 주기 판을 가로로 몇 번 되풀이해 그 폭을 채우나(타일 수 · 1 이상).</summary>
        public static int TileCount(double widthPx, double tileWidthPx)
        {
            if (tileWidthPx <= 0 || widthPx <= 0) return 1;
            int n = (int)Math.Ceiling(widthPx / tileWidthPx);
            return n < 1 ? 1 : n;
        }

        /// <summary>정본 `%` 대시(`A 0 50%`)를 주기 안의 px 로. 0~1 비율을 받는다.</summary>
        public static double DashFromRatio(double period, double ratio)
        {
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;
            return period * ratio;
        }

        /// <summary>
        /// 흐르는 줄무늬(`@keyframes bwhazard { from background-position 0 → to 한 타일 }`)의 그 순간 위상(캔버스 px).
        /// <paramref name="t"/> 는 흐른 시간(초) · <paramref name="dur"/> 는 한 바퀴(초) · <paramref name="tileW"/> 는 한 타일 가로.
        /// 축 위 자리로 바꿔 돌려주므로 <see cref="IsInk"/> 의 phase 로 그대로 넣는다.
        /// </summary>
        public static double ScrollPhase(double t, double dur, double tileW, double angleDeg)
        {
            if (dur <= 0 || tileW <= 0) return 0;
            double k = t / dur;
            k -= Math.Floor(k);
            double ax, ay;
            Axis(angleDeg, out ax, out ay);
            return k * tileW * Math.Abs(ax);
        }
    }
}
