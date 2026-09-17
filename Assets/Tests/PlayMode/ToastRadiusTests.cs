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
    /// T415 9회차 — 토스트의 모서리(정본 `css/style.css` **1933** `.toast { border-radius: 2rem }`).
    /// 클론 `Popups.cs` 는 그것을 `h * 0.5f`(= 알약)로 깐다. 둘은 **같은 그림**이다 — 정본 상자 높이가
    /// `toast_h` 2.2rem 이라 CSS 가 반지름을 «높이의 반»(1.1rem)으로 줄이기 때문이다(알약 동치 · 결정 543).
    /// 자(`check_border_radius`)는 `h * 0.5f` 같은 셈을 못 보므로 그 동치를 여기서 <see cref="RadiusRules.IsPill"/> 로 지킨다
    /// (`PassRadiusTests` 가 패스 알약 둘에 쓴 그 길 그대로).
    /// </summary>
    public class ToastRadiusTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(PopupLayer.Instance != null && UiRoot.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(PopupLayer.Instance, "팝업 층이 15초 안에 안 섰다");
            yield return null;
        }

        [Test]
        public void 정본_2rem_은_토스트_높이에서_알약과_같은_그림이다()
        {
            float h = UiKit.H("toast_h");
            float canon = 2f * UiKit.H("rem_h");
            Assert.IsTrue(RadiusRules.IsPill(canon, h),
                "정본 1933 `.toast { border-radius: 2rem }`(" + canon.ToString("0.0") + "px) ≥ 높이의 반(" + (h * 0.5f).ToString("0.0") + "px) — 그래서 CSS 가 알약으로 줄인다(결정 543)");
            Assert.AreEqual(h * 0.5f, RadiusRules.Clamp(canon, UiKit.L("toast_w") * UiKit.RefW, h), 0.5f,
                "CSS 가 줄인 값 = 높이의 반 — 클론이 그리는 그 값이다");
        }

        [UnityTest]
        public IEnumerator 토스트는_알약으로_선다()
        {
            yield return Boot();
            PopupLayer.Instance.Toast("모서리 잣대");
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            RectTransform box = null;
            foreach (RectTransform rt in UiRoot.Instance.App.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "toast" && rt.Find("line") != null) { box = rt; break; }
            Assert.IsNotNull(box, "토스트 상자(toast)");
            float h = box.rect.height;
            Image line = box.Find("line").GetComponent<Image>();
            Assert.AreEqual(UiShapes.Rounded, line.sprite, "토스트 테: 둥근 9-슬라이스");
            Assert.AreEqual(UiShapes.RoundedMultiplier(h * 0.5f), line.pixelsPerUnitMultiplier, 1e-3f,
                "토스트 테는 알약(높이의 반 = " + (h * 0.5f).ToString("0.0") + "px) · 정본 2rem 이 그 높이에서 줄어든 값과 같다");
            Debug.Log("[T415] 토스트 높이 " + h.ToString("0.0") + "px · 알약 반지름 " + (h * 0.5f).ToString("0.0") + "px · 정본 2rem = " + (2f * UiKit.H("rem_h")).ToString("0.0") + "px");
        }
    }
}
