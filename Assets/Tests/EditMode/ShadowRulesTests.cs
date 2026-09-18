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
            Assert.AreEqual(30, t.Count, "자(check_box_shadows)가 세는 자리와 같은 수여야 한다 — 36회차 equipcell_drop 28 · 38회차 pettile_drop 29 · 39회차 mountcell_drop 30");
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
            Assert.AreEqual(7, hard, "딱딱한 턱(blur·spread 0) — 카드·패널·퀘스트 행·던전 배너·장착 바·자동 제련 스피너 + 대장간 나이 막대(29회차)");
            Assert.AreEqual(23, soft, "흐린 그림자(36회차 equipcell_drop 21 · 38회차 pettile_drop 22 · 39회차 mountcell_drop 23) — 29회차에 남은 열하나의 **값을 먼저 재** 표에 담았다(배선은 그 파일의 lock 이 풀리는 회차 · 자의 KNOWN 이 그 목록이다)");
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
