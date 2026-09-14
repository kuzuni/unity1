using System;
using NUnit.Framework;
using Forge.Core.BattleFx;

namespace Forge.Tests
{
    /// <summary>
    /// T135 ⓐ — 피격 붉은 비네트의 «셈» 이 정본과 같은가.
    /// 정본: `ui.js` 1360 `flashDamage(sev)`(세기) · `style.css` 438 `#dmg-flash`(그림·마스크) · 481 `@keyframes dmgvignette`(시계).
    /// 그림·시계를 Core 가 쥐므로 유니티 없이 여기서 잰다 — 화면 쪽 단언은 `Assets/Tests/PlayMode/DamageVignetteTests.cs`.
    /// </summary>
    public class DmgVignetteRulesTests
    {
        [Test]
        public void 세기는_정본_min_64_32더하기_sev곱1_05_이고_소수_둘째에서_자른다()
        {
            // `Math.min(0.64, 0.32 + (sev || 0.12) * 1.05).toFixed(2)`
            Assert.AreEqual(0.45, FxRules.DmgVigPeak(0.12), 1e-9, "기본 세기(sev 0.12) = .32 + .126 = .446 → .45");
            Assert.AreEqual(0.45, FxRules.DmgVigPeak(0), 1e-9, "정본 `sev || 0.12` — 0 이면 기본값으로 떨어진다");
            Assert.AreEqual(0.64, FxRules.DmgVigPeak(1), 1e-9, "치명/사망(sev 1)은 상한 .64 에 걸린다");
            Assert.AreEqual(0.64, FxRules.DmgVigPeak(0.5), 1e-9, ".32 + .525 = .845 → 상한 .64");
            Assert.AreEqual(0.53, FxRules.DmgVigPeak(0.2), 1e-9, ".32 + .21 = .53");
            Assert.LessOrEqual(FxRules.DmgVigPeak(9), FxRules.DmgVigMax, "어떤 피해도 상한을 못 넘는다");
        }

        [Test]
        public void 시계는_13ms에_정점_44ms까지_유지_440ms에_0이다()
        {
            double peak = FxRules.DmgVigPeak(1);
            Assert.AreEqual(0, FxRules.DmgVigAlpha(0, peak), 1e-9, "0% = 0");
            Assert.AreEqual(peak, FxRules.DmgVigAlpha(FxRules.DmgVigMs * FxRules.DmgVigRise, peak), 1e-9, "3%(=13.2ms) 에 정점");
            Assert.AreEqual(peak, FxRules.DmgVigAlpha(FxRules.DmgVigMs * FxRules.DmgVigHold - 0.001, peak), 1e-6, "10%(=44ms) 까지 정점을 유지한다");
            Assert.AreEqual(0, FxRules.DmgVigAlpha(FxRules.DmgVigMs, peak), 1e-9, "100% = 0");
            Assert.AreEqual(0, FxRules.DmgVigAlpha(FxRules.DmgVigMs * 2, peak), 1e-9, "끝난 뒤에도 0");
            // 정점 플래토는 13~44ms **31ms** 여야 한다(css 주석: 155ms 플래토가 «붉게 물든 채 멈춘» 것으로 읽혔다).
            double plateau = FxRules.DmgVigMs * (FxRules.DmgVigHold - FxRules.DmgVigRise);
            Assert.AreEqual(30.8, plateau, 0.05, "정점 플래토 31ms");
            // 램프는 임팩트 프레임(+16ms) 안에 정점에 닿아야 한다.
            Assert.Less(FxRules.DmgVigMs * FxRules.DmgVigRise, 16, "정점이 첫 프레임 안에 온다");
        }

        [Test]
        public void 꼬리는_단조감소하고_ease_out이라_앞이_빠르다()
        {
            double peak = FxRules.DmgVigPeak(1);
            double prev = peak;
            for (double ms = FxRules.DmgVigMs * FxRules.DmgVigHold; ms <= FxRules.DmgVigMs; ms += 4)
            {
                double a = FxRules.DmgVigAlpha(ms, peak);
                Assert.LessOrEqual(a, prev + 1e-9, ms + "ms 에서 알파가 도로 올랐다");
                prev = a;
            }
            // 꼬리 절반 시점(242ms)에 이미 정점의 3분의 1 아래 — ease-out 은 앞이 빠르다.
            double mid = FxRules.DmgVigAlpha(FxRules.DmgVigMs * 0.55, peak);
            Assert.Less(mid, peak / 3, "ease-out 이면 절반 지점에서 이미 많이 빠져 있다");
        }

