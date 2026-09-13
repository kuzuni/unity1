using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core;
using Forge.Game;
using Forge.Game.Gallery;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T54 — **UI 없이 세계만** 찍는 컷(`ui-screens/world_frame.png`). 왜 따로 찍나:
    /// 원작 캔버스 상자(`#game-area`) framing 은 `Bootstrap.ApplyGameAreaProjection` 이 카메라 **투영**으로 주는데,
    /// T27·T45 의 촬영은 오버레이 캔버스를 그 카메라의 `ScreenSpaceCamera` 로 잠시 옮겨 얹는 길이라
    /// 카메라 투영을 건드리면 **캔버스까지 따라 움직인다**(런 134 실측: 상단바 소실·시트 상승). 그래서 그 길은 기본 투영 그대로 두고,
    /// framing 판정은 캔버스가 아예 없는 이 컷으로 한다 — 세계만 있으니 투영을 마음껏 걸 수 있다.
    /// 판정은 «워커가 PNG 를 열어 영웅·적이 띠 안에 서는가» 이고(ROUTINE §1), 테스트 자체는 그림을 남기는 것까지만 한다.
    /// 띠(원작 `#game-area`)는 이 그림에서 세로 <c>topbar_h</c> ~ <c>sheet_top</c> — 540×960 에서 y 62~530 이다.
    /// </summary>
    public class WorldFrameShotTests
    {
        private const int ShotW = 540, ShotH = 960;

        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator 세계만_찍어_원작_캔버스_상자_framing_을_남긴다()
        {
            yield return Boot();
            Camera main = Camera.main;
            Assert.IsNotNull(main, "MainCamera 가 없다");

            // 전투가 한 판 굴러 적이 서고 영웅이 움직인 뒤에 찍는다(부팅 직후는 빈 무대다).
            float t = 0f;
            while (t < 3f) { t += Time.unscaledDeltaTime; yield return null; }

            if (!SystemInfo.graphicsDeviceType.ToString().Contains("Null"))
            {
                RenderTexture prevActive = RenderTexture.active;
                var rt = new RenderTexture(ShotW, ShotH, 24, RenderTextureFormat.ARGB32);
                var camGo = new GameObject("t54-world-frame-cam");
                var cam = camGo.AddComponent<Camera>();
                Texture2D shot = null;
                try
                {
                    cam.CopyFrom(main);
                    cam.rect = new Rect(0f, 0f, 1f, 1f);        // 레터박스만 걷는다(RT 가 곧 앱 상자다)
                    cam.targetTexture = rt;
                    Bootstrap.ApplyGameAreaProjection(cam);      // 게임과 같은 framing — 여기엔 캔버스가 없으니 UI 가 안 따라온다
                    cam.Render();
                    RenderTexture.active = rt;
                    shot = new Texture2D(ShotW, ShotH, TextureFormat.RGB24, false);
                    shot.ReadPixels(new Rect(0, 0, ShotW, ShotH), 0, 0);
                    shot.Apply(false);
                    GallerySheet.Save(shot, "world_frame");
                }
                finally
                {
                    RenderTexture.active = prevActive;
                    cam.targetTexture = null;
                    if (shot != null) Object.Destroy(shot);
                    Object.Destroy(camGo);
                    rt.Release();
                    Object.Destroy(rt);
                }
            }

            // 그림과 별개로 «셈» 은 여기서 못 박는다 — 띠 한가운데가 광축이고 띠 대역이 ±hb 다(Core 단언과 같은 계약).
            Viewport.FrustumEdges f = Viewport.GameAreaFrustum(main.nearClipPlane, main.fieldOfView,
                Bootstrap.PortraitAspect, Bootstrap.GameAreaTop, Bootstrap.GameAreaBottom);
            float hb = main.nearClipPlane * Mathf.Tan(main.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float top = Bootstrap.GameAreaTop, bottom = Bootstrap.GameAreaBottom;
            float yTop = f.Top - top * (f.Top - f.Bottom);
            float yBottom = f.Top - bottom * (f.Top - f.Bottom);
            Assert.AreEqual(hb, yTop, 1e-5f, "띠 윗변이 원작 카메라의 +hb");
            Assert.AreEqual(-hb, yBottom, 1e-5f, "띠 아랫변이 −hb");
        }
    }
}
