using System;

namespace Forge.Core.Save
{
    /// <summary>
    /// 백그라운드·복귀 규칙(T88 · 원작 `main.js` 180~190 · 258~260 · 111). 원작은 탭이 숨어 있는 동안 전투 틱을 **일부러 돌리지 않는다** —
    /// «안 그러면 스로틀링된 채로 틱이 돌아 처치·보상이 시뮬레이션되고 오프라인 보상까지 같은 시간에 또 지급된다» — 그리고 탭이 다시 보일 때
    /// 논리 시계를 리셋해 **숨은 구간을 통째로 오프라인 보상 계산으로 넘긴다**. 대장간 업그레이드·부화·연구는 절대시각 `endsAt`(<see cref="AbsTimer"/>)이라
    /// 깨어나는 순간 «지금 ≥ endsAt» 로 닫힌 식대로 끝난다. 그래서 «따라잡기» 의 닫힌 식은 원작 `offlineRewardFor`(<see cref="Offline.RewardFor"/>) 하나다.
    /// 숫자는 원작 코드 상수(리터럴)라 여기 둔다(결정 80 의 «정본 코드 상수» 갈래).
    /// </summary>
    public static class LifecycleRules
    {
        /// <summary>원작 `main.js` 191 `Math.min(5000, now - logicLast)` — 보이는 탭에서 밀린 틱을 따라잡는 상한. 이보다 긴 공백은 «백그라운드 구간» 으로 본다.</summary>
        public const double BackgroundGapMs = 5000;
        /// <summary>원작 `main.js` 111 `offlinePending.elapsed >= 60` — 1분 미만 누적은 팝업을 생략한다(수동 [수집] 은 `ui.js` 6085 가 같은 60초로 켠다).</summary>
        public const double OfflinePopupMinSec = 60;
    }

    /// <summary>복귀 한 번의 판정 — 화면 쪽(<c>AppLifecycle</c>)은 이것을 읽고 팝업을 열거나 말 뿐이다.</summary>
    public sealed class ResumePlan
    {
        /// <summary>잠든 실시간(ms · 0 하한).</summary>
        public double SleptMs;
        /// <summary>원작 기준 «백그라운드 구간» 이었는가(<see cref="LifecycleRules.BackgroundGapMs"/> 이상).</summary>
        public bool Background;
        /// <summary>미수집 오프라인 누적분(<see cref="Offline.Pending"/> · 없으면 null). 기준은 잠든 시각이 아니라 `lastOfflineClaim` 이다 — 원작과 같다.</summary>
        public OfflineReward Pending;
        /// <summary>부팅 때와 같은 규칙으로 오프라인 팝업을 열 것인가(백그라운드 구간 + 누적 60초 이상).</summary>
        public bool ShowOffline;
    }

    public static class Lifecycle
    {
        /// <summary>프레임 사이 벽시계 공백이 백그라운드 구간인가(OS 가 앱을 재웠거나 탭이 숨어 프레임이 멎은 자리 · 플랫폼 콜백이 안 오는 WebGL 도 이것으로 잡는다).</summary>
        public static bool IsBackgroundGap(double lastFrameMs, double nowMs) { return nowMs - lastFrameMs >= LifecycleRules.BackgroundGapMs; }

        /// <summary>
        /// 깨어났다 — <paramref name="pausedAtMs"/> 에 잠들어 <paramref name="nowMs"/> 에 돌아왔다. 상태를 바꾸지 않는다(지급은 [수집]·팝업이 원작 `claimOfflineNow` 로).
        /// 전투는 따라잡지 않는다(원작이 숨은 구간의 틱을 버린다) · 절대시각 타이머는 호출자가 다음 1초 틱에서 `IsDone` 으로 닫는다.
        /// </summary>
        public static ResumePlan OnResume(SaveDefs defs, SaveState state, double pausedAtMs, double nowMs, OfflineMults mults)
        {
            var plan = new ResumePlan();
            plan.SleptMs = Math.Max(0, nowMs - pausedAtMs);
            plan.Background = plan.SleptMs >= LifecycleRules.BackgroundGapMs;
            plan.Pending = (defs != null && state != null) ? Offline.Pending(defs, state, nowMs, mults) : null;
            plan.ShowOffline = plan.Background && plan.Pending != null && plan.Pending.Elapsed >= LifecycleRules.OfflinePopupMinSec;
            return plan;
        }
    }
}
