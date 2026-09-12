using System;
using System.Globalization;
using Forge.Core.Battle;
using Forge.Core.Data;
using Forge.Core.Meta;
using Forge.Core.Save;

namespace Forge.Core.Dungeon
{
    /// <summary>
    /// 원작 `web/js/dungeons.js` 의 `Dungeons` 를 **함수 이름·순서 그대로** 옮긴 것(T23) — 열쇠 2/2 매일 09:00 리셋 · 완료 시 소모 · 최고 클리어 단계 소탕.
    /// 상태는 원작처럼 세이브 루트(`S.dungeons {keys, best, lastReset}` · `S.dungeonRun {id, stage, waves}` · 재화 칸)에 산다 — 이 클래스는 필드를 두지 않는다.
    /// 바깥은 <see cref="IDungeonHost"/>(값·저장·퀘스트) 와 <see cref="Emitted"/>(화면 호출) 로 받고, 전투는 <see cref="Attach"/> 로 T7 <see cref="Battle.Battle"/> 에 꽂는다
    /// (입장·복원 → `ctx.Dungeon` 세팅 + `SetupStage` · 클리어·실패 → Battle 이 `ctx.Dungeon` 을 비운 뒤 <see cref="OnClear"/>/<see cref="OnFail"/> 를 부른다).
    /// 대조: `tools/dungeon_vectors.js`(정본을 node vm 에 올림) → `Assets/Tests/EditMode/Vectors/t23-dungeons.json` ↔ `DungeonTests`.
    /// </summary>
    public sealed class Dungeons
    {
        readonly SaveState S;
        readonly IDungeonHost H;
        readonly Rng _rng;
        readonly TimeZoneInfo _tz;
        Battle.Battle _battle;

        /// <summary>`UI.*`·`Combat.setupStage` 호출 — 이름은 원작 함수 그대로(<see cref="DungeonEventKind"/>).</summary>
        public event Action<DungeonEvent> Emitted;

        /// <param name="s">세이브 루트(T13) — 원작 `S`.</param>
        /// <param name="host">시각·최고 기록·기술트리 배율·퀘스트·저장.</param>
        /// <param name="rng">`Math.random`(웨이브 수 뽑기) — 원작 시뮬레이터와 같은 mulberry32 를 주면 같은 판.</param>
        /// <param name="tz">리셋 날짜 키의 달력(원작은 브라우저 로컬) — null 이면 기기 로컬. 테스트는 UTC 를 준다.</param>
        public Dungeons(SaveState s, IDungeonHost host, Rng rng, TimeZoneInfo tz = null)
        {
            if (s == null) throw new ArgumentNullException("s");
            if (host == null) throw new ArgumentNullException("host");
            if (rng == null) throw new ArgumentNullException("rng");
            S = s; H = host; _rng = rng; _tz = tz ?? TimeZoneInfo.Local;
        }

        /// <summary>T7 전투에 꽂는다: `ctx.OnDungeonClear/OnDungeonFail` ← 이 모듈 · 입장·복원이 `ctx.Dungeon` 과 `SetupStage` 를 직접 민다.</summary>
        public void Attach(Battle.Battle battle)
        {
            _battle = battle;
            if (battle == null) return;
            battle.Context.OnDungeonClear = OnClear;
            battle.Context.OnDungeonFail = OnFail;
        }
        public Battle.Battle AttachedBattle { get { return _battle; } }

        // ── 진행 중 던전(원작 `get run()` = `S.dungeonRun || null`) ──

