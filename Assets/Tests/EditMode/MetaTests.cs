using System;
using System.Collections.Generic;
using NUnit.Framework;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.Meta;

namespace Forge.Tests
{
    /// <summary>T25 테스트가 읽는 `meta.json` — <see cref="DataDir"/> 와 같은 폴더.</summary>
    static class MetaDir
    {
        static MetaTable _m;
        public static MetaTable Meta { get { return _m ?? (_m = MetaTable.LoadDirectory(DataDir.Path)); } }
    }

    /// <summary>T25 — meta.json 표: 정본 shop/pass/quests/league/chat 의 개수·젬 금지·순서.</summary>
    public class MetaTableTests
    {
        MetaTable M { get { return MetaDir.Meta; } }

        [Test]
        public void 상점_특가3_젬팩4_젬없음()
        {
            Assert.AreEqual(3, M.Shop.Deals.Count);
            Assert.AreEqual(new[] { "tech", "pet", "mount" }, new[] { M.Shop.Deals[0].Key, M.Shop.Deals[1].Key, M.Shop.Deals[2].Key });
            Assert.AreEqual(4, M.Shop.GemPacks.Count);
            Assert.AreEqual(3300, M.Shop.GemPacks[3].Gems);
            foreach (var d in M.Shop.Deals) Assert.IsFalse(d.Reward.Has("gems"), d.Key);
            Assert.AreEqual(150, M.Shop.Find("tech").Reward["potions"]);
            Assert.AreEqual(300, M.Shop.Find("tech").Reward["hammers"]);
            Assert.IsNull(M.Shop.Find("없음"));
        }

        [Test]
        public void 패스_마일스톤16_무료프리미엄()
        {
            Assert.AreEqual(16, M.Pass.Milestones.Count);
            Assert.AreEqual("1-5", M.Pass.Milestones[0].Stage);
            Assert.AreEqual("10-5", M.Pass.Milestones[15].Stage);
            Assert.AreEqual(500, M.Pass.Milestones[0].Free["coins"]);
            Assert.AreEqual(5, M.Pass.Milestones[0].Premium["gems"]);
            Assert.AreEqual("₩13,900", M.Pass.PremiumPriceKr);
        }

        [Test]
        public void 퀘스트_정의14_젬보상없음_순서고정()
        {
            Assert.AreEqual(14, M.Quests.Defs.Count);
            Assert.AreEqual("craft", M.Quests.Defs[0].Id);
            Assert.AreEqual("keySpend", M.Quests.Defs[13].Id);
            foreach (var d in M.Quests.Defs) Assert.AreNotEqual("gems", d.RwCur, d.Id);
            Assert.AreEqual(20, M.Quests.StepCap);
            Assert.AreEqual("hammers", M.Quests.NoGemFallback);
            Assert.AreEqual(new[] { "coinSpend", "upgradeStart", "gearUpgrade" }, M.Quests.ForgeLocked);
            Assert.AreEqual("코인", M.Quests.CurKr["coins"]);
            Assert.AreEqual("", M.Quests.Def("coinSpend").Unit);
            Assert.IsNull(M.Quests.Def("craft").Unit);
        }

        [Test]
        public void 리그_상수와_순위보상표()
        {
            Assert.AreEqual(5, M.League.TicketMax);
            Assert.AreEqual(20, M.League.BotCount);
            Assert.AreEqual(5, M.League.ChallengeCount);
            Assert.AreEqual(3 * 24 * 3600 * 1000.0, M.League.SeasonMs);
            Assert.AreEqual(50, M.League.StartScore);
            Assert.AreEqual(20, M.League.NamePool.Length);
            Assert.AreEqual(7, M.League.RewardTiers.Count);
            Assert.AreEqual("4-5", M.League.RewardTiers[3].Label);
            Assert.AreEqual(21, M.League.RewardByRank.Count);
            // 원작 rewardForRank(1) · rewardForRank(21) 을 node 로 평가한 값
            var r1 = M.League.RewardByRank[0];
            Assert.AreEqual(3, r1.Mult);
            Assert.AreEqual(new[] { "hammers", "coins", "tickets", "eggCurrency", "potions", "winders" }, new List<string>(r1.Reward.Keys).ToArray());
            Assert.AreEqual(900, r1.Reward["hammers"]); Assert.AreEqual(18000, r1.Reward["coins"]); Assert.AreEqual(120, r1.Reward["tickets"]);
            Assert.AreEqual(360, r1.Reward["eggCurrency"]); Assert.AreEqual(450, r1.Reward["potions"]); Assert.AreEqual(180, r1.Reward["winders"]);
            var r21 = M.League.RewardByRank[20];
            Assert.AreEqual(0.6, r21.Mult); Assert.AreEqual(180, r21.Reward["hammers"]); Assert.AreEqual(3600, r21.Reward["coins"]);
            Assert.AreEqual(2.4, M.League.RewardByRank[1].Mult); Assert.AreEqual(2, M.League.RewardByRank[2].Mult);
            Assert.AreEqual(1.6, M.League.RewardByRank[4].Mult); Assert.AreEqual(1.2, M.League.RewardByRank[9].Mult); Assert.AreEqual(1, M.League.RewardByRank[19].Mult);
        }

