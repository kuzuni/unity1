using System;
using NUnit.Framework;
using Forge.Core.Save;

namespace Forge.Tests
{
    /// <summary>
    /// T88 — 백그라운드 복귀 따라잡기(원작 `main.js` 180~191 · 111 · `state.js` offlineRewardFor). 원작은 숨은 구간의 전투 틱을 버리고 그 시간을
    /// 오프라인 수급(닫힌 식)으로 넘기며, 대장간·부화·연구는 절대시각 `endsAt` 이라 «지금 ≥ endsAt» 한 번으로 끝난다. 여기서는
    /// «잠든 시각·깨어난 시각을 주면 따라잡은 상태가 실제로 그만큼 돈 상태와 같은가» 를 30초 · 1시간 · 8시간 · 캡(4시간) 경계에서 잰다.
    /// </summary>
    public class CatchUpTests
    {
        static SaveDefs D { get { return SaveFixture.Defs; } }
        const double NOW = SaveVectors.NOW;

        static SaveState StateClaimedAgo(double agoSec)
        {
            var s = new SaveState(D.DefaultState(NOW - agoSec * 1000));
            s.LastOfflineClaim = NOW - agoSec * 1000;
            return s;
        }

        [Test]
        public void 삼십초_잠들면_백그라운드_구간이지만_팝업은_없다()
        {
            var s = StateClaimedAgo(30);
            ResumePlan p = Lifecycle.OnResume(D, s, NOW - 30 * 1000, NOW, OfflineMults.One);
            Assert.AreEqual(30 * 1000, p.SleptMs, 1e-9);
            Assert.IsTrue(p.Background, "5초 상한(main.js 191)보다 길면 백그라운드 구간");
            Assert.IsNotNull(p.Pending);
            Assert.AreEqual(30, p.Pending.Elapsed, 1e-9);
            Assert.IsFalse(p.ShowOffline, "1분 미만 누적은 팝업 생략(main.js 111)");
        }

        [Test]
        public void 한_시간_잠들면_닫힌_식_그대로_코인_3600_해머_60_이고_팝업이_뜬다()
        {
            var s = StateClaimedAgo(3600);
            ResumePlan p = Lifecycle.OnResume(D, s, NOW - 3600 * 1000, NOW, OfflineMults.One);
            Assert.IsTrue(p.ShowOffline);
            Assert.AreEqual(3600 * D.OfflineCoinPerSec, p.Pending.Coins, 1e-9);
            Assert.AreEqual(60 * D.OfflineHammerPerMin, p.Pending.Hammers, 1e-9);
            // «실제로 그만큼 돈 상태» — 초당 1틱씩 3600 번 누적한 값과 닫힌 식이 같다(정수 내림 · 소수 이월 없음 규칙은 식 쪽에 있다)
            double coinsTicked = 0, hammersTicked = 0;
            for (int sec = 1; sec <= 3600; sec++) { coinsTicked = Math.Floor(sec * D.OfflineCoinPerSec); hammersTicked = Math.Floor(sec / 60.0 * D.OfflineHammerPerMin); }
            Assert.AreEqual(coinsTicked, p.Pending.Coins, 1e-9);
            Assert.AreEqual(hammersTicked, p.Pending.Hammers, 1e-9);
        }

        [Test]
        public void 여덟_시간_잠들면_캡_4시간에서_멈춘다_경계_포함()
        {
            double cap = D.OfflineCapSec;
            var s8 = StateClaimedAgo(8 * 3600);
            ResumePlan p8 = Lifecycle.OnResume(D, s8, NOW - 8 * 3600 * 1000, NOW, OfflineMults.One);
            Assert.AreEqual(8 * 3600, p8.Pending.Elapsed, 1e-9, "경과는 캡 전 값");
            Assert.AreEqual(cap, p8.Pending.Counted, 1e-9, "센 시간은 캡");
            Assert.AreEqual(Math.Floor(cap * D.OfflineCoinPerSec), p8.Pending.Coins, 1e-9);
            Assert.AreEqual(Math.Floor(cap / 60 * D.OfflineHammerPerMin), p8.Pending.Hammers, 1e-9);
            var sCap = StateClaimedAgo(cap);
            var sCap1 = StateClaimedAgo(cap + 1);
            ResumePlan pCap = Lifecycle.OnResume(D, sCap, NOW - cap * 1000, NOW, OfflineMults.One);
            ResumePlan pCap1 = Lifecycle.OnResume(D, sCap1, NOW - (cap + 1) * 1000, NOW, OfflineMults.One);
            Assert.AreEqual(pCap.Pending.Coins, pCap1.Pending.Coins, 1e-9, "캡 +1초는 캡과 같은 코인");
            Assert.AreEqual(pCap.Pending.Hammers, pCap1.Pending.Hammers, 1e-9);
            // 기술트리 캡 배율(원작 TechTree.offlineCapMult)은 캡을 늘린다
            ResumePlan pMul = Lifecycle.OnResume(D, s8, NOW - 8 * 3600 * 1000, NOW, new OfflineMults(1.5, 1, 1));
            Assert.AreEqual(cap * 1.5, pMul.Pending.Counted, 1e-9);
        }

