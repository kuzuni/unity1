using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;

namespace Forge.Tests.EditMode
{
    /// <summary>
    /// T388 — 정본이 앱 크기의 비율로 못 박은 «목록 상한·버튼 하한» 중 클론 실측이 **넘는** 세 자리의 값을 표가 쥔다(코드에 수 없음 · §1):
    /// `.forge-age-list` max-height .59H(style.css 722) · `.mat-grid` max-height .4H(802 · 탈것 쪽 `mtup_grid_max_f` 는 이미 있었다) ·
    /// `.modal-card.sheet .dg-banner .btn` min-width .1573W + 6.6px(3913). 배선은 각 파일 lock 뒤 2회차. 안 넘는 넷(리그 목록·프로필 카드·드롭다운·재화 알약)은 키를 안 둔다.
    /// </summary>
    public class LayoutLimitsTests
    {
        static string Root_()
        {
            return Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
        }
        static JsonObject Res_(string name)
        {
            return MiniJson.ParseObject(File.ReadAllText(Path.Combine(Root_(), "Assets", "Forge", "Resources", name + ".json")));
        }
        static double CatalogLayout_(string key)
        {
            JsonObject cat = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Root_(), "Assets", "Forge", "catalog.json")));
            List<JsonObject> layout = J.List(J.Require(cat, "layout"), x => J.Obj(x));
            foreach (JsonObject e in layout) if (J.Str(e["key"]) == key) return J.Num(J.Require(e, "value"));
            Assert.Fail("catalog.json layout 에 " + key + " 가 없다");
            return 0;
        }

        [Test]
        public void 모든_장비_목록의_상한은_정본_59퍼센트H_다()
        {
            JsonObject layout = J.Obj(J.Require(Res_("ForgeInfoUi"), "layout"));
            Assert.IsTrue(J.IsNum(layout["fl_list_max_h_f"]), "ForgeInfoUi.json layout 에 fl_list_max_h_f 가 없다");
            Assert.AreEqual(0.59, J.Num(layout["fl_list_max_h_f"]), 1e-9, "style.css 722 `.forge-age-list { max-height: calc(var(--app-h) * .59) }`");
            // 카드 고정 높이(.76H)보다 작아야 «목록이 카드 나머지를 채우던» 그림과 달라진다(실측 67%H 가 이 상한 아래로 내려온다).
            Assert.Less(J.Num(layout["fl_list_max_h_f"]), J.Num(layout["fl_card_h_f"]));
        }

        [Test]
        public void 펫_재료_격자의_상한은_탈것_쪽과_같은_정본_40퍼센트H_다()
        {
            JsonObject layout = J.Obj(J.Require(Res_("PetSkillUi"), "layout"));
            Assert.IsTrue(J.IsNum(layout["petup_grid_max_f"]), "PetSkillUi.json layout 에 petup_grid_max_f 가 없다");
            Assert.AreEqual(0.4, J.Num(layout["petup_grid_max_f"]), 1e-9, "style.css 802 `.mat-grid { max-height: calc(var(--app-h) * .4) }`");
            Assert.AreEqual(J.Num(layout["mtup_grid_max_f"]), J.Num(layout["petup_grid_max_f"]), 1e-9, "정본은 펫·탈것이 같은 `.mat-grid` 규칙이다");
        }

        [Test]
        public void 던전_배너_버튼의_하한은_정본_1573퍼센트W_더하기_6_6px_다()
        {
            Assert.AreEqual(0.1573, CatalogLayout_("dg_banner_btn_minw_f"), 1e-9, "style.css 3913 `min-width: calc(var(--app-w) * .1573 + 6.6px)` 의 비율 쪽");
            Assert.AreEqual(6.6, CatalogLayout_("dg_banner_btn_minw_px"), 1e-9, "같은 선언의 CSS px 쪽");
            // 기준 캔버스(1080×1920 · rem = 1920/844×16)에서 이 하한(≈184px)은 종전 `.dg-right .btn` 4.6rem(≈167px)보다 **넓다** — 그래서 종전 값으로 그리면 하한 아래다.
            double rem = 1920.0 / 844.0 * 16.0;
            Assert.Greater(0.1573 * 1080.0 + 6.6 * 2.164, CatalogLayout_("dg_btn_w_rem") * rem, "런 704 screen_dungeons 실측 84px@540 < 정본 하한 ≈92px@540");
        }
    }
}
