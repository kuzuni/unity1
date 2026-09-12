using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.Mounts;
using Forge.Core.Pets;

namespace Forge.Tests
{
    /// <summary>
    /// T40 — Core `MountSystem` ↔ 원작 `mounts.js` 대조. 기대값은 전부 `Vectors/mount_vectors.json`(`tools/mount_vectors.js` 가 정본을 mulberry32 시드로
    /// **실제로 돌려** 뽑은 것)에서 온다 — 여기에 수치를 손으로 적지 않는다. 같은 시드 → 같은 등급·같은 종·같은 옵션·같은 인덱스.
    /// </summary>
    static class MountVectors
    {
        static JsonObject _v;
        public static JsonObject V
        {
            get
            {
                if (_v != null) return _v;
                foreach (string start in new[] { Directory.GetCurrentDirectory(), AppDomain.CurrentDomain.BaseDirectory })
                {
                    string d = start;
                    for (int i = 0; i < 10 && !string.IsNullOrEmpty(d); i++)
                    {
                        string cand = Path.Combine(d, "Assets", "Tests", "EditMode", "Vectors", "mount_vectors.json");
                        if (File.Exists(cand)) { _v = MiniJson.ParseObject(File.ReadAllText(cand)); return _v; }
                        d = Path.GetDirectoryName(d);
                    }
                }
                throw new FileNotFoundException("Assets/Tests/EditMode/Vectors/mount_vectors.json 을 못 찾았다 — node tools/mount_vectors.js");
            }
        }
        public static JsonObject Consts { get { return J.Obj(V["consts"]); } }
        public static List<object> Arr(string key) { return J.Arr(J.Require(V, key)); }
    }

    /// <summary>원작이 밖에서 받는 것을 벡터의 `consts`(정본 Forge·Ascension 상수)로 채운 시험용 호스트 — 기술트리 % 는 케이스마다 준다.</summary>
    sealed class TestMountHost : IMountHost
    {
        public double Winders { get; set; }
        public double MountDmgPct, MountHpPct, MountCostPct, ExtraMountPct;
        public int AscendMount;
        readonly string[] _rarities; readonly int _ages;
        readonly double _tierBaseAtk, _tierBaseHp, _tierStep, _atkSlots, _hpSlots, _levelStep, _starMult;

        public TestMountHost(GameData g)
        {
            var c = MountVectors.Consts; var f = J.Obj(c["forge"]);
            _rarities = g.Defs.Rarities; _ages = g.Defs.Ages.Length;
            _tierBaseAtk = J.Num(f["TIER_BASE_ATK"]); _tierBaseHp = J.Num(f["TIER_BASE_HP"]); _tierStep = J.Num(f["TIER_STEP"]);
            _atkSlots = J.Num(f["ATK_SLOTS"]); _hpSlots = J.Num(f["HP_SLOTS"]); _levelStep = J.Num(f["LEVEL_STEP"]);
            _starMult = J.Num(c["STAR_MULT"]);
        }

        public double LevelMult(int level) { return Math.Pow(_levelStep, (level != 0 ? level : 1) - 1); }
        double AgeOfRarity(string r) { return Array.IndexOf(_rarities, r) * (double)(_ages - 1) / (_rarities.Length - 1); }
        public double GearSumAtkAt(string r) { return _atkSlots * (_tierBaseAtk * Math.Pow(_tierStep, AgeOfRarity(r))) * LevelMult(1); }
        public double GearSumHpAt(string r) { return _hpSlots * (_tierBaseHp * Math.Pow(_tierStep, AgeOfRarity(r))) * LevelMult(1); }
        public Big StarMult(int stars) { return stars <= 0 ? Big.One : Big.Of(_starMult).Pow(stars); }
        public int MountAscendCount() { return AscendMount; }
        public double MountDmgMult() { return 1 + MountDmgPct / 100; }
        public double MountHpMult() { return 1 + MountHpPct / 100; }
        public double MountCostMult() { return Math.Max(0.1, 1 - MountCostPct / 100); }
        public double ExtraMountChance() { return ExtraMountPct / 100; }
    }

