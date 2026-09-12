using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Battle;
using Forge.Core.Data;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Battle;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T55 — 부팅 전투가 세이브와 이어졌는가: 한 판을 돌리면 `S.kills`·`S.coins`·`S.hammers` 가 오르고(문맥은 거울), 스테이지 클리어가 `S.clearedBosses`·`S.stage` 에 남고,
    /// 장착 무기를 바꾸면 문맥의 무기 종이 따라오며, 던전 모듈이 이 전투에 꽂혀 있다(입장 → 문맥의 판 · 클리어 팝업 [보상 수령] → 본대 복귀). 콘솔 빨강은 러너가 실패시킨다.
    /// </summary>
    public class BattleSaveGlueTests
    {
        static IEnumerator Boot()
        {
            BattleScene.AutoBoot = true;
            if (BattleScene.Instance != null) Object.Destroy(BattleScene.Instance.gameObject);
            BattleSaveGlue.Uninstall();
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t0 = Time.realtimeSinceStartup;
            while ((BattleScene.Instance == null || !BattleScene.Instance.Ready || !SaveIo.Ready || !DungeonUiHost.Ready) && Time.realtimeSinceStartup - t0 < 30f) yield return null;
            Assert.IsTrue(BattleScene.Instance != null && BattleScene.Instance.Ready, "전투 씬이 30초 안에 서지 않았다");
            Assert.IsTrue(SaveIo.Ready && SaveIo.State != null, "세이브가 30초 안에 서지 않았다");
            Assert.IsTrue(DungeonUiHost.Ready, "던전 호스트가 30초 안에 서지 않았다");
        }

        static void Run(BattleScene s, double seconds)
        {
            for (double t = 0; t < seconds; t += 0.05) s.Step(0.05f);
        }

        [UnityTest]
        public IEnumerator 한_판이_처치_재화_첫_클리어_진행을_세이브에_남기고_무기_종과_던전이_이어진다()
        {
            yield return Boot();
            BattleScene bs = BattleScene.Instance;
            SaveState S = SaveIo.State;
            BattleContext ctx = bs.Battle.Context;
            Assert.IsTrue(BattleSaveGlue.Live, "문맥이 세이브와 이어져 있지 않다");
            Assert.AreEqual(S.Kills, ctx.Kills, "부팅 직후 문맥 = 세이브"); Assert.AreEqual(S.Coins, ctx.Coins); Assert.AreEqual(S.Hammers, ctx.Hammers);
            Assert.AreEqual(S.Chapter, ctx.Progress.Chapter); Assert.AreEqual(S.Stage, ctx.Progress.Stage);
            Assert.AreEqual(BattleSaveSync.WeaponTypeOf(S), ctx.WeaponType);
            // 던전 모듈이 이 전투에 꽂혀 있다(입장·복원이 문맥의 판을 민다 · onClear/onFail)
            Assert.AreSame(bs.Battle, DungeonUiHost.Instance.Dungeons.AttachedBattle, "Dungeons.Attach(battle) 이 안 됐다");
            Assert.IsNotNull(ctx.OnDungeonClear); Assert.IsNotNull(ctx.OnDungeonFail); Assert.IsNotNull(ctx.Save, "saveGame 훅");

            // 한 판: 처치가 나올 때까지 시뮬 시간을 민다(1-1 · 60초 안에 반드시 한 마리는 잡힌다 — T27 실측)
            bs.ManualStep = true;
            double kills0 = S.Kills, coins0 = S.Coins;
            for (int i = 0; i < 90 && ctx.Kills <= kills0; i++) { Run(bs, 1.0); yield return null; }
            Assert.Greater(ctx.Kills, kills0, "90초를 돌려도 처치가 없다");
            Assert.AreEqual(ctx.Kills, S.Kills, "처치가 세이브에 안 적혔다(원작 combat.js `S.kills++`)");
            Assert.Greater(S.Coins, coins0, "처치 코인이 세이브에 안 적혔다"); Assert.AreEqual(ctx.Coins, S.Coins, "문맥은 세이브의 거울");
            Assert.AreEqual(ctx.Hammers, S.Hammers);

            // UI 가 그 사이 코인을 쓰면 다음 프레임에 문맥이 따라온다(지출이 되살아나지 않는다)
            double before = S.Coins;
            S.Coins = before - 100;
            Run(bs, 0.05);
            Assert.AreEqual(S.Coins, ctx.Coins, "문맥이 지출을 따라온다");
            Assert.Less(S.Coins, before - 50, "지출 100 이 되살아나지 않는다(0.05초 드랍은 몇 코인 뿐)");

            // 스테이지 클리어(원작 stageClear): 첫 클리어 키 · 진행 전진 · 최고 기록 · saveGame
            string key = ctx.Progress.StageKey();
            int stage0 = S.Stage;
            double coinsBeforeClear = S.Coins;
            bs.Battle.StageClear();
            BattleSaveGlue.Sync();
            Assert.IsTrue(JsonTree.Truthy(S.ClearedBosses[key]), "첫 클리어 키가 세이브에 안 남았다");
            Assert.AreEqual(stage0 + 1, S.Stage, "스테이지가 세이브에서 안 올랐다"); Assert.AreEqual(ctx.Progress.Stage, S.Stage);
            Assert.GreaterOrEqual(S.BestStage, S.Stage); Assert.Greater(S.Coins, coinsBeforeClear, "첫 클리어 보너스 코인");

            // 장착 무기를 바꾸면 문맥의 무기 종이 따라온다(원작 322행 `S.equipment.weapon.wtype`)
            var weapon = new JsonObject(); weapon["wtype"] = "bow"; weapon["slot"] = "weapon"; weapon["age"] = "stone"; weapon["level"] = 1.0;
            S.Equipment["weapon"] = weapon;
            Run(bs, 0.05);
            Assert.AreEqual("bow", ctx.WeaponType, "무기 종이 세이브를 안 따라온다");
            S.Equipment["weapon"] = null;
            Run(bs, 0.05);
            Assert.AreEqual("sword", ctx.WeaponType, "무기 해제 → sword");

            // 던전: 세이브에서 판이 닫히면(전투 밖 onClear) 문맥의 판도 걷힌다(본대 라벨·테마 복귀)
            ctx.Dungeon = new DungeonRun { Id = "hammer", Stage = 1, Waves = 2, MonsterHp = 10, Theme = "dungeon_hammer" };
            Run(bs, 0.05);
            Assert.IsNull(ctx.Dungeon, "세이브에 dungeonRun 이 없는데 문맥의 판이 남았다");
            bs.ManualStep = false;
        }
    }
}
