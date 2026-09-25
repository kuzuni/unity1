using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
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
            TextMeshProUGUI sub = PopupKit.Label(content, "sub", TextKind.Sub, "모든 퀘스트는 수령해도 같은 내용으로 반복됩니다", "pp_ink", TextAlignmentOptions.Center, true);
            sub.color = PinnedColorUi.C("sheet_sub_ink");   // T396 14회차 — 정본 3853 `.sheet-sub { color: #4a4a4a }` 한 선택자 한 키(던전 시트와 같은 자리 · 전엔 카탈로그 quest_sub 로 갈려 있었다)
            KeepAll.Apply(sub, "sheet_sub");   // T466 — 정본 3856 `.sheet-sub { word-break: keep-all }`
            LineHeight.Apply(sub, "sheet_sub_lh");   // T354 17회차 — 정본 3854 `.sheet-sub { line-height: 1.4 }`(던전 시트 5회차와 같은 선택자 · T178 반납으로 파일이 열렸다 · 한 줄로 서면 눈엔 안 보이고 꺾이면 산다)
            PopupKit.Spacer(content, rem * 0.45f);

            List<Quest> list = h.Quests.List(h.QuestState);
            int ready = h.Quests.ReadyCount(h.QuestState);
            float rowW = UiKit.L("quest_row_w") * w;
            // T387 — 정본 `.qst-right .btn`(style.css 2058) `min-width: 3.9rem; min-height: 1.9rem`.
            //   표가 그 수를 이미 정확히 쥐고 있다(`quest_btn_w` 0.0739 = 3.9rem/H · `quest_btn_h` 0.036 = 1.9rem/H)는데
            //   여기서 ×1.6·×1.3 을 얹어 6.24rem·2.47rem 으로 그렸다(§1 위반). 곱을 걷는다.
            //   정본 수는 **하한**이지만 글자(«수령» .78rem 두 자)+패딩(.4rem×2)이 3.9rem 을 못 넘어 하한이 곧 실제 크기다.
            float btnW = UiKit.H("quest_btn_w"), btnH = UiKit.H("quest_btn_h");

            string allLabel = "일괄수령" + (ready > 0 ? " (" + ready + ")" : "");
            RectTransform allBar = PopupKit.Item(content, "allbar", -1f, btnH);
            Button all = null;
            all = PopupKit.Btn(allBar, "claim-all", allLabel, "pp_green", "pp_green_dk", () => OnClaimAll(h, all ? all.GetComponent<RectTransform>() : null), btnW * 1.6f, btnH, "stage_ink", TextKind.Sub, ready == 0);
            // T387 — [일괄수령]은 정본에서 `.qst-allbar .btn.sm`(style.css 2020·674)이고 **하한이 아예 없다**:
            //   `flex: 0 0 auto; padding: .3rem .6rem` — 즉 **글자 폭 + 좌우 패딩**이 곧 폭이다.
            //   클론은 그 자리에 [수령] 버튼 폭의 ×1.6 을 써 왔는데, 정본에서 두 버튼은 아무 관계도 없다.
            //   높이는 `btnH`(1.9rem) 를 그대로 둔다 — 정본은 «줄 높이 + .3rem×2» 지만 클론 글자가 하한(버튼 44)이라
            //   그 셈이 1.9rem 언저리로 떨어진다(글자 하한은 주인 지시라 이 작업이 못 건드린다 · T136·T372·T383 축).
            float allPad = QuestStyle.L("allbar_btn_pad_x_rem") * rem;
            float allW = PetSkillKit.TextWidth(TextKind.Button, allLabel) + allPad * 2f;
            UiKit.Anchor(all.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), new Vector2(rowW * 0.5f, 0f), allW, btnH);

            if (list.Count == 0) PopupKit.Label(content, "empty", TextKind.Body, "퀘스트를 불러오지 못했습니다", "pp_muted");

            float icon = UiKit.H("quest_icon");
            // T387 — 정본 `.qst-bar`(2039) `height: .95rem`. catalog.json 의 `quest_bar_h` 는 «.8rem» 이라 정본보다 낮고
            //   그 위에 ×1.6 까지 얹혀 1.28rem 으로 그렸다. 곱을 걷고 **곁 표**(`QuestUi.json` · catalog 는 T364 lock)를 읽는다.
            float barH = QuestStyle.L("bar_h_rem") * rem;
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
                // 정본 2023 `.qst-row { border-radius: .8rem }` — 값은 맞았지만 코드에 박혀 있었다(T345 19회차 · 표 qst_row_r_rem)
                float rowR = RadiusUi.Px("qst_row_r_rem");
                PopupKit.Outlined(row, "face", done ? "quest_done_bg" : "pp_paper", rowR, PopupKit.Line3, done ? "pp_green" : "pp_line");
                UiShadow.Drop(row, "qstrow_lip", rowR);   // 정본 .qst-row(2026) `0 .25rem 0 rgba(0,0,0,.3)` — 종이 카드가 한 겹 떠 있다

                float padX = rem * 0.7f, padY = rem * 0.55f;
                RectTransform icoBox = UiKit.Box(row, "icon");
                UiKit.Place(icoBox, padX, (rowH - icon) * 0.5f, icon, icon);
                PopupKit.IconOr(icoBox, "img", def.Icon);

                float bodyX = padX + icon + rem * 0.6f;
                float rightW = btnW + rem * 0.4f;
                float bodyW = rowW - bodyX - rightW - padX;
                // T333 22회차 — 이름을 정본 클래스 그대로 «qst-name» 으로 부른다(ui.js 4565 `<div class="qst-name">`).
                //   종전 «name» 은 화면마다 흔한 이름이라 자가 앱 뿌리에서 남의 상자를 잡을 수 있었다(T414 갈래).
                TextMeshProUGUI name = UiKit.Text(row, "qst-name", TextKind.Sub, def.Text + " " + PopupKit.Fmt(q.Need) + unit, "pp_ink", TextAlignmentOptions.Left);
                name.fontStyle = FontStyles.Bold;
                // T333 22회차 — 정본 **8383** 묶음(`.qst-row .qst-name`)의 «밝은 종이 위 글자는 흰 엠보스»(`0 1px 0 rgba(255,255,255,.92)`).
                //   14~21회차엔 «T331 산 lock + 클론에 그 이름 자리가 아직 없다» 로 KNOWN 이었는데 **둘 다 낡은 말**이었다 —
                //   그 lock 은 죽었고 이름 자리는 99행에 처음부터 있었다(`qst-name` 이 아니라 `name` 이라 자의 `#` 닻이 못 찾았을 뿐이다).
                UiKit.TextShadow(name, "paper_emboss");
                UiKit.Place(name.rectTransform, bodyX, padY, bodyW, PopupKit.FontSize(TextKind.Sub) * 1.3f);

                RectTransform bar = UiKit.Box(row, "bar");
                UiKit.Place(bar, bodyX, padY + PopupKit.FontSize(TextKind.Sub) * 1.3f + rem * 0.28f, bodyW, barH);
                // T365 10회차 — 정본 2040 `.qst-bar { border: var(--ol2) solid var(--pp-line); overflow: hidden }` — 검정 고리 + 안쪽 면(bg·fill 은 면 안에서 채운다 · 전엔 테가 없었다)
                float barLine = UiKit.L("line2_px");
                UiKit.Rounded(bar, "line", "pp_line", barH * 0.5f);
                RectTransform barFace = UiKit.Box(bar, "face");
                PopupKit.Inset(barFace, barLine);
                UiKit.Rounded(barFace, "bg", "quest_bar_bg", barH * 0.5f - UiKit.L("line2_px"));   // 자가 단을 읽게 키를 그대로
                Image fill = UiKit.Rounded(barFace, "fill", done ? "quest_bar_done" : "quest_bar", barH * 0.5f - barLine);
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = new Vector2((float)pct, 1f);
                fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
                // T178 39회차 — 정본 **8805/8806** `.qst-bar i { background: #4fc3f7 }` · `.qst-row.done .qst-bar i { background: #81e884 }`(aaa-skin ⓖ · 2026-08-19)가
                //   2044/2047 의 두 겹(세로 띠 + 위 1px 광택)을 **단색으로 끈다**(«종전 3정지 그라디언트의 중간색(38%)을 그대로 단색으로 · 신호는 그대로, 광택만 사라진다»).
                //   6·10회차의 `qst-fill-grad`·`qst-fill-rim` 을 걷었다 — 채움은 표 `quest_bar`/`quest_bar_done`(catalog · 바로 그 두 값) 한 칸. 7998 트랙 홈도 같은 블록이 끈다(38회차 겹 걷음).
                // T377 18회차 — 정본 2052 `.qst-bar em { color: #fff }` + 2053 의 어두운 글자 그림자. 이것은 **어두운 트랙**(7991 #262c34)을
                //   전제로 한 짝이다. 클론은 트랙을 #dddddd(밝은 쪽)로 두고 글자를 `pp_ink`(어두운 쪽)로 맞춰 **짝은 맞지만 정본과 반대**였다 —
                //   트랙을 정본값으로 되돌리는 같은 회차에 글자도 흰쪽으로 돌린다(한쪽만 고치면 어두운 글자가 어두운 트랙에 묻는다).
                TextMeshProUGUI progT = UiKit.Text(bar, "prog", TextKind.Sub, PopupKit.Fmt(System.Math.Min(q.Prog, q.Need)) + "/" + PopupKit.Fmt(q.Need), "stage_ink");
                progT.fontStyle = FontStyles.Bold;
                UiKit.TextShadow(progT, "qst_bar_em");   // T333 13회차 — 정본 2053 `.qst-bar em { text-shadow: 0 1px 1px rgba(0,0,0,.75) }`(글자 색 #fff 는 T396 잉크 축)

                RectTransform right = UiKit.Box(row, "right");
                UiKit.Place(right, rowW - padX - btnW, 0f, btnW, rowH);
                RectTransform rw = UiKit.Box(right, "reward");
                UiKit.Place(rw, 0f, padY * 0.6f, btnW, icon * 0.6f);
                Image rwIco = PopupKit.IconOr(rw, "ico", ShopSheet.CurIcon(q.Rw.Cur));
                UiKit.Place(rwIco.rectTransform, 0f, 0f, icon * 0.6f, icon * 0.6f);
                TextMeshProUGUI rwT = UiKit.Text(rw, "amt", TextKind.Sub, PopupKit.Fmt(q.Rw.Amt), "pp_ink", TextAlignmentOptions.Left);
                rwT.fontStyle = FontStyles.Bold;
                TabularText.Apply(rwT);   // T352 ⓒ 6회차 — 정본 8635 `.qst-reward { font-variant-numeric: tabular-nums }`: 행마다 세로 열을 이루는 보상 수 — 숫자 구간만 <mspace>(결정 570 · 칸 폭은 글꼴에서)
                rwT.rectTransform.offsetMin = new Vector2(icon * 0.6f + DungeonPopups.RemL("qst_reward_gap_rem"), 0f);   // T484 — 정본 2056 `.qst-reward { gap: .2rem }`(전엔 icon×.05 = 붙어 있었다)
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
    /// <summary>
    /// T387 — 퀘스트 시트 치수 곁 표(`Resources/QuestUi.json`). 값을 코드에 안 박는다(§1) ·
    /// `catalog.json` 이 남의 lock 일 때가 잦아 곁 표로 둔다(T177 <see cref="ForgeItemStyle"/> · T339 <see cref="ForgeAutoStyle"/> 과 같은 꼴).
    /// </summary>
    public static class QuestStyle
    {
        public const string ResourcePath = "QuestUi";
        static JsonObject root, layout;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new System.InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T387)");
            root = MiniJson.ParseObject(ta.text);
            layout = J.Obj(root["layout"]);
        }

        public static void Reset() { root = null; layout = null; }

        /// <summary>배치 값 원문(접미가 곱할 기준을 말한다 — `_rem` 이면 rem 을 곱한다).</summary>
        public static float L(string key)
        {
            Load();
            object v = layout == null ? null : layout[key];
            if (!J.IsNum(v)) throw new KeyNotFoundException("QuestUi.json 에 배치 값 «" + key + "» 이 없다 (T387)");
            return (float)J.Num(v);
        }
    }
}
