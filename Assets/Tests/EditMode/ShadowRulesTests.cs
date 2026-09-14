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
        public void 표는_정본_다섯_자리를_그대로_쥔다()
        {
            ShadowTable t = T();
            Assert.AreEqual(5, t.Count);
            ShadowSpec q = t.Get("qstrow_lip");
            Assert.AreEqual(0.0, q.DxRem, 1e-9);
            Assert.AreEqual(0.25, q.DyRem, 1e-9, "정본 .qst-row `0 .25rem 0`");
            Assert.AreEqual(0.3, q.A, 1e-9, "rgba(0,0,0,.3)");
            ShadowSpec e = t.Get("equipped_lip");
            Assert.AreEqual(0.22, e.DyRem, 1e-9);
            Assert.AreEqual(0.35, e.A, 1e-9);
        }

        [Test]
        public void 다섯_자리가_모두_딱딱한_턱이다()
        {
            // 흐린 자리는 굽는 길이 선 뒤(3회차)에 표에 담는다 — 지금 표에 섞이면 도우미가 조용히 딱딱하게 그린다.
            foreach (string k in T().Keys) Assert.IsTrue(T().Get(k).IsHard, k + " 는 blur·spread 가 0 이어야 한다");
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
