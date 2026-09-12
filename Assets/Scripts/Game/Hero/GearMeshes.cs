using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using Forge.Core.Data;
using Forge.Core.Gear;
using Forge.Core.Voxel;
using Forge.Game.Voxel;

namespace Forge.Game.Hero
{
    /// <summary>재질 서술(정본 three 재질 한 장 · `gear-meshes.json` `mats[]`).</summary>
    public sealed class GearMatDesc
    {
        public string T;            // std | lam | basic
        public int C, E;
        public double Ei, Met, Rough, Op, Env;
        public bool Tr, Flat, Vc, Map, Dw;
        public int Side;            // 0 front · 1 back · 2 double
        public string Key;

        public static GearMatDesc From(object v)
        {
            var o = J.Obj(v);
            var d = new GearMatDesc
            {
                T = J.Str(o["t"], "std"), C = J.Int(o["c"], 0xffffff), E = J.Int(o["e"]), Ei = J.Num(o["ei"]),
                Met = J.Num(o["met"]), Rough = J.Num(o["rough"], 1), Op = J.Num(o["op"], 1), Env = J.Num(o["env"], 1),
                Tr = J.Bool(o["tr"]), Flat = J.Bool(o["flat"]), Vc = J.Bool(o["vc"]), Map = J.Bool(o["map"]), Dw = J.Bool(o["dw"], true), Side = J.Int(o["side"])
            };
            d.Key = d.T + "|" + d.C.ToString("x6") + "|" + d.E.ToString("x6") + "|" + d.Ei + "|" + d.Met + "|" + d.Rough + "|" + d.Op + "|" + (d.Tr ? 1 : 0) + "|" + d.Side + "|" + (d.Dw ? 1 : 0);
            return d;
        }
    }

    /// <summary>칸 목록(`cellsets[]`) — 팔레트 + 평탄 [x, y, z, 팔레트 인덱스] × n.</summary>
    public sealed class GearCellSet
    {
        public int[] Pal;
        public int[] C;
        List<VoxelCell> _cells;

        public List<VoxelCell> Cells
        {
            get
            {
                if (_cells != null) return _cells;
                _cells = new List<VoxelCell>(C.Length / 4);
                for (int i = 0; i + 3 < C.Length; i += 4) _cells.Add(new VoxelCell(C[i], C[i + 1], C[i + 2], Pal[C[i + 3]]));
                return _cells;
            }
        }

        public static GearCellSet From(object v)
        {
            var o = J.Obj(v);
            return new GearCellSet { Pal = J.IntArr(o["pal"]), C = J.IntArr(o["c"]) };
        }
    }

    /// <summary>파츠 = 정본 메시 하나. `Mat` 은 루트 기준 4×4(열우선 · three 좌표계).</summary>
    public sealed class GearPart
    {
        public string K;                                  // vox | box | ring | geo
        public double Size, Jitter, Ao;                   // vox
        public int Color, Cs;
        public bool Center;
        public double[] BoxSize;                          // box
        public double Ir, Or;                             // ring
        public int Seg;
        public double[] Pos, Col;                         // geo
        public int[] Idx;
        public int Verts;
        public double[] Mat;
        public int Layer;                                 // 승천 데코 층(1~5) · 0 = 본체

        public static GearPart From(object v)
        {
            var o = J.Obj(v);
            var p = new GearPart { K = J.Str(o["k"]), Verts = J.Int(o["verts"]), Mat = J.NumArr(o["mat"]), Layer = J.Int(o["layer"]) };
            switch (p.K)
            {
                case "vox":
                    p.Size = J.Num(o["size"]); p.Color = J.Int(o["color"], 0xffffff); p.Jitter = J.Num(o["jitter"]); p.Ao = J.Num(o["ao"], 1);
                    p.Center = J.Bool(o["center"]); p.Cs = J.Int(o["cs"]);
                    break;
                case "box": p.BoxSize = J.NumArr(o["size"]); break;
                case "ring": p.Ir = J.Num(o["ir"]); p.Or = J.Num(o["or"]); p.Seg = J.Int(o["seg"]); break;
                default:
                    p.Pos = J.NumArr(o["pos"]); p.Idx = o.Has("idx") ? J.IntArr(o["idx"]) : null; p.Col = o.Has("col") ? J.NumArr(o["col"]) : null;
                    break;
            }
            return p;
        }
    }

