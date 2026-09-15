using UnityEngine;
using TMPro;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T361 — 정본 `white-space` 표(`Resources/WrapUi.json`)의 로더 + 거는 도우미. 규약·낱말은 Core <see cref="WrapRules"/>.
    /// ⓐ(공장 뒤집기 · `UiKit.Text` 기본을 «접는다» 로) 때 공장이 <see cref="Apply"/> 를 부르고, 그 전에는 자리 파일이 한 줄씩 부를 수 있다.
    /// </summary>
    public static class WrapUi
    {
        public const string ResourcePath = "WrapUi";
        static WrapTable table;

        public static WrapTable Table
        {
            get
            {
                if (table == null)
                {
                    TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
                    if (ta == null) throw new System.InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T361)");
                    table = WrapTable.From(MiniJson.ParseObject(ta.text));
                }
                return table;
            }
        }
        public static void Reset() { table = null; }

        /// <summary>그 자리가 접는가(표에 없으면 정본 기본 = 접는다).</summary>
        public static bool Wraps(string key) { return Table.Wraps(key); }

        /// <summary>표대로 건다 — 접는 자리는 Normal, 정본 `nowrap` 자리는 NoWrap. 넘침 모드는 건드리지 않는다(잘림·말줄임은 T351 의 몫). 돌려주는 값은 «접는가».</summary>
        public static bool Apply(TMP_Text t, string key)
        {
            bool w = Wraps(key);
            t.textWrappingMode = w ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            return w;
        }
    }
}
