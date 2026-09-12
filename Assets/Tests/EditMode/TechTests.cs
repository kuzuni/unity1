using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core;
using Forge.Core.Ascend;
using Forge.Core.Data;
using Forge.Core.Tech;

namespace Forge.Tests
{
    /// <summary>
    /// T24 — 기술 트리·승천. 정본 `web/js/techtree.js`·`ascension.js` 를 node vm 에 그대로 올려(util·bignum·gamedata 와 함께 · `Date.now` 고정 · S 는 벡터마다 새로)
    /// 같은 입력의 출력을 찍은 벡터(<see cref="Vectors"/> · 41절)와 C# 결과를 대조한다 — 비용·시간 150 · 행 골격 3분기 · 부모 145 · 해금 · 배율 29 · 총 보너스 26줄 · 이관 4 · 연구 흐름 · 승천.
    /// 벡터를 다시 뽑으려면: 정본 여섯 파일을 vm 컨텍스트에 로드하고 `S`·`Skills.summonLevel`·`Pets.summonLevel`·`Mounts.level`·`Forge.resetRollLevels` 스텁을 두고 각 절의 입력을 그대로 호출(완료 기록 참조).
    /// </summary>
    public partial class TechTreeTests
    {
        static TechData _td;
        static TechData Td { get { return _td ?? (_td = TechData.Load(File.ReadAllText(Path.Combine(DataDir.Path, TechData.File)))); } }
        static JsonObject _v;
        static JsonObject V { get { return _v ?? (_v = MiniJson.ParseObject(Vectors)); } }
        const double Now = 1800000000000; // 벡터의 고정 «지금»(ms)

        static TechTree Tree(JsonObject levels)
        {
            var s = new TechState();
            if (levels != null) foreach (var kv in levels) s.Tech[kv.Key] = J.Int(kv.Value);
            return new TechTree(Td.Tech, DataDir.Game.Defs, s);
        }
        static TechTree Tree(params object[] kv)
        {
            var s = new TechState();
            for (int i = 0; i < kv.Length; i += 2) s.Tech[(string)kv[i]] = (int)kv[i + 1];
            return new TechTree(Td.Tech, DataDir.Game.Defs, s);
        }
        static List<string> Strs(object v) { return new List<string>(J.StrArr(v)); }

        [Test]
        public void 표를_읽는다()
        {
            var t = Td.Tech;
            Assert.AreEqual(5, t.Tiers); Assert.AreEqual(5, t.MaxLevel);
            Assert.AreEqual(3, t.Branches.Count);
            Assert.AreEqual("power", t.Branches[0].Id); Assert.AreEqual(7, t.Branches[0].Types.Length);
            Assert.AreEqual("forge", t.Branches[1].Id); Assert.AreEqual(10, t.Branches[1].Types.Length);
            Assert.AreEqual("skillpet", t.Branches[2].Id); Assert.AreEqual(12, t.Branches[2].Types.Length);
            Assert.AreEqual(29, t.Nodes.Count); Assert.AreEqual(29, t.Bonus.Count);
            foreach (var b in t.Branches) foreach (string ty in b.Types) { Assert.IsNotNull(t.Node(ty), ty); Assert.IsTrue(t.Bonus.Has(ty), ty); }
            Assert.AreEqual("레벨", t.Node("gearMaxLevel").Unit); Assert.AreEqual("개", t.Node("autoForgeSlot").Unit); Assert.IsNull(t.Node("weaponMastery").Unit);
            Assert.AreEqual("atk", t.Bonus["weaponMastery"].Expand); Assert.AreEqual("all", t.Bonus["gearMaxLevel"].Expand); Assert.IsNull(t.Bonus["mountDmg"].Expand);
            Assert.AreEqual(9, t.LegacyMap.Count); Assert.AreEqual(2, t.LegacyMap["gearPower"].Length); Assert.AreEqual("forgeTimer", t.LegacyMap["forgeSpeed"][0]);
            Assert.AreEqual(5, t.Roman.Length); Assert.AreEqual("V", t.Roman[4]);
            Assert.IsTrue(t.LvMult > 1 && t.TierMult > 1 && t.TimeBase > 0 && t.TimeLvMult > 1 && t.TimeTierMult > 1);
            Assert.IsNull(t.Branch("nope")); Assert.IsNull(t.Node("nope"));
        }

        [Test]
        public void 비용_시간_빈_트리_150()
        {
            var tree = Tree();
            var rows = J.Arr(V["costTime"]);
            Assert.AreEqual(150, rows.Count);
            foreach (var r in rows)
            {
                var a = J.Arr(r); string id = J.Str(a[0]); int lv = J.Int(a[1]);
                Assert.AreEqual(J.Num(a[2]), tree.Cost(id, lv), id + " Lv" + lv + " 비용");
                Assert.AreEqual(J.Num(a[3]), tree.Time(id, lv), id + " Lv" + lv + " 시간");
            }
            Assert.IsNull(tree.Cost("nope@1", 1)); Assert.IsNull(tree.Time("nope@1", 1));
        }

        [Test]
        public void 비용_시간_할인_단축()
        {
            var tree = Tree("techCost@1", 5, "techCost@2", 3, "techTimer@1", 5, "techTimer@3", 2);
            Assert.AreEqual(J.Num(V["techCostMult"]), tree.TechCostMult());
            Assert.AreEqual(J.Num(V["techTimeMult"]), tree.TechTimeMult());
            foreach (var r in J.Arr(V["costTimeDiscounted"]))
            {
                var a = J.Arr(r); string id = J.Str(a[0]); int lv = J.Int(a[1]);
                Assert.AreEqual(J.Num(a[2]), tree.Cost(id, lv), id + " Lv" + lv + " 비용");
                Assert.AreEqual(J.Num(a[3]), tree.Time(id, lv), id + " Lv" + lv + " 시간");
            }
        }

        [Test]
        public void 행_골격_세_분기()
        {
            var tree = Tree();
            foreach (var kv in J.Obj(V["rows"]))
            {
                var rows = tree.Rows(kv.Key);
                var exp = J.Arr(kv.Value);
                Assert.AreEqual(exp.Count, rows.Count, kv.Key + " 행 수");
                for (int i = 0; i < exp.Count; i++)
                {
                    var e = J.Arr(exp[i]);
                    CollectionAssert.AreEqual(J.StrArr(e[0]), rows[i].Ids, kv.Key + " 행 " + i);
                    Assert.AreEqual(J.Int(e[1]), rows[i].Tier, kv.Key + " 행 " + i + " tier");
                    Assert.AreEqual(J.Bool(e[2]), rows[i].First, kv.Key + " 행 " + i + " first");
                    Assert.AreEqual(J.Bool(e[3]), rows[i].LastOfTier, kv.Key + " 행 " + i + " lastOfTier");
                }
                Assert.AreEqual(1, rows[0].Ids.Length, kv.Key + " 맨 위 행은 노드 1개");
                Assert.AreEqual(1, rows[rows.Count - 1].Ids.Length, kv.Key + " 맨 아래 행은 노드 1개");
                Assert.AreSame(tree.RowsCached(kv.Key), tree.RowsCached(kv.Key));
                Assert.IsTrue(tree.TierBreak(rows[0], rows[rows.Count - 1])); Assert.IsFalse(tree.TierBreak(rows[0], rows[1])); Assert.IsFalse(tree.TierBreak(null, rows[0]));
            }
            Assert.AreEqual(0, tree.Rows("nope").Count);
            Assert.AreEqual(0, tree.NodesOf("nope").Count);
            Assert.AreEqual(0, tree.ParentsOf("zzz@1").Count);
        }

        [Test]
        public void 분기_노드_목록()
        {
            var tree = Tree();
            foreach (var kv in J.Obj(V["nodesOf"])) CollectionAssert.AreEqual(J.StrArr(kv.Value), tree.NodesOf(kv.Key), kv.Key);
            foreach (var kv in J.Obj(V["tierNodes"])) CollectionAssert.AreEqual(J.StrArr(kv.Value), tree.TierNodes(kv.Key, 3), kv.Key);
            Assert.AreEqual(145, tree.NodesOf("power").Count + tree.NodesOf("forge").Count + tree.NodesOf("skillpet").Count);
            Assert.AreEqual("forge", tree.BranchOf("offlineCap@4").Id); Assert.IsNull(tree.BranchOf("nope@1"));
        }

        [Test]
        public void 부모_145()
        {
            var tree = Tree();
            var ps = J.Obj(V["parents"]);
            Assert.AreEqual(145, ps.Count);
            foreach (var kv in ps) CollectionAssert.AreEqual(J.StrArr(kv.Value), tree.ParentsOf(kv.Key), kv.Key);
        }