    /// <summary>갑옷 조각 — 리그 본 이름 · 본 기준 로컬 변환(three) · 조각 기준 파츠.</summary>
    public sealed class GearPiece
    {
        public string Bone;
        public double[] Pos, Rot, Scale;
        public List<GearPart> Parts;

        public static GearPiece From(object v)
        {
            var o = J.Obj(v);
            return new GearPiece { Bone = J.Str(o["bone"]), Pos = J.NumArr(o["pos"]), Rot = J.NumArr(o["rot"]), Scale = J.NumArr(o["scale"]), Parts = J.List(o["parts"], GearPart.From) };
        }
    }

    /// <summary>지오메트리 변형 하나(등급이 젬·트림을 더하면 변형이 갈린다) — 본체 파츠 + 승천 데코 5층 (갑옷은 조각 목록).</summary>
    public sealed class GearGeom
    {
        public List<GearPart> Parts, Decor;
        public int[] Dm, Dhm;
        public List<GearPiece> Pieces;

        public static GearGeom From(object v)
        {
            var o = J.Obj(v);
            var g = new GearGeom();
            if (o.Has("pieces")) g.Pieces = J.List(o["pieces"], GearPiece.From);
            else
            {
                g.Parts = J.List(o["parts"], GearPart.From);
                g.Decor = J.List(o["decor"], GearPart.From);
                g.Dm = J.IntArr(o["dm"]); g.Dhm = J.IntArr(o["dhm"]);
            }
            return g;
        }
    }

    /// <summary>등급 → 지오메트리 변형 인덱스 + 재질(원재질 `M` · 영웅 그레이딩 `Hm` · 갑옷은 조각별 `PieceM`).</summary>
    public sealed class GearRarity
    {
        public int G;
        public int[] M, Hm;
        public List<int[]> PieceM;

        public static GearRarity From(object v)
        {
            var o = J.Obj(v);
            var r = new GearRarity { G = J.Int(o["g"]) };
            var m = J.Arr(o["m"]);
            if (m != null && m.Count > 0 && m[0] is List<object>) r.PieceM = J.List(o["m"], J.IntArr);
            else { r.M = J.IntArr(o["m"]); r.Hm = o.Has("hm") ? J.IntArr(o["hm"]) : r.M; }
            return r;
        }
    }

    public sealed class GearModelDef
    {
        public string Key, Age, Style, Name;
        public int AgeIdx, NameIdx;
        public List<GearGeom> Geoms;
        public OrderedMap<GearRarity> ByRarity;

        public static GearModelDef From(string key, object v)
        {
            var o = J.Obj(v);
            return new GearModelDef
            {
                Key = key, Age = J.Str(o["age"]), Style = J.Str(o["style"]), Name = J.Str(o["name"]), AgeIdx = J.Int(o["ageIdx"]), NameIdx = J.Int(o["nameIdx"]),
                Geoms = J.List(o["geoms"], GearGeom.From), ByRarity = J.Map(o["byRarity"], GearRarity.From)
            };
        }

        /// <summary>등급 행(없는 등급 = common · 원작 `RARITIES.indexOf(undefined)` −1 은 젬·트림이 없어 common 과 같다).</summary>
        public GearRarity Rarity(string rarity)
        {
            GearRarity r;
            if (rarity != null && ByRarity.TryGet(rarity, out r)) return r;
            return ByRarity.ValueAt(0);
        }
    }

    /// <summary>세운 결과 — 메시 하나(재질마다 서브메시) + 재질 배열.</summary>
    public sealed class GearBuilt
    {
        public Mesh Mesh;
        public Material[] Materials;
        public int VertexCount { get { return Mesh != null ? Mesh.vertexCount : 0; } }
    }

    public sealed class GearBuiltPiece
    {
        public string Bone;
        public Vector3 LocalPos, LocalScale;
        public Quaternion LocalRot;
        public GearBuilt Built;
    }

