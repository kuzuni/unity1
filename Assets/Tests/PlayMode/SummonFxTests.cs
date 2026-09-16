using System.Collections;
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
            float scale = UiRoot.Instance.App.lossyScale.y;
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
}
}
