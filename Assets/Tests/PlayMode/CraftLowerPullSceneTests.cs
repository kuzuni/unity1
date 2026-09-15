using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Forging;
using Forge.Core.Pets;
using Forge.Core.Save;
using CoreRng = Forge.Core.Data.Rng;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T390 — 제작 비교 팝업은 하단 앵커라 «버튼 아래 여백» 이 카드 높이와 위끝을 민다(정본 `style.css` 1781 주석). 정본은 그 여백을
    /// `.row` padding-bottom 1.44rem + (`.modal-card` 패딩 1.1rem − `.cmp-lower` margin-bottom .85rem) 으로 못박았다(1819·1821).
    /// 런 723 실측: 클론 63px ↔ 원작 26px(540×960). 여기서는 실물 배치에서 [판매] 아래끝 → 카드 바닥이 그 합(표값)인지 잰다.
    /// </summary>
    public class CraftLowerPullSceneTests
    {
        static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null; yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            yield return null;
        }

        /// <summary>T149 — 카드 팝(scale .7 → 1)이 끝날 때까지 프레임을 넘긴다(연출 중간값을 재지 않는다).</summary>
        static IEnumerator SettleCardPop()
        {
            for (int i = 0; i < 600 && Object.FindObjectsByType<CardPop>(FindObjectsSortMode.None).Length > 0; i++)
                yield return null;
            Assert.AreEqual(0, Object.FindObjectsByType<CardPop>(FindObjectsSortMode.None).Length, "카드 팝이 600프레임 안에 안 끝났다(T149)");
        }

        [UnityTest]
        public IEnumerator 판매_버튼_아래끝에서_카드_바닥까지는_정본_1_44rem_더하기_카드_패딩_빼기_당김이다()
        {
            yield return Boot();
            ForgeHost F = ForgeHost.Instance;
            ForgeItem it = F.Engine.RollItem();
            it.Subs = SubstatRoll.Roll(F.Defs, CoreRng.Mulberry(43224), 2);
            ForgeCraftPopup.Show(F, it);
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return SettleCardPop();
            Canvas.ForceUpdateCanvases();
            Popup p = F.Meta.Popups.Find(ForgeCraftPopup.Name);
            Assert.IsNotNull(p, "비교 팝업이 열렸다");
            RectTransform card = (RectTransform)p.Root.Find("card");
            RectTransform lower = (RectTransform)card.Find("lower");
            RectTransform row = (RectTransform)lower.Find("row");
            RectTransform sell = (RectTransform)row.Find("sell");
            Assert.IsNotNull(sell, "판매 버튼");

            float rem = PopupKit.Rem;
            float pull = CraftStyle.Px("cmp_lower_pull_rem"), rowPad = CraftStyle.Px("cmp_row_pad_bottom_rem"), cardPad = UiKit.H("card_pad");
            // ① 카드 층의 아래 패딩 = card_pad − 당김(정본 1.1rem − .85rem = .25rem · 양수)
            VerticalLayoutGroup cardLg = card.GetComponent<VerticalLayoutGroup>();
            Assert.AreEqual(Mathf.RoundToInt(cardPad - pull), cardLg.padding.bottom, "카드 아래 패딩 = card_pad − 당김");
            Assert.Greater(cardLg.padding.bottom, 0, "당김이 패딩을 넘지 않는다");
            Assert.AreEqual(Mathf.RoundToInt(cardPad + rem * 0.5f), cardLg.padding.top, "위 패딩은 안 건드린다");
            // ② 회색 패널 자신의 아래 패딩 0(정본 .cmp-lower 에 padding 없음)
            Assert.AreEqual(0, lower.GetComponent<VerticalLayoutGroup>().padding.bottom, "패널 아래 패딩 0");
            // ③ 줄 높이 = 버튼 + 1.44rem
            Assert.AreEqual(sell.rect.height + rowPad, row.rect.height, 0.6f, "줄 높이 = 버튼 높이 + 정본 padding-bottom 1.44rem");
            // ④ 실물: [판매] 아래끝 → 카드 바닥 = 1.44rem + (card_pad − 당김)
            Vector3[] sc = new Vector3[4]; sell.GetWorldCorners(sc);
            float sellBottomInCard = card.InverseTransformPoint(sc[0]).y;
            float gap = sellBottomInCard - card.rect.yMin;
            float want = rowPad + Mathf.RoundToInt(cardPad - pull);
            Assert.AreEqual(want, gap, 1.5f, "판매 버튼 아래끝 → 카드 바닥(기준 px) — 정본 26px@960 ≈ " + (want * 960f / UiKit.RefH).ToString("0.0") + "px@960");
            ForgeCraftPopup.Hide(F);
            yield return null;
        }
    }
}
