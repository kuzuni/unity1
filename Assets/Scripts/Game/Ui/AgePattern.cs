using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.CraftFx;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 시대 무늬 층(정본 `[data-age]::before` + `--af-pat` + `gp-*` · T124 · 주인 지시 `grade-pattern-animation`): 항성간 이상 다섯 시대의 무늬를
    /// 상자(자동 제련 행 · 확률/목록 머리줄 · 장착 셀) 안에 **한 정의로** 깐다. 무늬 타일은 정본 그라디언트·SVG 정의를 Core 가 래스터한 것을 굽고(시대당 한 번 · 캐시),
    /// 움직임은 정본 키프레임 그대로(항성간 두 층 반대 방향 + 반짝임 · 다중 우주 `steps(7)` 계단 · 양자 파문(링 메시) · 지하 세계 흘러내림 + 숨쉬기 · 천상 표류 + 반짝임).
    /// 모든 이동량은 타일 한 주기라 되돌아갈 때 튀지 않는다. 앞 다섯 시대는 표에 없어 <see cref="Attach"/> 가 null 을 돌려주고 아무것도 안 깐다(정본 «민무늬»).
    /// 정본 `prefers-reduced-motion` 은 유니티에 그 OS 신호가 없어(T118 과 같은 판단) <see cref="Motion"/> 스위치 하나로만 둔다 — 무늬는 남고 흐름만 멈춘다.
    /// 세 자리에 거는 줄은 `ForgeUi`·`ForgeAutoPopup`·`ForgeInfoPopup`(T87 lock) 이 2회차에 잇는다.
    /// </summary>
    public sealed class AgePattern : MonoBehaviour
    {
        /// <summary>정본 `prefers-reduced-motion: reduce` 자리 — false 면 무늬는 그대로 두고 움직임만 멈춘다.</summary>
        public static bool Motion = true;

        static AgePatternSpec spec;
        static readonly Dictionary<string, Texture2D> tiles = new Dictionary<string, Texture2D>();

        public static AgePatternSpec Spec { get { if (spec == null) spec = AgePatternSpec.From(AgePatternStyle.Root); return spec; } }
        public static bool Has(string age) { return Spec.Has(age); }

        public string Age { get; private set; }
        public AgeSpec A { get; private set; }
        public RectTransform Host { get; private set; }
        public RectTransform Layer { get; private set; }
        public CanvasGroup Group { get; private set; }
        public AgePatternGraphic[] Layers { get; private set; }
        public AgeRingsGraphic Rings { get; private set; }
        /// <summary>바탕 흐림(장착 셀 .55 · 막대 1) × 반짝임.</summary>
        public float BaseOpacity { get; private set; }
        /// <summary>테스트용 시계: true 면 <see cref="Tick"/> 로만 간다.</summary>
        public bool Manual;
        public double Ms { get; private set; }
        float pxPerRem;

        /// <summary>
        /// 상자에 무늬 층을 깐다. <paramref name="cell"/> = 장착 셀(흐림 .55 · 정본 `filter: opacity(.55)`) / 막대(1) · <paramref name="mask"/> = 자동 제련 막대의 왼쪽 30→50% 마스크.
        /// 무늬가 없는 시대면 null(아무것도 안 만든다). 층은 <paramref name="siblingIndex"/> 자리(기본 1 = 바탕 채움 바로 위 · 썸네일·글자 뒤).
        /// </summary>
        public static AgePattern Attach(RectTransform host, string age, bool cell = false, bool mask = false, int siblingIndex = 1)
        {
            if (host == null) return null;
            AgePatternSpec s = Spec;
            AgeSpec a = s.Get(age);
            if (a == null) return null;
            RectTransform layer = UiKit.Box(host, "age-pattern");
            layer.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, host.childCount - 1));
            layer.gameObject.AddComponent<RectMask2D>();      // 정본 overflow:hidden
            CanvasGroup g = layer.gameObject.AddComponent<CanvasGroup>();
            g.blocksRaycasts = false; g.interactable = false;
            AgePattern p = layer.gameObject.AddComponent<AgePattern>();
            p.Age = age; p.A = a; p.Host = host; p.Layer = layer; p.Group = g;
            p.BaseOpacity = cell ? (float)s.CellOpacity : 1f;
            p.pxPerRem = PopupKit.Rem;
            float mFrom = mask ? (float)s.BarMaskFrom : -1f, mTo = mask ? (float)s.BarMaskTo : -1f;
            var list = new List<AgePatternGraphic>();
            for (int i = 0; i < a.Layers.Length; i++)
            {
                RectTransform lr = UiKit.Box(layer, "pat-" + i);
                AgePatternGraphic gr = lr.gameObject.AddComponent<AgePatternGraphic>();
                gr.texture = Tile(age, i);
                gr.color = AgePatternStyle.Parse(a.Layers[i].Color);
                gr.raycastTarget = false;
                gr.SetMask(mFrom, mTo);
                list.Add(gr);
            }
            p.Layers = list.ToArray();
            if (a.Rings != null)
            {
                RectTransform rr = UiKit.Box(layer, "pat-rings");
                AgeRingsGraphic ring = rr.gameObject.AddComponent<AgeRingsGraphic>();
                ring.raycastTarget = false;
                ring.color = AgePatternStyle.Parse(a.Rings.Color);
                ring.Setup(a, p.pxPerRem, s.RingSegments, mFrom, mTo);
                p.Rings = ring;
            }
            p.Apply();
            return p;
        }

        /// <summary>시대·층의 무늬 타일(정본 정의를 Core 가 래스터 · 시대당 한 번 굽고 캐시 · 반복 래핑).</summary>
        public static Texture2D Tile(string age, int layer)
        {
            string key = age + "#" + layer;
            Texture2D t;
            if (tiles.TryGetValue(key, out t) && t != null) return t;
            AgeSpec a = Spec.Get(age);
            LayerSpec l = a.Layers[layer];
            int w, h;
            float[] cov = AgePatternRules.Raster(Spec, l, PopupKit.Rem, out w, out h);
            t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.name = "age-pat-" + key;
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Bilinear;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float c = cov[y * w + x];
                    // 래스터는 위에서 아래, 텍스처는 아래에서 위 — 뒤집어 넣는다
                    px[(h - 1 - y) * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(c) * 255f));
                }
            t.SetPixels32(px);
            t.Apply(false, false);
            tiles[key] = t;
            return t;
        }

        /// <summary>테스트/시계 주입 — dt 만큼 진행하고 그린다.</summary>
        public void Tick(double dtMs) { Ms += dtMs; Apply(); }

        private void Update() { if (!Manual && Motion) Tick(Time.unscaledDeltaTime * 1000.0); else if (!Manual) Apply(); }

        /// <summary>지금 시각의 층 자리·밝기·위상을 그림에 준다.</summary>
        public void Apply()
        {
            if (A == null) return;
            AgePatternSpec s = Spec;
            Group.alpha = BaseOpacity * (float)AgePatternRules.Pulse(s, A, Ms);
            Rect hr = Host.rect;
            for (int i = 0; i < Layers.Length; i++)
            {
                AgePatternGraphic g = Layers[i];
                Texture2D t = g.texture as Texture2D;
                if (t == null) continue;
                double xRem, yRem;
                AgePatternRules.Shift(A, i, Ms, out xRem, out yRem);
                // CSS background-position +x = 무늬가 오른쪽으로 · +y = 아래로 → uv 는 반대 방향(x) · 텍스처는 위가 1 이라 y 는 같은 부호
                float u = -(float)(xRem * pxPerRem) / t.width, v = (float)(yRem * pxPerRem) / t.height;
                g.uvRect = new Rect(u, v, hr.width / t.width, hr.height / t.height);
            }
            if (Rings != null) Rings.SetPhase((float)AgePatternRules.RingPhaseRem(A, Ms));
        }
    }

    /// <summary>타일 무늬 한 층 — `RawImage` 에 정본 마스크(`linear-gradient(90deg, transparent 0 30%, #000 50%)`)를 정점 알파로 얹은 것.</summary>
    public sealed class AgePatternGraphic : RawImage
    {
        float maskFrom = -1f, maskTo = -1f;
        public void SetMask(float from, float to) { maskFrom = from; maskTo = to; SetVerticesDirty(); }
        public bool Masked { get { return maskFrom >= 0f && maskTo > maskFrom; } }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (!Masked) { base.OnPopulateMesh(vh); return; }
            Rect r = GetPixelAdjustedRect();
            Rect uv = uvRect;
            vh.Clear();
            float[] xs = { 0f, maskFrom, maskTo, 1f };
            float[] al = { 0f, 0f, 1f, 1f };
            Color32 c = color;
            for (int i = 0; i < 4; i++)
            {
                float x = r.xMin + r.width * xs[i];
                float u = uv.xMin + uv.width * xs[i];
                Color32 cc = c; cc.a = (byte)Mathf.RoundToInt(c.a * al[i]);
                vh.AddVert(new Vector3(x, r.yMin), cc, new Vector2(u, uv.yMin));
                vh.AddVert(new Vector3(x, r.yMax), cc, new Vector2(u, uv.yMax));
            }
            for (int i = 0; i < 3; i++)
            {
                int b = i * 2;
                vh.AddTriangle(b, b + 1, b + 3);
                vh.AddTriangle(b, b + 3, b + 2);
            }
        }
    }

    /// <summary>양자 파문 — 정본 `repeating-radial-gradient(circle at 52% 50%, …)` 를 링 메시로(위상 `--gp-q` 가 커지면 링이 바깥으로 밀려난다 · 상자 크기가 곧 그라디언트 상자).</summary>
    public sealed class AgeRingsGraphic : MaskableGraphic
    {
        AgeSpec a; float pxPerRem, phaseRem, maskFrom = -1f, maskTo = -1f; int segments = 48;
        public float PhaseRem { get { return phaseRem; } }
        public int RingCount { get; private set; }

        public void Setup(AgeSpec spec, float pxPerRem, int segments, float mFrom, float mTo)
        {
            a = spec; this.pxPerRem = pxPerRem; this.segments = Mathf.Max(8, segments); maskFrom = mFrom; maskTo = mTo;
            SetVerticesDirty();
        }
        public void SetPhase(float rem) { if (Mathf.Abs(rem - phaseRem) < 1e-6f) return; phaseRem = rem; SetVerticesDirty(); }

        float MaskAlpha(float x01)
        {
            if (!(maskFrom >= 0f && maskTo > maskFrom)) return 1f;
            return Mathf.Clamp01((x01 - maskFrom) / (maskTo - maskFrom));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            RingCount = 0;
            if (a == null || a.Rings == null) return;
            Rect r = GetPixelAdjustedRect();
            float cx = r.xMin + r.width * (float)a.Rings.CenterF[0], cy = r.yMin + r.height * (float)a.Rings.CenterF[1];
            float dx = Mathf.Max(cx - r.xMin, r.xMax - cx), dy = Mathf.Max(cy - r.yMin, r.yMax - cy);
            float rmax = Mathf.Sqrt(dx * dx + dy * dy);
            float P = (float)a.Rings.PeriodRem * pxPerRem, on = (float)a.Rings.OnRem * pxPerRem, q = phaseRem * pxPerRem;
            if (!(P > 0f)) return;
            int kmin = Mathf.FloorToInt(-q / P) - 1, kmax = Mathf.CeilToInt((rmax - q) / P) + 1;
            Color32 c = color;
            for (int k = kmin; k <= kmax; k++)
            {
                float inner = q + k * P, outer = inner + on;
                if (outer <= 0f) continue;
                if (inner > rmax) break;
                inner = Mathf.Max(0f, inner);
                int b = vh.currentVertCount;
                for (int i = 0; i <= segments; i++)
                {
                    float ang = i / (float)segments * Mathf.PI * 2f, ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
                    Vector2 pi = new Vector2(cx + ca * inner, cy + sa * inner), po = new Vector2(cx + ca * outer, cy + sa * outer);
                    Color32 ci = c, co = c;
                    ci.a = (byte)Mathf.RoundToInt(c.a * MaskAlpha(Mathf.InverseLerp(r.xMin, r.xMax, pi.x)));
                    co.a = (byte)Mathf.RoundToInt(c.a * MaskAlpha(Mathf.InverseLerp(r.xMin, r.xMax, po.x)));
                    vh.AddVert(pi, ci, Vector2.zero);
                    vh.AddVert(po, co, Vector2.zero);
                }
                for (int i = 0; i < segments; i++)
                {
                    int v0 = b + i * 2;
                    vh.AddTriangle(v0, v0 + 1, v0 + 3);
                    vh.AddTriangle(v0, v0 + 3, v0 + 2);
                }
                RingCount++;
            }
        }
    }

    /// <summary>`Resources/AgePatternUi.json` 원문(수치는 <see cref="AgePatternSpec"/> 가 읽는다) + 색 파서.</summary>
    public static class AgePatternStyle
    {
        public const string ResourcePath = "AgePatternUi";
        static JsonObject root;
        public static JsonObject Root
        {
            get
            {
                if (root != null) return root;
                TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
                if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T124)");
                root = MiniJson.ParseObject(ta.text);
                return root;
            }
        }
        public static void Reset() { root = null; }
        public static Color Parse(string hex)
        {
            Color c;
            if (hex == null || !ColorUtility.TryParseHtmlString(hex, out c)) throw new FormatException("AgePatternUi 색 «" + hex + "» 을 못 읽는다");
            return c;
        }
    }
}
