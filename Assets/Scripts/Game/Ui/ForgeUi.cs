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
        /// <summary>장비 칸 면 = 시대색 58% + #17181a.</summary>
        public static Color CellFace(Color age) { return Mix(age, Ink, 0.42f); }
        /// <summary>장비 칸 테 = 시대색 80% + 검정.</summary>
        public static Color CellLine(Color age) { return Mix(age, Color.black, 0.2f); }
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

        /// <summary>장비 아이콘 타일(시대색 프레임 + 아이콘 · 잉크 76%). 반환 = 타일 루트.</summary>
        public static RectTransform ItemTile(Transform parent, string name, float size, GameDefs d, string age, string iconKey, float inkFrac = 0.76f, bool agePattern = false)
        {
            Color ac = AgeColor(d, age);
            RectTransform rt = UiKit.Box(parent, name);
            rt.sizeDelta = new Vector2(size, size);
            Tile(rt, "frame", CellFace(ac), CellLine(ac), size * 0.16f, PopupKit.Line3);
            // T124 — 정본 `.fl-face.equip-cell[data-age]` 만 시대 무늬를 입는다(제작 카드·상세 머리 아이콘은 equip-cell 이 아니다) → 호출자가 켠다
            if (agePattern) AgePattern.Attach(rt, age, cell: true, mask: false, siblingIndex: 1);
            Image ico = PopupKit.IconOr(rt, "img", iconKey);
            float k = size * inkFrac;
            UiKit.Anchor(ico.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, k, k);
            return rt;
        }

        /// <summary>
        /// T122 — 정본 `itemImgHTML(item)`: `Scene3D.itemThumb(item)` 이 있으면 3D 썸네일 <img>(타일 100% · object-fit contain), 없으면 슬롯 플레이스홀더.
        /// 동기 호출도 정본 그대로(비교·상세 카드는 한두 장 · 키 단위 캐시). 목록처럼 많은 칸은 <see cref="ItemFaces.Request"/> 로 프레임마다 받아 <see cref="ApplyThumb"/> 로 갈아 끼운다.
        /// </summary>
        public static RectTransform ItemTile(Transform parent, string name, float size, GameDefs d, ForgeItem it, float inkFrac = 0.76f, bool agePattern = false)
        {
            RectTransform rt = ItemTile(parent, name, size, d, it.Age, ItemIconKey(d, it), inkFrac, agePattern);
            ApplyThumb(rt, ItemFaces.Get(d, it), size);
            return rt;
        }

        /// <summary>구운 썸네일이 있으면 타일의 `img` 를 그것으로 갈아 끼운다(정본 hydrate · `.fl-face img` 100%). null 이면 실루엣 그대로(false).</summary>
        public static bool ApplyThumb(RectTransform tile, Sprite thumb, float size)
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
            return true;
        }

        /// <summary>Lv 배지(흰 글자 + 검정 링) — 타일 아래쪽.</summary>
        public static TextMeshProUGUI LvBadge(RectTransform tile, double level, float size)
        {
            TextMeshProUGUI t = UiKit.Text(tile, "lv", TextKind.Sub, "Lv. " + JsNum.ToString(level), "stage_ink");
            t.fontStyle = FontStyles.Bold;
            PopupKit.Ring(t, "pp_line", 0.25f);
            UiKit.Anchor(t.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, size * 0.08f), size, t.fontSize * 1.2f);
            return t;
        }

        /// <summary>★N 배지(타일 바닥에 반쯤 걸침).</summary>
        public static TextMeshProUGUI StarBadge(RectTransform tile, int stars, float size)
        {
            if (stars <= 0) return null;
            TextMeshProUGUI t = UiKit.Text(tile, "star", TextKind.Sub, "★" + (stars > 1 ? stars.ToString() : string.Empty), "coin");
            t.fontStyle = FontStyles.Bold;
            PopupKit.Ring(t, "pp_line", 0.25f);
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
                face.color = new Color(1f, 1f, 1f, 0.7f);
                TextMeshProUGUI empty = UiKit.Text(card, "empty", TextKind.Sub, "빈 슬롯 — 장착 중인 장비 없음", "pp_muted");
                return card;
            }
            Color ac = AgeColor(d, item.Age);
            // 카드 자체는 판이 없다 — 원작 `.cmp-card-wrap.cur .cmp-card{border:none}` 와
            // `.cmp-lower .cmp-card-wrap.new .cmp-card{background:transparent}`. 흰 판은 이것을 품은
            // 팝업 카드(cur)와 회색 하부 패널(new)이 쥔다. (T57: 여기서 흰 테를 한 겹 더 그려
            // 원작에 없는 상자가 생기고, 반대로 새 장비 카드는 판 없이 3D 배경 위에 떠 보였다.)
            RectTransform tileRt = ItemTile(card, "tile", tile, d, item);   // T122 — 정본 itemImgHTML: 3D 썸네일이 있으면 그것, 없으면 실루엣
            UiKit.Place(tileRt, rem * 0.7f, rem * 0.6f, tile, tile);
            LvBadge(tileRt, item.Level, tile);
            StarBadge(tileRt, item.Stars, tile);
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
            // T124 — 시대 무늬 층(정본 `.af-age-bar::before`·`.fi-age-bar::before` · 항성간 이상 다섯만 · 바탕 채움 바로 위 · 글자·체크 뒤) · 자동 제련 막대만 왼쪽 30→50% 마스크
            AgePattern.Attach(bar, age, cell: false, mask: autoForge, siblingIndex: 1);
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
