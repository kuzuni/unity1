using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Dungeon;
using Forge.Game.Audio;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 던전 클리어 보상 팝업(ROUTINE T21 · 원작 ui.js showDungeonClear/onDungeonClearConfirm).
    /// 흐름: Battle 클리어 → Core Dungeons.OnClear → ShowDungeonClear 이벤트 → 여기 → [보상 수령] → <see cref="Confirmed"/>(전투 씬 T8 이 finishDungeonClear 로 본대 복귀).
    /// 보상은 OnClear 가 이미 지급·저장했다 — 팝업은 보여 주기만 한다.
    /// 던전 «실패» 는 원작이 별도 화면 없이 토스트(💀 … 실패… 본대로 복귀합니다) + 사망 암전(Scene3D.deathFade · T8)이라 여기 화면이 없다.
    /// </summary>
    public static class DungeonClearPopup
    {
        static RectTransform overlay;
        static DungeonRewards rewards;
        static bool busy;

        public static bool IsOpen { get { return overlay != null; } }
        public static string Title { get; private set; }
        public static string Sub { get; private set; }
        public static int CellCount { get; private set; }
        public static Button ConfirmButton { get; private set; }
        /// <summary>[보상 수령] 뒤 — 전투 씬이 본대 복귀(원작 Combat.finishDungeonClear)를 잇는다.</summary>
        public static event Action<DungeonRewards> Confirmed;

        public static void Show(DungeonDef def, int stage, DungeonRewards r)
        {
            Close();
            rewards = r;
            busy = false;
            float W = UiKit.RefW;
            overlay = DungeonPopups.Overlay("modal-dungeon-clear");
            float cw = W * UiKit.L("dgc_card_w");
            float pad = DungeonPopups.RemL("card_pad_rem");
            float gap = DungeonPopups.RemL("dgc_gap_rem");
            float titleH = DungeonPopups.LineH(TextKind.Title);
            float subH = DungeonPopups.LineH(TextKind.Body);
            float ico = DungeonPopups.RemL("dgc_icon_rem");
            float cellPad = DungeonPopups.RemL("dgc_cell_pad_rem");
            float cellH = cellPad * 2f + ico + DungeonPopups.Rem(0.3f) + DungeonPopups.LineH(TextKind.Sub);
            float btnH = DungeonPopups.RemL("dgc_btn_h_rem");
            float ch = pad * 2f + titleH + gap + subH + gap + cellH + gap + btnH;
            RectTransform card = DungeonPopups.Card(overlay, "card", cw, ch, DungeonPopups.RemL("card_radius_rem"));
            // 원작 .dgclear-card 는 테 색이 금색(#ffd54f) — 바깥 테를 그 색으로 덧그린다.
            RectTransform gold = DungeonPopups.Bordered(card, "gold", "pp_paper", DungeonPopups.RemL("card_radius_rem"), DungeonPopups.Line3, "dgclear_title");

            float y = pad;
            Title = "클리어!";
            TextMeshProUGUI t = DungeonPopups.Bold(gold, "title", TextKind.Title, Title, "dgclear_title");
            UiKit.Place(t.rectTransform, 0f, y, cw, titleH);
            y += titleH + gap;

            Sub = def.Kr + " " + stage + "단계";
            float faceD = subH * 1.4f / 1.25f;
            string face; DungeonSheet.FaceIcon.TryGetValue(def.Id, out face);
            float subW = Sub.Length * subH * 0.55f + faceD * 1.3f;
            float sx = (cw - subW) * 0.5f;
            Image fi = UiKit.Icon(gold, "face", face ?? "skull");
            UiKit.Place(fi.rectTransform, sx, y + (subH - faceD) * 0.5f, faceD, faceD);
            TextMeshProUGUI s = DungeonPopups.Bold(gold, "sub", TextKind.Body, Sub, "dgclear_sub", TextAlignmentOptions.Left);
            UiKit.Place(s.rectTransform, sx + faceD * 1.3f, y, subW - faceD * 1.3f, subH);
            y += subH + gap;

            // 보상 칸들
            var cells = new List<KeyValuePair<string, double>>();
            if (r != null)
            {
                if (Math.Floor(r.Hammers) > 0) cells.Add(new KeyValuePair<string, double>("hammer", r.Hammers));
                if (Math.Floor(r.Coins) > 0) cells.Add(new KeyValuePair<string, double>("coin", r.Coins));
                if (Math.Floor(r.Tickets) > 0) cells.Add(new KeyValuePair<string, double>("ticket", r.Tickets));
                if (Math.Floor(r.EggCurrency) > 0) cells.Add(new KeyValuePair<string, double>("eggCracked", r.EggCurrency));
                if (Math.Floor(r.Potions) > 0) cells.Add(new KeyValuePair<string, double>("potion", r.Potions));
            }
            CellCount = cells.Count;
            float cellW = DungeonPopups.RemL("dgc_cell_minw_rem");
            float cgap = DungeonPopups.RemL("dgc_cells_gap_rem");
            float rowW = cells.Count * cellW + Mathf.Max(0, cells.Count - 1) * cgap;
            float cx = (cw - rowW) * 0.5f;
            for (int i = 0; i < cells.Count; i++)
            {
                RectTransform cell = UiKit.Box(gold, "cell-" + cells[i].Key);
                UiKit.Place(cell, cx + i * (cellW + cgap), y, cellW, cellH);
                DungeonPopups.Bordered(cell, "bg", "dgclear_cell", DungeonPopups.RemL("dgc_cell_radius_rem"), DungeonPopups.Line2, "dgclear_cell_border");
                Image img = UiKit.Icon(cell, "ico", cells[i].Key);
                UiKit.Place(img.rectTransform, (cellW - ico) * 0.5f, cellPad, ico, ico);
                TextMeshProUGUI amt = DungeonPopups.Bold(cell, "amt", TextKind.Sub, "+" + NumFmt.Fmt(cells[i].Value), "dgclear_amt");
                UiKit.Place(amt.rectTransform, 0f, cellPad + ico + DungeonPopups.Rem(0.3f), cellW, DungeonPopups.LineH(TextKind.Sub));
            }
            y += cellH + gap;

            ConfirmButton = DungeonPopups.Pill(gold, "confirm", "보상 수령", DungeonPopups.Skin.Blue, TextKind.Button, Confirm);
            UiKit.Place(DungeonPopups.Root(ConfirmButton), pad, y, cw - pad * 2f, btnH);

            // 정본 ui.js 4710 — 카드를 띄운 **뒤** 한 번(«클리어 팬페어» 주석). 열 때마다 한 번이지 보상 수령에는 없다(T120).
            Sfx.LevelUp();
        }

        public static void Confirm()
        {
            if (busy) return;
            busy = true;
            DungeonRewards r = rewards;
            Close();
            if (DungeonUiHost.Instance != null) DungeonUiHost.Instance.RenderTopBar();
            var h = Confirmed;
            if (h != null) h(r);
        }

        public static void Close()
        {
            if (overlay == null) return;
            UnityEngine.Object.Destroy(overlay.gameObject);
            overlay = null;
        }
    }
}
