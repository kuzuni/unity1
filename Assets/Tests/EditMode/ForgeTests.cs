using System;
using System.Collections.Generic;
using NUnit.Framework;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.Forging;
using Forge.Core.Pets;

namespace Forge.Tests
{
    /// <summary>
    /// T14 — 대장간 엔진을 원작 `web/js/forge.js` 와 같은 입력·같은 시드로 대조한다.
    /// 벡터(<see cref="ForgeVectors.Json"/>)는 정본 forge.js 를 node vm 에 올리고(util·bignum·gamedata·balance-data·ascension 순 · TechTree/Quests/SFX/UI 스텁)
    /// `Math.random` 에 mulberry32(원작 `tools/shot-summon-result.js` 의 RNG · <see cref="Mulberry32"/> 와 같은 식)를 심어 뽑았다 — 방법은 PROGRESS T14 완료 기록.
    /// </summary>
    public class ForgeTests
    {
        static JsonObject V { get { return _v ?? (_v = MiniJson.ParseObject(ForgeVectors.Json)); } }
        static JsonObject _v;
        static GameData G { get { return DataDir.Game; } }

        /// <summary>double 대조 — 같은 식·같은 순서라 원칙은 일치지만 `Math.Pow` 의 런타임 ulp 차를 1e-9 상대 오차로 허용한다.</summary>
        static void Near(double expected, double actual, string what)
        {
            double tol = 1e-9 * Math.Max(1, Math.Abs(expected));
            Assert.That(Math.Abs(expected - actual) <= tol, what + ": 기대 " + JsNum.ToString(expected) + " 실제 " + JsNum.ToString(actual));
        }

        sealed class Rig
        {
            public ForgeState S = new ForgeState();
            public Wallet W = new Wallet();
            public ForgeMods Mods = new ForgeMods();
            public double Now;
            public ForgeEngine F;
            public List<KeyValuePair<string, double>> Bumps = new List<KeyValuePair<string, double>>();
            public List<int> Levels = new List<int>();
            public int Saves, Crafts;

            public Rig(int forgeLevel, uint seed = 1)
            {
                S.ForgeLevel = forgeLevel;
                F = new ForgeEngine(G, S, W, Rng.Mulberry(seed), () => Now, Mods);
                F.QuestBump += (k, n) => Bumps.Add(new KeyValuePair<string, double>(k, n));
                F.LevelReached += lv => Levels.Add(lv);
                F.SaveRequested += () => Saves++;
                F.Crafted += () => Crafts++;
            }
        }

        static void AssertItem(List<object> e, ForgeItem it, string what)
        {
            Assert.AreEqual(J.Str(e[0]), it.Name, what + " name");
            Assert.AreEqual(J.Str(e[1]), it.Slot, what + " slot");
            Assert.AreEqual(J.Str(e[2]), it.Age, what + " age");
            Assert.AreEqual(J.Int(e[3]), it.AgeIdx, what + " ageIdx");
            Assert.AreEqual(J.Str(e[4]), it.Rarity, what + " rarity");
            Assert.AreEqual(J.Num(e[5]), it.Level, what + " level");
            Assert.AreEqual(J.Str(e[6]), it.Main, what + " main");
            Assert.AreEqual(J.Num(e[7]), it.Value, what + " value");
            var subs = J.Arr(e[8]);
            Assert.AreEqual(subs.Count, it.Subs.Count, what + " subs 수");
            for (int i = 0; i < subs.Count; i++)
            {
                var s = J.Arr(subs[i]);
                Assert.AreEqual(J.Str(s[0]), it.Subs[i].Key, what + " sub" + i + " key");
                Assert.AreEqual(J.Num(s[1]), it.Subs[i].Value, what + " sub" + i + " value");
                Assert.AreEqual(G.Defs.Substat(it.Subs[i].Key).Label, it.Subs[i].Label, what + " sub" + i + " label");
            }
            Assert.AreEqual(J.Str(e[9]), it.WType, what + " wtype");
            Assert.AreEqual(J.Int(e[10]), it.NameIdx, what + " nameIdx");
            Assert.AreEqual(J.Int(e[11]), it.Stars, what + " stars");
        }

        // ===== 상수 · 표 =====

        [Test]
        public void 상수는_정본_forge_js_와_같다()
        {
            var c = J.Obj(V["consts"]);
            Assert.AreEqual(J.Int(c["MAX_LEVEL"]), ForgeRules.MaxLevel);
            Assert.AreEqual(J.Num(c["TIER_BASE_ATK"]), ForgeRules.TierBaseAtk);
            Assert.AreEqual(J.Num(c["TIER_BASE_HP"]), ForgeRules.TierBaseHp);
            Assert.AreEqual(J.Num(c["TIER_STEP"]), ForgeRules.TierStep);
            Assert.AreEqual(J.Int(c["ATK_SLOTS"]), ForgeRules.AtkSlots);
            Assert.AreEqual(J.Int(c["HP_SLOTS"]), ForgeRules.HpSlots);
            Assert.AreEqual(J.Num(c["LEVEL_STEP"]), ForgeRules.LevelStep);
            Assert.AreEqual(J.Int(c["ROLL_BASE_CAP"]), ForgeRules.RollBaseCap);
            Assert.AreEqual(J.Num(c["ROLL_UP_PCT"]), ForgeRules.RollUpPct);
            Assert.AreEqual(J.Num(c["ROLL_SAME_PCT"]), ForgeRules.RollSamePct);
        }

        [Test]
        public void 만렙은_확률표_행수와_같고_업그레이드_표는_2부터_35까지다()
        {
            Assert.AreEqual(35, ForgeRules.MaxLevel);
            Assert.AreEqual(ForgeRules.MaxLevel, G.Balance.Forge.MaxLevel);
            Assert.IsFalse(G.Balance.Forge.HasUpgrade(1));
            for (int lv = 2; lv <= ForgeRules.MaxLevel; lv++) Assert.IsTrue(G.Balance.Forge.HasUpgrade(lv), "업그레이드 표 " + lv);
            Assert.IsFalse(G.Balance.Forge.HasUpgrade(ForgeRules.MaxLevel + 1));
            Assert.AreEqual(10, G.Defs.Ages.Length);
            Assert.AreEqual(6, G.Defs.Rarities.Length);
            Assert.AreEqual(8, G.Defs.Slots.Length);
        }

        [Test]
        public void 시대_확률표_35행은_시대_10종_안에서_합이_100이다()
        {
            var sums = J.Arr(V["probSums"]);
            Assert.AreEqual(35, sums.Count);
            var ages = new HashSet<string>(G.Defs.Ages);
            foreach (var row in sums)
            {
                var r = J.Obj(row);
                int level = J.Int(r["level"]);
                var probs = G.Balance.Forge.ProbabilitiesAt(level);
                double sum = 0;
                foreach (var kv in probs) { Assert.IsTrue(ages.Contains(kv.Key), level + "레벨 시대 " + kv.Key); Assert.Greater(kv.Value, 0); sum += kv.Value; }
                Assert.AreEqual(J.Num(r["sum"]), sum, 1e-9, level + "레벨 합(정본과)");
                Assert.LessOrEqual(Math.Abs(sum - 100), 0.05 + 1e-9, level + "레벨 합 = 100 ± 0.05(추출원 반올림 · weightedPick 이 정규화)");
            }
            Assert.AreEqual(100, G.Balance.Forge.ProbabilitiesAt(1)["primitive"]);
            Assert.AreEqual(3, G.Balance.Forge.ProbabilitiesAt(35).Count);
        }

        [Test]
        public void 티어_기본치_레벨_배율_등급축()
        {
            foreach (var t in J.List(V["tier"], J.Obj))
            {
                int a = J.Int(t["ageIdx"]);
                Near(J.Num(t["atk"]), ForgeRules.TierBaseAtkAt(a), "tierBaseAtk " + a);
                Near(J.Num(t["hp"]), ForgeRules.TierBaseHpAt(a), "tierBaseHp " + a);
            }
            foreach (var m in J.List(V["levelMult"], J.Obj)) Near(J.Num(m["mult"]), ForgeRules.LevelMult(J.Int(m["level"])), "levelMult " + J.Int(m["level"]));
            foreach (var r in J.List(V["rarityAxis"], J.Obj))
            {
                string rarity = J.Str(r["rarity"]);
                Near(J.Num(r["age"]), ForgeRules.AgeOfRarity(G.Defs, rarity), "ageOfRarity " + rarity);
                Near(J.Num(r["sumAtk1"]), ForgeRules.GearSumAtkAt(G.Defs, rarity), "gearSumAtkAt " + rarity);
                Near(J.Num(r["sumHp1"]), ForgeRules.GearSumHpAt(G.Defs, rarity), "gearSumHpAt " + rarity);
                Near(J.Num(r["sumAtk50"]), ForgeRules.GearSumAtkAt(G.Defs, rarity, 50), "gearSumAtkAt50 " + rarity);
                Near(J.Num(r["sumHp50"]), ForgeRules.GearSumHpAt(G.Defs, rarity, 50), "gearSumHpAt50 " + rarity);
            }
            Assert.AreEqual(0, ForgeRules.AgeOfRarity(G.Defs, "common"));
            Assert.AreEqual(9, ForgeRules.AgeOfRarity(G.Defs, "mythic"));
        }

        [Test]
        public void 업그레이드_비용_시간_표_36행_기술트리_배율_하한1()
        {
            int n = 0;
            foreach (var row in J.List(V["upgrades"], J.Obj))
            {
                int lv = J.Int(row["level"]);
                var rig = new Rig(lv);
                ForgeUpgrade info = rig.F.UpgradeInfo();
                var next = J.Obj(row["next"]);
                if (next == null) { Assert.IsNull(info, lv + "레벨: 만렙이면 null"); Assert.IsFalse(rig.F.CanStartUpgrade()); continue; }
                Assert.IsNotNull(info, lv + "레벨 info");
                Assert.AreEqual(J.Num(next["cost"]), info.Cost, lv + " cost");
                Assert.AreEqual(J.Num(next["time"]), info.Time, lv + " time");
                Assert.AreEqual(J.Num(row["cost0"]), rig.F.UpgradeCost(info), lv + " cost0");
                Assert.AreEqual(J.Num(row["time0"]), rig.F.UpgradeTime(info), lv + " time0");
                rig.Mods.ForgeCostMult = Math.Max(0.1, 1 - 20 / 100.0);
                rig.Mods.ForgeTimeMult = 1 / (1 + 50 / 100.0);
                Assert.AreEqual(J.Num(row["costMod"]), rig.F.UpgradeCost(info), lv + " costMod");
                Near(J.Num(row["timeMod"]), rig.F.UpgradeTime(info), lv + " timeMod");
                rig.Mods.ForgeCostMult = Math.Max(0.1, 1 - 95 / 100.0);
                Assert.AreEqual(J.Num(row["costFloor"]), rig.F.UpgradeCost(info), lv + " costFloor");
                n++;
            }
            Assert.AreEqual(34, n);
            Assert.AreEqual(1, ForgeRules.UpgradeCost(new ForgeUpgrade { Cost = 5, Time = 1 }, 0.1), "비용 하한 1");
        }

        [Test]
        public void 젬_스킵_비용은_남은_10분당_1_올림()
        {
            foreach (var g in J.List(V["gemSkip"], J.Obj))
            {
                double ms = J.Num(g["remainMs"]);
                Assert.AreEqual(J.Num(g["gems"]), ForgeRules.GemSkipCost(ms), "remain " + ms);
                var rig = new Rig(1);
                rig.Now = 1000; rig.S.UpgradeEndsAt = 1000 + ms;
                Assert.AreEqual(J.Num(g["gems"]), rig.F.GemSkipCost(), "engine remain " + ms);
            }
            Assert.AreEqual(0, ForgeRules.GemSkipCost(-5000));
            Assert.AreEqual(J.Num(V["gemSkipIdle"]), new Rig(1).F.GemSkipCost(), "진행 중이 아니면 0");
        }

        [Test]
        public void 등급_가중치_35레벨()
        {
            foreach (var row in J.List(V["rarityWeights"], J.Obj))
            {
                int fl = J.Int(row["fl"]);
                var w = ForgeRules.RarityWeights(fl);
                var e = J.NumMap(row["w"]);
                Assert.AreEqual(6, w.Count);
                for (int i = 0; i < 6; i++)
                {
                    Assert.AreEqual(e.KeyAt(i), w.KeyAt(i), fl + " 순서 " + i);
                    Assert.AreEqual(e.ValueAt(i), w.ValueAt(i), fl + " " + w.KeyAt(i));
                }
            }
            Assert.AreEqual(G.Defs.Rarities, new List<string>(ForgeRules.RarityWeights(1).Keys).ToArray(), "순서 = RARITIES");
        }

        [Test]
        public void 확률표_범위_밖_레벨은_1레벨_행()
        {
            var fb = J.Obj(V["ageProbsFallback"]);
            var f = new Rig(1).F;
            foreach (var k in new[] { "at0", "at36", "at35" })
            {
                int lv = int.Parse(k.Substring(2));
                var e = J.NumMap(fb[k]);
                var a = f.AgeProbsAt(lv);
                Assert.AreEqual(e.Count, a.Count, k);
                for (int i = 0; i < e.Count; i++) { Assert.AreEqual(e.KeyAt(i), a.KeyAt(i), k); Assert.AreEqual(e.ValueAt(i), a.ValueAt(i), k); }
            }
        }

        [Test]
        public void 부위별_변형수와_개별_드랍_확률()
        {
            var f = new Rig(1).F;
            foreach (var row in J.List(V["variantCount"], J.Obj))
            {
                string age = J.Str(row["age"]);
                foreach (var slot in new[] { "weapon", "helmet", "armor", "ring", "belt" })
                    Assert.AreEqual(J.Int(row[slot]), f.VariantCount(age, slot), age + " " + slot);
            }
            int n = 0;
            foreach (var blk in J.List(V["itemDropChance"], J.Obj))
            {
                var rig = new Rig(J.Int(blk["fl"]));
                foreach (var r in J.List(blk["rows"], J.Obj))
                {
                    Near(J.Num(r["pct"]), rig.F.ItemDropChance(J.Str(r["age"]), J.Str(r["slot"])), "fl " + J.Int(blk["fl"]) + " " + J.Str(r["age"]) + " " + J.Str(r["slot"]));
                    n++;
                }
            }
            Assert.AreEqual(4 * 80, n);
            Assert.AreEqual(f.WeaponsOfAge("medieval"), f.WeaponsOfAge("없는시대"), "미정의 시대는 중세 무기 풀");
        }

        // ===== 뽑기(시드 고정) =====

        [Test]
        public void rollItem_시드_3개_레벨_5개_300개가_정본과_같다()
        {
            int n = 0;
            foreach (var blk in J.List(V["rollItems"], J.Obj))
            {
                uint seed = (uint)J.Int(blk["seed"]);
                int fl = J.Int(blk["fl"]);
                var rig = new Rig(fl, seed);
                rig.S.AscendCount = J.Int(blk["stars"]);
                var items = J.Arr(blk["items"]);
                for (int i = 0; i < items.Count; i++) { AssertItem(J.Arr(items[i]), rig.F.RollItem(), "seed " + seed + " fl " + fl + " #" + i); n++; }
                var after = J.NumMap(blk["rollLevelAfter"]);
                Assert.AreEqual(after.Count, rig.S.RollLevel.Count, "rollLevel 시대 수");
                foreach (var kv in after) Assert.AreEqual(kv.Value, rig.S.RollLevel[kv.Key], "seed " + seed + " fl " + fl + " rollLevel." + kv.Key);
            }
            Assert.AreEqual(300, n);
        }

        [Test]
        public void 뽑기_레벨_랜덤워크_300보와_캡()
        {
            var walk = J.Obj(V["rollWalk"]);
            var rig = new Rig(1, (uint)J.Int(walk["seed"]));
            var steps = J.NumArr(walk["steps"]);
            for (int i = 0; i < steps.Length; i++) Assert.AreEqual(steps[i], rig.F.AdvanceRollLevel("primitive"), "step " + i);
            Assert.AreEqual(300, steps.Length);

            var cap = J.Obj(V["rollWalkCap"]);
            var rig2 = new Rig(1, (uint)J.Int(cap["seed"]));
            rig2.Mods.GearMaxLevelBonus = 10;
            rig2.S.RollLevel["medieval"] = J.Num(cap["start"]);
            Assert.AreEqual(J.Num(cap["cap"]), rig2.F.MaxItemLevel());
            var st = J.NumArr(cap["steps"]);
            for (int i = 0; i < st.Length; i++) Assert.AreEqual(st[i], rig2.F.AdvanceRollLevel("medieval"), "cap step " + i);
            Assert.AreEqual(1, rig2.S.RollLevel["primitive"], "ensure 가 나머지 시대를 1 로");

            var rig3 = new Rig(1);
            rig3.S.RollLevel["medieval"] = 105;
            Assert.AreEqual(J.Num(cap["rollLevelOfClamped"]), rig3.F.RollLevelOf("medieval"), "캡이 내려가면 잘라서 반환");
            Assert.AreEqual(105, rig3.S.RollLevel["medieval"], "저장값은 안 건드린다");
        }

