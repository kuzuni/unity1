using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Forge.Core;

namespace Forge.Game.Ui
{
    /// <summary>열린 팝업 하나(원작 `.modal` — 딤 + 카드). <see cref="Root"/> 가 앱 상자를 꽉 채우는 딤이고, 화면은 그 안에 카드·시트를 세운다.</summary>
    public sealed class Popup
    {
        public string Name;
        /// <summary>이 팝업을 소유한 popup 탭(pvp·quest·shop). 열린 동안 그 탭이 빨간 ✕ 다(원작 MODAL_TAB). 없으면 null.</summary>
        public string Tab;
        public bool AboveTabBar;
        public RectTransform Root;
        public bool IsOpen { get { return Root != null; } }
    }

    /// <summary>
    /// 팝업 층(ROUTINE T22 · 원작 ui.js `showModal` · `closeOpened` · `closeAllTabSurfaces` · `openStub` · `toast`).
    /// 두 층: 탭바 **아래**(원작 `.modal` z20 — 시트·일반 팝업 · 탭바가 위에 남는다) · 탭바 **위**(원작 `dim-tabbar` z40 — 리그 보상·상대 선택). 토스트는 맨 위.
    /// 씬 파일을 안 만진다 — <see cref="UiRoot"/> 의 앱 상자 안에 형제로 선다.
    /// </summary>
    public sealed class PopupLayer : MonoBehaviour
    {
        public static PopupLayer Instance { get; private set; }

        private RectTransform under;
        private RectTransform over;
        private RectTransform toasts;
        /// <summary>전투 토스트 레인(정본 `#toasts-combat` · T138) — **모달 아래** 형제라 팝업을 읽는 중에 끼어들지 않는다(정본 index.html 203 주석 · 사용자 지시 2026-08-18).</summary>
        private RectTransform toastsCombat;
        private readonly List<Popup> open = new List<Popup>();

        /// <summary>팝업이 열리거나 닫혀 «어느 popup 탭이 ✕ 여야 하는가» 가 바뀌었다(인자 = 탭 키 또는 null).</summary>
        public event Action<string> TabXChanged;

        public int OpenCount { get { return open.Count; } }

        /// <summary>탭바 위 층(`aboveTabBar` 팝업이 사는 곳) — 테스트가 층 순서를 본다(T78).</summary>
        public RectTransform OverLayer { get { return over; } }
        public IReadOnlyList<Popup> Open { get { return open; } }
        /// <summary>전투 토스트 레인 상자(테스트가 층 순서·자식을 본다 · T138).</summary>
        public RectTransform CombatLane { get { return toastsCombat; } }

        public static PopupLayer Create(UiRoot root)
        {
            if (Instance != null && Instance.under != null && Instance.under.parent == root.App) return Instance;
            RectTransform app = root.App;
            RectTransform under = UiKit.Box(app, "modals");
            under.SetSiblingIndex(root.TabBand.GetSiblingIndex());
            RectTransform over = UiKit.Box(app, "modals-over");
            RectTransform toasts = UiKit.Box(app, "toasts");
            RectTransform toastsCombat = UiKit.Box(app, Forge.Core.Ui.LootFeedRules.CombatBoxName(LootFeed.Spec));
            toastsCombat.SetSiblingIndex(under.GetSiblingIndex());   // 모달 바로 아래(정본 z 19 < .modal 20)
            PopupLayer layer = under.gameObject.AddComponent<PopupLayer>();
            layer.under = under;
            layer.over = over;
            layer.toasts = toasts;
            layer.toastsCombat = toastsCombat;
            Instance = layer;
            return layer;
        }

