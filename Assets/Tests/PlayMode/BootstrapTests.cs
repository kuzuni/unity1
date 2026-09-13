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

            // 앱 상자는 9:16 레터박스(T1)이고 **카메라 rect 도 그것**이다.
            // T54 가 rect 를 원작 `#game-area` 띠로 좁혀 봤지만 URP 가 그 rect 에서 세계를 한 픽셀도 안 그렸다(런 116 실측 · 되돌림 · T70 은 그 계약으로 옮겼던 것을 여기서 함께 되돌린다).
            // 원작 캔버스 상자 framing 은 rect 가 아니라 투영 행렬로 줄 자리다 — 그때 이 단언은 그대로 두고 투영을 따로 본다.
            ViewportRect app = Viewport.Letterbox(Screen.width, Screen.height, Bootstrap.PortraitAspect);
            float appAspect = (Screen.width * app.W) / (Screen.height * app.H);
            Assert.AreEqual(Bootstrap.PortraitAspect, appAspect, 0.02f, "앱 상자(레터박스)는 9:16");

            Assert.AreEqual(app.X, cam.rect.x, 1e-4f, "카메라 rect = 앱 상자");
            Assert.AreEqual(app.Y, cam.rect.y, 1e-4f);
            Assert.AreEqual(app.W, cam.rect.width, 1e-4f);
            Assert.AreEqual(app.H, cam.rect.height, 1e-4f);
            Assert.Greater(cam.rect.height, 0f, "높이가 0 이면 3D 가 한 픽셀도 안 나온다");

            float camAspect = cam.pixelWidth / (float)cam.pixelHeight;
            Assert.AreEqual(Bootstrap.PortraitAspect, camAspect, 0.02f, "레터박스 뒤 카메라 화면비는 9:16");

            Assert.IsTrue(RenderSettings.fog, "원작 setTheme 의 선형 안개");
            Assert.AreEqual(FogMode.Linear, RenderSettings.fogMode);
        }
    }
}
