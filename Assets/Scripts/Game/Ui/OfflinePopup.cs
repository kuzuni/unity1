using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Save;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 오프라인 보상 팝업(ROUTINE T22 · 원작 ui.js showOffline/updateOfflineValues/onCollectOffline · T13 <see cref="Offline"/> 와 짝 — 여기서는 화면만).
    /// 위 = 제목 · 수집 시간(최대) · 수급률 둘 / 아래 = 합계 · [수집]. 켜둔 채로 누적이 자라므로 1초마다 수치만 갱신한다. ✕ 는 단순 닫힘(보상은 남는다).
    /// 머리 판(T68 · 정본 `style.css` `.offline-top`): 카드 높이의 42.80% · 평면 어두운 판 · 흰 제목 · «수집 시간:» 회색 · 시간·요율 초록 ·
    /// 요율은 `flex-direction: column`(원형 아이콘 **위** · 글자 **아래** · 두 칸 사이 2.4rem) · 수집 버튼 우상단 `.offline-collect-dot`(.7rem 빨간 원 · 흰 테).
    /// 정본 `#0e111b`·`#ccc` 와 같은 키는 catalog.json 에 없어(T62 lock · 그 뒤 추가) 가장 가까운 `pp_ink`·`pp_gray` 를 쓴다.
    /// </summary>
    public static class OfflinePopup
    {
        public const string Name = "offline";
        private static TextMeshProUGUI counted, max, coins, hammers;
        private static RectTransform collectBtn;   // T134 3회차 — 수령 연출의 시작점([수집] · 정본 5941)
        private static float acc;

        public static void Show(MetaHost h, OfflineReward o)
        {
            Popup p = h.Popups.Show(Name, null, PopupZUi.AboveTabBar(Name));   // T346 — 정본 `#offline-modal` z 40 > 탭바 30(표 PopupZUi)
            if (p.Root.childCount > 1) { Update(o); return; }
            float rem = PopupKit.Rem, w = UiKit.RefW, H = UiKit.RefH;
            float cardW = UiKit.L("offline_w") * w - PopupKit.Line3 * 2f, outerH = UiKit.L("offline_h") * H;   // T473 — 표값은 원작 카드 몸 · Card 의 w 는 패딩 상자
            float cardH = outerH - PopupKit.Line3 * 2f;   // T465 — 표값은 원작 카드 몸(border-box) · PopupKit.Card 의 h 는 패딩 상자
            RectTransform card = PopupKit.Card(p.Root, "card", cardW, cardH, "pp_paper", rem);
            float inner = cardW;   // T473 — rect 가 곧 테 안쪽
            float topH = outerH * TopFrac;
            RectTransform top = UiKit.Box(card, "top");
            UiKit.Place(top, 0f, 0f, inner, topH);
            UiKit.Panel(top, "bg", "pp_ink").color = PinnedColorUi.C(TopFaceKey);   // T377 22회차 — 정본 256 .offline-top #0e111b(자리 전용 키 · 카탈로그 pp_ink 가 아니다)
            float y = rem * 1.3f;
            TextMeshProUGUI title = UiKit.Text(top, "title", TextKind.Title2, "오프라인 보상", "stage_ink");   // T404 ⓑ — 정본 267 `.offline-title { font-size: 1.2rem }` = 43.7px → Title2 42(전엔 Title 60 · +37%)
            title.fontStyle = FontStyles.Bold;
            float titleH = PopupKit.FontSize(TextKind.Title2) * 1.25f;
            UiKit.Place(title.rectTransform, 0f, y, inner, titleH);
            PopupKit.Ring(title, "sheet_title", "pp_line");   // 정본 .offline-title .11em
            y += titleH + rem * 0.15f;
            float lineH = PopupKit.FontSize(TextKind.Sub) * 1.3f;
            TextMeshProUGUI subL = UiKit.Text(top, "sub", TextKind.Sub, "수집 시간:", SubInkKey, TextAlignmentOptions.Right);
            subL.fontStyle = FontStyles.Bold;
            // T396 3회차 — 정본 267 `.offline-sub { color: #ccc }` 는 **선택자에 못박은 잉크**다. 종전 클론은 전역 `pp_gray`(#c4c4c4)로
            //   «가장 가까운 토큰» 을 골라 찍고 있었다(이 파일의 옛 주석이 그 바꿔치기를 그대로 적어 뒀다). 값은 표가 쥔다(§1).
            subL.color = PinnedColorUi.C(SubPinnedInk);
            UiKit.Place(subL.rectTransform, 0f, y, inner * 0.45f, lineH);
            counted = UiKit.Text(top, "counted", TextKind.Sub, string.Empty, GreenKey, TextAlignmentOptions.Left);
            counted.fontStyle = FontStyles.Bold;
            UiKit.Place(counted.rectTransform, inner * 0.47f, y, inner * 0.3f, lineH);
            max = UiKit.Text(top, "max", TextKind.Sub, string.Empty, SubInkKey, TextAlignmentOptions.Left);
            max.fontStyle = FontStyles.Bold;
            max.color = PinnedColorUi.C(SubPinnedInk);   // 같은 줄의 짝 — 정본도 한 선택자(.offline-sub)가 둘을 덮는다
            UiKit.Place(max.rectTransform, inner * 0.77f, y, inner * 0.23f, lineH);
            y += lineH + rem * 1.2f;
            // 요율 둘 — 세로(아이콘 위 · 글자 아래) · 두 칸 사이 2.4rem · 행 전체를 가운데에
            float circle = rem * 2.6f, gap = rem * 2.4f, rateW = Mathf.Max(circle, inner * 0.36f);
            float rateH = circle + rem * 0.3f + lineH;
            // 글자 하한(60/36px)이 정본 rem 글자보다 커서 정본 여백(1.65rem)을 그대로 두면 행이 판 밖으로 15px 넘친다(CI 런 113) — 판 안에 붙인다
            y = Mathf.Min(y, topH - rateH - rem * 0.5f);
            float rx = (inner - (rateW * 2f + gap)) * 0.5f;
            Rate(top, "coin", "coin", PopupKit.FmtDec(o.CoinRate) + "/초", rx, y, rateW, circle, lineH);
            Rate(top, "hammer", "hammer", PopupKit.FmtDec(o.HammerRate) + "/분", rx + rateW + gap, y, rateW, circle, lineH);

            RectTransform bottom = UiKit.Box(card, "bottom");
            float bottomH = cardH - topH;
            UiKit.Place(bottom, 0f, topH, inner, bottomH);
            // T155 — 정본 `.offline-bottom { padding: 1.1rem .9rem 1.3rem; justify-content: center; gap: 1.79rem }`(style.css 279~281):
            //        합계줄 + [수집] 덩어리를 흰 몸통 **세로 가운데**에 둔다(종전엔 위에서 아래로 쌓아 아래가 통째로 비었다 · 검수 Q 런 364).
            //        [수집] 은 이 버튼만의 치수 `.offline-collect-btn { width: 51.7%; height: 4.6rem }`(306) — 그 주석이 원본 파란 면을 29.80%W × 7.49%H 로 실측해 두었다.
            //        면 치수를 표(offline_collect_w/h)에 두고 상자 = 면 + 테(line3) · 아래턱(btn_lip) 이다(PopupKit.Btn 이 면을 그만큼 안으로 그린다).
            float lip = UiKit.H("btn_lip");
            float bw = UiKit.W("offline_collect_w") + PopupKit.Line3 * 2f, bh = UiKit.H("offline_collect_h") + PopupKit.Line3 * 2f + lip;
            float padT = rem * BottomPadTopRem, padB = rem * BottomPadBottomRem, gapV = rem * BottomGapRem;
            // T404 2회차 — 정본 305 `.offline-total { font-size: 1.15rem }` = 41.9px → Title2 42(전엔 Sub 36 · −14%). 합계 줄 높이·아이콘도 그 글자 기준.
            float totalH = PopupKit.FontSize(TextKind.Title2) * 1.3f;
            float blockH = totalH + gapV + bh;
            float by = padT + Mathf.Max(0f, (bottomH - padT - padB - blockH) * 0.5f);
            float ico = totalH;
            RectTransform total = UiKit.Box(bottom, "total");
            UiKit.Place(total, 0f, by, inner, totalH);
            Image ci = PopupKit.IconOr(total, "coin-ico", "coin");
            UiKit.Place(ci.rectTransform, inner * 0.18f, 0f, ico, ico);
            // 정본 `.offline-total { color: #fff; -webkit-text-stroke-width: .2em }` — **흰 칠 + 검정 링**이다.
            // (정본 style.css 290~296 이 «검정으로 오독하기 쉽다 · 다시 검정으로 돌리지 말 것» 이라고 못 박아 뒀다 · T109 2회차가 키라인을 붙이며 드러났다)
            coins = UiKit.Text(total, "coins", TextKind.Title2, string.Empty, "white", TextAlignmentOptions.Left);   // 정본 .offline-total 1.15rem(T404)
            coins.fontStyle = FontStyles.Bold;
            UiKit.OutlinePx(coins, "pp_line", KeylineUi.Em("offline_total", coins.fontSize));   // 정본 .offline-total { -webkit-text-stroke: .2em var(--pp-line) }
            UiKit.Place(coins.rectTransform, inner * 0.18f + ico + rem * 0.2f, 0f, inner * 0.25f, totalH);
            Image hi = PopupKit.IconOr(total, "hammer-ico", "hammer");
            UiKit.Place(hi.rectTransform, inner * 0.55f, 0f, ico, ico);
            hammers = UiKit.Text(total, "hammers", TextKind.Title2, string.Empty, "white", TextAlignmentOptions.Left);   // 정본 .offline-total color:#fff · 1.15rem(T404)
            hammers.fontStyle = FontStyles.Bold;
            UiKit.OutlinePx(hammers, "pp_line", KeylineUi.Em("offline_total", hammers.fontSize));   // 정본 .offline-total(같은 줄의 두 수)
            UiKit.Place(hammers.rectTransform, inner * 0.55f + ico + rem * 0.2f, 0f, inner * 0.25f, totalH);
            by += totalH + gapV;
            // T144 — 정본은 파랑: 668 `.btn.primary`(초록 · 0-2-0)를 3548 `.modal-card .btn.primary { background: var(--pp-blue) }`(0-3-0)가 덮고
            //        오프라인 카드는 `modal-card offline-card`(ui.js 5891)다 · 307 주석의 «원본 파란 면 실측» 도 같은 말 · T68 의 «초록이 맞다» 는 기본 규칙만 본 오독
            Button collect = PopupKit.Btn(bottom, "collect", "수집", "pp_blue", "pp_blue_dk", () => Collect(h), bw, bh);
            UiKit.Place(collect.GetComponent<RectTransform>(), (inner - bw) * 0.5f, by, bw, bh);
            CollectDot(collect.GetComponent<RectTransform>(), bw, rem);
            collectBtn = collect.GetComponent<RectTransform>();

            PopupKit.XButton(card, () => Close(h));
            Update(o);
        }

        /// <summary>정본 `.offline-top` — 카드 높이의 42.80%(원본 헤더 23.27%H / 카드 콘텐츠 54.37%H). 어두운 판 `#0e111b` 은 카탈로그에 가장 가까운 `pp_ink`(T62 lock 뒤 키 추가).</summary>
        public const float TopFrac = 0.4280f;
        public const string TopFaceKey = "offline_top_face";   // T377 22회차 — 정본 256 `.offline-top { background: #0e111b }`(PinnedColorUi) · 전엔 전역 pp_ink #17181a
        /// <summary>정본 `.offline-sub` 글자 — 종류·하한을 위해 남겨 둔 **카탈로그 폴백** 키(값은 아래 못박은 잉크가 덮는다).</summary>
        public const string SubInkKey = "pp_gray";
        /// <summary>T396 3회차 — 정본 267 `.offline-sub { color: #ccc }` 를 그대로 쥔 표 키(`PinnedColorUi.json`). 전엔 `pp_gray`(#c4c4c4)로 근사했다.</summary>
        public const string SubPinnedInk = "offline_sub_ink";
        /// <summary>정본 `--pp-green`(시간·요율·해머 원판) = 카탈로그 `offline_green`.</summary>
        public const string GreenKey = "offline_green";
        /// <summary>정본 `.offline-collect-dot` — .7rem 빨간 원 · 흰 테(`--ol2`) · 버튼 우상단(top/right −.3rem).</summary>
        public const float DotRem = 0.7f, DotOffsetRem = 0.3f;
        /// <summary>정본 `.offline-bottom { padding: 1.1rem .9rem 1.3rem; gap: 1.79rem }`(T155 · style.css 279~281) — 합계줄·[수집] 덩어리를 세로 가운데에 두는 셈의 상수.</summary>
        public const float BottomPadTopRem = 1.1f, BottomPadBottomRem = 1.3f, BottomGapRem = 1.79f;

        /// <summary>정본 `.offline-rate`(flex column · align center · gap .3rem): 원형 아이콘 2.6rem **위**, 초록 글자 **아래**.</summary>
        private static void Rate(Transform parent, string name, string icon, string text, float x, float y, float w, float circleD, float lineH)
        {
            RectTransform box = UiKit.Box(parent, name);
            UiKit.Place(box, x, y, w, circleD + PopupKit.Rem * 0.3f + lineH);
            RectTransform circle = UiKit.Box(box, "circle");
            UiKit.Place(circle, (w - circleD) * 0.5f, 0f, circleD, circleD);
            // T417 — 정본 style.css 는 이 배지를 두 번 말하고 **뒤엣것이 이긴다**: 앞 272~277 이 `border: ol2 solid #000` 과 `.coin { #ffb300 }`·`.hammer { pp-green }`
            // 색 원을 주지만 파일 끝 7268~7269(최종값)가 `background: none; border: none` 으로 끄고 `.ico { width/height: 100% }` 로 아이콘이 칸을 꽉 채운다
            // (7262 주석 «아이콘 자체가 금속·보석 재질을 가지므로 배지 색·테두리를 빼고 꽉 채운다»). 클론은 앞 규칙에서 멈춰 line·face 두 원 + 인셋 20% 였다(런 872 —
            // 코인 금테가 주황 면에 먹히고 정본엔 없는 초록 원이 해머 뒤에 섰다). 배지 칸(2.6rem)과 글줄 간격은 정본 272~274 그대로다.
            Image ico = PopupKit.IconOr(circle, "ico", icon);
            PopupKit.Inset(ico.rectTransform, 0f);
            TextMeshProUGUI t = UiKit.Text(box, "text", TextKind.Sub, text, GreenKey, TextAlignmentOptions.Center);
            t.fontStyle = FontStyles.Bold;
            UiKit.Place(t.rectTransform, 0f, circleD + PopupKit.Rem * 0.3f, w, lineH);
        }

        /// <summary>정본 `.offline-collect-dot` — 수집 버튼 우상단에 반쯤 걸친 빨간 점(흰 테).</summary>
        private static void CollectDot(RectTransform button, float buttonW, float rem)
        {
            float d = rem * DotRem, off = rem * DotOffsetRem;
            RectTransform dot = UiKit.Box(button, "dot");
            UiKit.Place(dot, buttonW - d + off, -off, d, d);
            UiKit.Circle(dot, "line", "white");
            Image face = UiKit.Circle(dot, "face", "pp_red");
            PopupKit.Inset(face.rectTransform, UiKit.L("line2_px"));
            face.raycastTarget = false;
        }

        /// <summary>원작 updateOfflineValues — 숫자만 갱신(버튼 노드를 매초 갈지 않는다).</summary>
        public static void Update(OfflineReward o)
        {
            if (counted == null || o == null) return;
            counted.text = PopupKit.FmtTime(o.Counted);
            max.text = o.Elapsed > o.Counted ? "(최대)" : string.Empty;
            coins.text = PopupKit.Fmt(o.Coins);
            hammers.text = PopupKit.Fmt(o.Hammers);
        }

        /// <summary>MetaHost 의 1초 틱이 부른다.</summary>
        public static void Tick(MetaHost h)
        {
            if (!h.Popups.IsOpen(Name) || SaveIo.Instance == null) return;
            Update(SaveIo.Instance.PendingOffline());
        }

        private static void Collect(MetaHost h)
        {
            OfflineReward r = SaveIo.Instance != null ? SaveIo.Instance.ClaimOffline() : null;
            if (r == null) { h.Toast("💤 아직 누적된 오프라인 보상이 없습니다"); Close(h); return; }
            // 정본 ui.js 5941 — 수령 연출은 팝업을 닫기 **전에**(시작점 좌표는 호출 시점에 잡히고 연출 층은 닫히는 팝업 위에서 이어진다) · T134 3회차
            RewardBurst.Play(RewardBurst.Rewards("coins", r.Coins, "hammers", r.Hammers), collectBtn);
            Close(h);
            h.Touch();
        }

        public static void Close(MetaHost h)
        {
            h.Popups.Hide(Name);
            counted = max = coins = hammers = null;
            collectBtn = null;
        }
    }
}
