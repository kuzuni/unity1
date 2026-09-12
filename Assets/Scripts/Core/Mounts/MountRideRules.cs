using System;
using Forge.Core.Data;
using Forge.Core.Hero;

namespace Forge.Core.Mounts
{
    /// <summary>탑승 정합의 결과(정본 `refreshMount` 의 rideScale · rideWide · heroY · baseY · standing).</summary>
    public struct RideFit
    {
        /// <summary>정본 `standing = !form.stand` — 평판형만 원래 서 있으니 그대로, 나머지 전 계열은 서서 탄다(주인 지시 2026-08-21).</summary>
        public bool Standing;
        /// <summary>세로(y)·길이(z) 배율(sc 에 곱하기 전).</summary>
        public double RideScale;
        /// <summary>가로(x) 배율 — 0 이면 세로와 같다(정본 `rideWide = 0`).</summary>
        public double RideWide;
        /// <summary>영웅 y(정본 `heroY` · 접지 보정 전).</summary>
        public double HeroY;
        /// <summary>탈것 그룹 y(정본 `baseY = form.hover * sc * rideScale` · 접지 보정 전).</summary>
        public double BaseY;
        /// <summary>정본 `const wide = rideWide || rideScale`.</summary>
        public double Wide { get { return RideWide != 0 ? RideWide : RideScale; } }
    }

    /// <summary>
    /// 탑승 씬 규칙(정본 `scene3d.js` `makeMountMesh` 9464 · `refreshMount` 10795~10965 · `refreshMountFollowers` 10968 · `update` 탈것 블록 18669~18730 의 인라인 리터럴) —
    /// 표가 아니라 규칙이라 코드가 출처 주석과 함께 든다(T10 `PetSceneRules` 와 같은 길). 표 값(계열·안장·자세·비례·각속도)은 <see cref="MountRideDefs"/> 가 쥔다.
    /// 좌표는 전부 three(+z 정면 · y 위) — Game 이 <c>ThreeSpace</c> 로 뒤집는다.
    /// </summary>
    public static class MountRideRules
    {
        /// <summary>정본 `makeMountMesh` — `Mobs.build(model, { cell, vivid: 0.2 })`. 등급색은 몸에 안 바른다(`void rarity`).</summary>
        public const double Vivid = 0.2;
        /// <summary>정본 `sc = 1.1 + RARITIES.indexOf(m.rarity) * 0.1` — 탄 것·무리 공통.</summary>
        public const double ScaleBase = 1.1, ScalePerRarity = 0.1;
        /// <summary>정본 `U.clamp(needSaddle / (form.saddle * sc), 1, 3.4)` — 과대·과소 확대 방지.</summary>
        public const double RideScaleMin = 1, RideScaleMax = 3.4;
        /// <summary>정본 `needSaddle = form.seatByLeg ? 0.06 + pelvisLocal : …` — 자전거 피팅의 발 여유.</summary>
        public const double SeatByLegClear = 0.06;
        /// <summary>정본 `rideScale = form.bulk || 1.7` — 비행형에 bulk 가 없을 때.</summary>
        public const double DefaultBulk = 1.7;
        /// <summary>정본 `heroPelvisLocalY` — 실측 가드 대역 0.25~0.95(치비 0.44 · 성인 0.77 둘 다 담는다) · 마지막 통과값 없으면 0.4375.</summary>
        public const double PelvisFallback = 0.4375, PelvisMin = 0.25, PelvisMax = 0.95;
        /// <summary>정본 `heroSeatDropY` 의 리그 없음 폴백 0.149 — 유니티 리그는 스커트 태싯(`seatParts`)이 없어 늘 이 값(서서 타면 안 쓰인다).</summary>
        public const double SeatDropFallback = 0.149;
        /// <summary>정본 평판형 발 높이 실측점 `knee.localToWorld(0, -0.315, 0.045)` — 무릎 본 기준 발바닥.</summary>
        public static readonly double[] FootInKnee = { 0, -0.315, 0.045 };
        /// <summary>정본 `Math.abs(deckTop - footY) < 0.6` — 이보다 어긋나면 실측이 오염된 것이라 안 맞춘다.</summary>
        public const double DeckSnapMax = 0.6;
        /// <summary>정본 `U.rand(0, Math.PI * 2)` · 무리 `U.rand(0.85, 1.2)`.</summary>
        public const double PhaseMax = 6.283185307179586, FollowerSpeedMin = 0.85, FollowerSpeedMax = 1.2;
        /// <summary>정본 탄 탈것 바운스: 비행형 `sin(t·1.9)·0.13·(walking ? 1.35 : 1)` · 지상형 `|sin(t·4)|·0.05·walkBoost(1.6)` · 무리 비행형은 진폭 0.11.</summary>
        public const double BobFlyFreq = 1.9, BobFlyAmp = 0.13, BobFlyWalkK = 1.35, BobFreq = 4, BobAmp = 0.05, WalkBoost = 1.6, FollowerFlyAmp = 0.11;
        /// <summary>정본 `lean = (walking ? sin(t·4)·0.07 : 0) + partPitch` · `rot.x += (lean − rot.x)·min(1, dt·8)` · `heroG.rotation.x = mg.rotation.x · 0.6`.</summary>
        public const double LeanFreq = 4, LeanAmp = 0.07, SmoothK = 8, HeroPitchK = 0.6;

