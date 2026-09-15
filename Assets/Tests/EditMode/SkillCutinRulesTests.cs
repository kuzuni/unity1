using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests.EditMode
{
    /// <summary>
    /// T384 — 스킬 컷인의 셈(<see cref="SkillCutinSpec"/>)이 정본 `@keyframes cutin .8s ease-out forwards`(style.css 1913~1919)를 그대로 낸다:
    /// 0% α0 배율 .3 → 15% α1 배율 1.25 → 30% 배율 1 → 75% α1 → 100% α0 + 위로 .6rem. 값은 표 `SkillCutinUi.json` 에서 읽는다(코드에 수 없음 · §1).
    /// </summary>
    public class SkillCutinRulesTests
    {
        static string File_()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            return Path.Combine(root, "Assets", "Forge", "Resources", "SkillCutinUi.json");
        }
        static SkillCutinSpec Spec_() { return SkillCutinSpec.From(MiniJson.ParseObject(File.ReadAllText(File_()))); }

        [Test]
        public void 표가_정본_자리와_크기를_쥔다()
        {
            SkillCutinSpec s = Spec_();
            Assert.AreEqual(0.20, s.TopF, 1e-9, "top: 20%");
            Assert.AreEqual(1.05, s.FontRem, 1e-9, "font-size 1.05rem — 키우지 마라(정본 주석)");
            Assert.AreEqual(0.04, s.LsEm, 1e-9, "letter-spacing .04em");
            Assert.AreEqual(1.5, s.IconEm, 1e-9, ".ico.cutin-ico 1.5em");
            Assert.AreEqual(0.3, s.IconGapEm, 1e-9, "margin-right .3em");
            Assert.AreEqual(0.6, s.RiseRem, 1e-9, "100% translateY(-.6rem)");
            Assert.AreEqual(800, s.DurMs, 1e-9, "animation .8s");
            Assert.AreEqual(10, s.TextBlurPx, 1e-9, "text-shadow 0 0 10px currentColor");
            Assert.AreEqual(6, s.IconBlurPx, 1e-9, "drop-shadow(0 0 6px currentColor)");
        }

        [Test]
        public void 키프레임_자리마다_정본_값이_나온다()
        {
            SkillCutinSpec s = Spec_();
            double a, sc, r;
            s.Sample(0, out a, out sc, out r);
            Assert.AreEqual(0, a, 1e-9, "0% opacity 0"); Assert.AreEqual(0.3, sc, 1e-9, "0% scale(.3)"); Assert.AreEqual(0, r, 1e-9);
            s.Sample(120, out a, out sc, out r);
            Assert.AreEqual(1, a, 1e-9, "15% opacity 1"); Assert.AreEqual(1.25, sc, 1e-9, "15% scale(1.25)"); Assert.AreEqual(0, r, 1e-9, "15% 는 아직 안 오른다");
            s.Sample(240, out a, out sc, out r);
            Assert.AreEqual(1, a, 1e-9, "30% 는 opacity 키가 없다 — 15→75 사이라 1"); Assert.AreEqual(1, sc, 1e-9, "30% scale(1)"); Assert.AreEqual(0, r, 1e-9, "30% translateY 0");
            s.Sample(600, out a, out sc, out r);
            Assert.AreEqual(1, a, 1e-9, "75% opacity 1"); Assert.AreEqual(1, sc, 1e-9, "75% 는 transform 키가 없다 — 30→100 사이라 배율 1");
            Assert.Greater(r, 0, "75% 는 30→100 사이라 이미 오르는 중"); Assert.Less(r, 0.6, "아직 .6rem 전");
            s.Sample(800, out a, out sc, out r);
            Assert.AreEqual(0, a, 1e-9, "100% opacity 0"); Assert.AreEqual(1, sc, 1e-9); Assert.AreEqual(0.6, r, 1e-9, "100% translateY(-.6rem)");
            Assert.IsTrue(s.Done(800)); Assert.IsFalse(s.Done(799));
        }

        [Test]
        public void 구간마다_ease_out_이_다시_걸린다()
        {
            SkillCutinSpec s = Spec_();
            double a, sc, r;
            // 0→15% 의 한가운데(7.5% = 60ms): ease-out 은 앞이 빠르니 선형(.5)보다 더 가 있다.
            s.Sample(60, out a, out sc, out r);
            Assert.Greater(a, 0.5, "ease-out 은 선형보다 앞선다");
            Assert.Greater(sc, 0.3 + (1.25 - 0.3) * 0.5, "배율도 같은 이징");
            // 75→100% 의 한가운데(87.5% = 700ms): 내려가는 구간도 앞이 빠르다 → 선형(.5)보다 더 내려가 있다.
            s.Sample(700, out a, out sc, out r);
            Assert.Less(a, 0.5, "사라지는 구간도 ease-out");
        }
    }
}
