using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T415 7회차 — 패스 화면의 둥근 모서리 넷이 정본 값으로 선다.
    /// ⓐ 리본 밴드(정본 2705 `.pass-banner` .2rem)·보상 칸(2822 `.pass-cell` .6rem)은 **표(`RadiusUi.json`)** 를 읽는다 — 반지름은
    ///   9-슬라이스 배율(<see cref="UiShapes.RoundedMultiplier"/>)로 되읽는다(ChatRadiusTests 와 같은 길).
    /// ⓑ 마일스톤 필(2801 1rem)·보상 알약(2830 1rem)은 정본 1rem 이 상자 반높이보다 커서 CSS 가 반높이로 줄인 **알약**이다(결정 543) —
    ///   클론은 `h * 0.5f` 로 깐다. 자(check_border_radius)는 그 셈을 못 보므로 여기서 <see cref="RadiusRules.IsPill"/> 로 지킨다.
    /// </summary>
    public class PassRadiusTests
    {
        static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(UiRoot.Instance != null && UiRoot.Instance.App != null && MetaHost.Ready && PopupLayer.Instance != null) && t < 20f)
            { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "메타 호스트 부팅");
            yield return null;
        }

        static float Mult(float radiusPx) { return UiShapes.RoundedMultiplier(radiusPx); }

        static void AssertRounded(Image img, float radiusPx, string what)
        {
            Assert.IsNotNull(img, what + ": Image 가 없다");
            Assert.AreEqual(UiShapes.Rounded, img.sprite, what + ": 둥근 9-슬라이스 스프라이트여야 한다");
            Assert.AreEqual(Mult(radiusPx), img.pixelsPerUnitMultiplier, 1e-3f, what + ": 반지름이 다르다");
        }

        [Test]
        public void 표의_패스_두_값은_정본_rem_그대로다()
        {
            float px = RadiusUi.PxPerRem;
            Assert.Greater(px, 0f, "1rem px");
            Assert.AreEqual(0.2f * px, RadiusUi.Px("pass_banner_r_rem"), 1e-3f, "정본 2705 .pass-banner .2rem");
            Assert.AreEqual(0.6f * px, RadiusUi.Px("pass_cell_r_rem"), 1e-3f, "정본 2822 .pass-cell .6rem");
        }

        [UnityTest]
        public IEnumerator 패스_리본과_보상_칸은_표의_반지름으로_서고_필과_알약은_알약_동치다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            PassPopup.Open(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(PassPopup.Name);
            Assert.IsNotNull(p, "패스 팝업이 열린다");

            float line3 = PopupKit.Line3, rem = RadiusUi.PxPerRem;
            float bannerR = RadiusUi.Px("pass_banner_r_rem"), cellR = RadiusUi.Px("pass_cell_r_rem");

            // ⓐ 리본 밴드 — RadiusUi.Outlined(band, "face", …) 는 상자 «face» 아래 테 «line» 과 안쪽 면 «face»
            Transform band = null;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true)) if (rt.name == "band") { band = rt; break; }
            Assert.IsNotNull(band, "리본 밴드(band)");
            Transform bandBox = band.Find("face");
            Assert.IsNotNull(bandBox, "밴드의 테·면 상자(face)");
            AssertRounded(bandBox.Find("line").GetComponent<Image>(), bannerR, "리본 밴드 테");
            AssertRounded(bandBox.Find("face").GetComponent<Image>(), Mathf.Max(1f, bannerR - line3), "리본 밴드 면(테 안쪽)");

            // ⓐ 보상 칸(무료·프리미엄) · ⓑ 안의 보상 알약 · ⓑ 마일스톤 필
            int cells = 0, pills = 0, labels = 0;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if ((rt.name == "free" || rt.name == "premium") && rt.Find("face") != null && rt.Find("face/line") != null)
                {
                    cells++;
                    AssertRounded(rt.Find("face/line").GetComponent<Image>(), cellR, rt.name + " 칸 테");
                    AssertRounded(rt.Find("face/face").GetComponent<Image>(), Mathf.Max(1f, cellR - line3), rt.name + " 칸 면");
                }
                else if (rt.name.StartsWith("pill-") && rt.Find("bg") != null)
                {
                    pills++;
                    float hgt = rt.rect.height;
                    Assert.IsTrue(RadiusRules.IsPill(rem * 1f, hgt), "정본 1rem ≥ 알약 반높이(" + hgt + ") — 알약 동치가 아니면 표 키로 옮겨야 한다");
                    AssertRounded(rt.Find("bg").GetComponent<Image>(), hgt * 0.5f, "보상 알약 " + rt.name);
                }
                else if (rt.name == "label" && rt.parent != null && rt.parent.name.StartsWith("seg-") && rt.Find("line") != null)
                {
                    labels++;
                    float hgt = rt.rect.height;
                    Assert.IsTrue(RadiusRules.IsPill(rem * 1f, hgt), "정본 1rem ≥ 필 반높이(" + hgt + ") — 알약 동치가 아니면 표 키로 옮겨야 한다");
                    AssertRounded(rt.Find("line").GetComponent<Image>(), hgt * 0.5f, "마일스톤 필 테");
                    AssertRounded(rt.Find("face").GetComponent<Image>(), Mathf.Max(1f, hgt * 0.5f - PopupKit.Line2), "마일스톤 필 면");   // T365 23회차 — 필 테가 정본 2803 ol2(Line2)로 넓어져 안쪽 면 반지름도 그만큼 준다(런 1250 빨강)
                }
            }
            Assert.Greater(cells, 0, "보상 칸이 하나도 없다");
            Assert.Greater(pills, 0, "보상 알약이 하나도 없다");
            Assert.Greater(labels, 0, "마일스톤 필이 하나도 없다");

            PassPopup.Close(h);
            yield return null;
        }
    }
}
