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
using Forge.Game.SkillFx;
using Forge.Game.Voxel;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T12 — 스킬 오브젝트: 원작 `scene3d-skillfx.js` 의 액터 안무가 유니티에서 «실체가 나와서 때리는가». 전투 씬(T8) 위에 <see cref="SkillFxScene"/> 을 붙이고
    /// 표의 스킬 18종을 전부 시전해 ⓐ 액터 등장(종 = 원작 표) ⓑ 타격 ⓒ 퇴장(액터·큐브 0) ⓓ 콘솔 빨강 0 을 본다. 시각 계약(시전 박자 · `*_IMPACT_MS` · `impactAt`)은 따로 잰다.
    /// 빨간 로그가 나면 러너가 실패시킨다.
    /// </summary>
    public class SkillFxTests
    {
        static GameData _data; static SaveDefs _defs;
        static GameData Data { get { return _data ?? (_data = GameData.LoadDirectory(System.IO.Path.Combine(Application.streamingAssetsPath, "data"))); } }
        static SaveDefs Defs { get { return _defs ?? (_defs = SaveDefs.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(Application.streamingAssetsPath, "data", SaveDefs.FileName)))); } }

        /// <summary>안 죽고(체력 1e12) 못 죽이는(공격 1 · 느린 평타) 영웅 — 적이 사거리 안에 살아 있어야 표적 안무가 돈다.</summary>
        static HeroStats SoftHero() { return new HeroStats { Atk = Big.Of(1), Hp = Big.Of(1e12), AttacksPerSec = 0.05, Block = 0 }; }

        /// <summary>원작 표 fx → 등장해야 하는 액터 종(`mobs-skillfx.js`). 화살비는 액터 없이 화살(복셀 투사체)만.</summary>
        static readonly Dictionary<string, string[]> ExpectedActors = new Dictionary<string, string[]>
        {
            { "shurikenrun", new[] { "shuriken" } }, { "arrowrain", new string[0] }, { "burrowworm", new[] { "worm" } },
            { "explode", new[] { "imp", "fireblock" } }, { "beam", new[] { "archer" } }, { "warcry", new[] { "orcchief" } },
            { "meteor", new[] { "rockgolem" } }, { "bolt", new[] { "thunderbird" } }, { "heal", new[] { "angel" } },
            { "breath", new[] { "wyvern" } }, { "guillotine", new[] { "executioner" } }, { "aura", new[] { "statue" } },
            { "nova", new[] { "starbot", "fireblock" } }, { "voidrift", new[] { "voidknight" } }, { "timewarp", new[] { "clockbot", "shuriken" } },
            { "dragonfire", new[] { "firedragon", "fireblock" } }, { "spear", new[] { "spearknight" } }, { "wardshield", new[] { "shieldgolem" } },
        };

        static IEnumerator Boot()
        {
            BattleScene.AutoBoot = false; SkillFxScene.AutoBoot = false;
            // 앞 테스트의 것을 «먼저» 걷고 한 프레임 쉰 뒤 씬을 바꾼다 — 같은 프레임에 씬 언로드까지 겹치면 파괴 순서가 정해져 있지 않다.
            if (SkillFxScene.Instance != null) { SkillFxScene.Instance.Detach(); UnityEngine.Object.Destroy(SkillFxScene.Instance.gameObject); }
            if (BattleScene.Instance != null) UnityEngine.Object.Destroy(BattleScene.Instance.gameObject);
            yield return null;
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
        }

        sealed class Rig { public BattleScene Scene; public SkillFxScene Fx; public SkillFxDirector D; }

        static Rig Make(uint seed)
        {
            var b = UnityEngine.Object.FindAnyObjectByType<Bootstrap>();
            var s = BattleScene.Create(b != null ? b.transform : null, false);
            s.ManualStep = true;
            var battle = BattleScene.MakeBattle(Data, Defs, SoftHero, seed);
            battle.Context.AutoCast = false;
            battle.Context.Skill = Spec;
            s.Attach(battle, Data, Defs);
            var fx = SkillFxScene.Create(s.transform.parent, false);
            var d = fx.Attach(s, seed);
            return new Rig { Scene = s, Fx = fx, D = d };
        }

        /// <summary>T20 `SkillBar.Wire` 가 전투가 새로 서면 세이브의 장착 스킬(기본 powerStrike)·autoCast·Spec 을 문맥에 덮어쓴다 — 이 테스트는 스킬을 «손으로» 시전하므로
        /// 시전 직전마다 자동 시전을 끄고 제공자·쿨타임을 되돌린다(자동 시전이 끼어들면 쿨타임 때문에 TryCast 가 false 가 되고 액터 수가 어긋난다 · CI 런 54·58).</summary>
        static void Manual(BattleScene s, string id = null)
        {
            BattleContext c = s.Battle.Context;
            c.AutoCast = false;
            c.EquippedSkills = new List<string>();
            c.Skill = Spec;
            if (id != null) s.Battle.Cooldowns.Remove(id);
            if (SaveIo.State != null) SaveIo.State.AutoCast = false;
        }
        static SkillSpec Spec(string id) { SkillDef d = Data.Defs.Skill(id); return d == null ? null : SkillSpec.From(d, Big.Of(1), Big.Of(100), Big.Of(10)); }

        /// <summary>사거리(x &lt; 3.2) 안에 산 적이 설 때까지 민다(원작 `Combat.tryCast` 의 표적 조건).</summary>
        static IEnumerator EnsureTarget(BattleScene s)
        {
            for (int i = 0; i < 300; i++)
            {
                foreach (Enemy e in s.Battle.Enemies) if (e.Alive && e.X < BattleRules.SkillRange) yield break;
                Manual(s);
                s.Step(0.1f);
                if (i % 6 == 5) yield return null;
            }
            Assert.Fail("30초 안에 사거리 안 적이 없다");
        }

        static IEnumerator Run(BattleScene s, double seconds, float dt = 0.05f, int stepsPerFrame = 6)
        {
            double t = 0;
            while (t < seconds)
            {
                for (int i = 0; i < stepsPerFrame && t < seconds; i++) { s.Step(dt); t += dt; }
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator 표의_스킬_18종_전부_시전_액터_등장_타격_퇴장_콘솔_빨강_0()
        {
            yield return Boot();
            Rig r = Make(777);
            Assert.IsTrue(r.Scene.Ready); Assert.IsTrue(r.Fx.Ready);
            Assert.AreEqual(20, Data.SkillFx.Count, "mobs-skillfx.json 20종");
            yield return Run(r.Scene, 2.0);   // 적이 사거리 안으로 걸어 들어온다
            int casts = 0;
            foreach (SkillDef d in Data.Defs.SkillDefs)
            {
                yield return EnsureTarget(r.Scene);
                Manual(r.Scene, d.Id);
                int before = r.D.Casts.Count;
                Assert.IsTrue(r.Scene.Battle.TryCast(d.Id, true), d.Id + " 시전(사거리 안 적 필요)");
                r.Scene.Step(0.1f);   // 한 틱(100ms) — 전투 씬이 이벤트를 배수해 SkillFx 로 넘긴다
                Assert.AreEqual(before + 1, r.D.Casts.Count, d.Id + " — SkillEffect 이벤트가 연출로 이어졌다");
                CastRecord rec = r.D.Casts[r.D.Casts.Count - 1];
                Assert.AreEqual(d.Fx, rec.Fx, d.Id);
                Assert.AreEqual(SkillFxDirector.SkillTier(d), rec.Tier, d.Id + " 등급");
                yield return Run(r.Scene, 3.6);
                Assert.GreaterOrEqual(rec.PayloadAt, 0, d.Id + " 페이로드");
                string[] expect = ExpectedActors[d.Fx];
                foreach (string id in expect) Assert.IsTrue(rec.Actors.Contains(id), d.Id + "(" + d.Fx + ") 에 " + id + " 가 나와야 한다 — 나온 것: " + string.Join(",", rec.Actors));
                if (d.Type != "heal" && d.Type != "buff" && d.Fx != "arrowrain") Assert.GreaterOrEqual(rec.Hits, 1, d.Id + " 타격(mcHit)");   // 화살비는 projectileBolt(파편·불티)만
                Assert.GreaterOrEqual(rec.WeightAt, 0, d.Id + " 무게(skillImpactWeight)");
                casts++;
            }
            Assert.AreEqual(18, casts);
            yield return Run(r.Scene, 3.0);
            Assert.AreEqual(0, r.D.Pool.Active, "액터 전부 퇴장");
            Assert.AreEqual(0, r.D.CubeCount, "큐브 전부 정리");
            Assert.IsTrue(r.D.Timeline.Idle, "애니·예약 남은 것 0");
            Assert.AreEqual(0, r.D.Lights.Busy, "광원 리스 전부 반납");
            Assert.GreaterOrEqual(r.D.Pool.ProtoCount, 17, "프로토타입은 종당 1회");
            Assert.GreaterOrEqual(r.Fx.Handled, 36, "SkillCutin 18 + SkillEffect 18(자동 시전이 끼면 그 이상)");
        }

        static SkillDef Def(string id, string fx, string type, string rarity, string color, double? impactAt = null)
        {
            return new SkillDef { Id = id, Fx = fx, Type = type, Rarity = rarity, Color = color, Cd = 10, ImpactAt = impactAt };
        }

        /// <summary>디렉터만 dt 0.01 로 돌린다(적·영웅 자리는 그대로) — 시각을 잰다.</summary>
        static void StepD(SkillFxDirector d, double seconds) { int n = (int)Math.Round(seconds / 0.01); for (int i = 0; i < n; i++) d.Step(0.01); }

        [UnityTest]
        public IEnumerator 시각_계약_시전_박자_와_IMPACT_MS_와_impactAt()
        {
            yield return Boot();
            Rig r = Make(4242);
            yield return Run(r.Scene, 2.0);
            yield return EnsureTarget(r.Scene);
            Manual(r.Scene);
            r.D.Clear();
            List<int> targets = new BattleSceneStage(r.Scene).Targets("single");
            Assert.AreEqual(1, targets.Count, "우선 표적 하나");
            // ⓐ 시전 박자 = castMsFor(fx, tier): 등급 90+26t · 메테오·용 0 · 낙뢰 하한 190
            foreach (SkillDef d in Data.Defs.SkillDefs)
            {
                CastRecord rec = r.D.Cast(d, d.Type == "aoe" ? new BattleSceneStage(r.Scene).Targets("aoe") : d.Type == "single" ? targets : new List<int>());
                StepD(r.D, 0.3);
                double expect = SkillFxDirector.CastMsFor(d.Fx, SkillFxDirector.SkillTier(d)) / 1000.0;
                Assert.AreEqual(expect, rec.PayloadAt - rec.T0, 0.011, d.Id + " 페이로드 지연");
                StepD(r.D, 3.5);
            }
            r.D.Clear();
            // ⓑ 결정적 타격 = *_IMPACT_MS(무게 층 동기 시각) — 처형 430 · 초신성 560 · 공허 540 · 신의 창 500 (페이로드 기준)
            var impact = new Dictionary<string, int> { { "guillotine", SkillFxDirector.GuillotineImpactMs }, { "nova", SkillFxDirector.NovaImpactMs }, { "voidrift", SkillFxDirector.VoidriftImpactMs }, { "spear", SkillFxDirector.GodspearImpactMs } };
            foreach (var kv in impact)
            {
                SkillDef d = r.D.DefByFx(kv.Key);
                CastRecord rec = r.D.Cast(d, targets);
                StepD(r.D, 3.0);
                Assert.AreEqual(kv.Value / 1000.0, rec.BigHit - rec.PayloadAt, 0.03, kv.Key + " 결정적 타격 시각");
                Assert.AreEqual(kv.Value / 1000.0, rec.WeightAt - rec.PayloadAt, 0.03, kv.Key + " 무게 시각");
            }
            // 종말의 화룡: 첫 착탄은 브레스 첫 불덩이(예비 뒤 180ms 지연 + 비행 200ms) — 무게 820ms 언저리
            {
                CastRecord rec = r.D.Cast(r.D.DefByFx("dragonfire"), targets);
                StepD(r.D, 3.0);
                Assert.AreEqual(SkillFxDirector.DragonfireImpactMs / 1000.0, rec.WeightAt - rec.PayloadAt, 0.03, "화룡 무게 시각");
                Assert.That(rec.FirstHit - rec.PayloadAt, Is.InRange(0.8, 1.05), "화룡 첫 착탄");
            }
            // ⓒ 커먼 3종: 오브젝트가 실제로 닿는 순간 = Core 피해 시각(impactAt · 표창 1.55 · 화살 1.35 · 지렁이 1.35)
            foreach (string fx in new[] { "shurikenrun", "arrowrain", "burrowworm" })
            {
                SkillDef d = r.D.DefByFx(fx);
                Assert.IsTrue(d.ImpactAt.HasValue, fx + " impactAt");
                CastRecord rec = r.D.Cast(d, targets);
                StepD(r.D, 4.0);
                if (fx == "arrowrain") Assert.GreaterOrEqual(rec.Hits, 0);   // 화살은 projectileBolt(파편) — mcHit 없음
                else Assert.AreEqual(d.ImpactAt.Value, rec.FirstHit - rec.T0, 0.1, fx + " 첫 착탄 ≈ Core 피해 시각");
                Assert.AreEqual(SkillFxDirector.WeightDelayMs(fx) / 1000.0, rec.WeightAt - rec.PayloadAt, 0.02, fx + " 무게 = 착탄 시각");
            }
            r.D.Clear();
            Assert.AreEqual(0, r.D.Pool.Active); Assert.AreEqual(0, r.D.CubeCount);
        }

        [UnityTest]
        public IEnumerator 프로토타입_한_번_복제_메시_공유_퇴장_정리()
        {
            yield return Boot();
            Rig r = Make(9);
            SkillActorPool pool = r.D.Pool;
            Assert.IsTrue(pool.Has("swordbot")); Assert.IsFalse(pool.Has("nope"));
            Assert.Greater(pool.PartCountOf("swordbot"), 0, "파츠 수(계획 · 거울 확장 포함)");
            SkillActor a = pool.Spawn("swordbot", 1, new Vector3(1, 0, 0), SkillFxDirector.CreatureYaw);
            SkillActor b = pool.Spawn("swordbot", 1.2, new Vector3(2, 0, 0), -SkillFxDirector.CreatureYaw);
            Assert.AreEqual(2, pool.Active);
            Assert.AreEqual(1, pool.ProtoCount, "같은 종은 프로토타입 하나");
            var protoMeshes = new HashSet<Mesh>(pool.MeshesOf("swordbot"));
            int shared = 0;
            foreach (MeshFilter mf in a.G.GetComponentsInChildren<MeshFilter>(true)) { Assert.IsTrue(protoMeshes.Contains(mf.sharedMesh), "복제본 메시 = 프로토타입 공유"); shared++; }
            Assert.AreEqual(pool.PartCountOf("swordbot"), shared);
            foreach (string part in new[] { "armR", "armL", "legL", "legR", "head", "weapon" }) Assert.IsTrue(a.Has(part), part);
            a.RotX("armR", -2.3);
            Assert.AreEqual(-2.3, a.GetRot("armR", 0), 1e-9);
            Assert.AreEqual(ThreeSpace.Pos(1, 0, 0), a.T.localPosition);
            pool.Free(a); pool.Free(b); pool.Free(a);
            yield return null;
            Assert.AreEqual(0, pool.Active);
            Assert.IsTrue(a.Freed);
            Assert.AreEqual(2, pool.Spawned);
            // 프로토타입은 씬에서 보이지 않는다(비활성 홀더 아래)
            foreach (var mf in pool.MeshesOf("swordbot")) Assert.IsNotNull(mf);
        }

        [UnityTest]
        public IEnumerator 낙뢰_예고_번개새는_페이로드가_안_오면_2_6초_뒤_스스로_나간다()
        {
            yield return Boot();
            Rig r = Make(31);
            yield return Run(r.Scene, 2.0);
            Manual(r.Scene);
            r.D.Clear();   // 자동 시전(T20 SkillBar 가 켠 powerStrike)이 남긴 액터를 걷는다 — 여기서부터는 디렉터만 민다
            Assert.AreEqual(0, r.D.Pool.Active);
            List<int> targets = new BattleSceneStage(r.Scene).Targets("single");
            SkillFxDirector.ThunderHandle h = r.D.McThunderTell(targets, 0xfff176, 2);
            Assert.IsNotNull(h); Assert.AreEqual(1, r.D.Pool.Active);
            StepD(r.D, 1.0);
            Assert.AreEqual(1, r.D.Pool.Active, "1초 뒤에도 선회 중");
            StepD(r.D, 2.2);
            Assert.AreEqual(0, r.D.Pool.Active, "2.6초 넘기면 스스로 퇴장");
            Assert.IsTrue(h.Stop && h.A.Freed);
            // 예고 없이 낙뢰가 오면 새를 새로 부른다
            r.D.McThunderStrike(null, targets, 0xfff176, 2);
            Assert.AreEqual(1, r.D.Pool.Active);
            StepD(r.D, 2.0);
            Assert.AreEqual(0, r.D.Pool.Active);
        }

        /// <summary>T52 — 원작 `skillCastBeat` 시전 포즈: 1박 동안 `heroG.rotation.z` 가 −0.14·k²(지원계 −0.07)로 젖고, 2박 발화 시각에 +0.30 내질렀다가 0.14초에 0.</summary>
        [UnityTest]
        public IEnumerator 시전_젖힘_1박_동안_기울고_릴리즈에_내질러_제자리()
        {
            yield return Boot();
            Rig r = Make(52);
            yield return Run(r.Scene, 2.0);
            Manual(r.Scene);
            r.D.Clear();
            HeroView h = r.Scene.Hero;
            // 평타·넉백이 z 를 쥐고 있지 않은 순간을 잡는다(원작 `_attacking` 양보) — 그 뒤로는 디렉터만 밀므로 영웅 상태가 안 바뀐다
            for (int i = 0; i < 200 && h.LeanBusy; i++) { Manual(r.Scene); r.Scene.Step(0.05f); if (i % 6 == 5) yield return null; }
            Assert.IsFalse(h.LeanBusy, "10초 안에 평타·넉백이 없는 순간이 있어야 한다");
            r.D.Clear();
            Assert.AreEqual(0, h.LeanZ, 1e-9, "시작은 0");
            foreach (var c in new[] { new { Fx = "slash", Z = SkillFxDirector.CastLeanZ }, new { Fx = "heal", Z = SkillFxDirector.CastLeanSupportZ } })
            {
                double dur = SkillFxDirector.CastBeatMs(5) / 1000.0;   // mythic 1박 220ms
                r.D.SkillCastBeat(0xffffff, c.Fx, 5);
                StepD(r.D, 0.21);
                double k = 0.21 / dur;
                Assert.AreEqual(-c.Z * k * k, h.LeanZ, 1e-3, c.Fx + " 1박 중 젖힘 = −z·k²");
                StepD(r.D, 0.01);   // 1박 끝 → 릴리즈 애니 등록(다음 프레임부터)
                StepD(r.D, 0.04);   // 릴리즈 30% 부근: z0 + 0.30 → 앞으로 내지른다
                Assert.Greater(h.LeanZ, 0.05, c.Fx + " 릴리즈 스냅(앞으로)");
                StepD(r.D, 0.12);   // 0.14초 지나면 0
                Assert.AreEqual(0, h.LeanZ, 1e-9, c.Fx + " 릴리즈 뒤 제자리");
                Assert.IsTrue(r.D.Timeline.Idle, c.Fx + " 애니 남은 것 0");
            }
            Assert.AreEqual(0, r.D.CubeCount); Assert.AreEqual(0, r.D.Lights.Busy);
        }

        [UnityTest]
        public IEnumerator 구형_fx_참격_회오리_응급처치_안무도_있다()
        {
            yield return Boot();
            Rig r = Make(5);
            yield return Run(r.Scene, 2.0);
            yield return EnsureTarget(r.Scene);
            Manual(r.Scene);
            r.D.Clear();
            List<int> targets = new BattleSceneStage(r.Scene).Targets("aoe");
            CastRecord s1 = r.D.Cast(Def("x1", "slash", "aoe", "mythic", "#cfd8dc"), targets); StepD(r.D, 2.5);
            Assert.IsTrue(s1.Actors.Contains("swordbot")); Assert.AreEqual(3, s1.Hits, "미식 참격 = 검사 로봇 3기");
            CastRecord s2 = r.D.Cast(Def("x2", "ring", "aoe", "common", "#b0bec5"), targets); StepD(r.D, 2.5);
            Assert.IsTrue(s2.Actors.Contains("shuriken")); Assert.AreEqual(10, s2.Hits, "표창 10개");
            CastRecord s3 = r.D.Cast(Def("x3", "firstaid", "heal", "rare", "#8d6e63"), new List<int>()); StepD(r.D, 2.5);
            Assert.IsTrue(s3.Actors.Contains("medic"));
            Assert.AreEqual(0, r.D.Pool.Active); Assert.AreEqual(0, r.D.CubeCount);
        }

        [UnityTest]
        public IEnumerator 큐브_재질은_풀에서_되쓴다_두_번째_시전에_새_재질_0()
        {
            yield return Boot();
            Rig r = Make(11);
            List<int> targets = new BattleSceneStage(r.Scene).Targets("aoe");
            // 첫 시전: 큐브(시전 박자 모트·차지 코어·트레일)가 재질을 풀에서 꺼내고(없으면 만든다) 퇴장하며 돌려준다
            CastRecord s1 = r.D.Cast(Def("p1", "slash", "aoe", "mythic", "#cfd8dc"), targets); StepD(r.D, 2.5);
            r.D.Clear();
            Assert.AreEqual(0, r.D.CubeCount, "큐브 전부 퇴장");
            int made1 = Forge.Game.Battle.FxMaterials.PoolMade;
            int pooled1 = Forge.Game.Battle.FxMaterials.Pooled;
            Assert.Greater(made1, 0, "첫 시전이 재질을 만들었다(큐브를 쓰는 연출)");
            Assert.Greater(pooled1, 0, "퇴장한 큐브의 재질이 풀로 돌아왔다");
            // 둘째 시전: 같은 조합이면 새 재질 0 — 되쓰기가 새면 여기서 는다(런 113·118 «재시전 켬 +135»)
            CastRecord s2 = r.D.Cast(Def("p2", "slash", "aoe", "mythic", "#cfd8dc"), targets); StepD(r.D, 2.5);
            r.D.Clear();
            Assert.AreEqual(made1, Forge.Game.Battle.FxMaterials.PoolMade, "둘째 시전은 풀에서만 꺼낸다 — 새 Material 0");
            Assert.AreEqual(0, r.D.CubeCount);
            Assert.GreaterOrEqual(Forge.Game.Battle.FxMaterials.Pooled, pooled1, "돌려준 재질이 다시 풀에 있다");
        }
    }
}
