using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;

namespace Forge.Tests.EditMode
{
    /// <summary>
    /// T394 — 기술 트리 개요 카드의 세로는 정본 계약(style.css 2072~2087 · 원본 shot-042407 실측)을 표가 그대로 쥔다:
    /// 카드 높이 15.73%H · 헤더 3.15%H. 루트 폰트가 앱 높이 기준(844px = 16px)이라 %H 는 rem 으로 그대로 환산된다(%H × 844/1600). 코드에 수 없음(§1).
    /// </summary>
    public class TechCardHeightTests
    {
        static double Layout_(string key)
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            JsonObject cat = MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "Assets", "Forge", "catalog.json")));
            List<JsonObject> layout = J.List(J.Require(cat, "layout"), x => J.Obj(x));
            foreach (JsonObject e in layout) if (J.Str(e["key"]) == key) return J.Num(J.Require(e, "value"));
            Assert.Fail("catalog.json layout 에 " + key + " 가 없다");
            return 0;
        }
        const double RemPerH = 844.0 / 1600.0;   // 1%H = 844/1600 rem

        [Test]
        public void 카드_높이는_정본_계약_15_73퍼센트H_다()
        {
            double h = Layout_("tb_card_h_rem");
            Assert.AreEqual(8.30, h, 1e-9, "tb_card_h_rem = 15.73%H × 844/1600");
            Assert.AreEqual(15.73, h / RemPerH, 0.02, "rem → %H 되돌리면 정본 계약 15.73%H");
            Assert.Less(h, 9.6, "종전 9.6(17.81%H · 런 734)보다 작아야 행마다 +24px 쌓이던 밀림이 없어진다");
        }

        [Test]
        public void 헤더_높이는_표에서_오고_정본_3_15퍼센트H_다()
        {
            double head = Layout_("tb_head_h_rem");
            Assert.AreEqual(1.66, head, 1e-9, "tb_head_h_rem = 3.15%H × 844/1600");
            Assert.AreEqual(3.15, head / RemPerH, 0.02, "rem → %H 되돌리면 정본 계약 3.15%H");
            // 헤더는 글자 .82rem + 패딩 .45rem×2 = 1.72rem 상자 안(정본 줄높이 normal 이라 그보다 약간 작다) — 패딩 둘이 헤더 안에 든다.
            Assert.Greater(head, Layout_("tb_head_pad_rem") * 2.0, "패딩 둘이 헤더 안에 든다");
            // 아이콘 세로 예산 3.9rem(정본 2096~2099 🚨) — 헤더 + 아이콘 위 여백 + 원판 지름이 카드 안에 든다.
            double icon = Layout_("tb_icon_top_rem") + Layout_("tb_icon_bg_rem");
            Assert.LessOrEqual(head + icon, Layout_("tb_card_h_rem") + 1e-9, "헤더 + 아이콘 자리가 카드 높이 안에 든다");
        }
    }
}
