using System;
using System.Collections.Generic;

namespace Forge.Core.Hero
{
    /// <summary>정본 `WEAPON_GRIP` 한 종 — rot(무기 로컬 오일러 · rot[1] 은 근접에서 «날 롤») · pos(파지점 보정) · hand(L = 왼손 파지 · 활계) · scale(활계 축소) · pose(다관절 거치 rx 가산) · shaftR.</summary>
    public sealed class GripDef
    {
        public double[] Rot;
        public double[] Pos;
        public string Hand;
        public double? Scale;
        public double? ShaftR;
        public PoseAdd Pose;
    }

    /// <summary>`applyWeaponGrip` 이 리그와 무기 그룹에 실제로 먹인 값(three 로컬 · 어깨 본 기준) — Game 이 Transform 에 옮긴다.</summary>
    public sealed class GripResult
    {
        public string WtypeId, Shape;
        public GripDef Grip;
        public bool Melee;
        /// <summary>무기가 달리는 본(마크식 통짜 박스 리그 = 어깨 · 활계는 왼어깨).</summary>
        public string ParentBone;
        public double[] Pos, Rot, Quat;
        public double Scale;
        public double RestX, ElbowFix, BladeRoll;
        public PoseAdd RestPose, RidePose;
        public bool ShieldVisible;
        /// <summary>공격이 끝날 때 되돌릴 값(정본 `_gripRot`/`_gripPos` = 실제로 먹인 합성 결과).</summary>
        public double[] GripRot, GripPos;
    }

    /// <summary>
    /// 정본 `scene3d.js` 의 무기 파지 — `WEAPON_GRIP` 표 · `gripOf` · `applyWeaponGrip`(마인크래프트 handheld 파지: 근접은 각을 `MC_CARRY_X` 로 덮어쓰고 날 롤은
    /// 자루 축 쿼터니언 우측곱 · 원거리는 표의 rot 그대로 · 부착 = 어깨 본 · 파지점 = 어깨 로컬 −(limbH − MC_GRIP_PULL)) · `refreshHeroEquip` 의 restX.
    /// 순수 계산(three 좌표) — 유니티 변환은 Game. 값은 `t6-hero.json` 의 `grip`(WEAPON_TYPES 전 종 + 기본) 과 대조한다.
    /// </summary>
    public static class WeaponGrip
    {
        const double H = Math.PI / 2;
        /// <summary>마크 handheld 파지각 = −1.05(10안 중 3번) + π(앞쪽을 향하게).</summary>
        public const double MC_CARRY_X = -1.05 + Math.PI;
        /// <summary>자루를 주먹 쪽으로 끌어내리는 양(월드 단위 · 파지점 = 어깨 로컬 −(limbH − 이 값)).</summary>
        public const double MC_GRIP_PULL = 0.10;
        /// <summary>기본 존재감 배율(weapon-size-2x · 1.22 의 ×2).</summary>
        public const double WeaponScale = 2.44;
        /// <summary>무기가 없을 때의 형상(정본 `w ? w.wtype : 'club'`) · WEAPON_TYPES 에 없을 때의 restX.</summary>
        public const string DefaultWtype = "club";
        public const double DefaultRestX = -0.25;
        public const double DefaultShaftR = 0.033;

        public static readonly HashSet<string> RangedShapes = new HashSet<string> { "bow", "crossbow", "gun", "pistol", "rifle", "smg", "cannon", "sling", "thrown" };

        static double[] R(double a, double b, double c) { return new[] { a, b, c }; }
        static PoseAdd P(params object[] kv)
        {
            var p = new PoseAdd();
            for (int i = 0; i < kv.Length; i += 2) p.Add((string)kv[i], Convert.ToDouble(kv[i + 1]));
            return p;
        }

