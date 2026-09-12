using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Forge.Core.Data;
using Forge.Core.Save;

namespace Forge.Game
{
    /// <summary>
    /// 세이브 입출력(ROUTINE T13 · 원작 main.js 의 저장 시점 + state.js 의 localStorage 자리). Core <see cref="SaveCodec"/> 가 문자열 ↔ 상태를 맡고
    /// 여기는 ⓐ `StreamingAssets/data/*.json` 을 읽어 <see cref="GameData"/>·<see cref="SaveDefs"/> 를 세우고 ⓑ `persistentDataPath/&lt;SAVE_KEY&gt;.json`
    /// 을 읽고 쓰며 ⓒ 원작과 같은 시점에 저장한다: **30초마다** · 앱이 뒤로 갈 때(`visibilitychange` hidden = OnApplicationPause/Focus) ·
    /// 종료(`beforeunload` = OnApplicationQuit). 부팅 시 자동 지급은 없다 — 누적분은 <see cref="PendingOffline"/> 로 보여주기만 하고 지급은 [수집](<see cref="ClaimOffline"/>) 뿐.
    /// UiRoot 처럼 `sceneLoaded` 훅으로 Bootstrap 아래 자립한다(씬 파일을 안 고친다).
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class SaveIo : MonoBehaviour
    {
        /// <summary>원작 main.js `setInterval(saveGame, 30000)`.</summary>
        public const float AutosaveIntervalSec = 30f;

        public static SaveIo Instance { get; private set; }
        /// <summary>정본 표(7 파일). 로드 전에는 null.</summary>
        public static GameData Data { get; private set; }
        public static SaveDefs Defs { get; private set; }
        /// <summary>원작의 `S`. <see cref="Ready"/> 전에는 null.</summary>
        public static SaveState State { get; private set; }
        public static bool Ready { get; private set; }
        /// <summary>원작 `loadGame()` 반환값 — false 면 새 게임.</summary>
        public static bool LoadedFromDisk { get; private set; }
        /// <summary>기술트리(T24)가 채운다 — 원작 `TechTree.offline*Mult()`. 기본 전부 1.</summary>
        public static OfflineMults Mults = OfflineMults.One;
        /// <summary>원작 `Mounts.migrateInventory` 자리(T11 이 붙인다).</summary>
        public static Action<JsonObject> MountsMigrate;
        /// <summary>상태가 준비되면 한 번(이미 준비된 뒤 구독하면 즉시 안 부른다 — <see cref="Ready"/> 를 먼저 보라).</summary>
        public static event Action OnReady;

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

        public static SaveIo Create(Transform parent)
        {
            if (Instance != null) return Instance;
            var go = new GameObject("SaveIo");
            go.transform.SetParent(parent, false);
            return go.AddComponent<SaveIo>();
        }

        /// <summary>JS `Date.now()`.</summary>
        public static double NowMs() { return AbsTimer.NowMs(DateTime.UtcNow); }

        /// <summary>세이브 파일 경로 — 원작 localStorage 키 `forgeclone_save_v1` 를 파일 이름으로.</summary>
        public static string SavePath { get { return Path.Combine(Application.persistentDataPath, (Defs != null ? Defs.SaveKey : "forgeclone_save") + ".json"); } }

        private void Awake()
        {
            Instance = this;
            StartCoroutine(Boot());
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private IEnumerator Boot()
        {
            string[] names = new string[GameData.Files.Length + 1];
            Array.Copy(GameData.Files, names, GameData.Files.Length);
            names[names.Length - 1] = SaveDefs.FileName;
            var texts = new string[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                var read = ReadStreaming(names[i], t => texts[i] = t);
                while (read.MoveNext()) yield return read.Current;
                if (texts[i] == null) { Debug.LogError("[SaveIo] StreamingAssets/data/" + names[i] + " 를 못 읽었다 — 세이브를 세우지 않는다"); yield break; }
            }
            var t7 = texts;
            Data = GameData.Load(t7[0], t7[1], t7[2], t7[3], t7[4], t7[5], t7[6]);
            Defs = SaveDefs.Parse(t7[7]);
            Load();
            Ready = true;
            InvokeRepeating("Save", AutosaveIntervalSec, AutosaveIntervalSec);
            var h = OnReady;
            if (h != null) h();
        }

        /// <summary>에디터·PC 는 파일로, Android(apk 안)·WebGL(URL)은 UnityWebRequest 로 — `streamingAssetsPath` 규약.</summary>
        private static IEnumerator ReadStreaming(string name, Action<string> done)
        {
            string p = Path.Combine(Application.streamingAssetsPath, "data", name);
            if (File.Exists(p))
            {
                done(File.ReadAllText(p));
                yield break;
            }
            using (var req = UnityWebRequest.Get(p))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) done(req.downloadHandler.text);
                else { Debug.LogError("[SaveIo] " + p + ": " + req.error); done(null); }
            }
        }

