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

        // ── T178 3회차 — 둥근 면 위의 겹 셋(상점 배너 · 리그 수집 알약 · 플레이어 정보 미리보기) ──────────────

        [UnityTest]
        public IEnumerator 세_자리_표가_정본_각도와_정지점을_그대로_쥔다()
        {
            yield return Boot();
            Assert.AreEqual(180f, SurfaceArt.Angle("shop_banner"), 1e-4f, "정본 2899 `.shop-banner` 180deg");
            Assert.AreEqual(180f, SurfaceArt.Angle("lgr_collect_pill"), 1e-4f, "정본 2522 `.league-collect-pill` 180deg");
            Assert.AreEqual(180f, SurfaceArt.Angle("pinfo_preview"), 1e-4f, "정본 3179 `.pinfo-preview` 180deg");

            Color[] col; float[] off;
            SurfaceArt.Stops("shop_banner", out col, out off);
            Assert.AreEqual(2, col.Length);
            Assert.AreEqual(255f / 255f, col[0].r, 1e-3f, "#ffb300 r"); Assert.AreEqual(179f / 255f, col[0].g, 1e-3f, "#ffb300 g");
            Assert.AreEqual(232f / 255f, col[1].r, 1e-3f, "#e89400 r"); Assert.AreEqual(148f / 255f, col[1].g, 1e-3f, "#e89400 g");
            SurfaceArt.Stops("lgr_collect_pill", out col, out off);
            Assert.AreEqual(227f / 255f, col[0].r, 1e-3f, "#e3e3e3"); Assert.AreEqual(194f / 255f, col[1].r, 1e-3f, "#c2c2c2");
            // 정지점 둘이 같은 55% — CSS 처럼 55% 앞은 첫 색 그대로 · 뒤는 끝 색 그대로(날카로운 경계)
            SurfaceArt.Stops("pinfo_preview", out col, out off);
            Assert.AreEqual(0.55f, off[0], 1e-4f); Assert.AreEqual(0.55f, off[1], 1e-4f);
            Assert.AreEqual(157f / 255f, SurfaceArt.Sample(col, off, 0.5f).r, 1e-3f, "55% 앞은 #9d8256");
            Assert.AreEqual(111f / 255f, SurfaceArt.Sample(col, off, 0.6f).r, 1e-3f, "55% 뒤는 #6f5334");

            // 180deg 는 위→아래: 구운 그림의 맨 윗줄이 시작 색, 맨 아랫줄이 끝 색(텍스처는 아래가 0행)
            Sprite sp = SurfaceArt.Bake("shop_banner", 4f);
            Texture2D t = sp.texture;
            Color topPx = t.GetPixel(t.width / 2, t.height - 1), botPx = t.GetPixel(t.width / 2, 0);
            Assert.AreEqual(179f / 255f, topPx.g, 0.02f, "맨 위는 #ffb300");
            Assert.AreEqual(148f / 255f, botPx.g, 0.02f, "맨 아래는 #e89400");
            Sprite pv = SurfaceArt.Bake("pinfo_preview", 3f);
            Texture2D pt = pv.texture;
            Assert.AreEqual(157f / 255f, pt.GetPixel(pt.width / 2, pt.height - 1).r, 0.02f, "미리보기 위 톤");
            Assert.AreEqual(111f / 255f, pt.GetPixel(pt.width / 2, 0).r, 0.02f, "미리보기 아래 톤");
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { Transform r = FindDeep(root.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        [UnityTest]
        public IEnumerator 상점_배너는_둥근_면_위에_마스크로_겹을_얹는다()
        {
            yield return Boot();
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            UiRoot.Instance.TabBar.OnTab("shop");
            yield return null;
            yield return null;
            Transform grad = FindDeep(UiRoot.Instance.App, "shop-banner-grad");
            Assert.IsNotNull(grad, "상점 배너의 겹(shop-banner-grad)이 섰다");
            UnityEngine.UI.Image img = grad.GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(img.sprite, "겹은 구운 그림이다(색 한 칸이 아니다)");
            Assert.IsFalse(img.raycastTarget, "겹은 클릭을 안 먹는다");
            Assert.AreEqual("face", grad.parent.name, "겹은 둥근 면(face)의 자식이다");
            UnityEngine.UI.Mask mask = grad.parent.GetComponent<UnityEngine.UI.Mask>();
            Assert.IsNotNull(mask, "면에 Mask 가 걸려 겹이 모서리 밖으로 안 샌다(정본 border-radius 가 background 를 자르는 결)");
            Assert.IsTrue(mask.showMaskGraphic, "면 그림은 그대로 보인다");
            Assert.AreEqual(Vector2.zero, img.rectTransform.offsetMin, "면을 꽉 채운다");
            Assert.AreEqual(Vector2.zero, img.rectTransform.offsetMax);
        }
    }
}
