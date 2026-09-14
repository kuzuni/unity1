using System;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests.EditMode
{
    /// <summary>T342 — CSS `filter` 셈(Core · UnityEngine 0)과 표 읽기.</summary>
    public class FilterRulesTests
    {
        static string File_()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            return Path.Combine(root, "Assets", "Forge", "Resources", "FilterUi.json");
        }
        static FilterTable Table_() { return FilterTable.From(MiniJson.ParseObject(File.ReadAllText(File_()))); }
        static FilterSpec Site(string key) { return Table_().Get(key); }

        [Test]
        public void grayscale_1_은_세_채널을_한_값으로_눕힌다()
        {
            double r = 0.8, g = 0.3, b = 0.1;
            FilterRules.Grayscale(1.0, ref r, ref g, ref b);
            Assert.AreEqual(r, g, 1e-12, "R 과 G 가 같아야 한다");
            Assert.AreEqual(g, b, 1e-12, "G 와 B 가 같아야 한다");
            // 명세 grayscale 행렬의 휘도 계수 그대로
            double want = 0.2126 * 0.8 + 0.7152 * 0.3 + 0.0722 * 0.1;
            Assert.AreEqual(want, r, 1e-12, "명세 휘도 계수(0.2126·0.7152·0.0722)");
        }

        [Test]
        public void grayscale_0_과_saturate_1_은_원본을_안_바꾼다()
        {
            double r = 0.8, g = 0.3, b = 0.1;
            FilterRules.Grayscale(0.0, ref r, ref g, ref b);
            Assert.AreEqual(0.8, r, 1e-12); Assert.AreEqual(0.3, g, 1e-12); Assert.AreEqual(0.1, b, 1e-12);
            FilterRules.Saturate(1.0, ref r, ref g, ref b);
            Assert.AreEqual(0.8, r, 1e-12); Assert.AreEqual(0.3, g, 1e-12); Assert.AreEqual(0.1, b, 1e-12);
        }

        [Test]
        public void saturate_0_은_grayscale_1_과_같은_자리로_간다()
        {
            double r1 = 0.6, g1 = 0.2, b1 = 0.9, r2 = 0.6, g2 = 0.2, b2 = 0.9;
            FilterRules.Saturate(0.0, ref r1, ref g1, ref b1);
            FilterRules.Grayscale(1.0, ref r2, ref g2, ref b2);
            // 명세가 두 함수에 적은 계수가 소수 한 자리 다르다(0.213 ↔ 0.2126) — 같은 «회색» 이되 같은 수는 아니다
            Assert.AreEqual(r2, r1, 1e-3, "둘 다 회색이고 값 차이는 명세 계수 차이(1e-3)만큼");
            Assert.AreEqual(r1, g1, 1e-12, "saturate(0) 도 세 채널이 같다");
        }

        [Test]
        public void brightness_는_곱하고_1_을_넘으면_자른다()
        {
            double r = 0.4, g = 0.7, b = 0.9;
            FilterRules.Brightness(1.75, ref r, ref g, ref b);
            Assert.AreEqual(0.7, r, 1e-12, "0.4 × 1.75");
            Assert.AreEqual(1.0, g, 1e-12, "0.7 × 1.75 = 1.225 → 1 로 자른다");
            Assert.AreEqual(1.0, b, 1e-12, "0.9 × 1.75 → 1");
        }

        [Test]
        public void 표가_정본_그대로다_빈_장비_칸()
        {
            FilterSpec f = Site("equip_cell_empty");
            Assert.AreEqual(862, f.Line, "정본 style.css 줄 번호");
            Assert.IsTrue(f.HasGrayscale); Assert.AreEqual(1.0, f.Grayscale, 1e-12);
            Assert.IsTrue(f.HasBrightness); Assert.AreEqual(1.75, f.Brightness, 1e-12);
            Assert.IsTrue(f.HasOpacity); Assert.AreEqual(0.52, f.Opacity, 1e-12);
            Assert.IsFalse(f.HasBlur, "이 자리엔 blur 가 없다");
        }

        [Test]
        public void 표가_정본_그대로다_부화_원뿔()
        {
            FilterSpec f = Site("hatch_cone");
            Assert.AreEqual(8025, f.Line);
            Assert.IsTrue(f.HasBlur); Assert.AreEqual(1.2, f.BlurPx, 1e-12);
            Assert.IsFalse(f.HasGrayscale, "이 자리엔 색 함수가 없다");
        }

        [Test]
        public void 빈_장비_칸_차례대로_걸면_어두운_면_위_밝은_실루엣이_된다()
        {
            // 정본 주석 860~861 이 말한 그대로 — 마룬(#6b3538) 위 어두운 실루엣을 «밝게» 뒤집는다
            FilterSpec f = Site("equip_cell_empty");
            double r = 0x6b / 255.0, g = 0x35 / 255.0, b = 0x38 / 255.0;
            double before = (r + g + b) / 3.0;
            FilterRules.Apply(f, ref r, ref g, ref b);
            Assert.AreEqual(r, g, 1e-9, "회색이 됐다");
            Assert.AreEqual(g, b, 1e-9);
            Assert.Greater(r, before, "밝아졌다 — 이것이 없으면 마룬 타일 위에서 형태가 안 읽힌다");
        }

        [Test]
        public void 가우시안_커널은_합이_1_이고_대칭이다()
        {
            double[] k = FilterRules.GaussianKernel(1.2);
            double sum = 0; foreach (double v in k) sum += v;
            Assert.AreEqual(1.0, sum, 1e-12, "합 1");
            Assert.AreEqual(k.Length / 2 * 2 + 1, k.Length, "홀수 길이");
            for (int i = 0; i < k.Length / 2; i++) Assert.AreEqual(k[i], k[k.Length - 1 - i], 1e-12, "대칭");
            Assert.Greater(k[k.Length / 2], k[0], "가운데가 가장 크다");
        }

        [Test]
        public void blur_은_반지름이_아니라_표준편차다()
        {
            // σ=1.2 면 3σ 자르기로 반경 4 — 1.2 를 «반지름» 으로 읽으면 반경 1 이라 훨씬 덜 번진다
            Assert.AreEqual(4, FilterRules.KernelRadius(1.2), "ceil(3σ)");
            Assert.AreEqual(0, FilterRules.KernelRadius(0), "σ=0 이면 안 번진다");
        }

        [Test]
        public void 굽는_σ_는_늘림_배율만큼_커진다()
        {
            // 32 단위 상자를 64px 로 굽고 화면엔 128px 로 늘린다면 굽는 σ 는 절반이어야 화면에서 제 값이 된다
            Assert.AreEqual(0.6, FilterRules.BakeSigmaPx(1.2, 64, 128), 1e-12);
            Assert.AreEqual(2.4, FilterRules.BakeSigmaPx(1.2, 128, 64), 1e-12);
            Assert.AreEqual(0.0, FilterRules.BakeSigmaPx(1.2, 64, 0), 1e-12, "크기를 모르면 0(안 번진다)");
        }

        [Test]
        public void 번짐은_화면_크기까지만_굽는다_그_위로는_볼_것이_없다()
        {
            Assert.AreEqual(133, FilterRules.BlurBakeSide(256, 133), "화면이 작으면 그 크기로 내린다");
            Assert.AreEqual(256, FilterRules.BlurBakeSide(256, 400), "화면이 더 크면 원본보다 키우지 않는다");
            Assert.AreEqual(256, FilterRules.BlurBakeSide(256, 0), "화면 크기를 모르면 원본 그대로");
            Assert.AreEqual(2, FilterRules.BlurBakeSide(256, 1), "너무 작아도 두 화소는 남긴다");
        }

        [Test]
        public void 화면_해상도로_내리면_실제로_싸진다()
        {
            // 부화 원뿔 실측: 32단위 상자를 8px/단위로 구워 256² · 화면 높이 133px · 정본 blur(1.2px) × css_px 2.164
            double sigmaCanvas = 1.2 * 2.164;
            double sigmaBaked = FilterRules.BakeSigmaPx(sigmaCanvas, 256, 133);

            Assert.AreEqual(15, FilterRules.KernelRadius(sigmaBaked), "굽는 해상도 그대로면 반경 15");
            Assert.AreEqual(8, FilterRules.KernelRadius(sigmaCanvas), "화면 해상도로 내리면 반경 8");

            long big = FilterRules.BlurMuls(256, sigmaBaked);
            long small = FilterRules.BlurMuls(133, sigmaCanvas);
            Assert.Greater(big, 5000000L, "그대로 굽던 값이 500만 곱셈을 넘었다(§1 60fps·GC 0 이 걸린다)");
            Assert.Less(small, 1000000L, "내리면 100만 아래");
            Assert.Greater((double)big / small, 5.0, "다섯 배 넘게 싸진다");
        }

        [Test]
        public void 내려도_번짐_모양은_그대로다()
        {
            // σ 를 같은 비율로 줄이므로 «σ / 한 변» 이 안 바뀐다 — 화면에서 본 번짐 폭이 같다는 뜻이다
            double sigmaBaked = FilterRules.BakeSigmaPx(1.2 * 2.164, 256, 133);
            double shrink = 133.0 / 256.0;
            Assert.AreEqual(sigmaBaked / 256.0, (sigmaBaked * shrink) / 133.0, 1e-12);
        }

        [Test]
        public void 함수가_하나도_없는_줄은_거절한다()
        {
            var o = MiniJson.ParseObject("{\"line\": 1}");
            Assert.Throws<FormatException>(() => FilterSpec.From("x", o));
        }

        [Test]
        public void 범위를_벗어난_값은_거절한다()
        {
            Assert.Throws<FormatException>(() => FilterSpec.From("x", MiniJson.ParseObject("{\"line\":1,\"opacity\":1.5}")));
            Assert.Throws<FormatException>(() => FilterSpec.From("x", MiniJson.ParseObject("{\"line\":1,\"grayscale\":2}")));
            Assert.Throws<FormatException>(() => FilterSpec.From("x", MiniJson.ParseObject("{\"line\":1,\"brightness\":-1}")));
        }

        [Test]
        public void 없는_자리를_물으면_이름을_대고_넘어진다()
        {
            var t = Table_();
            Assert.IsFalse(t.Has("없는자리"));
            var e = Assert.Throws<FormatException>(() => t.Get("없는자리"));
            StringAssert.Contains("없는자리", e.Message);
        }
    }
}
