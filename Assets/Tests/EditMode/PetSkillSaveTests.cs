using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.PetSave;
using Forge.Core.Pets;
using Forge.Core.Skills;

namespace Forge.Tests
{
    /// <summary>T20 — 세이브 트리(원작 `S`) ↔ PetState/SkillState 코덱 왕복. 정본 새 게임 저장 원문(SaveVectors.DefaultSave · T13 벡터)과 손으로 꾸민 상태를 «같은 글자» 로 되돌리는가.</summary>
    public class PetSkillSaveTests
    {
        static JsonObject Default() { return MiniJson.ParseObject(SaveVectors.DefaultSave); }

        [Test]
        public void 새_게임_펫_칸을_읽는다()
        {
            PetState st = PetSkillSave.ReadPets(Default());
            Assert.AreEqual(1, st.Eggs.Count, "시작 알 1개(common)");
            Assert.AreEqual("common", st.Eggs[0].Rarity);
            Assert.AreEqual(0, st.Hatching.Count);
            Assert.AreEqual(0, st.Pets.Count);
            Assert.AreEqual(0, st.ActivePets.Count);
            Assert.AreEqual(0, st.PetSummonCount);
            Assert.AreEqual(0, st.HatchSlotBonus);
        }

        [Test]
        public void 새_게임_스킬_칸을_읽는다()
        {
            SkillState st = PetSkillSave.ReadSkills(Default());
            Assert.AreEqual(1, st.Skills.Count);
            Assert.AreEqual("powerStrike", st.Skills.KeyAt(0));
            Assert.AreEqual(1, st.Skills["powerStrike"].Level);
            Assert.AreEqual(0, st.Skills["powerStrike"].Dupes);
            Assert.AreEqual(0, st.Skills["powerStrike"].Stars);
            CollectionAssert.AreEqual(new[] { "powerStrike" }, st.Equipped);
            Assert.AreEqual(0, st.SummonCount);
        }

        [Test]
        public void 새_게임을_읽고_되쓰면_글자가_같다()
        {
            JsonObject s = Default();
            string before = MiniJson.Serialize(s);
            PetSkillSave.WritePets(s, PetSkillSave.ReadPets(s));
            PetSkillSave.WriteSkills(s, PetSkillSave.ReadSkills(s));
            Assert.AreEqual(before, MiniJson.Serialize(s), "키 순서·수 표현이 원작 저장 원문과 같아야 한다");
        }

