using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.Forging;

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
            float w = UiKit.L("modal_wide_w") * UiKit.RefW;
            float pad = UiKit.H("card_pad");
            RectTransform card = PopupKit.Card(root, "card", w, -1f, "pp_paper", rem * 1.1f);
            PopupKit.Column(card, pad + rem * 0.5f, rem * 0.5f);
            float inner = w - (pad + rem * 0.5f) * 2f;
            RectTransform curCard = ForgeUi.ItemCard(card, "cur", inner, cur, "장착됨", cur != null ? (newIsHigher ? "down" : "up") : null, false, d, h.GearSys.ItemValue);
            ForgeUi.Ribbon(curCard, "장착됨", false);

            RectTransform lower = PopupKit.Item(card, "lower", inner, -1f);
            Image lf = UiKit.Rounded(lower, "face", "pp_gray", rem * 0.7f);
            lf.color = new Color(0xbe / 255f, 0xbe / 255f, 0xbe / 255f);
            VerticalLayoutGroup lg = PopupKit.Column(lower, rem * 0.4f, rem * 0.45f);
            ForgeUi.ItemCard(lower, "new", inner - rem * 0.8f, item, newTag, cur != null ? (newIsHigher ? "up" : "down") : null, true, d, h.GearSys.ItemValue);
            RectTransform row = PopupKit.Item(lower, "row", -1f, UiKit.H("btn_h") * 1.7f + rem * 1.4f);
            float bw = (inner - rem * 0.8f - rem * 1.3f - rem * 1.9f) * 0.5f, bh = UiKit.H("btn_h") * 1.7f;
            Button sell = PopupKit.Btn(row, "sell", "판매\n🪙 +" + NumFmt.Fmt(h.GearSys.SellPrice(item)), "pp_red", "pp_red_dk", () => h.ResolveCraft("sell"), bw, bh, "stage_ink", TextKind.Sub);
            TwoLine(sell);
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
            PopupKit.Label(card, "title", TextKind.Body, "정말 판매할까요?", "pp_ink");
            RectTransform cmp = PopupKit.Item(card, "cmp", -1f, rem * 4.6f);
            float colW = (w - rem * 1.8f - rem * 2f) * 0.5f;
            Col(cmp, "sold", 0f, colW, "파는 것", true, sold, d);
            TextMeshProUGUI gt = UiKit.Text(cmp, "gt", TextKind.Body, ">", "pp_red");
            gt.fontStyle = FontStyles.Bold;
            UiKit.Place(gt.rectTransform, colW, 0f, rem * 2f, rem * 4.6f);
            Col(cmp, "kept", colW + rem * 2f, colW, "남는 것", false, kept, d);
            int gap = Array.IndexOf(d.Ages, sold.Age) - Array.IndexOf(d.Ages, kept.Age);
            PopupKit.Label(card, "note", TextKind.Sub, "파는 쪽이 " + gap + "시대 더 최신입니다.\n같거나 이전 시대면 이 창은 뜨지 않습니다.", "pp_muted", TextAlignmentOptions.Center, true, false, PopupKit.FontSize(TextKind.Sub) * 2.8f);
            RectTransform row = PopupKit.Item(card, "row", -1f, UiKit.H("btn_h") * 1.5f);
            float bw = (w - rem * 1.8f - rem * 0.8f) * 0.5f, bh = UiKit.H("btn_h") * 1.5f;
            Button s = PopupKit.Btn(row, "sell", "판매 🪙 +" + NumFmt.Fmt(h.GearSys.SellPrice(sold)), "pp_red", "pp_red_dk", () => h.OnSellConfirm(), bw, bh, "stage_ink", TextKind.Sub);
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
            ct.color = ForgeUi.InkOf(Color.white) ;
            ct.color = (0.2126f * ac.r + 0.7152f * ac.g + 0.0722f * ac.b) > 0.5f ? Color.black : Color.white;
            TextMeshProUGUI nm = UiKit.Text(col, "name", TextKind.Sub, it.Name, "pp_ink");
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

        static RectTransform CraftCard(Transform parent, ForgeHost h, ForgeItem it, float size)
        {
            RectTransform tile = ForgeUi.ItemTile(parent, "card", size, h.Defs, it.Age, ForgeUi.ItemIconKey(h.Defs, it), 0.9f);
            return tile;
        }

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
            RectTransform card = CraftCard(reveal, h, item, size);
            UiKit.Place(card, a.x - size * 0.5f, a.y - size * 1.1f, size, size);
            h.Delay(ForgeHost.RevealCardSec, () => { DismissReveal(); done(); });
        }

        /// <summary>원작 showAutoDropCard — 필터 탈락 장비를 모루 위에 잠깐(0.62초).</summary>
        public static void ShowAutoDropCard(ForgeHost h, ForgeItem item, Action done)
        {
            DismissReveal();
            reveal = Overlay("auto-drop-card");
            float size = PopupKit.Rem * 3.7f;
            Vector2 a = AnvilTop();
            RectTransform card = CraftCard(reveal, h, item, size);
            UiKit.Place(card, a.x - size * 0.5f, a.y - size * 1.1f, size, size);
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
            dim.color = new Color(0f, 0f, 0f, 0.42f);
            dim.raycastTarget = true;
            int cols = items.Count <= 4 ? items.Count : items.Count <= 9 ? 3 : 4;
            int rows = (items.Count + cols - 1) / cols;
            float size = PopupKit.Rem * 3.7f, gap = PopupKit.Rem * 0.5f;
            float gw = cols * size + (cols - 1) * gap, gh = rows * size + (rows - 1) * gap;
            RectTransform grid = UiKit.Box(batch, "cb-grid");
            UiKit.Anchor(grid, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, gw, gh);
            for (int i = 0; i < items.Count; i++)
            {
                RectTransform card = CraftCard(grid, h, items[i], size);
                card.name = "cb-card-" + i;
                UiKit.Place(card, (i % cols) * (size + gap), (i / cols) * (size + gap), size, size);
            }
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
}
