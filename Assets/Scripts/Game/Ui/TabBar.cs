using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 하단 탭바(ROUTINE T18 · 원작 index.html #tabbar + ui.js onTabClick/switchTab/refreshTabX).
    /// 탭 순서·라벨·아이콘·종류는 카탈로그 <c>tabs</c> 가 쥔다: PVP · 던전 · 소환 · 퀘스트 · 상점 · **디버그**(정본 `index.html` 160~165 의 여섯 칸 그대로).
    /// ⚠ 종전 주석은 «디버그는 배포 탭바에서 숨긴다 — 원작 main.js» 였는데 **정본은 그 반대**를 적어 두었다(main.js 127~131 🚨:
    ///   «디버그 탭은 기본 노출이다 … 되돌리지 말 것 — 다시 숨기려면 사용자 지시가 한 번 더 있어야 한다»). 결정 52 → 497·T169 로 되돌렸다.
    /// «sheet» 탭(소환)은 흰 전체화면 시트를 토글하고, «popup» 탭(PVP·던전·퀘스트·상점·디버그)은 <see cref="OpenRequested"/> 로 뒤 작업(T21·T22)에 넘긴다.
    /// 열린 표면이 있는 탭은 빨간 ✕ 원이 된다(누르면 닫는다).
    /// </summary>
    public sealed class TabBar : MonoBehaviour
    {
        private sealed class Tab
        {
            public UiCatalog.TabEntry Entry;
            public Button Button;
            public Image Icon;
            public TextMeshProUGUI Label;
            public GameObject XMark;
            public GameObject Glow;      // T178 5회차 — 정본 `#tabbar button.active, #tabbar button.tab-x` 의 방사형 둘
            public GameObject FootGlow;
        }

        private readonly List<string> keys = new List<string>();
        private readonly Dictionary<string, Tab> tabs = new Dictionary<string, Tab>();
        private readonly Dictionary<string, RectTransform> panels = new Dictionary<string, RectTransform>();

        /// <summary>popup 탭을 눌렀다 — 인자는 탭 키(pvp · dungeon · quest · shop · debug). 뒤 작업이 팝업을 연다.</summary>
        public event Action<string> OpenRequested;
        /// <summary>시트 탭이 바뀌었다(null = 홈으로).</summary>
        public event Action<string> Switched;

        /// <summary>지금 열린 시트 탭(없으면 null = 홈 전투 화면).</summary>
        public string ActiveTab { get; private set; }
        /// <summary>빨간 ✕ 로 그려진 탭(없으면 null).</summary>
        public string XTab { get; private set; }

        public IReadOnlyList<string> Keys { get { return keys; } }

        public string Label(string key) { return tabs[key].Entry.label; }
        public bool IsX(string key) { return tabs[key].XMark.activeSelf; }
        public Button ButtonOf(string key) { return tabs[key].Button; }

        /// <summary>시트 탭의 패널(흰 페이지) — T20 이 여기에 내용을 세운다. popup 탭은 패널이 없다(null).</summary>
        public RectTransform Panel(string key)
        {
            RectTransform p;
            return panels.TryGetValue(key, out p) ? p : null;
        }

        public void Build(RectTransform band, RectTransform panelHost)
        {
            UiCatalog cat = UiCatalog.Instance;
            float line = UiKit.L("line_px");
            UiKit.Panel(band, "bg", "tabbar_bg");
            float bandH = (1f - UiKit.L("tabbar_top")) * UiKit.RefH;
            // T178 4회차 — 정본 8317 `#tabbar { background-image: … }` 겹 둘: 위 밝고 아래 어두운 밴드(180deg) + 아래 가장자리 1px 림(0deg). 테(line)·버튼 아래.
            SurfaceArt.Fill(band, "tabbar-grad", "tabbar_shade", UiKit.RefW, bandH);
            SurfaceArt.Fill(band, "tabbar-rim", "tabbar_rim", UiKit.RefW, bandH);
            UiKit.Line(band, "line", "topbar_line", line, true);

            int n = cat.Tabs.Count;
            float bw = UiKit.RefW / n;
            float icon = UiKit.H("tab_icon");
            float padTop = UiKit.H("tab_pad_top");
            float padBottom = UiKit.H("tab_pad_bottom");
            float labelH = cat.Kind(TextKind.Sub).size * 1.25f;
            float contentH = padTop + icon + labelH + padBottom;
            float yTop = (bandH - contentH) * 0.5f;
            float x = UiKit.H("tabx");
            float xShadow = UiKit.H("tabx_shadow");
            float line3 = UiKit.L("line3_px");

            for (int i = 0; i < n; i++)
            {
                UiCatalog.TabEntry e = cat.Tabs[i];
                string key = e.key;
                Tab t = new Tab { Entry = e };
                t.Button = UiKit.Button(band, "tab-" + key, () => OnTab(key));
                RectTransform rt = t.Button.GetComponent<RectTransform>();
                UiKit.Place(rt, i * bw, 0f, bw, bandH);

                t.Icon = UiKit.Icon(rt, "ico", e.icon);
                UiKit.Place(t.Icon.rectTransform, (bw - icon) * 0.5f, yTop + padTop, icon, icon);
                t.Label = UiKit.Text(rt, "label", TextKind.Sub, e.label, "tab_ink", TextAlignmentOptions.Center);
                t.Label.fontStyle = FontStyles.Bold;
                UiKit.Place(t.Label.rectTransform, 0f, yTop + padTop + icon, bw, labelH);

                RectTransform xm = UiKit.Box(rt, "tab-x");
                UiKit.Place(xm, (bw - x) * 0.5f, (bandH - x) * 0.5f, x, x);
                Image shadow = UiKit.Circle(xm, "shadow", "tabx_shadow");
                UiKit.Anchor(shadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -xShadow), x, x);
                UiKit.Circle(xm, "ring", "pp_line");
                Image face = UiKit.Circle(xm, "face", "tabx_bg");
                face.rectTransform.offsetMin = new Vector2(line3, line3);
                face.rectTransform.offsetMax = new Vector2(-line3, -line3);
                Image mark = UiKit.Icon(xm, "mark", "xmark");
                mark.color = UiKit.C("tabx_ink");
                float mk = x * UiKit.L("tabx_icon");
                UiKit.Anchor(mark.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, mk, mk);
                xm.gameObject.SetActive(false);
                t.XMark = xm.gameObject;

                // T178 5회차 — 정본 8326: 켜진 칸(또는 ✕ 칸)에만 노란 속빛 둘이 깔린다. 만들어 두고 껐다 켠다(굽기는 키·비율당 한 번).
                Image glow = SurfaceArt.Fill(rt, "tab-glow", "tab_active_glow", bw, bandH);
                Image foot = SurfaceArt.Fill(rt, "tab-footglow", "tab_active_foot", bw, bandH);
                glow.transform.SetAsFirstSibling();
                foot.transform.SetSiblingIndex(1);
                t.Glow = glow.gameObject; t.FootGlow = foot.gameObject;
                t.Glow.SetActive(false); t.FootGlow.SetActive(false);

                keys.Add(key);
                tabs[key] = t;

                if (e.kind == "sheet")
                {
                    RectTransform p = UiKit.Box(panelHost, "panel-" + key);
                    UiKit.Panel(p, "bg", "pp_paper");
                    UiKit.Line(p, "line", "pp_line", line3, true);
                    UiKit.Button(p, "hit", null);
                    p.gameObject.SetActive(false);
                    panels[key] = p;
                }
            }
            RefreshTabX();
        }

        /// <summary>탭을 눌렀다(원작 onTabClick). ✕ 상태면 닫기 · popup 탭은 홈으로 돌아간 뒤 팝업 요청 · sheet 탭은 토글.</summary>
        public void OnTab(string key)
        {
            Tab t;
            if (!tabs.TryGetValue(key, out t)) throw new ArgumentException("모르는 탭 «" + key + "»");
            if (XTab == key) { CloseOpened(); return; }
            if (t.Entry.kind == "popup")
            {
                Switch(null);
                Action<string> h = OpenRequested;
                if (h != null) h(key);
                return;
            }
            Switch(ActiveTab == key ? null : key);
        }

        /// <summary>popup 탭이 연 팝업이 살아 있는 동안 그 탭을 빨간 ✕ 로(원작 MODAL_TAB · refreshTabX). null 이면 지운다 — T22 가 팝업을 열고 닫을 때 부른다.</summary>
        public void SetPopupX(string key)
        {
            popupX = key;
            RefreshTabX();
        }

        private string popupX;

        /// <summary>시트 탭 전환(원작 switchTab). null 이면 홈.</summary>
        public void Switch(string tab)
        {
            ActiveTab = tab;
            popupX = null;
            foreach (KeyValuePair<string, RectTransform> kv in panels) kv.Value.gameObject.SetActive(kv.Key == tab);
            RefreshTabX();
            Action<string> h = Switched;
            if (h != null) h(tab);
        }

        /// <summary>열린 것을 닫고 홈으로.</summary>
        public void CloseOpened() { Switch(null); }

        private void RefreshTabX()
        {
            XTab = ActiveTab ?? popupX;
            foreach (KeyValuePair<string, Tab> kv in tabs)
            {
                bool isX = XTab == kv.Key;
                Tab t = kv.Value;
                t.Icon.gameObject.SetActive(!isX);
                t.Label.gameObject.SetActive(!isX);
                t.XMark.SetActive(isX);
                t.Label.color = UiKit.C(kv.Key == ActiveTab ? "tab_active" : "tab_ink");
                // 정본은 `.active` 와 `.tab-x` **둘 다**에 같은 겹을 준다(style.css 8325 선택자 두 개).
                bool lit = isX || kv.Key == ActiveTab;
                if (t.Glow != null) t.Glow.SetActive(lit);
                if (t.FootGlow != null) t.FootGlow.SetActive(lit);
            }
        }
    }
}
