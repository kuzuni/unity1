using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;

namespace Forge.Tests.EditMode
{
    /// <summary>
    /// T400 — 자동 제련 카드는 정본이 공용 팝업 상한을 일부러 벗긴 카드다(style.css 4673~4682 경고 주석): 높이 84.52%H · 위끝 7.01%H · 아래끝 91.75%H(탭바 위끝 90.25%H 보다 아래).
    /// 표 `ForgeAutoUi.json` 이 그 셋을 쥔다(코드에 수 없음 · §1). 배선(`FitBetweenBars` 깎기 걷기)은 `ForgeAutoPopup.cs` lock 뒤 2회차.
    /// </summary>
    public class AutoForgeCardTests
    {
        static string Root_() { return Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path))); }
        static JsonObject Layout_()
        {
            return J.Obj(J.Require(MiniJson.ParseObject(File.ReadAllText(Path.Combine(Root_(), "Assets", "Forge", "Resources", "ForgeAutoUi.json"))), "layout"));
        }
        static double Catalog_(string key)
        {
            JsonObject cat = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Root_(), "Assets", "Forge", "catalog.json")));
            List<JsonObject> layout = J.List(J.Require(cat, "layout"), x => J.Obj(x));
            foreach (JsonObject e in layout) if (J.Str(e["key"]) == key) return J.Num(J.Require(e, "value"));
            Assert.Fail("catalog.json layout 에 " + key + " 가 없다");
            return 0;
        }

        [Test]
        public void 카드_높이와_위끝은_정본_실측_84_52와_7_01퍼센트H_다()
        {
            JsonObject l = Layout_();
            Assert.AreEqual(0.8452, J.Num(J.Require(l, "card_h_f")), 1e-9, "style.css 4682 `.af-card { height: calc(var(--app-h) * .8452) }`");
            Assert.IsTrue(J.IsNum(l["card_top_f"]), "ForgeAutoUi.json layout 에 card_top_f 가 없다");
            Assert.AreEqual(0.0701, J.Num(l["card_top_f"]), 1e-9, "정본 4675 주석 «카드 y7.01%H»");
        }

        [Test]
        public void 카드_아래끝은_탭바_위끝_아래로_내려간다_정본이_상한을_벗긴_까닭()
        {
            JsonObject l = Layout_();
            double bottom = J.Num(J.Require(l, "card_top_f")) + J.Num(J.Require(l, "card_h_f"));
            Assert.AreEqual(0.9175, bottom, 0.003, "정본 주석 «카드 하단 91.75%H»(7.01 + 84.52 = 91.53)");
            Assert.Greater(bottom, Catalog_("tabbar_top"), "원작 카드는 탭바 위끝(90.25%H)보다 아래로 내려간다 — 그래서 정본이 공용 상한을 벗기고 닫기 버튼을 z 31 로 올렸다");
            Assert.Less(bottom, 1.0, "앱 바닥 안");
        }
    }
}
