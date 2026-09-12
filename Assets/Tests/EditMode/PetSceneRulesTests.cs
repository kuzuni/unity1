using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Pets;
using Forge.Core.Voxel;
using Forge.Core.World;

namespace Forge.Tests
{
    /// <summary>T10 벡터(`Assets/Tests/EditMode/Vectors/t10-pets.json` · `tools/pet_scene_vectors.js` 가 정본 `formationSpot` 을 실행하고 update 펫 블록의 식을 실물 표·관절 위에서 계산한 것).</summary>
    static class PetSceneVectors
    {
        static JsonObject _doc;
        public static JsonObject Doc
        {
            get
            {
                if (_doc != null) return _doc;
                string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
                string file = Path.Combine(root, "Assets", "Tests", "EditMode", "Vectors", "t10-pets.json");
                if (!File.Exists(file)) throw new FileNotFoundException("T10 벡터가 없다 — node tools/pet_scene_vectors.js 로 뽑는다: " + file);
                _doc = MiniJson.ParseObject(File.ReadAllText(file));
                return _doc;
            }
        }
        public static JsonObject Consts { get { return J.Obj(Doc["consts"]); } }
        public static SceneDefs Defs { get { return WorldVectors.Defs; } }
    }

    /// <summary>T10 — 정본 펫 출전 규칙(대열 · 등급 스케일 · 몸통 자세 · 관절 드라이버)과 Core `PetFormation`·`PetSceneRules`·`PetPose` 가 같은 입력에 같은 값을 내는가.</summary>
    public class PetSceneRulesTests
    {
        const double Eps = 1e-12;

        [Test]
        public void 씬_상수표_대열_칸이_정본_배포값으로_읽힌다()
        {
            SceneDefs d = PetSceneVectors.Defs;
            JsonObject c = PetSceneVectors.Consts;
            Assert.AreEqual(J.Num(c["CREATURE_YAW"]), d.CreatureYaw, Eps, "CREATURE_YAW");
            var row0 = J.Obj(c["PET_ROW0"]);
            var mounted = J.Arr(row0["mounted"]); var unmounted = J.Arr(row0["unmounted"]);
            Assert.AreEqual(mounted.Count, d.PetRow0Mounted.Length); Assert.AreEqual(unmounted.Count, d.PetRow0Unmounted.Length);
            for (int i = 0; i < mounted.Count; i++)
            {
                double[] m = J.NumArr(mounted[i]), u = J.NumArr(unmounted[i]);
                Assert.AreEqual(m[0], d.PetRow0Mounted[i][0], Eps); Assert.AreEqual(m[1], d.PetRow0Mounted[i][1], Eps);
                Assert.AreEqual(u[0], d.PetRow0Unmounted[i][0], Eps); Assert.AreEqual(u[1], d.PetRow0Unmounted[i][1], Eps);
            }
            var pa = J.Obj(c["PET_ARC"]);
            Assert.IsTrue(d.PetArc.Rear, "펫 대열은 후방 격자"); Assert.AreEqual(J.Int(pa["cols"]), d.PetArc.Cols);
            Assert.AreEqual(J.Num(pa["rx0"]), d.PetArc.Rx0, Eps); Assert.AreEqual(J.Num(pa["rzStep"]), d.PetArc.RzStep, Eps);
            Assert.AreEqual(J.Num(pa["mountedRx"]), d.PetArc.MountedRx, Eps); Assert.AreEqual(J.Num(pa["mountedRz"]), d.PetArc.MountedRz, Eps);
            Assert.IsFalse(d.PetArc.HasZBase, "PET_ARC 에는 zBase 가 없다(undefined)");
            var ma = J.Obj(c["MOUNT_ARC"]);
            Assert.IsFalse(d.MountArc.Rear); Assert.IsTrue(d.MountArc.HasZBase, "MOUNT_ARC 는 zBase 를 가진다");
            Assert.AreEqual(J.Num(ma["zBase"]), d.MountArc.ZBase, Eps); Assert.AreEqual(J.Num(ma["hmin"]), d.MountArc.Hmin, Eps);
            Assert.AreEqual(J.Num(ma["hmax"]), d.MountArc.Hmax, Eps); Assert.AreEqual(J.Num(ma["gap"]), d.MountArc.Gap, Eps);
            ArcSpec sh = d.PetArc.ShiftedForMount();
            Assert.AreEqual(d.PetArc.Rx0 + d.PetArc.MountedRx, sh.Rx0, Eps); Assert.AreEqual(d.PetArc.Rz0 + d.PetArc.MountedRz, sh.Rz0, Eps);
            Assert.AreEqual(d.PetArc.RxStep, sh.RxStep, Eps); Assert.AreEqual(d.PetArc.Cols, sh.Cols);
        }

