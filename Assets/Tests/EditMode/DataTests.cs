using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core;
using Forge.Core.Data;

namespace Forge.Tests
{
    /// <summary>T3 테스트가 읽는 `Assets/StreamingAssets/data/` — dotnet 하니스(bin 폴더)와 유니티 에디터(프로젝트 루트) 어디서 돌아도 위로 올라가 찾는다.</summary>
    static class DataDir
    {
        static string _dir;
        public static string Path
        {
            get
            {
                if (_dir != null) return _dir;
                foreach (string start in new[] { Directory.GetCurrentDirectory(), AppDomain.CurrentDomain.BaseDirectory })
                {
                    string d = start;
                    for (int i = 0; i < 10 && !string.IsNullOrEmpty(d); i++)
                    {
                        string cand = System.IO.Path.Combine(d, "Assets", "StreamingAssets", "data");
                        if (File.Exists(System.IO.Path.Combine(cand, GameData.BalanceFile))) { _dir = cand; return _dir; }
                        d = System.IO.Path.GetDirectoryName(d);
                    }
                }
                throw new DirectoryNotFoundException("Assets/StreamingAssets/data 를 못 찾았다 — cwd=" + Directory.GetCurrentDirectory());
            }
        }

        static GameData _g;
        public static GameData Game { get { return _g ?? (_g = GameData.LoadDirectory(Path)); } }
    }

    /// <summary>T3 — MiniJson: 파서·직렬화기 · 키 순서 보존 · JS 수 표기.</summary>
    public class MiniJsonTests
    {
        [Test]
        public void 원시값을_읽는다()
        {
            Assert.AreEqual(1.5, MiniJson.Parse("1.5"));
            Assert.AreEqual(-3.0, MiniJson.Parse("-3"));
            Assert.AreEqual(1e21, MiniJson.Parse("1e21"));
            Assert.AreEqual("a\"b\\c\né", MiniJson.Parse("\"a\\\"b\\\\c\\n\\u00e9\""));
            Assert.AreEqual(true, MiniJson.Parse("true"));
            Assert.AreEqual(false, MiniJson.Parse(" false "));
            Assert.IsNull(MiniJson.Parse("null"));
        }

        [Test]
        public void 객체는_키_순서를_지킨다()
        {
            var o = MiniJson.ParseObject("{\"common\":60,\"rare\":25,\"epic\":10,\"legendary\":4,\"ultimate\":0.9,\"mythic\":0.1}");
            Assert.AreEqual(6, o.Count);
            Assert.AreEqual(new[] { "common", "rare", "epic", "legendary", "ultimate", "mythic" }, new List<string>(o.Keys).ToArray());
            Assert.AreEqual(0.1, o["mythic"]);
            Assert.IsNull(o["없는키"]);
            var m = J.NumMap(o);
            Assert.AreEqual("mythic", m.KeyAt(5));
            Assert.AreEqual(0.9, m.ValueAt(4));
        }

        [Test]
        public void 중첩_배열과_객체()
        {
            var o = MiniJson.ParseObject("{\"parts\":[{\"id\":\"body\",\"box\":[6,3,14],\"at\":[0,1.5,1],\"c\":15257518,\"paint\":[{\"c\":13808268,\"y\":0}]}]}");
            var parts = J.Arr(o["parts"]);
            Assert.AreEqual(1, parts.Count);
            var p = J.Obj(parts[0]);
            Assert.AreEqual("body", J.Str(p["id"]));
            Assert.AreEqual(new[] { 6, 3, 14 }, J.IntArr(p["box"]));
            Assert.AreEqual(new[] { 0.0, 1.5, 1.0 }, J.NumArr(p["at"]));
            Assert.AreEqual(15257518, J.Int(p["c"]));
            Assert.AreEqual(0.0, J.Num(J.Obj(J.Arr(p["paint"])[0])["y"]));
        }

        [Test]
        public void 한글과_서로게이트_이스케이프()
        {
            Assert.AreEqual("표창 난무", MiniJson.Parse("\"표창 난무\""));
            Assert.AreEqual("😀", MiniJson.Parse("\"\\ud83d\\ude00\""));
        }

        [Test]
        public void 문법_오류는_위치를_준다()
        {
            var ex = Assert.Throws<JsonException>(() => MiniJson.Parse("{\"a\":1,}"));
            Assert.AreEqual(7, ex.Position);
            Assert.Throws<JsonException>(() => MiniJson.Parse("[1,2"));
            Assert.Throws<JsonException>(() => MiniJson.Parse("{\"a\":1} x"));
            Assert.Throws<JsonException>(() => MiniJson.ParseObject("[1]"));
        }

        [Test]
        public void 직렬화는_JS_stringify_와_같은_글자()
        {
            var o = new JsonObject();
            o["n"] = 1e21; o["f"] = 0.1 + 0.2; o["i"] = 1500.0; o["s"] = "a\"b\n한";
            o["b"] = true; o["z"] = null; o["arr"] = new List<object> { 1.0, 2.5, "x" };
            o["big"] = Big.Of("1.5e300"); o["nan"] = double.NaN;
            Assert.AreEqual("{\"n\":1e+21,\"f\":0.30000000000000004,\"i\":1500,\"s\":\"a\\\"b\\n한\",\"b\":true,\"z\":null,\"arr\":[1,2.5,\"x\"],\"big\":\"1.5e300\",\"nan\":null}", MiniJson.Serialize(o));
        }

