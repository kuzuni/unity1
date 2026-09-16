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
            h.Popups.Show(Name, null, PopupZUi.AboveTabBar(Name));   // T346 2회차 — 정본 `#pass-modal` z 40 > 탭바 30(표 PopupZUi.json) · 딤이 하단 네비를 덮는다
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
            // T378 5회차 — 표에 **원작 실측**(리본 41px/890 = 4.61%H)이 이미 들어 있는데 코드가 ×1.3 을 얹고 있었다.
            //   정본 `.pass-banner`(style.css 2705~2713)는 높이를 안 주고 `padding .49rem .5rem` + `font-size 1.05rem/line-height 1.15` 로 **내용이 정한다** —
            //   클론은 그 결과 높이를 표로 받으므로 곱을 얹을 자리가 없다.
            float bannerH = UiKit.H("pass_banner_h");
            float descH = PopupKit.FontSize(TextKind.Sub) * 2.8f;
            // T378 5회차 — 정본 `.pass-header-row span`(2764~2765)은 `padding .2rem 0 · line-height 1` 이라 역시 내용이 높이를 정한다. 표값 2.92%H 가 그 실측이다.
            float headerH = UiKit.H("pass_header_h");
            float trackH = UiKit.H("pass_track_h");
            float cardH = padTop + bannerH + rem * 1.19f + descH + rem * 1.31f + headerH + trackH + padBottom;
            RectTransform card = PopupKit.Card(root, "card", cardW, cardH, "pass_bg", rem);
            // T331 25회차 — 정본 8602 `.modal-card.pass-card { box-shadow: 0 1.05rem 1.6rem -.5rem rgba(0,0,0,.6) }`.
            //   ⓐ CSS `box-shadow` 는 **겹치지 않는다** — 이 한 줄이 `.modal-card`(3518)의 딱딱한 턱 `0 .5rem 0` 을
            //     통째로 갈아 끼운다. 정본 주석도 그 뜻을 못 박았다: «어두운 `.pass-card` 는 3차 블록이 `:not()` 으로
            //     빼 둔 카드라 여기서도 따로 적는다 — 칠은 여전히 안 주고 **그림자만** 준다»(8592~8593).
            //     그래서 `PopupKit.Card` 가 모든 카드에 깔아 준 턱을 이 카드에서만 걷는다(안 걷으면 두 겹이 된다).
            //   ⓑ 번짐이 **음수**인 유일한 자리다(`-.5rem` — 판을 안으로 줄여 굽는다 · 표 `passcard_drop`).
            UiShadow.Remove(card, "card_lip");
            UiShadow.Remove(card, "modalcard_cast");   // 34회차 — 8602 는 **목록을 통째로** 갈아 끼운다(앰비언트도 없다)
            UiShadow.Drop(card, "passcard_drop", rem);
            // T132 — 정본 ui.js 4924 `<div class="pass-sword">${IconGen.img('passsword')}</div>` · style.css 2686: 카드 윗변에서 4.81rem 위 · 가운데 · 3.72×6.72rem.
            // 리본(.pass-banner)보다 먼저 세운다 — 정본 DOM 순서대로 리본이 칼자루 위를 덮는다. 치수는 StaticIconsUi.json(§1).
            Image sword = PopupKit.IconOr(card, "pass-sword", "passsword");
            UiKit.Anchor(sword.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -StaticIconsUi.Rem("pass_sword_top_rem")), StaticIconsUi.Rem("pass_sword_w_rem"), StaticIconsUi.Rem("pass_sword_h_rem"));
            DropShadow.Apply(sword, "pass_sword");   // T332 — 정본 2698 `drop-shadow(.14rem .18rem .16rem rgba(0,0,0,.45))` · 자리를 잡은 **뒤**에 부른다
            float inner = cardW - PopupKit.Line3 * 2f;
            float padX = rem * 1.1f;

            // 리본(카드 좌우를 넘는다)
            float ribbonW = cardW + rem * 2.55f * 2f;
            RectTransform ribbon = UiKit.Box(card, "banner");
            UiKit.Anchor(ribbon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -padTop), ribbonW, bannerH);
            // T159 2회차 — 정본 2722~2723 은 꼬리 바깥 변에 깊이 31% 의 V 홈을 판다(`clip-path`). 정본 주석이 실측까지 적어 뒀다:
            // «기존 클론은 세로 중앙의 단순 ◀ 삼각형이라 꼬리까지 합친 폭이 79.16%(원본 88.32%)에 그쳤다». 여태 민무늬 직사각형이었다.
            float tailW = rem * 2.3f, tailH = rem * 2.44f, tailY = rem * 0.31f;
            ClipShape.Face(ribbon, "tail-l", "pass_tail_l", 0f, tailY, tailW, tailH, "pass_banner_dk");
            ClipShape.Face(ribbon, "tail-r", "pass_tail_r", ribbonW - tailW, tailY, tailW, tailH, "pass_banner_dk");
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
            // T383 9회차 — 정본 2734 `.pass-desc { font-size: .78rem }`(= 28.4px): 하한 `Sub`(36)로 찍으면 「보상을 받」 이 반 칸(inner/2)에 안 들어가
            // 정본 `<br>` 두 줄(ui.js 4928)이 세 줄이 된다(런 666). 결정 633 대로 새 종류 없이 예외 칸 `Micro` 를 쓰되 크기는 표(TextSizeUi)에서 — §1 예외 여섯째 자리.
            TextMeshProUGUI d = UiKit.Text(desc, "desc", TextKind.Micro, "전투를 진행하여 보상을 받\n으세요!", "stage_ink");
            TextSizeUi.Apply(d, "pass_desc");
            d.fontStyle = FontStyles.Bold;
            d.textWrappingMode = TextWrappingModes.Normal;
            // T354 15회차 — 정본 2734 `.pass-desc { line-height: 1.4 }`. 이 글은 정본이 `<br>` 로 나눈 **두 줄**이라(T383)
            // 줄 간격이 눈에 보이는 자리다 — TMP 자산 기본 1.448 이 그대로 서면 정본보다 한 줄 걸러 1.7px 씩 벌어진다.
            LineHeight.Apply(d, "pass_desc_lh");
            UiKit.Place(d.rectTransform, 0f, 0f, inner * 0.5f, descH);
            // T378 5회차 — 가격 페넌트(정본 2739 `.pass-price`)는 `width: fit-content` 라 폭이 글자에서 나고, 표의 15.98%W 가 그 원작 실측이다. ×1.2 는 클론이 얹은 곱이다.
            float priceW = UiKit.L("pass_price_w") * w, priceH = UiKit.H("pass_price_h");
            Button price = UiKit.Button(desc, "price", () => h.Toast("💎 프리미엄 패스는 데모 버전에서 지원하지 않습니다"));
            RectTransform prt = price.GetComponent<RectTransform>();
            UiKit.Place(prt, inner * 0.75f - priceW * 0.5f, (descH - priceH) * 0.5f, priceW, priceH);
            // T159 3회차 — 정본 2739 는 둥근 사각이 아니라 **아래가 뾰족한 페넌트**(`--pen`)다. 검정 바깥 층과 3px 안쪽 주황 면이
            // **같은 clip** 을 쓴다(정본 주석: «테두리 속성은 clip-path 에 잘려 쓸 수 없음»). 여태 둥근 사각 두 장이라 꼭짓점이 없었다.
            ClipShape.Face(prt, "price-line", "pass_pennant", 0f, 0f, priceW, priceH, "pp_line");
            ClipShape.Face(prt, "price-face", "pass_pennant", PopupKit.Line3, PopupKit.Line3,
                priceW - PopupKit.Line3 * 2f, priceH - PopupKit.Line3 * 2f, "pass_price");
            TextMeshProUGUI pt = UiKit.Text(prt, "label", TextKind.Sub, h.Meta.Pass.PremiumPriceKr, "stage_ink");
            WrapUi.Apply(pt, "pass_price");   // T361 7회차 — 정본 white-space 표(WrapUi.json) 2744 `.pass-price { nowrap }`
            pt.fontStyle = FontStyles.Bold;
            // 정본 `padding: .92rem 1.16rem 1.23rem` — 아래가 .31rem 넓다(꼭짓점 몫). 그 차이만큼 글자를 올려 꼭짓점과 안 겹치게 한다.
            float penLift = (ClipShape.Num("pass_pennant", "pad_bottom_rem") - ClipShape.Num("pass_pennant", "pad_top_rem")) * rem;
            pt.rectTransform.offsetMin = new Vector2(pt.rectTransform.offsetMin.x, pt.rectTransform.offsetMin.y + penLift);
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
            // T375 — 박힌 곱 넷을 걷었다(§1 «수치는 코드에 박지 않는다»). 정본은 이 넷을 전부 **값으로** 정해 둔다:
            //   필 폭 `calc(var(--app-w) * .206)`(style.css 2801) · 필 높이는 `padding .1rem 0 + line-height 1`(같은 줄 · 원작 실측 18.3/488W)
            //   칸 패딩 `.4rem .5rem`(2824 — 세로와 가로가 **다르다**) · 보상 알약 `padding .1rem .6rem` + 한 줄(2830).
            //   종전 클론은 폭에 ×1.3 을 곱하고 높이 셋을 글꼴에서 뽑아, 필이 +26% 넓고 보상 행 피치가 +10% 였다(T28 64회차 실측).
            float labelW = UiKit.L("pass_label_w") * w, labelH = UiKit.L("pass_label_h") * w;
            float cellPadY = rem * UiKit.L("pass_cell_pad_y_rem"), cellPadX = rem * 0.5f;
            float pillH = rem * UiKit.L("pass_reward_pill_h_rem");
            List<PassMilestone> ms = h.Meta.Pass.Milestones;
            for (int i = 0; i < ms.Count; i++)
            {
                PassMilestone m = ms[i];
                int c = int.Parse(m.Stage.Split('-')[0]);
                bool reached = h.Pass.Reached(m.Stage, bestAbs, bestStage);
                bool claimed = h.Pass.Claimed(h.PassState, m.Stage);
                int lines = Mathf.Max(m.Free.Count, m.Premium.Count);
                float cellH = cellPadY * 2f + lines * pillH + (lines - 1) * rem * 0.18f;
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
                RectTransform freeCell = Cell(seg, "free", rem * 0.2f + PopupKit.Line3 * 0f, rowY, cellW, cellH, claimed || reached ? "pass_cell_lit" : "pass_cell", m.Free, claimed || reached ? "pass_pill_lit" : "pass_pill", pillH, cellPadX, cellPadY);
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
                RectTransform premCell = Cell(seg, "premium", cellW + gap + rem * 0.2f, rowY, cellW, cellH, "pass_cell", m.Premium, "pass_pill", pillH, cellPadX, cellPadY);
                Image lockIco = PopupKit.IconOr(premCell, "lock", "lock");
                UiKit.Anchor(lockIco.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-rem * 0.35f, -rem * 0.3f), rem, rem);
                UiKit.Button(premCell, "hit", () => h.Toast("💎 프리미엄 패스는 데모 버전에서 지원하지 않습니다")).transform.SetAsLastSibling();
            }
            PopupKit.Spacer(content, rem * 0.6f);

            PopupKit.XButton(card, () => Close(h));
        }

        private static RectTransform Cell(Transform parent, string name, float x, float y, float w, float h, string faceKey, OrderedMap<double> reward, string pillKey, float pillH, float padX, float padY)
        {
            RectTransform cell = UiKit.Box(parent, name);
            UiKit.Place(cell, x, y, w, h);
            PopupKit.Outlined(cell, "face", faceKey, UiKit.H("pass_cell_r"), PopupKit.Line3);
            // T159 3회차 — 정본 2864~2874: 칸 **아래**(top:100%)에 꼬리 삼각형이 붙는다(검정 층 + 칸 색 면 층 · `background: inherit`).
            // 무료 칸은 수직변이 오른쪽 · 프리미엄은 거울상. 클론엔 이 꼬리가 아예 없었다.
            bool free = name == "free";
            string tail = free ? "pass_cell_tail_free" : "pass_cell_tail_prem";
            float rem0 = PopupKit.Rem, ol3 = PopupKit.Line3;
            float tw = ClipShape.Num(tail, "w_rem") * rem0, th = ClipShape.Num(tail, "h_rem") * rem0;
            float faceX = free ? w - tw : 0f;
            ClipShape.Face(cell, "tail-line", tail, faceX + (free ? ol3 : -ol3), h, tw + ol3, th + ol3, "pp_line");
            ClipShape.Face(cell, "tail-face", tail, faceX, h, tw, th, faceKey);
            for (int i = 0; i < reward.Count; i++)
            {
                RectTransform pill = UiKit.Box(cell, "pill-" + reward.KeyAt(i));
                UiKit.Place(pill, padX, padY + i * (pillH + PopupKit.Rem * 0.18f), w * 0.85f - padX, pillH);
                UiKit.Rounded(pill, "bg", pillKey, pillH * 0.5f);
                Image ico = PopupKit.IconOr(pill, "ico", ShopSheet.CurIcon(reward.KeyAt(i)));
                UiKit.Place(ico.rectTransform, PopupKit.Rem * 0.3f, pillH * 0.1f, pillH * 0.8f, pillH * 0.8f);
                TextMeshProUGUI t = UiKit.Text(pill, "amt", TextKind.Sub, PopupKit.Fmt(reward.ValueAt(i)), "stage_ink", TextAlignmentOptions.Left);
                t.fontStyle = FontStyles.Bold;
                TabularText.Apply(t);   // T352 ⓒ — 정본 8637 `.pass-cell span:not(.pass-badge) { font-variant-numeric: tabular-nums }`
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
