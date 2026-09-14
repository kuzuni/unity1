using NUnit.Framework;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>T352 ⓒ — 문구의 숫자 구간만 &lt;mspace&gt; 로 감싸는 자(정본 tabular-nums = 숫자만 등폭).</summary>
    public class TabularNumsTests
    {
        const double Em = 0.5834;

        [Test]
        public void 숫자_구간만_감싸고_단위_글자는_밖에_둔다()
        {
            Assert.AreEqual("<mspace=0.583em>1.5</mspace>K", TabularNums.Wrap("1.5K", Em));
            Assert.AreEqual("<mspace=0.583em>999</mspace>", TabularNums.Wrap("999", Em));
            Assert.AreEqual("<mspace=0.583em>12,345</mspace>", TabularNums.Wrap("12,345", Em), "숫자 사이 쉼표는 구간 안");
            Assert.AreEqual("⭐ <mspace=0.583em>1</mspace>-<mspace=0.583em>3</mspace> 보상", TabularNums.Wrap("⭐ 1-3 보상", Em), "두 구간 · 사이 글자는 밖");
        }

        [Test]
        public void 끝에_붙은_점이나_쉼표는_구간에_안_넣는다()
        {
            Assert.AreEqual("<mspace=0.583em>5</mspace>.", TabularNums.Wrap("5.", Em));
            Assert.AreEqual("<mspace=0.583em>5</mspace>,x", TabularNums.Wrap("5,x", Em));
        }

        [Test]
        public void 숫자가_없거나_폭이_없거나_이미_감쌌으면_그대로_돌려준다()
        {
            Assert.AreEqual("무료", TabularNums.Wrap("무료", Em));
            Assert.AreEqual("", TabularNums.Wrap("", Em));
            Assert.IsNull(TabularNums.Wrap(null, Em));
            Assert.AreEqual("12", TabularNums.Wrap("12", 0), "em 0 = 등폭 없음");
            string once = TabularNums.Wrap("12", Em);
            Assert.AreSame(once, TabularNums.Wrap(once, Em), "두 번 감싸지 않는다");
            Assert.IsTrue(TabularNums.IsWrapped(once));
            Assert.IsFalse(TabularNums.IsWrapped("12"));
        }

        [Test]
        public void em_은_불변_문화권_소수_셋째_자리까지다()
        {
            Assert.AreEqual("0.6", TabularNums.EmText(0.6));
            Assert.AreEqual("0.583", TabularNums.EmText(0.58339));
            Assert.AreEqual("1", TabularNums.EmText(1.0));
        }
    }
}
