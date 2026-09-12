using System;

namespace Forge.Core.Save
{
    /// <summary>기술트리(T24 · 원작 `TechTree.offline*Mult()`)가 주는 오프라인 배율. 연구가 없으면 전부 1.</summary>
    public struct OfflineMults
    {
        public double Cap, Coin, Hammer;
        public OfflineMults(double cap, double coin, double hammer) { Cap = cap; Coin = coin; Hammer = hammer; }
        public static OfflineMults One { get { return new OfflineMults(1, 1, 1); } }
    }

    /// <summary>원작 `offlineRewardFor()` 의 반환 — 재화는 정수 내림 · 수급률은 실수 표시값.</summary>
    public sealed class OfflineReward
    {
        /// <summary>`pendingOffline()` 이 붙이는 경과 초(내림 · 캡 전). `RewardFor` 만 부르면 0.</summary>
        public double Elapsed;
        /// <summary>캡을 씌운 뒤 실제로 센 초.</summary>
        public double Counted;
        public double Coins, Hammers;
        public double CoinRate, HammerRate;
    }

    /// <summary>
    /// 오프라인 보상(원작 state.js 하단 · 수급률 기반). 기본 수급률(코인 1/초 · 해머 1/분 · 캡 4시간 = `state.json`)에 기술트리
    /// 배율을 곱해 누적하되 재화는 정수부만 지급한다 — 소수부는 버리고 이월하지 않는다(주인 지시 2026-08-17). 내림을 여기 한 곳에서
    /// 하는 이유: 팝업 미리보기와 실제 지급이 같은 값을 쓰게(«603.87 로 보여 놓고 603 을 준다» 가 구조적으로 안 생기게).
    /// </summary>
    public static class Offline
    {
        /// <summary>원작 `offlineRewardFor(elapsedSec)`.</summary>
        public static OfflineReward RewardFor(SaveDefs d, double elapsedSec, OfflineMults m)
        {
            double cap = d.OfflineCapSec * m.Cap;
            double t = Math.Min(elapsedSec, cap);
            double coinRate = d.OfflineCoinPerSec * m.Coin;
            double hammerRate = d.OfflineHammerPerMin * m.Hammer;
            return new OfflineReward
            {
                Counted = t,
                Coins = Math.Floor(t * coinRate),
                Hammers = Math.Floor((t / 60) * hammerRate),
                CoinRate = coinRate,
                HammerRate = hammerRate,
            };
        }

        /// <summary>원작 `pendingOffline()` — 미수집 누적분(상태를 안 건드린다 · 1초 미만이면 null). 팝업은 이것을 미리보기로 쓴다.</summary>
        public static OfflineReward Pending(SaveDefs d, SaveState s, double nowMs, OfflineMults m)
        {
            double elapsed = Math.Floor((nowMs - s.LastOfflineClaim) / 1000);
            if (elapsed < 1) return null;
            var r = RewardFor(d, elapsed, m);
            r.Elapsed = elapsed;
            return r;
        }

        /// <summary>
        /// 원작 `claimOfflineNow()` — [수집] 전용. 여기서만 재화가 지급되고 기준 시각이 리셋된다. 정수 내림 결과가 전부 0 이면
        /// 수집을 성립시키지 않는다(기준 시각만 리셋되면 소수부가 통째로 날아가 짧게 누를수록 손해) → null.
        /// </summary>
        public static OfflineReward ClaimNow(SaveDefs d, SaveState s, double nowMs, OfflineMults m)
        {
            var r = Pending(d, s, nowMs, m);
            if (r == null) return null;
            if (r.Coins <= 0 && r.Hammers <= 0) return null;
            s.Coins = s.Coins + r.Coins;
            s.Hammers = s.Hammers + r.Hammers;
            s.LastOfflineClaim = nowMs;
            return r;
        }
    }
}
