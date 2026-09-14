using System;
using System.IO;
using NUnit.Framework;
using Forge.Core.CraftFx;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>
    /// T135 ⓒ — 제작 비교 팝업 «새로운!» 카드 강조의 셈이 정본 `style.css` 1833~1846 과 같은가.
    /// `newpulse`(글로우 번짐 6↔16px · 1.3s) · `shinesweep`(흰 띠 −80%→130% · 1.7s · 55~100% 멈춤) · 띠 기하(45%·220%·top −60%·15°·105° 그라디언트·α .5).
    /// </summary>
    public class CmpNewRulesTests
    {
        static CmpNewSpec S()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            string file = Path.Combine(root, "Assets", "Forge", "Resources", "CmpNewUi.json");
            return CmpNewSpec.From(MiniJson.ParseObject(File.ReadAllText(file)));
        }

        [Test]
        public void 주기와_띠_기하가_정본_그대로다()
        {
            var s = S();
            Assert.AreEqual(1300, s.PulseMs, 1e-9, "css 1833 `newpulse 1.3s`");
            Assert.AreEqual(1700, s.SweepMs, 1e-9, "css 1838 `shinesweep 1.7s`");
            Assert.AreEqual(0.45, s.SweepWF, 1e-9, "css 1835 `width: 45%`");
            Assert.AreEqual(2.20, s.SweepHF, 1e-9, "css 1835 `height: 220%`");
            Assert.AreEqual(-0.60, s.SweepTopF, 1e-9, "css 1835 `top: -60%`");
            Assert.AreEqual(15, s.SweepRotDeg, 1e-9, "css 1837 `rotate(15deg)`");
            Assert.AreEqual(105, s.SweepAngleDeg, 1e-9, "css 1836 `linear-gradient(105deg, …)`");
            Assert.AreEqual(0.5, s.SweepPeakA, 1e-9, "css 1836 `rgba(255,255,255,.5)`");
        }

        [Test]
        public void 글로우는_6과_16_사이를_숨쉬고_한_바퀴마다_되풀이한다()
        {
            var s = S();
            Assert.AreEqual(6, s.GlowPx(0), 1e-9, "0% = 6px");
            Assert.AreEqual(16, s.GlowPx(s.PulseMs * 0.5), 1e-9, "50% = 16px");
            Assert.AreEqual(6, s.GlowPx(s.PulseMs - 1e-9), 1e-6, "100% = 6px");
            Assert.AreEqual(6, s.GlowPx(s.PulseMs), 1e-9, "`infinite` — 한 바퀴 뒤 처음으로 돌아온다");
            Assert.AreEqual(16, s.GlowPx(s.PulseMs * 1.5), 1e-9, "다음 바퀴의 정점도 같다");
            // 전 구간이 6~16 안 · 앞 절반은 오르고 뒤 절반은 내린다.
            double prev = s.GlowPx(0);
            for (double ms = 10; ms <= s.PulseMs * 0.5; ms += 10)
            {
                double g = s.GlowPx(ms);
                Assert.GreaterOrEqual(g, prev - 1e-9, ms + "ms 에서 글로우가 도로 줄었다");
                Assert.LessOrEqual(g, 16 + 1e-9); Assert.GreaterOrEqual(g, 6 - 1e-9);
                prev = g;
            }
            prev = s.GlowPx(s.PulseMs * 0.5);
            for (double ms = s.PulseMs * 0.5 + 10; ms < s.PulseMs; ms += 10)
            {
                double g = s.GlowPx(ms);
                Assert.LessOrEqual(g, prev + 1e-9, ms + "ms 에서 글로우가 도로 늘었다");
                prev = g;
            }
        }

        [Test]
        public void 띠는_왼쪽_밖에서_오른쪽_밖으로_쓸고_55퍼센트_뒤에는_멈춰_쉰다()
        {
            var s = S();
            Assert.AreEqual(-0.80, s.SweepLeftF(0), 1e-9, "0% — 카드 왼쪽 밖");
            Assert.AreEqual(1.30, s.SweepLeftF(s.SweepMs * 0.55), 1e-9, "55% — 카드 오른쪽 밖");
            Assert.AreEqual(1.30, s.SweepLeftF(s.SweepMs * 0.8), 1e-9, "55~100% 는 멈춰 있다");
            Assert.AreEqual(1.30, s.SweepLeftF(s.SweepMs - 1e-9), 1e-6, "100% 도 같은 자리");
            Assert.AreEqual(-0.80, s.SweepLeftF(s.SweepMs), 1e-9, "`infinite` — 한 바퀴 뒤 다시 왼쪽 밖");
            Assert.IsFalse(s.SweepResting(s.SweepMs * 0.3), "쓰는 중");
            Assert.IsTrue(s.SweepResting(s.SweepMs * 0.7), "쉬는 중");
            Assert.IsTrue(s.SweepResting(s.SweepMs * 0.99));
            // 0~55% 는 단조 증가(되돌아가지 않는다).
            double prev = s.SweepLeftF(0);
            for (double ms = 10; ms <= s.SweepMs * 0.55; ms += 10)
            {
                double l = s.SweepLeftF(ms);
                Assert.GreaterOrEqual(l, prev - 1e-9, ms + "ms 에서 띠가 되돌아갔다");
                prev = l;
            }
        }

        [Test]
        public void 띠는_한_주기의_절반쯤에_카드_한가운데를_지난다()
        {
            var s = S();
            // 정본이 ease-in-out 이라 가운데(left ≈ .275 = 카드 가운데에 띠 가운데가 오는 자리)는 쓸기 구간의 중반이다.
            double mid = 0.5 - s.SweepWF * 0.5;   // 띠 왼쪽 모서리가 이 값일 때 띠 가운데가 카드 가운데
            double at = -1;
            for (double ms = 0; ms <= s.SweepMs * 0.55; ms += 1)
                if (s.SweepLeftF(ms) >= mid) { at = ms; break; }
            Assert.Greater(at, 0, "쓸기 구간 안에서 카드 한가운데를 지나야 한다");
            double u = at / (s.SweepMs * 0.55);
            Assert.That(u, Is.InRange(0.35, 0.65), "ease-in-out 이면 한가운데 통과가 쓸기 구간의 중반쯤이다 — 지금 " + u.ToString("0.00"));
        }

        [Test]
        public void 퍼센트_환산은_주기를_넘어도_감긴다()
        {
            Assert.AreEqual(0, CmpNewSpec.Percent(0, 1000), 1e-9);
            Assert.AreEqual(50, CmpNewSpec.Percent(500, 1000), 1e-9);
            Assert.AreEqual(0, CmpNewSpec.Percent(1000, 1000), 1e-9);
            Assert.AreEqual(25, CmpNewSpec.Percent(3250, 1000), 1e-9, "세 바퀴 반");
            Assert.AreEqual(0, CmpNewSpec.Percent(123, 0), 1e-9, "주기 0 은 0");
        }

        [Test]
        public void 표가_망가지면_세우다_바로_터진다()
        {
            Assert.Throws<FormatException>(() => CmpNewSpec.From(MiniJson.ParseObject(
                "{\"layout\":{\"pulse_ms\":0,\"sweep_ms\":1700,\"sweep_w_f\":0.45,\"sweep_h_f\":2.2,\"sweep_top_f\":-0.6,\"sweep_rot_deg\":15,\"sweep_angle_deg\":105,\"sweep_peak_a\":0.5},"
                + "\"newpulse\":[{\"at\":0,\"glow_px\":6},{\"at\":100,\"glow_px\":6}],\"shinesweep\":[{\"at\":0,\"left_f\":0},{\"at\":100,\"left_f\":1}]}")),
                "주기 0 은 못 쓴다");
            Assert.Throws<FormatException>(() => CmpNewSpec.From(MiniJson.ParseObject(
                "{\"layout\":{\"pulse_ms\":1300,\"sweep_ms\":1700,\"sweep_w_f\":0.45,\"sweep_h_f\":2.2,\"sweep_top_f\":-0.6,\"sweep_rot_deg\":15,\"sweep_angle_deg\":105,\"sweep_peak_a\":0.5},"
                + "\"newpulse\":[{\"at\":50,\"glow_px\":6},{\"at\":0,\"glow_px\":16}],\"shinesweep\":[{\"at\":0,\"left_f\":0},{\"at\":100,\"left_f\":1}]}")),
                "퍼센트가 거꾸로면 못 쓴다");
        }
    }
}
