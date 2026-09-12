using System;
using System.Collections.Generic;
using Forge.Core.Data;
using Forge.Core.Save;

namespace Forge.Core.Battle
{
    /// <summary>
    /// 전투 문맥(<see cref="BattleContext"/>) ↔ 세이브 트리(<see cref="SaveState"/> = 원작 `S`) 의 동기화 규칙(T55). 원작은 전투가 `S.kills++ · S.coins += · S.hammers += · S.clearedBosses[key] = true ·
    /// S.chapter/stage/difficulty · S.best* · S.equipment.weapon.wtype` 를 **직접** 읽고 쓴다(`combat.js` 322·445~505·560~577). 유니티 Core 전투는 세이브를 모르고 문맥의 칸에만 쓰므로, 이 클래스가 그 칸을 세이브와 맞춘다.
    /// · 재화·처치(`Kills`·`Coins`·`Hammers`): 문맥은 **거울**이다 — 지난 동기화 뒤 문맥이 올린 만큼(delta)만 세이브에 더하고, 세이브 값(UI 가 그 사이 쓴 코인까지)을 문맥에 되돌린다. 절대값을 덮어쓰면 대장간 지출이 되살아난다.
    /// · 진행(`Progress` ↔ chapter/stage/difficulty/best*): 지난 동기화 뒤 **문맥이 바뀌었으면 세이브로**(스테이지 클리어·사망 후퇴), 아니면 **세이브를 문맥으로**(리셋·치트·복원).
    /// · 첫 클리어(`ClearedBosses` ↔ `clearedBosses{key:true}`): 합집합.
    /// · 무기 종(`WeaponType` ← `equipment.weapon.wtype || 'sword'`): 늘 세이브가 원본(원작 322행).
    /// 장착 스킬·자동시전은 T20 `SkillBar.Wire` 가 꽂는다(결정 132) · 던전 판은 T23 `Dungeons.Attach` 가 문맥에 직접 쓴다 — 여기서는 «세이브에 run 이 없는데 문맥에 남은 판» 만 걷는다(<see cref="DungeonStale"/>).
    /// UnityEngine 참조 0 — Game `BattleSaveGlue` 가 시점(부팅·프레임 뒤·저장 훅)만 정한다.
    /// </summary>
    public sealed class BattleSaveSync
    {
        /// <summary>원작 `WEAPON_TYPES[(S.equipment.weapon && S.equipment.weapon.wtype) || 'sword']`.</summary>
        public const string DefaultWeaponType = "sword";

        public readonly BattleContext Ctx;
        public readonly SaveState Save;

        // 지난 동기화 시점의 문맥 값 — «문맥이 그 뒤 얼마나 올렸나 / 바뀌었나» 의 기준
        double kills, coins, hammers;
        int difficulty = 0, chapter = 1, stage = 1, bestDifficulty = 0, bestChapter = 1, bestStage = 1;
        bool filled;

        /// <summary>문맥이 아직 «빈 채»(처치 0 · 재화 0 · 진행 1-1)라고 보고 기준을 잡는다 — 세이브가 나중에 서도 그때까지 오른 처치가 delta 로 살아남는다.</summary>
        public BattleSaveSync(BattleContext ctx, SaveState save)
        {
            if (ctx == null) throw new ArgumentNullException("ctx");
            if (save == null) throw new ArgumentNullException("save");
            Ctx = ctx; Save = save;
        }

        /// <summary>한 번이라도 <see cref="Fill"/>/<see cref="Sync"/> 가 돌았는가.</summary>
        public bool Filled { get { return filled; } }

        /// <summary>정본 `Combat.start()` 자리 — 세이브 값을 문맥에 그대로 싣는다(문맥이 올린 것이 있으면 그것도 세이브에 먼저 더한다 = <see cref="Sync"/> 와 같다).</summary>
        public void Fill() { Sync(); }

