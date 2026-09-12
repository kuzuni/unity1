using UnityEngine;
using Forge.Core.Audio;

namespace Forge.Game.Audio
{
    /// <summary>
    /// 원작 `SFX` 의 효과음 표면 — 이름·인자 규약 그대로(`SFX.hit(crit)` → <see cref="Hit"/>). 켜짐은 원작 `get on()`(= `!S || S.sfxOn !== false`)처럼
    /// 세이브의 `sfxOn` 을 매 호출 읽는다. 클립은 <see cref="AudioBank"/> 가 미리 구운 것을 <c>PlayOneShot</c> 으로 겹쳐 낸다(원작도 노드를 겹친다).
    /// 호출 지점(전투 타격·대장간 망치·소환·장비·보스 사이렌)은 그 화면·전투 작업(T8·T19·T20·T21)이 붙인다.
    /// </summary>
    public sealed class Sfx : MonoBehaviour
    {
        public static Sfx Instance { get; private set; }
        /// <summary>마지막으로 재생한 클립(스모크 테스트용).</summary>
        public static AudioClip LastPlayed { get; private set; }
        /// <summary>재생에 성공한 횟수(스모크 테스트용).</summary>
        public static int PlayedCount { get; private set; }

        AudioSource _src;

        /// <summary>원작 `SFX.on` — 세이브가 없으면 켜짐 · `sfxOn !== false`.</summary>
        public static bool On
        {
            get
            {
                var st = SaveIo.State;
                if (st == null || !st.Has("sfxOn")) return true;
                return st.SfxOn;
            }
        }

        private void Awake()
        {
            Instance = this;
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.loop = false;
            _src.spatialBlend = 0f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>원작 `resume()` — 브라우저 자동재생 정책용. 유니티는 할 일이 없다(WebGL 은 엔진이 첫 입력에서 오디오를 연다).</summary>
        public static void Resume() { }

        /// <summary>이름·인자로 낸다(원작 `SFX[name](...)`). 꺼져 있거나 은행이 아직이면 조용히 건너뛴다(원작도 ctx 가 없으면 무음).</summary>
        public static bool Play(SfxCall call)
        {
            if (!On) return false;
            Sfx me = Instance;
            AudioBank bank = AudioBank.Instance;
            if (me == null || bank == null) return false;
            AudioClip clip = bank.Sfx(call);
            if (clip == null) return false;
            me._src.PlayOneShot(clip);
            LastPlayed = clip;
            PlayedCount++;
            return true;
        }

        static int Tier(int tier) { return Mathf.Clamp(tier, 0, 5); }

        public static void Hit(bool crit) { Play(new SfxCall("hit", crit ? 1 : 0)); }
        public static void BossSiren() { Play(new SfxCall("bossSiren")); }
        public static void AnvilHit(bool strong) { Play(new SfxCall("anvilHit", strong ? 1 : 0)); }
        /// <summary>dur 0 = 원작 기본(0.35).</summary>
        public static void StormRumble(double dur = 0) { Play(new SfxCall("stormRumble", dur)); }
        public static void StormCrackle() { Play(new SfxCall("stormCrackle")); }
        public static void StormStrike(int i) { Play(new SfxCall("stormStrike", Mathf.Clamp(i, 0, 4))); }
        public static void SlashArc(int i, int tier) { Play(new SfxCall("slashArc", Mathf.Clamp(i, 0, 4), Tier(tier))); }
        public static void ArrowShot(int i, int tier) { Play(new SfxCall("arrowShot", i, Tier(tier))); }
        public static void MawRoar(int tier) { Play(new SfxCall("mawRoar", Tier(tier))); }
        public static void MawBite(int tier) { Play(new SfxCall("mawBite", Tier(tier))); }
        public static void HealDescend(int tier) { Play(new SfxCall("healDescend", Tier(tier))); }
        public static void AuraRise(int tier) { Play(new SfxCall("auraRise", Tier(tier))); }
        public static void VoidTear(int tier) { Play(new SfxCall("voidTear", Tier(tier))); }
        public static void VoidPierce(int tier) { Play(new SfxCall("voidPierce", Tier(tier))); }
        public static void VoidSnap() { Play(new SfxCall("voidSnap")); }
        public static void EquipToss() { Play(new SfxCall("equipToss")); }
        public static void EquipSnap() { Play(new SfxCall("equipSnap")); }
        public static void EquipDrop() { Play(new SfxCall("equipDrop")); }
        public static void Craft() { Play(new SfxCall("craft")); }
        public static void CraftReveal(int ageIdx) { Play(new SfxCall("craftReveal", Mathf.Max(0, ageIdx))); }
        public static void LevelUp() { Play(new SfxCall("levelUp")); }
        public static void Gacha(string rarity) { Play(new SfxCall("gacha", 0, 0, rarity)); }
        public static void SummonCharge(string rarity) { Play(new SfxCall("summonCharge", 0, 0, rarity)); }
        public static void SummonReveal(string rarity) { Play(new SfxCall("summonReveal", 0, 0, rarity)); }
    }
}