        [Test]
        public void 이징은_cubic_bezier_0_0_58_1_을_따라간다()
        {
            // 그 베지에의 실제 두 점(손계산): x=.3425 → y=.5 · x=.7347 → y=.896
            Assert.AreEqual(0.5, FxRules.EaseOut01(0.3425), 0.01);
            Assert.AreEqual(0.896, FxRules.EaseOut01(0.7347), 0.01);
            Assert.AreEqual(0, FxRules.EaseOut01(-1), 1e-9);
            Assert.AreEqual(1, FxRules.EaseOut01(2), 1e-9);
        }

        [Test]
        public void 타원_반지름은_모서리에서_1_한가운데서_0이다()
        {
            Assert.AreEqual(0, FxRules.DmgVigT(0.5, 0.5), 1e-9, "한가운데");
            Assert.AreEqual(1, FxRules.DmgVigT(0, 0), 1e-9, "왼위 모서리");
            Assert.AreEqual(1, FxRules.DmgVigT(1, 1), 1e-9, "오른아래 모서리");
            Assert.AreEqual(1 / Math.Sqrt(2), FxRules.DmgVigT(0, 0.5), 1e-9, "좌변 한가운데 = 1/√2");
        }

        [Test]
        public void 그림은_안쪽_46퍼센트가_투명하고_바깥으로_갈수록_붉고_진하다()
        {
            double r, g, b, a;
            FxRules.DmgVigSample(0.0, out r, out g, out b, out a);
            Assert.AreEqual(0, a, 1e-9, "한가운데는 완전 투명 — 3D 전투를 가리지 않는다");
            FxRules.DmgVigSample(FxRules.DmgVigStop0, out r, out g, out b, out a);
            Assert.AreEqual(0, a, 1e-9, "46% 정지점까지 투명");
            FxRules.DmgVigSample(FxRules.DmgVigStop1, out r, out g, out b, out a);
            Assert.AreEqual(FxRules.DmgVigMidA, a, 1e-9, "72% 정지점 = rgba(120,14,14,.34)");
            Assert.AreEqual(120 / 255.0, r, 1e-6); Assert.AreEqual(14 / 255.0, g, 1e-6); Assert.AreEqual(14 / 255.0, b, 1e-6);
            FxRules.DmgVigSample(1.0, out r, out g, out b, out a);
            Assert.AreEqual(FxRules.DmgVigEdgeA, a, 1e-9, "모서리 = rgba(255,58,44,.92)");
            Assert.AreEqual(1.0, r, 1e-6); Assert.AreEqual(58 / 255.0, g, 1e-6); Assert.AreEqual(44 / 255.0, b, 1e-6);
            // 첫 구간은 «검게» 가 아니라 «붉은 채 알파만» 올라야 한다(프리멀티플라이드 · css `transparent` 섞임).
            FxRules.DmgVigSample((FxRules.DmgVigStop0 + FxRules.DmgVigStop1) / 2, out r, out g, out b, out a);
            Assert.AreEqual(FxRules.DmgVigMidA / 2, a, 1e-9, "46~72% 는 알파가 선형으로 오른다");
            Assert.AreEqual(120 / 255.0, r, 1e-6, "그 구간의 색은 계속 rgb(120,14,14) — 검정으로 안 간다");
            Assert.Greater(r, g, "어느 자리든 붉은 쪽이다");
        }

        [Test]
        public void 알파는_반지름을_따라_단조증가한다()
        {
            double prev = -1;
            for (double t = 0; t <= 1.0001; t += 0.02)
            {
                double r, g, b, a;
                FxRules.DmgVigSample(t, out r, out g, out b, out a);
                Assert.GreaterOrEqual(a, prev - 1e-9, "t=" + t + " 에서 알파가 도로 내려갔다");
                prev = a;
            }
        }

        [Test]
        public void 세로_마스크는_위_82퍼센트를_두고_바닥에서_0이_된다()
        {
            Assert.AreEqual(1, FxRules.DmgVigMask(0), 1e-9, "씬 위");
            Assert.AreEqual(1, FxRules.DmgVigMask(FxRules.DmgVigMaskKeep), 1e-9, "82% 까지 그대로");
            Assert.AreEqual(0.5, FxRules.DmgVigMask(0.91), 1e-9, "82~100% 선형");
            Assert.AreEqual(0, FxRules.DmgVigMask(1), 1e-9, "바닥에서 0 — 시트 경계에서 «잘리는» 대신 사라진다");
            Assert.AreEqual(0, FxRules.DmgVigMask(2), 1e-9);
        }
    }
}
