using System.Text;
using TMPro;
using UnityEngine;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T396 20회차 — 정본이 **한 글 안의 한 줄**에만 리터럴 색을 못박은 자리(4613 `.tn-skip small` #c62828 · 5637 `.asc-wipe-warn` #ff6b5e).
    /// 클론은 그 글을 한 TMP 로 찍는다(줄 수·줄높이·크기 자 셋이 그 TMP 를 잰다) — 조각을 떼면 자 셋과 `check_br_lines` 가 같이 움직이므로,
    /// 그 줄만 `&lt;color=#hex&gt;…&lt;/color&gt;` 로 감싼다(TMP 리치 태그 · 글자 수·줄 수엔 안 센다). 값은 표 `PinnedColorUi.json`.
    /// check_richtext(T175) ALLOW — 리치 태그를 쓰려면 richText 를 켜야 한다. 플레이어 글이 안 지나는 두 자리(기술 노드 [건너뛰기] 라벨 ·
    /// 승천 초점 효과 글줄 — 둘 다 붙박이 문구 + 데이터 이름)만 부르고, 꺾쇠(`&lt;` `&gt;`)가 든 글은 손대지 않는다(TabularText·KeepAll 과 같은 규약).
    /// </summary>
    public static class LineInk
    {
        /// <summary>글의 <paramref name="line"/> 번째 줄(0부터 · `\n` 기준)을 표 키의 색으로 감싼다. 감쌌으면 true(줄이 없거나 꺾쇠가 든 글이면 false).</summary>
        public static bool Apply(TMP_Text t, int line, string pinnedKey)
        {
            if (t == null || string.IsNullOrEmpty(t.text)) return false;
            string wrapped = Wrap(t.text, line, PinnedColorUi.C(pinnedKey));
            if (wrapped == null) return false;
            t.richText = true;
            t.text = wrapped;
            return true;
        }

        /// <summary>순수 문자열 셈 — <paramref name="line"/> 번째 줄을 `&lt;color=#rrggbb&gt;…&lt;/color&gt;` 로. 꺾쇠가 든 글·없는 줄이면 null.</summary>
        public static string Wrap(string text, int line, Color color)
        {
            if (text == null || line < 0) return null;
            if (text.IndexOf('<') >= 0 || text.IndexOf('>') >= 0) return null;
            string[] lines = text.Split('\n');
            if (line >= lines.Length || lines[line].Length == 0) return null;
            lines[line] = "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + lines[line] + "</color>";
            return string.Join("\n", lines);
        }

        /// <summary>줄 잉크 태그가 들어 있는가.</summary>
        public static bool Has(TMP_Text t) { return t != null && t.text != null && t.text.IndexOf("<color=#", System.StringComparison.Ordinal) >= 0; }

        /// <summary>태그를 벗긴 글(자·되돌리기).</summary>
        public static string Strip(string s)
        {
            if (s == null) return null;
            var sb = new StringBuilder(s.Length);
            int i = 0;
            while (i < s.Length)
            {
                if (s[i] == '<')
                {
                    int j = s.IndexOf('>', i);
                    if (j > i && (s.Substring(i, j - i + 1).StartsWith("<color=#") || s.Substring(i, j - i + 1) == "</color>")) { i = j + 1; continue; }
                }
                sb.Append(s[i]); i++;
            }
            return sb.ToString();
        }
    }
}
