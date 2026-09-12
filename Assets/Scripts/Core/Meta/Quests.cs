using System;
using System.Collections.Generic;
using Forge.Core;
using Forge.Core.Data;

namespace Forge.Core.Meta
{
    public sealed class QuestReward
    {
        public string Cur;
        public double Amt;
        public QuestReward() { }
        public QuestReward(string cur, double amt) { Cur = cur; Amt = amt; }
    }

    /// <summary>원작 `S.quests[i]` — `{ id, need, prog, rw:{cur, amt} }`.</summary>
    public sealed class Quest
    {
        public string Id;
        public double Need;
        public double Prog;
        public QuestReward Rw;
    }

    /// <summary>원작 `S.quests` + `S.questsCleared`.</summary>
    public sealed class QuestState
    {
        public double Cleared;
        public List<Quest> Quests = new List<Quest>();
    }

    /// <summary>`Quests.claim(i)` 의 반환 — 토스트가 그대로 읽는다.</summary>
    public sealed class QuestClaim
    {
        public string Cur;
        public double Amt;
        public string Text;
    }

    /// <summary>`Quests.claimAll()` 의 반환 — 재화별 합계와 건수.</summary>
    public sealed class QuestClaimAll
    {
        public int N;
        public OrderedMap<double> Gains = new OrderedMap<double>();
    }

    /// <summary>
    /// 원작 `quests.js` — 고정 반복 퀘스트. DEFS 전 항목이 DEFS 순서 그대로 상시 노출 · 수령하면 **같은 행동** 퀘스트가 제자리에서 진행도 0으로
    /// 다시 시작(요구치·보상은 누적 수령 tier 를 따라 상승 · STEP_CAP 에서 멈춤). 날짜·요일·일차 개념을 절대 넣지 않는다 — 시계가 없다.
    /// 대장간이 만렙이면 코인이 나갈 곳이 없어 `FORGE_LOCKED` 세 항목은 목록에서 내린다(이미 깬 것은 남긴다) — 그 판정은 `forgeUpgradable`(원작 `Forge.upgradeInfo()` 가 null 이 아닌가)로 받는다.
    /// </summary>
    public sealed class Quests
    {
        public const string Gems = "gems";
        readonly QuestTable _t;
        readonly Func<bool> _forgeUpgradable;

        /// <param name="forgeUpgradable">원작 `!!Forge.upgradeInfo()` — 만렙이면 false. null 이면 «모르면 막지 않는다»(로드 순서 방어와 같다).</param>
        public Quests(QuestTable table, Func<bool> forgeUpgradable = null)
        {
            if (table == null) throw new ArgumentNullException("table");
            _t = table; _forgeUpgradable = forgeUpgradable;
        }
        public QuestTable Table { get { return _t; } }

        public QuestDef Def(string id) { return _t.Def(id); }

        /// <summary>`Quests.ensure()` — 저장분을 id 로 이어받아 DEFS 순서로 고정 배치한다(구세이브의 젬 보상은 그 자리에서 다시 만든다 · 못 깨는 항목은 깬 것만 남긴다).</summary>
        public void Ensure(QuestState s)
        {
            if (s == null) throw new ArgumentNullException("s");
            if (double.IsNaN(s.Cleared) || double.IsInfinity(s.Cleared) || s.Cleared < 0) s.Cleared = 0;
            if (s.Quests == null) s.Quests = new List<Quest>();
            var saved = new Dictionary<string, Quest>();
            for (int i = 0; i < s.Quests.Count; i++)
            {
                var q = s.Quests[i];
                if (q == null || q.Id == null || Def(q.Id) == null || double.IsNaN(q.Need) || double.IsInfinity(q.Need) || q.Need <= 0) continue;
                if (!saved.ContainsKey(q.Id)) saved[q.Id] = q;
            }
            var next = new List<Quest>();
            for (int i = 0; i < _t.Defs.Count; i++)
            {
                var def = _t.Defs[i];
                Quest prev;
                saved.TryGetValue(def.Id, out prev);
                bool done = prev != null && Prog(prev) >= prev.Need;
                if (!Available(def) && !done) continue;
                if (prev != null)
                {
                    var rw = prev.Rw != null && prev.Rw.Cur == CurOf(def.RwCur) ? prev.Rw : RollReward(def, prev.Need);
                    next.Add(new Quest { Id = def.Id, Need = prev.Need, Prog = Math.Min(prev.Need, Math.Max(0, Prog(prev))), Rw = rw });
                }
                else next.Add(Fresh(s, def));
            }
            s.Quests = next;
        }

        static double Prog(Quest q) { return double.IsNaN(q.Prog) ? 0 : q.Prog; }

