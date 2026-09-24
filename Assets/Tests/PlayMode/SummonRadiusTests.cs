using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Game;
using Forge.Game.Ui;
using Forge.Core.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T415 20회차 — 소환 결과의 둥근 모서리 둘(19회차가 T178 뒤로 남긴 KNOWN):
    /// 정본 5715 `.sr-streaks i { border-radius: .1rem }`(막대 폭 .15rem 에 .1 이라 양 끝이 반원) ·
    /// 7084~7088 `.sr-grid.one .sr-name { width: fit-content; padding: .2rem .5rem; border-radius: .5rem }`(x1 이름판 · 7046 의 92%·.3rem 을 덮는다).
    /// </summary>
    public class SummonRadiusTests
    {
        private static IEnumerator Boot()
        {
            PetSkillHost.SuppressSave = true;
            PetSkillHost.Seed = 20260924;
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            Scene active = SceneManager.GetActiveScene();
            for (int i = 0; i < 600 && !(SkillPetSheet.Instance != null && SkillPetSheet.Instance.gameObject.scene == active && PetSkillHost.Ready); i++) yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다");
            Assert.IsTrue(PetSkillHost.Ready);
            yield return null;
        }

        private static Transform FindIn(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        private static void AssertRadius(Transform t, float radiusPx, string what)
        {
            Assert.IsNotNull(t, what);
            Image img = t.GetComponent<Image>();
            Assert.IsNotNull(img, what + " Image");
            Assert.AreSame(UiShapes.Rounded, img.sprite, what + " 둥근 스프라이트");
            Assert.AreEqual(UiShapes.RoundedMultiplier(radiusPx), img.pixelsPerUnitMultiplier, 1e-4f, what + " 반지름");
        }

        [Test]
        public void 표는_빛줄기_끝_1rem_과_x1_이름판_5rem_을_정본_값_그대로_쥔다()
        {
            Assert.AreEqual(0.1f, RadiusUi.Px("sr_streak_r_rem") / RadiusUi.PxPerRem, 1e-4f, "style.css 5715 .sr-streaks i .1rem");
            Assert.AreEqual(0.5f, PetSkillStyle.L("sr_name_one_r_rem"), 1e-6f, "style.css 7088 .sr-grid.one .sr-name .5rem");
            Assert.AreEqual(0.5f, PetSkillStyle.L("sr_name_one_pad_x_rem"), 1e-6f, "style.css 7088 padding .2rem .5rem 의 좌우");
            Assert.AreEqual(0.3f, PetSkillStyle.L("sr_name_r_rem"), 1e-6f, "style.css 7046 .sr-name .3rem(여럿 격자는 그대로)");
        }

        /// <summary>구운 판은 양 끝 띠(반원)를 갖고, 붙인 막대는 9-슬라이스 + 띠의 화면 높이 = min(표 .1rem, 막대 반폭) 이 되는 배율이다.</summary>
        [UnityTest]
        public IEnumerator 빛줄기_막대는_반원_끝_띠를_가진_판을_9슬라이스로_붙이고_띠_높이는_표_반지름이다()
        {
            yield return Boot();
            var list = new List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = "가" },
                new SkillSummonResultView.Entry { Key = "sk:b", IconKey = "sk_fireball", Rarity = "common", Name = "나" },
                new SkillSummonResultView.Entry { Key = "sk:c", IconKey = "sk_fireball", Rarity = "rare", Name = "다" },
                new SkillSummonResultView.Entry { Key = "sk:d", IconKey = "sk_fireball", Rarity = "ultimate", Name = "라" },
            };
            SkillSummonResultView v = SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", list, "ultimate", null);
            Assert.IsNotNull(v, "결과 연출 팝업이 서지 않았다");
            yield return null; yield return null;
            Assert.Greater(v.StreakCount, 0, "수렴 빛줄기가 없다");
            float r = RadiusUi.Px("sr_streak_r_rem");
            for (int i = 0; i < v.StreakCount; i++)
            {
                Image si = v.StreakOf(i);
                Assert.IsNotNull(si, "빛줄기 " + i);
                Assert.IsNotNull(si.sprite, "빛줄기 판 " + i);
                Assert.Greater(si.sprite.border.y, 0.5f, "판의 아래 끝 띠(반원 높이 · 텍스처 px)");
                Assert.AreEqual(si.sprite.border.y, si.sprite.border.w, 1e-4f, "양 끝 띠가 같다(정본은 네 모서리 같은 반지름)");
                Assert.AreEqual(Image.Type.Sliced, si.type, "9-슬라이스로 붙인다 — 늘여도 끝이 안 찌그러진다");
                float barW = si.rectTransform.sizeDelta.x;
                float capPx = Mathf.Min(r, barW * 0.5f);
                Assert.Greater(capPx, 0.5f, "끝 높이가 0 이다");
                Assert.AreEqual(si.sprite.border.y / capPx, si.pixelsPerUnitMultiplier, 1e-3f, "띠의 화면 높이 = min(표 .1rem, 막대 반폭) 이 되는 배율");
            }
            // 여럿 격자의 이름판은 종전 그대로(7046 · 폭 92% · .3rem)
            Transform name = FindIn(v.transform, "sr-name");
            Assert.IsNotNull(name, "이름판(여럿)");
            AssertRadius(name.Find("bg"), PetSkillStyle.Px("sr_name_r_rem"), "여럿 격자 이름판(.sr-name 7046)");
            v.Close();
            yield return null;
        }

        /// <summary>x1 이름판은 글자 폭 + 좌우 .5rem 의 좁은 판(셀 폭 상한)이고 반지름 .5rem 이다.</summary>
        [UnityTest]
        public IEnumerator x1_이름판은_글자_폭에_붙는_좁은_판이고_반지름은_표의_5rem_이다()
        {
            yield return Boot();
            var one = new List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:z", IconKey = "sk_fireball", Rarity = "common", Name = "하나", IsNew = true },
            };
            SkillSummonResultView v = SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", one, "common", null);
            Assert.IsNotNull(v, "결과 연출 팝업(x1)이 서지 않았다");
            yield return null; yield return null;
            Transform cell = FindIn(v.transform, "sr-cell-0");
            Assert.IsNotNull(cell, "x1 셀");
            RectTransform name = (RectTransform)cell.Find("sr-name");
            Assert.IsNotNull(name, "x1 이름판");
            AssertRadius(name.Find("bg"), PetSkillStyle.Px("sr_name_one_r_rem"), "x1 이름판(.sr-grid.one .sr-name 7088)");
            float cw = ((RectTransform)cell).sizeDelta.x;
            float want = Mathf.Min(cw, PetSkillKit.TextWidth(TextKind.Button, "하나") + PetSkillStyle.Px("sr_name_one_pad_x_rem") * 2f);
            Assert.AreEqual(want, name.sizeDelta.x, 0.5f, "fit-content: 글자 폭 + 좌우 패딩(셀 폭 상한)");
            Assert.Less(name.sizeDelta.x, cw * PetSkillStyle.L("sr_name_w_f") - 1f, "x1 은 여럿 격자의 92% 풀폭 판이 아니다(정본 주석 7078~7083)");
            v.Close();
            yield return null;
        }
    }
}
