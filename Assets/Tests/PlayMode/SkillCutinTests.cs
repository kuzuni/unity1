using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TMPro;
using Forge.Core.Data;
using Forge.Core.Ui;
using Forge.Game.Battle;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T384 — 스킬 컷인 콜아웃(정본 `#skill-cutin` · ui.js 3759 `skillCutin(def)`)이 실제 오버레이 띠에 선다: 아이콘 + 이름을 그 스킬 색으로,
    /// 15% 지점 배율 1.25 · .8초 뒤 사라짐 · 글자·아이콘 둘 다 글로우 · 섬광 위에 선다. 시계는 <see cref="SkillCutin.Tick"/> 을 손으로 민다.
    /// </summary>
    public class SkillCutinTests
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

        static SkillDef AnyWithIcon(GameData data)
        {
            foreach (SkillDef d in data.Defs.SkillDefs) if (UiIcons.Has("sk_" + d.Id)) return d;
            return null;
        }

        [UnityTest]
        public IEnumerator 컷인은_스킬_색_아이콘과_이름으로_섰다가_8할초_뒤_사라진다()
        {
            yield return Boot();
            GameData data = GameData.LoadDirectory(System.IO.Path.Combine(Application.streamingAssetsPath, "data"));
            SkillDef def = AnyWithIcon(data);
            Assert.IsNotNull(def, "sk_* 아이콘이 있는 스킬이 하나도 없다(T31 아틀라스)");
            Color want; Assert.IsTrue(ColorUtility.TryParseHtmlString(def.Color, out want), "gamedata 스킬 색 " + def.Color);

            SkillCutin c = SkillCutin.Ensure();
            Assert.IsNotNull(c, "오버레이 띠가 없다");
            c.ManualClock = true;
            SkillCutinSpec sp = SkillCutin.Spec;
            c.Show(def);
            yield return null;

            Assert.IsTrue(c.Active, "시전 직후 돈다");
            Assert.AreEqual("skill-cutin", c.Root.name);
            Assert.AreSame(BattleOverlay.Instance.Layer, c.Root.parent, "전투 오버레이 띠 안(정본 #game-area 의 자식)");
            Assert.AreEqual(def.Name, c.Label.text, "스킬 이름");
            Assert.AreEqual(want.r, c.Label.color.r, 1e-3f, "글자 색 = 그 스킬 색 r"); Assert.AreEqual(want.g, c.Label.color.g, 1e-3f, "g"); Assert.AreEqual(want.b, c.Label.color.b, 1e-3f, "b");
            float rem = BattleOverlay.Rem;
            Assert.AreEqual(Mathf.Max((float)(sp.FontRem * rem), UiCatalog.Instance.Kind(TextKind.Sub).min), c.Label.fontSize, 0.01f, "정본 1.05rem — 하한 바로 위");
            Assert.AreEqual(sp.LsEm * 100.0, c.Label.characterSpacing, 1e-3, "letter-spacing .04em(TMP 1/100 em)");
            Assert.IsTrue(c.Label.fontMaterial.IsKeywordEnabled("UNDERLAY_ON"), "글자 글로우(text-shadow 0 0 10px currentColor)");
            Assert.AreEqual(want.r, c.Label.fontMaterial.GetColor("_UnderlayColor").r, 1e-3f, "글로우 색 = 스킬 색");
            Assert.IsNotNull(c.Icon); Assert.IsTrue(c.Icon.gameObject.activeSelf, "아이콘");
            Assert.AreSame(UiIcons.Skill(def.Id), c.Icon.sprite, "IconGen.skill(def.id) = 아틀라스 sk_<id>(런 688: 아틀라스 스프라이트 이름은 키에 꼬리가 붙어 문자열로 비교하지 않는다)");
            Assert.AreEqual((float)(sp.IconEm * c.Label.fontSize), c.Icon.rectTransform.rect.width, 0.5f, ".ico.cutin-ico 1.5em 정사각");
            Assert.IsNotNull(c.IconGlow); Assert.IsTrue(c.IconGlow.gameObject.activeSelf, "아이콘 글로우(drop-shadow 0 0 6px currentColor)");
            Assert.AreEqual(want.r, c.IconGlow.color.r, 1e-3f, "아이콘 글로우 색 = 스킬 색");
            Assert.Less(c.IconGlow.transform.GetSiblingIndex(), c.Icon.transform.GetSiblingIndex(), "글로우는 아이콘 뒤");
            // 자리: 위끝이 띠의 20% (피벗 가운데라 중심 = 20% + 반 높이)
            float layerH = BattleOverlay.Instance.Layer.rect.height;
            Assert.AreEqual(-(float)(sp.TopF * layerH) - c.Root.rect.height * 0.5f, c.Root.anchoredPosition.y, 0.5f, "top: 20%");
            Assert.AreEqual(0f, c.Alpha, 1e-6f, "0% opacity 0");
            Assert.AreEqual(0.3f, c.Root.localScale.x, 1e-4f, "0% scale(.3)");

            c.Tick(0.12f);   // 15%
            Assert.AreEqual(1f, c.Alpha, 1e-4f, "15% opacity 1");
            Assert.AreEqual(1.25f, c.Root.localScale.x, 1e-4f, "15% scale(1.25)");
            c.Tick(0.12f);   // 30%
            Assert.AreEqual(1f, c.Root.localScale.x, 1e-4f, "30% scale(1)");

            // 서열: 섬광(z5)이 있으면 그 위에 선다.
            BattleOverlay ov = BattleOverlay.Instance;
            ov.SkillFlash(def.Color);
            yield return null;
            Assert.Greater(c.Root.GetSiblingIndex(), ov.SkillFlashImage.transform.GetSiblingIndex(), "컷인(6)은 섬광(5) 위");

            c.Tick(0.6f);    // 누적 840ms > 800ms
            Assert.IsFalse(c.Active, "끝나면 멎는다");
            Assert.IsFalse(c.Root.gameObject.activeSelf, "꺼진다(forwards 의 마지막 키 = 투명)");

            // 연타는 처음부터(정본 리플로우 강제).
            c.Show(def);
            Assert.IsTrue(c.Active); Assert.AreEqual(0f, c.Alpha, 1e-6f, "다시 0% 부터");
            c.Tick(0.9f);
            Assert.IsFalse(c.Active);
        }
    }
}
