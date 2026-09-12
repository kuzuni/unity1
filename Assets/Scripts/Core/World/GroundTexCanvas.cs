using System;
using System.Collections.Generic;

namespace Forge.Core.World
{
    /// <summary>
    /// 캔버스 2D 의 부분집합을 **순수 픽셀 버퍼**로 — 정본 `scene3d.js` 의 지면 소재 굽기(`makeGroundTexture` 등)가 쓰는 것만.
    /// `tools/ground_vectors.js` 의 `Context2D` 와 **같은 래스터 규칙**이다(둘을 함께 바꾼다):
    /// 픽셀당 2×2 표본(0.25/0.75) · 프리멀티플라이드 double · source-over/destination-out · 그라디언트는 CTM 사용자 좌표에서
    /// 프리멀티플라이드 선형 보간 · 선은 butt/round 캡 + 항상 둥근 이음 · 변환은 translate·scale 만 · 8비트 양자화는
    /// <see cref="GetImageData"/>/<see cref="PutImageData"/> 에서만(짝수 반올림 = Uint8ClampedArray).
    /// </summary>
    public sealed class GroundTexCanvas
    {
        public struct Rgba
        {
            public double R, G, B, A;
            public Rgba(double r, double g, double b, double a) { R = r; G = g; B = b; A = a; }
            /// <summary>`rgba(r,g,b,a)` 문자열 인자와 같은 뜻(0~255 · a 0~1).</summary>
            public static Rgba Bytes(double r, double g, double b, double a = 1) { return new Rgba(r / 255.0, g / 255.0, b / 255.0, a); }
            public static Rgba Hex(int hex) { return new Rgba(((hex >> 16) & 255) / 255.0, ((hex >> 8) & 255) / 255.0, (hex & 255) / 255.0, 1); }
            public Rgba Premultiplied { get { return new Rgba(R * A, G * A, B * A, A); } }
        }

        public sealed class Gradient
        {
            readonly bool _radial;
            readonly double _x0, _y0, _r0, _x1, _y1, _r1;
            readonly List<KeyValuePair<double, Rgba>> _stops = new List<KeyValuePair<double, Rgba>>();
            Gradient(bool radial, double x0, double y0, double r0, double x1, double y1, double r1) { _radial = radial; _x0 = x0; _y0 = y0; _r0 = r0; _x1 = x1; _y1 = y1; _r1 = r1; }
            public static Gradient Radial(double x0, double y0, double r0, double x1, double y1, double r1) { return new Gradient(true, x0, y0, r0, x1, y1, r1); }
            public static Gradient Linear(double x0, double y0, double x1, double y1) { return new Gradient(false, x0, y0, 0, x1, y1, 0); }
            public Gradient AddColorStop(double t, Rgba c) { _stops.Add(new KeyValuePair<double, Rgba>(t, c)); return this; }

            /// <summary>사용자 좌표에서 프리멀티플라이드 색.</summary>
            public Rgba At(double ux, double uy)
            {
                double t;
                if (_radial)
                {
                    double d = Math.Sqrt((ux - _x1) * (ux - _x1) + (uy - _y1) * (uy - _y1));
                    t = _r1 > _r0 ? (d - _r0) / (_r1 - _r0) : 1;
                }
                else
                {
                    double dx = _x1 - _x0, dy = _y1 - _y0, L = dx * dx + dy * dy;
                    t = L > 0 ? ((ux - _x0) * dx + (uy - _y0) * dy) / L : 0;
                }
                if (!(t > 0)) t = 0; else if (t > 1) t = 1;
                if (_stops.Count == 0) return new Rgba(0, 0, 0, 0);
                if (t <= _stops[0].Key) return _stops[0].Value.Premultiplied;
                for (int i = 1; i < _stops.Count; i++)
                {
                    if (t <= _stops[i].Key)
                    {
                        var a = _stops[i - 1]; var b = _stops[i];
                        double u = b.Key > a.Key ? (t - a.Key) / (b.Key - a.Key) : 0;
                        Rgba pa = a.Value.Premultiplied, pb = b.Value.Premultiplied;
                        return new Rgba(pa.R + (pb.R - pa.R) * u, pa.G + (pb.G - pa.G) * u, pa.B + (pb.B - pa.B) * u, pa.A + (pb.A - pa.A) * u);
                    }
                }
                return _stops[_stops.Count - 1].Value.Premultiplied;
            }
        }

