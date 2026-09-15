using System;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>T117 — 정본 `UI.coinBurst` 의 셈(개수·라벨·격자·지연·키프레임)을 표(`Resources/CoinBurstUi.json`)로 재현한다 · JS 반올림 규칙.</summary>
    public class CoinBurstRulesTests
    {
        static CoinBurstSpec S()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            string file = Path.Combine(root, "Assets", "Forge", "Resources", "CoinBurstUi.json");
            return CoinBurstSpec.From(MiniJson.ParseObject(File.ReadAllText(file)));
        }
        static double Lo(double a, double b) { return a; }          // U.rand 대역: 최솟값
        static double Mid(double a, double b) { return (a + b) / 2; }

        [Test]
        public void 개수는_금액_눈금으로_3에서_10()
        {
            var s = S();
            Assert.AreEqual(0, CoinBurstRules.Count(s, 0), "0 이하면 연출 없음");
            Assert.AreEqual(0, CoinBurstRules.Count(s, -5));
            Assert.AreEqual(3, CoinBurstRules.Count(s, 1), "log10(1)=0 → 3");
            Assert.AreEqual(3, CoinBurstRules.Count(s, 1.9), "floor 뒤 1");
            Assert.AreEqual(6, CoinBurstRules.Count(s, 100), "3 + round(2·1.7=3.4) = 6");
            Assert.AreEqual(8, CoinBurstRules.Count(s, 1000), "3 + round(5.1) = 8");
            Assert.AreEqual(10, CoinBurstRules.Count(s, 1e6), "3 + round(10.2) = 13 → 상한 10");
            Assert.AreEqual(10, CoinBurstRules.Count(s, 1e30));
        }

        [Test]
        public void 금액_링은_표의_두_항을_환산해_큰_쪽이다()
        {
            var s = S();
            Assert.AreEqual(1.4, s.AmtRingMinPx, 1e-12, "정본 7428 --ol 의 절대 항 1.4px");
            Assert.AreEqual(0.075, s.AmtRingRem, 1e-12, "정본 7428 --ol 의 rem 항 .075rem");
            // 촬영 배율 2.164 · 1rem = 36.4 캔버스 px(844 기준): 1.4×2.164 = 3.03 > .075×36.4 = 2.73 — 절대 항이 이긴다
            Assert.AreEqual(1.4 * 2.164, CoinBurstRules.AmtRingCanvasPx(s, 2.164, 36.4), 1e-9);
            // rem 이 충분히 크면 rem 항이 이긴다(max 의 뜻)
            Assert.AreEqual(0.075 * 60.0, CoinBurstRules.AmtRingCanvasPx(s, 2.164, 60.0), 1e-9);
        }

        [Test]
        public void 라벨은_전부_합나누기개수_같은_값이고_JS_반올림이다()
        {
            Assert.AreEqual(17, CoinBurstRules.Per(100, 6), "16.67 → 17");
            Assert.AreEqual(1, CoinBurstRules.Per(1, 3), "최소 1");
            Assert.AreEqual(3, CoinBurstRules.Per(5, 2), "JS Math.round(2.5) = 3 — 은행가 반올림(2)이 아니다");
            Assert.AreEqual(2, CoinBurstRules.JsRound(1.5));
            Assert.AreEqual(1, CoinBurstRules.JsRound(1.4999));
            Assert.AreEqual(333, CoinBurstRules.Per(1000.9, 3), "총액은 먼저 floor(1000.9 → 1000) · 1000/3 = 333.33 → 333");
        }

        [Test]
        public void 줄수는_4까지_1_8까지_2_그_위_3()
        {
            var s = S();
            Assert.AreEqual(1, CoinBurstRules.Rows(s, 3)); Assert.AreEqual(1, CoinBurstRules.Rows(s, 4));
            Assert.AreEqual(2, CoinBurstRules.Rows(s, 5)); Assert.AreEqual(2, CoinBurstRules.Rows(s, 8));
            Assert.AreEqual(3, CoinBurstRules.Rows(s, 9)); Assert.AreEqual(3, CoinBurstRules.Rows(s, 10));
            // 10개 · 3줄: 칸 4/3/3 (ceil((10−r)/3))
            Assert.AreEqual(4, CoinBurstRules.ColsIn(10, 3, 0)); Assert.AreEqual(3, CoinBurstRules.ColsIn(10, 3, 1)); Assert.AreEqual(3, CoinBurstRules.ColsIn(10, 3, 2));
        }

        [Test]
        public void 격자_착지점은_줄마다_가운데_대칭이고_i는_줄에_라운드로빈()
        {
            var s = S();
            double rem = 16, cssPx = 1;
            var p = CoinBurstRules.Layout(s, 1e6, rem, cssPx, (a, b) => 0);   // 흔들림 0
            Assert.AreEqual(10, p.Length);
            for (int r = 0; r < 3; r++)
            {
                double sum = 0; int cnt = 0;
                for (int i = 0; i < p.Length; i++) if (p[i].Row == r) { sum += p[i].Dx; cnt++; }
                Assert.AreEqual(r == 0 ? 4 : 3, cnt, "줄 " + r + " 칸 수");
                Assert.AreEqual(0, sum, 1e-9, "줄 " + r + " 의 dx 합 = 0(가운데 대칭)");
            }
            Assert.AreEqual(0, p[0].Row); Assert.AreEqual(1, p[1].Row); Assert.AreEqual(2, p[2].Row); Assert.AreEqual(0, p[3].Row);
            // 줄 간격은 흔들지 않는다: drop = row0 + row·rowGap
            Assert.AreEqual(s.Row0Rem * rem, p[0].Drop, 1e-9);
            Assert.AreEqual((s.Row0Rem + s.RowGapRem) * rem, p[1].Drop, 1e-9);
            Assert.AreEqual((s.Row0Rem + 2 * s.RowGapRem) * rem, p[2].Drop, 1e-9);
            // 이웃 칸 간격 = colGap
            Assert.AreEqual(s.ColGapRem * rem, p[3].Dx - p[0].Dx, 1e-9);
            // 지연은 i·26 + rand(0,24) — 단조 증가
            for (int i = 1; i < p.Length; i++) Assert.Greater(p[i].DelayMs, p[i - 1].DelayMs);
            Assert.AreEqual(0, p[0].DelayMs, 1e-9);
            // 떠오름은 −(58~104)·cssPx
            var q = CoinBurstRules.Layout(s, 100, rem, 2.164, Mid);
            foreach (var c in q) { Assert.LessOrEqual(c.Rise, -s.RiseMinPx * 2.164 + 1e-9); Assert.GreaterOrEqual(c.Rise, -s.RiseMaxPx * 2.164 - 1e-9); }
        }

        [Test]
        public void 세로_키프레임은_45에서_꼭대기_72에서_착지_84에서_바운스_끝에서_사라진다()
        {
            var s = S();
            var p = CoinBurstRules.Layout(s, 100, 16, 1, Lo)[0];
            double bounce = s.BounceRem * 16;
            double y, a;
            CoinBurstRules.FlyY(s, p, bounce, 0, out y, out a);   Assert.AreEqual(0, y, 1e-9); Assert.AreEqual(1, a, 1e-9);
            CoinBurstRules.FlyY(s, p, bounce, 45, out y, out a);  Assert.AreEqual(p.Rise, y, 1e-9, "꼭대기 = rise(음수 = 위)"); Assert.AreEqual(1, a, 1e-9);
            CoinBurstRules.FlyY(s, p, bounce, 72, out y, out a);  Assert.AreEqual(p.Drop, y, 1e-9, "착지 = drop"); Assert.AreEqual(1, a, 1e-9, "착지 시점은 완전 불투명(`sell-coin-land-visible`)");
            CoinBurstRules.FlyY(s, p, bounce, 84, out y, out a);  Assert.AreEqual(p.Drop - bounce, y, 1e-9, "바운스 = drop − .5rem"); Assert.AreEqual(1, a, 1e-9);
            CoinBurstRules.FlyY(s, p, bounce, 100, out y, out a); Assert.AreEqual(p.Drop, y, 1e-9); Assert.AreEqual(0, a, 1e-9);
            CoinBurstRules.FlyY(s, p, bounce, 20, out y, out a);  Assert.Less(y, 0, "올라가는 중"); Assert.Greater(y, p.Rise, "아직 꼭대기 전");
            CoinBurstRules.FlyY(s, p, bounce, 92, out y, out a);  Assert.Greater(a, 0, "84→100 사이에서만 사라진다"); Assert.Less(a, 1);
            Assert.AreEqual(p.DelayMs + s.FlyMs * s.LandK, CoinBurstRules.LandMs(s, p), 1e-9);
            Assert.AreEqual(s.FlyMs + p.DelayMs + s.RemoveSlackMs, CoinBurstRules.PieceEndMs(s, p), 1e-9);
        }

        [Test]
        public void 금액_라벨은_임팩트_팝_뒤_천천히_밀려_올라가며_사라진다()
        {
            var s = S();
            double a, ty, sc;
            CoinBurstRules.Amt(s, 0, out a, out ty, out sc);   Assert.AreEqual(0, a, 1e-9); Assert.AreEqual(0.8, sc, 1e-9);
            CoinBurstRules.Amt(s, 7, out a, out ty, out sc);   Assert.AreEqual(1, a, 1e-9); Assert.AreEqual(1.16, sc, 1e-9, "임팩트 팝"); Assert.AreEqual(-64, ty, 1e-9);
            CoinBurstRules.Amt(s, 13, out a, out ty, out sc);  Assert.AreEqual(1, sc, 1e-9);
            CoinBurstRules.Amt(s, 45.5, out a, out ty, out sc); Assert.AreEqual(-100, ty, 1e-9, "13→78 은 linear — 한가운데는 −58 과 −142 의 중간");
            CoinBurstRules.Amt(s, 78, out a, out ty, out sc);  Assert.AreEqual(-142, ty, 1e-9, "상승 거리 −170% 를 늘리지 말 것(겹침)");
            CoinBurstRules.Amt(s, 100, out a, out ty, out sc); Assert.AreEqual(0, a, 1e-9); Assert.AreEqual(-170, ty, 1e-9);
            Assert.AreEqual(s.AmtMs + s.AmtSlackMs, CoinBurstRules.AmtEndMs(s), 1e-9);
        }
    }
}
