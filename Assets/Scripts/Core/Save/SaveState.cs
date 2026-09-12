using System;
using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Save
{
    /// <summary>
    /// 원작의 `S`(게임 상태 통짜 객체 · state.js). 트리는 <see cref="JsonObject"/> 그대로 둔다 — 원작이 그렇듯 각 시스템
    /// (대장간 T14 · 펫 T16 · 스킬 T17 · 던전 T23 …)이 자기 칸을 `ensure()` 로 메꾸고 읽고 쓴다. 여기서는 state.js 가 직접
    /// 만지는 코어 칸만 강타입 접근자로 연다. 세이브 = <see cref="SaveCodec.Serialize"/>(JSON.stringify(S)).
    /// </summary>
    public sealed class SaveState
    {
        public readonly JsonObject Root;

        public SaveState(JsonObject root)
        {
            if (root == null) throw new ArgumentNullException("root");
            Root = root;
        }

        // ── 원시 접근(없는 키 = JS undefined = null · 수는 전부 double) ──
        public object this[string key] { get { return Root[key]; } set { Root[key] = value; } }
        public double Num(string key) { return J.Num(Root[key]); }
        public int Int(string key) { return J.Int(Root[key]); }
        public string Str(string key) { return J.Str(Root[key]); }
        public bool Bool(string key) { return J.Bool(Root[key]); }
        public JsonObject Obj(string key) { return J.Obj(Root[key]); }
        public List<object> Arr(string key) { return J.Arr(Root[key]); }
        public bool Has(string key) { return Root.Has(key); }

        // ── 코어 칸 ──
        public double Version { get { return Num("version"); } set { Root["version"] = value; } }
        public double CreatedAt { get { return Num("createdAt"); } set { Root["createdAt"] = value; } }
        public double LastSeen { get { return Num("lastSeen"); } set { Root["lastSeen"] = value; } }
        public double LastOfflineClaim { get { return Num("lastOfflineClaim"); } set { Root["lastOfflineClaim"] = value; } }
        public string Nickname { get { return Str("nickname"); } set { Root["nickname"] = value; } }

        public int Chapter { get { return Int("chapter"); } set { Root["chapter"] = (double)value; } }
        public int Stage { get { return Int("stage"); } set { Root["stage"] = (double)value; } }
        public int Difficulty { get { return Int("difficulty"); } set { Root["difficulty"] = (double)value; } }
        public int BestChapter { get { return Int("bestChapter"); } set { Root["bestChapter"] = (double)value; } }
        public int BestStage { get { return Int("bestStage"); } set { Root["bestStage"] = (double)value; } }
        public int BestDifficulty { get { return Int("bestDifficulty"); } set { Root["bestDifficulty"] = (double)value; } }
        public double Kills { get { return Num("kills"); } set { Root["kills"] = value; } }
        public double TotalCrafts { get { return Num("totalCrafts"); } set { Root["totalCrafts"] = value; } }

        public double Hammers { get { return Num("hammers"); } set { Root["hammers"] = value; } }
        public double Coins { get { return Num("coins"); } set { Root["coins"] = value; } }
        public double Gems { get { return Num("gems"); } set { Root["gems"] = value; } }
        public double Tickets { get { return Num("tickets"); } set { Root["tickets"] = value; } }
        public double Winders { get { return Num("winders"); } set { Root["winders"] = value; } }
        public double Potions { get { return Num("potions"); } set { Root["potions"] = value; } }
        public double EggCurrency { get { return Num("eggCurrency"); } set { Root["eggCurrency"] = value; } }

        public int ForgeLevel { get { return Int("forgeLevel"); } set { Root["forgeLevel"] = (double)value; } }
        /// <summary>대장간 업그레이드 완료 절대시각(ms) · null 이면 미진행(<see cref="AbsTimer"/>).</summary>
        public double? ForgeUpgradeEndsAt { get { return J.NumOrNull(Root["forgeUpgradeEndsAt"]); } set { Root["forgeUpgradeEndsAt"] = value.HasValue ? (object)value.Value : null; } }
        public bool AutoForgeOn { get { return Bool("autoForgeOn"); } set { Root["autoForgeOn"] = value; } }
        public bool AutoCast { get { return Bool("autoCast"); } set { Root["autoCast"] = value; } }
        public bool SfxOn { get { return Bool("sfxOn"); } set { Root["sfxOn"] = value; } }
        public bool MusicOn { get { return Bool("musicOn"); } set { Root["musicOn"] = value; } }

        public JsonObject Equipment { get { return Obj("equipment"); } }
        public JsonObject Skills { get { return Obj("skills"); } }
        public JsonObject ClearedBosses { get { return Obj("clearedBosses"); } }
        public List<object> EquippedSkills { get { return Arr("equippedSkills"); } }
        public List<object> Eggs { get { return Arr("eggs"); } }
        public List<object> Hatching { get { return Arr("hatching"); } }
        public List<object> Pets { get { return Arr("pets"); } }
        public List<object> ActivePets { get { return Arr("activePets"); } }
        public List<object> Mounts { get { return Arr("mounts"); } }
        public List<object> ActiveMounts { get { return Arr("activeMounts"); } }

        // ── 진행 좌표 도우미(원작 state.js 의 자유 함수) ──
        public int CurAbsChapter(SaveDefs d) { return d.AbsChapter(Difficulty, Chapter); }
        public int BestAbsChapter(SaveDefs d) { return d.AbsChapter(BestDifficulty, BestChapter); }
        public int CurRank(SaveDefs d) { return d.ProgressRank(Difficulty, Chapter, Stage); }
        public int BestRank(SaveDefs d) { return d.ProgressRank(BestDifficulty, BestChapter, BestStage); }

        /// <summary>원작 `stageName()` — «어려움 3-5» (상단 라벨·토스트·사망 배너가 전부 이걸 쓴다).</summary>
        public string StageName(SaveDefs d) { return d.StageDifficultyLabel(Chapter, Difficulty) + " " + Chapter + "-" + Stage; }

        /// <summary>원작 `stageKey()` — «1-3» · 티어가 붙으면 «d1:1-3»(티어 0 은 옛 키와 글자까지 같다 — clearedBosses 호환).</summary>
        public string StageKey() { return Difficulty > 0 ? "d" + Difficulty + ":" + Chapter + "-" + Stage : Chapter + "-" + Stage; }

        /// <summary>원작 `isUnlocked(key)` — 해금 기준은 전부 티어 0 좌표라 챕터를 그대로 절대 챕터로 쓴다. 표에 없는 키는 열린 것.</summary>
        public bool IsUnlocked(SaveDefs d, GameDefs g, string key)
        {
            Unlock def = null;
            for (int i = 0; i < g.Unlocks.Count; i++) if (g.Unlocks[i].Key == key) { def = g.Unlocks[i]; break; }
            if (def == null) return true;
            string[] cs = def.Stage.Split('-');
            int c = int.Parse(cs[0]), s = int.Parse(cs[1]);
            return BestRank(d) >= c * 100 + s;
        }
    }
}