        [Test]
        public void 해금은_바로_위_행의_선을_따른다()
        {
            var tree = Tree("weaponMastery@1", 1, "mountCost@1", 1, "forgeTimer@1", 2, "forgeCost@1", 1, "sellPrice@1", 1);
            foreach (var kv in J.Obj(V["unlock"]))
            {
                var a = J.Arr(kv.Value);
                Assert.AreEqual(J.Bool(a[0]), tree.IsUnlocked(kv.Key), kv.Key + " 해금");
                CollectionAssert.AreEqual(J.StrArr(a[1]), tree.LockedBy(kv.Key), kv.Key + " lockedBy");
            }
        }

        static TechTree Tree4()
        {
            return Tree("weaponMastery@1", 5, "weaponMastery@2", 2, "armorMastery@1", 1, "gearMaxLevel@1", 3, "gearMaxLevel@4", 1,
                "mountCost@1", 5, "mountCost@2", 5, "forgeTimer@1", 5, "forgeTimer@2", 5, "forgeTimer@3", 5, "forgeCost@1", 4,
                "autoForgeSlot@1", 2, "offlineCap@1", 1, "techTimer@1", 5, "techCost@1", 2, "hatchTimer@1", 3, "extraEgg@2", 1, "skillSummonCost@1", 5, "skillSummonCost@2", 5, "skillSummonCost@3", 5);
        }

        [Test]
        public void 타입_총레벨_효과_단위()
        {
            var tree = Tree4();
            foreach (var kv in J.Obj(V["typeLevel"])) Assert.AreEqual(J.Int(kv.Value), tree.TypeLevel(kv.Key), kv.Key + " typeLevel");
            foreach (var kv in J.Obj(V["pct"])) Assert.AreEqual(J.Num(kv.Value), tree.Pct(kv.Key), kv.Key + " pct");
            foreach (var kv in J.Obj(V["unitOf"])) Assert.AreEqual(J.Str(kv.Value), tree.UnitOf(kv.Key), kv.Key + " unit");
            foreach (var kv in J.Obj(V["nodeTotal"])) Assert.AreEqual(J.Num(kv.Value), tree.NodeTotal(kv.Key), kv.Key + " nodeTotal");
            Assert.AreEqual(J.Num(V["pctById"]), tree.Pct("weaponMastery@3"));
            Assert.AreEqual(J.Num(V["pctUnknown"]), tree.Pct("nope"));
            Assert.AreEqual(J.Num(V["nodeTotalUnknown"]), tree.NodeTotal("nope@1"));
            Assert.AreEqual("레벨당", tree.GainNote());
        }

        static double Mult(TechTree t, string name)
        {
            switch (name)
            {
                case "gearAtkMult": return t.GearAtkMult();
                case "gearHpMult": return t.GearHpMult();
                case "gearMaxLevelBonus": return t.GearMaxLevelBonus();
                case "mountDmgMult": return t.MountDmgMult();
                case "mountHpMult": return t.MountHpMult();
                case "mountCostMult": return t.MountCostMult();
                case "extraMountChance": return t.ExtraMountChance();
                case "forgeTimeMult": return t.ForgeTimeMult();
                case "forgeCostMult": return t.ForgeCostMult();
                case "sellPriceMult": return t.SellPriceMult();
                case "thiefHammerMult": return t.ThiefHammerMult();
                case "thiefCoinMult": return t.ThiefCoinMult();
                case "autoForgeSlotBonus": return t.AutoForgeSlotBonus();
                case "freeForgeChance": return t.FreeForgeChance();
                case "offlineCapMult": return t.OfflineCapMult();
                case "offlineCoinMult": return t.OfflineCoinMult();
                case "offlineHammerMult": return t.OfflineHammerMult();
                case "techTimeMult": return t.TechTimeMult();
                case "techCostMult": return t.TechCostMult();
                case "skillDmgMult": return t.SkillDmgMult();
                case "skillPassiveDmgMult": return t.SkillPassiveDmgMult();
                case "skillPassiveHpMult": return t.SkillPassiveHpMult();
                case "petDmgMult": return t.PetDmgMult();
                case "petHpMult": return t.PetHpMult();
                case "skillSummonCostMult": return t.SkillSummonCostMult();
                case "hatchSpeedMult": return t.HatchSpeedMult();
                case "extraEggChance": return t.ExtraEggChance();
                case "dungeonTicketMult": return t.DungeonTicketMult();
                case "dungeonPotionMult": return t.DungeonPotionMult();
            }
            throw new ArgumentException(name);
        }

        [Test]
        public void 효과_배율_29()
        {
            var tree = Tree4();
            var m = J.Obj(V["mults"]);
            Assert.AreEqual(29, m.Count);
            foreach (var kv in m) Assert.AreEqual(J.Num(kv.Value), Mult(tree, kv.Key), kv.Key);
            // 하한 0.1 — 비용 감소형은 −100% 를 넘어도 0.1 아래로 안 간다
            var big = Tree("mountCost@1", 5, "mountCost@2", 5, "mountCost@3", 5, "mountCost@4", 5, "mountCost@5", 5);
            Assert.AreEqual(25, big.Pct("mountCost")); Assert.AreEqual(0.75, big.MountCostMult());
        }

        [Test]
        public void 총_보너스_줄()
        {
            var tree = Tree4();
            var lines = tree.TotalBonuses();
            var exp = J.Arr(V["totalBonuses"]);
            Assert.AreEqual(exp.Count, lines.Count);
            for (int i = 0; i < exp.Count; i++)
            {
                var e = J.Obj(exp[i]);
                Assert.AreEqual(J.Str(e["id"]), lines[i].Id, "줄 " + i);
                Assert.AreEqual(J.Str(e["label"]), lines[i].Label, "줄 " + i);
                Assert.AreEqual(J.Str(e["text"]), lines[i].Text, "줄 " + i);
            }
            Assert.AreEqual(0, Tree().TotalBonuses().Count);
            CollectionAssert.AreEqual(new[] { "weapon", "gloves", "necklace", "ring" }, tree.ExpandSlots("atk"));
            CollectionAssert.AreEqual(new[] { "helmet", "armor", "shoes", "belt" }, tree.ExpandSlots("hp"));
            Assert.AreEqual(8, tree.ExpandSlots("all").Count);
        }

        [Test]
        public void 진행률_만렙_다음비용_단계표기()
        {
            var tree = Tree4();
            foreach (var kv in J.Obj(V["branchProgress"])) Assert.AreEqual(J.Num(kv.Value), tree.BranchProgress(kv.Key), kv.Key);
            Assert.AreEqual(J.Num(V["branchProgressUnknown"]), tree.BranchProgress("nope"));
            foreach (var kv in J.Obj(V["isMax"])) Assert.AreEqual(J.Bool(kv.Value), tree.IsMax(kv.Key), kv.Key);
            foreach (var kv in J.Obj(V["nextCost"])) Assert.AreEqual(J.NumOrNull(kv.Value), tree.NextCost(kv.Key), kv.Key + " nextCost");
            foreach (var kv in J.Obj(V["nextTime"])) Assert.AreEqual(J.NumOrNull(kv.Value), tree.NextTime(kv.Key), kv.Key + " nextTime");
            var tierOf = J.IntArr(V["tierOf"]);
            string[] tierIn = { "a@1", "a@5", "a@6", "a@0", "a", "a@x" };
            for (int i = 0; i < tierIn.Length; i++) Assert.AreEqual(tierOf[i], tree.TierOf(tierIn[i]), tierIn[i]);
            var roman = J.StrArr(V["roman"]);
            int[] romanIn = { 0, 1, 3, 5, 6 };
            for (int i = 0; i < romanIn.Length; i++) Assert.AreEqual(roman[i], tree.Roman(romanIn[i]), "roman " + romanIn[i]);
            Assert.AreEqual(J.Str(V["tierLabel"]), tree.TierLabel("forgeTimer@4"));
            Assert.AreEqual("forgeTimer", tree.TypeOf("forgeTimer@4")); Assert.AreEqual("x", tree.TypeOf("x"));
            Assert.AreEqual("a@3", tree.Nid("a", 3));
        }

