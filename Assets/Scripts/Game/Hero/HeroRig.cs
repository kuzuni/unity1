using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Forge.Core.Data;
using Forge.Core.Hero;
using Forge.Game.Voxel;
using Forge.Game.Render;

namespace Forge.Game.Hero
{
    /// <summary>
    /// 영웅(T6) — 정본 `ProChar.createKnight()` 의 **마인크래프트 박스 리그**(머리1·몸통1·팔다리 각1 큐브 · 얼굴 디캘) + 코드 애니(대기/걷기/근접 스윙 · Animator 안 씀) +
    /// 마크 handheld 무기 파지(`applyWeaponGrip`) + 스윙 시계(`heroAttack` 의 클립·파지 슬러프 부분). 규칙 계산은 전부 Core(<see cref="HeroPoseSolver"/> · <see cref="WeaponGrip"/> · <see cref="HeroSwing"/>)
    /// 이고 여기서는 Transform·Mesh 만 만든다. 좌표: three → 유니티 z 반전(결정 4 · <see cref="ThreeSpace"/>).
    /// 계층: this(outer · BODY_SCALE) → root → pelvis → hipL/R → kneeL/R · spine → cape · shoulderL/R → elbowL/R · neck → head. 무기는 어깨 본 아래 <see cref="WeaponMount"/>.
    /// 돌진·트레일·스우시·링·표적은 전투 씬(T8) 몫이다 — 이 컴포넌트는 «리그·클립·파지» 까지만 안다.
    /// </summary>
    public sealed class HeroRig : MonoBehaviour
    {
        public HeroRigSpec Spec { get; private set; }
        public HeroPoseSolver Solver { get; private set; }
        public Transform Root { get; private set; }
        public readonly Dictionary<string, Transform> Bones = new Dictionary<string, Transform>();
        public readonly List<MeshRenderer> Boxes = new List<MeshRenderer>();
        public readonly List<MeshRenderer> Decals = new List<MeshRenderer>();

        /// <summary>무기 그룹(정본 `weaponG`) — 파지 결과대로 어깨 본 아래에 붙는다. 무기 메시(T15)는 이 아래에 자루 = 로컬 +y 로 세운다.</summary>
        public Transform WeaponMount { get; private set; }
        public GripResult Grip { get; private set; }
        public string WtypeId { get; private set; }
        public WeaponType WeaponDef { get; private set; }

        /// <summary>true 면 Update 가 시간을 안 밟는다(테스트 · 씬이 직접 <see cref="Step"/> 을 부른다).</summary>
        public bool ManualStep;

        public bool Attacking { get; private set; }
        public string AttackMotion { get; private set; }
        /// <summary>진행 중인 공격의 총 길이(초 · 정본 atkT)와 경과.</summary>
        public double AttackTime { get; private set; }
        public double AttackElapsed { get; private set; }
        Action attackDone;

        /// <summary>영웅을 세운다(부모 아래 · 배율 BODY_SCALE · 무기 없음 = 기본 club 파지에 막대 한 자루).</summary>
        public static HeroRig Create(Transform parent = null, string name = "Hero", HeroRigSpec spec = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var rig = go.AddComponent<HeroRig>();
            rig.Build(spec ?? HeroRigSpec.Default);
            return rig;
        }

        void Build(HeroRigSpec spec)
        {
            Spec = spec;
            Solver = new HeroPoseSolver(spec);
            transform.localScale = Vector3.one * (float)HeroRigSpec.BodyScale;
            Root = new GameObject("root").transform;
            Root.SetParent(transform, false);
            Bones.Clear();
            foreach (var b in spec.Bones)
            {
                var t = new GameObject(b.Name).transform;
                Transform parent = b.Parent == "root" ? Root : Bones[b.Parent];
                t.SetParent(parent, false);
                Bones[b.Name] = t;
            }
            foreach (var box in spec.Boxes)
            {
                var mr = Part(Bones[box.Parent], "box", HeroMeshes.Box(box.W, box.H, box.D, box.Color), HeroMeshes.Skin(box.Rough), box.X, box.Y, box.Z, true);
                Boxes.Add(mr);
            }
            foreach (var d in spec.Decals)
            {
                var mr = Part(Bones[d.Parent], "decal", HeroMeshes.Quad(d.W, d.H, d.Color), HeroMeshes.Basic(), d.X, d.Y, d.Z, false);
                Decals.Add(mr);
            }
            WeaponMount = new GameObject("weaponG").transform;
            WeaponMount.SetParent(Bones["shoulderR"], false);
            Apply();
        }

