using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Forge.Core.Data;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T168 — 정본 자간(`letter-spacing`)을 그 자리에 준다.
    ///
    /// 왜 한 군데인가: 정본 값은 **em** 인데 TMP <see cref="TMP_Text.characterSpacing"/> 은 **1/100 em** 이다.
    /// 환산이 호출부마다 흩어지면 «×100 을 빠뜨린 자리» 가 조용히 생긴다(그러면 자간이 100분의 1 이라 사실상 0 이다).
    /// 그래서 환산은 여기 한 줄뿐이고, 값은 `Assets/Forge/Resources/LetterSpacingUi.json` 이 쥔다(§1 — 수치를 코드에 안 박는다).
    ///
    /// 되밀기(`indentKey`): 자간은 **마지막 글자 뒤에도** 붙어 가운데 정렬이 왼쪽으로 쏠린다. 정본은 같은 몫을
    /// `text-indent`(`.bw-sub`) · `padding-left`(사망 배너 제목)로 되민다 — 여기서는 TMP 여백(<see cref="TMP_Text.margin"/>)의 왼쪽으로 같은 일을 한다.
    /// </summary>
    public static class LetterSpacing
    {
        public const string ResourcePath = "LetterSpacingUi";

        static JsonObject root;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new KeyNotFoundException("Resources/" + ResourcePath + ".json 이 없다");
            root = J.Obj(MiniJson.Parse(ta.text));
            if (root == null || root.Count == 0) throw new KeyNotFoundException(ResourcePath + ".json 을 못 읽었다");
        }

        /// <summary>표의 자간(em).</summary>
        public static float Em(string key)
        {
            Load();
            object v = root[key];
            if (!J.IsNum(v)) throw new KeyNotFoundException(ResourcePath + ".json 에 «" + key + "» 이 없다");
            return (float)J.Num(v);
        }

        /// <summary>TMP 단위(1/100 em) — 환산은 이 한 줄뿐이다.</summary>
        public static float Tmp(string key) { return Em(key) * 100f; }

        /// <summary>그 글자에 표의 자간을 준다. <paramref name="indentKey"/> 를 주면 같은 몫을 왼쪽 여백으로 되민다.</summary>
        public static void Apply(TMP_Text text, string key, string indentKey = null)
        {
            if (text == null) return;
            text.characterSpacing = Tmp(key);
            if (indentKey == null) return;
            Vector4 m = text.margin;
            m.x += Em(indentKey) * text.fontSize;
            text.margin = m;
        }
    }
}
