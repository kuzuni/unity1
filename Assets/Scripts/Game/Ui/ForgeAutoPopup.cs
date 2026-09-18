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
            float w = ForgeAutoStyle.CardW(W, rem) - PopupKit.Line3 * 2f, pad = rem * 0.9f;   // T473 — 표값은 정본 CSS width(border-box) · Card 의 w 는 패딩 상자
            float inner = w - pad * 2f;
            // 정본 style.css 4673~4682: 이 카드만 공용 상한(--popup-h-max)을 벗기고 높이 84.52%H · 위끝 7.01%H(4675 주석 «카드 y7.01%H · 하단 91.75%H»)로 둔다 —
            // 하단이 탭바 위끝(90.25%H)보다 아래라 T78 의 FitBetweenBars 깎기(76.25%H · 위끝 8.65%H · 런 769)는 정본과 어긋난다. 닫기 버튼은 T346 이 이미 탭바 위 층에 띄우므로 깎을 까닭이 없다(T400).
            float outerH = H * ForgeAutoStyle.L("card_h_f");
            float cardH = outerH - PopupKit.Line3 * 2f;   // T465 — 표값은 원작 카드 몸(border-box) · Card 의 h 는 패딩 상자
            float cardY = H * (0.5f - ForgeAutoStyle.L("card_top_f")) - outerH * 0.5f;   // PopupKit.Card 는 가운데 앵커 · 양수 = 위
            RectTransform card = PopupKit.Card(root, "card", w, cardH, "pp_paper", rem * 1.1f, "pp_line", cardY);
            // 정본 `.af-card`(style.css 5030)는 그림자가 **둘**이다 — 주석 그대로 «공용 아래턱(0 .5rem 0)에
            // 은은한 앰비언트를 더해 팝업이 화면에서 떠 보이게». 아래턱은 위 `PopupKit.Card` 가 이미 깔았고
            // 여기서는 그 뒤에 흐린 겹 하나를 더 깐다(CSS 목록의 뒤쪽이 아래로 간다 — 나중에 깐 것이 더 뒤다).
            UiShadow.Drop(card, "afcard_drop", rem * 1.1f, -1f, -1f, PopupKit.Line3);   // T465·T473 — 그늘은 카드 몸(테 포함 · 사방)에
            TextMeshProUGUI title = UiKit.Text(card, "af-title", TextKind.Button, "자동 제련", "pp_ink");   // T391 ⓑ — 정본 4695 `.af-title { 1.12rem }` = 40.8px(5033 덮음 1.26rem = 45.9) → Button 44(전엔 Title 60)
            title.fontStyle = FontStyles.Bold;
            // T109 11회차 — 정본 style.css 3846 `h3.af-title { -webkit-text-stroke: .11em var(--pp-line) }`(5033 `.af-title 4px #fff` 는 특이도가 낮아 진다 · ui.js 2302 는 h3).
            UiKit.OutlinePx(title, "pp_line", KeylineUi.Em("sheet_title", title.fontSize));
            float th = PopupKit.FontSize(TextKind.Button) * 1.3f;
            UiKit.Place(title.rectTransform, pad, pad, inner, th);

            float bottomH = rem * 1.9f * 2f + ForgeAutoStyle.StartBtnH(rem) + rem * 1.6f;   // T378 13회차 — 버튼 높이는 정본 4816(하한·패딩) · 위아래 여백(1.5/2.09rem)은 이 축 밖이라 그대로 뒀다
            float scrollTop = pad + th + rem * 0.4f;
            float scrollH = cardH - scrollTop - bottomH - pad;   // T465 — cardH 가 이미 패딩 상자라 옛 테 보정을 걷었다
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
                RectTransform ageBar =                 ForgeUi.AgeBar(content, "af-age-" + age, inner, barH, d, age, NumFmt.PctTrim(probs.Get(age, 0)), null, stars, () => h.ToggleKeepAge(a), cfg.KeepAges.Contains(age), autoForge: true);   // T124 — 자동 제련 막대는 정본 마스크(왼쪽 30→50%)
                // T331 40회차 — 정본 4828 `.af-age-bar` 의 바깥 두 겹: 딱딱한 턱 `0 .12rem 0 rgba(0,0,0,.3)`(afagebar_lip) 위에 앰비언트 `0 .2rem .4rem rgba(0,0,0,.24)`(afagebar_drop).
                //   공장 `ForgeUi.AgeBar`(T415 lock)를 안 열고 부르는 쪽에서 · 막대 뿌리의 형제(무늬 층 = 형제 1 · AgePatternTests)를 안 흔들려고 첫 자식 «bar» 틀 안 맨 뒤에 둘 다 — CSS 는 앞 겹이 위라 턱을 먼저(뒤에 깐 앰비언트가 형제 0 이 된다).
                RectTransform ageBarBox = (RectTransform)ageBar.Find("bar");
                UiShadow.Drop(ageBarBox, "afagebar_lip", RadiusUi.Px("af_age_bar_r_rem"), inner, barH);
                UiShadow.Drop(ageBarBox, "afagebar_drop", RadiusUi.Px("af_age_bar_r_rem"), inner, barH);
            }
            RectTransform filterRow = PopupKit.Item(content, "af-filter-row", -1f, UiKit.H("settings_toggle_h") + rem * 0.3f);
            TextMeshProUGUI fl = UiKit.Text(filterRow, "label", TextKind.Sub, "필터", "pp_ink", TextAlignmentOptions.Right);
            fl.fontStyle = FontStyles.Bold;
            float tw = UiKit.H("settings_toggle_w");
            UiKit.Place(fl.rectTransform, 0f, 0f, inner - tw - rem * 0.5f, UiKit.H("settings_toggle_h") + rem * 0.3f);
            // T377 12회차 — 정본은 이 토글을 **설정 토글과 따로** 못박아 뒀다: 4761 `.af-toggle { background: #1e2a4a }` ·
            //   4768 `.af-toggle.on { background: #35d435 }` · 4766 `.af-toggle .knob { background: var(--pp-blue) }`.
            //   클론은 공용 `PopupKit.Toggle` 기본값(= 설정 토글 3107 의 `pp_gray`/`pp_blue`/흰 손잡이)으로 그려,
            //   켜짐이 **초록이 아니라 파랑**이라 설정 토글과 구별이 안 됐다. 자리 전용 키로 받는다(값은 카탈로그 · `check_pinned_colors` 가 지킨다).
            Button tg = PopupKit.Toggle(filterRow, "af-toggle", cfg.FilterOn, () => h.ToggleAutoFilterOn(),
                "af_toggle_on", "af_toggle", "af_toggle_knob");
            float tgH = UiKit.H("settings_toggle_h");
            UiKit.Place(tg.GetComponent<RectTransform>(), inner - tw, rem * 0.15f, tw, tgH);
            // T178 22회차 — 정본 4986 `.af-toggle { background-image: linear-gradient(180deg, rgba(0,0,0,.34) 0, rgba(0,0,0,0) 55%, rgba(255,255,255,.14) 100%) }`
            //   + 4990 `.af-toggle .knob { background-image: radial-gradient(circle at 34% 26%, …) }`. 정본 주석 4984 가 둘을 한 줄로 적었다 —
            //   «트랙은 **파인 홈**, 노브는 **광택 구슬**». 클론은 둘 다 민무늬 단색이라 토글이 평평한 스티커였다.
            //   ⚠ 이 겹은 **자동 제련 토글에만** 준다 — 공용 `PopupKit.Toggle` 은 설정 토글도 쓰는데 정본 3107 엔 이런 줄이 없다.
            //   그래서 도우미를 안 건드리고 **여기서** 그 두 칸에 얹는다(T377 12회차가 색을 인수로 연 것과 같은 가르기).
            RectTransform tgRt = tg.GetComponent<RectTransform>();
            Image tgFace = tgRt.Find("face").GetComponent<Image>();
            SurfaceArt.FillMasked(tgFace, "bg-grad", "af_toggle_track",
                tw - PopupKit.Line2 * 2f, tgH - PopupKit.Line2 * 2f, tgFace.color);
            float knobK = tgH - PopupKit.Line2 * 4f;
            Image knobFace = tgRt.Find("knob/face").GetComponent<Image>();
            SurfaceArt.FillMasked(knobFace, "bg-grad", "af_knob_gloss",
                knobK - PopupKit.Line * 2f, knobK - PopupKit.Line * 2f, knobFace.color);
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
            UiKit.Place(bottom, pad, cardH - bottomH - pad, inner, bottomH);   // T465 — 옛 테 보정을 걷었다
            float rowH = rem * 1.9f;
            TextMeshProUGUI hl = UiKit.Text(bottom, "hammers-label", TextKind.Sub, "한 번에 사용된 망치 수", "pp_ink", TextAlignmentOptions.Left);
            hl.fontStyle = FontStyles.Bold;
            UiKit.Place(hl.rectTransform, 0f, 0f, inner * 0.6f, rowH);
            float spW = inner * 0.36f;
            Button sp = UiKit.Button(bottom, "af-spinner", () => ToggleDropdown(h));
            RectTransform spRt = sp.GetComponent<RectTransform>();
            UiKit.Place(spRt, inner - spW, rem * 0.15f, spW, rowH - rem * 0.3f);
            // T345 — 정본 4783 `.af-spinner { border-radius: .45rem }`(표 `af_spinner_r_rem` · 전엔 .3rem)
            // T365 19회차 — 정본 4785 는 **면 `#17181a` + ol3 `--pp-line` 테** 두 겹이다. 클론은 `pp_line`(#000000) 판 **한 장**이라
            //   테가 없고 면까지 순검정이었다 — `pp_ink` 가 곧 정본의 #17181a 다.
            Image spf = RadiusUi.Outlined(spRt, "face", "pp_ink", "af_spinner_r_rem", PopupKit.Line3);
            // T178 20회차 — 정본 5005 의 **면 겹**(`background-image: linear-gradient(180deg, …)` · 표 SurfaceUi.json `af_spinner`).
            //   안쪽 림라이트·그늘(5007 앞 두 겹)과 바깥 턱(셋째 겹)은 T331·T355 가 이미 세웠고, 여태 없던 것은 이 «검정 금속 톤» 한 장이다.
            //   둥근 면이라 FillMasked(면에 Mask) — 모서리 밖으로 안 샌다. 바탕(pp_ink)은 표의 over_color 가 미리 섞는다.
            SurfaceArt.FillMasked(spf, "bg-grad", "af_spinner", spW, rowH - rem * 0.3f);
            // T331 26회차 — 정본 5007 의 **셋째 겹** `0 .12rem 0 rgba(0,0,0,.28)`(바깥 턱).
            //   앞의 둘은 안쪽이라 이미 서 있다(위 림라이트·아래 그늘 = `btn_lip` 관용구 갈래).
            //   면을 세운 **뒤**에 부른다 — 그늘의 둥근 모서리를 그 면에서 되읽는다(`RadiusOf`).
            UiShadow.Drop(spRt, "afspinner_lip");
            PressFx.Attach(sp.gameObject, spRt, "af_spinner", spf);   // T355 ⓔ — 정본 5009·5011 .af-spinner:active { translateY(.1rem) · .07s ease-out }
            TextMeshProUGUI spt = UiKit.Text(spRt, "value", TextKind.Sub, NumFmt.Fmt(cfg.HammersPerBatch) + (ddOpen ? "  ▼" : "  ▲"), "stage_ink", TextAlignmentOptions.Right);
            spt.fontStyle = FontStyles.Bold;
            UiKit.TextShadow(spt, "af_spinner");   // T333 14회차 — 정본 5005 `.af-spinner { text-shadow: 0 .07rem 0 rgba(0,0,0,.5) }`
            spt.rectTransform.offsetMax = new Vector2(-rem * 0.5f, 0f);
            TextMeshProUGUI cl = UiKit.Text(bottom, "continue-label", TextKind.Sub, "목표 장비를 찾으면 제련 계속하기", "pp_ink", TextAlignmentOptions.Left);
            cl.fontStyle = FontStyles.Bold;
            UiKit.Place(cl.rectTransform, 0f, rowH, inner * 0.8f, rowH);
            float cb = rowH * 0.65f;
            Button ck = UiKit.Button(bottom, "af-check-continue", () => h.ToggleStopOnTarget());
            RectTransform ckRt = ck.GetComponent<RectTransform>();
            UiKit.Place(ckRt, inner - cb, rowH + (rowH - cb) * 0.5f, cb, cb);
            // T396 10회차 — 정본 4722~4730 `.af-check { background: #17181a } .af-check.on { background: #17181a; color: #23c552 }`(주석 «켜진 상태도 배경은 검정 그대로 두고 ✓ 글리프만 초록 —
            //   상자를 통째로 초록으로 채우던 종전 구현은 원본과 다른 물건»). 클론이 바로 그 종전 구현(켜짐 상자 초록 + 흰 ✓)이었다 → 상자는 두 상태 다 af_check_face · ✓ 만 af_check_on_ink.
            Color ckFace = PinnedColorUi.C("af_check_face");
            Image ckf = ForgeUi.Tile(ckRt, "box", ckFace, Color.black, cb * 0.2f, PopupKit.Line);
            // T178 20회차 — 정본 4971 `.af-check { background-image: linear-gradient(180deg, rgba(255,255,255,.14) 0, rgba(255,255,255,0) 46%, rgba(0,0,0,.3) 100%) }`.
            //   바탕이 **런타임 색**(켜짐 검정 · 꺼짐 초록)이라 색을 넘겨 sRGB 로 미리 섞는다(T178 17회차 갈래 · 표에 over_color 를 안 적은 까닭).
            SurfaceArt.FillMasked(ckf, "bg-grad", "af_check", cb - PopupKit.Line * 2f, cb - PopupKit.Line * 2f, ckFace);
            PressFx.Attach(ck.gameObject, ckRt, "af_check", ckf);   // T355 ⓒ — 정본 4974·4982 .af-check:active { translateY(.06rem); brightness(1.12) · .07s }(계속하기 체크 = ui.js 2318)
            if (!cfg.StopOnTarget)
            {
                Image mk = PopupKit.IconOr(ckRt, "mark", "check");
                mk.color = PinnedColorUi.C("af_check_on_ink");   // T396 10회차 — 정본 4730 ✓ #23c552(ui.js 2318 tint)
                UiKit.Anchor(mk.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, cb * 0.8f, cb * 0.8f);
                IconShadow.Drop(mk, "af_check_on");   // T453 — 정본 4978 `.af-check.on { text-shadow: 0 .06rem .1rem rgba(0,0,0,.6) }`: ✓ 는 아이콘이라 사본 한 장을 번져 뒤에 깐다
            }
            float bw = inner * 0.45f, bh = ForgeAutoStyle.StartBtnH(rem);   // T378 13회차 — 정본 4816 `.af-start { padding: .6rem 0; min-height: 4.45rem }`(폭 45.4% 는 이 축 밖)
            Button start = PopupKit.Btn(bottom, "af-start", h.AutoOn ? "중지" : "시작", "pp_blue", "pp_blue_dk", () => h.OnToggleAutoForge(), bw, bh, "stage_ink", TextKind.Button, false, "af_start");   // T109 11회차 — 정본 5015 `.af-start { 4px #000 }`(공용 2px 대신)
            // T178 22회차 — 정본 5016 `.af-start { background-image: linear-gradient(180deg, …) }`: 위가 밝고 아래가 어두운 **솟은** 면
            //   (토글 트랙의 «파인 홈» 과 정확히 반대 방향이다). 아래턱·림라이트·그림자는 T331·T355 가 이미 세웠고 여태 없던 것이 이 한 장이다.
            //   면 바탕이 `pp_blue` 한 값이라 표가 `over_color` 로 미리 합성한다(af_spinner 와 같은 갈래).
            //   `PopupKit.Btn` 의 면은 좌우 `Line3`, 아래로 턱(`btn_lip`)만큼 들어간 칸이다(440~452행).
            float startLip = UiKit.H("btn_lip");
            SurfaceArt.FillMasked(start.transform.Find("face").GetComponent<Image>(), "bg-grad", "af_start",
                bw - PopupKit.Line3 * 2f, bh - PopupKit.Line3 * 2f - startLip);
            UiKit.Place(start.GetComponent<RectTransform>(), (inner - bw) * 0.5f, rowH * 2f + rem * 0.5f, bw, bh);
            // T331 40회차 — 정본 5019 `.af-start` 의 드리운 그림자 `0 .22rem .5rem rgba(20,60,140,.4)`(표 afstart_drop · 남색). 반지름은 버튼이 실제로 쓰는 «line» 의 것을 되읽는다.
            UiShadow.Drop(start.GetComponent<RectTransform>(), "afstart_drop", UiShadow.RadiusOf((RectTransform)start.transform.Find("line")), bw, bh);

            if (ddOpen)
            {
                int max = h.HammerBatchMax();
                float itemH = rem * 1.4f;
                float listH = itemH * 6f;
                RectTransform dd = UiKit.Box(bottom, "af-dd-list");
                // T388 6회차 — 정본 **4791~4795** `.af-dd-list { position: absolute; right: 0; … min-width: 5.4rem; flex-direction: column }`:
                //   드롭다운은 **내용만큼**(가장 넓은 단추) 넓되 **5.4rem 밑으로는 안 내려간다**. 항목이 숫자 한둘이라 사실상 그 하한이 곧 폭이다.
                //   클론은 폭을 **스피너 폭**(`spW`)에 묶어 7.24rem 으로 섰다 — 하한을 안 쥔 것이 아니라 **엉뚱한 것에 묶여** 있었다.
                //   오른끝은 정본 `right: 0` 대로 스피너 오른끝에 맞춘다(폭만 줄고 자리는 안 움직인다).
                //   ⚠ 여기서 «내용» 을 따로 재지 않는 까닭: 항목은 숫자 한두 자라 정본에서도 **하한이 곧 폭**이다.
                //     대신 자가 «항목 글자가 안 잘리는가» 를 지켜, 항목이 넓어지는 날 그 자가 먼저 빨개진다(§1 — 짐작한 여백을 코드에 박지 않는다).
                float ddW = rem * ForgeAutoStyle.L("af_dd_min_w_rem");
                UiKit.Place(dd, inner - ddW, rem * 0.15f - listH, ddW, listH);
                // T345 — 정본 4791 `.af-dd-list { border-radius: .45rem }`(표 `af_dd_list_r_rem` · 전엔 .3rem)
                // T365 19회차 — 정본 4793 도 스피너와 같은 두 겹(면 `#17181a` + ol3 테)이다.
                Image ddbg = RadiusUi.Outlined(dd, "bg", "pp_ink", "af_dd_list_r_rem", PopupKit.Line3);
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
            // T178 22회차 — 정본 4998 `.af-sub-row { background-image: linear-gradient(180deg, rgba(255,255,255,.75) 0, rgba(255,255,255,0) 48%, rgba(0,0,0,.07) 100%) }`.
            //   정본 주석 4996: «위 하이라이트/아래 그늘로 **얇은 카드 두께**». 안쪽 림라이트 둘과 바깥 그림자(4999)는 T331 26회차가 세웠고
            //   면 겹 한 장이 빠져 있었다. 바탕이 바로 위에서 코드가 준 색이라 그 색을 그대로 넘긴다.
            //   ⚠ 이 행은 `LayoutElement` 로 크기를 **예약만** 한 레이아웃 자식이라 이 프레임의 `rect` 가 0 이다(T331 27회차) — 크기를 직접 준다.
            SurfaceArt.FillMasked(face, "bg-grad", "af_sub_row", w, hgt, face.color);
            // T331 26회차 — 정본 4999 의 **셋째 겹** `0 .07rem .12rem rgba(0,0,0,.1)`.
            //   표에서 가장 옅은 자리지만 정본이 이 겹으로 «얇은 카드 두께» 를 만든다(주석 4996).
            //   ⚑ 27회차 — 이 행은 `LayoutElement` 로 크기를 **예약만** 한 레이아웃 자식이라 이 프레임의 `rect` 는 아직 0 이다.
            //     굽는 길이 그럴 때 조용히 빈손으로 돌아오므로(런 921 의 빨강) 크기를 직접 준다.
            UiShadow.Drop(row, "afsubrow_drop", hgt * 0.5f, w, hgt);
            float cb = hgt * 0.62f;
            RectTransform box = UiKit.Box(row, "check");
            UiKit.Place(box, rem * 0.5f, (hgt - cb) * 0.5f, cb, cb);
            ForgeUi.Tile(box, "box", PinnedColorUi.C("af_check_face"), Color.black, RadiusUi.Px("af_check_r_rem"), PopupKit.Line);   // T415 13회차 — 정본 4724 `.af-check { border-radius: .35rem }` 을 표에서(전엔 `cb * 0.2f` ≈ .22rem 이 박혀 있었다)   // T396 10회차 — 정본 4727·4730: 켜짐도 상자는 #17181a(초록 상자는 «종전 구현»)
            if (on)
            {
                Image mk = PopupKit.IconOr(box, "mark", "check");
                mk.color = PinnedColorUi.C("af_check_on_ink");   // T396 10회차 — ✓ 만 #23c552(ui.js 2289·2295 tint)
                UiKit.Anchor(mk.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, cb * 0.8f, cb * 0.8f);
                IconShadow.Drop(mk, "af_check_on");   // T453 — 정본 4978 `.af-check.on { text-shadow: 0 .06rem .1rem rgba(0,0,0,.6) }`: ✓ 는 아이콘이라 사본 한 장을 번져 뒤에 깐다
            }
            TextMeshProUGUI l = UiKit.Text(row, "label", TextKind.Sub, s.Label, "pp_ink", TextAlignmentOptions.Left);
            l.fontStyle = FontStyles.Bold;
            UiKit.Place(l.rectTransform, rem * 0.5f + cb + rem * 0.5f, 0f, w - cb - rem, hgt);
            Button b = row.gameObject.AddComponent<Button>();
            b.targetGraphic = face;
            b.onClick.AddListener(() => onClick());
            PressFx.Attach(row.gameObject, row, "af_sub_row", face);   // T355 ⓓ — 정본 5000·5002 .af-sub-row:active { translateY(.06rem); brightness(.97) · .07s } · 행은 레이아웃 자식이라 기준 자리는 누르는 순간 잡힌다
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

        /// <summary>T378 13회차 — 정본 **4816** `.af-start { padding: .6rem 0; min-height: 4.45rem }`: [시작]/[중지] 버튼 높이는
        /// **max(하한 4.45rem, 세로 패딩 × 2 + 한 줄)** 이다. 종전엔 표값 `btn_h`(2.4rem)에 코드가 `× 1.9` 를 얹어 흉내 냈다(§1 · 4.56rem).
        /// 값 둘은 곁 표(ForgeAutoUi.json) · 셈은 공용 <see cref="PopupKit.BtnH"/>(12회차의 두 줄 갈래와 같은 식).</summary>
        public static float StartBtnH(float rem)
        {
            return PopupKit.BtnH(TextKind.Button, L("af_start_pad_y_rem") * rem, null, L("af_start_min_h_rem") * rem, 1);
        }

        /// <summary>정본 `width: min(calc(var(--app-w) * .7719), 23rem)` 을 그대로 — 둘 중 작은 쪽이다.</summary>
        public static float CardW(float appW, float rem)
        {
            return Mathf.Min(L("card_w_f") * appW, L("card_w_max_rem") * rem);
        }
    }
}
