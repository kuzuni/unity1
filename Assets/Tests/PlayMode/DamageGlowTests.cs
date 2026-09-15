using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TMPro;
using Forge.Core.BattleFx;
using Forge.Core.Ui;
using Forge.Game.Battle;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T333 10회차 — 데미지 숫자 글로우: 정본 `@keyframes dmgcrit`(565 0% · 567 9%)·`dmgkill`(578 0% · 580 7%)이 **태어나는 프레임**에 두는 겹이
    /// 표(`DmgGlowUi.json`)대로 공유 재질의 TMP 언더레이에 얹히고, 그 퍼센트를 지나면 쉬는 겹으로 갈리는가. 글로우가 없는 등급(일반타·영웅 피해)은 언더레이가 꺼져 있어야 한다.
    /// </summary>
    public class DamageGlowTests
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

        /// <summary>그 글자의 공유 재질에 표의 겹 <paramref name="key"/> 가 그대로 구워졌나(색·알파·흐림 = 표 → UnderlaySdf 환산).</summary>
        private static void AssertGlow(TextMeshProUGUI t, string key, string what)
        {
            Assert.IsNotNull(t, what + " 글자를 못 찾았다");
            Material m = t.fontSharedMaterial;
            Assert.IsNotNull(m, what + " 공유 재질");
            Assert.IsTrue(m.IsKeywordEnabled("UNDERLAY_ON"), what + " UNDERLAY_ON");
            Color want = DmgGlowUi.C(key), got = m.GetColor("_UnderlayColor");
            Assert.AreEqual(want.r, got.r, 1e-3, what + " 글로우 R");
            Assert.AreEqual(want.g, got.g, 1e-3, what + " 글로우 G");
            Assert.AreEqual(want.b, got.b, 1e-3, what + " 글로우 B");
            Assert.AreEqual(want.a, got.a, 1e-3, what + " 글로우 알파(정본 rgba 의 넷째 항)");
            DmgGlowSpec.Glow g = DmgGlowUi.Spec.Get(key);
            float gs = m.GetFloat("_GradientScale"), rc = m.HasProperty("_ScaleRatioC") ? m.GetFloat("_ScaleRatioC") : 1f;
            UnderlaySdf u = UnderlaySdf.FromPx(g.DxPx * KeylineUi.CssPx, g.DyPx * KeylineUi.CssPx, g.BlurPx * KeylineUi.CssPx,
                                              t.fontSize, gs, rc <= 0f ? 1f : rc, t.font.faceInfo.pointSize);
            Assert.AreEqual(u.Softness01, (double)m.GetFloat("_UnderlaySoftness"), 1e-3, what + " 흐림 = 표 " + g.BlurPx + "px 환산");
            Assert.AreEqual(0.0, (double)m.GetFloat("_UnderlayOffsetX"), 1e-3, what + " 정본 글로우는 치우침이 없다(0 0 Npx)");
            Assert.AreEqual(0.0, (double)m.GetFloat("_UnderlayOffsetY"), 1e-3, what + " 정본 글로우는 치우침이 없다");
        }

        [UnityTest]
        public IEnumerator 크리와_처치_숫자는_태어나는_겹으로_서고_정본_퍼센트에서_쉬는_겹으로_갈린다()
        {
            yield return Boot();
            UiRoot root = UiRoot.Instance;
            DamageNumbers.ResetMaterials();
            var nums = new DamageNumbers();
            nums.Spawn(new Vector3(0.1f, 1.0f, 0f), "711", "dmg", 0, -40, 1);
            nums.Spawn(new Vector3(0.2f, 1.0f, 0f), "712", "dmg-crit", 0, -40, 1);
            nums.Spawn(new Vector3(0.3f, 1.0f, 0f), "713", "dmg-kill", 0, -40, 1);
            yield return null;
            RectTransform layer = DamageNumbers.Layer(root);
            TextMeshProUGUI dmg = Find(layer, "711"), crit = Find(layer, "712"), kill = Find(layer, "713");
            AssertGlow(crit, "dmg_crit_born", "크리 태어나는 겹(565 0%)");
            AssertGlow(kill, "dmg_kill_born", "처치 태어나는 겹(578 0%)");
            // 정본에 글로우 키프레임이 없는 등급은 언더레이가 꺼져 있다 — 없는 겹을 지어내지 않는다
            Assert.IsNotNull(dmg, "일반타 글자");
            Assert.IsFalse(dmg.fontSharedMaterial.IsKeywordEnabled("UNDERLAY_ON"), "일반타(.float-dmg)는 글로우가 아니라 바닥 그림자 한 겹뿐 — 그 자리는 키라인 자 몫이다");
            Assert.AreNotSame(crit.fontSharedMaterial, kill.fontSharedMaterial, "크리·처치는 겹이 다르니 재질도 다르다");

            // 정본 9%(크리)·7%(처치)를 지나면 쉬는 겹으로 — 클론 수명은 HitRules.DmgLifeMs 라 비율로 센다
            float life = (float)(HitRules.DmgLifeMs / 1000);
            nums.Step(life * 0.05f);
            yield return null;
            AssertGlow(crit, "dmg_crit_born", "크리 5% — 아직 태어나는 겹");
            nums.Step(life * 0.06f);   // 누적 11% > 9%·7%
            yield return null;
            AssertGlow(crit, "dmg_crit_rest", "크리 11% — 쉬는 겹(567 9%)");
            AssertGlow(kill, "dmg_kill_rest", "처치 11% — 쉬는 겹(580 7%)");
            Assert.AreEqual(3, nums.Count, "셋 다 아직 살아 있다(수명 안)");

            // 재질은 «겹마다 하나» 를 나눠 쓴다 — 숫자마다 복제하면 초당 수십 개가 재질을 만든다(T50)
            nums.Spawn(new Vector3(0.4f, 1.0f, 0f), "714", "dmg-crit", 0, -40, 1);
            yield return null;
            TextMeshProUGUI crit2 = Find(layer, "714");
            AssertGlow(crit2, "dmg_crit_born", "새 크리도 태어나는 겹");
            nums.Step(life * 0.2f);
            yield return null;
            Assert.AreSame(crit.fontSharedMaterial, crit2.fontSharedMaterial, "같은 겹·같은 크기는 재질을 나눠 쓴다");
            nums.Clear();
            yield return null;
        }
    }
}