        /// <summary>정본 `WEAPON_GRIP`(키 = shape · 순서 그대로).</summary>
        public static readonly Dictionary<string, GripDef> Table = new Dictionary<string, GripDef>
        {
            { "sword", new GripDef { Rot = R(0.22, -H, -0.3), Pose = P("elbowR", -0.3) } },
            { "dagger", new GripDef { Rot = R(0.22, -H, -0.1), Pos = new double[] { 0, -0.04, 0 }, Pose = P("elbowR", -0.5) } },
            { "axe", new GripDef { Rot = R(0.95, -H, -0.15), Pose = P("elbowR", -0.85) } },
            { "hammer", new GripDef { Rot = R(0.95, 0, -0.15), Pose = P("elbowR", -0.85) } },
            { "club", new GripDef { Rot = R(0.14, 0, -0.08), Pose = P("elbowR", -0.3) } },
            { "spear", new GripDef { Rot = R(0.65, 0, 0), Pose = P("elbowR", -0.3) } },
            { "staff", new GripDef { Rot = R(0.58, 0, 0), Pose = P("elbowR", -0.28) } },
            { "bow", new GripDef { Rot = R(0.95, 0, 0), Hand = "L", Scale = 0.6, Pose = P("shoulderL", -1.15, "elbowL", -0.12, "shoulderR", -0.95, "elbowR", -1.05) } },
            { "crossbow", new GripDef { Rot = R(2.9, 0, 0), Hand = "L", Scale = 0.6, Pose = P("shoulderL", -0.85, "elbowL", -0.12, "shoulderR", -0.7, "elbowR", -1) } },
            { "gun", new GripDef { Rot = R(0, 0, 0), Pose = P("shoulderR", -0.35, "elbowR", 0.2) } },
            { "thrown", new GripDef { Rot = R(1.95, 0, 0), Pose = P("shoulderR", -1.45, "elbowR", -0.5) } },
            { "mace", new GripDef { Rot = R(0.95, 0, -0.15), Pose = P("elbowR", -0.85) } },
            { "rapier", new GripDef { Rot = R(0.18, 0, -0.22), Pose = P("elbowR", -0.4) } },
            { "scythe", new GripDef { Rot = R(0.62, -H, 0), Pose = P("elbowR", -0.3) } },
            { "sling", new GripDef { Rot = R(0.2, 0, 0), Pose = P("shoulderR", -0.5, "elbowR", -0.7) } },
            { "pistol", new GripDef { Rot = R(0, 0, 0), Pose = P("shoulderR", -0.45, "elbowR", 0.25) } },
            { "rifle", new GripDef { Rot = R(0, 0, 0), Pose = P("shoulderR", -0.35, "elbowR", 0.2) } },
            { "smg", new GripDef { Rot = R(0, 0, 0), Pose = P("shoulderR", -0.4, "elbowR", 0.22) } },
            { "cannon", new GripDef { Rot = R(0, 0, 0), Pose = P("shoulderR", -0.3, "elbowR", 0.15) } },
        };

        /// <summary>정본 `gripOf(wtypeId)` — id 직접 지정 → 계열(shape) → sword.</summary>
        public static GripDef GripOf(string wtypeId, string shape)
        {
            GripDef g;
            if (wtypeId != null && Table.TryGetValue(wtypeId, out g)) return g;
            if (shape != null && Table.TryGetValue(shape, out g)) return g;
            return Table["sword"];
        }

        /// <summary>정본 `weaponShape(wtypeId)` — 표의 shape · 없으면 id 그대로.</summary>
        public static string ShapeOf(string wtypeId, string tableShape)
        {
            return tableShape ?? wtypeId;
        }

