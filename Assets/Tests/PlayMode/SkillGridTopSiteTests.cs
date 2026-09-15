using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T369 2회차 — 스킬 격자가 **시작하는 자리**. 정본 `style.css` 4013~4015 `.sk-grid` 머리말이 «1행 오브 상단 92px(10.34%H)» 로
    /// 값을 글자로 못 박아 두었고(원본 shot-042340 · 앱 496×890 · 화소 재확인 y92), 1회차가 그것을 표 `PetSkillUi.json layout.sk_grid_top_h` 로 옮겼다.
    /// 여기서 재는 것: ⓐ 첫 행 오브의 **화면 위끝 기준** 자리가 표값 그대로인가(종전 8.96%H — 13px 위였다) ⓑ 패시브 배너와 겹치지 않는가
    /// (표값이 쌓인 높이보다 작아지면 격자가 배너를 타고 올라간다 — 한 줄 배선이 조용히 무너지는 유일한 길이다).
    /// </summary>
    public class SkillGridTopSiteTests
    {
        static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null; yield return null;
            float t = 0f;
            while (!(UiRoot.Instance != null && UiRoot.Instance.App != null && MetaHost.Ready) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            yield return null;
        }

        static Transform FindActive(Transform root, string name)
        {
            if (!root.gameObject.activeInHierarchy) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { Transform r = FindActive(root.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        /// <summary>앱 위끝에서 그 상자의 위끝까지 — 기준 높이(RefH) 로 환산한 px. 촬영 해상도와 무관하게 견줄 수 있다.</summary>
        static float TopFromApp(RectTransform app, RectTransform rt)
        {
            var a = new Vector3[4]; var c = new Vector3[4];
            app.GetWorldCorners(a); rt.GetWorldCorners(c);
            float appH = a[1].y - a[0].y;
            Assert.Greater(appH, 0f, "앱 상자 높이");
            return (a[1].y - c[1].y) / appH * UiKit.RefH;
        }

        [UnityTest]
        public IEnumerator 첫_행_오브_위끝이_표가_쥔_정본_10_34퍼센트H_에_선다()
        {
            yield return Boot();
            UiRoot.Instance.TabBar.OnTab("summon");
            yield return null;
            SkillPetSheet sheet = SkillPetSheet.Instance;
            Assert.IsNotNull(sheet, "소환 시트");
            sheet.Switch(SkillPetSheet.SubSkills);
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();

            RectTransform app = (RectTransform)UiRoot.Instance.App;
            Transform orb = FindActive(app, "sk-orb");
            Assert.IsNotNull(orb, "스킬 격자 첫 칸의 오브(sk-orb) — 세이브에 스킬이 하나도 없으면 이 자를 못 잰다");
            float want = PetSkillStyle.Px("sk_grid_top_h");
            float got = TopFromApp(app, (RectTransform)orb);
            Assert.AreEqual(want, got, 2f, "정본 .sk-grid 머리말 «1행 오브 상단 10.34%H» — 표값 그대로여야 한다(종전 8.96%H)");

            Transform banner = FindActive(app, "passive-banner");
            Assert.IsNotNull(banner, "패시브 배너");
            RectTransform brt = (RectTransform)banner;
            float bannerBottom = TopFromApp(app, brt) + brt.rect.height;
            Assert.Greater(got, bannerBottom, "격자가 배너를 타고 올라가면 안 된다");
            float gapH = (got - bannerBottom) / UiKit.RefH;
            Assert.AreEqual(0.0236f, gapH, 0.004f, "정본 «부제 띠 아래끝 ↔ 1행 오브 위끝 21px(2.36%H)» — 머리·배너 높이는 그대로 두고 틈만 맞춘 결과");
        }
    }
}
