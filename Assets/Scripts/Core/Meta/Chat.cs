using System;
using System.Collections.Generic;
using Forge.Core;
using Forge.Core.Data;

namespace Forge.Core.Meta
{
    /// <summary>`Chat.persona(name)` — 같은 닉네임 = 같은 인물(클랜 태그·아바타·성별을 이름 해시로 결정).</summary>
    public sealed class ChatPersona
    {
        public string Name, Tag, Avatar, Gender;
    }

    /// <summary>원작 채팅 한 줄 — `type: 'msg'`(말풍선) 또는 `'share'`(리그 전투 공유 카드).</summary>
    public sealed class ChatMessage
    {
        public const string TypeMsg = "msg";
        public const string TypeShare = "share";

        public string Type;
        public string Tag, Name, Avatar, Gender, Text;
        public double At;
        public bool Mine;
        // share 카드
        public bool Win;
        public string MyName, MyAvatar, OppName, OppAvatar;
        public Big MyCp, OppCp;
    }

    /// <summary>원작 `S.chat` — `{ messages, lastBotAt }`.</summary>
    public sealed class ChatState
    {
        public List<ChatMessage> Messages;
        /// <summary>NaN = 아직 없음(원작의 `lastBotAt` 칸이 없거나 수가 아닌 세이브) → `Ensure` 가 지금 시각으로 채운다.</summary>
        public double LastBotAt = double.NaN;
    }

    /// <summary>
    /// 원작 `chat.js` — 서버리스 클론의 봇 채팅. 봇이 클랜태그+닉네임 풀에서 주기적으로 한담을 만들고, 내 메시지와 리그 전투 공유 카드를 섞는다.
    /// 문자열 표는 `meta.json`(T25) · 인물 해시·시드·틱 규칙만 여기 있다.
    /// </summary>
    public sealed class Chat
    {
        // 원작 chat.js 함수 안 리터럴(표가 아니라 식의 상수 — 정본 줄 그대로).
        /// <summary>`seed()`: 18개를 90초 간격으로 과거에 심는다(목록이 확실히 넘치게) · i = 2, 11 은 장문 · i = n − 2 는 공유 카드.</summary>
        public const int SeedCount = 18;
        public const double SeedGapMs = 90000;
        public const int SeedLongA = 2, SeedLongB = 11;
        /// <summary>`seedShareCard`: 원본 화면의 공유 카드는 다른 플레이어([MELK] MilkMessiah → Bearopotamus) 것 · 전투력 4.1e12 vs 50.8e9.</summary>
        public const string SeedShareWinner = "MilkMessiah", SeedShareLoser = "Bearopotamus";
        public const double SeedShareWinCp = 4.1e12, SeedShareLoseCp = 50.8e9;
        /// <summary>`tick()`: 첫 간격 20초 · 이후 U.rand(15000, 40000).</summary>
        public const double FirstGapMs = 20000, GapMinMs = 15000, GapMaxMs = 40000;
        /// <summary>`sendPlayer`: 200자 절단.</summary>
        public const int MaxPlayerTextLen = 200;
        public const string GenderMale = "♂", GenderFemale = "♀";

        readonly ChatTable _t;
        readonly AvatarTable _avatars;
        readonly Rng _rng;
        double _nextGap = FirstGapMs;

        public Chat(ChatTable table, AvatarTable avatars, Rng rng)
        {
            if (table == null) throw new ArgumentNullException("table");
            if (avatars == null) throw new ArgumentNullException("avatars");
            if (rng == null) throw new ArgumentNullException("rng");
            _t = table; _avatars = avatars; _rng = rng;
        }
        public ChatTable Table { get { return _t; } }

        /// <summary>`Chat.ensure()` — 하위 필드까지 실재를 보고(손상 세이브 방어) 비어 있으면 심는다.</summary>
        public void Ensure(ChatState s, double nowMs)
        {
            if (s == null) throw new ArgumentNullException("s");
            if (s.Messages == null) s.Messages = new List<ChatMessage>();
            if (double.IsNaN(s.LastBotAt) || double.IsInfinity(s.LastBotAt)) s.LastBotAt = nowMs;
            if (s.Messages.Count == 0) Seed(s, nowMs);
        }

        /// <summary>`Chat.seed()` — 원본(shot-043500)은 «스크롤된 백로그» 라 넘치는 수를 심고 장문 둘·공유 카드 하나를 끼운다.</summary>
        public void Seed(ChatState s, double nowMs)
        {
            int n = SeedCount;
            double gap = SeedGapMs, baseAt = nowMs - n * gap;
            for (int i = 0; i < n; i++)
            {
                if (i == n - 2) SeedShareCard(s, baseAt + i * gap);
                else if (i == SeedLongA || i == SeedLongB) PushBotMessage(s, baseAt + i * gap, _rng.Choice(_t.LongLines));
                else PushBotMessage(s, baseAt + i * gap, null);
            }
        }

