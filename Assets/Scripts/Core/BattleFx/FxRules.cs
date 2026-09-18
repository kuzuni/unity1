using System;
using Forge.Core.CraftFx;
using Forge.Core.Voxel;

namespace Forge.Core.BattleFx
{
    /// <summary>
    /// T39 — 전투 씬 후속 연출의 규칙 숫자·순수식(원작 `scene3d.js`: `RIM`/`ENEMY_RIM`(1294·1316) · `bossMaterialTell`(11362) · `bossRegalia`(11415) ·
    /// `setDissolve`(12520 · DSV_*) · `impactFlare/Spikes/Ring`(12544~) · `flashMesh`(12768) · `hitEnemy`(12974)·`killEnemy`(13089) 의 플래시/플레어 인자 ·
    /// `bossEntrance`(13475~) · `flashLight`(16844) · `scorchDecal`(16873) · `expandRing`(16892) · `TRAIL_*`(17053~) · `corpseBlob`(13414) · `SCENE_CUT_DUR`(17360) ·
    /// `DEATH_FADE`(17401)). 엔진 참조 0 — Game 쪽(`HitFlashFx`·`BossFx`·`TrailFx`·`BattleOverlay`)이 이 값만 읽는다. 값은 정본 줄 그대로.
    /// </summary>
    public static class FxRules
    {
        // ── 림(applyRimLight · RIM + ENEMY_RIM 오버라이드) ──
        public const int RimColor = 0xdcefff, RimDark = 0x0a1119;
        public const double RimStrength = 0, RimPower = 5.0, RimDarkStrength = 0.88, RimDarkPower = 1.35;
        /// <summary>`ENEMY_RIM: { darkStrength: 0.98, darkPower: 0.85 }`.</summary>
        public const double EnemyRimDarkStrength = 0.98, EnemyRimDarkPower = 0.85;

        // ── 보스 재질(bossMaterialTell) ──
        /// <summary>채도 ×1.08 · 명도 ×0.68(바닥 0.12).</summary>
        public const double BossSatK = 1.08, BossLightK = 0.68, BossLightFloor = 0.12;
        /// <summary>몸 팔레트는 정점색이고 재질 색은 흰색이라(T4 `Mobs.build` 규약) 재질 배율로 옮긴다 — 흰색의 HSL 명도 1 × 0.68.</summary>
        public static int BossTintHex()
        {
            double h, s, l;
            ColorHsl.GetHsl(0xffffff, out h, out s, out l);
            return ColorHsl.FromHsl(h, Math.Min(1, s * BossSatK), Math.Max(BossLightFloor, l * BossLightK));
        }

        // ── 보스 레갈리아(bossRegalia) ──
        public const int RegaliaGold = 0xffd54f, RegaliaGoldDark = 0xc79a2e, RegaliaGoldEmissive = 0xffa000, RegaliaGoldDarkEmissive = 0xff8f00;
        public const double RegaliaGoldEmissiveI = 0.32, RegaliaGoldDarkEmissiveI = 0.18, RegaliaGemEmissiveI = 0.95;
        public const int RegaliaSectors = 12, RegaliaSlices = 14, RegaliaSpikes = 6;
        public const double RegaliaSliceSpan = 0.45, RegaliaSolidFrac = 0.6, RegaliaSeatFrac = 0.3, RegaliaHeadRMin = 0.05, RegaliaBandR = 0.94;
        public const double RegaliaRingTube = 0.15, RegaliaRingH = 0.30, RegaliaOrnMin = 0.13, RegaliaSpikeH = 0.8, RegaliaSpikeR = 0.17, RegaliaFrontK = 1.5, RegaliaFrontR = 1.15;
        public const double RegaliaSpikeRing = 0.92, RegaliaSpikeLift = 0.10, RegaliaSpikeTilt = 0.26, RegaliaGemR = 0.22, RegaliaGemRMax = 0.5, RegaliaGemLift = 0.05;
        public static readonly double[] RidgeK = { 0.62, 1.0, 0.86, 0.58 };
        public const double RidgeY = 0.62, RidgeBand = 0.22, RidgeHalfMin = 0.12, RidgeR = 0.19, RidgeH = 0.95, RidgeSink = 0.18, RidgeTilt = -0.55, RidgeTopRad = 0.55;
        public const double HornY = 0.72, HornBand = 0.06, HornMinSide = 0.03, HornR = 0.22, HornH = 1.1, HornX = 0.86, HornTilt = 0.62, RegaliaTopPad = 0.03;
        public const double RegaliaVoxelJitter = 0.04, RegaliaVoxelAo = 0.9;
        /// <summary>보석 = 종 키 컬러의 보색(`offsetHSL(0.5, 0.25, 0.12)`).</summary>
        public static int GemColor(int baseHex)
        {
            double h, s, l;
            ColorHsl.GetHsl(baseHex, out h, out s, out l);
            return ColorHsl.FromHsl((h + 0.5) % 1, Math.Max(0, Math.Min(1, s + 0.25)), Math.Max(0, Math.Min(1, l + 0.12)));
        }