        [Test]
        public void 이관_폐기노드_타입키_범위밖_환불()
        {
            var tree = Tree("forgeSpeed", 3, "gearPower", 2, "offlineGain", 4, "eggGain", 5, "petPower", 1, "weaponMastery", 7, "bogus@1", 2, "weaponMastery@9", 3, "armorMastery@2", -1, "mountDmg@3", 2);
            var w = new Wallet { Potions = 100 };
            tree.State.Research = new TechResearch("weaponMastery@9", Now + 5000);
            tree.Ensure(w);
            var e = J.Obj(V["ensure1"]);
            Assert.AreEqual(J.Int(e["count"]), tree.State.Tech.Count, "키 수 = 29 × 5");
            var nz = new Dictionary<string, int>();
            foreach (var kv in tree.State.Tech) if (kv.Value != 0) nz[kv.Key] = kv.Value;
            var expNz = J.Obj(e["tech"]);
            Assert.AreEqual(expNz.Count, nz.Count, "0 아닌 키 수");
            foreach (var kv in expNz) Assert.AreEqual(J.Int(kv.Value), nz.ContainsKey(kv.Key) ? nz[kv.Key] : -999, kv.Key);
            Assert.AreEqual(J.Num(e["potions"]), w.Potions, "사라진 노드 연구 환불(같은 타입 1단계 기준)");
            Assert.IsNull(tree.State.Research);
        }

        [Test]
        public void 이관_폐기타입_연구는_환불없이_취소()
        {
            var tree = Tree("forgeSpeed", 3);
            var w = new Wallet { Potions = 10 };
            tree.State.Research = new TechResearch("forgeSpeed@1", Now + 5000);
            tree.Ensure(w);
            var e = J.Obj(V["ensure2"]);
            Assert.AreEqual(J.Num(e["potions"]), w.Potions); Assert.IsNull(tree.State.Research);
            Assert.AreEqual(J.Int(e["forgeTimer1"]), tree.Level("forgeTimer@1"));
        }

        [Test]
        public void 이관_빈_세이브와_살아있는_연구()
        {
            var tree = new TechTree(Td.Tech, DataDir.Game.Defs, new TechState { Tech = null });
            tree.Ensure(null);
            var e3 = J.Obj(V["ensure3"]);
            Assert.AreEqual(J.Int(e3["count"]), tree.State.Tech.Count); Assert.IsNull(tree.State.Research);
            foreach (var kv in tree.State.Tech) Assert.AreEqual(0, kv.Value, kv.Key);

            var t4 = Tree("weaponMastery@1", 2);
            var w = new Wallet { Potions = 7 };
            t4.State.Research = new TechResearch("weaponMastery@1", Now + 5000);
            t4.Ensure(w);
            var e4 = J.Obj(V["ensure4"]);
            Assert.AreEqual(J.Num(e4["potions"]), w.Potions);
            Assert.AreEqual(J.Str(J.Obj(e4["research"])["id"]), t4.State.Research.Id);
            Assert.AreEqual(J.Num(J.Obj(e4["research"])["endsAt"]), t4.State.Research.EndsAt);
        }

        [Test]
        public void 연구_흐름_시작_동시1건_취소불가_젬건너뛰기_완료()
        {
            var f = J.Obj(V["flow"]);
            var tree = Tree();
            var w = new Wallet { Potions = 200, Gems = 10 };
            Assert.AreEqual(J.Bool(f["canStartLocked"]), tree.CanStart("armorMastery@1", w), "잠긴 노드");
            w.Potions = 10; Assert.AreEqual(J.Bool(f["canStartPoor"]), tree.CanStart("weaponMastery@1", w), "물약 부족"); w.Potions = 200;
            Assert.AreEqual(J.Bool(f["canStart"]), tree.CanStart("weaponMastery@1", w));
            Assert.AreEqual(J.Bool(f["startLocked"]), tree.Start("armorMastery@1", w, Now));
            Assert.AreEqual(J.Bool(f["start"]), tree.Start("weaponMastery@1", w, Now));
            var after = J.Obj(f["afterStart"]);
            Assert.AreEqual(J.Num(after["potions"]), w.Potions, "선결제");
            Assert.AreEqual(J.Str(J.Obj(after["research"])["id"]), tree.State.Research.Id);
            Assert.AreEqual(J.Num(J.Obj(after["research"])["endsAt"]), tree.State.Research.EndsAt);
            Assert.AreEqual(J.Str(after["researchingId"]), tree.ResearchingId());
            Assert.AreEqual(J.Bool(f["canStartBusy"]), tree.CanStart("forgeTimer@1", w), "동시 1건");
            Assert.AreEqual(J.Bool(f["startBusy"]), tree.Start("forgeTimer@1", w, Now));
            Assert.AreEqual(J.Bool(f["cancel"]), tree.Cancel());
            Assert.AreEqual(J.Bool(f["isDoneEarly"]), tree.IsDone(Now));
            Assert.AreEqual(J.Bool(f["claimEarly"]), tree.Claim(Now));
            Assert.AreEqual(J.Num(f["gemSkipCost"]), tree.GemSkipCost(Now));
            w.Gems = 0; Assert.AreEqual(J.Bool(f["gemSkipPoor"]), tree.GemSkip(w, Now), "젬 부족"); w.Gems = 10;
            Assert.AreEqual(J.Bool(f["gemSkip"]), tree.GemSkip(w, Now));
            var skip = J.Obj(f["afterSkip"]);
            Assert.AreEqual(J.Num(skip["gems"]), w.Gems); Assert.AreEqual(J.Bool(skip["isDone"]), tree.IsDone(Now)); Assert.AreEqual(J.Str(skip["research"]), tree.State.Research.Id);
            Assert.IsFalse(tree.CanStart("forgeTimer@1", w), "완료 대기 중에도 동시 1건 가드");
            string claimed;
            Assert.AreEqual(J.Bool(f["claim"]), tree.Claim(Now, out claimed)); Assert.AreEqual("weaponMastery@1", claimed);
            var ac = J.Obj(f["afterClaim"]);
            Assert.AreEqual(J.Int(ac["level"]), tree.Level("weaponMastery@1")); Assert.IsNull(tree.State.Research);
            Assert.AreEqual(J.Bool(ac["canStartArmor"]), tree.CanStart("armorMastery@1", w));
            Assert.AreEqual(J.Num(f["gemSkipCostIdle"]), tree.GemSkipCost(Now));
            Assert.IsNull(tree.ResearchingId());
        }

        [Test]
        public void 젬_건너뛰기_비용은_남은_10분당_1()
        {
            var tree = Tree();
            foreach (var r in J.Arr(V["gemSkipCosts"]))
            {
                var a = J.Arr(r);
                tree.State.Research = new TechResearch("weaponMastery@1", Now + J.Num(a[0]));
                Assert.AreEqual(J.Num(a[1]), tree.GemSkipCost(Now), "남은 " + J.Num(a[0]) + "ms");
            }
            var ft = J.Obj(V["flowTime"]);
            var t2 = Tree(); var w = new Wallet { Potions = 200 };
            t2.Start("weaponMastery@1", w, Now);
            Assert.AreEqual(J.Num(ft["endsAtMinusNow"]), t2.State.Research.EndsAt - Now);
            Assert.AreEqual(J.Num(ft["time1"]), t2.Time("weaponMastery@1", 1));
            Assert.IsFalse(t2.IsDone(t2.State.Research.EndsAt - 1)); Assert.IsTrue(t2.IsDone(t2.State.Research.EndsAt));
        }
    }

    /// <summary>T24 — 승천: 별 배율(Big) · 라인 진행도 · 승천(wipe 콜백 + 횟수) · 별 합계.</summary>
    public class AscensionTests
    {
        static TechData _td;
        static TechData Td { get { return _td ?? (_td = TechData.Load(File.ReadAllText(Path.Combine(DataDir.Path, TechData.File)))); } }
        static JsonObject A { get { return J.Obj(MiniJson.ParseObject(TechTreeTests.Vectors)["ascension"]); } }
        static Ascension Asc() { return new Ascension(Td.Ascension, DataDir.Game.Balance.Skills.MaxLevel, DataDir.Game.Balance.Mounts.MaxLevel); }

        [Test]
        public void 표를_읽는다()
        {
            var t = Td.Ascension;
            Assert.AreEqual(1e9, t.StarMult); Assert.AreEqual(35, t.ForgeLevel);
            CollectionAssert.AreEqual(new[] { "forge", "skill", "pet", "mount" }, t.Lines);
            Assert.AreEqual("장비", t.LineKr["forge"]); Assert.AreEqual(4, t.LineIcon.Count);
            Assert.IsTrue(t.HasLine("pet")); Assert.IsFalse(t.HasLine("gear"));
            Assert.AreEqual(100, DataDir.Game.Balance.Skills.MaxLevel, "스킬·펫 소환 상한 = skillRatesData 행 수");
            Assert.AreEqual(50, DataDir.Game.Balance.Mounts.MaxLevel, "탈것 상한 = mountSummonRates 행 수");
        }

