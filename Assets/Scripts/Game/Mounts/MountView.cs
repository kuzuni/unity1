using System.Collections.Generic;
using UnityEngine;
using Forge.Core.Data;
using Forge.Core.Mounts;
using Forge.Core.Voxel;
using Forge.Game.Voxel;

namespace Forge.Game.Mounts
{
    /// <summary>장착 한 칸 — 원작 `S.mounts[S.activeMounts[i]]` 의 `{name, rarity, stars}`.</summary>
    public sealed class MountSlot
    {
        public string Name, Rarity;
        public int Stars;
        public MountSlot(string name, string rarity, int stars = 0) { Name = name; Rarity = rarity; Stars = stars; }
    }

    /// <summary>
    /// 탈것 한 마리의 시각(T11) — 정본 `makeMountMesh`(9464 · `Mobs.build(model, {cell: form.saddle / model.seat, vivid: 0.2})` + 파츠 갈래 legs/head/wings/claws/tail/wheels/spinners/glow/flat) ·
    /// `refreshMount`/`refreshMountFollowers` 의 래퍼 그룹(위치·회전 = 그룹 · 배율 = 안의 메시) · `animateMountParts`(파츠 드라이버 · 탄 것과 무리가 같은 함수).
    /// 조형은 T4 <see cref="VoxelMob"/> 가 표(mobs-mounts.json) 그대로 세운다 — 칸 크기만 계열 안장 높이에서 온다(결정 19). 수치·식은 Core <see cref="MountRideRules"/>·<see cref="MountPartDriver"/>.
    /// 승천 데코는 정본 10820·10982 대로 `makeMountMesh` 뒤 · **배율 전**에 <see cref="AscendDecor.Apply"/>(T399).
    /// 마구(등자·고삐·핸들바·크랭크)는 세우지 않는다 — 서서 타는 지금 정본도 정렬 대상에서 뺀다(«안장에 달린 장식»)이고, 유니티 조립기(T4)에 그 갈래가 없다(T11 완료 기록 참조).
    /// </summary>
    public sealed class MountView
    {
        sealed class PartRot
        {
            public Transform Node;
            public double[] Rot;
            public double Value;
        }

        sealed class GlowPart
        {
            public Renderer R;
            public MaterialPropertyBlock Block;
            public bool Emissive;
            public Color Base;
        }

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public readonly string Name, Rarity;
        public readonly int Stars, Index;
        public readonly MountForm Form;
        public readonly MobModel Model;
        public readonly VoxelMobRig Rig;
        /// <summary>정본 `g`(래퍼 그룹) — 위치·회전은 여기.</summary>
        public readonly Transform G;
        /// <summary>정본 `mesh`(Mobs.build 의 group) — 배율이 걸린다.</summary>
        public readonly Transform Mesh;
        /// <summary>승천 데코 뿌리(별 0 · 6승천 순환이면 null) — 정본 `deco.userData.ascendDecorRoot`.</summary>
        public readonly AscendDecorRoot Decor;
        /// <summary>정본 `sc = 1.1 + RARITIES.indexOf(rarity) * 0.1`.</summary>
        public readonly double Sc;
        public readonly bool Flat;
        public readonly MountBodyKind Kind;
        /// <summary>정본 `userData.baseY / spotX / phase / speed`.</summary>
        public double BaseY, SpotX, SpotZ, Phase, Speed = 1;
        readonly double[] gRot = { 0, 0, 0 };
        readonly List<PartRot> legs = new List<PartRot>(), wings = new List<PartRot>(), claws = new List<PartRot>(), wheels = new List<PartRot>(), spinners = new List<PartRot>();
        readonly PartRot head, tail;
        readonly List<GlowPart> glow = new List<GlowPart>();

        /// <summary>three 오일러(라디안) — rx 는 피치(그룹 rotation.x · 탄 상태에서는 영웅에게 ×0.6 전달), ry = CREATURE_YAW, rz = 평판 뱅킹.</summary>
        public double Rx { get { return gRot[0]; } }
        public double Ry { get { return gRot[1]; } }
        public double Rz { get { return gRot[2]; } }
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }

        public MountView(Transform parent, string goName, int index, MountSlot slot, MobModel model, MountForm form, double sc, double creatureYaw)
        {
            Index = index; Name = slot.Name; Rarity = slot.Rarity; Stars = slot.Stars;
            Form = form; Model = model; Sc = sc;
            var go = new GameObject(goName);
            go.transform.SetParent(parent, false);
            G = go.transform;
            Rig = VoxelMob.Build(model, MountRideRules.Cell(form, model), MountRideRules.Vivid, G, "Mesh " + slot.Name);
            Mesh = Rig.Root.transform;
            Decor = AscendDecor.Apply(Rig, slot.Stars);   // 정본 10820·10982 `applyAscendDecor(mesh, m.stars, 'mount')` — 스케일 전(T399)
            Mesh.localScale = Vector3.one * (float)sc;
            Flat = model.Flat;
            Kind = MountPartDriver.KindOf(Flat, Rig.Wings.Count > 0, Rig.Wheels.Count > 0);
            foreach (var l in Rig.Legs) legs.Add(Part(l.Node, l.Value));
            foreach (var w in Rig.Wings) wings.Add(Part(w.Node, w.Value));
            foreach (var c in Rig.Claws) claws.Add(Part(c.Node, c.Value));
            foreach (var w in Rig.Wheels) wheels.Add(Part(w, 0));
            foreach (var s in Rig.Spinners) spinners.Add(Part(s, 0));
            if (Rig.Head != null) head = Part(Rig.Head, 0);
            if (Rig.Tail != null) tail = Part(Rig.Tail, 0);
            foreach (var gt in Rig.Glow)
            {
                var r = gt.GetComponentInChildren<Renderer>();
                if (r == null) continue;
                var m = r.sharedMaterial;
                var gp = new GlowPart { R = r, Block = new MaterialPropertyBlock() };
                gp.Emissive = m != null && m.HasProperty(EmissionColorId) && m.IsKeywordEnabled("_EMISSION");
                gp.Base = m == null ? Color.white : (gp.Emissive ? m.GetColor(EmissionColorId) : (m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : Color.white));
                glow.Add(gp);
            }
            gRot[1] = creatureYaw;
            ThreeSpace.Apply(G, gRot);
        }

