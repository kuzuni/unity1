using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Ui;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T454 ⓐⓑ — 정본 `transition` 이 클론에 없던 두 자리: 토스트 등장(1936 · α 0→1 · y −.5rem→0 · .25s) · 토글 손잡이(3113/4992 · left .15s).
    /// 시간축은 CI 프레임으로 안 잰다(결정 780) — «첫 프레임 = 시작 상태» 와 «SettleAll 뒤 = 끝 상태» 를 보고, 끝까지는 상한을 둔 채 기다린다.
    /// </summary>
    public class TransitionTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(PopupLayer.Instance != null && UiRoot.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(PopupLayer.Instance, "팝업 층이 15초 안에 안 섰다");
            ToggleSlide.Forget();
            yield return null;
        }

        /// <summary>기본 레인(`toasts` · 앱 상자 아래 — 팝업 층 아래가 아니다 · 런 1131 의 빨강)의 마지막 토스트.</summary>
        static RectTransform LastToast()
        {
            RectTransform box = null;
            foreach (RectTransform rt in UiRoot.Instance.App.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "toast" && rt.Find("line") != null && rt.parent != null && rt.parent.name == "toasts"
                    && (box == null || rt.GetSiblingIndex() > box.GetSiblingIndex())) box = rt;
            return box;
        }

        [UnityTest]
        public IEnumerator 토스트는_위_반_rem_에서_투명하게_시작해_표의_길이_뒤_제자리에서_불투명하다()
        {
            yield return Boot();
            TransitionSpec s = TransitionUi.Table.Get("toast_in");
            Assert.AreEqual(250, s.Ms, "정본 1936 .25s");
            PopupLayer.Instance.Toast("등장 잣대");
            RectTransform box = LastToast();
            Assert.IsNotNull(box, "toast");
            ToastEnter te = box.GetComponent<ToastEnter>();
            Assert.IsNotNull(te, "토스트에 ToastEnter 가 붙는다");
            Assert.IsTrue(te.Entering, "세운 프레임엔 들어오는 중");
            Assert.AreEqual(0f, te.Alpha, 1e-3f, "첫 프레임 α 0(정본 .toast opacity 0)");
            float rem = PopupKit.Rem;
            Assert.AreEqual(te.BasePos.y + 0.5f * rem, box.anchoredPosition.y, 0.5f, "첫 프레임은 위 .5rem(정본 translateY(-.5rem) · 캔버스는 위가 +)");
            Assert.AreEqual(te.BasePos.x, box.anchoredPosition.x, 1e-3f, "가로는 안 움직인다");
            // 정지 촬영 길 — 바로 끝 상태
            ToastEnter.SettleAll();
            Assert.IsFalse(te.Entering);
            Assert.AreEqual(1f, te.Alpha, 1e-3f, "SettleAll 뒤 α 1(정본 .show)");
            Assert.AreEqual(te.BasePos.y, box.anchoredPosition.y, 1e-3f, "SettleAll 뒤 제자리(transform: none)");
            // 시간으로도 끝난다 — «딱 ms» 로 재지 않고 끝날 때까지(상한 8×ms)
            PopupLayer.Instance.Toast("등장 잣대 둘");
            RectTransform box2 = LastToast();
            Assert.AreNotSame(box, box2);
            ToastEnter te2 = box2.GetComponent<ToastEnter>();
            Assert.IsNotNull(te2);
            float t2 = 0f;
            while (te2.Entering && t2 * 1000f < s.Ms * 8) { t2 += Time.unscaledDeltaTime; yield return null; }
            Assert.IsFalse(te2.Entering, "ms 가 지나면 끝(상한 8×ms 안에)");
            Assert.AreEqual(1f, te2.Alpha, 1e-3f);
            Assert.AreEqual(te2.BasePos.y, box2.anchoredPosition.y, 1e-3f);
        }

        [UnityTest]
        public IEnumerator 토글은_누른_뒤_다시_세워질_때_손잡이가_앞_닻에서_출발해_새_닻에_닿고_안_누르면_안_움직인다()
        {
            yield return Boot();
            TransitionSpec s = TransitionUi.Table.Get("toggle");
            Assert.AreEqual(150, s.Ms, "정본 3113 .15s");
            Assert.IsTrue(TransitionUi.Table.Has("af-toggle"), "4992 자동 제련 토글 칸(이름이 열쇠)");
            RectTransform row = UiKit.Box(UiRoot.Instance.App, "t454-row");
            UiKit.Anchor(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 200f, 60f);
            float off = PopupKit.Line2 * 2f;
            // ① 첫 세움(안 누름) — 전이 없음 · 손잡이는 바로 꺼진 닻(0)
            Button b1 = PopupKit.Toggle(row, "toggle", false, () => { });
            RectTransform knob1 = (RectTransform)b1.transform.Find("knob");
            Assert.IsNotNull(knob1);
            Assert.IsNull(knob1.GetComponent<ToggleSlide>(), "안 누르고 세운 토글은 안 움직인다");
            Assert.AreEqual(0f, knob1.anchorMin.x, 1e-4f);
            // ② 누른다(화면을 다시 세우는 자리 — 여기선 자가 그 역을 한다) → 같은 열쇠로 켜진 채 다시 세움
            b1.onClick.Invoke();
            Object.DestroyImmediate(b1.gameObject);
            Button b2 = PopupKit.Toggle(row, "toggle", true, () => { });
            RectTransform knob2 = (RectTransform)b2.transform.Find("knob");
            ToggleSlide sl = knob2.GetComponent<ToggleSlide>();
            Assert.IsNotNull(sl, "누른 뒤 다시 세워진 토글의 손잡이는 미끄러진다");
            Assert.IsTrue(sl.Sliding);
            Assert.AreEqual(0f, sl.FromAnchorX, 1e-4f, "앞 닻(꺼짐 = 0)에서 출발");
            Assert.AreEqual(1f, sl.ToAnchorX, 1e-4f, "새 닻(켜짐 = 1)로");
            Assert.AreEqual(0f, knob2.anchorMin.x, 1e-4f, "첫 프레임은 앞 닻 그대로(한 프레임 안에 잇는다)");
            Assert.AreEqual(off, knob2.anchoredPosition.x, 1e-3f, "첫 프레임 오프셋도 앞 상태(+2·ol2)");
            ToggleSlide.SettleAll();
            Assert.IsFalse(sl.Sliding);
            Assert.AreEqual(1f, knob2.anchorMin.x, 1e-4f, "SettleAll 뒤 새 닻");
            Assert.AreEqual(1f, knob2.anchorMax.x, 1e-4f);
            Assert.AreEqual(-off, knob2.anchoredPosition.x, 1e-3f, "켜짐 오프셋(−2·ol2)");
            // ③ 누르고 다시 세우되 같은 상태면(다른 이유로 다시 세움) 전이 없음 · 적어 둔 것은 소비된다
            b2.onClick.Invoke();
            Object.DestroyImmediate(b2.gameObject);
            Button b3 = PopupKit.Toggle(row, "toggle", true, () => { });
            Assert.IsNull(b3.transform.Find("knob").GetComponent<ToggleSlide>(), "상태가 같으면 안 움직인다");
            Object.DestroyImmediate(b3.gameObject);
            Button b4 = PopupKit.Toggle(row, "toggle", false, () => { });
            Assert.IsNull(b4.transform.Find("knob").GetComponent<ToggleSlide>(), "누름 없이 세우면(적어 둔 것이 소비된 뒤) 안 움직인다");
            // ④ 시간으로도 끝난다(상한 8×ms)
            b4.onClick.Invoke();
            Object.DestroyImmediate(b4.gameObject);
            Button b5 = PopupKit.Toggle(row, "toggle", true, () => { });
            ToggleSlide sl5 = b5.transform.Find("knob").GetComponent<ToggleSlide>();
            Assert.IsNotNull(sl5);
            float t2 = 0f;
            while (sl5.Sliding && t2 * 1000f < s.Ms * 8) { t2 += Time.unscaledDeltaTime; yield return null; }
            Assert.IsFalse(sl5.Sliding, "ms 가 지나면 끝(상한 8×ms 안에)");
            Assert.AreEqual(1f, ((RectTransform)b5.transform.Find("knob")).anchorMin.x, 1e-4f);
            Object.Destroy(row.gameObject);
        }
    }
}
