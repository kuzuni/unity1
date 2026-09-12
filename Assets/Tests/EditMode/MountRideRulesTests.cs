using System;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Hero;
using Forge.Core.Mounts;
using Forge.Core.World;

namespace Forge.Tests.EditMode
{
    /// <summary>
    /// T11 — 탑승 상수표(scene.json 의 T11 9키)와 정합·드라이버 규칙이 정본 `scene3d.js`(10420~10630 · 10735~10965 · 18669~18740) 의 식과 같은가.
    /// 표 값은 `scene.json`(추출기) 을 읽고, 식은 정본 리터럴을 손으로 계산한 값과 1e-12 로 견준다(T10 `PetSceneRulesTests` 와 같은 길 — 정본 `refreshMount` 는 실물 THREE 씬이 필요해 실행 벡터 대신 식 대조).
    /// </summary>
    public class MountRideRulesTests
    {
        static MountRideDefs _defs;
        static MountRideDefs Defs
        {
            get
            {
                if (_defs != null) return _defs;
                string file = Path.Combine(DataDir.Path, SceneDefs.File);
                _defs = MountRideDefs.From(MiniJson.ParseObject(File.ReadAllText(file)));
                return _defs;
            }
        }

        [Test]
        public void 표를_읽는다_계열5_종별안장14_서서타기_비례_각속도()
        {
            MountRideDefs d = Defs;
            Assert.AreEqual(5, d.Forms.Count, "flat · wheeled · fly · quad · biped");
            MountForm flat = d.Forms["flat"], wheeled = d.Forms["wheeled"], fly = d.Forms["fly"], quad = d.Forms["quad"], biped = d.Forms["biped"];
            Assert.IsTrue(flat.Stand); Assert.AreEqual(0.193, flat.Saddle, 1e-12); Assert.AreEqual(0.10, flat.Hover, 1e-12); Assert.IsFalse(flat.HasBulk);
            Assert.IsTrue(wheeled.SeatByLeg); Assert.AreEqual(0.36, wheeled.Saddle, 1e-12); Assert.AreEqual(0, wheeled.Hover); Assert.IsNotNull(wheeled.BarReach); Assert.AreEqual(-1.2, wheeled.BarReach.Shoulder, 1e-12);
            Assert.IsTrue(fly.HasBulk); Assert.AreEqual(1.28, fly.Bulk, 1e-12); Assert.AreEqual(0.04, fly.Hover, 1e-12); Assert.AreEqual(0.38, fly.Saddle, 1e-12); Assert.IsNotNull(fly.ReinReach);
            Assert.AreEqual(0.44, quad.Saddle, 1e-12); Assert.IsFalse(quad.Stand); Assert.IsFalse(quad.NoNarrow);
            Assert.IsTrue(biped.NoNarrow); Assert.AreEqual(0.72, biped.Saddle, 1e-12);
            Assert.AreEqual(14, d.SaddleOf.Count);
            Assert.AreEqual(1.45, d.StandBulk, 1e-12); Assert.AreEqual(1.45, d.SeatRatio, 1e-12); Assert.AreEqual(0.74, d.WidthRatio, 1e-12);
            Assert.AreEqual(5.4, d.Gait, 1e-12); Assert.AreEqual(1.7, d.IdleGait, 1e-12);
            // RIDE_STAND_POSE — 5본 · 축별 가산 객체(숫자 아님)
            Assert.AreEqual(5, d.StandPose.Items.Count);
            PoseDelta hipL = d.StandPose.Get("hipL");
            Assert.IsFalse(hipL.IsNumber); Assert.AreEqual(0.04, hipL.Rx, 1e-12); Assert.AreEqual(0, hipL.Ry); Assert.AreEqual(-0.10, hipL.Rz, 1e-12);
            Assert.AreEqual(-0.16, d.StandPose.Get("kneeR").Rx, 1e-12);
            Assert.AreEqual(0.04, d.StandPose.Get("spine").Rx, 1e-12);
            // 계열 pose 도 같은 꼴 — flat 의 보드 스탠스(두 다리 ry 같은 부호 · spine.ry 로 되돌림)
            Assert.AreEqual(1.34, flat.Pose.Get("hipL").Ry, 1e-12); Assert.AreEqual(1.34, flat.Pose.Get("hipR").Ry, 1e-12); Assert.AreEqual(-0.62, flat.Pose.Get("spine").Ry, 1e-12);
        }

