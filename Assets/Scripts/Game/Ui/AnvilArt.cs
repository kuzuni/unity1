using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T98 1회차 — 모루 그림을 **정본 SVG 그대로** 세운다(정본 `web/js/ui.js` 2387 `ANVIL_SVG` · viewBox 132×86).
    ///
    /// 왜 다시 그리나: 클론의 옛 모루(`ForgeSheet.DrawAnvil`)는 상판·앞면·받침이 전부 **둥근 사각**이고 뿔이
    /// 오른쪽으로 튀어나온 둥근 사각이라, 원작 확대 대조에서 «모루» 가 아니라 «주황 블록 + 로켓 노즈» 로 읽혔다
    /// (T98 등재 · 워커 G 의 런 170 눈 대조). 정본 주석이 그 진단을 이미 적어 뒀고 — «실물 모루를 한눈에 모루로
    /// 만드는 건 뿔이 아니라 상판 위의 세 구멍/단» — 좌표도 «감으로 찍지 말 것: 상판 면을 매개변수로 풀어서 뽑았다»
    /// 라고 못 박아 뒀다. 그래서 여기는 **꼭짓점을 손으로 옮기지 않는다**: 표(`Resources/AnvilArtUi.json`)가 정본
    /// `d` 문자열을 글자 그대로 쥐고, <see cref="SvgPath"/> 가 펴고, <see cref="CraftFxPoly"/> 가 굽는다.
    ///
    /// 겹 순서는 정본 SVG 의 등장 순서 그대로다(뒤 → 앞): 받침 · 음각 · 목 · 뿔 · 상판 앞면 · 상판 윗면 ·
    /// 그 위에 획 없는 결·광택·구멍들. 정본이 `<g stroke="#170d0b" stroke-width="3">` 로 묶은 여섯에는
    /// **키라인 면**을 몸통 바로 뒤에 깐다(`CraftFxPoly.Inflate` = 무게중심 부풀림 · 폭의 절반만큼 바깥으로).
    ///
    /// 1회차는 여기까지 — `ForgeSheet.DrawAnvil` 에 바꿔 끼우는 것은 그 파일이 T108 lock 이라 2회차다.
    /// </summary>
    public static class AnvilArt
    {
        public const string ResourcePath = "AnvilArtUi";

        private static JsonObject root;
        private static List<object> parts;
        private static JsonObject grads;
        private static float viewW, viewH, strokeW;
        private static Color strokeColor;
        private static int samples;

        private static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new KeyNotFoundException("Resources/" + ResourcePath + ".json 이 없다");
            JsonObject r = J.Obj(MiniJson.Parse(ta.text));
            List<object> view = J.Arr(r["view"]);
            if (view == null || view.Count != 2) throw new KeyNotFoundException(ResourcePath + ".json 에 «view»(viewBox 폭·높이)가 없다");
            viewW = (float)J.Num(view[0]);
            viewH = (float)J.Num(view[1]);
            JsonObject st = J.Obj(r["stroke"]);
            if (st == null) throw new KeyNotFoundException(ResourcePath + ".json 에 «stroke» 가 없다");
            strokeW = (float)J.Num(st["width"]);
            strokeColor = Hex(J.Str(st["color"]), "stroke.color");
            samples = (int)J.Num(r["curve_samples"], SvgPath.DefaultCurveSamples);
            grads = J.Obj(r["grads"]);
            parts = J.Arr(r["parts"]);
            if (parts == null || parts.Count == 0) throw new KeyNotFoundException(ResourcePath + ".json 에 «parts» 가 없다");
            root = r;
        }

        /// <summary>정본 viewBox 폭(132) — 부르는 쪽이 «한 유닛 = 몇 px» 을 이 값으로 잡는다.</summary>
        public static float ViewW { get { Load(); return viewW; } }

        /// <summary>정본 viewBox 높이(86).</summary>
        public static float ViewH { get { Load(); return viewH; } }

        /// <summary>표에 담긴 겹 수(자가 «정본 겹을 다 옮겼나» 를 이 값으로 묻는다).</summary>
        public static int PartCount { get { Load(); return parts.Count; } }

        /// <summary>i 번째 겹의 이름(정본 SVG 등장 순서).</summary>
        public static string PartName(int i) { Load(); return J.Str(J.Obj(parts[i])["name"]); }

        /// <summary>i 번째 겹의 꼭짓점(viewBox 단위 · 타원 겹이면 null).</summary>
        public static double[][] PartPoints(int i)
        {
            Load();
            JsonObject o = J.Obj(parts[i]);
            string d = J.Str(o["d"]);
            return d == null ? null : SvgPath.Flatten(d, samples);
        }

        /// <summary>이름으로 겹을 찾는다(없으면 −1).</summary>
        public static int PartIndex(string name)
        {
            Load();
            for (int i = 0; i < parts.Count; i++) if (J.Str(J.Obj(parts[i])["name"]) == name) return i;
            return -1;
        }

        /// <summary>겹의 상자(viewBox 단위) — 타원 겹도 다룬다. 부르는 쪽이 «받침 자리» 같은 것을 표에서 얻는다.</summary>
        public static Rect PartBounds(string name)
        {
            Load();
            int i = PartIndex(name);
            if (i < 0) throw new KeyNotFoundException(ResourcePath + ".json 에 겹 «" + name + "» 이 없다");
            JsonObject o = J.Obj(parts[i]);
            List<object> el = J.Arr(o["ellipse"]);
            if (el != null)
            {
                float cx = (float)J.Num(el[0]), cy = (float)J.Num(el[1]), rx = (float)J.Num(el[2]), ry = (float)J.Num(el[3]);
                return new Rect(cx - rx, cy - ry, rx * 2f, ry * 2f);
            }
            double[] b = SvgPath.Bounds(SvgPath.Flatten(J.Str(o["d"]), samples));
            return new Rect((float)b[0], (float)b[1], (float)(b[2] - b[0]), (float)(b[3] - b[1]));
        }

        /// <summary>
        /// 겹에서 **가장 밝은 칠** — 구운 텍스처는 `Apply(false, true)` 로 읽기가 막혀 있어 자가 픽셀을 못 본다.
        /// «받침이 어두운 주철인가» 같은 물음은 표의 스톱으로 답한다(가장 밝은 스톱까지 어두우면 면 전체가 어둡다).
        /// </summary>
        public static Color StopBrightest(string name)
        {
            Load();
            int i = PartIndex(name);
            if (i < 0) throw new KeyNotFoundException(ResourcePath + ".json 에 겹 «" + name + "» 이 없다");
            JsonObject o = J.Obj(parts[i]);
            Color tint; Color[] stops; float[] offs; Vector2 f, t;
            Fill(o, out tint, out stops, out offs, out f, out t);
            if (stops == null) return tint;
            Color best = stops[0];
            for (int k = 1; k < stops.Length; k++)
                if (Luma(stops[k]) > Luma(best)) best = stops[k];
            return best;
        }

        private static float Luma(Color c) { return 0.299f * c.r + 0.587f * c.g + 0.114f * c.b; }

        /// <summary>
        /// 모루 한 벌을 <paramref name="parent"/> 아래에 세운다. 자리는 viewBox 좌상단 기준이고
        /// <paramref name="unit"/> 이 «viewBox 한 유닛 = 캔버스 몇 px» 이다(`ForgeSheet` 의 `vbUnit` 과 같은 뜻).
        /// </summary>
        public static RectTransform Build(Transform parent, string name, float unit)
        {
            Load();
            RectTransform box = UiKit.Box(parent, name);
            // ⚠ `UiKit.Box` 는 부모를 **꽉 채운다**(`Fill`) — `sizeDelta` 만 주면 «부모 폭 + 그 값» 이 되어
            //    런 308 에서 528 이어야 할 상자가 1608(= 1080 + 528)로 섰다. 자리·크기는 `Place` 로 준다(집 규칙).
            UiKit.Place(box, 0f, 0f, viewW * unit, viewH * unit);
            for (int i = 0; i < parts.Count; i++)
            {
                JsonObject o = J.Obj(parts[i]);
                string pn = J.Str(o["name"]);
                List<object> el = J.Arr(o["ellipse"]);
                if (el != null) { Ellipse(box, pn, el, o, unit); continue; }

                double[][] pts = SvgPath.Flatten(J.Str(o["d"]), samples);
                float sw = (float)J.Num(o["stroke"], 0);
                // 정본 `<g stroke=… stroke-width=3>` — 획은 면 **가장자리 가운데**에 걸리므로 절반만 바깥으로 부풀린 면을 몸통 뒤에 깐다.
                if (sw > 0) Poly(box, pn + "-line", CraftFxPoly.Inflate(V2(pts), sw * 0.5f), null, null, Vector2.zero, Vector2.zero, strokeColor, unit);
                Color tint; Color[] stops; float[] offs; Vector2 gf, gt;
                Fill(o, out tint, out stops, out offs, out gf, out gt);
                Poly(box, pn, V2(pts), stops, offs, gf, gt, tint, unit);
            }
            return box;
        }

        private static Vector2[] V2(double[][] pts)
        {
            Vector2[] v = new Vector2[pts.Length];
            for (int i = 0; i < pts.Length; i++) v[i] = new Vector2((float)pts[i][0], (float)pts[i][1]);
            return v;
        }

        /// <summary>겹의 칠 — 그라디언트 이름(`fill`)이면 표에서 스톱을 꺼내고, 단색(`color`+`alpha`)이면 흰 스프라이트를 그 색으로 물들인다.</summary>
        private static void Fill(JsonObject o, out Color tint, out Color[] stops, out float[] offs, out Vector2 from, out Vector2 to)
        {
            stops = null; offs = null; from = Vector2.zero; to = new Vector2(0f, 1f);
            string g = J.Str(o["fill"]);
            if (g != null)
            {
                JsonObject gd = J.Obj(grads == null ? null : grads[g]);
                if (gd == null) throw new KeyNotFoundException(ResourcePath + ".json 의 «grads» 에 «" + g + "» 이 없다");
                List<object> sa = J.Arr(gd["stops"]);
                stops = new Color[sa.Count];
                offs = new float[sa.Count];
                for (int i = 0; i < sa.Count; i++)
                {
                    List<object> s = J.Arr(sa[i]);
                    offs[i] = (float)J.Num(s[0]);
                    Color c = Hex(J.Str(s[1]), g);
                    c.a = (float)J.Num(s[2], 1);
                    stops[i] = c;
                }
                List<object> f = J.Arr(gd["from"]), t = J.Arr(gd["to"]);
                from = new Vector2((float)J.Num(f[0]), (float)J.Num(f[1]));
                to = new Vector2((float)J.Num(t[0]), (float)J.Num(t[1]));
                tint = Color.white;
                return;
            }
            tint = Hex(J.Str(o["color"]), J.Str(o["name"]));
            tint.a = (float)J.Num(o["alpha"], 1);
        }

        private static Image Poly(RectTransform box, string name, Vector2[] pts, Color[] stops, float[] offs, Vector2 from, Vector2 to, Color tint, float unit)
        {
            Rect b = CraftFxPoly.Bounds(pts);
            RectTransform rt = UiKit.Box(box, name);
            Image img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.sprite = stops == null ? CraftFxPoly.Bake(name, pts) : CraftFxPoly.Bake(name, pts, stops, offs, from, to);
            img.type = Image.Type.Simple;
            img.color = tint;
            UiKit.Place(rt, b.xMin * unit, b.yMin * unit, b.width * unit, b.height * unit);
            return img;
        }

        private static Image Ellipse(RectTransform box, string name, List<object> e, JsonObject o, float unit)
        {
            float cx = (float)J.Num(e[0]), cy = (float)J.Num(e[1]), rx = (float)J.Num(e[2]), ry = (float)J.Num(e[3]);
            Color tint = Hex(J.Str(o["color"]), name);
            tint.a = (float)J.Num(o["alpha"], 1);
            RectTransform rt = UiKit.Box(box, name);
            Image img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            // 단색 타원 — 굽는 자가 스톱을 요구하므로 흰색 둘을 주고 색은 `Image.color` 로 물들인다(단색이 곧 두 스톱이 같은 그라디언트다).
            img.sprite = CraftFxPoly.BakeEllipse(name, rx, ry, new Color[] { Color.white, Color.white }, new float[] { 0f, 1f });
            img.type = Image.Type.Simple;
            img.color = tint;
            UiKit.Place(rt, (cx - rx) * unit, (cy - ry) * unit, rx * 2f * unit, ry * 2f * unit);
            return img;
        }

        private static Color Hex(string hex, string where)
        {
            Color c;
            if (hex == null || !ColorUtility.TryParseHtmlString(hex, out c))
                throw new System.FormatException(ResourcePath + ".json 의 «" + where + "» 색 «" + hex + "» 을 못 읽는다");
            return c;
        }
    }
}
