using System;
using NUnit.Framework;
using Forge.Core.Ui;

namespace Forge.Tests.EditMode
{
    /// <summary>
    /// T385 — 구슬 위 아이콘 조명 겹의 셈(<see cref="OrbIconRules"/>).
    /// 재는 것 셋: ⓐ `::after` 상자(−60% · 220%) → 부모 기준 환산 ⓑ 방사형 감쇠 ⓒ **아이콘 기준을 구체 기준으로 옮기는 자리** —
    /// 정본 수를 클론 아이콘 크기에 그대로 쓰면 빛이 다른 데서 난다는 것을 여기서 못 박는다.
    /// </summary>
    public class OrbIconRulesTests
    {
        // 정본 `.sr-ico::after`(style.css 6539) 그대로.
        static OrbIconRules.OverlayBox Box() { return new OrbIconRules.OverlayBox(-0.6, -0.6, 2.2, 2.2); }

        // 정본 `.sr-orb` 첫 겹(6500) = 겹 하나(6544) — 16% 13% at 36% 19%, 투명 72%.
        const double L1Rx = 0.16, L1Ry = 0.13, L1AtX = 0.36, L1AtY = 0.19, L1End = 0.72;
        // 둘째 겹(6501 = 6545) — 58% 52% at 50% 15%, 투명 64%.
        const double L2AtX = 0.5, L2AtY = 0.15;

        // 정본은 `.sr-orb` font-size 2.4rem 안의 1em 아이콘이다.
        const double JeongbonIconFrac = 1.0 / 2.4;
        // 구체 하이라이트가 서는 자리 — 정본 19%(클론 `sr-hilite` 도 위에서 19%).
        const double OrbHiliteY = 0.19;

        [Test]
        public void 겹_상자는_아이콘_한_변의_2_2배이고_위왼쪽으로_0_6_나간다()
        {
            OrbIconRules.OverlayBox b = Box();
            Assert.AreEqual(0.192, OrbIconRules.CentreX(b, L1AtX), 1e-9, "−0.6 + 2.2×0.36");
            Assert.AreEqual(-0.182, OrbIconRules.CentreY(b, L1AtY), 1e-9, "−0.6 + 2.2×0.19 — 아이콘 윗변보다 위다");
            Assert.AreEqual(0.352, OrbIconRules.RadiusX(b, L1Rx), 1e-9, "2.2×0.16");
            Assert.AreEqual(0.286, OrbIconRules.RadiusY(b, L1Ry), 1e-9, "2.2×0.13");
        }

        [Test]
        public void 두_겹_다_아이콘_윗변_밖에서_빛난다()
        {
            OrbIconRules.OverlayBox b = Box();
            Assert.Less(OrbIconRules.CentreY(b, L1AtY), 0.0, "첫 겹 중심은 아이콘 위");
            Assert.Less(OrbIconRules.CentreY(b, L2AtY), 0.0, "둘째 겹 중심도 아이콘 위");
            Assert.Less(OrbIconRules.CentreX(b, L1AtX), 0.5, "첫 겹은 왼쪽 — 구체 하이라이트와 같은 쪽");
        }

        [Test]
        public void 방사형_감쇠는_중심_1_마지막_정지_0_이고_사이는_줄기만_한다()
        {
            Assert.AreEqual(1.0, OrbIconRules.Falloff(0.0, L1End), 1e-12);
            Assert.AreEqual(0.0, OrbIconRules.Falloff(L1End, L1End), 1e-12, "마지막 정지에서 투명");
            Assert.AreEqual(0.0, OrbIconRules.Falloff(L1End * 2, L1End), 1e-12, "정지 밖은 계속 투명");
            double prev = 1.0;
            for (int i = 1; i <= 20; i++)
            {
                double v = OrbIconRules.Falloff(L1End * i / 20.0, L1End);
                Assert.LessOrEqual(v, prev + 1e-12, "감쇠는 안 올라간다");
                prev = v;
            }
            Assert.Throws<ArgumentOutOfRangeException>(delegate { OrbIconRules.Falloff(0.1, 0.0); });
        }

        [Test]
        public void 타원_거리는_반지름_위에서_1_이다()
        {
            Assert.AreEqual(1.0, OrbIconRules.EllipseT(0.352, 0.0, 0.0, 0.0, 0.352, 0.286), 1e-12, "가로 반지름 위");
            Assert.AreEqual(1.0, OrbIconRules.EllipseT(0.0, 0.286, 0.0, 0.0, 0.352, 0.286), 1e-12, "세로 반지름 위");
            Assert.AreEqual(0.0, OrbIconRules.EllipseT(0.0, 0.0, 0.0, 0.0, 0.352, 0.286), 1e-12, "중심");
            Assert.Throws<ArgumentOutOfRangeException>(delegate { OrbIconRules.EllipseT(0, 0, 0, 0, 0, 1); });
        }

        [Test]
        public void 정본_비율에서는_겹의_빛이_구체_하이라이트_자리에_온다()
        {
            OrbIconRules.OverlayBox b = Box();
            double y = OrbIconRules.ToOrbFrac(JeongbonIconFrac, OrbIconRules.CentreY(b, L1AtY));
            // 0.5 − (1/2.4)/2 + (−0.182)×(1/2.4) = 0.2917 − 0.0758 = 0.2158
            Assert.AreEqual(0.2158, y, 1e-3, "정본 아이콘 비율에서 첫 겹은 구체 위에서 21.6%");
            Assert.Less(Math.Abs(y - OrbHiliteY), 0.03, "구체 하이라이트 19% 와 3%p 안쪽 — 주석의 «같은 위치» 가 이 뜻이다");
        }

        [Test]
        public void 클론_아이콘_비율에_그대로_쓰면_빛이_다른_데서_난다()
        {
            OrbIconRules.OverlayBox b = Box();
            // 클론 `SkillSummonResult` 는 아이콘을 칸의 0.62 로 놓는다 — 정본(0.417)보다 크다.
            double y = OrbIconRules.ToOrbFrac(0.62, OrbIconRules.CentreY(b, L1AtY));
            Assert.AreEqual(0.0772, y, 1e-3, "0.19 + (−0.182)×0.62");
            Assert.Greater(Math.Abs(y - OrbHiliteY), 0.1,
                "구체 하이라이트에서 10%p 넘게 어긋난다 — 배선 회차는 겹을 **구체 기준**으로 놓아야 한다");
        }

        [Test]
        public void screen_합성은_어두운_바탕에서_더하기와_가깝다()
        {
            Assert.AreEqual(0.5, OrbIconRules.ScreenBlend(0.5, 0.0), 1e-12, "투명 겹은 바탕 그대로");
            Assert.AreEqual(1.0, OrbIconRules.ScreenBlend(0.5, 1.0), 1e-12, "흰 겹은 흰색");
            Assert.AreEqual(OrbIconRules.ScreenBlend(0.3, 0.7), OrbIconRules.ScreenBlend(0.7, 0.3), 1e-12, "자리를 바꿔도 같다");
            double dark = Math.Abs(OrbIconRules.ScreenBlend(0.1, 0.35) - (0.1 + 0.35));
            double light = Math.Abs(OrbIconRules.ScreenBlend(0.8, 0.35) - (0.8 + 0.35));
            Assert.Less(dark, light, "어두운 바탕일수록 더하기 근사가 덜 어긋난다");
        }
    }
}
