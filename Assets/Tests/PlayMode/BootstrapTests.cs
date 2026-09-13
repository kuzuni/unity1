using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core;
using Forge.Game;

namespace Forge.Tests.PlayMode
{
    /// <summary>T1 — 부팅 씬(SampleScene)을 열어 앱 상자 9:16 · 3D 카메라 띠(«#game-area» · T54)·안개·Bootstrap 이 서는지 본다. 빨간 로그가 나면 테스트 러너가 실패시킨다.</summary>
    public class BootstrapTests
    {
        [UnityTest]
        public IEnumerator 부팅_씬이_세로_9대16_원근_카메라로_선다()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            Bootstrap boot = Object.FindAnyObjectByType<Bootstrap>();
            Assert.IsNotNull(boot, "SampleScene 에 Bootstrap 이 없다");

            Camera cam = Camera.main;
            Assert.IsNotNull(cam, "MainCamera 태그 카메라가 없다");
            Assert.IsFalse(cam.orthographic, "원작은 원근 카메라(PerspectiveCamera)다");
            Assert.AreEqual(62f, cam.fieldOfView, 0.01f, "CAM_FOV 62");
            Assert.AreEqual(0.1f, cam.nearClipPlane, 1e-4f);
            Assert.AreEqual(100f, cam.farClipPlane, 1e-3f);

            // 앱 상자는 9:16 레터박스(T1) — 카메라가 쓰는 것은 그 안의 «#game-area» 띠다(T54 `b9c5fd9`: 상단바 밑 ~ 장비 시트 위).
            // 그래서 «카메라 화면비 = 9:16» 은 띠가 앱 상자 전체로 물러난 갈래(카탈로그가 비었을 때)에만 참이다.
            ViewportRect app = Viewport.Letterbox(Screen.width, Screen.height, Bootstrap.PortraitAspect);
            float appAspect = (Screen.width * app.W) / (Screen.height * app.H);
            Assert.AreEqual(Bootstrap.PortraitAspect, appAspect, 0.02f, "앱 상자(레터박스)는 9:16");

            ViewportRect band = Viewport.GameArea(app, Bootstrap.GameAreaTop, Bootstrap.GameAreaBottom);
            Assert.AreEqual(band.X, cam.rect.x, 1e-4f, "카메라 rect.x = #game-area 띠");
            Assert.AreEqual(band.Y, cam.rect.y, 1e-4f, "카메라 rect.y = #game-area 띠");
            Assert.AreEqual(band.W, cam.rect.width, 1e-4f, "카메라 rect.w = #game-area 띠");
            Assert.AreEqual(band.H, cam.rect.height, 1e-4f, "카메라 rect.h = #game-area 띠");
            Assert.Greater(cam.rect.height, 0f, "띠 높이가 0 이면 3D 가 한 픽셀도 안 나온다");
            Assert.LessOrEqual(cam.rect.height, app.H + 1e-4f, "띠는 앱 상자 안이다");

            // 띠가 앱 상자 전체로 물러난 갈래(카탈로그 키가 비었을 때)에서는 옛 계약대로 카메라도 9:16 이다.
            if (Mathf.Approximately(band.H, app.H))
            {
                float camAspect = cam.pixelWidth / (float)cam.pixelHeight;
                Assert.AreEqual(Bootstrap.PortraitAspect, camAspect, 0.02f, "띠가 앱 상자 전체면 카메라도 9:16");
            }

            Assert.IsTrue(RenderSettings.fog, "원작 setTheme 의 선형 안개");
            Assert.AreEqual(FogMode.Linear, RenderSettings.fogMode);
        }
    }
}
