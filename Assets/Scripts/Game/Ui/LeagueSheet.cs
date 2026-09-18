using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Meta;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 리그(PVP 탭 · ROUTINE T22 · 원작 ui.js openLeague/renderLeagueBoard/openLeagueRewards/renderLeagueRewards/openLeagueChallenge/renderLeagueChallenge/onChallenge · shot-042149·042208·042228).
    /// 세 얼굴: ⓐ 리그 목록(어두운 시트 · 탭바 그대로) ⓑ 보상 팝업(시트 위에 딤 α.90 으로 겹침 · 탭바까지 덮는다) ⓒ 상대 선택(흰 카드 · 탭바까지 덮는다).
    /// </summary>
    public static class LeagueSheet
    {
        public const string Name = "league";
        public const string RewardsName = "league-rewards";
        public const string ChallengeName = "league-challenge";
        private static readonly string[] RewardCurs = { "hammers", "coins", "tickets", "eggCurrency", "potions", "winders" };

        public static void Open(MetaHost h)
        {
            Ensure(h);
            h.Popups.Hide(RewardsName);
            h.Popups.Hide(ChallengeName);
            h.Popups.Show(Name, "pvp");
            Render(h);
        }

        private static void Ensure(MetaHost h)
        {
            LeagueEnsureResult r = h.League.Ensure(h.LeagueState, h.Wallet, h.MyCp, h.NowMs, h.TodayKey);
            if (r.SeasonReward != null) h.Toast("🏆 리그 시즌 종료! 순위 보상을 획득했습니다");   // 정본 league.js 64 그대로(순위 숫자는 원작에 없다 · T143 ⓒ)
            h.WriteStates();
        }

        public static void Close(MetaHost h)
        {
            h.Popups.Hide(RewardsName);
            h.Popups.Hide(ChallengeName);
            h.Popups.Hide(Name);
        }

        public static void Render(MetaHost h)
        {
            if (h.Popups.IsOpen(Name)) RenderBoard(h);
            if (h.Popups.IsOpen(RewardsName)) RenderRewards(h);
            if (h.Popups.IsOpen(ChallengeName)) RenderChallenge(h);
        }

        // ---- ⓐ 목록 ----
        private static void RenderBoard(MetaHost h)
        {
            Popup p = h.Popups.Find(Name);
            if (p == null) return;
            RectTransform root = PopupLayer.Clear(p);
            RectTransform sheet = PopupKit.Sheet(root, "sheet", "league_bg");
            float rem = PopupKit.Rem, w = UiKit.RefW, H = UiKit.RefH;

            float emblem = UiKit.L("league_emblem") * w;
            RectTransform em = UiKit.Box(sheet, "emblem");
            UiKit.Anchor(em, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -UiKit.H("league_emblem_top")), emblem, emblem);
            PopupKit.IconOr(em, "img", "leagueEmblem");

            float titleH = PopupKit.FontSize(TextKind.Head) * 1.3f;
            float titleY = UiKit.H("league_emblem_top") + emblem * 0.8f;
            TextMeshProUGUI title = UiKit.Text(sheet, "title", TextKind.Head, "플래티넘 리그", "stage_ink");
            LineHeight.Apply(title, "league_title_lh");   // T354 24회차 — 정본 2298 `.league-title { line-height: 1 }`   // T391 ⓑ — 정본 3805 `.sheet-title { font-size: 1.35rem }` = 49.1px → Head 48(전엔 Title 60)
            title.fontStyle = FontStyles.Bold;
            UiKit.Place(title.rectTransform, 0f, titleY, w, titleH);
            PopupKit.Ring(title);

            float barW = UiKit.L("league_bar_w") * w, barH = UiKit.H("league_bar_h");   // T378 10회차 — 정본 2308 `.league-season-bar { width: .4217W; height: .0257H }` = 표 그대로(전엔 ×1.25 · ×1.6)
            float barY = titleY + titleH + rem * 0.2f;
            Button bar = UiKit.Button(sheet, "season-bar", () => OpenRewards(h));
            RectTransform brt = bar.GetComponent<RectTransform>();
            UiKit.Place(brt, (w - barW) * 0.5f, barY, barW, barH);
            UiKit.Rounded(brt, "bg", "league_bar", barH * 0.5f);
            Image gift = PopupKit.IconOr(brt, "gift", "gift");
            UiKit.Anchor(gift.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(-w * 0.0362f + barH * 0.5f, 0f), barH * 1.5f, barH * 1.5f);
            double remain = (h.LeagueState.SeasonEndsAt - h.NowMs) / 1000;
            TextMeshProUGUI barT = UiKit.Text(brt, "text", TextKind.Sub, "시즌 종료: " + PopupKit.FmtTime(remain), "stage_ink");
            barT.fontStyle = FontStyles.Bold;

            // 랭킹 8행 창(내 순위 주변)
            List<LeagueEntry> board = h.League.Board(h.LeagueState, h.MyCp, h.Nickname, h.AvatarEmoji);
            int myRank = h.League.MyRank(h.LeagueState, h.MyCp);
            int start = Mathf.Max(0, Mathf.Min(myRank - 4, board.Count - 8));
            // T421 — 목록 위끝은 원작 실측(첫 행 카드 21.21%H · 표 league_list_top)으로 못 박는다. 종전 `barY + barH + .4rem` 흐름 셈은 시즌 바가 정본 높이로 돌아온 뒤(T378) 3.09%H 높게 섰다 —
            //        정본 머리 블록(문장·제목·바 · 2298~2320)은 값이 셋이라 흐름으로 옮기면 어긋남이 쌓인다(T404 등재문의 함정). 시즌 바 자리는 그대로.
            float listY = UiKit.H("league_list_top");
            float footTop = UiKit.L("league_foot_top") * H;
            float listW = UiKit.L("league_list_w") * w;
            RectTransform listBox = UiKit.Box(sheet, "list");
            // T388 — 정본 2320 `.league-list { max-height: calc(var(--app-h) * .542) }` 이 상한이다. 종전은 발 밴드까지 채웠다(53.9%H · 우연히 상한 안) — 상한을 표에서 읽어 잠근다.
            float listH = Mathf.Min(footTop - listY - rem * 0.3f, UiKit.H("league_list_max_h"));
            UiKit.Place(listBox, (w - listW) * 0.5f, listY, listW, listH);
            RectTransform content = PopupKit.ScrollList(listBox, "rows", UiKit.H("league_row_gap"), 0f, 0f);
            float rowH = UiKit.H("league_row_h");   // T378 10회차 — 정본 2320·2326(행 6.36%H + 간격 1.12%H = 원작 피치 7.48%H · 등재가 화소까지 닫은 자리 · 전엔 ×1.1 = 8.12%H)
            for (int i = start; i < Mathf.Min(board.Count, start + 8); i++)
                Row(content, board[i], i + 1, rowH, listW);

            // 회색(다크 전환) 밴드: 내 행 + ◀ · [도전]
            RectTransform foot = UiKit.Box(sheet, "foot");
            UiKit.Band(foot, UiKit.L("league_foot_top"), 1f);
            // 정본 `.league-foot`(style.css 2376) `0 -.14rem .34rem rgba(0,0,0,.40)` — **위로** 뜨는 그늘이다.
            // 정본 주석: «리스트 쪽으로 떨어지는 그림자 — 밴드가 위에 얹힌 판». 밴드라 모서리는 각지다(반지름 0).
            // 그늘은 발판의 첫 자식이라 발판 제 바탕 뒤에 깔리고, 위로 삐져나온 만큼이 리스트 위에 얹힌다.
            UiShadow.Drop(foot, "leaguefoot_up", 0f);
            UiKit.Panel(foot, "bg", "league_foot");
            // T178 23회차 — 정본 2361 `.league-foot { background-image: linear-gradient(180deg, rgba(255,255,255,.16) 0 1px,
            //   rgba(255,255,255,.05) 8%, rgba(255,255,255,0) 30%, rgba(0,0,0,.22) 100%) }` — 밴드가 «위에 얹힌 판» 으로 읽히게 하는 면 겹.
            //   바탕(2373 `#1a1f2b`)은 한 값이라 표가 `over_color` 로 미리 합성한다. 그늘·윗변 테는 바로 위·아래 줄이 이미 세운다.
            SurfaceArt.Fill(foot, "bg-grad", "league_foot", w, H - footTop);
            UiKit.Line(foot, "line", "pp_line", PopupKit.Line3, true);
            LeagueEntry me = null;
            foreach (LeagueEntry e in board) if (e.IsMe) { me = e; break; }
            RectTransform pinned = UiKit.Box(foot, "pinned");
            UiKit.Place(pinned, (w - listW) * 0.5f, rem * 0.5f, listW, rowH);
            if (me != null) Row(pinned, me, myRank, rowH, listW);
            float btnW = UiKit.L("league_challenge_w") * w, btnH = UiKit.H("league_challenge_h");
            float actY = rem * 0.5f + rowH + rem * 0.5f;
            Button ch = PopupKit.Btn(foot, "challenge", "도전", "pp_green", "pp_green_dk", () => OpenChallenge(h), btnW, btnH);
            UiKit.Place(ch.GetComponent<RectTransform>(), (w - btnW) * 0.5f, actY, btnW, btnH);
            Button back = PopupKit.BackButton(foot, () => Close(h));
            UiKit.Place(back.GetComponent<RectTransform>(), w * 0.0161f, actY + (btnH - UiKit.H("back_h")) * 0.5f, UiKit.H("back_w"), UiKit.H("back_h"));
        }

        /// <summary>랭킹 한 행(원작 leagueRow): 순위 · 아바타 · 이름+전투력 · 점수 pill · 서버.</summary>
        private static void Row(Transform parent, LeagueEntry e, int rank, float rowH, float rowW)
        {
            float rem = PopupKit.Rem, w = UiKit.RefW;
            RectTransform row = PopupKit.Item(parent, "row-" + rank, -1f, rowH);
            PopupKit.Outlined(row, "face", e.IsMe ? "pp_blue" : "league_row", rem * 0.6f, UiKit.L("line2_px"));   // T365 6회차 — 정본 2328 `.league-row` ol2(전엔 ol1)
            float x = rem * 0.5f;
            TextMeshProUGUI rk = UiKit.Text(row, "rank", TextKind.Body, rank.ToString(), "stage_ink");
            rk.fontStyle = FontStyles.Bold;
            UiKit.OutlinePx(rk, "pp_line", KeylineUi.Px("league_row_text"));   // T109 5회차 — 정본 style.css 8403 `.league-row .league-rank { 2px var(--pp-line) }`
            UiKit.TextShadow(rk, "league_row");   // T333 3회차 — 정본 8392 의 첫 겹(0 1px 1px rgba(0,0,0,.75)) · 키라인과 같은 재질에 얹힌다(순서 무관)
            UiKit.Place(rk.rectTransform, x, 0f, rem * 1.8f, rowH);
            x += rem * 1.8f + rem * 0.5f;
            float av = UiKit.H("league_avatar");
            RectTransform avatar = PopupKit.Avatar(row, "avatar", av, e.Avatar, RadiusUi.Px("league_avatar_r_rem"));
            UiKit.Place(avatar, x, (rowH - av) * 0.5f, av, av);
            x += av + rem * 0.5f;
            float scoreW = UiKit.L("league_score_w") * w, scoreH = UiKit.H("league_score_h");   // T378 10회차 — 정본 2345 `.league-score { width: .187W; height: .0257H }` 고정 pill(전엔 ×1.6)
            float nameW = rowW - x - scoreW - rem * 1.2f;
            TextMeshProUGUI nm = UiKit.Text(row, "name", TextKind.Sub, e.Name, "stage_ink", TextAlignmentOptions.Left);
            LineHeight.Apply(nm, "league_name_lh");   // T354 11회차 — 정본 2337 `.league-name { line-height: 1.3 }`(긴 이름이 칸에서 꺾인다)
            nm.fontStyle = FontStyles.Bold;
            UiKit.OutlinePx(nm, "pp_line", KeylineUi.Px("league_row_text"));   // T109 5회차 — 정본 8403 `.league-row .league-name { 2px var(--pp-line) }`
            UiKit.TextShadow(nm, "league_row");   // T333 3회차 — 정본 8392 첫 겹
            UiKit.Place(nm.rectTransform, x, rowH * 0.08f, nameW, rowH * 0.45f);
            // T89 — «⚔» 는 글꼴에 없어 □ 로 찍혔다. 정본 TOAST_ICON 이 `⚔ → tm_sword` 를 쥐고 있으니 그 아이콘으로 선다.
            RectTransform cp = UiKit.IconTextRow(row, "cp", TextKind.Sub, "⚔ " + PopupKit.Fmt(e.Cp), "league_cp", TextAlignmentOptions.Left);
            foreach (TextMeshProUGUI piece in UiKit.RowTexts(cp))
            {
                piece.fontStyle = FontStyles.Bold;
                // T109 5회차 — 정본 8408 `.league-row .league-name small { max(1.4px, .1em) var(--pp-line) }`: 이름 아래 `<small>` 이 전투력이다(ui.js 4740).
                // 부모의 2px 을 물리면 작은 숫자의 속공간이 메워져 뭉갠다고 정본 주석이 적어 둔 자리 — 글자 크기에 비례.
                UiKit.OutlinePx(piece, "pp_line", KeylineUi.Em("league_name_small", piece.fontSize));
            }
            UiKit.Place(cp, x, rowH * 0.5f, nameW, rowH * 0.45f);
            RectTransform score = UiKit.Box(row, "score");
            UiKit.Place(score, rowW - rem * 0.5f - scoreW, rowH * 0.12f, scoreW, scoreH);
            UiKit.Rounded(score, "bg", "league_score", RadiusUi.Px("league_score_r_rem"));
            // T89 — 정본 `ui.js` 4741: `<span class="league-score">${IconGen.img('star')} ${U.fmt(e.score)}</span>`.
            // 클론은 «★»(U+2605) 글자로 찍어 글꼴에 없어 □ 였다 — 표의 ⭐ 를 써서 같은 `star` 아이콘 + 수로 세운다.
            RectTransform scRow = UiKit.IconTextRow(score, "text", TextKind.Sub, "⭐ " + PopupKit.Fmt(e.Score), "stage_ink");
            // T333 3회차 — 정본 8392 는 `.league-score` 도 같은 한 겹을 받는다(알약 판 위 점수).
            // T352 3회차 — 정본 8635 `.league-score { font-variant-numeric: tabular-nums }`(주석: «세로로 열을 이루는 숫자만 등폭으로 — 행마다 좌우로 흔들리던 자리»).
            //             점수는 봇 20~200 · 내 점수라 두세 자리가 창 8행 + 발 밴드에서 세로 열을 이룬다 — 숫자 구간만 <mspace> 로(결정 570 · 칸 폭은 글꼴에서).
            foreach (TextMeshProUGUI piece in UiKit.RowTexts(scRow)) { WrapUi.Apply(piece, "league_score"); piece.fontStyle = FontStyles.Bold; UiKit.TextShadow(piece, "league_row"); TabularText.Apply(piece); }
            TextMeshProUGUI sv = UiKit.Text(row, "server", TextKind.Sub, "서버 " + e.Server, e.IsMe ? "stage_ink" : "league_server", TextAlignmentOptions.Right);
            // T109 5회차 — 정본 2355 `.league-row.me .league-server { max(1.2px, .1em) var(--pp-line) }`: 파란 me 행만 키라인.
            // 어두운 행의 회색 «서버 N» 은 정본도 민무늬다(2350 주석: 근흑 판 위라 검정 링이 아무것도 안 갈라 준다).
            if (e.IsMe) UiKit.OutlinePx(sv, "pp_line", KeylineUi.Em("league_server_me", sv.fontSize));
            if (e.IsMe) sv.color = PinnedColorUi.C("league_server_me_ink");   // T396 10회차 — 정본 2355 `.league-row.me .league-server { color: #dce6ff }`(전엔 stage_ink 흰색)
            UiKit.Place(sv.rectTransform, rowW - rem * 0.55f - scoreW * 1.2f, rowH - rem * 0.2f - PopupKit.FontSize(TextKind.Sub) * 1.1f, scoreW * 1.2f, PopupKit.FontSize(TextKind.Sub) * 1.1f);
        }

        // ---- ⓑ 보상 팝업(시트 위 겹침 · 탭바까지 딤) ----
        public static void OpenRewards(MetaHost h)
        {
            Ensure(h);
            h.Popups.Show(RewardsName, null, true, "modal_dim_deep");
            RenderRewards(h);
        }

        private static void RenderRewards(MetaHost h)
        {
            Popup p = h.Popups.Find(RewardsName);
            if (p == null) return;
            RectTransform root = PopupLayer.Clear(p);
            float rem = PopupKit.Rem, w = UiKit.RefW, H = UiKit.RefH;
            int myRank = h.League.MyRank(h.LeagueState, h.MyCp);
            var cur = h.League.RewardForRank(myRank);
            float cardW = UiKit.L("modal_wide_w") * w;
            float ribbonH = UiKit.H("lgr_ribbon_h");   // T378 10회차 — 정본 2485 `.league-reward-banner { height: .0528H }`(전엔 ×1.2)
            float descH = PopupKit.FontSize(TextKind.Sub) * 2.9f;
            float gridRowH = PopupKit.FontSize(TextKind.Sub) * 1.5f;
            float gridH = gridRowH * 2f + rem * 0.3f;
            float collectH = PopupKit.FontSize(TextKind.Sub) * 3f;
            float tableH = UiKit.H("lgr_table_h");
            float cardH = ribbonH + rem * 1.2f + descH + rem * 0.5f + gridH + rem * 0.55f + collectH + rem * 0.65f + tableH + rem * 2.78f;
            // 정본 `.lgr-overlay .idet-wrap { top: .76rem }` 은 CSS 보정값(아래로 되돌림)이라 옮기지 않는다 — 종전 `rem * 0.76f` 는 PopupKit.Card 에서 양수 = 위라 부호까지 반대였다(T395 · 카드 위끝 18.23 → ≈19.7%H · 원작 19.21).
            RectTransform card = PopupKit.Card(root, "card", cardW, cardH, "league_bg", rem, "pp_line", 0f);
            float inner = cardW - PopupKit.Line3 * 2f;

            // 리본(카드보다 넓다)
            float rw = UiKit.L("lgr_ribbon_w") * w;
            RectTransform ribbon = UiKit.Box(card, "ribbon");
            UiKit.Anchor(ribbon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, rw, ribbonH);
            float tailW = w * 0.0444f, tailH = H * 0.0584f, tailY = H * 0.009f;
            // T159 2회차 — 정본 2503~2504 은 꼬리 **바깥 변**에 절반 깊이의 V 홈을 판다(`clip-path` · 안쪽 변은 본체에 붙는 직선).
            // 여태 `UiKit.Panel` 민무늬 직사각형이라 그 홈이 통째로 없었다. 꼭짓점은 `ClipShapeUi.json` 이 쥔다.
            ClipShape.Face(ribbon, "tail-l", "lgr_tail_l", -tailW, tailY, tailW, tailH, "lgr_ribbon_dk");
            ClipShape.Face(ribbon, "tail-r", "lgr_tail_r", rw, tailY, tailW, tailH, "lgr_ribbon_dk");
            PopupKit.Outlined(ribbon, "face", "lgr_ribbon", rem * 0.3f, PopupKit.Line);
            TextMeshProUGUI rt = UiKit.Text(ribbon, "text", TextKind.Body, "플래티넘 리그 보상", "stage_ink");
            rt.fontStyle = FontStyles.Bold;
            PopupKit.Ring(rt, "league_reward_banner", "pp_line");   // 정본 .league-reward-banner 2px

            float y = ribbonH + rem * 1.2f;
            TextMeshProUGUI desc = UiKit.Text(card, "desc", TextKind.Sub, "현재 순위(" + myRank + ")를 유지하면 시즌 종료 시\n다음 보상을 받을 수 있습니다:", "stage_ink");
            desc.fontStyle = FontStyles.Bold;
            desc.textWrappingMode = TextWrappingModes.Normal;
            LineHeight.Apply(desc, "league_reward_desc_lh");   // T354 11회차 — 정본 2514 `.league-reward-desc { line-height: 1.4 }`(줄바꿈이 박혀 두 줄 · 접히면 더)
            UiKit.Place(desc.rectTransform, rem * 1.1f, y, inner - rem * 2.2f, descH);
            y += descH + rem * 0.5f;
            RectTransform grid = UiKit.Box(card, "grid");
            UiKit.Place(grid, rem * 1.1f, y, inner - rem * 2.2f, gridH);
            RewardGrid(grid, cur, inner - rem * 2.2f, gridRowH, "lgr_pill", "stage_ink", "league_reward_grid_span");
            y += gridH + rem * 0.55f;
            double remain = (h.LeagueState.SeasonEndsAt - h.NowMs) / 1000;
            RectTransform collect = UiKit.Box(card, "collect");
            float collectW = inner * 0.55f;
            UiKit.Place(collect, (cardW - collectW) * 0.5f, y, collectW, collectH);
            Image collectFace = PopupKit.Outlined(collect, "face", "lgr_collect", rem * 0.5f, PopupKit.Line3);
            SurfaceArt.FillMasked(collectFace, "collect-grad", "lgr_collect_pill", collectW, collectH);   // 정본 .league-collect-pill linear-gradient(180deg, #e3e3e3, #c2c2c2) · T178 3회차
            TextMeshProUGUI c1 = UiKit.Text(collect, "label", TextKind.Sub, "수집까지:", "pp_ink");
            LineHeight.Apply(c1, "league_collect_pill_lh");   // T354 24회차 — 정본 2523 `.league-collect-pill { line-height: 1.3 }`
            c1.fontStyle = FontStyles.Bold;
            UiKit.Place(c1.rectTransform, 0f, rem * 0.2f, collectW, collectH * 0.45f);
            TextMeshProUGUI c2 = UiKit.Text(collect, "time", TextKind.Sub, PopupKit.FmtTime(remain), "pp_green_dk");
            LineHeight.Apply(c2, "league_collect_pill_lh");   // T354 24회차 — 같은 알약(2523)
            c2.color = PinnedColorUi.C("league_collect_time_ink");   // T396 10회차 — 정본 2528 `.league-collect-pill b { color: #1d8f3c }`(전엔 토큰 pp_green_dk #1f8c34 근사)
            c2.fontStyle = FontStyles.Bold;
            UiKit.Place(c2.rectTransform, 0f, collectH * 0.5f, collectW, collectH * 0.45f);
            y += collectH + rem * 0.65f;

            // 흰 보상 표(카드 좌우 꽉)
            RectTransform table = UiKit.Box(card, "table");
            UiKit.Place(table, 0f, y, cardW, tableH);
            PopupKit.Outlined(table, "face", "pp_paper", rem * 0.7f, UiKit.L("line2_px"));   // T365 6회차 — 정본 2537 `.league-reward-table` ol2(전엔 ol1)
            RectTransform rows = PopupKit.ScrollList(table, "rows", 0f, 0f, rem * 0.3f);
            float tierH = UiKit.H("lgr_tier_h");   // T378 10회차 — 정본 2540 주석 «티어 피치 8.61%H»(전엔 ×1.15 = 9.9%H · 4행 누적 +5.2%p)
            float rankW = UiKit.H("lgr_rank_w");   // T378 10회차 — 정본 2556 `.league-tier-rank { width: 2.4rem }` = 표 0.0455H(전엔 ×1.5 = 3.6rem)
            List<LeagueRewardTier> tiers = h.Meta.League.RewardTiers;
            for (int i = 0; i < tiers.Count; i++)
            {
                LeagueRewardTier t = tiers[i];
                var r = h.League.RewardForRank(t.Rank);
                RectTransform row = PopupKit.Item(rows, "tier-" + t.Rank, -1f, tierH);
                if (i > 0) TierDash(row);   // T368 5회차 — 정본 2548 `.league-reward-tier` 단 사이 대시 줄(`:first-child` 는 없음 · 전엔 이름만 dash 인 실선)
                RectTransform rk = UiKit.Box(row, "rank");
                UiKit.Place(rk, rem * 1.1f, 0f, rankW, tierH);
                if (t.Rank <= 3)
                {
                    // T130 — 정본 ui.js 4790 `IconGen.img('rank' + t.rank)`: 1·2위 왕관 배지 · 3위 벽돌색 마름모(아틀라스 rank1~3 · T31). 종전 crown/badge 는 GUI PRO Kit 데모 스프라이트였다.
                    Image badge = PopupKit.IconOr(rk, "badge", "rank" + t.Rank);
                    UiKit.Anchor(badge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, rem * 2.15f, rem * 2.15f);
                }
                // T391 ⓑ — 정본 2573 `.lgr-rank-n { 1.22rem }`(1~3위 숫자) = 44.4px → Button 44 · 2563 `.league-tier-rank.text { 1.48rem }`(4위 아래 글자) = 53.9px → Head 48(전엔 둘 다 Sub 36)
                TextMeshProUGUI lab = UiKit.Text(rk, "label", t.Rank > 3 ? TextKind.Head : TextKind.Button, t.Rank <= 3 ? t.Rank.ToString() : t.Label, "stage_ink");   // 한 호출 — 키라인 자(T109)·종류 자(T391)가 같은 «label» 을 읽는다
                lab.fontStyle = FontStyles.Bold;
                WrapUi.Apply(lab, "league_tier_rank");   // T361 2회차 — 정본 white-space 표(WrapUi.json) 2556 `.league-tier-rank { nowrap }`
                PopupKit.Ring(lab, t.Rank <= 3 ? "lgr_rank_n" : "league_tier_rank", "pp_line");   // 정본 .lgr-rank-n .16rem · .league-tier-rank.text .108em
                RectTransform g = UiKit.Box(row, "grid");
                float gx = rem * 1.1f + rankW + rem * 0.6f;
                UiKit.Place(g, gx, (tierH - gridH) * 0.5f, cardW - gx - rem * 1.1f, gridH);
                RewardGrid(g, r, cardW - gx - rem * 1.1f, gridRowH, "lgr_table_pill", "pp_ink", "league_tier_grid_span");
            }

            PopupKit.XButton(card, () => { h.Popups.Hide(RewardsName); Open(h); });
        }

        /// <summary>재화 6종 3열 pill 격자(원작 leagueRewardGrid).</summary>
        /// <summary>정본 2548~2555 `.league-reward-tier { background-image: repeating-linear-gradient(to right, var(--pp-line) 0 .0323W, transparent .0323W .0625W);
        /// background-size: 100% 2px; background-position: 0 0 }` — 단(tier) 행 위끝에 왼쪽 끝부터 꽉 차는 대시 줄. 표 `SurfaceUi.json` `stripes.league_tier_dash`
        /// (주기·대시 = 앱 폭 비율 · 두께 = CSS px) · 한 주기를 구워 Tiled 로 되풀이한다(T368 3회차 소환 바 대시와 같은 길).</summary>
        public static Image TierDash(RectTransform row)
        {
            const string key = "league_tier_dash";
            float period = SurfaceArt.StripeNum(key, "period_w", 0f) * UiKit.RefW;
            float dash = SurfaceArt.StripeNum(key, "dash_w", 0f) * UiKit.RefW;
            float h = Mathf.Max(1f, SurfaceArt.StripeNum(key, "band_css_px", 2f) * KeylineUi.CssPx);
            RectTransform rt = UiKit.Box(row, "dash");
            rt.SetAsFirstSibling();                                   // background — 행의 다른 조각보다 뒤
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0f, 1f);
            rt.offsetMin = new Vector2(0f, -h); rt.offsetMax = new Vector2(0f, 0f);   // position 0 0 · size 100% 2px
            Image img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.type = Image.Type.Tiled;
            img.sprite = SurfaceArt.BakeStripe(key, period, dash, 0f, h);   // phase 0 — 왼쪽 끝이 대시 시작
            return img;
        }

        private static void RewardGrid(RectTransform grid, Forge.Core.Data.OrderedMap<double> r, float gw, float rowH, string pillKey, string inkKey, string wrapKey)
        {
            float rem = PopupKit.Rem;
            float gapX = rem * 0.5f, gapY = rem * 0.3f;
            float cw = (gw - gapX * 2f) / 3f;
            for (int i = 0; i < RewardCurs.Length; i++)
            {
                string cur = RewardCurs[i];
                RectTransform pill = UiKit.Box(grid, "pill-" + cur);
                UiKit.Place(pill, (i % 3) * (cw + gapX), (i / 3) * (rowH + gapY), cw, rowH);
                UiKit.Rounded(pill, "bg", pillKey, rowH * 0.5f);
                Image ico = PopupKit.IconOr(pill, "ico", ShopSheet.CurIcon(cur));
                UiKit.Place(ico.rectTransform, rem * 0.4f, rowH * 0.1f, rowH * 0.8f, rowH * 0.8f);
                TextMeshProUGUI t = UiKit.Text(pill, "amt", TextKind.Sub, PopupKit.Fmt(r.Get(cur, 0)), inkKey, TextAlignmentOptions.Left);
                t.fontStyle = FontStyles.Bold;
                WrapUi.Apply(t, wrapKey);   // T361 2회차 — 정본 white-space 표(WrapUi.json) 2520 `.league-reward-grid span` · 2582 `.league-tier-grid span` 둘 다 nowrap
                t.rectTransform.offsetMin = new Vector2(rem * 0.4f + rowH * 0.9f, 0f);
            }
        }

        // ---- ⓒ 상대 선택 ----
        public static void OpenChallenge(MetaHost h)
        {
            Ensure(h);
            h.Popups.Show(ChallengeName, null, true, "modal_dim_deep");
            RenderChallenge(h);
        }

        private static void RenderChallenge(MetaHost h)
        {
            Popup p = h.Popups.Find(ChallengeName);
            if (p == null) return;
            RectTransform root = PopupLayer.Clear(p);
            float rem = PopupKit.Rem, w = UiKit.RefW;
            float cardW = UiKit.L("modal_wide_w") * w;
            float rowH = UiKit.L("lc_row_h") * w;
            float pillH = UiKit.L("lc_pill_h") * w;   // T378 10회차 — 정본 2594 `.league-ticket-pill { height: .0411W }`(전엔 ×1.4)
            List<LeagueChallengeOption> list = h.League.ChallengeList(h.LeagueState);
            // T463 — 정본 1752 `.modal-card { display: flex; flex-direction: column; gap: .45rem }`: 카드의 네 자식(제목·안내문·알약·목록) **사이마다** .45rem 이 든다.
            //   클론은 각 자식의 아래 마진(.2 · 1.05 · 1.9rem)을 Spacer 자식으로 세우므로 gap 을 Column 에 걸면 Spacer 앞뒤로 두 번 든다(결정 기록) —
            //   그래서 gap 은 그 마진 Spacer 셋과 카드 높이 셈에 **한 번씩** 더한다(거리 = margin + gap · 정본과 같다). 값은 표 catalog layout `card_gap_rem`(.45).
            float gap = UiKit.L("card_gap_rem") * rem;
            float cardH = rem * 1.1f + PopupKit.FontSize(TextKind.Title) * 1.3f + rem * 0.2f + gap + PopupKit.FontSize(TextKind.Sub) * 1.5f + rem * 1.05f + gap + pillH + rem * 1.9f + gap + list.Count * (rowH + rem * 0.5f) + rem * 1.15f + rem * 1.1f;
            RectTransform card = PopupKit.Card(root, "card", cardW, cardH, "pp_paper", rem);
            PopupKit.Column(card, UiKit.H("card_pad"), 0f);   // T463 — 틈 0 은 «gap 없음» 이 아니라 «gap 을 아래 마진 Spacer 에 접었다» 다(위 주석)
            // T462 — 이 제목은 정본 ui.js 4868 `.profile-title`(style.css 2991 `font-size: 1.5rem; margin: 0 0 .2rem`)이지 2298 `.league-title`(1.15rem · 시트 제목)이 아니다.
            //   1.5rem = 54.6px 은 `ProfilePopup` 이 같은 선택자에 쓰는 `Title`(60 · +10% · check_text_kinds ±12% 안) 단으로 — 새 단을 더하지 않는다(T391 창 규약 · T404 Title2 는 1.15rem 단이라 그대로).
            //   아래 마진 .2rem 은 카드 높이 셈(위 cardH)과 아래 Spacer 둘 다에 넣는다(전엔 둘 다 없었다 · 제목이 −23% 라 카드가 짧아 위끝 +2.93%p · T28 119회차).
            TextMeshProUGUI title = PopupKit.Label(card, "title", TextKind.Title, "상대 선택", "pp_ink");   // T456 — 정본 ui.js 4868 `.profile-title` → style.css 2992 `color: var(--pp-ink)`: 흰 카드 위 진한 잉크(전엔 stage_ink #fff 라 흰 위 흰 · 런 1102 실측 가장 어두운 화소 189). T404 ⓑ — 정본 2298 `.league-title { 1.15rem }` = 41.9px → Title2 42(전엔 Title 60)
            PopupKit.Ring(title);
            PopupKit.Spacer(card, rem * 0.2f + gap);   // T462 — 정본 2991 `.profile-title { margin: 0 0 .2rem }` + T463 카드 gap .45
            PopupKit.Label(card, "desc", TextKind.Sub, "도전 티켓은 매일 09:00에 보충됩니다!", "pp_ink", TextAlignmentOptions.Center, false, true, PopupKit.FontSize(TextKind.Sub) * 1.5f);
            PopupKit.Spacer(card, rem * 1.05f + gap);   // 정본 2587 `.league-challenge-desc { margin: 0 0 1.05rem }` + T463 카드 gap
            RectTransform pillRow = PopupKit.Item(card, "ticket-row", -1f, pillH);
            float pillW = UiKit.L("lc_pill_w") * w;   // T378 10회차 — 정본 2594 `.league-ticket-pill { width: .1392W }`(전엔 ×1.3)
            RectTransform pill = UiKit.Box(pillRow, "pill");
            UiKit.Anchor(pill, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, pillW, pillH);
            UiKit.Rounded(pill, "bg", "pp_ink", RadiusUi.Px("league_ticket_pill_r_rem"));
            Image tk = PopupKit.IconOr(pill, "ico", "ticket");
            UiKit.Place(tk.rectTransform, rem * 0.4f, pillH * 0.15f, pillH * 0.7f, pillH * 0.7f);
            TextMeshProUGUI tkT = UiKit.Text(pill, "text", TextKind.Sub, h.LeagueState.Tickets + "/" + h.Meta.League.TicketMax, "stage_ink");
            tkT.fontStyle = FontStyles.Bold;
            tkT.rectTransform.offsetMin = new Vector2(pillH * 0.9f, 0f);
            PopupKit.Spacer(card, rem * 1.9f + gap);   // 정본 2593 `.league-ticket-pill { margin: 0 auto 1.9rem }` + T463 카드 gap

            float av = UiKit.H("lc_avatar");
            float btnW = UiKit.L("lc_btn_w") * w, btnH = UiKit.L("lc_btn_h_rem") * rem;   // 정본 2626 .league-challenge-row .btn.sm min-height 2.9rem (T402 · 표)
            float rowW = cardW - PopupKit.Line3 * 2f - UiKit.H("card_pad") * 2f + w * 0.0185f * 2f;
            for (int i = 0; i < list.Count; i++)
            {
                LeagueChallengeOption o = list[i];
                RectTransform slot = PopupKit.Item(card, "opp-" + i, -1f, rowH + rem * 0.5f);
                RectTransform row = UiKit.Box(slot, "row");
                UiKit.Anchor(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, rowW, rowH);
                UiKit.Rounded(row, "bg", "challenge_row", RadiusUi.Px("league_challenge_row_r_rem"));
                RectTransform avatar = PopupKit.Avatar(row, "avatar", av, o.Bot.Avatar, RadiusUi.Px("league_challenge_avatar_r_rem"));
                UiKit.Place(avatar, rem * 0.6f, (rowH - av) * 0.5f, av, av);
                float nx = rem * 0.6f + av + rem * 0.6f;
                // 정본 `.league-challenge-side` 는 **세로 한 칸**(별 위 · 도전 버튼 아래)이라 이름 칸은 버튼 폭만 비우면 된다.
                float nw = rowW - nx - btnW - rem * 1.2f;
                TextMeshProUGUI nm = UiKit.Text(row, "name", TextKind.Sub, o.Bot.Name, "pp_ink", TextAlignmentOptions.Left);
                nm.fontStyle = FontStyles.Bold;
                LineHeight.Apply(nm, "league_challenge_name_lh");   // T354 11회차 — 정본 2632 `.league-challenge-name { line-height: 1.3 }`
                WrapUi.Apply(nm, "league_challenge_name");   // T361 6회차 — 표 clone_nowrap: 정본은 접지만 클론 고정 행 + 글자 하한이라 안 접는다(런 752 BrLinesTests «BlandBuddy22667» 두 줄)
                UiKit.Place(nm.rectTransform, nx, rowH * 0.12f, nw, rowH * 0.4f);
                // 원작 `.league-challenge-name small` = IconGen.img('power') + 전투력 — 글자 «⚔» 가 아니라 T31 아이콘(T58 · 글꼴에 없는 글자는 □ 로 찍힌다)
                float cpH = rowH * 0.4f, cpIco = cpH * 0.9f;
                Image cpI = PopupKit.IconOr(row, "cp-ico", "power");
                UiKit.Place(cpI.rectTransform, nx, rowH * 0.5f + (cpH - cpIco) * 0.5f, cpIco, cpIco);
                TextMeshProUGUI cp = UiKit.Text(row, "cp", TextKind.Sub, PopupKit.Fmt(o.Bot.Cp), "challenge_cp", TextAlignmentOptions.Left);
                cp.fontStyle = FontStyles.Bold;
                UiKit.OutlinePx(cp, "pp_line", KeylineUi.Px("league_challenge_cp"));   // T109 5회차 — 정본 2635 `.league-challenge-name small { 2px var(--pp-line) }`(T28 22회차가 잡은 «민주황» 자리)
                UiKit.Place(cp.rectTransform, nx + cpIco + rem * 0.15f, rowH * 0.5f, nw - cpIco - rem * 0.15f, cpH);
                // 원작 `.league-challenge-side .star` = IconGen.img('star') + «+N»(순검정) — «★» 글자 대신 아이콘(T58)
                // 정본 `.league-challenge-side { display:flex; flex-direction:column; align-items:center; gap:.3rem }`
                // — 별점이 **도전 버튼 위**에 얹힌 세로 한 칸이다(클론은 버튼 **왼쪽**에 나란히 뒀었다 · 런 183 `screen_league-challenge` 2.4/10 의 밴드2 미짝 넷).
                float starW = rem * 2.6f, starIco = PopupKit.FontSize(TextKind.Sub) * 1.0f;
                float sideX = rowW - rem * 0.6f - btnW;
                float starH = PopupKit.FontSize(TextKind.Sub) * 1.1f;
                float sideGap = rem * 0.3f;
                float sideY = (rowH - (starH + sideGap + btnH)) * 0.5f;
                RectTransform starBox = UiKit.Box(row, "star");
                UiKit.Place(starBox, sideX, sideY, btnW, starH);
                float starIn = (btnW - starW) * 0.5f;   // 별+«+N» 을 칸 가운데로(정본 align-items: center)
                Image starI = PopupKit.IconOr(starBox, "star-ico", "star");
                UiKit.Place(starI.rectTransform, starIn, (starH - starIco) * 0.5f, starIco, starIco);
                TextMeshProUGUI star = UiKit.Text(starBox, "star-n", TextKind.Sub, "+" + o.StarReward, "pp_line", TextAlignmentOptions.Left);
                star.fontStyle = FontStyles.Bold;
                UiKit.Place(star.rectTransform, starIn + starIco + rem * 0.05f, 0f, starW - starIco, starH);
                int idx = o.Index;
                bool can = h.League.CanChallenge(h.LeagueState, idx);
                // 원작 `.btn.sm` 두 줄: «도전» / 티켓 아이콘 + «1»(IconGen.img('ticket')) — 이모지 대신 아이콘 줄(T58)
                Button b = PopupKit.Btn(row, "challenge", "도전", "challenge_btn", "challenge_btn_dk", () => OnChallenge(h, idx), btnW, btnH, "pp_line", TextKind.Sub, !can);
                RectTransform br = b.GetComponent<RectTransform>();
                UiKit.Place(br, sideX, sideY + starH + sideGap, btnW, btnH);
                RectTransform lab = b.transform.Find("label").GetComponent<RectTransform>();
                LineHeight.Apply(lab.GetComponent<TextMeshProUGUI>(), "modal_card_league_challenge_side_btn_lh");   // T354 24회차 — 정본 2642 `.modal-card .league-challenge-side .btn { line-height: 1.25 }`
                float lip = UiKit.H("btn_lip");
                lab.offsetMin = new Vector2(0f, lip + (btnH - lip) * 0.42f);
                float tkH = (btnH - lip) * 0.42f, tkIco = tkH * 0.85f;
                float tkTextW = PopupKit.FontSize(TextKind.Sub) * 0.8f;
                float tkX = (btnW - tkIco - rem * 0.1f - tkTextW) * 0.5f;
                Image tkI = PopupKit.IconOr(br, "ticket-ico", "ticket");
                tkI.raycastTarget = false;
                UiKit.Place(tkI.rectTransform, tkX, btnH - lip - tkH + (tkH - tkIco) * 0.5f, tkIco, tkIco);
                TextMeshProUGUI tkN = UiKit.Text(br, "ticket-n", TextKind.Sub, "1", "pp_line", TextAlignmentOptions.Left);
                tkN.fontStyle = FontStyles.Bold;
                tkN.raycastTarget = false;
                UiKit.Place(tkN.rectTransform, tkX + tkIco + rem * 0.1f, btnH - lip - tkH, tkTextW + rem, tkH);
            }

            PopupKit.XButton(card, () => { h.Popups.Hide(ChallengeName); Open(h); });
        }

        private static void OnChallenge(MetaHost h, int idx)
        {
            LeagueChallenge r = h.League.Challenge(h.LeagueState, idx, h.MyCp);
            if (r == null) { h.Toast("🎟 도전 티켓이 부족합니다"); return; }
            h.Toast(r.Win ? "🏆 승리! ⭐+" + r.StarReward : "💀 " + r.Bot.Name + "에게 패배했습니다");
            h.Chat.ShareLeagueResult(h.ChatState, r.Win, h.MyCp, r.Bot, h.Nickname, h.AvatarEmoji, h.NowMs);
            h.Touch();
        }
    }
}