        // ── 디졸브(setDissolve) ──
        public const double DsvLo = 0.24, DsvHi = 0.78, DsvTail = 0.88, DsvEnd = 0.95;
        /// <summary>진행도 f(0~1) → 셰이더 임계값. 노이즈가 실제로 분포한 대역 [LO, HI] 로 펴고 마지막 12% 는 0.95 까지 밀어 올린다.</summary>
        public static double DissolveValue(double f)
        {
            double c = f < 0 ? 0 : f > 1 ? 1 : f;
            if (f <= 0) return 0;
            if (c <= DsvTail) return DsvLo + (c / DsvTail) * (DsvHi - DsvLo);
            return DsvHi + ((c - DsvTail) / (1 - DsvTail)) * (DsvEnd - DsvHi);
        }
        public const double DsvEmberWidth = 0.13, DsvEmberMix = 0.92, DsvEmberGain = 1.6;
        public const double DsvEmberR = 1.0, DsvEmberG = 0.52, DsvEmberB = 0.16;
        public const double DsvNoiseA = 6.5, DsvNoiseB = 15.0, DsvNoiseWA = 0.62, DsvNoiseWB = 0.38, DsvYLo = -0.6, DsvYHi = 1.2, DsvYK = 0.10;

        // ── 몸 플래시(flashMesh · hitEnemy/killEnemy 인자) ──
        public static double FlashPeak(bool crit) { return crit ? 0.28 : 0.2; }
        public static double FlashDur(bool crit) { return crit ? 0.14 : 0.1; }
        public static int FlashColor(bool crit) { return crit ? 0xff7a1a : 0xcfe8ff; }
        public static double FlashOutline(bool crit) { return crit ? 0.78 : 0.38; }
        public const double KillFlashPeak = 0.3, KillFlashDur = 0.09, KillFlashShapeK = 1.0, KillFlashOutline = 1.0;
        public const int KillFlashColor = 0xfff6e0;
        /// <summary>shapeK 형태 보존 — emissive 를 재질 명도에 비례(`1 − sk × 0.65 × (1 − lum)`).</summary>
        public static double FlashEmScale(double shapeK, double lum) { return 1 - shapeK * 0.65 * (1 - lum); }
        public static double Luma(double r, double g, double b) { double v = 0.299 * r + 0.587 * g + 0.114 * b; return v < 0 ? 0 : v > 1 ? 1 : v; }