        [Test]
        public void 채팅표와_아바타와_진행상수()
        {
            Assert.AreEqual(60, M.Chat.MaxMessages);
            Assert.AreEqual(10, M.Chat.ClanTags.Length);
            Assert.AreEqual(18, M.Chat.Names.Length);
            Assert.AreEqual(24, M.Chat.Lines.Length);
            Assert.AreEqual(3, M.Chat.LongLines.Length);
            Assert.AreEqual(24, M.Avatars.Pool.Length);
            Assert.AreEqual("🛡️", M.Avatars.DefaultAvatar);
            Assert.AreEqual(25, M.State.ChaptersPerCycle);
            Assert.AreEqual(10, M.State.StagesPerChapter);
            Assert.AreEqual(3, M.State.MaxDifficulty);
            Assert.AreEqual(new[] { "", "어려움", "매우 어려움", "헬" }, M.State.DifficultyNames);
                    }
    }

    /// <summary>T25 — 일일 리셋 키(`Dungeons.resetDateKey` · 로컬 09:00). 절대 챕터는 T13 `SaveDefs.AbsChapter` 가 쥔다.</summary>
    public class DailyResetTests
    {
        [TestCase(2026, 9, 12, 18, 47, "Sat Sep 12 2026")]
        [TestCase(2026, 9, 12, 8, 59, "Fri Sep 11 2026")]
        [TestCase(2026, 9, 12, 9, 0, "Sat Sep 12 2026")]
        [TestCase(2026, 9, 2, 12, 0, "Wed Sep 02 2026")]
        [TestCase(2026, 1, 1, 3, 0, "Wed Dec 31 2025")]
        public void 리셋키는_JS_toDateString_꼴_09시_경계(int y, int mo, int d, int h, int mi, string expect)
        {
            Assert.AreEqual(expect, DailyReset.ResetDateKey(new DateTime(y, mo, d, h, mi, 0)));
        }

    }

    /// <summary>T25 — 상점: 하루 1회 무료 수령 · 09:00 리셋 · 젬은 어떤 경로로도 안 들어온다.</summary>
    public class ShopTests
    {
        [Test]
        public void 특가는_하루_한번_리셋되면_다시()
        {
            var shop = new Shop(MetaDir.Meta.Shop);
            var s = new ShopState();
            var w = new KeyedWallet();
            Assert.IsFalse(shop.Ensure(s, "Sat Sep 12 2026"), "첫 ensure 는 리셋이 아니다");
            Assert.IsTrue(shop.CanClaim(s, "tech"));
            Assert.IsTrue(shop.ClaimDeal(s, w, "tech"));
            Assert.AreEqual(150, w.Get("potions"));
            Assert.AreEqual(300, w.Get("hammers"));
            Assert.IsFalse(shop.ClaimDeal(s, w, "tech"), "같은 날 두 번은 안 된다");
            Assert.IsFalse(shop.ClaimDeal(s, w, "없는키"));
            Assert.IsTrue(shop.ClaimDeal(s, w, "mount"));
            Assert.AreEqual(2150, w.Get("winders"));
            Assert.AreEqual(4300, w.Get("hammers"));
            Assert.IsFalse(shop.Ensure(s, "Sat Sep 12 2026"));
            Assert.IsTrue(shop.Ensure(s, "Sun Sep 13 2026"), "날짜 키가 바뀌면 리셋");
            Assert.IsTrue(shop.CanClaim(s, "tech"));
            Assert.IsTrue(shop.ClaimDeal(s, w, "tech"));
            Assert.AreEqual(0, w.Get("gems"));
        }

        [Test]
        public void 세이브_루트_지갑은_없던_키를_0에서_더한다()
        {
            var root = MiniJson.ParseObject("{\"coins\": 5}");
            var w = new SaveStateWallet(new Forge.Core.Save.SaveState(root));
            Assert.AreEqual(5, w.Get("coins"));
            Assert.AreEqual(0, w.Get("winders"));
            new Shop(MetaDir.Meta.Shop).ClaimDeal(new ShopState(), w, "mount");
            Assert.AreEqual(2150, w.Get("winders"));
            Assert.AreEqual(4000, w.Get("hammers"));
            Assert.AreEqual(2150.0, root["winders"]);
        }