        [Test]
        public void mountFormOf_종별_안장은_사본에_덮고_없는_종은_quad_그대로()
        {
            MountRideDefs d = Defs;
            MountForm quad = d.Forms["quad"];
            Assert.AreSame(quad, d.FormOfName("Elk"), "표에 없는 종 → quad 그 객체(Elk 는 일부러 비워 둔 종)");
            Assert.AreSame(quad, d.FormOfName("Donkey"), "종별 안장이 계열값(0.44)과 같으면 사본을 안 만든다");
            Assert.AreSame(quad, d.FormOfName(null));
            MountForm pony = d.FormOfName("Pony");
            Assert.AreNotSame(quad, pony); Assert.AreEqual(0.425, pony.Saddle, 1e-12); Assert.AreEqual(quad.Hover, pony.Hover); Assert.AreSame(quad.Pose, pony.Pose); Assert.AreSame(quad.ReinReach, pony.ReinReach);
            Assert.AreEqual(0.44, quad.Saddle, 1e-12, "원본은 안 바뀐다");
            Assert.AreEqual(0.515, d.FormOfName("Camel").Saddle, 1e-12);
            Assert.AreEqual("flat", d.FormOfName("Hover Board").Key); Assert.AreEqual("flat", d.FormOfName("Hover Disk").Key);
            Assert.AreEqual("wheeled", d.FormOfName("Bike").Key); Assert.AreEqual("wheeled", d.FormOfName("Dump Truck").Key); Assert.AreEqual("wheeled", d.FormOfName("Cleaning Robot").Key);
            Assert.AreEqual("fly", d.FormOfName("Pterosaur").Key); Assert.AreEqual("fly", d.FormOfName("Star Whale").Key);
            Assert.AreEqual("biped", d.FormOfName("Bipedal Mech").Key);
            Assert.AreEqual("quad", d.FormOfName("Mech Spider").Key);
        }

        [Test]
        public void 등급_스케일과_칸_크기()
        {
            string[] rar = DataDir.Game.Defs.Rarities;
            Assert.AreEqual(1.1, MountRideRules.Scale(rar, rar[0]), 1e-12);
            Assert.AreEqual(1.1 + 0.1 * (rar.Length - 1), MountRideRules.Scale(rar, rar[rar.Length - 1]), 1e-12);
            Assert.AreEqual(1.0, MountRideRules.Scale(rar, "없는등급"), 1e-12, "indexOf −1 (JS 그대로)");
            MobModel pony = DataDir.Game.Mounts.Get("Pony");
            Assert.IsNotNull(pony); Assert.IsTrue(pony.Seat.HasValue, "탈것 표에는 cell 이 없고 seat 이 있다");
            MountForm f = Defs.FormOfName("Pony");
            Assert.AreEqual(f.Saddle / pony.Seat.Value, MountRideRules.Cell(f, pony), 1e-12, "cell = form.saddle / model.seat");
            Assert.AreEqual(0, MountRideRules.Cell(f, new MobModel()), "seat 없으면 조립기 기본 칸");
        }

