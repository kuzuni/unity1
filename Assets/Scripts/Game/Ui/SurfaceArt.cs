using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;

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

        /// <summary>정지점 사이 선형 — 첫 정지점 앞·마지막 정지점 뒤는 그 색 그대로다(CSS 와 같다).</summary>
        public static Color Sample(Color[] col, float[] pos, float t)
        {
            if (t <= pos[0]) return col[0];
            for (int i = 1; i < pos.Length; i++)
            {
                if (t > pos[i]) continue;
                float span = pos[i] - pos[i - 1];
                float k = span <= 0f ? 1f : (t - pos[i - 1]) / span;
                return Color.Lerp(col[i - 1], col[i], k);
            }
            return col[col.Length - 1];
        }

        /// <summary>그 겹 한 장을 굽는다(같은 키·같은 비율은 한 번만). `unit: "px"` 인 겹은 <see cref="Bake(string, float, float)"/> 로 선 길이를 준다.</summary>
        public static Sprite Bake(string key, float aspect) { return Bake(key, aspect, 0f); }

        /// <summary>
        /// 그 겹 한 장을 굽는다. <paramref name="lineLenCanvasPx"/> = 그 자리의 그라디언트 선 길이(캔버스 px · |W·sin각| + |H·cos각|) —
        /// 정지점이 CSS px 인 겹(`0 1px` 림)은 이것으로 나눠 0~1 로 바꾼다(T178 4회차). % 겹은 무시한다.
        /// </summary>
        public static Sprite Bake(string key, float aspect, float lineLenCanvasPx)
        {
            if (aspect <= 0f || float.IsNaN(aspect)) aspect = 1f;
            if (aspect > 8f) aspect = 8f;                      // 아주 납작한 자리도 굽는 비용을 묶는다
            bool pxUnit = PxOffsets(key);
            string name = key + "-" + aspect.ToString("0.00") + (pxUnit ? "-L" + Mathf.RoundToInt(lineLenCanvasPx) : "");
            Sprite hit;
            if (cache.TryGetValue(name, out hit) && hit != null) return hit;

            int shortSide = Mathf.Max(8, (int)J.Num(Table()["bake_px"], 96));
            int w = Mathf.Max(8, Mathf.RoundToInt(shortSide * aspect)), h = shortSide;
            Color[] col; float[] pos;
            Stops(key, out col, out pos);
            float rad = Angle(key) * Mathf.Deg2Rad;
            float dx = Mathf.Sin(rad), dy = -Mathf.Cos(rad);   // CSS: 0deg 는 위로 · y 는 아래가 +
            if (pxUnit)
            {
                // CSS px → 선 길이의 분수. 자리 길이를 모르면(0) 굽는 판의 길이를 쓴다(림이 굵게 나오지만 안 사라진다).
                float realLen = lineLenCanvasPx > 0f ? lineLenCanvasPx : Mathf.Abs(w * dx) + Mathf.Abs(h * dy);
                float k = CssPx / Mathf.Max(1f, realLen);
                for (int i = 0; i < pos.Length; i++) pos[i] = Mathf.Clamp01(pos[i] * k);
            }
            float len = Mathf.Abs(w * dx) + Mathf.Abs(h * dy);
            if (len <= 0f) len = 1f;

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = "sf-" + name;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color32[] px = new Color32[w * h];
            float cx = w * 0.5f, cy = h * 0.5f;
            for (int y = 0; y < h; y++)
            {
                float py = h - 0.5f - y;                       // 텍스처는 아래가 0행이고 CSS 는 위가 0
                for (int x = 0; x < w; x++)
                {
                    float t = 0.5f + ((x + 0.5f - cx) * dx + (py - cy) * dy) / len;
                    Color c = Sample(col, pos, Mathf.Clamp01(t));
                    px[y * w + x] = new Color32(
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.a) * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sp.name = tex.name;
            cache[name] = sp;
            return sp;
        }

        /// <summary>그 겹을 <paramref name="parent"/> 를 꽉 채우게 얹는다(자리·크기는 부모가 쥔다 — 겹은 layout 을 안 바꾼다).</summary>
        public static Image Fill(RectTransform parent, string name, string key, float w, float h)
        {
            RectTransform rt = UiKit.Box(parent, name);
            UiKit.Fill(rt);
            Image img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.type = Image.Type.Simple;
            float rad = Angle(key) * Mathf.Deg2Rad;
            img.sprite = Bake(key, h > 0f ? w / h : 1f, Mathf.Abs(w * Mathf.Sin(rad)) + Mathf.Abs(h * Mathf.Cos(rad)));
            img.color = Color.white;
            return img;
        }

        /// <summary>
        /// 둥근 면(`UiKit.Rounded`·`PopupKit.Outlined` 의 face) 위에 겹을 얹는다 — 면에 <see cref="Mask"/> 를 걸어 겹이 모서리 밖으로 안 새게(정본은 `border-radius` 가 background 를 같이 자른다).
        /// 면 그림은 그대로 보인다(`showMaskGraphic`) — 겹이 반투명한 자리에서 면 색이 비친다. T178 3회차.
        /// </summary>
        public static Image FillMasked(Image face, string name, string key, float w, float h)
        {
            if (face.GetComponent<Mask>() == null) face.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            return Fill(face.rectTransform, name, key, w, h);
        }
    }
}
