using System;
using Forge.Core.Hero;
using Forge.Core.World;

namespace Forge.Core.BattleFx
{
    /// <summary>
    /// 원작 `scene3d.js` 의 피격·처치·공격·카메라 연출 «수치와 곡선»(T8) — `hitEnemy`(12974~) · `killEnemy`(13100~) · `enemyAttack`(12341~) · `driveFlinch`(18295~) ·
    /// `heroHit`(13324~) · `heroAttack`(12037~) · `hitHpBar`/`driveHpBar`(12912~) · `damageNumber` 인자 · `shake`/`fovPunch`. 순수 계산이라 Core 에 두고 dotnet 으로 검증한다.
    /// 씬(Game `EnemyView`·`HeroView`·`HpBar`)은 이것을 읽어 Transform 만 움직인다. 값을 고치려면 정본을 고친다(§1 «정본에서 고칠 것»).
    /// </summary>
    public static class HitRules
    {
        // ── hitEnemy ③ 넉백 + 움찔 ──
        /// <summary>`kb = 0.18 + min(0.34, sev·1.2) + (crit ? 0.12 : 0)`.</summary>
        public static double Knockback(double sev, bool crit) { return 0.18 + Math.Min(0.34, sev * 1.2) + (crit ? 0.12 : 0); }
        /// <summary>`roll = (0.06 + min(0.2, sev·0.7)) · (crit ? 1.3 : 1)`.</summary>
        public static double KnockRoll(double sev, bool crit) { return (0.06 + Math.Min(0.2, sev * 0.7)) * (crit ? 1.3 : 1); }
        /// <summary>넉백 복귀 길이(초) · 복귀 곡선 `p = (1 − k)²`(x = ox + p·kb · rz = −p·roll).</summary>
        public static double KnockDur(bool crit) { return crit ? 0.2 : 0.16; }
        public static double KnockReturn(double k) { return (1 - k) * (1 - k); }

        // ── hitEnemy ④ 히트축 스쿼시 ──
        public static double PunchDur(bool crit) { return crit ? 0.30 : 0.24; }
        public static double PunchHold(bool crit) { return crit ? 0.075 : 0.055; }
        /// <summary>`(0.07 + min(0.07, sev·0.28)) · (crit ? 1.55 : 1)`.</summary>
        public static double PunchAmp(double sev, bool crit) { return (0.07 + Math.Min(0.07, sev * 0.28)) * (crit ? 1.55 : 1); }
        /// <summary>`update` 의 펀치 진폭 — 유지 구간은 최대 압축 · 그 뒤 감쇠 진동 `amp·e^(−4.8v)·cos(v·π·3.2)`.</summary>
        public static double Punch(double elapsed, double dur, double hold, double amp)
        {
            if (elapsed < hold) return amp;
            double v = (elapsed - hold) / Math.Max(1e-4, dur - hold);
            return amp * Math.Exp(-4.8 * v) * Math.Cos(v * Math.PI * 3.2);
        }

        // ── hitEnemy ④-b 플린치 포즈(driveFlinch) ──
        public static double FlinchDur(bool crit) { return crit ? 0.34 : 0.26; }
        /// <summary>`(0.55 + min(0.45, sev·1.8)) · (crit ? 1.45 : 1)`.</summary>
        public static double FlinchAmp(double sev, bool crit) { return (0.55 + Math.Min(0.45, sev * 1.8)) * (crit ? 1.45 : 1); }
        public const double FlinchRise = 0.06;
        /// <summary>진행 v(0→1)의 가중치 — 6% 즉발 스냅 뒤 감쇠 진동 `e^(−3.6u)·cos(u·π·2.4)`.</summary>
        public static double FlinchWeight(double v)
        {
            if (v < FlinchRise) return v / FlinchRise;
            double u = (v - FlinchRise) / (1 - FlinchRise);
            return Math.Exp(-3.6 * u) * Math.Cos(u * Math.PI * 2.4);
        }
        /// <summary>플린치 가산 계수(`driveFlinch` 의 상수) — 몸 rx −0.20 · ry 0.15 · 관절팔 어깨 rx 0.85 / rz ±0.34 / 팔꿈치 0.45 · 폴백 팔 0.8 · 다리 고관절 −0.28 / 무릎 −|a|·0.40 · 기둥 다리 −0.26 · 늑대 다리 −0.34 · 꼬리 rz 0.62 · 날개 rz s·0.5 · 갓 rz 0.30.</summary>
        public const double FlBodyX = -0.20, FlBodyY = 0.15, FlArmSh = 0.85, FlArmShZ = 0.34, FlArmElbow = 0.45, FlArmFallback = 0.8, FlLegHip = -0.28, FlLegKnee = 0.40, FlGleg = -0.26, FlWolfLeg = -0.34, FlTail = 0.62, FlWing = 0.5, FlCap = 0.30;

