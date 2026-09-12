using System;
using Forge.Core.Data;
using Forge.Core.Forging;

namespace Forge.Core.Gear
{
    /// <summary>
    /// 원작 `forge.js` 장비 절의 순수식(T15) — `itemValue` · `itemPower` · `isMatchingGear` · `sellPrice` · `heroStats` 의 코드 상수.
    /// 표 수치(등급 배율 · 부위 주스탯 · 서브스탯 키)는 `gamedata.json`(<see cref="GameDefs"/>)에서 읽는다.
    /// </summary>
    public static class GearRules
    {
        /// <summary>맨몸 기본 공격력 15 · 체력 150(원작 `heroStats` 첫 줄).</summary>
        public const double BaseAtk = 15, BaseHp = 150;
        /// <summary>치명타 확률 = min(80, 5 + 서브스탯).</summary>
        public const double CritBase = 5, CritCap = 80;
        /// <summary>치명타 피해 = 100 + 서브스탯.</summary>
        public const double CritDmgBase = 100;
        /// <summary>초당 공격 = 1.1 × (1 + 공속%/100).</summary>
        public const double AttacksPerSecBase = 1.1;
        public const double DblAtkCap = 50, BlockCap = 80, SkillCdCap = 80;
        /// <summary>판매가 = floor(20 × 1.01^(레벨−1) × 등급 배율 × 기술트리 배율).</summary>
        public const double SellBase = 20, SellLevelStep = 1.01;
        /// <summary>서브스탯 위력 환산 = Π(1 + 값/200).</summary>
        public const double SubPowerDiv = 200;
        /// <summary>무기 없음 → `club` · 무기 종 없음 → `sword`(원작 `refreshHeroEquip`).</summary>
        public const string NoWeaponWtype = "club", DefaultWtype = "sword";

        /// <summary>`Forge.itemValue(item)` = Big(value) × 별 배율. null → 0.</summary>
        public static Big ItemValue(ForgeItem item, Func<int, Big> starMult)
        {
            if (item == null) return Big.Zero;
            return Big.Of(item.Value).Mul(starMult != null ? starMult(item.Stars) : Big.One);
        }

        /// <summary>`Forge.itemPower(item)` = itemValue × Π(1 + 서브값/200)(Number 로 누적 · 원작 순서 그대로).</summary>
        public static Big ItemPower(ForgeItem item, Func<int, Big> starMult)
        {
            if (item == null) return Big.Zero;
            double sub = 1;
            if (item.Subs != null) for (int i = 0; i < item.Subs.Count; i++) sub *= (1 + item.Subs[i].Value / SubPowerDiv);
            return ItemValue(item, starMult).Mul(Big.Of(sub));
        }

        /// <summary>`Forge.isMatchingGear(a, b)` — 부위·등급·이름이 같다.</summary>
        public static bool IsMatchingGear(ForgeItem a, ForgeItem b)
        {
            return a != null && b != null && a.Slot == b.Slot && a.Rarity == b.Rarity && a.Name == b.Name;
        }

        /// <summary>`Forge.sellPrice(item)`. 반쪽 항목(레벨 NaN · 등급 없음)은 원작처럼 NaN 을 낸다 — 지급 자리(<see cref="GearSystem.Sell"/>)가 거른다.</summary>
        public static double SellPrice(GameDefs defs, ForgeItem item, double sellPriceMult)
        {
            if (item == null) return double.NaN;
            double rm = item.Rarity != null ? defs.RarityMult.Get(item.Rarity, double.NaN) : double.NaN;
            return Math.Floor(SellBase * Math.Pow(SellLevelStep, item.Level - 1) * rm * sellPriceMult);
        }
    }
}