        [Test]
        public void 정합_서서타기_계열별()
        {
            MountRideDefs d = Defs;
            double pelvis = 0.4375, drop = MountRideRules.SeatDropFallback, sc = 1.3;
            // 사족(Pony 안장 0.425): needSaddle = pelvis·1.45 · rideScale = clamp(need ÷ (saddle·sc), 1, 3.4) · 서서 → ×1.45 · heroY = saddle·sc·rideScale · 가로 좁히기 해제 · baseY 0
            MountForm pony = d.FormOfName("Pony");
            RideFit q = MountRideRules.Fit(d, pony, sc, pelvis, drop);
            double rs = Math.Max(1, Math.Min(3.4, pelvis * 1.45 / (0.425 * sc))) * 1.45;
            Assert.IsTrue(q.Standing);
            Assert.AreEqual(rs, q.RideScale, 1e-12); Assert.AreEqual(0, q.RideWide); Assert.AreEqual(rs, q.Wide, 1e-12);
            Assert.AreEqual((0 + 0.425) * sc * rs, q.HeroY, 1e-12); Assert.AreEqual(0, q.BaseY);
            // 클램프 하한: 안장이 이미 높으면(작은 골반) rideScale = 1 → ×1.45
            RideFit lo = MountRideRules.Fit(d, pony, 3.0, 0.3, drop);
            Assert.AreEqual(1.45, lo.RideScale, 1e-12);
            // 자전거(seatByLeg): needSaddle = 0.06 + pelvis · 가로는 원래도 안 좁힌다
            RideFit w = MountRideRules.Fit(d, d.Forms["wheeled"], sc, pelvis, drop);
            Assert.AreEqual(Math.Max(1, Math.Min(3.4, (0.06 + pelvis) / (0.36 * sc))) * 1.45, w.RideScale, 1e-12);
            Assert.AreEqual(0.36 * sc * w.RideScale, w.HeroY, 1e-12);
            // 비행: rideScale = bulk 1.28 × 1.45 · heroY = (0.04 + 0.38)·sc·rideScale · baseY = 0.04·sc·rideScale
            RideFit f = MountRideRules.Fit(d, d.Forms["fly"], sc, pelvis, drop);
            Assert.IsTrue(f.Standing);
            Assert.AreEqual(1.28 * 1.45, f.RideScale, 1e-12); Assert.AreEqual((0.04 + 0.38) * sc * 1.28 * 1.45, f.HeroY, 1e-12); Assert.AreEqual(0.04 * sc * 1.28 * 1.45, f.BaseY, 1e-12);
            // 이족(noNarrow · 안장 0.72): 서서 타므로 사족과 같은 식
            RideFit b = MountRideRules.Fit(d, d.Forms["biped"], sc, pelvis, drop);
            Assert.AreEqual(Math.Max(1, Math.Min(3.4, pelvis * 1.45 / (0.72 * sc))) * 1.45, b.RideScale, 1e-12); Assert.AreEqual(0, b.RideWide);
            // 평판(stand): 원래 서 있으니 크기 그대로 · heroY = (0.10 + 0.193)·sc · baseY = 0.10·sc
            RideFit p = MountRideRules.Fit(d, d.Forms["flat"], sc, pelvis, drop);
            Assert.IsFalse(p.Standing);
            Assert.AreEqual(1, p.RideScale); Assert.AreEqual(0, p.RideWide); Assert.AreEqual((0.10 + 0.193) * sc, p.HeroY, 1e-12); Assert.AreEqual(0.10 * sc, p.BaseY, 1e-12);
            Assert.IsFalse(MountRideRules.Flying(d.Forms["flat"]), "평판은 hover 가 있어도 비행 리듬이 아니다"); Assert.IsTrue(MountRideRules.Flying(d.Forms["fly"])); Assert.IsFalse(MountRideRules.Flying(pony));
            // 자세: 서서 타면 RIDE_STAND_POSE · 평판은 계열 pose · 손은 서면 null
            Assert.AreSame(d.StandPose, MountRideRules.RidePose(d, pony)); Assert.AreSame(d.Forms["flat"].Pose, MountRideRules.RidePose(d, d.Forms["flat"]));
            Assert.IsNull(MountRideRules.Reach(pony, true, true, true));
            Assert.IsNull(MountRideRules.Reach(d.Forms["flat"], false, false, false), "평판은 bar/rein 이 없다");
            FreeHandReach r = MountRideRules.Reach(d.Forms["wheeled"], false, true, false);
            Assert.IsNotNull(r); Assert.AreEqual(-1.2, r.Shoulder, 1e-12); Assert.AreEqual(0.1, r.ShoulderZ, 1e-12); Assert.AreEqual(-0.2, r.Elbow, 1e-12);
        }

        [Test]
        public void 접지_보정_발판_재측정_골반_가드()
        {
            Assert.AreEqual(0.051, MountRideRules.Lift(-0.051), 1e-12); Assert.AreEqual(0, MountRideRules.Lift(0.2));
            MountForm flat = Defs.Forms["flat"];
            Assert.AreEqual(0.13 + 0.193 * 1.2, MountRideRules.DeckTop(flat, 1.2, 1, 0.13), 1e-12);
            Assert.AreEqual(0.05, MountRideRules.DeckSnap(0.4, 0.35), 1e-12);
            Assert.AreEqual(0, MountRideRules.DeckSnap(0.4, 1.1), "0.6 이상 어긋나면 안 맞춘다"); Assert.AreEqual(0, MountRideRules.DeckSnap(0.4, double.PositiveInfinity));
            double last = 0;
            Assert.AreEqual(0.4375, MountRideRules.PelvisLocal(double.NaN, ref last), 1e-12, "실측 없음 · 마지막 통과값 없음 → 0.4375");
            Assert.AreEqual(0.44, MountRideRules.PelvisLocal(0.44, ref last), 1e-12); Assert.AreEqual(0.44, last, 1e-12);
            Assert.AreEqual(0.44, MountRideRules.PelvisLocal(1.4, ref last), 1e-12, "대역 밖(프레임 섞임 오염) → 마지막 통과값");
            Assert.AreEqual(0.77, MountRideRules.PelvisLocal(0.77, ref last), 1e-12, "성인 비례도 대역 안");
        }

