using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.Forging;
using Forge.Game.Audio;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 제작 비교 팝업(ROUTINE T19 · 원작 ui.js `showCraftModal`·`resolveCraft`·`showSellConfirm` · shot-043224): 위 «장착됨» 카드 · 아래 회색 패널에 «새로운!/교체됨» 카드 + [판매][장착].
    /// 닫기 버튼이 없다 — 닫히는 길은 [판매]·딤 클릭(=보류)뿐. 카드 오버레이(원작 `buildCraftCard`·`showCraftReveal`·`showAutoDropCard`·`showCraftBatch`)도 여기.
    /// </summary>
    public static class ForgeCraftPopup
    {
        public const string Name = "craft", SellName = "sellwarn";
        static ForgeItem current;
        static RectTransform reveal, batch;

        public static ForgeItem Current { get { return current; } }

        public static void Show(ForgeHost h, ForgeItem item)
        {
            current = item;
            h.Meta.Popups.Show(Name);
            Render(h);
        }

        public static void Hide(ForgeHost h)
        {
            h.Meta.Popups.Hide(Name);
            current = null;
        }

        public static void Render(ForgeHost h)
        {
            Popup p = h.Meta.Popups.Find(Name);
            if (p == null || current == null) return;
            ForgeItem item = current;
            RectTransform root = PopupLayer.Clear(p);
            Button dim = root.GetChild(0).gameObject.GetComponent<Button>() ?? root.GetChild(0).gameObject.AddComponent<Button>();
            dim.onClick.RemoveAllListeners();
            dim.onClick.AddListener(() => h.OnCraftDimClick());
            GameDefs d = h.Defs;
            ForgeItem cur = h.Gear.Get(item.Slot);
            bool isMatch = h.GearSys.IsMatchingGear(item, cur);
            bool swapped = h.PendingSwapped;
            string newTag = swapped ? "교체됨" : "새로운!";
            bool newIsHigher = cur == null || h.GearSys.ItemValue(item).Gte(h.GearSys.ItemValue(cur));

            float rem = PopupKit.Rem;
            // T113 — 정본 1748: 폭은 공용 .modal-card.wide(74%) 가 아니라 68.8%W · 1742: 카드는 가운데가 아니라 **하단 앵커**
            // (padding-bottom = 탭바 높이 + 1.65rem → 카드 바닥이 앱 바닥에서 그만큼 위 · 원본 실측 86.9%H). T111 장비 상세와 같은 길(자기 표 CraftUi.json).
            float w = CraftStyle.Px("card_w");
            float pad = UiKit.H("card_pad");
            RectTransform card = PopupKit.Card(root, "card", w, -1f, "pp_paper", rem * 1.1f);
            UiKit.Anchor(card, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, CraftStyle.BottomPx()), w, card.sizeDelta.y);
            VerticalLayoutGroup cardLg = PopupKit.Column(card, pad + rem * 0.5f, rem * 0.5f);
            // T390 — 정본 style.css 1819 `.cmp-lower { margin: 0 -.85rem -.85rem }`(주석 «카드 패딩 1.1rem + 테두리 3px 이므로 -.85rem 음수 마진이 인셋 7px 을 만든다»):
            // 회색 패널이 카드의 아래 패딩(1.1rem = `card_pad`)을 .85rem 파고든다 → 카드 층의 **아래** 패딩 = card_pad − 당김(표 `cmp_lower_pull_rem`).
            // 하단 앵커 카드라 이 여백이 곧 카드 위끝을 민다(1781 주석) — 런 723 실측 버튼 아래 여백 63px ↔ 원작 26px(+33px 이 통째로 카드 높이).
            cardLg.padding = new RectOffset(cardLg.padding.left, cardLg.padding.right, cardLg.padding.top, Mathf.RoundToInt(pad - CraftStyle.Px("cmp_lower_pull_rem")));
            float inner = w - (pad + rem * 0.5f) * 2f;
            RectTransform curCard = ForgeUi.ItemCard(card, "cur", inner, cur, "장착됨", cur != null ? (newIsHigher ? "down" : "up") : null, false, d, h.GearSys.ItemValue);
            ForgeUi.Ribbon(curCard, "장착됨", false);

            RectTransform lower = PopupKit.Item(card, "lower", inner, -1f);
            Image lf = UiKit.Rounded(lower, "face", "pp_gray", rem * 0.7f);
            lf.color = new Color(0xbe / 255f, 0xbe / 255f, 0xbe / 255f);
            VerticalLayoutGroup lg = PopupKit.Column(lower, rem * 0.4f, rem * 0.45f);
            // T390 — 정본 `.cmp-lower` 에는 padding 규칙이 없다: 패널 아래 여백은 `.row` 의 padding-bottom(아래 표) 하나뿐이라 패널 자신의 아래 패딩은 0.
            lg.padding = new RectOffset(lg.padding.left, lg.padding.right, lg.padding.top, 0);
            ForgeUi.ItemCard(lower, "new", inner - rem * 0.8f, item, newTag, cur != null ? (newIsHigher ? "up" : "down") : null, true, d, h.GearSys.ItemValue);
            // T390 — 정본 1821 `.cmp-lower .row { padding-bottom: 1.44rem }`(표 `cmp_row_pad_bottom_rem` · 전엔 `rem * 1.4f` 가 박혀 있었다).
            RectTransform row = PopupKit.Item(lower, "row", -1f, UiKit.H("btn_h") * 1.7f + CraftStyle.Px("cmp_row_pad_bottom_rem"));
            float bw = (inner - rem * 0.8f - rem * 1.3f - rem * 1.9f) * 0.5f, bh = UiKit.H("btn_h") * 1.7f;
            // T110 — 정본 ui.js 3266 `판매<small>${IconGen.img('coin')} +N</small>`: 아랫줄은 코인 **아이콘** + 수(글자 🪙 가 아니다 · 세로 갈래 IconTextStack).
            Button sell = PopupKit.Btn(row, "sell", "", "pp_red", "pp_red_dk", () => h.ResolveCraft("sell"), bw, bh, "stage_ink", TextKind.Sub);
            PinSell(sell);
            IconTextStack.ReplaceLabel(sell, TextKind.Sub, "판매\n🪙 +" + NumFmt.Fmt(h.GearSys.SellPrice(item)), "stage_ink", "pp_red");
            UiKit.Place(sell.GetComponent<RectTransform>(), rem * 0.96f, 0f, bw, bh);
            string equipLabel = "장착" + (cur != null ? "\n" + (swapped ? "다시 장착" : "기존 교체") : string.Empty);
            Button equip = PopupKit.Btn(row, "equip", equipLabel, "pp_blue", "pp_blue_dk", () => h.ResolveCraft("equip"), bw, bh, "stage_ink", TextKind.Sub);
            TwoLine(equip);
            UiKit.Place(equip.GetComponent<RectTransform>(), rem * 0.96f + bw + rem * 1.3f, 0f, bw, bh);
            if (isMatch)
            {
                TextMeshProUGUI same = UiKit.Text(lower, "same", TextKind.Sub, "같은 장비", "pp_muted");
                PopupKit.Size(same.rectTransform, -1f, same.fontSize * 1.2f);
            }
        }

        /// <summary>
        /// T377 3회차 — 정본 `style.css` 8686 `.btn.btn.danger.danger, .btn.btn.sell.sell { background-color: #ff1017; box-shadow: inset 0 -.22rem 0 #4e0507 }` 는
        /// 전역 `--pp-red`(#e8362f)·`--pp-red-dk` 가 아니라 **버튼 규칙에만 준 리터럴**이다(8692 «토큰을 옮기지 말 것»). 그래서 `PopupKit.Btn` 의 키 인수는
        /// `pp_red` 그대로 두고(키라인·글자 그림자 규칙이 «색 버튼» 을 그 키로 가른다) **면·턱 `Image` 의 색만** 자리 전용 표 키로 덮는다 —
        /// 채팅 뒤로 버튼(`ChatScreen`)과 같은 길. 값은 `Resources/PinnedColorUi.json` · `tools/check_pinned_colors.py` 가 정본과 같은지 지킨다.
        /// </summary>
        static void PinSell(Button b)
        {
            Transform t = b.transform;
            t.Find("face").GetComponent<Image>().color = PinnedColorUi.C("sell_btn_face");
            t.Find("lip").GetComponent<Image>().color = PinnedColorUi.C("sell_btn_lip");
        }

        static void TwoLine(Button b)
        {
            TextMeshProUGUI t = b.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) { t.textWrappingMode = TextWrappingModes.Normal; t.lineSpacing = -20f; }
        }

        // ---- 판매 경고(원작 showSellConfirm — 파는 쪽이 남는 쪽보다 시대가 최신) ----

        public static void ShowSellConfirm(ForgeHost h, ForgeItem sold, ForgeItem kept)
        {
            Popup p = h.Meta.Popups.Show(SellName);
            RectTransform root = PopupLayer.Clear(p);
            GameDefs d = h.Defs;
            float rem = PopupKit.Rem;
            float w = UiKit.RefW * 0.76f;
            RectTransform card = PopupKit.Card(root, "card", w, -1f, "pp_paper", rem * 1.1f);
            PopupKit.Column(card, rem * 0.9f, rem * 0.7f);
            TextMeshProUGUI title = PopupKit.Label(card, "title", TextKind.Body, "정말 판매할까요?", "pp_ink");
            // T109 13회차 — 정본 style.css 3846 의 제목 묶음에 `h3.sellwarn-title` 이 들어 있다
            // (`-webkit-text-stroke: .11em var(--pp-line)`). ui.js 3849 가 이 제목을 그 클래스로 찍는다.
            // 폭은 `KeylineUi.Em`(표 · em → px)이 낸다 — `ForgeAutoPopup` 의 af-title 과 같은 길이다.
            UiKit.OutlinePx(title, "pp_line", KeylineUi.Em("sheet_title", title.fontSize));
            RectTransform cmp = PopupKit.Item(card, "cmp", -1f, rem * 4.6f);
            float colW = (w - rem * 1.8f - rem * 2f) * 0.5f;
            Col(cmp, "sold", 0f, colW, "파는 것", true, sold, d);
            TextMeshProUGUI gt = UiKit.Text(cmp, "gt", TextKind.Body, ">", "pp_red");
            gt.fontStyle = FontStyles.Bold;
            UiKit.Place(gt.rectTransform, colW, 0f, rem * 2f, rem * 4.6f);
            Col(cmp, "kept", colW + rem * 2f, colW, "남는 것", false, kept, d);
            int gap = Array.IndexOf(d.Ages, sold.Age) - Array.IndexOf(d.Ages, kept.Age);
            TextMeshProUGUI note = PopupKit.Label(card, "note", TextKind.Sub, "파는 쪽이 " + gap + "시대 더 최신입니다.\n같거나 이전 시대면 이 창은 뜨지 않습니다.", "pp_muted", TextAlignmentOptions.Center, true, false, PopupKit.FontSize(TextKind.Sub) * 2.8f);
            // T354 10회차 — 정본 2239 `.sellwarn-note { line-height: 1.35 }`. 이 글은 줄바꿈이 박혀 **두 줄**이라 줄 간격이 눈에 보이는 자리다.
            LineHeight.Apply(note, "sellwarn_note_lh");
            RectTransform row = PopupKit.Item(card, "row", -1f, UiKit.H("btn_h") * 1.5f);
            float bw = (w - rem * 1.8f - rem * 0.8f) * 0.5f, bh = UiKit.H("btn_h") * 1.5f;
            // T110 — 정본 ui.js 3865 도 `판매<small>coin +N</small>` 두 줄이다(클론은 한 줄 글자였다).
            Button s = PopupKit.Btn(row, "sell", "", "pp_red", "pp_red_dk", () => h.OnSellConfirm(), bw, bh, "stage_ink", TextKind.Sub);
            PinSell(s);
            IconTextStack.ReplaceLabel(s, TextKind.Sub, "판매\n🪙 +" + NumFmt.Fmt(h.GearSys.SellPrice(sold)), "stage_ink", "pp_red");
            UiKit.Place(s.GetComponent<RectTransform>(), 0f, 0f, bw, bh);
            Button c = PopupKit.Btn(row, "cancel", "취소", "pp_gray", "pp_gray_dk", () => h.OnSellCancel(), bw, bh, "stage_ink", TextKind.Sub);
            UiKit.Place(c.GetComponent<RectTransform>(), bw + rem * 0.8f, 0f, bw, bh);
        }

        static void Col(RectTransform parent, string name, float x, float w, string tagText, bool red, ForgeItem it, GameDefs d)
        {
            float rem = PopupKit.Rem;
            RectTransform col = UiKit.Box(parent, name);
            UiKit.Place(col, x, 0f, w, rem * 4.6f);
            Image bg = UiKit.Rounded(col, "bg", "pp_panel", rem * 0.55f);
            bg.color = new Color(23 / 255f, 24 / 255f, 26 / 255f, 0.05f);
            float lh = PopupKit.FontSize(TextKind.Sub) * 1.3f;
            TextMeshProUGUI tag = UiKit.Text(col, "tag", TextKind.Sub, tagText, red ? "pp_red" : "pp_muted");
            tag.fontStyle = FontStyles.Bold;
            UiKit.Place(tag.rectTransform, 0f, rem * 0.3f, w, lh);
            Color ac = ForgeUi.AgeColor(d, it.Age);
            RectTransform chip = UiKit.Box(col, "age");
            float cw = w * 0.8f;
            UiKit.Place(chip, (w - cw) * 0.5f, rem * 0.3f + lh + rem * 0.15f, cw, lh);
            Image cf = UiKit.Rounded(chip, "bg", "pp_paper", lh * 0.4f);
            cf.color = ac;
            TextMeshProUGUI ct = UiKit.Text(chip, "label", TextKind.Sub, ForgeUi.AgeKr(d, it.Age), "pp_ink");
            ct.fontStyle = FontStyles.Bold;
            WrapUi.Apply(ct, "swc_age");   // T361 7회차 — 정본 white-space 표(WrapUi.json) 2232 `.swc-age { nowrap }`
            ct.color = ForgeUi.InkOf(Color.white) ;
            ct.color = (0.2126f * ac.r + 0.7152f * ac.g + 0.0722f * ac.b) > 0.5f ? Color.black : Color.white;
            TextMeshProUGUI nm = UiKit.Text(col, "name", TextKind.Sub, it.Name, "pp_ink");
            LineHeight.Apply(nm, "swc_name_lh");   // T354 10회차 — 정본 2236 `.swc-name { line-height: 1.15; word-break: keep-all }`(긴 이름이 꺾이는 칸)
            UiKit.Place(nm.rectTransform, 0f, rem * 0.3f + lh * 2f + rem * 0.3f, w, lh);
        }

        public static void HideSellConfirm(ForgeHost h) { h.Meta.Popups.Hide(SellName); }

        // ---- 카드 오버레이(모루 위 리빌 · 탈락 · 배치 카드판) ----

        static RectTransform Overlay(string name)
        {
            UiRoot root = UiRoot.Instance;
            RectTransform rt = UiKit.Box(root.App, name);
            rt.SetSiblingIndex(root.TabBand.GetSiblingIndex());
            return rt;
        }

        static RectTransform CraftCard(Transform parent, ForgeHost h, ForgeItem it, float size, string faceKey, string lineKey)
        {
            // T122 ⓑ — 정본 buildCraftCard 도 itemImgHTML(3D 썸네일 · 없으면 실루엣)로 그린다: ForgeItem 오버로드(2회차)가 그 폴백 순서를 쥔다
            // T382 — 정본 buildCraftCard(ui.js 1918)·cb-card(1972)는 둘 다 itemImgHTML(it, 'adc-img cell-img') = 슬롯과 같은 fit-ink(THUMB_INK .76 · 3145).
            //        여기 박혀 있던 .9(T19 첫 커밋 · 결정 없음)는 썸네일 없는 슬롯(실루엣)의 잉크를 18% 키웠다 — 인수를 걷어 ItemTile 기본(.76)으로. 3D 썸네일은 ApplyThumb 가 표 img_frac 로 잡는다.
            RectTransform tile = ForgeUi.ItemTile(parent, "card", size, h.Defs, it);
            // T371 5회차 — 정본 1063 `.auto-drop-card` · 1131 `.craft-batch .cb-card { background: color-mix(in srgb, var(--rc) 58%, #17181a); border: … 80%, #000 }`:
            //   ItemTile 은 ForgeUi.CellFace/CellLine(비율이 코드에 박힘 · T332 lock)로 칠하므로 부르는 쪽이 표 `ColorMixUi.json` 으로 덮는다(2회차 모루 카드와 같은 길 · §1).
            Color ac = ForgeUi.AgeColor(h.Defs, it.Age);
            MixFrame(tile, ac, faceKey, lineKey);
            // T178 15회차 — 정본 1063·1131 은 그 면 위에 `.equip-cell`(828) 과 같은 **45°/−45° 교차 해칭**(rgba(0,0,0,.13) 2px / 12px 주기)을 두 겹 깐다(주석 «장비 슬롯과 똑같이 … 해칭 배경을 그대로 옮겼다»).
            //   표 `SurfaceUi.json` stripes.cell_hatch · 바탕이 곧 이 면의 color-mix 색이라 그 색 위에 미리 합성해 굽고 둥근 면이 마스크한다.
            Transform hf = tile != null ? tile.Find("frame/face") : null;
            if (hf != null) SurfaceArt.FillHatch(hf.GetComponent<Image>(), "hatch", "cell_hatch", ColorMixUi.Mix(faceKey, ac));
            return tile;
        }

        /// <summary>T371 — `ForgeUi.Tile(…, "frame", …)` 이 세운 테(line)·면(face) 두 자식의 색을 표 키로 덮는다.</summary>
        static void MixFrame(RectTransform tile, Color ac, string faceKey, string lineKey)
        {
            Transform frame = tile != null ? tile.Find("frame") : null;
            if (frame == null) return;
            Transform f = frame.Find("face"), l = frame.Find("line");
            Image face = f != null ? f.GetComponent<Image>() : null, line = l != null ? l.GetComponent<Image>() : null;
            if (face != null) face.color = ColorMixUi.Mix(faceKey, ac);
            if (line != null) line.color = ColorMixUi.Mix(lineKey, ac);
        }

        /// <summary>정본 `AGES.indexOf(item.age)` — 표에 없는 시대는 −1(정본과 같다 · `Sfx.CraftReveal` 이 0 으로 받는다).</summary>
        public static int AgeIndex(ForgeHost h, ForgeItem it) { return Array.IndexOf(h.Defs.Ages, it.Age); }

        static Vector2 AnvilTop()
        {
            UiRoot root = UiRoot.Instance;
            float sheetTop = UiKit.L("sheet_top") * UiKit.RefH;
            float rem = PopupKit.Rem;
            float cell = (UiKit.RefW - UiKit.RefW * 0.1094f * 2f - UiKit.RefW * 0.0294f * 4f) / 5f;
            float y = sheetTop + rem * 0.55f + cell * 2f + rem * 0.6f + rem * 0.5f;
            return new Vector2(UiKit.RefW * 0.5f, y);
        }

        /// <summary>원작 showCraftReveal — 모루 위로 튀어올라 머문 뒤 팝업에 자리를 넘긴다(0.56초).</summary>
        public static void ShowReveal(ForgeHost h, ForgeItem item, Action done)
        {
            DismissReveal();
            reveal = Overlay("craft-reveal");
            float size = PopupKit.Rem * 3.7f;
            Vector2 a = AnvilTop();
            // 링(`crring`)이 카드 **뒤**라 먼저 만든다 — 정본은 `box-shadow` 라 그림 바깥으로 퍼진다.
            Image ring = UiKit.Rounded(reveal, "cr-ring", "pp_line", size * 0.16f);
            RectTransform card = CraftCard(reveal, h, item, size, "drop_card_face", "drop_card_line");   // 정본 1063 .auto-drop-card(.craft-reveal 도 같은 클래스)
            // 광택(`crsheen`) — 정본 `.craft-reveal { overflow: hidden }` + `::after { inset: 0 }` 이라
            // 카드 폭만 한 마스크 상자 안에서 띠가 −130% → 150% 로 쓸린다.
            RectTransform mask = UiKit.Box(card, "cr-sheen-box");
            PopupKit.Inset(mask, 0f);
            mask.gameObject.AddComponent<RectMask2D>();
            Image sheenImg = UiKit.Panel(mask, "cr-sheen", "pp_paper");
            sheenImg.sprite = CraftCardArt.Sheen();
            sheenImg.type = Image.Type.Simple;
            sheenImg.color = Color.white;
            sheenImg.raycastTarget = false;
            RectTransform sheen = sheenImg.rectTransform;
            UiKit.Anchor(sheen, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size, size);
            CraftCardFx.Play(card, CraftCardFx.Mode.Reveal, a, size, ring, ForgeUi.AgeColor(h.Defs, item.Age), sheen);
            Sfx.CraftReveal(AgeIndex(h, item));   // 정본 ui.js 1938 `SFX.craftReveal(AGES.indexOf(item.age))` — T119 4회차
            h.Delay(ForgeHost.RevealCardSec, () => { DismissReveal(); done(); });
        }

        /// <summary>원작 showAutoDropCard — 필터 탈락 장비를 모루 위에 잠깐(0.62초).</summary>
        public static void ShowAutoDropCard(ForgeHost h, ForgeItem item, Action done)
        {
            DismissReveal();
            reveal = Overlay("auto-drop-card");
            float size = PopupKit.Rem * 3.7f;
            Vector2 a = AnvilTop();
            RectTransform card = CraftCard(reveal, h, item, size, "drop_card_face", "drop_card_line");   // 정본 1063 .auto-drop-card(.craft-reveal 도 같은 클래스)
            // 정본 .auto-drop-card(style.css 1073) `0 .3rem .6rem rgba(0,0,0,.45)` — 모루 위에 뜬 카드라 흐린 그늘이 진다.
            // 반지름은 같은 줄의 `border-radius: .7rem`. 연출(CraftCardFx)이 이 상자를 움직여도 그늘은 자식이라 같이 간다.
            UiShadow.Drop(card, "autodrop_drop", PopupKit.Rem * 0.7f);
            CraftCardFx.Play(card, CraftCardFx.Mode.AutoDrop, a, size, null, Color.clear);
            h.Delay(ForgeHost.AutoCardSec, () => { DismissReveal(); done(); });
        }

        public static void DismissReveal()
        {
            if (reveal != null) { UnityEngine.Object.Destroy(reveal.gameObject); reveal = null; }
        }

        /// <summary>모루가 보이는 기본 화면인가(하단 시트도 팝업도 안 열린 상태) — 카드판은 이때만 편다.</summary>
        static bool ForgeScreenVisible(ForgeHost h)
        {
            UiRoot root = UiRoot.Instance;
            if (root == null || root.TabBar.ActiveTab != null) return false;
            return h.Meta.Popups.OpenCount == 0;
        }

        /// <summary>원작 showCraftBatch — N장을 한 화면에 동시에(1.6초 · 눌러서 바로 넘기기). 남의 화면이면 카드 없이 done 만.</summary>
        public static void ShowBatch(ForgeHost h, List<ForgeItem> items, Action done)
        {
            if (items == null || items.Count == 0) { done(); return; }
            if (!ForgeScreenVisible(h)) { done(); return; }
            DismissBatch();
            batch = Overlay("craft-batch");
            Image dim = UiKit.Panel(batch, "dim", "modal_dim");
            dim.color = new Color(0f, 0f, 0f, CraftCardFx.DimBase);
            dim.raycastTarget = true;
            int cols = items.Count <= 4 ? items.Count : items.Count <= 9 ? 3 : 4;
            int rows = (items.Count + cols - 1) / cols;
            float size = PopupKit.Rem * 3.7f, gap = PopupKit.Rem * 0.5f;
            float gw = cols * size + (cols - 1) * gap, gh = rows * size + (rows - 1) * gap;
            RectTransform grid = UiKit.Box(batch, "cb-grid");
            UiKit.Anchor(grid, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, gw, gh);
            for (int i = 0; i < items.Count; i++)
            {
                RectTransform card = CraftCard(grid, h, items[i], size, "batch_card_face", "batch_card_line");   // 정본 1131 .craft-batch .cb-card
                card.name = "cb-card-" + i;
                UiKit.Place(card, (i % cols) * (size + gap), (i / cols) * (size + gap), size, size);
                // 정본 `.craft-batch .cb-card`(style.css 1140) `0 .3rem .6rem rgba(0,0,0,.45)` — 자동 폐기 카드와 같은 그늘이다.
                // 반지름은 카드(ForgeUi.ItemTile)가 실제로 쓰는 것을 그림에서 되읽는다 — 그 파일을 열지 않아도 된다(T331 5회차).
                UiShadow.Drop(card, "cbcard_drop");
            }
            // 정본 1118 «⚠️ 카드마다 animation-delay 를 주지 말 것» — 격자 **전체**가 `cbpop` 하나를 탄다.
            CraftCardFx.PlayBatch(grid, dim);
            Sfx.CraftReveal(AgeIndex(h, items[0]));   // 정본 ui.js 1976 `SFX.craftReveal(AGES.indexOf(items[0].age))` — 카드판을 붙인 직후 · T119 4회차
            bool finished = false;
            Action finish = () => { if (finished) return; finished = true; DismissBatch(); done(); };
            Button b = dim.gameObject.AddComponent<Button>();
            b.onClick.AddListener(() => finish());
            h.Delay(ForgeHost.CraftBatchSec, finish);
        }

        public static void DismissBatch()
        {
            if (batch != null) { UnityEngine.Object.Destroy(batch.gameObject); batch = null; }
        }

        public static bool BatchVisible { get { return batch != null; } }
    }

    /// <summary>
    /// T113 제작 비교 팝업의 배치표(<c>Assets/Forge/Resources/CraftUi.json</c> — 정본 `style.css` 1742 `#craft-modal` · 1748 `.modal-card.wide`).
    /// T111 <see cref="GearDetailStyle"/> 과 같은 꼴 — 코드에 숫자를 박지 않는다(§1). 키 접미: _w 앱 폭 분수 · _rem 정본 rem.
    /// </summary>
    public static class CraftStyle
    {
        public const string ResourcePath = "CraftUi";
        static JsonObject root, layout;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T113)");
            root = MiniJson.ParseObject(ta.text);
            layout = J.Obj(root["layout"]);
        }

        public static void Reset() { root = null; layout = null; }

        /// <summary>배치 값 원문.</summary>
        public static float L(string key)
        {
            Load();
            object v = layout[key];
            if (!J.IsNum(v)) throw new System.Collections.Generic.KeyNotFoundException("CraftUi.json 에 배치 값 «" + key + "» 이 없다");
            return (float)J.Num(v);
        }

        /// <summary>키 접미에 맞춰 기준 px 로(_w 앱 폭 · _rem 정본 rem).</summary>
        public static float Px(string key)
        {
            float v = L(key);
            if (key.EndsWith("_w")) return v * UiKit.RefW;
            if (key.EndsWith("_rem")) return v * PopupKit.Rem;
            return v;
        }

        /// <summary>카드 바닥이 앱 바닥에서 뜨는 높이(기준 px) — 정본 `padding-bottom: calc(var(--tabbar-h) + 1.65rem)` = 탭바 높이(카탈로그 `tabbar_top`) + `bottom_rem`.</summary>
        public static float BottomPx() { return (1f - UiKit.L("tabbar_top")) * UiKit.RefH + Px("bottom_rem"); }
    }
}
