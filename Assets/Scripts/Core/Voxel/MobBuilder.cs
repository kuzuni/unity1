using System;
using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Voxel
{
    /// <summary>파츠 하나의 조립 계획 — 정본 `Mobs.build` 가 THREE 노드에 하던 것을 값으로 든다. 위치는 three 좌표(칸 × cell) · Game 이 z 를 뒤집는다(결정 4).</summary>
    public sealed class MobPartPlan
    {
        /// <summary>표에서의 순번.</summary>
        public int Index;
        public MobPart Source;
        /// <summary>파츠 이름표 = `id` → `tag` → `part<i>`(정본 `pid`).</summary>
        public string Pid;
        /// <summary>표의 `parent` — pivots 에 있는 id 여야 붙는다(없으면 뿌리 · 정본과 같다).</summary>
        public MobPartPlan ParentPlan;
        public bool HasPivot;
        /// <summary>pivot 노드의 부모 기준 로컬 위치(three · 세계 단위) — HasPivot 일 때만.</summary>
        public double[] PivotPos;
        /// <summary>메시의 로컬 위치(pivot 이 있으면 pivot 기준 · 없으면 부모 기준).</summary>
        public double[] MeshPos;
        /// <summary>노드 회전(three XYZ 오일러 · 라디안) — pivot 노드 또는 메시.</summary>
        public double[] Rot;
        /// <summary>vivid 보정 뒤 기본 색.</summary>
        public int Color;
        public string MatKey;
        public MobMat Mat;
        public bool Basic;
        public VoxelMesh Mesh;
        /// <summary>정본 `userData.part === 'head'` — 탑승 가림 판정이 머리만 골라 본다.</summary>
        public bool IsHead;
        public double Gait = 1, S = 1;
        /// <summary>이 파츠의 origin(칸) — 자식의 base(pivot 이면 pivot · 아니면 at).</summary>
        public double[] Origin;
    }

    /// <summary>제너릭 관절 드라이버 서술(정본 `out.joints` 항목) — base 는 rot 적용 뒤 그 축의 각(three 좌표 · 라디안).</summary>
    public sealed class MobJointPlan
    {
        public MobPartPlan Part;
        public string Axis;
        public double Base, Amp, Ph, F, Gain;
        public bool Abs, Spin;
    }

    /// <summary>종 하나의 조립 계획 — 정본 `Mobs.build` 의 반환 객체(parts · legs · wings · wheels · spinners · glow · claws · head · tail · joints).</summary>
    public sealed class MobPlan
    {
        public MobModel Model;
        public double Cell;
        public double Vivid;
        public readonly List<MobPartPlan> Parts = new List<MobPartPlan>();
        /// <summary>id → 계획(정본 `out.parts[id]` · 나중 것이 덮는다).</summary>
        public readonly Dictionary<string, MobPartPlan> ById = new Dictionary<string, MobPartPlan>();
        public MobPartPlan Head, Tail;
        public readonly List<MobPartPlan> Legs = new List<MobPartPlan>();
        public readonly List<MobPartPlan> Wings = new List<MobPartPlan>();
        public readonly List<MobPartPlan> Wheels = new List<MobPartPlan>();
        public readonly List<MobPartPlan> Spinners = new List<MobPartPlan>();
        public readonly List<MobPartPlan> Glow = new List<MobPartPlan>();
        public readonly List<MobPartPlan> Claws = new List<MobPartPlan>();
        public readonly List<MobJointPlan> Joints = new List<MobJointPlan>();
        /// <summary>재질 키(첫 등장 순) — 같은 키끼리 Material 하나를 공유한다.</summary>
        public readonly List<string> MatKeys = new List<string>();
        public int VertexCount;
    }

    /// <summary>
    /// 정본 `mobs.js` `Mobs.build` 의 순수 부분(T4). `Plan(model, cell, vivid)` → <see cref="MobPlan"/>. THREE 노드 대신 값으로 두고 Game `VoxelMob` 이 그대로 세운다.
    /// 규약(정본 그대로): cell = 인자 → 표 → 0.03 · `off` 파츠 건너뜀 · vivid 는 HSL 보정 · `Voxel.box` + paint → `Voxel.build`(size=cell · center · jitter 0.022(basic 0) · ao 0.85) ·
    /// parent 는 pivots[parent] 아래(없으면 뿌리) · pivot 이 있으면 (pivot − base)·cell 에 빈 노드, 메시는 (at − pivot)·cell · 없으면 (at − base)·cell.
    /// </summary>
    public static class MobBuilder
    {
        /// <summary>정본 기본 칸 크기(`opts.cell || model.cell || 0.03`).</summary>
        public const double DefaultCell = 0.03;
        public const double MobJitter = 0.022;
        public const double MobAo = 0.85;

        /// <summary>정본 `matKey` — 성질(basic · opacity · emissive · emissiveIntensity · rough)이 같은 파츠끼리 재질 하나.</summary>
        public static string MatKey(MobMat m)
        {
            if (m == null) return "std";
            bool basic = IsBasic(m);
            return (basic ? "b" : "s") + "|" + (m.Opacity.HasValue ? JsNum.ToString(m.Opacity.Value) : "1")
                + "|" + (m.Emissive.HasValue ? JsNum.ToString(m.Emissive.Value) : "")
                + "|" + (m.EmissiveIntensity.HasValue && m.EmissiveIntensity.Value != 0 ? JsNum.ToString(m.EmissiveIntensity.Value) : "0")
                + "|" + (m.Rough.HasValue ? JsNum.ToString(m.Rough.Value) : "");
        }

        /// <summary>`mat.basic`(무조명) — T3 <see cref="MobMat"/> 에는 칸이 없어 원문에서 읽는다.</summary>
        public static bool IsBasic(MobMat m)
        {
            return m != null && m.Raw != null && J.Bool(m.Raw["basic"]);
        }

        /// <summary>`off` 파츠(정본 `if (!p || p.off) continue`).</summary>
        public static bool IsOff(MobPart p)
        {
            return p == null || (p.Raw != null && J.Bool(p.Raw["off"]));
        }

        /// <param name="leftHanded">true 면 메시를 유니티 좌표(z 반전 · 감김 반전)로 굽는다. 노드 위치·회전은 언제나 three 값이다(Game 이 뒤집는다).</param>
        public static MobPlan Plan(MobModel model, double cell = 0, double vivid = 0, bool leftHanded = false)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (cell <= 0) cell = model.Cell > 0 ? model.Cell : DefaultCell;
            var plan = new MobPlan { Model = model, Cell = cell, Vivid = vivid };
            var pivots = new Dictionary<string, MobPartPlan>();
            var parts = model.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                if (IsOff(p)) continue;
                if (p.Box == null || p.Box.Length < 3 || p.At == null || p.At.Length < 3)
                    throw new InvalidOperationException(model.Name + " 파츠 " + i + ": box/at 가 없다");
                int col = ColorHsl.Vivid(p.C, vivid);
                var cells = MobPainter.CellsOf(p, col);
                bool basic = IsBasic(p.Mat);
                var mesh = VoxelGeometry.Build(cells, new VoxelBuildOptions
                {
                    Size = cell, Color = col, Center = true, Jitter = basic ? 0 : MobJitter, Ao = MobAo, LeftHanded = leftHanded,
                });

                MobPartPlan parentPlan = null;
                if (p.Parent != null) pivots.TryGetValue(p.Parent, out parentPlan);
                double[] b = parentPlan != null ? parentPlan.Origin : new double[] { 0, 0, 0 };
                var pp = new MobPartPlan
                {
                    Index = i, Source = p, Pid = p.Id ?? p.Tag ?? ("part" + i), ParentPlan = parentPlan,
                    Color = col, Mat = p.Mat, Basic = basic, MatKey = MatKey(p.Mat), Mesh = mesh,
                    Rot = new[] { Get(p.Rot, 0), Get(p.Rot, 1), Get(p.Rot, 2) },
                };
                if (p.Pivot != null && p.Pivot.Length >= 3)
                {
                    pp.HasPivot = true;
                    pp.PivotPos = new[] { (p.Pivot[0] - b[0]) * cell, (p.Pivot[1] - b[1]) * cell, (p.Pivot[2] - b[2]) * cell };
                    pp.MeshPos = new[] { (p.At[0] - p.Pivot[0]) * cell, (p.At[1] - p.Pivot[1]) * cell, (p.At[2] - p.Pivot[2]) * cell };
                    pp.Origin = new[] { p.Pivot[0], p.Pivot[1], p.Pivot[2] };
                    if (p.Id != null) pivots[p.Id] = pp;
                }
                else
                {
                    pp.MeshPos = new[] { (p.At[0] - b[0]) * cell, (p.At[1] - b[1]) * cell, (p.At[2] - b[2]) * cell };
                    pp.Origin = new[] { p.At[0], p.At[1], p.At[2] };
                    if (p.Id != null) pivots[p.Id] = pp;
                }
                if (p.Id != null) plan.ById[p.Id] = pp;
                pp.IsHead = p.Head || p.Tag == "head" || p.Parent == "head";
                switch (p.Tag)
                {
                    case "head": plan.Head = pp; break;
                    case "tail": if (plan.Tail == null) plan.Tail = pp; break;
                    case "leg": pp.Gait = p.Gait ?? 1; plan.Legs.Add(pp); break;
                    case "wing": pp.S = p.S ?? 1; plan.Wings.Add(pp); break;
                    case "wheel": plan.Wheels.Add(pp); break;
                    case "spinner": plan.Spinners.Add(pp); break;
                    case "glow": plan.Glow.Add(pp); break;
                    case "claw": pp.S = p.S ?? 1; plan.Claws.Add(pp); break;
                }
                if (p.Joint != null)
                {
                    string axis = p.Joint.Axis ?? "x";
                    plan.Joints.Add(new MobJointPlan
                    {
                        Part = pp, Axis = axis, Base = AxisAngle(pp.Rot, axis),
                        Amp = p.Joint.Amp ?? 0.3, Ph = p.Joint.Ph ?? 0, F = p.Joint.F ?? 1, Gain = p.Joint.Gain ?? 1,
                        Abs = p.Joint.Abs, Spin = p.Joint.Spin,
                    });
                }
                if (!plan.MatKeys.Contains(pp.MatKey)) plan.MatKeys.Add(pp.MatKey);
                plan.VertexCount += mesh.VertexCount;
                plan.Parts.Add(pp);
            }
            return plan;
        }

        static double Get(double[] a, int i) { return a != null && a.Length > i ? a[i] : 0; }

        /// <summary>`node.rotation[axis] || 0`.</summary>
        public static double AxisAngle(double[] rot, string axis)
        {
            switch (axis)
            {
                case "y": return Get(rot, 1);
                case "z": return Get(rot, 2);
                default: return Get(rot, 0);
            }
        }
    }
}