        PartRot Part(Transform node, double value)
        {
            MobPartPlan pp;
            double[] rot = Rig.PlanOf.TryGetValue(node, out pp) && pp.Rot != null ? new[] { pp.Rot[0], pp.Rot[1], pp.Rot[2] } : new double[] { 0, 0, 0 };
            return new PartRot { Node = node, Rot = rot, Value = value };
        }

        /// <summary>정본 `mesh.scale.set(sc * wide, sc * rideScale, sc * rideScale)` — x 만 좁히고 z 는 세로와 같다.</summary>
        public void SetScale(double x, double y, double z)
        {
            Mesh.localScale = new Vector3((float)x, (float)y, (float)z);
        }

        /// <summary>three 좌표로 그룹 위치.</summary>
        public void SetPosition(double x, double y, double z)
        {
            X = x; Y = y; Z = z;
            G.localPosition = ThreeSpace.Pos(x, y, z);
        }

        /// <summary>그룹 rotation.x(three) — 호출부(<see cref="MountRider"/>)가 소유한다(정본 주석: 탄 상태에서는 영웅에게도 전달돼야 해서 한 곳에서만).</summary>
        public void SetPitch(double rx)
        {
            gRot[0] = rx;
            ThreeSpace.Apply(G, gRot);
        }

        /// <summary>정본 `Box3().setFromObject(g).min.y` — 렌더러 경계의 최저 y(월드 · 부모가 원점이면 씬 y). 접지 보정(lift)용.</summary>
        public double MinY()
        {
            double min = double.PositiveInfinity;
            foreach (var r in Rig.Renderers) if (r != null) min = System.Math.Min(min, r.bounds.min.y);
            return double.IsPositiveInfinity(min) ? 0 : min;
        }

        /// <summary>정본 `animateMountParts(mg, t, moving, dt)` — 파츠를 돌리고 이 프레임의 몸통 피치(그룹 rotation.x 에 더할 양)를 돌려준다. 평판형 뱅킹(rotation.z)은 여기서 그룹에 바로 건다.</summary>
        public double Animate(MountRideDefs defs, double t, bool moving, double dt)
        {
            double w = MountPartDriver.GaitW(defs, moving);
            foreach (var lg in legs) SetAxis(lg, 0, MountPartDriver.LegRx(t, w, moving, lg.Value));
            if (head != null)
            {
                head.Rot[0] = MountPartDriver.HeadRx(t, w, moving);
                head.Rot[1] = MountPartDriver.HeadRy(t);
                ThreeSpace.Apply(head.Node, head.Rot);
            }
            foreach (var wg in wings) SetAxis(wg, 2, MountPartDriver.WingRz(t, wg.Value));
            foreach (var cw in claws) SetAxis(cw, 0, MountPartDriver.ClawRx(t, moving, cw.Value));
            if (tail != null)
            {
                tail.Rot[2] = MountPartDriver.TailRz(t);
                tail.Rot[1] = MountPartDriver.TailRy(t);
                ThreeSpace.Apply(tail.Node, tail.Rot);
            }
            if (spinners.Count > 0) { double k = MountPartDriver.SpinnerStep(moving, dt); foreach (var sg in spinners) SetAxis(sg, 2, sg.Rot[2] - k); }
            if (wheels.Count > 0) { double k = MountPartDriver.WheelStep(moving, dt); foreach (var wg in wheels) SetAxis(wg, 0, wg.Rot[0] - k); }
            if (glow.Count > 0)
            {
                float k = (float)MountPartDriver.GlowK(t, moving);
                foreach (var gp in glow)
                {
                    if (gp.Emissive) gp.Block.SetColor(EmissionColorId, gp.Base * k);
                    else gp.Block.SetColor(BaseColorId, gp.Base * (float)MountPartDriver.GlowColorK(k));
                    gp.R.SetPropertyBlock(gp.Block);
                }
            }
            if (Flat) { gRot[2] = MountPartDriver.FlatRoll(t, moving); ThreeSpace.Apply(G, gRot); }
            return MountPartDriver.Pitch(Kind, t, w, moving);
        }

        static void SetAxis(PartRot p, int axis, double v)
        {
            p.Rot[axis] = v;
            ThreeSpace.Apply(p.Node, p.Rot);
        }

        public void Destroy()
        {
            if (G != null) Object.Destroy(G.gameObject);
        }
    }
}
