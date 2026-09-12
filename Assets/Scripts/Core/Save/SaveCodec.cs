using System;
using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Save
{
    /// <summary>`SaveCodec.Load` 의 결과 — 원작 `loadGame()` 의 반환값(true = 세이브를 읽었다 · false = 새 게임)과 보정 수·경고.</summary>
    public sealed class LoadResult
    {
        public SaveState State;
        /// <summary>원작 `loadGame()` 반환값. 세이브가 객체가 아니거나 파싱이 실패하면 false(새 게임).</summary>
        public bool Loaded;
        /// <summary>원작 `ensureStateShape()` 가 메꾼 칸 수.</summary>
        public int Fixed;
        /// <summary>원작이 `console.error` 로 남기는 손상 세이브 보정 줄(장착 장비 subs·시대 키). 조용히 고치지 않고 짖는다.</summary>
        public readonly List<string> Warnings = new List<string>();
    }

    /// <summary>
    /// 원작 state.js 의 `saveGame()`/`loadGame()` — 통짜 JSON 세이브 + 구세이브·손상 세이브 보정(T13).
    /// 저장소(localStorage ↔ 파일)는 Game `SaveIo` 가 대고, 여기는 문자열 ↔ 상태 트리만 다룬다(순수 C# · EditMode 로 원작과 대조).
    /// 로드 순서는 원작 그대로: 파싱 → 객체 검사 → `!version` 이면 기본값 → <see cref="MigrateActiveMounts"/> → <see cref="EnsureStateShape"/> →
    /// <see cref="PruneDanglingRefs"/> → lastOfflineClaim 폴백 → 폐기 칸(inventory·heldCrafts) 정리. **순서를 바꾸면 구세이브의 장착 탈것이 조용히 벗겨진다**(원작 주석).
    /// </summary>
    public static class SaveCodec
    {
        /// <summary>원작 `saveGame()` — `S.lastSeen = U.now()` 뒤 `JSON.stringify(S)`(MiniJson 은 같은 글자를 낸다).</summary>
        public static string Serialize(SaveState s, double nowMs)
        {
            s.LastSeen = nowMs;
            return MiniJson.Serialize(s.Root);
        }

        /// <summary>
        /// 원작 `loadGame()`. <paramref name="raw"/> 가 null·빈 문자열이면 새 게임. <paramref name="mountsMigrate"/> 는 원작
        /// `Mounts.migrateInventory()`(탈것 인벤 맵 → 개체 배열 · T11) 자리 — 그 모듈이 옮겨지면 붙인다(없으면 원작의 `typeof Mounts === 'undefined'` 갈래).
        /// </summary>
        public static LoadResult Load(string raw, SaveDefs d, GameDefs g, double nowMs, Action<JsonObject> mountsMigrate = null)
        {
            var r = new LoadResult();
            try
            {
                if (!string.IsNullOrEmpty(raw))
                {
                    var s = MiniJson.Parse(raw) as JsonObject;
                    // 통짜 교체라 세이브가 객체가 아니면(배열·숫자·문자열·null) 그 뒤가 전부 무너진다
                    if (s == null) { r.State = new SaveState(d.DefaultState(nowMs)); r.Loaded = false; return r; }
                    if (!JsonTree.Truthy(s["version"])) s = d.DefaultState(nowMs);
                    MigrateActiveMounts(s, mountsMigrate);   // ensureStateShape 보다 먼저!
                    r.Fixed = EnsureStateShape(s, d, nowMs);  // 빠진/망가진 코어 필드를 기본값으로 메꾼다 (부팅 중단 방지)
                    PruneDanglingRefs(s, d, g, r.Warnings);   // 형태는 맞지만 대상이 없는 참조(없는 스킬 id·허공 펫 인덱스)를 끊는다
                    if (!JsonTree.Truthy(s["lastOfflineClaim"])) s["lastOfflineClaim"] = JsonTree.Truthy(s["lastSeen"]) ? s["lastSeen"] : (object)nowMs;
                    // 폐기된 보관함(S.inventory)·보류 큐(S.heldCrafts) 데이터는 조용히 버린다 — 보류는 이제 pendingCraft 1슬롯이 전부다(주인 확정 2026-08-17).
                    s.Remove("inventory");
                    if (JsonTree.Truthy(s["heldCrafts"]))
                    {
                        var held = s["heldCrafts"] as List<object>;
                        if (!JsonTree.Truthy(s["pendingCraft"]) && held != null && held.Count > 0 && JsonTree.Truthy(held[0])) s["pendingCraft"] = held[0];
                        s.Remove("heldCrafts");
                    }
                    r.State = new SaveState(s);
                    r.Loaded = true;
                    return r;
                }
            }
            catch (Exception) { /* 파싱 실패 → 새 게임 (원작과 같은 갈래) */ }
            r.State = new SaveState(d.DefaultState(nowMs));
            r.Loaded = false;
            return r;
        }

        /// <summary>
        /// 원작 `migrateActiveMounts()` — 구세이브 `activeMount: "Brown Horse"`(문자열 1개) → `activeMounts: ["Brown Horse"]`. 이름 → 인덱스 이관은
        /// `Mounts.migrateInventory` 몫(<paramref name="mountsMigrate"/>). 끝에 원작 `installMountCompat` 처럼 데이터 칸 `activeMount` 를 지운다(세이브에서 제거).
        /// </summary>
        public static void MigrateActiveMounts(JsonObject s, Action<JsonObject> mountsMigrate)
        {
            string legacy = s["activeMount"] as string;
            if (legacy != null && legacy.Length == 0) legacy = null;
            if (!(s["activeMounts"] is List<object>)) s["activeMounts"] = legacy != null ? new List<object> { legacy } : new List<object>();
            if (mountsMigrate != null) mountsMigrate(s);
            s.Remove("activeMount");
        }

        /// <summary>
        /// 원작 `ensureStateShape()` — 기본값 형태(shape)와 **다르면** 기본값으로: undefined → 기본값 · null → 기본값(기본값이 null 인 칸은 그대로) ·
        /// 타입 불일치 → 기본값 · 비유한 수 → 기본값 · 음수 → 하한(재화 0 · 진행 수치 1). 중첩은 `STATE_SHAPE_KEYS` 의 고정 형태 레코드만 한 겹 더. 반환 = 메꾼 칸 수.
        /// </summary>
        public static int EnsureStateShape(JsonObject s, SaveDefs d, double nowMs)
        {
            var dflt = d.DefaultState(nowMs);
            int fixedN = 0;
            foreach (string k in dflt.Keys)
            {
                object want = dflt[k];
                bool has = s.Has(k);
                object got = s[k];
                // 기본값이 null인 필드는 아무 값이나 받는다(구조상 '없음'이 정상 상태)
                if (want == null) { if (!has) { s[k] = null; fixedN++; } continue; }
                if (!has || got == null || JsonTree.Kind(got) != JsonTree.Kind(want)) { s[k] = want; fixedN++; continue; }
                if (want is double)
                {
                    double gd = (double)got;
                    if (double.IsNaN(gd) || double.IsInfinity(gd)) { s[k] = want; fixedN++; continue; }
                    double min = Array.IndexOf(d.MinOneKeys, k) >= 0 ? 1 : 0;
                    if (gd < min) { s[k] = min; fixedN++; continue; }
                }
                if (Array.IndexOf(d.ShapeKeys, k) >= 0)
                {
                    var wo = (JsonObject)want;
                    var go = (JsonObject)got;
                    foreach (string sk in wo.Keys)
                    {
                        object sw = wo[sk];
                        bool shas = go.Has(sk);
                        object sg = go[sk];
                        if (sw == null) { if (!shas) { go[sk] = null; fixedN++; } continue; }
                        if (!shas || sg == null || JsonTree.Kind(sg) != JsonTree.Kind(sw)) { go[sk] = sw; fixedN++; }
                    }
                }
            }
            return fixedN;
        }

        /// <summary>
        /// 원작 `pruneDanglingRefs()` — 타입은 맞는데 가리키는 대상이 없는 참조를 끊고 진행 좌표 상한을 씌운다: 없는 스킬 id · 객체가 아닌 펫/알/부화 ·
        /// 범위 밖·중복 출전 인덱스(상한까지) · 티어/챕터/스테이지/대장간 레벨 clamp · 최고 기록 끌어올리기 · 장착 장비 슬롯(subs 배열화 · 시대 키 보정).
        /// </summary>
        public static void PruneDanglingRefs(JsonObject s, SaveDefs d, GameDefs g, List<string> warnings)
        {
            var st = new SaveState(s);
            var eq = new List<object>();
            foreach (object id in st.EquippedSkills)
            {
                var sid = id as string;
                if (sid != null && g.Skill(sid) != null) eq.Add(sid);
            }
            if (eq.Count > d.SkillMaxActive) eq.RemoveRange(d.SkillMaxActive, eq.Count - d.SkillMaxActive);
            s["equippedSkills"] = eq;

            s["pets"] = OnlyObjects(st.Pets);
            s["eggs"] = OnlyObjects(st.Eggs);
            s["hatching"] = OnlyObjects(st.Hatching);

            // 출전 펫: 중복·범위 밖 인덱스를 걷어내고 상한(Pets.MAX_ACTIVE)까지 자른다 — 로드 경로가 유일한 관문이라 여기서 안 자르면 기존 세이브가 상한을 우회한다.
            s["activePets"] = ValidIndexes(st.ActivePets, st.Pets.Count, d.PetMaxActive);
            var mounts = s["mounts"] as List<object>;
            s["activeMounts"] = ValidIndexes(st.ActiveMounts, mounts == null ? -1 : mounts.Count, d.MountMaxActive);

            // 진행 좌표 상한: ensureStateShape 는 하한(0/1)만 보고 위쪽은 안 본다.
            s["difficulty"] = JsonTree.Clamp(Math.Floor(st.Num("difficulty")), 0, d.MaxDifficulty);
            s["bestDifficulty"] = JsonTree.Clamp(Math.Floor(st.Num("bestDifficulty")), 0, d.MaxDifficulty);
            s["chapter"] = JsonTree.Clamp(Math.Floor(st.Num("chapter")), 1, d.ChaptersPerCycle);
            s["bestChapter"] = JsonTree.Clamp(Math.Floor(st.Num("bestChapter")), 1, d.ChaptersPerCycle);
            s["stage"] = JsonTree.Clamp(Math.Floor(st.Num("stage")), 1, d.StagesPerChapter);
            s["bestStage"] = JsonTree.Clamp(Math.Floor(st.Num("bestStage")), 1, d.StagesPerChapter);
            s["forgeLevel"] = JsonTree.Clamp(Math.Floor(st.Num("forgeLevel")), 1, d.ForgeMaxLevel);
            // 최고 기록이 현재 진행보다 뒤처져 있으면(구세이브·손상) 현재 좌표로 끌어올린다 — 해금·패스가 되감기지 않게
            if (st.BestRank(d) < st.CurRank(d)) { st.BestDifficulty = st.Difficulty; st.BestChapter = st.Chapter; st.BestStage = st.Stage; }

            // 장비 슬롯은 STATE_SHAPE_KEYS 재귀가 '기본값이 null'이라 어떤 값이든 통과시킨다 — 문자열·배열이면 null 로.
            var equipment = st.Equipment;
            var slots = new List<string>(equipment.Keys);
            foreach (string slot in slots)
            {
                object itv = equipment[slot];
                if (itv != null && !(itv is JsonObject)) { equipment[slot] = null; continue; }
                var it = itv as JsonObject;
                if (it == null) continue;
                if (!(it["subs"] is List<object>))
                {
                    warnings.Add("[state] 장착 장비의 subs 가 배열이 아니다 — 빈 배열로 보정한다: " + slot + " " + Describe(it["subs"]));
                    it["subs"] = new List<object>();
                }
                var age = it["age"] as string;
                if (age == null || Array.IndexOf(g.Ages, age) < 0)
                {
                    string byIdx = null;
                    object ai = it["ageIdx"];
                    if (JsonTree.IsInteger(ai) && (double)ai >= 0 && (double)ai < g.Ages.Length) byIdx = g.Ages[(int)(double)ai];
                    string fixedAge = byIdx ?? g.Ages[0];
                    warnings.Add("[state] 장착 장비의 시대 키가 표에 없다 — 보정한다: " + slot + " " + Describe(it["age"]) + " → " + fixedAge);
                    it["age"] = fixedAge;
                    it["ageIdx"] = (double)Array.IndexOf(g.Ages, fixedAge);
                }
            }
        }

        static List<object> OnlyObjects(List<object> src)
        {
            var o = new List<object>();
            foreach (object x in src) if (x is JsonObject) o.Add(x);
            return o;
        }

        /// <summary>정수 · 0 이상 · 길이 미만 · 중복 없음 → 앞에서 <paramref name="cap"/> 개. 길이가 음수면(배열이 아님) 전부 버린다.</summary>
        static List<object> ValidIndexes(List<object> src, int length, int cap)
        {
            var seen = new HashSet<double>();
            var o = new List<object>();
            foreach (object x in src)
            {
                if (!JsonTree.IsInteger(x)) continue;
                double i = (double)x;
                if (i < 0 || length < 0 || i >= length || seen.Contains(i)) continue;
                seen.Add(i);
                o.Add(x);
            }
            if (o.Count > cap) o.RemoveRange(cap, o.Count - cap);
            return o;
        }

        static string Describe(object v)
        {
            if (v == null) return "undefined";
            if (v is string) return (string)v;
            return MiniJson.Serialize(v);
        }
    }
}
