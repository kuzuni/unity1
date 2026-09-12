using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Battle;
using Forge.Core.Pets;
using Forge.Game.Battle;
using Forge.Game.Pets;
using Forge.Game.Voxel;

namespace Forge.Tests.PlayMode
{
    /// <summary>T10 — 부팅 씬에 펫 무리가 서고(전투 씬 뒤) 출전 3마리가 정본 자리·등급 크기·3/4 방향으로 서서 영웅을 따라오며 관절이 도는가. 콘솔 빨강 0(러너가 실패시킨다).</summary>
    public class PetSceneTests
    {
        static IEnumerator Boot()
        {
            BattleScene.AutoBoot = true; PetParty.AutoBoot = true;
            if (BattleScene.Instance != null) Object.Destroy(BattleScene.Instance.gameObject);
            if (PetParty.Instance != null) Object.Destroy(PetParty.Instance.gameObject);
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t0 = Time.realtimeSinceStartup;
            while ((PetParty.Instance == null || !PetParty.Instance.Ready) && Time.realtimeSinceStartup - t0 < 30f) yield return null;
            Assert.IsNotNull(PetParty.Instance, "PetParty 가 Bootstrap 아래에 서지 않았다");
            Assert.IsTrue(PetParty.Instance.Ready, "PetParty 가 30초 안에 준비되지 않았다(전투 씬·세이브 대기)");
            Assert.IsTrue(BattleScene.Instance != null && BattleScene.Instance.Ready);
        }

        static List<PetSlot> Three()
        {
            return new List<PetSlot> { new PetSlot("Cat", "common"), new PetSlot("Dog", "epic", 1), new PetSlot("Bear", "mythic") };
        }

