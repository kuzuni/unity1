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
    /// T415 14회차 — 시대 막대의 모서리: 정본 4701 `.af-age-bar { border-radius: .6rem }` · 5083 `.fi-age-bar { border-radius: .55rem }`.
    /// 종전 `ForgeUi.AgeBar` 는 두 막대를 «막대 높이 × .25»(1.75rem 의 .4375rem) 한 리터럴로 그려 둘 다 정본과 어긋났다(§1 «수치는 코드에 박지 않는다» · ⓒ 갈래).
    /// 반지름은 9-슬라이스 배율(<see cref="UiShapes.RoundedMultiplier"/>)로 읽는다 — 화면 반지름 = 표값 × 1rem px.
    /// </summary>
    public class AgeBarRadiusTests
    {
        static float Rem { get { return UiKit.L("rem_h") * UiKit.RefH; } }
        static float Mult(string key) { return UiShapes.RoundedMultiplier(RadiusUi.Px(key)); }

        private static IEnumerator Boot()
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

        static RectTransform FindDeep(Transform root, string name)
        {
            foreach (RectTransform r in root.GetComponentsInChildren<RectTransform>(true)) if (r.name == name) return r;
            return null;
        }

        [Test]
        public void 표는_두_막대의_모서리를_정본_값_그대로_쥔다()
        {
            Assert.AreEqual(0.6f * Rem, RadiusUi.Px("af_age_bar_r_rem"), 1e-3f, "정본 4701 `.af-age-bar { .6rem }`");
            Assert.AreEqual(0.55f * Rem, RadiusUi.Px("fi_age_bar_r_rem"), 1e-3f, "정본 5083 `.fi-age-bar { .55rem }`");
            Assert.AreNotEqual(RadiusUi.Px("af_age_bar_r_rem"), RadiusUi.Px("fi_age_bar_r_rem"), "두 막대는 정본 값이 다르다 — 한 리터럴로 뭉개면 안 된다");
            Assert.Greater(Mathf.Abs(1.75f * Rem * 0.25f - RadiusUi.Px("fi_age_bar_r_rem")), 1e-3f, "종전 «막대 높이 × .25»(.4375rem)가 아니다");
        }

        [UnityTest]
        public IEnumerator 확률_정보_막대와_자동_제련_막대의_모서리가_각_표값으로_선다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            Assert.IsNotNull(h);
            // 2차 — 런 1209 빨강: 새 세이브는 2-10 전이라 `ForgeAutoPopup.Open` 이 🔒 토스트만 내고 팝업을 안 연다(ForgeHost.OnAutoForgeBtn → Open 24행) → `AgePatternTests` 와 같이 해금하고
            //   제련 레벨을 촬영과 같은 29 로 올려 열 시대 행이 다 서게 한다(레벨 1 은 뒤 시대 확률이 0 이라 행이 없다).
            h.S.BestChapter = 3; h.S.BestStage = 1;
            h.S.ForgeLevel = 29;
            h.Pull();
            Assert.IsTrue(h.AutoForgeUnlocked, "2-10 뒤 해금");
            h.Push();
            ForgeInfoPopup.Open(h);
            yield return null; yield return null;
            Popup info = h.Meta.Popups.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(info, "확률 정보 팝업");
            int seen = 0;
            foreach (string age in h.Defs.Ages)
            {
                RectTransform bar = FindDeep(info.Root, "age-" + age);
                if (bar == null) continue;
                seen++;
                Image line = bar.Find("bar/line").GetComponent<Image>();
                Assert.AreEqual(Mult("fi_age_bar_r_rem"), line.pixelsPerUnitMultiplier, 1e-3f, "확률 정보 막대 " + age + ": 모서리가 표 «fi_age_bar_r_rem»(.55rem) 이 아니다");
                Transform seg = bar.Find("next/face");
                if (seg != null) Assert.AreEqual(Mult("fi_age_bar_r_rem"), seg.GetComponent<Image>().pixelsPerUnitMultiplier, 1e-3f, "다음 확률 칸도 막대와 같은 모서리(정본 `.fi-age-next` 는 제 반지름이 없고 막대에 잘린다)");
            }
            Assert.Greater(seen, 0, "확률 정보 막대를 하나도 못 찾았다");
            h.Meta.Popups.Hide(ForgeInfoPopup.Name);
            yield return null;

            ForgeAutoPopup.Open(h);
            yield return null; yield return null;
            Popup auto = h.Meta.Popups.Find(ForgeAutoPopup.Name);
            Assert.IsNotNull(auto, "자동 제련 팝업");
            int seenAf = 0;
            foreach (string age in h.Defs.Ages)
            {
                RectTransform bar = FindDeep(auto.Root, "af-age-" + age);
                if (bar == null) continue;
                seenAf++;
                Image line = bar.Find("bar/line").GetComponent<Image>();
                Assert.AreEqual(Mult("af_age_bar_r_rem"), line.pixelsPerUnitMultiplier, 1e-3f, "자동 제련 막대 " + age + ": 모서리가 표 «af_age_bar_r_rem»(.6rem) 이 아니다");
            }
            Assert.Greater(seenAf, 0, "자동 제련 막대를 하나도 못 찾았다");
            Debug.Log("[T415] 확률 정보 막대 " + seen + " · 자동 제련 막대 " + seenAf + " · fi " + RadiusUi.Px("fi_age_bar_r_rem").ToString("0.0") + "px · af " + RadiusUi.Px("af_age_bar_r_rem").ToString("0.0") + "px");
            h.Meta.Popups.Hide(ForgeAutoPopup.Name);
            yield return null;
        }
    }
}
