using System;
using System.Collections.Generic;
using Forge.Core.Data;
using Forge.Core.Voxel;
using VoxelShapes = Forge.Core.Voxel.Voxel;   // 네임스페이스 Forge.Core.Voxel 과 이름이 같아 Core.Ui 안에서는 별칭으로 부른다

namespace Forge.Core.Ui
{
    /// <summary>티어 모티프(정본 `ASCEND_MOTIF` · 1 불 · 2 얼음 · 3 뇌전 · 4 신성 · 5 심연) — 룬 링 색 · 스킬 연출 색 변주가 공유한다.</summary>
    public sealed class AscendMotif
    {
        public int Tier;
        public string Name;
        public int Color;
    }

    /// <summary>비율 × r 에 하한을 두는 자(정본 `Math.max(min, r * k)`) — `Round` 면 정본 `Math.max(min, Math.round(r * k))`.</summary>
    public struct AscendClamp
    {
        public double K, Min;
        public bool Round;
        public double Of(double r)
        {
            double v = r * K;
            if (Round) v = Math.Round(v, MidpointRounding.AwayFromZero);
            return Math.Max(Min, v);
        }
    }

    /// <summary>
    /// 펫·탈것 승천 데코(T399 · 정본 `scene3d.js` 9653 `ascendTier` · 9670 `applyAscendDecor`)의 **수치표·셈** — `Resources/AscendDecorUi.json`.
    /// 별 수가 tier = stars % 6 으로 순환하고 tier n 이면 L1 밴드 → L2 가시 → L3 룬 링 → L4 스터드 → L5 왕관을 **누적**으로 얹는다.
    /// 치수는 전부 몸의 경계 상자(스케일 전 원본) 비율이다. 값은 전부 표에서 온다(§1) · UnityEngine 참조 0 — 좌표는 정본 three 좌표(Game 이 <c>ThreeSpace</c> 로 뒤집는다).
    /// </summary>
    public sealed class AscendDecorSpec
    {
        public int Cycle;
        public double Metalness, Roughness;
        /// <summary>색인 = tier(0 은 null).</summary>
        public AscendMotif[] Motifs;

        public double BandYK, BandRK, BandH, BandJitter;
        public AscendClamp BandCell, BandT;
        public int BandColor;

        public int SpikeN, SpikeColor;
        public double SpikeR1, SpikeRadiusK, SpikeJitter;
        public AscendClamp SpikeCell, SpikeR0, SpikeH;

        public double RuneInK, RuneOutK, RuneOpacity, RuneYOff;
        public int RuneSegments;

        public int StudN, StudColor, StudEmissive;
        public double StudAngle0, StudRadiusK, StudYK, StudEmissiveK, StudJitter;
        public AscendClamp StudCell, StudR;

        public int CrownHorns, CrownColor, CrownEmissive;
        public double CrownHornR1, CrownHornYK, CrownYK, CrownEmissiveK, CrownJitter;
        public AscendClamp CrownR, CrownCell, CrownRingT, CrownRingH, CrownHornR0, CrownHornH;

        /// <summary>정본 `ascendTier(stars) = (((stars || 0) % 6) + 6) % 6` — 6승천이면 0승천 디자인으로 순환한다.</summary>
        public int Tier(int stars) { return ((stars % Cycle) + Cycle) % Cycle; }

        /// <summary>tier 의 모티프(1~5) — 정본 `ASCEND_MOTIF[Math.min(tier, 5)]` · tier 0 은 null.</summary>
        public AscendMotif MotifOf(int tier)
        {
            if (tier <= 0) return null;
            return Motifs[Math.Min(tier, Motifs.Length - 1)];
        }

        public static int HexOf(string s)
        {
            if (string.IsNullOrEmpty(s) || s[0] != '#' || s.Length != 7) throw new FormatException("AscendDecorUi: 색은 #rrggbb 여야 한다 — " + s);
            return Convert.ToInt32(s.Substring(1), 16);
        }

