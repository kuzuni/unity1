using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Forge.Core.Data;
using Forge.Core.Voxel;
using Forge.Game.Voxel;

namespace Forge.Game.SkillFx
{
    /// <summary>
    /// 스킬 액터 한 체(원작 `fxActor` 반환 `{ g, P, id }`) — three 좌표·오일러(라디안)로 다루고 적용할 때만 유니티로 바꾼다(`ThreeSpace` · 결정 4).
    /// 파츠 회전은 원작처럼 축 하나씩 덮어쓴다(`arm.rotation.x = …`) — 표의 기본 회전에서 그 축만 바뀐다.
    /// </summary>
    public sealed class SkillActor
    {
        public string Id;
        public GameObject G;
        public Transform T;
        public readonly Dictionary<string, Transform> P = new Dictionary<string, Transform>();
        readonly Dictionary<string, double[]> rot = new Dictionary<string, double[]>();
        readonly Dictionary<string, double> wingS = new Dictionary<string, double>();
        /// <summary>그룹 오일러(three · rx ry rz).</summary>
        public readonly double[] GRot = { 0, 0, 0 };
        /// <summary>three 좌표.</summary>
        public Vector3 Pos { get; private set; }
        public double Scale { get; private set; } = 1;
        public bool Freed { get; private set; }

        internal SkillActor(string id, GameObject g, Dictionary<string, string> partPath, Dictionary<string, double[]> baseRot, Dictionary<string, double> wings)
        {
            Id = id; G = g; T = g.transform;
            foreach (var kv in partPath)
            {
                Transform n = T.Find(kv.Value);
                if (n == null) continue;
                P[kv.Key] = n;
                double[] b; rot[kv.Key] = baseRot.TryGetValue(kv.Key, out b) ? new[] { b[0], b[1], b[2] } : new double[] { 0, 0, 0 };
            }
            foreach (var kv in wings) wingS[kv.Key] = kv.Value;
        }

        public bool Has(string part) { return P.ContainsKey(part); }
        public Transform Part(string part) { Transform t; return P.TryGetValue(part, out t) ? t : null; }
        /// <summary>날개 좌우 부호(표 `s` · 없으면 wingL −1 · wingR +1 — 원작 `mcFlap` 폴백).</summary>
        public double WingS(string part) { double s; return wingS.TryGetValue(part, out s) ? s : (part == "wingL" ? -1 : 1); }

        public void SetPos(Vector3 threePos) { Pos = threePos; T.localPosition = ThreeSpace.Pos(threePos.x, threePos.y, threePos.z); }
        public void SetPos(double x, double y, double z) { SetPos(new Vector3((float)x, (float)y, (float)z)); }
        public void SetX(double x) { SetPos(x, Pos.y, Pos.z); }
        public void SetY(double y) { SetPos(Pos.x, y, Pos.z); }
        public void SetYaw(double yaw) { GRot[1] = yaw; ThreeSpace.Apply(T, GRot); }
        public void SetGRot(double rx, double ry, double rz) { GRot[0] = rx; GRot[1] = ry; GRot[2] = rz; ThreeSpace.Apply(T, GRot); }
        public void AddGRot(int axis, double d) { GRot[axis] += d; ThreeSpace.Apply(T, GRot); }
        public void SetScale(double s) { Scale = s; T.localScale = Vector3.one * (float)s; }
        public void SetScale3(double sx, double sy, double sz) { Scale = sx; T.localScale = new Vector3((float)sx, (float)sy, (float)sz); }

        /// <summary>파츠 오일러 한 축을 덮어쓴다(원작 `P.armR.rotation.x = v`). 없는 파츠는 무시(원작 `if (arm)`).</summary>
        public void Rot(string part, int axis, double v)
        {
            Transform t; double[] r;
            if (!P.TryGetValue(part, out t) || !rot.TryGetValue(part, out r)) return;
            r[axis] = v;
            ThreeSpace.Apply(t, r);
        }
        public void RotX(string part, double v) { Rot(part, 0, v); }
        public void RotY(string part, double v) { Rot(part, 1, v); }
        public void RotZ(string part, double v) { Rot(part, 2, v); }
        public double GetRot(string part, int axis) { double[] r; return rot.TryGetValue(part, out r) ? r[axis] : 0; }
        public void AddRot(string part, int axis, double d) { Rot(part, axis, GetRot(part, axis) + d); }
        public void PartScale(string part, double s) { Transform t; if (P.TryGetValue(part, out t)) t.localScale = Vector3.one * (float)s; }

        internal void MarkFreed() { Freed = true; }
    }

