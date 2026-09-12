using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Battle;
using Forge.Core.Mounts;
using Forge.Core.Pets;
using Forge.Game.Battle;
using Forge.Game.Mounts;
using Forge.Game.Pets;
using Forge.Game.Voxel;

namespace Forge.Tests.PlayMode
{
    /// <summary>T11 — 부팅 씬에 탑승기가 서고(전투 씬 뒤) 탄 탈것이 영웅 발밑에 정합대로 서며 영웅이 그 위에 서고(높이·하체 자세), 무리는 뒤쪽 호에, 파츠가 돌고, 내리면 원래대로. 콘솔 빨강 0(러너가 실패시킨다).</summary>
    public class MountSceneTests
    {
        static IEnumerator Boot()
        {
            BattleScene.AutoBoot = true; PetParty.AutoBoot = true; MountRider.AutoBoot = true;
            if (BattleScene.Instance != null) Object.Destroy(BattleScene.Instance.gameObject);
            if (PetParty.Instance != null) Object.Destroy(PetParty.Instance.gameObject);
            if (MountRider.Instance != null) Object.Destroy(MountRider.Instance.gameObject);
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t0 = Time.realtimeSinceStartup;
            while ((MountRider.Instance == null || !MountRider.Instance.Ready || PetParty.Instance == null || !PetParty.Instance.Ready) && Time.realtimeSinceStartup - t0 < 30f) yield return null;
            Assert.IsNotNull(MountRider.Instance, "MountRider 가 Bootstrap 아래에 서지 않았다");
            Assert.IsTrue(MountRider.Instance.Ready, "MountRider 가 30초 안에 준비되지 않았다(전투 씬·세이브 대기)");
            Assert.IsTrue(BattleScene.Instance != null && BattleScene.Instance.Ready);
            Assert.IsTrue(PetParty.Instance != null && PetParty.Instance.Ready);
        }

