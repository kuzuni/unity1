using Forge.Core.Data;

namespace Forge.Core.Pets
{
    /// <summary>
    /// 펫 출전 씬 규칙(정본 `scene3d.js` `refreshPets` 9762~9810 · `update` 펫 블록 18598~18645 의 인라인 리터럴) — 표가 아니라 규칙이라 코드가 출처 주석과 함께 든다(T9 `WorldRules` 와 같은 길).
    /// 표 값(`PET_MOTION`·`RARITIES`·`CREATURE_YAW`·`PET_ROW0`·`PET_ARC`)은 <see cref="GameDefs"/>·<see cref="Forge.Core.World.SceneDefs"/> 가 쥔다.
    /// </summary>
    public static class PetSceneRules
    {
        /// <summary>정본 `makePetMesh` — `Mobs.build(model, { vivid: 0.2 })`.</summary>
        public const double Vivid = 0.2;
        /// <summary>정본 `mesh.scale.setScalar((0.85 + RARITIES.indexOf(p.rarity) * 0.14) * 1.5)` — 등급 사다리 × pet-size-place ×1.5.</summary>
        public const double ScaleBase = 0.85, ScalePerRarity = 0.14, SizeMul = 1.5;
        /// <summary>정본 `g.position.set(Combat.HERO_X + spot[0] + worldX, 0.4, spot[1])`.</summary>
        public const double BaseY = 0.4;
        /// <summary>정본 `U.rand(0, Math.PI * 2)` · `U.rand(0.85, 1.25)` — 개체별 위상차·속도차.</summary>
        public const double PhaseMax = 6.283185307179586, SpeedMin = 0.85, SpeedMax = 1.25;
        /// <summary>정본 `PET_MOTION[name] || { freq: 4, amp: 0.08 }`.</summary>
        public const double DefaultFreq = 4, DefaultAmp = 0.08;
        /// <summary>정본 `walkBoost = this.walking ? 1.7 : 1`.</summary>
        public const double WalkBoost = 1.7;
        /// <summary>정본 `if (mo.sway) rot.z = sin(t · freq · 0.8) · sway`.</summary>
        public const double SwayFreqK = 0.8;
        /// <summary>종별 특수 몸짓 — Scorpion/Spider `sin(t·16)·0.03`(옆걸음 스커틀) · Snail `sin(t·0.9)·0.06`(미끄러지듯 왕복).</summary>
        public const double ScuttleFreq = 16, ScuttleAmp = 0.03, SlideFreq = 0.9, SlideAmp = 0.06;
        /// <summary>정본 `ud.spotX || -0.3` — spotX 가 없을 때의 폴백(펫은 항상 있다 · 0 이면 −0.3 이 되는 JS 의 `||` 그대로).</summary>
        public const double SpotXFallback = -0.3;

        /// <summary>정본 등급 스케일 — `RARITIES.indexOf(p.rarity)`(없으면 −1 · JS 그대로) 로 (0.85 + idx·0.14)·1.5.</summary>
        public static double Scale(string[] rarities, string rarity)
        {
            int idx = -1;
            if (rarities != null) for (int i = 0; i < rarities.Length; i++) if (rarities[i] == rarity) { idx = i; break; }
            return (ScaleBase + idx * ScalePerRarity) * SizeMul;
        }

        /// <summary>`PET_MOTION[name] || { freq: 4, amp: 0.08 }`.</summary>
        public static PetMotionSpec MotionOf(GameDefs defs, string name)
        {
            OrderedMap<double> m = null;
            if (defs != null && defs.PetMotion != null && name != null) defs.PetMotion.TryGet(name, out m);
            return PetMotionSpec.From(m);
        }

        /// <summary>종별 x 흔들림 — 정본 `xJitter`.</summary>
        public static double XJitter(string name, double t)
        {
            if (name == "Scorpion" || name == "Spider") return System.Math.Sin(t * ScuttleFreq) * ScuttleAmp;
            if (name == "Snail") return System.Math.Sin(t * SlideFreq) * SlideAmp;
            return 0;
        }
    }

    /// <summary>`PET_MOTION` 한 칸 — freq·amp 는 항상, hop·sway·yaw·pitch 는 있을 때만(JS truthy — 0 은 없음과 같다).</summary>
    public sealed class PetMotionSpec
    {
        public double Freq, Amp;
        public bool Hop;
        public double Sway, Yaw, Pitch;

        public static PetMotionSpec From(OrderedMap<double> m)
        {
            if (m == null) return new PetMotionSpec { Freq = PetSceneRules.DefaultFreq, Amp = PetSceneRules.DefaultAmp };
            double v;
            var s = new PetMotionSpec { Freq = m.Get("freq", 0), Amp = m.Get("amp", 0) };
            s.Hop = m.TryGet("hop", out v) && v != 0;
            s.Sway = m.Get("sway", 0); s.Yaw = m.Get("yaw", 0); s.Pitch = m.Get("pitch", 0);
            return s;
        }
    }
}
