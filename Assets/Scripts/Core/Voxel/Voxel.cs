using System;
using System.Collections.Generic;

namespace Forge.Core.Voxel
{
    /// <summary>칸 하나 — 정본 `voxel.js` 의 `{x, y, z, c}`. <see cref="C"/> 가 음수면 «색 없음»(빌드의 기본색을 쓴다).</summary>
    public struct VoxelCell
    {
        public int X, Y, Z;
        /// <summary>0xRRGGBB · 음수 = 없음(정본의 `c === undefined`).</summary>
        public int C;

        public VoxelCell(int x, int y, int z, int c) { X = x; Y = y; Z = z; C = c; }
        public bool HasColor { get { return C >= 0; } }
        public override string ToString() { return "(" + X + "," + Y + "," + Z + (HasColor ? " #" + C.ToString("x6") : "") + ")"; }
    }

    /// <summary>면 하나 — 정본 `Voxel.faces` 의 출력 항목(`n · corners · ao · c · vx/vy/vz`).</summary>
    public sealed class VoxelFace
    {
        /// <summary>법선(축 단위 벡터 · 정본 FACES 순서 +x −x +y −y +z −z).</summary>
        public int[] N;
        /// <summary>코너 4개(칸 단위 · 반 칸 오프셋) — 바깥에서 볼 때 반시계.</summary>
        public double[][] Corners;
        /// <summary>코너별 AO 단계 0..3(3 = 가장 밝음).</summary>
        public int[] Ao;
        /// <summary>면 색(칸 색 · 없으면 기본색).</summary>
        public int C;
        public int Vx, Vy, Vz;
    }

    /// <summary>정수 경계 상자(정본 `Voxel.bounds`).</summary>
    public struct VoxelBounds
    {
        public int X0, X1, Y0, Y1, Z0, Z1;
        public int W { get { return X1 - X0 + 1; } }
        public int H { get { return Y1 - Y0 + 1; } }
        public int D { get { return Z1 - Z0 + 1; } }
    }

    /// <summary>
    /// 정본 `web/js/voxel.js` 의 순수 계산부(T4) — 면 생성(안 보이는 면 제거) · 이음새 AO · 좌표 해시 색변화 · 조립 유틸.
    /// 값·순서·판정이 원작과 같아야 한다: `tools/voxel_vectors.js` 가 원작을 node 에서 돌려 뽑은 벡터와 EditMode 가 대조한다.
    /// 여기에는 UnityEngine 이 없다 — 메시로 만드는 것은 <see cref="VoxelGeometry"/>(배열) → Game `VoxelMob`(Mesh).
    /// </summary>
    public static class Voxel
    {
        /// <summary>6면의 법선(정본 FACES 순서 그대로: +x −x +y −y +z −z).</summary>
        public static readonly int[][] FaceNormals =
        {
            new[] { 1, 0, 0 }, new[] { -1, 0, 0 }, new[] { 0, 1, 0 }, new[] { 0, -1, 0 }, new[] { 0, 0, 1 }, new[] { 0, 0, -1 },
        };

        /// <summary>면마다 코너 4개의 로컬 오프셋(반 칸 단위 · −1/+1) — 바깥에서 볼 때 반시계. 뒤집히면 면이 안쪽을 향한다.</summary>
        public static readonly int[][][] FaceCorners =
        {
            new[] { new[] { 1, -1, -1 }, new[] { 1, 1, -1 }, new[] { 1, 1, 1 }, new[] { 1, -1, 1 } },
            new[] { new[] { -1, -1, 1 }, new[] { -1, 1, 1 }, new[] { -1, 1, -1 }, new[] { -1, -1, -1 } },
            new[] { new[] { -1, 1, -1 }, new[] { -1, 1, 1 }, new[] { 1, 1, 1 }, new[] { 1, 1, -1 } },
            new[] { new[] { -1, -1, 1 }, new[] { -1, -1, -1 }, new[] { 1, -1, -1 }, new[] { 1, -1, 1 } },
            new[] { new[] { -1, -1, 1 }, new[] { 1, -1, 1 }, new[] { 1, 1, 1 }, new[] { -1, 1, 1 } },
            new[] { new[] { 1, -1, -1 }, new[] { -1, -1, -1 }, new[] { -1, 1, -1 }, new[] { 1, 1, -1 } },
        };

        // ── 점유 집합 ──────────────────────────────────────────────────────────
        /// <summary>좌표 → 하나의 키(각 축 21비트 · ±2^20 칸까지).</summary>
        public static long Key(int x, int y, int z)
        {
            return ((long)(x + 0x100000) << 42) | ((long)(y + 0x100000) << 21) | (long)(z + 0x100000);
        }

        public static HashSet<long> Occupancy(IList<VoxelCell> cells)
        {
            var set = new HashSet<long>();
            for (int i = 0; i < cells.Count; i++) set.Add(Key(cells[i].X, cells[i].Y, cells[i].Z));
            return set;
        }

