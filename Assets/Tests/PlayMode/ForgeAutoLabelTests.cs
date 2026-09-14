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
    /// T108 — 장비 시트의 자동 제련 버튼 라벨: 정본 `ui.js` 1551 `자동${IconGen.img('autoloop')}<br>${unlocked ? ON|OFF : IconGen.img('lock')}` 대로
    /// 윗줄 «자동» + 고리 **아이콘**, 아랫줄 ON/OFF 또는 잠금 **아이콘** — 글자 ↻(U+21BB)·🔒 는 어디에도 없다(주인 글꼴에 없어 □ 였다).
    /// `ForgeUiTests.cs` 가 아니라 제 파일인 것은 그 파일이 T94·T114 lock 이기 때문이다.
    /// </summary>
    public class ForgeAutoLabelTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && PopupLayer.Instance != null && UiText.Loaded) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            Assert.IsTrue(UiText.Loaded, "ui-text.json(T89) 을 못 읽었다");
            yield return null;
        }

        private static Transform SheetChild(string name)
        {
            foreach (Transform t in UiRoot.Instance.Sheet.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        private static void AssertNoGlyphLeft(Transform btn)
        {
            foreach (TextMeshProUGUI t in btn.GetComponentsInChildren<TextMeshProUGUI>(false))
            {
                Assert.IsFalse(t.text.Contains("↻"), "글자 ↻ 가 남았다: «" + t.text + "»");
                Assert.IsFalse(t.text.Contains("\U0001F512"), "글자 🔒 가 남았다: «" + t.text + "»");
            }
        }

        private static Image[] Icons(Transform line) { return line.GetComponentsInChildren<Image>(false); }

        private static string TextOf(Transform line)
        {
            string s = "";
            foreach (TextMeshProUGUI t in line.GetComponentsInChildren<TextMeshProUGUI>(false)) s += t.text;
            return s;
        }

        [UnityTest]
        public IEnumerator 자동_제련_버튼은_고리_아이콘과_잠금_아이콘으로_서고_글자_두부가_없다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            float fs = PopupKit.FontSize(TextKind.Sub);

            // ⓐ 새 세이브 = 2-10 전 → 잠금: 윗줄 «자동» + autoloop · 아랫줄 lock 아이콘만
            Assert.IsFalse(h.AutoForgeUnlocked, "새 세이브는 자동 제련이 잠겨 있다");
            Transform btn = SheetChild("auto-btn");
            Assert.IsNotNull(btn, "자동 버튼");
            AssertNoGlyphLeft(btn);
            RectTransform stack = (RectTransform)btn.Find("label-stack");
            Assert.IsNotNull(stack, "세로 갈래 라벨(IconTextStack)");
            Assert.AreEqual(2, IconTextStack.LineCount(stack), "정본 <br> 하나 = 줄 둘");
            Transform l1 = stack.Find("line-1"), l2 = stack.Find("line-2");
            Assert.AreEqual("자동", TextOf(l1), "윗줄 글자");
            Image[] top = Icons(l1);
            Assert.AreEqual(1, top.Length, "윗줄 고리 아이콘 하나");
            Assert.IsNotNull(top[0].sprite, "autoloop 스프라이트(T31 아틀라스)");
            Assert.AreEqual(fs * UiKit.L("auto_loop_ico_em"), top[0].rectTransform.sizeDelta.x, 0.01f, "정본 .auto-loop-ico 1.15em");
            LayoutElement cell = top[0].transform.parent.GetComponent<LayoutElement>();
            Assert.IsNotNull(cell, "아이콘 칸의 LayoutElement");
            Assert.AreEqual(fs * (UiKit.L("auto_loop_ico_em") + UiKit.L("auto_loop_ico_ml_em") + UiKit.L("ico_mr_em")), cell.preferredWidth, 0.01f, "칸 폭 = 왼 마진 + 한 변 + 오른 마진");
            Assert.AreEqual(fs, cell.preferredHeight, 0.01f, "칸 높이 = 줄 높이(음수 세로 마진 = 줄을 안 키운다)");
            Assert.AreEqual("", TextOf(l2), "잠금 줄엔 글자가 없다");
            Image[] bottom = Icons(l2);
            Assert.AreEqual(1, bottom.Length, "아랫줄 잠금 아이콘 하나");
            Assert.IsNotNull(bottom[0].sprite, "lock 스프라이트(T31 아틀라스)");
            Assert.AreEqual(fs * UiKit.L("ico_em"), bottom[0].rectTransform.sizeDelta.x, 0.01f, "정본 .ico 1.45em");
            LayoutElement fit = l1.GetComponent<LayoutElement>();
            Assert.IsNotNull(fit, "줄 크기를 직접 재어 박는다(Fit)");
            Assert.Greater(fit.preferredWidth, fs * UiKit.L("auto_loop_ico_em"), "윗줄 폭 = 글자 + 아이콘 칸");
            Transform plain = btn.Find("label");
            Assert.IsTrue(plain == null || !plain.gameObject.activeSelf, "Btn 의 빈 한 줄 라벨은 꺼져 있다");
            foreach (TMP_Text t in btn.GetComponentsInChildren<TMP_Text>(false))
                Assert.IsNotNull(t.GetComponent<UiTextKindTag>(), t.name + " 은 UiKit.Text 를 거치지 않았다(T18)");

            // ⓑ 2-10 도달 → 해금: 아랫줄 OFF 글자 · 잠금 아이콘 0 · 고리 아이콘은 그대로
            h.S.BestChapter = 3; h.S.BestStage = 1;
            Assert.IsTrue(h.AutoForgeUnlocked);
            ForgeSheet.Render(h);
            yield return null;
            btn = SheetChild("auto-btn");
            Assert.IsNotNull(btn, "다시 그린 자동 버튼");
            AssertNoGlyphLeft(btn);
            stack = (RectTransform)btn.Find("label-stack");
            Assert.AreEqual(2, IconTextStack.LineCount(stack));
            l1 = stack.Find("line-1"); l2 = stack.Find("line-2");
            Assert.AreEqual("자동", TextOf(l1));
            Assert.AreEqual(1, Icons(l1).Length, "해금 뒤에도 윗줄 고리 아이콘");
            Assert.AreEqual("OFF", TextOf(l2), "해금 · 꺼짐 = OFF");
            Assert.AreEqual(0, Icons(l2).Length, "해금되면 잠금 아이콘이 없다");
            yield return null;
        }
    }
}
