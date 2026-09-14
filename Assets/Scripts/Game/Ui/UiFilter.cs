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
                if (src != null) img.sprite = BakeFiltered(src, f);
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
        public static Sprite Blur(Sprite src, double sigmaPx, string keySuffix)
        {
            if (src == null || sigmaPx <= 0) return src;
            string key = src.GetInstanceID() + "|blur|" + keySuffix;
            Sprite got;
            if (baked.TryGetValue(key, out got) && got != null) return got;

            Rect r = src.textureRect;
            int w0 = Mathf.Max(1, Mathf.RoundToInt(r.width)), h0 = Mathf.Max(1, Mathf.RoundToInt(r.height));
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
            if (tex.isReadable)
            {
                if (x < 0 || y < 0 || x + w > tex.width || y + h > tex.height) return null;
                return tex.GetPixels(x, y, w, h);
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
                if (x < 0 || y < 0 || x + w > tex.width || y + h > tex.height) return null;
                tmp.ReadPixels(new Rect(x, y, w, h), 0, 0, false);
                tmp.Apply(false, false);
                return tmp.GetPixels();
            }
            finally
            {
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                if (tmp != null) UnityEngine.Object.Destroy(tmp);
            }
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
