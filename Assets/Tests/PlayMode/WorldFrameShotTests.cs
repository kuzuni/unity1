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
                    // T341 — CopyFrom 은 URP 추가 데이터를 안 옮겨 포스트(톤맵·노출·색 보정)가 꺼진 채 찍혔다(런 503 실측 · 결정 537). 게임이 보는 그대로 찍는다.
                    var urp = camGo.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() ?? camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                    urp.renderPostProcessing = true;
                    cam.Render();
                    RenderTexture.active = rt;
                    shot = new Texture2D(ShotW, ShotH, TextureFormat.RGB24, false);
                    shot.ReadPixels(new Rect(0, 0, ShotW, ShotH), 0, 0);
                    shot.Apply(false);
                    GallerySheet.Save(shot, "world_frame");
                    AssertNotEmpty(shot);
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

        /// <summary>
        /// T54 촬영 단언 — «비었는데 초록» 막이. 띠(원작 `#game-area`) 안 가로 중앙 60% 가 **배경·하늘 한 색이 아니어야** 한다:
        /// 거기에 영웅·적이 서기 때문이다. 임계는 실측으로 잡았다(런 139 `world_frame.png`: 최빈색 **40.6%** · 고유색 **2290**).
        /// 세계가 비면 그 자리는 안개색 한 가지다(최빈 100% · 고유색 1) — T54 이전 촬영 30장이 정확히 그 꼴이었다.
        /// </summary>
        private static void AssertNotEmpty(Texture2D shot)
        {
            int y0 = Mathf.RoundToInt(Bootstrap.GameAreaTop * shot.height);
            int y1 = Mathf.RoundToInt(Bootstrap.GameAreaBottom * shot.height);
            int x0 = Mathf.RoundToInt(0.2f * shot.width), x1 = Mathf.RoundToInt(0.8f * shot.width);
            var seen = new System.Collections.Generic.Dictionary<int, int>();
            int total = 0, best = 0;
            for (int y = y0; y < y1; y += 2)
            {
                // Texture2D 는 아래가 0 이고 띠 비율은 «위에서부터» 라 뒤집어 읽는다.
                int ty = shot.height - 1 - y;
                for (int x = x0; x < x1; x += 2)
                {
                    Color32 c = shot.GetPixel(x, ty);
                    int key = (c.r << 16) | (c.g << 8) | c.b;
                    int n;
                    seen.TryGetValue(key, out n);
                    n++;
                    seen[key] = n;
                    if (n > best) best = n;
                    total++;
                }
            }
            Assert.Greater(total, 0, "띠 표본이 0 이다");
            float topShare = best / (float)total;
            Assert.Less(topShare, 0.85f, "띠 중앙이 한 색으로 덮였다(= 세계에 아무도 없다) · 최빈색 비율 " + topShare.ToString("P1"));
            Assert.Greater(seen.Count, 50, "띠 중앙 색이 " + seen.Count + "종뿐 — 하늘만 찍혔다");
        }
    }
}
