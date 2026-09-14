using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.Meta;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 진행 패스 팝업(ROUTINE T22 · 원작 ui.js openPass/renderPass/onClaimPass · shot-042705): 근흑 카드 + 파란 리본 «진행 패스» + 안내문/가격 페넌트 + [무료|프리미엄] 탭 행 +
    /// 흰 트랙(구간마다 가운데 레일 색이 도달 여부로 갈린다 · 마일스톤 필 · 무료 칸/프리미엄 칸 · ✓/🔒 배지). 무료만 실지급 · 프리미엄은 토스트.
    /// </summary>
    public static class PassPopup
    {
        public const string Name = "pass";

        public static void Open(MetaHost h)
        {
            h.Popups.Show(Name);
            Render(h);
        }

        public static void Close(MetaHost h) { h.Popups.Hide(Name); }

        public static void Render(MetaHost h)
        {
            Popup p = h.Popups.Find(Name);
            if (p == null) return;
            RectTransform root = PopupLayer.Clear(p);
            float rem = PopupKit.Rem;
            float w = UiKit.RefW, H = UiKit.RefH;
            float cardW = UiKit.L("pass_card_w") * w;
            float padTop = UiKit.H("pass_pad_top"), padBottom = UiKit.H("pass_pad_bottom");
            float bannerH = UiKit.H("pass_banner_h") * 1.3f;
            float descH = PopupKit.FontSize(TextKind.Sub) * 2.8f;
            float headerH = UiKit.H("pass_header_h") * 1.5f;
            float trackH = UiKit.H("pass_track_h");
            float cardH = padTop + bannerH + rem * 1.19f + descH + rem * 1.31f + headerH + trackH + padBottom;
            RectTransform card = PopupKit.Card(root, "card", cardW, cardH, "pass_bg", rem);
            // T132 — 정본 ui.js 4924 `<div class="pass-sword">${IconGen.img('passsword')}</div>` · style.css 2686: 카드 윗변에서 4.81rem 위 · 가운데 · 3.72×6.72rem.
            // 리본(.pass-banner)보다 먼저 세운다 — 정본 DOM 순서대로 리본이 칼자루 위를 덮는다. 치수는 StaticIconsUi.json(§1).
            Image sword = PopupKit.IconOr(card, "pass-sword", "passsword");
            UiKit.Anchor(sword.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -StaticIconsUi.Rem("pass_sword_top_rem")), StaticIconsUi.Rem("pass_sword_w_rem"), StaticIconsUi.Rem("pass_sword_h_rem"));
            float inner = cardW - PopupKit.Line3 * 2f;
            float padX = rem * 1.1f;

            // 리본(카드 좌우를 넘는다)
            float ribbonW = cardW + rem * 2.55f * 2f;
            RectTransform ribbon = UiKit.Box(card, "banner");
            UiKit.Anchor(ribbon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -padTop), ribbonW, bannerH);
            RectTransform tailL = UiKit.Box(ribbon, "tail-l");
            UiKit.Place(tailL, 0f, rem * 0.31f, rem * 2.3f, rem * 2.44f);
            UiKit.Panel(tailL, "bg", "pass_banner_dk");
            RectTransform tailR = UiKit.Box(ribbon, "tail-r");
            UiKit.Place(tailR, ribbonW - rem * 2.3f, rem * 0.31f, rem * 2.3f, rem * 2.44f);
            UiKit.Panel(tailR, "bg", "pass_banner_dk");
            RectTransform band = UiKit.Box(ribbon, "band");
            UiKit.Place(band, rem * 1.44f, 0f, ribbonW - rem * 2.88f, bannerH);
            PopupKit.Outlined(band, "face", "pass_banner", rem * 0.2f, PopupKit.Line3);
            TextMeshProUGUI title = UiKit.Text(band, "title", TextKind.Title, "진행 패스", "stage_ink");
            title.fontStyle = FontStyles.Bold;
            PopupKit.Ring(title, "pass_card", "pp_line");   // 정본 .pass-card .11em(상속)

            // 안내문 · 가격 페넌트(2등분 그리드)
            float y = padTop + bannerH + rem * 1.19f;
            RectTransform desc = UiKit.Box(card, "desc-row");
            UiKit.Place(desc, PopupKit.Line3, y, inner, descH);
            TextMeshProUGUI d = UiKit.Text(desc, "desc", TextKind.Sub, "전투를 진행하여 보상을 받\n으세요!", "stage_ink");
            d.fontStyle = FontStyles.Bold;
            d.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Place(d.rectTransform, 0f, 0f, inner * 0.5f, descH);
            float priceW = UiKit.L("pass_price_w") * w * 1.2f, priceH = UiKit.H("pass_price_h");
            Button price = UiKit.Button(desc, "price", () => h.Toast("💎 프리미엄 패스는 데모 버전에서 지원하지 않습니다"));
            RectTransform prt = price.GetComponent<RectTransform>();
            UiKit.Place(prt, inner * 0.75f - priceW * 0.5f, (descH - priceH) * 0.5f, priceW, priceH);
            UiKit.Rounded(prt, "line", "pp_line", rem * 0.3f);
            Image pface = UiKit.Rounded(prt, "face", "pass_price", rem * 0.2f);
            PopupKit.Inset(pface.rectTransform, PopupKit.Line3);
            TextMeshProUGUI pt = UiKit.Text(prt, "label", TextKind.Sub, h.Meta.Pass.PremiumPriceKr, "stage_ink");
            pt.fontStyle = FontStyles.Bold;
            PopupKit.Ring(pt, "pass_card", "pp_line");

            // [무료 | 프리미엄] 탭 행
            y += descH + rem * 1.31f;
            RectTransform header = UiKit.Box(card, "header");
            UiKit.Place(header, PopupKit.Line3, y, inner, headerH);
            UiKit.Panel(header, "line", "pp_line");
            Image free = UiKit.Panel(header, "free", "pp_blue");
            free.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            free.rectTransform.offsetMin = new Vector2(0f, PopupKit.Line3);
            free.rectTransform.offsetMax = new Vector2(-PopupKit.Line3 * 0.5f, -PopupKit.Line3);
            Image prem = UiKit.Panel(header, "premium", "pass_tab_prem");
            prem.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            prem.rectTransform.offsetMin = new Vector2(PopupKit.Line3 * 0.5f, PopupKit.Line3);
            prem.rectTransform.offsetMax = new Vector2(0f, -PopupKit.Line3);
            TextMeshProUGUI ft = UiKit.Text(free.transform, "label", TextKind.Sub, "무료", "stage_ink");
            ft.fontStyle = FontStyles.Bold;
            TextMeshProUGUI prt2 = UiKit.Text(prem.transform, "label", TextKind.Sub, "프리미엄", "stage_ink");
            prt2.fontStyle = FontStyles.Bold;

            // 흰 트랙
            y += headerH;
            RectTransform track = UiKit.Box(card, "track");
            UiKit.Place(track, PopupKit.Line3, y, inner, trackH);
            UiKit.Panel(track, "bg", "pass_track");
            RectTransform content = PopupKit.ScrollList(track, "list", 0f, 0f, 0f);

            int bestAbs = h.S.BestAbsChapter(SaveIo.Defs);
            int bestStage = h.S.BestStage;
            float railW = UiKit.L("pass_rail_w") * w;
            float labelW = UiKit.L("pass_label_w") * w * 1.3f, labelH = PopupKit.FontSize(TextKind.Sub) * 1.4f;
            float cellPad = rem * 0.5f;
            float pillH = PopupKit.FontSize(TextKind.Sub) * 1.3f;
            List<PassMilestone> ms = h.Meta.Pass.Milestones;
            for (int i = 0; i < ms.Count; i++)
            {
                PassMilestone m = ms[i];
                int c = int.Parse(m.Stage.Split('-')[0]);
                bool reached = h.Pass.Reached(m.Stage, bestAbs, bestStage);
                bool claimed = h.Pass.Claimed(h.PassState, m.Stage);
                int lines = Mathf.Max(m.Free.Count, m.Premium.Count);
                float cellH = cellPad * 2f + lines * pillH + (lines - 1) * rem * 0.18f;
                float segH = rem * 0.63f + labelH + rem * 1.03f + cellH + rem * 0.2f;
                RectTransform seg = PopupKit.Item(content, "seg-" + m.Stage, -1f, segH);
                // 가운데 레일(도달 = 파랑 · 미도달 = 죽은 색)
                Image rail = UiKit.Panel(seg, "rail", reached ? "pass_rail" : "pass_rail_dead");
                UiKit.Anchor(rail.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, railW, segH);
                rail.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                rail.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                rail.rectTransform.sizeDelta = new Vector2(railW, 0f);
                // 마일스톤 필
                RectTransform lab = UiKit.Box(seg, "label");
                UiKit.Anchor(lab, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -rem * 0.63f), labelW, labelH);
                UiKit.Rounded(lab, "line", "pp_line", labelH * 0.5f);
                Image labFace = UiKit.Rounded(lab, "face", reached ? "pass_label_lit" : "pass_label", labelH * 0.5f - PopupKit.Line);
                PopupKit.Inset(labFace.rectTransform, PopupKit.Line);
                TextMeshProUGUI labT = UiKit.Text(lab, "text", TextKind.Sub, SaveIo.Defs.StageDifficultyLabel(c, 0) + " " + m.Stage, "stage_ink");
                labT.fontStyle = FontStyles.Bold;
                PopupKit.Ring(labT, "pass_card", "pp_line");
                // 보상 칸 둘
                float rowY = rem * 0.63f + labelH + rem * 1.03f;
                float gap = rem * 1.6f;
                float cellW = (inner - gap) * 0.5f - rem * 0.2f;
                string stage = m.Stage;
                RectTransform freeCell = Cell(seg, "free", rem * 0.2f + PopupKit.Line3 * 0f, rowY, cellW, cellH, claimed || reached ? "pass_cell_lit" : "pass_cell", m.Free, claimed || reached ? "pass_pill_lit" : "pass_pill", pillH, cellPad);
                if (claimed)
                {
                    float badge = rem * 1.45f;
                    RectTransform chk = UiKit.Box(freeCell, "check");
                    UiKit.Anchor(chk, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-rem * 0.4f, 0f), badge, badge);
                    UiKit.Circle(chk, "line", "pp_line");
                    Image cf = UiKit.Circle(chk, "face", "pp_green");
                    PopupKit.Inset(cf.rectTransform, PopupKit.Line);
                    // T89 — 정본 `ui.js` 4905 는 `<span class="pass-badge check">${IconGen.img('check')}</span>` 다.
                    // «✓»(U+2713)는 글꼴에 없어 □ 로 찍혔다 — T31 아이콘으로.
                    Image ck = UiKit.Icon(chk, "mark", "check");
                    PopupKit.Inset(ck.rectTransform, badge * 0.24f);
                }
                else if (reached)
                {
                    Button b = UiKit.Button(freeCell, "claim", () => OnClaim(h, stage, freeCell));   // 정본 5004 from = 그 칸(cell)
                    b.transform.SetAsLastSibling();
                }
                RectTransform premCell = Cell(seg, "premium", cellW + gap + rem * 0.2f, rowY, cellW, cellH, "pass_cell", m.Premium, "pass_pill", pillH, cellPad);
                Image lockIco = PopupKit.IconOr(premCell, "lock", "lock");
                UiKit.Anchor(lockIco.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-rem * 0.35f, -rem * 0.3f), rem, rem);
                UiKit.Button(premCell, "hit", () => h.Toast("💎 프리미엄 패스는 데모 버전에서 지원하지 않습니다")).transform.SetAsLastSibling();
            }
            PopupKit.Spacer(content, rem * 0.6f);

            PopupKit.XButton(card, () => Close(h));
        }

        private static RectTransform Cell(Transform parent, string name, float x, float y, float w, float h, string faceKey, OrderedMap<double> reward, string pillKey, float pillH, float pad)
        {
            RectTransform cell = UiKit.Box(parent, name);
            UiKit.Place(cell, x, y, w, h);
            PopupKit.Outlined(cell, "face", faceKey, UiKit.H("pass_cell_r"), PopupKit.Line3);
            for (int i = 0; i < reward.Count; i++)
            {
                RectTransform pill = UiKit.Box(cell, "pill-" + reward.KeyAt(i));
                UiKit.Place(pill, pad, pad + i * (pillH + PopupKit.Rem * 0.18f), w * 0.85f - pad, pillH);
                UiKit.Rounded(pill, "bg", pillKey, pillH * 0.5f);
                Image ico = PopupKit.IconOr(pill, "ico", ShopSheet.CurIcon(reward.KeyAt(i)));
                UiKit.Place(ico.rectTransform, PopupKit.Rem * 0.3f, pillH * 0.1f, pillH * 0.8f, pillH * 0.8f);
                TextMeshProUGUI t = UiKit.Text(pill, "amt", TextKind.Sub, PopupKit.Fmt(reward.ValueAt(i)), "stage_ink", TextAlignmentOptions.Left);
                t.fontStyle = FontStyles.Bold;
                t.rectTransform.offsetMin = new Vector2(PopupKit.Rem * 0.3f + pillH * 0.9f, 0f);
                PopupKit.Ring(t, "pass_card", "pp_line");
            }
            return cell;
        }

        private static void OnClaim(MetaHost h, string stage, RectTransform from)
        {
            if (!h.Pass.Claim(h.PassState, h.Wallet, stage, h.S.BestAbsChapter(SaveIo.Defs), h.S.BestStage)) return;
            PassMilestone m = null;
            foreach (PassMilestone x in h.Meta.Pass.Milestones) if (x.Stage == stage) { m = x; break; }
            // 정본 ui.js 5004 — m.free 를 그 칸에서 터뜨린다 · 토스트 없음 · T134 3회차
            if (m != null) RewardBurst.Play(RewardBurst.Rewards(m.Free, null), from);
            h.Touch();
        }
    }
}
