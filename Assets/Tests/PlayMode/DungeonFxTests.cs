using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
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
    /// T335 ⓐⓒ — 던전 클리어 팝업의 작은 연출 셋(칸 팝 · 카드 가라앉기 · 딤 페이드)과 «연구 완료» 노드 테 맥동이 실제 화면에서 돈다.
    /// 그림이 아니라 **계층·값**으로 본다(촬영 목록에 던전 클리어 샷이 없다). 시계는 벽시계라 프레임 길이와 무관하게 «몇 ms 뒤» 로 잰다.
    /// </summary>
    public class DungeonFxTests
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

        static Transform FindIn(Transform root, string name)
        {
            foreach (Transform tr in root.GetComponentsInChildren<Transform>(true)) if (tr.name == name) return tr;
            return null;
        }

        static List<RectTransform> Cells(Transform card)
        {
            var list = new List<RectTransform>();
            foreach (RectTransform rt in card.GetComponentsInChildren<RectTransform>(true)) if (rt.name.StartsWith("cell-")) list.Add(rt);
            return list;
        }

        [UnityTest]
        public IEnumerator 클리어_팝업은_칸이_순서대로_튀고_수령_뒤_카드가_가라앉으며_딤이_걷힌_뒤_뿌리가_사라진다()
        {
            yield return Boot();
            H.S.BestChapter = 5;
            H.S.BestStage = 1;
            UiRoot.Instance.TabBar.OnTab("dungeon");
            yield return null;
            DungeonDetailPopup.Open("hammer");
            yield return null;
            DungeonDetailPopup.Enter();
            yield return null;
            H.Dungeons.OnClear();
            yield return null;
            Assert.IsTrue(DungeonClearPopup.IsOpen, "onClear → showDungeonClear");

            Transform ov = FindIn(UiRoot.Instance.App, "modal-dungeon-clear");
            Assert.IsNotNull(ov, "클리어 팝업 뿌리");
            Transform card = ov.Find("card");
            Assert.IsNotNull(card, "카드");
            DungeonClearFx pop = card.GetComponent<DungeonClearFx>();
            Assert.IsNotNull(pop, "카드에 칸 팝 러너가 붙는다(정본 dgc-pop)");
            Assert.AreEqual(DungeonClearFx.Phase.Pop, pop.Mode);
            List<RectTransform> cells = Cells(card);
            Assert.GreaterOrEqual(cells.Count, 1, "보상 칸");
            // 첫 프레임 — 마지막 칸은 아직 .3 근처(backwards · 칸마다 .09s 늦게)
            RectTransform last = cells[cells.Count - 1];
            Assert.Less(last.localScale.x, 0.95f, "첫 프레임의 마지막 칸은 아직 작다(from scale .3)");
            CanvasGroup lastG = last.GetComponent<CanvasGroup>();
            Assert.IsNotNull(lastG, "칸 알파는 CanvasGroup 으로");
            Assert.Less(lastG.alpha, 1f, "첫 프레임의 마지막 칸은 아직 투명하다");

            float t0 = Time.realtimeSinceStartup;
            while (card.GetComponent<DungeonClearFx>() != null)
            {
                Assert.Less(Time.realtimeSinceStartup - t0, 5f, "칸 팝이 5초 안에 안 끝났다");
                yield return null;
            }
            foreach (RectTransform c in cells)
            {
                Assert.AreEqual(1f, c.localScale.x, 1e-4f, "팝이 끝나면 원래 크기");
                CanvasGroup g = c.GetComponent<CanvasGroup>();
                Assert.IsTrue(g == null || g.alpha >= 0.999f, "팝이 끝나면 원래 알파");
            }

            // [보상 수령] — 논리적으로는 지금 닫힌다 · 뿌리는 러너가 걷을 때까지 남는다
            Image dim = ov.Find("dim").GetComponent<Image>();
            float dimA0 = dim.color.a;
            DungeonClearPopup.Confirm();
            Assert.IsFalse(DungeonClearPopup.IsOpen, "수령 직후 팝업은 닫힌 것으로 친다(정본 _dgclearBusy)");
            DungeonClearFx leave = ov.GetComponent<DungeonClearFx>();
            Assert.IsNotNull(leave, "뿌리에 떠남 러너가 붙는다");
            Assert.AreEqual(DungeonClearFx.Phase.Leave, leave.Mode);
            Assert.IsNull(card.GetComponent<DungeonClearFx>(), "돌던 팝은 끝냈다");

            while (leave != null && leave.ElapsedMs < 300) yield return null;   // 지연 .12s 지나 가라앉는 중 · 딤도 걷히는 중
            Assert.IsNotNull(leave, "300ms 엔 아직 뿌리가 있다(.57s 까지)");
            Assert.Less(card.localScale.x, 1f, "카드가 가라앉는다(scale → .9)");
            CanvasGroup cg = card.GetComponent<CanvasGroup>();
            Assert.IsNotNull(cg);
            Assert.Less(cg.alpha, 1f, "카드가 옅어진다");
            Assert.Less(dim.color.a, dimA0, "딤이 함께 걷힌다(.dgclear-out)");

            t0 = Time.realtimeSinceStartup;
            while (ov != null)
            {
                Assert.Less(Time.realtimeSinceStartup - t0, 5f, "떠남이 5초 안에 안 끝났다");
                yield return null;
            }
            Assert.IsNull(FindIn(UiRoot.Instance.App, "modal-dungeon-clear"), "카드·딤이 다 걷히면 뿌리가 사라진다(정본 setTimeout → hidden)");
        }

        [UnityTest]
        public IEnumerator 연구_완료_노드는_테가_브론즈와_초록_사이를_맥동하고_완료하면_멎는다()
        {
            yield return Boot();
            TechPanel p = TechPanel.OpenTechTree();
            yield return null;
            p.ShowBranch("power");
            yield return null;
            TechTree tree = H.Tech;
            string id = p.NodeIds[0];
            H.S.Potions = 1e9;
            TechPopups.OpenNode(id);
            yield return null;
            TechPopups.OnStart();
            yield return null;
            Assert.AreEqual(id, tree.ResearchingId());
            tree.State.Research.EndsAt = H.Now() - 1;
            p.Render();
            yield return null;
            Assert.AreEqual("완료!", p.NodeLabel(id));

            Transform node = FindIn(p.transform, "node-" + id);
            Assert.IsNotNull(node, "노드");
            Image ring = node.Find("circle").GetComponent<Image>();
            TechReadyPulse pulse = ring.GetComponent<TechReadyPulse>();
            Assert.IsNotNull(pulse, "완료 노드의 테에 맥동 러너가 붙는다(정본 tt-ready)");
            Assert.GreaterOrEqual(pulse.T, 0.0); Assert.LessOrEqual(pulse.T, 1.0);
            Color from = UiKit.C(DungeonClearFx.Spec.ReadyFromKey), to = UiKit.C(DungeonClearFx.Spec.ReadyToKey);
            Color c1 = ring.color;
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 0.35f) yield return null;
            Color c2 = ring.color;
            Assert.AreNotEqual(c1, c2, "0.35초 사이에 테 색이 움직인다(1.1s 왕복)");
            // 두 색은 늘 from 과 to 사이(성분별)
            for (int i = 0; i < 3; i++)
            {
                float lo = Mathf.Min(from[i], to[i]) - 1e-3f, hi = Mathf.Max(from[i], to[i]) + 1e-3f;
                Assert.IsTrue(c2[i] >= lo && c2[i] <= hi, "테 색 성분 " + i + " 이 from~to 밖이다");
            }

            TechPopups.OpenNode(id);
            yield return null;
            TechPopups.OnClaim();
            yield return null;
            Assert.IsNull(tree.ResearchingId(), "완료 → 연구 없음");
            Transform node2 = FindIn(p.transform, "node-" + id);
            Assert.IsNotNull(node2);
            Image ring2 = node2.Find("circle").GetComponent<Image>();
            Assert.IsNull(ring2.GetComponent<TechReadyPulse>(), "완료를 누르면 맥동이 멎는다(노드가 active 로 다시 선다)");
        }
    }
}
