using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>
    /// T357 — 정본의 CSS 알파 겹은 **sRGB 에서** 섞이는데 클론(Linear 색공간)은 **선형에서** 섞는다.
    /// 표(`SurfaceUi.json` `tabbar_shade` = 정본 `style.css` 8317)와 바탕(`catalog.json` `tabbar_bg` = `#0e111b`)으로
    /// 두 길의 값을 수로 못 박는다 — 런 537 `screen_main.png` 탭바에서 잰 다섯 점이 **선형 쪽**과 맞았다(그것이 이 절의 증거다).
    /// </summary>
    public class SurfaceBlendRulesTests
    {
        static string Root()
        {
            return Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
        }

        /// <summary>표의 `tabbar_shade` 를 정지점 배열로 — 값은 표가 쥐고 자는 읽기만 한다(§1 «수치는 코드에 박지 않는다»).</summary>
        static void Shade(out double[] pos, out double[][] rgba)
        {
            string path = Path.Combine(Root(), "Assets", "Forge", "Resources", "SurfaceUi.json");
            var root = MiniJson.ParseObject(File.ReadAllText(path));
            var shade = J.Obj(J.Require(root, "tabbar_shade"));
            var stops = J.Arr(J.Require(shade, "stops"));
            var offs = J.Arr(J.Require(shade, "offsets"));
            Assert.AreEqual(stops.Count, offs.Count, "정지점과 위치 수가 같아야 한다");
            pos = new double[offs.Count];
            rgba = new double[stops.Count][];
            for (int i = 0; i < offs.Count; i++)
            {
                pos[i] = J.Num(offs[i]);
                var s = J.Arr(stops[i]);
                rgba[i] = new double[] { J.Num(s[0]), J.Num(s[1]), J.Num(s[2]), J.Num(s[3]) };
            }
        }

        /// <summary>바탕 `#0e111b` — `catalog.json` 의 `tabbar_bg`.</summary>
        static byte[] TabBarBg()
        {
            string path = Path.Combine(Root(), "Assets", "Forge", "catalog.json");
            var root = MiniJson.ParseObject(File.ReadAllText(path));
            var colors = J.Arr(J.Require(root, "colors"));
            for (int i = 0; i < colors.Count; i++)
            {
                var c = J.Obj(colors[i]);
                if (J.Str(J.Require(c, "key")) != "tabbar_bg") continue;
                string hex = J.Str(J.Require(c, "hex")).TrimStart('#');
                return new byte[]
                {
                    Convert.ToByte(hex.Substring(0, 2), 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16)
                };
            }
            throw new InvalidOperationException("catalog.json 에 tabbar_bg 가 없다");
        }

        [Test]
        public void sRGB_과_선형은_같은_알파를_다르게_섞는다_그리고_어두운_바탕일수록_벌어진다()
        {
            // 흰 16% 를 거의 검정 위에 얹는다 — 정본은 거의 안 밝아지고, 선형은 크게 밝아진다.
            Assert.AreEqual(52, SurfaceBlendRules.OverSrgb(14, 255, 0.16), 1, "정본(sRGB) 합성");
            Assert.Greater(SurfaceBlendRules.OverLinear(14, 255, 0.16), SurfaceBlendRules.OverSrgb(14, 255, 0.16),
                           "선형 합성이 더 밝다 — 이 절의 뿌리");
            // 밝은 바탕에서는 둘이 붙는다(그래서 여태 «UI 가 좀 밝다» 가 어두운 자리에서만 드러났다).
            Assert.AreEqual(SurfaceBlendRules.OverSrgb(200, 255, 0.16), SurfaceBlendRules.OverLinear(200, 255, 0.16), 6,
                            "밝은 바탕에서는 두 길의 차가 작다");
            // 알파 0·1 은 두 길이 같아야 한다.
            Assert.AreEqual(14, SurfaceBlendRules.OverLinear(14, 255, 0.0));
            Assert.AreEqual(255, SurfaceBlendRules.OverLinear(14, 255, 1.0));
        }

        [Test]
        public void 정지점_보간은_CSS_규칙대로_색과_알파를_함께_민다()
        {
            double[] pos; double[][] rgba;
            Shade(out pos, out rgba);
            double r, g, b, a;
            SurfaceBlendRules.Sample(pos, rgba, 0.0, out r, out g, out b, out a);
            Assert.AreEqual(rgba[0][3], a, 1e-9, "첫 정지점");
            SurfaceBlendRules.Sample(pos, rgba, 1.0, out r, out g, out b, out a);
            Assert.AreEqual(rgba[rgba.Length - 1][3], a, 1e-9, "끝 정지점");
            // 두 정지점의 한가운데는 알파도 한가운데
            double mid = (pos[0] + pos[1]) * 0.5;
            SurfaceBlendRules.Sample(pos, rgba, mid, out r, out g, out b, out a);
            Assert.AreEqual((rgba[0][3] + rgba[1][3]) * 0.5, a, 1e-9, "정지점 사이는 선형 보간");
            // 범위 밖은 끝 값으로 잡아 둔다(CSS 와 같다)
            SurfaceBlendRules.Sample(pos, rgba, -0.5, out r, out g, out b, out a);
            Assert.AreEqual(rgba[0][3], a, 1e-9);
        }

        /// <summary>
        /// 런 537 `screen_main.png` 탭바에서 x 중앙값으로 잰 다섯 점(세로 위치 t = 겹 안 비율):
        /// 0.098→98 · 0.231→71 · 0.385→53 · 0.590→19 · 0.897→9.
        /// 그 다섯이 **선형 합성**과 ±2 안에서 맞고, **정본(sRGB) 합성**과는 위쪽 절반에서 크게 벌어진다.
        /// </summary>
        [Test]
        public void 런537_탭바_실측_다섯_점은_선형_합성과_맞고_정본_합성과는_위쪽에서_벌어진다()
        {
            double[] pos; double[][] rgba;
            Shade(out pos, out rgba);
            byte[] bg = TabBarBg();
            Assert.AreEqual(new byte[] { 0x0e, 0x11, 0x1b }, bg, "탭바 바탕은 정본 #0e111b");

            double[] t = { 0.098, 0.231, 0.385, 0.590, 0.897 };
            byte[] measured = { 98, 71, 53, 19, 9 };
            var lin = new byte[3];
            var srgb = new byte[3];
            var gaps = new List<int>();
            for (int i = 0; i < t.Length; i++)
            {
                SurfaceBlendRules.CompositeLinear(bg, pos, rgba, t[i], lin);
                SurfaceBlendRules.CompositeSrgb(bg, pos, rgba, t[i], srgb);
                Assert.AreEqual(measured[i], lin[0], 2,
                    "t=" + t[i] + " 실측 " + measured[i] + " 이 선형 합성 " + lin[0] + " 과 어긋난다 — 뿌리 진단이 바뀐 것이다");
                gaps.Add(lin[0] - srgb[0]);
            }
            Assert.Greater(gaps[0], 40, "겹 맨 위(흰 16%)에서 두 길의 차가 가장 크다");
            Assert.LessOrEqual(Math.Abs(gaps[4]), 3, "겹 맨 아래(검정 38%)에서는 두 길이 거의 같다");
        }

        /// <summary>고칠 길을 자가 쥔다 — 아는 바탕 위라면 «sRGB 로 합성한 값을 불투명하게» 구우면 정본과 같아진다.</summary>
        public static byte[] BakedOpaque(byte[] bg, double[] pos, double[][] rgba, double t)
        {
            var outRgb = new byte[3];
            SurfaceBlendRules.CompositeSrgb(bg, pos, rgba, t, outRgb);
            return outRgb;
        }

        [Test]
        public void 아는_바탕_위라면_불투명하게_구운_값이_정본과_같다()
        {
            double[] pos; double[][] rgba;
            Shade(out pos, out rgba);
            byte[] bg = TabBarBg();
            for (double t = 0.0; t <= 1.0001; t += 0.1)
            {
                byte[] baked = BakedOpaque(bg, pos, rgba, t);
                var srgb = new byte[3];
                SurfaceBlendRules.CompositeSrgb(bg, pos, rgba, t, srgb);
                // 불투명하게 얹으면 섞는 공간이 끼어들 자리가 없다 — 구운 값이 그대로 화면 값이다.
                Assert.AreEqual(srgb[0], baked[0]);
                Assert.AreEqual(srgb[1], baked[1]);
                Assert.AreEqual(srgb[2], baked[2]);
            }
        }
    }
}
