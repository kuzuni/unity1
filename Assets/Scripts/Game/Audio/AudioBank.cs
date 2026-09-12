using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Forge.Core.Audio;
using Forge.Core.Data;

namespace Forge.Game.Audio
{
    /// <summary>
    /// 소리 은행 — 원작 `sfx.js` 가 «외부 파일 금지 · 전부 코드 합성» 이라 유니티도 오디오 파일을 들이지 않는다(T30). Core 렌더러(순수 C#)를 **백그라운드 스레드**에서 돌려
    /// 효과음 기본 변형(<see cref="SfxRecipes.DefaultCalls"/> × 테이크 2)과 음악 4모드 루프를 샘플로 굽고, 메인 스레드가 <c>AudioClip.Create</c> 로 클립화해 캐시한다.
    /// 부팅 씬의 <see cref="Bootstrap"/> 아래에 자립한다(SaveIo 와 같은 규약). 수치는 `StreamingAssets/data/sfx.json`(음악 표) · `gamedata.json`(RARITIES)에서 읽는다.
    /// </summary>
    public sealed class AudioBank : MonoBehaviour
    {
        /// <summary>호출마다 피치·노이즈가 달라지던 원작 청감을 흉내내는 테이크 수(같은 키를 시드만 바꿔 굽는다 · 결정 기록).</summary>
        public const int Takes = 2;

        public static AudioBank Instance { get; private set; }
        public static SfxTable Table { get; private set; }
        public static string[] Rarities { get; private set; }
        /// <summary>표를 읽었는가(렌더 시작 가능).</summary>
        public static bool Loaded { get; private set; }
        /// <summary>효과음 기본 변형 테이크 0 이 전부 클립이 됐는가.</summary>
        public static bool SfxReady { get; private set; }
        /// <summary>렌더 큐가 다 비었는가(음악 4모드 · 테이크 2 포함).</summary>
        public static bool AllReady { get; private set; }
        public static event Action OnSfxReady;
        public static event Action<string> OnMusicReady;

        sealed class Job
        {
            public string Name;
            public SfxCall Call;
            public int Take;
            public string Mode;
            public RenderedClip Clip;
        }

        readonly Dictionary<string, AudioClip[]> _sfx = new Dictionary<string, AudioClip[]>();
        readonly Dictionary<string, AudioClip> _music = new Dictionary<string, AudioClip>();
        readonly HashSet<string> _requested = new HashSet<string>();
        readonly ConcurrentQueue<Job> _todo = new ConcurrentQueue<Job>();
        readonly ConcurrentQueue<Job> _done = new ConcurrentQueue<Job>();
        readonly AutoResetEvent _wake = new AutoResetEvent(false);
        Thread _worker;
        /// <summary>WebGL 은 스레드가 없다 — 프레임마다 한 건씩 메인 스레드에서 굽는다(결정 기록).</summary>
        bool _sync;
        volatile bool _quit;
        int _pendingDefault;
        int _queued;

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

        public static AudioBank Create(Transform parent)
        {
            if (Instance != null) return Instance;
            var go = new GameObject("AudioBank");
            go.transform.SetParent(parent, false);
            var bank = go.AddComponent<AudioBank>();
            go.AddComponent<Sfx>();
            go.AddComponent<Music>();
            return bank;
        }

        private void Awake()
        {
            Instance = this;
            StartCoroutine(Boot());
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            _quit = true;
            _wake.Set();
            Instance = null;
            Loaded = SfxReady = AllReady = false;
        }

        private IEnumerator Boot()
        {
            string sfxJson = null, gameJson = null;
            var r1 = ReadStreaming(SfxTable.File, t => sfxJson = t);
            while (r1.MoveNext()) yield return r1.Current;
            var r2 = ReadStreaming(GameData.GameDataFile, t => gameJson = t);
            while (r2.MoveNext()) yield return r2.Current;
            if (sfxJson == null || gameJson == null) { Debug.LogError("[AudioBank] StreamingAssets/data 의 sfx.json/gamedata.json 을 못 읽었다 — 소리를 세우지 않는다"); yield break; }
            Table = SfxTable.Parse(sfxJson);
            Rarities = J.StrArr(J.Require(MiniJson.ParseObject(gameJson), "RARITIES"));
            Loaded = true;

            List<SfxCall> defaults = SfxRecipes.DefaultCalls(Rarities);
            _pendingDefault = defaults.Count;
            foreach (SfxCall c in defaults) Enqueue(new Job { Name = "sfx:" + c.Key + "#0", Call = c, Take = 0 });
            Enqueue(new Job { Name = "music:normal", Mode = "normal" });
            for (int take = 1; take < Takes; take++) foreach (SfxCall c in defaults) Enqueue(new Job { Name = "sfx:" + c.Key + "#" + take, Call = c, Take = take });
            foreach (string mode in Table.Modes.Keys) if (mode != "normal") Enqueue(new Job { Name = "music:" + mode, Mode = mode });

            _sync = Application.platform == RuntimePlatform.WebGLPlayer;
            if (_sync) yield break;
            _worker = new Thread(Work) { IsBackground = true, Name = "AudioBank" };
            _worker.Start();
        }