        // ── hitEnemy ⑤·⑥ HP 바 · 데미지 숫자 ──
        /// <summary>데미지 숫자 등급(`cls`) — 처치 > 스킬 > 크리 > 일반.</summary>
        public static string DmgClass(bool kill, string kind, bool crit)
        {
            if (kill) return "dmg-kill";
            if (kind == "skill") return "dmg-skill";
            return crit ? "dmg-crit" : "dmg";
        }
        /// <summary>숫자 아크 — 상승 `−(30 + sev·18 + (crit ? 12 : 0))`px · 크기 `1 + min(0.3, sev·0.9)` · 좌우 드리프트 rand(6, 26)px · 수명 900ms.</summary>
        public static double DmgRise(double sev, bool crit) { return -(30 + sev * 18 + (crit ? 12 : 0)); }
        public static double DmgPop(double sev) { return 1 + Math.Min(0.3, sev * 0.9); }
        public const double DmgDxMin = 6, DmgDxMax = 26, DmgLifeMs = 900, DmgTopMargin = 20, DmgRiseDefault = 30, DmgSidePad = 4, DmgSlotDx = 54, DmgSlotDy = 26, DmgSlotStep = 28;
        /// <summary>숫자 월드 y = 바 위 `0.135·0.5·bs + 0.30 + (crit ? 0.3 : 0) + (kill ? 0.22 : 0)` · x +0.45 · 지터 rand(0, 0.1).</summary>
        public static double DmgYAboveBar(double barScale, bool crit, bool kill) { return HpBar.BgH * 0.5 * barScale + 0.30 + (crit ? 0.3 : 0) + (kill ? 0.22 : 0); }
        public const double DmgX = 0.45, DmgYJitter = 0.1;
        /// <summary>바가 없을 때의 갈래 `(barY || 1.1)·(baseScale || 1) + 0.135·2.2 + (crit ? 0.3 : 0)`.</summary>
        public static double DmgYFallback(double barY, double baseScale, bool crit) { return (barY != 0 ? barY : 1.1) * (baseScale != 0 ? baseScale : 1) + HpBar.BgH * 2.2 + (crit ? 0.3 : 0); }
        /// <summary>영웅 숫자(`heroHit`): x −0.15 · y = 바 y + 0.135·2.2 · dx −rand(10, 30) · rise −(26 + sev·14) · pop `1 + min(0.25, sev·0.8)`.</summary>
        public const double HeroDmgX = -0.15, HeroDmgYK = 2.2, HeroDmgDxMin = 10, HeroDmgDxMax = 30;
        public static double HeroDmgRise(double sev) { return -(26 + sev * 14); }
        public static double HeroDmgPop(double sev) { return 1 + Math.Min(0.25, sev * 0.8); }

