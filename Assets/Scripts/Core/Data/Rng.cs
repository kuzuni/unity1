using System;
using System.Collections.Generic;

namespace Forge.Core.Data
{
    /// <summary>[0,1) 난수원 — 원작 도구가 `Math.random` 자리에 심는 결정론 PRNG 둘을 그대로 둔다.</summary>
    public interface IRandomSource
    {
        double Next();
    }

    /// <summary>
    /// mulberry32 — 원작 `web/tools/shot-summon-result.js`·`probe-sr-crowd.js` 가 `Math.random` 에 심는 식 그대로
    /// (시드 14 = 소환 x5 등급 사다리 · 9 = 탈것 x1 신화 · 3 = x75 전 등급). 32비트 상태 하나 · 같은 시드 → 같은 수열.
    /// </summary>
    public sealed class Mulberry32 : IRandomSource
    {
        uint _a;
        public Mulberry32(uint seed) { _a = seed; }
        public void Reseed(uint seed) { _a = seed; }

        public double Next()
        {
            unchecked
            {
                _a += 0x6D2B79F5u;
                uint t = (_a ^ (_a >> 15)) * (1u | _a);
                t = (t + ((t ^ (t >> 7)) * (61u | t))) ^ t;
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }
    }

    /// <summary>
    /// xorshift32 — 원작 `web/tools/lib-seed.js`(`SEED_INIT` · 시드 0x2f6e2b1)와 같은 식. T2 의 `mobs-props.json` 표본이 이 수열로 뽑혔다.
    /// 시드 0 은 0 에 머문다(원작도 같다) — 쓰지 않는다.
    /// </summary>
    public sealed class Xorshift32 : IRandomSource
    {
        uint _s;
        public const uint LibSeed = 0x2f6e2b1;
        public Xorshift32(uint seed) { _s = seed; }
        public void Reseed(uint seed) { _s = seed; }

        public double Next()
        {
            unchecked
            {
                _s ^= _s << 13;
                _s ^= _s >> 17;
                _s ^= _s << 5;
                return _s / 4294967296.0;
            }
        }
    }

    /// <summary>
    /// 원작 `util.js` 의 난수 도우미(`U.rand`·`randInt`·`choice`·`chance`·`weightedPick`)를 난수원 위에 그대로 옮긴 것.
    /// 엔진(전투·대장간·펫)은 `Math.random` 대신 이것을 받아 시드 고정으로 원작 시뮬레이터와 판 단위 대조한다(T7).
    /// </summary>
    public sealed class Rng
    {
        readonly IRandomSource _src;
        public Rng(IRandomSource src) { if (src == null) throw new ArgumentNullException("src"); _src = src; }

        public static Rng Mulberry(uint seed) { return new Rng(new Mulberry32(seed)); }
        public static Rng Xorshift(uint seed) { return new Rng(new Xorshift32(seed)); }

        /// <summary>Math.random() — [0,1).</summary>
        public double Random() { return _src.Next(); }

        /// <summary>U.rand(a, b) = a + random × (b − a).</summary>
        public double Rand(double a, double b) { return a + _src.Next() * (b - a); }

        /// <summary>U.randInt(a, b) = floor(a + random × (b − a + 1)) — 양끝 포함.</summary>
        public int RandInt(int a, int b) { return (int)Math.Floor(a + _src.Next() * (b - a + 1)); }

        /// <summary>U.choice(arr) = arr[floor(random × length)].</summary>
        public T Choice<T>(IReadOnlyList<T> arr)
        {
            if (arr == null || arr.Count == 0) throw new ArgumentException("empty list");
            return arr[(int)Math.Floor(_src.Next() * arr.Count)];
        }

        /// <summary>U.chance(p) = random &lt; p.</summary>
        public bool Chance(double p) { return _src.Next() < p; }

        /// <summary>
        /// U.weightedPick({키: 확률}) — 합으로 정규화하는 가중치 추첨(합이 1 이든 100 이든 · 99.95 여도 된다).
        /// 순서대로 빼 내려가다 0 이하가 되는 키 · 끝까지 안 걸리면 마지막 키(원작 폴백).
        /// </summary>
        public string WeightedPick(OrderedMap<double> weights)
        {
            if (weights == null || weights.Count == 0) throw new ArgumentException("empty weights");
            double total = 0;
            for (int i = 0; i < weights.Count; i++) total += weights.ValueAt(i);
            double r = _src.Next() * total;
            for (int i = 0; i < weights.Count; i++)
            {
                r -= weights.ValueAt(i);
                if (r <= 0) return weights.KeyAt(i);
            }
            return weights.KeyAt(weights.Count - 1);
        }
    }
}