    public class MountTests
    {
        GameData G { get { return DataDir.Game; } }
        TestMountHost _host;
        MountSystem _sys;

        MountSystem Make(uint seed, MountState state = null)
        {
            _host = new TestMountHost(G);
            _sys = new MountSystem(G, MountRules.Original(), _host, Rng.Mulberry(seed), state);
            return _sys;
        }

        static Mount M(string name, string rarity, int level = 1, int stars = 0, double xp = 0, List<Substat> subs = null)
        {
            return new Mount { Name = name, Rarity = rarity, Level = level, Stars = stars, Xp = xp, Subs = subs ?? new List<Substat>() };
        }
        static List<Mount> Ponies(int n) { var l = new List<Mount>(); for (int i = 0; i < n; i++) l.Add(M("Pony", "common")); return l; }
        static List<Mount> Named(int n) { var l = new List<Mount>(); for (int i = 0; i < n; i++) l.Add(M("M" + i, "common")); return l; }
        static List<int> Ints(object arr)
        {
            var l = new List<int>(); var a = J.Arr(arr);
            if (a != null) foreach (object v in a) if (v is double && Math.Floor((double)v) == (double)v) l.Add(J.Int(v));
            return l;
        }
        static void Near(double expected, double actual, string what) { Assert.AreEqual(expected, actual, 1e-9 * Math.Max(1, Math.Abs(expected)), what); }
        static void BigEq(JsonObject exp, Big actual, string what)
        {
            Assert.AreEqual(J.Num(exp["e"]), actual.E, what + " .e");
            Near(J.Num(exp["m"]), actual.M, what + " .m");
        }

        void AssertMounts(List<object> exp, List<Mount> actual, string what)
        {
            Assert.AreEqual(exp.Count, actual.Count, what + " mounts.length");
            for (int i = 0; i < exp.Count; i++)
            {
                var p = J.Obj(exp[i]); Mount q = actual[i]; string w = what + " mounts[" + i + "]";
                Assert.AreEqual(J.Str(p["name"]), q.Name, w + ".name");
                Assert.AreEqual(J.Str(p["rarity"]), q.Rarity, w + ".rarity");
                Assert.AreEqual(J.Int(p["level"]), q.Level, w + ".level");
                Assert.AreEqual(J.Int(p["stars"]), q.Stars, w + ".stars");
                Near(J.Num(p["xp"]), q.Xp, w + ".xp");
                var subs = J.Arr(p["subs"]);
                Assert.AreEqual(subs.Count, q.Subs.Count, w + ".subs.length");
                for (int k = 0; k < subs.Count; k++)
                {
                    var s = J.Obj(subs[k]);
                    Assert.AreEqual(J.Str(s["key"]), q.Subs[k].Key, w + ".subs[" + k + "].key");
                    Near(J.Num(s["value"]), q.Subs[k].Value, w + ".subs[" + k + "].value");
                }
            }
        }

        /// <summary>벡터의 `snap(S)` 과 상태 전체를 대조.</summary>
        void AssertSnap(JsonObject after, string what)
        {
            AssertMounts(J.Arr(after["mounts"]), _sys.State.Mounts, what);
            Assert.AreEqual(Ints(after["activeMounts"]).ToArray(), _sys.State.ActiveMounts.ToArray(), what + " activeMounts");
            Assert.AreEqual(J.Num(after["winders"]), _host.Winders, what + " winders");
            Assert.AreEqual(J.Int(after["mountOpens"]), _sys.State.MountOpens, what + " mountOpens");
        }

