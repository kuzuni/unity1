using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T110·T108 — 이모지가 섞인 **두 줄 이상** 문구를 «줄마다 아이콘 + 글자» 로 세로로 세운다(정본 `.btn` 안의 `<br>`:
    /// «판매<br>🪙 +N» · «자동 🔁<br>ON» 꼴). 가로 <see cref="UiKit.IconTextRow"/> 는 줄바꿈을 모른 채 한 줄로 누르므로
    /// 줄바꿈(<c>\n</c>)에서 잘라 줄마다 <see cref="UiKit.IconTextRow"/> 를 세우고 세로 레이아웃으로 쌓는다 — 그래서
    /// 아이콘 규칙(표에 있는 이모지만 · 나머지 글자 그대로 · 아이콘 한 칸 = 글자 크기 정사각)은 가로 줄과 똑같다.
    /// 줄이 하나뿐이면 가로 줄 하나를 감싼 상자라 모양이 <see cref="UiKit.IconTextRow"/> 와 같다.
    /// <para>왜 <c>UiKit.cs</c> 가 아니라 제 파일인가 — 그 파일은 T121 lock(글꼴 굽기) 이다(결정 275). 호출부(T87 lock 의 `Forge*`)가 풀리면 2회차가 잇는다.</para>
    /// </summary>
    public static class IconTextStack
    {
        /// <returns>세로 상자(줄 상자 <c>"line-1"</c>·<c>"line-2"</c>… · 줄 안은 <see cref="UiKit.IconTextRow"/> 와 같이 <c>"msg"</c>·<c>"ico-N"</c>).</returns>
        public static RectTransform Build(Transform parent, string name, TextKind kind, string msg, string colorKey = null,
                                          TextAlignmentOptions align = TextAlignmentOptions.Center, float lineGap = 0f)
        {
            RectTransform box = UiKit.Box(parent, name);
            var lay = box.gameObject.AddComponent<VerticalLayoutGroup>();
            lay.childAlignment = align.ToString().IndexOf("Left", System.StringComparison.Ordinal) >= 0 ? TextAnchor.MiddleLeft
                               : align.ToString().IndexOf("Right", System.StringComparison.Ordinal) >= 0 ? TextAnchor.MiddleRight
                               : TextAnchor.MiddleCenter;
            lay.childControlWidth = true; lay.childControlHeight = true;
            lay.childForceExpandWidth = false; lay.childForceExpandHeight = false;
            lay.spacing = lineGap;
            string[] lines = (msg ?? "").Split('\n');
            for (int i = 0; i < lines.Length; i++)
                UiKit.IconTextRow(box, "line-" + (i + 1), kind, lines[i], colorKey, align);
            return box;
        }

        /// <summary>줄 수(정본 `<br>` 개수 + 1).</summary>
        public static int LineCount(RectTransform stack)
        {
            int n = 0;
            for (int i = 0; i < stack.childCount; i++) if (stack.GetChild(i).name.StartsWith("line-")) n++;
            return n;
        }
    }
}
