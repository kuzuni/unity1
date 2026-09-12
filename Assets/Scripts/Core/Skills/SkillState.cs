using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Skills
{
    /// <summary>원작 `S.skills[id] = { level, dupes, stars }` 한 칸.</summary>
    public sealed class SkillEntry
    {
        public int Level = 1;
        /// <summary>조각(중복 소환 적립) — 레벨업은 수동 `Upgrade` 로만 쓴다.</summary>
        public int Dupes;
        /// <summary>얻을 때의 스킬 라인 승천 수(`Ascension.count('skill')`).</summary>
        public int Stars;
    }

    /// <summary>
    /// 원작 `S` 의 스킬 칸 셋 — `skills`(삽입 순서가 곧 `upgradeAll`·`quickEquip`·`ownedPassive` 의 순회 순서라 <see cref="OrderedMap{T}"/>) ·
    /// `equippedSkills` · `summonCount`(스킬 소환 누적 → 소환 레벨). 지갑(젬·티켓)은 <see cref="ISkillHost"/> 쪽이다.
    /// </summary>
    public sealed class SkillState
    {
        public OrderedMap<SkillEntry> Skills = new OrderedMap<SkillEntry>();
        public List<string> Equipped = new List<string>();
        public int SummonCount;

        public SkillEntry Get(string id) { SkillEntry e; return id != null && Skills.TryGet(id, out e) ? e : null; }
    }
}
