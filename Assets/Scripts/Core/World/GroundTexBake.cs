using System;
using System.Collections.Generic;
using Forge.Core.Data;
using Cv = Forge.Core.World.GroundTexCanvas;
using Rgba = Forge.Core.World.GroundTexCanvas.Rgba;

namespace Forge.Core.World
{
    /// <summary>용암 균열 그물의 한 가닥 — 정본 `crackNetwork()` 의 `{pts(정규화 0~1), depth(0 주 혈관 · 1 지류 · 2 실핏줄)}`.</summary>
    public sealed class CrackSeg
    {
        public List<double[]> Pts = new List<double[]>();
        public int Depth;
    }

    /// <summary>
    /// 정본 `scene3d.js` 의 지면 소재 굽기(T34): `makeGroundTexture`(알베도 512 · kin 6 레시피 + `tintGround`) · `makeGroundNormalMap`
    /// (높이 캔버스 → 소벨 노멀 256) · `crackNetwork`/`strokeCrackNet`/`makeCrackTexture`(용암 발광 256) · `buildTerrain` 의 포석 줄눈 데칼
    /// (1024×256 · 알파). 난수 호출 **순서·횟수를 정본과 같게** 두었다(`tools/ground_vectors.js` 가 정본 함수를 같은 시드로 실제로 돌린
    /// 벡터와 EditMode 가 해시까지 대조한다). 수치는 정본 그리기 코드의 리터럴이고 표(`SceneDefs` 의 `CRACK_W`·`CRACK_A`·BIOMES tint)는 표에서.
    /// </summary>
    public sealed class GroundTexBake
    {
        public const int AlbedoSize = 512, NormalSize = 256, CrackSize = 256, CobbleWidth = 1024, CobbleHeight = 256;
        /// <summary>알베도·노멀의 지면 반복(정본 `tex.repeat.set(12, 6)`).</summary>
        public const double RepeatX = 12, RepeatY = 6;
        /// <summary>포석 줄눈 데칼 반복(정본 `ptex.repeat.set(2, 1)`) · 평면 60 × 4.70 · y 0.02.</summary>
        public const double CobbleRepeatX = 2, CobblePlaneWidth = 60, CobblePlaneDepth = 4.70, CobbleY = 0.02;

        readonly SceneDefs _defs;
        public GroundTexBake(SceneDefs defs) { if (defs == null) throw new ArgumentNullException("defs"); _defs = defs; }

        static readonly double TwoPi = Math.PI * 2;
        static double Floor(double v) { return Math.Floor(v); }
        /// <summary>JS `x | 0` (양수 소수 → 내림 · 음수 → 0 쪽으로).</summary>
        static int Or0(double v) { return (int)Math.Truncate(v); }
        static Rgba RgbaFixed3(double r, double g, double b, double a) { return Rgba.Bytes(r, g, b, JsNum.ParseFloat(JsNum.ToFixed(a, 3))); }

