using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Forge.Core;
using Forge.Core.Ascend;
using Forge.Core.Data;
using Forge.Core.Dungeon;
using Forge.Core.Save;
using Forge.Core.Skills;
using Forge.Core.Tech;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T21 화면들의 접착층 — 세이브(T13 <see cref="SaveIo"/>) 위에 Core 던전(T23)·기술트리(T24)·승천(T24)을 세우고 화면에 건넨다.
    /// 원작에서 <c>S</c>·<c>Dungeons</c>·<c>TechTree</c>·<c>Ascension</c> 전역이 하던 몫. 기술 상태는 세이브 트리의 <c>tech</c>·<c>techResearch</c>,
    /// 승천 횟수는 <c>lineAscend</c> 에 원작 키 그대로 읽고 쓴다(T22 의 MetaSave 코덱과 같은 규약).
    /// 부팅 씬의 <see cref="Bootstrap"/> 아래에 스스로 선다(SaveIo 가 Ready 된 뒤 tech.json 을 읽고 <see cref="Ready"/>).
    /// </summary>
    [DefaultExecutionOrder(-800)]
    public sealed class DungeonUiHost : MonoBehaviour, IDungeonHost, IWallet
    {
        public static DungeonUiHost Instance { get; private set; }
        public static bool Ready { get; private set; }
        public static event Action OnReady;

        public TechData TechData { get; private set; }
        public Dungeons Dungeons { get; private set; }
        public TechTree Tech { get; private set; }
        public Ascension Asc { get; private set; }
        public AscensionState AscState { get; private set; }

        /// <summary>Core 던전이 부르는 화면 함수(원작 UI.toast · openDungeons · showDungeonClear …).</summary>
        public event Action<DungeonEvent> DungeonEvent;

        public SaveState S { get { return SaveIo.State; } }
        public GameData Data { get { return SaveIo.Data; } }

        readonly SkillRules skillRules = new SkillRules();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Instance != null) return;
            foreach (Bootstrap b in Resources.FindObjectsOfTypeAll<Bootstrap>())
            {
                if (!b.gameObject.scene.isLoaded) continue;
                Create(b.transform);
                return;
            }
        }

        public static DungeonUiHost Create(Transform parent)
        {
            if (Instance != null) return Instance;
            var go = new GameObject("DungeonUiHost");
            go.transform.SetParent(parent, false);
            return go.AddComponent<DungeonUiHost>();
        }

        private void Awake()
        {
            Instance = this;
            Ready = false;
            StartCoroutine(Boot());
        }

        private void OnDestroy()
        {
            if (Instance == this) { Instance = null; Ready = false; }
        }

        private IEnumerator Boot()
        {
            // 한 프레임 뒤에 시작 — SaveIo 는 Awake 안에서 동기로 Ready 가 되므로 같은 프레임에 UiRoot(탭바)가 아직 없을 수 있다(T22 결정과 같은 부팅 경쟁 · CI 런 32).
            yield return null;
            while (!SaveIo.Ready || UiRoot.Instance == null || UiRoot.Instance.TabBar == null)
            {
                if (SaveIo.Instance == null) { Debug.LogError("[DungeonUiHost] SaveIo 가 없다 — 세이브 없이는 던전·기술을 세울 수 없다"); yield break; }
                yield return null;
            }
            string json = null;
            string p = Path.Combine(Application.streamingAssetsPath, "data", TechData.File);
            if (File.Exists(p)) json = File.ReadAllText(p);
            else
            {
                using (var req = UnityWebRequest.Get(p))
                {
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success) json = req.downloadHandler.text;
                }
            }
            if (json == null) { Debug.LogError("[DungeonUiHost] StreamingAssets/data/" + TechData.File + " 를 못 읽었다"); yield break; }

            TechData = TechData.Load(json);
            Tech = new TechTree(TechData.Tech, Data.Defs, LoadTechState());
            Tech.Ensure(this);
            SaveTechState();

            Asc = new Ascension(TechData.Ascension, skillRules.SummonLevelCap, Data.Balance.Mounts.MaxLevel);
            AscState = LoadAscState();
            Asc.Ensure(AscState);
            SaveAscState();

            Dungeons = new Dungeons(S, this, Rng.Mulberry((uint)(SaveIo.NowMs() % 4294967295.0)), TimeZoneInfo.Local);
            Dungeons.Emitted += OnDungeonEvent;
            Dungeons.Ensure();

            Ready = true;
            DungeonSheet.Install(this);
            var h = OnReady;
            if (h != null) h();
        }

        private void OnDungeonEvent(DungeonEvent e)
        {
            var h = DungeonEvent;
            if (h != null) h(e);
        }

        // ===== IWallet (기술 연구 비용) =====
        public double Potions { get { return S.Potions; } set { S.Potions = value; } }
        public double Gems { get { return S.Gems; } set { S.Gems = value; } }

        // ===== IDungeonHost =====
        public double Now() { return SaveIo.NowMs(); }
        public int BestRank() { return S.BestRank(SaveIo.Defs); }
        public double ThiefHammerMult() { return Tech.ThiefHammerMult(); }
        public double ThiefCoinMult() { return Tech.ThiefCoinMult(); }
        public double DungeonTicketMult() { return Tech.DungeonTicketMult(); }
        public double DungeonPotionMult() { return Tech.DungeonPotionMult(); }
        /// <summary>퀘스트 진행(원작 Quests.bump) — 퀘스트 모듈(T22)이 서면 그쪽이 받는다. 지금은 기록만.</summary>
        public void QuestBump(string key) { LastQuestBump = key; }
        public string LastQuestBump { get; private set; }
        public void Save() { if (SaveIo.Instance != null) SaveIo.Instance.Save(); }

        // ===== 기술 상태 ↔ 세이브 트리 (원작 S.tech · S.techResearch) =====
        TechState LoadTechState()
        {
            var st = new TechState();
            JsonObject tech = S.Obj("tech");
            if (tech != null) foreach (KeyValuePair<string, object> kv in tech) st.Tech[kv.Key] = J.Int(kv.Value);
            JsonObject r = S.Obj("techResearch");
            if (r != null && J.Str(r["id"]) != null) st.Research = new TechResearch(J.Str(r["id"]), J.Num(r["endsAt"]));
            return st;
        }

        public void SaveTechState()
        {
            var tech = new JsonObject();
            foreach (KeyValuePair<string, int> kv in Tech.State.Tech) tech[kv.Key] = (double)kv.Value;
            S["tech"] = tech;
            if (Tech.State.Research == null) S["techResearch"] = null;
            else
            {
                var r = new JsonObject();
                r["id"] = Tech.State.Research.Id;
                r["endsAt"] = Tech.State.Research.EndsAt;
                S["techResearch"] = r;
            }
        }

        // ===== 승천 =====
        AscensionState LoadAscState()
        {
            var st = new AscensionState();
            JsonObject la = S.Obj("lineAscend");
            if (la != null) foreach (KeyValuePair<string, object> kv in la) st.LineAscend[kv.Key] = J.Int(kv.Value);
            return st;
        }

        void SaveAscState()
        {
            var la = new JsonObject();
            foreach (KeyValuePair<string, int> kv in AscState.LineAscend) la[kv.Key] = (double)kv.Value;
            S["lineAscend"] = la;
        }

        /// <summary>원작 Ascension.progress 가 읽는 네 지표 — 대장간 레벨 · 스킬 소환 레벨(Skills.summonLevel) · 펫 소환 레벨(Pets.summonLevel) · 탈것 레벨(Mounts.level).</summary>
        public AscensionLevels Levels()
        {
            var lv = new AscensionLevels();
            lv.ForgeLevel = S.ForgeLevel;
            lv.SkillSummonLevel = Math.Min(skillRules.SummonLevelCap, (int)Math.Floor(S.Num("summonCount") / skillRules.SummonsPerLevel) + 1);
            lv.PetSummonLevel = Math.Min(Data.Balance.Skills.MaxLevel, S.Int("petSummonCount") / PetSummonsPerLevel + 1);
            lv.MountLevel = MountLevel();
            return lv;
        }

        /// <summary>원작 pets.js summonLevel 의 «5» (T16 PetSystem.SummonLevel 과 같은 값).</summary>
        const int PetSummonsPerLevel = 5;

        /// <summary>원작 mounts.js level(): 누적 오픈 수가 다음 행의 needed 에 못 미치면 멈춘다.</summary>
        int MountLevel()
        {
            int lvl = 1;
            double opens = S.Num("mountOpens");
            for (int l = 1; l < Data.Balance.Mounts.MaxLevel; l++)
            {
                MountSummonRow row = Data.Balance.Mounts.SummonAt(l);
                if (row.NeededIsMax || opens < row.Needed) break;
                lvl = l + 1;
            }
            return lvl;
        }

        /// <summary>원작 Ascension.starBreakdown — 보유 아이템의 별 합계(장비 8슬롯 · 스킬 · 펫 · 탈것).</summary>
        public StarBreakdown Stars()
        {
            var gear = new List<int>(); var skill = new List<int>(); var pet = new List<int>(); var mount = new List<int>();
            JsonObject eq = S.Obj("equipment");
            if (eq != null) foreach (KeyValuePair<string, object> kv in eq) { JsonObject it = J.Obj(kv.Value); if (it != null) gear.Add(J.Int(it["stars"])); }
            JsonObject sk = S.Obj("skills");
            if (sk != null) foreach (KeyValuePair<string, object> kv in sk) { JsonObject it = J.Obj(kv.Value); if (it != null) skill.Add(J.Int(it["stars"])); }
            List<object> pets = S.Arr("pets");
            if (pets != null) foreach (object o in pets) { JsonObject it = J.Obj(o); if (it != null) pet.Add(J.Int(it["stars"])); }
            List<object> mounts = S.Arr("mounts");
            if (mounts != null) foreach (object o in mounts) { JsonObject it = J.Obj(o); if (it != null) mount.Add(J.Int(it["stars"])); }
            return Ascension.Breakdown(gear, skill, pet, mount);
        }

        /// <summary>원작 Ascension.ascend(line) — 지표 초기화 + 그 라인 보유물 소멸(ascension.js 원문 순서) + 횟수 +1 + 저장.</summary>
        public bool Ascend(string line)
        {
            bool ok = Asc.Ascend(line, Levels(), AscState, WipeLine);
            if (!ok) return false;
            SaveAscState();
            Save();
            return true;
        }

        void WipeLine(string line)
        {
            if (line == "forge")
            {
                S.ForgeLevel = 1;
                S.ForgeUpgradeEndsAt = null;
                JsonObject roll = S.Obj("rollLevel");
                if (roll != null) foreach (string k in new List<string>(roll.Keys)) roll[k] = 1.0;
                JsonObject eq = S.Obj("equipment");
                if (eq != null) foreach (string k in new List<string>(eq.Keys)) eq[k] = null;
            }
            else if (line == "skill")
            {
                S["summonCount"] = 0.0;
                S["skills"] = new JsonObject();
                S["equippedSkills"] = new List<object>();
            }
            else if (line == "pet")
            {
                S["petSummonCount"] = 0.0;
                S["pets"] = new List<object>();
                S["activePets"] = new List<object>();
            }
            else if (line == "mount")
            {
                S["mountOpens"] = 0.0;
                S["mounts"] = new List<object>();
                S["activeMounts"] = new List<object>();
            }
        }

        // ===== 화면 공용 =====

        /// <summary>상단바 재화(원작 renderTopBar) — 코인·젬.</summary>
        public void RenderTopBar()
        {
            if (Hud.Instance != null) Hud.Instance.SetCurrency(NumFmt.Fmt(S.Coins), NumFmt.Fmt(S.Gems));
        }

        /// <summary>원작 U.josa(word, '이/가') — 받침 있으면 앞, 없으면 뒤.</summary>
        public static string Josa(string word, string with, string without)
        {
            if (string.IsNullOrEmpty(word)) return without;
            char c = word[word.Length - 1];
            if (c < 0xAC00 || c > 0xD7A3) return without;
            return (c - 0xAC00) % 28 != 0 ? with : without;
        }
    }
}