        [Test]
        public void 별_배율은_STAR_MULT의_거듭제곱_Big()
        {
            var a = Asc(); var v = A;
            var exp = J.StrArr(v["starMult"]);
            int[] stars = { 0, 1, 2, 3, 5 };
            for (int i = 0; i < stars.Length; i++) Assert.AreEqual(exp[i], a.StarMult(stars[i]).ToString(), "별 " + stars[i]);
            Assert.AreEqual(J.Str(v["starMultNeg"]), a.StarMult(-1).ToString());
            Assert.IsTrue(a.StarMult(1).Gt(Big.Of(1.75e8)), "승천1 원시장비 > 승천0 디바인 100레벨(격차 1.75e8)");
        }

        [Test]
        public void 진행도_준비_횟수()
        {
            var a = Asc(); var v = A;
            var s = new AscensionState(); s.LineAscend["forge"] = 2; s.LineAscend["skill"] = 0; s.LineAscend["pet"] = 1; s.LineAscend["mount"] = 0;
            var lv = new AscensionLevels { ForgeLevel = 35, SkillSummonLevel = 100, PetSummonLevel = 40, MountLevel = 50 };
            foreach (var kv in J.Obj(v["summonMax"])) Assert.AreEqual(J.Int(kv.Value), a.SummonMax(kv.Key), kv.Key + " summonMax");
            foreach (var kv in J.Obj(v["progress"]))
            {
                var p = a.Progress(kv.Key, lv); var e = J.Obj(kv.Value);
                Assert.AreEqual(J.Int(e["cur"]), p.Cur, kv.Key + " cur"); Assert.AreEqual(J.Int(e["max"]), p.Max, kv.Key + " max");
            }
            foreach (var kv in J.Obj(v["ready"])) Assert.AreEqual(J.Bool(kv.Value), a.Ready(kv.Key, lv), kv.Key + " ready");
            foreach (var kv in J.Obj(v["count"])) Assert.AreEqual(J.Int(kv.Value), a.Count(s, kv.Key), kv.Key + " count");
            Assert.AreEqual(0, a.Count(null, "forge")); Assert.AreEqual(0, a.Count(new AscensionState { LineAscend = null }, "forge"));
            lv.PetSummonLevel = 120;
            var pc = J.Obj(v["progressPetClamped"]);
            Assert.AreEqual(J.Int(pc["cur"]), a.Progress("pet", lv).Cur); Assert.AreEqual(J.Int(pc["max"]), a.Progress("pet", lv).Max);
        }

        [Test]
        public void 승천은_준비된_라인만_wipe_뒤_횟수_1()
        {
            var a = Asc(); var v = A;
            var s = new AscensionState(); s.LineAscend["forge"] = 2; s.LineAscend["skill"] = 0; s.LineAscend["pet"] = 1; s.LineAscend["mount"] = 0;
            var lv = new AscensionLevels { ForgeLevel = 35, SkillSummonLevel = 100, PetSummonLevel = 40, MountLevel = 50 };
            var wiped = new List<string>();
            Action<string> wipe = wiped.Add;
            Assert.AreEqual(J.Bool(v["ascendPet"]), a.Ascend("pet", lv, s, wipe)); Assert.AreEqual(0, wiped.Count, "준비 안 된 라인은 안 지운다");
            Assert.AreEqual(J.Bool(v["ascendForge"]), a.Ascend("forge", lv, s, wipe)); CollectionAssert.AreEqual(new[] { "forge" }, wiped);
            Check(J.Obj(J.Obj(v["afterForge"])["lineAscend"]), s);
            Assert.AreEqual(J.Bool(v["ascendSkill"]), a.Ascend("skill", lv, s, wipe)); Check(J.Obj(J.Obj(v["afterSkill"])["lineAscend"]), s);
            Assert.AreEqual(J.Bool(v["ascendMount"]), a.Ascend("mount", lv, s, wipe)); Check(J.Obj(J.Obj(v["afterMount"])["lineAscend"]), s);
            lv.PetSummonLevel = 120;
            Assert.AreEqual(J.Bool(v["ascendPet2"]), a.Ascend("pet", lv, s, wipe)); Check(J.Obj(J.Obj(v["afterPet"])["lineAscend"]), s);
            CollectionAssert.AreEqual(new[] { "forge", "skill", "mount", "pet" }, wiped);
            Assert.IsTrue(a.Ascend("forge", lv, s, null), "wipe 콜백 없이도 횟수는 오른다"); Assert.AreEqual(4, a.Count(s, "forge"));
        }

        static void Check(JsonObject exp, AscensionState s)
        {
            foreach (var kv in exp) Assert.AreEqual(J.Int(kv.Value), s.LineAscend[kv.Key], kv.Key);
        }

        [Test]
        public void ensure_와_별_합계()
        {
            var a = Asc();
            var v = MiniJson.ParseObject(TechTreeTests.Vectors);
            var s = new AscensionState { LineAscend = null }; a.Ensure(s);
            foreach (var kv in J.Obj(v["ascEnsure"])) Assert.AreEqual(J.Int(kv.Value), s.LineAscend[kv.Key], kv.Key);
            Assert.AreEqual(4, s.LineAscend.Count);
            var s2 = new AscensionState(); s2.LineAscend["forge"] = 3; a.Ensure(s2);
            foreach (var kv in J.Obj(v["ascEnsure2"])) Assert.AreEqual(J.Int(kv.Value), s2.LineAscend[kv.Key], kv.Key);
            var b = Ascension.Breakdown(new[] { 2, 0, 1 }, new[] { 3, 0 }, new[] { 1, 1 }, new[] { 4 });
            var eb = J.Obj(v["starBreakdown"]);
            Assert.AreEqual(J.Int(eb["gear"]), b.Gear); Assert.AreEqual(J.Int(eb["skill"]), b.Skill); Assert.AreEqual(J.Int(eb["pet"]), b.Pet); Assert.AreEqual(J.Int(eb["mount"]), b.Mount);
            Assert.AreEqual(J.Int(v["totalStars"]), b.Total);
            Assert.AreEqual(J.Int(v["totalStars"]), Ascension.TotalStars(new[] { 2, 0, 1 }, new[] { 3, 0 }, new[] { 1, 1 }, new[] { 4 }));
            Assert.AreEqual(0, Ascension.TotalStars(null, null, null, null));
        }
    }

