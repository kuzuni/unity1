using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Forge.Core;
using Forge.Core.Battle;
using Forge.Core.Data;
using Forge.Core.Save;

namespace Forge.Tests
{
    /// <summary>
    /// T7 — Core `Battle` 이 원작 `combat.js` 와 **판 단위로 같은가**. 기대값은 `tools/sim/sim_combat.js`(정본을 node vm 에 그대로 올려 같은 시드로 돌린 것)가
    /// `tools/sim/expected/combat_*.json` 에 남긴다. 여기서는 같은 입력·같은 시드·같은 틱 시각으로 `Battle` 을 돌려 이벤트 줄을 만들고
    /// ⓐ 앞 600틱 줄 전부 ⓑ 100틱 창 해시(전 구간) ⓒ 처치 시각·드랍(틱 단위 합) ⓓ 판 결과(클리어·사망·던전) ⓔ 끝 상태 를 대조한다.
    /// 다시 뽑기: `node tools/sim/sim_combat.js`(정본 체크아웃 `.wwwww-src` 필요).
    /// </summary>
    static class SimExpected
    {
        public static string Dir
        {
            get
            {
                string data = DataDir.Path; // <repo>/Assets/StreamingAssets/data
                string repo = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(data)));
                return Path.Combine(repo, "tools", "sim", "expected");
            }
        }
        static SaveDefs _save;
        public static SaveDefs Save { get { return _save ?? (_save = SaveDefs.Parse(File.ReadAllText(Path.Combine(DataDir.Path, SaveDefs.FileName)))); } }
        public static JsonObject Load(string name)
        {
            string p = Path.Combine(Dir, "combat_" + name + ".json");
            Assert.That(File.Exists(p), Is.True, "기대값이 없다: " + p + " — node tools/sim/sim_combat.js 로 뽑는다");
            return MiniJson.ParseObject(File.ReadAllText(p));
        }

        // 시뮬레이터와 같은 양자화 — Big 은 floor(m·1e6+0.5)e<e> · 작은 수는 floor(v·1e6+0.5) · 큰 수는 Big 으로.
        public static string Q(Big v) { return ((long)Math.Floor(v.M * 1e6 + 0.5)).ToString() + "e" + JsNum.ToString(v.E); }
        public static string Q(double v)
        {
            if (Math.Abs(v) < 1e9) return ((long)Math.Floor(v * 1e6 + 0.5)).ToString();
            return Q(Big.Of(v));
        }
        static readonly HashSet<string> HashTagless = new HashSet<string> { "float", "loot", "toast" };
        public static string Line(BattleEvent e)
        {
            return e.Tick + "|" + e.Kind + "|" + e.Id + "|" + (e.Value.HasValue ? Q(e.Value.Value) : "") + "|" + Q(e.Num) + "|" + (e.Flag ? 1 : 0) + "|" + e.Tag;
        }
        public static string HashLine(BattleEvent e)
        {
            return e.Tick + "|" + e.Kind + "|" + e.Id + "|" + (e.Value.HasValue ? Q(e.Value.Value) : "") + "|" + Q(e.Num) + "|" + (e.Flag ? 1 : 0) + "|" + (HashTagless.Contains(e.Kind) ? "" : e.Tag);
        }
        public static uint Fnv1a(string s, uint h)
        {
            byte[] b = Encoding.UTF8.GetBytes(s);
            unchecked { for (int i = 0; i < b.Length; i++) { h ^= b[i]; h *= 0x01000193; } }
            return h;
        }
    }

    /// <summary>시뮬레이터 JSON 의 `scenario` 를 `BattleContext` 로 — 값은 전부 JSON 에서(공식 중복 없음).</summary>
    static class SimScenario
    {
        public static HeroStats Hero(JsonObject h)
        {
            return new HeroStats
            {
                Atk = Big.Of(J.Num(h["atk"])), Hp = Big.Of(J.Num(h["hp"])), CritCh = J.Num(h["critCh"]), CritDmg = J.Num(h["critDmg"]),
                AttacksPerSec = J.Num(h["attacksPerSec"]), DblAtk = J.Num(h["dblAtk"]), Block = J.Num(h["block"]), HpRegen = J.Num(h["hpRegen"]),
                Lifesteal = J.Num(h["lifesteal"]), MeleeDmg = J.Num(h["meleeDmg"]), RangedDmg = J.Num(h["rangedDmg"]), SkillDmg = J.Num(h["skillDmg"]), SkillCd = J.Num(h["skillCd"]),
            };
        }

        public static BattleContext Context(JsonObject sc, GameData g)
        {
            var c = new BattleContext(g.Defs, SimExpected.Save);
            JsonObject prog = J.Obj(sc["progress"]);
            c.Progress.Chapter = J.Int(prog["chapter"]); c.Progress.Stage = J.Int(prog["stage"]); c.Progress.Difficulty = J.Int(prog["difficulty"]);
            c.Progress.BestChapter = c.Progress.Chapter; c.Progress.BestStage = c.Progress.Stage; c.Progress.BestDifficulty = c.Progress.Difficulty;
            // 시뮬레이터는 원작 defaultState() 위에 시나리오를 덮는다 — 같은 기본값을 state.json 에서
            JsonObject def = SimExpected.Save.DefaultTemplate;
            c.Coins = J.Num(def["coins"]); c.Hammers = J.Num(def["hammers"]); c.Kills = J.Num(def["kills"]);
            c.WeaponType = J.Str(sc["weapon"]);
            c.AutoCast = true;
            foreach (object id in J.Arr(sc["skills"])) c.EquippedSkills.Add((string)id);
            JsonObject vals = J.Obj(sc["skillVals"]);
            var specs = new Dictionary<string, SkillSpec>();
            foreach (var kv in vals)
            {
                JsonObject v = J.Obj(kv.Value);
                specs[kv.Key] = SkillSpec.From(g.Defs.Skill(kv.Key), Big.Of(J.Num(v["dmg"])), Big.Of(J.Num(v["heal"])), Big.Of(J.Num(v["buff"])));
            }
            c.Skill = id => specs[id];
            double tech = J.Num(sc["techSkillDmgMult"], 1);
            c.SkillDmgMult = () => tech;
            HeroStats hero = Hero(J.Obj(sc["hero"]));
            c.HeroStats = () => hero;
            JsonObject d = J.Obj(sc["dungeon"]);
            if (d != null) c.Dungeon = new DungeonRun { Id = J.Str(d["id"]), Stage = J.Int(d["stage"]), Waves = J.Int(d["waves"]), MonsterHp = J.Num(d["monsterHp"]), Theme = J.Str(d["theme"]), Label = J.Str(d["label"]) };   // label: 정본 updateStageLabel 던전 갈래(T66) — 없으면 null → 진행 좌표
            return c;
        }
    }

    [TestFixture]
    public class BattleSimTests
    {
        static string PhaseName(BattlePhase p)
        {
            switch (p)
            {
                case BattlePhase.Idle: return "idle";
                case BattlePhase.Fight: return "fight";
                case BattlePhase.WaveDelay: return "waveDelay";
                case BattlePhase.StageDelay: return "stageDelay";
                case BattlePhase.BossWarn: return "bossWarn";
                default: return "dungeonClear";
            }
        }

        sealed class Run
        {
            public Battle B; public BattleContext C; public int Ticks, Rounds;
            public List<string> Prefix = new List<string>();
            public List<string> Windows = new List<string>();
            public Dictionary<int, List<string>> LinesByWindow = new Dictionary<int, List<string>>();
            public Dictionary<int, int[]> KillsByTick = new Dictionary<int, int[]>(); // tick → {count, bossCount}
            public Dictionary<int, double[]> DropsByTick = new Dictionary<int, double[]>(); // tick → {Δ코인, Δ해머}
            public List<string> Outcomes = new List<string>();
        }

        static Run Play(JsonObject exp)
        {
            JsonObject sc = J.Obj(exp["scenario"]);
            GameData g = DataDir.Game;
            var r = new Run();
            r.C = SimScenario.Context(sc, g);
            r.B = new Battle(r.C, Rng.Mulberry((uint)J.Int(sc["seed"])));
            int maxTicks = J.Int(sc["maxTicks"]), rounds = J.Int(sc["rounds"]), prefixTicks = J.Int(sc["prefixTicks"]), window = J.Int(sc["window"]), finishDelay = J.Int(sc["finishDelayTicks"]);
            int tick = 0, finishAt = -1, evCursor = 0;
            string keyBefore = r.C.Progress.StageKey();
            uint h = 0x811c9dc5; int wn = 0, wt0 = 0;
            Action<int> flushWindow = upto =>
            {
                while (wt0 + window <= upto) { r.Windows.Add(wt0 + ":" + wn + ":" + h.ToString("x8")); wt0 += window; wn = 0; h = 0x811c9dc5; }
            };
            Action collect = () =>
            {
                for (; evCursor < r.B.Events.Count; evCursor++)
                {
                    BattleEvent e = r.B.Events[evCursor];
                    flushWindow(e.Tick);
                    if (e.Tick <= prefixTicks) r.Prefix.Add(SimExpected.Line(e));
                    List<string> wl; if (!r.LinesByWindow.TryGetValue(wt0, out wl)) { wl = new List<string>(); r.LinesByWindow[wt0] = wl; }
                    wl.Add(SimExpected.Line(e));
                    h = SimExpected.Fnv1a(SimExpected.HashLine(e) + "\n", h); wn++;
                }
            };
            r.B.Start(0);
            collect();
            while (tick < maxTicks && r.Rounds < rounds)
            {
                tick++;
                double coins0 = r.C.Coins, hammers0 = r.C.Hammers;
                int ev0 = r.B.Events.Count;
                r.B.Tick(BattleRules.Tick, tick * 100);
                if (finishAt == tick) { r.B.FinishDungeonClear(); finishAt = -1; }
                if (r.B.Phase == BattlePhase.DungeonClear && finishAt < 0) finishAt = tick + finishDelay;
                // 이번 틱의 처치·판 결과
                int kills = 0, bosses = 0; bool down = false, dclear = false, save = false;
                for (int i = ev0; i < r.B.Events.Count; i++)
                {
                    BattleEvent e = r.B.Events[i];
                    if (e.Kind == BattleEventKind.Kill) { kills++; if (e.Flag) bosses++; }
                    else if (e.Kind == BattleEventKind.HeroDown) down = true;
                    else if (e.Kind == BattleEventKind.DungeonClear) dclear = true;
                    else if (e.Kind == BattleEventKind.Save) save = true;
                }
                if (kills > 0) r.KillsByTick[tick] = new[] { kills, bosses };
                if (r.C.Coins != coins0 || r.C.Hammers != hammers0) r.DropsByTick[tick] = new[] { r.C.Coins - coins0, r.C.Hammers - hammers0 };
                if (down || dclear || save)
                {
                    r.Rounds++;
                    r.Outcomes.Add(tick + "|" + (down ? "defeat" : dclear ? "dungeonClear" : "clear") + "|" + keyBefore + "|" + r.C.Progress.StageName());
                }
                keyBefore = r.C.Progress.StageKey();
                collect();
            }
            // 시뮬레이터는 t0 ≤ 마지막 틱인 창까지 전부 남긴다(마지막 창은 짧다)
            while (wt0 <= tick) { r.Windows.Add(wt0 + ":" + wn + ":" + h.ToString("x8")); wt0 += window; wn = 0; h = 0x811c9dc5; }
            r.Ticks = tick;
            return r;
        }

        static void AssertClose(double a, double b, string what)
        {
            double tol = Math.Max(1e-9 * Math.Abs(b), 1e-9);
            Assert.That(Math.Abs(a - b) <= tol, Is.True, what + ": " + a + " ≠ " + b);
        }

        static void Compare(string name)
        {
            JsonObject exp = SimExpected.Load(name);
            Run r = Play(exp);

            // ⓐ 앞 600틱 이벤트 줄 전부
            List<object> prefix = J.Arr(exp["prefix"]);
            int n = Math.Min(prefix.Count, r.Prefix.Count);
            for (int i = 0; i < n; i++)
            {
                if ((string)prefix[i] != r.Prefix[i])
                {
                    var sb = new StringBuilder();
                    sb.AppendLine(name + ": 앞구간 " + i + "번째 줄이 다르다");
                    for (int k = Math.Max(0, i - 6); k < Math.Min(n, i + 6); k++)
                        sb.AppendLine((k == i ? "→ " : "  ") + "원작 " + (string)prefix[k] + "\n" + (k == i ? "→ " : "  ") + "C#   " + r.Prefix[k]);
                    Assert.Fail(sb.ToString());
                }
            }
            Assert.That(r.Prefix.Count, Is.EqualTo(prefix.Count), name + ": 앞구간 줄 수");

            // ⓑ 100틱 창 해시 — 전 구간
            List<object> windows = J.Arr(exp["windows"]);
            Assert.That(r.Ticks, Is.EqualTo(J.Int(exp["ticks"])), name + ": 틱 수");
            Assert.That(r.Windows.Count, Is.EqualTo(windows.Count), name + ": 창 수");
            for (int i = 0; i < windows.Count; i++)
            {
                JsonObject w = J.Obj(windows[i]);
                string want = J.Int(w["t0"]) + ":" + J.Int(w["n"]) + ":" + J.Str(w["h"]);
                if (r.Windows[i] != want)
                {
                    List<string> wl; r.LinesByWindow.TryGetValue(J.Int(w["t0"]), out wl);
                    Assert.Fail(name + ": 창 " + i + "(틱 " + J.Int(w["t0"]) + "~) 이벤트가 다르다 — 원작 " + want + " · C# " + r.Windows[i]
                        + "\n원작 줄은 `node tools/sim/sim_combat.js --dump " + name + " " + J.Int(w["t0"]) + " " + (J.Int(w["t0"]) + J.Int(J.Obj(exp["scenario"])["window"])) + "`\nC# 줄:\n" + (wl == null ? "(없음)" : string.Join("\n", wl.ToArray())));
                }
            }

            // ⓒ 처치 시각(틱·수·보스) · 드랍(재화가 바뀐 틱마다 차분)
            var wantKills = new Dictionary<int, int[]>();
            foreach (object o in J.Arr(exp["kills"]))
            {
                JsonObject k = J.Obj(o);
                int t = J.Int(k["t"]);
                int[] acc; if (!wantKills.TryGetValue(t, out acc)) { acc = new int[2]; wantKills[t] = acc; }
                acc[0]++; if (J.Bool(k["boss"])) acc[1]++;
            }
            Assert.That(r.KillsByTick.Count, Is.EqualTo(wantKills.Count), name + ": 처치가 난 틱 수");
            foreach (var kv in wantKills)
            {
                int[] got; Assert.That(r.KillsByTick.TryGetValue(kv.Key, out got), Is.True, name + ": 틱 " + kv.Key + " 에 처치가 없다");
                Assert.That(got[0], Is.EqualTo(kv.Value[0]), name + ": 틱 " + kv.Key + " 처치 수");
                Assert.That(got[1], Is.EqualTo(kv.Value[1]), name + ": 틱 " + kv.Key + " 보스 처치 수");
            }
            List<object> drops = J.Arr(exp["drops"]);
            Assert.That(r.DropsByTick.Count, Is.EqualTo(drops.Count), name + ": 재화가 바뀐 틱 수");
            foreach (object o in drops)
            {
                double[] d = J.NumArr(o);
                double[] got; Assert.That(r.DropsByTick.TryGetValue((int)d[0], out got), Is.True, name + ": 틱 " + d[0] + " 에 드랍이 없다");
                AssertClose(got[0], d[1], name + ": 틱 " + d[0] + " 코인");
                Assert.That(got[1], Is.EqualTo(d[2]), name + ": 틱 " + d[0] + " 해머");
            }

            // ⓓ 판 결과
            List<object> outcomes = J.Arr(exp["outcomes"]);
            Assert.That(r.Outcomes.Count, Is.EqualTo(outcomes.Count), name + ": 판 수");
            for (int i = 0; i < outcomes.Count; i++)
            {
                JsonObject o = J.Obj(outcomes[i]);
                Assert.That(r.Outcomes[i], Is.EqualTo(J.Int(o["t"]) + "|" + J.Str(o["kind"]) + "|" + J.Str(o["key"]) + "|" + J.Str(o["label"])), name + ": 판 " + i);
            }
            Assert.That(r.Rounds, Is.EqualTo(J.Int(exp["rounds"])));

            // ⓔ 끝 상태
            JsonObject f = J.Obj(exp["final"]);
            Progress p = r.C.Progress;
            Assert.That(p.Chapter + "-" + p.Stage + " d" + p.Difficulty, Is.EqualTo(J.Int(f["chapter"]) + "-" + J.Int(f["stage"]) + " d" + J.Int(f["difficulty"])), name + ": 끝 진행");
            Assert.That(p.BestChapter + "-" + p.BestStage + " d" + p.BestDifficulty, Is.EqualTo(J.Int(f["bestChapter"]) + "-" + J.Int(f["bestStage"]) + " d" + J.Int(f["bestDifficulty"])), name + ": 최고 기록");
            Assert.That(r.C.Kills, Is.EqualTo(J.Num(f["kills"])), name + ": 처치 수");
            AssertClose(r.C.Coins, J.Num(f["coins"]), name + ": 코인");
            Assert.That(r.C.Hammers, Is.EqualTo(J.Num(f["hammers"])), name + ": 해머");
            Assert.That(SimExpected.Q(r.B.Hero.Hp), Is.EqualTo(J.Str(f["heroHp"])), name + ": 영웅 hp");
            Assert.That(SimExpected.Q(r.B.Hero.MaxHp), Is.EqualTo(J.Str(f["heroMaxHp"])), name + ": 영웅 maxHp");
            Assert.That(PhaseName(r.B.Phase), Is.EqualTo(J.Str(f["phase"])), name + ": 페이즈");
            Assert.That(r.B.Wave, Is.EqualTo(J.Int(f["wave"])), name + ": 웨이브");
            Assert.That(SimExpected.Q(r.B.CombatPower()), Is.EqualTo(J.Str(f["combatPower"])), name + ": 전투력");
            Assert.That(r.C.ClearedBosses.Count, Is.EqualTo(J.Int(f["clearedBosses"])), name + ": 첫 클리어 수");
        }

        [Test] public void 원작_대조_근접_1장() { Compare("melee_ch1"); }
        [Test] public void 원작_대조_원거리_3장() { Compare("ranged_ch3"); }
        [Test] public void 원작_대조_던전_티어상승() { Compare("dungeon_tier"); }

        [Test]
        public void 같은_시드는_같은_판()
        {
            JsonObject exp = SimExpected.Load("melee_ch1");
            Run a = Play(exp), b = Play(exp);
            Assert.That(a.Windows, Is.EqualTo(b.Windows));
            Assert.That(a.Outcomes, Is.EqualTo(b.Outcomes));
        }
    }

    [TestFixture]
    public class BattleUnitTests
    {
        static BattleContext Ctx(int chapter = 1, int stage = 1, int diff = 0)
        {
            var c = new BattleContext(DataDir.Game.Defs, SimExpected.Save);
            c.Progress.Chapter = chapter; c.Progress.Stage = stage; c.Progress.Difficulty = diff;
            c.Progress.BestChapter = chapter; c.Progress.BestStage = stage; c.Progress.BestDifficulty = diff;
            var hero = new HeroStats { Atk = Big.Of(15), Hp = Big.Of(150), CritCh = 5, CritDmg = 100, AttacksPerSec = 1.1 };
            c.HeroStats = () => hero;
            return c;
        }

        [Test]
        public void 진행_절대챕터_랭크_키_이름()
        {
            var p = new Progress(SimExpected.Save);
            Assert.That(p.ChaptersPerCycle, Is.EqualTo(25));
            Assert.That(p.StagesPerChapter, Is.EqualTo(10));
            Assert.That(p.MaxDifficulty, Is.EqualTo(3));
            Assert.That(p.AbsChapter(0, 1), Is.EqualTo(1));
            Assert.That(p.AbsChapter(1, 1), Is.EqualTo(26));
            Assert.That(p.AbsChapter(3, 25), Is.EqualTo(100));
            Assert.That(p.Rank(1, 1, 1) > p.Rank(0, 25, 10), Is.True, "티어 1 의 1-1 이 티어 0 의 25-10 보다 크다");
            p.Chapter = 3; p.Stage = 5;
            Assert.That(p.StageKey(), Is.EqualTo("3-5"));
            Assert.That(p.StageName(), Is.EqualTo("어려움 3-5"));
            p.Difficulty = 1;
            Assert.That(p.StageKey(), Is.EqualTo("d1:3-5"));
            Assert.That(p.StageName(), Is.EqualTo("어려움 3-5"));
            p.Difficulty = 2; Assert.That(p.StageName(), Is.EqualTo("매우 어려움 3-5"));
            p.Difficulty = 3; Assert.That(p.StageName(), Is.EqualTo("헬 3-5"));
            p.Difficulty = 0; p.Chapter = 1; Assert.That(p.StageName(), Is.EqualTo("쉬움 1-5"));
            p.Chapter = 2; Assert.That(p.StageName(), Is.EqualTo("보통 2-5"));
            p.Chapter = 9; Assert.That(p.StageName(), Is.EqualTo("매우 어려움 9-5"));
        }

        [Test]
        public void 사이클_길이는_챕터_테마_수()
        {
            var c = new BattleContext(DataDir.Game.Defs, SimExpected.Save);
            Assert.That(c.Progress.ChaptersPerCycle, Is.EqualTo(DataDir.Game.Defs.ChapterThemes.Count), "state.json CHAPTERS_PER_CYCLE = CHAPTER_THEMES.length");
            Assert.That(c.Progress.ChaptersPerCycle, Is.EqualTo(25));
            Assert.Throws<ArgumentNullException>(() => new Progress((SaveDefs)null));
            Assert.Throws<ArgumentException>(() => new Progress(25, 10, 3, new[] { "", "어려움" }));
        }

        [Test]
        public void 몬스터_HP_곡선과_보스_완화()
        {
            var c = Ctx(1, 1);
            var b = new Battle(c, Rng.Mulberry(1));
            Assert.That(b.MonsterBaseHp().ToNumber(), Is.EqualTo(55).Within(1e-9));
            Assert.That(b.BossEase(), Is.EqualTo(0.35).Within(1e-12));
            c.Progress.Stage = 9;
            Assert.That(b.BossEase(), Is.EqualTo(1).Within(1e-12), "1-9 에서 1.0 복귀");
            Assert.That(b.MonsterBaseHp().ToNumber(), Is.EqualTo(55 * Math.Pow(1.19, 8)).Within(1e-6));
            c.Progress.Chapter = 2; c.Progress.Stage = 1;
            Assert.That(b.BossEase(), Is.EqualTo(1));
            Assert.That(b.MonsterBaseHp().ToNumber(), Is.EqualTo(55 * 5.6).Within(1e-9));
            c.Progress.Difficulty = 1; c.Progress.Chapter = 1;
            Assert.That(b.MonsterBaseHp().Log10(), Is.EqualTo(Math.Log10(55) + 25 * Math.Log10(5.6)).Within(1e-9), "어려움 1-1 = 절대 26챕터");
            c.Dungeon = new DungeonRun { Id = "x", Stage = 1, Waves = 2, MonsterHp = 1234 };
            Assert.That(b.MonsterBaseHp().ToNumber(), Is.EqualTo(1234));
            Assert.That(b.BossEase(), Is.EqualTo(1));
            Assert.That(b.TotalWaves(), Is.EqualTo(2));
            c.Dungeon.Waves = 0;
            Assert.That(b.TotalWaves(), Is.EqualTo(BattleRules.DungeonDefaultWaves));
            c.Dungeon = null;
            Assert.That(b.TotalWaves(), Is.EqualTo(5));
        }

        [Test]
        public void 시작하면_첫_웨이브_적_둘과_이벤트()
        {
            var c = Ctx();
            var b = new Battle(c, Rng.Mulberry(7));
            b.Start();
            Assert.That(b.Phase, Is.EqualTo(BattlePhase.Fight));
            Assert.That(b.Wave, Is.EqualTo(1));
            Assert.That(b.Enemies.Count, Is.EqualTo(2));
            Assert.That(b.Hero.Hp.ToNumber(), Is.EqualTo(150));
            Assert.That(b.Hero.MaxHp.ToNumber(), Is.EqualTo(150));
            foreach (Enemy e in b.Enemies)
            {
                Assert.That(e.Hp.ToNumber(), Is.EqualTo(55).Within(1e-9));
                Assert.That(e.Atk.ToNumber(), Is.EqualTo(55.0 / 14).Within(1e-9));
                Assert.That(e.X, Is.GreaterThanOrEqualTo(3.1).And.LessThan(3.1 + 1.2 + 0.4));
                Assert.That(e.Speed, Is.GreaterThanOrEqualTo(1.0).And.LessThanOrEqualTo(1.4));
                Assert.That(e.StopX.HasValue, Is.True, "대열이 짜였다");
            }
            Assert.That(b.Enemies[0].StopX, Is.EqualTo(BattleRules.MeleeX).Within(1e-12));
            Assert.That(b.Enemies[1].StopX, Is.EqualTo(BattleRules.MeleeX + 0.5 * 0.62 + 0.2 + 0.5 * 0.62).Within(1e-12), "둘째는 첫째 뒷면 + 반폭·PACK");
            Assert.That(b.Enemies[1].Z, Is.EqualTo(0.95));
            var kinds = new List<string>();
            foreach (BattleEvent e in b.Events) kinds.Add(e.Kind);
            Assert.That(kinds, Is.EqualTo(new[] { "heroRevive", "music", "clearEnemies", "theme", "stageLabel", "spawn", "spawn", "wavePips" }));
            Assert.That(b.Events[3].Tag, Is.EqualTo("ch:1"));
            Assert.That(b.Events[4].Tag, Is.EqualTo("쉬움 1-1"));
        }

        [Test]
        public void 행군_판정과_틱_카운트()
        {
            var c = Ctx();
            var b = new Battle(c, Rng.Mulberry(3));
            b.Start();
            b.Tick(0.1, 100);
            Assert.That(b.TickCount, Is.EqualTo(1));
            Assert.That(b.Walking, Is.False, "적이 살아 있는 fight 는 행군 안 함");
            foreach (Enemy e in b.Enemies) e.Alive = false;
            b.Tick(0.1, 200);
            Assert.That(b.Walking, Is.True, "적이 없으면 행군");
        }

        [Test]
        public void 보스_웨이브는_경고_뒤_스폰()
        {
            var c = Ctx(2, 3);
            var b = new Battle(c, Rng.Mulberry(11));
            b.Start();
            b.Wave = 4;
            b.Phase = BattlePhase.WaveDelay; b.PhaseTimer = 0.1;
            foreach (Enemy e in b.Enemies) e.Alive = false;
            b.Tick(0.1, 100);
            Assert.That(b.Phase, Is.EqualTo(BattlePhase.BossWarn));
            Assert.That(b.Wave, Is.EqualTo(5));
            Assert.That(b.PhaseTimer, Is.EqualTo(BattleRules.BossImpact));
            int n = b.Events.Count;
            int ticks = 0;
            while (b.Phase == BattlePhase.BossWarn && ticks < 100) { ticks++; b.Tick(0.1, 100 + ticks * 100); }
            Assert.That(b.Phase, Is.EqualTo(BattlePhase.Fight));
            Assert.That(ticks, Is.EqualTo(16), "1.55초 → 16틱째에 솟는다");
            Enemy boss = null;
            foreach (Enemy e in b.Enemies) if (e.Alive) boss = e;
            Assert.That(boss, Is.Not.Null);
            Assert.That(boss.IsBoss, Is.True);
            Assert.That(boss.X, Is.EqualTo(BattleRules.BossSpawnX));
            Assert.That(boss.Hp.ToNumber(), Is.EqualTo(55 * 5.6 * Math.Pow(1.19, 2) * (1 + 0.08 * 4) * 6).Within(1e-6));
            Assert.That(boss.Atk.ToNumber(), Is.EqualTo(boss.Hp.ToNumber() / 9).Within(1e-6));
        }

        [Test]
        public void 수동_시전은_사거리_밖이면_실패하고_쿨을_안_돈다()
        {
            var c = Ctx();
            c.EquippedSkills.Add("fireball");
            var def = DataDir.Game.Defs.Skill("fireball");
            c.Skill = id => SkillSpec.From(def, Big.Of(480), Big.Zero, Big.Zero);
            var b = new Battle(c, Rng.Mulberry(5));
            b.Start();
            b.Cooldowns["fireball"] = 0;
            Assert.That(b.TryCast("fireball", true), Is.False, "적이 아직 x≥3.2");
            Assert.That(b.Cooldowns["fireball"], Is.EqualTo(0));
            Assert.That(b.Events[b.Events.Count - 1].Kind, Is.EqualTo(BattleEventKind.Toast));
            foreach (Enemy e in b.Enemies) e.X = 1.0;
            Assert.That(b.TryCast("fireball", true), Is.True);
            Assert.That(b.Cooldowns["fireball"], Is.EqualTo(12).Within(1e-12), "cd 12 · skillCd 0");
            b.Tick(0.1, 100); b.Tick(0.1, 200); b.Tick(0.1, 300);
            int hits = 0;
            foreach (BattleEvent e in b.Events) if (e.Kind == BattleEventKind.Hit && e.Tag == "skill") { hits++; Assert.That(e.Num, Is.EqualTo(1), "480 은 55 를 처치"); }
            Assert.That(hits, Is.EqualTo(2), "광역이 둘 다 맞힌다");
            Assert.That(b.Phase, Is.EqualTo(BattlePhase.WaveDelay));
            Assert.That(c.Kills, Is.EqualTo(2));
        }

        [Test]
        public void 버프는_고정_가산이고_만료되면_빠진다()
        {
            var c = Ctx();
            c.EquippedSkills.Add("warCry");
            var def = DataDir.Game.Defs.Skill("warCry");
            c.Skill = id => SkillSpec.From(def, Big.Zero, Big.Zero, Big.Of(120));
            var b = new Battle(c, Rng.Mulberry(5));
            b.Start();
            b.Cooldowns["warCry"] = 0;
            Assert.That(b.TryCast("warCry", false), Is.True);
            Assert.That(b.Hero.Stats.Atk.ToNumber(), Is.EqualTo(135), "15 + 120");
            Assert.That(b.Buffs.Count, Is.EqualTo(1));
            b.Tick(0.1, 7900);
            Assert.That(b.Buffs.Count, Is.EqualTo(1), "until 8000 > 7900");
            b.Tick(0.1, 8000);
            Assert.That(b.Buffs.Count, Is.EqualTo(0));
            Assert.That(b.Hero.Stats.Atk.ToNumber(), Is.EqualTo(15));
        }

        [Test]
        public void 회복은_지속시간에_걸쳐_흐르고_만피를_넘지_않는다()
        {
            var c = Ctx();
            c.EquippedSkills.Add("blessing");
            var def = DataDir.Game.Defs.Skill("blessing");
            c.Skill = id => SkillSpec.From(def, Big.Zero, Big.Of(100), Big.Zero);
            var b = new Battle(c, Rng.Mulberry(5));
            b.Start();
            b.Hero.Hp = Big.Of(50);
            b.Cooldowns["blessing"] = 0;
            Assert.That(b.TryCast("blessing", false), Is.True);
            Assert.That(b.Hots.Count, Is.EqualTo(1));
            Assert.That(b.Hots[0].Per.ToNumber(), Is.EqualTo(20).Within(1e-9), "100 / dur 5");
            b.Tick(0.1, 100);
            // 회복 2 + 자연 회복 150·1%·0.1 = 0.15
            Assert.That(b.Hero.Hp.ToNumber(), Is.EqualTo(52.15).Within(1e-9));
            for (int i = 2; i <= 60; i++) b.Tick(0.1, i * 100);
            Assert.That(b.Hots.Count, Is.EqualTo(0), "5초면 만료");
            Assert.That(b.Hero.Hp.ToNumber(), Is.EqualTo(150).Within(1e-9), "만피 상한");
            int floats = 0;
            foreach (BattleEvent e in b.Events) if (e.Kind == BattleEventKind.Float) floats++;
            Assert.That(floats, Is.GreaterThan(0));
        }

        [Test]
        public void 사망은_후퇴하고_벽시계로_기상한다()
        {
            var c = Ctx(3, 4);
            var b = new Battle(c, Rng.Mulberry(9));
            b.Start();
            b.Tick(0.1, 100);
            b.DamageHero(Big.Of(1e9));
            Assert.That(b.Phase, Is.EqualTo(BattlePhase.StageDelay));
            Assert.That(c.Progress.Stage, Is.EqualTo(3), "3-4 → 3-3");
            Assert.That(c.Progress.BestStage, Is.EqualTo(4), "최고 기록은 안 깎인다");
            Assert.That(b.DownUntil, Is.EqualTo(100 + BattleRules.DeathDownMs));
            Assert.That(b.Hero.Hp, Is.EqualTo(b.Hero.MaxHp));
            Assert.That(b.Enemies.Count, Is.EqualTo(0));
            string fade = null;
            foreach (BattleEvent e in b.Events) if (e.Kind == BattleEventKind.DeathFade) fade = e.Tag;
            Assert.That(fade, Is.EqualTo("어려움 3-3 스테이지로 이동합니다"));
            // 누워 있는 동안(1600ms) 스테이지 타이머가 멈춘다
            for (int i = 2; i <= 16; i++) b.Tick(0.1, i * 100);
            Assert.That(b.Phase, Is.EqualTo(BattlePhase.StageDelay));
            Assert.That(b.DownUntil, Is.Not.EqualTo(0));
            b.Tick(0.1, 1700);
            Assert.That(b.DownUntil, Is.EqualTo(0));
            Assert.That(b.RiseUntil, Is.EqualTo(1700 + BattleRules.DeathRiseMs));
            for (int i = 18; i <= 26; i++) b.Tick(0.1, i * 100);
            Assert.That(b.RiseUntil, Is.EqualTo(0), "2550 지나면 일어선다");
            for (int i = 27; i <= 31; i++) b.Tick(0.1, i * 100);
            Assert.That(b.Phase, Is.EqualTo(BattlePhase.Fight), "행군 0.4초 뒤 새 스테이지");
            Assert.That(b.Wave, Is.EqualTo(1));
        }

        [Test]
        public void 첫_스테이지_사망은_후퇴_없음()
        {
            var c = Ctx(1, 1);
            var b = new Battle(c, Rng.Mulberry(9));
            b.Start();
            b.Tick(0.1, 100);
            b.DamageHero(Big.Of(1e9));
            Assert.That(c.Progress.Stage, Is.EqualTo(1));
            string fade = null;
            foreach (BattleEvent e in b.Events) if (e.Kind == BattleEventKind.DeathFade) fade = e.Tag;
            Assert.That(fade, Is.EqualTo("회복 후 다시 도전합니다"));
        }

        [Test]
        public void 스테이지_클리어_첫보상_전진_티어상승()
        {
            var c = Ctx(25, 10);
            var b = new Battle(c, Rng.Mulberry(2));
            b.Start();
            int saves = 0; c.Save = () => saves++;
            b.StageClear();
            Assert.That(c.ClearedBosses.Contains("25-10"), Is.True);
            Assert.That(c.Progress.Difficulty, Is.EqualTo(1));
            Assert.That(c.Progress.Chapter, Is.EqualTo(1));
            Assert.That(c.Progress.Stage, Is.EqualTo(1));
            Assert.That(c.Progress.BestDifficulty, Is.EqualTo(1));
            Assert.That(saves, Is.EqualTo(1));
            Assert.That(b.Phase, Is.EqualTo(BattlePhase.StageDelay));
            Assert.That(b.PhaseTimer, Is.EqualTo(BattleRules.StageDelay));
            int toasts = 0; foreach (BattleEvent e in b.Events) if (e.Kind == BattleEventKind.Toast) toasts++;
            Assert.That(toasts, Is.EqualTo(2), "첫 클리어 + 난이도 상승");
            Assert.That(c.Coins, Is.EqualTo(Math.Ceiling(60 * Math.Pow(1.6, 24) * Math.Pow(1.06, 9))));
            // 헬 25-10 은 종점 — 반복
            c.Progress.Difficulty = 3; c.Progress.Chapter = 25; c.Progress.Stage = 10;
            b.StageClear();
            Assert.That(c.Progress.StageKey(), Is.EqualTo("d3:25-10"));
        }

        [Test]
        public void 던전_클리어는_팝업을_기다리고_복귀한다()
        {
            var c = Ctx(4, 2);
            c.Dungeon = new DungeonRun { Id = "invasion", Stage = 1, Waves = 1, MonsterHp = 10, Theme = "dungeon_invasion" };
            int cleared = 0; c.OnDungeonClear = () => { cleared++; Assert.That(c.Dungeon, Is.Null, "run 은 먼저 비운다"); };
            var b = new Battle(c, Rng.Mulberry(4));
            b.Start();
            Assert.That(b.Phase, Is.EqualTo(BattlePhase.BossWarn), "1웨이브 = 보스 단판");
            Assert.That(b.Events[3].Tag, Is.EqualTo("dungeon_invasion"));
            for (int i = 1; i <= 16; i++) b.Tick(0.1, i * 100);
            Assert.That(b.Phase, Is.EqualTo(BattlePhase.Fight));
            Enemy boss = b.Enemies[0];
            Assert.That(boss.Hp.ToNumber(), Is.EqualTo(60).Within(1e-9), "10 × 6(완화 없음)");
            double coins0 = c.Coins;
            b.DamageEnemy(boss, Big.Of(1000), false, null);
            Assert.That(c.Coins, Is.EqualTo(coins0), "던전은 처치 보상 없음");
            Assert.That(cleared, Is.EqualTo(1));
            Assert.That(b.Phase, Is.EqualTo(BattlePhase.DungeonClear));
            for (int i = 17; i <= 20; i++) b.Tick(0.1, i * 100);
            Assert.That(b.Phase, Is.EqualTo(BattlePhase.DungeonClear), "팝업 전엔 조용히 대기");
            Assert.That(b.Walking, Is.False);
            b.FinishDungeonClear();
            Assert.That(b.Phase, Is.EqualTo(BattlePhase.StageDelay));
            Assert.That(b.PhaseTimer, Is.EqualTo(BattleRules.DungeonReturnDelay));
            var tail = new List<string>();
            for (int i = b.Events.Count - 6; i < b.Events.Count; i++) tail.Add(b.Events[i].Kind);
            Assert.That(tail, Is.EqualTo(new[] { "clearEnemies", "music", "sceneCut", "theme", "stageLabel", "wavePips" }));
            Assert.That(b.Events[b.Events.Count - 3].Tag, Is.EqualTo("ch:4"));
            for (int i = 21; i <= 30; i++) b.Tick(0.1, i * 100);
            Assert.That(b.Phase, Is.EqualTo(BattlePhase.Fight), "본대 스테이지 재시작");
            Assert.That(c.Progress.StageKey(), Is.EqualTo("4-2"));
        }

        [Test]
        public void 던전_사망은_본대_복귀()
        {
            var c = Ctx(4, 2);
            c.Dungeon = new DungeonRun { Id = "invasion", Stage = 1, Waves = 3, MonsterHp = 10, Theme = "t" };
            int failed = 0; c.OnDungeonFail = () => failed++;
            var b = new Battle(c, Rng.Mulberry(4));
            b.Start();
            b.Tick(0.1, 100);
            b.DamageHero(Big.Of(1e9));
            Assert.That(failed, Is.EqualTo(1));
            Assert.That(c.Dungeon, Is.Null);
            Assert.That(c.Progress.Stage, Is.EqualTo(2), "던전 실패는 후퇴 대상이 아니다");
            string fade = null; foreach (BattleEvent e in b.Events) if (e.Kind == BattleEventKind.DeathFade) fade = e.Tag;
            Assert.That(fade, Is.EqualTo("본대로 복귀합니다"));
        }

        [Test]
        public void 전투력_식()
        {
            var c = Ctx();
            var hero = new HeroStats { Atk = Big.Of(100), Hp = Big.Of(800), CritCh = 20, CritDmg = 150, AttacksPerSec = 2 };
            c.HeroStats = () => hero;
            var b = new Battle(c, Rng.Mulberry(1));
            Assert.That(b.CombatPower().IsZero, Is.True, "스탯 전이면 0");
            b.RecalcHero();
            Assert.That(b.CombatPower().ToNumber(), Is.EqualTo(100 * 2 * (1 + 0.2 * 1.5) + 100).Within(1e-9));
        }

        [Test]
        public void 반폭_훅이_대열_간격을_바꾼다()
        {
            var c = Ctx(2, 3);
            c.EnemyHalfW = id => 0.8;
            var b = new Battle(c, Rng.Mulberry(1));
            b.Start();
            Assert.That(b.Enemies[1].StopX, Is.EqualTo(BattleRules.MeleeX + 0.8 * 0.62 + 0.2 + 0.8 * 0.62).Within(1e-12));
        }

        [Test]
        public void 영웅_스탯이_없으면_만들_수_없다()
        {
            var c = new BattleContext(DataDir.Game.Defs, SimExpected.Save);
            Assert.Throws<ArgumentException>(() => new Battle(c, Rng.Mulberry(1)));
        }
    }
}
