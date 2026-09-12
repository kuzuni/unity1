namespace Forge.Core.Battle
{
    /// <summary>
    /// 원작 `web/js/combat.js` 의 <c>Combat</c> 상수와, 전투가 기대는 바깥 상수(`scene3d.js` · `state.js` · `dungeons.js`) 를 **이름 그대로** 모아 둔 것(T7).
    /// 밸런스 «표» 가 아니라 전투 규칙 코드의 일부다(정본에서도 JSON 이 아니라 combat.js 안에 산다) — T2 추출 대상이 아니라 여기 둔다.
    /// 값을 고치려면 정본을 고치고 여기를 따라 맞춘다(«정본에서 고칠 것»).
    /// </summary>
    public static class BattleRules
    {
        /// <summary>100ms 고정 틱(초). `main.js LOGIC_TICK_MS / 1000`.</summary>
        public const double Tick = 0.1;
        /// <summary>적 근접 정지 위치 — 대열 맨 앞자리.</summary>
        public const double MeleeX = -0.5;
        public const double HeroX = -1.35;
        /// <summary>이웃한 두 적의 (반폭+반폭) 중 x 로 벌릴 비율(`restackMelee`).</summary>
        public const double MeleePack = 0.62;
        /// <summary>그 위에 얹는 고정 여유(유닛).</summary>
        public const double MeleeGap = 0.2;
        /// <summary>슬롯별 깊이 레인 — 앞줄 / 카메라 쪽 / 안쪽.</summary>
        public static readonly double[] MeleeLaneZ = { 0, 0.95, -0.9 };

        /// <summary>쓰러져 누워 있는 구간(ms). 불변식: DOWN + RISE + MARCH*1000 ≤ Scene3D.DEATH_FADE 의 delay+fadeIn+hold.</summary>
        public const double DeathDownMs = 1600;
        /// <summary>기상 클립 길이(ms · ProChar Revive dur 0.85s).</summary>
        public const double DeathRiseMs = 850;
        /// <summary>기상 뒤 한 박자(초) — 이 뒤에 setupStage.</summary>
        public const double DeathMarchS = 0.4;

        /// <summary>보스 웨이브 HP 배율 — 일반 몹 대비.</summary>
        public const double BossHpMult = 6;
        /// <summary>보스 공격력 = HP / 이 값.</summary>
        public const double BossAtkDiv = 9;
        /// <summary>일반 몹 공격력 = HP / 이 값.</summary>
        public const double MobAtkDiv = 14;
        /// <summary>메인 스테이지 한 판의 웨이브 수(마지막이 보스). 던전은 run.waves(1~3).</summary>
        public const int MainWaves = 5;
        /// <summary>던전 run.waves 가 없는 구버전 진행 상태용 폴백(`Dungeons.DEFAULT_WAVES`).</summary>
        public const int DungeonDefaultWaves = 3;

        /// <summary>몬스터 기본 HP 곡선 `55 · 5.6^(절대챕터−1) · 1.19^(스테이지−1)`(`monsterBaseHp`).</summary>
        public const double MonsterHpBase = 55, MonsterHpPerChapter = 5.6, MonsterHpPerStage = 1.19;
        /// <summary>웨이브마다 HP +8%(`spawnWave`).</summary>
        public const double WaveHpStep = 0.08;
        /// <summary>1장 초반 보스 완화 `min(1, 0.35 + 0.09·(스테이지−1))`(`bossEase`).</summary>
        public const double BossEaseBase = 0.35, BossEasePerStage = 0.09;

        /// <summary>일반 몹 등장 x = 3.1 + i·1.2 + rand(0, 0.4) · 속도 rand(1.0, 1.4) · 첫 공격 대기 rand(0.3, 0.9).</summary>
        public const double SpawnX = 3.1, SpawnGap = 1.2, SpawnJitter = 0.4, SpeedMin = 1.0, SpeedMax = 1.4, FirstAtkMin = 0.3, FirstAtkMax = 0.9;
        /// <summary>적 공격 주기(초) · 타격 지연(초).</summary>
        public const double EnemyAtkPeriod = 1.7, EnemyHitDelay = 0.15;
        /// <summary>영웅 사거리 — 원거리는 접근 전에 발사, 근접은 붙었을 때.</summary>
        public const double RangedRange = 3.4, MeleeRange = 1.3;
        /// <summary>연타 두 번째 타격 간격(초) · 타격 피해 흔들림 rand(0.9, 1.1).</summary>
        public const double DoubleHitGap = 0.12, DmgJitterMin = 0.9, DmgJitterMax = 1.1;
        /// <summary>치명 셰이크 · 광역 스킬 셰이크 · 단일 스킬 셰이크 · 보스 처치 셰이크.</summary>
        public const double CritShake = 0.12, AoeShake = 0.35, SingleShake = 0.22, BossKillShake = 0.5;
        /// <summary>스킬 사거리(x &lt; 3.2) · 착탄 시각 기본값(광역 0.25 · 단일 0.2 · `impactAt` 없을 때).</summary>
        public const double SkillRange = 3.2, AoeImpactDefault = 0.25, SingleImpactDefault = 0.2;
        /// <summary>스킬 쿨 첫 진입·스테이지 전환 스태거 `1 + rand·2`.</summary>
        public const double CooldownStaggerBase = 1, CooldownStaggerSpan = 2;
        /// <summary>체력 자연 회복 기본 1%/s(+ 서브스탯 hpRegen).</summary>
        public const double BaseRegenPerSec = 0.01;
        /// <summary>지속 회복 숫자 표시 뭉침(초).</summary>
        public const double HotFloatEvery = 0.5;
        /// <summary>웨이브 사이 행군(초) · 스테이지 클리어 뒤 행군(초) · 던전 클리어 뒤 복귀 행군(초).</summary>
        public const double WaveDelay = 1.6, StageDelay = 2.2, DungeonReturnDelay = 0.8;

        /// <summary>처치 코인 `ceil(3 · 1.6^(절대챕터−1) · 1.06^(스테이지−1))` · 보스 ×8 · 첫 클리어 보너스 `ceil(60 · …)`.</summary>
        public const double CoinBase = 3, CoinPerChapter = 1.6, CoinPerStage = 1.06, BossCoinMult = 8, FirstClearBonusBase = 60;
        /// <summary>해머 `1 + floor(절대챕터/3)` · 보스 ×8 · 일반 35%.</summary>
        public const double HammerChapterDiv = 3, BossHammerMult = 8, HammerChance = 0.35;

        /// <summary>`Scene3D.BOSS_IMPACT` — 보스가 지면을 밟는 순간(초) · 이때 스폰.</summary>
        public const double BossImpact = 1.55;
        /// <summary>`Scene3D.BOSS_SPAWN_X` — 보스가 서는 자리(논리 x).</summary>
        public const double BossSpawnX = 1.75;
        /// <summary>`Scene3D.enemyHalfW` 의 메시가 없을 때 폴백(반폭).</summary>
        public const double DefaultEnemyHalfW = 0.5;
    }
}
