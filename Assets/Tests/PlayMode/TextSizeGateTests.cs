using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>T18 — 모든 활성 글자가 UiKit 을 거쳤고(종류 표식) 그 종류의 하한 이상인가(ROUTINE §1: 본문 40 · 버튼 44 · 보조 36 · 제목 60).</summary>
    public class TextSizeGateTests
    {
        [UnityTest]
        public IEnumerator 모든_활성_글자가_종류_하한_이상이다()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            UiCatalog cat = UiCatalog.Instance;

            TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
            int seen = 0;
            foreach (TMP_Text t in texts)
            {
                if (!t.gameObject.activeInHierarchy) continue;
                seen++;
                UiTextKindTag tag = t.GetComponent<UiTextKindTag>();
                Assert.IsNotNull(tag, Path(t.transform) + " 은 UiKit.Text 를 거치지 않았다(종류 표식 없음)");
                float min = cat.Kind(tag.Kind).min;
                Assert.GreaterOrEqual(t.fontSize, min, Path(t.transform) + " 글자 " + t.fontSize + " < 종류 " + tag.Kind + " 하한 " + min);
                Assert.AreSame(UiFont.Primary, t.font, Path(t.transform) + " 은 주인 글꼴(NotoSans)이 아니다");
            }
            Assert.Greater(seen, 0, "활성 글자가 하나도 없다 — HUD 가 안 섰다");
        }

        [Test]
        public void 카탈로그의_종류_하한은_ROUTINE_규칙_그대로다()
        {
            UiCatalog cat = UiCatalog.Instance;
            Assert.GreaterOrEqual(cat.Kind(TextKind.Body).min, 40f);
            Assert.GreaterOrEqual(cat.Kind(TextKind.Button).min, 44f);
            Assert.GreaterOrEqual(cat.Kind(TextKind.Sub).min, 36f);
            Assert.GreaterOrEqual(cat.Kind(TextKind.Title).min, 60f);
            foreach (TextKind k in System.Enum.GetValues(typeof(TextKind)))
                Assert.GreaterOrEqual(cat.Kind(k).size, cat.Kind(k).min, k + " 의 크기가 제 하한보다 작다");
        }

        private static string Path(Transform t)
        {
            string s = t.name;
            while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
            return s;
        }
    }
}
