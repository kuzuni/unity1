using Forge.Core.Data;
using TMPro;
using UnityEngine;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T352 ⓐ — 글자 **굵기** 표(<c>Assets/Forge/Resources/TextWeightUi.json</c>).
    /// 정본 `style.css` 의 `font-weight` **233 선언 중 225 가 bold** 고 regular 로 남는 자리는 **여덟뿐**이다.
    /// 그래서 공장(<see cref="UiKit.Text"/>)의 **기본이 bold** 고, 이 표는 «regular 로 남는 자리» 만 쥔다 —
    /// 클론은 여태 **부호가 반대**였다(기본 regular + 27개 파일 120 자리에 `FontStyles.Bold` 손박음).
    /// ⚑ 굵기 단을 더 만들거나 Black 자면을 들이는 것이 **아니다**: 정본 **8624~8631** 이 스스로
    /// «웹폰트 금지라 폴백 sans 는 regular/bold **두 축**뿐 · 600·650 은 700 으로 반올림 · **한 단 내려가려면 500 이어야 한다**»
    /// 라고 적어 뒀다. 곧 400 넷·500 넷만 regular 고 600 이상은 전부 bold 다 — `FontStyles.Bold` on/off 가 옳은 이식이다.
    /// </summary>
    public static class TextWeightUi
    {
        public const string ResourcePath = "TextWeightUi";
        static JsonObject root, colorKeys, sites;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new System.InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T352)");
            root = MiniJson.ParseObject(ta.text);
            colorKeys = J.Obj(root["regular_color_keys"]);
            sites = J.Obj(root["regular_sites"]);
        }

        /// <summary>자용.</summary>
        public static void Reset() { root = null; colorKeys = null; sites = null; }

        /// <summary>그 **색 키**로 찍는 글자가 regular 인가 — 정본이 색과 굵기를 한 클래스에 묶어 둔 자리(`.muted` 657 · `.league-server` 8633).</summary>
        public static bool RegularByColor(string colorKey)
        {
            if (string.IsNullOrEmpty(colorKey)) return false;
            Load();
            return colorKeys != null && colorKeys.Has(colorKey);
        }

        /// <summary>표에 그 **자리 키**가 있는가(없는 키를 조용히 넘기지 않게 자가 본다).</summary>
        public static bool HasSite(string siteKey)
        {
            Load();
            return sites != null && sites.Has(siteKey);
        }

        /// <summary>색 키로는 안 걸리는 **이름난 자리**를 regular 로 되돌린다(`.rates-tip`·`.forge-item-cell small`·`.btn small`·`.age-tag small`).</summary>
        public static void Regular(TMP_Text t, string siteKey)
        {
            if (t == null) return;
            Load();
            if (sites == null || !sites.Has(siteKey))
                throw new System.Collections.Generic.KeyNotFoundException(ResourcePath + ".json 에 regular 자리 «" + siteKey + "» 이 없다");
            t.fontStyle &= ~FontStyles.Bold;
        }
    }
}
