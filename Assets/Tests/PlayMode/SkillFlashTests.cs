using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Ui;
using Forge.Game.Battle;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T335 ⓑ — 스킬 시전 섬광(정본 `#skill-flash` css 1919~1921 · ui.js 3779 `skillFlash(color)`)이 실제 오버레이에서 돈다:
    /// 스킬 색으로 물든 방사형 한 장이 .5s 동안 0 → 1(25%) → 0 을 그리고, 비네트(z 12)보다 **아래**(z 5)에 선다. 시계는 <see cref="BattleOverlay.Tick"/> 을 손으로 민다(DamageVignetteTests 와 같은 길).
    /// </summary>
    public class SkillFlashTests
    {
        static IEnumerator Boot()
        {
            BattleScene.AutoBoot = false;
            if (BattleScene.Instance != null) UnityEngine.Object.Destroy(BattleScene.Instance.gameObject);
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!MetaHost.Ready && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다");
        }

        [UnityTest]
        public IEnumerator 섬광은_스킬_색_방사형으로_켜졌다_꺼지고_비네트_아래에_선다()
        {
            yield return Boot();
            var ov = BattleOverlay.Ensure();
            Assert.IsNotNull(ov, "오버레이가 없다");
            DungeonFxSpec sp = DungeonClearFx.Spec;

            ov.SkillFlash("#4dd0e1");   // gamedata skills 의 실제 색 하나
            Assert.IsTrue(ov.SkillFlashActive, "시전 직후 돈다");
            var im = ov.SkillFlashImage;
            Assert.IsNotNull(im); Assert.IsTrue(im.gameObject.activeSelf);
            Assert.AreEqual("skill-flash", im.name);
            Assert.AreEqual(0x4d / 255f, im.color.r, 1e-3, "스킬 색 r"); Assert.AreEqual(0xd0 / 255f, im.color.g, 1e-3, "g"); Assert.AreEqual(0xe1 / 255f, im.color.b, 1e-3, "b");
            Assert.AreEqual(0f, ov.SkillFlashAlpha, 1e-6, "0% opacity 0");
            Assert.IsNotNull(im.sprite); Assert.AreEqual("skill-flash", im.sprite.name, "방사형 한 장을 굽는다");
            Assert.AreEqual(ov.Layer, im.transform.parent, "오버레이 띠(#game-area) 안");

            ov.Tick(0.125f);
            Assert.AreEqual(1f, ov.SkillFlashAlpha, 1e-3, "25% 에서 opacity 1");
            ov.Tick(0.125f);
            float mid = ov.SkillFlashAlpha;
            Assert.Greater(mid, 0f); Assert.Less(mid, 1f);
            Assert.AreEqual((float)sp.FlashAlphaAt(250), mid, 1e-3, "50% 는 표의 값");

            // 비네트가 켜지면 스스로 맨 아래로 간다 — 섬광은 그보다 더 아래(정본 z 5 < 12)로 돌아와야 한다.
            ov.FlashDamage(1);
            ov.Tick(0.05f);
            Assert.AreEqual(0, im.transform.GetSiblingIndex(), "섬광이 맨 아래");
            Assert.Greater(ov.Vignette.transform.GetSiblingIndex(), im.transform.GetSiblingIndex(), "비네트는 섬광 위");

            ov.Tick(0.3f);   // 누적 600ms > 500ms
            Assert.IsFalse(ov.SkillFlashActive, "끝나면 멎는다");
            Assert.IsFalse(im.gameObject.activeSelf, "꺼진다");
            Assert.AreEqual(0f, ov.SkillFlashAlpha, 1e-6);

            // 연타는 처음부터(정본 리플로우 강제).
            ov.SkillFlash("#9575cd");
            ov.Tick(0.05f);
            float a1 = ov.SkillFlashAlpha;
            ov.SkillFlash("#9575cd");
            Assert.Less(ov.SkillFlashAlpha, a1, "다시 부르면 0 부터");
            Assert.AreEqual(0x95 / 255f, im.color.r, 1e-3, "새 스킬 색");

            Assert.Throws<FormatException>(() => ov.SkillFlash("보라"), "색이 아니면 조용히 넘기지 않는다");
            yield return null;
        }
    }
}