        private void Awake() { if (Instance == null) Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public Popup Find(string name)
        {
            for (int i = 0; i < open.Count; i++) if (open[i].Name == name) return open[i];
            return null;
        }

        public bool IsOpen(string name) { return Find(name) != null; }

        /// <summary>팝업을 연다(원작 showModal). 같은 이름이 이미 열려 있으면 그것을 돌려준다 — 화면은 내용만 다시 그린다.</summary>
        public Popup Show(string name, string tab = null, bool aboveTabBar = false, string dimKey = "modal_dim")
        {
            Popup p = Find(name);
            if (p != null) return p;
            p = new Popup { Name = name, Tab = tab, AboveTabBar = aboveTabBar };
            p.Root = UiKit.Box(aboveTabBar ? over : under, "modal-" + name);
            Image dim = UiKit.Panel(p.Root, "dim", dimKey);
            dim.color = UiKit.PerceivedDim(dim.color);   // T94 — 정본 rgba(0,0,0,.5) 를 선형 공간에서 브라우저와 같은 밝기로(결정 191 · modal_dim_deep 도 같은 환산)
            dim.raycastTarget = true;
            open.Add(p);
            CardPop.Begin(p.Root);   // T135 ⓑ — 정본 `.modal.opening .modal-card` cardpop: 처음 열 때만(위의 «이미 열림» 갈래는 안 거친다 = ui.js 1156)
            RaiseTabX();
            return p;
        }

        /// <summary>팝업의 내용을 비운다(재렌더 직전 · 딤은 남긴다).</summary>
        public static RectTransform Clear(Popup p)
        {
            for (int i = p.Root.childCount - 1; i >= 1; i--) Destroy(p.Root.GetChild(i).gameObject);
            return p.Root;
        }

        public void Hide(string name) { Hide(Find(name)); }

        public void Hide(Popup p)
        {
            if (p == null || !p.IsOpen) return;
            open.Remove(p);
            Destroy(p.Root.gameObject);
            p.Root = null;
            RaiseTabX();
        }

        /// <summary>원작 closeAllTabSurfaces / closeOpened — 전부 닫는다.</summary>
        public void HideAll()
        {
            for (int i = open.Count - 1; i >= 0; i--)
            {
                Popup p = open[i];
                open.RemoveAt(i);
                if (p.Root != null) Destroy(p.Root.gameObject);
                p.Root = null;
            }
            RaiseTabX();
        }

        private void RaiseTabX()
        {
            string tab = null;
            for (int i = open.Count - 1; i >= 0; i--) if (open[i].Tab != null) { tab = open[i].Tab; break; }
            Action<string> h = TabXChanged;
            if (h != null) h(tab);
        }

        // ---- 스텁 팝업 (원작 openStub) ----

        public Popup ShowStub(string title, string desc)
        {
            Popup p = Show("stub");
            Clear(p);
            RectTransform card = PopupKit.Card(p.Root, "card", UiKit.L("modal_card_w") * UiKit.RefW - PopupKit.Line3 * 2f, -1f, "pp_paper", UiKit.H("card_r"));   // T473 — 표값은 정본 CSS width
            PopupKit.Column(card, UiKit.H("card_pad"), PopupKit.Rem * 0.45f);
            PopupKit.Label(card, "title", TextKind.Title2, title, "pp_ink");   // T404 ⓑ — 정본 1761 `.modal-card h3 { 1.15rem }` = 41.9px → Title2 42(전엔 Title 60)
            PopupKit.Label(card, "desc", TextKind.Body, desc, "pp_muted", TextAlignmentOptions.Center, true);
            // T132 — 정본 ui.js 1257 `${IconGen.img('barrier', 'stub-ico wide')}다음 업데이트에서 추가될 예정입니다.` : 바리케이드가 글자 앞에 선다
            // (style.css 1767·1770: 높이 1.35em · 가로 1.88em(ASPECT 1.39) · 오른쪽 .42em). 치수는 StaticIconsUi.json(§1).
            RectTransform soon = UiKit.Box(card, "soon-row");
            HorizontalLayoutGroup soonLay = soon.gameObject.AddComponent<HorizontalLayoutGroup>();
            soonLay.childAlignment = TextAnchor.MiddleCenter;
            soonLay.childControlWidth = true; soonLay.childControlHeight = true;
            soonLay.childForceExpandWidth = false; soonLay.childForceExpandHeight = false;
            float soonEm = PopupKit.FontSize(TextKind.Body);
            soonLay.spacing = StaticIconsUi.Em("stub_ico_gap_em", soonEm);
            Image barrier = PopupKit.IconOr(soon, "barrier", "barrier");
            LayoutElement barrierLe = barrier.gameObject.AddComponent<LayoutElement>();
            barrierLe.preferredWidth = StaticIconsUi.Em("stub_ico_w_em", soonEm);
            barrierLe.preferredHeight = StaticIconsUi.Em("stub_ico_h_em", soonEm);
            barrierLe.flexibleWidth = 0f;
            PopupKit.Label(soon, "soon", TextKind.Body, "다음 업데이트에서 추가될 예정입니다.", "pp_muted", TextAlignmentOptions.Center, true);
            PopupKit.Btn(card, "close", "닫기", "pp_gray", "pp_gray_dk", () => Hide(p), -1f, PopupKit.ModalBtnH, "pp_ink");
            return p;
        }

        // ---- 토스트 (원작 toast(msg, lane) · 2.6초) ----

        /// <summary>정본 `UI.toast(msg, lane)` — `lane == "combat"` 이면 전투 레인(모달 아래 · 정본 `#toasts-combat`), 아니면 기본 레인(팝업 위). T138 이 레인 인자를 붙였다.</summary>
        public void Toast(string msg, string lane = null)
        {
            float w = UiKit.L("toast_w") * UiKit.RefW;
            float h = UiKit.H("toast_h");
            bool combat = Forge.Core.Ui.LootFeedRules.IsCombatLane(LootFeed.Spec, lane) && toastsCombat != null;
            RectTransform box = combat ? toastsCombat : toasts;
            int stack = combat ? combatStack : toastStack;
            RectTransform t = UiKit.Box(box, "toast");
            UiKit.Anchor(t, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, UiKit.H("toast_bottom") + stack * h * 1.2f), w, h);
            UiKit.Rounded(t, "line", "toast_line", h * 0.5f);
            Image face = UiKit.Rounded(t, "bg", "toast_bg", h * 0.5f - PopupKit.Line);
            PopupKit.Inset(face.rectTransform, PopupKit.Line);
            // T89 — 정본 `UI.paintIconText` 의 자리가 바로 여기다(표 이름이 `TOAST_ICON` 인 이유).
            // 표에 있는 이모지는 T31 아이콘으로, 나머지는 글자로 선다. 표가 아직 안 읽혔으면 조각 하나 = 옛 모양 그대로.
            RectTransform rowRt = UiKit.IconTextRow(t, "msg-row", TextKind.Sub, msg, "ink");
            foreach (TextMeshProUGUI piece in rowRt.GetComponentsInChildren<TextMeshProUGUI>(true)) piece.fontStyle = FontStyles.Bold;
            if (combat) combatStack++; else toastStack++;
            LastToast = msg;
            LastToastLane = combat ? lane : null;
            // T454 ⓐ — 정본 1936 `.toast { opacity: 0; translateY(-.5rem); transition: all .25s }` → 1938 `.show`: 위 .5rem 에서 내려오며 켜진다(값은 표 TransitionUi.json `toast_in`).
            ToastEnter.Attach(t, "toast_in");
            StartCoroutine(ToastLife(t.gameObject, combat));
        }

        /// <summary>마지막 토스트 문구 · 그 레인(기본이면 null)(테스트가 본다).</summary>
        public string LastToast { get; private set; }
        public string LastToastLane { get; private set; }
        private int toastStack, combatStack;

        private IEnumerator ToastLife(GameObject go, bool combat)
        {
            yield return new WaitForSecondsRealtime(2.6f);
            if (combat) combatStack = Mathf.Max(0, combatStack - 1); else toastStack = Mathf.Max(0, toastStack - 1);
            if (go != null) Destroy(go);
        }
    }

    /// <summary>팝업 조각 공장 — 카드·시트·버튼·◀·✕·토글·목록. 치수는 카탈로그(`layout`) · 색은 카탈로그(`colors`) 에서만 읽는다(§1).</summary>
    public static class PopupKit
    {
        public static float Rem { get { return UiKit.H("rem_h"); } }
        /// <summary>글자 종류의 크기(카탈로그 textKinds) — 행 높이를 글자에 맞출 때.</summary>
        public static float FontSize(TextKind k) { return UiCatalog.Instance.Kind(k).size; }

        /// <summary>T378 11·12회차 — «제목 + small» 두 줄 버튼의 높이. 정본 `.btn` 은 높이를 안 주고 **글이 높이를 정한다**:
        /// 세로 패딩(px) x 2 + 두 줄의 줄높이 — 정본이 `line-height` 를 준 자리면 그 표 키(`lhKey` · LineHeightUi.json), 안 준 자리면 normal = 글꼴 자산 비율.
        /// 정본이 `min-height` 를 준 자리(3565 `.modal-card > .row .btn`)는 `minH` 로 넘겨 둘 중 큰 쪽을 쓴다(border-box 하한).
        /// 클론은 두 줄을 다 `k` 로 찍는다(정본 small 은 글자 하한 §1 에 걸려 같은 단).</summary>
        public static float TwoLineBtnH(TextKind k, float padY, string lhKey, float minH = 0f)
        {
            return BtnH(k, padY, lhKey, minH, 2);
        }

