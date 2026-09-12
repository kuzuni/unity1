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
using Forge.Core.Forging;
using Forge.Core.Gear;
using Forge.Core.Save;
using Forge.Core.Tech;
using Wallet = Forge.Core.Forging.Wallet;
using Forge.Game.Hero;

namespace Forge.Game.Ui
{
    /// <summary>기술트리(T24)가 대장간에 주는 배율 — `TechTree` 가 아직 없으면(T21 전) 전부 1 · 0.</summary>
    public sealed class TechForgeMods : IForgeMods
    {
        public Func<TechTree> Tech;
        TechTree T { get { return Tech != null ? Tech() : null; } }
        public double ForgeCostMult { get { TechTree t = T; return t != null ? t.ForgeCostMult() : 1; } }
        public double ForgeTimeMult { get { TechTree t = T; return t != null ? t.ForgeTimeMult() : 1; } }
        public double FreeForgeChance { get { TechTree t = T; return t != null ? t.FreeForgeChance() : 0; } }
        public double GearMaxLevelBonus { get { TechTree t = T; return t != null ? t.GearMaxLevelBonus() : 0; } }
        public double SellPriceMult { get { TechTree t = T; return t != null ? t.SellPriceMult() : 1; } }
        public double GearAtkMult { get { TechTree t = T; return t != null ? t.GearAtkMult() : 1; } }
        public double GearHpMult { get { TechTree t = T; return t != null ? t.GearHpMult() : 1; } }
        public double AutoForgeSlotBonus { get { TechTree t = T; return t != null ? t.AutoForgeSlotBonus() : 0; } }
    }

    /// <summary>
    /// 대장간·장비 화면의 접착(ROUTINE T19 · 원작 ui.js 의 제작·대기품·자동 제련 시퀀스·업그레이드 흐름). 규칙은 전부 Core(T14 <see cref="ForgeEngine"/> · T15 <see cref="GearSystem"/>)가 계산하고
    /// 여기는 세이브 트리(T13)와 상태를 주고받으며(<see cref="ForgeSave"/>) 화면(<see cref="ForgeSheet"/>·팝업들)을 열고 닫는다. 재화 넷은 <see cref="MetaHost"/> 와 같은 세이브 칸을 쓰므로
    /// 행동마다 <see cref="Pull"/>(읽기) → 규칙 → <see cref="Push"/>(되쓰기) 로 맞춘다.
    /// 씬은 안 만진다 — <see cref="MetaHost"/> 가 준비되면 그 옆에 선다. 3D 썸네일(T37)·소리(T30)·영웅 페이퍼돌 갱신(T8)은 훅 자리만 둔다.
    /// </summary>
    public sealed class ForgeHost : MonoBehaviour
    {
        public static ForgeHost Instance { get; private set; }
        public static bool Ready { get; private set; }
        public static event Action OnReady;

        /// <summary>원작 연출 시각(ms): 모루 3타 0.72초 · 리빌 카드 0.56초 · 탈락 카드 0.62초 · 배치 카드판 1.6초.</summary>
        public const float AnvilStrikeSec = 0.72f, RevealCardSec = 0.56f, AutoCardSec = 0.62f, CraftBatchSec = 1.6f;
        /// <summary>원작 `HAMMER_BATCH_MAX` 22(UI-SPEC 82) · `FILL_CRAFT_CAP` 600(프레임 안전판).</summary>
        public const int HammerBatchMaxBase = 22, FillCraftCap = 600;

        public MetaHost Meta { get; private set; }
        public GameData Data { get; private set; }
        public GameDefs Defs { get { return Data.Defs; } }
        public SaveState S { get { return SaveIo.State; } }

        public ForgeState Forge { get; private set; }
        public Wallet Wallet { get; private set; }
        public GearState Gear { get; private set; }
        public ForgeEngine Engine { get; private set; }
        public GearSystem GearSys { get; private set; }
        public GearHost GearHostImpl { get; private set; }
        public TechForgeMods Mods { get; private set; }
        /// <summary>승천 표(tech.json) — 없으면 null(별 배율 1 · 승천 준비 false).</summary>
        public Ascension Ascension { get; private set; }
        /// <summary>T21 이 꽂는 기술트리(배율 원천). 그 전엔 null.</summary>
        public Func<TechTree> Tech;

