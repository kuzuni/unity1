using System;
using System.Collections.Generic;
using UnityEngine;
using Forge.Core.Data;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T378 7회차 — 프로필·설정 팝업의 곁 표(`Resources/ProfileUi.json`). «표값 × 박힌 상수» 축에서 걷은 자리가
    /// 새 키를 필요로 하는데 `catalog.json` 이 남의 lock 이라 T65·T364 꼴로 곁 표에 둔다.
    /// 값은 앱 높이 비율(`_h`)이다 — `catalog.json` 의 `settings_toggle_h` 와 같은 단위.
    /// </summary>
    public static class ProfileUi
    {
        public const string ResourcePath = "ProfileUi";

        static JsonObject layout;

        static void Load()
        {
            if (layout != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T378)");
            layout = J.Obj(MiniJson.ParseObject(ta.text)["layout"]);
            if (layout == null || layout.Count == 0) throw new InvalidOperationException(ResourcePath + ".json 에 layout 이 없다");
        }

        /// <summary>표의 비율 원문. 없으면 던진다 — 조용히 0 으로 물러나면 자리가 무너진 채 초록이 된다.</summary>
        public static float L(string key)
        {
            Load();
            object v = layout[key];
            if (!J.IsNum(v)) throw new KeyNotFoundException(ResourcePath + ".json 에 배치 값 «" + key + "» 이 없다");
            return (float)J.Num(v);
        }

        /// <summary>`_h` 꼬리 = 앱 높이 비율 — 기준 높이(<see cref="UiKit.RefH"/>)로 환산한 px.</summary>
        public static float H(string key) { return L(key) * UiKit.RefH; }

        /// <summary>자용.</summary>
        public static void Reset() { layout = null; }
    }
}
