using System.Collections.Generic;
using NUnit.Framework;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.Meta;

namespace Forge.Tests.EditMode
{
    /// <summary>T22 — T25 상태 ↔ 세이브 트리 코덱. 원작 `S.shop`·`S.passClaimed`·`S.quests`·`S.league`·`S.chat` 의 JSON 꼴을 글자까지 지키고, 없는 칸은 새 상태로 읽는다.</summary>
    public class MetaSaveTests
    {
        static JsonObject Root(string json) { return MiniJson.ParseObject(json); }

        [Test]
        public void 빈_트리는_새_상태를_준다()
        {
            var root = Root("{}");
            Assert.AreEqual(0, MetaSave.ReadShop(root).Claimed.Count);
            Assert.IsNull(MetaSave.ReadShop(root).LastReset);
            Assert.AreEqual(0, MetaSave.ReadPass(root).Claimed.Count);
            var q = MetaSave.ReadQuests(root);
            Assert.AreEqual(0, q.Cleared);
            Assert.AreEqual(0, q.Quests.Count);
            Assert.IsNull(MetaSave.ReadLeague(root), "원작 S.league 는 시즌 전엔 없다 — League.Ensure 가 만든다");
            var c = MetaSave.ReadChat(root);
            Assert.IsNull(c.Messages);
            Assert.IsTrue(double.IsNaN(c.LastBotAt));
        }

        [Test]
        public void 상점_패스_왕복은_원작_꼴이다()
        {
            var root = Root("{\"shop\":{\"lastReset\":\"Sat Sep 12 2026\",\"claimed\":{\"tech\":true,\"pet\":false}},\"passClaimed\":{\"1-5\":true,\"1-10\":true}}");
            var shop = MetaSave.ReadShop(root);
            Assert.AreEqual("Sat Sep 12 2026", shop.LastReset);
            CollectionAssert.AreEquivalent(new[] { "tech" }, shop.Claimed, "false 는 안 받은 것");
            var pass = MetaSave.ReadPass(root);
            CollectionAssert.AreEquivalent(new[] { "1-5", "1-10" }, pass.Claimed);

            var outRoot = new JsonObject();
            MetaSave.WriteShop(outRoot, shop);
            MetaSave.WritePass(outRoot, pass);
            string json = MiniJson.Serialize(outRoot);
            StringAssert.Contains("\"shop\":{\"lastReset\":\"Sat Sep 12 2026\",\"claimed\":{\"tech\":true}}", json);
            StringAssert.Contains("\"passClaimed\":{\"1-5\":true,\"1-10\":true}", json);
        }

        [Test]
        public void 퀘스트_왕복()
        {
            var root = Root("{\"quests\":[{\"id\":\"craft\",\"need\":10,\"prog\":3,\"rw\":{\"cur\":\"coins\",\"amt\":400}},{\"id\":\"sellGear\",\"need\":8,\"prog\":8,\"rw\":{\"cur\":\"hammers\",\"amt\":6}}],\"questsCleared\":2}");
            var q = MetaSave.ReadQuests(root);
            Assert.AreEqual(2, q.Cleared);
            Assert.AreEqual(2, q.Quests.Count);
            Assert.AreEqual("craft", q.Quests[0].Id);
            Assert.AreEqual(10, q.Quests[0].Need);
            Assert.AreEqual(3, q.Quests[0].Prog);
            Assert.AreEqual("coins", q.Quests[0].Rw.Cur);
            Assert.AreEqual(400, q.Quests[0].Rw.Amt);
            var outRoot = new JsonObject();
            MetaSave.WriteQuests(outRoot, q);
            Assert.AreEqual(MiniJson.Serialize(root), MiniJson.Serialize(outRoot));
        }

        [Test]
        public void 리그_봇_cp_는_Big_문자열이다()
        {
            var s = new LeagueState
            {
                Score = 70, Tickets = 3, LastTicketReset = "Sat Sep 12 2026", SeasonEndsAt = 1789500000000,
                Bots = new List<LeagueBot> { new LeagueBot { Name = "Bearopotamus", Avatar = "🛡️", Cp = Big.Of(50.8e9), Score = 120, Server = 7 } },
            };
            var root = new JsonObject();
            MetaSave.WriteLeague(root, s);
            string json = MiniJson.Serialize(root);
            StringAssert.Contains("\"cp\":\"" + Big.Of(50.8e9).ToString() + "\"", json, "원작 Big.toJSON = toString()");
            var back = MetaSave.ReadLeague(MiniJson.ParseObject(json));
            Assert.AreEqual(70, back.Score);
            Assert.AreEqual(3, back.Tickets);
            Assert.AreEqual("Sat Sep 12 2026", back.LastTicketReset);
            Assert.AreEqual(1789500000000, back.SeasonEndsAt);
            Assert.AreEqual(1, back.Bots.Count);
            Assert.IsTrue(back.Bots[0].Cp.Eq(Big.Of(50.8e9)));
            Assert.AreEqual(7, back.Bots[0].Server);

            var legacy = MetaSave.ReadLeague(Root("{\"league\":{\"score\":50,\"tickets\":5,\"bots\":[{\"name\":\"a\",\"avatar\":\"x\",\"cp\":123,\"score\":1,\"server\":2}]}}"));
            Assert.IsTrue(legacy.Bots[0].Cp.Eq(Big.Of(123)), "수로 적힌 옛 값도 읽는다(원작 Big.of)");
        }

        [Test]
        public void 채팅_왕복은_말풍선과_공유_카드를_가른다()
        {
            var s = new ChatState { LastBotAt = 1000, Messages = new List<ChatMessage>() };
            s.Messages.Add(new ChatMessage { Type = ChatMessage.TypeMsg, Tag = "MELK", Name = "MilkMessiah", Avatar = "🛡️", Gender = "♂", Text = "Ligma", At = 900, Mine = false });
            s.Messages.Add(new ChatMessage { Type = ChatMessage.TypeShare, Win = true, MyName = "용사", MyAvatar = "🛡️", MyCp = Big.Of(4.1e12), OppName = "Bearopotamus", OppAvatar = "🧑‍🚀", OppCp = Big.Of(50.8e9), At = 950, Mine = true });
            var root = new JsonObject();
            MetaSave.WriteChat(root, s);
            string json = MiniJson.Serialize(root);
            StringAssert.Contains("\"type\":\"msg\",\"tag\":\"MELK\",\"name\":\"MilkMessiah\"", json);
            StringAssert.Contains("\"type\":\"share\",\"win\":true", json);
            StringAssert.Contains("\"lastBotAt\":1000", json);
            var back = MetaSave.ReadChat(MiniJson.ParseObject(json));
            Assert.AreEqual(2, back.Messages.Count);
            Assert.AreEqual("Ligma", back.Messages[0].Text);
            Assert.AreEqual("MELK", back.Messages[0].Tag);
            Assert.IsTrue(back.Messages[1].Win);
            Assert.IsTrue(back.Messages[1].Mine);
            Assert.IsTrue(back.Messages[1].MyCp.Eq(Big.Of(4.1e12)));
            Assert.AreEqual("Bearopotamus", back.Messages[1].OppName);
            Assert.AreEqual(1000, back.LastBotAt);
        }
    }
}
