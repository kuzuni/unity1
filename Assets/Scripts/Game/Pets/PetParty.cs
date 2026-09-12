using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Forge.Core.Battle;
using Forge.Core.Data;
using Forge.Core.Pets;
using Forge.Core.World;
using Forge.Game.Battle;
using Forge.Game.Map;

namespace Forge.Game.Pets
{
    /// <summary>출전 한 칸 — 원작 `S.pets[S.activePets[i]]` 의 `{name, rarity, stars}`.</summary>
    public sealed class PetSlot
    {
        public string Name, Rarity;
        public int Stars;
        public PetSlot(string name, string rarity, int stars = 0) { Name = name; Rarity = rarity; Stars = stars; }
    }

    /// <summary>
    /// 출전 펫 무리(T10) — 원작 `Scene3D.refreshPets()`(9762) + `update` 의 펫 블록(18598). 부팅 씬의 <see cref="Bootstrap"/> 아래에 스스로 선다(T8 <see cref="BattleScene"/>·T9 <see cref="World"/> 와 같은 길).
    /// 전투 씬(<see cref="BattleScene.Instance"/>)의 시계·worldX·걷기를 읽어 따라오고, 세이브(<see cref="SaveIo.State"/> 의 `activePets`)가 준비되면 그 출전 칸을 세운다 — 원작이 `init()` 끝과 출전 변경 때 `refreshPets` 를 부르듯 <see cref="Refresh"/>/<see cref="Set"/> 이 다시 세운다.
    /// 자리는 Core <see cref="PetFormation"/>(정본 `formationSpot`) · 자세는 <see cref="PetPose"/>. 탑승 여부(<see cref="Mounted"/>)는 T11 이 켠다 — 자리가 mounted 표로 바뀐다.
    /// 스탯 기여(원작 `Pets.activeBonus()` → `heroStats`)는 Core `PetSystem.ActiveBonus`(T16)·`GearSystem.HeroStats`(T15)에 이미 있고 전투 문맥에 꽂는 접착은 T40.
    /// </summary>
    [DefaultExecutionOrder(-600)]
    public sealed class PetParty : MonoBehaviour
    {
        /// <summary>false 면 **바로 다음 씬 로드 한 번** 은 스스로 서지 않는다(읽는 즉시 true 로 돌아온다 · T54).</summary>
        public static bool AutoBoot = true;
        public static PetParty Instance { get; private set; }

        public bool Ready { get; private set; }
        public SceneDefs Defs { get; private set; }
        public readonly List<PetView> Pets = new List<PetView>();
        /// <summary>정본 `Mounts.ridden()` — 탈것을 타고 있으면 앞 3자리·격자가 mounted 표로 간다(T11 이 켠다).</summary>
        public bool Mounted { get; private set; }
        /// <summary>개체별 위상·속도(정본 `U.rand`) 의 난수원 — 테스트가 시드를 고정한다.</summary>
        public Rng Rng = Rng.Mulberry((uint)(DateTime.UtcNow.Ticks & 0xffffffff));
        public double HeroX = BattleRules.HeroX;

