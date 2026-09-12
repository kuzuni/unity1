namespace Forge.Core.Dungeon
{
    /// <summary>
    /// 원작 `dungeons.js` 가 **바깥에서** 받는 값과 호출 — 시각(`Date.now`) · 진행 기록(`bestRank()` · `state.js`) · 기술트리 배율 4종(T24 `TechTree`) ·
    /// 반복 퀘스트(`Quests.bump` · T25) · 저장(`saveGame()` · T13). 화면 호출(`UI.*`)은 <see cref="Dungeons.Emitted"/> 이벤트다.
    /// 던전 규칙은 이 면 뒤의 값을 계산하지 않는다 — 결정 17(T16 `IPetHost`)과 같은 갈래.
    /// </summary>
    public interface IDungeonHost
    {
        /// <summary>`Date.now()` — ms 절대시각(리셋 날짜 키에 쓴다).</summary>
        double Now();
        /// <summary>`bestRank()` = 최고 기록의 절대 챕터·100 + 스테이지(`Progress.BestRank` · `SaveState.BestRank`).</summary>
        int BestRank();

        /// <summary>`TechTree.thiefHammerMult()`.</summary>
        double ThiefHammerMult();
        /// <summary>`TechTree.thiefCoinMult()`.</summary>
        double ThiefCoinMult();
        /// <summary>`TechTree.dungeonTicketMult()` — «던전 티켓 보너스».</summary>
        double DungeonTicketMult();
        /// <summary>`TechTree.dungeonPotionMult()` — «던전 물약 보너스».</summary>
        double DungeonPotionMult();

        /// <summary>`Quests.bump(key)` — `keySpend`(열쇠 사용) · `dungeonClear`(던전 완료).</summary>
        void QuestBump(string key);
        /// <summary>`saveGame()`.</summary>
        void Save();
    }
}
