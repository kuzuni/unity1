using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T135 ⓑ — 팝업을 처음 열면 카드가 표대로 튀고(scale·α 가 같은 시각의 셈과 같다), 끝나면 원래 모습으로 돌아오며,
    /// 열린 채 다시 부르는 재호출·재렌더는 튀지 않는다(정본 `ui.js` 1156). 카드 없는 시트는 대상이 아니다.
    /// </summary>
    public class CardPopTests
    {
        static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다");
            Assert.IsNotNull(PopupLayer.Instance);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 처음_열면_카드가_표대로_튀고_끝나면_원래_모습이며_재호출은_안_튄다()
        {
            yield return Boot();
            PopupLayer L = PopupLayer.Instance;
            CardPopSpec s = CardPop.Spec;
            Popup p = L.ShowStub("팝 시험", "설명");
            CardPop pop = p.Root.GetComponent<CardPop>();
            Assert.IsNotNull(pop, "새 팝업 뿌리에 CardPop 이 붙는다");
            RectTransform card = (RectTransform)p.Root.Find("card");
            Assert.IsNotNull(card);
            yield return null;
            // 한 프레임 뒤: 아직 도는 중 — 같은 시각의 셈과 값이 같다(경과는 러너가 쥔다)
            Assert.IsFalse(s.Done(pop.ElapsedMs), "한 프레임(≈16ms)은 250ms 안이다 · 경과 " + pop.ElapsedMs);
            CanvasGroup cg = card.GetComponent<CanvasGroup>();
            Assert.IsNotNull(cg, "α 는 CanvasGroup 으로 건다");
            Assert.AreEqual((float)s.ScaleAt(pop.ElapsedMs), card.localScale.x, 1e-3f, "scale = 표(경과 " + pop.ElapsedMs + ")");
            Assert.AreEqual(card.localScale.x, card.localScale.y, 1e-6f, "가로세로 같은 배율");
            Assert.AreEqual((float)s.AlphaAt(pop.ElapsedMs), cg.alpha, 1e-3f, "α = 표");
            Assert.Less(card.localScale.x, 1f, "아직 다 안 컸다");
            Assert.GreaterOrEqual(card.localScale.x, 0.7f, "from scale(.7) 아래로는 안 간다");

            // 열린 채 재호출 — 같은 팝업 · 러너가 새로 안 붙는다(정본 1156)
            Popup again = L.Show("stub");
            Assert.AreSame(p, again);
            Assert.AreEqual(1, p.Root.GetComponents<CardPop>().Length, "재호출은 opening 을 다시 안 붙인다");

            // 끝까지 돈다 → 원래 모습 · 러너와 내 CanvasGroup 은 걷힌다
            float t = 0f;
            while (p.Root.GetComponent<CardPop>() != null && t < 3f) { t += Time.unscaledDeltaTime; yield return null; }
            yield return null;
            Assert.IsNull(p.Root.GetComponent<CardPop>(), "끝나면 스스로 사라진다");
            card = (RectTransform)p.Root.Find("card");
            Assert.IsNotNull(card, "카드는 그대로");
            Assert.AreEqual(1f, card.localScale.x, 1e-6f, "transform: none");
            Assert.AreEqual(1f, card.localScale.y, 1e-6f);
            Assert.IsNull(card.GetComponent<CanvasGroup>(), "내가 더한 CanvasGroup 은 걷는다");

            // 끝난 뒤 재렌더(Clear + 다시 세움)도 튀지 않는다
            PopupLayer.Clear(p);
            RectTransform re = PopupKit.Card(p.Root, "card", UiKit.L("modal_card_w") * UiKit.RefW, -1f, "pp_paper", UiKit.H("card_r"));
            yield return null;
            Assert.IsNull(p.Root.GetComponent<CardPop>());
            Assert.AreEqual(1f, re.localScale.x, 1e-6f, "재렌더는 안 튄다");
            L.Hide(p);
            yield return null;

            // 닫았다 다시 열면(새 팝업) 또 튄다
            Popup p2 = L.ShowStub("팝 시험 2", "설명");
            Assert.IsNotNull(p2.Root.GetComponent<CardPop>(), "새로 여는 것은 처음 열림이다");
            L.Hide(p2);
            yield return null;
        }

        /// <summary>촬영·픽셀 자가 쓰는 길 — 팝을 지금 끝내면 같은 프레임에 카드가 원래 모습이고 러너·CanvasGroup 이 걷힌다(T128 ⓒ · 런 341).</summary>
        [UnityTest]
        public IEnumerator SettleAll_은_팝을_지금_끝내_카드를_원래_모습으로_돌린다()
        {
            yield return Boot();
            PopupLayer L = PopupLayer.Instance;
            Popup p = L.ShowStub("정착 시험", "설명");
            yield return null;
            RectTransform card = (RectTransform)p.Root.Find("card");
            Assert.Less(card.localScale.x, 1f, "정착 전엔 도는 중");
            CardPop.SettleAll();
            Assert.AreEqual(1f, card.localScale.x, 1e-6f, "정착 즉시 scale 1");
            Assert.AreEqual(1f, card.localScale.y, 1e-6f);
            CanvasGroup cg = card.GetComponent<CanvasGroup>();
            Assert.IsTrue(cg == null || cg.alpha >= 1f - 1e-6f, "α 1(CanvasGroup 은 프레임 끝에 걷힌다)");
            yield return null;
            Assert.IsNull(p.Root.GetComponent<CardPop>(), "러너는 걷힌다");
            Assert.IsNull(card.GetComponent<CanvasGroup>(), "내가 더한 CanvasGroup 은 걷힌다");
            Assert.AreEqual(1f, card.localScale.x, 1e-6f, "다음 프레임에도 원래 모습(러너가 되살리지 않는다)");
            L.Hide(p);
            yield return null;
        }
    }
}
