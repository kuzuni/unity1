using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Data;
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
        /// <summary>T477 — 진행바 채움의 폭(테 두 겹 안 · 좌우 1.33% 들여쓴 뒤) · 자가 본다.</summary>
        public static float ProgWidthForTest { get { return progW; } }

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
            float cw = W * UiKit.L("idet_card_w") - DungeonPopups.Line3 * 2f;   // T473 — 표값은 정본 CSS width(border-box) · Card 의 w 는 패딩 상자
            // T477 — CSS % 의 밑변을 자리마다 가른다(정본 3654 주석 «백분율 기준은 padding=래퍼 폭, width/margin=카드 안쪽 폭»):
            //   카드 제 `padding: 4.4%`(3646)는 **앱 폭**(컨테이닝 블록 = 모달 ≈ 앱 · 아래 130 의 `padB` 와 같은 밑변),
            //   카드 안 자식의 `width`(4638 아이콘 15.1%)·`gap`(3682 4.2%)·`margin`·`padding`(3703 subs 7%/4% · 4643 진행바 1.33% · 4642 머리 −3.2%)은 **카드 콘텐츠 폭**(`inner`).
            //   종전엔 다섯 자리를 다 패딩 상자 폭 `cw` 에 곱해 패딩이 작고(−1.04%p/변) 아이콘이 크고(+1.33%p) 진행바가 +3.8%p 넓었다(런 1205).
            float pad = W * UiKit.L("idet_pad");
            float inner = cw - pad * 2f;
            float icoD = inner * UiKit.L("idet_icon");
            float gap = inner * UiKit.L("idet_gap");
            float bodyH = DungeonPopups.LineH(TextKind.Body), subH = DungeonPopups.LineH(TextKind.Sub);
            float headH = Mathf.Max(icoD, bodyH + subH);   // T413 — 정본 머리는 «이름+레벨 / 총합» 두 줄이다(전엔 subH * 2 로 세 줄을 셌다)
            float descH = researching ? 0f : inner * UiKit.L("idet_subs_mt") + inner * UiKit.L("idet_subs_pad") * 2f + subH * 2f;
            // T430 — 정본 4629~4635 가 까닭까지 적었다: «패널을 지우면 카드가 짧아지므로 지운 만큼을 ⓐ «연구 진행 중» 위 여백과
            //   ⓑ 카드 아래 여백으로 되돌려 원본의 세로 리듬을 만든다». 클론은 **지우는 쪽(`descH`)만** 옮겨 카드가 −22% 짧았다(T28 93회차 실측).
            //   ⓑ = 4635 `.item-detail.tn-researching { padding-bottom: 10.6% }` — 아래 패딩만 바뀜다(위는 그대로 `pad`).
            float padB = researching ? W * TechStyle.L("tn_researching_pad_b_app_f") : pad;   // 밑변은 **앱 폭** — 카드 자신의 padding % 라 그 컨테이닝 블록(모달)을 잰다
            float actionH = ActionHeight(State, inner);
            float ch = pad + padB + headH + descH + actionH + DungeonPopups.RemL("card_gap_rem");
            RectTransform card = DungeonPopups.Card(overlay, "card", cw, ch, DungeonPopups.RemL("card_r_rem"));

            // 머리: 청동 원 아이콘(lv/5 배지) + 이름 · 단계 · 총합 · (레벨당 · 이 노드)
            float y = pad;
            // T477 — 정본 4642 `.item-detail[data-tech-node] .idet-head { margin-left: -3.2% }`(카드 콘텐츠 폭 기준 · 4640 «머리만 본문보다 왼쪽 — 아이콘 인셋 14px ↔ 진행바 28px»):
            //   머리(아이콘 + 제목 블록)는 안쪽 왼변보다 그만큼 왼쪽에서 시작하고 그만큼 넓다. 표 TechUi `idet_head_ml_f`(음수).
            float headX = pad + inner * TechStyle.L("idet_head_ml_f");
            RectTransform ic = UiKit.Box(card, "icon");
            UiKit.Place(ic, headX, y, icoD, icoD);
            RectTransform circleFace = DungeonPopups.BorderedCircle(ic, "circle", "tn_bronze", DungeonPopups.Line2, "tn_bronze_border");
            // T178 19회차 — 정본 3689 `.idet-icon.tn-bronze { background: linear-gradient(160deg, #d9a066, #a5642f) }`: 청동 원은 단색이 아니라 비스듬한 겹이다.
            //   원 면(UiKit.Circle 스프라이트)에 마스크를 걸고 그 안에 표 `tn_bronze` 겹 한 장(SurfaceArt.FillMasked) — 테(#7a4a22)는 그대로.
            Image circleImg = circleFace.GetComponent<Image>();
            SurfaceArt.FillMasked(circleImg, "bg-grad", "tn_bronze", icoD - DungeonPopups.Line2 * 2f, icoD - DungeonPopups.Line2 * 2f);
            Image face = max ? UiKit.Icon(ic, "face", "check") : TechPanel.TechIcon(ic, "face", id);
            float fd = icoD * UiKit.L("idet_icon_face");
            UiKit.Anchor(face.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, fd, fd);
            // T359 6회차 — 정본 **2194** `.idet-icon.tn-dim .ico, .idet-icon.tn-dim img { filter: grayscale(.55); opacity: .85 }`.
            //   붙는 조건은 정본 `ui.js` **5601** `.idet-icon tn-bronze${!open && !max ? ' tn-dim' : ''}` 그대로다 — 여기 조건과 같다.
            //   종전엔 **두 가지가 틀렸다**: ⓐ 값이 `tt_tlocked_alpha`(**0.72**)였는데 그것은 **트리 노드**의 값이다(정본 **2192** `.tech-tree-node.tlocked { opacity: .72 }`).
            //     ⓑ 그 알파를 **원반 상자 전체**에 걸었는데 정본은 `.idet-icon` 자신이 아니라 그 안의 **`.ico`/`img`(= 글리프)** 에만 건다 — 청동 원은 안 흐려진다.
            //   (같은 줄의 `grayscale(.55)` 는 **T342 축**이라 여기서 안 건드린다.)
            if (!open && !max) OpacityUi.Apply(face.gameObject, "idet_icon_tn_dim");
            TextMeshProUGUI star = DungeonPopups.Bold(ic, "star", TextKind.Sub, lv + "/" + Tree.Table.MaxLevel, "pp_ink", TextAlignmentOptions.Left);
            UiKit.Place(star.rectTransform, icoD * 0.06f, icoD - subH * 0.4f, icoD * 1.5f, subH);

            float tx = headX + icoD + gap;
            float tw = (pad + inner) - tx;   // T477 — 머리 상자의 오른변은 안쪽 오른변 그대로(음수 마진은 왼쪽만 늘린다)
            NameText = def.Name;
            TextMeshProUGUI name = DungeonPopups.Bold(card, "name", TextKind.Body, def.Name, "pp_ink", TextAlignmentOptions.Left);
            // T333 19회차 — 정본 8381 묶음 `.modal-card .idet-name` 은 이 조각에도 닿는다: 5601 이 이 카드를 `<div class="modal-card paper item-detail">` 로 세우고
            //   5603 이 이름을 `<div class="idet-name">` 로 둔다. 장비 상세(`ForgeInfoPopup` idet-name)와 **같은 선언·같은 키**다.
            UiKit.TextShadow(name, "paper_emboss");
            UiKit.Place(name.rectTransform, tx, y, tw, bodyH);
            // T413 — 정본 `ui.js` **5603** 은 이 조각을 이름 뒤 `<small class="tn-lv">` 로 둔다: `<div class="idet-name">${name} <small class="tn-lv">${roman}단계 · Lv.${lv}/${MAX}</small></div>`.
            //   `<small>` 은 인라인이라 **이름과 같은 줄**이고, 정본 머리는 «이름+레벨 / 총합» **두 줄**이다. 클론은 이것을 제 줄 하나로 빼서 **세 줄**이었다(머리 잉크 +17px · 런 848 실측).
            //   틈은 정본 **3695** `.tn-lv { margin-left: .15rem }` — 표 `TechStyle` 이 쥔다. 상자 높이는 이름과 같은 `bodyH` 라 둘이 같은 줄에 가운데로 선다.
            //   ⚠ 같은 줄의 `font-size: .72rem`(26.2px)은 §1 하한 `Sub`(36)보다 작다 — **글자 하한 축(T391·T404)** 이라 여기서 안 건드린다.
            TextMeshProUGUI lvl = DungeonPopups.Bold(card, "lv", TextKind.Sub, Tree.TierLabel(id) + "단계 · Lv." + lv + "/" + Tree.Table.MaxLevel, "pp_muted", TextAlignmentOptions.Left);
            // T333 19회차 — `<small class="tn-lv">` 는 그 `.idet-name` **안**의 인라인 조각이라 같은 겹을 물려받는다(text-shadow 는 상속된다).
            //   클론은 이것을 제 조각으로 떼어 놓았으니(T413) 겹도 따로 걸어야 정본과 같은 그림이 된다.
            UiKit.TextShadow(lvl, "paper_emboss");
            float lvx = tx + name.preferredWidth + PopupKit.Rem * TechStyle.L("tn_lv_margin_left_rem");
            UiKit.Place(lvl.rectTransform, lvx, y, Mathf.Max(0f, tx + tw - lvx), bodyH);
            MainText = "+" + NumFmt.Fmt(Tree.TotalOf(id)) + unit;
            TextMeshProUGUI main = DungeonPopups.Bold(card, "main", TextKind.Sub, MainText, "pp_ink", TextAlignmentOptions.Left);
            UiKit.Place(main.rectTransform, tx, y + bodyH, tw, subH);   // T413 — 레벨이 이름 줄로 붙어 한 줄 올라온다
            // T396 19회차 — 정본 ui.js 5604 `<div class="idet-main">+N% <small class="tn-gain">(…)</small></div>` · style.css 3693 `.tn-gain { color: #1fa64a }`:
            //   괄호 조각은 제 잉크(카탈로그 `tb_val` = 같은 #1fa64a · `_` 칸에 «.tb-val · .tn-gain»)라 한 TMP 로 찍으면 색이 못 갈린다 — 조각을 떼어
            //   주 수치 오른쪽에 잇는다(같은 줄의 레벨 배지가 이름 `preferredWidth` 로 붙는 것과 같은 길 · 172행). 앞 두 칸은 정본의 띄어쓰기.
            //   ⚠ `<small>` 의 크기(한 단 작음)는 글자 종류 축(T391·T461) 몫이라 여기선 잉크만 가른다.
            TextMeshProUGUI gain = DungeonPopups.Bold(card, "gain", TextKind.Sub, "  (" + Tree.GainNote() + " +" + NumFmt.Fmt(def.Per) + unit + " · 이 노드 +" + NumFmt.Fmt(Tree.NodeTotal(id)) + unit + ")", "tb_val", TextAlignmentOptions.Left);
            float gx = tx + main.preferredWidth;
            UiKit.Place(gain.rectTransform, gx, y + bodyH, Mathf.Max(0f, tx + tw - gx), subH);
            y += headH;

            if (!researching)
            {
                y += inner * UiKit.L("idet_subs_mt");   // T477 — 3703 `.idet-subs { margin-top: 7%; padding: 4% }` 의 밑변 = 카드 콘텐츠 폭
                float spad = inner * UiKit.L("idet_subs_pad");
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

        static float ActionHeight(NodeState s, float inner)
        {
            float subH = DungeonPopups.LineH(TextKind.Sub), btnH = DungeonPopups.RemL("tech_btn_h_rem"), gap = DungeonPopups.RemL("card_gap_rem");
            // T430 ⓐ — 정본 4633 `.item-detail.tn-researching > .idet-lead { margin-top: 12.6% }`. 연구 중·수령 대기에서만 걸리고,
            //   정본 `.modal-card` 는 `gap: .45rem` 인 flex 열이라 이 여백은 **그 텀 위에 더해진다**(클론의 `card_gap_rem` 이 그 텀이다 · 두 번 세지 않는다 · 결정 728).
            float leadMt = inner * TechStyle.L("tn_lead_mt_f");   // 밑변은 **카드 안쪽 폭** — 자식의 margin % 는 제 컨테이닝 블록의 콘텐츠 폭이다
            switch (s)
            {
                case NodeState.Max: return subH;
                // T428 1회차 — **그리는 쪽(`RenderAction` 의 `bh`)과 같은 키여야 한다.** T401 3회차가 그린 쪽만 `tech_btn_h_rem`(정본 1750 `.item-detail[data-tech-node] .btn { min-height: 3.6rem }`)으로
                //   올리고 **재는 이 줄은 옛 `btn_sm_h_rem`(2rem) 그대로** 두어, 카드는 1.6rem(= 29.1px · 샷 540×960) 짧게 서고 안내줄이 카드 밖에서 잘렸다(런 943 `screen_tech-node.png`:
                //   안내줄 잉크 오른끝이 여섯 줄 내리 x=451 로 같고 «열립니다» 가 «열립니」 로 끊겼다 · T28 91회차 등재).
                case NodeState.Locked: return btnH + gap + subH * 2f;
                case NodeState.Ready: return leadMt + subH + gap + DungeonPopups.RemL("tech_prog_h_rem") + DungeonPopups.RemL("tech_claim_mt_rem") + btnH;
                case NodeState.Researching: return leadMt + subH + gap + DungeonPopups.RemL("tech_prog_h_rem") + DungeonPopups.RemL("tech_claim_mt_rem") + btnH;
                default: return btnH + gap + subH;
            }
        }

        // T345 8회차 — 정본 4612 `.tech-btns .btn { border-radius: .6rem }` 이 공용 `.btn`(663 .55)을 덮는다: 이 줄의 버튼 넷만 표 `tech_btn_r_rem` 을 쓴다.
        /// <summary>T354 23회차 — 정본 4612 `.tech-btns .btn { line-height: 1.2 }`(ui.js 5586 `.idet-btns.tech-btns` 의 [연구 시작]·[완료]·[건너뛰기 ◆ N]·[잠김]).
        /// 라벨을 만드는 `DungeonPopups.Pill` 은 공용이라(던전은 5356 `.dgd-btn` 1.25 를 따로 쥔다) 부르는 쪽이 자식을 집어 건다(14회차 `DungeonDetailPopup` 과 같은 꼴).</summary>
        static void BtnLh(Button b)
        {
            if (b == null) return;
            Transform l = DungeonPopups.Root(b).Find("label");
            if (l == null) return;
            TextMeshProUGUI t = l.GetComponent<TextMeshProUGUI>();
            if (t != null) LineHeight.Apply(t, "tech_btns_btn_lh");
        }

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
                // T401 1회차 — 정본 **1750** `.item-detail[data-tech-node] .btn { min-height: 3.6rem }` 은 노드 상세의 **모든** 버튼을 덮는다(`.btn.sm` 도).
                //   여기만 `btn_sm_h_rem`(2rem)이라 같은 팝업의 [연구 시작]·[완료]·[건너뛰기](`tech_btn_h_rem` 3.4)보다 **−41%** 였다.
                //   같은 자리의 형제와 한 키를 쓰게 맞춘다. 그 키(`tech_btn_h_rem`)는 3회차에 **3.6**(정본 1750 그대로)으로 올렸다 — 이 키를 읽는 자리가 이 팝업 셋뿐이라 4612 의 3.4 가 아니라 1750 이 맞다.
                float bw = inner * UiKit.L("tech_btn_w"), bh = DungeonPopups.RemL("tech_btn_h_rem");
                ActionButton = DungeonPopups.Pill(card, "locked", "잠김", DungeonPopups.Skin.Gray, TextKind.Button, null, RadiusUi.Px("tech_btn_r_rem"), false);
                BtnLh(ActionButton); UiKit.Place(DungeonPopups.Root(ActionButton), cx - bw * 0.5f, y, bw, bh);
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
                // T430 ⓐ — 4633 의 `margin-top: 12.6%`(카드 안쪽 폭 기준 · 등재문 ⚠ 줄 · 정본 셈은 표 `_tn_lead_mt` 에 적었다).
                y += inner * TechStyle.L("tn_lead_mt_f");
                TextMeshProUGUI lead = DungeonPopups.Bold(card, "lead", TextKind.Sub, ready ? "연구 시간 종료 — 수령 대기" : "연구 진행 중 (취소 불가)", "pp_ink");
                UiKit.Place(lead.rectTransform, pad, y, inner, subH);
                y += subH + gap;
                float ph = DungeonPopups.RemL("tech_prog_h_rem");
                float pr = DungeonPopups.RemL("tech_prog_r_rem");
                // T477 — 정본 4643 `.item-detail[data-tech-node] .tech-prog { margin-left: 1.33%; margin-right: 1.33% }`(카드 콘텐츠 폭 기준 ·
                //   4642 주석 «진행바 폭 원본 323px(65.78%W) · 클론은 카드 안쪽 폭을 꽉 채워 331.8px 였다 — 좌우 4.4px 들여쓴다»). 표 TechUi `idet_prog_mx_f`.
                float pmx = inner * TechStyle.L("idet_prog_mx_f");
                RectTransform prog = UiKit.Box(card, "prog");
                UiKit.Place(prog, pad + pmx, y, inner - pmx * 2f, ph);
                RectTransform track = DungeonPopups.Bordered(prog, "bg", "tech_prog_bg", pr, DungeonPopups.Line3);
                progW = inner - pmx * 2f - DungeonPopups.Line3 * 2f;
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
                    BtnLh(ActionButton); UiKit.Place(DungeonPopups.Root(ActionButton), cx - bw * 0.5f, y, bw, btnH);
                }
                else
                {
                    float bw = inner * UiKit.L("tech_claim_w");
                    ActionButton = DungeonPopups.Pill(card, "skip", "건너뛰기\n◆ " + NumFmt.Fmt(Tree.GemSkipCost(Host.Now())), DungeonPopups.Skin.Silver, TextKind.Button, OnGemSkip, RadiusUi.Px("tech_btn_r_rem"));
                    BtnLh(ActionButton); UiKit.Place(DungeonPopups.Root(ActionButton), cx - bw * 0.5f, y, bw, btnH);
                    // T396 20회차 — 정본 4613 `.tn-skip small { color: #c62828 }`: 아랫줄 «◆ N» 만 그 리터럴(표 tn_skip_gem_ink) · 한 TMP 그대로(줄높이 자 불변).
                    { Transform l = DungeonPopups.Root(ActionButton).Find("label"); if (l != null) LineInk.Apply(l.GetComponent<TextMeshProUGUI>(), 1, "tn_skip_gem_ink"); }
                }
                return;
            }
            // 대기: [연구 시작 · 🧪cost · ⏳time]
            double cost = Tree.NextCost(id) ?? 0;
            double time = Tree.Time(id, lv + 1) ?? 0;
            bool other = Tree.ResearchingId() != null;
            bool disabled = other || Host.Potions < cost;
            ActionButton = DungeonPopups.Pill(card, "start", "연구 시작 · 물약 " + NumFmt.Fmt(cost) + " · " + NumFmt.FmtTime(time), disabled ? DungeonPopups.Skin.Gray : DungeonPopups.Skin.Blue, TextKind.Button, OnStart, RadiusUi.Px("tech_btn_r_rem"), !disabled);
            BtnLh(ActionButton); UiKit.Place(DungeonPopups.Root(ActionButton), pad, y, inner, btnH);
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
            float cw = W * UiKit.L("tbn_card_w") - DungeonPopups.Line3 * 2f;   // T473 — 같은 까닭
            float pad = DungeonPopups.RemL("card_pad_rem");
            float titleH = DungeonPopups.LineH(TextKind.Button);
            float rowH = DungeonPopups.LineH(TextKind.Sub) + DungeonPopups.RemL("tbn_row_pad_rem") * 2f;
            float listH = Mathf.Min(H * UiKit.L("tbn_list_maxh"), Mathf.Max(rowH, lines.Count * rowH));
            float ch = pad * 2f + titleH + DungeonPopups.RemL("card_gap_rem") + listH;
            RectTransform card = DungeonPopups.Card(overlay, "card", cw, ch, DungeonPopups.RemL("card_r_rem"));
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
                WrapUi.Apply(v, "tb_val");   // T361 7회차 — 정본 white-space 표(WrapUi.json) 2253 `.tb-val { nowrap }`
                UiKit.Place(v.rectTransform, lw * 0.6f, 0f, lw * 0.4f, rowH);
                if (i < lines.Count - 1) UiKit.Line(row, "line", "tb_line", DungeonPopups.Line2, false);
            }
            DungeonPopups.XButton(card, Close);
        }
    }

    /// <summary>
    /// T413 — 기술(연구) 팝업의 곁 표(<c>Assets/Forge/Resources/TechUi.json</c>). `catalog.json` 은 T345 산 lock 이라
    /// T65(`PlayerInfoUi.json`)·T339(`ForgeInfoUi.json`)·T388(`DungeonUi.json`)과 같은 꼴로 뗐다. 수치는 코드에 안 박는다(§1).
    /// </summary>
    public static class TechStyle
    {
        public const string ResourcePath = "TechUi";
        static JsonObject root, layout;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new System.InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T413)");
            root = MiniJson.ParseObject(ta.text);
            layout = J.Obj(root["layout"]);
        }

        public static void Reset() { root = null; layout = null; }

        /// <summary>배치 값 원문(꼬리가 곱할 기준을 말한다 — `_rem` = 정본 rem).</summary>
        public static float L(string key)
        {
            Load();
            object v = layout == null ? null : layout[key];
            if (!J.IsNum(v)) throw new System.Collections.Generic.KeyNotFoundException(ResourcePath + ".json 에 «" + key + "» 이 없다");
            return (float)J.Num(v);
        }
    }
}
