using System;
using System.IO;
using NUnit.Framework;
using Forge.Core.CraftFx;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>T138 — 정본 `floatLoot`(줄 상한·수명·lootpop) 와 `toast(msg, lane)` 레인 규칙을 표(`Resources/LootFeedUi.json`)로 재현한다.</summary>
    public class LootFeedRulesTests
    {
        static LootFeedSpec S()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            string file = Path.Combine(root, "Assets", "Forge", "Resources", "LootFeedUi.json");
            return LootFeedSpec.From(MiniJson.ParseObject(File.ReadAllText(file)));
        }

        [Test]
        public void 줄이_6을_넘으면_맨_위를_버린다_그래서_동시에_보이는_최대는_7()
        {
            var s = S();
            Assert.AreEqual(6, s.KeepMax);
            Assert.IsFalse(LootFeedRules.DropFirst(s, 0)); Assert.IsFalse(LootFeedRules.DropFirst(s, 6), "children.length > 6 이어야 버린다 — 6 은 안 버린다");
            Assert.IsTrue(LootFeedRules.DropFirst(s, 7)); Assert.IsTrue(LootFeedRules.DropFirst(s, 9));
            int n = 0;
            for (int i = 0; i < 20; i++) n = LootFeedRules.AfterPush(s, n);
            Assert.AreEqual(7, n, "빈 레인에 20번 붙이면 7 에서 멈춘다(정본 `> 6` 규칙 그대로)");
            Assert.AreEqual(1, LootFeedRules.AfterPush(s, 0)); Assert.AreEqual(7, LootFeedRules.AfterPush(s, 6)); Assert.AreEqual(7, LootFeedRules.AfterPush(s, 7));
        }

        [Test]
        public void 수명은_1_6초이고_lootpop_은_정본_키_그대로()
        {
            var s = S();
            Assert.AreEqual(1600, LootFeedRules.LifeMs(s));
            double a, ty;
            LootFeedRules.LootPop(s, 0, out a, out ty); Assert.AreEqual(0, a); Assert.AreEqual(0.5, ty, "translateY(.5rem)");
            LootFeedRules.LootPop(s, 12, out a, out ty); Assert.AreEqual(1, a, 1e-9); Assert.AreEqual(0, ty, 1e-9);
            LootFeedRules.LootPop(s, 80, out a, out ty); Assert.AreEqual(1, a, 1e-9); Assert.AreEqual(0, ty, 1e-9);
            LootFeedRules.LootPop(s, 100, out a, out ty); Assert.AreEqual(0, a, 1e-9); Assert.AreEqual(0, ty, 1e-9);
            LootFeedRules.LootPop(s, 6, out a, out ty); Assert.IsTrue(a > 0 && a < 1, "첫 구간은 CSS `ease` 로 오른다 — 지금 " + a);
            LootFeedRules.LootPop(s, 90, out a, out ty); Assert.IsTrue(a > 0 && a < 1);
            Assert.AreEqual(new CssEase(0.25, 0.1, 0.25, 1).Ease(0.5), RewardBurstSpec.EaseOf("ease").Ease(0.5), 1e-9, "CSS ease = cubic-bezier(.25,.1,.25,1)");
        }

        [Test]
        public void 토스트_레인은_combat_만_전투_상자로()
        {
            var s = S();
            Assert.IsTrue(LootFeedRules.IsCombatLane(s, "combat"));
            Assert.IsFalse(LootFeedRules.IsCombatLane(s, null)); Assert.IsFalse(LootFeedRules.IsCombatLane(s, "")); Assert.IsFalse(LootFeedRules.IsCombatLane(s, "Combat"));
            Assert.AreEqual("toasts-combat", LootFeedRules.CombatBoxName(s), "정본 #toasts-combat");
            Assert.AreEqual(0.6, s.RightRem); Assert.AreEqual(3.4, s.BottomRem); Assert.AreEqual(0.2, s.GapRem); Assert.AreEqual(0.78, s.FontRem);
            Assert.AreEqual(300, s.ToastHideMs);
        }
    }
}
