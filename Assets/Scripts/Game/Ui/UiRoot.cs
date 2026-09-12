using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Forge.Core;

namespace Forge.Game.Ui
{
    /// <summary>
    /// UI 껍데기 뿌리(ROUTINE T18 · 원작 #app + main.js fitLayout). 오버레이 캔버스 안에 «앱 상자»(기준 1080×1920) 하나를 세이프에어리어 안
    /// 9:16 레터박스에 놓고 높이 비례로 스케일한다 — 원작이 루트 폰트를 앱높이/844×16 으로 잡아 rem 단위 레이아웃을 통째로 키우던 것과 같은 셈.
    /// 세로줄(원작 flex column): 상단바+HUD 층 → 장비 시트(T19 가 채운다) → 채팅 미리보기 → 탭바. 시트 패널(소환)은 탭바 위까지를 덮는다.
    /// 씬은 안 만진다 — 부팅 씬에 <see cref="Bootstrap"/> 이 있으면 그 아래에 스스로 선다.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class UiRoot : MonoBehaviour
    {
        public static UiRoot Instance { get; private set; }

        public Canvas Canvas { get; private set; }
        /// <summary>기준 캔버스(카탈로그 reference) 크기의 앱 상자 — 모든 UI 의 부모.</summary>
        public RectTransform App { get; private set; }
        public RectTransform HudLayer { get; private set; }
        public RectTransform Sheet { get; private set; }
        public RectTransform Chat { get; private set; }
        public RectTransform PanelHost { get; private set; }
        public RectTransform TabBand { get; private set; }
        public Hud Hud { get; private set; }
        public TabBar TabBar { get; private set; }

        private int lastW = -1, lastH = -1;
        private Rect lastSafe;

        // ── T45 SafeArea 주입 지점(테스트 전용 · 게임 코드는 안 쓴다) ──────────────────────────────────
        /// <summary>노치 모의 값(주인 지시 2026-09-12 · ROUTINE §1 «SafeArea»): 위 120px(상단 카메라·노치) · 아래 60px(홈바). 촬영(T27)과 검증(T45)이 같은 상수를 쓴다.</summary>
        public const float NotchTopPx = 120f, NotchBottomPx = 60f;
        /// <summary>테스트가 꽂는 safeArea(null = `Screen.safeArea`). 정적이라 UiRoot 가 서기 전에 꽂아도 첫 Layout 부터 먹는다.</summary>
        public static Rect? SafeAreaOverride { get; private set; }

        /// <summary>테스트용: safeArea 를 덮어쓴다(null 로 되돌린다). 살아 있는 UiRoot 는 바로 다시 배치한다.</summary>
        public static void OverrideSafeArea(Rect? rect)
        {
            SafeAreaOverride = rect;
            if (Instance != null) Instance.Layout();
        }

        /// <summary>노치 모의 safeArea: 화면 w×h 에서 위 <see cref="NotchTopPx"/> · 아래 <see cref="NotchBottomPx"/> 를 깎은 것(왼쪽 아래 원점).</summary>
        public static Rect NotchSafeArea(int w, int h)
        {
            return new Rect(0f, NotchBottomPx, w, h - NotchTopPx - NotchBottomPx);
        }

        /// <summary>지금 배치에 쓰는 safeArea(덮어쓴 값이 있으면 그것 · 아니면 `Screen.safeArea`).</summary>
        public static Rect EffectiveSafeArea { get { return SafeAreaOverride ?? Screen.safeArea; } }

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

        /// <summary>UI 뿌리를 세운다(한 씬에 하나).</summary>
        public static UiRoot Create(Transform parent)
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("UiRoot", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            int ui = LayerMask.NameToLayer("UI");
            if (ui >= 0) go.layer = ui;
            go.transform.SetParent(parent, false);
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            UiRoot root = go.AddComponent<UiRoot>();
            root.Canvas = canvas;
            root.Build();
            return root;
        }

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Build()
        {
            float sheetTop = UiKit.L("sheet_top");
            float chatTop = UiKit.L("chat_top");
            float tabTop = UiKit.L("tabbar_top");
            float line = UiKit.L("line_px");
            float line3 = UiKit.L("line3_px");

            App = UiKit.Box(transform, "app");

            HudLayer = UiKit.Box(App, "game-hud");
            UiKit.Band(HudLayer, 0f, sheetTop);

            Sheet = UiKit.Box(App, "equip-sheet");
            UiKit.Band(Sheet, sheetTop, chatTop);
            UiKit.Panel(Sheet, "bg", "sheet_bg");
            UiKit.Line(Sheet, "line", "pp_line", line3, true);

            Chat = UiKit.Box(App, "chat-preview");
            UiKit.Band(Chat, chatTop, tabTop);
            UiKit.Panel(Chat, "bg", "chat_bg");
            UiKit.Line(Chat, "line", "pp_line", line, true);

            PanelHost = UiKit.Box(App, "panels");
            UiKit.Band(PanelHost, 0f, tabTop);

            TabBand = UiKit.Box(App, "tabbar");
            UiKit.Band(TabBand, tabTop, 1f);

            Hud = HudLayer.gameObject.AddComponent<Hud>();
            Hud.Build(HudLayer, Chat);
            TabBar = TabBand.gameObject.AddComponent<TabBar>();
            TabBar.Build(TabBand, PanelHost);

            Layout();
        }

        private void Update()
        {
            if (Screen.width != lastW || Screen.height != lastH || EffectiveSafeArea != lastSafe) Layout();
        }

        /// <summary>앱 상자를 세이프에어리어 안 9:16 레터박스에 맞춘다(화면 픽셀 = 캔버스 단위 · 오버레이 캔버스 scaleFactor 1).
        /// 3D 카메라(<see cref="Bootstrap"/>)는 전체 화면 레터박스 그대로다 — safeArea 는 여기(눌러야 하는 UI)에만 걸린다(T45 결정 기록).</summary>
        public void Layout()
        {
            lastW = Screen.width;
            lastH = Screen.height;
            lastSafe = EffectiveSafeArea;
            Rect sa = lastSafe;
            if (sa.width <= 0f || sa.height <= 0f) sa = new Rect(0f, 0f, lastW, lastH);
            ViewportRect r = Viewport.Letterbox(Mathf.RoundToInt(sa.width), Mathf.RoundToInt(sa.height), Bootstrap.PortraitAspect);
            float ph = r.H * sa.height;
            float scale = ph / UiKit.RefH;
            App.anchorMin = App.anchorMax = Vector2.zero;
            App.pivot = Vector2.zero;
            App.sizeDelta = new Vector2(UiKit.RefW, UiKit.RefH);
            App.localScale = new Vector3(scale, scale, 1f);
            App.anchoredPosition = new Vector2(sa.x + r.X * sa.width, sa.y + r.Y * sa.height);
        }

        /// <summary>앱 상자의 화면 픽셀 사각형(왼쪽 아래 원점).</summary>
        public Rect AppScreenRect
        {
            get
            {
                return new Rect(App.anchoredPosition.x, App.anchoredPosition.y,
                    App.sizeDelta.x * App.localScale.x, App.sizeDelta.y * App.localScale.y);
            }
        }
    }
}
