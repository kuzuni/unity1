using System;
using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.World
{
    /// <summary>신설 바이옴 한 줄(정본 `Scene3D.BIOMES` · kin = 형질을 물려받는 원본 6종 + 덮어쓸 값만). 없는 칸은 null — JS 의 undefined 와 같다.</summary>
    public sealed class BiomeSpec
    {
        public string Key;
        public string Kin;
        public double[] Tint;
        public string Ridge;
        /// <summary>`stone` 이 있으면 그 색(0xRRGGBB).</summary>
        public int? Stone;
        /// <summary>`emissive` 키가 있는가(undefined 와 null 을 가른다 — 흑요석은 명시적 null 로 발광을 끈다).</summary>
        public bool HasEmissive;
        /// <summary>`emissive` 값(키가 없거나 null 이면 null).</summary>
        public JsonObject Emissive;
        public JsonObject Crystal;
        public JsonObject Foliage;
        public double[] Fog;
        public bool? Veg;
        public bool? Aurora;
        public JsonObject Raw;

        public static BiomeSpec From(string key, JsonObject o)
        {
            var b = new BiomeSpec { Key = key, Raw = o, Kin = J.Str(o["kin"]), Ridge = J.Str(o["ridge"]) };
            object v;
            if (o.TryGet("tint", out v) && v != null) b.Tint = J.NumArr(v);
            if (o.TryGet("stone", out v) && v != null) b.Stone = J.Int(v);
            if (o.TryGet("emissive", out v)) { b.HasEmissive = true; b.Emissive = J.Obj(v); }
            if (o.TryGet("crystal", out v)) b.Crystal = J.Obj(v);
            if (o.TryGet("foliage", out v)) b.Foliage = J.Obj(v);
            if (o.TryGet("fog", out v) && v != null) b.Fog = J.NumArr(v);
            if (o.TryGet("veg", out v) && v != null) b.Veg = J.Bool(v);
            if (o.TryGet("aurora", out v) && v != null) b.Aurora = J.Bool(v);
            return b;
        }

        /// <summary>발광 표의 한 칸(crack·intensity·glow·light) — 없으면 null.</summary>
        public int? EmissiveHex(string key)
        {
            object v;
            if (Emissive == null || !Emissive.TryGet(key, out v) || v == null) return null;
            return J.Int(v);
        }

        public double? EmissiveNum(string key)
        {
            object v;
            if (Emissive == null || !Emissive.TryGet(key, out v) || v == null) return null;
            return J.Num(v);
        }
    }

    /// <summary>
    /// 대열 호/격자 서술(정본 `PET_ARC`·`MOUNT_ARC`) — `formationSpot(i, arc, row0)` 의 두 번째 인자. `Rear` 면 후방(−x) 격자(펫), 아니면 좌우 대칭 호(탈것 무리).
    /// `zBase` 는 있을 때만(undefined 와 가른다 — 정본 `arc.zBase === undefined ? rz·cos(a) : zBase − rz·cos(a)`).
    /// </summary>
    public sealed class ArcSpec
    {
        public bool Rear;
        public double Rx0, RxStep, Rz0, RzStep;
        public int Cols;
        public double MountedRx, MountedRz;
        public bool HasZBase;
        public double ZBase, Hmin, Hmax, Gap;
        public JsonObject Raw;

        public static ArcSpec From(JsonObject o)
        {
            var a = new ArcSpec { Raw = o };
            object v;
            a.Rear = o.TryGet("rear", out v) && J.Bool(v);
            a.Rx0 = J.Num(o["rx0"]); a.RxStep = J.Num(o["rxStep"]); a.Rz0 = J.Num(o["rz0"]); a.RzStep = J.Num(o["rzStep"]);
            a.Cols = J.Int(o["cols"]);
            a.MountedRx = J.Num(o["mountedRx"]); a.MountedRz = J.Num(o["mountedRz"]);
            if (o.TryGet("zBase", out v) && v != null) { a.HasZBase = true; a.ZBase = J.Num(v); }
            a.Hmin = J.Num(o["hmin"]); a.Hmax = J.Num(o["hmax"]); a.Gap = J.Num(o["gap"]);
            return a;
        }

        /// <summary>정본 `refreshPets` 의 탑승 중 호 — `{ ...PET_ARC, rx0: rx0 + mountedRx, rz0: rz0 + mountedRz }`(나머지 칸 그대로).</summary>
        public ArcSpec ShiftedForMount()
        {
            return new ArcSpec
            {
                Raw = Raw, Rear = Rear, Rx0 = Rx0 + MountedRx, RxStep = RxStep, Rz0 = Rz0 + MountedRz, RzStep = RzStep, Cols = Cols,
                MountedRx = MountedRx, MountedRz = MountedRz, HasZBase = HasZBase, ZBase = ZBase, Hmin = Hmin, Hmax = Hmax, Gap = Gap,
            };
        }
    }

    /// <summary>
    /// `Assets/StreamingAssets/data/scene.json`(T2 추출기의 scene 갈래 · 정본 `Scene3D` 의 상수표) 강타입.
    /// 값은 전부 여기서 읽는다 — 코드에는 안 박는다(§1). setTheme 안의 인라인 리터럴(규칙)은 <see cref="WorldRules"/>.
    /// </summary>
    public sealed class SceneDefs
    {
        public const string File = "scene.json";

        public bool SimpleBg;
        public double GroundK, GroundMax, FoliageK, FoliageMax, SatK, FarDesat, LeafFloor, LeafCool;
        public double[] SunDay, SunNight, CamPos;
        public double CamLookY, CamFov;
        // T105 — 정본 `previewBuild()` 의 미니 씬 카메라 리그(플레이어 정보 팝업 미리보기): fov · near/far · 자리 · 시선 · 영웅 리그 자리.
        // three 좌표 그대로(유니티 쪽 ThreeSpace 가 z 를 뒤집는다) · 옛 표에는 없어 HasPreviewCam=false 로 물러난다.
        public bool HasPreviewCam;
        public double PvFov, PvNear, PvFar;
        public double[] PvPos, PvLook, PvHero;
        public double[] RidgeMixDay, RidgeMixNight;
        public double VoxCell, VoxStep;
        public double ShadeStrength;
        public double TerrainMacro, TerrainLod, TerrainLodNear, TerrainLodFar, TerrainSnow;
        public double SoilYK, SoilSatK, SoilSatMax;
        public double[][] LeafOffFoliage;
        public double[] LeafOffBush;
        public double VoxAmbientLeaf, VoxAmbientProp;
        public double[] CrackW, CrackA;
        public OrderedMap<BiomeSpec> Biomes;
        // T10 — 크리처 공통 3/4 facing(정본 `CREATURE_YAW` · 펫·탈것·소환체가 한 값) · 펫 앞 3자리(`PET_ROW0` mounted/unmounted · [x, z]) · 펫 후방 격자(`PET_ARC`) · 탈것 무리 호(`MOUNT_ARC` · T11)
        public double CreatureYaw;
        public double[][] PetRow0Mounted, PetRow0Unmounted;
        public ArcSpec PetArc, MountArc;
        public JsonObject Raw;

        public static readonly string[] BaseBiomes = { "forest", "desert", "rock", "snow", "magic", "lava" };

        public static SceneDefs Parse(string json) { return From(MiniJson.ParseObject(json)); }

        public static SceneDefs From(JsonObject o)
        {
            var d = new SceneDefs { Raw = o };
            d.SimpleBg = J.Bool(J.Require(o, "SIMPLE_BG"));
            var v = J.Obj(J.Require(o, "VALUE"));
            d.GroundK = J.Num(v["groundK"]); d.GroundMax = J.Num(v["groundMax"]); d.FoliageK = J.Num(v["foliageK"]); d.FoliageMax = J.Num(v["foliageMax"]);
            d.SatK = J.Num(v["satK"]); d.FarDesat = J.Num(v["farDesat"]); d.LeafFloor = J.Num(v["leafFloor"]); d.LeafCool = J.Num(v["leafCool"]);
            d.SunDay = J.NumArr(J.Require(o, "SUN_DAY"));
            d.SunNight = J.NumArr(J.Require(o, "SUN_NIGHT"));
            d.CamPos = J.NumArr(J.Require(o, "CAM_POS"));
            d.CamLookY = J.Num(J.Require(o, "CAM_LOOK_Y"));
            d.CamFov = J.Num(J.Require(o, "CAM_FOV"));
            object pcv;
            if (o.TryGet("PREVIEW_CAM", out pcv) && pcv != null)
            {
                var pc = J.Obj(pcv);
                d.HasPreviewCam = true;
                d.PvFov = J.Num(pc["fov"]); d.PvNear = J.Num(pc["near"]); d.PvFar = J.Num(pc["far"]);
                d.PvPos = J.NumArr(pc["pos"]); d.PvLook = J.NumArr(pc["look"]); d.PvHero = J.NumArr(pc["hero"]);
            }
            var rm = J.Obj(J.Require(o, "RIDGE_MIX"));
            d.RidgeMixDay = J.NumArr(rm["day"]); d.RidgeMixNight = J.NumArr(rm["night"]);
            var vg = J.Obj(J.Require(o, "VOXG"));
            d.VoxCell = J.Num(vg["cell"]); d.VoxStep = J.Num(vg["step"]);
            d.ShadeStrength = J.Num(J.Obj(J.Require(o, "SHADE"))["strength"]);
            var t = J.Obj(J.Require(o, "TERRAIN"));
            d.TerrainMacro = J.Num(t["macro"]); d.TerrainLod = J.Num(t["lod"]); d.TerrainLodNear = J.Num(t["lodNear"]); d.TerrainLodFar = J.Num(t["lodFar"]); d.TerrainSnow = J.Num(t["snow"]);
            var s = J.Obj(J.Require(o, "SOIL"));
            d.SoilYK = J.Num(s["yK"]); d.SoilSatK = J.Num(s["satK"]); d.SoilSatMax = J.Num(s["satMax"]);
            var lo = J.Obj(J.Require(o, "LEAF_OFF"));
            d.LeafOffFoliage = J.List(lo["foliage"], x => J.NumArr(x)).ToArray();
            d.LeafOffBush = J.NumArr(lo["bush"]);
            var va = J.Obj(J.Require(o, "VOX_AMBIENT"));
            d.VoxAmbientLeaf = J.Num(va["leaf"]); d.VoxAmbientProp = J.Num(va["prop"]);
            d.CrackW = J.NumArr(J.Require(o, "CRACK_W"));
            d.CrackA = J.NumArr(J.Require(o, "CRACK_A"));
            var bo = J.Obj(J.Require(o, "BIOMES"));
            d.Biomes = new OrderedMap<BiomeSpec>();
            foreach (string k in bo.Keys) d.Biomes.Add(k, BiomeSpec.From(k, J.Obj(bo[k])));
            d.CreatureYaw = J.Num(J.Require(o, "CREATURE_YAW"));
            var r0 = J.Obj(J.Require(o, "PET_ROW0"));
            d.PetRow0Mounted = J.List(r0["mounted"], x => J.NumArr(x)).ToArray();
            d.PetRow0Unmounted = J.List(r0["unmounted"], x => J.NumArr(x)).ToArray();
            d.PetArc = ArcSpec.From(J.Obj(J.Require(o, "PET_ARC")));
            d.MountArc = ArcSpec.From(J.Obj(J.Require(o, "MOUNT_ARC")));
            return d;
        }

        /// <summary>정본 `bkin` — 신설 바이옴은 kin, 원본 6종은 자기 자신.</summary>
        public string Kin(string biome)
        {
            BiomeSpec sp;
            if (biome != null && Biomes.TryGet(biome, out sp) && !string.IsNullOrEmpty(sp.Kin)) return sp.Kin;
            return biome;
        }

        /// <summary>신설 바이옴 표(없으면 null = 원본 6종의 예전 경로).</summary>
        public BiomeSpec Spec(string biome)
        {
            BiomeSpec sp;
            return biome != null && Biomes.TryGet(biome, out sp) ? sp : null;
        }
    }
}