        [Test]
        public void 정본_JSON_일곱_파일이_되읽으면_같다()
        {
            foreach (string name in GameData.Files)
            {
                string text = File.ReadAllText(System.IO.Path.Combine(DataDir.Path, name));
                object tree = MiniJson.Parse(text);
                string once = MiniJson.Serialize(tree);
                string twice = MiniJson.Serialize(MiniJson.Parse(once));
                Assert.AreEqual(once, twice, name);
            }
        }
    }

    /// <summary>T3 — GameData: data/*.json 일곱 개 → 강타입 표. 수치는 정본(T2 완료 기록)의 개수를 그대로 단언한다.</summary>
    public class GameDataTests
    {
        GameData G { get { return DataDir.Game; } }

        [Test]
        public void 조형_표_종_수_펫25_탈것29_적7_스킬오브젝트20()
        {
            Assert.AreEqual(25, G.Pets.Count);
            Assert.AreEqual(29, G.Mounts.Count);
            Assert.AreEqual(7, G.Enemies.Count);
            Assert.AreEqual(20, G.SkillFx.Count);
            Assert.AreEqual("Snail", G.Pets.Names[0]);
            Assert.AreEqual("Pony", G.Mounts.Names[0]);
            Assert.AreEqual("slime", G.Enemies.Names[0]);
            Assert.AreEqual("swordbot", G.SkillFx.Names[0]);
        }

        [Test]
        public void 펫_Snail_파츠와_관절_계약이_그대로_온다()
        {
            var snail = G.Pets["Snail"];
            Assert.AreEqual(0.022, snail.Cell);
            Assert.AreEqual(J.Arr(snail.Raw["parts"]).Count, snail.Parts.Count);
            var body = snail.Part("body");
            Assert.AreEqual(new[] { 6, 3, 14 }, body.Box);
            Assert.AreEqual(new[] { 0.0, 1.5, 1.0 }, body.At);
            Assert.AreEqual(15257518, body.C);
            Assert.AreEqual(1, body.Paint.Count);
            Assert.IsFalse(body.Paint[0].Y.IsRange); Assert.AreEqual(0, body.Paint[0].Y.Lo);
            Assert.IsNull(body.Paint[0].X);
            var head = snail.Part("head");
            Assert.AreEqual("head", head.Tag);
            Assert.AreEqual(new[] { 0.0, 3.0, 6.0 }, head.Pivot);
            Assert.AreEqual("y", head.Joint.Axis); Assert.AreEqual(0.12, head.Joint.Amp); Assert.AreEqual(0.6, head.Joint.F);
            Assert.IsNull(head.Joint.Ph);
            var eye = head.Paint[0];
            Assert.IsTrue(eye.X.IsRange); Assert.AreEqual(1, eye.X.Lo); Assert.AreEqual(1, eye.X.Hi);
            Assert.IsFalse(eye.Z.IsRange); Assert.AreEqual(-1, eye.Z.Lo);
            Assert.IsTrue(eye.Mx);
            Assert.AreEqual("head", snail.Parts[2].Parent);
        }

        [Test]
        public void 탈것은_cell_이_없고_seat_form_head_가_있다()
        {
            var pony = G.Mounts["Pony"];
            Assert.AreEqual(0.0, pony.Cell);
            Assert.AreEqual(15.5, pony.Seat);
            Assert.AreEqual("quad", pony.Form);
            Assert.AreEqual("bridle", pony.Harness);
            Assert.AreEqual(5.0, pony.Head.Hw); Assert.AreEqual(18.0, pony.Head.Y); Assert.AreEqual(11.0, pony.Head.Z); Assert.AreEqual(9.0, pony.Head.D);
            var legFL = pony.Part("legFL");
            Assert.AreEqual("leg", legFL.Tag); Assert.AreEqual(1.0, legFL.Gait);
            Assert.AreEqual("x", legFL.Joint.Axis); Assert.AreEqual(0.44, legFL.Joint.Amp); Assert.AreEqual(0.0, legFL.Joint.Ph); Assert.AreEqual(1.7, legFL.Joint.Gain);
            Assert.AreEqual(Math.PI, pony.Part("legFR").Joint.Ph.Value, 1e-15);
            Assert.IsTrue(G.Mounts["Turtle"].NoBridle);
            Assert.IsTrue(G.Mounts["Hover Board"].Flat);
            Assert.AreEqual(new[] { 0.0, 17.0, 9.0 }, G.Mounts["Bike"].Bar);
            foreach (var kv in G.Mounts.Models) { Assert.IsNotNull(kv.Value.Form, kv.Key); Assert.IsTrue(kv.Value.Seat.HasValue, kv.Key); }
        }