        /// <summary>T8 이 꽂는다 — 영웅 페이퍼돌 갱신(원작 Scene3D.refreshHeroEquip). 없으면 씬의 <see cref="HeroRig"/> 를 찾아 <see cref="Paperdoll.Refresh"/>.</summary>
        public Action<bool> RefreshHeroEquip;
        /// <summary>T8 이 꽂는다 — 원작 Combat.recalcHero.</summary>
        public Action RecalcHero;
        /// <summary>T21 이 꽂는다 — 원작 UI.openAscension('forge'). 없으면 스텁.</summary>
        public Action<string> OpenAscension;
        /// <summary>T30 이 꽂는다 — anvilHit·craft·craftReveal·equipSnap·equipToss·equipDrop.</summary>
        public Action<string> Sfx;

        // ---- 대기품 · 자동 제련(원작 S.pendingCraft · autoMatchQueue · autoMatchHeld · autoBatch · autoForgeOn · UI._autoSeq) ----
        public ForgeItem Pending { get; private set; }
        public bool PendingSwapped { get; private set; }
        public List<ForgeItem> AutoQueue { get; private set; }
        public bool AutoHeld { get; private set; }
        public List<ForgeItem> AutoBatch { get; private set; }
        public bool AutoOn { get; private set; }
        public bool AutoSeqRunning { get { return autoSeq; } }
        public bool AnvilBusy { get; private set; }
        public bool Striking { get; private set; }

        bool autoSeq, stopAfterPick;
        string pendingCraftMode;
        Action heroChangedHook;
        int strikeGen;
        bool strikeLive;
        float tickAcc;

        public event Action Changed;

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

        public static ForgeHost Create(Transform parent)
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("ForgeHost");
            go.transform.SetParent(parent, false);
            return go.AddComponent<ForgeHost>();
        }

        private void Awake()
        {
            Instance = this;
            Ready = false;
            StartCoroutine(Boot());
        }

        private void OnDestroy()
        {
            if (heroChangedHook != null) DebugPanel.HeroChanged -= heroChangedHook;
            if (Instance == this) { Instance = null; Ready = false; }
        }

        private IEnumerator Boot()
        {
            while (!MetaHost.Ready || PopupLayer.Instance == null || SaveIo.Data == null || UiRoot.Instance == null) yield return null;
            Meta = MetaHost.Instance;
            Data = SaveIo.Data;

            string techJson = null;
            IEnumerator read = ReadStreaming(TechData.File, t => techJson = t);
            while (read.MoveNext()) yield return read.Current;
            if (techJson != null)
            {
                TechData td = TechData.Load(techJson);
                Ascension = new Ascension(td.Ascension, Data.Balance.Skills.MaxLevel, Data.Balance.Mounts.MaxLevel);
            }
            else Debug.LogError("[ForgeHost] StreamingAssets/data/" + TechData.File + " 를 못 읽었다 — 별 배율 1 · 승천 준비 없음으로 선다");

            Mods = new TechForgeMods { Tech = () => Tech != null ? Tech() : null };
            Forge = new ForgeState();
            Wallet = new Wallet();
            Pull();
            Gear = GearCodec.StateFrom(S.Equipment, Defs);
            ReadTransient();

            Engine = new ForgeEngine(Data, Forge, Wallet, Rng.Mulberry((uint)(Meta.NowMs % uint.MaxValue)), () => Meta.NowMs, Mods);
            Engine.QuestBump += (k, n) => Meta.Bump(k, n);
            Engine.LevelReached += lv => Meta.Toast("⚒️ 대장간 레벨 " + lv + " 달성!");
            Engine.Crafted += () => PlaySfx("craft");

            GearHostImpl = new GearHost
            {
                StarMultFn = st => Ascension != null ? Ascension.StarMult(st) : Big.One,
                OnQuestBump = (k, n) => Meta.Bump(k, n),
                OnRefreshHeroEquip = DoRefreshHeroEquip,
                OnRecalcHero = () => { Action h = RecalcHero; if (h != null) h(); }
            };
            GearSys = new GearSystem(Data, Gear, Wallet, GearHostImpl);
            GearSys.SellRefused += (it, price) => Debug.LogError("Forge.sell: 판매가가 유한하지 않아 지급을 건너뛴다 — " + MiniJson.Serialize(GearCodec.ItemTo(it)) + " price=" + price);

            Meta.ForgeUpgradable = () => Engine.UpgradeInfo() != null;
            PlayerInfoPopup.HeroAtk = () => GearSys.HeroStats().Atk;
            PlayerInfoPopup.HeroHp = () => GearSys.HeroStats().Hp;
            PlayerInfoPopup.SubLines = SubLines;
            Meta.Changed += OnMetaChanged;
            UiRoot.Instance.TabBar.Switched += OnTabSwitched;
            heroChangedHook = () => { if (this == null) return; Pull(); Rerender(); };
            DebugPanel.HeroChanged += heroChangedHook;

            DoRefreshHeroEquip(false);
            ForgeSheet.Render(this);
            Ready = true;
            Action r = OnReady;
            if (r != null) r();

            // 원작 boot: 지난 세션이 고르지 않고 떠난 제작품 복원 → 자동 제련이 켜져 있었으면 시퀀스 재개
            RestorePendingCraft();
            if (AutoOn) StartAutoSeq();
        }

