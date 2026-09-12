using System.Collections.Generic;

namespace Forge.Core.Hero
{
    /// <summary>본 하나의 정의 — 부모 본 이름(root 는 부모 null)과 베이스 포즈(정본 `R.base[k]` · 매 프레임 여기서 시작).</summary>
    public sealed class HeroBoneDef
    {
        public string Name, Parent;
        public BonePose Base;
    }

    /// <summary>살색 박스 하나(정본 `add(parent, mat, w, h, d, x, y, z)`) — 부모 본 · 크기 · 로컬 위치 · 색.</summary>
    public sealed class HeroBoxDef
    {
        public string Parent;
        public double W, H, D, X, Y, Z;
        public int Color;
        public double Rough;
    }

    /// <summary>얼굴 디캘(정본 `decal(mat, w, h, x, y, z)` · 머리 앞면에 납작하게 그린 판 · MeshBasic).</summary>
    public sealed class HeroDecalDef
    {
        public string Parent;
        public double W, H, X, Y, Z;
        public int Color;
        public bool ToneMapped;
    }

    /// <summary>
    /// 정본 `ProChar.createKnight()` 가 세우는 **마인크래프트 박스 리그**(2026-08-21 «머리1·몸통1·팔다리 각1 큐브, 맨살»)의 값 부분 —
    /// 1px = 0.05 · 머리 8 · 몸통 8×12×4 · 팔다리 4×12×4 · 관절 피벗 = 본(hipL/R · shoulderL/R · spine · neck · head) · 얼굴은 평면 디캘.
    /// 본 계층·위치는 기사 리그(createKnight)의 것을 그대로 물려받는다(pelvis 0.377 · spine 0.1 · neck 0.5 · head 0.197 · elbow −0.19 · knee −0.192 · cape).
    /// 값은 `tools/hero_vectors.js` 가 정본을 실제로 돌려 뽑은 `t6-hero.json` 의 `rig` 와 EditMode 에서 1:1 대조한다.
    /// </summary>
    public sealed class HeroRigSpec
    {
        /// <summary>마크 비율 1px.</summary>
        public const double PX = 0.05;
        public const double HeadS = 8 * PX, TorW = 8 * PX, TorH = 12 * PX, TorD = 4 * PX;
        public const double LimbW = 4 * PX, LimbH = 12 * PX, LimbD = 4 * PX, ArmW = 4 * PX;
        /// <summary>골반 로컬 발바닥 · 고관절 높이 · 몸통 윗면.</summary>
        public const double Feet = -0.44, HipY = Feet + LimbH, TorTop = HipY + TorH;
        /// <summary>기사 리그의 본 위치(치비 비례 뒤): pelvis = 0.615 + (0.192 − 0.32) + (0.165 − 0.275).</summary>
        public const double PelvisY = 0.615 + (0.192 - 0.32) + (0.165 - 0.275);
        public const double SpineY = 0.1, NeckY = 0.5, HeadY = 0.197, ElbowY = -0.19, KneeY = -0.192;
        public const double CapeY = 0.42, CapeZ = -0.16, CapeRx = 0.14;
        /// <summary>전신 높이 역배율(치비 비례를 바꾸되 화면 높이는 그대로) · 교정 전 발바닥 월드 y.</summary>
        public const double BodyScale = 0.889, GroundY = -0.053;
        /// <summary>
        /// root 의 최종 y = 0.08 + GROUND_Y / BODY_SCALE − footLocal. footLocal 은 정본이 **기사 리그 전체 bbox(부츠)** 에서 재는 값이라 박스만으로는 못 구한다 —
        /// `tools/hero_vectors.js` 가 createKnight 를 실제로 돌려 읽은 값(t6-hero.json `rig.root.base.py`)을 그대로 둔다(EditMode 가 대조).
        /// </summary>
        public const double RootY = 0.11515045267984966;
        /// <summary>살색(정본 `skinM` 0xe0a074 · roughness 0.62 · flatShading) · 먹(눈썹·입) · 흰자 · 동공.</summary>
        public const int SkinColor = 0xe0a074, InkColor = 0x1c1c22, WhiteColor = 0xffffff, PupilColor = 0x111114;
        public const double SkinRough = 0.62;