        /// <summary>`S.dungeonRun` 원값(손상 검사는 <see cref="RestoreRun"/> 몫).</summary>
        public object RunRaw { get { return S["dungeonRun"]; } }
        /// <summary>`this.run` 이 참인가(원작 `if (this.run)`) — JS 참/거짓.</summary>
        public bool InRun { get { return JsonTree.Truthy(S["dungeonRun"]); } }
        /// <summary>`this.run` 을 객체로(객체가 아니면 null).</summary>
        public JsonObject Run
        {
            get { return J.Obj(S["dungeonRun"]); }
            set { S["dungeonRun"] = value; }
        }
        static JsonObject MakeRun(string id, int stage, int waves)
        {
            var r = new JsonObject();
            r["id"] = id; r["stage"] = (double)stage; r["waves"] = (double)waves;
            return r;
        }
        /// <summary>진행 중 던전을 T7 전투 입력으로(`Combat.monsterBaseHp/totalWaves/setupStage` 가 읽던 것) — 없으면 null.</summary>
        public DungeonRun BattleRun()
        {
            JsonObject r = Run;
            if (r == null) return null;
            string id = J.Str(r["id"]); int stage = (int)Num(r["stage"]); int waves = (int)Num(r["waves"]);
            return new DungeonRun { Id = id, Stage = stage, Waves = waves, MonsterHp = MonsterHp(id, stage), Theme = "dungeon_" + id };
        }

        // ── 표 · 순수 계산 ──

        /// <summary>`Dungeons.def(id)`.</summary>
        public DungeonDef Def(string id) { return DungeonDefs.Find(id); }
        DungeonDef DefOrThrow(string id)
        {
            DungeonDef d = DungeonDefs.Find(id);
            if (d == null) throw new ArgumentException("알 수 없는 던전 id: " + id, "id"); // 원작은 undefined.unlock 에서 TypeError
            return d;
        }

        /// <summary>`rollWaves()` = `MIN + floor(random · (MAX − MIN + 1))` — 식 그대로(난수 소비 1회).</summary>
        public int RollWaves()
        {
            return DungeonRules.MinWaves + (int)Math.Floor(_rng.Random() * (DungeonRules.MaxWaves - DungeonRules.MinWaves + 1));
        }

        /// <summary>`resetDateKey()` = `new Date(Date.now() − 9h).toDateString()` — 09:00 이전은 전날(T25 `DailyReset` 과 같은 키).</summary>
        public string ResetDateKey() { return ResetDateKey(H.Now(), _tz); }
        public static string ResetDateKey(double nowMs, TimeZoneInfo tz)
        {
            DateTimeOffset t = DateTimeOffset.FromUnixTimeMilliseconds((long)Math.Floor(nowMs));
            DateTime local = TimeZoneInfo.ConvertTime(t, tz ?? TimeZoneInfo.Local).DateTime;
            return DailyReset.ResetDateKey(local);
        }

        /// <summary>`unlocked(id)` — `bestRank() >= c·100 + s`(해금 좌표는 티어 0 의 값이라 c 를 절대 챕터로 본다).</summary>
        public bool Unlocked(string id)
        {
            DungeonDef d = DefOrThrow(id);
            return H.BestRank() >= d.UnlockChapter * 100 + d.UnlockStage;
        }

        /// <summary>`monsterHp(id, stage)` = 55 · 5.6^(해금 챕터−1) · 1.35^(단계−1).</summary>
        public double MonsterHp(string id, int stage)
        {
            int unlockCh = DefOrThrow(id).UnlockChapter;
            return DungeonRules.MonsterHpBase * Math.Pow(DungeonRules.MonsterHpPerChapter, unlockCh - 1) * Math.Pow(DungeonRules.MonsterHpPerStage, stage - 1);
        }

        /// <summary>`rewardAmount(stage)` = 100 + 2·(max(1, stage) − 1).</summary>
        public double RewardAmount(int stage)
        {
            return DungeonRules.BaseReward + DungeonRules.PerStage * (Math.Max(1, stage) - 1);
        }

