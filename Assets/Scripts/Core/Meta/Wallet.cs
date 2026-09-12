using System;
using System.Collections.Generic;
using Forge.Core.Data;
using Forge.Core.Save;

namespace Forge.Core.Meta
{
    /// <summary>
    /// 재화 지갑(키 기반) — 원작은 `S.coins`·`S.hammers`… 처럼 상태 객체의 숫자 칸이고 지급은 `S[k] = (S[k] || 0) + v` 한 줄이다.
    /// 상점·패스·퀘스트·리그는 재화를 **키**로 받으므로(보상 표가 `{coins, hammers, …}` 객체) 이 면을 쓴다 — T24 `Tech.IWallet`(물약·젬 속성)·T14 `Forge.Wallet` 과는 다른 면이다.
    /// 세이브(T13 `SaveState`)는 <see cref="SaveStateWallet"/> 로 꽂는다.
    /// </summary>
    public interface IRewardWallet
    {
        double Get(string cur);
        void Add(string cur, double amt);
    }

    /// <summary>사전 하나로 된 지갑(테스트·독립 실행용). 없는 재화는 0.</summary>
    public sealed class KeyedWallet : IRewardWallet
    {
        readonly Dictionary<string, double> _v = new Dictionary<string, double>();
        public double Get(string cur) { double v; return _v.TryGetValue(cur, out v) ? v : 0; }
        public void Add(string cur, double amt) { _v[cur] = Get(cur) + amt; }
        public IEnumerable<string> Currencies { get { return _v.Keys; } }
    }

    /// <summary>T13 세이브 루트 위의 지갑 — `S[k] = (S[k] || 0) + v` 를 `Root[k]` 로 그대로(없던 키는 0 에서 시작).</summary>
    public sealed class SaveStateWallet : IRewardWallet
    {
        readonly SaveState _s;
        public SaveStateWallet(SaveState s) { if (s == null) throw new ArgumentNullException("s"); _s = s; }
        public double Get(string cur) { return _s.Num(cur); }
        public void Add(string cur, double amt) { _s[cur] = _s.Num(cur) + amt; }
    }

    public static class RewardWalletExt
    {
        /// <summary>`for (const k in reward) S[k] = (S[k] || 0) + reward[k]` — `skip` 재화(젬 방어 가드)는 건너뛴다.</summary>
        public static void AddAll(this IRewardWallet w, OrderedMap<double> reward, string skip = null)
        {
            if (reward == null) return;
            for (int i = 0; i < reward.Count; i++)
            {
                string k = reward.KeyAt(i);
                if (skip != null && k == skip) continue;
                w.Add(k, reward.ValueAt(i));
            }
        }
    }
}
