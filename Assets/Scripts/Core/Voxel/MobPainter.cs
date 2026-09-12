using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Voxel
{
    /// <summary>
    /// 정본 `mobs.js` 의 칸 칠하기(`span` · `paint` · `expand`). 규칙 하나 = {c, x/y/z 범위} · 범위는 [a,b](끝 포함)·수 하나·생략(전체) ·
    /// 음수는 뒤에서(−1 = 마지막 칸) · `mx` 면 x 범위를 거울로 한 번 더(눈 두 짝을 한 줄로). 값은 <see cref="MobPaint"/>(T3) 원문 그대로 받는다.
    /// </summary>
    public static class MobPainter
    {
        /// <summary>축 지정 → [lo, hi](끝 포함). null = 전체 · 음수는 n 에서 뺀다(정본 `span`).</summary>
        public static int[] Span(PaintAxis r, int n)
        {
            if (r == null) return new[] { 0, n - 1 };
            int a = r.Lo < 0 ? n + r.Lo : r.Lo;
            int b = r.Hi < 0 ? n + r.Hi : r.Hi;
            if (!r.IsRange) b = a;
            return new[] { a, b };
        }

        /// <summary>`mx` 규칙을 거울 규칙으로 한 줄 더 만든다(정본 `expand` · 원 규칙 뒤에 바로 붙는다).</summary>
        public static List<MobPaint> Expand(IList<MobPaint> rules, int w)
        {
            if (rules == null) return null;
            var outRules = new List<MobPaint>(rules.Count * 2);
            for (int i = 0; i < rules.Count; i++)
            {
                var R = rules[i];
                outRules.Add(R);
                if (R != null && R.Mx)
                {
                    int[] xs = Span(R.X, w);
                    outRules.Add(new MobPaint { C = R.C, X = new PaintAxis { Lo = w - 1 - xs[1], Hi = w - 1 - xs[0], IsRange = true }, Y = R.Y, Z = R.Z, Mx = false });
                }
            }
            return outRules;
        }

        /// <summary>규칙을 순서대로 적용해 칸 색을 바꾼다(정본 `paint` · 새 목록).</summary>
        public static List<VoxelCell> Paint(IList<VoxelCell> cells, int w, int h, int d, IList<MobPaint> rules)
        {
            var outCells = new List<VoxelCell>(cells);
            if (rules == null || rules.Count == 0) return outCells;
            for (int k = 0; k < rules.Count; k++)
            {
                var R = rules[k];
                if (R == null) continue;
                int[] xs = Span(R.X, w), ys = Span(R.Y, h), zs = Span(R.Z, d);
                for (int i = 0; i < outCells.Count; i++)
                {
                    var v = outCells[i];
                    if (v.X < xs[0] || v.X > xs[1]) continue;
                    if (v.Y < ys[0] || v.Y > ys[1]) continue;
                    if (v.Z < zs[0] || v.Z > zs[1]) continue;
                    outCells[i] = new VoxelCell(v.X, v.Y, v.Z, R.C);
                }
            }
            return outCells;
        }

        /// <summary>파츠 하나의 칸 목록 = `Voxel.box` + paint(expand 포함). 색은 `color`(vivid 보정 뒤 값)로 채운 뒤 칠한다.</summary>
        public static List<VoxelCell> CellsOf(MobPart p, int color)
        {
            var cells = Voxel.Box(p.Box[0], p.Box[1], p.Box[2], color);
            if (p.Paint != null && p.Paint.Count > 0) cells = Paint(cells, p.Box[0], p.Box[1], p.Box[2], Expand(p.Paint, p.Box[0]));
            return cells;
        }
    }
}
