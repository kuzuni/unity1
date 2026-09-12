using System;
using Forge.Core.Data;

namespace Forge.Core.World
{
    /// <summary>정본 `setTheme(t)` 이 한 챕터에 정하는 것 중 배경 제거 모드(SIMPLE_BG)에서 **보이는** 값 전부.</summary>
    public sealed class ThemeLook
    {
        public string Biome, Kin;
        public bool Night;
        public Col FogColor;
        public double FogNear, FogFar;
        /// <summary>단색 배경(= 안개색 · SIMPLE_BG).</summary>
        public Col Background;
        /// <summary>값 그레이드 지면색(잎·능선·반구광 바닥이 여기서 파생).</summary>
        public Col GroundGrade;
        /// <summary>지면 재질색(흙 보정 뒤).</summary>
        public Col TerrainColor;
        /// <summary>노면 회랑(|z| &lt; 2.25) 배율 = 석재색 ÷ 흙색.</summary>
        public double[] Road;
        public double Snow;
        public double[] SunPos;
        public double SunIntensity, HemiIntensity, RimIntensity;
        public Col SunColor, HemiColor, HemiGround, RimColor;
        public double Exposure;
        public Col Stone;
        public Col TerrainEmissive;
        public double TerrainEmissiveIntensity;
        public bool CrackMap;
    }

    /// <summary>
    /// 정본 `Scene3D.setTheme(t)` 의 색·조명 파생 계산(T9 · 순수 C#). 순서·식은 원문 그대로 — `tools/world_vectors.js` 가 정본을 실제로 돌려 뽑은
    /// `t9-world.json` 과 EditMode 가 대조한다. 보이지 않는 파생(능선 3겹·잎·이끼·블롭 그림자·결정 재질)은 SIMPLE_BG 에서 화면에 없어 여기 없다(T35).
    /// </summary>
    public static class WorldGrade
    {
        public static ThemeLook Compute(SceneDefs d, ChapterTheme t)
        {
            return Compute(d, t.Sky, t.Fog, t.Ground, t.Biome ?? "forest", t.Celestial);
        }

        public static ThemeLook Compute(SceneDefs d, int sky, int fog, int ground, string biome, string celestial)
        {
            var L = new ThemeLook();
            bool night = (celestial ?? "sun") == "moon";
            Col skyC = Col.FromHex(sky);
            Col fogC = Col.FromHex(fog).Lerp(skyC, WorldRules.FogLerpToSky);
            if (!night) fogC = fogC.OffsetHsl(0, WorldRules.DayFogSatOffset, WorldRules.DayFogLightOffset);

            double gh, gs, gl;
            Col.FromHex(ground).GetHsl(out gh, out gs, out gl);
            double nightK = night ? WorldRules.NightGradeK : 1;
            double dL = -Math.Min(d.GroundMax, d.GroundK * gl) * nightK;
            Col gC = Col.FromHex(ground).OffsetHsl(0, d.SatK * dL, dL);
            Col soilC = SoilOf(d, gC, biome);
            Col roadC = RoadOf(soilC);

            string kin = d.Kin(biome);
            BiomeSpec sp = d.Spec(biome);

            L.Biome = biome; L.Kin = kin; L.Night = night;
            L.FogColor = fogC; L.Background = fogC;
            L.GroundGrade = gC; L.TerrainColor = soilC;
            L.Road = new[] {
                roadC.R / Math.Max(soilC.R, WorldRules.RoadDivFloor),
                roadC.G / Math.Max(soilC.G, WorldRules.RoadDivFloor),
                roadC.B / Math.Max(soilC.B, WorldRules.RoadDivFloor) };
            L.Snow = biome == "snow" ? WorldRules.SnowUniform : 0;
            L.HemiColor = skyC;
            L.HemiGround = gC.OffsetHsl(0, 0, WorldRules.HemiGroundLightOffset);
            L.Exposure = night ? WorldRules.ExposureNight : WorldRules.ExposureDay;

            // JS: `sp.emissive !== null` — 키가 없으면(undefined) 참, 명시적 null(흑요석)만 거짓.
            bool emissiveNotNull = sp == null || !sp.HasEmissive || sp.Emissive != null;
            if (night)
            {
                L.SunIntensity = WorldRules.NightSun; L.HemiIntensity = WorldRules.NightHemi; L.RimIntensity = WorldRules.NightRim;
                L.SunColor = Col.FromHex(WorldRules.NightSunColor).Lerp(skyC, WorldRules.NightSunLerpSky);
                L.SunPos = (double[])d.SunNight.Clone();
            }
            else
            {
                bool lavaLit = kin == "lava" && emissiveNotNull;
                L.SunIntensity = lavaLit ? WorldRules.LavaLitSun : WorldRules.DaySun;
                L.HemiIntensity = lavaLit ? WorldRules.LavaLitHemi : WorldRules.DayHemi;
                L.RimIntensity = WorldRules.DayRim;
                L.SunColor = Col.FromHex(WorldRules.DaySunColor).Lerp(skyC, WorldRules.DaySunLerpSky);
                L.SunPos = (double[])d.SunDay.Clone();
            }
            if (kin == "magic" && night) { L.RimIntensity = WorldRules.MagicNightRim; L.RimColor = Col.FromHex(WorldRules.MagicNightRimColor); }
            else L.RimColor = Col.FromHex(WorldRules.RimColor);

            if (sp != null && sp.Fog != null) { L.FogNear = sp.Fog[0]; L.FogFar = sp.Fog[1]; }
            else
            {
                L.FogNear = night ? WorldRules.FogNearNight : kin == "rock" ? WorldRules.FogNearRock : WorldRules.FogNearDefault;
                L.FogFar = (night || kin == "lava") ? WorldRules.FogFarNightOrLava : kin == "rock" ? WorldRules.FogFarRock : WorldRules.FogFarDefault;
            }

            if (sp != null && sp.Stone.HasValue) L.Stone = Col.FromHex(sp.Stone.Value);
            else if (kin == "snow") L.Stone = Col.FromHex(WorldRules.StoneSnow);
            else if (kin == "desert") L.Stone = Col.FromHex(WorldRules.StoneDesert);
            else if (kin == "rock") L.Stone = Col.FromHex(WorldRules.StoneRock);
            else L.Stone = StoneFrom(soilC);

            if (kin == "lava" && emissiveNotNull)
            {
                int? glow = sp != null ? sp.EmissiveHex("glow") : null;
                L.HemiGround = glow.HasValue ? Col.FromHex(glow.Value).MultiplyScalar(WorldRules.LavaGlowHemiK) : Col.FromHex(WorldRules.LavaHemiGround);
            }

            // tEm = sp.emissive !== undefined ? sp.emissive : (kin === 'lava' ? {crack, intensity} : null)
            int? crack = null; double intensity = 1;
            if (sp != null && sp.HasEmissive)
            {
                if (sp.Emissive != null) { crack = sp.EmissiveHex("crack"); double? i = sp.EmissiveNum("intensity"); if (i.HasValue) intensity = i.Value; }
            }
            else if (kin == "lava") { crack = WorldRules.LavaCrack; intensity = WorldRules.LavaCrackIntensity; }
            if (kin == "lava" && crack.HasValue)
            {
                L.TerrainEmissive = Col.FromHex(crack.Value); L.TerrainEmissiveIntensity = intensity; L.CrackMap = true;
            }
            else { L.TerrainEmissive = new Col(0, 0, 0); L.TerrainEmissiveIntensity = 1; L.CrackMap = false; }
            return L;
        }