    /// <summary>
    /// 장비 3D 외형(T37) — `gear-meshes.json`(`tools/export_gear_meshes.js` 가 정본 `makeWeapon`·`makeHelmet`·`dressMcRig`·`gradeHeroGearValue`·`applyAscendDecor` 를
    /// 실물 three 로 돌려 뽑은 캡처)을 유니티 Mesh·Material 로 세우고 <see cref="Paperdoll.WeaponMeshProvider"/>·<see cref="Paperdoll.OnDressed"/> 에 꽂는다.
    /// 조형은 모른다 — 파츠(칸 목록·상자·링)를 정본 행렬 그대로 굽고 z 만 뒤집는다(결정 4). 칸 목록은 T4 <see cref="VoxelGeometry"/>(정본 `Voxel.build` 이식)로 다시 굽는다.
    /// 파일 읽기는 호출자 몫(안드로이드·WebGL 은 UnityWebRequest — SaveIo 규약) · 에디터·PC·테스트는 <see cref="LoadFromStreamingAssets"/>.
    /// </summary>
    public sealed class GearMeshes
    {
        public const string File = "gear-meshes.json";
        public const string HelmetNode = "helmetG", ClothNode = "mcCloth", WeaponNode = "weapon";

        public List<GearMatDesc> Mats;
        public List<GearCellSet> CellSets;
        public OrderedMap<GearModelDef> Weapons, Helmets, Armors;
        public string[] Rarities;
        public int DecorTiers;
        public string HelmetMountBone;
        public double[] HelmetMountPos;
        public JsonObject Meta;

        public static GearMeshes Parse(string json)
        {
            var o = MiniJson.ParseObject(json);
            var g = new GearMeshes();
            g.Meta = J.Obj(J.Require(o, "meta"));
            g.Rarities = J.StrArr(g.Meta["rarities"]);
            g.DecorTiers = J.Int(g.Meta["decorTiers"], 5);
            var hm = J.Obj(g.Meta["helmetMount"]);
            g.HelmetMountBone = hm != null ? J.Str(hm["bone"], "head") : "head";
            g.HelmetMountPos = hm != null ? J.NumArr(hm["pos"]) : new double[] { 0, 0, 0 };
            g.Mats = J.List(J.Require(o, "mats"), GearMatDesc.From);
            g.CellSets = J.List(J.Require(o, "cellsets"), GearCellSet.From);
            g.Weapons = Models(J.Obj(J.Require(o, "weapons")));
            g.Helmets = Models(J.Obj(J.Require(o, "helmets")));
            g.Armors = Models(J.Obj(J.Require(o, "armors")));
            return g;
        }

        static OrderedMap<GearModelDef> Models(JsonObject o)
        {
            var m = new OrderedMap<GearModelDef>();
            foreach (var kv in o) m.Add(kv.Key, GearModelDef.From(kv.Key, kv.Value));
            return m;
        }

        public static string StreamingPath { get { return Path.Combine(Application.streamingAssetsPath, "data", File); } }

        /// <summary>에디터·PC·테스트: 파일로 읽는다. (안드로이드·WebGL 은 부팅 로더가 문자열을 <see cref="Parse"/> 에 준다.)</summary>
        public static GearMeshes LoadFromStreamingAssets()
        {
            return Parse(System.IO.File.ReadAllText(StreamingPath));
        }

        // ── 찾기 ────────────────────────────────────────────────────────────────
        public GearModelDef Weapon(string wtypeId) { GearModelDef d; return wtypeId != null && Weapons.TryGet(wtypeId, out d) ? d : null; }

        /// <summary>투구 = 시대/이름 인덱스 · 인덱스가 어긋나면 이름으로 · 그래도 없으면 그 시대 첫 것.</summary>
        public GearModelDef Helmet(string age, int nameIdx, string name = null) { return Find(Helmets, age, nameIdx, name); }
        public GearModelDef Armor(string age, int nameIdx, string name = null) { return Find(Armors, age, nameIdx, name); }

        static GearModelDef Find(OrderedMap<GearModelDef> map, string age, int nameIdx, string name)
        {
            if (age == null) return null;
            GearModelDef d;
            if (map.TryGet(age + "/" + nameIdx, out d)) return d;
            GearModelDef first = null;
            foreach (var kv in map)
            {
                if (kv.Value.Age != age) continue;
                if (first == null) first = kv.Value;
                if (name != null && kv.Value.Name == name) return kv.Value;
            }
            return first;
        }

