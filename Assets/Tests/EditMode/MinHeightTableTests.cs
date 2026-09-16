using System.IO;
using NUnit.Framework;
using Forge.Core.Data;

namespace Forge.Tests.EditMode
{
    /// <summary>
    /// T402 — 정본 `min-height` 하한 셋이 코드가 아니라 표에서 온다(§1 «수치는 코드에 박지 않는다»).
    /// `.hatch-cell.empty` 8.6rem(style.css 4516) · `.league-challenge-row .btn.sm` 2.9rem(2626) · `.petup-bulkrow` 의 줄 상자 곱 1.4(4366 은 하한 2rem 만).
    /// 값 변화 0 · 화면 변화 0 — 틀린 것은 값이 아니라 «어디에 있는가» 였다.
    /// </summary>
    public class MinHeightTableTests
    {
        static string Root_()
        {
            return Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
        }
        static JsonObject PetLayout_()
        {
            return J.Obj(J.Require(MiniJson.ParseObject(File.ReadAllText(Path.Combine(Root_(), "Assets", "Forge", "Resources", "PetSkillUi.json"))), "layout"));
        }
        static double CatalogLayout_(string key)
        {
            JsonObject cat = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Root_(), "Assets", "Forge", "catalog.json")));
            foreach (JsonObject e in J.List(J.Require(cat, "layout"), x => J.Obj(x))) if (J.Str(e["key"]) == key) return J.Num(J.Require(e, "value"));
            Assert.Fail("catalog.json layout 에 " + key + " 가 없다");
            return 0;
        }
        static string Src_(string file)
        {
            return File.ReadAllText(Path.Combine(Root_(), "Assets", "Scripts", "Game", "Ui", file));
        }

        [Test]
        public void 세_하한이_표에_정본_값으로_있다()
        {
            JsonObject pet = PetLayout_();
            Assert.IsTrue(J.IsNum(pet["hatch_cell_empty_h_rem"]), "PetSkillUi.json 에 hatch_cell_empty_h_rem 이 없다");
            Assert.AreEqual(8.6, J.Num(pet["hatch_cell_empty_h_rem"]), 1e-9, "정본 4516 .hatch-cell.empty min-height 8.6rem");
            Assert.IsTrue(J.IsNum(pet["petup_bulk_line_k"]), "PetSkillUi.json 에 petup_bulk_line_k 가 없다");
            Assert.AreEqual(1.4, J.Num(pet["petup_bulk_line_k"]), 1e-9, "대량 선택 칩의 줄 상자 곱");
            Assert.AreEqual(2.0, J.Num(pet["petup_bulk_min_rem"]), 1e-9, "정본 4366 .petup-bulkrow min-height 2rem 은 그대로");
            Assert.AreEqual(2.9, CatalogLayout_("lc_btn_h_rem"), 1e-9, "정본 2626 .league-challenge-row .btn.sm min-height 2.9rem");
        }

        [Test]
        public void 부르는_쪽에_그_수가_다시_박히지_않았다()
        {
            // 값이 코드로 되돌아가면 표가 거짓말이 된다 — 세 자리의 옛 리터럴을 자취로 잡는다.
            string pet = Src_("PetPanel.cs");
            StringAssert.DoesNotContain("Rem(8.6f)", pet, "PetPanel.cs 가 8.6rem 을 다시 박았다");
            StringAssert.Contains("hatch_cell_empty_h_rem", pet, "PetPanel.cs 가 표 키를 안 읽는다");
            string league = Src_("LeagueSheet.cs");
            StringAssert.DoesNotContain("rem * 2.9f", league, "LeagueSheet.cs 가 2.9rem 을 다시 박았다");
            StringAssert.Contains("lc_btn_h_rem", league, "LeagueSheet.cs 가 표 키를 안 읽는다");
            string petup = Src_("PetUpgradePopup.cs");
            StringAssert.DoesNotContain("sub * 1.4f", petup, "PetUpgradePopup.cs 가 곱 1.4 를 다시 박았다");
            StringAssert.Contains("petup_bulk_line_k", petup, "PetUpgradePopup.cs 가 표 키를 안 읽는다");
        }
    }
}
