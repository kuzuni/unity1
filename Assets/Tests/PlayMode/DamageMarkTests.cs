using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TMPro;
using Forge.Core.Data;
using Forge.Game.Battle;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T440 — 정본 `style.css` 549 `.float-dmg.dmg-hero::before { content: '▼'; font-size: .72em; margin-right: .14em; vertical-align: .04em }`:
    /// «내게 들어온 피해» 의 ▼ 는 숫자의 .72 배 크기로 .14em 띄우고 .04em 올려 붙는다(546~548 «숫자 크기는 안 건드리고 기호만 작게»).
    /// 전엔 `"▼" + 숫자` 한 문자열이라 같은 크기였다. 값은 표 `Resources/DamageUi.json`.
    /// </summary>
    public class DamageMarkTests
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
                if (t.name == "dmg" && t.text == text) return t;
            return null;
        }

        [Test]
        public void 표는_정본_549_의_em_값_셋을_그대로_쥔다()
        {
            DamageStyle.Reset();
            Assert.AreEqual(0.72f, DamageStyle.L("hero_mark_em"), 1e-6f, "font-size .72em");
            Assert.AreEqual(0.14f, DamageStyle.L("hero_mark_gap_em"), 1e-6f, "margin-right .14em");
            Assert.AreEqual(0.04f, DamageStyle.L("hero_mark_rise_em"), 1e-6f, "vertical-align .04em");
        }

        [UnityTest]
        public IEnumerator 영웅_피해의_표식은_숫자의_72퍼센트_크기로_14em_띄워_04em_올려_따로_선다()
        {
            yield return Boot();
            UiRoot root = UiRoot.Instance;
            DamageNumbers.ResetMaterials();
            var nums = new DamageNumbers();
            nums.Spawn(new Vector3(0.2f, 1.0f, 0f), "801", "dmg-hero", 0, -40, 1);
            nums.Spawn(new Vector3(0.4f, 1.0f, 0f), "802", "dmg-crit", 0, -40, 1);
            yield return null;
            RectTransform layer = DamageNumbers.Layer(root);
            TextMeshProUGUI hero = Find(layer, "801"), crit = Find(layer, "802");
            Assert.IsNotNull(hero, "영웅 피해 숫자(접두 없이 «801»)");
            Assert.IsNotNull(crit, "크리 숫자");

            Transform mt = hero.transform.Find("mark");
            Assert.IsNotNull(mt, "▼ 조각 «mark» 가 숫자 상자의 자식으로 있다");
            Assert.IsTrue(mt.gameObject.activeSelf, "영웅 피해에서 ▼ 가 켜져 있다");
            TextMeshProUGUI m = mt.GetComponent<TextMeshProUGUI>();
            Assert.AreEqual("▼", m.text, "표식 글자");
            float fs = hero.fontSize;
            float em = DamageStyle.L("hero_mark_em"), gapEm = DamageStyle.L("hero_mark_gap_em"), riseEm = DamageStyle.L("hero_mark_rise_em");
            Assert.AreEqual(fs * em, m.fontSize, 0.01f, "▼ 크기 = 숫자 × .72");
            Assert.Less(m.fontSize, fs, "숫자보다 작다");
            Assert.AreEqual(fs * riseEm, m.rectTransform.anchoredPosition.y, 0.01f, "▼ 올림 = 숫자 × .04em");
            float gap = fs * gapEm, markW = DamageNumbers.ApproxWidth(m.fontSize, "▼"), numW = DamageNumbers.ApproxWidth(fs, "801");
            Assert.AreEqual(markW + gap, hero.margin.x, 0.01f, "숫자는 (표식 폭 + 틈) 만큼 왼쪽 margin — 한 덩어리가 원점에 가운데");
            Assert.AreEqual(-numW * 0.5f - gap * 0.5f, m.rectTransform.anchoredPosition.x, 0.5f, "▼ 가운데 = −숫자 폭/2 − 틈/2 (표식 오른끝 → 숫자 잉크 왼끝 = .14em)");
            Assert.AreEqual(hero.color, m.color, "▼ 는 숫자와 같은 잉크(정본 ::before 는 색을 물려받는다)");
            Assert.AreEqual(hero.fontSharedMaterial.IsKeywordEnabled("OUTLINE_ON"), m.fontSharedMaterial.IsKeywordEnabled("OUTLINE_ON"), "▼ 도 같은 키라인 겹");
            Assert.AreNotSame(hero.fontSharedMaterial, m.fontSharedMaterial, "크기가 달라 재질은 따로 굽는다(키라인 px 환산이 글자 크기에 걸린다)");
            // 다른 종류는 표식이 없고 margin 도 0
            Transform ct = crit.transform.Find("mark");
            Assert.IsTrue(ct == null || !ct.gameObject.activeSelf, "크리 숫자엔 ▼ 가 없다");
            Assert.AreEqual(0f, crit.margin.x, 1e-6f, "크리 숫자는 margin 0");
            nums.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator 풀에서_다시_나온_숫자는_종류에_따라_표식을_켜고_끈다()
        {
            yield return Boot();
            UiRoot root = UiRoot.Instance;
            DamageNumbers.ResetMaterials();
            var nums = new DamageNumbers();
            nums.Spawn(new Vector3(0.2f, 1.0f, 0f), "811", "dmg-hero", 0, -40, 1);
            yield return null;
            // 수명을 넘겨 풀로 돌려보낸 뒤 같은 상자를 크리로 다시 꺼낸다
            nums.Step((float)(Forge.Core.BattleFx.HitRules.DmgLifeMs / 1000.0) + 1f);
            Assert.AreEqual(0, nums.Count, "수명이 지나 풀로 돌아갔다");
            nums.Spawn(new Vector3(0.2f, 1.0f, 0f), "812", "dmg-crit", 0, -40, 1);
            yield return null;
            RectTransform layer = DamageNumbers.Layer(root);
            TextMeshProUGUI crit = Find(layer, "812");
            Assert.IsNotNull(crit, "풀에서 나온 크리 숫자");
            Transform mt = crit.transform.Find("mark");
            Assert.IsTrue(mt == null || !mt.gameObject.activeSelf, "영웅 피해였던 상자가 크리로 다시 나오면 ▼ 를 끈다");
            Assert.AreEqual(0f, crit.margin.x, 1e-6f, "margin 도 0 으로");
            nums.Spawn(new Vector3(0.3f, 1.0f, 0f), "813", "dmg-hero", 0, -40, 1);
            yield return null;
            TextMeshProUGUI hero = Find(layer, "813");
            Assert.IsNotNull(hero, "영웅 피해 숫자");
            Transform ht = hero.transform.Find("mark");
            Assert.IsNotNull(ht, "▼ 조각"); Assert.IsTrue(ht.gameObject.activeSelf, "▼ 가 켜진다");
            nums.Clear();
            yield return null;
        }
    }
}
