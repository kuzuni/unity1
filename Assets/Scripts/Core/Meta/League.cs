using System;
using System.Collections.Generic;
using Forge.Core;
using Forge.Core.Data;

namespace Forge.Core.Meta
{
    /// <summary>원작 리그 봇 — `{ name, avatar, cp(Big · 세이브엔 문자열), score, server }`.</summary>
    public sealed class LeagueBot
    {
        public string Name;
        public string Avatar;
        public Big Cp;
        public double Score;
        public int Server;
    }

    /// <summary>원작 `S.league` — `{ score, tickets, lastTicketReset, seasonEndsAt, bots }`. `Bots == null` 이면 아직 시즌이 없다.</summary>
    public sealed class LeagueState
    {
        public double Score;
        public int Tickets;
        public string LastTicketReset;
        public double SeasonEndsAt;
        public List<LeagueBot> Bots;
    }

    /// <summary>`League.board()` 한 줄 — 봇 또는 나(`IsMe` · Server 는 «나»).</summary>
    public sealed class LeagueEntry
    {
        public string Name, Avatar;
        public Big Cp;
        public double Score;
        public string Server;
        public bool IsMe;
    }

    /// <summary>`League.challengeList()` 한 줄 — 봇 + ⭐ 보상(강한 순 5 → 1).</summary>
    public sealed class LeagueChallengeOption
    {
        public int Index;
        public LeagueBot Bot;
        public int StarReward;
    }

    /// <summary>`League.challenge(idx)` 의 반환.</summary>
    public sealed class LeagueChallenge
    {
        public bool Win;
        public LeagueBot Bot;
        /// <summary>이겼을 때만 별 · 졌으면 0.</summary>
        public int StarReward;
    }

    /// <summary>`League.ensure()` 가 무엇을 했는가 — 화면이 다시 그릴지·토스트를 띄울지 이걸로 안다.</summary>
    public sealed class LeagueEnsureResult
    {
        public bool SeasonStarted;
        public bool TicketsReset;
        /// <summary>시즌이 끝나 지급된 순위 보상(없으면 null) — 원작은 «🏆 리그 시즌 종료!» 토스트를 띄운다.</summary>
        public OrderedMap<double> SeasonReward;
        public int EndedRank;
    }

    /// <summary>
    /// 원작 `league.js` — 서버 없이 가짜 랭커 20명을 전투력 비교로 이기는 봇 리그(PvP · UI-SPEC 3~5). 시즌 3일 · 티켓 5/일(09:00 리셋).
    /// 봇 생성·승패는 난수(`Rng`)를 받는다 — 원작 `NAME_POOL` 섞기는 `sort(() => Math.random() - 0.5)`(V8 정렬 의존 · 재현 불가)라 Fisher-Yates 로 옮겼다(PROGRESS 결정 기록).
    /// 순위 보상은 표(`REWARD_BY_RANK` · 원작 `rewardForRank(r)` 를 1~21위로 평가한 것)에서 읽고 식을 다시 쓰지 않는다.
    /// </summary>
    public sealed class League
    {
        // 원작 league.js 함수 안 리터럴(표가 아니라 식의 상수 — 정본 줄 그대로).
        /// <summary>`genBots`: 봇 전투력 = 내 전투력 × U.rand(0.4, 2.5) · 최소 1.</summary>
        public const double BotCpMin = 0.4, BotCpMax = 2.5;
        /// <summary>`genBots`: 봇 점수 = round(U.rand(20, 200)).</summary>
        public const double BotScoreMin = 20, BotScoreMax = 200;
        /// <summary>`genBots`: 봇 서버 = U.randInt(1, 30).</summary>
        public const int BotServerMin = 1, BotServerMax = 30;
        /// <summary>`challenge`: 승률 = clamp(0.15 + myCp/(myCp+botCp) × 0.7, 0.05, 0.95).</summary>
        public const double WinBase = 0.15, WinSpan = 0.7, WinMin = 0.05, WinMax = 0.95;
        /// <summary>`board()` 의 내 서버 표기.</summary>
        public const string MyServer = "나";

        readonly LeagueTable _t;
        readonly AvatarTable _avatars;
        readonly Rng _rng;

