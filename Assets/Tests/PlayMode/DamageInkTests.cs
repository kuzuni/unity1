using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TMPro;
using Forge.Game.Battle;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T396 2회차 — 전투 숫자의 **글자 색**은 정본이 선택자에만 리터럴로 못박은 값이다(style.css 508 크리 · 511 처치 ·
    /// 526 스킬 · 527 영웅 피해 · 550 막음). 정본 509 가 그 뜻을 적어 뒀다: «크리 위계는 '크기'가 아니라 **색·펀치**로 준다».
    /// 클론은 크리를 전역 `cp`(#ff8a65 ↔ 정본 #ff8a1e)로, 스킬·영웅·막음·처치를 `stage_ink`(흰색)로 찍고 있었다 —
    /// **스킬(#82b1ff)·막음(#90caf9)은 파랑·하늘색인데 흰색이었다.** 값은 표(`PinnedColorUi.json`)가 쥔다.
    /// </summary>
    public class DamageInkTests
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

        private static void AssertInk(TextMeshProUGUI t, string key, string what)
        {
            Assert.IsNotNull(t, what + " 글자를 못 찾았다");
            Color want = PinnedColorUi.C(key);
            Assert.AreEqual(want.r, t.color.r, 1f / 255f, what + " r = 표 «" + key + "»");
            Assert.AreEqual(want.g, t.color.g, 1f / 255f, what + " g = 표 «" + key + "»");
            Assert.AreEqual(want.b, t.color.b, 1f / 255f, what + " b = 표 «" + key + "»");
        }

        [UnityTest]
        public IEnumerator 전투_숫자_다섯의_글자색이_정본이_못박은_값이다()
        {
            yield return Boot();
            UiRoot root = UiRoot.Instance;
            DamageNumbers.ResetMaterials();
            var nums = new DamageNumbers();
            nums.Spawn(new Vector3(0.1f, 1.0f, 0f), "701", "dmg-crit", 0, -40, 1);
            nums.Spawn(new Vector3(0.2f, 1.0f, 0f), "702", "dmg-kill", 0, -40, 1);
            nums.Spawn(new Vector3(0.3f, 1.0f, 0f), "703", "dmg-skill", 0, -40, 1);
            nums.Spawn(new Vector3(0.4f, 1.0f, 0f), "704", "dmg-hero", 0, -40, 1);
            nums.Spawn(new Vector3(0.5f, 1.0f, 0f), "705", "block", 0, -40, 1);
            yield return null;
            RectTransform layer = DamageNumbers.Layer(root);
            TextMeshProUGUI crit = Find(layer, "701"), kill = Find(layer, "702"), skill = Find(layer, "703");
            TextMeshProUGUI hero = Find(layer, "▼704"), block = Find(layer, "705");
            AssertInk(crit, "dmg_crit_ink", "크리(정본 508 #ff8a1e)");
            AssertInk(kill, "dmg_kill_ink", "처치(정본 511 #fff3c4)");
            AssertInk(skill, "dmg_skill_ink", "스킬(정본 526 #82b1ff)");
            AssertInk(hero, "dmg_hero_ink", "영웅 피해(정본 527 #fff2f4)");
            AssertInk(block, "dmg_block_ink", "막음(정본 550 #90caf9)");

            // 색이 곧 위계다 — 스킬·막음은 **파랑 쪽**(B > R)이고 크리는 **주황 쪽**(R > B)이다. 전부 흰색이던 종전이면 이 줄들이 넘어진다.
            Assert.Greater(skill.color.b, skill.color.r + 0.2f, "스킬은 파랑이다(흰색이 아니다)");
            Assert.Greater(block.color.b, block.color.r + 0.1f, "막음은 하늘색이다(흰색이 아니다)");
            Assert.Greater(crit.color.r, crit.color.b + 0.4f, "크리는 주황이다");
            Assert.AreNotEqual(crit.color, kill.color, "크리와 처치는 다른 색이다(정본이 위계를 색으로 준다)");
            nums.Clear();
            yield return null;
        }
    }
}
