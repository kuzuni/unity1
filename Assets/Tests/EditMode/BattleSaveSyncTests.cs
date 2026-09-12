using System.IO;
using NUnit.Framework;
using Forge.Core;
using Forge.Core.Battle;
using Forge.Core.Data;
using Forge.Core.Save;

namespace Forge.Tests.EditMode
{
    /// <summary>
    /// T55 — 전투 문맥 ↔ 세이브 동기화 규칙(<see cref="BattleSaveSync"/>): 원작 `combat.js` 가 `S` 에 직접 쓰던 칸(처치·재화·첫 클리어·진행·무기 종)이 delta·우선순위·합집합 규칙으로 맞는가.
    /// 세이브는 `state.json` 의 `defaultState()`(T13) · 문맥은 gamedata.json 정의 위에.
    /// </summary>
    public class BattleSaveSyncTests
    {
        static SaveDefs Defs { get { return SaveDefs.Parse(File.ReadAllText(Path.Combine(DataDir.Path, SaveDefs.FileName))); } }

        static SaveState NewSave() { return new SaveState(Defs.DefaultState(1000)); }
        static BattleContext NewCtx() { return new BattleContext(DataDir.Game.Defs, Defs) { WeaponType = "sword" }; }

        [Test]
        public void Fill은_세이브_값을_문맥에_싣는다()
        {
            SaveState s = NewSave();
            s.Kills = 7; s.Coins = 1234; s.Hammers = 9; s.Chapter = 3; s.Stage = 4; s.Difficulty = 1; s.BestChapter = 5; s.BestStage = 2; s.BestDifficulty = 1;
            s.ClearedBosses["1-1"] = true; s.ClearedBosses["1-2"] = false;
            BattleContext c = NewCtx();
            var sync = new BattleSaveSync(c, s);
            Assert.IsFalse(sync.Filled);
            sync.Fill();
            Assert.IsTrue(sync.Filled);
            Assert.AreEqual(7, c.Kills); Assert.AreEqual(1234, c.Coins); Assert.AreEqual(9, c.Hammers);
            Assert.AreEqual(3, c.Progress.Chapter); Assert.AreEqual(4, c.Progress.Stage); Assert.AreEqual(1, c.Progress.Difficulty);
            Assert.AreEqual(5, c.Progress.BestChapter); Assert.AreEqual(2, c.Progress.BestStage); Assert.AreEqual(1, c.Progress.BestDifficulty);
            Assert.IsTrue(c.ClearedBosses.Contains("1-1")); Assert.IsFalse(c.ClearedBosses.Contains("1-2"), "false 는 클리어가 아니다(원작 `!S.clearedBosses[key]`)");
            Assert.AreEqual("sword", c.WeaponType, "무기 없음 → sword(원작 322행)");
            Assert.AreEqual(7, s.Kills, "Fill 은 세이브를 안 바꾼다"); Assert.AreEqual(1234, s.Coins);
        }

        [Test]
        public void 처치_재화는_delta_로_세이브에_더하고_UI_지출은_살아남는다()
        {
            SaveState s = NewSave();
            s.Coins = 500; s.Hammers = 80; s.Kills = 0;
            BattleContext c = NewCtx();
            var sync = new BattleSaveSync(c, s);
            sync.Fill();
            // 전투가 한 프레임에 올린 것
            c.Kills += 2; c.Coins += 30; c.Hammers += 1;
            // 그 사이 UI(대장간)가 코인을 썼다
            s.Coins -= 200;
            sync.Sync();
            Assert.AreEqual(2, s.Kills); Assert.AreEqual(500 - 200 + 30, s.Coins, "지출과 드랍이 둘 다 산다"); Assert.AreEqual(81, s.Hammers);
            Assert.AreEqual(s.Coins, c.Coins, "문맥은 세이브의 거울"); Assert.AreEqual(2, c.Kills); Assert.AreEqual(81, c.Hammers);
            sync.Sync();
            Assert.AreEqual(2, s.Kills, "같은 프레임에 두 번 불려도 delta 0"); Assert.AreEqual(330, s.Coins);
            // 세이브만 바뀐 경우(오프라인 보상·리셋) → 문맥이 따라온다
            s.Coins = 10; s.Kills = 100;
            sync.Sync();
            Assert.AreEqual(10, c.Coins); Assert.AreEqual(100, c.Kills);
        }