        /// <summary>T378 13회차 — 같은 식의 **줄 수 갈래**(정본 `.af-start`(4816)처럼 한 줄이면서 `min-height` + `padding` 을 함께 준 자리).
        /// 높이 = max(하한, 세로 패딩 × 2 + 줄 수 × 줄높이) — 두 줄 갈래(<see cref="TwoLineBtnH"/>)가 쓰던 식 그대로고 줄 수만 인수로 뺐다.</summary>
        public static float BtnH(TextKind k, float padY, string lhKey, float minH = 0f, int lines = 1)
        {
            var f = UiFont.Primary.faceInfo;
            float fs = FontSize(k);
            float ratio = lhKey == null ? f.lineHeight / f.pointSize : (float)LineHeight.Table.Get(lhKey);
            return Mathf.Max(minH, padY * 2f + lines * fs * ratio);
        }
        /// <summary>T447 — 정본 `.btn`(style.css **665**)에는 **높이 규칙이 없다**: 상자 = 글자 줄 + 세로 패딩 두 겹 + 테 두 겹이고 **내용이 정한다**.
        /// 팝업 갈래는 **3542** `.modal-card .btn, .panel .btn, #equip-sheet .btn { border: var(--ol3) solid var(--pp-line) }` 가 테를 `ol3` 로 덮는다.
        /// 아래턱 `.22rem` 은 `inset` box-shadow 라 **상자를 안 키우고 채움을 먹는다** — 그래서 여기 안 더한다(`Btn` 이 면을 그릴 때 뺀다).
        /// 단은 정본이 못 박은 `.88rem`(`btn_font_rem`)이다: 클론 라벨의 글자 종류와 다를 수 있지만 **정본에서 상자를 정하는 것은 규칙의 단**이다(라벨 단은 T391·T404 축).
        /// 정본이 이 자리에 `line-height` 를 안 줬으니 normal = 글꼴 자산 비율을 쓴다(<see cref="BtnH"/> 와 한 셈).
        /// 종전 표값 `btn_h`(2.4rem)는 근거가 없어 원작 대비 채움이 −0.65%p 였다(런 1067 실측 4.08 ↔ 3.43%H).</summary>
        public static float ModalBtnH
        {
            get
            {
                var f = UiFont.Primary.faceInfo;
                return Rem * UiKit.L("btn_font_rem") * (f.lineHeight / f.pointSize)
                     + Rem * UiKit.L("btn_pad_y_rem") * 2f + Line3 * 2f;
            }
        }
        public static float Line { get { return UiKit.L("line_px"); } }
        public static float Line3 { get { return UiKit.L("line3_px"); } }
        /// <summary>정본 --ol2(4px) — 설정 토글·작은 아바타·리그 행 같은 «중간 단» 테(T365 10회차).</summary>
        public static float Line2 { get { return UiKit.L("line2_px"); } }
        /// <summary>탭바 위쪽 y(앱 위에서 · 기준 px).</summary>
        public static float TabTop { get { return UiKit.L("tabbar_top") * UiKit.RefH; } }

        /// <summary>카드 아래턱에 걸치는 ✕(<see cref="XButton"/>)가 카드 밑으로 내려가는 양 — 원 반지름 + 아래턱 그림자.</summary>
        public static float XOverhang { get { return UiKit.H("xbtn") * 0.5f + UiKit.H("xbtn_shadow"); } }

        /// <summary>
        /// 가운데 정렬 카드가 **HUD 상단바 아래 ~ 탭바 위**에 들어가게 높이와 y 오프셋을 잡는다(T78).
        /// 종전에는 높이를 <c>H × 0.84</c> 로 박아 카드 아래턱(+✕ 반쯤 걸친 것)이 탭바 밑으로 들어가
        /// <c>autoforge</c> 의 닫기 ✕ 가 탭바에 가렸다(검수 Q 실측 · 런 127 `screen_autoforge.png`).
        /// </summary>
        public static void FitBetweenBars(float wantH, out float h, out float yOffset)
        {
            float H = UiKit.RefH;
            float top = UiKit.H("topbar_h") + Rem * 0.6f;
            float bottom = TabTop - XOverhang - Rem * 0.3f;
            h = Mathf.Min(wantH, bottom - top);
            yOffset = H * 0.5f - (top + bottom) * 0.5f;
        }

        public static void Inset(RectTransform rt, float px)
        {
            rt.offsetMin = new Vector2(px, px);
            rt.offsetMax = new Vector2(-px, -px);
        }

        /// <summary>
        /// T465 — 부모를 채우는 상자를 **위·아래로** `px` 만큼 더 내민다(좌우는 그대로).
        /// 카드의 테·면·그늘이 이것으로 카드 rect(패딩 상자) 밖에 선다 — CSS `border-box` 의 테는 `padding` 바깥이다.
        /// </summary>
        public static void GrowY(RectTransform rt, float px)
        {
            rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y - px);
            rt.offsetMax = new Vector2(rt.offsetMax.x, rt.offsetMax.y + px);
        }

        /// <summary>T473 — `GrowY` 의 사방 짝: 부모를 채우는 상자를 **네 변** 모두 `px` 만큼 밖으로 내민다(카드의 테·면·그늘 = rect(패딩 상자) + 테 두께 사방).</summary>
        public static void Grow(RectTransform rt, float px)
        {
            rt.offsetMin = new Vector2(rt.offsetMin.x - px, rt.offsetMin.y - px);
            rt.offsetMax = new Vector2(rt.offsetMax.x + px, rt.offsetMax.y + px);
        }

        // ---- 컨테이너 ----

