using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game;

namespace Forge.Tests.PlayMode
{
    /// <summary>T1 — 부팅 씬(SampleScene)을 열어 세로 9:16 카메라·안개·Bootstrap 이 서는지 본다. 빨간 로그가 나면 테스트 러너가 실패시킨다.</summary>
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

            float aspect = cam.pixelWidth / (float)cam.pixelHeight;
            Assert.AreEqual(Bootstrap.PortraitAspect, aspect, 0.02f, "레터박스 뒤 카메라 화면비는 9:16");

            Assert.IsTrue(RenderSettings.fog, "원작 setTheme 의 선형 안개");
            Assert.AreEqual(FogMode.Linear, RenderSettings.fogMode);
        }
    }
}
