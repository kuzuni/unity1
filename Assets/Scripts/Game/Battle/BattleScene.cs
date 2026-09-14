using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Forge.Core;
using Forge.Core.Battle;
using Forge.Core.BattleFx;
using Forge.Core.Data;
using Forge.Core.Save;
using Forge.Game.Audio;
using Forge.Game.Map;
using Forge.Game.Ui;
using Forge.Game.Voxel;

namespace Forge.Game.Battle
{
    /// <summary>
    /// 전투 씬(ROUTINE T8) — Core <see cref="Battle"/>(T7 · 100ms 고정 틱)의 이벤트를 구독해 그린다. **씬은 규칙을 계산하지 않는다.**
    /// 원작 대응: `main.js` 의 논리 틱 루프(LOGIC_TICK 100ms 누적) + `scene3d.js update`(적 동기화·영웅 행군·카메라 추종/셰이크/fovPunch·파티클) + 원작이 `Scene3D.*` 를 직접 부르던 자리(spawnEnemy·enemyAttack·heroAttack·hitEnemy·killEnemy·heroHit·heroDown·heroRevive·clearEnemies·setTheme·wavePips·stageLabel).
    /// 부팅 씬의 <see cref="Bootstrap"/> 아래에 스스로 선다(T9 World·T18 UiRoot 와 같은 길 · <see cref="AutoBoot"/>). 영웅 스탯은 T15 전까지 <see cref="BareHeroStats"/>.
    /// 테스트는 <see cref="ManualStep"/> + <see cref="Step"/> 으로 시간을 직접 민다.
    /// </summary>
    [DefaultExecutionOrder(-700)]
    public sealed class BattleScene : MonoBehaviour
    {
        /// <summary>정본 `monsterMesh` 의 게임 경로 채도 보정.</summary>
        public const double EnemyVivid = 0.16;
        /// <summary>FxCatalog 키(용도) — hit/crit(hitEnemy 플레어) · kill/bossKill(killEnemy 버스트) · bossLand(spawnEnemy 보스 착지 링) · heroHit · revive.</summary>
        public const string FxHit = "hit", FxCrit = "crit", FxKill = "kill", FxBossKill = "bossKill", FxBossLand = "bossLand", FxHeroHit = "heroHit", FxRevive = "revive";
        public const string DataFolder = "data";

        /// <summary>false 면 **바로 다음 씬 로드 한 번** 은 스스로 서지 않는다(읽는 즉시 true 로 돌아온다 · T54)(테스트가 제 전투를 세울 때).</summary>
        public static bool AutoBoot = true;
        public static BattleScene Instance { get; private set; }

        public Forge.Core.Battle.Battle Battle { get; private set; }
        public GameData Data { get; private set; }
        public SaveDefs SaveDefs { get; private set; }
        public bool Ready { get; private set; }
        public bool ManualStep;

        public CubeParticles Fx { get; private set; }
        /// <summary>T39 일회성 연출 시계(원작 addAnim).</summary>
        public FxAnims Anims { get; private set; }
        /// <summary>T39 임팩트 프리미티브(플레어·스파이크·링·점광·그을음·스우시·기둥).</summary>
        public ImpactFx Impact { get; private set; }
        /// <summary>원작 `camPush` — 보스 워닝이 소유하는 카메라 돌리 인(three z 를 이만큼 당긴다).</summary>
        public double CamPush;
        public BossEntrance LastEntrance { get; private set; }
        public DamageNumbers Numbers { get; private set; }
        public HeroView Hero { get; private set; }
        public readonly Dictionary<int, EnemyView> Enemies = new Dictionary<int, EnemyView>();
        /// <summary>정본 `worldX`(영웅 월드 x) · `_clock` · 논리 시각(ms).</summary>
        public double WorldX { get; private set; }
        public double Clock { get; private set; }
        public double NowMs { get; private set; }
        public double ShakeMag { get; private set; }
        public int ThemeIndex { get; private set; } = -1;

        // 통계(테스트·검수용)
        public int SpawnCount, KillCount, HitCount, EventCount;
        public readonly HashSet<string> KindsSpawned = new HashSet<string>();
        public readonly HashSet<string> KindsKilled = new HashSet<string>();
        public string LastStageLabel { get; private set; }
        public int LastWave { get; private set; }
        /// <summary>이벤트를 그린 뒤 바깥 층(T12 스킬 오브젝트 · T21 던전 팝업 …)에 같은 이벤트를 넘긴다 — 이 씬은 skill*/dungeon*/toast 를 안 그린다.</summary>
        public event Action<BattleEvent> EventHandled;
        /// <summary>한 프레임 스텝 끝(파티클·숫자 뒤 · 카메라 전) — 바깥 층의 게임 시간이 이 박자를 따른다.</summary>
        public event Action<float> Stepped;

