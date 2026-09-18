using System;
using NUnit.Framework;
using Forge.Core.BattleFx;

namespace Forge.Tests
{
    /// <summary>
    /// T467 — 전투 경고 부제의 등장 조임 «셈». 정본 `style.css` 431~436 `@keyframes bwsub { 0% { letter-spacing: 1.4em; text-indent: 1.4em }
    /// 16% { … .55em } }` · 416 `animation: bwsub 2s ease-out`. 값은 표가 쥐고 여기는 곡선만 잰다(유니티 없이).
    /// </summary>
    public class WarnSubSpacingTests
    {
        const double From = 1.4, To = 0.55, At = 0.16;

        [Test]
        public void 시작은_1_4em_이고_16퍼센트부터는_55em_에_정착한다()
        {
            Assert.AreEqual(From, FxRules.WarnSubSpacing(0, From, To, At), 1e-9, "0% = 1.4em");
            Assert.AreEqual(To, FxRules.WarnSubSpacing(At, From, To, At), 1e-9, "16% = .55em");
            Assert.AreEqual(To, FxRules.WarnSubSpacing(0.5, From, To, At), 1e-9, "50% 도 .55em(정착)");
            Assert.AreEqual(To, FxRules.WarnSubSpacing(1.0, From, To, At), 1e-9, "100% 도 .55em");
            Assert.AreEqual(From, FxRules.WarnSubSpacing(-0.1, From, To, At), 1e-9, "음수 진행도는 0% 로 본다");
        }

        [Test]
        public void 조이는_구간은_단조_감소이고_ease_out_이라_선형보다_목표에_가깝다()
        {
            double prev = From;
            for (int i = 1; i <= 100; i++)
            {
                double v = FxRules.WarnSubSpacing(At * i / 100.0, From, To, At);
                Assert.LessOrEqual(v, prev + 1e-12, "단조 감소");
                Assert.GreaterOrEqual(v, To - 1e-12, "정착값 아래로 안 내려간다");
                prev = v;
            }
            double mid = FxRules.WarnSubSpacing(At * 0.5, From, To, At);
            double linear = From + (To - From) * 0.5;
            Assert.Less(mid, linear, "ease-out(0,0,.58,1) 은 앞이 빠르다 — 중간점이 선형 중간(.975)보다 목표 쪽");
            Assert.Greater(mid, To, "그래도 아직 정착 전이다");
        }

        [Test]
        public void 정착_진행도가_0_이면_처음부터_정착값이다()
        {
            Assert.AreEqual(To, FxRules.WarnSubSpacing(0, From, To, 0), 1e-9);
        }
    }
}
