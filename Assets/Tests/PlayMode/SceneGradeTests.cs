using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Data;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T341 — 정본 `#game3d { filter: saturate(1.12) contrast(1.07) }`(style.css 152 · «컬러 그레이딩 — 무보정 에디터 뷰포트 인상 제거»)가
    /// 클론에서는 URP 볼륨 `ForgeVolume.asset` 의 ColorAdjustments 로 서 있는가. 값은 에셋(데이터)이 쥐고 기대값은 `Resources/SceneGradeUi.json` —
    /// 자는 «씬에 실제로 선 볼륨의 프로필» 에서 읽는다(에셋 파일을 직접 읽지 않는다 · 씬이 다른 프로필을 물고 있으면 그것이 빨강이어야 한다).
    /// 경계도 본다: 정본은 3D 캔버스 한 장에만 걸고 HUD(#app)는 안 물든다 — 클론의 앱 캔버스는 ScreenSpaceOverlay 라 볼륨 밖이다.
    /// 그림 판정(세계 컷 PNG 의 채도·대비가 원작 쪽으로 움직였는가)은 워커가 `world_frame.png` 를 열어 본다(ROUTINE §1·§5).
    /// </summary>
    public class SceneGradeTests
    {
        static JsonObject Table()
        {
            TextAsset ta = Resources.Load<TextAsset>("SceneGradeUi");
            Assert.IsNotNull(ta, "Resources/SceneGradeUi.json 이 안 실렸다(.meta 짝 · T182 자)");
            return J.Obj(J.Require(MiniJson.ParseObject(ta.text), "layout"));
        }

        static double L(JsonObject t, string key) { return J.Num(J.Require(t, key)); }

        [UnityTest]
        public IEnumerator 씬_볼륨의_ColorAdjustments_가_정본_채도_대비_눈금으로_켜져_있다()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            JsonObject t = Table();
            // 표 자체의 일관성 — CSS 계수와 URP 눈금이 같은 뜻(1 + v/100)인지
            Assert.AreEqual(L(t, "css_saturate"), 1.0 + L(t, "saturation") / 100.0, 1e-9, "표: saturate(1.12) ↔ +12");
            Assert.AreEqual(L(t, "css_contrast"), 1.0 + L(t, "contrast") / 100.0, 1e-9, "표: contrast(1.07) ↔ +7");

            Volume volume = null;
            foreach (Volume v in Resources.FindObjectsOfTypeAll<Volume>())
                if (v.gameObject.scene.isLoaded) { volume = v; break; }
            Assert.IsNotNull(volume, "씬에 Volume 이 없다(World.FindSceneRefs 가 찾는 그것)");
            Assert.IsNotNull(volume.sharedProfile, "Volume 이 프로필을 안 물고 있다");
            ColorAdjustments ca;
            Assert.IsTrue(volume.profile.TryGet(out ca), "프로필에 ColorAdjustments 가 없다");
            Assert.IsTrue(ca.active, "ColorAdjustments 가 꺼져 있다");
            Assert.IsTrue(ca.saturation.overrideState, "saturation override 가 꺼져 있다 — 정본 saturate(1.12) 자리가 빈다");
            Assert.IsTrue(ca.contrast.overrideState, "contrast override 가 꺼져 있다 — 정본 contrast(1.07) 자리가 빈다");
            double tol = L(t, "tol");
            Assert.AreEqual(L(t, "saturation"), ca.saturation.value, tol, "saturation = 표(+12)");
            Assert.AreEqual(L(t, "contrast"), ca.contrast.value, tol, "contrast = 표(+7)");
            // 이미 있던 노출은 그대로(이 작업이 건드리지 않는다) — World 가 정본 toneMappingExposure 에서 매 테마 다시 쓴다
            Assert.IsTrue(ca.postExposure.overrideState, "postExposure override 는 원래 켜져 있었다(T9)");

            // 경계 — 앱 캔버스는 볼륨 밖(정본: #app 은 #game3d 의 형제라 필터가 안 닿는다)
            Assert.IsNotNull(UiRoot.Instance, "UiRoot");
            Canvas appCanvas = UiRoot.Instance.GetComponentInParent<Canvas>() ?? UiRoot.Instance.GetComponentInChildren<Canvas>();
            Assert.IsNotNull(appCanvas, "앱 캔버스");
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, appCanvas.rootCanvas.renderMode, "HUD 는 포스트 밖(정본과 같은 경계)");
            Debug.Log("[T341] saturation " + ca.saturation.value + " · contrast " + ca.contrast.value + " · postExposure " + ca.postExposure.value.ToString("0.###") + " · 앱 캔버스 " + appCanvas.rootCanvas.renderMode);
        }

        /// <summary>
        /// 화소 판정 — 같은 프레임을 «두 override 켬 / 끔» 으로 두 번 그려(포스트 켠 카메라 · 게임 투영 · 띠만) 평균 채도의 비가 표의 saturate(1.12) 근처이고 휘도 표준편차(대비)도 오르는가.
        /// 왜 이렇게 재나: 레포의 촬영 카메라들은 `Camera.CopyFrom` 만 해서 URP 추가 데이터(renderPostProcessing)가 기본 false — **PNG 는 톤맵·노출·그레이드 없이 찍힌다**(런 503 실측: 세계 컷 채도 .2559 → .2569 · 결정 537).
        /// 그래서 «켬/끔» 을 한 카메라에서 직접 견준다 — 톤맵·노출은 양쪽에 똑같이 들어 있어 차이는 이 두 값뿐이다.
        /// </summary>
        [UnityTest]
        public IEnumerator 채도_대비_override_가_실제_화소를_표_비율만큼_움직인다()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            if (SystemInfo.graphicsDeviceType.ToString().Contains("Null")) { Debug.Log("[T341] Null 그래픽 장치 — 화소 판정 생략"); yield break; }
            Camera main = Camera.main;
            Assert.IsNotNull(main, "MainCamera");
            float t = 0f;
            while (t < 1.5f) { t += Time.unscaledDeltaTime; yield return null; }   // 세계가 서고 적이 나온 뒤

            JsonObject tb = Table();
            Volume volume = null;
            foreach (Volume v in Resources.FindObjectsOfTypeAll<Volume>())
                if (v.gameObject.scene.isLoaded) { volume = v; break; }
            Assert.IsNotNull(volume);
            ColorAdjustments ca;
            Assert.IsTrue(volume.profile.TryGet(out ca));

            const int W = 270, H = 480;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var camGo = new GameObject("t341-grade-cam");
            var cam = camGo.AddComponent<Camera>();
            Texture2D on = null, off = null;
            RenderTexture prev = RenderTexture.active;
            try
            {
                cam.CopyFrom(main);
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.targetTexture = rt;
                Bootstrap.ApplyGameAreaProjection(cam);
                var urp = camGo.GetComponent<UniversalAdditionalCameraData>() ?? camGo.AddComponent<UniversalAdditionalCameraData>();
                urp.renderPostProcessing = true;   // 볼륨이 닿게 — CopyFrom 은 이것을 안 옮긴다
                on = Shoot(cam, rt, W, H);
                ca.saturation.overrideState = false; ca.contrast.overrideState = false;
                off = Shoot(cam, rt, W, H);
                ca.saturation.overrideState = true; ca.contrast.overrideState = true;

                int y0 = Mathf.RoundToInt(Bootstrap.GameAreaTop * H), y1 = Mathf.RoundToInt(Bootstrap.GameAreaBottom * H);
                double satOn, stdOn, satOff, stdOff;
                Band(on, y0, y1, out satOn, out stdOn);
                Band(off, y0, y1, out satOff, out stdOff);
                double ratio = satOff > 1e-6 ? satOn / satOff : 0.0;
                double want = L(tb, "css_saturate");
                Debug.Log("[T341] 띠 평균 채도 켬 " + satOn.ToString("0.0000") + " / 끔 " + satOff.ToString("0.0000") + " = ×" + ratio.ToString("0.000") + " (표 " + want + ") · 휘도σ 켬 " + stdOn.ToString("0.0000") + " / 끔 " + stdOff.ToString("0.0000"));
                Assert.Greater(satOff, 0.02, "끔 상태의 띠가 무채색이다 — 세계가 안 섰거나 포스트가 안 도는 카메라");
                Assert.AreEqual(want, ratio, L(tb, "pixel_ratio_tol"), "켬/끔 평균 채도 비 = 정본 saturate(1.12) 근처(톤맵 뒤라 정확히 같진 않다 · 표 pixel_ratio_tol)");
                Assert.Greater(stdOn, stdOff, "대비(휘도 표준편차)가 오른다 — contrast(1.07)");
            }
            finally
            {
                RenderTexture.active = prev;
                cam.targetTexture = null;
                if (on != null) Object.Destroy(on);
                if (off != null) Object.Destroy(off);
                Object.Destroy(camGo);
                rt.Release();
                Object.Destroy(rt);
            }
        }

        static Texture2D Shoot(Camera cam, RenderTexture rt, int w, int h)
        {
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply(false);
            return tex;
        }

        /// <summary>띠(y0~y1 · 위에서 셈)의 평균 HSV 채도와 휘도 표준편차 — 워커가 PNG 를 재는 스크립트와 같은 식.</summary>
        static void Band(Texture2D tex, int y0, int y1, out double sat, out double lstd)
        {
            Color32[] px = tex.GetPixels32();
            int w = tex.width, h = tex.height, n = 0; double s = 0, l = 0, l2 = 0;
            for (int yy = y0; yy < y1; yy++)
            {
                int row = (h - 1 - yy) * w;   // GetPixels32 는 아래가 0
                for (int x = 0; x < w; x++)
                {
                    Color32 c = px[row + x];
                    int mx = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), mn = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                    s += mx > 0 ? (mx - mn) / (double)mx : 0.0;
                    double lum = (0.2126 * c.r + 0.7152 * c.g + 0.0722 * c.b) / 255.0;
                    l += lum; l2 += lum * lum; n++;
                }
            }
            sat = s / n; double ml = l / n; lstd = System.Math.Sqrt(System.Math.Max(0.0, l2 / n - ml * ml));
        }
    }
}
