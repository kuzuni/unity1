using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.Skills;

namespace Forge.Tests
{
    /// <summary>
    /// T17 — Core `SkillSystem` ↔ 원작 `skills.js` 대조. 기대값은 전부 `Vectors/skill_vectors.json`(`tools/skill_vectors.js` 가 정본을
    /// mulberry32 시드로 **실제로 돌려** 뽑은 것)에서 온다 — 여기에 수치를 손으로 적지 않는다. 같은 시드 → 같은 등급·같은 종·같은 순서.
    /// </summary>
    static class SkillVectors
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
                        string cand = Path.Combine(d, "Assets", "Tests", "EditMode", "Vectors", "skill_vectors.json");
                        if (File.Exists(cand)) { _v = MiniJson.ParseObject(File.ReadAllText(cand)); return _v; }
                        d = Path.GetDirectoryName(d);
                    }
                }
                throw new FileNotFoundException("Assets/Tests/EditMode/Vectors/skill_vectors.json 을 못 찾았다 — node tools/skill_vectors.js");
            }
        }
        public static JsonObject Consts { get { return J.Obj(V["consts"]); } }
        public static List<object> Arr(string key) { return J.Arr(J.Require(V, key)); }
        public static JsonObject Obj(string key) { return J.Obj(J.Require(V, key)); }
    }

    /// <summary>원작이 밖에서 받는 것을 벡터 `consts`(STAR_MULT)와 케이스별 기술트리 % 로 채운 시험용 호스트 — 정본 `techtree.js`·`ascension.js` 공식 그대로.</summary>
    sealed class TestSkillHost : ISkillHost
    {
        public double Gems { get; set; }
        public double Tickets { get; set; }
        public double SummonCostPct, PassiveDmgPct, PassiveHpPct;
        public int AscendSkill;
        readonly double _starMult;
        public TestSkillHost() { _starMult = J.Num(SkillVectors.Consts["STAR_MULT"]); }
        public double SkillSummonCostMult() { return Math.Max(0.1, 1 - SummonCostPct / 100); }
        public double SkillPassiveDmgMult() { return 1 + PassiveDmgPct / 100; }
        public double SkillPassiveHpMult() { return 1 + PassiveHpPct / 100; }
        public Big StarMult(int stars) { return stars <= 0 ? Big.One : Big.Of(_starMult).Pow(stars); }
        public int SkillAscendCount() { return AscendSkill; }
    }

    public class SkillTests
    {
        GameData G { get { return DataDir.Game; } }
        TestSkillHost _host;
        SkillSystem _sys;

        SkillSystem Make(uint seed, SkillState state = null)
        {
            _host = new TestSkillHost();
            _sys = new SkillSystem(G, SkillRules.Original(), _host, Rng.Mulberry(seed), state);
            return _sys;
        }

        /// <summary>벡터의 입력 꼴 `[[id, level, dupes, stars], …]` → 상태(삽입 순서 유지).</summary>
        static SkillState StateOf(object skillsArr, object equippedArr = null, int summonCount = 0)
        {
            var st = new SkillState { SummonCount = summonCount };
            if (skillsArr != null)
                foreach (object row in J.Arr(skillsArr))
                {
                    var r = J.Arr(row);
                    st.Skills.Add(J.Str(r[0]), new SkillEntry { Level = J.Int(r[1]), Dupes = J.Int(r[2]), Stars = r.Count > 3 ? J.Int(r[3]) : 0 });
                }
            if (equippedArr != null) st.Equipped = new List<string>(J.StrArr(equippedArr));
            return st;
        }

        static void Near(double expected, double actual, string what)
        {
            Assert.AreEqual(expected, actual, 1e-9 * Math.Max(1, Math.Abs(expected)), what);
        }
        static void BigEq(JsonObject exp, Big actual, string what)
        {
            Assert.AreEqual(J.Num(exp["e"]), actual.E, what + " .e");
            Near(J.Num(exp["m"]), actual.M, what + " .m");
        }

        /// <summary>벡터의 `skillsOf(S)` 꼴 `[[id, {level, dupes, stars}], …]` 과 상태 대조(순서까지).</summary>
        static void AssertSkills(object exp, SkillState st, string what)
        {
            var rows = J.Arr(exp);
            Assert.AreEqual(rows.Count, st.Skills.Count, what + " 보유 수");
            for (int i = 0; i < rows.Count; i++)
            {
                var r = J.Arr(rows[i]); var o = J.Obj(r[1]);
                Assert.AreEqual(J.Str(r[0]), st.Skills.KeyAt(i), what + " 순서 " + i);
                SkillEntry e = st.Skills.ValueAt(i);
                Assert.AreEqual(J.Int(o["level"]), e.Level, what + " " + r[0] + " level");
                Assert.AreEqual(J.Int(o["dupes"]), e.Dupes, what + " " + r[0] + " dupes");
                Assert.AreEqual(J.Int(o["stars"]), e.Stars, what + " " + r[0] + " stars");
            }
        }

        void AssertSnap(JsonObject after, string what)
        {
            AssertSkills(after["skills"], _sys.State, what);
            Assert.AreEqual(J.StrArr(after["equipped"]), _sys.State.Equipped.ToArray(), what + " equipped");
            Assert.AreEqual(J.Int(after["summonCount"]), _sys.State.SummonCount, what + " summonCount");
            Near(J.Num(after["gems"]), _host.Gems, what + " gems");
            Near(J.Num(after["tickets"]), _host.Tickets, what + " tickets");
        }

        [Test]
        public void 상수_원작_코드_상수와_같다()
        {
            var c = SkillVectors.Consts; var r = SkillRules.Original();
            Assert.AreEqual(J.Int(c["SUMMON_TICKET_COST"]), r.SummonTicketCost, "SUMMON_TICKET_COST");
            Assert.AreEqual(J.Int(c["SUMMON_GEM_COST"]), r.SummonGemCost, "SUMMON_GEM_COST");
            Assert.AreEqual(J.Int(c["MAX_LEVEL"]), r.MaxLevel, "MAX_LEVEL");
            Assert.AreEqual(J.Int(c["MAX_ACTIVE"]), r.MaxActive, "MAX_ACTIVE");
            var ids = new List<string>(); foreach (var d in G.Defs.SkillDefs) ids.Add(d.Id);
            Assert.AreEqual(J.StrArr(c["skillIds"]), ids.ToArray(), "SKILL_DEFS 순서");
            Assert.AreEqual(18, ids.Count, "18종");
        }

        [Test]
        public void 티켓_비용_기술트리_배율()
        {
            Make(1);
            foreach (object o in SkillVectors.Arr("ticketCost"))
            {
                var c = J.Obj(o); _host.SummonCostPct = J.Num(c["pct"]);
                Near(J.Num(c["cost"]), _sys.TicketCost(J.Num(c["count"])), "ticketCost(" + c["count"] + ") pct " + c["pct"]);
            }
            _host.SummonCostPct = 0;
            Near(J.Num(SkillVectors.V["ticketCostDefault"]), _sys.TicketCost(), "ticketCost()");
        }

        [Test]
        public void 소환_레벨_누적_소환수()
        {
            foreach (object o in SkillVectors.Arr("summonLevel"))
            {
                var c = J.Obj(o); Make(1, new SkillState { SummonCount = J.Int(c["summonCount"]) });
                Assert.AreEqual(J.Int(c["level"]), _sys.SummonLevel(), "summonLevel @ " + c["summonCount"]);
            }
        }

        [Test]
        public void 확률표_레벨별_등급_순서()
        {
            Make(1);
            string[] rar = G.Defs.Rarities;
            foreach (object o in SkillVectors.Arr("rates"))
            {
                var c = J.Obj(o); var exp = J.NumArr(c["rates"]); var got = _sys.Rates(J.Int(c["level"]));
                Assert.AreEqual(rar, new List<string>(got.Keys).ToArray(), "키 순서 @ " + c["level"]);
                for (int i = 0; i < rar.Length; i++) Near(exp[i], got[rar[i]], "rates(" + c["level"] + ")." + rar[i]);
            }
            var d = SkillVectors.Obj("ratesDefault");
            Make(1, new SkillState { SummonCount = J.Int(d["summonCount"]) });
            var exp2 = J.NumArr(d["rates"]); var got2 = _sys.Rates();
            for (int i = 0; i < rar.Length; i++) Near(exp2[i], got2[rar[i]], "rates()." + rar[i]);
        }

        [Test]
        public void 소환_가능_여부()
        {
            Make(1);
            foreach (object o in SkillVectors.Arr("canSummon"))
            {
                var c = J.Obj(o); _host.Gems = J.Num(c["gems"]); _host.Tickets = J.Num(c["tickets"]); _host.SummonCostPct = J.Num(c["pct"]);
                Assert.AreEqual(J.Bool(c["can"]), _sys.CanSummon(J.Bool(c["useGems"]), J.Num(c["count"])), "canSummon " + MiniJson.Serialize(c));
            }
        }

        [Test]
        public void 소환_배치_같은_시드_같은_결과()
        {
            foreach (object o in SkillVectors.Arr("summon"))
            {
                var c = J.Obj(o); string name = J.Str(c["name"]);
                Make(J.UInt(c["seed"]), StateOf(c["skills"], c["equipped"], J.Int(c["summonCount"])));
                _host.Gems = J.Num(c["gems"]); _host.Tickets = J.Num(c["tickets"]); _host.SummonCostPct = J.Num(c["pct"]); _host.AscendSkill = J.Int(c["ascendSkill"]);
                bool useGems = J.Bool(c["useGems"]); int count = J.Int(c["count"]);
                var pre = J.Obj(c["pre"]);
                Assert.AreEqual(J.Int(pre["level"]), _sys.SummonLevel(), name + " pre.level");
                Assert.AreEqual(J.Bool(pre["can"]), _sys.CanSummon(useGems, count), name + " pre.can");
                Near(J.Num(pre["cost"]), useGems ? _sys.Rules.SummonGemCost * (double)count : _sys.TicketCost(count), name + " pre.cost");

                var r = _sys.Summon(useGems, count);
                if (c["result"] == null)
                {
                    Assert.IsNull(r, name + " 은 비용 부족 → null");
                    Assert.AreEqual(0, _sys.RecalcRequests, name + " 재계산 0");
                }
                else
                {
                    Assert.IsNotNull(r, name + " 결과");
                    var res = J.Obj(c["result"]); var items = J.Arr(res["results"]);
                    Assert.AreEqual(J.Int(res["count"]), r.Count, name + " count");
                    Assert.AreEqual(items.Count, r.Results.Count, name + " results 수");
                    for (int i = 0; i < items.Count; i++)
                    {
                        var it = J.Obj(items[i]);
                        Assert.AreEqual(J.Str(it["id"]), r.Results[i].Def.Id, name + " [" + i + "] id");
                        Assert.AreEqual(J.Bool(it["isNew"]), r.Results[i].IsNew, name + " [" + i + "] isNew");
                        Assert.AreEqual(J.Int(it["level"]), r.Results[i].Level, name + " [" + i + "] level");
                    }
                    var gacha = J.StrArr(c["gacha"]);
                    Assert.AreEqual(1, gacha.Length, name + " 효과음 한 번");
                    Assert.AreEqual(gacha[0], r.BestRarity, name + " bestRarity");
                    Assert.AreEqual(J.Int(c["recalc"]), r.NewCount, name + " 신규 수 = recalcHero 횟수");
                    Assert.AreEqual(J.Int(c["recalc"]), _sys.RecalcRequests, name + " RecalcRequests");
                }
                AssertSnap(J.Obj(c["after"]), name + " after");
            }
        }

        [Test]
        public void 피해_회복_버프_레벨배율_승천()
        {
            foreach (object o in SkillVectors.Arr("values"))
            {
                var c = J.Obj(o); string id = J.Str(c["id"]);
                var st = new SkillState(); st.Skills.Add(id, new SkillEntry { Level = J.Int(c["level"]), Dupes = 0, Stars = J.Int(c["stars"]) });
                Make(1, st);
                string what = id + " lv" + c["level"] + " ★" + c["stars"];
                Near(J.Num(c["levelMult"]), _sys.LevelMult(id), what + " levelMult");
                BigEq(J.Obj(c["dmg"]), _sys.Dmg(id), what + " dmg");
                BigEq(J.Obj(c["heal"]), _sys.HealAmt(id), what + " heal");
                BigEq(J.Obj(c["buff"]), _sys.BuffAtk(id), what + " buff");
            }
            var u = SkillVectors.Obj("valuesUnowned"); string uid = J.Str(u["id"]);
            Make(1);
            Near(J.Num(u["levelMult"]), _sys.LevelMult(uid), "미보유 levelMult(0.85)");
            BigEq(J.Obj(u["dmg"]), _sys.Dmg(uid), "미보유 dmg");
            BigEq(J.Obj(u["heal"]), _sys.HealAmt(uid), "미보유 heal");
            BigEq(J.Obj(u["buff"]), _sys.BuffAtk(uid), "미보유 buff");
            Assert.AreEqual(J.Int(u["level"]), _sys.Level(uid), "미보유 level 0");
            Assert.IsTrue(J.Bool(u["defNull"]) && _sys.Def("nope") == null, "모르는 id → null");
        }

        [Test]
        public void 전투용_Spec_은_같은_값을_담는다()
        {
            var st = new SkillState(); st.Skills.Add("meteor", new SkillEntry { Level = 7, Stars = 1 });
            Make(1, st);
            var s = _sys.Spec("meteor");
            Assert.IsNotNull(s);
            Assert.AreEqual("meteor", s.Id); Assert.AreEqual("aoe", s.Type);
            Assert.AreEqual(G.Defs.Skill("meteor").Cd, s.Cd);
            Assert.IsTrue(s.Dmg.Equals(_sys.Dmg("meteor")) && s.HealAmt.Equals(_sys.HealAmt("meteor")) && s.BuffAtk.Equals(_sys.BuffAtk("meteor")));
            Assert.IsNull(_sys.Spec("nope"));
        }

        [Test]
        public void 조각_요구량_표()
        {
            Make(1);
            foreach (object o in SkillVectors.Arr("shards"))
            {
                var c = J.Obj(o);
                Assert.AreEqual(J.Int(c["n"]), _sys.ShardsRequired(J.Int(c["level"])), "shardsRequired(" + c["level"] + ")");
            }
        }

        [Test]
        public void 업그레이드_시퀀스_만렙_조각부족()
        {
            foreach (object o in SkillVectors.Arr("upgrade"))
            {
                var c = J.Obj(o); string name = J.Str(c["name"]); string id = J.Str(c["id"]);
                var start = new List<object> { c["start"] };
                Make(1, StateOf(start));
                var steps = J.Arr(c["steps"]);
                for (int i = 0; i < steps.Count; i++)
                {
                    var s = J.Obj(steps[i]);
                    Assert.AreEqual(J.Bool(s["can"]), _sys.CanUpgrade(id), name + " [" + i + "] can");
                    Assert.AreEqual(J.Bool(s["ok"]), _sys.Upgrade(id), name + " [" + i + "] ok");
                    Assert.AreEqual(J.Int(s["level"]), _sys.State.Get(id).Level, name + " [" + i + "] level");
                    Assert.AreEqual(J.Int(s["dupes"]), _sys.State.Get(id).Dupes, name + " [" + i + "] dupes");
                }
                Assert.AreEqual(J.Int(c["recalc"]), _sys.RecalcRequests, name + " 재계산 횟수");
            }
            var u = SkillVectors.Obj("upgradeUnknown"); Make(1);
            Assert.AreEqual(J.Bool(u["can"]), _sys.CanUpgrade("nope")); Assert.AreEqual(J.Bool(u["ok"]), _sys.Upgrade("nope"));
        }

        [Test]
        public void 전부_업그레이드_보유_순서대로()
        {
            var c = SkillVectors.Obj("upgradeAll");
            Make(1, StateOf(c["before"]));
            Assert.AreEqual(J.Int(c["count"]), _sys.UpgradeAll(), "올린 횟수");
            AssertSkills(c["after"], _sys.State, "upgradeAll after");
            Assert.AreEqual(J.Int(c["recalc"]), _sys.RecalcRequests, "재계산 횟수");
        }

        [Test]
        public void 빠른_장착_등급_레벨_안정정렬()
        {
            var c = SkillVectors.Obj("quickEquip");
            Make(1, StateOf(c["skills"], c["before"]));
            _sys.QuickEquip();
            Assert.AreEqual(J.StrArr(c["after"]), _sys.State.Equipped.ToArray(), "quickEquip");
            Assert.AreEqual(J.Int(c["recalc"]), _sys.RecalcRequests, "재계산 1");
            var one = SkillVectors.Obj("quickEquipOne");
            var st = new SkillState(); st.Skills.Add("fireball", new SkillEntry());
            Make(1, st); _sys.QuickEquip();
            Assert.AreEqual(J.StrArr(one["after"]), _sys.State.Equipped.ToArray(), "보유 하나");
        }

        [Test]
        public void 장착_토글_슬롯_3()
        {
            var c = SkillVectors.Obj("toggle");
            Make(1, StateOf(c["skills"]));
            var steps = J.Arr(c["steps"]);
            for (int i = 0; i < steps.Count; i++)
            {
                var s = J.Obj(steps[i]); string id = J.Str(s["id"]);
                Assert.AreEqual(J.Bool(s["ok"]), _sys.ToggleEquip(id), "toggle[" + i + "] " + id);
                Assert.AreEqual(J.StrArr(s["equipped"]), _sys.State.Equipped.ToArray(), "toggle[" + i + "] equipped");
            }
            Assert.AreEqual(J.Int(c["recalc"]), _sys.RecalcRequests, "재계산 횟수 = 성공한 토글 수");
        }

        [Test]
        public void 보유_패시브_장착_무관()
        {
            foreach (object o in SkillVectors.Arr("passive"))
            {
                var c = J.Obj(o); string id = J.Str(c["id"]);
                var st = new SkillState(); st.Skills.Add(id, new SkillEntry { Level = J.Int(c["level"]), Stars = J.Int(c["stars"]) });
                Make(1, st); _host.PassiveDmgPct = J.Num(c["dmgPct"]); _host.PassiveHpPct = J.Num(c["hpPct"]);
                var p = _sys.PassiveOf(id);
                string what = id + " lv" + c["level"] + " ★" + c["stars"];
                BigEq(J.Obj(c["atk"]), p.Atk, what + " atk"); BigEq(J.Obj(c["hp"]), p.Hp, what + " hp");
            }
            Make(1);
            var z = SkillVectors.Obj("passiveUnknown"); var pz = _sys.PassiveOf("nope");
            BigEq(J.Obj(z["atk"]), pz.Atk, "모르는 id atk"); BigEq(J.Obj(z["hp"]), pz.Hp, "모르는 id hp");

            var s = SkillVectors.Obj("ownedPassive");
            Make(1, StateOf(s["skills"])); _host.PassiveDmgPct = J.Num(s["dmgPct"]); _host.PassiveHpPct = J.Num(s["hpPct"]);
            var sum = _sys.OwnedPassive();
            BigEq(J.Obj(s["atk"]), sum.Atk, "ownedPassive atk"); BigEq(J.Obj(s["hp"]), sum.Hp, "ownedPassive hp");
            Make(1); var e = SkillVectors.Obj("ownedPassiveEmpty"); var se = _sys.OwnedPassive();
            BigEq(J.Obj(e["atk"]), se.Atk, "빈 atk"); BigEq(J.Obj(e["hp"]), se.Hp, "빈 hp");
        }
    }
}
