using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Forge.Core.Data;
using Forge.Core.Voxel;

namespace Forge.Game.Voxel
{
    /// <summary>
    /// 정본 `mobs.js` `makeMat`/`matKey` — 성질(basic · opacity · emissive · emissiveIntensity · rough)이 같은 파츠끼리 Material 하나를 공유한다.
    /// 색은 정점에 굽으므로 재질 색은 항상 흰색. 정본은 `MeshStandardMaterial{vertexColors, flatShading, metalness 0, roughness 0.9}` /
    /// `MeshBasicMaterial{vertexColors}` — URP 에서 정점 색을 곱하는 내장 셰이더는 `Universal Render Pipeline/Particles/Lit`·`Particles/Unlit`
    /// 이라 그것을 쓴다(URP `Lit` 은 정점 색을 무시한다 · 결정 기록). 원형 재질은 `Assets/Forge/Resources/VoxelLit.mat`·`VoxelUnlit.mat`
    /// (빌드에 셰이더가 실리게 하는 자리) · 없으면 `Shader.Find` 로 만든다. 캐시는 전역(같은 키 = 같은 Material · 배칭).
    /// </summary>
    public static class VoxelMaterials
    {
        public const string LitShader = "Universal Render Pipeline/Particles/Lit";
        public const string UnlitShader = "Universal Render Pipeline/Particles/Unlit";
        public const string LitResource = "VoxelLit";
        public const string UnlitResource = "VoxelUnlit";

        /// <summary>정본 기본값: roughness 0.9 · emissiveIntensity 0.7.</summary>
        public const double DefaultRough = 0.9;
        public const double DefaultEmissiveIntensity = 0.7;

        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        static readonly int BlendId = Shader.PropertyToID("_Blend");
        static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

        /// <summary>키(<see cref="MobBuilder.MatKey"/>)에 해당하는 공유 Material. 처음 보는 키면 만든다.</summary>
        public static Material Get(string key, MobMat mat)
        {
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = Make(key, mat);
            cache[key] = m;
            return m;
        }

        public static void ClearCache() { cache.Clear(); }

        static Material Template(bool basic)
        {
            var res = Resources.Load<Material>(basic ? UnlitResource : LitResource);
            if (res != null) return new Material(res);
            var sh = Shader.Find(basic ? UnlitShader : LitShader);
            if (sh == null)
            {
                Debug.LogWarning("VoxelMaterials: 셰이더를 못 찾았다 — " + (basic ? UnlitShader : LitShader) + " (Resources/" + (basic ? UnlitResource : LitResource) + ".mat 도 없음)");
                sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            }
            return new Material(sh);
        }

        static Material Make(string key, MobMat mat)
        {
            bool basic = MobBuilder.IsBasic(mat);
            var m = Template(basic);
            m.name = "Voxel " + key;
            double opacity = mat != null && mat.Opacity.HasValue ? mat.Opacity.Value : 1;
            var baseColor = Color.white;
            if (m.HasProperty(MetallicId)) m.SetFloat(MetallicId, 0);
            if (!basic && m.HasProperty(SmoothnessId))
            {
                double rough = mat != null && mat.Rough.HasValue ? mat.Rough.Value : DefaultRough;
                m.SetFloat(SmoothnessId, (float)(1 - rough));
            }
            if (!basic && mat != null && mat.Emissive.HasValue && m.HasProperty(EmissionColorId))
            {
                double k = mat.EmissiveIntensity.HasValue ? mat.EmissiveIntensity.Value : DefaultEmissiveIntensity;
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.SetColor(EmissionColorId, ToColor(mat.Emissive.Value) * (float)k);
            }
            if (opacity < 1)
            {
                // 정본: transparent · opacity · depthWrite false. URP 파티클 셰이더의 알파 블렌드 칸을 직접 준다.
                baseColor.a = (float)opacity;
                if (m.HasProperty(SurfaceId)) m.SetFloat(SurfaceId, 1);
                if (m.HasProperty(BlendId)) m.SetFloat(BlendId, 0);
                if (m.HasProperty(SrcBlendId)) m.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
                if (m.HasProperty(DstBlendId)) m.SetFloat(DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
                if (m.HasProperty(ZWriteId)) m.SetFloat(ZWriteId, 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, baseColor);
            else m.color = baseColor;
            return m;
        }

        /// <summary>0xRRGGBB → 유니티 sRGB 색 칸 값(결정 4 · hex 를 그대로).</summary>
        public static Color ToColor(int hex)
        {
            return new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f, 1f);
        }
    }
}
