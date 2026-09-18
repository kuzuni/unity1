using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T339 — 대장간 팝업 네 화면의 카드 폭이 **정본 표**대로인가(여태 넷 다 코드에 박힌 `0.85` 였다).
    ///
    /// 정본은 화면마다 다르다: `.af-card`·`.fi-card` 는 **77.19%W**(둘 다 `min()` 의 다른 쪽이 이겨 같은 값이 된다) ·
    /// `.fl-card` 는 **71%W**. 런 474 실측으로 클론은 넷 다 **83.70%W** 였다.
    /// </summary>
    public class ForgeCardWidthTests
    {
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

        [TearDown]
        public void CleanSave()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
        }

        [UnityTest]
        public IEnumerator 표가_정본_min_을_그대로_셈한다()
        {
            yield return Boot();
            float W = UiKit.RefW, rem = PopupKit.Rem;

            // 정본 4682 `.af-card { width: min(calc(var(--app-w) * .7719), 23rem) }` — 기준 캔버스에서는 **앞쪽**이 이긴다.
            float af = ForgeAutoStyle.CardW(W, rem);
            Assert.AreEqual(Mathf.Min(0.7719f * W, 23f * rem), af, 0.01f, "min 을 그대로 옮긴다");
            Assert.AreEqual(0.7719f, af / W, 0.005f, "= 77.19%W");

            // 정본 5043 `.fi-card { width: min(calc(var(--app-w) * .9), 22.9rem) }` — 여기서는 **뒤쪽(rem 상한)**이 이겨 같은 77.19%W 가 된다.
            float fi = ForgeInfoStyle.FiCardW(W, rem);
            Assert.AreEqual(22.9f * rem, fi, 0.01f, "rem 상한이 이긴다 — `.9` 만 쓰면 90%W 로 한참 넓다");
            Assert.AreEqual(0.7719f, fi / W, 0.005f, "정본 주석의 «원본 카드 77.19%W»");
            Assert.AreEqual(af, fi, 1f, "두 카드는 결국 같은 폭이다(오는 길만 다르다)");

            // 정본 5606 `.fl-card { width: 71% }` — 목록만 다르다.
            Assert.AreEqual(0.71f, ForgeInfoStyle.L("fl_card_w_f"), 1e-4f);
            Assert.Less(ForgeInfoStyle.L("fl_card_w_f"), 0.7719f, "목록 카드는 확률 정보보다 **좁다** — 여태 둘이 같았다");
        }

        [UnityTest]
        public IEnumerator 세_팝업의_실제_카드_폭이_표대로다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            float W = UiKit.RefW, rem = PopupKit.Rem;

            // §0 빨강 수리(런 503 · «autoforge 가 열렸다 · Expected not null · But was null»): 새 세이브는 2-10 전이라
            // `ForgeAutoPopup.Open` 이 🔒 토스트만 내고 팝업을 안 연다(24행) — AgePatternTests 209 · ForgeUiTests 335 와 같이 먼저 해금한다.
            h.S.BestChapter = 3; h.S.BestStage = 1; h.Pull();
            Assert.IsTrue(h.AutoForgeUnlocked, "2-10 뒤 해금");
            // 자동 제련은 **2-10 도달 뒤에만** 열린다(`ForgeAutoPopup.Open` 이 잠기면 토스트만 내고 돌아간다) —
            // 이 자는 세이브를 지우고 부팅하므로 `ForgeUiTests` 가 쓰는 길 그대로 진도를 올려 연다(런 512 빨강 수리).
            h.S.BestChapter = 3; h.S.BestStage = 1;
            Assert.IsTrue(h.AutoForgeUnlocked, "2-10 을 넘겨 자동 제련이 해금됐다");
            ForgeAutoPopup.Open(h);
            yield return null;
            Assert.AreEqual(ForgeAutoStyle.CardW(W, rem), CardWidth(h, ForgeAutoPopup.Name), 0.5f, "자동 제련 — 정본 77.19%W(클론은 83.70 이었다)");
            float afH = CardHeight(h, ForgeAutoPopup.Name);
            Assert.Greater(afH, 0f, "자동 제련 카드 높이");
            // 높이는 탭바에 걸리는 몫만큼만 깎인다(T78 FitBetweenBars) — 표의 84.52%H 를 넘지는 않는다.
            Assert.LessOrEqual(afH + PopupKit.Line3 * 2f, UiKit.RefH * ForgeAutoStyle.L("card_h_f") + 0.5f, "표의 84.52%H 가 상한이다(카드 몸 = rect + 테 두 겹 · T465)");
            // T400 2회차 — 정본 4675 «카드 y7.01%H · 하단 91.75%H»: 높이는 표 그대로(깎지 않는다) · 위끝은 card_top_f.
            {
                Assert.AreEqual(UiKit.RefH * ForgeAutoStyle.L("card_h_f") - PopupKit.Line3 * 2f, afH, 0.5f, "카드 rect(패딩 상자) = 표 84.52%H(카드 몸) − 테 두 겹(T465) — T78 FitBetweenBars 깎기(76.25%H)를 걷었다");
                Popup ap = h.Meta.Popups.Find(ForgeAutoPopup.Name);
                Assert.IsNotNull(ap, "자동 제련 팝업");
                RectTransform afCard = null;
                foreach (RectTransform rt in ap.Root.GetComponentsInChildren<RectTransform>(true)) if (rt.name == "card") { afCard = rt; break; }
                Assert.IsNotNull(afCard, "자동 제련 카드(card)");
                float top = UiKit.RefH * 0.5f - afCard.anchoredPosition.y - afCard.rect.height * 0.5f;   // 가운데 앵커 · 양수 = 위
                Assert.AreEqual(UiKit.RefH * ForgeAutoStyle.L("card_top_f"), top, 0.5f, "카드 위끝 = 표 card_top_f(7.01%H) — 전엔 8.65%H");
                Assert.Greater((top + afCard.rect.height) / UiKit.RefH, UiKit.L("tabbar_top"), "정본대로 카드 하단이 탭바 위끝보다 아래로 내려간다");
            }
            ForgeAutoPopup.Close(h);
            yield return null;

            // 확률 정보와 목록은 **같은 팝업 이름**(forge-info)의 두 얼굴이다 — 폭은 서로 달라야 한다.
            ForgeInfoPopup.Open(h);
            yield return null;
            float fiW = CardWidth(h, ForgeInfoPopup.Name);
            Assert.AreEqual(ForgeInfoStyle.FiCardW(W, rem), fiW, 0.5f, "확률 정보 — 정본 77.19%W");
            // T339 2회차 — 높이도 표대로(정본 5048 `height: calc(var(--app-h) * .8104)`).
            // 원작 PNG 실측 79.9%H(y85~789 · shot-042831) ↔ 클론은 내용이 정하던 72.1%H 였다.
            Assert.AreEqual(UiKit.RefH * ForgeInfoStyle.L("fi_card_h_f") - PopupKit.Line3 * 2f, CardHeight(h, ForgeInfoPopup.Name), 0.5f, "확률 정보 rect(패딩 상자) — 정본 81.04%H(카드 몸) − 테 두 겹(T465)");
            ForgeInfoPopup.Close(h);
            yield return null;

            ForgeInfoPopup.OpenList(h);
            yield return null;
            float flW = CardWidth(h, ForgeInfoPopup.Name);
            Assert.AreEqual(W * ForgeInfoStyle.L("fl_card_w_f"), flW, 0.5f, "모든 장비의 목록 — 정본 71%W");
            Assert.Less(flW, fiW - 1f, "목록이 확률 정보보다 좁다 — 여태 둘이 같았다");
            ForgeInfoPopup.Close(h);
            yield return null;
        }

        static RectTransform Card(ForgeHost h, string popup)
        {
            Popup p = h.Meta.Popups.Find(popup);
            Assert.IsNotNull(p, popup + " 가 열렸다");
            RectTransform card = (RectTransform)p.Root.Find("card");
            Assert.IsNotNull(card, popup + " 의 card");
            return card;
        }

        static float CardWidth(ForgeHost h, string popup) { return Card(h, popup).rect.width; }
        static float CardHeight(ForgeHost h, string popup) { return Card(h, popup).rect.height; }
    }
}
