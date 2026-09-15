using System.Collections.Generic;
using Forge.Core.Data;
using TMPro;
using UnityEngine;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T383 — §1 글자 하한의 예외 칸(<see cref="TextKind.Micro"/> · 결정 633)에 **정본 크기**를 주는 표(<c>Assets/Forge/Resources/TextSizeUi.json</c>).
    /// 정본이 rem 으로 못 박은 <c>font-size</c> 를 자리 키 → rem 으로 쥐고, <see cref="Apply"/> 가 종류 <c>Micro</c> 인 글자에 px 로 건다.
    /// 자(<c>TextSizeGateTests</c>)는 «종류의 하한» 만 보므로 정본 크기(Micro 18 이상)가 그대로 선다 — 하한 36 이 정본이 못박은 모양(<c>&lt;br&gt;</c> 줄 수)을
    /// 깨는 자리에만 쓴다. 수치는 표에만 있다(§1).
    /// </summary>
    public static class TextSizeUi
    {
        public const string ResourcePath = "TextSizeUi";
        static JsonObject root, size;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new System.InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T383)");
            root = MiniJson.ParseObject(ta.text);
            size = J.Obj(root["size"]);
        }
        public static void Reset() { root = null; size = null; }

        public static bool Has(string key) { Load(); return J.Obj(size[key]) != null; }

        /// <summary>표의 정본 크기(rem) — 없는 키는 예외(표에 먼저 적는다).</summary>
        public static float Rem(string key)
        {
            Load();
            JsonObject o = J.Obj(size[key]);
            if (o == null) throw new KeyNotFoundException(ResourcePath + ".json 에 크기 «" + key + "» 가 없다");
            object v = o["rem"];
            if (!J.IsNum(v)) throw new KeyNotFoundException(ResourcePath + ".json «" + key + "» 에 rem 이 없다");
            float r = (float)J.Num(v);
            if (r <= 0f) throw new System.FormatException(ResourcePath + ".json «" + key + "» rem 은 0 보다 커야 한다");
            return r;
        }

        /// <summary>정본 크기를 기준 캔버스 px 로(rem × <see cref="PopupKit.Rem"/>).</summary>
        public static float Px(string key) { return Rem(key) * PopupKit.Rem; }

        /// <summary>종류 Micro 인 글자에 정본 크기를 건다 — 돌려주는 값은 px. Micro 하한(18)보다 작은 표값은 하한이 이긴다(§1).</summary>
        public static float Apply(TMP_Text t, string key)
        {
            float px = Px(key);
            float min = UiCatalog.Instance.Kind(TextKind.Micro).min;
            if (px < min) px = min;
            t.fontSize = px;
            return px;
        }
    }
}
