using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Forge.Core.Data;
using Forge.Core.Voxel;
using Forge.Game.Render;

namespace Forge.Game.Voxel
{
    /// <summary>tag 로 모인 파츠(leg 의 gait · wing/claw 의 s) — 정본 `node.userData.gait / .s`.</summary>
    public sealed class VoxelLimb
    {
        public Transform Node;
        public string Pid;
        /// <summary>leg → gait · wing/claw → s.</summary>
        public double Value;
    }

    /// <summary>제너릭 관절 드라이버 서술(정본 `out.joints`) — `Rot` 은 three 오일러 사본이라 `Set(angle)` 이 그 축만 바꿔 <see cref="ThreeSpace"/> 로 적용한다.</summary>
    public sealed class VoxelJoint
    {
        public Transform Node;
        public string Axis;
        public double Base, Amp, Ph, F, Gain;
        public bool Abs, Spin;
        public double[] Rot;

        /// <summary>`node.rotation[axis] = angle`(three 라디안).</summary>
        public void Set(double angle)
        {
            switch (Axis)
            {
                case "y": Rot[1] = angle; break;
                case "z": Rot[2] = angle; break;
                default: Rot[0] = angle; break;
            }
            ThreeSpace.Apply(Node, Rot);
        }
    }

    /// <summary>조립 결과 — 정본 `Mobs.build` 의 반환 객체(group · parts · legs · wings · wheels · spinners · glow · claws · head · tail · joints · boxes).</summary>
    public sealed class VoxelMobRig
    {
        public GameObject Root;
        public MobPlan Plan;
        public double Cell { get { return Plan.Cell; } }
        /// <summary>id → 노드(pivot 이 있으면 pivot 노드 · 아니면 메시).</summary>
        public readonly Dictionary<string, Transform> Parts = new Dictionary<string, Transform>();
        /// <summary>노드 → 파츠 계획(box·at·rot 원문 = 정본 `boxes`).</summary>
        public readonly Dictionary<Transform, MobPartPlan> PlanOf = new Dictionary<Transform, MobPartPlan>();
        public readonly List<MeshRenderer> Renderers = new List<MeshRenderer>();
        public readonly List<MeshFilter> Meshes = new List<MeshFilter>();
        public readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        public Transform Head, Tail;
        public readonly List<VoxelLimb> Legs = new List<VoxelLimb>();
        public readonly List<VoxelLimb> Wings = new List<VoxelLimb>();
        public readonly List<VoxelLimb> Claws = new List<VoxelLimb>();
        public readonly List<Transform> Wheels = new List<Transform>();
        public readonly List<Transform> Spinners = new List<Transform>();
        public readonly List<Transform> Glow = new List<Transform>();
        public readonly List<VoxelJoint> Joints = new List<VoxelJoint>();

        public int PartCount { get { return Meshes.Count; } }

        public void Destroy()
        {
            if (Root != null) Object.Destroy(Root);
        }
    }

