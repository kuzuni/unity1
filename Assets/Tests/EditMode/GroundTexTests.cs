using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.World;

namespace Forge.Tests
{
    /// <summary>
    /// T34 — 지면 소재 굽기 ↔ 정본. 기대값은 `Assets/Tests/EditMode/Vectors/t34-ground.json`(`tools/ground_vectors.js` 가 정본
    /// `makeGroundTexture`·`makeGroundNormalMap`·`makeCrackTexture`·포석 줄눈 블록을 픽셀 버퍼 캔버스 시밍 위에서 **실제로 실행**한 것 ·
    /// xorshift32 시드 0x2f6e2b1). C# `GroundTexBake` 는 같은 시드로 **바이트 해시까지** 같아야 한다 — 표본·행 합은 어긋난 자리를 알리는 용도.
    /// `FORGE_GROUND_DUMP=<폴더>` 를 주면 구운 RGBA 를 `<이름>.rgba` 로 떨군다(눈 확인용 · 레포에 안 넣는다).
    /// </summary>
    static class GroundVectors
    {
        static JsonObject _doc;
        public static JsonObject Doc
        {
            get
            {
                if (_doc != null) return _doc;
                string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
                string file = Path.Combine(root, "Assets", "Tests", "EditMode", "Vectors", "t34-ground.json");
                if (!File.Exists(file)) throw new FileNotFoundException("T34 벡터가 없다 — node tools/ground_vectors.js 로 뽑는다: " + file);
                _doc = MiniJson.ParseObject(File.ReadAllText(file));
                return _doc;
            }
        }
        public static uint Seed { get { return J.UInt(Doc["seed"]); } }
        public static SceneDefs Defs { get { return WorldVectors.Defs; } }

        public static string Fnv1a64(byte[] bytes)
        {
            ulong h = 0xcbf29ce484222325UL;
            for (int i = 0; i < bytes.Length; i++) { h ^= bytes[i]; h *= 0x100000001b3UL; }
            return h.ToString("x16");
        }

        public static JsonObject Texture(string name)
        {
            foreach (object o in J.Arr(Doc["textures"])) { var t = J.Obj(o); if (J.Str(t["name"]) == name) return t; }
            throw new KeyNotFoundException("벡터에 없는 텍스처: " + name);
        }

        /// <summary>바이트 배열을 벡터 한 장과 대조 — 해시가 같으면 끝 · 다르면 표본·행 합으로 어디가 다른지 말한다.</summary>
        public static void AssertSame(string name, byte[] got, int w, int h)
        {
            var t = Texture(name);
            Assert.AreEqual(J.Int(t["w"]), w, name + " w");
            Assert.AreEqual(J.Int(t["h"]), h, name + " h");
            Assert.AreEqual(w * h * 4, got.Length, name + " bytes");
            string dump = Environment.GetEnvironmentVariable("FORGE_GROUND_DUMP");
            if (!string.IsNullOrEmpty(dump)) { Directory.CreateDirectory(dump); File.WriteAllBytes(Path.Combine(dump, name + "." + w + "x" + h + ".rgba"), got); }
            string hash = Fnv1a64(got);
            if (hash == J.Str(t["hash"])) return;
            // 진단
            var samples = J.Arr(t["samples"]);
            int bad = 0, maxd = 0; string first = null;
            foreach (object o in samples)
            {
                int[] s = J.IntArr(o);
                int i = (s[1] * w + s[0]) * 4;
                int d = Math.Max(Math.Max(Math.Abs(got[i] - s[2]), Math.Abs(got[i + 1] - s[3])), Math.Max(Math.Abs(got[i + 2] - s[4]), Math.Abs(got[i + 3] - s[5])));
                if (d > 0) { bad++; maxd = Math.Max(maxd, d); if (first == null) first = "(" + s[0] + "," + s[1] + ") 기대 " + s[2] + "," + s[3] + "," + s[4] + "," + s[5] + " 실제 " + got[i] + "," + got[i + 1] + "," + got[i + 2] + "," + got[i + 3]; }
            }
            double[] rows = J.NumArr(t["rows"]);
            int badRows = 0, firstRow = -1;
            for (int y = 0; y < h; y++)
            {
                long sum = 0; for (int x = 0; x < w; x++) { int i = (y * w + x) * 4; sum += got[i] + got[i + 1] + got[i + 2] + got[i + 3]; }
                if (sum != (long)rows[y]) { badRows++; if (firstRow < 0) firstRow = y; }
            }
            long total = 0; for (int i = 0; i < got.Length; i++) total += got[i];
            Assert.Fail(name + ": 해시 다름 " + hash + " ≠ " + t["hash"] + " · 표본 " + bad + "/" + samples.Count + " 다름(최대 " + maxd + ") 첫 " + first
                + " · 행 " + badRows + "/" + h + " 다름(첫 행 " + firstRow + ") · 바이트 합 " + total + " vs " + t["sum"]);
        }
    }

