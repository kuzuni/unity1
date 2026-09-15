using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.Forging;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 자동 제련 팝업(ROUTINE T19 · UI-SPEC 21~24 ④ · 원작 ui.js `openAutoForge`·`renderAutoForge`·`closeAutoForge`·`onToggleKeepAge`·`onToggleFilterSub`·`onToggleAutoFilterOn`·`onPickHammers`·`onToggleStopOnTarget` · shot-042950/043117):
    /// 유지 = 뽑을 수 있는 시대(0% 는 숨김) 막대 + 체크 · 필터 = 우측 토글 + 회색 pill 행(13종) · 한 번에 사용된 망치 수 = 검정 스피너(1~22+기술트리) · 목표 장비를 찾으면 제련 계속하기 체크 · 큰 파란 [시작]/[중지] · 카드 아래 빨간 ✕.
    /// </summary>
    public static class ForgeAutoPopup
    {
        public const string Name = "autoforge";
        static bool ddOpen;

        public static bool DropdownOpen { get { return ddOpen; } }

        public static void Open(ForgeHost h)
        {
            if (!h.AutoForgeUnlocked) { h.Meta.Toast("🔒 스테이지 2-10 도달 시 해금됩니다"); return; }
            h.Meta.Popups.Show(Name, null, true);   // T78 — 정본 `#autoforge-modal` z-index 40(딤이 탭바까지 · slug modal-dim-tabbar)
            Render(h);
        }

        public static void Close(ForgeHost h) { ddOpen = false; h.Meta.Popups.Hide(Name); }

        public static void ToggleDropdown(ForgeHost h) { ddOpen = !ddOpen; Render(h); }

        public static void Render(ForgeHost h)
        {
            Popup p = h.Meta.Popups.Find(Name);
            if (p == null) return;
            RectTransform root = PopupLayer.Clear(p);
            GameDefs d = h.Defs;
            AutoForgeConfig cfg = h.Engine.AutoForgeConfig();
            OrderedMap<double> probs = h.Engine.AgeProbsAt(h.Forge.ForgeLevel);
            float rem = PopupKit.Rem, W = UiKit.RefW, H = UiKit.RefH;
            // T339 — 여태 박혀 있던 `0.85`·`0.84` 를 정본 표로. style.css 4682
            //        `.af-card { width: min(calc(var(--app-w) * .7719), 23rem); height: calc(var(--app-h) * .8452) }`.
            //        min 을 그대로 옮긴다 — 기준 캔버스에서는 앞쪽이 이겨 77.19%W 다(클론은 83.70%W 였다 · 런 474 실측).
            float w = ForgeAutoStyle.CardW(W, rem), pad = rem * 0.9f;
            float inner = w - pad * 2f - PopupKit.Line3 * 2f;
            float cardH, cardY;
            PopupKit.FitBetweenBars(H * ForgeAutoStyle.L("card_h_f"), out cardH, out cardY);   // T78 — ✕ 가 탭바에 가리지 않게
            RectTransform card = PopupKit.Card(root, "card", w, cardH, "pp_paper", rem * 1.1f, "pp_line", cardY);
            TextMeshProUGUI title = UiKit.Text(card, "af-title", TextKind.Title, "자동 제련", "pp_ink");
            title.fontStyle = FontStyles.Bold;
            // T109 11회차 — 정본 style.css 3846 `h3.af-title { -webkit-text-stroke: .11em var(--pp-line) }`(5033 `.af-title 4px #fff` 는 특이도가 낮아 진다 · ui.js 2302 는 h3).
            UiKit.OutlinePx(title, "pp_line", KeylineUi.Em("sheet_title", title.fontSize));
            float th = PopupKit.FontSize(TextKind.Title) * 1.3f;
            UiKit.Place(title.rectTransform, pad, pad, inner, th);

            float bottomH = rem * 1.9f * 2f + UiKit.H("btn_h") * 1.9f + rem * 1.6f;
            float scrollTop = pad + th + rem * 0.4f;
            float scrollH = cardH - scrollTop - bottomH - pad - PopupKit.Line3 * 2f;
            RectTransform scrollBox = UiKit.Box(card, "af-scroll");
            UiKit.Place(scrollBox, pad, scrollTop, inner, scrollH);
            RectTransform content = PopupKit.ScrollList(scrollBox, "list", rem * 0.25f, 0f, rem * 0.1f, TextAnchor.UpperLeft);
            PopupKit.Label(content, "af-label", TextKind.Sub, "유지", "pp_ink", TextAlignmentOptions.Left);
            float barH = rem * 1.75f;
            int stars = h.AscendCount;
            for (int i = 0; i < d.Ages.Length; i++)
            {
                string age = d.Ages[i];
                if (probs.Get(age, 0) <= 0) continue;
                string a = age;
                ForgeUi.AgeBar(content, "af-age-" + age, inner, barH, d, age, NumFmt.PctTrim(probs.Get(age, 0)), null, stars, () => h.ToggleKeepAge(a), cfg.KeepAges.Contains(age), autoForge: true);   // T124 — 자동 제련 막대는 정본 마스크(왼쪽 30→50%)
            }
            RectTransform filterRow = PopupKit.Item(content, "af-filter-row", -1f, UiKit.H("settings_toggle_h") + rem * 0.3f);
            TextMeshProUGUI fl = UiKit.Text(filterRow, "label", TextKind.Sub, "필터", "pp_ink", TextAlignmentOptions.Right);
            fl.fontStyle = FontStyles.Bold;
            float tw = UiKit.H("settings_toggle_w");
            UiKit.Place(fl.rectTransform, 0f, 0f, inner - tw - rem * 0.5f, UiKit.H("settings_toggle_h") + rem * 0.3f);
            Button tg = PopupKit.Toggle(filterRow, "af-toggle", cfg.FilterOn, () => h.ToggleAutoFilterOn());
            UiKit.Place(tg.GetComponent<RectTransform>(), inner - tw, rem * 0.15f, tw, UiKit.H("settings_toggle_h"));
            if (cfg.FilterOn)
            {
                for (int i = 0; i < d.Substats.Count; i++)
                {
                    SubstatDef s = d.Substats[i];
                    string key = s.Key;
                    SubRow(content, h, s, cfg.FilterSubs.Contains(key), inner, barH, () => h.ToggleFilterSub(key));
                }
            }

            // ---- 하단: 망치 수 · 계속하기 · 시작 ----
            RectTransform bottom = UiKit.Box(card, "af-bottom");
            UiKit.Place(bottom, pad, cardH - bottomH - pad - PopupKit.Line3, inner, bottomH);
            float rowH = rem * 1.9f;
            TextMeshProUGUI hl = UiKit.Text(bottom, "hammers-label", TextKind.Sub, "한 번에 사용된 망치 수", "pp_ink", TextAlignmentOptions.Left);
            hl.fontStyle = FontStyles.Bold;
            UiKit.Place(hl.rectTransform, 0f, 0f, inner * 0.6f, rowH);
            float spW = inner * 0.36f;
            Button sp = UiKit.Button(bottom, "af-spinner", () => ToggleDropdown(h));
            RectTransform spRt = sp.GetComponent<RectTransform>();
            UiKit.Place(spRt, inner - spW, rem * 0.15f, spW, rowH - rem * 0.3f);
            // T345 — 정본 4783 `.af-spinner { border-radius: .45rem }`(표 `af_spinner_r_rem` · 전엔 .3rem)
            Image spf = RadiusUi.Rounded(spRt, "face", "pp_line", "af_spinner_r_rem");
            TextMeshProUGUI spt = UiKit.Text(spRt, "value", TextKind.Sub, NumFmt.Fmt(cfg.HammersPerBatch) + (ddOpen ? "  ▼" : "  ▲"), "stage_ink", TextAlignmentOptions.Right);
            spt.fontStyle = FontStyles.Bold;
            spt.rectTransform.offsetMax = new Vector2(-rem * 0.5f, 0f);
            TextMeshProUGUI cl = UiKit.Text(bottom, "continue-label", TextKind.Sub, "목표 장비를 찾으면 제련 계속하기", "pp_ink", TextAlignmentOptions.Left);
            cl.fontStyle = FontStyles.Bold;
            UiKit.Place(cl.rectTransform, 0f, rowH, inner * 0.8f, rowH);
            float cb = rowH * 0.65f;
            Button ck = UiKit.Button(bottom, "af-check-continue", () => h.ToggleStopOnTarget());
            RectTransform ckRt = ck.GetComponent<RectTransform>();
            UiKit.Place(ckRt, inner - cb, rowH + (rowH - cb) * 0.5f, cb, cb);
            ForgeUi.Tile(ckRt, "box", cfg.StopOnTarget ? Color.black : new Color(0.14f, 0.77f, 0.32f, 1f), Color.black, cb * 0.2f, PopupKit.Line);
            if (!cfg.StopOnTarget)
            {
                Image mk = PopupKit.IconOr(ckRt, "mark", "check");
                UiKit.Anchor(mk.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, cb * 0.8f, cb * 0.8f);
            }
            float bw = inner * 0.45f, bh = UiKit.H("btn_h") * 1.9f;
            Button start = PopupKit.Btn(bottom, "af-start", h.AutoOn ? "중지" : "시작", "pp_blue", "pp_blue_dk", () => h.OnToggleAutoForge(), bw, bh, "stage_ink", TextKind.Button, false, "af_start");   // T109 11회차 — 정본 5015 `.af-start { 4px #000 }`(공용 2px 대신)
            UiKit.Place(start.GetComponent<RectTransform>(), (inner - bw) * 0.5f, rowH * 2f + rem * 0.5f, bw, bh);

            if (ddOpen)
            {
                int max = h.HammerBatchMax();
                float itemH = rem * 1.4f;
                float listH = itemH * 6f;
                RectTransform dd = UiKit.Box(bottom, "af-dd-list");
                UiKit.Place(dd, inner - spW, rem * 0.15f - listH, spW, listH);
                // T345 — 정본 4791 `.af-dd-list { border-radius: .45rem }`(표 `af_dd_list_r_rem` · 전엔 .3rem)
                Image ddbg = RadiusUi.Rounded(dd, "bg", "pp_line", "af_dd_list_r_rem");
                ddbg.raycastTarget = true;
                RectTransform ddc = PopupKit.ScrollList(dd, "items", 0f, 0f, 0f);
                for (int n = 1; n <= max; n++)
                {
                    int pick = n;
                    Button ib = PopupKit.Btn(ddc, "n-" + n, n.ToString(), cfg.HammersPerBatch == n ? "pp_blue" : "pp_gray", cfg.HammersPerBatch == n ? "pp_blue_dk" : "pp_gray_dk", () => { ddOpen = false; h.PickHammers(pick); }, -1f, itemH, "stage_ink", TextKind.Sub);
                }
                ScrollRect sr = dd.GetComponent<ScrollRect>();
                int cur = (int)cfg.HammersPerBatch;
                if (sr != null && max > 6) sr.verticalNormalizedPosition = 1f - Mathf.Clamp01((cur - 3.5f) / (max - 6f));
            }
            PopupKit.XButton(card, () => Close(h));
        }

        static void SubRow(Transform parent, ForgeHost h, SubstatDef s, bool on, float w, float hgt, System.Action onClick)
        {
            float rem = PopupKit.Rem;
            RectTransform row = PopupKit.Item(parent, "af-sub-" + s.Key, w, hgt);
            Image face = UiKit.Rounded(row, "face", "pp_gray", hgt * 0.5f);
            face.color = new Color(0xd6 / 255f, 0xd6 / 255f, 0xd6 / 255f);
            float cb = hgt * 0.62f;
            RectTransform box = UiKit.Box(row, "check");
            UiKit.Place(box, rem * 0.5f, (hgt - cb) * 0.5f, cb, cb);
            ForgeUi.Tile(box, "box", on ? new Color(0.14f, 0.77f, 0.32f, 1f) : Color.black, Color.black, cb * 0.2f, PopupKit.Line);
            if (on)
            {
                Image mk = PopupKit.IconOr(box, "mark", "check");
                UiKit.Anchor(mk.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, cb * 0.8f, cb * 0.8f);
            }
            TextMeshProUGUI l = UiKit.Text(row, "label", TextKind.Sub, s.Label, "pp_ink", TextAlignmentOptions.Left);
            l.fontStyle = FontStyles.Bold;
            UiKit.Place(l.rectTransform, rem * 0.5f + cb + rem * 0.5f, 0f, w - cb - rem, hgt);
            Button b = row.gameObject.AddComponent<Button>();
            b.targetGraphic = face;
            b.onClick.AddListener(() => onClick());
        }
    }

    /// <summary>
    /// T339 — 자동 제련 카드 치수표(`Resources/ForgeAutoUi.json`). 값을 코드에 안 박는다(§1) ·
    /// `catalog.json` 이 남의 lock 일 때가 잦아 곁 표로 둔다(T177 <see cref="ForgeItemStyle"/> 과 같은 꼴).
    /// </summary>
    public static class ForgeAutoStyle
    {
        public const string ResourcePath = "ForgeAutoUi";
        static JsonObject root, layout;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new System.InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T339)");
            root = MiniJson.ParseObject(ta.text);
            layout = J.Obj(root["layout"]);
        }

        public static void Reset() { root = null; layout = null; }

        /// <summary>배치 값 원문(분수·rem — 접미가 곱할 기준을 말한다).</summary>
        public static float L(string key)
        {
            Load();
            object v = layout == null ? null : layout[key];
            if (!J.IsNum(v)) throw new KeyNotFoundException("ForgeAutoUi.json 에 배치 값 «" + key + "» 이 없다 (T339)");
            return (float)J.Num(v);
        }

        /// <summary>정본 `width: min(calc(var(--app-w) * .7719), 23rem)` 을 그대로 — 둘 중 작은 쪽이다.</summary>
        public static float CardW(float appW, float rem)
        {
            return Mathf.Min(L("card_w_f") * appW, L("card_w_max_rem") * rem);
        }
    }
}