        public League(LeagueTable table, AvatarTable avatars, Rng rng)
        {
            if (table == null) throw new ArgumentNullException("table");
            if (avatars == null) throw new ArgumentNullException("avatars");
            if (rng == null) throw new ArgumentNullException("rng");
            _t = table; _avatars = avatars; _rng = rng;
        }
        public LeagueTable Table { get { return _t; } }

        /// <summary>`League.ensure()` — 시즌이 없으면 시작 · 티켓 일일 리셋 · 시즌 종료 정산(보상 지급 + 점수 리셋 새 시즌).</summary>
        public LeagueEnsureResult Ensure(LeagueState s, IRewardWallet wallet, Big myCp, double nowMs, string todayKey)
        {
            if (s == null) throw new ArgumentNullException("s");
            var r = new LeagueEnsureResult();
            if (s.Bots == null) { StartSeason(s, false, myCp, nowMs, todayKey); r.SeasonStarted = true; }
            r.TicketsReset = CheckTicketReset(s, todayKey);
            int rank;
            r.SeasonReward = CheckSeasonEnd(s, wallet, myCp, nowMs, todayKey, out rank);
            r.EndedRank = rank;
            return r;
        }

        /// <summary>`League.startSeason(resetScore)` — resetScore 는 시즌 갱신(3일 종료) 때만 true(봇 점수가 새로 뽑히는 것과 맞춰 내 점수도 START_SCORE 로).</summary>
        public void StartSeason(LeagueState s, bool resetScore, Big myCp, double nowMs, string todayKey)
        {
            s.Score = resetScore || s.Score == 0 ? _t.StartScore : s.Score;
            s.Tickets = _t.TicketMax;
            s.LastTicketReset = todayKey;
            s.SeasonEndsAt = nowMs + _t.SeasonMs;
            s.Bots = GenBots(myCp);
        }

        /// <summary>`League.genBots(myCp)` — 이름 풀을 섞어 BOT_COUNT 명(모자라면 `Guest{i}`) · 난수 소비 순서 = 아바타 → cp 배율 → 점수 → 서버 · 전투력 강한 순 정렬(안정).</summary>
        public List<LeagueBot> GenBots(Big myCp)
        {
            var names = new List<string>(_t.NamePool);
            for (int i = names.Count - 1; i > 0; i--)
            {
                int j = _rng.RandInt(0, i);
                string tmp = names[i]; names[i] = names[j]; names[j] = tmp;
            }
            var bots = new List<LeagueBot>(_t.BotCount);
            for (int i = 0; i < _t.BotCount; i++)
            {
                var b = new LeagueBot();
                b.Name = i < names.Count && !string.IsNullOrEmpty(names[i]) ? names[i] : "Guest" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                b.Avatar = _rng.Choice(_avatars.Pool);
                b.Cp = myCp.Mul(Big.Of(_rng.Rand(BotCpMin, BotCpMax))).Max(Big.One);
                b.Score = JsNum.Round(_rng.Rand(BotScoreMin, BotScoreMax));
                b.Server = _rng.RandInt(BotServerMin, BotServerMax);
                bots.Add(b);
            }
            StableSort(bots, (a, b) => b.Cp.Cmp(a.Cp));
            return bots;
        }

        /// <summary>`League.checkTicketReset()` — 날짜 키가 바뀌면 티켓을 TICKET_MAX 로. 리셋했으면 true.</summary>
        public bool CheckTicketReset(LeagueState s, string todayKey)
        {
            if (s.LastTicketReset == todayKey) return false;
            s.LastTicketReset = todayKey;
            s.Tickets = _t.TicketMax;
            return true;
        }

        /// <summary>`League.checkSeasonEnd()` — 시즌이 끝났으면 내 순위 보상을 지갑에 더하고 점수 리셋 새 시즌. 지급한 보상(없으면 null).</summary>
        public OrderedMap<double> CheckSeasonEnd(LeagueState s, IRewardWallet wallet, Big myCp, double nowMs, string todayKey, out int rank)
        {
            rank = 0;
            if (nowMs < s.SeasonEndsAt) return null;
            rank = MyRank(s, myCp);
            var reward = RewardForRank(rank);
            wallet.AddAll(reward);
            StartSeason(s, true, myCp, nowMs, todayKey);
            return reward;
        }

