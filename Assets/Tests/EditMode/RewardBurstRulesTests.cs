using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core.CraftFx;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>T134 — 정본 `UI.rewardBurst` 의 셈(개수 눈금·지연·흩어짐·승격·카운터·라벨 자리·키프레임)을 표(`Resources/RewardBurstUi.json`)로 재현한다 · JS 반올림.</summary>
    public class RewardBurstRulesTests
    {
        static RewardBurstSpec S()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            string file = Path.Combine(root, "Assets", "Forge", "Resources", "RewardBurstUi.json");
            return RewardBurstSpec.From(MiniJson.ParseObject(File.ReadAllText(file)));
        }
        static double Lo(double a, double b) { return a; }
        static double Mid(double a, double b) { return (a + b) / 2; }
        const double CssPx = 2.164;   // catalog anvil_fx_px 와 같은 수(식 검산용)

        [Test]
        public void 개수는_양의_로그_눈금으로_2에서_7()
        {
            var s = S();
            Assert.AreEqual(0, RewardBurstRules.Count(s, 0), "0 이면 아이콘 없음");
            Assert.AreEqual(0, RewardBurstRules.Count(s, double.NaN));
            Assert.AreEqual(2, RewardBurstRules.Count(s, 1), "log10(1)=0 → 2");
            Assert.AreEqual(3, RewardBurstRules.Count(s, 5), "2 + round(.699·1.3=.91) = 3");
            Assert.AreEqual(5, RewardBurstRules.Count(s, 100), "2 + round(2.6) = 5");
            Assert.AreEqual(7, RewardBurstRules.Count(s, 1e4), "2 + round(5.2) = 7");
            Assert.AreEqual(7, RewardBurstRules.Count(s, 1e9), "상한 7");
        }

        [Test]
        public void 수령_목록은_floor_뒤_0_초과만_순서대로()
        {
            var list = RewardBurstRules.Entries(new[] {
                new KeyValuePair<string, double>("coins", 100.9), new KeyValuePair<string, double>("gems", 0),
                new KeyValuePair<string, double>("hammers", -3), new KeyValuePair<string, double>("tickets", 2), new KeyValuePair<string, double>("potions", double.NaN) });
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("coins", list[0].Currency); Assert.AreEqual(100, list[0].Amount, "floor(100.9)");
            Assert.AreEqual("tickets", list[1].Currency); Assert.AreEqual(2, list[1].Amount);
            Assert.AreEqual(0, RewardBurstRules.Entries(null).Count);
        }

        [Test]
        public void 지연과_유예와_총_수명()
        {
            var s = S();
            Assert.AreEqual(2 * 90 + 7 * 44 + 820 + 120, RewardBurstRules.HoldMs(s, 2), "_toastHoldUntil");
            Assert.AreEqual(90 + 4 * 44 + 22, RewardBurstRules.LastDelayMs(s, 1, 5), "ci·90 + (n−1)·44 + 22");
            Assert.AreEqual(90 + 2 * 44, RewardBurstRules.IconDelayMs(s, 1, 2, Lo), "지터 0");
            Assert.AreEqual(90 + 2 * 44 + 22, RewardBurstRules.IconDelayMs(s, 1, 2, (a, b) => b), "지터 상한 22");
            double last = RewardBurstRules.LastDelayMs(s, 1, 5);
            Assert.AreEqual(last + 820 * 0.94 + 140, RewardBurstRules.PopMs(s, last), 1e-9, "마침표 = 마지막 착지 + 140");
            Assert.AreEqual(last + 820 + 460, RewardBurstRules.AnchorOutMs(s, last), 1e-9);
            Assert.AreEqual(110, RewardBurstRules.AmtDelayMs(s, 1)); Assert.AreEqual(720 + 110, RewardBurstRules.AmtEndMs(s, 1));
            var p = new RewardIcon { DelayMs = 100 };
            Assert.AreEqual(100 + 820 * 0.94, RewardBurstRules.LandMs(s, p), 1e-9);
            Assert.AreEqual(100 + 820 + 60, RewardBurstRules.IconEndMs(s, p), 1e-9);
            Assert.AreEqual(last + 820 + 460 + 340, RewardBurstRules.TotalMs(s, 2, 5), 1e-9, "앵커 페이드 끝이 제일 늦다");
            Assert.AreEqual(0, RewardBurstRules.TotalMs(s, 0, 0));
        }

        [Test]
        public void 흩어짐은_항상_버튼_위쪽이고_반경은_18에서_40px()
        {
            var s = S();
            Func<double, double, double>[] rands = {
                Lo, Mid, (a, b) => b - 1e-9,
                (a, b) => b == Math.PI * 2 ? Math.PI / 2 : Mid(a, b),      // 각도만 위(sin 1)
                (a, b) => b == Math.PI * 2 ? Math.PI * 1.5 : Mid(a, b),    // 각도 아래(sin −1) — 그래도 위로
            };
            foreach (var r in rands)
            {
                RewardIcon[] ic = RewardBurstRules.Icons(s, 0, 3, CssPx, r);
                Assert.AreEqual(3, ic.Length);
                for (int i = 0; i < 3; i++)
                {
                    Assert.LessOrEqual(ic[i].Ry, -10 * CssPx + 1e-9, "ry = −|sin|·rad·.9 − 10px — 아래로 안 퍼진다");
                    Assert.GreaterOrEqual(ic[i].Ry, -(40 * 0.9 + 10) * CssPx - 1e-9);
                    Assert.LessOrEqual(Math.Abs(ic[i].Rx), 40 * CssPx + 1e-9);
                    Assert.LessOrEqual(Math.Abs(ic[i].RotDeg), 160);
                    Assert.AreEqual(i, ic[i].Index);
                    Assert.GreaterOrEqual(ic[i].DelayMs, i * 44);
                }
            }
            RewardIcon[] up = RewardBurstRules.Icons(s, 0, 1, 1, (a, b) => b == Math.PI * 2 ? Math.PI / 2 : Mid(a, b));
            Assert.AreEqual(-(29 * 0.9 + 10), up[0].Ry, 1e-9, "각도 90° · 반경 29 → −36.1");
            Assert.AreEqual(0, up[0].Rx, 1e-9);
        }

        [Test]
        public void 누적_카운터_값은_JS_반올림()
        {
            Assert.AreEqual(20, RewardBurstRules.Per(100, 0, 5)); Assert.AreEqual(100, RewardBurstRules.Per(100, 4, 5), "마지막은 정확히 양");
            Assert.AreEqual(2, RewardBurstRules.Per(7, 0, 3), "2.33 → 2"); Assert.AreEqual(5, RewardBurstRules.Per(7, 1, 3), "4.67 → 5");
            Assert.AreEqual(3, RewardBurstRules.Per(5, 0, 2), "2.5 → 3 (은행가 반올림 2 가 아니다)");
            Assert.AreEqual(3, RewardBurstRules.JsRound(2.5)); Assert.AreEqual(2, RewardBurstRules.JsRound(2.4999));
        }

        [Test]
        public void 출발점은_버튼_상단_1_3_또는_상자_가운데()
        {
            var s = S();
            double sx, sy; double? top;
            RewardBurstRules.Source(s, 1080, 1920, true, 100, 200, 50, 40, out sx, out sy, out top);
            Assert.AreEqual(125, sx); Assert.AreEqual(200 + 40 * 0.32, sy, 1e-9); Assert.AreEqual(200, top.Value);
            RewardBurstRules.Source(s, 1080, 1920, false, 0, 0, 0, 0, out sx, out sy, out top);
            Assert.AreEqual(540, sx); Assert.AreEqual(1920 * 0.58, sy, 1e-9); Assert.IsNull(top, "srcTop 없음 → 라벨은 출발점 위 16px");
            RewardBurstRules.Source(s, 1080, 1920, true, 5, 5, 0, 0, out sx, out sy, out top);
            Assert.IsNull(top, "크기 0 인 버튼은 없는 것");
        }

        [Test]
        public void 가려진_도착점은_카드_상단으로_승격하고_폴백은_화면_위_가운데()
        {
            var s = S();
            var t = new RewardTarget { X = 50, Y = 500 };
            var a = RewardBurstRules.Promote(s, t, false, false, 0, 0, 0, 2);
            Assert.IsFalse(a.Covered); Assert.AreEqual(500, a.Y);
            var b = RewardBurstRules.Promote(s, t, true, false, 0, 0, 0, 2);
            Assert.IsTrue(b.Covered); Assert.IsFalse(b.Promoted); Assert.AreEqual(500, b.Y, "딤만 잡히면 자리는 그대로(앵커만 선다)");
            var c = RewardBurstRules.Promote(s, t, true, true, 100, 300, 980, 2);
            Assert.IsTrue(c.Promoted); Assert.AreEqual(300 + 18 * 2, c.Y, "카드 상단 + 18px"); Assert.AreEqual(100 + 26 * 2, c.X, "좌우 26px 안으로");
            var d = RewardBurstRules.Promote(s, new RewardTarget { X = 1000, Y = 500 }, true, true, 100, -100, 980, 2);
            Assert.AreEqual(16 * 2, d.Y, "최소 16px"); Assert.AreEqual(980 - 26 * 2, d.X);
            var f = RewardBurstRules.FallbackTarget(s, 1080, 2);
            Assert.AreEqual(540, f.X); Assert.AreEqual(20, f.Y); Assert.IsTrue(f.Covered);
        }

        [Test]
        public void 카운터와_획득량_라벨의_자리()
        {
            var s = S();
            Assert.AreEqual(20, RewardBurstRules.TickY(s, new RewardTarget { Y = 50, Covered = true }, 1), "가려짐 → 앵커 위 30");
            Assert.AreEqual(8, RewardBurstRules.TickY(s, new RewardTarget { Y = 5, Covered = true }, 1), "최소 8");
            Assert.AreEqual(72, RewardBurstRules.TickY(s, new RewardTarget { Y = 50 }, 1), "pill 아래 22");
            Assert.AreEqual(42, RewardBurstRules.TickY(s, new RewardTarget { Y = 5 }, 1), "max(y,20)+22");
            double lx, ly;
            RewardBurstRules.AmtPos(s, 300, 400, 200, 1, 500, 1, out lx, out ly);
            Assert.AreEqual(236, lx, "sx − 64"); Assert.AreEqual(200 - 42 - 30, ly, "srcTop − 42 − ci·30");
            RewardBurstRules.AmtPos(s, 10, 400, 200, 0, 500, 1, out lx, out ly);
            Assert.AreEqual(44, lx, "왼쪽 44 안");
            RewardBurstRules.AmtPos(s, 300, 400, null, 0, 500, 1, out lx, out ly);
            Assert.AreEqual(300, lx); Assert.AreEqual(384, ly, "srcTop 없음 → sy − 16");
        }

        [Test]
        public void 키프레임은_정본_값을_양_끝과_중간_키에서_그대로_준다()
        {
            var s = S();
            Assert.AreEqual(0, RewardBurstRules.FlyX(s, 0, 30, -200)); Assert.AreEqual(30, RewardBurstRules.FlyX(s, 34, 30, -200), 1e-9); Assert.AreEqual(-200, RewardBurstRules.FlyX(s, 100, 30, -200), 1e-9);
            Assert.AreEqual(-200, RewardBurstRules.FlyX(s, 150, 30, -200), 1e-9, "fill both — 범위 밖은 끝값");
            double mid = RewardBurstRules.FlyX(s, 17, 30, -200);
            Assert.IsTrue(mid > 0 && mid < 30, "첫 구간은 0 → rx 사이 · 지금 " + mid);
            double y, rot, sc, a;
            RewardBurstRules.FlyY(s, 0, -36, -700, 120, out y, out rot, out sc, out a);
            Assert.AreEqual(0, y); Assert.AreEqual(0, rot); Assert.AreEqual(0.35, sc); Assert.AreEqual(0.2, a);
            RewardBurstRules.FlyY(s, 34, -36, -700, 120, out y, out rot, out sc, out a);
            Assert.AreEqual(-36, y, 1e-9); Assert.AreEqual(36, rot, 1e-9, "rot·.3"); Assert.AreEqual(1, sc, 1e-9); Assert.AreEqual(1, a, 1e-9);
            RewardBurstRules.FlyY(s, 94, -36, -700, 120, out y, out rot, out sc, out a);
            Assert.AreEqual(-700, y, 1e-9); Assert.AreEqual(120, rot, 1e-9); Assert.AreEqual(0.62, sc, 1e-9); Assert.AreEqual(1, a, 1e-9);
            RewardBurstRules.FlyY(s, 100, -36, -700, 120, out y, out rot, out sc, out a);
            Assert.AreEqual(-700, y, 1e-9); Assert.AreEqual(0.5, sc, 1e-9); Assert.AreEqual(0, a, 1e-9);
            double o, bw;
            RewardBurstRules.Glow(s, 0, out sc, out o); Assert.AreEqual(0.2, sc); Assert.AreEqual(0, o);
            RewardBurstRules.Glow(s, 14, out sc, out o); Assert.AreEqual(0.8, sc, 1e-9); Assert.AreEqual(1, o, 1e-9);
            RewardBurstRules.Glow(s, 100, out sc, out o); Assert.AreEqual(1.6, sc, 1e-9); Assert.AreEqual(0, o, 1e-9);
            RewardBurstRules.Ring(s, 0, out sc, out o, out bw); Assert.AreEqual(0.3, sc); Assert.AreEqual(1, o); Assert.AreEqual(0.4, bw);
            RewardBurstRules.Ring(s, 100, out sc, out o, out bw); Assert.AreEqual(2.1, sc, 1e-9); Assert.AreEqual(0, o, 1e-9); Assert.AreEqual(0.08, bw, 1e-9);
            RewardBurstRules.Pop(s, 24, out sc, out rot, out o); Assert.AreEqual(1.12, sc, 1e-9); Assert.AreEqual(0, rot, 1e-9); Assert.AreEqual(1, o, 1e-9);
            RewardBurstRules.Pop(s, 0, out sc, out rot, out o); Assert.AreEqual(-40, rot);
            RewardBurstRules.AnchorIn(s, 60, out sc, out o); Assert.AreEqual(1.14, sc, 1e-9); Assert.AreEqual(1, o, 1e-9);
            double ty;
            RewardBurstRules.Amt(s, 12, out o, out ty, out sc); Assert.AreEqual(1, o, 1e-9); Assert.AreEqual(-0.3, ty, 1e-9); Assert.AreEqual(1.35, sc, 1e-9);
            RewardBurstRules.Amt(s, 100, out o, out ty, out sc); Assert.AreEqual(0, o, 1e-9); Assert.AreEqual(-2, ty, 1e-9); Assert.AreEqual(0.92, sc, 1e-9);
            RewardBurstRules.TickBump(s, 40, out sc, out ty); Assert.AreEqual(1.22, sc, 1e-9); Assert.AreEqual(-0.12, ty, 1e-9);
            double br;
            RewardBurstRules.Pulse(s, 35, false, out sc, out br); Assert.AreEqual(1.26, sc, 1e-9); Assert.AreEqual(1, br, 1e-9);
            RewardBurstRules.Pulse(s, 35, true, out sc, out br); Assert.AreEqual(1, sc, "밴드는 크기가 안 변한다(rw-pulse-band)"); Assert.AreEqual(1, br, 1e-9);
        }

        [Test]
        public void 타이밍_키워드와_표_읽기()
        {
            CssEase eo = RewardBurstSpec.EaseOf("ease-out");
            Assert.AreEqual(new CssEase(0, 0, 0.58, 1).Ease(0.5), eo.Ease(0.5), 1e-9, "CSS ease-out = cubic-bezier(0,0,.58,1)");
            Assert.AreEqual(0.5, RewardBurstSpec.EaseOf("linear").Ease(0.5), 1e-9);
            Assert.AreEqual(0.5, RewardBurstSpec.EaseOf(null).Ease(0.5), 1e-9);
            Assert.Throws<FormatException>(() => RewardBurstSpec.EaseOf("bouncy"));
            var s = S();
            Assert.AreEqual(7, s.CurrencyIcon.Count, "정본 CURRENCY_ICON 일곱");
            Assert.AreEqual("coin", s.CurrencyIcon["coins"]); Assert.AreEqual("hammer", s.CurrencyIcon["hammers"]); Assert.AreEqual("egg", s.CurrencyIcon["eggCurrency"]);
            Assert.AreEqual(2, s.CurrencyPill.Count, "pill 이 있는 재화는 코인·젬뿐(정본 pillSel)");
            Assert.AreEqual("pill-coin", s.CurrencyPill["coins"]); Assert.IsFalse(s.CurrencyPill.Has("hammers"));
            Assert.AreEqual(820, s.FlyMs); Assert.AreEqual(4, s.AmtStrokePx); Assert.AreEqual(3.5, s.TickStrokePx);
        }
    }
}
