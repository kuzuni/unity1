using System;
using UnityEngine;
using Forge.Core.Battle;
using Forge.Core.Dungeon;
using Forge.Game.Ui;

namespace Forge.Game.Battle
{
    /// <summary>
    /// 전투 ↔ 세이브 접착(T55) — 원작 전투가 `S` 에 직접 쓰던 처치·재화·첫 클리어·진행·무기 종과 `saveGame()`·던전 판을 유니티 전투 문맥에 잇는다. 규칙은 Core <see cref="BattleSaveSync"/> · 여기는 시점만:
    /// ⓐ <see cref="Fill"/> — <c>BattleScene.MakeBattle</c> 이 문맥을 만들자마자(원작 `Combat.start()` 이 `S` 를 읽는 자리) 세이브 값을 싣고 `ctx.Save`(원작 `saveGame()`) 를 꽂는다.
    /// ⓑ 프레임마다 <c>BattleScene.Stepped</c> 뒤에 <see cref="Sync"/> — 전투가 올린 처치·재화를 세이브로, UI 가 쓴 코인을 문맥으로(둘 다 같은 스레드라 프레임 경계에서 맞추면 어긋나지 않는다).
    /// ⓒ 던전(T23 `Dungeons`): 호스트가 서면 <c>Dungeons.Attach(battle)</c>(입장·복원이 문맥의 판과 `SetupStage` 를 민다 · `OnDungeonClear/Fail` 꽂힘) → 부팅 한 번 <c>RestoreRun</c>(원작 main.js 97 · `Combat.start()` 뒤) → 클리어 팝업 [보상 수령] → <c>Battle.FinishDungeonClear</c>(원작 `finishDungeonClear`).
    /// 장착 스킬·자동시전은 T20 `SkillBar.Wire` 가 이미 꽂는다(결정 132) — 여기서 다시 쓰지 않는다. T43 <see cref="HeroStatsGlue"/> 와 같은 꼴(정적 · Install/Uninstall).
    /// </summary>
    public static class BattleSaveGlue
    {
        static BattleSaveSync sync;
        static BattleScene scene;
        static BattleContext ctx;
        static Action<float> onStepped;
        static Action onSaveReady, onDungeonReady;
        static Action<DungeonRewards> onClearConfirmed;
        static Dungeons attachedDungeons;
        static Forge.Core.Save.SaveState restoredFor;

        /// <summary>지금 문맥이 세이브와 이어져 있는가(세이브가 준비된 뒤).</summary>
        public static bool Live { get { return sync != null && SaveIo.Ready && SaveIo.State != null && ReferenceEquals(sync.Save, SaveIo.State); } }
        public static BattleSaveSync Current { get { return sync; } }

        /// <summary>정본 `Combat.start()` 자리 — 문맥에 세이브 값을 싣고 `Save` 훅을 꽂는다. 세이브가 아직 없으면 준비되는 순간(`SaveIo.OnReady`)으로 미룬다(그때까지 오른 처치는 delta 로 산다).</summary>
        public static void Fill(BattleContext c)
        {
            if (c == null) return;
            Unbind();
            ctx = c;
            c.Save = SaveNow;
            if (SaveIo.Ready && SaveIo.State != null) { sync = new BattleSaveSync(c, SaveIo.State); sync.Fill(); }
            // 세이브가 아직 없거나 **나중에 갈아끼워지면**(씬 재로드 뒤 새 SaveIo 가 서면 정적 `State` 가 새 객체가 된다 — 옛 객체에 계속 쓰면 세이브에 아무것도 안 남는다 · CI 런 90 실측) 그때 다시 잇는다
            onSaveReady = () => { if (ctx == c) Rebind(); };
            SaveIo.OnReady += onSaveReady;
            if (BattleScene.Instance != null) Install(BattleScene.Instance);
        }