        // ── 접점 임팩트(hitEnemy ②) ──
        public const double HitFlareMax = 0.55;
        public static double FlareCoreSize(bool crit, double fmax) { return Math.Min(crit ? 0.62 : 0.5, fmax * 0.62); }
        public static double FlareCoreDur(bool crit) { return crit ? 0.12 : 0.09; }
        public static double FlareMidSize(bool crit, double fmax) { return Math.Min(crit ? 0.95 : 0.74, fmax); }
        public static double FlareMidDur(bool crit) { return crit ? 0.14 : 0.1; }
        public static double FlareMidPeak(bool crit) { return crit ? 0.42 : 0.3; }
        public const int FlareCore = 0xffffff, FlareMid = 0xffb45a, FlareOuter = 0xff7a2a;
        public const double FlareCorePeak = 0.55, FlareCoreSpin = 0.3, FlareMidSpin = -0.24, FlareOuterSizeCap = 1.25, FlareOuterSizeK = 1.3, FlareOuterDur = 0.17, FlareOuterSpin = -0.5, FlareOuterPeak = 0.3;
        public const double FlareDefaultPeak = 0.85, FlareStart = 0.9, FlareGrow = 0.45;
        public static int SpikeCount(bool crit) { return crit ? 9 : 6; }
        public static int SpikeColor(bool crit) { return crit ? 0xffb15a : 0xdff2ff; }
        public static double SpikeSize(bool crit, double fmax) { return fmax * (crit ? 1.5 : 1); }
        public static double SpikeDur(bool crit) { return crit ? 0.15 : 0.11; }
        public static double SpikeOpacity(bool crit) { return crit ? 0.95 : 0.8; }
        public static double SpikeWidth(bool crit, double size) { return size * (crit ? 0.16 : 0.12); }
        public const double SpikeLenMin = 0.55, SpikeLenMax = 1.0, SpikeJitter = 0.18, SpikeGrowPow = 2.2, SpikeStartLen = 0.35, SpikeGrowLen = 0.9, SpikeShrinkW = 0.55, SpikeOffset0 = 0.2, SpikeOffsetK = 0.5;
        public static int RingColor(bool crit) { return crit ? 0xff9a4d : 0xcfe8ff; }
        public static double RingDur(bool crit) { return crit ? 0.16 : 0.12; }
        public static double RingOpacity(bool crit) { return crit ? 0.95 : 0.75; }
        public static double RingGrow(bool crit) { return crit ? 1.5 : 1.05; }
        public const double RingStart = 0.25, RingGrowPow = 2.6, PxRingF = 1.54, PxRingCell = 0.06;
        /// <summary>픽셀 계단 링 — 격자 12칸 · 바깥 6.45 · 안쪽 4.4(굵은 링 3.9).</summary>
        public const double PxRingOuter = 6.45, PxRingInner = 4.4, PxRingInnerThick = 3.9;
        public const int PxRingHalf = 6;

        // ── 처치 버스트(killEnemy) ──
        public static double KillFlareMax(bool isBoss) { return isBoss ? 1.1 : 0.95; }
        public const double KillFlareCoreSize = 0.5, KillFlareCoreDur = 0.13, KillFlareCoreSpin = 0.3, KillFlareCorePeak = 0.5;
        public const int KillFlareMid = 0xffd28a;
        public const double KillFlareMidDur = 0.18, KillFlareMidSpin = 0.2, KillFlareMidPeak = 0.42, KillFlareOuterSize = 1.28, KillFlareOuterDur = 0.22, KillFlareOuterSpin = -0.4, KillFlareOuterPeak = 0.24;
        public static int KillLightColor(bool isBoss) { return isBoss ? 0xffab40 : 0xffcc80; }
        public static double KillLightDur(bool isBoss) { return isBoss ? 0.45 : 0.3; }
        public const int KillRingColor = 0xffab40;
        public static double KillRingR(bool isBoss) { return isBoss ? 1.8 : 1.0; }
        public const double ScorchDx = 0.18;
        public static double ScorchRadius(bool isBoss, double baseScale) { return (isBoss ? 1.2 : 0.66) * baseScale; }
        public static double ScorchDur(bool isBoss) { return isBoss ? 2.4 : 1.8; }

        // ── 점광(flashLight) · 그을음(scorchDecal) · 지면 충격 링(expandRing) ──
        public const double LightDistance = 7, LightPeak = 2.8, LightDy = 0.8, LightDz = 0.5, LightDefaultDur = 0.3;
        public const int ScorchColor = 0x3a1a0a;
        public const double ScorchY = 0.035, ScorchPeak = 0.72, ScorchIn = 0.08, ScorchOutPow = 1.6, ScorchScale0 = 0.72, ScorchScaleK = 0.28, ScorchScaleSpeed = 6;
        /// <summary>그을음 불투명도 곡선.</summary>
        public static double ScorchOpacity(double k) { return k < ScorchIn ? ScorchPeak * (k / ScorchIn) : ScorchPeak * Math.Pow(1 - (k - ScorchIn) / (1 - ScorchIn), ScorchOutPow); }
        public static double ScorchScale(double k) { return ScorchScale0 + ScorchScaleK * Math.Min(1, k * ScorchScaleSpeed); }
        public const int ExpandUnderColor = 0x2a1a0e;
        public const double ExpandDur = 0.17, ExpandGrowPow = 2.4, ExpandStart = 0.35, ExpandRingY = 0.12, ExpandUnderY = 0.118, ExpandUnderScale = 1.04, ExpandRingOpacity = 0.95, ExpandUnderOpacity = 0.55;
        public static double ExpandScale(double k, double maxR) { double g = 1 - Math.Pow(1 - k, ExpandGrowPow); return ExpandStart + g * maxR * 2; }
        public static double FadeSq(double k) { return (1 - k) * (1 - k); }

