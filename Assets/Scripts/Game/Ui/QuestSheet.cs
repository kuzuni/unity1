using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Meta;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 반복 퀘스트 시트(ROUTINE T22 · 원작 ui.js openQuests/onClaimQuest/onClaimAllQuests · 주인 지시 2026-08-18 quest-tab · quest-claim-all).
    /// 흰 전체화면 + 제목 «퀘스트» + 부제 + [일괄수령] + 세로 목록(아이콘 · 이름+요구치 · 진행 바 · 보상 · [수령]). 날짜·일차 표기는 넣지 않는다(사양).
    /// </summary>
    public static class QuestSheet
    {
        public const string Name = "quest";

        public static void Open(MetaHost h)
        {
            h.Popups.Show(Name, "quest");
            Render(h);
        }

        public static void Close(MetaHost h) { h.Popups.Hide(Name); }

        public static void Render(MetaHost h)
        {
            Popup p = h.Popups.Find(Name);
            if (p == null) return;
            RectTransform root = PopupLayer.Clear(p);
            RectTransform sheet = PopupKit.Sheet(root, "sheet", "pp_paper");
            float rem = PopupKit.Rem;
            float w = UiKit.RefW;

            RectTransform box = UiKit.Box(sheet, "scroll");
            UiKit.Band(box, 0f, UiKit.L("tabbar_top"));
            RectTransform content = PopupKit.ScrollList(box, "list", UiKit.H("quest_row_gap"), 0f, UiKit.H("sheet_pad_top"));

            TextMeshProUGUI title = PopupKit.Label(content, "title", TextKind.Title, "퀘스트", "stage_ink");
            PopupKit.Ring(title, "pp_line", 0.2f);
            PopupKit.Label(content, "sub", TextKind.Sub, "모든 퀘스트는 수령해도 같은 내용으로 반복됩니다", "quest_sub", TextAlignmentOptions.Center, true);
            PopupKit.Spacer(content, rem * 0.45f);

            List<Quest> list = h.Quests.List(h.QuestState);
            int ready = h.Quests.ReadyCount(h.QuestState);
            float rowW = UiKit.L("quest_row_w") * w;
            float btnW = UiKit.H("quest_btn_w") * 1.6f, btnH = UiKit.H("quest_btn_h") * 1.3f;

            RectTransform allBar = PopupKit.Item(content, "allbar", -1f, btnH);
            Button all = null;
            all = PopupKit.Btn(allBar, "claim-all", "일괄수령" + (ready > 0 ? " (" + ready + ")" : ""), "pp_green", "pp_green_dk", () => OnClaimAll(h, all ? all.GetComponent<RectTransform>() : null), btnW * 1.6f, btnH, "stage_ink", TextKind.Sub, ready == 0);
            UiKit.Anchor(all.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), new Vector2(rowW * 0.5f, 0f), btnW * 1.6f, btnH);

            if (list.Count == 0) PopupKit.Label(content, "empty", TextKind.Body, "퀘스트를 불러오지 못했습니다", "pp_muted");

            float icon = UiKit.H("quest_icon");
            float barH = UiKit.H("quest_bar_h") * 1.6f;
            float rowH = rem * 0.55f * 2f + PopupKit.FontSize(TextKind.Sub) * 1.3f + barH + rem * 0.28f;
            for (int i = 0; i < list.Count; i++)
            {
                Quest q = list[i];
                QuestDef def = h.Quests.Def(q.Id);
                bool done = h.Quests.IsDone(q);
                double pct = Mathf.Clamp01((float)(q.Prog / q.Need));
                string unit = def.Unit == null ? "회" : def.Unit;

                RectTransform slot = PopupKit.Item(content, "q-" + q.Id, -1f, rowH);
                RectTransform row = UiKit.Box(slot, "row");
                UiKit.Anchor(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, rowW, rowH);
                PopupKit.Outlined(row, "face", done ? "quest_done_bg" : "pp_paper", rem * 0.8f, PopupKit.Line3, done ? "pp_green" : "pp_line");
                UiShadow.Drop(row, "qstrow_lip", rem * 0.8f);   // 정본 .qst-row(2026) `0 .25rem 0 rgba(0,0,0,.3)` — 종이 카드가 한 겹 떠 있다

                float padX = rem * 0.7f, padY = rem * 0.55f;
                RectTransform icoBox = UiKit.Box(row, "icon");
                UiKit.Place(icoBox, padX, (rowH - icon) * 0.5f, icon, icon);
                PopupKit.IconOr(icoBox, "img", def.Icon);

                float bodyX = padX + icon + rem * 0.6f;
                float rightW = btnW + rem * 0.4f;
                float bodyW = rowW - bodyX - rightW - padX;
                TextMeshProUGUI name = UiKit.Text(row, "name", TextKind.Sub, def.Text + " " + PopupKit.Fmt(q.Need) + unit, "pp_ink", TextAlignmentOptions.Left);
                name.fontStyle = FontStyles.Bold;
                UiKit.Place(name.rectTransform, bodyX, padY, bodyW, PopupKit.FontSize(TextKind.Sub) * 1.3f);

                RectTransform bar = UiKit.Box(row, "bar");
                UiKit.Place(bar, bodyX, padY + PopupKit.FontSize(TextKind.Sub) * 1.3f + rem * 0.28f, bodyW, barH);
                UiKit.Rounded(bar, "bg", "quest_bar_bg", barH * 0.5f);
                Image fill = UiKit.Rounded(bar, "fill", done ? "quest_bar_done" : "quest_bar", barH * 0.5f);
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = new Vector2((float)pct, 1f);
                fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
                // T178 6회차 — 정본 `.qst-bar i`(2044)·`.qst-row.done .qst-bar i`(2047)는 **두 겹**이다: 세로 색 띠 + 위 1 CSS px 흰 광택.
                //   색 한 칸으로는 «채움이 평평해» 보인다. 상태(파랑/초록)는 정본 주석대로 **띠 키**로만 가른다 — 광택은 두 상태가 같다.
                float fillW = bodyW * (float)pct;
                SurfaceArt.FillMasked(fill, "qst-fill-grad", done ? "qst_bar_done_ramp" : "qst_bar_ramp", fillW, barH);
                // T178 10회차 — 림의 바탕은 **상태로 갈린다**(파랑 `qst_bar_ramp` ↔ 초록 `qst_bar_done_ramp`)라 표의 `over_layer` 한 칸으로는 못 적는다.
                //   그래서 부르는 쪽이 그때의 바탕 겹을 알려 준다 — 그러면 굽는 쪽이 정본이 섞는 길(sRGB)로 미리 합성한다(T357 · 8회차의 사슬과 같은 값).
                SurfaceArt.Fill(fill.rectTransform, "qst-fill-rim", "qst_bar_rim", fillW, barH, done ? "qst_bar_done_ramp" : "qst_bar_ramp");
                TextMeshProUGUI progT = UiKit.Text(bar, "prog", TextKind.Sub, PopupKit.Fmt(System.Math.Min(q.Prog, q.Need)) + "/" + PopupKit.Fmt(q.Need), "pp_ink");
                progT.fontStyle = FontStyles.Bold;

                RectTransform right = UiKit.Box(row, "right");
                UiKit.Place(right, rowW - padX - btnW, 0f, btnW, rowH);
                RectTransform rw = UiKit.Box(right, "reward");
                UiKit.Place(rw, 0f, padY * 0.6f, btnW, icon * 0.6f);
                Image rwIco = PopupKit.IconOr(rw, "ico", ShopSheet.CurIcon(q.Rw.Cur));
                UiKit.Place(rwIco.rectTransform, 0f, 0f, icon * 0.6f, icon * 0.6f);
                TextMeshProUGUI rwT = UiKit.Text(rw, "amt", TextKind.Sub, PopupKit.Fmt(q.Rw.Amt), "pp_ink", TextAlignmentOptions.Left);
                rwT.fontStyle = FontStyles.Bold;
                rwT.rectTransform.offsetMin = new Vector2(icon * 0.65f, 0f);
                int idx = i;
                Button claim = null;
                claim = PopupKit.Btn(right, "claim", "수령", done ? "pp_green" : "pp_gray", done ? "pp_green_dk" : "pp_gray_dk", () => OnClaim(h, idx, claim ? claim.GetComponent<RectTransform>() : null), btnW, btnH, "stage_ink", TextKind.Sub, !done);
                UiKit.Place(claim.GetComponent<RectTransform>(), 0f, rowH - padY * 0.6f - btnH, btnW, btnH);
            }
            PopupKit.Spacer(content, UiKit.RefH - PopupKit.TabTop + rem * 0.9f);

            PopupKit.SheetBack(sheet, () => Close(h));
        }

        private static void OnClaim(MetaHost h, int i, RectTransform from)
        {
            QuestClaim got = h.Quests.Claim(h.QuestState, h.Wallet, i);
            if (got == null) return;
            // 정본 ui.js 4607 — 토스트 없음 · «+획득량» 라벨이 같은 정보를 같은 자리에서 말한다(T134 3회차)
            RewardBurst.Play(RewardBurst.Rewards(got.Cur, got.Amt), from);
            h.Touch();
        }

        private static void OnClaimAll(MetaHost h, RectTransform from)
        {
            QuestClaimAll r = h.Quests.ClaimAll(h.QuestState, h.Wallet);
            if (r.N == 0) { h.Toast("📜 수령할 수 있는 퀘스트가 없습니다"); return; }
            // 정본 ui.js 4623 — 연출을 토스트보다 **먼저**(정본 주석: rewardBurst 가 토스트 보류를 세운다 · 뒤에 부르면 «+획득량» 과 겹친다)
            RewardBurst.Play(RewardBurst.Rewards(r.Gains, null), from);
            var parts = new List<string>();
            for (int i = 0; i < r.Gains.Count; i++)
            {
                string cur = r.Gains.KeyAt(i);
                parts.Add(h.Meta.Quests.CurKr.Get(cur, cur) + " +" + PopupKit.Fmt(r.Gains.ValueAt(i)));
            }
            h.Touch();
            h.Toast("📜 " + r.N + "개 수령! " + string.Join(" · ", parts.ToArray()));
        }
    }
}