        struct State { public Rgba Fill, Stroke; public Gradient FillGrad; public double LineWidth; public bool RoundCap; public bool DestOut; public double Sx, Sy, Tx, Ty; }
        sealed class Shape
        {
            public List<double[]> Pts;                    // 다각형/폴리라인(캔버스 좌표)
            public bool Closed;
            public bool IsEllipse; public double Ex, Ey, Rx, Ry, Rot, Sx, Sy, Tx, Ty;   // 타원(사용자 좌표 + 그때 CTM)
        }

        public readonly int Width, Height;
        readonly double[] _buf;    // 프리멀티플라이드 r,g,b,a
        State _s;
        readonly Stack<State> _stack = new Stack<State>();
        readonly List<Shape> _path = new List<Shape>();

        public GroundTexCanvas(int width, int height)
        {
            Width = width; Height = height;
            _buf = new double[width * height * 4];
            _s = new State { Fill = Rgba.Hex(0), Stroke = Rgba.Hex(0), LineWidth = 1, Sx = 1, Sy = 1 };
        }

        // ---- 상태 ----
        public Rgba FillStyle { set { _s.Fill = value; _s.FillGrad = null; } }
        public Gradient FillGradient { set { _s.FillGrad = value; } }
        public Rgba StrokeStyle { set { _s.Stroke = value; } }
        public double LineWidth { set { _s.LineWidth = value; } }
        /// <summary>true = 'round' · false = 'butt'.</summary>
        public bool LineCapRound { set { _s.RoundCap = value; } }
        /// <summary>lineJoin — 시밍은 언제나 둥근 이음이라 값을 받기만 한다(정본 호출 자리를 남기려고).</summary>
        public bool LineJoinRound { set { } }
        /// <summary>true = 'destination-out' · false = 'source-over'.</summary>
        public bool DestinationOut { set { _s.DestOut = value; } }

        public void Save() { _stack.Push(_s); }
        public void Restore() { if (_stack.Count > 0) _s = _stack.Pop(); }
        public void Translate(double x, double y) { _s.Tx += x * _s.Sx; _s.Ty += y * _s.Sy; }
        public void Scale(double x, double y) { _s.Sx *= x; _s.Sy *= y; }
        double[] T(double x, double y) { return new[] { x * _s.Sx + _s.Tx, y * _s.Sy + _s.Ty }; }

        // ---- 경로 ----
        public void BeginPath() { _path.Clear(); }
        public void ClosePath() { Shape s = _path.Count > 0 ? _path[_path.Count - 1] : null; if (s != null && s.Pts != null) s.Closed = true; }
        public void MoveTo(double x, double y) { _path.Add(new Shape { Pts = new List<double[]> { T(x, y) } }); }
        public void LineTo(double x, double y)
        {
            Shape s = _path.Count > 0 ? _path[_path.Count - 1] : null;
            if (s == null || s.Pts == null) MoveTo(x, y); else s.Pts.Add(T(x, y));
        }
        public void Ellipse(double x, double y, double rx, double ry, double rot)
        {
            _path.Add(new Shape { IsEllipse = true, Ex = x, Ey = y, Rx = rx, Ry = ry, Rot = rot, Sx = _s.Sx, Sy = _s.Sy, Tx = _s.Tx, Ty = _s.Ty });
        }
        public void Arc(double x, double y, double r) { Ellipse(x, y, r, r, 0); }

        public void ClearRect(double x, double y, double w, double h)
        {
            double[] a = T(x, y), b = T(x + w, y + h);
            int y0 = Math.Max(0, (int)Math.Floor(Math.Min(a[1], b[1]))), y1 = Math.Min(Height, (int)Math.Ceiling(Math.Max(a[1], b[1])));
            int x0 = Math.Max(0, (int)Math.Floor(Math.Min(a[0], b[0]))), x1 = Math.Min(Width, (int)Math.Ceiling(Math.Max(a[0], b[0])));
            for (int py = y0; py < y1; py++) for (int px = x0; px < x1; px++) { int i = (py * Width + px) * 4; _buf[i] = _buf[i + 1] = _buf[i + 2] = _buf[i + 3] = 0; }
        }

