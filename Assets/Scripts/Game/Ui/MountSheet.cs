using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.Mounts;
using Forge.Game.Gallery;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 탈것 시트(원작 ui.js openMounts · openMountDetail · closeMounts · onSummonMount · onEquipMount · onRideMount · index.html `#mount-modal` · css `.modal-card.sheet.mount-sheet`):
    /// 펫/스킬 화면과 같은 꼴의 전체 흰 시트(탭바 위까지 · 원작 padding-bottom = 탭바 높이 + .9rem) — 가운데 제목 «탈것 N/250» → 태엽 알약 한 줄 → 5열 타일 그리드(스크롤 · 리본 «탑승 중»/«장착됨» · Lv · ★) →
    /// 하단 소환 바(◀ 닫기 · xN · 소환 xN ⚙비용 / 승천 가능 / 보관함 가득 · i Lv.N 게이지 = (오픈 − prev)/(need − prev)). 상세 팝업은 펫 상세와 같은 종이 카드 + [업그레이드]·[타기]·[장착/해제].
    /// 진입은 원작처럼 장비 시트의 탈것 칸(T19 ForgeSheet)에서 <see cref="Open"/>(원작 `UI.openMounts()`). 규칙은 Core <see cref="MountSystem"/>(T40) · 변경 뒤 <see cref="PetSkillHost.MountsChanged"/>(세이브 되쓰기 + T11 탑승 갱신).
    /// ⚠ 이 화면만 원작 스크린샷이 없다(css 4252행) — 원작 CSS 가 같은 컴포넌트의 화법을 따랐듯 여기서도 펫 시트의 값을 그대로 쓴다.
    /// </summary>
    public static class MountSheet
    {
        public const string ModalName = "mounts", DetailModal = "mount-detail", Kind = "mount";
        static SkillPetSheet sheet;
        static readonly Dictionary<int, Button> tiles = new Dictionary<int, Button>();

        public static bool IsOpen { get { return sheet != null && sheet.Modal != null && sheet.Modal.IsOpen(ModalName); } }
        public static Button SummonButton { get; private set; }
        public static Button MultButton { get; private set; }
        public static Button RatesButton { get; private set; }
        public static Button BackButton { get; private set; }
        public static string Title { get; private set; }
        public static int GridCells { get; private set; }
        public static Button Tile(int i) { Button b; return tiles.TryGetValue(i, out b) ? b : null; }

        static PetSkillHost H { get { return sheet.Host; } }
        static MountSystem M { get { return H.Mounts; } }
        static GameDefs Defs { get { return H.Data.Defs; } }

        /// <summary>원작 `UI.openMounts()` — 어느 탭에서든 전체 모달로 연다(호스트가 아직 안 섰으면 아무 일도 없다).</summary>
        public static void Open()
        {
            if (SkillPetSheet.Instance == null || !PetSkillHost.Ready) return;
            sheet = SkillPetSheet.Instance;
            Render();
        }

        /// <summary>원작 `closeMounts` — 상세·업그레이드도 같이 닫는다.</summary>
        public static void Close()
        {
            if (sheet == null || sheet.Modal == null) return;
            MountUpgradePopup.Close();
            sheet.Modal.Close(DetailModal);
            sheet.Modal.Close(ModalName);
        }

        /// <summary>열려 있으면 다시 그린다(원작 `keepScroll(() => openMounts())`).</summary>
        public static void Refresh() { if (IsOpen) Render(); }

        static string Ribbon(int idx) { return M.RiddenIdx() == idx ? PetSkillStyle.T("ridden") : PetSkillStyle.T("equipped"); }

        // ===== openMounts =====
        public static void Render()
        {
            M.Ensure();
            tiles.Clear();
            float sheetH = UiKit.L("tabbar_top") * UiKit.RefH;
            float topRem = -((UiKit.RefH - sheetH) * 0.5f) / PetSkillStyle.RemPx;   // 앱 위 끝에 붙인다(모달 층은 앱 가운데 기준)
            PetSkillModal.Handle m = sheet.Modal.Open(ModalName, 1f, sheetH, topRem, false);
            RectTransform c = m.Content;
            float pad = PetSkillStyle.Px("pad_rem");
            float gap = PetSkillStyle.Px("gap_rem");
            float W = UiKit.RefW - pad * 2f;
            float bodyH = sheetH - pad - PetSkillStyle.Px("mounts_bottom_rem");
            RectTransform body = UiKit.Box(c, "mount-sheet");
            UiKit.Place(body, pad, pad, W, bodyH);
            float y = 0f;

            // ---- sheet-head ----
            float headH = PetSkillStyle.Px("head_h_rem");
            RectTransform head = UiKit.Box(body, "sheet-head");
            UiKit.Place(head, 0f, y, W, headH);
            Title = PetSkillStyle.T("mounts_title", M.Count(), M.Rules.InvCap);
            TextMeshProUGUI title = PetSkillKit.Stroked(head, "sheet-title", TextKind.Title, Title, PetSkillStyle.C("white"), "sheet_title");   // 정본 h2.sheet-title .11em
            UiKit.TextShadow(title, "paper_emboss");   // T333 2회차 — 정본 8381 한 벌 «밝은 종이 위 글자는 흰 엠보스»(0 1px 0 rgba(255,255,255,.92))
            UiKit.Fill(title.rectTransform);
            y += headH + gap;

            // ---- mount-pill-row(가운데 태엽 알약) ----
            float rowH = PetSkillStyle.Px("mount_pill_row_h_rem"), pillH = PetSkillStyle.Px("pill_h_rem");
            RectTransform row = UiKit.Box(body, "mount-pill-row");
            UiKit.Place(row, 0f, y, W, rowH);
            RectTransform pill = PetSkillKit.Pill(row, "pill-winder", PetSkillStyle.C("mount_pill"), "winder", PetSkillStyle.Fmt(H.Winders), pillH, 0f, (rowH - pillH) * 0.5f);
            pill.anchoredPosition = new Vector2((W - pill.sizeDelta.x) * 0.5f, (rowH - pillH) * 0.5f);
            y += rowH + gap;

            // ---- summon-bar(아래 고정) 자리 · grid-scroll(그 위까지) ----
            float barH = PetSkillStyle.Px("bar_h");
            float barY = bodyH - barH;
            ScrollRect scroll;
            RectTransform content = PetSkillKit.Scroll(body, "grid-scroll", out scroll);
            UiKit.Place((RectTransform)content.parent, 0f, y, W, Mathf.Max(10f, barY - gap - y));
            BuildGrid(content, W);
            BuildBar(body, W, barY, barH, pad);
        }

        static void BuildGrid(RectTransform content, float W)
        {
            float colW = PetSkillStyle.Px("pet_col_w"), colGap = PetSkillStyle.Px("pet_col_gap_w"), rowGap = PetSkillStyle.Px("sk_row_gap_h");
            float starH = UiCatalog.Instance.Kind(TextKind.Sub).size;
            int cols = 5;
            float gridW = cols * colW + (cols - 1) * colGap;
            float x0 = (W - gridW) * 0.5f;
            float cellH = colW + PetSkillStyle.Rem(0.1f) + starH;
            int n = M.Count();
            GridCells = n;
            float y;
            if (n == 0)
            {
                TextMeshProUGUI empty = PetSkillKit.Text(content, "grid-empty", TextKind.Sub, PetSkillStyle.T("mounts_empty"), PetSkillStyle.C("muted"), TextAlignmentOptions.Center, false);
                UiKit.Place(empty.rectTransform, 0f, PetSkillStyle.Rem(2.4f), W, starH * 1.5f);
                LineHeight.Apply(empty, "sk_grid_grid_empty_lh");   // T354 — 정본 4030 .sk-grid .grid-empty { line-height: 1.5 } (탈것 격자도 .sk-grid · ui.js 5660)
                y = PetSkillStyle.Rem(2.4f) * 2f + starH * 1.5f;
            }
            else
            {
                int rows = (n + cols - 1) / cols;
                for (int i = 0; i < n; i++)
                {
                    float cx = x0 + (i % cols) * (colW + colGap), cy = (i / cols) * (cellH + rowGap);
                    tiles[i] = TileAt(content, i, cx, cy, colW, cellH, starH);
                }
                y = rows * cellH + (rows - 1) * rowGap + rowGap;
            }
            content.sizeDelta = new Vector2(0f, y);
        }

        /// <summary>원작 `.pet-tile`(탈것): 등급색 타일 + 3D 얼굴(`mountFace`) + 리본(탑승 중/장착됨) + Lv + ★ — 타일은 **개체 인덱스**로 연다(같은 종이 여럿).</summary>
        static Button TileAt(RectTransform parent, int i, float x, float y, float size, float cellH, float starH)
        {
            Mount mt = M.Inst(i);
            bool active = M.IsActive(i);
            Button b = UiKit.Button(parent, "mount-tile-" + i, () => OpenDetail(i));
            RectTransform cell = b.GetComponent<RectTransform>();
            UiKit.Place(cell, x, y, size, cellH);
            RectTransform face = sheet.Pets.TileFace(cell, mt.Name, mt.Rarity, size, active, mt.Level, true, Ribbon(i), GalleryKind.Mounts);
            UiKit.Place(face, 0f, 0f, size, size);
            if (mt.Stars > 0) SkillPanel.StarRow(cell, mt.Stars, size, size + PetSkillStyle.Rem(0.1f), starH);
            return b;
        }

        static void BuildBar(RectTransform parent, float W, float barY, float barH, float pad)
        {
            RectTransform bar = UiKit.Box(parent, "summon-bar");
            UiKit.Place(bar, 0f, barY, W, barH);
            BackButton = SkillPanel.BackButtonAt(bar, PetSkillStyle.Px("back_left_w") - pad, barH - PetSkillStyle.Px("back_bottom_h") - PetSkillStyle.Px("back_h"), Close);
            int mult = H.SummonMult(Kind);
            float xw = PetSkillStyle.Px("x5_w"), xh = PetSkillStyle.Px("x5_h");
            MultButton = MultToggle(bar, mult, W * (1f - PetSkillStyle.L("x5_right_pet_f")) - xw, barH - PetSkillStyle.Rem(0.22f) - xh, xw, xh);
            float sw = PetSkillStyle.Px("summon_btn_w");
            bool ascend = H.AscendReady("mount");
            bool full = M.Space() < 1;
            if (ascend)
                SummonButton = PetSkillKit.PaperButton(bar, "summon-btn", PetSkillKit.BtnKind.Ascend, PetSkillStyle.T("ascend_ready"), PetSkillStyle.T("ascend_sub"), false, () => PetSkillHost.Say(PetSkillStyle.T("ascend_ready")));
            else if (full)
                SummonButton = SkillPanel.SummonBtn(bar, PetSkillStyle.T("summon_full"), "winder", PetSkillStyle.T("gauge", M.Count(), M.Rules.InvCap), true, OnSummon);
            else
                SummonButton = SkillPanel.SummonBtn(bar, PetSkillStyle.T("summon_x", mult), "winder", JsNum.ToString(M.WinderCost(mult)), !M.CanSummon(mult), OnSummon);
            UiKit.Place(SummonButton.GetComponent<RectTransform>(), (W - sw) * 0.5f, 0f, sw, barH);
            int lvl = M.Level();
            double? need = M.NextNeeded();
            double prev = M.PrevNeeded();
            int opens = M.State.MountOpens;
            float ratio = need.HasValue ? Mathf.Clamp01((float)((opens - prev) / (need.Value - prev))) : 1f;
            string gt = need.HasValue ? PetSkillStyle.T("gauge", JsNum.ToString(opens - prev), JsNum.ToString(need.Value - prev)) : PetSkillStyle.T("gauge_max");
            RatesButton = SkillPanel.SummonInfo(bar, PetSkillStyle.Px("info_left_w") - pad, barH, lvl, ratio, gt, () => SkillRatesPopup.Open(sheet, Kind));
        }

        static Button MultToggle(RectTransform bar, int mult, float x, float yTop, float w, float h)
        {
            bool on = mult > 1;
            Button b = UiKit.Button(bar, "x5-toggle", () => { H.CycleSummonMult(Kind); Refresh(); });
            RectTransform br = b.GetComponent<RectTransform>();
            UiKit.Place(br, x, yTop, w, h);
            RectTransform skin = PetSkillKit.Framed(br, "skin", PetSkillStyle.C(on ? "pp_blue" : "white"), PetSkillStyle.Px("x5_r_rem") * 0.5f + h * 0.25f, PetSkillKit.Line3);
            UiKit.Fill(skin);
            ((Image)skin.Find("line").GetComponent<Image>()).color = PetSkillStyle.C(on ? "pp_line" : "pp_blue");
            TextMeshProUGUI t = PetSkillKit.Text(br, "t", TextKind.Sub, "x" + mult, PetSkillStyle.C(on ? "white" : "pp_blue"));
            UiKit.Fill(t.rectTransform);
            return b;
        }

        // ===== 동작(원작 on*) =====

        /// <summary>원작 `onSummonMount` — 못 하면 «보관함 가득»/«태엽 부족» 토스트 · 되면 목록 재렌더 + 소환 결과 연출(x1 [다시 소환]).</summary>
        public static void OnSummon()
        {
            int count = H.SummonMult(Kind);
            MountSummonResult r = M.Summon(count);
            if (r == null)
            {
                PetSkillHost.Say(M.Space() < 1 ? PetSkillStyle.T("toast_mount_full", M.Count(), M.Rules.InvCap) : PetSkillStyle.T("toast_winders_short"));
                return;
            }
            H.MountsChanged();
            H.Save();
            Refresh();
            SkillSummonResultView.Open(sheet, Kind, r, OnSummon);
        }

        /// <summary>원작 `onEquipMount` — 장착/해제 토글(장착은 1마리 · 다른 것을 장착하면 이전 것은 자동 해제).</summary>
        public static void OnEquip(int idx)
        {
            if (!M.Equip(idx)) return;
            H.MountsChanged();
            H.Save();
            Refresh();
        }

        /// <summary>원작 `onRideMount` — 장착 중 여러 마리일 때 올라탈 놈 고르기(몸은 하나라 탑승은 1마리).</summary>
        public static void OnRide(int idx)
        {
            if (!M.SetRidden(idx)) return;
            H.MountsChanged();
            H.Save();
            Refresh();
        }

        // ===== openMountDetail — 펫 상세와 같은 패턴 =====
        public static void OpenDetail(int idx)
        {
            Mount mt = M.Inst(idx);
            if (mt == null) return;
            bool active = M.IsActive(idx);
            bool ridden = M.RiddenIdx() == idx;
            Big atk, hp;
            M.MountPower(mt, out atk, out hp);
            bool maxed = mt.Level >= M.Rules.IndivMaxLevel;
            int same = 0;
            for (int k = 0; k < M.Count(); k++) if (M.Inst(k).Name == mt.Name) same++;
            float wf = PetSkillStyle.L("petd_w_f");
            float w = wf * UiKit.RefW;
            float padX = PetSkillStyle.Px("petd_pad_x_rem"), padT = PetSkillStyle.Px("petd_pad_top_rem"), padB = PetSkillStyle.Px("petd_pad_bottom_rem");
            float inner = w - padX * 2f;
            float tile = inner * PetSkillStyle.L("petd_tile_w_f");
            float starH = UiCatalog.Instance.Kind(TextKind.Sub).size;
            float body = UiCatalog.Instance.Kind(TextKind.Body).size;
            int subLines = Mathf.Max(1, mt.Subs != null ? mt.Subs.Count : 0) + 1;   // + «같은 종류 보유 N개»
            float headH = Mathf.Max(tile + PetSkillStyle.Rem(0.25f) + starH, body * 1.3f + body * 1.35f * 2f + PetSkillStyle.Rem(0.45f) + starH * 1.45f * subLines);
            float btnH = PetSkillStyle.Px("petd_btn_h_rem");
            float h = padT + headH + PetSkillStyle.Px("petd_head_bottom_rem") + btnH + padB;
            PetSkillModal.Handle m = sheet.Modal.Open(DetailModal, wf, h, PetSkillStyle.L("petd_top_rem"));
            RectTransform c = m.Content;
            float y = padT;
            RectTransform tilecol = UiKit.Box(c, "petd-tilecol");
            UiKit.Place(tilecol, padX, y, tile, headH);
            RectTransform face = sheet.Pets.TileFace(tilecol, mt.Name, mt.Rarity, tile, active, mt.Level, false, Ribbon(idx), GalleryKind.Mounts);
            UiKit.Place(face, 0f, 0f, tile, tile);
            if (mt.Stars > 0) SkillPanel.StarRow(tilecol, mt.Stars, tile, tile + PetSkillStyle.Rem(0.25f), starH);
            float bx = padX + tile + PetSkillStyle.Px("petd_head_gap_rem");
            float bw = inner - tile - PetSkillStyle.Px("petd_head_gap_rem");
            float by = y + PetSkillStyle.Rem(0.1f);
            TextMeshProUGUI name = PetSkillKit.Stroked(c, "petd-name", TextKind.Body, PetSkillStyle.T("mount_name", Defs.RarityKr.Get(mt.Rarity, mt.Rarity), Defs.MountKr.Get(mt.Name, mt.Name)), PetSkillStyle.Rarity(Defs, mt.Rarity), "petd_name", TextAlignmentOptions.Left);   // 정본 .petd-wrap .petd-name max(1.2px, .125em)
            UiKit.Place(name.rectTransform, bx, by, bw, body * 1.3f);
            by += body * 1.3f + PetSkillStyle.Rem(0.05f);
            TextMeshProUGUI st1 = PetSkillKit.Text(c, "petd-atk", TextKind.Body, PetSkillStyle.T("dmg", PetSkillStyle.Fmt(atk)), PetSkillStyle.C("black"), TextAlignmentOptions.Left);
            UiKit.Place(st1.rectTransform, bx, by, bw, body * 1.35f);
            by += body * 1.35f;
            TextMeshProUGUI st2 = PetSkillKit.Text(c, "petd-hp", TextKind.Body, PetSkillStyle.T("hp", PetSkillStyle.Fmt(hp)), PetSkillStyle.C("black"), TextAlignmentOptions.Left);
            UiKit.Place(st2.rectTransform, bx, by, bw, body * 1.35f);
            by += body * 1.35f + PetSkillStyle.Rem(0.45f);
            int line = 0;
            if (mt.Subs == null || mt.Subs.Count == 0)
            {
                TextMeshProUGUI ns = PetSkillKit.Text(c, "petd-subs", TextKind.Sub, PetSkillStyle.T("no_subs"), PetSkillStyle.C("subs_ink"), TextAlignmentOptions.Left);
                UiKit.Place(ns.rectTransform, bx, by, bw, starH * 1.45f);
                LineHeight.Apply(ns, "petd_subs_lh");   // T354 — 정본 5551 .petd-subs { line-height: 1.45 } (탈것 상세는 .petd-wrap 밖 · ui.js 5707)
                line = 1;
            }
            else
                for (; line < mt.Subs.Count; line++)
                {
                    TextMeshProUGUI s = PetSkillKit.Text(c, "petd-sub-" + line, TextKind.Sub, PetSkillStyle.SubText(mt.Subs[line]), PetSkillStyle.C("subs_ink"), TextAlignmentOptions.Left);
                    UiKit.Place(s.rectTransform, bx, by + line * starH * 1.45f, bw, starH * 1.45f);
                }
            // 중복은 숫자가 아니라 개체다(2026-08-19 인벤 개편) — 같은 종류 보유 수가 그 자리를 대신한다
            TextMeshProUGUI sameT = PetSkillKit.Text(c, "petd-same", TextKind.Sub, PetSkillStyle.T("same_kind", same), PetSkillStyle.C("muted"), TextAlignmentOptions.Left, false);
            UiKit.Place(sameT.rectTransform, bx, by + line * starH * 1.45f, bw, starH * 1.45f);
            // buttons: [업그레이드] · [타기](장착 중 · 안 타는 놈) · [장착/해제]
            float btnY = padT + headH + PetSkillStyle.Px("petd_head_bottom_rem");
            float bgap = PetSkillStyle.Px("petd_btn_gap_rem"), bpad = PetSkillStyle.Px("petd_btn_pad_x_rem");
            int nb = active && !ridden ? 3 : 2;
            float ew = (w - bpad * 2f - bgap * (nb - 1)) / nb;
            float x = bpad;
            Button up = maxed
                ? PetSkillKit.PaperButton(c, "btn-upgrade", PetSkillKit.BtnKind.Primary, PetSkillStyle.T("upgrade"), PetSkillStyle.T("max_level", M.Rules.IndivMaxLevel), true, null)
                : PetSkillKit.PaperButton(c, "btn-upgrade", PetSkillKit.BtnKind.Primary, PetSkillStyle.T("upgrade"), null, false, () => { sheet.Modal.Close(DetailModal); MountUpgradePopup.Open(sheet, idx); });
            UiKit.Place(up.GetComponent<RectTransform>(), x, btnY, ew, btnH);
            x += ew + bgap;
            if (active && !ridden)
            {
                Button ride = PetSkillKit.PaperButton(c, "btn-ride", PetSkillKit.BtnKind.Primary, PetSkillStyle.T("ride"), null, false, () => { OnRide(idx); OpenDetail(idx); });
                UiKit.Place(ride.GetComponent<RectTransform>(), x, btnY, ew, btnH);
                x += ew + bgap;
            }
            Button tg = PetSkillKit.PaperButton(c, "btn-toggle", active ? PetSkillKit.BtnKind.Danger : PetSkillKit.BtnKind.Primary, PetSkillStyle.T(active ? "remove" : "equip"), null, false, () => { OnEquip(idx); OpenDetail(idx); });
            UiKit.Place(tg.GetComponent<RectTransform>(), x, btnY, ew, btnH);
        }
    }
}