        /// <summary>원작 `ascendTier(stars)` = ((stars % 6) + 6) % 6 — 데코 층 상한.</summary>
        public static int AscendTier(int stars) { return ((stars % 6) + 6) % 6; }

        // ── 세우기 ──────────────────────────────────────────────────────────────
        /// <summary>무기·투구 모델 하나를 메시로. hero = 영웅 그레이딩 재질(`gradeHeroGearValue` 뒤) · stars → 승천 데코 층.</summary>
        public GearBuilt BuildModel(GearModelDef def, string rarity, bool hero, int stars, string name = null)
        {
            var r = def.Rarity(rarity);
            var geom = def.Geoms[r.G];
            var b = new MeshBuilder(this);
            int[] mats = hero ? r.Hm : r.M;
            for (int i = 0; i < geom.Parts.Count; i++) b.Add(geom.Parts[i], mats[i]);
            int tier = AscendTier(stars);
            if (tier > 0 && geom.Decor != null)
            {
                int[] dm = hero ? geom.Dhm : geom.Dm;
                for (int i = 0; i < geom.Decor.Count; i++) if (geom.Decor[i].Layer <= tier) b.Add(geom.Decor[i], dm[i]);
            }
            return b.Build(name ?? ("gear " + def.Key + " " + (rarity ?? "common")));
        }

        /// <summary>갑옷 한 벌 → 조각들(본 이름 · 본 기준 유니티 로컬 변환 · 메시).</summary>
        public List<GearBuiltPiece> BuildArmor(GearModelDef def, string rarity)
        {
            var r = def.Rarity(rarity);
            var geom = def.Geoms[r.G];
            var outList = new List<GearBuiltPiece>();
            for (int i = 0; i < geom.Pieces.Count; i++)
            {
                var pc = geom.Pieces[i];
                var b = new MeshBuilder(this);
                int[] mats = r.PieceM[i];
                for (int k = 0; k < pc.Parts.Count; k++) b.Add(pc.Parts[k], mats[k]);
                outList.Add(new GearBuiltPiece
                {
                    Bone = pc.Bone,
                    LocalPos = ThreeSpace.Pos(pc.Pos),
                    LocalRot = ThreeSpace.Rot(pc.Rot),
                    LocalScale = new Vector3((float)pc.Scale[0], (float)pc.Scale[1], (float)pc.Scale[2]),
                    Built = b.Build("cloth " + def.Key + " " + pc.Bone)
                });
            }
            return outList;
        }

        /// <summary>정점 수 기대값(캡처 `verts` 합) — 테스트가 T4 빌더와 대조한다.</summary>
        public static int ExpectedVerts(GearGeom geom, int tier)
        {
            int n = 0;
            if (geom.Parts != null) foreach (var p in geom.Parts) n += p.Verts;
            if (tier > 0 && geom.Decor != null) foreach (var p in geom.Decor) if (p.Layer <= tier) n += p.Verts;
            if (geom.Pieces != null) foreach (var pc in geom.Pieces) foreach (var p in pc.Parts) n += p.Verts;
            return n;
        }

        // ── 메시 조립: 파츠를 three 공간에서 행렬로 굽고 z 를 뒤집는다(감김도 뒤집는다) · 재질마다 서브메시 ──
        sealed class MeshBuilder
        {
            readonly GearMeshes _g;
            readonly List<Vector3> _v = new List<Vector3>();
            readonly List<Color> _c = new List<Color>();
            readonly List<int> _matOrder = new List<int>();
            readonly Dictionary<int, List<int>> _tris = new Dictionary<int, List<int>>();

            public MeshBuilder(GearMeshes g) { _g = g; }