        [Test]
        public void 보상에_젬이_되살아나도_가산하지_않는다()
        {
            var t = new ShopTable { Deals = new List<ShopDeal>(), GemPacks = new List<GemPack>() };
            var reward = new OrderedMap<double>(); reward.Add("gems", 99); reward.Add("coins", 1);
            t.Deals.Add(new ShopDeal { Key = "x", Reward = reward });
            var shop = new Shop(t);
            var w = new KeyedWallet();
            Assert.IsTrue(shop.ClaimDeal(new ShopState(), w, "x"));
            Assert.AreEqual(0, w.Get("gems"));
            Assert.AreEqual(1, w.Get("coins"));
        }
    }

    /// <summary>T25 — 패스: 스테이지 값 · 절대 챕터로 도달 판정 · 무료만 지급.</summary>
    public class PassTests
    {
        Pass P { get { return new Pass(MetaDir.Meta.Pass, MetaDir.Meta.State); } }

        [TestCase("1-5", 5)]
        [TestCase("3-5", 25)]
        [TestCase("10-5", 95)]
        public void 스테이지값(string key, int expect) { Assert.AreEqual(expect, P.StageValue(key)); }

        [Test]
        public void 도달은_최고_절대챕터로_잰다()
        {
            var p = P;
            Assert.IsTrue(p.Reached("1-5", 1, 5));
            Assert.IsFalse(p.Reached("1-5", 1, 4));
            Assert.IsTrue(p.Reached("3-5", 3, 5));
            Assert.IsTrue(p.Reached("10-5", 26, 1), "어려움 1-1(절대 26)에서도 마일스톤이 도로 잠기지 않는다");
        }

        [Test]
        public void 수령은_무료칸만_한번()
        {
            var p = P;
            var s = new PassState();
            var w = new KeyedWallet();
            Assert.IsFalse(p.Claim(s, w, "1-5", 1, 4), "미도달");
            Assert.IsTrue(p.CanClaim(s, "1-5", 1, 5));
            Assert.IsTrue(p.Claim(s, w, "1-5", 1, 5));
            Assert.AreEqual(500, w.Get("coins"));
            Assert.AreEqual(50, w.Get("hammers"));
            Assert.AreEqual(0, w.Get("gems"), "프리미엄 칸은 지급하지 않는다");
            Assert.IsFalse(p.Claim(s, w, "1-5", 9, 9), "이미 수령");
            Assert.IsFalse(p.Claim(s, w, "없음", 99, 9));
            Assert.IsTrue(p.Claimed(s, "1-5"));
        }
    }

    /// <summary>T25 — 퀘스트: 정본 `Quests.ensure()` 를 tier 0·1·5·20·25 로 돌린 벡터와 대조 · bump/claim/claimAll · 만렙 필터.</summary>
    public class QuestTests
    {
        Quests Q(Func<bool> forge = null) { return new Quests(MetaDir.Meta.Quests, forge); }

        // 원작 node 벡터: [id, need, cur, amt] × 14 (tier 0 / 1 / 5 / 20 / 25 → 25 는 STEP_CAP 20 에서 멈춘다)
        static readonly object[][] T0 = { new object[] { "craft", 10, "coins", 400 }, new object[] { "sellGear", 8, "hammers", 6 }, new object[] { "equipGear", 4, "coins", 600 }, new object[] { "coinSpend", 2000, "hammers", 10 }, new object[] { "upgradeStart", 1, "coins", 600 }, new object[] { "gearUpgrade", 1, "hammers", 10 }, new object[] { "skillSummon", 5, "tickets", 12 }, new object[] { "techDone", 1, "potions", 4 }, new object[] { "petHatch", 3, "eggCurrency", 15 }, new object[] { "petMerge", 1, "eggCurrency", 25 }, new object[] { "mountSummon", 3, "winders", 60 }, new object[] { "mountMerge", 1, "winders", 10 }, new object[] { "dungeonClear", 1, "potions", 3 }, new object[] { "keySpend", 2, "potions", 4 } };
        static readonly object[][] T1 = { new object[] { "craft", 13, "coins", 520 }, new object[] { "sellGear", 10, "hammers", 8 }, new object[] { "equipGear", 5, "coins", 750 }, new object[] { "coinSpend", 2800, "hammers", 14 }, new object[] { "upgradeStart", 2, "coins", 1200 }, new object[] { "gearUpgrade", 2, "hammers", 20 }, new object[] { "skillSummon", 7, "tickets", 17 }, new object[] { "techDone", 2, "potions", 8 }, new object[] { "petHatch", 4, "eggCurrency", 20 }, new object[] { "petMerge", 2, "eggCurrency", 50 }, new object[] { "mountSummon", 4, "winders", 80 }, new object[] { "mountMerge", 2, "winders", 20 }, new object[] { "dungeonClear", 2, "potions", 6 }, new object[] { "keySpend", 3, "potions", 6 } };
        static readonly object[][] T5 = { new object[] { "craft", 25, "coins", 1000 }, new object[] { "sellGear", 18, "hammers", 14 }, new object[] { "equipGear", 9, "coins", 1350 }, new object[] { "coinSpend", 6000, "hammers", 30 }, new object[] { "upgradeStart", 6, "coins", 3600 }, new object[] { "gearUpgrade", 6, "hammers", 60 }, new object[] { "skillSummon", 15, "tickets", 36 }, new object[] { "techDone", 6, "potions", 24 }, new object[] { "petHatch", 8, "eggCurrency", 40 }, new object[] { "petMerge", 6, "eggCurrency", 150 }, new object[] { "mountSummon", 8, "winders", 160 }, new object[] { "mountMerge", 6, "winders", 60 }, new object[] { "dungeonClear", 6, "potions", 18 }, new object[] { "keySpend", 7, "potions", 14 } };
        static readonly object[][] T20 = { new object[] { "craft", 70, "coins", 2800 }, new object[] { "sellGear", 48, "hammers", 36 }, new object[] { "equipGear", 24, "coins", 3600 }, new object[] { "coinSpend", 18000, "hammers", 90 }, new object[] { "upgradeStart", 21, "coins", 12600 }, new object[] { "gearUpgrade", 21, "hammers", 210 }, new object[] { "skillSummon", 45, "tickets", 108 }, new object[] { "techDone", 21, "potions", 84 }, new object[] { "petHatch", 23, "eggCurrency", 115 }, new object[] { "petMerge", 21, "eggCurrency", 525 }, new object[] { "mountSummon", 23, "winders", 460 }, new object[] { "mountMerge", 21, "winders", 210 }, new object[] { "dungeonClear", 21, "potions", 63 }, new object[] { "keySpend", 22, "potions", 44 } };