        // ── 카메라(위계 사다리: 스윙 0.15 < 크리 0.2 < 처치 0.3 < 보스 처치 0.42 < 보스 등장 0.4~0.5) ──
        public const double CritShake = 0.2, CritFov = 0.026, CritFovDur = 0.13, KillShake = 0.3, BossKillShake = 0.42, KillFov = 0.034, KillFovDur = 0.15, HeroDownShake = 0.4, HeroKneeShake = 0.1, HeroBodyShake = 0.24;
        /// <summary>`shake(mag)` 는 max 합성 · `update` 감쇠 `mag·0.001^dt` · 오프셋 x ±mag · y ±mag·0.6 · 0.001 아래는 0.</summary>
        public static double ShakeDecay(double mag, double dt) { return mag * Math.Pow(0.001, dt); }
        public const double ShakeYK = 0.6, ShakeEps = 0.001;
        /// <summary>`fovPunch(amount, dur)`: `fov0·(1 − amount·(1 − k)²)`.</summary>
        public static double FovPunch(double fov0, double amount, double k) { return fov0 * (1 - amount * (1 - k) * (1 - k)); }

        // ── heroHit ──
        /// <summary>`kb = 0.16 + min(0.26, sev·1.2)` · 롤 `0.04 + min(0.12, sev·0.5)` · 0.22초 `sin(kπ)` · 셰이크 `min(0.22, 0.05 + sev·0.6)` · sev 기본 0.12.</summary>
        public static double HeroKnockback(double sev) { return 0.16 + Math.Min(0.26, sev * 1.2); }
        public static double HeroKnockRoll(double sev) { return 0.04 + Math.Min(0.12, sev * 0.5); }
        public const double HeroKnockDur = 0.22, HeroSevDefault = 0.12;
        public static double HeroShake(double sev) { return Math.Min(0.22, 0.05 + sev * 0.6); }

        // ── heroAttack 돌진(리그 경로) ──
        /// <summary>돌진 목표 `min(적 x − 0.6, 0.7 + worldX)` · 0.85 비율 · 점프 `sin(kπ)·0.18` · 몸통 비틀기 `0.55 + sin(kπ)·0.55` · 기울임 `−sin(kπ)·0.15`.</summary>
        public const double DashStop = 0.6, DashMaxX = 0.7, DashK = 0.85, DashJump = 0.18, DashTwist = 0.55, DashLean = 0.15;
        public static double Lunge01(double k) { return k < 0.5 ? k * 2 : (1 - k) * 2; }

        // ── enemyAttack(0.3초 · 1930s 카툰 돌진) ──
        public const double EnemyAttackDur = 0.3;
        /// <summary>k(0~1) → 몸 x 변위 dx(코일 +0.14 → 스냅 −0.55 → 오버슈트 복귀)와 공격 스쿼시.</summary>
        public static void Lunge(double k, out double dx, out double atkSq)
        {
            if (k < 0.42)
            {
                double t = k / 0.42;
                dx = 0.14 * (1 - (1 - t) * (1 - t));
                atkSq = -0.13 * t;
            }
            else if (k < 0.64)
            {
                double t = (k - 0.42) / 0.22;
                dx = 0.14 - 0.69 * (1 - Math.Pow(1 - t, 3));
                atkSq = -0.13 + 0.29 * (1 - Math.Pow(1 - t, 2));
            }
            else
            {
                double t = (k - 0.64) / 0.36;
                dx = -0.55 * (1 - HeroEase.Back(t));
                atkSq = 0.16 * (1 - t) * Math.Cos(t * Math.PI * 1.6);
            }
        }
        /// <summary>2관절 팔(armRJ) 와인드업 → 스냅 → 복귀: 어깨 rx · 팔꿈치 rx · 몸통 rz.</summary>
        public static void LungeArm(double k, out double sh, out double elbow, out double rotZ)
        {
            double w = Math.Min(1, k / 0.42), st = k < 0.42 ? 0 : Math.Min(1, (k - 0.42) / 0.22), rec = k > 0.82 ? (k - 0.82) / 0.18 : 0;
            sh = (0.9 * w) * (1 - st) - 1.45 * st * (1 - rec);
            elbow = -(0.35 + 0.75 * w) * (1 - st) - 0.15 * st;
            rotZ = (0.14 * w) * (1 - st) - 0.1 * st * (1 - rec);
        }
        /// <summary>관절 없는 리그 폴백 팔 `−sin(kπ)·1.6`.</summary>
        public static double LungeFallbackArm(double k) { return -Math.Sin(k * Math.PI) * 1.6; }

