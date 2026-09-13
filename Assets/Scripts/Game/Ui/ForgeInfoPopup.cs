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
            float w = UiKit.RefW * 0.85f;
            float pad = rem * 0.9f;
            float inner = w - pad * 2f - PopupKit.Line3 * 2f;
            RectTransform card = PopupKit.Card(root, "card", w, -1f, "pp_paper", rem * 1.1f);
            PopupKit.Column(card, pad, rem * 0.3f);
            RectTransform head = PopupKit.Item(card, "head", -1f, PopupKit.FontSize(TextKind.Title) * 1.25f);
            TextMeshProUGUI title = UiKit.Text(head, "title", TextKind.Title, "확률 정보", "pp_ink");
            title.fontStyle = FontStyles.Bold;
            float ib = rem * 1.5f;
            Button infoBtn = ForgeUi.InfoButton(head, "fi-info-btn", ib, () => OpenList(h));
            UiKit.Anchor(infoBtn.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-rem * 0.3f, 0f), ib, ib);
            PopupKit.Label(card, "sub", TextKind.Sub, "제련 확률", "pp_ink");
            RectTransform pills = PopupKit.Item(card, "pills", -1f, rem * 1.7f);
            float pw = inner * 0.4f, ph = rem * 1.7f;
            RectTransform cp = ForgeUi.Pill(pills, "coin", "coin", NumFmt.Fmt(h.Wallet.Coins), pw, ph);
            UiKit.Place(cp, inner * 0.5f - pw - rem * 0.5f, 0f, pw, ph);
            RectTransform gp = ForgeUi.Pill(pills, "gem", "gem", NumFmt.Fmt(h.Wallet.Gems), pw, ph);
            UiKit.Place(gp, inner * 0.5f + rem * 0.5f, 0f, pw, ph);
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
            float bh = UiKit.H("btn_h") * 1.7f;
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
                Image track = UiKit.Rounded(prog, "track", "pp_line", rem * 0.5f);
                double frac = Math.Min(1, Math.Max(0, 1 - remain / h.Engine.UpgradeTime(info)));
                Image fill = UiKit.Rounded(prog, "upg-fill", "pp_blue", rem * 0.5f - PopupKit.Line);
                fill.rectTransform.anchorMin = new Vector2(0f, 0f);
                fill.rectTransform.anchorMax = new Vector2((float)frac, 1f);
                fill.rectTransform.offsetMin = new Vector2(PopupKit.Line, PopupKit.Line);
                fill.rectTransform.offsetMax = new Vector2(-PopupKit.Line, -PopupKit.Line);
                TextMeshProUGUI tt = UiKit.Text(prog, "upg-time", TextKind.Sub, NumFmt.FmtTime(remain), "stage_ink");
                tt.fontStyle = FontStyles.Bold;
                PopupKit.Spacer(card, rem * 0.5f);
                Button skip = PopupKit.Btn(card, "fi-skip", "건너뛰기\n💎 " + NumFmt.Fmt(h.Engine.GemSkipCost()), "pp_gray", "pp_gray_dk", () => h.OnGemSkipForge(), inner * 0.6f, bh, "stage_ink", TextKind.Sub);
                TwoLine(skip);
            }
            else
            {
                double cost = h.Engine.UpgradeCost(info), time = h.Engine.UpgradeTime(info);
                bool poor = h.Wallet.Coins < cost;
                Button up = PopupKit.Btn(card, "fi-upgrade", "레벨 " + (h.Forge.ForgeLevel + 1) + " 업그레이드\n🪙 " + NumFmt.Fmt(cost) + " · ⏱ " + NumFmt.FmtTime(time), "pp_blue", "pp_blue_dk", () => h.OnStartUpgrade(), inner * 0.8f, bh, "stage_ink", TextKind.Sub, poor);
                TwoLine(up);
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
            float w = W * 0.85f, pad = rem * 0.9f;
            float inner = w - pad * 2f - PopupKit.Line3 * 2f;
            float cardH, cardY;
            PopupKit.FitBetweenBars(H * 0.76f, out cardH, out cardY);   // T78 — ✕ 가 탭바에 가리지 않게
            RectTransform card = PopupKit.Card(root, "card", w, cardH, "pp_paper", rem * 1.1f, "pp_line", cardY);
            TextMeshProUGUI title = UiKit.Text(card, "title", TextKind.Title, "모든 장비의 목록", "pp_ink");
            title.fontStyle = FontStyles.Bold;
            float th = PopupKit.FontSize(TextKind.Title) * 1.3f;
            UiKit.Place(title.rectTransform, pad, pad, inner, th);
            RectTransform scrollBox = UiKit.Box(card, "forge-age-list");
            UiKit.Place(scrollBox, pad, pad + th + rem * 0.4f, inner, cardH - pad * 2f - th - rem * 0.4f - PopupKit.Line3 * 2f);
            RectTransform content = PopupKit.ScrollList(scrollBox, "list", H * 0.0492f * 0.5f, 0f, rem * 0.2f);
            int stars = h.AscendCount;
            float barH = rem * 1.75f;
            float cellGapX = W * 0.0395f, cellGapY = H * 0.0126f, gridPadX = W * 0.0249f, gridPadY = W * 0.0229f;
            float cell = (inner - gridPadX * 2f - cellGapX * 4f) / 5f;
            float labelH = PopupKit.FontSize(TextKind.Sub) * 1.2f;
            float cellH = cell + H * 0.0069f + labelH;
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
                    cells.Add(rt => Cell(rt, h, age, "weapon", idx, wt, ForgeUi.WeaponIconKey(d, wt), wp, stars, cell, labelH));
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
                        cells.Add(rt => Cell(rt, h, age, sl, idx, null, ForgeUi.SlotIconKey(sl), sp, stars, cell, labelH));
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

        static void Cell(RectTransform rt, ForgeHost h, string age, string slot, int variant, string wtype, string icon, double pct, int stars, float size, float labelH)
        {
            RectTransform tile = ForgeUi.ItemTile(rt, "fl-face", size, h.Defs, age, icon, 0.8f);
            UiKit.Place(tile, 0f, 0f, size, size);
            Image face = tile.GetComponentInChildren<Image>();
            if (stars > 0)
            {
                TextMeshProUGUI st = UiKit.Text(tile, "asc", TextKind.Sub, ForgeUi.Stars(stars, 3), "coin");
                st.fontStyle = FontStyles.Bold;
                PopupKit.Ring(st, "pp_line", 0.25f);
                UiKit.Anchor(st.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, size, st.fontSize * 1.2f);
            }
            TextMeshProUGUI l = UiKit.Text(rt, "pct", TextKind.Sub, pct.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "%", "pp_ink");
            l.fontStyle = FontStyles.Bold;
            l.enableAutoSizing = false;
            UiKit.Place(l.rectTransform, -size * 0.3f, size + UiKit.RefH * 0.0069f, size * 1.6f, labelH);
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
            double pct = h.Engine.ItemDropChance(age, slot);

            float rem = PopupKit.Rem;
            float w = UiKit.L("modal_card_w") * UiKit.RefW, pad = rem * 0.9f;
            float inner = w - pad * 2f - PopupKit.Line3 * 2f;
            RectTransform card = PopupKit.Card(root, "card", w, -1f, "pp_paper", rem * 1.1f);
            PopupKit.Column(card, pad, rem * 0.5f);
            float tile = rem * 3.6f;
            RectTransform head = PopupKit.Item(card, "idet-head", -1f, tile + rem * 0.4f);
            RectTransform t = ForgeUi.ItemTile(head, "idet-icon", tile, d, age, icon);
            UiKit.Place(t, 0f, rem * 0.2f, tile, tile);
            float lh = PopupKit.FontSize(TextKind.Body) * 1.3f;
            TextMeshProUGUI nm = UiKit.Text(head, "idet-name", TextKind.Body, "[" + ForgeUi.AgeKr(d, age) + "] " + name, "pp_ink", TextAlignmentOptions.Left);
            nm.fontStyle = FontStyles.Bold;
            UiKit.Place(nm.rectTransform, tile + rem * 0.6f, rem * 0.2f, inner - tile - rem * 0.6f, lh);
            TextMeshProUGUI mv = UiKit.Text(head, "idet-main", TextKind.Sub, NumFmt.Fmt(baseVal) + " " + ForgeUi.StatLabel(main), "pp_ink", TextAlignmentOptions.Left);
            mv.fontStyle = FontStyles.Bold;
            UiKit.Place(mv.rectTransform, tile + rem * 0.6f, rem * 0.2f + lh, inner - tile - rem * 0.6f, lh);
            TextMeshProUGUI pv = UiKit.Text(head, "idet-pct", TextKind.Sub, "확률 " + pct.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "%", "pp_muted", TextAlignmentOptions.Left);
            UiKit.Place(pv.rectTransform, tile + rem * 0.6f, rem * 0.2f + lh * 2f, inner - tile - rem * 0.6f, lh);
            RectTransform subs = PopupKit.Item(card, "idet-subs", -1f, -1f);
            Image sbg = UiKit.Rounded(subs, "bg", "pp_panel", rem * 0.6f);
            VerticalLayoutGroup sg = PopupKit.Column(subs, rem * 0.5f, rem * 0.15f);
            PopupKit.Label(subs, "idet-lead", TextKind.Sub, "장비은(는) 아래 목록에서 2x개의 고유한 하위 스탯을 굴립니다:", "pp_ink", TextAlignmentOptions.Left, true, false, PopupKit.FontSize(TextKind.Sub) * 2.7f);
            for (int i = 0; i < d.Substats.Count; i++)
            {
                SubstatDef s = d.Substats[i];
                TextMeshProUGUI row = PopupKit.Label(subs, "substat-" + s.Key, TextKind.Sub, ForgeUi.SubRangeText(d, s.Key, s.Max) + " " + s.Label, "pp_ink", TextAlignmentOptions.Left, false, false);
            }
            // ✕ 는 화면당 하나다(T57): 이 팝업은 목록 팝업 **위에** 서므로 제 ✕ 를 또 달면 둘이 겹쳐 보인다
            // (원작 shot-042931 에는 밝은 ✕ 가 0개 · 딤 아래 목록의 ✕ 하나뿐이다 · 결정 기록 참조).
            // 닫는 길은 딤 탭(위 dim.onClick = CloseItemDetail)과 목록 팝업의 ✕ 다.
        }
    }
}
