using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 정본 CSS `filter` 를 화면에 거는 자리 — 표는 `Resources/FilterUi.json`, 셈은 <see cref="FilterRules"/>(Core) 가 쥔다.
    ///
    /// ⚠ `Image.color` 로는 `grayscale`·`brightness` 를 못 한다 — 틴트는 **곱하기**라 흰색은 원본 그대로고 어둡게만 갈 수 있다.
    ///    그래서 원본 스프라이트의 화소를 읽어 **걸러진 스프라이트를 구워** 끼운다(굽기 결과는 키로 캐시한다).
    ///    `opacity` 만은 합성 알파라 <see cref="Image.color"/> 의 a 로 준다(정본도 합성 단계다).
    ///
    /// 도우미를 `ForgeUi`·`UiKit` 에 안 넣고 새 파일로 낸 까닭: 두 파일 다 남의 산 lock 이다(T332 · T178·T331·T333·T335).
    /// </summary>
    public static class UiFilter
    {
        public const string ResourcePath = "FilterUi";

        static FilterTable table;
        static readonly Dictionary<string, Sprite> baked = new Dictionary<string, Sprite>(StringComparer.Ordinal);

        /// <summary>표 — 처음 부를 때 한 번 읽는다.</summary>
        public static FilterTable Table
        {
            get
            {
                if (table == null)
                {
                    var ta = Resources.Load<TextAsset>(ResourcePath);
                    if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 을 못 읽었다(.meta 가 없으면 유니티가 안 싣는다)");
                    table = FilterTable.From(MiniJson.ParseObject(ta.text));
                }
                return table;
            }
        }

        /// <summary>테스트가 표를 다시 읽게 한다(구운 것도 버린다).</summary>
        public static void Reset() { table = null; baked.Clear(); }

        /// <summary>
        /// <see cref="Blur"/> 가 낸 판의 키움 비(가로·세로 · «판 한 변 ÷ 줄인 원본 한 변» = 1 + 2×커널 반경/원본). 번지지 않은 판(원본 그대로)이면 (1, 1).
        /// 그림자를 원본과 **같은 상자**에 넣으면 실루엣이 이 비만큼 줄고 번짐이 상자 끝에서 잘린다 — 정본 `drop-shadow` 는 요소 상자 밖으로 번진다(T411 3회차).
        /// 사전에 기억하지 않고 **셈으로** 낸다(4회차 · 런 875): 원본·화면 한 변에서 <see cref="Blur"/> 가 줄인 원본 한 변을 같은 식으로 다시 세면 되고,
        /// 그래야 캐시가 비워지거나 씬이 다시 실려도 답이 같다.
        /// </summary>
        public static Vector2 BlurGrow(Sprite src, Sprite bakedSprite, double displayPx)
        {
            if (src == null || bakedSprite == null || bakedSprite == src) return Vector2.one;
            Rect r = src.textureRect;
            int w0 = Mathf.Max(1, Mathf.RoundToInt(r.width)), h0 = Mathf.Max(1, Mathf.RoundToInt(r.height));
            int side = FilterRules.BlurBakeSide(Mathf.Max(w0, h0), displayPx);
            if (side > 0 && side < Mathf.Max(w0, h0))
            {
                double shrink = (double)side / Mathf.Max(w0, h0);
                w0 = Mathf.Max(2, Mathf.RoundToInt(w0 * (float)shrink)); h0 = Mathf.Max(2, Mathf.RoundToInt(h0 * (float)shrink));
            }
            return new Vector2(bakedSprite.rect.width / w0, bakedSprite.rect.height / h0);
        }

        /// <summary>
        /// 색 filter(`grayscale`·`saturate`·`brightness`)를 스프라이트 화소에 구워 끼우고, `opacity` 는 틴트 알파로 준다.
        /// 표에 색 함수가 없으면 굽지 않는다(원본 그대로 두고 알파만 건다).
        /// </summary>
        public static void ApplyColor(Image img, string siteKey)
        {
            if (img == null) return;
            FilterSpec f = Table.Get(siteKey);
            if (f.HasGrayscale || f.HasSaturate || f.HasBrightness)
            {
                Sprite src = img.sprite;
                // §0-6 수리(런 501) — 굽기는 **부팅 길** 위에 있다(`ForgeHost.Boot` → `ForgeSheet.EquipCell`).
                // 여기서 던지면 화면이 통째로 안 선다(그 런에서 PlayMode 244개 중 198개가 그렇게 넘어졌다).
                // 그래서 무슨 일이 있어도 원본을 두고 지나간다 — 경고 한 줄만 남긴다(빨강이 아니라 §1 «콘솔 에러 0» 을 안 깬다).
                if (src != null)
                {
                    try { img.sprite = BakeFiltered(src, f); }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning("UiFilter: 색 filter 를 못 구웠다 — 원본 그대로 둔다 (" + siteKey + ") " + e.Message);
                    }
                }
            }
            Color c = img.color;
            img.color = new Color(c.r, c.g, c.b, f.HasOpacity ? (float)f.Opacity : c.a);
        }

        /// <summary>원본 스프라이트 화소에 색 filter 를 걸어 구운 새 스프라이트(같은 원본·같은 자리면 한 번만 굽는다).</summary>
        public static Sprite BakeFiltered(Sprite src, FilterSpec f)
        {
            string key = src.GetInstanceID() + "|" + f.Key;
            Sprite got;
            if (baked.TryGetValue(key, out got) && got != null) return got;

            Rect r = src.textureRect;
            int w = Mathf.Max(1, Mathf.RoundToInt(r.width)), h = Mathf.Max(1, Mathf.RoundToInt(r.height));
            Color[] px = ReadPixels(src.texture, Mathf.RoundToInt(r.x), Mathf.RoundToInt(r.y), w, h);
            if (px == null) return src;
            for (int i = 0; i < px.Length; i++)
            {
                Color p = px[i];
                double cr = p.r, cg = p.g, cb = p.b;
                FilterRules.Apply(f, ref cr, ref cg, ref cb);
                px[i] = new Color((float)cr, (float)cg, (float)cb, p.a);
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "filt-" + f.Key, filterMode = src.texture.filterMode, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(px);
            tex.Apply(false, false);                                   // CPU 사본을 남긴다 — PlayMode 가 화소를 되읽는다
            var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), src.pixelsPerUnit);
            sp.name = "filt-" + f.Key + "-" + src.name;
            baked[key] = sp;
            return sp;
        }

        /// <summary>
        /// 이미 구운 스프라이트의 알파에 가우시안 번짐(`blur`)을 건다 — 색면 도형(<see cref="PetHatchCone"/>)의 가장자리용.
        /// σ 는 **구운 화소** 단위다: 정본이 말한 CSS px 를 <see cref="FilterRules.BakeSigmaPx"/> 로 옮겨 넘긴다.
        /// 번짐이 잘리지 않게 테두리를 커널 반경만큼 넓혀 굽는다.
        /// </summary>
        public static Sprite Blur(Sprite src, double sigmaPx, string keySuffix) { return Blur(src, sigmaPx, keySuffix, 0); }

        /// <param name="displayPx">화면에 설 한 변(화소). 주면 **그 크기까지만 굽는다** — 번짐은 해상도를 올려도 볼 것이 없고
        /// 커널이 제곱으로 비싸진다(<see cref="FilterRules.BlurBakeSide"/> 의 실측 5.1M → 0.8M · 2.6MB → 0.7MB). 0 이면 원본 해상도.</param>
        public static Sprite Blur(Sprite src, double sigmaPx, string keySuffix, double displayPx)
        {
            if (src == null || sigmaPx <= 0) return src;
            string key = src.GetInstanceID() + "|blur|" + keySuffix;
            Sprite got;
            if (baked.TryGetValue(key, out got) && got != null) return got;

            Rect r = src.textureRect;
            int w0 = Mathf.Max(1, Mathf.RoundToInt(r.width)), h0 = Mathf.Max(1, Mathf.RoundToInt(r.height));

            // 화면보다 크게 구워 둔 도형이면 화면 크기로 내려서 번진다 — σ 도 같은 비율로 줄인다(번짐 모양은 그대로).
            int side = FilterRules.BlurBakeSide(Mathf.Max(w0, h0), displayPx);
            if (side > 0 && side < Mathf.Max(w0, h0))
            {
                double shrink = (double)side / Mathf.Max(w0, h0);
                int dw = Mathf.Max(2, Mathf.RoundToInt(w0 * (float)shrink)), dh = Mathf.Max(2, Mathf.RoundToInt(h0 * (float)shrink));
                Sprite small = Resample(src, dw, dh, keySuffix);
                if (small != null) { src = small; r = src.textureRect; w0 = dw; h0 = dh; sigmaPx *= shrink; }
            }

            Color[] src0 = ReadPixels(src.texture, Mathf.RoundToInt(r.x), Mathf.RoundToInt(r.y), w0, h0);
            if (src0 == null) return src;

            double[] k = FilterRules.GaussianKernel(sigmaPx);
            int rad = (k.Length - 1) / 2;
            int w = w0 + 2 * rad, h = h0 + 2 * rad;

            // 테두리를 투명으로 넓힌 판에 옮겨 담는다(번짐이 판 밖으로 안 잘리게)
            var a = new Color[w * h];
            for (int y = 0; y < h0; y++)
                for (int x = 0; x < w0; x++)
                    a[(y + rad) * w + (x + rad)] = src0[y * w0 + x];

            var b = new Color[w * h];
            // 가로 → 세로(분리 가능 커널). 색은 프리멀티플라이드로 섞어야 가장자리에 검은 테가 안 생긴다.
            BlurAxis(a, b, w, h, k, rad, true);
            BlurAxis(b, a, w, h, k, rad, false);

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "blur-" + keySuffix, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(a);
            tex.Apply(false, false);
            var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), src.pixelsPerUnit);
            sp.name = "blur-" + keySuffix + "-" + src.name;
            baked[key] = sp;
            return sp;
        }

        /// <summary>
        /// 텍스처 한 조각의 화소를 읽는다 — **읽을 수 없는 텍스처(import 설정 Read/Write off)도 읽는다**.
        ///
        /// ⚠ 런 494 가 가르친 것: `Texture2D.GetPixels` 는 읽기 불가 텍스처에서 **던지기 전에 유니티가 콘솔에 빨간 줄을 먼저 찍는다**
        ///    («Texture … is not readable»). `try/catch` 로 감싸도 그 줄은 이미 남아 §1 «플레이 콘솔 에러 0» 이 깨진다 —
        ///    실제로 그 한 뿌리가 PlayMode 193 개를 빨갛게 만들었다. 그러니 **던지게 두지 말고 미리 가른다**.
        ///    아이콘 아틀라스(T31)처럼 읽기 불가인 것은 GPU 로 한 번 베껴(Blit → RenderTexture → ReadPixels) 읽는다 — 화소는 같다.
        /// </summary>
        /// <returns>못 읽으면 null(부르는 쪽이 원본을 그대로 쓴다) — 빨간 줄은 안 남긴다.</returns>
        static Color[] ReadPixels(Texture2D tex, int x, int y, int w, int h)
        {
            if (tex == null) return null;
            if (x < 0 || y < 0 || x + w > tex.width || y + h > tex.height) return null;
            if (tex.isReadable)
            {
                // §0-6 수리(런 501): `isReadable` 이 true 라도 **CPU 사본이 없을 수 있다** — 압축·크런치 판이거나
                // 업로드 뒤 버린 판이면 `GetPixels` 가 «texture data is either not readable, corrupted or does not exist»
                // 로 던진다. 런 501 의 아이콘 아틀라스 `atlas-0` 이 그랬고, 그 예외가 `ForgeSheet.EquipCell` → `ForgeHost.Boot`
                // 를 타고 올라가 **PlayMode 244개 중 198개**를 넘어뜨렸다. 여기서 받아 아래 GPU 베끼기로 내려간다.
                try { return tex.GetPixels(x, y, w, h); }
                catch (UnityEngine.UnityException) { }
                catch (System.ArgumentException) { }
            }

            // 읽기 불가 — GPU 로 베낀다. sRGB 판으로 읽어야 색이 안 어긋난다.
            RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture prev = RenderTexture.active;
            Texture2D tmp = null;
            try
            {
                Graphics.Blit(tex, rt);
                RenderTexture.active = rt;
                tmp = new Texture2D(w, h, TextureFormat.RGBA32, false);
                // ⚠ 좌표계가 다르다: `Sprite.textureRect`·`GetPixels` 는 **왼쪽 아래**가 원점인데
                //    `Texture2D.ReadPixels` 는 활성 RenderTexture 를 **왼쪽 위** 원점으로 읽는다.
                //    그대로 넘기면 아틀라스에서 **다른 아이콘을 잘라 온다**(맨 윗줄만 우연히 맞는다).
                tmp.ReadPixels(new Rect(x, tex.height - (y + h), w, h), 0, 0, false);
                tmp.Apply(false, false);
                Color[] got = tmp.GetPixels();
                // ReadPixels 가 위에서부터 채웠으므로 줄 차례를 뒤집어 `GetPixels` 와 같은 꼴로 돌려준다.
                var flipped = new Color[got.Length];
                for (int row = 0; row < h; row++) System.Array.Copy(got, (h - 1 - row) * w, flipped, row * w, w);
                return flipped;
            }
            finally
            {
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                if (tmp != null) UnityEngine.Object.Destroy(tmp);
            }
        }

        /// <summary>GPU 로 한 번 베껴 크기를 줄인다(쌍선형) — 번지기 전에만 쓴다.</summary>
        static Sprite Resample(Sprite src, int w, int h, string keySuffix)
        {
            string key = src.GetInstanceID() + "|small|" + w + "x" + h + "|" + keySuffix;
            Sprite got;
            if (baked.TryGetValue(key, out got) && got != null) return got;

            RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture prev = RenderTexture.active;
            try
            {
                rt.filterMode = FilterMode.Bilinear;
                Graphics.Blit(src.texture, rt);
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "small-" + keySuffix, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                tex.Apply(false, false);
                var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), src.pixelsPerUnit);
                sp.name = tex.name;
                baked[key] = sp;
                return sp;
            }
            finally { RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt); }
        }

        static void BlurAxis(Color[] src, Color[] dst, int w, int h, double[] k, int rad, bool horizontal)
        {
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    double pr = 0, pg = 0, pb = 0, pa = 0;
                    for (int i = -rad; i <= rad; i++)
                    {
                        int sx = horizontal ? x + i : x;
                        int sy = horizontal ? y : y + i;
                        if (sx < 0 || sx >= w || sy < 0 || sy >= h) continue;    // 판 밖은 투명 — 이미 넓혀 뒀다
                        Color c = src[sy * w + sx];
                        double wt = k[i + rad], al = c.a * wt;
                        pr += c.r * al; pg += c.g * al; pb += c.b * al; pa += al;
                    }
                    dst[y * w + x] = pa > 0.0000001
                        ? new Color((float)(pr / pa), (float)(pg / pa), (float)(pb / pa), (float)pa)
                        : new Color(0, 0, 0, 0);
                }
            }
        }
    }
}