        /// <summary>원작 `loadGame()` — 파일이 없거나 깨졌으면 새 게임. 손상 보정 줄은 경고로 남긴다(원작은 console.error · §1 «플레이 콘솔 빨강 0» 게이트라 경고로).</summary>
        public void Load()
        {
            if (Defs == null || Data == null) { Debug.LogWarning("[SaveIo] 표가 아직 안 읽혔다 — Ready 뒤에 부를 것"); return; }
            string raw = null;
            try { if (File.Exists(SavePath)) raw = File.ReadAllText(SavePath); } catch (Exception e) { Debug.LogWarning("[SaveIo] 세이브 읽기 실패 — 새 게임: " + e.Message); }
            var r = SaveCodec.Load(raw, Defs, Data.Defs, NowMs(), MountsMigrate);
            foreach (string w in r.Warnings) Debug.LogWarning(w);
            State = r.State;
            LoadedFromDisk = r.Loaded;
        }

        /// <summary>원작 `saveGame()` — lastSeen 갱신 뒤 통짜 JSON. 임시 파일에 쓰고 바꿔치기(쓰다 죽어도 이전 세이브가 남게) · 실패는 무시(원작과 같다).</summary>
        public void Save()
        {
            if (State == null) return;
            try
            {
                string text = SaveCodec.Serialize(State, NowMs());
                string path = SavePath;
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, text);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception) { /* 저장 실패 무시 */ }
        }

        /// <summary>원작 `resetGame()` — 세이브 삭제 + 새로고침(현재 씬을 다시 연다).</summary>
        public void ResetGame()
        {
            try { if (File.Exists(SavePath)) File.Delete(SavePath); } catch (Exception) { /* 저장소 접근 실패 무시 */ }
            State = null;
            CancelInvoke("Save");
            Scene active = SceneManager.GetActiveScene();
            if (active.buildIndex >= 0) SceneManager.LoadScene(active.buildIndex); else SceneManager.LoadScene(active.name);
        }

        /// <summary>원작 `pendingOffline()` — 미수집 누적분 미리보기(상태 불변 · 1초 미만이면 null).</summary>
        public OfflineReward PendingOffline() { return State == null ? null : Offline.Pending(Defs, State, NowMs(), Mults); }

        /// <summary>원작 `claimOfflineNow()` — [수집]. 지급 뒤 바로 저장한다.</summary>
        public OfflineReward ClaimOffline()
        {
            if (State == null) return null;
            var r = Offline.ClaimNow(Defs, State, NowMs(), Mults);
            if (r != null) Save();
            return r;
        }

        // 원작: document.visibilitychange(hidden) → saveGame · window.beforeunload → saveGame
        private void OnApplicationPause(bool paused) { if (paused) Save(); }
        private void OnApplicationFocus(bool focus) { if (!focus) Save(); }
        private void OnApplicationQuit() { Save(); }
    }
}
