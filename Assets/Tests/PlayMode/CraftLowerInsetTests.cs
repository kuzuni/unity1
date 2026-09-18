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
            // 코드가 `cur`·`lower` 에 건네는 폭의 셈 그대로(float) — 층 패딩이 RectOffset(int) 라 아래 `contentW` 와 반올림 몫만큼 갈린다(2·3회차가 좇던 그 몫).
            float inner = CraftStyle.Px("card_w") - PopupKit.Line3 * 2f - (UiKit.H("card_pad") + rem * 0.5f) * 2f;   // T473 — 카드 rect = 몸 − 테 두 겹
            // 층이 실제로 아이를 눕히는 폭 — 패딩이 RectOffset(int) 라 위 float 셈과 반올림 몫만큼 갈린다(런 999 실측 0.653px = 한쪽 0.327).
            float contentW = card.rect.width - cardLg.padding.left - cardLg.padding.right;
            Assert.AreEqual(contentW, inner, 1.0f, "층 패딩(int)으로 센 내용 폭과 반올림 몫 안에서 같다");
            // ① 패널 폭 = 내용 폭 + 당김×2 — «장착됨» 카드(cur)는 내용 폭 그대로(카드 층 패딩은 안 건드린다)
            //    `lower` 는 LayoutElement.minWidth = panelW 가 내용 폭보다 넓어 그 값을 지킨다 → float 셈으로 잰다.
            Assert.AreEqual(inner + pull * 2f, lower.rect.width, 0.6f, "회색 패널 폭 = 카드 내용 폭 + .85rem×2");
            // 4회차 — `cur` 는 다르다: preferredWidth(=inner)가 내용 폭보다 **좁아** `childForceExpandWidth` 가 내용 폭까지 늘린다.
            //    곧 이 칸이 재야 할 것은 «float inner» 가 아니라 **층이 눕힌 그 폭**이다(런 999 는 0.653 차로 ±0.6 자를 넘겨 빨갰다 —
            //    바로 윗줄이 같은 반올림을 1.0 으로 이미 허용하고 있었으니 자가 제 안에서 서로 어긋나 있었다). 자를 느슨하게 푸는 대신 **묻는 것을 바꾼다**.
            Assert.AreEqual(contentW, cur.rect.width, 0.1f, "장착됨 카드는 층이 눕힌 내용 폭 그대로(카드 층 패딩을 안 건드린다)");
            // ② 실물: 카드 왼끝 → 패널 왼끝 = 카드 좌 패딩 − 당김 · 오른쪽도 같다(카드 층이 가운데 맞춰 반씩)
            float leftGap = X(lower, card, 0) - X(card, card, 0);
            float rightGap = X(card, card, 3) - X(lower, card, 3);
            Assert.AreEqual(cardLg.padding.left - pull, leftGap, 1.0f, "카드 왼끝 → 패널 왼끝 = 좌 패딩 − 당김(정본 인셋 7px)");
            Assert.AreEqual(leftGap, rightGap, 1.0f, "좌우 인셋이 같다");
            Assert.Greater(leftGap, 0f, "당김이 카드 패딩을 넘지 않는다(패널이 카드 밖으로 안 나간다)");
            // ③ 버튼 줄은 패널 안폭에서 나고, 버튼 폭은 코드와 같은 셈(패널 폭 − .8rem − 1.3rem − 1.9rem)/2 — 정본 1821 `.row { margin: … .96rem; gap: 1.3rem }` · `.btn { flex: 1 }`.
            //    3회차 — 패널 안 패딩도 RectOffset(int) 라 «패널 − .8rem» 과 «좌우 여백이 같다» 는 1080×1920 에서 0.9·1.6px 어긋난다(반올림 + 1.9 vs .96×2). 셈을 코드와 글자 그대로 맞춘다.
            VerticalLayoutGroup lg = lower.GetComponent<VerticalLayoutGroup>();
            float rowW = row.rect.width;
            Assert.AreEqual(lower.rect.width - lg.padding.left - lg.padding.right, rowW, 0.6f, "줄 폭 = 패널 폭 − 패널 안 패딩(int)");
            Assert.AreEqual(rem * 0.8f, lg.padding.left + lg.padding.right, 1.0f, "패널 안 패딩 = .4rem×2(반올림 안)");
            float wantBw = (inner + pull * 2f - rem * 0.8f - rem * 1.3f - rem * 1.9f) * 0.5f;
            Assert.AreEqual(wantBw, sell.rect.width, 0.6f, "판매 폭 = (패널 − .8 − 1.3 − 1.9rem)/2");
            Assert.AreEqual(wantBw, equip.rect.width, 0.6f, "장착 폭 = 판매 폭");
            float sellL = X(sell, row, 0) - X(row, row, 0), gap13 = X(equip, row, 0) - X(sell, row, 3), equipR = X(row, row, 3) - X(equip, row, 3);
            Assert.AreEqual(rem * 0.96f, sellL, 0.6f, "판매 왼끝은 줄 왼끝에서 .96rem");
            Assert.AreEqual(rem * 1.3f, gap13, 0.6f, "판매 오른끝 → 장착 왼끝 = 정본 gap 1.3rem");
            Assert.AreEqual(rem * 0.96f, equipR, 2.0f, "장착 오른 여백도 .96rem 언저리(코드의 1.9 ↔ .96×2 = .02rem + 패딩 반올림 몫 안)");
            Assert.Greater(sell.rect.width, (inner - rem * 4.0f) * 0.5f + pull * 0.5f, "버튼이 옛 폭((inner − 4rem)/2)보다 당김의 절반 이상 넓다 — 줄이 패널 폭을 따랐다");
            ForgeCraftPopup.Hide(F);
            yield return null;
        }
    }
}
