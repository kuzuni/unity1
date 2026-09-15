using System.IO;
using NUnit.Framework;
using Forge.Core.Data;

namespace Forge.Tests.EditMode
{
    /// <summary>
    /// T374 — 펫 업그레이드 «업그레이드» 버튼의 세로는 표가 쥔다: `PetSkillUi.json` `layout.petup_sel_btn_h` = 원작 shot-042503(505×889) 실측
    /// 버튼 바깥 상자 42px = 4.72%H. 정본 `style.css` 4363 은 폭(`petup_sel_btn_w` .1525)만 못 박고 높이는 글자 줄 상자에 맡기는데
    /// TMP 줄 상자가 브라우저 `normal` 보다 좁아 33px 로 줄었다 — 폭에 한 처방을 높이에도 한다. 코드엔 수가 없다(§1).
    /// </summary>
    public class PetUpgradeButtonTests
    {
        static string File_()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            return Path.Combine(root, "Assets", "Forge", "Resources", "PetSkillUi.json");
        }
        static JsonObject Layout_() { return J.Obj(J.Require(MiniJson.ParseObject(File.ReadAllText(File_())), "layout")); }

        [Test]
        public void 표에_버튼_세로_키가_있고_정본_4_72퍼센트H_다()
        {
            JsonObject layout = Layout_();
            object v = layout["petup_sel_btn_h"];
            Assert.IsTrue(J.IsNum(v), "PetSkillUi.json layout 에 petup_sel_btn_h 가 없다");
            Assert.AreEqual(0.0472, J.Num(v), 1e-9, "원작 shot-042503 «업그레이드» 버튼 바깥 상자 42px / 889px");
        }

        [Test]
        public void 원작_앱_높이_889_에서_42px_로_돌아온다()
        {
            JsonObject layout = Layout_();
            double h = J.Num(layout["petup_sel_btn_h"]);
            Assert.AreEqual(42.0, h * 889.0, 0.5, "원작 shot-042503(505×889) 화소 실측 42px");
            // 폭 키와 같은 «앱 비율» 단위라 둘이 한 표에서 같이 산다 — 폭 15.25%W(77px) 는 그대로
            Assert.AreEqual(0.1525, J.Num(layout["petup_sel_btn_w"]), 1e-9, "폭은 건드리지 않는다");
            Assert.Greater(h, 0.0);
            Assert.Less(h, 0.1, "버튼 하나가 화면의 10분의 1 을 넘지 않는다");
        }
    }
}