            public void Add(GearPart p, int matIdx)
            {
                float[] pos; float[] col; int[] tri;
                switch (p.K)
                {
                    case "vox":
                        {
                            var cs = _g.CellSets[p.Cs];
                            var o = VoxelBuildOptions.Default;
                            o.Size = p.Size; o.Color = p.Color; o.Jitter = p.Jitter; o.Ao = p.Ao; o.Center = p.Center; o.LeftHanded = false;
                            var vm = VoxelGeometry.Build(cs.Cells, o);
                            pos = vm.Positions; col = vm.Colors; tri = vm.Triangles;
                            break;
                        }
                    case "box": Box(p.BoxSize, out pos, out tri); col = null; break;
                    case "ring": Ring(p.Ir, p.Or, p.Seg, out pos, out tri); col = null; break;
                    default:
                        {
                            pos = new float[p.Pos.Length];
                            for (int i = 0; i < pos.Length; i++) pos[i] = (float)p.Pos[i];
                            if (p.Idx != null) tri = p.Idx;
                            else { tri = new int[pos.Length / 3]; for (int i = 0; i < tri.Length; i++) tri[i] = i; }
                            col = null;
                            if (p.Col != null) { col = new float[p.Col.Length]; for (int i = 0; i < col.Length; i++) col[i] = (float)p.Col[i]; }
                            break;
                        }
                }
                int baseIndex = _v.Count;
                double[] m = p.Mat;
                for (int i = 0; i + 2 < pos.Length; i += 3)
                {
                    double x = pos[i], y = pos[i + 1], z = pos[i + 2];
                    double wx = m[0] * x + m[4] * y + m[8] * z + m[12];
                    double wy = m[1] * x + m[5] * y + m[9] * z + m[13];
                    double wz = m[2] * x + m[6] * y + m[10] * z + m[14];
                    _v.Add(new Vector3((float)wx, (float)wy, -(float)wz));
                    _c.Add(col != null ? new Color(col[i], col[i + 1], col[i + 2], 1f) : Color.white);
                }
                List<int> list;
                if (!_tris.TryGetValue(matIdx, out list)) { list = new List<int>(); _tris[matIdx] = list; _matOrder.Add(matIdx); }
                for (int i = 0; i + 2 < tri.Length; i += 3)
                {
                    // z 거울은 감김을 뒤집는다 — 뒤 두 정점을 바꿔 앞면을 유지(T4 VoxelGeometry 와 같은 규약)
                    list.Add(baseIndex + tri[i]); list.Add(baseIndex + tri[i + 2]); list.Add(baseIndex + tri[i + 1]);
                }
            }

            public GearBuilt Build(string name)
            {
                var mesh = new Mesh { name = name };
                if (_v.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(_v);
                mesh.SetColors(_c);
                mesh.subMeshCount = _matOrder.Count;
                var mats = new Material[_matOrder.Count];
                for (int s = 0; s < _matOrder.Count; s++)
                {
                    mesh.SetTriangles(_tris[_matOrder[s]], s);
                    mats[s] = GearMaterials.Get(_g.Mats[_matOrder[s]]);
                }
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return new GearBuilt { Mesh = mesh, Materials = mats };
            }

            /// <summary>THREE.BoxGeometry(w, h, d) — 원점 중심 · 면마다 정점 4(24) · 삼각형 12.</summary>
            static void Box(double[] size, out float[] pos, out int[] tri)
            {
                float hw = (float)size[0] / 2, hh = (float)size[1] / 2, hd = (float)size[2] / 2;
                Vector3[] c = { new Vector3(hw, 0, 0), new Vector3(-hw, 0, 0), new Vector3(0, hh, 0), new Vector3(0, -hh, 0), new Vector3(0, 0, hd), new Vector3(0, 0, -hd) };
                Vector3[] u = { new Vector3(0, 0, -hd), new Vector3(0, 0, hd), new Vector3(hw, 0, 0), new Vector3(hw, 0, 0), new Vector3(hw, 0, 0), new Vector3(-hw, 0, 0) };
                Vector3[] v = { new Vector3(0, hh, 0), new Vector3(0, hh, 0), new Vector3(0, 0, -hd), new Vector3(0, 0, hd), new Vector3(0, hh, 0), new Vector3(0, hh, 0) };
                pos = new float[24 * 3]; tri = new int[12 * 3];
                for (int f = 0; f < 6; f++)
                {
                    Vector3[] q = { c[f] - u[f] - v[f], c[f] + u[f] - v[f], c[f] + u[f] + v[f], c[f] - u[f] + v[f] };
                    for (int k = 0; k < 4; k++) { int p = (f * 4 + k) * 3; pos[p] = q[k].x; pos[p + 1] = q[k].y; pos[p + 2] = q[k].z; }
                    int b = f * 4, t = f * 6;
                    tri[t] = b; tri[t + 1] = b + 1; tri[t + 2] = b + 2; tri[t + 3] = b; tri[t + 4] = b + 2; tri[t + 5] = b + 3;
                }
            }

            /// <summary>THREE.RingGeometry(inner, outer, seg) — xy 평면 · 정점 2×(seg+1).</summary>
            static void Ring(double ir, double or, int seg, out float[] pos, out int[] tri)
            {
                int n = seg + 1;
                pos = new float[2 * n * 3]; tri = new int[seg * 6];
                for (int j = 0; j < 2; j++)
                {
                    double r = j == 0 ? ir : or;
                    for (int i = 0; i < n; i++)
                    {
                        double a = (double)i / seg * Math.PI * 2;
                        int p = (j * n + i) * 3;
                        pos[p] = (float)(r * Math.Cos(a)); pos[p + 1] = (float)(r * Math.Sin(a)); pos[p + 2] = 0;
                    }
                }
                for (int i = 0; i < seg; i++)
                {
                    int a = i, b = i + n, c = i + 1 + n, d = i + 1, t = i * 6;
                    tri[t] = a; tri[t + 1] = b; tri[t + 2] = d; tri[t + 3] = b; tri[t + 4] = c; tri[t + 5] = d;
                }
            }
        }

