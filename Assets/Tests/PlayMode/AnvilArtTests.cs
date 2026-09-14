using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Ui;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T98 1회차 — 정본 SVG 를 그대로 옮긴 모루 그림(<see cref="AnvilArt"/>)이 **표대로 서는가**.
    /// 그림을 «비슷하게» 가 아니라 «정본 겹 순서·정본 꼭짓점·정본 획» 으로 본다.
    /// 배선(`ForgeSheet.DrawAnvil` 을 이것으로 바꾸기)은 그 파일이 T108 lock 이라 2회차 몫이고,
    /// 그래서 이 자는 제 상자를 따로 세워 본다(남의 화면을 안 건드린다).
    /// </summary>
    public class AnvilArtTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(UiRoot.Instance != null && UiRoot.Instance.App != null) && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(UiRoot.Instance, "UiRoot 가 10초 안에 안 섰다");
        }

        [UnityTest]
        public IEnumerator T98_모루가_정본_SVG_겹_순서와_꼭짓점_그대로_선다()
        {
            yield return Boot();
            RectTransform host = UiKit.Box(UiRoot.Instance.App, "t98-host");
            try
            {
                const float unit = 4f;                       // viewBox 한 유닛 = 4px(자가 재기 좋은 크기 · 화면 값과 무관)
                RectTransform box = AnvilArt.Build(host, "anvil-art", unit);
                yield return null;
                Canvas.ForceUpdateCanvases();

                Assert.AreEqual(AnvilArt.ViewW * unit, box.rect.width, 0.01f, "상자 폭 = viewBox 폭 × 유닛");
                Assert.AreEqual(AnvilArt.ViewH * unit, box.rect.height, 0.01f, "상자 높이 = viewBox 높이 × 유닛");

                // ⓐ 겹 순서 — 정본 SVG 등장 순서 그대로여야 뒤/앞이 맞다(받침이 상판을 덮으면 안 된다).
                int pi = 0;
                for (int i = 0; i < box.childCount; i++)
                {
                    string n = box.GetChild(i).name;
                    if (n.EndsWith("-line", System.StringComparison.Ordinal)) continue;   // 키라인은 제 몸통 바로 앞에 깔린다
                    Assert.AreEqual(AnvilArt.PartName(pi), n, pi + "번째 겹이 정본 순서와 다르다");
                    pi++;
                }
                Assert.AreEqual(AnvilArt.PartCount, pi, "표의 겹이 다 서지 않았다");

                // ⓑ 정본이 획으로 묶은 여섯 — 키라인 면이 **몸통보다 크고 바로 뒤**에 있다.
                string[] stroked = { "anv-base", "anv-recess", "anv-neck", "anv-horn", "anv-front", "anv-top" };
                foreach (string n in stroked)
                {
                    RectTransform body = (RectTransform)box.Find(n);
                    RectTransform line = (RectTransform)box.Find(n + "-line");
                    Assert.IsNotNull(body, n + " 몸통");
                    Assert.IsNotNull(line, n + " 키라인(정본 stroke 3)");
                    Assert.AreEqual(body.GetSiblingIndex() - 1, line.GetSiblingIndex(), n + ": 키라인은 몸통 바로 뒤다");
                    Assert.Greater(line.rect.width, body.rect.width, n + ": 키라인 면이 몸통보다 넓어야 획으로 보인다");
                    Assert.Greater(line.rect.height, body.rect.height, n + ": 키라인 면이 몸통보다 높아야 한다");
                }

                // ⓒ 상판 윗면 자리 — 정본 `M23 4 L90 3 L95 25 L12 26 Z` 의 상자(x 12~95 · y 3~26).
                RectTransform top = (RectTransform)box.Find("anv-top");
                double[] b = SvgPath.Bounds(AnvilArt.PartPoints(5));
                Assert.AreEqual("anv-top", AnvilArt.PartName(5), "여섯째 겹이 상판 윗면이다");
                Assert.AreEqual((b[2] - b[0]) * unit, top.rect.width, 0.01f, "상판 폭이 정본 꼭짓점과 다르다");
                Assert.AreEqual((b[3] - b[1]) * unit, top.rect.height, 0.01f, "상판 높이가 정본 꼭짓점과 다르다");

                // ⓓ 뿔은 상판보다 **오른쪽으로 튀어나온다**(정본: 상판 오른쪽에서 20%W 돌출 · 끝이 둥글다).
                RectTransform horn = (RectTransform)box.Find("anv-horn");
                float topRight = top.anchoredPosition.x + top.rect.width;
                float hornRight = horn.anchoredPosition.x + horn.rect.width;
                Assert.Greater(hornRight, topRight, "뿔이 상판 오른쪽 밖으로 나가야 한다");
                Assert.AreEqual(0.20f * AnvilArt.ViewW * unit, hornRight - topRight, 1f * unit, "돌출량이 정본 실측(20%W = 26.4유닛)과 다르다 — 표 값은 26.0유닛이라 1유닛 안이다");

                // ⓔ 그림이 실제로 구워졌는가 — 겹마다 스프라이트가 있다(«칸만 있고 그림 0» 을 막는다).
                foreach (Image img in box.GetComponentsInChildren<Image>(true))
                    Assert.IsNotNull(img.sprite, img.name + " 이 그림 없이 칸만 있다");
            }
            finally { Object.Destroy(host.gameObject); }
            yield return null;
        }
    }
}
