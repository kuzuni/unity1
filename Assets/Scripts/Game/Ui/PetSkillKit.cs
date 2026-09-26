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

        /// <summary>
        /// 장착 줄 작은 아이콘 원판(정본 **4150** `.sk-mini { background: radial-gradient(circle at 34% 26%, 흰 .55, 투명 46%), radial-gradient(circle at 50% 120%, 검 .42, 투명 62%), var(--rc) }`)의
        /// 두 겹을 <see cref="Framed"/>(펫 · 둥근 네모) 또는 <see cref="Orb"/>(스킬 · 원)의 안쪽 면 위에 한 판으로 굽는다 — 면 = 아이콘 − 테, 바탕 = 등급색. CSS `circle` 의 기본 크기(farthest-corner)는 표가 반지름으로 미리 셈해 뒀다(결정 834). T178 46회차.
        /// </summary>
        public static void MiniPlate(RectTransform box, Color rc, float mini, float linePx)
        {
            Image inner = box.Find("face").GetComponent<Image>();
            SurfaceArt.FillFace(inner, "bg-grad", null, SurfaceArt.SkMiniLayers, rc, mini - linePx * 2f, mini - linePx * 2f);
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
            // T352 13회차 — **이 공장은 `UiKit.Text` 위에 선다.** 그 공장의 기본이 이 회차에 bold 로 뒤집혔으므로
            //   `bold: false` 는 이제 «안 걸었다» 가 아니라 «**걷어낸다**» 여야 한다 — 안 그러면 `bold: false` 로 부른
            //   열여섯 자리(`empty`·`muted`·`hint` 류 · 정본 `.muted` 657 이 400 을 주는 갈래)가 전부 굵어진다.
            //   뒤집기 전에는 두 꼴이 같은 그림이었지만(기본이 regular 였으니) 뒤집은 뒤에는 다르다.
            t.fontStyle = bold ? (t.fontStyle | FontStyles.Bold) : (t.fontStyle & ~FontStyles.Bold);
            return t;
        }

        /// <summary>흰 글자 + 검정 외곽선(원작 -webkit-text-stroke · paint-order stroke fill).</summary>
        public static TextMeshProUGUI Stroked(Transform parent, string name, TextKind kind, string text, Color color, float width01, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI t = Text(parent, name, kind, text, color, align);
            UiKit.Outline(t, "pp_line", width01);
            return t;
        }

        /// <summary>T104 2회차 — 같은 활자를 정본 폭표 키(<see cref="KeylineUi"/> · px 또는 em)로. 글자 크기를 정한 뒤 그 순간의 fontSize 로 환산한다(<see cref="UiKit.OutlinePx"/>).</summary>
        public static TextMeshProUGUI Stroked(Transform parent, string name, TextKind kind, string text, Color color, string keylineKey, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI t = Text(parent, name, kind, text, color, align);
            UiKit.OutlinePx(t, "pp_line", KeylineUi.Stroke(keylineKey, t.fontSize));
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
            TextMeshProUGUI t = Stroked(pill, "label", TextKind.Sub, label, PetSkillStyle.C("white"), "cur_pill", TextAlignmentOptions.Left);   // 정본 .cur-pill 3px
            UiKit.Place(t.rectTransform, pad + ico + Rem(0.3f), 0f, w - pad * 2f - ico, h);
            return pill;
        }

        public enum BtnKind { Gray, Primary, Danger, Silver, Ascend }

        /// <summary>원작 종이 버튼(`.panel .btn` / `.modal-card .btn`): 검정 테 · 둥근 · 아래 안쪽 그늘(inset shadow) · 굵은 글자. 라벨은 Button 종류, 부제(small)는 Sub.</summary>
        /// <param name="labelKind">라벨 글자 종류. 정본이 «좁은 버튼에서는 글자를 줄인다» 고 적은 자리(예 `.petup-selrow .btn.silver` = .78rem)는
        /// 우리 하한(§1 버튼 44)에 안 들어가므로 **한 단계 작은 종류**(Sub 36)를 준다 — 그래도 정본보다 크므로 폭은 부르는 쪽이 늘린다.</param>
        /// <param name="letterSpacingEm">정본 `letter-spacing`(em) — TMP `characterSpacing` 은 1/100 em 단위다.</param>
        public static Button PaperButton(Transform parent, string name, BtnKind kind, string label, string sub, bool disabled, UnityAction onClick, float radiusPx = -1f,
                                         TextKind labelKind = TextKind.Button, float letterSpacingEm = 0f, string keylineKey = null, bool plainFace = false)
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
                // T396 5회차 — 비활성 글자는 둘이다: 공용 `.btn.*.disabled`(정본 8725 #7b7b7b · disabled_ink) 와 **은색** 버튼
                //   `.skd-btn.silver.disabled`(5266) · `.petup-selrow .btn.silver.disabled`(5497) 의 #6f6f6f(disabled_ink2 · 한 톤 어둡다).
                //   전엔 종류와 무관하게 disabled_ink 였다 — 표에 disabled_ink2 가 있었지만 쓰는 곳이 0 이었다.
                ink = PetSkillStyle.C(kind == BtnKind.Silver ? "disabled_ink2" : "disabled_ink");
            }
            Button b = UiKit.Button(parent, name, onClick);
            RectTransform rt = b.GetComponent<RectTransform>();
            RectTransform box = Framed(rt, "skin", dk, r, Line3);
            Image face = Fill(box, "top", bg, Mathf.Max(1f, r - Line3));
            face.rectTransform.offsetMin = new Vector2(Line3, Line3 + inset);
            face.rectTransform.offsetMax = new Vector2(-Line3, -Line3);
            face.raycastTarget = false;
            // T178 37회차 — 정본 5209 `.summon-btn` · 5260 `.skd-btn.silver` · 5274 `.btn.silver` `background: linear-gradient(180deg, #e3e3e3, #c2c2c2)` · 5268 `.skd-btn.silver.disabled`(#d9d9d9 → #bdbdbd) ·
            //   5641 `.summon-bar .btn.big.ascend-ready`(#4caf50 → #2e7d32): 면은 단색이 아니라 **위 밝고 아래 짙은 세로 램프**다(불투명 두 정지점 · 바탕 없이 FillMasked · 아래턱은 face 밖의 skin 띠라 그대로).
            //   `.petup-selrow .btn.silver`(5487)는 단색 #a3a3a3 로 이 겹을 덮으니 그 자리(PetUpgradePopup)는 `plainFace` 로 뺀다 · 파랑·빨강(Primary/Danger)은 7833~8504 cascade 갈래라 다음 회차.
            //   180deg 순수 세로 램프에 정지점이 분수라 판은 상자 크기와 무관하다 — 정사각 한 장을 늘린다(버튼 크기는 부르는 쪽이 뒤에 준다).
            // T178 42회차 — 정본 **8661** `.btn.btn.summon-btn.summon-btn:not(.ascend-ready) { background: #a3a3a3 }`(0-4-0 · 문서 뒤)이 5209 의 은색 램프를 **단색으로 끈다**
            //   (정본 주석 «원본 30장 census: 회색 버튼 면 163,163,163 · 턱 50,49,50 — 소환 버튼과 같은 언어»). 37회차가 세운 소환 버튼 램프는 걷는다 — 승천(ascend-ready)은 그대로.
            //   선택자 글자가 달라(`.summon-btn` ↔ `.btn.btn.summon-btn.summon-btn:not(…)`) 자(check_surface_gradients)의 cascade-off 검출이 못 본 자리다.
            bool summonGray = name == "summon-btn" && kind == BtnKind.Silver;
            string surface = plainFace || summonGray ? null : kind == BtnKind.Silver ? (disabled ? "btn_silver_disabled" : "btn_silver") : kind == BtnKind.Ascend ? "btn_ascend" : null;
            if (surface != null) SurfaceArt.FillMasked(face, "bg-grad", surface, 1f, 1f);
            // T178 42회차 — 정본 **8504**(7833→8204→8336→8504 마지막 선언) `.btn.btn:not(.silver):not(.ascend-ready)` 유리 겹 셋(위 1px 하늘색 림 · 좌우 1px 키라인 · 46% 하드 스톱 밴드 + 남색 그늘):
            //   파랑(Primary)·회색(Gray) 종이 버튼 — 은색·승천은 :not() 으로 빠지고 빨강은 8686, 소환 회색은 8661 이 덮는다. 1px 겹이라 크기가 잡힌 뒤 굽는다.
            else if (!plainFace && !summonGray && (kind == BtnKind.Primary || kind == BtnKind.Gray)) SurfaceArt.FillFaceWhenSized(face, "bg-grad", SurfaceArt.BtnGlassLayers, bg);
            // T178 41회차 — 정본 8686 `.btn.btn.danger.danger` 세 겹(펫 상세 [해제] · 8699 `.petd-wrap .btn.danger` 는 색·턱만 덮는다) — 1px 림·키라인이라 크기가 잡히는 프레임에 굽는다.
            else if (!plainFace && kind == BtnKind.Danger) SurfaceArt.FillFaceWhenSized(face, "bg-grad", SurfaceArt.BtnDangerLayers, bg);
            bool two = !string.IsNullOrEmpty(sub);
            TextMeshProUGUI lt = disabled || kind == BtnKind.Gray
                ? Text(rt, "label", labelKind, label, ink)
                : Stroked(rt, "label", labelKind, label, ink, keylineKey ?? (kind == BtnKind.Silver ? "petup_btn_silver" : "petd_btn"));   // 정본 .petup-selrow .btn.silver 2px · .petd-wrap .petd-btn 3px(결정 268)
            if (letterSpacingEm != 0f) lt.characterSpacing = letterSpacingEm * 100f;   // TMP 는 1/100 em
            lt.textWrappingMode = TextWrappingModes.NoWrap;   // 정본 `.btn { white-space: nowrap }`
            if (kind == BtnKind.Silver) WrapUi.Apply(lt, "petup_selrow_btn_silver");   // T361 7회차 — 정본 white-space 표(WrapUi.json) 4363 `.petup-selrow .btn.silver { nowrap }`
            if (two)
            {
                UiKit.Band(lt.rectTransform, 0.06f, 0.56f);
                TextMeshProUGUI st = disabled ? Text(rt, "sub", TextKind.Sub, sub, ink) : Stroked(rt, "sub", TextKind.Sub, sub, ink, keylineKey ?? (kind == BtnKind.Silver ? "petup_btn_silver" : "petd_btn"));
                UiKit.Band(st.rectTransform, 0.5f, 0.94f);
            }
            else UiKit.Band(lt.rectTransform, 0.04f, 0.9f);
            b.interactable = !disabled;
            return b;
        }

        /// <summary>원작 `.sk-lv` — 검정 알약 위 흰 굵은 글자(Lv.N).</summary>
        public static RectTransform LvBadge(Transform parent, string text, float w, float h) { return LvBadge(parent, text, w, h, null); }

        /// <summary>
        /// 알약 배지. <paramref name="keylineKey"/> 를 주면 **판은 그대로 두고 글자에만** 정본 키라인을 건다 —
        /// 정본 `.petd-wrap .petd-tile .sk-lv`(style.css 5467)가 그 꼴이다(기본 `.sk-lv` 4045 의 검정 알약 위에
        /// 흰 칠 + 2.5px 링). 글자 이름도 그때는 `sk-lv` 로 둔다 — 정본 클래스 이름이고 `check_keyline`(T109)이
        /// «그 자리가 있는가» 를 그 이름으로 본다.
        /// </summary>
        public static RectTransform LvBadge(Transform parent, string text, float w, float h, string keylineKey)
        {
            RectTransform box = UiKit.Box(parent, "lv");
            Fill(box, "bg", PetSkillStyle.C("ink"), h * 0.5f);
            TextMeshProUGUI t;
            if (keylineKey == null)
            {
                t = Text(box, "t", TextKind.Sub, text, PetSkillStyle.C("white"));
            }
            else
            {
                // 이름은 정본 클래스 그대로 `sk-lv` — 자(`tools/check_keyline.py`)가 «그 자리가 있는가» 를 이 이름으로 본다.
                // 같은 클래스 안이지만 `PetSkillKit.` 을 붙여 부른다: 그 자는 «<받는이>.Text(…, "이름"» 꼴로 자리를 찾는다.
                t = PetSkillKit.Text(box, "sk-lv", TextKind.Sub, text, PetSkillStyle.C("white"));
                UiKit.OutlinePx(t, "pp_line", KeylineUi.Stroke(keylineKey, t.fontSize));
            }
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
            // T178 39회차 — 정본 **8776~8820 «aaa-skin ⓖ 게이지 축»**(사용자 확정 화풍 ㉯ 플랫/매트 · ㉳ 분절 블록)이 7992 트랙 홈·7953 채움 광택을
            //   `background-image: none` 으로 **끈다**(마지막 선언이 이긴다) — 38회차가 세운 두 겹을 걷었다. 트랙·채움은 색 한 칸(플랫 매트)이 정본이다.
            // T178 40회차 — 같은 블록 ⑴ **8798** 하드 키라인(`inset 0 0 0 ol1 검 .55` · 표 keylines.gauge)은 `.summon-gauge` 에만(petup-xpbar·rates-prog 는 그 선택자에 없다) ·
            //   inset 그림자는 자식(채움) 아래라 채움보다 먼저 세운다. ⑶ **8812** 분절 눈금(표 stripes.gauge_seg · 피치 .62rem · 틈 ol2 · 검 .58)은
            //   summon-gauge·petup-xpbar·rates-prog 셋(sk-shard 는 목록에 없다) · 채움 위(z 2) · 글자(t · box 의 뒤 형제)는 그 위(z 3).
            float faceW = Mathf.Max(1f, w - linePx * 2f), faceH = Mathf.Max(1f, h - linePx * 2f);
            if (name == "summon-gauge") SurfaceArt.Keyline(face, "keyline", "gauge", faceW, faceH, Mathf.Max(1f, radiusPx - linePx), UiKit.L("line_px"));   // ol1 은 catalog 토큰(PetSkillUi 엔 line2·line3 만 있다)
            Image fill = Fill(face, "fill", PetSkillStyle.C("pp_blue"), Mathf.Max(1f, radiusPx - linePx));
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            fill.type = Image.Type.Sliced;
            if (name == "summon-gauge" || name == "petup-xpbar" || name == "rates-prog") SurfaceArt.SegTicks(face, "seg-ticks", "gauge_seg", Line2, faceH);
            TextMeshProUGUI t = Stroked(box, "t", kind, label, PetSkillStyle.C("white"), "gauge_span");   // 정본 .rates-prog span · .petup-xpbar span 2.5px
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
                // T332 5회차 — 정본 `.mt-face.has-thumb > img { filter: drop-shadow(0 2px 2px rgba(0,0,0,.35)) }`(style.css 7604):
                // **3D 스냅샷으로 바뀐 뒤에만** 접지 그림자가 진다(이모지 폴백엔 안 건다 — `.has-thumb` 가 그 가름이고, 여기선 `sp != null` 이 같은 뜻이다).
                // 값은 장비 칸과 같은 `cell` 키다(굽기가 아니라 UI 층에서 거는 까닭은 결정 529).
                ForgeUi.ThumbShadow(img, "cell");
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
