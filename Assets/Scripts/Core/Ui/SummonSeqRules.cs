using System;
using System.Collections.Generic;

namespace Forge.Core.Ui
{
    /// <summary>
    /// T334 — 소환 결과 연출의 **상태 기계**(정본 `ui.js` `tickSummonResult` 703~731 · `fireSummonHero` 736~758 · `finishSummonResult` 761~765).
    ///
    /// 정본은 매 프레임 «지금까지 떴어야 할 셀» 을 경과 시간으로 다시 판정해 밀린 만큼 따라잡고(한 프레임에 여러 개가 몰려도
    /// 효과음은 최고 등급 하나 · <see cref="Tick"/> 의 반환값), 홀드백이고 마지막 한 칸을 남긴 순간부터 `charging`(정지가 아니라 축적),
    /// 주역 셀이 착지하는 프레임에 `hero`(+ 홀드백이면 `flash` · 아니면 주역 셀 중심 가산 원형 `wipe` — 정본 주석 «x75 신화가 x5 신화보다
    /// 약하다» 의 원인이 이 분기였다), 전부 뜬 시각에서 `SR_TAIL_MS` 뒤 `done`(`charging` 해제). 수치·지연표는 인자로 받는다 —
    /// 표(`SummonFxUi.json` `seq` 절)와 러너는 2회차(T179 lock 뒤). UnityEngine 참조 0.
    /// </summary>
    public sealed class SummonSeqRun
    {
        readonly double[] delays;
        readonly int[] rank;

        /// <summary>셀 수.</summary>
        public int Count { get { return delays.Length; } }
        /// <summary>홀드백(최고 등급 1개를 마지막에 한 박자 늦게) — 정본 `_srHoldback`.</summary>
        public bool Holdback { get; private set; }
        /// <summary>주역(최고 등급) 셀 번호 · 없으면 −1 — 정본 `_srHeroIdx`.</summary>
        public int HeroIdx { get; private set; }
        /// <summary>마지막 아이콘이 뜬 뒤 [확인]까지의 여운(ms) — 정본 `SR_TAIL_MS`.</summary>
        public double TailMs { get; private set; }
        /// <summary>주역 비트(광창·충격파·화면 킥) 길이(ms) — 정본 fireSummonHero 주석 «350ms».</summary>
        public double HeroKickMs { get; private set; }

        /// <summary>지금까지 뜬 셀 수(정본 `_srIdx`).</summary>
        public int Revealed { get; private set; }
        /// <summary>`charging` — 홀드백 대기 구간(소환진·눈금·비네트).</summary>
        public bool Charging { get; private set; }
        /// <summary>주역이 착지한 시각(ms) · 아직이면 −1.</summary>
        public double HeroAtMs { get; private set; }
        /// <summary>`flash` — 뜸들인 단독 등장의 전 화면 섬광(홀드백일 때만).</summary>
        public bool Flash { get; private set; }
        /// <summary>`wipe` — 홀드백이 없는 주역(대량 소환)의 주역 셀 중심 가산 원형 와이프.</summary>
        public bool Wipe { get; private set; }
        /// <summary>전부 뜬 뒤 `done` 이 되는 시각(ms) · 아직이면 −1.</summary>
        public double FinishAtMs { get; private set; }
        /// <summary>`done` — 힌트를 감추고 [확인]을 띄운 상태.</summary>
        public bool Done { get; private set; }

        public bool Hero { get { return HeroAtMs >= 0; } }

        /// <param name="delaysMs">셀마다 뜨는 시각(ms · 오름차순 · 정본 `_srDelays`).</param>
        /// <param name="rarityRank">셀마다 등급 순위(정본 `RARITIES.indexOf` · 클수록 높다).</param>
        public SummonSeqRun(IList<double> delaysMs, IList<int> rarityRank, bool holdback, int heroIdx, double tailMs, double heroKickMs)
        {
            if (delaysMs == null || rarityRank == null || delaysMs.Count != rarityRank.Count) throw new ArgumentException("delays 와 rank 의 길이가 다르다");
            delays = new double[delaysMs.Count]; rank = new int[rarityRank.Count];
            for (int i = 0; i < delays.Length; i++) { delays[i] = delaysMs[i]; rank[i] = rarityRank[i]; }
            Holdback = holdback; HeroIdx = heroIdx; TailMs = tailMs; HeroKickMs = heroKickMs;
            HeroAtMs = -1; FinishAtMs = -1;
        }

        /// <summary>
        /// 정본 `tickSummonResult` 한 프레임. 밀린 셀을 다 띄우고 상태를 옮긴다.
        /// 반환 = 이번 프레임에 뜬 것 중 최고 등급 순위(효과음은 그 하나만) · 아무것도 안 떴으면 −1.
        /// </summary>
        public int Tick(double elapsedMs)
        {
            int loud = -1;
            while (Revealed < Count && delays[Revealed] <= elapsedMs)
            {
                if (rank[Revealed] > loud) loud = rank[Revealed];
                Revealed++;
            }
            if (Done) return loud;
            // 홀드백 대기 구간은 «정지» 가 아니라 «축적» — 마지막 한 칸을 남긴 순간부터.
            if (Holdback && Revealed == Count - 1) Charging = true;
            // 주역 셀이 착지하는 프레임: 그 셀 위치를 원점으로 등급색 한 번.
            if (loud >= 0 && Revealed >= Count && HeroIdx >= 0 && !Hero)
            {
                HeroAtMs = elapsedMs;
                Charging = false;
                if (Holdback) Flash = true; else Wipe = true;
            }
            if (Revealed >= Count && FinishAtMs < 0) FinishAtMs = elapsedMs + TailMs;
            if (FinishAtMs >= 0 && elapsedMs >= FinishAtMs)
            {
                Done = true;
                Charging = false;
            }
            return loud;
        }

        /// <summary>주역 비트가 도는 중인가(착지 뒤 `HeroKickMs` 안).</summary>
        public bool HeroKicking(double elapsedMs) { return Hero && elapsedMs >= HeroAtMs && elapsedMs - HeroAtMs < HeroKickMs; }
    }
}