    public partial class TechTreeTests
    {
        /// <summary>정본 techtree.js·ascension.js 출력 벡터(JSON · 생성 방법은 클래스 주석·완료 기록).</summary>
        public const string Vectors = @"{""costTime"":[[""weaponMastery@1"",1,50,20],[""weaponMastery@1"",2,78,35],[""weaponMastery@1"",3,121,62],[""weaponMastery@1"",4,187,108],[""weaponMastery@1"",5,289,188],[""weaponMastery@2"",1,144,68],[""weaponMastery@2"",2,224,119],[""weaponMastery@2"",3,346,209],[""weaponMastery@2"",4,537,365],[""weaponMastery@2"",5,832,638],[""weaponMastery@3"",1,415,232],[""weaponMastery@3"",2,643,405],[""weaponMastery@3"",3,997,709],[""weaponMastery@3"",4,1545,1240],[""weaponMastery@3"",5,2394,2169],[""weaponMastery@4"",1,1195,787],[""weaponMastery@4"",2,1852,1376],[""weaponMastery@4"",3,2870,2408],[""weaponMastery@4"",4,4448,4213],[""weaponMastery@4"",5,6895,7373],[""weaponMastery@5"",1,3440,2673],[""weaponMastery@5"",2,5332,4678],[""weaponMastery@5"",3,8265,8186],[""weaponMastery@5"",4,12810,14324],[""weaponMastery@5"",5,19855,25067],[""extraMount@1"",1,55,20],[""extraMount@1"",2,86,35],[""extraMount@1"",3,133,62],[""extraMount@1"",4,205,108],[""extraMount@1"",5,318,188],[""extraMount@2"",1,159,68],[""extraMount@2"",2,246,119],[""extraMount@2"",3,381,209],[""extraMount@2"",4,590,365],[""extraMount@2"",5,915,638],[""extraMount@3"",1,457,232],[""extraMount@3"",2,708,405],[""extraMount@3"",3,1097,709],[""extraMount@3"",4,1699,1240],[""extraMount@3"",5,2634,2169],[""extraMount@4"",1,1314,787],[""extraMount@4"",2,2037,1376],[""extraMount@4"",3,3157,2408],[""extraMount@4"",4,4893,4213],[""extraMount@4"",5,7584,7373],[""extraMount@5"",1,3784,2673],[""extraMount@5"",2,5865,4678],[""extraMount@5"",3,9091,8186],[""extraMount@5"",4,14091,14324],[""extraMount@5"",5,21841,25067],[""forgeTimer@1"",1,40,20],[""forgeTimer@1"",2,62,35],[""forgeTimer@1"",3,97,62],[""forgeTimer@1"",4,149,108],[""forgeTimer@1"",5,231,188],[""forgeTimer@2"",1,116,68],[""forgeTimer@2"",2,179,119],[""forgeTimer@2"",3,277,209],[""forgeTimer@2"",4,429,365],[""forgeTimer@2"",5,665,638],[""forgeTimer@3"",1,332,232],[""forgeTimer@3"",2,515,405],[""forgeTimer@3"",3,798,709],[""forgeTimer@3"",4,1236,1240],[""forgeTimer@3"",5,1916,2169],[""forgeTimer@4"",1,956,787],[""forgeTimer@4"",2,1482,1376],[""forgeTimer@4"",3,2296,2408],[""forgeTimer@4"",4,3559,4213],[""forgeTimer@4"",5,5516,7373],[""forgeTimer@5"",1,2752,2673],[""forgeTimer@5"",2,4266,4678],[""forgeTimer@5"",3,6612,8186],[""forgeTimer@5"",4,10248,14324],[""forgeTimer@5"",5,15884,25067],[""offlineHammer@1"",1,60,20],[""offlineHammer@1"",2,93,35],[""offlineHammer@1"",3,145,62],[""offlineHammer@1"",4,224,108],[""offlineHammer@1"",5,347,188],[""offlineHammer@2"",1,173,68],[""offlineHammer@2"",2,268,119],[""offlineHammer@2"",3,416,209],[""offlineHammer@2"",4,644,365],[""offlineHammer@2"",5,998,638],[""offlineHammer@3"",1,498,232],[""offlineHammer@3"",2,772,405],[""offlineHammer@3"",3,1196,709],[""offlineHammer@3"",4,1854,1240],[""offlineHammer@3"",5,2873,2169],[""offlineHammer@4"",1,1434,787],[""offlineHammer@4"",2,2222,1376],[""offlineHammer@4"",3,3444,2408],[""offlineHammer@4"",4,5338,4213],[""offlineHammer@4"",5,8273,7373],[""offlineHammer@5"",1,4128,2673],[""offlineHammer@5"",2,6399,4678],[""offlineHammer@5"",3,9918,8186],[""offlineHammer@5"",4,15372,14324],[""offlineHammer@5"",5,23826,25067],[""techTimer@1"",1,45,20],[""techTimer@1"",2,70,35],[""techTimer@1"",3,109,62],[""techTimer@1"",4,168,108],[""techTimer@1"",5,260,188],[""techTimer@2"",1,130,68],[""techTimer@2"",2,201,119],[""techTimer@2"",3,312,209],[""techTimer@2"",4,483,365],[""techTimer@2"",5,749,638],[""techTimer@3"",1,374,232],[""techTimer@3"",2,579,405],[""techTimer@3"",3,897,709],[""techTimer@3"",4,1390,1240],[""techTimer@3"",5,2155,2169],[""techTimer@4"",1,1075,787],[""techTimer@4"",2,1667,1376],[""techTimer@4"",3,2583,2408],[""techTimer@4"",4,4003,4213],[""techTimer@4"",5,6205,7373],[""techTimer@5"",1,3096,2673],[""techTimer@5"",2,4799,4678],[""techTimer@5"",3,7438,8186],[""techTimer@5"",4,11529,14324],[""techTimer@5"",5,17870,25067],[""dungeonPotion@1"",1,50,20],[""dungeonPotion@1"",2,78,35],[""dungeonPotion@1"",3,121,62],[""dungeonPotion@1"",4,187,108],[""dungeonPotion@1"",5,289,188],[""dungeonPotion@2"",1,144,68],[""dungeonPotion@2"",2,224,119],[""dungeonPotion@2"",3,346,209],[""dungeonPotion@2"",4,537,365],[""dungeonPotion@2"",5,832,638],[""dungeonPotion@3"",1,415,232],[""dungeonPotion@3"",2,643,405],[""dungeonPotion@3"",3,997,709],[""dungeonPotion@3"",4,1545,1240],[""dungeonPotion@3"",5,2394,2169],[""dungeonPotion@4"",1,1195,787],[""dungeonPotion@4"",2,1852,1376],[""dungeonPotion@4"",3,2870,2408],[""dungeonPotion@4"",4,4448,4213],[""dungeonPotion@4"",5,6895,7373],[""dungeonPotion@5"",1,3440,2673],[""dungeonPotion@5"",2,5332,4678],[""dungeonPotion@5"",3,8265,8186],[""dungeonPotion@5"",4,12810,14324],[""dungeonPotion@5"",5,19855,25067]],""costTimeDiscounted"":[[""weaponMastery@1"",1,42,16],[""weaponMastery@1"",3,101,48],[""weaponMastery@1"",5,243,147],[""forgeTimer@3"",1,279,181],[""forgeTimer@3"",3,670,554],[""forgeTimer@3"",5,1609,1695],[""dungeonPotion@5"",1,2890,2089],[""dungeonPotion@5"",3,6942,6395],[""dungeonPotion@5"",5,16679,19584],[""techCost@1"",1,38,16],[""techCost@1"",3,91,48],[""techCost@1"",5,219,147]],""techCostMult"":0.84,""techTimeMult"":0.78125,""rows"":{""power"":[[[""weaponMastery@1""],1,true,false],[[""armorMastery@1"",""gearMaxLevel@1""],1,false,false],[[""mountDmg@1"",""mountHp@1""],1,false,false],[[""mountCost@1"",""extraMount@1""],1,false,true],[[""weaponMastery@2""],2,true,false],[[""armorMastery@2"",""gearMaxLevel@2""],2,false,false],[[""mountDmg@2"",""mountHp@2""],2,false,false],[[""mountCost@2"",""extraMount@2""],2,false,true],[[""weaponMastery@3""],3,true,false],[[""armorMastery@3"",""gearMaxLevel@3""],3,false,false],[[""mountDmg@3"",""mountHp@3""],3,false,false],[[""mountCost@3"",""extraMount@3""],3,false,true],[[""weaponMastery@4""],4,true,false],[[""armorMastery@4"",""gearMaxLevel@4""],4,false,false],[[""mountDmg@4"",""mountHp@4""],4,false,false],[[""mountCost@4"",""extraMount@4""],4,false,true],[[""weaponMastery@5""],5,true,false],[[""armorMastery@5"",""gearMaxLevel@5""],5,false,false],[[""mountDmg@5"",""mountHp@5""],5,false,false],[[""mountCost@5""],5,false,false],[[""extraMount@5""],5,false,true]],""forge"":[[[""forgeTimer@1""],1,true,false],[[""forgeCost@1"",""sellPrice@1""],1,false,false],[[""thiefHammer@1"",""thiefCoin@1""],1,false,false],[[""autoForgeSlot@1"",""freeForge@1""],1,false,false],[[""offlineCap@1"",""offlineCoin@1""],1,false,false],[[""offlineHammer@1""],1,false,true],[[""forgeTimer@2""],2,true,false],[[""forgeCost@2"",""sellPrice@2""],2,false,false],[[""thiefHammer@2"",""thiefCoin@2""],2,false,false],[[""autoForgeSlot@2"",""freeForge@2""],2,false,false],[[""offlineCap@2"",""offlineCoin@2""],2,false,false],[[""offlineHammer@2""],2,false,true],[[""forgeTimer@3""],3,true,false],[[""forgeCost@3"",""sellPrice@3""],3,false,false],[[""thiefHammer@3"",""thiefCoin@3""],3,false,false],[[""autoForgeSlot@3"",""freeForge@3""],3,false,false],[[""offlineCap@3"",""offlineCoin@3""],3,false,false],[[""offlineHammer@3""],3,false,true],[[""forgeTimer@4""],4,true,false],[[""forgeCost@4"",""sellPrice@4""],4,false,false],[[""thiefHammer@4"",""thiefCoin@4""],4,false,false],[[""autoForgeSlot@4"",""freeForge@4""],4,false,false],[[""offlineCap@4"",""offlineCoin@4""],4,false,false],[[""offlineHammer@4""],4,false,true],[[""forgeTimer@5""],5,true,false],[[""forgeCost@5"",""sellPrice@5""],5,false,false],[[""thiefHammer@5"",""thiefCoin@5""],5,false,false],[[""autoForgeSlot@5"",""freeForge@5""],5,false,false],[[""offlineCap@5"",""offlineCoin@5""],5,false,false],[[""offlineHammer@5""],5,false,true]],""skillpet"":[[[""techTimer@1""],1,true,false],[[""skillDmg@1"",""skillPassiveDmg@1""],1,false,false],[[""skillPassiveHp@1"",""techCost@1""],1,false,false],[[""petHp@1"",""petDmg@1""],1,false,false],[[""skillSummonCost@1"",""hatchTimer@1""],1,false,false],[[""extraEgg@1"",""dungeonTicket@1""],1,false,false],[[""dungeonPotion@1""],1,false,true],[[""techTimer@2""],2,true,false],[[""skillDmg@2"",""skillPassiveDmg@2""],2,false,false],[[""skillPassiveHp@2"",""techCost@2""],2,false,false],[[""petHp@2"",""petDmg@2""],2,false,false],[[""skillSummonCost@2"",""hatchTimer@2""],2,false,false],[[""extraEgg@2"",""dungeonTicket@2""],2,false,false],[[""dungeonPotion@2""],2,false,true],[[""techTimer@3""],3,true,false],[[""skillDmg@3"",""skillPassiveDmg@3""],3,false,false],[[""skillPassiveHp@3"",""techCost@3""],3,false,false],[[""petHp@3"",""petDmg@3""],3,false,false],[[""skillSummonCost@3"",""hatchTimer@3""],3,false,false],[[""extraEgg@3"",""dungeonTicket@3""],3,false,false],[[""dungeonPotion@3""],3,false,true],[[""techTimer@4""],4,true,false],[[""skillDmg@4"",""skillPassiveDmg@4""],4,false,false],[[""skillPassiveHp@4"",""techCost@4""],4,false,false],[[""petHp@4"",""petDmg@4""],4,false,false],[[""skillSummonCost@4"",""hatchTimer@4""],4,false,false],[[""extraEgg@4"",""dungeonTicket@4""],4,false,false],[[""dungeonPotion@4""],4,false,true],[[""techTimer@5""],5,true,false],[[""skillDmg@5"",""skillPassiveDmg@5""],5,false,false],[[""skillPassiveHp@5"",""techCost@5""],5,false,false],[[""petHp@5"",""petDmg@5""],5,false,false],[[""skillSummonCost@5"",""hatchTimer@5""],5,false,false],[[""extraEgg@5"",""dungeonTicket@5""],5,false,false],[[""dungeonPotion@5""],5,false,true]]},""parents"":{""weaponMastery@1"":[],""armorMastery@1"":[""weaponMastery@1""],""gearMaxLevel@1"":[""weaponMastery@1""],""mountDmg@1"":[""armorMastery@1""],""mountHp@1"":[""gearMaxLevel@1""],""mountCost@1"":[""mountDmg@1""],""extraMount@1"":[""mountHp@1""],""weaponMastery@2"":[""mountCost@1"",""extraMount@1""],""armorMastery@2"":[""weaponMastery@2""],""gearMaxLevel@2"":[""weaponMastery@2""],""mountDmg@2"":[""armorMastery@2""],""mountHp@2"":[""gearMaxLevel@2""],""mountCost@2"":[""mountDmg@2""],""extraMount@2"":[""mountHp@2""],""weaponMastery@3"":[""mountCost@2"",""extraMount@2""],""armorMastery@3"":[""weaponMastery@3""],""gearMaxLevel@3"":[""weaponMastery@3""],""mountDmg@3"":[""armorMastery@3""],""mountHp@3"":[""gearMaxLevel@3""],""mountCost@3"":[""mountDmg@3""],""extraMount@3"":[""mountHp@3""],""weaponMastery@4"":[""mountCost@3"",""extraMount@3""],""armorMastery@4"":[""weaponMastery@4""],""gearMaxLevel@4"":[""weaponMastery@4""],""mountDmg@4"":[""armorMastery@4""],""mountHp@4"":[""gearMaxLevel@4""],""mountCost@4"":[""mountDmg@4""],""extraMount@4"":[""mountHp@4""],""weaponMastery@5"":[""mountCost@4"",""extraMount@4""],""armorMastery@5"":[""weaponMastery@5""],""gearMaxLevel@5"":[""weaponMastery@5""],""mountDmg@5"":[""armorMastery@5""],""mountHp@5"":[""gearMaxLevel@5""],""mountCost@5"":[""mountDmg@5"",""mountHp@5""],""extraMount@5"":[""mountCost@5""],""forgeTimer@1"":[],""forgeCost@1"":[""forgeTimer@1""],""sellPrice@1"":[""forgeTimer@1""],""thiefHammer@1"":[""forgeCost@1""],""thiefCoin@1"":[""sellPrice@1""],""autoForgeSlot@1"":[""thiefHammer@1""],""freeForge@1"":[""thiefCoin@1""],""offlineCap@1"":[""autoForgeSlot@1""],""offlineCoin@1"":[""freeForge@1""],""offlineHammer@1"":[""offlineCap@1"",""offlineCoin@1""],""forgeTimer@2"":[""offlineHammer@1""],""forgeCost@2"":[""forgeTimer@2""],""sellPrice@2"":[""forgeTimer@2""],""thiefHammer@2"":[""forgeCost@2""],""thiefCoin@2"":[""sellPrice@2""],""autoForgeSlot@2"":[""thiefHammer@2""],""freeForge@2"":[""thiefCoin@2""],""offlineCap@2"":[""autoForgeSlot@2""],""offlineCoin@2"":[""freeForge@2""],""offlineHammer@2"":[""offlineCap@2"",""offlineCoin@2""],""forgeTimer@3"":[""offlineHammer@2""],""forgeCost@3"":[""forgeTimer@3""],""sellPrice@3"":[""forgeTimer@3""],""thiefHammer@3"":[""forgeCost@3""],""thiefCoin@3"":[""sellPrice@3""],""autoForgeSlot@3"":[""thiefHammer@3""],""freeForge@3"":[""thiefCoin@3""],""offlineCap@3"":[""autoForgeSlot@3""],""offlineCoin@3"":[""freeForge@3""],""offlineHammer@3"":[""offlineCap@3"",""offlineCoin@3""],""forgeTimer@4"":[""offlineHammer@3""],""forgeCost@4"":[""forgeTimer@4""],""sellPrice@4"":[""forgeTimer@4""],""thiefHammer@4"":[""forgeCost@4""],""thiefCoin@4"":[""sellPrice@4""],""autoForgeSlot@4"":[""thiefHammer@4""],""freeForge@4"":[""thiefCoin@4""],""offlineCap@4"":[""autoForgeSlot@4""],""offlineCoin@4"":[""freeForge@4""],""offlineHammer@4"":[""offlineCap@4"",""offlineCoin@4""],""forgeTimer@5"":[""offlineHammer@4""],""forgeCost@5"":[""forgeTimer@5""],""sellPrice@5"":[""forgeTimer@5""],""thiefHammer@5"":[""forgeCost@5""],""thiefCoin@5"":[""sellPrice@5""],""autoForgeSlot@5"":[""thiefHammer@5""],""freeForge@5"":[""thiefCoin@5""],""offlineCap@5"":[""autoForgeSlot@5""],""offlineCoin@5"":[""freeForge@5""],""offlineHammer@5"":[""offlineCap@5"",""offlineCoin@5""],""techTimer@1"":[],""skillDmg@1"":[""techTimer@1""],""skillPassiveDmg@1"":[""techTimer@1""],""skillPassiveHp@1"":[""skillDmg@1""],""techCost@1"":[""skillPassiveDmg@1""],""petHp@1"":[""skillPassiveHp@1""],""petDmg@1"":[""techCost@1""],""skillSummonCost@1"":[""petHp@1""],""hatchTimer@1"":[""petDmg@1""],""extraEgg@1"":[""skillSummonCost@1""],""dungeonTicket@1"":[""hatchTimer@1""],""dungeonPotion@1"":[""extraEgg@1"",""dungeonTicket@1""],""techTimer@2"":[""dungeonPotion@1""],""skillDmg@2"":[""techTimer@2""],""skillPassiveDmg@2"":[""techTimer@2""],""skillPassiveHp@2"":[""skillDmg@2""],""techCost@2"":[""skillPassiveDmg@2""],""petHp@2"":[""skillPassiveHp@2""],""petDmg@2"":[""techCost@2""],""skillSummonCost@2"":[""petHp@2""],""hatchTimer@2"":[""petDmg@2""],""extraEgg@2"":[""skillSummonCost@2""],""dungeonTicket@2"":[""hatchTimer@2""],""dungeonPotion@2"":[""extraEgg@2"",""dungeonTicket@2""],""techTimer@3"":[""dungeonPotion@2""],""skillDmg@3"":[""techTimer@3""],""skillPassiveDmg@3"":[""techTimer@3""],""skillPassiveHp@3"":[""skillDmg@3""],""techCost@3"":[""skillPassiveDmg@3""],""petHp@3"":[""skillPassiveHp@3""],""petDmg@3"":[""techCost@3""],""skillSummonCost@3"":[""petHp@3""],""hatchTimer@3"":[""petDmg@3""],""extraEgg@3"":[""skillSummonCost@3""],""dungeonTicket@3"":[""hatchTimer@3""],""dungeonPotion@3"":[""extraEgg@3"",""dungeonTicket@3""],""techTimer@4"":[""dungeonPotion@3""],""skillDmg@4"":[""techTimer@4""],""skillPassiveDmg@4"":[""techTimer@4""],""skillPassiveHp@4"":[""skillDmg@4""],""techCost@4"":[""skillPassiveDmg@4""],""petHp@4"":[""skillPassiveHp@4""],""petDmg@4"":[""techCost@4""],""skillSummonCost@4"":[""petHp@4""],""hatchTimer@4"":[""petDmg@4""],""extraEgg@4"":[""skillSummonCost@4""],""dungeonTicket@4"":[""hatchTimer@4""],""dungeonPotion@4"":[""extraEgg@4"",""dungeonTicket@4""],""techTimer@5"":[""dungeonPotion@4""],""skillDmg@5"":[""techTimer@5""],""skillPassiveDmg@5"":[""techTimer@5""],""skillPassiveHp@5"":[""skillDmg@5""],""techCost@5"":[""skillPassiveDmg@5""],""petHp@5"":[""skillPassiveHp@5""],""petDmg@5"":[""techCost@5""],""skillSummonCost@5"":[""petHp@5""],""hatchTimer@5"":[""petDmg@5""],""extraEgg@5"":[""skillSummonCost@5""],""dungeonTicket@5"":[""hatchTimer@5""],""dungeonPotion@5"":[""extraEgg@5"",""dungeonTicket@5""]},""nodesOf"":{""power"":[""weaponMastery@1"",""armorMastery@1"",""gearMaxLevel@1"",""mountDmg@1"",""mountHp@1"",""mountCost@1"",""extraMount@1"",""weaponMastery@2"",""armorMastery@2"",""gearMaxLevel@2"",""mountDmg@2"",""mountHp@2"",""mountCost@2"",""extraMount@2"",""weaponMastery@3"",""armorMastery@3"",""gearMaxLevel@3"",""mountDmg@3"",""mountHp@3"",""mountCost@3"",""extraMount@3"",""weaponMastery@4"",""armorMastery@4"",""gearMaxLevel@4"",""mountDmg@4"",""mountHp@4"",""mountCost@4"",""extraMount@4"",""weaponMastery@5"",""armorMastery@5"",""gearMaxLevel@5"",""mountDmg@5"",""mountHp@5"",""mountCost@5"",""extraMount@5""],""forge"":[""forgeTimer@1"",""forgeCost@1"",""sellPrice@1"",""thiefHammer@1"",""thiefCoin@1"",""autoForgeSlot@1"",""freeForge@1"",""offlineCap@1"",""offlineCoin@1"",""offlineHammer@1"",""forgeTimer@2"",""forgeCost@2"",""sellPrice@2"",""thiefHammer@2"",""thiefCoin@2"",""autoForgeSlot@2"",""freeForge@2"",""offlineCap@2"",""offlineCoin@2"",""offlineHammer@2"",""forgeTimer@3"",""forgeCost@3"",""sellPrice@3"",""thiefHammer@3"",""thiefCoin@3"",""autoForgeSlot@3"",""freeForge@3"",""offlineCap@3"",""offlineCoin@3"",""offlineHammer@3"",""forgeTimer@4"",""forgeCost@4"",""sellPrice@4"",""thiefHammer@4"",""thiefCoin@4"",""autoForgeSlot@4"",""freeForge@4"",""offlineCap@4"",""offlineCoin@4"",""offlineHammer@4"",""forgeTimer@5"",""forgeCost@5"",""sellPrice@5"",""thiefHammer@5"",""thiefCoin@5"",""autoForgeSlot@5"",""freeForge@5"",""offlineCap@5"",""offlineCoin@5"",""offlineHammer@5""],""skillpet"":[""techTimer@1"",""skillDmg@1"",""skillPassiveDmg@1"",""skillPassiveHp@1"",""techCost@1"",""petHp@1"",""petDmg@1"",""skillSummonCost@1"",""hatchTimer@1"",""extraEgg@1"",""dungeonTicket@1"",""dungeonPotion@1"",""techTimer@2"",""skillDmg@2"",""skillPassiveDmg@2"",""skillPassiveHp@2"",""techCost@2"",""petHp@2"",""petDmg@2"",""skillSummonCost@2"",""hatchTimer@2"",""extraEgg@2"",""dungeonTicket@2"",""dungeonPotion@2"",""techTimer@3"",""skillDmg@3"",""skillPassiveDmg@3"",""skillPassiveHp@3"",""techCost@3"",""petHp@3"",""petDmg@3"",""skillSummonCost@3"",""hatchTimer@3"",""extraEgg@3"",""dungeonTicket@3"",""dungeonPotion@3"",""techTimer@4"",""skillDmg@4"",""skillPassiveDmg@4"",""skillPassiveHp@4"",""techCost@4"",""petHp@4"",""petDmg@4"",""skillSummonCost@4"",""hatchTimer@4"",""extraEgg@4"",""dungeonTicket@4"",""dungeonPotion@4"",""techTimer@5"",""skillDmg@5"",""skillPassiveDmg@5"",""skillPassiveHp@5"",""techCost@5"",""petHp@5"",""petDmg@5"",""skillSummonCost@5"",""hatchTimer@5"",""extraEgg@5"",""dungeonTicket@5"",""dungeonPotion@5""]},""tierNodes"":{""power"":[""weaponMastery@3"",""armorMastery@3"",""gearMaxLevel@3"",""mountDmg@3"",""mountHp@3"",""mountCost@3"",""extraMount@3""],""forge"":[""forgeTimer@3"",""forgeCost@3"",""sellPrice@3"",""thiefHammer@3"",""thiefCoin@3"",""autoForgeSlot@3"",""freeForge@3"",""offlineCap@3"",""offlineCoin@3"",""offlineHammer@3""],""skillpet"":[""techTimer@3"",""skillDmg@3"",""skillPassiveDmg@3"",""skillPassiveHp@3"",""techCost@3"",""petHp@3"",""petDmg@3"",""skillSummonCost@3"",""hatchTimer@3"",""extraEgg@3"",""dungeonTicket@3"",""dungeonPotion@3""]},""rowsUnknown"":[],""nodesOfUnknown"":[],""parentsUnknown"":[],""unlock"":{""weaponMastery@1"":[true,[]],""armorMastery@1"":[true,[]],""gearMaxLevel@1"":[true,[]],""mountDmg@1"":[false,[""armorMastery@1""]],""weaponMastery@2"":[false,[""extraMount@1""]],""armorMastery@2"":[false,[""weaponMastery@2""]],""forgeTimer@2"":[false,[""offlineHammer@1""]],""forgeCost@2"":[false,[""forgeTimer@2""]],""thiefHammer@1"":[true,[]],""thiefCoin@1"":[true,[]],""techTimer@1"":[true,[]],""skillDmg@1"":[false,[""techTimer@1""]]},""typeLevel"":{""weaponMastery"":7,""armorMastery"":1,""gearMaxLevel"":4,""mountDmg"":0,""mountHp"":0,""mountCost"":10,""extraMount"":0,""forgeTimer"":15,""forgeCost"":4,""sellPrice"":0,""thiefHammer"":0,""thiefCoin"":0,""autoForgeSlot"":2,""freeForge"":0,""offlineCap"":1,""offlineCoin"":0,""offlineHammer"":0,""techTimer"":5,""skillDmg"":0,""skillPassiveDmg"":0,""skillPassiveHp"":0,""techCost"":2,""petHp"":0,""petDmg"":0,""skillSummonCost"":15,""hatchTimer"":3,""extraEgg"":1,""dungeonTicket"":0,""dungeonPotion"":0},""pct"":{""weaponMastery"":14,""armorMastery"":2,""gearMaxLevel"":8,""mountDmg"":0,""mountHp"":0,""mountCost"":10,""extraMount"":0,""forgeTimer"":60,""forgeCost"":8,""sellPrice"":0,""thiefHammer"":0,""thiefCoin"":0,""autoForgeSlot"":2,""freeForge"":0,""offlineCap"":16,""offlineCoin"":0,""offlineHammer"":0,""techTimer"":20,""skillDmg"":0,""skillPassiveDmg"":0,""skillPassiveHp"":0,""techCost"":4,""petHp"":0,""petDmg"":0,""skillSummonCost"":15,""hatchTimer"":30,""extraEgg"":2,""dungeonTicket"":0,""dungeonPotion"":0},""nodeTotal"":{""weaponMastery@1"":10,""weaponMastery@2"":4,""gearMaxLevel@4"":2,""skillDmg@1"":0},""unitOf"":{""weaponMastery"":""%"",""armorMastery"":""%"",""gearMaxLevel"":""레벨"",""mountDmg"":""%"",""mountHp"":""%"",""mountCost"":""%"",""extraMount"":""%"",""forgeTimer"":""%"",""forgeCost"":""%"",""sellPrice"":""%"",""thiefHammer"":""%"",""thiefCoin"":""%"",""autoForgeSlot"":""개"",""freeForge"":""%"",""offlineCap"":""%"",""offlineCoin"":""%"",""offlineHammer"":""%"",""techTimer"":""%"",""skillDmg"":""%"",""skillPassiveDmg"":""%"",""skillPassiveHp"":""%"",""techCost"":""%"",""petHp"":""%"",""petDmg"":""%"",""skillSummonCost"":""%"",""hatchTimer"":""%"",""extraEgg"":""%"",""dungeonTicket"":""%"",""dungeonPotion"":""%""},""pctById"":14,""pctUnknown"":0,""nodeTotalUnknown"":0,""mults"":{""gearAtkMult"":1.1400000000000001,""gearHpMult"":1.02,""gearMaxLevelBonus"":8,""mountDmgMult"":1,""mountHpMult"":1,""mountCostMult"":0.9,""extraMountChance"":0,""forgeTimeMult"":0.625,""forgeCostMult"":0.92,""sellPriceMult"":1,""thiefHammerMult"":1,""thiefCoinMult"":1,""autoForgeSlotBonus"":2,""freeForgeChance"":0,""offlineCapMult"":1.16,""offlineCoinMult"":1,""offlineHammerMult"":1,""techTimeMult"":0.8333333333333334,""techCostMult"":0.96,""skillDmgMult"":1,""skillPassiveDmgMult"":1,""skillPassiveHpMult"":1,""petDmgMult"":1,""petHpMult"":1,""skillSummonCostMult"":0.85,""hatchSpeedMult"":0.7692307692307692,""extraEggChance"":0.02,""dungeonTicketMult"":1,""dungeonPotionMult"":1},""totalBonuses"":[{""id"":""weaponMastery"",""label"":""무기 보너스 피해"",""text"":""+14%""},{""id"":""weaponMastery"",""label"":""장갑 보너스 피해"",""text"":""+14%""},{""id"":""weaponMastery"",""label"":""목걸이 보너스 피해"",""text"":""+14%""},{""id"":""weaponMastery"",""label"":""반지 보너스 피해"",""text"":""+14%""},{""id"":""armorMastery"",""label"":""투구 보너스 체력"",""text"":""+2%""},{""id"":""armorMastery"",""label"":""갑옷 보너스 체력"",""text"":""+2%""},{""id"":""armorMastery"",""label"":""신발 보너스 체력"",""text"":""+2%""},{""id"":""armorMastery"",""label"":""벨트 보너스 체력"",""text"":""+2%""},{""id"":""gearMaxLevel"",""label"":""무기 최대 레벨"",""text"":""+8""},{""id"":""gearMaxLevel"",""label"":""투구 최대 레벨"",""text"":""+8""},{""id"":""gearMaxLevel"",""label"":""갑옷 최대 레벨"",""text"":""+8""},{""id"":""gearMaxLevel"",""label"":""장갑 최대 레벨"",""text"":""+8""},{""id"":""gearMaxLevel"",""label"":""목걸이 최대 레벨"",""text"":""+8""},{""id"":""gearMaxLevel"",""label"":""반지 최대 레벨"",""text"":""+8""},{""id"":""gearMaxLevel"",""label"":""신발 최대 레벨"",""text"":""+8""},{""id"":""gearMaxLevel"",""label"":""벨트 최대 레벨"",""text"":""+8""},{""id"":""mountCost"",""label"":""탈것 소환 비용 감소"",""text"":""+10%""},{""id"":""forgeTimer"",""label"":""대장간 업그레이드 속도"",""text"":""+60%""},{""id"":""forgeCost"",""label"":""대장간 업그레이드 비용 감소"",""text"":""+8%""},{""id"":""autoForgeSlot"",""label"":""자동 제련 동시 해머"",""text"":""+2""},{""id"":""offlineCap"",""label"":""최대 오프라인 시간"",""text"":""+16%""},{""id"":""techTimer"",""label"":""기술 연구 속도"",""text"":""+20%""},{""id"":""techCost"",""label"":""기술 연구 비용 감소"",""text"":""+4%""},{""id"":""skillSummonCost"",""label"":""스킬 소환 비용 감소"",""text"":""+15%""},{""id"":""hatchTimer"",""label"":""알 부화 속도"",""text"":""+30%""},{""id"":""extraEgg"",""label"":""추가 알 소환 확률"",""text"":""+2%""}],""branchProgress"":{""power"":12.571428571428573,""forge"":8.799999999999999,""skillpet"":8.666666666666668},""branchProgressUnknown"":0,""isMax"":{""weaponMastery@1"":true,""weaponMastery@2"":false},""nextCost"":{""weaponMastery@1"":null,""weaponMastery@2"":333,""nope@1"":null},""nextTime"":{""weaponMastery@1"":null,""weaponMastery@2"":174},""tierOf"":[1,5,1,1,1,1],""roman"":[""I"",""I"",""III"",""V"",""I""],""tierLabel"":""IV"",""ensure1"":{""tech"":{""mountDmg@3"":2,""weaponMastery@1"":5,""forgeTimer@1"":3,""armorMastery@1"":2,""offlineCoin@1"":4,""offlineHammer@1"":4,""petDmg@1"":1,""petHp@1"":1},""count"":145,""potions"":389,""research"":null},""ensure2"":{""potions"":10,""research"":null,""forgeTimer1"":3},""ensure3"":{""count"":145,""research"":true},""ensure4"":{""potions"":7,""research"":{""id"":""weaponMastery@1"",""endsAt"":1800000005000}},""flow"":{""canStartLocked"":false,""canStartPoor"":false,""canStart"":true,""startLocked"":false,""start"":true,""afterStart"":{""potions"":150,""research"":{""id"":""weaponMastery@1"",""endsAt"":1800000020000},""researchingId"":""weaponMastery@1""},""canStartBusy"":false,""startBusy"":false,""cancel"":false,""isDoneEarly"":false,""claimEarly"":false,""gemSkipCost"":1,""gemSkipPoor"":false,""gemSkip"":true,""afterSkip"":{""gems"":9,""isDone"":true,""research"":""weaponMastery@1""},""claim"":true,""afterClaim"":{""level"":1,""research"":null,""canStartArmor"":true},""gemSkipCostIdle"":0},""gemSkipCosts"":[[0,0],[-5000,0],[1,1],[60000,1],[600000,1],[600001,2],[1530000,3],[3600000,6],[86400000,144]],""flowTime"":{""endsAtMinusNow"":20000,""time1"":20},""ascension"":{""starMult"":[""1e0"",""1e9"",""1e18"",""1e27"",""1e45""],""starMultNeg"":""1e0"",""summonMax"":{""forge"":100,""skill"":100,""pet"":100,""mount"":50},""progress"":{""forge"":{""cur"":35,""max"":35},""skill"":{""cur"":100,""max"":100},""pet"":{""cur"":40,""max"":100},""mount"":{""cur"":50,""max"":50}},""ready"":{""forge"":true,""skill"":true,""pet"":false,""mount"":true},""count"":{""forge"":2,""skill"":0,""pet"":1,""mount"":0},""ascendPet"":false,""ascendForge"":true,""afterForge"":{""forgeLevel"":1,""forgeUpgradeEndsAt"":null,""rollLevel"":{},""lineAscend"":{""forge"":3,""skill"":0,""pet"":1,""mount"":0}},""ascendSkill"":true,""afterSkill"":{""summonCount"":0,""skills"":{},""equippedSkills"":[],""lineAscend"":{""forge"":3,""skill"":1,""pet"":1,""mount"":0}},""ascendMount"":true,""afterMount"":{""mountOpens"":0,""mounts"":[],""activeMounts"":[],""lineAscend"":{""forge"":3,""skill"":1,""pet"":1,""mount"":1}},""progressPetClamped"":{""cur"":100,""max"":100},""ascendPet2"":true,""afterPet"":{""petSummonCount"":0,""pets"":[],""activePets"":[],""lineAscend"":{""forge"":3,""skill"":1,""pet"":2,""mount"":1}}},""ascEnsure"":{""forge"":0,""skill"":0,""pet"":0,""mount"":0},""ascEnsure2"":{""forge"":3,""skill"":0,""pet"":0,""mount"":0},""starBreakdown"":{""gear"":3,""skill"":3,""pet"":2,""mount"":4},""totalStars"":12}";
    }
}