        [UnityTest]
        public IEnumerator 출전_3마리가_정본_자리_크기_방향으로_서고_영웅을_따라온다()
        {
            yield return Boot();
            PetParty pp = PetParty.Instance;
            BattleScene s = BattleScene.Instance;
            s.ManualStep = true;
            pp.Set(Three());
            Assert.AreEqual(3, pp.Pets.Count);
            Assert.AreEqual(3, pp.transform.Find("Pets").childCount, "펫 3 = 래퍼 그룹 3");
            string[] rar = s.Data.Defs.Rarities;
            for (int i = 0; i < 3; i++)
            {
                PetView v = pp.Pets[i];
                double[] spot = PetFormation.PetSpot(pp.Defs, i, false);
                Assert.AreEqual(spot[0], v.SpotX, 1e-12); Assert.AreEqual(spot[1], v.SpotZ, 1e-12);
                Assert.Greater(v.Rig.PartCount, 0, v.Name + " 파츠");
                Assert.AreEqual((float)PetSceneRules.Scale(rar, v.Rarity), v.Mesh.localScale.x, 1e-5f, v.Name + " 등급 스케일");
                // 자리: x = HERO_X + spotX + worldX (+ 종별 지터 = Cat/Dog/Bear 0) · z 는 three → 유니티 반전
                Vector3 e = ThreeSpace.Pos(BattleRules.HeroX + spot[0] + s.WorldX, v.Pose.Y, spot[1]);
                Assert.AreEqual(e.x, v.G.localPosition.x, 1e-4f, v.Name + " x"); Assert.AreEqual(e.z, v.G.localPosition.z, 1e-4f, v.Name + " z");
                Assert.GreaterOrEqual(v.Pose.Y, PetSceneRules.BaseY - 0.2, v.Name + " y 는 0.4 근방");
                Assert.AreEqual(pp.Defs.CreatureYaw, v.Pose.Ry, 1e-9, v.Name + " CREATURE_YAW(Cat/Dog/Bear 는 yaw 흔들림 없음)");
                Assert.Less(Quaternion.Angle(ThreeSpace.Rot(v.Pose.Rx, v.Pose.Ry, v.Pose.Rz), v.G.localRotation), 0.01f, v.Name + " 회전");
            }
            Assert.Less(pp.Pets[0].SpotX, 0, "펫은 영웅 뒤(−x)");
            Assert.Greater(pp.Pets[2].Rig.Renderers.Count, 0, "메시가 있다");
            // 관절이 돈다 — 프레임을 밀면 관절 노드의 회전이 바뀐다
            PetView dog = pp.Pets[1];
            Assert.Greater(dog.Rig.Joints.Count, 0, "Dog 표에 관절 서술이 있다");
            Quaternion q0 = dog.Rig.Joints[0].Node.localRotation;
            double y0 = dog.Pose.Y;
            pp.Step(0.37, s.WorldX, true);
            yield return null;
            Assert.Greater(Quaternion.Angle(q0, dog.Rig.Joints[0].Node.localRotation), 0.01f, "관절 각이 시계에 따라 바뀐다");
            Assert.AreNotEqual(y0, dog.Pose.Y, "몸통이 바운스한다(Dog 은 hop)");
            // 따라오기: worldX 가 12 앞서면 x 도 12 앞선다(자리는 그대로)
            float x0 = pp.Pets[0].G.localPosition.x;
            pp.Step(0.37, s.WorldX + 12, true);
            Assert.AreEqual(x0 + 12f, pp.Pets[0].G.localPosition.x, 1e-3f, "영웅 전진을 따라온다");
            // 탑승 표로 바꾸면 4번째부터 후방 격자가 mountedRx 만큼 더 물러난다
            var four = Three(); four.Add(new PetSlot("Griffin", "legendary"));
            pp.Set(four);
            Assert.AreEqual(4, pp.Pets.Count);
            Assert.AreEqual(-pp.Defs.PetArc.Rx0, pp.Pets[3].SpotX, 1e-12);
            pp.SetMounted(true);
            Assert.AreEqual(4, pp.Pets.Count);
            Assert.AreEqual(-(pp.Defs.PetArc.Rx0 + pp.Defs.PetArc.MountedRx), pp.Pets[3].SpotX, 1e-12, "탑승 중 후방 격자");
            pp.SetMounted(false);
            pp.Set(new List<PetSlot>());
            Assert.AreEqual(0, pp.Pets.Count);
            yield return null;
            Assert.AreEqual(0, pp.transform.Find("Pets").childCount, "지운 펫은 트리에서도 사라진다(Destroy 는 프레임 끝)");
            s.ManualStep = false;
        }

        [UnityTest]
        public IEnumerator 없는_종은_건너뛰고_세이브_출전칸_변환은_원작_규칙()
        {
            yield return Boot();
            PetParty pp = PetParty.Instance;
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("펫 표에 «없는종»"));
            pp.Set(new List<PetSlot> { new PetSlot("없는종", "rare"), new PetSlot("Snail", "rare") });
            Assert.AreEqual(1, pp.Pets.Count, "표에 없는 종은 건너뛴다");
            Assert.AreEqual("Snail", pp.Pets[0].Name);
            Assert.AreEqual(1, pp.Pets[0].Index, "자리 인덱스는 출전 순번 그대로(원작 forEach 의 i)");
            var pets = new List<object> { Forge.Core.Data.MiniJson.ParseObject("{\"name\":\"Cat\",\"rarity\":\"epic\",\"stars\":2}"), Forge.Core.Data.MiniJson.ParseObject("{\"name\":\"Dog\",\"rarity\":\"common\"}") };
            var slots = PetParty.SlotsOf(pets, new List<object> { 1.0, 0.0, 5.0, -1.0 });
            Assert.AreEqual(2, slots.Count, "범위 밖 인덱스는 건너뛴다");
            Assert.AreEqual("Dog", slots[0].Name); Assert.AreEqual("Cat", slots[1].Name); Assert.AreEqual(2, slots[1].Stars); Assert.AreEqual(0, slots[0].Stars);
            pp.Set(new List<PetSlot>());
        }
    }
}
