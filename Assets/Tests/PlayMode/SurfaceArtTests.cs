using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
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

        /// <summary>
        /// T178 19회차 — 던전 상세 hero 의 바탕 겹(정본 2060 = 목록 배너와 같은 `dg_banner` · 일러스트 **뒤**)과 기술 노드 팝업 청동 원의 겹(정본 3689 160deg #d9a066→#a5642f · 원 면 마스크 안).
        /// 이름 «bg-grad» 는 목록 배너도 쓰므로 앱 뿌리가 아니라 **그 팝업 뿌리**의 길로 찾는다(T414).
        /// </summary>
        [UnityTest]
        public IEnumerator 던전_상세_hero_바탕과_기술_노드_청동_원에_정본_겹이_선다()
        {
            yield return Boot();
            float t0 = Time.realtimeSinceStartup;
            while (!DungeonUiHost.Ready && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            Assert.IsTrue(DungeonUiHost.Ready, "DungeonUiHost");
            // 표 — 청동 원
            Assert.AreEqual(160f, SurfaceArt.Angle("tn_bronze"), 1e-4f, "정본 3689 160deg");
            Color[] col; float[] off;
            SurfaceArt.Stops("tn_bronze", out col, out off);
            Assert.AreEqual(2, col.Length);
            Assert.AreEqual(new Color32(0xd9, 0xa0, 0x66, 255), (Color32)col[0], "#d9a066"); Assert.AreEqual(new Color32(0xa5, 0x64, 0x2f, 255), (Color32)col[1], "#a5642f");
            Sprite bz = SurfaceArt.Bake("tn_bronze", 1f);
            Texture2D bt = bz.texture;
            // 160deg 는 «위 살짝 왼쪽 → 아래 살짝 오른쪽» — 맨 위 행이 밝은 청동, 맨 아래 행이 어두운 청동
            Assert.Greater(bt.GetPixel(bt.width / 2, bt.height - 1).r, bt.GetPixel(bt.width / 2, 0).r + 0.1f, "위가 밝고 아래가 어둡다");

            // 던전 상세 — hero 의 첫 자식이 바탕 겹(일러스트 뒤) · 크기 = hero
            DungeonUiHost h = DungeonUiHost.Instance;
            h.S.BestChapter = 5; h.S.BestStage = 1;
            UiRoot.Instance.TabBar.OnTab("dungeon");
            yield return null;
            DungeonDetailPopup.Open("hammer");
            yield return null;
            Transform ov = FindDeep(UiRoot.Instance.App, "modal-dungeon-detail");
            Assert.IsNotNull(ov, "던전 상세 팝업 뿌리");
            Transform hero = ov.Find("card/hero");
            Assert.IsNotNull(hero, "hero");
            Transform bg = hero.Find("bg-grad");
            Assert.IsNotNull(bg, "hero 바탕 겹(bg-grad)");
            Assert.AreEqual(0, bg.GetSiblingIndex(), "바탕 겹은 일러스트 뒤(첫 자식)");
            Assert.Less(bg.GetSiblingIndex(), hero.Find("scene").GetSiblingIndex(), "일러스트가 위");
            Image bgi = bg.GetComponent<Image>();
            Assert.IsNotNull(bgi.sprite, "구운 겹"); Assert.IsFalse(bgi.raycastTarget);
            RectTransform hr = (RectTransform)hero, br = (RectTransform)bg;
            Assert.AreEqual(hr.rect.width, br.rect.width, 1f, "겹 폭 = hero"); Assert.AreEqual(hr.rect.height, br.rect.height, 1f, "겹 높이 = hero");
            Assert.AreEqual(120f, SurfaceArt.Angle("dg_banner"), 1e-4f, "정본 2060 = 1952 120deg");
            DungeonDetailPopup.Close();
            yield return null;

            // 기술 노드 팝업 — 청동 원 면에 마스크 + 겹
            TechPanel p = TechPanel.OpenTechTree();
            yield return null;
            p.ShowBranch("power");
            yield return null;
            TechPopups.OpenNode(p.NodeIds[0]);
            yield return null;
            Transform tov = FindDeep(UiRoot.Instance.App, "modal-tech-node");
            Assert.IsNotNull(tov, "기술 노드 팝업 뿌리");
            Transform face = tov.Find("card/icon/circle/face");
            Assert.IsNotNull(face, "청동 원 면");
            Assert.IsNotNull(face.GetComponent<Mask>(), "원 면이 마스크한다(정본 border-radius 50% 가 background 를 자른다)");
            Transform g = face.Find("bg-grad");
            Assert.IsNotNull(g, "청동 겹(bg-grad)");
            Image gi = g.GetComponent<Image>();
            Assert.IsNotNull(gi.sprite, "구운 겹"); Assert.IsFalse(gi.raycastTarget);
            Assert.AreEqual("tn_bronze", gi.sprite.name.Split('-')[1], "표 tn_bronze 겹(스프라이트 이름 sf-tn_bronze-…)");
            TechPopups.Close();
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

        /// <summary>T178 39회차 — 퀘스트 진행 막대 채움은 **색 한 칸**이다. 6회차가 정본 2044·2047(세로 띠 + 위 1px 광택 두 겹)을 세웠는데,
        /// 정본 **8776~8820 «aaa-skin ⓖ 게이지 축»**(2026-08-19 · 사용자 확정 화풍 ㉯ 플랫/매트)이 뒤에서 `.qst-bar i { background: #4fc3f7 }` ·
        /// `.qst-row.done .qst-bar i { background: #81e884 }` 로 **단색으로 끈다**(«종전 3정지의 중간색(38%)을 단색으로 · 신호는 그대로, 광택만 사라진다»).
        /// 트랙 홈(7992)도 같은 블록이 `none` 으로 끈다. cascade 는 마지막 선언이 이긴다 — 자(check_surface_gradients)가 이제 그것을 센다.</summary>
        [UnityTest]
        public IEnumerator 퀘스트_막대_채움은_정본_최종_규칙대로_색_한_칸이다()
        {
            yield return Boot();
            // ⓐ catalog 의 두 채움 색이 정본 8806/8807 의 단색 그 값이다
            Color32 qb = UiKit.C("quest_bar"), qd = UiKit.C("quest_bar_done");
            Assert.IsTrue(qb.r == 0x4f && qb.g == 0xc3 && qb.b == 0xf7, "quest_bar = #4fc3f7(8806): " + qb);
            Assert.IsTrue(qd.r == 0x81 && qd.g == 0xe8 && qd.b == 0x84, "quest_bar_done = #81e884(8807): " + qd);
            // ⓑ 걷은 표 키(qst_bar_ramp·qst_bar_done_ramp·qst_bar_rim·gauge_track·gauge_fill)는 표에 없다
            foreach (string gone in new[] { "qst_bar_ramp", "qst_bar_done_ramp", "qst_bar_rim", "gauge_track", "gauge_fill" })
            {
                bool has = true;
                try { SurfaceArt.Angle(gone); } catch (KeyNotFoundException) { has = false; }
                Assert.IsFalse(has, "정본이 끈 겹의 표 키가 남아 있다: " + gone);
            }
            // ⓒ 실물
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            UiRoot.Instance.TabBar.OnTab("quest");
            yield return null;
            yield return null;
            Assert.IsNull(FindDeep(UiRoot.Instance.App, "qst-fill-grad"), "채움 띠(qst-fill-grad)는 걷었다 — 정본 8806 이 끈다");
            Assert.IsNull(FindDeep(UiRoot.Instance.App, "qst-fill-rim"), "채움 광택(qst-fill-rim)도 걷었다");
            int bars = 0;
            foreach (Transform bar in UiRoot.Instance.App.GetComponentsInChildren<Transform>(true))
            {
                if (bar.name != "bar" || bar.Find("face/bg") == null || bar.Find("face/fill") == null) continue;
                bars++;
                Transform bg = bar.Find("face/bg"), fill = bar.Find("face/fill");
                Assert.IsNull(bg.Find("bg-grad"), "트랙 홈(7992)은 8798 이 none 으로 끈다 — bg 아래 겹 없음");
                Assert.AreEqual(0, fill.childCount, "채움은 자식 겹이 없는 색 한 칸");
                Color fc = fill.GetComponent<Image>().color;
                Assert.IsTrue(fc == UiKit.C("quest_bar") || fc == UiKit.C("quest_bar_done"), "채움 색은 quest_bar / quest_bar_done 둘 중 하나: " + fc);
            }
            Assert.Greater(bars, 0, "퀘스트 막대가 하나는 섰다");
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
        /// <summary>T178 11회차 — 정본 390 `.bw-banner { background: linear-gradient(180deg, #1e0202, #5a0707 45%, #240303) }`.
        /// 클론은 `pp_red_dk` **단색 한 장**이라 띠가 납작했다 — 정본은 가운데가 가장 밝아 부풀어 오른 것처럼 보인다.</summary>
        [UnityTest]
        public IEnumerator 보스_경고_배너는_가운데가_가장_밝은_세_정지점_겹을_진다()
        {
            yield return Boot();
            Assert.AreEqual(180f, SurfaceArt.Angle("bw_banner"), 1e-4f, "정본 390 180deg");
            Color[] col; float[] off;
            SurfaceArt.Stops("bw_banner", out col, out off);
            Assert.AreEqual(3, col.Length, "정지점 셋");
            Assert.AreEqual(0.45f, off[1], 1e-4f, "가운데 정지점은 45%");
            Assert.AreEqual(30f / 255f, col[0].r, 1e-3f, "#1e0202");
            Assert.AreEqual(90f / 255f, col[1].r, 1e-3f, "#5a0707");
            Assert.AreEqual(36f / 255f, col[2].r, 1e-3f, "#240303");
            Assert.Greater(col[1].r, col[0].r + 0.1f, "가운데가 위 끝보다 밝다");
            Assert.Greater(col[1].r, col[2].r + 0.1f, "가운데가 아래 끝보다 밝다");
            // 180deg 는 위→아래 — 구운 그림의 맨 윗줄이 첫 색이고, 45% 자리가 가장 밝다(텍스처는 아래가 0행).
            Sprite sp = SurfaceArt.Bake("bw_banner", 8f, 100f);
            Texture2D t = sp.texture;
            int x = t.width / 2;
            Color top = t.GetPixel(x, t.height - 1), mid = t.GetPixel(x, Mathf.RoundToInt(t.height * 0.55f)), bot = t.GetPixel(x, 0);
            Assert.AreEqual(30f / 255f, top.r, 0.02f, "맨 위는 #1e0202");
            Assert.AreEqual(36f / 255f, bot.r, 0.02f, "맨 아래는 #240303");
            Assert.Greater(mid.r, top.r + 0.1f, "45% 줄이 위 끝보다 밝다");
            Assert.Greater(mid.r, bot.r + 0.1f, "45% 줄이 아래 끝보다 밝다");
            Assert.AreEqual(1f, top.a, 1e-3f, "면이라 불투명하다");
        }

        /// <summary>T178 12회차 — 정본 147~150 `#game-area::after { background: radial-gradient(ellipse 120% 95% at 50% 42%, transparent 62%, rgba(8,10,16,.28) 100%) }`.
        /// 정본 주석 «비네트 포스트 — 화면 가장자리를 살짝 눌러 시선을 중앙 전투 라인으로 모음». 클론엔 이 **상시** 겹이 통째로 없었다.</summary>
        [UnityTest]
        public IEnumerator 무대_띠_위에_상시_비네트가_가운데는_비우고_가장자리만_누른다()
        {
            yield return Boot();
            Assert.IsTrue(SurfaceArt.IsRadial("stage_vignette"), "정본 147 은 radial-gradient 다");
            float cx, cy, rx, ry;
            SurfaceArt.Ellipse("stage_vignette", out cx, out cy, out rx, out ry);
            Assert.AreEqual(0.5f, cx, 1e-4f, "at 50%"); Assert.AreEqual(0.42f, cy, 1e-4f, "at 42%");
            Assert.AreEqual(1.20f, rx, 1e-4f, "ellipse 120%"); Assert.AreEqual(0.95f, ry, 1e-4f, "95%");
            Color[] col; float[] off;
            SurfaceArt.Stops("stage_vignette", out col, out off);
            Assert.AreEqual(0.62f, off[0], 1e-4f, "62% 까지는 비어 있다");
            Assert.AreEqual(0f, col[0].a, 1e-4f, "`transparent` — 프리멀티플라이드라 같은 색의 알파 0 이다");
            Assert.AreEqual(0.28f, col[1].a, 1e-3f, "가장자리 알파 .28");
            Assert.AreEqual(8f / 255f, col[1].r, 1e-3f, "rgba(8,10,16,…) r");
            Assert.AreEqual(16f / 255f, col[1].b, 1e-3f, "rgba(8,10,16,…) b");

            BattleOverlay ov = BattleOverlay.Ensure();
            Assert.IsNotNull(ov, "오버레이");
            RectTransform band = ov.StageVignetteBand;
            Assert.IsNotNull(band, "상시 비네트 띠가 선다");
            // 정본 z-index 1 — 3D 위 · `#fx-layer`·`#boss-warning` 아래. 그래서 연출 띠 **바로 앞 형제**다.
            Assert.AreEqual(ov.Layer.parent, band.parent, "연출 띠와 같은 부모");
            Assert.Less(band.GetSiblingIndex(), ov.Layer.GetSiblingIndex(), "연출 띠보다 아래 — 암전·씬컷·보스 워닝이 위로 온다");
            Transform grad = band.Find("vignette-grad");
            Assert.IsNotNull(grad, "구운 겹이 깔린다");
            UnityEngine.UI.Image gi = grad.GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(gi.sprite, "구운 그림이다");
            Texture2D t = gi.sprite.texture;
            float mid = t.GetPixel(t.width / 2, Mathf.RoundToInt(t.height * 0.58f)).a;   // CSS y 42% = 텍스처 아래에서 58%
            float corner = t.GetPixel(2, 2).a;
            Assert.Less(mid, 0.01f, "가운데(50%/42%)는 안 누른다 — 전투 라인이 그대로 보인다");
            Assert.Greater(corner, mid + 0.05f, "모서리는 눌린다");
        }

        /// <summary>T178 13회차 — 정본 8235 `.shop-reward-pill { background-image: linear-gradient(180deg, rgba(0,0,0,.16) 0, rgba(0,0,0,.03) 40%, rgba(255,255,255,.10) 82%, rgba(255,255,255,.34) 100%) }`.
        /// 회색 알약 위의 «오목한 홈» — 위는 그늘 · 아래는 빛. 클론은 단색 면 한 장이었다.</summary>
        [UnityTest]
        public IEnumerator 상점_보상_알약은_위가_그늘_아래가_빛인_네_정지점_홈을_진다()
        {
            yield return Boot();
            Assert.AreEqual(180f, SurfaceArt.Angle("shop_reward_pill"), 1e-4f, "정본 8235 180deg");
            Color[] col; float[] off;
            SurfaceArt.Stops("shop_reward_pill", out col, out off);
            Assert.AreEqual(4, col.Length, "정지점 넷");
            Assert.AreEqual(0.40f, off[1], 1e-4f, "40%"); Assert.AreEqual(0.82f, off[2], 1e-4f, "82%");
            Assert.AreEqual(0.16f, col[0].a, 1e-3f, "맨 위 검정 .16"); Assert.AreEqual(0f, col[0].r, 1e-3f, "검정");
            Assert.AreEqual(0.34f, col[3].a, 1e-3f, "맨 아래 흰 .34"); Assert.AreEqual(1f, col[3].r, 1e-3f, "흰색");
            // 위 절반은 **어둡게** 깔리고 아래 절반은 **밝게** 깔린다 — 그것이 «오목한 홈» 이다.
            Assert.AreEqual(0f, SurfaceArt.Sample(col, off, 0.2f).r, 1e-3f, "20% 는 아직 그늘(검정)");
            Assert.AreEqual(1f, SurfaceArt.Sample(col, off, 0.9f).r, 1e-3f, "90% 는 빛(흰색)");
            Sprite sp = SurfaceArt.Bake("shop_reward_pill", 4f, 40f);
            Texture2D t = sp.texture;
            int x = t.width / 2;
            Color topPx = t.GetPixel(x, t.height - 1), botPx = t.GetPixel(x, 0);
            Assert.AreEqual(0f, topPx.r, 0.02f, "맨 윗줄은 검정 그늘");
            Assert.AreEqual(1f, botPx.r, 0.02f, "맨 아랫줄은 흰 빛");
            Assert.Greater(botPx.a, topPx.a, "아래 빛이 위 그늘보다 진하다(.34 > .16)");
        }

        /// <summary>T178 14회차 — 수령 임팩트 글로우 `.rw-glow`(정본 7460)는 단색 원이 아니라 **방사형 판**이다:
        /// 가운데 #ffae14 알파 1 · 56% 에서 알파 .4 · 72% 밖은 투명(`circle` 의 반지름 = 정사각 반대각 절반 · SurfaceUi.json `rw_glow`).</summary>
        [UnityTest]
        public IEnumerator 수령_임팩트_글로우는_가운데가_진하고_72퍼센트_밖이_투명한_방사형_판이다()
        {
            yield return Boot();
            var rewards = new Dictionary<string, double> { { "coins", 100 } };
            int n = RewardBurst.Play(rewards, UiRoot.Instance.Sheet);
            Assert.Greater(n, 0, "수령 연출이 섰다");
            yield return null;
            RewardBurst rb = RewardBurst.Instance;
            Assert.IsNotNull(rb, "연출 층");
            Transform glow = FindDeep(rb.Layer, "rw-glow");
            Assert.IsNotNull(glow, "임팩트 글로우 rw-glow 가 층에 있다");
            RectTransform grt = (RectTransform)glow;
            Assert.AreEqual(grt.sizeDelta.x, grt.sizeDelta.y, 0.01f, "정본 9rem 정사각(circle 은 그 위에 선다)");
            UnityEngine.UI.Image img = glow.GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(img, "Image");
            Assert.IsNotNull(img.sprite, "겹은 구운 그림이다 — 단색 원(UiKit.Circle) 이 아니다");
            Texture2D tex = img.sprite.texture;
            int w = tex.width, h = tex.height;
            Color mid = tex.GetPixel(w / 2, h / 2), corner = tex.GetPixel(1, 1);
            Assert.AreEqual(1f, mid.r, 0.02f, "가운데 #ffae14 — R");
            Assert.AreEqual(174f / 255f, mid.g, 0.03f, "가운데 #ffae14 — G");
            Assert.AreEqual(20f / 255f, mid.b, 0.03f, "가운데 #ffae14 — B");
            Assert.AreEqual(1f, mid.a, 0.02f, "가운데 알파 1");
            Assert.Less(corner.a, 0.02f, "모서리(반대각 = 반지름 100%) 는 72% 밖이라 투명하다");
            // 56% 정지점: 가운데에서 오른쪽으로 .56 × 반지름(.7071 × 변) — 알파 .4 ± 화소 하나의 기울기.
            int x56 = Mathf.RoundToInt((0.5f + 0.56f * 0.7071f) * w - 0.5f);
            Color p56 = tex.GetPixel(x56, h / 2);
            Assert.AreEqual(0.4f, p56.a, 0.08f, "56% 정지점의 알파 .4 (rgba(255,140,0,.4))");
            Assert.AreEqual(140f / 255f, p56.g, 0.04f, "56% 정지점의 G = 140");
            // 30% 정지점: 알파 .9 · G 150.
            int x30 = Mathf.RoundToInt((0.5f + 0.30f * 0.7071f) * w - 0.5f);
            Color p30 = tex.GetPixel(x30, h / 2);
            Assert.AreEqual(0.9f, p30.a, 0.06f, "30% 정지점의 알파 .9");
            Assert.AreEqual(1f, img.color.r, 1e-3f, "그림 위 색은 흰색(박동은 알파만 만진다) — 표 색을 두 번 곱하지 않는다");
        }

        /// <summary>
        /// T178 17회차 — 정본 7969~7973 `#summon-subtabs.subtab-strip button.active` 는 파란 면 위에 겹 둘을 쌓는다:
        /// 위 1 CSS px 흰 광택(.50) + 세로 명암(위 흰 .26 → 46% 에서 0 → 아래 검정 .16).
        /// 면 색이 곁 표(PetSkillUi `pp_blue`)에서 오므로 **부르는 쪽이 색을 주고** 그 색 위에 사슬로 굽는다 —
        /// 사슬이 끊기면 윈겹만 색 위에 서고 아래 명암이 통째로 사라진다(그때 밑줄은 면 색 그대로라 이 자가 넘어진다).
        /// </summary>
        [Test]
        public void 서브탭_켜진_칸은_파란_면_위에_겹_둘이_사슬로_구워진다()
        {
            Color face = new Color32(0, 93, 255, 255);            // PetSkillUi pp_blue #005dff
            Sprite rim = SurfaceArt.Bake("subtab_active_rim", 3f, 48f, face);
            Sprite shade = SurfaceArt.Bake("subtab_active_shade", 3f, 48f, face);
            Assert.IsNotNull(rim, "윈겹을 굽는다");
            Assert.IsNotNull(shade, "아래겹을 굽는다");
            Texture2D tr = rim.texture, ts = shade.texture;
            int w = tr.width, h = tr.height;

            Color rimBottom = tr.GetPixel(w / 2, 0), shadeBottom = ts.GetPixel(ts.width / 2, 0);
            Assert.AreEqual(1f, rimBottom.a, 1e-3f, "바탕을 받은 겹은 불투명하게 구워진다");
            // 밑줄(t=1) = 검정 .16 을 면 위에 얹은 값 — 윈겹은 그 줄에서 투명하니 둘이 같아야 한다.
            Assert.AreEqual(Mathf.RoundToInt(shadeBottom.g * 255f), Mathf.RoundToInt(rimBottom.g * 255f), 2,
                            "윈겹의 밑줄은 아래겹과 같다 — 다르면 사슬(over_layer)이 끊겼다");
            int wantG = SurfaceBlendRules.OverSrgb(93, 0, 0.16);
            Assert.AreEqual(wantG, Mathf.RoundToInt(rimBottom.g * 255f), 3, "아래 검정 .16 (정본 7973) 을 sRGB 바이트로 얹은 값");
            Assert.Less(Mathf.RoundToInt(rimBottom.g * 255f), 93 - 5, "면 색 그대로면 명암이 통째로 없는 것이다");

            Color rimTop = tr.GetPixel(w / 2, h - 1), shadeTop = ts.GetPixel(ts.width / 2, ts.height - 1);
            Assert.Greater(rimTop.r, shadeTop.r + 0.10f, "맨 윈줄은 흰 .50 광택이 더 얹혀 아래겹보다 밝다(정본 7970)");
            Assert.Greater(shadeTop.r, 40f / 255f, "아래겹의 맨 윈줄은 흰 .26 이 얹혀 면 색(R 0)보다 밝다");
            Assert.Greater(rimTop.r, rimBottom.r + 0.15f, "위가 밝고 아래가 어둡다 — 명암의 방향");
        }

        /// <summary>T178 17회차 — 그 겹이 실물 서브탭 켜진 칸에 서는가(색 한 칸짜리 Image 가 아니다).</summary>
        [UnityTest]
        public IEnumerator 소환_서브탭_켜진_칸에_굽는_겹이_실제로_선다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(PetSkillHost.Ready && SkillPetSheet.Instance != null); i++) yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 섰다");
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            yield return null;
            Transform grad = FindDeep(UiRoot.Instance.App, "active-grad");
            Assert.IsNotNull(grad, "서브탭 켜진 칸의 겹 «active-grad» 가 있다");
            UnityEngine.UI.Image gi = grad.GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(gi, "Image");
            Assert.IsNotNull(gi.sprite, "겹은 구운 그림이다 — 색 한 칸짜리가 아니다");
            Assert.AreEqual(1f, gi.color.r, 1e-3f, "그림 위 색은 흰색 — 표 색을 두 번 곱하지 않는다");
            Assert.IsNotNull(grad.parent.GetComponent<UnityEngine.UI.Mask>(),
                             "둥근 면에 Mask 가 걸려 겹이 모서리 밖으로 안 샌다(정본은 border-radius 가 background 를 같이 자른다)");
        }

        /// <summary>
        /// T178 20회차 — 자동 제련 팝업의 «면 겹» 둘. 정본 **5005** `.af-spinner`(검정 금속 톤: 위 흰 .17 → 45% 흰 .02 → 아래 검정 .34)과
        /// **4971** `.af-check`(흰 .14 → 46% 투명 → 아래 검정 .3). 둘 다 안쪽 그늘·바깥 턱(T331)·눌림(T355)은 이미 서 있고 **면 겹만** 없던 자리다.
        /// 스피너 바탕은 표의 `over_color`(pp_ink)로 미리 섞고, 체크 상자는 바탕이 **런타임 색**(켜짐 검정 · 꺼짐 초록)이라 부르는 쪽이 색을 준다.
        /// 굽는 픽셀(위가 밝고 아래가 어둡다 · 불투명하게 합성됐다)과 **실물에 섰는가**(Mask · 구운 그림 · 흰 색)를 같이 묻는다.
        /// </summary>
        [UnityTest]
        public IEnumerator 자동_제련_스피너와_체크_상자에_정본_면_겹이_구워져_선다()
        {
            // ⓐ 구운 픽셀 — 스피너(바탕 pp_ink 는 표가 섞는다)
            Sprite sp = SurfaceArt.Bake("af_spinner", 3f, 48f);
            Assert.IsNotNull(sp, "스피너 겹을 굽는다");
            Texture2D ts = sp.texture;
            Color topS = ts.GetPixel(ts.width / 2, ts.height - 1), botS = ts.GetPixel(ts.width / 2, 0);
            Assert.AreEqual(1f, topS.a, 1e-3f, "바탕을 받은 겹은 불투명하게 구워진다(over_color pp_ink)");
            Assert.Greater(topS.r, botS.r + 0.03f, "위가 밝고 아래가 어둡다 — 180deg 의 방향(정본 5005)");

            // ⓑ 구운 픽셀 — 체크 상자(바탕을 부르는 쪽이 준다 · 꺼짐 초록)
            Color face = new Color(0.14f, 0.77f, 0.32f, 1f);
            Sprite ck = SurfaceArt.Bake("af_check", 1f, 24f, face);
            Assert.IsNotNull(ck, "체크 상자 겹을 굽는다");
            Texture2D tc = ck.texture;
            Color topC = tc.GetPixel(tc.width / 2, tc.height - 1), botC = tc.GetPixel(tc.width / 2, 0);
            Assert.AreEqual(1f, topC.a, 1e-3f, "런타임 색 위에 sRGB 로 미리 섞어 굽는다");
            Assert.Greater(topC.g, face.g - 0.02f, "맨 윗줄은 흰 .14 가 얹혀 면 색보다 어둡지 않다");
            Assert.Less(botC.g, face.g - 0.05f, "맨 아랫줄은 검정 .3 이 얹혀 면 색보다 어둡다");
            Assert.Greater(topC.g, botC.g + 0.05f, "위가 밝고 아래가 어둡다(정본 4971)");

            // ⓒ 실물 — 자동 제련은 2-10 뒤에만 열린다(ForgeCardWidthTests 와 같은 길)
            yield return Boot();
            float t0 = Time.realtimeSinceStartup;
            while (!ForgeHost.Ready && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            ForgeHost h = ForgeHost.Instance;
            Assert.IsNotNull(h, "ForgeHost");
            h.S.BestChapter = 3; h.S.BestStage = 1; h.Pull();
            Assert.IsTrue(h.AutoForgeUnlocked, "2-10 을 넘겨 자동 제련이 해금됐다");
            ForgeAutoPopup.Open(h);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = h.Meta.Popups.Find(ForgeAutoPopup.Name);
            Assert.IsNotNull(p, "자동 제련 팝업이 열렸다");

            foreach (string host in new[] { "af-spinner", "af-check-continue" })
            {
                Transform ht = FindDeep(p.Root, host);
                Assert.IsNotNull(ht, host + " 가 있다");
                Transform grad = FindDeep(ht, "bg-grad");
                Assert.IsNotNull(grad, host + " 의 면 겹 «bg-grad» 가 선다");
                Image gi = grad.GetComponent<Image>();
                Assert.IsNotNull(gi, host + ": Image");
                Assert.IsNotNull(gi.sprite, host + ": 겹은 구운 그림이다 — 색 한 칸짜리가 아니다");
                Assert.AreEqual(1f, gi.color.r, 1e-3f, host + ": 그림 위 색은 흰색 — 표 색을 두 번 곱하지 않는다");
                Assert.IsNotNull(grad.parent.GetComponent<Mask>(), host + ": 둥근 면에 Mask 가 걸려 겹이 모서리 밖으로 안 샌다");
            }
            h.Meta.Popups.Hide(ForgeAutoPopup.Name);
            yield return null;
        }

        /// <summary>
        /// T178 22회차 — 같은 팝업의 남은 면 겹 **넷**. 정본 주석 **4984** 가 앞 둘을 한 줄로 적어 뒀다 —
        /// «트랙은 **파인 홈**, 노브는 **광택 구슬**». 트랙(4986)은 위가 어둡고 아래가 밝아 **안으로 팬** 것처럼 읽히고,
        /// 큰 [시작](5016)은 그 반대로 **솟는다** — 한 화면에서 두 방향이 맞부딪히는 것이 이 자리의 뜻이다.
        /// 서브옵션 행(4998)은 정본이 «얇은 카드 두께» 라 적은 겹이고(그 세 그림자는 T331 26회차가 이미 세웠다),
        /// 노브(4990)는 `circle` 기본 크기 **farthest-corner** 라 반지름이 √(.66²+.74²) = .9916 이다.
        /// </summary>
        [UnityTest]
        public IEnumerator 자동_제련_토글과_시작_버튼과_서브행에_정본_면_겹이_구워져_선다()
        {
            // ⓐ 트랙은 **파인 홈** — 위가 어둡고 아래가 밝다(정본 4986 · 버튼과 반대 방향)
            Color track = new Color(0.12f, 0.16f, 0.29f, 1f);            // af_toggle(#1e2a4a) 자리
            Sprite tk = SurfaceArt.Bake("af_toggle_track", 1f, 26f, track);
            Assert.IsNotNull(tk, "트랙 겹을 굽는다");
            Texture2D tt = tk.texture;
            Color topT = tt.GetPixel(tt.width / 2, tt.height - 1), botT = tt.GetPixel(tt.width / 2, 0);
            Assert.AreEqual(1f, topT.a, 1e-3f, "런타임 색 위에 미리 섞어 굽는다");
            Assert.Less(topT.b, botT.b - 0.03f, "**위가 어둡고 아래가 밝다** — 파인 홈(정본 4986)");

            // ⓑ 큰 [시작] 은 **솟는다** — 위가 밝고 아래가 어둡다(정본 5016 · over_color pp_blue)
            Sprite st = SurfaceArt.Bake("af_start", 3f, 80f);
            Assert.IsNotNull(st, "[시작] 겹을 굽는다");
            Texture2D ts2 = st.texture;
            Color topB = ts2.GetPixel(ts2.width / 2, ts2.height - 1), botB = ts2.GetPixel(ts2.width / 2, 0);
            Assert.AreEqual(1f, topB.a, 1e-3f, "바탕을 받은 겹은 불투명하게 구워진다(over_color pp_blue)");
            Assert.Greater(topB.b, botB.b + 0.03f, "위가 밝고 아래가 어둡다 — 트랙의 파인 홈과 **반대**(정본 5016)");

            // ⓒ 노브 광택은 **한 점에서 퍼진다** — 왼쪽 위(34%,26%)가 가장 밝고 오른쪽 아래가 면 색이다
            Color knob = new Color(0f, 0.36f, 1f, 1f);                   // af_toggle_knob(var(--pp-blue)) 자리
            Sprite kn = SurfaceArt.Bake("af_knob_gloss", 1f, 24f, knob);
            Assert.IsNotNull(kn, "노브 광택을 굽는다");
            Texture2D tk2 = kn.texture;
            int kw = tk2.width, kh = tk2.height;
            Color hot = tk2.GetPixel(Mathf.RoundToInt(kw * 0.34f), Mathf.RoundToInt(kh * (1f - 0.26f)));
            Color cold = tk2.GetPixel(Mathf.RoundToInt(kw * 0.9f), Mathf.RoundToInt(kh * 0.1f));
            Assert.Greater(hot.r, cold.r + 0.2f, "중심 (34%,26%) 이 가장 밝다 — 광택 구슬(정본 4990)");
            Assert.AreEqual(knob.b, cold.b, 0.04f, "54% 밖은 면 색 그대로다(반지름 .9916 의 54%)");

            // ⓓ 서브 행은 위가 밝고 아래가 살짝 어둡다(정본 4998 · 아래 겹이 .07 뿐이라 창이 좁다)
            Color row = new Color(0xd6 / 255f, 0xd6 / 255f, 0xd6 / 255f, 1f);
            Sprite sr = SurfaceArt.Bake("af_sub_row", 3f, 30f, row);
            Assert.IsNotNull(sr, "서브 행 겹을 굽는다");
            Texture2D tr = sr.texture;
            Color topR = tr.GetPixel(tr.width / 2, tr.height - 1), botR = tr.GetPixel(tr.width / 2, 0);
            Assert.Greater(topR.r, row.r + 0.05f, "맨 윗줄은 흰 .75 가 얹혀 면 색보다 밝다");
            Assert.Less(botR.r, row.r, "맨 아랫줄은 검정 .07 이 얹혀 면 색보다 어둡다");

            // ⓓ' 런 1042 가 낸 빨강 17 의 자리 — **방사형 겹을 «런타임 색» 길로 얹기**.
            //   `Fill`/`FillMasked` 의 `Color` 오버로드가 `IsRadial` 검사 **앞에서** `Angle(key)` 를 불러 터졌다(각도가 없는 겹이다).
            //   그 길이 실제로 통하는지를 여기서 못 박는다 — 겹 하나가 팝업 전체를 못 서게 했던 자리라 «안 터진다» 만으로도 값이 있다.
            yield return Boot();
            {
                RectTransform probe = UiKit.Box(UiRoot.Instance.App, "t178-radial-probe");
                try
                {
                    Image face = UiKit.Rounded(probe, "face", "pp_blue", 6f);
                    Image lay = SurfaceArt.FillMasked(face, "bg-grad", "af_knob_gloss", 24f, 24f, knob);
                    Assert.IsNotNull(lay, "방사형 겹을 런타임 색 길로 얹는다");
                    Assert.IsNotNull(lay.sprite, "그 길로도 구운 그림이 나온다(각도를 안 묻는다)");
                }
                finally { Object.Destroy(probe.gameObject); }
                yield return null;
            }

            // ⓔ 실물 — 네 자리가 화면에 실제로 서는가
            float t0 = Time.realtimeSinceStartup;
            while (!ForgeHost.Ready && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            ForgeHost h = ForgeHost.Instance;
            Assert.IsNotNull(h, "ForgeHost");
            h.S.BestChapter = 3; h.S.BestStage = 1;
            h.Pull();
            Assert.IsTrue(h.AutoForgeUnlocked, "2-10 을 넘겨 자동 제련이 해금됐다");
            // 런 1048 빨강: 서브옵션 행은 필터가 켜져야 서는데 `FilterOn` 을 **`Pull` 앞에서** 켜서 세이브 값으로 다시 덮였다.
            //   순서는 `Pull`(세이브 → 엔진) → 설정 → `Push` 다(`AgePatternTests` 250~256 · `PinnedColorSitesTests` 12회차 칸과 같은 길).
            h.Engine.AutoForgeConfig().FilterOn = true;
            h.Push();
            ForgeAutoPopup.Open(h);
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p2 = h.Meta.Popups.Find(ForgeAutoPopup.Name);
            Assert.IsNotNull(p2, "자동 제련 팝업이 열렸다");
            Transform tgT = FindDeep(p2.Root, "af-toggle");
            Assert.IsNotNull(tgT, "필터 토글");
            foreach (string path in new[] { "face/bg-grad", "knob/face/bg-grad" })
            {
                Transform g = tgT.Find(path);
                Assert.IsNotNull(g, "토글 " + path + " 가 선다");
                Assert.IsNotNull(g.GetComponent<Image>().sprite, "토글 " + path + ": 구운 그림이다");
            }
            Transform startT = FindDeep(p2.Root, "af-start");
            Assert.IsNotNull(startT, "[시작] 버튼");
            Assert.IsNotNull(startT.Find("face/bg-grad"), "[시작] 면 겹이 선다");
            Transform subT = null;
            foreach (Transform c in p2.Root.GetComponentsInChildren<Transform>(true))
                if (c.name.StartsWith("af-sub-", System.StringComparison.Ordinal)) { subT = c; break; }
            Assert.IsNotNull(subT, "필터 서브옵션 행 — 필터를 켰으니 선다");
            Assert.IsNotNull(subT.Find("face/bg-grad"), "서브 행 면 겹이 선다");
            h.Meta.Popups.Hide(ForgeAutoPopup.Name);
            yield return null;
        }

        /// <summary>
        /// T178 21회차 — **섞는 자리**: CSS 그라디언트의 색 보간은 «프리멀티플라이드 알파» 다(CSS Images 3).
        /// 그냥 섞으면 «투명에 가까운 흰색 → 반투명 검정» 짝에서 가운데가 **회색으로 밝아지는 띠**가 생긴다 — 브라우저엔 없는 띠다.
        /// 실측(런 1015 `screen_autoforge.png`): 정본 5005 스피너의 45%(흰 .02) ↔ 100%(검정 .34) 사이 **63.6%** 자리가
        /// 면 색 23 위에서 **42** 로 밝아져 있었다. 프리멀티플라이드로 섞으면 그 자리가 23 이라 위에서 아래로 **단조롭게** 어두워진다.
        /// </summary>
        [Test]
        public void 정지점_사이는_프리멀티플라이드로_섞는다_가운데가_밝아지는_띠가_없다()
        {
            // ⓐ 식 자체 — 흰 .02 ↔ 검정 .34 의 한가운데
            Color w02 = new Color(1f, 1f, 1f, 0.02f), b34 = new Color(0f, 0f, 0f, 0.34f);
            Color mid = SurfaceArt.LerpPremul(w02, b34, 0.5f);
            Assert.AreEqual(0.18f, mid.a, 1e-4f, "알파는 그냥 섞기와 같다");
            Assert.Less(mid.r, 0.10f, "색은 거의 검정이다 — 그냥 섞으면 .5 회색이 된다(그것이 밝은 띠의 정체)");
            Color same = SurfaceArt.LerpPremul(new Color(1f, 0f, 0f, 1f), new Color(0f, 0f, 1f, 1f), 0.5f);
            Assert.AreEqual(0.5f, same.r, 1e-4f, "알파가 같으면 그냥 섞기와 똑같다(옛 자리들이 안 흔들린다)");

            // ⓑ 구운 그림 — 스피너 겹은 위에서 아래로 단조롭게 어두워진다(가운데 띠가 없다)
            Sprite sp = SurfaceArt.Bake("af_spinner", 3f, 48f);
            Assert.IsNotNull(sp, "스피너 겹을 굽는다");
            Texture2D t = sp.texture;
            int x = t.width / 2;
            float prev = t.GetPixel(x, t.height - 1).r;
            for (int i = 1; i <= 8; i++)
            {
                int y = Mathf.RoundToInt((t.height - 1) * (1f - i / 8f));
                float cur = t.GetPixel(x, y).r;
                Assert.LessOrEqual(cur, prev + 0.004f, "위에서 아래로 단조롭게 어두워진다 — 밝아지는 칸이 있으면 그냥 섞기다(y=" + y + ")");
                prev = cur;
            }
            // 45% 자리와 63.6% 자리(실측이 42 였던 곳)의 방향
            float p45 = t.GetPixel(x, Mathf.RoundToInt((t.height - 1) * 0.55f)).r;
            float p64 = t.GetPixel(x, Mathf.RoundToInt((t.height - 1) * 0.364f)).r;
            Assert.Less(p64, p45 + 0.004f, "63.6% 는 45% 보다 밝지 않다(종전엔 23 → 42 로 밝아졌다)");
        }

        /// <summary>
        /// T178 23회차 — 리그 시트 **발 밴드**의 면 겹(정본 **2361** `.league-foot`).
        /// `linear-gradient(180deg, rgba(255,255,255,.16) **0 1px**, rgba(255,255,255,.05) **8%**, rgba(255,255,255,0) **30%**, rgba(0,0,0,.22) **100%**)` —
        /// **한 그라디언트가 길이(px)와 백분율을 섞어 쓰는 첫 자리**다. 여태 표는 `unit: "px"`(겹 전체가 px)만 알았으므로
        /// 이 자리는 정지점마다 단위를 적는 `units` 로 갈랐다(`SurfaceArt.PxMask`).
        ///
        /// ⚑ 그래서 이 자의 핵심 칸은 «방향» 이 아니라 **ⓑ 다**: 선 길이를 두 배로 늘려 다시 구우면
        /// **px 정지점만 절반으로 줄고 % 정지점은 제자리**여야 한다. 만약 누가 `units` 를 지우고 `unit: "px"` 로 되돌리면
        /// 8%·30%·100% 가 8·30·100 **CSS px** 이 되어 밴드 위 몇 줄에 겹이 다 몰리고, 이 칸이 먼저 운다.
        /// </summary>
        [UnityTest]
        public IEnumerator 리그_발_밴드에_정본_면_겹이_구워져_서고_px_정지점만_선_길이를_탄다()
        {
            float footH = UiKit.RefH * (1f - UiKit.L("league_foot_top"));
            Assert.Greater(footH, 1f, "발 밴드 높이");

            // ⓐ 방향 — 위 1px 흰 림에서 시작해 바닥 검정 .22 로 단조롭게 어두워진다(180deg).
            Sprite sp = SurfaceArt.Bake("league_foot", UiKit.RefW / footH, footH);
            Assert.IsNotNull(sp, "발 밴드 겹을 굽는다");
            Texture2D t = sp.texture;
            int x = t.width / 2;
            Color top = t.GetPixel(x, t.height - 1), bot = t.GetPixel(x, 0);
            Assert.AreEqual(1f, top.a, 1e-3f, "바탕(#1a1f2b = 표 league_foot)을 받은 겹은 불투명하게 구워진다");
            Assert.AreEqual(1f, bot.a, 1e-3f, "아래도 불투명");
            Assert.Greater(top.r, bot.r + 0.03f, "위가 밝고 아래가 어둡다 — 180deg 의 방향(정본 2361)");
            float prev = 2f;
            for (int i = 0; i <= 8; i++)
            {
                float v = t.GetPixel(x, Mathf.RoundToInt((t.height - 1) * (1f - i / 8f))).r;
                Assert.Less(v, prev + 0.004f, "위에서 아래로 단조롭게 어두워진다 — " + i + "/8 에서 되밝아졌다");
                prev = v;
            }

            // ⓑ 정지점마다 단위가 갈린다 — 선 길이를 두 배로 하면 **% 자리는 그대로**다.
            Sprite sp2 = SurfaceArt.Bake("league_foot", UiKit.RefW / (footH * 2f), footH * 2f);
            Assert.IsNotNull(sp2, "두 배 길이로도 굽는다");
            Texture2D t2 = sp2.texture;
            foreach (float f in new[] { 0.30f, 0.60f, 1.00f })
            {
                float a = t.GetPixel(x, Mathf.RoundToInt((t.height - 1) * (1f - f))).r;
                float b = t2.GetPixel(t2.width / 2, Mathf.RoundToInt((t2.height - 1) * (1f - f))).r;
                Assert.AreEqual(a, b, 0.012f, "백분율 정지점은 선 길이를 안 탄다 — " + (f * 100f) + "% 에서 " + a + " ↔ " + b);
            }

            // ⓒ 실물 — 리그 시트를 열어 발 밴드에 «bg-grad» 가 서는지.
            yield return Boot();
            MetaHost.Instance.OpenLeague();
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = MetaHost.Instance.Popups.Find(LeagueSheet.Name);
            Assert.IsNotNull(p, "리그 시트가 열렸다");
            Transform foot = FindDeep(p.Root, "foot");
            Assert.IsNotNull(foot, "발 밴드(foot)");
            Transform grad = FindDeep(foot, "bg-grad");
            Assert.IsNotNull(grad, "발 밴드의 면 겹 «bg-grad» 가 선다(정본 2361 — 종전엔 단색 한 장이었다)");
            Image gi = grad.GetComponent<Image>();
            Assert.IsNotNull(gi, "면 겹: Image");
            Assert.IsNotNull(gi.sprite, "면 겹은 구운 그림이다 — 색 한 칸짜리가 아니다");
            Assert.AreEqual(1f, gi.color.r, 1e-3f, "그림 위 색은 흰색 — 표 색을 두 번 곱하지 않는다");
            yield return null;
        }


        /// <summary>
        /// T178 24회차 — 장비 **상세** 팝업 아이콘 상자의 교차 해칭(정본 **3661** `#forge-item-modal .idet-icon`).
        /// 정본 주석 3657 이 뜻을 적어 뒀다 — «장비 상세도 목록과 같은 언어 … 해칭 배경 + 시대색 58% 틴트 면 + 시대색 80% 테».
        /// 면 색(color-mix)은 T371 이 이미 덮어 뒀고 **해칭 두 겹만** 없었다. 그래서 이 자는 «색» 이 아니라 «해칭이 서는가» 를 본다 —
        /// `.equip-cell`·제작 카드와 **같은 표 키**(`cell_hatch`)를 쓰는지까지(다른 키로 갈라 놓으면 세 화면이 서로 달라진다).
        /// </summary>
        [UnityTest]
        public IEnumerator 장비_상세_아이콘_상자도_목록_셀과_같은_교차_해칭을_받는다()
        {
            yield return Boot();
            float t0 = Time.realtimeSinceStartup;
            while (!ForgeHost.Ready && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            ForgeHost h = ForgeHost.Instance;
            Assert.IsNotNull(h, "ForgeHost");
            string age = h.Defs.Ages[0];
            string wt = h.Engine.WeaponsOfAge(age)[0];
            ForgeInfoPopup.OpenDetail(h, age, "weapon", 0, wt);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = h.Meta.Popups.Find(ForgeInfoPopup.ItemName);
            Assert.IsNotNull(p, "장비 상세 팝업");
            Transform face = FindDeep(p.Root, "idet-icon");
            Assert.IsNotNull(face, "아이콘 상자(idet-icon)");
            Transform hf = face.Find("frame/face");
            Assert.IsNotNull(hf, "아이콘 상자의 면(frame/face)");
            Transform hatch = hf.Find("hatch");
            Assert.IsNotNull(hatch, "면 위에 해칭 겹 «hatch» 가 선다(정본 3661 — 종전엔 면 색만 있었다)");
            Image hi = hatch.GetComponent<Image>();
            Assert.IsNotNull(hi, "해칭: Image");
            Assert.IsNotNull(hi.sprite, "해칭은 구운 그림이다");
            Assert.AreEqual(Image.Type.Tiled, hi.type, "해칭은 되풀이 타일이다 — 늘리면 주기가 상자 크기를 탄다");
            Assert.IsNotNull(hf.GetComponent<Mask>(), "둥근 면에 Mask 가 걸려 해칭이 모서리 밖으로 안 샌다");
            // 같은 표 키를 쓰는가 — `.equip-cell`·제작 카드와 한 그림이어야 한다(정본이 그렇게 적었다).
            Color mix = ColorMixUi.Mix("idet_icon_face", ForgeUi.AgeColor(h.Defs, age));
            Assert.AreEqual(SurfaceArt.BakeHatch("cell_hatch", mix), hi.sprite,
                            "목록 셀·제작 카드와 **같은 키**(cell_hatch)로 구운 같은 그림이다");
            ForgeInfoPopup.Close(h);
            yield return null;
        }


        /// <summary>
        /// T178 27회차 — 소환 결과 **제목 띠**(정본 **6207**)와 그 위·아래 **금색 헤어라인**(**6220**).
        /// 둘의 공통점이 이 자의 핵심이다 — **양끝 알파가 0** 이라 «공중에서 뚝 끊기지 않게» 사라진다(정본 주석).
        /// 클론은 띠가 단색 한 장이라 양끝이 각졌고 헤어라인은 아예 없었다.
        /// 그래서 «겹이 섰다» 만 보지 않고 **구운 화소의 양끝이 투명하고 가운데가 짙은가**를 본다 — 그것이 이 선언의 뜻이다.
        /// </summary>
        [UnityTest]
        public IEnumerator 소환_결과_제목_띠와_헤어라인은_양끝이_사라지는_가로_겹이다()
        {
            // ⓐ 구운 화소 — 띠: 가운데 알파 .85 · 양끝 0
            Sprite bp = SurfaceArt.Bake("sr_title_band", 6f, 600f);
            Assert.IsNotNull(bp, "제목 띠를 굽는다");
            Texture2D bt = bp.texture;
            Color mid = bt.GetPixel(bt.width / 2, bt.height / 2);
            Color lend = bt.GetPixel(0, bt.height / 2), rend = bt.GetPixel(bt.width - 1, bt.height / 2);
            Assert.AreEqual(0.85f, mid.a, 0.03f, "가운데 알파 = 정본 .85");
            Assert.Less(lend.a, 0.06f, "왼쪽 끝은 사라진다(알파 0)");
            Assert.Less(rend.a, 0.06f, "오른쪽 끝도 사라진다(알파 0)");
            Assert.Less(mid.r + mid.g, lend.r + lend.g + 2f, "가운데는 짙은 남색이다(정본 rgba(12,20,52,·))");

            // ⓑ 구운 화소 — 헤어라인: 금색(정본 rgba(255,214,120,·))이고 같은 꼴로 사라진다
            Sprite hp = SurfaceArt.Bake("sr_title_hair", 200f, 600f);
            Assert.IsNotNull(hp, "헤어라인을 굽는다");
            Texture2D ht = hp.texture;
            Color hm = ht.GetPixel(ht.width / 2, ht.height / 2), he = ht.GetPixel(0, ht.height / 2);
            Assert.AreEqual(0.7f, hm.a, 0.03f, "가운데 알파 = 정본 .7");
            Assert.Less(he.a, 0.06f, "헤어라인도 양끝이 사라진다");
            Assert.Greater(hm.r, hm.b + 0.3f, "금색이다 — 붉은 쪽이 파란 쪽보다 훨씬 높다");

            // ⓒ 실물 — 띠 판의 그림이 바뀌었고 헤어라인 둘이 섰다
            yield return Boot();
            float t0 = Time.realtimeSinceStartup;
            while (!(SkillPetSheet.Instance != null && PetSkillHost.Ready) && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다");
            var list = new System.Collections.Generic.List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = "가" },
            };
            SkillSummonResultView v = SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", list, "common", null);
            Assert.IsNotNull(v, "결과 연출 팝업이 서지 않았다");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Transform band = FindDeep(v.transform, "sr-title");
            Assert.IsNotNull(band, "제목 띠(sr-title)");
            Image bg = band.Find("bg") != null ? band.Find("bg").GetComponent<Image>() : null;
            Assert.IsNotNull(bg, "띠의 판(bg)");
            Assert.IsNotNull(bg.sprite, "띠는 구운 그림이다 — 색 한 칸짜리가 아니다(종전엔 단색이라 양끝이 각졌다)");
            Assert.AreEqual(1f, bg.color.r, 1e-3f, "그림 위 색은 흰색 — 표 색을 두 번 곱하지 않는다");
            foreach (string n in new[] { "hair-top", "hair-bot" })
            {
                Transform hr = band.Find(n);
                Assert.IsNotNull(hr, "헤어라인 «" + n + "» 이 선다(정본 6220)");
                Image hi = hr.GetComponent<Image>();
                Assert.IsNotNull(hi, n + ": Image");
                Assert.IsNotNull(hi.sprite, n + ": 구운 그림");
            }
            yield return null;
        }

        /// <summary>
        /// T178 29회차 — 소환 결과 x1 요약의 **[다시 소환]** 버튼(정본 **5791** `.sr-again { background: linear-gradient(rgba(52,66,120,.9), rgba(30,40,82,.9)) }`).
        /// 27회차가 «알파 .9 라 무엇 위에 섞나부터 정하라» 고 남긴 자리다 — 바탕은 모달의 방사형(5653)이라 표에 `over_color` 를 못 적고
        /// **부르는 쪽이 색을 준다**(팔레트 `sr_bg_c` · 결정 808). 그래서 이 자는 «겹이 섰다» 만 보지 않고 **구운 화소가 불투명하고(sRGB 미리 합성)
        /// 위가 밝고 아래가 어두우며 그 값이 정본 셈(.9×정본 + .1×바탕)과 맞는가** 를 본다 — 종전엔 `sr_again` 단색 .9 를 선형 공간에서 섞었다.
        /// </summary>
        [UnityTest]
        public IEnumerator 소환_결과_다시_소환_버튼은_바탕을_받아_불투명하게_구운_세로_명암_겹이다()
        {
            // ⓐ 구운 화소 — 바탕 sr_bg_c(#070b20) 위 sRGB 합성: 위 = .9×(52,66,120)+.1×바탕 · 아래 = .9×(30,40,82)+.1×바탕 · 알파 1
            Color under = PetSkillStyle.C("sr_bg_c");
            float ur = Mathf.Round(under.r * 255f), ug = Mathf.Round(under.g * 255f), ub = Mathf.Round(under.b * 255f);
            Sprite sp = SurfaceArt.Bake("sr_again", 8.6f / 2.3f, 100f, under);
            Assert.IsNotNull(sp, "[다시 소환] 겹을 굽는다");
            Texture2D tx = sp.texture;
            Color top = tx.GetPixel(tx.width / 2, tx.height - 1), bot = tx.GetPixel(tx.width / 2, 0);
            Assert.AreEqual(1f, top.a, 1e-3f, "바탕을 미리 섞어 불투명하다(T357 — 선형 공간에서 알파를 남기면 어두운 바탕에서 밝게 뜬다)");
            Assert.AreEqual(1f, bot.a, 1e-3f, "아래도 불투명");
            float tol = 2.5f / 255f;
            Assert.AreEqual((0.9f * 52f + 0.1f * ur) / 255f, top.r, tol, "위 R = .9×52 + .1×바탕");
            Assert.AreEqual((0.9f * 66f + 0.1f * ug) / 255f, top.g, tol, "위 G = .9×66 + .1×바탕");
            Assert.AreEqual((0.9f * 120f + 0.1f * ub) / 255f, top.b, tol, "위 B = .9×120 + .1×바탕");
            Assert.AreEqual((0.9f * 30f + 0.1f * ur) / 255f, bot.r, tol, "아래 R = .9×30 + .1×바탕");
            Assert.AreEqual((0.9f * 40f + 0.1f * ug) / 255f, bot.g, tol, "아래 G = .9×40 + .1×바탕");
            Assert.AreEqual((0.9f * 82f + 0.1f * ub) / 255f, bot.b, tol, "아래 B = .9×82 + .1×바탕");
            Assert.Greater(top.r + top.g + top.b, bot.r + bot.g + bot.b + 0.15f, "위가 밝고 아래가 어둡다(정본 첫 정지점 → 둘째)");

            // ⓑ 실물 — x1 요약의 버튼 면에 Mask + 구운 겹(bg-grad)이 서고, 그 그림이 같은 바탕으로 구워졌다
            yield return Boot();
            float t0 = Time.realtimeSinceStartup;
            while (!(SkillPetSheet.Instance != null && PetSkillHost.Ready) && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다");
            var list = new System.Collections.Generic.List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = "가" },
            };
            SkillSummonResultView v = SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", list, "common", null);
            Assert.IsNotNull(v, "결과 연출 팝업이 서지 않았다");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Transform again = FindDeep(v.transform, "sr-again");
            Assert.IsNotNull(again, "[다시 소환] 버튼(sr-again · x1 요약에서만 선다)");
            Transform face = again.Find("skin/face");
            Assert.IsNotNull(face, "버튼의 둥근 면(skin/face)");
            Assert.IsNotNull(face.GetComponent<Mask>(), "면에 Mask — 겹이 모서리 밖으로 안 샌다");
            Transform g = face.Find("bg-grad");
            Assert.IsNotNull(g, "겹(bg-grad)이 면 위에 선다(정본 5791)");
            Image gi = g.GetComponent<Image>();
            Assert.IsNotNull(gi, "겹: Image");
            Assert.IsNotNull(gi.sprite, "구운 그림이다 — 색 한 칸짜리가 아니다(종전엔 `sr_again` 단색 .9)");
            Assert.AreEqual(1f, gi.color.r, 1e-3f, "그림 위 색은 흰색 — 표 색을 두 번 곱하지 않는다");
            Texture2D rt = gi.sprite.texture;
            Color rtop = rt.GetPixel(rt.width / 2, rt.height - 1), rbot = rt.GetPixel(rt.width / 2, 0);
            Assert.AreEqual(1f, rtop.a, 1e-3f, "실물 겹도 불투명하다(바탕을 받았다)");
            Assert.AreEqual(top.r, rtop.r, tol, "실물 위 R = 자가 구운 것과 같은 바탕(sr_bg_c)");
            Assert.AreEqual(bot.b, rbot.b, tol, "실물 아래 B = 자가 구운 것과 같은 바탕(sr_bg_c)");
            yield return null;
        }

        /// <summary>
        /// T178 30회차 — 소환 결과 구슬의 **접지 그림자**(정본 **6350** `.sr-orbwrap::before { left: 13%; right: 13%; bottom: -6%; height: 13%;
        /// border-radius: 50%; background: radial-gradient(closest-side, rgba(0,0,0,.62), rgba(0,0,0,0) 76%) }` · 정본 주석 6349
        /// «구체 내부 그라디언트로는 읽히지 않아 구 밖에 눌린 타원으로 깐다»). 클론엔 통째로 없었다.
        /// 자는 ⓐ 구운 화소 — 가운데 검정 .62 · 76% 밖 알파 0 ⓑ 실물 — 래퍼 안에 `sr-ground` 가 표의 자리에 서고 구슬 **앞 순서**(구슬 뒤)다.
        /// </summary>
        [UnityTest]
        public IEnumerator 소환_결과_구슬_접지_그림자는_구슬_뒤_바닥에_눌린_검정_방사_타원이다()
        {
            // ⓐ 구운 화소
            float side = PetSkillStyle.L("sr_ground_side_f"), gh0 = PetSkillStyle.L("sr_ground_h_f");
            Sprite sp = SurfaceArt.Bake("sr_ground", (1f - side * 2f) / gh0);
            Assert.IsNotNull(sp, "접지 그림자를 굽는다");
            Texture2D tx = sp.texture;
            Color mid = tx.GetPixel(tx.width / 2, tx.height / 2);
            Color edge = tx.GetPixel((int)(tx.width * 0.97f), tx.height / 2);
            Assert.AreEqual(0.62f, mid.a, 0.03f, "가운데 알파 = 정본 .62");
            Assert.Less(mid.r + mid.g + mid.b, 0.02f, "검정이다");
            Assert.Less(edge.a, 0.03f, "76% 밖은 사라진다(알파 0) — 투명을 품은 채 얹는 겹");
            Color q = tx.GetPixel((int)(tx.width * 0.69f), tx.height / 2);   // u = .38 → 알파 = .62 × (1 − .38/.76) = .31
            Assert.AreEqual(0.31f, q.a, 0.04f, "반지름 절반 자리(u .38)는 .31 — 정지점 사이 선형");

            // ⓑ 실물
            yield return Boot();
            float t0 = Time.realtimeSinceStartup;
            while (!(SkillPetSheet.Instance != null && PetSkillHost.Ready) && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다");
            var list = new System.Collections.Generic.List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = "가" },
            };
            SkillSummonResultView v = SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", list, "common", null);
            Assert.IsNotNull(v, "결과 연출 팝업이 서지 않았다");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Transform wrap = FindDeep(v.transform, "sr-orbwrap");
            Assert.IsNotNull(wrap, "구슬 래퍼(sr-orbwrap)");
            Transform g = wrap.Find("sr-ground");
            Assert.IsNotNull(g, "접지 그림자(sr-ground)가 래퍼 안에 선다(정본 6350)");
            Transform orb = wrap.Find("sr-orb"), deep = wrap.Find("sr-orb-deep");
            Assert.IsNotNull(orb, "구슬 본체(sr-orb)");
            Assert.Less(g.GetSiblingIndex(), orb.GetSiblingIndex(), "그림자는 구슬 본체 앞 순서(= 구슬 뒤)다 — `::before`");
            if (deep != null) Assert.Less(g.GetSiblingIndex(), deep.GetSiblingIndex(), "깊은 판 앞 순서이기도 하다");
            Assert.Greater(g.GetSiblingIndex(), 0, "첫 자식은 잔상 계약(결정 804) — 그림자가 그 자리를 뺏지 않는다");
            RectTransform gr = (RectTransform)g;
            Assert.AreEqual(side, gr.anchorMin.x, 1e-3f, "left 13%");
            Assert.AreEqual(1f - side, gr.anchorMax.x, 1e-3f, "right 13%");
            Assert.AreEqual(-PetSkillStyle.L("sr_ground_bottom_f"), gr.anchorMin.y, 1e-3f, "bottom −6%");
            Assert.AreEqual(gh0 - PetSkillStyle.L("sr_ground_bottom_f"), gr.anchorMax.y, 1e-3f, "height 13%");
            Image gi = g.GetComponent<Image>();
            Assert.IsNotNull(gi, "그림자: Image");
            Assert.IsNotNull(gi.sprite, "구운 그림이다 — 색 한 칸짜리가 아니다");
            Assert.AreEqual(1f, gi.color.r, 1e-3f, "그림 위 색은 흰색");
            yield return null;
        }

        /// <summary>
        /// T178 31회차 — 소환 결과 **중앙 광원**(정본 **5955** `.sr-halo { radial-gradient(closest-side, var(--pre-halo) 0%, rgba(90,130,255,.1) 48%, rgba(0,0,0,0) 100%) }`)
        /// 과 공개 뒤 판(**7166** `.done .sr-halo` · 정지점 둘 · `--halo` .4). 0% 정지점이 **런타임 등급색**이라 «부르는 쪽이 정지점을 준다» 길
        /// (`SurfaceArt.Bake(key, aspect, stopIndex, color)`)로 굽는다. 자는 ⓐ 준 정지점이 화소에 그대로 앉고 색마다 다른 판이 나오는가
        /// ⓑ 실물 광원이 구운 그림(흰 색)이고 가운데 알파가 .3 + .16 × pk(일반 = .3) 인가 ⓒ done 뒤 판이 .4 짜리로 갈아타는가 — 종전엔 `Disc` 단색 .18 이었다.
        /// </summary>
        [UnityTest]
        public IEnumerator 소환_결과_중앙_광원은_등급색_정지점을_받아_구운_방사_겹이고_done_뒤_판으로_갈아탄다()
        {
            // ⓐ 정지점을 부르는 쪽이 준다 — 빨강 .3 을 0% 에 주면 가운데가 빨강 .3 · 48% 자리는 표의 (90,130,255,.1) · 끝은 0
            Sprite red = SurfaceArt.Bake("sr_halo", 26f / 16f, 0, new Color(1f, 0f, 0f, 0.3f));
            Assert.IsNotNull(red, "광원을 굽는다");
            Texture2D rt = red.texture;
            Color mid = rt.GetPixel(rt.width / 2, rt.height / 2);
            Assert.AreEqual(0.3f, mid.a, 0.03f, "가운데 알파 = 준 정지점 .3");
            Assert.Greater(mid.r, 0.9f, "가운데는 준 색(빨강)이다");
            Assert.Less(mid.b, 0.1f, "가운데는 준 색(빨강)이다 — 표의 기본 파랑이 아니다");
            Color q = rt.GetPixel((int)(rt.width * 0.74f), rt.height / 2);   // u = .48 → 둘째 정지점
            Assert.AreEqual(0.1f, q.a, 0.03f, "48% 자리 알파 = 정본 .1");
            Assert.Greater(q.b, q.r + 0.3f, "48% 자리는 표의 파랑(90,130,255)");
            Color edge = rt.GetPixel(rt.width - 1, rt.height / 2);
            Assert.Less(edge.a, 0.03f, "끝은 사라진다(알파 0)");
            Sprite green = SurfaceArt.Bake("sr_halo", 26f / 16f, 0, new Color(0f, 1f, 0f, 0.3f));
            Assert.AreNotEqual(red, green, "정지점 색이 다르면 다른 판(캐시 이름에 색이 붙는다)");
            Assert.AreEqual(red, SurfaceArt.Bake("sr_halo", 26f / 16f, 0, new Color(1f, 0f, 0f, 0.3f)), "같은 색은 같은 판(캐시)");

            // ⓑ 실물
            yield return Boot();
            float t0 = Time.realtimeSinceStartup;
            while (!(SkillPetSheet.Instance != null && PetSkillHost.Ready) && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다");
            var list = new System.Collections.Generic.List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = "가" },
            };
            SkillSummonResultView v = SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", list, "common", null);
            Assert.IsNotNull(v, "결과 연출 팝업이 서지 않았다");
            yield return null;
            Transform halo = FindDeep(v.transform, "halo");
            Assert.IsNotNull(halo, "광원(halo)");
            Image hi = halo.GetComponent<Image>();
            Assert.IsNotNull(hi.sprite, "광원은 구운 그림이다 — 색 한 칸짜리 Disc 가 아니다");
            Assert.AreEqual(1f, hi.color.r, 1e-3f, "그림 위 색은 흰색(등급색은 정지점에 있다)");
            Texture2D ht = hi.sprite.texture;
            Color hm = ht.GetPixel(ht.width / 2, ht.height / 2);
            Assert.AreEqual(PetSkillStyle.L("sr_halo_pre_a0"), hm.a, 0.03f, "일반(pk 0)의 가운데 알파 = .3 + .16 × 0");
            Assert.Less(ht.GetPixel(ht.width - 1, ht.height / 2).a, 0.03f, "실물도 끝이 사라진다");
            Sprite pre = hi.sprite;

            // ⓒ done 뒤 — 정지점 둘짜리 .4 판으로 갈아탄다(정본 7166 · 이산 전환 · 진행 .5)
            float t = 0f;
            while (!v.Done && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(v.Done, "done");
            t = 0f;
            while (hi.sprite == pre && t < 2f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.AreNotEqual(pre, hi.sprite, "done 뒤 광원 판이 갈아탔다(정본 7166)");
            Texture2D dt = hi.sprite.texture;
            Assert.AreEqual(PetSkillStyle.L("sr_halo_done_a"), dt.GetPixel(dt.width / 2, dt.height / 2).a, 0.03f, "done 판 가운데 알파 = .4");
            Assert.Less(dt.GetPixel(dt.width - 1, dt.height / 2).a, 0.03f, "done 판도 끝이 사라진다");
            v.Close();
            yield return null;
        }

        /// <summary>
        /// T178 32회차 — 소환진 바닥 두 겹(정본 5806~5813 `.sr-floor::before` · 링 61/63.5/66.5 + 빛 고인 면 0/58/100)과 홀드백 착지 섬광(6156 `.sr-flash` · circle at --fx --fy · #fff 0% · --rc 26% · 0 72%).
        /// ⓐ 판: 흰색 + 정본 알파 단면(fill 가운데 .2 · 58% 자리 .07 · 끝 0 / ring 63.5% 자리 .5 · 그 안팎 0 / flash 가운데 흰 1 · 26% 자리 = 준 등급색 · 72% 뒤 0)
        /// ⓑ 실물: 소환진 Image 가 구운 판이고 틴트는 종전 `floor_fill` rgb(알파 1 · 알파는 판이 쥔다) · 링 자식 `sr-floor-ring` 이 구운 판 + `floor_line` 틴트
        /// ⓒ done: 두 판이 .26 / .85 로 다시 구워지고 틴트는 등급색 / 파생색 · 섬광은 정사각 판을 클립 안에 두고 26% 정지점이 등급색이다 — 종전엔 색 한 칸 판이었다.
        /// </summary>
        [UnityTest]
        public IEnumerator 소환진_바닥은_알파_단면_두_판이고_섬광은_등급색_정지점의_farthest_corner_원판이다()
        {
            // ⓐ 판
            Sprite fill = SurfaceArt.Bake("sr_floor_fill", 2.5f);
            Texture2D ft = fill.texture;
            Assert.AreEqual(0.2f, ft.GetPixel(ft.width / 2, ft.height / 2).a, 0.03f, "바닥 면 가운데 알파 = 정본 .2");
            Assert.AreEqual(0.07f, ft.GetPixel((int)(ft.width * (0.5f + 0.29f)), ft.height / 2).a, 0.03f, "58% 자리 알파 = 정본 .07");
            Assert.Less(ft.GetPixel(ft.width - 1, ft.height / 2).a, 0.03f, "끝은 사라진다");
            Assert.Greater(ft.GetPixel(ft.width / 2, ft.height / 2).r, 0.97f, "판은 흰색 — 색은 틴트가 쥔다(결정 817)");
            Sprite ring = SurfaceArt.Bake("sr_floor_ring", 2.5f);
            Texture2D rt = ring.texture;
            Assert.AreEqual(0.5f, rt.GetPixel((int)(rt.width * (0.5f + 0.3175f)), rt.height / 2).a, 0.06f, "링 63.5% 자리 알파 = 정본 .5");
            Assert.Less(rt.GetPixel((int)(rt.width * (0.5f + 0.25f)), rt.height / 2).a, 0.03f, "링 안쪽(50%)은 비어 있다");
            Assert.Less(rt.GetPixel((int)(rt.width * (0.5f + 0.36f)), rt.height / 2).a, 0.03f, "링 바깥(72%)은 비어 있다");
            Sprite fl = SurfaceArt.Bake("sr_flash", 1f, 1, new Color(1f, 0f, 0f, 1f));
            Texture2D lt = fl.texture;
            Color lc = lt.GetPixel(lt.width / 2, lt.height / 2), l26 = lt.GetPixel((int)(lt.width * (0.5f + 0.13f)), lt.height / 2);
            Assert.Greater(lc.g, 0.9f, "섬광 가운데는 흰색");
            Assert.AreEqual(1f, lc.a, 0.03f, "섬광 가운데 알파 1");
            Assert.Greater(l26.r, 0.9f); Assert.Less(l26.g, 0.15f, "26% 자리는 준 등급색(빨강)");
            Assert.Less(lt.GetPixel((int)(lt.width * (0.5f + 0.4f)), lt.height / 2).a, 0.03f, "72% 뒤는 사라진다");

            // ⓑ 실물 — 홀드백 한 판(조연 셋 + 최고 하나)이라 착지 섬광이 뜬다
            yield return Boot();
            float t0 = Time.realtimeSinceStartup;
            while (!(SkillPetSheet.Instance != null && PetSkillHost.Ready) && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다");
            var list = new System.Collections.Generic.List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = "가" },
                new SkillSummonResultView.Entry { Key = "sk:b", IconKey = "sk_fireball", Rarity = "common", Name = "나" },
                new SkillSummonResultView.Entry { Key = "sk:c", IconKey = "sk_fireball", Rarity = "rare", Name = "다" },
                new SkillSummonResultView.Entry { Key = "sk:d", IconKey = "sk_fireball", Rarity = "ultimate", Name = "라" },
            };
            SkillSummonResultView v = SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", list, "ultimate", null);
            Assert.IsNotNull(v, "결과 연출 팝업이 서지 않았다");
            yield return null;
            Transform floor = FindDeep(v.transform, "sr-floor");
            Assert.IsNotNull(floor, "소환진(sr-floor)");
            Image fi = floor.GetComponent<Image>();
            Assert.IsNotNull(fi.sprite, "소환진은 구운 판이다");
            Assert.IsTrue(fi.sprite.texture.name.StartsWith("sf-sr_floor_fill", System.StringComparison.Ordinal), "바닥 면 판 sr_floor_fill: " + fi.sprite.texture.name);
            Color ffill = SummonFxStyle.C("floor_fill");
            Assert.AreEqual(ffill.b, fi.color.b, 0.01f, "틴트는 종전 floor_fill 의 rgb");
            Assert.AreEqual(1f, fi.color.a, 0.01f, "틴트 알파 1 — 알파 .2 는 판이 쥔다");
            Transform ringT = floor.Find("sr-floor-ring");
            Assert.IsNotNull(ringT, "링 겹(sr-floor-ring)");
            Image ri = ringT.GetComponent<Image>();
            Assert.IsTrue(ri.sprite != null && ri.sprite.texture.name.StartsWith("sf-sr_floor_ring", System.StringComparison.Ordinal), "링 판 sr_floor_ring");
            Assert.Less(ringT.GetSiblingIndex(), floor.Find("sr-floor-ticks").GetSiblingIndex(), "링(::before)은 눈금(::after) 아래");
            Sprite preFill = fi.sprite, preRing = ri.sprite;

            // ⓒ done
            float t = 0f;
            while (!v.Done && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(v.Done, "done");
            Color rc = PetSkillStyle.Rarity(PetSkillHost.Instance.Data.Defs, "ultimate");
            Assert.AreNotEqual(preFill, fi.sprite, "done 뒤 바닥 면 판이 .26 짜리로 갈아탔다");
            Assert.AreEqual(SummonFxStyle.L("floor_done_a"), fi.sprite.texture.GetPixel(fi.sprite.texture.width / 2, fi.sprite.texture.height / 2).a, 0.03f, "done 판 가운데 알파 = floor_done_a");
            Assert.AreEqual(rc.r, fi.color.r, 0.02f, "done 틴트는 등급색");
            Assert.AreNotEqual(preRing, ri.sprite, "done 뒤 링 판이 .85 짜리로 갈아탔다");
            Assert.AreNotEqual(ffill.b, ri.color.b, "done 링 틴트는 등급 파생색");
            Transform flashT = FindDeep(v.transform, "sr-flash");
            Assert.IsNotNull(flashT, "섬광 판");
            Image fim = flashT.GetComponent<Image>();
            Assert.IsNotNull(fim.sprite, "섬광은 구운 판이다(종전 색 한 칸)");
            Assert.IsTrue(fim.sprite.texture.name.StartsWith("sf-sr_flash", System.StringComparison.Ordinal), "섬광 판 sr_flash");
            Assert.IsNotNull(flashT.parent.GetComponent<RectMask2D>(), "상자 밖은 클립이 자른다");
            RectTransform frt = (RectTransform)flashT;
            Assert.AreEqual(frt.rect.width, frt.rect.height, 1f, "farthest-corner 원 = 정사각 판");
            Assert.GreaterOrEqual(frt.rect.width, ((RectTransform)flashT.parent).rect.width - 1f, "지름 ≥ 상자 폭(가장 먼 귀까지)");
            Assert.AreEqual(1f, fim.color.r, 1e-3f, "섬광 틴트는 흰색 — 등급색은 26% 정지점");
            v.Close();
            yield return null;
        }

        /// <summary>
        /// T178 33회차 — 소환 결과 **[확인] 금색 버튼**(정본 **7139** `.sr-ok { linear-gradient(#ffe89a 0%, #ffc93c 46%, #e8a015 100%) }` = 8748 쌍둥이)과
        /// **NEW 배지**(**6980** `.sr-new { linear-gradient(#ff6a5e, #e02114) }`). 둘 다 정지점이 전부 불투명이라 바탕 없이 굽는다.
        /// 자는 ⓐ 구운 화소의 위·가운데·아래가 정본 세 색(배지는 두 색)이고 불투명한가 ⓑ 실물 두 면에 Mask + 구운 겹이 서는가 — 종전엔 팔레트 단색이었다.
        /// </summary>
        [UnityTest]
        public IEnumerator 소환_결과_확인_버튼과_NEW_배지는_정본_세_색_두_색의_불투명_세로_겹이다()
        {
            // ⓐ [확인] — 위 #ffe89a · 46% #ffc93c · 아래 #e8a015
            Sprite ok = SurfaceArt.Bake("sr_ok", 11f / 3f, 100f);
            Assert.IsNotNull(ok, "[확인] 면을 굽는다");
            Texture2D ot = ok.texture;
            Color top = ot.GetPixel(ot.width / 2, ot.height - 1), bot = ot.GetPixel(ot.width / 2, 0);
            Color mid = ot.GetPixel(ot.width / 2, ot.height - 1 - Mathf.RoundToInt(0.46f * (ot.height - 1)));
            float tol = 3f / 255f;
            Assert.AreEqual(1f, top.a, 1e-3f, "불투명(정지점 셋 다 알파 1)");
            Assert.AreEqual(232f / 255f, top.g, tol, "위 G = #ffe89a");
            Assert.AreEqual(154f / 255f, top.b, tol, "위 B = #ffe89a");
            Assert.AreEqual(201f / 255f, mid.g, 6f / 255f, "46% 자리 G ≈ #ffc93c");
            Assert.AreEqual(232f / 255f, bot.r, tol, "아래 R = #e8a015");
            Assert.AreEqual(160f / 255f, bot.g, tol, "아래 G = #e8a015");
            Assert.AreEqual(21f / 255f, bot.b, tol, "아래 B = #e8a015");
            // ⓐ NEW — 위 #ff6a5e · 아래 #e02114
            Sprite nw = SurfaceArt.Bake("sr_new", 2f, 30f);
            Assert.IsNotNull(nw, "NEW 배지 면을 굽는다");
            Texture2D nt = nw.texture;
            Color ntop = nt.GetPixel(nt.width / 2, nt.height - 1), nbot = nt.GetPixel(nt.width / 2, 0);
            Assert.AreEqual(1f, ntop.a, 1e-3f, "배지도 불투명");
            Assert.AreEqual(106f / 255f, ntop.g, tol, "위 G = #ff6a5e");
            Assert.AreEqual(94f / 255f, ntop.b, tol, "위 B = #ff6a5e");
            Assert.AreEqual(224f / 255f, nbot.r, tol, "아래 R = #e02114");
            Assert.AreEqual(33f / 255f, nbot.g, tol, "아래 G = #e02114");

            // ⓑ 실물
            yield return Boot();
            float t0 = Time.realtimeSinceStartup;
            while (!(SkillPetSheet.Instance != null && PetSkillHost.Ready) && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다");
            var list = new System.Collections.Generic.List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = "가", IsNew = true },
            };
            SkillSummonResultView v = SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", list, "common", null);
            Assert.IsNotNull(v, "결과 연출 팝업이 서지 않았다");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Transform okb = FindDeep(v.transform, "sr-ok");
            Assert.IsNotNull(okb, "[확인] 버튼(sr-ok)");
            Transform okTop = okb.Find("skin/top");
            Assert.IsNotNull(okTop, "[확인] 둥근 면(skin/top)");
            Assert.IsNotNull(okTop.GetComponent<Mask>(), "[확인] 면에 Mask");
            Transform og = okTop.Find("bg-grad");
            Assert.IsNotNull(og, "[확인] 겹(bg-grad)이 선다(정본 7139)");
            Image ogi = og.GetComponent<Image>();
            Assert.IsNotNull(ogi.sprite, "[확인] 겹은 구운 그림이다 — 팔레트 단색이 아니다");
            Assert.AreEqual(1f, ogi.color.r, 1e-3f, "그림 위 색은 흰색");
            Transform nb = FindDeep(v.transform, "sr-new");
            Assert.IsNotNull(nb, "NEW 배지(sr-new · IsNew 셀)");
            Transform nface = nb.Find("face");
            Assert.IsNotNull(nface, "배지 둥근 면(face)");
            Assert.IsNotNull(nface.GetComponent<Mask>(), "배지 면에 Mask");
            Transform ng = nface.Find("bg-grad");
            Assert.IsNotNull(ng, "배지 겹(bg-grad)이 선다(정본 6980)");
            Assert.IsNotNull(ng.GetComponent<Image>().sprite, "배지 겹은 구운 그림이다");
            v.Close();
            yield return null;
        }

        /// <summary>
        /// T178 34회차 — 소환 결과의 «투명 품는 방사» 둘. ⓐ 정본 **5728** `.sr-wrap::after { radial-gradient(122% 84% at 50% 44%, rgba(0,0,0,0) 40%, rgba(0,0,0,.58) 100%) }`
        /// 상시 비네트(표 `sr_wrap_vig`) — 가운데는 비고 귀퉁이가 검게 눌린다. ⓑ 정본 **5755** `.sr-body::before { width: 124%; height: calc(100% + 2rem);
        /// radial-gradient(58% 48% at 50% 50%, rgba(4,7,20,.58) 0%, rgba(4,7,20,.26) 56%, rgba(4,7,20,0) 100%) }` 그리드 뒤 받침(표 `sr_body_plate`) —
        /// 가운데 .58 · 56% 자리 .26 · 끝은 사라진다. 실물: 비네트는 wrap 에서 충전 비네트(`sr-vig`) 바로 뒤 · 받침은 몸의 광선(`sr-rays`) 바로 다음 형제로 몸보다 큰 상자.
        /// </summary>
        [UnityTest]
        public IEnumerator 소환_결과_상시_비네트와_그리드_뒤_받침은_투명을_품은_방사_두_판이다()
        {
            // ⓐ 판 — 비네트(상자 비율 9:16)
            Sprite vg = SurfaceArt.Bake("sr_wrap_vig", 9f / 16f);
            Texture2D vt = vg.texture;
            Assert.Less(vt.GetPixel(vt.width / 2, (int)(vt.height * 0.56f)).a, 0.02f, "비네트 가운데(44% 자리)는 비어 있다(40% 안)");
            Color corner = vt.GetPixel(0, vt.height - 1);   // CSS 위-왼 귀퉁이(텍스처는 아래가 0행)
            Assert.Greater(corner.a, 0.15f, "귀퉁이는 눌린다(타원 밖 40%→100% 사이 · 실측 " + corner.a.ToString("0.00") + ")");
            Assert.Less(corner.a, 0.45f, "귀퉁이는 100% 정지점(.58) 안쪽이다 — 타원이 상자보다 크다(122%×84%)");
            Assert.Less(corner.r + corner.g + corner.b, 0.05f, "판은 검정");
            // 받침(상자 비율 124% : (100%+2rem) ≈ 넓적)
            float wf = SurfaceArt.Num("sr_body_plate", "box_w_f"), hx = SurfaceArt.Num("sr_body_plate", "box_h_extra_rem");
            Assert.AreEqual(1.24f, wf, 1e-4f, "정본 5757 width 124%"); Assert.AreEqual(2f, hx, 1e-4f, "정본 5757 height +2rem");
            Sprite pl = SurfaceArt.Bake("sr_body_plate", 1.6f);
            Texture2D pt = pl.texture;
            Color pc = pt.GetPixel(pt.width / 2, pt.height / 2);
            Assert.AreEqual(0.58f, pc.a, 0.03f, "받침 가운데 알파 = 정본 .58");
            Assert.Less(pc.r, 0.05f, "받침 색은 (4,7,20) 남색");
            Assert.AreEqual(0.26f, pt.GetPixel((int)(pt.width * (0.5f + 0.56f * 0.58f)), pt.height / 2).a, 0.04f, "56% 자리(rx .58 의 56%) 알파 = 정본 .26");
            Color edge = pt.GetPixel(pt.width - 1, pt.height / 2);
            Assert.Less(edge.a, 0.13f, "상자 오른끝(반지름의 86%)은 거의 사라진다");
            Assert.Greater(edge.a, 0.02f, "그러나 0 은 100% 정지점(상자 밖)에서다");

            // ⓑ 실물
            yield return Boot();
            float t0 = Time.realtimeSinceStartup;
            while (!(SkillPetSheet.Instance != null && PetSkillHost.Ready) && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다");
            var list = new System.Collections.Generic.List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = "가" },
                new SkillSummonResultView.Entry { Key = "sk:b", IconKey = "sk_fireball", Rarity = "common", Name = "나" },
                new SkillSummonResultView.Entry { Key = "sk:c", IconKey = "sk_fireball", Rarity = "rare", Name = "다" },
                new SkillSummonResultView.Entry { Key = "sk:d", IconKey = "sk_fireball", Rarity = "ultimate", Name = "라" },
            };
            SkillSummonResultView v = SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", list, "ultimate", null);
            Assert.IsNotNull(v, "결과 연출 팝업이 서지 않았다");
            yield return null;
            Transform vig = FindDeep(v.transform, "sr-vig"), vigS = FindDeep(v.transform, "sr-vig-static");
            Assert.IsNotNull(vig, "충전 비네트(sr-vig)"); Assert.IsNotNull(vigS, "상시 비네트(sr-vig-static · 5728)");
            Assert.AreSame(vig.parent, vigS.parent, "둘은 wrap 의 형제");
            Assert.AreEqual(vig.GetSiblingIndex() + 1, vigS.GetSiblingIndex(), "::after 는 ::before 바로 뒤(위)");
            Image vsi = vigS.GetComponent<Image>();
            Assert.IsTrue(vsi.sprite != null && vsi.sprite.texture.name.StartsWith("sf-sr_wrap_vig", System.StringComparison.Ordinal), "상시 비네트 판 sr_wrap_vig: " + (vsi.sprite == null ? "null" : vsi.sprite.texture.name));
            RectTransform vsr = (RectTransform)vigS;
            Assert.AreEqual(0f, vsr.anchorMin.x, 1e-4f); Assert.AreEqual(1f, vsr.anchorMax.y, 1e-4f, "wrap 을 꽉 채운다(inset 0)");
            Assert.AreEqual(1f, vsi.color.a, 1e-3f, "상시 — 알파를 흔들지 않는다(충전 비네트와 다른 판)");
            Transform body = FindDeep(v.transform, "sr-body");
            Assert.IsNotNull(body, "몸(sr-body)");
            Transform plate = body.Find("sr-body-plate");
            Assert.IsNotNull(plate, "그리드 뒤 받침(sr-body-plate · 5755)");
            Image pli = plate.GetComponent<Image>();
            Assert.IsTrue(pli.sprite != null && pli.sprite.texture.name.StartsWith("sf-sr_body_plate", System.StringComparison.Ordinal), "받침 판 sr_body_plate");
            Transform rays = body.Find("sr-rays"), grid = body.Find("sr-grid");
            Assert.IsNotNull(grid, "그리드");
            if (rays != null) Assert.AreEqual(rays.GetSiblingIndex() + 1, plate.GetSiblingIndex(), "받침(z 5)은 광선(z 0) 바로 다음");
            else Assert.AreEqual(0, plate.GetSiblingIndex(), "광선이 없으면 받침이 첫 형제");
            Assert.Less(plate.GetSiblingIndex(), grid.GetSiblingIndex(), "받침(5) < 그리드(40)");
            RectTransform br = (RectTransform)body, pr = (RectTransform)plate;
            Assert.AreEqual(br.rect.width * wf, pr.sizeDelta.x, 0.5f, "받침 폭 = 몸 × 124%");
            Assert.AreEqual(br.rect.height + PetSkillStyle.Rem(hx), pr.sizeDelta.y, 0.5f, "받침 높이 = 몸 + 2rem");
            Assert.AreEqual(0.5f, pr.anchorMin.x, 1e-4f); Assert.AreEqual(0.5f, pr.pivot.y, 1e-4f, "가운데 정렬(translate −50%)");
            v.Close();
            yield return null;
        }

        /// <summary>
        /// T178 35회차 — 상단바 밴드(정본 **7900** `#topbar` 겹 둘: 위 1px 림라이트 + 아래로 가는 그늘 · 탭바 4회차의 뒤집은 짝)와
        /// 재화 알약 홈(정본 **7924** `.currency-pills .pill` · 위 검정 .30 → 46% 투명 → 아래 흰 .10 · 둥근 면이라 Mask).
        /// </summary>
        [UnityTest]
        public IEnumerator 상단바_밴드와_재화_알약에_겹이_선다()
        {
            yield return Boot();
            // ⓐ 표
            Assert.AreEqual(180f, SurfaceArt.Angle("topbar_shade"), 1e-4f, "7902 180deg(위에서 아래로)");
            Assert.AreEqual(180f, SurfaceArt.Angle("topbar_rim"), 1e-4f, "7901 180deg — 탭바 림(0deg)을 뒤집었다");
            Assert.IsTrue(SurfaceArt.PxOffsets("topbar_rim"), "림은 1 CSS px 단위");
            Color[] sc; float[] so;
            SurfaceArt.Stops("topbar_shade", out sc, out so);
            Assert.AreEqual(4, sc.Length); Assert.AreEqual(0.4f, so[1], 1e-4f, "40%"); Assert.AreEqual(0.68f, so[2], 1e-4f, "68%");
            Assert.AreEqual(0.09f, sc[0].a, 1e-4f, "위 흰 .09"); Assert.AreEqual(0.26f, sc[3].a, 1e-4f, "아래 검 .26");
            SurfaceArt.Stops("pill_groove", out sc, out so);
            Assert.AreEqual(3, sc.Length); Assert.AreEqual(0.46f, so[1], 1e-4f, "46%");
            Assert.AreEqual(0.3f, sc[0].a, 1e-4f, "위 검정 .30"); Assert.AreEqual(0f, sc[1].a, 1e-4f, "가운데 투명"); Assert.AreEqual(0.1f, sc[2].a, 1e-4f, "아래 흰 .10");
            Assert.AreEqual(180f, SurfaceArt.Angle("pill_groove"), 1e-4f);

            // ⓑ 실물 — 상단바: bg 다음 · 겹 → 림 순 · 테(line) 아래 · 꽉 채움 · 불투명(바탕을 미리 합성)
            Transform bar = null;
            foreach (RectTransform rt in UiRoot.Instance.App.GetComponentsInChildren<RectTransform>(true)) if (rt.name == "topbar") { bar = rt; break; }
            Assert.IsNotNull(bar, "상단바(topbar)");
            Transform grad = bar.Find("topbar-grad"), rim = bar.Find("topbar-rim"), bg = bar.Find("bg"), line = bar.Find("line");
            Assert.IsNotNull(grad, "상단바 밴드 겹(topbar-grad)"); Assert.IsNotNull(rim, "상단바 위 림(topbar-rim)");
            Assert.Less(bg.GetSiblingIndex(), grad.GetSiblingIndex(), "바탕 위");
            Assert.Less(grad.GetSiblingIndex(), rim.GetSiblingIndex(), "밴드 → 림 순");
            Assert.Less(rim.GetSiblingIndex(), line.GetSiblingIndex(), "테(border-bottom) 아래");
            Image gi = grad.GetComponent<Image>();
            Assert.IsNotNull(gi.sprite, "구운 그림"); Assert.IsFalse(gi.raycastTarget, "클릭 안 먹음");
            Assert.AreEqual(Vector2.zero, gi.rectTransform.offsetMin); Assert.AreEqual(Vector2.zero, gi.rectTransform.offsetMax);
            Color32[] px = gi.sprite.texture.GetPixels32(); int W = gi.sprite.texture.width, H = gi.sprite.texture.height;
            Color32 top = px[(H - 1) * W + W / 2], bot = px[W / 2];
            Assert.Greater(top.r, bot.r, "위는 흰 기 · 아래는 검(아래로 가는 그늘)");
            Assert.AreEqual(255, top.a, "바탕(topbar_bg)을 알아 미리 합성했다"); Assert.AreEqual(255, bot.a);
            Image ri = rim.GetComponent<Image>();
            Assert.IsNotNull(ri.sprite, "림도 구운 그림");
            Color32[] rp = ri.sprite.texture.GetPixels32(); int RW = ri.sprite.texture.width, RH = ri.sprite.texture.height;
            Assert.Greater(rp[(RH - 1) * RW + RW / 2].r, rp[(RH / 2) * RW + RW / 2].r, "림은 **위** 줄이 밝다(180deg · 탭바는 아래 줄)");

            // 재화 알약: 둥근 면(bg)에 Mask + bg-grad · 아이콘·숫자는 형제 그대로
            Transform pill = bar.Find("pill-coin");   // T414 — 상단바 안에서만 찾는다(오프라인 카드도 같은 이름을 짓는다)
            Assert.IsNotNull(pill, "코인 알약(pill-coin · 상단바 자식)");
            Transform pbg = pill.Find("bg");
            Assert.IsNotNull(pbg, "알약 면(bg)");
            Assert.IsNotNull(pbg.GetComponent<Mask>(), "면에 Mask — 겹이 알약 밖으로 안 샌다");
            Transform groove = pbg.Find("bg-grad");
            Assert.IsNotNull(groove, "알약 홈 겹(bg-grad · 7924)");
            Image gr = groove.GetComponent<Image>();
            Assert.IsNotNull(gr.sprite, "구운 그림");
            Color32[] gp = gr.sprite.texture.GetPixels32(); int GW = gr.sprite.texture.width, GH = gr.sprite.texture.height;
            Assert.Less(gp[(GH - 1) * GW + GW / 2].r, gp[GW / 2].r, "홈: 위(검 .30)가 아래(흰 .10)보다 어둡다");
            Assert.AreEqual(255, gp[GW / 2].a, "바탕(card_bg)을 미리 합성했다");
            Assert.IsNotNull(pill.Find("ico"), "아이콘 그대로"); Assert.IsNotNull(pill.Find("value"), "숫자 그대로");
            Assert.Less(pbg.GetSiblingIndex(), pill.Find("ico").GetSiblingIndex(), "면(홈 포함)은 아이콘 아래");
        }

        /// <summary>
        /// T178 36회차 — 시대 막대의 겹 셋. ⓐ 정본 **8526** `.fi-age-bar[data-age], .af-age-bar[data-age]` 면 위 톤 램프(흰 .42 → .14 44% → .03 48% → 검 .10 52% → .30 100% · 8250 은 덮인다 · 시대색 위)
        /// ⓑ **4845** `.af-age-bar::after` 광택(흰 .3 → 검 .26) ⓒ **5125** `.fi-age-bar::after` 광택(흰 .13 → 검 .15 · 한 단 옅다).
        /// 실물: 램프는 둥근 면(bar/face) 안 Mask 로 무늬(형제 1)·글자 **아래** · 광택은 막대의 **맨 마지막** 형제(글자·체크 위 · 정본 `::after` 가 위치 지정이라 흐름 안 내용 위에 칠해진다).
        /// </summary>
        [UnityTest]
        public IEnumerator 시대_막대에_톤_램프와_광택_겹이_선다()
        {
            yield return Boot();
            // ⓐ 표
            Color[] sc; float[] so;
            SurfaceArt.Stops("age_bar_ramp", out sc, out so);
            Assert.AreEqual(5, sc.Length, "8526 정지점 다섯"); Assert.AreEqual(180f, SurfaceArt.Angle("age_bar_ramp"), 1e-4f);
            Assert.AreEqual(0.44f, so[1], 1e-4f); Assert.AreEqual(0.48f, so[2], 1e-4f); Assert.AreEqual(0.52f, so[3], 1e-4f, "«위 48% 를 밝히고 52% 에서 어두운 반쪽으로»");
            Assert.AreEqual(0.42f, sc[0].a, 1e-4f, "위 흰 .42(8250 의 .34 가 아니다 — 마지막 선언)"); Assert.AreEqual(0.3f, sc[4].a, 1e-4f, "아래 검 .30");
            SurfaceArt.Stops("af_age_bar_gloss", out sc, out so);
            Assert.AreEqual(0.3f, sc[0].a, 1e-4f, "4846 흰 .3"); Assert.AreEqual(0.44f, so[1], 1e-4f); Assert.AreEqual(0.26f, sc[3].a, 1e-4f, "4847 검 .26");
            SurfaceArt.Stops("fi_age_bar_gloss", out sc, out so);
            Assert.AreEqual(0.13f, sc[0].a, 1e-4f, "5127 흰 .13"); Assert.AreEqual(0.42f, so[1], 1e-4f); Assert.AreEqual(0.15f, sc[3].a, 1e-4f, "5128 검 .15");
            Assert.Less(0.13f, 0.3f, "확률 정보 막대 광택은 자동 제련보다 옅다(«원본은 평면 색면»)");

            // ⓑ 실물 — 자동 제련 막대(2-10 뒤 해금)
            float t0 = Time.realtimeSinceStartup;
            while (!ForgeHost.Ready && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            ForgeHost h = ForgeHost.Instance;
            Assert.IsNotNull(h, "ForgeHost");
            h.S.BestChapter = 3; h.S.BestStage = 1; h.Pull();
            Assert.IsTrue(h.AutoForgeUnlocked, "2-10 을 넘겨 자동 제련이 해금됐다");
            ForgeAutoPopup.Open(h);
            yield return null;
            Popup auto = h.Meta.Popups.Find(ForgeAutoPopup.Name);
            Assert.IsNotNull(auto, "자동 제련 팝업");
            int afBars = 0;
            foreach (string age in h.Defs.Ages)
            {
                Transform bar = FindDeep(auto.Root, "af-age-" + age);
                if (bar == null) continue;
                afBars++;
                Transform face = bar.Find("bar/face");
                Assert.IsNotNull(face, age + ": 둥근 면(bar/face)");
                Assert.IsNotNull(face.GetComponent<Mask>(), age + ": 면에 Mask — 램프가 모서리 밖으로 안 샌다");
                Transform ramp = face.Find("bg-grad");
                Assert.IsNotNull(ramp, age + ": 톤 램프(bg-grad · 8526)");
                Image ri = ramp.GetComponent<Image>();
                Assert.IsTrue(ri.sprite != null && ri.sprite.texture.name.StartsWith("sf-age_bar_ramp", System.StringComparison.Ordinal), age + ": 램프 판 age_bar_ramp");
                Color32[] rp = ri.sprite.texture.GetPixels32(); int RW = ri.sprite.texture.width, RH = ri.sprite.texture.height;
                Assert.AreEqual(255, rp[RW / 2].a, age + ": 바탕(시대색)을 미리 합성했다 — 불투명");
                Assert.Greater(rp[(RH - 1) * RW + RW / 2].r + rp[(RH - 1) * RW + RW / 2].g + rp[(RH - 1) * RW + RW / 2].b, rp[RW / 2].r + rp[RW / 2].g + rp[RW / 2].b, age + ": 위(흰 .42)가 아래(검 .30)보다 밝다");
                Transform gloss = bar.Find("gloss");
                Assert.IsNotNull(gloss, age + ": 광택(gloss · 4845)");
                Assert.AreEqual(bar.childCount - 1, gloss.GetSiblingIndex(), age + ": 광택은 막대의 맨 마지막 형제(글자·체크 위)");
                Image gi = gloss.GetComponent<Image>();
                Assert.IsTrue(gi.sprite != null && gi.sprite.texture.name.StartsWith("sf-af_age_bar_gloss", System.StringComparison.Ordinal), age + ": 광택 판 af_age_bar_gloss");
                Assert.IsFalse(gi.raycastTarget, "클릭 안 먹음");
                Assert.Less(gi.sprite.texture.GetPixels32()[gi.sprite.texture.width / 2].a, 255, age + ": 광택은 투명 겹(바탕 없음)");
                Assert.AreEqual(0, bar.Find("bar").GetSiblingIndex(), age + ": 틀은 형제 0 그대로(무늬 층 형제 1 · AgePatternTests)");
            }
            Assert.Greater(afBars, 0, "자동 제련 막대를 못 찾았다");
            h.Meta.Popups.Hide(ForgeAutoPopup.Name);
            yield return null;
            // ⓒ 실물 — 확률 정보 막대(fi)
            ForgeInfoPopup.Open(h);
            yield return null;
            Popup info = h.Meta.Popups.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(info, "대장간 정보 팝업");
            int fiBars = 0;
            foreach (string age in h.Defs.Ages)
            {
                Transform bar = FindDeep(info.Root, "age-" + age);
                if (bar == null) continue;
                fiBars++;
                Transform gloss = bar.Find("gloss");
                Assert.IsNotNull(gloss, age + ": 광택(gloss · 5125)");
                Image gi = gloss.GetComponent<Image>();
                Assert.IsTrue(gi.sprite != null && gi.sprite.texture.name.StartsWith("sf-fi_age_bar_gloss", System.StringComparison.Ordinal), age + ": 광택 판 fi_age_bar_gloss(자동 제련과 다른 판)");
                Assert.IsNotNull(bar.Find("bar/face/bg-grad"), age + ": 램프도 같은 8526");
            }
            Assert.Greater(fiBars, 0, "확률 정보 막대를 못 찾았다");
            h.Meta.Popups.Hide(ForgeInfoPopup.Name);
            yield return null;
        }

        /// <summary>
        /// T178 37회차 — 은색 버튼 면의 세로 램프(정본 **5209** `.summon-btn` · 5260 `.skd-btn.silver` · 5274 `.btn.silver` = #e3e3e3 → #c2c2c2 · 5268 비활성 #d9d9d9 → #bdbdbd ·
        /// 5641 승천 소환 #4caf50 → #2e7d32)와 리그 점수 알약 겹(정본 **7881** · 흰 .16 → 46% 투명 → 검 .30 · 바탕 #020203).
        /// 실물: 소환 버튼 `skin/top`(둥근 면)에 Mask + `bg-grad`(불투명 · 위가 아래보다 밝다 · 아래턱 skin 띠는 그대로) · 리그 행 `score/bg` 에 Mask + `bg-grad`(불투명 · 위 밝고 아래 검).
        /// </summary>
        [UnityTest]
        public IEnumerator 은색_버튼_면과_리그_점수_알약에_겹이_선다()
        {
            yield return Boot();
            // ⓐ 표
            Color[] sc; float[] so;
            SurfaceArt.Stops("btn_silver", out sc, out so);
            Assert.AreEqual(2, sc.Length); Assert.AreEqual(227f / 255f, sc[0].r, 1e-3f, "위 #e3e3e3"); Assert.AreEqual(194f / 255f, sc[1].r, 1e-3f, "아래 #c2c2c2"); Assert.AreEqual(1f, sc[0].a, 1e-4f, "불투명");
            SurfaceArt.Stops("btn_silver_disabled", out sc, out so);
            Assert.AreEqual(217f / 255f, sc[0].r, 1e-3f, "5268 위 #d9d9d9"); Assert.AreEqual(189f / 255f, sc[1].r, 1e-3f, "아래 #bdbdbd");
            SurfaceArt.Stops("btn_ascend", out sc, out so);
            Assert.AreEqual(76f / 255f, sc[0].r, 1e-3f, "5641 위 #4caf50"); Assert.AreEqual(125f / 255f, sc[1].g, 1e-3f, "아래 #2e7d32");
            SurfaceArt.Stops("league_score_skin", out sc, out so);
            Assert.AreEqual(3, sc.Length); Assert.AreEqual(0.16f, sc[0].a, 1e-4f, "7881 흰 .16"); Assert.AreEqual(0.46f, so[1], 1e-4f, "46%"); Assert.AreEqual(0.3f, sc[2].a, 1e-4f, "검 .30");
            Assert.AreEqual(180f, SurfaceArt.Angle("btn_silver"), 1e-4f); Assert.AreEqual(180f, SurfaceArt.Angle("league_score_skin"), 1e-4f);

            // ⓑ 실물 — 스킬 패널의 소환 버튼(승천 아님 = SkillPanel.SummonBtn 의 PaperButton Silver). 런 1272: 펫 패널은 탭을 열기 전엔 안 서서 null 이었다 —
            //   탭바 자와 같은 길로 소환 탭·스킬 하위판을 먼저 연다.
            float t0 = Time.realtimeSinceStartup;
            while (!(SkillPetSheet.Instance != null && PetSkillHost.Ready) && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            Assert.IsTrue(PetSkillHost.Ready, "소환 호스트가 20초 안에 안 섰다");
            TabBar tb0 = UiRoot.Instance.TabBar;
            if (tb0.ActiveTab != "summon") tb0.OnTab("summon");
            SkillPetSheet.Instance.Switch(SkillPetSheet.SubSkills);
            yield return null;
            Button sb = SkillPetSheet.Instance.Skills.SummonButton;
            Assert.IsNotNull(sb, "스킬 소환 버튼(소환 탭 · 스킬 하위판)");
            Transform top = sb.transform.Find("skin/top");
            Assert.IsNotNull(top, "둥근 면(skin/top)");
            // 런 1289: 소환 버튼은 42회차부터 겹이 없다(8661 단색) — Mask 는 겹이 있을 때만 요구한다(아래 승천 갈래).
            // T178 42회차 — 정본 **8661** `.btn.btn.summon-btn.summon-btn:not(.ascend-ready) { background: #a3a3a3 }` 이 5209 램프를 단색으로 끈다(0-4-0 · 문서 뒤).
            //   37회차가 여기 세운 램프는 걷었다 — 소환 버튼(은색 갈래)엔 bg-grad 가 없고 면은 #a3a3a3 한 칸. 승천 갈래(ascend-ready)만 5641 초록 램프.
            Transform sg = top.Find("bg-grad");
            if (sg != null)
            {
                Image sgi = sg.GetComponent<Image>();
                Assert.IsTrue(sgi.sprite != null && sgi.sprite.texture.name.StartsWith("sf-btn_ascend", System.StringComparison.Ordinal), "소환 버튼에 남은 겹은 승천 램프(btn_ascend)뿐이어야 한다: " + (sgi.sprite == null ? "null" : sgi.sprite.texture.name));
                Assert.IsNotNull(top.GetComponent<Mask>(), "겹이 있으면 면에 Mask — 램프가 모서리 밖으로 안 샌다");
            }
            else
            {
                Color32 fc = top.GetComponent<Image>().color;
                Assert.IsTrue(fc.r == 0xa3 && fc.g == 0xa3 && fc.b == 0xa3, "소환 버튼 면 = 8661 #a3a3a3 한 칸: " + fc);
            }
            Assert.IsNotNull(sb.transform.Find("skin/line"), "검정 테 그대로");
            Assert.IsNull(sb.transform.Find("skin/bg-grad"), "램프는 face 안 — 아래턱(skin 띠)을 덮지 않는다");

            // ⓒ 실물 — 리그 행 점수 알약
            t0 = Time.realtimeSinceStartup;
            while (!MetaHost.Ready && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            MetaHost mh = MetaHost.Instance;
            Assert.IsNotNull(mh, "MetaHost");
            LeagueSheet.Open(mh);
            yield return null;
            Popup leaguePop = PopupLayer.Instance.Find(LeagueSheet.Name);   // 런 1274: 리그 시트는 팝업 층(PopupLayer)에 선다 — 아래 시트(UiRoot.Sheet)가 아니다(BoxBorderSitesTests 와 같은 길)
            Assert.IsNotNull(leaguePop, "리그 시트(팝업 층)");
            Transform scoreBg = null;
            foreach (RectTransform rt in leaguePop.Root.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "score" && rt.parent != null && rt.parent.name.StartsWith("row-", System.StringComparison.Ordinal) && rt.Find("bg") != null) { scoreBg = rt.Find("bg"); break; }
            Assert.IsNotNull(scoreBg, "리그 행 점수 알약(score/bg)");
            Assert.IsNotNull(scoreBg.GetComponent<Mask>(), "알약 면에 Mask");
            Transform lg = scoreBg.Find("bg-grad");
            Assert.IsNotNull(lg, "점수 알약 겹(bg-grad · 7881)");
            Image lgi = lg.GetComponent<Image>();
            Assert.IsTrue(lgi.sprite != null && lgi.sprite.texture.name.StartsWith("sf-league_score_skin", System.StringComparison.Ordinal), "겹 판 league_score_skin");
            Color32[] lp = lgi.sprite.texture.GetPixels32(); int LW = lgi.sprite.texture.width, LH = lgi.sprite.texture.height;
            Assert.AreEqual(255, lp[LW / 2].a, "바탕(league_score)을 미리 합성했다 — 불투명");
            Assert.Greater(lp[(LH - 1) * LW + LW / 2].r, lp[LW / 2].r, "위(흰 .16)가 아래(검 .30)보다 밝다");
            LeagueSheet.Close(mh);
            yield return null;
        }

        /// <summary>T178 39회차 — 소환 게이지(`PetSkillKit.Gauge`)의 트랙·채움은 **색 한 칸**이다. 38회차가 정본 7992(트랙 홈)·7953(채움 광택)을 세웠는데
        /// 정본 8798/8803(aaa-skin ⓖ)이 둘 다 `background-image: none` 으로 끈다 — 마지막 선언이 이긴다. 업그레이드 막대(`upg-fill`)·기술 노드 채움도 같은 줄이라
        /// 같은 회차에 걷었다(그 둘은 자 표의 «—» 행과 코드 검색이 본다). ㉳ 분절 눈금(8812)은 다음 회차.</summary>
        [UnityTest]
        public IEnumerator 소환_게이지_트랙과_채움은_정본_최종_규칙대로_색_한_칸이다()
        {
            yield return Boot();
            float t0 = Time.realtimeSinceStartup;
            while (!(SkillPetSheet.Instance != null && PetSkillHost.Ready) && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            Assert.IsTrue(PetSkillHost.Ready, "소환 호스트가 20초 안에 안 섰다");
            TabBar tb0 = UiRoot.Instance.TabBar;
            if (tb0.ActiveTab != "summon") tb0.OnTab("summon");
            SkillPetSheet.Instance.Switch(SkillPetSheet.SubSkills);
            yield return null;
            Transform gauge = FindDeep(SkillPetSheet.Instance.Skills.transform, "summon-gauge");
            Assert.IsNotNull(gauge, "소환 게이지(summon-gauge · 스킬 하위판)");
            Transform face = gauge.Find("face");
            Assert.IsNotNull(face, "게이지 면(face)");
            Assert.IsNull(face.Find("bg-grad"), "트랙 홈(7992 gauge_track)은 8798 이 끈다 — 걷었다");
            Transform fill = face.Find("fill");
            Assert.IsNotNull(fill, "채움(face/fill)");
            Assert.IsNull(fill.Find("bg-grad"), "채움 광택(7953 gauge_fill)은 8803 이 끈다 — 걷었다");
            Assert.AreEqual(0, fill.childCount, "채움은 자식 겹이 없는 색 한 칸");
            Assert.AreEqual(PetSkillStyle.C("pp_blue"), fill.GetComponent<Image>().color, "채움 = pp_blue 한 칸");
            // 런 1281(40회차): 같은 면에 8798 키라인·8812 눈금이 섰다 — «둘뿐» 은 39회차의 전제였다. 이 칸이 묻는 것은 «정본이 끈 겹(그라디언트 판)이 없다» 다:
            //   면 아래 Image 는 face·keyline·fill·seg-ticks 넷 중 하나뿐이고 `bg-grad`(구운 그라디언트) 는 없다.
            foreach (Image im in face.GetComponentsInChildren<Image>(true))
            {
                string n = im.name;
                Assert.IsTrue(n == "face" || n == "fill" || n == "keyline" || n == "seg-ticks", "면 아래 낯선 Image(정본이 끈 겹?): " + n);
                Assert.IsFalse(im.sprite != null && im.sprite.texture != null && im.sprite.texture.name.StartsWith("sf-gauge_", System.StringComparison.Ordinal), "걷은 판 gauge_track/gauge_fill 이 남아 있다: " + n);
            }
        }

        /// <summary>T178 40회차 — 정본 **8812** `.upg-progress::after, .summon-gauge::after, .qst-bar::after, .summon-prog::after, .petup-xpbar::after, .rates-prog::after`
        /// 분절 눈금(`repeating-linear-gradient(90deg, 투명 0 (seg−gap), 검 .58 (seg−gap) seg)` · `--seg` .62rem · 틈 ol2)과 **8798** 하드 키라인(`inset 0 0 0 ol1 검 .55` · 트랙 셋).
        /// 표 두 칸 + 구운 타일의 화소(주기 끝만 잉크) + 실물: 소환 게이지(face 에 keyline → fill → seg-ticks 순) · 스킬 파편(sk-shard)엔 없음 · 퀘스트 막대(keyline + seg-ticks).</summary>
        [UnityTest]
        public IEnumerator 게이지_여섯에_분절_눈금이_서고_트랙_셋에_하드_키라인이_선다()
        {
            yield return Boot();
            // ⓐ 표
            Assert.AreEqual(0.62f, SurfaceArt.StripeNum("gauge_seg", "period_rem", 0f), 1e-4f, "8790 --seg .62rem");
            Assert.AreEqual(90f, SurfaceArt.StripeNum("gauge_seg", "angle_deg", 0f), 1e-4f, "세로 구분선 = 90deg 축");
            // ⓑ 구운 타일 — 주기의 끝 gap 만큼만 잉크(검 .58) · 앞은 투명
            float rem = PopupKit.Rem, period = 0.62f * rem, gap = PopupKit.Line2;
            Sprite tile = SurfaceArt.BakeStripe("gauge_seg", period, gap, period - gap, 20f);
            Texture2D tt = tile.texture; int TW = tt.width, TH = tt.height;
            Color32 first = tt.GetPixels32()[(TH / 2) * TW], last = tt.GetPixels32()[(TH / 2) * TW + TW - 1];
            Assert.AreEqual(0, first.a, "타일 앞(0 ~ seg−gap)은 투명");
            Assert.AreEqual(0x94, last.a, "타일 끝(seg−gap ~ seg)은 검 .58(0x94)");
            Assert.AreEqual(0, last.r, "잉크는 검정");
            // ⓒ 실물 — 소환 게이지(37회차와 같은 길)
            float t0 = Time.realtimeSinceStartup;
            while (!(SkillPetSheet.Instance != null && PetSkillHost.Ready) && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            Assert.IsTrue(PetSkillHost.Ready, "소환 호스트가 20초 안에 안 섰다");
            TabBar tb0 = UiRoot.Instance.TabBar;
            if (tb0.ActiveTab != "summon") tb0.OnTab("summon");
            SkillPetSheet.Instance.Switch(SkillPetSheet.SubSkills);
            yield return null;
            Transform gauge = FindDeep(SkillPetSheet.Instance.Skills.transform, "summon-gauge");
            Assert.IsNotNull(gauge, "소환 게이지(summon-gauge · 스킬 하위판)");
            Transform face = gauge.Find("face");
            Transform kl = face.Find("keyline"), fill = face.Find("fill"), ticks = face.Find("seg-ticks");
            Assert.IsNotNull(kl, "8798 하드 키라인(face/keyline)");
            Assert.IsNotNull(ticks, "8812 분절 눈금(face/seg-ticks)");
            Assert.Less(kl.GetSiblingIndex(), fill.GetSiblingIndex(), "키라인은 채움 아래(inset 그림자는 자식 아래에 깔린다)");
            Assert.Greater(ticks.GetSiblingIndex(), fill.GetSiblingIndex(), "눈금은 채움 위(::after z 2)");
            Image ti = ticks.GetComponent<Image>();
            Assert.AreEqual(Image.Type.Tiled, ti.type, "한 타일을 굽고 Tiled 로 되풀이");
            Assert.IsTrue(ti.sprite != null && ti.sprite.texture.name.StartsWith("sf-stripe-gauge_seg", System.StringComparison.Ordinal), "눈금 타일 gauge_seg: " + (ti.sprite == null ? "null" : ti.sprite.texture.name));
            Assert.IsFalse(ti.raycastTarget, "pointer-events: none");
            Image ki = kl.GetComponent<Image>();
            Assert.IsTrue(ki.sprite != null && ki.sprite.texture.name.StartsWith("sf-dashed-", System.StringComparison.Ordinal), "키라인은 틈 0 의 둥근 테 한 장");
            Color32[] kp = ki.sprite.texture.GetPixels32(); int KW = ki.sprite.texture.width, KH = ki.sprite.texture.height;
            Assert.AreEqual(0, kp[(KH / 2) * KW + KW / 2].a, "테 안쪽 가운데는 투명");
            Assert.Greater(kp[(KH / 2) * KW].a, 0x60, "왼쪽 가장자리는 검 .55 테");
            Transform shard = FindDeep(SkillPetSheet.Instance.Skills.transform, "sk-shard");
            if (shard != null) { Assert.IsNull(shard.Find("face/seg-ticks"), "sk-shard 는 8812 목록에 없다"); Assert.IsNull(shard.Find("face/keyline"), "sk-shard 는 8798 목록에도 없다"); }
            // ⓓ 실물 — 퀘스트 막대
            t0 = Time.realtimeSinceStartup;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            UiRoot.Instance.TabBar.OnTab("quest");
            yield return null; yield return null;
            int bars = 0;
            foreach (Transform bar in UiRoot.Instance.App.GetComponentsInChildren<Transform>(true))
            {
                if (bar.name != "bar" || bar.Find("face/bg") == null || bar.Find("face/fill") == null) continue;
                bars++;
                Transform qk = bar.Find("face/keyline"), qf = bar.Find("face/fill"), qt = bar.Find("face/seg-ticks");
                Assert.IsNotNull(qk, "퀘스트 막대 키라인(8798)"); Assert.IsNotNull(qt, "퀘스트 막대 눈금(8812)");
                Assert.Less(qk.GetSiblingIndex(), qf.GetSiblingIndex(), "키라인은 채움 아래"); Assert.Greater(qt.GetSiblingIndex(), qf.GetSiblingIndex(), "눈금은 채움 위");
                Assert.AreEqual(Image.Type.Tiled, qt.GetComponent<Image>().type);
            }
            Assert.Greater(bars, 0, "퀘스트 막대가 하나는 섰다");
        }

        /// <summary>T178 41회차 — 정본 **8686** `.btn.btn.danger.danger, .btn.btn.sell.sell` 세 겹(위 1px 분홍 림 · 좌우 1px 키라인 `calc(100% − 1px)` = `-px` 단위 · 46% 밴드 + 검붉은 그늘)을
        /// 빨간 면(판매·뒤로 ◀·해제·던전 빨강 알약·채팅 ◀) 위에 한 판(BakeFace)으로 · **8618** `.pass-cell.premium` 135deg 사선 해칭(5/10 CSS px 검 .24 · 타일 + Tiled).
        /// 표 + 실물(퀘스트 시트 뒤로 ◀ 면 · 패스 팝업 프리미엄 칸).</summary>
        [UnityTest]
        public IEnumerator 빨간_버튼_면에_세_겹이_한_판으로_서고_패스_프리미엄_칸에_사선_해칭이_선다()
        {
            yield return Boot();
            // ⓐ 표 — 옆 키라인 겹의 단위: 앞 셋 px · 뒤 셋 -px(끝에서 잰다)
            Color[] sc; float[] so;
            SurfaceArt.Stops("btn_danger_side", out sc, out so);
            Assert.AreEqual(6, sc.Length); Assert.AreEqual(90f, SurfaceArt.Angle("btn_danger_side"), 1e-4f, "좌우 키라인 = 90deg");
            bool[] px = SurfaceArt.PxMask("btn_danger_side", 6), ex = SurfaceArt.EndPxMask("btn_danger_side", 6);
            Assert.IsTrue(px[0] && px[1] && px[2] && !px[3] && !px[4] && !px[5], "앞 셋이 px");
            Assert.IsTrue(!ex[0] && !ex[1] && !ex[2] && ex[3] && ex[4] && ex[5], "뒤 셋이 -px(calc(100% − 1px))");
            Assert.AreEqual(0.24f, sc[0].a, 1e-4f, "왼쪽 흰 .24"); Assert.AreEqual(0.22f, sc[5].a, 1e-4f, "오른쪽 검 .22"); Assert.AreEqual(0f, sc[5].r, 1e-4f);
            SurfaceArt.Stops("btn_danger_body", out sc, out so);
            Assert.AreEqual(5, sc.Length); Assert.AreEqual(0.46f, so[1], 1e-4f, "46% 밴드 엣지"); Assert.AreEqual(0f, sc[2].a, 1e-4f, "47% 투명"); Assert.AreEqual(0.16f, sc[4].a, 1e-4f, "아래 검붉음 .16");
            SurfaceArt.Stops("btn_danger_rim", out sc, out so);
            Assert.AreEqual(0.62f, sc[0].a, 1e-4f, "위 림 .62"); Assert.IsTrue(SurfaceArt.PxMask("btn_danger_rim", 3)[1], "1px");
            Assert.AreEqual(135f, SurfaceArt.StripeNum("pass_premium_hatch", "angle_deg", 0f), 1e-4f);
            Assert.AreEqual(10f, SurfaceArt.StripeNum("pass_premium_hatch", "period_css_px", 0f), 1e-4f); Assert.AreEqual(5f, SurfaceArt.StripeNum("pass_premium_hatch", "dash_css_px", 0f), 1e-4f);

            // ⓑ 실물 — 퀘스트 시트 뒤로 ◀(PopupKit.SheetBack → BackButton · face pp_red)
            float t0 = Time.realtimeSinceStartup;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            MetaHost h = MetaHost.Instance;
            QuestSheet.Open(h);
            yield return null; yield return null; yield return null; yield return null;   // FillFaceWhenSized — 두 프레임 연속 같은 크기일 때 굽는다
            Canvas.ForceUpdateCanvases();
            Popup qp = h.Popups.Find(QuestSheet.Name);
            Assert.IsNotNull(qp, "퀘스트 시트가 열려 있다");
            Transform back = null;
            foreach (RectTransform rt in qp.Root.GetComponentsInChildren<RectTransform>(true)) if (rt.name == "back-btn") { back = rt; break; }
            Assert.IsNotNull(back, "뒤로 ◀(back-btn)");
            Transform bf = back.Find("face");
            Assert.IsNotNull(bf, "◀ 면(face)");
            Assert.IsNotNull(bf.GetComponent<Mask>(), "면에 Mask");
            Transform bg = bf.Find("bg-grad");
            Assert.IsNotNull(bg, "8686 세 겹 한 판(face/bg-grad)");
            Image bgi = bg.GetComponent<Image>();
            Assert.IsTrue(bgi.sprite != null && bgi.sprite.texture.name.StartsWith("sf-face-", System.StringComparison.Ordinal), "BakeFace 판: " + (bgi.sprite == null ? "null" : bgi.sprite.texture.name));
            Assert.IsTrue(bgi.sprite.texture.name.Contains("btn_danger_body+btn_danger_side+btn_danger_rim"), "겹 셋 순서(아래→위)");
            Color32[] bp = bgi.sprite.texture.GetPixels32(); int BW = bgi.sprite.texture.width, BH = bgi.sprite.texture.height;
            // 런 1285 실측 — 판은 **면의 실제 크기**로 구워야 1px 림·키라인이 화면에서 1px 로 선다(기본 100×100 rect 로 구우면 늘려져 사라진다 · 채팅 ◀ 만 맞았었다).
            Assert.AreEqual(Mathf.RoundToInt(((RectTransform)bf).rect.width), BW, 1, "판 가로 = 면 rect 가로(레이아웃이 끝난 크기)");
            Assert.AreEqual(Mathf.RoundToInt(((RectTransform)bf).rect.height), BH, 1, "판 세로 = 면 rect 세로");
            Assert.AreEqual(255, bp[(BH / 2) * BW + BW / 2].a, "바탕(pp_red)을 미리 합성 — 불투명");
            float Lum(Color32 c) { return c.r * 0.2126f + c.g * 0.7152f + c.b * 0.0722f; }
            Assert.Greater(Lum(bp[(BH - 1) * BW + BW / 2]), Lum(bp[(BH / 2) * BW + BW / 2]) + 8f, "위 1px 분홍 림이 가운데보다 밝다");
            Assert.Greater(Lum(bp[(BH / 2) * BW]), Lum(bp[(BH / 2) * BW + BW - 1]) + 8f, "왼쪽 흰 키라인이 오른쪽 검 키라인보다 밝다");
            Assert.Greater(Lum(bp[(BH / 2) * BW + BW / 2]), Lum(bp[BW / 2]) + 4f, "아래(검붉음 .16)가 가운데보다 어둡다");
            Assert.Greater(Lum(bp[(BH * 3 / 4) * BW + BW / 2]), Lum(bp[(BH / 2 - 2) * BW + BW / 2]) + 2f, "46% 위 밴드가 47% 아래보다 밝다");
            QuestSheet.Close(h);
            yield return null;

            // ⓒ 실물 — 패스 팝업 프리미엄 칸
            PassPopup.Open(h);
            yield return null; yield return null;
            Popup pass = PopupLayer.Instance.Find(PassPopup.Name);
            Assert.IsNotNull(pass, "패스 팝업");
            int prem = 0, free = 0;
            foreach (RectTransform rt in pass.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name == "premium" && rt.Find("face/face") != null)
                {
                    prem++;
                    Transform hatch = rt.Find("face/face/hatch");
                    Assert.IsNotNull(hatch, "프리미엄 칸 해칭(face/face/hatch · 8618)");
                    Image hi = hatch.GetComponent<Image>();
                    Assert.AreEqual(Image.Type.Tiled, hi.type, "한 타일 + Tiled");
                    Assert.IsTrue(hi.sprite != null && hi.sprite.texture.name.StartsWith("sf-stripe-pass_premium_hatch", System.StringComparison.Ordinal), "타일 pass_premium_hatch");
                    if (prem == 1)
                    {
                        Color32[] hp = hi.sprite.texture.GetPixels32(); int seen0 = 0, seenInk = 0;
                        foreach (Color32 c in hp) { if (c.a == 0) seen0++; else if (c.a == 0x3d && c.r == 0) seenInk++; }
                        Assert.Greater(seen0, 0, "투명 자리"); Assert.Greater(seenInk, 0, "검 .24(0x3d) 자리");
                        Assert.AreEqual(hp.Length, seen0 + seenInk, "두 값뿐(하드 엣지 · 블러 없음)");
                    }
                }
                else if (rt.name == "free" && rt.Find("face/face") != null) { free++; Assert.IsNull(rt.Find("face/face/hatch"), "무료 칸엔 해칭이 없다"); }
            }
            Assert.Greater(prem, 0, "프리미엄 칸이 하나는 섰다"); Assert.Greater(free, 0, "무료 칸이 하나는 섰다");
            h.Popups.Hide(PassPopup.Name);
            yield return null;
        }

        /// <summary>T178 42회차 — 정본 **8504**(같은 선택자 7833 → 8204 → 8336 → 8504 의 마지막 선언) `.btn.btn:not(.silver):not(.ascend-ready)` 유리 겹 셋(위 1px 하늘색 림 ·
        /// 좌우 1px 키라인 · 46%↔47% 하드 스톱 밴드 + 남색 그늘)이 파란 종이 버튼(스킬 패널 [모두 업그레이드] · PaperButton Primary)에 한 판으로 선다 · 소환 버튼(8661 단색)엔 안 선다.</summary>
        [UnityTest]
        public IEnumerator 파란_버튼_면에_유리_겹_셋이_한_판으로_선다()
        {
            yield return Boot();
            // ⓐ 표
            Color[] gc; float[] go;
            SurfaceArt.Stops("btn_glass_rim", out gc, out go);
            Assert.AreEqual(0.88f, gc[0].a, 1e-4f, "위 림 .88"); Assert.AreEqual(228f / 255f, gc[0].r, 1e-3f, "하늘색 (228,244,255)"); Assert.IsTrue(SurfaceArt.PxMask("btn_glass_rim", 3)[1], "1px");
            SurfaceArt.Stops("btn_glass_body", out gc, out go);
            Assert.AreEqual(5, gc.Length); Assert.AreEqual(0.46f, gc[0].a, 1e-4f, "위 하늘색 .46"); Assert.AreEqual(0.46f, go[1], 1e-4f); Assert.AreEqual(0.47f, go[2], 1e-4f, "하드 스톱 46↔47"); Assert.AreEqual(0f, gc[2].a, 1e-4f);
            Assert.AreEqual(0.16f, gc[4].a, 1e-4f, "아래 남색 .16"); Assert.AreEqual(44f / 255f, gc[4].b, 1e-3f, "(4,14,44)");
            Assert.AreEqual("btn_danger_side", SurfaceArt.BtnGlassLayers[1], "옆 키라인은 8686 과 같은 수 — 한 키를 같이 쓴다");
            // ⓑ 실물 — 스킬 패널 [모두 업그레이드](PaperButton Primary · pp_blue)
            float t0 = Time.realtimeSinceStartup;
            while (!(SkillPetSheet.Instance != null && PetSkillHost.Ready) && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            Assert.IsTrue(PetSkillHost.Ready, "소환 호스트가 20초 안에 안 섰다");
            TabBar tb0 = UiRoot.Instance.TabBar;
            if (tb0.ActiveTab != "summon") tb0.OnTab("summon");
            SkillPetSheet.Instance.Switch(SkillPetSheet.SubSkills);
            yield return null; yield return null; yield return null; yield return null;   // FillFaceWhenSized — 두 프레임 안정
            Canvas.ForceUpdateCanvases();
            Button ua = SkillPetSheet.Instance.Skills.UpgradeAllButton;
            Assert.IsNotNull(ua, "[모두 업그레이드](btn-upgrade-all)");
            Transform top = ua.transform.Find("skin/top");
            Assert.IsNotNull(top, "둥근 면(skin/top)");
            Transform g = top.Find("bg-grad");
            Assert.IsNotNull(g, "8504 유리 겹 한 판(skin/top/bg-grad)");
            Image gi = g.GetComponent<Image>();
            Assert.IsTrue(gi.sprite != null && gi.sprite.texture.name.Contains("btn_glass_body+btn_danger_side+btn_glass_rim"), "BakeFace 판(아래→위): " + (gi.sprite == null ? "null" : gi.sprite.texture.name));
            Color32[] bp = gi.sprite.texture.GetPixels32(); int BW = gi.sprite.texture.width, BH = gi.sprite.texture.height;
            Assert.AreEqual(Mathf.RoundToInt(((RectTransform)top).rect.width), BW, 1, "판 가로 = 면 rect 가로");
            Assert.AreEqual(Mathf.RoundToInt(((RectTransform)top).rect.height), BH, 1, "판 세로 = 면 rect 세로");
            float Lum(Color32 c) { return c.r * 0.2126f + c.g * 0.7152f + c.b * 0.0722f; }
            Color32 topPx = bp[(BH - 1) * BW + BW / 2], mid = bp[(BH / 2) * BW + BW / 2], bot = bp[BW / 2];
            Assert.AreEqual(255, mid.a, "바탕(pp_blue)을 미리 합성 — 불투명");
            Assert.Greater(Lum(topPx), Lum(mid) + 20f, "위 1px 하늘색 림이 가운데보다 훨씬 밝다");
            Assert.Greater(Lum(bp[(BH / 2) * BW]), Lum(bp[(BH / 2) * BW + BW - 1]) + 8f, "왼쪽 흰 키라인 > 오른쪽 검 키라인");
            Assert.Greater(Lum(mid), Lum(bot) + 4f, "아래 남색 그늘이 가운데보다 어둡다");
            Assert.Greater(Lum(bp[(BH * 3 / 4) * BW + BW / 2]), Lum(bp[(BH / 2 - 2) * BW + BW / 2]) + 6f, "46% 위 광택 밴드가 47% 아래보다 밝다(하드 스톱)");
        }

        /// <summary>T178 43회차 — 정본 **8272** 시트(`.modal-card.sheet:not(.league-sheet):not(.shop-sheet)` · 겹 6)와 **8286** 팝업 카드(`.modal-card:not(.sheet):not(.pass-card):not(.lgr-card)` · 겹 4)의
        /// 종이 면이 한 판(BakeFace)으로 선다 — 표(rem 단위 + plus_px · 결 겹 · 방사) + 실물(퀘스트 시트 bg · 확률 정보 팝업 카드 face) · 판 크기 = 면 rect · 위 림 · 머리 밴드 선 · 아래 아이보리.</summary>
        [UnityTest]
        public IEnumerator 모달_카드와_시트의_종이_면이_한_판으로_선다()
        {
            yield return Boot();
            // ⓐ 표
            bool[] rm = SurfaceArt.RemMask("card_head_band", 5); float[] pp = SurfaceArt.PlusPx("card_head_band", 5);
            Assert.IsTrue(rm[1] && rm[2] && rm[3] && rm[4], "머리 밴드 정지점은 rem"); Assert.IsNotNull(pp); Assert.AreEqual(1f, pp[3], 1e-4f, "calc(2.6rem + 1px)"); Assert.AreEqual(0f, pp[2], 1e-4f);
            Color[] c; float[] o;
            SurfaceArt.Stops("card_paper_ramp", out c, out o); Assert.AreEqual(3, c.Length); Assert.AreEqual(1f, c[0].a, 1e-4f, "램프는 불투명"); Assert.AreEqual(231f / 255f, c[2].b, 1e-3f, "#f3efe7");
            SurfaceArt.Stops("sheet_paper_ramp", out c, out o); Assert.AreEqual(238f / 255f, c[2].b, 1e-3f, "#f8f4ee — 파랑 채널 238 바닥");
            Assert.IsTrue(SurfaceArt.IsRadial("sheet_top_light") && SurfaceArt.IsRadial("card_top_light"), "위 광원은 방사");
            Assert.AreEqual(8f, SurfaceArt.StripeNum("sheet_grain_a", "period_css_px", 0f), 1e-4f); Assert.AreEqual(-45f, SurfaceArt.StripeNum("sheet_grain_b", "angle_deg", 0f), 1e-4f);
            Assert.AreEqual(6, SurfaceArt.SheetPaperLayers.Length); Assert.AreEqual(4, SurfaceArt.CardPaperLayers.Length);
            // ⓑ 실물 — 퀘스트 시트(PopupKit.Sheet · pp_paper)
            float t0 = Time.realtimeSinceStartup;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            MetaHost h = MetaHost.Instance;
            QuestSheet.Open(h);
            yield return null; yield return null; yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            Popup qp = h.Popups.Find(QuestSheet.Name);
            Assert.IsNotNull(qp, "퀘스트 시트");
            Transform sbg = null;
            foreach (RectTransform rt in qp.Root.GetComponentsInChildren<RectTransform>(true)) if (rt.name == "bg" && rt.Find("bg-grad") != null && rt.parent != null && rt.parent.name == "sheet") { sbg = rt; break; }
            Assert.IsNotNull(sbg, "시트 면(sheet/bg)에 종이 한 판(bg-grad)");
            Image sgi = sbg.Find("bg-grad").GetComponent<Image>();
            Assert.IsTrue(sgi.sprite != null && sgi.sprite.texture.name.Contains("sheet_paper_ramp+sheet_top_light+stripe:sheet_grain_b+stripe:sheet_grain_a+sheet_head_band+sheet_top_rim"), "겹 여섯 순서(아래→위): " + (sgi.sprite == null ? "null" : sgi.sprite.texture.name));
            Color32[] sp = sgi.sprite.texture.GetPixels32(); int SW = sgi.sprite.texture.width, SH = sgi.sprite.texture.height;
            // 런 1293: 시트 면은 1080×1780 캔버스 px 라 상한(face_px_max 1024)에 걸린다 — 판은 **긴 변 기준 한 배율**로 줄고(비율 유지) 림·rem·결은 캔버스 px 로 재서 그림은 같다.
            float srw = ((RectTransform)sbg).rect.width, srh = ((RectTransform)sbg).rect.height;
            float ssc = Mathf.Min(1f, 1024f / Mathf.Max(srw, srh));
            Assert.AreEqual(Mathf.RoundToInt(srw * ssc), SW, 1, "판 가로 = 시트 면 rect 가로 × 배율(긴 변 1024 상한)");
            Assert.AreEqual(Mathf.RoundToInt(srh * ssc), SH, 1, "판 세로 = 시트 면 rect 세로 × 같은 배율(비율 유지)");
            float Lum(Color32 k) { return k.r * 0.2126f + k.g * 0.7152f + k.b * 0.0722f; }
            float DarkestNear(Color32[] px, int W, int y0, int span) { float m = 999f; for (int y = y0 - span; y <= y0 + span; y++) if (y >= 0 && y < px.Length / W) m = Mathf.Min(m, Lum(px[y * W + W / 2])); return m; }
            Assert.AreEqual(255, sp[SW / 2].a, "불투명(램프가 바닥)");
            Assert.Greater(Lum(sp[(SH - 1) * SW + SW / 2]), 250f, "위 2px 흰 림(.9)");
            int bandY = SH - 1 - Mathf.RoundToInt(3.1f * PopupKit.Rem * ssc);          // 머리 밴드 아래 1px 선 자리(위에서 3.1rem · 판 배율)
            Assert.Less(DarkestNear(sp, SW, bandY, 2), Lum(sp[(bandY - 8) * SW + SW / 2]) - 4f, "3.1rem 자리의 1px 선(.11)이 그 아래보다 어둡다");
            // 런 1294: 한 화소는 흰 결 줄(45°/−45° · .95/.55) 위에 앉을 수 있다(238 + 17×.55 = 247) — 아래 가운데 작은 창에서 **파랑이 가장 낮은 화소**(결 사이 바탕)를 본다.
            Color32 Ground(Color32[] px, int W, int H, int cx, int y0, int span) { Color32 g = px[y0 * W + cx]; for (int y = Mathf.Max(0, y0 - span); y <= Mathf.Min(H - 1, y0 + span); y++) for (int x = Mathf.Max(0, cx - span); x <= Mathf.Min(W - 1, cx + span); x++) { Color32 c2 = px[y * W + x]; if (c2.b < g.b) g = c2; } return g; }
            Color32 sground = Ground(sp, SW, SH, SW / 2, 4, 8);
            Assert.Less(sground.b, 245, "아래는 아이보리(#f8f4ee 파랑 238 근처 · 결 사이 바탕): " + sground);
            Assert.Greater(sground.r, sground.b, "따뜻한 종이 — 빨강 > 파랑: " + sground);
            QuestSheet.Close(h);
            yield return null;
            // ⓒ 실물 — 확률 정보 팝업 카드(PopupKit.Card · pp_paper · 높이는 내용으로)
            t0 = Time.realtimeSinceStartup;
            while (!ForgeHost.Ready && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 안 섰다");
            ForgeHost fh = ForgeHost.Instance;
            ForgeInfoPopup.Open(fh);
            yield return null; yield return null; yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            Popup fp = fh.Meta.Popups.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(fp, "확률 정보 팝업");
            Transform cface = null;
            foreach (RectTransform rt in fp.Root.GetComponentsInChildren<RectTransform>(true)) if (rt.name == "face" && rt.parent != null && rt.parent.name == "card" && rt.Find("bg-grad") != null) { cface = rt; break; }
            Assert.IsNotNull(cface, "카드 면(card/face)에 종이 한 판(bg-grad)");
            Image cgi = cface.Find("bg-grad").GetComponent<Image>();
            Assert.IsTrue(cgi.sprite != null && cgi.sprite.texture.name.Contains("card_paper_ramp+card_top_light+stripe:card_grain+card_head_band"), "겹 넷 순서: " + (cgi.sprite == null ? "null" : cgi.sprite.texture.name));
            float crw = ((RectTransform)cface).rect.width, crh = ((RectTransform)cface).rect.height;
            float csc = Mathf.Min(1f, 1024f / Mathf.Max(crw, crh));
            Assert.AreEqual(Mathf.RoundToInt(crw * csc), cgi.sprite.texture.width, 1, "판 가로 = 카드 면 rect 가로 × 배율");
            Assert.AreEqual(Mathf.RoundToInt(crh * csc), cgi.sprite.texture.height, 1, "판 세로 = 카드 면 rect 세로 × 같은 배율(내용이 정한 높이 뒤에 구웠다)");
            Color32[] cp = cgi.sprite.texture.GetPixels32(); int CW = cgi.sprite.texture.width, CH = cgi.sprite.texture.height;
            int cband = CH - 1 - Mathf.RoundToInt(2.6f * PopupKit.Rem * csc);
            Assert.Less(DarkestNear(cp, CW, cband, 2), Lum(cp[(cband - 8) * CW + CW / 2]) - 4f, "2.6rem 자리의 1px 선(.10)");
            Color32 cground = Ground(cp, CW, CH, CW / 2, 4, 8);
            Assert.Greater(cground.r, cground.b, "아래 #f3efe7 — 따뜻한 종이: " + cground);
            Assert.Less(cground.b, 240, "아래 #f3efe7 파랑 231 근처(결 .028 이 얹혀 조금 어둡다): " + cground);
            fh.Meta.Popups.Hide(ForgeInfoPopup.Name);
            yield return null;
        }

        /// <summary>T178 44회차 — 행 플레이트: 정본 **8089** `.qst-row`(결 + 광택 밴드 램프) · **8415** `.league-row:not(.me), .equipped-row`(어두운 판 결 + 위 광 → 아래 그늘) · **8435** `.league-row.me`(위 광만)
        /// 가 각 행 면 위 한 판(BakeFace)으로 선다 — 표 + 퀘스트 시트 행 · 리그 행(나/남) · 스킬 장착됨 바.</summary>
        [UnityTest]
        public IEnumerator 퀘스트_행과_리그_행과_장착됨_바에_행_플레이트_겹이_선다()
        {
            yield return Boot();
            // ⓐ 표
            Color[] c; float[] o;
            SurfaceArt.Stops("qst_row_ramp", out c, out o); Assert.AreEqual(5, c.Length); Assert.AreEqual(0.95f, c[0].a, 1e-4f, "위 흰 .95"); Assert.AreEqual(0.32f, o[2], 1e-4f, "32% 밴드 엣지"); Assert.AreEqual(0.14f, c[4].a, 1e-4f, "아래 (23,24,26,.14)");
            SurfaceArt.Stops("league_row_ramp", out c, out o); Assert.AreEqual(4, c.Length); Assert.AreEqual(0.22f, c[0].a, 1e-4f); Assert.AreEqual(0.36f, c[3].a, 1e-4f, "아래 검 .36"); Assert.AreEqual(0f, c[3].r, 1e-4f);
            SurfaceArt.Stops("league_me_ramp", out c, out o); Assert.AreEqual(3, c.Length); Assert.AreEqual(0.38f, c[0].a, 1e-4f); Assert.AreEqual(0f, c[2].a, 1e-4f, "58% 에서 끝 — 아래 그늘 없음(8422 주석)");
            Assert.AreEqual(9f, SurfaceArt.StripeNum("qst_row_grain", "period_css_px", 0f), 1e-4f); Assert.AreEqual(2f, SurfaceArt.StripeNum("league_me_grain", "dash_css_px", 0f), 1e-4f);
            float Lum(Color32 k) { return k.r * 0.2126f + k.g * 0.7152f + k.b * 0.0722f; }
            Color32 Ground(Color32[] px, int W, int H, int cx, int y0, int span) { Color32 g = px[y0 * W + cx]; for (int y = Mathf.Max(0, y0 - span); y <= Mathf.Min(H - 1, y0 + span); y++) for (int x = Mathf.Max(0, cx - span); x <= Mathf.Min(W - 1, cx + span); x++) { Color32 c2 = px[y * W + x]; if (Lum(c2) < Lum(g)) g = c2; } return g; }
            // ⓑ 퀘스트 시트 행
            float t0 = Time.realtimeSinceStartup;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            MetaHost h = MetaHost.Instance;
            QuestSheet.Open(h);
            yield return null; yield return null; yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            Popup qp = h.Popups.Find(QuestSheet.Name);
            Assert.IsNotNull(qp, "퀘스트 시트");
            int qrows = 0;
            foreach (RectTransform rt in qp.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name != "face" || rt.parent == null || rt.parent.name != "face" || rt.parent.parent == null || rt.parent.parent.name != "row") continue;
                Transform g = rt.Find("bg-grad"); Assert.IsNotNull(g, "퀘스트 행 면(row/face/face)에 플레이트 한 판(bg-grad)");
                Image gi = g.GetComponent<Image>();
                Assert.IsTrue(gi.sprite != null && gi.sprite.texture.name.Contains("qst_row_ramp+stripe:qst_row_grain"), "8089 겹 둘: " + (gi.sprite == null ? "null" : gi.sprite.texture.name));
                if (qrows == 0)
                {
                    Assert.AreEqual(Mathf.RoundToInt(rt.rect.width), gi.sprite.texture.width, 1, "판 가로 = 행 면 rect 가로");
                    Color32[] px = gi.sprite.texture.GetPixels32(); int W = gi.sprite.texture.width, H = gi.sprite.texture.height;
                    Assert.Greater(Lum(px[(H - 2) * W + W / 2]), Lum(Ground(px, W, H, W / 2, 3, 4)) + 8f, "위 광택(흰 .95)이 아래 그늘(.14)보다 밝다");
                }
                qrows++;
            }
            Assert.Greater(qrows, 0, "퀘스트 행이 하나는 섰다");
            QuestSheet.Close(h);
            yield return null;
            // ⓒ 리그 행(나 = 위 광만 · 남 = 아래 그늘)
            LeagueSheet.Open(h);
            yield return null; yield return null; yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            Popup lp = PopupLayer.Instance.Find(LeagueSheet.Name);
            Assert.IsNotNull(lp, "리그 시트(팝업 층)");
            int others = 0, mine = 0;
            foreach (RectTransform rt in lp.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name != "face" || rt.parent == null || rt.parent.name != "face" || rt.parent.parent == null || !rt.parent.parent.name.StartsWith("row-", System.StringComparison.Ordinal)) continue;
                Transform g = rt.Find("bg-grad"); Assert.IsNotNull(g, "리그 행 면에 플레이트(bg-grad)");
                string tn = g.GetComponent<Image>().sprite.texture.name;
                if (tn.Contains("league_me_ramp+stripe:league_me_grain")) mine++;
                else if (tn.Contains("league_row_ramp+stripe:league_row_grain"))
                {
                    others++;
                    if (others == 1)
                    {
                        Texture2D tx = g.GetComponent<Image>().sprite.texture; Color32[] px = tx.GetPixels32(); int W = tx.width, H = tx.height;
                        Assert.Greater(Lum(px[(H - 2) * W + W / 2]), Lum(px[1 * W + W / 2]) + 10f, "어두운 행: 위 광(.22)이 아래 그늘(검 .36)보다 밝다");
                    }
                }
                else Assert.Fail("리그 행 플레이트가 둘 중 하나가 아니다: " + tn);
            }
            Assert.Greater(others, 0, "남의 행이 하나는 섰다"); Assert.AreEqual(1, mine, "내 행(.me)은 하나 · 위 광만 판");
            LeagueSheet.Close(h);
            yield return null;
            // ⓓ 스킬 패널 장착됨 바
            t0 = Time.realtimeSinceStartup;
            while (!(SkillPetSheet.Instance != null && PetSkillHost.Ready) && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            Assert.IsTrue(PetSkillHost.Ready, "소환 호스트가 20초 안에 안 섰다");
            TabBar tb0 = UiRoot.Instance.TabBar;
            if (tb0.ActiveTab != "summon") tb0.OnTab("summon");
            SkillPetSheet.Instance.Switch(SkillPetSheet.SubSkills);
            yield return null; yield return null; yield return null; yield return null;
            Transform eq = FindDeep(SkillPetSheet.Instance.Skills.transform, "equipped-row");
            Assert.IsNotNull(eq, "장착됨 바(equipped-row)");
            Transform eg = eq.Find("face/bg-grad");
            Assert.IsNotNull(eg, "장착됨 바 면에 플레이트(8415 편입)");
            Assert.IsTrue(eg.GetComponent<Image>().sprite.texture.name.Contains("league_row_ramp+stripe:league_row_grain"), "리그 행과 같은 처방");
        }

    }
}
