using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T163 — 기술 트리 노드 원판 바닥의 «눌림 띠»(정본 `.tech-tree-node { box-shadow: inset 0 -.3rem 0 rgba(0,0,0,.22) }` · `.locked` .08 · `.tlocked` .12).
    /// 모든 노드에 띠가 있고 · 높이가 표 `tt_shade_rem`(.3rem) 만큼 아래로 내려간 원(마스크 초승달)이며 · 색이 면 상태 키를 따른다.
    /// 그림은 다음 런 `screen_tech-branch.png` 단면(면 아래 .3rem 이 면보다 어둡다)으로 눈 확인.
    /// </summary>
    public class TechNodeShadeTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t0 = Time.realtimeSinceStartup;
            while (!DungeonUiHost.Ready)
            {
                Assert.Less(Time.realtimeSinceStartup - t0, 20f, "DungeonUiHost 가 20초 안에 Ready 되지 않았다");
                yield return null;
            }
            yield return null;
        }

        [TearDown]
        public void CleanSave()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
        }

        [UnityTest]
        public IEnumerator 노드마다_바닥_띠가_있고_높이와_색이_표대로다()
        {
            yield return Boot();
            TechPanel p = TechPanel.OpenTechTree();
            yield return null;
            Assert.IsNotNull(p, "기술 트리 패널");
            p.ShowBranch("power");
            yield return null;
            Assert.Greater(p.NodeIds.Count, 0, "power 가지에 노드가 있어야 한다");

            float px = DungeonPopups.RemL("tt_shade_rem");
            Assert.Greater(px, 0f, "tt_shade_rem 표 값");
            int locked = 0, tlocked = 0, others = 0;
            foreach (string id in p.NodeIds)
            {
                Transform node = FindIn(p.transform, "node-" + id);
                Assert.IsNotNull(node, id + ": 노드 상자");
                Transform faceT = node.Find("circle/face");
                Assert.IsNotNull(faceT, id + ": 원판 면");
                Image face = faceT.GetComponent<Image>();
                Mask mask = faceT.GetComponent<Mask>();
                Assert.IsNotNull(mask, id + ": 면이 마스크다(초승달 클립)");
                Assert.IsTrue(mask.showMaskGraphic, id + ": 면 자체는 그대로 보인다");
                Transform shadeT = faceT.Find("shade");
                Assert.IsNotNull(shadeT, id + ": 바닥 띠");
                Image shade = shadeT.GetComponent<Image>();
                Assert.AreEqual(UiShapes.Circle, shade.sprite, id + ": 띠는 같은 원 스프라이트(마스크로 초승달만 남는다)");
                RectTransform srt = shade.rectTransform;
                Assert.AreEqual(-px, srt.anchoredPosition.y, 0.5f, id + ": 정본 inset 0 -.3rem — 원을 .3rem 내린다");
                Assert.AreEqual(0f, srt.anchoredPosition.x, 0.5f, id + ": 가로로는 안 움직인다");
                // 늘림 앵커에서 anchoredPosition 을 내리면 offsetMin·offsetMax 의 y 가 **둘 다** −px 가 된다(크기는 면 그대로 · 자리만 아래로) — 런 390 실측 (0, −10.92)
                Assert.AreEqual(-px, srt.offsetMin.y, 0.5f, id + ": 아래 여백 −px(면과 같은 크기 · 아래로만)");
                Assert.AreEqual(-px, srt.offsetMax.y, 0.5f, id + ": 위 여백 −px(면과 같은 크기 · 아래로만)");
                Assert.AreEqual(0f, srt.offsetMin.x, 0.5f, id + ": 가로 크기는 면과 같다");
                Assert.AreEqual(0f, srt.offsetMax.x, 0.5f, id + ": 가로 크기는 면과 같다");
                Assert.IsFalse(shade.raycastTarget, id + ": 띠는 탭을 안 먹는다");

                // 색 = 면 상태 키에 매인 검정 α(정본 .22/.08/.12) · 선형 색 공간이면 PerceivedDim 환산
                string key;
                if (face.color == UiKit.C("tech_locked")) { key = "tt_shade_locked"; locked++; }
                else if (face.color == UiKit.C("tech_tlocked")) { key = "tt_shade_tlocked"; tlocked++; }
                else { key = "tt_shade"; others++; }
                Color want = UiKit.PerceivedDim(UiKit.C(key));
                Assert.AreEqual(want.a, shade.color.a, 1e-3f, id + ": 띠 α = " + key);
                Assert.AreEqual(0f, shade.color.r, 1e-3f, id + ": 띠는 검정");
                Assert.Less(shade.color.a, 1f, id + ": 띠는 반투명(면이 비친다)");
            }
            // 새 세이브의 power 가지: 첫 노드는 열려 있고(locked · 흰 원) 뒤 단계는 tlocked(회색) — 두 상태가 다 서야 표 셋이 실제로 쓰인다
            Assert.Greater(locked, 0, "흰 원(locked) 노드가 있어야 한다");
            Assert.Greater(tlocked + others, 0, "회색(tlocked) 또는 색 노드가 있어야 한다");
            Debug.Log("[T163] power 가지 노드 " + p.NodeIds.Count + " · locked " + locked + " · tlocked " + tlocked + " · 그 밖 " + others + " · 띠 px " + px);
        }

        private static Transform FindIn(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
    }
}
