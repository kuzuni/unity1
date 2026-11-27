using System;
using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>
    /// T331 — 정본 `box-shadow` **바깥 그림자** 한 자리의 수치(`Resources/ShadowUi.json` 의 `spots` 한 칸).
    ///
    /// CSS 는 `box-shadow: <dx> <dy> <blur> <spread> <color>` 다. 정본의 딱딱한 턱은 `blur`·`spread` 가 0 이라
    /// **같은 모양을 한 겹 뒤에 깔고 내리기만** 하면 화면이 같아진다 — 구울 것이 없다(흐린 자리는 굽는 길이 따로 필요하다).
    ///
    /// ⚠ 부호: CSS 의 +y 는 **아래**고 UGUI 의 +y 는 **위**다. 이 표는 **정본 그대로**(아래가 양수) 담고,
    ///    화면에 거는 쪽(`UiShadow`)이 한 자리에서 뒤집는다 — 표를 뒤집어 담으면 정본과 대조할 때마다 헷갈린다.
    /// UnityEngine 참조 0.
    /// </summary>
    public struct ShadowSpec
    {
        /// <summary>정본 좌표의 치우침(rem · +y 는 **아래**).</summary>
        public double DxRem, DyRem;
        /// <summary>흐림·번짐(rem · 딱딱한 턱은 둘 다 0).</summary>
        public double BlurRem, SpreadRem;
        /// <summary>색(0~1).</summary>
        public double R, G, B, A;

        /// <summary>흐림도 번짐도 없는가 — 그러면 같은 모양 한 겹으로 끝난다(굽지 않는다).</summary>
        public bool IsHard { get { return BlurRem <= 0 && SpreadRem <= 0; } }
    }

    /// <summary>정본 그림자 자리 표 — 키는 자(`tools/check_box_shadows.py`)가 세는 키와 같다.</summary>
    public sealed class ShadowTable
    {
        readonly Dictionary<string, ShadowSpec> spots = new Dictionary<string, ShadowSpec>(StringComparer.Ordinal);

        public int Count { get { return spots.Count; } }
        public IEnumerable<string> Keys { get { return spots.Keys; } }

        public static ShadowTable From(JsonObject root)
        {
            JsonObject s = J.Obj(J.Require(root, "spots"));
            var t = new ShadowTable();
            foreach (var kv in s)
            {
                JsonObject o = J.Obj(kv.Value);
                double[] rgba = J.NumArr(J.Require(o, "rgba"));
                if (rgba == null || rgba.Length != 4) throw new FormatException("ShadowUi «" + kv.Key + "»: rgba 는 넷이어야 한다");
                foreach (double c in rgba) if (c < 0 || c > 1) throw new FormatException("ShadowUi «" + kv.Key + "»: rgba 는 0~1 이다(CSS 의 0~255 를 그대로 옮기지 마라)");
                var sp = new ShadowSpec
                {
                    DxRem = J.Num(J.Require(o, "dx_rem")),
                    DyRem = J.Num(J.Require(o, "dy_rem")),
                    BlurRem = J.Num(J.Require(o, "blur_rem")),
                    SpreadRem = J.Num(J.Require(o, "spread_rem")),
                    R = rgba[0], G = rgba[1], B = rgba[2], A = rgba[3],
                };
                // 번짐(`spread`)은 **음수가 될 수 있다** — 정본 `.pass-card` 가 `-.5rem` 으로 그늘을 안으로 줄인다.
                // 흐림(`blur`)만 음수를 막는다(CSS 도 막는다).
                if (sp.BlurRem < 0) throw new FormatException("ShadowUi «" + kv.Key + "»: blur 는 음수가 아니다");
                if (sp.DxRem == 0 && sp.DyRem == 0 && sp.IsHard) throw new FormatException("ShadowUi «" + kv.Key + "»: 치우침도 흐림도 0 이면 그림자가 아니다");
                t.spots[kv.Key] = sp;
            }
            if (t.spots.Count == 0) throw new FormatException("ShadowUi: spots 가 비었다");
            return t;
        }

        public bool Has(string key) { return spots.ContainsKey(key); }

        public ShadowSpec Get(string key)
        {
            ShadowSpec s;
            if (!spots.TryGetValue(key, out s)) throw new KeyNotFoundException("ShadowUi.json 의 «spots» 에 «" + key + "» 이 없다");
            return s;
        }

        /// <summary>화면 좌표(UGUI)의 치우침 — CSS 의 아래(+y)를 여기서 한 번만 뒤집는다.</summary>
        public void OffsetPx(string key, double remPx, out double x, out double y)
        {
            ShadowSpec s = Get(key);
            x = s.DxRem * remPx;
            y = -s.DyRem * remPx;
        }

        /// <summary>CSS `blur` 반지름 → 가우시안 표준편차. CSS 규격이 «반지름의 절반» 으로 못 박은 값이다.</summary>
        public static double SigmaOf(double blurPx) { return blurPx * 0.5; }

        /// <summary>
        /// 둥근 네모의 **부호 있는 거리**(밖이 양수 · 안이 음수) — 그림자를 굽는 자리마다 이 거리로 흐림을 푼다.
        /// </summary>
        /// <param name="px">상자 한가운데를 원점으로 한 자리.</param>
        /// <param name="halfW">반너비(번짐 `spread` 를 이미 더한 값).</param>
        /// <param name="radius">모서리 반지름(반너비·반높이보다 클 수 없다).</param>
        public static double RoundRectDistance(double px, double py, double halfW, double halfH, double radius)
        {
            double r = Math.Min(radius, Math.Min(halfW, halfH));
            double qx = Math.Abs(px) - halfW + r, qy = Math.Abs(py) - halfH + r;
            double outX = qx > 0 ? qx : 0, outY = qy > 0 ? qy : 0;
            double inner = Math.Min(Math.Max(qx, qy), 0);
            return inner + Math.Sqrt(outX * outX + outY * outY) - r;
        }

        /// <summary>
        /// 거리 <paramref name="dist"/>(밖이 양수)에서 흐린 그림자의 덮는 정도(0~1).
        ///
        /// 곧은 모서리를 가우시안으로 번지게 하면 정확히 **정규분포의 누적**이 된다 — 모서리 위(거리 0)에서 .5 이고
        /// 안으로 갈수록 1, 밖으로 갈수록 0. 굽는 자리는 이 값에 색 알파를 곱하면 된다. σ 가 0이면 계단(딱딱한 턱)이다.
        /// </summary>
        public static double EdgeCoverage(double dist, double sigma)
        {
            if (sigma <= 0) return dist <= 0 ? 1 : 0;
            return 0.5 * Erfc(dist / (sigma * Math.Sqrt(2.0)));
        }

        /// <summary>T511 2회차 — 타원 번짐을 구울 때 세로로 적분할 칸(실측: 32칸이면 참값과 최대 3e-3).</summary>
        public const int GlowSamples = 32;

        /// <summary>
        /// T511 2회차 — **타원을 가우시안으로 번지게 한 덮임**(0~1). `box-shadow` 의 흐림을 타원 모양에 먹인 바로 그 값이다.
        ///
        /// 왜 <see cref="EdgeCoverage"/> 로는 안 되나 — 그 자는 «부호 거리 → 정규분포 누적» 인데 그것은 **곧은 턱에만 정확**하다.
        /// 타원은 턱이 휘어 있고, 가장 심한 곳은 **장축 끝**이다(그 자리의 곡률 반지름은 `b²/a` — 소환진에서 **1.76rem** 이라
        /// σ 1.8rem 보다 작다). 실측: 장축 끝에서 참값 **0.3565** 인데 «거리+erfc» 는 **0.5** 로 — **0.157** 어긋난다.
        /// 단축 끝(곡률 반지름 `a²/b` = 27.5rem · 거의 곧다)에서는 0.013 뿐이다. 판 전체로는 모양을 타원 거리로
        /// 바로 맞춰도 최대 22~36%, 지금 굽는 길(둥근 네모 거리)은 34~39% 어긋난다. 그래서 거리로 풀지 않고 **적분한다**.
        ///
        /// 셈: 한 **가로줄**(y 고정)에서 타원은 `x ∈ [-xr, xr]` 널판이라 가로 번짐이 **erf 차로 딱 풀린다**.
        /// 그 널판을 세로로 가우시안 가중해 더하면 2차원 합성이다 —
        /// `G(x,y) = ∫ g(v)·[Φ((xr(y+v) − x)/σ) − Φ((−xr(y+v) − x)/σ)] dv`, `g` 는 표준편차 σ 의 정규분포.
        /// ⚑ **적분 구간을 `|y| &lt; b` 로 자른다** — 거기가 널판 폭이 0 으로 꺾이는 **턱**이고, 턱을 칸 안에 두면
        /// 가운뎃점 규칙이 느리게·들쭉날쭉 수렴한다(실측 — 고른 48칸 1.6e-2 ↔ 자른 24칸 4.0e-3 · 자른 16칸이 고른 96칸보다 낫다).
        /// </summary>
        /// <param name="px">타원 가운데를 원점으로 한 자리(단위는 <paramref name="a"/> 와 같다).</param>
        /// <param name="py">같은 원점의 세로 자리(부호는 부르는 쪽이 정한다 — 타원은 위아래 대칭이라 셈은 안 바뀐다).</param>
        /// <param name="a">타원 반너비(번짐 `spread` 를 이미 더한 값) · <paramref name="b"/> 반높이.</param>
        /// <param name="sigma">가우시안 표준편차 = 흐림의 절반(<see cref="SigmaOf"/>).</param>
        /// <param name="samples">세로 적분 칸(0 이하면 <see cref="GlowSamples"/>).</param>
        public static double EllipseCoverage(double px, double py, double a, double b, double sigma, int samples = 0)
        {
            if (a <= 0 || b <= 0) return 0;
            if (sigma <= 0) return (px * px) / (a * a) + (py * py) / (b * b) <= 1 ? 1 : 0;
            int n = samples > 0 ? samples : GlowSamples;
            double lo = Math.Max(-3.5 * sigma, -b - py), hi = Math.Min(3.5 * sigma, b - py);
            if (hi <= lo) return 0;
            double h = (hi - lo) / n, sum = 0;
            for (int i = 0; i < n; i++)
            {
                double v = lo + h * (i + 0.5);
                double y = py + v;
                double t = 1 - (y / b) * (y / b);
                double xr = a * Math.Sqrt(t > 0 ? t : 0);
                sum += Math.Exp(-0.5 * (v / sigma) * (v / sigma))
                       * (Normal((xr - px) / sigma) - Normal((-xr - px) / sigma)) * h;
            }
            return sum / (sigma * Math.Sqrt(2 * Math.PI));
        }

        /// <summary>
        /// T511 2회차 — 정본 `.sr-floor::before` 의 **그림자 겹 알파**(색 알파 1 기준 · 0~1).
        ///
        /// 한 선택자가 `box-shadow` 를 **둘** 적었다 — `inset 0 0 &lt;안쪽흐림&gt;`(테 안에 고인 빛)과
        /// `0 &lt;dy&gt; &lt;흐림&gt; &lt;번짐&gt;`(테 밖으로 번지는 빛). 둘은 **겹치지 않는다**(CSS 규격 — 안쪽 그림자는 테 안에,
        /// 바깥 그림자는 테 밖에만 그린다). 그래서 한 판에 같이 구울 수 있다.
        ///
        /// 안쪽 겹은 «모양 **바깥**을 번지게 해 모양 안에 자른 것» 이라 `1 − 덮임`(번짐 0 · 치우침 0)이고,
        /// 바깥 겹은 «번짐으로 넓히고 `dy` 로 내린 모양의 덮임» 을 모양 밖에 자른 것이다.
        /// </summary>
        /// <param name="px">테 상자(타원) 가운데를 원점으로 한 자리 · <paramref name="py"/> 는 **CSS 쪽 부호**(아래가 양수)로 준다.</param>
        /// <param name="a">테 상자 반너비 · <paramref name="b"/> 반높이.</param>
        /// <param name="insetSigma">안쪽 겹 σ(= 안쪽 흐림의 절반) · <paramref name="outerSigma"/> 바깥 겹 σ.</param>
        /// <param name="spread">바깥 겹 번짐(모양을 이만큼 넓힌다) · <paramref name="dy"/> 바깥 겹 치우침(CSS 부호 · 양수면 아래).</param>
        public static double EllipseShadowAlpha(double px, double py, double a, double b,
                                                double insetSigma, double outerSigma, double spread, double dy,
                                                int samples = 0)
        {
            if (a <= 0 || b <= 0) return 0;
            if ((px * px) / (a * a) + (py * py) / (b * b) <= 1)
                return 1 - EllipseCoverage(px, py, a, b, insetSigma, samples);
            return EllipseCoverage(px, py - dy, a + spread, b + spread, outerSigma, samples);
        }

        /// <summary>정규분포의 누적 `Φ` — <see cref="Erfc"/> 로 낸다(따로 급수를 두지 않는다).</summary>
        public static double Normal(double z) { return 0.5 * Erfc(-z / Math.Sqrt(2.0)); }

        /// <summary>여 오차함수 `erfc` — Abramowitz &amp; Stegun 7.1.26(오차 1.5e-7 아래).</summary>
        public static double Erfc(double x)
        {
            double z = Math.Abs(x);
            double t = 1.0 / (1.0 + 0.5 * z);
            double y = t * Math.Exp(-z * z - 1.26551223 + t * (1.00002368 + t * (0.37409196 + t * (0.09678418 +
                t * (-0.18628806 + t * (0.27886807 + t * (-1.13520398 + t * (1.48851587 +
                t * (-0.82215223 + t * 0.17087277)))))))));
            return x >= 0 ? y : 2.0 - y;
        }
    }
}