        // ================================================================= 알베도
        /// <summary>`makeGroundTexture(biome)` — RGBA 512×512(불투명). `net` 은 용암 kin 에만 쓰인다(`crackNetwork()` 공유).</summary>
        public byte[] Albedo(string biome, IReadOnlyList<CrackSeg> net, Rng rng)
        {
            string kin = _defs.Kin(biome);
            const int size = AlbedoSize;
            var ctx = new Cv(size, size);
            ctx.FillStyle = Rgba.Hex(0xc2c2c2);
            ctx.FillRect(0, 0, size, size);
            Action<Action> tile9 = draw =>
            {
                for (int ox = -1; ox <= 1; ox++) for (int oy = -1; oy <= 1; oy++) { ctx.Save(); ctx.Translate(ox * size, oy * size); draw(); ctx.Restore(); }
            };
            // 공용: 큰 색조 패치
            Action<int, double> patches = (n, alpha) =>
            {
                var list = new List<double[]>();
                for (int i = 0; i < n; i++)
                {
                    double x = rng.Random() * size, y = rng.Random() * size, r = 40 + rng.Random() * 110;
                    bool warm = rng.Random() < 0.5;
                    double bs = 130 + rng.Random() * 85;
                    double cr = warm ? bs + 28 : bs - 16, cg = bs, cb = warm ? bs - 30 : bs + 18;
                    double ry = r * (0.45 + rng.Random() * 0.5), rot = rng.Random() * Math.PI;
                    double a = alpha + rng.Random() * 0.2;
                    list.Add(new[] { x, y, r, ry, rot, Or0(cr), Or0(cg), Or0(cb), a });
                }
                tile9(() =>
                {
                    foreach (double[] p in list)
                    {
                        ctx.FillStyle = Rgba.Bytes(p[5], p[6], p[7], p[8]);
                        ctx.BeginPath();
                        ctx.Ellipse(p[0], p[1], p[2], p[3], p[4]);
                        ctx.Fill();
                    }
                });
            };
            // 짧은 스트로크(눈 바람결)
            Action<int, double, double, double, double, double, double> strokes = (n, len, w, ang, spread, light, dark) =>
            {
                ctx.LineCapRound = true;
                var list = new List<double[]>();
                for (int i = 0; i < n; i++)
                {
                    double x = rng.Random() * size, y = rng.Random() * size;
                    double a = ang + (rng.Random() - 0.5) * spread;
                    double l = len * (0.6 + rng.Random() * 0.8);
                    double v = rng.Random() < 0.5 ? light : dark;
                    double al = 0.22 + rng.Random() * 0.26;
                    double ww = w * (0.7 + rng.Random() * 0.6);
                    list.Add(new[] { x, y, a, l, v, al, ww });
                }
                tile9(() =>
                {
                    foreach (double[] t in list)
                    {
                        ctx.StrokeStyle = Rgba.Bytes(t[4], t[4], t[4], t[5]);
                        ctx.LineWidth = t[6];
                        ctx.BeginPath();
                        ctx.MoveTo(t[0], t[1]);
                        ctx.LineTo(t[0] + Math.Cos(t[2]) * t[3], t[1] + Math.Sin(t[2]) * t[3]);
                        ctx.Stroke();
                    }
                });
            };
            switch (kin)
            {
                case "desert": AlbedoDesert(ctx, size, tile9, patches, rng); break;
                case "rock": AlbedoRock(ctx, size, tile9, patches, rng); break;
                case "snow":
                    {
                        patches(14, 0.1);
                        strokes(90, 46, 2.4, -0.25, 0.18, 224, 156);
                        var sparks = new List<double[]>();
                        for (int i = 0; i < 240; i++)
                        {
                            double x = rng.Random() * size, y = rng.Random() * size;
                            double a = 0.5 + rng.Random() * 0.5;
                            double w = rng.Random() < 0.8 ? 1.4 : 2.2;
                            sparks.Add(new[] { x, y, a, w });
                        }
                        tile9(() => { foreach (double[] sp in sparks) { ctx.FillStyle = Rgba.Bytes(255, 255, 255, sp[2]); ctx.FillRect(sp[0], sp[1], sp[3], 1.4); } });
                        break;
                    }
                case "lava": AlbedoLava(ctx, size, tile9, patches, net, rng); break;
                case "magic":
                    {
                        patches(30, 0.22);
                        var motes = new List<double[]>();
                        for (int i = 0; i < 130; i++)
                        {
                            double x = rng.Random() * size, y = rng.Random() * size;
                            int v = Or0(190 + rng.Random() * 65);
                            double a = 0.25 + rng.Random() * 0.4;
                            motes.Add(new[] { x, y, v, a });
                        }
                        tile9(() => { foreach (double[] m in motes) { ctx.FillStyle = Rgba.Bytes(m[2] - 40, m[2], m[2], m[3]); ctx.FillRect(m[0], m[1], 1.6, 1.6); } });
                        break;
                    }
                default:
                    {
                        // forest kin: 마른 흙 — 얼룩 + 잔자갈·흙덩이(축정렬 사각 · 사선 금지)
                        patches(30, 0.18);
                        var grit = new List<double[]>();
                        for (int i = 0; i < 300; i++)
                        {
                            int v = Or0(108 + rng.Random() * 96);
                            double x = rng.Random() * size, y = rng.Random() * size, w = 1 + rng.Random() * 3, h = 1 + rng.Random() * 3;
                            double a = 0.16 + rng.Random() * 0.22;
                            grit.Add(new[] { x, y, w, h, v, v - 8, v - 20, a });
                        }
                        for (int i = 0; i < 110; i++)
                        {
                            int v = Or0(74 + rng.Random() * 46);
                            double x = rng.Random() * size, y = rng.Random() * size, w = 4 + rng.Random() * 11, h = 3 + rng.Random() * 8;
                            double a = 0.1 + rng.Random() * 0.14;
                            grit.Add(new[] { x, y, w, h, v, v - 6, v - 16, a });
                        }
                        tile9(() => { foreach (double[] t in grit) { ctx.FillStyle = Rgba.Bytes(t[4], t[5], t[6], t[7]); ctx.FillRect(t[0], t[1], t[2], t[3]); } });
                        break;
                    }
            }
            // 미세 스펙클 노이즈(전 바이옴 공통)
            byte[] img = ctx.GetImageData();
            double speck = kin == "snow" ? 14 : 30;
            for (int i = 0; i < img.Length; i += 4)
            {
                double n = (rng.Random() - 0.5) * speck;
                img[i] = Cv.Clamp8(Col.Clamp(img[i] + n, 0, 255));
                img[i + 1] = Cv.Clamp8(Col.Clamp(img[i + 1] + n, 0, 255));
                img[i + 2] = Cv.Clamp8(Col.Clamp(img[i + 2] + n, 0, 255));
            }
            TintGround(img, biome);
            return img;
        }

