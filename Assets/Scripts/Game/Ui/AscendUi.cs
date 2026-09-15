using System;
using System.Collections.Generic;
using UnityEngine;
using Forge.Core.Data;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T398 — 승천 팝업 제목 두 토막(정본 `ui.js` 5864 «⭐ 승천 <small class="muted">보유 별 합계 ⭐ N</small>»)의 크기 표
    /// (<c>Assets/Forge/Resources/AscendUi.json</c>). 작은 토막의 px 는 큰 토막 px × (<c>title_small_rem</c> / <c>title_rem</c>) — 정본 .78rem ↔ 1.15rem.
    /// `catalog.json` 이 남의 lock 이라 곁 표에 둔다(T65 꼴). 수치는 표에만 있다(§1).
    /// </summary>
    public static class AscendUi
    {
        public const string ResourcePath = "AscendUi";
        static JsonObject root;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T398)");
            root = MiniJson.ParseObject(ta.text);
        }

        /// <summary>표의 수. 없으면 던진다 — 기본값으로 가리면 표가 비어도 화면이 그럴싸해 자가 못 잡는다.</summary>
        public static float Num(string key)
        {
            Load();
            object v = root[key];
            if (!J.IsNum(v)) throw new KeyNotFoundException(ResourcePath + ".json 에 수 «" + key + "» 이 없다");
            return (float)J.Num(v);
        }

        /// <summary>작은 토막(`small.muted`) 크기 / 큰 토막(h3) 크기 — 정본 .78 / 1.15.</summary>
        public static float TitleSmallRatio()
        {
            float big = Num("title_rem"), small = Num("title_small_rem");
            if (big <= 0f || small <= 0f || small > big) throw new FormatException(ResourcePath + ".json — title_small_rem 은 0 보다 크고 title_rem 보다 작아야 한다");
            return small / big;
        }

        /// <summary>자용.</summary>
        public static void Reset() { root = null; }
    }
}