        static void AssertSpots(string what, List<object> expect, System.Func<int, double[]> got)
        {
            for (int i = 0; i < expect.Count; i++)
            {
                double[] e = J.NumArr(expect[i]); double[] g = got(i);
                Assert.AreEqual(e[0], g[0], Eps, what + " i=" + i + " x"); Assert.AreEqual(e[1], g[1], Eps, what + " i=" + i + " z");
            }
        }

        [Test]
        public void formationSpot_펫_후방_격자가_정본_실행값과_같다_비탑승_탑승()
        {
            SceneDefs d = PetSceneVectors.Defs;
            var f = J.Obj(PetSceneVectors.Doc["formation"]);
            AssertSpots("비탑승", J.Arr(f["petUnmounted"]), i => PetFormation.PetSpot(d, i, false));
            AssertSpots("탑승", J.Arr(f["petMounted"]), i => PetFormation.PetSpot(d, i, true));
            // 앞 3자리는 표 그대로 · 4번째부터 후방 격자(비탑승 −2.72 · 탑승 −2.97) · 홀수 행은 z 반 칸 지그재그
            double[] s3 = PetFormation.PetSpot(d, 3, false), s7 = PetFormation.PetSpot(d, 7, false);
            Assert.AreEqual(-d.PetArc.Rx0, s3[0], Eps); Assert.AreEqual(d.PetArc.Rz0, s3[1], Eps);
            Assert.AreEqual(-(d.PetArc.Rx0 + d.PetArc.RxStep), s7[0], Eps); Assert.AreEqual(d.PetArc.Rz0 + d.PetArc.RzStep * 0.5, s7[1], Eps);
            Assert.AreEqual(-(d.PetArc.Rx0 + d.PetArc.MountedRx), PetFormation.PetSpot(d, 3, true)[0], Eps);
        }

        [Test]
        public void formationSpot_탈것_호가_정본_실행값과_같다_zBase_유무()
        {
            SceneDefs d = PetSceneVectors.Defs;
            var f = J.Obj(PetSceneVectors.Doc["formation"]);
            AssertSpots("탈것 호", J.Arr(f["mount"]), i => PetFormation.Spot(i, d.MountArc, null));
            ArcSpec nz = ArcSpec.From(d.MountArc.Raw); nz.HasZBase = false;
            AssertSpots("zBase 없는 호", J.Arr(f["mountNoZBase"]), i => PetFormation.Spot(i, nz, null));
            // 좌우 짝수 쌍 — 0·1 은 x 부호만 다르다
            double[] a = PetFormation.Spot(0, d.MountArc, null), b = PetFormation.Spot(1, d.MountArc, null);
            Assert.AreEqual(-a[0], b[0], Eps); Assert.AreEqual(a[1], b[1], Eps);
        }

        [Test]
        public void 등급_스케일과_몸짓_표_기본값()
        {
            string[] rar = PetSceneVectors.Defs != null ? DataDir.Game.Defs.Rarities : null;
            var sc = J.Obj(PetSceneVectors.Doc["scale"]);
            foreach (var kv in sc) Assert.AreEqual(J.Num(kv.Value), PetSceneRules.Scale(rar, kv.Key), Eps, "scale " + kv.Key);
            Assert.AreEqual(J.StrArr(PetSceneVectors.Consts["RARITIES"]), rar);
            PetMotionSpec def = PetSceneRules.MotionOf(DataDir.Game.Defs, "없는종");
            Assert.AreEqual(PetSceneRules.DefaultFreq, def.Freq, Eps); Assert.AreEqual(PetSceneRules.DefaultAmp, def.Amp, Eps); Assert.IsFalse(def.Hop);
            PetMotionSpec dog = PetSceneRules.MotionOf(DataDir.Game.Defs, "Dog");
            Assert.IsTrue(dog.Hop, "Dog 은 hop");
            Assert.AreEqual(0, PetSceneRules.XJitter("Cat", 1.2), Eps); Assert.AreNotEqual(0, PetSceneRules.XJitter("Snail", 1.2));
        }

