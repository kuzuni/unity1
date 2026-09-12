using System;
using System.Collections.Generic;
using Forge.Core.Data;
using Forge.Core.Pets;

namespace Forge.Core.Mounts
{
    /// <summary>
    /// 원작 `S`(세이브 트리 · T13 <c>SaveState.Root</c>)의 탈것 칸 ↔ <see cref="MountState"/> 코덱 + 구세이브 이관(원작 `Mounts.migrateInventory`).
    /// 키·순서·수 표현은 원작 `defaultState()`·`mounts.js` 그대로: `mounts[{name,rarity,level,xp,stars,subs[{key,label,value}]}]` · `activeMounts[idx]` · `mountOpens`.
    /// 태엽(`winders`)은 지갑 칸이라 여기서 다루지 않는다. T20 <c>PetSkillSave</c> 와 같은 꼴 · UnityEngine 참조 0.
    /// </summary>
    public static class MountSave
    {
        /// <summary>
        /// `S.mounts` 읽기. 배열이면 그대로(상한 자르기) · **구세이브 맵** `{name: {rarity, level, dupes, stars, xp, subs}}` 이면 원작 `migrateInventory` 그대로 —
        /// `dupes` 는 그만큼의 실제 개체로 펼치되(옵션은 새로 굴린다 · <paramref name="rng"/>) 원본을 앞에, 펼친 중복을 뒤에 두어 250 을 넘기면 중복부터 잘린다 ·
        /// 이름 장착 목록은 원본 인덱스로 옮긴다. 이름 이관(폐기 종)은 여기서 하지 않는다(<see cref="MountSystem.Ensure"/>).
        /// </summary>
        public static MountState ReadMounts(JsonObject s, GameDefs defs, Rng rng, MountRules rules)
        {
            rules = rules ?? MountRules.Original();
            var st = new MountState();
            var arr = J.Arr(s["mounts"]);
            var active = J.Arr(s["activeMounts"]);
            if (arr != null)
            {
                foreach (object v in arr)
                {
                    var o = J.Obj(v);
                    if (o == null) continue;
                    st.Mounts.Add(ReadMount(o));
                }
                if (active != null) foreach (object v in active) if (IsInt(v)) st.ActiveMounts.Add(J.Int(v));
            }
            else
            {
                var old = J.Obj(s["mounts"]);
                var firsts = new List<Mount>();
                var extras = new List<Mount>();
                var firstIdx = new Dictionary<string, int>();
                if (old != null)
                    foreach (var kv in old)
                    {
                        var m = J.Obj(kv.Value) ?? new JsonObject();
                        string rarity = J.Str(m["rarity"]) ?? "common";
                        int stars = J.Int(m["stars"]);
                        firstIdx[kv.Key] = firsts.Count;
                        firsts.Add(new Mount
                        {
                            Name = kv.Key, Rarity = rarity,
                            Level = J.Int(m["level"]) != 0 ? J.Int(m["level"]) : 1,
                            Xp = J.Num(m["xp"]), Stars = stars,
                            Subs = J.Arr(m["subs"]) != null ? ReadSubs(m["subs"]) : SubstatRoll.Roll(defs, rng, 2)
                        });
                        int dupes = J.Int(m["dupes"]);
                        for (int d = 0; d < dupes; d++)
                            extras.Add(new Mount { Name = kv.Key, Rarity = rarity, Level = 1, Xp = 0, Stars = stars, Subs = SubstatRoll.Roll(defs, rng, 2) });
                    }
                st.Mounts.AddRange(firsts);
                st.Mounts.AddRange(extras);
                if (active != null)
                    foreach (object v in active)
                    {
                        if (v is string) { int i; if (firstIdx.TryGetValue((string)v, out i)) st.ActiveMounts.Add(i); }
                        else if (IsInt(v)) st.ActiveMounts.Add(J.Int(v));
                    }
            }
            if (st.Mounts.Count > rules.InvCap) st.Mounts.RemoveRange(rules.InvCap, st.Mounts.Count - rules.InvCap);
            st.MountOpens = J.Int(s["mountOpens"]);
            return st;
        }

        public static void WriteMounts(JsonObject s, MountState st)
        {
            var mounts = new List<object>();
            foreach (Mount m in st.Mounts)
            {
                var o = new JsonObject();
                o["name"] = m.Name;
                o["rarity"] = m.Rarity;
                o["level"] = (double)m.Level;
                o["xp"] = m.Xp;
                o["stars"] = (double)m.Stars;
                o["subs"] = WriteSubs(m.Subs);
                mounts.Add(o);
            }
            s["mounts"] = mounts;
            var active = new List<object>();
            foreach (int i in st.ActiveMounts) active.Add((double)i);
            s["activeMounts"] = active;
            s["mountOpens"] = (double)st.MountOpens;
        }

        /// <summary>
        /// T13 <c>SaveCodec.Load(…, mountsMigrate)</c> 에 꽂는 원작 `Mounts.migrateInventory()` 자리 — JSON 트리의 맵꼴 `mounts` 를 개체 배열로, 이름 장착 목록을 인덱스로 바꿔 쓴다.
        /// 이미 배열이면 상한만 자른다(원작과 같다).
        /// </summary>
        public static Action<JsonObject> MigrateInventoryHook(GameDefs defs, Rng rng, MountRules rules)
        {
            return s =>
            {
                MountState st = ReadMounts(s, defs, rng, rules);
                var mounts = new List<object>();
                foreach (Mount m in st.Mounts)
                {
                    var o = new JsonObject();
                    o["name"] = m.Name; o["rarity"] = m.Rarity; o["level"] = (double)m.Level; o["xp"] = m.Xp; o["stars"] = (double)m.Stars; o["subs"] = WriteSubs(m.Subs);
                    mounts.Add(o);
                }
                s["mounts"] = mounts;
                if (s["activeMounts"] is List<object>)
                {
                    var active = new List<object>();
                    foreach (int i in st.ActiveMounts) active.Add((double)i);
                    s["activeMounts"] = active;
                }
            };
        }

        static Mount ReadMount(JsonObject o)
        {
            return new Mount
            {
                Name = J.Str(o["name"]),
                Rarity = J.Str(o["rarity"], "common"),
                Level = J.Int(o["level"], 1),
                Xp = J.Num(o["xp"]),
                Stars = J.Int(o["stars"]),
                Subs = ReadSubs(o["subs"]),
            };
        }

        static bool IsInt(object v) { return v is double && Math.Floor((double)v) == (double)v && !double.IsInfinity((double)v); }

        public static List<Substat> ReadSubs(object v)
        {
            var list = new List<Substat>();
            var arr = J.Arr(v);
            if (arr == null) return list;
            foreach (object x in arr)
            {
                var o = J.Obj(x);
                if (o != null) list.Add(new Substat(J.Str(o["key"]), J.Str(o["label"]), J.Num(o["value"])));
            }
            return list;
        }

        public static List<object> WriteSubs(List<Substat> subs)
        {
            var arr = new List<object>();
            if (subs == null) return arr;
            foreach (Substat s in subs)
            {
                var o = new JsonObject();
                o["key"] = s.Key; o["label"] = s.Label; o["value"] = s.Value;
                arr.Add(o);
            }
            return arr;
        }
    }
}
