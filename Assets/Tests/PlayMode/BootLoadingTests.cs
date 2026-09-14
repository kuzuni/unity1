using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>T142 2회차 — 부팅 로딩 오버레이가 표대로 서고 진행률·단계 글자·페이드가 정본 규칙을 따르는가.
    /// 진행률을 미는 쪽(부팅 절차)은 3회차가 단다 — 여기서는 손으로 밀어 본다.</summary>
    public class BootLoadingTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
        }

        private static BootLoading Open()
        {
            BootLoading.ResetCache();
            return BootLoading.Ensure(UiRoot.Instance.App);
        }

        [UnityTest]
        public IEnumerator 오버레이가_표대로_선다()
        {
            yield return Boot();
            BootLoading bl = Open();
            yield return null;
            Assert.IsNotNull(bl, "오버레이가 안 섰다");

            Transform root = bl.transform;
            Assert.IsNotNull(root.Find("bg"), "바탕이 없다");
            Transform box = root.Find("bl-box");
            Assert.IsNotNull(box, "가운데 상자가 없다");
            Assert.IsNotNull(box.Find("bl-forge/bl-anvil"), "모루가 없다");
            Assert.IsNotNull(box.Find("bl-forge/bl-hammer"), "망치가 없다");
            Assert.IsNotNull(box.Find("bl-title"), "제목이 없다");
            Assert.IsNotNull(box.Find("bl-track/bl-fill"), "진행 막대 채움이 없다");
            Assert.IsNotNull(box.Find("bl-stage"), "단계 글자가 없다");
            for (int i = 1; i <= 3; i++)
                Assert.IsNotNull(box.Find("bl-forge/bl-spark-" + i), "불티 " + i + " 이 없다");

            Assert.AreEqual("포지 클론", box.Find("bl-title").GetComponent<TextMeshProUGUI>().text);
            Object.Destroy(bl.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 진행률을_밀면_채움_폭과_단계_글자가_같이_간다()
        {
            yield return Boot();
            BootLoading bl = Open();
            yield return null;
            var spec = BootLoading.Spec;
            RectTransform fill = (RectTransform)bl.transform.Find("bl-box/bl-track/bl-fill");
            TextMeshProUGUI stage = bl.transform.Find("bl-box/bl-stage").GetComponent<TextMeshProUGUI>();

            bl.Set(8);
            yield return null;
            Assert.AreEqual(spec.FillWidthPx(8), fill.sizeDelta.x, 0.01, "8% 채움 폭");
            Assert.AreEqual("세이브 불러오는 중…", stage.text);

            bl.Set(42);
            yield return null;
            Assert.AreEqual(spec.FillWidthPx(42), fill.sizeDelta.x, 0.01, "42% 채움 폭");
            Assert.AreEqual("전장 짓는 중…", stage.text);

            // 마지막 단계(100%)는 정본 blSet 이 label 없이 부른다 — 글자를 안 건드린다
            bl.Set(96);
            yield return null;
            Assert.AreEqual("마무리 중…", stage.text);
            bl.Set(100);
            yield return null;
            Assert.AreEqual("마무리 중…", stage.text, "100% 는 글자를 안 바꾼다(정본 blSet 의 `if (s && label)`)");
            Assert.AreEqual(spec.TrackWPx, fill.sizeDelta.x, 0.01, "100% 면 막대가 꽉 찬다");

            Object.Destroy(bl.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 망치는_돌고_불티는_깜박인다()
        {
            yield return Boot();
            BootLoading bl = Open();
            yield return null;
            RectTransform hammer = (RectTransform)bl.transform.Find("bl-box/bl-forge/bl-hammer");
            // 피벗이 «머리 쪽 끝»(정본 transform-origin: 88% 88%)이라야 내리치는 그림이 된다
            Assert.AreEqual(0.88f, hammer.pivot.x, 0.001f);
            Assert.AreEqual(0.88f, hammer.pivot.y, 0.001f);

            float first = hammer.localEulerAngles.z;
            for (int i = 0; i < 20; i++) yield return null;
            float later = hammer.localEulerAngles.z;
            Assert.AreNotEqual(first, later, "망치가 멈춰 있다");

            Object.Destroy(bl.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 끝나면_페이드하고_스스로_사라진다()
        {
            yield return Boot();
            BootLoading bl = Open();
            yield return null;
            GameObject go = bl.gameObject;
            CanvasGroup g = go.GetComponent<CanvasGroup>();
            Assert.IsNotNull(g, "페이드용 CanvasGroup 이 없다");
            Assert.AreEqual(1f, g.alpha, 0.001f);

            bl.Done();
            Assert.IsTrue(bl.Fading);
            float t = 0f;
            while (go != null && t < 2f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(go == null, "정본 blDone 처럼 스스로 치워져야 한다(450ms)");
        }
    }
}
