using System.Collections.Generic;
using Forge.Core.Data;
using Forge.Core.Forging;
using Forge.Core.Pets;

namespace Forge.Core.Gear
{
    /// <summary>장비 계통의 세이브 상태 — 원작 `S.equipment[slot]`(8부위 · 비면 없음). 개체 동일성(참조)으로 «이전 장비» 를 돌려준다(원작 `equip` 의 prev).</summary>
    public sealed class GearState
    {
        public readonly Dictionary<string, ForgeItem> Equipment = new Dictionary<string, ForgeItem>();

        public ForgeItem Get(string slot) { ForgeItem it; return Equipment.TryGetValue(slot, out it) ? it : null; }
        public bool Has(string slot) { return Get(slot) != null; }
        public void Set(string slot, ForgeItem item) { if (item == null) Equipment.Remove(slot); else Equipment[slot] = item; }
    }

    /// <summary>`ForgeItem` ↔ JSON(세이브 T13 의 `equipment` 칸 · 원작 rollItem 객체 키 그대로). 반쪽 항목(레벨·등급 없음)도 그대로 든다 — 판매 경로가 거른다.</summary>
    public static class GearCodec
    {
        public static ForgeItem ItemFrom(JsonObject o)
        {
            if (o == null) return null;
            var it = new ForgeItem
            {
                Name = J.Str(o["name"]), Slot = J.Str(o["slot"]), Age = J.Str(o["age"]), AgeIdx = J.Int(o["ageIdx"], -1), Rarity = J.Str(o["rarity"]),
                Level = J.Num(o["level"], double.NaN), Main = J.Str(o["main"]), Value = J.Num(o["value"], double.NaN),
                WType = J.Str(o["wtype"]), NameIdx = J.Int(o["nameIdx"], -1), Stars = J.Int(o["stars"]),
            };
            var subs = J.Arr(o["subs"]);
            if (subs != null)
                for (int i = 0; i < subs.Count; i++)
                {
                    var s = J.Obj(subs[i]);
                    if (s != null) it.Subs.Add(new Substat(J.Str(s["key"]), J.Str(s["label"]), J.Num(s["value"])));
                }
            return it;
        }

        public static JsonObject ItemTo(ForgeItem it)
        {
            if (it == null) return null;
            var o = new JsonObject();
            o["name"] = it.Name; o["slot"] = it.Slot; o["age"] = it.Age; o["ageIdx"] = (double)it.AgeIdx; o["rarity"] = it.Rarity;
            o["level"] = it.Level; o["main"] = it.Main; o["value"] = it.Value;
            var subs = new List<object>();
            for (int i = 0; i < it.Subs.Count; i++)
            {
                var s = new JsonObject();
                s["key"] = it.Subs[i].Key; s["label"] = it.Subs[i].Label; s["value"] = it.Subs[i].Value;
                subs.Add(s);
            }
            o["subs"] = subs;
            o["wtype"] = it.WType; o["nameIdx"] = (double)it.NameIdx; o["stars"] = (double)it.Stars;
            return o;
        }

        /// <summary>`S.equipment` 객체 → 상태(부위 키만 · 값이 객체가 아니면 비운다).</summary>
        public static GearState StateFrom(JsonObject equipment, GameDefs defs)
        {
            var st = new GearState();
            if (equipment == null) return st;
            foreach (var kv in equipment)
            {
                var it = ItemFrom(J.Obj(kv.Value));
                if (it != null) st.Equipment[kv.Key] = it;
            }
            return st;
        }

        public static JsonObject StateTo(GearState st, GameDefs defs)
        {
            var o = new JsonObject();
            if (st == null) return o;
            var slots = defs != null && defs.Slots != null ? defs.Slots : new string[0];
            for (int i = 0; i < slots.Length; i++) { var it = st.Get(slots[i]); if (it != null) o[slots[i]] = ItemTo(it); }
            foreach (var kv in st.Equipment) if (System.Array.IndexOf(slots, kv.Key) < 0 && kv.Value != null) o[kv.Key] = ItemTo(kv.Value);
            return o;
        }
    }
}
