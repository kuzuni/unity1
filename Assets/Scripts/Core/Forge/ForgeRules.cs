using System;
using Forge.Core.Data;

namespace Forge.Core.Forging
{
    /// <summary>
    /// 원작 `forge.js` 의 `Forge` 객체가 **코드에 들고 있는** 상수와 순수 계산식(T14).
    /// 표(확률·비용·시간)는 `balance.json`(<see cref="ForgeTable"/>)에서 읽는다 — 여기에는 정본 `forge.js` 가 객체 필드로 박아 둔 값만 있다
    /// (그것들은 balance-data.js 표가 아니라 원작 코드 자체다 · 결정 기록 참조). 값을 바꾸려면 정본을 먼저 바꾼다.
    /// </summary>
    public static class ForgeRules
    {
        /// <summary>대장간 만렙(원작 `Forge.MAX_LEVEL`). 확률표 행 수(<see cref="ForgeTable.MaxLevel"/>)와 같아야 한다 — 테스트가 단언한다.</summary>
        public const int MaxLevel = 35;
        /// <summary>장비 티어 기본치 — 시대가 오를수록 ×<see cref="TierStep"/>.</summary>
        public const double TierBaseAtk = 12;
        public const double TierBaseHp = 70;
        public const double TierStep = 6;
        /// <summary>무기·장갑·목걸이·반지.</summary>
        public const int AtkSlots = 4;
        /// <summary>투구·갑옷·신발·벨트.</summary>
        public const int HpSlots = 4;
        /// <summary>레벨당 배수 — 장비·펫·탈것 공용 커브.</summary>
        public const double LevelStep = 1.01;

        /// <summary>뽑기 레벨 기본 캡(기술트리 «장비 레벨업» 노드로 상향).</summary>
        public const int RollBaseCap = 100;
        /// <summary>랜덤워크 +1 확률(%).</summary>
        public const double RollUpPct = 70;
        /// <summary>랜덤워크 동일 확률(%) — 나머지가 −1.</summary>
        public const double RollSamePct = 20;

        /// <summary>젬 스킵: 남은 시간 10분당 젬 1(원작 `gemSkipCost` 의 `/ 10`).</summary>
        public const double GemSkipMinutesPerGem = 10;

        public static double TierBaseAtkAt(int ageIdx) { return TierBaseAtk * Math.Pow(TierStep, ageIdx); }
        public static double TierBaseHpAt(int ageIdx) { return TierBaseHp * Math.Pow(TierStep, ageIdx); }

        /// <summary>레벨 배율 = LEVEL_STEP^(level − 1). 원작은 `level || 1` 이라 0 은 1 로 본다.</summary>
        public static double LevelMult(int level) { return Math.Pow(LevelStep, (level == 0 ? 1 : level) - 1); }

        /// <summary>펫·탈것 등급 6단계를 장비 시대 10단계에 양끝 맞춤(일반=원시 0 · 신화=디바인 9) — 사이는 실수.</summary>
        public static double AgeOfRarity(GameDefs defs, string rarity)
        {
            return Array.IndexOf(defs.Rarities, rarity) * (double)(defs.Ages.Length - 1) / (defs.Rarities.Length - 1);
        }

        /// <summary>같은 등급·레벨에서 장비 8부위 합(공격 4부위) — 펫·탈것 기준치가 여기서 나온다.</summary>
        public static double GearSumAtkAt(GameDefs defs, string rarity, int level = 1) { return AtkSlots * TierBaseAtk * Math.Pow(TierStep, AgeOfRarity(defs, rarity)) * LevelMult(level); }
        public static double GearSumHpAt(GameDefs defs, string rarity, int level = 1) { return HpSlots * TierBaseHp * Math.Pow(TierStep, AgeOfRarity(defs, rarity)) * LevelMult(level); }

        /// <summary>등급 가중치(대장간 레벨 fl) — 원작 `rarityWeights` 의 계수 그대로 · 순서 common→mythic(weightedPick 순서가 결과를 정한다).</summary>
        public static OrderedMap<double> RarityWeights(int fl)
        {
            var w = new OrderedMap<double>();
            w.Add("common", 60);
            w.Add("rare", 22 + fl * 0.3);
            w.Add("epic", 9 + fl * 0.35);
            w.Add("legendary", 3 + fl * 0.22);
            w.Add("ultimate", 0.6 + fl * 0.1);
            w.Add("mythic", 0.08 + fl * 0.04);
            return w;
        }

        /// <summary>업그레이드 비용 = max(1, floor(표 비용 × 기술트리 배율)).</summary>
        public static double UpgradeCost(ForgeUpgrade info, double costMult) { return Math.Max(1, Math.Floor(info.Cost * costMult)); }
        /// <summary>업그레이드 시간(초) = 표 시간 × 기술트리 배율.</summary>
        public static double UpgradeTime(ForgeUpgrade info, double timeMult) { return info.Time * timeMult; }

        /// <summary>남은 ms → 젬(10분당 1 · 올림). 0 이하면 0.</summary>
        public static double GemSkipCost(double remainMs)
        {
            double remainMin = Math.Max(0, remainMs / 60000);
            return Math.Ceiling(remainMin / GemSkipMinutesPerGem);
        }
    }
}
