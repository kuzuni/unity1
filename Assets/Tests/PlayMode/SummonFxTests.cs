using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T179 — 소환 결과 무대판의 연출 겹 셋(정본 `.sr-rays` z0 · `.sr-stars` z15 · `.sr-canopy` z20)이 서고, 자리·개수가 표(`SummonFxUi.json`)대로이며,
    /// 별은 done 뒤에만 켜진다. 그림은 `screen_summon-result` 류 PNG 눈 확인.
    /// 3회차: 바닥 반사(`.sr-reflect` z12 · 정본 6957~6978 · ui.js 771~790)가 done **다음** 프레임에 서고 — 몸 폭 · 위 변 = 셀 줄 끝 − 8px · 아래로만 1.22배 · 바닥과 별 사이 —
    /// 구운 한 장은 구체가 찍혀 있고 원본 좌표 맨 아래 줄은 마스크로 0 · α 는 .5s 뒤 .88.
    /// </summary>
    public class SummonFxTests
    {
        static IEnumerator Boot()
        {
            PetSkillHost.SuppressSave = true;
            PetSkillHost.Seed = 20260912;
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            Scene active = SceneManager.GetActiveScene();
            for (int i = 0; i < 600 && !(SkillPetSheet.Instance != null && SkillPetSheet.Instance.gameObject.scene == active && PetSkillHost.Ready && SkillBar.Instance != null); i++) yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다");
            Assert.IsTrue(PetSkillHost.Ready);
            yield return null;
        }

        static SkillPetSheet Sheet { get { return SkillPetSheet.Instance; } }
        static PetSkillHost Host { get { return PetSkillHost.Instance; } }

        [UnityTest]
        public IEnumerator 무대판에_광선_별_천개가_서고_별은_done_뒤에만_켜진다()
        {
            yield return Boot();
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            Sheet.Switch(SkillPetSheet.SubSkills);
            yield return null;
            while (Host.SummonMult("skill") != 1) Host.CycleSummonMult("skill");
            Host.Tickets = 10000;
            Host.Sync();
            yield return null;
            Sheet.Skills.SummonButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(Sheet.Modal.IsOpen(SkillSummonResultView.ModalName), "결과 연출 팝업");
            SkillSummonResultView v = SkillSummonResultView.Current;
            Assert.IsNotNull(v);
            Assert.AreEqual(1, v.CellCount, "x1 = 무대판(one)");
            SummonFx fx = v.Fx;
            Assert.IsNotNull(fx, "무대판(stage)이면 연출 겹이 선다");

            // ⓐ 층 사다리 — 광선은 몸의 첫 형제(z0) · 별은 바닥 뒤 · 천개는 별 뒤 · 그리드는 그 뒤(z40)
            RectTransform body = (RectTransform)fx.transform;
            Assert.AreEqual("sr-rays", body.GetChild(0).name, "광선이 맨 아래(z 0)");
            Transform floor = body.Find("sr-floor"), stars = body.Find("sr-stars"), canopy = body.Find("sr-canopy"), grid = body.Find("sr-grid");
            Assert.IsNotNull(floor); Assert.IsNotNull(stars); Assert.IsNotNull(canopy); Assert.IsNotNull(grid);
            Assert.Less(floor.GetSiblingIndex(), stars.GetSiblingIndex(), "바닥(10) < 별(15)");
            Assert.Less(stars.GetSiblingIndex(), canopy.GetSiblingIndex(), "별(15) < 천개(20)");
            Assert.Less(canopy.GetSiblingIndex(), grid.GetSiblingIndex(), "천개(20) < 그리드(40)");

            // ⓑ 광선 — 폭 190% 정사각 · 몸 가운데
            RectTransform rays = fx.Rays;
            Assert.AreEqual(body.rect.width * SummonFxStyle.L("rays_w_f"), rays.rect.width, 1f, "광선 폭 = 몸 폭 × 1.9");
            Assert.AreEqual(rays.rect.width, rays.rect.height, 1f, "정사각");
            Assert.IsNotNull(rays.GetComponent<Image>().sprite, "구운 광선 스프라이트");

            // ⓒ 천개 — one 판: 그리드 폭 × .62 · 비율 3:1 · 아치 + 빛발 3 + 스필
            RectTransform cp = fx.Canopy;
            float gw = ((RectTransform)grid).rect.width;
            Assert.AreEqual(gw * SummonFxStyle.L("canopy_one_w_f"), cp.rect.width, 1f, "천개 폭(one) = 그리드 폭 × .62");
            Assert.AreEqual(cp.rect.width / SummonFxStyle.L("canopy_one_aspect"), cp.rect.height, 1f, "비율 3:1");
            Assert.AreEqual(3, fx.RayBarCount, "빛발 셋");
            Assert.IsNotNull(cp.Find("arch")); Assert.IsNotNull(cp.Find("spill")); Assert.IsNotNull(cp.Find("ray-2"));
            // 천개 바닥은 그리드 위에서 mb 만큼 아래(정본 margin-bottom -2.6rem = 겹침) — 도입(srcanopy .45s · scale .8→1 · 가운데 피벗)이 끝난 뒤 잰다(런 438: 도중에 재서 −16px)
            float tin = 0f;
            while (tin < (SummonFxStyle.L("canopy_in_delay_ms") + SummonFxStyle.L("canopy_in_ms")) / 1000f + 0.1f) { tin += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(1f, cp.localScale.x, 1e-3f, "도입이 끝나면 천개 배율 1");
            Assert.IsTrue(fx.Baked, "굽기는 Build 가 아니라 첫 Update 에서(연 프레임을 가볍게 · 결정 516) — 도입이 끝났으면 다 구워져 있다");
            float mb = SummonFxStyle.L("canopy_one_mb_rem") * PetSkillStyle.RemPx;
            Vector3[] gc = new Vector3[4], cc = new Vector3[4];
            ((RectTransform)grid).GetWorldCorners(gc); cp.GetWorldCorners(cc);
            float scale = UiRoot.Instance.App.lossyScale.y;
            // 정본 margin-bottom 이 음수 = 천개 바닥이 그리드 위선보다 **아래**(겹침) → 세계 y(위가 +)로는 바닥 − 그리드위 = −mb (런 444: 부호를 거꾸로 적어 +23.7 을 기다렸다 · 실측 −23.66)
            Assert.AreEqual(-mb * scale, cc[0].y - gc[1].y, 2f * scale + 0.5f, "천개 바닥 = 그리드 위 − 2.6rem(겹침 · 아래로)");

            // ⓓ 별 — 24개 · 앞 12 위 밴드(y ≤ 18%) · 뒤 12 아래 밴드(y ≥ 76%) · done 전 α 0
            Assert.AreEqual(Mathf.RoundToInt(SummonFxStyle.L("stars_n")), fx.StarCount, "별 24");
            if (!v.Done) Assert.AreEqual(0f, fx.StarsAlpha, 1e-6f, "done 전엔 별이 꺼져 있다(정본 .sr-stars opacity 0)");
            int top = 0, bottom = 0;
            foreach (Transform st in stars) { if (!st.name.StartsWith("star-")) continue; float ay = ((RectTransform)st).anchorMin.y; if (ay >= 1f - 0.19f) top++; else if (ay <= 1f - 0.75f) bottom++; }
            Assert.AreEqual(12, top, "위 밴드 12"); Assert.AreEqual(12, bottom, "아래 밴드 12");

            // ⓔ done — 탭으로 전부 공개 → 별이 켜진다(.6s) · 러너가 느려 이미 done 이면 탭은 곧 닫기라 안 누른다
            if (!v.Done) v.OnTap();
            float t = 0f;
            while (!v.Done && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(v.Done, "탭 뒤 done");
            Assert.IsTrue(fx.Done, "연출 겹도 done 을 받는다");
            t = 0f;
            while (t < 0.8f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(1f, fx.StarsAlpha, 0.05f, "done .6s 뒤 별 α 1");
            Capture("screen_t179-summon");   // 촬영 목록에 소환 결과 팝업이 없다(summon-rates 뿐) — T134·T138 처럼 이 자가 한 장 굽는다(눈 확인용)
            Debug.Log("[T179] one 판 · 천개 " + cp.rect.width.ToString("0") + "×" + cp.rect.height.ToString("0") + " · 광선 " + rays.rect.width.ToString("0") + " · 별 " + fx.StarCount);
            if (SkillSummonResultView.Current != null) SkillSummonResultView.Current.OnTap();
            yield return null;
        }

        [UnityTest]
        public IEnumerator 바닥_반사가_done_다음_프레임에_서고_자리_배율_층_마스크가_표대로다()
        {
            yield return Boot();
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            Sheet.Switch(SkillPetSheet.SubSkills);
            yield return null;
            while (Host.SummonMult("skill") != 1) Host.CycleSummonMult("skill");
            Host.Tickets = 10000;
            Host.Sync();
            yield return null;
            Sheet.Skills.SummonButton.onClick.Invoke();
            yield return null;
            SkillSummonResultView v = SkillSummonResultView.Current;
            Assert.IsNotNull(v, "결과 연출 팝업");
            SummonFx fx = v.Fx;
            Assert.IsNotNull(fx, "무대판(stage)이면 연출 겹이 선다");
            RectTransform body = (RectTransform)fx.transform;
            RectTransform grid = (RectTransform)body.Find("sr-grid");
            Transform floor = body.Find("sr-floor"), stars = body.Find("sr-stars");
            Assert.IsNotNull(grid); Assert.IsNotNull(floor); Assert.IsNotNull(stars);
            // ⓐ done 전엔 없다(정본은 finishSummonResult 에서 만든다) — 러너가 느려 벌써 done 이면 이 단언은 건너뛴다
            if (!v.Done) { Assert.IsNull(fx.Reflect, "done 전엔 반사가 없다"); v.OnTap(); }
            float t = 0f;
            while (!v.Done && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(v.Done, "탭 뒤 done");
            // ⓑ done 다음 프레임(들)에 선다 — 탭 프레임 자체는 안 늘린다(결정 516)
            for (int i = 0; i < 30 && fx.Reflect == null; i++) yield return null;
            RectTransform rf = fx.Reflect;
            Assert.IsNotNull(rf, "done 뒤 반사(.sr-reflect)가 선다");
            Assert.AreEqual("sr-reflect", rf.name);
            // ⓒ 층 사다리 — 바닥(10) < 반사(12) < 별(15)
            Assert.Less(floor.GetSiblingIndex(), rf.GetSiblingIndex(), "바닥(10) < 반사(12)");
            Assert.Less(rf.GetSiblingIndex(), stars.GetSiblingIndex(), "반사(12) < 별(15)");
            // ⓓ 자리 — 몸 폭(left 0 · right 0) · 복제 그리드 높이 · scaleY(−1.22) · 위 변 = 그리드 아래 − 8px(위로 겹침) · 아래로만 1.22배(정본 transform-origin 55% = 위 변 고정)
            //    런 459·463: 피벗을 위(0.5,1)에 두고 음수 배율을 걸어 상자가 위로 뒤집혔다(위 변이 그리드 아래보다 749px 위) — 피벗은 아래(0.5,0) = 이음선이어야 한다
            Assert.AreEqual(body.rect.width, rf.rect.width, 1f, "반사 폭 = 몸 폭");
            Assert.AreEqual(grid.rect.height, rf.rect.height, 1f, "반사 상자 높이 = 그리드 높이(복제)");
            Assert.AreEqual(-SummonFxStyle.L("reflect_sy"), rf.localScale.y, 1e-3f, "scaleY(−1.22)");
            Vector3[] gc = new Vector3[4], rc = new Vector3[4];
            grid.GetWorldCorners(gc); rf.GetWorldCorners(rc);
            // §0-6 보탬(런 1167) — 팝업은 열릴 때 `CardPop`(scale .7→1 · backOut)이 도는데 느린 러너에선 done 다음 프레임이 그 중간(1.0097)에 걸린다 —
            //   세계 자의 배율을 앱(`App.lossyScale`)이 아니라 그리드·반사의 **부모(몸)** 의 lossyScale 로 잰다(같은 프레임 · 같은 부모 · T152 «CardPop 중간값을 재는 자» 갈래).
            float scale = Mathf.Abs(body.lossyScale.y);
            float gridBottom = Mathf.Min(gc[0].y, gc[1].y), rfTop = Mathf.Max(rc[0].y, rc[1].y), rfBottom = Mathf.Min(rc[0].y, rc[1].y);
            float topPx = SummonFxStyle.L("reflect_top_px") * SummonFxStyle.L("css_px");
            Assert.AreEqual(topPx * scale, rfTop - gridBottom, 1.5f * scale + 0.5f, "반사 위 변 = 셀 줄 끝 − 8px(세계 y 로는 그리드 아래보다 8px 위 — 그 아래로 1.22배 · 위로 뒤집히면 749px)");
            Assert.Less(rfBottom, gridBottom, "반사는 그리드 **아래**로 자란다(피벗 아래 가운데 = 이음선)");
            Assert.AreEqual(grid.rect.height * SummonFxStyle.L("reflect_sy") * scale, rfTop - rfBottom, 2f * scale + 0.5f, "아래로 1.22배");
            // ⓔ 그림 — 그래픽 장치가 있으면 한 장이 찍혀 있다: 구체 픽셀 > 0 · 다는 아니다 · 원본 좌표 맨 아래 줄(96~100%)은 마스크로 α 0
            Image im = rf.GetComponent<Image>();
            Assert.IsNotNull(im);
            if (Forge.Game.Gallery.GallerySheet.GraphicsAvailable)
            {
                Assert.IsTrue(fx.ReflectBaked, "구운 반사 스프라이트");
                Texture2D tx = im.sprite.texture;
                Color32[] px = tx.GetPixels32();
                int W = tx.width, H = tx.height, lit = 0, litTop = 0;
                for (int i = 0; i < px.Length; i++) if (px[i].a > 12) { lit++; if (i / W >= H / 2) litTop++; }
                Assert.Greater(lit, 0, "구체가 찍혔다(복제 그리드 → RT → 픽셀)");
                Assert.Less(lit, W * H, "전부 칠해진 판이 아니다(배경 α 0)");
                Assert.Greater(litTop, 0, "구체는 셀 위쪽(원본 좌표 위 절반)에 있다");
                int bottomLit = 0;
                for (int x = 0; x < W; x++) if (px[x].a > 2) bottomLit++;
                Assert.AreEqual(0, bottomLit, "원본 좌표 맨 아래 줄은 마스크(96% → 0)로 투명");
                Debug.Log("[T179] 반사 판 " + W + "×" + H + " · 구체 픽셀 " + lit + " · 위 절반 " + litTop);
                // ⓕ srreflect .5s ease-out → α .88
                t = 0f;
                while (t < SummonFxStyle.L("reflect_in_ms") / 1000f + 0.25f) { t += Time.unscaledDeltaTime; yield return null; }
                Assert.AreEqual(SummonFxStyle.L("reflect_a"), im.color.a, 0.05f, "반사 α .88");
            }
            else Debug.Log("[T179] 그래픽 장치 없음 — 반사 상자만 확인했다");
            Capture("screen_t179-reflect");
            if (SkillSummonResultView.Current != null) SkillSummonResultView.Current.OnTap();
            yield return null;
        }

        /// <summary>UI 를 한 장 그린다(T135 `DamageVignetteTests.Capture` 와 같은 길) — 눈 확인용 · 실패해도 판정을 안 흔든다.</summary>
        static void Capture(string saveAs)
        {
            UiRoot root = UiRoot.Instance;
            Canvas canvas = root.Canvas;
            RenderMode prevMode = canvas.renderMode;
            Camera prevCam = canvas.worldCamera;
            float prevPlane = canvas.planeDistance;
            RenderTexture prevActive = RenderTexture.active;
            int w = Mathf.Max(64, Screen.width), h = Mathf.Max(64, Screen.height);
            RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            GameObject camGo = new GameObject("t179-pixel-cam");
            Camera cam = camGo.AddComponent<Camera>();
            try
            {
                if (Camera.main != null) cam.CopyFrom(Camera.main);
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.targetTexture = rt;
                cam.ResetProjectionMatrix();
                cam.cullingMask = 1 << canvas.gameObject.layer;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                root.Layout();
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
                tex.Apply(false);
                try { Forge.Game.Gallery.GallerySheet.Save(tex, saveAs); } catch (System.Exception e) { Debug.Log("[T179] PNG 저장 생략: " + e.Message); }
                Object.Destroy(tex);
            }
            finally
            {
                RenderTexture.active = prevActive;
                canvas.renderMode = prevMode;
                canvas.worldCamera = prevCam;
                canvas.planeDistance = prevPlane;
                root.Layout();
                cam.targetTexture = null;
                Object.Destroy(camGo);
                Object.Destroy(rt);
            }
        }
    
        /// <summary>T334 3회차 — 주역 와이프. 정본 `fireSummonHero` 748~757 이 못 박은 갈래다:
        /// 홀드백이면 전 화면 `.flash`, **아니면 `.wipe`**(주역 셀 중심 가산 원형). 전 화면 섬광을 쓰면
        /// «x75 는 위쪽 20셀이 같이 하얗게 떠 등급 구분이 무너진다» — 정본이 쓰면 안 된다고 적어 둔 자리다.</summary>
        /// <summary>
        /// <summary>T419 ⓐ — 정본 6148 `.sr-flash { mix-blend-mode: screen }`: 홀드백 착지의 전 화면 섬광은 **밝히는 겹**이다.
        /// 흰색일 땐 screen ≡ 보통 알파라 안 드러나지만 그 판에 **등급색**을 칠하는 순간 갈린다(정본은 밝히고, 재질이 없으면 그 색 막을 덮는다).
        /// 그래서 «판이 있다» 가 아니라 **그 판의 셰이더**를 묻는다.</summary>
        [UnityTest]
        public IEnumerator 홀드백_섬광_판은_스크린_합성으로_선다()
        {
            yield return Boot();
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            Sheet.Switch(SkillPetSheet.SubSkills);
            yield return null;
            while (Host.SummonMult("skill") != 1) Host.CycleSummonMult("skill");
            Host.Tickets = 10000;
            Host.Sync();
            yield return null;
            Sheet.Skills.SummonButton.onClick.Invoke();
            yield return null;
            SkillSummonResultView v = SkillSummonResultView.Current;
            Assert.IsNotNull(v, "소환 결과 창");
            Transform f = null;
            foreach (Transform x in v.GetComponentsInChildren<Transform>(true))
                if (x.name == "sr-flash") { f = x; break; }
            Assert.IsNotNull(f, "섬광 판(sr-flash)");
            Image img = f.GetComponent<Image>();
            Assert.IsNotNull(img, "섬광은 판 하나다");
            Assert.IsNotNull(img.material, "섬광 판 재질 — 정본 6148 mix-blend-mode: screen");
            Assert.AreEqual(CraftFxPoly.ScreenShaderName, img.material.shader.name,
                "정본 6148 `.sr-flash { mix-blend-mode: screen }` — 이 레포의 그 재질(Forge/UiScreen)");
            Assert.IsFalse(img.raycastTarget, "연출 겹은 탭을 먹지 않는다");
            Debug.Log("[T419] 섬광 판 셰이더 " + img.material.shader.name);
        }

        /// T419 1회차 — 정본 5912 `.sr-canopy b::after`: 스필(b) 위를 훑는 빛띠는 `mix-blend-mode: screen`(이 레포의 그 재질 = `Forge/UiScreen`) · opacity .9 ·
        /// srsweep 2.2s linear 무한(translateX −115% → 115%). 띠는 스필의 자식(세로 마스크·호흡을 같이 받는다) · 스필 상자 밖은 RectMask2D 가 자른다.
        /// </summary>
        [UnityTest]
        public IEnumerator 스필_위_빛띠는_스크린_합성으로_스필_폭을_훑는다()
        {
            yield return Boot();
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            Sheet.Switch(SkillPetSheet.SubSkills);
            yield return null;
            while (Host.SummonMult("skill") != 1) Host.CycleSummonMult("skill");
            Host.Tickets = 10000;
            Host.Sync();
            yield return null;
            Sheet.Skills.SummonButton.onClick.Invoke();
            yield return null;
            SkillSummonResultView v = SkillSummonResultView.Current;
            Assert.IsNotNull(v);
            SummonFx fx = v.Fx;
            Assert.IsNotNull(fx, "무대판(stage)이면 연출 겹이 선다");
            fx.Bake();
            RectTransform spill = (RectTransform)fx.Canopy.Find("spill");
            Assert.IsNotNull(spill, "스필(b)");
            Assert.IsNotNull(spill.GetComponent<RectMask2D>(), "스필 상자 밖은 자른다(CSS mask-clip)");
            Transform sw = spill.Find("sr-sweep");
            Assert.IsNotNull(sw, "빛띠(b::after)");
            Image img = sw.GetComponent<Image>();
            Assert.IsNotNull(img.material, "빛띠 재질");
            Assert.AreEqual(CraftFxPoly.ScreenShaderName, img.material.shader.name, "정본 mix-blend-mode: screen — 이 레포의 그 재질");
            Assert.IsFalse(img.raycastTarget);
            Assert.IsNotNull(img.sprite, "구운 띠");
            // 띠 한 장 — 아래 행(마스크 1)의 최대 알파 = 심색 알파(.72) · 알파가 있는 화소 비율 ≈ 스톱 폭(70% − 34%)
            // 구운 판은 Apply(…, makeNoLongerReadable) 라 CPU 로 못 읽는다(런 898 ArgumentException) — 와이프 자와 같이 GPU 에서 되읽는다.
            Texture2D tex = Readable(img.sprite.texture);
            int W = tex.width, H = tex.height;
            Color32[] px = tex.GetPixels32();
            int lit = 0; byte maxA = 0;
            for (int x = 0; x < W; x++) { byte a = px[x].a; if (a > 0) lit++; if (a > maxA) maxA = a; }
            float[] st = SummonFxStyle.Arr("sweep_stops");
            Assert.AreEqual(SummonFxStyle.L("sweep_core_a") * 255f, maxA, 3f, "심(52%) 알파 .72");
            Assert.AreEqual(st[4] - st[0], (float)lit / W, 0.06f, "띠 폭 = 34%~70%");
            Assert.AreEqual(0, px[(H - 1) * W].a, "위 행(마스크 0)은 투명 — b 의 세로 마스크를 같이 받는다");
            // 움직임 — 2.2s 에 −115% → 115%(스필 폭 기준) · 선형
            yield return null;
            float x0 = img.rectTransform.anchoredPosition.x, w = spill.rect.width, tr = SummonFxStyle.L("sweep_travel_f");
            float t = 0f;
            while (t < 0.3f) { t += Time.unscaledDeltaTime; yield return null; }
            float x1 = img.rectTransform.anchoredPosition.x;
            Assert.AreNotEqual(x0, x1, "띠가 움직인다");
            Assert.LessOrEqual(Mathf.Abs(x0), tr * w + 1f); Assert.LessOrEqual(Mathf.Abs(x1), tr * w + 1f);
            float per = SummonFxStyle.L("sweep_period_s"), dxExpect = 2f * tr * w * (t / per);
            if (x1 > x0) Assert.AreEqual(dxExpect, x1 - x0, tr * w * 0.15f, "선형 속도 = 2·115%·폭 / 2.2s");
            Assert.AreEqual(SummonFxStyle.L("sweep_a") * spill.GetComponent<Image>().color.a, img.color.a, 0.02f, "opacity .9 × b 의 호흡");
        }

        [UnityTest]
        public IEnumerator 주역_와이프는_가산_혼합이고_정본_네_스톱대로_굽는다()
        {
            yield return Boot();

            // 굽기: 등급색이 들어가므로 등급마다 한 장 · 같은 색이면 같은 참조(캐시)
            Color rc = new Color(1f, 0.11f, 0.11f, 1f);
            Sprite a = SummonFx.BakeWipe("sr-wipe-test", rc);
            Sprite b = SummonFx.BakeWipe("sr-wipe-test", rc);
            Assert.IsNotNull(a, "와이프를 못 구웠다");
            Assert.AreSame(a, b, "같은 이름이면 한 번만 굽는다");

            Texture2D t = Readable(a.texture);
            int w = t.width, h = t.height;
            float s1 = SummonFxStyle.L("wipe_stop1_f"), s2 = SummonFxStyle.L("wipe_stop2_f"), s3 = SummonFxStyle.L("wipe_stop3_f");
            Assert.AreEqual(0.24f, s1, 1e-4f, "정본 24%");
            Assert.AreEqual(0.52f, s2, 1e-4f, "정본 52%");
            Assert.AreEqual(0.82f, s3, 1e-4f, "정본 82%");

            // 가운데는 흰색이고 불투명 · 24% 자리는 등급색 · 82% 밖은 투명(정본 네 스톱)
            Color mid = t.GetPixel(w / 2, h / 2);
            Assert.Greater(mid.a, 0.9f, "가운데는 불투명해야 한다");
            Assert.Greater(Mathf.Min(mid.r, mid.g, mid.b), 0.9f, "가운데는 흰색이다(정본 #fff 0%)");

            Color at24 = t.GetPixel(w / 2 + Mathf.RoundToInt(w / 2f * s1), h / 2);
            Assert.Greater(at24.r - at24.g, 0.3f, "24% 자리는 등급색이 실려야 한다(붉은 등급)");

            Color outside = t.GetPixel(w / 2 + Mathf.RoundToInt(w / 2f * 0.95f), h / 2);
            Assert.Less(outside.a, 0.02f, "82% 밖은 투명하다");

            // 바깥으로 갈수록 옅어진다(24% → 52% → 82%)
            float aIn = t.GetPixel(w / 2 + Mathf.RoundToInt(w / 2f * 0.30f), h / 2).a;
            float aMid = t.GetPixel(w / 2 + Mathf.RoundToInt(w / 2f * 0.60f), h / 2).a;
            Assert.Greater(aIn, aMid, "안쪽이 바깥보다 진하다");

            // 혼합 — 정본 `mix-blend-mode: screen`
            Material sm = CraftFxPoly.Screen();
            if (sm != null) Assert.AreEqual(CraftFxPoly.ScreenShaderName, sm.shader.name, "가산(스크린) 셰이더여야 한다");
        }

        /// <summary>
        /// 구운 한 장의 화소를 읽는다 — `SummonFx` 는 <c>Apply(false, true)</c> 로 구워 **CPU 사본을 버린다**(메모리).
        /// 그래서 `GetPixel` 은 런 503 에서처럼 «is not readable» 로 넘어진다. GPU 로 한 번 베껴 읽으면 화소는 같다(T342 와 같은 길).
        /// ⚠ 굽는 쪽을 읽기 가능으로 되돌리지 말 것 — 이 판들은 화면에만 쓰이고, 사본을 남기면 그만큼 메모리가 는다.
        /// </summary>
        static Texture2D Readable(Texture2D src)
        {
            if (src.isReadable) return src;
            RenderTexture rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture prev = RenderTexture.active;
            try
            {
                Graphics.Blit(src, rt);
                RenderTexture.active = rt;
                var copy = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0f, 0f, src.width, src.height), 0, 0, false);
                copy.Apply(false, false);
                return copy;
            }
            finally { RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt); }
        }

        /// <summary>T449 — 소환진(`.sr-floor`) 상자의 폭·비율이 표(`SummonFxUi.json` floor_*)에서 온다(정본 5771 one 64%/2.6 · 5800 그 밖 88%/2.5).
        /// 값은 종전 코드와 같으니 화면은 안 움직인다 — 자가 지키는 것은 «수가 코드에 안 박혀 있다» 다. x1(one)과 x5(stage · one 아님) 둘 다 잰다.</summary>
        [UnityTest]
        public IEnumerator 소환진_상자의_폭과_비율은_표에서_온다_x1_과_x5_둘_다()
        {
            yield return Boot();
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            Sheet.Switch(SkillPetSheet.SubSkills);
            yield return null;
            Host.Tickets = 10000;
            foreach (int mult in new[] { 1, 5 })
            {
                while (Host.SummonMult("skill") != mult) Host.CycleSummonMult("skill");
                Host.Sync();
                yield return null;
                Sheet.Skills.SummonButton.onClick.Invoke();
                yield return null;
                SkillSummonResultView v = SkillSummonResultView.Current;
                Assert.IsNotNull(v, "결과 연출 팝업(x" + mult + ")");
                Assert.AreEqual(mult, v.CellCount, "x" + mult + " = 셀 " + mult);
                SummonFx fx = v.Fx;
                Assert.IsNotNull(fx, "무대판(stage · n<=10)이면 연출 겹이 선다");
                RectTransform body = (RectTransform)fx.transform;
                RectTransform grid = (RectTransform)body.Find("sr-grid"), floor = (RectTransform)body.Find("sr-floor");
                Assert.IsNotNull(grid); Assert.IsNotNull(floor, "소환진");
                string k = mult == 1 ? "floor_one_" : "floor_";
                float gw = grid.rect.width;
                Assert.AreEqual(gw * SummonFxStyle.L(k + "w_f"), floor.rect.width, 1f, "소환진 폭 = 그리드 폭 × " + k + "w_f");
                Assert.AreEqual(floor.rect.width / SummonFxStyle.L(k + "aspect"), floor.rect.height, 1f, "소환진 높이 = 폭 / " + k + "aspect");
                RectTransform ticks = (RectTransform)floor.Find("sr-floor-ticks");
                Assert.IsNotNull(ticks, "룬 눈금 띠는 소환진과 같은 상자");
                Assert.AreEqual(floor.rect.width, ticks.rect.width, 0.5f); Assert.AreEqual(floor.rect.height, ticks.rect.height, 0.5f);
                v.Close();
                yield return null;
                yield return null;
            }
        }
    
        /// <summary>T448 — 동급(peer) 셀의 착지 링(정본 6710~6716 `.sr-cell.peer.on::after`): 동급 셀마다 셀 뒤(z −1) 폭 100% 정사각 링이 서고,
        /// 가산(screen) 재질이며, 주역 링과 **다른 스프라이트**(정지점 60/76/84/95 · 흰 .7)다. 자기 착지 뒤 .58s 가 지나면 알파 0 · 배율 `--ringmax` 1.5.
        /// 동급 = «주역이 아닌데 최고 등급» 이라 판을 직접 짠다(SummonChargeTests 와 같은 길) — 조연 둘 + 동급 둘 + 주역(마지막) 하나.</summary>
        [UnityTest]
        public IEnumerator 동급_셀은_자기_착지에_축소판_링을_한_번_돌린다()
        {
            yield return Boot();
            var list = new List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = "가" },
                new SkillSummonResultView.Entry { Key = "sk:b", IconKey = "sk_fireball", Rarity = "rare", Name = "나" },
                new SkillSummonResultView.Entry { Key = "sk:c", IconKey = "sk_fireball", Rarity = "ultimate", Name = "다" },
                new SkillSummonResultView.Entry { Key = "sk:d", IconKey = "sk_fireball", Rarity = "ultimate", Name = "라" },
                new SkillSummonResultView.Entry { Key = "sk:e", IconKey = "sk_fireball", Rarity = "ultimate", Name = "마" },
            };
            SkillSummonResultView v = SkillSummonResultView.Open(Sheet, "skill", list, "ultimate", null);
            yield return null;
            Assert.IsNotNull(v, "결과 연출 팝업");
            Assert.AreEqual(5, v.CellCount);
            List<Image> rings = v.PeerRings;
            Assert.AreEqual(2, rings.Count, "최고 등급 셋 중 주역(마지막) 하나를 뺀 둘이 동급 — 링도 둘");
            Assert.IsNotNull(v.HeroRing, "동급이 있으면 주역도 있다");
            float smax = v.RingMaxPeer;
            Assert.AreEqual(1.5f, smax, 1e-6f, "정본 6702 `.sr-cell.peer { --ringmax: 1.5 }`");
            Assert.Less(smax, v.RingMax, "동급 링은 주역 링보다 작게 퍼진다(축소판)");
            foreach (Image r in rings)
            {
                RectTransform rt = r.rectTransform;
                Assert.AreEqual("sr-peerring", rt.name);
                Assert.AreEqual(0, rt.GetSiblingIndex(), "링은 셀 뒤(z −1)");
                Assert.AreEqual(rt.rect.width, rt.rect.height, 0.01f, "정사각(aspect-ratio: 1)");
                Assert.AreEqual(((RectTransform)rt.parent).rect.width, rt.rect.width, 0.5f, "폭 100%");
                Assert.IsNotNull(r.sprite, "구운 링 한 장");
                Assert.AreNotEqual(v.HeroRing.sprite, r.sprite, "주역 링과 다른 판(정지점·흰 알파가 다르다)");
                Assert.IsNotNull(r.material, "가산 재질");
                Assert.AreEqual(CraftFxPoly.ScreenShaderName, r.material.shader.name, "정본 6712 mix-blend-mode: screen");
            }
            // 주역 셀에는 동급 링이 없다(정본 `.peer` 는 `.heroic` 과 겹치지 않는다).
            Assert.IsNull(v.HeroRing.transform.parent.Find("sr-peerring"), "주역 셀엔 동급 링이 없다");
            // 전부 착지하고 링 길이(.58s)가 지나면 링은 사라지고 최대 배율에 선다.
            float t = 0f;
            while (!v.Done && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(v.Done, "done");
            float wait = SummonFxStyle.H("peerring_ms") / 1000f + 0.2f;
            for (float w = 0f; w < wait; w += Time.unscaledDeltaTime) yield return null;
            foreach (Image r in rings)
            {
                Assert.AreEqual(0f, r.color.a, 1e-3f, "착지 링은 한 번 돌고 사라진다(forwards · 알파 0)");
                Assert.AreEqual(smax, r.rectTransform.localScale.x, 1e-3f, "끝 배율 = --ringmax 1.5");
            }
            v.Close();
            yield return null;
        }

        /// <summary>T475 — 정본 6704 `.sr-cell.peer .sr-orbwrap { outline: .11rem solid rgba(255,255,255,.32); outline-offset: .11rem }`: 동급 둘에만 흰 고리가 서고
        /// 주역(ultimate · 4등급)·아래 등급엔 없다. 고리 지름 = 래퍼 + 2 × (오프셋 + 폭) · 알파 = 표.</summary>
        [UnityTest]
        public IEnumerator 동급_셀_래퍼에만_흰_반투명_아웃라인이_서고_주역과_아래_등급엔_없다()
        {
            yield return Boot();
            var list = new List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = "가" },
                new SkillSummonResultView.Entry { Key = "sk:b", IconKey = "sk_fireball", Rarity = "rare", Name = "나" },
                new SkillSummonResultView.Entry { Key = "sk:c", IconKey = "sk_fireball", Rarity = "ultimate", Name = "다" },
                new SkillSummonResultView.Entry { Key = "sk:d", IconKey = "sk_fireball", Rarity = "ultimate", Name = "라" },
                new SkillSummonResultView.Entry { Key = "sk:e", IconKey = "sk_fireball", Rarity = "ultimate", Name = "마" },
            };
            SkillSummonResultView v = SkillSummonResultView.Open(Sheet, "skill", list, "ultimate", null);
            yield return null;
            Assert.IsNotNull(v, "결과 연출 팝업");
            Assert.IsNull(v.OutlineOf(0), "일반 — 고리 없음"); Assert.IsNull(v.OutlineOf(1), "희귀 — 고리 없음");
            Assert.IsNull(v.OutlineOf(4), "주역(ultimate · 4등급)은 동급이 아니고 신화도 아니다 — 고리 없음");
            float w = PetSkillStyle.Px("sr_peer_outline_w_rem"), off = PetSkillStyle.Px("sr_peer_outline_off_rem");
            Assert.AreEqual(0.11f, PetSkillStyle.L("sr_peer_outline_w_rem"), 1e-6f, "정본 6704 .11rem");
            Assert.AreEqual(0.32f, PetSkillStyle.L("sr_peer_outline_a"), 1e-6f, "정본 6704 알파 .32");
            for (int i = 2; i <= 3; i++)
            {
                Image ol = v.OutlineOf(i);
                Assert.IsNotNull(ol, "동급 " + i + " 의 고리");
                Assert.AreEqual("sr-outline", ol.name);
                RectTransform wrap = (RectTransform)ol.transform.parent;
                Assert.AreEqual("sr-orbwrap", wrap.name, "고리는 래퍼의 자식(래퍼 배율을 따른다)");
                Assert.AreEqual(wrap.rect.width + 2f * (off + w), ol.rectTransform.rect.width, 0.5f, "지름 = 래퍼 + 2 × (오프셋 + 폭)");
                Assert.AreEqual(ol.rectTransform.rect.width, ol.rectTransform.rect.height, 0.01f, "정사각");
                Assert.AreEqual(0.32f, ol.color.a, 1e-4f, "알파 = 표");
                Assert.IsNotNull(ol.sprite, "구운 고리 한 장");
                Transform glow = wrap.Find("glow"), orb = wrap.Find("sr-orb");
                Assert.IsNotNull(glow, "동급은 광채가 있다");
                Assert.Greater(ol.transform.GetSiblingIndex(), glow.GetSiblingIndex(), "outline 은 광채(box-shadow) 위");
                Assert.Less(ol.transform.GetSiblingIndex(), orb.GetSiblingIndex(), "구슬 본체 아래");
            }
        }

        /// <summary>T475 — 정본 6691 `.sr-cell[data-tier="5"] .sr-orbwrap { outline: .12rem … .34 }`: 신화 주역 하나뿐인 판에서도 고리가 선다(동급이 아니라 신화 값).</summary>
        [UnityTest]
        public IEnumerator 신화_셀은_동급이_아니어도_신화_값의_흰_아웃라인이_선다()
        {
            yield return Boot();
            var list = new List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = "가" },
                new SkillSummonResultView.Entry { Key = "sk:b", IconKey = "sk_fireball", Rarity = "mythic", Name = "나" },
            };
            SkillSummonResultView v = SkillSummonResultView.Open(Sheet, "skill", list, "mythic", null);
            yield return null;
            Assert.IsNotNull(v, "결과 연출 팝업");
            Assert.IsNull(v.OutlineOf(0), "일반 — 고리 없음");
            Image ol = v.OutlineOf(1);
            Assert.IsNotNull(ol, "신화 셀의 고리(6691)");
            Assert.AreEqual(0.12f, PetSkillStyle.L("sr_outline5_w_rem"), 1e-6f, "정본 6691 .12rem");
            Assert.AreEqual(0.34f, ol.color.a, 1e-4f, "정본 6691 알파 .34 — 동급 값(.32)이 아니다");
            float w = PetSkillStyle.Px("sr_outline5_w_rem"), off = PetSkillStyle.Px("sr_outline5_off_rem");
            RectTransform wrap = (RectTransform)ol.transform.parent;
            Assert.AreEqual(wrap.rect.width + 2f * (off + w), ol.rectTransform.rect.width, 0.5f, "지름 = 래퍼 + 2 × (.12 + .12)rem");
        }
    
        /// <summary>T480 ⓐ — 일반 셀 착지 링(정본 6358~6363 `.sr-orbwrap::after` + `srring` .55s): **모든** 셀의 래퍼 안 맨 위에 inset 0 정사각 링이 서고,
        /// 가산(screen) 재질이며 동급·주역 링과 다른 판이다. 자기 착지 뒤 .55s 가 지나면 알파 0 · 배율 `1.5 + .8 × --glow`(등급마다 다르다).</summary>
        [UnityTest]
        public IEnumerator 모든_셀은_자기_착지에_등급색_착지_링을_한_번_번지고_끝_배율은_glow_계단이다()
        {
            yield return Boot();
            var list = new List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = "가" },
                new SkillSummonResultView.Entry { Key = "sk:b", IconKey = "sk_fireball", Rarity = "rare", Name = "나" },
                new SkillSummonResultView.Entry { Key = "sk:c", IconKey = "sk_fireball", Rarity = "ultimate", Name = "다" },
            };
            SkillSummonResultView v = SkillSummonResultView.Open(Sheet, "skill", list, "ultimate", null);
            yield return null;
            Assert.IsNotNull(v, "결과 연출 팝업");
            Assert.AreEqual(3, v.CellCount);
            List<Image> rings = v.LandRings;
            Assert.AreEqual(3, rings.Count);
            for (int i = 0; i < rings.Count; i++)
            {
                Image r = rings[i];
                Assert.IsNotNull(r, "셀 " + i + " 착지 링");
                RectTransform rt = r.rectTransform;
                Assert.AreEqual("sr-landring", rt.name);
                RectTransform wrap = (RectTransform)rt.parent;
                Assert.AreEqual("sr-orbwrap", wrap.name, "래퍼 안(정본 ::after)");
                Assert.AreEqual(wrap.childCount - 1, rt.GetSiblingIndex(), "래퍼 자식 중 맨 위(::after)");
                Assert.AreEqual(wrap.rect.width, rt.rect.width, 0.5f, "inset 0 — 폭 100%");
                Assert.AreEqual(rt.rect.width, rt.rect.height, 0.01f, "정사각");
                Assert.IsNotNull(r.sprite, "구운 링 한 장");
                Assert.IsNotNull(r.material, "가산 재질");
                Assert.AreEqual(CraftFxPoly.ScreenShaderName, r.material.shader.name, "정본 6360 mix-blend-mode: screen");
                // T486 — «켜지기 전» 은 프레임이 아니라 **상태**(셀의 On)로 가른다(결정 775 의 약): 배치 러너의 첫 프레임이 150~200ms 면
                //   이 줄에 오기 전에 첫 셀이 이미 착지해 링이 .32(정본 6358 시작 알파)로 켜져 있다(런 1251 실측 · 같은 코드가 런 1248 에선 PASS).
                //   착지 전인 셀만 «알파 0» 을 묻고, 이미 착지한 셀의 «한 번 번지고 사라짐 · 끝 배율» 은 아래 Done + LandRing.Ms 블록이 그대로 잰다.
                if (!v.CellOn(i)) Assert.AreEqual(0f, r.color.a, 1e-3f, "켜지기 전엔 알파 0(셀 " + i + " 착지 전)");
            }
            Assert.AreNotEqual(rings[0].sprite, v.HeroRing.sprite, "주역 링과 다른 판(정지점 62/78/92)");
            Assert.AreEqual(1.5f, v.LandRingScaleEnd(0), 1e-4f, "일반: 1.5 + .8 × 0");
            Assert.AreEqual(1.5f + 0.8f * 0.8f, v.LandRingScaleEnd(2), 1e-4f, "궁극: 1.5 + .8 × .8");
            Assert.Greater(v.LandRingScaleEnd(2), v.LandRingScaleEnd(0), "임팩트는 등급에 비례한다(정본 주석)");
            float t = 0f;
            while (!v.Done && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(v.Done, "done");
            float wait = SummonFxStyle.LandRing == null ? 1f : (float)SummonFxStyle.LandRing.Ms / 1000f + 0.2f;
            for (float w = 0f; w < wait; w += Time.unscaledDeltaTime) yield return null;
            for (int i = 0; i < rings.Count; i++)
            {
                Assert.AreEqual(0f, rings[i].color.a, 1e-3f, "착지 링은 한 번 번지고 사라진다(forwards · 알파 0)");
                Assert.AreEqual(v.LandRingScaleEnd(i), rings[i].rectTransform.localScale.x, 1e-3f, "끝 배율 = 1.5 + .8 × --glow");
            }
            v.Close();
            yield return null;
        }

        /// <summary>T480 ⓑ — 고등급 셀 회전 광선(정본 6680~6688 `.sr-ray`): hi(전설·궁극·신화) 셀에만 래퍼 **첫** 자식(구슬 뒤)으로 폭 1.32 배 상자가 서고,
        /// 착지 뒤 .5s 에 `--ray`(궁극 .6) 로 밝아진 채 머물며 각은 계속 돈다(3.6s 에 한 바퀴). 일반·희귀 셀엔 없다.</summary>
        [UnityTest]
        public IEnumerator 고등급_셀에만_회전_광선이_구슬_뒤에_서고_착지_뒤_ray_알파로_계속_돈다()
        {
            yield return Boot();
            var list = new List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = "가" },
                new SkillSummonResultView.Entry { Key = "sk:b", IconKey = "sk_fireball", Rarity = "rare", Name = "나" },
                new SkillSummonResultView.Entry { Key = "sk:c", IconKey = "sk_fireball", Rarity = "ultimate", Name = "다" },
            };
            SkillSummonResultView v = SkillSummonResultView.Open(Sheet, "skill", list, "ultimate", null);
            yield return null;
            Assert.IsNotNull(v, "결과 연출 팝업");
            List<Image> rays = v.Rays;
            Assert.AreEqual(3, rays.Count);
            Assert.IsNull(rays[0], "일반 셀엔 광선이 없다(정본 `.sr-cell.hi` 만 배경을 준다)");
            Assert.IsNull(rays[1], "희귀 셀에도 없다");
            Image ray = rays[2];
            Assert.IsNotNull(ray, "궁극 셀의 광선");
            RectTransform rt = ray.rectTransform;
            Assert.AreEqual("sr-ray", rt.name);
            RectTransform wrap = (RectTransform)rt.parent;
            Assert.AreEqual("sr-orbwrap", wrap.name);
            // 정본 DOM 은 광선(431) → 잔상 순이라 둘 다 구슬 뒤다. 클론은 잔상 자(T334 15회차 · SummonChargeTests 333)가 «잔상 = 첫 자식» 을 쥐므로
            //   광선은 그 바로 위(1)에 둔다 — 둘 다 구슬·광채 아래인 것이 정본과 같은 점이다(결정 804).
            Assert.AreEqual(1, rt.GetSiblingIndex(), "래퍼 앞쪽 — 잔상(첫 자식) 바로 위 · 구슬 뒤(ui.js 431)");
            Transform glowT = wrap.Find("glow");
            Assert.IsNotNull(glowT, "궁극 셀엔 광채 원판이 있다");
            Assert.Less(rt.GetSiblingIndex(), glowT.GetSiblingIndex(), "광선은 광채·구슬 아래");
            Assert.AreEqual(wrap.rect.width * 1.32f, rt.rect.width, 0.5f, "inset −16% → 폭 1.32");
            Assert.AreEqual(rt.rect.width, rt.rect.height, 0.01f, "정사각(50% 원)");
            Assert.IsNotNull(ray.sprite, "살 + 마스크를 한 장에");
            Assert.AreEqual(0f, ray.color.a, 1e-3f, "켜지기 전엔 알파 0");
            Assert.AreEqual(0.6f, v.RayAlphaEnd(2), 1e-4f, "궁극 --ray .6");
            float t = 0f;
            while (!v.Done && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(v.Done, "done");
            float wait = (float)SummonFxStyle.Ray.FadeMs / 1000f + 0.2f;
            for (float w = 0f; w < wait; w += Time.unscaledDeltaTime) yield return null;
            Assert.AreEqual(0.6f, ray.color.a, 1e-3f, "srrayfade forwards — --ray 에 머문다");
            float z0 = rt.localRotation.eulerAngles.z;
            for (float w = 0f; w < 0.3f; w += Time.unscaledDeltaTime) yield return null;
            float z1 = rt.localRotation.eulerAngles.z;
            Assert.AreNotEqual(z0, z1, "srrayspin infinite — 각이 계속 바뀐다");
            Assert.AreEqual(0.6f, ray.color.a, 1e-3f, "돌아도 알파는 그대로");
            v.Close();
            yield return null;
        }
    }
}
