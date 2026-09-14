using System;
using System.Collections.Generic;
using Forge.Core.CraftFx;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>
    /// 공통 수령 연출(정본 `ui.js rewardBurst(rewards, opts)` · T134)의 **수치표** — `Resources/RewardBurstUi.json` 의 layout 절과 키프레임 절을 강타입으로.
    /// 값은 전부 표에서 온다(§1) · 길이 단위: `_rem` = 정본 rem · `_px` = 정본 절대 CSS px(기준 캔버스로는 `anvil_fx_px` 배율 · T87 결정 222) · `_ms` = 벽시계.
    /// </summary>
    public sealed class RewardBurstSpec
    {
        public double FlyMs, CurStepMs, IconStepMs, IconJitterMs, HoldIconsN, HoldSlackMs;
        public double CountBase, CountLogK, CountMin, CountMax;
        public double RadMinPx, RadMaxPx, RyK, RyMinPx, RotMaxDeg, RotMidK;
        public double SrcYF, FallbackXF, FallbackYF, TopbarFallbackYPx;
        public double ImpactUpPx, ImpactMs, GlowAnimMs, RingAnimMs, PulseMs, PulseAnimMs;
        public double LandK, FlySlackMs;
        public double PopSmMs, PopSmAnimMs, PopMs, PopAnimMs, PopAfterMs, PopJitterXPx, PopJitterYPx;
        public double TickOutMs, TickBumpMs, TickDyPx, TickMinYPx, TickUpPx, TickUpMinYPx;
        public double AnchorExtraMs, AnchorOutMs, AnchorInMs;
        public double AmtMs, AmtStepMs, AmtRemoveMs, AmtDxPx, AmtMarginPx, AmtDyPx, AmtDyNoSrcPx, AmtRowPx;
        public double CardTopPx, CardMinYPx, CardInsetXPx;
        public double GlowRem, RingRem, RingBorderRem, FlyIcoRem, AmtFontRem, AmtIcoRem, AmtGapRem, AmtStrokePx;
        public double PopRem, PopSmRem, AnchorRem, AnchorBorderRem, AnchorIcoRem, TickFontRem, TickIcoRem, TickGapRem, TickStrokePx;
        public double PulseScale, PulseBrightF;
        public Track FlyX, FlyY, Glow, Ring, Pop, AnchorIn, Amt, TickBump, Pulse;
        /// <summary>재화 키 → 아이콘 키(정본 `CURRENCY_ICON`) · 재화 키 → 상단바 pill 이름(정본 `pillSel` · 코인·젬만).</summary>
        public OrderedMap<string> CurrencyIcon, CurrencyPill;

        /// <summary>키프레임 한 줄 — 퍼센트 · 수치 칸(opacity·scale·…) · 기호 칸(x·y·rot 의 «rx»·«ty»·«mid» 같은 자리표) · 다음 키까지의 이징.</summary>
        public sealed class KeyStop
        {
            public double At;
            public readonly Dictionary<string, double> Num = new Dictionary<string, double>();
            public readonly Dictionary<string, string> Sym = new Dictionary<string, string>();
            public CssEase Ease = CssEase.Linear;
            public bool Has(string k) { return Num.ContainsKey(k) || Sym.ContainsKey(k); }
        }

        /// <summary>키프레임 묶음(정본 `@keyframes` 하나 또는 WAAPI `animate([...])` 한 줄) — 구간별 이징으로 보간한다.</summary>
        public sealed class Track
        {
            public KeyStop[] Keys;

            /// <summary>퍼센트가 든 구간과 그 구간 안의 이징된 진행도.</summary>
            public void Segment(double percent, out int i0, out double e)
            {
                if (percent <= Keys[0].At) { i0 = 0; e = 0; return; }
                int last = Keys.Length - 1;
                if (percent >= Keys[last].At) { i0 = last - 1; e = 1; return; }
                int i = 0;
                while (i < last - 1 && percent >= Keys[i + 1].At) i++;
                double span = Keys[i + 1].At - Keys[i].At;
                double u = span <= 0 ? 1 : (percent - Keys[i].At) / span;
                i0 = i; e = Keys[i].Ease.Ease(u);
            }

            /// <summary>수치 칸 하나를 보간한다 — 기호 칸이면 <paramref name="resolve"/> 로 값을 푼다(«0» 은 0).</summary>
            public double Sample(double percent, string key, Func<string, double> resolve)
            {
                int i; double e;
                Segment(percent, out i, out e);
                double a = Value(Keys[i], key, resolve), b = Value(Keys[i + 1], key, resolve);
                return a + (b - a) * e;
            }

            static double Value(KeyStop k, string key, Func<string, double> resolve)
            {
                double d;
                if (k.Num.TryGetValue(key, out d)) return d;
                string s;
                if (k.Sym.TryGetValue(key, out s))
                {
                    if (s == "0") return 0;
                    if (resolve == null) throw new FormatException("키프레임 기호 «" + s + "» 를 풀 수 없다(resolve 없음)");
                    return resolve(s);
                }
                throw new KeyNotFoundException("키프레임 " + k.At + "% 에 «" + key + "» 칸이 없다");
            }
        }

        public static RewardBurstSpec From(JsonObject root)
        {
            var L = J.Obj(J.Require(root, "layout"));
            Func<string, double> n = k => J.Num(J.Require(L, k));
            var s = new RewardBurstSpec
            {
                FlyMs = n("fly_ms"), CurStepMs = n("cur_step_ms"), IconStepMs = n("icon_step_ms"), IconJitterMs = n("icon_jitter_ms"), HoldIconsN = n("hold_icons_n"), HoldSlackMs = n("hold_slack_ms"),
                CountBase = n("count_base_n"), CountLogK = n("count_log_k"), CountMin = n("count_min_n"), CountMax = n("count_max_n"),
                RadMinPx = n("rad_min_px"), RadMaxPx = n("rad_max_px"), RyK = n("ry_k"), RyMinPx = n("ry_min_px"), RotMaxDeg = n("rot_max_deg"), RotMidK = n("rot_mid_k"),
                SrcYF = n("src_y_f"), FallbackXF = n("fallback_x_f"), FallbackYF = n("fallback_y_f"), TopbarFallbackYPx = n("topbar_fallback_y_px"),
                ImpactUpPx = n("impact_up_px"), ImpactMs = n("impact_ms"), GlowAnimMs = n("glow_anim_ms"), RingAnimMs = n("ring_anim_ms"), PulseMs = n("pulse_ms"), PulseAnimMs = n("pulse_anim_ms"),
                LandK = n("land_k"), FlySlackMs = n("fly_slack_ms"),
                PopSmMs = n("pop_sm_ms"), PopSmAnimMs = n("pop_sm_anim_ms"), PopMs = n("pop_ms"), PopAnimMs = n("pop_anim_ms"), PopAfterMs = n("pop_after_ms"), PopJitterXPx = n("pop_jitter_x_px"), PopJitterYPx = n("pop_jitter_y_px"),
                TickOutMs = n("tick_out_ms"), TickBumpMs = n("tick_bump_ms"), TickDyPx = n("tick_dy_px"), TickMinYPx = n("tick_min_y_px"), TickUpPx = n("tick_up_px"), TickUpMinYPx = n("tick_up_min_y_px"),
                AnchorExtraMs = n("anchor_extra_ms"), AnchorOutMs = n("anchor_out_ms"), AnchorInMs = n("anchor_in_ms"),
                AmtMs = n("amt_ms"), AmtStepMs = n("amt_step_ms"), AmtRemoveMs = n("amt_remove_ms"), AmtDxPx = n("amt_dx_px"), AmtMarginPx = n("amt_margin_px"), AmtDyPx = n("amt_dy_px"), AmtDyNoSrcPx = n("amt_dy_nosrc_px"), AmtRowPx = n("amt_row_px"),
                CardTopPx = n("card_top_px"), CardMinYPx = n("card_min_y_px"), CardInsetXPx = n("card_inset_x_px"),
                GlowRem = n("glow_rem"), RingRem = n("ring_rem"), RingBorderRem = n("ring_border_rem"), FlyIcoRem = n("fly_ico_rem"), AmtFontRem = n("amt_font_rem"), AmtIcoRem = n("amt_ico_rem"), AmtGapRem = n("amt_gap_rem"), AmtStrokePx = n("amt_stroke_px"),
                PopRem = n("pop_rem"), PopSmRem = n("pop_sm_rem"), AnchorRem = n("anchor_rem"), AnchorBorderRem = n("anchor_border_rem"), AnchorIcoRem = n("anchor_ico_rem"), TickFontRem = n("tick_font_rem"), TickIcoRem = n("tick_ico_rem"), TickGapRem = n("tick_gap_rem"), TickStrokePx = n("tick_stroke_px"),
                PulseScale = n("pulse_scale"), PulseBrightF = n("pulse_bright_f"),
            };
            s.FlyX = Stops(J.Require(root, "fly_x"));
            s.FlyY = Stops(J.Require(root, "fly_y"));
            s.Glow = Stops(J.Require(root, "glow"));
            s.Ring = Stops(J.Require(root, "ring"));
            s.Pop = Stops(J.Require(root, "pop"));
            s.AnchorIn = Stops(J.Require(root, "anchor_in"));
            s.Amt = Stops(J.Require(root, "amt"));
            s.TickBump = Stops(J.Require(root, "tick_bump"));
            s.Pulse = Stops(J.Require(root, "pulse"));
            s.CurrencyIcon = J.StrMap(J.Require(root, "currency_icon"));
            s.CurrencyPill = J.StrMap(J.Require(root, "currency_pill"));
            return s;
        }

        /// <summary>CSS 타이밍 함수 — 네 수의 cubic-bezier 또는 키워드(`ease-out` 등 · CSS 정의값).</summary>
        public static CssEase EaseOf(object e)
        {
            if (e == null) return CssEase.Linear;
            string kw = e as string;
            if (kw != null)
            {
                switch (kw)
                {
                    case "linear": return CssEase.Linear;
                    case "ease": return new CssEase(0.25, 0.1, 0.25, 1);
                    case "ease-in": return new CssEase(0.42, 0, 1, 1);
                    case "ease-out": return new CssEase(0, 0, 0.58, 1);
                    case "ease-in-out": return new CssEase(0.42, 0, 0.58, 1);
                    default: throw new FormatException("모르는 타이밍 키워드: " + kw);
                }
            }
            var v = J.NumArr(e);
            if (v.Length != 4) throw new FormatException("ease 는 cubic-bezier 네 수여야 한다");
            return new CssEase(v[0], v[1], v[2], v[3]);
        }

        static Track Stops(object arr)
        {
            var list = J.List(arr, x => J.Obj(x));
            var keys = new KeyStop[list.Count];
            double prev = -1;
            for (int i = 0; i < list.Count; i++)
            {
                var o = list[i];
                var k = new KeyStop { At = J.Num(J.Require(o, "at")) };
                if (k.At < prev) throw new FormatException("RewardBurstUi 키프레임 퍼센트는 오름차순이어야 한다");
                prev = k.At;
                foreach (var kv in o)
                {
                    if (kv.Key == "at") continue;
                    if (kv.Key == "ease") { k.Ease = EaseOf(kv.Value); continue; }
                    if (kv.Value is double) k.Num[kv.Key] = (double)kv.Value;
                    else if (kv.Value is string) k.Sym[kv.Key] = (string)kv.Value;
                    else throw new FormatException("키프레임 칸 «" + kv.Key + "» 은 수 또는 기호여야 한다");
                }
                keys[i] = k;
            }
            if (keys.Length < 2) throw new FormatException("키프레임이 둘 미만이다");
            return new Track { Keys = keys };
        }
    }

    /// <summary>수령 목록의 한 줄(정본 `entries` 의 [cur, amt] · 양은 floor 뒤 0 초과만).</summary>
    public struct RewardEntry
    {
        public string Currency;
        public double Amount;
    }

    /// <summary>아이콘 하나의 흩어짐(정본 per-i 난수 결과 · 길이는 기준 캔버스 px · 각도는 도).</summary>
    public struct RewardIcon
    {
        public int Index;
        public double DelayMs, Rx, Ry, RotDeg;
    }

    /// <summary>도착점(정본 `targetOf` → `promoteIfCovered`) — 기준 캔버스 px · 왼쪽 위 원점.</summary>
    public struct RewardTarget
    {
        public double X, Y;
        /// <summary>도착 pill 이 시트·팝업에 가려져 있다(앵커 배지를 세우고 카운터를 위로 올린다).</summary>
        public bool Covered;
        /// <summary>덮은 카드의 상단 모서리로 승격됐다.</summary>
        public bool Promoted;
    }

    /// <summary>
    /// 정본 `UI.rewardBurst(rewards, opts)` 의 셈(ui.js 3563~3760 · UnityEngine 참조 0): 재화별 아이콘 개수 `clamp(2 + round(log10(max(1,양))·1.3), 2, 7)` ·
    /// 출발점(누른 버튼 상단 1/3) · 흩어짐(항상 위쪽 · 반경 18~40px) · 지연(재화 90ms 층 + 아이콘 44ms 층 + 0~22 지터) · 가려진 도착점의 카드 상단 승격 ·
    /// 누적 카운터 값 `round(양·(i+1)/n)` · «+획득량» 라벨 자리 · 토스트 유예 시각. 반올림은 JS `Math.round`(.5 를 올림).
    /// </summary>
    public static class RewardBurstRules
    {
        public static double JsRound(double x) { return Math.Floor(x + 0.5); }

        /// <summary>정본 `entries` — 양을 floor 하고 0 초과만 남긴다(순서 유지). null · NaN 은 0.</summary>
        public static List<RewardEntry> Entries(IEnumerable<KeyValuePair<string, double>> rewards)
        {
            var list = new List<RewardEntry>();
            if (rewards == null) return list;
            foreach (var kv in rewards)
            {
                double v = kv.Value;
                if (double.IsNaN(v) || double.IsInfinity(v)) v = 0;
                v = Math.Floor(v);
                if (v > 0) list.Add(new RewardEntry { Currency = kv.Key, Amount = v });
            }
            return list;
        }

        /// <summary>재화 하나의 아이콘 수 — coinBurst 와 같은 로그 눈금(2~7).</summary>
        public static int Count(RewardBurstSpec s, double amount)
        {
            if (double.IsNaN(amount) || amount <= 0) return 0;
            double n = s.CountBase + JsRound(Math.Log10(Math.Max(1, amount)) * s.CountLogK);
            return (int)Math.Min(s.CountMax, Math.Max(s.CountMin, n));
        }

        /// <summary>정본 `_toastHoldUntil` 유예 길이 — `entries·90 + 7·44 + FLY + 120`.</summary>
        public static double HoldMs(RewardBurstSpec s, int entries) { return entries * s.CurStepMs + s.HoldIconsN * s.IconStepMs + s.FlyMs + s.HoldSlackMs; }

        /// <summary>재화 ci 의 마지막 아이콘 출발 시각(지터 상한 포함) — `ci·90 + (n−1)·44 + 22`.</summary>
        public static double LastDelayMs(RewardBurstSpec s, int ci, int n) { return ci * s.CurStepMs + (n - 1) * s.IconStepMs + s.IconJitterMs; }

        /// <summary>아이콘 i 의 출발 시각 — `ci·90 + i·44 + rand(0,22)`.</summary>
        public static double IconDelayMs(RewardBurstSpec s, int ci, int i, Func<double, double, double> rand) { return ci * s.CurStepMs + i * s.IconStepMs + rand(0, s.IconJitterMs); }

        /// <summary>
        /// 재화 ci 의 아이콘 n 개 — 지연·흩어짐(<paramref name="cssPx"/> = 정본 CSS px 1 의 기준 캔버스 px) · 회전.
        /// 흩어짐은 **항상 위쪽**(`ry = −|sin|·rad·.9 − 10`) — 아래로 퍼지면 다음 행의 보상으로 오독된다(정본 주석).
        /// </summary>
        public static RewardIcon[] Icons(RewardBurstSpec s, int ci, int n, double cssPx, Func<double, double, double> rand)
        {
            var outp = new RewardIcon[n];
            for (int i = 0; i < n; i++)
            {
                double delay = IconDelayMs(s, ci, i, rand);
                double ang = rand(0, Math.PI * 2), rad = rand(s.RadMinPx, s.RadMaxPx) * cssPx;
                double rx = Math.Cos(ang) * rad;
                double ry = -Math.Abs(Math.Sin(ang)) * rad * s.RyK - s.RyMinPx * cssPx;
                double rot = rand(-s.RotMaxDeg, s.RotMaxDeg);
                outp[i] = new RewardIcon { Index = i, DelayMs = delay, Rx = rx, Ry = ry, RotDeg = rot };
            }
            return outp;
        }

        /// <summary>착지 순간(분출 시작 기준 ms) — `delay + FLY·.94`.</summary>
        public static double LandMs(RewardBurstSpec s, RewardIcon p) { return p.DelayMs + s.FlyMs * s.LandK; }
        /// <summary>아이콘을 걷는 시각 — `delay + FLY + 60`.</summary>
        public static double IconEndMs(RewardBurstSpec s, RewardIcon p) { return p.DelayMs + s.FlyMs + s.FlySlackMs; }
        /// <summary>도착 마침표(큰 별 + 카운터 정리 + pill 박동) 시각 — `lastDelay + FLY·.94 + 140`.</summary>
        public static double PopMs(RewardBurstSpec s, double lastDelayMs) { return lastDelayMs + s.FlyMs * s.LandK + s.PopAfterMs; }
        /// <summary>앵커 배지가 물러나기 시작하는 시각 — `lastDelay + FLY + 460` (그 뒤 340ms 페이드).</summary>
        public static double AnchorOutMs(RewardBurstSpec s, double lastDelayMs) { return lastDelayMs + s.FlyMs + s.AnchorExtraMs; }
        /// <summary>«+획득량» 라벨의 시작·제거 시각 — `ci·110` · `720 + ci·110`.</summary>
        public static double AmtDelayMs(RewardBurstSpec s, int ci) { return ci * s.AmtStepMs; }
        public static double AmtEndMs(RewardBurstSpec s, int ci) { return s.AmtRemoveMs + ci * s.AmtStepMs; }
        /// <summary>연출이 완전히 걷히는 시각(재화 수 · 재화별 아이콘 수 최대) — 앵커 페이드까지.</summary>
        public static double TotalMs(RewardBurstSpec s, int entries, int maxN)
        {
            if (entries <= 0) return 0;
            double last = LastDelayMs(s, entries - 1, maxN);
            double a = AnchorOutMs(s, last) + s.AnchorOutMs;
            double b = PopMs(s, last) + Math.Max(s.PopMs, s.TickOutMs);
            double c = AmtEndMs(s, entries - 1);
            return Math.Max(a, Math.Max(b, c));
        }

        /// <summary>착지할 때 카운터가 보이는 누적값 — `round(amt·(i+1)/n)` (마지막은 정확히 amt).</summary>
        public static double Per(double amount, int i, int n) { return JsRound(amount * (i + 1) / n); }

        /// <summary>출발점 — 누른 버튼이 있으면 가로 가운데·세로 상단 .32 (srcTop = 버튼 위) · 없으면 상자 가운데·.58H(srcTop 없음).</summary>
        public static void Source(RewardBurstSpec s, double hostW, double hostH, bool hasFrom, double fx, double fy, double fw, double fh,
                                  out double sx, out double sy, out double? srcTop)
        {
            if (hasFrom && (fw > 0 || fh > 0)) { sx = fx + fw / 2; sy = fy + fh * s.SrcYF; srcTop = fy; }
            else { sx = hostW * s.FallbackXF; sy = hostH * s.FallbackYF; srcTop = null; }
        }

        /// <summary>정본 `targetOf` 폴백 — pill 도 상단바도 없으면 화면 위 가운데(10px).</summary>
        public static RewardTarget FallbackTarget(RewardBurstSpec s, double hostW, double cssPx)
        {
            return new RewardTarget { X = hostW * s.FallbackXF, Y = s.TopbarFallbackYPx * cssPx, Covered = true };
        }

        /// <summary>
        /// 정본 `promoteIfCovered` — 가려졌으면 Covered · 덮은 카드가 있으면 카드 상단 모서리 안쪽(위 18px · 최소 16px · 좌우 26px 안)으로 승격.
        /// 카드 사각은 기준 캔버스 px(왼쪽 위 원점).
        /// </summary>
        public static RewardTarget Promote(RewardBurstSpec s, RewardTarget t, bool covered, bool hasCard, double cardL, double cardT, double cardR, double cssPx)
        {
            if (!covered) return t;
            t.Covered = true;
            if (hasCard)
            {
                t.Y = Math.Max(cardT + s.CardTopPx * cssPx, s.CardMinYPx * cssPx);
                t.X = Math.Min(Math.Max(t.X, cardL + s.CardInsetXPx * cssPx), cardR - s.CardInsetXPx * cssPx);
                t.Promoted = true;
            }
            return t;
        }

        /// <summary>누적 카운터의 세로 자리 — 가려졌으면 앵커 **위**(`max(y−30, 8)`) · 아니면 pill 아래(`max(y,20)+22`).</summary>
        public static double TickY(RewardBurstSpec s, RewardTarget t, double cssPx)
        {
            return t.Covered ? Math.Max(t.Y - s.TickUpPx * cssPx, s.TickUpMinYPx * cssPx) : Math.Max(t.Y, s.TickMinYPx * cssPx) + s.TickDyPx * cssPx;
        }

        /// <summary>«+획득량» 라벨 자리 — 버튼 위-왼쪽 대각(`sx−64`, `srcTop−42`) · 재화마다 30px 씩 위로 · 좌우 44px 안.</summary>
        public static void AmtPos(RewardBurstSpec s, double sx, double sy, double? srcTop, int ci, double hostW, double cssPx, out double lx, out double ly)
        {
            double x = srcTop == null ? sx : sx - s.AmtDxPx * cssPx;
            lx = Math.Min(Math.Max(x, s.AmtMarginPx * cssPx), hostW - s.AmtMarginPx * cssPx);
            ly = (srcTop == null ? sy - s.AmtDyNoSrcPx * cssPx : srcTop.Value - s.AmtDyPx * cssPx) - ci * s.AmtRowPx * cssPx;
        }

        // ---- 키프레임 샘플 (퍼센트 0~100 · WAAPI fill:both / CSS forwards 라 범위 밖은 양 끝 값) ----

        /// <summary>날아가는 아이콘의 가로 오프셋 — 0 → rx(34%) → tx(100%).</summary>
        public static double FlyX(RewardBurstSpec s, double percent, double rx, double tx)
        {
            return s.FlyX.Sample(percent, "x", k => k == "rx" ? rx : k == "tx" ? tx : Unknown(k));
        }

        /// <summary>날아가는 아이콘의 세로 오프셋·회전·크기·불투명도 — 0/.35/.2 → ry·rot·.3(34%) → ty·rot·.62(94%) → ·.5·0(100%).</summary>
        public static void FlyY(RewardBurstSpec s, double percent, double ry, double ty, double rot, out double y, out double rotDeg, out double scale, out double opacity)
        {
            Func<string, double> r = k => k == "ry" ? ry : k == "ty" ? ty : k == "rot" ? rot : k == "mid" ? rot * s.RotMidK : Unknown(k);
            y = s.FlyY.Sample(percent, "y", r);
            rotDeg = s.FlyY.Sample(percent, "rot", r);
            scale = s.FlyY.Sample(percent, "scale", null);
            opacity = s.FlyY.Sample(percent, "opacity", null);
        }

        public static void Glow(RewardBurstSpec s, double percent, out double scale, out double opacity)
        { scale = s.Glow.Sample(percent, "scale", null); opacity = s.Glow.Sample(percent, "opacity", null); }

        public static void Ring(RewardBurstSpec s, double percent, out double scale, out double opacity, out double borderRem)
        { scale = s.Ring.Sample(percent, "scale", null); opacity = s.Ring.Sample(percent, "opacity", null); borderRem = s.Ring.Sample(percent, "border_rem", null); }

        public static void Pop(RewardBurstSpec s, double percent, out double scale, out double rotDeg, out double opacity)
        { scale = s.Pop.Sample(percent, "scale", null); rotDeg = s.Pop.Sample(percent, "rot_deg", null); opacity = s.Pop.Sample(percent, "opacity", null); }

        public static void AnchorIn(RewardBurstSpec s, double percent, out double scale, out double opacity)
        { scale = s.AnchorIn.Sample(percent, "scale", null); opacity = s.AnchorIn.Sample(percent, "opacity", null); }

        /// <summary>«+획득량» 라벨 — 불투명도 · translateY(rem) · scale.</summary>
        public static void Amt(RewardBurstSpec s, double percent, out double opacity, out double tyRem, out double scale)
        { opacity = s.Amt.Sample(percent, "opacity", null); tyRem = s.Amt.Sample(percent, "ty_rem", null); scale = s.Amt.Sample(percent, "scale", null); }

        public static void TickBump(RewardBurstSpec s, double percent, out double scale, out double tyRem)
        { scale = s.TickBump.Sample(percent, "scale", null); tyRem = s.TickBump.Sample(percent, "ty_rem", null); }

        /// <summary>pill 박동 — scale(밴드는 1 고정 · `rw-pulse-band`) · 밝기 0~1(brightness 1 → 1.85 를 흰색 섞기 비율로).</summary>
        public static void Pulse(RewardBurstSpec s, double percent, bool band, out double scale, out double bright)
        { scale = band ? 1 : s.Pulse.Sample(percent, "scale", null); bright = s.Pulse.Sample(percent, "bright", null); }

        static double Unknown(string k) { throw new FormatException("키프레임 기호를 모른다: " + k); }
    }
}
