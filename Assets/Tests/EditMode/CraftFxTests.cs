using NUnit.Framework;
using Forge.Core.CraftFx;

namespace Forge.Tests
{
    /// <summary>
    /// T87 1회차 — 대장간 «두들기기» 키프레임이 정본 `style.css`(1181~1320행) 그대로인가, 그리고 정본 주석이 못 박은 계약
    /// (맞물림·비가역 단조·식어 가는 백열·흔들림 상한)을 표가 실제로 지키는가. 브라우저 없이 자로 잰다.
    /// </summary>
    public class CraftFxTests
    {
        private const double Eps = 1e-9;

        [Test]
        public void 클럭과_타격_시각은_정본_그대로다()
        {
            Assert.AreEqual(1500, AnvilFxSpec.DurationMs, Eps, "--afdur 1500ms");
            CollectionAssert.AreEqual(new[] { 300.0, 650.0, 1100.0 }, AnvilFxSpec.StrikeMs, "타격 300/650/1100ms");
            CollectionAssert.AreEqual(new[] { 33.0, 40.0, 50.0 }, AnvilFxSpec.DwellMs, "드웰 33/40/50ms");
            // CSS 는 퍼센트를 셋째 자리에서 반올림해 적는다 — 표는 «적힌 대로»(StrikeStop) 이고 시각 계산과는 그 반올림만큼만 어긋나야 한다.
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(AnvilFxSpec.StrikeStop[i], AnvilFxSpec.StrikePercent(i), 1e-3, i + "타 퍼센트");
                Assert.AreEqual(AnvilFxSpec.DwellStop[i], AnvilFxSpec.DwellEndPercent(i), 1e-3, i + "타 드웰 끝");
            }
        }

        [Test]
        public void 트랙은_키에서_그_값이고_사이는_선형이다()
        {
            double[] v = new double[3];
            AnvilFxSpec.Bump.Sample(20.0, v);
            Assert.AreEqual(0.5, v[0], Eps, "1타 translateY");
            Assert.AreEqual(1.006, v[1], Eps);
            Assert.AreEqual(0.994, v[2], Eps);

            // 18.667%(0) ↔ 20.0%(0.5) 한가운데 = 0.25
            AnvilFxSpec.Bump.Sample((18.667 + 20.0) * 0.5, v);
            Assert.AreEqual(0.25, v[0], 1e-6, "선형 보간(linear 타이밍)");

            // 클럭으로 읽어도 같다 — 300ms = 20%
            double[] w = new double[3];
            AnvilFxSpec.Bump.SampleMs(300, AnvilFxSpec.DurationMs, w);
            Assert.AreEqual(0.5, w[0], 1e-9);

            // 범위 밖은 양 끝으로 자른다(`forwards`)
            AnvilFxSpec.Bump.Sample(-10, v);
            Assert.AreEqual(0, v[0], Eps);
            AnvilFxSpec.Bump.Sample(140, v);
            Assert.AreEqual(0, v[0], Eps);
        }

        [Test]
        public void 모루_빌릿_시트가_같은_타격_드웰_퍼센트에서_맞물린다()
        {
            double[] bump = new double[3], billet = new double[2], shake = new double[2];
            for (int i = 0; i < 3; i++)
            {
                double hit = AnvilFxSpec.StrikeStop[i];
                double dwell = AnvilFxSpec.DwellStop[i];

                AnvilFxSpec.Bump.Sample(hit, bump);
                double[] bumpDwell = new double[3];
                AnvilFxSpec.Bump.Sample(dwell, bumpDwell);
                Assert.AreEqual(bump[0], bumpDwell[0], Eps, i + "타: 모루는 드웰 내내 눌린 자세를 붙잡는다");

                AnvilFxSpec.Billet.Sample(hit, billet);
                double[] billetDwell = new double[2];
                AnvilFxSpec.Billet.Sample(dwell, billetDwell);
                Assert.AreEqual(billet[1], billetDwell[1], Eps, i + "타: 빌릿도 드웰 동안 같이 멈춘다");

                AnvilFxSpec.SheetShake.Sample(hit, shake);
                Assert.Greater(shake[1], 0, i + "타: 시트는 타격 순간 아래로 꽂힌다");
            }
        }

        [Test]
        public void 단조는_비가역이다_빌릿은_되펴지지_않는다()
        {
            // 타격 순간 스냅은 갈수록 깊다
            double[] v = new double[2];
            double[] snap = new double[3];
            for (int i = 0; i < 3; i++)
            {
                AnvilFxSpec.Billet.Sample(AnvilFxSpec.StrikeStop[i], v);
                snap[i] = v[1];
            }
            Assert.Less(snap[1], snap[0], "2타가 1타보다 깊다");
            Assert.Less(snap[2], snap[1], "3타가 2타보다 깊다");
            Assert.AreEqual(0.82, snap[0], Eps);
            Assert.AreEqual(0.66, snap[1], Eps);
            Assert.AreEqual(0.44, snap[2], Eps);

            // 타격 사이 되돌림은 «최소»(0.02 이하)이고, 쉬는 높이는 매번 더 낮다
            double rest0 = Rest(26.0), rest1 = Rest(50.0), rest2 = Rest(100.0);
            Assert.LessOrEqual(rest0 - snap[0], 0.02 + Eps, "1타 뒤 되돌림 최소");
            Assert.LessOrEqual(rest1 - snap[1], 0.02 + Eps, "2타 뒤 되돌림 최소");
            Assert.LessOrEqual(rest2 - snap[2], 0.02 + Eps, "3타 뒤 되돌림 최소");
            Assert.Less(rest1, rest0, "타격 사이 쉬는 높이가 낮아진다");
            Assert.Less(rest2, rest1, "완성품은 납작한 채 남는다");
            Assert.AreEqual(0.46, rest2, Eps, "끝값 .46 — forwards 로 붙잡는다");

            // 부피는 눌린 만큼 옆으로 — scaleX 는 반대로 커진다
            double[] end = new double[2];
            AnvilFxSpec.Billet.Sample(100, end);
            Assert.AreEqual(1.52, end[0], Eps);
        }