        static MeshRenderer Part(Transform parent, string name, Mesh mesh, Material mat, double x, double y, double z, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = ThreeSpace.Pos(x, y, z);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = shadows;
            EdgePartId.Tag(mr);   // T330 — 파츠 ID 번호를 생성 시각에 굳힌다(윤곽선 넷째 항 · 팔↔몸통·허리↔다리 경계)
            return mr;
        }

        public Transform Bone(string name)
        {
            Transform t;
            return Bones.TryGetValue(name, out t) ? t : null;
        }

        // ── 재생 ────────────────────────────────────────────────────────────────
        /// <summary>정본 `heroPlay(cands, once, timeScale)`.</summary>
        public bool Play(IList<string> cands, bool once = false, double timeScale = 0, Action done = null)
        {
            return Solver.Play(cands, once, timeScale, done);
        }

        public bool Play(string cand, bool once = false, double timeScale = 0, Action done = null)
        {
            return Solver.Play(cand, once, timeScale, done);
        }

        /// <summary>피격 플린치(정본 `ProChar.hit`) — sev = 최대 HP 대비 피해 비중.</summary>
        public void Hit(double sev) { Solver.Hit(sev); }

        // ── 무기 ────────────────────────────────────────────────────────────────
        /// <summary>
        /// 정본 `refreshHeroEquip` 의 무기 부분: 무기 그룹을 비우고 <paramref name="weaponMesh"/>(null 이면 T6 의 «막대 한 자루»)를 세운 뒤 파지(`applyWeaponGrip`)를 먹인다.
        /// <paramref name="def"/> 는 `WEAPON_TYPES[wtypeId]`(없으면 null · restX −0.25 · 모양 = id). 탑승 자세(T11)는 <paramref name="ridePose"/>/<paramref name="reach"/>.
        /// </summary>
        public GripResult Equip(string wtypeId, WeaponType def, Mesh weaponMesh = null, PoseAdd ridePose = null, FreeHandReach reach = null)
        {
            WtypeId = wtypeId ?? WeaponGrip.DefaultWtype;
            WeaponDef = def;
            for (int i = WeaponMount.childCount - 1; i >= 0; i--)
            {
                var old = WeaponMount.GetChild(i);
                old.SetParent(null, false);   // Destroy 는 프레임 끝에 지워진다 — 같은 프레임의 childCount 가 정확하게
                Destroy(old.gameObject);
            }
            var model = new GameObject("weapon");
            model.transform.SetParent(WeaponMount, false);
            model.AddComponent<MeshFilter>().sharedMesh = weaponMesh ?? HeroWeapon.Stick();
            var mr = model.AddComponent<MeshRenderer>();
            mr.sharedMaterial = HeroMeshes.Skin(VoxelMaterials.DefaultRough);
            mr.shadowCastingMode = ShadowCastingMode.On;
            EdgePartId.Tag(mr);   // T330 — 무기도 파츠(정본 heroG 아래 전부 ID_LAYER)
            Grip = WeaponGrip.Apply(Solver, WtypeId, def != null ? (double?)def.RestX : null, def != null ? def.Shape : null, ridePose, reach);
            var parent = Bones[Grip.ParentBone];
            if (WeaponMount.parent != parent) WeaponMount.SetParent(parent, false);
            RestoreGrip();
            Apply();
            return Grip;
        }

