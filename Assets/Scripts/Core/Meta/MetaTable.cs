using System;
using System.Collections.Generic;
using System.IO;
using Forge.Core.Data;

namespace Forge.Core.Meta
{
    /// <summary>
    /// `meta.json`(T2 추출기가 정본 `shop.js`·`pass.js`·`quests.js`·`league.js`·`chat.js` + `state.js` 진행 상수 + `icongen.js` 아바타에서 뽑는다)을
    /// 강타입 표로 세운다(T25). 어느 수치도 코드 상수로 두지 않는다 — 표에 없는 것은 여기에도 없다.
    /// 파일 읽기는 호출자 몫(<see cref="GameData"/> 와 같은 규약) · 데스크톱·테스트는 <see cref="LoadDirectory"/>.
    /// </summary>
    public sealed class MetaTable
    {
        public const string File = "meta.json";

        public StateDefs State;
        public AvatarTable Avatars;
        public ShopTable Shop;
        public PassTable Pass;
        public QuestTable Quests;
        public LeagueTable League;
        public ChatTable Chat;
        public JsonObject Raw;

        public static MetaTable Load(string json) { return From(MiniJson.ParseObject(json)); }

        public static MetaTable From(JsonObject o)
        {
            return new MetaTable
            {
                State = StateDefs.From(J.Obj(J.Require(o, "state"))),
                Avatars = AvatarTable.From(J.Obj(J.Require(o, "avatars"))),
                Shop = ShopTable.From(J.Obj(J.Require(o, "shop"))),
                Pass = PassTable.From(J.Obj(J.Require(o, "pass"))),
                Quests = QuestTable.From(J.Obj(J.Require(o, "quests"))),
                League = LeagueTable.From(J.Obj(J.Require(o, "league"))),
                Chat = ChatTable.From(J.Obj(J.Require(o, "chat"))),
                Raw = o
            };
        }

        public static MetaTable LoadDirectory(string dir)
        {
            return Load(System.IO.File.ReadAllText(Path.Combine(dir, File)));
        }
    }

    /// <summary>`state.js` 의 진행 상수 — 난이도 티어 순환(`CHAPTERS_PER_CYCLE` · `MAX_DIFFICULTY`)과 챕터당 스테이지 수.</summary>
    public sealed class StateDefs
    {
        public int ChaptersPerCycle;
        public int StagesPerChapter;
        public string[] DifficultyNames;
        public int MaxDifficulty;

        public static StateDefs From(JsonObject o)
        {
            return new StateDefs
            {
                ChaptersPerCycle = J.Int(J.Require(o, "CHAPTERS_PER_CYCLE")),
                StagesPerChapter = J.Int(J.Require(o, "STAGES_PER_CHAPTER")),
                DifficultyNames = J.StrArr(J.Require(o, "DIFFICULTY_NAMES")),
                MaxDifficulty = J.Int(J.Require(o, "MAX_DIFFICULTY"))
            };
        }
    }

    /// <summary>`icongen.js` 의 아바타 한 벌(24) — 리그 봇 얼굴이 채팅 공유 카드로도 넘어가므로 한 풀을 같이 쓴다.</summary>
    public sealed class AvatarTable
    {
        public string DefaultAvatar;
        public string[] Pool;

        public static AvatarTable From(JsonObject o)
        {
            return new AvatarTable { DefaultAvatar = J.Str(J.Require(o, "DEFAULT_AVATAR")), Pool = J.StrArr(J.Require(o, "AVATAR_POOL")) };
        }
    }

    // ===================== shop =====================

    public sealed class ShopDeal
    {
        public string Key, Name, Icon, PriceKr;
        /// <summary>재화 → 지급량(원작 `reward` — 젬 칸은 정본에서 이미 뺐다).</summary>
        public OrderedMap<double> Reward;
    }

    public sealed class GemPack
    {
        public double Gems;
        public string PriceKr;
    }