        [Test]
        public void ensureRollLevels_는_망가진_값을_1로_resetRollLevels_는_전부_1로()
        {
            var rig = new Rig(1);
            rig.S.RollLevel = new Dictionary<string, double> { { "primitive", double.NaN }, { "medieval", -3 }, { "space", 7 } };
            rig.F.EnsureRollLevels();
            var e = J.NumMap(V["ensureRollLevels"]);
            Assert.AreEqual(e.Count, rig.S.RollLevel.Count);
            foreach (var kv in e) Assert.AreEqual(kv.Value, rig.S.RollLevel[kv.Key], kv.Key);
            Assert.AreEqual(7, rig.S.RollLevel["space"]);

            rig.S.RollLevel["primitive"] = 40;
            rig.F.ResetRollLevels();
            var r = J.NumMap(V["resetRollLevels"]);
            Assert.AreEqual(r.Count, rig.S.RollLevel.Count);
            foreach (var kv in r) Assert.AreEqual(kv.Value, rig.S.RollLevel[kv.Key], kv.Key);

            var rig2 = new Rig(1);
            rig2.S.RollLevel = null;
            Assert.AreEqual(1, rig2.F.RollLevelOf("divine"), "null 이어도 1 부터");
        }

        [Test]
        public void craft_는_망치를_소모하고_모자라면_거기서_끝()
        {
            var c = J.Obj(V["craftShort"]);
            var rig = new Rig(5, (uint)J.Int(c["seed"]));
            rig.W.Hammers = 3;
            var items = rig.F.Craft(J.Int(c["req"]));
            var e = J.StrArr(c["items"]);
            Assert.AreEqual(e.Length, items.Count);
            for (int i = 0; i < e.Length; i++) Assert.AreEqual(e[i], items[i].Name + "|" + items[i].Age + "|" + items[i].Rarity + "|" + JsNum.ToString(items[i].Level), "#" + i);
            Assert.AreEqual(J.Num(c["hammers"]), rig.W.Hammers);
            Assert.AreEqual(J.Num(c["totalCrafts"]), rig.W.TotalCrafts);
            Assert.AreEqual(1, rig.Bumps.Count);
            Assert.AreEqual("craft", rig.Bumps[0].Key);
            Assert.AreEqual(3, rig.Bumps[0].Value);
            Assert.AreEqual(1, rig.Crafts, "SFX.craft 한 번");

            var none = J.Obj(V["craftNone"]);
            var rig0 = new Rig(5, 3);
            Assert.AreEqual(J.Int(none["items"]), rig0.F.Craft(3).Count);
            Assert.AreEqual(0, rig0.Crafts, "망치 0 이면 소리도 없다");
            Assert.AreEqual(1, rig0.Bumps.Count, "퀘스트는 0 개로도 부른다(원작 그대로)");
            Assert.AreEqual(0, rig0.Bumps[0].Value);
        }

        [Test]
        public void craft_무료_제련_확률은_망치를_안_쓴다()
        {
            var c = J.Obj(V["craftFree"]);
            var rig = new Rig(12, (uint)J.Int(c["seed"]));
            rig.W.Hammers = 20;
            rig.Mods.FreeForgeChance = 50 / 100.0;
            var items = rig.F.Craft(J.Int(c["req"]));
            Assert.AreEqual(J.Int(c["req"]), items.Count);
            Assert.AreEqual(J.Num(c["hammersAfter"]), rig.W.Hammers, "무료 제련이 섞여 20 − 10 보다 많이 남는다");
            Assert.AreEqual(J.Num(c["totalCrafts"]), rig.W.TotalCrafts);
        }

        // ===== 업그레이드 흐름 =====

        [Test]
        public void 업그레이드_시작_틱_젬스킵_만렙_흐름이_정본과_같다()
        {
            var fl = J.Obj(V["flow"]);
            var rig = new Rig(1);
            rig.W.Coins = 399; rig.W.Gems = 2; rig.Now = 5000;
            Assert.AreEqual(J.Bool(fl["canStartPoor"]), rig.F.CanStartUpgrade(), "코인 부족");
            Assert.AreEqual(J.Bool(fl["startPoor"]), rig.F.StartUpgrade());
            Assert.AreEqual(399, rig.W.Coins, "실패하면 코인 그대로");
            rig.W.Coins = 1000;
            Assert.AreEqual(J.Bool(fl["canStart"]), rig.F.CanStartUpgrade());
            Assert.AreEqual(J.Bool(fl["start"]), rig.F.StartUpgrade());
            Assert.AreEqual(J.Num(fl["coinsAfter"]), rig.W.Coins);
            Assert.AreEqual(J.Num(fl["endsAt"]), rig.S.UpgradeEndsAt);
            Assert.AreEqual(J.Int(fl["saves"]), rig.Saves);
            Assert.AreEqual(J.Bool(fl["canStartWhileRunning"]), rig.F.CanStartUpgrade(), "진행 중엔 못 건다");
            rig.Now = 5000 + 150000;
            Assert.AreEqual(J.Num(fl["gemCostMid"]), rig.F.GemSkipCost());
            Assert.AreEqual(J.Bool(fl["gemSkipPoor"]), rig.F.GemSkip());
            rig.Now = 5000 + 299999; rig.F.TickUpgrade();
            Assert.AreEqual(J.Int(fl["levelBeforeEnd"]), rig.S.ForgeLevel);
            rig.Now = 5000 + 300000; rig.F.TickUpgrade();
            Assert.AreEqual(J.Int(fl["levelAtEnd"]), rig.S.ForgeLevel);
            Assert.IsNull(rig.S.UpgradeEndsAt);
            Assert.AreEqual(J.Int(fl["savesAfterTick"]), rig.Saves);

            rig.W.Coins = 10000; rig.W.Gems = 5; rig.Now = 100000;
            Assert.AreEqual(J.Bool(fl["start2"]), rig.F.StartUpgrade());
            Assert.AreEqual(J.Num(fl["endsAt2"]), rig.S.UpgradeEndsAt);
            rig.Now = 200000;
            Assert.AreEqual(J.Num(fl["gemCost2"]), rig.F.GemSkipCost());
            Assert.AreEqual(J.Bool(fl["gemSkip2"]), rig.F.GemSkip());
            Assert.AreEqual(J.Num(fl["gemsAfter2"]), rig.W.Gems);
            Assert.AreEqual(J.Int(fl["level2"]), rig.S.ForgeLevel);
            Assert.IsNull(rig.S.UpgradeEndsAt);
            Assert.AreEqual(J.Bool(fl["gemSkipIdle"]), rig.F.GemSkip(), "진행 중이 아니면 false");

            var bumps = J.Arr(fl["bumpsAfterTick"]);
            Assert.AreEqual(bumps.Count, rig.Bumps.Count, "퀘스트 bump 수");
            for (int i = 0; i < bumps.Count; i++)
            {
                var b = J.Arr(bumps[i]);
                Assert.AreEqual(J.Str(b[0]), rig.Bumps[i].Key, "bump " + i);
                Assert.AreEqual(J.Num(b[1]), rig.Bumps[i].Value, "bump " + i);
            }
            Assert.AreEqual(J.Arr(fl["toasts"]).Count, rig.Levels.Count, "LevelReached = 토스트 수(벡터는 흐름 끝 시점의 배열)");
            Assert.AreEqual(2, rig.Levels.Count);
            Assert.AreEqual(2, rig.Levels[0]);
            Assert.AreEqual(3, rig.Levels[1]);

            var max = new Rig(35); max.W.Coins = 1e12;
            Assert.IsNull(max.F.UpgradeInfo());
            Assert.AreEqual(J.Obj(fl["maxInfo"]) == null, max.F.UpgradeInfo() == null);
            Assert.IsFalse(max.F.CanStartUpgrade());
            Assert.IsFalse(max.F.StartUpgrade());

            var last = new Rig(34); last.W.Coins = 1e12; last.Now = 0;
            Assert.IsTrue(last.F.StartUpgrade());
            last.Now = 1987200000;
            Assert.IsTrue(last.F.TickUpgrade());
            Assert.AreEqual(J.Int(fl["lv35"]), last.S.ForgeLevel);
            Assert.IsFalse(last.F.TickUpgrade(), "두 번 오르지 않는다");
        }

        // ===== 자동 제련 =====

        [Test]
        public void 자동_제련_설정_기본값과_옛_필드_마이그레이션()
        {
            var rig = new Rig(1);
            var cfg = rig.F.AutoForgeConfig();
            var d = J.Obj(V["autoDefault"]);
            Assert.AreEqual(0, cfg.KeepAges.Count);
            Assert.AreEqual(J.Bool(d["filterOn"]), cfg.FilterOn);
            Assert.AreEqual(0, cfg.FilterSubs.Count);
            Assert.AreEqual(J.Num(d["hammersPerBatch"]), cfg.HammersPerBatch);
            Assert.AreEqual(J.Bool(d["stopOnTarget"]), cfg.StopOnTarget);
            Assert.AreSame(cfg, rig.F.AutoForgeConfig(), "한 번 만든 설정을 계속 쓴다");
            Assert.IsFalse(rig.F.HasAutoTarget());

            var m = J.Obj(V["autoMigrate"]);
            var rig2 = new Rig(1);
            rig2.S.AutoForge = new AutoForgeConfig { KeepAges = new List<string> { "medieval" }, HammersPerBatch = 5, LegacyContinueOnTarget = true, StopOnTarget = true };
            var c2 = rig2.F.AutoForgeConfig();
            Assert.AreEqual(J.Bool(m["stopOnTarget"]), c2.StopOnTarget, "continueOnTarget 은 참/거짓 모두 «계속»");
            Assert.IsFalse(c2.LegacyContinueOnTarget.HasValue);
            Assert.AreEqual(J.Num(m["hammersPerBatch"]), c2.HammersPerBatch);
            Assert.AreEqual(J.StrArr(m["keepAges"]), c2.KeepAges.ToArray());
            Assert.IsTrue(rig2.F.HasAutoTarget());

            var ns = J.Obj(V["autoNoStop"]);
            var rig3 = new Rig(1);
            rig3.S.AutoForge = new AutoForgeConfig { FilterOn = true, FilterSubs = new List<string> { "critCh" }, HammersPerBatch = 5, KeepAges = null };
            var c3 = rig3.F.AutoForgeConfig();
            Assert.AreEqual(J.Bool(ns["stopOnTarget"]), c3.StopOnTarget);
            Assert.IsNotNull(c3.KeepAges);
            Assert.IsTrue(rig3.F.HasAutoTarget());
        }

        [Test]
        public void 자동_제련_필터_10사례()
        {
            int n = 0;
            foreach (var c in J.List(V["autoFilter"], J.Obj))
            {
                var cfg = J.Obj(c["cfg"]);
                var rig = new Rig(1);
                rig.S.AutoForge = new AutoForgeConfig
                {
                    KeepAges = new List<string>(J.StrArr(cfg["keepAges"])),
                    FilterOn = J.Bool(cfg["filterOn"]),
                    FilterSubs = new List<string>(J.StrArr(cfg["filterSubs"]))
                };
                var it = J.Obj(c["item"]);
                var item = new ForgeItem { Age = J.Str(it["age"]) };
                foreach (var s in J.List(it["subs"], J.Obj)) item.Subs.Add(new Substat(J.Str(s["key"]), "", J.Num(s["value"])));
                string what = "#" + n + " " + MiniJson.Serialize(cfg) + " " + MiniJson.Serialize(it);
                Assert.AreEqual(J.Bool(c["hasTargetActual"]), rig.F.HasAutoTarget(), what + " hasAutoTarget");
                Assert.AreEqual(J.Bool(c["hasTarget"]), rig.F.HasAutoTarget(), what + " hasAutoTarget(기대)");
                Assert.AreEqual(J.Bool(c["passes"]), rig.F.PassesAutoFilter(item), what + " passes");
                n++;
            }
            Assert.AreEqual(10, n);
        }

        [Test]
        public void 서브스탯_굴림_T16_SubstatRoll_을_장비도_같이_쓴다_풀_13종_중복_없음_소수_1자리()
        {
            var rng = Rng.Mulberry(99);
            for (int rep = 0; rep < 50; rep++)
            {
                var subs = SubstatRoll.Roll(G.Defs, rng, 4);
                Assert.AreEqual(4, subs.Count);
                var seen = new HashSet<string>();
                foreach (var s in subs)
                {
                    Assert.IsTrue(seen.Add(s.Key), "중복 " + s.Key);
                    var def = G.Defs.Substat(s.Key);
                    Assert.IsNotNull(def);
                    Assert.AreEqual(def.Label, s.Label);
                    Assert.GreaterOrEqual(s.Value, G.Defs.SubstatMin);
                    Assert.LessOrEqual(s.Value, def.Max);
                    Assert.AreEqual(s.Value, Math.Round(s.Value * 10) / 10, 1e-9, "소수 1자리");
                }
            }
            Assert.AreEqual(13, SubstatRoll.Roll(G.Defs, rng, 20).Count, "풀보다 많이 달라면 풀 크기까지");
            Assert.AreEqual(0, SubstatRoll.Roll(G.Defs, rng, 0).Count);
        }

        [Test]
        public void 생성자는_null_을_거부하고_기술트리_없이도_선다()
        {
            Assert.Throws<ArgumentNullException>(() => new ForgeEngine(null, new ForgeState(), new Wallet(), Rng.Mulberry(1), () => 0));
            Assert.Throws<ArgumentNullException>(() => new ForgeEngine(G, null, new Wallet(), Rng.Mulberry(1), () => 0));
            Assert.Throws<ArgumentNullException>(() => new ForgeEngine(G, new ForgeState(), null, Rng.Mulberry(1), () => 0));
            Assert.Throws<ArgumentNullException>(() => new ForgeEngine(G, new ForgeState(), new Wallet(), null, () => 0));
            Assert.Throws<ArgumentNullException>(() => new ForgeEngine(G, new ForgeState(), new Wallet(), Rng.Mulberry(1), null));
            var f = new ForgeEngine(G, new ForgeState(), new Wallet(), Rng.Mulberry(1), () => 0);
            Assert.AreEqual(100, f.MaxItemLevel());
            Assert.AreEqual(400, f.UpgradeCost(f.UpgradeInfo()));
            Assert.AreEqual(300, f.UpgradeTime(f.UpgradeInfo()));
        }
    }