        static void AssertVector(List<Quest> list, object[][] expect, string label)
        {
            Assert.AreEqual(expect.Length, list.Count, label + " 개수");
            for (int i = 0; i < expect.Length; i++)
            {
                Assert.AreEqual((string)expect[i][0], list[i].Id, label + "[" + i + "] id");
                Assert.AreEqual(Convert.ToDouble(expect[i][1]), list[i].Need, label + "[" + i + "] need");
                Assert.AreEqual(0, list[i].Prog, label + "[" + i + "] prog");
                Assert.AreEqual((string)expect[i][2], list[i].Rw.Cur, label + "[" + i + "] cur");
                Assert.AreEqual(Convert.ToDouble(expect[i][3]), list[i].Rw.Amt, label + "[" + i + "] amt");
            }
        }

        [Test]
        public void ensure_는_원작_tier_벡터와_같다()
        {
            AssertVector(Q().List(new QuestState { Cleared = 0 }), T0, "tier0");
            AssertVector(Q().List(new QuestState { Cleared = 1 }), T1, "tier1");
            AssertVector(Q().List(new QuestState { Cleared = 5 }), T5, "tier5");
            AssertVector(Q().List(new QuestState { Cleared = 20 }), T20, "tier20");
            AssertVector(Q().List(new QuestState { Cleared = 25 }), T20, "tier25 = STEP_CAP 에서 멈춤");
        }

        [Test]
        public void bump_claim_같은퀘스트가_제자리에서_다시()
        {
            var q = Q();
            var s = new QuestState();
            var w = new KeyedWallet();
            Assert.IsFalse(q.Bump(s, "없는행동"));
            Assert.IsFalse(q.Bump(s, "craft", 0));
            Assert.IsTrue(q.Bump(s, "craft", 4));
            Assert.AreEqual(4, s.Quests[0].Prog);
            Assert.IsFalse(q.CanClaim(s, 0));
            Assert.AreEqual(0, q.ReadyCount(s));
            Assert.IsTrue(q.Bump(s, "craft", 100));
            Assert.AreEqual(10, s.Quests[0].Prog, "요구치에서 멈춘다");
            Assert.IsFalse(q.Bump(s, "craft"), "완료된 것은 더 안 오른다");
            Assert.AreEqual(1, q.ReadyCount(s));
            Assert.IsNull(q.Claim(s, w, 1), "미완료는 null");
            var got = q.Claim(s, w, 0);
            Assert.IsNotNull(got);
            Assert.AreEqual("coins", got.Cur); Assert.AreEqual(400, got.Amt); Assert.AreEqual("장비 제작", got.Text);
            Assert.AreEqual(400, w.Get("coins"));
            Assert.AreEqual(1, s.Cleared);
            Assert.AreEqual(14, s.Quests.Count);
            Assert.AreEqual("craft", s.Quests[0].Id, "재추첨하지 않는다 — 제자리에 같은 행동");
            Assert.AreEqual(0, s.Quests[0].Prog);
            Assert.AreEqual(13, s.Quests[0].Need, "tier 1 요구치");
            Assert.AreEqual(520, s.Quests[0].Rw.Amt);
            Assert.AreEqual(8, s.Quests[1].Need, "다른 칸(sellGear · tier 0 요구치 8)은 수령 전 요구치를 유지한다(ensure 가 저장분을 잇는다 · tier 1 이면 10)");
        }

