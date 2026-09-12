using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.World;

namespace Forge.Tests
{
    /// <summary>T9 벡터(`Assets/Tests/EditMode/Vectors/t9-world.json` · `tools/world_vectors.js` 가 정본 setTheme·voxelGroundGeo 를 실물 three r128 위에서 실행한 것).</summary>
    static class WorldVectors
    {
        static JsonObject _doc;
        public static JsonObject Doc
        {
            get
            {
                if (_doc != null) return _doc;
                string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
                string file = Path.Combine(root, "Assets", "Tests", "EditMode", "Vectors", "t9-world.json");
                if (!File.Exists(file)) throw new FileNotFoundException("T9 벡터가 없다 — node tools/world_vectors.js 로 뽑는다: " + file);
                _doc = MiniJson.ParseObject(File.ReadAllText(file));
                return _doc;
            }
        }

        static SceneDefs _defs;
        public static SceneDefs Defs
        {
            get { return _defs ?? (_defs = SceneDefs.Parse(File.ReadAllText(Path.Combine(DataDir.Path, SceneDefs.File)))); }
        }

        public static void AssertCol(string what, object expect, Col got, double eps = 1e-9)
        {
            var e = J.Obj(expect);
            Assert.AreEqual(J.Num(e["r"]), got.R, eps, what + " r");
            Assert.AreEqual(J.Num(e["g"]), got.G, eps, what + " g");
            Assert.AreEqual(J.Num(e["b"]), got.B, eps, what + " b");
            Assert.AreEqual(J.Int(e["hex"]), got.Hex, what + " hex");
        }
    }

    /// <summary>T9 — 정본 `setTheme`·`voxelGroundGeo` 와 Core `WorldGrade`·`GroundGrid` 가 같은 입력에 같은 값을 내는가.</summary>
    public class WorldGradeTests
    {
        [Test]
        public void 씬_상수표가_읽힌다_정본_배포값()
        {
            var d = WorldVectors.Defs;
            Assert.IsTrue(d.SimpleBg, "정본은 배경 제거 모드(SIMPLE_BG true)");
            Assert.AreEqual(15, d.Biomes.Count, "신설 바이옴 15");
            Assert.AreEqual(0.75, d.VoxCell, 1e-12); Assert.AreEqual(0.375, d.VoxStep, 1e-12);
            Assert.AreEqual(3, d.SunDay.Length); Assert.AreEqual(3, d.SunNight.Length);
            Assert.AreEqual("forest", d.Kin("marsh")); Assert.AreEqual("lava", d.Kin("obsidian")); Assert.AreEqual("desert", d.Kin("desert"));
            var ob = d.Spec("obsidian");
            Assert.IsTrue(ob.HasEmissive && ob.Emissive == null, "흑요석은 emissive: null 로 발광을 끈다(undefined 와 다르다)");
            Assert.IsFalse(d.Spec("marsh").HasEmissive, "늪지는 emissive 키가 없다(undefined)");
            Assert.IsNull(d.Spec("forest"), "원본 6종은 표에 없다 = 예전 경로");
            Assert.AreEqual(25, DataDir.Game.Defs.ChapterThemes.Count);
        }

        [Test]
        public void three_Color_수학이_같다()
        {
            // three r128: getHex 는 ×255 뒤 버림. #7cb342 → HSL → 다시 hex 가 그대로.
            var c = Col.FromHex(0x7cb342);
            Assert.AreEqual(0x7cb342, c.Hex);
            double h, s, l; c.GetHsl(out h, out s, out l);
            var back = Col.FromHsl(h, s, l);
            Assert.AreEqual(c.R, back.R, 1e-12); Assert.AreEqual(c.G, back.G, 1e-12); Assert.AreEqual(c.B, back.B, 1e-12);   // 왕복은 double 로 같다(hex 는 버림이라 1 차이 날 수 있다 — three 도 같다)
            Assert.AreEqual(0, new Col(0, 0, 0).Hex);
            Assert.AreEqual(0xffffff, new Col(1, 1, 1).Hex);
            Assert.AreEqual(0.5, Col.EuclideanModulo(-0.5, 1), 1e-12);
            var m = Col.FromHex(0xff0000).Lerp(Col.FromHex(0x0000ff), 0.5);
            Assert.AreEqual(0.5, m.R, 1e-12); Assert.AreEqual(0.5, m.B, 1e-12); Assert.AreEqual(0x7f007f, m.Hex, "0.5×255 = 127.5 → 버림 127");
        }

