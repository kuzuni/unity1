using System;
using System.Collections;
using UnityEngine.SceneManagement;
using Forge.Game;
using Forge.Game.Gallery;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using Forge.Core.Ui;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T342 ⓐⓓ — 정본 CSS `filter` 가 **화소로** 걸렸는가.
    /// ⓐ `.equip-cell.empty .cell-img.dim`(style.css 862) `grayscale(1) brightness(1.75) opacity(.52)`
    /// ⓓ `.hatch-cone`(8025) `blur(1.2px)`
    /// </summary>
    public class UiFilterTests
    {
        GameObject root;

        static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null; yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            yield return null;
        }

        /// <summary>T134·T135 와 같은 촬영 길(캔버스를 임시 카메라로 한 판 찍는다).</summary>
        static void Capture(string saveAs)
        {
            UiRoot uiRoot = UiRoot.Instance;
            Canvas canvas = uiRoot.Canvas;
            RenderMode prevMode = canvas.renderMode;
            Camera prevCam = canvas.worldCamera;
            float prevPlane = canvas.planeDistance;
            RenderTexture prevActive = RenderTexture.active;
            int w = Mathf.Max(64, Screen.width), h = Mathf.Max(64, Screen.height);
            RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            GameObject camGo = new GameObject("t342-pixel-cam");
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
                uiRoot.Layout();
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
                tex.Apply(false);
                try { GallerySheet.Save(tex, saveAs); } catch (Exception e) { Debug.Log("[T342] PNG 저장 생략: " + e.Message); }
                UnityEngine.Object.Destroy(tex);
            }
            finally
            {
                RenderTexture.active = prevActive;
                canvas.renderMode = prevMode;
                canvas.worldCamera = prevCam;
                canvas.planeDistance = prevPlane;
                uiRoot.Layout();
                cam.targetTexture = null;
                UnityEngine.Object.Destroy(camGo);
                UnityEngine.Object.Destroy(rt);
            }
        }

        /// <summary>
        /// §1 «실제 화면을 본다» — 빈 장비 칸은 **촬영된 어느 화면에도 안 나온다**(촬영 세이브가 모든 칸을 채우고 있다).
        /// 그래서 이 자가 칸 하나를 비우고 대장간 시트를 다시 그려 **그 상태만** 한 장 남긴다 — 다음 회차가 눈으로 본다.
        /// </summary>
        [UnityTest]
        public IEnumerator 가_빈_장비_칸_한_장을_남긴다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            string slot = h.Defs.Slots[0];
            var keep = h.Gear.Get(slot);
            h.Gear.Set(slot, null);
            ForgeSheet.Render(h);
            yield return null; yield return null;
            Capture("screen_t342-empty-cell");

            // 촬영 한 장으로는 못 가린다(칸이 화면에서 27px 이라 회색·밝기를 눈으로 못 읽는다) —
            // **실물 아틀라스 경로**를 여기서 잰다: 내 단위 자는 제가 만든 읽히는 스프라이트를 쓰므로
            // «진짜 아이콘이 걸러졌는가» 는 한 번도 안 봤다(그것이 이 칸의 까닭이다).
            Image cell = FindEmptyCellIcon(slot);
            Assert.IsNotNull(cell, "빈 칸의 아이콘 Image 를 못 찾았다 — 칸을 비웠는데 «빈 칸» 으로 안 그려졌다는 뜻이다");
            Assert.AreEqual(0.52f, cell.color.a, 1e-3f, "정본 opacity(.52)");
            Assert.IsNotNull(cell.sprite, "빈 칸 아이콘에 스프라이트가 없다");
            StringAssert.StartsWith("filt-equip_cell_empty", cell.sprite.name,
                "실물 아이콘이 **안 걸러졌다** — UiFilter 가 원본을 그대로 돌려줬다(읽기 경로가 null 을 냈을 때 그렇게 된다)");

            // 걸러진 화소가 실제로 회색인가 — 정본 grayscale(1) 이면 R=G=B 다
            Texture2D t = cell.sprite.texture;
            int gray = 0, seen = 0;
            for (int y = 0; y < t.height; y += 2)
                for (int x = 0; x < t.width; x += 2)
                {
                    Color c = t.GetPixel(x, y);
                    if (c.a < 0.2f) continue;
                    seen++;
                    if (Mathf.Abs(c.r - c.g) < 0.02f && Mathf.Abs(c.g - c.b) < 0.02f) gray++;
                }
            Assert.Greater(seen, 20, "걸러진 그림에 보이는 화소가 거의 없다");
            Assert.AreEqual(seen, gray, "grayscale(1) 인데 색이 남은 화소가 있다 — 건 것은 알파뿐이라는 뜻이다");

            h.Gear.Set(slot, keep);
            ForgeSheet.Render(h);
            yield return null;
        }

        /// <summary>대장간 시트에서 «빈 칸» 으로 그려진 칸의 아이콘 Image — 이름이 `cell-<슬롯>` 인 칸 안의 `img`.</summary>
        static Image FindEmptyCellIcon(string slot)
        {
            foreach (Image img in UiRoot.Instance.GetComponentsInChildren<Image>(true))
            {
                if (!string.Equals(img.name, "img", StringComparison.Ordinal)) continue;
                Transform p = img.transform.parent;
                if (p != null && string.Equals(p.name, "cell-" + slot, StringComparison.Ordinal)) return img;
            }
            return null;
        }

        [SetUp] public void Up() { UiFilter.Reset(); root = new GameObject("t342"); }
        [TearDown] public void Down() { if (root != null) UnityEngine.Object.Destroy(root); UiFilter.Reset(); }

        /// <summary>읽을 수 있는 색판 스프라이트 하나(원본 화소를 우리가 안다).</summary>
        static Sprite Solid(Color c, int n = 8)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color[n * n];
            for (int i = 0; i < px.Length; i++) px[i] = c;
            t.SetPixels(px); t.Apply(false, false);
            return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        [Test]
        public void 표가_실제로_실린다()
        {
            Assert.IsTrue(UiFilter.Table.Has("equip_cell_empty"), "Resources/FilterUi.json 이 안 실렸다(.meta 누락이면 조용히 null 이다)");
            Assert.IsTrue(UiFilter.Table.Has("hatch_cone"));
        }

        [Test]
        public void 가_빈_장비_칸은_회색으로_눕고_밝아지고_알파가_52_다()
        {
            // 정본이 이 자리에 거는 바탕색과 같은 계열(마룬)로 넣어 본다
            Color src = new Color(0x6b / 255f, 0x35 / 255f, 0x38 / 255f, 1f);
            var img = new GameObject("img", typeof(RectTransform)).AddComponent<Image>();
            img.transform.SetParent(root.transform, false);
            img.sprite = Solid(src);
            img.color = Color.white;

            UiFilter.ApplyColor(img, "equip_cell_empty");

            Assert.AreEqual(0.52f, img.color.a, 1e-4f, "opacity(.52) 는 합성 알파로 간다");
            Color got = img.sprite.texture.GetPixel(4, 4);
            Assert.AreEqual(got.r, got.g, 1e-3f, "grayscale(1) — R=G");
            Assert.AreEqual(got.g, got.b, 1e-3f, "grayscale(1) — G=B");

            double r = src.r, g = src.g, b = src.b;
            FilterRules.Apply(UiFilter.Table.Get("equip_cell_empty"), ref r, ref g, ref b);
            Assert.AreEqual((float)r, got.r, 2e-2f, "Core 셈과 구운 화소가 같다");

            float before = (src.r + src.g + src.b) / 3f;
            Assert.Greater(got.r, before, "brightness(1.75) — 마룬보다 밝아야 «어두운 면 위 밝은 실루엣» 이 된다");
            Assert.AreEqual(1f, got.a, 1e-3f, "알파는 화소가 아니라 틴트로 준다(겹쳐 곱하지 않는다)");
        }

        [Test]
        public void 가_원본_스프라이트를_안_망친다()
        {
            var img = new GameObject("img", typeof(RectTransform)).AddComponent<Image>();
            img.transform.SetParent(root.transform, false);
            Sprite src = Solid(new Color(0.42f, 0.21f, 0.22f));
            img.sprite = src;
            UiFilter.ApplyColor(img, "equip_cell_empty");
            Assert.AreNotSame(src, img.sprite, "새로 구운 것을 끼운다");
            Color keep = src.texture.GetPixel(4, 4);
            Assert.AreEqual(0.42f, keep.r, 1e-2f, "원본 텍스처는 그대로다(다른 자리가 같은 아틀라스를 쓴다)");
        }

        [Test]
        public void 라_번짐은_가장자리를_경사로_바꾸고_판을_넓힌다()
        {
            // 반만 채운 판 — 번지면 경계에 중간값이 생긴다
            int n = 16;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    px[y * n + x] = x < n / 2 ? Color.white : new Color(1, 1, 1, 0);
            t.SetPixels(px); t.Apply(false, false);
            Sprite sharp = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);

            Sprite soft = UiFilter.Blur(sharp, 1.5, "t342-test");
            Assert.AreNotSame(sharp, soft);
            int rad = FilterRules.KernelRadius(1.5);
            Assert.AreEqual(n + 2 * rad, Mathf.RoundToInt(soft.textureRect.width), "커널 반경만큼 판을 넓혀야 번짐이 안 잘린다");

            // 경계(원본 x = n/2)는 넓힌 판에서 rad 만큼 밀린다 — 그 자리 알파가 «중간» 이어야 한다
            float a = soft.texture.GetPixel(n / 2 + rad, n / 2 + rad).a;
            Assert.Greater(a, 0.15f, "경계가 완전 투명이면 안 번진 것이다");
            Assert.Less(a, 0.85f, "경계가 꽉 차 있으면 안 번진 것이다");

            // 원본에서 완전히 투명하던 바깥쪽에도 조금 번져 나가야 한다
            float outer = soft.texture.GetPixel(n / 2 + rad + 2, n / 2 + rad).a;
            Assert.Greater(outer, 0.0f, "바깥으로 번져 나간다");
            Assert.Less(outer, a, "멀수록 옅다");
        }

        [Test]
        public void 라_σ_0_이면_원본을_그대로_돌려준다()
        {
            Sprite s = Solid(Color.white);
            Assert.AreSame(s, UiFilter.Blur(s, 0, "zero"));
        }

        [Test]
        public void 읽기_불가_아틀라스에도_콘솔_빨강을_안_남긴다()
        {
            // 런 494 — 이 한 뿌리가 PlayMode 193 개를 빨갛게 만들었다.
            // `GetPixels` 는 읽기 불가 텍스처에서 **던지기 전에 유니티가 콘솔 빨강을 먼저 찍는다** — try/catch 로는 못 막는다(§1 «플레이 콘솔 에러 0»).
            // 그러니 `isReadable` 로 미리 갈라 GPU 로 베껴 읽어야 한다.
            var rt = new RenderTexture(8, 8, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.Create();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, new Color(0x6b / 255f, 0x35 / 255f, 0x38 / 255f, 1f));
            RenderTexture.active = prev;

            // 읽기 불가 텍스처를 만든다(Apply(false, **false**) = CPU 사본을 버린다 — 아이콘 아틀라스와 같은 꼴)
            var hard = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var fill = new Color[64];
            for (int i = 0; i < fill.Length; i++) fill[i] = new Color(0x6b / 255f, 0x35 / 255f, 0x38 / 255f, 1f);
            hard.SetPixels(fill);
            hard.Apply(false, true);                      // makeNoLongerReadable: true
            Assert.IsFalse(hard.isReadable, "이 칸이 서려면 텍스처가 정말 읽기 불가여야 한다");

            var img = new GameObject("img", typeof(RectTransform)).AddComponent<Image>();
            img.transform.SetParent(root.transform, false);
            img.sprite = Sprite.Create(hard, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 100f);

            UiFilter.ApplyColor(img, "equip_cell_empty");   // 콘솔 빨강이 나면 이 자가 넘어진다(RedLog · T46)

            Assert.AreEqual(0.52f, img.color.a, 1e-4f, "못 읽더라도 opacity 는 건다");
            rt.Release();
            UnityEngine.Object.Destroy(rt);
        }

        [UnityTest]
        public IEnumerator 라_부화_원뿔은_이름이_맞을_때만_번진다()
        {
            var host = new GameObject("host", typeof(RectTransform));
            host.transform.SetParent(root.transform, false);

            PetHatchCone cone = PetHatchCone.Add(host.transform, PetHatchCone.BlurObjectName, Color.white, Color.white, 0.24f, PetHatchCone.Kind.Cone);
            cone.rectTransform.sizeDelta = new Vector2(120f, 180f);
            PetHatchCone other = PetHatchCone.Add(host.transform, "notch", Color.white, Color.white, 1f, PetHatchCone.Kind.Cone);
            other.rectTransform.sizeDelta = new Vector2(120f, 180f);
            yield return null;

            StringAssert.StartsWith("blur-", cone.sprite.name, "정본 선택자와 같은 이름(.hatch-cone)이면 번진다");
            StringAssert.DoesNotStartWith("blur-", other.sprite.name, "«장착됨» 라벨 홈은 정본이 그 선언을 안 건 자리다 — 번지면 안 된다");
        }
    }
}