        void AlbedoDesert(Cv ctx, int size, Action<Action> tile9, Action<int, double> patches, Rng rng)
        {
            patches(22, 0.16);
            var ripples = new List<Ripple>();
            for (double y = -8; y < size + 8; y += 9 + rng.Random() * 7)
            {
                var rp = new Ripple { Y = y, Amp = 3 + rng.Random() * 4, Ph = rng.Random() * 9, Cyc = 10 + Or0(rng.Random() * 9) };
                ripples.Add(rp);
            }
            foreach (Ripple rp in ripples)
            {
                // 밴드 자신의 값에서 파생한 지역 난수기(정본 · Math.imul + xorshift) — 전역 난수를 더 안 먹는다
                uint s = unchecked((uint)(Imul(rp.Cyc, unchecked((int)2654435761u)) ^ Imul(Or0(rp.Ph * 1e6), unchecked((int)2246822519u)) ^ Imul(Or0(rp.Y * 1e3), unchecked((int)3266489917u))));
                Func<double> rnd = () => { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return s / 4294967296.0; };
                rp.Harm = new double[3][];
                rp.Harm[0] = new[] { 2 + Or0(rnd() * 4), 0.5 + rnd() * 0.45, rnd() * 9 };
                rp.Harm[1] = new[] { rp.Cyc, 0.42 + rnd() * 0.3, rp.Ph };
                rp.Harm[2] = new[] { 19 + Or0(rnd() * 15), 0.1 + rnd() * 0.14, rnd() * 9 };
                rp.Env = new[] { 1 + Or0(rnd() * 3), rnd() * 9, 0.3 + rnd() * 0.32 };
            }
            double[][] layers = { new[] { 2.6, 96, 88, 74, 3.2, 0.34 }, new[] { 0, 238, 232, 214, 2.1, 0.5 } };
            tile9(() =>
            {
                foreach (Ripple rp in ripples)
                {
                    Func<double, double> yAt = x =>
                    {
                        double d = 0;
                        foreach (double[] h in rp.Harm) d += h[1] * Math.Sin((x / size) * h[0] * Math.PI * 2 + h[2]);
                        return rp.Y + d * rp.Amp * 0.8;
                    };
                    Func<double, double> envAt = x =>
                    {
                        double e = (Math.Sin((x / size) * rp.Env[0] * Math.PI * 2 + rp.Env[1]) + 1) / 2;
                        return Math.Max(0, e - rp.Env[2]) / (1 - rp.Env[2]);
                    };
                    foreach (double[] L in layers)
                    {
                        double off = L[0], w = L[4], a0 = L[5];
                        ctx.LineWidth = w;
                        for (int x = 0; x < size; x += 7)
                        {
                            int x2 = Math.Min(x + 7, size);
                            double k = envAt((x + x2) / 2.0);
                            if (k <= 0.02) continue;
                            ctx.StrokeStyle = RgbaFixed3(L[1], L[2], L[3], a0 * k);
                            ctx.BeginPath();
                            ctx.MoveTo(x, yAt(x) + off);
                            ctx.LineTo(x2, yAt(x2) + off);
                            ctx.Stroke();
                        }
                    }
                }
            });
        }
        sealed class Ripple { public double Y, Amp, Ph; public int Cyc; public double[][] Harm; public double[] Env; }
        /// <summary>JS `Math.imul`.</summary>
        static int Imul(int a, int b) { return unchecked(a * b); }

