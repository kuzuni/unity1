using System;

namespace Forge.Core.World
{
    /// <summary>지면 셰이더(T38 `Forge/Terrain`)에 넣는 값 한 벌 — 정본 `TERRAIN` 유니폼 + `applyShadeLift` 의 지면 재질 유니폼(`setTheme` 이 챕터마다 덮어쓰는 것).</summary>
    public sealed class TerrainShadeParams
    {
        /// <summary>정본 `uMacro` · `uMacroScale`(uv 6 주기 = 월드 30 · 지면 순환 주기) · `uLod` · `uLodNear` · `uLodFar` · `uSnow`(눈 바이옴만).</summary>
        public double Macro, MacroScale, Lod, LodNear, LodFar, Snow;
        /// <summary>정본 `uShadeTint`(지면 재질용 `stG`) · `uShadeStr`(밤 0.7배).</summary>
        public Col ShadeTint;
        public double ShadeStr;
        /// <summary>정본 `st` — 지면이 아닌 소품 재질(잎·돌·줄기)용 리프트 색. 소품(T35)이 같은 값을 쓴다.</summary>
        public Col PropShadeTint;
        /// <summary>정본 `lift` = max(0, 하늘 L − 0.38(밤 −0.46)).</summary>
        public double Lift;
    }

    /// <summary>
    /// 정본 `scene3d.js` `terrainShade`/`applyShadeLift`/`setTheme`(17817~17829) 의 값 부분 — 순수 계산(엔진 0). 셰이더 코드는 `Assets/Shaders/Terrain.shader`.
    /// 숫자 상수는 정본 코드 상수(WorldRules 와 같은 갈래): 리프트 명도 = 하늘 L − 0.38(밤 −0.46) · 지면 틴트 채도 = min(흙 s × 0.5, 0.12) · 소품 틴트 = 하늘→지면색 0.40 혼합 + 채도 0.10 · 밤 강도 0.7.
    /// </summary>
    public static class TerrainShade
    {
        public const double LiftDayOffset = -0.38, LiftNightOffset = -0.46;
        public const double TerrainTintSatK = 0.5, TerrainTintSatMax = 0.12;
        public const double PropTintMix = 0.40, PropTintSatOffset = 0.10;
        public const double NightStrengthK = 0.7;
        /// <summary>정본 `uMacroScale = 1/6` — 지면 순환 주기(월드 30)를 uv 로 환산한 역수. 반복 12 × (60 폭) 에서 uv 1 = 월드 5 → 30 = uv 6.</summary>
        public static double MacroScale(SceneDefs d)
        {
            double worldPerUv = WorldRules.GroundSpan / GroundTexBake.RepeatX;
            return 1.0 / (WorldRules.TilePeriod / worldPerUv);
        }

        /// <summary>테마 하나의 셰이더 값. sky 는 테마 하늘색(<see cref="ThemeLook.HemiColor"/> 가 그것 · 반구광 색 = `t.sky`).</summary>
        public static TerrainShadeParams Compute(SceneDefs d, ThemeLook L)
        {
            var p = new TerrainShadeParams
            {
                Macro = d.TerrainMacro, MacroScale = MacroScale(d), Lod = d.TerrainLod, LodNear = d.TerrainLodNear, LodFar = d.TerrainLodFar,
                Snow = L.Snow,
            };
            Col sky = L.HemiColor, gC = L.GroundGrade, soil = L.TerrainColor;
            double sh, ss, sl;
            sky.GetHsl(out sh, out ss, out sl);
            double mh, ms, ml;
            sky.Lerp(gC, PropTintMix).GetHsl(out mh, out ms, out ml);
            double lift = Math.Max(0, sl + (L.Night ? LiftNightOffset : LiftDayOffset));
            p.Lift = lift;
            p.PropShadeTint = Col.FromHsl(mh, Col.Clamp(ms + PropTintSatOffset, 0, 1), lift);
            double oh, os, ol;
            soil.GetHsl(out oh, out os, out ol);
            p.ShadeTint = Col.FromHsl(oh, Math.Min(os * TerrainTintSatK, TerrainTintSatMax), lift);
            p.ShadeStr = d.ShadeStrength * (L.Night ? NightStrengthK : 1);
            return p;
        }
    }
}
