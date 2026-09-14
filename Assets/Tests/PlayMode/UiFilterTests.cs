using System;
using System.Collections;
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
