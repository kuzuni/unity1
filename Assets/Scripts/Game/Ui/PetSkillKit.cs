using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Data;
using Forge.Game.Gallery;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T20 펫·스킬·소환 UI 의 표(<c>Assets/Forge/Resources/PetSkillUi.json</c> — 정본 CSS 실측 색·배치·ui.js 문구). 코드에 숫자·색·문구를 박지 않는다(§1).
    /// T22 가 <c>catalog.json</c> 을 쥐고 있어 T20 몫은 이 파일이 든다(PROGRESS 결정 기록) — T33 이 합칠 수 있다.
    /// </summary>
    public static class PetSkillStyle
    {
        public const string ResourcePath = "PetSkillUi";
        static JsonObject root, colors, layout, text;
        static readonly Dictionary<string, Color> colorCache = new Dictionary<string, Color>();

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T20)");
            root = MiniJson.ParseObject(ta.text);
            colors = J.Obj(root["colors"]);
            layout = J.Obj(root["layout"]);
            text = J.Obj(root["text"]);
        }

        public static void Reset() { root = null; colorCache.Clear(); }

        public static Color C(string key)
        {
            Load();
            Color c;
            if (colorCache.TryGetValue(key, out c)) return c;
            string hex = J.Str(colors[key]);
            if (hex == null) throw new KeyNotFoundException("PetSkillUi.json 에 색 «" + key + "» 이 없다");
            if (!ColorUtility.TryParseHtmlString(hex, out c)) throw new FormatException("색 «" + key + "» 의 값 «" + hex + "» 을 못 읽는다");
            colorCache[key] = c;
            return c;
        }

        /// <summary>배치 값 원문(분수·rem·px). 접미로 단위를 안다.</summary>
        public static float L(string key)
        {
            Load();
            object v = layout[key];
            if (!J.IsNum(v)) throw new KeyNotFoundException("PetSkillUi.json 에 배치 값 «" + key + "» 이 없다");
            return (float)J.Num(v);
        }

        public static float RefW { get { return UiKit.RefW; } }
        public static float RefH { get { return UiKit.RefH; } }
        /// <summary>1rem(원작 main.js fitLayout: 앱높이/844×16) 의 기준 px.</summary>
        public static float RemPx { get { return L("rem_h") * RefH; } }
        /// <summary>키 접미(_w · _h · _rem · _px · _f)에 맞춰 기준 px 로.</summary>
        public static float Px(string key)
        {
            float v = L(key);
            if (key.EndsWith("_w")) return v * RefW;
            if (key.EndsWith("_h")) return v * RefH;
            if (key.EndsWith("_rem")) return v * RemPx;
            return v;
        }
        public static float Rem(float rem) { return rem * RemPx; }

        public static string T(string key)
        {
            Load();
            string s = J.Str(text[key]);
            if (s == null) throw new KeyNotFoundException("PetSkillUi.json 에 문구 «" + key + "» 이 없다");
            return s;
        }
        public static string T(string key, params object[] args) { return string.Format(T(key), args); }

        /// <summary>정본 `RARITY_CSS[rarity]` — 등급색은 표(gamedata.json)에서.</summary>
        public static Color Rarity(GameDefs defs, string rarity)
        {
            string hex = defs.RarityCss.Get(rarity, null);
            Color c;
            if (hex == null || !ColorUtility.TryParseHtmlString(hex, out c)) return C("muted");
            return c;
        }
        public static string RarityHex(GameDefs defs, string rarity) { return defs.RarityCss.Get(rarity, null); }

        /// <summary>CSS `color-mix(in srgb, rc 60%, #fff)` — 펫 타일 얼굴 바탕.</summary>
        public static Color Mix(Color a, Color b, float aFrac) { return Color.Lerp(b, a, aFrac); }

        /// <summary>원작 `U.fmt` — Big 표기(T3 NumFmt).</summary>
        public static string Fmt(Big b) { return NumFmt.Fmt(b); }
        public static string Fmt(double v) { return NumFmt.Fmt(Big.Of(v)); }

        /// <summary>원작 `U.fmtTime(sec)` — «N일 N시» · «N시 N분» · «N분 N초» · «N초».</summary>
        public static string FmtTime(double sec)
        {
            sec = Math.Max(0, Math.Ceiling(sec));
            int d = (int)Math.Floor(sec / 86400), h = (int)Math.Floor(sec % 86400 / 3600), m = (int)Math.Floor(sec % 3600 / 60), s = (int)(sec % 60);
            if (d > 0) return T("time_d", d, h);
            if (h > 0) return T("time_h", h, m);
            if (m > 0) return T("time_m", m, s);
            return T("time_s", s);
        }

        /// <summary>원작 `U.subText(s)` — «+10% 치명타 확률»(쿨감은 −).</summary>
        public static string SubText(Forge.Core.Pets.Substat s)
        {
            return T("sub_text", s.Key == "skillCd" ? "-" : "+", JsNum.ToString(s.Value), s.Label);
        }
    }

    /// <summary>
    /// T20 공용 조각 — 원작 CSS 의 알약(cur-pill) · 종이 버튼(btn primary/danger/silver) · 검정 테 둥근 면 · 외곽선 글자.
    /// 글자는 전부 <see cref="UiKit.Text"/>(TextKind 하한) · 색은 카탈로그/표에서만.
    /// </summary>
    public static class PetSkillKit
    {
        public static float Rem(float v) { return PetSkillStyle.Rem(v); }
        public static float Line3 { get { return PetSkillStyle.L("line3_px"); } }
        public static float Line2 { get { return PetSkillStyle.L("line2_px"); } }

        /// <summary>검정 키라인(원작 --ol3) + 안쪽 색 면의 둥근 상자. 반환 = 안쪽 면(자식은 여기에).</summary>
        public static RectTransform Framed(Transform parent, string name, Color fill, float radiusPx, float linePx)
        {
            RectTransform box = UiKit.Box(parent, name);
            Image outline = UiKit.Rounded(box, "line", "pp_line", radiusPx);
            outline.color = UiKit.C("pp_line");
            Image face = UiKit.Rounded(box, "face", "pp_paper", Mathf.Max(1f, radiusPx - linePx));
            face.color = fill;
            face.rectTransform.offsetMin = new Vector2(linePx, linePx);
            face.rectTransform.offsetMax = new Vector2(-linePx, -linePx);
            return box;
        }

        /// <summary>둥근 단색 면(Image 색을 직접 준다 — 등급색처럼 표에서 오는 색).</summary>
        public static Image Fill(Transform parent, string name, Color c, float radiusPx)
        {
            Image img = UiKit.Rounded(parent, name, "pp_paper", radiusPx);
            img.color = c;
            return img;
        }

        public static Image Disc(Transform parent, string name, Color c)
        {
            Image img = UiKit.Circle(parent, name, "pp_paper");
            img.color = c;
            return img;
        }

        /// <summary>검정 테 원판(sk-orb): 바깥 검정 원 + 안쪽 등급색 원.</summary>
        public static RectTransform Orb(Transform parent, string name, Color fill, float linePx)
        {
            RectTransform box = UiKit.Box(parent, name);
            UiKit.Circle(box, "line", "pp_line");
            Image face = Disc(box, "face", fill);
            face.rectTransform.offsetMin = new Vector2(linePx, linePx);
            face.rectTransform.offsetMax = new Vector2(-linePx, -linePx);
            return box;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, TextKind kind, string text, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = true)
        {
            TextMeshProUGUI t = UiKit.Text(parent, name, kind, text, null, align);
            t.color = color;
            if (bold) t.fontStyle = FontStyles.Bold;
            return t;
        }

        /// <summary>흰 글자 + 검정 외곽선(원작 -webkit-text-stroke · paint-order stroke fill).</summary>
        public static TextMeshProUGUI Stroked(Transform parent, string name, TextKind kind, string text, Color color, float width01, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI t = Text(parent, name, kind, text, color, align);
            UiKit.Outline(t, "pp_line", width01);
            return t;
        }

        /// <summary>원작 `.cur-pill` — 검정 테 알약 + 아이콘 + 흰 굵은 글자(외곽선).</summary>
        public static RectTransform Pill(Transform parent, string name, Color bg, string iconKey, string label, float h, float x, float y, bool rightAnchor = false, float widthPx = 0f)
        {
            float rem = PetSkillStyle.RemPx;
            float pad = PetSkillStyle.Px("pill_pad_rem");
            float ico = PetSkillStyle.Px("pill_icon_rem");
            float w = widthPx > 0f ? widthPx : pad * 2f + ico + Rem(0.3f) + Mathf.Max(Rem(1.6f), label.Length * UiCatalog.Instance.Kind(TextKind.Sub).size * 0.62f);
            RectTransform pill = Framed(parent, name, bg, h * 0.5f, Line3);
            if (rightAnchor) UiKit.Anchor(pill, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-x, y), w, h);
            else UiKit.Anchor(pill, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x, y), w, h);
            Image img = UiKit.Icon(pill, "ico", iconKey);
            UiKit.Place(img.rectTransform, pad, (h - ico) * 0.5f, ico, ico);
            TextMeshProUGUI t = Stroked(pill, "label", TextKind.Sub, label, PetSkillStyle.C("white"), 0.3f, TextAlignmentOptions.Left);
            UiKit.Place(t.rectTransform, pad + ico + Rem(0.3f), 0f, w - pad * 2f - ico, h);
            return pill;
        }

        public enum BtnKind { Gray, Primary, Danger, Silver, Ascend }

        /// <summary>원작 종이 버튼(`.panel .btn` / `.modal-card .btn`): 검정 테 · 둥근 · 아래 안쪽 그늘(inset shadow) · 굵은 글자. 라벨은 Button 종류, 부제(small)는 Sub.</summary>
        /// <param name="labelKind">라벨 글자 종류. 정본이 «좁은 버튼에서는 글자를 줄인다» 고 적은 자리(예 `.petup-selrow .btn.silver` = .78rem)는
        /// 우리 하한(§1 버튼 44)에 안 들어가므로 **한 단계 작은 종류**(Sub 36)를 준다 — 그래도 정본보다 크므로 폭은 부르는 쪽이 늘린다.</param>
        /// <param name="letterSpacingEm">정본 `letter-spacing`(em) — TMP `characterSpacing` 은 1/100 em 단위다.</param>
        public static Button PaperButton(Transform parent, string name, BtnKind kind, string label, string sub, bool disabled, UnityAction onClick, float radiusPx = -1f,
                                         TextKind labelKind = TextKind.Button, float letterSpacingEm = 0f)
        {
            float r = radiusPx > 0f ? radiusPx : PetSkillStyle.Px("btn_r_rem");
            float inset = PetSkillStyle.Px("btn_inset_rem");
            Color bg, dk, ink;
            switch (kind)
            {
                case BtnKind.Primary: bg = PetSkillStyle.C("pp_blue"); dk = PetSkillStyle.C("pp_blue_dk"); ink = PetSkillStyle.C("white"); break;
                case BtnKind.Danger: bg = PetSkillStyle.C("pp_red"); dk = PetSkillStyle.C("pp_red_dk"); ink = PetSkillStyle.C("white"); break;
                case BtnKind.Silver: bg = PetSkillStyle.C("silver"); dk = PetSkillStyle.C("silver_dk"); ink = PetSkillStyle.C("white"); break;
                case BtnKind.Ascend: bg = PetSkillStyle.C("ascend_a"); dk = PetSkillStyle.C("ascend_b"); ink = PetSkillStyle.C("white"); break;
                default: bg = PetSkillStyle.C("silver"); dk = PetSkillStyle.C("silver_dk"); ink = PetSkillStyle.C("ink"); break;
            }
            if (disabled)
            {
                bg = PetSkillStyle.C(kind == BtnKind.Gray || kind == BtnKind.Silver ? "silver_disabled" : "silver_disabled");
                ink = PetSkillStyle.C("disabled_ink");
            }
            Button b = UiKit.Button(parent, name, onClick);
            RectTransform rt = b.GetComponent<RectTransform>();
            RectTransform box = Framed(rt, "skin", dk, r, Line3);
            Image face = Fill(box, "top", bg, Mathf.Max(1f, r - Line3));
            face.rectTransform.offsetMin = new Vector2(Line3, Line3 + inset);
            face.rectTransform.offsetMax = new Vector2(-Line3, -Line3);
            face.raycastTarget = false;
            bool two = !string.IsNullOrEmpty(sub);
            TextMeshProUGUI lt = disabled || kind == BtnKind.Gray
                ? Text(rt, "label", labelKind, label, ink)
                : Stroked(rt, "label", labelKind, label, ink, kind == BtnKind.Silver ? 0.35f : 0.25f);
            if (letterSpacingEm != 0f) lt.characterSpacing = letterSpacingEm * 100f;   // TMP 는 1/100 em
            lt.textWrappingMode = TextWrappingModes.NoWrap;   // 정본 `.btn { white-space: nowrap }`
            if (two)
            {
                UiKit.Band(lt.rectTransform, 0.06f, 0.56f);
                TextMeshProUGUI st = disabled ? Text(rt, "sub", TextKind.Sub, sub, ink) : Stroked(rt, "sub", TextKind.Sub, sub, ink, 0.25f);
                UiKit.Band(st.rectTransform, 0.5f, 0.94f);
            }
            else UiKit.Band(lt.rectTransform, 0.04f, 0.9f);
            b.interactable = !disabled;
            return b;
        }

        /// <summary>원작 `.sk-lv` — 검정 알약 위 흰 굵은 글자(Lv.N).</summary>
        public static RectTransform LvBadge(Transform parent, string text, float w, float h)
        {
            RectTransform box = UiKit.Box(parent, "lv");
            Fill(box, "bg", PetSkillStyle.C("ink"), h * 0.5f);
            TextMeshProUGUI t = Text(box, "t", TextKind.Sub, text, PetSkillStyle.C("white"));
            UiKit.Fill(t.rectTransform);
            box.sizeDelta = new Vector2(w, h);
            return box;
        }

        /// <summary>글자 폭 가늠(원작 nowrap 알약 폭을 흉내) — TMP 가 아직 레이아웃 전이라 글자 수로 잰다.</summary>
        public static float TextWidth(TextKind kind, string s)
        {
            float size = UiCatalog.Instance.Kind(kind).size;
            float w = 0f;
            foreach (char ch in s) w += ch > 0x2E80 ? size * 1.0f : (ch == ' ' ? size * 0.3f : size * 0.58f);
            return w;
        }

        /// <summary>원작 `.summon-gauge` / `.rates-prog` / `.petup-xpbar` — 어두운 홈에 파란 채움 + 가운데 흰 글자.</summary>
        public static RectTransform Gauge(Transform parent, string name, float w, float h, float ratio, string label, Color bg, float radiusPx, float linePx, TextKind kind)
        {
            RectTransform box = Framed(parent, name, bg, radiusPx, linePx);
            box.sizeDelta = new Vector2(w, h);
            RectTransform face = (RectTransform)box.Find("face");
            Image fill = Fill(face, "fill", PetSkillStyle.C("pp_blue"), Mathf.Max(1f, radiusPx - linePx));
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            fill.type = Image.Type.Sliced;
            TextMeshProUGUI t = Stroked(box, "t", kind, label, PetSkillStyle.C("white"), 0.3f);
            UiKit.Fill(t.rectTransform);
            return box;
        }

        /// <summary>세로 스크롤 상자(원작 .grid-scroll · overflow-y auto). 반환 = 내용 RectTransform(위 기준 · 높이는 호출자가 준다).</summary>
        public static RectTransform Scroll(Transform parent, string name, out ScrollRect scroll)
        {
            RectTransform view = UiKit.Box(parent, name);
            view.gameObject.AddComponent<RectMask2D>();
            Image hit = view.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            hit.raycastTarget = true;
            RectTransform content = UiKit.Box(view, "content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 10f);
            scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            return content;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        /// <summary>펫 얼굴 — 3D 썸네일(원작 Scene3D.petThumb 파이프라인 · <see cref="PetFaces"/>)이 있으면 그것, 없으면 정본 PET_ICONS 이모지 글자(원작 폴백).</summary>
        public static RectTransform PetFace(Transform parent, GameDefs defs, string name, float size) { return PetFace(parent, defs, name, size, GalleryKind.Pets); }

        /// <summary>종 얼굴(펫·탈것) — 3D 썸네일이 없으면(헤드리스) 정본 PET_ICONS/MOUNT_ICONS 이모지.</summary>
        public static RectTransform PetFace(Transform parent, GameDefs defs, string name, float size, GalleryKind kind)
        {
            RectTransform box = UiKit.Box(parent, "face");
            box.sizeDelta = new Vector2(size, size);
            Sprite sp = PetFaces.Get(name, kind);
            if (sp != null)
            {
                Image img = box.gameObject.AddComponent<Image>();
                img.sprite = sp;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }
            else
            {
                string emoji = kind == GalleryKind.Mounts ? defs.MountIcons.Get(name, "🐴") : defs.PetIcons.Get(name, "🐾");
                TextMeshProUGUI t = Text(box, "emoji", TextKind.Title, emoji, PetSkillStyle.C("ink"));
                UiKit.Fill(t.rectTransform);
            }
            return box;
        }
    }
}