        // ── killEnemy 쓰러짐 ──
        public static double KillDur(bool isBoss) { return isBoss ? 1.5 : 1.05; }
        public const double KillHitS = 0.025, KillHoldS = 0.18, KillDissolveS = 0.52, KillHitDx = 0.06, KillFallDx = 0.16, KillRestDx = 0.22, KillRoll = 1.45, KillNoLegSquash = 0.22, KillKneeBend = 1.5, KillHipBend = 0.5, KillKneeBase = -0.15;
        public static double KillDownS(bool isBoss) { return isBoss ? 0.42 : 0.3; }
        /// <summary>앞을 세운 붕괴 커브 `0.6·(1 − (1 − f)²) + 0.4·f`.</summary>
        public static double CollapseEase(double f) { return 0.6 * (1 - (1 - f) * (1 - f)) + 0.4 * f; }
        /// <summary>처치 파편색 = 표의 부피 가중 평균색을 0xff7043 쪽으로 45% 섞은 것 · 파편 24(보스 34) · 속도 1.15(1.5) · 크기 1.25(1.7).</summary>
        public static int ShardColor(int shardC) { return Col.FromHex(shardC).Lerp(Col.FromHex(0xff7043), 0.45).Hex; }
        public const int ShardTint = 0xff7043;
        /// <summary>`monsterMesh` 파편색 씨앗: 파츠 색의 부피(box x·y·z) 가중 평균 — 표 파츠(off 제외) 전부.</summary>
        public static int VolumeWeightedColor(System.Collections.Generic.IList<Forge.Core.Data.MobPart> parts)
        {
            double r = 0, g = 0, b = 0, vol = 0;
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                if (p == null || p.Box == null || p.Box.Length < 3) continue;
                double v = (double)p.Box[0] * p.Box[1] * p.Box[2];
                Col c = Col.FromHex(p.C);
                r += c.R * v; g += c.G * v; b += c.B * v; vol += v;
            }
            if (vol <= 0) return 0xff7043;
            return new Col(r / vol, g / vol, b / vol).Hex;
        }
        public static int KillShards(bool isBoss) { return isBoss ? 34 : 24; }
        public static double KillShardSpeed(bool isBoss) { return isBoss ? 1.5 : 1.15; }
        public static double KillShardScale(bool isBoss) { return isBoss ? 1.7 : 1.25; }
        /// <summary>피격 파편 `crit ? 8 : 4 + round(sev·4)` · 색 crit 0xff8a3d / 0xffd54f · dir 0.35 · spread 0.6 · 속도 1.35/1 · 크기 1.25/1 · 불티 `crit ? 10 : 4 + round(sev·5)` · 0xffab40/0xffee58 · 속도 1.9/1.4.</summary>
        public static int HitShards(double sev, bool crit) { return crit ? 8 : 4 + (int)Math.Round(sev * 4); }
        public static int HitSparks(double sev, bool crit) { return crit ? 10 : 4 + (int)Math.Round(sev * 5); }
        public const int HitShardColor = 0xffd54f, CritShardColor = 0xff8a3d, HitSparkColor = 0xffee58, CritSparkColor = 0xffab40, KillSparkColor = 0xffd54f, WhiteSpark = 0xffffff, DustColor = 0xbcaaa4, ReviveSpark = 0x69f0ae;
        public const double HitShardDir = 0.35, HitShardSpread = 0.6;
        /// <summary>접촉점: `pos + (−0.3·배율, 실높이·0.46, 0.12)` · 처치 버스트 = 실높이·0.5.</summary>
        public const double HitPtX = -0.3, HitPtY = 0.46, HitPtZ = 0.12, BurstY = 0.5;

