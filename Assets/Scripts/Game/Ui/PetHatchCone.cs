using UnityEngine;
using UnityEngine.UI;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 정점 색 사다리꼴/삼각 그래픽 — 원작 부화장의 램프 빛기둥(`.hatch-cone` · clip-path polygon(38% 0, 62% 0, 100% 100%, 0 100%) + 위→아래 알파 그라데이션)과
    /// «장착됨» 라벨의 오른쪽 홈(clip-path 화살). 코드 생성 도형(T18 UiShapes 와 같은 길 · 외부 그림 없음).
    /// </summary>
    public sealed class PetHatchCone : Graphic
    {
        public enum Kind { Cone, NotchRight }

        public Kind Shape = Kind.Cone;
        public Color Top = Color.white;
        public Color Bottom = Color.white;
        /// <summary>위 변의 폭 비율(원작 38%~62% → 0.24).</summary>
        public float TopFrac = 0.24f;

        public static PetHatchCone Add(Transform parent, string name, Color top, Color bottom, float topFrac, float dummy)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<PetHatchCone>();
            c.Top = top;
            c.Bottom = bottom;
            c.TopFrac = topFrac;
            c.raycastTarget = false;
            return c;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            if (Shape == Kind.Cone)
            {
                float half = r.width * TopFrac * 0.5f;
                float cx = r.x + r.width * 0.5f;
                vh.AddVert(new Vector3(r.xMin, r.yMin), Bottom, Vector2.zero);
                vh.AddVert(new Vector3(cx - half, r.yMax), Top, Vector2.zero);
                vh.AddVert(new Vector3(cx + half, r.yMax), Top, Vector2.zero);
                vh.AddVert(new Vector3(r.xMax, r.yMin), Bottom, Vector2.zero);
                vh.AddTriangle(0, 1, 2);
                vh.AddTriangle(0, 2, 3);
            }
            else
            {
                // 오른쪽 홈: 오른쪽 위·아래 모서리에서 가운데로 파고드는 삼각(바탕색으로 채워 라벨을 «화살» 꼴로)
                vh.AddVert(new Vector3(r.xMax, r.yMax), Top, Vector2.zero);
                vh.AddVert(new Vector3(r.xMin, r.y + r.height * 0.5f), Top, Vector2.zero);
                vh.AddVert(new Vector3(r.xMax, r.yMin), Top, Vector2.zero);
                vh.AddTriangle(0, 1, 2);
            }
        }
    }
}