    /// <summary>
    /// 마인크래프트 박스 모델 조립기(T4) — 정본 `Voxel.build` + `Mobs.build` 규약을 유니티 GameObject 트리로. 종 표는 JSON(T2)이 쥔다 —
    /// 여기서 좌표를 고치지 않는다(§1 조형 계약). 계산은 전부 Core <see cref="MobBuilder"/>(순수 · dotnet 대조)이고 여기서는 Mesh/Material/Transform 만 만든다.
    /// 좌표계: three → 유니티는 z 반전(결정 4 · <see cref="ThreeSpace"/>) · 메시는 Core 가 왼손 좌표로 굽는다(감김 반전 포함).
    /// </summary>
    public static class VoxelMob
    {
        /// <summary>종 하나를 세운다. cell 0 = 표 값(탈것 표에는 없다 → 씬 `MOUNT_FORMS` 가 준다) · vivid = 채도 보정(정본 씬 0.14~0.2).</summary>
        public static VoxelMobRig Build(MobModel model, double cell = 0, double vivid = 0, Transform parent = null, string name = null)
        {
            var plan = MobBuilder.Plan(model, cell, vivid, true);
            var rig = new VoxelMobRig { Plan = plan };
            rig.Root = new GameObject(name ?? ("Mob " + model.Name));
            if (parent != null) rig.Root.transform.SetParent(parent, false);
            var nodeOf = new Dictionary<MobPartPlan, Transform>();
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;

            for (int i = 0; i < plan.Parts.Count; i++)
            {
                var pp = plan.Parts[i];
                Transform parentNode = pp.ParentPlan != null && nodeOf.ContainsKey(pp.ParentPlan) ? nodeOf[pp.ParentPlan] : rig.Root.transform;

                var meshGo = new GameObject(pp.Pid);
                var mf = meshGo.AddComponent<MeshFilter>();
                mf.sharedMesh = ToMesh(pp.Mesh, model.Name + "/" + pp.Pid, linear);
                var mr = meshGo.AddComponent<MeshRenderer>();
                Material mat;
                if (!rig.Materials.TryGetValue(pp.MatKey, out mat)) { mat = VoxelMaterials.Get(pp.MatKey, pp.Mat); rig.Materials[pp.MatKey] = mat; }
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.On;
                mr.receiveShadows = true;
                rig.Renderers.Add(mr);
                rig.Meshes.Add(mf);
                EdgePartId.Tag(mr);   // T330 — 파츠 ID 번호를 생성 시각에 굳힌다(윤곽선 넷째 항 · 정본 idMatFor)

                Transform node;
                if (pp.HasPivot)
                {
                    var pv = new GameObject(pp.Pid + " pivot");
                    pv.transform.SetParent(parentNode, false);
                    pv.transform.localPosition = ThreeSpace.Pos(pp.PivotPos);
                    meshGo.transform.SetParent(pv.transform, false);
                    meshGo.transform.localPosition = ThreeSpace.Pos(pp.MeshPos);
                    node = pv.transform;
                }
                else
                {
                    meshGo.transform.SetParent(parentNode, false);
                    meshGo.transform.localPosition = ThreeSpace.Pos(pp.MeshPos);
                    node = meshGo.transform;
                }
                node.localRotation = ThreeSpace.Rot(pp.Rot);
                nodeOf[pp] = node;
                rig.PlanOf[node] = pp;
                if (pp.Source.Id != null) rig.Parts[pp.Source.Id] = node;
                var tagComp = meshGo.AddComponent<VoxelPartTag>();
                tagComp.Pid = pp.Pid; tagComp.Parent = pp.Source.Parent; tagComp.IsHead = pp.IsHead;
            }

            if (plan.Head != null) rig.Head = nodeOf[plan.Head];
            if (plan.Tail != null) rig.Tail = nodeOf[plan.Tail];
            foreach (var l in plan.Legs) rig.Legs.Add(new VoxelLimb { Node = nodeOf[l], Pid = l.Pid, Value = l.Gait });
            foreach (var w in plan.Wings) rig.Wings.Add(new VoxelLimb { Node = nodeOf[w], Pid = w.Pid, Value = w.S });
            foreach (var c in plan.Claws) rig.Claws.Add(new VoxelLimb { Node = nodeOf[c], Pid = c.Pid, Value = c.S });
            foreach (var w in plan.Wheels) rig.Wheels.Add(nodeOf[w]);
            foreach (var s in plan.Spinners) rig.Spinners.Add(nodeOf[s]);
            foreach (var g in plan.Glow) rig.Glow.Add(nodeOf[g]);
            foreach (var j in plan.Joints)
            {
                rig.Joints.Add(new VoxelJoint
                {
                    Node = nodeOf[j.Part], Axis = j.Axis, Base = j.Base, Amp = j.Amp, Ph = j.Ph, F = j.F, Gain = j.Gain, Abs = j.Abs, Spin = j.Spin,
                    Rot = new[] { j.Part.Rot[0], j.Part.Rot[1], j.Part.Rot[2] },
                });
            }
            return rig;
        }

        /// <summary>Core 배열 → Mesh(면마다 법선이 따로라 플랫 셰이딩). 정점 색은 hex 를 sRGB 로 보므로(결정 4) 선형 색공간이면 선형으로 바꿔 준다.</summary>
        public static Mesh ToMesh(VoxelMesh vm, string name, bool linearColorSpace)
        {
            int n = vm.VertexCount;
            var verts = new Vector3[n];
            var norms = new Vector3[n];
            var cols = new Color[n];
            for (int i = 0; i < n; i++)
            {
                int p = i * 3;
                verts[i] = new Vector3(vm.Positions[p], vm.Positions[p + 1], vm.Positions[p + 2]);
                norms[i] = new Vector3(vm.Normals[p], vm.Normals[p + 1], vm.Normals[p + 2]);
                var c = new Color(vm.Colors[p], vm.Colors[p + 1], vm.Colors[p + 2], 1f);
                cols[i] = linearColorSpace ? c.linear : c;
            }
            var mesh = new Mesh();
            mesh.name = name;
            if (n > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetColors(cols);
            mesh.SetTriangles(vm.Triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    /// <summary>파츠 이름표(정본 `userData.pid / pparent / part`) — 레이캐스트가 맞은 게 어느 파츠인지 말할 수 있어야 한다.</summary>
    public sealed class VoxelPartTag : MonoBehaviour
    {
        public string Pid;
        public string Parent;
        public bool IsHead;
    }
}
