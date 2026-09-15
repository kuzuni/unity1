using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>T334 1회차 — 소환 결과 상태 기계가 정본 `tickSummonResult`·`fireSummonHero`·`finishSummonResult` 와 같은 프레임에 같은 상태를 낸다.
    /// 수치는 정본 `ui.js` 상수(`SR_CHARGE_MS` 240 · `SR_SLOW_STEP` 125 · `SR_HOLDBACK_MS` 150 · `SR_TAIL_MS` 150 · 주역 비트 350ms)로 기대값을 셈한다.</summary>
    public class SummonSeqRulesTests
    {
        const double Charge = 240, Slow = 125, Hold = 150, Tail = 150, Kick = 350;

        static SummonSeqRun Solo(bool holdback, int heroIdx)
        {
            // 셋 — 마지막이 최고 등급(순위 5) · 정본 지연식 charge + i*slow + (holdback && 마지막 ? hold : 0)
            double[] d = { Charge, Charge + Slow, Charge + 2 * Slow + (holdback ? Hold : 0) };
            int[] r = { 0, 1, 5 };
            return new SummonSeqRun(d, r, holdback, heroIdx, Tail, Kick);
        }

        [Test]
        public void 홀드백_단독_주역은_마지막_한_칸에서_charging_착지_프레임에_hero_flash_그리고_tail_뒤_done()
        {
            SummonSeqRun run = Solo(true, 2);
            Assert.AreEqual(-1, run.Tick(0)); Assert.AreEqual(0, run.Revealed); Assert.IsFalse(run.Charging);
            Assert.AreEqual(0, run.Tick(300)); Assert.AreEqual(1, run.Revealed); Assert.IsFalse(run.Charging, "둘 남았으면 아직 축적이 아니다");
            Assert.AreEqual(1, run.Tick(400)); Assert.AreEqual(2, run.Revealed); Assert.IsTrue(run.Charging, "마지막 한 칸을 남긴 순간부터 charging");
            Assert.AreEqual(-1, run.Tick(600)); Assert.IsTrue(run.Charging); Assert.IsFalse(run.Hero); Assert.Less(run.FinishAtMs, 0);
            Assert.AreEqual(5, run.Tick(650), "주역 착지 프레임 — 효과음은 그 등급");
            Assert.AreEqual(3, run.Revealed);
            Assert.IsTrue(run.Hero); Assert.AreEqual(650, run.HeroAtMs);
            Assert.IsTrue(run.Flash, "뜸들인 단독 등장 = 전 화면 섬광"); Assert.IsFalse(run.Wipe);
            Assert.IsFalse(run.Charging, "fireSummonHero 가 charging 을 뗀다");
            Assert.AreEqual(650 + Tail, run.FinishAtMs, "전부 뜬 시각 + SR_TAIL_MS");
            Assert.IsFalse(run.Done);
            run.Tick(799); Assert.IsFalse(run.Done);
            run.Tick(800); Assert.IsTrue(run.Done, "tail 이 지나면 done");
            Assert.IsTrue(run.HeroKicking(900)); Assert.IsFalse(run.HeroKicking(650 + Kick), "주역 비트는 350ms");
        }

        [Test]
        public void 대량_소환은_홀드백이_없어_charging_없이_주역_착지에_wipe_다()
        {
            SummonSeqRun run = Solo(false, 2);
            run.Tick(300); run.Tick(400);
            Assert.AreEqual(2, run.Revealed); Assert.IsFalse(run.Charging, "홀드백이 없으면 축적 구간이 없다");
            Assert.AreEqual(5, run.Tick(500));
            Assert.IsTrue(run.Hero); Assert.IsTrue(run.Wipe, "정본 주석: 전 화면 섬광 대신 주역 셀 중심 가산 원형 와이프"); Assert.IsFalse(run.Flash);
        }

        [Test]
        public void 주역이_없으면_hero_는_안_뜨고_tail_뒤_done_만()
        {
            SummonSeqRun run = Solo(false, -1);
            Assert.AreEqual(5, run.Tick(1000));
            Assert.AreEqual(3, run.Revealed); Assert.IsFalse(run.Hero); Assert.IsFalse(run.Flash); Assert.IsFalse(run.Wipe);
            Assert.AreEqual(1150, run.FinishAtMs);
            run.Tick(1149); Assert.IsFalse(run.Done);
            run.Tick(1150); Assert.IsTrue(run.Done);
        }

        [Test]
        public void 밀린_프레임은_한_번에_따라잡고_효과음은_그_중_최고_등급_하나다()
        {
            SummonSeqRun run = Solo(true, 2);
            Assert.AreEqual(5, run.Tick(2000), "셋이 한 프레임에 몰려도 최고 등급 하나");
            Assert.AreEqual(3, run.Revealed);
            Assert.IsTrue(run.Hero); Assert.IsTrue(run.Flash);
            Assert.AreEqual(2000 + Tail, run.FinishAtMs);
            Assert.AreEqual(-1, run.Tick(2100), "다시 부르면 뜬 것이 없다");
            Assert.AreEqual(3, run.Revealed);
        }

        [Test]
        public void done_뒤에는_상태가_더_안_움직인다()
        {
            SummonSeqRun run = Solo(true, 2);
            run.Tick(5000);
            Assert.IsFalse(run.Done, "전부 뜬 프레임에는 아직 — done 은 tail 뒤(정본 setTimeout)");
            run.Tick(5000 + Tail);
            Assert.IsTrue(run.Done);
            double heroAt = run.HeroAtMs;
            run.Tick(9000);
            Assert.IsTrue(run.Done); Assert.IsFalse(run.Charging); Assert.AreEqual(heroAt, run.HeroAtMs);
        }

        [Test]
        public void 지연표와_등급표_길이가_다르면_거부한다()
        {
            Assert.Throws<System.ArgumentException>(() => new SummonSeqRun(new double[] { 1, 2 }, new int[] { 0 }, false, -1, Tail, Kick));
        }
    }

    /// <summary>T334 3회차 ⓑ — 충전 구간 키프레임 넷(정본 `style.css` 6815~6879)이 표에서 그대로 서고 정본 곡선대로 나온다.</summary>
    public class SummonChargeSpecTests
    {
        static SummonChargeSpec spec;
        static SummonChargeSpec S()
        {
            if (spec == null)
            {
                string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
                spec = SummonChargeSpec.From(MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "Assets", "Forge", "Resources", "SummonFxUi.json"))));
            }
            return spec;
        }

        [Test]
        public void 소환진은_부풀다_마지막_12퍼센트에_수축하고_밝기는_끝까지_오른다()
        {
            SummonChargeSpec s = S();
            double peak = s.FloorAt(s.ChargeMs * 0.88, "scale"), end = s.FloorAt(s.ChargeMs, "scale");
            Assert.Greater(peak, 1.05, "88% 에 가장 부풀어 있어야 한다");
            Assert.Less(end, 1.0, "마지막 12% 는 **수축** — 그 반동으로 주역이 터진다(정본 주석 «지우지 말 것»)");
            Assert.Greater(s.FloorAt(s.ChargeMs, "bright"), s.FloorAt(s.ChargeMs * 0.88, "bright"), "밝기는 끝까지 오른다 — 수축이 «꺼진다» 로 읽히면 안 된다");
        }

        [Test]
        public void 어느_구간에서도_값이_멈추지_않는다()
        {
            // 정본이 세 번 못 박은 것: ease-in 을 쓰면 앞 절반이 정지 프레임이 된다. linear + 촘촘한 키프레임이라야 한다.
            SummonChargeSpec s = S();
            double step = s.ChargeMs / 12.0, prevFloor = s.FloorAt(0, "bright"), prevVig = s.VigAt(0, "alpha");
            for (double t = step; t <= s.ChargeMs + 1e-9; t += step)
            {
                double f = s.FloorAt(t, "bright"), v = s.VigAt(t, "alpha");
                Assert.Greater(f - prevFloor, 1e-4, t + "ms 창에서 소환진 밝기가 멈췄다");
                Assert.Greater(v - prevVig, 1e-4, t + "ms 창에서 비네트가 멈췄다");
                prevFloor = f; prevVig = v;
            }
        }

        [Test]
        public void 중앙_광원은_여러_산을_그리고_간격이_좁아진다()
        {
            SummonChargeSpec s = S();
            var peaks = new List<double>();
            double prev = s.HaloAt(0, "alpha");
            bool rising = false;
            for (int i = 1; i <= 400; i++)
            {
                double t = s.ChargeMs * i / 400.0, a = s.HaloAt(t, "alpha");
                if (a < prev && rising) peaks.Add(t);
                rising = a > prev;
                prev = a;
            }
            // 마지막 «산» 은 내려오지 않는다 — 끝값이 곧 최고점(alpha 1)이라 그대로 주역 등장으로 넘어간다.
            peaks.Add(s.ChargeMs);
            Assert.GreaterOrEqual(peaks.Count, 3, "대기창 안에 산이 여럿 들어와야 «축적» 으로 읽힌다(한 주기로는 안 읽혔다 — 정본 주석)");
            // 정본 주석의 «132→110→99→88→77ms» 는 **의도**고, 실제 키프레임(12·44·80·100%)의 산 간격은
            // 89.6 · 100.8 · 56ms 다 — 단조로 좁아지지 않는다. 자는 실제 표를 지킨다: **마지막 박이 가장 빠르다**.
            double last = peaks[peaks.Count - 1] - peaks[peaks.Count - 2];
            for (int i = 1; i < peaks.Count - 1; i++)
                Assert.Less(last, peaks[i] - peaks[i - 1] + 1e-6, "마지막 산까지의 간격이 가장 짧아야 «조여든다» 로 읽힌다");
            Assert.AreEqual(1.0, s.HaloAt(s.ChargeMs, "alpha"), 1e-9, "끝은 완전 불투명");
            Assert.Greater(s.HaloAt(s.ChargeMs, "bright"), 2.9, "끝 밝기 2.95");
        }

        [Test]
        public void 눈금은_아홉_칸_계단이다()
        {
            SummonChargeSpec s = S();
            Assert.AreEqual(s.TickA0, s.TickAlpha(0), 1e-9);
            Assert.AreEqual(s.TickAlpha(s.ChargeMs * 0.02), s.TickAlpha(s.ChargeMs * 0.10), 1e-9, "한 칸 안에서는 안 움직인다(steps)");
            Assert.AreNotEqual(s.TickAlpha(s.ChargeMs * 0.10), s.TickAlpha(s.ChargeMs * 0.13), "칸을 넘으면 뛴다");
            Assert.AreEqual(s.TickA1, s.TickAlpha(s.ChargeMs), 1e-9, "끝은 완전 점등");
            // 도는 동안(진행 < 100%)의 계단은 아홉이다 — 끝값(TickA1)은 `forwards` 가 100% 키프레임에서 가져오는
            // 열 번째 값이라 세지 않는다(CSS `steps(9, end)` 가 그렇다).
            var seen = new List<double>();
            for (int i = 0; i < 300; i++)
            {
                double a = s.TickAlpha(s.ChargeMs * i / 300.0);
                if (seen.Count == 0 || System.Math.Abs(seen[seen.Count - 1] - a) > 1e-9) seen.Add(a);
            }
            Assert.AreEqual(9, seen.Count, "칸은 아홉이다(정본 steps(9))");
        }

        [Test]
        public void 조연_셀은_광원_쪽으로_빨려들며_작아진다()
        {
            SummonChargeSpec s = S();
            Assert.AreEqual(0.0, s.InhaleAt(0, "back_f"), 1e-9, "0% 는 srpop 의 끝 상태 그대로여야 한 프레임도 안 튄다");
            Assert.AreEqual(1.0, s.InhaleAt(0, "scale"), 1e-9);
            Assert.Greater(s.InhaleAt(s.ChargeMs, "back_f"), s.InhaleAt(s.ChargeMs * 0.5, "back_f"));
            Assert.Less(s.InhaleAt(s.ChargeMs, "scale"), 0.96);
            Assert.Less(s.InhaleAt(s.ChargeMs, "bright"), s.InhaleAt(0, "bright"), "조연은 어두워진다 — 주역이 밝을 자리를 낸다");
        }

        [Test]
        public void 끝난_뒤에는_마지막_칸에_머문다()
        {
            SummonChargeSpec s = S();
            Assert.AreEqual(100.0, s.Percent(s.ChargeMs * 3), 1e-9, "정본 forwards");
            Assert.AreEqual(s.FloorAt(s.ChargeMs, "scale"), s.FloorAt(s.ChargeMs * 3, "scale"), 1e-9);
        }

        [Test]
        public void 표에_ease_를_두면_거부한다()
        {
            // 이 구간의 가속감은 이징이 아니라 키프레임 간격이 만든다 — 정본이 세 번 못 박은 자리라 자가 지킨다.
            string bad = "{\u0022charge\u0022:{\u0022charge_ms\u0022:280,\u0022tick_steps\u0022:9,\u0022tick_a0\u0022:0.45,\u0022tick_a1\u0022:1,"
                + "\u0022vig_rx_f\u0022:0.82,\u0022vig_ry_f\u0022:0.51,\u0022vig_cy_f\u0022:0.44,\u0022vig_inner_f\u0022:0.12,\u0022vig_outer_a\u0022:0.9,\u0022vig_bake_px\u0022:256,"
                + "\u0022srfloorcharge\u0022:[{\u0022at\u0022:0,\u0022scale\u0022:1,\u0022bright\u0022:1,\u0022sat\u0022:1,\u0022ease\u0022:\u0022ease-in\u0022},{\u0022at\u0022:100,\u0022scale\u0022:1,\u0022bright\u0022:2,\u0022sat\u0022:1}],"
                + "\u0022srhalocharge\u0022:[],\u0022srvig\u0022:[],\u0022srinhale\u0022:[]}}";
            Assert.Throws<System.FormatException>(() => SummonChargeSpec.From(MiniJson.ParseObject(bad)));
        }
    }

    /// <summary>T334 5회차 — 완료 뒤 아이들 호흡이 표대로 서고 **등급이 오를수록 크게 숨쉰다**(정본 «위계가 구조여야 한다»).</summary>
    public class SummonIdleSpecTests
    {
        static SummonIdleSpec spec;
        static SummonIdleSpec S()
        {
            if (spec == null)
            {
                string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
                spec = SummonIdleSpec.From(MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "Assets", "Forge", "Resources", "SummonFxUi.json"))));
            }
            return spec;
        }

        [Test]
        public void 등급이_오를수록_크게_숨쉰다()
        {
            SummonIdleSpec s = S();
            double lowTy, lowAdd, hiTy, hiAdd;
            s.At(s.IdleMs * 0.5, 0, 0, out lowTy, out lowAdd);
            s.At(s.IdleMs * 0.5, 0, s.Tier.Length - 1, out hiTy, out hiAdd);
            Assert.Greater(hiAdd, lowAdd, "최고 등급이 가장 크게 부푼다");
            Assert.Less(hiTy, lowTy, "그리고 더 높이 뜬다(음수가 더 크다)");
            Assert.AreEqual(s.ScaleF * s.Weight(s.Tier.Length - 1), hiAdd, 1e-9, "정점 = scale_f × 등급 무게");
        }

        [Test]
        public void 정점은_한가운데고_양_끝은_제자리다()
        {
            SummonIdleSpec s = S();
            double ty, add;
            Assert.AreEqual(0.0, s.Phase(0, 0, out ty, out add), 1e-9, "0% 는 제자리");
            Assert.AreEqual(1.0, s.Phase(s.IdleMs * 0.5, 0, out ty, out add), 1e-9, "50% 가 정점");
            Assert.AreEqual(0.0, s.Phase(s.IdleMs, 0, out ty, out add), 1e-9, "100% 는 다시 제자리");
        }

        [Test]
        public void 셀마다_늦게_시작해_물결이_된다()
        {
            SummonIdleSpec s = S();
            double ty0, a0, ty1, a1;
            s.At(s.DelayStepMs, 0, 2, out ty0, out a0);
            s.At(s.DelayStepMs, 1, 2, out ty1, out a1);
            Assert.AreNotEqual(a0, a1, "같은 시각에 이웃 셀이 같은 자리면 물결이 아니다");
            Assert.AreEqual(0.0, a1, 1e-9, "둘째 셀은 그 시각에 막 시작한다(지연 .21s)");
        }

        [Test]
        public void 계단이_뒤집힌_표는_거부한다()
        {
            // 위계는 부산물이 아니라 **구조**여야 한다 — 표가 그 순서를 깨면 자가 막는다.
            char q = '"';
            string bad = "{" + q + "idle" + q + ":{" + q + "idle_ms" + q + ":2600," + q + "delay_step_ms" + q + ":210,"
                + q + "ty_rem" + q + ":-0.16," + q + "scale_f" + q + ":0.055," + q + "weight" + q + ":[1.0,0.5]}}";
            Assert.Throws<System.FormatException>(() => SummonIdleSpec.From(MiniJson.ParseObject(bad)));
        }
    }
}