        /// <summary>`rewards(id, stage)` — 망치 도둑 = 망치+코인 · 유령 마을 = 티켓 · 침략 = 알 화폐(배율 없음) · 좀비 = 물약. 배율은 기술트리(T24).</summary>
        public DungeonRewards Rewards(string id, int stage)
        {
            double n = RewardAmount(stage);
            var r = new DungeonRewards();
            if (id == "hammer") { r.Hammers = Math.Ceiling(n * H.ThiefHammerMult()); r.Coins = Math.Ceiling(n * H.ThiefCoinMult()); }
            else if (id == "ghost") r.Tickets = Math.Ceiling(n * H.DungeonTicketMult());
            else if (id == "invasion") r.EggCurrency = n;
            else r.Potions = Math.Ceiling(n * H.DungeonPotionMult());
            return r;
        }

        /// <summary>`rewardText(id, stage, sep = ' · ')` — 상세 팝업 pill 은 sep=' '(가운뎃점 없이 공백만).</summary>
        public string RewardText(string id, int stage, string sep = " · ")
        {
            DungeonRewards r = Rewards(id, stage);
            if (r.Hammers != 0) return "🔨 " + NumFmt.Fmt(r.Hammers) + sep + "🪙 " + NumFmt.Fmt(r.Coins);
            if (r.Tickets != 0) return "🎫 " + NumFmt.Fmt(r.Tickets);
            if (r.EggCurrency != 0) return "🥚 " + NumFmt.Fmt(r.EggCurrency);
            return "🧪 " + NumFmt.Fmt(r.Potions);
        }

        /// <summary>`grantRewards(id, stage)` — 세이브 재화 칸에 더한다(알 화폐는 `(S.eggCurrency || 0) + n`).</summary>
        public DungeonRewards GrantRewards(string id, int stage)
        {
            DungeonRewards r = Rewards(id, stage);
            if (r.Hammers != 0) { S.Hammers = S.Hammers + r.Hammers; S.Coins = S.Coins + r.Coins; }
            if (r.Tickets != 0) S.Tickets = S.Tickets + r.Tickets;
            if (r.Potions != 0) S.Potions = S.Potions + r.Potions;
            if (r.EggCurrency != 0) S.EggCurrency = (JsonTree.Truthy(S["eggCurrency"]) ? S.EggCurrency : 0) + r.EggCurrency;
            return r;
        }

        // ── 세이브 슬롯 ──

        /// <summary>`S.dungeons`(없으면 null).</summary>
        public JsonObject Slot { get { return J.Obj(S["dungeons"]); } }
        /// <summary>`S.dungeons.keys[id]` — JS 수 변환(없으면 NaN).</summary>
        public double Keys(string id) { JsonObject k = J.Obj(Slot["keys"]); return k == null ? double.NaN : Num(k, id); }
        /// <summary>`S.dungeons.best[id]`.</summary>
        public double Best(string id) { JsonObject b = J.Obj(Slot["best"]); return b == null ? double.NaN : Num(b, id); }

        /// <summary>원작 `bad = v => !v || typeof v !== 'object' || Array.isArray(v)`.</summary>
        static bool Bad(object v) { return !JsonTree.Truthy(v) || JsonTree.Kind(v) != "object"; }

        /// <summary>`ensure()` — 저장 슬롯 보정(하위 필드까지) + 매일 09:00 열쇠 리셋 + 없는 던전 칸 채우기. 부팅·입장·소탕·리셋 감지 때 부른다.</summary>
        public void Ensure()
        {
            if (Bad(S["dungeons"]))
            {
                var d = new JsonObject();
                d["keys"] = new JsonObject(); d["best"] = new JsonObject(); d["lastReset"] = "";
                S["dungeons"] = d;
            }
            JsonObject slot = Slot;
            if (Bad(slot["keys"])) slot["keys"] = new JsonObject();
            if (Bad(slot["best"])) slot["best"] = new JsonObject();
            if (!(slot["lastReset"] is string)) slot["lastReset"] = "";
            if (!S.Has("potions")) S.Potions = 0;
            string today = ResetDateKey();
            JsonObject keys = J.Obj(slot["keys"]), best = J.Obj(slot["best"]);
            if ((string)slot["lastReset"] != today)
            {
                slot["lastReset"] = today;
                foreach (DungeonDef d in DungeonDefs.All) keys[d.Id] = (double)DungeonRules.MaxKeys;
                Emit(DungeonEventKind.OpenDungeons);
                Emit(DungeonEventKind.RenderDungeonDetail);
            }
            foreach (DungeonDef d in DungeonDefs.All)
            {
                if (!keys.Has(d.Id)) keys[d.Id] = (double)DungeonRules.MaxKeys;
                if (!best.Has(d.Id)) best[d.Id] = 0.0;
            }
        }

