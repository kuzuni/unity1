using System;
using System.Collections.Generic;
using Forge.Core.Data;
using Forge.Core.Meta;

namespace Forge.Core.Ui
{
    /// <summary>이정표 하나(정본 <c>index.html</c> 의 <c>button.waypoint</c> 한 개) — 자리(무대 띠 비율)·아이콘 키·탭 동작·카운트다운 유무.</summary>
    public sealed class WaypointSpec
    {
        public string Id, Icon, Tap;
        public double TopF, RightF;
        public bool Countdown;
    }

    /// <summary>
    /// 맵 위 이정표 표(ROUTINE T139 · <c>Resources/WaypointsUi.json</c>): 정본 <c>style.css</c> 315~335 의 치수와 <c>index.html</c> 83·86 의 두 버튼.
    /// 값은 전부 표에서 온다(§1) · <c>_rem</c> = 정본 rem · <c>_f</c> = 무대 띠(<c>#game-area</c>) 비율. UnityEngine 참조 0.
    /// 리그 이정표(<c>waypoint-league</c>)는 정본이 지웠으므로(주인 지시 2026-08-19) 표에 있으면 예외 — «원작에 없는 것» 을 되살리지 못하게 한다.
    /// </summary>
    public sealed class WaypointsSpec
    {
        public const string TapStub = "stub";
        public const string TapPass = "pass";
        public const string RemovedLeague = "waypoint-league";

        public double IconRem, GapRem, TimeFontRem, TimePadXRem, TimeRadiusRem, ShadowDyRem, ShadowBlurRem, TickMs;
        public IReadOnlyList<WaypointSpec> Points;

        public static WaypointsSpec From(JsonObject root)
        {
            var L = J.Obj(J.Require(root, "layout"));
            Func<string, double> n = k => J.Num(J.Require(L, k));
            var s = new WaypointsSpec
            {
                IconRem = n("icon_rem"), GapRem = n("gap_rem"), TimeFontRem = n("time_font_rem"), TimePadXRem = n("time_pad_x_rem"),
                TimeRadiusRem = n("time_r_rem"), ShadowDyRem = n("shadow_dy_rem"), ShadowBlurRem = n("shadow_blur_rem"), TickMs = n("tick_ms"),
            };
            if (s.IconRem <= 0 || s.TickMs <= 0) throw new FormatException("WaypointsUi: icon_rem·tick_ms 는 양수여야 한다");
            var list = new List<WaypointSpec>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (object o in J.Arr(J.Require(root, "waypoints")))
            {
                JsonObject w = J.Obj(o);
                var p = new WaypointSpec
                {
                    Id = J.Str(J.Require(w, "id")), Icon = J.Str(J.Require(w, "icon")), Tap = J.Str(J.Require(w, "tap")),
                    TopF = J.Num(J.Require(w, "top_f")), RightF = J.Num(J.Require(w, "right_f")), Countdown = J.Bool(J.Require(w, "countdown")),
                };
                if (string.IsNullOrEmpty(p.Id) || string.IsNullOrEmpty(p.Icon)) throw new FormatException("WaypointsUi: 이정표에 id/icon 이 없다");
                if (p.Id == RemovedLeague) throw new FormatException("WaypointsUi: 리그 이정표는 정본이 지웠다(주인 지시 2026-08-19) — 되살리지 않는다");
                if (!ids.Add(p.Id)) throw new FormatException("WaypointsUi: 이정표 id 가 겹친다 — " + p.Id);
                if (p.Tap != TapStub && p.Tap != TapPass) throw new FormatException("WaypointsUi: tap 은 stub|pass — " + p.Id + " «" + p.Tap + "»");
                if (p.TopF < 0 || p.TopF > 1 || p.RightF < 0 || p.RightF > 1) throw new FormatException("WaypointsUi: top_f·right_f 는 0~1 — " + p.Id);
                list.Add(p);
            }
            if (list.Count == 0) throw new FormatException("WaypointsUi: 이정표가 없다");
            s.Points = list;
            return s;
        }
    }

    /// <summary>정본 <c>ui.js</c> 6126(매초 tick 의 카운트다운) · 6172 <c>msUntilDailyReset</c>: 오늘 09:00(<see cref="DailyReset.ResetHour"/>)까지, 지났으면 내일 09:00 까지.</summary>
    public static class WaypointsRules
    {
        /// <summary><c>const next = new Date(); next.setHours(9,0,0,0); if (next &lt;= new Date()) next.setDate(next.getDate() + 1); return next - Date.now();</c> — <paramref name="localNow"/> 는 기기 로컬 시각.</summary>
        public static double MsUntilDailyReset(DateTime localNow)
        {
            DateTime next = new DateTime(localNow.Year, localNow.Month, localNow.Day, DailyReset.ResetHour, 0, 0, localNow.Kind);
            if (next <= localNow) next = next.AddDays(1);
            return (next - localNow).TotalMilliseconds;
        }

        /// <summary>표시용 초 — 정본 <c>U.fmtTime(this.msUntilDailyReset() / 1000)</c>.</summary>
        public static double CountdownSec(DateTime localNow) { return MsUntilDailyReset(localNow) / 1000.0; }

        /// <summary>글자를 다시 쓸 때인가 — 지난 벽시계 ms 가 <c>tick_ms</c>(정본 1초 tick) 이상.</summary>
        public static bool TickDue(WaypointsSpec s, double sinceMs) { return sinceMs >= s.TickMs; }
    }
}