        public static AscendDecorSpec From(JsonObject root)
        {
            JsonObject L = J.Obj(J.Require(root, "layout"));
            JsonObject C = J.Obj(J.Require(root, "colors"));
            Func<string, double> N = k => J.Num(J.Require(L, k));
            Func<string, int> I = k => J.Int(J.Require(L, k));
            Func<string, int> H = k => HexOf(J.Str(J.Require(C, k)));
            Func<string, string, bool, AscendClamp> CL = (kk, mk, round) => new AscendClamp { K = N(kk), Min = N(mk), Round = round };
            var s = new AscendDecorSpec
            {
                Cycle = I("cycle"), Metalness = N("metalness"), Roughness = N("roughness"),
                BandYK = N("band_y_k"), BandCell = CL("band_cell_k", "band_cell_min", false), BandRK = N("band_r_k"),
                BandT = CL("band_t_k", "band_t_min_cells", false), BandH = N("band_h_cells"), BandJitter = N("band_jitter"), BandColor = H("band"),
                SpikeN = I("spike_n"), SpikeCell = CL("spike_cell_k", "spike_cell_min", false), SpikeR0 = CL("spike_r0_k", "spike_r0_min_cells", false),
                SpikeR1 = N("spike_r1_cells"), SpikeH = CL("spike_h_k", "spike_h_min_cells", true), SpikeRadiusK = N("spike_radius_k"), SpikeJitter = N("spike_jitter"), SpikeColor = H("spike"),
                RuneInK = N("rune_in_k"), RuneOutK = N("rune_out_k"), RuneSegments = I("rune_segments"), RuneOpacity = N("rune_opacity"), RuneYOff = N("rune_y_off"),
                StudN = I("stud_n"), StudAngle0 = N("stud_angle0"), StudCell = CL("stud_cell_k", "stud_cell_min", false), StudR = CL("stud_r_k", "stud_r_min_cells", false),
                StudRadiusK = N("stud_radius_k"), StudYK = N("stud_y_k"), StudEmissiveK = N("stud_emissive_k"), StudJitter = N("stud_jitter"), StudColor = H("stud"), StudEmissive = H("stud_emissive"),
                CrownR = CL("crown_r_k", "crown_r_min", false), CrownCell = CL("crown_cell_k", "crown_cell_min", false), CrownRingT = CL("crown_ring_t_k", "crown_ring_t_min_cells", false),
                CrownRingH = CL("crown_ring_h_k", "crown_ring_h_min_cells", true), CrownHorns = I("crown_horns"), CrownHornR0 = CL("crown_horn_r0_k", "crown_horn_r0_min_cells", false),
                CrownHornR1 = N("crown_horn_r1_cells"), CrownHornH = CL("crown_horn_h_k", "crown_horn_h_min_cells", true), CrownHornYK = N("crown_horn_y_k"), CrownYK = N("crown_y_k"),
                CrownEmissiveK = N("crown_emissive_k"), CrownJitter = N("crown_jitter"), CrownColor = H("crown"), CrownEmissive = H("crown_emissive"),
            };
            if (s.Cycle < 2) throw new FormatException("AscendDecorUi: cycle 은 2 이상이어야 한다(정본 6)");
            var motifs = J.List(J.Require(root, "motifs"), x => J.Obj(x));
            s.Motifs = new AscendMotif[s.Cycle];
            for (int i = 0; i < motifs.Count; i++)
            {
                int t = J.Int(J.Require(motifs[i], "tier"));
                if (t < 1 || t >= s.Cycle) throw new FormatException("AscendDecorUi: motifs.tier 는 1~" + (s.Cycle - 1) + " — " + t);
                s.Motifs[t] = new AscendMotif { Tier = t, Name = J.Str(J.Require(motifs[i], "name")), Color = HexOf(J.Str(J.Require(motifs[i], "color"))) };
            }
            for (int t = 1; t < s.Cycle; t++) if (s.Motifs[t] == null) throw new FormatException("AscendDecorUi: motifs 에 tier " + t + " 가 없다");
            return s;
        }
    }

