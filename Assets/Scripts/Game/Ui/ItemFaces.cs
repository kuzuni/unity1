using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Forge.Core.Data;
using Forge.Core.Forging;
using Forge.Core.Gear;
using Forge.Game.Gallery;
using Forge.Game.Hero;
using Forge.Game.Voxel;

namespace Forge.Game.Ui
{
    /// <summary>T122 굽기 수치표(<c>Assets/Forge/Resources/ItemFacesUi.json</c> — 정본 `scene3d.js` itemThumbInit·itemThumb·hydrateForgeThumbs). T87 lock 이 <c>catalog.json</c> 을 쥐고 있어 T122 몫은 이 파일이 든다(T111 <see cref="GearDetailStyle"/> 과 같은 꼴 · 결정 249).</summary>
    public static class ItemFacesStyle
    {
        public const string ResourcePath = "ItemFacesUi";
        static JsonObject root, thumb, colors;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T122)");
            root = MiniJson.ParseObject(ta.text);
            thumb = J.Obj(root["thumb"]);
            colors = J.Obj(root["colors"]);
        }

        public static void Reset() { root = null; thumb = null; colors = null; }

        public static float L(string key)
        {
            Load();
            object v = thumb[key];
            if (!J.IsNum(v)) throw new KeyNotFoundException("ItemFacesUi.json 에 굽기 값 «" + key + "» 이 없다");
            return (float)J.Num(v);
        }

        public static Color C(string key)
        {
            Load();
            string hex = J.Str(colors[key]);
            Color c;
            if (hex == null || !ColorUtility.TryParseHtmlString(hex, out c)) throw new KeyNotFoundException("ItemFacesUi.json 에 색 «" + key + "» 이 없다");
            return c;
        }

        /// <summary>three 벡터 셋(x·y·z 키) → 유니티(z 뒤집음 · 결정 4).</summary>
        public static Vector3 V(string prefix) { return ThreeSpace.Pos(L(prefix + "_x"), L(prefix + "_y"), L(prefix + "_z")); }
    }

    /// <summary>
    /// 장비 그림 = 실제 3D 장비(원작 `Scene3D.itemThumb` · T122). T37 <see cref="GearMeshes"/> 가 세운 무기·투구 메시(`BuildModel`)와 갑옷 조각(`Dress` — 몸통을 감춘 <see cref="HeroRig"/> 위에)을
    /// 정본의 고정 3/4 시선(ITEM_THUMB_DIR)·3광(앰비언트·키·림)으로 한 번 찍어 <see cref="Sprite"/> 로 캐시한다(<see cref="PetFaces"/> 와 같은 길 · 키 = slot:wtype:age:rarity:nameIdx:aTier).
    /// 그래픽 장치가 없거나(CI headless) 캡처에 없는 부위(장신구 다섯 — 정본 `makeAccessoryPreview` 는 T37 이 안 뽑았다)면 null — 호출자가 슬롯 실루엣으로 폴백한다(정본 `IconGen.img('slot_…')` 플레이스홀더와 같은 순서).
    /// 목록처럼 여러 장을 한꺼번에 굽는 자리는 <see cref="NewJob"/>·<see cref="Request"/>(한 프레임에 `chunk` 장 · 목록을 다시 그리면 이전 작업은 스스로 멈춘다 = 정본 `_thumbJob`).
    /// ⚠ 정본과 다른 것(결정 기록): 자동 프레이밍은 정점 투영 대신 경계 상자 외접구 · 접지 그림자·AO·비네트 2D 마감(thumbFinish)은 아직 없다 · ACES 톤매핑 없음.
    /// </summary>
    public static class ItemFaces
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static GameObject stage;
        static Camera cam;
        static Light key, rim;
        static readonly Vector3 Away = new Vector3(0f, -700f, 0f);
        static bool triedInstall;

        /// <summary>그래픽 장치 + 장비 메시 표가 있는가. 표는 아직 아무도 부팅 때 안 꽂으므로(T37 은 훅만 냈다) 스트리밍 파일이 있으면 여기서 한 번 꽂는다(에디터·PC·CI · 안드로이드·WebGL 은 부팅 로더 몫 — 없으면 실루엣).</summary>
        public static bool Available
        {
            get
            {
                if (!GallerySheet.GraphicsAvailable) return false;
                if (GearMeshes.Installed == null && !triedInstall)
                {
                    triedInstall = true;
                    try { if (System.IO.File.Exists(GearMeshes.StreamingPath)) GearMeshes.LoadFromStreamingAssets().Install(); }
                    catch (Exception ex) { Debug.LogWarning("[ItemFaces] gear-meshes 를 못 꽂았다 — 실루엣으로: " + ex.Message); }
                }
                return GearMeshes.Installed != null;
            }
        }

        /// <summary>캡처가 있는 부위(무기·투구·갑옷). 장신구 다섯은 정본 `makeAccessoryPreview` 가 T37 캡처에 없어 실루엣이다.</summary>
        public static bool Supports(string slot) { return slot == "weapon" || slot == "helmet" || slot == "armor"; }

        /// <summary>정본 키 — slot:wtype:age:rarity:nameIdx:aTier(별이 달라도 같은 썸네일이 재사용되지 않게 승천 티어가 든다).</summary>
        public static string Key(ForgeItem it)
        {
            return it.Slot + ":" + (it.WType ?? "") + ":" + it.Age + ":" + it.Rarity + ":" + it.NameIdx + ":a" + GearMeshes.AscendTier(it.Stars);
        }

        public static int CacheCount { get { return cache.Count; } }

        public static Sprite Get(GameDefs defs, string slot, string age, int ageIdx, string wtype, int nameIdx, string rarity, int stars)
        {
            return Get(defs, new ForgeItem { Slot = slot, Age = age, AgeIdx = ageIdx, WType = wtype, NameIdx = nameIdx, Rarity = rarity, Stars = stars });
        }

        public static Sprite Get(GameDefs defs, ForgeItem it)
        {
            if (it == null || string.IsNullOrEmpty(it.Slot)) return null;
            string k = Key(it);
            Sprite sp;
            if (cache.TryGetValue(k, out sp)) return sp;
            sp = Supports(it.Slot) && Available ? Bake(defs, it, k) : null;
            cache[k] = sp;
            return sp;
        }

        public static void Reset()
        {
            cache.Clear();
            queue.Clear();
            if (stage != null) { UnityEngine.Object.Destroy(stage); stage = null; }
            cam = null; key = null; rim = null;
        }

        // ---- 한 프레임에 몇 장씩(정본 hydrateForgeThumbs · _thumbJob) ----

        sealed class Req { public int Job; public GameDefs Defs; public ForgeItem Item; public Action<Sprite> Done; }
        static readonly List<Req> queue = new List<Req>();
        static int job;
        static ItemFacesPump pump;

        /// <summary>새 작업 번호 — 목록을 다시 그릴 때 부른다. 이전 번호로 남은 요청은 버려진다.</summary>
        public static int NewJob() { job++; queue.Clear(); return job; }
        public static int CurrentJob { get { return job; } }
        public static int Pending { get { return queue.Count; } }
        public static float Chunk { get { return ItemFacesStyle.L("chunk"); } }

        /// <summary>썸네일을 다음 프레임부터 굽는다(캐시에 있어도 다음 프레임에 준다 — 첫 굽기가 무거워 동기로 부르면 목록이 뜨는 순간을 붙잡는다 · 정본 주석). 못 굽는 부위·장치면 null 로 부른다.</summary>
        public static void Request(int jobId, GameDefs defs, ForgeItem it, Action<Sprite> onReady)
        {
            if (jobId != job || it == null) return;
            queue.Add(new Req { Job = jobId, Defs = defs, Item = it, Done = onReady });
            EnsurePump();
        }

        static void EnsurePump()
        {
            if (pump != null) return;
            GameObject go = new GameObject("ItemFaces Pump");
            UnityEngine.Object.DontDestroyOnLoad(go);
            pump = go.AddComponent<ItemFacesPump>();
        }

        /// <summary>한 프레임 몫 — <see cref="ItemFacesPump"/> 가 부른다. 처리한 수를 돌려준다.</summary>
        public static int Pump()
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(Chunk));
            int done = 0;
            while (done < n && queue.Count > 0)
            {
                Req r = queue[0];
                queue.RemoveAt(0);
                if (r.Job != job) continue;
                Sprite sp = null;
                try { sp = Get(r.Defs, r.Item); }
                catch (Exception ex) { Debug.LogWarning("[ItemFaces] " + Key(r.Item) + " 썸네일 실패 — 실루엣으로: " + ex.Message); }
                done++;
                if (r.Done != null) r.Done(sp);
            }
            return done;
        }

        // ---- 굽기 ----

        static void EnsureStage()
        {
            if (stage != null && cam != null) return;
            stage = new GameObject("ItemFaces Stage");
            stage.transform.position = Away;
            GameObject camGo = new GameObject("Thumb Camera");
            camGo.transform.SetParent(stage.transform, false);
            cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = ItemFacesStyle.L("fov");
            cam.nearClipPlane = ItemFacesStyle.L("near");
            cam.farClipPlane = ItemFacesStyle.L("far");
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.enabled = false;
            key = MakeLight("Thumb Key", Color.white, ItemFacesStyle.L("key"), ItemFacesStyle.V("key"));
            rim = MakeLight("Thumb Rim", ItemFacesStyle.C("rim"), ItemFacesStyle.L("rim"), ItemFacesStyle.V("rim"));
        }

        static Light MakeLight(string name, Color color, float intensity, Vector3 fromThree)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(stage.transform, false);
            Light l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = color;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            go.transform.rotation = Quaternion.LookRotation(-fromThree.normalized, Vector3.up);
            l.enabled = false;
            return l;
        }

        static Sprite Bake(GameDefs defs, ForgeItem it, string k)
        {
            GearMeshes gm = GearMeshes.Installed;
            EnsureStage();
            GameObject model = null;
            RenderTexture rt = null;
            RenderTexture prev = RenderTexture.active;
            Texture2D tex = null;
            bool fog = RenderSettings.fog;
            AmbientMode mode = RenderSettings.ambientMode;
            Color amb = RenderSettings.ambientLight;
            Color sky = RenderSettings.ambientSkyColor, eq = RenderSettings.ambientEquatorColor, ground = RenderSettings.ambientGroundColor;
            int px = Mathf.RoundToInt(ItemFacesStyle.L("px"));
            try
            {
                model = BuildModel(gm, defs, it);
                if (model == null) return null;
                model.transform.SetParent(stage.transform, false);
                model.transform.localPosition = Vector3.zero;
                if (it.Slot == "armor") model.transform.localRotation = Quaternion.AngleAxis(-ItemFacesStyle.L("armor_yaw") * Mathf.Rad2Deg, Vector3.up);
                RenderSettings.fog = false;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = Color.white * ItemFacesStyle.L("ambient");
                key.enabled = true; rim.enabled = true;
                Frame(model);
                rt = RenderTexture.GetTemporary(px, px, 16, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = null;
                RenderTexture.active = rt;
                tex = new Texture2D(px, px, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, px, px), 0, 0);
                tex.Apply();
                tex.name = "itemface:" + k;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ItemFaces] " + k + " 썸네일 실패 — 실루엣으로: " + ex.Message);
                return null;
            }
            finally
            {
                if (key != null) key.enabled = false;
                if (rim != null) rim.enabled = false;
                RenderSettings.fog = fog;
                RenderSettings.ambientMode = mode;
                RenderSettings.ambientLight = amb;
                RenderSettings.ambientSkyColor = sky;
                RenderSettings.ambientEquatorColor = eq;
                RenderSettings.ambientGroundColor = ground;
                RenderTexture.active = prev;
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                if (model != null) UnityEngine.Object.Destroy(model);
            }
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, px, px), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sp.name = "itemface:" + k;
            return sp;
        }

        /// <summary>정본 itemThumb 의 분기 — 무기 `makeWeapon` · 투구 `makeHelmet` · 갑옷 `makeArmorPreview`(몸통을 감춘 리그에 `dressMcRig` 조각을 입힌 것과 같은 자리) · 장신구는 캡처가 없어 null.</summary>
        static GameObject BuildModel(GearMeshes gm, GameDefs defs, ForgeItem it)
        {
            if (it.Slot == "weapon")
            {
                GearModelDef def = gm.Weapon(it.WType);
                if (def == null) return null;
                return Node("weapon " + it.WType, gm.BuildModel(def, it.Rarity, false, it.Stars));
            }
            if (it.Slot == "helmet")
            {
                GearModelDef def = gm.Helmet(it.Age, it.NameIdx, PaperdollLook.ItemNameOf(defs, it));
                if (def == null) return null;
                return Node("helmet " + it.Age + "/" + it.NameIdx, gm.BuildModel(def, it.Rarity, false, it.Stars));
            }
            if (it.Slot == "armor")
            {
                if (gm.Armor(it.Age, it.NameIdx, PaperdollLook.ItemNameOf(defs, it)) == null) return null;
                HeroRig rig = HeroRig.Create(null, "armor " + it.Age + "/" + it.NameIdx);
                rig.ManualStep = true;
                // 몸통 상자·데칼은 감춘다 — 정본 makeArmorPreview 는 옷 조각만 세운다(경계 상자도 옷만 잰다 · Frame 이 꺼진 렌더러를 뺀다)
                foreach (MeshRenderer mr in rig.Boxes) mr.enabled = false;
                foreach (MeshRenderer mr in rig.Decals) mr.enabled = false;
                PaperdollLook look = new PaperdollLook { Armor = it, ArmorName = PaperdollLook.ItemNameOf(defs, it), ArmorStyle = PaperdollLook.ItemStyleOf(defs, it) };
                gm.Dress(rig, look, false);
                return rig.gameObject;
            }
            return null;
        }

        static GameObject Node(string name, GearBuilt built)
        {
            GameObject go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = built.Mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = built.Materials;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        /// <summary>정본 thumbFrameToFit 의 자리 — 고정 시선(ITEM_THUMB_DIR)에서 거리만 다시 잡는다. 정점 투영 대신 보이는 렌더러의 경계 상자 외접구로(결정 기록).</summary>
        static void Frame(GameObject model)
        {
            Bounds b;
            if (!VisibleBounds(model, out b)) b = new Bounds(model.transform.position, Vector3.one * 0.1f);
            Vector3 dir = ItemFacesStyle.V("dir").normalized;
            float r = Mathf.Max(1e-3f, b.extents.magnitude);
            float d = r / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * ItemFacesStyle.L("pad");
            cam.transform.position = b.center + dir * d;
            cam.transform.LookAt(b.center, Vector3.up);
        }

        static bool VisibleBounds(GameObject root, out Bounds b)
        {
            b = new Bounds();
            bool any = false;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(false))
            {
                if (!r.enabled) continue;
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
            return any;
        }
    }

    /// <summary>프레임마다 <see cref="ItemFaces.Pump"/> 한 번 — 정본 requestAnimationFrame(pump).</summary>
    public sealed class ItemFacesPump : MonoBehaviour
    {
        void Update()
        {
            if (ItemFaces.Pending > 0) ItemFaces.Pump();
        }
    }
}