        // ---- 합성 ----
        void Paint(double bx0, double by0, double bx1, double by1, Func<double, double, bool> inside, bool useFill)
        {
            int x0 = Math.Max(0, (int)Math.Floor(bx0)), y0 = Math.Max(0, (int)Math.Floor(by0));
            int x1 = Math.Min(Width, (int)Math.Ceiling(bx1)), y1 = Math.Min(Height, (int)Math.Ceiling(by1));
            if (x1 <= x0 || y1 <= y0) return;
            Gradient grad = useFill ? _s.FillGrad : null;
            Rgba solid = (useFill ? _s.Fill : _s.Stroke).Premultiplied;
            bool outOp = _s.DestOut;
            double sx = _s.Sx, sy = _s.Sy, tx = _s.Tx, ty = _s.Ty;
            for (int py = y0; py < y1; py++)
                for (int px = x0; px < x1; px++)
                {
                    double r = 0, g = 0, b = 0, a = 0;
                    for (int k = 0; k < 4; k++)
                    {
                        double sxp = px + ((k & 1) != 0 ? 0.75 : 0.25), syp = py + ((k & 2) != 0 ? 0.75 : 0.25);
                        if (!inside(sxp, syp)) continue;
                        Rgba c = grad != null ? grad.At((sxp - tx) / sx, (syp - ty) / sy) : solid;
                        r += c.R; g += c.G; b += c.B; a += c.A;
                    }
                    if (a == 0) continue;
                    r *= 0.25; g *= 0.25; b *= 0.25; a *= 0.25;
                    int i = (py * Width + px) * 4;
                    double kk = 1 - a;
                    if (outOp) { _buf[i] *= kk; _buf[i + 1] *= kk; _buf[i + 2] *= kk; _buf[i + 3] *= kk; }
                    else { _buf[i] = r + _buf[i] * kk; _buf[i + 1] = g + _buf[i + 1] * kk; _buf[i + 2] = b + _buf[i + 2] * kk; _buf[i + 3] = a + _buf[i + 3] * kk; }
                }
        }

        public void FillRect(double x, double y, double w, double h)
        {
            double[] a = T(x, y), b = T(x + w, y + h);
            double x0 = Math.Min(a[0], b[0]), x1 = Math.Max(a[0], b[0]), y0 = Math.Min(a[1], b[1]), y1 = Math.Max(a[1], b[1]);
            Paint(x0, y0, x1, y1, (px, py) => px >= x0 && px < x1 && py >= y0 && py < y1, true);
        }

        public void Fill()
        {
            if (_path.Count == 0) return;
            double bx0 = double.PositiveInfinity, by0 = double.PositiveInfinity, bx1 = double.NegativeInfinity, by1 = double.NegativeInfinity;
            var tests = new List<Func<double, double, bool>>();
            foreach (Shape s in _path)
            {
                if (s.IsEllipse)
                {
                    double R = Math.Max(s.Rx, s.Ry);
                    double cx = s.Ex * s.Sx + s.Tx, cy = s.Ey * s.Sy + s.Ty;
                    bx0 = Math.Min(bx0, cx - R * Math.Abs(s.Sx)); bx1 = Math.Max(bx1, cx + R * Math.Abs(s.Sx));
                    by0 = Math.Min(by0, cy - R * Math.Abs(s.Sy)); by1 = Math.Max(by1, cy + R * Math.Abs(s.Sy));
                    double cr = Math.Cos(s.Rot), sr = Math.Sin(s.Rot);
                    Shape e = s;
                    tests.Add((px, py) =>
                    {
                        double ux = (px - e.Tx) / e.Sx - e.Ex, uy = (py - e.Ty) / e.Sy - e.Ey;
                        double lx = ux * cr + uy * sr, ly = -ux * sr + uy * cr;
                        double qx = lx / e.Rx, qy = ly / e.Ry;
                        return qx * qx + qy * qy <= 1;
                    });
                }
                else if (s.Pts.Count >= 3)
                {
                    List<double[]> P = s.Pts;
                    foreach (double[] p in P) { bx0 = Math.Min(bx0, p[0]); bx1 = Math.Max(bx1, p[0]); by0 = Math.Min(by0, p[1]); by1 = Math.Max(by1, p[1]); }
                    tests.Add((px, py) =>
                    {
                        bool inside = false;
                        for (int i = 0, j = P.Count - 1; i < P.Count; j = i++)
                        {
                            double xi = P[i][0], yi = P[i][1], xj = P[j][0], yj = P[j][1];
                            if ((yi > py) != (yj > py) && px < (xj - xi) * (py - yi) / (yj - yi) + xi) inside = !inside;
                        }
                        return inside;
                    });
                }
            }
            if (tests.Count == 0) return;
            Paint(bx0, by0, bx1, by1, (px, py) => { for (int i = 0; i < tests.Count; i++) if (tests[i](px, py)) return true; return false; }, true);
        }