        /// <summary>`League.board()` — 나를 포함한 전체 랭킹(점수 내림차순 · 같은 점수는 봇 순서 → 나 순 · 안정 정렬).</summary>
        public List<LeagueEntry> Board(LeagueState s, Big myCp, string myName = null, string myAvatar = null)
        {
            var list = new List<LeagueEntry>(s.Bots.Count + 1);
            for (int i = 0; i < s.Bots.Count; i++)
            {
                var b = s.Bots[i];
                list.Add(new LeagueEntry { Name = b.Name, Avatar = b.Avatar, Cp = b.Cp, Score = b.Score, Server = b.Server.ToString(System.Globalization.CultureInfo.InvariantCulture), IsMe = false });
            }
            list.Add(new LeagueEntry { Name = myName, Avatar = myAvatar, Cp = myCp, Score = s.Score, Server = MyServer, IsMe = true });
            StableSort(list, (a, b) => b.Score.CompareTo(a.Score));
            return list;
        }

        /// <summary>`League.myRank()` — 1부터.</summary>
        public int MyRank(LeagueState s, Big myCp)
        {
            var board = Board(s, myCp);
            for (int i = 0; i < board.Count; i++) if (board[i].IsMe) return i + 1;
            return 0;
        }

        /// <summary>`League.challengeList()` — 봇 목록 앞 CHALLENGE_COUNT(강한 순) · 순서대로 ⭐ CHALLENGE_COUNT … 1.</summary>
        public List<LeagueChallengeOption> ChallengeList(LeagueState s)
        {
            var list = new List<LeagueChallengeOption>();
            int n = Math.Min(_t.ChallengeCount, s.Bots.Count);
            for (int i = 0; i < n; i++) list.Add(new LeagueChallengeOption { Index = i, Bot = s.Bots[i], StarReward = _t.ChallengeCount - i });
            return list;
        }

        public bool CanChallenge(LeagueState s, int idx) { return s.Tickets > 0 && s.Bots != null && idx >= 0 && idx < s.Bots.Count; }

        /// <summary>`challenge` 의 승률 식 — 전투력이 Big 이라 나눗셈은 `RatioTo` 로(양쪽이 거대해져도 0~1).</summary>
        public double WinChance(Big myCp, Big botCp)
        {
            double p = WinBase + myCp.RatioTo(myCp.Add(botCp)) * WinSpan;
            return Math.Min(WinMax, Math.Max(WinMin, p));
        }

        /// <summary>`League.challenge(idx)` — 티켓 1 소모 · 이기면 점수 += ⭐. 못 하면 null.</summary>
        public LeagueChallenge Challenge(LeagueState s, int idx, Big myCp)
        {
            if (!CanChallenge(s, idx)) return null;
            var bot = s.Bots[idx];
            int star = _t.ChallengeCount - idx;
            bool win = _rng.Chance(WinChance(myCp, bot.Cp));
            s.Tickets--;
            if (win) s.Score += star;
            return new LeagueChallenge { Win = win, Bot = bot, StarReward = win ? star : 0 };
        }

        /// <summary>`League.rewardMult(rank)` — 표(REWARD_BY_RANK)에서 · 마지막 줄(21) 이 «21위 이하».</summary>
        public double RewardMult(int rank) { return RowFor(rank).Mult; }

        /// <summary>`League.rewardForRank(rank)` — 재화 6종(해머·코인·티켓·깨진 알·물약·태엽).</summary>
        public OrderedMap<double> RewardForRank(int rank) { return RowFor(rank).Reward; }

        LeagueRankReward RowFor(int rank)
        {
            var rows = _t.RewardByRank;
            if (rank < 1) rank = 1;
            return rows[Math.Min(rank, rows.Count) - 1];
        }

        /// <summary>JS `Array.prototype.sort` 는 안정 정렬(TimSort) — `List.Sort` 는 아니라서 삽입 정렬로 같은 순서를 낸다(원소 ≤ 21).</summary>
        static void StableSort<T>(List<T> list, Comparison<T> cmp)
        {
            for (int i = 1; i < list.Count; i++)
            {
                T v = list[i];
                int j = i - 1;
                while (j >= 0 && cmp(list[j], v) > 0) { list[j + 1] = list[j]; j--; }
                list[j + 1] = v;
            }
        }
    }
}