        [Test]
        public void 다섯_초_미만_공백은_전경_렉이라_팝업을_열지_않는다()
        {
            var s = StateClaimedAgo(3600);   // 누적은 충분하지만
            ResumePlan p = Lifecycle.OnResume(D, s, NOW - 4000, NOW, OfflineMults.One);
            Assert.IsFalse(p.Background, "5초 미만은 원작이 틱을 따라잡는 전경 구간");
            Assert.IsFalse(p.ShowOffline, "팝업은 백그라운드 복귀에만");
            Assert.IsNotNull(p.Pending, "누적분 자체는 남아 [수집] 버튼이 켠다(ui.js 6085)");
            Assert.IsFalse(Lifecycle.IsBackgroundGap(NOW - 4999, NOW));
            Assert.IsTrue(Lifecycle.IsBackgroundGap(NOW - 5000, NOW));
            Assert.AreEqual(5000, LifecycleRules.BackgroundGapMs, 1e-9);
            Assert.AreEqual(60, LifecycleRules.OfflinePopupMinSec, 1e-9);
        }

        [Test]
        public void 누적_기준은_잠든_시각이_아니라_lastOfflineClaim_이다()
        {
            var s = StateClaimedAgo(2 * 3600);   // 두 시간 전에 수집 · 10분 전에 잠듦
            ResumePlan p = Lifecycle.OnResume(D, s, NOW - 10 * 60 * 1000, NOW, OfflineMults.One);
            Assert.AreEqual(10 * 60 * 1000, p.SleptMs, 1e-9);
            Assert.AreEqual(2 * 3600, p.Pending.Elapsed, 1e-9, "원작 pendingOffline 은 lastOfflineClaim 기준");
            Assert.IsTrue(p.ShowOffline);
            // 시계가 뒤로 가거나 같은 순간이면 0 으로 잡고 팝업 없음
            ResumePlan back = Lifecycle.OnResume(D, s, NOW + 60000, NOW, OfflineMults.One);
            Assert.AreEqual(0, back.SleptMs, 1e-9);
            Assert.IsFalse(back.Background);
            // 상태가 없으면(부팅 전) 판정만 하고 누적은 null
            ResumePlan none = Lifecycle.OnResume(D, null, NOW - 3600 * 1000, NOW, OfflineMults.One);
            Assert.IsTrue(none.Background);
            Assert.IsNull(none.Pending);
            Assert.IsFalse(none.ShowOffline);
        }

        [Test]
        public void 절대시각_타이머는_깨어나는_순간_한_번의_비교로_초마다_돌린_것과_같다()
        {
            double paused = NOW - 3 * 3600 * 1000;
            double endsInside = paused + 25 * 60 * 1000;    // 잠든 사이 끝난 업그레이드
            double endsAfter = NOW + 7 * 60 * 1000;         // 깨어난 뒤 7분 남은 부화
            Assert.IsTrue(AbsTimer.IsDone(endsInside, NOW), "잠든 사이 끝난 것은 깨어나자마자 완료");
            Assert.IsFalse(AbsTimer.IsDone(endsAfter, NOW));
            Assert.AreEqual(7 * 60, AbsTimer.RemainingSec(endsAfter, NOW), 1e-9, "남은 시간은 벽시계 차이 그대로");
            // 초마다 1틱씩 돌렸을 때 완료를 보는 첫 초 == 닫힌 식(끝 시각) — 3시간 = 10800 틱을 돌지 않아도 같은 답
            double firstDoneTick = -1;
            for (int sec = 0; sec <= 3 * 3600; sec++) { if (AbsTimer.IsDone(endsInside, paused + sec * 1000)) { firstDoneTick = sec; break; } }
            Assert.AreEqual(25 * 60, firstDoneTick, 1e-9);
            Assert.AreEqual(Math.Ceiling((endsInside - paused) / 1000), firstDoneTick, 1e-9, "닫힌 식 = ceil((endsAt − 잠든 시각)/1000)");
        }
    }
}