        // ── 이음새 AO ──────────────────────────────────────────────────────────
        /// <summary>
        /// 한 코너의 어둠 = 그 코너에 닿는 옆 이웃 2개 + 대각 이웃 1개(면 법선 방향 층에서). 옆 둘이 다 차면 0(가장 어둡다).
        /// 반환 0..3(3 = 가장 밝음). 정본 `aoOf` 그대로.
        /// </summary>
        public static int AoOf(HashSet<long> occ, int vx, int vy, int vz, int[] n, int[] corner)
        {
            int a0 = -1, a1 = -1;
            for (int a = 0; a < 3; a++) if (n[a] == 0) { if (a0 < 0) a0 = a; else a1 = a; }
            int[] s1 = { n[0], n[1], n[2] }, s2 = { n[0], n[1], n[2] }, d = { n[0], n[1], n[2] };
            s1[a0] += corner[a0];
            s2[a1] += corner[a1];
            d[a0] += corner[a0];
            d[a1] += corner[a1];
            bool o1 = occ.Contains(Key(vx + s1[0], vy + s1[1], vz + s1[2]));
            bool o2 = occ.Contains(Key(vx + s2[0], vy + s2[1], vz + s2[2]));
            if (o1 && o2) return 0;
            bool od = occ.Contains(Key(vx + d[0], vy + d[1], vz + d[2]));
            return 3 - ((o1 ? 1 : 0) + (o2 ? 1 : 0) + (od ? 1 : 0));
        }

        /// <summary>AO 단계(0..3) → 밝기 계수 `1 − (3 − level)/3 × 0.38 × strength`(가장 어두운 값 0.62 · 정본 `aoShade`).</summary>
        public static double AoShade(int level, double strength = 1)
        {
            return 1 - (3 - level) / 3.0 * 0.38 * strength;
        }

        // ── 면 생성 ────────────────────────────────────────────────────────────
        /// <summary>이웃이 있는 면은 만들지 않는다(정본 ⓑ). 칸 순서 × 면 순서(+x −x +y −y +z −z) 그대로.</summary>
        public static List<VoxelFace> Faces(IList<VoxelCell> cells, int defaultColor)
        {
            var occ = Occupancy(cells);
            var outFaces = new List<VoxelFace>();
            for (int i = 0; i < cells.Count; i++)
            {
                var v = cells[i];
                for (int f = 0; f < 6; f++)
                {
                    int[] n = FaceNormals[f];
                    if (occ.Contains(Key(v.X + n[0], v.Y + n[1], v.Z + n[2]))) continue;
                    var corners = new double[4][];
                    var ao = new int[4];
                    for (int c = 0; c < 4; c++)
                    {
                        int[] co = FaceCorners[f][c];
                        corners[c] = new[] { v.X + co[0] * 0.5, v.Y + co[1] * 0.5, v.Z + co[2] * 0.5 };
                        ao[c] = AoOf(occ, v.X, v.Y, v.Z, n, co);
                    }
                    outFaces.Add(new VoxelFace { N = n, Corners = corners, Ao = ao, C = v.HasColor ? v.C : defaultColor, Vx = v.X, Vy = v.Y, Vz = v.Z });
                }
            }
            return outFaces;
        }

        // ── 큐브별 미세 색변화(좌표 해시 · 결정적) ──────────────────────────────
        /// <summary>
        /// 정본 `jitter(x,y,z,amt)`: `h = (x*73856093) ^ (y*19349663) ^ (z*83492791)` 를 JS int32 로 · `(h ^ (h>>>13)) >>> 0` · `1 + ((h % 1000)/1000 − 0.5) × 2 × amt`.
        /// JS 의 `^` 는 피연산자를 ToInt32(2^32 나머지)로 만든다 — 여기서는 long 곱을 int 로 잘라 같게 한다.
        /// </summary>
        public static double Jitter(int x, int y, int z, double amt)
        {
            int h = unchecked((int)((long)x * 73856093L)) ^ unchecked((int)((long)y * 19349663L)) ^ unchecked((int)((long)z * 83492791L));
            uint u = (uint)h;
            uint r = u ^ (u >> 13);
            return 1 + ((r % 1000u) / 1000.0 - 0.5) * 2 * amt;
        }

        /// <summary>색을 채널별로 배율(255 에서 자른다 · 정본 `scaleHex` · JS Math.round = 반올림은 +∞ 쪽).</summary>
        public static int ScaleHex(int hex, double k)
        {
            int r = (int)Math.Floor(Math.Min(255, ((hex >> 16) & 255) * k) + 0.5);
            int g = (int)Math.Floor(Math.Min(255, ((hex >> 8) & 255) * k) + 0.5);
            int b = (int)Math.Floor(Math.Min(255, (hex & 255) * k) + 0.5);
            return (r << 16) | (g << 8) | b;
        }

