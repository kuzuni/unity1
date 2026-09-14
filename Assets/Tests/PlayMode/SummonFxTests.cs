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
    /// 별은 done 뒤에만 켜진다. 그림은 `screen_summon-result` 류 PNG 눈 확인. 바닥 반사(.sr-reflect)는 2회차.
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
    }
}
