using System;
using System.Collections.Generic;
using Forge.Core.Data;
using Forge.Core.Meta;
using Forge.Core.Save;

namespace Forge.Core.Meta
{
    /// <summary>
    /// T25 상태(<see cref="ShopState"/>·<see cref="PassState"/>·<see cref="QuestState"/>·<see cref="LeagueState"/>·<see cref="ChatState"/>) ↔ 세이브 트리(T13 <see cref="SaveState.Root"/>) 코덱(ROUTINE T22).
    /// 원작은 `S.shop`·`S.passClaimed`·`S.quests`+`S.questsCleared`·`S.league`·`S.chat` 칸을 각 모듈이 직접 읽고 쓴다 — 여기서는 그 칸의 **JSON 꼴을 글자까지 그대로** 지킨다
    /// (`shop = {lastReset, claimed:{key:true}}` · `passClaimed = {"1-5":true}` · `quests = [{id,need,prog,rw:{cur,amt}}]` · `league = {score,tickets,lastTicketReset,seasonEndsAt,bots:[{name,avatar,cp(Big 문자열),score,server}]}` ·
    /// `chat = {messages:[{type,tag,name,avatar,gender,text,at,mine | win,myName,myAvatar,myCp,oppName,oppAvatar,oppCp}], lastBotAt}`). 큰 수는 원작 `Big.toJSON` = `toString()`(«M e E») 문자열이다.
    /// 읽기는 손상에 관대하다(없는 칸 = 새 상태 · 원작 `ensure()` 가 채운다). 수는 전부 double(정본 JSON 규약).
    /// </summary>
    public static class MetaSave
    {
        public const string ShopKey = "shop", PassKey = "passClaimed", QuestsKey = "quests", QuestsClearedKey = "questsCleared", LeagueKey = "league", ChatKey = "chat";

        // ---- 상점 ----
        public static ShopState ReadShop(JsonObject root)
        {
            var s = new ShopState();
            var o = J.Obj(root[ShopKey]);
            if (o == null) return s;
            s.LastReset = J.Str(o["lastReset"]);
            var c = J.Obj(o["claimed"]);
            if (c != null) foreach (var kv in c) if (JsonTree.Truthy(kv.Value)) s.Claimed.Add(kv.Key);
            return s;
        }

        public static void WriteShop(JsonObject root, ShopState s)
        {
            var o = new JsonObject();
            o["lastReset"] = s.LastReset;
            var c = new JsonObject();
            if (s.Claimed != null) foreach (string k in s.Claimed) c[k] = true;
            o["claimed"] = c;
            root[ShopKey] = o;
        }

        // ---- 진행 패스 ----
        public static PassState ReadPass(JsonObject root)
        {
            var s = new PassState();
            var o = J.Obj(root[PassKey]);
            if (o != null) foreach (var kv in o) if (JsonTree.Truthy(kv.Value)) s.Claimed.Add(kv.Key);
            return s;
        }

        public static void WritePass(JsonObject root, PassState s)
        {
            var o = new JsonObject();
            if (s.Claimed != null) foreach (string k in s.Claimed) o[k] = true;
            root[PassKey] = o;
        }

        // ---- 퀘스트 ----
        public static QuestState ReadQuests(JsonObject root)
        {
            var s = new QuestState();
            s.Cleared = J.Num(root[QuestsClearedKey]);
            var arr = J.Arr(root[QuestsKey]);
            if (arr == null) return s;
            foreach (object item in arr)
            {
                var q = J.Obj(item);
                if (q == null) continue;
                var rwo = J.Obj(q["rw"]);
                s.Quests.Add(new Quest
                {
                    Id = J.Str(q["id"]),
                    Need = J.Num(q["need"], double.NaN),
                    Prog = J.Num(q["prog"], double.NaN),
                    Rw = rwo == null ? null : new QuestReward(J.Str(rwo["cur"]), J.Num(rwo["amt"])),
                });
            }
            return s;
        }

        public static void WriteQuests(JsonObject root, QuestState s)
        {
            var arr = new List<object>();
            if (s.Quests != null)
                foreach (var q in s.Quests)
                {
                    var o = new JsonObject();
                    o["id"] = q.Id;
                    o["need"] = q.Need;
                    o["prog"] = q.Prog;
                    var rw = new JsonObject();
                    if (q.Rw != null) { rw["cur"] = q.Rw.Cur; rw["amt"] = q.Rw.Amt; }
                    o["rw"] = rw;
                    arr.Add(o);
                }
            root[QuestsKey] = arr;
            root[QuestsClearedKey] = s.Cleared;
        }

