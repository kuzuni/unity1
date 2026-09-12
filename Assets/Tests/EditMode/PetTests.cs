using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.Pets;

namespace Forge.Tests
{
    /// <summary>
    /// T16 — Core `PetSystem` ↔ 원작 `pets.js` 대조. 기대값은 전부 `pet_vectors.json`(`tools/pet_vectors.js` 가 정본을 mulberry32 시드로
    /// **실제로 돌려** 뽑은 것)에서 온다 — 여기에 수치를 손으로 적지 않는다. 같은 시드 → 같은 등급·같은 종·같은 옵션·같은 인덱스.
    /// </summary>
    static class PetVectors
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
                        string cand = Path.Combine(d, "Assets", "Tests", "EditMode", "pet_vectors.json");
                        if (File.Exists(cand)) { _v = MiniJson.ParseObject(File.ReadAllText(cand)); return _v; }
                        d = Path.GetDirectoryName(d);
                    }
                }
                throw new FileNotFoundException("Assets/Tests/EditMode/pet_vectors.json 을 못 찾았다 — node tools/pet_vectors.js");
            }
        }
        public static JsonObject Consts { get { return J.Obj(V["consts"]); } }
        public static List<object> Arr(string key) { return J.Arr(J.Require(V, key)); }
    }

    /// <summary>원작이 밖에서 받는 것을 벡터의 `consts`(정본 Forge·Ascension 상수)로 채운 시험용 호스트 — 기술트리 % 는 케이스마다 준다.</summary>
    sealed class TestPetHost : IPetHost
    {
        public double NowMs = 1700000000000;
        public double Gems { get; set; }
        public double EggCurrency { get; set; }
        public double PetDmgPct, PetHpPct, HatchTimerPct, ExtraEggPct;
        public int AscendPet;
        readonly string[] _rarities; readonly int _ages;
        readonly double _tierBaseAtk, _tierBaseHp, _tierStep, _atkSlots, _hpSlots, _levelStep, _starMult;

        public TestPetHost(GameData g)
        {
            var c = PetVectors.Consts; var f = J.Obj(c["forge"]);
            _rarities = g.Defs.Rarities; _ages = g.Defs.Ages.Length;
            _tierBaseAtk = J.Num(f["TIER_BASE_ATK"]); _tierBaseHp = J.Num(f["TIER_BASE_HP"]); _tierStep = J.Num(f["TIER_STEP"]);
            _atkSlots = J.Num(f["ATK_SLOTS"]); _hpSlots = J.Num(f["HP_SLOTS"]); _levelStep = J.Num(f["LEVEL_STEP"]);
            _starMult = J.Num(c["STAR_MULT"]);
        }

        public double Now() { return NowMs; }
        public double LevelMult(int level) { return Math.Pow(_levelStep, (level != 0 ? level : 1) - 1); }
        double AgeOfRarity(string r) { return Array.IndexOf(_rarities, r) * (double)(_ages - 1) / (_rarities.Length - 1); }
        public double GearSumAtkAt(string r) { return _atkSlots * (_tierBaseAtk * Math.Pow(_tierStep, AgeOfRarity(r))) * LevelMult(1); }
        public double GearSumHpAt(string r) { return _hpSlots * (_tierBaseHp * Math.Pow(_tierStep, AgeOfRarity(r))) * LevelMult(1); }
        public Big StarMult(int stars) { return stars <= 0 ? Big.One : Big.Of(_starMult).Pow(stars); }
        public int PetAscendCount() { return AscendPet; }
        public double PetDmgMult() { return 1 + PetDmgPct / 100; }
        public double PetHpMult() { return 1 + PetHpPct / 100; }
        public double HatchSpeedMult() { return 1 / (1 + HatchTimerPct / 100); }
        public double ExtraEggChance() { return ExtraEggPct / 100; }
    }

    public class PetTests
    {
        GameData G { get { return DataDir.Game; } }
        TestPetHost _host;
        PetSystem _sys;

        PetSystem Make(uint seed, PetState state = null)
        {
            _host = new TestPetHost(G);
            _sys = new PetSystem(G, PetRules.Original(), _host, Rng.Mulberry(seed), state);
            return _sys;
        }

        static Pet P(string name, string rarity, int level = 1, int stars = 0, int dupes = 0, double xp = 0, List<Substat> subs = null)
        {
            return new Pet { Name = name, Rarity = rarity, Level = level, Stars = stars, Dupes = dupes, Xp = xp, Subs = subs ?? new List<Substat>() };
        }
        static List<Egg> Eggs(int n, string rarity = "common") { var l = new List<Egg>(); for (int i = 0; i < n; i++) l.Add(new Egg(rarity)); return l; }
        static List<Egg> Eggs(params string[] r) { var l = new List<Egg>(); foreach (string s in r) l.Add(new Egg(s)); return l; }
        static List<Pet> Snails(int n) { var l = new List<Pet>(); for (int i = 0; i < n; i++) l.Add(P("Snail", "common")); return l; }

        static void Near(double expected, double actual, string what)
        {
            Assert.AreEqual(expected, actual, 1e-9 * Math.Max(1, Math.Abs(expected)), what);
        }
        static void BigEq(JsonObject exp, Big actual, string what)
        {
            Assert.AreEqual(J.Num(exp["e"]), actual.E, what + " .e");
            Near(J.Num(exp["m"]), actual.M, what + " .m");
        }
        static string[] Strs(object arr) { return J.StrArr(arr); }
        static int[] Ints(object arr) { return J.IntArr(arr); }

        /// <summary>벡터의 `snap(S)` 과 상태 전체를 대조.</summary>
        void AssertSnap(JsonObject after, string what)
        {
            var st = _sys.State;
            Assert.AreEqual(Strs(after["eggs"]), st.Eggs.ConvertAll(e => e.Rarity).ToArray(), what + " eggs");
            var hat = J.Arr(after["hatching"]);
            Assert.AreEqual(hat.Count, st.Hatching.Count, what + " hatching.length");
            for (int i = 0; i < hat.Count; i++)
            {
                var h = J.Obj(hat[i]);
                Assert.AreEqual(J.Str(h["rarity"]), st.Hatching[i].Rarity, what + " hatching[" + i + "]");
                Assert.AreEqual(J.Num(h["endsAt"]), st.Hatching[i].EndsAt, what + " hatching[" + i + "].endsAt");
            }
            var pets = J.Arr(after["pets"]);
            Assert.AreEqual(pets.Count, st.Pets.Count, what + " pets.length");
            for (int i = 0; i < pets.Count; i++)
            {
                var p = J.Obj(pets[i]); Pet q = st.Pets[i]; string w = what + " pets[" + i + "]";
                Assert.AreEqual(J.Str(p["name"]), q.Name, w + ".name");
                Assert.AreEqual(J.Str(p["rarity"]), q.Rarity, w + ".rarity");
                Assert.AreEqual(J.Int(p["level"]), q.Level, w + ".level");
                Assert.AreEqual(J.Int(p["dupes"]), q.Dupes, w + ".dupes");
                Assert.AreEqual(J.Int(p["stars"]), q.Stars, w + ".stars");
                Near(J.Num(p["xp"]), q.Xp, w + ".xp");
                AssertSubs(J.Arr(p["subs"]), q.Subs, w + ".subs");
            }
            Assert.AreEqual(Ints(after["activePets"]), st.ActivePets.ToArray(), what + " activePets");
            Assert.AreEqual(J.Num(after["gems"]), _host.Gems, what + " gems");
            Assert.AreEqual(J.Num(after["eggCurrency"]), _host.EggCurrency, what + " eggCurrency");
            Assert.AreEqual(J.Int(after["petSummonCount"]), st.PetSummonCount, what + " petSummonCount");
            Assert.AreEqual(J.Int(after["hatchSlotBonus"]), st.HatchSlotBonus, what + " hatchSlotBonus");
        }
        static void AssertSubs(List<object> exp, List<Substat> actual, string what)
        {
            Assert.AreEqual(exp.Count, actual.Count, what + ".length");
            for (int i = 0; i < exp.Count; i++)
            {
                var s = J.Obj(exp[i]);
                Assert.AreEqual(J.Str(s["key"]), actual[i].Key, what + "[" + i + "].key");
                if (s["label"] != null) Assert.AreEqual(J.Str(s["label"]), actual[i].Label, what + "[" + i + "].label");
                Assert.AreEqual(J.Num(s["value"]), actual[i].Value, what + "[" + i + "].value");
            }
        }

        // ---------------------------------------------------------------- 상수

        [Test]
        public void 코드_상수는_정본_pets_js_와_같다()
        {
            var c = PetVectors.Consts; var r = PetRules.Original();
            Assert.AreEqual(J.Int(c["BASE_HATCH_SLOTS"]), r.BaseHatchSlots);
            Assert.AreEqual(J.Int(c["MAX_HATCH_SLOTS_CAP"]), r.MaxHatchSlotsCap);
            Assert.AreEqual(J.Num(c["SLOT_GEM_COST"]), r.SlotGemCost);
            Assert.AreEqual(J.Int(c["INV_CAP"]), r.InvCap);
            Assert.AreEqual(J.Int(c["MAX_ACTIVE"]), r.MaxActive);
            Assert.AreEqual(J.Int(c["AUTO_ACTIVE"]), r.AutoActive);
            Assert.AreEqual(J.Num(c["POWER_DIV"]), r.PowerDiv);
            Assert.AreEqual(J.Int(c["EGG_CAP"]), r.EggCap);
            Assert.AreEqual(J.Int(c["MAX_LEVEL"]), r.MaxLevel);
            Assert.AreEqual(J.Num(c["SUMMON_EGG_COST"]), r.SummonEggCost);
            // 표에서 오는 것: 부화 시간 6등급 · 알 드랍 100 스테이지 · 종 25 · 소환 곡선 100레벨
            Assert.AreEqual(6, G.Balance.Pets.BaseHatchingTimes.Count);
            Assert.AreEqual(100, G.Balance.Pets.EggDropRates.Count);
            Assert.AreEqual(25, G.Balance.Pets.SpeciesCount);
            Assert.AreEqual(100, G.Balance.Skills.MaxLevel);
        }

        // ---------------------------------------------------------------- 알 드랍

        [Test]
        public void 알_등급_롤_스테이지_100개_와_폴백_시드_대조()
        {
            var cases = PetVectors.Arr("eggRoll");
            Assert.AreEqual(101, cases.Count);
            foreach (object o in cases)
            {
                var c = J.Obj(o); string key = J.Str(c["key"]);
                Make(J.UInt(c["seed"]));
                string[] exp = Strs(c["rarities"]);
                for (int k = 0; k < exp.Length; k++) Assert.AreEqual(exp[k], _sys.RollEggRarity(key), key + " #" + k);
            }
            Make(1000);
            Assert.AreEqual(Strs(J.Obj(cases[0])["rarities"])[0], _sys.RollEggRarity(1, 1));
        }

        [Test]
        public void 알_드랍표_행마다_합이_1이다()
        {
            foreach (var kv in G.Balance.Pets.EggDropRates)
            {
                double sum = 0; foreach (var r in kv.Value) sum += r.Value;
                Assert.AreEqual(1.0, sum, 1e-6, kv.Key);
            }
        }

        // ---------------------------------------------------------------- 소환

        [Test]
        public void 소환_레벨은_누적_5마다_1_상한_100()
        {
            foreach (object o in PetVectors.Arr("summonLevel"))
            {
                var c = J.Obj(o);
                Make(1, new PetState { PetSummonCount = J.Int(c["petSummonCount"]) });
                Assert.AreEqual(J.Int(c["level"]), _sys.SummonLevel(), "petSummonCount=" + c["petSummonCount"]);
            }
        }

        [Test]
        public void 소환_확률표는_스킬_곡선_재사용_키_순서_RARITIES()
        {
            Make(1);
            foreach (object o in PetVectors.Arr("rates"))
            {
                var c = J.Obj(o); int level = J.Int(c["level"]);
                OrderedMap<double> r = _sys.Rates(level);
                Assert.AreEqual(G.Defs.Rarities, new List<string>(r.Keys).ToArray());
                double[] exp = J.NumArr(c["rates"]);
                for (int i = 0; i < exp.Length; i++) Assert.AreEqual(exp[i], r.ValueAt(i), "level " + level + " [" + i + "]");
            }
        }

        [Test]
        public void 소환_배치_비용_클램프_보너스알_시드_대조()
        {
            foreach (object o in PetVectors.Arr("summon"))
            {
                var c = J.Obj(o); string what = "seed " + c["seed"] + " count " + c["count"];
                Make(J.UInt(c["seed"]), new PetState { PetSummonCount = J.Int(c["petSummonCount"]), Eggs = Eggs(J.Int(c["eggs"])) });
                _host.EggCurrency = J.Num(c["eggCurrency"]); _host.ExtraEggPct = J.Num(c["extraEgg"]);
                double count = J.Num(c["count"]);
                var pre = J.Obj(c["pre"]);
                Assert.AreEqual(J.Int(pre["summonCount"]), _sys.SummonCount(count), what + " summonCount");
                Assert.AreEqual(J.Num(pre["summonCost"]), _sys.SummonCost(count), what + " summonCost");
                Assert.AreEqual(J.Bool(pre["canSummon"]), _sys.CanSummon(count), what + " canSummon");
                Assert.AreEqual(J.Int(pre["eggSpace"]), _sys.EggSpace(), what + " eggSpace");
                SummonResult r = _sys.Summon(count);
                var er = J.Obj(c["result"]);
                if (er == null) Assert.IsNull(r, what + " → null");
                else
                {
                    Assert.IsNotNull(r, what);
                    var items = J.Arr(er["results"]);
                    Assert.AreEqual(items.Count, r.Results.Count, what + " results.length");
                    for (int i = 0; i < items.Count; i++)
                    {
                        var it = J.Obj(items[i]);
                        Assert.AreEqual(J.Str(it["rarity"]), r.Results[i].Rarity, what + " results[" + i + "]");
                        Assert.AreEqual(J.Bool(it["extra"]), r.Results[i].Extra, what + " results[" + i + "].extra");
                    }
                    Assert.AreEqual(J.Num(er["requested"]), r.Requested, what + " requested");
                    Assert.AreEqual(J.Int(er["summoned"]), r.Summoned, what + " summoned");
                    Assert.AreEqual(J.Bool(er["clamped"]), r.Clamped, what + " clamped");
                    string[] gacha = Strs(c["gacha"]);
                    Assert.AreEqual(1, gacha.Length, what + " gacha");
                    Assert.AreEqual(gacha[0], r.BestRarity, what + " bestRarity");
                }
                AssertSnap(J.Obj(c["after"]), what);
            }
        }

        // ---------------------------------------------------------------- 부화

        [Test]
        public void 부화_시간_등급표_x60_x기술트리()
        {
            foreach (object o in PetVectors.Arr("hatchTime"))
            {
                var c = J.Obj(o);
                Make(1); _host.HatchTimerPct = J.Num(c["hatchTimer"]);
                Near(J.Num(c["sec"]), _sys.HatchTimeSec(J.Str(c["rarity"])), c["rarity"] + " @" + c["hatchTimer"] + "%");
            }
        }

        [Test]
        public void 부화장_슬롯_기본3_상한5_단가_누적()
        {
            foreach (object o in PetVectors.Arr("slots"))
            {
                var c = J.Obj(o);
                Make(1, new PetState { HatchSlotBonus = J.Int(c["bonus"]) }); _host.Gems = 5000;
                Assert.AreEqual(J.Int(c["max"]), _sys.MaxHatchSlots(), "bonus " + c["bonus"]);
                Assert.AreEqual(J.Num(c["cost"]), _sys.SlotCost(), "bonus " + c["bonus"]);
                Assert.AreEqual(J.Bool(c["canBuy"]), _sys.CanBuySlot(), "bonus " + c["bonus"]);
            }
            Make(1); _host.Gems = 900;
            foreach (object o in PetVectors.Arr("buySlot"))
            {
                var c = J.Obj(o);
                Assert.AreEqual(J.Bool(c["ok"]), _sys.BuySlot());
                Assert.AreEqual(J.Num(c["gems"]), _host.Gems);
                Assert.AreEqual(J.Int(c["bonus"]), _sys.State.HatchSlotBonus);
                Assert.AreEqual(J.Int(c["max"]), _sys.MaxHatchSlots());
            }
        }

        [Test]
        public void 부화_시작_슬롯_상한과_없는_알()
        {
            Make(1, new PetState { Eggs = Eggs("rare", "common", "mythic", "epic", "legendary") });
            foreach (object o in PetVectors.Arr("startHatch"))
            {
                var c = J.Obj(o);
                Assert.AreEqual(J.Bool(c["ok"]), _sys.StartHatch(J.Int(c["idx"])), "idx " + c["idx"]);
                AssertSnap(J.Obj(c["after"]), "startHatch idx " + c["idx"]);
            }
        }

        [Test]
        public void 젬_스킵_값_남은_10분당_1_올림()
        {
            Make(1);
            foreach (object o in PetVectors.Arr("gemSkipCost"))
            {
                var c = J.Obj(o);
                Assert.AreEqual(J.Num(c["cost"]), _sys.GemSkipCost(new HatchSlot("common", _host.NowMs + J.Num(c["remainMs"]))), "remain " + c["remainMs"]);
            }
        }

        [Test]
        public void 젬_스킵은_젬을_빼고_바로_부화시킨다()
        {
            Make(31, new PetState { Hatching = new List<HatchSlot> { new HatchSlot("common", 1700000000000 + 30 * 60000.0), new HatchSlot("rare", 1700000000000 + 120 * 60000.0) } });
            _host.Gems = 10;
            foreach (object o in PetVectors.Arr("gemSkip"))
            {
                var c = J.Obj(o);
                List<HatchResult> r = _sys.GemSkip(J.Int(c["idx"]));
                Assert.AreEqual(J.Bool(c["ok"]), r != null, "idx " + c["idx"]);
                if (r != null) Assert.AreEqual(1, r.Count);
                AssertSnap(J.Obj(c["after"]), "gemSkip idx " + c["idx"]);
            }
        }

        [Test]
        public void tick_부화_완료_뒤에서부터_종_균등_옵션2_자동출전3_별()
        {
            var cases = PetVectors.Arr("tick");
            var c0 = J.Obj(cases[0]);
            double now = 1700000000000;
            Make(J.UInt(c0["seed"]), new PetState
            {
                Hatching = new List<HatchSlot> { new HatchSlot("common", now - 1), new HatchSlot("mythic", now + 1), new HatchSlot("rare", now), new HatchSlot("epic", now - 5), new HatchSlot("legendary", now - 7) }
            });
            _host.AscendPet = 2;
            List<HatchResult> r = _sys.Tick();
            AssertSnap(J.Obj(c0["after"]), "tick#1");
            string[] toasts = Strs(c0["toasts"]);
            Assert.AreEqual(toasts.Length, r.Count, "부화 건수 = 토스트 수");
            Assert.AreEqual(Strs(c0["quests"]).Length, r.Count, "petHatch 퀘스트 = 부화 건수");
            for (int i = 0; i < r.Count; i++)
            {
                Assert.AreSame(_sys.State.Pets[i], r[i].Pet, "결과 순서 = 개체 순서");
                Assert.AreEqual(G.Defs.PetKr[r[i].Name], ToastName(toasts[i]), "토스트 종 이름 #" + i);
                Assert.AreEqual(toasts[i].StartsWith("🥚"), r[i].Already, "하나 더 #" + i);
                Assert.AreEqual(i < 3, r[i].AutoActivated, "자동 출전 3 #" + i);
                Assert.IsFalse(r[i].ReturnedAsEgg || r[i].Dropped);
            }
            // 1ms 뒤 — 신화 알도 깬다 · 출전은 이미 3이라 그대로
            _host.NowMs += 1;
            var c1 = J.Obj(cases[1]);
            r = _sys.Tick();
            Assert.AreEqual(1, r.Count); Assert.IsFalse(r[0].AutoActivated);
            AssertSnap(J.Obj(c1["after"]), "tick#2");
        }

        static string ToastName(string toast)
        {
            // '🎉 새 펫: 서펀트 (전설)' · '🥚 생쥐 하나 더 획득 (일반)'
            string s = toast.Substring(2).Trim();
            if (s.StartsWith("새 펫: ")) s = s.Substring(5);
            int cut = s.IndexOf(" 하나 더", StringComparison.Ordinal);
            if (cut < 0) cut = s.IndexOf(" (", StringComparison.Ordinal);
            return s.Substring(0, cut);
        }

        [Test]
        public void tick_보유_상한이면_알로_되돌리고_알칸도_차면_사라진다()
        {
            var c = J.Obj(PetVectors.Arr("tick")[2]);
            double now = 1700000000000;
            Make(J.UInt(c["seed"]), new PetState
            {
                Pets = Snails(PetRules.Original().InvCap), ActivePets = new List<int> { 0, 1, 2 },
                Hatching = new List<HatchSlot> { new HatchSlot("ultimate", now - 1), new HatchSlot("rare", now - 1) },
                Eggs = Eggs(99)
            });
            List<HatchResult> r = _sys.Tick();
            Assert.AreEqual(J.Int(c["petsLen"]), _sys.State.Pets.Count);
            Assert.AreEqual(Strs(c["eggs"]), _sys.State.Eggs.ConvertAll(e => e.Rarity).ToArray());
            Assert.AreEqual(J.Int(c["hatching"]), _sys.State.Hatching.Count);
            Assert.AreEqual(2, r.Count);
            Assert.IsTrue(r[0].ReturnedAsEgg && !r[0].Dropped && r[0].Pet == null && r[0].Rarity == "rare");
            Assert.IsTrue(r[1].Dropped && !r[1].ReturnedAsEgg && r[1].Pet == null && r[1].Rarity == "ultimate");
            Assert.AreEqual(Strs(c["quests"]).Length, r.Count, "보관함이 차도 petHatch 퀘스트는 오른다(원작)");
        }

        [Test]
        public void tick_같은_이름이_있으면_하나_더()
        {
            var c = J.Obj(PetVectors.Arr("tick")[3]);
            Make(J.UInt(c["seed"]), new PetState { Pets = new List<Pet> { P("Dog", "common", 3) }, ActivePets = new List<int> { 0 }, Hatching = new List<HatchSlot> { new HatchSlot("common", 1700000000000 - 1) } });
            List<HatchResult> r = _sys.Tick();
            AssertSnap(J.Obj(c["after"]), "dup");
            string toast = Strs(c["toasts"])[0];
            Assert.AreEqual(toast.StartsWith("🥚"), r[0].Already);
            Assert.AreEqual(r[0].Name == "Dog", r[0].Already);
        }

        // ---------------------------------------------------------------- 경험치

        [Test]
        public void 경험치_곡선과_재료_값()
        {
            Make(1);
            foreach (object o in PetVectors.Arr("xpNeeded")) { var c = J.Obj(o); Assert.AreEqual(J.Num(c["xp"]), _sys.XpNeeded(J.Int(c["level"])), "level " + c["level"]); }
            foreach (object o in PetVectors.Arr("xpValue")) { var c = J.Obj(o); Assert.AreEqual(J.Num(c["xp"]), _sys.XpValue(J.Str(c["rarity"])), (string)c["rarity"]); }
        }

        [Test]
        public void 경험치_추가_다중_레벨업_만렙_잉여_버림()
        {
            foreach (object o in PetVectors.Arr("addXp"))
            {
                var c = J.Obj(o); string what = "level " + c["level"] + " xp " + c["xp"] + " +" + c["add"];
                Make(1, new PetState { Pets = new List<Pet> { P("Cat", "common", J.Int(c["level"]), 0, 0, J.Num(c["xp"])) } });
                _sys.AddXp(0, J.Num(c["add"]));
                Assert.AreEqual(J.Int(c["level_after"]), _sys.State.Pets[0].Level, what);
                Near(J.Num(c["xp_after"]), _sys.State.Pets[0].Xp, what);
                _sys.AddXp(9, 1);
                Assert.AreEqual(1, _sys.State.Pets.Count);
            }
        }

        [Test]
        public void 흡수_재료는_사라지고_출전_인덱스가_당겨진다()
        {
            var cases = PetVectors.Arr("absorb");
            Make(1, new PetState
            {
                Pets = new List<Pet> { P("Cat", "common", 1), P("Bear", "rare", 5), P("Panda", "epic", 1), P("Genie", "mythic", 30), P("Dog", "common", 2) },
                ActivePets = new List<int> { 3, 1, 4 },
                Eggs = Eggs("rare", "legendary", "common")
            });
            var st = _sys.State;
            var c0 = J.Obj(cases[0]);
            bool ok = _sys.AbsorbMaterials(st.Pets[0], new List<Pet> { st.Pets[1], st.Pets[3], st.Pets[0] }, new List<Egg> { st.Eggs[1], st.Eggs[0] });
            Assert.AreEqual(J.Bool(c0["ok"]), ok);
            AssertSnap(J.Obj(c0["after"]), "absorb");
            Assert.AreEqual(J.Bool(J.Obj(cases[1])["ok"]), _sys.AbsorbMaterials(null, new List<Pet>(), new List<Egg>()));
            var c2 = J.Obj(cases[2]);
            Assert.AreEqual(J.Bool(c2["ok"]), _sys.AbsorbMaterials(P("Ghost", "rare"), new List<Pet> { st.Pets[0] }, new List<Egg>()));
            AssertSnap(J.Obj(c2["after"]), "absorb ghost");
        }

        // ---------------------------------------------------------------- 스탯

        [Test]
        public void 등급_기준치_장비_8부위_합의_1_3()
        {
            Make(1);
            foreach (object o in PetVectors.Arr("baseStat"))
            {
                var c = J.Obj(o); AtkHp b = _sys.BaseStat(J.Str(c["rarity"]));
                Near(J.Num(c["atk"]), b.Atk, c["rarity"] + " atk");
                Near(J.Num(c["hp"]), b.Hp, c["rarity"] + " hp");
            }
        }

        [Test]
        public void 펫_기여_레벨_별_기술트리_Big()
        {
            foreach (object o in PetVectors.Arr("petPower"))
            {
                var c = J.Obj(o); string what = c["rarity"] + " L" + c["level"] + " ★" + c["stars"];
                Make(1); _host.PetDmgPct = J.Num(c["petDmg"]); _host.PetHpPct = J.Num(c["petHp"]);
                Pet p = P("X", J.Str(c["rarity"]), J.Int(c["level"]), J.Int(c["stars"]));
                Near(J.Num(c["levelMult"]), _sys.LevelMult(p), what + " levelMult");
                Big atk, hp; _sys.PetPower(p, out atk, out hp);
                BigEq(J.Obj(c["atk"]), atk, what + " atk");
                BigEq(J.Obj(c["hp"]), hp, what + " hp");
            }
        }

        [Test]
        public void 출전_합산_없는_인덱스는_건너뛴다()
        {
            var c = J.Obj(PetVectors.V["activeBonus"]);
            Make(1, new PetState
            {
                Pets = new List<Pet>
                {
                    P("Cat", "common", 1, 0, 0, 0, new List<Substat> { new Substat("critCh", "치명타 확률", 3.5), new Substat("hpPct", "체력", 12) }),
                    P("Genie", "mythic", 10, 1, 0, 0, new List<Substat> { new Substat("atkSpd", "공격 속도", 7.7) }),
                    P("Bear", "rare"), P("Dog", "common")
                },
                ActivePets = new List<int> { 1, 0, 7 }
            });
            Big atk, hp; var subs = new List<Substat>();
            _sys.ActiveBonus(out atk, out hp, subs);
            BigEq(J.Obj(c["atk"]), atk, "atk"); BigEq(J.Obj(c["hp"]), hp, "hp");
            AssertSubs(J.Arr(c["subs"]), subs, "subs");
            var z = J.Obj(PetVectors.V["activeBonusEmpty"]);
            Make(1); subs.Clear(); _sys.ActiveBonus(out atk, out hp, subs);
            BigEq(J.Obj(z["atk"]), atk, "빈 atk"); BigEq(J.Obj(z["hp"]), hp, "빈 hp");
            Assert.AreEqual(J.Int(z["subs"]), subs.Count);
        }

        // ---------------------------------------------------------------- 출전

        [Test]
        public void 출전_토글_상한3_해제는_언제나()
        {
            var pets = new List<Pet>(); for (int i = 0; i < 5; i++) pets.Add(P("P" + i, "common"));
            Make(1, new PetState { Pets = pets });
            foreach (object o in PetVectors.Arr("toggle"))
            {
                var c = J.Obj(o); int idx = J.Int(c["idx"]);
                Assert.AreEqual(J.Bool(c["can"]), _sys.CanActivate(idx), "can " + idx);
                Assert.AreEqual(J.Bool(c["ok"]), _sys.ToggleActive(idx), "toggle " + idx);
                Assert.AreEqual(Ints(c["active"]), _sys.State.ActivePets.ToArray(), "active after " + idx);
            }
        }

        // ---------------------------------------------------------------- 합성

        [Test]
        public void 합성_여분은_뒤에서_소모_키운_것_출전_중_별은_남긴다()
        {
            var c = J.Obj(PetVectors.Arr("merge")[0]);
            Make(1, new PetState
            {
                Pets = new List<Pet> { P("Snail", "common"), P("Cat", "common", 2), P("Dog", "common"), P("Mouse", "common"), P("Bear", "rare"), P("Turtle", "common"), P("Chicken", "common", 1, 1), P("Snail", "common") },
                ActivePets = new List<int> { 4, 2, 7 }
            });
            var pre = J.Obj(c["pre"]);
            Assert.AreEqual(J.Bool(pre["can"]), _sys.CanMerge("common"));
            Assert.AreEqual(Ints(pre["spare"]), _sys.SpareIdxsOfRarity("common").ToArray());
            Assert.AreEqual(J.Int(pre["dupes"]), _sys.DupesOfRarity("common"));
            string next;
            Assert.AreEqual(MergeOutcome.Merged, _sys.Merge("common", out next));
            Assert.AreEqual("rare", next);
            AssertSnap(J.Obj(c["after"]), "merge");
            Assert.AreEqual(1, Strs(c["quests"]).Length);
            Assert.AreEqual(J.Bool(c["canAgain"]), _sys.CanMerge("common"));
        }

        [Test]
        public void 합성_구세이브_dupes_숫자부터_소진()
        {
            var c = J.Obj(PetVectors.Arr("merge")[1]);
            Make(1, new PetState { Pets = new List<Pet> { P("Bear", "rare", 1, 0, 2), P("Spider", "rare"), P("Ostrich", "rare") } });
            var pre = J.Obj(c["pre"]);
            Assert.AreEqual(J.Bool(pre["can"]), _sys.CanMerge("rare"));
            Assert.AreEqual(J.Int(pre["dupes"]), _sys.DupesOfRarity("rare"));
            string next;
            Assert.AreEqual(MergeOutcome.Merged, _sys.Merge("rare", out next));
            Assert.AreEqual("epic", next);
            AssertSnap(J.Obj(c["after"]), "legacy");
        }

        [Test]
        public void 합성_최상위_등급_여분_부족_알칸_가득()
        {
            var cases = PetVectors.Arr("merge");
            var m = J.Obj(cases[2]);
            Make(1, new PetState { Pets = new List<Pet> { P("Genie", "mythic"), P("Genie", "mythic"), P("Genie", "mythic") } });
            string next;
            Assert.AreEqual(J.Bool(m["can"]), _sys.CanMerge("mythic"));
            Assert.AreEqual(MergeOutcome.NotEnough, _sys.Merge("mythic", out next));
            Assert.AreEqual(J.Int(m["petsLen"]), _sys.State.Pets.Count);
            var t = J.Obj(cases[3]);
            Make(1, new PetState { Pets = new List<Pet> { P("Snail", "common"), P("Snail", "common") } });
            Assert.AreEqual(J.Bool(t["can"]), _sys.CanMerge("common"));
            Assert.AreEqual(MergeOutcome.NotEnough, _sys.Merge("common", out next));
            var e = J.Obj(cases[4]);
            Make(1, new PetState { Pets = Snails(3), Eggs = Eggs(100) });
            Assert.AreEqual(J.Bool(e["can"]), _sys.CanMerge("common"));
            Assert.AreEqual(MergeOutcome.EggCapFull, _sys.Merge("common", out next));
            Assert.AreEqual(J.Int(e["petsLen"]), _sys.State.Pets.Count);
            Assert.AreEqual(100, _sys.State.Eggs.Count);
        }

        // ---------------------------------------------------------------- 서브스탯

        [Test]
        public void rollSubs_풀에서_중복_없이_소수_한_자리()
        {
            foreach (object o in PetVectors.Arr("rollSubs"))
            {
                var c = J.Obj(o); int count = c["count"] != null ? J.Int(c["count"]) : 2;
                var rng = Rng.Mulberry(J.UInt(c["seed"]));
                List<Substat> subs = SubstatRoll.Roll(G.Defs, rng, count);
                AssertSubs(J.Arr(c["subs"]), subs, "seed " + c["seed"]);
                Assert.LessOrEqual(subs.Count, G.Defs.Substats.Count);
            }
        }
    }
}