        // ── 흐름 ──

        /// <summary>`enter(id, stage)` — 열쇠는 입장이 아니라 완료 시점에 소모(실패해도 같은 열쇠로 재도전). `stage` 0 = `best + 1`.</summary>
        public bool Enter(string id, int stage = 0)
        {
            Ensure();
            if (InRun) { Toast("⚔️ 이미 던전에 진행 중입니다"); return false; }
            DungeonDef def = DefOrThrow(id);
            if (!Unlocked(id)) { Toast("🔒 스테이지 " + def.Unlock + " 도달 시 해금"); return false; }
            if (Keys(id) <= 0) { Toast("🗝 열쇠가 없습니다 (매일 09:00 리셋)"); return false; }
            double best = Best(id);
            double want = stage != 0 ? stage : best + 1;
            int st = (int)JsonTree.Clamp(want, 1, best + 1);
            Run = MakeRun(id, st, RollWaves());
            Toast(def.Icon + " " + def.Kr + " " + st + "단계 입장!");
            H.Save();
            SetupStage();
            return true;
        }

        /// <summary>`restoreRun()` — 부팅 복원: 세이브에 남은 진행을 그 단계 1웨이브부터. 손상·구버전은 안내 한 줄을 띄운 뒤 본대로.</summary>
        public bool RestoreRun()
        {
            Ensure();
            object raw = S["dungeonRun"];
            JsonObject r = J.Obj(raw);
            if (!JsonTree.Truthy(raw) || r == null) { S["dungeonRun"] = null; return false; }
            string id = J.Str(r["id"]);
            DungeonDef def = id == null ? null : DungeonDefs.Find(id);
            JsonObject bestMap = J.Obj(Slot["best"]);
            double bestRaw = bestMap != null ? (bestMap.Has(id ?? "") ? Num(bestMap, id) : double.NaN) : double.NaN;
            double best = JsonTree.Truthy(bestMap != null && id != null ? bestMap[id] : null) ? bestRaw : 0;
            double stageD = Math.Floor(Num(r["stage"]));
            bool stageOk = !double.IsNaN(stageD) && !double.IsInfinity(stageD);
            if (def == null || !stageOk || stageD < 1 || stageD > best + 1)
            {
                S["dungeonRun"] = null;
                H.Save();
                Toast("🚪 진행 중이던 던전에서 나와 본대로 복귀했습니다");
                return false;
            }
            int stage = (int)stageD;
            object w = r["waves"];
            int waves = (w is double && !double.IsNaN((double)w) && !double.IsInfinity((double)w))
                ? (int)JsonTree.Clamp(Math.Floor((double)w), DungeonRules.MinWaves, DungeonRules.MaxWaves)
                : DungeonRules.DefaultWaves;
            S["dungeonRun"] = MakeRun(id, stage, waves);
            H.Save();
            SetupStage();
            Toast(def.Icon + " " + def.Kr + " " + stage + "단계를 이어서 진행합니다");
            return true;
        }

