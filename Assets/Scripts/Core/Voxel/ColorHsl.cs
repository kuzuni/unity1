using System;

namespace Forge.Core.Voxel
{
    /// <summary>
    /// three r128 `Color.getHSL / setHSL / getHex` 이식(색 관리 없음 · 결정 4) — `Mobs.build` 의 `vivid` 채도 보정이 이것을 쓴다.
    /// 게임 씬은 ACES + 낮은 광량이라 표의 색이 한 단계 씻기므로 **표를 흔들지 않고** 조립 경로에서만 채도·밝기를 조금 올린다.
    /// </summary>
    public static class ColorHsl
    {
        public static void GetHsl(int hex, out double h, out double s, out double l)
        {
            double r = ((hex >> 16) & 255) / 255.0, g = ((hex >> 8) & 255) / 255.0, b = (hex & 255) / 255.0;
            double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
            l = (min + max) / 2.0;
            if (min == max) { h = 0; s = 0; return; }
            double delta = max - min;
            s = l <= 0.5 ? delta / (max + min) : delta / (2 - max - min);
            if (max == r) h = (g - b) / delta + (g < b ? 6 : 0);
            else if (max == g) h = (b - r) / delta + 2;
            else h = (r - g) / delta + 4;
            h /= 6;
        }

        static double Hue2Rgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1 / 6.0) return p + (q - p) * 6 * t;
            if (t < 1 / 2.0) return q;
            if (t < 2 / 3.0) return p + (q - p) * 6 * (2 / 3.0 - t);
            return p;
        }

        static double EuclideanModulo(double n, double m) { return ((n % m) + m) % m; }

        /// <summary>HSL → 0xRRGGBB(three `setHSL` 뒤 `getHex` — 채널을 255 배 해 정수로 자른다).</summary>
        public static int FromHsl(double h, double s, double l)
        {
            h = EuclideanModulo(h, 1);
            s = Math.Max(0, Math.Min(1, s));
            l = Math.Max(0, Math.Min(1, l));
            double r, g, b;
            if (s == 0) { r = g = b = l; }
            else
            {
                double p = l <= 0.5 ? l * (1 + s) : l + s - (l * s);
                double q = 2 * l - p;
                r = Hue2Rgb(q, p, h + 1 / 3.0);
                g = Hue2Rgb(q, p, h);
                b = Hue2Rgb(q, p, h - 1 / 3.0);
            }
            return ((int)(r * 255) << 16) ^ ((int)(g * 255) << 8) ^ (int)(b * 255);
        }

        /// <summary>`Mobs.build` 의 vivid: 채도가 0.06 넘는 색만 `s + vivid`(≤1) · `l + vivid×0.22`(≤0.94). vivid 0 이면 그대로.</summary>
        public static int Vivid(int hex, double vivid)
        {
            if (vivid == 0) return hex;
            double h, s, l;
            GetHsl(hex, out h, out s, out l);
            if (s > 0.06) return FromHsl(h, Math.Min(1, s + vivid), Math.Min(0.94, l + vivid * 0.22));
            // 정본은 setHSL 을 안 거치면 setHex 한 값을 getHex 로 되돌린다 — 같은 정수다.
            return hex;
        }
    }
}
