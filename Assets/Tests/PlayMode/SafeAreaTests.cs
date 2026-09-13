using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game;
using Forge.Game.Gallery;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T45 — 노치 모의(safeArea 위 120px · 아래 60px 깎음 · 주인 지시 «상단 카메라 안 가리게»)에서 «눌러야 하는 것» 전부(HUD 상단바·스테이지 표시·탭바·탭 ✕·채팅줄·토스트·팝업 ✕·닫기)의
    /// 월드 코너 4점이 safeArea 안에 있는가 — 세 해상도(540×1170 · 360×800 · 430×932). 3D 카메라는 전체 화면 레터박스 그대로(safeArea 를 이중으로 깎지 않는다).
    /// 그래픽 장치가 있으면 노치 모의 PNG(`ui-screens/ui_safearea_notch.png` · 빨간 선 = 노치 경계)를 남긴다(§1 «실제 화면을 본다»). 빨간 로그는 러너가 실패시킨다.
    /// </summary>
    public class SafeAreaTests
    {
        /// <summary>검증 해상도(ROUTINE T45): 540×1170(촬영 기준) · 360×800 · 430×932.</summary>
        public static readonly int[][] Resolutions = { new[] { 540, 1170 }, new[] { 360, 800 }, new[] { 430, 932 } };
        public const string ShotName = "ui_safearea_notch";

        [TearDown]
        public void TearDown() { UiRoot.OverrideSafeArea(null); }

        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            Assert.IsNotNull(UiRoot.Instance, "UiRoot 가 Bootstrap 아래에 서지 않았다");
        }

        /// <summary>RectTransform 의 월드(=오버레이 캔버스 화면 픽셀) 코너 4점이 전부 rect 안인가.</summary>
        private static void AssertInside(RectTransform rt, Rect sa, string what, float tol = 0.51f)
        {
            Assert.IsNotNull(rt, what + " 가 없다");
            Assert.IsTrue(rt.gameObject.activeInHierarchy, what + " 가 꺼져 있다");
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            for (int i = 0; i < 4; i++)
            {
                Assert.GreaterOrEqual(c[i].x, sa.xMin - tol, what + " 코너 " + i + " 왼쪽 밖 x=" + c[i].x + " safe=" + sa);
                Assert.LessOrEqual(c[i].x, sa.xMax + tol, what + " 코너 " + i + " 오른쪽 밖 x=" + c[i].x + " safe=" + sa);
                Assert.GreaterOrEqual(c[i].y, sa.yMin - tol, what + " 코너 " + i + " 아래(홈바) 밖 y=" + c[i].y + " safe=" + sa);
                Assert.LessOrEqual(c[i].y, sa.yMax + tol, what + " 코너 " + i + " 위(노치·카메라) 밖 y=" + c[i].y + " safe=" + sa);
            }
        }

        private static RectTransform Child(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            return t != null ? t.GetComponent<RectTransform>() : null;
        }

        private static IEnumerator CheckAll(Rect sa, string tag)
        {
            UiRoot root = UiRoot.Instance;
            UiRoot.OverrideSafeArea(sa);
            yield return null;
            Assert.AreEqual(sa, UiRoot.EffectiveSafeArea);
            Rect app = root.AppScreenRect;
            Assert.LessOrEqual(app.yMax, sa.yMax + 0.51f, tag + " 앱 상자 윗변이 노치 아래");
            Assert.GreaterOrEqual(app.yMin, sa.yMin - 0.51f, tag + " 앱 상자 아랫변이 홈바 위");
            Assert.AreEqual(Bootstrap.PortraitAspect, app.width / app.height, 0.01f, tag + " 앱 상자는 9:16 그대로");

            // HUD 상단바 · 프로필 카드 · 스테이지 표시 · 웨이브 노드
            RectTransform topbar = Child(root.HudLayer, "topbar");
            AssertInside(topbar, sa, tag + " HUD 상단바");
            AssertInside(root.Hud.ProfileButton.GetComponent<RectTransform>(), sa, tag + " 프로필 카드");
            AssertInside(Child(root.HudLayer, "stage-label"), sa, tag + " 스테이지 표시");
            AssertInside(Child(root.HudLayer, "wave-pips"), sa, tag + " 웨이브 노드");
            // 채팅줄
            AssertInside(root.Chat, sa, tag + " 채팅줄");
            AssertInside(root.Hud.ChatButton.GetComponent<RectTransform>(), sa, tag + " 채팅 버튼");
            // 탭바 + 탭 5개 + popup 탭 ✕
            AssertInside(root.TabBand, sa, tag + " 탭바");
            foreach (string key in root.TabBar.Keys) AssertInside(root.TabBar.ButtonOf(key).GetComponent<RectTransform>(), sa, tag + " 탭 " + key);
            root.TabBar.SetPopupX("shop");
            yield return null;
            Assert.IsTrue(root.TabBar.IsX("shop"), tag + " 상점 탭 ✕");
            AssertInside(Child(root.TabBar.ButtonOf("shop").transform, "tab-x"), sa, tag + " 탭 ✕");
            root.TabBar.SetPopupX(null);
            // 토스트 · 팝업(스텁)의 닫기 버튼
            PopupLayer layer = PopupLayer.Create(root);
            layer.Toast("safe-area " + tag);
            yield return null;
            RectTransform toast = Child(Child(root.App, "toasts"), "toast");
            AssertInside(toast, sa, tag + " 토스트");
            Popup stub = layer.ShowStub("T45", "노치 모의");
            yield return null;
            RectTransform close = FindDeep(stub.Root, "close");
            AssertInside(close, sa, tag + " 팝업 닫기 버튼");
            layer.Hide(stub);
            yield return null;
        }

        private static RectTransform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t.GetComponent<RectTransform>();
            for (int i = 0; i < t.childCount; i++)
            {
                RectTransform r = FindDeep(t.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        [UnityTest]
        public IEnumerator 노치_모의_세_해상도에서_누르는_UI_전부_safeArea_안이다()
        {
            yield return Boot();
            Camera cam = Camera.main;
            Rect camRect = cam.pixelRect;
            foreach (int[] r in Resolutions)
            {
                Rect sa = UiRoot.NotchSafeArea(r[0], r[1]);
                Assert.AreEqual(UiRoot.NotchBottomPx, sa.yMin, 1e-4f);
                Assert.AreEqual(r[1] - UiRoot.NotchTopPx, sa.yMax, 1e-4f);
                yield return CheckAll(sa, r[0] + "x" + r[1]);
                // ⓒ 3D 카메라 rect 는 safeArea 를 이중으로 깎지 않는다(띠 자체는 원작 `#game-area` · T54)
                Assert.AreEqual(camRect, cam.pixelRect, r[0] + "x" + r[1] + " 3D 카메라 rect 는 safeArea 와 무관");
            }
            UiRoot.OverrideSafeArea(null);
            yield return null;
            Assert.AreEqual(Screen.safeArea, UiRoot.EffectiveSafeArea, "되돌리면 Screen.safeArea");
        }

        /// <summary>촬영 크기(ROUTINE §1 «540×1170 세로») — 실제 창 크기와 무관하게 RenderTexture 로 그린다(배치모드 에디터의 ScreenCapture 는 에디터 창을 찍는다 · CI 런 58 실측).</summary>
        public const int ShotW = 540, ShotH = 1170;

        [UnityTest]
        public IEnumerator 노치_모의_화면을_찍어_남긴다()
        {
            yield return Boot();
            Rect sa = UiRoot.NotchSafeArea(ShotW, ShotH);
            UiRoot.OverrideSafeArea(sa);
            yield return null;
            yield return CheckAll(sa, ShotW + "x" + ShotH + " 촬영");
            if (!GallerySheet.GraphicsAvailable)
            {
                Debug.LogWarning("그래픽 장치가 없다(-nographics) — 노치 모의 촬영을 건너뛴다");
                yield break;
            }
            PopupLayer.Instance.Toast("노치 모의 촬영");
            yield return null;
            // T5 시트와 같은 길: 3D 카메라 사본 → RenderTexture 540×1170 · UI 캔버스는 그 카메라의 ScreenSpaceCamera 로 잠시 옮겨 같은 그림에 얹는다(오버레이 캔버스는 RT 에 안 그려진다).
            UiRoot root = UiRoot.Instance;
            Canvas canvas = root.Canvas;
            RenderMode prevMode = canvas.renderMode;
            Camera prevCam = canvas.worldCamera;
            var rt = new RenderTexture(ShotW, ShotH, 24, RenderTextureFormat.ARGB32);
            var camGo = new GameObject("t45-shot-cam");
            var cam = camGo.AddComponent<Camera>();
            RenderTexture prevActive = RenderTexture.active;
            Texture2D shot = null;
            try
            {
                // T54 3회차 되돌림 — 옛 길(카메라 하나 · 전체 rect). 띠 rect 는 URP 가 세계를 안 그렸다(런 116).
                cam.CopyFrom(Camera.main);
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.targetTexture = rt;
                Bootstrap.ApplyGameAreaProjection(cam);   // T54 — 게임과 같은 framing(이 RT 는 540×1170 이라 비가 다르다 · 자가 그것을 잰다)
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                root.Layout();
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rt;
                shot = new Texture2D(ShotW, ShotH, TextureFormat.RGB24, false);
                shot.ReadPixels(new Rect(0, 0, ShotW, ShotH), 0, 0);
            }
            finally
            {
                RenderTexture.active = prevActive;
                cam.targetTexture = null;
                canvas.renderMode = prevMode;
                canvas.worldCamera = prevCam;
                Object.Destroy(camGo);
                rt.Release();
                Object.Destroy(rt);
                root.Layout();
            }
            Assert.IsNotNull(shot, "화면 캡처");
            // 노치 경계 = 빨간 선(위 120px · 아래 60px) · 노치 영역은 어둡게 덮는다(원점 = 왼쪽 아래)
            int w = shot.width, h = shot.height;
            Color32[] px = shot.GetPixels32();
            int top = Mathf.RoundToInt(sa.yMax), bottom = Mathf.RoundToInt(sa.yMin);
            for (int y = 0; y < h; y++)
            {
                bool notch = y >= top || y < bottom;
                bool line = y == top || y == top + 1 || y == bottom || y == bottom - 1;
                if (!notch && !line) continue;
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (line) px[i] = new Color32(242, 25, 29, 255);
                    else px[i] = new Color32((byte)(px[i].r / 3), (byte)(px[i].g / 3), (byte)(px[i].b / 3), 255);
                }
            }
            shot.SetPixels32(px);
            shot.Apply(false, false);
            string file = GallerySheet.Save(shot, ShotName);
            Debug.Log("T45 노치 모의 화면: " + file + " (" + w + "x" + h + " · 빨간 선 y=" + top + "," + bottom + ")");
            Object.Destroy(shot);
        }
    }
}
