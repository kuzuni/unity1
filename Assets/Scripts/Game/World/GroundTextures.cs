using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Forge.Core.Data;
using Forge.Core.World;
using Forge.Game.Voxel;

namespace Forge.Game.Map
{
    /// <summary>
    /// 지면 소재(T34 · 정본 `groundTexFor`·`makeCrackTexture`·포석 줄눈 데칼)를 굽고 지면 재질에 붙인다. 굽기는 Core `GroundTexBake`
    /// (정본 실행 벡터와 바이트 해시까지 같다) · 여기는 Texture2D 로 옮기고 재질 칸에 꽂는 것뿐. 바이옴마다 한 번 굽고 캐시(정본 `_gtex`).
    /// 시드는 T2 표본과 같은 xorshift32 `LibSeed` 라 게임 안 텍스처 = 벡터의 그림이다(정본은 Math.random 이라 매번 다르다 — 결정 기록).
    /// 재질은 T9 가 세운 `Particles/Lit`: `_BaseMap`(12×6 반복) · `_BumpMap`(정본 normalScale 0.7) · 용암은 `_EmissionMap` = 발광 균열.
    /// 정본 `terrainShade`(매크로·거리 LOD·눈 탈색·uRoad)는 커스텀 셰이더 몫이라 다음 갈래(T34 완료 기록)로 남긴다.
    /// </summary>
    public static class GroundTextures
    {
        public const string CobbleName = "pathMesh";
        /// <summary>정본 `normalScale: new THREE.Vector2(0.7, 0.7)`.</summary>
        public const float NormalScale = 0.7f;

        sealed class Set { public Texture2D Map, Normal; }
        static readonly Dictionary<string, Set> cache = new Dictionary<string, Set>();
        static List<CrackSeg> crackNet;
        static Texture2D crackTex;
        static Texture2D cobbleTex;

        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int BumpMapId = Shader.PropertyToID("_BumpMap");
        static readonly int BumpScaleId = Shader.PropertyToID("_BumpScale");
        static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        static readonly int BlendId = Shader.PropertyToID("_Blend");
        static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

        static Rng Seeded() { return Rng.Xorshift(Xorshift32.LibSeed); }

        /// <summary>정본 `crackNetwork()` — 한 번만 · 알베도(용암)·노멀(용암)·발광이 공유.</summary>
        public static List<CrackSeg> CrackNet { get { return crackNet ?? (crackNet = GroundTexBake.CrackNetwork(Seeded())); } }