        [Test]
        public void claimAll_은_큰_인덱스부터_전부()
        {
            var q = Q();
            var s = new QuestState();
            var w = new KeyedWallet();
            q.Bump(s, "craft", 10); q.Bump(s, "sellGear", 8); q.Bump(s, "keySpend", 2);
            var r = q.ClaimAll(s, w);
            Assert.AreEqual(3, r.N);
            Assert.AreEqual(400, r.Gains["coins"]); Assert.AreEqual(6, r.Gains["hammers"]); Assert.AreEqual(4, r.Gains["potions"]);
            Assert.AreEqual(400, w.Get("coins")); Assert.AreEqual(6, w.Get("hammers")); Assert.AreEqual(4, w.Get("potions"));
            Assert.AreEqual(3, s.Cleared);
            Assert.AreEqual(0, q.ReadyCount(s));
            Assert.AreEqual(0, q.ClaimAll(s, w).N);
        }

        [Test]
        public void 만렙이면_대장간_셋은_내려가되_깬것은_남는다()
        {
            var q = Q(() => false);
            var s = new QuestState();
            var w = new KeyedWallet();
            var list = q.List(s);
            Assert.AreEqual(11, list.Count);
            foreach (var x in list) Assert.IsFalse(Array.IndexOf(MetaDir.Meta.Quests.ForgeLocked, x.Id) >= 0, x.Id);
            // 깨 놓고 수령 전에 만렙이 된 경우: 남긴다
            var s2 = new QuestState();
            s2.Quests.Add(new Quest { Id = "coinSpend", Need = 2000, Prog = 2000, Rw = new QuestReward("hammers", 10) });
            var q2 = Q(() => false);
            var l2 = q2.List(s2);
            Assert.AreEqual(12, l2.Count);
            Assert.AreEqual("coinSpend", l2[3].Id, "DEFS 순서 자리에 그대로");
            var got = q2.Claim(s2, w, 3);
            Assert.AreEqual(10, got.Amt);
            Assert.AreEqual(11, s2.Quests.Count, "수령하면 자리가 내려간다");
        }

        [Test]
        public void 구세이브_젬보상은_대체재화로_다시_만든다()
        {
            var q = Q();
            var s = new QuestState { Cleared = 0 };
            s.Quests.Add(new Quest { Id = "gearUpgrade", Need = 1, Prog = 1, Rw = new QuestReward("gems", 168) });
            s.Quests.Add(new Quest { Id = "모르는것", Need = 1, Prog = 0, Rw = new QuestReward("coins", 1) });
            s.Quests.Add(new Quest { Id = "craft", Need = 0, Prog = 0, Rw = new QuestReward("coins", 1) });
            var list = q.List(s);
            Assert.AreEqual(14, list.Count);
            Assert.AreEqual("hammers", list[5].Rw.Cur, "젬 → NO_GEM_FALLBACK");
            Assert.AreEqual(10, list[5].Rw.Amt, "need 1 기준으로 다시 굴린 보상");
            Assert.AreEqual(1, list[5].Prog, "진행도는 잇는다");
            Assert.AreEqual(10, list[0].Need, "need ≤ 0 인 저장분은 버리고 새로");
            var w = new KeyedWallet();
            var got = q.Claim(s, w, 5);
            Assert.AreEqual("hammers", got.Cur);
            Assert.AreEqual(0, w.Get("gems"));
        }

        [Test]
        public void 손댄_세이브의_젬보상은_수령_길목의_ensure_가_다시_굴린다()
        {
            // 원작 claim(i) → canClaim(i) → list() → ensure() 라 젬이 박힌 칸은 수령 전에 정의 재화(need 기준 보상)로 되돌아간다 — 젬은 어느 길로도 안 나간다.
            var q = Q();
            var s = new QuestState();
            q.Ensure(s);
            s.Quests[0].Rw = new QuestReward("gems", 7);
            s.Quests[0].Prog = s.Quests[0].Need;
            var w = new KeyedWallet();
            var got = q.Claim(s, w, 0);
            Assert.AreEqual("coins", got.Cur);
            Assert.AreEqual(400, got.Amt);
            Assert.AreEqual(400, w.Get("coins"));
            Assert.AreEqual(0, w.Get("gems"));
            Assert.AreEqual("hammers", q.CurOf("gems"), "curOf 가드 자체는 젬 → 대체 재화");
        }
    }

    /// <summary>T25 — 리그: 봇 20 강한 순 · 랭킹 안정 정렬 · 승률 식(원작 벡터) · 티켓·점수 · 시즌 종료 정산 · 순위 보상 표.</summary>
    public class LeagueTests
    {
        League L(uint seed = 7) { return new League(MetaDir.Meta.League, MetaDir.Meta.Avatars, Rng.Mulberry(seed)); }
        static readonly Big Cp = Big.Of(1000);

