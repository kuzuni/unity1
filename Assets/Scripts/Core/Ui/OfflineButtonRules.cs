using System;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>
    /// 메인 오프라인 보상 버튼 표(ROUTINE T133 · <c>Resources/OfflineButtonUi.json</c>): 정본 <c>style.css</c> 204~246 의 치수와
    /// <c>@keyframes ob-bob</c>·<c>ob-zzz</c> 구간을 그대로 담는다. 값은 전부 표에서 온다(§1) · <c>_rem</c> = 정본 rem ·
    /// <c>_f</c> = 버튼 안 비율 · <c>_ms</c> = 벽시계.
    /// </summary>
    public sealed class OfflineButtonSpec
    {
        public double BtnRem, LeftRem, BottomRem, ChestShadowDyRem, PressDyRem, PressScale, ReadySec;
        public double BobMs, BobDyRem, BobScale;
        public double ZzzLeftF, ZzzBottomF, ZzzFontRem, ZzzLinePx, ZzzMs, ZzzGapMs;
        public int ZzzCount;
        public double ZzzEndDxRem, ZzzEndDyRem, ZzzFromScale, ZzzToScale, ZzzFromRot, ZzzToRot, ZzzInF, ZzzOutF, TickMs;

        public static OfflineButtonSpec From(JsonObject root)
        {
            JsonObject L = J.Obj(J.Require(root, "layout"));
            Func<string, double> n = k => J.Num(J.Require(L, k));
            OfflineButtonSpec s = new OfflineButtonSpec
            {
                BtnRem = n("btn_rem"), LeftRem = n("left_rem"), BottomRem = n("bottom_rem"),
                ChestShadowDyRem = n("chest_shadow_dy_rem"), PressDyRem = n("press_dy_rem"), PressScale = n("press_scale"),
                ReadySec = n("ready_sec"), BobMs = n("bob_ms"), BobDyRem = n("bob_dy_rem"), BobScale = n("bob_scale"),
                ZzzLeftF = n("zzz_left_f"), ZzzBottomF = n("zzz_bottom_f"), ZzzFontRem = n("zzz_font_rem"),
                ZzzLinePx = n("zzz_line_px"), ZzzMs = n("zzz_ms"), ZzzGapMs = n("zzz_gap_ms"), ZzzCount = (int)n("zzz_count"),
                ZzzEndDxRem = n("zzz_end_dx_rem"), ZzzEndDyRem = n("zzz_end_dy_rem"),
                ZzzFromScale = n("zzz_from_scale"), ZzzToScale = n("zzz_to_scale"),
                ZzzFromRot = n("zzz_from_rot"), ZzzToRot = n("zzz_to_rot"),
                ZzzInF = n("zzz_in_f"), ZzzOutF = n("zzz_out_f"), TickMs = n("tick_ms"),
            };
            if (s.BtnRem <= 0 || s.BobMs <= 0 || s.ZzzMs <= 0 || s.TickMs <= 0)
                throw new FormatException("OfflineButtonUi: btn_rem·bob_ms·zzz_ms·tick_ms 는 양수여야 한다");
            if (s.ZzzCount <= 0) throw new FormatException("OfflineButtonUi: zzz_count 는 1 이상이어야 한다");
            if (s.ZzzInF <= 0 || s.ZzzInF >= s.ZzzOutF || s.ZzzOutF >= 1)
                throw new FormatException("OfflineButtonUi: 0 < zzz_in_f < zzz_out_f < 1 이어야 한다 (정본 16% → 70%)");
            if (s.ReadySec < 0) throw new FormatException("OfflineButtonUi: ready_sec 는 0 이상이어야 한다");
            return s;
        }
    }

    /// <summary>글자 한 개(<c>z</c>)의 한 순간 — 정본 <c>@keyframes ob-zzz</c> 의 보간 결과.</summary>
    public struct ZzzFrame
    {
        public double Alpha, DxRem, DyRem, Scale, RotDeg;
    }

    /// <summary>
    /// 정본 <c>ui.js</c> 6085(<c>ready</c> 토글) · <c>style.css</c> 218~241(<c>ob-bob</c>·<c>ob-zzz</c>) 의 셈.
    /// UnityEngine 참조 0 — <c>dotnet test</c> 가 정본 값과 바로 대조한다.
    /// </summary>
    public static class OfflineButtonRules
    {
        /// <summary>정본 6085 — <c>(now − lastOfflineClaim) / 1000 &gt;= 60</c> 이면 상자가 들썩인다.</summary>
        public static bool Ready(OfflineButtonSpec s, double nowMs, double lastClaimMs)
        {
            return (nowMs - lastClaimMs) / 1000.0 >= s.ReadySec;
        }

        /// <summary>틱이 왔는가 — 정본은 1초 틱에서 ready 를 다시 본다.</summary>
        public static bool TickDue(OfflineButtonSpec s, double sinceMs) { return sinceMs >= s.TickMs; }

        /// <summary>
        /// <c>@keyframes ob-bob</c>: <c>0%,100% translateY(0) scale(1)</c> · <c>50% translateY(−.14rem) scale(1.04)</c> ·
        /// <c>ease-in-out</c> 무한. 0↔1 을 오가는 삼각파에 ease-in-out(정본 CSS 기본 <c>ease-in-out</c> = smoothstep 근사)을 먹인다.
        /// </summary>
        public static void Bob(OfflineButtonSpec s, double tMs, out double dyRem, out double scale)
        {
            double p = Wrap(tMs / s.BobMs);            // 0~1 한 바퀴
            double tri = p <= 0.5 ? p * 2.0 : (1.0 - p) * 2.0;   // 0 → 1 → 0
            double e = EaseInOut(tri);
            dyRem = -s.BobDyRem * e;                    // 위로(음수 = 화면 위)
            scale = 1.0 + (s.BobScale - 1.0) * e;
        }

        /// <summary>글자 i 의 시작 지연 — 정본 <c>nth-child(2) .9s</c> · <c>nth-child(3) 1.8s</c>.</summary>
        public static double DelayMs(OfflineButtonSpec s, int i) { return s.ZzzGapMs * i; }

        /// <summary>
        /// <c>@keyframes ob-zzz</c>(2.7s ease-out 무한): <c>0%</c> α0 · (0,0) · scale .55 · rot −10° →
        /// <c>16%</c> α1 → <c>70%</c> α1 → <c>100%</c> α0 · (.75rem, −1.7rem) · scale 1.25 · rot 12°.
        /// 자리·크기·회전은 **한 바퀴 전체**에 걸쳐 가고(정본도 0%→100% 한 구간이다) α만 네 구간으로 꺾인다.
        /// </summary>
        public static ZzzFrame Zzz(OfflineButtonSpec s, double tMs, int index)
        {
            double p = Wrap((tMs - DelayMs(s, index)) / s.ZzzMs);
            double e = EaseOut(p);                                    // ease-out — 처음이 빠르고 끝이 느리다
            double a;
            if (p < s.ZzzInF) a = p / s.ZzzInF;                        // 0 → 1
            else if (p < s.ZzzOutF) a = 1.0;                           // 머문다
            else a = (1.0 - p) / (1.0 - s.ZzzOutF);                    // 1 → 0
            return new ZzzFrame
            {
                Alpha = Clamp01(a),
                DxRem = s.ZzzEndDxRem * e,
                DyRem = s.ZzzEndDyRem * e,
                Scale = s.ZzzFromScale + (s.ZzzToScale - s.ZzzFromScale) * e,
                RotDeg = s.ZzzFromRot + (s.ZzzToRot - s.ZzzFromRot) * e,
            };
        }

        /// <summary>음수 시각(지연 전)도 한 바퀴 안으로 접는다 — JS <c>animation-delay</c> 는 지연 동안 0% 를 유지한다.</summary>
        static double Wrap(double p)
        {
            if (p < 0) return 0;
            return p - Math.Floor(p);
        }

        static double Clamp01(double v) { return v < 0 ? 0 : (v > 1 ? 1 : v); }

        /// <summary>CSS <c>ease-in-out</c> 근사(smoothstep) — 양 끝이 느리고 가운데가 빠르다.</summary>
        static double EaseInOut(double t) { t = Clamp01(t); return t * t * (3.0 - 2.0 * t); }

        /// <summary>CSS <c>ease-out</c> 근사 — 처음이 빠르고 끝이 느리다.</summary>
        static double EaseOut(double t) { t = Clamp01(t); return 1.0 - (1.0 - t) * (1.0 - t); }
    }
}
