using System.Collections;
using NUnit.Framework;
using UnityEngine;
using Forge.Core.Ui;
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
        public IEnumerator 탭바와_확률_막대_표가_정본_각도_정지점_px_림을_그대로_쥔다()
        {
            yield return Boot();
            // T178 4회차 — 정본 8317 #tabbar(0deg 아래 1px 림 + 180deg 밴드) · 8577 .rate-bar(180deg 위 1px 림 + 에나멜 · 28% 하드 스톱)
            Assert.AreEqual(0f, SurfaceArt.Angle("tabbar_rim"), 1e-4f, "정본 8319 `linear-gradient(**0deg**, …)` — 아래에서 위로");
            Assert.AreEqual(180f, SurfaceArt.Angle("tabbar_shade"), 1e-4f);
            Assert.AreEqual(180f, SurfaceArt.Angle("rate_bar_rim"), 1e-4f);
            Assert.AreEqual(180f, SurfaceArt.Angle("rate_bar_enamel"), 1e-4f);
            Assert.IsTrue(SurfaceArt.PxOffsets("tabbar_rim") && SurfaceArt.PxOffsets("rate_bar_rim"), "림 둘은 정지점이 CSS px(`0 1px`)");
            Assert.IsFalse(SurfaceArt.PxOffsets("tabbar_shade") || SurfaceArt.PxOffsets("rate_bar_enamel"), "나머지는 %");

            Color[] col; float[] off;
            SurfaceArt.Stops("tabbar_shade", out col, out off);
            Assert.AreEqual(4, col.Length, "정본 8320~8321 정지점 넷");
            Assert.AreEqual(0.16f, col[0].a, 1e-3f); Assert.AreEqual(1f, col[0].r, 1e-3f, "위는 흰 .16");
            Assert.AreEqual(0.30f, off[1], 1e-3f); Assert.AreEqual(0.03f, col[1].a, 1e-3f);
            Assert.AreEqual(0.64f, off[2], 1e-3f); Assert.AreEqual(0f, col[2].r, 1e-3f, "64% 부터 검정");
            Assert.AreEqual(0.38f, col[3].a, 1e-3f, "아래는 검 .38");

            SurfaceArt.Stops("rate_bar_enamel", out col, out off);
            Assert.AreEqual(6, col.Length, "정본 8580~8583 정지점 여섯");
            Assert.AreEqual(0.10f, SurfaceArt.Sample(col, off, 0.27f).a, 1e-3f, "27% 에서 .10");
            Assert.AreEqual(0f, SurfaceArt.Sample(col, off, 0.30f).a, 1e-4f, "28% 하드 스톱 뒤는 0 — 정본 주석 «경계는 끊김이 만든다»");
            Assert.AreEqual(0f, SurfaceArt.Sample(col, off, 0.66f).a, 1e-4f, "66% 까지 평지");
            Color bottom = SurfaceArt.Sample(col, off, 1f);
            Assert.AreEqual(0.30f, bottom.a, 1e-3f); Assert.AreEqual(0f, bottom.r, 1e-3f, "아래는 검 .30");

            // px 림 — 선 길이 100 캔버스 px 자리에 구우면 1 CSS px(=css_px 캔버스 px)만 밝다: 0deg 는 아래가 시작이라 맨 아래 줄만 흰 줄이다.
            // T178 8회차부터 이 겹은 바탕(밴드)을 알아 **미리 합성해 불투명**으로 구워진다 — 그래서 두께를 알파가 아니라 «아래 겹보다 밝은 줄» 로 잰다.
            Sprite rim = SurfaceArt.Bake("tabbar_rim", 4f, 100f);
            Sprite band = SurfaceArt.Bake("tabbar_shade", 4f, 100f);
            Texture2D tx = rim.texture;
            Color32[] px = tx.GetPixels32(), bp = band.texture.GetPixels32();
            int W = tx.width, H = tx.height;
            Assert.AreEqual(W, band.texture.width); Assert.AreEqual(H, band.texture.height);
            Assert.AreEqual(255, px[W / 2].a, "미리 합성한 겹은 불투명하다(정본은 sRGB 에서 섞는다 · 결정 586)");
            Assert.Greater(px[W / 2].r - bp[W / 2].r, 10, "맨 아래 줄(0deg 의 시작)에 흰 림 .12 가 얹혀 밴드보다 밝다");
            Assert.AreEqual(bp[(H / 2) * W + W / 2].r, px[(H / 2) * W + W / 2].r, 1, "가운데 줄엔 림이 없다 — 아래 겹 값 그대로");
            Assert.AreEqual(bp[(H - 1) * W + W / 2].r, px[(H - 1) * W + W / 2].r, 1, "맨 위 줄도 아래 겹 값 그대로");
            float k = SurfaceArt.CssPx / 100f;
            int litRows = 0; for (int y = 0; y < H; y++) if (px[y * W + W / 2].r - bp[y * W + W / 2].r > 3) litRows++;
            Assert.LessOrEqual(litRows, Mathf.CeilToInt(k * H) + 1, "림 두께 = 1 CSS px 를 선 길이로 나눈 몫(판 96줄 중 " + (k * H).ToString("0.0") + "줄)");
            Assert.GreaterOrEqual(litRows, 1);
        }

        [UnityTest]
        public IEnumerator 탭바_밴드와_확률_막대에_겹이_선다()
        {
            yield return Boot();
            // ⓐ 탭바 — 밴드(tabbar)의 자식 · bg 다음 · 테(line)·버튼 앞 · 꽉 채움 · 클릭 안 먹음
            Transform band = UiRoot.Instance.TabBand;
            Assert.IsNotNull(band);
            Transform grad = band.Find("tabbar-grad"), rim = band.Find("tabbar-rim"), bg = band.Find("bg"), line = band.Find("line");
            Assert.IsNotNull(grad, "탭바 밴드 겹(tabbar-grad)"); Assert.IsNotNull(rim, "탭바 아래 림(tabbar-rim)");
            Assert.Less(bg.GetSiblingIndex(), grad.GetSiblingIndex(), "바탕 위");
            Assert.Less(grad.GetSiblingIndex(), rim.GetSiblingIndex(), "밴드 → 림 순");
            Assert.Less(rim.GetSiblingIndex(), line.GetSiblingIndex(), "테(border-top) 아래");
            UnityEngine.UI.Image gi = grad.GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(gi.sprite, "구운 그림"); Assert.IsFalse(gi.raycastTarget, "클릭 안 먹음");
            Assert.AreEqual(Vector2.zero, gi.rectTransform.offsetMin); Assert.AreEqual(Vector2.zero, gi.rectTransform.offsetMax);
            // 구운 밴드: 위 줄이 아래 줄보다 밝다(흰 .16 ↔ 검 .38) · 바탕을 아는 겹이라 **미리 합성해 불투명**(T178 8회차)
            Color32[] px = gi.sprite.texture.GetPixels32(); int W = gi.sprite.texture.width, H = gi.sprite.texture.height;
            Color32 top = px[(H - 1) * W + W / 2], bot = px[W / 2];
            Assert.Greater(top.r, bot.r, "위는 흰 기 · 아래는 검");
            Assert.AreEqual(255, top.a, "바탕(tabbar_bg)을 알아 미리 합성했다 — 알파로 남기면 유니티가 선형에서 섞어 정본보다 밝다");
            Assert.AreEqual(255, bot.a, "아래 줄도 불투명");

            // ⓑ 확률 막대 — 소환 시트의 [확률] 팝업을 열어 rate-bar-<등급> 의 둥근 면(face) 위에 Mask 로 겹 둘
            float t = 0f;
            while (!(SkillPetSheet.Instance != null && PetSkillHost.Ready) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(PetSkillHost.Ready, "소환 호스트가 20초 안에 안 섰다");
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            SkillPetSheet.Instance.Switch(SkillPetSheet.SubSkills);
            yield return null;
            SkillPetSheet.Instance.Skills.RatesButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(SkillPetSheet.Instance.Modal.IsOpen(SkillRatesPopup.ModalName), "확률 팝업");
            Transform enamel = FindDeep(UiRoot.Instance.App, "rate-enamel");
            Assert.IsNotNull(enamel, "확률 막대 에나멜 겹(rate-enamel)");
            Assert.AreEqual("face", enamel.parent.name, "둥근 면(face)의 자식");
            UnityEngine.UI.Mask mask = enamel.parent.GetComponent<UnityEngine.UI.Mask>();
            Assert.IsNotNull(mask, "면에 Mask"); Assert.IsTrue(mask.showMaskGraphic, "등급색 면은 그대로 보인다");
            Transform rrim = enamel.parent.Find("rate-rim");
            Assert.IsNotNull(rrim, "위 1px 림(rate-rim)");
            Assert.Less(enamel.GetSiblingIndex(), rrim.GetSiblingIndex(), "에나멜 → 림 순");
            UnityEngine.UI.Image ei = enamel.GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(ei.sprite); Assert.IsFalse(ei.raycastTarget);
            // 림은 위 줄만: 180deg 는 위가 시작 → 맨 위 줄 α > 0 · 가운데 0
            UnityEngine.UI.Image ri = rrim.GetComponent<UnityEngine.UI.Image>();
            Color32[] rp = ri.sprite.texture.GetPixels32(); int rw = ri.sprite.texture.width, rh = ri.sprite.texture.height;
            Assert.Greater(rp[(rh - 1) * rw + rw / 2].a, 100, "맨 위 줄 림 .62");
            Assert.AreEqual(0, rp[(rh / 2) * rw + rw / 2].a, "가운데는 투명");
            Debug.Log("[T178] 탭바 겹 " + W + "×" + H + " · 확률 막대 림 " + rw + "×" + rh);
            SkillPetSheet.Instance.Modal.CloseAll();
            yield return null;
        }

        /// <summary>
        /// T178 8회차 — 정본은 알파 겹을 **sRGB 바이트 위에서** 섞는다(브라우저 규칙). 이 프로젝트는 Linear 색공간이라
        /// 알파를 그대로 남기면 유니티가 **선형 값 위에서** 섞어 어두운 바탕에서 훨씬 밝아진다(T357 이 런 537 탭바에서 실측: 98/71/53/19/9 ↔ 정본 42/28/25/17/10).
        /// 그래서 바탕을 아는 겹은 표(`over_color`·`over_layer`)를 따라 **미리 합성해 불투명하게** 굽는다 — 이 자는 그 값이 정본 셈과 같은지를 바이트로 잰다.
        /// </summary>
        [UnityTest]
        public IEnumerator 바탕을_아는_겹은_정본처럼_sRGB_바이트_위에서_미리_섞인다()
        {
            yield return Boot();
            // 탭바 바탕 #0e111b = (14, 17, 27) · 겹 정지점 (255,255,255,.16) 0% → (0,0,0,.38) 100%
            Sprite shade = SurfaceArt.Bake("tabbar_shade", 1080f / 176f, 176f);
            Color32[] sp = shade.texture.GetPixels32();
            int W = shade.texture.width, H = shade.texture.height;
            Color32 top = sp[(H - 1) * W + W / 2], bot = sp[W / 2];
            // 정본 셈: dst*(1−a) + src*a 를 **바이트 위에서** — 위 14*.84+255*.16 = 53 · 아래 14*.62 = 9
            Assert.AreEqual(53, top.r, 3, "위 줄 r = 14*(1-.16) + 255*.16");
            Assert.AreEqual(55, top.g, 3, "위 줄 g = 17*(1-.16) + 255*.16");
            Assert.AreEqual(63, top.b, 3, "위 줄 b = 27*(1-.16) + 255*.16");
            Assert.AreEqual(9, bot.r, 3, "아래 줄 r = 14*(1-.38)");
            Assert.AreEqual(17, bot.b, 3, "아래 줄 b = 27*(1-.38)");
            Assert.AreEqual(255, top.a); Assert.AreEqual(255, bot.a);
            // 선형에서 섞었다면 위 줄 r 이 90 을 넘는다 — 그 값이 아니어야 한다(회귀의 눈)
            Assert.Less(top.r, 80, "선형 합성(≈98)으로 돌아가면 이 단언이 먼저 깨진다");

            // 림은 겹 사슬(over_layer: tabbar_shade) — 투명한 자리는 아래 겹 값 그대로 · 맨 아래 1px 줄만 밝다
            Sprite rim = SurfaceArt.Bake("tabbar_rim", 1080f / 176f, 176f);
            Color32[] rp = rim.texture.GetPixels32();
            Assert.AreEqual(W, rim.texture.width); Assert.AreEqual(H, rim.texture.height);
            Color32 rMid = rp[(H / 2) * W + W / 2], sMid = sp[(H / 2) * W + W / 2];
            Assert.AreEqual(sMid.r, rMid.r, 1, "림이 없는 자리는 아래 겹(밴드) 값 그대로여야 한다 — 사슬이 끊기면 바탕색이 밴드를 덮는다");
            Assert.AreEqual(sMid.b, rMid.b, 1);
            Assert.AreEqual(255, rMid.a, "림도 불투명하게 구워 위에 덮는다");
            Assert.Greater(rp[W / 2].r, sp[W / 2].r, "맨 아래 1px 줄은 흰 .12 가 얹혀 밴드보다 밝다");
            Debug.Log("[T178] 미리 합성 탭바 위(" + top.r + "," + top.g + "," + top.b + ") 아래(" + bot.r + "," + bot.g + "," + bot.b + ") 림 아래줄 r " + rp[W / 2].r);
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

        /// <summary>T178 5회차 — 정본 `#tabbar button.active, #tabbar button.tab-x`(style.css 8325~8328)의 방사형 둘:
        /// 켜진 칸에만 깔리고, 구운 그림이 **가운데가 밝고 가장자리가 투명한** 진짜 방사형인가(단색 판이 아니다).</summary>
        [UnityTest]
        public IEnumerator 켜진_탭에만_노란_방사형_둘이_깔린다()
        {
            yield return Boot();
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            TabBar tb = UiRoot.Instance.TabBar;
            RectTransform summon = tb.ButtonOf("summon").GetComponent<RectTransform>();
            Transform glow = FindDeep(summon, "tab-glow"), foot = FindDeep(summon, "tab-footglow");
            Assert.IsNotNull(glow, "켜진 칸 겹 tab-glow 가 있다");
            Assert.IsNotNull(foot, "켜진 칸 겹 tab-footglow 가 있다");
            Assert.IsFalse(glow.gameObject.activeSelf, "홈에서는 꺼져 있다");

            tb.OnTab("summon");
            yield return null;
            Assert.IsTrue(glow.gameObject.activeSelf, "켜진 칸(또는 ✕ 칸)에 깔린다 — 정본은 .active 와 .tab-x 둘 다에 준다");
            Assert.IsTrue(foot.gameObject.activeSelf);
            Transform other = FindDeep(tb.ButtonOf("shop").GetComponent<RectTransform>(), "tab-glow");
            Assert.IsFalse(other.gameObject.activeSelf, "다른 칸은 그대로 꺼져 있다");

            // 구운 그림이 방사형인가 — 가운데(중심 50%/42%)가 가장자리보다 진하다.
            UnityEngine.UI.Image img = glow.GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(img.sprite, "겹은 구운 그림이다");
            Texture2D tex = img.sprite.texture;
            int w = tex.width, h = tex.height;
            float mid = tex.GetPixel(w / 2, Mathf.RoundToInt(h * 0.58f)).a;     // CSS y 42% = 텍스처 아래에서 58%
            float corner = tex.GetPixel(1, 1).a;
            Assert.Greater(mid, corner + 0.05f, "가운데가 모서리보다 진하다(방사형) — 단색 판이면 같다");
            Assert.Less(corner, 0.02f, "74% 밖은 투명하다(정본 마지막 정지점 0)");

            tb.CloseOpened();
            yield return null;
            Assert.IsFalse(glow.gameObject.activeSelf, "닫으면 다시 꺼진다");
        }

        /// <summary>T178 6회차 — 퀘스트 진행 막대 채움은 **두 겹**이다(정본 2044·2047: 세로 색 띠 + 위 1 CSS px 흰 광택).
        /// 상태(수령 전 파랑 / 수령 대기 초록)는 **띠 키로만** 갈리고 광택은 같다 — 정본 주석이 못 박은 자리다.</summary>
        [UnityTest]
        public IEnumerator 퀘스트_막대_채움은_띠와_광택_두_겹이다()
        {
            yield return Boot();
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            UiRoot.Instance.TabBar.OnTab("quest");
            yield return null;
            yield return null;
            Transform grad = FindDeep(UiRoot.Instance.App, "qst-fill-grad");
            Assert.IsNotNull(grad, "채움 띠(qst-fill-grad)가 섰다");
            Transform rim = FindDeep(UiRoot.Instance.App, "qst-fill-rim");
            Assert.IsNotNull(rim, "채움 광택(qst-fill-rim)이 섰다");
            Assert.AreEqual("fill", grad.parent.name, "겹은 채움(fill)의 자식이다");
            UnityEngine.UI.Image gi = grad.GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(gi.sprite, "띠는 구운 그림이다(색 한 칸이 아니다)");
            Assert.IsFalse(gi.raycastTarget);
            UnityEngine.UI.Mask mask = grad.parent.GetComponent<UnityEngine.UI.Mask>();
            Assert.IsNotNull(mask, "채움에 Mask — 둥근 끝 밖으로 안 샌다");

            // 정본 띠는 위가 밝고 아래가 어둡다(#8fd9ff → #0288d1 · done 이면 #aef2b0 → #2e9e31).
            Texture2D tex = gi.sprite.texture;
            Color top = tex.GetPixel(tex.width / 2, tex.height - 2), bottom = tex.GetPixel(tex.width / 2, 1);
            float lt = top.r + top.g + top.b, lb = bottom.r + bottom.g + bottom.b;
            Assert.Greater(lt, lb + 0.3f, "위가 아래보다 밝다(정본 180deg 밝은 색 → 어두운 색)");

            // T178 10회차 — 림의 바탕은 **상태로 갈린다**(파랑 ↔ 초록)라 표의 `over_layer` 로는 못 적는다.
            //   부르는 쪽(QuestSheet)이 그때의 바탕 겹을 주면 굽는 쪽이 정본이 섞는 길(sRGB 바이트)로 미리 합성한다 — 그러면
            //   구운 판이 **불투명**해지고(섞을 자리가 없다) 값은 «띠 색 위에 흰 .5» 가 된다. 알파로 남아 있으면 유니티가
            //   선형에서 섞어 정본보다 밝아진다(T357 · 탭바 98 ↔ 42 가 그 자리였다).
            UnityEngine.UI.Image ri = rim.GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(ri.sprite, "광택도 구운 그림이다");
            Texture2D rtex = ri.sprite.texture;
            Color rimTop = rtex.GetPixel(rtex.width / 2, rtex.height - 2);
            Assert.AreEqual(1f, rimTop.a, 1e-3f, "바탕을 받은 겹은 불투명하게 구워진다(섞는 공간이 끼어들 자리가 없다)");
            Color bandTop = tex.GetPixel(tex.width / 2, tex.height - 2);
            float rimL = rimTop.r + rimTop.g + rimTop.b, bandL = bandTop.r + bandTop.g + bandTop.b;
            Assert.Greater(rimL, bandL, "흰 .5 를 얹었으니 띠보다 밝다");
            Assert.Less(rimL, 3f - 1e-3f, "그래도 순백은 아니다 — 알파 .5 를 바이트 위에서 섞은 값이다");
        }

        /// <summary>
        /// T178 10회차 — 바탕이 **런타임 색**인 자리(등급색 위의 확률 막대처럼)는 겹 이름 대신 그 색을 준다.
        /// 구운 판이 불투명해지고, 값은 «그 색 위에 정지점 색을 sRGB 바이트로 얹은 것» 과 같아야 한다(Core `SurfaceBlendRules`).
        /// 이 길이 없으면 그 자리는 알파로 남아 유니티가 선형에서 섞어 정본보다 밝아진다(T357).
        /// </summary>
        [Test]
        public void 바탕이_런타임_색인_겹은_그_색_위에서_sRGB_로_미리_섞인다()
        {
            Color baseCol = new Color32(14, 17, 27, 255);        // 어두운 바탕 — 두 길의 차가 가장 크게 벌어지는 자리
            Sprite baked = SurfaceArt.Bake("tabbar_shade", 4f, 96f, baseCol);
            Assert.IsNotNull(baked, "색 바탕으로 구운 판이 없다");
            Texture2D tex = baked.texture;
            for (int i = 0; i < 4; i++)
            {
                Color c = tex.GetPixel(tex.width / 2, Mathf.RoundToInt((tex.height - 1) * i / 3f));
                Assert.AreEqual(1f, c.a, 1e-3f, "바탕을 받은 겹은 불투명하게 구워진다");
            }
            // 맨 윗줄(t=0) = 흰 .16 을 (14,17,27) 위에 **바이트로** 얹은 값 ≈ 53 · 선형으로 섞었다면 98 쯤이다.
            Color top = tex.GetPixel(tex.width / 2, tex.height - 1);
            int r8 = Mathf.RoundToInt(top.r * 255f);
            int want = SurfaceBlendRules.OverSrgb(14, 255, 0.16);
            Assert.AreEqual(want, r8, 3, "정본이 섞는 길(sRGB 바이트)과 같아야 한다 — 선형이면 훨씬 밝다");
            Assert.Less(r8, 80, "선형 합성(≈98)으로 돌아가면 여기서 먼저 걸린다");
        }

        /// <summary>T178 9회차 — 정본 2107~2109 `.tech-branch-icon::before`: 카테고리색 원판 위에 겹 둘(왼쪽 위 방사형 광택 · 위→아래 명암)이 더 깔린다.
        /// 색 한 칸이면 «납작한 원» 이고, 정본 주석은 그 원판을 «빈 서류가 아니라 노드 버튼으로 읽히게» 하려고 둔 것이라 적었다.</summary>
        [UnityTest]
        public IEnumerator 기술_분기_원판에_광택과_명암_두_겹이_깔린다()
        {
            yield return Boot();
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            UiRoot.Instance.TabBar.OnTab("summon");
            yield return null;
            SkillPetSheet sheet = SkillPetSheet.Instance;
            Assert.IsNotNull(sheet, "소환 시트");
            sheet.Switch(SkillPetSheet.SubTech);
            yield return null;
            yield return null;

            Transform gloss = FindDeep(UiRoot.Instance.App, "tb-icon-gloss");
            Transform shade = FindDeep(UiRoot.Instance.App, "tb-icon-shade");
            Assert.IsNotNull(gloss, "원판 광택(tb-icon-gloss)");
            Assert.IsNotNull(shade, "원판 명암(tb-icon-shade)");
            // 원판은 두 겹 구조다 — 바깥 `icon-bg`(테두리 색 원) 안에 `face`(바탕색 원)가 테두리 두께만큼 들어앉는다(DungeonPopups.BorderedCircle).
            // 정본도 겹을 `background` 로 얹고 `border` 는 그 밖이라, 겹은 **면(face)** 에 깔려야 테두리를 안 덮는다.
            Assert.AreEqual("face", shade.parent.name, "겹 둘은 원판 면(face)의 자식이다 — 테두리(icon-bg)를 안 덮는다");
            Assert.AreEqual("icon-bg", shade.parent.parent.name, "그 면의 부모가 분기 원판이다");
            Assert.AreSame(shade.parent, gloss.parent);
            Assert.IsNotNull(shade.parent.GetComponent<UnityEngine.UI.Mask>(), "면에 Mask 가 걸려 겹이 원 밖으로 안 샌다");
            Assert.Less(shade.GetSiblingIndex(), gloss.GetSiblingIndex(), "정본 순서 — 명암 위에 광택");

            UnityEngine.UI.Image gi = gloss.GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(gi.sprite, "광택은 구운 그림이다");
            Texture2D tex = gi.sprite.texture;
            int w = tex.width, h = tex.height;
            float hot = tex.GetPixel(Mathf.RoundToInt(w * 0.32f), Mathf.RoundToInt(h * 0.78f)).a;   // CSS y 22% = 텍스처 아래에서 78%
            float far = tex.GetPixel(Mathf.RoundToInt(w * 0.9f), Mathf.RoundToInt(h * 0.1f)).a;
            Assert.Greater(hot, far + 0.1f, "정본 중심(32%/22%)이 반대쪽보다 진하다 — 방사형이다");
        }
    }
}
