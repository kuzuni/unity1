using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 정본 SVG 의 **각진 폴리곤 면**(+ 선형 그라디언트)을 구워 스프라이트로 내주는 자 — T87. 외부 그림 0(`UiShapes` 와 같은 길: 코드로 만들어 한 번 굽고 공유한다).
    ///
    /// 왜 둥근 사각(<see cref="UiShapes.Rounded"/>)으로 안 되는가 — 정본 `ui.js` 의 빌릿 주석이 못 박았다:
    /// «🚨 모서리를 둥글리지 말 것. 처음엔 rx 2.4 라운드 바로 그렸는데 비평가 2인이 독립적으로 *달군 쇳덩이가 아니라 UI 배지·진행도 칩·젤리* 로 읽었다 —
    ///  네 모서리가 다 둥근 알약은 게임 UI 의 어휘다. 소재는 **모서리를 챔퍼로 깎은 각봉**이어야 한다.»
    ///
    /// ⚠ 6회차에는 이 자리를 `Graphic` 상속(정점 메시)으로 만들었다가 **화면에 한 픽셀도 안 나왔다**(런 173 샷 실측: 모루 상자 안 픽셀 변화 0 ·
    ///   테스트는 «칸이 있다·값이 맞다» 만 봐서 초록이었다). 그래서 이 레포에서 실제로 칠해지는 것이 확인된 길(구운 스프라이트 + <see cref="Image"/>)로 되돌린다.
    ///   보이는지는 이제 사람 눈이 아니라 **픽셀 단언**이 지킨다(`ForgeUiTests` 의 «빌릿이 화면에 칠해진다»).
    ///
    /// 좌표는 정본 viewBox 단위 그대로 주고(132×86), 굽는 자가 그 도형의 바깥 사각만큼만 텍스처를 만든다.
    /// </summary>
    public static class CraftFxPoly
    {
        /// <summary>viewBox 한 단위를 몇 픽셀로 굽는가 — 카탈로그 `billet_bake_px`(정본 SVG 는 벡터라 확대에 견뎌야 한다 · 모루는 화면에서 ≈2px/단위다).</summary>
        public static float PixelsPerUnit { get { return UiKit.L("billet_bake_px"); } }

        /// <summary>가장자리 계단을 없애는 초과표본(한 변 n칸).</summary>
        private const int Super = 3;

        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <summary>도형의 바깥 사각(viewBox 단위).</summary>
        public static Rect Bounds(Vector2[] pts)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < pts.Length; i++)
            {
                if (pts[i].x < minX) minX = pts[i].x;
                if (pts[i].y < minY) minY = pts[i].y;
                if (pts[i].x > maxX) maxX = pts[i].x;
                if (pts[i].y > maxY) maxY = pts[i].y;
            }
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>테두리를 무게중심 기준으로 부풀린 사본 — 정본 `stroke` 자리(모루 `Outlined` 와 같은 길: 큰 면을 뒤에 깐다).</summary>
        public static Vector2[] Inflate(Vector2[] pts, float pad)
        {
            Rect b = Bounds(pts);
            Vector2 c = new Vector2(b.center.x, b.center.y);
            float sx = 1f + pad * 2f / Mathf.Max(1e-4f, b.width), sy = 1f + pad * 2f / Mathf.Max(1e-4f, b.height);
            Vector2[] outPts = new Vector2[pts.Length];
            for (int i = 0; i < pts.Length; i++) outPts[i] = new Vector2(c.x + (pts[i].x - c.x) * sx, c.y + (pts[i].y - c.y) * sy);
            return outPts;
        }

        /// <summary>단색 면(색은 <see cref="Image.color"/> 로 — 불투명도 애니메이션이 그 α 다).</summary>
        public static Sprite Bake(string name, Vector2[] pts)
        {
            return Bake(name, pts, null, null, Vector2.zero, new Vector2(0f, 1f));
        }

        /// <summary>
        /// 폴리곤을 구워 스프라이트로. <paramref name="stops"/> 가 있으면 SVG `linearGradient`(<paramref name="gradFrom"/>→<paramref name="gradTo"/> · 도형 바깥 사각의 0~1 좌표)를 함께 굽는다.
        /// 같은 이름은 한 번만 굽는다.
        /// </summary>
        public static Sprite Bake(string name, Vector2[] pts, Color[] stops, float[] offsets, Vector2 gradFrom, Vector2 gradTo)
        {
            Sprite hit;
            if (cache.TryGetValue(name, out hit) && hit != null) return hit;

            Rect b = Bounds(pts);
            int w = Mathf.Max(2, Mathf.CeilToInt(b.width * PixelsPerUnit));
            int h = Mathf.Max(2, Mathf.CeilToInt(b.height * PixelsPerUnit));
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color32[] px = new Color32[w * h];
            float inv = 1f / Super;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float cover = 0f;
                    float gx = 0f, gy = 0f;
                    for (int sy = 0; sy < Super; sy++)
                    {
                        for (int sx = 0; sx < Super; sx++)
                        {
                            // 텍스처는 아래가 0행이고 SVG 는 위가 0 이라 y 를 뒤집는다.
                            float fx = (x + (sx + 0.5f) * inv) / w;
                            float fy = 1f - (y + (sy + 0.5f) * inv) / h;
                            Vector2 p = new Vector2(b.xMin + fx * b.width, b.yMin + fy * b.height);
                            if (Inside(pts, p)) { cover += 1f; gx += fx; gy += fy; }
                        }
                    }
                    int n = Super * Super;
                    byte a = (byte)Mathf.RoundToInt(255f * cover / n);
                    Color c = Color.white;
                    if (stops != null && cover > 0f) c = Sample(stops, offsets, Project(new Vector2(gx / cover, gy / cover), gradFrom, gradTo));
                    px[y * w + x] = new Color32(
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f),
                        (byte)Mathf.RoundToInt(a * Mathf.Clamp01(c.a)));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);

            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sp.name = name;
            cache[name] = sp;
            return sp;
        }

        /// <summary>
        /// 방사 그라디언트 타원을 굽는다(SVG `radialGradient cx=.5 cy=.5 r=.5` 꼴 · 정본 `anv-billetglow` 의 빛 웅덩이).
        /// 가운데가 stop 0, 가장자리가 마지막 stop 이고 그 사이는 선형이다. 크기는 viewBox 단위의 반지름 둘.
        /// </summary>
        public static Sprite BakeEllipse(string name, float rx, float ry, Color[] stops, float[] offsets)
        {
            Sprite hit;
            if (cache.TryGetValue(name, out hit) && hit != null) return hit;

            int w = Mathf.Max(2, Mathf.CeilToInt(rx * 2f * PixelsPerUnit));
            int h = Mathf.Max(2, Mathf.CeilToInt(ry * 2f * PixelsPerUnit));
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x + 0.5f) / w * 2f - 1f, ny = (y + 0.5f) / h * 2f - 1f;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);          // 타원 안이면 0~1
                    Color c = Sample(stops, offsets, Mathf.Clamp01(d));
                    float a = d >= 1f ? 0f : c.a;
                    px[y * w + x] = new Color32(
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sp.name = name;
            cache[name] = sp;
            return sp;
        }

        /// <summary>
        /// 타원 **테두리**(정본 `.af-ring` 처럼 `fill:none` + stroke)를 굽는다 — 안팎 두 타원 사이만 칠한다.
        /// ⚠ 정본은 `vector-effect: non-scaling-stroke` 라 퍼져도 굵기가 그대로지만, 구운 스프라이트를 키우면 굵기도 같이 큰다(결정 230).
        /// </summary>
        public static Sprite BakeRing(string name, float rx, float ry, float stroke)
        {
            Sprite hit;
            if (cache.TryGetValue(name, out hit) && hit != null) return hit;

            int w = Mathf.Max(4, Mathf.CeilToInt(rx * 2f * PixelsPerUnit));
            int h = Mathf.Max(4, Mathf.CeilToInt(ry * 2f * PixelsPerUnit));
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color32[] px = new Color32[w * h];
            float inx = Mathf.Max(0.01f, rx - stroke * 0.5f) / rx, iny = Mathf.Max(0.01f, ry - stroke * 0.5f) / ry;
            float inv = 1f / Super;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float cover = 0f;
                    for (int sy = 0; sy < Super; sy++)
                    {
                        for (int sx = 0; sx < Super; sx++)
                        {
                            float nx = (x + (sx + 0.5f) * inv) / w * 2f - 1f;
                            float ny = (y + (sy + 0.5f) * inv) / h * 2f - 1f;
                            bool outside = nx * nx + ny * ny <= 1f;
                            float ix = nx / inx, iy = ny / iny;
                            bool inside = ix * ix + iy * iy <= 1f;
                            if (outside && !inside) cover += 1f;
                        }
                    }
                    byte a = (byte)Mathf.RoundToInt(255f * cover / (Super * Super));
                    px[y * w + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sp.name = name;
            cache[name] = sp;
            return sp;
        }

        /// <summary>축(<paramref name="from"/>→<paramref name="to"/>) 위 위치 t — SVG `linearGradient x1y1 → x2y2` 와 같은 뜻.</summary>
        private static float Project(Vector2 p, Vector2 from, Vector2 to)
        {
            Vector2 axis = to - from;
            float len2 = axis.sqrMagnitude;
            if (len2 <= 1e-9f) return 0f;
            return Mathf.Clamp01(Vector2.Dot(p - from, axis) / len2);
        }

        /// <summary>stop 사이는 선형(SVG 와 같다).</summary>
        private static Color Sample(Color[] stops, float[] offsets, float t)
        {
            if (stops.Length == 1) return stops[0];
            if (t <= offsets[0]) return stops[0];
            int n = stops.Length;
            if (t >= offsets[n - 1]) return stops[n - 1];
            int hi = 1;
            while (hi < n && offsets[hi] < t) hi++;
            int lo = hi - 1;
            float span = offsets[hi] - offsets[lo];
            float f = span <= 0f ? 1f : (t - offsets[lo]) / span;
            return Color.Lerp(stops[lo], stops[hi], f);
        }

        /// <summary>점이 폴리곤 안인가(짝홀 규칙 · 볼록·오목 무관).</summary>
        private static bool Inside(Vector2[] pts, Vector2 p)
        {
            bool inside = false;
            int n = pts.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if ((pts[i].y > p.y) != (pts[j].y > p.y))
                {
                    float x = pts[i].x + (p.y - pts[i].y) / (pts[j].y - pts[i].y) * (pts[j].x - pts[i].x);
                    if (p.x < x) inside = !inside;
                }
            }
            return inside;
        }
    }
}
