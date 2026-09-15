using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.Forging;
using Forge.Core.Gear;
using Forge.Core.Pets;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 대장간·장비 화면 공용 조각(ROUTINE T19 · 원작 ui.js `ageHex`·`ageIcon`·`ageStars`·`weaponEmoji`·`emptySlotFace`·`itemImgHTML`·`itemCardHTML`·`fi-age-bar`·`U.subText`·`U.subRangeText`).
    /// 시대색은 `gamedata.json` AGE_COLORS 에서 · 아이콘은 T31 아틀라스 키(`age_*`·`wpn_*`·`slot_*`) · 3D 썸네일은 T37 몫이라 아이콘으로 선다.
    /// </summary>
    public static class ForgeUi
    {
        public static readonly Color AgeFallback = new Color(0.5f, 0.5f, 0.5f, 1f);
        static readonly Color Ink = new Color(0x17 / 255f, 0x18 / 255f, 0x1a / 255f, 1f);

        // ---- 시대 ----

        public static Color AgeColor(GameDefs d, string age)
        {
            int hex;
            if (age == null || d.AgeColors == null || !d.AgeColors.TryGet(age, out hex))
            {
                Debug.LogError("[ForgeUi.AgeColor] 시대표에 없는 시대 키다(손상 세이브?): " + age);
                return AgeFallback;
            }
            return HexColor(hex);
        }

        /// <summary>0xRRGGBB → 유니티 sRGB 색(결정 4).</summary>
        public static Color HexColor(int hex) { return new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f, 1f); }

        public static string AgeKr(GameDefs d, string age) { return d.AgeKr != null && d.AgeKr.Has(age) ? d.AgeKr[age] : age; }
        public static string AgeIconKey(string age) { return "age_" + age; }

        public static Color Mix(Color a, Color b, float bAmount) { return Color.Lerp(a, b, bAmount); }
        /// <summary>장비 칸 면 — 정본 828 `.equip-cell { background: color-mix(in srgb, var(--rc) 58%, #17181a) }`. T371 6회차: 비율·상대색을 표 `ColorMixUi.json` `cell_face` 에서(전엔 `.42`·`Ink` 가 코드에 박혀 있었다 · 값은 같다).</summary>
        public static Color CellFace(Color age) { return ColorMixUi.Mix("cell_face", age); }
        /// <summary>장비 칸 테 — 정본 828 `border: … color-mix(in srgb, var(--rc) 80%, #000)`. T371 6회차: 표 `cell_line`(전엔 `.2` 박힘).</summary>
        public static Color CellLine(Color age) { return ColorMixUi.Mix("cell_line", age); }
        /// <summary>확률 정보 «다음 %» 세그먼트 — 막대색의 어두운 변주.</summary>
        public static Color Darker(Color c, float k = 0.72f) { return new Color(c.r * k, c.g * k, c.b * k, 1f); }

        /// <summary>흰 카드 위 시대색 잉크(원작 `inkRarity`) — 밝은 시대색은 4.5:1 까지 어둡힌다.</summary>
        public static Color InkOf(Color c)
        {
            float lum = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
            if (lum < 0.35f) return c;
            float k = 0.35f / lum;
            return new Color(c.r * k, c.g * k, c.b * k, 1f);
        }

        /// <summary>승천 별(원작 `ageStars`): 0 = '' · n≤foldAt = ★×n · 그 위는 ★n.</summary>
        public static string Stars(int n, int foldAt = 5)
        {
            if (n <= 0) return string.Empty;
            if (n <= foldAt) return new string('★', n);
            return "★" + n.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        // ---- 아이템 ----

        public static string ShapeOf(GameDefs d, string wtype)
        {
            WeaponType w = d.WeaponTypes != null ? d.WeaponTypes.Get(wtype, null) : null;
            return w != null && !string.IsNullOrEmpty(w.Shape) ? w.Shape : wtype;
        }

        /// <summary>무기 모양 아이콘 키(원작 WEAPON_SHAPE_ICON · hammer 는 재화 해머 재사용).</summary>
        public static string WeaponIconKey(GameDefs d, string wtype)
        {
            string shape = ShapeOf(d, wtype);
            if (shape == "hammer") return "hammer";
            string key = "wpn_" + shape;
            return UiIcons.Has(key) ? key : "wp_mystery";
        }

        public static string SlotIconKey(string slot) { return "slot_" + slot; }

        public static string ItemIconKey(GameDefs d, ForgeItem it)
        {
            if (it.Slot == "weapon" && !string.IsNullOrEmpty(it.WType)) return WeaponIconKey(d, it.WType);
            return SlotIconKey(it.Slot);
        }

        public static string SubText(Substat s)
        {
            return (s.Key == "skillCd" ? "-" : "+") + JsNum.ToString(s.Value) + "% " + s.Label;
        }

        public static string SubRangeText(GameDefs d, string key, double max)
        {
            string min = JsNum.ToString(d.SubstatMin);
            return key == "skillCd" ? "-" + min + "% - " + JsNum.ToString(max) + "%" : "+" + min + "% - " + JsNum.ToString(max) + "%";
        }

        public static string StatLabel(string main) { return main == "atk" ? "피해" : "체력"; }

        // ---- 조각 ----

        /// <summary>둥근 테 + 면(동적 색).</summary>
        public static Image Tile(Transform parent, string name, Color face, Color line, float radius, float lineW)
        {
            RectTransform rt = UiKit.Box(parent, name);
            Image l = UiKit.Rounded(rt, "line", "pp_line", radius);
            l.color = line;
            Image f = UiKit.Rounded(rt, "face", "pp_paper", Mathf.Max(1f, radius - lineW));
            f.color = face;
            PopupKit.Inset(f.rectTransform, lineW);
            return f;
        }

        /// <summary>
        /// T382 — 잉크 비율을 «안 주면 표에서» 받는 표식. C# 기본 인수는 **상수만** 되므로 숫자를 못 넣는다(§1 «수치는 코드에 박지 않는다»).
        /// 그래서 기본은 음수 표식이고, 실제 값은 <see cref="InkFrac"/> 가 `ItemFacesUi.json` `thumb_ink_f`(정본 `ui.js` 3145 `THUMB_INK: 0.76`)에서 읽는다.
        /// </summary>
        public const float InkFromTable = -1f;

        /// <summary>잉크 비율을 풀어 준다 — 음수(<see cref="InkFromTable"/>)면 표값, 아니면 호출자가 준 값(정본이 그 자리만 달리 주는 곳: 목록 `.fl-face` .8).</summary>
        public static float InkFrac(float inkFrac) { return inkFrac < 0f ? ItemFacesStyle.L("thumb_ink_f") : inkFrac; }

        /// <summary>장비 아이콘 타일(시대색 프레임 + 아이콘 · 잉크 비율은 표 `thumb_ink_f`). 반환 = 타일 루트.</summary>
        public static RectTransform ItemTile(Transform parent, string name, float size, GameDefs d, string age, string iconKey, float inkFrac = InkFromTable, bool agePattern = false)
        {
            Color ac = AgeColor(d, age);
            RectTransform rt = UiKit.Box(parent, name);
            rt.sizeDelta = new Vector2(size, size);
            Tile(rt, "frame", CellFace(ac), CellLine(ac), size * 0.16f, PopupKit.Line3);
            // T124 — 정본 `.fl-face.equip-cell[data-age]` 만 시대 무늬를 입는다(제작 카드·상세 머리 아이콘은 equip-cell 이 아니다) → 호출자가 켠다
            if (agePattern) AgePattern.Attach(rt, age, cell: true, mask: (string)null, siblingIndex: 1);   // 목록 타일은 셀이라 마스크가 없다(T380 키 갈래)
            Image ico = PopupKit.IconOr(rt, "img", iconKey);
            float k = size * InkFrac(inkFrac);
            UiKit.Anchor(ico.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, k, k);
            return rt;
        }

        /// <summary>
        /// T122 — 정본 `itemImgHTML(item)`: `Scene3D.itemThumb(item)` 이 있으면 3D 썸네일 <img>(타일 100% · object-fit contain), 없으면 슬롯 플레이스홀더.
        /// 동기 호출도 정본 그대로(비교·상세 카드는 한두 장 · 키 단위 캐시). 목록처럼 많은 칸은 <see cref="ItemFaces.Request"/> 로 프레임마다 받아 <see cref="ApplyThumb"/> 로 갈아 끼운다.
        /// </summary>
        public static RectTransform ItemTile(Transform parent, string name, float size, GameDefs d, ForgeItem it, float inkFrac = InkFromTable, bool agePattern = false)
        {
            RectTransform rt = ItemTile(parent, name, size, d, it.Age, ItemIconKey(d, it), inkFrac, agePattern);
            ApplyThumb(rt, ItemFaces.Get(d, it), size);
            return rt;
        }

        /// <summary>
        /// 구운 썸네일이 있으면 타일의 `img` 를 그것으로 갈아 끼운다(정본 hydrate · `.fl-face img` 100%). null 이면 실루엣 그대로(false).
        /// <paramref name="shadowKey"/> 는 접지 그림자 종류(<see cref="ThumbShadow"/>) — 기본 `cell` 이 정본의 다수값이고, 예외 둘만 호출자가 바꾼다.
        /// </summary>
        public static bool ApplyThumb(RectTransform tile, Sprite thumb, float size, string shadowKey = "cell")
        {
            if (tile == null || thumb == null) return false;
            Transform t = tile.Find("img");
            Image img = t != null ? t.GetComponent<Image>() : null;
            if (img == null) return false;
            img.sprite = thumb;
            img.color = Color.white;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            float k = size * ItemFacesStyle.L("img_frac");
            UiKit.Anchor(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, k, k);
            ThumbShadow(img, shadowKey);
            return true;
        }

        /// <summary>
        /// T332 3회차 — 정본의 **접지 그림자**를 UI 층에서 건다. 굽기(텍스처)로는 못 한다(결정 529): 정본 `pad 1.10` 이 남기는 아래 여백은
        /// `drop-shadow(0 **2px** …)` 오프셋 하나에 이미 다 쓰이고, CSS 필터는 `img` 상자 **밖으로** 나가지만 텍스처는 못 나간다 — 구우면 아래가 일자로 끊긴다.
        /// `UnityEngine.UI.Shadow` 는 그래픽 메시를 그대로 한 번 더 오프셋해 그리므로 스프라이트 알파를 그대로 따라간다 = `drop-shadow(0 dy 0 c)`.
        /// ⚠ **흐림은 근사로 뺐다** — `Shadow` 에 흐림 손잡이가 없다(정본 1.5~2 CSS px · 결정 520 의 «흐림은 근사» 와 같은 태도). 자리마다 다른 것은 알파다.
        /// 키(정본 `style.css`):
        /// `cell` = 장비 칸 `.equip-cell:not(.empty) .cell-img`(7672) · 펫·탈것 `.mt-face.has-thumb > img`(7604) · 상세·비교 카드 `.adc-img`(1081·1147·1876) — `0 2px 2px rgba(0,0,0,.35)`
        /// `list` = 목록 `.fl-face img`(763~770) — `0 2px 1.5px rgba(0,0,0,.28)`
        /// 빈 키 = 장비 상세 머리 `.idet-icon img`(3668) — 아웃라인만 걸고 **접지 그림자는 없다**.
        /// </summary>
        public static void ThumbShadow(Image img, string key)
        {
            if (img == null) return;
            Shadow sh = null;
            foreach (Shadow c in img.GetComponents<Shadow>()) if (c.GetType() == typeof(Shadow)) { sh = c; break; }   // Outline 도 Shadow 를 잇는다 — 그것은 건드리지 않는다
            if (string.IsNullOrEmpty(key))
            {
                if (sh != null) sh.enabled = false;
                return;
            }
            Color c2 = ItemFacesStyle.C("gs_ink");
            c2.a = ItemFacesStyle.L("gs_" + key + "_a");
            ImageShadow(img, 0f, ItemFacesStyle.L("gs_dy_css_px"), c2);
        }

        /// <summary>
        /// 그림(`Image`)에 정본 `drop-shadow(dx dy 0 color)` 한 겹 — `UnityEngine.UI.Shadow` 는 그래픽 메시를 그대로 오프셋해 한 번 더 그리므로 스프라이트 알파를 그대로 따라간다.
        /// <paramref name="dxCssPx"/>·<paramref name="dyCssPx"/> 는 **정본 CSS px** 다(캔버스 환산은 <see cref="KeylineUi.CssPx"/> · 결정 222) — CSS 의 «아래로» 는 유니티 UI 에서 −y.
        /// ⚠ **글자에는 안 듣는다**: `Shadow` 는 `IMeshModifier` 인데 TMP 는 그 길을 안 탄다 — 글자는 `UiKit.TextShadow`(TMP 언더레이 · T333)를 쓴다(T332 7회차).
        /// 흐림은 못 낸다(`Shadow` 에 손잡이가 없다) — 정본이 흐림 0 인 자리(별 둘)와 오프셋이 비정수라 저절로 번지는 자리(썸네일 접지)에만 쓴다.
        /// </summary>
        public static void ImageShadow(Image img, float dxCssPx, float dyCssPx, Color color)
        {
            if (img == null) return;
            Shadow sh = null;
            foreach (Shadow c in img.GetComponents<Shadow>()) if (c.GetType() == typeof(Shadow)) { sh = c; break; }
            if (sh == null) sh = img.gameObject.AddComponent<Shadow>();
            sh.enabled = true;
            sh.effectColor = color;
            sh.effectDistance = new Vector2(dxCssPx * KeylineUi.CssPx, -dyCssPx * KeylineUi.CssPx);
            sh.useGraphicAlpha = true;
        }

        /// <summary>Lv 배지(흰 글자 + 검정 링) — 타일 아래쪽.</summary>
        public static TextMeshProUGUI LvBadge(RectTransform tile, double level, float size)
        {
            TextMeshProUGUI t = UiKit.Text(tile, "lv", TextKind.Sub, "Lv. " + JsNum.ToString(level), "stage_ink");
            WrapUi.Apply(t, "equip_cell_cell_lv");   // T361 7회차 — 정본 white-space 표(WrapUi.json) 941 `.equip-cell .cell-lv { nowrap }`
            t.fontStyle = FontStyles.Bold;
            LetterSpacing.Apply(t, "equip_cell_lv_ls_em");   // T168 4회차 — 정본 style.css 936~946 `.equip-cell .cell-lv { letter-spacing: .05em }`
            PopupKit.Ring(t, "pp_line", 0.25f);
            UiKit.Anchor(t.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, size * 0.08f), size, t.fontSize * 1.2f);
            return t;
        }

        /// <summary>
        /// ★N 배지(타일 바닥에 반쯤 걸침). 링(8방 키라인)은 정본 `.equip-cell .cell-star`(style.css 954~962)라 **모든 자리가 쓴다**.
        /// <paramref name="shadowKey"/> 는 그 위에 한 겹 더 얹는 **딱딱한 그림자**(T332 ⓒ) — 정본이 그것을 거는 자리는
        /// 제작 비교 카드의 `.cmp-star`(1860 · `drop-shadow(0 1px 0 rgba(0,0,0,.4))`) **하나뿐**이라 호출자가 켠다.
        /// 장비 칸·플레이어 정보 칸의 별은 정본에 그 한 겹이 없다(링만) — 기본값 `null` 이 그 뜻이다.
        /// </summary>
        public static TextMeshProUGUI StarBadge(RectTransform tile, int stars, float size, string shadowKey = null)
        {
            if (stars <= 0) return null;
            TextMeshProUGUI t = UiKit.Text(tile, "star", TextKind.Sub, "★" + (stars > 1 ? stars.ToString() : string.Empty), "coin");
            t.fontStyle = FontStyles.Bold;
            PopupKit.Ring(t, "pp_line", 0.25f);
            if (!string.IsNullOrEmpty(shadowKey)) UiKit.TextShadow(t, shadowKey);   // 글자라 TMP 언더레이(T333) — `UnityEngine.UI.Shadow` 는 TMP 메시를 안 잡는다
            UiKit.Anchor(t.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, size, t.fontSize * 1.2f);
            return t;
        }

        /// <summary>
        /// 아이템 카드(원작 `itemCardHTML` · 비교/세부정보 공용): 좌 아이콘(Lv·★·«새로운!») + 우 이름/주스탯(화살표)/서브스탯. item 이 null 이면 «빈 슬롯» 점선 카드.
        /// 반환 = 카드 루트(레이아웃 칸 · 높이는 내용).
        /// </summary>
        public static RectTransform ItemCard(Transform parent, string name, float w, ForgeItem item, string tag, string arrowDir, bool isNew, GameDefs d, Func<ForgeItem, Big> value)
        {
            float rem = PopupKit.Rem;
            float lineH = PopupKit.FontSize(TextKind.Sub) * 1.35f;
            float tile = rem * 3.6f;
            int subs = item != null && item.Subs != null ? item.Subs.Count : 0;
            float textH = lineH * (2 + subs);
            float h = Mathf.Max(tile + rem * 1.2f, textH + rem * 1.4f) + (isNew ? rem * 1.2f : 0f);
            RectTransform card = PopupKit.Item(parent, name, w, h);
            if (item == null)
            {
                Image face = PopupKit.Outlined(card, "face", "pp_paper", rem * 0.8f, PopupKit.Line3);
                face.color = Color.white;
                TextMeshProUGUI empty = UiKit.Text(card, "empty", TextKind.Sub, "빈 슬롯 — 장착 중인 장비 없음", "pp_muted");
                // T359 4회차 — 정본 `style.css` **1830** `.cmp-card.empty { opacity: .7 }` 는 **카드 한 겹 전체**에 걸린다(테·글자까지).
                //   클론은 그 .7 을 얼굴 이미지의 알파에 숫자로 박아 두어(§1 위반) 글자는 안 흐려졌다 — 표(`OpacityUi` `cmp_card_empty`)에서 읽어 카드에 건다.
                //   ⚠ 이 자리에 남은 두 가지는 **다른 축**이라 안 건드렸다: 정본 `.cmp-card` 는 배경이 아예 없고(면 축) 테가 `border-style: dashed` 다(T365 테 축).
                OpacityUi.Apply(card.gameObject, "cmp_card_empty");
                return card;
            }
            Color ac = AgeColor(d, item.Age);
            // 카드 자체는 판이 없다 — 원작 `.cmp-card-wrap.cur .cmp-card{border:none}` 와
            // `.cmp-lower .cmp-card-wrap.new .cmp-card{background:transparent}`. 흰 판은 이것을 품은
            // 팝업 카드(cur)와 회색 하부 패널(new)이 쥔다. (T57: 여기서 흰 테를 한 겹 더 그려
            // 원작에 없는 상자가 생기고, 반대로 새 장비 카드는 판 없이 3D 배경 위에 떠 보였다.)
            RectTransform tileRt = ItemTile(card, "tile", tile, d, item);   // T122 — 정본 itemImgHTML: 3D 썸네일이 있으면 그것, 없으면 실루엣
            // T371 6회차 — 정본 1852 `.cmp-img { background: color-mix(in srgb, var(--rc) 58%, #17181a) }`(«아이콘 배경도 시대색 통일»): 비교 카드의 그림 바탕은 제 키 `cmp_img_face` 로 받는다
            //   (값은 장비 칸 `cell_face` 와 같지만 정본이 따로 적은 자리라 자가 따로 센다). ⚠ 정본 `.cmp-img` 의 테는 `var(--ol2) solid var(--pp-line)`(섞기 아님 · 테 축 T365 몫) — 여기서 안 건드린다.
            Transform cmpFrame = tileRt.Find("frame");
            if (cmpFrame != null) { Transform cf = cmpFrame.Find("face"); if (cf != null) cf.GetComponent<Image>().color = ColorMixUi.Mix("cmp_img_face", ac); }
            UiKit.Place(tileRt, rem * 0.7f, rem * 0.6f, tile, tile);
            LvBadge(tileRt, item.Level, tile);
            StarBadge(tileRt, item.Stars, tile, "cmp_star");   // T332 ⓒ — 정본 `.cmp-star`(1860)만 딱딱한 그림자 한 겹을 더 진다
            if (isNew)
            {
                TextMeshProUGUI nt = UiKit.Text(card, "newtag", TextKind.Sub, tag, "pp_red");
                nt.fontStyle = FontStyles.Bold;
                UiKit.Place(nt.rectTransform, rem * 0.7f, rem * 0.6f + tile + rem * 0.5f, tile, lineH);
            }
            float tx = rem * 0.7f + tile + rem * 0.7f;
            float tw = w - tx - rem * 0.5f;
            TextMeshProUGUI nm = UiKit.Text(card, "name", TextKind.Body, "[" + AgeKr(d, item.Age) + "] " + item.Name, "pp_ink", TextAlignmentOptions.Left);
            nm.fontStyle = FontStyles.Bold;
            nm.color = InkOf(ac);
            UiKit.Place(nm.rectTransform, tx, rem * 0.6f, tw, lineH);
            string arrow = arrowDir == "up" ? " ▲" : arrowDir == "down" ? " ▼" : string.Empty;
            TextMeshProUGUI st = UiKit.Text(card, "stat", TextKind.Sub, NumFmt.Fmt(value(item)) + " " + StatLabel(item.Main), "pp_ink", TextAlignmentOptions.Left);
            st.fontStyle = FontStyles.Bold;
            UiKit.Place(st.rectTransform, tx, rem * 0.6f + lineH, tw, lineH);
            if (arrow.Length > 0)
            {
                TextMeshProUGUI ar = UiKit.Text(card, "arrow", TextKind.Sub, arrow.Trim(), arrowDir == "up" ? "pp_green" : "pp_red", TextAlignmentOptions.Left);
                ar.fontStyle = FontStyles.Bold;
                float sw = st.preferredWidth;
                UiKit.Place(ar.rectTransform, tx + sw + rem * 0.2f, rem * 0.6f + lineH, rem * 2f, lineH);
            }
            for (int i = 0; i < subs; i++)
            {
                TextMeshProUGUI s = UiKit.Text(card, "sub-" + i, TextKind.Sub, SubText(item.Subs[i]), "pp_ink", TextAlignmentOptions.Left);
                UiKit.Place(s.rectTransform, tx, rem * 0.6f + lineH * (2 + i), tw, lineH);
            }
            return card;
        }

        /// <summary>
        /// 깃발 리본(원작 `.cmp-ribbon` — 카드 좌단 밖으로 걸친 «장착됨»).
        /// T156 2회차 — 둥근 사각(`PopupKit.Outlined`)을 걷고 **정본 깃발**로: 오른쪽 «&lt;» 오목 노치 + 아랫변 없는 테두리
        /// (모양·수치는 <see cref="RibbonArt"/> 와 `Resources/RibbonUi.json` 이 쥔다).
        /// 글자는 **가운데 정렬**이고 치우침은 비대칭 패딩(.5rem ↔ 1.5rem)이 만든다 — 정본 주석이 못 박은 자리다(결정 339).
        /// </summary>
        public static RectTransform Ribbon(RectTransform card, string text, bool red)
        {
            float rem = PopupKit.Rem;
            float pl, pr, pt, pb;
            RibbonArt.Padding(rem, out pl, out pr, out pt, out pb);
            float fs = PopupKit.FontSize(TextKind.Sub);
            float w = RibbonArt.Width(UiKit.RefW), h = fs + pt + pb;
            RectTransform rt = UiKit.Box(card, "ribbon");
            rt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            // 자리도 표에서(정본 `top: -1.05rem; left: -1.52rem`) — 종전 `(-1.2rem, -0.8h)` 는 어림이라 깃발이 카드 밖으로
            // **안 내밀고** 왼쪽 변에 붙었다(런 406 PNG ↔ 원작 `shot-043224` 실측: 원작은 카드 왼쪽 밖으로 나온다).
            // 정본 주석: «`.modal-card`(padding 1.1rem + --ol2 테)의 같은 자리에 앉으므로 22.2 + 7(내밈) ≈ 2rem».
            Vector2 off = RibbonArt.Offset(rem);
            UiKit.Place(rt, off.x, off.y, w, h);
            RibbonArt.Build(rt, "flag", w, h, rem);
            TextMeshProUGUI t = UiKit.Text(rt, "label", TextKind.Sub, text, red ? "pp_red" : "pp_ink");
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            // 정본 `padding: .15rem 1.5rem .15rem .5rem` — 오른쪽이 «&lt;» 파임 몫만큼 넓어 글자가 왼쪽으로 치우쳐 보인다.
            UiKit.Place(t.rectTransform, pl, pt, w - pl - pr, fs);
            return rt;
        }

        /// <summary>시대 막대(원작 `.fi-age-bar`): 좌 아이콘+이름+★ · 중 현재% · 우 어두운 세그먼트에 다음%(null 이면 없음). 반환 = 막대 루트.</summary>
        public static RectTransform AgeBar(Transform parent, string name, float w, float h, GameDefs d, string age, string cur, string next, int stars, Action onClick = null, bool? check = null, bool autoForge = false)
        {
            Color ac = AgeColor(d, age);
            RectTransform bar = PopupKit.Item(parent, name, w, h);
            Image f = Tile(bar, "bar", ac, Color.black, h * 0.25f, PopupKit.Line);
            // T124 — 시대 무늬 층(정본 `.af-age-bar::before`·`.fi-age-bar::before` · 항성간 이상 다섯만 · 바탕 채움 바로 위 · 글자·체크 뒤).
            // T380 2회차 — **두 막대의 마스크 값이 다르다**: 정본 4841 `.af-age-bar::before` 는 30→50%, 5116~5123 `.fi-age-bar::before` 는 **24→46%** 다.
            //   여태 클론은 자동 제련 막대에만 마스크를 걸어 정보 팝업·목록 머리 막대는 무늬가 **왼쪽 아이콘·이름 뒤까지** 갔다.
            //   정본 `ui.js` 2026(확률 정보)·2110(목록 머리 `fi-age-bar fl-head`)이 둘 다 `.fi-age-bar` 고 `.fl-head` 는 마스크를 안 덮는다(5610~5611).
            AgePattern.Attach(bar, age, cell: false, mask: autoForge ? AgePatternKeys.AfBar : AgePatternKeys.FiBar, siblingIndex: 1);
            float rem = PopupKit.Rem;
            float x = rem * 0.5f;
            if (check.HasValue)
            {
                float cb = h * 0.62f;
                RectTransform box = UiKit.Box(bar, "check");
                UiKit.Place(box, x, (h - cb) * 0.5f, cb, cb);
                Tile(box, "box", check.Value ? new Color(0.14f, 0.77f, 0.32f, 1f) : Color.black, Color.black, cb * 0.2f, PopupKit.Line);
                if (check.Value)
                {
                    Image ck = PopupKit.IconOr(box, "mark", "check");
                    UiKit.Anchor(ck.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, cb * 0.8f, cb * 0.8f);
                }
                x += cb + rem * 0.4f;
            }
            float ico = h * 0.7f;
            Image ai = PopupKit.IconOr(bar, "ico", AgeIconKey(age));
            UiKit.Place(ai.rectTransform, x, (h - ico) * 0.5f, ico, ico);
            x += ico + rem * 0.35f;
            TextMeshProUGUI nm = UiKit.Text(bar, "name", TextKind.Sub, AgeKr(d, age) + (stars > 0 ? " " + Stars(stars) : string.Empty), "pp_ink", TextAlignmentOptions.Left);
             if (!autoForge) WrapUi.Apply(nm, "fi_age_name");   // T361 7회차 — 정본 white-space 표(WrapUi.json) 5092 `.fi-age-name { nowrap }`(자동 제련 `.af-age-name` 은 정본 선언 없음)
            nm.fontStyle = FontStyles.Bold;
            UiKit.Place(nm.rectTransform, x, 0f, w * 0.5f, h);
            float segW = next != null ? w * 0.25f : 0f;
            if (next != null)
            {
                RectTransform seg = UiKit.Box(bar, "next");
                UiKit.Place(seg, w - segW, 0f, segW, h);
                Image sf = UiKit.Rounded(seg, "face", "pp_paper", h * 0.25f);
                sf.color = Darker(ac);
                PopupKit.Inset(sf.rectTransform, PopupKit.Line);
                TextMeshProUGUI nt = UiKit.Text(seg, "pct", TextKind.Sub, next, "pp_ink", TextAlignmentOptions.Right);
                nt.fontStyle = FontStyles.Bold;
                nt.rectTransform.offsetMax = new Vector2(-rem * 0.5f, 0f);
            }
            TextMeshProUGUI ct = UiKit.Text(bar, "cur", TextKind.Sub, cur, "pp_ink", TextAlignmentOptions.Right);
            ct.fontStyle = FontStyles.Bold;
            UiKit.Place(ct.rectTransform, w * 0.5f, 0f, w * 0.5f - segW - rem * 0.5f, h);
            if (onClick != null)
            {
                Button b = bar.gameObject.AddComponent<Button>();
                b.targetGraphic = f;
                b.onClick.AddListener(() => onClick());
            }
            return bar;
        }

        /// <summary>재화 알약(원작 `.fi-pill`) — 아이콘 + 수.</summary>
        public static RectTransform Pill(Transform parent, string name, string iconKey, string text, float w, float h)
        {
            RectTransform rt = PopupKit.Item(parent, name, w, h);
            Image face = UiKit.Rounded(rt, "face", "pp_line", h * 0.5f);
            Image ico = PopupKit.IconOr(rt, "ico", iconKey);
            UiKit.Place(ico.rectTransform, h * 0.1f, h * 0.1f, h * 0.8f, h * 0.8f);
            TextMeshProUGUI t = UiKit.Text(rt, "amt", TextKind.Sub, text, "stage_ink", TextAlignmentOptions.Right);
            t.fontStyle = FontStyles.Bold;
            t.rectTransform.offsetMin = new Vector2(h, 0f);
            t.rectTransform.offsetMax = new Vector2(-h * 0.4f, 0f);
            return rt;
        }

        /// <summary>작은 «i» 원 버튼(원작 `.info-btn`·`.fi-info-btn`).</summary>
        public static Button InfoButton(Transform parent, string name, float size, UnityEngine.Events.UnityAction onClick)
        {
            Button b = UiKit.Button(parent, name, onClick);
            RectTransform rt = b.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            UiKit.Circle(rt, "ring", "pp_line");
            Image face = UiKit.Circle(rt, "face", "pp_paper");
            PopupKit.Inset(face.rectTransform, PopupKit.Line);
            TextMeshProUGUI t = UiKit.Text(rt, "glyph", TextKind.Sub, "i", "pp_ink");
            t.fontStyle = FontStyles.Bold;
            return b;
        }
    }
}