        /// <summary>정본 등급 스케일 — `RARITIES.indexOf(rarity)`(없으면 −1 · JS 그대로).</summary>
        public static double Scale(string[] rarities, string rarity)
        {
            int idx = -1;
            if (rarities != null) for (int i = 0; i < rarities.Length; i++) if (rarities[i] == rarity) { idx = i; break; }
            return ScaleBase + idx * ScalePerRarity;
        }

        /// <summary>정본 `cell = form.saddle / model.seat` — 탈것 표에는 cell 이 없고 안장 높이가 칸 크기를 정한다(T4 결정 19). seat 이 없으면 0(= 조립기 기본 칸).</summary>
        public static double Cell(MountForm form, MobModel model)
        {
            if (model == null || !model.Seat.HasValue || model.Seat.Value <= 0) return 0;
            return form.Saddle / model.Seat.Value;
        }

        /// <summary>정본 `standing = !this.mountFormOf(name).stand`.</summary>
        public static bool Standing(MountForm form) { return !form.Stand; }

        /// <summary>정본 update 의 `flying = form.hover > 0 && !form.stand` — 부유 리듬 분기(탄 것·무리 공통).</summary>
        public static bool Flying(MountForm form) { return form.Hover > 0 && !form.Stand; }

        /// <summary>정본 `heroPelvisLocalY` 의 가드 — 실측이 유한하고 대역 안이면 그 값(그리고 그것이 다음 «마지막 통과값»), 아니면 마지막 통과값, 그것도 없으면(0) 0.4375.</summary>
        public static double PelvisLocal(double measured, ref double last)
        {
            if (!double.IsNaN(measured) && !double.IsInfinity(measured) && measured > PelvisMin && measured < PelvisMax) { last = measured; return measured; }
            return last != 0 ? last : PelvisFallback;
        }

        /// <summary>
        /// 정본 `refreshMount` 의 탑승 정합 — 평판형: 크기 그대로 · heroY = (hover + saddle)·sc / 비행형: rideScale = bulk · heroY = (hover + saddle)·sc·rideScale − seatLocal /
        /// 지상형: needSaddle = seatByLeg ? 0.06 + pelvis : pelvis·1.45 · rideScale = clamp(needSaddle ÷ (saddle·sc), 1, 3.4) · heroY = saddle·sc·rideScale − seatLocal · 가로 = rideScale·0.74(seatByLeg·noNarrow 는 제외).
        /// 그 뒤 **서서 타기**(평판형 외 전부): rideScale ×= RIDE_STAND_BULK · heroY = (hover + saddle)·sc·rideScale · 가로 좁히기 해제. baseY = hover·sc·rideScale.
        /// </summary>
        public static RideFit Fit(MountRideDefs defs, MountForm form, double sc, double pelvisLocal, double seatDrop)
        {
            double rideScale = 1, rideWide = 0, heroY;
            double seatLocal = pelvisLocal - seatDrop;
            if (form.Stand)
            {
                heroY = (form.Hover + form.Saddle) * sc;
            }
            else if (form.Hover != 0)
            {
                rideScale = form.HasBulk ? form.Bulk : DefaultBulk;
                heroY = (form.Hover + form.Saddle) * sc * rideScale - seatLocal;
            }
            else
            {
                double needSaddle = form.SeatByLeg ? SeatByLegClear + pelvisLocal : pelvisLocal * defs.SeatRatio;
                rideScale = Math.Max(RideScaleMin, Math.Min(RideScaleMax, needSaddle / (form.Saddle * sc)));
                heroY = form.Saddle * sc * rideScale - seatLocal;
                if (!form.SeatByLeg && !form.NoNarrow) rideWide = rideScale * defs.WidthRatio;
            }
            bool standing = Standing(form);
            if (standing)
            {
                rideScale *= defs.StandBulk;
                heroY = (form.Hover + form.Saddle) * sc * rideScale;
                rideWide = 0;
            }
            return new RideFit { Standing = standing, RideScale = rideScale, RideWide = rideWide, HeroY = heroY, BaseY = form.Hover * sc * rideScale };
        }

