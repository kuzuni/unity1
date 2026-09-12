using System.Collections.Generic;
using Forge.Core.Ascend;
using Forge.Core.Data;
using Forge.Core.Pets;
using Forge.Core.Skills;
using Forge.Core.Tech;

namespace Forge.Core.PetSave
{
    /// <summary>
    /// 원작 `S`(세이브 트리 · T13 <c>SaveState.Root</c>)의 펫·스킬 칸 ↔ Core 상태(T16 <see cref="PetState"/> · T17 <see cref="SkillState"/>) 코덱(ROUTINE T20).
    /// 원작은 `S.pets` 같은 객체를 그 자리에서 고치므로 «읽기 → 규칙 실행 → 되쓰기» 가 곧 원작의 한 프레임이다. 키·순서·수 표현은 원작 `defaultState()`·`pets.js`·`skills.js` 그대로:
    /// `eggs[{rarity}]` · `hatching[{rarity,endsAt}]` · `pets[{name,rarity,level,dupes,xp,stars,subs[{key,label,value}]}]` · `activePets[idx]` · `petSummonCount` · `hatchSlotBonus` ·
    /// `skills{id:{level,dupes,stars}}` · `equippedSkills[id]` · `summonCount` · `summonMult{skill,pet,mount}` · `lineAscend{…}` · `tech{}`·`techResearch`.
    /// 되쓰기는 **있는 키를 제자리에서 덮어** JSON 키 순서(=원작 저장 순서)를 지킨다. UnityEngine 참조 0 — dotnet <c>PetSkillSaveTests</c> 가 왕복을 잰다.
    /// </summary>
    public static class PetSkillSave
    {
        /// <summary>원작 `UI.SUMMON_MULTS` — 소환 배수 4단 순환(x1→x5→x25→x75 · 스킬·펫·탈것 공통).</summary>
        public static readonly int[] SummonMults = { 1, 5, 25, 75 };

        // ===== 펫 =====

        public static PetState ReadPets(JsonObject s)
        {
            var st = new PetState();
            var eggs = J.Arr(s["eggs"]);
            if (eggs != null)
                foreach (object v in eggs)
                {
                    var o = J.Obj(v);
                    if (o != null) st.Eggs.Add(new Egg(J.Str(o["rarity"])));
                }
            var hatching = J.Arr(s["hatching"]);
            if (hatching != null)
                foreach (object v in hatching)
                {
                    var o = J.Obj(v);
                    if (o != null) st.Hatching.Add(new HatchSlot(J.Str(o["rarity"]), J.Num(o["endsAt"])));
                }
            var pets = J.Arr(s["pets"]);
            if (pets != null)
                foreach (object v in pets)
                {
                    var o = J.Obj(v);
                    if (o == null) continue;
                    st.Pets.Add(new Pet
                    {
                        Name = J.Str(o["name"]),
                        Rarity = J.Str(o["rarity"], "common"),
                        Level = J.Int(o["level"], 1),
                        Dupes = J.Int(o["dupes"]),
                        Xp = J.Num(o["xp"]),
                        Stars = J.Int(o["stars"]),
                        Subs = ReadSubs(o["subs"]),
                    });
                }
            var active = J.Arr(s["activePets"]);
            if (active != null)
                foreach (object v in active) if (J.IsNum(v)) st.ActivePets.Add(J.Int(v));
            st.PetSummonCount = J.Int(s["petSummonCount"]);
            st.HatchSlotBonus = J.Int(s["hatchSlotBonus"]);
            return st;
        }

        public static void WritePets(JsonObject s, PetState st)
        {
            var eggs = new List<object>();
            foreach (Egg e in st.Eggs) { var o = new JsonObject(); o["rarity"] = e.Rarity; eggs.Add(o); }
            s["eggs"] = eggs;
            var hatching = new List<object>();
            foreach (HatchSlot h in st.Hatching) { var o = new JsonObject(); o["rarity"] = h.Rarity; o["endsAt"] = h.EndsAt; hatching.Add(o); }
            s["hatching"] = hatching;
            var pets = new List<object>();
            foreach (Pet p in st.Pets)
            {
                var o = new JsonObject();
                o["name"] = p.Name;
                o["rarity"] = p.Rarity;
                o["level"] = (double)p.Level;
                o["dupes"] = (double)p.Dupes;
                o["xp"] = p.Xp;
                o["stars"] = (double)p.Stars;
                o["subs"] = WriteSubs(p.Subs);
                pets.Add(o);
            }
            s["pets"] = pets;
            var active = new List<object>();
            foreach (int i in st.ActivePets) active.Add((double)i);
            s["activePets"] = active;
            s["petSummonCount"] = (double)st.PetSummonCount;
            s["hatchSlotBonus"] = (double)st.HatchSlotBonus;
        }

