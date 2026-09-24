using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Forging;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T415 19회차 — 장비 타일 공장(`ForgeUi.ItemTile`)과 그 둘레의 둥근 모서리 아홉: 정본 828 `.equip-cell` .7 · 728 `.forge-item-grid` .7 ·
    /// 990 `.anvil-btn.held-slot` .7 · 1026 `.deck::before` .7 · 1063 `.auto-drop-card` .7 · 1131 `.cb-card` .7 · 1825 `.cmp-card` .8 ·
    /// 1852 `.cmp-img` .55 · 3683 `.idet-icon` .55 — 종전 «크기 × .16» 한 리터럴이 네 자리를 다 어긋나게 했다(셀 .58 · 카드 .59 · 그림 .576).
    /// 전부 표(`RadiusUi.json`)에서 읽고, 화면의 둥근 테가 그 배율로 선다.
    /// </summary>
    public class TileRadiusTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            yield return null;
        }

        private static Transform FindIn(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        private static void AssertRadius(Transform t, string key, string what)
        {
            Assert.IsNotNull(t, what);
            Image img = t.GetComponent<Image>();
            Assert.IsNotNull(img, what + " Image");
            Assert.AreSame(UiShapes.Rounded, img.sprite, what + " 둥근 스프라이트");
            Assert.AreEqual(UiShapes.RoundedMultiplier(RadiusUi.Px(key)), img.pixelsPerUnitMultiplier, 1e-4f, what + " 반지름 = 표 " + key);
        }

        [Test]
        public void 표는_장비_칸_목록_판_보류_카드_결과_묶음_카드_비교_상세_타일의_반지름을_정본_값_그대로_쥔다()
        {
            float rem = RadiusUi.PxPerRem;
            Assert.AreEqual(0.7f, RadiusUi.Px("equip_cell_r_rem") / rem, 1e-4f, "style.css 828 .equip-cell .7rem");
            Assert.AreEqual(0.7f, RadiusUi.Px("forge_item_grid_r_rem") / rem, 1e-4f, "style.css 728 .forge-item-grid .7rem");
            Assert.AreEqual(0.7f, RadiusUi.Px("held_slot_r_rem") / rem, 1e-4f, "style.css 990 .anvil-btn.held-slot .7rem");
            Assert.AreEqual(0.7f, RadiusUi.Px("held_deck_r_rem") / rem, 1e-4f, "style.css 1026 .anvil-btn.held-slot.deck::before .7rem");
            Assert.AreEqual(0.7f, RadiusUi.Px("adc_card_r_rem") / rem, 1e-4f, "style.css 1063 .auto-drop-card .7rem");
            Assert.AreEqual(0.7f, RadiusUi.Px("cb_card_r_rem") / rem, 1e-4f, "style.css 1131 .craft-batch .cb-card .7rem");
            Assert.AreEqual(0.8f, RadiusUi.Px("cmp_card_r_rem") / rem, 1e-4f, "style.css 1825 .cmp-card .8rem");
            Assert.AreEqual(0.55f, RadiusUi.Px("cmp_img_r_rem") / rem, 1e-4f, "style.css 1852 .cmp-img .55rem");
            Assert.AreEqual(0.55f, RadiusUi.Px("idet_icon_r_rem") / rem, 1e-4f, "style.css 3683 .idet-icon .55rem");
        }

        /// <summary>정본 2090 «목록 타일은 .equip-cell CSS 를 그대로 입는다» — 장비 시트 칸(찬 칸·빈 칸)과 목록 타일이 같은 키, 목록 판은 제 키.</summary>
        [UnityTest]
        public IEnumerator 장비_시트_칸과_목록_타일은_equip_cell_반지름으로_서고_목록_판은_제_반지름으로_선다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeItem it = h.Engine.RollItem();
            h.Gear.Set(it.Slot, it);
            ForgeSheet.Render(h);
            yield return null;
            Transform full = null, empty = null;
            foreach (Transform t in UiRoot.Instance.Sheet.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "cell-" + it.Slot) full = t;
                else if (empty == null && t.name.StartsWith("cell-", System.StringComparison.Ordinal) && t.Find("slot-name") != null) empty = t;
            }
            Assert.IsNotNull(full, "장착 칸 cell-" + it.Slot);
            AssertRadius(full.Find("frame/line"), "equip_cell_r_rem", "장착 칸 테(.equip-cell)");
            if (empty != null) AssertRadius(empty.Find("frame/line"), "equip_cell_r_rem", "빈 칸 테(.equip-cell)");

            ForgeInfoPopup.OpenList(h);
            yield return null; yield return null;
            Popup p = h.Meta.Popups.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(p, "목록 팝업");
            int tiles = 0;
            foreach (RectTransform t in p.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (t.name != "fl-face") continue;
                AssertRadius(t.Find("frame/line"), "equip_cell_r_rem", "목록 타일 테(fl-face equip-cell)");
                tiles++;
            }
            Assert.Greater(tiles, 0, "목록 타일이 하나도 없다");
            Transform grid = FindIn(p.Root, "forge-item-grid");
            Assert.IsNotNull(grid, "목록 회색 판(forge-item-grid)");
            AssertRadius(grid.Find("bg"), "forge_item_grid_r_rem", "목록 판(.forge-item-grid)");
            ForgeInfoPopup.Close(h);
            yield return null;
        }

        /// <summary>같은 공장이 자리마다 제 줄의 키를 받는다 — 비교 카드 그림 .55 · 결과 리빌 카드 .7 · 묶음 카드 .7.</summary>
        [UnityTest]
        public IEnumerator 비교_카드_그림과_결과_카드와_묶음_카드는_제_줄의_반지름으로_선다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeItem it = h.Engine.RollItem();
            ForgeCraftPopup.Show(h, it);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "제작 비교 팝업");
            Transform root = h.Meta.Popups.Find(ForgeCraftPopup.Name).Root;
            Transform tile = FindIn(root, "tile");
            Assert.IsNotNull(tile, "비교 카드의 그림 타일(tile)");
            AssertRadius(tile.Find("frame/line"), "cmp_img_r_rem", "비교 카드 그림 테(.cmp-img)");
            ForgeCraftPopup.Hide(h);
            yield return null;

            ForgeCraftPopup.ShowReveal(h, it, () => { });
            yield return null; yield return null;
            Transform reveal = UiRoot.Instance.App.Find("craft-reveal");
            Assert.IsNotNull(reveal, "결과 겹(craft-reveal)");
            Transform card = FindIn(reveal, "card");
            Assert.IsNotNull(card, "결과 카드(card)");
            AssertRadius(card.Find("frame/line"), "adc_card_r_rem", "결과 카드 테(.auto-drop-card)");
            ForgeCraftPopup.DismissReveal();
            yield return null;

            var items = new List<ForgeItem> { h.Engine.RollItem(), h.Engine.RollItem() };
            ForgeCraftPopup.ShowBatch(h, items, () => { });
            yield return null; yield return null;
            Transform batch = UiRoot.Instance.App.Find("craft-batch");
            Assert.IsNotNull(batch, "묶음 겹(craft-batch)");
            Transform cb = FindIn(batch, "cb-card-0");
            Assert.IsNotNull(cb, "묶음 카드(cb-card-0)");
            AssertRadius(cb.Find("frame/line"), "cb_card_r_rem", "묶음 카드 테(.craft-batch .cb-card)");
            ForgeCraftPopup.DismissBatch();
            yield return null;
        }
    }
}