        [Test]
        public void 꾸민_상태를_되쓰면_원작_키_꼴이다()
        {
            JsonObject s = Default();
            var pets = new PetState();
            pets.Eggs.Add(new Egg("rare"));
            pets.Eggs.Add(new Egg("mythic"));
            pets.Hatching.Add(new HatchSlot("epic", 1700000123456));
            var p = new Pet { Name = "Treant", Rarity = "ultimate", Level = 6, Dupes = 0, Xp = 87988, Stars = 1 };
            p.Subs.Add(new Substat("critChance", "치명타 확률", 10));
            p.Subs.Add(new Substat("doubleChance", "더블 찬스", 16.9));
            pets.Pets.Add(p);
            pets.ActivePets.Add(0);
            pets.PetSummonCount = 343;
            pets.HatchSlotBonus = 2;
            PetSkillSave.WritePets(s, pets);

            var sk = new SkillState();
            sk.Skills.Add("fireball", new SkillEntry { Level = 61, Dupes = 1, Stars = 2 });
            sk.Skills.Add("powerStrike", new SkillEntry { Level = 3, Dupes = 0, Stars = 0 });
            sk.Equipped.Add("fireball");
            sk.SummonCount = 265;
            PetSkillSave.WriteSkills(s, sk);

            string json = MiniJson.Serialize(s);
            StringAssert.Contains(@"""eggs"":[{""rarity"":""rare""},{""rarity"":""mythic""}]", json);
            StringAssert.Contains(@"""hatching"":[{""rarity"":""epic"",""endsAt"":1700000123456}]", json);
            StringAssert.Contains(@"""pets"":[{""name"":""Treant"",""rarity"":""ultimate"",""level"":6,""dupes"":0,""xp"":87988,""stars"":1,""subs"":[{""key"":""critChance"",""label"":""치명타 확률"",""value"":10},{""key"":""doubleChance"",""label"":""더블 찬스"",""value"":16.9}]}]", json);
            StringAssert.Contains(@"""activePets"":[0]", json);
            StringAssert.Contains(@"""petSummonCount"":343", json);
            StringAssert.Contains(@"""hatchSlotBonus"":2", json);
            StringAssert.Contains(@"""skills"":{""fireball"":{""level"":61,""dupes"":1,""stars"":2},""powerStrike"":{""level"":3,""dupes"":0,""stars"":0}}", json);
            StringAssert.Contains(@"""equippedSkills"":[""fireball""]", json);
            StringAssert.Contains(@"""summonCount"":265", json);

            // 다시 읽어도 같은 상태
            PetState p2 = PetSkillSave.ReadPets(s);
            Assert.AreEqual(2, p2.Eggs.Count);
            Assert.AreEqual(16.9, p2.Pets[0].Subs[1].Value, 1e-12);
            Assert.AreEqual(1, p2.Pets[0].Stars);
            SkillState s2 = PetSkillSave.ReadSkills(s);
            Assert.AreEqual(61, s2.Skills["fireball"].Level);
            Assert.AreEqual("fireball", s2.Skills.KeyAt(0), "삽입 순서 보존(원작 순회 순서)");
            Assert.AreEqual(before(json), MiniJson.Serialize(s), "두 번째 되쓰기도 같은 글자");
        }

        static string before(string x) { return x; }

        [Test]
        public void 소환_배수는_x1_x5_x25_x75_순환이다()
        {
            JsonObject s = Default();
            Assert.AreEqual(1, PetSkillSave.SummonMult(s, "skill"));
            Assert.AreEqual(5, PetSkillSave.CycleSummonMult(s, "skill"));
            Assert.AreEqual(25, PetSkillSave.CycleSummonMult(s, "skill"));
            Assert.AreEqual(75, PetSkillSave.CycleSummonMult(s, "skill"));
            Assert.AreEqual(1, PetSkillSave.CycleSummonMult(s, "skill"));
            Assert.AreEqual(1, PetSkillSave.SummonMult(s, "pet"), "다른 종류는 그대로");
            StringAssert.Contains(@"""summonMult"":{""skill"":1,""pet"":1,""mount"":1}", MiniJson.Serialize(s));
            // 구세이브(칸 없음)는 1 로 폴백하고, 순환은 칸을 만든다
            var old = MiniJson.ParseObject("{}");
            Assert.AreEqual(1, PetSkillSave.SummonMult(old, "pet"));
            Assert.AreEqual(5, PetSkillSave.CycleSummonMult(old, "pet"));
            Assert.AreEqual(@"{""summonMult"":{""pet"":5}}", MiniJson.Serialize(old));
        }

        [Test]
        public void 승천_기술_칸을_읽는다()
        {
            JsonObject s = Default();
            var asc = PetSkillSave.ReadAscension(s);
            Assert.AreEqual(0, asc.LineAscend["pet"]);
            Assert.AreEqual(4, asc.LineAscend.Count);
            var tech = PetSkillSave.ReadTech(s);
            Assert.AreEqual(0, tech.Tech.Count);
            Assert.IsNull(tech.Research, "techResearch: null");

            var s2 = MiniJson.ParseObject(@"{""lineAscend"":{""forge"":0,""skill"":1,""pet"":2,""mount"":0},""tech"":{""petDmg@1"":3},""techResearch"":{""id"":""petDmg@2"",""endsAt"":5}}");
            Assert.AreEqual(2, PetSkillSave.ReadAscension(s2).LineAscend["pet"]);
            var t2 = PetSkillSave.ReadTech(s2);
            Assert.AreEqual(3, t2.Level("petDmg@1"));
            Assert.AreEqual("petDmg@2", t2.Research.Id);
            Assert.AreEqual(5, t2.Research.EndsAt, 1e-9);
        }
    }
}