    /// <summary>`shop.js` — 오늘의 특가 3(무료 수령 1회/일) · 보석 패키지 4(결제 더미).</summary>
    public sealed class ShopTable
    {
        public List<ShopDeal> Deals;
        public List<GemPack> GemPacks;

        public ShopDeal Find(string key)
        {
            for (int i = 0; i < Deals.Count; i++) if (Deals[i].Key == key) return Deals[i];
            return null;
        }

        public static ShopTable From(JsonObject o)
        {
            var t = new ShopTable { Deals = new List<ShopDeal>(), GemPacks = new List<GemPack>() };
            foreach (var v in J.Arr(J.Require(o, "DEALS")))
            {
                var d = J.Obj(v);
                t.Deals.Add(new ShopDeal { Key = J.Str(d["key"]), Name = J.Str(d["name"]), Icon = J.Str(d["icon"]), PriceKr = J.Str(d["priceKR"]), Reward = J.NumMap(J.Require(d, "reward")) });
            }
            foreach (var v in J.Arr(J.Require(o, "GEM_PACKS")))
            {
                var p = J.Obj(v);
                t.GemPacks.Add(new GemPack { Gems = J.Num(p["gems"]), PriceKr = J.Str(p["priceKR"]) });
            }
            return t;
        }
    }

    // ===================== pass =====================

    public sealed class PassMilestone
    {
        /// <summary>"c-s" — 절대 챕터가 아니라 표의 챕터 표기.</summary>
        public string Stage;
        public OrderedMap<double> Free;
        public OrderedMap<double> Premium;
    }

    /// <summary>`pass.js` — 스테이지 도달 마일스톤 16. 무료만 실지급 · 프리미엄은 잠금 표시.</summary>
    public sealed class PassTable
    {
        public List<PassMilestone> Milestones;
        public string PremiumPriceKr;

        public PassMilestone Find(string stage)
        {
            for (int i = 0; i < Milestones.Count; i++) if (Milestones[i].Stage == stage) return Milestones[i];
            return null;
        }

        public static PassTable From(JsonObject o)
        {
            var t = new PassTable { Milestones = new List<PassMilestone>(), PremiumPriceKr = J.Str(J.Require(o, "PREMIUM_PRICE_KR")) };
            foreach (var v in J.Arr(J.Require(o, "MILESTONES")))
            {
                var m = J.Obj(v);
                t.Milestones.Add(new PassMilestone { Stage = J.Str(m["stage"]), Free = J.NumMap(J.Require(m, "free")), Premium = J.NumMap(J.Require(m, "premium")) });
            }
            return t;
        }
    }

    // ===================== quests =====================

    public sealed class QuestDef
    {
        public string Id, Icon, Text;
        /// <summary>null 이면 개수 · "" 처럼 있으면 «코인 5,000 소비» 같은 양(원작 `unit`).</summary>
        public string Unit;
        public double Need, Step;
        public string RwCur;
        public double RwAmt;
    }

    /// <summary>`quests.js` — 고정 반복 퀘스트 14(날짜·요일 개념 없음 · 로테이션 없음).</summary>
    public sealed class QuestTable
    {
        public int StepCap;
        public List<QuestDef> Defs;
        public OrderedMap<string> CurKr;
        public string NoGemFallback;
        public string[] ForgeLocked;

        public QuestDef Def(string id)
        {
            for (int i = 0; i < Defs.Count; i++) if (Defs[i].Id == id) return Defs[i];
            return null;
        }

