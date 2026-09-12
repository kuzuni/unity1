using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Forge.Core.Data;
using Forge.Core.Hero;
using Forge.Game.Hero;
using Forge.Game.Voxel;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T6 — 영웅 박스 리그가 서고(본 계층 · 박스 6 · 디캘 7 · BODY_SCALE), 근접 스윙 한 사이클의 어깨 각 곡선이 원작 키프레임과 ±5% 이고, 무기 로컬 회전이 마크 파지(MC_CARRY_X · 날 롤 −π/2)인가.
    /// 빨간 로그가 나면 러너가 실패시킨다(콘솔 빨강 0).
    /// </summary>
    public class HeroTests
    {
        static GameData _data;
        static GameData Data
        {
            get
            {
                if (_data != null) return _data;
                _data = GameData.LoadDirectory(System.IO.Path.Combine(Application.streamingAssetsPath, "data"));
                return _data;
            }
        }

        static WeaponType Wt(string id)
        {
            WeaponType w;
            return Data.Defs.WeaponTypes.TryGet(id, out w) ? w : null;
        }

        static HeroRig Make()
        {
            var rig = HeroRig.Create(null, "HeroT6");
            rig.ManualStep = true;
            return rig;
        }

        [UnityTest]
        public IEnumerator 박스_리그가_정본_계층_치수로_선다()
        {
            var rig = Make();
            yield return null;
            Assert.AreEqual(13, rig.Bones.Count, "본 13(pelvis·hip/knee L/R·spine·cape·shoulder/elbow L/R·neck·head)");
            Assert.AreEqual(6, rig.Boxes.Count, "살색 박스 6");
            Assert.AreEqual(7, rig.Decals.Count, "얼굴 디캘 7");
            Assert.AreEqual((float)HeroRigSpec.BodyScale, rig.transform.localScale.y, 1e-5f, "BODY_SCALE 역배율");
            Assert.AreSame(rig.Root, rig.Bone("pelvis").parent);
            Assert.AreSame(rig.Bone("pelvis"), rig.Bone("hipL").parent);
            Assert.AreSame(rig.Bone("spine"), rig.Bone("shoulderR").parent);
            Assert.AreSame(rig.Bone("neck"), rig.Bone("head").parent);
            Assert.AreEqual((float)HeroRigSpec.PelvisY, rig.Bone("pelvis").localPosition.y, 1e-5f);
            Assert.AreEqual(-0.1f, rig.Bone("hipL").localPosition.x, 1e-5f);
            Assert.AreEqual(0.3f, rig.Bone("shoulderR").localPosition.x, 1e-5f);
            Assert.AreEqual((float)HeroRigSpec.RootY, rig.Root.localPosition.y, 1e-5f);
            foreach (var mr in rig.Boxes) { Assert.IsNotNull(mr.sharedMaterial); Assert.IsNotNull(mr.GetComponent<MeshFilter>().sharedMesh); Assert.AreEqual(24, mr.GetComponent<MeshFilter>().sharedMesh.vertexCount, "박스 = 6면 × 4정점(플랫)"); }
            foreach (var mr in rig.Decals) Assert.AreEqual(4, mr.GetComponent<MeshFilter>().sharedMesh.vertexCount);
            // 디캘은 머리 앞면(three +z → 유니티 −z) 에 붙는다
            foreach (var mr in rig.Decals) Assert.Less(mr.transform.localPosition.z, -0.2f, "디캘 z 는 머리 앞면 바깥(−z)");
            // 전신 높이·발바닥: 월드 bbox
            var b = new Bounds(rig.Boxes[0].bounds.center, Vector3.zero);
            foreach (var mr in rig.Boxes) b.Encapsulate(mr.bounds);
            double headTop = (HeroRigSpec.RootY + HeroRigSpec.PelvisY + HeroRigSpec.SpineY + HeroRigSpec.NeckY + HeroRigSpec.HeadY + rig.Spec.Boxes[5].Y + HeroRigSpec.HeadS / 2) * HeroRigSpec.BodyScale;
            double feet = (HeroRigSpec.RootY + HeroRigSpec.PelvisY + HeroRigSpec.Feet) * HeroRigSpec.BodyScale;
            Assert.AreEqual((float)headTop, b.max.y, 0.01f, "머리 윗면 월드 y");
            Assert.AreEqual((float)feet, b.min.y, 0.01f, "발바닥 월드 y");
            UnityEngine.Object.Destroy(rig.gameObject);
        }

        [UnityTest]
        public IEnumerator 무기_로컬_회전은_마크_파지_MC_CARRY_X_와_날_롤이다()
        {
            var rig = Make();
            var grip = rig.Equip("sword", Wt("sword"));
            yield return null;
            Assert.AreSame(rig.Bone("shoulderR"), rig.WeaponMount.parent, "무기는 어깨 뼈에 직접(팬텀 팔꿈치 아래가 아니라)");
            Assert.AreEqual(1, rig.WeaponMount.childCount, "막대 한 자루");
            var expPos = ThreeSpace.Pos(0, -(HeroRigSpec.LimbH - WeaponGrip.MC_GRIP_PULL), 0);
            Assert.AreEqual(0, Vector3.Distance(expPos, rig.WeaponMount.localPosition), 1e-5f, "파지점 = 어깨 로컬 −(limbH − MC_GRIP_PULL) = −0.50");
            Assert.AreEqual((float)WeaponGrip.WeaponScale, rig.WeaponMount.localScale.x, 1e-5f, "weapon-size-2x 2.44");
            // 근접: x = MC_CARRY_X(+ 팔꿈치 프레임 보정) · 오일러 y 0 · z = 표의 rot[2] · 그 뒤 자루 축(로컬 y) 롤 −π/2 를 우측곱
            Assert.AreEqual(-Math.PI / 2, grip.BladeRoll, 1e-9);
            var expected = ThreeSpace.Rot(WeaponGrip.MC_CARRY_X + grip.ElbowFix, 0, -0.3) * Quaternion.AngleAxis(-(float)(grip.BladeRoll * Mathf.Rad2Deg), Vector3.up);
            Assert.Less(Quaternion.Angle(expected, rig.WeaponMount.localRotation), 0.01f, "무기 로컬 회전 = Rx(MC_CARRY_X+fix)·Rz(rot2)·Ry(−π/2)");
            Assert.Less(Quaternion.Angle(HeroRig.ToUnity(grip.Quat), rig.WeaponMount.localRotation), 0.01f, "Core 쿼터니언 변환과 일치");
            // 팔꿈치 거치·좌우 기울기가 없는 파지에서는 정확히 (MC_CARRY_X, −π/2, 0)
            WeaponGrip.Table["__t6probe__"] = new GripDef { Rot = new[] { 0.62, -Math.PI / 2, 0.0 } };
            try
            {
                var probe = rig.Equip("__t6probe__", null);
                yield return null;
                var exp2 = ThreeSpace.Rot(WeaponGrip.MC_CARRY_X, -Math.PI / 2, 0);
                Assert.Less(Quaternion.Angle(exp2, rig.WeaponMount.localRotation), 0.01f, "(MC_CARRY_X, −π/2, 0)");
                Assert.AreEqual(WeaponGrip.MC_CARRY_X, probe.Rot[0], 1e-9); Assert.AreEqual(-Math.PI / 2, probe.Rot[1], 1e-9); Assert.AreEqual(0, probe.Rot[2], 1e-9);
            }
            finally { WeaponGrip.Table.Remove("__t6probe__"); }
            // 활: 왼어깨 · 표의 rot 그대로 · 0.6 축소
            rig.Equip("bow", Wt("bow"));
            yield return null;
            Assert.AreSame(rig.Bone("shoulderL"), rig.WeaponMount.parent);
            Assert.Less(Quaternion.Angle(ThreeSpace.Rot(0.83, 0, 0), rig.WeaponMount.localRotation), 0.01f, "bow rot[0] 0.95 + elbowFix −0.12");
            Assert.AreEqual((float)(WeaponGrip.WeaponScale * 0.6), rig.WeaponMount.localScale.x, 1e-5f);
            // 무기 없음 = club(정본 `w ? w.wtype : 'club'`)
            rig.Equip(null, Wt("club"));
            yield return null;
            Assert.AreEqual("club", rig.WtypeId);
            Assert.AreSame(rig.Bone("shoulderR"), rig.WeaponMount.parent);
            UnityEngine.Object.Destroy(rig.gameObject);
        }

        [UnityTest]
        public IEnumerator 근접_스윙_한_사이클의_어깨_각_곡선이_원작_키프레임과_5퍼센트_안이다()
        {
            var rig = Make();
            rig.Equip("sword", Wt("sword"));
            rig.Play("Idle");
            rig.Step(0.1);
            var clip = HeroClips.Get("slash");
            var keys = clip.Track("shoulderR.rx").Keys;   // 예비(0.075, +0.16) → 와인드업(0.27, −2.10) → 홀드 → 타격(0.45) → 팔로스루(0.55, +1.00) → 회복(1, −0.14)
            double range = 0;
            foreach (var k in keys) range = Math.Max(range, Math.Abs(k.V));
            range *= 2;
            Assert.IsTrue(rig.Attack("slash"));
            Assert.IsTrue(rig.Attacking);
            Assert.AreEqual(clip.Dur / HeroSwing.SwingSpd, rig.AttackTime, 1e-9, "atkT = dur / SWING_SPD");
            double atkT = rig.AttackTime, prev = 0;
            var neutral = HeroRig.ToUnity(ThreeQuat.FromEulerXYZ(0, rig.Grip.BladeRoll, 0));
            var gripQ = HeroRig.ToUnity(rig.Grip.Quat);
            for (int i = 1; i < keys.Length; i++)
            {
                double t = keys[i].T;
                rig.Step((t - prev) * atkT);
                prev = t;
                double rx = rig.Solver.Bone("shoulderR").Rx;
                Assert.AreEqual(keys[i].V, rx, range * 0.05, "slash shoulderR.rx @" + t + " (시각 비 예비→타격→회복)");
                // Transform 에도 같은 각이 실린다(three → 유니티)
                Assert.Less(Quaternion.Angle(ThreeSpace.Rot(rx, rig.Solver.Bone("shoulderR").Ry, rig.Solver.Bone("shoulderR").Rz), rig.Bone("shoulderR").localRotation), 0.01f);
                if (Math.Abs(t - HeroSwing.Contact("slash")) < 1e-9)
                    Assert.Less(Quaternion.Angle(neutral, rig.WeaponMount.localRotation), 0.01f, "접촉 시각(0.45)에 무기는 중립(0, 날 롤, 0)");
                yield return null;
            }
            rig.Step(1e-6);   // 부동소수 합이 atkT 에 못 미칠 수 있다 — 한 틱 더
            Assert.IsFalse(rig.Attacking, "atkT 가 지나면 공격 종료");
            Assert.Less(Quaternion.Angle(gripQ, rig.WeaponMount.localRotation), 0.01f, "끝나면 파지각 복원(endAtk)");
            Assert.AreEqual(-0.14, rig.Solver.Bone("shoulderR").Rx, 1e-6, "끝값 = Idle 0초 포즈");
            // 와인드업 정점은 클램프(−2.95) 안이고 rz 는 [−0.5, 0.6] 안
            rig.Attack("chop");
            rig.Step(0.2881 * rig.AttackTime);
            Assert.GreaterOrEqual(rig.Solver.Bone("shoulderR").Rx, HeroPoseSolver.ClampRxMin);
            Assert.LessOrEqual(rig.Solver.Bone("shoulderR").Rz, HeroPoseSolver.ClampRzMax);
            UnityEngine.Object.Destroy(rig.gameObject);
        }

        [UnityTest]
        public IEnumerator 대기_걷기_클립과_피격_플린치가_본을_움직인다()
        {
            var rig = Make();
            rig.Equip("club", Wt("club"));
            Assert.IsTrue(rig.Play("Walking"));
            Assert.AreEqual("Walking", rig.Solver.State);
            var seen = new HashSet<int>();
            float rootY0 = float.NaN;
            for (int i = 0; i < 25; i++)   // 0.66초 사이클의 6할 — 한 바퀴를 다 돌면 root.py 가 제자리로 온다
            {
                rig.Step(1 / 60.0);
                if (float.IsNaN(rootY0)) rootY0 = rig.Root.localPosition.y;
                seen.Add((int)Math.Round(rig.Solver.Bone("hipL").Rx * 10));
                if (i % 5 == 4) yield return null;
            }
            Assert.Greater(seen.Count, 5, "걷기 사이클에서 hipL.rx 가 여러 값을 지난다");
            Assert.AreNotEqual(rootY0, rig.Root.localPosition.y, "러버호스 바운스(root.py)");
            Assert.IsFalse(rig.Play("Walking"), "같은 루프 클립 재시작 금지");
            Assert.IsTrue(rig.Play("Idle"));
            rig.Step(0.5);
            Assert.AreNotEqual(1f, rig.Bone("spine").localScale.y, "대기 스쿼시&스트레치(spine.sy)");
            rig.Hit(0.5);
            rig.Step(0.02);
            Assert.Greater(rig.Solver.Bone("neck").Rz, 0, "피격에 머리가 젖혀진다");
            for (int i = 0; i < 20; i++) rig.Step(0.02);
            Assert.AreEqual(0, rig.Solver.HitT, 1e-9);
            // 자동 갱신도 돈다(ManualStep 끄면 Update 가 Time.deltaTime 을 밟는다)
            rig.ManualStep = false;
            double t0 = rig.Solver.T;
            yield return null;
            yield return null;
            Assert.Greater(rig.Solver.T, t0, "Update 가 시간을 밟는다");
            UnityEngine.Object.Destroy(rig.gameObject);
        }
    }
}
