using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T349 — 촬영 카메라가 게임 카메라의 URP 추가 데이터까지 물려받는가. `Camera.CopyFrom` 만 한 카메라는 그 데이터가 없어(포스트 꺼짐)
    /// PNG 의 3D 띠가 톤맵·노출·색 보정 없이 찍힌다(런 503 실측). <see cref="ShotCam"/> 이 그 구멍을 한 자리에서 막는다.
    /// </summary>
    public class ShotCamTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && Camera.main != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            Assert.IsNotNull(Camera.main, "메인 카메라가 없다");
        }

        [UnityTest]
        public IEnumerator 게임_카메라는_URP_추가_데이터를_쥐고_CopyFrom_만_한_카메라는_그것을_잃는다()
        {
            yield return Boot();
            Camera main = Camera.main;
            UniversalAdditionalCameraData mainUrp = main.GetComponent<UniversalAdditionalCameraData>();
            Assert.IsNotNull(mainUrp, "씬의 메인 카메라에 UniversalAdditionalCameraData 가 없다 — URP 카메라가 아니다");
            Assert.IsTrue(mainUrp.renderPostProcessing, "게임 카메라는 포스트(톤맵·노출·색 보정)를 켜 둔다(씬 m_RenderPostProcessing: 1)");

            GameObject bareGo = new GameObject("t349-bare-cam");
            Camera bare = bareGo.AddComponent<Camera>();
            try
            {
                bare.CopyFrom(main);
                UniversalAdditionalCameraData bareUrp = bareGo.GetComponent<UniversalAdditionalCameraData>();
                // CopyFrom 은 추가 데이터를 안 옮긴다 — 없거나(컴포넌트 자체가 없다) 있어도 포스트가 꺼져 있다. 이것이 열여섯 자가 밟던 자리다.
                Assert.IsTrue(bareUrp == null || !bareUrp.renderPostProcessing, "CopyFrom 만으로 포스트가 켜졌다면 이 자의 전제(런 503)가 바뀐 것이다 — T349 절을 다시 읽어라");
                Assert.IsFalse(ShotCam.SameUrp(main, bare), "CopyFrom 만 한 카메라가 게임 카메라와 같은 URP 데이터를 쥐고 있다");
            }
            finally { Object.DestroyImmediate(bareGo); }
        }

        [UnityTest]
        public IEnumerator ShotCam_From_은_URP_추가_데이터까지_통째로_복사하고_RT_를_문다()
        {
            yield return Boot();
            Camera main = Camera.main;
            RenderTexture rt = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGB32);
            Camera cam = ShotCam.From(main, "t349-shot-cam", rt);
            try
            {
                Assert.IsTrue(ShotCam.SameUrp(main, cam), "촬영 카메라의 URP 추가 데이터가 게임 카메라와 다르다");
                UniversalAdditionalCameraData urp = cam.GetComponent<UniversalAdditionalCameraData>();
                Assert.IsNotNull(urp);
                Assert.IsTrue(urp.renderPostProcessing, "촬영 카메라의 포스트가 꺼져 있다 — PNG 가 톤맵·노출·색 보정 없이 찍힌다");
                Assert.AreSame(rt, cam.targetTexture, "RT 를 안 물었다");
                Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), cam.rect, "레터박스를 걷지 않았다");
                Assert.AreEqual(main.fieldOfView, cam.fieldOfView, 0.001f, "CopyFrom 이 안 됐다(시야각)");
                Assert.AreEqual(main.cullingMask, cam.cullingMask, "CopyFrom 이 안 됐다(cullingMask)");
                // 한 번 더 부르면(이미 데이터가 있는 카메라) 새 컴포넌트를 붙이지 않고 같은 것을 돌려준다
                UniversalAdditionalCameraData again = ShotCam.CopyUrp(main, cam);
                Assert.AreSame(urp, again, "이미 있는 추가 데이터를 두고 새로 붙였다");
                Assert.AreEqual(1, cam.GetComponents<UniversalAdditionalCameraData>().Length);
            }
            finally
            {
                cam.targetTexture = null;
                Object.DestroyImmediate(cam.gameObject);
                Object.DestroyImmediate(rt);
            }
        }
    }
}
