using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T156 1회차 — 제작 비교 «장착됨» 리본이 **정본 깃발**인가(정본 `style.css` 1801~1809 `.cmp-ribbon`).
    /// 그림이 아니라 **꼭짓점·상자**로 본다. 배선(`ForgeUi.Ribbon` 을 이것으로 바꾸기)은 그 파일이 T122 lock 이라 2회차다 —
    /// 그래서 이 자는 제 상자를 따로 세운다(남의 화면을 안 건드린다).
    /// </summary>
    public class RibbonArtTests
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
        public IEnumerator T156_리본은_오른쪽이_파인_깃발이고_아랫변_테두리가_없다()
        {
            yield return Boot();

            // ⓐ 표 — 정본 clip-path 다섯 꼭짓점 · 노치는 폭의 88% · 세로 한가운데
            Vector2[] clip = RibbonArt.Clip();
            Assert.AreEqual(5, clip.Length, "정본 `polygon(0 0, 100% 0, 88% 50%, 100% 100%, 0 100%)` 은 꼭짓점 다섯이다");
            Assert.AreEqual(new Vector2(0f, 0f), clip[0]);
            Assert.AreEqual(new Vector2(1f, 0f), clip[1]);
            Assert.AreEqual(0.88f, clip[2].x, 1e-4f, "노치 꼭짓점 x — 이 값이 82% 면 «오목한 노치» 가 아니라 그냥 사다리꼴이다(정본 주석)");
            Assert.AreEqual(0.50f, clip[2].y, 1e-4f, "노치는 세로 한가운데로 파인다");
            Assert.AreEqual(new Vector2(1f, 1f), clip[3]);
            Assert.AreEqual(new Vector2(0f, 1f), clip[4]);
            // 파임은 **안쪽으로** 들어간다 — 오른쪽 두 꼭짓점보다 왼쪽에 있어야 «<» 다.
            Assert.Less(clip[2].x, clip[1].x, "노치가 오른쪽 변보다 안쪽이어야 «<» 로 파인다");

            // ⓑ 폭은 **앱 폭** 기준(정본 주석: 래퍼 대비 %로 주면 두 화면이 다른 px 이 된다)
            float appW = UiRoot.Instance.App.rect.width;
            Assert.AreEqual(appW * 0.203f, RibbonArt.Width(appW), 0.01f, "정본 `width: calc(var(--app-w) * .203)`");

            // ⓒ 패딩은 오른쪽이 «<» 파임 몫만큼 넓다(정본 `.15rem 1.5rem .15rem .5rem`)
            float rem = PopupKit.Rem, pl, pr, pt, pb;
            RibbonArt.Padding(rem, out pl, out pr, out pt, out pb);
            Assert.AreEqual(0.5f * rem, pl, 0.01f);
            Assert.AreEqual(1.5f * rem, pr, 0.01f);
            Assert.AreEqual(pt, pb, 0.01f, "위아래 패딩은 같다(.15rem)");
            Assert.Greater(pr, pl * 2f, "오른쪽이 왼쪽보다 훨씬 넓다 — 그 몫이 파임이다");

            // ⓓ 세운 모습 — 테두리 면이 위·왼·오른쪽으로는 몸통 밖으로 나가고 **아래로는 안 나간다**(정본 `border-bottom: none`)
            RectTransform host = UiKit.Box(UiRoot.Instance.App, "t156-host");
            try
            {
                float w = RibbonArt.Width(appW), h = rem * 1.6f;
                RectTransform box = RibbonArt.Build(host, "cmp-ribbon", w, h, rem);
                yield return null;
                Canvas.ForceUpdateCanvases();

                RectTransform face = (RectTransform)box.Find("face");
                RectTransform line = (RectTransform)box.Find("line");
                Assert.IsNotNull(face, "종이 면");
                Assert.IsNotNull(line, "테두리 면");
                Assert.IsNotNull(face.GetComponent<Image>().sprite, "면이 구워졌다");
                Assert.IsNotNull(line.GetComponent<Image>().sprite, "테두리가 구워졌다");
                Assert.AreEqual(line.GetSiblingIndex() + 1, face.GetSiblingIndex(), "테두리는 종이 **뒤**에 깔린다");

                float border = RibbonArt.Border(rem);
                Assert.Greater(border, 0f, "정본 `--ol2` 는 0 이 아니다");
                Vector3[] fc = new Vector3[4], lc = new Vector3[4];
                face.GetWorldCorners(fc); line.GetWorldCorners(lc);
                // 유니티 월드 모서리: 0 = 좌하 · 2 = 우상
                Assert.Less(lc[0].x, fc[0].x, "테두리가 왼쪽으로 나간다");
                Assert.Greater(lc[2].x, fc[2].x, "테두리가 오른쪽으로 나간다");
                Assert.Greater(lc[2].y, fc[2].y, "테두리가 위로 나간다");
                Assert.AreEqual(fc[0].y, lc[0].y, 0.51f, "**아래로는 안 나간다** — 정본 `border-bottom: none`(깃발이 카드 윗변에 이어 붙는다)");
            }
            finally { Object.Destroy(host.gameObject); }
            yield return null;
        }
    }
}