        // ---- 리그 ----
        public static LeagueState ReadLeague(JsonObject root)
        {
            var o = J.Obj(root[LeagueKey]);
            if (o == null) return null;
            var s = new LeagueState
            {
                Score = J.Num(o["score"]),
                Tickets = J.Int(o["tickets"]),
                LastTicketReset = J.Str(o["lastTicketReset"]),
                SeasonEndsAt = J.Num(o["seasonEndsAt"]),
            };
            var bots = J.Arr(o["bots"]);
            if (bots != null)
            {
                s.Bots = new List<LeagueBot>();
                foreach (object item in bots)
                {
                    var b = J.Obj(item);
                    if (b == null) continue;
                    s.Bots.Add(new LeagueBot
                    {
                        Name = J.Str(b["name"]),
                        Avatar = J.Str(b["avatar"]),
                        Cp = ReadBig(b["cp"]),
                        Score = J.Num(b["score"]),
                        Server = J.Int(b["server"]),
                    });
                }
            }
            return s;
        }

        public static void WriteLeague(JsonObject root, LeagueState s)
        {
            if (s == null) { root[LeagueKey] = null; return; }
            var o = new JsonObject();
            o["score"] = s.Score;
            o["tickets"] = (double)s.Tickets;
            o["lastTicketReset"] = s.LastTicketReset;
            o["seasonEndsAt"] = s.SeasonEndsAt;
            if (s.Bots != null)
            {
                var bots = new List<object>();
                foreach (var b in s.Bots)
                {
                    var bo = new JsonObject();
                    bo["name"] = b.Name;
                    bo["avatar"] = b.Avatar;
                    bo["cp"] = b.Cp.ToString();
                    bo["score"] = b.Score;
                    bo["server"] = (double)b.Server;
                    bots.Add(bo);
                }
                o["bots"] = bots;
            }
            else o["bots"] = null;
            root[LeagueKey] = o;
        }

        // ---- 채팅 ----
        public static ChatState ReadChat(JsonObject root)
        {
            var s = new ChatState();
            var o = J.Obj(root[ChatKey]);
            if (o == null) return s;
            s.LastBotAt = J.Num(o["lastBotAt"], double.NaN);
            var arr = J.Arr(o["messages"]);
            if (arr == null) return s;
            s.Messages = new List<ChatMessage>();
            foreach (object item in arr)
            {
                var m = J.Obj(item);
                if (m == null) continue;
                s.Messages.Add(new ChatMessage
                {
                    Type = J.Str(m["type"], ChatMessage.TypeMsg),
                    Tag = J.Str(m["tag"]),
                    Name = J.Str(m["name"]),
                    Avatar = J.Str(m["avatar"]),
                    Gender = J.Str(m["gender"]),
                    Text = J.Str(m["text"]),
                    At = J.Num(m["at"]),
                    Mine = J.Bool(m["mine"]),
                    Win = J.Bool(m["win"]),
                    MyName = J.Str(m["myName"]),
                    MyAvatar = J.Str(m["myAvatar"]),
                    MyCp = ReadBig(m["myCp"]),
                    OppName = J.Str(m["oppName"]),
                    OppAvatar = J.Str(m["oppAvatar"]),
                    OppCp = ReadBig(m["oppCp"]),
                });
            }
            return s;
        }

        public static void WriteChat(JsonObject root, ChatState s)
        {
            var o = new JsonObject();
            var arr = new List<object>();
            if (s.Messages != null)
                foreach (var m in s.Messages)
                {
                    var mo = new JsonObject();
                    mo["type"] = m.Type ?? ChatMessage.TypeMsg;
                    if (m.Type == ChatMessage.TypeShare)
                    {
                        mo["win"] = m.Win;
                        if (m.Tag != null) mo["tag"] = m.Tag;
                        if (m.Name != null) mo["name"] = m.Name;
                        if (m.Gender != null) mo["gender"] = m.Gender;
                        mo["myName"] = m.MyName;
                        mo["myAvatar"] = m.MyAvatar;
                        mo["myCp"] = m.MyCp.ToString();
                        mo["oppName"] = m.OppName;
                        mo["oppAvatar"] = m.OppAvatar;
                        mo["oppCp"] = m.OppCp.ToString();
                        mo["at"] = m.At;
                        mo["mine"] = m.Mine;
                    }
                    else
                    {
                        if (m.Tag != null) mo["tag"] = m.Tag;
                        mo["name"] = m.Name;
                        mo["avatar"] = m.Avatar;
                        mo["gender"] = m.Gender;
                        mo["text"] = m.Text;
                        mo["at"] = m.At;
                        mo["mine"] = m.Mine;
                    }
                    arr.Add(mo);
                }
            o["messages"] = arr;
            o["lastBotAt"] = double.IsNaN(s.LastBotAt) ? (object)null : s.LastBotAt;
            root[ChatKey] = o;
        }

        /// <summary>원작 `Big.of(v)` — 세이브엔 문자열(«M e E»), 옛 세이브·리터럴은 수. 없으면 0.</summary>
        public static Big ReadBig(object v)
        {
            if (v is string) { try { return Big.Parse((string)v); } catch (Exception) { return Big.Zero; } }
            if (v is double) return Big.Of((double)v);
            return Big.Zero;
        }
    }
}