        [Test]
        public void 상수는_정본_mounts_js_와_같다()
        {
            var c = MountVectors.Consts; var r = MountRules.Original();
            Assert.AreEqual(J.Int(c["MAX_LEVEL"]), r.MaxLevel);
            Assert.AreEqual(J.Int(c["INDIV_MAX_LEVEL"]), r.IndivMaxLevel);
            Assert.AreEqual(J.Int(c["INV_CAP"]), r.InvCap);
            Assert.AreEqual(J.Int(c["MAX_ACTIVE_MOUNTS"]), r.MaxActiveMounts);
            var legacy = J.Obj(c["LEGACY_SPECIES"]);
            Assert.AreEqual(legacy.Count, r.LegacySpecies.Count);
            for (int i = 0; i < legacy.Count; i++) { Assert.AreEqual(legacy.Keys[i], r.LegacySpecies.KeyAt(i)); Assert.AreEqual(J.Str(legacy[legacy.Keys[i]]), r.LegacySpecies.ValueAt(i)); }
            Assert.AreEqual(J.Num(c["WINDERS_PER_SUMMON"]), G.Balance.Mounts.WindersPerSummon);
            Assert.AreEqual(r.MaxLevel, G.Balance.Mounts.MaxLevel, "표 행 수 = MAX_LEVEL");
            foreach (string name in r.LegacySpecies.Keys) Assert.IsNotNull(G.Mounts.Get(r.LegacySpecies[name]), "이관 대상 종이 조형 표에 있다: " + name);
        }

        [Test]
        public void 소환_레벨_needed_prevNeeded_문턱_150행()
        {
            var rows = MountVectors.Arr("level");
            Assert.IsTrue(rows.Count >= 100);
            foreach (object o in rows)
            {
                var c = J.Obj(o);
                Make(1, new MountState { MountOpens = J.Int(c["mountOpens"]) });
                string w = "mountOpens " + c["mountOpens"];
                Assert.AreEqual(J.Int(c["level"]), _sys.Level(), w + " level");
                double? next = _sys.NextNeeded();
                if (c["nextNeeded"] == null) Assert.IsFalse(next.HasValue, w + " nextNeeded=MAX");
                else Assert.AreEqual(J.Num(c["nextNeeded"]), next.Value, w + " nextNeeded");
                Assert.AreEqual(J.Num(c["prevNeeded"]), _sys.PrevNeeded(), w + " prevNeeded");
            }
        }

        [Test]
        public void 등급_확률표는_needed_를_뺀_키_순서()
        {
            foreach (object o in MountVectors.Arr("rates"))
            {
                var c = J.Obj(o);
                Make(1, new MountState { MountOpens = J.Int(c["mountOpens"]) });
                Assert.AreEqual(J.Int(c["level"]), _sys.Level());
                var r = _sys.Rates();
                Assert.AreEqual(J.StrArr(c["keys"]), new List<string>(r.Keys).ToArray(), "keys @" + c["mountOpens"]);
                double[] vals = J.NumArr(c["values"]);
                for (int i = 0; i < vals.Length; i++) Assert.AreEqual(vals[i], r.ValueAt(i), "value " + i + " @" + c["mountOpens"]);
            }
        }

        [Test]
        public void 태엽_비용과_canSummon()
        {
            foreach (object o in MountVectors.Arr("winderCost"))
            {
                var c = J.Obj(o);
                Make(1); _host.MountCostPct = J.Num(c["mountCost"]);
                Assert.AreEqual(J.Num(c["cost"]), _sys.WinderCost(J.Num(c["count"])), "mountCost " + c["mountCost"] + " count " + c["count"]);
            }
            foreach (object o in MountVectors.Arr("canSummon"))
            {
                var c = J.Obj(o);
                Make(1); _host.Winders = J.Num(c["winders"]);
                Assert.AreEqual(J.Bool(c["can"]), _sys.CanSummon(J.Num(c["count"])), "winders " + c["winders"] + " count " + c["count"]);
            }
        }

