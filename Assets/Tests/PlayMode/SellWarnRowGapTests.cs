using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Forging;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T482 1회차 — 판매 경고 카드의 버튼 줄(정본 `ui.js` 3864 `<div class="row">` · 판매 · 취소)은 정본 `style.css` 659
    /// `.row { display: flex; gap: .45rem }` 의 가로 흐름이다: 두 버튼이 **같은 폭**으로 줄을 나눠 갖고 사이 틈은 표 `sellwarn_row_gap_rem`(.45rem).
    /// 전엔 `rem*0.8f` 가 코드에 박혀 있었다(T33 58회차 실측 · +12.6px).
    /// </summary>
    public class SellWarnRowGapTests
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

        [Test]
        public void 표는_판매_경고_버튼_줄의_틈을_정본_45rem_으로_쥔다()
        {
            Assert.AreEqual(0.45f, CraftStyle.L("sellwarn_row_gap_rem"), 1e-6f, "style.css 659 `.row { gap: .45rem }`");
        }

        [UnityTest]
        public IEnumerator 판매_경고의_판매_취소_버튼은_같은_폭이고_사이_틈은_표값이다()
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
            RectTransform row = (RectTransform)FindIn(p.Root, "row");
            Assert.IsNotNull(row, "버튼 줄(row)");
            RectTransform sell = (RectTransform)row.Find("sell"), cancel = (RectTransform)row.Find("cancel");
            Assert.IsNotNull(sell, "판매 버튼"); Assert.IsNotNull(cancel, "취소 버튼");
            float gap = CraftStyle.Px("sellwarn_row_gap_rem");
            float sellRight = sell.anchoredPosition.x + sell.rect.width;
            Assert.AreEqual(gap, cancel.anchoredPosition.x - sellRight, 0.5f, "두 버튼 사이 = 표 sellwarn_row_gap_rem(정본 .45rem) — 전엔 .8rem 박힘");
            Assert.AreEqual(sell.rect.width, cancel.rect.width, 0.5f, "두 버튼은 같은 폭(정본 `.row` flex 균등)");
            float rem = PopupKit.Rem;
            float rowW = UiKit.RefW * 0.76f - PopupKit.Line3 * 2f - rem * 1.8f;   // 카드 몸 − 좌우 패딩 .9rem×2(2218)
            Assert.AreEqual(rowW, sell.rect.width * 2f + gap, 1.0f, "두 버튼 + 틈 = 줄 폭(카드 몸 − 패딩)");
            PopupLayer.Instance.Hide(ForgeCraftPopup.SellName);
            yield return null;
        }
    }
}
