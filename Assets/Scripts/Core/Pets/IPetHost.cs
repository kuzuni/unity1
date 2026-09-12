namespace Forge.Core.Pets
{
    /// <summary>
    /// 원작 `pets.js` 가 **바깥에서** 받는 것 — 시각(`U.now`) · 지갑(`S.gems`·`S.eggCurrency`) · 대장간 기준축(`Forge.levelMult`·
    /// `gearSumAtkAt`·`gearSumHpAt` · T14) · 승천(`Ascension.starMult`·`count('pet')` · T24) · 기술트리 배율(T24).
    /// 펫 규칙은 이 면 뒤의 값을 계산하지 않는다 — 그 계통이 Core 에 서면 그것을 꽂고, 그 전에는 테스트가 원작 공식으로 채운다.
    /// </summary>
    public interface IPetHost
    {
        /// <summary>`U.now()` — ms 절대시각.</summary>
        double Now();

        /// <summary>`S.gems`.</summary>
        double Gems { get; set; }
        /// <summary>`S.eggCurrency`(🥚).</summary>
        double EggCurrency { get; set; }

        /// <summary>`Forge.levelMult(level)` = LEVEL_STEP^(level−1) — 장비·펫·탈것 공용 커브.</summary>
        double LevelMult(int level);
        /// <summary>`Forge.gearSumAtkAt(rarity)` — 같은 등급 Lv1 장비 8부위 중 공격 4부위 합.</summary>
        double GearSumAtkAt(string rarity);
        /// <summary>`Forge.gearSumHpAt(rarity)` — 체력 4부위 합.</summary>
        double GearSumHpAt(string rarity);

        /// <summary>`Ascension.starMult(stars)` — 별 N개 → STAR_MULT^N (별 0 = 1).</summary>
        Big StarMult(int stars);
        /// <summary>`Ascension.count('pet')` — 새로 부화하는 펫에 찍히는 별 수.</summary>
        int PetAscendCount();

        /// <summary>`TechTree.petDmgMult()`.</summary>
        double PetDmgMult();
        /// <summary>`TechTree.petHpMult()`.</summary>
        double PetHpMult();
        /// <summary>`TechTree.hatchSpeedMult()` — 부화 시간에 곱한다(1/(1+%)).</summary>
        double HatchSpeedMult();
        /// <summary>`TechTree.extraEggChance()` — 소환 1회당 보너스 알 확률(0~1).</summary>
        double ExtraEggChance();
    }
}
