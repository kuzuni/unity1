using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Voxel;

namespace Forge.Tests
{
    /// <summary>T4 벡터(`Assets/Tests/EditMode/Vectors/t4-voxel.json` · `tools/voxel_vectors.js` 가 정본 voxel.js·mobs.js 를 실물 three r128 위에서 돌려 뽑은 것).</summary>
    static class VoxelVectors
    {
        static JsonObject _doc;
        public static JsonObject Doc
        {
            get
            {
                if (_doc != null) return _doc;
                string dataDir = DataDir.Path; // <root>/Assets/StreamingAssets/data
                string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(dataDir)));
                string file = Path.Combine(root, "Assets", "Tests", "EditMode", "Vectors", "t4-voxel.json");
                if (!File.Exists(file)) throw new FileNotFoundException("T4 벡터가 없다 — node tools/voxel_vectors.js 로 뽑는다: " + file);
                _doc = MiniJson.ParseObject(File.ReadAllText(file));
                return _doc;
            }
        }

        public static MobTable Table(string k)
        {
            var g = DataDir.Game;
            switch (k)
            {
                case "pets": return g.Pets;
                case "mounts": return g.Mounts;
                case "enemies": return g.Enemies;
                case "skillfx": return g.SkillFx;
            }
            throw new ArgumentException(k);
        }

        /// <summary>voxel_vectors.js 의 `shapes` 와 같은 모양.</summary>
        public static List<VoxelCell> Shape(string name)
        {
            switch (name)
            {
                case "single": return new List<VoxelCell> { new VoxelCell(0, 0, 0, -1) };
                case "pair": return new List<VoxelCell> { new VoxelCell(0, 0, 0, -1), new VoxelCell(1, 0, 0, -1) };
                case "box222": return Voxel.Box(2, 2, 2, 0xff0000);
                case "box333": return Voxel.Box(3, 3, 3, 0x336699);
                case "L": return new List<VoxelCell> { new VoxelCell(0, 0, 0, -1), new VoxelCell(1, 0, 0, -1), new VoxelCell(0, 1, 0, -1) };
                case "cup": return new List<VoxelCell> { new VoxelCell(0, 0, 0, -1), new VoxelCell(1, 0, 1, -1), new VoxelCell(0, 1, 1, -1) };
                case "bar411": return Voxel.Box(4, 1, 1, 0x808080);
                case "slab313": return Voxel.Box(3, 1, 3, 0x123456);
                case "stair":
                    return new List<VoxelCell>
                    {
                        new VoxelCell(0, 0, 0, 1), new VoxelCell(1, 0, 0, 2), new VoxelCell(1, 1, 0, 3), new VoxelCell(2, 1, 0, 4),
                        new VoxelCell(2, 1, 1, 5), new VoxelCell(1, 0, 1, 6), new VoxelCell(0, -1, 1, 7),
                    };
            }
            throw new ArgumentException(name);
        }
    }

    /// <summary>T4 — 정본 `test-voxel.js` ①~④ 를 그대로 + 벡터 대조(면 · AO · jitter · aoShade).</summary>
    public class VoxelFacesTests
    {
        static int MinAo(List<VoxelFace> f) { int m = 9; foreach (var x in f) foreach (var a in x.Ao) if (a < m) m = a; return m; }

        [Test]
        public void 면_제거_수()
        {
            Assert.AreEqual(6, Voxel.Faces(VoxelVectors.Shape("single"), 0).Count, "단일 복셀은 6면");
            Assert.AreEqual(10, Voxel.Faces(VoxelVectors.Shape("pair"), 0).Count, "붙은 복셀 2개는 10면");
            Assert.AreEqual(24, Voxel.Faces(Voxel.Box(2, 2, 2, 0), 0).Count, "2×2×2 는 24면");
            var f333 = Voxel.Faces(Voxel.Box(3, 3, 3, 0), 0);
            Assert.AreEqual(54, f333.Count, "3×3×3 은 54면");
            Assert.AreEqual(0, f333.FindAll(f => f.Vx == 1 && f.Vy == 1 && f.Vz == 1).Count, "정중앙 복셀은 0면");
            Assert.AreEqual(384, Voxel.Faces(Voxel.Box(8, 8, 8, 0), 0).Count, "8×8×8 은 384면");
        }

        [Test]
        public void 이음새_AO()
        {
            Assert.AreEqual(3, MinAo(Voxel.Faces(VoxelVectors.Shape("single"), 0)), "외톨이 복셀은 AO 전부 3");
            Assert.AreEqual(2, MinAo(Voxel.Faces(VoxelVectors.Shape("L"), 0)), "평면 L 은 최소 AO 2");
            var cup = Voxel.Faces(VoxelVectors.Shape("cup"), 0).FindAll(x => x.Vx == 0 && x.Vy == 0 && x.Vz == 0 && x.N[2] == 1);
            Assert.AreEqual(0, MinAo(cup), "앞층에서 두 옆이 막히면 그 코너는 AO 0");
            Assert.Greater(MinAo(Voxel.Faces(Voxel.Box(4, 1, 1, 0), 0)), 0, "곧은 막대에는 AO 0 이 없다");
            var top = Voxel.Faces(Voxel.Box(3, 1, 3, 0), 0).FindAll(x => x.N[1] == 1);
            Assert.AreEqual(3, MinAo(top), "평평한 판의 윗면 AO 는 전부 3");
        }

        [Test]
        public void AO_밝기_계수()
        {
            double s0 = Voxel.AoShade(0), s1 = Voxel.AoShade(1), s2 = Voxel.AoShade(2), s3 = Voxel.AoShade(3);
            Assert.IsTrue(s0 < s1 && s1 < s2 && s2 < s3, "단조 증가");
            Assert.GreaterOrEqual(s0, 0.4, "가장 어두운 AO 도 0.4 이상");
            Assert.AreEqual(1, s3, 1e-12, "AO 3 은 원색 그대로");
            Assert.AreEqual(0.62, s0, 1e-12);
        }

        [Test]
        public void 색변화는_좌표_해시라_재현된다()
        {
            double a = Voxel.Jitter(3, 7, 11, 0.06), b = Voxel.Jitter(3, 7, 11, 0.06), c = Voxel.Jitter(4, 7, 11, 0.06);
            Assert.AreEqual(a, b);
            Assert.AreNotEqual(a, c);
            double mn = 9, mx = -9;
            for (int x = 0; x < 12; x++) for (int y = 0; y < 12; y++) { double v = Voxel.Jitter(x, y, 0, 0.06); if (v < mn) mn = v; if (v > mx) mx = v; }
            Assert.IsTrue(mn >= 0.94 - 1e-9 && mx <= 1.06 + 1e-9, mn + " ~ " + mx);
        }

        [Test]
        public void 모든_면이_법선축에_대해_평평하다()
        {
            foreach (var x in Voxel.Faces(Voxel.Box(2, 2, 2, 0), 0))
            {
                int ax = x.N[0] != 0 ? 0 : x.N[1] != 0 ? 1 : 2;
                foreach (var c in x.Corners) Assert.AreEqual(x.Corners[0][ax], c[ax], 1e-9);
            }
        }

        [Test]
        public void 벡터_faces_아홉_모양이_정본과_같다()
        {
            var faces = J.Obj(VoxelVectors.Doc["faces"]);
            Assert.AreEqual(9, faces.Count);
            foreach (var kv in faces)
            {
                var want = J.Arr(kv.Value);
                var got = Voxel.Faces(VoxelVectors.Shape(kv.Key), 0xabcdef);
                Assert.AreEqual(want.Count, got.Count, kv.Key + " 면 수");
                for (int i = 0; i < want.Count; i++)
                {
                    var w = J.Obj(want[i]);
                    var g = got[i];
                    string at = kv.Key + " 면 " + i;
                    Assert.AreEqual(J.IntArr(w["n"]), g.N, at + " n");
                    Assert.AreEqual(J.IntArr(w["ao"]), g.Ao, at + " ao");
                    Assert.AreEqual(J.Int(w["c"]), g.C, at + " c");
                    Assert.AreEqual(J.IntArr(w["v"]), new[] { g.Vx, g.Vy, g.Vz }, at + " v");
                    var corners = J.Arr(w["corners"]);
                    for (int c = 0; c < 4; c++) Assert.AreEqual(J.NumArr(corners[c]), g.Corners[c], at + " corner " + c);
                }
            }
        }

        [Test]
        public void 벡터_jitter_aoShade()
        {
            var jit = J.Arr(VoxelVectors.Doc["jitter"]);
            Assert.Greater(jit.Count, 500);
            foreach (var row in jit)
            {
                var a = J.NumArr(row);
                Assert.AreEqual(a[4], Voxel.Jitter((int)a[0], (int)a[1], (int)a[2], a[3]), 1e-6, "jitter(" + a[0] + "," + a[1] + "," + a[2] + "," + a[3] + ")");
            }
            foreach (var row in J.Arr(VoxelVectors.Doc["aoShade"]))
            {
                var a = J.NumArr(row);
                Assert.AreEqual(a[2], Voxel.AoShade((int)a[0], a[1]), 1e-6);
            }
        }

        [Test]
        public void 조립_유틸()
        {
            var b = Voxel.Box(2, 3, 4, 7);
            Assert.AreEqual(24, b.Count);
            Assert.AreEqual(new VoxelCell(0, 0, 1, 7), b[1], "x→y→z 중첩 순서");
            var moved = Voxel.At(b, 1, -2, 3);
            Assert.AreEqual(new VoxelCell(1, -2, 3, 7), moved[0]);
            Assert.AreEqual(new VoxelCell(0, 0, 0, 7), b[0], "입력은 안 고친다");
            Assert.AreEqual(26, Voxel.Hollow(Voxel.Box(3, 3, 3, 1)).Count, "hollow(3×3×3) 은 26칸");
            var m = Voxel.MirrorX(new List<VoxelCell> { new VoxelCell(2, 1, 1, 5) }, 0);
            Assert.AreEqual(-2, m[0].X);
            var r = Voxel.RotY(new List<VoxelCell> { new VoxelCell(1, 0, 0, 5) }, 1);
            Assert.AreEqual(new VoxelCell(0, 0, -1, 5), r[0], "rotY 1 회: (x,y,z) → (z,y,−x)");
            Assert.AreEqual(new VoxelCell(1, 0, 0, 5), Voxel.RotY(r, 3)[0], "4회면 제자리");
            Assert.AreEqual(new VoxelCell(1, 0, 0, 5), Voxel.RotY(new List<VoxelCell> { new VoxelCell(1, 0, 0, 5) }, -4)[0]);
            VoxelBounds bb;
            Assert.IsTrue(Voxel.Bounds(moved, out bb));
            Assert.AreEqual(2, bb.W); Assert.AreEqual(3, bb.H); Assert.AreEqual(4, bb.D); Assert.AreEqual(-2, bb.Y0);
            Assert.AreEqual(0xff0000, Voxel.ScaleHex(0xff0000, 2));
            Assert.AreEqual(0x808080, Voxel.ScaleHex(0xffffff, 0.5), "255×0.5 = 127.5 → 128(JS round)");
            var rc = Voxel.Recolor(b, (v, i) => i == 0 ? 9 : -1);
            Assert.AreEqual(9, rc[0].C); Assert.AreEqual(7, rc[1].C);
        }
    }

    /// <summary>T4 — `VoxelGeometry`: 정점 4/면 · 인덱스 6/면 · flip 규칙 · 왼손 좌표(z 반전 + 감김 반전).</summary>
    public class VoxelGeometryTests
    {
        static readonly VoxelBuildOptions MobOpts = new VoxelBuildOptions { Size = 0.03, Color = 0xffffff, Jitter = 0.022, Ao = 0.85, Center = true, LeftHanded = false };

        [Test]
        public void 한_칸은_24정점_36인덱스_6면()
        {
            var m = VoxelGeometry.Build(Voxel.Box(1, 1, 1, 0x336699), MobOpts);
            Assert.AreEqual(6, m.FaceCount);
            Assert.AreEqual(24, m.VertexCount);
            Assert.AreEqual(36, m.Triangles.Length);
            Assert.AreEqual(new[] { 0.0, 0.0, 0.0 }, m.Center, "중심 정렬");
            for (int i = 0; i < m.VertexCount; i++)
            {
                Assert.AreEqual(0.015, Math.Abs(m.Positions[i * 3]), 1e-6);
                Assert.AreEqual(0.015, Math.Abs(m.Positions[i * 3 + 1]), 1e-6);
                Assert.AreEqual(0.015, Math.Abs(m.Positions[i * 3 + 2]), 1e-6);
            }
            var m2 = VoxelGeometry.Build(Voxel.Box(2, 2, 2, 0x336699), MobOpts);
            Assert.AreEqual(24 * 4, m2.VertexCount);
        }

        static double[] Cross(double[] a, double[] b) { return new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] }; }

        static void AssertWinding(VoxelMesh m)
        {
            for (int t = 0; t < m.Triangles.Length; t += 3)
            {
                int i0 = m.Triangles[t], i1 = m.Triangles[t + 1], i2 = m.Triangles[t + 2];
                double[] a = { m.Positions[i0 * 3], m.Positions[i0 * 3 + 1], m.Positions[i0 * 3 + 2] };
                double[] b = { m.Positions[i1 * 3] - a[0], m.Positions[i1 * 3 + 1] - a[1], m.Positions[i1 * 3 + 2] - a[2] };
                double[] c = { m.Positions[i2 * 3] - a[0], m.Positions[i2 * 3 + 1] - a[1], m.Positions[i2 * 3 + 2] - a[2] };
                var n = Cross(b, c);
                double dot = n[0] * m.Normals[i0 * 3] + n[1] * m.Normals[i0 * 3 + 1] + n[2] * m.Normals[i0 * 3 + 2];
                Assert.Greater(dot, 0, "삼각형 " + t / 3 + " 의 감김이 법선과 반대다");
            }
        }

        [Test]
        public void 감김은_법선과_같은_쪽이다_오른손_왼손_모두()
        {
            var cells = VoxelVectors.Shape("stair");
            var rh = VoxelGeometry.Build(cells, MobOpts);
            AssertWinding(rh);
            var lo = MobOpts; lo.LeftHanded = true;
            var lh = VoxelGeometry.Build(cells, lo);
            AssertWinding(lh);
            Assert.AreEqual(rh.VertexCount, lh.VertexCount);
            for (int i = 0; i < rh.VertexCount; i++)
            {
                Assert.AreEqual(rh.Positions[i * 3], lh.Positions[i * 3]);
                Assert.AreEqual(rh.Positions[i * 3 + 1], lh.Positions[i * 3 + 1]);
                Assert.AreEqual(-rh.Positions[i * 3 + 2], lh.Positions[i * 3 + 2], "z 반전");
                Assert.AreEqual(-rh.Normals[i * 3 + 2], lh.Normals[i * 3 + 2]);
                Assert.AreEqual(rh.Colors[i * 3], lh.Colors[i * 3], "색은 그대로");
            }
        }

        [Test]
        public void flip_규칙_AO_낮은_코너_쪽으로_가른다()
        {
            // cup 의 (0,0,0) +z 면: 코너 AO 가 다르므로 flip 이 정해진다 — 인덱스 순서가 정본 order 와 같은지 본다.
            var cells = VoxelVectors.Shape("cup");
            var faces = Voxel.Faces(cells, 0xabcdef);
            var m = VoxelGeometry.Build(cells, MobOpts);
            for (int f = 0; f < faces.Count; f++)
            {
                var F = faces[f];
                bool flip = (F.Ao[0] + F.Ao[2]) < (F.Ao[1] + F.Ao[3]);
                int[] order = flip ? new[] { 1, 2, 3, 1, 3, 0 } : new[] { 0, 1, 2, 0, 2, 3 };
                for (int k = 0; k < 6; k++) Assert.AreEqual(f * 4 + order[k], m.Triangles[f * 6 + k]);
            }
        }

        [Test]
        public void 정점_색은_칸색_AO_jitter_곱이다()
        {
            var m = VoxelGeometry.Build(new List<VoxelCell> { new VoxelCell(2, 3, 4, 0x80ff00) }, new VoxelBuildOptions { Size = 1, Color = 0, Jitter = 0.022, Ao = 0.85, Center = false });
            double jf = Voxel.Jitter(2, 3, 4, 0.022);
            Assert.AreEqual((float)(128 / 255.0 * jf), m.Colors[0], 1e-7);
            Assert.AreEqual((float)(1.0 * jf), m.Colors[1], 1e-7);
            Assert.AreEqual(0f, m.Colors[2]);
            Assert.AreEqual(2.5f, m.Positions[0], 1e-7, "center=false 면 칸 좌표 그대로(+x 면 첫 코너 x = 2 + 0.5)");
        }
    }

    /// <summary>T4 — `MobPainter`: 음수 인덱스 · mx 거울 · 규칙 순서 + 정본 paint 체크섬(파츠 1110).</summary>
    public class MobPainterTests
    {
        static MobPaint Rule(string json) { return MobPaint.From(MiniJson.ParseObject(json)); }

        [Test]
        public void span_음수는_뒤에서()
        {
            Assert.AreEqual(new[] { 0, 4 }, MobPainter.Span(null, 5));
            Assert.AreEqual(new[] { 4, 4 }, MobPainter.Span(PaintAxis.From(-1.0), 5));
            Assert.AreEqual(new[] { 1, 3 }, MobPainter.Span(PaintAxis.From(new List<object> { 1.0, -2.0 }), 5));
            Assert.AreEqual(new[] { 2, 2 }, MobPainter.Span(PaintAxis.From(2.0), 5));
        }

        [Test]
        public void mx_거울과_음수_인덱스()
        {
            // Snail 머리: box [5,4,5] · 눈 흰자 {x:[1,1], y:[1,1], z:-1, mx} → x=1 과 x=3 · z=4 · y=1
            var cells = Voxel.Box(5, 4, 5, 0xe8d2ae);
            var rules = MobPainter.Expand(new List<MobPaint> { Rule("{\"c\":16052454,\"x\":[1,1],\"y\":[1,1],\"z\":-1,\"mx\":true}") }, 5);
            Assert.AreEqual(2, rules.Count, "expand 가 거울 규칙을 한 줄 더 만든다");
            Assert.AreEqual(3, rules[1].X.Lo); Assert.AreEqual(3, rules[1].X.Hi); Assert.IsFalse(rules[1].Mx);
            var painted = MobPainter.Paint(cells, 5, 4, 5, rules);
            int n = 0;
            foreach (var v in painted)
            {
                bool eye = v.Y == 1 && v.Z == 4 && (v.X == 1 || v.X == 3);
                Assert.AreEqual(eye ? 16052454 : 0xe8d2ae, v.C, v.ToString());
                if (eye) n++;
            }
            Assert.AreEqual(2, n);
            Assert.AreEqual(0xe8d2ae, cells[0].C, "입력은 안 고친다");
        }

        [Test]
        public void 뒤_규칙이_앞_규칙을_덮는다()
        {
            var cells = Voxel.Box(2, 2, 2, 1);
            var painted = MobPainter.Paint(cells, 2, 2, 2, new List<MobPaint> { Rule("{\"c\":5,\"y\":0}"), Rule("{\"c\":6,\"y\":0,\"x\":1}") });
            foreach (var v in painted) Assert.AreEqual(v.Y == 0 ? (v.X == 1 ? 6 : 5) : 1, v.C, v.ToString());
        }

        static long Hash(List<VoxelCell> cells)
        {
            long h = 7;
            for (int i = 0; i < cells.Count; i++) h = (h * 31 + (uint)cells[i].C) % 1000000007L;
            return h;
        }

        [Test]
        public void 벡터_paint_체크섬_모든_파츠()
        {
            var paint = J.Obj(VoxelVectors.Doc["paint"]);
            int parts = 0;
            foreach (var tk in paint)
            {
                var table = VoxelVectors.Table(tk.Key);
                var models = J.Obj(tk.Value);
                Assert.AreEqual(table.Count, models.Count, tk.Key + " 종 수");
                foreach (var mk in models)
                {
                    var model = table[mk.Key];
                    var rows = J.Arr(mk.Value);
                    Assert.AreEqual(model.Parts.Count, rows.Count, tk.Key + "/" + mk.Key + " 파츠 수");
                    for (int i = 0; i < rows.Count; i++)
                    {
                        var p = model.Parts[i];
                        if (rows[i] == null) { Assert.IsTrue(MobBuilder.IsOff(p)); continue; }
                        var want = J.NumArr(rows[i]);
                        var cells = MobPainter.CellsOf(p, p.C);
                        string at = tk.Key + "/" + mk.Key + " #" + i + "(" + p.Id + ")";
                        Assert.AreEqual((int)want[0], cells.Count, at + " 칸 수");
                        Assert.AreEqual((long)want[1], Hash(cells), at + " 색 체크섬");
                        parts++;
                    }
                }
            }
            Assert.AreEqual(1110, parts);
        }
    }

    /// <summary>T4 — `ColorHsl.Vivid`: three r128 getHSL/setHSL/getHex 와 같은 값(표 색 전부 × vivid 3종).</summary>
    public class ColorHslTests
    {
        [Test]
        public void 벡터_vivid()
        {
            var rows = J.Arr(VoxelVectors.Doc["vivid"]);
            Assert.Greater(rows.Count, 1000);
            foreach (var row in rows)
            {
                var a = J.NumArr(row);
                Assert.AreEqual((int)a[2], ColorHsl.Vivid((int)a[0], a[1]), "vivid(#" + ((int)a[0]).ToString("x6") + ", " + a[1] + ")");
            }
        }

        [Test]
        public void 회색은_안_건드리고_vivid_0은_그대로()
        {
            Assert.AreEqual(0x808080, ColorHsl.Vivid(0x808080, 0.2));
            Assert.AreEqual(0xe8d2ae, ColorHsl.Vivid(0xe8d2ae, 0));
            double h, s, l;
            ColorHsl.GetHsl(0xe8d2ae, out h, out s, out l);
            Assert.AreEqual(0.10344827586206894, h, 1e-12);
            Assert.AreEqual(0.5576923076923075, s, 1e-12);
            Assert.AreEqual(0.7960784313725491, l, 1e-12);
            Assert.AreEqual(0xf5ddb7, ColorHsl.Vivid(0xe8d2ae, 0.2));
        }
    }

    /// <summary>T4 — `MobBuilder.Plan`: 정본 `Mobs.build`(실물 three) 와 파츠 81종 1110개 전부 대조 — 위치·회전·재질 키·정점 수·정점 합·태그 모음·관절.</summary>
    public class MobBuilderTests
    {
        static void Sums(VoxelMesh m, out double[] ps, out double[] cs, out double pw)
        {
            ps = new double[3]; cs = new double[3]; pw = 0;
            for (int j = 0; j < m.Triangles.Length; j++)
            {
                int v = m.Triangles[j] * 3;
                ps[0] += m.Positions[v]; ps[1] += m.Positions[v + 1]; ps[2] += m.Positions[v + 2];
                cs[0] += m.Colors[v]; cs[1] += m.Colors[v + 1]; cs[2] += m.Colors[v + 2];
                pw += (m.Positions[v] * 1 + m.Positions[v + 1] * 7 + m.Positions[v + 2] * 13) * (j % 5 + 1);
            }
        }

        static List<string> Pids(List<MobPartPlan> l) { var r = new List<string>(); foreach (var p in l) r.Add(p.Pid); return r; }

        [Test]
        public void 벡터_build_모든_종()
        {
            var build = J.Obj(VoxelVectors.Doc["build"]);
            int parts = 0, models = 0;
            foreach (var tk in build)
            {
                var table = VoxelVectors.Table(tk.Key);
                foreach (var mk in J.Obj(tk.Value))
                {
                    var want = J.Obj(mk.Value);
                    var model = table[mk.Key];
                    string at = tk.Key + "/" + mk.Key;
                    var plan = MobBuilder.Plan(model, 0, 0);
                    Assert.AreEqual(J.Num(want["cell"]), plan.Cell, 1e-12, at + " cell");
                    var wparts = J.Arr(want["parts"]);
                    Assert.AreEqual(wparts.Count, plan.Parts.Count, at + " 파츠 수");
                    for (int i = 0; i < wparts.Count; i++)
                    {
                        var w = J.Obj(wparts[i]);
                        var pp = plan.Parts[i];
                        string pat = at + " #" + J.Int(w["i"]) + "(" + pp.Pid + ")";
                        Assert.AreEqual(J.Int(w["i"]), pp.Index, pat + " index");
                        Assert.AreEqual(J.Str(w["pid"]), pp.Pid, pat + " pid");
                        Assert.AreEqual(J.Bool(w["hasPivot"]), pp.HasPivot, pat + " pivot 유무");
                        Assert.AreEqual(J.Str(w["matKey"]), pp.MatKey, pat + " matKey");
                        Assert.AreEqual(J.Str(w["parentPid"]), pp.ParentPlan == null ? null : pp.ParentPlan.Pid, pat + " 부모");
                        Assert.AreEqual(J.Bool(w["isHead"]), pp.IsHead, pat + " head 표식");
                        Assert.AreEqual(J.Int(w["tri"]), pp.Mesh.Triangles.Length, pat + " 정점 수(면×6)");
                        var mp = J.NumArr(w["meshPos"]);
                        for (int k = 0; k < 3; k++) Assert.AreEqual(mp[k], pp.MeshPos[k], 1e-6, pat + " meshPos[" + k + "]");
                        if (pp.HasPivot) { var pv = J.NumArr(w["pivotPos"]); for (int k = 0; k < 3; k++) Assert.AreEqual(pv[k], pp.PivotPos[k], 1e-6, pat + " pivotPos[" + k + "]"); }
                        var rot = J.NumArr(w["rot"]);
                        for (int k = 0; k < 3; k++) Assert.AreEqual(rot[k], pp.Rot[k], 1e-12, pat + " rot[" + k + "]");
                        double[] ps, cs; double pw;
                        Sums(pp.Mesh, out ps, out cs, out pw);
                        var wps = J.NumArr(w["psum"]); var wcs = J.NumArr(w["csum"]);
                        for (int k = 0; k < 3; k++) Assert.AreEqual(wps[k], ps[k], 1e-5, pat + " 위치 합[" + k + "]");
                        for (int k = 0; k < 3; k++) Assert.AreEqual(wcs[k], cs[k], 2e-3, pat + " 색 합[" + k + "]");
                        Assert.AreEqual(J.Num(w["pw"]), pw, 1e-3, pat + " 위치 가중합(순서)");
                        parts++;
                    }
                    Assert.AreEqual(J.Str(want["head"]), plan.Head == null ? null : plan.Head.Pid, at + " head");
                    Assert.AreEqual(J.Str(want["tail"]), plan.Tail == null ? null : plan.Tail.Pid, at + " tail");
                    var legs = J.Arr(want["legs"]);
                    Assert.AreEqual(legs.Count, plan.Legs.Count, at + " legs");
                    for (int i = 0; i < legs.Count; i++) { var r = J.Arr(legs[i]); Assert.AreEqual(J.Str(r[0]), plan.Legs[i].Pid); Assert.AreEqual(J.Num(r[1]), plan.Legs[i].Gait, at + " gait"); }
                    var wings = J.Arr(want["wings"]);
                    Assert.AreEqual(wings.Count, plan.Wings.Count, at + " wings");
                    for (int i = 0; i < wings.Count; i++) { var r = J.Arr(wings[i]); Assert.AreEqual(J.Str(r[0]), plan.Wings[i].Pid); Assert.AreEqual(J.Num(r[1]), plan.Wings[i].S, at + " wing s"); }
                    var claws = J.Arr(want["claws"]);
                    Assert.AreEqual(claws.Count, plan.Claws.Count, at + " claws");
                    for (int i = 0; i < claws.Count; i++) { var r = J.Arr(claws[i]); Assert.AreEqual(J.Str(r[0]), plan.Claws[i].Pid); Assert.AreEqual(J.Num(r[1]), plan.Claws[i].S, at + " claw s"); }
                    Assert.AreEqual(J.StrArr(want["wheels"]), Pids(plan.Wheels).ToArray(), at + " wheels");
                    Assert.AreEqual(J.StrArr(want["spinners"]), Pids(plan.Spinners).ToArray(), at + " spinners");
                    Assert.AreEqual(J.StrArr(want["glow"]), Pids(plan.Glow).ToArray(), at + " glow");
                    var joints = J.Arr(want["joints"]);
                    Assert.AreEqual(joints.Count, plan.Joints.Count, at + " joints");
                    for (int i = 0; i < joints.Count; i++)
                    {
                        var r = J.Arr(joints[i]); var j = plan.Joints[i];
                        string jat = at + " joint " + i;
                        Assert.AreEqual(J.Str(r[0]), j.Part.Pid, jat);
                        Assert.AreEqual(J.Str(r[1]), j.Axis, jat + " axis");
                        Assert.AreEqual(J.Num(r[2]), j.Base, 1e-6, jat + " base");
                        Assert.AreEqual(J.Num(r[3]), j.Amp, 1e-12, jat + " amp");
                        Assert.AreEqual(J.Num(r[4]), j.Ph, 1e-12, jat + " ph");
                        Assert.AreEqual(J.Num(r[5]), j.F, 1e-12, jat + " f");
                        Assert.AreEqual(J.Num(r[6]), j.Gain, 1e-12, jat + " gain");
                        Assert.AreEqual(J.Bool(r[7]), j.Abs, jat + " abs");
                        Assert.AreEqual(J.Bool(r[8]), j.Spin, jat + " spin");
                    }
                    Assert.AreEqual(J.Int(want["mats"]), plan.MatKeys.Count, at + " 재질 수");
                    models++;
                }
            }
            Assert.AreEqual(81, models, "펫 25 · 탈것 29 · 적 7 · 스킬 오브젝트 20");
            Assert.AreEqual(1110, parts);
        }

        [Test]
        public void 벡터_full_정점_배열_그대로()
        {
            var full = J.Obj(VoxelVectors.Doc["full"]);
            Assert.GreaterOrEqual(full.Count, 2);
            foreach (var kv in full)
            {
                var seg = kv.Key.Split('/');
                var model = VoxelVectors.Table(seg[0])[seg[1]];
                var plan = MobBuilder.Plan(model, 0, 0);
                MobPartPlan pp = null;
                foreach (var p in plan.Parts) if (seg[2].StartsWith("#") ? p.Index == int.Parse(seg[2].Substring(1)) : p.Source.Id == seg[2]) { pp = p; break; }
                Assert.IsNotNull(pp, kv.Key);
                var pos = J.NumArr(J.Obj(kv.Value)["pos"]); var col = J.NumArr(J.Obj(kv.Value)["col"]);
                Assert.AreEqual(pos.Length, pp.Mesh.Triangles.Length * 3, kv.Key + " 정점 수");
                for (int j = 0; j < pp.Mesh.Triangles.Length; j++)
                {
                    int v = pp.Mesh.Triangles[j] * 3;
                    for (int k = 0; k < 3; k++)
                    {
                        Assert.AreEqual(pos[j * 3 + k], pp.Mesh.Positions[v + k], 1e-6, kv.Key + " pos " + j);
                        Assert.AreEqual(col[j * 3 + k], pp.Mesh.Colors[v + k], 1e-5, kv.Key + " col " + j);
                    }
                }
            }
        }

        [Test]
        public void matKey_정본_문자열()
        {
            Assert.AreEqual("std", MobBuilder.MatKey(null));
            Assert.AreEqual("s|0.72|2789276|0.45|", MobBuilder.MatKey(MobMat.From(MiniJson.ParseObject("{\"emissive\":2789276,\"emissiveIntensity\":0.45,\"opacity\":0.72}"))));
            Assert.AreEqual("s|0.62||0|0.25", MobBuilder.MatKey(MobMat.From(MiniJson.ParseObject("{\"opacity\":0.62,\"rough\":0.25}"))));
            Assert.AreEqual("b|1||0|", MobBuilder.MatKey(MobMat.From(MiniJson.ParseObject("{\"basic\":true}"))));
            Assert.IsTrue(MobBuilder.IsBasic(MobMat.From(MiniJson.ParseObject("{\"basic\":true}"))));
            Assert.IsFalse(MobBuilder.IsBasic(MobMat.From(MiniJson.ParseObject("{\"opacity\":0.5}"))));
        }

        [Test]
        public void cell_우선순위와_vivid_와_왼손()
        {
            var g = DataDir.Game;
            var snail = g.Pets["Snail"];
            Assert.AreEqual(snail.Cell, MobBuilder.Plan(snail).Cell, "표 cell");
            Assert.AreEqual(0.05, MobBuilder.Plan(snail, 0.05).Cell, "인자가 표를 이긴다");
            var pony = g.Mounts["Pony"];
            Assert.AreEqual(0, pony.Cell);
            Assert.AreEqual(MobBuilder.DefaultCell, MobBuilder.Plan(pony).Cell, "탈것 표에는 cell 이 없어 0.03");
            var plain = MobBuilder.Plan(snail, 0, 0);
            var vivid = MobBuilder.Plan(snail, 0, 0.2);
            Assert.AreEqual(ColorHsl.Vivid(snail.Parts[0].C, 0.2), vivid.Parts[0].Color);
            Assert.AreNotEqual(plain.Parts[0].Color, vivid.Parts[0].Color);
            Assert.AreEqual(plain.Parts[0].MeshPos, vivid.Parts[0].MeshPos, "vivid 는 색만 바꾼다");
            var lh = MobBuilder.Plan(snail, 0, 0, true);
            Assert.AreEqual(-plain.Parts[1].Mesh.Positions[2], lh.Parts[1].Mesh.Positions[2], "왼손 = z 반전(메시)");
            Assert.AreEqual(plain.Parts[1].PivotPos[2], lh.Parts[1].PivotPos[2], "노드 위치는 언제나 three 값(Game 이 뒤집는다)");
            Assert.AreEqual(1, plain.ById["head"].Index);
            Assert.IsTrue(plain.ById["head"].HasPivot);
            Assert.AreSame(plain.ById["head"], plain.Head);
            Assert.AreSame(plain.Head, plain.Parts[2].ParentPlan, "parent:'head' 파츠는 head 아래");
        }
    }
}
