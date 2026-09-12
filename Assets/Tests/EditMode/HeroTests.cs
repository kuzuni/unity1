using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Hero;

namespace Forge.Tests
{
    /// <summary>T6 벡터(`Assets/Tests/EditMode/Vectors/t6-hero.json` · `tools/hero_vectors.js` 가 정본 prochar.js·scene3d.js 파지 절을 실물 three r128 위에서 돌려 뽑은 것).</summary>
    static class HeroVectors
    {
        static JsonObject _doc;
        public static JsonObject Doc
        {
            get
            {
                if (_doc != null) return _doc;
                string dataDir = DataDir.Path;
                string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(dataDir)));
                string file = Path.Combine(root, "Assets", "Tests", "EditMode", "Vectors", "t6-hero.json");
                if (!File.Exists(file)) throw new FileNotFoundException("T6 벡터가 없다 — node tools/hero_vectors.js 로 뽑는다: " + file);
                _doc = MiniJson.ParseObject(File.ReadAllText(file));
                return _doc;
            }
        }

        public static JsonObject Consts { get { return J.Obj(Doc["consts"]); } }

        /// <summary>정본 restPose/ridePose 객체 → <see cref="PoseAdd"/>(숫자 = rx 가산 · 객체 = 축별).</summary>
        public static PoseAdd Pose(object v)
        {
            var o = J.Obj(v);
            if (o == null) return null;
            var p = new PoseAdd();
            foreach (var kv in o)
            {
                if (J.IsNum(kv.Value)) p.Add(kv.Key, J.Num(kv.Value));
                else
                {
                    var d = J.Obj(kv.Value);
                    p.Add(kv.Key, PoseDelta.Of(J.Num(d["rx"]), J.Num(d["ry"]), J.Num(d["rz"])));
                }
            }
            return p;
        }

        public static void PoseEq(object expected, PoseAdd actual, string what)
        {
            var o = J.Obj(expected);
            if (o == null) { Assert.IsNull(actual, what + " restPose 는 null"); return; }
            Assert.IsNotNull(actual, what + " restPose 가 null");
            Assert.AreEqual(o.Count, actual.Items.Count, what + " restPose 칸 수");
            int i = 0;
            foreach (var kv in o)
            {
                Assert.AreEqual(kv.Key, actual.Items[i].Key, what + " restPose 키 순서");
                var d = actual.Items[i].Value;
                if (J.IsNum(kv.Value)) { Assert.IsTrue(d.IsNumber, what + "." + kv.Key + " 는 숫자"); Assert.AreEqual(J.Num(kv.Value), d.Rx, 1e-9, what + "." + kv.Key); }
                else
                {
                    var e = J.Obj(kv.Value);
                    Assert.IsFalse(d.IsNumber, what + "." + kv.Key + " 는 객체");
                    Assert.AreEqual(J.Num(e["rx"]), d.Rx, 1e-9, what + "." + kv.Key + ".rx");
                    Assert.AreEqual(J.Num(e["ry"]), d.Ry, 1e-9, what + "." + kv.Key + ".ry");
                    Assert.AreEqual(J.Num(e["rz"]), d.Rz, 1e-9, what + "." + kv.Key + ".rz");
                }
                i++;
            }
        }

        public static void ArrEq(object expected, double[] actual, double tol, string what)
        {
            var e = J.NumArr(expected);
            Assert.AreEqual(e.Length, actual.Length, what + " 길이");
            for (int i = 0; i < e.Length; i++) Assert.AreEqual(e[i], actual[i], tol, what + "[" + i + "]");
        }

        public static WeaponType Wt(string id)
        {
            WeaponType w;
            return DataDir.Game.Defs.WeaponTypes.TryGet(id, out w) ? w : null;
        }
    }

    /// <summary>T6 — 이징·클립 표·별칭·샘플러가 정본 `ProChar` 와 같다.</summary>
    public class HeroClipTests
    {
        [Test]
        public void 이징_6종이_정본과_같다()
        {
            var e = J.Obj(HeroVectors.Doc["ease"]);
            var fns = new Dictionary<string, Func<double, double>>
            {
                { "ease", HeroEase.Smooth }, { "easeOut", HeroEase.Out }, { "easeIn", HeroEase.In },
                { "easeBack", HeroEase.Back }, { "easeBounce", HeroEase.Bounce }, { "easeSnap", HeroEase.Snap },
            };
            int n = 0;
            foreach (var kv in e)
            {
                var fn = fns[kv.Key];
                foreach (var row in J.Arr(kv.Value))
                {
                    var a = J.NumArr(row);
                    Assert.AreEqual(a[1], fn(a[0]), 1e-6, kv.Key + "(" + a[0] + ")");
                    n++;
                }
            }
            Assert.AreEqual(6 * 41, n);
            var names = J.Obj(HeroVectors.Consts["EASES"]);
            Assert.AreEqual(names.Count, HeroEase.Names.Length);
            foreach (var pair in HeroEase.Names) Assert.AreEqual(J.Str(names[pair[0]]), pair[1], "EASES." + pair[0]);
            Assert.AreEqual(HeroEase.Smooth(0.3), HeroEase.ByName(null, 0.3), 1e-12, "이징 이름 없음 = smoothstep");
            Assert.AreEqual(HeroEase.Smooth(0.3), HeroEase.ByName("nope", 0.3), 1e-12, "모르는 이름 = smoothstep");
        }

        [Test]
        public void 클립_표_13종이_정본_CLIPS_와_키프레임까지_같다()
        {
            var clips = J.Obj(HeroVectors.Doc["clips"]);
            Assert.AreEqual(clips.Count, HeroClips.All.Length, "클립 수");
            int keys = 0;
            for (int i = 0; i < HeroClips.All.Length; i++)
            {
                var c = HeroClips.All[i];
                Assert.AreEqual(clips.Keys[i], c.Name, "클립 순서");
                var e = J.Obj(clips[c.Name]);
                Assert.AreEqual(J.Num(e["dur"]), c.Dur, 1e-12, c.Name + ".dur");
                Assert.AreEqual(J.Bool(e["loop"]), c.Loop, c.Name + ".loop");
                Assert.AreEqual(J.Bool(e["once"]), c.Once, c.Name + ".once");
                Assert.AreEqual(J.Bool(e["groundPose"]), c.GroundPose, c.Name + ".groundPose");
                var tr = J.Obj(e["tracks"]);
                Assert.AreEqual(tr.Count, c.Tracks.Length, c.Name + " 트랙 수");
                for (int t = 0; t < c.Tracks.Length; t++)
                {
                    Assert.AreEqual(tr.Keys[t], c.Tracks[t].Key, c.Name + " 트랙 순서");
                    var ek = J.Arr(tr[c.Tracks[t].Key]);
                    Assert.AreEqual(ek.Count, c.Tracks[t].Keys.Length, c.Name + "." + c.Tracks[t].Key + " 키 수");
                    for (int k = 0; k < ek.Count; k++)
                    {
                        var row = J.Arr(ek[k]);
                        var ck = c.Tracks[t].Keys[k];
                        Assert.AreEqual(J.Num(row[0]), ck.T, 1e-12, c.Name + "." + c.Tracks[t].Key + "[" + k + "].t");
                        Assert.AreEqual(J.Num(row[1]), ck.V, 1e-12, c.Name + "." + c.Tracks[t].Key + "[" + k + "].v");
                        Assert.AreEqual(row.Count > 2 ? J.Str(row[2]) : null, ck.Ease, c.Name + "." + c.Tracks[t].Key + "[" + k + "].ease");
                        keys++;
                    }
                }
                var cf = J.Arr(e["capeFlat"]);
                if (cf == null) Assert.IsNull(c.CapeFlat, c.Name + ".capeFlat 없음");
                else
                {
                    Assert.AreEqual(cf.Count, c.CapeFlat.Length, c.Name + ".capeFlat 수");
                    for (int k = 0; k < cf.Count; k++)
                    {
                        var row = J.NumArr(cf[k]);
                        Assert.AreEqual(row[0], c.CapeFlat[k].T, 1e-12);
                        Assert.AreEqual(row[1], c.CapeFlat[k].V, 1e-12);
                    }
                }
            }
            Assert.Greater(keys, 900, "키프레임 총수");
            Assert.AreEqual("Idle", HeroClips.All[0].Name);
            Assert.AreEqual("Revive", HeroClips.All[HeroClips.All.Length - 1].Name);
        }

        [Test]
        public void 별칭_해석이_정본_resolveClip_과_같다()
        {
            var alias = J.Obj(HeroVectors.Doc["alias"]);
            Assert.AreEqual(15, alias.Count);
            foreach (var kv in alias)
            {
                var e = J.Obj(kv.Value);
                Assert.AreEqual(J.Str(e["clip"]), HeroClips.Resolve(J.StrArr(e["cands"])), "alias " + kv.Key);
            }
            var re = J.Arr(HeroVectors.Consts["CLIP_ALIAS"]);
            Assert.AreEqual(re.Count, HeroClips.Alias.Length, "CLIP_ALIAS 줄 수");
            for (int i = 0; i < re.Count; i++)
            {
                var row = J.StrArr(re[i]);
                Assert.AreEqual(row[0], HeroClips.Alias[i].Key.ToString(), "정규식 " + i);
                Assert.AreEqual(row[1], HeroClips.Alias[i].Value, "별칭 " + i);
            }
            Assert.IsNull(HeroClips.Resolve(null));
            Assert.IsNull(HeroClips.Resolve(new string[0]));
        }

        [Test]
        public void 트랙_샘플러가_정본_sample_과_같다()
        {
            var rows = J.Arr(HeroVectors.Doc["sample"]);
            Assert.Greater(rows.Count, 900);
            foreach (var r in rows)
            {
                var a = J.Arr(r);
                var clip = HeroClips.Get(J.Str(a[0]));
                var keys = clip.Track(J.Str(a[1])).Keys;
                Assert.AreEqual(J.Num(a[3]), HeroClips.Sample(keys, J.Num(a[2])), 1e-6, J.Str(a[0]) + "." + J.Str(a[1]) + "@" + J.Num(a[2]));
            }
        }
    }

    /// <summary>T6 — 박스 리그 스펙이 정본 createKnight(박스 모드)와 같다.</summary>
    public class HeroRigSpecTests
    {
        static void PoseEq(JsonObject e, BonePose p, string what)
        {
            Assert.AreEqual(J.Num(e["rx"]), p.Rx, 1e-9, what + ".rx"); Assert.AreEqual(J.Num(e["ry"]), p.Ry, 1e-9, what + ".ry"); Assert.AreEqual(J.Num(e["rz"]), p.Rz, 1e-9, what + ".rz");
            Assert.AreEqual(J.Num(e["px"]), p.Px, 1e-9, what + ".px"); Assert.AreEqual(J.Num(e["py"]), p.Py, 1e-9, what + ".py"); Assert.AreEqual(J.Num(e["pz"]), p.Pz, 1e-9, what + ".pz");
            Assert.AreEqual(J.Num(e["sx"]), p.Sx, 1e-9, what + ".sx"); Assert.AreEqual(J.Num(e["sy"]), p.Sy, 1e-9, what + ".sy"); Assert.AreEqual(J.Num(e["sz"]), p.Sz, 1e-9, what + ".sz");
        }

        [Test]
        public void 치수_본_계층_베이스_포즈가_정본과_같다()
        {
            var rig = J.Obj(HeroVectors.Doc["rig"]);
            var mc = J.Obj(rig["mc"]);
            Assert.AreEqual(J.Num(mc["PX"]), HeroRigSpec.PX, 1e-12);
            Assert.AreEqual(J.Num(mc["headS"]), HeroRigSpec.HeadS, 1e-12);
            Assert.AreEqual(J.Num(mc["torW"]), HeroRigSpec.TorW, 1e-12); Assert.AreEqual(J.Num(mc["torH"]), HeroRigSpec.TorH, 1e-12); Assert.AreEqual(J.Num(mc["torD"]), HeroRigSpec.TorD, 1e-12);
            Assert.AreEqual(J.Num(mc["limbW"]), HeroRigSpec.LimbW, 1e-12); Assert.AreEqual(J.Num(mc["limbH"]), HeroRigSpec.LimbH, 1e-12); Assert.AreEqual(J.Num(mc["limbD"]), HeroRigSpec.LimbD, 1e-12);
            Assert.AreEqual(J.Num(mc["armW"]), HeroRigSpec.ArmW, 1e-12); Assert.AreEqual(J.Num(mc["FEET"]), HeroRigSpec.Feet, 1e-12);
            Assert.AreEqual(J.Num(mc["hipY"]), HeroRigSpec.HipY, 1e-12); Assert.AreEqual(J.Num(mc["torTop"]), HeroRigSpec.TorTop, 1e-12); Assert.AreEqual(J.Num(mc["spineY"]), HeroRigSpec.SpineY, 1e-12);
            Assert.AreEqual(J.Num(rig["bodyScale"]), HeroRigSpec.BodyScale, 1e-12);
            Assert.AreEqual(J.Num(rig["groundY"]), HeroRigSpec.GroundY, 1e-12);
            HeroVectors.ArrEq(rig["outerScale"], new[] { HeroRigSpec.BodyScale, HeroRigSpec.BodyScale, HeroRigSpec.BodyScale }, 1e-9, "outer.scale");
            Assert.IsTrue(J.Bool(rig["simple"]));
            Assert.AreEqual(J.Str(rig["handL"]), HeroRigSpec.HandLBone); Assert.AreEqual(J.Str(rig["handR"]), HeroRigSpec.HandRBone);

            var spec = HeroRigSpec.Default;
            PoseEq(J.Obj(J.Obj(rig["root"])["base"]), spec.RootBase, "root");
            var bones = J.Obj(rig["bones"]);
            Assert.AreEqual(bones.Count, spec.Bones.Count, "본 수");
            for (int i = 0; i < spec.Bones.Count; i++)
            {
                var b = spec.Bones[i];
                Assert.AreEqual(bones.Keys[i], b.Name, "본 순서");
                var e = J.Obj(bones[b.Name]);
                Assert.AreEqual(J.Str(e["parent"]), b.Parent, b.Name + ".parent");
                PoseEq(J.Obj(e["base"]), b.Base, b.Name);
            }
        }

        [Test]
        public void 살색_박스_6_과_얼굴_디캘_7_이_정본과_같다()
        {
            var rig = J.Obj(HeroVectors.Doc["rig"]);
            var spec = HeroRigSpec.Default;
            var boxes = J.Arr(rig["boxes"]);
            Assert.AreEqual(6, boxes.Count); Assert.AreEqual(boxes.Count, spec.Boxes.Count);
            for (int i = 0; i < boxes.Count; i++)
            {
                // 정본 traverse 는 트리 순서(머리 박스가 척추 박스보다 먼저) · 스펙은 add 호출 순서 — 부모 본으로 짝짓는다(본마다 박스 하나)
                var e = J.Obj(boxes[i]);
                HeroBoxDef b = null;
                foreach (var cand in spec.Boxes) if (cand.Parent == J.Str(e["parent"])) b = cand;
                Assert.IsNotNull(b, "box " + i + " parent " + J.Str(e["parent"]));
                HeroVectors.ArrEq(e["pos"], new[] { b.X, b.Y, b.Z }, 1e-6, "box " + i + " pos");
                HeroVectors.ArrEq(e["size"], new[] { b.W, b.H, b.D }, 1e-6, "box " + i + " size");
                Assert.AreEqual((int)J.Num(e["color"]), b.Color, "box " + i + " color");
                Assert.AreEqual(J.Num(e["rough"]), b.Rough, 1e-12);
                Assert.IsFalse(J.Bool(e["basic"])); Assert.IsTrue(J.Bool(e["flat"]));
            }
            var decals = J.Arr(rig["decals"]);
            Assert.AreEqual(7, decals.Count); Assert.AreEqual(decals.Count, spec.Decals.Count);
            for (int i = 0; i < decals.Count; i++)
            {
                var e = J.Obj(decals[i]); var d = spec.Decals[i];
                Assert.AreEqual(J.Str(e["parent"]), d.Parent, "decal " + i);
                HeroVectors.ArrEq(e["pos"], new[] { d.X, d.Y, d.Z }, 1e-6, "decal " + i + " pos");
                HeroVectors.ArrEq(e["size"], new[] { d.W, d.H }, 1e-6, "decal " + i + " size");
                Assert.AreEqual((int)J.Num(e["color"]), d.Color, "decal " + i + " color");
                Assert.IsTrue(J.Bool(e["basic"]));
                Assert.AreEqual(J.Bool(e["toneMapped"]), d.ToneMapped, "decal " + i + " toneMapped");
            }
        }
    }

    /// <summary>T6 — 마인크래프트 파지(applyWeaponGrip)와 스윙 시계가 정본과 같다.</summary>
    public class WeaponGripTests
    {
        [Test]
        public void 상수가_정본과_같다()
        {
            var c = HeroVectors.Consts;
            Assert.AreEqual(J.Num(c["MC_CARRY_X"]), WeaponGrip.MC_CARRY_X, 1e-12);
            Assert.AreEqual(J.Num(c["MC_GRIP_PULL"]), WeaponGrip.MC_GRIP_PULL, 1e-12);
            var rs = J.Obj(c["RANGED_SHAPES"]);
            Assert.AreEqual(rs.Count, WeaponGrip.RangedShapes.Count);
            foreach (var kv in rs) Assert.IsTrue(WeaponGrip.RangedShapes.Contains(kv.Key), kv.Key);
            var table = J.Obj(c["WEAPON_GRIP"]);
            Assert.AreEqual(table.Count, WeaponGrip.Table.Count, "WEAPON_GRIP 종 수");
            foreach (var kv in table)
            {
                var e = J.Obj(kv.Value); var g = WeaponGrip.Table[kv.Key];
                HeroVectors.ArrEq(e["rot"], g.Rot, 1e-12, kv.Key + ".rot");
                if (e["pos"] != null) HeroVectors.ArrEq(e["pos"], g.Pos, 1e-12, kv.Key + ".pos"); else Assert.IsNull(g.Pos, kv.Key + ".pos");
                Assert.AreEqual(J.Str(e["hand"]), g.Hand, kv.Key + ".hand");
                Assert.AreEqual(J.NumOrNull(e["scale"]), g.Scale, kv.Key + ".scale");
                HeroVectors.PoseEq(e["pose"], g.Pose, kv.Key);
            }
            var clamp = J.Obj(c["CLAMP"]);
            Assert.AreEqual(J.Num(clamp["rxMin"]), HeroPoseSolver.ClampRxMin); Assert.AreEqual(J.Num(clamp["rxMax"]), HeroPoseSolver.ClampRxMax);
            Assert.AreEqual(J.Num(clamp["rzMin"]), HeroPoseSolver.ClampRzMin); Assert.AreEqual(J.Num(clamp["rzMax"]), HeroPoseSolver.ClampRzMax);
            var hit = J.Obj(c["HIT"]);
            Assert.AreEqual(J.Num(hit["dur"]), HeroPoseSolver.HitDuration); Assert.AreEqual(J.Num(hit["rise"]), HeroPoseSolver.HitRise);
            Assert.AreEqual(J.Num(c["BODY_SCALE"]), HeroRigSpec.BodyScale); Assert.AreEqual(J.Num(c["GROUND_Y"]), HeroRigSpec.GroundY);
            // WEAPON_TYPES 의 motion/restX/shape 는 gamedata.json(T2)과 정본 gamedata.js 가 같다
            var wts = J.Obj(c["WEAPON_TYPES"]);
            foreach (var kv in wts)
            {
                var e = J.Obj(kv.Value); var w = HeroVectors.Wt(kv.Key);
                Assert.IsNotNull(w, "gamedata.json 에 없는 무기 " + kv.Key);
                Assert.AreEqual(J.Str(e["motion"]), w.Motion); Assert.AreEqual(J.Num(e["restX"]), w.RestX, 1e-12); Assert.AreEqual(J.Str(e["shape"]), w.Shape); Assert.AreEqual(J.Str(e["kind"]), w.Kind);
            }
        }

        static GripResult Apply(HeroPoseSolver rig, string id)
        {
            var w = HeroVectors.Wt(id);
            return WeaponGrip.Apply(rig, id, w != null ? (double?)w.RestX : null, w != null ? w.Shape : null);
        }

        [Test]
        public void 무기_전_종의_파지가_정본_applyWeaponGrip_과_같다()
        {
            var grip = J.Obj(HeroVectors.Doc["grip"]);
            Assert.GreaterOrEqual(grip.Count, 50);
            var rig = new HeroPoseSolver(HeroRigSpec.Default);
            foreach (var kv in grip)
            {
                string id = kv.Key == "__unknown__" ? "noSuchWeapon" : kv.Key;
                var e = J.Obj(kv.Value);
                var r = Apply(rig, id);
                Assert.AreEqual(J.Str(e["shape"]), r.Shape, id + " shape");
                Assert.AreEqual(J.Str(e["parent"]), r.ParentBone, id + " parent");
                HeroVectors.ArrEq(e["pos"], r.Pos, 1e-6, id + " pos");
                HeroVectors.ArrEq(e["rot"], r.Rot, 1e-6, id + " rot");
                HeroVectors.ArrEq(e["quat"], r.Quat, 1e-6, id + " quat");
                HeroVectors.ArrEq(e["scale"], new[] { r.Scale, r.Scale, r.Scale }, 1e-6, id + " scale");
                Assert.AreEqual(J.Num(e["restX"]), r.RestX, 1e-6, id + " restX");
                Assert.AreEqual(J.Num(e["restX"]), rig.RestX, 1e-6, id + " rig.RestX");
                HeroVectors.PoseEq(e["restPose"], rig.RestPose, id);
                Assert.AreEqual(J.Num(e["elbowFix"]), r.ElbowFix, 1e-6, id + " elbowFix");
                Assert.AreEqual(J.Num(e["bladeRoll"]), r.BladeRoll, 1e-6, id + " bladeRoll");
                HeroVectors.ArrEq(e["gripRot"], r.GripRot, 1e-6, id + " gripRot");
                HeroVectors.ArrEq(e["gripPos"], r.GripPos, 1e-6, id + " gripPos");
                Assert.AreEqual(J.Bool(e["shieldVisible"]), r.ShieldVisible, id + " shield");
                Assert.AreEqual(J.Bool(e["melee"]), r.Melee, id + " melee");
                Assert.IsNull(rig.RidePose);
            }
        }

        [Test]
        public void 근접_파지는_MC_CARRY_X_로_덮고_날_롤은_자루_축_우측곱이다()
        {
            var rig = new HeroPoseSolver(HeroRigSpec.Default);
            var sword = Apply(rig, "sword");
            // 롤을 빼면 정확히 (MC_CARRY_X + elbowFix, 0, rot[2]) — 우측곱은 자루 방향을 안 바꾼다
            var noRoll = ThreeQuat.Multiply(sword.Quat, ThreeQuat.FromAxisAngle(0, 1, 0, -sword.BladeRoll));
            HeroVectors.ArrEq(new List<object> { WeaponGrip.MC_CARRY_X + sword.ElbowFix, 0.0, -0.3 }, ThreeQuat.ToEulerXYZ(noRoll), 1e-9, "sword 롤 제거");
            Assert.AreEqual(-Math.PI / 2, sword.BladeRoll, 1e-12, "검 날 롤 −π/2");
            // 팔꿈치 거치·좌우 기울기가 0 인 파지(scythe 꼴 · pose 없음)는 무기 로컬 회전이 정확히 (MC_CARRY_X, −π/2, 0)
            WeaponGrip.Table["__t6probe__"] = new GripDef { Rot = new[] { 0.62, -Math.PI / 2, 0.0 } };
            try
            {
                var probe = WeaponGrip.Apply(rig, "__t6probe__", null, null);
                HeroVectors.ArrEq(new List<object> { WeaponGrip.MC_CARRY_X, -Math.PI / 2, 0.0 }, probe.Rot, 1e-9, "probe rot");
                Assert.AreEqual(0, probe.ElbowFix, 1e-12);
                Assert.IsNull(rig.RestPose);
            }
            finally { WeaponGrip.Table.Remove("__t6probe__"); }
            // 원거리는 표의 rot 그대로 · 왼손 · restX 0 · 방패 숨김
            var bow = Apply(rig, "bow");
            Assert.AreEqual("shoulderL", bow.ParentBone); Assert.AreEqual(0, bow.BladeRoll); Assert.AreEqual(0, rig.RestX); Assert.IsFalse(bow.ShieldVisible);
            Assert.AreEqual(WeaponGrip.WeaponScale * 0.6, bow.Scale, 1e-12, "활계 0.6 축소");
            // 빈 손이 바를 잡는 합성(T11 훅) — 무기 안 든 쪽 어깨·팔꿈치가 restPose 에 더해지고 오른손이 빈 손이면 restX 0
            var reach = WeaponGrip.Apply(rig, "bow", HeroVectors.Wt("bow").RestX, "bow", null, new FreeHandReach { Shoulder = -1.2, ShoulderZ = 0.3, Elbow = -0.4 });
            var sh = rig.RestPose.Get("shoulderR"); Assert.IsNotNull(sh); Assert.AreEqual(-1.2, sh.Rx, 1e-12); Assert.AreEqual(-0.3, sh.Rz, 1e-12, "오른손은 rz 부호 반전");
            Assert.AreEqual(-0.4, rig.RestPose.Get("elbowR").Rx, 1e-12);
            Assert.AreEqual(0, rig.RestX);
            Assert.AreEqual(-0.12, reach.ElbowFix, 1e-12, "왼손 파지의 elbowFix 는 elbowL");
        }

        [Test]
        public void 스윙_시계가_정본_heroAttack_과_같다()
        {
            var swing = J.Obj(HeroVectors.Doc["swing"]);
            var grip = J.Obj(HeroVectors.Doc["grip"]);
            Assert.AreEqual(9, swing.Count);
            foreach (var kv in swing)
            {
                string motion = kv.Key;
                var e = J.Obj(kv.Value);
                Assert.AreEqual(J.Str(e["clip"]), HeroSwing.ClipName(motion), motion + " clip");
                Assert.AreEqual(J.Num(e["atkT"]), HeroSwing.AttackTime(motion), 1e-6, motion + " atkT");
                Assert.AreEqual(J.Num(e["contact"]), HeroSwing.Contact(motion), 1e-12, motion + " contact");
                Assert.AreEqual(J.Num(e["spd"]), HeroSwing.SwingSpd, 1e-12);
                double g0, g1, g2;
                HeroSwing.Gates(motion, out g0, out g1, out g2);
                Assert.AreEqual(J.Num(e["G0"]), g0, 1e-6, motion + " G0"); Assert.AreEqual(J.Num(e["G1"]), g1, 1e-6, motion + " G1"); Assert.AreEqual(J.Num(e["G2"]), g2, 1e-6, motion + " G2");
                Assert.AreEqual(J.Num(e["GW"]), g1 - g0, 1e-6, motion + " GW");
                var g = J.Obj(grip[J.Str(e["wtype"])]);
                var gripRot = J.NumArr(g["gripRot"]);
                double bladeRoll = J.Num(g["bladeRoll"]);
                foreach (var row in J.Arr(e["q"]))
                {
                    var a = J.Arr(row);
                    double k = J.Num(a[0]);
                    HeroVectors.ArrEq(a[1], HeroSwing.BlendGrip(motion, gripRot, bladeRoll, k), 1e-5, motion + " q@" + k);
                }
                Assert.AreEqual(1, HeroSwing.GripWeight(motion, 0), 1e-12);
                Assert.AreEqual(0, HeroSwing.GripWeight(motion, HeroSwing.Contact(motion)), 1e-12, motion + " 접촉 시각은 중립");
                Assert.AreEqual(1, HeroSwing.GripWeight(motion, 1), 1e-12);
            }
            Assert.AreEqual(HeroSwing.DefaultClipDur / HeroSwing.SwingSpd, HeroSwing.AttackTime("noSuchMotion"), 1e-12, "모르는 동작 = slash 후보 · 0.5 는 클립이 없을 때만");
        }
    }

    /// <summary>T6 — 재생기(play/update/hit)가 정본 `ProChar.update` 를 프레임 단위로 재현한다.</summary>
    public class HeroPoseSolverTests
    {
        static readonly string[] Names = { "sword", "axe", "spear", "hammer", "boneDagger", "bow", "gun", "staff" };

        static void Load(HeroPoseSolver rig, string wt)
        {
            var g = J.Obj(J.Obj(HeroVectors.Doc["grip"])[wt]);
            rig.RestX = J.Num(g["restX"]);
            rig.RestPose = HeroVectors.Pose(g["restPose"]);
        }

        static void Ride(HeroPoseSolver rig)
        {
            var ride = HeroVectors.Pose(HeroVectors.Consts["RIDE_STAND_POSE"]);
            rig.RestPose = PoseAdd.Merge(rig.RestPose, ride);
            rig.RidePose = ride;
        }

        /// <summary>hero_vectors.js 의 `scene()` 설정을 이름으로 재현한다.</summary>
        static void Setup(string name, HeroPoseSolver rig, int[] done)
        {
            switch (name)
            {
                case "idle": rig.Play("Idle"); break;
                case "walk": rig.Play("Walking"); break;
                case "walk-restX": rig.RestX = 0.35; rig.Play(new[] { "Walking", "Idle" }); break;
                case "slash-sword": Load(rig, "sword"); rig.Play(HeroSwing.Candidates("slash"), true, 1.25, () => done[0]++); break;
                case "chop-axe-then-idle": Load(rig, "axe"); rig.Play(HeroSwing.Candidates("chop"), true, 1.25, () => { done[0]++; rig.Play("Idle"); }); break;
                case "thrust-spear": Load(rig, "spear"); rig.Play(HeroSwing.Candidates("thrust"), true, 1.25); break;
                case "slam-hammer": Load(rig, "hammer"); rig.Play(HeroSwing.Candidates("slam"), true, 1.25); break;
                case "double-dagger": Load(rig, "boneDagger"); rig.Play(HeroSwing.Candidates("double"), true, 1.25); break;
                case "bow": Load(rig, "bow"); rig.Play(HeroSwing.Candidates("bow"), true, 1.25); break;
                case "gun": Load(rig, "gun"); rig.Play(HeroSwing.Candidates("gun"), true, 1.25); break;
                case "cast-throw": Load(rig, "staff"); rig.Play(HeroSwing.Candidates("cast"), true, 1.25, () => { done[0]++; rig.Play(HeroSwing.Candidates("throw"), true, 1.25, () => done[0]++); }); break;
                case "death-revive": rig.Play("Death_A", true, 1, () => { done[0]++; rig.Play("Revive", true, 1, () => done[0]++); }); break;
                case "ride-idle": Load(rig, "sword"); Ride(rig); rig.Play("Idle"); break;
                case "ride-slash": Load(rig, "sword"); Ride(rig); rig.Play(HeroSwing.Candidates("slash"), true, 1.25); break;
                case "hit-idle": rig.Play("Idle"); rig.Hit(0.2); break;
                case "hit-during-slash": Load(rig, "sword"); rig.Play(HeroSwing.Candidates("slash"), true, 1.25); rig.Hit(0.6); break;
                case "hit-during-death": rig.Play("Death_A", true); rig.Hit(0.9); break;
                case "loop-restart-ignored": rig.Play("Idle"); rig.Update(0.5); rig.Play("Idle"); break;
                case "unknown-clip": rig.Play("Idle"); rig.Play("Nope"); break;
                case "big-dt-wrap": rig.Play("Walking"); break;
                default: Assert.Fail("모르는 장면 " + name); break;
            }
        }

        static void FrameEq(JsonObject frame, HeroPoseSolver rig, string what)
        {
            var pose = J.Obj(frame["pose"]);
            Assert.AreEqual(J.Str(frame["state"]), rig.State, what + " state");
            Assert.AreEqual(J.Num(frame["t"]), rig.T, 1e-6, what + " _t");
            foreach (var kv in pose)
            {
                var e = J.Obj(kv.Value);
                BonePose b = kv.Key == "root" ? rig.Root : rig.Bone(kv.Key);
                BonePose bs = kv.Key == "root" ? rig.BaseRoot : rig.Base[kv.Key];
                Assert.IsNotNull(b, what + " 본 " + kv.Key);
                HeroVectors.ArrEq(e["r"], new[] { b.Rx, b.Ry, b.Rz }, 1e-5, what + " " + kv.Key + ".r");
                if (e["p"] != null) HeroVectors.ArrEq(e["p"], new[] { b.Px, b.Py, b.Pz }, 1e-5, what + " " + kv.Key + ".p");
                else HeroVectors.ArrEq(new List<object> { bs.Px, bs.Py, bs.Pz }, new[] { b.Px, b.Py, b.Pz }, 1e-9, what + " " + kv.Key + ".p=base");
                if (e["s"] != null) HeroVectors.ArrEq(e["s"], new[] { b.Sx, b.Sy, b.Sz }, 1e-5, what + " " + kv.Key + ".s");
                else HeroVectors.ArrEq(new List<object> { bs.Sx, bs.Sy, bs.Sz }, new[] { b.Sx, b.Sy, b.Sz }, 1e-9, what + " " + kv.Key + ".s=base");
            }
        }

        [Test]
        public void 장면_20개를_프레임마다_정본과_대조한다()
        {
            var scenes = J.Arr(HeroVectors.Doc["update"]);
            Assert.AreEqual(20, scenes.Count);
            int frames = 0;
            foreach (var s in scenes)
            {
                var sc = J.Obj(s);
                string name = J.Str(sc["name"]);
                var rig = new HeroPoseSolver(HeroRigSpec.Default);
                var done = new int[1];
                Setup(name, rig, done);
                var fr = J.Arr(sc["frames"]);
                for (int i = 0; i < fr.Count; i++)
                {
                    var f = J.Obj(fr[i]);
                    rig.Update(J.Num(f["dt"]));
                    FrameEq(f, rig, name + " #" + i);
                    frames++;
                }
                Assert.AreEqual((int)J.Num(sc["done"]), done[0], name + " onDone 호출 수");
            }
            Assert.Greater(frames, 500);
        }

        [Test]
        public void 재생_규칙_루프_중복_금지_once_는_1에서_멈춤_모르는_클립_무시()
        {
            var rig = new HeroPoseSolver(HeroRigSpec.Default);
            Assert.IsFalse(rig.Play("Nope"), "모르는 클립");
            Assert.IsNull(rig.Clip);
            rig.Update(0.1);
            Assert.AreEqual(0, rig.Bone("spine").Rx, "클립이 없으면 베이스 그대로");
            Assert.IsTrue(rig.Play("Idle"));
            Assert.AreEqual("Idle", rig.State);
            rig.Update(0.7);
            Assert.IsFalse(rig.Play("Idle"), "같은 루프 클립 재시작 금지");
            Assert.AreEqual(0.7, rig.T, 1e-12);
            int done = 0;
            Assert.IsTrue(rig.Play(HeroSwing.Candidates("slash"), true, HeroSwing.SwingSpd, () => done++));
            Assert.AreEqual("", rig.State, "once 클립은 state 를 비운다");
            Assert.AreEqual("slash", rig.Clip.Name);
            for (int i = 0; i < 10; i++) rig.Update(0.1);
            Assert.AreEqual(1, done, "onDone 은 한 번");
            Assert.AreEqual(1, rig.ClipT, 1e-12, "once 는 1 에서 멈춘다");
            Assert.AreEqual(-0.14, rig.Bone("shoulderR").Rx, 1e-9, "slash 끝값 = Idle 0초 포즈(swing-snap ⓐ)");
            Assert.IsTrue(rig.Play("Death_A"), "Death 는 표에서 once");
            Assert.IsTrue(rig.Once);
            Assert.IsTrue(rig.Clip.GroundPose);
        }

        [Test]
        public void 어깨_클램프와_피격_플린치()
        {
            var rig = new HeroPoseSolver(HeroRigSpec.Default);
            rig.Play(HeroSwing.Candidates("chop"), true, 1);
            rig.Update(0.55 * 0.2881);   // chop 와인드업 정점 −2.40 → 클램프 안(−2.95)
            Assert.Greater(rig.Bone("shoulderR").Rx, HeroPoseSolver.ClampRxMin - 1e-9);
            rig.RestPose = new PoseAdd().Add("shoulderR", 0, 0, -3.0);
            rig.Play("Idle");
            rig.Update(0.01);
            Assert.AreEqual(HeroPoseSolver.ClampRzMin, rig.Bone("shoulderR").Rz, 1e-9, "rz 하한 클램프");
            rig.RestPose = new PoseAdd().Add("shoulderL", 0, 0, 3.0);
            rig.Update(0.01);
            Assert.AreEqual(HeroPoseSolver.ClampRzMax, rig.Bone("shoulderL").Rz, 1e-9, "rz 상한 클램프");
            rig.Simple = false;
            rig.Update(0.01);
            Assert.Greater(rig.Bone("shoulderL").Rz, 2.0, "박스 리그가 아니면 클램프 없음");

            rig = new HeroPoseSolver(HeroRigSpec.Default);
            rig.Play("Idle");
            rig.Hit(1.0);
            Assert.AreEqual(1.0, rig.HitAmp, 1e-12, "진폭 상한 1.0");
            rig.Update(0.018);   // 즉발 스냅 구간(6%)
            Assert.Greater(rig.Bone("neck").Rz, 0, "머리가 젖혀진다");
            for (int i = 0; i < 30; i++) rig.Update(0.02);
            Assert.AreEqual(0, rig.HitT, 1e-12, "0.30초 뒤 소진");
            double neckIdle = rig.Bone("neck").Rz;
            Assert.AreEqual(0, neckIdle, 1e-9, "플린치가 끝나면 Idle 로 돌아온다");
        }
    }
}