        // ── 보스 등장(bossEntrance) ──
        public const double BossWarnDur = 2.0, BossBeat = 0.42;
        public const int BossBeats = 3;
        public const double BossCamPush = 0.9;
        public static double BossCamPushAt(double t, double impact) { return BossCamPush * (1 - Math.Pow(1 - Math.Min(1, t / impact), 3)); }
        public const int BossRingColor = 0xff3d2e, BossLightColor = 0xff2a1e, BossImpactRing = 0xff6a3c, BossImpactRing2 = 0xffe0b2, BossImpactSpark = 0xffab40, BossImpactLight = 0xff8a3d, BossPillarColor = 0xff1a10;
        public static double BossBeatRing(int b) { return 1.0 + b * 0.45; }
        public const double BossBeatLightDur = 0.26;
        public static double BossBeatShake(int b) { return 0.05 + b * 0.035; }
        public const double BossImpactShake = 0.5, BossImpactFov = 0.05, BossImpactFovDur = 0.3, BossImpactRingR = 3.4, BossImpactRing2R = 2.0, BossImpactSparks = 22, BossImpactSparkSpeed = 2.2, BossImpactLightDur = 0.4, BossCamRelease = 0.45;
        public const double PillarOpacity = 0.72, PillarFadeIn = 0.5, PillarScale0 = 0.55, PillarScaleK = 0.45, PillarSpin = 0.9, PillarCell = 0.42, PillarLift = 1.68, PillarJitter = 0.08;
        public const int PillarLayers = 8, PillarWideLayers = 4, PillarWideR = 2, PillarNarrowR = 1;
        public const double PillarBrightPow = 1.8;
        public static double PillarPulse(double t) { double u = (t % BossBeat) / BossBeat; return 0.35 + 0.65 * (1 - u) * (1 - u); }

        // ── 무기 궤적(trail) · 스우시 ──
        public const double TrailLife = 0.22, TrailMinStep = 0.06, TrailBase = 0.12, TrailTipDefault = 0.7, TrailImpactRelax = 0.22, TrailAlpha = 0.72, TrailFadePow = 1.6, TrailCorePow = 4.0, TrailCoreDefault = 0.35;
        public const int TrailSegments = 48, TrailInterpMax = 12;
        public const double TrailInterpStep = 0.06;
        public static double TrailTip(string shape)
        {
            switch (shape)
            {
                case "sword": return 0.95; case "axe": return 0.85; case "spear": return 1.25; case "hammer": return 0.8; case "dagger": return 0.55;
                case "club": return 0.6; case "mace": return 0.75; case "rapier": return 1.05; case "scythe": return 1.15;
            }
            return TrailTipDefault;
        }
        public struct TrailTier { public int Color; public double Gain, Core; }
        public static TrailTier TrailTierOf(string tier)
        {
            switch (tier)
            {
                case "crit": return new TrailTier { Color = 0xff7a1a, Gain = 1.3, Core = 0.62 };
                case "kill": return new TrailTier { Color = 0xfff6e0, Gain = 1.5, Core = 0.9 };
            }
            return new TrailTier { Color = 0xcfe8ff, Gain = 1.0, Core = 0.35 };
        }
        /// <summary>`trailStart` 의 색 보정 — `offsetHSL(0, 0.35, −0.04)`.</summary>
        public static int TrailStartColor(int weaponHex)
        {
            double h, s, l;
            ColorHsl.GetHsl(weaponHex, out h, out s, out l);
            return ColorHsl.FromHsl(h, Math.Max(0, Math.Min(1, s + 0.35)), Math.Max(0, Math.Min(1, l - 0.04)));
        }
        public const double SwooshR = 0.34, SwooshTube = 0.03, SwooshArc = Math.PI * 0.55, SwooshOpacity = 0.6, SwooshPeak = 0.85, SwooshDur = 0.1, SwooshDx = 0.4, SwooshY = 1.0, SwooshZ = 0.05, SwooshRotY = 0.4, SwooshRotZ = -0.9, SwooshGrow = 0.8, SwooshSweep = 1.6;

