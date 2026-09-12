using System;
using System.Collections.Generic;
using Forge.Core.Pets;

namespace Forge.Core.Gear
{
    /// <summary>
    /// 장비 계통(원작 `forge.js` 의 장착·판매·능력치 절)이 밖에서 받는 것 — `Ascension.starMult` · `TechTree` 배율 셋 · `Pets/Mounts.activeBonus` ·
    /// `Skills.ownedPassive` · `Combat.buffs` · `Quests.bump` · `Scene3D.refreshHeroEquip` · `Combat.recalcHero`. 값은 전부 호스트가 준다(T14 `IForgeMods` 와 같은 방식).
    /// </summary>
    public interface IGearHost
    {
        /// <summary>`Ascension.starMult(stars)` — 별 0 이면 1.</summary>
        Big StarMult(int stars);
        /// <summary>`TechTree.sellPriceMult()`.</summary>
        double SellPriceMult { get; }
        /// <summary>`TechTree.gearAtkMult()`(무기 숙련).</summary>
        double GearAtkMult { get; }
        /// <summary>`TechTree.gearHpMult()`(방어구 숙련).</summary>
        double GearHpMult { get; }
        /// <summary>`Pets.activeBonus()` — 출전 펫 고정 공격력·체력 + 서브스탯(목록에 덧붙인다).</summary>
        void PetBonus(out Big atk, out Big hp, List<Substat> subs);
        /// <summary>`Mounts.activeBonus()`(T11) — 장착 탈것.</summary>
        void MountBonus(out Big atk, out Big hp, List<Substat> subs);
        /// <summary>`Skills.ownedPassive()` — 보유 스킬 패시브(서브스탯 없음).</summary>
        void SkillPassive(out Big atk, out Big hp);
        /// <summary>`Combat.buffs[].buff.atkFlat` 의 합(살아 있는 버프만). 전투 엔진(T7 `Battle.RecalcHero`)이 스스로 더하는 경로에서는 0 을 준다.</summary>
        Big BuffAtkFlat { get; }
        /// <summary>`Quests.bump(key, n)`.</summary>
        void QuestBump(string key, double n);
        /// <summary>`Scene3D.refreshHeroEquip(withFlash)` — 페이퍼돌 갱신(Game `Paperdoll`).</summary>
        void RefreshHeroEquip(bool withFlash);
        /// <summary>`Combat.recalcHero()`.</summary>
        void RecalcHero();
    }

    /// <summary>기본 호스트 — 칸마다 대리자·값을 꽂는다(테스트·부팅 초기). 안 꽂은 칸은 원작의 «없음» 값(배율 1 · 보너스 0 · 별 배율 1).</summary>
    public sealed class GearHost : IGearHost
    {
        public Func<int, Big> StarMultFn;
        public double SellPriceMult { get; set; } = 1;
        public double GearAtkMult { get; set; } = 1;
        public double GearHpMult { get; set; } = 1;
        public Big PetAtk = Big.Zero, PetHp = Big.Zero, MountAtk = Big.Zero, MountHp = Big.Zero, SkillAtk = Big.Zero, SkillHp = Big.Zero;
        public List<Substat> PetSubs = new List<Substat>();
        public List<Substat> MountSubs = new List<Substat>();
        public Big BuffAtkFlat { get; set; } = Big.Zero;
        public Action<string, double> OnQuestBump;
        public Action<bool> OnRefreshHeroEquip;
        public Action OnRecalcHero;

        public Big StarMult(int stars) { return StarMultFn != null ? StarMultFn(stars) : Big.One; }
        public void PetBonus(out Big atk, out Big hp, List<Substat> subs) { atk = PetAtk; hp = PetHp; if (subs != null) subs.AddRange(PetSubs); }
        public void MountBonus(out Big atk, out Big hp, List<Substat> subs) { atk = MountAtk; hp = MountHp; if (subs != null) subs.AddRange(MountSubs); }
        public void SkillPassive(out Big atk, out Big hp) { atk = SkillAtk; hp = SkillHp; }
        public void QuestBump(string key, double n) { if (OnQuestBump != null) OnQuestBump(key, n); }
        public void RefreshHeroEquip(bool withFlash) { if (OnRefreshHeroEquip != null) OnRefreshHeroEquip(withFlash); }
        public void RecalcHero() { if (OnRecalcHero != null) OnRecalcHero(); }
    }
}