        /// <summary>
        /// 가운데 카드(원작 .modal-card). h ≤ 0 이면 내용 높이를 따른다.
        /// <para>
        /// T465(세로)·T473(가로) — 돌려주는 rect 는 CSS 의 **패딩 상자**다: 테(`line`)·면(`face`)·그늘은 그 rect 보다 **사방** `Line3` 씩 **밖에** 선다
        /// (`Grow`). 곧 `w`·`h` 는 «패딩 + 내용» 이고 화면의 카드 몸(border-box)은 `w + 2 × Line3` × `h + 2 × Line3` 다 — 호출부가 `UiKit.Place(child, pad, pad, …)` 로 놓는
        /// `pad` 는 정본 `padding` 처럼 **테 안쪽**부터 잰다. 정본 CSS 의 `width` 나 원작 PNG 에서 잰 카드 폭·높이(border-box)를 줄 땐 `2 × Line3` 를 빼서 준다.
        /// </para>
        /// </summary>
        public static RectTransform Card(Transform parent, string name, float w, float h, string faceKey, float radius, string lineKey = "pp_line", float yOffset = 0f)
        {
            RectTransform rt = UiKit.Box(parent, name);
            UiKit.Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, yOffset), w, h > 0 ? h : 10f);
            Image hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            hit.raycastTarget = true;
            // 정본 `.modal-card`(style.css 3518) `0 .5rem 0 rgba(0,0,0,.25)` — 모든 모달 카드가 같이 쓰는 아래턱이다.
            // 딱딱한 턱이라 굽지 않고 같은 모양 한 겹을 뒤에 깔기만 한다 — 상자에 늘어붙으므로
            // 아래 `ContentSizeFitter` 로 높이가 나중에 정해지는 카드에서도 따라간다.
            UiShadow.Drop(rt, "card_lip", radius, -1f, -1f, Line3);   // T465·T473 — 그늘은 카드 몸(테 포함 · 사방)에 진다
            // T331 34회차 — 정본 8596 `.modal-card:not(.sheet):not(.pass-card)` 의 **둘째 겹**
            //   `0 1.05rem 1.6rem -.5rem rgba(0,0,0,.55)`(앰비언트 캐스트). 정본 주석: «하드 오프셋뿐이라
            //   die-cut 로 보인다 — 없애지 말고 그 뒤에 번진 캐스트를 한 겹 덧댄다».
            //   ⚑ 이 공장은 높이를 `-1`(내용이 정한다)로 받는 자리가 많아 **크기가 그 프레임엔 0** 이다 —
            //     그래서 굽는 겹은 `DropWhenSized` 로 첫 유효 크기까지 미룬다(28회차의 조용한 실패를 피한다).
            //   ⚑ 전체화면 시트는 이 공장을 안 쓰고(`PopupKit.Sheet`) 패스 카드는 제 회차에서 걷는다(결정 719).
            UiShadow.DropWhenSized(rt, "modalcard_cast", radius, Line3);
            Image line = UiKit.Rounded(rt, "line", lineKey, radius);
            Grow(line.rectTransform, Line3);                                    // T465·T473 — 테는 패딩 상자 밖(사방)
            Image face = UiKit.Rounded(rt, "face", faceKey, Mathf.Max(1f, radius - Line3));   // 면 = 테 안쪽 = rect 그대로(T473 — 가로도)
            // T178 43회차 — 정본 8286(7778 → 8116 → 8286 마지막 선언) `.modal-card:not(.sheet):not(.pass-card):not(.lgr-card)` 종이 면 겹 넷(아이보리 램프 · 위 광원 · 45° 결 · 머리 밴드 2.6rem + 1px):
            //   종이 면(pp_paper)에만 — 어두운 카드(패스 · 리그 보상 = 8837 `background-image: none`)는 제 색 키라 빠진다. 높이가 내용으로 정해지는 카드라 크기가 잡힌 뒤(두 프레임 안정) 굽는다.
            if (faceKey == "pp_paper") SurfaceArt.FillFaceWhenSized(face, "bg-grad", SurfaceArt.CardPaperLayers, UiKit.C(faceKey));
            if (h <= 0)
            {
                ContentSizeFitter f = rt.gameObject.AddComponent<ContentSizeFitter>();
                f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            return rt;
        }

        /// <summary>전체화면 시트(원작 .modal-card.sheet) — 앱 상자를 꽉 채우고 탭바는 위에 남는다.</summary>
        public static RectTransform Sheet(Transform parent, string name, string faceKey)
        {
            RectTransform rt = UiKit.Box(parent, name);
            Image bg = UiKit.Panel(rt, "bg", faceKey);
            bg.raycastTarget = true;
            // T178 43회차 — 정본 8272(7718 → 8096 → 8272 마지막 선언) `.modal-card.sheet:not(.league-sheet):not(.shop-sheet)` 종이 면 겹 여섯(아이보리 램프 · 위 광원 · −45°/45° 흰 결 둘 · 머리 밴드 3.1rem + 1px · 위 2px 림):
            //   종이 시트(pp_paper)에만 — 리그·상점(제 어두운 배경 키)은 :not() 그대로 빠진다. 시트는 앱 상자를 꽉 채워 크기가 잡힌 뒤 굽는다.
            if (faceKey == "pp_paper") SurfaceArt.FillFaceWhenSized(bg, "bg-grad", SurfaceArt.SheetPaperLayers, UiKit.C(faceKey));
            return rt;
        }

        /// <summary>
        /// 레이아웃 그룹을 달기 **전에** 이미 들어 있던 자식은 «내용» 이 아니라 **배경**이다(테·면·bg·마스크) —
        /// 이 레포의 모든 호출자가 «칸을 만들고 → 배경을 깔고 → Column/Row 를 단다» 순서로 쓴다.
        /// 그것을 레이아웃 칸으로 세면 <see cref="Image"/> 가 `ILayoutElement`(스프라이트 크기)라
        /// **판이 늘어난 배경이 아니라 빈 막대 한 줄**이 되고, 카드에는 판이 없어져 글자가 뒷화면 위에 뜬다
        /// (T28 2회차 실측: forge-detail 1.8 · craft-compare 3.1 · gear-detail 2.7 · autoforge-filter 3.7 — T57).
        /// 그래서 그 자식들은 레이아웃에서 빼 원래대로 «부모를 채우는 배경» 으로 남긴다.
        /// </summary>
        private static void MarkBackgrounds(RectTransform rt)
        {
            for (int i = 0; i < rt.childCount; i++)
            {
                GameObject go = rt.GetChild(i).gameObject;
                LayoutElement le = go.GetComponent<LayoutElement>();
                if (le == null) le = go.AddComponent<LayoutElement>();
                le.ignoreLayout = true;
            }
        }

        public static VerticalLayoutGroup Column(RectTransform rt, float pad, float gap, TextAnchor align = TextAnchor.UpperCenter, bool controlHeight = true)
        {
            MarkBackgrounds(rt);
            VerticalLayoutGroup g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            int p = Mathf.RoundToInt(pad);
            g.padding = new RectOffset(p, p, p, p);
            g.spacing = gap;
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = controlHeight;
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = false;
            return g;
        }

        public static HorizontalLayoutGroup Row(RectTransform rt, float pad, float gap, TextAnchor align = TextAnchor.MiddleLeft)
        {
            MarkBackgrounds(rt);
            HorizontalLayoutGroup g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            int p = Mathf.RoundToInt(pad);
            g.padding = new RectOffset(p, p, p, p);
            g.spacing = gap;
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = false;
            g.childForceExpandHeight = false;
            return g;
        }

        /// <summary>레이아웃 그룹 안의 칸. w &lt; 0 = 남는 폭을 차지 · h &lt; 0 = 내용 높이.</summary>
        public static RectTransform Item(Transform parent, string name, float w, float h)
        {
            RectTransform rt = UiKit.Box(parent, name);
            Size(rt, w, h);
            return rt;
        }

        public static LayoutElement Size(RectTransform rt, float w, float h)
        {
            LayoutElement le = rt.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = rt.gameObject.AddComponent<LayoutElement>();
            if (w >= 0) { le.preferredWidth = w; le.minWidth = w; le.flexibleWidth = 0f; }
            else { le.flexibleWidth = 1f; }
            if (h >= 0) { le.preferredHeight = h; le.minHeight = h; le.flexibleHeight = 0f; }
            return le;
        }

        public static RectTransform Spacer(Transform parent, float h) { return Item(parent, "spacer", -1f, h); }

        /// <summary>세로 스크롤 목록(원작 overflow-y:auto). 반환 = 내용 칸(VerticalLayoutGroup) · 호출자는 여기에 행을 쌓는다.</summary>
        public static RectTransform ScrollList(Transform parent, string name, float gap, float padX, float padY, TextAnchor align = TextAnchor.UpperCenter)
        {
            RectTransform box = UiKit.Box(parent, name);
            Image mask = box.gameObject.AddComponent<Image>();
            mask.color = new Color(0f, 0f, 0f, 0f);
            mask.raycastTarget = true;
            box.gameObject.AddComponent<RectMask2D>();
            ScrollRect sr = box.gameObject.AddComponent<ScrollRect>();
            RectTransform content = UiKit.Box(box, "content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);
            VerticalLayoutGroup g = Column(content, 0f, gap, align);
            g.padding = new RectOffset(Mathf.RoundToInt(padX), Mathf.RoundToInt(padX), Mathf.RoundToInt(padY), Mathf.RoundToInt(padY));
            ContentSizeFitter f = content.gameObject.AddComponent<ContentSizeFitter>();
            f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = content;
            sr.viewport = box;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 40f;
            return content;
        }

        // ---- 면 ----

        /// <summary>검정 테를 두른 둥근 면. 반환 = 안쪽 면.</summary>
        public static Image Outlined(Transform parent, string name, string faceKey, float radius, float line, string lineKey = "pp_line")
        {
            RectTransform rt = UiKit.Box(parent, name);
            UiKit.Rounded(rt, "line", lineKey, radius);
            Image face = UiKit.Rounded(rt, "face", faceKey, Mathf.Max(1f, radius - line));
            Inset(face.rectTransform, line);
            return face;
        }

        // ---- 글자 ----

        public static TextMeshProUGUI Label(Transform parent, string name, TextKind kind, string text, string colorKey, TextAlignmentOptions align = TextAlignmentOptions.Center, bool wrap = false, bool bold = true, float h = -1f)
        {
            TextMeshProUGUI t = UiKit.Text(parent, name, kind, text, colorKey, align);
            if (bold) t.fontStyle = FontStyles.Bold;
            if (wrap) t.textWrappingMode = TextWrappingModes.Normal;
            LayoutElement le = t.gameObject.AddComponent<LayoutElement>();
            if (h >= 0) { le.preferredHeight = h; le.minHeight = h; }
            else if (!wrap) { le.preferredHeight = t.fontSize * 1.3f; le.minHeight = le.preferredHeight; }
            return t;
        }

        /// <summary>«흰 칠 + 검정 링» 활자(원작 -webkit-text-stroke · 제목·수치).</summary>
        public static TextMeshProUGUI Ring(TextMeshProUGUI t, string lineKey = "pp_line", float width01 = 0.2f)
        {
            UiKit.Outline(t, lineKey, width01);
            return t;
        }

        /// <summary>T104 2회차 — 정본 폭표 키(<see cref="KeylineUi"/>)로 링을 두른다. 글자 크기를 정한 뒤에 부른다.</summary>
        public static TextMeshProUGUI Ring(TextMeshProUGUI t, string keylineKey, string lineKey)
        {
            UiKit.OutlinePx(t, lineKey, KeylineUi.Stroke(keylineKey, t.fontSize));
            return t;
        }

        // ---- 버튼 ----

        /// <summary>입체 버튼(원작 .btn · 면 + 아래턱 + 검정 테). w &lt; 0 = 레이아웃이 폭을 준다.
        /// 라벨 키라인(T109 7회차 · 정본 `style.css` 8719 `.btn.btn.primary/on/equip/danger/sell { -webkit-text-stroke: var(--ol2) var(--pp-line) }`):
        /// <paramref name="keylineKey"/> 가 null 이면 면 색 키 표(<see cref="KeylineUi.BtnFace"/> · `KeylineUi.json` btn_face)가 정한다 · "" 는 끈다 ·
        /// 그 밖은 폭표 키(`.af-start`·`.fi-skip` 4px 처럼 제 규칙이 있는 버튼) · 비활성은 정본 8725 `.disabled { -webkit-text-stroke: 0 }` 대로 민글자.</summary>
        public static Button Btn(Transform parent, string name, string label, string faceKey, string lipKey, UnityAction onClick, float w, float h, string inkKey = "stage_ink", TextKind kind = TextKind.Button, bool disabled = false, string keylineKey = null, float radius = -1f)
        {
            Button b = UiKit.Button(parent, name, onClick);
            RectTransform rt = b.GetComponent<RectTransform>();
            Size(rt, w, h);
            // T415 18회차 — 정본 3542 `.modal-card .btn, .panel .btn, #equip-sheet .btn { border-radius: .7rem }`: 이 공장을 부르는 자리는 전부 그 셋 안이라 기본은 표 `modal_btn_r_rem`(.7rem)이다(663 의 맨 `.btn` .55rem = catalog `btn_r` 은 이제 부르는 자리가 없다).
            float r = radius > 0f ? radius : RadiusUi.Px("modal_btn_r_rem");   // T415 13회차 — 정본이 선택자에 따로 못박은 반지름(`.fi-card .fi-skip` .6rem)은 부르는 쪽이 표값을 넘긴다 · 안 넘기면 공용 `btn_r`
            float lip = UiKit.H("btn_lip");
            UiKit.Rounded(rt, "line", "pp_line", r);
            Image lipImg = UiKit.Rounded(rt, "lip", lipKey, Mathf.Max(1f, r - Line3));
            Inset(lipImg.rectTransform, Line3);
            Image face = UiKit.Rounded(rt, "face", faceKey, Mathf.Max(1f, r - Line3));
            face.rectTransform.offsetMin = new Vector2(Line3, Line3 + lip);
            face.rectTransform.offsetMax = new Vector2(-Line3, -Line3);
            // T178 41회차 — 정본 8686 `.btn.btn.danger.danger, .btn.btn.sell.sell` 세 겹(위 1px 분홍 림 · 좌우 1px 키라인 · 46% 밴드 + 검붉은 그늘 · 표 btn_danger_*):
            //   빨간 면(pp_red = 판매 버튼)에만 · 크기는 레이아웃이 줄 수 있어 잡히는 프레임에 굽는다. 파랑(8504)은 cascade 갈래라 따로.
            if (faceKey == "pp_red") SurfaceArt.FillFaceWhenSized(face, "bg-grad", SurfaceArt.BtnDangerLayers, UiKit.C(faceKey));
            // T178 42회차 — 정본 8504(마지막 선언) `.btn.btn:not(.silver):not(.ascend-ready)` 유리 겹 셋은 **그 밖의 모든 면**(파랑·초록·회색·종이·[도전])에 얹힌다 —
            //   8770 `.modal-card .btn.btn.fi-skip.fi-skip { background: #afafaf }`(단축 · 문서 뒤)만 되돌린다(fi_skip_face).
            else if (faceKey != "fi_skip_face") SurfaceArt.FillFaceWhenSized(face, "bg-grad", SurfaceArt.BtnGlassLayers, UiKit.C(faceKey));
            TextMeshProUGUI t = UiKit.Text(rt, "label", kind, label, inkKey);
            t.fontStyle = FontStyles.Bold;
            t.rectTransform.offsetMin = new Vector2(0f, lip);
            string kl = keylineKey ?? KeylineUi.BtnFace(faceKey);
            if (!string.IsNullOrEmpty(kl) && !disabled) Ring(t, kl, "pp_line");
            // T333 — 글자 그림자(정본 cascade 대로): 색 버튼(btn_face 표 = .primary/.on/.equip/.danger/.sell)은 카드·패널 안에서 8504 `0 1px 1px rgba(4,18,52,.62)`(0-5-0)가 8719 의 none(0-4-0)을 이겨
            // 키라인 위에 남색 한 겹 · 색 버튼 비활성은 8727 none(0-5-0) · 그 밖(회색·종이·디버그 = .silver 계열)은 없음 · 비활성 회색은 8356 흰 엠보스 · 제 규칙이 none 인 버튼(소환 8661)은 표 btn_none_keylines.
            bool colored = KeylineUi.BtnFace(faceKey) != null;
            bool noneRule = !string.IsNullOrEmpty(keylineKey) && TextShadowUi.IsNoneKeyline(keylineKey);
            if (noneRule) { }
            else if (disabled && !colored) UiKit.TextShadow(t, "btn_label_disabled");
            else if (!disabled && colored) UiKit.TextShadow(t, "btn_label");
            if (disabled)
            {
                b.interactable = false;
                // T359 5회차 — 정본 `style.css` **673** `.btn.disabled { … opacity: .45 … }`. 여기 숫자로 박혀 있었다(§1).
                //   정본 `opacity` 는 **그 상자 한 겹 전체**(글자·테까지)라 CanvasGroup 한 장이 같은 뜻이다 — 값만 표로 옮긴다.
                OpacityUi.Apply(rt.gameObject, "btn_disabled");
            }
            return b;
        }

        /// <summary>카드 아래턱 가운데에 반쯤 걸치는 빨간 원 ✕(원작 .x-btn — 30장 중 25장 실측 #f2191d · 아래턱 #4a0709).</summary>
        public static Button XButton(RectTransform card, UnityAction onClick)
        {
            float x = UiKit.H("xbtn");
            float shadow = UiKit.H("xbtn_shadow");
            Button b = UiKit.Button(card, "x-btn", onClick);
            RectTransform rt = b.GetComponent<RectTransform>();
            UiKit.Anchor(rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, x * (0.5f - UiKit.L("xbtn_over")) - Line3), x, x);   // T465 — 카드 몸의 아래변은 rect 아래변보다 Line3 아래다
            rt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Image sh = UiKit.Circle(rt, "shadow", "tabx_shadow");
            UiKit.Anchor(sh.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -shadow), x, x);
            UiKit.Circle(rt, "ring", "pp_line");
            Image face = UiKit.Circle(rt, "face", "tabx_bg");
            Inset(face.rectTransform, Line3);
            Image mark = UiKit.Icon(rt, "mark", "xmark");
            mark.color = UiKit.C("tabx_ink");
            float mk = x * UiKit.L("tabx_icon");
            UiKit.Anchor(mark.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, mk, mk);
            return b;
        }

        /// <summary>빨간 둥근 ◀(원작 .league-back-btn / .sheet-back-btn).</summary>
        public static Button BackButton(Transform parent, UnityAction onClick)
        {
            float w = UiKit.H("back_w");
            float h = UiKit.H("back_h");
            Button b = UiKit.Button(parent, "back-btn", onClick);
            RectTransform rt = b.GetComponent<RectTransform>();
            Size(rt, w, h);
            // 정본 5189 `.btn.back-btn { border-radius: calc(var(--app-w) * .0094) }` — `.back-btn`(5178) 의 .6rem 과
            // `.btn.round`(3170) 의 50% 를 **뒤에 온 같은 특이도**가 이긴다(정본 주석: «종전 50%/.6rem 은 통짜 알약으로 읽혔다»).
            // 클론은 리그 뒤로 버튼 값 `Rem * 0.45f` 를 쓰고 있었다 — 앱 폭 비율이라 rem 으로 적으면 비율이 다른 화면에서 어긋난다(T345 20회차).
            float r = RadiusUi.Px("back_btn_r_w");
            UiKit.Rounded(rt, "line", "pp_line", r);
            Image lip = UiKit.Rounded(rt, "lip", "pp_red_dk", Mathf.Max(1f, r - Line3));
            Inset(lip.rectTransform, Line3);
            Image face = UiKit.Rounded(rt, "face", "pp_red", Mathf.Max(1f, r - Line3));
            face.rectTransform.offsetMin = new Vector2(Line3, Line3 + Rem * 0.36f);
            face.rectTransform.offsetMax = new Vector2(-Line3, -Line3);
            SurfaceArt.FillFaceWhenSized(face, "bg-grad", SurfaceArt.BtnDangerLayers, UiKit.C("pp_red"));   // T178 41회차 — 8686 `.btn.danger` 세 겹(뒤로 ◀ 도 .btn.danger.round)
            Image tri = Tri(rt, "tri", "stage_ink");
            float tw = UiKit.RefH * 0.0179f;
            UiKit.Anchor(tri.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, Rem * 0.18f), tw, tw);
            return b;
        }

        /// <summary>시트 좌하단(탭바 바로 위) ◀ 고정 자리.</summary>
        public static Button SheetBack(RectTransform sheet, UnityAction onClick)
        {
            Button b = BackButton(sheet, onClick);
            RectTransform rt = b.GetComponent<RectTransform>();
            rt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            float w = UiKit.H("back_w"), h = UiKit.H("back_h");
            UiKit.Place(rt, UiKit.H("back_left"), TabTop - UiKit.H("back_bottom") - h, w, h);
            return b;
        }

        /// <summary>설정 토글(원작 .settings-toggle). on 이면 파랑.</summary>
        /// <param name="onKey">켜진 트랙 면 키(기본 = 정본 3115 `.settings-toggle.on { background: var(--pp-blue) }`).</param>
        /// <param name="offKey">꺼진 트랙 면 키(기본 = 정본 3109 `.settings-toggle { background: var(--pp-gray) }`).</param>
        /// <param name="knobKey">손잡이 면 키(기본 = 정본 3113 `.settings-toggle::after { background: #fff }`).</param>
        /// <remarks>
        /// T377 12회차 — 정본은 토글을 **두 벌** 쥔다: 설정 토글(3107 · 토큰 회색/파랑 · 흰 손잡이)과 자동 제련 **필터 토글**(4759 ·
        /// **못박은** `#1e2a4a` / `#35d435` · 손잡이가 `var(--pp-blue)`). 클론은 한 벌로 그려 필터 토글이 설정 팔레트로 찍히고 있었다 —
        /// 부르는 쪽이 제 색을 줄 수 있게 열되 **기본값은 설정 토글 그대로**라 다른 자리는 한 화소도 안 움직인다.
        /// 치수(정본 1.873×1.269rem ↔ 설정 2.5×1.35rem)는 이 축이 아니다 — 그것은 표·기하 축(T378·T402)의 몫이다.
        /// </remarks>
        public static Button Toggle(Transform parent, string name, bool on, UnityAction onClick,
            string onKey = "pp_blue", string offKey = "pp_gray", string knobKey = "pp_paper")
        {
            float w = UiKit.H("settings_toggle_w"), h = UiKit.H("settings_toggle_h");
            // T454 ⓑ — 정본 3113·4992 `transition: left .15s`: 누르면 화면을 다시 세우는 자리라 «누른 순간의 상태» 를 열쇠(부모 이름/토글 이름)로 적어 두고,
            //   다시 세워질 때 그것을 가져가 손잡이를 앞 닻 → 새 닻으로 미끄러뜨린다. 표 칸은 토글 이름(없으면 `toggle`) · 값은 TransitionUi.json.
            string slideKey = parent.name + "/" + name;
            bool? before = ToggleSlide.Take(slideKey);
            Button b = UiKit.Button(parent, name, () => { ToggleSlide.Expect(slideKey, on); if (onClick != null) onClick(); });
            RectTransform rt = b.GetComponent<RectTransform>();
            Size(rt, w, h);
            UiKit.Rounded(rt, "line", "pp_line", h * 0.5f);
            // T365 10회차 — 정본 3108 `.settings-toggle { border: var(--ol2) … }` = ol2(전엔 ol1)
            Image face = UiKit.Rounded(rt, "face", on ? onKey : offKey, h * 0.5f - Line2);
            Inset(face.rectTransform, Line2);
            float k = h - Line2 * 4f;
            // T365 10회차 — 정본 3113 `.settings-toggle::after { border: var(--ol1) solid var(--pp-line) }`: 손잡이도 검정 고리(ol1) + 흰 면(전엔 흰 원 한 장)
            Image knob = UiKit.Rounded(rt, "knob", "pp_line", k * 0.5f);
            UiKit.Anchor(knob.rectTransform, new Vector2(on ? 1f : 0f, 0.5f), new Vector2(on ? 1f : 0f, 0.5f), new Vector2(on ? -Line2 * 2f : Line2 * 2f, 0f), k, k);
            Image knobFace = UiKit.Rounded(knob.transform, "face", knobKey, k * 0.5f - Line);
            Inset(knobFace.rectTransform, Line);
            // T331 43회차 — 정본 4991 `.af-toggle .knob` 의 둘째 겹 `0 .1rem .18rem rgba(0,0,0,.4)`(표 afknob_drop) — 자동 제련 토글에만(설정 토글 3113 엔 그늘이 없다).
            //   손잡이는 Image 자신(검정 고리)이라 그늘을 그 **안** 맨 뒤에 깐다: 고리(pp_line 검정) 위에 얹힌 .4 검정은 눈에 0 이고 흰 면(face)은 자식이라 그 위다 · 손잡이가 미끄러질 때 같이 간다(결정 801).
            if (name == "af-toggle") UiShadow.Drop(knob.rectTransform, "afknob_drop", k * 0.5f, k, k);
            if (before.HasValue && before.Value != on)
                ToggleSlide.Begin(knob.rectTransform, TransitionUi.Table.Has(name) ? name : "toggle", before.Value ? 1f : 0f, before.Value ? -Line2 * 2f : Line2 * 2f);
            return b;
        }

        /// <summary>
        /// T365 28회차 — 자동 제련 **필터 토글**(정본 4759~4769 `.af-toggle` · `.af-toggle .knob`)의 제 갈래. 설정 토글(<see cref="Toggle"/>)과는
        /// 색만이 아니라 **기하와 테 단이 다르다**: 트랙 1.873×1.269rem 에 **ol3** 테 · 손잡이 1.631rem 정원에 **ol3** 테 ·
        /// 손잡이가 트랙보다 **커서 위아래로 넘치고** 꺼짐엔 트랙 왼쪽 **밖**(`left: calc(-.846rem - var(--ol3))` = 바깥면 기준 −.846rem) ·
        /// 켜짐엔 `calc(1.027rem - var(--ol3))`(= 바깥면 기준 +1.027rem)에 선다(정본 주석 4747~4757 «트랙 31×21 · 노브 27 정원 · OFF −14px / ON +17px — 좌우 대칭»).
        /// 치수는 부르는 쪽이 표(ForgeAutoUi.json)에서 준다 — 도우미는 값을 안 박는다(§1). 손잡이 피벗은 가운데라 `left` 는 반지름만큼 옮겨 받는다.
        /// 미끄러짐(T454)·그늘(T331 43회차 · 결정 801)은 설정 토글과 같은 길 — 닻은 늘 왼쪽(0)이고 오프셋만 갈린다.
        /// </summary>
        /// <param name="knobD">손잡이 지름(px · 정본 1.631rem).</param>
        /// <param name="knobOffX">꺼짐 손잡이 왼끝(트랙 바깥면 기준 · px · 정본 −.846rem).</param>
        /// <param name="knobOnX">켜짐 손잡이 왼끝(트랙 바깥면 기준 · px · 정본 1.027rem).</param>
        public static Button AutoToggle(Transform parent, string name, bool on, UnityAction onClick,
            string onKey, string offKey, string knobKey, float w, float h, float knobD, float knobOffX, float knobOnX)
        {
            string slideKey = parent.name + "/" + name;
            bool? before = ToggleSlide.Take(slideKey);
            Button b = UiKit.Button(parent, name, () => { ToggleSlide.Expect(slideKey, on); if (onClick != null) onClick(); });
            RectTransform rt = b.GetComponent<RectTransform>();
            Size(rt, w, h);
            // 정본 4761 `.af-toggle { border: var(--ol3) solid var(--pp-line); border-radius: 1rem }` — 반높이(.63rem) < 1rem 이라 알약(결정 543)
            UiKit.Rounded(rt, "line", "pp_line", h * 0.5f);
            Image face = UiKit.Rounded(rt, "face", on ? onKey : offKey, h * 0.5f - Line3);
            Inset(face.rectTransform, Line3);
            // 정본 4765 `.af-toggle .knob { border: var(--ol3) solid var(--pp-line); border-radius: 50% }` — 검정 고리(ol3) + 파란 면
            Image knob = UiKit.Rounded(rt, "knob", "pp_line", knobD * 0.5f);
            float kx = (on ? knobOnX : knobOffX) + knobD * 0.5f;
            UiKit.Anchor(knob.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(kx, 0f), knobD, knobD);
            Image knobFace = UiKit.Rounded(knob.transform, "face", knobKey, knobD * 0.5f - Line3);
            Inset(knobFace.rectTransform, Line3);
            UiShadow.Drop(knob.rectTransform, "afknob_drop", knobD * 0.5f, knobD, knobD);   // T331 43회차 — 정본 4991 둘째 겹 · 손잡이 안 맨 뒤(결정 801)
            if (before.HasValue && before.Value != on)
                ToggleSlide.Begin(knob.rectTransform, TransitionUi.Table.Has(name) ? name : "toggle", 0f, (before.Value ? knobOnX : knobOffX) + knobD * 0.5f);
            return b;
        }

        // ---- 아이콘 ----

        /// <summary>아이콘 — T31 아틀라스(원작 IconGen 키) → 카탈로그 스프라이트(GUI PRO Kit) → 없으면 회색 원 자리표.</summary>
        public static Image IconOr(Transform parent, string name, string key)
        {
            bool has = UiIcons.Has(key);
            if (!has)
            {
                has = true;
                try { UiCatalog.Instance.SpriteOf(key); }
                catch (KeyNotFoundException) { has = false; }
            }
            if (has) return UiKit.Icon(parent, name, key);
            Image ph = UiKit.Circle(parent, name, "icon_missing");
            ph.raycastTarget = false;
            return ph;
        }

        /// <summary>정사각 아이콘 칸(레이아웃 안).</summary>
        public static Image IconBox(Transform parent, string name, string key, float size)
        {
            RectTransform box = Item(parent, name + "-box", size, size);
            Image i = IconOr(box, name, key);
            return i;
        }

        /// <summary>아바타 타일(흰 면 + 검정 테) + T31 `UiIcons.Avatar(emoji)` 도트 초상(아틀라스에 없으면 빈 흰 타일).</summary>
        public static RectTransform Avatar(Transform parent, string name, float size, string emoji, float radius, float line = -1f)
        {
            RectTransform rt = Item(parent, name, size, size);
            // T365 10회차 — 정본 작은 아바타(.avatar · .league-avatar 2316 · .league-challenge-avatar · .chat-avatar · .pinfo-id .avatar)는 전부 ol2,
            //   프로필 큰 아바타(.profile-avatar-big 3002)만 ol3 — 기본 Line2, 호출부가 폭을 주면 그 폭(전엔 모두 ol1).
            float ln = line > 0f ? line : Line2;
            UiKit.Rounded(rt, "line", "pp_line", radius);
            Image face = UiKit.Rounded(rt, "face", "avatar_bg", Mathf.Max(1f, radius - ln));
            Inset(face.rectTransform, ln);
            Sprite portrait = UiIcons.Avatar(emoji);
            if (portrait != null)
            {
                Image img = UiKit.Panel(rt, "portrait", "avatar_bg");
                img.sprite = portrait;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
                Inset(img.rectTransform, Line * 2f);
            }
            return rt;
        }

        private static Sprite triangle;

        /// <summary>왼쪽을 가리키는 삼각형(원작 `tri_left` 캔버스 아이콘) — 코드 생성.</summary>
        public static Sprite TriangleLeft
        {
            get
            {
                if (triangle == null)
                {
                    const int n = 48;
                    Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                    tex.wrapMode = TextureWrapMode.Clamp;
                    Color32[] px = new Color32[n * n];
                    for (int y = 0; y < n; y++)
                        for (int x = 0; x < n; x++)
                        {
                            float fx = (x + 0.5f) / n, fy = (y + 0.5f) / n;
                            float half = Mathf.Abs(fy - 0.5f) * 2f;
                            bool inside = fx >= half && fx <= 1f;
                            px[y * n + x] = new Color32(255, 255, 255, inside ? (byte)255 : (byte)0);
                        }
                    tex.SetPixels32(px);
                    tex.Apply(false, true);
                    triangle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    triangle.name = "ui-tri-left";
                }
                return triangle;
            }
        }

        public static Image Tri(Transform parent, string name, string colorKey)
        {
            if (UiIcons.Has("tri_left")) return UiKit.Icon(parent, name, "tri_left");
            Image img = UiKit.Panel(parent, name, colorKey);
            img.sprite = TriangleLeft;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            return img;
        }

        // ---- 수 표기 (원작 U.fmt · U.fmtTime) ----
        public static string Fmt(double n) { return NumFmt.Fmt(n); }
        public static string Fmt(Big b) { return NumFmt.Fmt(b); }
        public static string FmtDec(double n) { return NumFmt.FmtDec(n); }
        public static string FmtTime(double sec) { return NumFmt.FmtTime(sec); }
    }
}
