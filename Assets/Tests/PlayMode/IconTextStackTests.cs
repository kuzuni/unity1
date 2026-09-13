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
