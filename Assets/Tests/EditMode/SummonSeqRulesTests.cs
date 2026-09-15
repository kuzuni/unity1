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

    /// <summary>T334 6회차 — 주역 착지의 화면 킥이 표대로 서고 **제자리에서 시작해 제자리로 돌아온다**.</summary>
    public class SummonHeroSpecTests
    {
        static SummonHeroSpec spec;
        static SummonHeroSpec S()
        {
            if (spec == null)
            {
                string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
                spec = SummonHeroSpec.From(MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "Assets", "Forge", "Resources", "SummonFxUi.json"))));
            }
            return spec;
        }

        [Test]
        public void 제자리에서_시작해_제자리로_돌아온다()
        {
            SummonHeroSpec s = S();
            double tx, ty, sc;
            s.At(0, out tx, out ty, out sc);
            Assert.AreEqual(0.0, tx, 1e-9); Assert.AreEqual(0.0, ty, 1e-9); Assert.AreEqual(1.0, sc, 1e-9);
            s.At(s.ShakeMs, out tx, out ty, out sc);
            Assert.AreEqual(0.0, tx, 1e-9); Assert.AreEqual(0.0, ty, 1e-9); Assert.AreEqual(1.0, sc, 1e-9);
            s.At(s.ShakeMs * 5, out tx, out ty, out sc);
            Assert.AreEqual(0.0, tx, 1e-9, "다 흔든 뒤에도 제자리(정본 both 필의 마지막 키가 «없음»)");
        }

        [Test]
        public void 첫_흔들림이_가장_세고_점점_잦아든다()
        {
            // 정본 주석: «최고 등급 착지 — 앞의 것보다 짧고 세게». 12% 가 정점이고 뒤로 갈수록 폭이 준다.
            SummonHeroSpec s = S();
            double a, b, c, tx, ty, sc;
            s.At(s.ShakeMs * 0.12, out tx, out ty, out sc); a = System.Math.Abs(tx) + System.Math.Abs(ty);
            s.At(s.ShakeMs * 0.28, out tx, out ty, out sc); b = System.Math.Abs(tx) + System.Math.Abs(ty);
            s.At(s.ShakeMs * 0.46, out tx, out ty, out sc); c = System.Math.Abs(tx) + System.Math.Abs(ty);
            Assert.Greater(a, b); Assert.Greater(b, c);
            s.At(s.ShakeMs * 0.12, out tx, out ty, out sc);
            Assert.Greater(sc, 1.0, "정점에서는 살짝 커진다");
        }

        [Test]
        public void 흔드는_중인가를_길이로_가른다()
        {
            SummonHeroSpec s = S();
            Assert.IsTrue(s.Kicking(0));
            Assert.IsTrue(s.Kicking(s.ShakeMs - 1));
            Assert.IsFalse(s.Kicking(s.ShakeMs), "길이를 넘으면 끝");
            Assert.IsFalse(s.Kicking(-1));
        }

        [Test]
        public void 조연은_물러났다_머물다_돌아온다()
        {
            // 정본 주석: «나머지를 물리고(후퇴) … «다른 사건» 으로 만든다». 18~58% 가 평지라 물러난 채로 머문다.
            SummonHeroSpec s = S();
            double sc0, sa0, br0, sc1, sa1, br1, sc2, sa2, br2, sc3, sa3, br3;
            s.RecedeAt(0, out sc0, out sa0, out br0);
            s.RecedeAt(s.RecedeMs * 0.30, out sc1, out sa1, out br1);
            s.RecedeAt(s.RecedeMs * 0.50, out sc2, out sa2, out br2);
            s.RecedeAt(s.RecedeMs, out sc3, out sa3, out br3);
            Assert.AreEqual(1.0, sc0, 1e-9, "시작은 제자리");
            Assert.Less(sc1, 1.0, "물러난다");
            Assert.Less(br1, 1.0, "어두워진다");
            Assert.Less(sa1, 1.0, "채도도 빠진다");
            Assert.AreEqual(sc1, sc2, 1e-9, "18~58% 는 평지 — 물러난 채로 머문다");
            Assert.AreEqual(1.0, sc3, 1e-9, "끝나면 제자리");
            Assert.AreEqual(1.0, br3, 1e-9);
            Assert.AreEqual(1.0, sa3, 1e-9);
            Assert.IsTrue(s.Receding(s.RecedeMs - 1));
            Assert.IsFalse(s.Receding(s.RecedeMs));
        }

        [Test]
        public void 주역_등장은_넘쳤다가_1_0_으로_정착한다()
        {
            // 정본 주석: «더 길고 더 크게 넘치고, 끝에서 원래 크기로 안 돌아온다(무대에 남는다)» +
            //   «정착 스케일을 1보다 크게 두면 셀 폭을 넘는 이름판이 옆 셀 이름과 겹친다 … 큰 몸집은 등급 계단이 맡는다».
            SummonHeroSpec s = S();
            double bf, ty, sc, op;
            s.HeroPopAt(0, out bf, out ty, out sc, out op);
            Assert.AreEqual(0.0, op, 1e-9, "시작은 안 보인다");
            Assert.Less(sc, 0.5, "아주 작게 시작한다");
            Assert.Greater(bf, 0.0, "슬롯 → 광원 벡터에서 날아온다");
            s.HeroPopAt(s.HeroPopMs * 0.42, out bf, out ty, out sc, out op);
            Assert.Greater(sc, 1.2, "42% 에 크게 넘친다");
            Assert.Less(ty, 0.0, "그 순간 살짝 떠오른다(위로)");
            s.HeroPopAt(s.HeroPopMs, out bf, out ty, out sc, out op);
            Assert.AreEqual(1.0, sc, 1e-9, "정착은 1.0 — 1보다 크면 이름판이 옆 셀과 겹친다(정본 실측)");
            Assert.AreEqual(0.0, bf, 1e-9); Assert.AreEqual(0.0, ty, 1e-9);
            Assert.IsTrue(s.HeroPopping(s.HeroPopMs - 1));
            Assert.IsFalse(s.HeroPopping(s.HeroPopMs));
        }

        [Test]
        public void 광창은_떠올랐다_사라지고_돌면서_커진다()
        {
            SummonHeroSpec s = S();
            double a0, sc0, r0, a1, sc1, r1, a2, sc2, r2;
            s.BeamAt(0, out a0, out sc0, out r0);
            s.BeamAt(s.BeamMs * 0.24, out a1, out sc1, out r1);
            s.BeamAt(s.BeamMs, out a2, out sc2, out r2);
            Assert.AreEqual(0.0, a0, 1e-9, "안 보이게 시작");
            Assert.Greater(a1, 0.9, "24% 가 정점");
            Assert.AreEqual(0.0, a2, 1e-9, "사라진다 — 안 그러면 빛기둥이 남는다");
            Assert.Less(sc0, sc1); Assert.Less(sc1, sc2, "내내 커진다");
            Assert.Less(r0, r2, "−9° 에서 9° 로 돈다");
            Assert.IsTrue(s.Beaming(s.BeamMs - 1));
            Assert.IsFalse(s.Beaming(s.BeamMs));
        }

        [Test]
        public void 하이라이트는_등급이_오를수록_더_희게_당긴다()
        {
            // 정본 srHilite: 색을 등급마다 새로 고르지 않고 «목표 휘도까지 당기는 양» 만 등급으로 가른다.
            SummonHeroSpec s = S();
            // 궁극의(#ff1c1c) 로 잰다 — 낮은 등급일수록 덜 당긴다.
            double lo = s.HiliteAmount(255, 28, 28, 0), hi = s.HiliteAmount(255, 28, 28, 5);
            Assert.Greater(hi, lo, "등급이 오를수록 더 희게 당긴다");
            Assert.GreaterOrEqual(lo, 0.0); Assert.LessOrEqual(hi, 1.0);
            // 이미 아주 밝은 색은 더 당길 것이 거의 없다.
            Assert.Less(s.HiliteAmount(250, 250, 250, 0), s.HiliteAmount(60, 60, 60, 0));
        }

        [Test]
        public void 정착_배율이_1_이_아닌_표는_거부한다()
        {
            char q = '"';
            string head = "{" + q + "hero" + q + ":{" + q + "shake_ms" + q + ":440," + q + "shake_ease" + q + ":[0.2,0.9,0.3,1],"
                + q + "srshakehit" + q + ":[{" + q + "at" + q + ":0," + q + "tx_pct" + q + ":0," + q + "ty_pct" + q + ":0," + q + "scale" + q + ":1},"
                + "{" + q + "at" + q + ":100," + q + "tx_pct" + q + ":0," + q + "ty_pct" + q + ":0," + q + "scale" + q + ":1}],"
                + q + "recede_ms" + q + ":680," + q + "recede_ease" + q + ":[0.3,0.85,0.35,1],"
                + q + "srrecede" + q + ":[{" + q + "at" + q + ":0," + q + "scale" + q + ":1," + q + "sat" + q + ":1," + q + "bright" + q + ":1},"
                + "{" + q + "at" + q + ":100," + q + "scale" + q + ":1," + q + "sat" + q + ":1," + q + "bright" + q + ":1}],"
                + q + "heropop_ms" + q + ":520," + q + "heropop_ease" + q + ":[0.16,1.5,0.36,1]," + q + "heropop_dy0_rem" + q + ":0.9,"
                + q + "srheropop" + q + ":[{" + q + "at" + q + ":0," + q + "back_f" + q + ":1," + q + "ty_rem" + q + ":0.9," + q + "scale" + q + ":0.42," + q + "alpha" + q + ":0},";
            string bad = head + "{" + q + "at" + q + ":100," + q + "back_f" + q + ":0," + q + "ty_rem" + q + ":0," + q + "scale" + q + ":1.18," + q + "alpha" + q + ":1}]}}";
            Assert.Throws<System.FormatException>(() => SummonHeroSpec.From(MiniJson.ParseObject(bad)));
        }

        [Test]
        public void 물러난_채_굳는_표는_거부한다()
        {
            // 마지막 키가 1/1/1 이 아니면 결과 화면이 어두운 채로 굳는다.
            char q = '"';
            string bad = "{" + q + "hero" + q + ":{" + q + "shake_ms" + q + ":440," + q + "shake_ease" + q + ":[0.2,0.9,0.3,1],"
                + q + "srshakehit" + q + ":[{" + q + "at" + q + ":0," + q + "tx_pct" + q + ":0," + q + "ty_pct" + q + ":0," + q + "scale" + q + ":1},"
                + "{" + q + "at" + q + ":100," + q + "tx_pct" + q + ":0," + q + "ty_pct" + q + ":0," + q + "scale" + q + ":1}],"
                + q + "recede_ms" + q + ":680," + q + "recede_ease" + q + ":[0.3,0.85,0.35,1],"
                + q + "srrecede" + q + ":[{" + q + "at" + q + ":0," + q + "scale" + q + ":1," + q + "sat" + q + ":1," + q + "bright" + q + ":1},"
                + "{" + q + "at" + q + ":100," + q + "scale" + q + ":0.9," + q + "sat" + q + ":1," + q + "bright" + q + ":1}]}}";
            Assert.Throws<System.FormatException>(() => SummonHeroSpec.From(MiniJson.ParseObject(bad)));
        }

        [Test]
        public void 제자리로_안_돌아오는_표는_거부한다()
        {
            // 마지막 키가 0 이 아니면 판이 튄 채로 남는다 — 표에서 막는다.
            char q = '"';
            string bad = "{" + q + "hero" + q + ":{" + q + "shake_ms" + q + ":440," + q + "shake_ease" + q + ":[0.2,0.9,0.3,1],"
                + q + "srshakehit" + q + ":[{" + q + "at" + q + ":0," + q + "tx_pct" + q + ":0," + q + "ty_pct" + q + ":0," + q + "scale" + q + ":1},"
                + "{" + q + "at" + q + ":100," + q + "tx_pct" + q + ":1," + q + "ty_pct" + q + ":0," + q + "scale" + q + ":1}]}}";
            Assert.Throws<System.FormatException>(() => SummonHeroSpec.From(MiniJson.ParseObject(bad)));
        }
    }

    /// <summary>T334 11회차 — 셀별 광원 재점화(정본 `.sr-relight`)가 표대로 켜졌다 꺼지고 **등급이 오를수록 세다**.</summary>
    public class SummonRelightSpecTests
    {
        static SummonRelightSpec spec;
        static SummonRelightSpec S()
        {
            if (spec == null)
            {
                string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
                spec = SummonRelightSpec.From(MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "Assets", "Forge", "Resources", "SummonFxUi.json"))));
            }
            return spec;
        }

        [Test]
        public void 켜졌다_꺼지고_커지며_사라진다()
        {
            SummonRelightSpec s = S();
            double a0, sc0, aPk, scPk, a1, sc1;
            s.At(0, 5, out a0, out sc0);
            s.At(s.Ms * 0.24, 5, out aPk, out scPk);
            s.At(s.Ms, 5, out a1, out sc1);
            Assert.AreEqual(0.0, a0, 1e-9, "재점화는 꺼진 채로 시작한다");
            Assert.AreEqual(0.0, a1, 1e-9, "재점화는 꺼진 채로 끝난다 — 안 그러면 결과 화면 가운데에 등급색 얼룩이 남는다");
            Assert.Greater(aPk, 0.5, "24% 에 정점 — 정본 calc(.55 + .42 * --glow)");
            Assert.Less(sc0, scPk, "작게 시작해");
            Assert.Less(scPk, sc1, "끝까지 커지며 퍼진다");
        }

        [Test]
        public void 등급이_오를수록_세게_켜진다()
        {
            SummonRelightSpec s = S();
            double lo, hi, dummy;
            s.At(s.Ms * 0.24, 0, out lo, out dummy);
            s.At(s.Ms * 0.24, 5, out hi, out dummy);
            Assert.Greater(hi, lo, "정본 fillSummonRelights 의 --glow = 0.16 + tier * 0.13");
            Assert.LessOrEqual(hi, 1.0, "가산 판의 정점 알파가 1을 넘으면 광원이 하얗게 탄다");
            Assert.AreEqual(s.GlowBase, s.Glow(0), 1e-9);
            Assert.AreEqual(s.GlowBase + 3 * s.GlowStep, s.Glow(3), 1e-9);
        }

        [Test]
        public void 켜진_채로_끝나는_표는_거부한다()
        {
            // 마지막 키의 알파가 0 이 아니면 결과 화면 가운데에 등급색 얼룩이 남는다 — 표에서 막는다.
            char q = '"';
            string bad = "{" + q + "relight" + q + ":{" + q + "relight_ms" + q + ":340," + q + "relight_ease" + q + ":[0.1,0.9,0.3,1],"
                + q + "w_rem" + q + ":9," + q + "w_vw_f" + q + ":0.38," + q + "stop_lite" + q + ":0," + q + "stop_rc" + q + ":0.3,"
                + q + "stop_out" + q + ":0.7," + q + "blur_px" + q + ":3," + q + "glow_base" + q + ":0.16," + q + "glow_step" + q + ":0.13,"
                + q + "a_base" + q + ":0.55," + q + "a_glow" + q + ":0.42,"
                + q + "srrelight" + q + ":[{" + q + "at" + q + ":0," + q + "alpha_f" + q + ":0," + q + "scale" + q + ":0.28},"
                + "{" + q + "at" + q + ":100," + q + "alpha_f" + q + ":1," + q + "scale" + q + ":1.46}]}}";
            Assert.Throws<System.FormatException>(() => SummonRelightSpec.From(MiniJson.ParseObject(bad)));
        }

        [Test]
        public void 뒤집힌_정지점과_넘치는_알파를_거부한다()
        {
            char q = '"';
            string head = "{" + q + "relight" + q + ":{" + q + "relight_ms" + q + ":340," + q + "relight_ease" + q + ":[0.1,0.9,0.3,1],"
                + q + "w_rem" + q + ":9," + q + "w_vw_f" + q + ":0.38," + q + "blur_px" + q + ":3,"
                + q + "glow_base" + q + ":0.16," + q + "glow_step" + q + ":0.13,";
            string tail = q + "srrelight" + q + ":[{" + q + "at" + q + ":0," + q + "alpha_f" + q + ":0," + q + "scale" + q + ":0.28},"
                + "{" + q + "at" + q + ":100," + q + "alpha_f" + q + ":0," + q + "scale" + q + ":1.46}]}}";
            // 정지점이 뒤집히면 심지가 테두리에 서고 광원이 «도넛» 이 된다.
            string flip = head + q + "stop_lite" + q + ":0.5," + q + "stop_rc" + q + ":0.3," + q + "stop_out" + q + ":0.7,"
                + q + "a_base" + q + ":0.55," + q + "a_glow" + q + ":0.42," + tail;
            Assert.Throws<System.FormatException>(() => SummonRelightSpec.From(MiniJson.ParseObject(flip)));
            // 최고 등급에서 정점 알파가 1을 넘으면 등급색 구분이 하얗게 탄다.
            string over = head + q + "stop_lite" + q + ":0," + q + "stop_rc" + q + ":0.3," + q + "stop_out" + q + ":0.7,"
                + q + "a_base" + q + ":0.8," + q + "a_glow" + q + ":0.42," + tail;
            Assert.Throws<System.FormatException>(() => SummonRelightSpec.From(MiniJson.ParseObject(over)));
        }
    }

    /// <summary>T334 12회차 — 착지 스파크(정본 `.sr-spark`)가 표대로 터져 날아가 꺼진다.</summary>
    public class SummonSparkSpecTests
    {
        static SummonSparkSpec spec;
        static SummonSparkSpec S()
        {
            if (spec == null)
            {
                string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
                spec = SummonSparkSpec.From(MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "Assets", "Forge", "Resources", "SummonFxUi.json"))));
            }
            return spec;
        }

        [Test]
        public void 터져_날아가_꺼진다()
        {
            SummonSparkSpec s = S();
            double a0, sc0, a1, sc1;
            s.At(0, 5, out a0, out sc0);
            s.At(s.Ms, 5, out a1, out sc1);
            Assert.Greater(a0, 0.9, "신화는 거의 불투명하게 터진다(정본 .45 + .55 × 1)");
            Assert.AreEqual(0.0, a1, 1e-9, "끝에는 꺼진다 — 안 그러면 결과 화면에 흰 점 아홉이 남는다");
            Assert.Less(sc0, sc1, "심에서 바깥으로 날아간다");
            Assert.AreEqual(0.16, sc0, 1e-9, "정본 0% 배율");
        }

        [Test]
        public void 등급이_오를수록_세고_멀리_간다()
        {
            SummonSparkSpec s = S();
            double aLo, sLo, aHi, sHi;
            s.At(0, 0, out aLo, out sLo);
            s.At(0, 5, out aHi, out sHi);
            Assert.Greater(aHi, aLo, "정본 calc(.45 + .55 * --glow)");
            double eLo, eHi, dummy;
            s.At(s.Ms, 0, out dummy, out eLo);
            s.At(s.Ms, 5, out dummy, out eHi);
            Assert.Greater(eHi, eLo, "정본 calc(1.05 + .55 * --glow)");
            // 등급 계단은 CSS `.sr-cell` 의 --glow 다 — 재점화가 쓰는 0.16 + tier*0.13 과 다른 수다.
            Assert.AreEqual(0.0, s.Glow(0), 1e-9);
            Assert.AreEqual(1.0, s.Glow(5), 1e-9);
        }

        [Test]
        public void 복제_여덟은_축_넷이_등급색이고_대각_넷이_희다()
        {
            SummonSparkSpec s = S();
            int rarity = 0, white = 0;
            for (int i = 1; i <= 8; i++)
            {
                double x, y, r; bool isRc;
                s.Dot(i, out x, out y, out r, out isRc);
                Assert.Greater(r, 0.0, i + "번 복제가 퍼짐에 다 깎였다");
                double d = System.Math.Sqrt(x * x + y * y);
                if (isRc) { rarity++; Assert.AreEqual(s.AxisRem, d, 1e-9, "축 복제는 1.5rem"); }
                else { white++; Assert.AreEqual(System.Math.Sqrt(2) * s.DiagRem, d, 1e-9, "대각 복제는 1.06rem 씩"); }
            }
            Assert.AreEqual(4, rarity, "축 넷이 등급색");
            Assert.AreEqual(4, white, "대각 넷이 흰색");
            double cx, cy, cr; bool crc;
            s.Dot(0, out cx, out cy, out cr, out crc);
            Assert.AreEqual(0.0, cx, 1e-9); Assert.AreEqual(0.0, cy, 1e-9);
            Assert.AreEqual(s.DotRem * 0.5, cr, 1e-9, "심은 .22rem 지름");
            Assert.IsFalse(crc, "심은 흰색");
            // 판이 축 복제를 담는다 — 안 담으면 잘린 채로 커진다.
            Assert.GreaterOrEqual(s.BoxRem, (s.AxisRem + s.DotRem * 0.5) * 2);
        }

        [Test]
        public void 켜진_채_끝나거나_복제가_사라지는_표는_거부한다()
        {
            char q = '"';
            string tier = "{" + q + "tier" + q + ":{" + q + "glow" + q + ":[0,0.16,0.3,0.55,0.8,1]}," + q + "spark" + q + ":{";
            string body = q + "spark_ms" + q + ":420," + q + "spark_ease" + q + ":[0.14,0.82,0.3,1],"
                + q + "dot_rem" + q + ":0.22," + q + "axis_rem" + q + ":1.5," + q + "diag_rem" + q + ":1.06," + q + "box_rem" + q + ":3.22,";
            string sc = q + "srspark_s" + q + ":[{" + q + "at" + q + ":0," + q + "base" + q + ":0.16," + q + "glow" + q + ":0},"
                + "{" + q + "at" + q + ":100," + q + "base" + q + ":1.05," + q + "glow" + q + ":0.55}]}}";
            string spreadOk = q + "axis_spread_rem" + q + ":[-0.02,-0.02,-0.05,-0.02]," + q + "diag_spread_rem" + q + ":[-0.06,-0.07,-0.06,-0.07],";
            // ⑴ 마지막 알파가 0 이 아니면 흰 점 아홉이 남는다.
            string lit = tier + body + spreadOk
                + q + "srspark_a" + q + ":[{" + q + "at" + q + ":0," + q + "base" + q + ":0.45," + q + "glow" + q + ":0.55},"
                + "{" + q + "at" + q + ":100," + q + "base" + q + ":0.2," + q + "glow" + q + ":0}]," + sc;
            Assert.Throws<System.FormatException>(() => SummonSparkSpec.From(MiniJson.ParseObject(lit)));
            // ⑵ 퍼짐이 심 반지름을 다 깎으면 그 복제는 화면에서 사라진다.
            string gone = tier + body
                + q + "axis_spread_rem" + q + ":[-0.02,-0.02,-0.20,-0.02]," + q + "diag_spread_rem" + q + ":[-0.06,-0.07,-0.06,-0.07],"
                + q + "srspark_a" + q + ":[{" + q + "at" + q + ":0," + q + "base" + q + ":0.45," + q + "glow" + q + ":0.55},"
                + "{" + q + "at" + q + ":100," + q + "base" + q + ":0," + q + "glow" + q + ":0}]," + sc;
            Assert.Throws<System.FormatException>(() => SummonSparkSpec.From(MiniJson.ParseObject(gone)));
        }
    }

    /// <summary>T334 15회차 — 비행 잔상(정본 `.sr-ghost`)이 광원 쪽에 뒤처졌다가 따라붙어 사라진다.</summary>
    public class SummonGhostSpecTests
    {
        static SummonGhostSpec spec;
        static SummonGhostSpec S()
        {
            if (spec == null)
            {
                string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
                spec = SummonGhostSpec.From(MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "Assets", "Forge", "Resources", "SummonFxUi.json"))));
            }
            return spec;
        }

        [Test]
        public void 뒤처졌다가_따라붙어_사라진다()
        {
            SummonGhostSpec s = S();
            const double pop = 440;   // 전설 셀의 팝 길이 — 잔상은 제 길이가 없고 이것을 그대로 쓴다
            double b0, sc0, a0, bMid, scMid, aMid, b1, sc1, a1;
            s.At(0, pop, 5, out b0, out sc0, out a0);
            s.At(pop * 0.4, pop, 5, out bMid, out scMid, out aMid);
            s.At(pop, pop, 5, out b1, out sc1, out a1);
            Assert.Greater(b0, 0.0, "꼬리는 광원 쪽에 뒤처져 시작한다");
            Assert.Less(bMid, b0, "따라붙는다");
            Assert.AreEqual(0.0, b1, 1e-9, "끝에는 본체 자리에 겹친다");
            Assert.AreEqual(1.0, sc1, 1e-9, "끝 배율은 제자리");
            Assert.Greater(a0, 0.0);
            Assert.AreEqual(0.0, a1, 1e-9, "꺼진다 — 안 그러면 결과 화면에 흐린 원이 남는다");
        }

        [Test]
        public void 길이는_제_것이_없고_셀의_팝을_따른다()
        {
            SummonGhostSpec s = S();
            // 같은 «진행 비율» 이면 길이가 달라도 같은 값이 나온다 — 그것이 `var(--pop)` 계약이다.
            double bA, scA, aA, bB, scB, aB;
            s.At(320 * 0.5, 320, 3, out bA, out scA, out aA);
            s.At(520 * 0.5, 520, 3, out bB, out scB, out aB);
            Assert.AreEqual(bA, bB, 1e-9);
            Assert.AreEqual(aA, aB, 1e-9);
            // 그리고 길이를 넘기면 끝난 자리에 머문다.
            double b1, sc1, a1;
            s.At(9999, 320, 3, out b1, out sc1, out a1);
            Assert.AreEqual(0.0, a1, 1e-9);
        }

        [Test]
        public void 등급이_오를수록_짙다()
        {
            SummonGhostSpec s = S();
            double b, sc, aLo, aHi;
            s.At(0, 400, 0, out b, out sc, out aLo);
            s.At(0, 400, 5, out b, out sc, out aHi);
            Assert.Greater(aHi, aLo, "정본 calc(.42 + .4 * --glow)");
            Assert.LessOrEqual(aHi, 1.0);
        }

        [Test]
        public void 앞서_나가거나_남아_굳는_표는_거부한다()
        {
            char q = '"';
            string head = "{" + q + "tier" + q + ":{" + q + "glow" + q + ":[0,0.16,0.3,0.55,0.8,1]}," + q + "ghost" + q + ":{"
                + q + "stop_lite" + q + ":0," + q + "stop_rc" + q + ":0.52," + q + "stop_out" + q + ":0.74," + q + "blur_px" + q + ":6,"
                + q + "ghost_ease" + q + ":[0,0,0.58,1]," + q + "srghost" + q + ":[";
            string k0 = "{" + q + "at" + q + ":0," + q + "back_f" + q + ":0.58," + q + "scale" + q + ":1.06," + q + "a_base" + q + ":0.42," + q + "a_glow" + q + ":0.4}";
            // ⑴ 꼬리가 진행 방향 앞으로 가면 «앞서 나간 잔상» 이 된다.
            string ahead = head + k0 + ",{" + q + "at" + q + ":50," + q + "back_f" + q + ":0.9," + q + "scale" + q + ":1.02," + q + "a_base" + q + ":0.2," + q + "a_glow" + q + ":0.2},"
                + "{" + q + "at" + q + ":100," + q + "back_f" + q + ":0," + q + "scale" + q + ":1," + q + "a_base" + q + ":0," + q + "a_glow" + q + ":0}]}}";
            Assert.Throws<System.FormatException>(() => SummonGhostSpec.From(MiniJson.ParseObject(ahead)));
            // ⑵ 마지막이 안 꺼지면 결과 화면에 흐린 원이 남는다.
            string lit = head + k0 + ",{" + q + "at" + q + ":100," + q + "back_f" + q + ":0," + q + "scale" + q + ":1," + q + "a_base" + q + ":0.1," + q + "a_glow" + q + ":0}]}}";
            Assert.Throws<System.FormatException>(() => SummonGhostSpec.From(MiniJson.ParseObject(lit)));
        }
    }

    /// <summary>T334 16회차 — 등급 챕터 펄스(정본 `.sr-tierpulse`)가 예고로 서서 달아올랐다 식는다.</summary>
    public class SummonTierBreakSpecTests
    {
        static SummonTierBreakSpec spec;
        static SummonTierBreakSpec S()
        {
            if (spec == null)
            {
                string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
                spec = SummonTierBreakSpec.From(MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "Assets", "Forge", "Resources", "SummonFxUi.json"))));
            }
            return spec;
        }

        [Test]
        public void 달아올랐다_식는다()
        {
            SummonTierBreakSpec s = S();
            Assert.AreEqual(0.0, s.AlphaAt(0, 5), 1e-9, "꺼진 채로 시작한다");
            Assert.Greater(s.AlphaAt(s.PulseMs * 0.2, 5), 0.0, "20% 에 정점");
            Assert.AreEqual(0.0, s.AlphaAt(s.PulseMs, 5), 1e-9, "식는다 — 안 그러면 화면이 등급색으로 물든 채 굳는다");
            Assert.AreEqual(0.0, s.AlphaAt(9999, 5), 1e-9, "끝난 뒤에도 꺼져 있다");
        }

        [Test]
        public void 세기는_재점화나_CSS_계단이_아니라_제_수다()
        {
            SummonTierBreakSpec s = S();
            // 정본 fillSummonTierBreaks 의 `0.15 + tier * 0.04` — 이 겹만의 수다.
            Assert.AreEqual(s.PkBase, s.Pk(0), 1e-9);
            Assert.AreEqual(s.PkBase + 5 * s.PkStep, s.Pk(5), 1e-9);
            Assert.Greater(s.AlphaAt(s.PulseMs * 0.2, 5), s.AlphaAt(s.PulseMs * 0.2, 0), "등급이 오를수록 세다");
            Assert.LessOrEqual(s.Pk(5), 1.0, "가산 판의 정점이 1을 넘으면 화면이 하얗게 탄다");
        }

        [Test]
        public void 예고는_경계_셀보다_앞서되_충전보다_앞서지_않는다()
        {
            SummonTierBreakSpec s = S();
            const double charge = 280;
            // 한참 뒤에 뜨는 경계 — 반 박자 앞이다.
            Assert.AreEqual(2000 - s.LeadMs, s.BreakAt(2000, charge), 1e-9);
            // 충전 직후에 뜨는 경계 — 충전 끝보다 앞으로는 안 당긴다(정본 Math.max).
            Assert.AreEqual(charge, s.BreakAt(charge + 10, charge), 1e-9);
            Assert.AreEqual(charge, s.BreakAt(0, charge), 1e-9);
        }

        [Test]
        public void 물든_채_굳거나_뒤집힌_정지점을_거부한다()
        {
            char q = '"';
            string head = "{" + q + "tierbreak" + q + ":{" + q + "break_lead_ms" + q + ":160," + q + "pulse_ms" + q + ":540,"
                + q + "pulse_ease" + q + ":[0,0,0.58,1]," + q + "inset_f" + q + ":-0.02,"
                + q + "pulse_rx" + q + ":1.2," + q + "pulse_ry" + q + ":0.9," + q + "pulse_cx" + q + ":0.5," + q + "pulse_cy" + q + ":0.42,";
            string pk = q + "pk_base" + q + ":0.15," + q + "pk_step" + q + ":0.04,";
            string ok = q + "srtierpulse" + q + ":[{" + q + "at" + q + ":0," + q + "f" + q + ":0},{" + q + "at" + q + ":20," + q + "f" + q + ":1},{" + q + "at" + q + ":100," + q + "f" + q + ":0}]}}";
            // ⑴ 마지막이 0 이 아니면 화면이 물든 채 굳는다.
            string lit = head + q + "stop_lite" + q + ":0," + q + "stop_rc" + q + ":0.3," + q + "stop_out" + q + ":0.64," + pk
                + q + "srtierpulse" + q + ":[{" + q + "at" + q + ":0," + q + "f" + q + ":0},{" + q + "at" + q + ":100," + q + "f" + q + ":1}]}}";
            Assert.Throws<System.FormatException>(() => SummonTierBreakSpec.From(MiniJson.ParseObject(lit)));
            // ⑵ 정지점이 뒤집히면 심지가 테두리에 선다.
            string flip = head + q + "stop_lite" + q + ":0.5," + q + "stop_rc" + q + ":0.3," + q + "stop_out" + q + ":0.64," + pk + ok;
            Assert.Throws<System.FormatException>(() => SummonTierBreakSpec.From(MiniJson.ParseObject(flip)));
        }
    }
}