        /// <summary>정본 접지 보정 `lift = max(0, −bbox.min.y)` — 들어 올린 만큼 baseY·heroY 에 같이 더한다(한쪽만 올리면 영웅이 안장에 파묻힌다).</summary>
        public static double Lift(double bboxMinY) { return Math.Max(0, -bboxMinY); }

        /// <summary>정본 평판형 `deckTop = baseY + form.saddle * sc * rideScale`.</summary>
        public static double DeckTop(MountForm form, double sc, double rideScale, double baseY) { return baseY + form.Saddle * sc * rideScale; }

        /// <summary>정본 평판형 발 재측정 — `|deckTop − footY| < 0.6` 이면 heroY 에 더할 양, 아니면 0.</summary>
        public static double DeckSnap(double deckTop, double footY)
        {
            if (double.IsNaN(footY) || double.IsInfinity(footY)) return 0;
            double d = deckTop - footY;
            return Math.Abs(d) < DeckSnapMax ? d : 0;
        }

        /// <summary>정본 `this.ridePose = standing ? RIDE_STAND_POSE : form.pose`.</summary>
        public static PoseAdd RidePose(MountRideDefs defs, MountForm form)
        {
            return Standing(form) ? defs.StandPose : form.Pose;
        }

        /// <summary>
        /// 정본 `freeHandReach()` — 핸들바가 달려 있으면 `barReach`, 고삐가 달려 있으면 `reinReach`, 아니면 null. 서서 타면 정렬 대상(bar·rein)을 비우므로 null 이고,
        /// 유니티 <c>VoxelMob</c> 은 마구(등자·고삐·핸들바)를 아예 안 세우므로 <paramref name="hasBar"/>/<paramref name="hasRein"/> 은 지금 늘 false 다 — 규칙만 옮겨 둔다.
        /// </summary>
        public static FreeHandReach Reach(MountForm form, bool standing, bool hasBar, bool hasRein)
        {
            if (standing) return null;
            if (form.BarReach != null && hasBar) return form.BarReach.ToReach();
            if (form.ReinReach != null && hasRein) return form.ReinReach.ToReach();
            return null;
        }

        /// <summary>정본 탄 탈것 시계 `t = clock + phase` · 무리 `t = clock·speed + phase`.</summary>
        public static double Time(double clock, double speed, double phase) { return clock * (speed != 0 ? speed : 1) + phase; }

        /// <summary>정본 탄 탈것 바운스 — 비행형은 부호가 살아 있는 사인(떠 있는 부유 리듬), 지상형은 `|sin|`(접지 반동).</summary>
        public static double Bob(bool flying, double t, bool walking)
        {
            if (flying) return Math.Sin(t * BobFlyFreq) * BobFlyAmp * (walking ? BobFlyWalkK : 1);
            return Math.Abs(Math.Sin(t * BobFreq)) * BobAmp * (walking ? WalkBoost : 1);
        }

        /// <summary>정본 무리 바운스 — 비행형 `sin(t·1.9)·0.11` · 지상형 `|sin(t·4)|·0.05·(walking ? 1.6 : 1)`.</summary>
        public static double FollowerBob(bool flying, double t, bool walking)
        {
            if (flying) return Math.Sin(t * BobFlyFreq) * FollowerFlyAmp;
            return Math.Abs(Math.Sin(t * BobFreq)) * BobAmp * (walking ? WalkBoost : 1);
        }

        /// <summary>정본 `lean = (walking ? sin(t·4)·0.07 : 0) + partPitch` — 탄 탈것의 목표 피치(쓰러진 영웅이면 partPitch 만).</summary>
        public static double Lean(double t, bool walking, double partPitch)
        {
            return (walking ? Math.Sin(t * LeanFreq) * LeanAmp : 0) + partPitch;
        }

        /// <summary>정본 `rot.x += (target − rot.x) · min(1, dt·8)`.</summary>
        public static double Smooth(double cur, double target, double dt) { return cur + (target - cur) * Math.Min(1, dt * SmoothK); }

        /// <summary>정본 `heroG.rotation.x = mg.rotation.x * 0.6`.</summary>
        public static double HeroPitch(double mountRx) { return mountRx * HeroPitchK; }

