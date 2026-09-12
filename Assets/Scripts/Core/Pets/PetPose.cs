using System;
using Forge.Core.Voxel;

namespace Forge.Core.Pets
{
    /// <summary>
    /// 펫 한 마리의 프레임 자세(정본 `update` 펫 블록 18598~18645) — 몸통 위치·회전(three 오일러 · 라디안)과 관절 각. Game `PetView` 가 값을 넣기만 한다.
    /// 시계 `t = clock · speed + phase` · 몸짓 표 `mo` · 걷는 중 `walking`(정본 `this.walking`).
    /// </summary>
    public struct PetPose
    {
        /// <summary>three 좌표 — x = HERO_X + spotX + worldX + xJitter · y = 0.4 + 바운스 · z = spotZ.</summary>
        public double X, Y, Z;
        /// <summary>three XYZ 오일러 — rx(pitch) · ry(CREATURE_YAW + yaw 흔들림) · rz(sway).</summary>
        public double Rx, Ry, Rz;

        /// <summary>정본 `t = this._clock * (ud.speed || 1) + (ud.phase || 0)`.</summary>
        public static double Time(double clock, double speed, double phase) { return clock * (speed != 0 ? speed : 1) + phase; }

        /// <summary>몸통 자세. `spotX` 는 정본 `ud.spotX || -0.3` 규칙(0 이면 폴백)을 그대로 밟는다.</summary>
        public static PetPose Body(string name, PetMotionSpec mo, double t, double heroX, double spotX, double spotZ, double worldX, bool walking, double creatureYaw)
        {
            double sx = spotX != 0 ? spotX : PetSceneRules.SpotXFallback;
            double homeX = heroX + sx + worldX;
            double walkBoost = walking ? PetSceneRules.WalkBoost : 1;
            double bounce = mo.Hop ? Math.Abs(Math.Sin(t * mo.Freq)) * mo.Amp * walkBoost : Math.Sin(t * mo.Freq) * mo.Amp * walkBoost;
            var p = new PetPose
            {
                X = homeX + PetSceneRules.XJitter(name, t),
                Y = PetSceneRules.BaseY + bounce,
                Z = spotZ,
                Rx = 0, Ry = creatureYaw, Rz = 0,
            };
            if (mo.Sway != 0) p.Rz = Math.Sin(t * mo.Freq * PetSceneRules.SwayFreqK) * mo.Sway;
            if (mo.Yaw != 0) p.Ry = creatureYaw + Math.Sin(t * mo.Freq) * mo.Yaw;
            if (mo.Pitch != 0) p.Rx = Math.Sin(t * mo.Freq) * mo.Pitch;
            return p;
        }

        /// <summary>
        /// 관절 각(정본 `if (ud.joints) for (const j of ud.joints) …`) — `a = t·freq·f + ph` · spin 이면 `base + a` · 아니면 `base + (abs ? |sin a| : sin a)·amp·w`, w = 1 + (gain − 1)·(walking ? 1 : 0).
        /// 위상은 몸통 바운스와 같은 시계·같은 주파수(mo.freq)라 걸음과 바운스가 안 어긋난다.
        /// </summary>
        public static double JointAngle(double t, double freq, bool walking, double jBase, double amp, double ph, double f, double gain, bool abs, bool spin)
        {
            double a = t * freq * f + ph;
            if (spin) return jBase + a;
            double w = 1 + (gain - 1) * (walking ? 1 : 0);
            return jBase + (abs ? Math.Abs(Math.Sin(a)) : Math.Sin(a)) * amp * w;
        }

        public static double JointAngle(double t, double freq, bool walking, MobJointPlan j)
        {
            return JointAngle(t, freq, walking, j.Base, j.Amp, j.Ph, j.F, j.Gain, j.Abs, j.Spin);
        }
    }
}
