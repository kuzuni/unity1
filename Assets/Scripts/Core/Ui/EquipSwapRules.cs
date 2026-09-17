using System;
using System.Collections.Generic;
using Forge.Core.CraftFx;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>
    /// 장비 교체 «던져내기»(정본 `ui.js` `EQSW_*` · `playEquipSwapFx` · css `@keyframes eqswX/eqswY/eqswR/eqswSquash/eqswDust/eqswSnap` · T118)의 **수치표** —
    /// `Resources/EquipSwapUi.json` 의 layout 절과 키프레임 여섯을 강타입으로. 값은 전부 표에서 온다(§1) · 단위: `_rem` = 정본 rem · `_px` = 정본 절대 CSS px(기준 캔버스는 `anvil_fx_px` 배율) · `_ms` = 벽시계.
    /// </summary>
    public sealed class EquipSwapSpec
    {
        public double FlyMs, LandK, SnapMs, SnapDelayMs, Shrink;
        public double TiltMinDeg, TiltMaxDeg, TurnDeg;
        public int TurnsMin, TurnsMax;
        public double ReachWF, ReachHF, ReachPadPx;
        public double LandDistMinW, LandDistMaxW, DropMinHF, RiseMinHF, RiseMaxHF;
        public double RemoveSlackMs, DustMs, DustRemoveMs, DustWF, DustHF, DustYF;
        public double BounceBigRem, BounceSmallRem, ShadowDyRem, ShadowBlurRem, HollowBrightnessF, HollowSaturateF;
        /// <summary>정본 키프레임 — x(`eqswX` · v = dx 비율) · y(`eqswY` · 자리 기호 + 불투명도) · r(`eqswR` · v = spin 비율) · squash(`eqswSquash` · sk 배수) · dust(`eqswDust`) · snap(`eqswSnap`).</summary>
        public KeyStop[] X, Y, R, Squash, Dust, Snap;

        public sealed class KeyStop
        {
            public double At;
            public double V;              // x · r: 0~1 비율
            public string Sym;            // y: "0" · "rise" · "drop" · "drop-big" · "drop-small"
            public double Opacity;        // y · dust · snap
            public double Sx, Sy;         // squash · dust: scale (squash 는 OfSk 이면 sk 배수)
            public bool OfSk;
            public double TyPct;          // dust · snap: translateY(자기 높이 %)
            public double Scale;          // snap
            public double Brightness;     // snap (UI Image 는 1 초과를 못 낸다 — 표에만 · 결정 기록)
            public CssEase Ease;          // 이 키에서 다음 키까지(마지막 키는 안 쓴다)
        }

        public static EquipSwapSpec From(JsonObject root)
        {
            var L = J.Obj(J.Require(root, "layout"));
            var s = new EquipSwapSpec
            {
                FlyMs = N(L, "fly_ms"), LandK = N(L, "land_k"), SnapMs = N(L, "snap_ms"), SnapDelayMs = N(L, "snap_delay_ms"), Shrink = N(L, "shrink_f"),
                TiltMinDeg = N(L, "tilt_min_deg"), TiltMaxDeg = N(L, "tilt_max_deg"), TurnDeg = N(L, "turn_deg"),
                TurnsMin = J.Int(J.Require(L, "turns_min_n")), TurnsMax = J.Int(J.Require(L, "turns_max_n")),
                ReachWF = N(L, "reach_w_f"), ReachHF = N(L, "reach_h_f"), ReachPadPx = N(L, "reach_pad_px"),
                LandDistMinW = N(L, "land_dist_min_w"), LandDistMaxW = N(L, "land_dist_max_w"), DropMinHF = N(L, "drop_min_h_f"),
                RiseMinHF = N(L, "rise_min_h_f"), RiseMaxHF = N(L, "rise_max_h_f"),
                RemoveSlackMs = N(L, "remove_slack_ms"), DustMs = N(L, "dust_ms"), DustRemoveMs = N(L, "dust_remove_ms"),
                DustWF = N(L, "dust_w_f"), DustHF = N(L, "dust_h_f"), DustYF = N(L, "dust_y_f"),
                BounceBigRem = N(L, "bounce_big_rem"), BounceSmallRem = N(L, "bounce_small_rem"),
                ShadowDyRem = N(L, "shadow_dy_rem"), ShadowBlurRem = N(L, "shadow_blur_rem"),
                HollowBrightnessF = N(L, "hollow_brightness_f"), HollowSaturateF = N(L, "hollow_saturate_f"),
            };
            s.X = Stops(J.Require(root, "x"), "x"); s.Y = Stops(J.Require(root, "y"), "y"); s.R = Stops(J.Require(root, "r"), "r");
            s.Squash = Stops(J.Require(root, "squash"), "squash"); s.Dust = Stops(J.Require(root, "dust"), "dust"); s.Snap = Stops(J.Require(root, "snap"), "snap");
            return s;
        }

        static double N(JsonObject o, string key) { return J.Num(J.Require(o, key)); }
        static double Opt(JsonObject o, string key, double dflt) { object v; return o.TryGet(key, out v) && v != null ? J.Num(v) : dflt; }

        static KeyStop[] Stops(object arr, string track)
        {
            var list = J.List(arr, x => J.Obj(x));
            var outp = new KeyStop[list.Count];
            double prev = -1;
            for (int i = 0; i < list.Count; i++)
            {
                var o = list[i];
                var k = new KeyStop { At = J.Num(J.Require(o, "at")) };
                if (k.At < prev) throw new FormatException("EquipSwapUi «" + track + "» 키프레임 퍼센트는 오름차순이어야 한다");
                prev = k.At;
                switch (track)
                {
                    case "x": case "r": k.V = N(o, "v"); break;
                    case "y": k.Sym = J.Str(J.Require(o, "y")); k.Opacity = N(o, "opacity"); break;
                    case "squash": k.Sx = N(o, "sx"); k.Sy = N(o, "sy"); { object b; k.OfSk = o.TryGet("of_sk", out b) && b is bool && (bool)b; } break;
                    case "dust": k.Opacity = N(o, "opacity"); k.Sx = N(o, "sx"); k.Sy = N(o, "sy"); k.TyPct = N(o, "ty_pct"); break;
                    case "snap": k.TyPct = N(o, "ty_pct"); k.Scale = N(o, "scale"); k.Opacity = N(o, "opacity"); k.Brightness = Opt(o, "brightness", 1); break;
                }
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
            if (outp.Length < 2) throw new FormatException("EquipSwapUi «" + track + "» 키프레임이 둘 미만이다");
            return outp;
        }
    }

    /// <summary>정본 `grabEquipSwapFx` 가 렌더 **전에** 붙잡는 값(앱 상자 좌표 · 왼쪽 위 원점 · 기준 캔버스 px): 칸 중심·크기 · 앱 크기 · 바닥(장비 시트 아랫단) · 열린 팝업 카드의 가로 범위.</summary>
    public struct EquipSwapGrab
    {
        public double Cx, Cy, W, H, HostW, HostH, FloorY;
        public bool HasBlock;
        public double BlockL, BlockR;
    }

    /// <summary>정본 `playEquipSwapFx` 의 per-call 계산 결과 — 방향 · 기울기 · 회전(도) · 착지 반경 · 착지 x · 낙하 · 눕는가 · dx · 튕김.</summary>
    public struct EquipSwapPlan
    {
        public int Dir;
        public double Tilt, Spin, Reach, LandX, Drop, Dx, Rise;
        public bool Lands;
    }

    /// <summary>
    /// 정본 `UI.playEquipSwapFx(fx)` 의 셈(ui.js 3362~3466 · UnityEngine 참조 0): 바깥쪽으로(`dir`) 정수 바퀴 + 기울기 8~22° 로 돌고, 착지 반경은 기울인 사각형의 AABB 반폭
    /// `(W·sk·1.12·cosθ + H·sk·1.05·sinθ)/2 + 2px` · 착지 x 는 `cx + dir·W·rand(1.5,2.2)` 를 반경 안으로 클램프 · 낙하 = 시트 바닥에서 줄어든 타일의 세로 반경만큼 위 ·
    /// 팝업 카드가 착지 자리를 덮으면 카드 **옆 빈 띠**로(날아가던 쪽 먼저) · 띠가 없으면 화면 밖으로 떨어뜨린다(`lands=false` · 먼지·착지음 없음). 난수 순서는 정본 그대로(기울기 → 바퀴 → 거리 → 튕김).
    /// </summary>
    public static class EquipSwapRules
    {
        /// <summary>정본 `U.clamp(v, a, b)` = `min(b, max(a, v))`(util.js 103).</summary>
        public static double Clamp(double v, double lo, double hi) { return Math.Min(hi, Math.Max(lo, v)); }

        /// <param name="cssPx">정본 절대 CSS px 1 의 기준 캔버스 px(`anvil_fx_px`).</param>
        /// <param name="rand">정본 `U.rand(a, b)` — a 이상 b 미만.</param>
        public static EquipSwapPlan Plan(EquipSwapSpec s, EquipSwapGrab g, double cssPx, Func<double, double, double> rand)
        {
            var p = new EquipSwapPlan();
            p.Dir = g.Cx < g.HostW / 2 ? -1 : 1;
            p.Tilt = rand(s.TiltMinDeg, s.TiltMaxDeg);
            int turns = rand(0, 1) < 0.5 ? s.TurnsMin : s.TurnsMax;
            p.Spin = p.Dir * (s.TurnDeg * turns + p.Tilt);
            double th = p.Tilt * Math.PI / 180, sk = s.Shrink;
            p.Reach = (g.W * sk * s.ReachWF * Math.Cos(th) + g.H * sk * s.ReachHF * Math.Sin(th)) / 2 + s.ReachPadPx * cssPx;
            p.LandX = Clamp(g.Cx + p.Dir * g.W * rand(s.LandDistMinW, s.LandDistMaxW), p.Reach, g.HostW - p.Reach);
            p.Drop = Math.Max(g.H * s.DropMinHF, g.FloorY - g.Cy - (g.H * sk * (Math.Cos(th) + Math.Sin(th))) / 2);
            p.Lands = true;
            if (g.HasBlock && p.LandX + p.Reach > g.BlockL && p.LandX - p.Reach < g.BlockR)
            {
                var bands = new List<double[]>();
                double lo1 = p.Reach, hi1 = g.BlockL - p.Reach;
                if (hi1 >= lo1) bands.Add(new[] { lo1, hi1, -1.0 });
                double lo2 = g.BlockR + p.Reach, hi2 = g.HostW - p.Reach;
                if (hi2 >= lo2) bands.Add(new[] { lo2, hi2, 1.0 });
                if (bands.Count > 0)
                {
                    double[] band = null;
                    for (int i = 0; i < bands.Count; i++) if ((int)bands[i][2] == p.Dir) { band = bands[i]; break; }
                    if (band == null) band = bands[0];
                    p.LandX = Clamp(p.LandX, band[0], band[1]);
                }
                else
                {
                    p.Drop = g.HostH - g.Cy + g.H;
                    p.Lands = false;
                }
            }
            p.Dx = p.LandX - g.Cx;
            p.Rise = -rand(s.RiseMinHF, s.RiseMaxHF) * g.H;
            return p;
        }

        // ── 시각(ms · 연출 시작 기준) ──
        /// <summary>타일이 처음 바닥에 닿는 시각 — `FLY·LAND_K`(먼지·착지음).</summary>
        public static double LandMs(EquipSwapSpec s) { return s.FlyMs * s.LandK; }
        /// <summary>날아간 타일을 걷는 시각 — `FLY + 160`.</summary>
        public static double FlyEndMs(EquipSwapSpec s) { return s.FlyMs + s.RemoveSlackMs; }
        /// <summary>딸깍이 시작하는 시각(`EQSW_SNAP_DELAY`)과 끝(`+ SNAP_MS`).</summary>
        public static double SnapStartMs(EquipSwapSpec s) { return s.SnapDelayMs; }
        public static double SnapEndMs(EquipSwapSpec s) { return s.SnapDelayMs + s.SnapMs; }

        // ── 먼지 자리·크기(정본 착지 setTimeout 안) ──
        public static double DustX(EquipSwapGrab g, EquipSwapPlan p) { return g.Cx + p.Dx; }
        public static double DustY(EquipSwapSpec s, EquipSwapGrab g, EquipSwapPlan p) { return g.Cy + p.Drop + g.H * s.Shrink * s.DustYF; }
        public static double DustW(EquipSwapSpec s, EquipSwapGrab g) { return g.W * s.Shrink * s.DustWF; }
        public static double DustH(EquipSwapSpec s, EquipSwapGrab g) { return DustW(s, g) * s.DustHF; }

        /// <summary>구간별 타이밍 함수로 키 사이를 보간한다(CSS 는 키마다 `animation-timing-function` 을 다시 건다 · `forwards`).</summary>
        static void Segment(EquipSwapSpec.KeyStop[] keys, double percent, out int i0, out double e)
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

        static double Lerp(double a, double b, double e) { return a + (b - a) * e; }

        /// <summary>`eqswX` — 가로 오프셋(기준 px · 오른쪽이 +): 58% 에서 dx 에 닿고 멈춘다.</summary>
        public static double X(EquipSwapSpec s, EquipSwapPlan p, double percent)
        {
            int i; double e; Segment(s.X, percent, out i, out e);
            return p.Dx * Lerp(s.X[i].V, s.X[i + 1].V, e);
        }

        static double YOf(EquipSwapSpec s, EquipSwapSpec.KeyStop k, EquipSwapPlan p, double rem)
        {
            switch (k.Sym)
            {
                case "0": return 0;
                case "rise": return p.Rise;
                case "drop": return p.Drop;
                case "drop-big": return p.Drop - s.BounceBigRem * rem;
                case "drop-small": return p.Drop - s.BounceSmallRem * rem;
                default: throw new FormatException("EquipSwapUi y 자리 기호를 모른다: " + k.Sym);
            }
        }

        /// <summary>`eqswY` — 세로 오프셋(기준 px · 아래가 +)과 불투명도: 튕겨 오름 → 낙하 → 착지 → 큰 튐(.8rem) → 작은 튐(.26rem) → 누운 채 사라짐.</summary>
        public static void Y(EquipSwapSpec s, EquipSwapPlan p, double rem, double percent, out double y, out double opacity)
        {
            int i; double e; Segment(s.Y, percent, out i, out e);
            var a = s.Y[i]; var b = s.Y[i + 1];
            y = Lerp(YOf(s, a, p, rem), YOf(s, b, p, rem), e);
            opacity = Lerp(a.Opacity, b.Opacity, e);
        }

        /// <summary>`eqswR` — 회전(도 · CSS 시계 방향 +): 58% 에서 spin 에 닿고 멈춘다.</summary>
        public static double R(EquipSwapSpec s, EquipSwapPlan p, double percent)
        {
            int i; double e; Segment(s.R, percent, out i, out e);
            return p.Spin * Lerp(s.R[i].V, s.R[i + 1].V, e);
        }

        /// <summary>`eqswSquash` — 줄어듦(sk)과 착지 스쿼시(가로 1.12 / 세로 .84 → 되튐 .96/1.05 → sk).</summary>
        public static void Squash(EquipSwapSpec s, double percent, out double sx, out double sy)
        {
            int i; double e; Segment(s.Squash, percent, out i, out e);
            var a = s.Squash[i]; var b = s.Squash[i + 1];
            double ax = a.OfSk ? s.Shrink * a.Sx : a.Sx, ay = a.OfSk ? s.Shrink * a.Sy : a.Sy;
            double bx = b.OfSk ? s.Shrink * b.Sx : b.Sx, by = b.OfSk ? s.Shrink * b.Sy : b.Sy;
            sx = Lerp(ax, bx, e); sy = Lerp(ay, by, e);
        }

        /// <summary>`eqswDust` — 납작한 타원의 불투명도 · scale(x, y) · translateY(자기 높이 %).</summary>
        public static void Dust(EquipSwapSpec s, double percent, out double opacity, out double sx, out double sy, out double tyPct)
        {
            int i; double e; Segment(s.Dust, percent, out i, out e);
            var a = s.Dust[i]; var b = s.Dust[i + 1];
            opacity = Lerp(a.Opacity, b.Opacity, e); sx = Lerp(a.Sx, b.Sx, e); sy = Lerp(a.Sy, b.Sy, e); tyPct = Lerp(a.TyPct, b.TyPct, e);
        }

        /// <summary>`eqswSnap` — 위에서 떨어져 홈에 걸리며 오버슈트(1.16 → .955 → 1.028 → 1) · translateY(-42% → 0) · 불투명도(.25 → 1) · 밝기(1.75 → 1).</summary>
        public static void Snap(EquipSwapSpec s, double percent, out double tyPct, out double scale, out double opacity, out double brightness)
        {
            int i; double e; Segment(s.Snap, percent, out i, out e);
            var a = s.Snap[i]; var b = s.Snap[i + 1];
            tyPct = Lerp(a.TyPct, b.TyPct, e); scale = Lerp(a.Scale, b.Scale, e); opacity = Lerp(a.Opacity, b.Opacity, e); brightness = Lerp(a.Brightness, b.Brightness, e);
        }
    }
}
