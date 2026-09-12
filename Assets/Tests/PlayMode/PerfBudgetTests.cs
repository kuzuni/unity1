using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core;
using Forge.Core.Battle;
using Forge.Core.Data;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Battle;
using Forge.Game.Pets;
using Forge.Game.SkillFx;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T44 — 60fps 예산. 주인 지시 2026-09-12 «60fps 로 프레임 돌아야»(ROUTINE §1 «60fps»).
    /// ⓐ 부팅이 <c>targetFrameRate 60</c> · <c>vSyncCount 0</c> 을 세우는가 ⓑ 전투 **최대 부하**(보스 + 잡몹 + 펫 3 + 스킬 오브젝트 + 히트 파티클 · 데미지 숫자)를
    /// 200프레임 돌려 **메인스레드 게임 시간**(전투 틱 · 애니 · 연출 · 씬 그래프 쓰기)의 평균·p95 와 **프레임당 관리 힙 증가**를 재고 예산 안인가
    /// ⓒ 부하 장면의 드로우콜 재료(렌더러 수 · 공유 재질 수)를 재고 기록한다.
    ///
    /// 🚩 왜 «게임 시간» 인가: CI 러너는 GPU 가 없어 소프트웨어 래스터라이저로 그린다 — 벽시계 프레임 시간은 래스터라이저 속도지 우리 코드가 아니다.
    /// 그래서 <see cref="BattleScene.ManualStep"/> 으로 한 프레임 분의 게임 일감을 **우리가 직접** 밀고 그 구간만 잰다(렌더는 <c>yield return null</c> 밖).
    /// 벽시계(<c>Time.unscaledDeltaTime</c>)는 판정에 넣지 않고 기록만 한다.
    /// </summary>
    public class PerfBudgetTests
    {
        /// <summary>CI 러너(GPU 없음) 통과선 — 폰은 이 2배 여유가 있어도 16.6ms 안(ROUTINE §2 T44).</summary>
        public const double AvgBudgetMs = 8.0, P95BudgetMs = 12.0;

        /// <summary>프레임당 관리 힙 증가 상한(바이트). «0» 은 에디터 플레이모드에서 잴 수 없다 — 결정 기록 참조.</summary>
        public const long GcPerFrameCap = 16 * 1024;

        /// <summary>부하 장면의 복셀 렌더러 상한(회귀 잡이용 · 파츠마다 하나인 구조의 실측 여유).</summary>
        public const int RendererCap = 900;

        const int WarmFrames = 60, MeasureFrames = 200;

        static GameData _data; static SaveDefs _defs;
        static GameData Data { get { return _data ?? (_data = GameData.LoadDirectory(System.IO.Path.Combine(Application.streamingAssetsPath, "data"))); } }
        static SaveDefs Defs { get { return _defs ?? (_defs = SaveDefs.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(Application.streamingAssetsPath, "data", SaveDefs.FileName)))); } }

        /// <summary>안 죽는(체력 1e12) 영웅인데 **빠르게 때린다**(초 5회) — 히트 파티클·데미지 숫자가 끊이지 않는다. 적은 <see cref="Fatten"/> 이 안 죽게 불린다.</summary>
        static HeroStats LoadHero()
        {
            return new HeroStats { Atk = Big.Of(1), Hp = Big.Of(1e12), CritCh = 50, CritDmg = 100, AttacksPerSec = 5, Block = 0 };
        }

        /// <summary>부하 장면이 200프레임 내내 «부하» 로 남게 적 체력을 불린다 — 하나라도 죽으면 웨이브가 넘어가 부하가 빠진다(측정 조건이 바뀐다).</summary>
        static void Fatten(Forge.Core.Battle.Battle battle)
        {
            List<Enemy> alive = battle.AliveEnemies();
            for (int i = 0; i < alive.Count; i++) { alive[i].Hp = Big.Of(1e18); alive[i].MaxHp = alive[i].Hp; }
        }

        sealed class Rig
        {
            public BattleScene S; public PetParty P; public SkillFxScene Fx; public SkillFxDirector D;
            public readonly List<string> Skills = new List<string>();
        }

        Rig rig;

        IEnumerator Boot()
        {
            BattleScene.AutoBoot = false; SkillFxScene.AutoBoot = false; PetParty.AutoBoot = false;
            if (SkillFxScene.Instance != null) UnityEngine.Object.Destroy(SkillFxScene.Instance.gameObject);
            if (PetParty.Instance != null) UnityEngine.Object.Destroy(PetParty.Instance.gameObject);
            if (BattleScene.Instance != null) UnityEngine.Object.Destroy(BattleScene.Instance.gameObject);
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
        }

        /// <summary>한 프레임 분의 게임 일감 — 전투 씬(Stepped 로 스킬 연출까지) + 펫 무리. <c>ManualStep</c> 이라 이들의 Update 는 자리를 비운다.</summary>
        static void StepFrame(Rig r, float dt)
        {
            r.S.Step(dt);
            r.P.Step(r.S.Clock, r.S.WorldX, r.S.Battle.Walking && !r.S.Hero.Attacking && !r.S.Hero.Dead);
        }

        static IEnumerator Run(Rig r, double seconds, float dt)
        {
            double t = 0;
            while (t < seconds) { StepFrame(r, dt); t += dt; yield return null; }
        }

        /// <summary>쿨을 비우고 스킬 둘을 다시 시전한다 — 측정 내내 스킬 오브젝트가 무대에 남아 있게.</summary>
        static void CastSkills(Rig r)
        {
            Fatten(r.S.Battle);
            r.S.Battle.Cooldowns.Clear();
            for (int i = 0; i < r.Skills.Count; i++) r.S.Battle.TryCast(r.Skills[i], false);
        }

        /// <summary>보스 + 잡몹 + 펫 3 + 스킬 오브젝트 2 가 동시에 무대에 선 «최대 부하» 장면을 세운다.</summary>
        IEnumerator MakeLoadScene(uint seed)
        {
            yield return Boot();
            float dt = 1f / Bootstrap.TargetFps;

            var b = UnityEngine.Object.FindAnyObjectByType<Bootstrap>();
            Assert.IsNotNull(b, "SampleScene 에 Bootstrap 이 없다");

            var s = BattleScene.Create(b.transform, false);
            s.ManualStep = true;
            Forge.Core.Battle.Battle battle = BattleScene.MakeBattle(Data, Defs, LoadHero, seed);
            battle.Context.AutoCast = false;
            battle.Context.Skill = id => { SkillDef d = Data.Defs.Skill(id); return d == null ? null : SkillSpec.From(d, Big.Of(1), Big.Of(100), Big.Of(10)); };
            s.Attach(battle, Data, Defs);
            Assert.IsTrue(s.Ready, "전투 씬이 서지 않았다");

            var fx = SkillFxScene.Create(s.transform.parent, false);
            SkillFxDirector d2 = fx.Attach(s, seed);

            var r = new Rig { S = s, Fx = fx, D = d2 };

            // 스킬 둘 — 액터가 큰 것부터(용의 숨결 · 표창 난무), 없으면 표의 앞에서 공격형 둘.
            var want = new List<string> { "dragonfire", "shurikenrun" };
            foreach (SkillDef sd in Data.Defs.SkillDefs) if (want.Contains(sd.Fx) && r.Skills.Count < 2) r.Skills.Add(sd.Id);
            foreach (SkillDef sd in Data.Defs.SkillDefs)
            {
                if (r.Skills.Count >= 2) break;
                if (sd.Type != "heal" && sd.Type != "buff" && !r.Skills.Contains(sd.Id)) r.Skills.Add(sd.Id);
            }
            Assert.AreEqual(2, r.Skills.Count, "스킬 표에서 둘을 못 골랐다");

            var pp = PetParty.Create(b.transform);
            r.P = pp;
            float t0 = Time.realtimeSinceStartup;
            while (!pp.Ready && Time.realtimeSinceStartup - t0 < 30f) yield return null;
            Assert.IsTrue(pp.Ready, "PetParty 가 30초 안에 준비되지 않았다");
            pp.Set(new List<PetSlot> { new PetSlot("Cat", "common"), new PetSlot("Dog", "epic", 1), new PetSlot("Bear", "mythic") });
            Assert.AreEqual(3, pp.Pets.Count, "펫 출전 3");

            // 적이 사거리 안으로 걸어 들어온다
            yield return Run(r, 2.0, dt);
            Fatten(battle);

            // 보스 웨이브로 건너뛴다(원작 마지막 웨이브) — 경고 → 등장 연출 → 보스 스폰
            battle.Wave = battle.TotalWaves() - 1;
            battle.NextWave();
            Assert.AreEqual(BattlePhase.BossWarn, battle.Phase, "보스 경고 단계");
            yield return Run(r, 5.0, dt);

            // 잡몹을 한 벌 더 얹는다(보스 + 앞 웨이브 생존 + 3) — «최대 부하»
            battle.Wave = 3;
            battle.SpawnWave();
            Fatten(battle);
            yield return Run(r, 0.5, dt);

            int alive = battle.AliveEnemies().Count;
            Assert.GreaterOrEqual(alive, 5, "부하 장면 적 5 이상(보스 + 잡몹 4) — 실제 " + alive);
            bool boss = false;
            foreach (Enemy e in battle.AliveEnemies()) if (e.IsBoss) boss = true;
            Assert.IsTrue(boss, "부하 장면에 보스가 있어야 한다");

            CastSkills(r);
            yield return Run(r, 0.6, dt);
            Assert.GreaterOrEqual(d2.Casts.Count, 1, "스킬 오브젝트가 무대에 서야 한다");
            Fatten(battle);

            rig = r;
        }

        [TearDown]
        public void TearDown()
        {
            if (rig != null)
            {
                if (rig.Fx != null) UnityEngine.Object.Destroy(rig.Fx.gameObject);
                if (rig.P != null) UnityEngine.Object.Destroy(rig.P.gameObject);
                if (rig.S != null) UnityEngine.Object.Destroy(rig.S.gameObject);
                rig = null;
            }
        }

        [UnityTest]
        public IEnumerator 부팅이_targetFrameRate_60_vSync_0_을_세운다()
        {
            yield return Boot();
            Assert.IsNotNull(UnityEngine.Object.FindAnyObjectByType<Bootstrap>(), "SampleScene 에 Bootstrap 이 없다");
            Assert.AreEqual(60, Bootstrap.TargetFps, "목표 프레임 상수");
            Assert.AreEqual(Bootstrap.TargetFps, Application.targetFrameRate, "Application.targetFrameRate 가 60 이 아니다");
            Assert.AreEqual(0, QualitySettings.vSyncCount, "vSyncCount 0 이어야 targetFrameRate 가 산다");
        }

        [UnityTest]
        public IEnumerator 전투_최대_부하_200프레임_메인스레드_예산_과_프레임당_GC()
        {
            yield return MakeLoadScene(4400);
            Rig r = rig;
            float dt = 1f / Bootstrap.TargetFps;

            var ms = new double[MeasureFrames];
            var wall = new double[MeasureFrames];
            var sw = new System.Diagnostics.Stopwatch();

            // 예열 — 첫 프레임의 지연 생성(메시·재질·풀)이 측정에 섞이지 않게
            for (int f = 0; f < WarmFrames; f++)
            {
                StepFrame(r, dt);
                if (f % 30 == 0) CastSkills(r);
                yield return null;
            }

            GC.Collect();
            yield return null;
            long gc0 = GC.GetTotalMemory(false);

            for (int f = 0; f < MeasureFrames; f++)
            {
                sw.Restart();
                StepFrame(r, dt);
                if (f % 30 == 0) CastSkills(r);
                sw.Stop();
                ms[f] = sw.Elapsed.TotalMilliseconds;
                wall[f] = Time.unscaledDeltaTime * 1000.0;
                yield return null;
            }
            long gc1 = GC.GetTotalMemory(false);

            double sum = 0, wsum = 0, max = 0;
            for (int i = 0; i < MeasureFrames; i++) { sum += ms[i]; wsum += wall[i]; if (ms[i] > max) max = ms[i]; }
            double avg = sum / MeasureFrames, wallAvg = wsum / MeasureFrames;
            var sorted = new double[MeasureFrames];
            Array.Copy(ms, sorted, MeasureFrames);
            Array.Sort(sorted);
            double p95 = sorted[(int)(MeasureFrames * 0.95)];
            long gcPerFrame = Math.Max(0, (gc1 - gc0)) / MeasureFrames;

            string num = "데미지 숫자 " + r.S.Numbers.SpawnedTotal + " · 파티클 " + (r.S.Fx != null ? r.S.Fx.Count : 0) +
                         " · 적 " + r.S.Battle.AliveEnemies().Count + " · 펫 " + r.P.Pets.Count + " · 스킬 시전 " + r.D.Casts.Count;
            string line = "[T44] 부하 장면 메인스레드 게임 시간 평균 " + avg.ToString("F3") + "ms · p95 " + p95.ToString("F3") +
                          "ms · 최대 " + max.ToString("F3") + "ms · 프레임당 관리 힙 " + gcPerFrame + "B · 벽시계 평균 " +
                          wallAvg.ToString("F2") + "ms(소프트웨어 렌더 포함 · 판정 밖) · " + num;
            Debug.Log(line);

            Assert.Greater(r.S.Numbers.SpawnedTotal, 0, "부하 장면에 데미지 숫자가 없다 — 부하가 아니다");
            Assert.LessOrEqual(avg, AvgBudgetMs, "메인스레드 게임 시간 평균이 예산을 넘었다 — " + line);
            Assert.LessOrEqual(p95, P95BudgetMs, "메인스레드 게임 시간 p95 가 예산을 넘었다 — " + line);
            Assert.LessOrEqual(gcPerFrame, GcPerFrameCap, "프레임당 관리 힙 증가가 상한을 넘었다(풀링이 샌다) — " + line);
        }

        [UnityTest]
        public IEnumerator 부하_장면_드로우콜_재료_렌더러와_공유_재질()
        {
            yield return MakeLoadScene(4401);
            Rig r = rig;
            float dt = 1f / Bootstrap.TargetFps;
            yield return Run(r, 0.5, dt);

            var seen = new HashSet<Material>();
            int renderers = 0;
            var roots = new List<Transform> { r.S.transform, r.P.transform };
            foreach (Transform root in roots)
            {
                MeshRenderer[] mrs = root.GetComponentsInChildren<MeshRenderer>(true);
                renderers += mrs.Length;
                foreach (MeshRenderer mr in mrs) if (mr.sharedMaterial != null) seen.Add(mr.sharedMaterial);
            }

            string stats = "";
#if UNITY_EDITOR
            stats = " · UnityStats 드로우콜 " + UnityEditor.UnityStats.drawCalls + " · 배치 " + UnityEditor.UnityStats.batches +
                    " · SetPass " + UnityEditor.UnityStats.setPassCalls;
#endif
            string line = "[T44] 부하 장면 렌더러 " + renderers + " · 공유 재질 " + seen.Count + stats;
            Debug.Log(line);

            Assert.Greater(renderers, 0, "부하 장면에 복셀 렌더러가 없다");
            Assert.LessOrEqual(renderers, RendererCap, "복셀 렌더러가 상한을 넘었다 — " + line);
            // 정본 `matKey` 공유(VoxelMaterials 전역 캐시)가 살아 있는가 = SRP Batcher 가 한 배치로 묶을 재료.
            Assert.Less(seen.Count, renderers, "파츠마다 재질이 따로다 — matKey 공유가 깨졌다: " + line);
            Assert.LessOrEqual(seen.Count, 64, "공유 재질이 너무 많다(키가 갈렸다) — " + line);
        }
    }
}
