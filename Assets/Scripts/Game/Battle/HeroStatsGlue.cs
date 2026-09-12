using System;
using System.Collections.Generic;
using UnityEngine;
using Forge.Core;
using Forge.Core.Battle;
using Forge.Core.Gear;
using Forge.Core.Pets;
using Forge.Core.Skills;
using Forge.Core.Tech;
using Forge.Game.Ui;

namespace Forge.Game.Battle
{
    /// <summary>
    /// T43 전투 스탯 접착 — 원작 `combat.js` 가 `Forge.heroStats()` 를 부르는 자리. 세이브 위에 선 T19 <see cref="ForgeHost"/> 의 <see cref="GearSystem"/>(장비 8부위 ·
    /// 서브스탯 · 별 배율)에 T20 <see cref="PetSkillHost"/> 의 펫 출전 보너스(`Pets.activeBonus`) · 보유 스킬 패시브(`Skills.ownedPassive`) · 기술트리 숙련 배율
    /// (`TechTree.gearAtkMult/gearHpMult`) · 탈것(`Mounts.activeBonus` · T11 이 <see cref="MountBonus"/> 에 꽂는다)을 **부를 때마다** 채워 <see cref="HeroStats"/> 를 낸다.
    /// 호스트가 아직 없으면(세이브 없음 · 부팅 초기) 맨몸(<see cref="BareHeroStats"/>) — 원작 `heroStats` 의 장비·펫·탈것·스킬 항이 전부 0 인 값이다.
    /// 버프 가산은 여기서 더하지 않는다(T7 `Battle.RecalcHero` 가 살아 있는 버프를 더한다 · `GearHost.BuffAtkFlat` 0).
    /// 재계산 신호: 펫 출전/합성/흡수(<see cref="PetSkillHost.RecalcHero"/>) · 장착/판매(<see cref="ForgeHost.RecalcHero"/>) · 두 호스트의 준비 완료 → <see cref="Battle.RecalcHero"/>.
    /// </summary>
    public static class HeroStatsGlue
    {
        /// <summary>`Mounts.activeBonus()` 자리 — T11(탈것 씬)이 장착 탈것의 고정 공격력·체력·서브스탯을 주는 대리자를 꽂는다. 없으면 0.</summary>
        public delegate void BonusFn(out Big atk, out Big hp, List<Substat> subs);
        public static BonusFn MountBonus;

        /// <summary>마지막 <see cref="Make"/> 가 맨몸이 아니라 세이브 위의 값이었는가(테스트·디버그).</summary>
        public static bool LastWasLive { get; private set; }

        /// <summary>장비·펫·스킬·기술트리를 다 알 수 있는 상태인가.</summary>
        public static bool Live { get { return ForgeHost.Ready && ForgeHost.Instance != null && ForgeHost.Instance.GearSys != null && ForgeHost.Instance.GearHostImpl != null; } }

        /// <summary>원작 `Forge.heroStats()` — `BattleContext.HeroStats` 에 준다.</summary>
        public static HeroStats Make()
        {
            if (!Live) { LastWasLive = false; return BareHeroStats.Make(); }
            ForgeHost fh = ForgeHost.Instance;
            GearHost h = fh.GearHostImpl;
            PetSkillHost ps = PetSkillHost.Ready ? PetSkillHost.Instance : null;

            // 기술트리 숙련 배율 — 원작은 부를 때마다 `TechTree.gearAtkMult()` 를 읽는다(연구가 끝나면 다음 재계산에 반영)
            TechTree tech = ps != null ? ps.Tech : (DungeonUiHost.Ready && DungeonUiHost.Instance != null ? DungeonUiHost.Instance.Tech : null);
            h.GearAtkMult = tech != null ? tech.GearAtkMult() : 1;
            h.GearHpMult = tech != null ? tech.GearHpMult() : 1;
            h.SellPriceMult = tech != null ? tech.SellPriceMult() : 1;

            // 출전 펫(`Pets.activeBonus`) — 고정 공격력·체력 + 서브스탯 원본
            Big pa = Big.Zero, ph = Big.Zero; var petSubs = new List<Substat>();
            if (ps != null && ps.Pets != null) ps.Pets.ActiveBonus(out pa, out ph, petSubs);
            h.PetAtk = pa; h.PetHp = ph; h.PetSubs = petSubs;

            // 장착 탈것(`Mounts.activeBonus`) — T11 이 꽂기 전에는 0
            Big ma = Big.Zero, mh = Big.Zero; var mountSubs = new List<Substat>();
            BonusFn mb = MountBonus;
            if (mb != null) mb(out ma, out mh, mountSubs);
            h.MountAtk = ma; h.MountHp = mh; h.MountSubs = mountSubs;

            // 보유 스킬 패시브(`Skills.ownedPassive`) — 장착과 무관 · 서브스탯 없음
            Big sa = Big.Zero, sh = Big.Zero;
            if (ps != null && ps.Skills != null) { PassiveBonus pb = ps.Skills.OwnedPassive(); sa = pb.Atk; sh = pb.Hp; }
            h.SkillAtk = sa; h.SkillHp = sh;

            h.BuffAtkFlat = Big.Zero;
            LastWasLive = true;
            return fh.GearSys.HeroStats();
        }

        // ---- 재계산 신호 → Battle.RecalcHero ----
        static BattleScene installed;
        static Action onForgeReady, onPetReady, onRecalc;

        /// <summary>전투 씬에 신호를 잇는다(부팅 · 한 번). 호스트가 뒤늦게 서면 그때 다시 계산한다(맨몸 → 세이브 값).</summary>
        public static void Install(BattleScene scene)
        {
            Uninstall();
            installed = scene;
            onRecalc = () => { BattleScene s = installed; if (s != null && s.Battle != null) s.Battle.RecalcHero(); };
            onForgeReady = () => { Hook(); onRecalc(); };
            onPetReady = () => { Hook(); onRecalc(); };
            ForgeHost.OnReady += onForgeReady;
            PetSkillHost.OnReady += onPetReady;
            Hook();
        }

        static ForgeHost hookedForge; static PetSkillHost hookedPet;
        static void Hook()
        {
            if (ForgeHost.Instance != null && hookedForge != ForgeHost.Instance)
            {
                if (hookedForge != null) hookedForge.RecalcHero -= onRecalc;
                hookedForge = ForgeHost.Instance; hookedForge.RecalcHero += onRecalc;
            }
            if (PetSkillHost.Instance != null && hookedPet != PetSkillHost.Instance)
            {
                if (hookedPet != null) hookedPet.RecalcHero -= onRecalc;
                hookedPet = PetSkillHost.Instance; hookedPet.RecalcHero += onRecalc;
            }
        }

        public static void Uninstall()
        {
            if (onForgeReady != null) ForgeHost.OnReady -= onForgeReady;
            if (onPetReady != null) PetSkillHost.OnReady -= onPetReady;
            if (hookedForge != null && onRecalc != null) hookedForge.RecalcHero -= onRecalc;
            if (hookedPet != null && onRecalc != null) hookedPet.RecalcHero -= onRecalc;
            hookedForge = null; hookedPet = null; installed = null; onForgeReady = null; onPetReady = null; onRecalc = null;
        }

        /// <summary>지금 값으로 한 번 다시 계산(테스트·디버그).</summary>
        public static void Recalc() { Action r = onRecalc; if (r != null) r(); }
    }
}
