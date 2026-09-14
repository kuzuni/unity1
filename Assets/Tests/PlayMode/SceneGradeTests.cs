using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Data;
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
    }
}