        // ── 조형 원형 · 조립 유틸(전부 새 목록을 돌려준다 · 입력을 안 고친다) ───────
        /// <summary>직육면체 칸 덩어리 — x → y → z 중첩 순서(정본 `Voxel.box` 와 같은 순서 · paint 체크섬이 이 순서를 전제한다).</summary>
        public static List<VoxelCell> Box(int w, int h, int d, int color)
        {
            var outCells = new List<VoxelCell>(Math.Max(0, w * h * d));
            for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) for (int z = 0; z < d; z++) outCells.Add(new VoxelCell(x, y, z, color));
            return outCells;
        }

        public static List<VoxelCell> At(IList<VoxelCell> cells, int dx, int dy, int dz)
        {
            var outCells = new List<VoxelCell>(cells.Count);
            for (int i = 0; i < cells.Count; i++) { var v = cells[i]; outCells.Add(new VoxelCell(v.X + dx, v.Y + dy, v.Z + dz, v.C)); }
            return outCells;
        }

        public static List<VoxelCell> Merge(params IList<VoxelCell>[] lists)
        {
            var outCells = new List<VoxelCell>();
            for (int i = 0; i < lists.Length; i++) if (lists[i] != null) outCells.AddRange(lists[i]);
            return outCells;
        }

        /// <summary>x 를 거울면 `about` 에 대해 뒤집는다(정본 `mirrorX` · 기본 0).</summary>
        public static List<VoxelCell> MirrorX(IList<VoxelCell> cells, int about = 0)
        {
            var outCells = new List<VoxelCell>(cells.Count);
            for (int i = 0; i < cells.Count; i++) { var v = cells[i]; outCells.Add(new VoxelCell(2 * about - v.X, v.Y, v.Z, v.C)); }
            return outCells;
        }

        static List<VoxelCell> Rot(IList<VoxelCell> cells, int k, Func<VoxelCell, VoxelCell> step)
        {
            int n = ((k % 4) + 4) % 4;
            var cur = new List<VoxelCell>(cells);
            for (int t = 0; t < n; t++)
            {
                var next = new List<VoxelCell>(cur.Count);
                for (int i = 0; i < cur.Count; i++) next.Add(step(cur[i]));
                cur = next;
            }
            return cur;
        }

        /// <summary>90° 회전 k 번(행렬식 +1 — 축 맞바꾸기로 대신하지 않는다 · 정본 `rotX`).</summary>
        public static List<VoxelCell> RotX(IList<VoxelCell> cells, int k = 1) { return Rot(cells, k, v => new VoxelCell(v.X, -v.Z, v.Y, v.C)); }
        public static List<VoxelCell> RotY(IList<VoxelCell> cells, int k = 1) { return Rot(cells, k, v => new VoxelCell(v.Z, v.Y, -v.X, v.C)); }
        public static List<VoxelCell> RotZ(IList<VoxelCell> cells, int k = 1) { return Rot(cells, k, v => new VoxelCell(-v.Y, v.X, v.Z, v.C)); }

        /// <summary>색을 다시 칠한다 — fn 이 음수를 주면 그 칸은 그대로(정본 `recolor` 의 undefined).</summary>
        public static List<VoxelCell> Recolor(IList<VoxelCell> cells, Func<VoxelCell, int, int> fn)
        {
            var outCells = new List<VoxelCell>(cells.Count);
            for (int i = 0; i < cells.Count; i++) { var v = cells[i]; int c = fn(v, i); outCells.Add(new VoxelCell(v.X, v.Y, v.Z, c < 0 ? v.C : c)); }
            return outCells;
        }

        public static bool Bounds(IList<VoxelCell> cells, out VoxelBounds b)
        {
            b = new VoxelBounds();
            if (cells.Count == 0) return false;
            b.X0 = b.Y0 = b.Z0 = int.MaxValue; b.X1 = b.Y1 = b.Z1 = int.MinValue;
            for (int i = 0; i < cells.Count; i++)
            {
                var v = cells[i];
                if (v.X < b.X0) b.X0 = v.X; if (v.X > b.X1) b.X1 = v.X;
                if (v.Y < b.Y0) b.Y0 = v.Y; if (v.Y > b.Y1) b.Y1 = v.Y;
                if (v.Z < b.Z0) b.Z0 = v.Z; if (v.Z > b.Z1) b.Z1 = v.Z;
            }
            return true;
        }

        /// <summary>겉껍질만 — 6이웃 중 하나라도 빈 칸(정본 `hollow` · 반투명 파츠의 두께용).</summary>
        public static List<VoxelCell> Hollow(IList<VoxelCell> cells)
        {
            var occ = Occupancy(cells);
            var outCells = new List<VoxelCell>();
            for (int i = 0; i < cells.Count; i++)
            {
                var v = cells[i];
                bool open = false;
                for (int f = 0; f < 6 && !open; f++) { int[] n = FaceNormals[f]; if (!occ.Contains(Key(v.X + n[0], v.Y + n[1], v.Z + n[2]))) open = true; }
                if (open) outCells.Add(v);
            }
            return outCells;
        }
    }
}
