using System.Collections.Generic;
using UnityEngine;
using Forge.Core.Data;
using Forge.Game.Voxel;

namespace Forge.Game.Hero
{
    /// <summary>
    /// 영웅 박스 리그의 메시·재질 — 정본 `BoxGeometry`(살색 박스 · flatShading) 와 `PlaneGeometry`(얼굴 디캘 · MeshBasic) 의 대응.
    /// 정점 색 메시(T4 와 같은 재질 계약 · `VoxelMaterials` 의 Particles/Lit·Unlit) · 면마다 법선을 따로 둬 플랫 셰이딩. 유니티 왼손 좌표로 직접 굽는다(박스는 대칭 · 디캘은 −z 를 본다 = three 의 +z 앞면).
    /// </summary>
    public static class HeroMeshes
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        /// <summary>정점 색이 있는 축정렬 박스(w × h × d · 원점 중심). 같은 치수·색은 메시를 공유한다.</summary>
        public static Mesh Box(double w, double h, double d, int hex)
        {
            string key = "box|" + w + "|" + h + "|" + d + "|" + hex;
            Mesh m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            var b = new Builder(hex);
            float hw = (float)w / 2, hh = (float)h / 2, hd = (float)d / 2;
            b.Face(new Vector3(hw, 0, 0), new Vector3(0, 0, hd), new Vector3(0, hh, 0));
            b.Face(new Vector3(-hw, 0, 0), new Vector3(0, 0, hd), new Vector3(0, hh, 0));
            b.Face(new Vector3(0, hh, 0), new Vector3(hw, 0, 0), new Vector3(0, 0, hd));
            b.Face(new Vector3(0, -hh, 0), new Vector3(hw, 0, 0), new Vector3(0, 0, hd));
            b.Face(new Vector3(0, 0, hd), new Vector3(hw, 0, 0), new Vector3(0, hh, 0));
            b.Face(new Vector3(0, 0, -hd), new Vector3(hw, 0, 0), new Vector3(0, hh, 0));
            m = b.Build("HeroBox " + w + "x" + h + "x" + d);
            cache[key] = m;
            return m;
        }

        /// <summary>얼굴 디캘 판(w × h · 원점 중심 · 법선 −z = 정본 PlaneGeometry 의 +z 앞면을 z 반전한 것).</summary>
        public static Mesh Quad(double w, double h, int hex)
        {
            string key = "quad|" + w + "|" + h + "|" + hex;
            Mesh m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            var b = new Builder(hex);
            b.Face(Vector3.zero, new Vector3((float)w / 2, 0, 0), new Vector3(0, (float)h / 2, 0), new Vector3(0, 0, -1));
            m = b.Build("HeroQuad " + w + "x" + h);
            cache[key] = m;
            return m;
        }

        public static void ClearCache() { cache.Clear(); }

        static MobMat basicMat;
        /// <summary>살색 박스 재질(정본 `MeshStandardMaterial{roughness 0.62, flatShading}` · 색은 정점) — 전역 캐시 키 `hero|skin|<rough>`(VoxelMaterials 가 키로 공유).</summary>
        public static Material Skin(double rough)
        {
            return VoxelMaterials.Get("hero|skin|" + rough, new MobMat { Rough = rough });
        }

        /// <summary>디캘 재질(정본 `MeshBasicMaterial` · 조명 없음) — 키 `hero|basic`.</summary>
        public static Material Basic()
        {
            if (basicMat == null)
            {
                var raw = new JsonObject();
                raw["basic"] = true;
                basicMat = new MobMat { Raw = raw };
            }
            return VoxelMaterials.Get("hero|basic", basicMat);
        }

        sealed class Builder
        {
            readonly List<Vector3> verts = new List<Vector3>();
            readonly List<Vector3> norms = new List<Vector3>();
            readonly List<Color> cols = new List<Color>();
            readonly List<int> tris = new List<int>();
            readonly Color color;

            public Builder(int hex)
            {
                var c = VoxelMaterials.ToColor(hex);
                color = QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c;
            }

            /// <summary>중심 c · 바깥 법선 n(u·v 에 수직) 의 사각 면. 감김은 유니티 앞면(법선 = −cross(u, v) 규칙으로 u·v 를 고른다).</summary>
            public void Face(Vector3 c, Vector3 u, Vector3 v)
            {
                Vector3 n = Vector3.Cross(u, v).normalized;
                // 바깥쪽이 원점 반대편이 되게 — 중심 방향과 같은 부호로
                if (Vector3.Dot(n, c) < 0) n = -n;
                Face(c, u, v, n);
            }

            public void Face(Vector3 c, Vector3 u, Vector3 v, Vector3 n)
            {
                // 삼각형 (0,2,1)·(0,3,2) 의 법선은 −cross(u, v). n 과 맞지 않으면 u·v 를 바꿔 감김을 뒤집는다.
                if (Vector3.Dot(-Vector3.Cross(u, v), n) < 0) { var t = u; u = v; v = t; }
                int b = verts.Count;
                verts.Add(c - u - v); verts.Add(c + u - v); verts.Add(c + u + v); verts.Add(c - u + v);
                for (int i = 0; i < 4; i++) { norms.Add(n); cols.Add(color); }
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
                tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
            }

            public Mesh Build(string name)
            {
                var m = new Mesh { name = name };
                m.SetVertices(verts);
                m.SetNormals(norms);
                m.SetColors(cols);
                m.SetTriangles(tris, 0);
                m.RecalculateBounds();
                return m;
            }
        }
    }
}