    public class GroundTexCanvasTests
    {
        [Test]
        public void Clamp8_은_Uint8ClampedArray_규칙_짝수_반올림()
        {
            Assert.AreEqual(0, GroundTexCanvas.Clamp8(-3));
            Assert.AreEqual(255, GroundTexCanvas.Clamp8(300));
            Assert.AreEqual(2, GroundTexCanvas.Clamp8(2.5));
            Assert.AreEqual(4, GroundTexCanvas.Clamp8(3.5));
            Assert.AreEqual(3, GroundTexCanvas.Clamp8(3.4999));
            Assert.AreEqual(4, GroundTexCanvas.Clamp8(3.5001));
            Assert.AreEqual(0, GroundTexCanvas.Clamp8(double.NaN));
        }

        [Test]
        public void 픽셀당_4표본_덮임과_source_over()
        {
            var c = new GroundTexCanvas(4, 4);
            c.FillStyle = GroundTexCanvas.Rgba.Hex(0xffffff);
            c.FillRect(0, 0, 4, 4);
            c.FillStyle = GroundTexCanvas.Rgba.Bytes(0, 0, 0, 1);
            c.FillRect(0.5, 0, 1, 4);        // 픽셀 0 의 오른쪽 절반 + 픽셀 1 의 왼쪽 절반
            byte[] d = c.GetImageData();
            Assert.AreEqual(128, d[0]);      // 0.5 덮임 → 흰 0.5
            Assert.AreEqual(128, d[4]);
            Assert.AreEqual(255, d[8]);
            Assert.AreEqual(255, d[3]);
            c.DestinationOut = true;
            c.FillStyle = GroundTexCanvas.Rgba.Bytes(0, 0, 0, 1);
            c.FillRect(2, 0, 2, 4);
            d = c.GetImageData();
            Assert.AreEqual(0, d[8 + 3]);    // 지워짐
            Assert.AreEqual(255, d[4 + 3]);
        }

        [Test]
        public void 둥근_캡_선과_타원()
        {
            var c = new GroundTexCanvas(16, 16);
            c.StrokeStyle = GroundTexCanvas.Rgba.Hex(0xff0000);
            c.LineWidth = 4; c.LineCapRound = true;
            c.BeginPath(); c.MoveTo(4, 8); c.LineTo(12, 8); c.Stroke();
            byte[] d = c.GetImageData();
            Assert.AreEqual(255, d[(8 * 16 + 8) * 4 + 3], "선 위");
            Assert.AreEqual(255, d[(8 * 16 + 2) * 4 + 3], "둥근 캡 안(x 2.5 는 4−2 안)");
            Assert.AreEqual(0, d[(8 * 16 + 0) * 4 + 3], "캡 밖");
            Assert.AreEqual(0, d[(2 * 16 + 8) * 4 + 3], "폭 밖");
            var e = new GroundTexCanvas(16, 16);
            e.FillStyle = GroundTexCanvas.Rgba.Hex(0x00ff00);
            e.BeginPath(); e.Ellipse(8, 8, 6, 2, 0); e.Fill();
            d = e.GetImageData();
            Assert.AreEqual(255, d[(8 * 16 + 3) * 4 + 3], "장축 안");
            Assert.AreEqual(0, d[(4 * 16 + 8) * 4 + 3], "단축 밖");
        }
    }

    /// <summary>T34 — 정본 실행 벡터와 바이트 해시 대조.</summary>
    public class GroundTexTests
    {
        static readonly string[] Kins = { "forest", "desert", "rock", "snow", "lava", "magic" };
        static readonly string[] Tinted = { "marsh", "salt", "ash", "glacier", "obsidian", "sanctum" };

