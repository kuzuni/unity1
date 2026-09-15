using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Tech;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T345 4회차 — 기술 트리 노드의 시간 배지(정본 style.css 2209 `.tech-tree-label .tech-tree-node-time` · ui.js 5420·5422):
    /// 연구 중·완료 노드만 «pp-ink 위 pp-green 글자» 알약(반지름 .6rem = 표 `tech_node_time_r_rem` · 패딩 .05/.45rem = catalog `tt_time_*`)이고
    /// 다른 노드의 «lv/5» 는 민글자다. 글자가 바뀌면 알약 폭이 따라간다(정본 inline-block).
    /// </summary>
    public class TechNodeTimeTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = Time.realtimeSinceStartup;
            while (!DungeonUiHost.Ready)
            {
                Assert.Less(Time.realtimeSinceStartup - t, 20f, "DungeonUiHost 가 20초 안에 Ready 되지 않았다");
                yield return null;
            }
            yield return null;
        }

        static DungeonUiHost H { get { return DungeonUiHost.Instance; } }

        [UnityTest]
        public IEnumerator 연구_중_노드만_시간_배지_알약이_서고_민노드는_민글자이며_완료_글자에_폭이_따라간다()
        {
            yield return Boot();
            TechPanel p = TechPanel.OpenTechTree();
            yield return null;
            p.ShowBranch("power");
            yield return null;
            string id = p.NodeIds[0];
            Assert.IsNull(p.NodeTimePill(id), "연구 전엔 배지가 없다(«lv/5» 민글자)");
            Assert.IsNotNull(p.NodeLabel(id));

            H.S.Potions = 1e9;
            TechPopups.OpenNode(id);
            yield return null;
            TechPopups.OnStart();
            yield return null;
            Assert.AreEqual(id, H.Tech.ResearchingId(), "연구가 시작됐다");
            p.Render();
            yield return null;

            RectTransform pill = p.NodeTimePill(id);
            Assert.IsNotNull(pill, "연구 중 노드 아래에 시간 배지 알약이 선다");
            Image bg = pill.Find("bg").GetComponent<Image>();
            Assert.AreEqual(UiKit.C("pp_ink"), bg.color, "정본 background: var(--pp-ink)");
            Assert.AreEqual(UiShapes.Rounded, bg.sprite, "둥근 9-슬라이스");
            Assert.AreEqual(UiShapes.RoundedMultiplier(RadiusUi.Px("tech_node_time_r_rem")), bg.pixelsPerUnitMultiplier, 1e-3f, "반지름 .6rem = 표 tech_node_time_r_rem");
            Assert.AreEqual(0.6f * RadiusUi.PxPerRem, RadiusUi.Px("tech_node_time_r_rem"), 1e-3f, "정본 2211 border-radius .6rem");
            TextMeshProUGUI label = pill.GetComponentInChildren<TextMeshProUGUI>();
            Assert.IsNotNull(label, "배지 안 글자");
            Assert.AreEqual(UiKit.C("pp_green"), label.color, "정본 color: var(--pp-green)");
            Assert.AreEqual(label.text, p.NodeLabel(id), "배지 글자가 곧 노드 라벨이다");
            float padX = DungeonPopups.RemL("tt_time_pad_x_rem");
            Assert.AreEqual(PetSkillKit.TextWidth(TextKind.Sub, label.text) + padX * 2f, pill.sizeDelta.x, 0.01f, "폭 = 글자 + 좌우 .45rem");
            Assert.AreEqual(DungeonPopups.RemL("tt_label_rem") - DungeonPopups.RemL("tt_time_mt_rem"), pill.sizeDelta.y, 0.01f, "높이 = 라벨 줄 − margin-top .1rem");
            foreach (string other in p.NodeIds)
                if (other != id) Assert.IsNull(p.NodeTimePill(other), other + ": 연구 중이 아닌 노드엔 배지가 없다");

            // 완료로 넘어가면 «완료!» 배지 — 다시 그려도 알약이고 폭이 글자를 따른다
            H.Tech.State.Research.EndsAt = H.Now() - 1;
            p.Render();
            yield return null;
            Assert.AreEqual("완료!", p.NodeLabel(id));
            RectTransform pill2 = p.NodeTimePill(id);
            Assert.IsNotNull(pill2, "완료 노드도 배지 알약");
            Assert.AreEqual(PetSkillKit.TextWidth(TextKind.Sub, "완료!") + padX * 2f, pill2.sizeDelta.x, 0.01f, "폭이 «완료!» 글자를 따른다");
        }
    }
}
