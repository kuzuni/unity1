using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 던전·기술트리·승천 화면(ROUTINE T21)이 함께 쓰는 조각 — 원작 <c>css/style.css</c> 의 .modal · .modal-card · .x-btn · .btn · .cur-pill · .league-back-btn.
    /// 치수는 카탈로그 layout(<c>…_rem</c> = 원작 rem · 분수 = 앱 폭/높이 비율) · 색은 카탈로그 colors — 코드에 숫자·색을 두지 않는다(§1).
    /// T22 의 공용 모달 층(<c>Ui/Popups.cs</c>)이 서면 이 조각을 그리로 흡수해도 된다(결정 기록).
    /// </summary>
    public static class DungeonPopups
    {
        public enum Skin { Blue, Red, Silver, Gray, DgdSilver }

        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        /// <summary>원작 rem → 기준 px(1rem = 앱높이/844×16 · 카탈로그 rem_h).</summary>
        public static float Rem(float rem) { return rem * UiKit.L("rem_h") * UiKit.RefH; }
        /// <summary>카탈로그의 rem 값(키 …_rem) → 기준 px.</summary>
        public static float RemL(string key) { return Rem(UiKit.L(key)); }
        public static float Line3 { get { return UiKit.L("line3_px"); } }
        public static float Line2 { get { return UiKit.L("line2_px"); } }
        public static float Kind(TextKind k) { return UiCatalog.Instance.Kind(k).size; }
        /// <summary>글자 한 줄 상자 높이(줄 간격 1.25).</summary>
        public static float LineH(TextKind k) { return Kind(k) * 1.25f; }

        // ---- 층 ----

        /// <summary>딤 오버레이 — 앱 상자 전체(탭바까지 · 원작 .modal 은 #app 직속 풀스크린). 뒤 화면 클릭을 막는다.</summary>
        public static RectTransform Overlay(string name)
        {
            RectTransform rt = UiKit.Box(UiRoot.Instance.App, name);
            Image dim = UiKit.Panel(rt, "dim", "modal_dim");
            dim.color = UiKit.PerceivedDim(dim.color);   // T94 — 공용 Popups.Show 와 같은 환산(결정 191)
            dim.raycastTarget = true;
            return rt;
        }

        /// <summary>가운데 카드(흰 종이 · 검정 테 line3 · 둥근 모서리). w·h 는 기준 px.</summary>
        public static RectTransform Card(RectTransform overlay, string name, float w, float h, float radiusPx, string bgKey = "pp_paper")
        {
            RectTransform card = UiKit.Box(overlay, name);
            UiKit.Anchor(card, Center, Center, Vector2.zero, w, h);
            Bordered(card, "bg", bgKey, radiusPx, Line3);
            return card;
        }

        /// <summary>테두리 있는 둥근 면 — 바깥(테 색) 위에 안쪽(바탕)을 테 두께만큼 들여 얹는다. 안쪽을 돌려준다.</summary>
        public static RectTransform Bordered(RectTransform parent, string name, string bgKey, float radiusPx, float borderPx, string borderKey = "pp_line")
        {
            Image outer = UiKit.Rounded(parent, name, borderKey, radiusPx);
            Image inner = UiKit.Rounded(outer.rectTransform, "face", bgKey, Mathf.Max(0f, radiusPx - borderPx));
            inner.rectTransform.offsetMin = new Vector2(borderPx, borderPx);
            inner.rectTransform.offsetMax = new Vector2(-borderPx, -borderPx);
            return inner.rectTransform;
        }

        /// <summary>테두리 있는 원판(바깥 테 + 안쪽 면). 안쪽을 돌려준다.</summary>
        public static RectTransform BorderedCircle(RectTransform parent, string name, string bgKey, float borderPx, string borderKey = "pp_line")
        {
            Image outer = UiKit.Circle(parent, name, borderKey);
            Image inner = UiKit.Circle(outer.rectTransform, "face", bgKey);
            inner.rectTransform.offsetMin = new Vector2(borderPx, borderPx);
            inner.rectTransform.offsetMax = new Vector2(-borderPx, -borderPx);
            return inner.rectTransform;
        }

        /// <summary>아래쪽 눌림 그림자 띠(원작 inset 0 -Nrem 0 색) — 둥근 면의 바닥에 붙는다.</summary>
        public static Image BottomShade(RectTransform face, string colorKey, float px, float radiusPx)
        {
            Image s = UiKit.Rounded(face, "shade", colorKey, radiusPx);
            RectTransform rt = s.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, px);
            return s;
        }

        // ---- 글자 ----

        /// <summary>줄바꿈하는 글자(원작 문단). 상자 안에서 자동 줄바꿈 · 넘치면 잘리지 않고 흘러나온다.</summary>
        public static TextMeshProUGUI Para(Transform parent, string name, TextKind kind, string text, string colorKey, TextAlignmentOptions align)
        {
            TextMeshProUGUI t = UiKit.Text(parent, name, kind, text, colorKey, align);
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        public static TextMeshProUGUI Bold(Transform parent, string name, TextKind kind, string text, string colorKey, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI t = UiKit.Text(parent, name, kind, text, colorKey, align);
            t.fontStyle = FontStyles.Bold;
            return t;
        }

        // ---- 버튼 ----

        /// <summary>원작 .modal-card .btn(둥근 알약 · 검정 테 · 아래 그림자 띠 · 굵은 글자). 배치는 호출자가 돌려받은 RectTransform 에 한다.</summary>
        public static Button Pill(RectTransform parent, string name, string label, Skin skin, TextKind kind, Action onClick, float radiusPx = -1f, bool interactable = true)
        {
            string bg, dk, ink;
            switch (skin)
            {
                case Skin.Blue: bg = "pp_blue"; dk = "pp_blue_dk"; ink = "white"; break;
                case Skin.Red: bg = "pp_red"; dk = "pp_red_dk"; ink = "white"; break;
                case Skin.Silver: bg = "silver"; dk = "silver_dk"; ink = "pp_ink"; break;
                case Skin.DgdSilver: bg = "dgd_btn"; dk = "dgd_btn_dk"; ink = "white"; break;
                default: bg = "pp_gray"; dk = "pp_gray_dk"; ink = "btn_disabled_ink"; break;
            }
            if (radiusPx < 0f) radiusPx = RemL("btn_radius_rem");
            RectTransform rt = UiKit.Box(parent, name);
            RectTransform face = Bordered(rt, "bg", bg, radiusPx, Line3);
            BottomShade(face, dk, RemL("btn_shadow_rem"), Mathf.Max(0f, radiusPx - Line3));
            TextMeshProUGUI t = Bold(rt, "label", kind, label, ink);
            t.rectTransform.offsetMax = new Vector2(0f, -RemL("btn_shadow_rem") * 0.5f);
            // T109 11회차 — 정본 style.css 5363 `.dgd-btn.silver { -webkit-text-stroke: 2px var(--pp-line) }`(흰 칠 + 검정 링 · 회색 잠김 알약은 규칙 없음).
            if (skin == Skin.DgdSilver) PopupKit.Ring(t, "dgd_btn_silver", "pp_line");
            Button b = UiKit.Button(rt, "hit", onClick == null ? null : (UnityAction)(() => onClick()));
            b.interactable = interactable;
            return b;
        }

        /// <summary>버튼(Pill) 의 뿌리 RectTransform — 배치용.</summary>
        public static RectTransform Root(Button b) { return b.transform.parent as RectTransform; }

        /// <summary>원작 .league-back-btn.sheet-back-btn — 시트 왼쪽 아래 빨간 ◀ (2.1×1.75rem · 아래 그림자 .36rem).</summary>
        /// <param name="radiusKey">모서리 반지름을 덮는 표(<see cref="RadiusUi"/>) 키 — 정본이 그 화면에만 따로 준 값이 있을 때만 준다
        /// (T345 7회차: 정본 2255 `.panel .btn.tech-tree-back` .6rem 이 공용 뒤로 버튼을 덮는다). null 이면 종전대로 catalog `back_radius_rem`.</param>
        /// <summary>
        /// T401 2회차 — **치수도 반지름 키를 따라간다**: 정본 2255 `.panel .btn.tech-tree-back` 은 리그 뒤로 버튼(2.1×1.75rem)과 달리 제 규칙으로
        /// `width: 2.5rem; height: 2.5rem` 을 따로 준다. 부르는 쪽(`TechPanel.cs`)은 지금 남의 산 lock 이라 인수를 더 받을 수 없어,
        /// **이미 넘어오는 반지름 키의 앞자리**(`tech_back_r_rem` → `tech_back_w_rem`·`tech_back_h_rem`)를 함께 본다 — 둘 다 표에 있을 때만 쓰고
        /// 없으면 종전 기본값이다(다른 부르는 쪽은 한 글자도 안 달라진다).
        /// </summary>
        static bool SizeKeysFor(string radiusKey, out float w, out float h)
        {
            w = h = 0f;
            if (radiusKey == null || !radiusKey.EndsWith("_r_rem")) return false;
            string stem = radiusKey.Substring(0, radiusKey.Length - "_r_rem".Length);
            string wk = stem + "_w_rem", hk = stem + "_h_rem";
            if (!UiCatalog.Instance.HasLayout(wk) || !UiCatalog.Instance.HasLayout(hk)) return false;
            w = RemL(wk); h = RemL(hk);
            return true;
        }

        public static Button BackButton(RectTransform parent, Action onClick, string radiusKey = null)
        {
            float w, h;
            if (!SizeKeysFor(radiusKey, out w, out h)) { w = RemL("back_w_rem"); h = RemL("back_h_rem"); }
            RectTransform rt = UiKit.Box(parent, "back-btn");
            UiKit.Anchor(rt, Vector2.zero, Vector2.zero, new Vector2(RemL("back_left_rem"), RemL("back_bottom_rem")), w, h);
            float r = radiusKey != null ? RadiusUi.Px(radiusKey) : RemL("back_radius_rem");
            RectTransform face = Bordered(rt, "bg", "pp_red", r, Line3);
            BottomShade(face, "pp_red_dk", RemL("back_shadow_rem"), Mathf.Max(0f, r - Line3));
            Image ico = UiKit.Icon(rt, "ico", "tri_left");
            float d = UiKit.L("back_icon") * UiKit.RefH;
            UiKit.Anchor(ico.rectTransform, Center, Center, new Vector2(0f, RemL("back_shadow_rem") * 0.5f), d, d);
            return UiKit.Button(rt, "hit", () => onClick());
        }

        /// <summary>원작 .x-btn — 카드 아래 가운데에 반쯤 걸치는 빨간 원 ✕ (3.4rem · margin-top −1.7rem).</summary>
        public static Button XButton(RectTransform card, Action onClick)
        {
            float d = RemL("x_btn_rem");
            RectTransform rt = UiKit.Box(card, "x-btn");
            UiKit.Anchor(rt, new Vector2(0.5f, 0f), Center, Vector2.zero, d, d);
            BorderedCircle(rt, "bg", "x_btn", Line3);
            Image mark = UiKit.Icon(rt, "mark", "xmark");
            mark.color = UiKit.C("white");
            float mk = d * UiKit.L("x_icon");
            UiKit.Anchor(mark.rectTransform, Center, Center, Vector2.zero, mk, mk);
            return UiKit.Button(rt, "hit", () => onClick());
        }

        /// <summary>원작 .tri-btn — 파란 ◀ ▶ (아이콘만 · 히트는 조금 넓게).</summary>
        public static Button TriButton(RectTransform parent, string name, bool left, float d, Action onClick)
        {
            RectTransform rt = UiKit.Box(parent, name);
            string key = left ? "tri_left" : "tri_right";
            Image ico = UiKit.Icon(rt, "ico", key, TintOf(key));
            UiKit.Anchor(ico.rectTransform, Center, Center, Vector2.zero, d, d);
            return UiKit.Button(rt, "hit", () => onClick());
        }

        /// <summary>아틀라스에 «이름|틴트» 변형이 있으면 그 틴트(원작 TRI_BLUE 처럼 UI 가 실제로 부르는 색) — 없으면 null(원색).</summary>
        public static string TintOf(string name)
        {
            string prefix = name + "|";
            foreach (string k in UiIcons.Keys)
                if (k.StartsWith(prefix, StringComparison.Ordinal)) return k.Substring(prefix.Length);
            return null;
        }

        /// <summary>원작 .fi-info-btn — 검정 원 안 흰 소문자 i (1.5rem).</summary>
        public static Button InfoButton(RectTransform parent, Action onClick)
        {
            float d = RemL("info_btn_rem");
            RectTransform rt = UiKit.Box(parent, "info-btn");
            UiKit.Anchor(rt, Vector2.one, Vector2.one, new Vector2(-RemL("info_right_rem"), -RemL("info_top_rem")), d, d);
            UiKit.Circle(rt, "bg", "info_btn");
            // T331 30회차 — 정본 8174 `.info-btn` 의 **둘째 겹** `0 .1rem .16rem rgba(0,0,0,.38)`(첫 겹은 안쪽 림라이트).
            //   ⚑ 이 버튼은 클론에 **공장이 둘**이다 — 여기(던전·기술 판)와 `ForgeUi.InfoButton`(대장간·장비 시트).
            //     자의 `NEED` 가 그 둘을 세므로 한 곳만 걸린 동안은 «절반만 섰다» 로 남는다.
            //   ⚠ 이 줄을 `"info-btn"` 과 `Circle(` **사이**에 넣지 마라 — T345 의 자가 «원 공장이 곁 6줄 안» 을 본다.
            UiShadow.Drop(rt, "infobtn_drop", d * 0.5f);
            Bold(rt, "glyph", TextKind.Sub, "i", "white");
            return UiKit.Button(rt, "hit", () => onClick());
        }

        // ---- 재화 알약 ----

        /// <summary>원작 .cur-pill — 검정 테 알약(색 바탕) + 아이콘 + 흰 굵은 수. 폭은 pill_w_rem(고정) · 높이는 글자+패딩.</summary>
        public static RectTransform CurPill(RectTransform parent, string name, string iconKey, string bgKey, string text, out TextMeshProUGUI label)
        {
            float h = LineH(TextKind.Sub) + RemL("pill_pad_y_rem") * 2f;
            float w = RemL("pill_w_rem");
            RectTransform rt = UiKit.Box(parent, name);
            rt.sizeDelta = new Vector2(w, h);
            Bordered(rt, "bg", bgKey, RemL("pill_radius_rem"), Line3);
            float ico = RemL("pill_icon_rem");
            float padX = RemL("pill_pad_x_rem");
            Image img = UiKit.Icon(rt, "ico", iconKey);
            UiKit.Place(img.rectTransform, padX, (h - ico) * 0.5f, ico, ico);
            label = Bold(rt, "num", TextKind.Sub, text, "white", TextAlignmentOptions.Left);
            WrapUi.Apply(label, "cur_pill");   // T361 4회차 — 정본 white-space 표(WrapUi.json) 3985 `.cur-pill { nowrap }`
            UiKit.Place(label.rectTransform, padX + ico + padX * 0.4f, 0f, w - padX * 2f - ico, h);
            return rt;
        }

        /// <summary>줄 상자(가로 선) — 부모 폭 전체 · 두께 px · 위에서 yTop.</summary>
        public static Image HLine(RectTransform parent, string name, string colorKey, float yTop, float px)
        {
            Image img = UiKit.Panel(parent, name, colorKey);
            UiKit.Place(img.rectTransform, 0f, yTop, parent.rect.width > 0f ? parent.rect.width : parent.sizeDelta.x, px);
            return img;
        }

        /// <summary>토스트 한 줄(원작 UI.toast — T22 의 공용 토스트가 서기 전까지 여기 것).</summary>
        public static void Toast(string text) { DungeonToast.Show(text); }
    }

    /// <summary>토스트(앱 상자 위쪽 · 2.2초 뒤 사라짐). 원작 #toasts 의 가장 단순한 꼴 — T22 가 공용 토스트를 세우면 그쪽으로.</summary>
    public sealed class DungeonToast : MonoBehaviour
    {
        static DungeonToast instance;
        RectTransform row;
        RectTransform box;
        Coroutine hide;

        public static string Last { get; private set; }

        public static void Show(string text)
        {
            Last = text;
            if (UiRoot.Instance == null) return;
            if (instance == null)
            {
                RectTransform rt = UiKit.Box(UiRoot.Instance.App, "toast");
                float w = UiKit.L("toast_w") * UiKit.RefW;
                float h = DungeonPopups.RemL("toast_h_rem");
                UiKit.Anchor(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -UiKit.L("toast_top") * UiKit.RefH), w, h);
                instance = rt.gameObject.AddComponent<DungeonToast>();
                instance.box = rt;
                DungeonPopups.Bordered(rt, "bg", "pp_ink", h * 0.5f, DungeonPopups.Line2, "pp_line");
            }
            instance.transform.SetAsLastSibling();
            instance.Paint(text);
            instance.box.gameObject.SetActive(true);
            if (instance.hide != null) instance.StopCoroutine(instance.hide);
            instance.hide = instance.StartCoroutine(instance.HideLater());
        }

        /// <summary>문구를 다시 그린다 — 아이콘 표(`TOAST_ICON`)의 이모지는 T31 아이콘으로 선다(T89 `UiKit.IconTextRow`).
        /// ⚠ 여기가 «아이콘 길» 이다: 예전에는 `Bold(...)` 로 글자만 세워 ⭐·🔒·🧪·💎 가 화면에서 두부(□)였다(T107 실측).
        /// 줄은 조각이 여럿이라 한 번 만들고 `.text` 만 바꿀 수 없다 — 부를 때마다 지우고 다시 세운다.</summary>
        void Paint(string text)
        {
            if (row != null) { row.gameObject.SetActive(false); Destroy(row.gameObject); }   // Destroy 는 프레임 끝이라 먼저 끈다
            row = UiKit.IconTextRow(box, "text", TextKind.Sub, text, "white");
            foreach (TextMeshProUGUI piece in UiKit.RowTexts(row)) piece.fontStyle = FontStyles.Bold;
        }

        IEnumerator HideLater()
        {
            yield return new WaitForSeconds(2.2f);
            box.gameObject.SetActive(false);
            hide = null;
        }
    }
}
