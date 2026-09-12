using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core;
using Forge.Core.Battle;
using Forge.Core.Pets;
using Forge.Game;
using Forge.Game.Battle;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T43 — 세이브 상태 → `GearSystem.HeroStats`(장비 + 펫 출전 + 스킬 패시브 + 기술트리) → `BattleContext.HeroStats`. 전투의 영웅 스탯이 대장간·펫 호스트가
    /// 내는 값과 같고, 펫을 출전시키면 `Battle.Hero.Atk` 이 오르는가. 빨간 로그는 러너가 실패시킨다.
    /// </summary>
    public class HeroStatsGlueTests
    {
        static IEnumerator Boot()
        {
            BattleScene.AutoBoot = false;
            if (BattleScene.Instance != null) UnityEngine.Object.Destroy(BattleScene.Instance.gameObject);
            HeroStatsGlue.Uninstall();
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && PetSkillHost.Ready) && t < 25f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 25초 안에 준비되지 않았다");
            Assert.IsTrue(PetSkillHost.Ready, "PetSkillHost 가 25초 안에 준비되지 않았다");
        }

        static BattleScene MakeScene()
        {
            var b = UnityEngine.Object.FindAnyObjectByType<Bootstrap>();
            var s = BattleScene.Create(b != null ? b.transform : null, false);
            s.Attach(BattleScene.MakeBattle(SaveIo.Data, SaveIo.Defs, HeroStatsGlue.Make, 7u), SaveIo.Data, SaveIo.Defs);
            HeroStatsGlue.Install(s);
            return s;
        }

        static void AssertBig(string what, Big e, Big g) { Assert.AreEqual(e.E, g.E, what + " e"); Assert.AreEqual(e.M, g.M, 1e-9, what + " m"); }

        [UnityTest]
        public IEnumerator 호스트가_없으면_맨몸_있으면_GearSystem_HeroStats_와_같다()
        {
            BattleScene.AutoBoot = false;
            if (BattleScene.Instance != null) UnityEngine.Object.Destroy(BattleScene.Instance.gameObject);
            yield return null;
            // 씬을 새로 열기 전 — 호스트가 없다 → 맨몸(원작 heroStats 의 장비·펫·탈것·스킬 항 0)
            HeroStatsGlue.Uninstall();
            if (!HeroStatsGlue.Live)
            {
                HeroStats bare = HeroStatsGlue.Make();
                Assert.IsFalse(HeroStatsGlue.LastWasLive);
                AssertBig("맨몸 atk", Big.Of(BareHeroStats.Atk), bare.Atk);
                AssertBig("맨몸 hp", Big.Of(BareHeroStats.Hp), bare.Hp);
            }
            yield return Boot();
            BattleScene s = MakeScene();
            yield return null;
            Assert.IsTrue(HeroStatsGlue.Live, "호스트가 섰다");
            HeroStats live = HeroStatsGlue.Make();
            Assert.IsTrue(HeroStatsGlue.LastWasLive);
            HeroStats expect = ForgeHost.Instance.GearSys.HeroStats();
            AssertBig("전투 atk = GearSystem.HeroStats", expect.Atk, s.Battle.Hero.Stats.Atk);
            AssertBig("전투 hp", expect.Hp, s.Battle.Hero.Stats.Hp);
            Assert.AreEqual(expect.CritCh, live.CritCh, 1e-9, "치명");
            Assert.AreEqual(expect.AttacksPerSec, live.AttacksPerSec, 1e-9, "공속");
            Assert.AreEqual(expect.SkillCd, live.SkillCd, 1e-9, "쿨감");
            UnityEngine.Object.Destroy(s.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 펫을_출전시키면_전투_영웅_공격력이_오른다()
        {
            yield return Boot();
            BattleScene s = MakeScene();
            yield return null;
            PetSkillHost ps = PetSkillHost.Instance;
            PetSkillHost.SuppressSave = true;
            try
            {
                Big before = s.Battle.Hero.Stats.Atk;
                PetState st = ps.Pets.State;
                // 신화 펫 한 마리를 보관함에 넣고 출전 — 원작 `Pets.toggleActive` → `Combat.recalcHero`
                st.Pets.Add(new Pet { Name = "Genie", Rarity = "mythic", Level = 1, Stars = 0, Subs = new List<Substat> { new Substat("dmgPct", "피해", 10) } });
                int idx = st.Pets.Count - 1;
                Assert.IsTrue(ps.Pets.CanActivate(idx) || st.ActivePets.Count >= PetRules.Original().MaxActive, "출전 가능(또는 이미 3마리)");
                if (st.ActivePets.Count >= PetRules.Original().MaxActive) ps.Pets.ToggleActive(st.ActivePets[0]);
                Assert.IsTrue(ps.Pets.ToggleActive(idx), "출전");
                ps.RequestRecalc();
                yield return null;
                Big after = s.Battle.Hero.Stats.Atk;
                Assert.IsTrue(after.Gt(before), "출전 뒤 전투 atk 가 오른다: " + before.M + "e" + before.E + " → " + after.M + "e" + after.E);
                HeroStats expect = ForgeHost.Instance.GearSys.HeroStats();
                AssertBig("출전 뒤 전투 atk = GearSystem.HeroStats", expect.Atk, after);
                // 해제하면 되돌아온다
                Assert.IsTrue(ps.Pets.ToggleActive(idx), "해제");
                st.Pets.RemoveAt(idx);
                ps.RequestRecalc();
                yield return null;
                Assert.IsTrue(s.Battle.Hero.Stats.Atk.Lt(after), "해제 뒤 내려간다");
            }
            finally
            {
                PetSkillHost.SuppressSave = false;
                UnityEngine.Object.Destroy(s.gameObject);
            }
            yield return null;
        }
    }
}