        /// <summary>`Chat.seedShareCard(at)` — 남의 카드(mine:false).</summary>
        public void SeedShareCard(ChatState s, double at)
        {
            var win = Persona(SeedShareWinner);
            var lose = Persona(SeedShareLoser);
            Push(s, new ChatMessage
            {
                Type = ChatMessage.TypeShare, Win = true, Tag = win.Tag, Name = win.Name, Gender = win.Gender,
                MyName = win.Name, MyAvatar = win.Avatar, MyCp = Big.Of(SeedShareWinCp),
                OppName = lose.Name, OppAvatar = lose.Avatar, OppCp = Big.Of(SeedShareLoseCp), At = at, Mine = false
            });
        }

        /// <summary>`Chat.persona(name)` — h = (h × 31 + charCodeAt) &gt;&gt;&gt; 0 · 태그 h % tags · 아바타 (h &gt;&gt;&gt; 4) % pool · 성별 (h &gt;&gt;&gt; 8) &amp; 1.</summary>
        public ChatPersona Persona(string name)
        {
            uint h = 0;
            for (int i = 0; i < name.Length; i++)
            {
                unchecked { h = (uint)(((ulong)h * 31UL + name[i]) & 0xFFFFFFFFUL); }
            }
            return new ChatPersona
            {
                Name = name,
                Tag = _t.ClanTags[(int)(h % (uint)_t.ClanTags.Length)],
                Avatar = _avatars.Pool[(int)((h >> 4) % (uint)_avatars.Pool.Length)],
                Gender = ((h >> 8) & 1) != 0 ? GenderMale : GenderFemale
            };
        }

        public ChatPersona RandomBot() { return Persona(_rng.Choice(_t.Names)); }

        /// <summary>`Chat.pushBotMessage(at, text)` — text 가 null 이면 LINES 에서 뽑는다(난수 순서: 이름 → 줄).</summary>
        public void PushBotMessage(ChatState s, double at, string text)
        {
            var bot = RandomBot();
            Push(s, new ChatMessage
            {
                Type = ChatMessage.TypeMsg, Tag = bot.Tag, Name = bot.Name, Avatar = bot.Avatar, Gender = bot.Gender,
                Text = text ?? _rng.Choice(_t.Lines), At = at, Mine = false
            });
        }

        /// <summary>`Chat.push(entry)` — MAX_MESSAGES 를 넘으면 앞을 잘라낸다.</summary>
        public void Push(ChatState s, ChatMessage entry)
        {
            if (s.Messages == null) s.Messages = new List<ChatMessage>();
            s.Messages.Add(entry);
            int over = s.Messages.Count - _t.MaxMessages;
            if (over > 0) s.Messages.RemoveRange(0, over);
        }

        /// <summary>`Chat.tick()` — 1초 틱에서 부른다 · 15~40초 간격으로 봇 메시지 1개. 새 메시지가 생겼으면 true.</summary>
        public bool Tick(ChatState s, double nowMs)
        {
            Ensure(s, nowMs);
            if (nowMs - s.LastBotAt > _nextGap)
            {
                s.LastBotAt = nowMs;
                _nextGap = _rng.Rand(GapMinMs, GapMaxMs);
                PushBotMessage(s, nowMs, null);
                return true;
            }
            return false;
        }

        /// <summary>`Chat.sendPlayer(text)` — 공백뿐이면 false · 200자 절단 · mine:true. 닉네임·아바타·성별은 프로필(상태)에서 호출자가 준다.</summary>
        public bool SendPlayer(ChatState s, string text, string myName, string myAvatar, string myGender, double nowMs)
        {
            text = (text ?? "").Trim();
            if (text.Length == 0) return false;
            if (text.Length > MaxPlayerTextLen) text = text.Substring(0, MaxPlayerTextLen);
            Push(s, new ChatMessage { Type = ChatMessage.TypeMsg, Name = myName, Avatar = myAvatar, Gender = myGender, Tag = "", Text = text, At = nowMs, Mine = true });
            return true;
        }

        /// <summary>`Chat.shareLeagueResult(win, myCp, opp)` — 리그 도전 결과를 공유 카드로 자동 게시(mine:true).</summary>
        public void ShareLeagueResult(ChatState s, bool win, Big myCp, LeagueBot opp, string myName, string myAvatar, double nowMs)
        {
            Push(s, new ChatMessage
            {
                Type = ChatMessage.TypeShare, Win = win, MyName = myName, MyAvatar = myAvatar, MyCp = myCp,
                OppName = opp.Name, OppAvatar = opp.Avatar, OppCp = opp.Cp, At = nowMs, Mine = true
            });
        }

        public ChatMessage LastMessage(ChatState s, double nowMs)
        {
            Ensure(s, nowMs);
            return s.Messages.Count == 0 ? null : s.Messages[s.Messages.Count - 1];
        }
    }
}
