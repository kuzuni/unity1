using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 전투 전리품 레인(정본 `index.html` 92 `#loot-feed` · `ui.js` 1371 `floatLoot(text)` · T138): 무대 오른쪽 아래(right .6rem · bottom 3.4rem)에
    /// 줄이 아래에서 위로 쌓이고, 6 줄을 **넘으면** 맨 위를 버리며, 각 줄은 아이콘+글자(`paintIconText` = <see cref="UiKit.IconTextRow"/>)로 1.6초 `lootpop` 뒤 사라진다.
    /// 층은 정본 `#game-area` 안(무대 띠 = 상단바 아래 ~ 장비 시트 위 · z 4). 수치는 전부 `Resources/LootFeedUi.json` · 셈은 Core <see cref="LootFeedRules"/>.
    /// 호출(정본 `combat.js` 450 코인 · 454 보스 해머 — 클론은 `BattleScene` 의 Loot 갈래가 영웅 머리 위 숫자로 대신하던 자리)은 T135 lock 뒤 2회차.
    /// 글자 크기: 정본 .78rem 은 §1 보조 하한(36px) 아래라 종류(Sub) 크기로 선다 — T136 이 작은 종류를 더하면 표 `font_rem` 으로 바꾼다.
    /// </summary>
    public sealed class LootFeed : MonoBehaviour
    {
        public static LootFeed Instance { get; private set; }
        /// <summary>레인 상자(정본 `#loot-feed` · 무대 띠의 오른쪽 아래 · 아래에서 위로 쌓임).</summary>
        public RectTransform Lane { get; private set; }
        /// <summary>지금 보이는 줄 수 · 마지막 호출들의 문구(버려진 줄 포함 · 테스트).</summary>
        public int Lines { get { return Lane == null ? 0 : Lane.childCount; } }
        public readonly List<string> Pushed = new List<string>();
        public int Dropped { get; private set; }

        static LootFeedSpec spec;
        public static LootFeedSpec Spec { get { if (spec == null) spec = LootFeedSpec.From(LootFeedStyle.Root); return spec; } }

        /// <summary>층을 세운다(없으면) — `UiRoot.App` 안, 무대 띠(상단바 아래~장비 시트 위)와 같은 자리 · 전투 오버레이가 있으면 그 바로 위 형제.</summary>
        public static LootFeed Ensure()
        {
            if (Instance != null) return Instance;
            UiRoot root = UiRoot.Instance;
            if (root == null || root.App == null) return null;
            LootFeedSpec s = Spec;
            float rem = PopupKit.Rem;
            RectTransform layer = UiKit.Box(root.App, LootFeedStyle.T("layer"));
            UiKit.Band(layer, UiKit.L("topbar_h"), UiKit.L("sheet_top"));
            BattleOverlay ov = BattleOverlay.Instance;
            if (ov != null) layer.SetSiblingIndex(ov.transform.GetSiblingIndex() + 1);
            else if (root.HudLayer != null) layer.SetSiblingIndex(root.HudLayer.GetSiblingIndex() + 1);
            LootFeed f = layer.gameObject.AddComponent<LootFeed>();
            // 레인: 오른쪽 아래 앵커 · 피벗 오른쪽 아래 → 줄이 늘면 위로 자란다(정본 flex-direction: column · align-items: flex-end)
            RectTransform lane = UiKit.Box(layer, "lane");
            float w = layer.rect.width > 0 ? layer.rect.width * (float)s.LaneWF : UiKit.RefW * (float)s.LaneWF;
            UiKit.Anchor(lane, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-(float)s.RightRem * rem, (float)s.BottomRem * rem), w, 0f);
            VerticalLayoutGroup col = lane.gameObject.AddComponent<VerticalLayoutGroup>();
            col.childAlignment = TextAnchor.LowerRight;
            col.childControlWidth = false; col.childControlHeight = false;
            col.childForceExpandWidth = false; col.childForceExpandHeight = false;
            col.spacing = (float)s.GapRem * rem;
            ContentSizeFitter fit = lane.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            f.Lane = lane;
            Instance = f;
            return f;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>정본 `UI.floatLoot(text)` — 돌아오는 값 = 붙인 뒤 줄 수(층을 못 세우면 0).</summary>
        public static int Push(string text)
        {
            LootFeed f = Ensure();
            if (f == null) return 0;
            return f.Add(text);
        }

        int Add(string text)
        {
            LootFeedSpec s = Spec;
            float rem = PopupKit.Rem;
            if (LootFeedRules.DropFirst(s, Lane.childCount) && Lane.childCount > 0) { Destroy(Lane.GetChild(0).gameObject); Lane.GetChild(0).SetParent(null, false); Dropped++; }
            Pushed.Add(text);
            RectTransform line = UiKit.Box(Lane, LootFeedStyle.T("line"));
            // 알약 배경은 레이아웃 밖(배경) — PopupKit.MarkBackgrounds 와 같은 규칙
            Image bg = UiKit.Rounded(line, "bg", "toast_bg", (float)s.RadiusRem * rem);
            bg.color = LootFeedStyle.C("line_bg");
            bg.raycastTarget = false;
            bg.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            HorizontalLayoutGroup pad = line.gameObject.AddComponent<HorizontalLayoutGroup>();
            int px = Mathf.RoundToInt((float)s.PadXRem * rem), py = Mathf.RoundToInt((float)s.PadYRem * rem);
            pad.padding = new RectOffset(px, px, py, py);
            pad.childControlWidth = true; pad.childControlHeight = true;
            pad.childForceExpandWidth = false; pad.childForceExpandHeight = false;
            ContentSizeFitter fit = line.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            RectTransform row = UiKit.IconTextRow(line, "row", TextKind.Sub, text, "ink", TextAlignmentOptions.Right);
            foreach (TextMeshProUGUI piece in UiKit.RowTexts(row)) { piece.fontStyle = FontStyles.Bold; piece.color = LootFeedStyle.C("text"); piece.raycastTarget = false; }
            CanvasGroup cg = line.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            StartCoroutine(Life(s, line, row, cg, rem));
            return Lane.childCount;
        }

        IEnumerator Life(LootFeedSpec s, RectTransform line, RectTransform row, CanvasGroup cg, float rem)
        {
            double ms = 0, end = LootFeedRules.LifeMs(s);
            Vector2 pos0 = row.anchoredPosition;
            while (ms < end && line != null)
            {
                double pct = Math.Min(100.0, ms / s.LifeMs * 100.0);
                double a, ty;
                LootFeedRules.LootPop(s, pct, out a, out ty);
                cg.alpha = (float)a;
                row.anchoredPosition = new Vector2(pos0.x, pos0.y - (float)(ty * rem));   // translateY(+) = 아래
                yield return null;
                ms += Time.unscaledDeltaTime * 1000.0;
            }
            if (line != null) Destroy(line.gameObject);
        }
    }

    /// <summary>`Resources/LootFeedUi.json` — 색·문구(수치는 <see cref="LootFeedSpec"/> 가 같은 표에서 읽는다).</summary>
    public static class LootFeedStyle
    {
        public const string ResourcePath = "LootFeedUi";
        static JsonObject root, colors, text;
        static readonly Dictionary<string, Color> colorCache = new Dictionary<string, Color>();

        public static JsonObject Root { get { Load(); return root; } }

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T138)");
            root = MiniJson.ParseObject(ta.text);
            colors = J.Obj(root["colors"]);
            text = J.Obj(root["text"]);
        }

        public static void Reset() { root = null; colorCache.Clear(); }

        public static Color C(string key)
        {
            Load();
            Color c;
            if (colorCache.TryGetValue(key, out c)) return c;
            string hex = J.Str(colors[key]);
            if (hex == null) throw new KeyNotFoundException("LootFeedUi.json 에 색 «" + key + "» 이 없다");
            if (!ColorUtility.TryParseHtmlString(hex, out c)) throw new FormatException("색 «" + key + "» 의 값 «" + hex + "» 을 못 읽는다");
            colorCache[key] = c;
            return c;
        }

        public static string T(string key)
        {
            Load();
            string s = J.Str(text[key]);
            if (s == null) throw new KeyNotFoundException("LootFeedUi.json 에 문구 «" + key + "» 이 없다");
            return s;
        }
    }
}
