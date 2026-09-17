using System.Collections;
using NUnit.Framework;
using TMPro;
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
    /// T434 — 장비 카드의 **안쪽** 패딩이 정본 세 갈래대로인가(<see cref="ForgeUi"/>.ItemCard).
    ///
    /// 정본은 셋으로 적어 뒀고 **위 값은 셋 다 같다**:
    /// 바탕 `.cmp-card { padding: **.9rem** .7rem .7rem }`(style.css 1825) ·
    /// 장착 `.cmp-card-wrap.cur .cmp-card { padding: .9rem .4rem **.4rem** }`(1812) ·
    /// 새 장비 `.cmp-card-wrap.new .cmp-card { padding-bottom: **1.5rem** }`(1815 · 위·좌우는 바탕 그대로).
    ///
    /// ⚑ **이 자가 서 있는 까닭**(1·2회차의 값비싼 교훈 · 결정 753): 그 전엔 이 어긋남을 «위가 모자란다» 로 읽고
    /// **모달 층**의 위 패딩을 키웠다가 화면이 반대로 가서 되돌렸다 — 두 카드가 «내용 높이 + 고정 바닥» 이라
    /// 위를 더하면 내용이 내려가는 게 아니라 **카드가 위로 자란다**. 고칠 자리는 카드 **안쪽**이고
    /// 하는 일은 더하기가 아니라 **재분배**다(위 +0.3rem · 아래 −0.4rem). 그래서 이 자는 위·아래를 **같이** 본다 —
    /// 위만 재면 1회차가 통과시킨 그 상태(위는 맞고 카드가 부푼)를 또 초록으로 넘긴다.
    /// </summary>
    public class CmpCardPadTests
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

        /// <summary>카드 위끝 → 첫 글줄(이름) 상자 위끝 · 마지막 글줄 아래끝 → 카드 아래끝을 카드 제 자리에서 잰다.</summary>
        private static void AssertPad(RectTransform card, float pt, float pb, string what)
        {
            RectTransform nm = (RectTransform)card.Find("name");
            Assert.IsNotNull(nm, what + ": 이름줄");
            RectTransform last = null;
            for (int i = 0; ; i++)
            {
                Transform s = card.Find("sub-" + i);
                if (s == null) break;
                last = (RectTransform)s;
            }
            Assert.IsNotNull(last, what + ": 부 옵션 줄(글 블록이 카드 높이를 정하는 갈래여야 잰다)");

            Vector3[] cc = new Vector3[4], nc = new Vector3[4], lc = new Vector3[4];
            card.GetWorldCorners(cc); nm.GetWorldCorners(nc); last.GetWorldCorners(lc);
            float cardTop = card.InverseTransformPoint(cc[1]).y, cardBot = card.InverseTransformPoint(cc[0]).y;
            float nameTop = card.InverseTransformPoint(nc[1]).y, lastBot = card.InverseTransformPoint(lc[0]).y;

            Assert.AreEqual(pt, cardTop - nameTop, 0.6f, what + ": 위 패딩 = 정본 .9rem(1825 · 세 갈래 공통) — 종전 클론은 0.6rem 이었다");
            Assert.AreEqual(pb, lastBot - cardBot, 0.6f, what + ": 아래 패딩 = 정본 값 — 종전 클론은 0.8rem(cur)·2.0rem(new) 이었다");
        }

        [UnityTest]
        public IEnumerator 장비_카드_안쪽_패딩은_정본_세_갈래대로고_위는_셋이_같다()
        {
            yield return Boot();
            PlayLog log = PlayLog.Start("cmp-card-pad");
            ForgeHost h = ForgeHost.Instance;
            float pt = CraftStyle.Px("cmp_card_pt_rem");

            h.S.Hammers = 10; h.Pull();
            h.OnCraft();
            float t = 0f;
            while (!h.Meta.Popups.IsOpen(ForgeCraftPopup.Name) && t < 8f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "제작 뒤 비교 팝업이 열린다");
            Canvas.ForceUpdateCanvases();
            Transform root = h.Meta.Popups.Find(ForgeCraftPopup.Name).Root;

            // ⓐ 새 장비 카드(`.cmp-card-wrap.new`) — 아래만 1.5rem 으로 넓다(정본 1815 · 그 아래가 [판매][장착] 줄과의 틈이다).
            RectTransform newCard = (RectTransform)FindIn(root, "new");
            Assert.IsNotNull(newCard, "새 장비 카드");
            AssertPad(newCard, pt, CraftStyle.Px("cmp_card_pb_new_rem"), "새 장비 카드");

            // ⓑ 위 카드(`.cur`) — 같은 팝업 안에서 **아래 값만 다르다**. 한 자에서 나란히 봐야 «한 값으로 뭉갰다» 가 걸린다.
            RectTransform curCard = (RectTransform)FindIn(root, "cur");
            if (curCard != null && curCard.Find("sub-0") != null)
                AssertPad(curCard, pt, CraftStyle.Px("cmp_card_pb_cur_rem"), "제작 비교 장착 카드");

            Assert.AreNotEqual(CraftStyle.L("cmp_card_pb_cur_rem"), CraftStyle.L("cmp_card_pb_new_rem"),
                               "정본은 두 갈래의 **아래**를 다르게 적었다(.4rem ↔ 1.5rem) — 한 키로 묶으면 안 된다");

            // T434 4회차 — 두 카드를 벌리는 것은 **각자의 안쪽 패딩**이지 층의 틈이 아니다: 정본 1783 `.cmp-wrap { gap: **0** }`
            //   (윗줄 주석 «하단 앵커라 gap 을 키우면 위 카드가 올라간다 … 안쪽 패딩으로 이미 벌어져 있으므로 0»).
            //   `cmp_card_pt_rem`·`cmp_card_pb_*` 와 **한 벌**이라 여기서 같이 본다 — 누가 «두 카드가 붙었다» 며 틈을 되살리면 이 줄이 먼저 운다.
            VerticalLayoutGroup wrapLg = ((RectTransform)newCard.parent.parent).GetComponent<VerticalLayoutGroup>()
                                         ?? ((RectTransform)FindIn(root, "card")).GetComponent<VerticalLayoutGroup>();
            Assert.IsNotNull(wrapLg, "카드 세로 층");
            Assert.AreEqual(CraftStyle.Px("cmp_wrap_gap_rem"), wrapLg.spacing, 0.5f, "`.cmp-wrap` 의 층 틈 = 정본 gap 0 — 하단 앵커라 이 틈이 곧 카드 위끝을 민다");
            Assert.AreEqual(0f, CraftStyle.L("cmp_wrap_gap_rem"), 1e-4f, "표도 0 이다(정본 1783)");

            ForgeItem item = h.Pending;
            h.ResolveCraft("equip");
            yield return null;
            if (h.Meta.Popups.IsOpen(ForgeCraftPopup.Name)) { h.ResolveCraft("sell"); yield return null; if (h.Meta.Popups.IsOpen(ForgeCraftPopup.SellName)) { h.OnSellConfirm(); yield return null; } }

            // ⓒ 장비 상세도 같은 조각이다(정본 ui.js 3296 이 같은 `.cmp-wrap` 으로 세운다) — `cur` 값이 그대로 걸린다.
            Assert.IsNotNull(item, "제작된 장비");
            GearDetailPopup.Open(h, item.Slot);
            yield return null;
            Canvas.ForceUpdateCanvases();
            RectTransform gearCard = (RectTransform)FindIn(h.Meta.Popups.Find(GearDetailPopup.Name).Root, "cur");
            Assert.IsNotNull(gearCard, "장비 상세 카드");
            if (gearCard.Find("sub-0") != null)
                AssertPad(gearCard, pt, CraftStyle.Px("cmp_card_pb_cur_rem"), "장비 상세 카드");

            GearDetailPopup.Close(h);
            yield return null;
            log.AssertNoRed();
            log.Dispose();
        }
    }
}
