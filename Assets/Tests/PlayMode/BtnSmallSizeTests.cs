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
    /// <summary>T461 — 정본 667 `.btn small { font-size: .7rem }`: 두 줄 버튼(`IconTextStack.ReplaceLabel`)의 둘째 줄은 §1 예외 칸 `Micro` 로 찍히고
    /// 크기는 첫 줄 × (표 `btn_small` .7 / catalog `btn_font_rem` .88)이다(등재 판정 ⓓ). 첫 줄은 준 종류 그대로 · 둘째 줄도 `Micro` 하한(18) 이상.</summary>
    public class BtnSmallSizeTests
    {
        static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = Time.realtimeSinceStartup;
            while (UiRoot.Instance == null || UiRoot.Instance.App == null)
            {
                Assert.Less(Time.realtimeSinceStartup - t, 20f, "UiRoot 가 20초 안에 준비되지 않았다");
                yield return null;
            }
            yield return null;
        }

        [Test]
        public void 표의_잔글씨_크기는_정본_667_그대로다()
        {
            Assert.AreEqual(0.7f, TextSizeUi.Rem("btn_small"), 1e-6f, "정본 667 .btn small .7rem");
            Assert.AreEqual(0.88f, UiKit.L("btn_font_rem"), 1e-6f, "정본 665 .btn .88rem");
        }

        [UnityTest]
        public IEnumerator 두_줄_버튼의_둘째_줄은_Micro_이고_첫_줄의_7_대_88_이다()
        {
            yield return Boot();
            RectTransform box = UiKit.Box(UiRoot.Instance.App, "t461-box");
            UiKit.Place(box, 0f, 0f, 600f, 200f);
            Button b = PopupKit.Btn(box, "sell", "판매", "pp_blue", "pp_blue_dk", () => { }, 400f, 120f);
            RectTransform stack = IconTextStack.ReplaceLabel(b, TextKind.Sub, "판매\n\U0001FA99 +5", "stage_ink", "pp_blue");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.AreEqual(2, IconTextStack.LineCount(stack), "정본 <small> 하나 = 줄 둘");
            UiCatalog cat = UiCatalog.Instance;
            float first = cat.Kind(TextKind.Sub).size;
            float want = Mathf.Max(cat.Kind(TextKind.Micro).min, first * TextSizeUi.Rem("btn_small") / UiKit.L("btn_font_rem"));
            int n1 = 0, n2 = 0;
            for (int i = 0; i < stack.childCount; i++)
            {
                RectTransform line = (RectTransform)stack.GetChild(i);
                if (!line.name.StartsWith("line-")) continue;
                foreach (TextMeshProUGUI t in UiKit.RowTexts(line))
                {
                    UiTextKindTag tag = t.GetComponent<UiTextKindTag>();
                    Assert.IsNotNull(tag, t.name + " 종류 표식");
                    if (line.name == "line-1")
                    {
                        n1++;
                        Assert.AreEqual(TextKind.Sub, tag.Kind, "첫 줄은 준 종류 그대로");
                        Assert.AreEqual(first, t.fontSize, 0.01f, "첫 줄 크기 = 종류 크기");
                    }
                    else
                    {
                        n2++;
                        Assert.AreEqual(TextKind.Micro, tag.Kind, "둘째 줄은 §1 예외 칸 Micro(정본 <small>)");
                        Assert.AreEqual(want, t.fontSize, 0.01f, "둘째 줄 크기 = 첫 줄 × (.7 / .88)");
                        Assert.GreaterOrEqual(t.fontSize, cat.Kind(TextKind.Micro).min, "Micro 하한");
                        Assert.Less(t.fontSize, first, "잔글씨는 첫 줄보다 작다");
                    }
                }
            }
            Assert.Greater(n1, 0, "첫 줄 글자"); Assert.Greater(n2, 0, "둘째 줄 글자");
            Object.Destroy(box.gameObject);
            yield return null;
        }
    }
}
