using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Forge.Core.Save;
using Forge.Game.Ui;

namespace Forge.Game
{
    /// <summary>
    /// 앱 잠듦·깨어남(T88 · 원작 `main.js` 의 `visibilitychange` 자리). 폰은 OS 가 앱을 재우면 프레임이 멎으므로 <c>runInBackground</c> 만으로는
    /// 안 된다 — 잠든 시각을 적어 두고 깨어날 때 <see cref="Lifecycle.OnResume"/> 로 «백그라운드 구간» 을 판정한다. 원작 규칙 그대로:
    /// ⓐ 숨은 구간의 전투 틱은 돌리지 않고 오프라인 수급(닫힌 식 · <see cref="Offline"/>)으로 넘긴다 ⓑ 대장간·부화·연구는 절대시각 `endsAt` 이라
    /// 호스트들의 1초 틱이 다음 프레임에 «지금 ≥ endsAt» 로 닫는다 ⓒ 누적 60초 이상이면 부팅 때와 같은 오프라인 팝업을 연다(`main.js` 111).
    /// 콜백이 안 오는 판(WebGL 탭 숨김 · 데스크톱 절전)은 프레임 사이 벽시계 공백(<see cref="LifecycleRules.BackgroundGapMs"/>)으로 같은 길을 탄다.
    /// <see cref="SaveIo"/>·<see cref="UiRoot"/> 처럼 `sceneLoaded` 훅으로 Bootstrap 아래 자립한다(씬 파일을 안 고친다).
    /// </summary>
    [DefaultExecutionOrder(-850)]
    public sealed class AppLifecycle : MonoBehaviour
    {
        public static AppLifecycle Instance { get; private set; }

        /// <summary>마지막 복귀 판정(테스트·디버그) — 아직 한 번도 안 깨어났으면 null.</summary>
        public ResumePlan LastResume { get; private set; }
        /// <summary>깨어난 횟수.</summary>
        public int ResumeCount { get; private set; }
        /// <summary>잠든 시각(ms · 안 잠들었으면 null).</summary>
        public double? PausedAtMs { get; private set; }
        /// <summary>깨어났다(판정을 넘긴다). 팝업은 이미 열렸거나 말았다.</summary>
        public event Action<ResumePlan> Resumed;

        private double lastFrameMs;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Instance != null) return;
            foreach (Bootstrap b in Resources.FindObjectsOfTypeAll<Bootstrap>())
            {
                if (!b.gameObject.scene.isLoaded) continue;
                Create(b.transform);
                return;
            }
        }

        public static AppLifecycle Create(Transform parent)
        {
            if (Instance != null) return Instance;
            var go = new GameObject("AppLifecycle");
            go.transform.SetParent(parent, false);
            return go.AddComponent<AppLifecycle>();
        }

        private void Awake()
        {
            Instance = this;
            lastFrameMs = SaveIo.NowMs();
            Bootstrap.ApplyRunInBackground();
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        // 원작: visibilitychange(hidden) — 세이브는 SaveIo 가 같은 콜백에서 한다. 여기서는 잠든 시각만 적는다.
        private void OnApplicationPause(bool paused) { if (paused) MarkPaused(); else ResumeNow(); }
        private void OnApplicationFocus(bool focus) { if (!focus) MarkPaused(); }

        private void Update()
        {
            double now = SaveIo.NowMs();
            // 콜백 없이 프레임이 멎었던 자리(WebGL 탭 숨김 · 절전) — 원작이 «탭이 다시 보일 때 logicLast 를 리셋» 하던 그 순간
            if (PausedAtMs.HasValue || Lifecycle.IsBackgroundGap(lastFrameMs, now)) ResumeAt(PausedAtMs ?? lastFrameMs, now);
            lastFrameMs = now;
        }

        /// <summary>잠들었다(중복 호출은 첫 시각을 지킨다).</summary>
        public void MarkPaused()
        {
            if (!PausedAtMs.HasValue) PausedAtMs = SaveIo.NowMs();
        }

        /// <summary>지금 깨어났다(잠든 기록이 없으면 마지막 프레임 시각을 잠든 시각으로 본다).</summary>
        public ResumePlan ResumeNow() { return ResumeAt(PausedAtMs ?? lastFrameMs, SaveIo.NowMs()); }

        /// <summary>테스트용 — 잠든 시각·지금 시각을 넣어 같은 길을 탄다.</summary>
        public ResumePlan ResumeAt(double pausedAtMs, double nowMs)
        {
            PausedAtMs = null;
            lastFrameMs = nowMs;
            ResumePlan plan = Lifecycle.OnResume(SaveIo.Defs, SaveIo.State, pausedAtMs, nowMs, SaveIo.Mults);
            LastResume = plan;
            ResumeCount++;
            if (plan.ShowOffline)
            {
                MetaHost h = MetaHost.Instance;
                if (h != null && MetaHost.Ready && h.Popups != null && !h.Popups.IsOpen(OfflinePopup.Name)) OfflinePopup.Show(h, plan.Pending);
            }
            Action<ResumePlan> r = Resumed;
            if (r != null) r(plan);
            return plan;
        }
    }
}