        void AlbedoRock(Cv ctx, int size, Action<Action> tile9, Action<int, double> patches, Rng rng)
        {
            patches(26, 0.18);
            var plates = new List<Poly>();
            for (int i = 0; i < 30; i++)
            {
                double x = rng.Random() * size, y = rng.Random() * size, r = 26 + rng.Random() * 60;
                int v = Or0(118 + rng.Random() * 96);
                Rgba col = Rgba.Bytes(v, v, v + 6, 0.2 + rng.Random() * 0.22);
                int nv = 4 + Or0(rng.Random() * 3);
                var pts = new List<double[]>();
                for (int k = 0; k < nv; k++)
                {
                    double a = ((double)k / nv) * Math.PI * 2, rr = r * (0.6 + rng.Random() * 0.5);
                    pts.Add(new[] { x + Math.Cos(a) * rr, y + Math.Sin(a) * rr });
                }
                plates.Add(new Poly { Col = col, Pts = pts });
            }
            var cracks = new List<Poly>();
            for (int i = 0; i < 14; i++)
            {
                Rgba col = Rgba.Bytes(52, 50, 48, 0.3 + rng.Random() * 0.25);
                double w = 1.4 + rng.Random() * 1.2;
                double x = rng.Random() * size, y = rng.Random() * size, a = rng.Random() * Math.PI * 2;
                var pts = new List<double[]> { new[] { x, y } };
                for (int st = 0; st < 9; st++)
                {
                    a += (rng.Random() - 0.5) * 1.1;
                    x += Math.Cos(a) * 16; y += Math.Sin(a) * 16;
                    pts.Add(new[] { x, y });
                }
                cracks.Add(new Poly { Col = col, W = w, Pts = pts });
            }
            tile9(() =>
            {
                foreach (Poly p in plates) { ctx.FillStyle = p.Col; ctx.BeginPath(); Path(ctx, p.Pts); ctx.ClosePath(); ctx.Fill(); }
                foreach (Poly cr in cracks) { ctx.StrokeStyle = cr.Col; ctx.LineWidth = cr.W; ctx.BeginPath(); Path(ctx, cr.Pts); ctx.Stroke(); }
            });
        }
        sealed class Poly { public Rgba Col; public double W; public List<double[]> Pts; }
        static void Path(Cv ctx, List<double[]> pts) { for (int k = 0; k < pts.Count; k++) { if (k == 0) ctx.MoveTo(pts[k][0], pts[k][1]); else ctx.LineTo(pts[k][0], pts[k][1]); } }