        // ── 영웅에 입히기(원작 refreshHeroEquip 의 무기·투구·옷 부분) ──────────────
        GearBuilt _lastWeapon;
        static GearMeshes _installed;
        public static GearMeshes Installed { get { return _installed; } }

        /// <summary><see cref="Paperdoll"/> 훅에 꽂는다 — 이후 <see cref="Paperdoll.Refresh"/> 가 무기 메시·투구·옷을 이 캡처로 세운다.</summary>
        public void Install()
        {
            _installed = this;
            Paperdoll.WeaponMeshProvider = ProvideWeapon;
            Paperdoll.OnDressed = Dress;
        }

        public static void Uninstall()
        {
            if (_installed == null) return;
            Paperdoll.WeaponMeshProvider = null;
            Paperdoll.OnDressed = null;
            _installed = null;
        }

        Mesh ProvideWeapon(string wtypeId, PaperdollLook look)
        {
            _lastWeapon = null;
            var def = Weapon(wtypeId);
            if (def == null) return null;                 // 표에 없는 종 → T6 막대
            string rarity = look.Weapon != null ? look.Weapon.Rarity : null;
            int stars = look.Weapon != null ? look.Weapon.Stars : 0;
            _lastWeapon = BuildModel(def, rarity, true, stars);
            return _lastWeapon.Mesh;
        }

        /// <summary>무기 재질 배열 + 투구(head 본 · helmetMount) + 옷 한 벌(관절 본) — 이전 것은 걷어낸다.</summary>
        public void Dress(HeroRig rig, PaperdollLook look, bool withFlash)
        {
            if (rig == null) return;
            // 무기: HeroRig.Equip 이 메시만 받았으니 재질 배열은 여기서
            var mount = rig.WeaponMount;
            if (mount != null && _lastWeapon != null)
            {
                for (int i = 0; i < mount.childCount; i++)
                {
                    var mf = mount.GetChild(i).GetComponent<MeshFilter>();
                    var mr = mount.GetChild(i).GetComponent<MeshRenderer>();
                    if (mf != null && mr != null && mf.sharedMesh == _lastWeapon.Mesh) mr.sharedMaterials = _lastWeapon.Materials;
                }
            }
            // 투구
            var head = rig.Bone(HelmetMountBone) ?? rig.Bone("head");
            if (head != null)
            {
                RemoveNamed(head, HelmetNode);
                if (look.Helmet != null)
                {
                    var def = Helmet(look.Helmet.Age, look.Helmet.NameIdx, look.HelmetName);
                    if (def != null)
                    {
                        var built = BuildModel(def, look.Helmet.Rarity, true, look.Helmet.Stars);
                        var go = Node(head, HelmetNode, built);
                        go.transform.localPosition = ThreeSpace.Pos(HelmetMountPos);
                    }
                }
            }
            // 옷 한 벌(dressMcRig)
            foreach (var bone in rig.Bones.Values) RemoveNamed(bone, ClothNode);
            if (look.Armor != null)
            {
                var def = Armor(look.Armor.Age, look.Armor.NameIdx, look.ArmorName);
                if (def != null)
                {
                    foreach (var pc in BuildArmor(def, look.Armor.Rarity))
                    {
                        var bone = rig.Bone(pc.Bone);
                        if (bone == null) continue;
                        var go = Node(bone, ClothNode, pc.Built);
                        go.transform.localPosition = pc.LocalPos;
                        go.transform.localRotation = pc.LocalRot;
                        go.transform.localScale = pc.LocalScale;
                    }
                }
            }
        }

