using NUnit.Framework;
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
}
