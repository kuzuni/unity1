using System;
using System.Collections.Generic;

namespace Forge.Core.Ui
{
    /// <summary>문구 한 조각 — 아이콘이면 <see cref="Icon"/> 에 아이콘 이름, 아니면 <see cref="Text"/> 에 글자.</summary>
    public struct IconRun
    {
        public string Text;
        public string Icon;
        public bool IsIcon { get { return Icon != null; } }
        public override string ToString() { return IsIcon ? "[" + Icon + "]" : Text; }
    }

    /// <summary>
    /// 화면 문구의 이모지를 아이콘으로 가르는 자(ROUTINE T89) — 정본 <c>ui.js</c> 의 <c>UI.paintIconText</c>(1400~1425행) 그대로.
    /// 표(<c>TOAST_ICON</c>)는 <c>Assets/StreamingAssets/data/ui-text.json</c> 이 쥔다(정본에서 <c>tools/export_data.js</c> 가 뽑는다 · 손으로 안 짓는다).
    /// <para>
    /// 규칙 넷은 전부 정본 주석에 근거가 있다:
    /// ⓐ **선두만이 아니라 문구 전체**를 훑는다(«🏆 1-1 첫 클리어! 🪙+60» 처럼 뒤에 붙는 재화 이모지가 남으면 한 줄에 아이콘과 이모지가 섞인다).
    /// ⓑ 이모지가 **이형 선택자(U+FE0F)** 로 두 글자인 경우가 있어 두 글자를 먼저 본다.
    /// ⓒ 한 글자로 맞은 뒤 남은 U+FE0F 는 같이 건너뛴다(보이지 않는 문자가 문구에 남는다).
    /// ⓓ 이모지 뒤 공백은 아이콘 마진이 대신하므로 건너뛴다.
    /// </para>
    /// 표에 없는 이모지는 **글자 그대로 남긴다** — 정본과 같다(아이콘을 새로 그리면 표에 줄이 늘고 그 순간부터 반영된다).
    /// 닉네임·장비명이 문구에 그대로 들어오므로 글자 조각은 절대 고쳐 쓰지 않는다.
    /// </summary>
    public static class IconText
    {
        /// <summary>문구 → 조각 목록(글자·아이콘 섞임). <paramref name="icon"/> 은 이모지 → 아이콘 이름(없으면 null).</summary>
        public static List<IconRun> Split(string msg, Func<string, string> icon)
        {
            var runs = new List<IconRun>();
            if (string.IsNullOrEmpty(msg)) return runs;
            if (icon == null) { runs.Add(new IconRun { Text = msg }); return runs; }

            var buf = new System.Text.StringBuilder();
            Action flush = delegate
            {
                if (buf.Length == 0) return;
                runs.Add(new IconRun { Text = buf.ToString() });
                buf.Length = 0;
            };

            int i = 0;
            while (i < msg.Length)
            {
                int n1 = CharLen(msg, i);
                string one = msg.Substring(i, n1);
                int n2 = i + n1 < msg.Length ? CharLen(msg, i + n1) : 0;
                string two = n2 > 0 ? msg.Substring(i, n1 + n2) : null;

                string name = two != null ? icon(two) : null;
                int used = n1 + n2;
                if (name == null) { name = icon(one); used = n1; }
                if (name == null) { buf.Append(one); i += n1; continue; }

                flush();
                runs.Add(new IconRun { Icon = name });
                i += used;
                if (i < msg.Length && msg[i] == '️') i++;          // ⓒ 남은 이형 선택자
                while (i < msg.Length && msg[i] == ' ') i++;            // ⓓ 이모지 뒤 공백
            }
            flush();
            return runs;
        }

        /// <summary>아이콘으로 바뀔 자리를 뺀 «글자만» — 글꼴 글리프 검사(T89 막이)가 이것을 본다.</summary>
        public static string TextOnly(string msg, Func<string, string> icon)
        {
            var sb = new System.Text.StringBuilder();
            foreach (IconRun r in Split(msg, icon)) if (!r.IsIcon) sb.Append(r.Text);
            return sb.ToString();
        }

        /// <summary>UTF-16 한 칸이 서로게이트 짝이면 2, 아니면 1(정본의 <c>Array.from</c> 코드포인트 훑기와 같은 단위).</summary>
        private static int CharLen(string s, int i)
        {
            return i + 1 < s.Length && char.IsHighSurrogate(s[i]) && char.IsLowSurrogate(s[i + 1]) ? 2 : 1;
        }
    }
}
