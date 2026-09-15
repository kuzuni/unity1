using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Forge.Core.Data;

namespace Forge.Tests
{
    /// <summary>
    /// T375 — 패스 트랙의 치수 넷이 **표에서** 온다(§1 «수치는 코드에 박지 않는다»). 정본은 그 넷을 전부 값으로 못 박아 뒀다:
    /// 필 폭 `calc(var(--app-w) * .206)`(`style.css` 2801) · 필 높이 `padding .1rem 0 + line-height 1`(같은 줄 · 원작 18.3/488W) ·
    /// 칸 패딩 `.4rem .5rem`(2824 — **세로와 가로가 다르다**) · 보상 알약 `padding .1rem .6rem` + 한 줄(2830).
    /// 종전 클론은 폭에 ×1.3 을 곱하고 높이 셋을 글꼴에서 뽑아 필이 +26% 넓고 보상 행 피치가 +10% 였다(T28 64회차 실측).
    /// 이 자는 **코드에 그 곱이 다시 들어오면** 빨개진다.
    /// </summary>
    public class PassTrackRulesTests
    {
        static string Root()
        {
            return Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
        }

        static string Source()
        {
            return File.ReadAllText(Path.Combine(Root(), "Assets", "Scripts", "Game", "Ui", "PassPopup.cs"));
        }

        static double Layout(string key)
        {
            var root = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Root(), "Assets", "Forge", "catalog.json")));
            var arr = J.Arr(J.Require(root, "layout"));
            for (int i = 0; i < arr.Count; i++)
            {
                var one = J.Obj(arr[i]);
                if (J.Str(J.Require(one, "key")) == key) return J.Num(J.Require(one, "value"));
            }
            throw new System.InvalidOperationException("catalog.json 의 layout 에 " + key + " 가 없다");
        }

        [Test]
        public void 트랙_치수_넷이_표에_있고_정본_값과_같다()
        {
            Assert.AreEqual(0.206, Layout("pass_label_w"), 1e-9, "정본 2801 `width: calc(var(--app-w) * .206)`");
            Assert.AreEqual(0.0375, Layout("pass_label_h"), 1e-9, "원작 shot-042705 실측 18.3px/488W");
            Assert.AreEqual(0.4, Layout("pass_cell_pad_y_rem"), 1e-9, "정본 2824 `.pass-cell { padding: .4rem .5rem }` 의 **세로**");
            Assert.Greater(Layout("pass_reward_pill_h_rem"), 0.0, "보상 알약 높이가 표에 있다");
        }

        [Test]
        public void 그_넉_줄에_박힌_곱이_없다()
        {
            string src = Source();
            Match m = Regex.Match(src, @"float labelW[^;]*;");
            Assert.IsTrue(m.Success, "labelW 줄을 못 찾았다");
            StringAssert.DoesNotContain("1.3f", m.Value, "필 폭에 ×1.3 이 돌아왔다(정본은 .206 그대로다)");
            StringAssert.DoesNotContain("FontSize", m.Value, "필 높이를 글꼴에서 뽑고 있다 — 정본은 값으로 정한다");
            StringAssert.Contains("pass_label_w", m.Value);
            StringAssert.Contains("pass_label_h", m.Value);

            Match pad = Regex.Match(src, @"float cellPadY[^;]*;");
            Assert.IsTrue(pad.Success, "cellPadY 줄을 못 찾았다");
            StringAssert.Contains("pass_cell_pad_y_rem", pad.Value, "칸 세로 패딩이 표에서 온다");

            Match pill = Regex.Match(src, @"float pillH[^;]*;");
            Assert.IsTrue(pill.Success, "pillH 줄을 못 찾았다");
            StringAssert.Contains("pass_reward_pill_h_rem", pill.Value, "보상 알약 높이가 표에서 온다");
            StringAssert.DoesNotContain("FontSize", pill.Value);
        }

        [Test]
        public void 칸의_세로_가로_패딩이_따로_간다()
        {
            string src = Source();
            StringAssert.Contains("float padX, float padY", src, "정본 `.4rem .5rem` 은 세로·가로가 다르다 — 한 값으로 쓰면 행이 길어진다");
            StringAssert.Contains("cellPadX, cellPadY", src, "부르는 쪽도 둘을 따로 준다");
        }
    }
}
