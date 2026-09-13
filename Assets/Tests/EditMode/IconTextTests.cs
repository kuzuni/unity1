using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>
    /// T89 — 화면 문구의 이모지를 아이콘으로 가르는 자가 정본 `UI.paintIconText`(ui.js 1400~1425) 와 같은가.
    /// 표는 `Assets/StreamingAssets/data/ui-text.json`(정본 `TOAST_ICON` 을 `tools/export_data.js` 가 뽑은 것).
    /// </summary>
    public class IconTextTests
    {
        static Dictionary<string, string> table;

        static Func<string, string> Icon
        {
            get
            {
                if (table == null)
                {
                    string text = File.ReadAllText(Path.Combine(DataDir.Path, "ui-text.json"));
                    var root = MiniJson.ParseObject(text);
                    var t = J.Obj(root["TOAST_ICON"]);
                    table = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (var kv in t) table[kv.Key] = J.Str(kv.Value);
                }
                return k => { string v; return table.TryGetValue(k, out v) ? v : null; };
            }
        }

        static string Shape(string msg)
        {
            var sb = new System.Text.StringBuilder();
            foreach (IconRun r in IconText.Split(msg, Icon)) sb.Append(r.ToString());
            return sb.ToString();
        }

        [Test]
        public void 정본_표가_읽히고_재화_이모지가_아이콘_이름을_준다()
        {
            Assert.IsNull(Icon("x"), "ASCII 는 표에 없다");     // 표를 먼저 읽힌다
            Assert.GreaterOrEqual(table.Count, 30, "TOAST_ICON 줄 수");
            Assert.AreEqual("coin", Icon("\U0001FA99"));
            Assert.AreEqual("gem", Icon("\U0001F48E"));
            Assert.AreEqual("hammer", Icon("\U0001F528"));
            Assert.AreEqual("tm_sword", Icon("⚔"));
            Assert.IsNull(Icon("가"), "표에 없는 글자는 null");
        }

        [Test]
        public void 선두만이_아니라_문구_전체의_이모지를_바꾼다()
        {
            // 정본 주석의 실제 사례 — 뒤에 붙는 재화 이모지가 남으면 한 줄에 아이콘과 이모지가 섞인다.
            Assert.AreEqual("[trophy]1-1 첫 클리어! [coin]+60", Shape("\U0001F3C6 1-1 첫 클리어! \U0001FA99+60"));
        }

        [Test]
        public void 이형_선택자와_이모지_뒤_공백을_건너뛴다()
        {
            // ⚔️(U+2694 U+FE0F) + 공백 → 아이콘 하나 · 보이지 않는 문자도 공백도 안 남는다.
            Assert.AreEqual("[tm_sword]전투력", Shape("⚔️ 전투력"));
            Assert.AreEqual("[hammer]29", Shape("⚙️29".Replace("⚙", "\U0001F528")));
        }

        [Test]
        public void 표에_없는_이모지와_글자는_그대로_남는다()
        {
            Assert.AreEqual("\U0001F600 안녕", Shape("\U0001F600 안녕"), "표에 없는 이모지는 글자 그대로(정본과 같다)");
            Assert.AreEqual("moonzzanf: 안녕", Shape("moonzzanf: 안녕"), "닉네임·문장은 한 조각으로 남는다");
        }

        [Test]
        public void 글자만_뽑기는_아이콘_자리를_뺀다()
        {
            Assert.AreEqual("1-1 첫 클리어! +60", IconText.TextOnly("\U0001F3C6 1-1 첫 클리어! \U0001FA99+60", Icon));
        }

        [Test]
        public void 조각_수가_토스트_문구에서_정본과_같다()
        {
            // 정본이 실제로 내는 토스트 몇 줄 — 아이콘 조각과 글자 조각의 수가 규칙대로 나오는가.
            Assert.AreEqual(2, IconText.Split("\U0001F528 29", Icon).Count, "[hammer]+«29» 두 조각(이모지 뒤 공백은 아이콘 마진이 대신한다)");
            Assert.AreEqual(1, IconText.Split("\U0001F512", Icon).Count, "아이콘 하나뿐이면 글자 조각이 없다");
            Assert.AreEqual(1, IconText.Split("스테이지 2-10 도달 시 해금됩니다", Icon).Count, "이모지가 없으면 조각 하나 = 라벨 하나(호출부 계약이 안 바뀐다)");
        }

        [Test]
        public void 빈_문구와_표_없음도_안_터진다()
        {
            Assert.AreEqual(0, IconText.Split("", Icon).Count);
            Assert.AreEqual(0, IconText.Split(null, Icon).Count);
            Assert.AreEqual("\U0001FA99 코인", Shape2(IconText.Split("\U0001FA99 코인", null)), "표가 없으면 통째로 글자");
        }

        static string Shape2(List<IconRun> runs)
        {
            var sb = new System.Text.StringBuilder();
            foreach (IconRun r in runs) sb.Append(r.ToString());
            return sb.ToString();
        }
    }
}
