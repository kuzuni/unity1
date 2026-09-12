using System;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.World;

namespace Forge.Tests
{
    /// <summary>T38 — 지면 셰이더 값(정본 TERRAIN 유니폼 · setTheme 의 applyShadeLift 유니폼)이 정본 식과 같다. 셰이더 컴파일은 CI 유니티 잡(WorldTests)이 본다.</summary>
    public class TerrainShadeTests
    {
        static SceneDefs Defs { get { return SceneDefs.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(DataDir.Path, SceneDefs.File))); } }

        static ThemeLook Look(int sky, int fog, int ground, string biome, string celestial)
        {
            return WorldGrade.Compute(Defs, sky, fog, ground, biome, celestial);
        }

        [Test]
        public void TERRAIN_유니폼은_scene_json_값이고_매크로_주기는_지면_순환_30()
        {
            var d = Defs;
            var L = Look(0x87ceeb, 0xc0e2b6, 0x7cb342, "forest", null);   // 1챕터
            var p = TerrainShade.Compute(d, L);
            Assert.AreEqual(0.30, p.Macro, 1e-12); Assert.AreEqual(0.45, p.Lod, 1e-12);
            Assert.AreEqual(14, p.LodNear, 1e-12); Assert.AreEqual(34, p.LodFar, 1e-12);
            Assert.AreEqual(d.TerrainMacro, p.Macro); Assert.AreEqual(d.TerrainLod, p.Lod);
            Assert.AreEqual(1.0 / 6, p.MacroScale, 1e-12, "uv 6 = 월드 30 (반복 12 / 폭 60 → uv 1 = 5)");
            Assert.AreEqual(0, p.Snow, 1e-12, "초원은 눈 탈색 없음");
        }

        [Test]
        public void 눈_탈색은_snow_바이옴만_0_7_이고_빙하_툰드라는_0()
        {
            var d = Defs;
            Assert.AreEqual(0.7, TerrainShade.Compute(d, Look(0x1a237e, 0x283593, 0xaac2e2, "snow", null)).Snow, 1e-12);
            Assert.AreEqual(0, TerrainShade.Compute(d, Look(0x8fc7e8, 0xc2e2f2, 0x9fc9de, "glacier", null)).Snow, 1e-12, "18 빙하는 청빙 — 대상 아님");
            Assert.AreEqual(0, TerrainShade.Compute(d, Look(0xa9b7bf, 0xc6d2d8, 0x9a8f60, "tundra", null)).Snow, 1e-12);
        }

        [Test]
        public void 암부_리프트_명도는_하늘_L_에서_0_38_밤은_0_46_을_빼고_0_아래는_0()
        {
            var d = Defs;
            // 1챕터 하늘 0x87ceeb: three getHSL l = 0.72549… → lift 0.3454902 (node+three 실측)
            var day = TerrainShade.Compute(d, Look(0x87ceeb, 0xc0e2b6, 0x7cb342, "forest", null));
            Assert.AreEqual(0.34549019607843134, day.Lift, 1e-9);
            Assert.AreEqual(d.ShadeStrength, day.ShadeStr, 1e-12, "낮 강도 = SHADE.strength 0.26");
            Assert.AreEqual(0.26, day.ShadeStr, 1e-12);
            // 6 설원 밤 하늘 0x1a237e: l 0.29804 − 0.46 < 0 → 0
            var night = TerrainShade.Compute(d, Look(0x1a237e, 0x283593, 0xaac2e2, "snow", "moon"));
            Assert.AreEqual(0, night.Lift, 1e-12);
            Assert.AreEqual(0.26 * 0.7, night.ShadeStr, 1e-12, "밤 강도 0.7배");
            // 18 빙하 낮 하늘 0x8fc7e8: l 0.73529… → lift 0.3552941 (three 실측)
            var glacier = TerrainShade.Compute(d, Look(0x8fc7e8, 0xc2e2f2, 0x9fc9de, "glacier", null));
            Assert.AreEqual(0.35529411764705887, glacier.Lift, 1e-9);
        }

        [Test]
        public void 지면_틴트는_흙_색상에_채도_절반_상한_0_12_명도_리프트()
        {
            var d = Defs;
            var L = Look(0x87ceeb, 0xc0e2b6, 0x7cb342, "forest", null);
            var p = TerrainShade.Compute(d, L);
            double h, s, l, oh, os, ol;
            L.TerrainColor.GetHsl(out oh, out os, out ol);
            p.ShadeTint.GetHsl(out h, out s, out l);
            Assert.AreEqual(oh, h, 1e-6, "색상 = 흙");
            Assert.AreEqual(Math.Min(os * 0.5, 0.12), s, 1e-6, "채도 = min(흙 s × 0.5, 0.12)");
            Assert.AreEqual(p.Lift, l, 1e-6, "명도 = lift");
            Assert.LessOrEqual(s, 0.12 + 1e-9);
            // 소품용 틴트(st): 하늘→지면색 0.40 혼합의 색상 · 채도 +0.10 · 같은 lift
            double mh, ms, ml;
            L.HemiColor.Lerp(L.GroundGrade, 0.40).GetHsl(out mh, out ms, out ml);
            double ph, ps, pl;
            p.PropShadeTint.GetHsl(out ph, out ps, out pl);
            Assert.AreEqual(mh, ph, 1e-6); Assert.AreEqual(Math.Min(1, ms + 0.10), ps, 1e-6); Assert.AreEqual(p.Lift, pl, 1e-6);
            // lift 0 이면 틴트는 검정(리프트 없음)
            var night = TerrainShade.Compute(d, Look(0x1a237e, 0x283593, 0xaac2e2, "snow", "moon"));
            Assert.AreEqual(0, night.ShadeTint.R, 1e-12); Assert.AreEqual(0, night.ShadeTint.G, 1e-12); Assert.AreEqual(0, night.ShadeTint.B, 1e-12);
        }

        [Test]
        public void 테마_25종_전부_값이_유한하고_범위_안이다()
        {
            var d = Defs;
            var themes = DataDir.Game.Defs.ChapterThemes;
            Assert.AreEqual(25, themes.Count);
            foreach (var t in themes)
            {
                var L = WorldGrade.Compute(d, t);
                var p = TerrainShade.Compute(d, L);
                Assert.IsTrue(p.Lift >= 0 && p.Lift <= 1, t.Biome + " lift");
                Assert.IsTrue(p.ShadeStr > 0 && p.ShadeStr <= 0.26, t.Biome + " str");
                Assert.IsTrue(p.Snow == 0 || p.Snow == 0.7, t.Biome + " snow");
                Assert.AreEqual(t.Biome == "snow" ? 0.7 : 0, p.Snow, 1e-12, t.Biome + " snow 는 바이옴 이름으로만 가른다");
                Assert.IsTrue(p.ShadeTint.R >= 0 && p.ShadeTint.R <= 1 && p.ShadeTint.G >= 0 && p.ShadeTint.G <= 1 && p.ShadeTint.B >= 0 && p.ShadeTint.B <= 1, t.Biome + " tint");
            }
        }
    }
}
