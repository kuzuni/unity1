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
            PetSkillHost.Seed = 20260912;
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
            foreach (string slot in F.Defs.Slots)
            {
                ForgeItem it = null;
                for (int i = 0; i < 60 && it == null; i++)
                {
                    ForgeItem r = F.Engine.RollItem();
                    if (r != null && r.Slot == slot) it = r;
                }
                if (it == null) continue;
                it.Level = 20 + it.AgeIdx;
                it.Subs = SubstatRoll.Roll(F.Defs, rng, 2);
                F.Gear.Set(slot, it);
            }
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
            M.Touch(false);
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
        /// 예외로 PlayMode 런이 통째로 죽는다(CI 런 60 실측). 그래서 T45 `SafeAreaTests` 가 CI 런 64 에서 검증한 길을 그대로 쓴다 —
        /// 본 카메라 사본을 RenderTexture 에 그리고, 오버레이 캔버스를 잠깐 그 카메라의 `ScreenSpaceCamera` 로 옮겨 같은 그림에 얹는다.
        /// safeArea 는 호출자가 이미 <see cref="ShotW"/>×<see cref="ShotH"/> 로 꽂아 두었으므로 앱 상자가 RT 를 꽉 채운다.
        /// 실패하면 경고 한 줄(빨강 아님)만 남기고 그림을 건너뛴다 — 이 테스트의 판정은 «열렸는가·글자·빨강 0» 이지 그림이 아니다.
        /// </summary>
        private static string Capture(string name)
        {
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
            GameObject uiCamGo = new GameObject("t27-shot-ui-cam");
            Camera uiCam = uiCamGo.AddComponent<Camera>();
            Texture2D shot = null;
            try
            {
                // T54 — 게임과 같은 겹: **3D 는 원작 `#game-area` 띠에만**(cam) · **UI 는 앱 상자 전체**(uiCam).
                // 한 카메라로 둘 다 그리면(옛 코드) 캔버스가 그 카메라의 rect 를 따라가 UI 까지 띠 안으로 눌린다 — 런 106 에서 실제로 그랬다.
                if (Camera.main != null) cam.CopyFrom(Camera.main);
                ViewportRect band = Viewport.GameArea(new ViewportRect(0f, 0f, 1f, 1f), Bootstrap.GameAreaTop, Bootstrap.GameAreaBottom);
                cam.rect = new Rect(band.X, band.Y, band.W, band.H);
                cam.targetTexture = rt;

                uiCam.CopyFrom(cam);
                uiCam.rect = new Rect(0f, 0f, 1f, 1f);
                uiCam.clearFlags = CameraClearFlags.Depth;     // 띠에 그린 3D 를 지우지 않는다
                uiCam.cullingMask = 1 << canvas.gameObject.layer;   // 캔버스가 실제로 선 레이어만 — 세계는 안 그린다(띠 밖으로 새면 원작에 없는 그림이다)
                uiCam.targetTexture = rt;

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = uiCam;
                canvas.planeDistance = 1f;
                root.Layout();
                Canvas.ForceUpdateCanvases();
                // 띠 밖(상단바·시트·채팅·탭바 자리)은 어느 카메라도 지우지 않으므로 먼저 한 번 민다 — 불투명 UI 가 그 위를 덮는다.
                RenderTexture.active = rt;
                GL.Clear(true, true, Color.black);
                RenderTexture.active = prevActive;
                cam.Render();
                uiCam.Render();
                RenderTexture.active = rt;
                shot = new Texture2D(ShotW, ShotH, TextureFormat.RGB24, false);
                shot.ReadPixels(new Rect(0, 0, ShotW, ShotH), 0, 0);
                shot.Apply(false);
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
                if (shot != null) UnityEngine.Object.Destroy(shot);
                UnityEngine.Object.Destroy(camGo);
                UnityEngine.Object.Destroy(uiCamGo);
                rt.Release();
                UnityEngine.Object.Destroy(rt);
            }
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
                string file = Capture(s.Name);
                if (file != null) files[s.Name] = file;
                Trace("  그림=" + (file != null ? "ok" : "없음"));
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
