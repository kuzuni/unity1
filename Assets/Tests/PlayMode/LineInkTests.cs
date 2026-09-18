using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T396 20회차 — 정본이 한 글의 **한 줄**에만 리터럴 색을 못박은 두 자리(4613 `.tn-skip small` #c62828 · 5637 `.asc-wipe-warn` #ff6b5e).
    /// 클론은 그 글을 한 TMP 로 찍으므로 조각을 떼지 않고 `LineInk` 가 그 줄만 `&lt;color&gt;` 로 감싼다 — 줄 수·줄높이 자는 그대로다.
    /// </summary>
    public class LineInkTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = Time.realtimeSinceStartup;
            while (!DungeonUiHost.Ready || UiRoot.Instance == null || !MetaHost.Ready)
            {
                Assert.Less(Time.realtimeSinceStartup - t, 20f, "DungeonUiHost/UiRoot/MetaHost 가 20초 안에 준비되지 않았다");
                yield return null;
            }
            yield return null;
        }

        static Transform Find(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++) { Transform r = Find(t.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        [Test]
        public void 줄_하나만_색_태그로_감싸고_꺾쇠가_든_글이나_없는_줄은_손대지_않는다()
        {
            Color red = new Color(0xc6 / 255f, 0x28 / 255f, 0x28 / 255f);
            Assert.AreEqual("건너뛰기\n<color=#C62828>◆ 12</color>", LineInk.Wrap("건너뛰기\n◆ 12", 1, red));
            Assert.AreEqual("<color=#C62828>가</color>\n나", LineInk.Wrap("가\n나", 0, red));
            Assert.IsNull(LineInk.Wrap("가\n나", 2, red), "없는 줄");
            Assert.IsNull(LineInk.Wrap("가\n", 1, red), "빈 줄");
            Assert.IsNull(LineInk.Wrap("가 <b>나</b>\n다", 1, red), "꺾쇠가 든 글은 안 건드린다(플레이어 글의 태그가 먹히는 길을 안 연다)");
            Assert.IsNull(LineInk.Wrap(null, 0, red));
            string w = LineInk.Wrap("· 가\n· 나\n· 다", 1, red);
            Assert.AreEqual("· 가\n· 나\n· 다", LineInk.Strip(w), "벗기면 원문");
            Assert.AreEqual(3, w.Split('\n').Length, "줄 수는 그대로");
        }

        /// <summary>정본 5637 `.asc-wipe-warn { color: #ff6b5e }` — 승천 효과 세 줄 글의 가운데 줄(소멸 경고)만. 그려진 줄 수(셋 · 둘째 항목 안 한 번 접힘이면 넷)는 `BrLinesTests` 가 잰다(결정 796·797).</summary>
        [UnityTest]
        public IEnumerator 승천_효과_글줄은_가운데_줄만_경고색이고_논리_줄_셋은_그대로다()
        {
            yield return Boot();
            AscendPopup.Open("forge");
            yield return null;
            Transform eff = Find(AscendPopup.Root, "eff");
            Assert.IsNotNull(eff, "효과 글줄(eff)");
            TextMeshProUGUI t = eff.GetComponent<TextMeshProUGUI>();
            Assert.IsTrue(LineInk.Has(t), "가운데 줄에 색 태그가 있다: " + t.text.Replace("\n", "⏎"));
            Assert.IsTrue(t.richText, "태그가 먹히려면 richText");
            string[] lines = t.text.Split('\n');
            Assert.AreEqual(3, lines.Length, "정본 5848·5849 세 줄 그대로");
            Color warn = PinnedColorUi.C("asc_wipe_warn_ink");
            Assert.AreEqual("#FF6B5E", "#" + ColorUtility.ToHtmlStringRGB(warn), "정본 5637 #ff6b5e");
            Assert.IsTrue(lines[1].StartsWith("<color=#FF6B5E>") && lines[1].EndsWith("</color>"), "가운데 줄(소멸 경고)만 감싼다: " + lines[1]);
            Assert.IsFalse(lines[0].Contains("<color") || lines[2].Contains("<color"), "첫·셋째 줄은 그대로");
            Assert.AreEqual(TextSizeUi.Px("asc_focus_eff"), t.fontSize, 0.5f, "크기는 표(정본 .76rem) 그대로 — 태그가 크기를 안 건드린다");
            AscendPopup.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator 기술_노드_건너뛰기_버튼은_아랫줄_젬_값만_빨강이고_줄높이_자는_그대로다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(MetaHost.Ready && UiRoot.Instance != null && UiRoot.Instance.TabBar != null); i++) yield return null;
            TechPanel p = TechPanel.OpenTechTree();
            yield return null;
            Assert.IsNotNull(p, "기술 트리");
            p.ShowBranch("power");
            yield return null;
            Assert.Greater(p.NodeIds.Count, 0, "노드");
            // 연구 중 상태를 만들어야 [건너뛰기] 가 선다 — 첫 노드 연구 시작
            string id = p.NodeIds[0];
            TechPopups.OpenNode(id);
            yield return null;
            Assert.IsTrue(TechPopups.IsNodeOpen, "노드 팝업");
            Button b = TechPopups.ActionButton;
            Assert.IsNotNull(b, "행동 버튼");
            Transform label = DungeonPopups.Root(b).Find("label");
            Assert.IsNotNull(label, "버튼 라벨");
            TextMeshProUGUI t = label.GetComponent<TextMeshProUGUI>();
            if (!t.text.StartsWith("건너뛰기"))
            {
                // 아직 연구 전이면 [연구 시작] 을 눌러 연구 중으로 — 그러면 팝업이 다시 서고 [건너뛰기 ◆ N] 이 선다
                b.onClick.Invoke();
                yield return null; yield return null;
                if (!TechPopups.IsNodeOpen) { TechPopups.OpenNode(id); yield return null; }
                b = TechPopups.ActionButton;
                Assert.IsNotNull(b, "연구 중 행동 버튼");
                label = DungeonPopups.Root(b).Find("label");
                t = label.GetComponent<TextMeshProUGUI>();
            }
            if (!t.text.StartsWith("건너뛰기")) { Debug.Log("[LineInkTests] 이 저장에선 연구 중 상태를 못 만들었다(" + LineInk.Strip(t.text).Replace("\n", "⏎") + ") — 환경 · 순수 셈 자가 감싸기를 지킨다"); TechPopups.Close(); yield break; }
            Assert.IsTrue(LineInk.Has(t), "[건너뛰기] 아랫줄에 색 태그: " + t.text.Replace("\n", "⏎"));
            string[] lines = t.text.Split('\n');
            Assert.AreEqual(2, lines.Length, "두 줄 그대로");
            Assert.AreEqual("#C62828", "#" + ColorUtility.ToHtmlStringRGB(PinnedColorUi.C("tn_skip_gem_ink")), "정본 4613 #c62828");
            Assert.IsTrue(lines[1].StartsWith("<color=#C62828>"), "아랫줄 «◆ N» 만: " + lines[1]);
            Assert.IsFalse(lines[0].Contains("<color"), "윗줄 «건너뛰기» 는 알약 잉크 그대로");
            TechPopups.Close();
        }
    }
}