        /// <summary>`Quests.tier()` — 누적 완료 수 · STEP_CAP 에서 멈춘다.</summary>
        public double Tier(QuestState s) { return Math.Min(s.Cleared, _t.StepCap); }

        /// <summary>`Quests.needOf(def)` = need + step × tier.</summary>
        public double NeedOf(QuestState s, QuestDef def) { return def.Need + def.Step * Tier(s); }

        /// <summary>`Quests.curOf(def)` — 젬은 어떤 경로로도 보상 재화가 될 수 없다(대체 재화 NO_GEM_FALLBACK).</summary>
        public string CurOf(string cur) { return cur == Gems ? _t.NoGemFallback : cur; }

        /// <summary>`Quests.rollReward(def, need)` — 보상은 요구치와 같은 비율로 커진다(최소 1 · JS Math.round).</summary>
        public QuestReward RollReward(QuestDef def, double need)
        {
            double k = need / def.Need;
            return new QuestReward(CurOf(def.RwCur), Math.Max(1, JsNum.Round(def.RwAmt * k)));
        }

        /// <summary>`Quests.available(def)` — FORGE_LOCKED 항목은 대장간이 더 올릴 수 있을 때만.</summary>
        public bool Available(QuestDef def)
        {
            if (Array.IndexOf(_t.ForgeLocked, def.Id) < 0) return true;
            if (_forgeUpgradable == null) return true;
            return _forgeUpgradable();
        }

        /// <summary>`Quests.fresh(def)` — 이 행동의 새(진행도 0) 퀘스트 · 현재 tier 기준.</summary>
        public Quest Fresh(QuestState s, QuestDef def)
        {
            double need = NeedOf(s, def);
            return new Quest { Id = def.Id, Need = need, Prog = 0, Rw = RollReward(def, need) };
        }

        public List<Quest> List(QuestState s) { Ensure(s); return s.Quests; }
        public bool IsDone(Quest q) { return q.Prog >= q.Need; }

        /// <summary>`Quests.bump(action, n)` — 행동 지점 훅. 그 행동을 보는 미완 퀘스트의 진행도를 올린다. 하나라도 움직였으면 true(화면 갱신·저장).</summary>
        public bool Bump(QuestState s, string action, double n = 1)
        {
            if (!(n > 0)) return false;
            Ensure(s);
            bool touched = false;
            for (int i = 0; i < s.Quests.Count; i++)
            {
                var q = s.Quests[i];
                if (q.Id != action || IsDone(q)) continue;
                q.Prog = Math.Min(q.Need, q.Prog + n);
                touched = true;
            }
            return touched;
        }

        public bool CanClaim(QuestState s, int i)
        {
            var list = List(s);
            return i >= 0 && i < list.Count && IsDone(list[i]);
        }

        /// <summary>`Quests.claim(i)` — 지급 → questsCleared +1 → 같은 퀘스트가 제자리에서 진행도 0(못 깨는 상태가 됐으면 자리가 내려간다). 못 받으면 null.</summary>
        public QuestClaim Claim(QuestState s, IRewardWallet wallet, int i)
        {
            if (!CanClaim(s, i)) return null;
            var q = s.Quests[i];
            var def = Def(q.Id);
            string cur = CurOf(q.Rw.Cur);
            var got = new QuestClaim { Cur = cur, Amt = q.Rw.Amt, Text = def.Text };
            wallet.Add(cur, q.Rw.Amt);
            s.Cleared = s.Cleared + 1;
            if (Available(def)) s.Quests[i] = Fresh(s, def); else s.Quests.RemoveAt(i);
            return got;
        }

        /// <summary>`Quests.claimAll()` — 완료된 것을 큰 인덱스부터 `Claim` 으로 전부 수령(규칙을 다시 쓰지 않는다).</summary>
        public QuestClaimAll ClaimAll(QuestState s, IRewardWallet wallet)
        {
            var list = List(s);
            var idx = new List<int>();
            for (int i = 0; i < list.Count; i++) if (IsDone(list[i])) idx.Add(i);
            var r = new QuestClaimAll();
            for (int k = idx.Count - 1; k >= 0; k--)
            {
                var got = Claim(s, wallet, idx[k]);
                if (got == null) continue;
                r.Gains.Add(got.Cur, r.Gains.Get(got.Cur, 0) + got.Amt);
                r.N++;
            }
            return r;
        }

        /// <summary>`Quests.readyCount()` — 수령 대기 수(탭 배지).</summary>
        public int ReadyCount(QuestState s)
        {
            var list = List(s);
            int n = 0;
            for (int i = 0; i < list.Count; i++) if (IsDone(list[i])) n++;
            return n;
        }
    }
}
