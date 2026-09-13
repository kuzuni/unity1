using System;

namespace Forge.Core.CraftFx
{
    /// <summary>
    /// T87 27회차 — 제작 **결과 카드** 계열(정본 `web/css/style.css` 1060~1156)의 키프레임을 글자 그대로 옮긴 표. 엔진 참조 0.
    /// 모루·망치 오버레이(<see cref="AnvilFxSpec"/>·<see cref="AutoForgeFxSpec"/>)와 달리 **마스터 클럭이 따로**다 —
    /// 카드는 두들기기(1500ms)가 끝난 뒤 `done()` 이 띄우는 것이라 제 길이를 쥔다(리빌 560 · 탈락 620 · 카드판 340/180ms).
    ///
    /// 옮기며 지켜야 하는 계약(정본 주석이 못 박은 것):
    ///  · **리빌은 머문다 · 탈락은 빨려 들어간다** — 정본 1091: «탈락 카드와 몸통은 같지만 끝에서 빨려 들어가지 않는다:
    ///    튀어올라 **머문 채** 비교 팝업에 자리를 넘긴다». 그래서 <see cref="Pop"/> 의 끝 불투명도는 1 이고
    ///    <see cref="AutoDrop"/> 의 끝 불투명도는 0 이다. 이 둘을 바꿔 쓰면 리빌 카드가 팝업 뜨기 직전에 사라진다.
    ///  · **세로 이동은 «카드 제 높이의 %»** 다(CSS `translate(-50%, -117%)` 의 두 번째 값은 요소 크기 기준) —
    ///    앱 크기를 따라가므로 `anvil_fx_px`(절대 CSS px 환산 · 함정 ⓒ)를 곱하지 **않는다**. 오버레이 겹과 다른 자리다.
    ///  · **축은 카드 한가운데**(CSS `transform-origin` 기본 50% 50%) — 유니티는 `localScale` 이 피벗을 축으로 도니
    ///    카드 피벗을 (.5,.5) 로 두고 자리는 «가운데» 로 계산한다(함정 ⓑ 와 같은 갈래).
    ///  · **카드판은 카드마다 지연을 주지 않는다** — 정본 1118: «⚠️ 카드마다 animation-delay 를 주지 말 것 —
    ///    그 시차가 곧 사용자가 물린 '1초에 1개씩'이다». 격자 **전체**가 <see cref="BatchPop"/> 하나를 탄다.
    ///  · 광택(<see cref="Sheen"/>)은 **80ms 늦게** 시작하고 58% 에서 끝자리에 닿아 나머지는 머문다.
    /// </summary>
    public static class CraftCardSpec
    {
        /// <summary>`crpop`·`crring`·`crsheen` 의 길이(ms) — `.craft-reveal { animation: crpop .56s … }`. `ForgeHost.RevealCardSec` 와 같아야 한다.</summary>
        public const double RevealMs = 560;

        /// <summary>`adcpop` 의 길이(ms) — `.auto-drop-card { animation: adcpop .62s … }`. `ForgeHost.AutoCardSec` 와 같아야 한다.</summary>
        public const double AutoDropMs = 620;

        /// <summary>`cbpop` 의 길이(ms) — `.cb-grid { animation: cbpop .34s … }`.</summary>
        public const double BatchPopMs = 340;

        /// <summary>`cbfade` 의 길이(ms) — `.craft-batch { animation: cbfade .18s ease-out }`(딤이 깔리는 시간).</summary>
        public const double BatchFadeMs = 180;

        /// <summary>`crsheen` 의 지연(ms) — `animation: crsheen .56s ease-out .08s forwards`.</summary>
        public const double SheenDelayMs = 80;

        /// <summary>정본 `.auto-drop-card` 기본 자세의 세로 이동(%) — 애니가 없을 때 카드가 놓이는 자리(`transform: translate(-50%, -110%)`).</summary>
        public const double RestTranslateYPct = -110;

        /// <summary>`crring` 이 퍼지는 최대 거리(rem) — `box-shadow: … 0 0 0 1.1rem`.</summary>
        public const double RingSpreadRem = 1.1;

        /// <summary>`crring` 시작 색의 불투명도 — `color-mix(in srgb, var(--rc) 72%, transparent)`.</summary>
        public const double RingAlpha0 = 0.72;

        /// <summary>정본 `cubic-bezier(.18,.9,.28,1.06)` — 리빌 카드와 카드판이 같이 쓰는 «튀어오름» 이징(끝에서 1을 넘겼다 돌아온다).</summary>
        public static readonly CssEase Bounce = new CssEase(0.18, 0.9, 0.28, 1.06);

        /// <summary>CSS `ease-out` = `cubic-bezier(0, 0, .58, 1)` — 링·광택·탈락 카드가 쓴다.</summary>
        public static readonly CssEase EaseOut = new CssEase(0, 0, 0.58, 1);

        /// <summary>
        /// `crpop` — 제작 결과 리빌 카드. 채널 = 불투명도 · translateY(카드 높이 %) · scale.
        /// 62% 와 100% 가 **같은 값**이라 마지막 38% 는 머문다(정본이 «머문 채 자리를 넘긴다» 로 적은 그 구간).
        /// </summary>
        public static readonly CssTrack Pop = new CssTrack(
            new double[] { 0, 24, 44, 62, 100 },
            new double[][]
            {
                new double[] { 0, -74, 0.62 },     // 0%   — 모루에 붙은 채 작게 시작
                new double[] { 1, -126, 1.12 },    // 24%  — 가장 높이 튀어 오르며 커진다
                new double[] { 1, -112, 0.97 },    // 44%  — 내려앉으며 살짝 눌린다
                new double[] { 1, -117, 1.00 },    // 62%  — 제자리
                new double[] { 1, -117, 1.00 },    // 100% — 머문다(비교 팝업이 이 자리를 넘겨받는다)
            });

