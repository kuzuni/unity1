using System;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>T460 — 피해 숫자 아크 표(`DmgArcUi.json`)가 정본 `@keyframes dmgrise`(553)·`dmgcrit`(564)·`dmgkill`(577)의 수와 507 의 길이·곡선을 그대로 쥐는가 · 구간 곡선이 실제로 걸리는가.</summary>
    public class DmgArcRulesTests
    {
        static string TablePath()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            return Path.Combine(root, "Assets", "Forge", "Resources", "DmgArcUi.json");
        }
        static DmgArcSpec S() { return DmgArcSpec.From(MiniJson.ParseObject(File.ReadAllText(TablePath()))); }

        [Test]
        public void 길이와_곡선은_정본_507_그대로다()
        {
            DmgArcSpec s = S();
            Assert.AreEqual(550, s.DurationMs, 1e-9, "정본 507 .55s — 수명 900 이 아니다");
            Assert.AreEqual(0.16, s.Ease[0], 1e-9); Assert.AreEqual(0.86, s.Ease[1], 1e-9);
            Assert.AreEqual(0.35, s.Ease[2], 1e-9); Assert.AreEqual(1.0, s.Ease[3], 1e-9);
        }

        [Test]
        public void 세_아크의_키프레임_수가_정본_스팟과_같다()
        {
            DmgArcSpec s = S();
            Assert.IsTrue(s.Has("dmg") && s.Has("crit") && s.Has("kill"), "dmgrise·dmgcrit·dmgkill 셋");
            double dx, rise, sc, al, rot;
            s.Sample("dmg", 0.34, out dx, out rise, out sc, out al, out rot);
            Assert.AreEqual(0.7, dx, 1e-9, "dmgrise 34% dx .7"); Assert.AreEqual(1.0, rise, 1e-9, "34% rise 1"); Assert.AreEqual(1.0, sc, 1e-9); Assert.AreEqual(1.0, al, 1e-9);
            s.Sample("dmg", 0, out dx, out rise, out sc, out al, out rot);
            Assert.AreEqual(0.9, sc, 1e-9, "dmgrise 0% scale .9 — 페이드인 없음(opacity 1)"); Assert.AreEqual(1.0, al, 1e-9);
            s.Sample("crit", 0.09, out dx, out rise, out sc, out al, out rot);
            Assert.AreEqual(1.35, sc, 1e-9, "dmgcrit 9% scale 1.35"); Assert.AreEqual(5, rot, 1e-9, "9% rotate 5deg"); Assert.AreEqual(0.34, rise, 1e-9);
            s.Sample("crit", 0, out dx, out rise, out sc, out al, out rot);
            Assert.AreEqual(1.12, sc, 1e-9, "dmgcrit 0% 1.12 — 태어나는 프레임부터 일반타보다 크다(561 주석)"); Assert.AreEqual(-10, rot, 1e-9);
            s.Sample("kill", 0.07, out dx, out rise, out sc, out al, out rot);
            Assert.AreEqual(1.62, sc, 1e-9, "dmgkill 7% 오버슛 1.62"); Assert.AreEqual(3, rot, 1e-9);
            s.Sample("kill", 0.62, out dx, out rise, out sc, out al, out rot);
            Assert.AreEqual(1.2, sc, 1e-9, "dmgkill 62% 1.2"); Assert.AreEqual(0, rot, 1e-9, "62% 는 rotate 를 뺀 키프레임 — transform 이 통째로 갈리므로 0");
            foreach (string name in new[] { "dmg", "crit", "kill" })
            {
                s.Sample(name, 1, out dx, out rise, out sc, out al, out rot);
                Assert.AreEqual(0, al, 1e-9, name + " 100% opacity 0"); Assert.AreEqual(1, dx, 1e-9, name + " 100% dx 1");
                s.Sample(name, 1.5, out dx, out rise, out sc, out al, out rot);
                Assert.AreEqual(0, al, 1e-9, name + " 아크가 끝난 뒤엔 마지막 프레임에 머문다(forwards)");
            }
        }

        [Test]
        public void 구간_곡선이_걸린다_선형이_아니다()
        {
            DmgArcSpec s = S();
            // dmgrise 0%→7% 의 한가운데(3.5%): 선형이면 scale .9+.34×.5 = 1.07 · ease-out 꼴 곡선(.16,.86,.35,1)은 앞이 빨라 그보다 크다
            double dx, rise, sc, al, rot;
            s.Sample("dmg", 0.035, out dx, out rise, out sc, out al, out rot);
            double w = CssBezier.Y(0.5, 0.16, 0.86, 0.35, 1.0);
            Assert.Greater(w, 0.5, "cubic-bezier(.16,.86,.35,1) 은 앞이 빠른 곡선");
            Assert.AreEqual(0.9 + 0.34 * w, sc, 1e-6, "구간 진행률에 곡선을 건 값");
            Assert.AreNotEqual(1.07, Math.Round(sc, 6), "선형 보간이 아니다");
            Assert.AreEqual(0, CssBezier.Y(0, 0.16, 0.86, 0.35, 1.0), 1e-9); Assert.AreEqual(1, CssBezier.Y(1, 0.16, 0.86, 0.35, 1.0), 1e-9);
            Assert.AreEqual(0.5, CssBezier.Y(0.5, 0, 0, 1, 1), 1e-6, "linear 는 y = x");
            double prev = 0;
            for (int i = 1; i <= 20; i++) { double y = CssBezier.Y(i / 20.0, 0.16, 0.86, 0.35, 1.0); Assert.GreaterOrEqual(y, prev - 1e-9, "단조"); prev = y; }
        }

        [Test]
        public void 표가_틀리면_읽을_때_던진다()
        {
            Assert.Throws<InvalidOperationException>(() => DmgArcSpec.From(MiniJson.ParseObject(
                "{\"duration_ms\":550,\"ease\":[0.16,0.86,0.35,1],\"arcs\":{\"dmg\":[{\"at\":0,\"dx_f\":0,\"rise_f\":0,\"scale\":1,\"opacity\":1,\"rot\":0},{\"at\":0.5,\"dx_f\":1,\"rise_f\":1,\"scale\":1,\"opacity\":0,\"rot\":0}]}}")),
                "100% 키프레임이 없으면 던진다");
            Assert.Throws<InvalidOperationException>(() => DmgArcSpec.From(MiniJson.ParseObject(
                "{\"duration_ms\":550,\"ease\":[0.16,0.86,0.35,1],\"arcs\":{\"crit\":[{\"at\":0,\"dx_f\":0,\"rise_f\":0,\"scale\":1,\"opacity\":1,\"rot\":0},{\"at\":1,\"dx_f\":1,\"rise_f\":1,\"scale\":1,\"opacity\":0,\"rot\":0}]}}")),
                "기본 아크 dmg 가 없으면 던진다");
            Assert.Throws<InvalidOperationException>(() => DmgArcSpec.From(MiniJson.ParseObject(
                "{\"duration_ms\":550,\"ease\":[0.16,0.86],\"arcs\":{}}")), "ease 가 넷이 아니면 던진다");
        }
    }
}
