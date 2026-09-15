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
            float stageRowH = DungeonPopups.LineH(TextKind.Sub) + DungeonPopups.LineH(TextKind.Body);
            float pillH = DungeonPopups.LineH(TextKind.Sub) + DungeonPopups.RemL("dgd_pill_pad_rem") * 2f;
            float keysH = DungeonPopups.LineH(TextKind.Title);
            float btnH = DungeonPopups.RemL("dgd_btn_h_rem");
            float ch = Mathf.Max(H * UiKit.L("dgd_card_minh"),
                heroH + DungeonPopups.RemL("dgd_hero_mb_rem") + stageRowH + DungeonPopups.RemL("card_gap_rem") + pillH + DungeonPopups.RemL("dgd_pill_mb_rem")
                + keysH + DungeonPopups.RemL("dgd_keys_mb_rem") + btnH + DungeonPopups.RemL("dgd_btn_mb_rem"));
            RectTransform card = DungeonPopups.Card(overlay, "card", cw, ch, radius);

            // 배너(그림 + 제목 오버레이) — 모서리는 카드 위쪽 둥근 반지름을 따른다.
            RectTransform hero = UiKit.Box(card, "hero");
            UiKit.Place(hero, DungeonPopups.Line3, DungeonPopups.Line3, cw - DungeonPopups.Line3 * 2f, heroH);
            Image scene = UiKit.Icon(hero, "scene", DungeonSheet.SceneIcon(d.Id));
            scene.preserveAspect = false;
            TextMeshProUGUI title = DungeonPopups.Bold(hero, "title", TextKind.Body, d.Kr, "white");
            UiKit.OutlinePx(title, "pp_ink", KeylineUi.Stroke("dgd_title", title.fontSize));   // 정본 .dgd-title 4방향 2px 그림자 ≈ 4px 스트로크(5154 주석)
            float th = DungeonPopups.LineH(TextKind.Body);
            UiKit.Place(title.rectTransform, 0f, DungeonPopups.Rem(0.4f), cw, th);

            float y = DungeonPopups.Line3 + heroH + DungeonPopups.RemL("dgd_hero_mb_rem");

            // ◀ 난이도 C-S ▶
            float gap = W * UiKit.L("dgd_tri_gap");
            float labelW = cw * 0.3f;
            float cx = cw * 0.5f;
            StageText = DgStageText(curStage);
            TextMeshProUGUI lab = DungeonPopups.Bold(card, "stage-label", TextKind.Sub, "난이도", "pp_ink");
            UiKit.Place(lab.rectTransform, cx - labelW * 0.5f, y, labelW, DungeonPopups.LineH(TextKind.Sub));
            TextMeshProUGUI num = DungeonPopups.Bold(card, "stage-num", TextKind.Body, StageText, "pp_ink");
            UiKit.Place(num.rectTransform, cx - labelW * 0.5f, y + DungeonPopups.LineH(TextKind.Sub), labelW, DungeonPopups.LineH(TextKind.Body));
            PrevButton = DungeonPopups.TriButton(card, "prev", true, triD, () => StepStage(-1));
            UiKit.Place(DungeonPopups.Root(PrevButton), cx - labelW * 0.5f - gap * 0.5f - triD, y + (stageRowH - triD) * 0.5f, triD, triD);
            NextButton = DungeonPopups.TriButton(card, "next", false, triD, () => StepStage(1));
            UiKit.Place(DungeonPopups.Root(NextButton), cx + labelW * 0.5f + gap * 0.5f, y + (stageRowH - triD) * 0.5f, triD, triD);
            DungeonPopups.Root(PrevButton).gameObject.SetActive(curStage > 1);
            DungeonPopups.Root(NextButton).gameObject.SetActive(curStage < best + 1);
            y += stageRowH + DungeonPopups.RemL("card_gap_rem");

            // 보상 알약
            float pw = cw * UiKit.L("dgd_pill_w");
            RectTransform pill = UiKit.Box(card, "reward-pill");
            UiKit.Place(pill, (cw - pw) * 0.5f, y, pw, pillH);
            UiKit.Rounded(pill, "bg", "dgd_pill", DungeonPopups.RemL("dgd_pill_r_rem"));
            RewardText = BuildRewardRow(pill, dg.Rewards(curId, curStage), pw, pillH);
            y += pillH + DungeonPopups.RemL("dgd_pill_mb_rem");

            // 열쇠
            KeysText = NumFmt.Fmt(keys) + "/" + DungeonRules.MaxKeys;
            float kIco = keysH * 0.8f;
            float kw = keysH * 2.6f;
            TextMeshProUGUI kt = DungeonPopups.Bold(card, "keys", TextKind.Title, KeysText, "white", TextAlignmentOptions.Left);
            UiKit.OutlinePx(kt, "pp_line", KeylineUi.Stroke("dgd_keys", kt.fontSize));   // 정본 .dgd-keys 4px
            UiKit.Place(kt.rectTransform, cx - kw * 0.5f + kIco * 1.1f, y, kw, keysH);
            Image key = UiKit.Icon(card, "key", "key");
            UiKit.Place(key.rectTransform, cx - kw * 0.5f, y + (keysH - kIco) * 0.5f, kIco, kIco);
            y += keysH + DungeonPopups.RemL("dgd_keys_mb_rem");

            // 버튼 둘
            float mx = cw * UiKit.L("dgd_btn_mx");
            float bgap = cw * UiKit.L("dgd_btn_gap");
            float bw = (cw - mx * 2f - bgap) * 0.5f;
            float by = ch - DungeonPopups.RemL("dgd_btn_mb_rem") - btnH;
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