    /// <summary>데코 조각 하나 — 큐브 덩어리(<see cref="Cells"/>) 또는 발밑 룬 링(<see cref="IsRune"/>). 자리·회전은 정본 three 좌표(데코 뿌리 = 몸 메시 뿌리 기준).</summary>
    public sealed class AscendDecorPiece
    {
        public int Layer;
        public string Name;
        public List<VoxelCell> Cells;
        /// <summary>칸 한 변(월드) · 색(정본은 칸을 흰색으로 굽고 `decoMat(hex)` 재질색을 곱한다 = hex — 클론은 hex 를 정점에 굽고 재질은 흰색 · 같은 값) · 정본 `center` · `jitter`.</summary>
        public double Size, Jitter;
        public int Color;
        public bool Center;
        /// <summary>-1 이면 발광 없음.</summary>
        public int Emissive = -1;
        public double EmissiveIntensity;
        public double[] Pos = { 0, 0, 0 };
        public double[] Rot = { 0, 0, 0 };
        public bool IsRune;
        public double RuneInner, RuneOuter, Opacity;
        public int RuneSegments;
        public int CellCount { get { return Cells == null ? 0 : Cells.Count; } }
    }

    /// <summary>정본 `applyAscendDecor` 의 순수 셈 — 경계 상자(three 좌표 · 스케일 전)와 tier 로 조각 목록을 낸다. Game <c>AscendDecor</c> 가 메시·재질로 세운다.</summary>
    public sealed class AscendDecorPlan
    {
        public int Tier;
        public AscendMotif Motif;
        /// <summary>정본 `r = max(size.x, size.z) × .5` · `bandY = min.y + size.y × .55`.</summary>
        public double R, BandY;
        public readonly List<AscendDecorPiece> Pieces = new List<AscendDecorPiece>();

        /// <summary>층 n 의 조각 수.</summary>
        public int CountOf(int layer)
        {
            int n = 0;
            for (int i = 0; i < Pieces.Count; i++) if (Pieces[i].Layer == layer) n++;
            return n;
        }

        /// <summary>얹힌 층의 수(서로 다른 Layer 값) — 정본 «누적 단조 계약»: tier 와 같아야 한다.</summary>
        public int LayerCount
        {
            get
            {
                var seen = new HashSet<int>();
                for (int i = 0; i < Pieces.Count; i++) seen.Add(Pieces[i].Layer);
                return seen.Count;
            }
        }

