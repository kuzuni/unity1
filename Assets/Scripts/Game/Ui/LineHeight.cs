using Forge.Core.Data;
using Forge.Core.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T354 — 정본 `line-height` 표(<c>Assets/Forge/Resources/LineHeightUi.json</c> · T354 1회차)를 글자에 건다.
    /// 정본은 «글자 크기의 배수» 이고 TMP <c>lineSpacing</c> 은 em/100 단위라 <see cref="LineHeightRules.TmpLineSpacing"/> 로 옮긴다
    /// (자산 줄높이 NotoSansKR 1.448em 이 기본이라 정본 1.15~1.3 자리는 음수 · 1.5 자리는 양수가 된다).
    /// <c>UiKit.cs</c> 가 T178·T331·T355 lock 이라 따로 둔 파일 — 공장에 넣는 것은 그 lock 뒤 한 줄이다.
    /// </summary>
    public static class LineHeight
    {
        public const string ResourcePath = "LineHeightUi";
        static LineHeightTable table;

        public static LineHeightTable Table
        {
            get
            {
                if (table == null)
                {
                    TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
                    if (ta == null) throw new System.InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T354)");
                    table = LineHeightTable.From(MiniJson.ParseObject(ta.text));
                }
                return table;
            }
        }
        public static void Reset() { table = null; }

        /// <summary>표 키 → 정본 배수(rem·앱 폭 키는 그 글자의 크기로 배수를 낸다).</summary>
        public static double Ratio(TMP_Text t, string key)
        {
            return LineHeightRules.Ratio(Table.Get(key), key, t.fontSize, PopupKit.Rem, UiKit.RefW);
        }

        /// <summary>글자가 아직 없는 자리(줄 칸 높이를 **먼저** 셈하는 곳)에서 쓰는 같은 셈 — 배수만 낸다.
        /// T354 18회차 — `PlayerInfoPopup` 머리줄처럼 글을 놓기 전에 줄 피치를 정해야 하는 자리가 있다.
        /// 위 <see cref="Ratio(TMP_Text, string)"/> 와 **한 글자도 다르지 않다**(같은 Core 셈 · 같은 rem·앱 폭).</summary>
        public static double Ratio(float fontSize, string key)
        {
            return LineHeightRules.Ratio(Table.Get(key), key, fontSize, PopupKit.Rem, UiKit.RefW);
        }

        /// <summary>그 글자에 표의 줄높이를 건다 — 돌려주는 값은 정본 배수.</summary>
        public static double Apply(TMP_Text t, string key)
        {
            double r = Ratio(t, key);
            FaceInfo f = t.font.faceInfo;
            t.lineSpacing = (float)LineHeightRules.TmpLineSpacing(r, f.lineHeight, f.pointSize);
            return r;
        }

        /// <summary>지금 그 글자가 실제로 그리는 줄 간격(배수) — 첫 두 줄의 기준선 차 ÷ 글자 크기(두 줄 미만이면 0).</summary>
        public static double MeasuredRatio(TMP_Text t)
        {
            t.ForceMeshUpdate();
            TMP_TextInfo ti = t.textInfo;
            if (ti == null || ti.lineCount < 2) return 0;
            float pitch = ti.lineInfo[0].baseline - ti.lineInfo[1].baseline;
            return pitch / t.fontSize;
        }
    }
}