        // ── HP 바(monsterMesh · driveHpBar · hitHpBar) ──
        public static class HpBar
        {
            public const double BgW = 0.86, BgH = 0.135, FgW = 0.8, FgH = 0.09, BgOpacity = 0.82, GhostZ = 0.005, FgZ = 0.01;
            public const int BgColor = 0x0d1114, GhostDefault = 0xffca28, GhostInit = 0xe8a800, FoeFill = 0xe5484d, FoeFillLow = 0xa81b1b, HeroFill = 0x2ebd6b, HeroFillMid = 0xe8a800, HeroFillLow = 0xd63a3a, FoeGhostBig = 0xffd166, HeroGhostBig = 0xd63a3a, GhostSmall = 0xe8a800, FoePip = 0xe5484d, HeroPip = 0x2ebd6b;
            /// <summary>영웅 바 높이(머리 위 여유) · 잔상 추격 속도 · 플래시 감쇠 · 세로 펀치 · 바 흔들림.</summary>
            public const double HeroY = 2.18, GhostMinSpeed = 0.45, GhostSpeedK = 4.5, FlashDecay = 7, FlashLerp = 0.9, FlashOpacity = 0.18, PunchDur = 0.12, PunchK = 0.18, ShakeDecay = 5.5, ShakeX = 0.05, ShakeY = 0.025, ShakeEps = 0.02, GhostEps = 0.004;
            /// <summary>캐럿(`makeOwnerPip`): 외곽 삼각 w 0.075 h 0.13 · 채움 w 0.05 h 0.1 · yTop = 바 바닥 + 0.012 · 채움은 −0.006.</summary>
            public const double PipOuterW = 0.075, PipOuterH = 0.13, PipInnerW = 0.05, PipInnerH = 0.1, PipYTop = 0.012, PipInnerDy = 0.006;
            /// <summary>처치 바 드레인 0.19초(홀드 0.06 · 소진 0.13 · 플래시 0.05 · 펀치 1.26배 0.13초) 뒤 0.12초 페이드(1.15배).</summary>
            public const double KillT = 0.19, KillHold = 0.06, KillDrain = 0.13, KillFlash = 0.05, KillPunch = 0.26, KillPunchT = 0.13, KillFadeT = 0.12, KillFadeGrow = 0.15;

            public static int FillColor(bool foe, double ratio)
            {
                if (foe) return ratio > 0.2 ? FoeFill : FoeFillLow;
                return ratio > 0.5 ? HeroFill : ratio > 0.2 ? HeroFillMid : HeroFillLow;
            }
            public static int GhostColorOnHit(bool foe, double sev) { return foe ? (sev > 0.15 ? FoeGhostBig : GhostSmall) : (sev > 0.15 ? HeroGhostBig : GhostSmall); }
            /// <summary>`hitHpBar`: 잔상 홀드 `0.15 + min(0.13, sev·0.5)` · 흔들림 `min(1.2, 이전 + 0.3 + sev·2.4)`.</summary>
            public static double GhostHold(double sev) { return 0.15 + Math.Min(0.13, sev * 0.5); }
            public static double ShakeAdd(double prev, double sev) { return Math.Min(1.2, prev + 0.3 + sev * 2.4); }
            /// <summary>잔상 추격 한 프레임: `max(ratio, ghost − max(0.45, (ghost − ratio)·4.5)·dt)`.</summary>
            public static double GhostChase(double ghost, double ratio, double dt) { return Math.Max(ratio, ghost - Math.Max(GhostMinSpeed, (ghost - ratio) * GhostSpeedK) * dt); }
            /// <summary>채움 x 오프셋 `−0.4·(1 − v)`.</summary>
            public static double FillX(double v) { return -(FgW / 2) * (1 - v); }
        }
    }
}
