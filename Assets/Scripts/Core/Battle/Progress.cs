using System;
using Forge.Core.Save;

namespace Forge.Core.Battle
{
    /// <summary>
    /// 원작 `state.js` 의 진행 축 — 난이도 티어(0 기본 · 1 어려움 · 2 매우 어려움 · 3 헬) × 챕터(1~사이클) × 스테이지(1~챕터 길이)
    /// 와 그 도우미(`absChapter`·`progressRank`·`stageKey`·`stageName`). 세이브 전체(T13)가 아니라 전투가 읽고 쓰는 칸만이다.
    /// 티어 이름·상한·챕터 스테이지 수·사이클 길이는 `state.json`(<see cref="SaveDefs"/> · T13)에서 받는다 — 코드 상수 0.
    /// </summary>
    public sealed class Progress
    {
        /// <summary>`DIFFICULTY_NAMES` — 인덱스 = 티어. 0 은 기본 순환(라벨을 챕터에서 뽑는다).</summary>
        public readonly string[] DifficultyNames;
        /// <summary>`MAX_DIFFICULTY` = 이름 수 − 1.</summary>
        public readonly int MaxDifficulty;
        /// <summary>`STAGES_PER_CHAPTER`.</summary>
        public readonly int StagesPerChapter;
        /// <summary>`CHAPTERS_PER_CYCLE` = `CHAPTER_THEMES.length`.</summary>
        public readonly int ChaptersPerCycle;

        public int Difficulty, Chapter = 1, Stage = 1;
        public int BestDifficulty, BestChapter = 1, BestStage = 1;

        public Progress(SaveDefs defs)
            : this(Req(defs).ChaptersPerCycle, defs.StagesPerChapter, defs.MaxDifficulty, defs.DifficultyNames) { }

        static SaveDefs Req(SaveDefs defs)
        {
            if (defs == null) throw new ArgumentNullException("defs", "state.json(SaveDefs) 없이는 진행 축을 세울 수 없다");
            return defs;
        }

        public Progress(int chaptersPerCycle, int stagesPerChapter, int maxDifficulty, string[] difficultyNames)
        {
            if (chaptersPerCycle <= 0 || stagesPerChapter <= 0 || difficultyNames == null || difficultyNames.Length != maxDifficulty + 1)
                throw new ArgumentException("진행 상수가 비었다 — state.json 의 CHAPTERS_PER_CYCLE·STAGES_PER_CHAPTER·MAX_DIFFICULTY·DIFFICULTY_NAMES 를 확인");
            ChaptersPerCycle = chaptersPerCycle;
            StagesPerChapter = stagesPerChapter;
            MaxDifficulty = maxDifficulty;
            DifficultyNames = difficultyNames;
        }

        static int Clamp(int v, int a, int b) { return Math.Min(b, Math.Max(a, v)); }

        /// <summary>티어를 편 «절대 챕터»(티어 0 챕터 1 = 1 · 티어 1 챕터 1 = 26).</summary>
        public int AbsChapter(int tier, int chapter) { return Clamp(tier, 0, MaxDifficulty) * ChaptersPerCycle + chapter; }
        public int CurAbsChapter { get { return AbsChapter(Difficulty, Chapter); } }
        public int Rank(int tier, int chapter, int stage) { return AbsChapter(tier, chapter) * 100 + stage; }
        public int CurRank { get { return Rank(Difficulty, Chapter, Stage); } }
        public int BestRank { get { return Rank(BestDifficulty, BestChapter, BestStage); } }

        /// <summary>`stageKey()` — 티어 0 은 옛 키와 글자까지 같다(`"3-5"`) · 티어가 있으면 `"d1:3-5"`.</summary>
        public string StageKey()
        {
            return Difficulty > 0 ? "d" + Difficulty + ":" + Chapter + "-" + Stage : Chapter + "-" + Stage;
        }

        /// <summary>`stageDifficultyLabel` — 티어가 있으면 티어 이름 · 티어 0 은 챕터에서(원작 실측 근거: 쉬움/보통 한 챕터씩 · 3~8 어려움 · 그 위 매우 어려움).</summary>
        public string DifficultyLabel(int chapter, int tier)
        {
            if (tier > 0) return DifficultyNames[Clamp(tier, 1, MaxDifficulty)];
            if (chapter <= 1) return "쉬움";
            if (chapter <= 2) return "보통";
            if (chapter <= 8) return "어려움";
            return "매우 어려움";
        }

        /// <summary>`stageName()` — «어려움 3-5».</summary>
        public string StageName() { return DifficultyLabel(Chapter, Difficulty) + " " + Chapter + "-" + Stage; }

        /// <summary>최고 기록을 현재로(단방향 — `curRank() > bestRank()` 일 때만 부른다).</summary>
        public void RecordBest() { BestDifficulty = Difficulty; BestChapter = Chapter; BestStage = Stage; }
    }
}
