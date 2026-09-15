using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Core.Forging;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T383 2회차 — 정본 `ui.js` 가 `<br>` 로 줄 수를 못박은 자리(`tools/check_br_lines.py` 의 표 22 → 클론 자리 21)가 **화면에서** 그 줄 수로 서는가.
    /// 자(파이썬)는 코드가 줄 수를 아는지만 본다 — 하한(Sub 36)이 한 번 더 접는 꼴(런 666 pass-desc 세 줄)은 여기서만 보인다.
    /// 한 상자(`\n`)는 <c>textInfo.lineCount</c> · 아이콘 줄 갈래(<see cref="IconTextStack"/>)는 «line-N» 행 수 = 정본 줄 수이고 행마다 한 줄.
    /// 새 세이브로 열리는 자리부터(펫·탈것 상세 · 도전 행 · 판매 경고 밖 자리는 다음 회차).
    /// </summary>
    public class BrLinesTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && DungeonUiHost.Ready && UiRoot.Instance != null && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready && MetaHost.Ready && DungeonUiHost.Ready, "호스트 셋이 20초 안에 준비되지 않았다");
            yield return null;
        }

        static Transform Find(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        static int Lines(TextMeshProUGUI t)
        {
            Canvas.ForceUpdateCanvases();
            t.ForceMeshUpdate();
            return t.textInfo.lineCount;
        }

        /// <summary>아이콘 줄 갈래: «line-N» 행 수와 행마다 한 줄인지.</summary>
        static int StackRows(Transform stack, string what)
        {
            int rows = 0;
            for (int i = 0; i < stack.childCount; i++)
            {
                Transform c = stack.GetChild(i);
                if (!c.name.StartsWith("line-")) continue;
                rows++;
                foreach (TextMeshProUGUI t in c.GetComponentsInChildren<TextMeshProUGUI>(true))
                    if (t.gameObject.activeInHierarchy && !string.IsNullOrEmpty(t.text)) Assert.AreEqual(1, Lines(t), what + ": 행 «" + c.name + "» 의 글 «" + t.text + "» 은 한 줄");
            }
            return rows;
        }

        /// <summary>정본 1511 «대장간<br>레벨 N»(새 세이브 = 업그레이드 가능) · 1551 «자동<br>OFF/🔒» — 장비 시트 두 버튼.</summary>
        [UnityTest]
        public IEnumerator 장비_시트_대장간_버튼과_자동_버튼은_정본대로_두_줄이다()
        {
            yield return Boot();
            Transform fb = Find(UiRoot.Instance.Sheet.transform, "forge-btn");
            Assert.IsNotNull(fb, "대장간 버튼(forge-btn)");
            TextMeshProUGUI ft = fb.GetComponentInChildren<TextMeshProUGUI>(true);
            Assert.IsNotNull(ft, "대장간 버튼 글");
            Assert.IsTrue(ft.text.Contains("\n"), "코드가 두 줄을 안다: " + ft.text);
            Assert.AreEqual(2, Lines(ft), "정본 1506·1507·1511 `<br>` = 두 줄 — 실제 «" + ft.text.Replace("\n", "⏎") + "»");
            Transform stack = Find(UiRoot.Instance.Sheet.transform, "label-stack");
            Assert.IsNotNull(stack, "자동 버튼의 줄 갈래(label-stack)");
            Assert.AreEqual(2, StackRows(stack, "자동 버튼"), "정본 1551 «자동<br>…» = 두 행");
        }

        /// <summary>정본 2047 «레벨 N 업그레이드<br><small>코인 · 시간</small>» — 대장간 정보 팝업의 업그레이드 버튼(새 세이브 = 업그레이드 가능 갈래).</summary>
        [UnityTest]
        public IEnumerator 대장간_정보_업그레이드_버튼은_정본대로_두_행이다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeInfoPopup.Open(h);
            yield return null;
            Popup p = PopupLayer.Instance.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(p, "대장간 정보 팝업");
            Transform up = Find(p.Root, "fi-upgrade");
            Assert.IsNotNull(up, "업그레이드 버튼(fi-upgrade)");
            Transform stack = Find(up, "label-stack");
            Assert.IsNotNull(stack, "업그레이드 버튼의 줄 갈래");
            Assert.AreEqual(2, StackRows(stack, "업그레이드 버튼"), "정본 2047 = 두 행");
            PopupLayer.Instance.Hide(ForgeInfoPopup.Name);
            yield return null;
        }

        /// <summary>정본 4678 «이전 스테이지<br>소탕» — 던전 상세 소탕 버튼.</summary>
        [UnityTest]
        public IEnumerator 던전_상세_소탕_버튼은_정본대로_두_줄이다()
        {
            yield return Boot();
            DungeonDetailPopup.Open("hammer");
            yield return null;
            Assert.IsNotNull(DungeonDetailPopup.SweepButton, "소탕 버튼");
            TextMeshProUGUI t = DungeonPopups.Root(DungeonDetailPopup.SweepButton).GetComponentInChildren<TextMeshProUGUI>(true);
            Assert.IsNotNull(t, "소탕 버튼 글");
            Assert.AreEqual(2, Lines(t), "정본 4678 = 두 줄 — 실제 «" + t.text.Replace("\n", "⏎") + "»");
            DungeonDetailPopup.Close();
            yield return null;
        }

        /// <summary>정본 4812 «…유지하면 시즌 종료 시<br>다음 보상을 받을 수 있습니다:» — 리그 보상 안내.</summary>
        [UnityTest]
        public IEnumerator 리그_보상_안내는_정본대로_두_줄이다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            LeagueSheet.OpenRewards(h);
            yield return null;
            Popup p = PopupLayer.Instance.Find(LeagueSheet.RewardsName);
            Assert.IsNotNull(p, "리그 보상 팝업");
            Transform d = Find(p.Root, "desc");
            Assert.IsNotNull(d, "안내 글(desc)");
            TextMeshProUGUI t = d.GetComponent<TextMeshProUGUI>();
            Assert.AreEqual(2, Lines(t), "정본 4812 = 두 줄 — 실제 «" + t.text.Replace("\n", "⏎") + "»");
            PopupLayer.Instance.Hide(LeagueSheet.RewardsName);
            yield return null;
        }

        /// <summary>정본 5848~5849 «…됩니다<br>· ⚠ 보유 중인 기존 …<br>· 이후 새로 …» — 승천 초점 카드 효과 글줄 세 줄.</summary>
        [UnityTest]
        public IEnumerator 승천_효과_글줄은_정본대로_세_줄이다()
        {
            yield return Boot();
            AscendPopup.Open("forge");
            yield return null;
            Transform eff = Find(AscendPopup.Root, "eff");
            Assert.IsNotNull(eff, "효과 글줄(eff)");
            TextMeshProUGUI t = eff.GetComponent<TextMeshProUGUI>();
            Assert.AreEqual(3, Lines(t), "정본 5848·5849 = 세 줄 — 실제 «" + t.text.Replace("\n", "⏎") + "»");
            AscendPopup.Close();
            yield return null;
        }

        /// <summary>정본 3863 «…더 최신입니다.<br>같거나 이전 시대면 …» — 판매 경고 안내.</summary>
        [UnityTest]
        public IEnumerator 판매_경고_안내는_정본대로_두_줄이다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeItem a = h.Engine.RollItem(), b = h.Engine.RollItem();
            ForgeCraftPopup.ShowSellConfirm(h, a, b);
            yield return null;
            Popup p = PopupLayer.Instance.Find(ForgeCraftPopup.SellName);
            Assert.IsNotNull(p, "판매 경고 팝업");
            Transform n = Find(p.Root, "note");
            Assert.IsNotNull(n, "안내 글(note)");
            TextMeshProUGUI t = n.GetComponent<TextMeshProUGUI>();
            Assert.AreEqual(2, Lines(t), "정본 3863 = 두 줄 — 실제 «" + t.text.Replace("\n", "⏎") + "»");
            PopupLayer.Instance.Hide(ForgeCraftPopup.SellName);
            yield return null;
        }
    }
}