        [Test]
        public void 적은_jelly_fly_hop_머리키와_재질을_든다()
        {
            var slime = G.Enemies["slime"];
            Assert.IsTrue(slime.Jelly); Assert.IsFalse(slime.Fly);
            Assert.AreEqual(0.05, slime.Cell);
            Assert.AreEqual(0.62, slime.Part("body").Mat.Opacity); Assert.AreEqual(0.25, slime.Part("body").Mat.Rough);
            Assert.IsNull(slime.Part("body").Mat.Emissive);
            Assert.IsTrue(G.Enemies["bat"].Fly);
            Assert.IsTrue(G.Enemies["mushroom"].Hop);
            var core = G.SkillFx["swordbot"].Part("core");
            Assert.AreEqual(2806208, core.Mat.Emissive); Assert.AreEqual(1.5, core.Mat.EmissiveIntensity);
        }

        [Test]
        public void 모든_종의_파츠는_box3_at3_색을_가진다()
        {
            foreach (var table in new[] { G.Pets, G.Mounts, G.Enemies, G.SkillFx })
                foreach (var kv in table.Models)
                {
                    Assert.AreEqual(J.Arr(kv.Value.Raw["parts"]).Count, kv.Value.Parts.Count, kv.Key);
                    foreach (var p in kv.Value.Parts)
                    {
                        Assert.AreEqual(3, p.Box.Length, kv.Key);
                        Assert.AreEqual(3, p.At.Length, kv.Key);
                        Assert.IsTrue(p.C >= 0 && p.C <= 0xFFFFFF, kv.Key);
                        if (p.Pivot != null) Assert.AreEqual(3, p.Pivot.Length, kv.Key);
                        if (p.Parent != null) Assert.IsNotNull(kv.Value.Part(p.Parent), kv.Key + " parent " + p.Parent);
                    }
                }
        }

        [Test]
        public void 대장간_확률표_35레벨_행합_100_업그레이드_2에서35()
        {
            var f = G.Balance.Forge;
            Assert.AreEqual(35, f.MaxLevel);
            for (int lv = 1; lv <= 35; lv++)
            {
                double sum = 0; foreach (var kv in f.ProbabilitiesAt(lv)) sum += kv.Value;
                Assert.AreEqual(100.0, sum, 0.06, "level " + lv);
            }
            Assert.AreEqual("primitive", f.ProbabilitiesAt(1).KeyAt(0));
            Assert.AreEqual(100.0, f.ProbabilitiesAt(1)["primitive"]);
            Assert.AreEqual(3, f.ProbabilitiesAt(35).Count);
            foreach (var kv in f.ProbabilitiesAt(35)) Assert.IsTrue(Array.IndexOf(DataDir.Game.Defs.Ages, kv.Key) >= 0, kv.Key);
            Assert.AreEqual(34, f.Upgrades.Count);
            Assert.IsFalse(f.HasUpgrade(1)); Assert.IsTrue(f.HasUpgrade(35)); Assert.IsFalse(f.HasUpgrade(36));
            Assert.AreEqual(400.0, f.Upgrade(2).Cost); Assert.AreEqual(300.0, f.Upgrade(2).Time);
            Assert.AreEqual(103000000.0, f.Upgrade(35).Cost); Assert.AreEqual(1987200.0, f.Upgrade(35).Time);
        }

        [Test]
        public void 펫_알드랍_100행_부화시간_6등급_스탯_25종()
        {
            var p = G.Balance.Pets;
            Assert.AreEqual(100, p.EggDropRates.Count);
            Assert.IsTrue(p.HasEggDrop(1, 1)); Assert.IsTrue(p.HasEggDrop(10, 10)); Assert.IsFalse(p.HasEggDrop(11, 1));
            Assert.AreEqual(0.175, p.EggDropAt(10, 10)["common"]);
            foreach (var kv in p.EggDropRates) { double s = 0; foreach (var r in kv.Value) s += r.Value; Assert.AreEqual(1.0, s, 0.001, kv.Key); }
            Assert.AreEqual(6, p.BaseHatchingTimes.Count);
            Assert.AreEqual(30.0, p.BaseHatchingTimes["common"]); Assert.AreEqual(1920.0, p.BaseHatchingTimes["mythic"]);
            Assert.AreEqual(25, p.SpeciesCount);
            Assert.AreEqual(new[] { 6, 5, 5, 3, 3, 3 }, new[] { p.Stats["common"].Count, p.Stats["rare"].Count, p.Stats["epic"].Count, p.Stats["legendary"].Count, p.Stats["ultimate"].Count, p.Stats["mythic"].Count });
            var snail = p.Find("Snail");
            Assert.AreEqual("common", snail.Rarity); Assert.AreEqual(50.0, snail.Damage); Assert.AreEqual(1200.0, snail.Health);
            Assert.IsNull(p.Find("없는펫"));
            foreach (var kv in p.Stats) foreach (var s in kv.Value) Assert.IsNotNull(G.Pets.Get(s.Name), "조형 표에 없는 펫 " + s.Name);
        }