        [Test]
        public void 테마_25_파생값이_정본_setTheme_와_같다()
        {
            var d = WorldVectors.Defs;
            var themes = DataDir.Game.Defs.ChapterThemes;
            var vec = J.Arr(WorldVectors.Doc["themes"]);
            Assert.AreEqual(themes.Count, vec.Count, "벡터의 테마 수 = CHAPTER_THEMES");
            for (int i = 0; i < vec.Count; i++)
            {
                var v = J.Obj(vec[i]);
                var t = themes[i];
                string w = "테마 " + (i + 1) + "(" + t.Biome + ") ";
                Assert.AreEqual(J.Int(J.Obj(v["theme"])["sky"]), t.Sky, w + "sky(gamedata.json 과 벡터가 같은 정본)");
                ThemeLook L = WorldGrade.Compute(d, t);
                Assert.AreEqual(J.Str(v["kin"]), L.Kin, w + "kin");
                Assert.AreEqual(J.Bool(v["night"]), L.Night, w + "night");
                var fog = J.Obj(v["fog"]);
                WorldVectors.AssertCol(w + "안개색", fog["color"], L.FogColor);
                Assert.AreEqual(J.Num(fog["near"]), L.FogNear, 1e-12, w + "fog near");
                Assert.AreEqual(J.Num(fog["far"]), L.FogFar, 1e-12, w + "fog far");
                WorldVectors.AssertCol(w + "배경", v["background"], L.Background);
                WorldVectors.AssertCol(w + "gC", v["gC"], L.GroundGrade);
                var tr = J.Obj(v["terrain"]);
                WorldVectors.AssertCol(w + "지면색(soilOf)", tr["color"], L.TerrainColor);
                double[] road = J.NumArr(tr["road"]);
                for (int k = 0; k < 3; k++) Assert.AreEqual(road[k], L.Road[k], 1e-9, w + "road[" + k + "]");
                Assert.AreEqual(J.Num(tr["snow"]), L.Snow, 1e-12, w + "snow");
                WorldVectors.AssertCol(w + "지면 발광", tr["emissive"], L.TerrainEmissive);
                Assert.AreEqual(J.Num(tr["emissiveIntensity"]), L.TerrainEmissiveIntensity, 1e-12, w + "발광 세기");
                Assert.AreEqual(J.Bool(tr["crackMap"]), L.CrackMap, w + "균열 맵");
                var sun = J.Obj(v["sun"]);
                double[] sp = J.NumArr(sun["pos"]);
                for (int k = 0; k < 3; k++) Assert.AreEqual(sp[k], L.SunPos[k], 1e-12, w + "sun pos");
                Assert.AreEqual(J.Num(sun["intensity"]), L.SunIntensity, 1e-12, w + "sun intensity");
                WorldVectors.AssertCol(w + "태양색", sun["color"], L.SunColor);
                var hemi = J.Obj(v["hemi"]);
                Assert.AreEqual(J.Num(hemi["intensity"]), L.HemiIntensity, 1e-12, w + "hemi intensity");
                WorldVectors.AssertCol(w + "반구광 하늘", hemi["color"], L.HemiColor);
                WorldVectors.AssertCol(w + "반구광 바닥", hemi["groundColor"], L.HemiGround);
                var rim = J.Obj(v["rim"]);
                Assert.AreEqual(J.Num(rim["intensity"]), L.RimIntensity, 1e-12, w + "rim intensity");
                WorldVectors.AssertCol(w + "림색", rim["color"], L.RimColor);
                Assert.AreEqual(J.Num(v["exposure"]), L.Exposure, 1e-12, w + "노출");
                WorldVectors.AssertCol(w + "돌색", v["stone"], L.Stone);
            }
        }

