using System;
using System.Collections.Generic;
using UnityEngine;
using Forge.Core.Data;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T377 — 정본이 **선택자에만 리터럴로 못박은 면 색**(`style.css` 8692: «전역 `--pp-red` 는 안 건드린다 · 버튼 규칙에만 리터럴»)의 표.
    /// 그 자리는 전역 토큰(`pp_red`)이 아니라 **자리 전용 키**로 색을 받는다 — 값은 `Resources/PinnedColorUi.json` 이 쥐고
    /// `tools/check_pinned_colors.py` 가 정본 리터럴과 같은지 지킨다. `catalog.json` 이 남의 lock 이라 곁 표에 둔다(T65 꼴).
    /// </summary>
    public static class PinnedColorUi
    {
        public const string ResourcePath = "PinnedColorUi";

        static JsonObject colors;
        static readonly Dictionary<string, Color> cache = new Dictionary<string, Color>();

        static void Load()
        {
            if (colors != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T377)");
            JsonObject root = MiniJson.ParseObject(ta.text);
            colors = J.Obj(root["colors"]);
            if (colors == null || colors.Count == 0) throw new InvalidOperationException(ResourcePath + ".json 에 colors 가 없다");
        }

        /// <summary>표의 면 색. 없으면 던진다 — 조용히 토큰으로 물러나면 이 표가 있는 뜻이 없다.</summary>
        public static Color C(string key)
        {
            Load();
            Color c;
            if (cache.TryGetValue(key, out c)) return c;
            string hex = J.Str(colors[key]);
            if (hex == null) throw new KeyNotFoundException(ResourcePath + ".json 에 색 «" + key + "» 이 없다");
            if (!ColorUtility.TryParseHtmlString(hex, out c)) throw new FormatException(ResourcePath + ".json 의 «" + key + "» 이 색이 아니다: " + hex);
            cache[key] = c;
            return c;
        }

        /// <summary>자용.</summary>
        public static void Reset() { colors = null; cache.Clear(); }
    }
}
