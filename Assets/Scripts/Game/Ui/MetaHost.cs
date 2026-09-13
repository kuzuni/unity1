using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.Meta;
using Forge.Core.Save;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T25 Core(상점·패스·퀘스트·리그·채팅)를 세이브(T13)·HUD(T18)·탭바에 잇는 접착(ROUTINE T22 · 원작 main.js 의 부팅·1초 틱 + ui.js 의 탭 라우팅).
    /// 규칙은 전부 Core 가 계산한다 — 여기는 상태를 세이브 트리와 주고받고(<see cref="MetaSave"/>) 화면을 열고 닫는다.
    /// 부팅: <see cref="SaveIo"/> 가 준비되면 `meta.json` 을 읽어 표를 세우고, 세이브의 `shop`·`passClaimed`·`quests`·`league`·`chat` 칸을 상태로 읽는다.
    /// 다른 시스템(T14·T16·T17·T23·T24)은 행동 지점에서 <see cref="Bump"/> 를 부른다(원작 `Quests.bump`). 전투력은 T8 이 <see cref="CombatPower"/> 를 꽂는다.
    /// </summary>
    [DefaultExecutionOrder(-800)]
    public sealed class MetaHost : MonoBehaviour
    {
        public static MetaHost Instance { get; private set; }
        /// <summary>지금 살아 있는 MetaHost 가 부팅을 마쳤는가 — 인스턴스에서 파생한다(정적 플래그로 두면 씬을 다시 여는 PlayMode 테스트에서 앞 씬의 값이 남아 새 호스트가 부팅되기 전에 참이 된다 · CI 런 27 실측).</summary>
        public static bool Ready { get { return Instance != null && Instance.booted; } }
        private bool booted;
        /// <summary>표·상태가 준비된 뒤 한 번.</summary>
        public static event Action OnReady;

        public MetaTable Meta { get; private set; }
        public Shop Shop { get; private set; }
        public Pass Pass { get; private set; }
        public Quests Quests { get; private set; }
        public League League { get; private set; }
        public Chat Chat { get; private set; }

        public ShopState ShopState { get; private set; }
        public PassState PassState { get; private set; }
        public QuestState QuestState { get; private set; }
        public LeagueState LeagueState { get; private set; }
        public ChatState ChatState { get; private set; }

        public IRewardWallet Wallet { get; private set; }
        public Rng Rng { get; private set; }

        /// <summary>
        /// 원작 `Combat.combatPower()`. 기본값이 살아 있는 전투(<see cref="Forge.Game.Battle.BattleScene"/>)의 <c>Battle.CombatPower()</c> 를 읽는다 —
        /// T7 이 정본 식(`atk × 공속 × (1+치명) + hp/8`)을 Core 에 이미 옮겼으므로 여기서 수를 다시 세지 않는다(§1).
        /// 전투가 아직 안 섰으면 0. 다른 것을 꽂고 싶으면 이 대리자를 덮어쓰면 된다(테스트·PvP).
        /// T63 실측: 예전 기본값이 `() => Big.Zero` 라 **아무도 안 꽂아** 상단바 전투력이 장비 8부위를 낀 채로도 `⚔ 0` 이었다.
        /// </summary>
        public Func<Big> CombatPower = LiveCombatPower;

        /// <summary>살아 있는 전투에서 읽는 전투력(없으면 0) — <see cref="CombatPower"/> 의 기본 대리자.</summary>
        private static Big LiveCombatPower()
        {
            Forge.Game.Battle.BattleScene bs = Forge.Game.Battle.BattleScene.Instance;
            if (bs == null || !bs.Ready || bs.Battle == null) return Big.Zero;
            return bs.Battle.CombatPower();
        }
        /// <summary>원작 `!!Forge.upgradeInfo()` — T19 대장간 UI 가 꽂는다. null = «모르면 막지 않는다».</summary>
        public Func<bool> ForgeUpgradable;
        /// <summary>원작 `SFX.musicEnabled`·`SFX.toggleMusic()` 자리 — T30 이 꽂는다. 그 전엔 세이브 `musicOn` 칸만 뒤집는다.</summary>
        public Func<bool> MusicEnabled;
        public Action ToggleMusic;
        public Action SfxResumeAndCraft;

        /// <summary>상태·재화가 바뀌었다 — 열린 화면이 다시 그린다.</summary>
        public event Action Changed;

        public SaveState S { get { return SaveIo.State; } }
        public double NowMs { get { return SaveIo.NowMs(); } }
        public string TodayKey { get { return DailyReset.ResetDateKey(DateTime.Now); } }
        public Big MyCp { get { Func<Big> f = CombatPower; return f != null ? f() : Big.Zero; } }

        private float tickAcc;
        private Screens screens;

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

        public static MetaHost Create(Transform parent)
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("MetaHost");
            go.transform.SetParent(parent, false);
            return go.AddComponent<MetaHost>();
        }

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
            // 한 프레임 뒤에 시작한다 — 같은 sceneLoaded 에서 서는 SaveIo·UiRoot 가 먼저 제 자리를 잡게(정적 Ready 가 앞 씬 것일 수 있다).
            yield return null;
            while (SaveIo.Instance == null || !SaveIo.Ready || SaveIo.State == null || UiRoot.Instance == null || UiRoot.Instance.TabBar == null) yield return null;
            string metaJson = null;
            IEnumerator read = ReadStreaming(MetaTable.File, t => metaJson = t);
            while (read.MoveNext()) yield return read.Current;
            if (metaJson == null) { Debug.LogError("[MetaHost] StreamingAssets/data/" + MetaTable.File + " 를 못 읽었다 — 상점·패스·퀘스트·리그·채팅을 세우지 않는다"); yield break; }
            Meta = MetaTable.Load(metaJson);
            Wallet = new SaveStateWallet(S);
            Rng = Rng.Mulberry((uint)(NowMs % uint.MaxValue));
            Shop = new Shop(Meta.Shop);
            Pass = new Pass(Meta.Pass, Meta.State);
            Quests = new Quests(Meta.Quests, () => { Func<bool> f = ForgeUpgradable; return f == null || f(); });
            League = new League(Meta.League, Meta.Avatars, Rng);
            Chat = new Chat(Meta.Chat, Meta.Avatars, Rng);
            ReadStates();
            Chat.Ensure(ChatState, NowMs);
            WriteStates();

            screens = new Screens(this, UiRoot.Instance, PopupLayer.Create(UiRoot.Instance));
            SyncHud(true);
            booted = true;
            Action h = OnReady;
            if (h != null) h();

            // 원작 main.js boot: 1분 이상 누적된 오프라인 보상은 팝업으로
            OfflineReward pending = SaveIo.Instance != null ? SaveIo.Instance.PendingOffline() : null;
            if (pending != null && pending.Elapsed >= 60) OfflinePopup.Show(this, pending);
        }

        private static IEnumerator ReadStreaming(string name, Action<string> done)
        {
            string p = Path.Combine(Application.streamingAssetsPath, "data", name);
            if (File.Exists(p)) { done(File.ReadAllText(p)); yield break; }
            using (UnityWebRequest req = UnityWebRequest.Get(p))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) done(req.downloadHandler.text);
                else { Debug.LogError("[MetaHost] " + p + ": " + req.error); done(null); }
            }
        }

        // ---- 세이브 트리 ↔ 상태 ----

        private void ReadStates()
        {
            JsonObject root = S.Root;
            ShopState = MetaSave.ReadShop(root);
            PassState = MetaSave.ReadPass(root);
            QuestState = MetaSave.ReadQuests(root);
            LeagueState = MetaSave.ReadLeague(root) ?? new LeagueState();
            ChatState = MetaSave.ReadChat(root);
        }

        /// <summary>상태를 세이브 트리에 되쓴다 — 자동 저장(30초·백그라운드·종료)이 언제 돌아도 최신이게 매 변경마다 부른다.</summary>
        public void WriteStates()
        {
            JsonObject root = S.Root;
            MetaSave.WriteShop(root, ShopState);
            MetaSave.WritePass(root, PassState);
            MetaSave.WriteQuests(root, QuestState);
            MetaSave.WriteLeague(root, LeagueState);
            MetaSave.WriteChat(root, ChatState);
        }

        /// <summary>원작 `saveGame()` 자리 — 상태 되쓰기 + 파일 저장.</summary>
        public void Save()
        {
            WriteStates();
            if (SaveIo.Instance != null) SaveIo.Instance.Save();
        }

        /// <summary>무엇이 바뀐 뒤 — 되쓰기 · HUD · 열린 화면 갱신.</summary>
        public void Touch(bool save = true)
        {
            if (save) Save(); else WriteStates();
            SyncHud();
            Action h = Changed;
            if (h != null) h();
        }

        private string hudNick, hudCp, hudCoins, hudGems, hudAvatar;

        /// <summary>원작 renderTopBar — 닉네임·전투력·코인·젬. **원천 값이 바뀌었을 때만** HUD 에 쓴다 — 매초 무조건 덮으면 HUD 표면을 직접 쓰는 다른 코드·테스트(T18 `HUD_Set`)의 글자를 되돌린다(CI 런 40 실측).</summary>
        public void SyncHud(bool force = false)
        {
            Hud hud = Hud.Instance;
            if (hud == null || S == null) return;
            string nick = Nickname, cp = PopupKit.Fmt(MyCp), coins = PopupKit.Fmt(S.Coins), gems = PopupKit.Fmt(S.Gems);
            // 닉네임과 전투력을 **따로** 민다 — 전투력은 전투 중 매초 바뀌는데(T63) 같이 밀면 닉네임을 직접 준 쪽의 글자를 덮는다(런 108 실측).
            if (force || nick != hudNick) { hud.SetProfile(nick, cp); hudNick = nick; hudCp = cp; }
            if (force || cp != hudCp) { hud.SetCombatPower(cp); hudCp = cp; }
            if (force || coins != hudCoins || gems != hudGems) { hud.SetCurrency(coins, gems); hudCoins = coins; hudGems = gems; }
            // 원작은 아바타도 renderTopBar 가 닉네임·전투력과 같이 그린다(ui.js renderTopBar · onPickAvatar 가 다시 부른다).
            string avatar = AvatarEmoji;
            if (force || avatar != hudAvatar) { hud.SetAvatar(avatar); hudAvatar = avatar; }
        }

        // ---- 프로필 칸(원작 S.nickname · S.avatarEmoji · S.gender · S.settingsDummy) ----

        public string Nickname { get { string n = S.Nickname; return string.IsNullOrEmpty(n) ? "용사" : n; } }
        public string AvatarEmoji { get { string a = S.Str("avatarEmoji"); return string.IsNullOrEmpty(a) ? Meta.Avatars.DefaultAvatar : a; } }
        public string Gender { get { string g = S.Str("gender"); return g == Chat.GenderFemale ? Chat.GenderFemale : Chat.GenderMale; } }

        public int AvatarIndex(string emoji)
        {
            if (Meta == null || emoji == null) return -1;
            return Array.IndexOf(Meta.Avatars.Pool, emoji);
        }

        public JsonObject SettingsDummy
        {
            get
            {
                JsonObject d = S.Obj("settingsDummy");
                if (d == null)
                {
                    d = new JsonObject();
                    d["vibration"] = true; d["chatShow"] = true; d["chatDark"] = false; d["clanChatPreview"] = true;
                    S["settingsDummy"] = d;
                }
                return d;
            }
        }

        // ---- 행동 훅 ----

        /// <summary>원작 `Quests.bump(action, n)` — 각 시스템의 행동 지점에서 부른다.</summary>
        public void Bump(string action, double n = 1)
        {
            if (!Ready) return;
            if (Quests.Bump(QuestState, action, n)) Touch(false);
        }

        /// <summary>1초 틱(원작 main.js) — 봇 채팅 · HUD.</summary>
        private void Update()
        {
            if (!Ready) return;
            tickAcc += Time.unscaledDeltaTime;
            if (tickAcc < 1f) return;
            tickAcc = 0f;
            if (Chat.Tick(ChatState, NowMs)) Touch(false);
            SyncHud();
            OfflinePopup.Tick(this);
        }

        // ---- 화면 열기 ----
        public void OpenShop() { screens.OpenShop(); }
        public void OpenQuests() { screens.OpenQuests(); }
        public void OpenLeague() { screens.OpenLeague(); }
        public void OpenPass() { screens.OpenPass(); }
        public void OpenProfile() { screens.OpenProfile(); }
        public void OpenPlayerInfo() { screens.OpenPlayerInfo(); }
        public void OpenChat() { screens.OpenChat(); }
        public void OpenDebug() { screens.OpenDebug(); }
        public void OpenStub(string title, string desc) { PopupLayer.Instance.ShowStub(title, desc); }
        public void Toast(string msg) { PopupLayer.Instance.Toast(msg); }
        public PopupLayer Popups { get { return PopupLayer.Instance; } }

        /// <summary>탭·HUD 버튼 라우팅(원작 onTabClick · #topbar onclick · #chat-preview onclick).</summary>
        private sealed class Screens
        {
            private readonly MetaHost host;
            private readonly UiRoot root;
            private readonly PopupLayer popups;

            public Screens(MetaHost host, UiRoot root, PopupLayer popups)
            {
                this.host = host;
                this.root = root;
                this.popups = popups;
                root.TabBar.OpenRequested += OnTab;
                root.TabBar.Switched += OnSwitched;
                popups.TabXChanged += root.TabBar.SetPopupX;
                root.Hud.ProfileButton.onClick.AddListener(OpenProfile);
                root.Hud.ChatButton.onClick.AddListener(OpenChat);
                host.Changed += Rerender;
            }

            private void OnTab(string key)
            {
                if (key == "shop") OpenShop();
                else if (key == "quest") OpenQuests();
                else if (key == "pvp") OpenLeague();
            }

            /// <summary>탭이 바뀌면(홈 포함) 열린 팝업을 전부 접는다(원작 closeAllTabSurfaces · closeOpened).</summary>
            private void OnSwitched(string tab) { popups.HideAll(); }

            private void Rerender()
            {
                if (popups.IsOpen(ShopSheet.Name)) ShopSheet.Render(host);
                if (popups.IsOpen(QuestSheet.Name)) QuestSheet.Render(host);
                if (popups.IsOpen(LeagueSheet.Name)) LeagueSheet.Render(host);
                if (popups.IsOpen(PassPopup.Name)) PassPopup.Render(host);
                if (popups.IsOpen(ProfilePopup.Name)) ProfilePopup.Render(host);
                if (popups.IsOpen(PlayerInfoPopup.Name)) PlayerInfoPopup.Render(host);
                if (popups.IsOpen(DebugPanel.Name)) DebugPanel.Render(host);
                ChatScreen.OnChanged(host);
            }

            public void OpenShop() { ShopSheet.Open(host); }
            public void OpenQuests() { QuestSheet.Open(host); }
            public void OpenLeague() { LeagueSheet.Open(host); }
            public void OpenPass() { PassPopup.Open(host); }
            public void OpenProfile() { ProfilePopup.Open(host); }
            public void OpenPlayerInfo() { PlayerInfoPopup.Open(host); }
            public void OpenChat() { ChatScreen.Open(host); }
            public void OpenDebug() { DebugPanel.Open(host); }
        }
    }
}
