using System;
using NUnit.Framework;
using Forge.Core.Ui;

namespace Forge.Tests.EditMode
{
    /// <summary>
    /// T368 — 정본 `repeating-linear-gradient` 줄무늬의 셈(<see cref="StripeRules"/>).
    /// 재는 것 셋: ⓐ CSS 각도 뜻(0 = 위 · 90 = 오른쪽 · −45 = 오른쪽 위) ⓑ 주기 안 «앞 색» 의 경계 ⓒ 기울면 늘어나는 가로 타일(정본이 1.556rem 으로 적어 둔 그 수).
    /// </summary>
    public class StripeRulesTests
    {
        [Test]
        public void CSS_각도_뜻_그대로_0은_위_90은_오른쪽_음45는_오른쪽_위()
        {
            double ax, ay;
            StripeRules.Axis(0, out ax, out ay);
            Assert.AreEqual(0.0, ax, 1e-9, "0deg 는 가로 성분 0");
            Assert.AreEqual(-1.0, ay, 1e-9, "0deg 는 위(y 아래가 +)");
            StripeRules.Axis(90, out ax, out ay);
            Assert.AreEqual(1.0, ax, 1e-9, "90deg 는 오른쪽");
            Assert.AreEqual(0.0, ay, 1e-9);
            StripeRules.Axis(-45, out ax, out ay);
            Assert.AreEqual(-Math.Sqrt(0.5), ax, 1e-9, "−45deg 는 왼쪽 위로 가는 축");
            Assert.AreEqual(-Math.Sqrt(0.5), ay, 1e-9);
        }

        [Test]
        public void 가로_줄무늬는_주기_안_앞_d_만_잉크다()
        {
            // 정본 2548 `.league-reward-tier`: 주기 31px · 대시 16px(앱 폭 499 의 6.25% / 3.23%)
            const double p = 31.0, d = 16.0;
            Assert.IsTrue(StripeRules.IsInk(0, 0, 90, p, d, 0), "주기 첫 화소는 대시");
            Assert.IsTrue(StripeRules.IsInk(15.9, 0, 90, p, d, 0), "대시 끝 직전");
            Assert.IsFalse(StripeRules.IsInk(16.0, 0, 90, p, d, 0), "대시 끝은 빈칸의 시작");
            Assert.IsFalse(StripeRules.IsInk(30.9, 0, 90, p, d, 0), "주기 끝 직전은 빈칸");
            Assert.IsTrue(StripeRules.IsInk(31.0, 0, 90, p, d, 0), "다음 주기는 다시 대시");
            Assert.IsTrue(StripeRules.IsInk(-31.0, 0, 90, p, d, 0), "음수 자리도 같은 주기(왼쪽으로 이어진다)");
            Assert.IsFalse(StripeRules.IsInk(-1.0, 0, 90, p, d, 0), "−1 은 앞 주기의 빈칸");
            Assert.IsFalse(StripeRules.IsInk(0, 99, 90, p, d, 0) != StripeRules.IsInk(0, 0, 90, p, d, 0), "가로 줄무늬는 y 를 안 탄다");
        }

        [Test]
        public void 위상은_줄무늬를_축_방향으로_당긴다()
        {
            const double p = 100.0, d = 50.0;
            Assert.IsTrue(StripeRules.IsInk(0, 0, 90, p, d, 0));
            Assert.IsFalse(StripeRules.IsInk(0, 0, 90, p, d, 50), "반 주기를 당기면 x=0 이 빈칸으로 바뀐다");
            Assert.IsTrue(StripeRules.IsInk(50, 0, 90, p, d, 50));
            // 정본 4206 은 «반 대시만큼 당겨 x=0 에 대시 중심을 맞춘다» — 당김이 대시의 절반이면 x=0 이 대시 한가운데다
            Assert.IsTrue(StripeRules.IsInk(0, 0, 90, p, d, -25), "−¼주기 당김: x=0 은 대시 안");
            Assert.IsTrue(StripeRules.IsInk(24, 0, 90, p, d, -25));
            Assert.IsFalse(StripeRules.IsInk(26, 0, 90, p, d, -25), "그 대시는 x=25 에서 끝난다");
        }