        [Test]
        public void 소환_배치_비용_클램프_보너스_자동장착_확률표_변화()
        {
            foreach (object o in MountVectors.Arr("summon"))
            {
                var c = J.Obj(o);
                string w = "seed " + c["seed"];
                var st = new MountState { Mounts = Ponies(J.Int(c["mounts"])), ActiveMounts = Ints(c["active"]), MountOpens = J.Int(c["mountOpens"]) };
                Make(J.UInt(c["seed"]), st);
                _host.Winders = J.Num(c["winders"]); _host.ExtraMountPct = J.Num(c["extraMount"]); _host.MountCostPct = J.Num(c["mountCost"]); _host.AscendMount = J.Int(c["ascendMount"]);
                var pre = J.Obj(c["pre"]);
                double count = J.Num(c["count"]);
                int n = _sys.SummonCount(count);
                Assert.AreEqual(J.Int(pre["summonCount"]), n, w + " summonCount");
                Assert.AreEqual(J.Num(pre["winderCost"]), _sys.WinderCost(n), w + " winderCost");
                Assert.AreEqual(J.Bool(pre["canSummon"]), _sys.CanSummon(n), w + " canSummon");
                Assert.AreEqual(J.Int(pre["space"]), _sys.Space(), w + " space");
                MountSummonResult r = _sys.Summon(count);
                var exp = J.Obj(c["result"]);
                if (exp == null) Assert.IsNull(r, w + " result null");
                else
                {
                    Assert.IsNotNull(r, w + " result");
                    var list = J.Arr(exp["results"]);
                    Assert.AreEqual(list.Count, r.Results.Count, w + " results.length");
                    for (int i = 0; i < list.Count; i++)
                    {
                        var x = J.Obj(list[i]);
                        Assert.AreEqual(J.Str(x["name"]), r.Results[i].Name, w + " results[" + i + "].name");
                        Assert.AreEqual(J.Str(x["rarity"]), r.Results[i].Rarity, w + " results[" + i + "].rarity");
                        Assert.AreEqual(J.Bool(x["isNew"]), r.Results[i].IsNew, w + " results[" + i + "].isNew");
                        Assert.AreEqual(J.Int(x["level"]), r.Results[i].Level, w + " results[" + i + "].level");
                    }
                    string[] gacha = J.StrArr(c["gacha"]);
                    Assert.AreEqual(1, gacha.Length, w + " gacha 1회");
                    Assert.AreEqual(gacha[0], r.BestRarity, w + " bestRarity");
                    Assert.AreEqual(new[] { "mountSummon:" + r.Results.Count }, J.StrArr(c["quests"]), w + " quests");
                }
                AssertSnap(J.Obj(c["after"]), w);
            }
        }

        [Test]
        public void 장착_1마리_토글_교체_setRidden_ridden()
        {
            Make(1, new MountState { Mounts = new List<Mount> { M("Pony", "common"), M("Donkey", "common"), M("Sheep", "rare"), M("Pony", "common") } });
            foreach (object o in MountVectors.Arr("equip"))
            {
                var c = J.Obj(o);
                int idx = J.Int(c["idx"]); string op = J.Str(c["op"]); string w = op + "(" + idx + ")";
                bool ok = op == "equip" ? _sys.Equip(idx) : _sys.SetRidden(idx);
                Assert.AreEqual(J.Bool(c["ok"]), ok, w + " ok");
                Assert.AreEqual(Ints(c["active"]).ToArray(), _sys.State.ActiveMounts.ToArray(), w + " active");
                if (c["riddenIdx"] == null) Assert.IsFalse(_sys.RiddenIdx().HasValue, w + " riddenIdx null");
                else Assert.AreEqual(J.Int(c["riddenIdx"]), _sys.RiddenIdx().Value, w + " riddenIdx");
                Assert.AreEqual(J.Str(c["ridden"]), _sys.Ridden(), w + " ridden");
                var isActive = J.Arr(c["isActive"]);
                for (int k = 0; k < isActive.Count; k++) Assert.AreEqual(J.Bool(isActive[k]), _sys.IsActive(k), w + " isActive(" + k + ")");
            }
            var e = J.Obj(MountVectors.V["equipEmpty"]);
            Make(1);
            Assert.IsFalse(_sys.RiddenIdx().HasValue); Assert.IsNull(_sys.RiddenInst()); Assert.IsNull(_sys.Ridden());
            Assert.AreEqual(J.Bool(e["isActive0"]), _sys.IsActive(0));
            Assert.AreEqual(J.Int(e["count"]), _sys.Count()); Assert.AreEqual(J.Int(e["space"]), _sys.Space());
        }