        Camera cam; Vector3 camBase; float fov0 = -1; double fovT = -1, fovDur, fovAmt;
        double acc;
        Transform stage;
        bool signaled;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void ForgeSignal(string name);
#endif
        /// <summary>T26 템플릿의 신호 계약 `window.forgeSignal('battle')` — 전투 진입 프레임에 한 번(배포 스모크가 기다린다). 웹이 아니면 아무것도 안 한다.</summary>
        static void Signal(string name)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { ForgeSignal(name); } catch (Exception e) { Debug.LogWarning("[BattleScene] forgeSignal 실패: " + e.Message); }
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // T54 — 이 플래그는 «이번 씬 로드 하나» 만 끈다: 읽는 즉시 기본값으로 되돌린다.
            // 그러지 않으면 정적 필드라 PlayMode 런 내내 누워 있어, 눕힌 픽스처가 끝난 뒤에 도는 **남의 픽스처**까지
            // 전투 없는 앱을 부팅한다(CI 런 78 실측: SkillFxTests 가 눕힌 채 UiShotsTests 가 찍어 화면 30장이 전부 영웅·적·펫 0).
            bool auto = AutoBoot;
            AutoBoot = true;
            if (!auto || Instance != null) return;
            foreach (Bootstrap b in Resources.FindObjectsOfTypeAll<Bootstrap>())
            {
                if (!b.gameObject.scene.isLoaded) continue;
                Create(b.transform, true);
                return;
            }
        }

        /// <summary>씬 오브젝트를 세운다. <paramref name="boot"/> 면 데이터를 읽고 기본 전투(맨몸 영웅)를 스스로 시작한다 — 아니면 <see cref="Attach"/> 를 기다린다.</summary>
        public static BattleScene Create(Transform parent, bool boot)
        {
            var go = new GameObject("BattleScene");
            if (parent != null) go.transform.SetParent(parent, false);
            var s = go.AddComponent<BattleScene>();
            if (boot) s.StartCoroutine(s.Boot());
            return s;
        }

        void Awake()
        {
            Instance = this;
            stage = new GameObject("Stage").transform;
            stage.SetParent(transform, false);
            var fx = new GameObject("Particles");
            fx.transform.SetParent(transform, false);
            Fx = fx.AddComponent<CubeParticles>();
            Numbers = new DamageNumbers();
            Anims = new FxAnims();
            Impact = new ImpactFx(stage, Anims, Camera.main);
        }

        void OnDestroy() { if (Instance == this) { Instance = null; HeroStatsGlue.Uninstall(); } }