        /// <summary>정본 `soilOf(gC, biome)` — forest kin + 초록 지면만 마른 흙(h 0.095)으로 · 상대 휘도 ×yK 를 이분 탐색으로 맞춘다.</summary>
        public static Col SoilOf(SceneDefs d, Col gC, string biome)
        {
            if (d.Kin(biome) != "forest") return gC;
            double h, s, l;
            gC.GetHsl(out h, out s, out l);
            if (h < WorldRules.SoilGreenHueMin || h > WorldRules.SoilGreenHueMax) return gC;
            double sat = Math.Min(s * d.SoilSatK, d.SoilSatMax);
            double target = gC.Luma * d.SoilYK;
            double lo = 0, hi = 1;
            for (int i = 0; i < WorldRules.SoilBisectSteps; i++)
            {
                double m = (lo + hi) / 2;
                Col o = Col.FromHsl(WorldRules.SoilHue, sat, m);
                if (o.Luma < target) lo = m; else hi = m;
            }
            return Col.FromHsl(WorldRules.SoilHue, sat, (lo + hi) / 2);
        }

        /// <summary>정본 `roadOf(soil)` — 무채 회석재 포석(밝은 지면 챕터만 어두운 쪽으로).</summary>
        public static Col RoadOf(Col soil)
        {
            double h, s, l;
            soil.GetHsl(out h, out s, out l);
            double nl = l > WorldRules.RoadBrightGroundL
                ? Math.Max(0.30, l - Math.Max(0.12, l * 0.28))
                : Math.Min(0.62, l + Math.Max(0.235, l * 0.86));
            return Col.FromHsl(h, Math.Min(s * WorldRules.RoadSatK, WorldRules.RoadSatMax), nl);
        }

        /// <summary>정본 `stoneFrom(gC)` — stone 스펙이 없는 kin(forest·magic·lava)의 돌색.</summary>
        public static Col StoneFrom(Col gC)
        {
            double h, s, l;
            gC.GetHsl(out h, out s, out l);
            return Col.FromHsl(h, Math.Min(WorldRules.StoneFromSatMax, s * WorldRules.StoneFromSatK),
                Col.Clamp(l + WorldRules.StoneFromLOffset, WorldRules.StoneFromLMin, WorldRules.StoneFromLMax));
        }
    }
}
