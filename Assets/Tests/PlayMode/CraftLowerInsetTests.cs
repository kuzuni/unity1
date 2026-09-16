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
    /// T429 — 정본 `style.css` 1819 `.cmp-lower { margin: 0 -.85rem -.85rem }` 의 **좌우** 절반: 회색 하부 패널은 카드 내용 폭보다 당김×2 넓고
    /// 카드 좌우 패딩을 당김만큼 파고든다(정본 주석 «카드 좌우/하단에서 7px 인셋 · x90~415»). 런 955 실측은 왼 25·오른 26 이었다(아래 틈만 맞았다).
    /// 표 `cmp_lower_pull_rem` 하나가 아래(T390)·좌우(T429) 세 자리를 쥔다 — 여기서는 실물 배치에서 좌우 둘을 잰다.
    /// </summary>
    public class CraftLowerInsetTests
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

        static IEnumerator SettleCardPop()
        {
            for (int i = 0; i < 600 && Object.FindObjectsByType<CardPop>(FindObjectsSortMode.None).Length > 0; i++)
                yield return null;
            Assert.AreEqual(0, Object.FindObjectsByType<CardPop>(FindObjectsSortMode.None).Length, "카드 팝이 600프레임 안에 안 끝났다(T149)");
        }

        static float X(RectTransform rt, RectTransform inSpace, int corner)
        {
            Vector3[] c = new Vector3[4]; rt.GetWorldCorners(c);
            return inSpace.InverseTransformPoint(c[corner]).x;
        }

        [UnityTest]
        public IEnumerator 회색_패널은_카드_좌우_패딩을_당김만큼_파고들고_버튼_줄은_그_패널_폭에서_난다()
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
            RectTransform cur = (RectTransform)card.Find("cur");
            RectTransform lower = (RectTransform)card.Find("lower");
            RectTransform row = (RectTransform)lower.Find("row");
            RectTransform sell = (RectTransform)row.Find("sell");
            RectTransform equip = (RectTransform)row.Find("equip");
            Assert.IsNotNull(sell, "판매 버튼"); Assert.IsNotNull(equip, "장착 버튼");

            float rem = PopupKit.Rem;
            float pull = CraftStyle.Px("cmp_lower_pull_rem");
            VerticalLayoutGroup cardLg = card.GetComponent<VerticalLayoutGroup>();
            float inner = card.rect.width - cardLg.padding.left - cardLg.padding.right;
            // ① 패널 폭 = 내용 폭 + 당김×2 — «장착됨» 카드(cur)는 내용 폭 그대로(카드 층 패딩은 안 건드린다)
            Assert.AreEqual(inner + pull * 2f, lower.rect.width, 0.6f, "회색 패널 폭 = 카드 내용 폭 + .85rem×2");
            Assert.AreEqual(inner, cur.rect.width, 0.6f, "장착됨 카드는 내용 폭 그대로");
            // ② 실물: 카드 왼끝 → 패널 왼끝 = 카드 좌 패딩 − 당김 · 오른쪽도 같다(카드 층이 가운데 맞춰 반씩)
            float leftGap = X(lower, card, 0) - X(card, card, 0);
            float rightGap = X(card, card, 3) - X(lower, card, 3);
            Assert.AreEqual(cardLg.padding.left - pull, leftGap, 1.0f, "카드 왼끝 → 패널 왼끝 = 좌 패딩 − 당김(정본 인셋 7px)");
            Assert.AreEqual(leftGap, rightGap, 1.0f, "좌우 인셋이 같다");
            Assert.Greater(leftGap, 0f, "당김이 카드 패딩을 넘지 않는다(패널이 카드 밖으로 안 나간다)");
            // ③ 버튼 줄은 패널 안폭(패널 − .4rem×2)에서 좌우 .96rem·틈 1.3rem 을 뺀 나머지를 둘이 나눈다(정본 1821 · `.btn { flex: 1 }`)
            float rowW = row.rect.width;
            Assert.AreEqual(lower.rect.width - rem * 0.8f, rowW, 0.6f, "줄 폭 = 패널 폭 − 패널 안 패딩 .4rem×2");
            float wantBw = (rowW - rem * 1.9f - rem * 1.3f) * 0.5f;
            Assert.AreEqual(wantBw, sell.rect.width, 0.6f, "판매 폭");
            Assert.AreEqual(wantBw, equip.rect.width, 0.6f, "장착 폭");
            float sellL = X(sell, row, 0) - X(row, row, 0), equipR = X(row, row, 3) - X(equip, row, 3);
            Assert.AreEqual(rem * 0.96f, sellL, 1.0f, "판매 왼끝은 줄 왼끝에서 .96rem");
            Assert.AreEqual(sellL, equipR, 1.0f, "장착 오른 여백은 판매 왼 여백과 같다(줄이 패널 폭을 따라 넓어졌다)");
            ForgeCraftPopup.Hide(F);
            yield return null;
        }
    }
}
