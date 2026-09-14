using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Battle;
using Forge.Core.Data;
using Forge.Core.Dungeon;
using Forge.Core.Forging;
using Forge.Core.Pets;
using Forge.Core.Save;
using Forge.Core.Skills;
using Forge.Core.Tech;
using CoreRng = Forge.Core.Data.Rng;
using Forge.Core;
using Forge.Game;
using Forge.Game.Battle;
using Forge.Game.Gallery;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T27 — 원작 화면 30장을 **원작과 같은 이름·같은 순서·같은 상태**로 열어 콘솔 빨강 0 을 재고 `ui-screens/screen_&lt;이름&gt;.png` 를 남긴다.
    /// 정본: `web/tools/shot-screens.js` 의 `SCREENS` 표(이름 ↔ `ref/screens/shot-*.png` 짝)와 `SEED`(캡처용 진행 상태 주입).
    /// 짝 표는 `ui-screens/screens.json` 으로 같이 남긴다 — T28 의 비율 대조가 «어느 클론 PNG 가 어느 원작 shot 인가» 를 그것에서 읽는다.
    ///
    /// 그림은 «있으면 좋은 것»이다(`-nographics` CI 에선 못 찍는다) — 이 테스트가 단언하는 것은 셋:
    ///   ① 30 화면이 다 **열렸는가**(화면마다 열림 술어) · ② 열린 화면의 글자가 T18 종류 하한 이상인가 · ③ 도는 내내 콘솔 빨강 0 인가(<see cref="PlayLog"/>).
    /// 화면 하나에서 터져도 나머지를 계속 돈다 — 30장 중 «첫 장» 만 보고 끝나면 나머지 29장이 성한지 알 수 없기 때문(원작 shot-screens.js 도 같은 꼴로 모아 뱉는다).
    /// </summary>
    public class UiShotsTests
    {
        /// <summary>원작 SCREENS 한 줄 — 이름 · 원작 shot 번호(없으면 null) · 여는 법 · «열렸다» 술어.</summary>
        private sealed class Shot
        {
            public string Name;
            public string Ref;
            public Action Open;
            public Func<bool> Opened;
            public bool Optional;
            /// <summary>노치 모의(T45 `UiRoot.NotchSafeArea`)로 찍는 줄인가.</summary>
            public bool Notch;
        }

        private const string OutPrefix = "screen_";
        /// <summary>촬영 크기 — 정확히 9:16 이라 앱 상자가 RenderTexture 를 꽉 채운다(원작 캡처 499×892 와 같은 비율 · T45 `SafeAreaTests` 540×1170 과 같은 폭).</summary>
        public const int ShotW = 540, ShotH = 960;
        /// <summary>어디까지 갔는지 남기는 자취 — CI 유니티 잡 로그는 꼬리 5000줄로 잘리고 아티팩트는 프록시가 막는다(T46 과 같은 사정). `screens` 브랜치에서 읽는다.</summary>
        private const string TraceFile = "uishots.txt";

        private static MetaHost M { get { return MetaHost.Instance; } }
        private static ForgeHost F { get { return ForgeHost.Instance; } }
        private static PetSkillHost P { get { return PetSkillHost.Instance; } }
        private static DungeonUiHost D { get { return DungeonUiHost.Instance; } }
        private static SkillPetSheet Sheet { get { return SkillPetSheet.Instance; } }
        private static PopupLayer Popups { get { return PopupLayer.Instance; } }

        [SetUp]
        public void NoDiskSave() { PetSkillHost.SuppressSave = true; }

        [TearDown]
        public void DropSave()
        {
            PetSkillHost.SuppressSave = false;
            UiRoot.OverrideSafeArea(null);
            try { if (File.Exists(SaveIo.SavePath)) File.Delete(SaveIo.SavePath); }
            catch (Exception) { /* 저장소 접근 실패는 무시 */ }
        }

        private static IEnumerator Boot()
        {
            // 자취는 **부팅보다 먼저** 연다 — 부팅이 못 서면(호스트 30초 대기 초과) 그 자리에서 단언이 터져
            // 그 뒤 줄이 하나도 안 남기 때문이다(CI 런 68 이 그 꼴이었다: 파일 자체가 없어 «어디서 멈췄나» 를 못 봤다).
            Trace("# T27 UiShotsTests 자취 — 어디까지 갔는지 한 줄씩(런이 죽어도 남는다)", true);
            Trace("boot 시작 · 유니티=" + Application.unityVersion + " · 배치=" + Application.isBatchMode
                  + " · 그래픽=" + GallerySheet.GraphicsAvailable + " · 화면=" + Screen.width + "x" + Screen.height);
            try { if (File.Exists(SaveIo.SavePath)) File.Delete(SaveIo.SavePath); }
            catch (Exception) { /* 없으면 그만 */ }
            // T128 — 세 호스트의 난수를 **같은 시드**로 묶는다(그 전엔 펫만 고정이라 장비·리그·채팅·던전이 런마다 흔들렸다).
            PetSkillHost.Seed = 20260912;
            MetaHost.Seed = 20260912;
            DungeonUiHost.Seed = 20260912;
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = 0f;
            while (t < 30f && !(MetaHost.Ready && ForgeHost.Ready && PetSkillHost.Ready && DungeonUiHost.Ready
                                && SkillPetSheet.Instance != null && PopupLayer.Instance != null))
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            string hosts = "meta=" + MetaHost.Ready + " forge=" + ForgeHost.Ready + " petskill=" + PetSkillHost.Ready
                           + " dungeon=" + DungeonUiHost.Ready + " sheet=" + (SkillPetSheet.Instance != null)
                           + " popups=" + (PopupLayer.Instance != null) + " · " + t.ToString("0.0") + "초";
            Trace("boot 호스트 · " + hosts);
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 30초 안에 서지 않았다 — " + hosts);
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 30초 안에 서지 않았다 — " + hosts);
            Assert.IsTrue(PetSkillHost.Ready, "PetSkillHost 가 30초 안에 서지 않았다 — " + hosts);
            Assert.IsTrue(DungeonUiHost.Ready, "DungeonUiHost 가 30초 안에 서지 않았다 — " + hosts);
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다 — " + hosts);
            yield return null;
        }

        /// <summary>T128 — 이번 촬영이 세운 장비 8부위의 «시대/등급/레벨» 서명. 런마다 같아야 대조가 성립한다.</summary>
        private static string GearSignature;

        /// <summary>
        /// T128 — 위 서명의 기대값. 촬영 난수를 고정한 뒤 **런 260·271 두 런이 글자 하나까지 같았다**(그 전에는 매 런 달랐다).
        /// 이 단언이 빨개지는 경우는 둘뿐이다: ⓐ 촬영 난수가 다시 흔들린다(고쳐야 한다) ⓑ 정본 표(balance·gamedata)가 바뀌어
        /// 굴림 결과가 바뀐다 — 그때는 **원작 샷과의 대조 기준이 함께 바뀐 것**이라 새 서명을 여기 적고 그 회차 기록에 이유를 남긴다.
        /// </summary>
        private const string GearSignatureExpected =
            "weapon=underworld/mythic/28 helmet=multiverse/legendary/26 armor=interstellar/common/25 gloves=multiverse/common/26 "
            + "necklace=underworld/rare/28 ring=multiverse/common/26 shoes=multiverse/legendary/26 belt=quantum/rare/27";

        /// <summary>원작 `shot-screens.js` 의 `SEED` — 빈 화면·잠금으로 레이아웃이 안 보이는 것을 막는 캡처용 진행 상태. 수치는 정본 그대로다(밸런스를 만들지 않는다).</summary>
        private static void Seed()
        {
            SaveState s = SaveIo.State;
            s.Chapter = 4; s.Stage = 1; s.BestChapter = 20; s.BestStage = 9;
            s.Hammers = 3.02e5; s.Coins = 2.71e7; s.Gems = 8150; s.Tickets = 160;
            s.Winders = 320; s.Potions = 45; s.EggCurrency = 900; s.Kills = 48210;
            s.ForgeLevel = 29; s.Nickname = "용사";
            s.AutoForgeOn = true;

            // 대장간 — 자동 제련 설정(정본 SEED: 마지막 두 시대 + 치명타 확률·피해 · 계속하기 켬)
            F.Pull();
            AutoForgeConfig cfg = F.Engine.AutoForgeConfig();
            cfg.HammersPerBatch = 22;
            cfg.KeepAges = new List<string> { "underworld", "divine" };
            cfg.FilterSubs = new List<string> { "critCh", "critDmg" };
            cfg.StopOnTarget = false;
            cfg.FilterOn = false;
            F.Forge.ForgeLevel = 29;

            // 장비 8부위 풀장착 — 서브옵션은 정본과 같이 2줄로 고정(랜덤 1~4줄이면 카드 높이가 매 런 달라져 대조가 안 된다)
            CoreRng rng = CoreRng.Mulberry(20260912);
            // T128 — **촬영용 굴림은 시드 고정 엔진으로 한다.** `ForgeHost` 는 게임 엔진의 난수를 벽시계로 씨 뿌리므로
            // (`ForgeHost.cs` 172 `Rng.Mulberry((uint)(Meta.NowMs …))`) 같은 `Seed()` 라도 부위마다 시대·등급이 런마다 달라졌다
            // (검수 Q 실측: 런 238↔254 `player-info` 장비 셀 크게 다른 픽셀 5.5% ↔ 상태가 같은 `settings` 는 1.0%).
            // 그러면 T28 채점의 «내려간 화면» 이 회귀인지 상태 차이인지 못 가린다. 게임 난수는 그대로 두고
            // **촬영에서만** 같은 표·같은 상태에 고정 시드를 물린 엔진을 따로 세워 굴린다(밸런스·콘텐츠 0 · 정본 SEED 와 같은 절차).
            ForgeEngine roll = new ForgeEngine(F.Data, F.Forge, F.Wallet, CoreRng.Mulberry(20260912), () => SaveIo.NowMs(), F.Mods);
            StringBuilder gearTrace = new StringBuilder();
            foreach (string slot in F.Defs.Slots)
            {
                ForgeItem it = null;
                for (int i = 0; i < 60 && it == null; i++)
                {
                    ForgeItem r = roll.RollItem();
                    if (r != null && r.Slot == slot) it = r;
                }
                if (it == null) continue;
                it.Level = 20 + it.AgeIdx;
                it.Subs = SubstatRoll.Roll(F.Defs, rng, 2);
                // 정본 SEED 127행 `S.equipment.weapon.rarity = 'mythic'` — 무기 칸만 신화로 박는다(원작 샷이 그 상태다).
                if (slot == "weapon") it.Rarity = "mythic";
                F.Gear.Set(slot, it);
                gearTrace.Append(slot).Append('=').Append(it.Age).Append('/').Append(it.Rarity)
                         .Append('/').Append(it.Level.ToString("0")).Append(' ');
            }
            // 이 줄이 런마다 같아야 «상태가 같은 두 런» 이다 — 다르면 촬영 상태가 또 흔들린 것이다(T128).
            GearSignature = gearTrace.ToString().TrimEnd();
            Trace("seed 장비 · " + GearSignature);
            F.Forge.UpgradeEndsAt = SaveIo.NowMs() + 96 * 60e3;   // 확률 정보 팝업 하단 진행바
            F.Push();

            // 스킬 — 정본은 15/18 보유(3줄 그리드) · 앞 3개 장착
            var sk = P.Skills.State;
            sk.Skills = new OrderedMap<SkillEntry>();
            sk.Equipped = new List<string>();
            List<SkillDef> defs = P.Data.Defs.SkillDefs;
            int take = defs.Count < 15 ? defs.Count : 15;
            for (int i = 0; i < take; i++)
                sk.Skills.Add(defs[i].Id, new SkillEntry { Level = 20 + i * 3, Dupes = (i * 3) % 8, Stars = 0 });
            for (int i = 0; i < take && sk.Equipped.Count < 3; i++) sk.Equipped.Add(defs[i].Id);
            sk.SummonCount = 260;

            // T77 — 정본 SEED 144행 `Combat.recalcHero()` 자리. 상태에 장비·스킬을 **직접** 넣으면 아무도 재계산을 안 부르고,
            // HUD 전투력은 부팅 때(장비 0) 계산해 둔 스탯을 계속 읽는다 — 그래서 촬영 상단바가 «⚔ 45»(맨몸 수)로 찍혔다.
            HeroStatsGlue.Recalc();

            // 펫 — 알·부화·보유를 채운다(정본: 알 30 소환 → 8마리 부화 · 부화 1칸 진행 · 출전 3)
            PetState ps = P.Pets.State;
            ps.Eggs.Clear(); ps.Pets.Clear(); ps.Hatching.Clear(); ps.ActivePets.Clear();
            SaveIo.State.EggCurrency = 5000;
            P.Pets.Summon(30);
            var petKeys = P.Data.Defs.PetKr.Keys;
            for (int i = 0; i < 8 && ps.Eggs.Count > 0; i++)
            {
                Egg e = ps.Eggs[0];
                ps.Eggs.RemoveAt(0);
                ps.Pets.Add(new Pet { Name = petKeys[i % petKeys.Count], Rarity = e.Rarity, Level = 10 + i * 3, Dupes = 4 });
            }
            if (ps.Hatching.Count < 2 && ps.Eggs.Count > 0)
            {
                Egg e = ps.Eggs[0];
                ps.Eggs.RemoveAt(0);
                ps.Hatching.Add(new HatchSlot(e.Rarity, SaveIo.NowMs() + 3600e3));
            }
            for (int i = 0; i < 3 && i < ps.Pets.Count; i++) ps.ActivePets.Add(i);
            ps.PetSummonCount = 150;
            SaveIo.State.EggCurrency = 900;

            // 기술 연구 진행 중(초록 배지) — 정본과 같은 노드·남은 시간
            P.Tech.State.Research = new TechResearch("forgeTimer@1", SaveIo.NowMs() + 42 * 60e3);

            // 던전 — 상세 팝업의 난이도가 원작과 같은 값(198)으로 찍히도록
            D.Dungeons.Ensure();
            JsonObject best = J.Obj(D.Dungeons.Slot["best"]);
            JsonObject keys = J.Obj(D.Dungeons.Slot["keys"]);
            foreach (DungeonDef d in DungeonDefs.All)
            {
                if (best != null) best[d.Id] = 198.0;
                if (keys != null) keys[d.Id] = 2.0;
            }

            P.Sync();
            HeroStatsGlue.Recalc();   // T77 — 펫 출전·기술 연구까지 넣은 뒤 한 번 더(정본은 펫 소환이 제 안에서 부른다)
            M.Touch(false);           // HUD 는 재계산 **뒤에** 다시 적는다
        }

        /// <summary>
        /// T77 — 촬영 전 단언: 상단바 전투력이 **장비를 태운 값**인가. 정본 SEED 는 장비 8부위(Lv.26~27 = 시대 6~7)를 채우므로
        /// 전투력은 맨몸(≈36)이 아니라 수백만 단위여야 한다. 수치는 박지 않는다(§1) — 맨몸은 <see cref="BareHeroStats"/> ·
        /// 식은 Core `Battle.CombatPower` 를 그대로 쓴다. 이 단언이 빨강이면 원인이 시드가 아니라 접착(T43·T55 갈래)이다.
        /// </summary>
        private static void AssertCombatPowerTookGear()
        {
            BattleScene bs = BattleScene.Instance;
            Assert.IsNotNull(bs, "전투 씬이 없어 전투력을 잴 수 없다");
            Assert.IsNotNull(bs.Battle, "전투가 아직 안 섰다");
            Assert.IsTrue(HeroStatsGlue.Live, "장비·펫·스킬 호스트가 안 서서 맨몸 스탯을 쓰고 있다");

            Big cp = M.MyCp;
            Big bare = Cp(BareHeroStats.Make());
            Big geared = Cp(HeroStatsGlue.Make());
            Assert.IsTrue(cp.Cmp(bare) > 0, "상단바 전투력 " + NumFmt.Fmt(cp) + " 이 맨몸 " + NumFmt.Fmt(bare) + " 보다 크지 않다 — 장비가 안 탔다");
            Assert.AreEqual(NumFmt.Fmt(geared), NumFmt.Fmt(cp), "상단바 전투력이 장비를 태운 값과 다르다");
        }

        /// <summary>원작 `Combat.combatPower()` 식(Core `Battle.CombatPower` 와 같은 줄)을 스탯 하나에 적용한다.</summary>
        private static Big Cp(HeroStats st)
        {
            return st.Atk.Mul(st.AttacksPerSec * (1 + st.CritCh / 100 * st.CritDmg / 100)).Add(st.Hp.Div(8));
        }

        /// <summary>원작 `SCREENS` 표 그대로(순서·이름·shot 번호). 탈것 화면은 T20 이 아직 안 세웠으므로 «있으면 찍는다».</summary>
        private static List<Shot> Screens()
        {
            var tb = UiRoot.Instance.TabBar;
            var list = new List<Shot>();

            list.Add(new Shot
            {
                Name = "main", Ref = "042120",
                Open = delegate { CloseAll(); },
                Opened = delegate { return Popups.OpenCount == 0 && tb.ActiveTab == null; }
            });
            list.Add(new Shot
            {
                Name = "offline", Ref = "042110",
                Open = delegate { OfflinePopup.Show(M, Offline.RewardFor(SaveIo.Defs, 4 * 3600, OfflineMults.One)); },
                Opened = delegate { return Popups.IsOpen(OfflinePopup.Name); }
            });
            list.Add(new Shot
            {
                Name = "league", Ref = "042149",
                Open = delegate { M.OpenLeague(); },
                Opened = delegate { return Popups.IsOpen(LeagueSheet.Name); }
            });
            list.Add(new Shot
            {
                Name = "league-rewards", Ref = "042208",
                Open = delegate { M.OpenLeague(); LeagueSheet.OpenRewards(M); },
                Opened = delegate { return Popups.IsOpen(LeagueSheet.RewardsName); }
            });
            list.Add(new Shot
            {
                Name = "league-challenge", Ref = "042228",
                Open = delegate { M.OpenLeague(); LeagueSheet.OpenChallenge(M); },
                Opened = delegate { return Popups.IsOpen(LeagueSheet.ChallengeName); }
            });
            list.Add(new Shot
            {
                Name = "dungeons", Ref = "042251",
                Open = delegate { tb.OnTab("dungeon"); },
                Opened = delegate { return DungeonSheet.Instance != null && DungeonSheet.Instance.IsOpen; }
            });
            list.Add(new Shot
            {
                Name = "dungeon-detail", Ref = "042304",
                Open = delegate { tb.OnTab("dungeon"); DungeonDetailPopup.Open("hammer"); },
                Opened = delegate { return DungeonDetailPopup.IsOpen; }
            });
            list.Add(new Shot
            {
                Name = "skills", Ref = "042340",
                Open = delegate { OpenSummon(SkillPetSheet.SubSkills); },
                Opened = delegate { return Sheet.IsSheetOpen && Sheet.ActiveSub == SkillPetSheet.SubSkills; }
            });
            // 원작 042356·042445 는 같은 상태다 — 원작도 같은 오프너로 두 장을 찍는다.
            list.Add(new Shot
            {
                Name = "pets", Ref = "042356",
                Open = delegate { OpenSummon(SkillPetSheet.SubPets); },
                Opened = delegate { return Sheet.IsSheetOpen && Sheet.ActiveSub == SkillPetSheet.SubPets; }
            });
            list.Add(new Shot
            {
                Name = "pets-2", Ref = "042445",
                Open = delegate { OpenSummon(SkillPetSheet.SubPets); },
                Opened = delegate { return Sheet.IsSheetOpen && Sheet.ActiveSub == SkillPetSheet.SubPets; }
            });
            list.Add(new Shot
            {
                Name = "tech-overview", Ref = "042407",
                Open = delegate { OpenSummon(SkillPetSheet.SubTech); if (TechPanel.Instance != null) TechPanel.Instance.ShowOverview(); },
                Opened = delegate { return TechPanel.Instance != null && TechPanel.Instance.Current == TechPanel.View.Overview; }
            });
            // 원작 042426 은 **장착 안 한** 스킬을 연 상태다(앞 3개는 시드가 장착했다) — 정본과 같이 인덱스 3.
            list.Add(new Shot
            {
                Name = "skill-detail", Ref = "042426",
                Open = delegate
                {
                    OpenSummon(SkillPetSheet.SubSkills);
                    var keys = P.Skills.State.Skills.Keys;
                    if (keys.Count > 3) Sheet.Skills.OpenSkillDetail(keys[3]);
                },
                Opened = delegate { return Sheet.Modal.IsOpen(SkillPanel.DetailModal); }
            });
            // 원작 042449/042503 의 대상은 그리드 가운데(인덱스 1) — 정본과 같은 자리를 연다.
            list.Add(new Shot
            {
                Name = "pet-detail", Ref = "042449",
                Open = delegate { OpenSummon(SkillPetSheet.SubPets); Sheet.Pets.OpenPetDetail(1); },
                Opened = delegate { return Sheet.Modal.IsOpen(PetPanel.DetailModal); }
            });
            list.Add(new Shot
            {
                Name = "pet-upgrade", Ref = "042503",
                Open = delegate { OpenSummon(SkillPetSheet.SubPets); PetUpgradePopup.Open(Sheet, 1); },
                Opened = delegate { return Sheet.Modal.IsOpen(PetUpgradePopup.ModalName); }
            });
            // 원작 042521 은 **펫** 확률표다(스킬로 열면 배경 그리드와 문구가 달라진다).
            list.Add(new Shot
            {
                Name = "summon-rates", Ref = "042521",
                Open = delegate { OpenSummon(SkillPetSheet.SubPets); SkillRatesPopup.Open(Sheet, "pet"); },
                Opened = delegate { return Sheet.Modal.IsOpen(SkillRatesPopup.ModalName); }
            });
            // 원작 042546 의 제목은 '스킬, 펫 & 기술' — skillpet 분기(12노드) · 앞 7노드가 1/5 로 올라간 상태.
            list.Add(new Shot
            {
                Name = "tech-branch", Ref = "042546",
                Open = delegate
                {
                    TechTree tt = P.Tech;
                    List<string> ids = tt.NodesOf("skillpet");
                    for (int i = 0; i < 7 && i < ids.Count; i++) tt.State.Tech[ids[i]] = 1;
                    if (ids.Count > 6) tt.State.Research = new TechResearch(ids[6], SaveIo.NowMs() + (13 * 60 + 30) * 60e3);
                    OpenSummon(SkillPetSheet.SubTech);
                    if (TechPanel.Instance != null) TechPanel.Instance.ShowBranch("skillpet");
                },
                Opened = delegate { return TechPanel.Instance != null && TechPanel.Instance.Current == TechPanel.View.Branch; }
            });
            // 원작 042605 의 팝업 노드는 '추가 알 획득 기회 IV' = extraEgg 4단계 · 연구 진행 중.
            // 해금 규칙이 «바로 위 행 노드 전부» 라 조상 전체를 1레벨로 펴 두지 않으면 [잠김] 짧은 카드가 찍힌다(정본 주석).
            list.Add(new Shot
            {
                Name = "tech-node", Ref = "042605",
                Open = delegate
                {
                    TechTree tt = P.Tech;
                    string id = tt.Nid("extraEgg", 4);
                    OpenUp(tt, id, 0);
                    tt.State.Tech[id] = 1;
                    tt.State.Research = new TechResearch(id, SaveIo.NowMs() + (13 * 60 + 30) * 60e3);
                    OpenSummon(SkillPetSheet.SubTech);
                    if (TechPanel.Instance != null) TechPanel.Instance.ShowBranch("skillpet");
                    TechPopups.OpenNode(id);
                },
                Opened = delegate { return TechPopups.IsNodeOpen; }
            });
            list.Add(new Shot
            {
                Name = "shop", Ref = "042632",
                Open = delegate { M.OpenShop(); },
                Opened = delegate { return Popups.IsOpen(ShopSheet.Name); }
            });
            list.Add(new Shot
            {
                Name = "pass", Ref = "042705",
                Open = delegate { M.OpenPass(); },
                Opened = delegate { return Popups.IsOpen(PassPopup.Name); }
            });
            list.Add(new Shot
            {
                Name = "profile", Ref = "042724",
                Open = delegate { M.OpenProfile(); },
                Opened = delegate { return Popups.IsOpen(ProfilePopup.Name) && ProfilePopup.View == "profile"; }
            });
            list.Add(new Shot
            {
                Name = "settings", Ref = "042744",
                Open = delegate { M.OpenProfile(); ProfilePopup.SwitchView(M, "settings"); },
                Opened = delegate { return Popups.IsOpen(ProfilePopup.Name) && ProfilePopup.View == "settings"; }
            });
            list.Add(new Shot
            {
                Name = "forge-info", Ref = "042831",
                Open = delegate { ForgeInfoPopup.Open(F); },
                Opened = delegate { return Popups.IsOpen(ForgeInfoPopup.Name) && ForgeInfoPopup.View == "level"; }
            });
            list.Add(new Shot
            {
                Name = "forge-list", Ref = "042905",
                Open = delegate { ForgeInfoPopup.OpenList(F); },
                Opened = delegate { return Popups.IsOpen(ForgeInfoPopup.Name) && ForgeInfoPopup.View == "list"; }
            });
            list.Add(new Shot
            {
                Name = "forge-detail", Ref = "042931",
                Open = delegate
                {
                    ForgeInfoPopup.OpenList(F);
                    ForgeInfoPopup.OpenDetail(F, F.Defs.Ages[0], "weapon", 0, F.Defs.WeaponTypes.KeyAt(0));
                },
                Opened = delegate { return Popups.IsOpen(ForgeInfoPopup.ItemName); }
            });
            // ⚠ 정본 주석: 원작 짝이 뒤집혀 있었다 — 043117 이 필터 OFF · 042950 이 필터 ON.
            list.Add(new Shot
            {
                Name = "autoforge", Ref = "043117",
                Open = delegate
                {
                    F.Pull();
                    F.Engine.AutoForgeConfig().FilterOn = false;
                    F.Push();
                    ForgeAutoPopup.Open(F);
                },
                Opened = delegate { return Popups.IsOpen(ForgeAutoPopup.Name); }
            });
            list.Add(new Shot
            {
                Name = "autoforge-filter", Ref = "042950",
                Open = delegate
                {
                    F.Pull();
                    F.Engine.AutoForgeConfig().FilterOn = true;
                    F.Push();
                    ForgeAutoPopup.Open(F);
                },
                Opened = delegate { return Popups.IsOpen(ForgeAutoPopup.Name); }
            });
            // 새 장비도 원작처럼 서브옵션 2줄로 고정(랜덤 1~4줄이면 카드 높이가 매 런 달라진다).
            list.Add(new Shot
            {
                Name = "craft-compare", Ref = "043224",
                Open = delegate
                {
                    ForgeItem it = F.Engine.RollItem();
                    it.Subs = SubstatRoll.Roll(F.Defs, CoreRng.Mulberry(43224), 2);
                    ForgeCraftPopup.Show(F, it);
                },
                Opened = delegate { return Popups.IsOpen(ForgeCraftPopup.Name); }
            });
            list.Add(new Shot
            {
                Name = "gear-detail", Ref = "043244",
                Open = delegate { GearDetailPopup.Open(F, "weapon"); },
                Opened = delegate { return Popups.IsOpen(GearDetailPopup.Name); }
            });
            list.Add(new Shot
            {
                Name = "player-info", Ref = "043313",
                Open = delegate { M.OpenPlayerInfo(); },
                Opened = delegate { return Popups.IsOpen(PlayerInfoPopup.Name); }
            });
            list.Add(new Shot
            {
                Name = "chat", Ref = "043500",
                Open = delegate { M.OpenChat(); },
                Opened = delegate { return Popups.IsOpen(ChatScreen.Name); }
            });
            // 탈것 화면은 원작에도 shot 짝이 없다(정본 SCREENS 의 `['mounts', null, …]`). 유니티 쪽은 T20 이 세운다 — 서면 찍고 아니면 건너뛴다.
            // T20 이 세웠다: 서브탭이 아니라 원작 #mount-modal 그대로 전체 모달(`MountSheet.Open()` · 장비 시트 탈것 칸이 부른다). 태엽을 넉넉히 주고 개체가 둘 이상 되게 소환한 뒤 찍는다.
            list.Add(new Shot
            {
                Name = "mounts", Ref = null, Optional = true,
                Open = delegate { OpenMounts(); },
                Opened = delegate { return MountSheet.IsOpen; }
            });
            list.Add(new Shot
            {
                Name = "mount-detail", Ref = null, Optional = true,
                Open = delegate { OpenMounts(); MountSheet.OpenDetail(0); },
                Opened = delegate { return MountSheet.IsOpen && Sheet.Modal.IsOpen(MountSheet.DetailModal); }
            });
            list.Add(new Shot
            {
                Name = "mount-upgrade", Ref = null, Optional = true,
                Open = delegate { OpenMounts(); MountUpgradePopup.Open(Sheet, 0); },
                Opened = delegate { return MountSheet.IsOpen && Sheet.Modal.IsOpen(MountUpgradePopup.ModalName); }
            });
            // 주인 지시(2026-09-12 «SafeArea 해서 모바일 상단 카메라 안 가리게») — T45 가 세운 노치 모의(`UiRoot.NotchSafeArea` 120/60)로 한 장 더 찍는다.
            // 원작 30장은 노치 없는 조건으로 찍힌 것이라 대조 대상에서 빼고(ref null), 이 한 장으로 «노치 폰에서 상단바가 카메라에 안 걸리는가» 를 눈으로 본다.
            // 앱 상자만 잘라 내므로 잘린 그림의 9:16 비율은 그대로다 — T28 비율 채점에는 영향이 없다.
            list.Add(new Shot
            {
                Name = "main-notch", Ref = null, Optional = true, Notch = true,
                Open = delegate { CloseAll(); },
                Opened = delegate { return Popups.OpenCount == 0; }
            });
            return list;
        }

        private static void OpenUp(TechTree tt, string id, int depth)
        {
            if (depth > 12) return;
            foreach (string p in tt.ParentsOf(id))
            {
                if (tt.State.Level(p) < 1) tt.State.Tech[p] = 1;
                OpenUp(tt, p, depth + 1);
            }
        }

        private static void OpenSummon(string sub)
        {
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            // ⚠ SkillPetSheet.Subs 에 없는 이름을 넘기면 Switch/SubVisible 이 Array.IndexOf −1 로 색인해 터진다 — 아는 서브탭만 누른다(탈것은 T20).
            if (sub == SkillPetSheet.SubSkills || sub == SkillPetSheet.SubPets || sub == SkillPetSheet.SubTech) Sheet.Switch(sub);
        }

        /// <summary>화면 사이 오염 제거 — 원작 shot-screens.js 의 «전 .modal 강제 닫기 + 탭 되돌리기 + 토스트 소거» 와 같은 자리.</summary>
        /// <summary>탈것 시트를 연다 — 개체가 둘 미만이면 태엽을 주고 x1 로 소환해(결과 연출은 바로 닫는다) 상세·업그레이드 재료가 있게 한다(T20 · PetUiTests 와 같은 길).</summary>
        private static void OpenMounts()
        {
            if (P == null || P.Mounts == null) return;
            while (P.SummonMult("mount") != 1) P.CycleSummonMult("mount");
            SaveIo.State.Winders = 5000;
            P.Sync();
            MountSheet.Open();
            for (int guard = 0; guard < 4 && P.Mounts.Count() < 2; guard++)
            {
                MountSheet.OnSummon();
                if (Sheet != null && Sheet.Modal != null) Sheet.Modal.Close(SkillSummonResultView.ModalName);
            }
            SaveIo.State.Winders = 320;   // Seed() 값으로 되돌려 알약이 원작 시드와 같게
            P.Sync();
            MountSheet.Refresh();
        }

        private static void CloseAll()
        {
            TechPopups.Close();
            AscendPopup.Close();
            DungeonDetailPopup.Close();
            if (DungeonSheet.Instance != null) DungeonSheet.Instance.Close();
            if (Sheet != null && Sheet.Modal != null) Sheet.Modal.CloseAll();
            if (Popups != null) Popups.HideAll();
            UiRoot.Instance.TabBar.CloseOpened();
        }

        /// <summary>열린 화면의 글자가 T18 종류 하한 이상인가 — 어긋난 것을 문자열로 모아 돌려준다(비면 성했다).</summary>
        private static string TextGate(string where)
        {
            UiCatalog cat = UiCatalog.Instance;
            var bad = new List<string>();
            foreach (TMP_Text t in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            {
                if (!t.gameObject.activeInHierarchy) continue;
                UiTextKindTag tag = t.GetComponent<UiTextKindTag>();
                if (tag == null) { bad.Add(where + ": " + t.name + " 은 UiKit.Text 를 안 거쳤다"); continue; }
                float min = cat.Kind(tag.Kind).min;
                if (t.fontSize < min) bad.Add(where + ": " + t.name + " 글자 " + t.fontSize + " < 하한 " + min);
                if (bad.Count >= 5) break;
            }
            return bad.Count == 0 ? null : string.Join(" · ", bad.ToArray());
        }

        /// <summary>
        /// 앱 상자(9:16)를 `ui-screens/screen_&lt;이름&gt;.png`(<see cref="ShotW"/>×<see cref="ShotH"/>) 로 남긴다. 그래픽 장치가 없으면 null.
        /// ⚠ `ScreenCapture.CaptureScreenshotAsTexture()` 는 프레임 끝에서만 옳은데 그 자리를 잡는 코루틴 대기가 **배치모드에서 안 불려**
        /// 예외로 PlayMode 런이 통째로 죽는다(CI 런 60 실측). 그래서 카메라 사본으로 RenderTexture 에 직접 그린다(T45 `SafeAreaTests` 가 런 64 에서 검증한 길).
        ///
        /// T83 — 한 장에 «UI + 게임 framing» 을 같이 담는다. 카메라 하나로 같이 그리면 둘 중 하나가 어긋난다(T54 가 받은 벽 셋: rect 띠는 URP 가
        /// 세계를 안 그리고 · 투영을 걸면 `ScreenSpaceCamera` 캔버스가 프러스텀을 따라 밀린다 · `CopyFrom` 은 투영까지 복사한다). 그래서 **따로 찍어 합성**한다:
        ///   ① 세계 — 게임과 같은 절두체(<see cref="Bootstrap.ApplyGameAreaProjection"/> · 촬영 RT 비율로 셈) · UI 층 제외 · 캔버스는 오버레이인 채(RT 에 안 실린다)
        ///   ② UI — 기본 투영 · UI 층만 · 검정 배경 위 한 번, 흰 배경 위 한 번(RT 알파는 UGUI 블렌드가 srcA² 로 적어 못 믿는다 — 두 장의 차가 정확한 매트다)
        ///   ③ 합성 — 채널마다 `out = Cb + world × (1 − (Cw − Cb))` (Cb = 검정 위 · Cw = 흰 위). 딤·반투명 판이 게임과 같은 밝기로 얹힌다.
        /// safeArea 는 호출자가 이미 <see cref="ShotW"/>×<see cref="ShotH"/> 로 꽂아 두었으므로 앱 상자가 RT 를 꽉 채운다.
        /// 실패하면 경고 한 줄(빨강 아님)만 남기고 그림을 건너뛴다 — 이 테스트의 판정은 «열렸는가·글자·빨강 0» 이지 그림이 아니다(픽셀 자는 T84).
        /// </summary>
        private static string Capture(string name, bool notch, out string pixelFail, out string pixelInfo)
        {
            pixelFail = null; pixelInfo = null;
            UiRoot root = UiRoot.Instance;
            if (root == null || root.Canvas == null) return null;
            Canvas canvas = root.Canvas;
            RenderMode prevMode = canvas.renderMode;
            Camera prevCam = canvas.worldCamera;
            float prevPlane = canvas.planeDistance;
            RenderTexture prevActive = RenderTexture.active;
            RenderTexture rt = new RenderTexture(ShotW, ShotH, 24, RenderTextureFormat.ARGB32);
            GameObject camGo = new GameObject("t27-shot-cam");
            Camera cam = camGo.AddComponent<Camera>();
            Texture2D world = null, onBlack = null, onWhite = null, shot = null;
            try
            {
                // T128 ⓒ — 찍기 전에 **돌고 있는 카드 팝을 끝낸다**. 정지 촬영이 팝 도중(불투명도 0→1 · 배율 .7→1)을 찍으면
                // 팝업 화면이 반투명·축소로 남아 T28 채점이 «내려간 화면» 으로 읽는다(런 341 실측 · `CardPop.SettleAll` 의 주석이
                // 이 자리를 가리켜 두었다). 게임 흐름은 안 건드린다 — 촬영 자만 부른다.
                CardPop.SettleAll();
                int uiLayer = canvas.gameObject.layer;
                if (Camera.main != null) cam.CopyFrom(Camera.main);
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.targetTexture = rt;

                // ① 세계 — 게임 절두체 · UI 층 제외. 캔버스는 아직 오버레이라 이 그림에 안 실린다.
                cam.cullingMask &= ~(1 << uiLayer);
                cam.ResetProjectionMatrix();
                Bootstrap.ApplyGameAreaProjection(cam);
                cam.Render();
                world = ReadBack(rt, TextureFormat.RGB24);

                // ② UI — 기본 투영 · UI 층만 · 검정/흰 배경 위 두 번.
                cam.ResetProjectionMatrix();
                cam.cullingMask = 1 << uiLayer;
                cam.clearFlags = CameraClearFlags.SolidColor;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                root.Layout();
                Canvas.ForceUpdateCanvases();
                cam.backgroundColor = Color.black;
                cam.Render();
                onBlack = ReadBack(rt, TextureFormat.RGB24);
                cam.backgroundColor = Color.white;
                cam.Render();
                onWhite = ReadBack(rt, TextureFormat.RGB24);

                // T84 — 찍은 그 자리에서 매트를 읽는다(파일로 돌지 않는다) · 노치 줄은 safeArea 위 띠가 비는 것이 정상이라 뺀다(T45 의 몫).
                if (!notch) pixelFail = PixelGate(name, onBlack, onWhite, world, out pixelInfo);

                // ③ 합성.
                shot = Composite(world, onBlack, onWhite);
                return GallerySheet.Save(shot, OutPrefix + name);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[UiShots] " + name + " 촬영 실패(그림만 건너뛴다): " + e.Message);
                return null;
            }
            finally
            {
                RenderTexture.active = prevActive;
                canvas.renderMode = prevMode;
                canvas.worldCamera = prevCam;
                canvas.planeDistance = prevPlane;
                if (root != null) root.Layout();
                Canvas.ForceUpdateCanvases();
                if (world != null) UnityEngine.Object.Destroy(world);
                if (onBlack != null) UnityEngine.Object.Destroy(onBlack);
                if (onWhite != null) UnityEngine.Object.Destroy(onWhite);
                if (shot != null) UnityEngine.Object.Destroy(shot);
                UnityEngine.Object.Destroy(camGo);
                rt.Release();
            }
        }

        static Texture2D ReadBack(RenderTexture rt, TextureFormat fmt)
        {
            RenderTexture.active = rt;
            var t = new Texture2D(rt.width, rt.height, fmt, false);
            t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            t.Apply(false);
            return t;
        }

        /// <summary>T84 — 띠의 UI 덮임 하한(0~1). 상단바·탭바는 불투명 판이라 성하면 ≈1 · 세계가 보이면 0 이다 — 딤(≈.5)만 있는 자리는 없다(그 아래 판이 불투명).</summary>
        public const float BandCoverMin = 0.6f;
        /// <summary>T84 — UI 가 있는 행의 세로 범위 ÷ 판 높이 하한(ROUTINE T84 ⓒ · `ui_score` 의 «채움» 과 같은 뜻).</summary>
        public const float FillMin = 0.98f;

        static float Frac(string key, float fallback)
        {
            try { UiCatalog c = UiCatalog.Instance; return c == null ? fallback : c.Layout(key); }
            catch (Exception) { return fallback; }
        }

        /// <summary>
        /// T84 — 찍은 RT 의 매트(검정 위·흰 위의 차 = UI 덮임)로 세 칸을 본다: ⓐ 상단 띠(카탈로그 `topbar_h`)가 UI 로 덮였는가 ⓑ 바닥 띠(`tabbar_top` 아래)가 UI 로 덮였는가
        /// ⓒ UI 가 있는 행의 세로 범위가 판의 <see cref="FillMin"/> 이상인가. 프레임 회귀(런 102·108·137~139)가 세 번 «유니티 잡 초록» 으로 지나간 자리다.
        /// 실패 문구에 화면·띠·읽은 색을 적는다(`playmode-red.txt` 로 바로 읽힌다 · T46 꼴). 픽셀은 아래가 0행(`ReadPixels`)이라 «위» 는 큰 y 다.
        /// </summary>
        public static string PixelGate(string name, Texture2D onBlack, Texture2D onWhite, Texture2D world, out string info)
        {
            int w = onBlack.width, h = onBlack.height;
            Color32[] b = onBlack.GetPixels32(), k = onWhite.GetPixels32();
            var rowMax = new int[h]; var rowSum = new long[h];
            for (int y = 0; y < h; y++)
            {
                int o = y * w, mx = 0; long sum = 0;
                for (int x = 0; x < w; x++)
                {
                    int cover = 255 - (k[o + x].g - b[o + x].g);
                    if (cover < 0) cover = 0; else if (cover > 255) cover = 255;
                    sum += cover; if (cover > mx) mx = cover;
                }
                rowMax[y] = mx; rowSum[y] = sum;
            }
            int topH = Mathf.RoundToInt(Frac("topbar_h", 0.065f) * h);
            int botH = Mathf.RoundToInt((1f - Frac("tabbar_top", 0.9025f)) * h);
            float top = Band(rowSum, w, h - topH + topH / 10, h - topH / 10);   // 띠의 안쪽 80%
            float bottom = Band(rowSum, w, botH / 10, botH - botH / 10);
            int first = -1, last = -1;
            for (int y = 0; y < h; y++) if (rowMax[y] >= 128) { if (first < 0) first = y; last = y; }
            float fill = first < 0 ? 0f : (last - first + 1) / (float)h;
            info = "픽셀 상단 " + Pct(top) + " · 바닥 " + Pct(bottom) + " · 채움 " + Pct(fill);
            var bad = new List<string>();
            if (top < BandCoverMin) bad.Add("상단 띠(위 " + topH + "px · topbar_h) UI 덮임 " + Pct(top) + " < " + Pct(BandCoverMin) + " — 상단바가 없다(세계 색 " + Rgb(world, w / 2, h - topH / 2) + ")");
            if (bottom < BandCoverMin) bad.Add("바닥 띠(아래 " + botH + "px · tabbar_top) UI 덮임 " + Pct(bottom) + " < " + Pct(BandCoverMin) + " — UI 가 바닥까지 안 닿는다(세계 색 " + Rgb(world, w / 2, botH / 2) + ")");
            if (fill < FillMin) bad.Add("UI 세로 채움 " + Pct(fill) + " < " + Pct(FillMin) + " — 앱 상자가 그림을 안 채운다(UI 행 " + (h - 1 - last) + "~" + (h - 1 - first) + " / " + h + " · 위에서 셈)");
            return bad.Count == 0 ? null : name + ": " + string.Join(" · ", bad.ToArray());
        }

        static float Band(long[] rowSum, int w, int y0, int y1)
        {
            if (y1 <= y0) return 0f;
            long sum = 0; for (int y = y0; y < y1; y++) sum += rowSum[y];
            return sum / (255f * w * (y1 - y0));
        }

        static string Pct(float f) { return Mathf.RoundToInt(f * 100f) + "%"; }

        static string Rgb(Texture2D t, int x, int y)
        {
            Color32 c = t.GetPixel(Mathf.Clamp(x, 0, t.width - 1), Mathf.Clamp(y, 0, t.height - 1));
            return c.r + "," + c.g + "," + c.b;
        }

        /// <summary>T83 ③ — 채널마다 <c>out = Cb + world × (1 − (Cw − Cb))</c>. 검정 위(Cb)가 «프리멀티플라이드 UI», 흰 위와의 차가 «덮임» 이다.</summary>
        public static Texture2D Composite(Texture2D world, Texture2D onBlack, Texture2D onWhite)
        {
            Color32[] w = world.GetPixels32(), b = onBlack.GetPixels32(), k = onWhite.GetPixels32();
            var o = new Color32[w.Length];
            for (int i = 0; i < w.Length; i++)
            {
                o[i] = new Color32(Over(b[i].r, k[i].r, w[i].r), Over(b[i].g, k[i].g, w[i].g), Over(b[i].b, k[i].b, w[i].b), 255);
            }
            var t = new Texture2D(world.width, world.height, TextureFormat.RGB24, false);
            t.SetPixels32(o);
            t.Apply(false);
            return t;
        }

        static byte Over(byte cb, byte cw, byte world)
        {
            int cover = 255 - (cw - cb);               // 0 = UI 가 없다 · 255 = UI 가 다 가린다
            if (cover < 0) cover = 0; else if (cover > 255) cover = 255;
            int v = cb + world * (255 - cover) / 255;
            return (byte)(v > 255 ? 255 : v);
        }

        /// <summary>한 줄씩 바로 덧붙인다(런이 중간에 죽어도 «어디까지 갔는지» 는 남는다).</summary>
        private static readonly List<string> traceLines = new List<string>();

        /// <summary>자취 꼬리 — 실패 메시지에 붙여 결과 XML(그리고 T46 `playmode-red.txt`)로도 나가게 한다. `screens` 브랜치는 뒤 런이 덮어써 놓칠 수 있다.</summary>
        private static string TraceTail(int n)
        {
            int from = traceLines.Count - n;
            if (from < 0) from = 0;
            return string.Join(" | ", traceLines.GetRange(from, traceLines.Count - from).ToArray());
        }

        private static void Trace(string line, bool reset = false)
        {
            if (reset) traceLines.Clear();
            traceLines.Add(line);
            try
            {
                string dir = Path.Combine(Directory.GetCurrentDirectory(), GallerySheet.OutDir);
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, TraceFile);
                if (reset) File.WriteAllText(file, line + "\n", new UTF8Encoding(false));
                else File.AppendAllText(file, line + "\n", new UTF8Encoding(false));
            }
            catch (Exception) { /* 자취가 테스트를 죽이지 않는다 */ }
        }

        /// <summary>T28 이 읽는 짝 표 — 클론 PNG ↔ 원작 `ref/screens/shot-&lt;번호&gt;.png`.</summary>
        private static void WriteManifest(List<Shot> shots, Dictionary<string, string> files)
        {
            var sb = new StringBuilder();
            sb.Append("{\"source\":\"web/tools/shot-screens.js SCREENS\",\"prefix\":\"" + OutPrefix + "\",\"screens\":[");
            for (int i = 0; i < shots.Count; i++)
            {
                Shot s = shots[i];
                string file;
                files.TryGetValue(s.Name, out file);
                if (i > 0) sb.Append(",");
                sb.Append("{\"name\":\"" + s.Name + "\",");
                sb.Append("\"ref\":" + (s.Ref == null ? "null" : "\"shot-" + s.Ref + ".png\"") + ",");
                sb.Append("\"file\":" + (file == null ? "null" : "\"" + OutPrefix + s.Name + ".png\"") + "}");
            }
            sb.Append("]}");
            string dir = Path.Combine(Directory.GetCurrentDirectory(), GallerySheet.OutDir);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "screens.json"), sb.ToString());
        }

        [UnityTest]
        public IEnumerator 원작_화면_전부를_열어_콘솔_빨강_0_이고_시트를_남긴다()
        {
            yield return Boot();
            PlayLog log = PlayLog.Start("ui-shots");
            var files = new Dictionary<string, string>();
            var failed = new List<string>();
            List<Shot> shots = null;
            string bootErr = null;
            Trace("boot ok · 촬영=" + ShotW + "x" + ShotH);
            try
            {
                Seed();
                Trace("seed ok");
                // T128 — 촬영 상태가 런마다 같은지를 **자가 묻는다**(자취 한 줄은 사람이 봐야 하지만 이 단언은 CI 가 본다).
                Assert.AreEqual(GearSignatureExpected, GearSignature,
                    "촬영 장비 상태가 기준과 다르다 — 난수가 다시 흔들렸거나 정본 표가 바뀌었다(T128 · 기록을 보고 서명을 갱신하라)");
                shots = Screens();
                Trace("목록 " + shots.Count + "줄");
            }
            catch (Exception e)
            {
                bootErr = "캡처용 시드/목록을 세우다 터졌다: " + e;
                Trace("BOOTFAIL " + bootErr);
            }
            if (bootErr != null)
            {
                log.Dispose();
                Assert.Fail(bootErr);
            }
            yield return null;
            yield return new WaitForSeconds(0.5f);   // 전투 씬이 자리잡을 시간(원작 2.5s · 여기는 씬만 서면 된다)
            AssertCombatPowerTookGear();             // T77 — 상단바가 «⚔ 45»(맨몸)로 찍히던 자리

            for (int i = 0; i < shots.Count; i++)
            {
                Shot s = shots[i];
                log.Mark(s.Name);
                Trace("→ " + s.Name);
                bool threw = false;
                // 촬영 크기의 safeArea 를 꽂아 둔다 — 앱 상자가 RT 를 꽉 채우고(9:16 정확) 러너 창 크기와 무관하게 같은 그림이 나온다.
                try { UiRoot.OverrideSafeArea(s.Notch ? UiRoot.NotchSafeArea(ShotW, ShotH) : new Rect(0f, 0f, ShotW, ShotH)); }
                catch (Exception e) { failed.Add(s.Name + ": safeArea 를 꽂다 터졌다 — " + e.Message); threw = true; }
                if (!threw)
                {
                    try { CloseAll(); }
                    catch (Exception e) { failed.Add(s.Name + ": 앞 화면을 닫다 터졌다 — " + e.Message); threw = true; }
                }
                yield return null;
                if (!threw)
                {
                    try { s.Open(); }
                    catch (Exception e) { failed.Add(s.Name + ": 여는 중 터졌다 — " + e.Message); Trace("  OPENFAIL " + e.Message); threw = true; }
                }
                CardPop.SettleAll();   // T135 ⓑ·T128 ⓒ — 정지 촬영은 카드 팝(.25s)이 끝난 모습을 찍는다(런 341 은 반투명·축소 중이 찍혔다)
                yield return null;
                yield return null;
                if (threw) continue;

                bool opened = false;
                try { opened = s.Opened(); }
                catch (Exception e) { failed.Add(s.Name + ": 열림 확인이 터졌다 — " + e.Message); Trace("  PREDFAIL " + e.Message); continue; }
                Trace("  열림=" + opened);
                if (!opened)
                {
                    if (!s.Optional) failed.Add(s.Name + ": 화면이 안 열렸다" + (s.Ref != null ? " (원작 shot-" + s.Ref + ".png)" : ""));
                    continue;
                }

                string gate = null;
                try { gate = TextGate(s.Name); }
                catch (Exception e) { gate = s.Name + ": 글자 게이트가 터졌다 — " + e.Message; }
                if (gate != null) { failed.Add(gate); Trace("  GATE " + gate); }

                if (!GallerySheet.GraphicsAvailable) continue;
                string pixelFail, pixelInfo;
                string file = Capture(s.Name, s.Notch, out pixelFail, out pixelInfo);
                if (file != null) files[s.Name] = file;
                Trace("  그림=" + (file != null ? "ok" : "없음") + (pixelInfo != null ? " · " + pixelInfo : ""));
                if (pixelFail != null) { failed.Add(pixelFail); Trace("  PIXEL " + pixelFail); }
            }

            log.Mark("ui-shots");
            UiRoot.OverrideSafeArea(null);
            try { CloseAll(); } catch (Exception) { /* 정리 중 터진 것은 아래 빨강 단언이 잡는다 */ }
            yield return null;

            try { WriteManifest(shots, files); }
            catch (Exception e) { Debug.LogWarning("[UiShots] screens.json 을 못 썼다: " + e.Message); }

            int required = 0;
            for (int i = 0; i < shots.Count; i++) if (!shots[i].Optional) required++;
            Trace("done · 필수 " + required + "장 · 어긋남 " + failed.Count + " · PNG " + files.Count + " · 빨강 " + log.RedCount);
            log.Dispose();

            if (failed.Count > 0) Assert.Fail("원작 화면 " + required + "장 중 " + failed.Count + "건이 어긋났다:\n  · " + string.Join("\n  · ", failed.ToArray()) + "\n자취 꼬리: " + TraceTail(10));
            log.AssertNoRed();
            Debug.Log("[UiShots] 화면 " + required + "장 열림 · PNG " + files.Count + "장(" + (GallerySheet.GraphicsAvailable ? "그래픽 있음" : "-nographics") + ")");
        }
    }
}