        /// <summary>
        /// `adcpop` — 자동 제련 탈락 카드. 채널은 <see cref="Pop"/> 과 같다.
        /// 정본 주석: «뽑힌 순간 튀어나왔다가, 코인으로 터지기 직전에 모루 쪽으로 빨려 들어간다» — 그래서 끝이 (0, −84%, .82) 다.
        /// </summary>
        public static readonly CssTrack AutoDrop = new CssTrack(
            new double[] { 0, 18, 70, 100 },
            new double[][]
            {
                new double[] { 0, -80, 0.70 },     // 0%
                new double[] { 1, -118, 1.05 },    // 18%  — 튀어나온다
                new double[] { 1, -110, 1.00 },    // 70%  — 기본 자세(-110%)에서 머문다
                new double[] { 0, -84, 0.82 },     // 100% — 모루 쪽으로 빨려 들어가며 사라진다
            });

        /// <summary>`crring` — 시대색 링. 채널 = 퍼진 거리(rem · `box-shadow` spread) · 불투명도.</summary>
        public static readonly CssTrack Ring = new CssTrack(
            new double[] { 0, 100 },
            new double[][]
            {
                new double[] { 0, RingAlpha0 },
                new double[] { RingSpreadRem, 0 },
            });

        /// <summary>
        /// `crsheen` — 광택 쓸림. 채널 = translateX(광택 띠 폭 %). 58% 에서 150% 에 닿고 나머지는 머문다.
        /// 시작 자세(−130%)는 `.craft-reveal::after` 의 기본 `transform` 과 같아야 첫 프레임에 안 튄다.
        /// </summary>
        public static readonly CssTrack Sheen = new CssTrack(
            new double[] { 0, 58, 100 },
            new double[][]
            {
                new double[] { -130 },
                new double[] { 150 },
                new double[] { 150 },
            });

        /// <summary>`cbpop` — 카드판 격자 **전체**. 채널 = 불투명도 · scale.</summary>
        public static readonly CssTrack BatchPop = new CssTrack(
            new double[] { 0, 62, 100 },
            new double[][]
            {
                new double[] { 0, 0.74 },
                new double[] { 1, 1.04 },
                new double[] { 1, 1.00 },
            });

        /// <summary>`cbfade` — 카드판 뒤 딤. 채널 = 불투명도(정본은 `from`/`to` 두 키뿐이다).</summary>
        public static readonly CssTrack BatchFade = new CssTrack(
            new double[] { 0, 100 },
            new double[][]
            {
                new double[] { 0 },
                new double[] { 1 },
            });

        /// <summary>`crsheen` 띠의 기울기 — `linear-gradient(105deg, …)`(CSS 0도 = 위 · 시계방향).</summary>
        public const double SheenAngleDeg = 105;

        /// <summary>띠의 색 정지 위치 — `transparent 38%, rgba(255,255,255,.4) 50%, transparent 62%`.</summary>
        public static readonly double[] SheenStops = { 0.38, 0.50, 0.62 };

        /// <summary>띠 한가운데의 흰색 불투명도(`rgba(255,255,255,.4)`).</summary>
        public const double SheenPeakAlpha = 0.40;

        /// <summary>
        /// 정사각 상자(0~1 · y 는 위가 1)에서 CSS 그라디언트 선의 시작·끝점을 푼다 —
        /// 브라우저는 선 길이를 `|W·sinA| + |H·cosA|` 로 잡아 **모서리가 0/1 에 닿게** 한다(CSS Images 3 §3.4).
        /// </summary>
        public static void SheenAxis(out double fromX, out double fromY, out double toX, out double toY)
        {
            double a = SheenAngleDeg * Math.PI / 180.0;
            double dx = Math.Sin(a), dy = Math.Cos(a);
            double len = Math.Abs(dx) + Math.Abs(dy);
            fromX = 0.5 - dx * len * 0.5; fromY = 0.5 - dy * len * 0.5;
            toX = 0.5 + dx * len * 0.5; toY = 0.5 + dy * len * 0.5;
        }

        /// <summary>광택의 진행(0~1) — 지연 80ms 를 뺀 뒤 <see cref="RevealMs"/> 로 나눈다. 지연 전에는 0(시작 자세).</summary>
        public static double SheenPercent(double ms)
        {
            double t = ms - SheenDelayMs;
            if (t <= 0) return 0;
            double p = t / RevealMs * 100.0;
            return p > 100 ? 100 : p;
        }

        /// <summary>
        /// 카드 **가운데**가 놓일 자리를 «CSS 기준점에서 아래로 잰 y» 로 돌려준다 — `translate(-50%, pct%)` 를 푼 것이다.
        /// 기준점(모루 위 한 점)은 정본 `ui.js` 가 카드의 `left`/`top` 으로 준다.
        /// </summary>
        /// <param name="anchorYDown">기준점 y(위에서 아래로 잰 값).</param>
        /// <param name="pct">세로 이동 퍼센트(정본 값 · 음수가 위쪽).</param>
        /// <param name="cardH">카드 높이.</param>
        public static double CenterYDown(double anchorYDown, double pct, double cardH)
        {
            return anchorYDown + pct / 100.0 * cardH + cardH * 0.5;
        }
    }
}