        public static QuestTable From(JsonObject o)
        {
            var t = new QuestTable
            {
                StepCap = J.Int(J.Require(o, "STEP_CAP")),
                Defs = new List<QuestDef>(),
                CurKr = J.StrMap(J.Require(o, "CUR_KR")),
                NoGemFallback = J.Str(J.Require(o, "NO_GEM_FALLBACK")),
                ForgeLocked = J.StrArr(J.Require(o, "FORGE_LOCKED"))
            };
            foreach (var v in J.Arr(J.Require(o, "DEFS")))
            {
                var d = J.Obj(v);
                var rw = J.Obj(J.Require(d, "rw"));
                t.Defs.Add(new QuestDef
                {
                    Id = J.Str(d["id"]), Icon = J.Str(d["icon"]), Text = J.Str(d["text"]), Unit = J.Str(d["unit"]),
                    Need = J.Num(d["need"]), Step = J.Num(d["step"]), RwCur = J.Str(rw["cur"]), RwAmt = J.Num(rw["amt"])
                });
            }
            return t;
        }
    }

    // ===================== league =====================

    public sealed class LeagueRewardTier
    {
        public string Label;
        public int Rank;
    }

    /// <summary>`League.rewardForRank(r)` 를 평가한 한 줄 — `Mult` 는 `rewardMult(r)` · 나머지 재화 6종.</summary>
    public sealed class LeagueRankReward
    {
        public int Rank;
        public double Mult;
        public OrderedMap<double> Reward;
    }

    /// <summary>`league.js` — 봇 20 · 티켓 5 · 도전 상대 5 · 시즌 3일 · 시작 점수 50 · 순위 보상 표(1~21위 · 21 = 21위 이하).</summary>
    public sealed class LeagueTable
    {
        public int TicketMax, BotCount, ChallengeCount;
        public double SeasonMs;
        public double StartScore;
        public string[] NamePool;
        public List<LeagueRewardTier> RewardTiers;
        public List<LeagueRankReward> RewardByRank;

        public static LeagueTable From(JsonObject o)
        {
            var t = new LeagueTable
            {
                TicketMax = J.Int(J.Require(o, "TICKET_MAX")),
                BotCount = J.Int(J.Require(o, "BOT_COUNT")),
                ChallengeCount = J.Int(J.Require(o, "CHALLENGE_COUNT")),
                SeasonMs = J.Num(J.Require(o, "SEASON_MS")),
                StartScore = J.Num(J.Require(o, "START_SCORE")),
                NamePool = J.StrArr(J.Require(o, "NAME_POOL")),
                RewardTiers = new List<LeagueRewardTier>(),
                RewardByRank = new List<LeagueRankReward>()
            };
            foreach (var v in J.Arr(J.Require(o, "REWARD_TIERS")))
            {
                var r = J.Obj(v);
                t.RewardTiers.Add(new LeagueRewardTier { Label = J.Str(r["label"]), Rank = J.Int(r["rank"]) });
            }
            foreach (var v in J.Arr(J.Require(o, "REWARD_BY_RANK")))
            {
                var r = J.Obj(v);
                var reward = new OrderedMap<double>();
                foreach (var kv in r)
                {
                    if (kv.Key == "rank" || kv.Key == "mult") continue;
                    reward.Add(kv.Key, J.Num(kv.Value));
                }
                t.RewardByRank.Add(new LeagueRankReward { Rank = J.Int(r["rank"]), Mult = J.Num(r["mult"]), Reward = reward });
            }
            return t;
        }
    }

    // ===================== chat =====================

    /// <summary>`chat.js` — 봇 한담 표시용 문자열 표(클랜 태그 · 닉네임 · 한 줄 · 장문).</summary>
    public sealed class ChatTable
    {
        public int MaxMessages;
        public string[] ClanTags, Names, Lines, LongLines;

        public static ChatTable From(JsonObject o)
        {
            return new ChatTable
            {
                MaxMessages = J.Int(J.Require(o, "MAX_MESSAGES")),
                ClanTags = J.StrArr(J.Require(o, "CLAN_TAGS")),
                Names = J.StrArr(J.Require(o, "NAMES")),
                Lines = J.StrArr(J.Require(o, "LINES")),
                LongLines = J.StrArr(J.Require(o, "LONG_LINES"))
            };
        }
    }
}
