using System;
using System.Collections.Generic;

namespace Forge.Core.Meta
{
    /// <summary>원작 `S.shop` — `{ lastReset, claimed: {key: true} }`.</summary>
    public sealed class ShopState
    {
        public string LastReset;
        public HashSet<string> Claimed = new HashSet<string>();
    }

    /// <summary>
    /// 원작 `shop.js` — 오늘의 특가 3종을 «무료 수령 1회/일» 로(원본 실결제 대체 · UI-SPEC 17) · 보석 패키지는 결제 더미.
    /// 젬은 어떤 경로로도 지급되지 않는다(«젬 보급 전면 제거») — `reward` 에 젬이 되살아나도 가산하지 않는다.
    /// </summary>
    public sealed class Shop
    {
        public const string Gems = "gems";
        readonly ShopTable _t;
        public Shop(ShopTable table) { if (table == null) throw new ArgumentNullException("table"); _t = table; }
        public ShopTable Table { get { return _t; } }

        /// <summary>`Shop.ensure()` — 날짜 키가 바뀌었으면 수령 기록을 비운다. 리셋이 일어났으면 true(열린 팝업을 다시 그린다).</summary>
        public bool Ensure(ShopState s, string todayKey)
        {
            if (s == null) throw new ArgumentNullException("s");
            if (s.Claimed == null) s.Claimed = new HashSet<string>();
            if (s.LastReset == null) { s.LastReset = todayKey; s.Claimed.Clear(); return false; }
            if (s.LastReset == todayKey) return false;
            s.LastReset = todayKey;
            s.Claimed.Clear();
            return true;
        }

        public bool Claimed(ShopState s, string key) { return s.Claimed != null && s.Claimed.Contains(key); }
        public bool CanClaim(ShopState s, string key) { return !Claimed(s, key); }

        /// <summary>`Shop.claimDeal(key)` — 특가 보상을 지갑에 더하고 오늘 수령으로 표시. 모르는 키·이미 받은 것은 false.</summary>
        public bool ClaimDeal(ShopState s, IRewardWallet wallet, string key)
        {
            var d = _t.Find(key);
            if (d == null || !CanClaim(s, key)) return false;
            wallet.AddAll(d.Reward, Gems);
            s.Claimed.Add(key);
            return true;
        }
    }
}