        // ── 영웅 블롭 · 시체 블롭(corpseBlob) ──
        public const double HeroBlobScale = 0.82, HeroBlobY = 0.025, HeroBlobOpacity = 0.17;
        public static readonly string[] CorpseBones = { "shoulderL", "pelvis", "kneeR" };
        public const double CorpseBlobY = 0.028, CorpseBlobSx = 1.35, CorpseBlobSz = 0.78, CorpseBlobOpacity = 0.5, CorpseBaseOpacity = 0.06, CorpseBlobDur = 0.3, CorpseRevivePause = 0.38;
        public static double SmoothStep01(double k) { return k * k * (3 - 2 * k); }

        // ── 암전(sceneCut · deathFade) ──
        public const double SceneCutMs = 420;
        public const double DeathDelayMs = 900, DeathFadeInMs = 700, DeathHoldMs = 1800, DeathFadeOutMs = 700;
        public static double DeathTotalMs { get { return DeathDelayMs + DeathFadeInMs + DeathHoldMs + DeathFadeOutMs; } }
        /// <summary>커버 불투명도(0~1) — 0~900 투명 · 900~1600 잠김 · 1600~3400 암전 · 3400~4100 걷힘(구간 이징 cubic-bezier(.3,0,.35,1) ≈ 스무스스텝).</summary>
        public static double DeathCoverAlpha(double ms)
        {
            if (ms <= DeathDelayMs) return 0;
            double a = DeathDelayMs + DeathFadeInMs;
            if (ms < a) return SmoothStep01((ms - DeathDelayMs) / DeathFadeInMs);
            double b = a + DeathHoldMs;
            if (ms < b) return 1;
            double c = b + DeathFadeOutMs;
            if (ms < c) return 1 - SmoothStep01((ms - b) / DeathFadeOutMs);
            return 0;
        }
        /// <summary>배너 등장 진행(0~1) — 900+700×0.55 에서 시작해 1600+500 에 끝난다(ease-out).</summary>
        public static double DeathBannerK(double ms)
        {
            double s = DeathDelayMs + DeathFadeInMs * 0.55, e = DeathDelayMs + DeathFadeInMs + 500;
            double k = (ms - s) / (e - s);
            k = k < 0 ? 0 : k > 1 ? 1 : k;
            return 1 - Math.Pow(1 - k, 3);
        }
        public const double DeathBannerRise = 0.6;
        public const string DeathTitle = "죽었습니다";
        /// <summary>씬컷 커버 — 검은 끝에서 빠르게 물러나는 감속 커브(cubic-bezier(.3,0,.35,1) 근사).</summary>
        public static double SceneCutAlpha(double k) { return 1 - SmoothStep01(k < 0 ? 0 : k > 1 ? 1 : k); }

