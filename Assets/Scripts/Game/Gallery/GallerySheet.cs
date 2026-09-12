using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using Forge.Game.Voxel;

namespace Forge.Game.Gallery
{
    /// <summary>
    /// 종 시트 촬영(T5) — 원작 `web/tools/mobsheet.html renderMob` + `shot-mobs.js` 를 그대로: 종마다 정사각 렌더 2장(yaw 0.62 · π/2) ·
    /// 원근 28° · 카메라 = 중심 + d·(0.32, 0.34, 1)(three) · d = (최대 치수 × 2.5 + 0.2) × pad · 키 라이트(2.4, 4, 3) 흰색 1.15 · 반구광(0xdfe8f5 · 0x3c3a35 · 0.75) ·
    /// 시트 배경 #191d22 · 칸 배경 #23272e · 4열. 결과는 `ui-screens/&lt;이름&gt;.png`(CI 가 `screens` 브랜치로 올린다 · ROUTINE §5).
    /// </summary>
    public static class GallerySheet
    {
        public const float Fov = 28f;
        public const float Near = 0.01f, Far = 40f;
        public const float YawA = 0.62f, YawB = Mathf.PI / 2f;
        public const float CamX = 0.32f, CamY = 0.34f, CamZ = 1f;
        public const float FitScale = 2.5f, FitAdd = 0.2f;
        public const float KeyIntensity = 1.15f, HemiIntensity = 0.75f;
        public static readonly Vector3 KeyPosThree = new Vector3(2.4f, 4f, 3f);
        public const int HemiSky = 0xdfe8f5, HemiGround = 0x3c3a35;
        public const int SheetBg = 0x191d22, CellBg = 0x23272e;
        public const int Gap = 8;
        public const string OutDir = "ui-screens";

        /// <summary>원작 `SHEET` 표: pets/mounts/enemies 260px · skillfx 340px pad 1.14. 소품은 pets 와 같게.</summary>
        public static int RenderSize(GalleryKind kind) { return kind == GalleryKind.SkillFx ? 340 : 260; }
        public static float Pad(GalleryKind kind) { return kind == GalleryKind.SkillFx ? 1.14f : 1f; }

        public static bool GraphicsAvailable { get { return SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null; } }

        /// <summary>촬영 리그(카메라 + 키 라이트 + 반구 앰비언트). 부모 아래에 만들며 씬 안개는 끈다(시트는 중립 배경).</summary>
        public sealed class Rig
        {
            public GameObject Root;
            public Camera Camera;
            public Light Key;

            public void Destroy() { if (Root != null) Object.Destroy(Root); }
        }

        public static Rig MakeRig(Transform parent)
        {
            var rig = new Rig();
            rig.Root = new GameObject("Gallery Sheet Rig");
            if (parent != null) rig.Root.transform.SetParent(parent, false);
            var camGo = new GameObject("Sheet Camera");
            camGo.transform.SetParent(rig.Root.transform, false);
            rig.Camera = camGo.AddComponent<Camera>();
            rig.Camera.fieldOfView = Fov;
            rig.Camera.nearClipPlane = Near;
            rig.Camera.farClipPlane = Far;
            rig.Camera.clearFlags = CameraClearFlags.SolidColor;
            rig.Camera.backgroundColor = VoxelMaterials.ToColor(CellBg);
            rig.Camera.enabled = false;
            var keyGo = new GameObject("Sheet Key Light");
            keyGo.transform.SetParent(rig.Root.transform, false);
            rig.Key = keyGo.AddComponent<Light>();
            rig.Key.type = LightType.Directional;
            rig.Key.color = Color.white;
            rig.Key.intensity = KeyIntensity;
            rig.Key.shadows = LightShadows.None;
            Vector3 keyPos = ThreeSpace.Pos(KeyPosThree.x, KeyPosThree.y, KeyPosThree.z);
            keyGo.transform.position = keyPos;
            keyGo.transform.rotation = Quaternion.LookRotation(-keyPos.normalized, Vector3.up);
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            Color sky = VoxelMaterials.ToColor(HemiSky) * HemiIntensity;
            Color ground = VoxelMaterials.ToColor(HemiGround) * HemiIntensity;
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientGroundColor = ground;
            RenderSettings.ambientEquatorColor = (sky + ground) * 0.5f;
            return rig;
        }

