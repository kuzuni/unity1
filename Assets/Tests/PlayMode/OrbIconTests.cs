using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T385 — 소환 결과 구슬 위 아이콘의 «조명 통합»(정본 `style.css` 6509~6547 이 주석으로 처방을 적어 둔 자리):
    /// ⑵ 약 4% 배럴 스케일 · ⑶ 구체 스페큘러를 아이콘 위에 한 겹 더(screen · α .35).
    /// ⑴ 접지 그림자는 T332 표(`DropShadowUi.json`)가 쥐고 있고 그 파일이 T411 산 lock 이라 이 자는 안 잰다.
    ///
    /// 이 자가 꼭 지키는 것 하나: **겹 상자는 구체 기준**이다. 정본의 «아이콘 한 변의 220%» 를 클론 아이콘(구체의 0.6~0.62)에
    /// 그대로 곱하면 겹이 구체의 1.32배로 부풀어 밝은 점이 구체 하이라이트에서 11%p 어긋난다(1회차 셈).
    /// </summary>
    public class OrbIconTests
    {
        static IEnumerator Boot()
        {
            PetSkillHost.SuppressSave = true;
            PetSkillHost.Seed = 20260916;
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            Scene active = SceneManager.GetActiveScene();
            for (int i = 0; i < 600 && !(SkillPetSheet.Instance != null && SkillPetSheet.Instance.gameObject.scene == active && PetSkillHost.Ready); i++) yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다");
            Assert.IsTrue(PetSkillHost.Ready);
            yield return null;
        }

        static SkillSummonResultView Open()
        {
            var list = new List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = "가" },
                new SkillSummonResultView.Entry { Key = "sk:b", IconKey = "sk_fireball", Rarity = "rare", Name = "나" },
            };
            return SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", list, "rare", null);
        }

        static Transform Find(Component root, string name)
        {
            foreach (Transform x in root.GetComponentsInChildren<Transform>(true)) if (x.name == name) return x;
            return null;
        }

        [UnityTest]
        public IEnumerator 아이콘은_배럴_배율을_쓰고_그_위에_구체_스페큘러가_한_겹_얹힌다()
        {
            yield return Boot();
            SkillSummonResultView v = Open();
            Assert.IsNotNull(v, "소환 결과 창이 안 열렸다");
            yield return null;
            Canvas.ForceUpdateCanvases();

            Transform icoT = Find(v, "sr-ico");
            Assert.IsNotNull(icoT, "구슬 위 아이콘(sr-ico)이 없다");
            RectTransform ico = (RectTransform)icoT;

            // ⑵ 배럴 스케일 — 수는 표가 쥔다(코드에 안 박는다).
            Assert.AreEqual(OrbIconUi.BarrelScale, ico.localScale.x, 1e-4f, "정본 6522 .sr-ico { transform: scale(1.04) }");
            Assert.AreEqual(ico.localScale.x, ico.localScale.y, 1e-4f, "가로세로 같은 배율");

            // ⑶ 스페큘러 겹 — 아이콘과 같은 칸(구체 래퍼) 안에서 아이콘 **뒤**(= 위)에 온다.
            Transform specT = Find(v, "sr-ico-spec");
            Assert.IsNotNull(specT, "아이콘 위 스페큘러 겹(sr-ico-spec)이 없다");
            RectTransform spec = (RectTransform)specT;
            Assert.AreSame(ico.parent, spec.parent, "겹은 아이콘과 같은 칸(구슬 래퍼) 안이다");
            Assert.Greater(spec.GetSiblingIndex(), ico.GetSiblingIndex(), "겹이 아이콘 위에 그려져야 한다");

            Image im = spec.GetComponent<Image>();
            Assert.IsNotNull(im, "겹에 Image 가 없다");
            Assert.IsNotNull(im.sprite, "겹이 구운 판을 안 쥐었다");
            Assert.AreEqual(OrbIconUi.Alpha, im.color.a, 1e-3f, "정본 6546 opacity: .35");
            Assert.IsFalse(im.raycastTarget, "장식 겹은 누름을 안 먹는다");
        }

        [UnityTest]
        public IEnumerator 스페큘러_겹은_아이콘이_아니라_구체_기준으로_놓여_밝은_점이_구체_하이라이트와_같은_쪽이다()
        {
            yield return Boot();
            SkillSummonResultView v = Open();
            Assert.IsNotNull(v);
            yield return null;
            Canvas.ForceUpdateCanvases();

            RectTransform spec = (RectTransform)Find(v, "sr-ico-spec");
            RectTransform ico = (RectTransform)Find(v, "sr-ico");
            RectTransform hi = (RectTransform)Find(v, "sr-hilite");
            Assert.IsNotNull(spec); Assert.IsNotNull(ico); Assert.IsNotNull(hi);
            RectTransform wrap = (RectTransform)spec.parent;
            float orb = wrap.rect.width;
            Assert.Greater(orb, 1f, "구슬 래퍼 폭");

            // ⓐ 겹 한 변 = 표의 상자(220%) × 정본 아이콘 비율(1/2.4) = 구체의 91.7%.
            double want = OrbIconUi.Box.W * OrbIconUi.IconFracOfOrb;
            Assert.AreEqual(want, spec.rect.width / orb, 0.01, "겹 상자는 **구체** 한 변의 비율로 잡는다");

            // ⓑ 클론 아이콘에 그대로 곱한 값(= 틀린 길)과는 뚜렷이 다르다 — 1회차가 경고한 자리를 못 박는다.
            double wrong = OrbIconUi.Box.W * (ico.rect.width / orb);
            Assert.Greater(Mathf.Abs((float)(wrong - want)), 0.2f, "클론 아이콘 비율로 곱하면 겹이 딴 크기다 — 그 길로 돌아가면 이 자가 운다");

            // ⓒ 첫 겹의 밝은 점을 구체 기준으로 옮기면 구체 하이라이트와 같은 쪽이다.
            OrbIconUi.Layer top = OrbIconUi.Layers[0];
            double bx = OrbIconRules.ToOrbFrac(want, top.AtX);
            double by = OrbIconRules.ToOrbFrac(want, top.AtY);            // CSS 와 같이 위가 0
            float hx = hi.anchorMin.x, hy = 1f - hi.anchorMin.y;          // 클론 하이라이트(앵커는 아래가 0)
            Assert.AreEqual(hx, (float)bx, 0.05f, "밝은 점의 가로가 구체 하이라이트와 같은 쪽 (겹 " + bx.ToString("0.000") + " ↔ 하이라이트 " + hx.ToString("0.000") + ")");
            Assert.AreEqual(hy, (float)by, 0.05f, "밝은 점의 세로가 구체 하이라이트와 같은 쪽 (겹 " + by.ToString("0.000") + " ↔ 하이라이트 " + hy.ToString("0.000") + ")");
        }
    }
}
