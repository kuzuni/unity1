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
    /// T471 — 장비 상세 카드의 패딩은 정본 `.modal-card`(style.css 1752) 의 **1.1rem 하나**다(1738 `.gd-card` 는 폭만 정한다).
    /// 종전 클론은 `card_pad + 0.6rem` 을 네 변에 다 걸어 카드가 세로 +1.2rem 두꺼웠고, 하단 앵커라 그 몫이 전부 위끝으로 갔다(런 1166 −1.51%p).
    /// 그 안쪽에서 상쇄하던 −0.96%H 는 정본 1783 `.cmp-wrap { margin-top: .5rem }`(0.95%H)이 클론에 없던 것 — 층의 **위** 패딩에만 더한다(표 `cmp_wrap_mt_rem`).
    /// </summary>
    public class GearDetailPadTests
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

        [TearDown]
        public void CleanSave()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
        }

        private static RectTransform FindUnder(RectTransform parent, string objName)
        {
            foreach (RectTransform rt in parent.GetComponentsInChildren<RectTransform>(true))
                if (rt != parent && rt.name == objName) return rt;
            return null;
        }

        [UnityTest]
        public IEnumerator 장비_상세_카드의_패딩은_정본_1_1rem_이고_위에만_cmp_wrap_마진_5rem_이_더_든다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            h.S.Hammers = 10; h.Pull();
            h.OnCraft();
            float t = 0f;
            while (!h.Meta.Popups.IsOpen(ForgeCraftPopup.Name) && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "제작 뒤 비교 팝업이 떠야 한다");
            ForgeItem item = h.Pending;
            h.ResolveCraft("equip");
            yield return null;
            if (h.Meta.Popups.IsOpen(ForgeCraftPopup.Name)) { h.ResolveCraft("sell"); yield return null; if (h.Meta.Popups.IsOpen(ForgeCraftPopup.SellName)) { h.Meta.Popups.Hide(ForgeCraftPopup.SellName); yield return null; } }
            Assert.IsNotNull(h.Gear.Get(item.Slot), "장착됐다");

            GearDetailPopup.Open(h, item.Slot);
            yield return null;
            yield return null;
            CardPop.SettleAll();
            Popup p = h.Meta.Popups.Find(GearDetailPopup.Name);
            Assert.IsNotNull(p, "장비 세부정보 팝업");
            RectTransform card = FindUnder((RectTransform)p.Root, "card");
            Assert.IsNotNull(card, "카드");
            VerticalLayoutGroup lg = card.GetComponent<VerticalLayoutGroup>();
            Assert.IsNotNull(lg, "카드 층(VerticalLayoutGroup)");

            float rem = PopupKit.Rem;
            float pad = UiKit.H("card_pad");
            float wrapMt = CraftStyle.Px("cmp_wrap_mt_rem");
            Assert.AreEqual(0.5f, CraftStyle.L("cmp_wrap_mt_rem"), 1e-6f, "표 cmp_wrap_mt_rem = 정본 1783 .5rem");
            Assert.AreEqual(1.1f * rem, pad, 0.5f, "card_pad = 정본 1752 1.1rem");
            int p1 = Mathf.RoundToInt(pad), pTop = Mathf.RoundToInt(pad + wrapMt);
            Assert.AreEqual(p1, lg.padding.left, "왼쪽 패딩 = 1.1rem(종전 +0.6rem 없음)");
            Assert.AreEqual(p1, lg.padding.right, "오른쪽 패딩 = 1.1rem");
            Assert.AreEqual(p1, lg.padding.bottom, "아래 패딩 = 1.1rem");
            Assert.AreEqual(pTop, lg.padding.top, "위 패딩 = 1.1rem + .cmp-wrap margin-top .5rem");
            Assert.Less(pTop, Mathf.RoundToInt(pad + rem * 0.6f), "종전 한 수(+0.6rem)보다 작다");

            RectTransform cur = FindUnder(card, "cur");
            Assert.IsNotNull(cur, "장착 카드(cur)");
            Assert.AreEqual(card.rect.width - pad * 2f, cur.rect.width, 0.5f, "장착 카드 폭 = 카드 폭 − 1.1rem × 2(종전 −1.2rem 더 있었다)");
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(card);
            Assert.AreEqual(pTop + cur.rect.height + p1, card.rect.height, 1f, "카드 rect(패딩 상자) = 위 패딩 + 장착 카드 + 아래 패딩 · 실측 " + card.rect.height.ToString("0.0"));
            Debug.Log("[T471] 카드 rect " + card.rect.height.ToString("0.0") + "px(몸 " + (card.rect.height + PopupKit.Line3 * 2f).ToString("0.0") + " = " + ((card.rect.height + PopupKit.Line3 * 2f) / UiKit.RefH * 100f).ToString("0.00") + "%H) · 위 패딩 " + pTop + " · 장착 카드 " + cur.rect.height.ToString("0.0"));
            GearDetailPopup.Close(h);
            yield return null;
        }
    }
}
