using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Ascend;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 승천 팝업(ROUTINE T21 · 원작 ui.js openAscension/onAscendLine/closeAscension).
    /// 제목(⭐ 승천 · 보유 별 합계) → 안내 → 라인 4행(장비·스킬·펫·탈것 · 조건을 채운 행은 초록 · 탭하면 그 라인 확인) → [라인 선택 시] 확인 상자(초기화·소멸 경고·이후 획득물 ⭐n) + [⭐ 승천][취소] / [닫기] → 아래 ✕.
    /// </summary>
    public static class AscendPopup
    {
        static RectTransform overlay;
        static string curLine;

        public static bool IsOpen { get { return overlay != null; } }
        /// <summary>열린 팝업의 뿌리(테스트가 자리·알파를 본다 · T359).</summary>
        public static RectTransform Root { get { return overlay; } }
        public static string Line { get { return curLine; } }
        public static int RowCount { get; private set; }
        public static Button AscendButton { get; private set; }
        public static Button CloseButton { get; private set; }
        public static string TitleText { get; private set; }
        /// <summary>승천이 일어났다(라인) — 장비·스킬·펫·탈것 화면(T19·T20)과 전투 재계산(T8)이 듣는다.</summary>
        public static event Action<string> Ascended;

        static readonly Dictionary<string, string> LineIcon = new Dictionary<string, string> { { "forge", "hammer" }, { "skill", "ticket" }, { "pet", "egg" }, { "mount", "winder" } };

        static DungeonUiHost Host { get { return DungeonUiHost.Instance; } }

        public static void Open(string line = null)
        {
            if (!DungeonUiHost.Ready) return;
            Close();
            Ascension asc = Host.Asc;
            AscensionState st = Host.AscState;
            asc.Ensure(st);
            curLine = line;
            AscensionLevels lv = Host.Levels();
            StarBreakdown b = Host.Stars();

            float W = UiKit.RefW;
            overlay = DungeonPopups.Overlay("modal-ascend");
            float cw = W * UiKit.L("card_w");
            float pad = DungeonPopups.RemL("card_pad_rem");
            float inner = cw - pad * 2f;
            float gap = DungeonPopups.RemL("card_gap_rem");
            float titleH = DungeonPopups.LineH(TextKind.Button);
            float subH = DungeonPopups.LineH(TextKind.Sub);
            float rowH = subH + DungeonPopups.RemL("asc_row_pad_y_rem") * 2f;
            float rowMy = DungeonPopups.RemL("asc_row_my_rem");
            string[] lines = asc.Table.Lines;
            float rowsH = lines.Length * (rowH + rowMy * 2f);
            float btnH = DungeonPopups.RemL("asc_btn_h_rem");
            float focusPad = DungeonPopups.RemL("asc_focus_pad_rem");
            float focusH = line != null ? DungeonPopups.RemL("asc_focus_mt_rem") + focusPad * 2f + DungeonPopups.RemL("asc_icon_rem") + DungeonPopups.LineH(TextKind.Body) + subH + subH * 4f : 0f;
            float ch = pad * 2f + titleH + gap + subH * 2f + gap + rowsH + focusH + DungeonPopups.RemL("asc_focus_mt_rem") + btnH;
            RectTransform card = DungeonPopups.Card(overlay, "card", cw, ch, DungeonPopups.RemL("card_r_rem"));

            float y = pad;
            // T398 — 정본 ui.js 5864 `<h3>${star} 승천 <small class="muted">보유 별 합계 ${star} N</small></h3>`: 제목은 **두 토막**이다 —
            // 굵은 잉크 «승천»(1761 h3 1.15rem) + 작고 흐린 «보유 별 합계 ⭐ N»(657 .muted .78rem · 400 · #78909c) · 별 아이콘이 앞뒤로 둘.
            // 가운뎃점 «·» 은 원작에 없다(§1). TitleText 는 자가 읽는 «글자만 이어 붙인 값» 이고 화면은 아래 한 줄(HorizontalLayoutGroup)이 세운다.
            TitleText = "승천 보유 별 합계 " + b.Total;
            float sd = titleH * 0.8f;
            // T420 — 정본 5619 `.asc-card { text-align: center }`: 카드 안 글자는 제목 줄(h3)까지 **통째로 가운데**다(안쪽에서 좌우를 다시 정하는 것은 행뿐 · 5627~5629).
            //   T398 이 세운 이 줄은 앞 별을 카드 왼쪽 여백에 못박고 `MiddleLeft` 로 몰아 제목만 −17.9%W 왼쪽이었다(런 891). 앞 별을 줄 **안** 첫 자식으로 넣고
            //   (정본 `${star} 승천` 의 띄어쓰기 한 칸은 큰 토막 글자 앞에 그대로) 줄을 카드 안쪽 폭 전체 · 가운데 정렬로 세운다.
            RectTransform titleRow = UiKit.Box(card, "title-row");
            var titleLay = titleRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            titleLay.childAlignment = TextAnchor.MiddleCenter;
            titleLay.childControlWidth = true; titleLay.childControlHeight = true;
            titleLay.childForceExpandWidth = false; titleLay.childForceExpandHeight = false;
            titleLay.spacing = 0f;
            Image star = UiKit.Icon(titleRow, "star", "star");
            var starLe = star.gameObject.AddComponent<LayoutElement>();
            starLe.preferredWidth = starLe.preferredHeight = sd; starLe.flexibleWidth = 0f;
            TextMeshProUGUI title = DungeonPopups.Bold(titleRow, "title", TextKind.Button, " 승천", "pp_ink", TextAlignmentOptions.Left);
            UiKit.OutlinePx(title, "pp_line", KeylineUi.Em("sheet_title", title.fontSize));   // 정본 3846 묶음(h3.sheet-title …) { .11em var(--pp-line) }
            UiKit.TextShadow(title, "paper_emboss");   // T333 2회차 — 정본 8381 한 벌 «밝은 종이 위 글자는 흰 엠보스»(0 1px 0 rgba(255,255,255,.92))
            float smallPx = title.fontSize * AscendUi.TitleSmallRatio();   // 표 AscendUi.json — .78 / 1.15
            TextMeshProUGUI small = UiKit.Text(titleRow, "title-small", TextKind.Micro, " 보유 별 합계 ", "muted2", TextAlignmentOptions.Left);
            small.fontSize = smallPx;
            Image star2 = UiKit.Icon(titleRow, "star-2", "star");
            var star2Le = star2.gameObject.AddComponent<LayoutElement>();
            star2Le.preferredWidth = star2Le.preferredHeight = smallPx; star2Le.flexibleWidth = 0f;
            TextMeshProUGUI smallN = UiKit.Text(titleRow, "title-small-n", TextKind.Micro, " " + b.Total, "muted2", TextAlignmentOptions.Left);
            smallN.fontSize = smallPx;
            UiKit.Place(titleRow, pad, y, inner, titleH);   // T420 — 줄이 카드 안쪽 폭 전체를 쓰고 안의 것들이 가운데로 모인다
            y += titleH + gap;
            TextMeshProUGUI guide = DungeonPopups.Para(card, "guide", TextKind.Sub, "라인마다 조건을 채우면 그 라인을 승천시킵니다 — 승천 횟수만큼 이후 획득물에 별이 붙습니다.", "muted2", TextAlignmentOptions.Center);
            UiKit.Place(guide.rectTransform, pad, y, inner, subH * 2f);
            y += subH * 2f + gap;

            RowCount = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string l = lines[i];
                AscensionProgress p = asc.Progress(l, lv);
                bool rdy = asc.Ready(l, lv);
                int cnt = asc.Count(st, l);
                RectTransform row = UiKit.Box(card, "row-" + l);
                UiKit.Place(row, pad, y + rowMy, inner, rowH);
                UiKit.Rounded(row, "bg", rdy ? "asc_ready" : "pp_panel", DungeonPopups.RemL("asc_row_r_rem"));
                string ink = rdy ? "white" : "pp_ink";
                float px = DungeonPopups.RemL("asc_row_pad_x_rem");
                float ico = subH * 0.9f;
                string ik; LineIcon.TryGetValue(l, out ik);
                Image im = UiKit.Icon(row, "ico", ik ?? "star");
                UiKit.Place(im.rectTransform, px, (rowH - ico) * 0.5f, ico, ico);
                float nameW = DungeonPopups.RemL("asc_name_w_rem");
                TextMeshProUGUI name = DungeonPopups.Bold(row, "name", TextKind.Sub, asc.Table.LineKr[l], ink, TextAlignmentOptions.Left);
                UiKit.Place(name.rectTransform, px + ico * 1.2f, 0f, nameW, rowH);
                string label = l == "forge" ? "대장간 Lv." + p.Cur + "/" + p.Max : "소환 Lv." + p.Cur + "/" + p.Max;
                float cntW = DungeonPopups.RemL("asc_cnt_w_rem");
                TextMeshProUGUI prog = UiKit.Text(row, "prog", TextKind.Sub, label, ink, TextAlignmentOptions.Left);
                UiKit.Place(prog.rectTransform, px + ico * 1.2f + nameW, 0f, inner - px * 2f - ico * 1.2f - nameW - cntW, rowH);
                OpacityUi.Apply(prog.gameObject, "asc_prog");   // T359 — 정본 5628 .asc-prog { opacity: .85 }
                // T359 — 정본 5624 `.asc-row.ready::after { content: '▶'; font-size: .7rem; opacity: .8; margin-left: .1rem }`: 화살은 cnt 칸 뒤의 제 상자(알파 .8) —
                //   cnt 글자에 붙이면 같은 알파가 되어 정본과 다르다. 준비 안 된 행엔 없다(::after 가 .ready 에만 있다).
                float rem = PopupKit.Rem;
                float arW = rdy ? OpacityUi.Rem("asc_arrow", "font_rem") * rem : 0f;
                float arMl = rdy ? OpacityUi.Rem("asc_arrow", "ml_rem") * rem : 0f;
                TextMeshProUGUI c = DungeonPopups.Bold(row, "cnt", TextKind.Sub, cnt > 0 ? "★" + cnt : "—", ink, TextAlignmentOptions.Right);
                UiKit.Place(c.rectTransform, inner - px - arW - arMl - cntW, 0f, cntW, rowH);
                if (rdy)
                {
                    TextMeshProUGUI ar = DungeonPopups.Bold(row, "arrow", TextKind.Sub, "▶", ink, TextAlignmentOptions.Right);
                    ar.fontSize = OpacityUi.Rem("asc_arrow", "font_rem") * rem;
                    UiKit.Place(ar.rectTransform, inner - px - arW, 0f, arW, rowH);
                    OpacityUi.Apply(ar.gameObject, "asc_row_ready_arrow");
                }
                if (rdy) UiKit.Button(row, "hit", () => Open(l));
                RowCount++;
                y += rowH + rowMy * 2f;
            }

            AscendButton = null;
            if (line != null)
            {
                y += DungeonPopups.RemL("asc_focus_mt_rem");
                RectTransform focus = UiKit.Box(card, "focus");
                UiKit.Place(focus, pad, y, inner, focusH - DungeonPopups.RemL("asc_focus_mt_rem"));
                DungeonPopups.Bordered(focus, "bg", "pp_sheet", DungeonPopups.RemL("asc_focus_r_rem"), DungeonPopups.Line3);
                float fy = focusPad;
                float fi = DungeonPopups.RemL("asc_icon_rem");
                string ik; LineIcon.TryGetValue(line, out ik);
                Image big = UiKit.Icon(focus, "ico", ik ?? "star");
                UiKit.Place(big.rectTransform, (inner - fi) * 0.5f, fy, fi, fi);
                fy += fi;
                string kr = asc.Table.LineKr[line];
                int next = asc.Count(st, line) + 1;
                TextMeshProUGUI ft = DungeonPopups.Bold(focus, "title", TextKind.Body, kr + " 승천", "pp_ink");
                UiKit.OutlinePx(ft, "pp_line", KeylineUi.Em("sheet_title", ft.fontSize));   // 정본 .asc-focus-title { .11em var(--pp-line) }
                UiKit.Place(ft.rectTransform, 0f, fy, inner, DungeonPopups.LineH(TextKind.Body));
                fy += DungeonPopups.LineH(TextKind.Body);
                TextMeshProUGUI fc = UiKit.Text(focus, "cnt", TextKind.Sub, "현재 승천 " + asc.Count(st, line) + "회 → " + next + "회", "pp_ink");
                UiKit.Place(fc.rectTransform, 0f, fy, inner, subH);
                fy += subH;
                string resetKr = line == "forge" ? "대장간 레벨이 1로 초기화" : "소환 레벨이 1로 초기화";
                string wipe = line == "forge" ? "(착용 장비 전부)" : line == "pet" ? "(출전 포함, 알은 유지)" : "(장착 포함)";
                string eff = "· " + resetKr + "됩니다\n· ⚠ 보유 중인 기존 " + kr + wipe + DungeonUiHost.Josa(kr, "이", "가") + " 전부 사라집니다\n· 이후 새로 "
                    + (line == "forge" ? "제작되는 장비" : "소환되는 " + kr) + DungeonUiHost.Josa(line == "forge" ? "장비" : kr, "이", "가") + " ★" + next + "로 나옵니다";
                // T383 5회차 — 정본 5635 `.asc-focus-eff { font-size: .76rem }`: 하한 `Sub`(36 ≈ .99rem)로 찍으면 셋째 항목이 접혀 정본 `<br>` 세 줄이 네 줄이 된다(런 688).
                // 결정 633 대로 새 종류 없이 예외 칸 `Micro` 를 쓰되 크기는 표(TextSizeUi · 정본 .76rem)에서 — §1 예외 넷째 자리.
                TextMeshProUGUI fe = DungeonPopups.Para(focus, "eff", TextKind.Micro, eff, "pp_ink", TextAlignmentOptions.Left);
                TextSizeUi.Apply(fe, "asc_focus_eff");
                UiKit.Place(fe.rectTransform, focusPad, fy, inner - focusPad * 2f, subH * 4f);
                OpacityUi.Apply(fe.gameObject, "asc_focus_eff");   // T359 — 정본 5635 .asc-focus-eff { opacity: .9 }
                LineHeight.Apply(fe, "asc_focus_eff_lh");   // T354 — 정본 5635 .asc-focus-eff { line-height: 1.5 }
                y += focusH - DungeonPopups.RemL("asc_focus_mt_rem");

                y += DungeonPopups.RemL("asc_focus_mt_rem");
                float bgap = DungeonPopups.RemL("asc_btns_gap_rem");
                float bw = (inner - bgap) * 0.5f;
                bool ready = asc.Ready(line, lv);
                string ln = line;
                AscendButton = DungeonPopups.Pill(card, "ascend", "★ 승천", ready ? DungeonPopups.Skin.Blue : DungeonPopups.Skin.Gray, TextKind.Button, () => OnAscend(ln), -1f, ready);
                UiKit.Place(DungeonPopups.Root(AscendButton), pad, y, bw, btnH);
                CloseButton = DungeonPopups.Pill(card, "cancel", "취소", DungeonPopups.Skin.Silver, TextKind.Button, Close);
                UiKit.Place(DungeonPopups.Root(CloseButton), pad + bw + bgap, y, bw, btnH);
            }
            else
            {
                y += DungeonPopups.RemL("asc_focus_mt_rem");
                CloseButton = DungeonPopups.Pill(card, "close", "닫기", DungeonPopups.Skin.Silver, TextKind.Button, Close);
                UiKit.Place(DungeonPopups.Root(CloseButton), pad, y, inner, btnH);
            }
            DungeonPopups.XButton(card, Close);
        }

        public static void OnAscend(string line)
        {
            Ascension asc = Host.Asc;
            if (!asc.Ready(line, Host.Levels())) { DungeonPopups.Toast("⭐ 아직 승천 조건을 채우지 못했습니다"); return; }
            if (!Host.Ascend(line)) { DungeonPopups.Toast("⭐ 승천에 실패했습니다"); return; }
            string kr = asc.Table.LineKr[line];
            DungeonPopups.Toast("⭐ " + kr + " 승천! 이후 획득물이 ⭐" + asc.Count(Host.AscState, line) + "로 나옵니다");
            Close();
            Host.RenderTopBar();
            var h = Ascended;
            if (h != null) h(line);
        }

        public static void Close()
        {
            if (overlay == null) return;
            UnityEngine.Object.Destroy(overlay.gameObject);
            overlay = null; curLine = null; AscendButton = null; CloseButton = null;
        }
    }
}
