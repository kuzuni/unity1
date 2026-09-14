using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 맵 위 이정표 버튼 둘(ROUTINE T139 · 정본 <c>index.html</c> 83 <c>#waypoint-mystery</c> · 86 <c>#waypoint-pass</c>): 무대 띠(<c>#game-area</c> = 상단바 아래 ~ 장비 시트 위)
    /// 오른쪽에 그림만 떠 있고(정본 주석 «상자 없이 맵 위에 그림만» · 그림자만 그림에), 미스터리 상자 아래엔 다음 09:00 까지의 카운트다운(정본 매초 tick).
    /// 누르면 정본대로 미스터리 = 준비 중 팝업(<c>openStub</c>) · 패스 = 진행 패스(<c>openPass</c>). 층은 HUD **아래**(정본 <c>.waypoint</c> z3 &lt; <c>#topbar</c> z5).
    /// 리그 보상 이정표는 정본이 지웠다(주인 지시 2026-08-19) — 옮기지 않는다. 수치는 전부 <c>Resources/WaypointsUi.json</c> · 셈은 Core <see cref="WaypointsRules"/>.
    /// 정본 <c>drop-shadow</c> 의 번짐(.1rem)은 uGUI <see cref="Shadow"/> 가 못 그려 오프셋·색만 옮긴다(결정 기록).
    /// </summary>
    public sealed class Waypoints : MonoBehaviour
    {
        public static Waypoints Instance { get; private set; }
        /// <summary>층(정본 <c>#game-area</c> 자리 · 무대 띠).</summary>
        public RectTransform Layer { get; private set; }
        /// <summary>이정표 버튼 — id(정본 DOM id) → 버튼.</summary>
        public readonly Dictionary<string, Button> Buttons = new Dictionary<string, Button>(StringComparer.Ordinal);
        /// <summary>카운트다운 글자 — id → 글자(카운트다운이 있는 이정표만).</summary>
        public readonly Dictionary<string, TextMeshProUGUI> Times = new Dictionary<string, TextMeshProUGUI>(StringComparer.Ordinal);
        /// <summary>마지막으로 쓴 카운트다운 문구 · 마지막 탭 · 틱 수(테스트).</summary>
        public string LastCountdown { get; private set; }
        public string LastTap { get; private set; }
        public int Ticks { get; private set; }

        static WaypointsSpec spec;
        public static WaypointsSpec Spec { get { if (spec == null) spec = WaypointsSpec.From(WaypointsStyle.Root); return spec; } }

        double sinceMs;

        /// <summary>층을 세운다(없으면) — <c>UiRoot.Build</c> 가 HUD 다음에 부른다. 앱 상자 안 무대 띠, HUD 층 바로 아래 형제.</summary>
        public static Waypoints Ensure(UiRoot root)
        {
            if (Instance != null) return Instance;
            if (root == null || root.App == null) return null;
            WaypointsSpec s = Spec;
            float rem = PopupKit.Rem;
            RectTransform layer = UiKit.Box(root.App, WaypointsStyle.T("layer"));
            UiKit.Band(layer, UiKit.L("topbar_h"), UiKit.L("sheet_top"));
            if (root.HudLayer != null) layer.SetSiblingIndex(root.HudLayer.GetSiblingIndex());   // 정본 z3 — HUD(z5)·팝업 아래
            Waypoints w = layer.gameObject.AddComponent<Waypoints>();
            w.Layer = layer;
            foreach (WaypointSpec p in s.Points) w.BuildOne(s, p, rem);
            Instance = w;
            w.Refresh();
            return w;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        void BuildOne(WaypointsSpec s, WaypointSpec p, float rem)
        {
            float icon = (float)s.IconRem * rem;
            Button b = UiKit.Button(Layer, p.Id, () => OnTap(p));
            RectTransform rt = (RectTransform)b.transform;
            // 정본 .waypoint: absolute · top/right % · column flex · align center · gap .15rem → 오른쪽 위 모서리를 (1−right, 1−top) 에, 안은 위에서 아래로 가운데 정렬
            UiKit.Anchor(rt, new Vector2(1f - (float)p.RightF, 1f - (float)p.TopF), Vector2.one, Vector2.zero, icon, icon);
            RectTransform iconBox = UiKit.Box(rt, WaypointsStyle.T("icon"));
            UiKit.Anchor(iconBox, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, icon, icon);
            Image img = UiKit.Icon(iconBox, "ico", p.Icon);    // 정본 WP_ICON — T31 아틀라스(wp_mystery · power) · .ico 는 칸 100%
            UiKit.Fill(img.rectTransform);
            Shadow sh = img.gameObject.AddComponent<Shadow>();  // 정본 filter: drop-shadow(0 .08rem .1rem rgba(0,0,0,.38)) — 번짐은 못 그린다
            sh.effectColor = WaypointsStyle.C("icon_shadow");
            sh.effectDistance = new Vector2(0f, -(float)s.ShadowDyRem * rem);
            sh.useGraphicAlpha = true;
            if (p.Countdown)
            {
                Image badge = UiKit.Rounded(rt, WaypointsStyle.T("time"), "toast_bg", (float)s.TimeRadiusRem * rem);
                badge.color = WaypointsStyle.C("time_bg");
                badge.raycastTarget = false;
                UiKit.Anchor(badge.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -(icon + (float)s.GapRem * rem)), icon, 0f);
                // 정본 .6rem · 800 — 크기는 §1 하한 아래라 종류(Sub)로(T136 이 작은 종류를 더하면 표 time_font_rem 으로)
                TextMeshProUGUI t = UiKit.Text(badge.transform, "text", TextKind.Sub, string.Empty, null, TextAlignmentOptions.Center);
                t.color = WaypointsStyle.C("time_ink");
                t.fontStyle = FontStyles.Bold;
                t.raycastTarget = false;
                UiKit.Fill(t.rectTransform);
                Times[p.Id] = t;
            }
            Buttons[p.Id] = b;
        }

        /// <summary>카운트다운을 다시 쓰고(정본 tick) 알약·버튼 크기를 글자에 맞춘다 — 상자 폭은 max(아이콘, 알약)(정본 flex column · align center).</summary>
        public void Refresh()
        {
            WaypointsSpec s = Spec;
            float rem = PopupKit.Rem;
            float icon = (float)s.IconRem * rem;
            string txt = PopupKit.FmtTime(WaypointsRules.CountdownSec(DateTime.Now));
            LastCountdown = txt;
            foreach (KeyValuePair<string, TextMeshProUGUI> kv in Times)
            {
                TextMeshProUGUI t = kv.Value;
                t.text = txt;
                Vector2 pv = t.GetPreferredValues();
                float bw = pv.x + 2f * (float)s.TimePadXRem * rem, bh = pv.y;
                RectTransform badge = (RectTransform)t.transform.parent;
                badge.sizeDelta = new Vector2(bw, bh);
                RectTransform btn = (RectTransform)badge.parent;
                btn.sizeDelta = new Vector2(Mathf.Max(icon, bw), icon + (float)s.GapRem * rem + bh);
            }
        }

        private void Update()
        {
            sinceMs += Time.unscaledDeltaTime * 1000.0;
            if (!WaypointsRules.TickDue(Spec, sinceMs)) return;
            sinceMs = 0;
            Ticks++;
            Refresh();
        }

        void OnTap(WaypointSpec p)
        {
            MetaHost h = MetaHost.Instance;
            if (h == null) return;
            LastTap = p.Id;
            if (p.Tap == WaypointsSpec.TapPass) h.OpenPass();                                                   // 정본 UI.openPass()
            else h.OpenStub(WaypointsStyle.T("stub_title"), WaypointsStyle.T("stub_desc"));                     // 정본 onWaypointMystery → openStub
        }
    }

    /// <summary><c>Resources/WaypointsUi.json</c> — 색·문구(수치는 <see cref="WaypointsSpec"/> 가 같은 표에서 읽는다).</summary>
    public static class WaypointsStyle
    {
        public const string ResourcePath = "WaypointsUi";
        static JsonObject root, colors, text;
        static readonly Dictionary<string, Color> colorCache = new Dictionary<string, Color>();

        public static JsonObject Root { get { Load(); return root; } }

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T139)");
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
            if (hex == null) throw new KeyNotFoundException("WaypointsUi.json 에 색 «" + key + "» 이 없다");
            if (!ColorUtility.TryParseHtmlString(hex, out c)) throw new FormatException("색 «" + key + "» 의 값 «" + hex + "» 을 못 읽는다");
            colorCache[key] = c;
            return c;
        }

        public static string T(string key)
        {
            Load();
            string s = J.Str(text[key]);
            if (s == null) throw new KeyNotFoundException("WaypointsUi.json 에 문구 «" + key + "» 이 없다");
            return s;
        }
    }
}
