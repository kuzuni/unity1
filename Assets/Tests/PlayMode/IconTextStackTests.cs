using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Forging;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T110 1회차 — 두 줄 버튼 라벨의 세로 갈래 <see cref="IconTextStack"/>: 줄바꿈에서 갈라 줄마다 아이콘 + 글자.
    /// 그림이 아니라 **계층**으로 본다(T89 `ToastIconTests` 와 같은 자). 호출부(`ForgeCraftPopup`·`ForgeInfoPopup`·`ForgeSheet`)는
    /// T87 lock 뒤 2회차가 잇는다 — 그때 `check_text_glyphs` 의 `LABEL_KNOWN` 넷을 뺀다.
    /// </summary>
    public class IconTextStackTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(UiRoot.Instance != null && UiRoot.Instance.App != null && UiText.Loaded) && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(UiRoot.Instance, "UiRoot 가 10초 안에 안 섰다");
            Assert.IsTrue(UiText.Loaded, "StreamingAssets/data/ui-text.json 을 못 읽었다(T89)");
        }

        private static IEnumerator BootForge()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            yield return null;
        }

        private static RectTransform FindIn(Transform root, string name)
        {
            foreach (RectTransform rt in root.GetComponentsInChildren<RectTransform>(true)) if (rt.name == name) return rt;
            return null;
        }

        private static string TextOf(Transform line)
        {
            var sb = new System.Text.StringBuilder();
            foreach (TextMeshProUGUI t in line.GetComponentsInChildren<TextMeshProUGUI>(true)) sb.Append(t.text);
            return sb.ToString();
        }

        /// <summary>버튼 하나: Btn 의 한 줄 라벨은 꺼져 있고 세로 갈래 두 줄 · 윗줄 글자 <paramref name="top"/> · 아랫줄 아이콘 하나 + 글자에 이모지 0.</summary>
        private static void AssertTwoLineIconBtn(Transform btn, string top, string what)
        {
            Assert.IsNotNull(btn, what + ": 버튼이 없다");
            Transform plain = btn.Find("label");
            Assert.IsTrue(plain == null || !plain.gameObject.activeSelf, what + ": Btn 의 한 줄 라벨이 켜져 있다");
            RectTransform stack = (RectTransform)btn.Find("label-stack");
            Assert.IsNotNull(stack, what + ": 세로 갈래(label-stack)가 없다");
            Assert.AreEqual(2, IconTextStack.LineCount(stack), what + ": 정본 <br>/<small> 하나 = 줄 둘");
            Transform l1 = stack.Find("line-1"), l2 = stack.Find("line-2");
            Assert.AreEqual(top, TextOf(l1), what + ": 윗줄 글자");
            Assert.AreEqual(0, l1.GetComponentsInChildren<Image>(true).Length, what + ": 윗줄엔 아이콘이 없다");
            Image[] icons = l2.GetComponentsInChildren<Image>(true);
            Assert.AreEqual(1, icons.Length, what + ": 아랫줄에 아이콘 하나(coin/gem)");
            Assert.IsNotNull(icons[0].sprite, what + ": 아이콘 스프라이트가 비었다(T31 아틀라스)");
            Assert.IsFalse(string.IsNullOrEmpty(TextOf(l2)), what + ": 아랫줄 수가 비었다");
            foreach (TextMeshProUGUI t in btn.GetComponentsInChildren<TextMeshProUGUI>(true))
                Assert.IsFalse(t.text != null && (t.text.Contains("\U0001FA99") || t.text.Contains("\U0001F48E")), what + ": 글자 조각에 이모지가 남았다 «" + t.text + "»");
            LayoutElement fit = l2.GetComponent<LayoutElement>();
            Assert.IsNotNull(fit, what + ": 줄 크기를 직접 재어 박는다(Fit)");
            Assert.Greater(fit.preferredWidth, 0f, what + ": 아랫줄 폭");
            foreach (TMP_Text t in btn.GetComponentsInChildren<TMP_Text>(false))
                Assert.IsNotNull(t.GetComponent<UiTextKindTag>(), t.name + " 은 UiKit.Text 를 거치지 않았다(T18)");
        }

        /// <summary>T110 2회차 — 호출부 넷: 비교 팝업 «판매 / coin +N» · 확률 정보 «레벨 N 업그레이드 / coin N · ⏱ T» · 업그레이드 중 «건너뛰기 / gem N».</summary>
        [UnityTest]
        public IEnumerator 대장간_팝업_두_줄_버튼_넷은_아랫줄에_아이콘이_서고_이모지_글자가_없다()
        {
            yield return BootForge();
            ForgeHost h = ForgeHost.Instance;

            // ⓐ 제작 비교 팝업의 판매 버튼(정본 3266)
            h.S.Hammers = 10; h.Pull();
            h.OnCraft();
            float t = 0f;
            while (!h.Meta.Popups.IsOpen(ForgeCraftPopup.Name) && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "제작 뒤 비교 팝업");
            Canvas.ForceUpdateCanvases();
            AssertTwoLineIconBtn(FindIn(h.Meta.Popups.Find(ForgeCraftPopup.Name).Root, "sell"), "판매", "비교 팝업 판매");
            h.ResolveCraft("equip");
            yield return null;
            if (h.Meta.Popups.IsOpen(ForgeCraftPopup.Name)) { h.ResolveCraft("sell"); yield return null; if (h.Meta.Popups.IsOpen(ForgeCraftPopup.SellName)) { h.OnSellConfirm(); yield return null; } }

            // ⓑ 확률 정보 팝업의 업그레이드 버튼(정본 2047)
            ForgeInfoPopup.Open(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = h.Meta.Popups.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(p, "확률 정보 팝업");
            AssertTwoLineIconBtn(FindIn(p.Root, "fi-upgrade"), "레벨 " + (h.Forge.ForgeLevel + 1) + " 업그레이드", "업그레이드");
            ForgeInfoPopup.Close(h);
            yield return null;

            // ⓒ 업그레이드 진행 중이면 건너뛰기 버튼(정본 2043)
            h.Forge.UpgradeEndsAt = SaveIo.NowMs() + 60 * 60e3;
            ForgeInfoPopup.Open(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            p = h.Meta.Popups.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(p);
            AssertTwoLineIconBtn(FindIn(p.Root, "fi-skip"), "건너뛰기", "건너뛰기");
            ForgeInfoPopup.Close(h);
            h.Forge.UpgradeEndsAt = null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator 두_줄_문구는_줄마다_아이콘과_글자로_세로로_선다()
        {
            yield return Boot();
            RectTransform host = UiKit.Box(UiRoot.Instance.App, "t110-host");
            try
            {
                // 정본 판매 버튼 꼴: «판매<br>🪙 +12» — 윗줄 글자만 · 아랫줄 코인 아이콘 + 수
                RectTransform st = IconTextStack.Build(host, "sell", TextKind.Button, "판매\n\U0001FA99 +12", "white");
                yield return null;
                Canvas.ForceUpdateCanvases();

                Assert.AreEqual(2, IconTextStack.LineCount(st), "줄바꿈 하나 = 줄 둘");
                Assert.IsNotNull(st.GetComponent<VerticalLayoutGroup>(), "세로 레이아웃");
                Transform l1 = st.Find("line-1"), l2 = st.Find("line-2");
                Assert.IsNotNull(l1); Assert.IsNotNull(l2);
                Assert.AreEqual(0, l1.GetComponentsInChildren<Image>(true).Length, "윗줄에는 아이콘이 없다");
                Assert.AreEqual("판매", l1.GetComponentInChildren<TextMeshProUGUI>(true).text);

                Image[] icons = l2.GetComponentsInChildren<Image>(true);
                Assert.AreEqual(1, icons.Length, "아랫줄에 코인 아이콘 하나");
                Assert.IsNotNull(icons[0].sprite, "아이콘 스프라이트가 비었다(T31 아틀라스 coin)");
                Assert.AreEqual("ico-1", icons[0].name);
                foreach (TextMeshProUGUI piece in st.GetComponentsInChildren<TextMeshProUGUI>(true))
                    Assert.IsFalse(piece.text.Contains("\U0001FA99"), "글자 조각에 이모지가 남았다: «" + piece.text + "»");
                Assert.AreEqual("+12", l2.GetComponentInChildren<TextMeshProUGUI>(true).text, "이모지 뒤 공백은 아이콘 마진이 대신한다(정본 ⓓ)");

                // 아이콘 한 칸 = 글자 크기 정사각(가로 줄과 같은 규칙)
                float size = UiCatalog.Instance.Kind(TextKind.Button).size;
                LayoutElement le = icons[0].GetComponent<LayoutElement>();
                Assert.IsNotNull(le);
                Assert.AreEqual(size, le.preferredWidth, 0.01f); Assert.AreEqual(size, le.preferredHeight, 0.01f);

                // 줄이 하나면 가로 줄 하나 — 호출부 계약이 안 바뀐다
                RectTransform one = IconTextStack.Build(host, "one", TextKind.Sub, "건너뛰기", "white");
                yield return null;
                Assert.AreEqual(1, IconTextStack.LineCount(one));
                Assert.AreEqual("건너뛰기", one.GetComponentInChildren<TextMeshProUGUI>(true).text);
                Assert.AreEqual(0, one.GetComponentsInChildren<Image>(true).Length);
            }
            finally { Object.Destroy(host.gameObject); }
            yield return null;
        }
    }
}
