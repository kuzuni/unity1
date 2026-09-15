using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T345 7회차 — 기술 트리의 뒤로 버튼은 **제 반지름**을 쓴다: 정본 `style.css` 2255 `.panel .btn.tech-tree-back { border-radius: .6rem }` 가
    /// 공용 뒤로 버튼(클론 catalog `back_radius_rem` .45rem)을 덮는다. 공용 <see cref="DungeonPopups.BackButton"/> 에 «반지름 키» 인자를
    /// 하나 달아 기술 트리의 두 자리만 표(`RadiusUi.json` `tech_back_r_rem`)를 읽게 했다 — 다른 화면(던전 시트)은 한 글자도 안 바뀐다.
    /// 반지름은 9-슬라이스 배율(<see cref="UiShapes.RoundedMultiplier"/>)로 읽는다(T345 3회차와 같은 자).
    /// </summary>
    public class TechRadiusTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && UiRoot.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            yield return null;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { Transform r = FindDeep(root.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        [Test]
        public void 표의_기술_트리_뒤로_버튼_값은_정본_점육rem_이고_공용값과_다르다()
        {
            Assert.AreEqual(0.6f * RadiusUi.PxPerRem, RadiusUi.Px("tech_back_r_rem"), 1e-3f, "정본 2255 `.panel .btn.tech-tree-back` .6rem");
            float shared = UiKit.L("back_radius_rem") * RadiusUi.PxPerRem;
            Assert.AreNotEqual(RadiusUi.Px("tech_back_r_rem"), shared, "덮는 값이 공용값과 같으면 이 자리는 애초에 없는 일이다");
        }

        [Test]
        public void 표의_기술_노드_버튼_값은_정본_점육rem_이고_공용_버튼값과_다르다()
        {
            // T345 8회차 — 정본 4612 `.tech-btns .btn { border-radius: .6rem }` 이 공용 `.btn`(663 `.55rem`)을 덮는다.
            Assert.AreEqual(0.6f * RadiusUi.PxPerRem, RadiusUi.Px("tech_btn_r_rem"), 1e-3f, "정본 4612 .tech-btns .btn .6rem");
            Assert.AreNotEqual(RadiusUi.Px("tech_btn_r_rem"), UiKit.L("btn_radius_rem") * RadiusUi.PxPerRem, "공용 버튼값과 같으면 이 자리는 애초에 없는 일이다");
        }

        [UnityTest]
        public IEnumerator 기술_트리_뒤로_버튼은_정본_점육rem_으로_선다()
        {
            yield return Boot();
            UiRoot.Instance.TabBar.OnTab("summon");
            yield return null;
            SkillPetSheet sheet = SkillPetSheet.Instance;
            Assert.IsNotNull(sheet, "소환 시트");
            sheet.Switch(SkillPetSheet.SubTech);
            yield return null;
            yield return null;
            TechPanel tp = TechPanel.Instance;
            Assert.IsNotNull(tp, "기술 트리 패널");

            Transform back = FindDeep(tp.transform, "back-btn");
            Assert.IsNotNull(back, "기술 트리 뒤로 버튼");
            Image bg = back.Find("bg").GetComponent<Image>();
            Assert.AreEqual(UiShapes.Rounded, bg.sprite, "둥근 9-슬라이스");
            Assert.AreEqual(UiShapes.RoundedMultiplier(RadiusUi.Px("tech_back_r_rem")), bg.pixelsPerUnitMultiplier, 1e-3f,
                "반지름이 표 «tech_back_r_rem»(정본 .6rem) 와 달라졌다");
            Assert.AreNotEqual(UiShapes.RoundedMultiplier(UiKit.L("back_radius_rem") * RadiusUi.PxPerRem), bg.pixelsPerUnitMultiplier,
                "공용 .45rem 을 그대로 쓰면 이 회차가 한 일이 없다");
        }
    }
}