        [Test]
        public void 시즌시작_봇20_강한순_이름중복없음()
        {
            var lg = L();
            var s = new LeagueState();
            var r = lg.Ensure(s, new KeyedWallet(), Cp, 0, "Sat Sep 12 2026");
            Assert.IsTrue(r.SeasonStarted); Assert.IsFalse(r.TicketsReset); Assert.IsNull(r.SeasonReward);
            Assert.AreEqual(20, s.Bots.Count);
            Assert.AreEqual(50, s.Score); Assert.AreEqual(5, s.Tickets);
            Assert.AreEqual(3 * 24 * 3600 * 1000.0, s.SeasonEndsAt);
            var names = new HashSet<string>();
            for (int i = 0; i < s.Bots.Count; i++)
            {
                var b = s.Bots[i];
                Assert.IsTrue(names.Add(b.Name), "이름 중복 " + b.Name);
                Assert.IsTrue(Array.IndexOf(MetaDir.Meta.League.NamePool, b.Name) >= 0, b.Name);
                Assert.IsTrue(Array.IndexOf(MetaDir.Meta.Avatars.Pool, b.Avatar) >= 0, b.Avatar);
                Assert.IsTrue(b.Cp.Gte(Big.Of(400)) && b.Cp.Lte(Big.Of(2500)), "cp 배율 0.4~2.5: " + b.Cp);
                Assert.IsTrue(b.Score >= 20 && b.Score <= 200 && b.Score == Math.Floor(b.Score), "점수 " + b.Score);
                Assert.IsTrue(b.Server >= 1 && b.Server <= 30, "서버 " + b.Server);
                if (i > 0) Assert.IsTrue(s.Bots[i - 1].Cp.Gte(b.Cp), "강한 순");
            }
            var again = new LeagueState();
            L().Ensure(again, new KeyedWallet(), Cp, 0, "Sat Sep 12 2026");
            Assert.AreEqual(s.Bots[0].Name, again.Bots[0].Name, "같은 시드 → 같은 봇");
            Assert.AreEqual(s.Bots[19].Cp, again.Bots[19].Cp);
        }

        [Test]
        public void 전투력_최소1_과_같은_시즌_재시작은_점수유지()
        {
            var lg = L();
            var s = new LeagueState();
            lg.StartSeason(s, false, Big.Zero, 0, "d");
            foreach (var b in s.Bots) Assert.IsTrue(b.Cp.Eq(Big.One), "cp 최소 1");
            s.Score = 77;
            lg.StartSeason(s, false, Cp, 0, "d");
            Assert.AreEqual(77, s.Score, "resetScore=false 면 점수 유지");
            lg.StartSeason(s, true, Cp, 0, "d");
            Assert.AreEqual(50, s.Score, "resetScore=true 면 START_SCORE");
        }

        [Test]
        public void 랭킹은_점수_내림차순_안정_나는_같은점수면_뒤()
        {
            var lg = L();
            var s = new LeagueState();
            lg.StartSeason(s, false, Cp, 0, "d");
            for (int i = 0; i < s.Bots.Count; i++) s.Bots[i].Score = 100 - i;
            s.Bots[3].Score = 50;
            s.Score = 50;
            var board = lg.Board(s, Cp, "용사", "🛡️");
            Assert.AreEqual(21, board.Count);
            Assert.AreEqual(100, board[0].Score);
            for (int i = 1; i < board.Count; i++) Assert.IsTrue(board[i - 1].Score >= board[i].Score);
            int meAt = -1, bot3At = -1;
            for (int i = 0; i < board.Count; i++) { if (board[i].IsMe) meAt = i; if (board[i].Name == s.Bots[3].Name) bot3At = i; }
            Assert.AreEqual(bot3At + 1, meAt, "같은 점수면 봇이 앞(안정 정렬)");
            Assert.AreEqual("나", board[meAt].Server);
            Assert.AreEqual(meAt + 1, lg.MyRank(s, Cp));
            s.Score = 1000;
            Assert.AreEqual(1, lg.MyRank(s, Cp));
        }

        [Test]
        public void 도전상대는_앞5_별5에서1()
        {
            var lg = L();
            var s = new LeagueState();
            lg.StartSeason(s, false, Cp, 0, "d");
            var c = lg.ChallengeList(s);
            Assert.AreEqual(5, c.Count);
            for (int i = 0; i < 5; i++) { Assert.AreEqual(5 - i, c[i].StarReward); Assert.AreSame(s.Bots[i], c[i].Bot); Assert.AreEqual(i, c[i].Index); }
        }

