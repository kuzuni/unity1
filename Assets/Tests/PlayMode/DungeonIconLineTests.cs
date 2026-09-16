using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>T433 — 인라인 아이콘이 든 줄의 상자: 정본 `.ico` 기본 1.45em(863·1763·2597)이 줄 상자를 민다.
    /// 던전 상세의 보상 알약(아이콘 둘)과 열쇠 줄(아이콘 + 숫자)이 «글자 줄 상자» 가 아니라 «아이콘 줄 상자 + 패딩» 으로 서는가.
    /// 여는 채비는 `DungeonStageRowTests` 그대로(새 세이브는 hammer 가 잠겨 해금 뒤 연다).</summary>
    public class DungeonIconLineTests
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

        static Transform FindDeep(Transform t, string name)
        {
            foreach (Transform c in t.GetComponentsInChildren<Transform>(true)) if (c.name == name) return c;
            return null;
        }

        [UnityTest]
        public IEnumerator 던전_상세_보상_알약과_열쇠_줄은_아이콘_줄_상자_1_45em_으로_선다()
        {
            Assert.AreEqual(1.45f, DungeonStyle.L("ico_em"), 1e-6f, "정본 .ico 기본 1.45em(863·1763·2597)");

            yield return Boot();
            DungeonUiHost.Instance.S.BestChapter = 5;
            DungeonUiHost.Instance.S.BestStage = 1;
            UiRoot.Instance.TabBar.OnTab("dungeon");
            yield return null;
            DungeonDetailPopup.Open("hammer");
            yield return null;
            yield return null;
            Assert.IsTrue(DungeonDetailPopup.IsOpen, "던전 상세가 열린다(해금 뒤)");
            Canvas.ForceUpdateCanvases();
            RectTransform app = UiRoot.Instance.App;
            Transform overlay = FindDeep(app, "modal-dungeon-detail");
            Assert.IsNotNull(overlay, "던전 상세 팝업");
            RectTransform card = overlay.Find("card") as RectTransform;
            Assert.IsNotNull(card, "카드");
            RectTransform pill = card.Find("reward-pill") as RectTransform;
            RectTransform keys = FindDeep(card, "keys") as RectTransform;
            Assert.IsNotNull(pill, "보상 알약"); Assert.IsNotNull(keys, "열쇠 줄 글자");

            // 셈 ↔ 도우미: 아이콘 줄 상자 = 글자 x 1.45 + 기준선 아래 몫(글꼴 descender) · 글자 줄 상자보다 크다
            var f = UiFont.Primary.faceInfo;
            float subFs = DungeonPopups.Kind(TextKind.Sub), titleFs = DungeonPopups.Kind(TextKind.Title);
            float subIco = subFs * 1.45f + Mathf.Abs(f.descentLine) / f.pointSize * subFs;
            float titleIco = titleFs * 1.45f + Mathf.Abs(f.descentLine) / f.pointSize * titleFs;
            Assert.AreEqual(subIco, DungeonDetailPopup.IconLineH(TextKind.Sub), 1e-3f, "Sub 아이콘 줄 상자");
            Assert.AreEqual(titleIco, DungeonDetailPopup.IconLineH(TextKind.Title), 1e-3f, "Title 아이콘 줄 상자");
            Assert.Greater(subIco, DungeonPopups.LineH(TextKind.Sub), "아이콘 줄 상자 > 글자 줄 상자(Sub)");
            Assert.Greater(titleIco, DungeonPopups.LineH(TextKind.Title), "아이콘 줄 상자 > 글자 줄 상자(Title)");

            float pad = DungeonPopups.RemL("dgd_pill_pad_rem");
            Assert.AreEqual(subIco + pad * 2f, pill.rect.height, 0.5f, "알약 높이 = 아이콘 줄 상자 + 패딩 x 2(정본 셈 · 글자 줄이 아니다)");
            Assert.Greater(pill.rect.height, DungeonPopups.LineH(TextKind.Sub) + pad * 2f + 1f, "옛 값(글자 줄 + 패딩)보다 크다");
            Assert.AreEqual(titleIco, keys.rect.height, 0.5f, "열쇠 줄 상자 = 아이콘 줄 상자(Title)");
            Assert.Greater(keys.rect.height, DungeonPopups.LineH(TextKind.Title) + 1f, "옛 값 LineH(Title)보다 크다");

            DungeonDetailPopup.Close();
            yield return null;
        }
    }
}
