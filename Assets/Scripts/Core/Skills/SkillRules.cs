namespace Forge.Core.Skills
{
    /// <summary>
    /// 원작 `skills.js` 의 **코드 상수**(JSON 표에 없는 것) — 정본 값 그대로 · `skill_vectors.json` 의 `consts` 가 같은 값인지 EditMode 가 본다.
    /// (T16 `PetRules` 와 같은 자리 — 표 수치는 <see cref="Data.GameData"/> 에서, 코드 상수는 여기서만.)
    /// </summary>
    public sealed class SkillRules
    {
        /// <summary>`SUMMON_TICKET_COST` — 소환 1회 티켓(UI-SPEC «소환 x5 🎫160» 역산).</summary>
        public int SummonTicketCost = 32;
        /// <summary>`SUMMON_GEM_COST` — 젬 소환가.</summary>
        public int SummonGemCost = 200;
        /// <summary>`MAX_LEVEL` — 승천은 Lv.100 부터.</summary>
        public int MaxLevel = 100;
        /// <summary>`MAX_ACTIVE` — 장착 슬롯 수(원작 3 · 스킬 버튼 3개).</summary>
        public int MaxActive = 3;
        /// <summary>`levelMult` = 1 + 0.15 × (level − 1).</summary>
        public double LevelMultStep = 0.15;
        /// <summary>`summonLevel` = min(100, floor(summonCount / 5) + 1).</summary>
        public int SummonsPerLevel = 5;
        public int SummonLevelCap = 100;

        /// <summary>`shardsRequired(level)` — 10 미만 2 · 30 미만 3 · 60 미만 6 · 그 위 8.</summary>
        public int ShardsRequired(int level)
        {
            if (level < 10) return 2;
            if (level < 30) return 3;
            if (level < 60) return 6;
            return 8;
        }

        public static SkillRules Original() { return new SkillRules(); }
    }
}
