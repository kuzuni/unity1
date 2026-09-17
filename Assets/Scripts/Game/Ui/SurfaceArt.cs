using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T178 — 정본이 면에 까는 **겹**(`linear-gradient`)을 그 각도·정지점 그대로 굽는다.
    ///
    /// 왜 있나: 정본 `style.css` 는 거의 모든 면에 겹을 깐다(선언 176 · 선택자 137) — 위쪽 1px 흰 줄 · 45° 미세 빗금 · 세로 명암 ·
    /// 배너의 비스듬한 바탕 · 제목 자리 스크림. 클론의 공용 표면(<see cref="UiKit.Panel"/>·<see cref="UiKit.Rounded"/>)은
    /// **색 한 칸짜리 <see cref="Image"/>** 라 그 겹이 조용히 사라지고, 화면이 «플라스틱 단색» 으로 읽힌다(T33 16회차 실측).
    /// 맨 <see cref="Graphic"/> 은 이 레포에서 안 칠해지므로(결정 223) 겹은 **구워서** 얹는다 — T87 `CraftFxPoly` 이래의 길이다.
    ///
    /// 각도는 **CSS 의 뜻 그대로**다: 0deg 가 위로, 90deg 가 오른쪽. 방향 벡터는 (sinθ, −cosθ)(y 아래가 +)이고
    /// 그라디언트 선 길이는 `|W·sinθ| + |H·cosθ|` — 그래서 0%·100% 정지점이 양 끝 모서리에 닿는다(CSS 규격).
    /// 각도가 **비율을 타므로** 정사각에 구워 늘리면 120° 가 다른 각이 된다 — 그 자리의 실제 비율로 굽는다(T156 `RibbonArt` 와 같은 까닭).
    ///
    /// **섞는 공간**(T178 8회차 · T357 이 수로 밝힌 것): 브라우저는 `rgba(…, .16)` 겹을 **sRGB 바이트 위에서** 섞고, 이 프로젝트는 Linear 색공간이라
    /// 유니티가 같은 알파를 **선형 값 위에서** 섞는다 — 어두운 바탕일수록 결과가 훨씬 밝게 나온다(런 537 탭바 실측 98/71/53/19/9 ↔ 정본 42/28/25/17/10).
    /// 그래서 **바탕을 아는 겹**은 표에 `over_color`(카탈로그 색 키) 또는 `over_layer`(이 표의 다른 겹 키)를 적어 두고, 여기서 <see cref="SurfaceBlendRules.OverSrgb"/> 로
    /// **미리 합성해 불투명하게** 굽는다. 불투명한 화소는 섞이지 않으므로 화면에 정본 바이트가 그대로 나온다. 바탕을 모르는 자리는 종전대로 알파로 얹는다.
    ///
    /// 굽은 그림은 `Apply(false, false)` 로 **읽을 수 있게** 남긴다(PlayMode 자가 «어느 쪽이 짙은가» 를 픽셀로 잰다 · T173 과 같은 길).
    /// </summary>
    public static class SurfaceArt
    {
        public const string ResourcePath = "SurfaceUi";

        static JsonObject root;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static JsonObject Table()
        {
            if (root != null) return root;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new KeyNotFoundException("Resources/" + ResourcePath + ".json 이 없다");
            root = J.Obj(MiniJson.Parse(ta.text));
            if (root == null || root.Count == 0) throw new KeyNotFoundException(ResourcePath + ".json 을 못 읽었다");
            return root;
        }

        /// <summary>그 겹의 정본 각도(도 · CSS 뜻 그대로).</summary>
        public static float Angle(string key)
        {
            JsonObject one = J.Obj(Table()[key]);
            object v = one == null ? null : one["angle_deg"];
            if (!J.IsNum(v)) throw new KeyNotFoundException(ResourcePath + ".json 의 «" + key + "» 에 angle_deg 가 없다");
            return (float)J.Num(v);
        }

        /// <summary>그 겹이 **방사형**인가(`shape: "radial"` · 정본 `radial-gradient(…)`) — 그러면 각도 대신 중심·반지름을 쓴다. T178 5회차.</summary>
        public static bool IsRadial(string key)
        {
            JsonObject one = J.Obj(Table()[key]);
            return one != null && J.Str(one["shape"]) == "radial";
        }

        /// <summary>방사형 겹의 중심·반지름(상자 비율 · CSS `radial-gradient(ellipse RX% RY% at CX% CY%, …)` 그대로).</summary>
        public static void Ellipse(string key, out float cx, out float cy, out float rx, out float ry)
        {
            JsonObject one = J.Obj(Table()[key]);
            if (one == null) throw new KeyNotFoundException(ResourcePath + ".json 에 «" + key + "» 이 없다");
            cx = (float)J.Num(one["cx"], 0.5); cy = (float)J.Num(one["cy"], 0.5);
            rx = (float)J.Num(one["rx"], 0.5); ry = (float)J.Num(one["ry"], 0.5);
            if (rx <= 0f || ry <= 0f) throw new KeyNotFoundException(ResourcePath + ".json 의 «" + key + "» 반지름이 0 이다");
        }

        /// <summary>그 겹의 정지점이 CSS px 인가(`unit: "px"` · 정본 `0 1px` 림) — 그러면 <see cref="Bake(string, float, float)"/> 가 자리의 선 길이로 나눈다. T178 4회차.</summary>
        public static bool PxOffsets(string key)
        {
            JsonObject one = J.Obj(Table()[key]);
            return one != null && J.Str(one["unit"]) == "px";
        }

        /// <summary>CSS px 하나 = 캔버스 px 몇 개(표 뿌리 `css_px`).</summary>
        public static float CssPx { get { return (float)J.Num(Table()["css_px"], 2.164); } }

        /// <summary>그 겹의 정지점 — 색과 자리(0~1 · `unit: "px"` 인 겹은 CSS px)를 한 쌍으로.</summary>
        public static void Stops(string key, out Color[] colors, out float[] offsets)
        {
            JsonObject one = J.Obj(Table()[key]);
            List<object> st = one == null ? null : J.Arr(one["stops"]);
            List<object> of = one == null ? null : J.Arr(one["offsets"]);
            if (st == null || of == null || st.Count < 2 || of.Count != st.Count)
                throw new KeyNotFoundException(ResourcePath + ".json 의 «" + key + "» 에 stops·offsets(길이가 같은 둘)이 없다");
            colors = new Color[st.Count];
            offsets = new float[of.Count];
            for (int i = 0; i < st.Count; i++)
            {
                List<object> c = J.Arr(st[i]);
                if (c == null || c.Count < 4)
                    throw new KeyNotFoundException(ResourcePath + ".json 의 «" + key + "» stop " + i + " 가 [r,g,b,a] 가 아니다");
                colors[i] = new Color((float)J.Num(c[0]) / 255f, (float)J.Num(c[1]) / 255f, (float)J.Num(c[2]) / 255f, (float)J.Num(c[3]));
                offsets[i] = (float)J.Num(of[i]);
            }
        }

        /// <summary>정지점 사이 선형 — 첫 정지점 앞·마지막 정지점 뒤는 그 색 그대로다(CSS 와 같다).
        /// ⚠ 섞기는 **프리멀티플라이드 알파**로 한다(T178 21회차 · 결정 <c>750</c>): CSS Images 3 이 그라디언트 보간을 그렇게 못 박아 뒀고,
        /// 그냥 섞으면 «투명한 흰색 → 반투명 검정» 같은 짝에서 **가운데가 회색으로 밝아지는 띠**가 생긴다(브라우저엔 없는 띠다).
        /// 실측(런 1015 `screen_autoforge.png` 스피너): 정본 5005 의 45%(흰 .02) → 100%(검정 .34) 사이 63.6% 자리가
        /// 면 색 23 위에서 **42** 로 밝아져 있었다 — 프리멀티플라이드로 섞으면 같은 자리가 23 이다.</summary>
        public static Color Sample(Color[] col, float[] pos, float t)
        {
            if (t <= pos[0]) return col[0];
            for (int i = 1; i < pos.Length; i++)
            {
                if (t > pos[i]) continue;
                float span = pos[i] - pos[i - 1];
                float k = span <= 0f ? 1f : (t - pos[i - 1]) / span;
                return LerpPremul(col[i - 1], col[i], k);
            }
            return col[col.Length - 1];
        }

        /// <summary>CSS 그라디언트의 색 보간 — 프리멀티플라이드 알파에서 섞고 되돌린다(알파가 같으면 그냥 섞기와 똑같다).</summary>
        public static Color LerpPremul(Color a, Color b, float k)
        {
            float outA = Mathf.Lerp(a.a, b.a, k);
            if (outA <= 1e-6f) return new Color(0f, 0f, 0f, 0f);
            float r = Mathf.Lerp(a.r * a.a, b.r * b.a, k) / outA;
            float g = Mathf.Lerp(a.g * a.a, b.g * b.a, k) / outA;
            float bl = Mathf.Lerp(a.b * a.a, b.b * b.a, k) / outA;
            return new Color(r, g, bl, outA);
        }

        /// <summary>그 겹 한 장을 굽는다(같은 키·같은 비율은 한 번만). `unit: "px"` 인 겹은 <see cref="Bake(string, float, float)"/> 로 선 길이를 준다.</summary>
        public static Sprite Bake(string key, float aspect) { return Bake(key, aspect, 0f); }

        /// <summary>
        /// 그 겹 한 장을 굽는다. <paramref name="lineLenCanvasPx"/> = 그 자리의 그라디언트 선 길이(캔버스 px · |W·sin각| + |H·cos각|) —
        /// 정지점이 CSS px 인 겹(`0 1px` 림)은 이것으로 나눠 0~1 로 바꾼다(T178 4회차). % 겹은 무시한다.
        /// </summary>
        public static Sprite Bake(string key, float aspect, float lineLenCanvasPx) { return Bake(key, aspect, lineLenCanvasPx, null); }

        /// <summary>
        /// T178 10회차 — <paramref name="overBaseLayer"/> 는 **부르는 쪽이 알려 주는 바탕 겹**이다. 표의 `over_layer` 는 한 값이라
        /// «상태에 따라 바탕이 갈리는» 자리(퀘스트 막대의 파랑/초록 채움처럼)를 못 적는다 — 그 자리는 부르는 쪽이 그때의 바탕 키를 준다.
        /// 주면 표의 `over_layer`·`over_color` 대신 이것으로 sRGB 합성한다(정본이 섞는 길 · T357).
        /// </summary>
        /// <summary>
        /// T178 10회차 — 바탕이 **런타임 색**인 자리(등급색 위의 확률 막대처럼)는 겹 이름 대신 그 색을 준다.
        /// 값은 <see cref="Bake(string,float,float,string)"/> 와 같은 길(sRGB 바이트 합성 · 불투명하게 굽는다)이다.
        /// </summary>
        public static Sprite Bake(string key, float aspect, float lineLenCanvasPx, Color overBaseColor)
        {
            if (aspect <= 0f || float.IsNaN(aspect)) aspect = 1f;
            if (aspect > 8f) aspect = 8f;
            Color32 b = To32(overBaseColor);
            string name = key + "-" + aspect.ToString("0.00") + "-L" + Mathf.RoundToInt(lineLenCanvasPx)
                        + "-C" + b.r + "_" + b.g + "_" + b.b;
            Sprite hit;
            if (cache.TryGetValue(name, out hit) && hit != null) return hit;

            int shortSide = Mathf.Max(8, (int)J.Num(Table()["bake_px"], 96));
            int w = Mathf.Max(8, Mathf.RoundToInt(shortSide * aspect)), h = shortSide;
            Color32[] px = Pixels(key, w, h, lineLenCanvasPx, 0, null, b);
            return Finish(name, w, h, px);
        }

        static Color32 To32(Color c)
        {
            return new Color32((byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f),
                               (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f),
                               (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f), 255);
        }

        public static Sprite Bake(string key, float aspect, float lineLenCanvasPx, string overBaseLayer)
        {
            if (aspect <= 0f || float.IsNaN(aspect)) aspect = 1f;
            if (aspect > 8f) aspect = 8f;                      // 아주 납작한 자리도 굽는 비용을 묶는다
            bool needLen = PxOffsets(key) || BaseLayer(key) != null || overBaseLayer != null;
            string name = key + "-" + aspect.ToString("0.00") + (needLen ? "-L" + Mathf.RoundToInt(lineLenCanvasPx) : "")
                        + (overBaseLayer != null ? "-O" + overBaseLayer : "");
            Sprite hit;
            if (cache.TryGetValue(name, out hit) && hit != null) return hit;

            int shortSide = Mathf.Max(8, (int)J.Num(Table()["bake_px"], 96));
            int w = Mathf.Max(8, Mathf.RoundToInt(shortSide * aspect)), h = shortSide;
            return Finish(name, w, h, Pixels(key, w, h, lineLenCanvasPx, 0, overBaseLayer, null));
        }

        /// <summary>그 겹이 얹히는 바탕이 «이 표의 다른 겹» 인가(`over_layer`) — 그러면 그 겹을 같은 판에 먼저 굽고 위에 합성한다.</summary>
        static string BaseLayer(string key)
        {
            JsonObject one = J.Obj(Table()[key]);
            return one == null ? null : J.Str(one["over_layer"]);
        }

        /// <summary>그 겹이 얹히는 바탕이 «카탈로그 단색» 인가(`over_color`).</summary>
        static string BaseColor(string key)
        {
            JsonObject one = J.Obj(Table()[key]);
            return one == null ? null : J.Str(one["over_color"]);
        }

        /// <summary>
        /// 그 겹 한 판의 화소. 표에 바탕(`over_color`·`over_layer`)이 적힌 겹은 **정본이 섞는 길(sRGB 바이트)** 로 미리 합성해
        /// 불투명하게 돌려준다(T178 8회차 · 셈은 Core <see cref="SurfaceBlendRules"/>). 바탕이 없으면 종전처럼 알파를 그대로 둔다.
        /// </summary>
        static Color32[] Pixels(string key, int w, int h, float lineLenCanvasPx, int depth) { return Pixels(key, w, h, lineLenCanvasPx, depth, null, null); }

        static Color32[] Pixels(string key, int w, int h, float lineLenCanvasPx, int depth, string overBaseLayer, Color32? overBaseColor)
        {
            if (depth > 4) throw new KeyNotFoundException(ResourcePath + ".json 의 «" + key + "» 바탕(over_layer)이 서로를 물고 돈다");
            Color[] col; float[] pos;
            Stops(key, out col, out pos);
            Color32[] px = IsRadial(key) ? RadialPixels(key, w, h, col, pos) : LinearPixels(key, w, h, col, pos, lineLenCanvasPx);

            // 부르는 쪽이 준 바탕이 먼저다(T178 10회차) — 표의 `over_layer` 는 «한 값» 이라 상태로 갈리는 자리를 못 적는다.
            if (overBaseColor.HasValue)
            {
                // 겹이 여러 장인 자리(정본이 `background-image` 에 겹을 쉼표로 쌓은 것)는 바탕 색이 **사슬 맨 아래**에 있다 —
                // 그러니 이 겹의 `over_layer` 를 같은 색 위에 먼저 굽고 그 판을 바탕으로 삼는다(T178 17회차).
                // 사슬이 없으면 종전대로 색 한 칸이 바탕이다(T178 10회차 · 등급색 위 막대).
                string chain = BaseLayer(key);
                Color32[] baseRt = chain != null ? Pixels(chain, w, h, lineLenCanvasPx, depth + 1, null, overBaseColor) : null;
                Color32 flatRt = overBaseColor.Value;
                for (int i = 0; i < px.Length; i++)
                {
                    Color32 top = px[i], bot = baseRt != null ? baseRt[i] : flatRt;
                    double aRt = top.a / 255.0;
                    px[i] = new Color32(SurfaceBlendRules.OverSrgb(bot.r, top.r, aRt),
                                        SurfaceBlendRules.OverSrgb(bot.g, top.g, aRt),
                                        SurfaceBlendRules.OverSrgb(bot.b, top.b, aRt), 255);
                }
                return px;
            }
            string overLayer = overBaseLayer ?? BaseLayer(key), overColor = overBaseLayer != null ? null : BaseColor(key);
            if (overBaseLayer != null && overBaseLayer == key)
                throw new KeyNotFoundException(ResourcePath + ".json 의 «" + key + "» 이 제 자신을 바탕으로 받았다");
            if (overLayer == null && overColor == null) return px;
            Color32[] under = overLayer != null ? Pixels(overLayer, w, h, lineLenCanvasPx, depth + 1, null, null) : null;
            Color32 flat = new Color32(0, 0, 0, 255);
            if (under == null)
            {
                Color c = UiKit.C(overColor);
                flat = new Color32((byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f),
                                   (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f),
                                   (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f), 255);
            }
            for (int i = 0; i < px.Length; i++)
            {
                Color32 top = px[i], bottom = under != null ? under[i] : flat;
                double a = top.a / 255.0;
                px[i] = new Color32(SurfaceBlendRules.OverSrgb(bottom.r, top.r, a),
                                    SurfaceBlendRules.OverSrgb(bottom.g, top.g, a),
                                    SurfaceBlendRules.OverSrgb(bottom.b, top.b, a), 255);
            }
            return px;
        }

        /// <summary>곧은 겹 한 판(각도·정지점 그대로 · `unit: "px"` 인 겹은 자리의 선 길이로 나눈다).</summary>
        static Color32[] LinearPixels(string key, int w, int h, Color[] col, float[] pos, float lineLenCanvasPx)
        {
            float rad = Angle(key) * Mathf.Deg2Rad;
            float dx = Mathf.Sin(rad), dy = -Mathf.Cos(rad);   // CSS: 0deg 는 위로 · y 는 아래가 +
            if (PxOffsets(key))
            {
                // CSS px → 선 길이의 분수. 자리 길이를 모르면(0) 굽는 판의 길이를 쓴다(림이 굵게 나오지만 안 사라진다).
                float realLen = lineLenCanvasPx > 0f ? lineLenCanvasPx : Mathf.Abs(w * dx) + Mathf.Abs(h * dy);
                float k = CssPx / Mathf.Max(1f, realLen);
                for (int i = 0; i < pos.Length; i++) pos[i] = Mathf.Clamp01(pos[i] * k);
            }
            float len = Mathf.Abs(w * dx) + Mathf.Abs(h * dy);
            if (len <= 0f) len = 1f;

            Color32[] px = new Color32[w * h];
            float cx = w * 0.5f, cy = h * 0.5f;
            for (int y = 0; y < h; y++)
            {
                float py = h - 0.5f - y;                       // 텍스처는 아래가 0행이고 CSS 는 위가 0
                for (int x = 0; x < w; x++)
                {
                    float t = 0.5f + ((x + 0.5f - cx) * dx + (py - cy) * dy) / len;
                    Color c = Sample(col, pos, Mathf.Clamp01(t));
                    px[y * w + x] = Byte4(c);
                }
            }
            return px;
        }

        static Color32 Byte4(Color c)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(c.a) * 255f));
        }

        /// <summary>구운 화소를 스프라이트로(같은 키·같은 비율은 캐시).</summary>
        // ---- T368 되풀이 줄무늬(repeating-linear-gradient) ----

        /// <summary>그 줄무늬 표 절(`stripes`)을 꺼낸다.</summary>
        static JsonObject Stripe(string key)
        {
            JsonObject st = J.Obj(Table()["stripes"]);
            JsonObject one = st == null ? null : J.Obj(st[key]);
            if (one == null) throw new KeyNotFoundException(ResourcePath + ".json 의 «stripes» 에 «" + key + "» 이 없다");
            return one;
        }

        /// <summary>표의 색 칸 — `#RRGGBB` 리터럴이면 그대로, 그 밖이면 카탈로그 키, 비어 있으면 투명(정본의 `transparent`).</summary>
        static Color StripeColor(object v)
        {
            string s = J.Str(v);
            if (string.IsNullOrEmpty(s)) return new Color(0f, 0f, 0f, 0f);
            if (s[0] == '#')
            {
                Color c;
                if (ColorUtility.TryParseHtmlString(s, out c)) return c;
                throw new KeyNotFoundException(ResourcePath + ".json 의 줄무늬 색 «" + s + "» 을 못 읽는다");
            }
            return UiKit.C(s);
        }

        /// <summary>T368 3회차 — 줄무늬 표 한 칸의 수(없으면 <paramref name="dflt"/>). 표의 수는 앱 폭/높이 비율이라 부르는 쪽이 RefW·RefH 를 곱한다.</summary>
        public static float StripeNum(string key, string field, float dflt)
        {
            JsonObject one = Stripe(key);
            return (float)J.Num(one[field], dflt);        // 없는 키는 null → 기본값(각도 등 다른 칸과 같은 길)
        }

        /// <summary>정본이 «가로축으로 환산한 한 주기»(`.bw-hazard` 의 background-size)라고 적어 둔 그 폭 — 캔버스 px. 셈은 Core <see cref="StripeRules.TileWidth"/>.</summary>
        public static float StripeTileWidth(string key, float periodCanvasPx)
        {
            JsonObject one = Stripe(key);
            double ang = J.Num(one["angle_deg"], 90);
            return (float)StripeRules.TileWidth(ang, periodCanvasPx, periodCanvasPx * 64.0);
        }

        /// <summary>
        /// 줄무늬 **한 타일**을 굽는다 — 되풀이는 <see cref="Image.type"/> `Tiled` 가 맡는다(판을 화면 폭만큼 굽지 않는다).
        /// <paramref name="periodCanvasPx"/>·<paramref name="dashCanvasPx"/>·<paramref name="phaseCanvasPx"/> 는 호출자가 앱 폭/높이 비율을 곱해 넘긴 캔버스 px 이고,
        /// <paramref name="heightCanvasPx"/> 는 띠 두께다. 기울어진 줄무늬(−45°)는 타일 가로가 주기 ÷ |sinθ| 로 늘어난다(정본 1.556rem 과 같은 수).
        /// </summary>
        public static Sprite BakeStripe(string key, float periodCanvasPx, float dashCanvasPx, float phaseCanvasPx, float heightCanvasPx)
        {
            JsonObject one = Stripe(key);
            double ang = J.Num(one["angle_deg"], 90);
            Color ink = StripeColor(one["ink"]), gap = StripeColor(one["gap"]);

            int shortSide = Mathf.Max(8, (int)J.Num(Table()["bake_px"], 96));
            double tileW = StripeRules.TileWidth(ang, periodCanvasPx, periodCanvasPx * 64.0);
            // 굽는 판: 세로는 띠 두께에 맞추되 최소 한 줄 · 가로는 한 타일을 캔버스 px 그대로(작으면 정수배로 키워 계단을 줄인다)
            int h = Mathf.Clamp(Mathf.RoundToInt(heightCanvasPx), 1, shortSide * 4);
            int w = Mathf.Max(2, Mathf.RoundToInt((float)tileW));
            int up = Mathf.Max(1, Mathf.CeilToInt(16f / w));      // 아주 좁은 타일은 정수배로 굽는다(주기가 안 어긋난다)
            w *= up; h = Mathf.Max(1, h * up);
            string name = "stripe-" + key + "-" + w + "x" + h + "-" + Mathf.RoundToInt(dashCanvasPx * 10f) + "-" + Mathf.RoundToInt(phaseCanvasPx * 10f);
            Sprite hit;
            if (cache.TryGetValue(name, out hit) && hit != null) return hit;

            double sx = tileW / w, sy = heightCanvasPx <= 0f ? 1.0 : heightCanvasPx / h;
            Color32 inkC = ink, gapC = gap;
            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // 화소 **가운데**를 잰다 — 가장자리를 재면 주기 경계에서 한 줄이 통째로 뒤집힌다.
                    // ⚠ 유니티 텍스처의 y 는 **아래가 0** 이고 CSS 의 y 는 **위가 0** 이다 — 안 뒤집으면 −45° 가 화면에서 +45° 로 기운다(거울상).
                    double cx = (x + 0.5) * sx, cy = (h - 1 - y + 0.5) * sy;
                    px[y * w + x] = StripeRules.IsInk(cx, cy, ang, periodCanvasPx, dashCanvasPx, phaseCanvasPx) ? inkC : gapC;
                }
            }
            return Finish(name, w, h, px);
        }

        /// <summary>
        /// T178 15회차 — **교차 해칭 한 타일**(정본 828·1063·1131 `repeating-linear-gradient(45deg, rgba(0,0,0,.13) 0 2px, transparent 2px 12px)` + 같은 −45deg):
        /// 표 `stripes.<key>` 의 `angle_deg`·`period_css_px`·`dash_css_px`·`ink`·`ink_alpha` 로 한 타일(가로 p÷|sinθ| · 세로 p÷|cosθ| · 셈은 Core <see cref="StripeRules.HatchTile"/>)을 굽고
        /// 되풀이는 <see cref="Image.Type.Tiled"/> 가 맡는다. 바탕(<paramref name="baseColor"/> · 그 카드의 `color-mix` 면 색)을 알므로 정본이 섞는 길(<see cref="SurfaceBlendRules.OverSrgb"/> · 겹마다 한 번)로
        /// **미리 합성해 불투명하게** 굽는다 — Linear 색공간에서 알파 .13 검정을 그대로 얹으면 6% 만 어두워져 빗금이 반쯤 사라진다(T178 8회차·T357 과 같은 까닭).
        /// </summary>
        public static Sprite BakeHatch(string key, Color baseColor)
        {
            JsonObject one = Stripe(key);
            double ang = J.Num(one["angle_deg"], 45);
            float cssPx = CssPx;
            double period = J.Num(one["period_css_px"], 0) * cssPx, dash = J.Num(one["dash_css_px"], 0) * cssPx;
            if (period <= 0 || dash <= 0) throw new KeyNotFoundException(ResourcePath + ".json stripes." + key + " 에 period_css_px·dash_css_px 가 없다 (T178)");
            Color ink = StripeColor(one["ink"]);
            double a = J.Num(one["ink_alpha"], ink.a);
            double tw, th;
            StripeRules.HatchTile(ang, period, period * 64.0, out tw, out th);
            int w = Mathf.Max(2, Mathf.RoundToInt((float)tw)), h = Mathf.Max(2, Mathf.RoundToInt((float)th));
            int up = Mathf.Max(1, Mathf.CeilToInt(16f / Mathf.Min(w, h)));
            w *= up; h *= up;
            Color32 b = Byte4(baseColor), ik = Byte4(ink);
            string name = "hatch-" + key + "-" + w + "x" + h + "-" + b.r + "." + b.g + "." + b.b;
            Sprite hit;
            if (cache.TryGetValue(name, out hit) && hit != null) return hit;
            // 겹 수(0·1·2)마다 한 번씩 정본 길로 섞은 바이트 — 격자점(두 겹)은 두 번 곱해진다
            Color32[] lv = new Color32[3];
            lv[0] = new Color32(b.r, b.g, b.b, 255);
            for (int n = 1; n < 3; n++)
                lv[n] = new Color32(SurfaceBlendRules.OverSrgb(lv[n - 1].r, ik.r, a), SurfaceBlendRules.OverSrgb(lv[n - 1].g, ik.g, a), SurfaceBlendRules.OverSrgb(lv[n - 1].b, ik.b, a), 255);
            double sx = tw / w, sy = th / h;
            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    double cx = (x + 0.5) * sx, cy = (h - 1 - y + 0.5) * sy;      // 화소 가운데 · 텍스처 y 는 아래가 0(BakeStripe 와 같다)
                    px[y * w + x] = lv[StripeRules.HatchLayers(cx, cy, ang, period, dash)];
                }
            return Finish(name, w, h, px);
        }

        /// <summary>둥근 면 위에 교차 해칭을 얹는다 — 면에 <see cref="Mask"/>(정본은 `border-radius` 가 background 를 같이 자른다) · 되풀이는 `Tiled`. 면 색이 곧 바탕이라 불투명 타일이 면을 덮는다.</summary>
        public static Image FillHatch(Image face, string name, string key, Color baseColor)
        {
            if (face.GetComponent<Mask>() == null) face.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            RectTransform rt = UiKit.Box(face.rectTransform, name);
            UiKit.Fill(rt);
            Image img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.type = Image.Type.Tiled;
            img.sprite = BakeHatch(key, baseColor);
            img.color = Color.white;
            return img;
        }

        /// <summary>
        /// T178 18회차 — **셀 면 통째 굽기**. 정본 7730 `.equip-cell:not(.egg-cell)` 처럼 «되풀이 해칭 위에 방사·선형 겹을 알파로 얹는» 자리는
        /// 타일(<see cref="BakeHatch"/>) 위에 알파 겹을 얹을 수 없다 — 이 프로젝트는 Linear 색공간이라 유니티가 선형 값 위에서 섞어 정본과 다른 색이 된다(T178 8회차·T357).
        /// 그래서 **칸 크기 그대로 한 판**에 바탕색 → 해칭 두 겹(<paramref name="hatchKey"/> · null 이면 없음) → <paramref name="layers"/>(아래 겹부터 위 겹 순 · 표 키)를
        /// 정본이 섞는 길(<see cref="SurfaceBlendRules.OverSrgb"/> · 겹마다 한 번)로 차례로 합성해 불투명하게 굽는다. 칸은 크기가 같아 색·크기별로 한 번만 굽는다(캐시).
        /// 판 해상도 = 칸의 캔버스 px(해칭 주기가 늘어나지 않게 · 상한은 표 `face_px_max`).
        /// </summary>
        public static Sprite BakeFace(string hatchKey, string[] layers, Color baseColor, float wCanvasPx, float hCanvasPx)
        {
            int cap = Mathf.Max(16, (int)J.Num(Table()["face_px_max"], 512));
            int w = Mathf.Clamp(Mathf.RoundToInt(wCanvasPx), 8, cap), h = Mathf.Clamp(Mathf.RoundToInt(hCanvasPx), 8, cap);
            Color32 b = To32(baseColor);
            string name = "face-" + (hatchKey ?? "-") + "-" + string.Join("+", layers) + "-" + w + "x" + h + "-" + b.r + "." + b.g + "." + b.b;
            Sprite hit;
            if (cache.TryGetValue(name, out hit) && hit != null) return hit;

            Color32[] px = new Color32[w * h];
            if (hatchKey != null)
            {
                JsonObject one = Stripe(hatchKey);
                double ang = J.Num(one["angle_deg"], 45), cssPx = CssPx;
                double period = J.Num(one["period_css_px"], 0) * cssPx, dash = J.Num(one["dash_css_px"], 0) * cssPx;
                if (period <= 0 || dash <= 0) throw new KeyNotFoundException(ResourcePath + ".json stripes." + hatchKey + " 에 period_css_px·dash_css_px 가 없다 (T178)");
                Color32 ik = Byte4(StripeColor(one["ink"]));
                double a = J.Num(one["ink_alpha"], StripeColor(one["ink"]).a);
                Color32[] lv = new Color32[3];
                lv[0] = new Color32(b.r, b.g, b.b, 255);
                for (int n = 1; n < 3; n++)
                    lv[n] = new Color32(SurfaceBlendRules.OverSrgb(lv[n - 1].r, ik.r, a), SurfaceBlendRules.OverSrgb(lv[n - 1].g, ik.g, a), SurfaceBlendRules.OverSrgb(lv[n - 1].b, ik.b, a), 255);
                double sx = wCanvasPx / w, sy = hCanvasPx / h;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        double cx = (x + 0.5) * sx, cy = (h - 1 - y + 0.5) * sy;      // 화소 가운데 · 텍스처 y 는 아래가 0(BakeHatch 와 같다)
                        px[y * w + x] = lv[StripeRules.HatchLayers(cx, cy, ang, period, dash)];
                    }
            }
            else
                for (int i = 0; i < px.Length; i++) px[i] = new Color32(b.r, b.g, b.b, 255);

            foreach (string key in layers)
            {
                float lineLen = 0f;
                if (!IsRadial(key))
                {
                    float rad = Angle(key) * Mathf.Deg2Rad;
                    lineLen = Mathf.Abs(wCanvasPx * Mathf.Sin(rad)) + Mathf.Abs(hCanvasPx * Mathf.Cos(rad));
                }
                Color32[] top = Pixels(key, w, h, lineLen, 0, null, null);        // 표에 바탕이 없는 겹 = 알파 그대로
                for (int i = 0; i < px.Length; i++)
                {
                    Color32 t = top[i], u = px[i];
                    double ta = t.a / 255.0;
                    px[i] = new Color32(SurfaceBlendRules.OverSrgb(u.r, t.r, ta), SurfaceBlendRules.OverSrgb(u.g, t.g, ta), SurfaceBlendRules.OverSrgb(u.b, t.b, ta), 255);
                }
            }
            return Finish(name, w, h, px);
        }

        /// <summary>둥근 면 위에 <see cref="BakeFace"/> 한 판을 얹는다(면에 <see cref="Mask"/> · `Simple` · 클릭 안 먹음). <paramref name="w"/>·<paramref name="h"/> = 면의 캔버스 px. T178 18회차.</summary>
        public static Image FillFace(Image face, string name, string hatchKey, string[] layers, Color baseColor, float w, float h)
        {
            if (face.GetComponent<Mask>() == null) face.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            RectTransform rt = UiKit.Box(face.rectTransform, name);
            UiKit.Fill(rt);
            Image img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.type = Image.Type.Simple;
            img.sprite = BakeFace(hatchKey, layers, baseColor, w, h);
            img.color = Color.white;
            return img;
        }

        static Sprite Finish(string name, int w, int h, Color32[] px)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = "sf-" + name;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels32(px);
            tex.Apply(false, false);
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sp.name = tex.name;
            cache[name] = sp;
            return sp;
        }

        /// <summary>
        /// 방사형 한 장(정본 `radial-gradient(ellipse RX% RY% at CX% CY%, …)`). 타원 좌표에서 잰 거리(0 = 중심 · 1 = 반지름 끝)를
        /// 그대로 정지점 t 로 쓴다 — CSS 도 «반지름 = 100%» 로 잰다. 상자 밖으로 나가는 부분은 마지막 정지점 색(대개 투명)이다.
        /// </summary>
        static Color32[] RadialPixels(string key, int w, int h, Color[] col, float[] pos)
        {
            float cx, cy, rx, ry;
            Ellipse(key, out cx, out cy, out rx, out ry);
            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float py = h - 0.5f - y;                        // 텍스처는 아래가 0행 · CSS 는 위가 0
                for (int x = 0; x < w; x++)
                {
                    float u = ((x + 0.5f) / w - cx) / rx;
                    float v = (py / h - cy) / ry;
                    Color c = Sample(col, pos, Mathf.Clamp01(Mathf.Sqrt(u * u + v * v)));
                    px[y * w + x] = Byte4(c);
                }
            }
            return px;
        }

        /// <summary>그 겹을 <paramref name="parent"/> 를 꽉 채우게 얹는다(자리·크기는 부모가 쥔다 — 겹은 layout 을 안 바꾼다).</summary>
        public static Image Fill(RectTransform parent, string name, string key, float w, float h) { return Fill(parent, name, key, w, h, null); }

        /// <summary>바탕 겹을 부르는 쪽이 알려 주는 판(T178 10회차) — 상태로 바탕이 갈리는 자리(퀘스트 막대 채움 파랑/초록)가 이 길로 정본 합성을 받는다.</summary>
        /// <summary>바탕이 **런타임 색**인 자리(등급색 위의 막대 등) — 색을 주면 같은 길로 sRGB 합성해 굽는다(T178 10회차).</summary>
        public static Image Fill(RectTransform parent, string name, string key, float w, float h, Color overBaseColor)
        {
            Image img = Fill(parent, name, key, w, h, (string)null);
            float aspect = h > 0f ? w / h : 1f;
            float rad2 = Angle(key) * Mathf.Deg2Rad;
            float lineLen = IsRadial(key) ? 0f : Mathf.Abs(w * Mathf.Sin(rad2)) + Mathf.Abs(h * Mathf.Cos(rad2));
            img.sprite = Bake(key, aspect, lineLen, overBaseColor);
            return img;
        }

        public static Image Fill(RectTransform parent, string name, string key, float w, float h, string overBaseLayer)
        {
            RectTransform rt = UiKit.Box(parent, name);
            UiKit.Fill(rt);
            Image img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.type = Image.Type.Simple;
            float aspect = h > 0f ? w / h : 1f;
            if (IsRadial(key)) img.sprite = Bake(key, aspect, 0f, overBaseLayer);       // 방사형은 각도·선 길이가 없다(T178 5회차)
            else
            {
                float rad = Angle(key) * Mathf.Deg2Rad;
                img.sprite = Bake(key, aspect, Mathf.Abs(w * Mathf.Sin(rad)) + Mathf.Abs(h * Mathf.Cos(rad)), overBaseLayer);
            }
            img.color = Color.white;
            return img;
        }

        /// <summary>
        /// 둥근 면(`UiKit.Rounded`·`PopupKit.Outlined` 의 face) 위에 겹을 얹는다 — 면에 <see cref="Mask"/> 를 걸어 겹이 모서리 밖으로 안 새게(정본은 `border-radius` 가 background 를 같이 자른다).
        /// 면 그림은 그대로 보인다(`showMaskGraphic`) — 겹이 반투명한 자리에서 면 색이 비친다. T178 3회차.
        /// </summary>
        public static Image FillMasked(Image face, string name, string key, float w, float h) { return FillMasked(face, name, key, w, h, null); }

        /// <summary>바탕 겹을 부르는 쪽이 알려 주는 판(T178 10회차).</summary>
        public static Image FillMasked(Image face, string name, string key, float w, float h, string overBaseLayer)
        {
            if (face.GetComponent<Mask>() == null) face.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            return Fill(face.rectTransform, name, key, w, h, overBaseLayer);
        }

        /// <summary>바탕이 **런타임 색**인 둥근 면(서브탭 켜짐 칸처럼 면 색이 곁 표에서 오는 자리) — T178 17회차.</summary>
        public static Image FillMasked(Image face, string name, string key, float w, float h, Color overBaseColor)
        {
            if (face.GetComponent<Mask>() == null) face.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            return Fill(face.rectTransform, name, key, w, h, overBaseColor);
        }
    }
}
