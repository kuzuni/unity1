using System.Collections.Generic;

namespace Forge.Core.Forging
{
    /// <summary>
    /// 대장간이 읽고 쓰는 세이브 조각 — 원작 `S.forgeLevel`·`S.rollLevel`·`S.forgeUpgradeEndsAt`·`S.autoForge`·`S.lineAscend.forge`.
    /// 직렬화·마이그레이션은 T13(`Core/Save`)이 쥔다 — 여기는 값만.
    /// </summary>
    public sealed class ForgeState
    {
        public int ForgeLevel = 1;
        /// <summary>시대 → 그 시대의 현재 뽑기 레벨. 비어 있으면 <see cref="ForgeEngine.EnsureRollLevels"/> 가 전부 1 로 채운다.</summary>
        public Dictionary<string, double> RollLevel = new Dictionary<string, double>();
        /// <summary>업그레이드 완료 절대시각(ms) · null 이면 미진행.</summary>
        public double? UpgradeEndsAt;
        /// <summary>대장간 라인 승천 횟수(원작 `Ascension.count('forge')`) — 제작 장비에 찍히는 별 수. T24 가 올리고 되돌린다.</summary>
        public int AscendCount;
        /// <summary>자동 제련 설정 · null 이면 <see cref="ForgeEngine.AutoForgeConfig"/> 가 기본값을 만든다.</summary>
        public AutoForgeConfig AutoForge;
    }

    /// <summary>재화 — 원작 `S.coins`·`S.gems`·`S.hammers`·`S.totalCrafts`(전부 Number). 대장간은 코인·젬을 쓰고 망치를 소모한다.</summary>
    public sealed class Wallet
    {
        public double Coins;
        public double Gems;
        public double Hammers;
        public double TotalCrafts;
    }

    /// <summary>자동 제련 설정(원작 `S.autoForge` · UI-SPEC 21~24 «자동 제련» 팝업).</summary>
    public sealed class AutoForgeConfig
    {
        /// <summary>유지 시대 — 비어 있으면 시대 조건 없음.</summary>
        public List<string> KeepAges = new List<string>();
        public bool FilterOn;
        /// <summary>옵션 필터 — 서브스탯 키 목록.</summary>
        public List<string> FilterSubs = new List<string>();
        /// <summary>1회 제련 사이클당 소모 망치 수(1~22).</summary>
        public double HammersPerBatch = 10;
        /// <summary>목표 장비가 나오면 정지(기본 false = 예산 끝까지 계속). 옛 `continueOnTarget` 은 참/거짓 모두 «계속» 으로 넘긴다.</summary>
        public bool StopOnTarget;
        /// <summary>옛 세이브의 `continueOnTarget` — 값이 있으면 <see cref="ForgeEngine.AutoForgeConfig"/> 가 지우고 StopOnTarget=false 로 넘긴다.</summary>
        public bool? LegacyContinueOnTarget;
    }
}
