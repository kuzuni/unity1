using NUnit.Framework;
using Forge.Core.Ui;

namespace Forge.Tests.EditMode
{
    /// <summary>
    /// T371 1회차 — 정본 `color-mix(in srgb, A p%, B)` 의 셈. 잰 것 셋: ⓐ 비율이 **앞 색의 몫**인가 ⓑ 공간이 **sRGB**(바이트 선형보간)인가
    /// ⓒ `transparent` 와 섞으면 **색은 그대로고 알파만** 준다(CSS 의 «알파 미리 곱하기»).
    /// 기준 값은 정본 자리에서 가져왔다 — 기술 가지 원판(style.css 2104 `78%, #fff`)의 힘 갈래 색으로 손셈과 맞춘다.
    /// </summary>
    public class ColorMixRulesTests
    {
        [Test]
        public void 비율은_앞_색의_몫이고_sRGB_바이트에서_섞인다()
        {
            // 정본 2110: color-mix(in srgb, #e2574c 78%, #fff) — 힘 갈래(catalog tech_branch_power)
            double r, g, b;
            ColorMixRules.SrgbOpaque(226, 87, 76, 255, 255, 255, 0.78, out r, out g, out b);
            Assert.AreEqual(226 * 0.78 + 255 * 0.22, r, 1e-9, "R");
            Assert.AreEqual(87 * 0.78 + 255 * 0.22, g, 1e-9, "G");
            Assert.AreEqual(76 * 0.78 + 255 * 0.22, b, 1e-9, "B");
            Assert.AreEqual(232, System.Math.Round(r), "정본 실측 (232,124,115) 의 R");
            Assert.AreEqual(124, System.Math.Round(g), "G");
            Assert.AreEqual(115, System.Math.Round(b), "B");
        }

        [Test]
        public void 투명과_섞으면_색은_그대로고_알파만_준다()
        {
            // 정본 8538 `.equip-cell:not(.egg-cell)` box-shadow: color-mix(in srgb, --rc 62%, transparent)
            double r, g, b, a;
            ColorMixRules.Srgb(226, 87, 76, 1, 0, 0, 0, 0, 0.62, out r, out g, out b, out a);
            Assert.AreEqual(0.62, a, 1e-9, "알파가 비율만큼 준다");
            Assert.AreEqual(226, r, 1e-9, "색은 그대로 — 알파를 미리 곱해 섞기 때문이다");
            Assert.AreEqual(87, g, 1e-9);
            Assert.AreEqual(76, b, 1e-9);
        }

        [Test]
        public void 끝값과_어긋난_입력은_CSS_처럼_다룬다()
        {
            double r, g, b, a;
            ColorMixRules.Srgb(10, 20, 30, 1, 200, 210, 220, 1, 1.5, out r, out g, out b, out a);   // 1 을 넘으면 1
            Assert.AreEqual(10, r, 1e-9, "비율 1 = 앞 색 그대로");
            ColorMixRules.Srgb(10, 20, 30, 0, 200, 210, 220, 0, 0.5, out r, out g, out b, out a);   // 둘 다 투명
            Assert.AreEqual(0, a, 1e-9, "둘 다 투명하면 투명하다");

            double h1, h2, h3;
            Assert.IsTrue(ColorMixRules.ParseHex("#fff", out h1, out h2, out h3));
            Assert.AreEqual(255, h1, 1e-9, "#fff 는 흰색이다(세 자리 표기)");
            Assert.IsTrue(ColorMixRules.ParseHex("17181a", out h1, out h2, out h3));
            Assert.AreEqual(0x17, h1, 1e-9, "# 없이도 읽는다");
            Assert.IsFalse(ColorMixRules.ParseHex("rgb(1,2,3)", out h1, out h2, out h3), "못 읽는 표기는 false");
            Assert.IsTrue(ColorMixRules.IsTransparent("transparent"));
            Assert.IsFalse(ColorMixRules.IsTransparent("#000"));
        }
    }
}
