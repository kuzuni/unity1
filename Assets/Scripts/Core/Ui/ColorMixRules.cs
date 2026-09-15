using System;

namespace Forge.Core.Ui
{
    /// <summary>
    /// T371 — 정본 CSS `color-mix(in srgb, A p%, B)` 를 옮기는 셈. 정본은 이것으로 **등급색(--rc)·가지색(--bc)** 에서
    /// 면·테·그림자 색을 만든다(style.css 24 자리). 규칙 셋만 지키면 값이 정본과 바이트로 같다:
    /// <list type="number">
    /// <item>공간은 **sRGB** — 리니어로 섞으면 밝아진다(결정 584 가 겹 합성에서 겪은 그 자리와 같은 함정).</item>
    /// <item>알파는 **미리 곱해**(premultiplied) 섞는다 — CSS 명세 그대로다. `transparent`(알파 0)와 섞으면 색은 그대로고 **알파만** p 로 준다.</item>
    /// <item>p 는 **앞 색 A 의 몫**이다. 정본 `58%` = A 0.58 + B 0.42.</item>
    /// </list>
    /// UnityEngine 참조 0 — 색은 0~255 바이트(채널)와 0~1 알파로 받는다(호출자가 Unity `Color` 와 오간다).
    /// </summary>
    public static class ColorMixRules
    {
        /// <summary>정본 `color-mix(in srgb, A f, B)` — f 는 A 의 몫(0~1). 채널은 0~255, 알파는 0~1.</summary>
        public static void Srgb(double ar, double ag, double ab, double aa,
                                double br, double bg, double bb, double ba,
                                double f,
                                out double r, out double g, out double b, out double a)
        {
            if (f < 0) f = 0;
            if (f > 1) f = 1;
            double wa = f * aa, wb = (1 - f) * ba;
            a = wa + wb;
            if (a <= 0)
            {
                // 둘 다 투명 — CSS 도 «투명한 검정» 을 낸다(색은 뜻이 없다).
                r = 0; g = 0; b = 0; a = 0;
                return;
            }
            r = (ar * wa + br * wb) / a;
            g = (ag * wa + bg * wb) / a;
            b = (ab * wa + bb * wb) / a;
        }

        /// <summary>둘 다 불투명할 때(정본 24 자리 중 대부분) — 채널만 섞는다.</summary>
        public static void SrgbOpaque(double ar, double ag, double ab, double br, double bg, double bb, double f,
                                      out double r, out double g, out double b)
        {
            double a;
            Srgb(ar, ag, ab, 1, br, bg, bb, 1, f, out r, out g, out b, out a);
        }

        /// <summary>정본 `#rrggbb`·`#rgb` 를 0~255 채널로. 못 읽으면 false(자·표가 잡는다).</summary>
        public static bool ParseHex(string hex, out double r, out double g, out double b)
        {
            r = 0; g = 0; b = 0;
            if (string.IsNullOrEmpty(hex)) return false;
            string s = hex.Trim();
            if (s.Length > 0 && s[0] == '#') s = s.Substring(1);
            if (s.Length == 3)
            {
                s = new string(new[] { s[0], s[0], s[1], s[1], s[2], s[2] });
            }
            if (s.Length != 6) return false;
            int v;
            if (!int.TryParse(s, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out v)) return false;
            r = (v >> 16) & 0xFF; g = (v >> 8) & 0xFF; b = v & 0xFF;
            return true;
        }

        /// <summary>정본이 «transparent» 라고 적은 자리인가 — 그 자리는 색이 아니라 **알파를 만드는** 섞기다.</summary>
        public static bool IsTransparent(string token)
        {
            return token != null && string.Equals(token.Trim(), "transparent", StringComparison.Ordinal);
        }
    }
}
