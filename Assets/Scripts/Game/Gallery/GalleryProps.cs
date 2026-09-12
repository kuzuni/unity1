using System.Collections.Generic;
using UnityEngine;
using Forge.Core.Data;
using Forge.Core.Voxel;
using Forge.Game.Voxel;

namespace Forge.Game.Gallery
{
    /// <summary>
    /// 소품 표본(T2 `mobs-props.json` samples · 정본 `Props.*` 생성 함수를 시드 고정으로 돌린 칸 목록)을 세운다 — 정본 `scene3d.js vxProp`
    /// (`Voxel.build(voxels, { size: u, jitter: 0.055, ao: 0.95, center: false })` · `mesh.position.y = size * 0.5` · 칸 색 = 재질 색 × `c`).
    /// 재질 이름 → 색은 정본 `scene3d.js propMat()` 의 기본 재질 색이다(잎·이끼처럼 `setTheme` 이 챕터마다 물들이는 것은 **초기값** — 테마 파생은 T9 의 몫).
    /// </summary>
    public static class GalleryProps
    {
        public const double PropJitter = 0.055;
        public const double PropAo = 0.95;

        /// <summary>정본 `propMat(role)` 의 재질 기본색(`scene3d.js` 생성자 + `propMat` switch). 없는 역할은 `stoneMat`.</summary>
        public static readonly Dictionary<string, int> RoleColor = new Dictionary<string, int>
        {
            { "trunk", 0x5d4037 }, { "birch", 0xd7d0be }, { "leaf0", 0x33691e }, { "leaf1", 0x33691e }, { "leaf2", 0x33691e },
            { "char", 0x30231d }, { "charRock", 0x2e2521 }, { "stone", 0x9a9083 }, { "snow", 0xf4faff }, { "moss", 0x4f8578 },
            { "cactus", 0x6da24f }, { "flower", 0xef6292 }, { "lava", 0xff7043 }, { "bone", 0xe6ddc8 }, { "bush", 0x4a7c2f },
            { "bamboo", 0x9ccc65 }, { "bambooLeaf", 0x8bc34a }, { "gill", 0xe8dfc8 }, { "spore", 0xf5f0e0 }, { "stem", 0x4a7332 },
            { "petal", 0xef6292 }, { "fern", 0x3d6b2a },
        };
        public const int DefaultRoleColor = 0x9a9083;

        public static int ColorOfRole(string role)
        {
            int c;
            return role != null && RoleColor.TryGetValue(role, out c) ? c : DefaultRoleColor;
        }

        /// <summary>정본 vertexColors 규약: 칸 색 `c` 는 재질 색에 곱해지는 계수.</summary>
        public static int Multiply(int a, int b)
        {
            int r = ((a >> 16) & 255) * ((b >> 16) & 255) / 255;
            int g = ((a >> 8) & 255) * ((b >> 8) & 255) / 255;
            int bl = (a & 255) * (b & 255) / 255;
            return (r << 16) | (g << 8) | bl;
        }

        /// <summary>표본의 파츠 하나 → 칸 목록(같은 자리 중복은 정본처럼 마지막 것만).</summary>
        public static List<VoxelCell> CellsOf(PropPart part)
        {
            int matColor = ColorOfRole(part.M);
            var seen = new Dictionary<long, int>();
            var cells = new List<VoxelCell>();
            for (int i = 0; i < part.V.Count; i++)
            {
                int[] v = part.V[i];
                if (v == null || v.Length < 3) continue;
                int col = v.Length > 3 ? Multiply(matColor, v[3]) : matColor;
                long key = ((long)(v[0] + 100000) << 40) | ((long)(v[1] + 100000) << 20) | (long)(v[2] + 100000);
                int at;
                if (seen.TryGetValue(key, out at)) cells[at] = new VoxelCell(v[0], v[1], v[2], col);
                else { seen[key] = cells.Count; cells.Add(new VoxelCell(v[0], v[1], v[2], col)); }
            }
            return cells;
        }

        public static GameObject Build(PropSample sample, Transform parent, string name)
        {
            var root = new GameObject(name ?? ("Prop " + sample.Kind));
            if (parent != null) root.transform.SetParent(parent, false);
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            Material mat = VoxelMaterials.Get(MobBuilder.MatKey(null), null);
            for (int i = 0; i < sample.Parts.Count; i++)
            {
                var part = sample.Parts[i];
                if (part == null || part.V == null || part.V.Count == 0) continue;
                var cells = CellsOf(part);
                var vm = VoxelGeometry.Build(cells, new VoxelBuildOptions
                {
                    Size = sample.U, Color = 0xffffff, Jitter = PropJitter, Ao = PropAo, Center = false, LeftHanded = true,
                });
                var go = new GameObject(part.M ?? ("part" + i));
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(0f, (float)(sample.U * 0.5), 0f);
                go.AddComponent<MeshFilter>().sharedMesh = VoxelMob.ToMesh(vm, sample.Kind + "/" + part.M, linear);
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
            }
            return root;
        }
    }
}
