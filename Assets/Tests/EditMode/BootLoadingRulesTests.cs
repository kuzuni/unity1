using System;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>T142 — 부팅 로딩 오버레이의 단계 표·망치 스윙·불티를 표(`Resources/BootLoadingUi.json`)에서 읽어
    /// 정본(`web/js/main.js` boot() · `web/index.html` 인라인 CSS)과 같은 값이 나오는지 잰다. 화면은 2회차가 세운다.</summary>
    public class BootLoadingRulesTests
    {
        static BootLoadingSpec S()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            string file = Path.Combine(root, "Assets", "Forge", "Resources", "BootLoadingUi.json");
            return BootLoadingSpec.From(MiniJson.ParseObject(File.ReadAllText(file)));
        }

        [Test]
        public void 단계는_정본_boot_의_일곱이고_퍼센트와_글자가_같다()
        {
            var s = S();
            Assert.AreEqual(7, s.Stages.Length);
            double[] pct = { 8, 24, 42, 58, 74, 96, 100 };
            string[] lab = { "세이브 불러오는 중…", "인터페이스 조립 중…", "전장 짓는 중…", "전투 준비 중…", "용광로 데우는 중…", "마무리 중…", "" };
            for (int i = 0; i < pct.Length; i++)
            {
                Assert.AreEqual(pct[i], s.Stages[i].Pct, 1e-9, "단계 " + i + " 퍼센트");
                Assert.AreEqual(lab[i], s.Stages[i].Label, "단계 " + i + " 글자");
            }
        }

        [Test]
        public void 진행률이_단계_사이면_아직_앞_단계다()
        {
            var s = S();
            Assert.AreEqual(-1, s.StageAt(0), "첫 단계(8%) 전에는 아무 단계도 아니다");
            Assert.AreEqual(0, s.StageAt(8));
            Assert.AreEqual(0, s.StageAt(23.9), "24% 에 닿기 전까지는 «세이브 불러오는 중…» 이다");
            Assert.AreEqual(1, s.StageAt(24));
            Assert.AreEqual(6, s.StageAt(100));
        }

        [Test]
        public void 채움_막대는_진행률에_비례하고_0과_100_밖은_잘린다()
        {
            var s = S();
            // 표의 수는 **정본 CSS px** 이고 `css_px`(499 ↔ 1080) 를 곱해 기준 캔버스로 온다.
            Assert.AreEqual(2.164, s.CssPx, 1e-9);
            // 정본 인라인 CSS `#boot-loading { … z-index: 200 }`(index.html 24) — 덮개는 `#app` 밖이라
            // 클론에서 이 수는 **제 오버레이 캔버스의 정렬 순서**가 된다(8회차).
            Assert.AreEqual(200, s.ZIndex, "정본 z-index: 200");
            Assert.AreEqual(216.0 * 2.164, s.TrackWPx, 1e-6, "정본 .bl-track width: 216 CSS px");
            Assert.AreEqual(0.0, s.FillWidthPx(0), 1e-9);
            Assert.AreEqual(s.TrackWPx * 0.5, s.FillWidthPx(50), 1e-6);
            Assert.AreEqual(s.TrackWPx, s.FillWidthPx(100), 1e-6);
            Assert.AreEqual(0.0, s.FillWidthPx(-5), 1e-9, "음수는 0 으로");
            Assert.AreEqual(s.TrackWPx, s.FillWidthPx(140), 1e-6, "100 넘으면 꽉 참");
        }

        [Test]
        public void 망치는_정본_bl_swing_의_두_각도_사이를_오간다()
        {
            var s = S();
            Assert.AreEqual(1000.0, s.SwingMs, 1e-9);
            // @keyframes bl-swing { 0%,12% { -52deg } 38%,55% { 6deg } 100% { -52deg } }
            Assert.AreEqual(-52.0, s.SwingDeg(0), 1e-9);
            Assert.AreEqual(-52.0, s.SwingDeg(120), 1e-9, "12% 까지는 들어 올린 채로 멈춰 있다");
            Assert.AreEqual(6.0, s.SwingDeg(380), 1e-9, "38% 에 내리쳐 닿는다");
            Assert.AreEqual(6.0, s.SwingDeg(550), 1e-9, "55% 까지 닿은 채로 머문다");
            Assert.AreEqual(-52.0, s.SwingDeg(1000), 1e-9);
            // 사이 구간은 두 각도 **안**에 있다(이징이 뭐든 넘어가지 않는다)
            for (double t = 0; t < 1000; t += 37)
            {
                double d = s.SwingDeg(t);
                Assert.GreaterOrEqual(d, -52.0 - 1e-9, "t=" + t);
                Assert.LessOrEqual(d, 6.0 + 1e-9, "t=" + t);
            }
        }

        [Test]
        public void 망치는_한_주기로_접힌다_그래서_무한_반복이다()
        {
            var s = S();
            for (double t = 0; t < 1000; t += 91)
                Assert.AreEqual(s.SwingDeg(t), s.SwingDeg(t + 3000), 1e-9, "t=" + t + " 는 세 주기 뒤와 같다");
        }

        [Test]
        public void 불티_셋은_저마다_지연과_방향이_있고_36퍼센트_전에는_안_보인다()
        {
            var s = S();
            double op, dx, dy;
            s.SparkAt(0, 0, out op, out dx, out dy);
            Assert.AreEqual(0.0, op, 1e-9, "0% 에는 투명하다(정본 0%,36% { opacity: 0 })");
            Assert.AreEqual(0.0, dx, 1e-9); Assert.AreEqual(0.0, dy, 1e-9);

            s.SparkAt(360, 0, out op, out dx, out dy);
            Assert.AreEqual(0.0, op, 1e-9, "36% 까지도 투명하다");

            s.SparkAt(440, 0, out op, out dx, out dy);
            Assert.AreEqual(1.0, op, 1e-9, "44% 에 켜진다");

            // 지연이 붙은 둘은 같은 벽시계에서 서로 다른 위상이다
            double op2, op3, d2x, d2y, d3x, d3y;
            s.SparkAt(440, 1, out op2, out d2x, out d2y);
            s.SparkAt(440, 2, out op3, out d3x, out d3y);
            Assert.Less(op2, 1.0, "s2 는 50ms 늦게 시작해 아직 정점이 아니다");
            Assert.Less(op3, 1.0, "s3 는 100ms 늦다");

            // 방향: s2 는 왼쪽 위, s3 는 오른쪽 위(정본 --dx/--dy)
            s.SparkAt(1000 - 1e-9, 1, out op2, out d2x, out d2y);
            s.SparkAt(1000 - 1e-9, 2, out op3, out d3x, out d3y);
            Assert.Less(d2x, 0.0, "s2 는 왼쪽으로");
            Assert.Greater(d3x, 0.0, "s3 는 오른쪽으로");
            Assert.Less(d2y, 0.0, "둘 다 위로");
            Assert.Less(d3y, 0.0);
        }

        [Test]
        public void 글자_크기는_환산한_뒤_종류_하한_안에_든다()
        {
            var s = S();
            // 런 331 실측: 환산을 안 하면 제목 22 · 단계 12 가 그대로 나가 `TextSizeGateTests` 가 **씬 전체에서**
            // 빨개졌다(그 자는 «활성 글자 전부» 를 본다 — 내 화면 하나가 남의 테스트 열여섯을 깼다).
            double title = s.TitlePx, stage = s.StagePx;
            Assert.AreEqual(22.0 * 2.164, title, 1e-6, "제목은 정본 22 CSS px");
            Assert.AreEqual(12.0 * 2.164, stage, 1e-6, "단계 글자는 정본 12 CSS px");
            // 카탈로그 종류 하한(Title 60 · Button 44 · Body 40 · Sub 36 · Micro 18)
            Assert.GreaterOrEqual(title, 44.0, "제목은 Button 하한을 넘는다(Title 60 엔 못 들어간다)");
            Assert.Less(title, 60.0, "그래서 Title 종류로 세우면 안 된다");
            Assert.GreaterOrEqual(stage, 18.0, "단계 글자는 Micro 하한을 넘는다");
            Assert.Less(stage, 36.0, "그래서 Sub 종류로 세우면 안 된다");
        }

        [Test]
        public void 색과_시각은_정본_인라인_CSS_그대로다()
        {
            var s = S();
            Assert.AreEqual("#ffd873", s.Colors["spark"]);
            Assert.AreEqual("#f2e8d8", s.Colors["title"]);
            Assert.AreEqual("#9aa7c0", s.Colors["stage"]);
            Assert.AreEqual("#e8a33d", s.Colors["fill_from"]);
            Assert.AreEqual("#ffd873", s.Colors["fill_to"]);
            Assert.AreEqual(0.12, s.TrackAlpha, 1e-9, ".bl-track background rgba(255,255,255,.12)");
            Assert.AreEqual(250.0, s.FillMs, 1e-9, ".bl-fill transition: width .25s");
            Assert.AreEqual(400.0, s.FadeMs, 1e-9, "인라인 CSS transition .4s");
            Assert.AreEqual(450.0, s.RemoveMs, 1e-9, "main.js blDone 의 setTimeout(…, 450)");
            Assert.Greater(s.RemoveMs, s.FadeMs, "페이드가 끝난 **뒤에** 치운다");
        }

        [Test]
        public void 신호가_선_만큼만_진행률이_간다()
        {
            var s = S();
            // 정본은 boot() 안에서 순서대로 blSet 을 부르지만 클론은 그 여섯 가지 일을 서로 다른
            // MonoBehaviour 가 제 차례에 한다 — 그래서 «부름» 이 아니라 «무엇이 섰는가» 로 읽는다.
            Assert.AreEqual(0, s.PctFromReady(false, false, false, false, false, false), 1e-9, "아무것도 안 섰으면 0");
            Assert.AreEqual(8, s.PctFromReady(true, false, false, false, false, false), 1e-9);
            Assert.AreEqual(24, s.PctFromReady(true, true, false, false, false, false), 1e-9);
            Assert.AreEqual(42, s.PctFromReady(true, true, true, false, false, false), 1e-9);
            Assert.AreEqual(58, s.PctFromReady(true, true, true, true, false, false), 1e-9);
            Assert.AreEqual(74, s.PctFromReady(true, true, true, true, true, false), 1e-9);
            Assert.AreEqual(100, s.PctFromReady(true, true, true, true, true, true), 1e-9, "여섯이 다 서면 마지막 칸");
            // 순서를 건너뛴 신호는 앞 단계에서 멎는다 — 뒤엣것이 먼저 서도 진행률이 앞질러 가지 않는다
            Assert.AreEqual(0, s.PctFromReady(false, true, true, true, true, true), 1e-9, "첫 신호가 아직이면 0 에서 멎는다");
            Assert.AreEqual(8, s.PctFromReady(true, false, true, true, true, true), 1e-9, "둘째가 아직이면 8 에서 멎는다");
        }

        [Test]
        public void 표가_깨지면_조용히_넘어가지_않는다()
        {
            // 고장 주입: 마지막 단계가 100 이 아니면 표를 못 읽는다(진행바가 안 차고 끝나는 갈래)
            const string bad = @"{""stages"":[{""pct"":8,""label"":""a""},{""pct"":90,""label"":""b""}],
                ""layout"":{},""times"":{},""colors"":{},""spark_move"":{},""swing"":[],""spark"":[]}";
            Assert.Throws<FormatException>(() => BootLoadingSpec.From(MiniJson.ParseObject(bad)));
            // 퍼센트가 거꾸로 가도 막는다
            const string back = @"{""stages"":[{""pct"":40,""label"":""a""},{""pct"":8,""label"":""b""},{""pct"":100,""label"":""""}],
                ""layout"":{},""times"":{},""colors"":{},""spark_move"":{},""swing"":[],""spark"":[]}";
            Assert.Throws<FormatException>(() => BootLoadingSpec.From(MiniJson.ParseObject(back)));
        }
    }
}