        /// <param name="min">경계 상자 최소(three 좌표 · x y z)</param><param name="max">경계 상자 최대</param>
        public static AscendDecorPlan Make(AscendDecorSpec s, int stars, double[] min, double[] max)
        {
            var plan = new AscendDecorPlan { Tier = s.Tier(stars) };
            if (plan.Tier == 0 || min == null || max == null) return plan;
            double sx = max[0] - min[0], sy = max[1] - min[1], sz = max[2] - min[2];
            if (!(sx > 0) || !(sy > 0) || !(sz > 0) || double.IsInfinity(sx) || double.IsNaN(sx)) return plan;   // 정본 `box.isEmpty()` — 조각 0
            double cx = (min[0] + max[0]) * 0.5, cz = (min[2] + max[2]) * 0.5;
            double r = Math.Max(sx, sz) * 0.5;
            plan.R = r; plan.Motif = s.MotifOf(plan.Tier);
            double bandY = min[1] + sy * s.BandYK;
            plan.BandY = bandY;
            int tier = plan.Tier;

            // L1 갑주 밴드 — 토러스 → 큐브 링(XZ 수평이라 회전 불필요)
            {
                double bs = s.BandCell.Of(r);
                plan.Pieces.Add(new AscendDecorPiece
                {
                    Layer = 1, Name = "band", Size = bs, Center = true, Jitter = s.BandJitter,
                    Cells = VoxelShapes.Ring(r * s.BandRK / bs, s.BandT.Of(r / bs), (int)s.BandH, s.BandColor), Color = s.BandColor,
                    Pos = new[] { cx, bandY, cz },
                });
            }
            // L2 가시 — 밴드 바깥으로 눕혀 뻗는 큐브 테이퍼(+y 축을 바깥 방사 방향으로: rotation.z = −π/2 · rotation.y = −a)
            if (tier >= 2)
            {
                double ss = s.SpikeCell.Of(r);
                for (int i = 0; i < s.SpikeN; i++)
                {
                    double a = (double)i / s.SpikeN * Math.PI * 2;
                    plan.Pieces.Add(new AscendDecorPiece
                    {
                        Layer = 2, Name = "spike" + i, Size = ss, Center = true, Jitter = s.SpikeJitter,
                        Cells = VoxelShapes.Taper(s.SpikeR0.Of(r / ss), s.SpikeR1, s.SpikeH.Of(r / ss), s.SpikeColor), Color = s.SpikeColor,
                        Pos = new[] { cx + Math.Cos(a) * r * s.SpikeRadiusK, bandY, cz + Math.Sin(a) * r * s.SpikeRadiusK },
                        Rot = new[] { 0, -a, -Math.PI / 2 },
                    });
                }
            }
            // L3 룬 링 — 발밑 모티프색 문양 고리(평면 데칼 · 정본도 큐브 아님)
            if (tier >= 3)
            {
                plan.Pieces.Add(new AscendDecorPiece
                {
                    Layer = 3, Name = "rune", IsRune = true, RuneInner = r * s.RuneInK, RuneOuter = r * s.RuneOutK, RuneSegments = s.RuneSegments,
                    Opacity = s.RuneOpacity, Color = plan.Motif.Color,
                    Pos = new[] { cx, min[1] + s.RuneYOff, cz }, Rot = new[] { -Math.PI / 2, 0, 0 },
                });
            }
            // L4 금장 스터드 — 구 → 큐브 젬(발광)
            if (tier >= 4)
            {
                double us = s.StudCell.Of(r);
                for (int i = 0; i < s.StudN; i++)
                {
                    double a = (double)i / s.StudN * Math.PI * 2 + s.StudAngle0;
                    plan.Pieces.Add(new AscendDecorPiece
                    {
                        Layer = 4, Name = "stud" + i, Size = us, Center = true, Jitter = s.StudJitter,
                        Cells = VoxelShapes.Gem(s.StudR.Of(r / us), s.StudColor), Color = s.StudColor, Emissive = s.StudEmissive, EmissiveIntensity = s.StudEmissiveK,
                        Pos = new[] { cx + Math.Cos(a) * r * s.StudRadiusK, min[1] + sy * s.StudYK, cz + Math.Sin(a) * r * s.StudRadiusK },
                    });
                }
            }
            // L5 왕관 — 정수리 금관: 큐브 링 + 큐브 테이퍼 뿔(그룹 자리 (cx, max.y + cr×.35, cz) 를 조각 자리에 더해 둔다 · 그룹 회전 없음)
            if (tier >= 5)
            {
                double cr = s.CrownR.Of(r);
                double cs = s.CrownCell.Of(cr);
                double gy = max[1] + cr * s.CrownYK;
                plan.Pieces.Add(new AscendDecorPiece
                {
                    Layer = 5, Name = "crown-ring", Size = cs, Center = true, Jitter = s.CrownJitter,
                    Cells = VoxelShapes.Ring(cr / cs, s.CrownRingT.Of(cr / cs), (int)s.CrownRingH.Of(cr / cs), s.CrownColor), Color = s.CrownColor, Emissive = s.CrownEmissive, EmissiveIntensity = s.CrownEmissiveK,
                    Pos = new[] { cx, gy, cz },
                });
                for (int i = 0; i < s.CrownHorns; i++)
                {
                    double a = (double)i / s.CrownHorns * Math.PI * 2;
                    plan.Pieces.Add(new AscendDecorPiece
                    {
                        Layer = 5, Name = "crown-horn" + i, Size = cs, Center = false, Jitter = s.CrownJitter,
                        Cells = VoxelShapes.Taper(s.CrownHornR0.Of(cr / cs), s.CrownHornR1, s.CrownHornH.Of(cr / cs), s.CrownColor), Color = s.CrownColor, Emissive = s.CrownEmissive, EmissiveIntensity = s.CrownEmissiveK,
                        Pos = new[] { cx + Math.Cos(a) * cr, gy + cr * s.CrownHornYK, cz + Math.Sin(a) * cr },
                    });
                }
            }
            return plan;
        }
    }
}
