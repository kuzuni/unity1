using Forge.Core.Battle;

namespace Forge.Core.Dungeon
{
    /// <summary>
    /// 원작 `web/js/dungeons.js` 의 `Dungeons` 객체에 박힌 **코드 상수**(T23) — 열쇠 2 · 웨이브 1~3 · 보상 100+2/단계 · 몬스터 HP 곡선 · 09:00 리셋.
    /// 결정 19·28ⓓ 와 같은 갈래: 정본에서도 JSON 표가 아니라 모듈 안의 리터럴이라 추출 대상이 아니다. 값을 고치려면 정본을 고치고 여기를 따라 맞춘다.
    /// </summary>
    public static class DungeonRules
    {
        /// <summary>`MAX_KEYS` — 던전마다 하루 열쇠 2개(매일 09:00 리셋 · 클리어·소탕 때 소모).</summary>
        public const int MaxKeys = 2;
        /// <summary>`MIN_WAVES`·`MAX_WAVES` — 던전 한 판의 웨이브 수(입장 때 한 번 뽑아 run.waves 에 굳힌다 · 마지막 웨이브가 보스).</summary>
        public const int MinWaves = 1, MaxWaves = 3;
        /// <summary>`DEFAULT_WAVES` — run.waves 가 없는 구버전 진행 상태용 폴백(T7 `BattleRules.DungeonDefaultWaves` 와 한 값).</summary>
        public const int DefaultWaves = BattleRules.DungeonDefaultWaves;
        /// <summary>`BASE_REWARD`·`PER_STAGE` — 단계 n 보상 수량 = 100 + 2·(n−1)(선형 · 주인 지시 2026-08-18).</summary>
        public const double BaseReward = 100, PerStage = 2;
        /// <summary>`monsterHp` = 55 · 5.6^(해금 챕터−1) · 1.35^(단계−1) — 앞 둘은 본대 곡선(T7)과 같은 값, 단계 배수만 던전 고유.</summary>
        public const double MonsterHpBase = BattleRules.MonsterHpBase, MonsterHpPerChapter = BattleRules.MonsterHpPerChapter, MonsterHpPerStage = 1.35;
    }
}