        [Test]
        public void 스킬_소환표_100레벨_6칸_탈것_소환표_50레벨_MAX()
        {
            var s = G.Balance.Skills;
            Assert.AreEqual(100, s.MaxLevel);
            Assert.AreEqual(new[] { 100.0, 0, 0, 0, 0, 0 }, s.RatesAt(1));
            Assert.AreEqual(6, s.RatesAt(100).Length);
            Assert.AreEqual(17.5, s.RatesAt(100)[0]);
            var m = G.Balance.Mounts;
            Assert.AreEqual(50, m.MaxLevel);
            Assert.AreEqual(2.0, m.SummonAt(1).Needed); Assert.IsFalse(m.SummonAt(1).NeededIsMax);
            Assert.AreEqual(1, m.SummonAt(1).Rates.Count); Assert.AreEqual(1.0, m.SummonAt(1).Rates["common"]);
            Assert.IsTrue(m.SummonAt(50).NeededIsMax);
            Assert.AreEqual(6, m.SummonAt(50).Rates.Count);
            Assert.AreEqual(50.0, m.WindersPerSummon);
            Assert.AreEqual(400.0, m.Boosts["mythic"]);
            int mounts = 0; foreach (var kv in m.Names) { mounts += kv.Value.Length; foreach (string n in kv.Value) Assert.IsNotNull(G.Mounts.Get(n), "조형 표에 없는 탈것 " + n); }
            Assert.AreEqual(29, mounts);
        }

        [Test]
        public void 게임_정의_시대10_등급6_부위8_서브스탯13_스킬18_테마25()
        {
            var d = G.Defs;
            Assert.AreEqual(36, d.Raw.Count);
            Assert.AreEqual(10, d.Ages.Length); Assert.AreEqual("primitive", d.Ages[0]); Assert.AreEqual(10, d.AgeKr.Count); Assert.AreEqual(10, d.AgeColors.Count);
            Assert.AreEqual(new[] { "common", "rare", "epic", "legendary", "ultimate", "mythic" }, d.Rarities);
            Assert.AreEqual(6, d.RarityMult.Count); Assert.AreEqual(6, d.RarityHex.Count);
            Assert.AreEqual(8, d.Slots.Length); Assert.AreEqual(8, d.SlotKr.Count);
            Assert.AreEqual(10, d.ItemNames.Count); Assert.AreEqual(5, d.ItemNames["primitive"]["helmet"].Length);
            Assert.AreEqual(10, d.AccNamesByAge.Count); Assert.AreEqual(5, d.AccNames.Count);
            Assert.AreEqual(16, d.NameSubstances.Count); Assert.AreEqual("holo", d.NameSubstances[0].Key); Assert.AreEqual(12, d.NameSubstances[0].Words.Length);
            Assert.AreEqual(53, d.WeaponTypes.Count);
            var club = d.WeaponTypes["club"];
            Assert.AreEqual("몽둥이", club.Kr); Assert.AreEqual("melee", club.Kind); Assert.AreEqual(0.12, club.Impact); Assert.AreEqual("slam", club.Motion); Assert.AreEqual(-0.25, club.RestX); Assert.AreEqual("club", club.Shape); Assert.AreEqual("stone", club.Mat);
            Assert.AreEqual(10, d.AgeWeapons.Count);
            Assert.AreEqual(1.0, d.SubstatMin);
            Assert.AreEqual(13, d.Substats.Count);
            Assert.AreEqual("critCh", d.Substats[0].Key); Assert.AreEqual("치명타 확률", d.Substats[0].Label); Assert.AreEqual(12.0, d.Substats[0].Max);
            Assert.AreEqual(7.0, d.Substat("skillCd").Max);
            Assert.AreEqual(18, d.SkillDefs.Count);
            var ps = d.Skill("powerStrike");
            Assert.AreEqual("표창 난무", ps.Name); Assert.AreEqual("common", ps.Rarity); Assert.AreEqual("single", ps.Type); Assert.AreEqual(3.0, ps.Mult); Assert.AreEqual(11.0, ps.Cd); Assert.AreEqual("shurikenrun", ps.Fx); Assert.AreEqual("#cfd8dc", ps.Color); Assert.AreEqual(1.55, ps.ImpactAt); Assert.IsNull(ps.Dur);
            var wc = d.Skill("warCry");
            Assert.AreEqual("buff", wc.Type); Assert.AreEqual(8.0, wc.Dur); Assert.IsNull(wc.ImpactAt);
            Assert.IsNull(d.Skill("없는스킬"));
            Assert.AreEqual(6, d.SkillBaseDmg.Count); Assert.AreEqual(6, d.SkillBasePassive.Count); Assert.AreEqual(18, d.SkillIcons.Count);
            Assert.AreEqual(25, d.PetKr.Count); Assert.AreEqual(25, d.PetColors.Count); Assert.AreEqual(25, d.PetMotion.Count);
            Assert.AreEqual(29, d.MountKr.Count); Assert.AreEqual(29, d.MountIcons.Count);
            Assert.AreEqual(1, d.Unlocks.Count); Assert.AreEqual("2-10", d.Unlocks[0].Stage); Assert.AreEqual("autoForge", d.Unlocks[0].Key);
            Assert.AreEqual(25, d.ChapterThemes.Count);
            Assert.AreEqual(0x87CEEB, d.ChapterThemes[0].Sky); Assert.AreEqual("forest", d.ChapterThemes[0].Biome); Assert.IsNull(d.ChapterThemes[0].Celestial);
            foreach (string name in d.PetKr.Keys) Assert.IsNotNull(G.Pets.Get(name), "PET_KR 에만 있는 펫 " + name);
            foreach (string name in d.MountKr.Keys) Assert.IsNotNull(G.Mounts.Get(name), "MOUNT_KR 에만 있는 탈것 " + name);
            foreach (var sd in d.SkillDefs) Assert.IsTrue(d.SkillIcons.Has(sd.Id), "아이콘 없는 스킬 " + sd.Id);
        }

