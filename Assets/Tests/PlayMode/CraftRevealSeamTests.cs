using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Forging;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Gallery;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T87 34회차 — 33회차가 «아직 눈으로 안 봤다» 고 남긴 이음매: 리빌 카드(`craft-reveal` · 0.56초) 가 걷히고 **같은 틱에** 비교 팝업이 뜬다
    /// (정본 `ui.js` showCraftReveal: `setTimeout(() => { el.remove(); done(); }, REVEAL_CARD_MS)` · 클론 `ShowReveal`: `Delay(RevealCardSec, () => { DismissReveal(); done(); })`).
    /// 값으로는 «팝업이 열린 다음 프레임에 카드 오버레이가 없다» 를, 그림으로는 두 장(`screen_t87-seam-before` = 카드만 · `screen_t87-seam-after` = 팝업만)을 남겨 다음 사람이 나란히 본다(§1 · 결정 293 «두 장을 남겨라»).
    /// </summary>
    public class CraftRevealSeamTests
    {
        static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            yield return null;
        }

        static bool NoGraphics() { return SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null; }

        static Transform Reveal() { UiRoot r = UiRoot.Instance; return r == null || r.App == null ? null : r.App.Find("craft-reveal"); }

        [UnityTest]
        public IEnumerator 리빌_카드가_걷히는_같은_틱에_비교_팝업이_자리를_넘겨받고_다음_프레임엔_카드가_없다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            UiRoot root = UiRoot.Instance;
            h.S.Hammers = 20; h.Pull();
            h.OnCraft();
            // ① 망치질(1.5초) 뒤 리빌 카드가 선다 — 그동안 팝업은 없다
            float t = 0f;
            while (Reveal() == null && t < 6f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(Reveal(), "망치질 뒤 리빌 카드 오버레이(craft-reveal)가 서야 한다");
            Assert.IsFalse(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "카드가 떠 있는 동안 비교 팝업은 아직 없다");
            Assert.IsTrue(h.AnvilBusy, "카드가 떠 있는 동안 모루는 잠긴다");
            // 카드 수명의 절반쯤에서 한 장 — 카드만 있는 그림
            float half = 0f;
            while (half < ForgeHost.RevealCardSec * 0.45f && Reveal() != null) { half += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(Reveal(), "카드 수명 절반에서 카드가 아직 있다");
            if (!NoGraphics()) Capture("screen_t87-seam-before");
            // ② 팝업이 열리는 프레임 — 정본 `el.remove(); done()` 처럼 같은 틱에 넘겨받는다
            t = 0f;
            while (!h.Meta.Popups.IsOpen(ForgeCraftPopup.Name) && t < 4f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "리빌(0.56초) 뒤 비교 팝업이 떠야 한다");
            Assert.IsFalse(h.AnvilBusy, "팝업이 뜨면 모루가 풀린다");
            Transform sameTick = Reveal();   // Destroy 는 프레임 끝이라 같은 틱엔 남아 있을 수 있다 — 화면에는 안 그려진다
            yield return null;
            Assert.IsNull(Reveal(), "팝업이 열린 다음 프레임엔 카드 오버레이가 없다(자리를 넘겨받았다)" + (sameTick != null ? " · 같은 틱엔 파괴 대기였다" : ""));
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name));
            if (!NoGraphics()) Capture("screen_t87-seam-after");
            // 팝업 안의 비교 카드가 실제로 서 있다(넘겨받은 자리가 빈 자리가 아니다)
            Popup p = h.Meta.Popups.Find(ForgeCraftPopup.Name);
            Assert.IsNotNull(p);
            Assert.IsNotNull(p.Root, "팝업 판");
            Assert.Greater(p.Root.GetComponentsInChildren<UnityEngine.UI.Image>(true).Length, 2, "비교 팝업에 카드 그림이 있다");
        }

        /// <summary>T135·T134 와 같은 길 — 캔버스 사본 카메라 → RT → ReadPixels → `ui-screens/<name>.png`(CI 가 screens 브랜치로 올린다).</summary>
        static void Capture(string saveAs)
        {
            UiRoot root = UiRoot.Instance;
            Canvas canvas = root.Canvas;
            RenderMode prevMode = canvas.renderMode;
            Camera prevCam = canvas.worldCamera;
            float prevPlane = canvas.planeDistance;
            RenderTexture prevActive = RenderTexture.active;
            int w = Mathf.Max(64, Screen.width), hh = Mathf.Max(64, Screen.height);
            RenderTexture rt = new RenderTexture(w, hh, 24, RenderTextureFormat.ARGB32);
            GameObject camGo = new GameObject("t87-seam-cam");
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
                Texture2D tex = new Texture2D(w, hh, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0f, 0f, w, hh), 0, 0);
                tex.Apply(false);
                try { GallerySheet.Save(tex, saveAs); } catch (System.Exception e) { Debug.Log("[T87] PNG 저장 생략: " + e.Message); }
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