        [Test]
        public void ensure_는_장착_목록을_정리하고_1마리로_줄인다()
        {
            var cases = new Dictionary<string, MountState>
            {
                { "dup_and_ghost", new MountState { Mounts = Named(3), ActiveMounts = new List<int> { 1, 1, 7, 0 } } },
                { "two_to_one", new MountState { Mounts = Named(2), ActiveMounts = new List<int> { 1, 0 } } },
                { "not_array", new MountState { Mounts = Named(2) } },
                { "undefined_fields", new MountState { Mounts = Named(1), ActiveMounts = new List<int> { 0 } } },
            };
            foreach (object o in MountVectors.Arr("ensure"))
            {
                var c = J.Obj(o); string name = J.Str(c["name"]);
                Make(1, cases[name]);
                _sys.Ensure();
                AssertSnap(J.Obj(c["after"]), name);
            }
        }

        [Test]
        public void 경험치_곡선과_재료_값과_addXp()
        {
            Make(1);
            foreach (object o in MountVectors.Arr("xpNeeded")) { var c = J.Obj(o); Assert.AreEqual(J.Num(c["xp"]), _sys.XpNeeded(J.Int(c["level"])), "xpNeeded " + c["level"]); }
            foreach (object o in MountVectors.Arr("xpValue")) { var c = J.Obj(o); Assert.AreEqual(J.Num(c["xp"]), _sys.XpValue(J.Str(c["rarity"])), "xpValue " + c["rarity"]); }
            foreach (object o in MountVectors.Arr("addXp"))
            {
                var c = J.Obj(o);
                Make(1, new MountState { Mounts = new List<Mount> { M("Pony", "common", J.Int(c["level"]), 0, J.Num(c["xp"])) } });
                _sys.AddXp(0, J.Num(c["add"]));
                _sys.AddXp(9, 1);
                string w = "level " + c["level"] + " +" + c["add"];
                Assert.AreEqual(J.Int(c["level_after"]), _sys.State.Mounts[0].Level, w + " level");
                Near(J.Num(c["xp_after"]), _sys.State.Mounts[0].Xp, w + " xp");
            }
        }

        [Test]
        public void 흡수는_내림차순으로_지우고_대상_장착_인덱스를_보정한다()
        {
            var inputs = new Dictionary<string, object[]>
            {
                { "target3_mats_0_1_5_dupes_self_ghost", new object[] { new List<Mount> { M("A", "common"), M("B", "rare", 5), M("C", "epic"), M("D", "mythic", 30), M("E", "common", 2), M("F", "legendary", 3) }, new List<int> { 3 }, 3, new List<int> { 0, 1, 5, 1, 3, 9 } } },
                { "target0_mats_after", new object[] { new List<Mount> { M("A", "common"), M("B", "rare", 5), M("C", "epic") }, new List<int> { 2 }, 0, new List<int> { 2, 1 } } },
                { "active_removed", new object[] { new List<Mount> { M("A", "common"), M("B", "rare") }, new List<int> { 1 }, 0, new List<int> { 1 } } },
                { "ghost_target", new object[] { new List<Mount> { M("A", "common") }, new List<int> { 0 }, 5, new List<int> { 0 } } },
                { "no_mats", new object[] { new List<Mount> { M("A", "common") }, new List<int> { 0 }, 0, new List<int>() } },
            };
            foreach (object o in MountVectors.Arr("absorb"))
            {
                var c = J.Obj(o); string name = J.Str(c["name"]);
                object[] inp = inputs[name];
                Make(1, new MountState { Mounts = (List<Mount>)inp[0], ActiveMounts = (List<int>)inp[1] });
                int consumed;
                bool ok = _sys.AbsorbMaterials((int)inp[2], (List<int>)inp[3], out consumed);
                Assert.AreEqual(J.Bool(c["ok"]), ok, name + " ok");
                string[] quests = J.StrArr(c["quests"]);
                Assert.AreEqual(quests.Length > 0, consumed > 0, name + " mountMerge 퀘스트 = 실제 소모");
                AssertSnap(J.Obj(c["after"]), name);
            }
        }

