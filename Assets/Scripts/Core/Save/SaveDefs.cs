using System;
using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Save
{
    /// <summary>
    /// `data/state.json`(T2 추출기가 원작 state.js 에서 뽑는다) — 세이브 키 · 오프라인 수급률·캡 · 난이도 티어 이름 ·
    /// 사이클/챕터 길이 · 형태 보정 키 목록 · `DEFAULT_STATE`(원작 `defaultState()` 평가값 · 시각 칸 0) ·
    /// `MODULE_CAPS`(state.js 가 다른 모듈에서 읽는 상한). 어느 수치도 코드 상수로 두지 않는다(ROUTINE §1).
    /// </summary>
    public sealed class SaveDefs
    {
        public const string FileName = "state.json";

        public string SaveKey;
        public double OfflineCapSec, OfflineCoinPerSec, OfflineHammerPerMin;
        public int ChaptersPerCycle, MaxDifficulty, StagesPerChapter;
        public string[] DifficultyNames;
        /// <summary>고정 형태 레코드 — 중첩 키까지 재귀로 메꾼다(원작 `STATE_SHAPE_KEYS`).</summary>
        public string[] ShapeKeys;
        /// <summary>0 이 될 수 없는 진행 수치 — 손상값은 1 로(원작 `STATE_MIN_ONE_KEYS`).</summary>
        public string[] MinOneKeys;
        /// <summary>`Pets.MAX_ACTIVE` · `Skills.MAX_ACTIVE` · `Mounts.MAX_ACTIVE_MOUNTS` · `Forge.MAX_LEVEL` — 키가 원작 식이다.</summary>
        public int PetMaxActive, SkillMaxActive, MountMaxActive, ForgeMaxLevel;
        /// <summary>원작 `defaultState()` 의 평가값 — 시각 칸(createdAt·lastSeen·lastOfflineClaim)은 0. 직접 고치지 말고 <see cref="DefaultState"/> 로 복제해 쓴다.</summary>
        public JsonObject DefaultTemplate;
        public JsonObject Raw;

        public static SaveDefs Parse(string json) { return From(MiniJson.ParseObject(json)); }

        public static SaveDefs From(JsonObject o)
        {
            var d = new SaveDefs { Raw = o };
            d.SaveKey = J.Str(J.Require(o, "SAVE_KEY"));
            d.OfflineCapSec = J.Num(J.Require(o, "OFFLINE_CAP_SEC"));
            d.OfflineCoinPerSec = J.Num(J.Require(o, "OFFLINE_COIN_PER_SEC"));
            d.OfflineHammerPerMin = J.Num(J.Require(o, "OFFLINE_HAMMER_PER_MIN"));
            d.ChaptersPerCycle = J.Int(J.Require(o, "CHAPTERS_PER_CYCLE"));
            d.DifficultyNames = J.StrArr(J.Require(o, "DIFFICULTY_NAMES"));
            d.MaxDifficulty = J.Int(J.Require(o, "MAX_DIFFICULTY"));
            d.StagesPerChapter = J.Int(J.Require(o, "STAGES_PER_CHAPTER"));
            d.ShapeKeys = J.StrArr(J.Require(o, "STATE_SHAPE_KEYS"));
            d.MinOneKeys = J.StrArr(J.Require(o, "STATE_MIN_ONE_KEYS"));
            d.DefaultTemplate = J.Obj(J.Require(o, "DEFAULT_STATE"));
            var caps = J.Obj(J.Require(o, "MODULE_CAPS"));
            d.PetMaxActive = J.Int(J.Require(caps, "Pets.MAX_ACTIVE"));
            d.SkillMaxActive = J.Int(J.Require(caps, "Skills.MAX_ACTIVE"));
            d.MountMaxActive = J.Int(J.Require(caps, "Mounts.MAX_ACTIVE_MOUNTS"));
            d.ForgeMaxLevel = J.Int(J.Require(caps, "Forge.MAX_LEVEL"));
            return d;
        }

        /// <summary>원작 `defaultState()` — 새 트리(깊은 복제) · 시각 칸 셋 = <paramref name="nowMs"/>(원작 `U.now()` = `Date.now()`).</summary>
        public JsonObject DefaultState(double nowMs)
        {
            var s = (JsonObject)JsonTree.Clone(DefaultTemplate);
            s["createdAt"] = nowMs;
            s["lastSeen"] = nowMs;
            s["lastOfflineClaim"] = nowMs;
            return s;
        }

        /// <summary>원작 `stageDifficultyLabel(chapter, tier)` — 티어 &gt; 0 이면 티어 이름, 티어 0 은 챕터에서 뽑는다(원본 실측 근거 · state.js).</summary>
        public string StageDifficultyLabel(int chapter, int tier)
        {
            if (tier > 0) return DifficultyNames[(int)JsonTree.Clamp(tier, 1, MaxDifficulty)];
            if (chapter <= 1) return "쉬움";
            if (chapter <= 2) return "보통";
            if (chapter <= 8) return "어려움";
            return "매우 어려움";
        }

        /// <summary>원작 `absChapter(tier, chapter)` — 티어를 포함한 절대 챕터(티어 1 챕터 1 = 26).</summary>
        public int AbsChapter(int tier, int chapter) { return (int)JsonTree.Clamp(tier, 0, MaxDifficulty) * ChaptersPerCycle + chapter; }

        /// <summary>원작 `progressRank(tier, chapter, stage)` — 진행도 비교용 단조 랭크.</summary>
        public int ProgressRank(int tier, int chapter, int stage) { return AbsChapter(tier, chapter) * 100 + stage; }
    }
}
