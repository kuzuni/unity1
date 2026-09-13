using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TMPro;
using Forge.Core.Ui;
using Forge.Game.Battle;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T109 6회차 — 데미지 숫자(`Battle/DamageNumbers`)의 키라인이 정본 `-webkit-text-stroke` 폭표(`KeylineUi.json` px: `.float-dmg .6px` · `.dmg-kill 1px` · `.dmg-hero .55px`)대로
    /// 공유 재질에 px 갈래(D = W · T104 식)로 얹히는가. 전에는 코드 상수 `_OutlineWidth .2`(여백 비율 · D 0)였다.
    /// </summary>
    public class DamageKeylineTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && UiRoot.Instance != null && UiRoot.Instance.App != null && Camera.main != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(UiRoot.Instance, "UiRoot 가 서지 않았다");
            Assert.IsNotNull(Camera.main, "메인 카메라가 없다");
            yield return null;
        }

        private static TextMeshProUGUI Find(RectTransform layer, string text)
        {
            foreach (TextMeshProUGUI t in layer.GetComponentsInChildren<TextMeshProUGUI>(false))
                if (t.text == text) return t;
            return null;
        }

        private static void AssertStroke(TextMeshProUGUI t, string strokeKey, string what)
        {
            Assert.IsNotNull(t, what + " 글자를 못 찾았다");
            Material m = t.fontSharedMaterial;
            Assert.IsNotNull(m, what + " 공유 재질");
            StringAssert.StartsWith("dmg outline", m.name, what + " 재질 이름");
            float g = m.GetFloat("_GradientScale"), r = m.GetFloat("_ScaleRatioA"), ps = t.font.faceInfo.pointSize;
            OutlineSdf want = OutlineSdf.FromStroke(KeylineUi.Px(strokeKey), t.fontSize, g, r, ps);
            Assert.Greater(want.Width01, 0, what + " 표 환산 W");
            Assert.IsFalse(want.Clipped, what + " 획이 여백 안이어야 한다");
            Assert.AreEqual((double)want.Width01, (double)m.GetFloat("_OutlineWidth"), 1e-3, what + " _OutlineWidth = FromStroke(" + strokeKey + " " + KeylineUi.Px(strokeKey).ToString("0.00") + "px · 글자 " + t.fontSize + ")");
            Assert.AreEqual((double)want.Dilate, (double)m.GetFloat("_FaceDilate"), 1e-3, what + " _FaceDilate = 같은 식의 D(채움을 안 먹는 갈래)");
            Assert.IsTrue(m.IsKeywordEnabled("OUTLINE_ON"), what + " OUTLINE_ON");
        }

        [UnityTest]
        public IEnumerator 데미지_숫자_셋의_키라인이_정본_폭표대로_공유_재질에_얹힌다()
        {
            yield return Boot();
            UiRoot root = UiRoot.Instance;
            DamageNumbers.ResetMaterials();
            var nums = new DamageNumbers();
            nums.Spawn(new Vector3(0.1f, 1.0f, 0f), "601", "dmg", 0, -40, 1);
            nums.Spawn(new Vector3(0.2f, 1.0f, 0f), "602", "dmg-kill", 0, -40, 1);
            nums.Spawn(new Vector3(0.3f, 1.0f, 0f), "603", "dmg-hero", 0, -40, 1);
            nums.Spawn(new Vector3(0.4f, 1.0f, 0f), "604", "dmg-crit", 0, -40, 1);
            yield return null;
            RectTransform layer = DamageNumbers.Layer(root);
            Assert.AreEqual(4, nums.Count, "넷이 살아 있다");
            TextMeshProUGUI dmg = Find(layer, "601"), kill = Find(layer, "602"), hero = Find(layer, "▼603"), crit = Find(layer, "604");
            AssertStroke(dmg, "float_dmg", "dmg(.float-dmg .6px)");
            AssertStroke(kill, "float_dmg_kill", "kill(.dmg-kill 1px)");
            AssertStroke(hero, "float_dmg_hero", "hero(.dmg-hero .55px)");
            AssertStroke(crit, "float_dmg", "crit(.float-dmg 를 물려받는다)");
            // 획이 다르면 재질도 다르다 · 같은 키·크기는 재질을 나눠 쓴다(숫자마다 복제 안 함)
            Assert.AreNotSame(dmg.fontSharedMaterial, hero.fontSharedMaterial, "획이 다르면 재질도 다르다");
            Assert.AreNotSame(dmg.fontSharedMaterial, kill.fontSharedMaterial, "kill 1px 과 dmg .6px 은 다른 재질");
            nums.Spawn(new Vector3(0.5f, 1.0f, 0f), "605", "dmg", 0, -40, 1);
            yield return null;
            Assert.AreSame(dmg.fontSharedMaterial, Find(layer, "605").fontSharedMaterial, "같은 종류는 재질을 나눠 쓴다(숫자마다 복제 안 함)");
            nums.Clear();
            yield return null;
        }
    }
}
