using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Forge.Game.Voxel;

namespace Forge.Game.Battle
{
    /// <summary>
    /// 전투 연출용 단색 재질(T8) — 원작 `MeshBasicMaterial({ color, transparent, opacity, toneMapped: false })` 의 대응. T4 의 `VoxelUnlit`(Particles/Unlit) 원형을 복제해
    /// `_BaseColor` 로 물들인다. 색은 정본 hex(HP 바·파편·불티)이고 이 파일은 «어떻게 그리나» 만 안다.
    /// </summary>
    public static class FxMaterials
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        static readonly int BlendId = Shader.PropertyToID("_Blend");
        static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly Dictionary<long, Material> shared = new Dictionary<long, Material>();
        static Material template;

        static Material Template()
        {
            if (template != null) return template;
            var res = Resources.Load<Material>(VoxelMaterials.UnlitResource);
            if (res != null) template = new Material(res);
            else
            {
                var sh = Shader.Find(VoxelMaterials.UnlitShader) ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                template = new Material(sh);
            }
            return template;
        }

        /// <summary>새 재질 인스턴스(호출자가 소유 · 색을 매 프레임 바꿔도 된다).</summary>
        public static Material Instance(int hex, double opacity = 1, Texture tex = null)
        {
            var m = new Material(Template());
            m.name = "Fx " + hex.ToString("x6");
            if (opacity < 1 || tex != null)
            {
                if (m.HasProperty(SurfaceId)) m.SetFloat(SurfaceId, 1);
                if (m.HasProperty(BlendId)) m.SetFloat(BlendId, 0);
                if (m.HasProperty(SrcBlendId)) m.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
                if (m.HasProperty(DstBlendId)) m.SetFloat(DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
                if (m.HasProperty(ZWriteId)) m.SetFloat(ZWriteId, 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            if (tex != null && m.HasProperty(BaseMapId)) m.SetTexture(BaseMapId, tex);
            SetColor(m, hex, opacity);
            return m;
        }

        /// <summary>같은 hex·불투명도를 나눠 쓰는 공유 재질(파편·불티처럼 개체가 많은 것).</summary>
        public static Material Shared(int hex, double opacity = 1)
        {
            long key = ((long)hex << 8) | (long)Mathf.RoundToInt((float)opacity * 255);
            Material m;
            if (shared.TryGetValue(key, out m) && m != null) return m;
            m = Instance(hex, opacity);
            shared[key] = m;
            return m;
        }

        public static void SetColor(Material m, int hex, double opacity = 1)
        {
            Color c = VoxelMaterials.ToColor(hex);
            c.a = (float)opacity;
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c);
            else m.color = c;
        }

        public static void SetColor(Material m, Color c)
        {
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c);
            else m.color = c;
        }

        public static Color GetColor(Material m)
        {
            return m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : m.color;
        }

        public static void ClearCache() { shared.Clear(); template = null; pool.Clear(); }

        // ===== 재질 풀(T74 · T50 `FxUnlitMaterials.Take/Release` 와 같은 꼴) =====
        // 정본 `fxGeo` 큐브는 시전마다 `MeshBasicMaterial` 을 새로 만들었다(관리 쓰레기 0 인 WebGL). 유니티는 Material 이 네이티브 오브젝트라 시전마다 만들고 버리면
        // 편집기 재질 후처리(52~59KB/프레임 · 런 113·118)와 GC 를 부른다 → 조합(투명 여부 · 가산)별로 되쓴다. 색·불투명도는 꺼낼 때 다시 칠한다 — 되쓰기는 새 시스템이 아니다.
        static readonly Dictionary<int, Stack<Material>> pool = new Dictionary<int, Stack<Material>>();
        /// <summary>풀에 쉬고 있는 재질 수.</summary>
        public static int Pooled { get { int n = 0; foreach (var kv in pool) n += kv.Value.Count; return n; } }
        /// <summary>풀이 새로 만든 재질 수(되쓰기가 새면 시전마다 는다).</summary>
        public static int PoolMade { get; private set; }

        public static int RecipeKey(bool transparent, bool additive) { return (transparent ? 1 : 0) | (additive ? 2 : 0); }

        /// <summary><see cref="Instance"/> 와 같은 결과를 풀에서 — 돌려줄 때는 <see cref="Release"/>(같은 키). additive = 원작 `AdditiveBlending`(SrcAlpha · One).</summary>
        public static Material Take(int hex, double opacity, bool additive, out int key)
        {
            bool transparent = opacity < 1 || additive;
            key = RecipeKey(transparent, additive);
            Stack<Material> st;
            Material m = null;
            if (pool.TryGetValue(key, out st))
                while (st.Count > 0) { Material p = st.Pop(); if (p != null) { m = p; break; } }
            if (m == null)
            {
                PoolMade++;
                m = Instance(hex, transparent ? Mathf.Min(0.999f, (float)opacity) : 1);
                if (additive)
                {
                    if (m.HasProperty(SrcBlendId)) m.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
                    if (m.HasProperty(DstBlendId)) m.SetFloat(DstBlendId, (float)BlendMode.One);
                }
            }
            SetColor(m, hex, opacity);
            return m;
        }

        public static void Release(Material m, int key)
        {
            if (m == null) return;
            Stack<Material> st;
            if (!pool.TryGetValue(key, out st)) pool[key] = st = new Stack<Material>();
            st.Push(m);
        }
    }
}
