using System.IO;
using NUnit.Framework;
using Forge.Core.Data;

namespace Forge.Tests.EditMode
{
    /// <summary>
    /// T369 — 스킬 격자가 시작하는 자리는 표가 쥔다: `PetSkillUi.json` `layout.sk_grid_top_h` = 정본 `style.css` 4013~4015 `.sk-grid` 머리말의
    /// «1행 오브 상단 92px(10.34%H)»(원본 shot-042340 · 앱 496×890 · 화소로 재확인 y92). 코드엔 수가 없다(§1) — 배선(`SkillPanel.cs`)은 그 파일 lock 뒤.
    /// </summary>
    public class SkillGridTopTests
    {
        static string File_()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            return Path.Combine(root, "Assets", "Forge", "Resources", "PetSkillUi.json");
        }
        static JsonObject Layout_() { return J.Obj(J.Require(MiniJson.ParseObject(File.ReadAllText(File_())), "layout")); }

        [Test]
        public void 표에_격자_위_여백_키가_있고_정본_10_34퍼센트H_다()
        {
            JsonObject layout = Layout_();
            object v = layout["sk_grid_top_h"];
            Assert.IsTrue(J.IsNum(v), "PetSkillUi.json layout 에 sk_grid_top_h 가 없다");
            Assert.AreEqual(0.1034, J.Num(v), 1e-9, "정본 4015 «1행 오브 상단 92px(10.34%H)»");
        }

        [Test]
        public void 원본_앱_높이_890_에서_92px_로_돌아온다()
        {
            double top = J.Num(Layout_()["sk_grid_top_h"]);
            Assert.AreEqual(92.0, top * 890.0, 0.5, "원본 shot-042340(496×890) 화소 실측 y92");
            // 칸 안쪽 키들과 같은 단위(앱 높이 비율)라 오브 지름·행 간격과 더할 수 있어야 한다
            Assert.Greater(top, 0.0);
            Assert.Less(top, 0.2, "격자 위 여백은 패널의 5분의 1 안");
        }
    }
}
