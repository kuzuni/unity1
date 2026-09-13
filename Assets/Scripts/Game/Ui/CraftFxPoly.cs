using UnityEngine;
using UnityEngine.UI;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 정본 SVG 의 **각진 폴리곤 + 선형 그라디언트** 면을 코드로 그리는 그래픽(T87 6회차 · 외부 그림 0 · `PetHatchCone` 과 같은 길).
    ///
    /// 왜 둥근 사각(<see cref="UiShapes"/>)으로 안 되는가 — 정본 `ui.js` 의 빌릿 주석이 못 박았다:
    /// «🚨 모서리를 둥글리지 말 것. 처음엔 rx 2.4 라운드 바로 그렸는데 비평가 2인이 독립적으로 *달군 쇳덩이가 아니라 UI 배지·진행도 칩·젤리* 로 읽었다 —
    ///  네 모서리가 다 둥근 알약은 게임 UI 의 어휘다. 소재는 **모서리를 챔퍼로 깎은 각봉**이어야 한다.»
    ///
    /// 좌표는 자기 사각 안의 **정규 좌표**(0~1 · y 는 위에서 아래 = SVG 와 같은 방향)라 viewBox 를 통째로 덮는 칸에 얹으면 SVG 좌표를 132·86 으로 나눠 그대로 준다.
    /// 그라디언트는 SVG `linearGradient` 그대로 «축(<see cref="GradFrom"/>→<see cref="GradTo"/>) 위의 stop 들» 이고, 면을 가로 띠로 잘라 띠마다 색을 찍어 보간한다
    /// (정점만 칠하면 8각형의 정점 y 에서만 색이 꺾여 4-stop 램프가 뭉갠다).
    /// </summary>
    public sealed class CraftFxPoly : Graphic
    {
        /// <summary>테두리 정규 좌표(볼록·시계/반시계 무관 · y 는 위에서 아래).</summary>
        public Vector2[] Pts;
        /// <summary>그라디언트 색(하나면 단색).</summary>
        public Color[] Stops;
        /// <summary>색마다의 축 위 위치(0~1 · 오름차순 · <see cref="Stops"/> 와 길이가 같다).</summary>
        public float[] StopOffsets;
        /// <summary>그라디언트 축 시작(정규 좌표 · SVG `x1 y1`).</summary>
        public Vector2 GradFrom = Vector2.zero;
        /// <summary>그라디언트 축 끝(정규 좌표 · SVG `x2 y2`).</summary>
        public Vector2 GradTo = new Vector2(0f, 1f);
        /// <summary>가로 띠 수 — 많을수록 램프가 매끈하다(카탈로그 값).</summary>
        public int Bands = 24;

        private float opacity = 1f;

        /// <summary>
        /// SVG 원소 `opacity` — 정본은 이 값을 애니메이션한다(`ab-hot` .12↔1 · `ab-cool` 0→.82 · `ab-glow` .5↔.95).
        /// stop 의 `stop-opacity` 와 **곱해진다**(정본도 그렇다) · 값이 바뀔 때만 면을 다시 만든다.
        /// </summary>
        public float Opacity
        {
            get { return opacity; }
            set
            {
                float v = Mathf.Clamp01(value);
                if (Mathf.Approximately(v, opacity)) return;
                opacity = v;
                SetVerticesDirty();
            }
        }

        /// <summary>부모를 꽉 채우는 폴리곤 면 하나를 단다(입력은 안 받는다).</summary>
        public static CraftFxPoly Add(Transform parent, string name, Vector2[] pts, Color[] stops, float[] offsets, Vector2 gradFrom, Vector2 gradTo, int bands)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            UiKit.Fill(go.GetComponent<RectTransform>());
            CraftFxPoly p = go.AddComponent<CraftFxPoly>();
            p.Pts = pts;
            p.Stops = stops;
            p.StopOffsets = offsets;
            p.GradFrom = gradFrom;
            p.GradTo = gradTo;
            p.Bands = Mathf.Max(1, bands);
            p.raycastTarget = false;
            return p;
        }

        /// <summary>단색 면(테두리·접합선처럼 stop 이 하나인 것).</summary>
        public static CraftFxPoly Add(Transform parent, string name, Vector2[] pts, Color flat)
        {
            return Add(parent, name, pts, new Color[] { flat }, new float[] { 0f }, Vector2.zero, new Vector2(0f, 1f), 1);
        }

        /// <summary>축 위 위치 t(0~1)의 색 — stop 사이는 선형(SVG 와 같다).</summary>
        public Color Sample(float t)
        {
            if (Stops == null || Stops.Length == 0) return Color.white;
            if (Stops.Length == 1) return Stops[0];
            if (t <= StopOffsets[0]) return Stops[0];
            int n = Stops.Length;
            if (t >= StopOffsets[n - 1]) return Stops[n - 1];
            int hi = 1;
            while (hi < n && StopOffsets[hi] < t) hi++;
            int lo = hi - 1;
            float span = StopOffsets[hi] - StopOffsets[lo];
            float f = span <= 0f ? 1f : (t - StopOffsets[lo]) / span;
            return Color.Lerp(Stops[lo], Stops[hi], f);
        }

        /// <summary>테두리를 무게중심 기준으로 부풀린 사본 — 정본 `stroke` 자리(모루 `Outlined` 와 같은 길: 큰 면을 뒤에 깐다).</summary>
        public static Vector2[] Inflate(Vector2[] pts, float dx, float dy)
        {
            if (pts == null || pts.Length == 0) return pts;
            Vector2 c = Vector2.zero;
            for (int i = 0; i < pts.Length; i++) c += pts[i];
            c /= pts.Length;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < pts.Length; i++)
            {
                if (pts[i].x < minX) minX = pts[i].x;
                if (pts[i].x > maxX) maxX = pts[i].x;
                if (pts[i].y < minY) minY = pts[i].y;
                if (pts[i].y > maxY) maxY = pts[i].y;
            }
            float w = Mathf.Max(1e-6f, maxX - minX), h = Mathf.Max(1e-6f, maxY - minY);
            float sx = 1f + dx * 2f / w, sy = 1f + dy * 2f / h;
            Vector2[] outPts = new Vector2[pts.Length];
            for (int i = 0; i < pts.Length; i++)
            {
                outPts[i] = new Vector2(c.x + (pts[i].x - c.x) * sx, c.y + (pts[i].y - c.y) * sy);
            }
            return outPts;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Pts == null || Pts.Length < 3) return;
            Rect r = GetPixelAdjustedRect();
            float top = float.MaxValue, bot = float.MinValue;
            for (int i = 0; i < Pts.Length; i++)
            {
                if (Pts[i].y < top) top = Pts[i].y;
                if (Pts[i].y > bot) bot = Pts[i].y;
            }
            if (bot - top <= 0f) return;

            int bands = Mathf.Max(1, Bands);
            float prevL = 0f, prevR = 0f, prevY = 0f;
            bool have = false;
            for (int b = 0; b <= bands; b++)
            {
                float y = Mathf.Lerp(top, bot, (float)b / bands);
                float l, rr;
                if (!Span(y, top, bot, out l, out rr)) { have = false; continue; }
                if (have)
                {
                    Quad(vh, r, prevL, prevR, prevY, l, rr, y);
                }
                prevL = l; prevR = rr; prevY = y; have = true;
            }
        }

        /// <summary>가로선 y 가 자르는 면의 왼쪽·오른쪽 x(정규 좌표). 자르지 못하면 false.</summary>
        private bool Span(float y, float top, float bot, out float left, out float right)
        {
            left = float.MaxValue; right = float.MinValue;
            // 맨 위·아래 선은 꼭짓점과 정확히 겹쳐 교차가 0 이 될 수 있어 안쪽으로 살짝 당긴다.
            float eps = (bot - top) * 1e-4f;
            float yy = Mathf.Clamp(y, top + eps, bot - eps);
            int n = Pts.Length;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = Pts[i], c = Pts[(i + 1) % n];
                if ((a.y <= yy && c.y >= yy) || (c.y <= yy && a.y >= yy))
                {
                    float d = c.y - a.y;
                    float x = Mathf.Abs(d) < 1e-9f ? a.x : a.x + (c.x - a.x) * (yy - a.y) / d;
                    if (x < left) left = x;
                    if (x > right) right = x;
                }
            }
            return right >= left;
        }

        private void Quad(VertexHelper vh, Rect r, float l0, float r0, float y0, float l1, float r1, float y1)
        {
            int i0 = vh.currentVertCount;
            Add(vh, r, l0, y0);
            Add(vh, r, r0, y0);
            Add(vh, r, r1, y1);
            Add(vh, r, l1, y1);
            vh.AddTriangle(i0, i0 + 1, i0 + 2);
            vh.AddTriangle(i0 + 2, i0 + 3, i0);
        }

        private void Add(VertexHelper vh, Rect r, float nx, float ny)
        {
            // 정규 좌표(y 아래로) → 사각 안 좌표(y 위로)
            Vector2 pos = new Vector2(r.x + r.width * nx, r.yMax - r.height * ny);
            Color c = Sample(Project(nx, ny));
            c.a *= opacity;
            vh.AddVert(pos, c, new Vector2(0.5f, 0.5f));
        }

        /// <summary>정규 좌표를 그라디언트 축에 투영한 t(0~1) — SVG `linearGradient x1y1→x2y2` 와 같은 뜻.</summary>
        private float Project(float nx, float ny)
        {
            Vector2 axis = GradTo - GradFrom;
            float len2 = axis.sqrMagnitude;
            if (len2 <= 1e-9f) return 0f;
            return Mathf.Clamp01(Vector2.Dot(new Vector2(nx, ny) - GradFrom, axis) / len2);
        }
    }
}
