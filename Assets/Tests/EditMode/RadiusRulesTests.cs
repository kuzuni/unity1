using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests.EditMode
{
    /// <summary>T345 — 정본 `border-radius` 표(`RadiusUi.json`)의 규약과 Core 셈(UnityEngine 0).</summary>
    public class RadiusRulesTests
    {
        static string File_()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            return Path.Combine(root, "Assets", "Forge", "Resources", "RadiusUi.json");
        }
        static RadiusTable Table_() { return RadiusTable.From(MiniJson.ParseObject(File.ReadAllText(File_()))); }

        [Test]
        public void 표는_읽히고_키마다_단위_꼬리를_달고_값은_0_이상이다()
        {
            RadiusTable t = Table_();
            Assert.GreaterOrEqual(t.Count, 10, "20회차 ⓡ 가 센 어긋난 리터럴 열 자리가 표에 있어야 한다");
            foreach (string k in t.Keys)
            {
                Assert.IsTrue(RadiusRules.IsRadiusKey(k), "키 꼬리가 단위여야 한다(_r_rem · _r_w): " + k);
                Assert.GreaterOrEqual(t.Get(k), 0.0, k);
            }
            Assert.IsTrue(t.Has("chat_bubble_r_rem"), "채팅 말풍선 자리");
            Assert.IsTrue(t.Has("back_btn_r_w"), "뒤로 버튼은 앱 폭 비율 키다");
            Assert.AreEqual(0.0, t.Get("chat_share_card_r_rem"), 1e-12, "공유 카드는 정본이 각진 카드(0)다");
        }

        [Test]
        public void rem_키는_1rem_px_를_곱하고_appw_키는_앱_폭을_곱한다()
        {
            // 1rem = 36.4px(844 높이 기준) · 앱 폭 474.75
            Assert.AreEqual(0.42 * 36.4, RadiusRules.Px(0.42, "chat_bubble_r_rem", 36.4, 474.75), 1e-9);
            Assert.AreEqual(0.0094 * 474.75, RadiusRules.Px(0.0094, "back_btn_r_w", 36.4, 474.75), 1e-9);
            Assert.AreEqual(0.0, RadiusRules.Px(0, "chat_share_card_r_rem", 36.4, 474.75), 1e-12);
        }

        [Test]
        public void 단위_꼬리가_없는_키는_셈도_표도_거른다()
        {
            Assert.Throws<FormatException>(() => RadiusRules.Px(1, "chat_bubble", 36.4, 474.75));
            Assert.Throws<FormatException>(() => RadiusTable.From(MiniJson.ParseObject("{\"chat_bubble\": 0.42}")));
            Assert.Throws<FormatException>(() => RadiusTable.From(MiniJson.ParseObject("{\"a_r_rem\": -1}")));
            Assert.Throws<FormatException>(() => RadiusTable.From(MiniJson.ParseObject("{\"a_r_rem\": \"x\"}")));
            Assert.Throws<FormatException>(() => RadiusTable.From(MiniJson.ParseObject("{\"_설명\": \"만\"}")));
            Assert.Throws<FormatException>(() => Table_().Get("없는_r_rem"));
        }

        [Test]
        public void 설명_칸은_자리가_아니다()
        {
            RadiusTable t = RadiusTable.From(MiniJson.ParseObject("{\"_\": \"설명\", \"_a\": \"줄\", \"a_r_rem\": 0.5}"));
            Assert.AreEqual(1, t.Count);
            Assert.IsFalse(t.Keys.Any(k => k.StartsWith("_")));
        }

        [Test]
        public void 반지름은_짧은_변의_반을_못_넘고_알약은_높이의_반_이상이다()
        {
            Assert.AreEqual(9.0, RadiusRules.Clamp(20, 40, 18), 1e-12, "CSS 는 반지름을 짧은 변의 반으로 줄인다");
            Assert.AreEqual(5.0, RadiusRules.Clamp(5, 40, 18), 1e-12);
            Assert.AreEqual(0.0, RadiusRules.Clamp(-3, 40, 18), 1e-12);
            // 결정 543 — 정본 1rem/높이 1.35rem 토글은 높이의 반(알약)과 같은 그림이다
            Assert.IsTrue(RadiusRules.IsPill(1.0, 1.35));
            Assert.IsTrue(RadiusRules.IsPill(0.675, 1.35));
            Assert.IsFalse(RadiusRules.IsPill(0.42, 1.35));
        }
    }
}
