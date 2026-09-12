using UnityEngine;
using UnityEngine.Rendering;
using Forge.Game.Hero;

namespace Forge.Game.Battle
{
    /// <summary>
    /// 접지 블롭 섀도우 — 원작 `makeBlobTexture`(16² 격자 계단 · NearestFilter · 알파 대역 1/.72/.36/.12) + `ensureBlobRes`(검정 · 불투명도 0.22/0.26/0.45 · depthWrite 끔) 의 대응.
    /// 지면 위 y 0.03 에 눕힌 1×1 판(scale = 반지름 배율). 화풍(하드 엣지 · 셀 양자화)을 지킨다.
    /// </summary>
    public static class BlobShadow
    {
        public const int N = 16;
        public const double Y = 0.03;
        static readonly double[][] Step = { new[] { 0.30, 1.0 }, new[] { 0.50, 0.72 }, new[] { 0.70, 0.36 }, new[] { 0.88, 0.12 } };
        static Texture2D tex;
        static Mesh quad;

        public static Texture2D Texture
        {
            get
            {
                if (tex != null) return tex;
                tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
                tex.name = "blob16";
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color32[N * N];
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        double dx = (x + 0.5) / N * 2 - 1, dy = (y + 0.5) / N * 2 - 1;
                        double d = System.Math.Sqrt(dx * dx + dy * dy);
                        double a = 0;
                        for (int i = 0; i < Step.Length; i++) if (d <= Step[i][0]) { a = Step[i][1]; break; }
                        px[y * N + x] = new Color32(255, 255, 255, (byte)System.Math.Round(a * 255));
                    }
                tex.SetPixels32(px);
                tex.Apply(false, false);
                return tex;
            }
        }

        /// <summary>검정 블롭 하나(부모 아래 · 로컬 원점이 발밑). 재질은 개체 소유(불투명도를 따로 바꾼다).</summary>
        public static MeshRenderer Create(Transform parent, string name, double opacity)
        {
            if (quad == null) quad = HeroMeshes.Quad(1, 1, 0xffffff);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, (float)Y, 0);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // −z 를 보던 판을 위(+y)로 돌린다(Rx(+90): −z → +y)
            go.AddComponent<MeshFilter>().sharedMesh = quad;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = FxMaterials.Instance(0x000000, opacity, Texture);
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return mr;
        }
    }
}
