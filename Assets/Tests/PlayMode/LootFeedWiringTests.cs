using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core;
using Forge.Core.Battle;
using Forge.Core.Data;
using Forge.Core.Save;
using Forge.Core.Hero;
using Forge.Game;
using Forge.Game.Battle;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T138 3회차 — 전투 씬이 Core 이벤트를 정본 자리로 보내는가: Loot → 전리품 레인(`combat.js` 450·454 `UI.floatLoot`) · Toast → 토스트 레인(`toast(msg, lane)` · 첫 클리어·난이도 상승 = combat).
    /// 레인·줄 상한·수명 자체는 1회차 `LootFeedTests` 가 지킨다 — 여기서는 «이벤트가 그 레인에 닿는가» 만 본다(주입 · `DamageVignetteTests` 와 같은 길).
    /// </summary>
    public class LootFeedWiringTests
    {
        static GameData _data; static SaveDefs _defs;
        static GameData Data { get { return _data ?? (_data = GameData.LoadDirectory(System.IO.Path.Combine(Application.streamingAssetsPath, "data"))); } }
        static SaveDefs Defs { get { return _defs ?? (_defs = SaveDefs.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(Application.streamingAssetsPath, "data", SaveDefs.FileName)))); } }
        static HeroStats StrongHero() { return new HeroStats { Atk = Big.Of(5000), Hp = Big.Of(100000), CritCh = 30, CritDmg = 100, AttacksPerSec = 1.1 }; }

        static IEnumerator Boot()
        {
            BattleScene.AutoBoot = false;
            if (BattleScene.Instance != null) Object.Destroy(BattleScene.Instance.gameObject);
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다");
            Assert.IsNotNull(PopupLayer.Instance, "팝업 층이 서지 않았다");
        }

        [UnityTest]
        public IEnumerator 전투_전리품은_레인에_쌓이고_전투_토스트는_이벤트의_레인대로_간다()
        {
            yield return Boot();
            var b = Object.FindAnyObjectByType<Bootstrap>();
            var s = BattleScene.Create(b != null ? b.transform : null, false);
            s.ManualStep = true;
            s.Attach(BattleScene.MakeBattle(Data, Defs, StrongHero, 9), Data, Defs);
            int before = LootFeed.Instance != null ? LootFeed.Instance.Lines : 0;
            // 정본 combat.js 450 — 처치 코인은 floatLoot(`🪙 +N`) · 454 보스 해머 floatLoot(`🔨 +N`)
            s.Battle.Events.Add(new BattleEvent { Kind = BattleEventKind.Loot, Tag = "🪙 +3" });
            s.Battle.Events.Add(new BattleEvent { Kind = BattleEventKind.Loot, Tag = "🔨 +8" });
            s.Step((float)BattleRules.Tick);   // 큐는 논리 틱이 지날 때만 비워진다(런 280 · DamageVignetteTests 와 같은 함정)
            Assert.IsNotNull(LootFeed.Instance, "전리품 레인이 서야 한다(정본 #loot-feed)");
            Assert.AreEqual(before + 2, LootFeed.Instance.Lines, "전리품 둘 = 레인 줄 둘(영웅 머리 위 숫자가 아니다)");

            // 정본 combat.js 484·499 — 전투 문구는 'combat' 레인(팝업 아래) · 373 «사거리 안에 적이 없습니다» 는 기본 레인
            PopupLayer pl = PopupLayer.Instance;
            s.Battle.Events.Add(new BattleEvent { Kind = BattleEventKind.Toast, Tag = "🏆 1-1 첫 클리어! 🪙+60", Lane = "combat" });
            s.Step((float)BattleRules.Tick);
            Assert.AreEqual("combat", pl.LastToastLane, "첫 클리어 토스트는 전투 레인");
            s.Battle.Events.Add(new BattleEvent { Kind = BattleEventKind.Toast, Tag = "사거리 안에 적이 없습니다" });
            s.Step((float)BattleRules.Tick);
            Assert.IsNull(pl.LastToastLane, "레인 없는 토스트는 기본 레인(팝업 위)");
            Object.Destroy(s.gameObject);
            yield return null;
        }
    }
}