    /// <summary>
    /// 원작 `fxActorProto`/`fxActor`/`fxActorFree` — 종당 프로토타입 한 번(`Mobs.build` = <see cref="VoxelMob.Build"/> · vivid 0.14 · 그림자 없음)을
    /// 굽고 시전마다 **복제**한다(메시·재질 공유 · 드로우콜만 는다). 퇴장은 복제본만 지운다 — 프로토타입의 메시·재질은 건드리지 않는다.
    /// </summary>
    public sealed class SkillActorPool
    {
        public const double Vivid = 0.14;

        sealed class Proto
        {
            public GameObject Root;
            public readonly Dictionary<string, string> PartPath = new Dictionary<string, string>();
            public readonly Dictionary<string, double[]> BaseRot = new Dictionary<string, double[]>();
            public readonly Dictionary<string, double> WingS = new Dictionary<string, double>();
            public int PartCount;
            public readonly List<Mesh> Meshes = new List<Mesh>();
        }

        readonly MobTable table;
        readonly Transform protoRoot, stage;
        readonly Dictionary<string, Proto> protos = new Dictionary<string, Proto>();
        readonly HashSet<SkillActor> live = new HashSet<SkillActor>();

        public int Active { get { return live.Count; } }
        public int Spawned { get; private set; }
        public readonly HashSet<string> IdsSpawned = new HashSet<string>();
        public int ProtoCount { get { int n = 0; foreach (var p in protos.Values) if (p != null) n++; return n; } }

        /// <param name="table">`mobs-skillfx.json`(T2 `SKILLFX_MODELS`).</param>
        /// <param name="stage">액터가 서는 부모(전투 무대).</param>
        public SkillActorPool(MobTable table, Transform stage)
        {
            this.table = table; this.stage = stage;
            var holder = new GameObject("SkillFx Prototypes");
            holder.transform.SetParent(stage, false);
            holder.SetActive(false);
            protoRoot = holder.transform;
        }

        static string PathOf(Transform root, Transform node)
        {
            var parts = new List<string>();
            for (Transform t = node; t != null && t != root; t = t.parent) parts.Add(t.name);
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        Proto Get(string id)
        {
            Proto p;
            if (protos.TryGetValue(id, out p)) return p;
            MobModel model = table != null ? table.Models.Get(id, null) : null;
            if (model == null) { protos[id] = null; return null; }   // 표에 없어도 캐시(매 시전 재시도 금지 — 원작 규약)
            VoxelMobRig rig = VoxelMob.Build(model, model.Cell, Vivid, protoRoot, "fx:" + id);
            p = new Proto { Root = rig.Root, PartCount = rig.PartCount };
            foreach (var mr in rig.Renderers) { mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false; }
            foreach (var mf in rig.Meshes) p.Meshes.Add(mf.sharedMesh);
            foreach (var kv in rig.Parts)
            {
                p.PartPath[kv.Key] = PathOf(rig.Root.transform, kv.Value);
                MobPartPlan plan;
                if (rig.PlanOf.TryGetValue(kv.Value, out plan) && plan.Rot != null && plan.Rot.Length >= 3) p.BaseRot[kv.Key] = new[] { plan.Rot[0], plan.Rot[1], plan.Rot[2] };
            }
            foreach (var w in rig.Wings) if (w.Pid != null) p.WingS[w.Pid] = w.Value;
            protos[id] = p;
            return p;
        }

        /// <summary>프로토타입이 있는가(표에 있는 종인가).</summary>
        public bool Has(string id) { return Get(id) != null; }
        public int PartCountOf(string id) { Proto p = Get(id); return p == null ? 0 : p.PartCount; }
        public IReadOnlyList<Mesh> MeshesOf(string id) { Proto p = Get(id); return p == null ? null : p.Meshes; }

        /// <summary>`fxActor(id, {scale, pos, yaw})` — 없으면 null.</summary>
        public SkillActor Spawn(string id, double scale, Vector3 threePos, double yaw)
        {
            Proto p = Get(id);
            if (p == null) return null;
            GameObject g = UnityEngine.Object.Instantiate(p.Root, stage, false);
            g.name = "fx " + id;
            g.SetActive(true);
            var a = new SkillActor(id, g, p.PartPath, p.BaseRot, p.WingS);
            a.SetScale(scale);
            a.SetPos(threePos);
            a.SetYaw(yaw);
            live.Add(a); Spawned++; IdsSpawned.Add(id);
            return a;
        }

        /// <summary>`fxActorFree(a)` — 씬에서 뗀다(복제본만 파괴 · 메시·재질은 프로토타입 공유물).</summary>
        public void Free(SkillActor a)
        {
            if (a == null || a.Freed) return;
            a.MarkFreed();
            live.Remove(a);
            if (a.G != null) UnityEngine.Object.Destroy(a.G);   // 이미 파괴된 것(유니티 null)은 건너뛴다
        }

        public void FreeAll()
        {
            foreach (var a in new List<SkillActor>(live)) Free(a);
        }
    }
}