        static IEnumerator ReadText(string name, Action<string> done)
        {
            string p = Path.Combine(Application.streamingAssetsPath, DataFolder, name);
            if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.WebGLPlayer)
            {
                try { done(File.ReadAllText(p)); } catch (Exception e) { Debug.LogError("[BattleScene] " + p + " 읽기 실패: " + e.Message); done(null); }
                yield break;
            }
            using (var req = UnityWebRequest.Get(p))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) done(req.downloadHandler.text);
                else { Debug.LogError("[BattleScene] " + p + " 읽기 실패: " + req.error); done(null); }
            }
        }

        IEnumerator Boot()
        {
            GameData data = SaveIo.Data;
            SaveDefs defs = SaveIo.Defs;
            if (data == null)
            {
                var texts = new Dictionary<string, string>();
                foreach (var f in GameData.Files) { string name = f; yield return ReadText(name, s => texts[name] = s); }
                foreach (var f in GameData.Files) if (!texts.ContainsKey(f) || texts[f] == null) { Debug.LogError("[BattleScene] " + f + " 를 못 읽었다 — 전투를 세우지 않는다"); yield break; }
                data = GameData.Load(f => texts[f]);
            }
            if (defs == null)
            {
                string stateJson = null;
                yield return ReadText(SaveDefs.FileName, s => stateJson = s);
                if (stateJson == null) yield break;
                defs = SaveDefs.Parse(stateJson);
            }
            // 세계(T9)가 서기를 잠깐 기다린다(없어도 전투는 돈다)
            for (int i = 0; i < 120 && World.Instance != null && !World.Instance.Ready; i++) yield return null;
            // T43 — 영웅 스탯은 세이브 위의 GearSystem.HeroStats(장비+펫+스킬 패시브+기술트리) · 호스트가 아직 없으면 맨몸 → 서는 순간 재계산
            // T86 — 여기서 읽은 data·defs 를 Attach 에 **같이 넘긴다**. 안 넘기면 Attach 가 SaveIo.Data 로 폴백하는데, WebGL·Android 는 SaveIo 가
            //       UnityWebRequest 로 늦게 읽어 그 순간 null 이라 «GameData 가 없다» 로 부팅이 죽는다(런 140 WebGL 스모크 실측 · 에디터는 동기 읽기라 안 보였다).
            Attach(MakeBattle(data, defs, HeroStatsGlue.Make, (uint)(DateTime.UtcNow.Ticks & 0xffffffff)), data, defs);
            HeroStatsGlue.Install(this);
        }

        /// <summary>맨몸 영웅·검 하나의 본대 전투(원작 `Combat.start()` 의 최소 문맥). 반폭 훅은 <see cref="Instance"/> 의 실측.</summary>
        public static Forge.Core.Battle.Battle MakeBattle(GameData data, SaveDefs defs, Func<HeroStats> stats, uint seed)
        {
            var ctx = new BattleContext(data.Defs, defs) { HeroStats = stats, WeaponType = "sword" };
            ctx.EnemyHalfW = id => Instance != null ? Instance.HalfWOf(id) : BattleRules.DefaultEnemyHalfW;
            BattleSaveGlue.Fill(ctx);   // T55 — 세이브의 처치·재화·진행·첫 클리어·무기 종을 싣고 saveGame 훅·던전 모듈을 잇는다(원작 Combat.start 이 S 를 읽는 자리)
            return new Forge.Core.Battle.Battle(ctx, Rng.Mulberry(seed));
        }

        /// <summary>전투를 붙이고 시작한다(<see cref="Battle.Start"/> → 첫 이벤트를 그린다).</summary>
        public void Attach(Forge.Core.Battle.Battle battle, GameData data = null, SaveDefs defs = null)
        {
            Battle = battle;
            Data = data ?? SaveIo.Data ?? Data;
            SaveDefs = defs ?? SaveIo.Defs ?? SaveDefs;
            if (Data == null) throw new InvalidOperationException("BattleScene.Attach: GameData 가 없다(적 표를 세울 수 없다)");
            cam = Camera.main;
            if (cam != null) { camBase = cam.transform.localPosition; fov0 = cam.fieldOfView; }
            Impact.Cam = cam;
            WeaponType wt = null;
            string wid = battle.Context.WeaponType ?? "sword";
            if (Data.Defs != null && Data.Defs.WeaponTypes != null) wt = Data.Defs.WeaponTypes.Get(wid, null);
            Hero = new HeroView(this, stage, wid, wt);
            Ready = true;
            if (battle.TickCount == 0 && battle.Phase == BattlePhase.Idle) battle.Start(NowMs);
            Drain();
            if (!signaled) { signaled = true; Signal("battle"); }
        }

        // ── 프레임 ──
        void Update()
        {
            if (!ManualStep) Step(Time.deltaTime);
        }

        /// <summary>한 프레임(초): 논리 틱 누적(100ms) → 이벤트 그리기 → 적·영웅·파티클·숫자·카메라.</summary>
        public void Step(float dt)
        {
            if (!Ready) return;
            acc += dt;
            while (acc >= BattleRules.Tick - 1e-9)
            {
                NowMs += BattleRules.Tick * 1000;
                Battle.Tick(BattleRules.Tick, NowMs);
                acc -= BattleRules.Tick;
                Drain();
            }
            Clock += dt;
            bool walking = Battle.Walking && !Hero.Attacking && !Hero.Dead;
            if (walking)
            {
                WorldX += EnemyGait.MarchSpeed * dt;
                if (World.Instance != null && World.Instance.Ready) World.Instance.SetWorldX(WorldX);
            }
            double hpRatio = Battle.Hero.MaxHp.IsZero ? 1 : Math.Max(0, Math.Min(1, Battle.Hero.Hp.RatioTo(Battle.Hero.MaxHp)));
            Hero.Step(dt, Battle.Walking, WorldX, hpRatio);
            var gone = new List<int>();
            foreach (var kv in Enemies)
            {
                kv.Value.Step(dt, Clock, WorldX);
                if (kv.Value.Removed) gone.Add(kv.Key);
            }
            foreach (int id in gone) Enemies.Remove(id);
            Fx.Step(dt);
            Numbers.Step(dt);
            if (Stepped != null) Stepped(dt);
            Anims.Step(dt);
            var overlay = BattleOverlay.Instance;
            if (overlay != null) overlay.Tick(dt);
            StepCamera(dt);
        }

        void StepCamera(float dt)
        {
            if (cam == null) return;
            Vector3 p = camBase + new Vector3((float)WorldX, 0, (float)CamPush);
            if (ShakeMag > HitRules.ShakeEps)
            {
                p.x += (UnityEngine.Random.value * 2 - 1) * (float)ShakeMag;
                p.y += (UnityEngine.Random.value * 2 - 1) * (float)(ShakeMag * HitRules.ShakeYK);
                ShakeMag = HitRules.ShakeDecay(ShakeMag, dt);
            }
            else ShakeMag = 0;
            cam.transform.localPosition = p;
            if (fovT >= 0 && fov0 > 0)
            {
                fovT += dt;
                double k = Math.Min(1, fovT / fovDur);
                cam.fieldOfView = (float)HitRules.FovPunch(fov0, fovAmt, k);
                if (k >= 1) { fovT = -1; cam.fieldOfView = fov0; }
            }
        }

        /// <summary>`shake(mag)` — max 합성.</summary>
        public void Shake(double mag) { ShakeMag = Math.Max(ShakeMag, mag); }
        /// <summary>`fovPunch(amount, dur)`.</summary>
        public void FovPunch(double amount, double dur) { fovAmt = amount; fovDur = dur; fovT = 0; }
        public double StopXOf(Enemy e) { return Battle.StopXOf(e); }

        /// <summary>`Scene3D.enemyHalfW(id)` — 대열 편성이 스폰 직후 묻는다. 그 시점엔 이벤트가 아직 안 그려졌으므로 여기서 먼저 세운다(정본 spawnWave 도 spawnEnemy 를 동기로 부른다).</summary>
        public double HalfWOf(int id)
        {
            EnemyView v;
            if (!Enemies.TryGetValue(id, out v))
            {
                if (!Ready) return BattleRules.DefaultEnemyHalfW;
                foreach (var e in Battle.Enemies) if (e.Id == id && e.Alive) { v = SpawnView(e); break; }
                if (v == null) return BattleRules.DefaultEnemyHalfW;
            }
            return v.HalfW;
        }

        EnemyView SpawnView(Enemy e)
        {
            int chapter = Battle.Context.Progress != null ? Battle.Context.Progress.Chapter : 1;
            string kind = EnemyGait.KindOf(e.Id, chapter);
            MobModel model = Data.Enemies.Models.Get(kind, null);
            if (model == null) throw new InvalidOperationException("적 표에 «" + kind + "» 이 없다 (mobs-enemies.json)");
            var v = new EnemyView(this, e, model, chapter, stage);
            Enemies[e.Id] = v;
            SpawnCount++; KindsSpawned.Add(kind);
            return v;
        }

        // ── 이벤트 ──
        void Drain()
        {
            var ev = Battle.Events;
            for (int i = 0; i < ev.Count; i++) Handle(ev[i]);
            ev.Clear();
        }

        void Handle(BattleEvent e)
        {
            EventCount++;
            EnemyView v;
            switch (e.Kind)
            {
                case BattleEventKind.HeroRevive: Hero.Revive(); break;
                case BattleEventKind.ClearEnemies: ClearEnemies(); break;
                case BattleEventKind.DeathWipe: ClearEnemies(); break;
                case BattleEventKind.Theme:
                    if (e.Tag != null && e.Tag.StartsWith("ch:", StringComparison.Ordinal))
                    {
                        int ch; if (int.TryParse(e.Tag.Substring(3), out ch)) SetChapterTheme(ch);
                    }
                    break;
                case BattleEventKind.StageLabel:
                    LastStageLabel = e.Tag;
                    if (Hud.Instance != null) Hud.Instance.SetStage(e.Tag);
                    break;
                case BattleEventKind.WavePips:
                    LastWave = (int)e.Num;
                    if (Hud.Instance != null) { int total = Battle.TotalWaves(); Hud.Instance.SetWaves(total, (int)e.Num, total); }
                    break;
                case BattleEventKind.Spawn:
                    if (!Enemies.ContainsKey(e.Id)) foreach (var en in Battle.Enemies) if (en.Id == e.Id) { SpawnView(en); break; }
                    break;
                case BattleEventKind.EnemyAttack:
                    if (Enemies.TryGetValue(e.Id, out v)) v.Attack();
                    break;
                case BattleEventKind.HeroAttack:
                    Hero.Attack(Enemies.TryGetValue(e.Id, out v) ? (double?)v.E.X + WorldX : null);
                    break;
                case BattleEventKind.Hit:
                    if (Enemies.TryGetValue(e.Id, out v))
                    {
                        HitCount++;
                        double sev = !v.E.MaxHp.IsZero && e.Value.HasValue ? Math.Max(0, Math.Min(1, e.Value.Value.RatioTo(v.E.MaxHp))) : 0.15;
                        v.Hit(sev, e.Flag, e.Tag, e.Num >= 1, e.Value.HasValue ? NumFmt.Fmt(e.Value.Value) : "");
                    }
                    break;
                case BattleEventKind.Kill:
                    if (Enemies.TryGetValue(e.Id, out v)) { v.Kill(); KillCount++; KindsKilled.Add(v.Kind); }
                    break;
                case BattleEventKind.Shake: Shake(e.Num); break;
                // 정본 `scene3d.js` 13351 — 영웅 피격은 몸 플래시·HP바·셰이크와 **같은 자리**에서 화면 비네트를 부른다(T135 ⓐ).
                case BattleEventKind.HeroHit:
                    Hero.Hit(e.Num, e.Value.HasValue ? NumFmt.Fmt(e.Value.Value) : null);
                    { var ovh = BattleOverlay.Ensure(); if (ovh != null) ovh.FlashDamage(e.Num); }
                    break;
                // 정본 13374 `UI.flashDamage(1)` — «치명타 피격보다 진하게»(상한 .64 에 걸린다).
                case BattleEventKind.HeroDown:
                    Hero.Down();
                    { var ovd = BattleOverlay.Ensure(); if (ovd != null) ovd.FlashDamage(1); }
                    break;
                case BattleEventKind.Float:
                    Numbers.Spawn(new Vector3((float)(Hero.X + HitRules.HeroDmgX), (float)(Hero.Y + HitRules.HpBar.HeroY + HitRules.HpBar.BgH * HitRules.HeroDmgYK), 0), e.Tag, e.Tag == "BLOCK" ? "block" : "heal", 0, -HitRules.DmgRiseDefault, 1);
                    break;
                // T138 3회차 — 정본 combat.js 450·454 `UI.floatLoot(…)`: 전리품은 영웅 머리 위 숫자가 아니라 무대 한쪽 레인(#loot-feed · 여섯 줄 상한 · 1.6초)에 쌓인다.
                case BattleEventKind.Loot: LootFeed.Push(e.Tag); break;
                // T138 3회차 — 정본 `UI.toast(msg, lane)`: 레인은 Core 가 이벤트에 실어 보낸다(첫 클리어·난이도 상승 = combat · «사거리 안에 적이 없습니다» 는 기본 레인).
                case BattleEventKind.Toast: if (PopupLayer.Instance != null) PopupLayer.Instance.Toast(e.Tag, e.Lane); break;
                case BattleEventKind.BossEntrance: StartBossEntrance(); break;
                case BattleEventKind.Music: if (Music.Instance != null) Music.Instance.SetMusicMode(e.Tag); break;
                case BattleEventKind.DeathFade: { var ov = BattleOverlay.Ensure(); if (ov != null) ov.DeathFade(e.Tag); break; }
                case BattleEventKind.SceneCut: { var ov = BattleOverlay.Ensure(); if (ov != null) ov.SceneCut(FxRules.SceneCutMs); break; }
                // skill*(T12) · toast(T22/T27) · save · dungeon*(T21) — 이 작업 밖.
                default: break;
            }
            if (EventHandled != null) EventHandled(e);
        }

        /// <summary>`bossEntrance()` — 워닝 배너·사이렌·경고 기둥·박 3회·돌리 인·착지 임팩트(T39 <see cref="BossEntrance"/>).</summary>
        public BossEntrance StartBossEntrance()
        {
            LastEntrance = new BossEntrance(this);
            LastEntrance.Start();
            return LastEntrance;
        }

        void SetChapterTheme(int chapter)
        {
            ThemeIndex = chapter - 1;
            var w = World.Instance;
            if (w != null && w.Ready && w.Themes != null && w.Themes.Count > 0) w.SetTheme(((chapter - 1) % w.Themes.Count + w.Themes.Count) % w.Themes.Count);
        }

        void ClearEnemies()
        {
            foreach (var kv in Enemies) kv.Value.Remove();
            Enemies.Clear();
        }
    }
}
