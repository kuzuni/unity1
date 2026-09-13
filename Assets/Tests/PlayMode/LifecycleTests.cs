using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T88 — 백그라운드 재생: 부팅하면 <c>Application.runInBackground</c> 가 켜져 있고 <see cref="AppLifecycle"/> 가 Bootstrap 아래에 서며,
    /// 잠들었다 깨어나면(OnApplicationPause 흉내) 원작 `visibilitychange`/부팅 규칙대로 — 1시간 뒤 복귀는 오프라인 팝업(누적 = lastOfflineClaim 기준 · 캡 4시간),
    /// 30초 뒤 복귀는 팝업 없이 화면이 이어진다 — 콘솔 빨강 0. 빨간 로그가 나면 러너가 실패시킨다.
    /// </summary>
    public class LifecycleTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && SaveIo.Ready) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready && SaveIo.Ready, "MetaHost/SaveIo 가 20초 안에 준비되지 않았다");
        }

        [UnityTest]
        public IEnumerator 부팅하면_runInBackground_가_켜지고_AppLifecycle_이_선다()
        {
            yield return Boot();
            Assert.IsTrue(Application.runInBackground, "Bootstrap.ApplyRunInBackground — 창이 초점을 잃어도 돈다(주인 지시)");
            AppLifecycle lc = AppLifecycle.Instance;
            Assert.IsNotNull(lc, "AppLifecycle 이 Bootstrap 아래에 서지 않았다");
            Assert.IsNotNull(Object.FindAnyObjectByType<Bootstrap>());
            Assert.AreEqual(Object.FindAnyObjectByType<Bootstrap>().transform, lc.transform.parent, "Bootstrap 아래");
            Assert.IsFalse(lc.PausedAtMs.HasValue);
        }

        [UnityTest]
        public IEnumerator 한_시간_잠들었다_깨면_오프라인_팝업이_열리고_30초면_열리지_않는다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            AppLifecycle lc = AppLifecycle.Instance;
            if (h.Popups.IsOpen(OfflinePopup.Name)) OfflinePopup.Close(h);
            yield return null;

            // 30초 — 백그라운드 구간(≥5초)이지만 누적 60초 미만: 팝업 없음(원작 main.js 111)
            double now = SaveIo.NowMs();
            SaveIo.State.LastOfflineClaim = now - 30 * 1000;
            lc.MarkPaused();
            Assert.IsTrue(lc.PausedAtMs.HasValue);
            ResumePlan p1 = lc.ResumeAt(now - 30 * 1000, now);
            yield return null;
            Assert.IsTrue(p1.Background);
            Assert.IsFalse(p1.ShowOffline, "30초 누적은 팝업을 생략한다");
            Assert.IsFalse(h.Popups.IsOpen(OfflinePopup.Name));
            Assert.IsFalse(lc.PausedAtMs.HasValue, "깨어나면 잠든 기록이 지워진다");

            // 1시간 — 팝업이 열리고 미리보기 = 닫힌 식(코인 3600 × 1/초 · 해머 60 × 1/분 · 기술트리 배율 1)
            now = SaveIo.NowMs();
            SaveIo.State.LastOfflineClaim = now - 3600 * 1000;
            ResumePlan p2 = lc.ResumeAt(now - 3600 * 1000, now);
            yield return null;
            Assert.IsTrue(p2.ShowOffline, "1시간 누적은 부팅 때와 같이 오프라인 팝업");
            Assert.IsTrue(h.Popups.IsOpen(OfflinePopup.Name), "오프라인 팝업이 열렸다");
            Assert.AreEqual(3600, p2.Pending.Elapsed, 1);
            OfflineReward expect = Offline.RewardFor(SaveIo.Defs, 3600, SaveIo.Mults);
            Assert.AreEqual(expect.Coins, p2.Pending.Coins, 1e-9, "코인 = 닫힌 식");
            Assert.AreEqual(expect.Hammers, p2.Pending.Hammers, 1e-9, "해머 = 닫힌 식");
            Assert.GreaterOrEqual(lc.ResumeCount, 2);

            // 같은 팝업이 열려 있는 동안 또 깨어나도 두 번 열지 않는다
            int before = h.Popups.OpenCount;
            lc.ResumeAt(now - 3600 * 1000, now);
            yield return null;
            Assert.AreEqual(before, h.Popups.OpenCount, "열린 오프라인 팝업 위에 또 열지 않는다");
            OfflinePopup.Close(h);
            yield return null;
            Assert.IsFalse(h.Popups.IsOpen(OfflinePopup.Name));
        }
    }
}
