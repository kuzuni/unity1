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

        /// <summary>T53 — 화면에 실제로 서는 한글이 글꼴에 있는가(두부 □ 막이).
        /// 폰트 애셋이 글리프를 못 찾으면 TMP 는 조용히 네모를 그린다 — 그래서 문자 단위로 묻는다.</summary>
        [UnityTest]
        public IEnumerator 화면_한글이_글꼴에_있다_두부가_없다()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            TMP_FontAsset fa = UiFont.Primary;
            System.Collections.Generic.HashSet<char> missing = new System.Collections.Generic.HashSet<char>();
            foreach (TextMeshProUGUI t in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            {
                if (!t.isActiveAndEnabled || string.IsNullOrEmpty(t.text)) continue;
                foreach (char c in t.text)
                {
                    if (c < 0xAC00 || c > 0xD7A3) continue;          // 한글 음절만 본다
                    if (!fa.HasCharacter(c, true, true)) missing.Add(c);
                }
            }
            Assert.IsEmpty(missing, "글꼴에 없는 한글(화면에 □ 로 나온다): " + new string(System.Linq.Enumerable.ToArray(missing)));
        }

        /// <summary>카탈로그 글꼴 자체가 한글을 쥐고 있는가(OS 폴백에 기대지 않는다 — 리눅스 CI·WebGL 에는 없다).</summary>
        [Test]
        public void 카탈로그_글꼴이_한글을_직접_쥔다()
        {
            TMP_FontAsset fa = UiFont.Primary;
            foreach (char c in "대장간던전소환퀘스트상점펫스킬탈것승천장비제작판매설정프로필리그채팅")
                Assert.IsTrue(fa.HasCharacter(c, false, false), "카탈로그 글꼴에 '" + c + "' 가 없다 — OS 폴백이 없는 환경에서 두부가 된다");
        }

        private static string Path(Transform t)
        {
            string s = t.name;
            while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
            return s;
        }
    }
}
