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
    /// T434 — 장비 카드를 품는 조각(`.cmp-wrap` + `.cmp-card-wrap.cur .cmp-card`)의 **위 여백이 세 겹**인가.
    ///
    /// 정본은 이 카드를 세 겹으로 띄운다 — 모달 패딩 **1.1rem**(`card_pad`) + `.cmp-wrap { margin-top: .5rem }`(style.css **1783**)
    /// + `.cmp-card-wrap.cur .cmp-card { padding: .9rem … }`(**1812**) = **2.5rem**. 클론은 뒤 두 겹을 **한 어림수**로 합쳐
    /// (`card_pad + rem*0.6` = 1.7rem · `pad + rem*0.5` = 1.6rem) 0.8~0.9rem 짧았다.
    ///
    /// **두 화면이 같은 조각을 쓴다**: 정본 `ui.js` 3296 이 장비 상세를 `<div class="cmp-wrap">${itemCardHTML(…)}</div>` 로 세우고
    /// 제작 비교의 위 카드도 같은 조각이다 — 그래서 값은 `CraftUi.json` **한 표**가 쥐고 이 자도 **두 자리를 나란히** 본다.
    /// 한쪽만 고치면 다음 사람이 «저 화면은 왜 다르지» 로 되돌린다.
    ///
    /// ⚠ **좌우·아래는 안 움직인다**는 것을 같이 본다: `PopupKit.Column` 의 둘째 인자는 네 변에 똑같이 걸려서,
    /// 위를 키우려고 그 인자를 올리면 **좌우까지 커져 카드 안 내용 폭이 줄고**(장비 상세) 회색 패널 폭의 밑동이 어긋난다(제작 비교 · T429).
    /// 그래서 «위가 정본대로다» 만 묻는 자는 이 절의 절반만 지킨다.
    /// </summary>
    public class CmpCardTopPadTests
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

        /// <summary>정본 세 겹 = `card_pad` + `cmp_wrap_mt_rem`(.5) + `cmp_card_pt_rem`(.9). 수치는 표에서만 읽는다(§1).</summary>
        private static float WantTop()
        {
            return UiKit.H("card_pad") + CraftStyle.Px("cmp_wrap_mt_rem") + CraftStyle.Px("cmp_card_pt_rem");
        }

        private static VerticalLayoutGroup CardColumn(Transform popupRoot, string what)
        {
            Transform card = FindIn(popupRoot, "card");
            Assert.IsNotNull(card, what + ": 카드");
            VerticalLayoutGroup lg = card.GetComponent<VerticalLayoutGroup>();
            Assert.IsNotNull(lg, what + ": 카드 세로 층(PopupKit.Column)");
            return lg;
        }

        [UnityTest]
        public IEnumerator 장비_상세와_제작_비교의_위_여백은_정본_세_겹이고_좌우는_안_움직인다()
        {
            yield return Boot();
            PlayLog log = PlayLog.Start("cmp-card-top-pad");
            ForgeHost h = ForgeHost.Instance;
            float rem = PopupKit.Rem, pad = UiKit.H("card_pad");
            float want = WantTop();

            // 표가 정본 두 값을 따로 쥐는가 — 한 어림수로 합쳐 두면 이 줄이 먼저 깨진다(그것이 이 절이 고친 병이다).
            Assert.AreEqual(0.5f, CraftStyle.L("cmp_wrap_mt_rem"), 1e-4f, "표 cmp_wrap_mt_rem = 정본 1783 .cmp-wrap margin-top .5rem");
            Assert.AreEqual(0.9f, CraftStyle.L("cmp_card_pt_rem"), 1e-4f, "표 cmp_card_pt_rem = 정본 1812 .cmp-card padding-top .9rem");
            Assert.AreEqual(pad + rem * 1.4f, want, 0.5f, "세 겹 합 = 카드 패딩 1.1rem + .5rem + .9rem = 2.5rem");

            // ⓐ 제작 비교(하단 앵커) — 제작해서 연다.
            h.S.Hammers = 10; h.Pull();
            h.OnCraft();
            float t = 0f;
            while (!h.Meta.Popups.IsOpen(ForgeCraftPopup.Name) && t < 8f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "제작 뒤 비교 팝업이 열린다");
            Canvas.ForceUpdateCanvases();
            VerticalLayoutGroup craftLg = CardColumn(h.Meta.Popups.Find(ForgeCraftPopup.Name).Root, "제작 비교");
            Assert.AreEqual(want, craftLg.padding.top, 0.5f, "제작 비교 위 여백 = 정본 세 겹(종전 pad + rem*0.5 = 1.6rem 은 0.9rem 짧다)");
            Assert.AreEqual(Mathf.RoundToInt(pad + rem * 0.5f), craftLg.padding.left, "좌우는 안 움직인다 — 키우면 위 «장착됨» 층까지 좁아진다");
            Assert.AreEqual(craftLg.padding.left, craftLg.padding.right, "좌우가 서로 같다");
            // 아래는 T390·T429 의 자리(card_pad − 당김) — 이 절이 건드리지 않았다는 것을 같이 못 박는다.
            Assert.AreEqual(Mathf.RoundToInt(pad - CraftStyle.Px("cmp_lower_pull_rem")), craftLg.padding.bottom, "아래 여백은 T390·T429 의 당김 그대로다");

            ForgeItem item = h.Pending;
            Assert.IsNotNull(item, "제작된 장비");
            h.ResolveCraft("equip");
            yield return null;
            if (h.Meta.Popups.IsOpen(ForgeCraftPopup.Name)) { h.ResolveCraft("sell"); yield return null; if (h.Meta.Popups.IsOpen(ForgeCraftPopup.SellName)) { h.OnSellConfirm(); yield return null; } }

            // ⓑ 장비 상세(상단 고정) — 같은 조각이므로 **같은 값**이어야 한다.
            GearDetailPopup.Open(h, item.Slot);
            yield return null;
            Assert.IsTrue(h.Meta.Popups.IsOpen(GearDetailPopup.Name), "장비 세부정보 팝업이 열린다");
            Canvas.ForceUpdateCanvases();
            VerticalLayoutGroup gearLg = CardColumn(h.Meta.Popups.Find(GearDetailPopup.Name).Root, "장비 상세");
            Assert.AreEqual(want, gearLg.padding.top, 0.5f, "장비 상세 위 여백 = 정본 세 겹(종전 card_pad + rem*0.6 = 1.7rem 은 0.8rem 짧다)");
            Assert.AreEqual(Mathf.RoundToInt(pad + rem * 0.6f), gearLg.padding.left, "좌우는 안 움직인다 — 키우면 카드 안 내용 폭(− rem*1.2)이 어긋나 카드 폭이 움직인다");
            Assert.AreEqual(gearLg.padding.left, gearLg.padding.right, "좌우가 서로 같다");

            // 두 화면이 **같은 조각**이라는 것 — 위 여백이 서로 같다(한쪽만 고치면 여기서 운다).
            Assert.AreEqual(craftLg.padding.top, gearLg.padding.top, "두 화면은 같은 `.cmp-wrap` 조각이라 위 여백이 같다(정본 ui.js 3296)");

            GearDetailPopup.Close(h);
            yield return null;
            log.AssertNoRed();
            log.Dispose();
        }
    }
}
