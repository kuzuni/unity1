using System;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Meta;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>T139 — 맵 위 이정표 표(<c>Resources/WaypointsUi.json</c>)와 정본 <c>msUntilDailyReset</c>(09:00 리셋 카운트다운) 셈. 순수 C#(dotnet 하니스에서도 돈다).</summary>
    public class WaypointsRulesTests
    {
        static string Json()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            return File.ReadAllText(Path.Combine(root, "Assets", "Forge", "Resources", "WaypointsUi.json"));
        }
        static WaypointsSpec S() { return WaypointsSpec.From(MiniJson.ParseObject(Json())); }

        [Test]
        public void 표는_정본_이정표_둘이고_리그_이정표는_없다()
        {
            WaypointsSpec s = S();
            Assert.AreEqual(2, s.Points.Count, "정본 index.html 83·86 — 미스터리 · 진행 패스(리그는 지웠다)");
            WaypointSpec m = s.Points[0], p = s.Points[1];
            Assert.AreEqual("waypoint-mystery", m.Id); Assert.AreEqual("wp_mystery", m.Icon, "정본 WP_ICON"); Assert.AreEqual(WaypointsSpec.TapStub, m.Tap); Assert.IsTrue(m.Countdown);
            Assert.AreEqual(0.21, m.TopF, 1e-9, "#waypoint-mystery top 21%"); Assert.AreEqual(0.03, m.RightF, 1e-9, "right 3%");
            Assert.AreEqual("waypoint-pass", p.Id); Assert.AreEqual("power", p.Icon, "정본 WP_ICON"); Assert.AreEqual(WaypointsSpec.TapPass, p.Tap); Assert.IsFalse(p.Countdown, "패스엔 카운트다운 span 이 없다");
            Assert.AreEqual(0.36, p.TopF, 1e-9, "#waypoint-pass top 36%"); Assert.AreEqual(0.04, p.RightF, 1e-9, "right 4%");
            foreach (WaypointSpec w in s.Points) Assert.AreNotEqual(WaypointsSpec.RemovedLeague, w.Id);
        }

        [Test]
        public void 치수는_정본_css_그대로다()
        {
            WaypointsSpec s = S();
            Assert.AreEqual(2.3, s.IconRem, 1e-9, ".waypoint-icon 2.3rem");
            Assert.AreEqual(0.15, s.GapRem, 1e-9, ".waypoint gap .15rem");
            Assert.AreEqual(0.6, s.TimeFontRem, 1e-9, ".waypoint-time .6rem");
            Assert.AreEqual(0.35, s.TimePadXRem, 1e-9, "padding 0 .35rem");
            Assert.AreEqual(0.4, s.TimeRadiusRem, 1e-9, "border-radius .4rem");
            Assert.AreEqual(0.08, s.ShadowDyRem, 1e-9, "drop-shadow y .08rem"); Assert.AreEqual(0.1, s.ShadowBlurRem, 1e-9, "blur .1rem");
            Assert.AreEqual(1000, s.TickMs, 1e-9, "정본 매초 tick");
            Assert.IsFalse(WaypointsRules.TickDue(s, 999)); Assert.IsTrue(WaypointsRules.TickDue(s, 1000)); Assert.IsTrue(WaypointsRules.TickDue(s, 5000));
        }

        [Test]
        public void 카운트다운은_오늘_09시까지_지났으면_내일_09시까지()
        {
            Assert.AreEqual(9, DailyReset.ResetHour, "정본 리터럴 9*3600*1000");
            Assert.AreEqual(60 * 1000.0, WaypointsRules.MsUntilDailyReset(new DateTime(2026, 9, 14, 8, 59, 0)), 1e-6, "08:59 → 1분");
            Assert.AreEqual(24 * 3600 * 1000.0, WaypointsRules.MsUntilDailyReset(new DateTime(2026, 9, 14, 9, 0, 0)), 1e-6, "정각은 next <= now 라 내일");
            Assert.AreEqual(24 * 3600 * 1000.0 - 1000, WaypointsRules.MsUntilDailyReset(new DateTime(2026, 9, 14, 9, 0, 1)), 1e-6, "09:00:01 → 23시 59분 59초");
            Assert.AreEqual(9.5 * 3600 * 1000.0, WaypointsRules.MsUntilDailyReset(new DateTime(2026, 9, 14, 23, 30, 0)), 1e-6, "23:30 → 9시간 30분");
            Assert.AreEqual(9 * 3600 + 1800, WaypointsRules.CountdownSec(new DateTime(2026, 9, 14, 23, 30, 0)), 1e-6);
            Assert.AreEqual(9 * 3600 * 1000.0, WaypointsRules.MsUntilDailyReset(new DateTime(2026, 12, 31, 0, 0, 0)), 1e-6, "해 넘김도 달력대로");
            Assert.Greater(WaypointsRules.MsUntilDailyReset(DateTime.Now), 0); Assert.LessOrEqual(WaypointsRules.MsUntilDailyReset(DateTime.Now), 24 * 3600 * 1000.0);
        }

        [Test]
        public void 깨진_표는_예외다()
        {
            string good = Json();
            Assert.Throws<FormatException>(() => WaypointsSpec.From(MiniJson.ParseObject(good.Replace("\"waypoint-pass\"", "\"waypoint-mystery\""))), "id 겹침");
            Assert.Throws<FormatException>(() => WaypointsSpec.From(MiniJson.ParseObject(good.Replace("\"waypoint-pass\"", "\"waypoint-league\""))), "리그 이정표는 정본이 지웠다");
            Assert.Throws<FormatException>(() => WaypointsSpec.From(MiniJson.ParseObject(good.Replace("\"tap\": \"pass\"", "\"tap\": \"league\""))), "모르는 tap");
            Assert.Throws<FormatException>(() => WaypointsSpec.From(MiniJson.ParseObject(good.Replace("\"top_f\": 0.36", "\"top_f\": 1.36"))), "비율 밖");
            Assert.Throws<FormatException>(() => WaypointsSpec.From(MiniJson.ParseObject(good.Replace("\"tick_ms\": 1000", "\"tick_ms\": 0"))), "tick 0");
        }
    }
}
