using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Forge.Core;
using Forge.Core.Ascend;
using Forge.Core.Data;
using Forge.Core.Forging;
using Forge.Core.Mounts;
using Forge.Core.PetSave;
using Forge.Core.Pets;
using Forge.Core.Save;
using Forge.Core.Skills;
using Forge.Core.Tech;
using Forge.Game.Audio;
using Forge.Game.Mounts;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T20 접착층 — 세이브(T13 <see cref="SaveIo"/>) ↔ Core 펫(T16)·스킬(T17) ↔ 화면. 원작에서 `Pets`·`Skills` 가 전역 `S` 를 직접 고치던 것을
    /// «세이브 트리 읽기 → 규칙 → <see cref="Sync"/> 로 되쓰기» 로 옮겼다(코덱 <see cref="PetSkillSave"/>). 원작이 밖에서 받던 값(지갑 · 대장간 축 ·
    /// 승천 · 기술트리 배율)은 이 클래스가 <see cref="IPetHost"/>·<see cref="ISkillHost"/> 로 준다 — 대장간 축은 T14 <see cref="ForgeRules"/> ·
    /// 승천은 T24 <see cref="Ascension"/>(tech.json) · 기술트리는 T24 <see cref="TechTree"/>(세이브 `tech` 칸 읽기).
    /// 부화 완료(원작 `Pets.tick` · 메인 루프)는 1초마다 여기서 돈다. 효과음은 T30 <see cref="Sfx"/> 를 훅(<see cref="SfxGacha"/> 등)에 꽂는다.
    /// </summary>
    [DefaultExecutionOrder(-800)]
    public sealed class PetSkillHost : MonoBehaviour, IPetHost, ISkillHost, IMountHost
    {
        public static PetSkillHost Instance { get; private set; }
        /// <summary>지금 살아 있는 호스트가 부팅을 마쳤는가 — 인스턴스에서 파생한다(정적 플래그면 씬을 다시 여는 PlayMode 테스트에서 앞 씬 호스트의 코루틴이 새 씬 위에서 참으로 올린다 · T19 결정 112 와 같은 경쟁).</summary>
        public static bool Ready { get { return Instance != null && Instance.booted; } }
        bool booted;
        public static event Action OnReady;

        /// <summary>효과음 훅(T30 이 채운다): gacha(최고 등급) · summonCharge(최고 등급) · summonReveal(등급).</summary>
        public static Action<string> SfxGacha, SfxSummonCharge, SfxSummonReveal;
        /// <summary>토스트 출구(원작 UI.toast) — <see cref="PetSkillModal"/> 이 꽂는다.</summary>
        public static Action<string> Toast;
        /// <summary>테스트용 시드(0 이면 시각).</summary>
        public static uint Seed;

        public GameData Data { get; private set; }
        public PetSystem Pets { get; private set; }
        public SkillSystem Skills { get; private set; }
        /// <summary>T40 Core 탈것(원작 `Mounts`) — 탈것 화면(<see cref="MountSheet"/>)이 쓴다.</summary>
        public MountSystem Mounts { get; private set; }
        public TechTree Tech { get; private set; }
        public Ascension Ascension { get; private set; }
        public AscensionState AscState { get; private set; }
        public Rng Rng { get; private set; }

        /// <summary>상태가 바뀌었다(패널 재렌더).</summary>
        public event Action Changed;
        /// <summary>원작 `Combat.recalcHero()` — 보유 패시브·출전 펫이 바뀌었다(T8 전투 씬이 듣는다).</summary>
        public event Action RecalcHero;

        int lastRecalc;
        float tickAt;
        const float TickSec = 1f;
        /// <summary>T24 추출기가 내는 기술·승천 표(9번째 JSON · `GameData.Files` 밖).</summary>
        public const string TechFile = "tech.json";

        static SaveState S { get { return SaveIo.State; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Instance != null && Instance.gameObject.scene == scene) return;
            foreach (Bootstrap b in Resources.FindObjectsOfTypeAll<Bootstrap>())
            {
                if (b.gameObject.scene != scene) continue;   // 방금 열린 씬의 Bootstrap 아래에만 — 내려가는 앞 씬 것에 붙으면 같이 지워진다
                Create(b.transform);
                return;
            }
        }

        public static PetSkillHost Create(Transform parent)
        {
            if (Instance != null && Instance.gameObject.scene == parent.gameObject.scene) return Instance;
            var go = new GameObject("PetSkillHost");
            go.transform.SetParent(parent, false);
            return go.AddComponent<PetSkillHost>();
        }

        void Awake()
        {
            Instance = this;
            StartCoroutine(Boot());
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>이 호스트가 아직 제 씬의 현역인가 — 앞 씬 호스트의 코루틴이 새 씬 위에서 이어 돌며 정적 상태를 만지지 않게 매 대기 뒤 본다.</summary>
        bool Alive { get { return this != null && Instance == this && gameObject.scene.isLoaded; } }

        IEnumerator Boot()
        {
            // 한 프레임 뒤에 시작한다 — 같은 sceneLoaded 에서 서는 SaveIo·UiRoot 가 먼저 제 자리를 잡게(앞 씬 것의 정적 Ready 를 보지 않으려고).
            yield return null;
            while (Alive && (SaveIo.Instance == null || SaveIo.Instance.gameObject.scene != gameObject.scene || !SaveIo.Ready || SaveIo.State == null)) yield return null;
            if (!Alive) yield break;
            string techJson = null;
            var read = ReadStreaming(TechFile, t => techJson = t);
            while (read.MoveNext()) yield return read.Current;
            if (!Alive) yield break;
            if (techJson == null) { Debug.LogError("[PetSkillHost] StreamingAssets/data/" + TechFile + " 를 못 읽었다 — 펫·스킬 화면을 세우지 않는다"); yield break; }
            Data = SaveIo.Data;
            TechData td = TechData.Load(techJson);
            Tech = new TechTree(td.Tech, Data.Defs, PetSkillSave.ReadTech(S.Root));
            Ascension = new Ascension(td.Ascension, Data.Balance.Skills.MaxLevel, Data.Balance.Mounts.MaxLevel);
            AscState = PetSkillSave.ReadAscension(S.Root);
            Rng = Rng.Mulberry(Seed != 0 ? Seed : (uint)(Environment.TickCount ^ (int)(SaveIo.NowMs() % int.MaxValue)));
            Pets = new PetSystem(Data, PetRules.Original(), this, Rng, PetSkillSave.ReadPets(S.Root));
            Skills = new SkillSystem(Data, SkillRules.Original(), this, Rng, PetSkillSave.ReadSkills(S.Root));
            MountRules mr = MountRules.Original();
            Mounts = new MountSystem(Data, mr, this, Rng, MountSave.ReadMounts(S.Root, Data.Defs, Rng, mr));
            Mounts.Ensure();
            lastRecalc = Skills.RecalcRequests;
            // 효과음(T30 Sfx · 원작 SFX.gacha/summonCharge/summonReveal) — 다른 것이 먼저 꽂았으면 그대로 둔다
            if (SfxGacha == null) SfxGacha = Sfx.Gacha;
            if (SfxSummonCharge == null) SfxSummonCharge = Sfx.SummonCharge;
            if (SfxSummonReveal == null) SfxSummonReveal = Sfx.SummonReveal;
            booted = true;
            while (Alive && (UiRoot.Instance == null || UiRoot.Instance.gameObject.scene != gameObject.scene || UiRoot.Instance.TabBar == null)) yield return null;
            if (!Alive) yield break;
            SkillPetSheet.Attach(UiRoot.Instance, this);
            SkillBar.Attach(UiRoot.Instance, this);
            var h = OnReady;
            if (h != null) h();
        }

        static IEnumerator ReadStreaming(string name, Action<string> done)
        {
            string p = Path.Combine(Application.streamingAssetsPath, "data", name);
            if (File.Exists(p)) { done(File.ReadAllText(p)); yield break; }
            using (var req = UnityWebRequest.Get(p))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) done(req.downloadHandler.text);
                else { Debug.LogError("[PetSkillHost] " + p + ": " + req.error); done(null); }
            }
        }

        void Update()
        {
            if (!booted || Instance != this) return;
            tickAt += Time.unscaledDeltaTime;
            if (tickAt < TickSec) return;
            tickAt = 0f;
            var results = Pets.Tick();
            if (results.Count == 0) return;
            Announce(results);
        }

        /// <summary>부화 결과를 알리고(원작 `Pets.tick` 토스트 · `Combat.recalcHero`) 되쓴다 — 틱과 젬 스킵이 같이 쓴다.</summary>
        public void Announce(System.Collections.Generic.List<HatchResult> results)
        {
            if (results == null || results.Count == 0) return;
            foreach (HatchResult r in results) ToastHatch(r);
            RequestRecalc();
            Sync();
        }

        /// <summary>테스트가 켠다 — 디스크에 쓰지 않는다(러너의 persistentDataPath 는 주인 에디터의 세이브다).</summary>
        public static bool SuppressSave;

        void ToastHatch(HatchResult r)
        {
            string kr = Data.Defs.PetKr.Get(r.Name, r.Name);
            string rk = Data.Defs.RarityKr.Get(r.Rarity, r.Rarity);
            if (r.ReturnedAsEgg) Say(PetSkillStyle.T("toast_pet_inv_full_back", Pets.State.Pets.Count, Pets.Rules.InvCap));
            else if (r.Dropped) Say(PetSkillStyle.T("toast_pet_inv_full", Pets.State.Pets.Count, Pets.Rules.InvCap));
            else if (r.Already) Say(PetSkillStyle.T("toast_pet_again", kr, rk));
            else Say(PetSkillStyle.T("toast_pet_new", kr, rk));
        }

        public static void Say(string msg)
        {
            var t = Toast;
            if (t != null) t(msg);
        }

        /// <summary>규칙이 상태를 바꾼 뒤: 세이브 트리로 되쓰고(원작은 그 자리 수정) · 재계산 신호 · 화면 갱신.</summary>
        public void Sync()
        {
            PetSkillSave.WritePets(S.Root, Pets.State);
            PetSkillSave.WriteSkills(S.Root, Skills.State);
            if (Mounts != null) MountSave.WriteMounts(S.Root, Mounts.State);
            if (Skills.RecalcRequests != lastRecalc) { lastRecalc = Skills.RecalcRequests; RequestRecalc(); }
            RefreshTopBar();
            var h = Changed;
            if (h != null) h();
        }

        /// <summary>탈것이 바뀌었다(소환·장착·타기·흡수) — 세이브 되쓰기 + T11 <see cref="MountRider"/> 가 3D 를 다시 세운다(원작 `Scene3D.refreshMount`) + 재계산.</summary>
        public void MountsChanged()
        {
            RequestRecalc();
            Sync();
            if (MountRider.Instance != null && MountRider.Instance.Ready) MountRider.Instance.RefreshFromSave();
        }

        /// <summary>원작 `Combat.recalcHero()` 자리 — 출전 펫 토글도 이것을 부른다.</summary>
        public void RequestRecalc()
        {
            var h = RecalcHero;
            if (h != null) h();
        }

        /// <summary>원작 `renderTopBar` — 젬을 썼으면 상단 알약이 바로 따라온다(코인은 그대로 다시 적는다).</summary>
        void RefreshTopBar()
        {
            if (Hud.Instance == null) return;
            Hud.Instance.SetCurrency(NumFmt.Fmt(Big.Of(S.Coins)), NumFmt.Fmt(Big.Of(S.Gems)));
        }

        public void Save() { if (!SuppressSave && SaveIo.Instance != null) SaveIo.Instance.Save(); }

        // ===== 소환 배수 =====
        public int SummonMult(string kind) { return PetSkillSave.SummonMult(S.Root, kind); }
        public int CycleSummonMult(string kind) { int v = PetSkillSave.CycleSummonMult(S.Root, kind); Sync(); return v; }

        // ===== 승천 =====
        public AscensionLevels Levels()
        {
            return new AscensionLevels { ForgeLevel = S.ForgeLevel, SkillSummonLevel = Skills.SummonLevel(), PetSummonLevel = Pets.SummonLevel(), MountLevel = Mounts != null ? Mounts.Level() : 1 };
        }
        public bool AscendReady(string line) { return Ascension.Ready(line, Levels()); }
        public int AscendCount(string line) { return Ascension.Count(AscState, line); }

        // ===== IPetHost =====
        public double Now() { return SaveIo.NowMs(); }
        public double Gems { get { return S.Gems; } set { S.Gems = value; } }
        public double EggCurrency { get { return S.EggCurrency; } set { S.EggCurrency = value; } }
        public double LevelMult(int level) { return ForgeRules.LevelMult(level); }
        public double GearSumAtkAt(string rarity) { return ForgeRules.GearSumAtkAt(Data.Defs, rarity); }
        public double GearSumHpAt(string rarity) { return ForgeRules.GearSumHpAt(Data.Defs, rarity); }
        public Big StarMult(int stars) { return Ascension.StarMult(stars); }
        public int PetAscendCount() { return Ascension.Count(AscState, "pet"); }
        public double PetDmgMult() { return Tech.PetDmgMult(); }
        public double PetHpMult() { return Tech.PetHpMult(); }
        public double HatchSpeedMult() { return Tech.HatchSpeedMult(); }
        public double ExtraEggChance() { return Tech.ExtraEggChance(); }

        // ===== ISkillHost =====
        public double Tickets { get { return S.Tickets; } set { S.Tickets = value; } }
        public double SkillSummonCostMult() { return Tech.SkillSummonCostMult(); }
        public double SkillPassiveDmgMult() { return Tech.SkillPassiveDmgMult(); }
        public double SkillPassiveHpMult() { return Tech.SkillPassiveHpMult(); }
        public int SkillAscendCount() { return Ascension.Count(AscState, "skill"); }

        // ===== IMountHost =====
        public double Winders { get { return S.Winders; } set { S.Winders = value; } }
        public int MountAscendCount() { return Ascension.Count(AscState, "mount"); }
        public double MountDmgMult() { return Tech.MountDmgMult(); }
        public double MountHpMult() { return Tech.MountHpMult(); }
        public double MountCostMult() { return Tech.MountCostMult(); }
        public double ExtraMountChance() { return Tech.ExtraMountChance(); }
    }
}
