using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Pets
{
    /// <summary>
    /// 원작 `U.rollSubs(count)` — `SUBSTATS` 풀에서 중복 없이 count 개를 뽑아 값은 등급 무관 `SUBSTAT_MIN`~최대치 · 소수 한 자리
    /// (`+(rand(min,max).toFixed(1))`). 장비(T15)·펫·탈것 공용 옵션 체계라 펫 폴더에 두되 이름은 원작 그대로.
    /// 난수 소비 순서 = 풀 인덱스(`randInt`) → 값(`rand`) · 한 줄에 둘.
    /// </summary>
    public static class SubstatRoll
    {
        public static List<Substat> Roll(GameDefs defs, Rng rng, int count)
        {
            var pool = new List<SubstatDef>(defs.Substats);
            var subs = new List<Substat>();
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int idx = rng.RandInt(0, pool.Count - 1);
                SubstatDef d = pool[idx];
                pool.RemoveAt(idx);
                double v = JsNum.ParseFloat(JsNum.ToFixed(rng.Rand(defs.SubstatMin, d.Max), 1));
                subs.Add(new Substat(d.Key, d.Label, v));
            }
            return subs;
        }
    }
}
