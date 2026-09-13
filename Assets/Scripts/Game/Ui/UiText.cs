using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Forge.Core.Data;
using Forge.Core.Ui;
using UnityEngine;
using UnityEngine.Networking;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 화면 문구의 이모지 → 아이콘 표(ROUTINE T89). 정본 `ui.js` 의 `TOAST_ICON` 을 `tools/export_data.js` 가 뽑은
    /// <c>StreamingAssets/data/ui-text.json</c> 을 읽어 <see cref="IconText"/> 에 넘긴다 — 표를 코드에 박지 않는다(§1).
    /// 읽기 규약은 <see cref="Audio.AudioBank"/> 와 같다(데스크톱·에디터는 File · WebGL/안드로이드는 UnityWebRequest).
    /// 표가 아직 안 읽혔으면 <see cref="Icon"/> 이 전부 null 을 주고, 그때는 문구가 **글자 그대로** 선다(정본에서 표에 없는 이모지와 같은 취급).
    /// </summary>
    public static class UiText
    {
        public const string File_ = "ui-text.json";

        private static Dictionary<string, string> table;

        /// <summary>표가 읽혔는가(테스트·디버그용).</summary>
        public static bool Loaded { get { return table != null; } }

        /// <summary>표 줄 수(0 = 아직 안 읽힘).</summary>
        public static int Count { get { return table == null ? 0 : table.Count; } }

        /// <summary>이모지(한 글자 또는 이형 선택자까지 두 글자) → 아이콘 이름. 없으면 null.</summary>
        public static string Icon(string key)
        {
            string v;
            return table != null && table.TryGetValue(key, out v) ? v : null;
        }

        /// <summary>테스트가 표를 직접 꽂는다(부팅을 안 기다리고).</summary>
        public static void Use(Dictionary<string, string> t) { table = t; }

        /// <summary>문자열 → 조각 목록(정본 `paintIconText`). 표가 없으면 통째로 글자 한 조각.</summary>
        public static List<IconRun> Split(string msg) { return IconText.Split(msg, Icon); }

        /// <summary>아이콘이 될 자리를 뺀 «글자만» — 글꼴 글리프 검사(T89 막이)가 이것을 본다.</summary>
        public static string TextOnly(string msg) { return IconText.TextOnly(msg, Icon); }

        /// <summary>부팅이 한 번 부른다(<see cref="Bootstrap"/> 아래 자립한 호스트가 없으므로 코루틴을 넘겨 받는다).</summary>
        public static IEnumerator Load()
        {
            if (table != null) yield break;
            string json = null;
            string p = Path.Combine(Application.streamingAssetsPath, "data", File_);
            if (System.IO.File.Exists(p)) json = System.IO.File.ReadAllText(p);
            else
            {
                using (var req = UnityWebRequest.Get(p))
                {
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success) json = req.downloadHandler.text;
                    else Debug.LogWarning("[UiText] " + p + ": " + req.error + " — 이모지가 글자로 남는다(T89)");
                }
            }
            if (json == null) yield break;
            Parse(json);
        }

        /// <summary>JSON 한 덩이 → 표(순수 · 테스트가 직접 부른다).</summary>
        public static void Parse(string json)
        {
            var root = MiniJson.ParseObject(json);
            var t = J.Obj(root["TOAST_ICON"]);
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in t) map[kv.Key] = J.Str(kv.Value);
            table = map;
        }
    }
}
