using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T89 — 화면 문구의 이모지가 **아이콘으로 선다**. 정본 `UI.paintIconText` 가 토스트에서 하는 일이고(표 이름이 `TOAST_ICON`),
    /// 이 레포에서는 <see cref="UiText"/>(표) + <see cref="UiKit.IconTextRow"/>(줄 세우기) + <see cref="PopupLayer.Toast"/> 가 맡는다.
    /// 그림이 아니라 **계층**으로 본다 — 촬영이 없는 런에서도 도는 단언이다.
    /// </summary>
    public class ToastIconTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator 부팅하면_이모지_아이콘_표가_읽힌다()
        {
            yield return Boot();
            float t = 0f;
            while (!UiText.Loaded && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(UiText.Loaded, "StreamingAssets/data/ui-text.json 을 못 읽었다(T89)");
            Assert.GreaterOrEqual(UiText.Count, 30, "TOAST_ICON 줄 수");
            Assert.AreEqual("coin", UiText.Icon("\U0001FA99"), "🪙 → coin");
            Assert.AreEqual("hammer", UiText.Icon("\U0001F528"), "🔨 → hammer");
        }

        [UnityTest]
        public IEnumerator 토스트의_이모지는_아이콘_칸으로_서고_글자에는_안_남는다()
        {
            yield return Boot();
            float t = 0f;
            while (!UiText.Loaded && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(UiText.Loaded, "표가 있어야 이 단언이 뜻이 있다");

            PopupLayer.Instance.Toast("\U0001F528 29");         // 정본 문구 꼴: 아이콘 + 수
            yield return null;
            Canvas.ForceUpdateCanvases();

            Transform row = Find(UiRoot.Instance.App, "msg-row");
            Assert.IsNotNull(row, "토스트 문구 줄(msg-row)이 없다");
            Image ico = row.GetComponentInChildren<Image>(true);
            Assert.IsNotNull(ico, "이모지 자리에 아이콘 칸이 안 섰다");
            Assert.IsNotNull(ico.sprite, "아이콘 스프라이트가 비었다(T31 아틀라스)");

            foreach (TextMeshProUGUI piece in row.GetComponentsInChildren<TextMeshProUGUI>(true))
                Assert.IsFalse(piece.text.Contains("\U0001F528"), "글자 조각에 이모지가 남았다: «" + piece.text + "»");

            // 표에 없는 문구는 옛 모양 그대로 — 라벨 한 장(호출부 계약이 안 바뀐다).
            PopupLayer.Instance.Toast("스테이지 2-10 도달 시 해금됩니다");
            yield return null;
            Assert.AreEqual("스테이지 2-10 도달 시 해금됩니다", PopupLayer.Instance.LastToast);
        }

        [UnityTest]
        public IEnumerator 라벨의_전투력_별_체크_연필이_글자가_아니라_아이콘이다()
        {
            yield return Boot();
            float t = 0f;
            while (!UiText.Loaded && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }

            // 정본이 아이콘으로 그리는 네 자리(ui.js 4741 star · 4905 check · 5033 pencil · TOAST_ICON ⚔ tm_sword)에
            // 클론이 글자(★ ✓ ✎ ⚔)를 찍어 전부 □ 였다 — 이제 이 글자들이 **어느 활성 라벨에도 없어야** 한다.
            RectTransform row = UiKit.IconTextRow(UiRoot.Instance.App, "t89-probe", TextKind.Sub, "⚔ 12", "ink");
            yield return null;
            Assert.IsNotNull(row.GetComponentInChildren<Image>(true), "⚔ 가 아이콘 칸으로 안 섰다");
            foreach (TextMeshProUGUI piece in UiKit.RowTexts(row))
                Assert.IsFalse(piece.text.Contains("⚔"), "글자 조각에 ⚔ 가 남았다");
            Object.Destroy(row.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 던전_토스트도_이모지를_아이콘으로_세운다()
        {
            yield return Boot();
            float t = 0f;
            while (!UiText.Loaded && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(UiText.Loaded, "표가 있어야 이 단언이 뜻이 있다");

            // T107 — 토스트 그릇이 셋인데(공용 `PopupLayer` · 던전 `DungeonToast` · 펫/스킬 `PetSkillModal`)
            // 둘은 아이콘 길을 안 거쳐 ⭐·🔒·🧪·💎 가 화면에서 □ 였다. 자(`check_text_glyphs`)는 «둘레에
            // Toast( 가 보이면 아이콘 길» 로 쳐서 그 문구를 통째로 건너뛰었다 — 초록인데 화면은 두부.
            DungeonPopups.Toast("\U0001F512 스테이지 2-10 도달 시 해금됩니다");
            yield return null;
            Canvas.ForceUpdateCanvases();

            DungeonToast box = Object.FindObjectOfType<DungeonToast>(true);
            Assert.IsNotNull(box, "던전 토스트 상자가 안 섰다");
            Transform row = box.transform.Find("text");
            Assert.IsNotNull(row, "던전 토스트 문구 줄(text)이 없다");
            Image ico = row.GetComponentInChildren<Image>(true);
            Assert.IsNotNull(ico, "🔒 자리에 아이콘 칸이 안 섰다");
            Assert.IsNotNull(ico.sprite, "아이콘 스프라이트가 비었다(T31 아틀라스)");
            foreach (TextMeshProUGUI piece in row.GetComponentsInChildren<TextMeshProUGUI>(true))
                Assert.IsFalse(piece.text.Contains("\U0001F512"), "글자 조각에 🔒 가 남았다: «" + piece.text + "»");

            // 문구 원문은 그대로 쥔다(호출부·테스트 계약).
            Assert.AreEqual("\U0001F512 스테이지 2-10 도달 시 해금됩니다", DungeonToast.Last);
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform tr in root.GetComponentsInChildren<Transform>(true)) if (tr.name == name) return tr;
            return null;
        }
    }
}
