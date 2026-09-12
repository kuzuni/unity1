using System;
using System.Collections.Generic;

namespace Forge.Core.Voxel
{
    /// <summary>
    /// 면 병합 메시 데이터(정본 `Voxel.build` 의 지오메트리 부분 · THREE 없이). 면 하나 = 정점 4 + 삼각형 2(인덱스 6).
    /// 정본은 면당 정점 6개(비인덱스)를 쓴다 — <see cref="Triangles"/> 를 인덱스로 펴면 **같은 순서·같은 값**이 나온다(EditMode 가 그것을 잰다).
    /// 좌표는 <see cref="VoxelGeometry.Build"/> 의 `leftHanded` 에 따라 three(오른손) 또는 유니티(z 부호 반전 · 삼각형 감김 반전).
    /// </summary>
    public sealed class VoxelMesh
    {
        /// <summary>정점 위치 xyz(세계 단위 = 칸 × size · 중심 정렬 뒤).</summary>
        public float[] Positions;
        /// <summary>정점 법선(면마다 따로 — 플랫 셰이딩은 여기서 나온다).</summary>
        public float[] Normals;
        /// <summary>정점 색 rgb 0..1(칸 색 × AO × jitter).</summary>
        public float[] Colors;
        /// <summary>삼각형 인덱스(면당 6).</summary>
        public int[] Triangles;
        public int FaceCount;
        /// <summary>중심 정렬에 쓴 칸 중심(cx, cy, cz).</summary>
        public double[] Center;

        public int VertexCount { get { return Positions.Length / 3; } }
    }

    /// <summary>`Voxel.build` 옵션(정본 기본값: size 0.1 · jitter 0.06 · ao 1 · color 0xffffff · center true). `Mobs.build` 는 size=cell · jitter 0.022(basic 은 0) · ao 0.85 를 준다.</summary>
    public struct VoxelBuildOptions
    {
        public double Size;
        public int Color;
        public double Jitter;
        public double Ao;
        public bool Center;
        /// <summary>true 면 유니티 좌표(z 반전 · 감김 반전). false 면 three 좌표 그대로(벡터 대조용).</summary>
        public bool LeftHanded;

        public static VoxelBuildOptions Default
        {
            get { return new VoxelBuildOptions { Size = 0.1, Color = 0xffffff, Jitter = 0.06, Ao = 1, Center = true, LeftHanded = false }; }
        }
    }

    /// <summary>정본 `Voxel.build` 의 순수 계산 — 칸 목록 → 정점/법선/색/인덱스 배열.</summary>
    public static class VoxelGeometry
    {
        public static VoxelMesh Build(IList<VoxelCell> cells, VoxelBuildOptions o)
        {
            var fl = Voxel.Faces(cells, o.Color);

            double cx = 0, cy = 0, cz = 0;
            if (o.Center && cells.Count > 0)
            {
                VoxelBounds b;
                Voxel.Bounds(cells, out b);
                cx = (b.X0 + b.X1) / 2.0; cy = (b.Y0 + b.Y1) / 2.0; cz = (b.Z0 + b.Z1) / 2.0;
            }

            int n = fl.Count;
            var pos = new float[n * 12];
            var nor = new float[n * 12];
            var col = new float[n * 12];
            var tri = new int[n * 6];
            double zs = o.LeftHanded ? -1 : 1;
            for (int f = 0; f < n; f++)
            {
                var F = fl[f];
                double r = ((F.C >> 16) & 255) / 255.0, g = ((F.C >> 8) & 255) / 255.0, bl = (F.C & 255) / 255.0;
                double jf = Voxel.Jitter(F.Vx, F.Vy, F.Vz, o.Jitter);
                for (int c = 0; c < 4; c++)
                {
                    int p = (f * 4 + c) * 3;
                    double[] cn = F.Corners[c];
                    pos[p] = (float)((cn[0] - cx) * o.Size);
                    pos[p + 1] = (float)((cn[1] - cy) * o.Size);
                    pos[p + 2] = (float)((cn[2] - cz) * o.Size * zs);
                    nor[p] = F.N[0]; nor[p + 1] = F.N[1]; nor[p + 2] = (float)(F.N[2] * zs);
                    double sh = Voxel.AoShade(F.Ao[c], o.Ao) * jf;
                    col[p] = (float)(r * sh); col[p + 1] = (float)(g * sh); col[p + 2] = (float)(bl * sh);
                }
                // AO 가 낮은 코너 쪽으로 사각형을 가른다(flipped-quad · 정본 그대로).
                bool flip = (F.Ao[0] + F.Ao[2]) < (F.Ao[1] + F.Ao[3]);
                int[] order = flip ? new[] { 1, 2, 3, 1, 3, 0 } : new[] { 0, 1, 2, 0, 2, 3 };
                int t = f * 6, b0 = f * 4;
                if (o.LeftHanded)
                {
                    // z 거울은 감김을 뒤집는다 — 삼각형마다 뒤 두 정점을 바꿔 앞면을 유지한다.
                    tri[t] = b0 + order[0]; tri[t + 1] = b0 + order[2]; tri[t + 2] = b0 + order[1];
                    tri[t + 3] = b0 + order[3]; tri[t + 4] = b0 + order[5]; tri[t + 5] = b0 + order[4];
                }
                else
                {
                    for (int k = 0; k < 6; k++) tri[t + k] = b0 + order[k];
                }
            }
            return new VoxelMesh { Positions = pos, Normals = nor, Colors = col, Triangles = tri, FaceCount = n, Center = new[] { cx, cy, cz } };
        }
    }
}