        static List<CrackSeg> _net;
        static List<CrackSeg> Net { get { return _net ?? (_net = GroundTexBake.CrackNetwork(Rng.Xorshift(GroundVectors.Seed))); } }
        static GroundTexBake Bake { get { return new GroundTexBake(GroundVectors.Defs); } }

        [Test]
        public void 균열_그물은_정본과_같다_주혈관2_지류_실핏줄()
        {
            var exp = J.Arr(GroundVectors.Doc["crackNet"]);
            Assert.AreEqual(exp.Count, Net.Count, "가닥 수");
            int mains = 0;
            for (int i = 0; i < exp.Count; i++)
            {
                var e = J.Obj(exp[i]);
                Assert.AreEqual(J.Int(e["depth"]), Net[i].Depth, "depth #" + i);
                if (Net[i].Depth == 0) mains++;
                var pts = J.Arr(e["pts"]);
                Assert.AreEqual(pts.Count, Net[i].Pts.Count, "점 수 #" + i);
                for (int k = 0; k < pts.Count; k++)
                {
                    double[] p = J.NumArr(pts[k]);
                    Assert.AreEqual(p[0], Net[i].Pts[k][0], 1e-12, "#" + i + "." + k + " x");
                    Assert.AreEqual(p[1], Net[i].Pts[k][1], 1e-12, "#" + i + "." + k + " y");
                }
            }
            Assert.AreEqual(2, mains);
        }

        [Test]
        public void 알베도_kin_6종_512_해시_일치()
        {
            foreach (string kin in Kins)
                GroundVectors.AssertSame("albedo_" + kin, Bake.Albedo(kin, Net, Rng.Xorshift(GroundVectors.Seed)), GroundTexBake.AlbedoSize, GroundTexBake.AlbedoSize);
        }

        [Test]
        public void 알베도_신설_바이옴_틴트_6종_해시_일치()
        {
            foreach (string b in Tinted)
            {
                Assert.IsNotNull(GroundVectors.Defs.Spec(b).Tint, b + " tint");
                GroundVectors.AssertSame("albedo_" + b, Bake.Albedo(b, Net, Rng.Xorshift(GroundVectors.Seed)), GroundTexBake.AlbedoSize, GroundTexBake.AlbedoSize);
            }
        }

        [Test]
        public void 노멀맵_12종_256_해시_일치()
        {
            foreach (string b in Kins) GroundVectors.AssertSame("normal_" + b, Bake.NormalMap(b, Net, Rng.Xorshift(GroundVectors.Seed)), GroundTexBake.NormalSize, GroundTexBake.NormalSize);
            foreach (string b in Tinted) GroundVectors.AssertSame("normal_" + b, Bake.NormalMap(b, Net, Rng.Xorshift(GroundVectors.Seed)), GroundTexBake.NormalSize, GroundTexBake.NormalSize);
        }

        [Test]
        public void 용암_발광_균열맵_해시_일치()
        {
            GroundVectors.AssertSame("crack", Bake.CrackTexture(Net), GroundTexBake.CrackSize, GroundTexBake.CrackSize);
        }

        [Test]
        public void 포석_줄눈_데칼_네_변_알파_0_해시_일치()
        {
            byte[] d = GroundTexBake.CobbleDecal(Rng.Xorshift(GroundVectors.Seed));
            int W = GroundTexBake.CobbleWidth, H = GroundTexBake.CobbleHeight;
            for (int x = 0; x < W; x++) { Assert.AreEqual(0, d[x * 4 + 3], "윗변 x=" + x); Assert.AreEqual(0, d[((H - 1) * W + x) * 4 + 3], "아랫변 x=" + x); }
            GroundVectors.AssertSame("cobble", d, W, H);
        }

        [Test]
        public void 원본_6종은_틴트가_한_픽셀도_안_바꾼다()
        {
            var bake = Bake;
            byte[] a = bake.Albedo("forest", Net, Rng.Xorshift(GroundVectors.Seed));
            byte[] b = (byte[])a.Clone();
            bake.TintGround(b, "forest");
            Assert.AreEqual(GroundVectors.Fnv1a64(a), GroundVectors.Fnv1a64(b));
        }
    }
}