        void AlbedoLava(Cv ctx, int size, Action<Action> tile9, Action<int, double> patches, IReadOnlyList<CrackSeg> net, Rng rng)
        {
            patches(16, 0.14);
            var cells = new List<Poly>();
            for (int i = 0; i < 64; i++)
            {
                double x = rng.Random() * size, y = rng.Random() * size, r = 14 + rng.Random() * 34;
                int v = Or0(96 + rng.Random() * 60);
                Rgba col = Rgba.Bytes(v, v - 4, v - 8, 0.3 + rng.Random() * 0.28);
                int nv = 5 + Or0(rng.Random() * 2);
                var pts = new List<double[]>();
                for (int k = 0; k < nv; k++)
                {
                    double a = ((double)k / nv) * Math.PI * 2 + 0.3, rr = r * (0.68 + rng.Random() * 0.4);
                    pts.Add(new[] { x + Math.Cos(a) * rr, y + Math.Sin(a) * rr });
                }
                cells.Add(new Poly { Col = col, Pts = pts });
            }
            tile9(() =>
            {
                foreach (Poly ce in cells)
                {
                    ctx.FillStyle = ce.Col;
                    ctx.BeginPath(); Path(ctx, ce.Pts); ctx.ClosePath(); ctx.Fill();
                    ctx.StrokeStyle = Rgba.Bytes(190, 182, 170, 0.3);
                    ctx.LineWidth = 1.6;
                    ctx.Stroke();
                }
            });
            // 발광 균열 자리의 암반 골: 크러스트 립(밝은 굳은 재) → 골 벽(그늘)
            StrokeCrackNet(ctx, size, net, new[] { 21.0, 19.0 }, d => Rgba.Bytes(176, 166, 150, 0.30), d => Rgba.Bytes(10, 8, 7, 0.93));
        }

        /// <summary>`tintGround` — 신설 바이옴만 HSL 을 튼다(원본 6종은 한 픽셀도 안 바뀐다). 바이트 배열을 제자리에서.</summary>
        public void TintGround(byte[] d, string biome)
        {
            BiomeSpec sp = _defs.Spec(biome);
            if (sp == null || sp.Tint == null) return;
            double dh = sp.Tint[0], ds = sp.Tint[1], dl = sp.Tint[2];
            for (int i = 0; i < d.Length; i += 4)
            {
                var col = new Col(d[i] / 255.0, d[i + 1] / 255.0, d[i + 2] / 255.0);
                double h, s, l; col.GetHsl(out h, out s, out l);
                Col c2 = Col.FromHsl((h + dh + 1) % 1, Col.Clamp(s + ds, 0, 1), Col.Clamp(l + dl, 0, 1));
                d[i] = Cv.Clamp8(c2.R * 255); d[i + 1] = Cv.Clamp8(c2.G * 255); d[i + 2] = Cv.Clamp8(c2.B * 255);
            }
        }