        List<PetSlot> slots = new List<PetSlot>();
        Transform root;

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
                Create(b.transform);
                return;
            }
        }

        public static PetParty Create(Transform parent)
        {
            var go = new GameObject("PetParty");
            if (parent != null) go.transform.SetParent(parent, false);
            return go.AddComponent<PetParty>();
        }

        void Awake()
        {
            Instance = this;
            root = new GameObject("Pets").transform;
            root.SetParent(transform, false);
            StartCoroutine(Boot());
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        static IEnumerator ReadText(string name, Action<string> done)
        {
            string p = Path.Combine(Application.streamingAssetsPath, BattleScene.DataFolder, name);
            if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.WebGLPlayer)
            {
                try { done(File.ReadAllText(p)); } catch (Exception e) { Debug.LogError("[PetParty] " + p + " 읽기 실패: " + e.Message); done(null); }
                yield break;
            }
            using (var req = UnityWebRequest.Get(p))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) done(req.downloadHandler.text);
                else { Debug.LogError("[PetParty] " + p + " 읽기 실패: " + req.error); done(null); }
            }
        }

        IEnumerator Boot()
        {
            // 씬 상수표(대열·CREATURE_YAW) — 세계(T9)가 이미 읽었으면 그것, 아니면 직접
            float t0 = Time.realtimeSinceStartup;
            while (World.Instance != null && !World.Instance.Ready && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            if (World.Instance != null && World.Instance.Ready) Defs = World.Instance.Defs;
            else
            {
                string json = null;
                yield return ReadText(SceneDefs.File, s => json = s);
                if (json == null) yield break;
                Defs = SceneDefs.Parse(json);
            }
            // 전투 씬(시계·worldX·표) 을 기다린다
            t0 = Time.realtimeSinceStartup;
            while ((BattleScene.Instance == null || !BattleScene.Instance.Ready) && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            if (BattleScene.Instance == null || !BattleScene.Instance.Ready) { Debug.LogWarning("[PetParty] 전투 씬이 20초 안에 서지 않았다 — 펫을 세우지 않는다"); yield break; }
            // 세이브(T13) 가 있으면 그 출전 칸(원작 init 끝의 refreshPets)
            if (SaveIo.Instance != null)
            {
                t0 = Time.realtimeSinceStartup;
                while (!SaveIo.Ready && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            }
            Ready = true;
            if (SaveIo.Ready && SaveIo.State != null) Set(SlotsOf(SaveIo.State.Pets, SaveIo.State.ActivePets));
            else Refresh();
        }

        /// <summary>원작 `S.activePets.forEach(pi => S.pets[pi])` — 없는 인덱스는 건너뛴다(`if (!p) return`).</summary>
        public static List<PetSlot> SlotsOf(List<object> pets, List<object> activePets)
        {
            var list = new List<PetSlot>();
            if (pets == null || activePets == null) return list;
            foreach (object a in activePets)
            {
                int pi = J.Int(a, -1);
                if (pi < 0 || pi >= pets.Count) continue;
                var p = J.Obj(pets[pi]);
                if (p == null) continue;
                list.Add(new PetSlot(J.Str(p["name"]), J.Str(p["rarity"]), J.Int(p["stars"])));
            }
            return list;
        }

        /// <summary>출전 칸을 바꾸고 다시 세운다(원작 출전 변경 → `refreshPets`).</summary>
        public void Set(List<PetSlot> newSlots)
        {
            slots = newSlots ?? new List<PetSlot>();
            Refresh();
        }

        /// <summary>탑승 여부(T11) — 바뀌면 자리를 다시 잡는다(원작 `refreshMount` 끝의 `refreshPets`).</summary>
        public void SetMounted(bool mounted)
        {
            if (Mounted == mounted) return;
            Mounted = mounted;
            Refresh();
        }

        /// <summary>원작 `refreshPets()` — 전부 지우고 출전 칸마다 `makePetMesh` → 등급 스케일 → CREATURE_YAW → `formationSpot` 자리 → 위상·속도.</summary>
        public void Refresh()
        {
            foreach (var p in Pets) p.Destroy();
            Pets.Clear();
            if (!Ready || Defs == null) return;
            var s = BattleScene.Instance;
            if (s == null || !s.Ready || s.Data == null) return;
            GameData data = s.Data;
            for (int i = 0; i < slots.Count; i++)
            {
                PetSlot sl = slots[i];
                MobModel model = sl.Name != null ? data.Pets.Models.Get(sl.Name, null) : null;
                if (model == null) { Debug.LogWarning("[PetParty] 펫 표에 «" + sl.Name + "» 이 없다 — 건너뛴다(정본은 회색 상자)"); continue; }
                double[] spot = PetFormation.PetSpot(Defs, i, Mounted);
                double phase = Rng.Rand(0, PetSceneRules.PhaseMax);
                double speed = Rng.Rand(PetSceneRules.SpeedMin, PetSceneRules.SpeedMax);
                var v = new PetView(root, i, sl.Name, sl.Rarity, sl.Stars, model, data.Defs, data.Defs.Rarities, spot, phase, speed, Defs.CreatureYaw);
                v.Step(s.Clock, s.WorldX, false, HeroX, Defs.CreatureYaw);
                Pets.Add(v);
            }
        }

        void Update()
        {
            var s = BattleScene.Instance;
            if (!Ready || s == null || !s.Ready || s.ManualStep) return;
            Step(s.Clock, s.WorldX, s.Battle.Walking && !s.Hero.Attacking && !s.Hero.Dead);
        }

        /// <summary>한 프레임(원작 update 펫 블록) — 테스트·ManualStep 전투가 직접 민다.</summary>
        public void Step(double clock, double worldX, bool walking)
        {
            for (int i = 0; i < Pets.Count; i++) Pets[i].Step(clock, worldX, walking, HeroX, Defs.CreatureYaw);
        }
    }
}