        /// <summary>전투 씬에 시점을 잇는다(프레임 뒤 동기화 · 던전 호스트 · 클리어 팝업). <see cref="Fill"/> 이 스스로 부른다 — 씬을 바꿔 세우면 다시.</summary>
        public static void Install(BattleScene s)
        {
            UnbindScene();
            scene = s;
            if (s == null) return;
            onStepped = dt => { if (scene != s || s == null) return; if (attachedDungeons == null) AttachDungeons(); Sync(); };   // 던전 모듈은 Attach(전투 붙임) 뒤 첫 프레임에 꽂힌다
            s.Stepped += onStepped;
            onClearConfirmed = r => { BattleScene sc = scene; if (sc != null && sc.Battle != null) sc.Battle.FinishDungeonClear(); };
            DungeonClearPopup.Confirmed += onClearConfirmed;
            onDungeonReady = () => AttachDungeons();
            DungeonUiHost.OnReady += onDungeonReady;
            AttachDungeons();
        }

        /// <summary>T23 던전 모듈을 이 전투에 꽂고(한 번) 부팅 복원(원작 `Dungeons.restoreRun()` · 전투 뒤).</summary>
        static void AttachDungeons()
        {
            BattleScene sc = scene;
            DungeonUiHost h = DungeonUiHost.Ready ? DungeonUiHost.Instance : null;
            if (sc == null || sc.Battle == null || h == null || h.Dungeons == null) return;
            Dungeons d = h.Dungeons;
            if (d.AttachedBattle == sc.Battle && attachedDungeons == d) return;
            d.Attach(sc.Battle);
            attachedDungeons = d;
            if (restoredFor != SaveIo.State)
            {
                restoredFor = SaveIo.State;
                try { d.RestoreRun(); }
                catch (Exception e) { Debug.LogError("[BattleSaveGlue] Dungeons.RestoreRun() 실패 — 나머지 부팅은 계속한다: " + e.Message); }
            }
        }

        /// <summary>지금 세이브 객체와 어긋나 있으면 다시 잇는다 — 처음이면 delta 를 살리고(<see cref="BattleSaveSync.Fill"/>), 세이브가 통째로 바뀌었으면 그대로 싣는다(<see cref="BattleSaveSync.Load"/> · 옛 세이브 값을 새 세이브에 더하면 재화가 두 배).</summary>
        static void Rebind()
        {
            BattleContext c = ctx;
            if (c == null || !SaveIo.Ready || SaveIo.State == null) return;
            if (sync != null && ReferenceEquals(sync.Save, SaveIo.State)) return;
            bool first = sync == null;
            sync = new BattleSaveSync(c, SaveIo.State);
            if (first) sync.Fill(); else sync.Load();
        }

        /// <summary>양방향 동기화 한 번(프레임 뒤 · 저장 직전 · 테스트). 세이브에 던전 판이 없는데 문맥에 남았으면 걷는다.</summary>
        public static void Sync()
        {
            Rebind();
            if (sync == null) return;
            sync.Sync();
            BattleScene sc = scene;
            if (sc != null && sc.Battle != null && BattleSaveSync.DungeonStale(sc.Battle.Context, sync.Save))
            {
                sc.Battle.Context.Dungeon = null;
                sc.Battle.LeaveDungeon();
            }
        }

        /// <summary>정본 `saveGame()` — 전투가 스테이지 클리어·사망 때 부른다: 문맥을 세이브에 맞춘 뒤 파일로.</summary>
        static void SaveNow()
        {
            Sync();
            if (SaveIo.Instance != null) SaveIo.Instance.Save();
        }

        static void UnbindScene()
        {
            if (scene != null && onStepped != null) scene.Stepped -= onStepped;
            if (onClearConfirmed != null) DungeonClearPopup.Confirmed -= onClearConfirmed;
            if (onDungeonReady != null) DungeonUiHost.OnReady -= onDungeonReady;
            scene = null; onStepped = null; onClearConfirmed = null; onDungeonReady = null; attachedDungeons = null;
        }

        static void Unbind()
        {
            UnbindScene();
            if (onSaveReady != null) SaveIo.OnReady -= onSaveReady;
            onSaveReady = null;
            sync = null; ctx = null;
        }

        /// <summary>전부 푼다(테스트·씬 파괴). 부팅 복원 표식도 되돌린다.</summary>
        public static void Uninstall()
        {
            Unbind();
            restoredFor = null;
        }
    }
}
