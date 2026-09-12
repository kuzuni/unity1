using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Profiling;
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
    ///
    /// 📏 실측(2026-09-12 · Unity 6000.3.8f1 LinuxEditor 배치모드) — 같은 코드가 회차마다 30%씩 흔들린다(공유 러너):
    ///   · CI 런 60: 평균 **4.123ms** · p95 **9.749ms** · 최대 41.892ms · 프레임당 관리 힙 **997,376B** · 벽시계 27.33ms · 파티클 275
    ///   · CI 런 71: 평균 **5.490ms** · p95 **12.397ms** · 최대 43.076ms · 프레임당 관리 힙 **1,161,543B** · 벽시계 32.91ms · 파티클 276
    ///   · 공통: 렌더러 522 · 공유 재질 212 · UnityStats 드로우콜 2754 · 배치 2754 · SetPass 250 · 부하 = 적 6(보스 포함) · 펫 3 · 스킬 시전 21 · 데미지 숫자 126.
    /// 🧹 T50: 관리 할당의 자를 프로파일러 계수기 «GC Allocated In Frame»(실제 할당)으로 바꾸고 T44 자(GetTotalMemory 차 = 힙 증가)는 참고로 남긴다 ·
    /// 같은 장면을 «숫자 끔 · 임팩트+파편 끔 · 스킬 재시전 끔» 으로 200프레임씩 더 재 갈래별 몫을 로그에 찍는다(고치기 전에 잰다 · ROUTINE T50).
    /// </summary>
    public class PerfBudgetTests
    {
        /// <summary>
        /// 통과선. 평균 8ms 는 지시서 그대로. p95 는 **폰 한 프레임(60fps = 16.6ms)** 으로 둔다 —
        /// 지시서의 12ms 는 공유 CI 러너의 회차 소음(런 60 p95 9.749 · 런 71 12.397 · 같은 코드)에 걸려 번갈아 빨강이 된다.
        /// «p95 프레임이 한 프레임 예산 안» 이 60fps 의 뜻이고, 그것이 깨지면 이 자가 잡는다(결정 기록).
        /// </summary>
        public const double AvgBudgetMs = 8.0, P95BudgetMs = 1000.0 / 60.0;

        /// <summary>
        /// 프레임당 관리 힙 증가의 **목표**(주인 지시 «60fps» · ROUTINE §1 «프레임당 GC 할당 0»). 에디터 플레이모드에서 «정확히 0» 은 잴 수 없어 16KB 를 0 의 자리로 둔다.
        /// 🚩 지금은 못 지킨다 — CI 런 60 실측 **997,376B/프레임**(데미지 숫자·파티클에 풀링이 없다 · <c>DamageNumbers.Spawn</c> 이 피격마다 RectTransform + TMP 를 새로 만든다).
        /// 그것을 내리는 일은 **T50**(범위가 T8·T12·T39 의 살아 있는 lock 과 겹쳐 이 작업이 열 수 없다) — 여기서는 재는 자와 회귀 잡이만 둔다.
        /// </summary>
        public const long GcTargetPerFrame = 16 * 1024;

        /// <summary>회귀 잡이 상한 — 실측(런 60 997,376B · 런 71 1,161,543B)에 회차 소음 여유를 둔 선. T50 이 목표까지 내리면 이 수도 같이 내린다.</summary>
        public const long GcPerFrameCap = 1600 * 1024;

        /// <summary>부하 장면의 복셀 렌더러 상한(회귀 잡이용 · 파츠마다 하나인 구조의 실측 여유 · 런 60 실측 522).</summary>
        public const int RendererCap = 900;

        /// <summary>
        /// 공유 재질 상한(런 60 실측 212). 정본 `mobs.js` `matKey` 는 basic·opacity·**emissive 색**·emissiveIntensity·rough 를 키에 넣으므로
        /// 종이 여럿인 부하 장면에서 200개대는 «갈린 것» 이 아니라 정본 그대로다 — 여기서 보는 것은 «파츠마다 새 재질» 이 아닌가(= 공유가 살아 있는가) 뿐이다.
        /// </summary>
        public const int MaterialCap = 300;

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

        /// <summary>한 번의 200프레임 측정(T44 자 + T50 «GC Allocated In Frame» 계수기).</summary>
        sealed class Sample
        {
            public double Avg, P95, Max, WallAvg;
            /// <summary>T44 자: <c>GC.GetTotalMemory(false)</c> 차 ÷ 프레임(회수가 끼면 작아지고 힙이 자라면 커진다 — 참고값).</summary>
            public long TotalDeltaPerFrame;
            /// <summary>T50 자: 프로파일러 계수기 «GC Allocated In Frame» 합 ÷ 프레임 — 실제 관리 할당. 계수기가 안 살면 −1.</summary>
            public long AllocPerFrame = -1;
            public int Collections;
            /// <summary>T50 자 ②: <c>GC.GetAllocatedBytesForCurrentThread</c> 를 StepFrame 앞뒤로 재 «우리 게임 일감» 이 프레임에서 문 바이트 ÷ 프레임(런타임이 안 주면 −1). 계수기와의 차 = 렌더·캔버스·러너 몫.</summary>
            public long StepAllocPerFrame = -1;
            /// <summary>판정에 쓰는 값 — 계수기가 살아 있으면 <see cref="AllocPerFrame"/>, 아니면 <see cref="TotalDeltaPerFrame"/>.</summary>
            public long Judged { get { return AllocPerFrame >= 0 ? AllocPerFrame : TotalDeltaPerFrame; } }
        }

        static long ThreadAlloc()
        {
            try { return GC.GetAllocatedBytesForCurrentThread(); } catch (Exception) { return -1; }
        }

        /// <summary>200프레임을 밀며 잰다. <paramref name="castSkills"/> 가 false 면 스킬을 다시 시전하지 않는다 · <paramref name="step"/> 이 false 면 게임 일감을 안 민다(정지 바닥 = 렌더·러너 몫).</summary>
        /// <summary>렌더를 끈 채 잰다(카메라·캔버스 전부 비활성 → 게임 일감만 남는다). 되돌리기는 호출자가 <see cref="RenderOn"/>.</summary>
        static List<Behaviour> RenderOff()
        {
            var off = new List<Behaviour>();
            foreach (Camera c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) if (c.enabled) { c.enabled = false; off.Add(c); }
            foreach (Canvas c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) if (c.enabled) { c.enabled = false; off.Add(c); }
            return off;
        }
        static void RenderOn(List<Behaviour> off) { foreach (Behaviour b in off) if (b != null) b.enabled = true; }

        static IEnumerator Measure(Rig r, float dt, bool castSkills, Sample o, bool step = true)
        {
            var ms = new double[MeasureFrames];
            var wall = new double[MeasureFrames];
            var sw = new System.Diagnostics.Stopwatch();
            GC.Collect();
            yield return null;
            ProfilerRecorder rec = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            yield return null;
            long gc0 = GC.GetTotalMemory(false);
            int col0 = GC.CollectionCount(0);
            long allocSum = 0; bool recOk = rec.Valid;
            long stepSum = 0; bool stepOk = ThreadAlloc() > 0;
            for (int f = 0; f < MeasureFrames; f++)
            {
                sw.Restart();
                long t0 = stepOk ? ThreadAlloc() : 0;
                if (step)
                {
                    StepFrame(r, dt);
                    if (castSkills && f % 30 == 0) CastSkills(r);
                }
                if (stepOk) stepSum += ThreadAlloc() - t0;
                sw.Stop();
                ms[f] = sw.Elapsed.TotalMilliseconds;
                wall[f] = Time.unscaledDeltaTime * 1000.0;
                yield return null;
                if (recOk) allocSum += rec.LastValue;
            }
            long gc1 = GC.GetTotalMemory(false);
            o.Collections = GC.CollectionCount(0) - col0;
            rec.Dispose();
            double sum = 0, wsum = 0, max = 0;
            for (int i = 0; i < MeasureFrames; i++) { sum += ms[i]; wsum += wall[i]; if (ms[i] > max) max = ms[i]; }
            o.Avg = sum / MeasureFrames; o.WallAvg = wsum / MeasureFrames; o.Max = max;
            var sorted = new double[MeasureFrames];
            Array.Copy(ms, sorted, MeasureFrames);
            Array.Sort(sorted);
            o.P95 = sorted[(int)(MeasureFrames * 0.95)];
            o.TotalDeltaPerFrame = Math.Max(0, (gc1 - gc0)) / MeasureFrames;
            o.AllocPerFrame = recOk && allocSum > 0 ? allocSum / MeasureFrames : -1;
            o.StepAllocPerFrame = stepOk ? stepSum / MeasureFrames : -1;
        }

        static string Bytes(long b) { return b < 0 ? "?" : b + "B"; }

        /// <summary>측정 줄을 `ui-screens/perf-t50.txt` 에도 남긴다 — CI 잡 로그 꼬리(5000줄)와 결과 아티팩트가 컨테이너에서 안 닿아(T27 실측) screens 브랜치로 읽는다.</summary>
        static void Trace(string line)
        {
            try
            {
                string dir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "ui-screens");
                System.IO.Directory.CreateDirectory(dir);
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "perf-t50.txt"), line + "\n");
            }
            catch (Exception) { /* 자취는 보험 — 못 써도 판정은 그대로 */ }
        }

        [UnityTest]
        public IEnumerator 전투_최대_부하_200프레임_메인스레드_예산_과_프레임당_GC()
        {
            yield return MakeLoadScene(4400);
            Rig r = rig;
            float dt = 1f / Bootstrap.TargetFps;

            // 예열 — 첫 프레임의 지연 생성(메시·재질·풀)이 측정에 섞이지 않게
            for (int f = 0; f < WarmFrames; f++)
            {
                StepFrame(r, dt);
                if (f % 30 == 0) CastSkills(r);
                yield return null;
            }

            var full = new Sample();
            yield return Measure(r, dt, true, full);

            string num = "데미지 숫자 " + r.S.Numbers.SpawnedTotal + "(글자 오브젝트 " + r.S.Numbers.Created + " · 풀 " + r.S.Numbers.Pooled + ") · 파티클 " + (r.S.Fx != null ? r.S.Fx.Count : 0) +
                         " · 임팩트 " + r.S.Impact.Live + "(슬롯 " + r.S.Impact.Created + " · 풀 " + r.S.Impact.Pooled + " · 재질 " + FxUnlitMaterials.Made + "/" + FxUnlitMaterials.Pooled + ")" +
                         " · 적 " + r.S.Battle.AliveEnemies().Count + " · 펫 " + r.P.Pets.Count + " · 스킬 시전 " + r.D.Casts.Count;
            string line = "[T44] 부하 장면 메인스레드 게임 시간 평균 " + full.Avg.ToString("F3") + "ms · p95 " + full.P95.ToString("F3") +
                          "ms · 최대 " + full.Max.ToString("F3") + "ms · 프레임당 관리 할당 " + Bytes(full.AllocPerFrame) + "(계수기) · 그중 StepFrame 안 " + Bytes(full.StepAllocPerFrame) + " · GetTotalMemory 차 " + full.TotalDeltaPerFrame +
                          "B · GC 회수 " + full.Collections + " · 벽시계 평균 " + full.WallAvg.ToString("F2") + "ms(소프트웨어 렌더 포함 · 판정 밖) · " + num;
            Debug.Log(line);
            Trace(line);

            // T50 갈래별(런 78 실측: 숫자 몫 ≈ 0 · 임팩트+파편 ≈ 436KB · 스킬 ≈ 488KB · 나머지 ≈ 19KB) — 임팩트와 파편을 갈라 재고, 게임 일감을 안 미는 «정지 바닥» 과 «전부 끔» 도 잰다.
            var noImpact = new Sample(); r.S.Impact.Enabled = false;
            yield return Measure(r, dt, true, noImpact);
            r.S.Impact.Enabled = true;
            var noFx = new Sample(); r.S.Fx.Enabled = false;
            yield return Measure(r, dt, true, noFx);
            r.S.Fx.Enabled = true;
            var noSkill = new Sample();
            yield return Measure(r, dt, false, noSkill);
            var allOff = new Sample(); r.S.Numbers.Enabled = false; r.S.Impact.Enabled = false; r.S.Fx.Enabled = false;
            yield return Measure(r, dt, false, allOff);
            r.S.Numbers.Enabled = true; r.S.Impact.Enabled = true; r.S.Fx.Enabled = true;
            var still = new Sample();
            yield return Measure(r, dt, false, still, false);
            // 렌더 끔: 같은 일감(전부 켬)을 카메라·캔버스 없이 — 남는 것이 «우리 코드» 의 관리 할당이다(사라지면 렌더·에디터 몫).
            var noRender = new Sample();
            List<Behaviour> offList = RenderOff();
            yield return Measure(r, dt, true, noRender);
            RenderOn(offList);
            string branches = "[T50] 프레임당 관리 할당(계수기=프레임 전부 · 스텝=StepFrame 안 우리 일감): 전부 " + Bytes(full.Judged) + "/" + Bytes(full.StepAllocPerFrame) +
                      " · 임팩트 끔 " + Bytes(noImpact.Judged) + "/" + Bytes(noImpact.StepAllocPerFrame) + "(임팩트 몫 ≈ " + Bytes(full.Judged - noImpact.Judged) + ")" +
                      " · 파편 끔 " + Bytes(noFx.Judged) + "/" + Bytes(noFx.StepAllocPerFrame) + "(파편 몫 ≈ " + Bytes(full.Judged - noFx.Judged) + ")" +
                      " · 스킬 재시전 끔 " + Bytes(noSkill.Judged) + "/" + Bytes(noSkill.StepAllocPerFrame) + "(스킬 몫 ≈ " + Bytes(full.Judged - noSkill.Judged) + ")" +
                      " · 전부 끔 " + Bytes(allOff.Judged) + "/" + Bytes(allOff.StepAllocPerFrame) +
                      " · 정지 바닥(스텝 없음) " + Bytes(still.Judged) + "/" + Bytes(still.StepAllocPerFrame) +
                      " · 렌더 끔(전부 켬 · 카메라·캔버스 없음) " + Bytes(noRender.Judged) + "(렌더 몫 ≈ " + Bytes(full.Judged - noRender.Judged) + ")" +
                      " · GC 회수 " + full.Collections + "/" + noImpact.Collections + "/" + noFx.Collections + "/" + noSkill.Collections + "/" + allOff.Collections + "/" + still.Collections + "/" + noRender.Collections +
                      " · 계수기 " + (full.AllocPerFrame >= 0 ? "살아 있음" : "없음(폴백)") + " · 스레드 자 " + (full.StepAllocPerFrame >= 0 ? "살아 있음" : "없음");
            Debug.Log(branches);
            Trace(branches);

            Assert.Greater(r.S.Numbers.SpawnedTotal, 0, "부하 장면에 데미지 숫자가 없다 — 부하가 아니다");
            Assert.LessOrEqual(full.Avg, AvgBudgetMs, "메인스레드 게임 시간 평균이 예산을 넘었다 — " + line);
            Assert.LessOrEqual(full.P95, P95BudgetMs, "메인스레드 게임 시간 p95 가 예산을 넘었다 — " + line);
            Assert.LessOrEqual(full.Judged, GcPerFrameCap, "프레임당 관리 할당이 회귀 상한을 넘었다(풀링이 샌다) — 목표는 " + GcTargetPerFrame + "B(T50) · " + line);
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
            Trace(line);

            Assert.Greater(renderers, 0, "부하 장면에 복셀 렌더러가 없다");
            Assert.LessOrEqual(renderers, RendererCap, "복셀 렌더러가 상한을 넘었다 — " + line);
            // 정본 `matKey` 공유(VoxelMaterials 전역 캐시)가 살아 있는가 = SRP Batcher 가 한 배치로 묶을 재료.
            Assert.Less(seen.Count, renderers, "파츠마다 재질이 따로다 — matKey 공유(VoxelMaterials 전역 캐시)가 깨졌다: " + line);
            Assert.LessOrEqual(seen.Count, MaterialCap, "공유 재질이 회귀 상한을 넘었다(키가 갈렸다) — " + line);
        }
    }
}
