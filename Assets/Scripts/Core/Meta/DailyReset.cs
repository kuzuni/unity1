using System;
using System.Globalization;

namespace Forge.Core.Meta
{
    /// <summary>
    /// 원작 `Dungeons.resetDateKey()` = `new Date(Date.now() - 9h).toDateString()` — 매일 09:00(기기 로컬) 리셋 키.
    /// 상점 특가·리그 티켓이 이 키로 «오늘» 을 가른다(T23 던전도 같은 키를 쓴다 — 옮기면 여기서 가져갈 것).
    /// 문자열 꼴은 JS `toDateString` 그대로 `"Sat Sep 12 2026"`(요일·월 영문 3자 · 일 2자리 0채움) — 세이브에 그대로 남으므로 바꾸지 않는다.
    /// </summary>
    public static class DailyReset
    {
        /// <summary>리셋 시각(원작 리터럴 `9 * 3600 * 1000`) — 로컬 09:00.</summary>
        public const int ResetHour = 9;

        /// <summary>`localNow` 는 기기 로컬 시각(`DateTime.Now`) — 브라우저 `Date` 가 로컬 달력으로 문자열을 만드는 것과 같다.</summary>
        public static string ResetDateKey(DateTime localNow)
        {
            return localNow.AddHours(-ResetHour).ToString("ddd MMM dd yyyy", CultureInfo.InvariantCulture);
        }
    }
}