        // ===== 스킬 =====

        public static SkillState ReadSkills(JsonObject s)
        {
            var st = new SkillState();
            var skills = J.Obj(s["skills"]);
            if (skills != null)
                foreach (var kv in skills)
                {
                    var o = J.Obj(kv.Value);
                    if (o == null) continue;
                    st.Skills.Add(kv.Key, new SkillEntry { Level = J.Int(o["level"], 1), Dupes = J.Int(o["dupes"]), Stars = J.Int(o["stars"]) });
                }
            var eq = J.Arr(s["equippedSkills"]);
            if (eq != null)
                foreach (object v in eq) { string id = J.Str(v); if (id != null) st.Equipped.Add(id); }
            st.SummonCount = J.Int(s["summonCount"]);
            return st;
        }

        public static void WriteSkills(JsonObject s, SkillState st)
        {
            var skills = new JsonObject();
            for (int i = 0; i < st.Skills.Count; i++)
            {
                SkillEntry e = st.Skills.ValueAt(i);
                var o = new JsonObject();
                o["level"] = (double)e.Level;
                o["dupes"] = (double)e.Dupes;
                o["stars"] = (double)e.Stars;
                skills[st.Skills.KeyAt(i)] = o;
            }
            s["skills"] = skills;
            var eq = new List<object>();
            foreach (string id in st.Equipped) eq.Add(id);
            s["equippedSkills"] = eq;
            s["summonCount"] = (double)st.SummonCount;
        }

        // ===== 승천 · 기술트리(읽기만 — 펫·스킬 규칙이 배율을 묻는다 · 쓰기는 T21·T24 몫) =====

        public static AscensionState ReadAscension(JsonObject s)
        {
            var st = new AscensionState();
            var la = J.Obj(s["lineAscend"]);
            if (la != null) foreach (var kv in la) st.LineAscend[kv.Key] = J.Int(kv.Value);
            return st;
        }

        public static TechState ReadTech(JsonObject s)
        {
            var st = new TechState();
            var tech = J.Obj(s["tech"]);
            if (tech != null) foreach (var kv in tech) st.Tech[kv.Key] = J.Int(kv.Value);
            var r = J.Obj(s["techResearch"]);
            if (r != null && J.Str(r["id"]) != null) st.Research = new TechResearch(J.Str(r["id"]), J.Num(r["endsAt"]));
            return st;
        }

        // ===== 소환 배수(원작 UI.summonMult / cycleSummonMult · S.summonMult 에 저장) =====

        public static int SummonMult(JsonObject s, string kind)
        {
            var m = J.Obj(s["summonMult"]);
            int v = m != null ? J.Int(m[kind]) : 0;
            return v > 0 ? v : 1;
        }

        /// <summary>다음 배수로 넘기고 그 값을 돌려준다(목록에 없는 값이면 처음으로 · 원작 `indexOf = -1` → `[0]`).</summary>
        public static int CycleSummonMult(JsonObject s, string kind)
        {
            var m = J.Obj(s["summonMult"]);
            if (m == null) { m = new JsonObject(); s["summonMult"] = m; }
            int cur = SummonMult(s, kind);
            int idx = System.Array.IndexOf(SummonMults, cur);
            int next = SummonMults[(idx + 1) % SummonMults.Length];
            m[kind] = (double)next;
            return next;
        }

        // ===== 내부 =====

        static List<Substat> ReadSubs(object v)
        {
            var list = new List<Substat>();
            var arr = J.Arr(v);
            if (arr == null) return list;
            foreach (object x in arr)
            {
                var o = J.Obj(x);
                if (o == null) continue;
                list.Add(new Substat(J.Str(o["key"]), J.Str(o["label"]), J.Num(o["value"])));
            }
            return list;
        }

        static List<object> WriteSubs(List<Substat> subs)
        {
            var arr = new List<object>();
            if (subs == null) return arr;
            foreach (Substat s in subs)
            {
                var o = new JsonObject();
                o["key"] = s.Key;
                o["label"] = s.Label;
                o["value"] = s.Value;
                arr.Add(o);
            }
            return arr;
        }
    }
}