        // ── 보스 워닝 배너(css #boss-warning) ──
        public const double WarnTop = 0.28, WarnDimIn = 0.08, WarnDimHold = 0.76, WarnFlashPeak = 0.12, WarnBannerK = 0.62, WarnHazardPeriod = 0.62, WarnScrollPeriod = 2.6, WarnSubDelay = 0.25;
        public const string WarnText = "WARNING ◆ WARNING ◆ WARNING ◆ WARNING ◆ ";
        public const string WarnSub = "보스 출현";
        public static double WarnDim(double u) { return u < WarnDimIn ? u / WarnDimIn : u < WarnDimHold ? 1 : 1 - (u - WarnDimHold) / (1 - WarnDimHold); }
        /// <summary>붉은 점멸 3회(.42s ease-out).</summary>
        public static double WarnFlash(double t)
        {
            if (t >= BossBeat * BossBeats) return 0;
            double u = (t % BossBeat) / BossBeat;
            return u < WarnFlashPeak ? u / WarnFlashPeak : 1 - (u - WarnFlashPeak) / (1 - WarnFlashPeak);
        }
        /// <summary>배너 scaleY — cubic-bezier(.16,1.5,.4,1) 오버슈트 근사(첫 12% 에 1.12 까지 튀고 1 로).</summary>
        public static double WarnBanner(double u)
        {
            if (u >= 0.92) return Math.Max(0, 1 - (u - 0.92) / 0.08);
            double k = Math.Min(1, u / 0.12);
            return k < 1 ? 1.12 * Math.Sin(k * Math.PI / 2) : 1 + 0.12 * Math.Max(0, 1 - (u - 0.12) / 0.2);
        }
        /// <summary>T467 — 부제 자간·들여쓰기(em)의 등장 조임. 정본 431~436 `@keyframes bwsub`: 0% <paramref name="fromEm"/> → 16%(<paramref name="tightenAt"/>) <paramref name="toEm"/>, 그 뒤 정착.
        /// 416 의 `ease-out` 은 키프레임 **구간마다** 걸리는 곡선이라 0→16% 안에서 `cubic-bezier(0,0,.58,1)` 로 보간한다. 값은 표(`LetterSpacingUi.json`)가 쥐고 여기는 셈뿐이다.</summary>
        public static double WarnSubSpacing(double u, double fromEm, double toEm, double tightenAt)
        {
            if (tightenAt <= 0 || u >= tightenAt) return toEm;
            double k = u <= 0 ? 0 : u / tightenAt;
            return fromEm + (toEm - fromEm) * WarnSubEase.Ease(k);
        }
        /// <summary>CSS `ease-out` = `cubic-bezier(0, 0, .58, 1)`.</summary>
        static readonly CssEase WarnSubEase = new CssEase(0, 0, 0.58, 1);
        public static double WarnSubAlpha(double u) { return u < WarnSubDelay ? u / WarnSubDelay : u < WarnDimHold ? 1 : Math.Max(0, 1 - (u - WarnDimHold) / (1 - WarnDimHold)); }

        // ── 피격 붉은 비네트(T135 ⓐ · `ui.js` 1360 `flashDamage` · `style.css` 438 `#dmg-flash` · 481 `@keyframes dmgvignette`) ──
        // 정본은 세기만 JS 가 `--vig` 로 주고 지속·감쇠는 CSS 키프레임이 쥔다("JS 타이머+트랜지션 조합은 연타 시 서로 잘라먹는다").
        // 여기서도 같게 나눈다 — 세기는 <see cref="DmgVigPeak"/>, 시계는 <see cref="DmgVigAlpha"/>, 그림은 <see cref="DmgVigSample"/>·<see cref="DmgVigMask"/>.

        /// <summary>`min(.64, .32 + sev×1.05)` (`ui.js` 1366) — 정본이 `toFixed(2)` 로 자르므로 소수 둘째 자리에서 반올림한다.</summary>
        public const double DmgVigBase = 0.32, DmgVigK = 1.05, DmgVigMax = 0.64;
        /// <summary>정본 `sev || 0.12` — 0·undefined 면 0.12 로 떨어진다.</summary>
        public const double DmgVigSevFallback = 0.12;
        /// <summary>`animation: dmgvignette .44s` + 키프레임 0%/3%/10%/100%.</summary>
        public const double DmgVigMs = 440, DmgVigRise = 0.03, DmgVigHold = 0.10;
        /// <summary>`radial-gradient(ellipse at center, transparent 46%, rgba(120,14,14,.34) 72%, rgba(255,58,44,.92) 100%)`.</summary>
        public const double DmgVigStop0 = 0.46, DmgVigStop1 = 0.72;
        public const int DmgVigMidRgb = 0x780e0e, DmgVigEdgeRgb = 0xff3a2c;
        public const double DmgVigMidA = 0.34, DmgVigEdgeA = 0.92;
        /// <summary>`mask-image: linear-gradient(to bottom, #000 82%, transparent 100%)` — 아래 18% 를 빼 씬 경계에서 잘리는 대신 사라지게 한다.</summary>
        public const double DmgVigMaskKeep = 0.82;

