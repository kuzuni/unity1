using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Forge.Core.Data;
using Forge.Game.Gallery;
using Forge.Game.Voxel;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 펫 슬롯 그림 = 실제 3D 펫(원작 `Scene3D.petThumb` · 사용자 지시 2026-08-18 «슬롯 아이콘 = 실제로 소환되는 그 펫»).
    /// T4 <c>VoxelMob</c> 으로 종을 세우고 T5 도감 리그(<see cref="GallerySheet"/> · 정면 3/4 · 위에서 · 경계 상자로 프레이밍)로 한 번 찍어 스프라이트로 캐시한다.
    /// 그래픽 장치가 없으면(CI headless) null — 호출자가 정본 PET_ICONS 이모지로 폴백한다(원작과 같은 순서).
    /// </summary>
    public static class PetFaces
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static GallerySheet.Rig rig;
        static GameObject stage;
        static readonly Vector3 Away = new Vector3(0f, -500f, 0f);

        public static bool Available { get { return GallerySheet.GraphicsAvailable && PetSkillHost.Instance != null && PetSkillHost.Instance.Data != null; } }

        public static Sprite Get(string name) { return Get(name, GalleryKind.Pets); }

        /// <summary>종 얼굴 — 펫은 `Pets` · 탈것은 `Mounts`(원작 `mountFace` = 같은 썸네일 파이프라인 · T20 탈것 화면).</summary>
        public static Sprite Get(string name, GalleryKind kind)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string key = kind + ":" + name;
            Sprite sp;
            if (cache.TryGetValue(key, out sp)) return sp;
            sp = Available ? Bake(name, kind) : null;
            cache[key] = sp;
            return sp;
        }

        public static void Reset()
        {
            cache.Clear();
            if (rig != null) { rig.Destroy(); rig = null; }
            if (stage != null) { Object.Destroy(stage); stage = null; }
        }

        static Sprite Bake(string name, GalleryKind kind)
        {
            GameData data = PetSkillHost.Instance.Data;
            GallerySpecies species = null;
            foreach (GallerySpecies s in MobGallery.SpeciesOf(data, kind)) if (s.Name == name) { species = s; break; }
            if (species == null) return null;
            // 씬이 다시 실리면(테스트 · 재시작) 리그·무대는 유니티 쪽에서 이미 죽어 있다 — C# 참조만 남았으면 새로 만든다.
            if (stage == null)
            {
                stage = new GameObject("PetFaces Stage");
                stage.transform.position = Away;
            }
            // 도감 리그(T5)는 전역 RenderSettings(안개·앰비언트)를 시트 규약으로 바꾼다 — 전투 씬의 테마 광원(T1)을 건드리면 안 되므로 찍는 동안만 바꾸고 되돌린다.
            bool fog = RenderSettings.fog;
            AmbientMode mode = RenderSettings.ambientMode;
            Color sky = RenderSettings.ambientSkyColor, eq = RenderSettings.ambientEquatorColor, ground = RenderSettings.ambientGroundColor;
            if (rig == null || rig.Root == null)
            {
                rig = GallerySheet.MakeRig(stage.transform);
                rig.Camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                rig.Key.enabled = false;
            }
            int px = Mathf.RoundToInt(PetSkillStyle.L("face_thumb_px"));
            GalleryEntry e = null;
            RenderTexture rt = null;
            RenderTexture prev = RenderTexture.active;
            Texture2D tex = null;
            try
            {
                e = MobGallery.Build(species, stage.transform);
                e.Root.transform.localPosition = Vector3.zero;
                rt = RenderTexture.GetTemporary(px, px, 16, RenderTextureFormat.ARGB32);
                RenderSettings.fog = false;
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = VoxelMaterials.ToColor(GallerySheet.HemiSky) * GallerySheet.HemiIntensity;
                RenderSettings.ambientGroundColor = VoxelMaterials.ToColor(GallerySheet.HemiGround) * GallerySheet.HemiIntensity;
                RenderSettings.ambientEquatorColor = (RenderSettings.ambientSkyColor + RenderSettings.ambientGroundColor) * 0.5f;
                rig.Key.enabled = true;
                rig.Camera.enabled = false;
                GallerySheet.RenderInto(rig.Camera, e.Root, GallerySheet.YawA, GallerySheet.Pad(kind), rt);
                RenderTexture.active = rt;
                tex = new Texture2D(px, px, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, px, px), 0, 0);
                // T332 2회차 — 정본은 펫·탈것 썸네일에도 슬롯과 **같은** 검정 아웃라인을 건다
                // (style.css 7663 `.pet-tile .tile-face .mt-face`·`.petd-tile .mt-face`·`.sk-mini .mt-face` · `--sw` 4방향 drop-shadow).
                // 굽는 길이 같으므로 자는 하나만 둔다 — 두께 환산에 쓰는 «칸 폭» 만 펫 타일 값으로 준다.
                ItemFaces.Outline(tex, ItemFacesStyle.L("pet_cell_canvas_px"));
                tex.Apply();
                tex.name = "petface:" + kind + ":" + name;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[PetFaces] " + name + " 썸네일 실패 — 이모지로: " + ex.Message);
                return null;
            }
            finally
            {
                rig.Key.enabled = false;
                RenderSettings.fog = fog;
                RenderSettings.ambientMode = mode;
                RenderSettings.ambientSkyColor = sky;
                RenderSettings.ambientEquatorColor = eq;
                RenderSettings.ambientGroundColor = ground;
                RenderTexture.active = prev;
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                if (e != null && e.Root != null) Object.Destroy(e.Root);
            }
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, px, px), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sp.name = "petface:" + kind + ":" + name;
            return sp;
        }
    }
}