        /// <summary>RGBA 바이트(캔버스 · 위가 0행) → Texture2D. 정본 three 는 `flipY` 라 uv(0,0) 이 그림 아래 — 유니티도 아래가 0행이라 행을 뒤집어 싣는다.</summary>
        public static Texture2D ToTexture(byte[] rgba, int w, int h, bool linear, string name)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true, linear);
            tex.name = name;
            var flipped = new byte[rgba.Length];
            int row = w * 4;
            for (int y = 0; y < h; y++) System.Buffer.BlockCopy(rgba, y * row, flipped, (h - 1 - y) * row, row);
            tex.LoadRawTextureData(flipped);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply(true, false);
            return tex;
        }

        /// <summary>정본 `groundTexFor(biome)` — 바이옴마다 알베도+노멀 한 번.</summary>
        static Set For(SceneDefs defs, string biome)
        {
            Set s;
            if (cache.TryGetValue(biome, out s) && s.Map != null && s.Normal != null) return s;
            var bake = new GroundTexBake(defs);
            s = new Set
            {
                Map = ToTexture(bake.Albedo(biome, CrackNet, Seeded()), GroundTexBake.AlbedoSize, GroundTexBake.AlbedoSize, false, "ground albedo " + biome),
                Normal = ToTexture(bake.NormalMap(biome, CrackNet, Seeded()), GroundTexBake.NormalSize, GroundTexBake.NormalSize, true, "ground normal " + biome)
            };
            cache[biome] = s;
            return s;
        }

        static Texture2D Crack(SceneDefs defs)
        {
            if (crackTex != null) return crackTex;
            crackTex = ToTexture(new GroundTexBake(defs).CrackTexture(CrackNet), GroundTexBake.CrackSize, GroundTexBake.CrackSize, false, "ground crack emissive");
            return crackTex;
        }

        /// <summary>정본 `buildProps` 의 지면 소재 스왑 + `setTheme` 의 emissiveMap 갈래 — 테마를 입힐 때마다.</summary>
        public static void Apply(Material terrainMat, SceneDefs defs, ThemeLook L)
        {
            if (terrainMat == null || defs == null || L == null) return;
            Set gt = For(defs, L.Biome);
            var repeat = new Vector2((float)GroundTexBake.RepeatX, (float)GroundTexBake.RepeatY);
            if (terrainMat.HasProperty(BaseMapId)) { terrainMat.SetTexture(BaseMapId, gt.Map); terrainMat.SetTextureScale(BaseMapId, repeat); }
            else terrainMat.mainTexture = gt.Map;
            if (terrainMat.HasProperty(BumpMapId))
            {
                terrainMat.SetTexture(BumpMapId, gt.Normal);
                terrainMat.SetTextureScale(BumpMapId, repeat);
                if (terrainMat.HasProperty(BumpScaleId)) terrainMat.SetFloat(BumpScaleId, NormalScale);
                terrainMat.EnableKeyword("_NORMALMAP");
            }
            if (terrainMat.HasProperty(EmissionMapId))
            {
                if (L.CrackMap)
                {
                    terrainMat.SetTexture(EmissionMapId, Crack(defs));
                    terrainMat.SetTextureScale(EmissionMapId, repeat);   // three r128 은 map 의 uvTransform 을 emissiveMap 에도 건다(정본 주석)
                    Color e = new Color((float)L.TerrainEmissive.R, (float)L.TerrainEmissive.G, (float)L.TerrainEmissive.B, 1f) * (float)L.TerrainEmissiveIntensity;
                    terrainMat.SetColor(EmissionColorId, e);
                    terrainMat.EnableKeyword("_EMISSION");
                    terrainMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                else
                {
                    terrainMat.SetTexture(EmissionMapId, null);
                    terrainMat.SetColor(EmissionColorId, Color.black);
                    terrainMat.DisableKeyword("_EMISSION");
                }
            }
        }

        /// <summary>정본 포석 줄눈 데칼 — `ground` 의 자식(타일 순환을 따라간다) · 60 × 4.70 평면 · y 0.02 · 투명 · x 반복 2 · 먼저 그린다(renderOrder −1).</summary>
        public static GameObject AttachCobble(Transform ground, SceneDefs defs)
        {
            if (cobbleTex == null)
            {
                cobbleTex = ToTexture(GroundTexBake.CobbleDecal(Seeded()), GroundTexBake.CobbleWidth, GroundTexBake.CobbleHeight, false, "ground cobble joints");
                cobbleTex.wrapMode = TextureWrapMode.Repeat;
                cobbleTex.anisoLevel = 16;
            }
            var go = new GameObject(CobbleName);
            go.transform.SetParent(ground, false);
            go.transform.localPosition = new Vector3(0f, (float)GroundTexBake.CobbleY, 0f);
            float hw = (float)(GroundTexBake.CobblePlaneWidth / 2), hd = (float)(GroundTexBake.CobblePlaneDepth / 2);
            var mesh = new Mesh { name = "pathGeo" };
            // 정본 PlaneGeometry(60, 4.70).rotateX(−π/2): 평면의 +y(v=1) 가 −z(three) = +z(유니티 · z 반전) 쪽.
            mesh.SetVertices(new List<Vector3> { new Vector3(-hw, 0, -hd), new Vector3(hw, 0, -hd), new Vector3(-hw, 0, hd), new Vector3(hw, 0, hd) });
            mesh.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) });
            mesh.SetNormals(new List<Vector3> { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
            mesh.SetTriangles(new[] { 0, 2, 1, 2, 3, 1 }, 0);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var m = MakeDecalMaterial();
            m.SetTexture(BaseMapId, cobbleTex);
            m.SetTextureScale(BaseMapId, new Vector2((float)GroundTexBake.CobbleRepeatX, 1f));
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = m;
            mr.receiveShadows = true;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        static Material MakeDecalMaterial()
        {
            var res = Resources.Load<Material>(VoxelMaterials.UnlitResource);
            Material m = res != null ? new Material(res) : new Material(Shader.Find(VoxelMaterials.UnlitShader) ?? Shader.Find("Universal Render Pipeline/Unlit"));
            m.name = "pathMat";
            // 정본 MeshLambertMaterial{transparent, depthWrite:false} · renderOrder −1 — URP 파티클 셰이더의 알파 블렌드 칸(VoxelMaterials 와 같은 규약)
            if (m.HasProperty(SurfaceId)) m.SetFloat(SurfaceId, 1);
            if (m.HasProperty(BlendId)) m.SetFloat(BlendId, 0);
            if (m.HasProperty(SrcBlendId)) m.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
            if (m.HasProperty(DstBlendId)) m.SetFloat(DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty(ZWriteId)) m.SetFloat(ZWriteId, 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent - 1;
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, Color.white);
            return m;
        }

        /// <summary>테스트·재시작용 — 구운 것을 버린다(다음 Apply 가 다시 굽는다).</summary>
        public static void ClearCache()
        {
            cache.Clear(); crackNet = null; crackTex = null; cobbleTex = null;
        }
    }
}
