using System;
using System.Collections.Generic;
using UnityEngine;
using Forge.Core.Data;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T131 사람 표시 아이콘(성별 · 클랜 배지)의 치수표(<c>Assets/Forge/Resources/PersonIconsUi.json</c> — 정본 `style.css` `.profile-field .ico` · `.chat-name-line .chat-gender/.chat-clan`).
    /// T87 lock 이 <c>catalog.json</c> 을 쥐고 있어 T131 몫은 이 파일이 든다(T65 <see cref="PlayerInfoStyle"/> · T111 <see cref="GearDetailStyle"/> 과 같은 꼴 · 결정 285 · T33 이 합칠 수 있다). 코드에 숫자를 박지 않는다(§1).
    /// «어디에 무엇을» 은 <see cref="Forge.Core.Meta.Chat.GenderIcon"/>·<see cref="Forge.Core.Meta.Chat.ClanBadge"/>(정본 `chatNameIcons` 순수식).
    /// </summary>
    public static class PersonIcons
    {
        public const string ResourcePath = "PersonIconsUi";
        static JsonObject root, layout;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T131)");
            root = MiniJson.ParseObject(ta.text);
            layout = J.Obj(root["layout"]);
        }

        public static void Reset() { root = null; layout = null; }

        /// <summary>배치 값 원문(접미 _w 앱 폭 분수 · _h 앱 높이 분수 · _em 글자 크기 배수).</summary>
        public static float L(string key)
        {
            Load();
            object v = layout[key];
            if (!J.IsNum(v)) throw new KeyNotFoundException("PersonIconsUi.json 에 배치 값 «" + key + "» 이 없다");
            return (float)J.Num(v);
        }

        /// <summary>키 접미에 맞춰 기준 px 로(_w 앱 폭 · _h 앱 높이 · _em 은 <paramref name="fontSize"/> 배수).</summary>
        public static float Px(string key, float fontSize = 0f)
        {
            float v = L(key);
            if (key.EndsWith("_w")) return v * UiKit.RefW;
            if (key.EndsWith("_h")) return v * UiKit.RefH;
            if (key.EndsWith("_em")) return v * fontSize;
            return v;
        }
    }
}
