using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T178 2회차 — 정본 «표면 겹»(`linear-gradient`)이 **각도·정지점 그대로** 구워지는가.
    ///
    /// 그림이 아니라 **구운 픽셀**로 본다. 각도는 CSS 의 뜻(0deg 위 · 90deg 오른쪽)이고 비율을 타므로,
    /// 정사각에 구워 늘리면 120° 가 다른 각이 된다 — 그래서 그 자리의 실제 비율로 굽는지도 같이 잰다.
    /// </summary>
    public class SurfaceArtTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(UiRoot.Instance != null && UiRoot.Instance.App != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(UiRoot.Instance, "UiRoot 가 20초 안에 안 섰다");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 표가_정본_각도와_정지점을_그대로_쥔다()
        {
            yield return Boot();

            Assert.AreEqual(120f, SurfaceArt.Angle("dg_banner"), 1e-4f, "정본 style.css 1952 `linear-gradient(**120deg**, …)`");
            Assert.AreEqual(90f, SurfaceArt.Angle("dg_banner_scrim"), 1e-4f, "정본 1982 `linear-gradient(**90deg**, …)`");

            Color[] col; float[] off;
            SurfaceArt.Stops("dg_banner", out col, out off);
            Assert.AreEqual(2, col.Length, "정본은 정지점 둘(var(--bg,#444c56) → #161b22)");
            Assert.AreEqual(68f / 255f, col[0].r, 1e-3f, "#444c56 의 r");
            Assert.AreEqual(22f / 255f, col[1].r, 1e-3f, "#161b22 의 r");
            Assert.Greater(col[0].grayscale, col[1].grayscale, "시작이 끝보다 밝다");

            SurfaceArt.Stops("dg_banner_scrim", out col, out off);
            Assert.AreEqual(4, col.Length, "정본 스크림은 정지점 넷(.22 0% · .14 22% · .05 34% · 0 42%)");
            Assert.AreEqual(0.22f, col[0].a, 1e-3f);
            Assert.AreEqual(0f, col[3].a, 1e-3f, "42% 에서 완전히 걷힌다");
            Assert.AreEqual(0.42f, off[3], 1e-3f, "정본 주석의 «도달 62%→42%» 가 이 값이다");
            // 42% 뒤는 마지막 색 그대로 — 오른쪽 절반은 아예 안 덮는다
            Assert.AreEqual(0f, SurfaceArt.Sample(col, off, 0.8f).a, 1e-4f, "오른쪽은 스크림이 없다(일러스트를 살린다)");
        }

        [UnityTest]
        public IEnumerator 구운_겹이_정본_방향으로_흐른다()
        {
            yield return Boot();

            // ⓐ 120deg — 방향 (sin120, −cos120) = 오른쪽·아래로. 즉 **왼쪽 위가 시작(밝다) · 오른쪽 아래가 끝(어둡다)**.
            Sprite banner = SurfaceArt.Bake("dg_banner", 3f);
            Texture2D tex = banner.texture;
            Assert.Greater(tex.width, tex.height, "그 자리의 실제 비율로 굽는다(각도는 비율을 탄다)");
            Color topLeft = tex.GetPixel(2, tex.height - 3);
            Color bottomRight = tex.GetPixel(tex.width - 3, 2);
            Assert.Greater(topLeft.grayscale, bottomRight.grayscale, "왼쪽 위가 오른쪽 아래보다 밝다 — 단색 한 장이면 둘이 같다");
            Color mid = tex.GetPixel(tex.width / 2, tex.height / 2);
            Assert.Less(mid.grayscale, topLeft.grayscale, "가운데는 그 사이다");
            Assert.Greater(mid.grayscale, bottomRight.grayscale);

            // ⓑ 90deg 스크림 — 왼쪽이 짙고 42% 지나면 투명
            Sprite scrim = SurfaceArt.Bake("dg_banner_scrim", 3f);
            Texture2D s = scrim.texture;
            Assert.AreEqual(0.22f, s.GetPixel(1, s.height / 2).a, 0.02f, "왼쪽 끝 알파 .22");
            Assert.AreEqual(0f, s.GetPixel(Mathf.RoundToInt(s.width * 0.6f), s.height / 2).a, 0.01f, "60% 자리는 완전히 걷혀 있다");
            Assert.Greater(s.GetPixel(1, s.height / 2).a, s.GetPixel(Mathf.RoundToInt(s.width * 0.3f), s.height / 2).a, "왼쪽으로 갈수록 짙다");
        }

        [UnityTest]
        public IEnumerator 겹은_상자를_꽉_채우고_클릭을_안_먹는다()
        {
            yield return Boot();
            RectTransform host = UiKit.Box(UiRoot.Instance.App, "t178-host");
            try
            {
                UiKit.Place(host, 0f, 0f, 300f, 100f);
                UnityEngine.UI.Image img = SurfaceArt.Fill(host, "bg-grad", "dg_banner", 300f, 100f);
                yield return null;
                Assert.IsNotNull(img.sprite, "구운 겹");
                Assert.IsFalse(img.raycastTarget, "겹은 클릭을 안 먹는다(정본 `pointer-events: none` 결)");
                RectTransform rt = img.rectTransform;
                Assert.AreEqual(Vector2.zero, rt.offsetMin, "상자를 꽉 채운다 — 겹은 layout 을 안 바꾼다");
                Assert.AreEqual(Vector2.zero, rt.offsetMax);
            }
            finally { Object.Destroy(host.gameObject); }
            yield return null;
        }
    }
}
