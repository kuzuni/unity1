namespace Forge.Core.Skills
{
    /// <summary>
    /// 원작 `skills.js` 가 **바깥에서** 받는 것 — 지갑(`S.gems`·`S.tickets`) · 기술트리 배율(`TechTree.skillSummonCostMult/
    /// skillPassiveDmgMult/skillPassiveHpMult` · T24) · 승천(`Ascension.starMult`·`count('skill')` · T24).
    /// 스킬 규칙은 이 면 뒤의 값을 계산하지 않는다 — Core `TechTree`·`Ascension` 을 꽂거나, 테스트가 정본 공식으로 채운다.
    /// UI 갱신(`UI.renderSkillBar`)·영웅 재계산(`Combat.recalcHero`)·저장·효과음·퀘스트는 호출자가 결과 객체를 보고 한다.
    /// </summary>
    public interface ISkillHost
    {
        /// <summary>`S.gems`.</summary>
        double Gems { get; set; }
        /// <summary>`S.tickets`(🎫 소환권).</summary>
        double Tickets { get; set; }

        /// <summary>`TechTree.skillSummonCostMult()` = max(0.1, 1 − %/100).</summary>
        double SkillSummonCostMult();
        /// <summary>`TechTree.skillPassiveDmgMult()` = 1 + %/100.</summary>
        double SkillPassiveDmgMult();
        /// <summary>`TechTree.skillPassiveHpMult()` = 1 + %/100.</summary>
        double SkillPassiveHpMult();

        /// <summary>`Ascension.starMult(stars)` — 별 N개 → STAR_MULT^N (별 0 = 1).</summary>
        Big StarMult(int stars);
        /// <summary>`Ascension.count('skill')` — 새로 얻는 스킬에 찍히는 별 수.</summary>
        int SkillAscendCount();
    }
}