        // ================================================================= 노멀
        /// <summary>`makeGroundNormalMap(biome)` — 높이 캔버스(랩어라운드 3×3) → 소벨 → RGBA 256×256(B=255).</summary>
        public byte[] NormalMap(string biome, IReadOnlyList<CrackSeg> net, Rng rng)
        {
            string kin = _defs.Kin(biome);
            const int size = NormalSize;
            var ctx = new Cv(size, size);
            ctx.FillStyle = Rgba.Hex(0x808080);
            ctx.FillRect(0, 0, size, size);
            Action<Action> tile9 = draw =>
            {
                for (int ox = -1; ox <= 1; ox++) for (int oy = -1; oy <= 1; oy++) { ctx.Save(); ctx.Translate(ox * size, oy * size); draw(); ctx.Restore(); }
            };
            if (kin == "desert")
            {
                var bumps = new List<double[]>();
                for (int i = 0; i < 150; i++)
                {
                    double x = rng.Random() * size, y = rng.Random() * size, rx = 16 + rng.Random() * 26, ry = 2.2 + rng.Random() * 3;
                    bool up = rng.Random() < 0.62;
                    bumps.Add(new[] { x, y, rx, ry, up ? 1 : 0 });
                }
                tile9(() =>
                {
                    foreach (double[] b in bumps)
                    {
                        var grad = Cv.Gradient.Radial(b[0], b[1], 0, b[0], b[1], b[2])
                            .AddColorStop(0, b[4] > 0 ? Rgba.Bytes(255, 255, 255, 0.4) : Rgba.Bytes(0, 0, 0, 0.36))
                            .AddColorStop(1, Rgba.Bytes(128, 128, 128, 0));
                        ctx.FillGradient = grad;
                        ctx.Save(); ctx.Translate(b[0], b[1]); ctx.Scale(1, b[3] / b[2]);
                        ctx.BeginPath(); ctx.Arc(0, 0, b[2]); ctx.Fill();
                        ctx.Restore();
                    }
                });
            }
            else if (kin == "rock" || kin == "lava")
            {
                var plates = new List<Poly>();
                for (int i = 0; i < 70; i++)
                {
                    double x = rng.Random() * size, y = rng.Random() * size, r = 9 + rng.Random() * 22;
                    int v = Or0(92 + rng.Random() * 88);
                    int nv = 5 + Or0(rng.Random() * 2);
                    var pts = new List<double[]>();
                    for (int k = 0; k < nv; k++)
                    {
                        double a = ((double)k / nv) * Math.PI * 2 + 0.4, rr = r * (0.66 + rng.Random() * 0.4);
                        pts.Add(new[] { x + Math.Cos(a) * rr, y + Math.Sin(a) * rr });
                    }
                    plates.Add(new Poly { Col = Rgba.Bytes(v, v, v), Pts = pts });
                }
                tile9(() => { foreach (Poly p in plates) { ctx.FillStyle = p.Col; ctx.BeginPath(); Path(ctx, p.Pts); ctx.ClosePath(); ctx.Fill(); } });
                if (kin == "lava")
                    StrokeCrackNet(ctx, size, net, new[] { 13.0, 10.0, 7.0, 4.4, 2.4 },
                        d => Rgba.Bytes(210, 210, 210, 0.10), d => Rgba.Bytes(40, 40, 40, 0.10), d => Rgba.Bytes(36, 36, 36, 0.14),
                        d => Rgba.Bytes(32, 32, 32, 0.20), d => Rgba.Bytes(28, 28, 28, 0.26));
            }
            else
            {
                bool soft = kin == "snow";
                var knolls = new List<double[]>();
                int cnt = soft ? 60 : 110;
                for (int i = 0; i < cnt; i++)
                {
                    double x = rng.Random() * size, y = rng.Random() * size;
                    double r = (soft ? 12 : 5) + rng.Random() * (soft ? 34 : 20);
                    bool up = rng.Random() < 0.6;
                    knolls.Add(new[] { x, y, r, up ? 1 : 0 });
                }
                tile9(() =>
                {
                    foreach (double[] k in knolls)
                    {
                        var grad = Cv.Gradient.Radial(k[0], k[1], 0, k[0], k[1], k[2])
                            .AddColorStop(0, k[3] > 0 ? Rgba.Bytes(255, 255, 255, 0.32) : Rgba.Bytes(0, 0, 0, 0.3))
                            .AddColorStop(1, Rgba.Bytes(128, 128, 128, 0));
                        ctx.FillGradient = grad;
                        ctx.BeginPath(); ctx.Arc(k[0], k[1], k[2]); ctx.Fill();
                    }
                });
            }
            // 미세 거칠기
            byte[] img = ctx.GetImageData();
            double rough = kin == "snow" ? 16 : 34;
            for (int i = 0; i < img.Length; i += 4)
            {
                double n = (rng.Random() - 0.5) * rough;
                byte v = Cv.Clamp8(Col.Clamp(img[i] + n, 0, 255));
                img[i] = img[i + 1] = img[i + 2] = v;
            }
            // 높이맵 → 법선맵(소벨 · 랩어라운드)
            var outp = new byte[size * size * 4];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int dx = At(img, size, x + 1, y) - At(img, size, x - 1, y);
                    int dy = At(img, size, x, y + 1) - At(img, size, x, y - 1);
                    int idx = (y * size + x) * 4;
                    outp[idx] = Cv.Clamp8(Col.Clamp(128 - dx, 0, 255));
                    outp[idx + 1] = Cv.Clamp8(Col.Clamp(128 - dy, 0, 255));
                    outp[idx + 2] = 255;
                    outp[idx + 3] = 255;
                }
            return outp;
        }
        static int At(byte[] h, int size, int x, int y) { return h[(((y + size) % size) * size + ((x + size) % size)) * 4]; }