        // 원작 node 벡터: U.clamp(0.15 + A.ratioTo(A.add(B)) * 0.7, 0.05, 0.95)
        [TestCase(100, 100, 0.5)]
        [TestCase(100, 400, 0.29)]
        [TestCase(1e6, 1, 0.8499993000006999)]
        [TestCase(1, 1e6, 0.1500006999993)]
        [TestCase(4.1e12, 50.8e9, 0.84143297677559)]
        public void 승률식은_원작과_같다(double my, double bot, double expect)
        {
            Assert.AreEqual(expect, L().WinChance(Big.Of(my), Big.Of(bot)), 1e-12);
        }

        [Test]
        public void 도전은_티켓을_쓰고_이기면_별()
        {
            var lg = L(3);
            var s = new LeagueState();
            lg.StartSeason(s, false, Cp, 0, "d");
            Assert.IsNull(lg.Challenge(s, 99, Cp));
            int wins = 0, stars = 0;
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(lg.CanChallenge(s, 0));
                var r = lg.Challenge(s, 0, Cp);
                Assert.IsNotNull(r);
                Assert.AreSame(s.Bots[0], r.Bot);
                Assert.AreEqual(r.Win ? 5 : 0, r.StarReward);
                if (r.Win) { wins++; stars += 5; }
            }
            Assert.AreEqual(0, s.Tickets);
            Assert.AreEqual(50 + stars, s.Score);
            Assert.IsFalse(lg.CanChallenge(s, 0));
            Assert.IsNull(lg.Challenge(s, 0, Cp), "티켓 0 이면 null");
            // 압도적이면 늘 이기고(95%) 미약하면 늘 진다(5%)는 것은 확률이라 단언하지 않는다 — 승률 식은 위 벡터로 본다.
            Assert.IsTrue(wins >= 0 && wins <= 5);
        }

        [Test]
        public void 티켓은_날짜키가_바뀌면_리셋()
        {
            var lg = L();
            var s = new LeagueState();
            lg.StartSeason(s, false, Cp, 0, "Sat Sep 12 2026");
            s.Tickets = 1;
            Assert.IsFalse(lg.CheckTicketReset(s, "Sat Sep 12 2026"));
            Assert.AreEqual(1, s.Tickets);
            var r = lg.Ensure(s, new KeyedWallet(), Cp, 1, "Sun Sep 13 2026");
            Assert.IsFalse(r.SeasonStarted); Assert.IsTrue(r.TicketsReset);
            Assert.AreEqual(5, s.Tickets);
        }

        [Test]
        public void 시즌이_끝나면_순위보상_지급_후_점수리셋_새시즌()
        {
            var lg = L();
            var s = new LeagueState();
            var w = new KeyedWallet();
            lg.StartSeason(s, false, Cp, 0, "d");
            s.Score = 100000;
            var oldBots = s.Bots;
            var r = lg.Ensure(s, w, Cp, s.SeasonEndsAt, "d");
            Assert.IsNotNull(r.SeasonReward);
            Assert.AreEqual(1, r.EndedRank);
            Assert.AreEqual(900, w.Get("hammers")); Assert.AreEqual(18000, w.Get("coins")); Assert.AreEqual(120, w.Get("tickets"));
            Assert.AreEqual(360, w.Get("eggCurrency")); Assert.AreEqual(450, w.Get("potions")); Assert.AreEqual(180, w.Get("winders"));
            Assert.AreEqual(50, s.Score, "새 시즌은 START_SCORE");
            Assert.AreNotSame(oldBots, s.Bots);
            Assert.AreEqual(s.SeasonEndsAt, 3 * 24 * 3600 * 1000.0 + 3 * 24 * 3600 * 1000.0);
            Assert.IsNull(lg.Ensure(s, w, Cp, s.SeasonEndsAt - 1, "d").SeasonReward, "아직이면 없음");
        }

