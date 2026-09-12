using System;

namespace Forge.Core.World
{
    /// <summary>
    /// three r128 <c>Color</c> 의 수학을 double 그대로 옮긴 값 형(T9). 정본 `setTheme` 이 쓰는 면만: hex ↔ rgb · lerp · getHSL/setHSL/offsetHSL · multiplyScalar.
    /// r/g/b 는 0..1 (sRGB 값 — three r128 은 색 관리 없이 hex 를 그대로 둔다 · 결정 4). <see cref="Hex"/> 는 three 의 `getHex`(×255 뒤 **버림**)와 같다.
    /// </summary>
    public struct Col
    {
        public double R, G, B;

        public Col(double r, double g, double b) { R = r; G = g; B = b; }

        public static Col FromHex(int hex)
        {
            return new Col(((hex >> 16) & 255) / 255.0, ((hex >> 8) & 255) / 255.0, (hex & 255) / 255.0);
        }

        /// <summary>three `getHex`: `(r*255)<<16 ^ (g*255)<<8 ^ (b*255)` — ToInt32 는 0 쪽으로 버린다.</summary>
        public int Hex
        {
            get { return (Trunc(R * 255) << 16) ^ (Trunc(G * 255) << 8) ^ Trunc(B * 255); }
        }

        private static int Trunc(double v) { return (int)Math.Truncate(v); }

        public Col Lerp(Col to, double a)
        {
            return new Col(R + (to.R - R) * a, G + (to.G - G) * a, B + (to.B - B) * a);
        }

        public Col MultiplyScalar(double k) { return new Col(R * k, G * k, B * k); }

        /// <summary>three `getHSL`.</summary>
        public void GetHsl(out double h, out double s, out double l)
        {
            double max = Math.Max(R, Math.Max(G, B)), min = Math.Min(R, Math.Min(G, B));
            l = (min + max) / 2.0;
            if (min == max) { h = 0; s = 0; return; }
            double d = max - min;
            s = l <= 0.5 ? d / (max + min) : d / (2 - max - min);
            if (max == R) h = (G - B) / d + (G < B ? 6 : 0);
            else if (max == G) h = (B - R) / d + 2;
            else h = (R - G) / d + 4;
            h /= 6;
        }

        public static double EuclideanModulo(double n, double m) { return ((n % m) + m) % m; }
        public static double Clamp(double v, double lo, double hi) { return Math.Max(lo, Math.Min(hi, v)); }

        private static double Hue2Rgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6) return p + (q - p) * 6 * t;
            if (t < 1.0 / 2) return q;
            if (t < 2.0 / 3) return p + (q - p) * 6 * (2.0 / 3 - t);
            return p;
        }

        /// <summary>three `setHSL` — h 는 1 로 되감고 s·l 은 0..1 로 자른다.</summary>
        public static Col FromHsl(double h, double s, double l)
        {
            h = EuclideanModulo(h, 1);
            s = Clamp(s, 0, 1);
            l = Clamp(l, 0, 1);
            if (s == 0) return new Col(l, l, l);
            double p = l <= 0.5 ? l * (1 + s) : l + s - (l * s);
            double q = 2 * l - p;
            return new Col(Hue2Rgb(q, p, h + 1.0 / 3), Hue2Rgb(q, p, h), Hue2Rgb(q, p, h - 1.0 / 3));
        }

        /// <summary>three `offsetHSL`.</summary>
        public Col OffsetHsl(double dh, double ds, double dl)
        {
            double h, s, l;
            GetHsl(out h, out s, out l);
            return FromHsl(h + dh, s + ds, l + dl);
        }

        /// <summary>상대 휘도(정본 `soilOf` 의 Y).</summary>
        public double Luma { get { return R * 0.2126 + G * 0.7152 + B * 0.0722; } }

        public override string ToString() { return "#" + Hex.ToString("x6") + " (" + R.ToString("0.####") + ", " + G.ToString("0.####") + ", " + B.ToString("0.####") + ")"; }
    }
}
