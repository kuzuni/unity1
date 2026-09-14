using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.Save;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 메인 화면 오프라인 보상 버튼(ROUTINE T133 · 정본 <c>index.html</c> 75 <c>#offline-btn</c>): 무대 띠 왼쪽 아래에 **상자 그림만** 떠 있고
    /// (정본 index.html 74 주석 «'오프라인' 글자는 없앴고 상자 그림만 남는다»), 상자 위로 <c>z z z</c> 셋이 솟아 오른쪽으로 흘러 사라진다.
    /// 보상이 쌓이면(<c>(now − lastOfflineClaim)/1000 ≥ 60</c> · 정본 <c>ui.js</c> 6085) 상자가 <c>ob-bob</c> 으로 들썩인다.
    /// 누르면 정본 <c>onClaimOffline</c>: 누적이 없으면 토스트만, 있으면 오프라인 팝업을 연다(지급은 팝업의 [수집] 이 한다 · 정본 ui.js 5930).
    /// 층은 HUD **아래**(정본 <c>#offline-btn</c> z4 &lt; <c>#topbar</c> z5) — T139 <see cref="Waypoints"/> 와 같은 자리·같은 꼴이다.
    /// 수치·문구는 전부 <c>Resources/OfflineButtonUi.json</c> · 셈은 Core <see cref="OfflineButtonRules"/>.
    /// 정본 <c>drop-shadow</c> 의 번짐(.12rem)은 uGUI <see cref="Shadow"/> 가 못 그려 오프셋·색만 옮긴다(T139 와 같은 판단).
    /// </summary>
    public sealed class OfflineButton : MonoBehaviour
    {
        public static OfflineButton Instance { get; private set; }

        /// <summary>버튼 자체(정본 <c>#offline-btn</c>).</summary>
        public Button Btn { get; private set; }
        /// <summary>상자 그림(정본 <c>.ob-chest</c>) — 들썩임이 이 칸에 걸린다.</summary>
        public RectTransform Chest { get; private set; }
        /// <summary>눌림 피드백(T355 · 표 `offline_chest`) — 들썩이는(ready) 동안은 정본처럼 animation 이 transform 을 쥐어 눌림이 안 보인다.</summary>
        public PressFx Press { get; private set; }
        /// <summary>졸음 글자 셋(정본 <c>.ob-zzz i</c>).</summary>
        public readonly List<TextMeshProUGUI> Zzz = new List<TextMeshProUGUI>();
        /// <summary>지금 보상이 쌓였는가(정본 <c>.ready</c>) · 마지막 탭 결과(테스트).</summary>
        public bool IsReady { get; private set; }
        public string LastTap { get; private set; }

        static OfflineButtonSpec spec;
        public static OfflineButtonSpec Spec { get { if (spec == null) spec = OfflineButtonSpec.From(OfflineButtonStyle.Root); return spec; } }

        double sinceMs, animMs;
        readonly List<RectTransform> zzzRt = new List<RectTransform>();

        /// <summary>버튼을 세운다(없으면) — <c>UiRoot.Build</c> 가 이정표 다음에 부른다.</summary>
        public static OfflineButton Ensure(UiRoot root)
        {
            if (Instance != null) return Instance;
            if (root == null || root.App == null) return null;
            OfflineButtonSpec s = Spec;
            float rem = PopupKit.Rem;
            float side = (float)s.BtnRem * rem;

            RectTransform layer = UiKit.Box(root.App, OfflineButtonStyle.T("btn") + "-layer");
            UiKit.Band(layer, UiKit.L("topbar_h"), UiKit.L("sheet_top"));
            if (root.HudLayer != null) layer.SetSiblingIndex(root.HudLayer.GetSiblingIndex());   // 정본 z4 — HUD(z5)·팝업 아래

            OfflineButton ob = layer.gameObject.AddComponent<OfflineButton>();
            Button b = UiKit.Button(layer, OfflineButtonStyle.T("btn"), ob.OnTap);
            RectTransform rt = (RectTransform)b.transform;
            // 정본 #offline-btn { position:absolute; bottom:.6rem; left:.5rem } — 왼쪽 아래 모서리 기준
            UiKit.Anchor(rt, Vector2.zero, Vector2.zero,
                         new Vector2((float)s.LeftRem * rem, (float)s.BottomRem * rem), side, side);

            RectTransform chest = UiKit.Box(rt, OfflineButtonStyle.T("chest"));
            UiKit.Fill(chest);
            Image img = UiKit.Icon(chest, "ico", OfflineButtonStyle.T("chest_icon"));   // 정본 paintStaticIcons → IconGen.img('chest')
            UiKit.Fill(img.rectTransform);
            Shadow sh = img.gameObject.AddComponent<Shadow>();   // 정본 filter: drop-shadow(0 .1rem .12rem rgba(0,0,0,.45))
            sh.effectColor = OfflineButtonStyle.C("chest_shadow");
            sh.effectDistance = new Vector2(0f, -(float)s.ChestShadowDyRem * rem);
            sh.useGraphicAlpha = true;

            // 정본 .ob-zzz { left:58%; bottom:62% } — 버튼 안 비율 자리에 폭 0 인 기준점을 두고 글자를 그 위에 얹는다
            RectTransform zzzRoot = UiKit.Box(rt, OfflineButtonStyle.T("zzz"));
            UiKit.Anchor(zzzRoot, new Vector2((float)s.ZzzLeftF, (float)s.ZzzBottomF),
                         new Vector2(0f, 0f), Vector2.zero, 0f, 0f);
            for (int i = 0; i < s.ZzzCount; i++)
            {
                // 정본 .78rem·900 — §1 하한 아래라 종류(Sub)로 선다(T136 이 작은 종류를 더하면 zzz_font_rem 으로)
                TextMeshProUGUI t = UiKit.Text(zzzRoot, "z" + i, TextKind.Sub, OfflineButtonStyle.T("zzz_letter"),
                                               null, TextAlignmentOptions.Center);
                t.color = OfflineButtonStyle.C("zzz_ink");
                t.fontStyle = FontStyles.Bold;
                t.raycastTarget = false;
                UiKit.OutlinePx(t, "pp_line", (float)s.ZzzLinePx);   // 정본 -webkit-text-stroke: 2.5px #000 — 색 키는 공용(catalog 는 산 lock)
                RectTransform trt = t.rectTransform;
                Vector2 pv = t.GetPreferredValues();
                UiKit.Anchor(trt, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, Mathf.Max(pv.x, 1f), Mathf.Max(pv.y, 1f));
                ob.Zzz.Add(t);
                ob.zzzRt.Add(trt);
            }

            ob.Btn = b;
            ob.Chest = chest;
            ob.Press = PressFx.Attach(b.gameObject, chest, "offline_chest");   // T355 — 정본 213·216 #offline-btn:active .ob-chest { translateY(.08rem) scale(.94) · .1s ease-out }
            Instance = ob;
            ob.Refresh();
            return ob;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>정본 6085 — 1초 틱마다 <c>ready</c> 를 다시 본다. 수령 직후(정본 5944)도 이 길로 꺼진다.</summary>
        public void Refresh()
        {
            SaveIo io = SaveIo.Instance;
            SaveState st = io != null ? SaveIo.State : null;
            IsReady = st != null && OfflineButtonRules.Ready(Spec, SaveIo.NowMs(), st.LastOfflineClaim);
        }

        private void Update()
        {
            OfflineButtonSpec s = Spec;
            float rem = PopupKit.Rem;
            double dt = Time.unscaledDeltaTime * 1000.0;
            animMs += dt;
            sinceMs += dt;
            if (OfflineButtonRules.TickDue(s, sinceMs)) { sinceMs = 0; Refresh(); }

            // 상자 — 보상이 쌓였을 때만 들썩인다(정본 #offline-btn.ready .ob-chest)
            if (Chest != null)
            {
                if (IsReady)
                {
                    double dy, sc;
                    OfflineButtonRules.Bob(s, animMs, out dy, out sc);
                    Chest.anchoredPosition = new Vector2(0f, (float)-dy * rem * -1f);
                    Chest.localScale = new Vector3((float)sc, (float)sc, 1f);
                }
                else if (Chest.localScale != Vector3.one && !(Press != null && Press.Active))
                {
                    Chest.anchoredPosition = Vector2.zero;
                    Chest.localScale = Vector3.one;
                }
                if (Press != null) Press.Suppressed = IsReady;   // T355 — 들썩임(animation)이 transform 을 쥔 동안은 눌림이 안 보인다(정본과 같다)
            }

            for (int i = 0; i < zzzRt.Count; i++)
            {
                ZzzFrame f = OfflineButtonRules.Zzz(s, animMs, i);
                RectTransform rt = zzzRt[i];
                rt.anchoredPosition = new Vector2((float)f.DxRem * rem, (float)f.DyRem * rem * -1f);
                rt.localScale = new Vector3((float)f.Scale, (float)f.Scale, 1f);
                rt.localRotation = Quaternion.Euler(0f, 0f, (float)-f.RotDeg);
                TextMeshProUGUI t = Zzz[i];
                Color c = t.color;
                c.a = (float)f.Alpha;
                t.color = c;
            }
        }

        /// <summary>정본 <c>onClaimOffline</c>(ui.js 5930): 누적이 없으면 토스트만 · 있으면 팝업을 연다(지급은 [수집]).</summary>
        void OnTap()
        {
            MetaHost h = MetaHost.Instance;
            if (h == null) return;
            OfflineReward r = SaveIo.Instance != null ? SaveIo.Instance.PendingOffline() : null;
            if (r == null)
            {
                LastTap = "empty";
                h.Toast(OfflineButtonStyle.T("toast_empty"));
                return;
            }
            LastTap = "open";
            OfflinePopup.Show(h, r);
        }
    }

    /// <summary><c>Resources/OfflineButtonUi.json</c> — 색·문구(수치는 <see cref="OfflineButtonSpec"/> 가 같은 표에서 읽는다).</summary>
    public static class OfflineButtonStyle
    {
        public const string ResourcePath = "OfflineButtonUi";
        static JsonObject root, colors, text;
        static readonly Dictionary<string, Color> colorCache = new Dictionary<string, Color>();

        public static JsonObject Root { get { Load(); return root; } }

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T133)");
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
            if (hex == null) throw new KeyNotFoundException("OfflineButtonUi.json 에 색 «" + key + "» 이 없다");
            if (!ColorUtility.TryParseHtmlString(hex, out c)) throw new FormatException("색 «" + key + "» 의 값 «" + hex + "» 을 못 읽는다");
            colorCache[key] = c;
            return c;
        }

        public static string T(string key)
        {
            Load();
            string s = J.Str(text[key]);
            if (s == null) throw new KeyNotFoundException("OfflineButtonUi.json 에 문구 «" + key + "» 이 없다");
            return s;
        }
    }
}
