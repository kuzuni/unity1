using System.IO;
using NUnit.Framework;
using Forge.Core.Data;

namespace Forge.Tests.EditMode
{
    /// <summary>
    /// T405 — 펫 업그레이드 카드의 % 패딩은 표 `PetSkillUi.json` `layout.petup_pad_app_f` 가 쥐고, 이름의 `_app_` 이 «앱 폭에 곱한다» 를 못 박는다.
    /// 정본 style.css 4337 `.petup-card { padding: 1.78% }` — CSS 의 % 패딩은 컨테이닝 블록(모달 = 앱 폭) 기준이라 카드 폭(73.5%W)에 곱하면 26.5% 작다.
    /// 옛 이름 `petup_pad_f`(카드 폭에 곱하던 자리)는 표에서 사라져야 한다 — 남아 있으면 누가 다시 카드 폭에 곱한다.
    /// </summary>
    public class PetUpgradePadTests
    {
        static string File_()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            return Path.Combine(root, "Assets", "Forge", "Resources", "PetSkillUi.json");
        }
        static JsonObject Layout_() { return J.Obj(J.Require(MiniJson.ParseObject(File.ReadAllText(File_())), "layout")); }

        [Test]
        public void 표에_앱_폭_기준_패딩_키가_있고_정본_1_78퍼센트_다()
        {
            JsonObject layout = Layout_();
            object v = layout["petup_pad_app_f"];
            Assert.IsTrue(J.IsNum(v), "PetSkillUi.json layout 에 petup_pad_app_f 가 없다");
            Assert.AreEqual(0.0178, J.Num(v), 1e-9, "정본 4337 .petup-card padding 1.78%");
            Assert.IsFalse(layout.Has("petup_pad_f"), "옛 키 petup_pad_f(카드 폭에 곱하던 자리)가 아직 표에 있다");
        }

        [Test]
        public void 앱_폭에_곱하면_원작_흰_틈_9px_로_돌아온다()
        {
            double f = J.Num(Layout_()["petup_pad_app_f"]);
            // 원작 shot-042503 앱 폭 500 · 카드 왼쪽 테 ~ 회색 판 사이 9px = 1.80%W (판 키라인 2px 는 테 두께 축 몫)
            Assert.AreEqual(9.0, f * 500.0, 0.6, "원작 앱 폭 500 에서 9px");
            // 카드 폭(73.5%W)에 곱하면 26.5% 작다 — 그 오차가 이 절의 까닭이다
            Assert.Less(f * 0.735 * 500.0, 7.0, "카드 폭에 곱한 옛 셈은 7px 아래로 떨어진다");
        }
    }
}
