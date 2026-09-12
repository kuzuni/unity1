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
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T39 — 전투 씬 후속 연출: 적 몸 셰이더(림·플래시·디졸브)가 실제로 컴파일돼 붙고, 피격/처치에 플레어·스파이크·링·점광·그을음·궤적이 뜨고 걷히며, 보스 등장 워닝(배너·기둥·박·돌리 인·착지)과
    /// 사망 암전·씬컷 오버레이가 시계대로 켜졌다 꺼지고, 보스 레갈리아가 몸 정점에서 앉는가 · 콘솔 빨강 0. 빨간 로그가 나면 러너가 실패시킨다.
    /// </summary>
    public class BattleFxSceneTests
    {
        static GameData _data; static SaveDefs _defs;
        static GameData Data { get { return _data ?? (_data = GameData.LoadDirectory(System.IO.Path.Combine(Application.streamingAssetsPath, "data"))); } }
        static SaveDefs Defs { get { return _defs ?? (_defs = SaveDefs.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(Application.streamingAssetsPath, "data", SaveDefs.FileName)))); } }

        static HeroStats StrongHero() { return new HeroStats { Atk = Big.Of(5000), Hp = Big.Of(100000), CritCh = 30, CritDmg = 100, AttacksPerSec = 1.1 }; }

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

        static IEnumerator Run(BattleScene s, double seconds, int stepsPerFrame = 6, Action<double> each = null)
        {
            double t = 0;
            while (t < seconds)
            {
                for (int i = 0; i < stepsPerFrame && t < seconds; i++) { s.Step(0.05f); t += 0.05; if (each != null) each(t); }
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator 적_몸_셰이더가_붙고_피격_처치에_플래시_임팩트_디졸브가_돈다()
        {
            yield return Boot();
            Assert.IsTrue(EnemyBodyFx.Available, "Forge/EnemyBody 셰이더가 컴파일돼 있어야 한다(Assets/Shaders/Dissolve.shader)");
            Shader fx = Shader.Find(FxUnlitMaterials.ShaderName);
            Assert.IsNotNull(fx, "Forge/FxUnlit");
            Assert.IsTrue(fx.isSupported);
            var s = Make(StrongHero, 2024);
            var first = new List<EnemyView>(s.Enemies.Values)[0];
            Assert.IsNotNull(first.Body);
            Assert.Greater(first.Body.LitCount, 0, "불투명 파츠가 EnemyBody 재질로 갈렸다");
            int bodyMats = 0;
            foreach (var r in first.Rig.Renderers) if (r != null && r.sharedMaterial != null && r.sharedMaterial.shader.name == EnemyBodyFx.ShaderName) bodyMats++;
            Assert.AreEqual(first.Body.LitCount, bodyMats, "렌더러의 sharedMaterial 이 개체 재질");
            Assert.AreEqual(0, first.Body.Dissolve, 1e-9, "살아 있는 동안 디졸브 0");

            double maxFlash = 0, maxDsv = 0; int maxLive = 0, maxAnims = 0; bool trailSeen = false;
            int guard = 0;
            while (s.HitCount == 0 && guard++ < 400)
            {
                s.Step(0.05f);
                if (s.Hero.Trail.Points > 0) trailSeen = true;
                if (guard % 6 == 0) yield return null;
            }
            Assert.Greater(s.HitCount, 0, "첫 타격");
            Assert.IsTrue(trailSeen || s.Hero.Trail.Points > 0, "스윙 동안 무기 궤적 리본이 기록됐다");
            Assert.Greater(s.Impact.Live, 0, "접점 플레어·스파이크·링이 떠 있다");
            Assert.Greater(s.Anims.Count, 0, "일회성 연출 시계가 돌고 있다");
            Assert.Greater(first.Body.FlashNow, 0, "몸 플래시(emissive) 가 켜졌다");
            yield return Run(s, 1.3, 6, t =>
            {
                if (first.Body != null && !first.Removed)
                {
                    maxFlash = Math.Max(maxFlash, first.Body.FlashNow);
                    maxDsv = Math.Max(maxDsv, first.Body.Dissolve);
                }
                maxLive = Math.Max(maxLive, s.Impact.Live);
                maxAnims = Math.Max(maxAnims, s.Anims.Count);
            });
            Assert.IsTrue(first.Removed, "시체 걷힘");
            Assert.Greater(maxDsv, 0.9, "쓰러진 뒤 홀드 구간을 지나며 디졸브가 끝까지 갔다(setDissolve f→1)");
            Assert.Greater(maxLive, 3, "플레어 3층 + 스파이크 + 링 + 충격 링 + 그을음이 겹쳐 떠 있었다");
            Assert.Greater(maxAnims, 3);
            Assert.AreEqual(0, first.Body.FlashNow, 1e-9, "플래시는 수명 뒤 0 으로 복귀");
            yield return Run(s, 3, 6);
            Assert.Less(s.Impact.Live, 40, "임팩트 오브젝트는 수명 뒤 걷힌다(누수 없음)");
            UnityEngine.Object.Destroy(s.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 보스_등장_워닝은_배너_기둥_박_3회_돌리_인_착지_뒤_전부_걷힌다()
        {
            yield return Boot();
            var s = Make(StrongHero, 77);
            var e = s.StartBossEntrance();
            var overlay = BattleOverlay.Instance;
            Assert.IsNotNull(overlay, "UiRoot 아래 battle-overlay");
            Assert.IsTrue(overlay.WarningActive, "워닝 배너 켜짐");
            Assert.IsNotNull(e.Pillar, "경고 기둥");
            Assert.AreEqual(0, e.Beat, "첫 박은 즉시");
            yield return Run(s, 1.0, 6);
            Assert.Greater(s.CamPush, 0.3, "임팩트 전 카메라 돌리 인");
            Assert.GreaterOrEqual(e.Beat, 2, "0.42초 박 3회 중 셋째");
            Assert.IsFalse(e.Impacted);
            Vector3 cam = Camera.main.transform.localPosition;
            yield return Run(s, 0.6, 6);
            Assert.IsTrue(e.Impacted, "1.55초 착지 임팩트");
            Assert.Greater(s.ShakeMag, 0, "착지 흔들림");
            yield return Run(s, 0.6, 6);
            Assert.IsTrue(e.Done, "2.0초에 연출 종료");
            Assert.AreEqual(0, s.CamPush, 1e-9, "카메라 릴리즈");
            Assert.IsNull(e.Pillar, "기둥 걷힘");
            Assert.IsFalse(overlay.WarningActive, "배너 닫힘");
            UnityEngine.Object.Destroy(s.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 보스_레갈리아는_몸_정점에서_앉고_HP바_높이를_올린다()
        {
            yield return Boot();
            var s = Make(StrongHero, 5);
            int made = 0, seen = 0;
            foreach (var v in s.Enemies.Values)
            {
                int kc; if (!EnemyGait.KindColor.TryGetValue(v.Kind, out kc)) kc = 0xffffff;
                double top = BossLook.Apply(v.G, v.Rig.Meshes, v.Body, kc, v.TopY, true, v.Cell, null, out made);
                Assert.GreaterOrEqual(made, 8, v.Kind + ": 관 링 + 가시 6 + 보석 이상");
                Assert.GreaterOrEqual(top, v.TopY, v.Kind + ": 관이 자란 만큼 topY");
                Transform crown = v.G.Find("regalia crownG");
                Assert.IsNotNull(crown, v.Kind + ": 관 그룹");
                Assert.AreEqual(8, crown.childCount, v.Kind + ": 링 1 + 가시 6 + 보석 1");
                foreach (Transform c in crown) Assert.AreEqual(EnemyBodyFx.ShaderName, c.GetComponent<MeshRenderer>().sharedMaterial.shader.name, "레갈리아도 림·디졸브 재질");
                seen++;
            }
            Assert.GreaterOrEqual(seen, 1);
            Assert.AreEqual(FxRules.BossTintHex(), FxRules.BossTintHex());
            yield return Run(s, 0.5, 6);
            UnityEngine.Object.Destroy(s.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 사망_암전과_씬컷_오버레이는_시계대로_켜졌다_꺼지고_영웅_블롭이_선다()
        {
            yield return Boot();
            var s = Make(StrongHero, 9);
            var overlay = BattleOverlay.Ensure();
            Assert.IsNotNull(overlay);
            Assert.IsTrue(overlay.DeathFade("회복 후 다시 도전합니다"));
            Assert.AreEqual("회복 후 다시 도전합니다", overlay.DeathSub);
            yield return Run(s, 0.5, 6);
            Assert.AreEqual(0, overlay.CoverAlpha, 1e-6, "0~900ms 투명(사망 클립을 읽는 구간)");
            yield return Run(s, 1.6, 6);
            Assert.AreEqual(1, overlay.CoverAlpha, 1e-3, "1600~3400ms 완전 암전");
            yield return Run(s, 2.2, 6);
            Assert.IsFalse(overlay.DeathActive, "4100ms 에 걷힘");
            overlay.SceneCut(FxRules.SceneCutMs);
            Assert.IsTrue(overlay.CutActive);
            yield return Run(s, 0.6, 6);
            Assert.IsFalse(overlay.CutActive, "420ms 하드컷 커버가 걷힌다");
            Assert.IsNotNull(s.Hero.Blobs);
            Assert.AreEqual(FxRules.HeroBlobOpacity, s.Hero.Blobs.BaseOpacity, 1e-3, "발밑 접지 블롭 0.17");
            Assert.IsFalse(s.Hero.Blobs.CorpseOn);
            UnityEngine.Object.Destroy(s.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 삼십초_자동_전투_연출_포함_콘솔_빨강_0()
        {
            yield return Boot();
            var s = Make(StrongHero, 31337);
            yield return Run(s, 30);
            Assert.AreEqual(7, s.KindsKilled.Count, "7종 전부 사망: " + string.Join(",", s.KindsKilled));
            Assert.Greater(s.HitCount, 7);
            Assert.Less(s.Impact.Live, 60, "임팩트 오브젝트 누수 없음");
            Assert.Less(s.Anims.Count, 60, "연출 시계 누수 없음");
            UnityEngine.Object.Destroy(s.gameObject);
            yield return null;
        }
    }
}