        [Test]
        public void 바운스_기울기_사망높이()
        {
            double t = 1.234;
            Assert.AreEqual(Math.Sin(t * 1.9) * 0.13 * 1.35, MountRideRules.Bob(true, t, true), 1e-12);
            Assert.AreEqual(Math.Sin(t * 1.9) * 0.13, MountRideRules.Bob(true, t, false), 1e-12);
            Assert.AreEqual(Math.Abs(Math.Sin(t * 4)) * 0.05 * 1.6, MountRideRules.Bob(false, t, true), 1e-12);
            Assert.AreEqual(Math.Abs(Math.Sin(t * 4)) * 0.05, MountRideRules.Bob(false, t, false), 1e-12);
            Assert.AreEqual(Math.Sin(t * 1.9) * 0.11, MountRideRules.FollowerBob(true, t, true), 1e-12, "무리 비행형은 걸어도 진폭 0.11");
            Assert.AreEqual(Math.Abs(Math.Sin(t * 4)) * 0.05 * 1.6, MountRideRules.FollowerBob(false, t, true), 1e-12);
            Assert.AreEqual(Math.Sin(t * 4) * 0.07 + 0.02, MountRideRules.Lean(t, true, 0.02), 1e-12); Assert.AreEqual(0.02, MountRideRules.Lean(t, false, 0.02), 1e-12);
            Assert.AreEqual(0.1 + (0.5 - 0.1) * Math.Min(1, 0.05 * 8), MountRideRules.Smooth(0.1, 0.5, 0.05), 1e-12);
            Assert.AreEqual(0.5, MountRideRules.Smooth(0.1, 0.5, 1), 1e-12, "dt·8 ≥ 1 이면 바로 목표");
            Assert.AreEqual(0.3 * 0.6, MountRideRules.HeroPitch(0.3), 1e-12);
            Assert.AreEqual(1.1 * 1 + 0.7, MountRideRules.Time(1.1, 1, 0.7), 1e-12); Assert.AreEqual(1.1 * 0.9 + 0.7, MountRideRules.Time(1.1, 0.9, 0.7), 1e-12); Assert.AreEqual(1.1 + 0.7, MountRideRules.Time(1.1, 0, 0.7), 1e-12, "speed 0 → 1 (JS `||`)");
            Assert.AreEqual(0, MountRideRules.DeathHeroY(1.2, true, 0, 0.85), "쓰러지면 지면");
            double k = 1 - 0.3 / 0.85;
            Assert.AreEqual(1.2 * k * k * (3 - 2 * k), MountRideRules.DeathHeroY(1.2, false, 0.3, 0.85), 1e-12, "기상 중 smoothstep 으로 되돌아온다");
            Assert.AreEqual(1.2, MountRideRules.DeathHeroY(1.2, false, 0, 0.85), 1e-12);
        }