        // ================================================================= 균열
        /// <summary>`crackNetwork()` — 주 혈관 2 · 지류 2씩 · 실핏줄(60%) · 좌표 정규화 0~1. 게임은 한 번 만들어 세 맵이 공유한다.</summary>
        public static List<CrackSeg> CrackNetwork(Rng rng)
        {
            var segs = new List<CrackSeg>();
            Func<double, double, double, int, double, int, double, List<double[]>> walk = (x, y, a, steps, step, depth, wob) =>
            {
                var pts = new List<double[]> { new[] { x, y } };
                for (int s = 0; s < steps; s++)
                {
                    a += rng.Rand(-wob, wob);
                    x += Math.Cos(a) * step; y += Math.Sin(a) * step;
                    pts.Add(new[] { x, y });
                }
                segs.Add(new CrackSeg { Pts = pts, Depth = depth });
                return pts;
            };
            Func<List<double[]>, int, double> dirAt = (pts, k) => Math.Atan2(pts[k][1] - pts[k - 1][1], pts[k][0] - pts[k - 1][0]);
            for (int i = 0; i < 2; i++)
            {
                double mx = rng.Random(), my = rng.Random(), ma = rng.Random() * Math.PI * 2;
                int msteps = 9 + Or0(rng.Random() * 3);
                List<double[]> main = walk(mx, my, ma, msteps, 0.05, 0, 0.40);
                for (int b = 0; b < 2; b++)
                {
                    int k = 2 + Or0(rng.Random() * (main.Count - 4));
                    double br = dirAt(main, k) + (rng.Random() < 0.5 ? 1 : -1) * rng.Rand(0.5, 1.15);
                    int tsteps = 4 + Or0(rng.Random() * 3);
                    List<double[]> trib = walk(main[k][0], main[k][1], br, tsteps, 0.035, 1, 0.5);
                    if (rng.Random() < 0.6 && trib.Count > 3)
                    {
                        int k2 = 1 + Or0(rng.Random() * (trib.Count - 2));
                        double ba = dirAt(trib, k2) + (rng.Random() < 0.5 ? 1 : -1) * rng.Rand(0.6, 1.3);
                        int ssteps = 3 + Or0(rng.Random() * 2);
                        walk(trib[k2][0], trib[k2][1], ba, ssteps, 0.025, 2, 0.6);
                    }
                }
            }
            return segs;
        }

