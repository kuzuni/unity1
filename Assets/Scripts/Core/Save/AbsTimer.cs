using System;

namespace Forge.Core.Save
{
    /// <summary>
    /// 절대시각 타이머(원작 규약). 원작은 대장간 업그레이드(`forgeUpgradeEndsAt`) · 부화(`hatching[].endsAt`) · 연구(`techResearch.endsAt`)를
    /// 전부 **`Date.now()` 기준 절대 ms** 로 세이브에 적고 매 초 `U.now() >= endsAt` 로 완료를 본다 — 그래서 앱을 껐다 켜도(세션 가동 시간이
    /// 아니라 벽시계라) 타이머가 이어진다. 유니티도 `Time.time` 이 아니라 이 자로 잰다.
    /// </summary>
    public static class AbsTimer
    {
        static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>JS `Date.now()` — 유닉스 에포크 기준 ms(정수 · 내림).</summary>
        public static double NowMs(DateTime utc) { return Math.Floor((utc.ToUniversalTime() - Epoch).TotalMilliseconds); }

        /// <summary>지금부터 <paramref name="durationSec"/> 초 뒤의 절대시각(ms).</summary>
        public static double EndsAt(double nowMs, double durationSec) { return nowMs + durationSec * 1000; }

        /// <summary>남은 초(0 하한).</summary>
        public static double RemainingSec(double endsAtMs, double nowMs) { return Math.Max(0, (endsAtMs - nowMs) / 1000); }

        /// <summary>원작 `U.now() >= endsAt`.</summary>
        public static bool IsDone(double endsAtMs, double nowMs) { return nowMs >= endsAtMs; }

        /// <summary>null(미진행)이면 false — 원작 `if (S.forgeUpgradeEndsAt && U.now() >= S.forgeUpgradeEndsAt)`.</summary>
        public static bool IsDone(double? endsAtMs, double nowMs) { return endsAtMs.HasValue && JsonTree.Truthy(endsAtMs.Value) && nowMs >= endsAtMs.Value; }
    }
}
