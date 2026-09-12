namespace Forge.Core.Mounts
{
    /// <summary>
    /// 원작 `mounts.js` 가 **바깥에서** 받는 것 — 지갑(`S.winders`) · 대장간 기준축(`Forge.levelMult`·`gearSumAtkAt`·`gearSumHpAt` · T14) ·
    /// 승천(`Ascension.starMult`·`count('mount')` · T24) · 기술트리 배율(`mountDmgMult`·`mountHpMult`·`mountCostMult`·`extraMountChance` · T24).
    /// T16 <c>IPetHost</c> 와 같은 꼴 — 탈것 규칙은 이 면 뒤의 값을 계산하지 않는다.
    /// </summary>
    public interface IMountHost
    {
        /// <summary>`S.winders` — 태엽(클록와인더).</summary>
        double Winders { get; set; }

        /// <summary>`Forge.levelMult(level)` = LEVEL_STEP^(level−1).</summary>
        double LevelMult(int level);
        /// <summary>`Forge.gearSumAtkAt(rarity)` — 같은 등급 Lv1 장비 8부위 중 공격 4부위 합.</summary>
        double GearSumAtkAt(string rarity);
        /// <summary>`Forge.gearSumHpAt(rarity)` — 체력 4부위 합.</summary>
        double GearSumHpAt(string rarity);

        /// <summary>`Ascension.starMult(stars)`.</summary>
        Big StarMult(int stars);
        /// <summary>`Ascension.count('mount')` — 새로 소환되는 탈것에 찍히는 별 수.</summary>
        int MountAscendCount();

        /// <summary>`TechTree.mountDmgMult()`.</summary>
        double MountDmgMult();
        /// <summary>`TechTree.mountHpMult()`.</summary>
        double MountHpMult();
        /// <summary>`TechTree.mountCostMult()` — 태엽 비용 배율(하한 0.1).</summary>
        double MountCostMult();
        /// <summary>`TechTree.extraMountChance()` — 소환 1회당 추가 1마리 확률(0~1).</summary>
        double ExtraMountChance();
    }
}
