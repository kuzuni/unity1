using System;
using System.Collections.Generic;
using System.Globalization;

namespace Forge.Core.Meta
{
    /// <summary>원작 `S.passClaimed` — `{ "1-5": true }`.</summary>
    public sealed class PassState
    {
        public HashSet<string> Claimed = new HashSet<string>();
    }

    /// <summary>
    /// 원작 `pass.js` — 스테이지 도달 마일스톤. 무료만 실지급 · 프리미엄은 잠금 표시(더미).
    /// 도달 판정은 **최고 도달 절대 챕터**(`bestAbsChapter()`)로 잰다 — 난이도 티어가 오르면 챕터가 1로 되감기므로 생 챕터로 재면 마일스톤이 도로 잠긴다.
    /// </summary>
    public sealed class Pass
    {
        readonly PassTable _t;
        readonly StateDefs _s;
        public Pass(PassTable table, StateDefs state)
        {
            if (table == null) throw new ArgumentNullException("table");
            if (state == null) throw new ArgumentNullException("state");
            _t = table; _s = state;
        }
        public PassTable Table { get { return _t; } }

        /// <summary>`Pass.stageValue("3-5")` = (c − 1) × 챕터당 스테이지 + s.</summary>
        public int StageValue(string key)
        {
            int dash = key.IndexOf('-');
            if (dash < 0) throw new FormatException("stage key: " + key);
            int c = int.Parse(key.Substring(0, dash), CultureInfo.InvariantCulture);
            int s = int.Parse(key.Substring(dash + 1), CultureInfo.InvariantCulture);
            return (c - 1) * _s.StagesPerChapter + s;
        }

        /// <summary>`Pass.reached(key)` — `(bestAbsChapter − 1) × 10 + bestStage ≥ stageValue(key)`.</summary>
        public bool Reached(string key, int bestAbsChapter, int bestStage)
        {
            return (bestAbsChapter - 1) * _s.StagesPerChapter + bestStage >= StageValue(key);
        }

        public bool Claimed(PassState s, string key) { return s.Claimed != null && s.Claimed.Contains(key); }
        public bool CanClaim(PassState s, string key, int bestAbsChapter, int bestStage) { return Reached(key, bestAbsChapter, bestStage) && !Claimed(s, key); }

        /// <summary>`Pass.claim(key)` — 무료 칸만 지갑에 더한다. 모르는 키·미도달·이미 수령은 false.</summary>
        public bool Claim(PassState s, IRewardWallet wallet, string key, int bestAbsChapter, int bestStage)
        {
            var m = _t.Find(key);
            if (m == null || !CanClaim(s, key, bestAbsChapter, bestStage)) return false;
            wallet.AddAll(m.Free);
            if (s.Claimed == null) s.Claimed = new HashSet<string>();
            s.Claimed.Add(key);
            return true;
        }
    }
}
