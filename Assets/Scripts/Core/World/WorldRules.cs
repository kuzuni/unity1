namespace Forge.Core.World
{
    /// <summary>
    /// 정본 `scene3d.js setTheme(t)` 안에 **인라인 리터럴로** 박힌 규칙 값(T9). 표(`scene.json`)가 아니라 코드라 여기 한 곳에 모은다 —
    /// 각 줄의 출처를 적어 두니 정본이 바뀌면 이 파일만 맞춘다(T7 `BattleRules`·T14 `ForgeRules` 와 같은 규약).
    /// </summary>
    public static class WorldRules
    {
        // 안개색 = fog → sky 30% lerp · 낮이면 채도 +0.09 · 명도 +0.01 (setTheme 첫머리)
        public const double FogLerpToSky = 0.3;
        public const double DayFogSatOffset = 0.09;
        public const double DayFogLightOffset = 0.01;
        // 밤 챕터는 값 그레이드를 절반만 (nightK)
        public const double NightGradeK = 0.5;
        // 반구광 바닥색 = gC 명도 −0.1
        public const double HemiGroundLightOffset = -0.1;
        // 톤맵 노출 (renderer.toneMappingExposure)
        public const double ExposureDay = 1.02;
        public const double ExposureNight = 0.82;
        // 밤 조명
        public const double NightSun = 0.55, NightHemi = 0.36, NightRim = 0.45;
        public const int NightSunColor = 0xc6d4ff;
        public const double NightSunLerpSky = 0.25;
        // 낮 조명 (용암 발광 챕터는 감광)
        public const double DaySun = 1.00, DayHemi = 0.15, DayRim = 0.18;
        public const double LavaLitSun = 0.54, LavaLitHemi = 0.22;
        public const int DaySunColor = 0xffedc4;
        public const double DaySunLerpSky = 0.15;
        // 림
        public const int RimColor = 0xcfe4ff;
        public const int MagicNightRimColor = 0x7fdcff;
        public const double MagicNightRim = 1.05;
        // 안개 심도 (sp.fog 가 없을 때)
        public const double FogNearNight = 11, FogNearRock = 15, FogNearDefault = 13;
        public const double FogFarNightOrLava = 30, FogFarRock = 42, FogFarDefault = 35;
        // 지면 셰이더 눈 수광면 탈색 — 기본 `snow` 바이옴에서만
        public const double SnowUniform = 0.7;
        // 돌색 (sp.stone 이 없을 때의 kin 분기)
        public const int StoneSnow = 0xc9d8e6, StoneDesert = 0xb97f5e, StoneRock = 0x51483e;
        // 용암 kin 기본 발광표 (sp.emissive 가 undefined 일 때)
        public const int LavaCrack = 0xff3d00, LavaGlow = 0xff5722, LavaLight = 0xff5722;
        public const double LavaCrackIntensity = 1.15;
        public const int LavaHemiGround = 0x8a3d1a;
        public const double LavaGlowHemiK = 0.45;
        // 흙 보정(soilOf)의 마른 흙 색상과 초록 판정 구간
        public const double SoilHue = 0.095;
        public const double SoilGreenHueMin = 0.17, SoilGreenHueMax = 0.45;
        public const int SoilBisectSteps = 24;
        // 노면 절대색(roadOf)
        public const double RoadBrightGroundL = 0.55;
        public const double RoadSatK = 0.24, RoadSatMax = 0.06;
        // 돌 파생(stoneFrom)
        public const double StoneFromSatK = 0.45, StoneFromSatMax = 0.22, StoneFromLOffset = 0.16, StoneFromLMin = 0.28, StoneFromLMax = 0.55;
        // 노면 배율 0 나눗셈 하한
        public const double RoadDivFloor = 0.02;
        // 지면 격자 (voxelGroundGeo · 폭·깊이 60 · 원점 · 타일 주기 30 · 벽 색 0.8 · 노면 |z| 경계)
        public const double GroundX0 = -30, GroundZ0 = -45, GroundSpan = 60, TilePeriod = 30;
        public const double WallShade = 0.8;
        public const double RoadHalfWidth = 2.25, RoadCoreHalfWidth = 1.5;
        public const double RoadJitter = 0.035, SoilJitter = 0.022;
        // 스크롤 (update): 영웅이 15 넘게 앞서면 지면 타일을 30 밀어 순환 · 행군 속도 1.7/s
        public const double ScrollAhead = 15, WalkSpeed = 1.7;
    }
}
