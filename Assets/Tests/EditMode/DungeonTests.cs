using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Forge.Core;
using Forge.Core.Battle;
using Forge.Core.Data;
using Forge.Core.Dungeon;
using Forge.Core.Save;

namespace Forge.Tests
{
    /// <summary>
    /// T23 — Core `Dungeons` 가 원작 `web/js/dungeons.js` 와 같은가. 기대값은 `tools/dungeon_vectors.js`(정본을 node vm 에 그대로 올려 같은 시드·시각으로 돌린 것)가
    /// `Assets/Tests/EditMode/Vectors/t23-dungeons.json` 에 남긴다: 표 4행 · 상수 · 보상 수량·문구(배율 4벌) · 몬스터 HP · 해금 · 09:00 날짜 키 · 흐름 14개(연산마다 세이브 전체 + 토스트·퀘스트·UI 호출·저장 횟수).
    /// 다시 뽑기: `node tools/dungeon_vectors.js`(정본 체크아웃 `.wwwww-src` 필요) · `--check` 로 정본과 같은지만 본다.
    /// </summary>
    static class DungeonVectors
    {
        static JsonObject _doc;
        public static JsonObject Doc
        {
            get
            {
                if (_doc != null) return _doc;
                string dataDir = DataDir.Path; // <root>/Assets/StreamingAssets/data
                string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(dataDir)));
                string file = Path.Combine(root, "Assets", "Tests", "EditMode", "Vectors", "t23-dungeons.json");
                if (!File.Exists(file)) throw new FileNotFoundException("T23 벡터가 없다 — node tools/dungeon_vectors.js 로 뽑는다: " + file);
                _doc = MiniJson.ParseObject(File.ReadAllText(file));
                return _doc;
            }
        }

        /// <summary>벡터 생성기의 `canon` 과 같은 규약 — 키 정렬 · JS `JSON.stringify` 표기(수는 `String(number)` · 문자열은 비ASCII 그대로).</summary>
        public static string Canon(object v)
        {
            if (v == null) return "null";
            if (v is bool) return (bool)v ? "true" : "false";
            if (v is double) return JsNum.ToString((double)v);
            if (v is string) return Str((string)v);
            var a = v as List<object>;
            if (a != null)
            {
                var sb = new StringBuilder("[");
                for (int i = 0; i < a.Count; i++) { if (i > 0) sb.Append(','); sb.Append(Canon(a[i])); }
                return sb.Append(']').ToString();
            }
            var o = v as JsonObject;
            if (o != null)
            {
                var keys = new List<string>(o.Keys);
                keys.Sort(string.CompareOrdinal);
                var sb = new StringBuilder("{");
                for (int i = 0; i < keys.Count; i++) { if (i > 0) sb.Append(','); sb.Append(Str(keys[i])).Append(':').Append(Canon(o[keys[i]])); }
                return sb.Append('}').ToString();
            }
            throw new ArgumentException("canon: " + v.GetType());
        }
        static string Str(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4")); else sb.Append(c);
                        break;
                }
            }
            return sb.Append('"').ToString();
        }

        /// <summary>벡터 스텁의 `JSON.stringify(r)` — 원작 보상 객체는 던전마다 든 키만(삽입 순서).</summary>
        public static string RewardJson(DungeonRewards r)
        {
            if (r.Hammers != 0) return "{\"hammers\":" + JsNum.ToString(r.Hammers) + ",\"coins\":" + JsNum.ToString(r.Coins) + "}";
            if (r.Tickets != 0) return "{\"tickets\":" + JsNum.ToString(r.Tickets) + "}";
            if (r.EggCurrency != 0) return "{\"eggCurrency\":" + JsNum.ToString(r.EggCurrency) + "}";
            return "{\"potions\":" + JsNum.ToString(r.Potions) + "}";
        }
    }

    /// <summary>벡터 스텁과 같은 바깥(시각 · 최고 기록 · 기술트리 % · 퀘스트 · 저장) + 이벤트 기록.</summary>
    sealed class DungeonTestHost : IDungeonHost
    {
        public double NowMs;
        public int Rank;
        public double ThiefHammer, ThiefCoin, DungeonTicket, DungeonPotion;
        public List<string> Quests = new List<string>();
        public int Saves;
        public List<string> Toasts = new List<string>();
        public List<string> Ui = new List<string>();
        public List<DungeonEvent> Events = new List<DungeonEvent>();
        public int Setup;

        public double Now() { return NowMs; }
        public int BestRank() { return Rank; }
        public double ThiefHammerMult() { return 1 + ThiefHammer / 100; }
        public double ThiefCoinMult() { return 1 + ThiefCoin / 100; }
        public double DungeonTicketMult() { return 1 + DungeonTicket / 100; }
        public double DungeonPotionMult() { return 1 + DungeonPotion / 100; }
        public void QuestBump(string key) { Quests.Add(key); }
        public void Save() { Saves++; }

        public void Reset() { Quests.Clear(); Saves = 0; Toasts.Clear(); Ui.Clear(); Events.Clear(); Setup = 0; }

        public void On(DungeonEvent e)
        {
            Events.Add(e);
            switch (e.Kind)
            {
                case DungeonEventKind.Toast: Toasts.Add(e.Text); break;
                case DungeonEventKind.OpenDungeons: Ui.Add("openDungeons"); break;
                case DungeonEventKind.RenderDungeonDetail: Ui.Add("renderDungeonDetail"); break;
                case DungeonEventKind.RewardBurst: Ui.Add("rewardBurst:" + DungeonVectors.RewardJson(e.Rewards)); break;
                case DungeonEventKind.RenderTopBar: Ui.Add("renderTopBar"); break;
                case DungeonEventKind.RenderPets: Ui.Add("renderPets"); break;
                case DungeonEventKind.ShowDungeonClear: Ui.Add("showDungeonClear:" + e.Id + ":" + e.Stage + ":" + DungeonVectors.RewardJson(e.Rewards)); break;
                case DungeonEventKind.SetupStage: Setup++; break;
            }
        }

        public void SetTech(JsonObject t)
        {
            ThiefHammer = J.Num(t["thiefHammer"]); ThiefCoin = J.Num(t["thiefCoin"]); DungeonTicket = J.Num(t["dungeonTicket"]); DungeonPotion = J.Num(t["dungeonPotion"]);
        }
        public void SetTech(List<object> t)
        {
            ThiefHammer = J.Num(t[0]); ThiefCoin = J.Num(t[1]); DungeonTicket = J.Num(t[2]); DungeonPotion = J.Num(t[3]);
        }
    }

    [TestFixture]
    public class DungeonTests
    {
        static JsonObject V { get { return DungeonVectors.Doc; } }
        static readonly string[] Ids = { "hammer", "ghost", "invasion", "zombie" };

        static Dungeons Make(DungeonTestHost h, SaveState s, uint seed)
        {
            var d = new Dungeons(s, h, Rng.Mulberry(seed), TimeZoneInfo.Utc);
            d.Emitted += h.On;
            return d;
        }
        static SaveState EmptyS()
        {
            var o = new JsonObject();
            o["hammers"] = 80.0; o["coins"] = 500.0; o["gems"] = 0.0; o["tickets"] = 40.0; o["potions"] = 0.0; o["eggCurrency"] = 0.0; o["dungeonRun"] = null;
            return new SaveState(o);
        }

        [Test]
        public void 표_4행_원작_그대로()
        {
            var defs = J.Arr(V["defs"]);
            Assert.AreEqual(4, defs.Count);
            Assert.AreEqual(4, DungeonDefs.All.Length);
            for (int i = 0; i < 4; i++)
            {
                JsonObject e = J.Obj(defs[i]); DungeonDef d = DungeonDefs.All[i];
                Assert.AreEqual(J.Str(e["id"]), d.Id); Assert.AreEqual(J.Str(e["name"]), d.Name); Assert.AreEqual(J.Str(e["kr"]), d.Kr);
                Assert.AreEqual(J.Str(e["icon"]), d.Icon); Assert.AreEqual(J.Str(e["unlock"]), d.Unlock); Assert.AreEqual(J.Str(e["reward"]), d.Reward);
                JsonObject th = J.Obj(e["theme"]);
                Assert.AreEqual(J.Int(th["sky"]), d.Theme.Sky, d.Id + " sky"); Assert.AreEqual(J.Int(th["fog"]), d.Theme.Fog, d.Id + " fog"); Assert.AreEqual(J.Int(th["ground"]), d.Theme.Ground, d.Id + " ground");
                Assert.AreEqual(J.Str(th["biome"]), d.Theme.Biome); Assert.AreEqual(J.Str(th["celestial"]), d.Theme.Celestial);
                Assert.AreSame(d, DungeonDefs.Find(d.Id));
            }
            Assert.IsNull(DungeonDefs.Find("nope"));
            Assert.AreEqual(2, DungeonDefs.Find("hammer").UnlockChapter); Assert.AreEqual(10, DungeonDefs.Find("hammer").UnlockStage);
            Assert.AreEqual(3, DungeonDefs.Find("invasion").UnlockChapter); Assert.AreEqual(1, DungeonDefs.Find("invasion").UnlockStage);
        }

        [Test]
        public void 상수_원작_그대로()
        {
            JsonObject c = J.Obj(V["consts"]);
            Assert.AreEqual(J.Int(c["MAX_KEYS"]), DungeonRules.MaxKeys);
            Assert.AreEqual(J.Int(c["MIN_WAVES"]), DungeonRules.MinWaves);
            Assert.AreEqual(J.Int(c["MAX_WAVES"]), DungeonRules.MaxWaves);
            Assert.AreEqual(J.Int(c["DEFAULT_WAVES"]), DungeonRules.DefaultWaves);
            Assert.AreEqual(J.Num(c["BASE_REWARD"]), DungeonRules.BaseReward);
            Assert.AreEqual(J.Num(c["PER_STAGE"]), DungeonRules.PerStage);
            Assert.AreEqual(BattleRules.DungeonDefaultWaves, DungeonRules.DefaultWaves);
        }

        [Test]
        public void 보상_수량_선형()
        {
            var d = Make(new DungeonTestHost(), EmptyS(), 1);
            foreach (object row in J.Arr(V["rewardAmount"]))
            {
                var a = J.Arr(row);
                Assert.AreEqual(J.Num(a[1]), d.RewardAmount(J.Int(a[0])), "stage " + J.Int(a[0]));
            }
        }

        [Test]
        public void 보상_종류_배율_문구_144()
        {
            var h = new DungeonTestHost();
            var d = Make(h, EmptyS(), 1);
            var rows = J.Arr(V["rewards"]);
            Assert.AreEqual(144, rows.Count);
            foreach (object row in rows)
            {
                JsonObject e = J.Obj(row);
                h.SetTech(J.Arr(e["tech"]));
                string id = J.Str(e["id"]); int st = J.Int(e["stage"]);
                JsonObject r = J.Obj(e["r"]);
                DungeonRewards got = d.Rewards(id, st);
                string tag = id + " " + st + " tech=" + DungeonVectors.Canon(e["tech"]);
                Assert.AreEqual(J.Num(r["hammers"]), got.Hammers, tag + " hammers");
                Assert.AreEqual(J.Num(r["coins"]), got.Coins, tag + " coins");
                Assert.AreEqual(J.Num(r["tickets"]), got.Tickets, tag + " tickets");
                Assert.AreEqual(J.Num(r["eggCurrency"]), got.EggCurrency, tag + " egg");
                Assert.AreEqual(J.Num(r["potions"]), got.Potions, tag + " potions");
                Assert.AreEqual(DungeonVectors.RewardJson(got), MiniJson.Serialize(r), tag + " 키 모양");
                Assert.AreEqual(J.Str(e["text"]), d.RewardText(id, st), tag + " text");
                Assert.AreEqual(J.Str(e["textSp"]), d.RewardText(id, st, " "), tag + " textSp");
            }
        }

        [Test]
        public void 몬스터_HP_곡선_48()
        {
            var d = Make(new DungeonTestHost(), EmptyS(), 1);
            var rows = J.Arr(V["monsterHp"]);
            Assert.AreEqual(48, rows.Count);
            foreach (object row in rows)
            {
                var a = J.Arr(row);
                double exp = J.Num(a[2]);
                Assert.AreEqual(exp, d.MonsterHp(J.Str(a[0]), J.Int(a[1])), Math.Abs(exp) * 1e-12, J.Str(a[0]) + " " + J.Int(a[1]));
            }
            Assert.AreEqual(BattleRules.MonsterHpBase, d.MonsterHp("hammer", 1) / Math.Pow(BattleRules.MonsterHpPerChapter, 1), 1e-9);
        }

        [Test]
        public void 해금은_최고_랭크로()
        {
            var h = new DungeonTestHost();
            var d = Make(h, EmptyS(), 1);
            foreach (object row in J.Arr(V["unlocked"]))
            {
                var a = J.Arr(row);
                h.Rank = J.Int(a[0]);
                JsonObject u = J.Obj(a[1]);
                foreach (string id in Ids) Assert.AreEqual(J.Bool(u[id]), d.Unlocked(id), "rank " + h.Rank + " " + id);
            }
            Assert.Throws<ArgumentException>(() => d.Unlocked("nope"));
        }

        [Test]
        public void 리셋_날짜_키_09시_기준()
        {
            foreach (object row in J.Arr(V["dateKey"]))
            {
                var a = J.Arr(row);
                Assert.AreEqual(J.Str(a[1]), Dungeons.ResetDateKey(J.Num(a[0]), TimeZoneInfo.Utc), "ms " + J.Num(a[0]));
            }
            // 인스턴스 갈래 = 호스트 시각 + 생성자 달력
            var h = new DungeonTestHost { NowMs = 1789207200000 };
            Assert.AreEqual("Sat Sep 12 2026", Make(h, EmptyS(), 1).ResetDateKey());
        }

        [Test]
        public void 흐름_14개_정본_실행과_상태_호출_일치()
        {
            var flows = J.Arr(V["flows"]);
            Assert.AreEqual(14, flows.Count);
            int clears = 0, ops = 0;
            foreach (object fo in flows)
            {
                JsonObject f = J.Obj(fo);
                string name = J.Str(f["name"]);
                var h = new DungeonTestHost { NowMs = J.Num(f["now"]), Rank = J.Int(f["bestRank"]) };
                h.SetTech(J.Obj(f["tech"]));
                var S = new SaveState(MiniJson.ParseObject(J.Str(f["S0"])));
                var d = Make(h, S, (uint)J.Int(f["seed"]));
                foreach (object so in J.Arr(f["steps"]))
                {
                    JsonObject step = J.Obj(so);
                    var op = J.Arr(step["op"]);
                    string kind = J.Str(op[0]);
                    string tag = name + " · " + DungeonVectors.Canon(op) + " #" + ops;
                    h.Reset();
                    object ret = null;
                    switch (kind)
                    {
                        case "ensure": d.Ensure(); break;
                        case "enter": ret = d.Enter(J.Str(op[1]), op.Count > 2 ? J.Int(op[2]) : 0); break;
                        case "sweep": ret = d.Sweep(J.Str(op[1])); break;
                        case "onClear": d.OnClear(); clears++; break;
                        case "onFail": d.OnFail(); break;
                        case "restoreRun": ret = d.RestoreRun(); break;
                        case "now": h.NowMs = J.Num(op[1]); break;
                        case "bestRank": h.Rank = J.Int(op[1]); break;
                        case "put": S[J.Str(op[1])] = JsonTree.Clone(op[2]); break;
                        default: Assert.Fail("op? " + kind); break;
                    }
                    ops++;
                    Assert.AreEqual(DungeonVectors.Canon(step["ret"]), DungeonVectors.Canon(ret), tag + " 반환");
                    Assert.AreEqual(J.Str(step["S"]), DungeonVectors.Canon(S.Root), tag + " 세이브");
                    JsonObject log = J.Obj(step["log"]);
                    Assert.AreEqual(DungeonVectors.Canon(log["toasts"]), DungeonVectors.Canon(new List<object>(h.Toasts.ToArray())), tag + " 토스트");
                    Assert.AreEqual(DungeonVectors.Canon(log["quests"]), DungeonVectors.Canon(new List<object>(h.Quests.ToArray())), tag + " 퀘스트");
                    Assert.AreEqual(DungeonVectors.Canon(log["ui"]), DungeonVectors.Canon(new List<object>(h.Ui.ToArray())), tag + " UI");
                    Assert.AreEqual(J.Int(log["saves"]), h.Saves, tag + " 저장 횟수");
                    Assert.AreEqual(J.Int(log["setup"]), h.Setup, tag + " setupStage 횟수");
                    Assert.AreEqual(J.Bool(log["heroHp"]), h.Setup > 0, tag + " 입장이 hp 를 채웠는가(스텁 규약)");
                }
            }
            Assert.GreaterOrEqual(clears, 10);
            Assert.GreaterOrEqual(ops, 80);
        }

        [Test]
        public void 진행_중_던전을_전투_입력으로()
        {
            var h = new DungeonTestHost { NowMs = 1789207200000, Rank = 2510 };
            var S = EmptyS();
            var d = Make(h, S, 3);
            Assert.IsNull(d.BattleRun());
            Assert.IsFalse(d.InRun);
            d.Ensure();
            Assert.Throws<ArgumentException>(() => d.Enter("nope"));
            J.Obj(d.Slot["best"])["invasion"] = 2.0;
            Assert.IsTrue(d.Enter("invasion", 3));
            DungeonRun r = d.BattleRun();
            Assert.AreEqual("invasion", r.Id); Assert.AreEqual(3, r.Stage); Assert.That(r.Waves, Is.InRange(1, 3));
            Assert.AreEqual(d.MonsterHp("invasion", 3), r.MonsterHp);
            Assert.AreEqual("dungeon_invasion", r.Theme);
            Assert.IsTrue(d.InRun);
        }

        [Test]
        public void 원작_없는_id_와_빈_run_은_예외()
        {
            var h = new DungeonTestHost { NowMs = 1789207200000, Rank = 2510 };
            var d = Make(h, EmptyS(), 1);
            Assert.Throws<InvalidOperationException>(() => d.OnClear());
            Assert.Throws<InvalidOperationException>(() => d.OnFail());
            Assert.Throws<ArgumentException>(() => d.MonsterHp("nope", 1));
            Assert.Throws<ArgumentException>(() => d.Sweep("nope"));
            Assert.Throws<ArgumentNullException>(() => new Dungeons(null, h, Rng.Mulberry(1)));
            Assert.Throws<ArgumentNullException>(() => new Dungeons(EmptyS(), null, Rng.Mulberry(1)));
            Assert.Throws<ArgumentNullException>(() => new Dungeons(EmptyS(), h, null));
        }

        [Test]
        public void JS_수_변환()
        {
            Assert.AreEqual(0, Dungeons.Num(null)); Assert.AreEqual(1, Dungeons.Num(true)); Assert.AreEqual(0, Dungeons.Num(false));
            Assert.AreEqual(2.5, Dungeons.Num(2.5)); Assert.AreEqual(3, Dungeons.Num("3")); Assert.AreEqual(0, Dungeons.Num("  "));
            Assert.IsTrue(double.IsNaN(Dungeons.Num("x"))); Assert.IsTrue(double.IsNaN(Dungeons.Num(new JsonObject()))); Assert.IsTrue(double.IsNaN(Dungeons.Num(new List<object>())));
        }
    }

    /// <summary>T7 `Battle` 과 한 판 — 입장이 `ctx.Dungeon`·hp·`SetupStage` 를 밀고, 클리어·실패가 `OnClear`/`OnFail` 로 돌아온다(시뮬레이터 던전 시나리오 `dungeon_tier` 의 영웅으로).</summary>
    [TestFixture]
    public class DungeonBattleTests
    {
        [Test]
        public void 입장_클리어_실패_한_바퀴()
        {
            JsonObject exp = SimExpected.Load("dungeon_tier");
            JsonObject sc = J.Obj(exp["scenario"]);
            GameData g = DataDir.Game;
            BattleContext ctx = SimScenario.Context(sc, g);
            ctx.Dungeon = null;
            var battle = new Battle(ctx, Rng.Mulberry((uint)J.Int(sc["seed"])));
            battle.Start(0);

            var h = new DungeonTestHost { NowMs = 1789207200000, Rank = 2510 };
            var S = new SaveState(SimExpected.Save.DefaultState(0));
            var d = new Dungeons(S, h, Rng.Mulberry(9), TimeZoneInfo.Utc);
            d.Emitted += h.On;
            d.Attach(battle);
            Assert.AreSame(battle, d.AttachedBattle);
            d.Ensure();
            J.Obj(d.Slot["best"])["invasion"] = 2.0;
            double egg0 = S.EggCurrency;

            // 입장 → ctx.Dungeon · hp 만땅 · SetupStage(1웨이브)
            battle.Hero.Hp = Big.One;
            Assert.IsTrue(d.Enter("invasion", 3));
            Assert.IsNotNull(ctx.Dungeon);
            Assert.AreEqual("invasion", ctx.Dungeon.Id); Assert.AreEqual(3, ctx.Dungeon.Stage);
            // 시뮬레이터 시나리오는 해금 챕터를 6 으로 «가정» 했지만(시나리오 입력) 원작 표의 침략 해금은 3-1 — 전투 입력은 표에서 온다
            Assert.AreEqual(55 * Math.Pow(5.6, 2) * Math.Pow(1.35, 2), ctx.Dungeon.MonsterHp, 1e-9);
            Assert.AreEqual(d.MonsterHp("invasion", 3), ctx.Dungeon.MonsterHp);
            Assert.AreEqual(J.Str(J.Obj(sc["dungeon"])["theme"]), ctx.Dungeon.Theme);
            Assert.AreEqual(0, battle.Hero.Hp.CompareTo(battle.Hero.MaxHp));
            Assert.AreEqual(ctx.Dungeon.Waves, battle.TotalWaves());
            Assert.AreEqual(1, h.Setup);
            Assert.AreEqual(1, battle.Wave);

            // 클리어까지 틱 → Battle 이 ctx.Dungeon 을 비우고 OnClear → 열쇠 −1 · 최고 3 · 알 화폐 +104 · 팝업
            int t = 0;
            while (battle.Phase != BattlePhase.DungeonClear && t < 40000) { t++; battle.Tick(BattleRules.Tick, t * 100); }
            Assert.AreEqual(BattlePhase.DungeonClear, battle.Phase, "던전 클리어 페이즈 도달(틱 " + t + ")");
            Assert.IsNull(ctx.Dungeon);
            Assert.IsFalse(d.InRun);
            Assert.AreEqual(1, d.Keys("invasion")); Assert.AreEqual(3, d.Best("invasion"));
            Assert.AreEqual(egg0 + 104, S.EggCurrency);
            Assert.AreEqual(new[] { "dungeonClear", "keySpend" }, h.Quests.ToArray());
            DungeonEvent popup = h.Events.Find(e => e.Kind == DungeonEventKind.ShowDungeonClear);
            Assert.IsNotNull(popup); Assert.AreEqual("invasion", popup.Id); Assert.AreEqual(3, popup.Stage); Assert.AreEqual(104, popup.Rewards.EggCurrency);
            Assert.IsTrue(h.Ui.Contains("renderPets"));
            battle.FinishDungeonClear();
            Assert.AreEqual(BattlePhase.StageDelay, battle.Phase);

            // 다시 입장(남은 열쇠 1) → 영웅 사망 → OnFail: 열쇠 그대로 · run 비움 · 토스트
            h.Reset();
            Assert.IsTrue(d.Enter("invasion"));
            Assert.AreEqual(4, ctx.Dungeon.Stage, "stage 생략 = best + 1");
            for (int i = 0; i < 200 && battle.Phase != BattlePhase.Fight; i++) { t++; battle.Tick(BattleRules.Tick, t * 100); }
            Assert.AreEqual(BattlePhase.Fight, battle.Phase);
            Assert.AreEqual(new[] { "🥚 침략 4단계 입장!" }, h.Toasts.ToArray());
            Assert.AreEqual(1, h.Saves);
            h.Reset();
            for (int i = 0; i < 200 && ctx.Dungeon != null; i++) battle.DamageHero(battle.Hero.MaxHp.Mul(10));
            Assert.IsNull(ctx.Dungeon, "치명 피해 뒤 Battle.OnDefeat → OnDungeonFail");
            Assert.IsFalse(d.InRun);
            Assert.AreEqual(1, d.Keys("invasion")); Assert.AreEqual(3, d.Best("invasion"));
            Assert.AreEqual(new[] { "💀 침략 실패... 본대로 복귀합니다" }, h.Toasts.ToArray());
            Assert.AreEqual(1, h.Saves);
            Assert.AreEqual(0, h.Quests.Count);
        }
    }
}
