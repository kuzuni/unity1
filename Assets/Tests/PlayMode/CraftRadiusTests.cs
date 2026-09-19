using System.Collections;
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
    /// T415 18회차 — 제작 팝업의 둥근 모서리 넷 + 공용 팝업 버튼: 정본 1819 `.cmp-lower` .7 · 2222 `.swc-col` .55 · 2230 `.swc-age` .45 ·
    /// 3566 `#craft-modal .row .btn` .85 · 3542 `.modal-card .btn` .7(PopupKit.Btn 기본) · 4341 `.petup-icon` .6 — 전부 표에서 읽고, 화면의 둥근 면이 그 배율로 선다.
    /// </summary>
    public class CraftRadiusTests
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
        public void 표는_제작_팝업_넷과_공용_버튼과_펫_업그레이드_아이콘의_반지름을_정본_값_그대로_쥔다()
        {
            float rem = RadiusUi.PxPerRem;
            Assert.AreEqual(0.7f, RadiusUi.Px("cmp_lower_r_rem") / rem, 1e-4f, "style.css 1819 .cmp-lower .7rem");
            Assert.AreEqual(0.55f, RadiusUi.Px("swc_col_r_rem") / rem, 1e-4f, "style.css 2222 .swc-col .55rem");
            Assert.AreEqual(0.45f, RadiusUi.Px("swc_age_r_rem") / rem, 1e-4f, "style.css 2230 .swc-age .45rem");
            Assert.AreEqual(0.85f, RadiusUi.Px("craft_row_btn_r_rem") / rem, 1e-4f, "style.css 3566 #craft-modal .row .btn .85rem");
            Assert.AreEqual(0.7f, RadiusUi.Px("modal_btn_r_rem") / rem, 1e-4f, "style.css 3542 .modal-card .btn .7rem");
            Assert.AreEqual(0.6f, PetSkillStyle.L("petup_icon_r_rem"), 1e-6f, "style.css 4341 .petup-icon .6rem");
        }

        [UnityTest]
        public IEnumerator 판매_경고_열_칩_버튼과_제작_비교_패널_버튼이_표_반지름으로_선다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeItem a = h.Engine.RollItem(), b = h.Engine.RollItem();
            ForgeCraftPopup.ShowSellConfirm(h, a, b);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Popup p = PopupLayer.Instance.Find(ForgeCraftPopup.SellName);
            Assert.IsNotNull(p, "판매 경고 팝업");
            Transform sold = FindIn(p.Root, "sold");
            Assert.IsNotNull(sold, "파는 것 열");
            AssertRadius(sold.Find("bg"), "swc_col_r_rem", "열 판(.swc-col)");
            AssertRadius(FindIn(sold, "age").Find("bg"), "swc_age_r_rem", "시대 칩(.swc-age)");
            Transform sellBtn = FindIn(p.Root, "sell");
            AssertRadius(sellBtn.Find("line"), "modal_btn_r_rem", "팝업 공용 버튼 테(.modal-card .btn)");
            PopupLayer.Instance.Hide(ForgeCraftPopup.SellName);
            yield return null;

            ForgeItem it = h.Engine.RollItem();
            ForgeCraftPopup.Show(h, it);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "제작 비교 팝업");
            Transform root = h.Meta.Popups.Find(ForgeCraftPopup.Name).Root;
            Transform lower = FindIn(root, "lower");
            Assert.IsNotNull(lower, "하부 회색 패널(lower)");
            AssertRadius(lower.Find("face"), "cmp_lower_r_rem", "하부 패널 면(.cmp-lower)");
            Transform rowSell = FindIn(root, "row");
            Assert.IsNotNull(rowSell, "버튼 줄(row)");
            AssertRadius(rowSell.Find("sell/line"), "craft_row_btn_r_rem", "[판매] 큰 버튼 테(#craft-modal .row .btn)");
            AssertRadius(rowSell.Find("equip/line"), "craft_row_btn_r_rem", "[장착] 큰 버튼 테");
            ForgeCraftPopup.Hide(h);
            yield return null;
        }
    }
}
