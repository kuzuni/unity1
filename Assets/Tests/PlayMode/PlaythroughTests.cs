using System;
using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Dungeon;
using Forge.Core.Forging;
using Forge.Game;
using Forge.Game.Battle;
using Forge.Game.Gallery;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T27 — «플레이 봇»: 부팅 한 판을 원작 손놀림 순서대로 끝까지 몬다 — 전투 → 제작 → 장착 → 펫(소환·부화·출전) → 스킬(소환·장착) → 던전(입장·클리어·보상).
    /// 화면마다 따로 도는 T19~T22 테스트와 달리 **한 세이브·한 씬 위에서 이어서** 민다 — 시스템끼리 물릴 때만 나오는 빨강(재화 이중 차감 · 세이브 덮어쓰기 · 재렌더 루프)이 여기서 잡힌다.
    /// 도는 내내 콘솔 빨강 0(<see cref="PlayLog"/> · §1 «플레이 콘솔 에러 0» · `LogAssert.NoUnexpectedReceived()` 는 안 쓴다).
    ///
    /// 전투는 실시간을 기다리지 않고 <see cref="BattleScene.ManualStep"/> 으로 시뮬 시간을 민다(T8 `BattleSceneTests` 와 같은 수법 · 프레임당 0.05초 × 6).
    /// </summary>
    public class PlaythroughTests
    {
        private static MetaHost M { get { return MetaHost.Instance; } }
        private static ForgeHost F { get { return ForgeHost.Instance; } }
        private static PetSkillHost P { get { return PetSkillHost.Instance; } }
        private static DungeonUiHost D { get { return DungeonUiHost.Instance; } }
        private static SkillPetSheet Sheet { get { return SkillPetSheet.Instance; } }

        [SetUp]
        public void NoDiskSave() { PetSkillHost.SuppressSave = true; }

        [TearDown]
        public void DropSave()
        {
            PetSkillHost.SuppressSave = false;
            if (BattleScene.Instance != null) BattleScene.Instance.ManualStep = false;
            try { if (File.Exists(SaveIo.SavePath)) File.Delete(SaveIo.SavePath); }
            catch (Exception) { /* 저장소 접근 실패는 무시 */ }
        }

        /// <summary>어디까지 갔는지 `ui-screens/playthrough.txt` 에 한 줄씩(런이 중간에 죽어도 남는다 · CI 잡 로그는 꼬리 5000줄로 잘리고 아티팩트는 프록시가 막는다 — T46 과 같은 사정).</summary>
        private static void Trace(string line, bool reset = false)
        {
            try
            {
                string dir = Path.Combine(Directory.GetCurrentDirectory(), GallerySheet.OutDir);
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, "playthrough.txt");
                if (reset) File.WriteAllText(file, line + "\n", new UTF8Encoding(false));
                else File.AppendAllText(file, line + "\n", new UTF8Encoding(false));
            }
            catch (Exception) { /* 자취가 테스트를 죽이지 않는다 */ }
        }

        private static IEnumerator Boot()
        {
            // 자취는 부팅보다 먼저 연다 — 부팅이 못 서면 그 자리에서 단언이 터져 그 뒤 줄이 안 남는다(CI 런 68 실측).
            Trace("# T27 PlaythroughTests 자취 — 한 판을 어디까지 몰았는지", true);
            Trace("boot 시작 · 배치=" + Application.isBatchMode + " · 화면=" + Screen.width + "x" + Screen.height);
            try { if (File.Exists(SaveIo.SavePath)) File.Delete(SaveIo.SavePath); }
            catch (Exception) { /* 없으면 그만 */ }
            BattleScene.AutoBoot = true;
            // T128 — 한 판 자취(`playthrough.txt`)도 런마다 같은 상태에서 나야 회차 사이 대조가 된다.
            PetSkillHost.Seed = 20260912;
            MetaHost.Seed = 20260912;
            DungeonUiHost.Seed = 20260912;
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = 0f;
            while (t < 30f && !(MetaHost.Ready && ForgeHost.Ready && PetSkillHost.Ready && DungeonUiHost.Ready
                                && SkillPetSheet.Instance != null && BattleScene.Instance != null && BattleScene.Instance.Ready))
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            string hosts = "meta=" + MetaHost.Ready + " forge=" + ForgeHost.Ready + " petskill=" + PetSkillHost.Ready
                           + " dungeon=" + DungeonUiHost.Ready + " sheet=" + (SkillPetSheet.Instance != null)
                           + " battle=" + (BattleScene.Instance != null && BattleScene.Instance.Ready) + " · " + t.ToString("0.0") + "초";
            Trace("boot 호스트 · " + hosts);
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 30초 안에 서지 않았다 — " + hosts);
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 30초 안에 서지 않았다 — " + hosts);
            Assert.IsTrue(PetSkillHost.Ready, "PetSkillHost 가 30초 안에 서지 않았다 — " + hosts);
            Assert.IsTrue(DungeonUiHost.Ready, "DungeonUiHost 가 30초 안에 서지 않았다 — " + hosts);
            Assert.IsNotNull(BattleScene.Instance, "전투 씬이 서지 않았다 — " + hosts);
            Assert.IsTrue(BattleScene.Instance.Ready, "전투 씬이 30초 안에 준비되지 않았다 — " + hosts);
            yield return null;
        }

        /// <summary>시뮬 시간을 민다 — 프레임당 0.05초 × <paramref name="stepsPerFrame"/>(T8 BattleSceneTests 와 같은 꼴).</summary>
        private static IEnumerator Run(BattleScene s, double seconds, int stepsPerFrame = 6)
        {
            double t = 0;
            while (t < seconds)
            {
                for (int i = 0; i < stepsPerFrame && t < seconds; i++) { s.Step(0.05f); t += 0.05; }
                yield return null;
            }
        }

        private static IEnumerator WaitCraftPopup()
        {
            float t = 0f;
            while (!F.Meta.Popups.IsOpen(ForgeCraftPopup.Name) && t < 8f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(F.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "망치질(0.72s) + 리빌(0.56s) 뒤 비교 팝업이 떠야 한다");
            yield return null;
        }

        private static IEnumerator CloseSummonResult()
        {
            float t = 0f;
            while (SkillSummonResultView.Current == null && t < 3f) { t += Time.unscaledDeltaTime; yield return null; }
            SkillSummonResultView v = SkillSummonResultView.Current;
            Assert.IsNotNull(v, "소환 결과 연출이 안 떴다");
            for (int i = 0; i < 4 && SkillSummonResultView.Current != null; i++)
            {
                SkillSummonResultView cur = SkillSummonResultView.Current;
                if (cur.Done && cur.OkButton != null && cur.OkButton.gameObject.activeInHierarchy) cur.OkButton.onClick.Invoke();
                else cur.OnTap();
                yield return null;
            }
            Assert.IsNull(SkillSummonResultView.Current, "소환 결과가 안 닫혔다");
        }

        [UnityTest]
        public IEnumerator 한_판_전투_제작_장착_펫_스킬_던전을_끝까지_몰아도_콘솔_빨강_0()
        {
            yield return Boot();
            PlayLog log = PlayLog.Start("playthrough");
            TabBar tb = UiRoot.Instance.TabBar;

            // ── ① 전투 — 새 세이브 1-1 에서 적이 서고 죽는가(규칙은 T7 Core · 여기선 «판이 굴러가는가»)
            log.Mark("전투");
            Trace("→ 전투");
            BattleScene bs = BattleScene.Instance;
            bs.ManualStep = true;
            yield return Run(bs, 60);
            Assert.Greater(bs.SpawnCount, 0, "적이 하나도 안 섰다");
            Assert.Greater(bs.EventCount, 0, "전투 이벤트가 씬에 하나도 안 왔다");
            Assert.Greater(bs.KillCount, 0, "60초를 돌려도 1-1 에서 한 마리도 못 잡았다");
            Assert.Greater(bs.Battle.Context.Kills, 0, "Core 전투가 처치를 안 셌다(원작 combat.js `S.kills++`)");
            Assert.Greater(SaveIo.State.Kills, 0, "세이브 kills 가 안 올랐다(T55 BattleSaveGlue · 원작 combat.js `S.kills++`)");
            Trace("  전투 · 스폰 " + bs.SpawnCount + " · 처치 " + bs.KillCount + " · ctx.kills " + bs.Battle.Context.Kills
                  + " · 세이브 kills " + SaveIo.State.Kills);

            // ── ② 제작 — 모루를 두드려 비교 팝업까지
            log.Mark("제작");
            Trace("→ 제작");
            bs.ManualStep = false;
            F.S.Hammers = 200;
            F.Pull();
            double hammers = F.S.Hammers;
            F.OnCraft();
            Assert.AreEqual(hammers - 1, F.S.Hammers, "제작은 해머 1 을 세이브에서 즉시 뺀다");
            yield return WaitCraftPopup();

            // ── ③ 장착 — 빈 부위면 팝업이 닫히고, 옛 장비가 있으면 맞바뀐 채 남는다(그때는 옛 장비를 판다)
            log.Mark("장착");
            Trace("→ 장착");
            ForgeItem item = F.Pending;
            Assert.IsNotNull(item, "대기품이 없다");
            string slot = item.Slot;
            F.ResolveCraft("equip");
            yield return null;
            Assert.AreSame(item, F.Gear.Get(slot), "장착이 칸을 안 채웠다");
            Assert.IsNotNull(F.S.Equipment[slot], "세이브 equipment 칸에 안 쓰였다");
            if (F.Meta.Popups.IsOpen(ForgeCraftPopup.Name))
            {
                F.ResolveCraft("sell");
                yield return null;
                if (F.Meta.Popups.IsOpen(ForgeCraftPopup.SellName)) { F.OnSellConfirm(); yield return null; }
            }
            Assert.IsFalse(F.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "제작 흐름이 안 끝났다");

            // ── ④ 펫 — 소환 → 부화 시작 → 젬 스킵 → 출전
            log.Mark("펫");
            Trace("→ 펫");
            tb.OnTab("summon");
            yield return null;
            Sheet.Switch(SkillPetSheet.SubPets);
            yield return null;
            while (P.SummonMult("pet") != 1) P.CycleSummonMult("pet");
            P.EggCurrency = 100000;
            P.Gems = 100000;
            P.Sync();
            yield return null;
            int eggsBefore = P.Pets.State.Eggs.Count;
            Sheet.Pets.SummonButton.onClick.Invoke();
            yield return null;
            yield return CloseSummonResult();
            Assert.Greater(P.Pets.State.Eggs.Count, eggsBefore, "펫 소환이 알을 안 늘렸다");

            int petsBefore = P.Pets.State.Pets.Count;
            Sheet.Pets.OnStartHatch(0);
            yield return null;
            Assert.Greater(P.Pets.State.Hatching.Count, 0, "부화가 안 걸렸다");
            Sheet.Pets.OnHatchSkip(0);
            yield return null;
            Assert.Greater(P.Pets.State.Pets.Count, petsBefore, "젬 스킵이 펫을 안 깠다");
            if (!P.Pets.State.ActivePets.Contains(0))
            {
                Sheet.Pets.OnTogglePet(0);
                yield return null;
            }
            Assert.Contains(0, P.Pets.State.ActivePets, "펫 출전이 안 걸렸다");

            // ── ⑤ 스킬 — 소환 → 결과 닫기 → 일괄 장착
            log.Mark("스킬");
            Trace("→ 스킬");
            Sheet.Switch(SkillPetSheet.SubSkills);
            yield return null;
            while (P.SummonMult("skill") != 1) P.CycleSummonMult("skill");
            P.Tickets = 100000;
            P.Sync();
            yield return null;
            int summonBefore = P.Skills.State.SummonCount;
            Sheet.Skills.SummonButton.onClick.Invoke();
            yield return null;
            yield return CloseSummonResult();
            Assert.AreEqual(summonBefore + 1, P.Skills.State.SummonCount, "x1 소환 = 굴림 1");
            Assert.Greater(P.Skills.State.Skills.Count, 0, "스킬을 하나도 못 얻었다");
            Sheet.Skills.QuickEquipButton.onClick.Invoke();
            yield return null;
            Assert.Greater(P.Skills.State.Equipped.Count, 0, "일괄 장착이 슬롯을 안 채웠다");

            // ── ⑥ 던전 — 목록 → 상세 → 입장 → 클리어 → 보상 수령
            log.Mark("던전");
            Trace("→ 던전");
            tb.CloseOpened();
            yield return null;
            SaveIo.State.BestChapter = 5;
            SaveIo.State.BestStage = 1;
            tb.OnTab("dungeon");
            yield return null;
            Assert.IsTrue(DungeonSheet.Instance != null && DungeonSheet.Instance.IsOpen, "던전 목록이 안 열렸다");
            Assert.IsTrue(D.Dungeons.Unlocked("hammer"), "5-1 도달이면 망치 도둑은 열려 있어야 한다");
            DungeonDetailPopup.Open("hammer");
            yield return null;
            Assert.IsTrue(DungeonDetailPopup.IsOpen, "던전 상세가 안 열렸다");
            DungeonDetailPopup.Enter();
            yield return null;
            Assert.IsTrue(D.Dungeons.InRun, "입장이 세이브에 dungeonRun 을 안 세웠다");
            D.Dungeons.OnClear();
            yield return null;
            Assert.IsTrue(DungeonClearPopup.IsOpen, "클리어 팝업이 안 떴다");
            DungeonClearPopup.Confirm();
            yield return null;
            Assert.IsFalse(D.Dungeons.InRun, "클리어 뒤에도 run 이 남았다");
            Assert.GreaterOrEqual((int)D.Dungeons.Best("hammer"), 1, "던전 최고 기록이 안 올랐다");

            // ── 마무리 — 전투 씬으로 돌아와 한 번 더 돌려도 성한가
            log.Mark("복귀 전투");
            Trace("→ 복귀 전투");
            tb.CloseOpened();
            yield return null;
            bs = BattleScene.Instance;
            Assert.IsNotNull(bs, "던전을 돌고 오니 전투 씬이 없다");
            bs.ManualStep = true;
            yield return Run(bs, 10);
            bs.ManualStep = false;

            Trace("done · 스폰 " + bs.SpawnCount + " · 처치 " + bs.KillCount + " · 펫 " + P.Pets.State.Pets.Count + " · 스킬 " + P.Skills.State.Skills.Count + " · 빨강 " + log.RedCount);
            log.Dispose();
            log.AssertNoRed();
            Debug.Log("[Playthrough] 전투 " + bs.SpawnCount + "스폰 · " + bs.KillCount + "처치 · 펫 " + P.Pets.State.Pets.Count
                      + " · 스킬 " + P.Skills.State.Skills.Count + " · 던전 최고 " + (int)D.Dungeons.Best("hammer"));
        }
    }
}