        [Test]
        public void 백열은_타격마다_타고_전체로는_식는다()
        {
            double p0 = AnvilFxSpec.BilletHot.Sample1(AnvilFxSpec.StrikeStop[0]);
            double p1 = AnvilFxSpec.BilletHot.Sample1(AnvilFxSpec.StrikeStop[1]);
            double p2 = AnvilFxSpec.BilletHot.Sample1(AnvilFxSpec.StrikeStop[2]);
            Assert.AreEqual(0.9, p0, Eps);
            Assert.AreEqual(0.92, p1, Eps);
            Assert.AreEqual(1.0, p2, Eps);
            Assert.Less(p0, p1, "피크가 갈수록 세다");
            Assert.Less(p1, p2);

            double end = AnvilFxSpec.BilletHot.Sample1(100);
            Assert.AreEqual(0.03, end, Eps, "끝은 거의 꺼진다");
            for (int i = 0; i < AnvilFxSpec.BilletHot.Count; i++)
            {
                Assert.GreaterOrEqual(AnvilFxSpec.BilletHot.ValueAt(i)[0], end - Eps, "끝값이 트랙 최소다(식어 간다)");
            }

            // 식은 쇠색은 거꾸로 — 끝에서 가장 진하다(완성품)
            double c0 = AnvilFxSpec.BilletCool.Sample1(0), cEnd = AnvilFxSpec.BilletCool.Sample1(100);
            Assert.AreEqual(0, c0, Eps);
            Assert.AreEqual(0.82, cEnd, Eps);
            for (int i = 0; i < AnvilFxSpec.BilletCool.Count; i++)
            {
                Assert.LessOrEqual(AnvilFxSpec.BilletCool.ValueAt(i)[0], cEnd + Eps, "끝값이 트랙 최대다");
            }
        }

        [Test]
        public void 시트_흔들림은_갈수록_깊고_정본_상한_안이다()
        {
            double[] v = new double[2];
            double[] depth = new double[3];
            for (int i = 0; i < 3; i++)
            {
                AnvilFxSpec.SheetShake.Sample(AnvilFxSpec.StrikeStop[i], v);
                depth[i] = v[1];
            }
            Assert.AreEqual(2.1, depth[0], Eps);
            Assert.AreEqual(2.9, depth[1], Eps);
            Assert.AreEqual(4.0, depth[2], Eps);
            Assert.Less(depth[0], depth[1]);
            Assert.Less(depth[1], depth[2]);

            for (int i = 0; i < AnvilFxSpec.SheetShake.Count; i++)
            {
                double[] k = AnvilFxSpec.SheetShake.ValueAt(i);
                Assert.LessOrEqual(System.Math.Abs(k[0]), AnvilFxSpec.ShakeMaxPx, "probe-anvil-shake 상한(x)");
                Assert.LessOrEqual(System.Math.Abs(k[1]), AnvilFxSpec.ShakeMaxPx, "probe-anvil-shake 상한(y)");
            }

            // 연출이 끝나면 시트는 제자리다(`sheetshake` 는 forwards 가 아니다)
            AnvilFxSpec.SheetShake.Sample(100, v);
            Assert.AreEqual(0, v[0], Eps);
            Assert.AreEqual(0, v[1], Eps);
        }

        [Test]
        public void 모루는_연출_끝에_제자리로_돌아온다()
        {
            double[] v = new double[3];
            AnvilFxSpec.Bump.Sample(100, v);
            Assert.AreEqual(0, v[0], Eps);
            Assert.AreEqual(1, v[1], Eps);
            Assert.AreEqual(1, v[2], Eps);

            // 잔진동은 갈수록 잦아든다(82.667 → 87.333 → 92.667)
            double[] a = new double[3], b = new double[3], c = new double[3];
            AnvilFxSpec.Bump.Sample(82.667, a);
            AnvilFxSpec.Bump.Sample(87.333, b);
            AnvilFxSpec.Bump.Sample(92.667, c);
            Assert.Greater(System.Math.Abs(a[0]), System.Math.Abs(b[0]), "잔진동 1 > 2");
            Assert.Greater(System.Math.Abs(b[0]), System.Math.Abs(c[0]), "잔진동 2 > 3");
        }

        [Test]
        public void 트랙은_깨진_표를_거부한다()
        {
            Assert.Throws<System.ArgumentException>(() => new CssTrack(new double[] { 0, 50, 40 }, new double[][] { new double[] { 0 }, new double[] { 1 }, new double[] { 2 } }), "퍼센트 역순");
            Assert.Throws<System.ArgumentException>(() => new CssTrack(new double[] { 0, 100 }, new double[][] { new double[] { 0 } }), "키 개수 불일치");
            Assert.Throws<System.ArgumentException>(() => new CssTrack(new double[] { 0, 100 }, new double[][] { new double[] { 0 }, new double[] { 1, 2 } }), "채널 수 불일치");
        }

        private static double Rest(double percent)
        {
            double[] v = new double[2];
            AnvilFxSpec.Billet.Sample(percent, v);
            return v[1];
        }
    }
}