        /// <summary>정본 사망·기상 구간의 영웅 높이 — `k = dead ? 0 : 1 − clamp(reviveT / REVIVE_DUR)` · `rideY · k²(3 − 2k)`(안장에서 내려와 지면에 눕고 기상 중 부드럽게 되돌아온다).</summary>
        public static double DeathHeroY(double rideY, bool dead, double reviveT, double reviveDur)
        {
            double k = dead ? 0 : 1 - Math.Max(0, Math.Min(1, reviveDur > 0 ? reviveT / reviveDur : 0));
            return rideY * (k * k * (3 - 2 * k));
        }
    }

    /// <summary>정본 `animateMountParts` 의 몸통 피치 분기 — `ud.flat` → `ud.wings` → `ud.wheels` → 네발(기본) 순서로 처음 맞는 것.</summary>
    public enum MountBodyKind { Flat, Wings, Wheels, Legs }

    /// <summary>
    /// 탈것 파츠 드라이버(정본 `animateMountParts` 10738~10792) — 다리(트롯 · 대각선 짝 같은 위상) · 머리(걸음의 절반 주기 끄덕임 + 좌우 스캔) · 날개 · 집게 · 꼬리 · 태엽 감개·바퀴(누적) · 노즐 밝기 · 평판 뱅킹 · 계열별 몸통 피치.
    /// 🚨 탄 탈것과 무리가 **같은 함수**를 쓴다(정본 주석 — 한쪽에만 넣어 드래곤 날개가 얼어 있던 사고). 각은 three 라디안 — Game 이 `ThreeSpace.Apply` 로 넣는다.
    /// </summary>
    public static class MountPartDriver
    {
        public const double LegAmp = 0.62, IdleAmp = 0.3;
        public const double HeadNodAmp = 0.11, HeadNodIdleK = 0.55, HeadScanFreq = 0.9, HeadScanAmp = 0.09;
        public const double WingBase = 0.45, WingFreq = 11, WingAmp = 0.62;
        public const double ClawFreqMove = 5.2, ClawFreqIdle = 2.4, ClawBase = -0.34, ClawOpenMove = 0.34, ClawOpenIdle = 0.18;
        public const double TailZFreq = 3.4, TailZAmp = 0.5, TailYFreq = 2.1, TailYAmp = 0.24;
        public const double SpinnerMove = 5.4, SpinnerIdle = 1.6, WheelMove = 7.6, WheelIdle = 1.0;
        public const double GlowBase = 0.72, GlowAmp = 0.28, GlowFreq = 6.2, GlowIdleK = 0.55;
        public const double FlatRollFreq = 0.83, FlatRollAmp = 0.10, FlatRollWalkK = 1.5;
        public const double FlatPitchFreq = 1.15, FlatPitchAmp = 0.05, FlatPitchMoveFreq = 3.1, FlatPitchMoveAmp = 0.03;
        public const double WingPitchFreq = 1.6, WingPitchAmp = 0.06;
        public const double WheelPitchFreqMove = 13, WheelPitchFreqIdle = 4, WheelPitchAmpMove = 0.012, WheelPitchAmpIdle = 0.004;
        public const double LegPitchPh = 0.9, LegPitchAmp = 0.035;

        /// <summary>정본 `w = moving ? MOUNT_GAIT : MOUNT_IDLE_GAIT`.</summary>
        public static double GaitW(MountRideDefs defs, bool moving) { return moving ? defs.Gait : defs.IdleGait; }

        /// <summary>정본 `amp = moving ? 1 : 0.3`.</summary>
        public static double Amp(bool moving) { return moving ? 1 : IdleAmp; }

        /// <summary>다리 `rotation.x = sin(t·w + (gait > 0 ? 0 : π)) · 0.62 · amp` — 대각선 짝(gait 부호)끼리 같은 위상 = 트롯.</summary>
        public static double LegRx(double t, double w, bool moving, double gait)
        {
            return Math.Sin(t * w + (gait > 0 ? 0 : Math.PI)) * LegAmp * Amp(moving);
        }

        /// <summary>머리 `rotation.x = sin(t·w·0.5) · 0.11 · (moving ? 1 : 0.55)` — 걸음의 절반 주기.</summary>
        public static double HeadRx(double t, double w, bool moving) { return Math.Sin(t * w * 0.5) * HeadNodAmp * (moving ? 1 : HeadNodIdleK); }

        /// <summary>머리 `rotation.y = sin(t·0.9) · 0.09` — 서 있어도 돈다.</summary>
        public static double HeadRy(double t) { return Math.Sin(t * HeadScanFreq) * HeadScanAmp; }

