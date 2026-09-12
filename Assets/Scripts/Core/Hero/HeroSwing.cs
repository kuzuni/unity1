using System;
using System.Collections.Generic;

namespace Forge.Core.Hero
{
    /// <summary>
    /// 정본 `Scene3D.heroAttack` 의 **스윙 시계**(swing-snap 2026-08-21) — 클립 실재생 길이(dur / SWING_SPD)가 모든 연출의 기준 시계다.
    /// 동작(`WEAPON_TYPES.motion`) → 클립 후보(`CLIP_MAP`) · 접촉 시각 `CONTACT` · 파지각 ↔ 중립 4구간 슬러프(`blendGrip`).
    /// 돌진·트레일·스우시·링(씬 연출)은 T8 전투 씬 몫이고 여기는 «무기 각과 시각» 만 — 값은 `t6-hero.json` 의 `swing` 과 대조.
    /// </summary>
    public static class HeroSwing
    {
        /// <summary>1.8 배속은 회복까지 밀어 여운을 지웠다 → 1.25.</summary>
        public const double SwingSpd = 1.25;
        public const double DefaultContact = 0.45, DefaultClipDur = 0.5;

        /// <summary>정본 `CLIP_MAP`(motion → GLB 클립 이름 후보 · `HeroClips.Resolve` 가 프로시저럴 클립으로 푼다).</summary>
        public static readonly Dictionary<string, string[]> ClipMap = new Dictionary<string, string[]>
        {
            { "slash", new[] { "1H_Melee_Attack_Slice_Diagonal", "1H_Melee_Attack_Slice_Horizontal" } },
            { "chop", new[] { "1H_Melee_Attack_Chop" } },
            { "thrust", new[] { "1H_Melee_Attack_Stab" } },
            { "slam", new[] { "2H_Melee_Attack_Chop", "2H_Melee_Attack_Slice" } },
            { "double", new[] { "Dualwield_Melee_Attack_Slice", "Dualwield_Melee_Attack_Chop" } },
            { "bow", new[] { "2H_Ranged_Shoot", "2H_Ranged_Shooting" } },
            { "gun", new[] { "1H_Ranged_Shoot", "1H_Ranged_Shooting" } },
            { "cast", new[] { "Spellcast_Shoot", "Spellcast_Raise", "Spellcasting", "2H_Ranged_Shoot" } },
            { "throw", new[] { "Throw", "Spellcast_Shoot", "1H_Melee_Attack_Chop" } },
        };

        /// <summary>클립에서 무기가 표적을 지나는 정규화 시각 — 스우시·링·돌진 최전방이 전부 여기에 맞는다.</summary>
        public static readonly Dictionary<string, double> ContactTable = new Dictionary<string, double>
        {
            { "slash", 0.45 }, { "chop", 0.47 }, { "thrust", 0.46 }, { "slam", 0.50 }, { "double", 0.28 },
        };

        public static string[] Candidates(string motion)
        {
            string[] c;
            return motion != null && ClipMap.TryGetValue(motion, out c) ? c : ClipMap["slash"];
        }

        /// <summary>동작의 프로시저럴 클립 이름(없으면 null).</summary>
        public static string ClipName(string motion) { return HeroClips.Resolve(Candidates(motion)); }

        public static double Contact(string motion)
        {
            double c;
            return motion != null && ContactTable.TryGetValue(motion, out c) ? c : DefaultContact;
        }

        /// <summary>이 공격의 총 길이(초) = 클립 dur / SWING_SPD(클립이 없으면 0.5 / SWING_SPD).</summary>
        public static double AttackTime(string motion)
        {
            var clip = HeroClips.Get(ClipName(motion));
            return (clip != null ? clip.Dur : DefaultClipDur) / SwingSpd;
        }

        /// <summary>4구간 경계: ⓐ~G0 파지 유지 → ⓑ~G1 중립으로 전환 → ⓒ~G2 중립(클립 소유 · 접촉 포함) → ⓓ 파지 복귀. 전환 시각은 접촉에서 역산 · 장병기는 폭을 좁힌다.</summary>
        public static void Gates(string motion, out double g0, out double g1, out double g2)
        {
            double contact = Contact(motion);
            double gw = motion == "thrust" ? 0.09 : 0.18;
            g1 = Math.Max(0.10, contact - 0.04);
            g0 = Math.Max(0, g1 - gw);
            g2 = motion == "double" ? 0.86 : 0.74;
        }

        /// <summary>파지각 가중치 w(k) — 1 = 파지 · 0 = 중립(경계에서 각속도 0).</summary>
        public static double GripWeight(string motion, double k)
        {
            double g0, g1, g2;
            Gates(motion, out g0, out g1, out g2);
            if (k <= g0) return 1;
            if (k < g1) return 1 - HeroEase.Smooth((k - g0) / (g1 - g0));
            if (k < g2) return 0;
            return HeroEase.Smooth((k - g2) / (1 - g2));
        }

        /// <summary>정본 `blendGrip(k)` — 중립 (0, bladeRoll, 0) 에서 파지각으로 w 만큼 slerp 한 무기 로컬 쿼터니언(three).</summary>
        public static double[] BlendGrip(string motion, double[] gripRot, double bladeRoll, double k)
        {
            var gripQ = ThreeQuat.FromEulerXYZ(gripRot[0], gripRot[1], gripRot[2]);
            var neutQ = ThreeQuat.FromEulerXYZ(0, bladeRoll, 0);
            return ThreeQuat.Slerp(neutQ, gripQ, GripWeight(motion, k));
        }
    }
}