        [Test]
        public void 지면_격자가_정본_voxelGroundGeo_와_같다()
        {
            var d = WorldVectors.Defs;
            var g = J.Obj(WorldVectors.Doc["ground"]);
            GroundMesh m = GroundGrid.Build(d);
            int nx = J.Int(g["nx"]), nz = J.Int(g["nz"]);
            Assert.AreEqual(J.Int(g["vertexCount"]), m.VertexCount, "정점 수(SIMPLE_BG = 벽 없음 · 셀당 6)");
            Assert.AreEqual(nx * nz * 6, m.VertexCount);
            var s = J.Obj(g["sum"]);
            Assert.AreEqual(J.Num(s["x"]), Sum(m.Positions, 0, 3), 1e-6, "Σx");
            Assert.AreEqual(J.Num(s["y"]), Sum(m.Positions, 1, 3), 1e-6, "Σy");
            Assert.AreEqual(J.Num(s["z"]), Sum(m.Positions, 2, 3), 1e-6, "Σz");
            Assert.AreEqual(J.Num(s["ny"]), Sum(m.Normals, 1, 3), 1e-9, "Σny");
            Assert.AreEqual(J.Num(s["u"]), Sum(m.Uvs, 0, 2), 1e-3, "Σu(정본은 Float32)");
            Assert.AreEqual(J.Num(s["v"]), Sum(m.Uvs, 1, 2), 1e-3, "Σv");
            Assert.AreEqual(J.Num(s["r"]), Sum(m.Colors, 0, 3), 0.01, "Σr(정본은 Float32)");
            Assert.AreEqual(J.Num(s["g"]), Sum(m.Colors, 1, 3), 0.01, "Σg");
            Assert.AreEqual(J.Num(s["b"]), Sum(m.Colors, 2, 3), 0.01, "Σb");
            var cells = J.Arr(g["cells"]);
            Assert.Greater(cells.Count, 100);
            foreach (var co in cells)
            {
                var c = J.Obj(co);
                int ix = J.Int(c["ix"]), iz = J.Int(c["iz"]);
                int vtx = (ix * nz + iz) * 6;
                string w = "셀(" + ix + "," + iz + ") ";
                Assert.AreEqual(J.Num(c["x"]), m.Positions[vtx * 3], 1e-9, w + "x");
                Assert.AreEqual(J.Num(c["y"]), m.Positions[vtx * 3 + 1], 1e-9, w + "y");
                Assert.AreEqual(J.Num(c["z"]), m.Positions[vtx * 3 + 2], 1e-9, w + "z");
                Assert.AreEqual(J.Num(c["u"]), m.Uvs[vtx * 2], 1e-6, w + "u");
                Assert.AreEqual(J.Num(c["v"]), m.Uvs[vtx * 2 + 1], 1e-6, w + "v");
                Assert.AreEqual(J.Num(c["r"]), m.Colors[vtx * 3], 1e-6, w + "r");
                Assert.AreEqual(J.Num(c["g"]), m.Colors[vtx * 3 + 1], 1e-6, w + "g");
                Assert.AreEqual(J.Num(c["b"]), m.Colors[vtx * 3 + 2], 1e-6, w + "b");
            }
            // 노면 회랑 표식: |z| < 2.25 인 셀만
            int roadCount = 0;
            for (int i = 0; i < m.VertexCount; i++) if (m.Road[i]) roadCount++;
            Assert.AreEqual(6 * nx * 6, roadCount, "노면 회랑 = z 셀 6줄(−2.25~2.25) × 정점 6");
        }

        [Test]
        public void 높이_함수는_배경_제거_모드에서_0_이고_아니면_단으로_내린다()
        {
            var d = WorldVectors.Defs;
            Assert.AreEqual(0, GroundGrid.HeightAt(d, 3.3, -9.1), 1e-12, "SIMPLE_BG → 완전 평면");
            double hs = GroundGrid.HeightSmooth(3.3, -9.1);
            Assert.Greater(hs, 0, "뒤쪽 능선 대역은 양수");
            var full = SceneDefs.From(MiniJson.ParseObject(File.ReadAllText(Path.Combine(DataDir.Path, SceneDefs.File))));
            full.SimpleBg = false;
            double q = GroundGrid.HeightAt(full, 3.3, -9.1);
            Assert.AreEqual(0, (q / full.VoxStep) % 1, 1e-9, "단(step) 배수로 양자화");
            Assert.LessOrEqual(q, GroundGrid.HeightSmooth((Math.Floor(3.3 / full.VoxCell) + 0.5) * full.VoxCell, (Math.Floor(-9.1 / full.VoxCell) + 0.5) * full.VoxCell) + 1e-12, "floor 라 원곡선 아래");
        }

        static double Sum(List<double> a, int k, int stride)
        {
            double s = 0;
            for (int i = k; i < a.Count; i += stride) s += a[i];
            return s;
        }
    }
}