        /// <summary>`strokeCrackNet` — 층마다(바깥부터) 모든 가닥을 3×3 랩어라운드로 · 폭은 256 기준 × (size/256) × `CRACK_W[depth]`.</summary>
        void StrokeCrackNet(Cv ctx, int size, IReadOnlyList<CrackSeg> net, double[] widths, params Func<int, Rgba>[] cols)
        {
            double k = size / 256.0;
            double[] W = _defs.CrackW;
            ctx.LineCapRound = true;
            ctx.LineJoinRound = true;
            for (int L = 0; L < widths.Length; L++)
                for (int ox = -1; ox <= 1; ox++)
                    for (int oy = -1; oy <= 1; oy++)
                        foreach (CrackSeg sg in net)
                        {
                            double w = widths[L] * W[sg.Depth];
                            if (!(w > 0)) continue;
                            ctx.StrokeStyle = cols[L](sg.Depth);
                            ctx.LineWidth = w * k;
                            ctx.BeginPath();
                            for (int p = 0; p < sg.Pts.Count; p++)
                            {
                                double X = (sg.Pts[p][0] + ox) * size, Y = (sg.Pts[p][1] + oy) * size;
                                if (p == 0) ctx.MoveTo(X, Y); else ctx.LineTo(X, Y);
                            }
                            ctx.Stroke();
                        }
        }

        /// <summary>`makeCrackTexture()` — 검정 바탕 + 발광 주황 균열 4겹(열기 블리드 → 광 → 중심 광 → 백열 코어) · RGBA 256×256.</summary>
        public byte[] CrackTexture(IReadOnlyList<CrackSeg> net)
        {
            const int size = CrackSize;
            var ctx = new Cv(size, size);
            ctx.FillStyle = Rgba.Hex(0);
            ctx.FillRect(0, 0, size, size);
            double[] A = _defs.CrackA;
            StrokeCrackNet(ctx, size, net, new[] { 5.0, 4.0, 4.4, 1.9 },
                d => RgbaFixed3(255, 88, 20, 0.06 * A[d]),
                d => RgbaFixed3(255, 72, 0, 0.26 * A[d]),
                d => RgbaFixed3(255, 140, 30, 0.62 * A[d]),
                d => RgbaFixed3(255, 214, 150, 0.95 * A[d]));
            return ctx.GetImageData();
        }

        // ================================================================= 포석 줄눈 데칼
        /// <summary>`buildTerrain` 의 포석 줄눈 데칼 — 러닝 본드 줄눈(세로 20×4행 · 가로 3) + 위아래 페더링(네 변 알파 0) · RGBA 1024×256.</summary>
        public static byte[] CobbleDecal(Rng rng)
        {
            const int W = CobbleWidth, H = CobbleHeight;
            var ctx = new Cv(W, H);
            ctx.ClearRect(0, 0, W, H);
            double[] ROW = { 46, 87, 128, 169, 210 };
            Action<double, double, double, double, double> line = (x0, y0, x1, y1, a) =>
            {
                ctx.StrokeStyle = RgbaFixed3(38, 32, 26, a);
                ctx.LineWidth = 1.6 + rng.Random() * 0.9;
                foreach (double ox in new[] { 0.0, -1024, 1024 })
                {
                    ctx.BeginPath(); ctx.MoveTo(x0 + ox, y0); ctx.LineTo(x1 + ox, y1); ctx.Stroke();
                }
            };
            for (int r = 0; r < 4; r++)
            {
                double off = (r & 1) != 0 ? 25.6 : 0;
                double yA = ROW[r], yB = ROW[r + 1];
                for (int k = 0; k < 20; k++) line(off + k * 51.2, yA, off + k * 51.2, yB, 0.30 + rng.Random() * 0.12);
                if (r > 0) line(0, yA, 1024, yA, 0.22 + rng.Random() * 0.08);
            }
            ctx.DestinationOut = true;
            Action<double, double> fade = (y0, y1) =>
            {
                var g2 = Cv.Gradient.Linear(0, y0, 0, y1).AddColorStop(0, Rgba.Bytes(0, 0, 0, 1)).AddColorStop(1, Rgba.Bytes(0, 0, 0, 0));
                ctx.FillGradient = g2;
                ctx.FillRect(0, Math.Min(y0, y1), 1024, Math.Abs(y1 - y0));
            };
            fade(0, 46);
            fade(256, 210);
            ctx.DestinationOut = false;
            return ctx.GetImageData();
        }
    }
}
