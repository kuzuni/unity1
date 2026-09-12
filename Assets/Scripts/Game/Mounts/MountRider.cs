using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Forge.Core.Battle;
using Forge.Core.BattleFx;
using Forge.Core.Data;
using Forge.Core.Hero;
using Forge.Core.Mounts;
using Forge.Core.Pets;
using Forge.Core.World;
using Forge.Game.Battle;
using Forge.Game.Hero;
using Forge.Game.Map;
using Forge.Game.Pets;
using Forge.Game.Voxel;

namespace Forge.Game.Mounts
{
    /// <summary>
    /// 탑승(T11) — 원작 `Scene3D.refreshMount()`(10795) · `refreshMountFollowers()`(10968) · `update` 의 탈것 블록(18669~18730) · 사망 구간 영웅 높이(18733). 부팅 씬의 <see cref="Bootstrap"/> 아래에 스스로 선다(T10 <see cref="PetParty"/> 와 같은 길).
    /// 세이브(<see cref="SaveIo.State"/> 의 `activeMounts[0]` = 탄 개체 · 나머지 = 뒤따르는 무리)가 준비되면 세운다 — 원작이 `init()` 끝과 장착 변경(`Mounts.setRidden`) 때 `refreshMount` 를 부르듯 <see cref="RefreshFromSave"/>/<see cref="Set"/> 이 다시 세운다(T20 탈것 화면이 부른다).
    /// 정합(안장 역산·서서 타기·접지 보정)은 Core <see cref="MountRideRules"/> · 파츠는 <see cref="MountView.Animate"/>. 영웅 쪽 접착(높이·기울기·하체 자세·HP 바)은 <see cref="ApplyHero"/> 가 <c>HeroView.Step</c> 뒤(LateUpdate)에 얹는다 — T8 파일은 안 만진다.
    /// 타면 <see cref="PetParty.SetMounted"/> 로 펫 자리가 탑승 표로 바뀐다(원작 refreshMount 끝의 refreshPets).
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class MountRider : MonoBehaviour
    {
        /// <summary>false 면 부팅 씬에서 스스로 서지 않는다.</summary>
        public static bool AutoBoot = true;
        public static MountRider Instance { get; private set; }

        public bool Ready { get; private set; }
        public SceneDefs Defs { get; private set; }
        public MountRideDefs RideDefs { get; private set; }
        /// <summary>정본 `mountGroup` — 영웅이 올라탄 탈것(장착 목록의 맨 앞 1마리). 없으면 null.</summary>
        public MountView Ridden { get; private set; }
        /// <summary>정본 `mountFollowers` — 장착은 했지만 타지 않은 나머지(뒤쪽 호).</summary>
        public readonly List<MountView> Followers = new List<MountView>();
        public bool Riding { get { return Ridden != null; } }
        /// <summary>정본 `rideY` — 영웅이 올라앉는 높이(접지 보정·발판 재측정 포함).</summary>
        public double RideY { get; private set; }
        /// <summary>정본 `ridePose` — 탑승 중 하체 자세(서서 타기면 RIDE_STAND_POSE · 평판형은 계열 pose).</summary>
        public PoseAdd RidePose { get; private set; }
        public RideFit Fit { get; private set; }
        /// <summary>이 프레임 영웅에게 얹은 바운스·피치(정본 `heroG.position.y += bob` · `heroG.rotation.x = mg.rotation.x * 0.6`).</summary>
        public double HeroBob { get; private set; }
        public double HeroPitch { get; private set; }
        /// <summary>정본 `this.ridePhase` — 검증이 위상을 고정하는 훅(게임에서는 null = 난수).</summary>
        public double? RidePhase;
        /// <summary>위상·속도 난수원(정본 `U.rand`) — 테스트가 시드를 고정한다(결정 85 와 같은 길).</summary>
        public Rng Rng = Rng.Mulberry((uint)(DateTime.UtcNow.Ticks & 0xffffffff));
        public double HeroX = BattleRules.HeroX;
        /// <summary>정본 `_pelvisLast` — 마지막으로 가드를 통과한 골반 높이(0 = 아직 없음).</summary>
        public double PelvisLast;

        MountSlot riddenSlot;
        bool heroRotApplied; Quaternion heroRotBase, heroRotApplied1;
        List<MountSlot> followerSlots = new List<MountSlot>();
        Transform root;

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
                Create(b.transform);
                return;
            }
        }

        public static MountRider Create(Transform parent)
        {
            var go = new GameObject("MountRider");
            if (parent != null) go.transform.SetParent(parent, false);
            return go.AddComponent<MountRider>();
        }

        void Awake()
        {
            Instance = this;
            root = new GameObject("Mounts").transform;
            root.SetParent(transform, false);
            StartCoroutine(Boot());
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        static IEnumerator ReadText(string name, Action<string> done)
        {
            string p = Path.Combine(Application.streamingAssetsPath, BattleScene.DataFolder, name);
            if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.WebGLPlayer)
            {
                try { done(File.ReadAllText(p)); } catch (Exception e) { Debug.LogError("[MountRider] " + p + " 읽기 실패: " + e.Message); done(null); }
                yield break;
            }
            using (var req = UnityWebRequest.Get(p))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) done(req.downloadHandler.text);
                else { Debug.LogError("[MountRider] " + p + " 읽기 실패: " + req.error); done(null); }
            }
        }

        IEnumerator Boot()
        {
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
            RideDefs = MountRideDefs.From(Defs.Raw);
            t0 = Time.realtimeSinceStartup;
            while ((BattleScene.Instance == null || !BattleScene.Instance.Ready) && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            if (BattleScene.Instance == null || !BattleScene.Instance.Ready) { Debug.LogWarning("[MountRider] 전투 씬이 20초 안에 서지 않았다 — 탈것을 세우지 않는다"); yield break; }
            if (SaveIo.Instance != null)
            {
                t0 = Time.realtimeSinceStartup;
                while (!SaveIo.Ready && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            }
            Ready = true;
            if (SaveIo.Ready && SaveIo.State != null) RefreshFromSave();
            else Refresh();
        }

        /// <summary>원작 `Mounts.riddenInst()` = `inst(S.activeMounts[0])`(없는 인덱스면 null) · 무리 = `S.activeMounts.slice(1)` 을 `inst` 로 풀되 없는 것은 건너뛴다.</summary>
        public static void SlotsOf(List<object> mounts, List<object> activeMounts, out MountSlot ridden, out List<MountSlot> followers)
        {
            ridden = null;
            followers = new List<MountSlot>();
            if (mounts == null || activeMounts == null) return;
            for (int i = 0; i < activeMounts.Count; i++)
            {
                MountSlot s = SlotAt(mounts, J.Int(activeMounts[i], -1));
                if (i == 0) ridden = s;
                else if (s != null) followers.Add(s);
            }
        }

        static MountSlot SlotAt(List<object> mounts, int idx)
        {
            if (idx < 0 || idx >= mounts.Count) return null;
            var m = J.Obj(mounts[idx]);
            if (m == null) return null;
            return new MountSlot(J.Str(m["name"]), J.Str(m["rarity"]), J.Int(m["stars"]));
        }

        /// <summary>세이브의 장착 칸으로 다시 세운다(원작 `Mounts.setRidden` → `Scene3D.refreshMount` · T20 이 부른다).</summary>
        public void RefreshFromSave()
        {
            MountSlot r; List<MountSlot> f;
            if (SaveIo.State == null) { Set(null, null); return; }
            SlotsOf(SaveIo.State.Mounts, SaveIo.State.ActiveMounts, out r, out f);
            Set(r, f);
        }

        /// <summary>장착 칸을 바꾸고 다시 세운다 — <paramref name="ridden"/> 이 null 이면 해제(지면 복귀).</summary>
        public void Set(MountSlot ridden, List<MountSlot> followers)
        {
            riddenSlot = ridden;
            followerSlots = followers ?? new List<MountSlot>();
            Refresh();
        }

        /// <summary>원작 `refreshMount()` — 탄 탈것을 지우고 무리를 다시 세운 뒤, 탄 개체가 있으면 정합(안장 역산 → 서서 타기 → 접지 보정 → 평판 발 재측정)과 영웅 자세를 건다.</summary>
        public void Refresh()
        {
            if (Ridden != null) { Ridden.Destroy(); Ridden = null; }
            RideY = 0; RidePose = null; Fit = default(RideFit); HeroBob = 0; HeroPitch = 0;
            RefreshFollowers();
            var s = BattleScene.Instance;
            if (!Ready || Defs == null || s == null || !s.Ready || s.Data == null || s.Hero == null) return;
            HeroView hero = s.Hero;
            MobModel model = riddenSlot != null && riddenSlot.Name != null ? s.Data.Mounts.Models.Get(riddenSlot.Name, null) : null;
            if (riddenSlot != null && model == null) Debug.LogWarning("[MountRider] 탈것 표에 «" + riddenSlot.Name + "» 이 없다 — 타지 않는다(정본은 회색 상자)");
            if (model == null)
            {
                // 해제: 지면 복귀 + 탑승 포즈·기울기 제거(정본 `if (!m)` 갈래 · rideSkirt(false) 는 유니티 리그에 스커트가 없어 해당 없음)
                EquipHero(hero, null, null);
                ApplyHero(hero, 0, 0);
                if (PetParty.Instance != null) PetParty.Instance.SetMounted(false);
                return;
            }
            MountForm form = RideDefs.FormOfName(riddenSlot.Name);
            double sc = MountRideRules.Scale(s.Data.Defs.Rarities, riddenSlot.Rarity);
            var view = new MountView(root, "Mount " + riddenSlot.Name, 0, riddenSlot, model, form, sc, Defs.CreatureYaw);
            double pelvisLocal = MountRideRules.PelvisLocal(MeasurePelvis(hero), ref PelvisLast);
            RideFit fit = MountRideRules.Fit(RideDefs, form, sc, pelvisLocal, MountRideRules.SeatDropFallback);
            view.SetScale(sc * fit.Wide, sc * fit.RideScale, sc * fit.RideScale);
            double baseY = fit.BaseY, heroY = fit.HeroY;
            view.SetPosition(hero.X, baseY, 0);
            // 접지 보정: 배율은 메시 원점 기준이라 원점 아래 파츠(바퀴 밑동·발굽)가 지면을 뚫는다 — 실제 bbox 하단을 재서 모자란 만큼 들어 올린다(영웅도 같이).
            double lift = MountRideRules.Lift(view.MinY() - root.position.y);
            if (lift > 0) { baseY += lift; heroY += lift; view.SetPosition(hero.X, baseY, 0); }
            view.BaseY = baseY;
            view.SpotX = 0;
            view.Phase = RidePhase.HasValue ? RidePhase.Value : Rng.Rand(0, MountRideRules.PhaseMax);
            Ridden = view;
            Fit = fit;
            RidePose = MountRideRules.RidePose(RideDefs, form);
            // 서서 타면 발이 등자에 없고 손이 고삐를 안 잡는다 — 마구 자체를 안 세우므로 reach 는 null(규칙은 Core `Reach` 에)
            FreeHandReach reach = MountRideRules.Reach(form, fit.Standing, false, false);
            EquipHero(hero, RidePose, reach);
            RideY = heroY;
            ApplyHero(hero, 0, 0);   // 즉시 스냅(공격 중엔 update 의 y 갱신이 막혀 이전 높이가 남는다)
            if (form.Stand)
            {
                // 평판형만: 포즈를 먹인 뒤 실제 발 높이를 재서 발판 위에 다시 세운다(상수를 믿지 말고 결과를 재서 되돌린다)
                hero.Rig.Step(1.0 / 60);
                ApplyHero(hero, 0, 0);
                double footY = double.PositiveInfinity;
                foreach (string side in new[] { "L", "R" })
                {
                    Transform knee = hero.Rig.Bone("knee" + side);
                    if (knee != null) footY = Math.Min(footY, knee.TransformPoint(ThreeSpace.Pos(MountRideRules.FootInKnee)).y - root.position.y);
                }
                double deckTop = MountRideRules.DeckTop(form, sc, fit.RideScale, baseY);
                double snap = MountRideRules.DeckSnap(deckTop, footY);
                if (snap != 0) { RideY += snap; ApplyHero(hero, 0, 0); }
            }
            if (PetParty.Instance != null) PetParty.Instance.SetMounted(true);
        }

        /// <summary>원작 `refreshMountFollowers()` — 원래 크기 그대로 영웅 뒤쪽 호(`MOUNT_ARC`)에 · 개체별 위상·속도.</summary>
        void RefreshFollowers()
        {
            foreach (var f in Followers) f.Destroy();
            Followers.Clear();
            var s = BattleScene.Instance;
            if (!Ready || Defs == null || s == null || !s.Ready || s.Data == null) return;
            for (int i = 0; i < followerSlots.Count; i++)
            {
                MountSlot sl = followerSlots[i];
                MobModel model = sl.Name != null ? s.Data.Mounts.Models.Get(sl.Name, null) : null;
                if (model == null) { Debug.LogWarning("[MountRider] 탈것 표에 «" + sl.Name + "» 이 없다 — 무리에서 건너뛴다(정본은 회색 상자)"); continue; }
                MountForm form = RideDefs.FormOfName(sl.Name);
                double sc = MountRideRules.Scale(s.Data.Defs.Rarities, sl.Rarity);
                var v = new MountView(root, "Follower " + i + " " + sl.Name, i, sl, model, form, sc, Defs.CreatureYaw);
                double[] spot = PetFormation.Spot(i, Defs.MountArc, null);
                v.SpotX = spot[0]; v.SpotZ = spot[1];
                v.BaseY = form.Hover * sc;
                v.Phase = Rng.Rand(0, MountRideRules.PhaseMax);
                v.Speed = Rng.Rand(MountRideRules.FollowerSpeedMin, MountRideRules.FollowerSpeedMax);
                v.SetPosition(HeroX + spot[0] + s.WorldX, v.BaseY, spot[1]);
                Followers.Add(v);
            }
        }

        /// <summary>정본 `heroPelvisLocalY` 의 실측 — 골반 본의 월드 y 에서 영웅 그룹 원점의 y 를 뺀다(영웅이 어디에 떠 있든 같은 값).</summary>
        static double MeasurePelvis(HeroView hero)
        {
            Transform pelvis = hero.Rig != null ? hero.Rig.Bone("pelvis") : null;
            if (pelvis == null) return double.NaN;
            return pelvis.position.y - hero.Rig.transform.position.y;
        }

        /// <summary>정본 `applyWeaponGrip()` — 무기 거치 자세와 탑승 포즈를 합성한다. 지금 든 무기 메시(T15 `GearMeshes`)는 그대로 둔다.</summary>
        static void EquipHero(HeroView hero, PoseAdd ridePose, FreeHandReach reach)
        {
            HeroRig rig = hero.Rig;
            if (rig == null) return;
            Mesh weapon = null;
            if (rig.WeaponMount != null && rig.WeaponMount.childCount > 0)
            {
                var mf = rig.WeaponMount.GetChild(0).GetComponent<MeshFilter>();
                if (mf != null) weapon = mf.sharedMesh;
            }
            rig.Equip(rig.WtypeId, rig.WeaponDef, weapon, ridePose, reach);
        }

        /// <summary>
        /// 영웅 접착(정본 update 탈것 블록의 `heroG.position.y += bob` · `heroG.rotation.x = mg.rotation.x * 0.6` + 사망·기상 구간 `heroGroundY`) — <c>HeroView.Step</c> 이 y=0·기울기 없이 놓은 위에 얹는다.
        /// 사망 중엔 안장에서 내려와 지면(0)에 눕고 기상 중 부드럽게 되돌아온다 · HP 바도 같은 높이를 따른다.
        /// </summary>
        void ApplyHero(HeroView hero, double bob, double pitch)
        {
            Transform t = hero.Rig != null ? hero.Rig.transform : null;
            if (t == null) return;
            bool grounded = hero.Dead || hero.ReviveT > 0;
            double y = grounded ? MountRideRules.DeathHeroY(RideY, hero.Dead, hero.ReviveT, BattleRules.DeathRiseMs / 1000) : RideY + bob;
            Vector3 p = t.localPosition;
            p.y = (float)(hero.Y + y);
            t.localPosition = p;
            // 기울기는 HeroView 가 놓은 회전(yaw·roll) 위에 곱한다 — 같은 프레임에 두 번 불려도 겹치지 않게 지난번 얹은 값이면 먼저 벗긴다
            if (heroRotApplied && t.localRotation == heroRotApplied1) t.localRotation = heroRotBase;
            heroRotBase = t.localRotation;
            if (!grounded && pitch != 0) t.localRotation = Quaternion.AngleAxis(-(float)(pitch * Mathf.Rad2Deg), Vector3.right) * t.localRotation;
            heroRotApplied1 = t.localRotation; heroRotApplied = true;
            HeroBob = grounded ? 0 : bob; HeroPitch = grounded ? 0 : pitch;
            if (hero.Bar != null) hero.Bar.SetPosition(ThreeSpace.Pos(hero.X, hero.Y + y + HitRules.HpBar.HeroY, 0));
        }

        void LateUpdate()
        {
            var s = BattleScene.Instance;
            if (!Ready || s == null || !s.Ready || s.ManualStep) return;
            Step(Time.deltaTime, s.Clock, s.Battle.Walking && !s.Hero.Attacking && !s.Hero.Dead);
        }

        /// <summary>한 프레임(원작 update 탈것 블록 + 무리 블록) — 전투 씬의 <c>Step</c>(영웅 갱신) **뒤에** 부른다. 테스트·ManualStep 전투가 직접 민다.</summary>
        public void Step(double dt, double clock, bool walking)
        {
            var s = BattleScene.Instance;
            if (s == null || !s.Ready || s.Hero == null) return;
            HeroView hero = s.Hero;
            if (Ridden != null)
            {
                MountView mg = Ridden;
                double t = MountRideRules.Time(clock, 1, mg.Phase);
                bool flying = MountRideRules.Flying(mg.Form);
                double bob = MountRideRules.Bob(flying, t, walking);
                // 공격 돌진 중에도 따라붙는다 — 별도 자리가 아니라 정확히 영웅 발밑
                mg.SetPosition(hero.X, mg.BaseY + bob, 0);
                double partPitch = mg.Animate(RideDefs, t, walking, dt);
                if (!hero.Dead)
                {
                    double lean = MountRideRules.Lean(t, walking, partPitch);
                    mg.SetPitch(MountRideRules.Smooth(mg.Rx, lean, dt));
                    ApplyHero(hero, hero.Attacking ? 0 : bob, MountRideRules.HeroPitch(mg.Rx));
                }
                else
                {
                    // 쓰러진 영웅은 탑승 중이 아니다 — 피치를 그룹에 바로 주고 영웅 y 는 사망 구간 규칙이 단독으로 쥔다
                    mg.SetPitch(MountRideRules.Smooth(mg.Rx, partPitch, dt));
                    ApplyHero(hero, 0, 0);
                }
            }
            for (int i = 0; i < Followers.Count; i++)
            {
                MountView fg = Followers[i];
                double t = MountRideRules.Time(clock, fg.Speed, fg.Phase);
                bool fly = MountRideRules.Flying(fg.Form);
                fg.SetPosition(HeroX + fg.SpotX + s.WorldX, fg.BaseY + MountRideRules.FollowerBob(fly, t, walking), fg.SpotZ);
                double fPitch = fg.Animate(RideDefs, t, walking, dt);
                fg.SetPitch(MountRideRules.Smooth(fg.Rx, fPitch, dt));
            }
        }
    }
}