        private static IEnumerator ReadStreaming(string name, Action<string> done)
        {
            string p = Path.Combine(Application.streamingAssetsPath, "data", name);
            if (File.Exists(p)) { done(File.ReadAllText(p)); yield break; }
            using (UnityWebRequest req = UnityWebRequest.Get(p))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) done(req.downloadHandler.text);
                else { Debug.LogError("[ForgeHost] " + p + ": " + req.error); done(null); }
            }
        }

        // ===== 세이브 트리 ↔ 상태 =====

        /// <summary>세이브 → 대장간 상태·재화(디버그 패널·다른 시스템이 바꾼 값을 받는다).</summary>
        public void Pull()
        {
            ForgeSave.ReadForge(S, Forge);
            ForgeSave.ReadWallet(S, Wallet);
        }

        /// <summary>대장간 상태·재화·장비·대기품 → 세이브(자동 저장이 언제 돌아도 최신이게).</summary>
        public void Push()
        {
            ForgeSave.WriteForge(S, Forge);
            ForgeSave.WriteWallet(S, Wallet);
            S["equipment"] = GearCodec.StateTo(Gear, Defs);
            S[ForgeSave.KeyPending] = ForgeSave.WriteItem(Pending, PendingSwapped);
            S[ForgeSave.KeyQueue] = ForgeSave.WriteItems(AutoQueue);
            S[ForgeSave.KeyHeld] = AutoHeld;
            S[ForgeSave.KeyBatch] = AutoBatch != null && AutoBatch.Count > 0 ? (object)ForgeSave.WriteItems(AutoBatch) : null;
            S.AutoForgeOn = AutoOn;
        }

        void ReadTransient()
        {
            bool sw;
            Pending = ForgeSave.ReadItem(S[ForgeSave.KeyPending], out sw);
            PendingSwapped = sw;
            AutoQueue = ForgeSave.ReadItems(S.Arr(ForgeSave.KeyQueue));
            AutoHeld = S.Bool(ForgeSave.KeyHeld);
            AutoBatch = ForgeSave.ReadItems(S.Arr(ForgeSave.KeyBatch));
            AutoOn = S.AutoForgeOn;
        }

        /// <summary>원작 `saveGame()` — 되쓰기 + 파일 저장 + HUD·화면 갱신.</summary>
        public void Save() { Push(); Meta.Touch(true); }
        /// <summary>되쓰기 + HUD·화면 갱신(파일 저장은 자동 저장에).</summary>
        public void Touch() { Push(); Meta.Touch(false); }

        /// <summary>남의 변경(상점 보상·퀘스트·디버그) — 화면만 다시 그린다. 세이브 → 상태 읽기(Pull)는 **행동 입구와 1초 틱에서만** 한다:
        /// 퀘스트 `Bump` 가 행동 도중 `Changed` 를 되부르므로 여기서 Pull 하면 방금 뺀 해머가 세이브의 옛 값으로 되살아난다(lost update).</summary>
        void OnMetaChanged() { Rerender(); }

        public void Rerender()
        {
            ForgeSheet.Render(this);
            if (Meta.Popups.IsOpen(ForgeInfoPopup.Name)) ForgeInfoPopup.Render(this);
            if (Meta.Popups.IsOpen(ForgeInfoPopup.ItemName)) ForgeInfoPopup.RenderDetail(this);
            if (Meta.Popups.IsOpen(ForgeAutoPopup.Name)) ForgeAutoPopup.Render(this);
            if (Meta.Popups.IsOpen(ForgeCraftPopup.Name)) ForgeCraftPopup.Render(this);
            if (Meta.Popups.IsOpen(GearDetailPopup.Name)) GearDetailPopup.Render(this);
            Action h = Changed;
            if (h != null) h();
        }

        /// <summary>탭이 바뀌면(원작 switchTab → closeAllTabSurfaces → resolvePendingCraft) 카드판을 걷고 대기품·큐를 정리한다.</summary>
        void OnTabSwitched(string tab)
        {
            ForgeCraftPopup.DismissBatch();
            ResolvePendingCraft();
        }

        private void Update()
        {
            if (!Ready) return;
            tickAcc += Time.unscaledDeltaTime;
            if (tickAcc < 1f) return;
            tickAcc = 0f;
            Pull();
            if (Engine.TickUpgrade())
            {
                Save();
                if (Meta.Popups.IsOpen(ForgeInfoPopup.Name)) ForgeInfoPopup.Render(this);
                if (Meta.Popups.IsOpen(ForgeAutoPopup.Name)) ForgeAutoPopup.Render(this);
            }
            else ForgeSheet.Tick(this);
            if (AutoOn && Wallet.Hammers < 1 && !AnvilBusy && !Meta.Popups.IsOpen(ForgeCraftPopup.Name)) StopAutoSeq();   // 원작 main.js 안전망 틱
        }

        // ===== 조회 =====

        public ForgeUpgrade UpgradeInfo() { return Engine.UpgradeInfo(); }
        public bool Upgrading { get { return Forge.UpgradeEndsAt.HasValue; } }
        public bool AutoForgeUnlocked { get { return S.IsUnlocked(SaveIo.Defs, Defs, "autoForge"); } }
        public bool AscendReady { get { return Ascension != null && Ascension.Ready("forge", new AscensionLevels { ForgeLevel = Forge.ForgeLevel }); } }
        public int AscendCount { get { return Forge.AscendCount; } }
        /// <summary>기술트리 «오토포지» 노드가 1회 동시 해머 상한을 올린다(업당 +1).</summary>
        public int HammerBatchMax() { return HammerBatchMaxBase + (int)Mods.AutoForgeSlotBonus; }

        /// <summary>보류 중인 제작품 = 비교 팝업이 닫혀 있는 대기품(팝업이 열려 있으면 모루 자리는 그대로).</summary>
        public ForgeItem HeldItem { get { return Pending != null && !Meta.Popups.IsOpen(ForgeCraftPopup.Name) ? Pending : null; } }
        public int HeldCount { get { return (Pending != null ? 1 : 0) + (AutoQueue != null ? AutoQueue.Count : 0); } }
        /// <summary>보류 더미 두께(원작 heldDeckDepth): 2→2 · 3~5→3 · 6~14→4 · 15+→5 · 한 장은 0.</summary>
        public static int HeldDeckDepth(int n) { return n >= 15 ? 5 : n >= 6 ? 4 : n >= 3 ? 3 : n >= 2 ? 2 : 0; }

        List<string> SubLines()
        {
            var lines = new List<string>();
            SubsBag bag = GearSys.AllSubsBag();
            foreach (string key in bag.Keys)
            {
                double v = bag[key];
                if (v == 0) continue;
                lines.Add((key == "skillCd" ? "-" : "+") + JsNum.ToString(v) + "% " + (Defs.Substat(key) != null ? Defs.Substat(key).Label : key));
            }
            return lines;
        }

        void DoRefreshHeroEquip(bool withFlash)
        {
            Action<bool> h = RefreshHeroEquip;
            if (h != null) { h(withFlash); return; }
            HeroRig rig = UnityEngine.Object.FindAnyObjectByType<HeroRig>();
            Paperdoll.Refresh(rig, Defs, Gear, withFlash);
        }

        void PlaySfx(string name) { Action<string> s = Sfx; if (s != null) s(name); }

        // ===== 대기품(원작 setPendingCraft · clearPendingCraft · queueAutoMatch · openNextAutoMatch · promoteHeldToSlot · restorePendingCraft · resolvePendingCraft) =====

        public void SetPendingCraft(ForgeItem item, bool swapped = false)
        {
            Pending = item;
            PendingSwapped = swapped;
            Save();
        }

        public ForgeItem ClearPendingCraft()
        {
            ForgeItem item = Pending;
            Pending = null;
            PendingSwapped = false;
            return item;
        }

        public void QueueAutoMatch(ForgeItem item)
        {
            if (Pending == item) ClearPendingCraft();
            AutoQueue.Add(item);
            Save();
        }

        public bool OpenNextAutoMatch()
        {
            if (AutoHeld) { PromoteHeldToSlot(); return false; }
            if (Pending != null) return false;
            while (AutoQueue.Count > 0)
            {
                ForgeItem item = AutoQueue[0];
                AutoQueue.RemoveAt(0);
                if (!ForgeSave.IsForgeShaped(item, Defs)) { Debug.LogError("openNextAutoMatch: 제작물의 최소 형태가 아닌 큐 항목을 버렸다 — " + MiniJson.Serialize(GearCodec.ItemTo(item))); continue; }
                SetPendingCraft(item);
                ShowCraftModal(item);
                return true;
            }
            Save();
            return false;
        }

        public bool PromoteHeldToSlot()
        {
            if (Pending != null) return false;
            while (AutoQueue.Count > 0)
            {
                ForgeItem item = AutoQueue[0];
                AutoQueue.RemoveAt(0);
                if (!ForgeSave.IsForgeShaped(item, Defs)) { Debug.LogError("promoteHeldToSlot: 제작물의 최소 형태가 아닌 큐 항목을 버렸다 — " + MiniJson.Serialize(GearCodec.ItemTo(item))); continue; }
                SetPendingCraft(item);
                return true;
            }
            AutoHeld = false;
            Save();
            return false;
        }

        public void RestorePendingCraft()
        {
            DrainAutoBatch();
            ForgeItem item = Pending;
            if (item != null && !ForgeSave.IsForgeShaped(item, Defs))
            {
                Debug.LogError("restorePendingCraft: 제작물의 최소 형태가 아닌 대기품을 버렸다 — " + MiniJson.Serialize(GearCodec.ItemTo(item)));
                ClearPendingCraft();
                Meta.Toast("⚠️ 손상된 제작 대기품 하나를 복원하지 못했습니다");
                Save();
                OpenNextAutoMatch();
                return;
            }
            if (item == null) { OpenNextAutoMatch(); return; }
            if (AutoHeld) { Rerender(); return; }
            ShowCraftModal(item);
        }

        /// <summary>탭을 옮기면 안 고른 제작품을 자동 판정(판매)한다 — 보류 모드만 예외(팝업만 접는다).</summary>
        public void ResolvePendingCraft()
        {
            Pull();
            DrainAutoBatch();
            if (AutoHeld)
            {
                CancelAnvilStrike();
                ForgeCraftPopup.Hide(this);
                Save();
                return;
            }
            var queued = new List<ForgeItem>(AutoQueue);
            AutoQueue.Clear();
            for (int i = 0; i < queued.Count; i++) if (queued[i] != null && queued[i].Slot != null) GearSys.AutoResolve(queued[i]);
            if (Pending == null) { if (queued.Count > 0) Save(); return; }
            CancelAnvilStrike();
            ForgeCraftPopup.Hide(this);
            ForgeItem item = ClearPendingCraft();
            if (item == null) { Save(); return; }
            GearSys.AutoResolve(item);
            Save();
        }

        // ===== 제작(원작 onCraft · onOpenHeld · onCraftDimClick · resolveCraft · doResolveCraft · sellWarning) =====

        public void OnCraft()
        {
            if (AnvilBusy) return;
            if (Pending != null) { OnOpenHeld(); return; }
            Pull();
            if (Wallet.Hammers < 1) { Meta.Toast("🔨 해머가 부족합니다 (분당 1개 수급)"); return; }
            List<ForgeItem> made = Engine.Craft(1);
            if (made.Count == 0) { Meta.Toast("🔨 해머가 부족합니다 (분당 1개 수급)"); return; }
            ForgeItem item = made[0];
            SetPendingCraft(item);
            AnvilBusy = true;
            PlayAnvilStrike(() =>
            {
                if (Pending != item) { AnvilBusy = false; return; }
                ForgeCraftPopup.ShowReveal(this, item, () =>
                {
                    AnvilBusy = false;
                    if (Pending == item) ShowCraftModal(item);
                });
            });
        }

        public void OnOpenHeld()
        {
            if (Pending == null) return;
            ShowCraftModal(Pending);
        }

        public void ShowCraftModal(ForgeItem item)
        {
            ForgeCraftPopup.Show(this, item);
            ForgeSheet.Render(this);
        }

        /// <summary>비교 팝업 딤 클릭 = 보류: 팝업만 닫고 장비는 대기품 그대로(모루 자리 카드). 자동 제련 중이면 남은 것 전부 보류 + 정지.</summary>
        public void OnCraftDimClick()
        {
            Pull();
            ForgeCraftPopup.Hide(this);
            ForgeSheet.Render(this);
            if (Pending == null) return;
            if (autoSeq || AutoOn || AutoQueue.Count > 0)
            {
                AutoHeld = true;
                int n = HeldCount;
                StopAutoSeq();
                Save();
                Meta.Toast(n > 1
                    ? "📌 남은 " + n + "개 전부 보류 — 자동 제련을 멈췄습니다. 모루 자리 카드를 누르면 하나씩 다시 고릅니다"
                    : "📌 보류 — 자동 제련을 멈췄습니다. 모루 자리 카드를 누르면 다시 고를 수 있습니다");
                return;
            }
            Meta.Toast("📌 보류 — 모루 자리의 카드를 누르면 다시 고를 수 있습니다");
            AutoSeqStep();
        }

        static int AgeRank(GameDefs d, ForgeItem it) { return it == null ? -1 : Array.IndexOf(d.Ages, it.Age); }

        /// <summary>파는 쪽이 남는 쪽보다 시대가 최신이면 경고(원작 sellWarning — 등급이 아니라 **시대**로 잰다).</summary>
        public bool SellWarning(string mode, out ForgeItem sold, out ForgeItem kept)
        {
            sold = null; kept = null;
            if (mode == "equip" || Pending == null) return false;
            ForgeItem k = Gear.Get(Pending.Slot);
            if (k == null) return false;
            if (AgeRank(Defs, Pending) <= AgeRank(Defs, k)) return false;
            sold = Pending; kept = k;
            return true;
        }

        public void ResolveCraft(string mode)
        {
            ForgeItem sold, kept;
            if (SellWarning(mode, out sold, out kept)) { pendingCraftMode = mode; ForgeCraftPopup.ShowSellConfirm(this, sold, kept); return; }
            DoResolveCraft(mode);
        }

        public void OnSellConfirm() { ForgeCraftPopup.HideSellConfirm(this); DoResolveCraft(pendingCraftMode); }
        public void OnSellCancel() { ForgeCraftPopup.HideSellConfirm(this); }

        /// <summary>[판매] 는 팝업을 닫고 · [장착] 은 두 카드를 맞바꾼다(내려온 옛 장비가 새 대기품) · 빈 부위면 닫는다.</summary>
        public void DoResolveCraft(string mode)
        {
            ForgeItem item = ClearPendingCraft();
            pendingCraftMode = null;
            ForgeItem prev = (item != null && mode == "equip") ? Gear.Get(item.Slot) : null;
            bool swapBack = mode == "equip" && prev != null;
            if (!swapBack) ForgeCraftPopup.Hide(this);
            if (item == null) return;
            Pull();
            if (mode == "equip") { GearSys.Equip(item); PlaySfx("equipSnap"); }
            else GearSys.Sell(item);
            if (swapBack)
            {
                Push();
                SetPendingCraft(prev, true);
                ShowCraftModal(prev);
            }
            else Save();
            AutoSeqStep();
            if (!autoSeq) OpenNextAutoMatch();
        }

        // ===== 업그레이드(원작 onStartUpgrade · onGemSkipForge) =====

        public void OnStartUpgrade()
        {
            Pull();
            if (Engine.StartUpgrade()) { Save(); ForgeInfoPopup.Open(this); }
            else Meta.Toast("🪙 코인이 부족합니다");
        }

        public void OnGemSkipForge()
        {
            Pull();
            if (Engine.GemSkip()) { Save(); ForgeInfoPopup.Open(this); }
            else Meta.Toast("💎 젬이 부족합니다");
        }

        // ===== 자동 제련(원작 onAutoForgeBtn · onToggleAutoForge · startAutoSeq · stopAutoSeq · autoSeqStep · autoSeqBatch · craftUntilMatches · drainAutoBatch · purgeNonMatchingHeld) =====

        public void OnAutoForgeBtn()
        {
            if (!AutoForgeUnlocked) { Meta.Toast("🔒 스테이지 2-10 도달 시 해금됩니다"); return; }
            Pull();
            if (!AutoOn) { ForgeAutoPopup.Open(this); return; }
            CancelAnvilStrike();
            StopAutoSeq();
            ForgeAutoPopup.Close(this);
            Meta.Toast("⏹ 자동 제련을 종료했습니다");
        }

        public void OnToggleAutoForge()
        {
            if (!AutoForgeUnlocked) { Meta.Toast("🔒 스테이지 2-10 도달 시 해금됩니다"); return; }
            Pull();
            AutoOn = !AutoOn;
            if (AutoOn) ForgeAutoPopup.Close(this);
            Save();
            if (AutoOn) StartAutoSeq(); else StopAutoSeq();
        }

        public void ToggleKeepAge(string age)
        {
            Pull();
            var cfg = Engine.AutoForgeConfig();
            if (!cfg.KeepAges.Remove(age)) cfg.KeepAges.Add(age);
            Save();
        }

        public void ToggleFilterSub(string key)
        {
            Pull();
            var cfg = Engine.AutoForgeConfig();
            if (!cfg.FilterSubs.Remove(key)) cfg.FilterSubs.Add(key);
            Save();
        }

        public void ToggleAutoFilterOn() { Pull(); var cfg = Engine.AutoForgeConfig(); cfg.FilterOn = !cfg.FilterOn; Save(); }
        public void ToggleStopOnTarget() { Pull(); var cfg = Engine.AutoForgeConfig(); cfg.StopOnTarget = !cfg.StopOnTarget; Save(); }
        public void PickHammers(int n) { Pull(); var cfg = Engine.AutoForgeConfig(); cfg.HammersPerBatch = Math.Min(HammerBatchMax(), Math.Max(1, n)); Save(); }

        public int PurgeNonMatchingHeld()
        {
            DrainAutoBatch();
            int sold = 0;
            var keep = new List<ForgeItem>();
            foreach (ForgeItem it in AutoQueue)
            {
                if (it == null || it.Slot == null) continue;
                if (Engine.PassesAutoFilter(it)) { keep.Add(it); continue; }
                GearSys.AutoResolve(it); sold++;
            }
            AutoQueue.Clear();
            AutoQueue.AddRange(keep);
            ForgeItem held = Pending;
            if (held != null && !Engine.PassesAutoFilter(held))
            {
                if (Meta.Popups.IsOpen(ForgeCraftPopup.Name)) { CancelAnvilStrike(); ForgeCraftPopup.Hide(this); }
                ClearPendingCraft();
                GearSys.AutoResolve(held); sold++;
            }
            Save();
            return sold;
        }

        public void StartAutoSeq()
        {
            if (autoSeq) return;
            AutoHeld = false;
            Pull();
            PurgeNonMatchingHeld();
            autoSeq = true;
            stopAfterPick = false;
            AutoSeqStep();
        }

        public void StopAutoSeq()
        {
            autoSeq = false;
            if (AutoOn) { AutoOn = false; Save(); }
            else if (Meta.Popups.IsOpen(ForgeAutoPopup.Name)) ForgeAutoPopup.Render(this);
            OpenNextAutoMatch();
        }

        void AutoSeqAdvance()
        {
            if (autoSeq) { AutoSeqStep(); return; }
            OpenNextAutoMatch();
        }

        public void AutoSeqStep()
        {
            if (!autoSeq || !AutoOn) return;
            if (AnvilBusy) return;
            if (Pending != null)
            {
                if (Meta.Popups.IsOpen(ForgeCraftPopup.Name)) return;
                ForgeItem held = Pending;
                if (Engine.PassesAutoFilter(held)) { ShowCraftModal(held); return; }
                ClearPendingCraft();
                GearSys.AutoResolve(held);
                Save();
            }
            if (OpenNextAutoMatch()) return;
            Pull();
            if (stopAfterPick || Wallet.Hammers < 1) { StopAutoSeq(); return; }
            AnvilBusy = true;
            PlayAnvilStrike(() => { AnvilBusy = false; AutoSeqBatch(); });
        }

        void AutoSeqBatch()
        {
            if (!autoSeq || !AutoOn) return;
            Pull();
            if (Wallet.Hammers < 1) { StopAutoSeq(); return; }
            var cfg = Engine.AutoForgeConfig();
            int n = (int)Math.Min(Math.Max(1, cfg.HammersPerBatch), Wallet.Hammers);
            List<ForgeItem> items, shown;
            if (!cfg.StopOnTarget && Engine.HasAutoTarget())
            {
                List<ForgeItem> lastSold;
                items = CraftUntilMatches(n, out lastSold);
                shown = items.Count > 0 ? items : lastSold;
            }
            else items = shown = Engine.Craft(n);
            if (shown.Count == 0) { StopAutoSeq(); return; }
            AutoBatch = new List<ForgeItem>(items);
            Save();
            AnvilBusy = true;
            ForgeCraftPopup.ShowBatch(this, shown, () =>
            {
                AnvilBusy = false;
                DrainAutoBatch();
                AutoSeqAdvance();
            });
        }

        /// <summary>통과분이 target 개 모일 때까지 계속 제작(탈락분은 그 자리에서 판매) — 망치 예산·FILL_CRAFT_CAP 이 먼저다.</summary>
        public List<ForgeItem> CraftUntilMatches(int target, out List<ForgeItem> lastSold)
        {
            var kept = new List<ForgeItem>();
            lastSold = new List<ForgeItem>();
            int crafted = 0, sold = 0;
            while (kept.Count < target && Wallet.Hammers >= 1 && crafted < FillCraftCap)
            {
                List<ForgeItem> chunk = Engine.Craft((int)Math.Min(Math.Min(target - kept.Count, Wallet.Hammers), FillCraftCap - crafted));
                if (chunk.Count == 0) break;
                crafted += chunk.Count;
                lastSold = new List<ForgeItem>();
                foreach (ForgeItem it in chunk)
                {
                    if (Engine.PassesAutoFilter(it)) kept.Add(it);
                    else { GearSys.AutoResolve(it); sold++; lastSold.Add(it); }
                }
                AutoBatch = new List<ForgeItem>(kept);
                Save();
            }
            return kept;
        }

        /// <summary>배치 결과를 규약대로 흘려보낸다 — 통과분 → 비교 팝업 큐 · 탈락분 → 판매. `AutoBatch` 를 비우는 곳은 여기뿐이다.</summary>
        public int DrainAutoBatch()
        {
            Pull();
            if (AutoBatch == null || AutoBatch.Count == 0) { AutoBatch = new List<ForgeItem>(); return 0; }
            var batch = new List<ForgeItem>(AutoBatch);
            AutoBatch = new List<ForgeItem>();
            int sold = 0;
            foreach (ForgeItem it in batch)
            {
                if (!ForgeSave.IsForgeShaped(it, Defs)) { Debug.LogError("drainAutoBatch: 제작물의 최소 형태가 아닌 autoBatch 항목을 버렸다 — " + MiniJson.Serialize(GearCodec.ItemTo(it))); continue; }
                if (Engine.PassesAutoFilter(it))
                {
                    if (Engine.AutoForgeConfig().StopOnTarget && autoSeq) stopAfterPick = true;
                    QueueAutoMatch(it);
                }
                else { GearSys.AutoResolve(it); sold++; }
            }
            Save();
            return batch.Count;
        }

        // ===== 모루 연출(원작 playAnvilStrike · cancelAnvilStrike — 소리·그림은 T30·T8 자리 · 여기서는 시각만 지킨다) =====

        void PlayAnvilStrike(Action done)
        {
            CancelAnvilStrike();
            Striking = true;
            PlaySfx("anvilHit");
            ForgeSheet.SetStriking(this, true);
            int gen = ++strikeGen;
            strikeLive = true;
            Delay(AnvilStrikeSec, () =>
            {
                if (gen != strikeGen || !strikeLive) return;   // 취소된 타격 — 콜백을 삼킨다
                strikeLive = false; Striking = false; ForgeSheet.SetStriking(this, false); done();
            });
        }

        public void CancelAnvilStrike()
        {
            strikeLive = false;
            strikeGen++;
            Striking = false;
            AnvilBusy = false;
            ForgeSheet.SetStriking(this, false);
            ForgeCraftPopup.DismissReveal();
        }

        public Coroutine Delay(float sec, Action then) { return StartCoroutine(DelayCo(sec, then)); }

        static IEnumerator DelayCo(float sec, Action then)
        {
            float t = 0f;
            while (t < sec) { t += Time.unscaledDeltaTime; yield return null; }
            then();
        }
    }
}