        [Test]
        public void 세이브가_늦게_서도_그때까지_오른_처치는_delta_로_산다()
        {
            BattleContext c = NewCtx();
            c.Kills = 3; c.Coins = 45;   // 세이브 없이 돌던 전투
            SaveState s = NewSave(); s.Kills = 10; s.Coins = 500;
            var sync = new BattleSaveSync(c, s);
            sync.Sync();
            Assert.AreEqual(13, s.Kills); Assert.AreEqual(545, s.Coins); Assert.AreEqual(13, c.Kills); Assert.AreEqual(545, c.Coins);
        }

        [Test]
        public void 진행은_문맥이_움직였으면_세이브로_아니면_세이브를_문맥으로()
        {
            SaveState s = NewSave();
            s.Chapter = 2; s.Stage = 9; s.BestChapter = 2; s.BestStage = 9;
            BattleContext c = NewCtx();
            var sync = new BattleSaveSync(c, s);
            sync.Fill();
            // 전투가 스테이지를 넘겼다(원작 stageClear: 2-9 → 2-10 · 최고 갱신)
            c.Progress.Stage = 10; c.Progress.RecordBest();
            sync.Sync();
            Assert.AreEqual(10, s.Stage); Assert.AreEqual(2, s.Chapter); Assert.AreEqual(10, s.BestStage);
            // 세이브만 바뀌었다(리셋·복원) → 문맥이 따라온다
            s.Chapter = 1; s.Stage = 1; s.Difficulty = 0;
            sync.Sync();
            Assert.AreEqual(1, c.Progress.Chapter); Assert.AreEqual(1, c.Progress.Stage); Assert.AreEqual(0, c.Progress.Difficulty);
            Assert.AreEqual(10, c.Progress.BestStage, "최고 기록은 안 건드렸으니 그대로");
            // 사망 후퇴(원작 onDefeat: stage--) 도 문맥 → 세이브
            c.Progress.Stage = 5; sync.Sync(); c.Progress.Stage = 4; sync.Sync();
            Assert.AreEqual(4, s.Stage);
        }

        [Test]
        public void 첫_클리어는_합집합_무기_종은_세이브가_원본()
        {
            SaveState s = NewSave();
            s.ClearedBosses["1-1"] = true;
            BattleContext c = NewCtx();
            var sync = new BattleSaveSync(c, s);
            sync.Fill();
            c.ClearedBosses.Add("1-2");
            s.ClearedBosses["d1:3-4"] = true;
            sync.Sync();
            Assert.IsTrue(JsonTree.Truthy(s.ClearedBosses["1-2"]), "문맥의 첫 클리어가 세이브로");
            Assert.IsTrue(c.ClearedBosses.Contains("d1:3-4"), "세이브의 키가 문맥으로");
            Assert.IsTrue(c.ClearedBosses.Contains("1-1"));
            // 무기 종
            var weapon = new JsonObject(); weapon["wtype"] = "bow"; weapon["slot"] = "weapon";
            s.Equipment["weapon"] = weapon;
            sync.Sync();
            Assert.AreEqual("bow", c.WeaponType);
            s.Equipment["weapon"] = null;
            sync.Sync();
            Assert.AreEqual("sword", c.WeaponType, "장착 해제 → sword");
            Assert.AreEqual("sword", BattleSaveSync.WeaponTypeOf(null));
        }