        [UnityTest]
        public IEnumerator 사족을_타면_서서_타는_정합대로_영웅이_올라서고_무리는_뒤쪽_호에_선다()
        {
            yield return Boot();
            MountRider mr = MountRider.Instance;
            BattleScene s = BattleScene.Instance;
            PetParty pp = PetParty.Instance;
            s.ManualStep = true;
            mr.Rng = Forge.Core.Data.Rng.Mulberry(7);
            mr.RidePhase = 0.5;
            HeroView hero = s.Hero;
            float groundY = hero.Rig.transform.localPosition.y;
            mr.Set(new MountSlot("Pony", "epic", 1), new List<MountSlot> { new MountSlot("Mini Dragon", "legendary"), new MountSlot("Bike", "rare") });
            Assert.IsTrue(mr.Riding); Assert.IsNotNull(mr.Ridden);
            MountView m = mr.Ridden;
            Assert.AreEqual("Pony", m.Name); Assert.AreEqual("quad", m.Form.Key); Assert.AreEqual(0.425, m.Form.Saddle, 1e-12, "종별 안장(MOUNT_SADDLE_OF)");
            Assert.Greater(m.Rig.PartCount, 0); Assert.Greater(m.Rig.Legs.Count, 0, "조랑말은 다리가 있다");
            string[] rar = s.Data.Defs.Rarities;
            double sc = MountRideRules.Scale(rar, "epic");
            Assert.AreEqual(sc, m.Sc, 1e-12);
            RideFit fit = mr.Fit;
            Assert.IsTrue(fit.Standing, "평판형이 아니면 서서 탄다");
            Assert.AreEqual(0, fit.RideWide, "서면 가로 좁히기 해제");
            Assert.GreaterOrEqual(fit.RideScale, mr.RideDefs.StandBulk, "clamp 하한 1 × RIDE_STAND_BULK");
            Assert.AreEqual((float)(sc * fit.RideScale), m.Mesh.localScale.y, 1e-5f, "세로 배율"); Assert.AreEqual((float)(sc * fit.Wide), m.Mesh.localScale.x, 1e-5f, "가로 배율 = 세로");
            // 영웅: 발이 안장 윗면에 — rideY = (hover + saddle)·sc·rideScale (+ 접지 보정) · 리그 y 가 그만큼 올라간다 · 하체 자세 = RIDE_STAND_POSE
            Assert.GreaterOrEqual(mr.RideY, fit.HeroY - 1e-9, "접지 보정은 더할 뿐 빼지 않는다");
            Assert.AreEqual(m.BaseY - fit.BaseY, mr.RideY - fit.HeroY, 1e-9, "들어 올린 만큼 안장(영웅)도 같이");
            Assert.AreEqual(groundY + (float)mr.RideY, hero.Rig.transform.localPosition.y, 1e-4f, "영웅이 안장 높이에 선다");
            Assert.AreSame(mr.RideDefs.StandPose, mr.RidePose);
            Assert.AreSame(mr.RideDefs.StandPose, hero.Rig.Grip.RidePose, "리그 파지에 탑승 자세가 합성됐다");
            Assert.IsTrue(pp.Mounted, "펫 자리가 탑승 표로 바뀐다");
            // 탈것은 영웅 발밑(x 같음 · z 0) · 3/4 방향
            Assert.AreEqual((float)hero.X, m.G.localPosition.x, 1e-4f); Assert.AreEqual(0f, m.G.localPosition.z, 1e-4f);
            Assert.AreEqual(mr.Defs.CreatureYaw, m.Ry, 1e-12);
            // 무리: 원래 크기(sc 균일) · MOUNT_ARC 자리 · 비행형은 hover·sc 만큼 뜬다
            Assert.AreEqual(2, mr.Followers.Count);
            MountView dragon = mr.Followers[0], bike = mr.Followers[1];
            Assert.AreEqual("fly", dragon.Form.Key); Assert.AreEqual("wheeled", bike.Form.Key);
            double[] spot0 = PetFormation.Spot(0, mr.Defs.MountArc, null), spot1 = PetFormation.Spot(1, mr.Defs.MountArc, null);
            Assert.AreEqual(spot0[0], dragon.SpotX, 1e-12); Assert.AreEqual(spot0[1], dragon.SpotZ, 1e-12); Assert.AreEqual(spot1[0], bike.SpotX, 1e-12);
            Assert.AreEqual(dragon.Sc, (double)dragon.Mesh.localScale.x, 1e-5); Assert.AreEqual(dragon.Sc, (double)dragon.Mesh.localScale.y, 1e-5);
            Assert.AreEqual(dragon.Form.Hover * dragon.Sc, dragon.BaseY, 1e-12); Assert.AreEqual(0, bike.BaseY);
            Assert.Greater(dragon.Rig.Wings.Count, 0, "미니 드래곤은 날개"); Assert.Greater(bike.Rig.Wheels.Count, 0, "자전거는 바퀴");
            Assert.AreEqual(3, mr.transform.Find("Mounts").childCount, "탄 것 1 + 무리 2");
            // 프레임: 다리·머리가 돌고 탈것이 바운스하며 영웅이 같은 바운스·기울기(×0.6)를 받는다
            Quaternion leg0 = m.Rig.Legs[0].Node.localRotation, wing0 = dragon.Rig.Wings[0].Node.localRotation, wheel0 = bike.Rig.Wheels[0].localRotation;
            s.Step(0.37f);
            mr.Step(0.37, s.Clock, true);
            Assert.Greater(Quaternion.Angle(leg0, m.Rig.Legs[0].Node.localRotation), 0.01f, "다리 각이 시계에 따라 바뀐다");
            Assert.Greater(Quaternion.Angle(wing0, dragon.Rig.Wings[0].Node.localRotation), 0.01f, "무리의 날개도 같은 함수로 돈다");
            Assert.Greater(Quaternion.Angle(wheel0, bike.Rig.Wheels[0].localRotation), 0.01f, "바퀴는 dt 로 굴러간다");
            double t = MountRideRules.Time(s.Clock, 1, m.Phase);
            double bob = MountRideRules.Bob(false, t, true);
            Assert.AreEqual(m.BaseY + bob, m.Y, 1e-9, "지상형 바운스 |sin(t·4)|·0.05·1.6");
            Assert.AreEqual(bob, mr.HeroBob, 1e-9, "영웅도 같은 바운스");
            Assert.AreEqual(MountRideRules.HeroPitch(m.Rx), mr.HeroPitch, 1e-9);
            Assert.AreEqual((float)(hero.Y + mr.RideY + bob), hero.Rig.transform.localPosition.y, 1e-4f);
            Assert.AreEqual((float)hero.X, m.G.localPosition.x, 1e-4f, "영웅 발밑을 따라온다");
            Assert.AreEqual(ThreeSpace.Pos(BattleRules.HeroX + spot0[0] + s.WorldX, dragon.Y, spot0[1]).x, dragon.G.localPosition.x, 1e-4f, "무리는 호 자리 + 전진");
            Assert.AreEqual(dragon.BaseY + MountRideRules.FollowerBob(true, MountRideRules.Time(s.Clock, dragon.Speed, dragon.Phase), true), dragon.Y, 1e-9, "무리 비행형 부유 리듬");
            // 두 번 밀어도 기울기가 겹쳐 쌓이지 않는다(같은 프레임 재적용 보호)
            mr.Step(0, s.Clock, true);
            Assert.AreEqual((float)(hero.Y + mr.RideY + bob), hero.Rig.transform.localPosition.y, 1e-4f);
            // 내리기: 지면 복귀 · 자세 제거 · 펫 표 복귀 · 무리도 사라진다
            mr.Set(null, null);
            Assert.IsFalse(mr.Riding); Assert.AreEqual(0, mr.RideY); Assert.IsNull(mr.RidePose); Assert.IsNull(hero.Rig.Grip.RidePose);
            Assert.AreEqual(0, mr.Followers.Count);
            Assert.IsFalse(pp.Mounted);
            s.Step(0.05f);
            mr.Step(0.05, s.Clock, false);
            Assert.AreEqual(groundY, hero.Rig.transform.localPosition.y, 1e-4f, "지면 복귀");
            yield return null;
            Assert.AreEqual(0, mr.transform.Find("Mounts").childCount, "지운 탈것은 트리에서도 사라진다(Destroy 는 프레임 끝)");
            s.ManualStep = false;
        }

