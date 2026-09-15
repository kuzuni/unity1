using System;
using System.Collections.Generic;
using UnityEngine;
using Forge.Core.Data;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T364 — 채팅 화면에서 정본이 **앱 폭 비율**(`--app-w`)로 못 박은 틈·여백의 곁 표(`Resources/ChatUi.json`).
    /// 클론은 이 자리들을 `rem`(= 앱 높이 기준)으로 어림했는데, 그러면 화면비가 9:16 을 벗어나는 순간 가로가 통째로 틀어진다 —
    /// 이 작업의 논지 그대로다. `catalog.json` 이 남의 lock 이라 T65 꼴로 곁 표에 둔다.
    /// </summary>
    public static class ChatUi
    {
        public const string ResourcePath = "ChatUi";

        static JsonObject layout;

        static void Load()
        {
            if (layout != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T364)");
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

        /// <summary>`_w` 꼬리 = 앱 폭 비율 — 기준 폭(<see cref="UiKit.RefW"/>)으로 환산한 px.</summary>
        public static float W(string key) { return L(key) * UiKit.RefW; }

        /// <summary>자용.</summary>
        public static void Reset() { layout = null; }
    }
}
