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

        public static void ClearCache() { shared.Clear(); template = null; }
    }
}
