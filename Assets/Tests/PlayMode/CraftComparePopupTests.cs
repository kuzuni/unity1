using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core;
using Forge.Core.Forging;
using Forge.Core.Pets;
using CoreRng = Forge.Core.Data.Rng;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T113 — 제작 비교 팝업 카드는 가운데가 아니라 **하단 앵커**(정본 `style.css` 1742 `#craft-modal { align-items:flex-end; padding-bottom: calc(var(--tabbar-h) + 1.65rem) }` · 원본 실측 카드 바닥 86.90%H)
    /// 이고 폭은 공용 74% 가 아니라 68.8%W(1748). 표는 `Resources/CraftUi.json`. `ForgeUiTests.cs` 는 남의 lock 이라 새 파일.
    /// 눈 확인은 촬영 자의 `screen_craft-compare.png` ↔ 원작 `shot-043224`.
    /// </summary>
    public class CraftComparePopupTests
    {

        /// <summary>T149 — 카드 팝(T135 `cardpop` · .25s ease-out · scale .7 → 1)이 끝날 때까지 프레임을 넘긴다.
        /// **시간을 어림하지 않는다**: 러너(<see cref="Forge.Game.Ui.CardPop"/>)는 끝나면 카드를 scale 1 로 돌리고 스스로 사라진다.
        /// 팝이 도는 동안 재면 폭이 «연출 중간값» 으로 나온다(런 341 실측: 70% → 57.95% · 68.8% → 62.5%) — 결정 277 과 같은 갈래다.</summary>
        static IEnumerator SettleCardPop()
        {
            for (int i = 0; i < 600 && Object.FindObjectsByType<CardPop>(FindObjectsSortMode.None).Length > 0; i++)
                yield return null;
            Assert.AreEqual(0, Object.FindObjectsByType<CardPop>(FindObjectsSortMode.None).Length, "카드 팝이 600프레임 안에 안 끝났다(T149)");
        }
        static IEnumerator Boot()
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

        const float BottomH = 0.869f, BottomTol = 0.015f;   // 원본 실측(ROUTINE T113) — 판정 문턱은 등재문 그대로
        const float CardW = 0.688f, WTol = 0.01f;

        [UnityTest]
        public IEnumerator 비교_카드는_하단_앵커_바닥_86_9퍼센트H_폭_68_8퍼센트W_다()
        {
            yield return Boot();
            ForgeHost F = ForgeHost.Instance;
            UiRoot root = UiRoot.Instance;
            // 촬영 자(UiShotsTests craft-compare)와 같은 열기 — 서브옵션 2줄 고정으로 카드 높이를 안정시킨다
            ForgeItem it = F.Engine.RollItem();
            it.Subs = SubstatRoll.Roll(F.Defs, CoreRng.Mulberry(43224), 2);
            ForgeCraftPopup.Show(F, it);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Popup p = F.Meta.Popups.Find(ForgeCraftPopup.Name);
            Assert.IsNotNull(p, "비교 팝업이 열렸다");
            RectTransform card = (RectTransform)p.Root.Find("card");
            Assert.IsNotNull(card, "카드");
            Assert.AreEqual(new Vector2(0.5f, 0f), card.anchorMin, "아래 앵커(정본 align-items: flex-end)");
            Assert.AreEqual(new Vector2(0.5f, 0f), card.pivot, "피벗 바닥 — 내용이 늘면 위로 자란다");
            Assert.AreEqual(CraftStyle.BottomPx() + PopupKit.Line3, card.anchoredPosition.y, 0.5f, "바닥 띄움 = 탭바 높이 + 1.65rem(표) + 테 한 겹(T465 — 표값은 카드 몸의 바닥 · rect 는 패딩 상자)");
            yield return SettleCardPop();   // T149 — 팝이 도는 동안 재면 폭이 연출 중간값이다
            // 실측: 앱 상자 안에서 카드 바닥·폭 분수
            Vector3[] a = new Vector3[4], c = new Vector3[4];
            root.App.GetWorldCorners(a); card.GetWorldCorners(c);
            float appW = a[2].x - a[0].x, appH = a[2].y - a[0].y;
            float bottomFrac = (a[2].y - c[0].y) / appH;   // 위에서 잰 카드 바닥
            float widthFrac = (c[2].x - c[0].x) / appW;
            Assert.AreEqual(BottomH, bottomFrac, BottomTol, "카드 바닥 = 86.9%H ±1.5(원본 shot-043224) — 지금 " + (bottomFrac * 100f).ToString("0.0") + "%");
            Assert.AreEqual(CardW, widthFrac, WTol, "카드 폭 = 68.8%W ±1(정본 1748) — 지금 " + (widthFrac * 100f).ToString("0.0") + "%");
            Assert.Greater(c[2].y - c[0].y, appH * 0.25f, "카드에 내용이 들어 높이가 있다(장착됨 카드 + 새 장비 + 버튼 줄)");
            Assert.Less((a[2].y - c[2].y) / appH, BottomH - 0.25f, "카드 위가 바닥보다 충분히 위 — 가운데 배치가 아니라 아래에서 위로 자란 꼴");
            Assert.AreEqual(0.688f, CraftStyle.L("card_w"), 1e-6f, "표 값 그대로"); Assert.AreEqual(1.65f, CraftStyle.L("bottom_rem"), 1e-6f);
            ForgeCraftPopup.Hide(F);
            yield return null;
            Assert.IsNull(F.Meta.Popups.Find(ForgeCraftPopup.Name), "닫힌다");
        }

        /// <summary>
        /// T156 2회차 — «장착됨» 리본이 **깃발**로 서는가(정본 `style.css` 1801 `.cmp-ribbon`).
        /// 모양·꼭짓점은 `RibbonArtTests` 가 표로 보고, 여기는 **실제 팝업에 그 깃발이 걸렸는가**를 본다:
        /// 옛 둥근 사각(`face` 한 장)이 아니라 `flag`(테두리 + 종이 두 겹)이고, 폭이 앱 폭의 20.3% 이며,
        /// 글자는 가운데 정렬인데 **상자가 비대칭 패딩만큼 왼쪽으로 좁혀져** 있어 치우쳐 보인다(결정 339).
        /// </summary>
        [UnityTest]
        public IEnumerator T156_장착됨_리본이_오른쪽이_파인_깃발로_걸린다()
        {
            yield return Boot();
            ForgeHost F = ForgeHost.Instance;
            UiRoot root = UiRoot.Instance;
            ForgeItem it = F.Engine.RollItem();
            it.Subs = SubstatRoll.Roll(F.Defs, CoreRng.Mulberry(43224), 2);
            ForgeCraftPopup.Show(F, it);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return SettleCardPop();

            Popup p = F.Meta.Popups.Find(ForgeCraftPopup.Name);
            Assert.IsNotNull(p, "비교 팝업이 열렸다");
            RectTransform ribbon = null;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true)) if (rt.name == "ribbon") { ribbon = rt; break; }
            Assert.IsNotNull(ribbon, "장착됨 리본");

            RectTransform flag = (RectTransform)ribbon.Find("flag");
            Assert.IsNotNull(flag, "깃발(옛 둥근 사각 한 장이면 이 칸이 없다)");
            Assert.IsNotNull(flag.Find("line"), "테두리 면");
            Assert.IsNotNull(flag.Find("face"), "종이 면");

            Vector3[] a = new Vector3[4], r = new Vector3[4];
            root.App.GetWorldCorners(a); ribbon.GetWorldCorners(r);
            float appW = a[2].x - a[0].x;
            Assert.AreEqual(0.203f, (r[2].x - r[0].x) / appW, 0.005f, "리본 폭 = 앱 폭의 20.3%(정본 `calc(var(--app-w) * .203)`)");

            TMPro.TextMeshProUGUI label = ribbon.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            Assert.IsNotNull(label, "리본 글자");
            Assert.AreEqual(TMPro.TextAlignmentOptions.Center, label.alignment, "정본 `text-align: center` — 치우침은 정렬이 아니라 패딩이 만든다(결정 339)");
            float rem = PopupKit.Rem, pl, pr, pt, pb;
            RibbonArt.Padding(rem, out pl, out pr, out pt, out pb);
            // ⚠ 여백은 **칸 좌표**로 잰다. `GetWorldCorners` 는 캔버스 배율이 실린 월드 값이라(런 398 실측: 기준 px 의 1/4)
            //    기준 캔버스 px 인 패딩 값과 바로 견주면 배율만큼 어긋난다 — 폭 비(20.3%)처럼 «비» 로 재는 자리와 다르다.
            RectTransform lt = label.rectTransform;
            float ribW = RibbonArt.Width(UiKit.RefW);
            Assert.AreEqual(pl, lt.anchoredPosition.x, 0.5f, "왼쪽 패딩 .5rem(칸 좌표)");
            Assert.AreEqual(ribW - pl - pr, lt.rect.width, 0.5f, "글자 상자는 양쪽 패딩만큼 좁다");
            float rightGap = ribW - (lt.anchoredPosition.x + lt.rect.width);
            Assert.AreEqual(pr, rightGap, 0.5f, "오른쪽 패딩 1.5rem — «<» 파임 몫이라 왼쪽보다 넓다");
            Assert.Greater(rightGap, pl, "글자 상자가 오른쪽으로 더 좁아 글자가 왼쪽으로 치우쳐 보인다(원작 그대로)");

            // 깃발은 카드 **밖으로 내민다**(정본 `left: -1.52rem` · 주석 «22.2 + 7(내밈)»). 원작 `shot-043224` 에서도
            // 리본 왼쪽 끝이 카드 왼쪽 변보다 왼쪽이다 — 런 406 클론은 변에 붙어 있었다(2회차 자리 수리).
            RectTransform outer = (RectTransform)p.Root.Find("card");
            Assert.IsNotNull(outer, "바깥 카드");
            Vector3[] oc = new Vector3[4];
            outer.GetWorldCorners(oc);
            // 월드 좌표로 재므로 «기준 px → 월드» 배율을 리본 제 폭에서 뽑아 쓴다(2회차에 겪은 좌표계 함정).
            float scale = (r[2].x - r[0].x) / RibbonArt.Width(UiKit.RefW);
            float outWorld = oc[0].x - r[0].x;
            Assert.Greater(outWorld, 0f, "리본 왼쪽 끝이 카드 왼쪽 변보다 왼쪽이어야 «내민 깃발» 이다 — 지금 "
                           + (outWorld / scale).ToString("0.0") + "기준px");
            Assert.AreEqual(RibbonArt.ProtrudeCss() * KeylineUi.CssPx * scale, outWorld, 4f * scale,
                            "내밈은 정본이 적어 둔 7 CSS px(«22.2 + 7(내밈) ≈ 2rem») — 지금 "
                            + (outWorld / scale / KeylineUi.CssPx).ToString("0.0") + "css px");

            ForgeCraftPopup.Hide(F);
            yield return null;
        }
    }
}