        [Test]
        public void 기울면_가로_타일이_늘어난다_정본_1556rem_과_같은_수()
        {
            // 정본 395 `.bw-hazard`: 주기 1.1rem · background-size 1.556rem = 1.1 × √2
            double tile = StripeRules.TileWidth(-45, 1.1, 1000);
            Assert.AreEqual(1.1 * Math.Sqrt(2.0), tile, 1e-6, "−45° 주기의 가로축 환산");
            Assert.AreEqual(1.556, tile, 0.001, "정본이 적어 둔 1.556rem 과 같다");
            Assert.AreEqual(31.0, StripeRules.TileWidth(90, 31.0, 1000), 1e-9, "가로 줄무늬는 주기 그대로");
            Assert.AreEqual(1000.0, StripeRules.TileWidth(0, 31.0, 1000), 1e-9, "세로 줄무늬는 가로로 안 되풀이된다 — 상한에서 멎는다");
        }

        [Test]
        public void 사선_줄무늬는_오른쪽_위로_같은_띠가_이어진다()
        {
            const double p = 20.0, d = 10.0;
            bool at00 = StripeRules.IsInk(0, 0, -45, p, d, 0);
            // −45° 축은 (−√2/2, −√2/2) 라 (x+1, y−1) 로 가면 축 자리가 그대로다 = 같은 띠
            Assert.AreEqual(at00, StripeRules.IsInk(5, -5, -45, p, d, 0), "오른쪽 위로 간 화소는 같은 띠");
            Assert.AreEqual(at00, StripeRules.IsInk(-5, 5, -45, p, d, 0), "왼쪽 아래도 같은 띠");
            // 축을 따라 반 주기를 가면 뒤집힌다(축 방향은 왼쪽 위)
            double half = p * 0.5 / Math.Sqrt(2.0);
            Assert.AreNotEqual(at00, StripeRules.IsInk(-half, -half, -45, p, d, 0), "축으로 반 주기 가면 색이 바뀐다");
        }

        [Test]
        public void 타일_수와_비율_대시와_흐름_위상()
        {
            Assert.AreEqual(1, StripeRules.TileCount(0, 10), "폭이 0이면 한 장");
            Assert.AreEqual(4, StripeRules.TileCount(31, 10), "31 을 10짜리로 덮으려면 네 장");
            Assert.AreEqual(3, StripeRules.TileCount(30, 10));
            Assert.AreEqual(50.0, StripeRules.DashFromRatio(100, 0.5), 1e-9, "정본 `A 0 50%`");
            Assert.AreEqual(0.0, StripeRules.DashFromRatio(100, -1), 1e-9, "비율은 0~1 로 잘린다");
            Assert.AreEqual(100.0, StripeRules.DashFromRatio(100, 2), 1e-9);
            // 흐름: .62s 에 한 타일 · 절반 지점이면 반 타일의 축 성분만큼 당겨져 있다
            double ph = StripeRules.ScrollPhase(0.31, 0.62, 1.556, -45);
            Assert.AreEqual(0.5 * 1.556 * Math.Sqrt(0.5), ph, 1e-6);
            Assert.AreEqual(0.0, StripeRules.ScrollPhase(0.62, 0.62, 1.556, -45), 1e-9, "한 바퀴를 돌면 제자리");
            Assert.AreEqual(StripeRules.ScrollPhase(0.1, 0.62, 1.556, -45), StripeRules.ScrollPhase(0.72, 0.62, 1.556, -45), 1e-9, "다음 바퀴의 같은 자리");
            Assert.AreEqual(0.0, StripeRules.ScrollPhase(1, 0, 1, 90), 1e-9, "한 바퀴가 0초면 안 흐른다");
        }
    }
}