        [Test]
        public void 소품은_생성기_17종과_xorshift_표본_23개()
        {
            var p = G.Props;
            Assert.AreEqual(17, p.Kinds.Length);
            Assert.AreEqual("pine", p.Kinds[0]);
            Assert.AreEqual(Xorshift32.LibSeed, p.Seed);
            Assert.AreEqual(23, p.Samples.Count);
            var pine = p.Samples["pine(1,{})"];
            Assert.AreEqual("pine", pine.Kind); Assert.AreEqual(Xorshift32.LibSeed, pine.Seed);
            Assert.AreEqual(0.17142857142857143, pine.U);
            Assert.AreEqual("trunk", pine.Parts[0].M); Assert.AreEqual(36, pine.Parts[0].N); Assert.AreEqual(36, pine.Parts[0].V.Count);
            Assert.AreEqual(new[] { -1, 0, -1, 0x9A9A9A }, pine.Parts[0].V[0]);
            Assert.AreEqual(3, pine.Parts[1].V[0].Length);
            foreach (var kv in p.Samples) { Assert.IsTrue(Array.IndexOf(p.Kinds, kv.Value.Kind) >= 0, kv.Key); foreach (var part in kv.Value.Parts) Assert.AreEqual(part.N, part.V.Count, kv.Key); }
        }

        [Test]
        public void 파일_이름_함수로도_읽는다()
        {
            var names = new List<string>();
            var g = GameData.Load(name => { names.Add(name); return File.ReadAllText(System.IO.Path.Combine(DataDir.Path, name)); });
            Assert.AreEqual(GameData.Files, names.ToArray());
            Assert.AreEqual(25, g.Pets.Count);
        }

        [Test]
        public void 빠진_표는_키_이름으로_실패한다()
        {
            var ex = Assert.Throws<JsonException>(() => BalanceTable.From(MiniJson.ParseObject("{\"forgeUpgrades\":{}}")));
            StringAssert.Contains("forgeProbabilities", ex.Message);
        }
    }

