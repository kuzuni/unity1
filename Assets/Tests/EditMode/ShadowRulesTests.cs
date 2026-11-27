using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>T331 2회차 — 정본 `box-shadow` 표가 그대로 서고, CSS(아래가 +y) ↔ UGUI(위가 +y) 부호가 한 자리에서만 뒤집힌다.</summary>
    public class ShadowRulesTests
    {
        static ShadowTable table;
        static ShadowTable T()
        {
            if (table == null)
            {
                string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
                table = ShadowTable.From(MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "Assets", "Forge", "Resources", "ShadowUi.json"))));
            }
            return table;
        }

        [Test]
        public void 표는_정본_자리를_그대로_쥔다()
        {
            // ⚑ 26회차 — 수는 **자란다**(회차마다 «아직 안 본 자리» 에서 하나씩 표로 옮긴다).
            //   그래서 못 박는 것은 «열둘» 이 아니라 ⓐ 자(`check_box_shadows`)의 표와 같은 수 ⓑ 값이 정본 그대로다.
            //   자의 표를 늘리면 이 수도 같이 늘려라 — 둘이 어긋나면 한쪽이 몰래 낡은 것이다.
            ShadowTable t = T();
            Assert.AreEqual(36, t.Count, "자(check_box_shadows)가 세는 자리와 같은 수여야 한다 — 36회차 28 · 38회차 29 · 39회차 30 · 40회차 33 · 41회차 35 · 43회차 소환 [확인] 36");
            ShadowSpec q = t.Get("qstrow_lip");
            Assert.AreEqual(0.0, q.DxRem, 1e-9);
            Assert.AreEqual(0.25, q.DyRem, 1e-9, "정본 .qst-row `0 .25rem 0`");
            Assert.AreEqual(0.3, q.A, 1e-9, "rgba(0,0,0,.3)");
            ShadowSpec e = t.Get("equipped_lip");
            Assert.AreEqual(0.22, e.DyRem, 1e-9);
            Assert.AreEqual(0.35, e.A, 1e-9);
        }

        [Test]
        public void 딱딱한_턱과_흐린_그림자로_갈린다()
        {
            int hard = 0, soft = 0;
            foreach (string k in T().Keys) { if (T().Get(k).IsHard) hard++; else soft++; }
            Assert.AreEqual(8, hard, "딱딱한 턱(blur·spread 0) — 카드·패널·퀘스트 행·던전 배너·장착 바·자동 제련 스피너 + 대장간 나이 막대(29회차) + 자동 제련 시대 막대 턱(40회차)");
            Assert.AreEqual(28, soft, "흐린 그림자(36회차 21 · 38회차 22 · 39회차 23 · 40회차 25 · 41회차 27 · 43회차 srok_drop 28) — 29회차에 남은 열하나의 **값을 먼저 재** 표에 담았다(배선은 그 파일의 lock 이 풀리는 회차 · 자의 KNOWN 이 그 목록이다)");
            Assert.AreEqual(T().Count, hard + soft, "갈래가 둘뿐이다");
        }

        [Test]
        public void 번짐은_음수가_될_수_있고_흐림은_안_된다()
        {
            // 정본 `.pass-card` 는 `-.5rem` 으로 그늘을 **안으로 줄인다** — CSS 가 허락하는 자리다.
            Assert.Less(T().Get("passcard_drop").SpreadRem, 0.0);
            string badBlur = Json("[0,0,0,0.3]", 0.25).Replace("\"blur_rem\":0", "\"blur_rem\":-1");
            Assert.Throws<System.FormatException>(() => ShadowTable.From(MiniJson.ParseObject(badBlur)));
        }

        [Test]
        public void 흐림은_곧은_모서리에서_정규분포의_누적이다()
        {
            // CSS 규격: 흐림 반지름의 **절반**이 표준편차다. 모서리 위(거리 0)는 정확히 절반이 덮인다.
            double sigma = ShadowTable.SigmaOf(10.0);
            Assert.AreEqual(5.0, sigma, 1e-9);
            Assert.AreEqual(0.5, ShadowTable.EdgeCoverage(0, sigma), 1e-6, "모서리 위는 반");
            Assert.Greater(ShadowTable.EdgeCoverage(-sigma, sigma), 0.84, "안쪽 1σ 는 84% 위");
            Assert.Less(ShadowTable.EdgeCoverage(sigma, sigma), 0.16, "바깥 1σ 는 16% 아래");
            Assert.Greater(ShadowTable.EdgeCoverage(-4 * sigma, sigma), 0.999, "깊은 안쪽은 꽉 찬다");
            Assert.Less(ShadowTable.EdgeCoverage(4 * sigma, sigma), 0.001, "먼 바깥은 0");
            // σ 가 0 이면 계단 — 딱딱한 턱이 흐린 길로 와도 같은 그림이 된다.
            Assert.AreEqual(1.0, ShadowTable.EdgeCoverage(-0.5, 0), 1e-9);
            Assert.AreEqual(0.0, ShadowTable.EdgeCoverage(0.5, 0), 1e-9);
        }

        [Test]
        public void 둥근_네모의_거리는_모서리에서_0이고_구석에서_둥글다()
        {
            // 100×60 상자 · 반지름 10
            Assert.AreEqual(0.0, ShadowTable.RoundRectDistance(50, 0, 50, 30, 10), 1e-9, "오른쪽 변 위");
            Assert.AreEqual(-10.0, ShadowTable.RoundRectDistance(40, 0, 50, 30, 10), 1e-9, "안쪽 10px");
            Assert.AreEqual(5.0, ShadowTable.RoundRectDistance(55, 0, 50, 30, 10), 1e-9, "바깥 5px");
            // 구석: 반지름 중심에서 r 만큼 떨어진 곳이 곧 모서리다
            double c = ShadowTable.RoundRectDistance(40 + 10 / System.Math.Sqrt(2), 20 + 10 / System.Math.Sqrt(2), 50, 30, 10);
            Assert.AreEqual(0.0, c, 1e-6, "둥근 구석의 대각선도 모서리 위다");
            Assert.Greater(ShadowTable.RoundRectDistance(50, 30, 50, 30, 10), 0.0, "네모 꼭짓점은 둥근 네모 **바깥**이다");
        }

        [Test]
        public void 패널_턱은_위로_뜬다()
        {
            Assert.Less(T().Get("panel_lip").DyRem, 0.0, "정본 .panel 은 `0 -.4rem 0` — 화면 아래에 붙은 패널이 위쪽으로 그늘을 낸다");
        }

        [Test]
        public void 화면_좌표로_바꿀_때_아래위가_뒤집힌다()
        {
            double x, y;
            T().OffsetPx("qstrow_lip", 40.0, out x, out y);
            Assert.AreEqual(0.0, x, 1e-9);
            Assert.AreEqual(-10.0, y, 1e-9, "CSS 의 아래(+.25rem)는 UGUI 에서 −10px 다");
            T().OffsetPx("panel_lip", 40.0, out x, out y);
            Assert.AreEqual(16.0, y, 1e-9, "위로 뜨는 턱(−.4rem)은 +16px");
        }

        [Test]
        public void 없는_자리를_물으면_거부한다()
        {
            Assert.IsFalse(T().Has("없는자리"));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => T().Get("없는자리"));
        }

        /// <summary>표 한 칸짜리 JSON — 따옴표를 문자로 넣는다(C# 문자열 안에서 읽기 좋게).</summary>
        static string Json(string rgba, double dy)
        {
            char q = '"';
            return "{" + q + "spots" + q + ":{" + q + "x" + q + ":{"
                + q + "dx_rem" + q + ":0," + q + "dy_rem" + q + ":" + dy + ","
                + q + "blur_rem" + q + ":0," + q + "spread_rem" + q + ":0,"
                + q + "rgba" + q + ":" + rgba + "}}}";
        }

        // ───────── T511 2회차 — 타원 번짐 셈(정본 `.sr-floor::before` 의 box-shadow 두 겹) ─────────
        //  이 칸들의 참값은 **짐작이 아니라 같은 식을 1024칸으로 돌려 뽑은 것**이다(세션 쪽지 `twin.py`).
        //  소환진 실치수: 반너비 11rem · 반높이 4.4rem · 흐림 3.6rem ⇒ σ 1.8rem.
        const double A = 11.0, B = 4.4, SIG = 1.8;

        [Test]
        public void 타원_번짐은_곧은_턱에서_정규분포_누적과_같아진다()
        {
            // 반높이를 1만 rem 으로 늘리면 턱이 곧아진다 — 그때 이 셈은 `EdgeCoverage` 와 같아야 한다.
            // 이 줄이 적분·가중·정규화 중 하나만 틀려도 바로 넘어진다(참값이 딴 데서 온다).
            foreach (double d in new[] { -2.0, 0.0, 2.0 })
            {
                double got = ShadowTable.EllipseCoverage(50.0 + d, 0.0, 50.0, 1e4, 2.0);
                double want = ShadowTable.EdgeCoverage(d, 2.0);
                Assert.That(got, Is.EqualTo(want).Within(1e-3), "곧은 턱 d=" + d);
            }
        }

        [Test]
        public void 타원_번짐은_거리_erfc_가_못_내는_값을_낸다_장축_끝()
        {
            // **이 번호가 있는 까닭**: 「부호 거리 → 정규분포 누적」은 곧은 턱에만 정확하다. 타원의 장축 끝은
            // 곡률 반지름이 b²/a = 1.76rem 로 σ 1.8rem 보다 **작아** 빛이 양옆으로 달아난다 — 참값 0.3425 인데
            // 거리 셈은 0.5 를 낸다(0.157 차 = 봉우리의 16%). 단축 끝은 거의 곧아서(a²/b = 27.5rem) 0.010 뿐이다.
            double tip = ShadowTable.EllipseCoverage(A, 0.0, A, B, SIG);
            double side = ShadowTable.EllipseCoverage(0.0, B, A, B, SIG);
            Assert.That(tip, Is.EqualTo(0.342498).Within(3e-3), "장축 끝");
            Assert.That(side, Is.EqualTo(0.489658).Within(3e-3), "단축 끝");
            Assert.That(System.Math.Abs(tip - ShadowTable.EdgeCoverage(0.0, SIG)), Is.GreaterThan(0.1),
                "장축 끝은 거리 셈과 0.1 이상 달라야 한다 — 같아지면 누군가 EdgeCoverage 로 되돌린 것이다");
            Assert.That(System.Math.Abs(side - ShadowTable.EdgeCoverage(0.0, SIG)), Is.LessThan(0.03),
                "단축 끝은 거의 곧아 거리 셈과 비슷하다(곡률이 뿌리라는 증거)");
        }

        [Test]
        public void 타원_번짐은_가운데가_차고_밖으로_단조롭게_준다()
        {
            Assert.That(ShadowTable.EllipseCoverage(0.0, 0.0, A, B, SIG), Is.EqualTo(0.984510).Within(3e-3));
            double prev = 1.0;
            for (int i = 0; i <= 4; i++)
            {
                double v = ShadowTable.EllipseCoverage(A + i, 0.0, A, B, SIG);
                Assert.That(v, Is.LessThan(prev), "장축 밖 " + i + "rem 에서 더 줄어야 한다");
                prev = v;
            }
            Assert.That(prev, Is.LessThan(0.01), "4rem 밖(= 2.2σ)이면 거의 0 이다");
            Assert.That(ShadowTable.EllipseCoverage(0.0, 0.0, 0.0, B, SIG), Is.EqualTo(0.0), "크기가 0 이면 0");
        }

        [Test]
        public void 적분_칸은_실측으로_정한_수다()
        {
            // 적분 구간을 |y| < b 로 **자른** 덕에 32칸이 참값과 3e-3 안이다. 8칸은 1e-2 로 **눈에 보이게** 나쁘다 —
            // 이 두 줄이 같이 있어야 «32» 가 짐작이 아니라 잰 수임이 남는다(칸을 줄이면 둘째 줄이 넘어간다).
            double truth = ShadowTable.EllipseCoverage(0.0, B + 0.4, A, B, SIG, 1024);
            double n32 = ShadowTable.EllipseCoverage(0.0, B + 0.4, A, B, SIG, ShadowTable.GlowSamples);
            double n8 = ShadowTable.EllipseCoverage(0.0, B + 0.4, A, B, SIG, 8);
            Assert.That(System.Math.Abs(n32 - truth), Is.LessThan(5e-3), "32칸은 참값 가까이");
            Assert.That(System.Math.Abs(n8 - truth), Is.GreaterThan(5e-3), "8칸은 재서 나쁘다");
            Assert.That(ShadowTable.GlowSamples, Is.EqualTo(32));
        }

        [Test]
        public void 소환진_그림자_겹은_안은_고인_빛_밖은_번진_빛이고_dy_만큼_아래로_치우친다()
        {
            // 정본 6949 `@keyframes srfloorglow` 의 두 틀 — 0%/100%(안 3.2 · 밖 3.6 · 번짐 .4) ↔ 50%(안 4.4 · 밖 5 · 번짐 .7).
            // 바깥 겹의 dy 는 .6rem(아래) 이고 `py` 는 CSS 부호라 **+가 아래**다.
            double inLo = ShadowTable.EllipseShadowAlpha(0, 0, A, B, 1.6, 1.8, 0.4, 0.6);
            double inHi = ShadowTable.EllipseShadowAlpha(0, 0, A, B, 2.2, 2.5, 0.7, 0.6);
            Assert.That(inLo, Is.EqualTo(0.006271).Within(3e-3), "가운데 0% 틀");
            Assert.That(inHi, Is.EqualTo(0.049565).Within(3e-3), "가운데 50% 틀");
            Assert.That(inHi, Is.GreaterThan(inLo), "안쪽 고인 빛은 50% 틀에서 더 차야 한다(흐림 3.2 → 4.4)");

            double up = ShadowTable.EllipseShadowAlpha(0, -(B + 1.0), A, B, 1.6, 1.8, 0.4, 0.6);
            double down = ShadowTable.EllipseShadowAlpha(0, B + 1.0, A, B, 1.6, 1.8, 0.4, 0.6);
            Assert.That(down, Is.GreaterThan(up + 0.1), "dy .6 이 아래를 더 밝힌다 — 부호가 뒤집히면 이 줄이 넘어진다");
            Assert.That(down, Is.EqualTo(0.489459).Within(3e-3));
            Assert.That(up, Is.EqualTo(0.243713).Within(3e-3));

            double side = ShadowTable.EllipseShadowAlpha(A + 0.5, 0, A, B, 1.6, 1.8, 0.4, 0.6);
            Assert.That(side, Is.EqualTo(0.327658).Within(3e-3), "장축 밖 .5rem");
            Assert.That(ShadowTable.EllipseShadowAlpha(A + 0.5, 0, A, B, 2.2, 2.5, 0.7, 0.6),
                        Is.GreaterThan(side), "바깥 번진 빛도 50% 틀에서 더 멀리 간다");
        }

        [Test]
        public void 망가진_표는_거부한다()
        {
            // rgba 를 CSS 의 0~255 로 그대로 옮겨 적는 실수 · 치우침도 흐림도 0 인 «그림자 아닌 것»
            string c255 = Json("[0,0,0,77]", 0.25);
            Assert.Throws<System.FormatException>(() => ShadowTable.From(MiniJson.ParseObject(c255)));
            string flat = Json("[0,0,0,0.3]", 0.0);
            Assert.Throws<System.FormatException>(() => ShadowTable.From(MiniJson.ParseObject(flat)));
        }
    }
}
