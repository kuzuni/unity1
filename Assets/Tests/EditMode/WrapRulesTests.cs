using System;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests.EditMode
{
    /// <summary>T361 — 정본 `white-space` 표(`WrapUi.json`)의 규약과 Core 낱말(UnityEngine 0). 정본과의 왕복 대조는 `tools/check_wrap.py`.</summary>
    public class WrapRulesTests
    {
        static string File_()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            return Path.Combine(root, "Assets", "Forge", "Resources", "WrapUi.json");
        }
        static JsonObject Root_() { return MiniJson.ParseObject(File.ReadAllText(File_())); }
        static WrapTable Table_() { return WrapTable.From(Root_()); }

        [Test]
        public void 표는_41_자리이고_nowrap_40_normal_1_이다()
        {
            WrapTable t = Table_();
            Assert.AreEqual(41, t.Count, "정본 white-space 선언 41");
            Assert.AreEqual(40, t.CountOf(WrapRules.NoWrap), "nowrap 40");
            Assert.AreEqual(1, t.CountOf(WrapRules.Normal), "normal 1(.sr-name · 두 줄 허용)");
            Assert.AreEqual(WrapRules.Normal, t.Mode("sr_name"), "style.css 7040 .sr-name { white-space: normal }");
            Assert.IsTrue(t.Wraps("sr_name"));
            Assert.IsFalse(t.Wraps("bw_track_span"), "정본 402 .bw-track span { white-space: nowrap }");
            Assert.IsFalse(t.Wraps("coin_amt"), "정본 7426 .coin-amt nowrap");
        }

        [Test]
        public void 표에_없는_자리는_정본_기본대로_접는다()
        {
            WrapTable t = Table_();
            Assert.IsTrue(WrapRules.DefaultWraps, "CSS 기본값 normal = 접는다 — 정본은 예외 40 만 nowrap");
            Assert.IsTrue(t.Wraps("rates_tip"), "정본 4600 .rates-tip 은 선언이 없다 → 접힌다(등재문 실물)");
            Assert.IsTrue(t.Wraps("qst_name"), "정본 2034 .qst-name 도 선언 없음 → 접힌다");
            Assert.IsFalse(t.Has("rates_tip"));
            Assert.Throws<FormatException>(() => t.Mode("rates_tip"), "낱말을 묻는 것은 표에 있는 자리만");
        }

        [Test]
        public void 낱말은_nowrap_normal_둘뿐이고_그_밖은_거부한다()
        {
            Assert.IsTrue(WrapRules.Wraps("normal"));
            Assert.IsFalse(WrapRules.Wraps("nowrap"));
            Assert.Throws<FormatException>(() => WrapRules.Wraps("pre"));
            Assert.Throws<FormatException>(() => WrapRules.Wraps(""));
            Assert.Throws<FormatException>(() => WrapRules.Wraps(null));
        }

        [Test]
        public void 키는_선택자에서_기계로_만든다_자와_같은_규칙()
        {
            Assert.AreEqual("currency_pills_pill", WrapRules.KeyOf(".currency-pills .pill"));
            Assert.AreEqual("summon_subtabs_subtab_strip_button", WrapRules.KeyOf("#summon-subtabs.subtab-strip button"));
            Assert.AreEqual("petup_selrow_btn_silver", WrapRules.KeyOf(".petup-selrow .btn.silver"));
            Assert.AreEqual("sk_mini_small", WrapRules.KeyOf(".sk-mini small"));
            Assert.Throws<FormatException>(() => WrapRules.KeyOf(""));
            Assert.Throws<FormatException>(() => WrapRules.KeyOf("---"));
            WrapTable t = Table_();
            foreach (string k in t.Keys) Assert.AreEqual(WrapRules.KeyOf(k), k, "표의 키가 규칙 밖이다: " + k);
        }

        [Test]
        public void 자리마다_정본_기록이_있고_원값과_낱말이_같다()
        {
            JsonObject root = Root_();
            JsonObject src = J.Obj(root["_정본"]);
            JsonObject sites = J.Obj(root["sites"]);
            Assert.IsNotNull(src); Assert.IsNotNull(sites);
            Assert.AreEqual(sites.Count, src.Count, "자리마다 «style.css:줄  선택자  → 값» 한 줄");
            foreach (var kv in sites)
            {
                string rec = J.Str(src[kv.Key]);
                Assert.IsNotNull(rec, kv.Key + " 의 정본 기록이 없다");
                StringAssert.StartsWith("style.css:", rec, kv.Key);
                StringAssert.EndsWith("→ " + J.Str(kv.Value), rec, kv.Key + ": 낱말이 원값과 다르다");
            }
        }

        [Test]
        public void 깨진_표는_거부한다()
        {
            Assert.Throws<FormatException>(() => WrapTable.From(null));
            Assert.Throws<FormatException>(() => WrapTable.From(MiniJson.ParseObject("{\"sites\": {}}")), "자리 0");
            Assert.Throws<FormatException>(() => WrapTable.From(MiniJson.ParseObject("{\"sites\": {\"a\": \"pre\"}}")), "정본에 없는 낱말");
            Assert.Throws<FormatException>(() => WrapTable.From(MiniJson.ParseObject("{\"sites\": {\"A-b\": \"nowrap\"}}")), "키 규칙 밖");
            Assert.Throws<FormatException>(() => WrapTable.From(MiniJson.ParseObject("{\"sites\": {\"a\": 1}}")), "낱말이 아니다");
            Assert.Throws<FormatException>(() => WrapTable.From(MiniJson.ParseObject("{\"x\": 1}")), "sites 없음");
            WrapTable ok = WrapTable.From(MiniJson.ParseObject("{\"sites\": {\"a\": \"nowrap\", \"_note\": \"x\"}}"));
            Assert.AreEqual(1, ok.Count, "`_` 칸은 설명");
        }
        /// <summary>T361 6회차 — 클론 쪽 예외(`clone_nowrap`): 정본은 접는 자리지만 클론 고정 높이 행 + 글자 하한 때문에 안 접는다. 정본 표 41 과 따로 센다.</summary>
        [Test]
        public void 클론_예외_칸은_안_접고_정본_표_41_과_따로_센다()
        {
            WrapTable t = Table_();
            Assert.AreEqual(41, t.Count, "정본 표는 그대로 41");
            Assert.GreaterOrEqual(t.CloneCount, 1, "클론 예외가 하나는 있다(league_challenge_name)");
            Assert.IsTrue(t.IsCloneException("league_challenge_name"));
            Assert.IsFalse(t.Wraps("league_challenge_name"), "클론 예외 자리는 안 접는다");
            Assert.AreEqual(WrapRules.NoWrap, t.Mode("league_challenge_name"));
            Assert.IsFalse(t.IsCloneException("coin_amt"), "정본 표 자리는 예외가 아니다");
            JsonObject root = Root_();
            object co; Assert.IsTrue(root.TryGet("clone_nowrap", out co));
            foreach (var kv in J.Obj(co)) if (!kv.Key.StartsWith("_")) Assert.IsTrue(J.Obj(root["_클론_자리"]).Has(kv.Key), "예외 자리마다 «_클론_자리» 에 정본 줄·까닭이 있다: " + kv.Key);
        }

        [Test]
        public void keep_all_표는_정본_word_break_세_자리를_쥐고_다른_자리는_false_다()
        {
            // T466 — style.css 2236 .swc-name · 3856 .sheet-sub · 7040 .sr-name { word-break: keep-all }
            var t = Table_();
            Assert.AreEqual(3, t.KeepAllCount, "정본 keep-all 선언 셋");
            Assert.IsTrue(t.KeepsAll("swc_name")); Assert.IsTrue(t.KeepsAll("sheet_sub")); Assert.IsTrue(t.KeepsAll("sr_name"));
            Assert.IsFalse(t.KeepsAll("waypoint_time"), "white-space 자리라도 keep-all 이 아니면 false(정본 기본 normal = 음절마다 꺾는다)");
            Assert.IsFalse(t.KeepsAll("no_such_site"), "표에 없는 자리는 false(던지지 않는다)");
            Assert.AreEqual(41, t.Count, "white-space 자리 41 은 그대로(keep_all 은 따로 센다)");
            var root = Root_();
            var ka = J.Obj(J.Require(root, "keep_all"));
            ka["sheet_sub"] = "normal";
            Assert.Throws<System.FormatException>(() => WrapTable.From(root), "keep_all 의 낱말은 keep-all 뿐");
        }
    }
}