    /// <summary>정본 forge.js 에서 뽑은 대조 벡터(JSON · 25 블록). 다시 뽑는 법은 PROGRESS T14 완료 기록.</summary>
    static class ForgeVectors
    {
        public const string Json = @"{""consts"":{""MAX_LEVEL"":35,""TIER_BASE_ATK"":12,""TIER_BASE_HP"":70,""TIER_STEP"":6,""ATK_SLOTS"":4,""HP_SLOTS"":4,""LEVEL_STEP"":1.01,""ROLL_BASE_CAP"":100,""ROLL_UP_PCT"":70,""ROLL_SAME_PCT"":20},""tier"":[{""ageIdx"":0,""atk"":12,""hp"":70},{""ageIdx"":1,""atk"":72,""hp"":420},{""ageIdx"":2,""atk"":432,""hp"":2520},{""ageIdx"":3,""atk"":2592,""hp"":15120},{""ageIdx"":4,""atk"":15552,""hp"":90720},{""ageIdx"":5,""atk"":93312,""hp"":544320},{""ageIdx"":6,""atk"":559872,""hp"":3265920},{""ageIdx"":7,""atk"":3359232,""hp"":19595520},{""ageIdx"":8,""atk"":20155392,""hp"":117573120},{""ageIdx"":9,""atk"":120932352,""hp"":705438720}],""levelMult"":[{""level"":1,""mult"":1},{""level"":2,""mult"":1.01},{""level"":10,""mult"":1.0936852726843609},{""level"":35,""mult"":1.4025769861695725},{""level"":100,""mult"":2.678033494476761},{""level"":110,""mult"":2.958215050591315},{""level"":0,""mult"":1}],""rarityAxis"":[{""rarity"":""common"",""age"":0,""sumAtk1"":48,""sumHp1"":280,""sumAtk50"":78.16072024604591,""sumHp50"":455.93753476860115},{""rarity"":""rare"",""age"":1.8,""sumAtk1"":1207.573261237289,""sumHp1"":7044.177357217519,""sumAtk50"":1966.3499135036054,""sumHp50"":11470.374495437698},{""rarity"":""epic"",""age"":3.6,""sumAtk1"":30379.857942817955,""sumHp1"":177215.8379997714,""sumAtk50"":49468.99120381688,""sumHp50"":288569.11535559845},{""rarity"":""legendary"",""age"":5.4,""sumAtk1"":764289.669415297,""sumHp1"":4458356.404922565,""sumAtk50"":1244529.8132939988,""sumHp50"":7259757.244214992},{""rarity"":""ultimate"",""age"":7.2,""sumAtk1"":19227828.513037484,""sumHp1"":112162332.99271865,""sumAtk50"":31309602.611384746,""sumHp50"":182639348.566411},{""rarity"":""mythic"",""age"":9,""sumAtk1"":483729408,""sumHp1"":2821754880,""sumAtk50"":787679977.7806959,""sumHp50"":4594799870.387393}],""upgrades"":[{""level"":1,""next"":{""cost"":400,""time"":300},""cost0"":400,""time0"":300,""costMod"":320,""timeMod"":200,""costFloor"":40},{""level"":2,""next"":{""cost"":700,""time"":900},""cost0"":700,""time0"":900,""costMod"":560,""timeMod"":600,""costFloor"":70},{""level"":3,""next"":{""cost"":1500,""time"":1800},""cost0"":1500,""time0"":1800,""costMod"":1200,""timeMod"":1200,""costFloor"":150},{""level"":4,""next"":{""cost"":3500,""time"":3600},""cost0"":3500,""time0"":3600,""costMod"":2800,""timeMod"":2400,""costFloor"":350},{""level"":5,""next"":{""cost"":10000,""time"":7140},""cost0"":10000,""time0"":7140,""costMod"":8000,""timeMod"":4760,""costFloor"":1000},{""level"":6,""next"":{""cost"":25000,""time"":27180},""cost0"":25000,""time0"":27180,""costMod"":20000,""timeMod"":18120,""costFloor"":2500},{""level"":7,""next"":{""cost"":50000,""time"":47160},""cost0"":50000,""time0"":47160,""costMod"":40000,""timeMod"":31440,""costFloor"":5000},{""level"":8,""next"":{""cost"":99900,""time"":67140},""cost0"":99900,""time0"":67140,""costMod"":79920,""timeMod"":44760,""costFloor"":9990},{""level"":9,""next"":{""cost"":150000,""time"":87180},""cost0"":150000,""time0"":87180,""costMod"":120000,""timeMod"":58120,""costFloor"":15000},{""level"":10,""next"":{""cost"":249900,""time"":126000},""cost0"":249900,""time0"":126000,""costMod"":199920,""timeMod"":84000,""costFloor"":24990},{""level"":11,""next"":{""cost"":336000,""time"":176400},""cost0"":336000,""time0"":176400,""costMod"":268800,""timeMod"":117600,""costFloor"":33600},{""level"":12,""next"":{""cost"":452000,""time"":234000},""cost0"":452000,""time0"":234000,""costMod"":361600,""timeMod"":156000,""costFloor"":45200},{""level"":13,""next"":{""cost"":612000,""time"":306000},""cost0"":612000,""time0"":306000,""costMod"":489600,""timeMod"":204000,""costFloor"":61200},{""level"":14,""next"":{""cost"":830000,""time"":385200},""cost0"":830000,""time0"":385200,""costMod"":664000,""timeMod"":256800,""costFloor"":83000},{""level"":15,""next"":{""cost"":1120000,""time"":464400},""cost0"":1120000,""time0"":464400,""costMod"":896000,""timeMod"":309600,""costFloor"":112000},{""level"":16,""next"":{""cost"":1510000,""time"":543600},""cost0"":1510000,""time0"":543600,""costMod"":1208000,""timeMod"":362400,""costFloor"":151000},{""level"":17,""next"":{""cost"":2040000,""time"":626400},""cost0"":2040000,""time0"":626400,""costMod"":1632000,""timeMod"":417600,""costFloor"":204000},{""level"":18,""next"":{""cost"":2750000,""time"":705600},""cost0"":2750000,""time0"":705600,""costMod"":2200000,""timeMod"":470400,""costFloor"":275000},{""level"":19,""next"":{""cost"":3700000,""time"":784800},""cost0"":3700000,""time0"":784800,""costMod"":2960000,""timeMod"":523200,""costFloor"":370000},{""level"":20,""next"":{""cost"":5020000,""time"":867180},""cost0"":5020000,""time0"":867180,""costMod"":4016000,""timeMod"":578120,""costFloor"":502000},{""level"":21,""next"":{""cost"":6780000,""time"":946800},""cost0"":6780000,""time0"":946800,""costMod"":5424000,""timeMod"":631200,""costFloor"":678000},{""level"":22,""next"":{""cost"":9160000,""time"":1026000},""cost0"":9160000,""time0"":1026000,""costMod"":7328000,""timeMod"":684000,""costFloor"":916000},{""level"":23,""next"":{""cost"":12300000,""time"":1105200},""cost0"":12300000,""time0"":1105200,""costMod"":9840000,""timeMod"":736800,""costFloor"":1230000},{""level"":24,""next"":{""cost"":16600000,""time"":1184400},""cost0"":16600000,""time0"":1184400,""costMod"":13280000,""timeMod"":789600,""costFloor"":1660000},{""level"":25,""next"":{""cost"":20000000,""time"":1263600},""cost0"":20000000,""time0"":1263600,""costMod"":16000000,""timeMod"":842400,""costFloor"":2000000},{""level"":26,""next"":{""cost"":24000000,""time"":1346400},""cost0"":24000000,""time0"":1346400,""costMod"":19200000,""timeMod"":897600,""costFloor"":2400000},{""level"":27,""next"":{""cost"":28800000,""time"":1425600},""cost0"":28800000,""time0"":1425600,""costMod"":23040000,""timeMod"":950400,""costFloor"":2880000},{""level"":28,""next"":{""cost"":34600000,""time"":1504800},""cost0"":34600000,""time0"":1504800,""costMod"":27680000,""timeMod"":1003200,""costFloor"":3460000},{""level"":29,""next"":{""cost"":41500000,""time"":1584000},""cost0"":41500000,""time0"":1584000,""costMod"":33200000,""timeMod"":1056000,""costFloor"":4150000},{""level"":30,""next"":{""cost"":49800000,""time"":1666800},""cost0"":49800000,""time0"":1666800,""costMod"":39840000,""timeMod"":1111200,""costFloor"":4980000},{""level"":31,""next"":{""cost"":59800000,""time"":1746000},""cost0"":59800000,""time0"":1746000,""costMod"":47840000,""timeMod"":1164000,""costFloor"":5980000},{""level"":32,""next"":{""cost"":71700000,""time"":1825200},""cost0"":71700000,""time0"":1825200,""costMod"":57360000,""timeMod"":1216800,""costFloor"":7170000},{""level"":33,""next"":{""cost"":86100000,""time"":1904400},""cost0"":86100000,""time0"":1904400,""costMod"":68880000,""timeMod"":1269600,""costFloor"":8610000},{""level"":34,""next"":{""cost"":103000000,""time"":1987200},""cost0"":103000000,""time0"":1987200,""costMod"":82400000,""timeMod"":1324800,""costFloor"":10300000},{""level"":35,""next"":null},{""level"":36,""next"":null}],""gemSkip"":[{""remainMs"":0,""gems"":0},{""remainMs"":1,""gems"":1},{""remainMs"":59999,""gems"":1},{""remainMs"":60000,""gems"":1},{""remainMs"":599999,""gems"":1},{""remainMs"":600000,""gems"":1},{""remainMs"":600001,""gems"":2},{""remainMs"":1234567,""gems"":3},{""remainMs"":86400000,""gems"":144},{""remainMs"":1987200000,""gems"":3312}],""gemSkipIdle"":0,""rarityWeights"":[{""fl"":1,""w"":{""common"":60,""rare"":22.3,""epic"":9.35,""legendary"":3.22,""ultimate"":0.7,""mythic"":0.12}},{""fl"":2,""w"":{""common"":60,""rare"":22.6,""epic"":9.7,""legendary"":3.44,""ultimate"":0.8,""mythic"":0.16}},{""fl"":3,""w"":{""common"":60,""rare"":22.9,""epic"":10.05,""legendary"":3.66,""ultimate"":0.9,""mythic"":0.2}},{""fl"":4,""w"":{""common"":60,""rare"":23.2,""epic"":10.4,""legendary"":3.88,""ultimate"":1,""mythic"":0.24}},{""fl"":5,""w"":{""common"":60,""rare"":23.5,""epic"":10.75,""legendary"":4.1,""ultimate"":1.1,""mythic"":0.28}},{""fl"":6,""w"":{""common"":60,""rare"":23.8,""epic"":11.1,""legendary"":4.32,""ultimate"":1.2000000000000002,""mythic"":0.32}},{""fl"":7,""w"":{""common"":60,""rare"":24.1,""epic"":11.45,""legendary"":4.54,""ultimate"":1.3,""mythic"":0.36000000000000004}},{""fl"":8,""w"":{""common"":60,""rare"":24.4,""epic"":11.8,""legendary"":4.76,""ultimate"":1.4,""mythic"":0.4}},{""fl"":9,""w"":{""common"":60,""rare"":24.7,""epic"":12.15,""legendary"":4.98,""ultimate"":1.5,""mythic"":0.44}},{""fl"":10,""w"":{""common"":60,""rare"":25,""epic"":12.5,""legendary"":5.2,""ultimate"":1.6,""mythic"":0.48000000000000004}},{""fl"":11,""w"":{""common"":60,""rare"":25.3,""epic"":12.85,""legendary"":5.42,""ultimate"":1.7000000000000002,""mythic"":0.52}},{""fl"":12,""w"":{""common"":60,""rare"":25.6,""epic"":13.2,""legendary"":5.640000000000001,""ultimate"":1.8000000000000003,""mythic"":0.5599999999999999}},{""fl"":13,""w"":{""common"":60,""rare"":25.9,""epic"":13.55,""legendary"":5.859999999999999,""ultimate"":1.9,""mythic"":0.6}},{""fl"":14,""w"":{""common"":60,""rare"":26.2,""epic"":13.899999999999999,""legendary"":6.08,""ultimate"":2,""mythic"":0.64}},{""fl"":15,""w"":{""common"":60,""rare"":26.5,""epic"":14.25,""legendary"":6.3,""ultimate"":2.1,""mythic"":0.6799999999999999}},{""fl"":16,""w"":{""common"":60,""rare"":26.8,""epic"":14.6,""legendary"":6.52,""ultimate"":2.2,""mythic"":0.72}},{""fl"":17,""w"":{""common"":60,""rare"":27.1,""epic"":14.95,""legendary"":6.74,""ultimate"":2.3000000000000003,""mythic"":0.76}},{""fl"":18,""w"":{""common"":60,""rare"":27.4,""epic"":15.3,""legendary"":6.96,""ultimate"":2.4,""mythic"":0.7999999999999999}},{""fl"":19,""w"":{""common"":60,""rare"":27.7,""epic"":15.649999999999999,""legendary"":7.18,""ultimate"":2.5,""mythic"":0.84}},{""fl"":20,""w"":{""common"":60,""rare"":28,""epic"":16,""legendary"":7.4,""ultimate"":2.6,""mythic"":0.88}},{""fl"":21,""w"":{""common"":60,""rare"":28.3,""epic"":16.35,""legendary"":7.62,""ultimate"":2.7,""mythic"":0.9199999999999999}},{""fl"":22,""w"":{""common"":60,""rare"":28.6,""epic"":16.7,""legendary"":7.84,""ultimate"":2.8000000000000003,""mythic"":0.96}},{""fl"":23,""w"":{""common"":60,""rare"":28.9,""epic"":17.049999999999997,""legendary"":8.059999999999999,""ultimate"":2.9000000000000004,""mythic"":1}},{""fl"":24,""w"":{""common"":60,""rare"":29.2,""epic"":17.4,""legendary"":8.280000000000001,""ultimate"":3.0000000000000004,""mythic"":1.04}},{""fl"":25,""w"":{""common"":60,""rare"":29.5,""epic"":17.75,""legendary"":8.5,""ultimate"":3.1,""mythic"":1.08}},{""fl"":26,""w"":{""common"":60,""rare"":29.8,""epic"":18.1,""legendary"":8.719999999999999,""ultimate"":3.2,""mythic"":1.12}},{""fl"":27,""w"":{""common"":60,""rare"":30.1,""epic"":18.45,""legendary"":8.940000000000001,""ultimate"":3.3000000000000003,""mythic"":1.1600000000000001}},{""fl"":28,""w"":{""common"":60,""rare"":30.4,""epic"":18.799999999999997,""legendary"":9.16,""ultimate"":3.4000000000000004,""mythic"":1.2000000000000002}},{""fl"":29,""w"":{""common"":60,""rare"":30.7,""epic"":19.15,""legendary"":9.379999999999999,""ultimate"":3.5000000000000004,""mythic"":1.24}},{""fl"":30,""w"":{""common"":60,""rare"":31,""epic"":19.5,""legendary"":9.6,""ultimate"":3.6,""mythic"":1.28}},{""fl"":31,""w"":{""common"":60,""rare"":31.299999999999997,""epic"":19.85,""legendary"":9.82,""ultimate"":3.7,""mythic"":1.32}},{""fl"":32,""w"":{""common"":60,""rare"":31.6,""epic"":20.2,""legendary"":10.04,""ultimate"":3.8000000000000003,""mythic"":1.36}},{""fl"":33,""w"":{""common"":60,""rare"":31.9,""epic"":20.549999999999997,""legendary"":10.26,""ultimate"":3.9000000000000004,""mythic"":1.4000000000000001}},{""fl"":34,""w"":{""common"":60,""rare"":32.2,""epic"":20.9,""legendary"":10.48,""ultimate"":4,""mythic"":1.4400000000000002}},{""fl"":35,""w"":{""common"":60,""rare"":32.5,""epic"":21.25,""legendary"":10.7,""ultimate"":4.1,""mythic"":1.4800000000000002}}],""ageProbsFallback"":{""at0"":{""primitive"":100},""at36"":{""primitive"":100},""at35"":{""quantum"":62,""underworld"":36,""divine"":2}},""probSums"":[{""level"":1,""sum"":100},{""level"":2,""sum"":100},{""level"":3,""sum"":100},{""level"":4,""sum"":100},{""level"":5,""sum"":100},{""level"":6,""sum"":100},{""level"":7,""sum"":100},{""level"":8,""sum"":100},{""level"":9,""sum"":100},{""level"":10,""sum"":100},{""level"":11,""sum"":100},{""level"":12,""sum"":100},{""level"":13,""sum"":100},{""level"":14,""sum"":100.05},{""level"":15,""sum"":99.95},{""level"":16,""sum"":100},{""level"":17,""sum"":99.95},{""level"":18,""sum"":99.95},{""level"":19,""sum"":100},{""level"":20,""sum"":100.05},{""level"":21,""sum"":99.95},{""level"":22,""sum"":100},{""level"":23,""sum"":100},{""level"":24,""sum"":100.01},{""level"":25,""sum"":100.05},{""level"":26,""sum"":100.05},{""level"":27,""sum"":100},{""level"":28,""sum"":100},{""level"":29,""sum"":100.00999999999999},{""level"":30,""sum"":100.01},{""level"":31,""sum"":99.95},{""level"":32,""sum"":99.95},{""level"":33,""sum"":100},{""level"":34,""sum"":100},{""level"":35,""sum"":100}],""variantCount"":[{""age"":""primitive"",""weapon"":5,""helmet"":5,""armor"":5,""ring"":5,""belt"":5},{""age"":""medieval"",""weapon"":7,""helmet"":5,""armor"":5,""ring"":5,""belt"":5},{""age"":""earlyModern"",""weapon"":6,""helmet"":5,""armor"":5,""ring"":5,""belt"":5},{""age"":""modern"",""weapon"":5,""helmet"":7,""armor"":5,""ring"":5,""belt"":5},{""age"":""space"",""weapon"":5,""helmet"":5,""armor"":5,""ring"":5,""belt"":5},{""age"":""interstellar"",""weapon"":5,""helmet"":6,""armor"":5,""ring"":5,""belt"":5},{""age"":""multiverse"",""weapon"":5,""helmet"":5,""armor"":5,""ring"":5,""belt"":5},{""age"":""quantum"",""weapon"":5,""helmet"":5,""armor"":5,""ring"":5,""belt"":5},{""age"":""underworld"",""weapon"":5,""helmet"":5,""armor"":5,""ring"":5,""belt"":5},{""age"":""divine"",""weapon"":5,""helmet"":5,""armor"":5,""ring"":5,""belt"":5}],""itemDropChance"":[{""fl"":1,""rows"":[{""age"":""primitive"",""slot"":""weapon"",""pct"":2.5},{""age"":""primitive"",""slot"":""helmet"",""pct"":2.5},{""age"":""primitive"",""slot"":""armor"",""pct"":2.5},{""age"":""primitive"",""slot"":""gloves"",""pct"":2.5},{""age"":""primitive"",""slot"":""necklace"",""pct"":2.5},{""age"":""primitive"",""slot"":""ring"",""pct"":2.5},{""age"":""primitive"",""slot"":""shoes"",""pct"":2.5},{""age"":""primitive"",""slot"":""belt"",""pct"":2.5},{""age"":""medieval"",""slot"":""weapon"",""pct"":0},{""age"":""medieval"",""slot"":""helmet"",""pct"":0},{""age"":""medieval"",""slot"":""armor"",""pct"":0},{""age"":""medieval"",""slot"":""gloves"",""pct"":0},{""age"":""medieval"",""slot"":""necklace"",""pct"":0},{""age"":""medieval"",""slot"":""ring"",""pct"":0},{""age"":""medieval"",""slot"":""shoes"",""pct"":0},{""age"":""medieval"",""slot"":""belt"",""pct"":0},{""age"":""earlyModern"",""slot"":""weapon"",""pct"":0},{""age"":""earlyModern"",""slot"":""helmet"",""pct"":0},{""age"":""earlyModern"",""slot"":""armor"",""pct"":0},{""age"":""earlyModern"",""slot"":""gloves"",""pct"":0},{""age"":""earlyModern"",""slot"":""necklace"",""pct"":0},{""age"":""earlyModern"",""slot"":""ring"",""pct"":0},{""age"":""earlyModern"",""slot"":""shoes"",""pct"":0},{""age"":""earlyModern"",""slot"":""belt"",""pct"":0},{""age"":""modern"",""slot"":""weapon"",""pct"":0},{""age"":""modern"",""slot"":""helmet"",""pct"":0},{""age"":""modern"",""slot"":""armor"",""pct"":0},{""age"":""modern"",""slot"":""gloves"",""pct"":0},{""age"":""modern"",""slot"":""necklace"",""pct"":0},{""age"":""modern"",""slot"":""ring"",""pct"":0},{""age"":""modern"",""slot"":""shoes"",""pct"":0},{""age"":""modern"",""slot"":""belt"",""pct"":0},{""age"":""space"",""slot"":""weapon"",""pct"":0},{""age"":""space"",""slot"":""helmet"",""pct"":0},{""age"":""space"",""slot"":""armor"",""pct"":0},{""age"":""space"",""slot"":""gloves"",""pct"":0},{""age"":""space"",""slot"":""necklace"",""pct"":0},{""age"":""space"",""slot"":""ring"",""pct"":0},{""age"":""space"",""slot"":""shoes"",""pct"":0},{""age"":""space"",""slot"":""belt"",""pct"":0},{""age"":""interstellar"",""slot"":""weapon"",""pct"":0},{""age"":""interstellar"",""slot"":""helmet"",""pct"":0},{""age"":""interstellar"",""slot"":""armor"",""pct"":0},{""age"":""interstellar"",""slot"":""gloves"",""pct"":0},{""age"":""interstellar"",""slot"":""necklace"",""pct"":0},{""age"":""interstellar"",""slot"":""ring"",""pct"":0},{""age"":""interstellar"",""slot"":""shoes"",""pct"":0},{""age"":""interstellar"",""slot"":""belt"",""pct"":0},{""age"":""multiverse"",""slot"":""weapon"",""pct"":0},{""age"":""multiverse"",""slot"":""helmet"",""pct"":0},{""age"":""multiverse"",""slot"":""armor"",""pct"":0},{""age"":""multiverse"",""slot"":""gloves"",""pct"":0},{""age"":""multiverse"",""slot"":""necklace"",""pct"":0},{""age"":""multiverse"",""slot"":""ring"",""pct"":0},{""age"":""multiverse"",""slot"":""shoes"",""pct"":0},{""age"":""multiverse"",""slot"":""belt"",""pct"":0},{""age"":""quantum"",""slot"":""weapon"",""pct"":0},{""age"":""quantum"",""slot"":""helmet"",""pct"":0},{""age"":""quantum"",""slot"":""armor"",""pct"":0},{""age"":""quantum"",""slot"":""gloves"",""pct"":0},{""age"":""quantum"",""slot"":""necklace"",""pct"":0},{""age"":""quantum"",""slot"":""ring"",""pct"":0},{""age"":""quantum"",""slot"":""shoes"",""pct"":0},{""age"":""quantum"",""slot"":""belt"",""pct"":0},{""age"":""underworld"",""slot"":""weapon"",""pct"":0},{""age"":""underworld"",""slot"":""helmet"",""pct"":0},{""age"":""underworld"",""slot"":""armor"",""pct"":0},{""age"":""underworld"",""slot"":""gloves"",""pct"":0},{""age"":""underworld"",""slot"":""necklace"",""pct"":0},{""age"":""underworld"",""slot"":""ring"",""pct"":0},{""age"":""underworld"",""slot"":""shoes"",""pct"":0},{""age"":""underworld"",""slot"":""belt"",""pct"":0},{""age"":""divine"",""slot"":""weapon"",""pct"":0},{""age"":""divine"",""slot"":""helmet"",""pct"":0},{""age"":""divine"",""slot"":""armor"",""pct"":0},{""age"":""divine"",""slot"":""gloves"",""pct"":0},{""age"":""divine"",""slot"":""necklace"",""pct"":0},{""age"":""divine"",""slot"":""ring"",""pct"":0},{""age"":""divine"",""slot"":""shoes"",""pct"":0},{""age"":""divine"",""slot"":""belt"",""pct"":0}]},{""fl"":10,""rows"":[{""age"":""primitive"",""slot"":""weapon"",""pct"":0.15},{""age"":""primitive"",""slot"":""helmet"",""pct"":0.15},{""age"":""primitive"",""slot"":""armor"",""pct"":0.15},{""age"":""primitive"",""slot"":""gloves"",""pct"":0.15},{""age"":""primitive"",""slot"":""necklace"",""pct"":0.15},{""age"":""primitive"",""slot"":""ring"",""pct"":0.15},{""age"":""primitive"",""slot"":""shoes"",""pct"":0.15},{""age"":""primitive"",""slot"":""belt"",""pct"":0.15},{""age"":""medieval"",""slot"":""weapon"",""pct"":1.0714285714285712},{""age"":""medieval"",""slot"":""helmet"",""pct"":1.5},{""age"":""medieval"",""slot"":""armor"",""pct"":1.5},{""age"":""medieval"",""slot"":""gloves"",""pct"":1.5},{""age"":""medieval"",""slot"":""necklace"",""pct"":1.5},{""age"":""medieval"",""slot"":""ring"",""pct"":1.5},{""age"":""medieval"",""slot"":""shoes"",""pct"":1.5},{""age"":""medieval"",""slot"":""belt"",""pct"":1.5},{""age"":""earlyModern"",""slot"":""weapon"",""pct"":0.6666666666666666},{""age"":""earlyModern"",""slot"":""helmet"",""pct"":0.8},{""age"":""earlyModern"",""slot"":""armor"",""pct"":0.8},{""age"":""earlyModern"",""slot"":""gloves"",""pct"":0.8},{""age"":""earlyModern"",""slot"":""necklace"",""pct"":0.8},{""age"":""earlyModern"",""slot"":""ring"",""pct"":0.8},{""age"":""earlyModern"",""slot"":""shoes"",""pct"":0.8},{""age"":""earlyModern"",""slot"":""belt"",""pct"":0.8},{""age"":""modern"",""slot"":""weapon"",""pct"":0.05},{""age"":""modern"",""slot"":""helmet"",""pct"":0.03571428571428571},{""age"":""modern"",""slot"":""armor"",""pct"":0.05},{""age"":""modern"",""slot"":""gloves"",""pct"":0.05},{""age"":""modern"",""slot"":""necklace"",""pct"":0.05},{""age"":""modern"",""slot"":""ring"",""pct"":0.05},{""age"":""modern"",""slot"":""shoes"",""pct"":0.05},{""age"":""modern"",""slot"":""belt"",""pct"":0.05},{""age"":""space"",""slot"":""weapon"",""pct"":0},{""age"":""space"",""slot"":""helmet"",""pct"":0},{""age"":""space"",""slot"":""armor"",""pct"":0},{""age"":""space"",""slot"":""gloves"",""pct"":0},{""age"":""space"",""slot"":""necklace"",""pct"":0},{""age"":""space"",""slot"":""ring"",""pct"":0},{""age"":""space"",""slot"":""shoes"",""pct"":0},{""age"":""space"",""slot"":""belt"",""pct"":0},{""age"":""interstellar"",""slot"":""weapon"",""pct"":0},{""age"":""interstellar"",""slot"":""helmet"",""pct"":0},{""age"":""interstellar"",""slot"":""armor"",""pct"":0},{""age"":""interstellar"",""slot"":""gloves"",""pct"":0},{""age"":""interstellar"",""slot"":""necklace"",""pct"":0},{""age"":""interstellar"",""slot"":""ring"",""pct"":0},{""age"":""interstellar"",""slot"":""shoes"",""pct"":0},{""age"":""interstellar"",""slot"":""belt"",""pct"":0},{""age"":""multiverse"",""slot"":""weapon"",""pct"":0},{""age"":""multiverse"",""slot"":""helmet"",""pct"":0},{""age"":""multiverse"",""slot"":""armor"",""pct"":0},{""age"":""multiverse"",""slot"":""gloves"",""pct"":0},{""age"":""multiverse"",""slot"":""necklace"",""pct"":0},{""age"":""multiverse"",""slot"":""ring"",""pct"":0},{""age"":""multiverse"",""slot"":""shoes"",""pct"":0},{""age"":""multiverse"",""slot"":""belt"",""pct"":0},{""age"":""quantum"",""slot"":""weapon"",""pct"":0},{""age"":""quantum"",""slot"":""helmet"",""pct"":0},{""age"":""quantum"",""slot"":""armor"",""pct"":0},{""age"":""quantum"",""slot"":""gloves"",""pct"":0},{""age"":""quantum"",""slot"":""necklace"",""pct"":0},{""age"":""quantum"",""slot"":""ring"",""pct"":0},{""age"":""quantum"",""slot"":""shoes"",""pct"":0},{""age"":""quantum"",""slot"":""belt"",""pct"":0},{""age"":""underworld"",""slot"":""weapon"",""pct"":0},{""age"":""underworld"",""slot"":""helmet"",""pct"":0},{""age"":""underworld"",""slot"":""armor"",""pct"":0},{""age"":""underworld"",""slot"":""gloves"",""pct"":0},{""age"":""underworld"",""slot"":""necklace"",""pct"":0},{""age"":""underworld"",""slot"":""ring"",""pct"":0},{""age"":""underworld"",""slot"":""shoes"",""pct"":0},{""age"":""underworld"",""slot"":""belt"",""pct"":0},{""age"":""divine"",""slot"":""weapon"",""pct"":0},{""age"":""divine"",""slot"":""helmet"",""pct"":0},{""age"":""divine"",""slot"":""armor"",""pct"":0},{""age"":""divine"",""slot"":""gloves"",""pct"":0},{""age"":""divine"",""slot"":""necklace"",""pct"":0},{""age"":""divine"",""slot"":""ring"",""pct"":0},{""age"":""divine"",""slot"":""shoes"",""pct"":0},{""age"":""divine"",""slot"":""belt"",""pct"":0}]},{""fl"":29,""rows"":[{""age"":""primitive"",""slot"":""weapon"",""pct"":0},{""age"":""primitive"",""slot"":""helmet"",""pct"":0},{""age"":""primitive"",""slot"":""armor"",""pct"":0},{""age"":""primitive"",""slot"":""gloves"",""pct"":0},{""age"":""primitive"",""slot"":""necklace"",""pct"":0},{""age"":""primitive"",""slot"":""ring"",""pct"":0},{""age"":""primitive"",""slot"":""shoes"",""pct"":0},{""age"":""primitive"",""slot"":""belt"",""pct"":0},{""age"":""medieval"",""slot"":""weapon"",""pct"":0},{""age"":""medieval"",""slot"":""helmet"",""pct"":0},{""age"":""medieval"",""slot"":""armor"",""pct"":0},{""age"":""medieval"",""slot"":""gloves"",""pct"":0},{""age"":""medieval"",""slot"":""necklace"",""pct"":0},{""age"":""medieval"",""slot"":""ring"",""pct"":0},{""age"":""medieval"",""slot"":""shoes"",""pct"":0},{""age"":""medieval"",""slot"":""belt"",""pct"":0},{""age"":""earlyModern"",""slot"":""weapon"",""pct"":0},{""age"":""earlyModern"",""slot"":""helmet"",""pct"":0},{""age"":""earlyModern"",""slot"":""armor"",""pct"":0},{""age"":""earlyModern"",""slot"":""gloves"",""pct"":0},{""age"":""earlyModern"",""slot"":""necklace"",""pct"":0},{""age"":""earlyModern"",""slot"":""ring"",""pct"":0},{""age"":""earlyModern"",""slot"":""shoes"",""pct"":0},{""age"":""earlyModern"",""slot"":""belt"",""pct"":0},{""age"":""modern"",""slot"":""weapon"",""pct"":0},{""age"":""modern"",""slot"":""helmet"",""pct"":0},{""age"":""modern"",""slot"":""armor"",""pct"":0},{""age"":""modern"",""slot"":""gloves"",""pct"":0},{""age"":""modern"",""slot"":""necklace"",""pct"":0},{""age"":""modern"",""slot"":""ring"",""pct"":0},{""age"":""modern"",""slot"":""shoes"",""pct"":0},{""age"":""modern"",""slot"":""belt"",""pct"":0},{""age"":""space"",""slot"":""weapon"",""pct"":0},{""age"":""space"",""slot"":""helmet"",""pct"":0},{""age"":""space"",""slot"":""armor"",""pct"":0},{""age"":""space"",""slot"":""gloves"",""pct"":0},{""age"":""space"",""slot"":""necklace"",""pct"":0},{""age"":""space"",""slot"":""ring"",""pct"":0},{""age"":""space"",""slot"":""shoes"",""pct"":0},{""age"":""space"",""slot"":""belt"",""pct"":0},{""age"":""interstellar"",""slot"":""weapon"",""pct"":0.17475000000000002},{""age"":""interstellar"",""slot"":""helmet"",""pct"":0.145625},{""age"":""interstellar"",""slot"":""armor"",""pct"":0.17475000000000002},{""age"":""interstellar"",""slot"":""gloves"",""pct"":0.17475000000000002},{""age"":""interstellar"",""slot"":""necklace"",""pct"":0.17475000000000002},{""age"":""interstellar"",""slot"":""ring"",""pct"":0.17475000000000002},{""age"":""interstellar"",""slot"":""shoes"",""pct"":0.17475000000000002},{""age"":""interstellar"",""slot"":""belt"",""pct"":0.17475000000000002},{""age"":""multiverse"",""slot"":""weapon"",""pct"":1.7000000000000002},{""age"":""multiverse"",""slot"":""helmet"",""pct"":1.7000000000000002},{""age"":""multiverse"",""slot"":""armor"",""pct"":1.7000000000000002},{""age"":""multiverse"",""slot"":""gloves"",""pct"":1.7000000000000002},{""age"":""multiverse"",""slot"":""necklace"",""pct"":1.7000000000000002},{""age"":""multiverse"",""slot"":""ring"",""pct"":1.7000000000000002},{""age"":""multiverse"",""slot"":""shoes"",""pct"":1.7000000000000002},{""age"":""multiverse"",""slot"":""belt"",""pct"":1.7000000000000002},{""age"":""quantum"",""slot"":""weapon"",""pct"":0.5750000000000001},{""age"":""quantum"",""slot"":""helmet"",""pct"":0.5750000000000001},{""age"":""quantum"",""slot"":""armor"",""pct"":0.5750000000000001},{""age"":""quantum"",""slot"":""gloves"",""pct"":0.5750000000000001},{""age"":""quantum"",""slot"":""necklace"",""pct"":0.5750000000000001},{""age"":""quantum"",""slot"":""ring"",""pct"":0.5750000000000001},{""age"":""quantum"",""slot"":""shoes"",""pct"":0.5750000000000001},{""age"":""quantum"",""slot"":""belt"",""pct"":0.5750000000000001},{""age"":""underworld"",""slot"":""weapon"",""pct"":0.05},{""age"":""underworld"",""slot"":""helmet"",""pct"":0.05},{""age"":""underworld"",""slot"":""armor"",""pct"":0.05},{""age"":""underworld"",""slot"":""gloves"",""pct"":0.05},{""age"":""underworld"",""slot"":""necklace"",""pct"":0.05},{""age"":""underworld"",""slot"":""ring"",""pct"":0.05},{""age"":""underworld"",""slot"":""shoes"",""pct"":0.05},{""age"":""underworld"",""slot"":""belt"",""pct"":0.05},{""age"":""divine"",""slot"":""weapon"",""pct"":0.0005},{""age"":""divine"",""slot"":""helmet"",""pct"":0.0005},{""age"":""divine"",""slot"":""armor"",""pct"":0.0005},{""age"":""divine"",""slot"":""gloves"",""pct"":0.0005},{""age"":""divine"",""slot"":""necklace"",""pct"":0.0005},{""age"":""divine"",""slot"":""ring"",""pct"":0.0005},{""age"":""divine"",""slot"":""shoes"",""pct"":0.0005},{""age"":""divine"",""slot"":""belt"",""pct"":0.0005}]},{""fl"":35,""rows"":[{""age"":""primitive"",""slot"":""weapon"",""pct"":0},{""age"":""primitive"",""slot"":""helmet"",""pct"":0},{""age"":""primitive"",""slot"":""armor"",""pct"":0},{""age"":""primitive"",""slot"":""gloves"",""pct"":0},{""age"":""primitive"",""slot"":""necklace"",""pct"":0},{""age"":""primitive"",""slot"":""ring"",""pct"":0},{""age"":""primitive"",""slot"":""shoes"",""pct"":0},{""age"":""primitive"",""slot"":""belt"",""pct"":0},{""age"":""medieval"",""slot"":""weapon"",""pct"":0},{""age"":""medieval"",""slot"":""helmet"",""pct"":0},{""age"":""medieval"",""slot"":""armor"",""pct"":0},{""age"":""medieval"",""slot"":""gloves"",""pct"":0},{""age"":""medieval"",""slot"":""necklace"",""pct"":0},{""age"":""medieval"",""slot"":""ring"",""pct"":0},{""age"":""medieval"",""slot"":""shoes"",""pct"":0},{""age"":""medieval"",""slot"":""belt"",""pct"":0},{""age"":""earlyModern"",""slot"":""weapon"",""pct"":0},{""age"":""earlyModern"",""slot"":""helmet"",""pct"":0},{""age"":""earlyModern"",""slot"":""armor"",""pct"":0},{""age"":""earlyModern"",""slot"":""gloves"",""pct"":0},{""age"":""earlyModern"",""slot"":""necklace"",""pct"":0},{""age"":""earlyModern"",""slot"":""ring"",""pct"":0},{""age"":""earlyModern"",""slot"":""shoes"",""pct"":0},{""age"":""earlyModern"",""slot"":""belt"",""pct"":0},{""age"":""modern"",""slot"":""weapon"",""pct"":0},{""age"":""modern"",""slot"":""helmet"",""pct"":0},{""age"":""modern"",""slot"":""armor"",""pct"":0},{""age"":""modern"",""slot"":""gloves"",""pct"":0},{""age"":""modern"",""slot"":""necklace"",""pct"":0},{""age"":""modern"",""slot"":""ring"",""pct"":0},{""age"":""modern"",""slot"":""shoes"",""pct"":0},{""age"":""modern"",""slot"":""belt"",""pct"":0},{""age"":""space"",""slot"":""weapon"",""pct"":0},{""age"":""space"",""slot"":""helmet"",""pct"":0},{""age"":""space"",""slot"":""armor"",""pct"":0},{""age"":""space"",""slot"":""gloves"",""pct"":0},{""age"":""space"",""slot"":""necklace"",""pct"":0},{""age"":""space"",""slot"":""ring"",""pct"":0},{""age"":""space"",""slot"":""shoes"",""pct"":0},{""age"":""space"",""slot"":""belt"",""pct"":0},{""age"":""interstellar"",""slot"":""weapon"",""pct"":0},{""age"":""interstellar"",""slot"":""helmet"",""pct"":0},{""age"":""interstellar"",""slot"":""armor"",""pct"":0},{""age"":""interstellar"",""slot"":""gloves"",""pct"":0},{""age"":""interstellar"",""slot"":""necklace"",""pct"":0},{""age"":""interstellar"",""slot"":""ring"",""pct"":0},{""age"":""interstellar"",""slot"":""shoes"",""pct"":0},{""age"":""interstellar"",""slot"":""belt"",""pct"":0},{""age"":""multiverse"",""slot"":""weapon"",""pct"":0},{""age"":""multiverse"",""slot"":""helmet"",""pct"":0},{""age"":""multiverse"",""slot"":""armor"",""pct"":0},{""age"":""multiverse"",""slot"":""gloves"",""pct"":0},{""age"":""multiverse"",""slot"":""necklace"",""pct"":0},{""age"":""multiverse"",""slot"":""ring"",""pct"":0},{""age"":""multiverse"",""slot"":""shoes"",""pct"":0},{""age"":""multiverse"",""slot"":""belt"",""pct"":0},{""age"":""quantum"",""slot"":""weapon"",""pct"":1.55},{""age"":""quantum"",""slot"":""helmet"",""pct"":1.55},{""age"":""quantum"",""slot"":""armor"",""pct"":1.55},{""age"":""quantum"",""slot"":""gloves"",""pct"":1.55},{""age"":""quantum"",""slot"":""necklace"",""pct"":1.55},{""age"":""quantum"",""slot"":""ring"",""pct"":1.55},{""age"":""quantum"",""slot"":""shoes"",""pct"":1.55},{""age"":""quantum"",""slot"":""belt"",""pct"":1.55},{""age"":""underworld"",""slot"":""weapon"",""pct"":0.8999999999999999},{""age"":""underworld"",""slot"":""helmet"",""pct"":0.8999999999999999},{""age"":""underworld"",""slot"":""armor"",""pct"":0.8999999999999999},{""age"":""underworld"",""slot"":""gloves"",""pct"":0.8999999999999999},{""age"":""underworld"",""slot"":""necklace"",""pct"":0.8999999999999999},{""age"":""underworld"",""slot"":""ring"",""pct"":0.8999999999999999},{""age"":""underworld"",""slot"":""shoes"",""pct"":0.8999999999999999},{""age"":""underworld"",""slot"":""belt"",""pct"":0.8999999999999999},{""age"":""divine"",""slot"":""weapon"",""pct"":0.05},{""age"":""divine"",""slot"":""helmet"",""pct"":0.05},{""age"":""divine"",""slot"":""armor"",""pct"":0.05},{""age"":""divine"",""slot"":""gloves"",""pct"":0.05},{""age"":""divine"",""slot"":""necklace"",""pct"":0.05},{""age"":""divine"",""slot"":""ring"",""pct"":0.05},{""age"":""divine"",""slot"":""shoes"",""pct"":0.05},{""age"":""divine"",""slot"":""belt"",""pct"":0.05}]}],""rollItems"":[{""seed"":1,""fl"":1,""stars"":0,""items"":[[""이중 사냥끈"",""belt"",""primitive"",0,""common"",1,""hp"",70,[[""hpRegen"",2.8]],null,3,0],[""가죽 손싸개"",""gloves"",""primitive"",0,""ultimate"",2,""atk"",55,[[""dblAtk"",5.7]],null,0,0],[""풀 엮은 신"",""shoes"",""primitive"",0,""common"",3,""hp"",71,[[""block"",1.2]],null,2,0],[""전투 페인트"",""helmet"",""primitive"",0,""rare"",4,""hp"",108,[[""meleeDmg"",38.2],[""block"",3.9]],null,2,0],[""털가죽 발싸개"",""shoes"",""primitive"",0,""ultimate"",5,""hp"",335,[[""lifesteal"",8.9]],null,1,0],[""깃털 장식"",""helmet"",""primitive"",0,""rare"",6,""hp"",110,[[""critCh"",7.8]],null,4,0],[""돌 너클"",""gloves"",""primitive"",0,""legendary"",7,""atk"",40,[[""rangedDmg"",14.3],[""hpPct"",10.7]],null,4,0],[""뼈 단검"",""weapon"",""primitive"",0,""epic"",7,""atk"",28,[[""hpRegen"",3.6],[""meleeDmg"",46.9],[""atkSpd"",7.5]],""boneDagger"",3,0],[""가면"",""helmet"",""primitive"",0,""common"",8,""hp"",75,[[""critDmg"",32.7]],null,1,0],[""몽둥이"",""weapon"",""primitive"",0,""common"",7,""atk"",12,[[""skillCd"",2.6]],""club"",0,0],[""수염"",""helmet"",""primitive"",0,""epic"",8,""hp"",165,[[""meleeDmg"",16.9],[""critDmg"",12.2]],null,0,0],[""투석구"",""weapon"",""primitive"",0,""rare"",9,""atk"",19,[[""dmgPct"",4],[""skillDmg"",8.3]],""sling"",4,0],[""해골 투구"",""helmet"",""primitive"",0,""epic"",10,""hp"",168,[[""hpPct"",5.6]],null,3,0],[""가죽 신"",""shoes"",""primitive"",0,""common"",11,""hp"",77,[[""skillDmg"",7.4]],null,0,0],[""사냥꾼 조끼"",""armor"",""primitive"",0,""common"",12,""hp"",78,[[""lifesteal"",18.3]],null,4,0],[""돌 너클"",""gloves"",""primitive"",0,""rare"",13,""atk"",20,[[""block"",2.1]],null,4,0],[""돌도끼"",""weapon"",""primitive"",0,""common"",14,""atk"",13,[[""skillDmg"",12.2]],""stoneAxe"",1,0],[""뼈 장식 띠"",""belt"",""primitive"",0,""rare"",15,""hp"",120,[[""dblAtk"",14],[""lifesteal"",5.7]],null,2,0],[""깃털 장식"",""helmet"",""primitive"",0,""rare"",16,""hp"",121,[[""rangedDmg"",1.4],[""hpRegen"",2.3]],null,4,0],[""조가비 펜던트"",""necklace"",""primitive"",0,""common"",17,""atk"",14,[[""hpPct"",8.6]],null,2,0]],""rollLevelAfter"":{""primitive"":18,""medieval"":1,""earlyModern"":1,""modern"":1,""space"":1,""interstellar"":1,""multiverse"":1,""quantum"":1,""underworld"":1,""divine"":1}},{""seed"":1,""fl"":10,""stars"":0,""items"":[[""이중 검대"",""belt"",""medieval"",1,""common"",1,""hp"",420,[[""hpRegen"",2.8]],null,3,0],[""사슬 장갑"",""gloves"",""medieval"",1,""ultimate"",2,""atk"",334,[[""dblAtk"",5.7]],null,0,0],[""그리브"",""shoes"",""medieval"",1,""common"",3,""hp"",428,[[""block"",1.2]],null,2,0],[""로마 투구"",""helmet"",""medieval"",1,""rare"",4,""hp"",649,[[""meleeDmg"",38.2],[""block"",3.9]],null,2,0],[""승마 부츠"",""shoes"",""earlyModern"",2,""ultimate"",1,""hp"",11592,[[""lifesteal"",8.9]],null,1,0],[""사신의 모자"",""helmet"",""medieval"",1,""rare"",5,""hp"",655,[[""critCh"",7.8]],null,4,0],[""철 너클"",""gloves"",""medieval"",1,""ultimate"",6,""atk"",348,[[""rangedDmg"",14.3],[""hpPct"",10.7]],null,4,0],[""사브르"",""weapon"",""earlyModern"",2,""legendary"",2,""atk"",1396,[[""hpRegen"",3.6],[""meleeDmg"",46.9],[""atkSpd"",7.5],[""rangedDmg"",14.3]],""sabre"",0,0],[""투척 도끼"",""weapon"",""earlyModern"",2,""common"",3,""atk"",440,[[""lifesteal"",13.5]],""thrown"",3,0],[""기사 장화"",""shoes"",""medieval"",1,""common"",6,""hp"",441,[[""block"",2.2]],null,4,0],[""성물 목걸이"",""necklace"",""medieval"",1,""common"",6,""atk"",75,[[""critDmg"",12.2]],null,0,0],[""석궁"",""weapon"",""medieval"",1,""rare"",7,""atk"",114,[[""dmgPct"",4],[""skillDmg"",8.3]],""crossbow"",6,0],[""슬라브 모자"",""helmet"",""earlyModern"",2,""epic"",4,""hp"",5711,[[""hpPct"",5.6]],null,3,0],[""버클 구두"",""shoes"",""earlyModern"",2,""common"",5,""hp"",2622,[[""skillDmg"",7.4]],null,0,0],[""성직자 로브"",""armor"",""medieval"",1,""common"",8,""hp"",450,[[""lifesteal"",18.3]],null,4,0],[""철 너클"",""gloves"",""medieval"",1,""rare"",9,""atk"",116,[[""block"",2.1]],null,4,0],[""레이피어"",""weapon"",""earlyModern"",2,""common"",6,""atk"",454,[[""skillDmg"",12.2]],""rapier"",1,0],[""문장 허리띠"",""belt"",""medieval"",1,""epic"",10,""hp"",1010,[[""dblAtk"",14],[""lifesteal"",5.7]],null,2,0],[""톱햇"",""helmet"",""earlyModern"",2,""rare"",7,""hp"",4012,[[""rangedDmg"",1.4],[""hpRegen"",2.3]],null,4,0],[""십자 펜던트"",""necklace"",""medieval"",1,""common"",11,""atk"",79,[[""hpPct"",8.6]],null,2,0]],""rollLevelAfter"":{""primitive"":1,""medieval"":12,""earlyModern"":8,""modern"":1,""space"":1,""interstellar"":1,""multiverse"":1,""quantum"":1,""underworld"":1,""divine"":1}},{""seed"":1,""fl"":20,""stars"":0,""items"":[[""이중 궤도 벨트"",""belt"",""space"",4,""common"",1,""hp"",90720,[[""hpRegen"",2.8]],null,3,0],[""여압 장갑"",""gloves"",""space"",4,""mythic"",2,""atk"",102098,[[""dblAtk"",5.7]],null,0,0],[""여압 그리브"",""shoes"",""space"",4,""common"",3,""hp"",92543,[[""block"",1.2]],null,2,0],[""방독면"",""helmet"",""space"",4,""epic"",4,""hp"",205631,[[""meleeDmg"",38.2],[""block"",3.9]],null,2,0],[""추진 부츠"",""shoes"",""space"",4,""mythic"",5,""hp"",613623,[[""lifesteal"",8.9]],null,1,0],[""위성 안테나 헬름"",""helmet"",""space"",4,""rare"",6,""hp"",143021,[[""critCh"",7.8]],null,4,0],[""자기장 너클"",""gloves"",""space"",4,""ultimate"",7,""atk"",75940,[[""rangedDmg"",14.3],[""hpPct"",10.7]],null,4,0],[""이온 블레이드"",""weapon"",""space"",4,""legendary"",7,""atk"",52828,[[""hpRegen"",3.6],[""meleeDmg"",46.9],[""atkSpd"",7.5],[""rangedDmg"",14.3]],""ionBlade"",0,0],[""항성 망치"",""weapon"",""interstellar"",5,""common"",1,""atk"",93312,[[""lifesteal"",13.5]],""starHammer"",2,0],[""착륙 장화"",""shoes"",""space"",4,""common"",8,""hp"",97264,[[""block"",2.2]],null,4,0],[""산소 회로 목걸이"",""necklace"",""space"",4,""common"",8,""atk"",16673,[[""critDmg"",12.2]],null,0,0],[""레일건"",""weapon"",""space"",4,""rare"",9,""atk"",25260,[[""dmgPct"",4],[""skillDmg"",8.3]],""railgun"",4,0],[""헤비듀티"",""helmet"",""interstellar"",5,""epic"",2,""hp"",1209479,[[""hpPct"",5.6]],null,4,0],[""자력 부츠"",""shoes"",""space"",4,""common"",10,""hp"",99219,[[""skillDmg"",7.4]],null,0,0],[""궤도 망토"",""armor"",""space"",4,""common"",11,""hp"",100211,[[""lifesteal"",18.3]],null,4,0],[""자기장 너클"",""gloves"",""space"",4,""rare"",12,""atk"",26026,[[""block"",2.1]],null,4,0],[""광자 창"",""weapon"",""interstellar"",5,""common"",3,""atk"",95187,[[""skillDmg"",12.2]],""photonLance"",1,0],[""추진 벨트"",""belt"",""space"",4,""epic"",13,""hp"",224896,[[""dblAtk"",14],[""lifesteal"",5.7]],null,2,0],[""위성 안테나 헬름"",""helmet"",""space"",4,""epic"",14,""hp"",227145,[[""rangedDmg"",1.4],[""hpRegen"",2.3]],null,4,0],[""궤도 펜던트"",""necklace"",""space"",4,""common"",15,""atk"",17876,[[""hpPct"",8.6]],null,2,0]],""rollLevelAfter"":{""primitive"":1,""medieval"":1,""earlyModern"":1,""modern"":1,""space"":16,""interstellar"":4,""multiverse"":1,""quantum"":1,""underworld"":1,""divine"":1}},{""seed"":1,""fl"":29,""stars"":0,""items"":[[""이중 차원 벨트"",""belt"",""multiverse"",6,""common"",1,""hp"",3265920,[[""hpRegen"",2.8]],null,3,0],[""홀로 장갑"",""gloves"",""multiverse"",6,""mythic"",2,""atk"",3675559,[[""dblAtk"",5.7]],null,0,0],[""렌더 그리브"",""shoes"",""multiverse"",6,""common"",3,""hp"",3331564,[[""block"",1.2]],null,2,0],[""스토커 헬름"",""helmet"",""multiverse"",6,""epic"",4,""hp"",7402737,[[""meleeDmg"",38.2],[""block"",3.9]],null,2,0],[""터널링 신발"",""shoes"",""quantum"",7,""mythic"",1,""hp"",127370880,[[""lifesteal"",8.9]],null,1,0],[""픽셀 크라운"",""helmet"",""multiverse"",6,""rare"",5,""hp"",5097794,[[""critCh"",7.8]],null,4,0],[""픽셀 너클"",""gloves"",""multiverse"",6,""ultimate"",6,""atk"",2706783,[[""rangedDmg"",14.3],[""hpPct"",10.7]],null,4,0],[""파동검"",""weapon"",""quantum"",7,""legendary"",2,""atk"",10857037,[[""hpRegen"",3.6],[""meleeDmg"",46.9],[""atkSpd"",7.5],[""rangedDmg"",14.3]],""waveBlade"",0,0],[""붕괴 망치"",""weapon"",""quantum"",7,""common"",3,""atk"",3426752,[[""lifesteal"",13.5]],""collapseHammer"",2,0],[""렌더 장화"",""shoes"",""multiverse"",6,""common"",6,""hp"",3432514,[[""block"",2.2]],null,4,0],[""차원 목걸이"",""necklace"",""multiverse"",6,""common"",6,""atk"",588431,[[""critDmg"",12.2]],null,0,0],[""메아리 활"",""weapon"",""multiverse"",6,""rare"",7,""atk"",891473,[[""dmgPct"",4],[""skillDmg"",8.3]],""echoBow"",4,0],[""헤어 반다나"",""helmet"",""quantum"",7,""epic"",4,""hp"",44416424,[[""hpPct"",5.6]],null,3,0],[""위상 부츠"",""shoes"",""quantum"",7,""common"",5,""hp"",20391176,[[""skillDmg"",7.4]],null,0,0],[""차원 망토"",""armor"",""multiverse"",6,""common"",8,""hp"",3501508,[[""lifesteal"",18.3]],null,4,0],[""픽셀 너클"",""gloves"",""multiverse"",6,""rare"",9,""atk"",909391,[[""block"",2.1]],null,4,0],[""터널링 단검"",""weapon"",""quantum"",7,""common"",6,""atk"",3530586,[[""skillDmg"",12.2]],""tunnelDagger"",1,0],[""픽셀 허리띠"",""belt"",""multiverse"",6,""epic"",10,""hp"",7858154,[[""dblAtk"",14],[""lifesteal"",5.7]],null,2,0],[""묶은 머리"",""helmet"",""quantum"",7,""epic"",7,""hp"",45762286,[[""rangedDmg"",1.4],[""hpRegen"",2.3]],null,4,0],[""데이터 펜던트"",""necklace"",""multiverse"",6,""common"",11,""atk"",618446,[[""hpPct"",8.6]],null,2,0]],""rollLevelAfter"":{""primitive"":1,""medieval"":1,""earlyModern"":1,""modern"":1,""space"":1,""interstellar"":1,""multiverse"":12,""quantum"":8,""underworld"":1,""divine"":1}},{""seed"":1,""fl"":35,""stars"":0,""items"":[[""이중 사슬 벨트"",""belt"",""underworld"",8,""common"",1,""hp"",117573120,[[""hpRegen"",2.8]],null,3,0],[""파동 장갑"",""gloves"",""quantum"",7,""mythic"",1,""atk"",21835008,[[""dblAtk"",5.7]],null,0,0],[""입자 그리브"",""shoes"",""quantum"",7,""common"",2,""hp"",19791475,[[""block"",1.2]],null,2,0],[""주파수 마스크"",""helmet"",""quantum"",7,""epic"",3,""hp"",43976657,[[""meleeDmg"",38.2],[""block"",3.9]],null,2,0],[""망자의 신발"",""shoes"",""underworld"",8,""mythic"",2,""hp"",771867532,[[""lifesteal"",8.9]],null,1,0],[""묶은 머리"",""helmet"",""quantum"",7,""epic"",4,""hp"",44416424,[[""critCh"",7.8]],null,4,0],[""입자 너클"",""gloves"",""quantum"",7,""mythic"",5,""atk"",22721596,[[""rangedDmg"",14.3],[""hpPct"",10.7]],null,4,0],[""지옥검"",""weapon"",""underworld"",8,""legendary"",3,""atk"",65793649,[[""hpRegen"",3.6],[""meleeDmg"",46.9],[""atkSpd"",7.5],[""rangedDmg"",14.3]],""hellBlade"",0,0],[""파멸의 망치"",""weapon"",""underworld"",8,""common"",4,""atk"",20766120,[[""lifesteal"",13.5]],""doomHammer"",2,0],[""파동 장화"",""shoes"",""quantum"",7,""common"",5,""hp"",20391176,[[""block"",2.2]],null,4,0],[""얽힘의 목걸이"",""necklace"",""quantum"",7,""common"",5,""atk"",3495630,[[""critDmg"",12.2]],null,0,0],[""얽힘의 지팡이"",""weapon"",""quantum"",7,""rare"",6,""atk"",5295879,[[""dmgPct"",4],[""skillDmg"",8.3]],""entangleStaff"",4,0],[""로트팽 바이저"",""helmet"",""underworld"",8,""epic"",5,""hp"",269163532,[[""hpPct"",5.6]],null,3,0],[""용암 부츠"",""shoes"",""underworld"",8,""common"",6,""hp"",123570530,[[""skillDmg"",7.4]],null,0,0],[""양자 망토"",""armor"",""quantum"",7,""common"",7,""hp"",20801039,[[""lifesteal"",18.3]],null,4,0],[""지옥 너클"",""gloves"",""underworld"",8,""rare"",7,""atk"",32093032,[[""block"",2.1]],null,4,0],[""영혼 낫"",""weapon"",""underworld"",8,""common"",8,""atk"",21609308,[[""skillDmg"",12.2]],""soulScythe"",1,0],[""위상 허리띠"",""belt"",""quantum"",7,""epic"",8,""hp"",46219909,[[""dblAtk"",14],[""lifesteal"",5.7]],null,2,0],[""망자의 두건"",""helmet"",""underworld"",8,""epic"",9,""hp"",280092651,[[""rangedDmg"",1.4],[""hpRegen"",2.3]],null,4,0],[""독니 펜던트"",""necklace"",""underworld"",8,""common"",10,""atk"",22043655,[[""hpPct"",8.6]],null,2,0]],""rollLevelAfter"":{""primitive"":1,""medieval"":1,""earlyModern"":1,""modern"":1,""space"":1,""interstellar"":1,""multiverse"":1,""quantum"":9,""underworld"":11,""divine"":1}},{""seed"":7,""fl"":1,""stars"":0,""items"":[[""돌 반지"",""ring"",""primitive"",0,""common"",1,""atk"",12,[[""dblAtk"",9.9]],null,1,0],[""전투 페인트"",""helmet"",""primitive"",0,""rare"",1,""hp"",105,[[""dmgPct"",3.8],[""lifesteal"",6.6]],null,2,0],[""사냥꾼 허리띠"",""belt"",""primitive"",0,""legendary"",2,""hp"",226,[[""critDmg"",13.2]],null,1,0],[""조개 장식판 띠"",""belt"",""primitive"",0,""rare"",3,""hp"",107,[[""rangedDmg"",10.5],[""hpPct"",9.7]],null,4,0],[""풀잎 망토"",""armor"",""primitive"",0,""common"",4,""hp"",72,[[""critCh"",2]],null,3,0],[""사냥꾼 허리띠"",""belt"",""primitive"",0,""common"",5,""hp"",72,[[""rangedDmg"",5.6]],null,1,0],[""가죽옷"",""armor"",""primitive"",0,""common"",6,""hp"",73,[[""rangedDmg"",5.7]],null,0,0],[""털가죽 장화"",""shoes"",""primitive"",0,""epic"",5,""hp"",160,[[""skillCd"",1.7],[""dmgPct"",10.1]],null,4,0],[""사냥꾼 허리띠"",""belt"",""primitive"",0,""rare"",6,""hp"",110,[[""meleeDmg"",41.1],[""critCh"",6.7]],null,1,0],[""이중 사냥끈"",""belt"",""primitive"",0,""common"",7,""hp"",74,[[""skillDmg"",13.5]],null,3,0],[""깃털 장식"",""helmet"",""primitive"",0,""common"",7,""hp"",74,[[""critCh"",11.4]],null,4,0],[""엄니 초커"",""necklace"",""primitive"",0,""common"",8,""atk"",12,[[""hpPct"",4.9]],null,4,0],[""풀 엮은 신"",""shoes"",""primitive"",0,""rare"",9,""hp"",113,[[""hpPct"",8.1]],null,2,0],[""털가죽 장화"",""shoes"",""primitive"",0,""common"",10,""hp"",76,[[""atkSpd"",26.7]],null,4,0],[""돌 너클"",""gloves"",""primitive"",0,""rare"",11,""atk"",19,[[""meleeDmg"",40]],null,4,0],[""조가비 펜던트"",""necklace"",""primitive"",0,""common"",10,""atk"",13,[[""hpRegen"",2.3]],null,2,0],[""몽둥이"",""weapon"",""primitive"",0,""common"",11,""atk"",13,[[""skillCd"",6.9]],""club"",0,0],[""덩굴 쌍고리"",""ring"",""primitive"",0,""common"",12,""atk"",13,[[""lifesteal"",16]],null,3,0],[""나무껍질 브레이서"",""gloves"",""primitive"",0,""common"",13,""atk"",13,[[""hpRegen"",2.6]],null,3,0],[""털가죽 장화"",""shoes"",""primitive"",0,""common"",13,""hp"",78,[[""dblAtk"",7.8]],null,4,0]],""rollLevelAfter"":{""primitive"":13,""medieval"":1,""earlyModern"":1,""modern"":1,""space"":1,""interstellar"":1,""multiverse"":1,""quantum"":1,""underworld"":1,""divine"":1}},{""seed"":7,""fl"":10,""stars"":0,""items"":[[""돌 반지"",""ring"",""primitive"",0,""common"",1,""atk"",12,[[""dblAtk"",9.9]],null,1,0],[""로마 투구"",""helmet"",""medieval"",1,""rare"",1,""hp"",630,[[""dmgPct"",3.8],[""lifesteal"",6.6]],null,2,0],[""전투 벨트"",""belt"",""medieval"",1,""legendary"",2,""hp"",1357,[[""critDmg"",13.2]],null,1,0],[""판금 장식대"",""belt"",""medieval"",1,""rare"",3,""hp"",642,[[""rangedDmg"",10.5],[""hpPct"",9.7]],null,4,0],[""항해사 조끼"",""armor"",""earlyModern"",2,""common"",1,""hp"",2520,[[""critCh"",2]],null,3,0],[""전투 벨트"",""belt"",""medieval"",1,""common"",4,""hp"",432,[[""rangedDmg"",5.6]],null,1,0],[""철판 갑옷"",""armor"",""medieval"",1,""common"",5,""hp"",437,[[""rangedDmg"",5.7]],null,0,0],[""기병 장화"",""shoes"",""earlyModern"",2,""epic"",2,""hp"",5599,[[""skillCd"",1.7],[""dmgPct"",10.1]],null,4,0],[""장식 새시"",""belt"",""earlyModern"",2,""epic"",3,""hp"",5655,[[""meleeDmg"",41.1],[""critCh"",6.7],[""lifesteal"",14.8]],null,2,0],[""승마 부츠"",""shoes"",""earlyModern"",2,""legendary"",4,""hp"",8308,[[""rangedDmg"",11.3],[""dmgPct"",5.4]],null,1,0],[""풀 엮은 신"",""shoes"",""primitive"",0,""common"",1,""hp"",70,[[""critCh"",7.8]],null,2,0],[""장식 새시"",""belt"",""earlyModern"",2,""ultimate"",5,""hp"",12062,[[""skillDmg"",19.1],[""atkSpd"",6.4],[""hpPct"",8.1]],null,2,0],[""기사 장화"",""shoes"",""medieval"",1,""common"",4,""hp"",432,[[""atkSpd"",26.7]],null,4,0],[""철 너클"",""gloves"",""medieval"",1,""rare"",5,""atk"",112,[[""meleeDmg"",40]],null,4,0],[""십자 펜던트"",""necklace"",""medieval"",1,""common"",4,""atk"",74,[[""hpRegen"",2.3]],null,2,0],[""검"",""weapon"",""medieval"",1,""common"",5,""atk"",74,[[""skillCd"",6.9]],""sword"",0,0],[""쌍고리 반지"",""ring"",""medieval"",1,""common"",6,""atk"",75,[[""lifesteal"",16]],null,3,0],[""펜싱 브레이서"",""gloves"",""earlyModern"",2,""common"",6,""atk"",454,[[""hpRegen"",2.6]],null,3,0],[""기사 장화"",""shoes"",""medieval"",1,""common"",7,""hp"",445,[[""dblAtk"",7.8]],null,4,0],[""그리브"",""shoes"",""medieval"",1,""common"",7,""hp"",445,[[""hpPct"",8.7]],null,2,0]],""rollLevelAfter"":{""primitive"":1,""medieval"":8,""earlyModern"":6,""modern"":1,""space"":1,""interstellar"":1,""multiverse"":1,""quantum"":1,""underworld"":1,""divine"":1}},{""seed"":7,""fl"":20,""stars"":0,""items"":[[""신호 반지"",""ring"",""space"",4,""common"",1,""atk"",15552,[[""dblAtk"",9.9]],null,1,0],[""방독면"",""helmet"",""space"",4,""rare"",1,""hp"",136080,[[""dmgPct"",3.8],[""lifesteal"",6.6]],null,2,0],[""공구 벨트"",""belt"",""space"",4,""ultimate"",2,""hp"",421485,[[""critDmg"",13.2]],null,1,0],[""합금판 벨트"",""belt"",""space"",4,""rare"",3,""hp"",138815,[[""rangedDmg"",10.5],[""hpPct"",9.7]],null,4,0],[""추진 슈트"",""armor"",""space"",4,""common"",4,""hp"",93468,[[""critCh"",2]],null,3,0],[""공구 벨트"",""belt"",""space"",4,""common"",5,""hp"",94403,[[""rangedDmg"",5.6]],null,1,0],[""우주복"",""armor"",""space"",4,""common"",6,""hp"",95347,[[""rangedDmg"",5.7]],null,0,0],[""착륙 장화"",""shoes"",""space"",4,""legendary"",5,""hp"",302091,[[""skillCd"",1.7],[""dmgPct"",10.1],[""skillDmg"",28.9]],null,4,0],[""궤도 초커"",""necklace"",""space"",4,""legendary"",6,""atk"",52304,[[""critCh"",6.7],[""lifesteal"",14.8],[""rangedDmg"",11.6],[""hpPct"",8.8]],null,4,0],[""산소 회로 목걸이"",""necklace"",""space"",4,""rare"",5,""atk"",24275,[[""hpRegen"",1]],null,0,0],[""아크 방사기"",""weapon"",""interstellar"",5,""epic"",1,""atk"",205286,[[""meleeDmg"",36.1],[""hpPct"",4.9]],""arcThrower"",4,0],[""여압 그리브"",""shoes"",""space"",4,""epic"",5,""hp"",207687,[[""hpPct"",8.1]],null,2,0],[""착륙 장화"",""shoes"",""space"",4,""common"",6,""hp"",95347,[[""atkSpd"",26.7]],null,4,0],[""자기장 너클"",""gloves"",""space"",4,""rare"",7,""atk"",24763,[[""meleeDmg"",40]],null,4,0],[""궤도 펜던트"",""necklace"",""space"",4,""rare"",6,""atk"",24517,[[""hpRegen"",2.3]],null,2,0],[""이온 블레이드"",""weapon"",""space"",4,""common"",7,""atk"",16508,[[""skillCd"",6.9]],""ionBlade"",0,0],[""쌍궤도 반지"",""ring"",""space"",4,""common"",8,""atk"",16673,[[""lifesteal"",16]],null,3,0],[""합금 건틀릿"",""gloves"",""space"",4,""rare"",9,""atk"",25260,[[""hpRegen"",2.6],[""skillDmg"",3.3]],null,1,0],[""자기장 너클"",""gloves"",""space"",4,""epic"",9,""atk"",37049,[[""skillCd"",2.5],[""lifesteal"",10.1]],null,4,0],[""자기장 너클"",""gloves"",""space"",4,""ultimate"",9,""atk"",77466,[[""dmgPct"",5.1],[""skillCd"",4.4]],null,4,0]],""rollLevelAfter"":{""primitive"":1,""medieval"":1,""earlyModern"":1,""modern"":1,""space"":10,""interstellar"":2,""multiverse"":1,""quantum"":1,""underworld"":1,""divine"":1}},{""seed"":7,""fl"":29,""stars"":0,""items"":[[""항성 반지"",""ring"",""interstellar"",5,""common"",1,""atk"",93312,[[""dblAtk"",9.9]],null,1,0],[""스토커 헬름"",""helmet"",""multiverse"",6,""rare"",1,""hp"",4898880,[[""dmgPct"",3.8],[""lifesteal"",6.6]],null,2,0],[""코드 벨트"",""belt"",""multiverse"",6,""ultimate"",2,""hp"",15173464,[[""critDmg"",13.2]],null,1,0],[""데이터판 벨트"",""belt"",""multiverse"",6,""epic"",3,""hp"",7329442,[[""rangedDmg"",10.5],[""hpPct"",9.7]],null,4,0],[""입자 조끼"",""armor"",""quantum"",7,""common"",1,""hp"",19595520,[[""critCh"",2]],null,3,0],[""코드 벨트"",""belt"",""multiverse"",6,""common"",4,""hp"",3364880,[[""rangedDmg"",5.6]],null,1,0],[""홀로 아머"",""armor"",""multiverse"",6,""common"",5,""hp"",3398529,[[""rangedDmg"",5.7]],null,0,0],[""렌더 장화"",""shoes"",""multiverse"",6,""legendary"",4,""hp"",10767618,[[""skillCd"",1.7],[""dmgPct"",10.1],[""skillDmg"",28.9]],null,4,0],[""홀로 초커"",""necklace"",""multiverse"",6,""ultimate"",5,""atk"",2679983,[[""critCh"",6.7],[""lifesteal"",14.8],[""rangedDmg"",11.6],[""hpPct"",8.8]],null,4,0],[""차원 목걸이"",""necklace"",""multiverse"",6,""rare"",4,""atk"",865255,[[""hpRegen"",1]],null,0,0],[""얽힘의 지팡이"",""weapon"",""quantum"",7,""epic"",2,""atk"",7464213,[[""meleeDmg"",36.1],[""hpPct"",4.9]],""entangleStaff"",4,0],[""렌더 그리브"",""shoes"",""multiverse"",6,""epic"",4,""hp"",7402737,[[""hpPct"",8.1]],null,2,0],[""렌더 장화"",""shoes"",""multiverse"",6,""common"",5,""hp"",3398529,[[""atkSpd"",26.7]],null,4,0],[""픽셀 너클"",""gloves"",""multiverse"",6,""epic"",6,""atk"",1294548,[[""meleeDmg"",40]],null,4,0],[""데이터 펜던트"",""necklace"",""multiverse"",6,""rare"",5,""atk"",873907,[[""hpRegen"",2.3]],null,2,0],[""현실 절단검"",""weapon"",""multiverse"",6,""common"",6,""atk"",588431,[[""skillCd"",6.9]],""realityBlade"",0,0],[""쌍차원 반지"",""ring"",""multiverse"",6,""common"",7,""atk"",594315,[[""lifesteal"",16]],null,3,0],[""얽힘의 건틀릿"",""gloves"",""quantum"",7,""rare"",3,""atk"",5140128,[[""hpRegen"",2.6],[""skillDmg"",3.3]],null,1,0],[""입자 너클"",""gloves"",""quantum"",7,""epic"",3,""atk"",7538855,[[""skillCd"",2.5],[""lifesteal"",10.1]],null,4,0],[""픽셀 너클"",""gloves"",""multiverse"",6,""ultimate"",8,""atk"",2761189,[[""dmgPct"",5.1],[""skillCd"",4.4]],null,4,0]],""rollLevelAfter"":{""primitive"":1,""medieval"":1,""earlyModern"":1,""modern"":1,""space"":1,""interstellar"":1,""multiverse"":9,""quantum"":3,""underworld"":1,""divine"":1}},{""seed"":7,""fl"":35,""stars"":0,""items"":[[""스핀 반지"",""ring"",""quantum"",7,""common"",1,""atk"",3359232,[[""dblAtk"",9.9]],null,1,0],[""묶은 머리"",""helmet"",""quantum"",7,""epic"",1,""hp"",43110144,[[""dmgPct"",3.8],[""lifesteal"",6.6],[""meleeDmg"",15.5]],null,4,0],[""헤어 반다나"",""helmet"",""quantum"",7,""legendary"",2,""hp"",63332720,[[""hpRegen"",2.6]],null,3,0],[""중첩 아뮬렛"",""necklace"",""quantum"",7,""legendary"",3,""atk"",10965608,[[""hpPct"",9.7],[""skillCd"",6],[""critCh"",2.9]],null,1,0],[""파동 장화"",""shoes"",""quantum"",7,""common"",4,""hp"",20189283,[[""dblAtk"",9.9]],null,4,0],[""용암 갑주"",""armor"",""underworld"",8,""rare"",1,""hp"",176359680,[[""critDmg"",73.1]],null,1,0],[""얽힘의 지팡이"",""weapon"",""quantum"",7,""rare"",5,""atk"",5243445,[[""skillCd"",4.3],[""atkSpd"",26.6]],""entangleStaff"",4,0],[""위상 허리띠"",""belt"",""quantum"",7,""rare"",6,""hp"",30892632,[[""skillDmg"",18.2],[""hpPct"",14.5]],null,2,0],[""재의 조끼"",""armor"",""underworld"",8,""common"",2,""hp"",118748851,[[""meleeDmg"",38]],null,4,0],[""관측자 반지"",""ring"",""quantum"",7,""epic"",7,""atk"",7844963,[[""meleeDmg"",16.3],[""block"",1],[""critCh"",11.4]],null,4,0],[""가시 초커"",""necklace"",""underworld"",8,""common"",3,""atk"",20560515,[[""hpPct"",4.9]],null,4,0],[""재의 그리브"",""shoes"",""underworld"",8,""epic"",4,""hp"",266498546,[[""hpPct"",8.1]],null,2,0],[""파동 장화"",""shoes"",""quantum"",7,""common"",8,""hp"",21009049,[[""atkSpd"",26.7]],null,4,0],[""입자 너클"",""gloves"",""quantum"",7,""epic"",9,""atk"",8002647,[[""meleeDmg"",40]],null,4,0],[""위상 펜던트"",""necklace"",""quantum"",7,""rare"",8,""atk"",5402327,[[""hpRegen"",2.3]],null,2,0],[""파동검"",""weapon"",""quantum"",7,""common"",9,""atk"",3637566,[[""skillCd"",6.9]],""waveBlade"",0,0],[""쌍스핀 반지"",""ring"",""quantum"",7,""common"",10,""atk"",3673942,[[""lifesteal"",16]],null,3,0],[""망자의 장갑"",""gloves"",""underworld"",8,""rare"",5,""atk"",31460672,[[""hpRegen"",2.6],[""skillDmg"",3.3]],null,1,0],[""지옥 너클"",""gloves"",""underworld"",8,""epic"",5,""atk"",46142319,[[""skillCd"",2.5],[""lifesteal"",10.1]],null,4,0],[""입자 너클"",""gloves"",""quantum"",7,""ultimate"",11,""atk"",17069137,[[""dmgPct"",5.1],[""skillCd"",4.4]],null,4,0]],""rollLevelAfter"":{""primitive"":1,""medieval"":1,""earlyModern"":1,""modern"":1,""space"":1,""interstellar"":1,""multiverse"":1,""quantum"":12,""underworld"":5,""divine"":1}},{""seed"":2024,""fl"":1,""stars"":2,""items"":[[""덩굴 쌍고리"",""ring"",""primitive"",0,""rare"",1,""atk"",18,[[""rangedDmg"",6],[""critDmg"",37.4]],null,3,2],[""몽둥이"",""weapon"",""primitive"",0,""rare"",2,""atk"",18,[[""dblAtk"",18.6]],""club"",0,2],[""사냥꾼 허리띠"",""belt"",""primitive"",0,""common"",3,""hp"",71,[[""skillCd"",1.1]],null,1,2],[""덩굴 쌍고리"",""ring"",""primitive"",0,""common"",2,""atk"",12,[[""rangedDmg"",10.3]],null,3,2],[""나무껍질 브레이서"",""gloves"",""primitive"",0,""common"",3,""atk"",12,[[""meleeDmg"",22.1]],null,3,2],[""가죽 신"",""shoes"",""primitive"",0,""common"",4,""hp"",72,[[""dblAtk"",18.9]],null,0,2],[""이빨 부적"",""necklace"",""primitive"",0,""common"",5,""atk"",12,[[""hpPct"",13.8]],null,1,2],[""호박 부적함"",""necklace"",""primitive"",0,""common"",6,""atk"",12,[[""lifesteal"",5.1]],null,3,2],[""조가비 펜던트"",""necklace"",""primitive"",0,""rare"",7,""atk"",19,[[""block"",1.8],[""rangedDmg"",2.9]],null,2,2],[""가죽 손싸개"",""gloves"",""primitive"",0,""epic"",8,""atk"",28,[[""block"",4.7],[""critDmg"",60.2],[""atkSpd"",12.8]],null,0,2],[""조가비 펜던트"",""necklace"",""primitive"",0,""common"",9,""atk"",12,[[""critCh"",3.5]],null,2,2],[""이중 사냥끈"",""belt"",""primitive"",0,""common"",9,""hp"",75,[[""skillDmg"",18.8]],null,3,2],[""가죽옷"",""armor"",""primitive"",0,""common"",10,""hp"",76,[[""dmgPct"",9.1]],null,0,2],[""돌 반지"",""ring"",""primitive"",0,""common"",11,""atk"",13,[[""dblAtk"",12.5]],null,1,2],[""돌창"",""weapon"",""primitive"",0,""rare"",12,""atk"",20,[[""hpRegen"",1.9]],""stoneSpear"",2,2],[""풀잎 망토"",""armor"",""primitive"",0,""common"",13,""hp"",78,[[""hpRegen"",1.3]],null,3,2],[""털가죽 발싸개"",""shoes"",""primitive"",0,""common"",14,""hp"",79,[[""dblAtk"",17.3]],null,1,2],[""투석구"",""weapon"",""primitive"",0,""common"",15,""atk"",13,[[""hpRegen"",1.5]],""sling"",4,2],[""가면"",""helmet"",""primitive"",0,""common"",15,""hp"",80,[[""hpPct"",6.1]],null,1,2],[""엮은 샌들"",""shoes"",""primitive"",0,""rare"",16,""hp"",121,[[""hpPct"",1.3],[""critDmg"",30.9]],null,3,2]],""rollLevelAfter"":{""primitive"":17,""medieval"":1,""earlyModern"":1,""modern"":1,""space"":1,""interstellar"":1,""multiverse"":1,""quantum"":1,""underworld"":1,""divine"":1}},{""seed"":2024,""fl"":10,""stars"":2,""items"":[[""쌍줄 은반지"",""ring"",""earlyModern"",2,""rare"",1,""atk"",648,[[""rangedDmg"",6],[""critDmg"",37.4]],null,3,2],[""전투도끼"",""weapon"",""medieval"",1,""rare"",1,""atk"",108,[[""dblAtk"",18.6]],""axe"",1,2],[""사냥꾼 허리띠"",""belt"",""primitive"",0,""rare"",1,""hp"",105,[[""skillCd"",1.1]],null,1,2],[""쌍고리 반지"",""ring"",""medieval"",1,""common"",2,""atk"",72,[[""rangedDmg"",10.3]],null,3,2],[""강철 브레이서"",""gloves"",""medieval"",1,""common"",3,""atk"",73,[[""meleeDmg"",22.1]],null,3,2],[""사슬 신발"",""shoes"",""medieval"",1,""common"",4,""hp"",432,[[""dblAtk"",18.9]],null,0,2],[""항해사 목걸이"",""necklace"",""earlyModern"",2,""common"",2,""atk"",436,[[""hpPct"",13.8]],null,1,2],[""로켓 펜던트"",""necklace"",""earlyModern"",2,""common"",3,""atk"",440,[[""lifesteal"",5.1]],null,3,2],[""십자 펜던트"",""necklace"",""medieval"",1,""rare"",5,""atk"",112,[[""block"",1.8],[""rangedDmg"",2.9]],null,2,2],[""사슬 장갑"",""gloves"",""medieval"",1,""epic"",6,""atk"",166,[[""block"",4.7],[""critDmg"",60.2],[""atkSpd"",12.8]],null,0,2],[""카메오 펜던트"",""necklace"",""earlyModern"",2,""common"",4,""atk"",445,[[""critCh"",3.5]],null,2,2],[""이중 탄띠"",""belt"",""earlyModern"",2,""common"",4,""hp"",2596,[[""skillDmg"",18.8]],null,3,2],[""철판 갑옷"",""armor"",""medieval"",1,""rare"",7,""hp"",668,[[""dmgPct"",9.1],[""block"",4.2]],null,0,2],[""총사 장갑"",""gloves"",""earlyModern"",2,""rare"",5,""atk"",674,[[""block"",3.5],[""skillDmg"",17.8]],null,0,2],[""이빨 부적"",""necklace"",""primitive"",0,""common"",1,""atk"",12,[[""hpRegen"",2.7]],null,1,2],[""쌍고리 반지"",""ring"",""medieval"",1,""common"",8,""atk"",77,[[""block"",1.9]],null,3,2],[""사슬 장갑"",""gloves"",""medieval"",1,""common"",9,""atk"",77,[[""hpRegen"",3.5]],null,0,2],[""전투 벨트"",""belt"",""medieval"",1,""common"",9,""hp"",454,[[""block"",2.8]],null,1,2],[""기사단 망토"",""armor"",""medieval"",1,""legendary"",10,""hp"",1469,[[""rangedDmg"",7.9],[""skillDmg"",18.1],[""hpPct"",1.3],[""critDmg"",30.9]],null,3,2],[""판금 장식대"",""belt"",""medieval"",1,""common"",11,""hp"",463,[[""hpRegen"",3.8]],null,4,2]],""rollLevelAfter"":{""primitive"":2,""medieval"":12,""earlyModern"":6,""modern"":1,""space"":1,""interstellar"":1,""multiverse"":1,""quantum"":1,""underworld"":1,""divine"":1}},{""seed"":2024,""fl"":20,""stars"":2,""items"":[[""쌍궤도 반지"",""ring"",""space"",4,""rare"",1,""atk"",23328,[[""rangedDmg"",6],[""critDmg"",37.4]],null,3,2],[""레이저 라이플"",""weapon"",""space"",4,""epic"",2,""atk"",34556,[[""dblAtk"",18.6],[""critDmg"",1.3]],""laser"",2,2],[""위상 허리띠"",""belt"",""quantum"",7,""legendary"",1,""hp"",62705664,[[""lifesteal"",8.2]],null,2,2],[""신호 반지"",""ring"",""space"",4,""rare"",3,""atk"",23796,[[""atkSpd"",13.7],[""critDmg"",22.9]],null,1,2],[""사령관 반지"",""ring"",""space"",4,""rare"",4,""atk"",24034,[[""dblAtk"",9.7]],null,4,2],[""레이저 라이플"",""weapon"",""space"",4,""common"",5,""atk"",16183,[[""critDmg"",15.8]],""laser"",2,2],[""여압 브레이서"",""gloves"",""space"",4,""legendary"",4,""atk"",51274,[[""critCh"",6.3],[""meleeDmg"",16.4],[""dblAtk"",5.1]],null,3,2],[""궤도 펜던트"",""necklace"",""space"",4,""rare"",3,""atk"",23796,[[""block"",1.8],[""rangedDmg"",2.9]],null,2,2],[""진공 랩"",""gloves"",""space"",4,""legendary"",4,""atk"",51274,[[""block"",4.7],[""critDmg"",60.2],[""atkSpd"",12.8],[""critCh"",9.7]],null,2,2],[""이온 블레이드"",""weapon"",""space"",4,""rare"",5,""atk"",24275,[[""dblAtk"",16.8]],""ionBlade"",0,2],[""추진 부츠"",""shoes"",""space"",4,""epic"",6,""hp"",209764,[[""rangedDmg"",6],[""meleeDmg"",1.8]],null,1,2],[""바이오 헬멧"",""helmet"",""space"",4,""rare"",6,""hp"",143021,[[""critDmg"",53.5],[""atkSpd"",9.4]],null,1,2],[""추진 부츠"",""shoes"",""space"",4,""common"",7,""hp"",96301,[[""critCh"",1.1]],null,1,2],[""엑소스켈레톤"",""armor"",""space"",4,""common"",8,""hp"",97264,[[""lifesteal"",9.5]],null,1,2],[""바이오 헬멧"",""helmet"",""space"",4,""rare"",9,""hp"",147355,[[""atkSpd"",10.2]],null,1,2],[""바이오 헬멧"",""helmet"",""space"",4,""common"",9,""hp"",98236,[[""critCh"",6.3]],null,1,2],[""바이오 헬멧"",""helmet"",""space"",4,""ultimate"",10,""hp"",456407,[[""block"",3.2],[""hpPct"",6.1]],null,1,2],[""무중력 샌들"",""shoes"",""space"",4,""rare"",11,""hp"",150316,[[""hpPct"",1.3],[""critDmg"",30.9]],null,3,2],[""합금판 벨트"",""belt"",""space"",4,""common"",12,""hp"",101213,[[""hpRegen"",3.8]],null,4,2],[""플라즈마 캐논"",""weapon"",""space"",4,""epic"",13,""atk"",38553,[[""block"",4]],""plasmaCannon"",3,2]],""rollLevelAfter"":{""primitive"":1,""medieval"":1,""earlyModern"":1,""modern"":1,""space"":14,""interstellar"":1,""multiverse"":1,""quantum"":2,""underworld"":1,""divine"":1}},{""seed"":2024,""fl"":29,""stars"":2,""items"":[[""쌍스핀 반지"",""ring"",""quantum"",7,""rare"",1,""atk"",5038848,[[""rangedDmg"",6],[""critDmg"",37.4]],null,3,2],[""균열 발사기"",""weapon"",""multiverse"",6,""epic"",1,""atk"",1231718,[[""dblAtk"",18.6],[""critDmg"",1.3]],""riftLauncher"",2,2],[""뼈 장식 벨트"",""belt"",""underworld"",8,""legendary"",1,""hp"",376233984,[[""lifesteal"",8.2]],null,2,2],[""해시 인장 반지"",""ring"",""multiverse"",6,""epic"",2,""atk"",1244035,[[""atkSpd"",13.7],[""critDmg"",22.9]],null,1,2],[""관리자 반지"",""ring"",""multiverse"",6,""rare"",3,""atk"",856688,[[""dblAtk"",9.7]],null,4,2],[""균열 발사기"",""weapon"",""multiverse"",6,""common"",4,""atk"",576836,[[""critDmg"",15.8]],""riftLauncher"",2,2],[""위상 브레이서"",""gloves"",""quantum"",7,""ultimate"",2,""atk"",15606991,[[""critCh"",6.3],[""meleeDmg"",16.4],[""dblAtk"",5.1]],null,3,2],[""홀로 초커"",""necklace"",""multiverse"",6,""epic"",3,""atk"",1256475,[[""block"",1.8],[""rangedDmg"",2.9],[""meleeDmg"",28.7]],null,4,2],[""스피드러너 캡"",""helmet"",""multiverse"",6,""common"",4,""hp"",3364880,[[""critDmg"",60.2]],null,3,2],[""홀로 장갑"",""gloves"",""multiverse"",6,""common"",3,""atk"",571125,[[""dmgPct"",7.9]],null,0,2],[""메아리 활"",""weapon"",""multiverse"",6,""common"",3,""atk"",571125,[[""skillCd"",5.5]],""echoBow"",4,2],[""데이터 펜던트"",""necklace"",""multiverse"",6,""rare"",3,""atk"",856688,[[""hpRegen"",3.3]],null,2,2],[""방화벽 마스크"",""helmet"",""multiverse"",6,""common"",4,""hp"",3364880,[[""atkSpd"",9.4]],null,1,2],[""가상 신발"",""shoes"",""multiverse"",6,""common"",4,""hp"",3364880,[[""critCh"",1.1]],null,1,2],[""가상 슈트"",""armor"",""multiverse"",6,""rare"",5,""hp"",5097794,[[""lifesteal"",9.5],[""hpRegen"",1.3]],null,3,2],[""터널링 신발"",""shoes"",""quantum"",7,""common"",1,""hp"",19595520,[[""dblAtk"",17.3]],null,1,2],[""메아리 활"",""weapon"",""multiverse"",6,""common"",6,""atk"",588431,[[""hpRegen"",1.5]],""echoBow"",4,2],[""방화벽 마스크"",""helmet"",""multiverse"",6,""common"",6,""hp"",3432514,[[""hpPct"",6.1]],null,1,2],[""터널링 샌들"",""shoes"",""quantum"",7,""rare"",2,""hp"",29687212,[[""hpPct"",1.3],[""critDmg"",30.9]],null,3,2],[""데이터판 벨트"",""belt"",""multiverse"",6,""common"",7,""hp"",3466839,[[""hpRegen"",3.8]],null,4,2]],""rollLevelAfter"":{""primitive"":1,""medieval"":1,""earlyModern"":1,""modern"":1,""space"":1,""interstellar"":1,""multiverse"":8,""quantum"":3,""underworld"":2,""divine"":1}},{""seed"":2024,""fl"":35,""stars"":2,""items"":[[""쌍사슬 반지"",""ring"",""underworld"",8,""rare"",1,""atk"",30233088,[[""rangedDmg"",6],[""critDmg"",37.4]],null,3,2],[""파멸의 망치"",""weapon"",""underworld"",8,""epic"",2,""atk"",44785281,[[""dblAtk"",18.6],[""critDmg"",1.3]],""doomHammer"",2,2],[""후광 허리띠"",""belt"",""divine"",9,""legendary"",1,""hp"",2257403904,[[""lifesteal"",8.2]],null,2,2],[""스핀 반지"",""ring"",""quantum"",7,""epic"",1,""atk"",7390310,[[""atkSpd"",13.7],[""critDmg"",22.9]],null,1,2],[""망령왕 반지"",""ring"",""underworld"",8,""rare"",3,""atk"",30840773,[[""dblAtk"",9.7]],null,4,2],[""붕괴 망치"",""weapon"",""quantum"",7,""common"",2,""atk"",3392824,[[""critDmg"",15.8]],""collapseHammer"",2,2],[""망자의 브레이서"",""gloves"",""underworld"",8,""ultimate"",4,""atk"",95524154,[[""critCh"",6.3],[""meleeDmg"",16.4],[""dblAtk"",5.1]],null,3,2],[""얽힘 초커"",""necklace"",""quantum"",7,""epic"",1,""atk"",7390310,[[""block"",1.8],[""rangedDmg"",2.9],[""meleeDmg"",28.7]],null,4,2],[""헤어 반다나"",""helmet"",""quantum"",7,""common"",2,""hp"",19791475,[[""critDmg"",60.2]],null,3,2],[""파동 장갑"",""gloves"",""quantum"",7,""common"",1,""atk"",3359232,[[""dmgPct"",7.9]],null,0,2],[""얽힘의 지팡이"",""weapon"",""quantum"",7,""common"",1,""atk"",3359232,[[""skillCd"",5.5]],""entangleStaff"",4,2],[""위상 펜던트"",""necklace"",""quantum"",7,""rare"",1,""atk"",5038848,[[""hpRegen"",3.3]],null,2,2],[""얽힘의 헬름"",""helmet"",""quantum"",7,""common"",2,""hp"",19791475,[[""atkSpd"",9.4]],null,1,2],[""터널링 신발"",""shoes"",""quantum"",7,""common"",2,""hp"",19791475,[[""critCh"",1.1]],null,1,2],[""입자 조끼"",""armor"",""quantum"",7,""rare"",3,""hp"",29984084,[[""lifesteal"",9.5],[""hpRegen"",1.3]],null,3,2],[""망자의 신발"",""shoes"",""underworld"",8,""common"",3,""hp"",119936339,[[""dblAtk"",17.3]],null,1,2],[""얽힘의 지팡이"",""weapon"",""quantum"",7,""common"",4,""atk"",3461020,[[""hpRegen"",1.5]],""entangleStaff"",4,2],[""얽힘의 헬름"",""helmet"",""quantum"",7,""common"",4,""hp"",20189283,[[""hpPct"",6.1]],null,1,2],[""재의 샌들"",""shoes"",""underworld"",8,""rare"",4,""hp"",181703554,[[""hpPct"",1.3],[""critDmg"",30.9]],null,3,2],[""뼈 장식판 벨트"",""belt"",""underworld"",8,""common"",5,""hp"",122347060,[[""hpRegen"",3.8]],null,4,2]],""rollLevelAfter"":{""primitive"":1,""medieval"":1,""earlyModern"":1,""modern"":1,""space"":1,""interstellar"":1,""multiverse"":1,""quantum"":5,""underworld"":6,""divine"":2}}],""rollWalk"":{""seed"":42,""steps"":[2,3,3,4,5,6,7,8,8,9,10,10,10,11,12,13,14,15,16,17,17,18,19,20,21,22,23,23,24,25,26,26,27,27,28,29,30,31,32,33,34,35,36,37,37,37,38,39,40,41,42,43,44,45,44,45,44,45,44,45,46,47,48,49,50,50,51,51,50,51,51,52,53,53,54,53,54,53,54,55,56,57,58,58,59,60,60,61,60,60,61,62,63,64,65,66,66,65,66,67,67,68,67,68,68,69,70,71,72,73,74,75,76,76,77,77,78,79,79,80,81,81,82,82,81,82,83,84,85,86,86,86,87,88,89,90,91,90,91,90,91,92,93,94,95,96,97,98,99,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,99,99,100,100,100,100,100,100,100,100,100,100,100,100,100,99,100,100,100,100,100,100,100,100,100,100,99,99,100,100,100,100,100,100,100,100,100,99,100,100,100,100,100,100,100,100,100,99,99,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,100,99,100,100,100,100,100,100,100,100,100,100,100,100,99,99,100,100,100,100,100,100,100,100,100,100,100,100,100,99,100,100,100,100,100,100,100,99,98,99,100,100,99,100,100,100,100,100]},""rollWalkCap"":{""seed"":42,""start"":105,""cap"":110,""steps"":[106,107,107,108,109,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,110,109,110,109,110,109,110],""rollLevelOfClamped"":100},""ensureRollLevels"":{""primitive"":1,""medieval"":1,""space"":7,""earlyModern"":1,""modern"":1,""interstellar"":1,""multiverse"":1,""quantum"":1,""underworld"":1,""divine"":1},""resetRollLevels"":{""primitive"":1,""medieval"":1,""earlyModern"":1,""modern"":1,""space"":1,""interstellar"":1,""multiverse"":1,""quantum"":1,""underworld"":1,""divine"":1},""craftShort"":{""seed"":3,""req"":5,""items"":[""털가죽 발싸개|primitive|common|1"",""나무껍질 브레이서|primitive|common|2"",""엄니 초커|primitive|common|2""],""hammers"":0,""totalCrafts"":3,""bumps"":[[""craft"",3]]},""craftNone"":{""items"":0,""bumps"":[[""craft"",0]]},""craftFree"":{""seed"":11,""req"":10,""hammersAfter"":17,""totalCrafts"":10},""flow"":{""canStartPoor"":false,""startPoor"":false,""canStart"":true,""start"":true,""coinsAfter"":600,""endsAt"":305000,""saves"":1,""bumps"":[[""coinSpend"",400],[""upgradeStart"",1],[""gearUpgrade"",1],[""coinSpend"",700],[""upgradeStart"",1],[""gearUpgrade"",1]],""canStartWhileRunning"":false,""gemCostMid"":1,""gemSkipPoor"":true,""levelBeforeEnd"":2,""levelAtEnd"":2,""endsAtCleared"":null,""toasts"":[""⚒️ 대장간 레벨 2 달성!"",""⚒️ 대장간 레벨 3 달성!""],""bumpsAfterTick"":[[""coinSpend"",400],[""upgradeStart"",1],[""gearUpgrade"",1],[""coinSpend"",700],[""upgradeStart"",1],[""gearUpgrade"",1]],""savesAfterTick"":2,""start2"":true,""endsAt2"":1000000,""gemCost2"":2,""gemSkip2"":true,""gemsAfter2"":3,""level2"":3,""endsAt2After"":null,""gemSkipIdle"":false,""maxInfo"":null,""maxCanStart"":null,""lv35"":35},""autoDefault"":{""keepAges"":[],""filterOn"":false,""filterSubs"":[],""hammersPerBatch"":10,""stopOnTarget"":false},""autoMigrate"":{""keepAges"":[""medieval""],""filterOn"":false,""filterSubs"":[],""hammersPerBatch"":5,""stopOnTarget"":false},""autoNoStop"":{""keepAges"":[],""filterOn"":true,""filterSubs"":[""critCh""],""hammersPerBatch"":5,""stopOnTarget"":false},""autoFilter"":[{""cfg"":{""keepAges"":[],""filterOn"":false,""filterSubs"":[]},""item"":{""age"":""primitive"",""subs"":[{""key"":""critCh"",""value"":1}]},""hasTarget"":false,""passes"":false,""hasTargetActual"":false},{""cfg"":{""keepAges"":[],""filterOn"":true,""filterSubs"":[]},""item"":{""age"":""primitive"",""subs"":[{""key"":""critCh"",""value"":1}]},""hasTarget"":false,""passes"":false,""hasTargetActual"":false},{""cfg"":{""keepAges"":[""medieval""],""filterOn"":false,""filterSubs"":[]},""item"":{""age"":""primitive"",""subs"":[]},""hasTarget"":true,""passes"":false,""hasTargetActual"":true},{""cfg"":{""keepAges"":[""medieval""],""filterOn"":false,""filterSubs"":[]},""item"":{""age"":""medieval"",""subs"":[]},""hasTarget"":true,""passes"":true,""hasTargetActual"":true},{""cfg"":{""keepAges"":[],""filterOn"":true,""filterSubs"":[""critCh"",""block""]},""item"":{""age"":""divine"",""subs"":[{""key"":""dmgPct"",""value"":1}]},""hasTarget"":true,""passes"":false,""hasTargetActual"":true},{""cfg"":{""keepAges"":[],""filterOn"":true,""filterSubs"":[""critCh"",""block""]},""item"":{""age"":""divine"",""subs"":[{""key"":""dmgPct"",""value"":1},{""key"":""block"",""value"":1}]},""hasTarget"":true,""passes"":true,""hasTargetActual"":true},{""cfg"":{""keepAges"":[""divine""],""filterOn"":true,""filterSubs"":[""critCh""]},""item"":{""age"":""divine"",""subs"":[{""key"":""dmgPct"",""value"":1}]},""hasTarget"":true,""passes"":false,""hasTargetActual"":true},{""cfg"":{""keepAges"":[""divine""],""filterOn"":true,""filterSubs"":[""critCh""]},""item"":{""age"":""divine"",""subs"":[{""key"":""critCh"",""value"":1}]},""hasTarget"":true,""passes"":true,""hasTargetActual"":true},{""cfg"":{""keepAges"":[""divine""],""filterOn"":false,""filterSubs"":[""critCh""]},""item"":{""age"":""divine"",""subs"":[{""key"":""dmgPct"",""value"":1}]},""hasTarget"":true,""passes"":true,""hasTargetActual"":true},{""cfg"":{""keepAges"":[""medieval""],""filterOn"":true,""filterSubs"":[""critCh""]},""item"":{""age"":""divine"",""subs"":[{""key"":""critCh"",""value"":1}]},""hasTarget"":true,""passes"":false,""hasTargetActual"":true}]}";
    }
}