        [Test]
        public void clearedBosses_칸이_없으면_만들고_던전_잔존_판정()
        {
            SaveState s = NewSave();
            s["clearedBosses"] = null;
            BattleContext c = NewCtx();
            c.ClearedBosses.Add("1-1");
            var sync = new BattleSaveSync(c, s);
            sync.Sync();
            Assert.IsNotNull(s.ClearedBosses); Assert.IsTrue(JsonTree.Truthy(s.ClearedBosses["1-1"]));
            Assert.IsFalse(BattleSaveSync.DungeonStale(c, s), "판이 없으면 잔존 아님");
            c.Dungeon = new DungeonRun { Id = "hammer", Stage = 1 };
            Assert.IsTrue(BattleSaveSync.DungeonStale(c, s), "세이브에 run 이 없는데 문맥에 판이 남았다");
            var run = new JsonObject(); run["id"] = "hammer"; run["stage"] = 1.0; run["waves"] = 2.0;
            s["dungeonRun"] = run;
            Assert.IsFalse(BattleSaveSync.DungeonStale(c, s));
        }
        [Test]
        public void Load는_delta_없이_세이브를_그대로_싣는다_세이브_갈아타기()
        {
            SaveState a = NewSave(); a.Coins = 500; a.Kills = 3;
            BattleContext c = NewCtx();
            var syncA = new BattleSaveSync(c, a);
            syncA.Fill();
            c.Kills += 2; c.Coins += 30;
            syncA.Sync();
            Assert.AreEqual(5, a.Kills); Assert.AreEqual(530, a.Coins);
            // 씬 재로드 — 새 세이브 객체(처음부터 · 코인 500 · 처치 0). 문맥에 남은 530/5 를 delta 로 더하면 두 배가 된다 → Load 는 그대로 싣는다
            SaveState b = NewSave(); b.Coins = 500; b.Kills = 0; b.Stage = 4; b.ClearedBosses["1-1"] = true;
            c.ClearedBosses.Add("9-9");
            var syncB = new BattleSaveSync(c, b);
            syncB.Load();
            Assert.AreEqual(0, c.Kills); Assert.AreEqual(500, c.Coins); Assert.AreEqual(500, b.Coins, "새 세이브는 안 바뀐다");
            Assert.AreEqual(4, c.Progress.Stage);
            Assert.IsTrue(c.ClearedBosses.Contains("1-1")); Assert.IsFalse(c.ClearedBosses.Contains("9-9"), "옛 문맥의 첫 클리어는 버린다");
            Assert.IsTrue(syncB.Filled);
            // 그 뒤 delta 는 새 세이브로
            c.Kills += 1; syncB.Sync();
            Assert.AreEqual(1, b.Kills); Assert.AreEqual(5, a.Kills, "옛 세이브는 더 안 건드린다");
        }

        [Test]
        public void 던전_판_안에서는_스테이지_라벨이_던전_이름이다()
        {
            BattleContext c = NewCtx();
            c.HeroStats = () => new HeroStats { Atk = Big.Of(10), Hp = Big.Of(100), CritDmg = 1.5, AttacksPerSec = 1 };
            var b = new Battle(c, Rng.Mulberry(1));
            b.Start(0);
            string last = LastLabel(b);
            Assert.AreEqual(c.Progress.StageName(), last, "본대: 진행 좌표");
            b.Events.Clear();
            c.Dungeon = new DungeonRun { Id = "hammer", Stage = 2, Waves = 2, MonsterHp = 100, Theme = "dungeon_hammer", Label = "망치 도둑 2단계" };
            b.SetupStage();
            Assert.AreEqual("망치 도둑 2단계", LastLabel(b), "던전 판: 원작 updateStageLabel 의 던전 갈래");
            b.Events.Clear();
            c.Dungeon = null;
            b.LeaveDungeon();
            Assert.AreEqual(c.Progress.StageName(), LastLabel(b), "본대 복귀: 진행 좌표");
        }

        static string LastLabel(Battle b)
        {
            string tag = null;
            for (int i = 0; i < b.Events.Count; i++) if (b.Events[i].Kind == BattleEventKind.StageLabel) tag = b.Events[i].Tag;
            return tag;
        }
    }
}