        [Test]
        public void 파츠_드라이버_식()
        {
            MountRideDefs d = Defs;
            double t = 0.83, dt = 1.0 / 60;
            double w = MountPartDriver.GaitW(d, true), wi = MountPartDriver.GaitW(d, false);
            Assert.AreEqual(5.4, w, 1e-12); Assert.AreEqual(1.7, wi, 1e-12);
            Assert.AreEqual(Math.Sin(t * 5.4) * 0.62, MountPartDriver.LegRx(t, w, true, 1), 1e-12, "gait > 0 위상 0");
            Assert.AreEqual(Math.Sin(t * 5.4 + Math.PI) * 0.62, MountPartDriver.LegRx(t, w, true, -1), 1e-12, "반대 짝은 반 사이클");
            Assert.AreEqual(Math.Sin(t * 1.7) * 0.62 * 0.3, MountPartDriver.LegRx(t, wi, false, 1), 1e-12, "서 있으면 amp 0.3");
            Assert.AreEqual(Math.Sin(t * 5.4 * 0.5) * 0.11, MountPartDriver.HeadRx(t, w, true), 1e-12);
            Assert.AreEqual(Math.Sin(t * 1.7 * 0.5) * 0.11 * 0.55, MountPartDriver.HeadRx(t, wi, false), 1e-12);
            Assert.AreEqual(Math.Sin(t * 0.9) * 0.09, MountPartDriver.HeadRy(t), 1e-12);
            Assert.AreEqual(-1 * (0.45 + Math.Sin(t * 11) * 0.62), MountPartDriver.WingRz(t, -1), 1e-12);
            double kc = Math.Sin(t * 5.2);
            Assert.AreEqual(-0.34 - Math.Max(0, kc) * 0.34, MountPartDriver.ClawRx(t, true, 1), 1e-12);
            double kci = Math.Sin(t * 2.4 + Math.PI);
            Assert.AreEqual(-0.34 - Math.Max(0, kci) * 0.18, MountPartDriver.ClawRx(t, false, -1), 1e-12);
            Assert.AreEqual(Math.Sin(t * 3.4) * 0.5, MountPartDriver.TailRz(t), 1e-12); Assert.AreEqual(Math.Sin(t * 2.1) * 0.24, MountPartDriver.TailRy(t), 1e-12);
            Assert.AreEqual(5.4 * dt, MountPartDriver.SpinnerStep(true, dt), 1e-12); Assert.AreEqual(1.6 * dt, MountPartDriver.SpinnerStep(false, dt), 1e-12);
            Assert.AreEqual(7.6 * dt, MountPartDriver.WheelStep(true, dt), 1e-12); Assert.AreEqual(1.0 * dt, MountPartDriver.WheelStep(false, dt), 1e-12);
            Assert.AreEqual(0.72 + Math.Sin(t * 6.2) * 0.28, MountPartDriver.GlowK(t, true), 1e-12); Assert.AreEqual(0.72 + Math.Sin(t * 6.2) * 0.28 * 0.55, MountPartDriver.GlowK(t, false), 1e-12);
            Assert.AreEqual(0.72 + 0.9 * 0.28, MountPartDriver.GlowColorK(0.9), 1e-12);
            Assert.AreEqual(Math.Sin(t * 0.83) * 0.10 * 1.5, MountPartDriver.FlatRoll(t, true), 1e-12); Assert.AreEqual(Math.Sin(t * 0.83) * 0.10, MountPartDriver.FlatRoll(t, false), 1e-12);
            Assert.AreEqual(Math.Sin(t * 1.15) * 0.05 + Math.Sin(t * 3.1) * 0.03, MountPartDriver.Pitch(MountBodyKind.Flat, t, w, true), 1e-12);
            Assert.AreEqual(Math.Sin(t * 1.15) * 0.05, MountPartDriver.Pitch(MountBodyKind.Flat, t, wi, false), 1e-12);
            Assert.AreEqual(Math.Sin(t * 1.6) * 0.06, MountPartDriver.Pitch(MountBodyKind.Wings, t, w, true), 1e-12);
            Assert.AreEqual(Math.Sin(t * 13) * 0.012, MountPartDriver.Pitch(MountBodyKind.Wheels, t, w, true), 1e-12); Assert.AreEqual(Math.Sin(t * 4) * 0.004, MountPartDriver.Pitch(MountBodyKind.Wheels, t, wi, false), 1e-12);
            Assert.AreEqual(Math.Sin(t * 5.4 * 0.5 + 0.9) * 0.035, MountPartDriver.Pitch(MountBodyKind.Legs, t, w, true), 1e-12); Assert.AreEqual(Math.Sin(t * 1.7 * 0.5 + 0.9) * 0.035 * 0.3, MountPartDriver.Pitch(MountBodyKind.Legs, t, wi, false), 1e-12);
            Assert.AreEqual(MountBodyKind.Flat, MountPartDriver.KindOf(true, true, true), "flat 이 먼저"); Assert.AreEqual(MountBodyKind.Wings, MountPartDriver.KindOf(false, true, true)); Assert.AreEqual(MountBodyKind.Wheels, MountPartDriver.KindOf(false, false, true)); Assert.AreEqual(MountBodyKind.Legs, MountPartDriver.KindOf(false, false, false));
        }
    }
}
