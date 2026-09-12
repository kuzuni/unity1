using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Forge.Core.Battle;
using Forge.Core.Data;
using Forge.Game.Battle;
using Forge.Game.Voxel;

namespace Forge.Game.SkillFx
{
    /// <summary>T8 <see cref="BattleScene"/> 을 <see cref="ISkillFxStage"/> 로 — 영웅·적 자리(three) · 파편 · 셰이크 · FOV · 표적.</summary>
    public sealed class BattleSceneStage : ISkillFxStage
    {
        readonly BattleScene s;
        public BattleSceneStage(BattleScene scene) { if (scene == null) throw new ArgumentNullException("scene"); s = scene; }

        public Vector3 HeroPos { get { return s.Hero != null ? new Vector3((float)s.Hero.X, (float)s.Hero.Y, 0) : new Vector3((float)(BattleRules.HeroX + s.WorldX), 0, 0); } }

        public bool TryEnemyPos(int id, out Vector3 threePos)
        {
            EnemyView v;
            if (s.Enemies.TryGetValue(id, out v) && v != null && !v.Dead && !v.Removed && v.G != null)
            {
                Vector3 u = v.G.localPosition;
                threePos = new Vector3(u.x, u.y, -u.z);
                return true;
            }
            threePos = Vector3.zero;
            return false;
        }

        /// <summary>원작 `Combat.tryCast` 의 표적 — `aoe` 는 사거리(x &lt; 3.2) 안 산 적 전부 · 그 밖(단일)은 우선 표적.</summary>
        public List<int> Targets(string type)
        {
            var ids = new List<int>();
            var b = s.Battle;
            if (b == null) return ids;
            if (type == "aoe")
            {
                foreach (Enemy e in b.Enemies) if (e.Alive && e.X < BattleRules.SkillRange) ids.Add(e.Id);
            }
            else
            {
                Enemy t = b.PriorityTarget();
                if (t != null) ids.Add(t.Id);
            }
            return ids;
        }

        public void Sparks(Vector3 p, int count, int hex, double speed, double scale) { if (s.Fx != null) s.Fx.Sparks(p, count, hex, speed, scale); }
        public void Shards(Vector3 p, int count, int hex, double dir, double spread, double speed, double scale) { if (s.Fx != null) s.Fx.Shards(p, count, hex, dir, spread, speed, scale); }
        public void Shake(double mag) { s.Shake(mag); }
        public void FovPunch(double amount, double dur) { s.FovPunch(amount, dur); }
        public int ParticleCount { get { return s.Fx != null ? s.Fx.Count : 0; } }
        public bool HeroLeanBusy { get { return s.Hero == null || s.Hero.LeanBusy; } }
        public double HeroLeanZ { get { return s.Hero != null ? s.Hero.LeanZ : 0; } }
        public void HeroLean(double z) { if (s.Hero != null) s.Hero.SetLean(z); }
    }

    /// <summary>
    /// 스킬 오브젝트 층의 씬 컴포넌트(T12) — 전투 씬(T8)이 뜨면 스스로 붙어 `SkillCutin`/`SkillEffect` 이벤트를 받아 <see cref="SkillFxDirector"/> 를 돌린다.
    /// 시간은 전투 씬의 스텝(<see cref="BattleScene.Stepped"/>)을 그대로 따른다 — 수동 스텝 테스트에서도 같은 박자.
    /// </summary>
    public sealed class SkillFxScene : MonoBehaviour
    {
        public static bool AutoBoot = true;
        public static SkillFxScene Instance { get; private set; }

        public SkillFxDirector Director { get; private set; }
        public BattleScene Scene { get; private set; }
        public bool Ready { get { return Director != null; } }
        public int Handled { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!AutoBoot || Instance != null) return;
            foreach (Bootstrap b in Resources.FindObjectsOfTypeAll<Bootstrap>())
            {
                if (!b.gameObject.scene.isLoaded) continue;
                Create(b.transform, true);
                return;
            }
        }

        /// <summary>컴포넌트를 세운다. <paramref name="boot"/> 면 전투 씬이 준비될 때까지 기다렸다가 스스로 붙는다.</summary>
        public static SkillFxScene Create(Transform parent, bool boot)
        {
            var go = new GameObject("SkillFx");
            if (parent != null) go.transform.SetParent(parent, false);
            var s = go.AddComponent<SkillFxScene>();
            if (boot) s.StartCoroutine(s.Boot());
            return s;
        }

        void Awake() { Instance = this; }
        void OnDestroy()
        {
            // 파괴 중에는 유니티 오브젝트를 만지지 않는다 — 무대·액터·큐브·라이트는 씬과 함께 내려가고, 파괴 순서는 정해져 있지 않다
            // (여기서 그것들의 필드를 건드리면 MissingReferenceException 이 «다음 테스트» 의 부팅 중에 빨간 로그로 터진다).
            Detach(true);
            if (Instance == this) Instance = null;
        }

        IEnumerator Boot()
        {
            for (int i = 0; i < 1800; i++)
            {
                if (BattleScene.Instance != null && BattleScene.Instance.Ready && BattleScene.Instance.Data != null) { Attach(BattleScene.Instance); yield break; }
                yield return null;
            }
        }

        /// <summary>전투 씬에 붙는다 — 이벤트·스텝 훅 구독 · 액터는 전투 씬 밑에 선다.</summary>
        public SkillFxDirector Attach(BattleScene scene, uint seed = 0)
        {
            if (scene == null) throw new ArgumentNullException("scene");
            if (scene.Data == null) throw new InvalidOperationException("SkillFxScene.Attach: BattleScene 에 GameData 가 없다");
            Detach();
            Scene = scene;
            var stageGo = new GameObject("SkillFx Stage");
            stageGo.transform.SetParent(scene.transform, false);
            Director = new SkillFxDirector(scene.Data, new BattleSceneStage(scene), stageGo.transform, Rng.Mulberry(seed != 0 ? seed : (uint)(DateTime.UtcNow.Ticks & 0xffffffff)));
            scene.EventHandled += OnEvent;
            scene.Stepped += OnStep;
            return Director;
        }

        public void Detach() { Detach(false); }

        /// <param name="destroying">true 면 구독만 풀고 오브젝트는 안 만진다(OnDestroy · 씬 언로드).</param>
        void Detach(bool destroying)
        {
            BattleScene s = Scene;
            Scene = null;
            if (!ReferenceEquals(s, null))
            {
                try { s.EventHandled -= OnEvent; s.Stepped -= OnStep; } catch (Exception) { }
            }
            SkillFxDirector d = Director;
            if (destroying) { Director = null; return; }
            if (d != null) d.Clear();
        }

        void OnEvent(BattleEvent e)
        {
            if (Director == null) return;
            if (e.Kind == BattleEventKind.SkillCutin || e.Kind == BattleEventKind.SkillEffect) Handled++;
            Director.Handle(e);
        }

        void OnStep(float dt) { if (Director != null) Director.Step(dt); }
    }
}
