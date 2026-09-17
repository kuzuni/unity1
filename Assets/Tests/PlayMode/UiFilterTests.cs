using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Forge.Game;
using Forge.Game.Gallery;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using Forge.Core.Data;
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
                // T349 4회차 — 이 카메라는 `cullingMask` 가 **UI 층 하나**다(3D 를 한 화소도 안 그린다).
                //        여기에 URP 포스트를 켜면 **UI 가 톤맵·색 보정에 물든다** — 정본은 `filter` 를 `#game3d` 에만 걸고
                //        HUD·패널·팝업에는 안 건다(T357). 3회차에 켰다가 순백 코어가 rgb 215 로 내려가 이 자가 빨갰다.
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
        public void 다_연구_잠금_노드는_반쯤_회색으로_눕고_알파는_안_건드린다()
        {
            Color src = new Color(0.80f, 0.20f, 0.20f, 1f);
            var img = new GameObject("ico", typeof(RectTransform)).AddComponent<Image>();
            img.transform.SetParent(root.transform, false);
            img.sprite = Solid(src);
            img.color = Color.white;

            UiFilter.ApplyColor(img, "tech_node_locked");

            Color got = img.sprite.texture.GetPixel(4, 4);
            Assert.Greater(got.r, got.g, "grayscale(.55) — 다 눕지 않는다(색 방향은 남는다)");
            Assert.Less(got.r - got.g, src.r - src.g, "그래도 원본보다는 좁다");
            Assert.AreEqual(1f, img.color.a, 1e-4f, "이 자리엔 opacity 가 없다 — 판의 옅어짐은 노드가 쥔다");

            double r = src.r, g = src.g, b = src.b;
            FilterRules.Apply(UiFilter.Table.Get("tech_node_locked"), ref r, ref g, ref b);
            Assert.AreEqual((float)r, got.r, 2e-2f, "Core 셈과 구운 화소가 같다");
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
    
        /// <summary>T342 ⓑ(5회차) — 빈 탈것 칸의 말 실루엣: 정본 857 `brightness(0) opacity(.32)` → 구운 화소가 전부 검정이고 틴트 알파가 .32 다(새 세이브는 탈것이 0 이라 칸이 비어 있다).</summary>
        [UnityTest]
        public IEnumerator 빈_탈것_칸의_실루엣은_새까맣게_굽고_알파_32_다()
        {
            yield return Boot();
            Image sil = null;
            foreach (Image im in UiRoot.Instance.Sheet.GetComponentsInChildren<Image>(true))
                if (im.name == "mount-sil" && im.transform.parent != null && im.transform.parent.name == "egg-cell") { sil = im; break; }
            Assert.IsNotNull(sil, "빈 탈것 칸(egg-cell)의 실루엣 mount-sil 이 없다");
            Assert.IsTrue(UiFilter.Table.Has("mount_slot_empty"), "FilterUi.json 에 mount_slot_empty 가 있다");
            Assert.AreEqual(0.32f, sil.color.a, 1e-3f, "정본 opacity(.32) 는 틴트 알파");
            Assert.IsNotNull(sil.sprite);
            StringAssert.StartsWith("filt-mount_slot_empty", sil.sprite.name, "실물 아이콘이 brightness(0) 으로 구워졌다");
            Texture2D t = sil.sprite.texture;
            int seen = 0, black = 0;
            for (int y = 0; y < t.height; y += 2)
                for (int x = 0; x < t.width; x += 2)
                {
                    Color c = t.GetPixel(x, y);
                    if (c.a < 0.2f) continue;
                    seen++;
                    if (c.r < 0.02f && c.g < 0.02f && c.b < 0.02f) black++;
                }
            Assert.Greater(seen, 20, "구운 그림에 보이는 화소가 거의 없다");
            Assert.AreEqual(seen, black, "brightness(0) 인데 검정이 아닌 화소가 있다");
        }

        /// <summary>T342 6회차 — 플레이어 정보 팝업의 빈 장비 칸도 같은 `equipCellHTML`(ui.js 3096) 이라 정본 862 가 걸린다 · 빈 탈것 칸은 정본 5166 이
        /// 실루엣 없이 «탈것» 글자뿐이다(장비 시트 1535 와 다르다). 칸 하나를 비우고 팝업을 열어 실물 아이콘이 걸러졌는지 + 실루엣이 없는지를 본다.</summary>
        [UnityTest]
        public IEnumerator 플레이어_정보의_빈_장비_칸은_걸러지고_빈_탈것_칸엔_실루엣이_없다()
        {
            yield return Boot();
            ForgeHost fh = ForgeHost.Instance;
            MetaHost h = MetaHost.Instance;
            string slot = fh.Defs.Slots[0];
            var keep = fh.Gear.Get(slot);
            fh.Gear.Set(slot, null);
            PlayerInfoPopup.Open(h);
            yield return null; yield return null;
            Popup p = PopupLayer.Instance.Find(PlayerInfoPopup.Name);
            Assert.IsNotNull(p, "플레이어 정보 팝업이 열린다");

            Image cell = null; bool sil = false;
            foreach (Image img in p.Root.GetComponentsInChildren<Image>(true))
            {
                Transform par = img.transform.parent;
                if (img.name == "img" && par != null && par.name == "slot-" + slot) cell = img;
                if (img.name == "mount-sil") sil = true;
            }
            Assert.IsNotNull(cell, "빈 칸 slot-" + slot + " 의 아이콘 Image 를 못 찾았다 — 칸을 비웠는데 «빈 칸» 으로 안 그려졌다");
            Assert.AreEqual(0.52f, cell.color.a, 1e-3f, "정본 opacity(.52) 는 틴트 알파");
            Assert.IsNotNull(cell.sprite, "빈 칸 아이콘에 스프라이트가 없다");
            StringAssert.StartsWith("filt-equip_cell_empty", cell.sprite.name, "실물 아이콘이 grayscale(1) brightness(1.75) 로 구워졌다");
            Assert.IsFalse(sil, "정본 5166 — 플레이어 정보의 빈 탈것 칸엔 mount-sil 이 없다(원작에 없는 것을 그리지 않는다)");

            PlayerInfoPopup.Close(h);
            fh.Gear.Set(slot, keep);
            yield return null;
        }

        /// <summary>T451 2회차 — 구체 본체(`sr-orbwrap/sr-orb`)의 색을 선 차례대로 모은다(두 시점을 같은 눈으로 재려고 뗐다).</summary>
        private static List<Color> Orbs(SkillSummonResultView v)
        {
            List<Color> outp = new List<Color>();
            foreach (Image img in v.GetComponentsInChildren<Image>(true))
                if (img.name == "sr-orb" && img.transform.parent != null && img.transform.parent.name == "sr-orbwrap") outp.Add(img.color);
            return outp;
        }

        /// <summary>
        /// T451 1회차 — 관측한 구슬 색이 **무엇과 가장 가까운가**를 이름으로 돌려준다(자국 전용 · 단언은 이것을 안 쓴다).
        ///
        /// 후보는 «있을 수 있는 정적 조합» 전부다: 등급 × tier 의 `OrbFilter(rc, t)`(팔레트가 어긋났거나 tier 를 잘못 집은 갈래) ·
        /// 같은 셀의 **다른 두 겹**(`sr-orb-deep` = 등급색을 `sr_orb_deep` 쪽으로 .62 섞은 것 · `sr-hilite`) ·
        /// 필터를 **안 건** 날것(rc) · **두 번 건** 것 · 팔레트 폴백(`muted`). 셋 다 아니면 그 말(«어느 정적 조합과도 안 맞는다»)이 곧 답이다 —
        /// 그때는 색이 «지어지는 중이거나 지나가는 값» 이라는 뜻이고, 고칠 자리는 팔레트가 아니라 **읽는 시점**이다(T442 2회차가 셈으로 좁힌 그 결론).
        /// </summary>
        private static string NearestName(GameDefs defs, Color c)
        {
            string best = null;
            float bestD = float.MaxValue;
            void Try(string what, Color k)
            {
                float d = Mathf.Abs(c.r - k.r) + Mathf.Abs(c.g - k.g) + Mathf.Abs(c.b - k.b);
                if (d < bestD) { bestD = d; best = what; }
            }
            for (int t = 0; t < defs.Rarities.Length; t++)
            {
                string r = defs.Rarities[t];
                Color rc = PetSkillStyle.Rarity(defs, r);
                for (int f = 0; f < defs.Rarities.Length; f++)
                {
                    Try(r + "×orb_" + f, SkillSummonResultView.OrbFilter(rc, f));
                    Try(r + "×orb_" + f + "×2", SkillSummonResultView.OrbFilter(SkillSummonResultView.OrbFilter(rc, f), f));
                }
                Try(r + " 날것(필터 안 걺)", rc);
                Try(r + " deep겹×orb_" + t, SkillSummonResultView.OrbFilter(Color.Lerp(rc, PetSkillStyle.C("sr_orb_deep"), 0.62f), t));
                Try(r + " hilite겹×orb_" + t, SkillSummonResultView.OrbFilter(PetSkillStyle.C("sr_hilite"), t));
            }
            Try("muted 폴백", PetSkillStyle.C("muted"));
            Try("muted 폴백×orb_0", SkillSummonResultView.OrbFilter(PetSkillStyle.C("muted"), 0));
            return string.Format("{0}(빗나감 {1:F4}{2})", best, bestD, bestD < 4.5f / 255f ? "" : " — **어느 정적 조합과도 안 맞는다 = 지어지는 중인 값**");
        }

        /// <summary>T342 7회차 ⓔ — 정본 6656~6665 `.sr-cell[data-tier=N] .sr-orb { filter: saturate(·) brightness(·) }`:
        /// 소환 결과의 구체 본체 색이 표 `summon_orb_N` 의 filter 를 거친 등급색이다(종전 «tier ≤ 1 검정 30%» 근사가 아니다).</summary>
        [UnityTest]
        public IEnumerator 마_소환_구슬은_등급마다_표의_filter_를_거친_색이다()
        {
            yield return Boot();
            float t = 0f;
            while (!(SkillPetSheet.Instance != null && PetSkillHost.Ready) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다");
            var defs = PetSkillHost.Instance.Data.Defs;
            var list = new List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = "가" },
                new SkillSummonResultView.Entry { Key = "sk:b", IconKey = "sk_fireball", Rarity = "mythic", Name = "나" },
            };
            SkillSummonResultView v = SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", list, "mythic", null);
            Assert.IsNotNull(v, "결과 연출 팝업이 서지 않았다");
            yield return null; yield return null;

            // T451 2회차 — **두 시점을 다 잰다**. 코드를 읽으면 구체 색은 세울 때 한 번 정해지고(`SkillSummonResult.cs:946`
            //   `Disc(wrap, "sr-orb", OrbFilter(rc, tier))`) 그 뒤 아무도 `sr-orb` 의 `color` 를 다시 안 쓴다 — `Finish()` 가 다시 칠하는 것은
            //   소환진(`floorImg`)·선(`tickImg`) 이고, 뒤에 붙는 겹들(`sr-ghost`·`sr-spark`…)은 **다른 이름의 형제**다.
            //   그런데 이 자는 같은 코드로 런마다 갈렸고(1017 빨 · 1021 초 · 1027 빨 · 1040 초 · 1042 초 · 1073 빨),
            //   관측값(0.340 → 0.398 · 기대 0.562)은 **어떤 바이트 색 × 어떤 tier 필터와도 안 맞는다**(0.398 을 .64·.74·.9·1.0 으로 나눠도
            //   정수 바이트가 하나도 안 나온다). 그러면 남는 갈래는 «색이 변한다» 가 아니라 «**내가 그 순간 무엇을 읽었나**» 뿐이다.
            //   ⇒ 두 프레임 뒤(`early`)와 **연출이 가라앉은 뒤**(`late`)를 나란히 재어 **자가 스스로 가르게** 한다:
            //     · 둘이 같은데 틀리다 → 세울 때 이미 틀린 것(팔레트·tier 갈래) — `NearestName` 이 무엇과 가까운지 찍는다.
            //     · 둘이 다르다 → 세운 뒤 누가 덮었거나 내가 «지어지는 중» 을 읽은 것(그 차이가 곧 증거다).
            //   단언은 **가라앉은 뒤**로 건다 — 그것이 사람이 실제로 보는 색이고(§1 «자는 게임을 잰다»), 잣대(허용 오차 1.5/255)는 그대로다.
            List<Color> early = Orbs(v);
            Assert.AreEqual(2, early.Count, "구체 둘(sr-orbwrap/sr-orb)");
            float settle = 0f;
            while (!v.Done && settle < 12f) { settle += Time.unscaledDeltaTime; yield return null; }
            yield return null;
            List<Color> orbs = Orbs(v);
            Assert.AreEqual(2, orbs.Count, "가라앉은 뒤에도 구체 둘(sr-orbwrap/sr-orb)");
            string shift = "같다";
            for (int i = 0; i < orbs.Count && i < early.Count; i++)
                if (Mathf.Abs(orbs[i].r - early[i].r) + Mathf.Abs(orbs[i].g - early[i].g) + Mathf.Abs(orbs[i].b - early[i].b) > 1f / 255f)
                    shift = "**다르다** — 두 프레임 뒤 " + string.Join(" / ", early.ConvertAll(c => c.ToString()).ToArray());
            foreach (var e in list)
            {
                int tier = Array.IndexOf(defs.Rarities, e.Rarity);
                Assert.GreaterOrEqual(tier, 0, e.Rarity + " 등급 번호");
                Color rc = PetSkillStyle.Rarity(defs, e.Rarity);
                Color want = SkillSummonResultView.OrbFilter(rc, tier);
                Color oldApprox = tier <= 1 ? Color.Lerp(rc, Color.black, 0.3f) : rc;
                bool found = false;
                foreach (Color c in orbs)
                    if (Mathf.Abs(c.r - want.r) < 1.5f / 255f && Mathf.Abs(c.g - want.g) < 1.5f / 255f && Mathf.Abs(c.b - want.b) < 1.5f / 255f) found = true;
                // T442 1회차 — 이 자는 **런마다 갈린다**(1017 빨강 · 1021 초록 · 1027 빨강). 그런데 자국이 «기대 ↔ 실물» 두 수뿐이라
                //   회차마다 워커가 그 둘로 셈을 되짚다 끝난다(1회차가 그렇게 반나절을 썼다). 실측으로 좁힌 것:
                //   ⓐ mythic 관측값은 정본 #aa1cff 에 saturate 1.08 을 건 값과 맞는다 ⇒ 표·FilterRules·거는 자리는 멀쩡하다.
                //   ⓑ 회색에 saturate 는 항등이라 common 관측값은 `rc × brightness` 여야 하는데, **어떤 바이트 색에 .64 를 곱해도 0.340 이 안 나온다**
                //      (135 → 0.339 · 136 → 0.341). 곧 «팔레트 색이 틀렸다» 도 «필터를 두 번 걸었다»(그러면 0.360)도 아니다.
                //   ⇒ 남은 것은 «이 자리가 실제로 무엇을 먹었나» 인데 그것이 자국에 없다. 그래서 **자국이 스스로 답하게** 한다:
                //      정본 hex · 그것을 판 rc · 쓴 필터 값 · 관측/rc 채널 비(比)를 함께 찍는다. 비가 곧 «무엇이 곱해졌나» 다.
                FilterSpec fs = UiFilter.Table.Get("summon_orb_" + tier);
                // T451 1회차 — 자국에 아직 **한 칸이 비어 있었다**: «관측 색 둘» 은 적히는데 **그 둘이 어느 칸의 구슬인지**,
                //   그리고 **그 값이 어떤 정적 조합과 맞는지**가 없다. 그래서 회차마다 워커가 관측값 하나를 손으로 되짚다 끝난다
                //   (T442 가 그렇게 여러 회차를 썼고 이 회차의 관측값 0.398 도 지난번 0.340 과 달라 «지어지는 중» 말고는 말이 안 된다).
                //   ⇒ 자가 **스스로 맞춰 보게** 한다: 관측 색마다 «모든 등급 × 모든 tier 의 OrbFilter» 와 구슬 세 겹(본체·deep·hilite)을
                //      전부 재어 **가장 가까운 것**을 이름으로 찍는다. 다음 빨강 한 번이면 «팔레트가 어긋났나 · 겹을 잘못 집었나 ·
                //      어느 것과도 안 맞나(= 지어지는 중)» 가 글로 나온다. 잣대는 한 글자도 안 바꿨다(결정 748).
                string near = string.Join(" ‖ ", orbs.ConvertAll(c => NearestName(defs, c)).ToArray());
                string ratios = string.Join(" / ", orbs.ConvertAll(c => string.Format("({0:F3},{1:F3},{2:F3})",
                    rc.r > 0.001f ? c.r / rc.r : -1f, rc.g > 0.001f ? c.g / rc.g : -1f, rc.b > 0.001f ? c.b / rc.b : -1f)).ToArray());
                string diag = string.Format(" | 가장 가까운 것: {5} | 두 시점 {6}(가라앉기까지 {7:F2}초 · Done {8}) | 정본hex {0} · rc {1} · 표 sat {2} bri {3} · 관측/rc {4}",
                    PetSkillStyle.RarityHex(defs, e.Rarity) ?? "(없다 — muted 폴백)", rc,
                    fs.HasSaturate ? fs.Saturate.ToString("F3") : "-", fs.HasBrightness ? fs.Brightness.ToString("F3") : "-", ratios, near, shift, settle, v.Done);
                Assert.IsTrue(found, e.Rarity + "(tier " + tier + ") 구체 색 = 등급색에 표 summon_orb_" + tier + " 를 건 값 " + want + " 이어야 한다 — 실물 " + string.Join(" / ", orbs.ConvertAll(c => c.ToString()).ToArray()) + diag);
                if (tier == 0)
                {
                    Assert.AreNotEqual(oldApprox, want, "tier 0 은 정본 filter(saturate .62 · brightness .64)가 종전 근사(검정 30%)와 다른 값이다");
                    Assert.Less(want.r + want.g + want.b, rc.r + rc.g + rc.b, "tier 0 은 등급색보다 어둡다");
                }
                if (tier == 5) Assert.AreEqual(rc.r * 0.213f + rc.g * 0.715f + rc.b * 0.072f, want.r * 0.213f + want.g * 0.715f + want.b * 0.072f, 2f / 255f, "tier 5 는 채도만 올리고 명부는 그대로");
            }
            yield return null;
        }
    }
}