    /// <summary>T3 — Rng: 원작 도구가 Math.random 에 심는 mulberry32(shot-summon-result.js) · xorshift32(lib-seed.js) 와 같은 수열. 기대값은 node 로 원작 식을 돌린 것.</summary>
    public class RngTests
    {
        static void AssertSeq(IRandomSource src, params double[] expected)
        {
            for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], src.Next(), "#" + i);
        }

        [Test]
        public void mulberry32_시드14_9_3_은_원작_수열()
        {
            AssertSeq(new Mulberry32(14), 0.447958143427968, 0.4258096602279693, 0.5107000032439828, 0.35384935210458934, 0.19429797329939902, 0.42553471657447517);
            AssertSeq(new Mulberry32(9), 0.19872891809791327, 0.8512107962742448, 0.1417661067098379, 0.8216717173345387, 0.7238044005353004, 0.6888616101350635);
            AssertSeq(new Mulberry32(3), 0.7202267837710679, 0.03866216051392257, 0.4561921926215291, 0.07492800964973867, 0.7630053898319602, 0.481180417817086);
            AssertSeq(new Mulberry32(0xffffffff), 0.8964226141106337, 0.189478256739676, 0.7156526781618595, 0.9440599093213677, 0.8452364315744489, 0.5391399988438934);
        }

        [Test]
        public void xorshift32_lib_seed_는_원작_수열()
        {
            AssertSeq(new Xorshift32(Xorshift32.LibSeed), 0.04182539903558791, 0.723172509809956, 0.9458599390927702, 0.2822792883962393, 0.9877411089837551, 0.577017672592774);
            AssertSeq(new Xorshift32(1), 0.00006295018829405308, 0.015747428173199296, 0.6164041024167091, 0.07161863497458398, 0.5584883580449969, 0.17357419803738594);
        }

        [Test]
        public void 되감으면_같은_수열()
        {
            var m = new Mulberry32(14); double a = m.Next(); m.Next(); m.Reseed(14);
            Assert.AreEqual(a, m.Next());
            var x = new Xorshift32(7); double b = x.Next(); x.Next(); x.Reseed(7);
            Assert.AreEqual(b, x.Next());
        }

        [Test]
        public void U_randInt_rand_choice_chance_weightedPick_은_원작과_같다()
        {
            var r = Rng.Mulberry(14);
            var ints = new int[10]; for (int i = 0; i < 10; i++) ints[i] = r.RandInt(1, 6);
            Assert.AreEqual(new[] { 3, 3, 4, 3, 2, 3, 5, 1, 6, 6 }, ints);

            r = Rng.Mulberry(14);
            Assert.AreEqual(2.8146040250180544, r.Rand(0, Math.PI * 2));
            Assert.AreEqual(2.6754410007995086, r.Rand(0, Math.PI * 2));
            Assert.AreEqual(3.20882275675916, r.Rand(0, Math.PI * 2));

            var w = new OrderedMap<double>();
            w.Add("common", 60); w.Add("rare", 25); w.Add("epic", 10); w.Add("legendary", 4); w.Add("ultimate", 0.9); w.Add("mythic", 0.1);
            r = Rng.Mulberry(3);
            var picks = new string[12]; for (int i = 0; i < 12; i++) picks[i] = r.WeightedPick(w);
            Assert.AreEqual("rare,common,common,common,rare,common,common,common,common,common,common,common", string.Join(",", picks));

            r = Rng.Mulberry(9);
            var letters = new[] { "a", "b", "c", "d", "e" };
            var ch = new string[8]; for (int i = 0; i < 8; i++) ch[i] = r.Choice(letters);
            Assert.AreEqual("a,e,a,e,d,d,b,e", string.Join(",", ch));

            r = Rng.Mulberry(9);
            var bs = new bool[8]; for (int i = 0; i < 8; i++) bs[i] = r.Chance(0.3);
            Assert.AreEqual(new[] { true, false, true, false, false, false, true, false }, bs);
        }

        [Test]
        public void weightedPick_은_합으로_정규화하고_마지막_키가_폴백()
        {
            var w = new OrderedMap<double>(); w.Add("a", 0); w.Add("b", 0);
            Assert.AreEqual("a", Rng.Mulberry(1).WeightedPick(w));
            var f = DataDir.Game.Balance.Forge.ProbabilitiesAt(13);
            var r = Rng.Mulberry(5);
            for (int i = 0; i < 200; i++) Assert.IsTrue(f.Has(r.WeightedPick(f)));
        }
    }

    /// <summary>T3 — BigNum: 원작 `bignum.js`·`util.js` 의 표기 함수와 «같은 입력 → 같은 문자열». 기대값은 node 로 정본 파일을 vm 에 올려 뽑았다.</summary>
    public class BigNumTests
    {
        [TestCase(0.0, "0")]
        [TestCase(1.0, "1")]
        [TestCase(999.0, "999")]
        [TestCase(1000.0, "1k")]
        [TestCase(1500.0, "1.5k")]
        [TestCase(4000.0, "4k")]
        [TestCase(2150.0, "2.15k")]
        [TestCase(782000.0, "782k")]
        [TestCase(999499.0, "999k")]
        [TestCase(999500.0, "1m")]
        [TestCase(999999.0, "1m")]
        [TestCase(1000000.0, "1m")]
        [TestCase(20700000000.0, "20.7b")]
        [TestCase(32000000000.0, "32b")]
        [TestCase(41700000000.0, "41.7b")]
        [TestCase(999500000.0, "1b")]
        [TestCase(999500000000.0, "1t")]
        [TestCase(1000000000000.0, "1t")]
        [TestCase(1500000000000.0, "1.5t")]
        [TestCase(999500000000000.0, "1aa")]
        [TestCase(1000000000000000.0, "1aa")]
        [TestCase(1234000000000000.0, "1.23aa")]
        [TestCase(999900000000000000.0, "1ab")]
        [TestCase(1000000000000000000.0, "1ab")]
        [TestCase(1e21, "1ac")]
        [TestCase(1.5e100, "15bc")]
        [TestCase(-1.0, "-1")]
        [TestCase(-1500.0, "-1.5k")]
        [TestCase(-999500.0, "-1m")]
        [TestCase(1e308, "100dt")]
        [TestCase(double.PositiveInfinity, "∞")]
        [TestCase(double.NegativeInfinity, "-∞")]
        [TestCase(double.NaN, "0")]
        [TestCase(12.9, "12")]
        [TestCase(-0.5, "-1")]
        [TestCase(123456.789, "123k")]
        public void U_fmt_number(double n, string expected) { Assert.AreEqual(expected, NumFmt.Fmt(n)); }

        [TestCase("0", "0")]
        [TestCase("1.5e300", "1.5dr")]
        [TestCase("1e2045", "1.00e2045")]
        [TestCase("1e2046", "1.00e2046")]
        [TestCase("1e2100", "1.00e2100")]
        [TestCase("1.2345e2000", "123zl")]
        [TestCase("9.995e14", "999t")]
        [TestCase("9.999e17", "1ab")]
        [TestCase("1e21", "1ac")]
        [TestCase("-2.5e9", "-2.5b")]
        [TestCase("1.5", "1")]
        [TestCase("12345", "12.3k")]
        [TestCase("abc", "0")]
        [TestCase("1e308", "100dt")]
        [TestCase("7.77e30", "7.77af")]
        public void U_fmt_Big(string saved, string expected) { Assert.AreEqual(expected, NumFmt.Fmt(saved)); Assert.AreEqual(expected, NumFmt.Fmt(Big.Of(saved))); }

        [TestCase(0.0, "0.00")]
        [TestCase(1.13, "1.13")]
        [TestCase(149.05, "149.05")]
        [TestCase(999.999, "1000.00")]
        [TestCase(1000.0, "1.00k")]
        [TestCase(8870.0, "8.87k")]
        [TestCase(999995.0, "1.00m")]
        [TestCase(999999.5, "1.00m")]
        [TestCase(1000000.0, "1.00m")]
        [TestCase(1500000000000.0, "1.50t")]
        [TestCase(1000000000000000.0, "1.00aa")]
        [TestCase(-1.005, "-1.00")]
        [TestCase(1.005, "1.00")]
        [TestCase(2.675, "2.67")]
        [TestCase(0.125, "0.13")]
        [TestCase(1e21, "1.00ac")]
        public void U_fmtDec_number(double n, string expected) { Assert.AreEqual(expected, NumFmt.FmtDec(n)); }

        [TestCase("1.5e300", "1.50dr")]
        [TestCase("9.995e5", "999.50k")]
        [TestCase("1234.5", "1.23k")]
        [TestCase("1e2100", "1.00e2100")]
        public void U_fmtDec_Big(string saved, string expected) { Assert.AreEqual(expected, NumFmt.FmtDec(saved)); }

        [TestCase(0.0, "0%")]
        [TestCase(12.5, "12.5%")]
        [TestCase(0.004, "0%")]
        [TestCase(0.005, "0.01%")]
        [TestCase(33.3333, "33.33%")]
        [TestCase(100.0, "100%")]
        [TestCase(-0.001, "0%")]
        [TestCase(1.005, "1%")]
        [TestCase(99.999, "100%")]
        [TestCase(double.NaN, "0%")]
        public void U_pctTrim(double p, string expected) { Assert.AreEqual(expected, NumFmt.PctTrim(p)); }

        [TestCase(0.0, "0초")]
        [TestCase(7.0, "7초")]
        [TestCase(59.2, "1분 0초")]
        [TestCase(60.0, "1분 0초")]
        [TestCase(125.0, "2분 5초")]
        [TestCase(3600.0, "1시 0분")]
        [TestCase(7800.0, "2시 10분")]
        [TestCase(48600.0, "13시 30분")]
        [TestCase(86400.0, "1일 0시")]
        [TestCase(93600.0, "1일 2시")]
        [TestCase(360000.0, "4일 4시")]
        [TestCase(-5.0, "0초")]
        [TestCase(1987200.0, "23일 0시")]
        [TestCase(0.001, "1초")]
        public void U_fmtTime(double sec, string expected) { Assert.AreEqual(expected, NumFmt.FmtTime(sec)); }

        [TestCase(0.0, "0")]
        [TestCase(-0.0, "0")]
        [TestCase(1.0, "1")]
        [TestCase(1.5, "1.5")]
        [TestCase(100.0, "100")]
        [TestCase(1e21, "1e+21")]
        [TestCase(100000000000000000000.0, "100000000000000000000")]
        [TestCase(123456789012345680000.0, "123456789012345680000")]
        [TestCase(0.000001, "0.000001")]
        [TestCase(1e-7, "1e-7")]
        [TestCase(1.5e-7, "1.5e-7")]
        [TestCase(0.30000000000000004, "0.30000000000000004")]
        [TestCase(1e308, "1e+308")]
        [TestCase(5e-324, "5e-324")]
        [TestCase(0.3333333333333333, "0.3333333333333333")]
        [TestCase(1000000000000000.0, "1000000000000000")]
        [TestCase(1234.5678, "1234.5678")]
        [TestCase(-1.5e-10, "-1.5e-10")]
        [TestCase(9007199254740992.0, "9007199254740992")]
        [TestCase(1.7976931348623157e308, "1.7976931348623157e+308")]
        [TestCase(double.NaN, "NaN")]
        [TestCase(double.PositiveInfinity, "Infinity")]
        [TestCase(double.NegativeInfinity, "-Infinity")]
        public void JS_Number_toString(double n, string expected) { Assert.AreEqual(expected, JsNum.ToString(n)); }

        [TestCase(1.005, 2, "1.00")]
        [TestCase(2.675, 2, "2.67")]
        [TestCase(0.125, 2, "0.13")]
        [TestCase(0.5, 0, "1")]
        [TestCase(1.5, 0, "2")]
        [TestCase(2.5, 0, "3")]
        [TestCase(-2.5, 0, "-3")]
        [TestCase(-0.001, 2, "-0.00")]
        [TestCase(999.995, 2, "1000.00")]
        [TestCase(999.5, 0, "1000")]
        [TestCase(1e21, 2, "1e+21")]
        [TestCase(0.0, 2, "0.00")]
        [TestCase(123.456, 1, "123.5")]
        [TestCase(100000000000000000000.0, 0, "100000000000000000000")]
        [TestCase(9.995, 2, "9.99")]
        [TestCase(1.45, 1, "1.4")]
        public void JS_Number_toFixed(double n, int digits, string expected) { Assert.AreEqual(expected, JsNum.ToFixed(n, digits)); }

        [Test]
        public void JS_Math_round_는_반을_위로()
        {
            Assert.AreEqual(3.0, JsNum.Round(2.5)); Assert.AreEqual(-2.0, JsNum.Round(-2.5)); Assert.AreEqual(2.0, JsNum.Round(1.5));
            Assert.AreEqual(0.0, JsNum.Round(0.49999999999999994));
            Assert.AreEqual(42.0, JsNum.ParseFloat(" 42abc")); Assert.IsNaN(JsNum.ParseFloat("abc")); Assert.AreEqual(1500.0, JsNum.ParseFloat("1.5e3x"));
        }

        [Test]
        public void Big_산술은_원작과_같은_toString()
        {
            Assert.AreEqual("2.678033494477e0", Big.Of(1.01).Pow(99).ToString());
            Assert.AreEqual("1.416602756031e0", Big.Of(1.01).Pow(35).ToString());
            Assert.AreEqual("2.80515397234e1", Big.Of(20).Mul(Big.Of(1.01).Pow(34)).ToString());
            Assert.AreEqual("1e600", (Big.Of(1e300) * 1e300).ToString());
            Assert.AreEqual("1e900", (Big.Of(1e300) * 1e300 * 1e300).ToString());
            Assert.AreEqual("7e0", (Big.Of(10) - 3).ToString());
            Assert.AreEqual("3e-1", (Big.Of(0.1) + 0.2).ToString());
            Assert.AreEqual("1e20", (Big.Of(1e20) + 1).ToString());
            Assert.AreEqual("1e17", (Big.Of(1e17) + 1).ToString());
            Assert.AreEqual("1e18", (Big.Of(1e18) + 1).ToString());
            Assert.AreEqual("0", (Big.Of(5) / 0).ToString());
            Assert.AreEqual("1e0", Big.Of(0).Pow(0).ToString());
            Assert.AreEqual("-8e0", Big.Of(-2).Pow(3).ToString());
            Assert.AreEqual("4e0", Big.Of(-2).Pow(2).ToString());
            Assert.AreEqual("1.414213562373e0", Big.Of(2).Pow(0.5).ToString());
            Assert.AreEqual("1e50", Big.Of(1e100).Sqrt().ToString());
            Assert.AreEqual("1.23e2", Big.Of(123.456).Floor().ToString());
            Assert.AreEqual("-1e0", Big.Of(-0.5).Floor().ToString());
            Assert.AreEqual("1.5e300", Big.Parse("1.5e300").Floor().ToString());
            Assert.AreEqual("2.145e1", (Big.Of(1.5) * 6.5 * 2.2).ToString());
            Assert.AreEqual("1e309", (Big.Of(1e308) * 10).ToString());
            Assert.AreEqual("1e-318", (Big.Of(1e-308) / 1e10).ToString());
            Assert.AreEqual("3.162277660168e2", Big.Pow10(2.5).ToString());
            Assert.AreEqual("1e-4", Big.Of(0.0001).ToString());
            Assert.AreEqual("1.234567890123e19", Big.Of(12345678901234567890.0).ToString());
            Assert.AreEqual("1e21", Big.Of(1e21).ToString());
            Assert.AreEqual("1e-7", Big.Of(1e-7).ToString());
            Assert.AreEqual("1.234567891235e8", Big.Of(123456789.123456789).ToString());
        }

        [Test]
        public void Big_생성_비교_변환()
        {
            Assert.AreEqual("1e1e+308", Big.Of(double.PositiveInfinity).ToString());
            Assert.AreEqual("-1e1e+308", Big.Of(double.NegativeInfinity).ToString());
            Assert.AreEqual("0", Big.Of(double.NaN).ToString());
            Assert.AreEqual("0", Big.Parse("garbage").ToString());
            Assert.AreEqual("4.2e1", Big.Parse(" 42 ").ToString());
            Assert.AreEqual("1.5e3", Big.Parse("1.5E3").ToString());
            Assert.AreEqual("0", Big.Parse(null).ToString());
            Assert.AreEqual(double.MaxValue, (Big.Of(1e308) * 1e308).ToNumber());
            Assert.AreEqual(0.75, Big.Of(3).RatioTo(4));
            Assert.AreEqual(10.0, Big.Of(1e200).RatioTo(1e199));
            Assert.AreEqual(0.0, Big.Of(3).RatioTo(0));
            Assert.AreEqual(0, Big.Of(7).Cmp(7.0000000000001));
            Assert.AreEqual(-1, Big.Of(7).Cmp(7.000000000001));
            Assert.AreEqual(-1, Big.Of(-1).Cmp(1));
            Assert.AreEqual(1, Big.Of(0).Cmp(-1));
            Assert.AreEqual(1, Big.Of(1e5).Cmp(1e4));
            Assert.AreEqual("4e0", Big.Of(3).Clamp(4, 9).ToString());
            Assert.AreEqual(0.28443073384451945, (Big.Of(1.75) * 1.1).Log10());
            Assert.IsTrue(Big.Of(1e300) > Big.Of(1e299)); Assert.IsTrue(Big.Of(-1e300) < Big.Of(-1e299)); Assert.IsTrue(Big.Of(2).Eq(2)); Assert.IsTrue(Big.Zero.IsZero); Assert.IsTrue(Big.Of(-3).IsNeg); Assert.IsTrue(Big.One.IsPos);
            Assert.AreEqual(Big.Of("1.5e300"), Big.Parse(Big.Of("1.5e300").ToString()));
            Assert.AreEqual(1500.0, Big.Parse("1.5e3").ToNumber());
            Assert.AreEqual(0.0, Big.Of(1e-308).Div(1e10).ToNumber());
            Assert.AreEqual(Big.Of(9), Big.Of(3).Max(9)); Assert.AreEqual(Big.Of(3), Big.Of(3).Min(9));
            Assert.AreEqual("-4.2e1", (-Big.Of(42)).ToString()); Assert.AreEqual("4.2e1", Big.Of(-42).Abs().ToString());
        }
    }
}