        [Test]
        public void 기여_기준치_레벨_별_기술트리_activeBonus()
        {
            foreach (object o in MountVectors.Arr("baseStat"))
            {
                var c = J.Obj(o); Make(1);
                AtkHp b = _sys.BaseStat(J.Str(c["rarity"]));
                Near(J.Num(c["atk"]), b.Atk, "baseStat atk " + c["rarity"]); Near(J.Num(c["hp"]), b.Hp, "baseStat hp " + c["rarity"]);
            }
            foreach (object o in MountVectors.Arr("mountPower"))
            {
                var c = J.Obj(o); Make(1);
                _host.MountDmgPct = J.Num(c["mountDmg"]); _host.MountHpPct = J.Num(c["mountHp"]);
                Mount m = M("X", J.Str(c["rarity"]), J.Int(c["level"]), J.Int(c["stars"]));
                string w = c["rarity"] + " L" + c["level"] + " ★" + c["stars"];
                Near(J.Num(c["levelMult"]), _sys.LevelMult(m), w + " levelMult");
                Big atk, hp; _sys.MountPower(m, out atk, out hp);
                BigEq(J.Obj(c["atk"]), atk, w + " atk"); BigEq(J.Obj(c["hp"]), hp, w + " hp");
            }
            var subs = new List<Substat> { new Substat("critCh", "치명타 확률", 3.5), new Substat("hpPct", "체력", 12) };
            var st = new MountState { Mounts = new List<Mount> { M("Pony", "common", 1, 0, 0, subs), M("Dragon", "mythic", 10, 1, 0, new List<Substat> { new Substat("atkSpd", "공격 속도", 7.7) }) }, ActiveMounts = new List<int> { 1 } };
            Make(1, st);
            CheckBonus(J.Obj(MountVectors.V["activeBonus"]), "activeBonus");
            st.ActiveMounts = new List<int> { 0 }; CheckBonus(J.Obj(MountVectors.V["activeBonus0"]), "activeBonus0");
            st.ActiveMounts = new List<int> { 7 }; CheckBonusCount(J.Obj(MountVectors.V["activeBonusGhost"]), "activeBonusGhost");
            Make(1); CheckBonusCount(J.Obj(MountVectors.V["activeBonusEmpty"]), "activeBonusEmpty");
        }

        void CheckBonus(JsonObject exp, string what)
        {
            Big atk, hp; var subs = new List<Substat>();
            _sys.ActiveBonus(out atk, out hp, subs);
            BigEq(J.Obj(exp["atk"]), atk, what + " atk"); BigEq(J.Obj(exp["hp"]), hp, what + " hp");
            var es = J.Arr(exp["subs"]);
            Assert.AreEqual(es.Count, subs.Count, what + " subs");
            for (int i = 0; i < es.Count; i++) { Assert.AreEqual(J.Str(J.Obj(es[i])["key"]), subs[i].Key, what + " subs[" + i + "]"); Near(J.Num(J.Obj(es[i])["value"]), subs[i].Value, what + " subs[" + i + "]"); }
        }
        void CheckBonusCount(JsonObject exp, string what)
        {
            Big atk, hp; var subs = new List<Substat>();
            _sys.ActiveBonus(out atk, out hp, subs);
            BigEq(J.Obj(exp["atk"]), atk, what + " atk"); BigEq(J.Obj(exp["hp"]), hp, what + " hp");
            Assert.AreEqual(J.Int(exp["subs"]), subs.Count, what + " subs");
        }

