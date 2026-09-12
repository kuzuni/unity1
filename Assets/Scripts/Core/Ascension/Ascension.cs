using System;
using System.Collections.Generic;

namespace Forge.Core.Ascend
{
    /// <summary>승천 세이브 조각(원작 `S.lineAscend`) — 라인 → 승천 횟수(= 이후 그 라인에서 얻는 아이템의 별 개수).</summary>
    public sealed class AscensionState
    {
        public Dictionary<string, int> LineAscend = new Dictionary<string, int>();
    }

    /// <summary>승천 조건이 걸리는 지표 — 장비만 대장간 레벨, 나머지는 각 소환 레벨(원작 `Forge`·`Skills.summonLevel()`·`Pets.summonLevel()`·`Mounts.level()` 이 준다).</summary>
    public struct AscensionLevels
    {
        public int ForgeLevel;
        public int SkillSummonLevel;
        public int PetSummonLevel;
        public int MountLevel;
    }

    /// <summary>진행도 {cur, max} — 승천 게이지·안내 문구 공용.</summary>
    public struct AscensionProgress
    {
        public int Cur, Max;
        public AscensionProgress(int cur, int max) { Cur = cur; Max = max; }
    }

    /// <summary>카테고리별 별 합계(보유 아이템 기준).</summary>
    public struct StarBreakdown
    {
        public int Gear, Skill, Pet, Mount;
        public int Total { get { return Gear + Skill + Pet + Mount; } }
    }

    /// <summary>
    /// 승천(별) 시스템 — 정본 `web/js/ascension.js` 를 그대로. 라인(장비/스킬/펫/탈것) 단위 프레스티지:
    /// 도달 조건을 채우면 그 라인을 승천시키고, 승천 횟수 = 이후 그 라인에서 새로 획득하는 아이템에 찍히는 별 개수.
    /// 승천하면 그 라인의 **기존 보유 아이템은 전부 사라진다**(ascend-wipe-line-items) — 지우는 대상(장비 8슬롯·대장간 레벨·뽑기 레벨 / 소환 횟수·스킬·장착 /
    /// 펫 소환 횟수·펫·출전(알·부화는 남긴다) / 탈것 열기 횟수·탈것·출전)은 각 시스템(T14·T17·T16·T11)의 상태라 <see cref="Ascend"/> 가 `wipeLine(line)` 으로 되돌려 준다.
    /// </summary>
    public sealed class Ascension
    {
        public readonly AscensionTable Table;
        readonly int _summonMax;
        readonly int _mountMax;

        /// <param name="summonMax">스킬·펫 소환 레벨 상한(원작 100 · `SkillTable.MaxLevel` = skillRatesData 행 수).</param>
        /// <param name="mountMax">탈것 소환 레벨 상한(원작 `Mounts.MAX_LEVEL` 50 · `MountTable.MaxLevel` = mountSummonRates 행 수).</param>
        public Ascension(AscensionTable table, int summonMax, int mountMax)
        {
            if (table == null) throw new ArgumentNullException("table");
            Table = table; _summonMax = summonMax; _mountMax = mountMax;
        }

        /// <summary>별 N개 → 배율 STAR_MULT^N(<see cref="Big"/>). 별 0(이하)이면 1.</summary>
        public Big StarMult(int stars)
        {
            return stars <= 0 ? Big.One : Big.Of(Table.StarMult).Pow(stars);
        }

        /// <summary>라인별 소환 레벨 상한 — 스킬·펫은 100, 탈것은 확률표가 50까지라 그 상한이 만렙.</summary>
        public int SummonMax(string line)
        {
            return line == "mount" ? _mountMax : _summonMax;
        }

        public void Ensure(AscensionState s)
        {
            if (s.LineAscend == null) s.LineAscend = new Dictionary<string, int>();
            for (int i = 0; i < Table.Lines.Length; i++) if (!s.LineAscend.ContainsKey(Table.Lines[i])) s.LineAscend[Table.Lines[i]] = 0;
        }

        /// <summary>승천 횟수 = 앞으로 그 라인에서 획득할 아이템의 별 개수.</summary>
        public int Count(AscensionState s, string line)
        {
            if (s == null || s.LineAscend == null) return 0;
            int v; return s.LineAscend.TryGetValue(line, out v) ? v : 0;
        }

        public AscensionProgress Progress(string line, AscensionLevels lv)
        {
            if (line == "forge") return new AscensionProgress(lv.ForgeLevel, Table.ForgeLevel);
            int max = SummonMax(line);
            int cur = line == "skill" ? lv.SkillSummonLevel : line == "pet" ? lv.PetSummonLevel : lv.MountLevel;
            return new AscensionProgress(Math.Min(cur, max), max);
        }

        public bool Ready(string line, AscensionLevels lv)
        {
            var p = Progress(line, lv);
            return p.Cur >= p.Max;
        }

        /// <summary>
        /// 라인 승천 — 지표를 초기화하고 그 라인의 기존 보유 아이템을 전부 지운 뒤 승천 횟수를 1 올린다. 성공 시 true.
        /// `wipeLine` 이 원작의 라인별 초기화를 맡는다(장비: forgeLevel 1 · 업그레이드 취소 · 뽑기 레벨 리셋 · 8슬롯 null /
        /// 스킬: summonCount 0 · skills {} · equippedSkills [] / 펫: petSummonCount 0 · pets [] · activePets [] (알·부화는 남긴다) / 탈것: mountOpens 0 · mounts [] · activeMounts []).
        /// </summary>
        public bool Ascend(string line, AscensionLevels lv, AscensionState s, Action<string> wipeLine)
        {
            Ensure(s);
            if (!Ready(line, lv)) return false;
            if (wipeLine != null) wipeLine(line);
            s.LineAscend[line] = Count(s, line) + 1;
            return true;
        }

        /// <summary>카테고리별 별 합계 — 각 목록은 보유 아이템의 stars(없으면 0).</summary>
        public static StarBreakdown Breakdown(IEnumerable<int> gearStars, IEnumerable<int> skillStars, IEnumerable<int> petStars, IEnumerable<int> mountStars)
        {
            return new StarBreakdown { Gear = Sum(gearStars), Skill = Sum(skillStars), Pet = Sum(petStars), Mount = Sum(mountStars) };
        }

        public static int TotalStars(IEnumerable<int> gearStars, IEnumerable<int> skillStars, IEnumerable<int> petStars, IEnumerable<int> mountStars)
        {
            return Breakdown(gearStars, skillStars, petStars, mountStars).Total;
        }

        static int Sum(IEnumerable<int> xs)
        {
            int s = 0;
            if (xs != null) foreach (int x in xs) s += x;
            return s;
        }
    }
}
