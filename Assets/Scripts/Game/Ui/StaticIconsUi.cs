using System;
using System.Collections.Generic;
using UnityEngine;
using Forge.Core.Data;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T132 — 정본이 아이콘으로 그리는 정적 자리의 치수표(<c>Assets/Forge/Resources/StaticIconsUi.json</c> · 정본 `style.css` `.pass-sword`·`.ico.stub-ico(.wide)`).
    /// T87 lock 이 <c>catalog.json</c> 을 쥐고 있어 T132 몫은 이 파일이 든다(T111 <see cref="GearDetailStyle"/> 과 같은 꼴 · T33 이 합칠 수 있다). 코드에 숫자를 박지 않는다(§1).
    /// </summary>
    public static class StaticIconsUi
    {
        public const string ResourcePath = "StaticIconsUi";
        static JsonObject root, layout;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T132)");
            root = MiniJson.ParseObject(ta.text);
            layout = J.Obj(root["layout"]);
        }

        public static void Reset() { root = null; layout = null; }

        /// <summary>표 값 원문(접미 _rem = rem 배수 · _em = 글자 크기 배수).</summary>
        public static float L(string key)
        {
            Load();
            object v = layout[key];
            if (!J.IsNum(v)) throw new KeyNotFoundException("StaticIconsUi.json 에 값 «" + key + "» 이 없다");
            return (float)J.Num(v);
        }

        /// <summary>`_rem` 키를 기준 px 로(정본 rem × 카탈로그 rem_h).</summary>
        public static float Rem(string key) { return L(key) * PopupKit.Rem; }

        /// <summary>`_em` 키를 그 글자 크기의 px 로.</summary>
        public static float Em(string key, float fontSize) { return L(key) * fontSize; }
    }
}
