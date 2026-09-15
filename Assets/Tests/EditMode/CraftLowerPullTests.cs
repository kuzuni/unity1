using System.IO;
using NUnit.Framework;
using Forge.Core.Data;

namespace Forge.Tests
{
    /// <summary>
    /// T390 — 제작 비교 팝업의 «버튼 아래 여백» 을 정본이 세 조각으로 못박아 뒀다(`style.css` 1819 `.cmp-lower { margin: 0 -.85rem -.85rem }` ·
    /// 1821 `.cmp-lower .row { padding-bottom: 1.44rem }` · 카드 패딩 1.1rem). 그 두 값이 표(`Resources/CraftUi.json`)에서 오고 코드에 박힌 수가 없는지,
    /// 당김이 카드 패딩보다 작아 카드 층의 아래 패딩이 양수로 남는지 본다. 실물 배치는 PlayMode `CraftLowerPullSceneTests` · 눈은 `screen_craft-compare.png`.
    /// </summary>
    public class CraftLowerPullTests
    {
        static string Root() { return Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path))); }
        static JsonObject Layout() { return J.Obj(MiniJson.ParseObject(File.ReadAllText(Path.Combine(Root(), "Assets", "Forge", "Resources", "CraftUi.json")))["layout"]); }

        static double CatalogValue(string key)
        {
            var cat = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Root(), "Assets", "Forge", "catalog.json")));
            foreach (object o in J.Arr(cat["layout"]))
            {
                JsonObject e = J.Obj(o);
                if (e != null && J.Str(e["key"]) == key) return J.Num(e["value"]);
            }
            Assert.Fail("catalog.json layout 에 «" + key + "» 이 없다");
            return 0;
        }

        [Test]
        public void 당김과_줄_아래_패딩은_표에서_오고_정본_값이다()
        {
            JsonObject l = Layout();
            Assert.IsTrue(J.IsNum(l["cmp_lower_pull_rem"]), "cmp_lower_pull_rem");
            Assert.IsTrue(J.IsNum(l["cmp_row_pad_bottom_rem"]), "cmp_row_pad_bottom_rem");
            Assert.AreEqual(0.85, J.Num(l["cmp_lower_pull_rem"]), 1e-9, "정본 1819 margin-bottom -.85rem");
            Assert.AreEqual(1.44, J.Num(l["cmp_row_pad_bottom_rem"]), 1e-9, "정본 1821 padding-bottom 1.44rem");
        }

        [Test]
        public void 당김은_카드_패딩보다_작아_카드_층_아래_패딩이_남는다()
        {
            // 카드 패딩은 catalog `card_pad`(H 분수 · 정본 1.1rem) — rem 으로 환산해 당김(.85rem)과 견준다. 당김이 더 크면 패널이 카드 밖으로 나간다.
            double cardPadRem = CatalogValue("card_pad") / CatalogValue("rem_h");
            double pull = J.Num(Layout()["cmp_lower_pull_rem"]);
            Assert.Greater(cardPadRem, pull, "card_pad(" + cardPadRem.ToString("0.00") + "rem) > 당김 " + pull + "rem — 정본 주석 «1.1rem 패딩 − .85rem = 인셋»");
            Assert.AreEqual(1.1, cardPadRem, 0.05, "catalog card_pad 는 정본 .modal-card padding 1.1rem");
        }

        [Test]
        public void 코드에_박힌_수가_없다()
        {
            string src = File.ReadAllText(Path.Combine(Root(), "Assets", "Scripts", "Game", "Ui", "ForgeCraftPopup.cs"));
            StringAssert.Contains("CraftStyle.Px(\"cmp_lower_pull_rem\")", src, "당김은 표에서");
            StringAssert.Contains("CraftStyle.Px(\"cmp_row_pad_bottom_rem\")", src, "줄 아래 패딩은 표에서");
            StringAssert.DoesNotContain("rem * 1.4f)", src, "T390 전의 박힌 수(rem × 1.4)");
        }
    }
}
