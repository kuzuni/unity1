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
        private readonly List<Popup> open = new List<Popup>();

        /// <summary>팝업이 열리거나 닫혀 «어느 popup 탭이 ✕ 여야 하는가» 가 바뀌었다(인자 = 탭 키 또는 null).</summary>
        public event Action<string> TabXChanged;

        public int OpenCount { get { return open.Count; } }
        public IReadOnlyList<Popup> Open { get { return open; } }

        public static PopupLayer Create(UiRoot root)
        {
            if (Instance != null && Instance.under != null && Instance.under.parent == root.App) return Instance;
            RectTransform app = root.App;
            RectTransform under = UiKit.Box(app, "modals");
            under.SetSiblingIndex(root.TabBand.GetSiblingIndex());
            RectTransform over = UiKit.Box(app, "modals-over");
            RectTransform toasts = UiKit.Box(app, "toasts");
            PopupLayer layer = under.gameObject.AddComponent<PopupLayer>();
            layer.under = under;
            layer.over = over;
            layer.toasts = toasts;
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
            dim.raycastTarget = true;
            open.Add(p);
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
            RectTransform card = PopupKit.Card(p.Root, "card", UiKit.L("modal_card_w") * UiKit.RefW, -1f, "pp_paper", UiKit.H("card_r"));
            PopupKit.Column(card, UiKit.H("card_pad"), PopupKit.Rem * 0.45f);
            PopupKit.Label(card, "title", TextKind.Title, title, "pp_ink");
            PopupKit.Label(card, "desc", TextKind.Body, desc, "pp_muted", TextAlignmentOptions.Center, true);
            PopupKit.Label(card, "soon", TextKind.Body, "다음 업데이트에서 추가될 예정입니다.", "pp_muted", TextAlignmentOptions.Center, true);
            PopupKit.Btn(card, "close", "닫기", "pp_gray", "pp_gray_dk", () => Hide(p), -1f, UiKit.H("btn_h"), "pp_ink");
            return p;
        }

        // ---- 토스트 (원작 toast · 2.6초) ----

        public void Toast(string msg)
        {
            float w = UiKit.L("toast_w") * UiKit.RefW;
            float h = UiKit.H("toast_h");
            RectTransform t = UiKit.Box(toasts, "toast");
            UiKit.Anchor(t, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, UiKit.H("toast_bottom") + toastStack * h * 1.2f), w, h);
            UiKit.Rounded(t, "line", "toast_line", h * 0.5f);
            Image face = UiKit.Rounded(t, "bg", "toast_bg", h * 0.5f - PopupKit.Line);
            PopupKit.Inset(face.rectTransform, PopupKit.Line);
            TextMeshProUGUI txt = UiKit.Text(t, "msg", TextKind.Sub, msg, "ink");
            txt.fontStyle = FontStyles.Bold;
            toastStack++;
            LastToast = msg;
            StartCoroutine(ToastLife(t.gameObject));
        }

        /// <summary>마지막 토스트 문구(테스트가 본다).</summary>
        public string LastToast { get; private set; }
        private int toastStack;

        private IEnumerator ToastLife(GameObject go)
        {
            yield return new WaitForSecondsRealtime(2.6f);
            toastStack = Mathf.Max(0, toastStack - 1);
            if (go != null) Destroy(go);
        }
    }

    /// <summary>팝업 조각 공장 — 카드·시트·버튼·◀·✕·토글·목록. 치수는 카탈로그(`layout`) · 색은 카탈로그(`colors`) 에서만 읽는다(§1).</summary>
    public static class PopupKit
    {
        public static float Rem { get { return UiKit.H("rem_h"); } }
        /// <summary>글자 종류의 크기(카탈로그 textKinds) — 행 높이를 글자에 맞출 때.</summary>
        public static float FontSize(TextKind k) { return UiCatalog.Instance.Kind(k).size; }
        public static float Line { get { return UiKit.L("line_px"); } }
        public static float Line3 { get { return UiKit.L("line3_px"); } }
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

        // ---- 컨테이너 ----

        /// <summary>가운데 카드(원작 .modal-card). h ≤ 0 이면 내용 높이를 따른다.</summary>
        public static RectTransform Card(Transform parent, string name, float w, float h, string faceKey, float radius, string lineKey = "pp_line", float yOffset = 0f)
        {
            RectTransform rt = UiKit.Box(parent, name);
            UiKit.Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, yOffset), w, h > 0 ? h : 10f);
            Image hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            hit.raycastTarget = true;
            UiKit.Rounded(rt, "line", lineKey, radius);
            Image face = UiKit.Rounded(rt, "face", faceKey, Mathf.Max(1f, radius - Line3));
            Inset(face.rectTransform, Line3);
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

        // ---- 버튼 ----

        /// <summary>입체 버튼(원작 .btn · 면 + 아래턱 + 검정 테). w &lt; 0 = 레이아웃이 폭을 준다.</summary>
        public static Button Btn(Transform parent, string name, string label, string faceKey, string lipKey, UnityAction onClick, float w, float h, string inkKey = "stage_ink", TextKind kind = TextKind.Button, bool disabled = false)
        {
            Button b = UiKit.Button(parent, name, onClick);
            RectTransform rt = b.GetComponent<RectTransform>();
            Size(rt, w, h);
            float r = UiKit.H("btn_r");
            float lip = UiKit.H("btn_lip");
            UiKit.Rounded(rt, "line", "pp_line", r);
            Image lipImg = UiKit.Rounded(rt, "lip", lipKey, Mathf.Max(1f, r - Line3));
            Inset(lipImg.rectTransform, Line3);
            Image face = UiKit.Rounded(rt, "face", faceKey, Mathf.Max(1f, r - Line3));
            face.rectTransform.offsetMin = new Vector2(Line3, Line3 + lip);
            face.rectTransform.offsetMax = new Vector2(-Line3, -Line3);
            TextMeshProUGUI t = UiKit.Text(rt, "label", kind, label, inkKey);
            t.fontStyle = FontStyles.Bold;
            t.rectTransform.offsetMin = new Vector2(0f, lip);
            if (disabled)
            {
                b.interactable = false;
                CanvasGroup cg = rt.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0.45f;
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
            UiKit.Anchor(rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, x * (0.5f - UiKit.L("xbtn_over"))), x, x);
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
            float r = Rem * 0.45f;
            UiKit.Rounded(rt, "line", "pp_line", r);
            Image lip = UiKit.Rounded(rt, "lip", "pp_red_dk", Mathf.Max(1f, r - Line3));
            Inset(lip.rectTransform, Line3);
            Image face = UiKit.Rounded(rt, "face", "pp_red", Mathf.Max(1f, r - Line3));
            face.rectTransform.offsetMin = new Vector2(Line3, Line3 + Rem * 0.36f);
            face.rectTransform.offsetMax = new Vector2(-Line3, -Line3);
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
        public static Button Toggle(Transform parent, string name, bool on, UnityAction onClick)
        {
            float w = UiKit.H("settings_toggle_w"), h = UiKit.H("settings_toggle_h");
            Button b = UiKit.Button(parent, name, onClick);
            RectTransform rt = b.GetComponent<RectTransform>();
            Size(rt, w, h);
            UiKit.Rounded(rt, "line", "pp_line", h * 0.5f);
            Image face = UiKit.Rounded(rt, "face", on ? "pp_blue" : "pp_gray", h * 0.5f - Line);
            Inset(face.rectTransform, Line);
            float k = h - Line * 4f;
            Image knob = UiKit.Circle(rt, "knob", "pp_paper");
            UiKit.Anchor(knob.rectTransform, new Vector2(on ? 1f : 0f, 0.5f), new Vector2(on ? 1f : 0f, 0.5f), new Vector2(on ? -Line * 2f : Line * 2f, 0f), k, k);
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
        public static RectTransform Avatar(Transform parent, string name, float size, string emoji, float radius)
        {
            RectTransform rt = Item(parent, name, size, size);
            UiKit.Rounded(rt, "line", "pp_line", radius);
            Image face = UiKit.Rounded(rt, "face", "avatar_bg", Mathf.Max(1f, radius - Line));
            Inset(face.rectTransform, Line);
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
