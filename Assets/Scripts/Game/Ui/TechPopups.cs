using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Tech;
using Forge.Game.Audio;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 기술 노드 팝업 + ⓘ 총 보너스 팝업(ROUTINE T21 · 원작 ui.js openTechNode/renderTechNodeModal/onTechStart/onTechGemSkip/onTechClaim/openTechBonuses/renderTechBonuses · 샷 042605).
    /// 노드 팝업 상태: 만렙 «연구 완료 (MAX)» · 잠김 «잠김 + 부모 안내» · 완료 대기 «[완료 · Lv.x → Lv.x+1]» · 연구 중 «진행바 + [건너뛰기 ◆N]»(취소 없음 · 주인 지시) · 대기 «[연구 시작 · 🧪비용 · ⏳시간]».
    /// </summary>
    public static class TechPopups
    {
        public enum NodeState { Max, Locked, Ready, Researching, Idle }

        static RectTransform overlay;
        static string curId;
        static RectTransform progFill;
        static TextMeshProUGUI progTime;
        static float progW;

        public static bool IsNodeOpen { get { return overlay != null && curId != null; } }
        public static bool IsBonusesOpen { get { return overlay != null && curId == null; } }
        public static string NodeId { get { return curId; } }
        public static NodeState State { get; private set; }
        public static Button ActionButton { get; private set; }
        public static string NameText { get; private set; }
        public static string MainText { get; private set; }
        public static int BonusRowCount { get; private set; }

        static DungeonUiHost Host { get { return DungeonUiHost.Instance; } }
        static TechTree Tree { get { return Host.Tech; } }

        public static void Close()
        {
            if (overlay == null) return;
            UnityEngine.Object.Destroy(overlay.gameObject);
            overlay = null; curId = null; progFill = null; progTime = null; ActionButton = null;
        }

        // ===== 노드 팝업 =====

        public static void OpenNode(string id)
        {
            if (!DungeonUiHost.Ready) return;
            curId = id;
            RenderNode();
        }

        public static void OnStart()
        {
            if (Tree.Start(curId, Host, Host.Now())) AfterChange();
            else DungeonPopups.Toast("🧪 물약이 부족하거나 다른 연구가 진행 중입니다");
        }

        public static void OnGemSkip()
        {
            if (Tree.GemSkip(Host, Host.Now())) AfterChange();
            else DungeonPopups.Toast("💎 젬이 부족합니다");
        }

        public static void OnClaim()
        {
            // 정본 techtree.js 382: 연구가 실제로 완료된 그 자리에서 `SFX.levelUp()` 이 운다(토스트 앞) · T120
            string done;
            if (Tree.Claim(Host.Now(), out done))
            {
                Sfx.LevelUp();
                // 정본 techtree.js 383: «🔬 <이름> <단계>단계 Lv.N 연구 완료!» — 소리 뒤에 말한다(성공 갈래 · T143 ⓑ · 실패 갈래 둘은 위에 이미 있다)
                TechNodeDef d = Tree.Def(done);
                DungeonPopups.Toast("🔬 " + (d != null ? d.Name : done) + " " + Tree.TierLabel(done) + "단계 Lv." + Tree.Level(done) + " 연구 완료!");
                AfterChange();
            }
        }

        static void AfterChange()
        {
            Host.SaveTechState();
            if (curId != null) RenderNode();
            if (TechPanel.Instance != null) TechPanel.Instance.RefreshAfterChange();
            else Host.RenderTopBar();
        }

        /// <summary>1초 틱 — 연구 중 진행바·시간 갱신, 끝나면 완료 대기 상태로 다시 그린다.</summary>
        public static void Tick()
        {
            if (!IsNodeOpen) return;
            if (State == NodeState.Researching)
            {
                if (Tree.IsDone(Host.Now())) { RenderNode(); return; }
                double remain = (Tree.State.Research.EndsAt - Host.Now()) / 1000;
                double total = Tree.Time(curId, Tree.Level(curId) + 1) ?? 1;
                if (progTime != null) progTime.text = NumFmt.FmtTime(remain);
                if (progFill != null) progFill.sizeDelta = new Vector2(progW * Mathf.Clamp01((float)(1 - remain / total)), progFill.sizeDelta.y);
            }
        }

        static void RenderNode()
        {
            string id = curId;
            Close();
            curId = id;
            TechNodeDef def = Tree.Def(id);
            int lv = Tree.Level(id);
            bool max = Tree.IsMax(id);
            bool researching = Tree.ResearchingId() == id;
            bool other = Tree.ResearchingId() != null && !researching;
            bool open = Tree.IsUnlocked(id);
            string unit = Tree.UnitOf(id);
            State = max ? NodeState.Max : !open ? NodeState.Locked : researching && Tree.IsDone(Host.Now()) ? NodeState.Ready : researching ? NodeState.Researching : NodeState.Idle;

            float W = UiKit.RefW;
            overlay = DungeonPopups.Overlay("modal-tech-node");
            float cw = W * UiKit.L("idet_card_w");
            float pad = cw * UiKit.L("idet_pad");
            float inner = cw - pad * 2f;
            float icoD = cw * UiKit.L("idet_icon");
            float gap = cw * UiKit.L("idet_gap");
            float bodyH = DungeonPopups.LineH(TextKind.Body), subH = DungeonPopups.LineH(TextKind.Sub);
            float headH = Mathf.Max(icoD, bodyH + subH * 2f);
            float descH = researching ? 0f : cw * UiKit.L("idet_subs_mt") + cw * UiKit.L("idet_subs_pad") * 2f + subH * 2f;
            float actionH = ActionHeight(State);
            float ch = pad * 2f + headH + descH + actionH + DungeonPopups.RemL("card_gap_rem");
            RectTransform card = DungeonPopups.Card(overlay, "card", cw, ch, DungeonPopups.RemL("card_radius_rem"));

            // 머리: 청동 원 아이콘(lv/5 배지) + 이름 · 단계 · 총합 · (레벨당 · 이 노드)
            float y = pad;
            RectTransform ic = UiKit.Box(card, "icon");
            UiKit.Place(ic, pad, y, icoD, icoD);
            DungeonPopups.BorderedCircle(ic, "circle", "tn_bronze", DungeonPopups.Line2, "tn_bronze_border");
            Image face = max ? UiKit.Icon(ic, "face", "check") : TechPanel.TechIcon(ic, "face", id);
            float fd = icoD * UiKit.L("idet_icon_face");
            UiKit.Anchor(face.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, fd, fd);
            if (!open && !max) ic.gameObject.AddComponent<CanvasGroup>().alpha = UiKit.L("tt_tlocked_alpha");
            TextMeshProUGUI star = DungeonPopups.Bold(ic, "star", TextKind.Sub, lv + "/" + Tree.Table.MaxLevel, "pp_ink", TextAlignmentOptions.Left);
            UiKit.Place(star.rectTransform, icoD * 0.06f, icoD - subH * 0.4f, icoD * 1.5f, subH);

            float tx = pad + icoD + gap;
            float tw = inner - icoD - gap;
            NameText = def.Name;
            TextMeshProUGUI name = DungeonPopups.Bold(card, "name", TextKind.Body, def.Name, "pp_ink", TextAlignmentOptions.Left);
            UiKit.Place(name.rectTransform, tx, y, tw, bodyH);
            TextMeshProUGUI lvl = DungeonPopups.Bold(card, "lv", TextKind.Sub, Tree.TierLabel(id) + "단계 · Lv." + lv + "/" + Tree.Table.MaxLevel, "pp_muted", TextAlignmentOptions.Left);
            UiKit.Place(lvl.rectTransform, tx, y + bodyH, tw, subH);
            MainText = "+" + NumFmt.Fmt(Tree.TotalOf(id)) + unit;
            TextMeshProUGUI main = DungeonPopups.Bold(card, "main", TextKind.Sub, MainText + "  (" + Tree.GainNote() + " +" + NumFmt.Fmt(def.Per) + unit + " · 이 노드 +" + NumFmt.Fmt(Tree.NodeTotal(id)) + unit + ")", "pp_ink", TextAlignmentOptions.Left);
            UiKit.Place(main.rectTransform, tx, y + bodyH + subH, tw, subH);
            y += headH;

            if (!researching)
            {
                y += cw * UiKit.L("idet_subs_mt");
                float spad = cw * UiKit.L("idet_subs_pad");
                float sh = spad * 2f + subH * 2f;
                RectTransform subs = UiKit.Box(card, "subs");
                UiKit.Place(subs, pad, y, inner, sh);
                UiKit.Rounded(subs, "bg", "pp_panel", DungeonPopups.RemL("idet_subs_radius_rem"));
                TextMeshProUGUI lead = DungeonPopups.Para(subs, "desc", TextKind.Sub, def.Desc, "pp_ink", TextAlignmentOptions.Left);
                lead.fontStyle = FontStyles.Bold;
                UiKit.Place(lead.rectTransform, spad, spad, inner - spad * 2f, subH * 2f);
                y += sh;
            }
            y += DungeonPopups.RemL("card_gap_rem");
            RenderAction(card, State, id, lv, pad, inner, y);
            DungeonPopups.XButton(card, Close);
        }

        static float ActionHeight(NodeState s)
        {
            float subH = DungeonPopups.LineH(TextKind.Sub), btnH = DungeonPopups.RemL("tech_btn_h_rem"), gap = DungeonPopups.RemL("card_gap_rem");
            switch (s)
            {
                case NodeState.Max: return subH;
                case NodeState.Locked: return DungeonPopups.RemL("btn_sm_h_rem") + gap + subH * 2f;
                case NodeState.Ready: return subH + gap + DungeonPopups.RemL("tech_prog_h_rem") + DungeonPopups.RemL("tech_claim_mt_rem") + btnH;
                case NodeState.Researching: return subH + gap + DungeonPopups.RemL("tech_prog_h_rem") + DungeonPopups.RemL("tech_claim_mt_rem") + btnH;
                default: return btnH + gap + subH;
            }
        }

        // T345 8회차 — 정본 4612 `.tech-btns .btn { border-radius: .6rem }` 이 공용 `.btn`(663 .55)을 덮는다: 이 줄의 버튼 넷만 표 `tech_btn_r_rem` 을 쓴다.
        static void RenderAction(RectTransform card, NodeState s, string id, int lv, float pad, float inner, float y)
        {
            float subH = DungeonPopups.LineH(TextKind.Sub), btnH = DungeonPopups.RemL("tech_btn_h_rem"), gap = DungeonPopups.RemL("card_gap_rem");
            float cx = pad + inner * 0.5f;
            ActionButton = null;
            if (s == NodeState.Max)
            {
                TextMeshProUGUI t = DungeonPopups.Bold(card, "lead", TextKind.Sub, "연구 완료 (MAX)", "pp_ink");
                UiKit.Place(t.rectTransform, pad, y, inner, subH);
                return;
            }
            if (s == NodeState.Locked)
            {
                float bw = inner * UiKit.L("tech_btn_w"), bh = DungeonPopups.RemL("btn_sm_h_rem");
                ActionButton = DungeonPopups.Pill(card, "locked", "잠김", DungeonPopups.Skin.Gray, TextKind.Button, null, RadiusUi.Px("tech_btn_r_rem"), false);
                UiKit.Place(DungeonPopups.Root(ActionButton), cx - bw * 0.5f, y, bw, bh);
                List<string> need = Tree.LockedBy(id);
                var names = new List<string>();
                for (int i = 0; i < need.Count; i++) { TechNodeDef pd = Tree.Def(need[i]); names.Add((pd != null ? pd.Name : need[i]) + " " + Tree.Roman(Tree.TierOf(need[i])) + "단계"); }
                string what = names.Count > 1 ? string.Join(" · ", names.ToArray()) + "를 각각" : (names.Count == 1 ? names[0] : "위 노드") + "를";
                TextMeshProUGUI t = DungeonPopups.Para(card, "hint", TextKind.Sub, what + " 1레벨 이상 올리면 열립니다", "pp_muted", TextAlignmentOptions.Center);
                UiKit.Place(t.rectTransform, pad, y + bh + gap, inner, subH * 2f);
                return;
            }
            if (s == NodeState.Ready || s == NodeState.Researching)
            {
                bool ready = s == NodeState.Ready;
                TextMeshProUGUI lead = DungeonPopups.Bold(card, "lead", TextKind.Sub, ready ? "연구 시간 종료 — 수령 대기" : "연구 진행 중 (취소 불가)", "pp_ink");
                UiKit.Place(lead.rectTransform, pad, y, inner, subH);
                y += subH + gap;
                float ph = DungeonPopups.RemL("tech_prog_h_rem");
                float pr = DungeonPopups.RemL("tech_prog_r_rem");
                RectTransform prog = UiKit.Box(card, "prog");
                UiKit.Place(prog, pad, y, inner, ph);
                RectTransform track = DungeonPopups.Bordered(prog, "bg", "tech_prog_bg", pr, DungeonPopups.Line3);
                progW = inner - DungeonPopups.Line3 * 2f;
                double remain = ready ? 0 : (Tree.State.Research.EndsAt - Host.Now()) / 1000;
                double total = Tree.Time(id, lv + 1) ?? 1;
                float frac = ready ? 1f : Mathf.Clamp01((float)(1 - remain / total));
                Image fill = UiKit.Rounded(track, "fill", "pp_blue", Mathf.Max(0f, pr - DungeonPopups.Line3));
                UiKit.Place(fill.rectTransform, 0f, 0f, progW * frac, ph - DungeonPopups.Line3 * 2f);
                progFill = fill.rectTransform;
                progTime = DungeonPopups.Bold(prog, "time", TextKind.Sub, ready ? "완료" : NumFmt.FmtTime(remain), "white");
                UiKit.Fill(progTime.rectTransform);
                y += ph + DungeonPopups.RemL("tech_claim_mt_rem");
                if (ready)
                {
                    float bw = inner * UiKit.L("tech_claim_w");
                    ActionButton = DungeonPopups.Pill(card, "claim", "완료 · Lv." + lv + " → Lv." + (lv + 1), DungeonPopups.Skin.Blue, TextKind.Button, OnClaim, RadiusUi.Px("tech_btn_r_rem"));
                    UiKit.Place(DungeonPopups.Root(ActionButton), cx - bw * 0.5f, y, bw, btnH);
                }
                else
                {
                    float bw = inner * UiKit.L("tech_claim_w");
                    ActionButton = DungeonPopups.Pill(card, "skip", "건너뛰기\n◆ " + NumFmt.Fmt(Tree.GemSkipCost(Host.Now())), DungeonPopups.Skin.Silver, TextKind.Button, OnGemSkip, RadiusUi.Px("tech_btn_r_rem"));
                    UiKit.Place(DungeonPopups.Root(ActionButton), cx - bw * 0.5f, y, bw, btnH);
                }
                return;
            }
            // 대기: [연구 시작 · 🧪cost · ⏳time]
            double cost = Tree.NextCost(id) ?? 0;
            double time = Tree.Time(id, lv + 1) ?? 0;
            bool other = Tree.ResearchingId() != null;
            bool disabled = other || Host.Potions < cost;
            ActionButton = DungeonPopups.Pill(card, "start", "연구 시작 · 물약 " + NumFmt.Fmt(cost) + " · " + NumFmt.FmtTime(time), disabled ? DungeonPopups.Skin.Gray : DungeonPopups.Skin.Blue, TextKind.Button, OnStart, RadiusUi.Px("tech_btn_r_rem"), !disabled);
            UiKit.Place(DungeonPopups.Root(ActionButton), pad, y, inner, btnH);
            if (other)
            {
                TextMeshProUGUI t = DungeonPopups.Bold(card, "hint", TextKind.Sub, "다른 연구가 진행 중입니다", "pp_muted");
                UiKit.Place(t.rectTransform, pad, y + btnH + gap, inner, subH);
            }
        }

        // ===== ⓘ 총 보너스 =====

        public static void OpenBonuses()
        {
            if (!DungeonUiHost.Ready) return;
            Close();
            List<TechBonusLine> lines = Tree.TotalBonuses();
            BonusRowCount = lines.Count;
            float W = UiKit.RefW, H = UiKit.RefH;
            overlay = DungeonPopups.Overlay("modal-tech-bonuses");
            float cw = W * UiKit.L("tbn_card_w");
            float pad = DungeonPopups.RemL("card_pad_rem");
            float titleH = DungeonPopups.LineH(TextKind.Button);
            float rowH = DungeonPopups.LineH(TextKind.Sub) + DungeonPopups.RemL("tbn_row_pad_rem") * 2f;
            float listH = Mathf.Min(H * UiKit.L("tbn_list_maxh"), Mathf.Max(rowH, lines.Count * rowH));
            float ch = pad * 2f + titleH + DungeonPopups.RemL("card_gap_rem") + listH;
            RectTransform card = DungeonPopups.Card(overlay, "card", cw, ch, DungeonPopups.RemL("card_radius_rem"));
            TextMeshProUGUI t = DungeonPopups.Bold(card, "title", TextKind.Button, "총 보너스", "pp_ink");
            UiKit.Place(t.rectTransform, 0f, pad, cw, titleH);
            float y = pad + titleH + DungeonPopups.RemL("card_gap_rem");
            RectTransform viewport = UiKit.Box(card, "list-viewport");
            UiKit.Place(viewport, pad, y, cw - pad * 2f, listH);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = UiKit.Box(viewport, "list");
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(0f, Mathf.Max(rowH, lines.Count * rowH));
            if (lines.Count > 0)
            {
                ScrollRect sr = viewport.gameObject.AddComponent<ScrollRect>();
                Image vhit = viewport.gameObject.AddComponent<Image>(); vhit.color = new Color(0f, 0f, 0f, 0f);
                sr.viewport = viewport; sr.content = content; sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped;
            }
            float lw = cw - pad * 2f;
            if (lines.Count == 0)
            {
                TextMeshProUGUI e = UiKit.Text(content, "empty", TextKind.Sub, "아직 연구한 기술이 없습니다", "muted2");
                UiKit.Place(e.rectTransform, 0f, 0f, lw, rowH);
            }
            for (int i = 0; i < lines.Count; i++)
            {
                RectTransform row = UiKit.Box(content, "row-" + lines[i].Id);
                UiKit.Place(row, 0f, i * rowH, lw, rowH);
                TextMeshProUGUI l = DungeonPopups.Bold(row, "label", TextKind.Sub, lines[i].Label, "pp_ink", TextAlignmentOptions.Left);
                UiKit.Place(l.rectTransform, 0f, 0f, lw * 0.6f, rowH);
                TextMeshProUGUI v = DungeonPopups.Bold(row, "val", TextKind.Sub, lines[i].Text, "tb_val", TextAlignmentOptions.Right);
                UiKit.Place(v.rectTransform, lw * 0.6f, 0f, lw * 0.4f, rowH);
                if (i < lines.Count - 1) UiKit.Line(row, "line", "tb_line", DungeonPopups.Line2, false);
            }
            DungeonPopups.XButton(card, Close);
        }
    }
}
