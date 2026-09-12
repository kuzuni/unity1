using System;
using System.Collections.Generic;
using Forge.Core.Voxel;

namespace Forge.Core.World
{
    /// <summary>정본 `voxelGroundGeo()` 의 배열(three 좌표 · 오른손). 셀마다 윗면 쿼드(정점 6) + 낮은 이웃 쪽 벽.</summary>
    public sealed class GroundMesh
    {
        public List<double> Positions = new List<double>();
        public List<double> Normals = new List<double>();
        public List<double> Uvs = new List<double>();
        public List<double> Colors = new List<double>();
        /// <summary>정점이 노면 회랑(|z| &lt; 2.25) 셀에 속하는가 — 셰이더 `uRoad` 배율을 정점색에 곱해 주는 자리(T9 결정).</summary>
        public List<bool> Road = new List<bool>();
        public int VertexCount { get { return Positions.Count / 3; } }
    }

    /// <summary>
    /// 정본 지형 격자(T9): `heightSmooth`·`heightAt`(VOXG 양자화 · SIMPLE_BG 면 0)·`cellRGB`(노면 포석/연석·흙 결·셀 지터)·`voxelGroundGeo`.
    /// 순수 계산 — `tools/world_vectors.js` 가 정본을 실제로 돌린 정점 수·합·표본 셀과 EditMode 가 대조한다.
    /// </summary>
    public static class GroundGrid
    {
        public static double Clamp01(double v) { return Math.Max(0, Math.Min(1, v)); }

        /// <summary>정본 `heightSmooth(x, z)` — 양자화 전 연속 높이.</summary>
        public static double HeightSmooth(double x, double z)
        {
            double P = Math.PI * 2 / WorldRules.TilePeriod;
            double n = Math.Sin(x * P * 2 + z * 0.3) * 0.5 + Math.Sin(x * P + 7.3) * 0.3 + Math.Cos(z * 0.6 + x * P * 3) * 0.2;
            double back = Clamp01((-z - 2.0) / 5.5);
            double front = Clamp01((z - 2.4) / 3);
            return back * back * (1.7 + n * 1.3) + front * (0.5 + n * 0.35);
        }

        /// <summary>정본 `heightAt(x, z)` — SIMPLE_BG 면 0(완전 평면) · 아니면 셀 중심에서 잰 연속 높이를 단(step)으로 내림.</summary>
        public static double HeightAt(SceneDefs d, double x, double z)
        {
            if (d.SimpleBg) return 0;
            double BS = d.VoxCell, SH = d.VoxStep;
            return Math.Floor(HeightSmooth((Math.Floor(x / BS) + 0.5) * BS, (Math.Floor(z / BS) + 0.5) * BS) / SH) * SH;
        }

        private static double Smooth(double t) { return t * t * (3 - 2 * t); }

        /// <summary>정본 `cellRGB(ix, iz)` — 셀 중심 색 배수(재질색에 곱해진다). 노면 회랑이면 <paramref name="road"/> 가 true.</summary>
        public static double[] CellRgb(SceneDefs d, int ix, int iz, out bool road)
        {
            double BS = d.VoxCell;
            int PERX = (int)Math.Round(WorldRules.TilePeriod / BS);
            double x = WorldRules.GroundX0 + (ix + 0.5) * BS, z = WorldRules.GroundZ0 + (iz + 0.5) * BS;
            int px = ((ix % PERX) + PERX) % PERX;
            double az = Math.Abs(z);
            if (az < WorldRules.RoadHalfWidth)
            {
                road = true;
                double jr = Voxel.Voxel.Jitter(px, 0, iz, WorldRules.RoadJitter);
                if (az >= WorldRules.RoadCoreHalfWidth)
                {
                    double kb = ((px + iz) & 1) != 0 ? 0.46 : 0.41;
                    return new[] { kb * jr, kb * jr, kb * 1.02 * jr };
                }
                int brick = (px + (iz & 1)) >> 1;
                double bv = (brick & 1) != 0 ? 1.07 : 0.93;
                double row = (iz & 1) != 0 ? 1.02 : 0.98;
                return new[] { bv * row * jr, bv * row * jr, bv * row * 1.01 * jr };
            }
            road = false;
            double P = Math.PI * 2 / WorldRules.TilePeriod;
            double n = Math.Sin(x * P + z * 0.34 + 1.3) * 0.62 + Math.Sin(x * P * 2 + 4.1 - z * 0.21) * 0.38;
            double r = 1, g = 1, b = 1;
            if (n > 0.12)
            {
                double k = Smooth(Clamp01((n - 0.12) / 0.5)) * 0.32;
                r = 1 + k * 1.15; g = 1 + k; b = 1 + k * 0.55;
            }
            else if (n < -0.16)
            {
                double k = Smooth(Clamp01((-n - 0.16) / 0.5)) * 0.38;
                r = 1 - k * 0.85; g = 1 - k; b = 1 - k * 1.2;
            }
            double j = Voxel.Voxel.Jitter(px, 0, iz, WorldRules.SoilJitter);
            return new[] { r * j, g * j, b * j };
        }