        /// <summary>
        /// 세이브를 **그대로** 문맥에 싣는다 — delta 없이(문맥에 남은 값은 버린다). 세이브 객체가 통째로 바뀌었을 때(씬 재로드 · 초기화 · 다른 세이브)의 갈아타기용:
        /// 옛 세이브에서 온 문맥 값을 새 세이브에 delta 로 더하면 재화가 두 배가 된다.
        /// </summary>
        public void Load()
        {
            SaveState s = Save; BattleContext c = Ctx;
            c.Kills = s.Kills; c.Coins = s.Coins; c.Hammers = s.Hammers;
            kills = c.Kills; coins = c.Coins; hammers = c.Hammers;
            Progress p = c.Progress;
            if (p != null)
            {
                p.Difficulty = s.Difficulty; p.Chapter = s.Chapter; p.Stage = s.Stage;
                p.BestDifficulty = s.BestDifficulty; p.BestChapter = s.BestChapter; p.BestStage = s.BestStage;
                difficulty = p.Difficulty; chapter = p.Chapter; stage = p.Stage;
                bestDifficulty = p.BestDifficulty; bestChapter = p.BestChapter; bestStage = p.BestStage;
            }
            c.ClearedBosses.Clear();
            JsonObject cleared = s.ClearedBosses;
            if (cleared == null) { cleared = new JsonObject(); s["clearedBosses"] = cleared; }
            for (int i = 0; i < cleared.Keys.Count; i++) { string key = cleared.Keys[i]; if (JsonTree.Truthy(cleared[key])) c.ClearedBosses.Add(key); }
            c.WeaponType = WeaponTypeOf(s);
            filled = true;
        }

        /// <summary>
        /// 양방향 동기화 한 번(프레임 뒤·저장 직전·부팅). 언제 몇 번 불러도 같은 결과(멱등) — 같은 프레임에 두 번 불리면 두 번째는 delta 0 이다.
        /// </summary>
        public void Sync()
        {
            SaveState s = Save; BattleContext c = Ctx;
            // ── 처치·재화: delta 를 세이브에, 세이브 값을 문맥에 ──
            double dk = c.Kills - kills, dc = c.Coins - coins, dh = c.Hammers - hammers;
            if (dk != 0) s.Kills = s.Kills + dk;
            if (dc != 0) s.Coins = s.Coins + dc;
            if (dh != 0) s.Hammers = s.Hammers + dh;
            c.Kills = s.Kills; c.Coins = s.Coins; c.Hammers = s.Hammers;
            kills = c.Kills; coins = c.Coins; hammers = c.Hammers;
            // ── 진행: 문맥이 바뀌었으면 세이브로, 아니면 세이브를 문맥으로 ──
            Progress p = c.Progress;
            if (p != null)
            {
                bool ctxMoved = p.Difficulty != difficulty || p.Chapter != chapter || p.Stage != stage;
                if (ctxMoved) { s.Difficulty = p.Difficulty; s.Chapter = p.Chapter; s.Stage = p.Stage; }
                else { p.Difficulty = s.Difficulty; p.Chapter = s.Chapter; p.Stage = s.Stage; }
                bool bestMoved = p.BestDifficulty != bestDifficulty || p.BestChapter != bestChapter || p.BestStage != bestStage;
                if (bestMoved) { s.BestDifficulty = p.BestDifficulty; s.BestChapter = p.BestChapter; s.BestStage = p.BestStage; }
                else { p.BestDifficulty = s.BestDifficulty; p.BestChapter = s.BestChapter; p.BestStage = s.BestStage; }
                difficulty = p.Difficulty; chapter = p.Chapter; stage = p.Stage;
                bestDifficulty = p.BestDifficulty; bestChapter = p.BestChapter; bestStage = p.BestStage;
            }
            // ── 첫 클리어: 합집합 ──
            JsonObject cleared = s.ClearedBosses;
            if (cleared == null) { cleared = new JsonObject(); s["clearedBosses"] = cleared; }
            foreach (string key in c.ClearedBosses) if (!JsonTree.Truthy(cleared[key])) cleared[key] = true;
            for (int i = 0; i < cleared.Keys.Count; i++) { string key = cleared.Keys[i]; if (JsonTree.Truthy(cleared[key])) c.ClearedBosses.Add(key); }
            // ── 무기 종: 세이브가 원본 ──
            c.WeaponType = WeaponTypeOf(s);
            filled = true;
        }

        /// <summary>원작 322행 — `S.equipment.weapon.wtype` 이 있으면 그것, 없으면 `sword`.</summary>
        public static string WeaponTypeOf(SaveState s)
        {
            JsonObject eq = s != null ? s.Equipment : null;
            JsonObject w = eq != null ? J.Obj(eq["weapon"]) : null;
            string wt = w != null ? J.Str(w["wtype"]) : null;
            return string.IsNullOrEmpty(wt) ? DefaultWeaponType : wt;
        }

        /// <summary>세이브에 `dungeonRun` 이 없는데 문맥에 던전 판이 남아 있는가 — 전투 밖에서 판이 닫힌 경우(원작은 `Dungeons.run` 게터라 즉시 null 이 된다). 참이면 호출부가 `ctx.Dungeon = null` + `Battle.LeaveDungeon()`.</summary>
        public static bool DungeonStale(BattleContext ctx, SaveState s)
        {
            return ctx != null && ctx.Dungeon != null && s != null && !JsonTree.Truthy(s["dungeonRun"]);
        }
    }
}