        public readonly List<HeroBoneDef> Bones = new List<HeroBoneDef>();
        public readonly BonePose RootBase;
        public readonly List<HeroBoxDef> Boxes = new List<HeroBoxDef>();
        public readonly List<HeroDecalDef> Decals = new List<HeroDecalDef>();
        /// <summary>무기 손 소켓이 달려 있던 본(정본 handL/handR 은 elbow 밑 · 박스 리그에서는 안 쓴다 — applyWeaponGrip 이 어깨에 단다).</summary>
        public const string HandLBone = "elbowL", HandRBone = "elbowR";

        static HeroRigSpec _default;
        public static HeroRigSpec Default { get { return _default ?? (_default = new HeroRigSpec()); } }

        public HeroRigSpec()
        {
            RootBase = new BonePose(0, 0, 0, 0, RootY, 0, 1, 1, 1);
            Bone("pelvis", "root", 0, PelvisY, 0);
            Bone("hipL", "pelvis", -(LimbW / 2), HipY, 0);
            Bone("kneeL", "hipL", 0, KneeY, 0);
            Bone("hipR", "pelvis", LimbW / 2, HipY, 0);
            Bone("kneeR", "hipR", 0, KneeY, 0);
            Bone("spine", "pelvis", 0, SpineY, 0);
            Bone("cape", "spine", 0, CapeY, CapeZ, CapeRx);
            Bone("shoulderL", "spine", -(TorW / 2 + ArmW / 2), TorTop - SpineY, 0);
            Bone("elbowL", "shoulderL", 0, ElbowY, 0);
            Bone("shoulderR", "spine", TorW / 2 + ArmW / 2, TorTop - SpineY, 0);
            Bone("elbowR", "shoulderR", 0, ElbowY, 0);
            Bone("neck", "spine", 0, NeckY, 0);
            Bone("head", "neck", 0, HeadY, 0);

            // 다리 · 팔(피벗 = 관절 · 박스는 아래로) · 몸통(척추) · 머리
            Box("hipL", LimbW, LimbH, LimbD, 0, -LimbH / 2, 0);
            Box("hipR", LimbW, LimbH, LimbD, 0, -LimbH / 2, 0);
            Box("shoulderL", ArmW, LimbH, LimbD, 0, -LimbH / 2, 0);
            Box("shoulderR", ArmW, LimbH, LimbD, 0, -LimbH / 2, 0);
            Box("spine", TorW, TorH, TorD, 0, (HipY + TorH / 2) - SpineY, 0);
            double headGY = SpineY + NeckY + HeadY;
            double hy = (TorTop + HeadS / 2) - headGY;
            Box("head", HeadS, HeadS, HeadS, 0, hy, 0);
            // 마인크래프트식 얼굴 — 머리 앞면 평면 디캘(흰자 · 동공 · 눈썹 × 좌우 · 입)
            double fz = HeadS / 2 + 0.002, eyeY = hy + HeadS * 0.06;
            foreach (int sx in new[] { -1, 1 })
            {
                Decal("head", WhiteColor, 0.12, 0.13, sx * 0.115, eyeY, fz, false);
                Decal("head", PupilColor, 0.06, 0.13, sx * 0.115, eyeY, fz + 0.002, false);
                Decal("head", InkColor, 0.13, 0.028, sx * 0.105, eyeY + 0.093, fz + 0.001, true);
            }
            Decal("head", InkColor, 0.14, 0.03, 0, hy - HeadS * 0.22, fz + 0.001, true);
        }

        void Bone(string name, string parent, double px, double py, double pz, double rx = 0)
        {
            Bones.Add(new HeroBoneDef { Name = name, Parent = parent, Base = new BonePose(rx, 0, 0, px, py, pz, 1, 1, 1) });
        }

        void Box(string parent, double w, double h, double d, double x, double y, double z)
        {
            Boxes.Add(new HeroBoxDef { Parent = parent, W = w, H = h, D = d, X = x, Y = y, Z = z, Color = SkinColor, Rough = SkinRough });
        }

        void Decal(string parent, int color, double w, double h, double x, double y, double z, bool toneMapped)
        {
            Decals.Add(new HeroDecalDef { Parent = parent, Color = color, W = w, H = h, X = x, Y = y, Z = z, ToneMapped = toneMapped });
        }

        public HeroBoneDef Bone(string name)
        {
            for (int i = 0; i < Bones.Count; i++) if (Bones[i].Name == name) return Bones[i];
            return null;
        }
    }
}
