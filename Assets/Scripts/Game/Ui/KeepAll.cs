using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T466 — 정본 `word-break: keep-all`(한글을 **어절(띄어쓰기)에서만** 꺾는다 · style.css 2236 `.swc-name` · 3856 `.sheet-sub` · 7040 `.sr-name`).
    /// 클론 TMP 는 음절마다 꺾는 것이 기본(브라우저 `normal` 과 같다)이라 전역 설정은 안 바꾸고, 표 `WrapUi.json` `keep_all` 에 적힌 자리에서만
    /// 글을 어절 단위로 `&lt;nobr&gt;…&lt;/nobr&gt;` 로 감싼다(TMP 리치 태그 · 그 안에서는 안 꺾는다). 리치 태그(`&lt;sprite name="x"&gt;` 처럼 태그 안에 띄어쓰기가 있는 것)는
    /// 한 덩어리로 보고 이웃 글자와 같은 어절에 붙인다 — 태그 한가운데를 가르지 않는다.
    /// 값(어느 자리가 keep-all 인가)은 표 · 판정은 Core <see cref="WrapTable.KeepsAll"/>.
    /// </summary>
    public static class KeepAll
    {
        const string Open = "<nobr>", Close = "</nobr>";
        /// <summary>«태그 + 글자» 가 띄어쓰기 없이 이어진 덩어리 = 어절 하나.</summary>
        static readonly Regex Token = new Regex(@"(?:<[^>]*>|[^\s<])+", RegexOptions.Compiled);

        /// <summary>표에 keep-all 로 적힌 자리면 글을 어절 단위로 감싼다(이미 감쌌으면 그대로). 감쌌으면 true.</summary>
        public static bool Apply(TMP_Text t, string key)
        {
            if (t == null || !WrapUi.Table.KeepsAll(key)) return false;
            string s = t.text;
            if (string.IsNullOrEmpty(s) || Has(s)) return Has(s);
            // check_richtext(T175) ALLOW — 리치 태그를 쓰려면 richText 를 켜야 한다. 플레이어 글(닉네임·채팅·리그 이름)은 이 세 자리를
            //   지나지 않지만(카탈로그 장비 이름 · 붙박이 안내문 · 데이터의 스킬·펫·탈것 이름), 그래도 꺾쇠가 든 글은 손대지 않는다 —
            //   그러면 정본 `U.escapeHtml` 이 막던 «태그로 먹힘» 은 여기서도 못 일어난다(TabularText 와 같은 규약).
            if (s.IndexOf('<') >= 0 || s.IndexOf('>') >= 0) return false;
            t.richText = true;
            t.text = Wrap(s);
            return true;
        }

        /// <summary>이미 어절 감싸기가 들어 있는가.</summary>
        public static bool Has(string s) { return s != null && s.IndexOf(Open, System.StringComparison.Ordinal) >= 0; }
        public static bool Has(TMP_Text t) { return t != null && Has(t.text); }

        /// <summary>순수 문자열 셈 — 어절마다 `&lt;nobr&gt;` 를 씌운다(띄어쓰기는 그대로 · 태그는 이웃 글자와 한 덩어리).</summary>
        public static string Wrap(string s)
        {
            if (string.IsNullOrEmpty(s) || Has(s)) return s;
            var sb = new StringBuilder(s.Length + 16);
            int last = 0;
            foreach (Match m in Token.Matches(s))
            {
                sb.Append(s, last, m.Index - last);
                bool hasGlyph = false;
                foreach (char c in Regex.Replace(m.Value, "<[^>]*>", "")) { if (!char.IsWhiteSpace(c)) { hasGlyph = true; break; } }
                if (hasGlyph) sb.Append(Open).Append(m.Value).Append(Close); else sb.Append(m.Value);
                last = m.Index + m.Length;
            }
            sb.Append(s, last, s.Length - last);
            return sb.ToString();
        }

        /// <summary>감싼 것을 벗긴다(자·되돌리기).</summary>
        public static string Strip(string s) { return s == null ? null : s.Replace(Open, "").Replace(Close, ""); }
    }
}
