using System;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>T124 — 시대 무늬 표(정본 `--af-pat` 다섯 + `gp-*` 키프레임)의 셈: 어느 시대에 무늬가 있나 · 진행/계단 · 한 주기 = 타일 한 칸 · 반짝임 · 파문 위상 · 래스터 커버리지.</summary>
    public class AgePatternRulesTests
    {
        static AgePatternSpec S()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            return AgePatternSpec.From(MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "Assets", "Forge", "Resources", "AgePatternUi.json"))));
        }
        static readonly string[] Plain = { "primitive", "medieval", "earlyModern", "modern", "space" };
        static readonly string[] Patterned = { "interstellar", "multiverse", "quantum", "underworld", "divine" };

        [Test]
        public void 무늬는_항성간_이상_다섯뿐이고_앞_다섯_시대는_민무늬다()
        {
            var s = S();
            foreach (string a in Patterned) Assert.IsTrue(s.Has(a), a);
            foreach (string a in Plain) Assert.IsFalse(s.Has(a), a + " 는 정본에 무늬가 없다(지어내지 말 것)");
            Assert.IsFalse(s.Has(null));
            Assert.AreEqual(2, s.Get("interstellar").Layers.Length, "별밭 두 층");
            Assert.AreEqual(2, s.Get("multiverse").Layers.Length, "밴드 두 벌");
            Assert.IsNotNull(s.Get("quantum").Rings); Assert.AreEqual(0, s.Get("quantum").Layers.Length, "양자는 링 메시만");
            Assert.AreEqual("poly", s.Get("underworld").Layers[0].Kind);
            Assert.AreEqual("stars", s.Get("divine").Layers[0].Kind);
        }

        [Test]
        public void 두_막대의_왼쪽_마스크는_정본대로_서로_다른_값이다()
        {
            var s = S();
            // 정본 `css/style.css` 4841 `.af-age-bar::before` = linear-gradient(90deg, transparent 0 30%, #000 50%)
            MaskSpec af = s.Mask(AgePatternKeys.AfBar);
            Assert.IsNotNull(af, "자동 제련 막대 마스크가 표에 있다");
            Assert.AreEqual(0.30, af.From, 1e-9, "af 막대는 30% 까지 비운다");
            Assert.AreEqual(0.50, af.To, 1e-9, "af 막대는 50% 에서 다 찬다");
            // 정본 `css/style.css` 5122 `.fi-age-bar::before` = transparent 0 24%, #000 46% — 아이콘·이름이 앉는 왼쪽이 af 보다 좁다
            MaskSpec fi = s.Mask(AgePatternKeys.FiBar);
            Assert.IsNotNull(fi, "정보 팝업 막대 마스크가 표에 있다(T380)");
            Assert.AreEqual(0.24, fi.From, 1e-9, "fi 막대는 24% 까지 비운다");
            Assert.AreEqual(0.46, fi.To, 1e-9, "fi 막대는 46% 에서 다 찬다");
            Assert.AreNotEqual(af.From, fi.From, "두 막대의 값이 같다고 베끼지 말 것 — 정본이 다르게 적었다");
            // 번지는 폭도 다르다 — af 20%p ↔ fi 22%p(정본이 막대마다 따로 적은 값이라 «한쪽을 옮겨 쓰기» 가 안 된다)
            Assert.AreEqual(0.20, af.To - af.From, 1e-9, "af 막대가 번지는 폭");
            Assert.AreEqual(0.22, fi.To - fi.From, 1e-9, "fi 막대가 번지는 폭");
            Assert.IsNull(s.Mask(null), "마스크 없는 자리(장착 셀)");
            Assert.IsNull(s.Mask("cell_opacity"), "짝(_to_f)이 없는 키는 마스크가 아니다");
            Assert.AreEqual(2, s.BarMasks.Count, "표에 있는 막대 종류는 둘");
        }

        [Test]
        public void 진행은_linear_또는_steps_7_end_이고_한_주기가_지나면_타일_한_칸_만큼_옮겨_있다()
        {
            var s = S();
            var i = s.Get("interstellar");
            double x0, y0, x1, y1;
            AgePatternRules.Shift(i, 0, 0, out x0, out y0);
            Assert.AreEqual(0.9, x0, 1e-9); Assert.AreEqual(0.2, y0, 1e-9);
            AgePatternRules.Shift(i, 0, 6000, out x1, out y1);
            Assert.AreEqual(0.9 + 3.1 * 0.5, x1, 1e-9, "12초의 절반 = 타일 반 칸");
            AgePatternRules.Shift(i, 1, 6000, out x1, out y1);
            Assert.AreEqual(0.2 - 2.3 * 0.5, x1, 1e-9, "작은 점 층은 반대 방향");
            // 한 주기 뒤 = 시작 + 타일 한 칸 → 타일 주기로 나눈 나머지가 같다(이음매 0)
            AgePatternRules.Shift(i, 0, 12000, out x1, out y1);
            Assert.AreEqual(0, AgePatternRules.Frac((x1 - x0) / i.Layers[0].TileW), 1e-9);
            var m = s.Get("multiverse");
            Assert.AreEqual(0, AgePatternRules.Progress(0, 3.5, 7), 1e-12);
            Assert.AreEqual(0, AgePatternRules.Progress(499, 3.5, 7), 1e-12, "steps(7, end): 첫 구간(0~500ms)은 0");
            Assert.AreEqual(1.0 / 7, AgePatternRules.Progress(500.001, 3.5, 7), 1e-9, "500ms 에 한 칸 뛴다");
            Assert.AreEqual(6.0 / 7, AgePatternRules.Progress(3499, 3.5, 7), 1e-9);
            AgePatternRules.Shift(m, 0, 3499, out x1, out y1); Assert.AreEqual(1.05 * 6 / 7, x1, 1e-9); Assert.AreEqual(0, y1, 1e-12);
            AgePatternRules.Shift(m, 1, 3499, out x1, out y1); Assert.AreEqual(0, x1, 1e-12); Assert.AreEqual(0.85 * 6 / 7, y1, 1e-9);
            var u = s.Get("underworld"); AgePatternRules.Shift(u, 0, 3500, out x1, out y1); Assert.AreEqual(1.0, y1, 1e-9, "7초에 2rem → 3.5초에 1rem 아래로");
            var d = s.Get("divine"); AgePatternRules.Shift(d, 0, 8000, out x1, out y1); Assert.AreEqual(-1.7, x1, 1e-9, "16초에 −3.4rem");
        }

        [Test]
        public void 반짝임과_숨쉬기는_alternate_ease_in_out_이고_양자는_밝기_고정이다()
        {
            var s = S();
            var i = s.Get("interstellar");
            Assert.AreEqual(0.62, AgePatternRules.Pulse(s, i, 0), 1e-9, "from");
            Assert.AreEqual(1.0, AgePatternRules.Pulse(s, i, 2800), 1e-9, "dur 뒤 to");
            Assert.AreEqual(0.62, AgePatternRules.Pulse(s, i, 5600), 1e-9, "되돌아와 from");
            Assert.AreEqual(AgePatternRules.Pulse(s, i, 700), AgePatternRules.Pulse(s, i, 5600 - 700), 1e-9, "alternate 는 대칭");
            double mid = AgePatternRules.Pulse(s, i, 1400);
            Assert.AreEqual((0.62 + 1) / 2, mid, 1e-6, "ease-in-out 의 한가운데는 중간값");
            Assert.Less(AgePatternRules.Pulse(s, i, 280), 0.62 + 0.38 * 0.1, "ease-in 초입은 느리다");
            var u = s.Get("underworld"); Assert.AreEqual(0.74, AgePatternRules.Pulse(s, u, 0), 1e-9); Assert.AreEqual(1.0, AgePatternRules.Pulse(s, u, 3600), 1e-9);
            Assert.AreEqual(1.0, AgePatternRules.Pulse(s, s.Get("quantum"), 1234), 1e-12, "양자·다중 우주는 반짝이지 않는다");
            Assert.AreEqual(1.0, AgePatternRules.Pulse(s, s.Get("multiverse"), 1234), 1e-12);
        }

        [Test]
        public void 양자_파문은_위상이_링_간격_한_주기를_돌고_링은_첫_스톱_앞뒤로도_되풀이된다()
        {
            var s = S(); var q = s.Get("quantum");
            Assert.AreEqual(0, AgePatternRules.RingPhaseRem(q, 0), 1e-12);
            Assert.AreEqual(0.42, AgePatternRules.RingPhaseRem(q, 1400), 1e-9);
            Assert.AreEqual(0, AgePatternRules.RingPhaseRem(q, 2800), 1e-9, "한 주기 뒤 위상 0 = 같은 그림");
            // 위상 0: 0~.17 흰 · .17~.84 투명 · .84~1.01 흰 (정본 주석 «위상 0 = 원래 정적 무늬와 픽셀 동일»)
            Assert.IsTrue(AgePatternRules.RingOn(q, 0.05, 0)); Assert.IsFalse(AgePatternRules.RingOn(q, 0.5, 0)); Assert.IsTrue(AgePatternRules.RingOn(q, 0.9, 0));
            // 위상 .42: 링이 통째로 바깥으로 밀린다 — .42~.59 흰 · 그 앞(0~.42)에도 −1 번째 링이 .84 앞에서 되풀이된다(−.42~−.25 → 안 보임) · .05 는 투명
            Assert.IsTrue(AgePatternRules.RingOn(q, 0.5, 0.42)); Assert.IsFalse(AgePatternRules.RingOn(q, 0.05, 0.42)); Assert.IsFalse(AgePatternRules.RingOn(q, 0.7, 0.42));
            double inner, outer;
            AgePatternRules.Ring(q, -1, 0.5, out inner, out outer); Assert.AreEqual(-0.34, inner, 1e-9); Assert.AreEqual(-0.17, outer, 1e-9);
            AgePatternRules.Ring(q, 1, 0.5, out inner, out outer); Assert.AreEqual(1.34, inner, 1e-9);
        }

        [Test]
        public void 래스터_커버리지가_정본_정의와_맞는다()
        {
            var s = S(); double ppr = 40;
            int w, h;
            var i = s.Get("interstellar");
            float[] a0 = AgePatternRules.Raster(s, i.Layers[0], ppr, out w, out h);
            Assert.AreEqual(124, w); Assert.AreEqual(76, h, "3.1×1.9rem 타일 · 40px/rem");
            double c0 = AgePatternRules.Coverage(a0);
            Assert.AreEqual(Math.PI * 0.22 * 0.22 / (3.1 * 1.9), c0, 0.006, "큰 점 = π·r²/타일(≈2.6%)");
            float[] a1 = AgePatternRules.Raster(s, i.Layers[1], ppr, out w, out h);
            Assert.AreEqual(Math.PI * 0.15 * 0.15 / (2.3 * 1.35), AgePatternRules.Coverage(a1), 0.006, "작은 점(≈2.3%)");
            var m = s.Get("multiverse");
            float[] b = AgePatternRules.Raster(s, m.Layers[0], ppr, out w, out h);
            Assert.AreEqual(42, w); Assert.AreEqual(4, h);
            Assert.AreEqual(0.34 / 1.05, AgePatternRules.Coverage(b), 0.03, "세로 밴드 켜진 비율");
            Assert.Greater(b[0], 0.9f, "띠는 0 에서 시작한다"); Assert.Less(b[w - 1], 0.1f);
            var u = s.Get("underworld");
            float[] d = AgePatternRules.Raster(s, u.Layers[0], ppr, out w, out h);
            Assert.AreEqual(80, w); Assert.AreEqual(80, h);
            Assert.AreEqual(240.0 / 1024, AgePatternRules.Coverage(d), 0.015, "마름모 넓이 = 20·24/2 / 32² (≈23%)");
            Assert.Greater(d[40 * w + 40], 0.99f, "한가운데는 마름모 안"); Assert.Less(d[2 * w + 2], 0.01f, "귀퉁이는 밖");
            var dv = s.Get("divine");
            float[] st = AgePatternRules.Raster(s, dv.Layers[0], ppr, out w, out h);
            Assert.AreEqual(136, w); Assert.AreEqual(68, h);
            double cs = AgePatternRules.Coverage(st);
            // 기대값은 SVG 정의에서 셈: 별 한 개의 넓이(신발끈) × Σscale² / (60×30). 타일 밖으로 나간 별 조각만큼 조금 작다(정본 주석의 ≈27% 는 원본 샷 실측이지 이 SVG 의 값이 아니다).
            double starArea = 0; var sp = dv.Layers[0].Star;
            for (int k = 0; k < sp.Length; k++) { var pa = sp[k]; var pb = sp[(k + 1) % sp.Length]; starArea += pa[0] * pb[1] - pb[0] * pa[1]; }
            starArea = Math.Abs(starArea) * 0.5;
            double scale2 = 0; foreach (var pl in dv.Layers[0].Places) scale2 += pl[2] * pl[2];
            double expect = starArea * scale2 / (dv.Layers[0].SvgPx[0] * dv.Layers[0].SvgPx[1]);
            Assert.Greater(cs, expect * 0.75, "별 여섯 — 타일 밖으로 잘린 만큼만 작다 (기대 " + expect.ToString("0.000") + ")"); Assert.Less(cs, expect * 1.05);
            Assert.IsTrue(AgePatternRules.InPolygon(AgePatternRules.Placed(dv.Layers[0].Star, 11, 9, 0.62, 14), 11, 9), "별 중심은 별 안");
        }
    }
}