        /// <summary>원작 `renderMob`: 그룹을 yaw 만큼 돌린 뒤 경계 상자로 카메라를 맞춘다(three yaw → 유니티 −yaw · 결정 4).</summary>
        public static void Frame(Camera cam, GameObject root, float yawThree, float pad)
        {
            root.transform.rotation = Quaternion.AngleAxis(-yawThree * Mathf.Rad2Deg, Vector3.up);
            Bounds b = MobGallery.WorldBounds(root);
            Vector3 c = b.center;
            float d = (Mathf.Max(b.size.x, b.size.y, b.size.z) * FitScale + FitAdd) * pad;
            cam.transform.position = c + new Vector3(d * CamX, d * CamY, -d * CamZ);
            cam.transform.LookAt(c, Vector3.up);
        }

        /// <summary>종 하나를 rt 에 찍는다(다른 종은 호출자가 꺼 둔다).</summary>
        public static void RenderInto(Camera cam, GameObject root, float yawThree, float pad, RenderTexture rt)
        {
            Frame(cam, root, yawThree, pad);
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            root.transform.rotation = Quaternion.identity;
        }

        /// <summary>표 하나의 시트(4열 · 칸 = 2장 나란히). 그래픽 장치가 없으면 null.</summary>
        public static Texture2D Compose(List<GalleryEntry> entries, GalleryKind kind, Rig rig)
        {
            if (!GraphicsAvailable || entries.Count == 0) return null;
            int size = RenderSize(kind);
            float pad = Pad(kind);
            int cellW = size * 2 + Gap, cellH = size + Gap;
            int rows = (entries.Count + MobGallery.Columns - 1) / MobGallery.Columns;
            int w = MobGallery.Columns * cellW + Gap, h = rows * cellH + Gap;
            var sheet = new Texture2D(w, h, TextureFormat.RGB24, false);
            var bg = VoxelMaterials.ToColor(SheetBg);
            var fill = new Color32[w * h];
            Color32 bg32 = bg;
            for (int i = 0; i < fill.Length; i++) fill[i] = bg32;
            sheet.SetPixels32(fill);

            var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            RenderTexture prev = RenderTexture.active;
            for (int i = 0; i < entries.Count; i++) entries[i].Root.SetActive(false);
            // 시트는 중립 배경(원작 renderMob 은 빈 씬) — 같은 씬에 있는 다른 렌더러(T9 지면 타일처럼 스스로 서는 것)는 찍는 동안 끈다.
            var hidden = HideOtherRenderers(entries, rig);
            try
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries[i];
                    e.Root.SetActive(true);
                    int col = i % MobGallery.Columns, row = i / MobGallery.Columns;
                    int x0 = Gap + col * cellW;
                    int y0 = h - Gap - (row + 1) * cellH + Gap;
                    float[] yaws = { YawA, YawB };
                    for (int k = 0; k < 2; k++)
                    {
                        RenderInto(rig.Camera, e.Root, yaws[k], pad, rt);
                        RenderTexture.active = rt;
                        sheet.ReadPixels(new Rect(0, 0, size, size), x0 + k * size, y0);
                    }
                    e.Root.SetActive(false);
                }
            }
            finally
            {
                RenderTexture.active = prev;
                for (int i = 0; i < hidden.Count; i++) if (hidden[i] != null) hidden[i].enabled = true;
                for (int i = 0; i < entries.Count; i++) entries[i].Root.SetActive(true);
                rt.Release();
                Object.Destroy(rt);
            }
            sheet.Apply(false);
            return sheet;
        }

        /// <summary>도감·촬영 리그 밖의 켜진 렌더러를 전부 끄고 그 목록을 돌려준다(호출자가 되돌린다).</summary>
        public static List<Renderer> HideOtherRenderers(List<GalleryEntry> entries, Rig rig)
        {
            var hidden = new List<Renderer>();
            var keep = new List<Transform>();
            for (int i = 0; i < entries.Count; i++) keep.Add(entries[i].Root.transform);
            if (rig != null && rig.Root != null) keep.Add(rig.Root.transform);
            foreach (var r in Object.FindObjectsOfType<Renderer>())
            {
                if (!r.enabled) continue;
                bool mine = false;
                for (int k = 0; k < keep.Count && !mine; k++) mine = r.transform.IsChildOf(keep[k]);
                if (mine) continue;
                r.enabled = false;
                hidden.Add(r);
            }
            return hidden;
        }

        /// <summary>`ui-screens/&lt;name&gt;.png` 로 저장하고 전체 경로를 돌려준다.</summary>
        public static string Save(Texture2D sheet, string name)
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), OutDir);
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, name + ".png");
            File.WriteAllBytes(file, sheet.EncodeToPNG());
            return file;
        }
    }
}
