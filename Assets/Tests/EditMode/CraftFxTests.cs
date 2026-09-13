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

        [Test]
        public void 망치는_모루_빌릿과_같은_클럭_같은_타격_퍼센트다()
        {
            CollectionAssert.AreEqual(AnvilFxSpec.StrikeMs, AutoForgeFxSpec.HitMs, "ui.js ANVIL_HITS = css afswing 타격 시각");
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(AnvilFxSpec.StrikeStop[i], AutoForgeFxSpec.ContactStop[i], Eps, i + "타 접촉 퍼센트가 모루와 같다");
                Assert.AreEqual(AnvilFxSpec.DwellStop[i], AutoForgeFxSpec.DwellStop[i], Eps, i + "타 드웰 끝이 모루와 같다");
                Assert.Less(AutoForgeFxSpec.SmearStop[i], AutoForgeFxSpec.ContactStop[i], i + "타 스미어는 접촉 직전이다");
            }
        }

        [Test]
        public void 망치는_타격마다_다른_자리를_친다()
        {
            // 정본: «대장장이는 소재를 옮긴다 — 같은 자리를 세 번 찍으면 프레임이 복사본으로 보인다»
            double[] v = new double[4];
            double[] hitX = new double[3];
            for (int i = 0; i < 3; i++)
            {
                AutoForgeFxSpec.SampleSwing(AutoForgeFxSpec.HitMs[i], v);
                hitX[i] = v[0];
                // 기본 트랙 + `--dxN` 몫이 CSS 의 calc 과 같아야 한다(접촉 키를 퍼센트로 찾는다)
                double baseX = KeyAt(AutoForgeFxSpec.Swing, AutoForgeFxSpec.ContactStop[i])[0];
                Assert.AreEqual(baseX + AutoForgeFxSpec.HitDx[i], hitX[i], 1e-6, i + "타 접촉 x = 적힌 값 + dx");
            }
            Assert.Less(hitX[0], hitX[1], "타격 자리가 오른쪽으로 걸어간다");
            Assert.Less(hitX[1], hitX[2]);
            Assert.Greater(hitX[2] - hitX[0], 9.0, "세 자리가 실제로 벌어져 있다(정본 −4.8 → 5.4)");
        }

        [Test]
        public void 망치는_접촉_직전에_늘어나고_드웰_동안_멈춘다()
        {
            double[] smear = new double[4], hit = new double[4], dwell = new double[4];
            double[] smearY = { 1.16, 1.18, 1.22 };
            for (int i = 0; i < 3; i++)
            {
                AutoForgeFxSpec.Swing.Sample(AutoForgeFxSpec.SmearStop[i], smear);
                AutoForgeFxSpec.Swing.Sample(AutoForgeFxSpec.ContactStop[i], hit);
                AutoForgeFxSpec.Swing.Sample(AutoForgeFxSpec.DwellStop[i], dwell);
                Assert.AreEqual(smearY[i], smear[3], Eps, i + "타 스미어 배율");
                Assert.AreEqual(1.0, hit[3], Eps, i + "타 접촉에서는 안 늘어난다");
                Assert.AreEqual(hit[2], dwell[2], Eps, i + "타: 드웰 동안 각도가 멈춘다");
                Assert.Greater(hit[1], 0, i + "타: 접촉은 모루 쪽(+y)이다");
            }
            Assert.Less(smearY[0], smearY[1], "스미어가 갈수록 길다");
            Assert.Less(smearY[1], smearY[2]);

            // 관통 깊이(접촉 y)도 갈수록 깊다 — 3타가 가장 깊다
            double[] a = new double[4], b = new double[4], c = new double[4];
            AutoForgeFxSpec.Swing.Sample(AutoForgeFxSpec.ContactStop[0], a);
            AutoForgeFxSpec.Swing.Sample(AutoForgeFxSpec.ContactStop[1], b);
            AutoForgeFxSpec.Swing.Sample(AutoForgeFxSpec.ContactStop[2], c);
            Assert.Less(a[1], b[1], "2타가 더 깊다");
            Assert.Less(b[1], c[1], "3타가 가장 깊다");
        }

        [Test]
        public void 블룸은_접촉_프레임을_들어_올린다()
        {
            // 정본: «이게 없으면 접촉 프레임이 정지 프레임보다 어둡다 — 회색 머리가 주황 상판과 크림 배경을 덮기 때문»
            double[] v = new double[2];
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(AutoForgeFxSpec.SampleBloom(i, AutoForgeFxSpec.HitMs[i], v), i + "타 블룸이 접촉 프레임에 켜져 있다");
                Assert.Greater(v[1], 0.3, i + "타: 접촉 프레임 밝기(배율과 곱해진다)");
                Assert.Greater(v[0], AutoForgeFxSpec.BloomScale[i] * 0.82, i + "타: 접촉 프레임에 이미 0.82배 이상");
                // 밝기도 타격별 배율과 곱해진다 — 3타가 1타보다 밝다
                Assert.IsFalse(AutoForgeFxSpec.SampleBloom(i, AutoForgeFxSpec.HitMs[i] - AutoForgeFxSpec.BloomLeadMs - 1, v), i + "타: 창 앞에는 없다");
            }
            double[] a0 = new double[2], a2 = new double[2];
            AutoForgeFxSpec.SampleBloom(0, AutoForgeFxSpec.HitMs[0], a0);
            AutoForgeFxSpec.SampleBloom(2, AutoForgeFxSpec.HitMs[2], a2);
            Assert.Greater(a2[1], a0[1], "3타 블룸이 1타보다 밝다(밝기도 --afbs 와 곱해진다)");
            Assert.Greater(a2[0], a0[0], "3타 블룸이 1타보다 넓다");
            // 수명이 짧다 — «2~3프레임» (120ms)
            Assert.LessOrEqual(AutoForgeFxSpec.BloomDurMs, 150.0, "블룸은 두세 프레임짜리다");
        }

        [Test]
        public void 잔열은_타격_사이에_다리를_놓고_오버레이_안에서_끝난다()
        {
            // 정본: 플래시(100ms)·섬광(75ms)·링(200ms)이 250ms 안에 다 사라져 «타격 사이 상판이 완전히 식은 그림» 이었다 → 잔열이 다리를 놓는다
            double[] v = new double[2];
            for (int i = 0; i < 2; i++)
            {
                // 짧은 겹이 전부 꺼진 뒤(섬광 75 · 플래시 100 · 링 200ms) — 다음 타격까지 남은 구간이 «식은 그림» 이 되면 안 된다
                double mid = AutoForgeFxSpec.HitMs[i] + 220;
                Assert.Less(mid, AutoForgeFxSpec.HitMs[i + 1], i + "타: 그 시각이 다음 타격 전이다");
                Assert.IsFalse(AutoForgeFxSpec.SampleBurst(AutoForgeFxSpec.Flash, CssEase.Linear, AutoForgeFxSpec.FlashDurMs, AutoForgeFxSpec.FlashLeadMs, AutoForgeFxSpec.FlashScale, i, mid, v), i + "타 플래시는 그때 이미 없다");
                Assert.IsFalse(AutoForgeFxSpec.SampleRing(i, mid, v), i + "타 링도 없다");
                Assert.IsTrue(AutoForgeFxSpec.SampleBurst(AutoForgeFxSpec.Heat, CssEase.Linear, AutoForgeFxSpec.HeatDurMs, AutoForgeFxSpec.HeatLeadMs, AutoForgeFxSpec.HeatScale, i, mid, v), i + "타 잔열은 그때도 살아 있다(다리)");
                Assert.Greater(v[1], 0.05, i + "타: 그 구간에도 밝기가 남는다");
            }
            // 3타 잔열은 오버레이 수명(1500ms) 안에서 끝난다 — 넘기면 잘려 나간다(정본 probe ⑥)
            double end = AutoForgeFxSpec.HitMs[2] - AutoForgeFxSpec.HeatLeadMs + AutoForgeFxSpec.HeatDurMs;
            Assert.LessOrEqual(end, AnvilFxSpec.DurationMs, "3타 잔열이 오버레이 안에서 끝난다");
            Assert.IsFalse(AutoForgeFxSpec.SampleBurst(AutoForgeFxSpec.Heat, CssEase.Linear, AutoForgeFxSpec.HeatDurMs, AutoForgeFxSpec.HeatLeadMs, AutoForgeFxSpec.HeatScale, 2, end + 1, v), "창이 끝나면 없다");

            // 플래시는 접촉 프레임에 이미 거의 최대(정본이 «.55 → .86 출발» 로 고친 자리)
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(AutoForgeFxSpec.SampleBurst(AutoForgeFxSpec.Flash, CssEase.Linear, AutoForgeFxSpec.FlashDurMs, AutoForgeFxSpec.FlashLeadMs, AutoForgeFxSpec.FlashScale, i, AutoForgeFxSpec.HitMs[i], v), i + "타 플래시");
                Assert.Greater(v[0], AutoForgeFxSpec.FlashScale[i] * 0.86, i + "타: 접촉에 이미 0.86배 이상");
                Assert.AreEqual(1.0, v[1], 1e-6, i + "타: 접촉에 완전 불투명");
                Assert.Greater(AutoForgeFxSpec.FlashScale[i], i == 0 ? 1.0 : AutoForgeFxSpec.FlashScale[i - 1], i + "타 플래시가 갈수록 크다");
            }
        }

        [Test]
        public void 타격_겹_셋은_접촉_프레임에_이미_거의_최대다()
        {
            // 정본이 세 번 적은 교훈: 작게 출발해 뒤에 커지면 «소리는 제때 나는데 빛만 메아리로 온다»
            double[] v = new double[2];
            for (int i = 0; i < 3; i++)
            {
                // ⓐ 접지 그림자 — 접촉 23ms 앞에 켜져 접촉 프레임(36%)에 가장 진하다
                Assert.IsTrue(AutoForgeFxSpec.SampleBurst(AutoForgeFxSpec.Shadow, AutoForgeFxSpec.ShadowEase, AutoForgeFxSpec.ShadowDurMs, AutoForgeFxSpec.ShadowLeadMs, AutoForgeFxSpec.ShadowScale, i, AutoForgeFxSpec.HitMs[i], v), i + "타 그림자");
                Assert.Greater(v[1], 0.3, i + "타: 접촉 프레임에 그림자가 이미 짙다(작게 출발해 뒤에 커지면 늦는다)");
                Assert.Greater(v[0], AutoForgeFxSpec.ShadowScale[i] * 0.9, i + "타: 접촉 프레임 그림자 배율도 거의 다 컸다");
                // 가장 진한 자리는 36% — 드웰(33~50ms) 한복판이라 «닿아 있는 동안» 이다
                double[] peak = new double[2];
                AutoForgeFxSpec.SampleBurst(AutoForgeFxSpec.Shadow, AutoForgeFxSpec.ShadowEase, AutoForgeFxSpec.ShadowDurMs, AutoForgeFxSpec.ShadowLeadMs, AutoForgeFxSpec.ShadowScale, i, AutoForgeFxSpec.HitMs[i] - AutoForgeFxSpec.ShadowLeadMs + AutoForgeFxSpec.ShadowDurMs * 0.36, peak);
                Assert.AreEqual(0.42, peak[1], 1e-6, i + "타: 36% 가 가장 진하다");
                Assert.AreEqual(AutoForgeFxSpec.ShadowScale[i], peak[0], 1e-6, i + "타: 36% 배율 = 제 배율");
                Assert.Less(AutoForgeFxSpec.ShadowDurMs * 0.36 - AutoForgeFxSpec.ShadowLeadMs, AnvilFxSpec.DwellMs[i] + 10.0, i + "타: 그 자리가 드웰 안(닿아 있는 동안)이다");
                Assert.IsFalse(AutoForgeFxSpec.SampleBurst(AutoForgeFxSpec.Shadow, AutoForgeFxSpec.ShadowEase, AutoForgeFxSpec.ShadowDurMs, AutoForgeFxSpec.ShadowLeadMs, AutoForgeFxSpec.ShadowScale, i, AutoForgeFxSpec.HitMs[i] - AutoForgeFxSpec.ShadowLeadMs - 1, v), i + "타: 창 앞");

                // ⓑ 섬광 · ⓒ 코어 — 접촉 7ms 앞에 켜지고 그 순간 이미 최대에 가깝다
                Assert.IsTrue(AutoForgeFxSpec.SampleBurst(AutoForgeFxSpec.Star, AutoForgeFxSpec.StarEase, AutoForgeFxSpec.StarDurMs, AutoForgeFxSpec.StarLeadMs, AutoForgeFxSpec.StarScale, i, AutoForgeFxSpec.HitMs[i], v), i + "타 섬광");
                Assert.Greater(v[0], AutoForgeFxSpec.StarScale[i] * 0.9, i + "타: 접촉 프레임 섬광이 이미 0.9배 이상");
                Assert.Greater(v[1], 0.9, i + "타: 접촉 프레임 섬광이 밝다");
                Assert.IsTrue(AutoForgeFxSpec.SampleBurst(AutoForgeFxSpec.Core, AutoForgeFxSpec.CoreEase, AutoForgeFxSpec.CoreDurMs, AutoForgeFxSpec.StarLeadMs, AutoForgeFxSpec.CoreScale, i, AutoForgeFxSpec.HitMs[i], v), i + "타 코어");
                Assert.Greater(v[0], AutoForgeFxSpec.CoreScale[i] * 0.88, i + "타: 접촉 프레임 코어가 이미 0.88배 이상");
                Assert.AreEqual(1.0, v[1], 1e-6, i + "타: 코어는 절반까지 완전 불투명");

                // 셋 다 창이 끝나면 꺼지고, 수명이 오버레이 안이다
                Assert.IsFalse(AutoForgeFxSpec.SampleBurst(AutoForgeFxSpec.Star, AutoForgeFxSpec.StarEase, AutoForgeFxSpec.StarDurMs, AutoForgeFxSpec.StarLeadMs, AutoForgeFxSpec.StarScale, i, AutoForgeFxSpec.HitMs[i] + AutoForgeFxSpec.StarDurMs, v), i + "타: 섬광 창 끝");
                Assert.LessOrEqual(AutoForgeFxSpec.HitMs[i] + AutoForgeFxSpec.ShadowDurMs, AnvilFxSpec.DurationMs, i + "타: 그림자 수명이 오버레이 안이다");
            }
            // 위계 — 배율이 타격마다 커진다
            for (int i = 1; i < 3; i++)
            {
                Assert.Greater(AutoForgeFxSpec.ShadowScale[i], AutoForgeFxSpec.ShadowScale[i - 1], "그림자가 갈수록 넓다");
                Assert.Greater(AutoForgeFxSpec.StarScale[i], AutoForgeFxSpec.StarScale[i - 1], "섬광이 갈수록 크다");
                Assert.Greater(AutoForgeFxSpec.CoreScale[i], AutoForgeFxSpec.CoreScale[i - 1], "코어가 갈수록 크다");
            }
        }

        [Test]
        public void 링은_망치가_실제로_닿는_자리에_선다()
        {
            // 정본 hx(h)=cx+SINK_X+dx · hy(h)=cy+SINK_Y — 타격 자리가 오른쪽으로 걸어가고 접점은 갈수록 깊이 눌린다
            double[] cx = new double[3], cy = new double[3];
            for (int i = 0; i < 3; i++)
            {
                cx[i] = AutoForgeFxSpec.HitCenterX(i);
                cy[i] = AutoForgeFxSpec.HitCenterY(i);
                Assert.AreEqual(AutoForgeFxSpec.HitX + AutoForgeFxSpec.SinkX[i] + AutoForgeFxSpec.HitDx[i], cx[i], Eps, i + "타 중심 x");
                Assert.AreEqual(AutoForgeFxSpec.HitY + AutoForgeFxSpec.SinkY[i], cy[i], Eps, i + "타 중심 y");
            }
            Assert.Less(cx[0], cx[1], "자리가 오른쪽으로 걸어간다");
            Assert.Less(cx[1], cx[2]);
            Assert.Less(cy[0], cy[1], "갈수록 깊이 눌린다(+y 는 아래)");
            Assert.Less(cy[1], cy[2]);
            // 접점은 빌릿 윗면(11)보다 아래이고 상판 아래로는 안 내려간다(모루 상판 앞면 y 25)
            for (int i = 0; i < 3; i++)
            {
                Assert.Greater(cy[i], AutoForgeFxSpec.HitY, i + "타: 눌린 자리는 정지 접점보다 아래다");
                Assert.Less(cy[i], 25.0, i + "타: 상판 안이다");
            }
        }

        [Test]
        public void 퇴장은_스윙_끝자세에서_이어받아_사라진다()
        {
            double[] end = new double[4], exit = new double[4];
            AutoForgeFxSpec.Swing.Sample(100, end);
            Assert.IsFalse(AutoForgeFxSpec.SampleExit(AutoForgeFxSpec.ExitStartMs - 1, exit), "1170ms 전에는 퇴장이 없다");
            Assert.IsTrue(AutoForgeFxSpec.SampleExit(AutoForgeFxSpec.ExitStartMs, exit), "1170ms 에 넘겨받는다");
            for (int c = 0; c < 3; c++) Assert.AreEqual(end[c], exit[c], Eps, "경계 자세가 같아야 안 튄다(채널 " + c + ")");
            Assert.AreEqual(1.0, exit[3], Eps, "넘겨받을 때는 보인다");

            AutoForgeFxSpec.SampleExit(AutoForgeFxSpec.ExitStartMs + AutoForgeFxSpec.ExitDurMs, exit);
            Assert.AreEqual(0.0, exit[3], Eps, "끝에서 사라진다");
            Assert.AreEqual(-74.0, exit[1], Eps, "왼쪽 위로 빠진다");
            Assert.LessOrEqual(AutoForgeFxSpec.ExitStartMs + AutoForgeFxSpec.ExitDurMs, AnvilFxSpec.DurationMs, "퇴장이 오버레이 수명 안에서 끝난다");

            // 앞이 빠르다(0→45%가 33% 구간) — 이징까지 옮겼는지 본다
            AutoForgeFxSpec.SampleExit(AutoForgeFxSpec.ExitStartMs + AutoForgeFxSpec.ExitDurMs * 0.5, exit);
            double[] lin = new double[4];
            AutoForgeFxSpec.Exit.Sample(50, lin);
            Assert.Less(exit[1], lin[1], "cubic-bezier(.2,.62,.5,1) 는 앞을 당긴다(같은 시각에 더 올라가 있다)");
        }

        [Test]
        public void 타격_링은_접촉_8ms_앞에_켜져_갈수록_크게_퍼진다()
        {
            double[] v = new double[2];
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(AutoForgeFxSpec.HitMs[i] - 8, AutoForgeFxSpec.RingStartMs(i), Eps, i + "타 링 시작");
                Assert.IsFalse(AutoForgeFxSpec.SampleRing(i, AutoForgeFxSpec.RingStartMs(i) - 1, v), i + "타: 켜지기 전");
                Assert.IsTrue(AutoForgeFxSpec.SampleRing(i, AutoForgeFxSpec.RingStartMs(i), v), i + "타: 시작");
                Assert.AreEqual(1.0, v[0], Eps, i + "타: 배율 1 에서 시작");
                Assert.AreEqual(1.0, v[1], Eps, i + "타: 가장 밝게 시작");
                // 접촉 프레임(8ms 뒤)에도 아직 살아 있어야 «소리와 그림» 이 같이 온다
                Assert.IsTrue(AutoForgeFxSpec.SampleRing(i, AutoForgeFxSpec.HitMs[i], v), i + "타: 접촉 프레임에 링이 있다");
                Assert.Greater(v[1], 0.5, i + "타: 접촉 프레임의 링이 아직 밝다");
                AutoForgeFxSpec.SampleRing(i, AutoForgeFxSpec.RingStartMs(i) + AutoForgeFxSpec.RingDurMs, v);
                Assert.AreEqual(AutoForgeFxSpec.RingScale[i], v[0], 1e-6, i + "타: 끝 배율");
                Assert.AreEqual(0.0, v[1], Eps, i + "타: 끝에서 꺼진다");
            }
            Assert.Less(AutoForgeFxSpec.RingScale[0], AutoForgeFxSpec.RingScale[1], "링이 갈수록 크게 퍼진다");
            Assert.Less(AutoForgeFxSpec.RingScale[1], AutoForgeFxSpec.RingScale[2]);
        }

        [Test]
        public void 이징은_브라우저와_같은_값을_낸다()
        {
            Assert.AreEqual(0.0, CssEase.Linear.Ease(0), Eps);
            Assert.AreEqual(0.5, CssEase.Linear.Ease(0.5), Eps);
            CssEase e = new CssEase(0.2, 0.62, 0.5, 1);
            Assert.AreEqual(0.0, e.Ease(0), Eps);
            Assert.AreEqual(1.0, e.Ease(1), Eps);
            Assert.Greater(e.Ease(0.25), 0.25, "앞을 당기는 곡선이다");
            for (double t = 0.05; t < 1.0; t += 0.05)
            {
                Assert.GreaterOrEqual(e.Ease(t) + 1e-9, e.Ease(t - 0.05), "단조 증가");
            }
            // 대칭 곡선(ease-in-out 꼴)은 한가운데가 0.5 다 — 푸는 방법이 맞는지 보는 자기 검사
            CssEase sym = new CssEase(0.42, 0, 0.58, 1);
            Assert.AreEqual(0.5, sym.Ease(0.5), 1e-6);
            Assert.Throws<System.ArgumentException>(() => new CssEase(1.4, 0, 0.5, 1), "x 는 0~1 이어야 한다");
        }

        /// <summary>퍼센트가 그 키와 같은 줄의 값(트랙에 «적힌 대로» 를 읽을 때).</summary>
        private static double[] KeyAt(CssTrack t, double percent)
        {
            for (int i = 0; i < t.Count; i++)
            {
                if (System.Math.Abs(t.StopAt(i) - percent) < 1e-9) return t.ValueAt(i);
            }
            throw new System.ArgumentException("그 퍼센트의 키가 없다: " + percent);
        }

        /// <summary>
        /// T87 21회차 — 불티(`afspark`). 정본이 이 층에 못 박은 것 넷을 표에서 지킨다:
        /// 위쪽 반구에서도 **손잡이 쪽(우상단)은 빼고** 튄다 · 사거리 하한이 머리 반폭(10.6)보다 크다 · 궤적이 **정확한 포물선**(중간 키 u·0.55 / v·0.30) ·
        /// 3타가 더 많고 더 멀리 가며 마지막 불티도 오버레이 수명(1500ms) 안에서 끝난다.
        /// </summary>
        [Test]
        public void 불티는_손잡이_쪽을_빼고_튀고_궤적이_포물선이다()
        {
            // 난수 자리에 «가운데 값» 을 넣어 같은 묶음을 본다(정본은 Math.random · 클론은 결정론 난수원)
            AutoForgeFxSpec.SparkSpec[] mid = AutoForgeFxSpec.BuildSparks(delegate(double a, double b) { return (a + b) * 0.5; });
            Assert.AreEqual(7 + 11 + 16, mid.Length, "불티 개수 = 7 + 11 + 16(정본 [7,11,16])");

            int[] per = new int[3];
            double[] far = new double[3];
            foreach (AutoForgeFxSpec.SparkSpec q in mid)
            {
                per[q.Strike]++;
                if (q.Dist > far[q.Strike]) far[q.Strike] = q.Dist;
                // 각도: −0.30π ~ −0.95π + 흔들림(−0.95π 를 넘지 않는다) · 손잡이가 뻗은 −20° 쪽은 비운다
                Assert.Less(q.AngleDeg, -53.9, "위쪽 반구 중 손잡이(−20°) 쪽은 비어야 한다 — 그 몫은 망치에 가려진다");
                Assert.Greater(q.AngleDeg, -171.1, "모루 아래로 파고드는 불티는 오독이다");
                Assert.GreaterOrEqual(q.Dist, AutoForgeFxSpec.SparkDistMin, "사거리 하한(머리 반폭 10.6 보다 커야 접촉 프레임에 보인다)");
                Assert.LessOrEqual(q.StartMs + q.DurMs, AnvilFxSpec.DurationMs, "불티가 오버레이 수명 안에서 끝난다");
                // 색온도: 멀리 가는 조각일수록 뜨겁다
                int want = q.Dist > AutoForgeFxSpec.SparkTintHot ? 0 : (q.Dist > AutoForgeFxSpec.SparkTintWarm ? 1 : 2);
                Assert.AreEqual(want, q.Tint, "색온도는 사거리로 갈린다");
            }
            Assert.AreEqual(new int[] { 7, 11, 16 }, per, "타격마다 개수가 는다(위계가 2단이면 크레셴도로 안 읽힌다)");
            Assert.Greater(far[2], far[0] * 1.7, "3타가 1타보다 훨씬 멀리 튄다(배수 1.8)");

            AutoForgeFxSpec.SparkSpec sp = mid[mid.Length - 1];
            double[] v = new double[4];
            // 창 앞·뒤에는 그릴 것이 없다
            Assert.IsFalse(AutoForgeFxSpec.SampleSpark(sp, sp.StartMs - 1, v), "켜지기 전");
            Assert.IsFalse(AutoForgeFxSpec.SampleSpark(sp, sp.StartMs + sp.DurMs + 1, v), "수명 뒤");
            // 접촉 프레임(= 켠 뒤 8ms)에 이미 밝고 늘어나 있다 — 작게 출발해 뒤에 커지면 «빛만 메아리로 온다»
            Assert.IsTrue(AutoForgeFxSpec.SampleSpark(sp, AutoForgeFxSpec.HitMs[sp.Strike], v));
            Assert.Greater(v[0], 0.85, "접촉 프레임에 거의 불투명");
            Assert.Greater(v[1], 0.85, "접촉 프레임에 이미 제 길이에 가깝다(실측 0.879 — 0.7 에서 출발해 이징이 앞에서 쏟아진다)");
            // 중간 키 = u·0.55 / v·0.30 → 등속 + 등가속이라 정확한 포물선이다
            Assert.IsTrue(AutoForgeFxSpec.SampleSpark(sp, sp.StartMs + sp.DurMs * 0.55, v));
            Assert.AreEqual(0.55, v[2], 1e-9, "중간 키의 발사 방향 몫");
            Assert.AreEqual(0.30, v[3], 1e-9, "중간 키의 중력 몫");
            // 끝에는 꺼지고 진행 방향으로만 가늘어진다(잔상)
            AutoForgeFxSpec.SampleSpark(sp, sp.StartMs + sp.DurMs, v);
            Assert.AreEqual(0.0, v[0], 1e-9, "수명 끝에는 투명");
            Assert.AreEqual(0.22, v[1], 1e-9, "끝에서 잔상처럼 가늘어진다");
            Assert.AreEqual(1.0, v[2], 1e-9, "끝에서 사거리를 다 쓴다");
            // 회전 프레임 분해 — u 는 발사 방향(등속) · v 는 중력(등가속)
            double rad = sp.AngleDeg * System.Math.PI / 180.0;
            Assert.AreEqual(sp.Dist + AutoForgeFxSpec.SparkGravity * System.Math.Sin(rad), sp.U, 1e-9, "u = d + g·sin a");
            Assert.AreEqual(AutoForgeFxSpec.SparkGravity * System.Math.Cos(rad), sp.V, 1e-9, "v = g·cos a");
            // 전역 좌표로 되돌리면 정본이 노린 «발사 + 중력» 이 딱 떨어진다(회전 프레임 분해의 효과):
            //   dx = d·cos a · dy = d·sin a + g  — 즉 «각 a 로 d 만큼 날아가 g 만큼 내려온 자리» 다(CSS 는 아래가 +y).
            double dxEnd = System.Math.Cos(rad) * sp.U - System.Math.Sin(rad) * sp.V;
            double dyEnd = System.Math.Sin(rad) * sp.U + System.Math.Cos(rad) * sp.V;
            Assert.AreEqual(sp.Dist * System.Math.Cos(rad), dxEnd, 1e-9, "가로는 발사 각도·사거리 그대로");
            Assert.AreEqual(sp.Dist * System.Math.Sin(rad) + AutoForgeFxSpec.SparkGravity, dyEnd, 1e-9, "세로는 발사 + 중력 g");
            // 중간점이 «직선의 중간» 보다 위다 = 궤적이 위로 부푼 포물선이다(정본: 직선이면 파편이 아니라 레이저다)
            double dyMid = System.Math.Sin(rad) * (0.55 * sp.U) + System.Math.Cos(rad) * (0.30 * sp.V);
            Assert.Less(dyMid, 0.55 * dyEnd - 1e-9, "중간점이 직선보다 위다(포물선)");
            Assert.Less(dyMid, 0.0, "중간점은 타격점보다 위다");
        }

        private static double Rest(double percent)
        {
            double[] v = new double[2];
            AnvilFxSpec.Billet.Sample(percent, v);
            return v[1];
        }
    }
}