        static GameObject Node(Transform parent, string name, GearBuilt built)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = built.Mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = built.Materials;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = true;
            return go;
        }

        static void RemoveNamed(Transform parent, string name)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var c = parent.GetChild(i);
                if (c.name != name) continue;
                c.SetParent(null, false);
                UnityEngine.Object.Destroy(c.gameObject);
            }
        }

        /// <summary>본 아래 이름으로 붙은 노드 수(테스트용).</summary>
        public static int CountNamed(Transform parent, string name)
        {
            int n = 0;
            for (int i = 0; i < parent.childCount; i++) if (parent.GetChild(i).name == name) n++;
            return n;
        }
    }

    /// <summary>
    /// 캡처 재질 서술 → URP 재질(T4 <see cref="VoxelMaterials"/> 와 같은 셰이더 · 정점색 × 기본색). std/lam 은 Lit(정점색) · basic 은 Unlit.
    /// 정본의 map(가죽 결 캔버스 텍스처)·envMapIntensity 는 색으로 접는다(복셀 화풍 — 결정 기록).
    /// </summary>
    public static class GearMaterials
    {
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
        static readonly int CullId = Shader.PropertyToID("_Cull");

        public static Material Get(GearMatDesc d)
        {
            Material m;
            if (cache.TryGetValue(d.Key, out m) && m != null) return m;
            m = Make(d);
            cache[d.Key] = m;
            return m;
        }

        public static void ClearCache() { cache.Clear(); }

        static Material Template(bool basic)
        {
            var res = Resources.Load<Material>(basic ? VoxelMaterials.UnlitResource : VoxelMaterials.LitResource);
            if (res != null) return new Material(res);
            var sh = Shader.Find(basic ? VoxelMaterials.UnlitShader : VoxelMaterials.LitShader);
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return new Material(sh);
        }

        static Material Make(GearMatDesc d)
        {
            bool basic = d.T == "basic";
            var m = Template(basic);
            m.name = "Gear " + d.Key;
            var baseColor = VoxelMaterials.ToColor(d.C);
            bool transparent = d.Tr && d.Op < 1;
            if (transparent) baseColor.a = (float)d.Op;
            if (m.HasProperty(MetallicId)) m.SetFloat(MetallicId, d.T == "std" ? (float)d.Met : 0f);
            if (m.HasProperty(SmoothnessId)) m.SetFloat(SmoothnessId, d.T == "std" ? (float)(1 - d.Rough) : 0f);
            if (!basic && d.E != 0 && d.Ei > 0 && m.HasProperty(EmissionColorId))
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.SetColor(EmissionColorId, VoxelMaterials.ToColor(d.E) * (float)d.Ei);
            }
            if (transparent)
            {
                if (m.HasProperty(SurfaceId)) m.SetFloat(SurfaceId, 1);
                if (m.HasProperty(BlendId)) m.SetFloat(BlendId, 0);
                if (m.HasProperty(SrcBlendId)) m.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
                if (m.HasProperty(DstBlendId)) m.SetFloat(DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
                if (m.HasProperty(ZWriteId)) m.SetFloat(ZWriteId, d.Dw ? 1 : 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            if (d.Side == 2 && m.HasProperty(CullId)) m.SetFloat(CullId, (float)CullMode.Off);
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, baseColor);
            else m.color = baseColor;
            return m;
        }
    }
}