        [Test]
        public void 몸통_자세와_관절_각이_정본_식과_같다_25종_8프레임()
        {
            GameData data = DataDir.Game;
            JsonObject c = PetSceneVectors.Consts;
            double heroX = J.Num(c["heroX"]), worldX = J.Num(c["worldX"]), phase = J.Num(c["phase"]), speed = J.Num(c["speed"]), yaw = J.Num(c["CREATURE_YAW"]);
            var pets = J.Arr(PetSceneVectors.Doc["pets"]);
            Assert.AreEqual(data.Pets.Count, pets.Count, "펫 25종 전부");
            int jointsTotal = 0;
            foreach (object po in pets)
            {
                var p = J.Obj(po);
                string name = J.Str(p["name"]);
                MobModel model = data.Pets.Models.Get(name, null);
                Assert.IsNotNull(model, name);
                MobPlan plan = MobBuilder.Plan(model, 0, PetSceneRules.Vivid, true);
                var vj = J.Arr(p["joints"]);
                Assert.AreEqual(vj.Count, plan.Joints.Count, name + " 관절 수");
                for (int k = 0; k < vj.Count; k++)
                {
                    var e = J.Obj(vj[k]); MobJointPlan g = plan.Joints[k];
                    Assert.AreEqual(J.Str(e["pid"]), g.Part.Pid, name + " 관절 " + k + " pid");
                    Assert.AreEqual(J.Str(e["axis"]), g.Axis); Assert.AreEqual(J.Num(e["base"]), g.Base, 1e-9); Assert.AreEqual(J.Num(e["amp"]), g.Amp, Eps);
                    Assert.AreEqual(J.Num(e["ph"]), g.Ph, Eps); Assert.AreEqual(J.Num(e["f"]), g.F, Eps); Assert.AreEqual(J.Num(e["gain"]), g.Gain, Eps);
                    Assert.AreEqual(J.Bool(e["abs"]), g.Abs); Assert.AreEqual(J.Bool(e["spin"]), g.Spin);
                }
                jointsTotal += vj.Count;
                double[] spot = J.NumArr(p["spot"]);
                PetMotionSpec mo = PetSceneRules.MotionOf(data.Defs, name);
                foreach (object fo in J.Arr(p["frames"]))
                {
                    var f = J.Obj(fo);
                    bool walking = J.Bool(f["walking"]);
                    double t = PetPose.Time(J.Num(f["clock"]), speed, phase);
                    string what = name + " clock=" + J.Num(f["clock"]) + (walking ? " 걷기" : " 대기");
                    Assert.AreEqual(J.Num(f["t"]), t, Eps, what + " t");
                    PetPose pose = PetPose.Body(name, mo, t, heroX, spot[0], spot[1], worldX, walking, yaw);
                    double[] pos = J.NumArr(f["pos"]), rot = J.NumArr(f["rot"]);
                    Assert.AreEqual(pos[0], pose.X, Eps, what + " x"); Assert.AreEqual(pos[1], pose.Y, Eps, what + " y"); Assert.AreEqual(pos[2], pose.Z, Eps, what + " z");
                    Assert.AreEqual(rot[0], pose.Rx, Eps, what + " rx"); Assert.AreEqual(rot[1], pose.Ry, Eps, what + " ry"); Assert.AreEqual(rot[2], pose.Rz, Eps, what + " rz");
                    double[] ang = J.NumArr(f["joints"]);
                    for (int k = 0; k < ang.Length; k++) Assert.AreEqual(ang[k], PetPose.JointAngle(t, mo.Freq, walking, plan.Joints[k]), 1e-9, what + " 관절 " + k);
                }
            }
            Assert.Greater(jointsTotal, 100, "관절 서술이 표에 실제로 있다");
        }
    }
}