        [TestCase(1, 3)] [TestCase(2, 2.4)] [TestCase(3, 2)] [TestCase(4, 1.6)] [TestCase(5, 1.6)] [TestCase(6, 1.2)] [TestCase(10, 1.2)]
        [TestCase(11, 1)] [TestCase(20, 1)] [TestCase(21, 0.6)] [TestCase(50, 0.6)] [TestCase(0, 3)]
        public void 순위보상_배수는_표에서(int rank, double mult)
        {
            var lg = L();
            Assert.AreEqual(mult, lg.RewardMult(rank));
            Assert.AreEqual(JsNum.Round(300 * mult), lg.RewardForRank(rank)["hammers"]);
            Assert.AreEqual(JsNum.Round(6000 * mult), lg.RewardForRank(rank)["coins"]);
        }
    }

    /// <summary>T25 — 채팅: 인물 해시(원작 벡터) · 시드 18 + 공유 카드 · 60개 상한 · 틱 간격 · 내 메시지.</summary>
    public class ChatTests
    {
        Chat C(uint seed = 5) { return new Chat(MetaDir.Meta.Chat, MetaDir.Meta.Avatars, Rng.Mulberry(seed)); }

        // 원작 node 벡터: Chat.persona(name) → [tag, avatar, gender]
        [TestCase("Pirimid", "GG", "🧑‍🌾", "♀")]
        [TestCase("Imax", "", "🧑‍🚒", "♀")]
        [TestCase("Bearopotamus", "LGBT", "🥷", "♀")]
        [TestCase("MilkMessiah", "ZX", "🧑‍🚀", "♀")]
        [TestCase("loui", "KO", "🧑‍🎨", "♂")]
        [TestCase("Zephyr", "FRY", "👽", "♀")]
        public void 인물해시는_원작과_같다(string name, string tag, string avatar, string gender)
        {
            var p = C().Persona(name);
            Assert.AreEqual(name, p.Name); Assert.AreEqual(tag, p.Tag); Assert.AreEqual(avatar, p.Avatar); Assert.AreEqual(gender, p.Gender);
        }

        [Test]
        public void 시드는_18개_장문둘_공유카드하나_과거로()
        {
            var c = C();
            var s = new ChatState();
            double now = 10000000;
            c.Ensure(s, now);
            Assert.AreEqual(18, s.Messages.Count);
            Assert.AreEqual(now, s.LastBotAt);
            int longs = 0, shares = 0;
            for (int i = 0; i < s.Messages.Count; i++)
            {
                var m = s.Messages[i];
                Assert.IsFalse(m.Mine);
                Assert.AreEqual(now - 18 * 90000 + i * 90000, m.At);
                if (m.Type == ChatMessage.TypeShare) { shares++; Assert.AreEqual(16, i); Assert.AreEqual("MilkMessiah", m.MyName); Assert.AreEqual("Bearopotamus", m.OppName); Assert.IsTrue(m.Win); Assert.AreEqual("ZX", m.Tag); Assert.IsTrue(m.MyCp.Eq(Big.Of(4.1e12))); Assert.IsTrue(m.OppCp.Eq(Big.Of(50.8e9))); }
                else
                {
                    Assert.AreEqual(ChatMessage.TypeMsg, m.Type);
                    bool isLong = Array.IndexOf(MetaDir.Meta.Chat.LongLines, m.Text) >= 0;
                    if (isLong) { longs++; Assert.IsTrue(i == 2 || i == 11, "장문 자리 " + i); }
                    else Assert.IsTrue(Array.IndexOf(MetaDir.Meta.Chat.Lines, m.Text) >= 0, m.Text);
                    Assert.AreEqual(c.Persona(m.Name).Tag, m.Tag, "같은 닉네임 = 같은 인물");
                }
            }
            Assert.AreEqual(2, longs); Assert.AreEqual(1, shares);
            c.Ensure(s, now + 1);
            Assert.AreEqual(18, s.Messages.Count, "비어 있지 않으면 다시 심지 않는다");
        }

        [Test]
        public void 상한60_틱간격_내메시지_공유카드()
        {
            var c = C();
            var s = new ChatState();
            double now = 0;
            c.Ensure(s, now);
            Assert.IsFalse(c.Tick(s, now + 20000), "첫 간격 20초 — 같으면 아직");
            Assert.IsTrue(c.Tick(s, now + 20001));
            Assert.AreEqual(19, s.Messages.Count);
            Assert.AreEqual(now + 20001, s.LastBotAt);
            Assert.IsFalse(c.Tick(s, now + 20001 + 15000), "다음 간격은 15~40초");
            Assert.IsTrue(c.Tick(s, now + 20001 + 40001));
            for (int i = 0; i < 100; i++) c.PushBotMessage(s, now, null);
            Assert.AreEqual(60, s.Messages.Count, "MAX_MESSAGES");
            Assert.IsFalse(c.SendPlayer(s, "   ", "용사", "🛡️", "♂", now));
            Assert.IsTrue(c.SendPlayer(s, "  " + new string('a', 300) + " ", "용사", "🛡️", "♂", now));
            var last = c.LastMessage(s, now);
            Assert.IsTrue(last.Mine); Assert.AreEqual(200, last.Text.Length); Assert.AreEqual("", last.Tag); Assert.AreEqual("용사", last.Name);
            var opp = new LeagueBot { Name = "Kite", Avatar = "🐺", Cp = Big.Of(5) };
            c.ShareLeagueResult(s, false, Big.Of(3), opp, "용사", "🛡️", now);
            last = c.LastMessage(s, now);
            Assert.AreEqual(ChatMessage.TypeShare, last.Type); Assert.IsTrue(last.Mine); Assert.IsFalse(last.Win); Assert.AreEqual("Kite", last.OppName); Assert.IsTrue(last.OppCp.Eq(Big.Of(5)));
            Assert.AreEqual(60, s.Messages.Count);
        }
    }
}
