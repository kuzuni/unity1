using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests.EditMode
{
    /// <summary>T354 — 정본 `line-height` 표(`LineHeightUi.json`)의 규약과 Core 셈(UnityEngine 0).</summary>
    public class LineHeightRulesTests
    {
        // NotoSansKR-Forge.ttf 실측(hhea) — unitsPerEm 1000 · asc 1160 · desc -288 · gap 0
        const double FaceLineHeight = 1.448;   // pointSize 1 로 잰 값(비율만 쓰므로 단위는 상관없다)
        const double FacePointSize = 1.0;

        static string File_()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            return Path.Combine(root, "Assets", "Forge", "Resources", "LineHeightUi.json");
        }
        static LineHeightTable Table_() { return LineHeightTable.From(MiniJson.ParseObject(File.ReadAllText(File_()))); }

        [Test]
        public void 표는_읽히고_키마다_단위_꼬리를_달고_값은_0_이상이다()
        {
            LineHeightTable t = Table_();
            Assert.AreEqual(81, t.Count, "정본 선언 81(style.css 80 + ui.js 인라인 1)");
            foreach (string k in t.Keys)
            {
                Assert.IsTrue(LineHeightRules.IsLineHeightKey(k), "단위 꼬리가 없는 키: " + k);
                Assert.GreaterOrEqual(t.Get(k), 0.0, k);
            }
        }

        [Test]
        public void 단위_꼬리는_서로_겹치지_않는다()
        {
            // ⚠ `_lh_rem`·`_lh_w` 도 «_lh» 를 품는다 — 배수 갈래가 그 둘을 먼저 걸러야 한다.
            Assert.IsTrue(LineHeightRules.IsRemKey("x_lh_rem"));
            Assert.IsTrue(LineHeightRules.IsAppWKey("x_lh_w"));
            Assert.IsTrue(LineHeightRules.IsRatioKey("x_lh"));
            Assert.IsFalse(LineHeightRules.IsRatioKey("x_lh_rem"));
            Assert.IsFalse(LineHeightRules.IsRatioKey("x_lh_w"));
            Assert.IsFalse(LineHeightRules.IsLineHeightKey("x_r_rem"), "반지름 키(T345)를 물지 않는다");
        }

        [Test]
        public void 글꼴이_제_힘으로_내는_배수는_1_448_이다()
        {
            Assert.AreEqual(1.448, LineHeightRules.FaceRatio(FaceLineHeight, FacePointSize), 1e-9);
            Assert.Throws<FormatException>(() => LineHeightRules.FaceRatio(1.0, 0.0));
        }

        [Test]
        public void 배수를_TMP_덧붙임으로_옮긴다_대부분_음수다()
        {
            // 정본이 가장 많이 쓰는 1 — 글꼴이 1.448 을 내므로 0.448 만큼 **줄여야** 한다.
            Assert.AreEqual(-0.448, LineHeightRules.Spacing(1.0, FaceLineHeight, FacePointSize), 1e-9);
            Assert.AreEqual(0.0, LineHeightRules.Spacing(1.448, FaceLineHeight, FacePointSize), 1e-9, "글꼴 값과 같으면 덧붙임 0");
            Assert.Greater(LineHeightRules.Spacing(1.6, FaceLineHeight, FacePointSize), 0.0, "더 벌리면 양수");
            // 글꼴 자산 크기가 달라져도 «배수» 는 그대로다 — pointSize 에 비례해 커진다.
            Assert.AreEqual(-0.448 * 90.0, LineHeightRules.Spacing(1.0, FaceLineHeight * 90.0, 90.0), 1e-6);
            Assert.Throws<FormatException>(() => LineHeightRules.Spacing(-0.1, FaceLineHeight, FacePointSize));
            Assert.Throws<FormatException>(() => LineHeightRules.Spacing(1.0, FaceLineHeight, 0.0));
        }

        [Test]
        public void 절대_줄높이_자리는_그_자리_글자_크기로_나눠_배수가_된다()
        {
            // 정본 `line-height: 1.15rem` 자리 — 1rem 이 40px 이고 글자가 32px 이면 배수는 1.4375
            Assert.AreEqual(1.15 * 40.0 / 32.0, LineHeightRules.Ratio(1.15, "x_lh_rem", 32.0, 40.0, 1080.0), 1e-9);
            // 정본 `calc(var(--app-w) * .0351)` 자리 — 앱 폭 1080 이면 37.908px
            Assert.AreEqual(0.0351 * 1080.0 / 32.0, LineHeightRules.Ratio(0.0351, "x_lh_w", 32.0, 40.0, 1080.0), 1e-9);
            Assert.AreEqual(1.25, LineHeightRules.Ratio(1.25, "x_lh", 32.0, 40.0, 1080.0), 1e-9);
            Assert.Throws<FormatException>(() => LineHeightRules.Ratio(1.0, "x_r_rem", 32.0, 40.0, 1080.0));
            Assert.Throws<FormatException>(() => LineHeightRules.RatioFromPx(10.0, 0.0));
        }

        [Test]
        public void 표에_세_단위가_다_있고_정본_자취가_자리마다_붙어_있다()
        {
            LineHeightTable t = Table_();
            Assert.AreEqual(1, t.Keys.Count(LineHeightRules.IsRemKey), "정본 `1.15rem` 한 자리");
            Assert.AreEqual(1, t.Keys.Count(LineHeightRules.IsAppWKey), "정본 `calc(var(--app-w) * …)` 한 자리");
            Assert.AreEqual(79, t.Keys.Count(LineHeightRules.IsRatioKey), "나머지는 배수");

            JsonObject root = MiniJson.ParseObject(File.ReadAllText(File_()));
            JsonObject src = J.Obj(root["_정본"]);
            foreach (string k in t.Keys)
                Assert.IsTrue(src.Has(k), "정본 자취가 없는 자리: " + k);
        }

        [Test]
        public void 정본_값_분포가_그대로_옮겨졌다()
        {
            LineHeightTable t = Table_();
            // 가장 많은 값은 **1** 스물다섯(정본이 `1` 24 번 · `1.0` 1 번으로 적었다 — 같은 수다) — 클론 기본 1.448 과 가장 크게 어긋나는 무리다.
            Assert.AreEqual(25, t.Keys.Count(k => LineHeightRules.IsRatioKey(k) && Math.Abs(t.Get(k) - 1.0) < 1e-9));
            Assert.AreEqual(3, t.Keys.Count(k => LineHeightRules.IsRatioKey(k) && t.Get(k) == 0.0), "정본 `line-height: 0` 세 자리");
            // 배선하면 어느 쪽으로 움직이는가 — 배수 자리 79 중 **74 가 글꼴 기본(1.448)보다 좁고** 다섯만 넓다(1.45 ×2 · 1.5 ×3).
            var ratios = t.Keys.Where(LineHeightRules.IsRatioKey).Select(t.Get).ToList();
            Assert.AreEqual(79, ratios.Count);
            Assert.AreEqual(74, ratios.Count(v => v < 1.448), "대부분은 지금보다 **좁아진다**");
            Assert.AreEqual(5, ratios.Count(v => v > 1.448), "넓어지는 자리 다섯");
            Assert.AreEqual(0, ratios.Count(v => v == 1.448), "우연히 글꼴 기본과 같은 자리는 없다");
        }
    
        /// <summary>T354 3회차 — TMP `lineSpacing` 은 em/100 단위: 배수 r → (r − 자산 줄높이 비율) × 100.</summary>
        [Test]
        public void TMP_lineSpacing_은_배수와_자산_줄높이_비율의_차를_100배로_낸다()
        {
            // pointSize 54 · lineHeight 78.192(1.448em)
            Assert.AreEqual(0.0, LineHeightRules.TmpLineSpacing(1.448, 78.192, 54), 1e-9, "자산 기본 줄높이면 0");
            Assert.AreEqual(-44.8, LineHeightRules.TmpLineSpacing(1.0, 78.192, 54), 1e-9, "정본 1 이면 −44.8");
            Assert.AreEqual(5.2, LineHeightRules.TmpLineSpacing(1.5, 78.192, 54), 1e-9, "정본 1.5 면 +5.2");
            Assert.AreEqual(-19.8, LineHeightRules.TmpLineSpacing(1.25, 78.192, 54), 1e-9);
            // Spacing(자산 단위) 과 같은 값의 단위 옮김
            double sp = LineHeightRules.Spacing(1.25, 78.192, 54);
            Assert.AreEqual(LineHeightRules.TmpLineSpacing(1.25, 78.192, 54), LineHeightRules.SpacingToTmp(sp, 54), 1e-9);
            // 글자 크기와 무관(pointSize 만 다르게 · 비율 같음)
            Assert.AreEqual(LineHeightRules.TmpLineSpacing(1.25, 78.192, 54), LineHeightRules.TmpLineSpacing(1.25, 144.8, 100), 1e-9);
            Assert.Throws<FormatException>(() => LineHeightRules.TmpLineSpacing(1.2, 78.192, 0));
            Assert.Throws<FormatException>(() => LineHeightRules.SpacingToTmp(1, 0));
        }
}
}
