using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.BattleFx;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T173 — 보스 경고 두 겹이 정본의 **방사형 그라디언트 + screen 합성**인가, 그리고 팝업이 떠 있을 때 경고 딤이 꺼지는가.
    ///
    /// 클론은 여태 `.bw-dim`·`.bw-flash` 가 **단색 판 한 장**이고 점멸이 보통 알파라, 연출이 도는 순간에 찍힌 화면이
    /// 씬 대역 통째로 한 색이 됐다(런 403 `gear-detail` 의 `(90,14,11)`). 여기서는 **구운 픽셀**로 그 셋을 잰다.
    /// </summary>
    public class BossWarnArtTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(UiRoot.Instance != null && UiRoot.Instance.App != null && MetaHost.Ready) && t < 20f)
            { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(UiRoot.Instance, "UiRoot 가 20초 안에 안 섰다");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 감광은_가운데가_옅고_가장자리가_짙다()
        {
            yield return Boot();
            BattleOverlay o = BattleOverlay.Ensure();
            o.BossWarning(2.0);
            yield return null;

            Image dim = Find(o.Layer, "bw-dim");
            Assert.IsNotNull(dim, "bw-dim");
            Assert.IsNotNull(dim.sprite, "방사형이 구워졌다 — 단색 판이면 스프라이트가 없다");
            Texture2D tex = dim.sprite.texture;

            // 정본 `radial-gradient(ellipse at 50% 42%, rgba(72,4,4,.34), rgba(6,2,4,.76))`
            // — 중심(위에서 42%)이 .34 · 가장 먼 모서리가 .76.
            int cx = Mathf.RoundToInt(tex.width * 0.5f);
            int cy = Mathf.RoundToInt(tex.height * (1f - 0.42f));   // 텍스처는 아래가 0행
            float mid = tex.GetPixel(cx, cy).a;
            float corner = tex.GetPixel(1, 1).a;
            Assert.AreEqual(0.34f, mid, 0.02f, "가운데 알파 = 정본 rgba(72,4,4,**.34**)");
            Assert.AreEqual(0.76f, corner, 0.03f, "가장 먼 모서리 = rgba(6,2,4,**.76**)");
            Assert.Less(mid, corner, "가운데가 가장자리보다 **옅다** — 단색 판이면 둘이 같다");
            // 모서리가 비어 있으면(타원 밖 투명) 정본과 다르다 — farthest-corner 로 상자를 꽉 채워야 한다
            Assert.Greater(tex.GetPixel(tex.width - 2, tex.height - 2).a, 0.5f, "네 모서리 다 칠해져 있다");
        }

        [UnityTest]
        public IEnumerator 점멸은_가산_합성이고_가운데가_가장_밝다()
        {
            yield return Boot();
            BattleOverlay o = BattleOverlay.Ensure();
            o.BossWarning(2.0);
            yield return null;

            Image flash = Find(o.Layer, "bw-flash");
            Assert.IsNotNull(flash, "bw-flash");
            Assert.IsNotNull(flash.sprite, "방사형이 구워졌다");
            Texture2D tex = flash.sprite.texture;
            int cx = Mathf.RoundToInt(tex.width * 0.5f);
            int cy = Mathf.RoundToInt(tex.height * (1f - 0.42f));
            Color mid = tex.GetPixel(cx, cy);
            Color far = tex.GetPixel(1, 1);
            Assert.AreEqual(0.62f, mid.a, 0.02f, "정본 rgba(255,72,48,**.62**)");
            Assert.AreEqual(0.34f, far.a, 0.02f, "70% 뒤는 마지막 색 그대로 — rgba(190,10,10,**.34**)");
            Assert.Greater(mid.r + mid.g + mid.b, far.r + far.g + far.b, "가운데가 더 밝다(경광등)");

            // 정본 `mix-blend-mode: screen` — 이 레포의 그 재질은 T87 이 세운 `Forge/UiScreen` 이다.
            // 셰이더를 못 찾으면 재질이 null 이고 여태처럼 보통 알파로 그려진다(연출이 사라지는 것보다 낫다 · CraftFxPoly.Screen 주석).
            if (CraftFxPoly.Screen() != null)
            {
                Assert.IsNotNull(flash.material, "점멸은 screen 재질로 그린다");
                Assert.AreEqual(CraftFxPoly.ScreenShaderName, flash.material.shader.name, "정본 mix-blend-mode: screen");
            }
        }

        [UnityTest]
        public IEnumerator 팝업이_떠_있으면_경고_딤만_꺼진다()
        {
            yield return Boot();
            BattleOverlay o = BattleOverlay.Ensure();
            MetaHost h = MetaHost.Instance;

            o.BossWarning(2.0);
            o.Tick(0.5f);                 // 감광은 앞 8% 에 차오른다(FxRules.WarnDimIn) — 한가운데 프레임으로 민다
            Image dim = Find(o.Layer, "bw-dim");
            Assert.Greater(dim.color.a, 0.5f, "팝업이 없을 때는 딤이 보인다");

            PassPopup.Open(h);
            yield return null;
            o.Tick(0.1f);                 // 팝업이 뜬 채로 한 박자 더 — 정본은 그 순간 딤을 끈다
            yield return null;
            Assert.AreEqual(0f, dim.color.a, 1e-4f, "정본 363 — 팝업이 떠 있으면 경고 딤은 끈다(모달 딤과 겹쳐 화면이 검게 죽는다)");
            Assert.IsNotNull(Find(o.Layer, "bw-banner"), "배너는 그대로 둔다 — 카드 옆 여백에서 «보스 온다» 가 읽혀야 한다");

            h.Popups.Hide(PassPopup.Name);
            yield return null;
        }

        /// <summary>T173 2회차 — §1 «실제 화면을 본다»: 촬영 목록에 보스 경고 프레임이 없다(T176 뒤로는 촬영이 경고가 꺼질 때까지 기다린다).
        /// 연출 한가운데 프레임(딤 가득 · 점멸 정점)과 팝업이 뜬 프레임(딤만 꺼짐)을 한 장씩 굽는다 — 눈 확인용 · 실패해도 판정을 안 흔든다(T179 길).
        /// 씬까지 같이 그린다(카메라 마스크를 안 좁힌다) — 검정 위에서는 방사형 감광이 안 읽힌다.</summary>
        [UnityTest]
        public IEnumerator T173_눈_확인용_보스_경고_프레임_두_장을_굽는다()
        {
            yield return Boot();
            BattleOverlay o = BattleOverlay.Ensure();
            MetaHost h = MetaHost.Instance;

            o.BossWarning(2.0);
            o.Tick((float)(FxRules.BossWarnDur * FxRules.WarnFlashPeak));   // 점멸 정점(u .12) · 딤은 이미 가득(WarnDimIn .08)
            yield return null;
            Canvas.ForceUpdateCanvases();
            Image dim = Find(o.Layer, "bw-dim");
            Assert.IsNotNull(dim, "bw-dim");
            Assert.Greater(dim.color.a, 0.5f, "한가운데 프레임 — 딤이 켜져 있다");
            Capture("screen_t173-boss-warn");

            PassPopup.Open(h);
            yield return null;
            o.Tick(0.05f);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.AreEqual(0f, dim.color.a, 1e-4f, "팝업이 뜬 프레임 — 딤은 꺼진다(정본 363)");
            Capture("screen_t173-boss-warn-popup");

            h.Popups.Hide(PassPopup.Name);
            yield return null;
        }

        /// <summary>UI 와 씬을 한 장 그린다(T179 `SummonFxTests.Capture` 와 같은 길 · 마스크만 안 좁힌다) — 눈 확인용 · 실패해도 판정을 안 흔든다.</summary>
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
            GameObject camGo = new GameObject("t173-pixel-cam");
            Camera cam = camGo.AddComponent<Camera>();
            try
            {
                if (Camera.main != null) cam.CopyFrom(Camera.main);
                // T349 — `CopyFrom` 은 URP 추가 데이터(renderPostProcessing·volumeLayerMask·antialiasing·renderShadows)를
                //        **안 옮긴다** — 그래서 촬영 PNG 의 3D 띠가 톤맵·노출·색 보정 없이 찍혔다(런 503 실측).
                ShotCam.CopyUrp(Camera.main, cam);
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.targetTexture = rt;
                cam.ResetProjectionMatrix();
                cam.cullingMask |= 1 << canvas.gameObject.layer;
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
                try { Forge.Game.Gallery.GallerySheet.Save(tex, saveAs); } catch (System.Exception e) { Debug.Log("[T173] PNG 저장 생략: " + e.Message); }
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

        /// <summary>
        /// T368 2회차 — 정본 style.css 395 `.bw-hazard` 는 **−45° 되풀이 줄무늬**다
        /// (`repeating-linear-gradient(-45deg, #ffca28 0 .55rem, #16100a .55rem 1.1rem)` · `background-size: 1.556rem`).
        /// 클론은 **세로 대시 24개**였다 — 각도가 0 이라 화면에서 «빗금» 이 아니라 «창살» 로 보였고 색도 카탈로그 근사였다.
        /// 여기서 재는 것 넷: ⓐ 굽은 판이 붙었는가(단색·대시 조각이 아니다) ⓑ 한 타일 가로가 정본 1.556rem 인가
        /// ⓒ 화소가 **사선**인가(오른쪽 위로 간 화소는 같은 색 · 바로 오른쪽은 다른 색이 나온다) ⓓ 두 색이 정본 값인가.
        /// </summary>
        [UnityTest]
        public IEnumerator 보스_경고_띠는_정본_사선_줄무늬다()
        {
            yield return Boot();
            BattleOverlay o = BattleOverlay.Ensure();
            o.BossWarning(2.0);
            yield return null;

            Image st = Find(o.Layer, "stripe");
            Assert.IsNotNull(st, "구운 줄무늬 판(stripe) 이 붙었다 — 대시 조각을 늘어놓던 자리다");
            Assert.IsNotNull(st.sprite, "줄무늬는 구워서 얹는다(단색 판이면 스프라이트가 없다)");
            Assert.AreEqual(Image.Type.Tiled, st.type, "되풀이는 Tiled 가 맡는다(화면 폭만큼 굽지 않는다)");
            Assert.IsFalse(st.raycastTarget, "장식은 클릭을 안 먹는다(정본 pointer-events: none 갈래)");

            float rem = BattleOverlay.Rem;
            float want = (float)Forge.Core.Ui.StripeRules.TileWidth(-45, 1.1f * rem, 1e9);
            Assert.AreEqual(1.556f * rem, want, rem * 0.01f, "정본이 적어 둔 background-size 1.556rem 과 같은 수가 나온다");

            Texture2D tex = st.sprite.texture;
            Assert.Greater(tex.width, 1); Assert.Greater(tex.height, 1);
            Color[] px = tex.GetPixels();
            int W = tex.width, H = tex.height;

            // ⓒ 사선인가 — «오른쪽 위로 한 칸» 이 같은 색인 화소가 대부분이어야 한다(세로 창살이면 «바로 위» 가 같다)
            int upRightSame = 0, upSame = 0, pairs = 0;
            for (int y = 0; y < H - 1; y++)
                for (int x = 0; x < W - 1; x++)
                {
                    Color a = px[y * W + x];
                    // 텍스처 y 는 **아래가 0** 이므로 화면의 «오른쪽 위» = 인덱스 (x+1, y+1). 정본 −45° 의 등가선이 그것이다.
                    if (Same(a, px[(y + 1) * W + x + 1])) upRightSame++;
                    if (Same(a, px[(y + 1) * W + x])) upSame++;
                    pairs++;
                }
            Assert.Greater(pairs, 0);
            Assert.Greater(upRightSame, upSame, "−45° 면 «비스듬한 이웃» 이 «바로 위» 보다 더 자주 같다(세로 창살이면 반대다)");
            Assert.Greater(upRightSame / (float)pairs, 0.9f, "비스듬한 이웃은 거의 늘 같은 색이다 — 그것이 곧 45° 띠다");

            // ⓓ 두 색이 정본 값인가(#ffca28 · #16100a)
            Color ink = Hex("#FFCA28"), gap = Hex("#16100A");
            int nInk = 0, nGap = 0, nOther = 0;
            for (int i = 0; i < px.Length; i++)
            {
                if (Same(px[i], ink)) nInk++;
                else if (Same(px[i], gap)) nGap++;
                else nOther++;
            }
            Assert.AreEqual(0, nOther, "정본 두 색 말고는 안 쓴다(카탈로그 근사색이 아니다)");
            Assert.Greater(nInk, 0, "노랑 #ffca28"); Assert.Greater(nGap, 0, "검정 #16100a");
            Assert.AreEqual(0.5f, nInk / (float)px.Length, 0.08f, "대시 .55rem / 주기 1.1rem = 반반");
            yield return null;
        }

        private static bool Same(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.02f && Mathf.Abs(a.g - b.g) < 0.02f && Mathf.Abs(a.b - b.b) < 0.02f && Mathf.Abs(a.a - b.a) < 0.02f;
        }

        private static Color Hex(string s)
        {
            Color c;
            Assert.IsTrue(ColorUtility.TryParseHtmlString(s, out c), s);
            return c;
        }

        private static Image Find(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root.GetComponent<Image>() ?? root.GetComponentInChildren<Image>(true);
            for (int i = 0; i < root.childCount; i++)
            {
                Image hit = Find(root.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }
    }
}
