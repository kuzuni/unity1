using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Dungeon;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 던전 상세 팝업(ROUTINE T21 · 원작 ui.js openDungeonDetail/renderDungeonDetail/onDungeonStageStep/onEnterDungeon/onSweepDungeon · 샷 042304).
    /// 카드 71% 폭: 풀폭 배너(제목 오버레이) → 파란 ◀ «난이도 / C-S» ▶ → 회색 보상 알약 → 열쇠 n/2 → 실버 버튼 2([이전 스테이지 소탕] [입장]) → 아래 빨간 ✕.
    /// 단계 표기 = 정수 단계를 챕터-스테이지(10 단위)로 환산(원작 dgStageText).
    /// </summary>
    public static class DungeonDetailPopup
    {
        static RectTransform overlay;
        static string curId;
        static int curStage;

        public static bool IsOpen { get { return overlay != null; } }
        public static string Id { get { return curId; } }
        public static int Stage { get { return curStage; } }
        public static string StageText { get; private set; }
        public static string RewardText { get; private set; }
        public static string KeysText { get; private set; }
        public static Button SweepButton { get; private set; }
        public static Button EnterButton { get; private set; }
        public static Button PrevButton { get; private set; }
        public static Button NextButton { get; private set; }

        static DungeonUiHost Host { get { return DungeonUiHost.Instance; } }

        /// <summary>원작 dgStageText — 정수 단계 n → «챕터-스테이지».</summary>
        public static string DgStageText(int n)
        {
            int s = Math.Max(1, n);
            return ((s - 1) / 10 + 1) + "-" + ((s - 1) % 10 + 1);
        }

        public static void Open(string id)
        {
            if (!DungeonUiHost.Ready) return;
            Dungeons dg = Host.Dungeons;
            if (!dg.Unlocked(id)) { DungeonPopups.Toast("🔒 " + dg.Def(id).Unlock + " 도달 시 해금"); return; }
            curId = id;
            curStage = (int)dg.Best(id) + 1;
            Render();
        }

        public static void Close()
        {
            if (overlay == null) return;
            UnityEngine.Object.Destroy(overlay.gameObject);
            overlay = null;
        }

        /// <summary>열려 있으면 다시 그린다(원작 renderDungeonDetail 을 09:00 리셋이 부르는 갈래).</summary>
        public static void Refresh() { if (overlay != null) Render(); }

        public static void StepStage(int delta)
        {
            int best = (int)Host.Dungeons.Best(curId);
            curStage = Mathf.Clamp(curStage + delta, 1, best + 1);
            Render();
        }

        public static void Sweep()
        {
            if (Host.Dungeons.Sweep(curId))
            {
                Render();
                if (DungeonSheet.Instance != null && DungeonSheet.Instance.IsOpen) DungeonSheet.Instance.Rebuild();
                Host.RenderTopBar();
            }
        }

        public static void Enter()
        {
            if (Host.Dungeons.Enter(curId, curStage))
            {
                Close();
                if (DungeonSheet.Instance != null) { DungeonSheet.Instance.Close(); DungeonSheet.Instance.UpdateStageLabel(); }
                Host.RenderTopBar();
            }
            else Render();
        }

        static void Render()
        {
            Close();
            Dungeons dg = Host.Dungeons;
            DungeonDef d = dg.Def(curId);
            int best = (int)dg.Best(curId);
            double keys = dg.Keys(curId);
            float W = UiKit.RefW, H = UiKit.RefH;

            overlay = DungeonPopups.Overlay("modal-dungeon-detail");
            float cw = W * UiKit.L("dgd_card_w");
            float heroH = H * UiKit.L("dgd_hero_h");
            float radius = DungeonPopups.RemL("card_r_rem");
            float triD = H * UiKit.L("dgd_tri");
            // T426 — 정본 5308 이 이 줄만 `line-height: 1.15` 로 덮는다(클론 바탕 `LineH` 는 1.25) — 바탕을 그대로 쓰면 두 줄이 벌어진다.
            //   위 .3rem(5304 `.dgd-stage-row { margin: .3rem 0 .5rem }`)은 배너 마진 .6rem 과 **안 합쳐진다**(flex column) — 둘 다 더한다. 셋 다 곁 표 DungeonUi.json(결정 731).
            float stageLh = DungeonStyle.L("dgd_stage_lh");
            float stageMt = DungeonPopups.Rem(DungeonStyle.L("dgd_stage_mt_rem")), stageMb = DungeonPopups.Rem(DungeonStyle.L("dgd_stage_mb_rem"));
            float stageLabelH = DungeonPopups.Kind(TextKind.Sub) * stageLh, stageNumH = DungeonPopups.Kind(TextKind.Title2) * stageLh;
            float stageRowH = stageLabelH + stageNumH;   // 난이도 수 = 정본 5310 `.dgd-stage b { 1.15rem }`(T404)
            // T433 — 정본 `.ico` 는 1.45em(863·1763·2597) 이라 **아이콘이 줄 상자를 민다**: 아이콘이 든 줄의 높이를 글자 줄로 잡으면 짧다(정본 2597 이 그 예외를 따로 적어 둔 까닭).
            //   알약 안엔 망치·코인 아이콘(정본 ui.js 4675) · 열쇠 줄은 아이콘 + 숫자(4676) — 둘 다 «글자 줄 상자 vs 아이콘 줄 상자» 중 큰 쪽(IconLineH · 표 DungeonUi.json ico_em).
            float pillH = IconLineH(TextKind.Sub) + DungeonPopups.RemL("dgd_pill_pad_rem") * 2f;
            float keysH = IconLineH(TextKind.Title);
            float btnH = DungeonPopups.RemL("dgd_btn_h_rem");
            float ch = Mathf.Max(H * UiKit.L("dgd_card_minh") - DungeonPopups.Line3 * 2f,   // T465 — 표의 min-height 는 border-box · 카드 rect 는 패딩 상자
                heroH + DungeonPopups.RemL("dgd_hero_mb_rem") + stageMt + stageRowH + stageMb + pillH + DungeonPopups.RemL("dgd_pill_mb_rem")
                + keysH + DungeonPopups.RemL("dgd_keys_mb_rem") + btnH + DungeonPopups.RemL("dgd_btn_mb_rem"));
            RectTransform card = DungeonPopups.Card(overlay, "card", cw, ch, radius);

            // 배너(그림 + 제목 오버레이) — 모서리는 카드 위쪽 둥근 반지름을 따른다.
            RectTransform hero = UiKit.Box(card, "hero");
            UiKit.Place(hero, DungeonPopups.Line3, 0f, cw - DungeonPopups.Line3 * 2f, heroH);   // T465 — 카드 rect 의 위끝이 곧 테 안쪽이다(옛 Line3 보정을 걷었다)
            // T178 19회차 — 정본 2060 `.dg-detail-hero { background: linear-gradient(120deg, var(--bg,#444c56), #161b22) }` = 목록 배너 1952 와 같은 겹(표 dg_banner).
            //   일러스트 **뒤**의 바탕이다(정본 주석 «목록 배너와 같은 dg_* 일러스트를 얹는다» · 그림이 없는 던전은 이것만 보인다). 여태 카드색이 비쳤다.
            SurfaceArt.Fill(hero, "bg-grad", "dg_banner", cw - DungeonPopups.Line3 * 2f, heroH);
            Image scene = UiKit.Icon(hero, "scene", DungeonSheet.SceneIcon(d.Id));
            scene.preserveAspect = false;
            TextMeshProUGUI title = DungeonPopups.Bold(hero, "title", TextKind.Body, d.Kr, "white");
            UiKit.OutlinePx(title, "pp_ink", KeylineUi.Stroke("dgd_title", title.fontSize));   // 정본 .dgd-title 4방향 2px 그림자 ≈ 4px 스트로크(5154 주석)
            float th = DungeonPopups.LineH(TextKind.Body);
            UiKit.Place(title.rectTransform, 0f, DungeonPopups.Rem(0.4f), cw, th);

            float y = heroH + DungeonPopups.RemL("dgd_hero_mb_rem") + stageMt;   // T426 — 배너 마진 + 줄 위 마진 .3rem · T465 — 옛 Line3 보정을 걷었다

            // ◀ 난이도 C-S ▶ — T364 12회차: 정본 5304 는 **flex 행**이다(`display:flex; align-items:center; justify-content:center; gap: calc(var(--app-w) * .1237)`).
            //   클론은 «반 틈(gap × 0.5) + 카드 폭 30% 고정 상자» 라 모델이 달랐다(3회차가 «값만 바꾸면 되레 벌어진다» 로 적어 둔 자리).
            //   ⓐ 틈은 이웃 사이마다 **온전히** 걸린다 ⓑ 가운데 자식 `.dgd-stage` 는 shrink-to-fit 이라 **제 글자 폭**만 차지한다
            //   ⓒ 삼각 단추 상자는 정본 4583 `.tri-btn { width: 1.77rem; height: 2.2rem }` 이고 아이콘(4%H · 5307)은 그 안에 가운데 —
            //      상자보다 넓어도 잉크 중심은 안 움직인다(정본 4586~4590 «위치 계약은 .dgd-stage-row gap 이 쥔다»).
            //   글자 **상자**는 넓게 둔다(가운데 정렬 도우미일 뿐이고 좁히면 한글이 글자 단위로 접힌다) — 자리 계약은 아래 stageW 가 쥔다.
            float gap = W * UiKit.L("dgd_tri_gap");
            float triW = DungeonPopups.RemL("tri_btn_w_rem"), triH = DungeonPopups.RemL("tri_btn_h_rem");
            float labelW = cw * 0.3f;
            float cx = cw * 0.5f;
            StageText = DgStageText(curStage);
            TextMeshProUGUI lab = DungeonPopups.Bold(card, "stage-label", TextKind.Sub, "난이도", "pp_ink");
            UiKit.Place(lab.rectTransform, cx - labelW * 0.5f, y, labelW, stageLabelH);   // T426 — 줄높이 1.15
            TextMeshProUGUI num = DungeonPopups.Bold(card, "stage-num", TextKind.Title2, StageText, "pp_ink");   // T404 ⓑ — 정본 5310 `.dgd-stage b { 1.15rem }` = 41.9px → Title2 42(전엔 Body 40 · −5%)
            UiKit.Place(num.rectTransform, cx - labelW * 0.5f, y + stageLabelH, labelW, stageNumH);
            float stageW = Mathf.Max(lab.preferredWidth, num.preferredWidth);   // 정본 `.dgd-stage` = shrink-to-fit(두 자식 중 넓은 쪽)
            float triY = y + (stageRowH - triH) * 0.5f;                         // 정본 `align-items: center`(줄은 글자 두 줄이 더 높다)
            PrevButton = DungeonPopups.TriButton(card, "prev", true, triD, () => StepStage(-1));
            UiKit.Place(DungeonPopups.Root(PrevButton), cx - stageW * 0.5f - gap - triW, triY, triW, triH);
            NextButton = DungeonPopups.TriButton(card, "next", false, triD, () => StepStage(1));
            UiKit.Place(DungeonPopups.Root(NextButton), cx + stageW * 0.5f + gap, triY, triW, triH);
            DungeonPopups.Root(PrevButton).gameObject.SetActive(curStage > 1);
            DungeonPopups.Root(NextButton).gameObject.SetActive(curStage < best + 1);
            y += stageRowH + stageMb;   // T426 — 정본 5304 아래 마진 .5rem(공용 card_gap .45 가 아니다)

            // 보상 알약
            float pw = cw * UiKit.L("dgd_pill_w");
            RectTransform pill = UiKit.Box(card, "reward-pill");
            UiKit.Place(pill, (cw - pw) * 0.5f, y, pw, pillH);
            UiKit.Rounded(pill, "bg", "dgd_pill", DungeonPopups.RemL("dgd_pill_r_rem"));
            RewardText = BuildRewardRow(pill, dg.Rewards(curId, curStage), pw, pillH);
            y += pillH + DungeonPopups.RemL("dgd_pill_mb_rem");

            // 버튼 둘은 바닥에서 잡는다(정본 .dgd-btns margin-bottom 2.45rem) — 열쇠 줄이 그 위 남는 공간을 나눠 가져야 하므로 먼저 셈한다.
            float by = ch - DungeonPopups.RemL("dgd_btn_mb_rem") - btnH;

            // 열쇠 — 정본 style.css 5338 주석: «.dgd-btns 만 margin-top: auto 라 카드의 남는 세로 공간이 전부 열쇠 줄 아래로 몰렸다 …
            // 열쇠 줄에도 auto 를 줘 남는 공간이 양쪽으로 갈리게 한다». `auto` 가 둘이라 남는 공간을 반반으로 가른다 —
            // 버튼만 바닥에 붙이면 열쇠가 위로 붙는다(T409). `dgd_keys_mb_rem` 은 정본 .dgd-keys margin-bottom .7rem = 최소 아래 여백.
            float keysFree = by - (y + keysH + DungeonPopups.RemL("dgd_keys_mb_rem"));
            float ky = y + Mathf.Max(0f, keysFree) * 0.5f;
            KeysText = NumFmt.Fmt(keys) + "/" + DungeonRules.MaxKeys;
            float kIco = keysH * 0.8f;
            float kw = keysH * 2.6f;
            TextMeshProUGUI kt = DungeonPopups.Bold(card, "keys", TextKind.Title, KeysText, "white", TextAlignmentOptions.Left);
            UiKit.OutlinePx(kt, "pp_line", KeylineUi.Stroke("dgd_keys", kt.fontSize));   // 정본 .dgd-keys 4px
            UiKit.Place(kt.rectTransform, cx - kw * 0.5f + kIco * 1.1f, ky, kw, keysH);
            Image key = UiKit.Icon(card, "key", "key");
            UiKit.Place(key.rectTransform, cx - kw * 0.5f, ky + (keysH - kIco) * 0.5f, kIco, kIco);

            // 버튼 둘
            float mx = cw * UiKit.L("dgd_btn_mx");
            float bgap = cw * UiKit.L("dgd_btn_gap");
            float bw = (cw - mx * 2f - bgap) * 0.5f;
            float br = DungeonPopups.RemL("dgd_btn_r_rem");
            bool canSweep = keys > 0 && best >= 1;
            bool canEnter = keys > 0;
            SweepButton = DungeonPopups.Pill(card, "sweep", "이전 스테이지\n소탕", canSweep ? DungeonPopups.Skin.DgdSilver : DungeonPopups.Skin.Gray, TextKind.Button, Sweep, br, canSweep);
            UiKit.Place(DungeonPopups.Root(SweepButton), mx, by, bw, btnH);
            EnterButton = DungeonPopups.Pill(card, "enter", "입장", canEnter ? DungeonPopups.Skin.DgdSilver : DungeonPopups.Skin.Gray, TextKind.Button, Enter, br, canEnter);
            UiKit.Place(DungeonPopups.Root(EnterButton), mx + bw + bgap, by, bw, btnH);
            // T354 14회차 — 정본 5356 `.dgd-btn { line-height: 1.25 }`. 왼쪽 버튼 라벨은 «이전 스테이지 / 소탕» **두 줄**이라
            // 줄 간격이 눈에 보이는 자리다(오른쪽 «입장» 은 한 줄이라 안 보이지만, 정본은 클래스에 걸었으므로 둘 다 건다).
            // 라벨을 만드는 `DungeonPopups.Pill` 은 남의 산 lock(T345)이라 **여기서 그 자식을 집어** 건다 — 그 파일은 안 건드린다.
            ApplyBtnLineHeight(SweepButton);
            ApplyBtnLineHeight(EnterButton);

            DungeonPopups.XButton(card, Close);
        }

        /// <summary>T433 — 인라인 아이콘이 든 줄의 상자 높이. 정본 `.ico` 기본 1.45em(표 `ico_em`)이 기준선 위에 서서 줄 상자를 밀므로
        /// 줄 상자 = max(글자 줄 상자 `LineH(k)`, 글자 x ico_em + 기준선 아래로 삐져나오는 몫). 아래 몫은 글꼴 자산의 descender(pointSize 비율)로 낸다 — 수를 코드에 안 박는다.</summary>
        public static float IconLineH(TextKind k)
        {
            float fs = DungeonPopups.Kind(k);
            var f = UiFont.Primary.faceInfo;
            float below = Mathf.Abs(f.descentLine) / f.pointSize * fs;
            return Mathf.Max(DungeonPopups.LineH(k), fs * DungeonStyle.L("ico_em") + below);
        }

        /// <summary>알약 버튼의 라벨(`DungeonPopups.Pill` 이 «label» 로 세운다)에 정본 줄 간격을 건다 — 없으면 조용히 지나간다.</summary>
        static void ApplyBtnLineHeight(Button b)
        {
            if (b == null) return;
            Transform t = DungeonPopups.Root(b).Find("label");
            TextMeshProUGUI tm = t != null ? t.GetComponent<TextMeshProUGUI>() : null;
            if (tm != null) LineHeight.Apply(tm, "dgd_btn_lh");
        }

        /// <summary>«보상: 🔨302 🪙27.1k» — 원작 Dungeons.rewardText 의 이모지를 아이콘으로(iconizeHTML). 글자 판(그림 없이)도 돌려준다.</summary>
        static string BuildRewardRow(RectTransform pill, DungeonRewards r, float pw, float ph)
        {
            float lh = DungeonPopups.LineH(TextKind.Sub);
            float ico = lh * 1.29f / 1.25f;
            float padX = DungeonPopups.Rem(0.9f);
            float y = (ph - lh) * 0.5f;
            string[] keys = { "hammers", "coins", "tickets", "eggCurrency", "potions" };
            double[] vals = { r.Hammers, r.Coins, r.Tickets, r.EggCurrency, r.Potions };
            string[] icons = { "hammer", "coin", "ticket", "eggCracked", "potion" };
            var sb = new System.Text.StringBuilder();
            float total = lh * 2.2f;
            var parts = new System.Collections.Generic.List<int>();
            for (int i = 0; i < vals.Length; i++) if (Math.Floor(vals[i]) > 0) { parts.Add(i); total += ico + NumFmt.Fmt(vals[i]).Length * lh * 0.5f + lh * 0.3f; }
            float x = Mathf.Max(padX, (pw - total) * 0.5f);
            TextMeshProUGUI label = DungeonPopups.Bold(pill, "label", TextKind.Sub, "보상:", "white", TextAlignmentOptions.Left);
            UiKit.OutlinePx(label, "pp_line", KeylineUi.Stroke("dgd_reward", label.fontSize));   // 정본 .dgd-reward-pill 2px(상속)
            UiKit.Place(label.rectTransform, x, y, lh * 2.2f, lh);
            x += lh * 2.2f;
            for (int k = 0; k < parts.Count; k++)
            {
                int i = parts[k];
                Image img = UiKit.Icon(pill, "ico-" + keys[i], icons[i]);
                UiKit.Place(img.rectTransform, x, y + (lh - ico) * 0.5f, ico, ico);
                DropShadow.Apply(img, "dgd_reward_pill_ico");   // T332 — 정본 5335 `drop-shadow(0 0 1.2px rgba(0,0,0,.9))`: 오프셋 0 이라 «검정 윤곽» 이다(회색 알약 위에서 흰 아이콘이 묻히지 않게) · 자리를 잡은 뒤에 부른다
                x += ico;
                string v = NumFmt.Fmt(vals[i]);
                float vw = v.Length * lh * 0.5f + lh * 0.3f;
                TextMeshProUGUI t = DungeonPopups.Bold(pill, "val-" + keys[i], TextKind.Sub, v, "white", TextAlignmentOptions.Left);
                UiKit.OutlinePx(t, "pp_line", KeylineUi.Stroke("dgd_reward", t.fontSize));
                UiKit.Place(t.rectTransform, x, y, vw, lh);
                x += vw;
                if (sb.Length > 0) sb.Append("  ");
                sb.Append(keys[i]).Append(' ').Append(v);
            }
            return sb.ToString();
        }
    }
}
