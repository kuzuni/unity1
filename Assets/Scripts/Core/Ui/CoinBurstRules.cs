using System;
using System.Collections.Generic;
using Forge.Core.CraftFx;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>
    /// 판매 코인 분출(정본 `ui.js coinBurst` · T117)의 **수치표** — `Resources/CoinBurstUi.json` 의 layout·fly_y·amt 절을 강타입으로.
    /// 값은 전부 표에서 온다(§1) · 길이 단위: `_rem` = 정본 rem · `_px` = 정본 절대 CSS px(기준 캔버스로는 `anvil_fx_px` 배율 · T87 결정 222) · `_ms` = 벽시계.
    /// </summary>
    public sealed class CoinBurstSpec
    {
        public double ColGapRem, RowGapRem, Row0Rem, JitterRem;
        public double RiseMinPx, RiseMaxPx;
        public double DelayStepMs, DelayJitterMs;
        public double FlyMs, LandK, AmtMs, RemoveSlackMs, AmtSlackMs;
        public double CoinRem, SpinMs, BounceRem, AmtFontRem, AmtOutline, OriginYF;
        public double CountBase, CountLogK, CountMin, CountMax;
        public int RowsUpTo1, RowsUpTo2;
        /// <summary>날아가는 세로 키프레임(정본 `@keyframes coinFlyY`): 퍼센트 · 자리 기호(0 · rise · drop · drop-bounce) · 불투명도 · 구간 이징.</summary>
        public KeyStop[] FlyY;
        /// <summary>착지 금액 키프레임(`@keyframes coinAmt`): 퍼센트 · 불투명도 · translateY(%) · scale · 구간 이징.</summary>
        public KeyStop[] Amt;

        public sealed class KeyStop
        {
            public double At;
            public string Y;          // fly_y 만: "0" · "rise" · "drop" · "drop-bounce"
            public double Opacity, TranslateYPct, Scale;
            public CssEase Ease;      // 이 키에서 다음 키까지의 타이밍 함수(마지막 키는 안 쓴다)
        }

        public static CoinBurstSpec From(JsonObject root)
        {
            var L = J.Obj(J.Require(root, "layout"));
            var s = new CoinBurstSpec
            {
                ColGapRem = J.Num(J.Require(L, "col_gap_rem")), RowGapRem = J.Num(J.Require(L, "row_gap_rem")), Row0Rem = J.Num(J.Require(L, "row0_rem")), JitterRem = J.Num(J.Require(L, "jitter_rem")),
                RiseMinPx = J.Num(J.Require(L, "rise_min_px")), RiseMaxPx = J.Num(J.Require(L, "rise_max_px")),
                DelayStepMs = J.Num(J.Require(L, "delay_step_ms")), DelayJitterMs = J.Num(J.Require(L, "delay_jitter_ms")),
                FlyMs = J.Num(J.Require(L, "fly_ms")), LandK = J.Num(J.Require(L, "land_k")), AmtMs = J.Num(J.Require(L, "amt_ms")),
                RemoveSlackMs = J.Num(J.Require(L, "remove_slack_ms")), AmtSlackMs = J.Num(J.Require(L, "amt_slack_ms")),
                CoinRem = J.Num(J.Require(L, "coin_rem")), SpinMs = J.Num(J.Require(L, "spin_ms")), BounceRem = J.Num(J.Require(L, "bounce_rem")),
                AmtFontRem = J.Num(J.Require(L, "amt_font_rem")), AmtOutline = J.Num(J.Require(L, "amt_outline_f")), OriginYF = J.Num(J.Require(L, "origin_y_f")),
                CountBase = J.Num(J.Require(L, "count_base_n")), CountLogK = J.Num(J.Require(L, "count_log_k")), CountMin = J.Num(J.Require(L, "count_min_n")), CountMax = J.Num(J.Require(L, "count_max_n")),
                RowsUpTo1 = J.Int(J.Require(L, "rows_upto1_n")), RowsUpTo2 = J.Int(J.Require(L, "rows_upto2_n")),
            };
            s.FlyY = Stops(J.Require(root, "fly_y"), true);
            s.Amt = Stops(J.Require(root, "amt"), false);
            return s;
        }

        static KeyStop[] Stops(object arr, bool fly)
        {
            var list = J.List(arr, x => J.Obj(x));
            var outp = new KeyStop[list.Count];
            double prev = -1;
            for (int i = 0; i < list.Count; i++)
            {
                var o = list[i];
                var k = new KeyStop { At = J.Num(J.Require(o, "at")) };
                if (k.At < prev) throw new FormatException("CoinBurstUi 키프레임 퍼센트는 오름차순이어야 한다");
                prev = k.At;
                k.Opacity = J.Num(J.Require(o, "opacity"));
                if (fly) k.Y = J.Str(J.Require(o, "y"));
                else { k.TranslateYPct = J.Num(J.Require(o, "ty_pct")); k.Scale = J.Num(J.Require(o, "scale")); }
                object e;
                if (o.TryGet("ease", out e) && e != null)
                {
                    var v = J.NumArr(e);
                    if (v.Length != 4) throw new FormatException("ease 는 cubic-bezier 네 수여야 한다");
                    k.Ease = new CssEase(v[0], v[1], v[2], v[3]);
                }
                else k.Ease = CssEase.Linear;
                outp[i] = k;
            }
            if (outp.Length < 2) throw new FormatException("키프레임이 둘 미만이다");
            return outp;
        }
    }

    /// <summary>코인 조각 하나의 궤적 값(정본 `coinBurst` 의 per-i 계산 결과 · 길이는 기준 캔버스 px · 시간은 ms).</summary>
    public struct CoinPiece
    {
        public int Index, Row, Col, ColsInRow;
        public double Dx, Rise, Drop, DelayMs;
    }

    /// <summary>
    /// 정본 `UI.coinBurst(total)` 의 셈(ui.js 3478~3560 · UnityEngine 참조 0): 개수 `clamp(3 + round(log10(max(1,총액))·1.7), 3, 10)` ·
    /// 라벨은 **전부 `max(1, round(총액/개수))` 같은 값**(`sell-coin-split-rising`) · 착지점은 줄×칸 격자(i 를 줄에 라운드로빈) ·
    /// 떠오름·지연은 난수. 반올림은 JS `Math.round`(.5 를 올림)다 — C# `Math.Round` 의 은행가 반올림이 아니다.
    /// </summary>
    public static class CoinBurstRules
    {
        /// <summary>JS `Math.round` — 음수가 아닌 값에서 .5 는 올린다.</summary>
        public static double JsRound(double x) { return Math.Floor(x + 0.5); }

        /// <summary>정본 첫 줄 `total = floor(Number(total)||0)` — 0 이하면 연출 없음(개수 0).</summary>
        public static double Total(double total)
        {
            if (double.IsNaN(total) || double.IsInfinity(total)) return 0;
            return Math.Floor(total);
        }

        public static int Count(CoinBurstSpec s, double total)
        {
            total = Total(total);
            if (total <= 0) return 0;
            double n = s.CountBase + JsRound(Math.Log10(Math.Max(1, total)) * s.CountLogK);
            return (int)Math.Min(s.CountMax, Math.Max(s.CountMin, n));
        }

        /// <summary>착지 자리마다 뜨는 값 — 합÷개수로 전부 같다(최소 1).</summary>
        public static double Per(double total, int n)
        {
            total = Total(total);
            if (n <= 0) return 0;
            return Math.Max(1, JsRound(total / n));
        }

        public static int Rows(CoinBurstSpec s, int n) { return n <= s.RowsUpTo1 ? 1 : (n <= s.RowsUpTo2 ? 2 : 3); }

        /// <summary>그 줄에 서는 칸 수 — `ceil((n − r) / rows)`.</summary>
        public static int ColsIn(int n, int rows, int r) { return (int)Math.Ceiling((n - r) / (double)rows); }

        /// <summary>
        /// 조각 n 개의 자리·지연. <paramref name="rem"/> = 기준 캔버스 px 로 환산한 1rem · <paramref name="cssPx"/> = 정본 절대 CSS px 1 의 기준 캔버스 px(`anvil_fx_px`) ·
        /// <paramref name="rand"/>(a, b) = 정본 `U.rand` (a 이상 b 미만).
        /// </summary>
        public static CoinPiece[] Layout(CoinBurstSpec s, double total, double rem, double cssPx, Func<double, double, double> rand)
        {
            int n = Count(s, total);
            var outp = new CoinPiece[n];
            if (n == 0) return outp;
            int rows = Rows(s, n);
            double colGap = s.ColGapRem * rem, rowGap = s.RowGapRem * rem, row0 = s.Row0Rem * rem;
            for (int i = 0; i < n; i++)
            {
                int row = i % rows, col = i / rows, cols = ColsIn(n, rows, row);
                double span = colGap * (cols - 1) / 2.0;
                double dx = (cols <= 1 ? 0 : -span + col * colGap) + rand(-s.JitterRem, s.JitterRem) * rem;
                double rise = -rand(s.RiseMinPx, s.RiseMaxPx) * cssPx;
                double drop = row0 + row * rowGap;
                double delay = i * s.DelayStepMs + rand(0, s.DelayJitterMs);
                outp[i] = new CoinPiece { Index = i, Row = row, Col = col, ColsInRow = cols, Dx = dx, Rise = rise, Drop = drop, DelayMs = delay };
            }
            return outp;
        }

        /// <summary>착지 순간(ms · 조각 시작 기준이 아니라 분출 시작 기준) — `delay + FLY·LAND_K`.</summary>
        public static double LandMs(CoinBurstSpec s, CoinPiece p) { return p.DelayMs + s.FlyMs * s.LandK; }
        /// <summary>조각을 걷는 시각 — `FLY + delay + 120`.</summary>
        public static double PieceEndMs(CoinBurstSpec s, CoinPiece p) { return s.FlyMs + p.DelayMs + s.RemoveSlackMs; }
        /// <summary>금액 라벨을 걷는 시각(착지 기준) — `AMT + 60`.</summary>
        public static double AmtEndMs(CoinBurstSpec s) { return s.AmtMs + s.AmtSlackMs; }

        /// <summary>구간별 타이밍 함수로 키 사이를 보간한다(CSS 는 키마다 `animation-timing-function` 을 다시 건다).</summary>
        static void Segment(CoinBurstSpec.KeyStop[] keys, double percent, out int i0, out double e)
        {
            if (percent <= keys[0].At) { i0 = 0; e = 0; return; }
            int last = keys.Length - 1;
            if (percent >= keys[last].At) { i0 = last - 1; e = 1; return; }
            int i = 0;
            while (i < last - 1 && percent >= keys[i + 1].At) i++;
            double span = keys[i + 1].At - keys[i].At;
            double u = span <= 0 ? 1 : (percent - keys[i].At) / span;
            i0 = i; e = keys[i].Ease.Ease(u);
        }

        static double YOf(CoinBurstSpec.KeyStop k, double rise, double drop, double bounce)
        {
            switch (k.Y)
            {
                case "0": return 0;
                case "rise": return rise;
                case "drop": return drop;
                case "drop-bounce": return drop - bounce;
                default: throw new FormatException("fly_y 자리 기호를 모른다: " + k.Y);
            }
        }

        /// <summary>날아가는 조각의 세로 오프셋(기준 px · 아래가 +)과 불투명도 — 퍼센트 0~100(`forwards`).</summary>
        public static void FlyY(CoinBurstSpec s, CoinPiece p, double bounce, double percent, out double y, out double opacity)
        {
            int i; double e;
            Segment(s.FlyY, percent, out i, out e);
            var a = s.FlyY[i]; var b = s.FlyY[i + 1];
            double ya = YOf(a, p.Rise, p.Drop, bounce), yb = YOf(b, p.Rise, p.Drop, bounce);
            y = ya + (yb - ya) * e;
            opacity = a.Opacity + (b.Opacity - a.Opacity) * e;
        }

        /// <summary>착지 금액 라벨의 불투명도 · translateY(자기 높이 %) · scale — 퍼센트 0~100.</summary>
        public static void Amt(CoinBurstSpec s, double percent, out double opacity, out double translateYPct, out double scale)
        {
            int i; double e;
            Segment(s.Amt, percent, out i, out e);
            var a = s.Amt[i]; var b = s.Amt[i + 1];
            opacity = a.Opacity + (b.Opacity - a.Opacity) * e;
            translateYPct = a.TranslateYPct + (b.TranslateYPct - a.TranslateYPct) * e;
            scale = a.Scale + (b.Scale - a.Scale) * e;
        }
    }
}
