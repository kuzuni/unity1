using System;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>T118 — 정본 `UI.playEquipSwapFx` 의 셈(방향·회전·착지 반경·착지 x·낙하·카드 띠 비켜서기·튕김)과 키프레임 여섯을 표(`Resources/EquipSwapUi.json`)로 재현한다.</summary>
    public class EquipSwapRulesTests
    {
        static EquipSwapSpec S()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            string file = Path.Combine(root, "Assets", "Forge", "Resources", "EquipSwapUi.json");
            return EquipSwapSpec.From(MiniJson.ParseObject(File.ReadAllText(file)));
        }
        const double CssPx = 2.164, Rem = 34.56;   // anvil_fx_px · 1rem(기준 캔버스) — 값 자체는 식 검산용
        static double Lo(double a, double b) { return a; }
        static double Hi(double a, double b) { return b; }
        /// <summary>가장자리 칸(무기 · 왼쪽) — 430×932 원작 실측 꼴을 기준 캔버스로 옮긴 값.</summary>
        static EquipSwapGrab Left() { return new EquipSwapGrab { Cx = 120, Cy = 1300, W = 150, H = 150, HostW = 1080, HostH = 1920, FloorY = 1560 }; }
        static EquipSwapGrab Right() { var g = Left(); g.Cx = 960; return g; }

        [Test]
        public void 표는_정본_상수와_같다()
        {
            var s = S();
            Assert.AreEqual(900, s.FlyMs); Assert.AreEqual(0.58, s.LandK, 1e-12); Assert.AreEqual(340, s.SnapMs); Assert.AreEqual(130, s.SnapDelayMs); Assert.AreEqual(0.55, s.Shrink, 1e-12);
            Assert.AreEqual(8, s.TiltMinDeg); Assert.AreEqual(22, s.TiltMaxDeg); Assert.AreEqual(1, s.TurnsMin); Assert.AreEqual(2, s.TurnsMax); Assert.AreEqual(360, s.TurnDeg);
            Assert.AreEqual(522, EquipSwapRules.LandMs(s), 1e-9, "착지 = 900·.58");
            Assert.AreEqual(1060, EquipSwapRules.FlyEndMs(s), 1e-9, "걷기 = 900+160");
            Assert.AreEqual(130, EquipSwapRules.SnapStartMs(s), 1e-9); Assert.AreEqual(470, EquipSwapRules.SnapEndMs(s), 1e-9);
            Assert.AreEqual(8, s.Y.Length, "eqswY 키 8"); Assert.AreEqual(3, s.X.Length); Assert.AreEqual(3, s.R.Length); Assert.AreEqual(6, s.Squash.Length); Assert.AreEqual(3, s.Dust.Length); Assert.AreEqual(5, s.Snap.Length);
        }

        [Test]
        public void 방향은_바깥쪽이고_회전은_정수_바퀴에_기울기_8에서_22()
        {
            var s = S();
            var pL = EquipSwapRules.Plan(s, Left(), CssPx, Lo);     // tilt 8 · rand(0,1)=0 → 1바퀴
            Assert.AreEqual(-1, pL.Dir, "왼쪽 칸은 왼쪽(바깥)으로");
            Assert.AreEqual(8, pL.Tilt, 1e-12); Assert.AreEqual(-(360 + 8), pL.Spin, 1e-12);
            var pR = EquipSwapRules.Plan(s, Right(), CssPx, Hi);    // tilt 22 · rand(0,1)=1 → 2바퀴
            Assert.AreEqual(1, pR.Dir); Assert.AreEqual(22, pR.Tilt, 1e-12); Assert.AreEqual(720 + 22, pR.Spin, 1e-12);
            // 임의 난수: |spin| − tilt 는 360 또는 720
            var rnd = new Random(7);
            for (int i = 0; i < 50; i++)
            {
                var p = EquipSwapRules.Plan(s, Left(), CssPx, (a, b) => a + rnd.NextDouble() * (b - a));
                double turns = (Math.Abs(p.Spin) - p.Tilt) / 360.0;
                Assert.IsTrue(Math.Abs(turns - 1) < 1e-9 || Math.Abs(turns - 2) < 1e-9, "정수 바퀴: " + turns);
                Assert.IsTrue(p.Tilt >= 8 && p.Tilt < 22);
                Assert.IsTrue(p.Rise <= -0.42 * 150 && p.Rise > -0.64 * 150, "튕김 = −(.42~.64)·h");
            }
        }

        [Test]
        public void 착지_반경은_기울인_사각형의_AABB_반폭이고_착지_x_는_반경_안으로_클램프()
        {
            var s = S();
            var g = Left();
            var p = EquipSwapRules.Plan(s, g, CssPx, Lo);
            double th = 8 * Math.PI / 180, sk = 0.55;
            double reach = (150 * sk * 1.12 * Math.Cos(th) + 150 * sk * 1.05 * Math.Sin(th)) / 2 + 2 * CssPx;
            Assert.AreEqual(reach, p.Reach, 1e-9);
            // cx 120 − 150·1.5 = −105 → 왼쪽 벽: reach 로 클램프
            Assert.AreEqual(reach, p.LandX, 1e-9, "가장자리 칸은 벽이 가까워 그만큼 짧게");
            Assert.AreEqual(reach - 120, p.Dx, 1e-9);
            Assert.IsTrue(p.Lands);
            // 낙하 = max(h·1.1, floor − cy − h·sk·(cos+sin)/2)
            double drop = Math.Max(150 * 1.1, 1560 - 1300 - (150 * sk * (Math.Cos(th) + Math.Sin(th))) / 2);
            Assert.AreEqual(drop, p.Drop, 1e-9);
            // 가운데 칸은 클램프 없이 dir·w·1.5
            var m = Left(); m.Cx = 540;
            var pm = EquipSwapRules.Plan(s, m, CssPx, Lo);
            Assert.AreEqual(1, pm.Dir, "정확히 가운데(cx == hostW/2)는 오른쪽");
            Assert.AreEqual(540 + 150 * 1.5, pm.LandX, 1e-9);
            // 바닥이 너무 가까우면 낙하 하한 h·1.1
            var low = Left(); low.FloorY = low.Cy + 10;
            Assert.AreEqual(150 * 1.1, EquipSwapRules.Plan(s, low, CssPx, Lo).Drop, 1e-9);
        }

        [Test]
        public void 팝업_카드가_착지_자리를_덮으면_날아가던_쪽_빈_띠로_옮기고_띠가_없으면_화면_밖으로()
        {
            var s = S();
            // 가운데 칸 · 오른쪽으로 · 카드가 x 300~900 을 덮는다 → 오른쪽 띠 [900+reach, 1080−reach]
            var g = Left(); g.Cx = 540; g.HasBlock = true; g.BlockL = 300; g.BlockR = 900;
            var p = EquipSwapRules.Plan(s, g, CssPx, Lo);
            Assert.IsTrue(p.Lands);
            Assert.AreEqual(900 + p.Reach, p.LandX, 1e-9, "날아가던 쪽(오른쪽) 띠의 안쪽 끝");
            // 오른쪽 띠가 없으면 반대쪽(왼쪽) 띠
            g.BlockR = 1080; p = EquipSwapRules.Plan(s, g, CssPx, Lo);
            Assert.IsTrue(p.Lands);
            Assert.AreEqual(300 - p.Reach, p.LandX, 1e-9, "반대쪽 띠의 바깥 끝(클램프 hi)");
            // 카드가 좌우를 다 먹으면 — 눕히지 않고 화면 밖으로
            g.BlockL = 0; g.BlockR = 1080; p = EquipSwapRules.Plan(s, g, CssPx, Lo);
            Assert.IsFalse(p.Lands, "좁은 화면: 비켜설 자리가 없다");
            Assert.AreEqual(1920 - 540 * 0 - g.Cy + 150, p.Drop, 1e-9, "drop = hostH − cy + h");
            // 카드가 착지 자리와 안 겹치면 그대로
            var far = Left(); far.HasBlock = true; far.BlockL = 800; far.BlockR = 1000;
            var pf = EquipSwapRules.Plan(s, far, CssPx, Lo);
            Assert.AreEqual(pf.Reach, pf.LandX, 1e-9); Assert.IsTrue(pf.Lands);
        }

        [Test]
        public void 키프레임_x_r_은_58에서_닿고_멈추며_y_는_착지_큰튐_작은튐_뒤_누운_채_사라진다()
        {
            var s = S();
            var p = EquipSwapRules.Plan(s, Right(), CssPx, Hi);
            Assert.AreEqual(0, EquipSwapRules.X(s, p, 0), 1e-9);
            Assert.AreEqual(p.Dx, EquipSwapRules.X(s, p, 58), 1e-9);
            Assert.AreEqual(p.Dx, EquipSwapRules.X(s, p, 100), 1e-9, "착지 뒤 옆으로 안 미끄러진다");
            Assert.AreEqual(p.Spin, EquipSwapRules.R(s, p, 58), 1e-9);
            Assert.AreEqual(p.Spin, EquipSwapRules.R(s, p, 80), 1e-9, "착지 뒤 안 돈다");
            double y, a;
            EquipSwapRules.Y(s, p, Rem, 0, out y, out a); Assert.AreEqual(0, y, 1e-9); Assert.AreEqual(1, a, 1e-9);
            EquipSwapRules.Y(s, p, Rem, 26, out y, out a); Assert.AreEqual(p.Rise, y, 1e-9, "26% 튕겨 오름 정점");
            EquipSwapRules.Y(s, p, Rem, 58, out y, out a); Assert.AreEqual(p.Drop, y, 1e-9, "58% 착지");
            EquipSwapRules.Y(s, p, Rem, 68, out y, out a); Assert.AreEqual(p.Drop - 0.8 * Rem, y, 1e-9, "68% 큰 튐 .8rem");
            EquipSwapRules.Y(s, p, Rem, 82, out y, out a); Assert.AreEqual(p.Drop - 0.26 * Rem, y, 1e-9, "82% 작은 튐 .26rem");
            EquipSwapRules.Y(s, p, Rem, 87, out y, out a); Assert.AreEqual(p.Drop, y, 1e-9); Assert.AreEqual(1, a, 1e-9, "87% 까지 불투명 1");
            EquipSwapRules.Y(s, p, Rem, 100, out y, out a); Assert.AreEqual(p.Drop, y, 1e-9); Assert.AreEqual(0, a, 1e-9, "누운 채 사라진다");
            EquipSwapRules.Y(s, p, Rem, 40, out y, out a);
            Assert.IsTrue(y > p.Rise && y < p.Drop, "낙하 중간은 정점과 바닥 사이");
        }

        [Test]
        public void 스쿼시_먼지_딸깍_키프레임()
        {
            var s = S();
            double sx, sy;
            EquipSwapRules.Squash(s, 0, out sx, out sy); Assert.AreEqual(1, sx, 1e-9); Assert.AreEqual(1, sy, 1e-9);
            EquipSwapRules.Squash(s, 54, out sx, out sy); Assert.AreEqual(0.55, sx, 1e-9); Assert.AreEqual(0.55, sy, 1e-9, "줄어듦 sk");
            EquipSwapRules.Squash(s, 60, out sx, out sy); Assert.AreEqual(0.55 * 1.12, sx, 1e-9); Assert.AreEqual(0.55 * 0.84, sy, 1e-9, "착지 스쿼시");
            EquipSwapRules.Squash(s, 68, out sx, out sy); Assert.AreEqual(0.55 * 0.96, sx, 1e-9); Assert.AreEqual(0.55 * 1.05, sy, 1e-9);
            EquipSwapRules.Squash(s, 100, out sx, out sy); Assert.AreEqual(0.55, sx, 1e-9); Assert.AreEqual(0.55, sy, 1e-9);
            double a, ty;
            EquipSwapRules.Dust(s, 0, out a, out sx, out sy, out ty); Assert.AreEqual(0, a, 1e-9); Assert.AreEqual(0.28, sx, 1e-9); Assert.AreEqual(-50, ty, 1e-9);
            EquipSwapRules.Dust(s, 18, out a, out sx, out sy, out ty); Assert.AreEqual(0.95, a, 1e-9); Assert.AreEqual(0.72, sx, 1e-9); Assert.AreEqual(0.86, sy, 1e-9);
            EquipSwapRules.Dust(s, 100, out a, out sx, out sy, out ty); Assert.AreEqual(0, a, 1e-9); Assert.AreEqual(1.5, sx, 1e-9); Assert.AreEqual(-62, ty, 1e-9, "옆으로 퍼지며 살짝 뜬다");
            double sc, br;
            EquipSwapRules.Snap(s, 0, out ty, out sc, out a, out br); Assert.AreEqual(-42, ty, 1e-9); Assert.AreEqual(0.58, sc, 1e-9); Assert.AreEqual(0.25, a, 1e-9);
            EquipSwapRules.Snap(s, 46, out ty, out sc, out a, out br); Assert.AreEqual(0, ty, 1e-9); Assert.AreEqual(1.16, sc, 1e-9, "오버슈트"); Assert.AreEqual(1, a, 1e-9); Assert.AreEqual(1.75, br, 1e-9);
            EquipSwapRules.Snap(s, 72, out ty, out sc, out a, out br); Assert.AreEqual(0.955, sc, 1e-9);
            EquipSwapRules.Snap(s, 88, out ty, out sc, out a, out br); Assert.AreEqual(1.028, sc, 1e-9, "걸쇠 잔떨림");
            EquipSwapRules.Snap(s, 100, out ty, out sc, out a, out br); Assert.AreEqual(1, sc, 1e-9); Assert.AreEqual(1, br, 1e-9);
            // 먼지 자리·크기
            var g = Left(); var p = EquipSwapRules.Plan(s, g, CssPx, Lo);
            Assert.AreEqual(g.Cx + p.Dx, EquipSwapRules.DustX(g, p), 1e-9);
            Assert.AreEqual(g.Cy + p.Drop + 150 * 0.55 * 0.46, EquipSwapRules.DustY(s, g, p), 1e-9);
            Assert.AreEqual(150 * 0.55 * 1.6, EquipSwapRules.DustW(s, g), 1e-9);
            Assert.AreEqual(150 * 0.55 * 1.6 * 0.34, EquipSwapRules.DustH(s, g), 1e-9);
        }
    }
}