        static JsonObject LegacyEntry(string rarity, int level = 0, int dupes = 0, int stars = 0, double xp = 0, List<object> subs = null)
        {
            var m = new JsonObject();
            m["rarity"] = rarity;
            if (level != 0) m["level"] = (double)level;
            if (dupes != 0) m["dupes"] = (double)dupes;
            if (stars != 0) m["stars"] = (double)stars;
            if (xp != 0) m["xp"] = xp;
            if (subs != null) m["subs"] = subs;
            return m;
        }

        [Test]
        public void 구세이브_맵은_개체_배열로_dupes_펼침_원본_앞_이름_장착은_인덱스()
        {
            var v = MountVectors.Arr("migrate");
            // ① 맵 + dupes + 옵션 있음/없음 + 이름·정수·소수 섞인 장착 목록 (seed 51)
            {
                var c = J.Obj(v[0]); Assert.AreEqual("legacy_map_dupes_active_names", J.Str(c["name"]));
                var s = new JsonObject(); var map = new JsonObject();
                var sub = new JsonObject(); sub["key"] = "block"; sub["label"] = "블록 확률"; sub["value"] = 2.5;
                map["Pony"] = LegacyEntry("common", 3, 2, 1, 40, new List<object> { sub });
                map["Brown Leaf"] = LegacyEntry("common", 0, 1);
                map["Dragon"] = LegacyEntry("mythic", 7);
                s["mounts"] = map;
                s["activeMounts"] = new List<object> { "Dragon", "Pony", "Nope", 3.0, 1.5 };
                var rng = Rng.Mulberry(J.UInt(c["seed"]));
                MountState st = MountSave.ReadMounts(s, G.Defs, rng, MountRules.Original());
                Make(1, st); _sys.Ensure();
                AssertSnap(J.Obj(c["after"]), "legacy map");
                // 같은 입력을 코덱 훅으로 돌려도(JSON 단계) 같은 배열이 나온다
                var s2 = MiniJson.ParseObject(MiniJson.Serialize(s));
                MountSave.MigrateInventoryHook(G.Defs, Rng.Mulberry(J.UInt(c["seed"])), MountRules.Original())(s2);
                MountState st2 = MountSave.ReadMounts(s2, G.Defs, Rng.Mulberry(99), MountRules.Original());
                Make(1, st2); _sys.Ensure();
                AssertSnap(J.Obj(c["after"]), "legacy map via hook");
            }
            // ② 130종 × dupes 1 = 260 → 원본 130 이 앞 · 중복부터 잘려 250 · 이름 장착 → 인덱스 129
            {
                var c = J.Obj(v[1]); Assert.AreEqual("legacy_overflow_keeps_originals", J.Str(c["name"]));
                var s = new JsonObject(); var map = new JsonObject();
                for (int i = 0; i < 130; i++) map["M" + i] = LegacyEntry("rare", 2, 1);
                s["mounts"] = map; s["activeMounts"] = new List<object> { "M129" };
                MountState st = MountSave.ReadMounts(s, G.Defs, Rng.Mulberry(J.UInt(c["seed"])), MountRules.Original());
                Make(1, st); _sys.Ensure();
                Assert.AreEqual(J.Int(c["len"]), st.Mounts.Count);
                Assert.AreEqual(J.StrArr(c["names"]), st.Mounts.ConvertAll(m => m.Name).ToArray());
                Assert.AreEqual(J.IntArr(c["levels"]), st.Mounts.ConvertAll(m => m.Level).ToArray());
                Assert.AreEqual(Ints(c["active"]).ToArray(), st.ActiveMounts.ToArray());
            }
            // ③ 폐기 종 이름 이관(개체는 그대로 · 장착 유지)
            {
                var c = J.Obj(v[2]); Assert.AreEqual("legacy_species_rename", J.Str(c["name"]));
                Make(1, new MountState { Mounts = new List<Mount> { M("Lily Pad", "common"), M("Pony", "common"), M("Log Raft", "common", 4, 2), M("Oak Leaf", "rare") }, ActiveMounts = new List<int> { 2 } });
                Assert.AreEqual(J.Int(c["moved"]), _sys.MigrateSpecies());
                AssertSnap(J.Obj(c["after"]), "species rename");
            }
            // ④ 배열 260 → 250 자르고 잘린 인덱스의 장착은 걷는다
            {
                var c = J.Obj(v[3]); Assert.AreEqual("array_clamped_250_active_pruned", J.Str(c["name"]));
                Make(1, new MountState { Mounts = Named(260), ActiveMounts = new List<int> { 255, 3 } });
                _sys.Ensure();
                Assert.AreEqual(J.Int(c["len"]), _sys.State.Mounts.Count);
                Assert.AreEqual(Ints(c["active"]).ToArray(), _sys.State.ActiveMounts.ToArray());
                // 코덱도 같은 자리에서 자른다
                var s = new JsonObject(); var arr = new List<object>();
                for (int i = 0; i < 260; i++) { var o = new JsonObject(); o["name"] = "M" + i; o["rarity"] = "common"; o["level"] = 1.0; arr.Add(o); }
                s["mounts"] = arr; s["activeMounts"] = new List<object> { 255.0, 3.0 };
                MountState st = MountSave.ReadMounts(s, G.Defs, Rng.Mulberry(1), MountRules.Original());
                Assert.AreEqual(250, st.Mounts.Count);
            }
            // ⑤ 칸이 없는 세이브
            {
                var c = J.Obj(v[4]); Assert.AreEqual("missing_fields", J.Str(c["name"]));
                MountState st = MountSave.ReadMounts(new JsonObject(), G.Defs, Rng.Mulberry(1), MountRules.Original());
                Make(1, st); _sys.Ensure();
                AssertSnap(J.Obj(c["after"]), "missing fields");
            }
        }