        /// <summary>정본 `voxelGroundGeo()` — 60×60 을 셀로 나눠 윗면 쿼드 + 낮은 이웃 쪽 벽(0.8 배 색). 좌표는 three(오른손) 그대로.</summary>
        public static GroundMesh Build(SceneDefs d)
        {
            double BS = d.VoxCell, X0 = WorldRules.GroundX0, Z0 = WorldRules.GroundZ0;
            int NX = (int)Math.Round(WorldRules.GroundSpan / BS), NZ = (int)Math.Round(WorldRules.GroundSpan / BS);
            var m = new GroundMesh();
            Func<int, int, double> H = (ix, iz) => HeightAt(d, X0 + (ix + 0.5) * BS, Z0 + (iz + 0.5) * BS);
            for (int ix = 0; ix < NX; ix++)
            {
                for (int iz = 0; iz < NZ; iz++)
                {
                    double h = H(ix, iz);
                    bool road;
                    double[] rgb = CellRgb(d, ix, iz, out road);
                    double x1 = X0 + ix * BS, x2 = x1 + BS, z1 = Z0 + iz * BS, z2 = z1 + BS;
                    Quad(m, x1, h, z2, x2, h, z2, x2, h, z1, x1, h, z1, 0, 1, 0, rgb, road);
                    double[] w = { rgb[0] * WorldRules.WallShade, rgb[1] * WorldRules.WallShade, rgb[2] * WorldRules.WallShade };
                    double e = H(ix + 1, iz), ww = H(ix - 1, iz), s = H(ix, iz + 1), nn = H(ix, iz - 1);
                    if (e < h) Quad(m, x2, e, z2, x2, e, z1, x2, h, z1, x2, h, z2, 1, 0, 0, w, road);
                    if (ww < h) Quad(m, x1, ww, z1, x1, ww, z2, x1, h, z2, x1, h, z1, -1, 0, 0, w, road);
                    if (s < h) Quad(m, x1, s, z2, x2, s, z2, x2, h, z2, x1, h, z2, 0, 0, 1, w, road);
                    if (nn < h) Quad(m, x2, nn, z1, x1, nn, z1, x1, h, z1, x2, h, z1, 0, 0, -1, w, road);
                }
            }
            return m;
        }

        private static void Emit(GroundMesh m, double x, double y, double z, double nx, double ny, double nz, double[] rgb, bool road)
        {
            m.Positions.Add(x); m.Positions.Add(y); m.Positions.Add(z);
            m.Normals.Add(nx); m.Normals.Add(ny); m.Normals.Add(nz);
            m.Uvs.Add((x + 30) / 60); m.Uvs.Add((z + 45) / 60);
            m.Colors.Add(rgb[0]); m.Colors.Add(rgb[1]); m.Colors.Add(rgb[2]);
            m.Road.Add(road);
        }

        private static void Quad(GroundMesh m,
            double ax, double ay, double az, double bx, double by, double bz, double cx, double cy, double cz, double dx, double dy, double dz,
            double nx, double ny, double nz, double[] rgb, bool road)
        {
            Emit(m, ax, ay, az, nx, ny, nz, rgb, road); Emit(m, bx, by, bz, nx, ny, nz, rgb, road); Emit(m, cx, cy, cz, nx, ny, nz, rgb, road);
            Emit(m, ax, ay, az, nx, ny, nz, rgb, road); Emit(m, cx, cy, cz, nx, ny, nz, rgb, road); Emit(m, dx, dy, dz, nx, ny, nz, rgb, road);
        }
    }
}
