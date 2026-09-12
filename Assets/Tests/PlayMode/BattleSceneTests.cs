using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core;
using Forge.Core.Battle;
using Forge.Core.BattleFx;
using Forge.Core.Data;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Battle;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T8 — 전투 씬: 30초 자동 전투(시뮬 시간 · 프레임마다 0.05초 × 여러 스텝) · 콘솔 빨강 0 · 적 7종 전부 한 번씩 스폰·사망 · 피격이 넉백·숫자·바·셰이크를 내는가 · 처치 뒤 시체가 걷히는가.
    /// 빨간 로그가 나면 러너가 실패시킨다. 전투 규칙은 T7 Core 가 쥐고 여기서는 «씬이 이벤트를 그렸는가» 만 본다.
    /// </summary>
    public class BattleSceneTests
    {
        static GameData _data; static SaveDefs _defs;
        static GameData Data { get { return _data ?? (_data = GameData.LoadDirectory(System.IO.Path.Combine(Application.streamingAssetsPath, "data"))); } }
        static SaveDefs Defs { get { return _defs ?? (_defs = SaveDefs.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(Application.streamingAssetsPath, "data", SaveDefs.FileName)))); } }

        /// <summary>한 방에 잡는 영웅(적 7종이 30초 안에 전부 죽고 영웅은 안 죽게) — 규칙 검증은 T7 몫이라 여기선 강하게.</summary>
        static HeroStats StrongHero()
        {
            return new HeroStats { Atk = Big.Of(5000), Hp = Big.Of(100000), CritCh = 30, CritDmg = 100, AttacksPerSec = 1.1 };
        }

        static IEnumerator Boot()
        {
            BattleScene.AutoBoot = false;
            if (BattleScene.Instance != null) UnityEngine.Object.Destroy(BattleScene.Instance.gameObject);
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
        }

        static BattleScene Make(Func<HeroStats> stats, uint seed)
        {
            var b = UnityEngine.Object.FindAnyObjectByType<Bootstrap>();
            var s = BattleScene.Create(b != null ? b.transform : null, false);
            s.ManualStep = true;
            s.Attach(BattleScene.MakeBattle(Data, Defs, stats, seed), Data, Defs);
            return s;
        }

        /// <summary>시뮬 시간을 민다 — 프레임당 0.05초 스텝 n 개(렌더는 프레임마다).</summary>
        static IEnumerator Run(BattleScene s, double seconds, int stepsPerFrame = 6)
        {
            double t = 0;
            while (t < seconds)
            {
                for (int i = 0; i < stepsPerFrame && t < seconds; i++) { s.Step(0.05f); t += 0.05; }
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator 삼십초_자동_전투_적_7종_전부_스폰_사망_콘솔_빨강_0()
        {
            yield return Boot();
            var s = Make(StrongHero, 12345);
            Assert.IsTrue(s.Ready);
            Assert.AreEqual(BattlePhase.Fight, s.Battle.Phase, "시작 직후 1웨이브 교전");
            Assert.AreEqual(2, s.Enemies.Count, "1웨이브 적 2");
            Assert.IsNotNull(s.Hero.Rig.Bone("pelvis"));
            yield return Run(s, 30);
            Assert.GreaterOrEqual(s.SpawnCount, 7, "30초면 웨이브 3 까지 7마리 이상");
            Assert.AreEqual(7, s.KindsSpawned.Count, "7종 전부 스폰: " + string.Join(",", s.KindsSpawned));
            Assert.AreEqual(7, s.KindsKilled.Count, "7종 전부 사망: " + string.Join(",", s.KindsKilled));
            Assert.Greater(s.HitCount, 7);
            Assert.Greater(s.Numbers.SpawnedTotal, 7, "피격마다 데미지 숫자");
            Assert.IsFalse(s.Hero.Dead, "강한 영웅은 안 죽는다");
            Assert.Greater(s.WorldX, 0, "행군했다(worldX 전진)");
            Assert.IsNotNull(s.LastStageLabel);
            Assert.GreaterOrEqual(s.LastWave, 1);
            foreach (var v in s.Enemies.Values) Assert.IsFalse(v.Removed, "살아 있는 목록엔 걷힌 적이 없다");
            UnityEngine.Object.Destroy(s.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 피격은_넉백_바_숫자_처치는_시체가_걷힌다()
        {
            yield return Boot();
            var s = Make(StrongHero, 777);
            var first = new List<EnemyView>(s.Enemies.Values)[0];
            double x0 = first.G.localPosition.x;
            // 적이 붙어 첫 타가 들어갈 때까지
            int guard = 0;
            while (s.HitCount == 0 && guard++ < 400) { s.Step(0.05f); if (guard % 6 == 0) yield return null; }
            Assert.Greater(s.HitCount, 0, "첫 타격");
            Assert.Greater(s.Numbers.SpawnedTotal, 0, "데미지 숫자");
            Assert.Greater(s.Numbers.DmgSpawned, 0, "dmg 등급 숫자(처치 뒤엔 loot 숫자가 마지막이라 LastClass 로 재지 않는다 · CI 런 58)");
            Assert.Greater(s.KillCount, 0, "한 방이라 처치");
            Assert.IsTrue(first.Dead || first.Removed, "첫 적이 죽었다");
            Assert.IsTrue(first.Bar.Dying || first.Bar.Done, "바 드레인");
            // 시체는 1.05초 뒤 걷힌다
            yield return Run(s, 1.3, 6);
            Assert.IsTrue(first.Removed, "시체 걷힘");
            Assert.IsFalse(s.Enemies.ContainsKey(first.Id));
            UnityEngine.Object.Destroy(s.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 카메라_셰이크는_감쇠하고_원점으로_돌아온다()
        {
            yield return Boot();
            var s = Make(StrongHero, 4242);
            var cam = Camera.main;
            Assert.IsNotNull(cam);
            Vector3 p0 = cam.transform.localPosition;
            double wx0 = s.WorldX;
            s.Shake(HitRules.KillShake);
            s.Step(0.016f);
            Assert.Greater(s.ShakeMag, 0);
            // 첫 타격 전(스폰 x ≥ 3.1 · 속도 ≤ 1.4 → 근접 사거리 1.3 안까지 ≥ 1.29초 + 무기 impact) 1.2초만 민다 — 더 길면 처치·피격 셰이크가 다시 들어와 0 이 아니다(CI 런 58 실측 0.15)
            yield return Run(s, 1.2, 6);
            Assert.AreEqual(0, s.ShakeMag, 1e-9, "0.3·0.001^1.2 ≈ 8e-5 < 0.001 → 0");
            Vector3 p = cam.transform.localPosition;
            Assert.AreEqual(p0.y, p.y, 1e-4f, "y 복귀");
            Assert.AreEqual(p0.x + (float)(s.WorldX - wx0), p.x, 1e-3f, "x = 기준 + worldX");
            UnityEngine.Object.Destroy(s.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 영웅이_죽으면_Death_클립_뒤_기상한다()
        {
            yield return Boot();
            // 맨몸 영웅 + 즉사급 적: 원작 맨몸치로 시작하되 hp 를 1 로 — 첫 적 공격에 쓰러진다
            var s = Make(() => new HeroStats { Atk = Big.Of(1), Hp = Big.Of(1), CritCh = 0, CritDmg = 100, AttacksPerSec = 1.1 }, 99);
            int guard = 0;
            while (!s.Hero.Dead && guard++ < 600) { s.Step(0.05f); if (guard % 6 == 0) yield return null; }
            Assert.IsTrue(s.Hero.Dead, "적 공격에 쓰러진다");
            Assert.AreEqual("", s.Hero.Rig.Solver.State, "Death 는 once 클립");
            // 1.6초 눕기 + 0.85초 기상
            yield return Run(s, 3.2, 6);
            Assert.IsFalse(s.Hero.Dead, "기상했다");
            Assert.AreEqual(BattlePhase.Fight, s.Battle.Phase, "다시 교전");
            UnityEngine.Object.Destroy(s.gameObject);
            yield return null;
        }

        /// <summary>
        /// T54 — «비었는데 초록» 막이. 앞 픽스처가 자동 부팅을 눕힌 **다음** 부팅에는 전투·펫·스킬 연출·탈것이 스스로 서야 한다.
        /// 이것이 깨져 있던 동안 원작 대조용 화면 30장이 전부 영웅·적·펫 0 으로 찍혔다(CI 런 78 · `screen_main.png`).
        /// 「오브젝트가 만들어졌는가」가 아니라 「**앱을 부팅하면** 만들어지는가」를 본다 — 그것이 화면에 사람이 서는 조건이다.
        /// </summary>
        [UnityTest]
        public IEnumerator 자동_부팅을_눕힌_다음_부팅에는_전투와_펫이_스스로_선다()
        {
            // ① 눕히고 한 번 싣는다 — 이 로드만 조용해야 한다(픽스처들이 제 전투를 세우는 길).
            yield return Boot();
            Assert.IsNull(BattleScene.Instance, "눕힌 그 로드에서는 스스로 서지 않는다");

            // ② 아무것도 안 건드리고 다시 싣는다 = 남의 픽스처(UiShotsTests 처럼)가 부팅하는 모습.
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            Assert.IsTrue(BattleScene.AutoBoot, "플래그는 읽히는 즉시 기본값으로 돌아온다");
            Assert.IsNotNull(BattleScene.Instance, "다음 부팅에는 전투 씬이 스스로 선다(이게 없으면 화면에 영웅·적이 없다)");
            Assert.IsNotNull(Forge.Game.Pets.PetParty.Instance, "펫 무리도 스스로 선다");
            Assert.IsNotNull(Forge.Game.SkillFx.SkillFxScene.Instance, "스킬 연출도 스스로 선다");
            Assert.IsNotNull(Forge.Game.Mounts.MountRider.Instance, "탈것도 스스로 선다");

            // 뒤 테스트에 전투 씬을 남기지 않는다(픽스처들이 제 것을 세운다).
            UnityEngine.Object.Destroy(BattleScene.Instance.gameObject);
            yield return null;
        }
    }
}