        /// <summary>CSS `ease-out` = cubic-bezier(0,0,.58,1) 근사. 검산: 그 곡선은 x=.3425 에서 y=.5 · x=.7347 에서 y=.896 이고 이 식은 .500·.897 을 준다.</summary>
        public static double EaseOut01(double k)
        {
            k = k < 0 ? 0 : k > 1 ? 1 : k;
            return 1 - Math.Pow(1 - k, 1.68);
        }

        /// <summary>`flashDamage(sev)` 가 `--vig` 에 넣는 값.</summary>
        public static double DmgVigPeak(double sev)
        {
            double s = sev > 0 ? sev : DmgVigSevFallback;
            return Math.Round(Math.Min(DmgVigMax, DmgVigBase + s * DmgVigK), 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>`@keyframes dmgvignette` — 0%→0 · 3%→정점 · 10%→정점 · 100%→0 (구간마다 ease-out).</summary>
        public static double DmgVigAlpha(double ms, double peak)
        {
            double u = ms / DmgVigMs;
            if (u <= 0 || u >= 1) return 0;
            if (u < DmgVigRise) return peak * EaseOut01(u / DmgVigRise);
            if (u < DmgVigHold) return peak;
            return peak * (1 - EaseOut01((u - DmgVigHold) / (1 - DmgVigHold)));
        }

        /// <summary>`ellipse at center`(기본 farthest-corner)의 그라디언트 반지름 — 상자 네 모서리에서 정확히 1, 한가운데서 0.</summary>
        public static double DmgVigT(double u, double v)
        {
            double dx = 2 * u - 1, dy = 2 * v - 1;
            return Math.Sqrt((dx * dx + dy * dy) / 2);
        }

        /// <summary>세로 마스크 — <paramref name="yFromTop"/> 0=씬 위, 1=씬 아래.</summary>
        public static double DmgVigMask(double yFromTop)
        {
            if (yFromTop <= DmgVigMaskKeep) return 1;
            double k = (yFromTop - DmgVigMaskKeep) / (1 - DmgVigMaskKeep);
            return k >= 1 ? 0 : 1 - k;
        }

        /// <summary>
        /// 반지름 <paramref name="t"/>(<see cref="DmgVigT"/>)에서의 비네트 색. 0~1 RGB 와 알파.
        /// 정지점 사이는 **프리멀티플라이드**로 섞는다 — CSS 가 `transparent`(rgba(0,0,0,0))를 그렇게 섞어서,
        /// 첫 구간이 검게 물들지 않고 붉은색 그대로 알파만 오른다.
        /// </summary>
        public static void DmgVigSample(double t, out double r, out double g, out double b, out double a)
        {
            double mr = ((DmgVigMidRgb >> 16) & 0xff) / 255.0, mg = ((DmgVigMidRgb >> 8) & 0xff) / 255.0, mb = (DmgVigMidRgb & 0xff) / 255.0;
            double er = ((DmgVigEdgeRgb >> 16) & 0xff) / 255.0, eg = ((DmgVigEdgeRgb >> 8) & 0xff) / 255.0, eb = (DmgVigEdgeRgb & 0xff) / 255.0;
            if (t <= DmgVigStop0) { r = mr; g = mg; b = mb; a = 0; return; }
            if (t >= DmgVigStop1)
            {
                double k2 = Math.Min(1, (t - DmgVigStop1) / (1 - DmgVigStop1));
                Mix(mr, mg, mb, DmgVigMidA, er, eg, eb, DmgVigEdgeA, k2, out r, out g, out b, out a);
                return;
            }
            double k1 = (t - DmgVigStop0) / (DmgVigStop1 - DmgVigStop0);
            Mix(mr, mg, mb, 0, mr, mg, mb, DmgVigMidA, k1, out r, out g, out b, out a);
        }

        static void Mix(double r0, double g0, double b0, double a0, double r1, double g1, double b1, double a1, double k,
                        out double r, out double g, out double b, out double a)
        {
            a = a0 + (a1 - a0) * k;
            if (a <= 0) { r = r1; g = g1; b = b1; return; }
            r = (r0 * a0 + (r1 * a1 - r0 * a0) * k) / a;
            g = (g0 * a0 + (g1 * a1 - g0 * a0) * k) / a;
            b = (b0 * a0 + (b1 * a1 - b0 * a0) * k) / a;
        }
    }
}
