namespace Forge.Core.Battle
{
    /// <summary>
    /// 원작 `Forge.heroStats()` 의 반환 꼴(T7 이 읽는 칸만). atk·hp 만 Big(승천 배율) · 나머지는 %·확률·공속(Number).
    /// **버프 가산은 여기에 넣지 않는다** — `Battle.RecalcHero` 가 `Atk` 에 살아 있는 버프 `atkFlat` 합을 더한다(원작 348~359줄: 서브스탯 % 배율 «뒤에» 고정 가산).
    /// T15(장비)·T16(펫)·T24(기술트리)가 <see cref="BattleContext.HeroStats"/> 로 이것을 준다. 불변 — `RecalcHero` 때마다 새 객체.
    /// </summary>
    public sealed class HeroStats
    {
        public Big Atk, Hp;
        public double CritCh, CritDmg, AttacksPerSec, DblAtk, Block, HpRegen, Lifesteal, MeleeDmg, RangedDmg, SkillDmg, SkillCd;

        public HeroStats Clone()
        {
            return new HeroStats
            {
                Atk = Atk, Hp = Hp, CritCh = CritCh, CritDmg = CritDmg, AttacksPerSec = AttacksPerSec, DblAtk = DblAtk, Block = Block,
                HpRegen = HpRegen, Lifesteal = Lifesteal, MeleeDmg = MeleeDmg, RangedDmg = RangedDmg, SkillDmg = SkillDmg, SkillCd = SkillCd,
            };
        }
    }
}