        /// <summary>날개 `rotation.z = s · (0.45 + sin(t·11) · 0.62)`.</summary>
        public static double WingRz(double t, double s) { return s * (WingBase + Math.Sin(t * WingFreq) * WingAmp); }

        /// <summary>집게 `rotation.x = −0.34 − max(0, k) · (moving ? 0.34 : 0.18)` · `k = sin(t·(moving ? 5.2 : 2.4) + (s > 0 ? 0 : π))` — 기본각에서 더 벌리는 쪽으로만.</summary>
        public static double ClawRx(double t, bool moving, double s)
        {
            double k = Math.Sin(t * (moving ? ClawFreqMove : ClawFreqIdle) + (s > 0 ? 0 : Math.PI));
            return ClawBase - Math.Max(0, k) * (moving ? ClawOpenMove : ClawOpenIdle);
        }

        /// <summary>꼬리 `rotation.z = sin(t·3.4) · 0.5`.</summary>
        public static double TailRz(double t) { return Math.Sin(t * TailZFreq) * TailZAmp; }

        /// <summary>꼬리 `rotation.y = sin(t·2.1) · 0.24`.</summary>
        public static double TailRy(double t) { return Math.Sin(t * TailYFreq) * TailYAmp; }

        /// <summary>태엽 감개 — `rotation.z −= (moving ? 5.4 : 1.6) · dt`(누적 · 서 있어도 감긴다). 반환 = 뺄 양.</summary>
        public static double SpinnerStep(bool moving, double dt) { return (moving ? SpinnerMove : SpinnerIdle) * dt; }

        /// <summary>바퀴 — `rotation.x −= (moving ? 7.6 : 1.0) · dt`(누적 · 사인으로 흔들면 덜덜 떠는 바퀴). 반환 = 뺄 양.</summary>
        public static double WheelStep(bool moving, double dt) { return (moving ? WheelMove : WheelIdle) * dt; }

        /// <summary>노즐 맥동 `k = 0.72 + sin(t·6.2) · 0.28 · (moving ? 1 : 0.55)` — emissiveIntensity 에 곱한다.</summary>
        public static double GlowK(double t, bool moving) { return GlowBase + Math.Sin(t * GlowFreq) * GlowAmp * (moving ? 1 : GlowIdleK); }

        /// <summary>발광 재질이 없을 때(Basic) 색에 곱하는 양 `0.72 + k · 0.28`.</summary>
        public static double GlowColorK(double k) { return GlowBase + k * GlowAmp; }

        /// <summary>평판형 뱅킹 `rotation.z = sin(t·0.83) · 0.10 · (moving ? 1.5 : 1)` — 영웅에게는 전달하지 않는다(rotation.z 는 회피·피격이 쓴다).</summary>
        public static double FlatRoll(double t, bool moving) { return Math.Sin(t * FlatRollFreq) * FlatRollAmp * (moving ? FlatRollWalkK : 1); }

        /// <summary>계열별 몸통 피치(반환 = 그룹 rotation.x 에 더할 양) — flat 부유 드리프트 · wings 비행 피치 · wheels 프레임 진동 · 네발 `sin(t·w·0.5 + 0.9) · 0.035 · amp`.</summary>
        public static double Pitch(MountBodyKind kind, double t, double w, bool moving)
        {
            switch (kind)
            {
                case MountBodyKind.Flat: return Math.Sin(t * FlatPitchFreq) * FlatPitchAmp + (moving ? Math.Sin(t * FlatPitchMoveFreq) * FlatPitchMoveAmp : 0);
                case MountBodyKind.Wings: return Math.Sin(t * WingPitchFreq) * WingPitchAmp;
                case MountBodyKind.Wheels: return Math.Sin(t * (moving ? WheelPitchFreqMove : WheelPitchFreqIdle)) * (moving ? WheelPitchAmpMove : WheelPitchAmpIdle);
                default: return Math.Sin(t * w * 0.5 + LegPitchPh) * LegPitchAmp * Amp(moving);
            }
        }

        /// <summary>정본 분기 순서 — `ud.flat` → `ud.wings` → `ud.wheels` → 네발.</summary>
        public static MountBodyKind KindOf(bool flat, bool hasWings, bool hasWheels)
        {
            if (flat) return MountBodyKind.Flat;
            if (hasWings) return MountBodyKind.Wings;
            if (hasWheels) return MountBodyKind.Wheels;
            return MountBodyKind.Legs;
        }
    }
}
