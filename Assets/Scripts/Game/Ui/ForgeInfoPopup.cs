using System;
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
    /// 대장간 팝업 3종(ROUTINE T19 · UI-SPEC 21~24 · 원작 ui.js `openForgeInfo`·`renderForgeLevelView`(shot-042831)·`openForgeList`·`renderForgeListView`(shot-042905)·`openForgeDetail`·`renderForgeDetailView`·`closeForgeItemDetail`·`closeForgeInfo`):
    /// ① 확률 정보(시대 막대 10 · 현재%/다음% · 업그레이드/진행바+건너뛰기/최고 레벨/승천) ② 모든 장비의 목록(시대별 셀 격자 · 개별 확률) ③ 장비 상세(목록 위에 겹침).
    /// </summary>
    public static class ForgeInfoPopup
    {
        public const string Name = "forge-info", ItemName = "forge-item";

        /// <summary>T372 — 「모든 장비의 목록」 칸 아래 «0.0000%» 라벨의 글자 종류. 정본 `.forge-item-cell small { font-size: .56rem }`(style.css 790 · 기준 캔버스 20.4px)이라
        /// §1 하한 `Sub`(36)로 찍으면 1.76배가 되어 라벨이 칸 피치를 넘는다. 결정 633 대로 **새 종류를 만들지 않고** 하한의 예외 한 자리 `Micro`(18)를 쓴다(§1 셋째 자리).</summary>
        public const TextKind PctKind = TextKind.Micro;
        static string view = "level";
        static string detailAge, detailSlot;
        static int detailVariant;
        static string detailWtype;

        public static string View { get { return view; } }

        // T78 — 정본 `css/style.css` 3777~3784(slug `modal-dim-tabbar`)가 이름으로 가른 «ⓑ 진짜 팝업» 이라
        // 딤이 탭바까지 덮는다(`#forge-info-modal`·`#forge-item-modal` 이 그 id 목록에 있다 · z-index 40/42).
        // 탭바 ✕ 가 유일한 닫기 길인 «ⓐ 탭 화면»(상점·던전 목록·제작 비교·장비 상세…)은 그대로 둔다.
        public static void Open(ForgeHost h) { view = "level"; h.Meta.Popups.Show(Name, null, true); Render(h); }
        public static void OpenList(ForgeHost h) { view = "list"; h.Meta.Popups.Show(Name, null, true); Render(h); }
        public static void Close(ForgeHost h) { h.Meta.Popups.Hide(ItemName); h.Meta.Popups.Hide(Name); }
        public static void CloseItemDetail(ForgeHost h) { h.Meta.Popups.Hide(ItemName); }

        public static void OpenDetail(ForgeHost h, string age, string slot, int variant, string wtype)
        {
            detailAge = age; detailSlot = slot; detailVariant = variant; detailWtype = wtype;
            h.Meta.Popups.Show(ItemName, null, true);   // T78 — 정본 `#forge-item-modal` z-index 42(탭바 위)
            RenderDetail(h);
        }

        public static void Render(ForgeHost h)
        {
            if (view == "list") RenderListView(h); else RenderLevelView(h);
        }

        static string Pct(double p) { return NumFmt.PctTrim(p); }

        public static void RenderLevelView(ForgeHost h)
        {
            Popup p = h.Meta.Popups.Find(Name);
            if (p == null) return;
            RectTransform root = PopupLayer.Clear(p);
            GameDefs d = h.Defs;
            ForgeUpgrade info = h.UpgradeInfo();
            bool upgrading = h.Upgrading;
            OrderedMap<double> curP = h.Engine.AgeProbsAt(h.Forge.ForgeLevel);
            OrderedMap<double> nextP = info != null ? h.Engine.AgeProbsAt(h.Forge.ForgeLevel + 1) : null;
            float rem = PopupKit.Rem;
            // T339 — 정본 style.css 5043 `.fi-card { width: min(calc(var(--app-w) * .9), 22.9rem) }` — 그 줄 주석이 답이다: «원본 카드 77.19%W».
            //        기준 캔버스에서는 **뒤쪽(22.9rem)이 이겨** 그 값이 된다(`.9` 만 쓰면 90%W 로 한참 넓다). 클론은 박힌 0.85 로 83.70%W 였다.
            float w = ForgeInfoStyle.FiCardW(UiKit.RefW, rem);
            float pad = rem * 0.9f;
            float inner = w - pad * 2f - PopupKit.Line3 * 2f;
            // 높이: 정본 5048 `height: calc(var(--app-h) * .8104)`. 1회차엔 등재의 «원작 68.90%H» 와 어긋나 보류했는데,
            // 2회차에 원작 PNG(`ref/screens/shot-042831.png`)의 카드 왼쪽 안쪽 세로줄을 직접 재니 **밝은 판이 y85~789 = 79.9%H**
            // 로 이어졌다 — CSS 주석의 «px 80~793 = 81.04%H» 와 맞고 68.90 은 자의 측정 한계였다(§4 기록 참조).
            // 클론은 같은 자로 72.1%H(내용이 정하던 값)라 표대로 고정하면 원작 쪽으로 간다. 내용이 더 짧으므로 넘침도 없다.
            RectTransform card = PopupKit.Card(root, "card", w, UiKit.RefH * ForgeInfoStyle.L("fi_card_h_f"), "pp_paper", rem * 1.1f);
            PopupKit.Column(card, pad, rem * 0.3f);
            RectTransform head = PopupKit.Item(card, "head", -1f, PopupKit.FontSize(TextKind.Title2) * 1.25f);
            TextMeshProUGUI title = UiKit.Text(head, "title", TextKind.Title2, "확률 정보", "pp_ink");   // T404 ⓑ — 정본 5056 `.fi-title { 1.12rem }` = 40.8px → Title2 42(전엔 Title 60 · +47%)
            title.fontStyle = FontStyles.Bold;
            // T109 14회차 — 정본 style.css 3846 의 제목 묶음에 `h3.fi-title` 이 들어 있다(`-webkit-text-stroke: .11em var(--pp-line)` · ui.js 2054). 폭은 표(sheet_title).
            UiKit.OutlinePx(title, "pp_line", KeylineUi.Em("sheet_title", title.fontSize));
            float ib = rem * 1.5f;
            Button infoBtn = ForgeUi.InfoButton(head, "fi-info-btn", ib, () => OpenList(h));
            UiKit.Anchor(infoBtn.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-rem * 0.3f, 0f), ib, ib);
            PopupKit.Label(card, "sub", TextKind.Sub, "제련 확률", "pp_ink");
            RectTransform pills = PopupKit.Item(card, "pills", -1f, rem * 1.7f);
            // T388 4회차 — 정본 **5065~5069** `.fi-pill { display: inline-flex; … min-width: 5.5rem }`: 알약은 **내용만큼 좁아지되 5.5rem 밑으로는 안 내려간다**.
            //   클론은 이 자리를 `inner * 0.4`(카드 안쪽 폭의 40% = 8.31rem)로 **고정**해 정본 하한의 1.51배로 섰다 — 하한을 «폭» 으로 오해한 것이 아니라 아예 안 쥐고 있었다.
            float ph = rem * 1.7f;
            float pillMin = rem * ForgeInfoStyle.L("fi_pill_min_w_rem");
            RectTransform cp = ForgeUi.Pill(pills, "coin", "coin", NumFmt.Fmt(h.Wallet.Coins), pillMin, ph);
            RectTransform gp = ForgeUi.Pill(pills, "gem", "gem", NumFmt.Fmt(h.Wallet.Gems), pillMin, ph);
            float cw = PillWidth(cp, pillMin, ph), gw = PillWidth(gp, pillMin, ph);
            // 정본 `.fi-pills` 는 가운데 정렬이라 **줄 전체**를 가운데에 둔다(폭이 서로 달라질 수 있다).
            //   ⚠ 틈은 정본 5064 `gap: .8rem` 인데 클론은 1rem 이다 — 틈은 **T364 축**이라 이 회차에서 안 건드렸다(표 `_fi_pill_gap` 에 적었다).
            float gap = rem * 1f;
            float rowX = (inner - (cw + gap + gw)) * 0.5f;
            UiKit.Place(cp, rowX, 0f, cw, ph);
            UiKit.Place(gp, rowX + cw + gap, 0f, gw, ph);
            RectTransform lv = PopupKit.Item(card, "level-row", -1f, PopupKit.FontSize(TextKind.Sub) * 1.4f);
            TextMeshProUGUI lvt = UiKit.Text(lv, "text", TextKind.Sub, "레벨 " + h.Forge.ForgeLevel + "   ▶   " + (info != null ? "레벨 " + (h.Forge.ForgeLevel + 1) : "최고"), "pp_ink", TextAlignmentOptions.Right);
            lvt.fontStyle = FontStyles.Bold;
            float barH = rem * 1.75f;
            int stars = h.AscendCount;
            RectTransform rowsBox = PopupKit.Item(card, "rows", -1f, d.Ages.Length * (barH + rem * 0.18f));
            VerticalLayoutGroup vg = PopupKit.Column(rowsBox, 0f, rem * 0.18f);
            for (int i = 0; i < d.Ages.Length; i++)
            {
                string age = d.Ages[i];
                ForgeUi.AgeBar(rowsBox, "age-" + age, inner, barH, d, age, Pct(curP.Get(age, 0)), info != null ? Pct(nextP.Get(age, 0)) : "—", stars);
            }
            PopupKit.Spacer(card, rem * 0.8f);
            // T378 11회차 — 정본 5161 `.fi-upgrade { padding: .65rem 1.5rem; font-size: 1.05rem }` 은 높이를 안 준다: 두 줄 글(제목<br><small>)이 높이를 정한다
            //   → 세로 패딩(표 `fi_upgrade_pad_y_rem`) x 2 + Sub 두 줄의 글꼴 줄높이(정본 line-height 미선언 = normal). 전엔 `btn_h * 1.7f` 가 박혀 있었다(T378 임시 목록의 그 자리).
            float bh = ForgeInfoStyle.TwoLineBtnH(TextKind.Sub, "fi_upgrade_pad_y_rem", null);
            if (h.AscendReady)
            {
                Button asc = PopupKit.Btn(card, "fi-upgrade", "★ 승천\n대장간 Lv." + (h.Ascension != null ? h.Ascension.Table.ForgeLevel : h.Forge.ForgeLevel) + " 도달 · 이후 제작 장비 ★" + (h.AscendCount + 1), "pp_blue", "pp_blue_dk",
                    () => { if (h.OpenAscension != null) h.OpenAscension("forge"); else h.Meta.OpenStub("승천", "대장간 라인 승천은 T21 승천 화면에서"); }, inner * 0.7f, bh, "stage_ink", TextKind.Sub);
                TwoLine(asc);
            }
            else if (info == null) PopupKit.Label(card, "fi-upg-label", TextKind.Body, "대장간 최고 레벨", "pp_ink");
            else if (upgrading)
            {
                double remain = (h.Forge.UpgradeEndsAt.Value - h.Meta.NowMs) / 1000;
                PopupKit.Label(card, "fi-upg-label", TextKind.Body, "업그레이드 진행 중....", "pp_ink");
                RectTransform prog = PopupKit.Item(card, "fi-prog", -1f, rem * 1.6f);
                // 정본 694 `.upg-progress { border-radius: .55rem }` — 클론은 .5 였다(T345 21회차 · 표 upg_progress_r_rem)
                float progR = RadiusUi.Px("upg_progress_r_rem");
                Image track = UiKit.Rounded(prog, "track", "pp_line", progR);
                double frac = Math.Min(1, Math.Max(0, 1 - remain / h.Engine.UpgradeTime(info)));
                Image fill = UiKit.Rounded(prog, "upg-fill", "pp_blue", progR - PopupKit.Line);
                fill.rectTransform.anchorMin = new Vector2(0f, 0f);
                fill.rectTransform.anchorMax = new Vector2((float)frac, 1f);
                fill.rectTransform.offsetMin = new Vector2(PopupKit.Line, PopupKit.Line);
                fill.rectTransform.offsetMax = new Vector2(-PopupKit.Line, -PopupKit.Line);
                TextMeshProUGUI tt = UiKit.Text(prog, "upg-time", TextKind.Sub, NumFmt.FmtTime(remain), "stage_ink");
                tt.fontStyle = FontStyles.Bold;
                PopupKit.Spacer(card, rem * 0.5f);
                // T110 — 정본 ui.js 2043 `건너뛰기<br><span class="fi-skip-gem">${IconGen.img('gem')} N</span>`: 아랫줄은 젬 **아이콘** + 수(세로 갈래 IconTextStack).
                // T109 14회차 — 정본 5150 `.fi-card .fi-skip { -webkit-text-stroke: 4px #000 }`: 회색 면은 공용 면 표(btn_face)에서 0 이라 이 자리는 제 키(fi_skip)를 넘긴다 — Btn 의 12번째 인자(자가 보는 자리)와 세로 갈래 조각 둘 다.
                // T378 11회차 — 정본 5151 `.fi-card .fi-skip { padding: .72rem 2.2rem; line-height: 1.25 }`: 이 버튼은 줄높이를 준 자리라 표 `fi_card_fi_skip_lh`(1.25)로 잰다.
                Button skip = PopupKit.Btn(card, "fi-skip", "", "pp_gray", "pp_gray_dk", () => h.OnGemSkipForge(), inner * 0.6f, ForgeInfoStyle.TwoLineBtnH(TextKind.Sub, "fi_skip_pad_y_rem", "fi_card_fi_skip_lh"), "stage_ink", TextKind.Sub, false, "fi_skip");
                IconTextStack.ReplaceLabel(skip, TextKind.Sub, "건너뛰기\n💎 " + NumFmt.Fmt(h.Engine.GemSkipCost()), "stage_ink", "pp_gray", "fi_skip");
            }
            else
            {
                double cost = h.Engine.UpgradeCost(info), time = h.Engine.UpgradeTime(info);
                bool poor = h.Wallet.Coins < cost;
                // T110 — 정본 ui.js 2047 `레벨 N 업그레이드<br><small>${IconGen.img('coin')} N · ⏱ T</small>`: 아랫줄은 코인 **아이콘** + 수 · 시간(⏱ 는 정본도 글자 · T106 몫).
                Button up = PopupKit.Btn(card, "fi-upgrade", "", "pp_blue", "pp_blue_dk", () => h.OnStartUpgrade(), inner * 0.8f, bh, "stage_ink", TextKind.Sub, poor);
                IconTextStack.ReplaceLabel(up, TextKind.Sub, "레벨 " + (h.Forge.ForgeLevel + 1) + " 업그레이드\n🪙 " + NumFmt.Fmt(cost) + " · ⏱ " + NumFmt.FmtTime(time), "stage_ink", "pp_blue");
            }
            PopupKit.XButton(card, () => Close(h));
        }

        static void TwoLine(Button b)
        {
            TextMeshProUGUI t = b.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) { t.textWrappingMode = TextWrappingModes.Normal; t.lineSpacing = -20f; }
        }

        /// <summary>모든 장비의 목록 — 시대별 헤더 막대 + 회색 패널 안 셀(무기는 그 시대에 등장하는 종류만 · % 는 개별 확률). 닫기 = 확률 정보로 복귀.</summary>
        public static void RenderListView(ForgeHost h)
        {
            Popup p = h.Meta.Popups.Find(Name);
            if (p == null) return;
            RectTransform root = PopupLayer.Clear(p);
            GameDefs d = h.Defs;
            float rem = PopupKit.Rem, W = UiKit.RefW, H = UiKit.RefH;
            // T339 — 정본 5606 `.fl-card { width: 71% }`(주석: «원본 shot-042905 71.3% · shot-042931 70.5% 실측»).
            //        목록 카드는 **확률 정보와 폭이 다르다**(71 ↔ 77.19) — 여태 둘 다 박힌 0.85 로 같았다.
            float w = W * ForgeInfoStyle.L("fl_card_w_f"), pad = rem * 0.9f;
            float inner = w - pad * 2f - PopupKit.Line3 * 2f;
            float cardH, cardY;
            // 높이는 정본에 선언이 없어(공용 상한이 잡는다) 쓰던 값을 표로만 옮겼다 — 재는 사람이 표를 고친다.
            PopupKit.FitBetweenBars(H * ForgeInfoStyle.L("fl_card_h_f"), out cardH, out cardY);   // T78 — ✕ 가 탭바에 가리지 않게
            float th = PopupKit.FontSize(TextKind.Title2) * 1.3f;   // T404 2회차 — 정본 ui.js 2125 `<h3 class="fi-title">모든 장비의 목록</h3>` = 5056 1.12rem → Title2
            float chrome = pad * 2f + th + rem * 0.4f + PopupKit.Line3 * 2f;                      // 카드 안에서 목록이 아닌 몫(패딩·제목·틈·테)
            // T388 3회차 — 정본 **722** `.forge-age-list { max-height: calc(var(--app-h) * .59); overflow-y: auto }`.
            //   여태 클론은 **카드를 `fl_card_h_f` .76 으로 고정하고 목록을 그 나머지로 채웠다** — 그래서 목록이 67%H 로 서서 상한을 +13% 넘었다(런 704 실측).
            //   정본은 반대다: `.fl-card` 에 높이 선언이 **없고** 상한은 **안쪽 목록**이 쥔다(카드는 내용만큼 자란다 · `fl_card_h_f` 는 그 위의 공용 상한 노릇).
            //   그래서 ⓐ 목록을 표의 .59H 로 자르고 ⓑ 카드도 그만큼 줄인다 — ⓑ 를 빼면 카드 아래에 정본에 없는 빈 자리가 남는다.
            float listH = Mathf.Min(cardH - chrome, H * ForgeInfoStyle.L("fl_list_max_h_f"));
            cardH = Mathf.Min(cardH, listH + chrome);
            RectTransform card = PopupKit.Card(root, "card", w, cardH, "pp_paper", rem * 1.1f, "pp_line", cardY);
            TextMeshProUGUI title = UiKit.Text(card, "title", TextKind.Title2, "모든 장비의 목록", "pp_ink");   // T404 2회차 — .fi-title 1.12rem = 40.8px → Title2 42(전엔 Title 60 · +47%)
            title.fontStyle = FontStyles.Bold;
            UiKit.Place(title.rectTransform, pad, pad, inner, th);
            RectTransform scrollBox = UiKit.Box(card, "forge-age-list");
            UiKit.Place(scrollBox, pad, pad + th + rem * 0.4f, inner, listH);
            // T364 11회차 ⑥ — 정본 722 `.forge-age-list { gap: calc(var(--app-h) * .0492) }`: 시대 구획 **사이** 틈. 여태 같은 값에 `× 0.5` 가 붙어 절반이었다
            //   (구획 상자는 머리 + .3rem + 격자만 담아 반틈을 메울 여백이 없다 · 정본 주석 716~721 «종전 .6rem 은 −3.8%p 라 등급 묶음이 한 덩어리로 읽혔다»). 표 ForgeInfoUi.json.
            RectTransform content = PopupKit.ScrollList(scrollBox, "list", H * ForgeInfoStyle.L("fl_age_gap_h"), 0f, rem * 0.2f);
            int stars = h.AscendCount;
            float barH = rem * 1.75f;
            // T364 5회차 — 정본 style.css **728** `.forge-item-grid { gap: calc(var(--app-h) * .0126) calc(var(--app-w) * .0395);
            //   padding: calc(var(--app-w) * .0229) calc(var(--app-w) * .0249) }`. 값은 맞았지만 여기 **숫자로 박혀** 있어
            //   자도 사람도 못 보던 자리다(T364 1회차가 «맞으나 코드에 박힘» 으로 적어 둔 넷 · §1 «수치는 코드에 박지 않는다»).
            //   ⚠ CSS `padding: A B` 는 세로 A · 가로 B 고, 여기선 **둘 다 `--app-w` 기준**이다(세로 패딩도 폭으로 잰다).
            float cellGapX = W * ForgeInfoStyle.L("fl_grid_gap_x_w"), cellGapY = H * ForgeInfoStyle.L("fl_grid_gap_y_h");
            float gridPadX = W * ForgeInfoStyle.L("fl_grid_pad_x_w"), gridPadY = W * ForgeInfoStyle.L("fl_grid_pad_y_w");
            float cell = (inner - gridPadX * 2f - cellGapX * 4f) / 5f;
            // T372 — 칸 아래 % 라벨은 정본 `.forge-item-cell small { font-size: .56rem }`(style.css 790 · 기준 캔버스 20.4px)다.
            //   `Sub` 하한 36 을 주면 1.76배가 되어 라벨이 칸 피치를 넘고 스물다섯이 한 줄로 붙는다(원작 shot-042905 는 다섯 덩어리 · 틈 3.12%W).
            //   결정 633: 새 종류를 만들지 않고 §1 하한의 예외 한 자리 `Micro`(18)를 쓴다 — 정본 20.4 와 2.4px 차라 칸 안에 든다.
            float labelH = PopupKit.FontSize(PctKind) * 1.2f;
            float cellH = cell + H * ForgeInfoStyle.L("fl_cell_gap_h") + labelH;   // T364 11회차 ⑦ — 정본 738 `.forge-item-cell { gap: calc(var(--app-h) * .0069) }`(셀 바닥 → % 글자) · 아래 라벨 자리와 한 키
            // T122 ⓑ — 정본 hydrateForgeThumbs(ui.js 2183): 목록을 다시 그리면 이전 굽기 작업은 스스로 멈춘다(_thumbJob) · 한 프레임 몇 장씩(펌프)
            int thumbJob = ItemFaces.NewJob();
            for (int ai = 0; ai < d.Ages.Length; ai++)
            {
                string age = d.Ages[ai];
                int ageIdx = ai;
                double ageP = h.Engine.AgeProbsAt(h.Forge.ForgeLevel).Get(age, 0);
                var cells = new List<Action<RectTransform>>();
                string[] weapons = h.Engine.WeaponsOfAge(age);
                double wp = h.Engine.ItemDropChance(age, "weapon");
                for (int i = 0; i < weapons.Length; i++)
                {
                    string wt = weapons[i]; int idx = i;
                    cells.Add(rt => Cell(rt, h, age, ageIdx, "weapon", idx, wt, ForgeUi.WeaponIconKey(d, wt), wp, stars, cell, labelH, thumbJob));
                }
                foreach (string slot in d.Slots)
                {
                    if (slot == "weapon") continue;
                    string[] names;
                    if (slot == "helmet" || slot == "armor")
                    {
                        OrderedMap<string[]> cat = d.ItemNames.Has(age) ? d.ItemNames[age] : null;
                        names = cat != null && cat.Has(slot) ? cat[slot] : new string[0];
                    }
                    else names = h.Engine.AccNames(age, slot);
                    double sp = h.Engine.ItemDropChance(age, slot);
                    for (int i = 0; i < names.Length; i++)
                    {
                        int idx = i; string sl = slot;
                        cells.Add(rt => Cell(rt, h, age, ageIdx, sl, idx, null, ForgeUi.SlotIconKey(sl), sp, stars, cell, labelH, thumbJob));
                    }
                }
                int rows = (cells.Count + 4) / 5;
                float gridH = gridPadY * 2f + rows * cellH + (rows - 1) * cellGapY;
                RectTransform section = PopupKit.Item(content, "section-" + age, -1f, barH + rem * 0.3f + gridH);
                RectTransform bar = ForgeUi.AgeBar(section, "head", inner, barH, d, age, Pct(ageP), null, stars);
                UiKit.Place(bar, 0f, 0f, inner, barH);
                RectTransform grid = UiKit.Box(section, "forge-item-grid");
                UiKit.Place(grid, 0f, barH + rem * 0.3f, inner, gridH);
                Image gbg = UiKit.Rounded(grid, "bg", "pp_gray", rem * 0.7f);
                gbg.color = new Color(0xd6 / 255f, 0xd6 / 255f, 0xd6 / 255f);
                for (int i = 0; i < cells.Count; i++)
                {
                    RectTransform c = UiKit.Box(grid, "cell-" + i);
                    UiKit.Place(c, gridPadX + (i % 5) * (cell + cellGapX), gridPadY + (i / 5) * (cellH + cellGapY), cell, cellH);
                    cells[i](c);
                }
            }
            PopupKit.XButton(card, () => Open(h));
        }

        /// <summary>
        /// T388 4회차 — 정본 `.fi-pill` 의 폭: **내용만큼**(inline-flex)이되 **`min-width: 5.5rem` 밑으로는 안 내려간다**(5066).
        /// 내용 폭은 <see cref="ForgeUi.Pill"/> 이 글자 상자에 준 여백 그대로다 — 왼쪽은 아이콘 칸 `h`, 오른쪽은 `h * 0.4`.
        /// </summary>
        static float PillWidth(RectTransform pill, float minW, float h)
        {
            Transform t = pill != null ? pill.Find("amt") : null;
            TextMeshProUGUI lbl = t != null ? t.GetComponent<TextMeshProUGUI>() : null;
            float content = lbl != null ? h * 1.4f + lbl.preferredWidth : 0f;
            return Mathf.Max(minW, content);
        }

        /// <summary>정본 hydrateForgeThumbs(ui.js 2190~2194)가 셀의 data- 로 만드는 썸네일 키 — rarity 'common' · 승천 티어 없음(stars 는 배지에만).</summary>
        static ForgeItem ThumbDef(string age, int ageIdx, string slot, int variant, string wtype)
        {
            return new ForgeItem { Slot = slot, Age = age, AgeIdx = ageIdx, WType = wtype, NameIdx = variant, Rarity = "common", Stars = 0 };
        }

        static void Cell(RectTransform rt, ForgeHost h, string age, int ageIdx, string slot, int variant, string wtype, string icon, double pct, int stars, float size, float labelH, int thumbJob)
        {
            RectTransform tile = ForgeUi.ItemTile(rt, "fl-face", size, h.Defs, age, icon, 0.8f, agePattern: true);   // T124 3회차 — 정본 ui.js 2090 `fl-face equip-cell[data-age]`: 목록 타일도 시대 무늬 층(.55)을 입는다
            UiKit.Place(tile, 0f, 0f, size, size);
            // T332 4회차 — 접지 그림자는 **플레이스홀더에도** 건다. 정본 선택자가 `.fl-face img, **.fl-face .ico**`(style.css 761)이고
            // 그 주석이 까닭을 댄다: «플레이스홀더 `.ico`(IconGen) 도 같이 걸어야 **하이드레이션 전후로 그림이 안 튄다**».
            // (윗줄 754 주석은 «img 에만 건다» 라고 적었지만 그 블록엔 `filter` 가 없다 — 렌더 결과는 아래 블록이 쥔다 · §1 «정본대로 = 렌더 결과» · 결정 520 ⓒ 와 같은 갈래.)
            // 그래서 캡처가 없는 장신구 칸(끝까지 실루엣)도 이웃과 같은 그림자를 쓴다 — 아래 `Request` 는 같은 키를 다시 걸 뿐이다.
            {
                Transform ph = tile.Find("img");
                if (ph != null) ForgeUi.ThumbShadow(ph.GetComponent<Image>(), "list");
            }
            // T122 ⓑ — 정본 2104 는 슬롯 아이콘을 플레이스홀더로 깔고 2183 hydrateForgeThumbs 가 다음 프레임부터 한 프레임 몇 장씩 3D 썸네일로 갈아 끼운다
            //   (같은 부위 다섯 칸이 전부 같은 그림이던 자리). 캡처가 없는 장신구는 정본도 실루엣(Request 가 null 로 답한다).
            if (ItemFaces.Supports(slot))
                ItemFaces.Request(thumbJob, h.Defs, ThumbDef(age, ageIdx, slot, variant, wtype), sp => { if (tile != null) ForgeUi.ApplyThumb(tile, sp, size, "list"); });   // T332 3회차 — 목록 `.fl-face img`(763~770)만 접지 그림자가 옅다(.28)
            Image face = tile.GetComponentInChildren<Image>();
            if (stars > 0)
            {
                TextMeshProUGUI st = UiKit.Text(tile, "asc", TextKind.Sub, ForgeUi.Stars(stars, 3), "coin");
                // T396 8회차 — 정본 **784** `.fl-face[data-asc]…::after { color: #ff8801 }`. **정본은 같은 별을 자리마다 다른 색으로 둔다** —
                //   격자 칸의 `.equip-cell .cell-star`(953)는 #ffd54f 라 전역 `coin` 이 맞지만(5회차가 그 짝을 적었다),
                //   여기 **목록 타일**은 한 단계 주황이다(G −77 · B −78). 같은 그림이라고 같은 키가 아니다.
                st.color = PinnedColorUi.C("list_asc_star_ink");
                st.fontStyle = FontStyles.Bold;
                PopupKit.Ring(st, "pp_line", 0.25f);
                UiKit.Anchor(st.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, size, st.fontSize * 1.2f);
            }
            TextMeshProUGUI l = UiKit.Text(rt, "pct", PctKind, pct.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "%", "pp_ink");
            TabularText.Apply(l);   // T352 8회차 — 정본 8635 `.forge-item-cell small { font-variant-numeric: tabular-nums }`: 다섯 열 스물다섯 칸의 % 가 세로로 열을 이룬다 — 숫자 구간만 <mspace>(결정 570 · 칸 폭은 글꼴에서) · ⓒ 여섯 중 마지막 자리(T396 반납으로 열렸다)
            // T372 — 굵기도 정본 렌더 결과를 따른다. 790 은 `font-weight: 800` 이지만 style.css **8633**
            //   `.rates-tip, .pass-desc, .forge-item-cell small, .league-server { font-weight: 500 }` 이 같은 특정도로 뒤에 와서 이긴다.
            //   그 줄의 정본 주석이 까닭을 댄다 — 폴백 sans 는 regular/bold 두 축뿐이라 500 은 **보통 굵기로 내려간다**.
            //   (§1 «정본대로 = 렌더 결과» · 굵기는 잉크 폭을 함께 쥐고 있어 이 자리 판정에 바로 들어간다.)
            l.enableAutoSizing = false;
            UiKit.Place(l.rectTransform, -size * 0.3f, size + UiKit.RefH * ForgeInfoStyle.L("fl_cell_gap_h"), size * 1.6f, labelH);   // T364 11회차 ⑦ — 위 cellH 와 같은 키
            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = face;
            b.onClick.AddListener(() => OpenDetail(h, age, slot, variant, wtype));
        }

        /// <summary>장비 상세(UI-SPEC 21~24 «장비 상세 팝업») — 흰 카드 · 좌 아이콘 + 우 [시대] 이름/기본 주스탯 · 아래 회색 옵션 패널(13종 범위) · 닫기 = 카드 아래 빨간 ✕.</summary>
        public static void RenderDetail(ForgeHost h)
        {
            Popup p = h.Meta.Popups.Find(ItemName);
            if (p == null || detailAge == null) return;
            RectTransform root = PopupLayer.Clear(p);
            Button dim = root.GetChild(0).gameObject.GetComponent<Button>() ?? root.GetChild(0).gameObject.AddComponent<Button>();
            dim.onClick.RemoveAllListeners();
            dim.onClick.AddListener(() => CloseItemDetail(h));
            GameDefs d = h.Defs;
            string age = detailAge, slot = detailSlot;
            int ageIdx = Array.IndexOf(d.Ages, age);
            string name, icon;
            if (slot == "weapon")
            {
                WeaponType wt = d.WeaponTypes.Get(detailWtype, null);
                name = wt != null ? wt.Kr : d.SlotKr.Get("weapon", "무기");
                icon = ForgeUi.WeaponIconKey(d, detailWtype);
            }
            else if (slot == "helmet" || slot == "armor")
            {
                OrderedMap<string[]> cat = d.ItemNames.Has(age) ? d.ItemNames[age] : null;
                string[] names = cat != null && cat.Has(slot) ? cat[slot] : null;
                name = names != null && detailVariant < names.Length ? names[detailVariant] : d.SlotKr.Get(slot, slot);
                icon = ForgeUi.SlotIconKey(slot);
            }
            else
            {
                string[] accs = h.Engine.AccNames(age, slot);
                name = detailVariant < accs.Length ? accs[detailVariant] : d.SlotKr.Get(slot, slot);
                icon = ForgeUi.SlotIconKey(slot);
            }
            string main = d.SlotMain.Get(slot, "atk");
            double baseVal = Math.Floor(main == "atk" ? ForgeRules.TierBaseAtkAt(ageIdx) : ForgeRules.TierBaseHpAt(ageIdx));

            float rem = PopupKit.Rem;
            // T177 — 정본 3649 `#forge-item-modal .modal-card.item-detail { width: 68.9% }`(공용 modal_card_w 75% 가 아니다 · T111·T113 과 같은 병)
            //        + 3720 `padding: 1.7% 2.7%`(컨테이닝 블록 = 모달 = 앱 폭). 값은 ForgeItemUi.json.
            float w = ForgeItemStyle.L("card_w") * UiKit.RefW;
            float padV = ForgeItemStyle.L("card_pad_v_app_f") * UiKit.RefW, pad = ForgeItemStyle.L("card_pad_h_app_f") * UiKit.RefW;
            float inner = w - pad * 2f - PopupKit.Line3 * 2f;   // = .idet-wrap 폭(아래 %마진·%패딩의 컨테이닝 블록)
            RectTransform card = PopupKit.Card(root, "card", w, -1f, "pp_paper", rem * 1.1f);
            VerticalLayoutGroup cg = PopupKit.Column(card, pad, ForgeItemStyle.L("card_gap_rem") * rem);   // 3648 gap: 0
            cg.padding = new RectOffset(Mathf.RoundToInt(pad), Mathf.RoundToInt(pad), Mathf.RoundToInt(padV), Mathf.RoundToInt(padV));
            float tile = rem * 3.6f;
            RectTransform head = PopupKit.Item(card, "idet-head", -1f, tile + rem * 0.4f);
            RectTransform t = ForgeUi.ItemTile(head, "idet-icon", tile, d, age, icon);
            // T371 10회차 — 정본 3661 `#forge-item-modal .idet-icon { background: color-mix(in srgb, var(--rc) 58%, #17181a); border-color: color-mix(… 80%, #000) }`:
            //   ItemTile 은 장비 칸 키(cell_face/cell_line)로 칠하고 값은 같지만, 정본이 이 팝업에만 가둔 선택자라 표 키(idet_icon_*)를 따로 쥔다 —
            //   부르는 쪽이 덮는다(ForgeCraftPopup.MixFrame 과 같은 길 · check_color_mix 가 이 줄로 자리를 센다).
            {
                Color idetAc = ForgeUi.AgeColor(d, age);
                Transform idetFrame = t.Find("frame");
                Transform idetF = idetFrame != null ? idetFrame.Find("face") : null, idetL = idetFrame != null ? idetFrame.Find("line") : null;
                Image idetFace = idetF != null ? idetF.GetComponent<Image>() : null, idetLine = idetL != null ? idetL.GetComponent<Image>() : null;
                if (idetFace != null) idetFace.color = ColorMixUi.Mix("idet_icon_face", idetAc);
                if (idetLine != null) idetLine.color = ColorMixUi.Mix("idet_icon_line", idetAc);
            }
            ForgeUi.ApplyThumb(t, ItemFaces.Get(d, ThumbDef(age, ageIdx, slot, detailVariant, detailWtype)), tile, "");   // T122 ⓑ — 정본 2244 `idet-icon`: thumb ? <img> : 아이콘(동기 · 한 장) · T332 3회차 — `.idet-icon img`(3668)는 아웃라인만이고 접지 그림자가 없다(빈 키)
            UiKit.Place(t, 0f, rem * 0.2f, tile, tile);
            float lh = PopupKit.FontSize(TextKind.Body) * 1.3f;
            TextMeshProUGUI nm = UiKit.Text(head, "idet-name", TextKind.Body, "[" + ForgeUi.AgeKr(d, age) + "] " + name, "pp_ink", TextAlignmentOptions.Left);
            nm.fontStyle = FontStyles.Bold;
            UiKit.Place(nm.rectTransform, tile + rem * 0.6f, rem * 0.2f, inner - tile - rem * 0.6f, lh);
            TextMeshProUGUI mv = UiKit.Text(head, "idet-main", TextKind.Sub, NumFmt.Fmt(baseVal) + " " + ForgeUi.StatLabel(main), "pp_ink", TextAlignmentOptions.Left);
            mv.fontStyle = FontStyles.Bold;
            UiKit.Place(mv.rectTransform, tile + rem * 0.6f, rem * 0.2f + lh, inner - tile - rem * 0.6f, lh);
            // T114 — 여기에 «확률 0.0000%» 셋째 줄을 달지 않는다: 정본 `ui.js` 2242~2248 의 `idet-head` 는
            //         `idet-icon` + `idet-title`(이름 · 스탯) **두 줄뿐**이고 원작 샷 `shot-042931` 도 그렇다.
            //         드랍 확률은 «모든 장비의 목록» 격자 셀(위 `Cell` 의 `pct`)에만 나온다 — 그 자리는 정본에도 있다.
            //         §1 «원작에 없는 것을 넣지 않는다» · 검수 Q 등재(런 223 `screen_forge-detail` 30장 중 꼴찌 1.2/10).
            // T177 — 정본 3723 `#forge-item-modal .idet-subs { margin-top: 12.9% }`(컨테이닝 블록 = .idet-wrap = inner · 원본 아이콘 하단→판 상단 41px): 카드 gap 이 0 이라 여백 칸으로.
            PopupKit.Spacer(card, ForgeItemStyle.L("subs_margin_top_wrap_f") * inner).name = "idet-subs-margin";
            RectTransform subs = PopupKit.Item(card, "idet-subs", -1f, -1f);
            // T146 — 정본 style.css 3722~3726 `#forge-item-modal .idet-subs { background: #d6d6d6 }`: 이 모달만 공용 판(--pp-panel #efefef)을
            //   덮어썼다(정본 주석 «원본 실측 rgb(214,214,214) — --pp-panel 은 25 밝았다»). 공용 pp_panel 을 고치면 다른 화면이 따라 어두워지니 제 키로.
            Image sbg = UiKit.Rounded(subs, "bg", "idet_panel", RadiusUi.Px("idet_subs_r_rem"));   // 정본 3703 `.idet-subs { border-radius: .8rem }` — 클론은 .6 이었다(#forge-item-modal 3722 는 반지름을 안 덮는다 · T345 21회차)
            // T177 — 3723 `padding: 3.3% 4% 4.4%; gap: 0`(컨테이닝 블록 = .idet-wrap)
            VerticalLayoutGroup sg = PopupKit.Column(subs, 0f, ForgeItemStyle.L("subs_gap_rem") * rem);
            int sp = Mathf.RoundToInt(ForgeItemStyle.L("subs_pad_side_wrap_f") * inner);
            sg.padding = new RectOffset(sp, sp, Mathf.RoundToInt(ForgeItemStyle.L("subs_pad_top_wrap_f") * inner), Mathf.RoundToInt(ForgeItemStyle.L("subs_pad_bottom_wrap_f") * inner));
            // T146 — 정본 3707 `.idet-lead { font-weight: 800 }` + 3728 `#forge-item-modal .idet-lead { color: #000 }`(순검정 · 굵게)
            // T177 — 높이는 줄바꿈 내용대로(정본 .92rem·1.13 은 §1 하한 아래라 Sub 그대로) · 3727 `margin-bottom: .96rem` 은 여백 칸으로(gap 0)
            PopupKit.Label(subs, "idet-lead", TextKind.Sub, "장비은(는) 아래 목록에서 2x개의 고유한 하위 스탯을 굴립니다:", "idet_lead_ink", TextAlignmentOptions.Left, true, true);
            PopupKit.Spacer(subs, ForgeItemStyle.L("lead_mb_rem") * rem).name = "idet-lead-margin";
            float rowH = ForgeItemStyle.L("row_pitch_h") * UiKit.RefH;   // 3714 «행 피치 1.93%H» — 정본이 적은 «2.47%H 로 13행 누적 +6.14%p» 병의 자리
            for (int i = 0; i < d.Substats.Count; i++)
            {
                SubstatDef s = d.Substats[i];
                // T146 — 정본 3708 `.substat-row { font-weight: 700 }` + 3731 `#forge-item-modal .idet-subs .substat-row { color: #3a3a3a }`
                TextMeshProUGUI row = PopupKit.Label(subs, "substat-" + s.Key, TextKind.Sub, ForgeUi.SubRangeText(d, s.Key, s.Max) + " " + s.Label, "idet_row_ink", TextAlignmentOptions.Left, false, true, rowH);
                LetterSpacing.Apply(row, "substat_row_ls_em");   // T168 3회차 — 정본 3729 `#forge-item-modal .idet-subs .substat-row { letter-spacing: -.01em }`(음수 · 이 모달에서만 좁다)
            }
            // ✕ 는 화면당 하나다(T57): 이 팝업은 목록 팝업 **위에** 서므로 제 ✕ 를 또 달면 둘이 겹쳐 보인다
            // (원작 shot-042931 에는 밝은 ✕ 가 0개 · 딤 아래 목록의 ✕ 하나뿐이다 · 결정 기록 참조).
            // 닫는 길은 딤 탭(위 dim.onClick = CloseItemDetail)과 목록 팝업의 ✕ 다.
        }
    }

    /// <summary>T177 — `Resources/ForgeItemUi.json`(장비 시대 상세 배치표 · T111 `GearDetailStyle`·T113 `CraftStyle` 과 같은 꼴). 숫자는 표에서만(§1).</summary>
    /// <summary>
    /// T339 — 확률 정보·목록 카드 치수표(`Resources/ForgeInfoUi.json`). 장비 시대 상세는 T177 의
    /// <see cref="ForgeItemStyle"/>(`ForgeItemUi.json`)가 쥔다 — 이 조각은 그 형제다.
    /// </summary>
    public static class ForgeInfoStyle
    {
        public const string ResourcePath = "ForgeInfoUi";
        static JsonObject root, layout;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T339)");
            root = MiniJson.ParseObject(ta.text);
            layout = J.Obj(root["layout"]);
        }

        public static void Reset() { root = null; layout = null; }

        /// <summary>배치 값 원문(분수·rem — 접미가 곱할 기준을 말한다).</summary>
        public static float L(string key)
        {
            Load();
            object v = layout == null ? null : layout[key];
            if (!J.IsNum(v)) throw new System.Collections.Generic.KeyNotFoundException("ForgeInfoUi.json 에 배치 값 «" + key + "» 이 없다 (T339)");
            return (float)J.Num(v);
        }

        /// <summary>정본 `width: min(calc(var(--app-w) * .9), 22.9rem)` 을 그대로 — 둘 중 작은 쪽이다(기준 캔버스에서는 뒤쪽이 이긴다).</summary>
        public static float FiCardW(float appW, float rem)
        {
            return UnityEngine.Mathf.Min(L("fi_card_w_f") * appW, L("fi_card_w_max_rem") * rem);
        }

        /// <summary>T378 11회차 — 두 줄 버튼(제목 + small)의 높이. 정본은 높이를 안 주고 **글이 높이를 정한다**: 세로 패딩(표 `padKey` · rem) x 2 + 두 줄의 줄높이.
        /// 줄높이는 정본이 `line-height` 를 준 자리면 그 표 키(`lhKey` · LineHeightUi.json), 안 준 자리면 normal = 글꼴 자산 비율(lineHeight ÷ pointSize).
        /// 클론은 두 줄을 다 `k` 로 찍으므로(정본 small 은 글자 하한 §1 에 걸려 같은 단이다) 두 줄 x 그 글자 크기다.</summary>
        public static float TwoLineBtnH(TextKind k, string padKey, string lhKey)
        {
            return PopupKit.TwoLineBtnH(k, L(padKey) * PopupKit.Rem, lhKey);   // 12회차에 공용 도우미로 올렸다(판매 경고 버튼도 쓴다)
        }
    }

    public static class ForgeItemStyle
    {
        public const string ResourcePath = "ForgeItemUi";
        static JsonObject root, layout;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T177)");
            root = MiniJson.ParseObject(ta.text);
            layout = J.Obj(root["layout"]);
        }

        public static void Reset() { root = null; layout = null; }

        /// <summary>배치 값 원문(분수·rem — 접미가 곱할 기준을 말한다).</summary>
        public static float L(string key)
        {
            Load();
            object v = layout[key];
            if (!J.IsNum(v)) throw new System.Collections.Generic.KeyNotFoundException("ForgeItemUi.json 에 배치 값 «" + key + "» 이 없다");
            return (float)J.Num(v);
        }
    }
}