        public void Stroke()
        {
            double hw = _s.LineWidth * _s.Sx / 2;
            bool round = _s.RoundCap;
            double bx0 = double.PositiveInfinity, by0 = double.PositiveInfinity, bx1 = double.NegativeInfinity, by1 = double.NegativeInfinity;
            var segs = new List<double[]>();     // ax, ay, bx, by
            var dots = new List<double[]>();
            foreach (Shape s in _path)
            {
                if (s.Pts == null || s.Pts.Count == 0) continue;
                List<double[]> P = s.Pts;
                foreach (double[] p in P) { bx0 = Math.Min(bx0, p[0] - hw); bx1 = Math.Max(bx1, p[0] + hw); by0 = Math.Min(by0, p[1] - hw); by1 = Math.Max(by1, p[1] + hw); }
                if (P.Count == 1) { if (round) dots.Add(P[0]); continue; }
                for (int i = 1; i < P.Count; i++) segs.Add(new[] { P[i - 1][0], P[i - 1][1], P[i][0], P[i][1] });
                for (int i = 1; i < P.Count - 1; i++) dots.Add(P[i]);
                if (s.Closed && P.Count >= 2) { segs.Add(new[] { P[P.Count - 1][0], P[P.Count - 1][1], P[0][0], P[0][1] }); dots.Add(P[0]); dots.Add(P[P.Count - 1]); }
            }
            if (segs.Count == 0 && dots.Count == 0) return;
            double hw2 = hw * hw;
            Func<double, double, bool> inside = (px, py) =>
            {
                for (int k = 0; k < segs.Count; k++)
                {
                    double[] g = segs[k];
                    double dx = g[2] - g[0], dy = g[3] - g[1], L = dx * dx + dy * dy;
                    if (L == 0) { if (round && (px - g[0]) * (px - g[0]) + (py - g[1]) * (py - g[1]) <= hw2) return true; continue; }
                    double t = ((px - g[0]) * dx + (py - g[1]) * dy) / L;
                    if (round) { if (t < 0) t = 0; else if (t > 1) t = 1; }
                    else if (t < 0 || t > 1) continue;
                    double qx = g[0] + dx * t - px, qy = g[1] + dy * t - py;
                    if (qx * qx + qy * qy <= hw2) return true;
                }
                for (int k = 0; k < dots.Count; k++) { double[] d = dots[k]; if ((px - d[0]) * (px - d[0]) + (py - d[1]) * (py - d[1]) <= hw2) return true; }
                return false;
            };
            Paint(bx0, by0, bx1, by1, inside, false);
        }

        // ---- 8비트 왕복 ----
        /// <summary>Uint8ClampedArray 대입 규칙 — 0~255 로 자르고 반올림은 짝수로.</summary>
        public static byte Clamp8(double x)
        {
            if (!(x > 0)) return 0;
            if (x >= 255) return 255;
            double f = Math.Floor(x), r = x - f;
            if (r < 0.5) return (byte)f;
            if (r > 0.5) return (byte)(f + 1);
            return ((int)f & 1) != 0 ? (byte)(f + 1) : (byte)f;
        }

        /// <summary>RGBA(스트레이트 알파) 바이트 — 정본 `getImageData(0,0,w,h).data` 와 같은 배치.</summary>
        public byte[] GetImageData()
        {
            var d = new byte[Width * Height * 4];
            for (int i = 0; i < d.Length; i += 4)
            {
                double a = _buf[i + 3];
                d[i + 3] = Clamp8(a * 255);
                if (a > 0) { d[i] = Clamp8(_buf[i] / a * 255); d[i + 1] = Clamp8(_buf[i + 1] / a * 255); d[i + 2] = Clamp8(_buf[i + 2] / a * 255); }
            }
            return d;
        }

        public void PutImageData(byte[] d)
        {
            for (int i = 0; i < d.Length; i += 4)
            {
                double a = d[i + 3] / 255.0;
                _buf[i] = d[i] / 255.0 * a; _buf[i + 1] = d[i + 1] / 255.0 * a; _buf[i + 2] = d[i + 2] / 255.0 * a; _buf[i + 3] = a;
            }
        }
    }
}