        void Enqueue(Job j)
        {
            _queued++;
            _todo.Enqueue(j);
            _wake.Set();
        }

        /// <summary>워커 — 유니티 API 를 부르지 않는다(순수 Core 렌더).</summary>
        void Work()
        {
            while (!_quit)
            {
                Job j;
                if (!_todo.TryDequeue(out j)) { _wake.WaitOne(250); continue; }
                RenderJob(j);
                _done.Enqueue(j);
            }
        }

        static void RenderJob(Job j)
        {
            try
            {
                if (j.Mode != null) j.Clip = AudioFactory.RenderMusic(Table, j.Mode, AudioFactory.Seed("music:" + j.Mode, 0));
                else j.Clip = AudioFactory.RenderSfx(j.Call, Rarities, AudioFactory.Seed(j.Call.Key, j.Take));
            }
            catch (Exception e)
            {
                j.Clip = null;
                j.Name = j.Name + " !" + e.GetType().Name + ": " + e.Message;
            }
        }

        private void Update()
        {
            Job j;
            if (_sync && _todo.TryDequeue(out j)) { RenderJob(j); _done.Enqueue(j); }
            int budget = 8;
            while (budget-- > 0 && _done.TryDequeue(out j))
            {
                _queued--;
                if (j.Clip == null) { Debug.LogError("[AudioBank] 렌더 실패 " + j.Name); continue; }
                AudioClip clip = AudioClip.Create(j.Name, j.Clip.Samples.Length, 1, j.Clip.SampleRate, false);
                clip.SetData(j.Clip.Samples, 0);
                if (j.Mode != null)
                {
                    _music[j.Mode] = clip;
                    var h = OnMusicReady;
                    if (h != null) h(j.Mode);
                }
                else
                {
                    AudioClip[] arr;
                    if (!_sfx.TryGetValue(j.Call.Key, out arr)) { arr = new AudioClip[Takes]; _sfx[j.Call.Key] = arr; }
                    arr[j.Take] = clip;
                    if (j.Take == 0 && _pendingDefault > 0 && --_pendingDefault == 0)
                    {
                        SfxReady = true;
                        var h = OnSfxReady;
                        if (h != null) h();
                    }
                }
                if (_queued == 0) AllReady = true;
            }
        }

        /// <summary>
        /// 구워진 효과음 클립(테이크 중 하나). 정확한 변형이 아직이면 백그라운드에 요청해 두고 **같은 이름의 구워진 변형**(tier 0 → 아무 것)으로 대신 낸다 —
        /// tier 는 게인 +0.008/단이라 귀로 거의 같고, 첫 타격이 무음이 되는 것보다 낫다. 이름 자체가 하나도 없으면 null.
        /// </summary>
        public AudioClip Sfx(SfxCall call)
        {
            AudioClip exact = Pick(call.Key);
            if (exact != null) return exact;
            if (Loaded && _requested.Add(call.Key))
            {
                AllReady = false;
                for (int take = 0; take < Takes; take++) Enqueue(new Job { Name = "sfx:" + call.Key + "#" + take, Call = call, Take = take });
            }
            AudioClip near = Pick(new SfxCall(call.Name, call.A, 0, call.Rarity).Key);
            if (near != null) return near;
            string prefix = call.Name + ":";
            foreach (var kv in _sfx)
            {
                if (!kv.Key.StartsWith(prefix, StringComparison.Ordinal)) continue;
                AudioClip any = Pick(kv.Key);
                if (any != null) return any;
            }
            return null;
        }

        AudioClip Pick(string key)
        {
            AudioClip[] arr;
            if (!_sfx.TryGetValue(key, out arr)) return null;
            int have = 0;
            for (int i = 0; i < arr.Length; i++) if (arr[i] != null) have++;
            if (have == 0) return null;
            int pick = UnityEngine.Random.Range(0, have);
            for (int i = 0; i < arr.Length; i++) if (arr[i] != null && pick-- == 0) return arr[i];
            return null;
        }

        /// <summary>그 키의 클립이 하나라도 구워졌는가.</summary>
        public bool HasSfx(string key) { return Pick(key) != null; }

        public AudioClip MusicClip(string mode)
        {
            AudioClip c;
            return _music.TryGetValue(mode, out c) ? c : null;
        }

        public bool HasMusic(string mode) { return _music.ContainsKey(mode); }
        public int SfxClipCount { get { int n = 0; foreach (var kv in _sfx) foreach (AudioClip c in kv.Value) if (c != null) n++; return n; } }

        /// <summary>에디터·PC 는 파일로, Android(apk 안)·WebGL(URL)은 UnityWebRequest 로 — `streamingAssetsPath` 규약(SaveIo 와 같다).</summary>
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
                else { Debug.LogError("[AudioBank] " + p + ": " + req.error); done(null); }
            }
        }
    }
}