        /// <summary>정본 `resetArm`/`endAtk` 의 무기 복원 — 파지 결과(`_gripRot`/`_gripPos`)로.</summary>
        void RestoreGrip()
        {
            if (Grip == null) return;
            WeaponMount.localPosition = ThreeSpace.Pos(Grip.GripPos);
            WeaponMount.localRotation = ToUnity(Grip.Quat);
            WeaponMount.localScale = Vector3.one * (float)Grip.Scale;
        }

        /// <summary>three 쿼터니언(x, y, z, w) → 유니티(z 반전 거울: 축 z 부호 반전 + 각 반전 = (−x, −y, z, w) · <see cref="ThreeSpace.Rot"/> 와 같은 규약).</summary>
        public static Quaternion ToUnity(double[] q)
        {
            return new Quaternion(-(float)q[0], -(float)q[1], (float)q[2], (float)q[3]);
        }

        // ── 공격(스윙 시계) ─────────────────────────────────────────────────────
        /// <summary>장착 무기의 동작(`WEAPON_TYPES.motion` · 없으면 slash)으로 <see cref="Attack(string, Action)"/>.</summary>
        public bool Attack(Action done = null)
        {
            return Attack(WeaponDef != null && WeaponDef.Motion != null ? WeaponDef.Motion : "slash", done);
        }

        /// <summary>
        /// 정본 `heroAttack` 의 리그 경로 중 «클립 + 무기 파지 슬러프»: 클립을 SWING_SPD 배속으로 한 번 재생하고 atkT 동안 무기 각을 파지 ↔ 중립(0, 날 롤, 0) 4구간으로 녹인다.
        /// 끝나면 파지각·위치를 복원한다(endAtk). 돌진·스우시·링·표적 판정은 T8 이 이 시계(<see cref="AttackTime"/> · <see cref="HeroSwing.Contact"/>)에 맞춘다.
        /// </summary>
        public bool Attack(string motion, Action done = null)
        {
            if (Grip == null) Equip(WtypeId, WeaponDef);
            AttackMotion = motion;
            AttackTime = HeroSwing.AttackTime(motion);
            AttackElapsed = 0;
            attackDone = done;
            Attacking = true;
            Solver.Play(HeroSwing.Candidates(motion), true, HeroSwing.SwingSpd);
            BlendGrip(0);
            return true;
        }

        void BlendGrip(double k)
        {
            var q = HeroSwing.BlendGrip(AttackMotion, Grip.GripRot, Grip.BladeRoll, k);
            WeaponMount.localRotation = ToUnity(q);
        }

        void EndAttack()
        {
            Attacking = false;
            RestoreGrip();
            var f = attackDone; attackDone = null;
            if (f != null) f();
        }

        // ── 프레임 ──────────────────────────────────────────────────────────────
        void Update()
        {
            if (!ManualStep) Step(Time.deltaTime);
        }

        /// <summary>한 프레임(초) — 재생기 갱신 → 공격 시계 → Transform 적용.</summary>
        public void Step(double dt)
        {
            Solver.Update(dt);
            if (Attacking)
            {
                AttackElapsed += dt;
                double k = AttackTime > 0 ? Math.Min(1, AttackElapsed / AttackTime) : 1;
                BlendGrip(k);
                if (AttackElapsed >= AttackTime) EndAttack();
            }
            Apply();
        }

        /// <summary>재생기의 본 포즈(three 로컬)를 Transform 에 옮긴다.</summary>
        public void Apply()
        {
            foreach (var name in Solver.BoneNames) ApplyPose(Bones[name], Solver.Bones[name]);
            ApplyPose(Root, Solver.Root);
        }

        static void ApplyPose(Transform t, BonePose p)
        {
            t.localPosition = ThreeSpace.Pos(p.Px, p.Py, p.Pz);
            t.localRotation = ThreeSpace.Rot(p.Rx, p.Ry, p.Rz);
            t.localScale = new Vector3((float)p.Sx, (float)p.Sy, (float)p.Sz);
        }
    }
}
