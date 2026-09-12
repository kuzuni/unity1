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
            if (r.SeasonReward != null) h.Toast("🏆 리그 시즌 종료! " + r.EndedRank + "위 보상 지급");
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

            float titleH = PopupKit.FontSize(TextKind.Title) * 1.3f;
            float titleY = UiKit.H("league_emblem_top") + emblem * 0.8f;
            TextMeshProUGUI title = UiKit.Text(sheet, "title", TextKind.Title, "플래티넘 리그", "stage_ink");
            title.fontStyle = FontStyles.Bold;
            UiKit.Place(title.rectTransform, 0f, titleY, w, titleH);
            PopupKit.Ring(title);

            float barW = UiKit.L("league_bar_w") * w * 1.25f, barH = UiKit.H("league_bar_h") * 1.6f;
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
            float listY = barY + barH + rem * 0.4f;
            float footTop = UiKit.L("league_foot_top") * H;
            float listW = UiKit.L("league_list_w") * w;
            RectTransform listBox = UiKit.Box(sheet, "list");
            UiKit.Place(listBox, (w - listW) * 0.5f, listY, listW, footTop - listY - rem * 0.3f);
            RectTransform content = PopupKit.ScrollList(listBox, "rows", UiKit.H("league_row_gap"), 0f, 0f);
            float rowH = UiKit.H("league_row_h") * 1.1f;
            for (int i = start; i < Mathf.Min(board.Count, start + 8); i++)
                Row(content, board[i], i + 1, rowH, listW);

            // 회색(다크 전환) 밴드: 내 행 + ◀ · [도전]
            RectTransform foot = UiKit.Box(sheet, "foot");
            UiKit.Band(foot, UiKit.L("league_foot_top"), 1f);
            UiKit.Panel(foot, "bg", "league_foot");
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
            PopupKit.Outlined(row, "face", e.IsMe ? "pp_blue" : "league_row", rem * 0.6f, PopupKit.Line);
            float x = rem * 0.5f;
            TextMeshProUGUI rk = UiKit.Text(row, "rank", TextKind.Body, rank.ToString(), "stage_ink");
            rk.fontStyle = FontStyles.Bold;
            UiKit.Place(rk.rectTransform, x, 0f, rem * 1.8f, rowH);
            x += rem * 1.8f + rem * 0.5f;
            float av = UiKit.H("league_avatar");
            RectTransform avatar = PopupKit.Avatar(row, "avatar", av, e.Avatar, rem * 0.4f);
            UiKit.Place(avatar, x, (rowH - av) * 0.5f, av, av);
            x += av + rem * 0.5f;
            float scoreW = UiKit.L("league_score_w") * w, scoreH = UiKit.H("league_score_h") * 1.6f;
            float nameW = rowW - x - scoreW - rem * 1.2f;
            TextMeshProUGUI nm = UiKit.Text(row, "name", TextKind.Sub, e.Name, "stage_ink", TextAlignmentOptions.Left);
            nm.fontStyle = FontStyles.Bold;
            UiKit.Place(nm.rectTransform, x, rowH * 0.08f, nameW, rowH * 0.45f);
            TextMeshProUGUI cp = UiKit.Text(row, "cp", TextKind.Sub, "⚔ " + PopupKit.Fmt(e.Cp), "league_cp", TextAlignmentOptions.Left);
            cp.fontStyle = FontStyles.Bold;
            UiKit.Place(cp.rectTransform, x, rowH * 0.5f, nameW, rowH * 0.45f);
            RectTransform score = UiKit.Box(row, "score");
            UiKit.Place(score, rowW - rem * 0.5f - scoreW, rowH * 0.12f, scoreW, scoreH);
            UiKit.Rounded(score, "bg", "league_score", rem * 0.5f);
            TextMeshProUGUI sc = UiKit.Text(score, "text", TextKind.Sub, "★ " + PopupKit.Fmt(e.Score), "stage_ink");
            sc.fontStyle = FontStyles.Bold;
            TextMeshProUGUI sv = UiKit.Text(row, "server", TextKind.Sub, "서버 " + e.Server, e.IsMe ? "stage_ink" : "league_server", TextAlignmentOptions.Right);
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
            float ribbonH = UiKit.H("lgr_ribbon_h") * 1.2f;
            float descH = PopupKit.FontSize(TextKind.Sub) * 2.9f;
            float gridRowH = PopupKit.FontSize(TextKind.Sub) * 1.5f;
            float gridH = gridRowH * 2f + rem * 0.3f;
            float collectH = PopupKit.FontSize(TextKind.Sub) * 3f;
            float tableH = UiKit.H("lgr_table_h");
            float cardH = ribbonH + rem * 1.2f + descH + rem * 0.5f + gridH + rem * 0.55f + collectH + rem * 0.65f + tableH + rem * 2.78f;
            RectTransform card = PopupKit.Card(root, "card", cardW, cardH, "league_bg", rem, "pp_line", rem * 0.76f);
            float inner = cardW - PopupKit.Line3 * 2f;

            // 리본(카드보다 넓다)
            float rw = UiKit.L("lgr_ribbon_w") * w;
            RectTransform ribbon = UiKit.Box(card, "ribbon");
            UiKit.Anchor(ribbon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, rw, ribbonH);
            float tailW = w * 0.0444f;
            RectTransform tl = UiKit.Box(ribbon, "tail-l");
            UiKit.Place(tl, -tailW, H * 0.009f, tailW, H * 0.0584f);
            UiKit.Panel(tl, "bg", "lgr_ribbon_dk");
            RectTransform tr = UiKit.Box(ribbon, "tail-r");
            UiKit.Place(tr, rw, H * 0.009f, tailW, H * 0.0584f);
            UiKit.Panel(tr, "bg", "lgr_ribbon_dk");
            PopupKit.Outlined(ribbon, "face", "lgr_ribbon", rem * 0.3f, PopupKit.Line);
            TextMeshProUGUI rt = UiKit.Text(ribbon, "text", TextKind.Body, "플래티넘 리그 보상", "stage_ink");
            rt.fontStyle = FontStyles.Bold;
            PopupKit.Ring(rt, "pp_line", 0.15f);

            float y = ribbonH + rem * 1.2f;
            TextMeshProUGUI desc = UiKit.Text(card, "desc", TextKind.Sub, "현재 순위(" + myRank + ")를 유지하면 시즌 종료 시\n다음 보상을 받을 수 있습니다:", "stage_ink");
            desc.fontStyle = FontStyles.Bold;
            desc.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Place(desc.rectTransform, rem * 1.1f, y, inner - rem * 2.2f, descH);
            y += descH + rem * 0.5f;
            RectTransform grid = UiKit.Box(card, "grid");
            UiKit.Place(grid, rem * 1.1f, y, inner - rem * 2.2f, gridH);
            RewardGrid(grid, cur, inner - rem * 2.2f, gridRowH, "lgr_pill", "stage_ink");
            y += gridH + rem * 0.55f;
            double remain = (h.LeagueState.SeasonEndsAt - h.NowMs) / 1000;
            RectTransform collect = UiKit.Box(card, "collect");
            float collectW = inner * 0.55f;
            UiKit.Place(collect, (cardW - collectW) * 0.5f, y, collectW, collectH);
            PopupKit.Outlined(collect, "face", "lgr_collect", rem * 0.5f, PopupKit.Line3);
            TextMeshProUGUI c1 = UiKit.Text(collect, "label", TextKind.Sub, "수집까지:", "pp_ink");
            c1.fontStyle = FontStyles.Bold;
            UiKit.Place(c1.rectTransform, 0f, rem * 0.2f, collectW, collectH * 0.45f);
            TextMeshProUGUI c2 = UiKit.Text(collect, "time", TextKind.Sub, PopupKit.FmtTime(remain), "pp_green_dk");
            c2.fontStyle = FontStyles.Bold;
            UiKit.Place(c2.rectTransform, 0f, collectH * 0.5f, collectW, collectH * 0.45f);
            y += collectH + rem * 0.65f;

            // 흰 보상 표(카드 좌우 꽉)
            RectTransform table = UiKit.Box(card, "table");
            UiKit.Place(table, 0f, y, cardW, tableH);
            PopupKit.Outlined(table, "face", "pp_paper", rem * 0.7f, PopupKit.Line);
            RectTransform rows = PopupKit.ScrollList(table, "rows", 0f, 0f, rem * 0.3f);
            float tierH = UiKit.H("lgr_tier_h") * 1.15f;
            float rankW = UiKit.H("lgr_rank_w") * 1.5f;
            List<LeagueRewardTier> tiers = h.Meta.League.RewardTiers;
            for (int i = 0; i < tiers.Count; i++)
            {
                LeagueRewardTier t = tiers[i];
                var r = h.League.RewardForRank(t.Rank);
                RectTransform row = PopupKit.Item(rows, "tier-" + t.Rank, -1f, tierH);
                if (i > 0)
                {
                    Image line = UiKit.Line(row, "dash", "pp_line", PopupKit.Line, true);
                    line.rectTransform.offsetMin = new Vector2(rem * 0.5f, 0f);
                    line.rectTransform.offsetMax = new Vector2(-rem * 0.5f, 0f);
                }
                RectTransform rk = UiKit.Box(row, "rank");
                UiKit.Place(rk, rem * 1.1f, 0f, rankW, tierH);
                if (t.Rank <= 3)
                {
                    Image badge = PopupKit.IconOr(rk, "badge", t.Rank <= 2 ? "crown" : "badge");
                    UiKit.Anchor(badge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, rem * 2.15f, rem * 2.15f);
                }
                TextMeshProUGUI lab = UiKit.Text(rk, "label", TextKind.Sub, t.Rank <= 3 ? t.Rank.ToString() : t.Label, "stage_ink");
                lab.fontStyle = FontStyles.Bold;
                PopupKit.Ring(lab, "pp_line", 0.2f);
                RectTransform g = UiKit.Box(row, "grid");
                float gx = rem * 1.1f + rankW + rem * 0.6f;
                UiKit.Place(g, gx, (tierH - gridH) * 0.5f, cardW - gx - rem * 1.1f, gridH);
                RewardGrid(g, r, cardW - gx - rem * 1.1f, gridRowH, "lgr_table_pill", "pp_ink");
            }

            PopupKit.XButton(card, () => { h.Popups.Hide(RewardsName); Open(h); });
        }

        /// <summary>재화 6종 3열 pill 격자(원작 leagueRewardGrid).</summary>
        private static void RewardGrid(RectTransform grid, Forge.Core.Data.OrderedMap<double> r, float gw, float rowH, string pillKey, string inkKey)
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
            float pillH = UiKit.L("lc_pill_h") * w * 1.4f;
            List<LeagueChallengeOption> list = h.League.ChallengeList(h.LeagueState);
            float cardH = rem * 1.1f + PopupKit.FontSize(TextKind.Title) * 1.3f + PopupKit.FontSize(TextKind.Sub) * 1.5f + rem * 1.05f + pillH + rem * 1.9f + list.Count * (rowH + rem * 0.5f) + rem * 1.15f + rem * 1.1f;
            RectTransform card = PopupKit.Card(root, "card", cardW, cardH, "pp_paper", rem);
            PopupKit.Column(card, UiKit.H("card_pad"), 0f);
            TextMeshProUGUI title = PopupKit.Label(card, "title", TextKind.Title, "상대 선택", "stage_ink");
            PopupKit.Ring(title);
            PopupKit.Label(card, "desc", TextKind.Sub, "도전 티켓은 매일 09:00에 보충됩니다!", "pp_ink", TextAlignmentOptions.Center, false, true, PopupKit.FontSize(TextKind.Sub) * 1.5f);
            PopupKit.Spacer(card, rem * 1.05f);
            RectTransform pillRow = PopupKit.Item(card, "ticket-row", -1f, pillH);
            float pillW = UiKit.L("lc_pill_w") * w * 1.3f;
            RectTransform pill = UiKit.Box(pillRow, "pill");
            UiKit.Anchor(pill, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, pillW, pillH);
            UiKit.Rounded(pill, "bg", "pp_ink", rem * 0.5f);
            Image tk = PopupKit.IconOr(pill, "ico", "ticket");
            UiKit.Place(tk.rectTransform, rem * 0.4f, pillH * 0.15f, pillH * 0.7f, pillH * 0.7f);
            TextMeshProUGUI tkT = UiKit.Text(pill, "text", TextKind.Sub, h.LeagueState.Tickets + "/" + h.Meta.League.TicketMax, "stage_ink");
            tkT.fontStyle = FontStyles.Bold;
            tkT.rectTransform.offsetMin = new Vector2(pillH * 0.9f, 0f);
            PopupKit.Spacer(card, rem * 1.9f);

            float av = UiKit.H("lc_avatar");
            float btnW = UiKit.L("lc_btn_w") * w, btnH = rem * 2.9f;
            float rowW = cardW - PopupKit.Line3 * 2f - UiKit.H("card_pad") * 2f + w * 0.0185f * 2f;
            for (int i = 0; i < list.Count; i++)
            {
                LeagueChallengeOption o = list[i];
                RectTransform slot = PopupKit.Item(card, "opp-" + i, -1f, rowH + rem * 0.5f);
                RectTransform row = UiKit.Box(slot, "row");
                UiKit.Anchor(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, rowW, rowH);
                UiKit.Rounded(row, "bg", "challenge_row", rem * 0.7f);
                RectTransform avatar = PopupKit.Avatar(row, "avatar", av, o.Bot.Avatar, rem * 0.4f);
                UiKit.Place(avatar, rem * 0.6f, (rowH - av) * 0.5f, av, av);
                float nx = rem * 0.6f + av + rem * 0.6f;
                float nw = rowW - nx - btnW - rem * 3.5f;
                TextMeshProUGUI nm = UiKit.Text(row, "name", TextKind.Sub, o.Bot.Name, "pp_ink", TextAlignmentOptions.Left);
                nm.fontStyle = FontStyles.Bold;
                UiKit.Place(nm.rectTransform, nx, rowH * 0.12f, nw, rowH * 0.4f);
                TextMeshProUGUI cp = UiKit.Text(row, "cp", TextKind.Sub, "⚔ " + PopupKit.Fmt(o.Bot.Cp), "challenge_cp", TextAlignmentOptions.Left);
                cp.fontStyle = FontStyles.Bold;
                UiKit.Place(cp.rectTransform, nx, rowH * 0.5f, nw, rowH * 0.4f);
                TextMeshProUGUI star = UiKit.Text(row, "star", TextKind.Sub, "★+" + o.StarReward, "pp_line");
                star.fontStyle = FontStyles.Bold;
                UiKit.Place(star.rectTransform, rowW - rem * 0.6f - btnW - rem * 2.6f, 0f, rem * 2.6f, rowH);
                int idx = o.Index;
                bool can = h.League.CanChallenge(h.LeagueState, idx);
                Button b = PopupKit.Btn(row, "challenge", "도전  🎟1", "challenge_btn", "challenge_btn_dk", () => OnChallenge(h, idx), btnW, btnH, "pp_line", TextKind.Sub, !can);
                UiKit.Place(b.GetComponent<RectTransform>(), rowW - rem * 0.6f - btnW, (rowH - btnH) * 0.5f, btnW, btnH);
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