        [UnityTest]
        public IEnumerator 평판형은_크기_그대로_발판_위에_서고_비행형은_떠서_부유한다()
        {
            yield return Boot();
            MountRider mr = MountRider.Instance;
            BattleScene s = BattleScene.Instance;
            s.ManualStep = true;
            mr.RidePhase = 0;
            HeroView hero = s.Hero;
            float groundY = hero.Rig.transform.localPosition.y;
            // 평판(Hover Board): stand → 원래 크기 · 계열 pose(보드 스탠스) · 발 재측정으로 발판 위
            mr.Set(new MountSlot("Hover Board", "rare"), null);
            MountView board = mr.Ridden;
            Assert.IsNotNull(board); Assert.AreEqual("flat", board.Form.Key); Assert.IsTrue(board.Flat, "표 flat → 뱅킹·부유 드리프트 갈래");
            Assert.IsFalse(mr.Fit.Standing); Assert.AreEqual(1, mr.Fit.RideScale);
            Assert.AreEqual((float)board.Sc, board.Mesh.localScale.y, 1e-5f, "크기 그대로");
            Assert.AreSame(board.Form.Pose, mr.RidePose);
            Assert.AreEqual(board.Form.Hover * board.Sc, mr.Fit.BaseY, 1e-12, "평판은 hover 0.10 만큼 뜬다");
            Assert.Greater(mr.RideY, 0, "발판 윗면 위");
            Assert.Less(System.Math.Abs(mr.RideY - (board.BaseY + board.Form.Saddle * board.Sc)), MountRideRules.DeckSnapMax, "발 재측정은 0.6 안에서만 움직인다");
            s.Step(0.2f);
            mr.Step(0.2, s.Clock, true);
            Assert.AreNotEqual(0, board.Rz, "평판은 뱅킹(rotation.z)");
            Assert.Less(Quaternion.Angle(ThreeSpace.Rot(mr.HeroPitch, Forge.Core.BattleFx.EnemyGait.HeroYaw, 0), hero.Rig.transform.localRotation), 0.01f, "영웅은 피치(×0.6)·yaw 만 — 뱅킹(rotation.z)은 안 간다(회피·피격 몫)");
            double tb = MountRideRules.Time(s.Clock, 1, board.Phase);
            Assert.AreEqual(board.BaseY + MountRideRules.Bob(false, tb, true), board.Y, 1e-9, "평판은 비행 리듬이 아니다");
            // 비행(Mini Dragon): bulk·stand-bulk 로 키우고 hover 만큼 떠서 부호 있는 사인으로 부유
            mr.Set(new MountSlot("Mini Dragon", "legendary"), null);
            MountView dragon = mr.Ridden;
            Assert.AreEqual("fly", dragon.Form.Key); Assert.IsTrue(mr.Fit.Standing);
            Assert.AreEqual(1.28 * mr.RideDefs.StandBulk, mr.Fit.RideScale, 1e-12);
            Assert.Greater(dragon.BaseY, 0, "hover·sc·rideScale 만큼 뜬다");
            Assert.AreEqual(dragon.BaseY - mr.Fit.BaseY, mr.RideY - mr.Fit.HeroY, 1e-9);
            s.Step(0.2f);
            mr.Step(0.2, s.Clock, false);
            double td = MountRideRules.Time(s.Clock, 1, dragon.Phase);
            Assert.AreEqual(dragon.BaseY + MountRideRules.Bob(true, td, false), dragon.Y, 1e-9, "비행형 sin(t·1.9)·0.13");
            Assert.AreEqual((float)(hero.Y + mr.RideY + MountRideRules.Bob(true, td, false)), hero.Rig.transform.localPosition.y, 1e-4f);
            // 없는 종: 경고 + 타지 않는다
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("탈것 표에 «없는종»"));
            mr.Set(new MountSlot("없는종", "rare"), null);
            Assert.IsFalse(mr.Riding);
            s.Step(0.05f); mr.Step(0.05, s.Clock, false);
            Assert.AreEqual(groundY, hero.Rig.transform.localPosition.y, 1e-4f);
            // 세이브 장착칸 변환: activeMounts[0] = 탄 것(없는 인덱스면 null) · 나머지 = 무리(없는 것은 건너뜀)
            var mounts = new List<object> { Forge.Core.Data.MiniJson.ParseObject("{\"name\":\"Pony\",\"rarity\":\"epic\",\"stars\":2}"), Forge.Core.Data.MiniJson.ParseObject("{\"name\":\"Bike\",\"rarity\":\"rare\"}") };
            MountSlot ridden; List<MountSlot> followers;
            MountRider.SlotsOf(mounts, new List<object> { 1.0, 5.0, 0.0 }, out ridden, out followers);
            Assert.AreEqual("Bike", ridden.Name); Assert.AreEqual(1, followers.Count); Assert.AreEqual("Pony", followers[0].Name); Assert.AreEqual(2, followers[0].Stars);
            MountRider.SlotsOf(mounts, new List<object> { 9.0, 0.0 }, out ridden, out followers);
            Assert.IsNull(ridden, "맨 앞이 없는 인덱스면 타지 않는다(원작 riddenInst null)"); Assert.AreEqual(1, followers.Count);
            MountRider.SlotsOf(mounts, new List<object>(), out ridden, out followers);
            Assert.IsNull(ridden); Assert.AreEqual(0, followers.Count);
            s.ManualStep = false;
        }
    }
}