        /// <summary>`sweep(id)` — 최고 클리어 단계 보상 즉시 수령(열쇠 1 소모 · 팝업 없이 연출만).</summary>
        public bool Sweep(string id)
        {
            Ensure();
            if (InRun) { Toast("⚔️ 이미 던전에 진행 중입니다"); return false; }
            DungeonDef def = DefOrThrow(id);
            if (Best(id) < 1) { Toast("먼저 1단계를 클리어해야 소탕할 수 있습니다"); return false; }
            if (Keys(id) <= 0) { Toast("🗝 열쇠가 없습니다 (매일 09:00 리셋)"); return false; }
            JsonObject keys = J.Obj(Slot["keys"]);
            keys[id] = Num(keys, id) - 1;
            H.QuestBump("keySpend");
            int st = (int)Best(id);
            DungeonRewards r = GrantRewards(id, st);
            Emit(DungeonEventKind.RewardBurst, rewards: r);
            Toast("⚡ " + def.Kr + " " + st + "단계 소탕 — " + RewardText(id, st));
            H.Save();
            Emit(DungeonEventKind.RenderTopBar);
            if (r.EggCurrency != 0) Emit(DungeonEventKind.RenderPets);
            return true;
        }

        /// <summary>`onClear()` — 전투가 부른다(T7 은 `ctx.Dungeon` 을 먼저 비운다 · 여기는 `S.dungeonRun` 을 읽고 비운다). 열쇠 소모 · 최고 단계 · 보상 지급 · 팝업.</summary>
        public void OnClear()
        {
            JsonObject run = Run;
            if (run == null) throw new InvalidOperationException("진행 중인 던전이 없다(원작은 null 구조 분해에서 TypeError)");
            string id = J.Str(run["id"]); int stage = (int)Num(run["stage"]);
            Run = null;
            JsonObject keys = J.Obj(Slot["keys"]), best = J.Obj(Slot["best"]);
            keys[id] = Math.Max(0, Num(keys, id) - 1);
            H.QuestBump("dungeonClear");
            H.QuestBump("keySpend");
            best[id] = Math.Max(Num(best, id), stage);
            DungeonRewards r = GrantRewards(id, stage);
            H.Save();
            Emit(DungeonEventKind.RenderTopBar);
            if (r.EggCurrency != 0) Emit(DungeonEventKind.RenderPets);
            Emit(DungeonEventKind.ShowDungeonClear, id: id, stage: stage, rewards: r);
        }

        /// <summary>`onFail()` — 상태와 안내만(화면 복귀는 전투 `LeaveDungeon` 이 같은 프레임에).</summary>
        public void OnFail()
        {
            JsonObject run = Run;
            if (run == null) throw new InvalidOperationException("진행 중인 던전이 없다(원작은 null.id 에서 TypeError)");
            DungeonDef d = DefOrThrow(J.Str(run["id"]));
            Run = null;
            Toast("💀 " + d.Kr + " 실패... 본대로 복귀합니다");
            H.Save();
        }

        // ── 도우미 ──

        void SetupStage()
        {
            if (_battle != null)
            {
                _battle.Context.Dungeon = BattleRun();
                _battle.Hero.Hp = _battle.Hero.MaxHp;
                _battle.SetupStage();
            }
            Emit(DungeonEventKind.SetupStage);
        }

        void Toast(string text) { Emit(DungeonEventKind.Toast, text); }
        void Emit(DungeonEventKind kind, string text = null, string id = null, int stage = 0, DungeonRewards rewards = null)
        {
            var h = Emitted;
            if (h != null) h(new DungeonEvent { Kind = kind, Text = text, Id = id, Stage = stage, Rewards = rewards });
        }

        /// <summary>JS `Number(v)`: null → 0 · bool → 0/1 · 수 → 그대로 · 문자열 → 10진 파싱(빈 문자열 0 · 못 읽으면 NaN) · 객체·배열 → NaN.</summary>
        public static double Num(object v)
        {
            if (v == null) return 0;
            if (v is double) return (double)v;
            if (v is bool) return (bool)v ? 1 : 0;
            string s = v as string;
            if (s != null)
            {
                s = s.Trim();
                if (s.Length == 0) return 0;
                double d;
                return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d) ? d : double.NaN;
            }
            return double.NaN;
        }
        /// <summary>`Number(obj[key])` — 없는 키는 undefined → NaN.</summary>
        static double Num(JsonObject o, string key) { return o.Has(key) ? Num(o[key]) : double.NaN; }
    }
}