        [Test]
        public void 세이브_코덱_왕복은_키_순서와_값을_지킨다()
        {
            var st = new MountState { Mounts = new List<Mount> { M("Pony", "common", 3, 1, 12.5, new List<Substat> { new Substat("critCh", "치명타 확률", 3.5) }), M("Dragon", "mythic") }, ActiveMounts = new List<int> { 1 }, MountOpens = 42 };
            var s = new JsonObject();
            MountSave.WriteMounts(s, st);
            string json = MiniJson.Serialize(s);
            Assert.AreEqual("{\"mounts\":[{\"name\":\"Pony\",\"rarity\":\"common\",\"level\":3,\"xp\":12.5,\"stars\":1,\"subs\":[{\"key\":\"critCh\",\"label\":\"치명타 확률\",\"value\":3.5}]},{\"name\":\"Dragon\",\"rarity\":\"mythic\",\"level\":1,\"xp\":0,\"stars\":0,\"subs\":[]}],\"activeMounts\":[1],\"mountOpens\":42}", json);
            MountState back = MountSave.ReadMounts(MiniJson.ParseObject(json), G.Defs, Rng.Mulberry(1), MountRules.Original());
            Assert.AreEqual(2, back.Mounts.Count); Assert.AreEqual("Pony", back.Mounts[0].Name); Assert.AreEqual(3, back.Mounts[0].Level); Assert.AreEqual(12.5, back.Mounts[0].Xp); Assert.AreEqual(1, back.Mounts[0].Stars);
            Assert.AreEqual("critCh", back.Mounts[0].Subs[0].Key); Assert.AreEqual("치명타 확률", back.Mounts[0].Subs[0].Label); Assert.AreEqual(3.5, back.Mounts[0].Subs[0].Value);
            Assert.AreEqual(new[] { 1 }, back.ActiveMounts.ToArray()); Assert.AreEqual(42, back.MountOpens);
            // 이미 배열인 세이브에 훅을 돌려도 바뀌는 것이 없다
            var s2 = MiniJson.ParseObject(json);
            MountSave.MigrateInventoryHook(G.Defs, Rng.Mulberry(1), MountRules.Original())(s2);
            Assert.AreEqual(json, MiniJson.Serialize(s2));
        }
    }
}
