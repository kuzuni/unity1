using System;
using UnityEngine;
using Forge.Core.Audio;

namespace Forge.Game.Audio
{
    /// <summary>
    /// 원작 배경 음악 표면 — `startMusic` · `stopMusic` · `toggleMusic` · `setMusicMode` · `musicEnabled` · `musicMode`. 원작은 look-ahead 스케줄러로 스텝을 실시간 예약하고
    /// **코드 경계(2마디)에서만** 모드를 갈아탄다(상점 오버레이 > Combat 지정 모드). 여기서는 모드마다 미리 구운 8마디 루프 클립을 돌리고, 코드 경계를 지날 때 같은
    /// 코드 번호 위치로 클립을 바꾼다 — 곡 중간에 뚝 끊기지 않는 규약은 같다(결정 기록).
    /// </summary>
    public sealed class Music : MonoBehaviour
    {
        public static Music Instance { get; private set; }

        /// <summary>상점 모달이 열려 있나 — 원작 `_shopOpen()`(UI 가 코드 경계마다 읽힌다). UI(T22)가 꽂는다.</summary>
        public static Func<bool> ShopOpen;

        /// <summary>실제 재생 중인 모드(원작 `musicMode`).</summary>
        public string MusicMode { get; private set; }
        /// <summary>Combat 이 지정한 기본 모드(원작 `_combatMode`) — shop 오버레이가 벗겨지면 여기로 복귀.</summary>
        public string CombatMode { get; private set; }
        /// <summary>원작 `musicTimer` 가 살아 있는가.</summary>
        public bool Running { get; private set; }
        public bool IsPlaying { get { return Running && _src != null && _src.isPlaying; } }
        public int ChordIndex { get { return _lastChord; } }

        AudioSource _src;
        int _lastChord = -1;
        bool _pendingStart;

        /// <summary>원작 `get musicEnabled()` = `!S || S.musicOn !== false`.</summary>
        public static bool MusicEnabled
        {
            get
            {
                var st = SaveIo.State;
                if (st == null || !st.Has("musicOn")) return true;
                return st.MusicOn;
            }
        }

        private void Awake()
        {
            Instance = this;
            MusicMode = "normal";
            CombatMode = "normal";
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.loop = true;
            _src.spatialBlend = 0f;
            AudioBank.OnMusicReady += OnClipReady;
        }

        private void OnDestroy()
        {
            AudioBank.OnMusicReady -= OnClipReady;
            if (Instance == this) Instance = null;
        }

        void OnClipReady(string mode)
        {
            if (_pendingStart && mode == "normal") { _pendingStart = false; StartMusic(); }
        }

        /// <summary>Combat(보스/던전 진입·이탈)이 호출 — 다음 코드 경계에서 자연스럽게 갈아탄다.</summary>
        public void SetMusicMode(string mode)
        {
            SfxTable t = AudioBank.Table;
            CombatMode = t != null && t.HasMode(mode) ? mode : "normal";
        }

        /// <summary>원작 `startMusic()` — 이미 돌고 있거나 꺼져 있으면 아무것도 안 한다. 클립이 아직이면 구워지는 대로 시작한다.</summary>
        public void StartMusic()
        {
            if (Running || !MusicEnabled) return;
            AudioBank bank = AudioBank.Instance;
            AudioClip clip = bank != null ? bank.MusicClip("normal") : null;
            if (clip == null) { _pendingStart = true; return; }
            MusicMode = "normal";
            _src.clip = clip;
            _src.timeSamples = 0;
            _src.Play();
            _lastChord = 0;
            Running = true;
        }

        /// <summary>원작 `stopMusic()`.</summary>
        public void StopMusic()
        {
            _pendingStart = false;
            if (!Running) return;
            _src.Stop();
            Running = false;
        }

        /// <summary>원작 `toggleMusic()` — `S.musicOn` 을 뒤집고 저장. 반환 = 새 값.</summary>
        public bool ToggleMusic()
        {
            var st = SaveIo.State;
            bool on = !MusicEnabled;
            if (st != null) st.MusicOn = on;
            if (on) StartMusic(); else StopMusic();
            if (SaveIo.Instance != null && st != null) SaveIo.Instance.Save();
            return on;
        }

        private void Update()
        {
            if (!Running) return;
            if (!MusicEnabled) { StopMusic(); return; }
            AudioClip clip = _src.clip;
            if (clip == null) return;
            int chord = ChordAt(clip, _src.timeSamples);
            if (chord != _lastChord)
            {
                _lastChord = chord;
                SwitchAtBoundary(chord);
            }
        }

        static int ChordsPerLoop { get { SfxTable t = AudioBank.Table; return t != null ? t.LoopSteps / t.ChordSteps : 4; } }

        static int ChordAt(AudioClip clip, int timeSamples)
        {
            int per = Mathf.Max(1, clip.samples / ChordsPerLoop);
            return Mathf.Clamp(timeSamples / per, 0, ChordsPerLoop - 1);
        }

        /// <summary>코드 경계 — 모드 결정은 여기서만: 상점 오버레이 > Combat 지정 모드. 목표 모드 클립이 아직 안 구워졌으면 다음 경계에 다시 본다.</summary>
        void SwitchAtBoundary(int chord)
        {
            string target = (ShopOpen != null && SafeShopOpen()) ? "shop" : CombatMode;
            if (target == MusicMode) return;
            AudioBank bank = AudioBank.Instance;
            AudioClip next = bank != null ? bank.MusicClip(target) : null;
            if (next == null) return;
            MusicMode = target;
            _src.clip = next;
            _src.timeSamples = Mathf.Clamp(chord * (next.samples / ChordsPerLoop), 0, next.samples - 1);
            _src.Play();
        }

        static bool SafeShopOpen()
        {
            try { return ShopOpen(); } catch (Exception) { return false; }
        }

        /// <summary>지금 위치의 코드 경계 규칙을 즉시 적용한다(테스트·디버그용 — 게임은 Update 가 경계에서 부른다).</summary>
        public void SwitchNow()
        {
            if (!Running || _src.clip == null) return;
            SwitchAtBoundary(ChordAt(_src.clip, _src.timeSamples));
        }
    }
}
