using System.Collections.Generic;
using Forge.Core.Data;
using Forge.Core.Pets;

namespace Forge.Core.Gear
{
    /// <summary>
    /// 원작 `U.sumSubs(...subsLists)` 의 반환 객체 — SUBSTATS 13 키가 표 순서로 0 부터 깔리고, 목록의 값이 키별로 더해진다(표에 없는 키도 뒤에 붙는다 · 원작 그대로).
    /// </summary>
    public sealed class SubsBag
    {
        readonly List<string> _keys = new List<string>();
        readonly Dictionary<string, double> _v = new Dictionary<string, double>();

        public IReadOnlyList<string> Keys { get { return _keys; } }
        public int Count { get { return _keys.Count; } }

        /// <summary>없는 키는 0(원작 `bag.x` 가 undefined 인 자리는 heroStats 가 안 읽는다).</summary>
        public double this[string key] { get { double d; return _v.TryGetValue(key, out d) ? d : 0; } }
        public bool Has(string key) { return _v.ContainsKey(key); }

        public void Add(string key, double value)
        {
            double d;
            if (_v.TryGetValue(key, out d)) _v[key] = d + value;
            else { _keys.Add(key); _v[key] = value; }
        }

        // heroStats 가 읽는 칸(원작 이름 그대로)
        public double CritCh { get { return this["critCh"]; } }
        public double CritDmg { get { return this["critDmg"]; } }
        public double Block { get { return this["block"]; } }
        public double HpRegen { get { return this["hpRegen"]; } }
        public double Lifesteal { get { return this["lifesteal"]; } }
        public double DblAtk { get { return this["dblAtk"]; } }
        public double DmgPct { get { return this["dmgPct"]; } }
        public double MeleeDmg { get { return this["meleeDmg"]; } }
        public double RangedDmg { get { return this["rangedDmg"]; } }
        public double AtkSpd { get { return this["atkSpd"]; } }
        public double SkillDmg { get { return this["skillDmg"]; } }
        public double SkillCd { get { return this["skillCd"]; } }
        public double HpPct { get { return this["hpPct"]; } }

        /// <summary>`U.sumSubs` — 표(<see cref="GameDefs.Substats"/>) 순서로 0 을 깐 뒤 목록들을 더한다. null 목록은 빈 것.</summary>
        public static SubsBag Sum(GameDefs defs, params IList<Substat>[] lists)
        {
            var bag = new SubsBag();
            if (defs != null && defs.Substats != null) for (int i = 0; i < defs.Substats.Count; i++) bag.Add(defs.Substats[i].Key, 0);
            if (lists != null)
                for (int l = 0; l < lists.Length; l++)
                {
                    var subs = lists[l];
                    if (subs == null) continue;
                    for (int i = 0; i < subs.Count; i++) if (subs[i] != null) bag.Add(subs[i].Key, subs[i].Value);
                }
            return bag;
        }
    }
}