        /// <summary>
        /// 정본 `refreshHeroEquip` 의 restX + `applyWeaponGrip` — 리그의 RestX/RestPose/RidePose 를 갱신하고 무기 그룹의 로컬 위치·회전·배율을 낸다.
        /// <paramref name="tableRestX"/>/<paramref name="tableShape"/> 는 `WEAPON_TYPES[wtypeId]` 의 restX·shape(표에 없으면 null).
        /// <paramref name="ridePose"/> 는 탑승 중 하체 자세(T11 · 지상이면 null) · <paramref name="freeHandReach"/> 는 빈 손이 잡을 바/고삐(T11 · 없으면 null).
        /// </summary>
        public static GripResult Apply(HeroPoseSolver rig, string wtypeId, double? tableRestX, string tableShape, PoseAdd ridePose = null, FreeHandReach freeHandReach = null, bool hasShield = true)
        {
            string shape = ShapeOf(wtypeId, tableShape);
            var grip = GripOf(wtypeId, shape);
            double armRest = tableRestX.HasValue ? tableRestX.Value : DefaultRestX;
            rig.RestX = armRest + 0.25;
            var res = new GripResult { WtypeId = wtypeId, Shape = shape, Grip = grip };
            double elbowFix = 0;
            string side = grip.Hand == "L" ? "L" : "R";
            res.ParentBone = "shoulder" + side;
            rig.RestPose = (ridePose != null || grip.Pose != null) ? PoseAdd.Merge(grip.Pose, ridePose) : null;
            if (freeHandReach != null)
            {
                string free = grip.Hand == "L" ? "R" : "L";
                var rp = PoseAdd.Merge(rig.RestPose, null) ?? new PoseAdd();
                rp.Add("shoulder" + free, PoseDelta.Of(freeHandReach.Shoulder, 0, (free == "L" ? 1 : -1) * freeHandReach.ShoulderZ));
                rp.Add("elbow" + free, PoseDelta.Of(freeHandReach.Elbow, 0, 0));
                rig.RestPose = rp;
                if (free == "R") rig.RestX = 0;
            }
            rig.RidePose = ridePose;
            if (grip.Hand == "L") rig.RestX = 0;
            res.ShieldVisible = hasShield && grip.Hand != "L";
            // 부모 프레임 보정 — 무기를 팬텀 팔꿈치 밑에서 어깨로 옮긴 만큼(base.elbow.rx + restPose.elbow)
            {
                BonePose bE;
                double baseRx = rig.Base.TryGetValue("elbow" + side, out bE) ? bE.Rx : 0;
                var rE = rig.RestPose != null ? rig.RestPose.Get("elbow" + side) : null;
                elbowFix = baseRx + (rE != null ? rE.Rx : 0);
            }
            double[] gp = grip.Pos ?? new double[] { 0, 0, 0 };
            double anchorY = -(HeroRigSpec.LimbH - MC_GRIP_PULL);
            res.Pos = new[] { gp[0], anchorY + gp[1], gp[2] };
            bool melee = !RangedShapes.Contains(shape);
            res.Melee = melee;
            double[] rot = { (melee ? MC_CARRY_X : grip.Rot[0]) + elbowFix, melee ? 0 : grip.Rot[1], grip.Rot[2] };
            double[] q = ThreeQuat.FromEulerXYZ(rot);
            res.BladeRoll = melee ? grip.Rot[1] : 0;
            if (res.BladeRoll != 0)
            {
                q = ThreeQuat.Multiply(q, ThreeQuat.FromAxisAngle(0, 1, 0, res.BladeRoll));
                rot = ThreeQuat.ToEulerXYZ(q);
            }
            res.Quat = q;
            res.Rot = rot;
            res.Scale = WeaponScale * (grip.Scale ?? 1);
            res.ElbowFix = elbowFix;
            res.RestX = rig.RestX;
            res.RestPose = rig.RestPose;
            res.RidePose = rig.RidePose;
            res.GripRot = new[] { rot[0], rot[1], rot[2] };
            res.GripPos = new[] { res.Pos[0], res.Pos[1], res.Pos[2] };
            return res;
        }
    }

    /// <summary>정본 `freeHandReach()` 의 반환(핸들바·고삐를 잡는 빈 손 각 · T11 탈것이 준다).</summary>
    public sealed class FreeHandReach
    {
        public double Shoulder, ShoulderZ, Elbow;
    }
}
