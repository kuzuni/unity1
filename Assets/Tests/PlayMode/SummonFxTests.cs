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
            // T501 — 도입의 **배율 원점**이 가운데인가. 정본 5839 `srcanopy` 엔 `transform-origin` 선언이 없어 CSS 기본 `50% 50%` 다.
            //   ⚑ 잣대를 **프레임이 아니라 규칙**에 건다(T422 의 길) — 도입 한가운데 프레임을 잡으러 가면 러너 프레임 간격에 걸려 펄럭인다.
            //   ⓐ 피벗이 가운데다 ⓑ 손으로 도입 배율을 먹여도 **가운데가 안 움직인다** ⓒ 배율 1 의 자리는 그대로다(정적 회귀 막이).
            Assert.AreEqual(0.5f, cp.pivot.x, 1e-4f, "천개 피벗 x 가 가운데(정본엔 transform-origin 이 없다 = 50% 50%)");
            Assert.AreEqual(0.5f, cp.pivot.y, 1e-4f, "천개 피벗 y 가 가운데");
            Vector3[] c1 = new Vector3[4], c2 = new Vector3[4];
            cp.GetWorldCorners(c1);
            float midX1 = (c1[0].x + c1[2].x) * 0.5f, midY1 = (c1[0].y + c1[2].y) * 0.5f;
            Vector3 keepScale = cp.localScale;
            cp.localScale = Vector3.one * SummonFxStyle.L("canopy_in_scale");
            Canvas.ForceUpdateCanvases();
            cp.GetWorldCorners(c2);
            float midX2 = (c2[0].x + c2[2].x) * 0.5f, midY2 = (c2[0].y + c2[2].y) * 0.5f;
            cp.localScale = keepScale;
            Canvas.ForceUpdateCanvases();
            float appS = UiRoot.Instance.App.lossyScale.y;
            Assert.AreEqual(midX1, midX2, 1f * appS + 0.5f,
                            "도입 배율에서도 천개 **가운데 x** 가 제자리다(좌상 피벗이면 상자 폭의 10% 왼쪽으로 밀린다 · T501)");
            Assert.AreEqual(midY1, midY2, 1f * appS + 0.5f, "가운데 y 도 제자리다");
            // 정적 자리 회귀 막이 — 배율 1 의 천개 가운데 x 는 그리드 가운데 x 와 같다(둘 다 몸 가운데 정렬)
            Vector3[] gc2 = new Vector3[4];
            ((RectTransform)grid).GetWorldCorners(gc2);
            Assert.AreEqual((gc2[0].x + gc2[2].x) * 0.5f, midX1, 1f * appS + 0.5f, "배율 1 에서 천개 가운데 = 그리드 가운데");
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
        static void Capture(string saveAs) { PlayShot.Capture(saveAs); }
    
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
    
        /// <summary>
        /// T33 93회차 — **셀 자신(`.sr-cell`) 문맥**의 층 차례. 92회차가 «흐름 안 블록이 섞여 정수 한 칸으로
        /// 못 적는다» 로 넘긴 자리인데 **그 전제가 틀렸다**: 까닭으로 든 «`.sr-name` 은 position 도 z 도 없다» 가
        /// 사실이 아니다 — 정본 `.sr-name` 블록은 열두 줄이고 **7043** 에 `position: relative; z-index: 3` 이 있다.
        /// 그래서 셀 문맥도 정수 사다리고 층은 셋이다: «광주 `::before` · 동급 링 · 주역 링 **−1**»(6334·6709·6754)
        /// → «구슬 래퍼 **2**»(6342) → «이름판·등급 칩 **3**»(7043·7060).
        /// ⚑ 이 표에 **음수 층**이 있어서 묻는 자도 새로 났다(`TryZOrderOfCellOwn`) — 옛 자는 «표에 없다» 를
        ///   −1 로 돌려줘서 −1 층과 구별이 안 되고, 그래서 사다리가 그 셋을 통째로 **안 세우고 지나갔다**.
        /// ⚑ 같은 −1 층 안의 차례는 정본도 **문서 순서**로 갈린다(`::before` 광주 → `::after` 링) — 손으로
        ///   `SetAsFirstSibling()` 하던 둘을 걷어 **만든 차례**가 그 순서를 내게 했다.
        /// </summary>
        [UnityTest]
        public IEnumerator 셀_제_문맥의_형제_차례가_정본_z_의_비감소다()
        {
            // 먼저 표의 전제 — 이 셋이 흔들리면 위 글이 거짓이 된다(92회차가 틀린 자리가 여기다).
            int zName, zWrap, zCol;
            Assert.IsTrue(SummonFxStyle.TryZOrderOfCellOwn("sr-name", out zName), "표에 `sr-name` 이 있다");
            Assert.AreEqual(3, zName, "정본 7043 `.sr-name { position: relative; z-index: 3 }` — 흐름 안 블록이 아니다");
            Assert.IsTrue(SummonFxStyle.TryZOrderOfCellOwn("sr-orbwrap", out zWrap), "표에 `sr-orbwrap` 이 있다");
            Assert.AreEqual(2, zWrap, "정본 6342 `.sr-orbwrap { z-index: 2 }`");
            Assert.IsTrue(SummonFxStyle.TryZOrderOfCellOwn("sr-glow-col", out zCol),
                "표에 `sr-glow-col` 이 **있다** — 옛 자는 이 −1 을 «표에 없다» 와 구별 못 했다");
            Assert.AreEqual(-1, zCol, "정본 6334 `.sr-cell::before { z-index: -1 }`");

            yield return Boot();
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            Sheet.Switch(SkillPetSheet.SubSkills);
            yield return null;
            Host.Tickets = 10000;
            Sheet.Skills.SummonButton.onClick.Invoke();
            yield return null;
            yield return null;
            SkillSummonResultView v = SkillSummonResultView.Current;
            Assert.IsNotNull(v, "결과 연출 팝업");

            int cells = 0, checkedPairs = 0, sawWrapThenName = 0;
            foreach (RectTransform cell in v.GetComponentsInChildren<RectTransform>(true))
            {
                if (!cell.name.StartsWith("sr-cell-")) continue;
                cells++;
                var kids = new List<KeyValuePair<int, string>>();
                for (int i = 0; i < cell.childCount; i++)
                {
                    Transform t = cell.GetChild(i);
                    int z;
                    if (!SummonFxStyle.TryZOrderOfCellOwn(t.name, out z)) continue;   // 표에 없는 상자(T536 의 광창 등)
                    kids.Add(new KeyValuePair<int, string>(t.GetSiblingIndex(), t.name));
                }
                kids.Sort((x, y) => x.Key.CompareTo(y.Key));
                for (int i = 1; i < kids.Count; i++)
                {
                    int zPrev, zCur;
                    SummonFxStyle.TryZOrderOfCellOwn(kids[i - 1].Value, out zPrev);
                    SummonFxStyle.TryZOrderOfCellOwn(kids[i].Value, out zCur);
                    checkedPairs++;
                    Assert.LessOrEqual(zPrev, zCur,
                        "셀 «" + cell.name + "» 안에서 «" + kids[i - 1].Value + "»(정본 z " + zPrev + ") 가 «"
                        + kids[i].Value + "»(z " + zCur + ") 보다 **앞** 형제다 — 앞 형제가 아래에 그려지므로 층이 높은 쪽이 아래로 깔렸다");
                    // 이 짝이 이 회차의 과녁이다 — 구슬 래퍼(2) 바로 뒤에 이름판(3)이 와야 «래퍼가 이름판 아래» 다.
                    if (kids[i - 1].Value == "sr-orbwrap" && kids[i].Value == "sr-name") sawWrapThenName++;
                }
                // 세우는 자가 실제로 돌았나 — 다시 불러 움직일 것이 없어야 한다(91·92회차와 같은 자)
                Assert.AreEqual(0, SummonFxStyle.OrderLadder(cell, "z_order_cell_own"),
                    "셀 «" + cell.name + "» 의 형제 차례가 셀 문맥 표의 사다리와 다르다 — 다시 세우니 상자가 움직였다");
            }
            Assert.Greater(cells, 0, "셀이 하나도 안 섰다 — 상자 이름이 바뀌었나");
            Assert.Greater(checkedPairs, 0, "견준 이웃 짝 — 잰 수 " + checkedPairs);
            // ⚠ 빽빽한 판(`dense`)은 정본이 이름판·칩을 숨기므로 그 짝이 없을 수 있다 — 그래서 «하나라도 있으면
            //   맞아야 한다» 로 못박는다(없으면 이 자가 과녁을 못 본 것이니 그것도 적어 둔다).
            Assert.Greater(sawWrapThenName, 0,
                "구슬 래퍼 바로 뒤에 이름판이 선 셀이 하나도 없다 — 이름판이 안 섰거나(빽빽한 판) 차례가 바뀌었다");
            v.Close();
            yield return null;
        }

        /// <summary>
        /// T33 92회차 — **셀 안**(구슬 래퍼 `.sr-orbwrap`) 겹의 층 차례. 90회차가 «셀 한 장이 제 문맥이라 이 표가
        /// 아니다» 로 빼 둔 자리고, 91회차가 ⓑ 로 넘긴 그 몫이다. 래퍼는 정본에서 `z-index: 2` 라 **스택 문맥을
        /// 만들므로** 그 안의 겹은 바깥 사다리와 못 겨룬다 — 그래서 표(`z_order_cell`)와 묻는 자(`ZOrderOfCell`)가 따로다.
        /// 층은 둘뿐이다: «z 를 안 적은 것들의 **0 층**»(광선·빛줄·잔상·구슬·하이라이트·아이콘·착지 링 — 정본에서
        /// `position` 만 있고 `z-index` 가 없다) → «배지·스파크 **2**»(정본 6476·6980·6997·7025).
        /// ⚑ 이 자가 런 143x 에서 잡아낸 것: 클론이 착지 링을 `SetAsLastSibling()` 으로 **배지 위**에 얹고 있었다 —
        ///   정본에선 배지(z 2)가 그 링(auto) **위**다. 링은 가산(screen)이라 «×N»·«NEW» 글자를 쓸고 지나갔다.
        /// </summary>
        [UnityTest]
        public IEnumerator 셀_안_겹의_형제_차례가_정본_셀_z_의_비감소다()
        {
            yield return Boot();
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            Sheet.Switch(SkillPetSheet.SubSkills);
            yield return null;
            Host.Tickets = 10000;
            Sheet.Skills.SummonButton.onClick.Invoke();
            yield return null;
            yield return null;
            SkillSummonResultView v = SkillSummonResultView.Current;
            Assert.IsNotNull(v, "결과 연출 팝업");

            int wraps = 0, checkedPairs = 0;
            foreach (RectTransform w in v.GetComponentsInChildren<RectTransform>(true))
            {
                if (w.name != "sr-orbwrap") continue;
                wraps++;
                var kids = new List<KeyValuePair<int, string>>();
                for (int i = 0; i < w.childCount; i++)
                {
                    Transform t = w.GetChild(i);
                    if (SummonFxStyle.ZOrderOfCell(t.name) < 0) continue;   // 표에 없는 상자(구운 광채·테·그림자·바닥)
                    kids.Add(new KeyValuePair<int, string>(t.GetSiblingIndex(), t.name));
                }
                kids.Sort((x, y) => x.Key.CompareTo(y.Key));
                for (int i = 1; i < kids.Count; i++)
                {
                    int zPrev = SummonFxStyle.ZOrderOfCell(kids[i - 1].Value);
                    int zCur = SummonFxStyle.ZOrderOfCell(kids[i].Value);
                    checkedPairs++;
                    Assert.LessOrEqual(zPrev, zCur,
                        "래퍼 안에서 «" + kids[i - 1].Value + "»(정본 셀 z " + zPrev + ") 가 «" + kids[i].Value
                        + "»(z " + zCur + ") 보다 **앞** 형제다 — 앞 형제가 아래에 그려지므로 층이 높은 쪽이 아래로 깔렸다");
                }
                // 세우는 자가 실제로 돌았나 — 다시 불러 움직일 것이 없어야 한다(91회차와 같은 자)
                Assert.AreEqual(0, SummonFxStyle.OrderLadder(w, true),
                    "래퍼의 형제 차례가 셀 표의 사다리와 다르다 — 다시 세우니 상자가 움직였다");
            }
            Assert.Greater(wraps, 0, "구슬 래퍼가 하나도 안 섰다 — 상자 이름이 바뀌었나");
            Assert.Greater(checkedPairs, 4, "견준 이웃 짝 — 잰 수 " + checkedPairs);
            v.Close();
            yield return null;
        }

        /// <summary>
        /// T33 90회차 — 소환 연출 겹의 **층 차례**(정본 `z-index`). 정본은 그 겹을 전부 `.sr-wrap` **형제**로 두고
        /// `z-index` 로 세우는데(`.sr-rays` 0 … `.sr-floor` 10 · `.sr-stars` 15 · `.sr-canopy` 20 · `.sr-shock` 30 ·
        /// `.sr-grid` 40 · `.sr-head`·`.sr-foot` 45 · `.sr-streaks`·`.sr-charge` 60) 클론은 **형제 순서**로 세운다.
        /// 그래서 견줄 수 있는 것은 «**같은 상자 안**에서 차례가 정본 z 의 비감소인가» 하나다(결정 909 의 둘째 걸음) —
        /// 클론이 그 겹을 두 상자(무대판 · 래퍼)로 갈라 두므로 **상자마다** 본다. 같은 z 가 여럿이라 **엄격 증가가 아니다**.
        /// ⚑ 이 한 칸이 표의 겹을 한 번에 잰다 — 88회차가 «실물에서 읽어야 한다» 고 적어 둔 그 자리다.
        /// T33 91회차 — 이 자가 런 1423 에서 **빨갛게 지목한 것을 고쳤다**: 판 안에서 `sr-foot`(45) 가
        /// `sr-shock`(30) 보다 앞 형제였다(= 충격파 링이 제목·버튼 **위**를 가로질렀다 — 정본이 5687~5690 에서
        /// «예전엔 z 가 3 이라 링이 이름판을 가로질렀다» 고 적어 둔 바로 그 흠이다). 세우는 자리가 겹마다
        /// 흩어져 있던 것이 까닭이라, 겹을 다 만든 뒤 **표가 한 번에** 세우게 했다(`SummonFxStyle.OrderLadder`).
        /// 표에도 클론이 달리 이름 지은 여덟을 등재해 census 가 판의 겹을 빠짐없이 센다.
        /// </summary>
        [UnityTest]
        public IEnumerator 연출_겹의_형제_차례가_정본_z_의_비감소다()
        {
            yield return Boot();
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            Sheet.Switch(SkillPetSheet.SubSkills);
            yield return null;
            Host.Tickets = 10000;
            Sheet.Skills.SummonButton.onClick.Invoke();
            yield return null;
            yield return null;
            SkillSummonResultView v = SkillSummonResultView.Current;
            Assert.IsNotNull(v, "결과 연출 팝업");

            // 표에 든 이름의 상자를 전부 모아 **부모별**로 차례를 본다(클론이 어느 상자에 두든 맞다).
            var groups = new Dictionary<Transform, List<KeyValuePair<int, string>>>();
            RectTransform content = null;
            int seen = 0;
            foreach (RectTransform t in v.GetComponentsInChildren<RectTransform>(true))
            {
                int z = SummonFxStyle.ZOrderOf(t.name);
                if (z < 0) continue;                       // 표에 없는 상자는 건너뛴다
                Transform par = t.parent;
                if (par == null) continue;
                if (par.name == "content") content = par as RectTransform;
                if (!groups.ContainsKey(par)) groups[par] = new List<KeyValuePair<int, string>>();
                groups[par].Add(new KeyValuePair<int, string>(t.GetSiblingIndex(), t.name));
                seen++;
            }
            Assert.Greater(seen, 6, "표에 든 겹이 실물에 여럿 서 있다 — 잰 수 " + seen);

            int checkedPairs = 0;
            foreach (KeyValuePair<Transform, List<KeyValuePair<int, string>>> g in groups)
            {
                List<KeyValuePair<int, string>> kids = g.Value;
                kids.Sort((x, y) => x.Key.CompareTo(y.Key));          // 형제 차례대로
                for (int i = 1; i < kids.Count; i++)
                {
                    int zPrev = SummonFxStyle.ZOrderOf(kids[i - 1].Value);
                    int zCur = SummonFxStyle.ZOrderOf(kids[i].Value);
                    checkedPairs++;
                    // T33 91회차 — 90회차의 이 문구는 앞뒤를 거꾸로 적었다: `kids[i-1]` 은 **앞** 형제다(= 아래에 그려진다).
                    Assert.LessOrEqual(zPrev, zCur,
                        g.Key.name + " 안에서 «" + kids[i - 1].Value + "»(정본 z " + zPrev + ") 가 «" + kids[i].Value
                        + "»(z " + zCur + ") 보다 **앞** 형제다 — 앞 형제가 아래에 그려지므로 층이 높은 쪽이 아래로 깔렸다(정본 사다리가 뒤집혔다)");
                }
            }
            Assert.Greater(checkedPairs, 4, "견준 이웃 짝 — 잰 수 " + checkedPairs);

            // T33 91회차 — 사다리를 세우는 자가 **실제로 돌았는지**도 본다: 한 번 더 부르면 움직일 것이 없어야 한다.
            //   («비감소» 는 표에 없는 상자가 끼어들면 느슨해진다 — 이 한 줄이 그 틈을 막는다 · 결정 918)
            Assert.IsNotNull(content, "판(content) 을 못 찾았다 — 겹을 담은 상자 이름이 바뀌었나");
            Assert.AreEqual(0, SummonFxStyle.OrderLadder(content),
                "판의 형제 차례가 표의 사다리와 다르다 — 다시 세우니 " + "상자가 움직였다(`OrderLadder` 가 안 돌았거나 뒤에 끼어든 겹이 있다)");
            v.Close();
            yield return null;
        }

        /// <summary>
        /// T33 85회차 — 빛줄기(정본 5716        /// <summary>
        /// T33 85회차 — 빛줄기(정본 5716 `.sr-streaks i { transform-origin: 50% 100% }`)의 **축이 아래끝**인가.
        /// 정본은 상자의 밑변을 축으로 `scaleY`(.55→1.5) 한다 — 그 밑변이 회전 뒤 **광원에 가까운 끝**이라
        /// 늘어난 몫은 **광원에서 먼 쪽으로** 자라고 가까운 끝은 제자리에 남는다.
        /// 종전 클론은 축을 위끝에 둬서 막대가 **len·(1−s)** 만큼 밀려 있었다(시작 0.99rem 바깥 · 끝 1.1rem 안쪽 ·
        /// 회차 중간에 방향이 뒤집힌다 · 결정 899). 이 자는 **프레임 창에 안 매인 구조 단언**이다 —
        /// 빛줄기는 180ms 만 살아 알파로 재면 펄럭인다(T422·T431·T443 가 그 자리에서 세 번 넘어졌다).
        /// </summary>
        [UnityTest]
        public IEnumerator 빛줄기의_축은_아래끝이라_가까운_끝이_제자리에_남는다()
        {
            yield return Boot();
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            Sheet.Switch(SkillPetSheet.SubSkills);
            yield return null;
            Host.Tickets = 10000;
            Sheet.Skills.SummonButton.onClick.Invoke();
            yield return null;
            SkillSummonResultView v = SkillSummonResultView.Current;
            Assert.IsNotNull(v, "결과 연출 팝업");
            if (v.Fx == null) Assert.Ignore("환경 — 무대판(stage · n<=10) 이 아니면 빛줄기 겹이 없다");
            Assert.Greater(v.StreakCount, 0, "빛줄기가 섰다");

            for (int i = 0; i < v.StreakCount; i++)
            {
                Image si = v.StreakOf(i);
                Assert.IsNotNull(si, "빛줄기 " + i);
                RectTransform bar = si.rectTransform;
                Assert.AreEqual(0.5f, bar.pivot.x, 1e-4f, "빛줄기 " + i + ": 축은 가로 가운데(정본 50%)");
                Assert.AreEqual(0f, bar.pivot.y, 1e-4f,
                    "빛줄기 " + i + ": 축은 **아래끝**이다(정본 `50% 100%` · 유니티는 y 가 위로 커서 0 이 아래다)");
                // 늘어남이 축을 안 움직인다 — 배율을 손으로 바꿔 **축 점**과 **먼 끝**의 월드 자리를 견준다(시간과 무관하다).
                // ⚠ 86회차 수리 — 종전엔 `GetWorldCorners` 의 **최저 꼭지**를 축으로 봤다가 런 **1398** 에서 빛줄기 2 가 빨갰다
                //   (기대 237.11 ↔ 실측 233.65). 막대는 **회전**해 있어 «가장 낮은 꼭지» 가 축이 아니다 — 각이 90°를 넘으면
                //   (스포크 2 는 2·360/9 + 14 = 94°) **위끝 꼭지**가 최저가 되고 그것은 늘이면 당연히 움직인다.
                //   축의 월드 자리는 `RectTransform.position`(= 피벗 점) 하나뿐이고 그것은 제 `localScale` 과 무관하다.
                Vector3 keep = bar.localScale;
                Vector3 axis0 = bar.position;
                Vector3 tip0 = bar.TransformPoint(new Vector3(0f, bar.rect.yMax, 0f));
                bar.localScale = new Vector3(keep.x, keep.y * 2f, keep.z);
                Vector3 axis1 = bar.position;
                Vector3 tip1 = bar.TransformPoint(new Vector3(0f, bar.rect.yMax, 0f));
                bar.localScale = keep;
                Assert.AreEqual(axis0.x, axis1.x, 0.01f, "빛줄기 " + i + ": 늘어나도 **축은 제자리**다(x)");
                Assert.AreEqual(axis0.y, axis1.y, 0.01f, "빛줄기 " + i + ": 늘어나도 **축은 제자리**다(y)");
                float d0 = (tip0 - axis0).magnitude, d1 = (tip1 - axis1).magnitude;
                Assert.Greater(d0, 1f, "빛줄기 " + i + ": 막대에 길이가 있다");
                Assert.AreEqual(d0 * 2f, d1, Mathf.Max(0.5f, d0 * 0.02f),
                    "빛줄기 " + i + ": 늘어난 몫은 전부 **축의 반대쪽**(광원에서 먼 쪽)으로 간다 — 축↔먼끝 거리가 배율 그대로 2배");
                if (i >= 2) break;      // 셋만 본다(스포크는 아홉 이상이고 단언은 구조라 같다)
            }
            v.Close();
            yield return null;
        }

        /// <summary>T448 — 동급(peer) 셀의 착지 링        /// <summary>T448 — 동급(peer) 셀의 착지 링(정본 6710~6716 `.sr-cell.peer.on::after`): 동급 셀마다 셀 뒤(z −1) 폭 100% 정사각 링이 서고,
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
                // ⚠ T33 93회차 — 종전엔 «맨 앞 형제(0)» 로 쟀는데 그 잣대가 **틀렸다**: 같은 셀의 등급 광주
                //   (`.sr-cell::before` 6334)도 z **−1** 이고 정본 트리 순서로 `::before` 가 `::after` **앞**이다
                //   — 곧 링의 옳은 자리는 0 이 아니라 «광주 다음, 구슬 래퍼 앞» 이다(92회차의 착지 링과 같은 가족의
                //   잘못: «z 가 맨 아래/맨 위면 형제도 맨 끝» 이 아니다). 자리는 표로 묻고 앞뒤만 견준다.
                int zr;
                Assert.IsTrue(SummonFxStyle.TryZOrderOfCellOwn(rt.name, out zr), "셀 문맥 표에 든 겹이다");
                Assert.AreEqual(-1, zr, "정본 6709 `.sr-cell.peer.on::after { z-index: -1 }`");
                Transform wrapOf = rt.parent.Find("sr-orbwrap");
                Assert.IsNotNull(wrapOf, "같은 셀의 구슬 래퍼");
                Assert.Less(rt.GetSiblingIndex(), wrapOf.GetSiblingIndex(), "링은 셀 뒤 — 구슬 래퍼보다 앞 형제다");
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
    
        /// <summary>T480 ⓐ — 일반 셀 착지 링(정본 6358~6363 `.sr-orbwrap::after` + `srring` .55s): **모든** 셀의 래퍼 안 **0 층 맨 위**(T33 92회차가 «자식 중 맨 위» 를 그렇게 바로잡았다 — 배지·스파크는 z 2 라 그 위다)에 inset 0 정사각 링이 서고,
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
                // T33 92회차 — 이 자는 «래퍼 자식 중 **맨 위**» 를 단언했는데 그것이 **틀린 주장**이었다(런 1431 이 그래서 빨갰다):
                //   `::after` 가 마지막 자식인 것은 맞지만 그것은 **z 를 안 적은(auto) 상자들의 0 층 안에서** 맨 위일 뿐이고,
                //   같은 래퍼의 배지·스파크는 `z-index: 2`(정본 6476 `.sr-spark` · 6980 `.sr-new` · 6997 `.sr-qty` · 7025 `.sr-dup`)라
                //   **그것들이 이 링 위**다. 그래서 묻는 것을 바꾼다 — «0 층 상자 전부보다 뒤 · z 2 상자 전부보다 앞».
                int zRing = SummonFxStyle.ZOrderOfCell(rt.name);
                Assert.AreEqual(0, zRing, "정본 6358 `.sr-orbwrap::after` 는 z 선언이 없다 = 0 층");
                for (int k = 0; k < wrap.childCount; k++)
                {
                    Transform sib = wrap.GetChild(k);
                    int zs = SummonFxStyle.ZOrderOfCell(sib.name);
                    if (zs < 0 || sib == rt.transform) continue;      // 표에 없는 상자(구운 광채·테·그림자·바닥)는 제 자리다
                    if (zs > zRing)
                        Assert.Less(rt.GetSiblingIndex(), sib.GetSiblingIndex(),
                            "«" + sib.name + "»(셀 z " + zs + ")가 착지 링(0 층) **위**여야 한다 — 가산 링이 배지 글자를 쓸고 지나간다");
                    else
                        Assert.Greater(rt.GetSiblingIndex(), sib.GetSiblingIndex(),
                            "착지 링은 같은 0 층 안에서 **맨 뒤**다(정본 ::after 는 그 층의 마지막 자식) — «" + sib.name + "» 보다 뒤여야 한다");
                }
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

        /// <summary>
        /// T492 둘째 결함 — 정본 `style.css` 5968 `.sr-stars { opacity: 0; transition: opacity .6s **ease-out** }`.
        /// 길이(600ms)는 맞았는데 이징이 **선형**이라, 정본이 앞쪽에 몰아 켜는 켜짐이 클론에선 끝까지 고르게 밝아졌다.
        ///
        /// ⚠ **자가 제 벽시계로 페이드를 세지 않는다** — 런 1332 에서 그렇게 깨졌다(앞 칸의 긴 단언들 뒤에 붙다 보니
        /// 자의 `t=0` 시점에 이미 페이드가 0.5s 지나 있었고, 「기다린 값 .715 ↔ 실측 .970」 로 빨갰다).
        /// 여기서는 연출이 제 시계로 내는 진행(<see cref="SummonFx.StarsFadeT"/>)을 **같은 프레임에** 읽어 견준다.
        /// </summary>
        [UnityTest]
        public IEnumerator 별가루_페이드는_선형이_아니라_표의_ease_out_을_따른다()
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
            Assert.IsNotNull(v, "소환 결과 창");
            SummonFx fx = v.Fx;
            Assert.IsNotNull(fx, "연출 겹");

            // done 으로 보낸다(이미 done 이면 그대로 — 아래는 진행을 보고 고른다).
            float t = 0f;
            while (!v.Done && t < 5f) { if (t <= 0f) v.OnTap(); t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(v.Done, "탭 뒤 done");

            // 진행이 창(0.15~0.85) 안에 드는 프레임을 잡아 **그 프레임의** 알파와 견준다.
            var ease = SummonFxStyle.Ease("stars_fade_ease");
            int sampled = 0;
            float worstGap = 0f;
            t = 0f;
            while (t < 2f)
            {
                float k = fx.StarsFadeT;
                if (k >= 0.15f && k <= 0.85f)
                {
                    float a = fx.StarsAlpha, want = (float)ease.Ease(k);
                    Assert.AreEqual(want, a, 0.02f, "별 α 는 표 이징을 따른다 — 진행 " + k.ToString("0.000") + " · 기다린 값 " + want.ToString("0.000") + " · 실측 " + a.ToString("0.000"));
                    if (a - k > worstGap) worstGap = a - k;
                    sampled++;
                }
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.Greater(sampled, 0, "페이드 한가운데를 한 프레임도 못 잡았다 — 창을 넓히거나 자리를 앞으로");
            Assert.Greater(worstGap, 0.10f, "ease-out 은 선형보다 뚜렷이 밝다(가장 벌어진 자리 " + worstGap.ToString("0.000") + ") — 선형으로 되돌아가면 여기서 운다");
            Assert.AreEqual(1f, fx.StarsAlpha, 0.02f, "끝나면 α 1");

            if (SkillSummonResultView.Current != null) SkillSummonResultView.Current.OnTap();
            yield return null;
        }

        /// <summary>
        /// T503 — 라벨(이름판 `.sr-name` · 등급칩 `.sr-sub`)은 셀과 **다른 애니**를 탄다: 정본 6256
        /// `.sr-cell.on .sr-name, .sr-cell.on .sr-sub { animation: srlabel var(--pop, .32s) linear forwards }` ·
        /// 6257~6260 `@keyframes srlabel { 0%, 68% { opacity: 0 } 100% { opacity: 1 } }`.
        /// 정본이 까닭을 실측으로 적어 뒀다(6250~6255): «비행 중에는 띄우지 않는다 — **예전엔 셀 opacity 하나로 같이 날아서**
        /// 이웃 라벨과 겹쳐 **한 덩어리로 융합**했다». 클론이 바로 그 «예전» 꼴이었다.
        /// ⚑ 잣대를 **프레임이 아니라 규칙**에 건다(T422·T501 의 길) — 비행 한가운데 프레임을 잡으러 가면 러너 프레임 간격에 걸려 펄럭인다.
        /// </summary>
        [UnityTest]
        public IEnumerator 라벨은_제_그룹에_묶여_비행_중엔_안_보이고_팝_68_퍼센트_뒤에_켜진다()
        {
            yield return Boot();
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            Sheet.Switch(SkillPetSheet.SubSkills);
            yield return null;
            // x5 — 이웃 라벨이 생겨야 정본이 적어 둔 «융합» 상황이 선다(x1 은 라벨이 하나다).
            while (Host.SummonMult("skill") != 5) Host.CycleSummonMult("skill");
            Host.Tickets = 10000;
            Host.Sync();
            yield return null;
            Sheet.Skills.SummonButton.onClick.Invoke();
            yield return null;
            SkillSummonResultView v = SkillSummonResultView.Current;
            Assert.IsNotNull(v, "결과 연출 팝업");
            Assert.AreEqual(5, v.CellCount, "x5");

            // ⓐ **구조** — 라벨마다 제 `CanvasGroup` 이 있고 그것이 셀 그룹과 다른 물건이다(셀 알파 하나로는 정본이 안 된다).
            CanvasGroup cellG = v.CellRootOf(0).GetComponent<CanvasGroup>();
            Assert.IsNotNull(cellG, "셀 그룹");
            CanvasGroup[] lg = v.LabelGroupsOf(0);
            Assert.IsNotNull(lg, "라벨 그룹(이름판 · 등급칩)");
            Assert.AreEqual(2, lg.Length, "정본 6256 은 `.sr-name` 과 `.sr-sub` **둘**을 묶는다");
            for (int i = 0; i < lg.Length; i++)
            {
                Assert.IsNotNull(lg[i]);
                Assert.AreNotSame(cellG, lg[i], "라벨 알파는 셀 알파와 **따로** 돈다");
                Assert.IsTrue(lg[i].transform.IsChildOf(v.CellRootOf(0)), "라벨은 셀 안에 산다(알파가 곱해진다 — CSS 와 같다)");
            }
            Assert.AreEqual("sr-name", lg[0].name); Assert.AreEqual("sr-sub", lg[1].name);

            // ⓑ **규칙** — 뷰가 읽는 그 표로 세 자리를 잰다(정본 6258 의 68% · 선형).
            Forge.Core.Ui.SummonPopSpec pp = SummonFxStyle.Pop;
            const double pop = 320;
            Assert.AreEqual(0f, (float)pp.LabelAlpha(pop * 0.50, pop), 1e-6f, "팝 50%(비행 중) 라벨 0");
            float a84 = (float)pp.LabelAlpha(pop * 0.84, pop);
            Assert.Greater(a84, 0f); Assert.Less(a84, 1f);
            Assert.AreEqual(0.5f, a84, 0.02f, "84% = 구간의 절반(정본이 적은 `linear`)");
            Assert.AreEqual(1f, (float)pp.LabelAlpha(pop, pop), 1e-6f, "100% 에 다 켜진다");

            // ⓒ **살아 있는 프레임** — 팝이 도는 동안 모아 본다. 진행률 ≤ 68% 인 프레임은 **하나도** 알파가 0 이 아니어서는 안 된다.
            //   (한 프레임도 못 잡으면 단언하지 않는다 — 느린 러너에서 펄럭이지 않게. 잡았는지는 끝에서 센다.)
            int low = 0, high = 0;
            float waited = 0f;
            while (waited < 3f && !v.Done)
            {
                for (int i = 0; i < v.CellCount; i++)
                {
                    float u = v.PopProgressOf(i), la = v.LabelAlphaOf(i);
                    if (u < 0f || la < 0f) continue;
                    if (u <= (float)pp.LabelAtF - 0.02f)
                    {
                        low++;
                        Assert.AreEqual(0f, la, 1e-4f, "셀 " + i + " 는 팝 " + u.ToString("0.000") + "(≤68%) 인데 라벨이 보인다 — 비행 중 융합(정본 6250 주석)");
                    }
                    else if (u >= 0.999f)
                    {
                        high++;
                        // T33 92회차 — «u ≥ 0.999 면 알파 **1**» 은 **틀린 문턱**이었다. 라벨은 68%~100% 구간에서 켜지므로
                        //   u = 0.99926 의 참값은 **0.9977** 이고 ±1e-4 를 벗어난다 — 런 1433 이 정확히 그 수(0.99768728)로 넘어졌다.
                        //   같은 코드로 런 1431 은 **통과**했다(프레임이 u = 1 에 떨어졌을 뿐이다) — 곧 클론이 아니라 **문턱이 범인**이다.
                        //   그래서 1 이 아니라 **그 진행률에서 표가 내는 값**과 견준다. 구간 전체를 보는 쪽이라 자가 더 세진다.
                        Assert.AreEqual((float)pp.LabelAlpha(u * pop, pop), la, 1e-3f,
                            "팝 " + u.ToString("0.0000") + " 의 라벨 알파는 표가 내는 값이다(forwards)");
                    }
                }
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.Greater(low + high, 0, "팝이 도는 프레임을 하나도 못 잡았다 — 창을 넓히거나 자리를 앞으로");

            // ⓓ **정착** — 전부 공개하고 팝 길이를 지나면 라벨은 다 켜져 있다(촬영이 찍는 모습이 이것이다).
            if (!v.Done) v.OnTap();
            float t2 = 0f;
            while (t2 < 0.8f) { t2 += Time.unscaledDeltaTime; yield return null; }
            for (int i = 0; i < v.CellCount; i++)
                Assert.AreEqual(1f, v.LabelAlphaOf(i), 1e-3f, "정착한 셀 " + i + " 의 라벨 알파 1");

            if (SkillSummonResultView.Current != null) SkillSummonResultView.Current.OnTap();
            yield return null;
        }

        /// <summary>
        /// T506 — 드러나는 조각들이 **팝 없이 즉시** 나타나던 것과 선형·손 근사 램프들. 정본 줄: `srtitle`(6226 · 6213 제목 .45s · 5783 solo .3s · 7116 집계 .3s) ·
        /// `srgridlift`(6162) · `srflash`(6158) · `srreflect`(6973) · `srintro`(5951 광선 · 5958 광원).
        /// ⚑ 잣대를 **프레임이 아니라 규칙·상태**에 건다(T422·T501·T503 의 길) — 램프 한가운데를 잡으러 가면 러너 프레임 간격에 걸려 펄럭인다.
        /// </summary>
        [UnityTest]
        public IEnumerator 드러나는_조각들이_제_그룹에서_위에서_내려오며_켜지고_램프가_정본_이징을_탄다()
        {
            yield return Boot();
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            Sheet.Switch(SkillPetSheet.SubSkills);
            yield return null;
            while (Host.SummonMult("skill") != 5) Host.CycleSummonMult("skill");
            Host.Tickets = 10000;
            Host.Sync();
            yield return null;
            Sheet.Skills.SummonButton.onClick.Invoke();
            yield return null;
            SkillSummonResultView v = SkillSummonResultView.Current;
            Assert.IsNotNull(v, "결과 연출 팝업");
            var sp = SummonFxStyle.Reveal;

            // ⓐ **구조** — 제목·집계가 제 `CanvasGroup` 에 묶였다(종전엔 그룹이 없어 `SetActive` 가 전부였다).
            Assert.IsNotNull(v.TitleGroup, "제목 띠 그룹(정본 6213)");
            Assert.IsNotNull(v.SumGroup, "집계 그룹(정본 7116)");
            // ⓑ 제목은 **열림**에서 탄다 — 이 프레임엔 아직 덜 켜져 있거나 막 켜졌다. 끝나면 1 이고 제자리다.
            Vector2 titleMid = ((RectTransform)v.TitleGroup.transform).anchoredPosition;
            float tw = 0f;
            while (tw < (float)sp.TitleMs / 1000f + 0.15f) { tw += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(1f, v.TitleGroup.alpha, 1e-3f, "제목 띠는 .45s 뒤 알파 1(`both`)");
            Vector2 titleEnd = ((RectTransform)v.TitleGroup.transform).anchoredPosition;
            Assert.GreaterOrEqual(titleMid.y, titleEnd.y - 0.5f,
                "제목은 **위에서 내려온다** — 도중 y 가 끝 y 보다 높거나 같다(정본 translateY(-.6rem) → 0)");

            // ⓒ **집계·solo 는 `.done` 에서** — 그 전엔 알파 0 이고, `.3s` 뒤 1 이며 제자리다.
            if (!v.Done)
            {
                Assert.AreEqual(0f, v.SumGroup.alpha, 1e-3f, "done 전엔 집계가 안 보인다(정본 `visibility: hidden` + `.done` 애니)");
                v.OnTap();
            }
            float dw = 0f;
            while (!v.Done && dw < 5f) { dw += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(v.Done);
            Vector2 sumHomeSeen = ((RectTransform)v.SumGroup.transform).anchoredPosition;
            dw = 0f;
            while (dw < (float)sp.SumMs / 1000f + 0.2f) { dw += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(1f, v.SumGroup.alpha, 1e-3f, "집계는 .3s 뒤 알파 1");
            Vector2 sumEnd = ((RectTransform)v.SumGroup.transform).anchoredPosition;
            Assert.GreaterOrEqual(sumHomeSeen.y, sumEnd.y - 0.5f, "집계도 위에서 내려온다");
            if (v.SoloGroup != null) Assert.AreEqual(1f, v.SoloGroup.alpha, 1e-3f, "x1 판의 solo 도 같은 꼴");

            // ⓓ **규칙** — 표가 쥔 램프 넷이 손 근사·선형과 실제로 갈린다(뷰가 읽는 그 표로 잰다).
            double br, sat;
            Assert.IsTrue(sp.GridLiftAt(0, out br, out sat) && br > 1f && sat > 1f, "srgridlift 는 1 보다 밝고 진하게 시작한다");
            Assert.IsFalse(sp.GridLiftAt(sp.GridLiftMs, out br, out sat), "채움이 없다 — 끝나면 none");
            Assert.Less(sp.FlashAt(275, 550, sp.FlashA0), sp.FlashA0 * 0.5,
                "srflash 는 ease-out 이라 한가운데가 선형(.45)보다 **낮다**");
            Assert.AreEqual(0.88 * 0.6846, sp.ReflectAt(250, 500, 0.88), 5e-4, "srreflect ease-out — `1−(1−k)²`(.75) 근사와 갈린다");
            double sc, k;
            sp.IntroAt(120, 240, 1.3, sp.RaysIntroEase, out sc, out k);
            Assert.AreEqual(0.3154, k, 5e-4, "srintro ease-in — `k²`(.25) 근사와 갈린다");

            if (SkillSummonResultView.Current != null) SkillSummonResultView.Current.OnTap();
            yield return null;
        }

        /// <summary>
        /// T509 — 셀 **안**의 겹들(착지 링 · 스파크 · 잔상 · 동급 링 · 광선 · 광창)이 «순백 + 제 알파» 를 써서
        /// 정본이 `.sr-cell`(6720 `srrecede` → `filter: saturate(.35) brightness(.6)`)·`.sr-grid`(6162 `srgridlift`)에
        /// **통째로 거는 필터**를 덮고 있었다. CSS `filter` 는 그 상자 아래 전부를 먹으므로 그 겹들도 같이 물러나야 한다.
        /// ⚑ 잣대를 **규칙**에 건다 — «그 프레임의 필터가 1 이 아니면 겹의 색도 그 필터를 곱한 값이어야 한다».
        /// 필터가 1 아닌 프레임을 한 번도 못 잡으면 단언하지 않고 끝에서 «잡았는지» 를 센다(러너 프레임 간격 · T422 의 길).
        /// </summary>
        [UnityTest]
        public IEnumerator 셀_안의_겹들이_그_셀에_걸린_필터를_곱해_쓴다()
        {
            yield return Boot();
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            Sheet.Switch(SkillPetSheet.SubSkills);
            yield return null;
            while (Host.SummonMult("skill") != 5) Host.CycleSummonMult("skill");
            Host.Tickets = 10000;
            Host.Sync();
            yield return null;
            Sheet.Skills.SummonButton.onClick.Invoke();
            yield return null;
            SkillSummonResultView v = SkillSummonResultView.Current;
            Assert.IsNotNull(v, "결과 연출 팝업");

            // 전부 공개 → 주역이 터지고 조연이 물러난다(`srrecede` .68s · `srgridlift` .5s).
            if (!v.Done) v.OnTap();
            int dim = 0, checkedFrames = 0, deep = 0;
            float worst = 0f, t = 0f;
            while (t < 2.5f)
            {
                for (int i = 0; i < v.CellCount; i++)
                {
                    Vector2 f = v.FilterOf(i);
                    Image ring = v.LandRingOf(i);
                    if (ring == null) continue;
                    if (f.x < 0.999f || f.y < 0.999f)
                    {
                        dim++;
                        // 기대값 — `saturate()` 는 **흰색에는 아무 일도 안 한다**(휘도 = 1 이라 회색축 위의 점이다).
                        //   그래서 흰 겹에 남는 것은 `brightness` 뿐이다: want = clamp01(밝기).
                        float want = Mathf.Clamp01(f.x);
                        Assert.AreEqual(want, ring.color.r, 0.02f,
                            "셀 " + i + " 착지 링이 필터(밝기 " + f.x.ToString("0.000") + " · 채도 " + f.y.ToString("0.000") + ")를 안 먹었다");
                        if (1f - ring.color.r > worst) worst = 1f - ring.color.r;
                        if (f.x < 0.8f) deep++;
                    }
                    else
                    {
                        checkedFrames++;
                        Assert.AreEqual(1f, ring.color.r, 0.02f, "필터가 1 인 프레임에는 흰색이다");
                    }
                }
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.Greater(dim + checkedFrames, 0, "셀을 한 프레임도 못 봤다 — 창을 넓히거나 자리를 앞으로");
            // ⚠ 램프의 **깊은 자리**(밝기 < .8)를 잡은 프레임이 있을 때만 «눈에 보이게» 를 단언한다 —
            //   끝자락(밝기 ≈ 1)만 잡힌 회차에 억지로 울리면 그것은 결함이 아니라 프레임 운이다.
            if (deep > 0)
                Assert.Greater(worst, 0.1f,
                    "물러나는 동안 그 겹이 **눈에 보이게** 어두워져야 한다(가장 벌어진 자리 " + worst.ToString("0.000") + ") — 순백으로 돌아가면 여기서 운다");

            if (SkillSummonResultView.Current != null) SkillSummonResultView.Current.OnTap();
            yield return null;
        }

        /// <summary>T511 ⓐⓒ — 끝난 뒤에도 화면이 죽지 않게 하는 호흡 둘. 수는 표가 쥔다(§1).</summary>
        [UnityTest]
        public IEnumerator 힌트_칩과_무대_받침이_정본_주기로_숨쉰다()
        {
            // 표: 정본 7136·7174 의 주기와 폭.
            Assert.AreEqual(1.4f, SummonFxStyle.L("hint_breath_s"), 1e-4f, "힌트 1.4s");
            Assert.AreEqual(0.45f, SummonFxStyle.L("hint_a_lo"), 1e-4f, "힌트 바닥 .45");
            Assert.AreEqual(2.6f, SummonFxStyle.L("stage_breath_s"), 1e-4f, "무대 받침 2.6s");
            Assert.AreEqual(0.78f, SummonFxStyle.L("stage_a_lo"), 1e-4f, "무대 바닥 .78");
            Assert.AreEqual(1.1f, SummonFxStyle.L("stage_scale_hi"), 1e-4f, "무대 정점 배율 1.1");

            // 왕복 셈: 0 에서 바닥 · 반주기에서 정점 · 한 주기에서 다시 바닥.
            // ⚑ 그리고 **선형이 아니다** — ease-in-out 은 1/4 지점에서 선형(0.5)보다 **아래**다(양 끝이 느리다).
            float lo = SummonFxStyle.L("hint_a_lo"), hi = SummonFxStyle.L("hint_a_hi"), per = SummonFxStyle.L("hint_breath_s");
            float a0 = Breath(0f, per, lo, hi), aMid = Breath(per * 0.5f, per, lo, hi), a1 = Breath(per, per, lo, hi);
            Assert.AreEqual(lo, a0, 1e-3f, "0 에서 바닥");
            Assert.AreEqual(hi, aMid, 1e-3f, "반주기에서 정점");
            Assert.AreEqual(lo, a1, 1e-3f, "한 주기에서 다시 바닥");
            // ⚠ **1/4 지점에서 재지 마라 — 거기서는 선형과 같다**(런 1368 이 그렇게 빨갰다).
            //   `cubic-bezier(.42,0,.58,1)` 은 (0.5, 0.5) 에 대해 **대칭**이고, 왕복의 1/4 지점은 올라가는 구간의 **한가운데**(s = .5)라
            //   값이 정확히 0.5 다. 곡선과 선형이 갈리는 곳은 **구간의 1/4·3/4**(s = .25 → .129 ↔ 선형 .25 · s = .75 → .871 ↔ .75)다.
            float aUp = Breath(per * 0.125f, per, lo, hi);     // 올라가는 구간의 1/4 — 선형보다 **아래**
            float aUp3 = Breath(per * 0.375f, per, lo, hi);    // 그 구간의 3/4 — 선형보다 **위**
            Assert.Less(aUp, lo + (hi - lo) * 0.25f, "구간 1/4 은 선형보다 아래다(ease-in 쪽이 느리다) — 선형·코사인으로 되돌림 방지");
            Assert.Greater(aUp3, lo + (hi - lo) * 0.75f, "구간 3/4 은 선형보다 위다(ease-out 쪽으로 붙는다)");
            Assert.AreEqual(lo + (hi - lo) * 0.5f, Breath(per * 0.25f, per, lo, hi), 1e-3f, "왕복의 1/4 지점은 대칭이라 선형과 **같다** — 이 줄이 위 둘의 자리를 지킨다");
            yield return null;
        }

        /// <summary>
        /// T511 2회차 — 소환진 **번진 빛**(정본 6929 `#summon-result-modal.done .sr-floor::before { animation: srfloorglow 3.2s ease-in-out infinite }`)이
        /// 실물 화면에서 **두 판 교차섞기**로 숨쉰다. 정본이 숨쉬게 하는 것은 알파도 배율도 아니라 **번짐 폭**이라
        /// 두 틀(0%/100% 와 50%)을 구워 두고 섞는 비율만 왕복시킨다 — 그 길을 실측으로 골랐다(등재문 ⓑ · 결정 863).
        /// </summary>
        [UnityTest]
        public IEnumerator 소환진_번진_빛은_두_틀을_섞어_숨쉰다()
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

            // ⓐ 겹 둘이 소환진 **뒤**에 선다(바깥 번짐은 소환진 밖으로 나가는 겹이라 판이 상자보다 크다).
            RectTransform body = (RectTransform)fx.transform;
            Transform floor = body.Find("sr-floor");
            Assert.IsNotNull(floor, "소환진");
            Transform lo = floor.Find("sr-floor-glow-lo"), hi = floor.Find("sr-floor-glow-hi");
            Assert.IsNotNull(lo, "0%/100% 틀 판");
            Assert.IsNotNull(hi, "50% 틀 판");
            Transform ring = floor.Find("sr-floor-ring");
            Assert.IsNotNull(ring, "링 겹");
            // ⚑ **T518 에서 바로잡은 자리** — T511 2회차는 «번진 빛은 링보다 뒤» 로 깔고 이 자에도 그렇게 박았는데 틀렸다.
            //    CSS 페인트 차례는 «바깥 그림자 → 바탕색 → 바탕 그림 → **안쪽 그림자** → 테» 라, 이 판이 같이 쥔
            //    **안쪽** 겹은 두 방사(고인 면·링) **위**에 와야 한다. 바깥 겹은 아래여야 하지만 두 방사는 타원 밖 알파가
            //    0 이라 가릴 것이 없다 ⇒ 판 통째가 «링 **위** · 룬 눈금(`::after`) **아래**» 가 정본과 같은 그림이다.
            Transform ticks = floor.Find("sr-floor-ticks");
            Assert.IsNotNull(ticks, "룬 눈금 띠");
            Assert.Greater(lo.GetSiblingIndex(), ring.GetSiblingIndex(), "번진 빛은 링 **위**(안쪽 그림자는 바탕 그림 위)");
            Assert.Greater(hi.GetSiblingIndex(), ring.GetSiblingIndex(), "번진 빛은 링 **위**");
            Assert.Less(lo.GetSiblingIndex(), ticks.GetSiblingIndex(), "룬 눈금(`::after`)보다는 아래");
            Assert.Less(hi.GetSiblingIndex(), ticks.GetSiblingIndex(), "룬 눈금보다는 아래");
            RectTransform lort = (RectTransform)lo;
            Assert.Greater(lort.rect.width, ((RectTransform)floor).rect.width, "판이 소환진 상자보다 넓다(번짐이 밖으로 나간다)");
            Assert.AreEqual(((RectTransform)hi).rect.width, lort.rect.width, 0.5f, "두 판은 **같은 넓힘**으로 구워야 겹친다");
            Assert.AreEqual(((RectTransform)hi).rect.height, lort.rect.height, 0.5f, "높이도 같다");

            // ⓑ `done` 전에는 두 틀이 꺼져 있고 **상시 판이 켜져 있다** — 정본은 `.done` 에서 `box-shadow` 목록을
            //    통째로 갈아 끼우므로 둘은 **맞교대**다(T518). 같이 켜지면 정본보다 밝아진다.
            Transform bs = floor.Find("sr-floor-glow-base");
            Assert.IsNotNull(bs, "상시 번진 빛 판(T518)");
            Assert.Greater(bs.GetSiblingIndex(), ring.GetSiblingIndex(), "상시 번진 빛도 링 **위**");
            Assert.Less(bs.GetSiblingIndex(), ticks.GetSiblingIndex(), "상시 번진 빛도 룬 눈금보다 아래");
            Assert.Greater(((RectTransform)bs).rect.width, ((RectTransform)lo).rect.width,
                "상시 판은 흐림이 더 커서(6rem ↔ 5rem) 더 넓게 굽는다");
            if (!v.Done)
            {
                Vector2 off = v.FloorGlowAlphas;
                Assert.AreEqual(0f, off.x, 1e-6f, "done 전 0%/100% 틀 α 0");
                Assert.AreEqual(0f, off.y, 1e-6f, "done 전 50% 틀 α 0");
                Assert.Greater(v.FloorGlowBaseAlpha, 0f, "done 전에는 상시 번진 빛이 켜져 있다(T518)");
                v.OnTap();
            }
            float t = 0f;
            while (!v.Done && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(v.Done, "탭 뒤 done");

            // ⓒ 한 주기(+여유) 동안 지켜본다 — 섞는 비율이 **두 끝을 다 지나야** 한다.
            //    ⚠ «딱 ms» 로 재지 않는다(러너 프레임이 들쭉날쭉하다 · 결정 875) — 한 주기 넘게 돌며 최솟값·최댓값을 모은다.
            float per = SummonFxStyle.L("floor_glow_s");
            float want = SummonFxStyle.L("floor_done_a");
            Assert.AreEqual(3.2f, per, 1e-4f, "정본 6929 의 3.2s");
            float kMin = 2f, kMax = -1f, sumMin = 9f, sumMax = -1f;
            float watched = 0f;
            while (watched < per * 1.3f)
            {
                watched += Time.unscaledDeltaTime;
                yield return null;
                Vector2 g = v.FloorGlowAlphas;
                if (g.x < 0f) break;
                Assert.AreEqual(0f, v.FloorGlowBaseAlpha, 1e-6f, "done 뒤에는 상시 판이 꺼진다 — 두 겹은 맞교대다(T518)");
                float sum = g.x + g.y;
                float k = sum > 1e-6f ? g.y / sum : 0f;
                if (k < kMin) kMin = k;
                if (k > kMax) kMax = k;
                if (sum < sumMin) sumMin = sum;
                if (sum > sumMax) sumMax = sum;
            }
            Assert.Less(kMin, 0.08f, "한 주기 안에 0%/100% 틀 쪽 끝을 지난다");
            Assert.Greater(kMax, 0.92f, "한 주기 안에 50% 틀 쪽 끝을 지난다");
            // 두 판 알파의 합은 겹 색 알파(.26)여야 한다 — 교차항 보정만큼(최대 +1.7%) 위로 벗어난다.
            Assert.GreaterOrEqual(sumMin, want - 1e-3f, "합이 겹 알파 아래로 내려가지 않는다");
            Assert.LessOrEqual(sumMax, want * 1.08f, "합이 겹 알파보다 크게 넘지 않는다(교차항 보정 폭)");
            yield return null;
        }

        /// <summary>자 쪽에서 같은 왕복을 다시 푼다 — 제품 코드의 `Wave` 와 같은 뜻(정본 `0%,100% ↔ 50%` · 구간마다 ease-in-out).</summary>
        static float Breath(float since, float period, float lo, float hi)
        {
            var e = new Forge.Core.CraftFx.CssEase(0.42, 0, 0.58, 1);
            float p = Mathf.Repeat(since, period) / period;
            float k = p <= 0.5f ? (float)e.Ease(p / 0.5f) : 1f - (float)e.Ease((p - 0.5f) / 0.5f);
            return Mathf.Lerp(lo, hi, k);
        }

    }
}
