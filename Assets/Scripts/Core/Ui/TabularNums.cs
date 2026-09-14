using System.Globalization;
using System.Text;

namespace Forge.Core.Ui
{
    /// <summary>
    /// T352 ⓒ — 정본 `font-variant-numeric: tabular-nums`(style.css 8637 · «세로로 열을 이루는 숫자만 등폭으로») 를 TMP 로 옮기는 자.
    /// <para>
    /// TMP 에는 «숫자만 등폭» 이 없다. 등폭을 주는 길은 둘인데 <c>TMP_Text.monoSpacing</c> 은 **글자 전체**를 한 칸에 넣어
    /// 단위 글자(K·M)·소수점까지 같은 칸으로 벌리고, <c>&lt;mspace=Nem&gt;</c> 태그는 **감싼 구간만** 등폭이다.
    /// 정본이 등폭으로 만드는 것은 숫자(figures)뿐이므로 태그가 정본에 가깝다 — 그래서 문구 안의 **숫자 구간만** 태그로 감싼다(결정 570).
    /// </para>
    /// 숫자 구간 = 숫자가 이어진 것 + 그 사이에 낀 소수점·쉼표(«1.5» «12,345»). 앞뒤의 단위 글자·부호·공백은 그대로 둔다.
    /// 칸 폭(em)은 부르는 쪽이 글꼴에서 읽어 넘긴다(가장 넓은 숫자의 advance — tabular figures 의 정의 그대로) — 여기 숫자를 박지 않는다.
    /// </summary>
    public static class TabularNums
    {
        public const string OpenHead = "<mspace=";
        public const string OpenTail = "em>";
        public const string Close = "</mspace>";

        /// <summary>태그에 적는 em 값(불변 문화권 · 소수 셋째 자리까지).</summary>
        public static string EmText(double em) { return em.ToString("0.###", CultureInfo.InvariantCulture); }

        /// <summary>이미 감싼 문구인가(두 번 감싸지 않는다).</summary>
        public static bool IsWrapped(string text) { return text != null && text.IndexOf(OpenHead, System.StringComparison.Ordinal) >= 0; }

        static bool Digit(char c) { return c >= '0' && c <= '9'; }

        /// <summary>문구의 숫자 구간마다 <c>&lt;mspace=Nem&gt;…&lt;/mspace&gt;</c> 를 두른다. em ≤ 0 · 빈 문구 · 숫자 없음 · 이미 감쌈이면 그대로 돌려준다.</summary>
        public static string Wrap(string text, double em)
        {
            if (string.IsNullOrEmpty(text) || em <= 0 || IsWrapped(text)) return text;
            string open = OpenHead + EmText(em) + OpenTail;
            var sb = new StringBuilder(text.Length + 32);
            int i = 0, n = text.Length;
            bool any = false;
            while (i < n)
            {
                if (!Digit(text[i])) { sb.Append(text[i]); i++; continue; }
                int j = i;
                while (j < n)
                {
                    if (Digit(text[j])) { j++; continue; }
                    // 숫자 사이에 낀 소수점·쉼표 하나만 구간에 넣는다(«1.5K» 의 점 · «12,345» 의 쉼표)
                    if ((text[j] == '.' || text[j] == ',') && j + 1 < n && Digit(text[j + 1])) { j += 2; continue; }
                    break;
                }
                sb.Append(open).Append(text, i, j - i).Append(Close);
                any = true;
                i = j;
            }
            return any ? sb.ToString() : text;
        }
    }
}
