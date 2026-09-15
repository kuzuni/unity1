using System;
using System.Collections.Generic;
using System.Linq;
using Forge.Core.CraftFx;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>
    /// 시대 무늬(정본 `style.css` `--af-pat` 다섯 + `gp-*` 키프레임 · T124 · 주인 지시 `grade-pattern-animation`)의 수치표 —
    /// `Resources/AgePatternUi.json` 강타입. 무늬 자체(색·크기·커버리지)는 정본 값 그대로이고 움직임은 «타일 한 주기» 라 이음매가 없다.
    /// </summary>
    public sealed class AgePatternSpec
    {
        public double CellOpacity;
        /// <summary>막대 종류별 왼쪽 마스크(정본 `linear-gradient(90deg, transparent 0 X%, #000 Y%)`) — 표의 «&lt;종류&gt;_mask_from_f/_to_f» 한 벌이 한 칸이다. 키 = `af_bar`(자동 제련 행) · `fi_bar`(대장간 정보 팝업).</summary>
        public readonly Dictionary<string, MaskSpec> BarMasks = new Dictionary<string, MaskSpec>();
        public CssEase EaseInOut;
        public int Supersample, RingSegments;
        public readonly Dictionary<string, AgeSpec> Ages = new Dictionary<string, AgeSpec>();

        public bool Has(string age) { return age != null && Ages.ContainsKey(age); }
        /// <summary>막대 마스크 한 벌 — 없는 키(장착 셀처럼 마스크가 없는 자리)면 null.</summary>
        public MaskSpec Mask(string bar) { MaskSpec m; return bar != null && BarMasks.TryGetValue(bar, out m) ? m : null; }
        public AgeSpec Get(string age) { AgeSpec a; return age != null && Ages.TryGetValue(age, out a) ? a : null; }

        const string MaskFromSuffix = "_mask_from_f", MaskToSuffix = "_mask_to_f";

        public static AgePatternSpec From(JsonObject root)
        {
            var L = J.Obj(J.Require(root, "layout"));
            var s = new AgePatternSpec
            {
                CellOpacity = J.Num(J.Require(L, "cell_opacity_f")),
                Supersample = J.Int(J.Require(L, "supersample_n")), RingSegments = J.Int(J.Require(L, "ring_segments_n")),
            };
            var e = J.NumArr(J.Require(L, "ease_in_out"));
            s.EaseInOut = new CssEase(e[0], e[1], e[2], e[3]);
            // 마스크는 «막대 종류» 로 열려 있다 — 표에 «<종류>_mask_from_f» 가 늘면 짝(_to_f)까지 한 칸으로 읽는다(정본이 막대마다 다른 값을 적는다 · T380).
            for (int i = 0; i < L.Keys.Count; i++)
            {
                string k = L.Keys[i];
                if (!k.EndsWith(MaskFromSuffix, StringComparison.Ordinal)) continue;
                string bar = k.Substring(0, k.Length - MaskFromSuffix.Length);
                s.BarMasks[bar] = new MaskSpec(J.Num(L[k]), J.Num(J.Require(L, bar + MaskToSuffix)));
            }
            var ages = J.Obj(J.Require(root, "ages"));
            foreach (string key in ages.Keys) s.Ages[key] = AgeSpec.From(key, J.Obj(ages[key]));
            return s;
        }
    }

    /// <summary>막대 종류 키(표의 «&lt;종류&gt;_mask_from_f/_to_f» 앞머리) — 정본이 두 막대에 다른 마스크를 적었다(T380).</summary>
    public static class AgePatternKeys
    {
        /// <summary>자동 제련 행 막대 — 정본 `.af-age-bar::before` 30→50%.</summary>
        public const string AfBar = "af_bar";
        /// <summary>대장간 정보 팝업 막대 — 정본 `.fi-age-bar::before` 24→46%.</summary>
        public const string FiBar = "fi_bar";
    }

    /// <summary>막대 왼쪽 마스크 한 벌 — 정본 `linear-gradient(90deg, transparent 0 From, #000 To)`. 비율(0~1)이다.</summary>
    public sealed class MaskSpec
    {
        public readonly double From, To;
        public MaskSpec(double from, double to) { From = from; To = to; }
    }

    public sealed class AgeSpec
    {
        public string Age;
        public LayerSpec[] Layers = new LayerSpec[0];
        public double MoveS; public int StepsN;
        public PulseSpec Pulse;     // null 이면 밝기 고정
        public RingsSpec Rings;     // 양자만

        public static AgeSpec From(string age, JsonObject o)
        {
            var a = new AgeSpec { Age = age };
            object v;
            if (o.TryGet("layers", out v) && v != null) a.Layers = J.List(v, x => LayerSpec.From(J.Obj(x))).ToArray();
            if (o.TryGet("move_s", out v) && v != null) a.MoveS = J.Num(v);
            if (o.TryGet("steps_n", out v) && v != null) a.StepsN = J.Int(v);
            if (o.TryGet("pulse", out v) && v != null) { var p = J.Obj(v); a.Pulse = new PulseSpec { From = J.Num(J.Require(p, "from")), To = J.Num(J.Require(p, "to")), DurS = J.Num(J.Require(p, "dur_s")) }; }
            if (o.TryGet("rings", out v) && v != null)
            {
                var r = J.Obj(v);
                a.Rings = new RingsSpec { CenterF = J.NumArr(J.Require(r, "center_f")), OnRem = J.Num(J.Require(r, "on_rem")), PeriodRem = J.Num(J.Require(r, "period_rem")), Color = J.Str(J.Require(r, "color")), DurS = J.Num(J.Require(r, "dur_s")) };
            }
            if (a.Layers.Length == 0 && a.Rings == null) throw new FormatException("시대 «" + age + "» 에 layers 도 rings 도 없다");
            if (a.Layers.Length > 0 && !(a.MoveS > 0)) throw new FormatException("시대 «" + age + "» 의 move_s 가 0 이하다");
            return a;
        }
    }

    public sealed class LayerSpec
    {
        public string Kind, Axis, Color;
        public double[] TileRem, PosRem, MoveRem, SvgPx;
        public double RadiusRem, EdgeRem, OnRem, PeriodRem;
        public double[][] Points, Star, Places;

        public static LayerSpec From(JsonObject o)
        {
            var l = new LayerSpec { Kind = J.Str(J.Require(o, "kind")), Color = J.Str(J.Require(o, "color")), MoveRem = J.NumArr(J.Require(o, "move_rem")) };
            object v;
            if (o.TryGet("tile_rem", out v) && v != null) l.TileRem = J.NumArr(v);
            if (o.TryGet("pos_rem", out v) && v != null) l.PosRem = J.NumArr(v);
            if (o.TryGet("svg_px", out v) && v != null) l.SvgPx = J.NumArr(v);
            if (o.TryGet("radius_rem", out v) && v != null) l.RadiusRem = J.Num(v);
            if (o.TryGet("edge_rem", out v) && v != null) l.EdgeRem = J.Num(v);
            if (o.TryGet("on_rem", out v) && v != null) l.OnRem = J.Num(v);
            if (o.TryGet("period_rem", out v) && v != null) l.PeriodRem = J.Num(v);
            if (o.TryGet("axis", out v) && v != null) l.Axis = J.Str(v);
            if (o.TryGet("points", out v) && v != null) l.Points = J.List(v, x => J.NumArr(x)).ToArray();
            if (o.TryGet("star", out v) && v != null) l.Star = J.List(v, x => J.NumArr(x)).ToArray();
            if (o.TryGet("places", out v) && v != null) l.Places = J.List(v, x => J.NumArr(x)).ToArray();
            switch (l.Kind)
            {
                case "dot": if (l.TileRem == null || !(l.RadiusRem > 0) || !(l.EdgeRem >= l.RadiusRem)) throw new FormatException("dot 층 표가 모자란다"); break;
                case "bands": if (!(l.OnRem > 0) || !(l.PeriodRem > l.OnRem) || (l.Axis != "x" && l.Axis != "y")) throw new FormatException("bands 층 표가 모자란다"); break;
                case "poly": if (l.TileRem == null || l.SvgPx == null || l.Points == null || l.Points.Length < 3) throw new FormatException("poly 층 표가 모자란다"); break;
                case "stars": if (l.TileRem == null || l.SvgPx == null || l.Star == null || l.Places == null) throw new FormatException("stars 층 표가 모자란다"); break;
                default: throw new FormatException("모르는 무늬 종류: " + l.Kind);
            }
            if (l.PosRem == null) l.PosRem = new[] { 0.0, 0.0 };
            return l;
        }

        /// <summary>타일 한 칸(rem) — bands 는 주기 × 1(다른 축은 얇은 띠 하나).</summary>
        public double TileW { get { return Kind == "bands" ? (Axis == "x" ? PeriodRem : PeriodRem) : TileRem[0]; } }
        public double TileH { get { return Kind == "bands" ? PeriodRem : TileRem[1]; } }
    }

    public sealed class PulseSpec { public double From, To, DurS; }
    public sealed class RingsSpec { public double[] CenterF; public double OnRem, PeriodRem, DurS; public string Color; }

    /// <summary>
    /// 정본 `gp-*` 키프레임의 셈(UnityEngine 0): 진행(linear · `steps(n, end)`) · 층별 자리(`background-position` 시작 + 이동 × 진행) ·
    /// 반짝임/숨쉬기(`infinite alternate ease-in-out`) · 양자 파문 위상(`--gp-q`) · 무늬 래스터(정본 그라디언트·SVG 정의를 픽셀 커버리지로).
    /// </summary>
    public static class AgePatternRules
    {
        public static double Frac(double x) { return x - Math.Floor(x); }

        /// <summary>한 주기 안의 진행 0~1. `steps(n, end)` 는 구간 끝에서 뛴다(값 = floor(u·n)/n).</summary>
        public static double Progress(double tMs, double durS, int stepsN)
        {
            if (!(durS > 0)) return 0;
            double u = Frac(tMs / (durS * 1000.0));
            if (stepsN > 0) u = Math.Floor(u * stepsN) / stepsN;
            return u;
        }

        /// <summary>층의 무늬 자리(rem · CSS `background-position` 뜻: +x 는 오른쪽 · +y 는 아래) = 시작 + 이동 × 진행.</summary>
        public static void Shift(AgeSpec a, int layer, double tMs, out double xRem, out double yRem)
        {
            LayerSpec l = a.Layers[layer];
            double u = Progress(tMs, a.MoveS, a.StepsN);
            xRem = l.PosRem[0] + l.MoveRem[0] * u;
            yRem = l.PosRem[1] + l.MoveRem[1] * u;
        }

        /// <summary>`animation: … Xs ease-in-out infinite alternate` — 앞으로 dur · 뒤로 dur, 매 회 이징을 다시 건다. 표가 없으면 1.</summary>
        public static double Pulse(AgePatternSpec s, AgeSpec a, double tMs)
        {
            if (a.Pulse == null) return 1;
            double u = Frac(tMs / (a.Pulse.DurS * 2000.0)) * 2.0;
            double p = u <= 1 ? u : 2 - u;
            return a.Pulse.From + (a.Pulse.To - a.Pulse.From) * s.EaseInOut.Ease(p);
        }

        /// <summary>양자 파문 위상(rem) — 링 간격 한 주기를 linear 로 돈다(`gp-quantum` 0 → .84rem).</summary>
        public static double RingPhaseRem(AgeSpec a, double tMs)
        {
            if (a.Rings == null) return 0;
            return Frac(tMs / (a.Rings.DurS * 1000.0)) * a.Rings.PeriodRem;
        }

        /// <summary>반지름 r(rem)에 흰 링이 있는가 — `repeating-radial-gradient` 는 첫 스톱 앞뒤로도 되풀이된다(위상 q 부터 on 만큼 · 주기 P).</summary>
        public static bool RingOn(AgeSpec a, double rRem, double phaseRem)
        {
            double P = a.Rings.PeriodRem;
            double f = Frac((rRem - phaseRem) / P) * P;
            return f < a.Rings.OnRem;
        }

        /// <summary>k 번째 링의 안·바깥 반지름(rem) — k 는 음수도 된다(위상보다 안쪽 링).</summary>
        public static void Ring(AgeSpec a, int k, double phaseRem, out double inner, out double outer)
        {
            inner = phaseRem + k * a.Rings.PeriodRem;
            outer = inner + a.Rings.OnRem;
        }

        /// <summary>타일 픽셀 크기(가로·세로) — rem × 픽셀/rem 을 반올림(최소 1).</summary>
        public static void TilePx(LayerSpec l, double pxPerRem, out int w, out int h)
        {
            w = Math.Max(1, (int)Math.Round(l.TileW * pxPerRem));
            h = Math.Max(1, (int)Math.Round(l.TileH * pxPerRem));
            if (l.Kind == "bands") { if (l.Axis == "x") h = 4; else w = 4; }
        }

        /// <summary>
        /// 층 하나를 타일 한 칸으로 래스터한다 — 픽셀마다 «칠해진 비율»(0~1 · 행 우선 · y 는 위에서 아래). 색은 호출자가 곱한다.
        /// dot = 하드 엣지 원(반지름~엣지 사이만 선형) · bands = 축 방향 띠 · poly/stars = SVG 다각형(짝홀 규칙 · 슈퍼샘플).
        /// </summary>
        public static float[] Raster(AgePatternSpec s, LayerSpec l, double pxPerRem, out int w, out int h)
        {
            TilePx(l, pxPerRem, out w, out h);
            var a = new float[w * h];
            int n = Math.Max(1, s.Supersample);
            double sw = l.SvgPx != null ? l.SvgPx[0] / w : 1, sh = l.SvgPx != null ? l.SvgPx[1] / h : 1;
            double R = l.RadiusRem * pxPerRem, E = l.EdgeRem * pxPerRem, cx = w * 0.5, cy = h * 0.5;
            double period = l.PeriodRem * pxPerRem, on = l.OnRem * pxPerRem;
            List<double[][]> polys = null;
            if (l.Kind == "poly") polys = new List<double[][]> { l.Points };
            else if (l.Kind == "stars") { polys = new List<double[][]>(); foreach (var pl in l.Places) polys.Add(Placed(l.Star, pl[0], pl[1], pl[2], pl[3])); }
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    double acc = 0;
                    if (l.Kind == "dot")
                    {
                        double px = x + 0.5, py = y + 0.5;
                        double r = Math.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
                        acc = r <= R ? 1 : (r >= E ? 0 : (E - r) / (E - R));
                        a[y * w + x] = (float)acc;
                        continue;
                    }
                    for (int sy = 0; sy < n; sy++)
                    {
                        for (int sx = 0; sx < n; sx++)
                        {
                            double px = x + (sx + 0.5) / n, py = y + (sy + 0.5) / n;
                            bool inside;
                            if (l.Kind == "bands")
                            {
                                double c = l.Axis == "x" ? px : py;
                                inside = Frac(c / period) * period < on;
                            }
                            else
                            {
                                double ux = px * sw, uy = py * sh;
                                inside = false;
                                foreach (var poly in polys) if (InPolygon(poly, ux, uy)) { inside = true; break; }
                            }
                            if (inside) acc += 1;
                        }
                    }
                    a[y * w + x] = (float)(acc / (n * n));
                }
            }
            return a;
        }

        /// <summary>SVG `transform="translate(tx,ty) scale(s) rotate(deg)"` — 점에는 회전 → 배율 → 이동 순으로 걸린다.</summary>
        public static double[][] Placed(double[][] star, double tx, double ty, double sc, double deg)
        {
            double rad = deg * Math.PI / 180.0, c = Math.Cos(rad), si = Math.Sin(rad);
            var outp = new double[star.Length][];
            for (int i = 0; i < star.Length; i++)
            {
                double x = star[i][0], y = star[i][1];
                double rx = x * c - y * si, ry = x * si + y * c;
                outp[i] = new[] { tx + rx * sc, ty + ry * sc };
            }
            return outp;
        }

        /// <summary>짝홀 규칙(SVG 기본 fill-rule 은 nonzero 지만 이 다각형들은 자기 교차가 없어 같다).</summary>
        public static bool InPolygon(double[][] poly, double x, double y)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                double xi = poly[i][0], yi = poly[i][1], xj = poly[j][0], yj = poly[j][1];
                if (((yi > y) != (yj > y)) && (x < (xj - xi) * (y - yi) / (yj - yi) + xi)) inside = !inside;
            }
            return inside;
        }

        /// <summary>커버리지(칠해진 비율 평균) — 정본 주석의 실측치(항성간 ≈7% · 천상 ≈27%)와 대조하는 자.</summary>
        public static double Coverage(float[] a)
        {
            if (a == null || a.Length == 0) return 0;
            double s = 0; foreach (float v in a) s += v; return s / a.Length;
        }
    }
}
