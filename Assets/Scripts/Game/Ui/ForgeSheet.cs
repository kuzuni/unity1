using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.Forging;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 장비 시트(ROUTINE T19 · 원작 ui.js `renderEquipSheet`·`equipCellHTML`·`anvilSlotHTML`·`updateAnvilCounter` · shot-042120):
    /// 5열 격자(장비 8칸 + 탈것 칸 2열) · 모루 행(좌 «i» · 중 모루 버튼 또는 보류 카드 · 우 [대장간 레벨 N][자동] + 남은 시간).
    /// 치수는 원작 style.css 실측(격자 좌우 10.94%W · 열 틈 2.94%W · 행 틈 .6rem · 모루 7rem · 버튼 높이 ≈5%H).
    /// </summary>
    public static class ForgeSheet
    {
        static TextMeshProUGUI hammerText, upgText;
        static RectTransform anvilRt;

        public static void Render(ForgeHost h)
        {
            UiRoot root = UiRoot.Instance;
            if (root == null || h == null || h.Engine == null) return;
            RectTransform sheet = root.Sheet;
            for (int i = sheet.childCount - 1; i >= 0; i--)
            {
                Transform c = sheet.GetChild(i);
                if (c.name == "bg" || c.name == "line") continue;
                Object.Destroy(c.gameObject);
            }
            hammerText = null; upgText = null; anvilRt = null;
            GameDefs d = h.Defs;
            float W = UiKit.RefW, rem = PopupKit.Rem;
            float sheetH = sheet.rect.height > 0 ? sheet.rect.height : (UiKit.L("chat_top") - UiKit.L("sheet_top")) * UiKit.RefH;
            float padX = rem * 0.6f, padTop = rem * 0.55f;

            // ---- 장비 격자 ----
            float gridX = W * 0.1094f, gridW = W - gridX * 2f, colGap = W * 0.0294f, rowGap = rem * 0.6f;
            float cell = (gridW - colGap * 4f) / 5f;
            RectTransform grid = UiKit.Box(sheet, "equip-grid");
            UiKit.Place(grid, gridX, padTop, gridW, cell * 2f + rowGap);
            string[] slots = d.Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                int col = i % 5, row = i / 5;
                RectTransform c = EquipCell(grid, h, slots[i], cell);
                UiKit.Place(c, col * (cell + colGap), row * (cell + rowGap), cell, cell);
            }
            RectTransform mount = MountCell(grid, h, cell * 2f + colGap, cell);
            UiKit.Place(mount, 3 * (cell + colGap), cell + rowGap, cell * 2f + colGap, cell);

            // ---- 모루 행 ----
            float rowY = padTop + cell * 2f + rowGap + rem * 0.5f;
            float rowH = sheetH - rowY - rem * 0.3f;
            RectTransform anvilRow = UiKit.Box(sheet, "anvil-row");
            UiKit.Place(anvilRow, padX, rowY, W - padX * 2f, rowH);
            float anvilW = rem * 7f;
            float sideW = (W - padX * 2f - anvilW - rem * 0.8f) * 0.5f;

            // 좌: i(플레이어 정보)
            float info = W * 0.0281f * 1.6f;
            Button ib = ForgeUi.InfoButton(anvilRow, "info-btn", info, () => h.Meta.OpenPlayerInfo());
            UiKit.Place(ib.GetComponent<RectTransform>(), W * 0.109f, UiKit.RefH * 0.0066f, info, info);

            // 중: 모루 / 보류 카드
            anvilRt = AnvilSlot(anvilRow, h, anvilW, rowH);
            UiKit.Place(anvilRt, sideW + rem * 0.4f, 0f, anvilW, rowH);

            // 우: [대장간 레벨 N][자동] · 남은 시간
            RectTransform right = UiKit.Box(anvilRow, "anvil-side-right");
            float rx = sideW + rem * 0.8f + anvilW;
            UiKit.Place(right, rx, UiKit.RefH * 0.0352f, W - padX * 2f - rx, rowH);
            float btnH = UiKit.RefH * 0.05f * 1.35f, gap = rem * 0.35f;
            float btnW = (W - padX * 2f - rx - gap) * 0.5f;
            ForgeUpgrade up = h.UpgradeInfo();
            string forgeLabel = up == null ? (h.AscendReady ? "★ 승천\n가능" : "대장간\n최고 레벨") : "대장간\n레벨 " + h.Forge.ForgeLevel;
            Button fb = TwoLineBtn(right, "forge-btn", forgeLabel, "pp_blue", "pp_blue_dk", () => ForgeInfoPopup.Open(h), btnW * 1.15f, btnH);
            UiKit.Place(fb.GetComponent<RectTransform>(), 0f, 0f, btnW * 1.15f, btnH);
            bool unlocked = h.AutoForgeUnlocked;
            string autoLabel = "자동 ↻\n" + (unlocked ? (h.AutoOn ? "ON" : "OFF") : "🔒");
            Button ab = TwoLineBtn(right, "auto-btn", autoLabel, unlocked ? (h.AutoOn ? "pp_green" : "pp_blue") : "pp_gray", unlocked ? (h.AutoOn ? "pp_green_dk" : "pp_blue_dk") : "pp_gray_dk", () => h.OnAutoForgeBtn(), btnW * 0.85f - gap, btnH);
            UiKit.Place(ab.GetComponent<RectTransform>(), btnW * 1.15f + gap, 0f, btnW * 0.85f - gap, btnH);
            if (h.Upgrading)
            {
                upgText = UiKit.Text(right, "equip-upg-time", TextKind.Sub, RemainText(h), "ink");
                upgText.fontStyle = FontStyles.Bold;
                UiKit.Place(upgText.rectTransform, 0f, btnH + rem * 0.25f, W - padX * 2f - rx, PopupKit.FontSize(TextKind.Sub) * 1.3f);
            }
        }

        static string RemainText(ForgeHost h)
        {
            return h.Forge.UpgradeEndsAt.HasValue ? NumFmt.FmtTime((h.Forge.UpgradeEndsAt.Value - h.Meta.NowMs) / 1000) : string.Empty;
        }

        /// <summary>매초 — 해머 카운터·남은 시간만 갱신(전체 재렌더는 과함).</summary>
        public static void Tick(ForgeHost h)
        {
            if (hammerText != null) hammerText.text = NumFmt.Fmt(h.Wallet.Hammers);
            if (upgText != null) upgText.text = RemainText(h);
        }

        /// <summary>모루 타격 연출 중 표시(원작 .striking — 그림은 T8/T30 자리 · 여기서는 모루를 살짝 눌러 둔다).</summary>
        /// <summary>
        /// 두들기기 시작·끝 — 정본은 `.anvil-btn.striking`(모루 `anvilbump`)과 `#equip-sheet.shaking`(`sheetshake`)을
        /// 같은 마스터 클럭(`--afdur` = `ui.js` `ANVIL_FX_MS` 1500ms)으로 돌린다. T87: 그 키프레임 표를 러너가 프레임마다 바른다.
        /// </summary>
        public static void SetStriking(ForgeHost h, bool on)
        {
            UiRoot root = UiRoot.Instance;
            if (root == null) return;
            AnvilFx fx = AnvilFx.Ensure(root.Sheet);
            if (fx == null) return;
            if (on) fx.Play(anvilRt, root.Sheet);
            else fx.Stop();
        }

        static Button TwoLineBtn(Transform parent, string name, string label, string face, string lip, UnityEngine.Events.UnityAction onClick, float w, float h)
        {
            Button b = PopupKit.Btn(parent, name, label, face, lip, onClick, w, h, "stage_ink", TextKind.Sub);
            TextMeshProUGUI t = b.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) { t.textWrappingMode = TextWrappingModes.Normal; t.lineSpacing = -20f; }
            return b;
        }

        static RectTransform EquipCell(Transform parent, ForgeHost h, string slot, float size)
        {
            GameDefs d = h.Defs;
            ForgeItem it = h.Gear.Get(slot);
            RectTransform rt = UiKit.Box(parent, "cell-" + slot);
            if (it == null)
            {
                Color face = ForgeUi.CellFace(new Color(0x6b / 255f, 0x35 / 255f, 0x38 / 255f));
                ForgeUi.Tile(rt, "frame", face, ForgeUi.CellLine(new Color(0x6b / 255f, 0x35 / 255f, 0x38 / 255f)), size * 0.16f, PopupKit.Line3);
                Image ico = PopupKit.IconOr(rt, "img", ForgeUi.SlotIconKey(slot));
                ico.color = new Color(1f, 1f, 1f, 0.52f);
                float k = size * 0.72f;
                UiKit.Anchor(ico.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, size * 0.06f), k, k);
                TextMeshProUGUI nm = UiKit.Text(rt, "slot-name", TextKind.Sub, d.SlotKr.Get(slot, slot), "pp_muted");
                nm.fontStyle = FontStyles.Bold;
                UiKit.Anchor(nm.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, size * 0.05f), size, nm.fontSize * 1.2f);
                return rt;
            }
            Color ac = ForgeUi.AgeColor(d, it.Age);
            Image f = ForgeUi.Tile(rt, "frame", ForgeUi.CellFace(ac), ForgeUi.CellLine(ac), size * 0.16f, PopupKit.Line3);
            Image img = PopupKit.IconOr(rt, "img", ForgeUi.ItemIconKey(d, it));
            float kk = size * 0.76f;
            UiKit.Anchor(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, size * 0.06f), kk, kk);
            ForgeUi.LvBadge(rt, it.Level, size);
            ForgeUi.StarBadge(rt, it.Stars, size);
            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = f;
            string s = slot;
            b.onClick.AddListener(() => GearDetailPopup.Open(h, s));
            return rt;
        }

        /// <summary>마지막 칸 = 탈것 슬롯(T11 이 채운다 · 지금은 빈 «탈것» 칸 · 누르면 탈것 창 요청 = 소환 시트).</summary>
        static RectTransform MountCell(Transform parent, ForgeHost h, float w, float hgt)
        {
            RectTransform rt = UiKit.Box(parent, "egg-cell");
            Image f = ForgeUi.Tile(rt, "frame", new Color(0x4f / 255f, 0xb2 / 255f, 0xee / 255f), Color.black, hgt * 0.16f, PopupKit.Line3);
            Image ico = PopupKit.IconOr(rt, "mount-sil", "horse");
            ico.color = new Color(0f, 0f, 0f, 0.32f);
            float k = hgt * 0.55f;
            UiKit.Anchor(ico.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, hgt * 0.08f), k, k);
            TextMeshProUGUI nm = UiKit.Text(rt, "slot-name", TextKind.Sub, "탈것", "stage_ink");
            nm.fontStyle = FontStyles.Bold;
            PopupKit.Ring(nm, "pp_line", 0.2f);
            UiKit.Anchor(nm.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, hgt * 0.06f), w, nm.fontSize * 1.2f);
            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = f;
            b.onClick.AddListener(() => MountSheet.Open());   // 원작 `UI.openMounts()` — T20 탈것 시트(전체 모달)
            return rt;
        }

        /// <summary>모루 자리 — 보류 제작품이 있으면 모루 대신 그 카드(더미 두께 = HeldDeckDepth). 망치 수(원작 `small#anvil-hammers`)는 모루일 때 **받침 위**(T61 · shot-042120 실측 61%)에, 카드일 때 카드 아래에.</summary>
        static RectTransform AnvilSlot(Transform parent, ForgeHost h, float w, float hgt)
        {
            ForgeItem held = h.HeldItem;
            float rem = PopupKit.Rem;
            Button b = UiKit.Button(parent, held == null ? "anvil-btn" : "held-slot", held == null ? (UnityEngine.Events.UnityAction)(() => h.OnCraft()) : () => h.OnOpenHeld());
            RectTransform rt = b.GetComponent<RectTransform>();
            float counterH = PopupKit.FontSize(TextKind.Sub) * 1.3f;
            RectTransform counter;
            if (held == null)
            {
                RectTransform anvil = UiKit.Box(rt, "anvil");
                UiKit.Place(anvil, 0f, 0f, w, hgt);
                UnityEngine.Rect baseRect = DrawAnvil(anvil, w, hgt);
                // 원작(shot-042120): «🔨 41307» 이 받침의 어두운 몸통 위에 얹혀 흰 글자가 읽힌다 — 받침 세로 61% 자리
                counter = UiKit.Box(anvil, "anvil-hammers");
                UiKit.Place(counter, baseRect.x, baseRect.y + baseRect.height * UiKit.L("anvil_count_y") - counterH * 0.5f, baseRect.width, counterH);
            }
            else
            {
                float bodyH = Mathf.Max(rem * 2f, hgt - counterH - rem * 0.2f);
                int n = h.HeldCount;
                int depth = ForgeHost.HeldDeckDepth(n);
                Color ac = ForgeUi.AgeColor(h.Defs, held.Age);
                float dg = rem * 0.21f;
                for (int i = depth; i >= 1; i--)
                {
                    Image edge = UiKit.Rounded(rt, "deck-" + i, "pp_paper", rem * 0.7f);
                    edge.color = ForgeUi.Mix(ac, new Color(0.87f, 0.89f, 0.93f), 0.45f);
                    UiKit.Place(edge.rectTransform, dg * i, 0f, w - dg * depth, bodyH);
                }
                RectTransform card = UiKit.Box(rt, "card");
                UiKit.Place(card, 0f, 0f, w - dg * depth, bodyH);
                ForgeUi.Tile(card, "frame", ForgeUi.Mix(ac, new Color(0x17 / 255f, 0x18 / 255f, 0x1a / 255f), 0.7f), ForgeUi.CellLine(ac), rem * 0.7f, PopupKit.Line3);
                float ico = rem * 2.1f;
                Image img = PopupKit.IconOr(card, "held-img", ForgeUi.ItemIconKey(h.Defs, held));
                UiKit.Anchor(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, rem * 0.35f), ico, ico);
                TextMeshProUGUI nm = UiKit.Text(card, "held-name", TextKind.Sub, held.Name, "stage_ink");
                nm.fontStyle = FontStyles.Bold;
                UiKit.Anchor(nm.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, rem * 0.25f), w, nm.fontSize * 1.2f);
                TextMeshProUGUI tag = UiKit.Text(rt, "held-tag", TextKind.Sub, "보류" + (n > 1 ? " " + n : string.Empty), "pp_ink");
                tag.fontStyle = FontStyles.Bold;
                float tw = tag.preferredWidth + rem * 0.7f, th = tag.fontSize * 1.25f;
                RectTransform tagBox = UiKit.Box(rt, "held-tag-bg");
                UiKit.Anchor(tagBox, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, tw, th);
                Image tb = UiKit.Rounded(tagBox, "bg", "coin", th * 0.5f);
                UiKit.Rounded(tagBox, "ring", "pp_line", th * 0.5f).transform.SetAsFirstSibling();
                PopupKit.Inset(tb.rectTransform, PopupKit.Line);
                tag.transform.SetParent(tagBox, false);
                UiKit.Fill(tag.rectTransform);
                counter = UiKit.Box(rt, "anvil-hammers");
                UiKit.Place(counter, 0f, bodyH + rem * 0.1f, w, counterH);
            }
            // 원작 style.css 1625 `.anvil-btn small`: 흰 굵은 글자 + 검정 text-stroke 2px — 어느 바닥 위에서도 읽힌다
            Image hi = PopupKit.IconOr(counter, "ico", "hammer");
            UiKit.Anchor(hi.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-rem * 0.9f, 0f), counterH * 0.9f, counterH * 0.9f);
            hammerText = UiKit.Text(counter, "count", TextKind.Sub, NumFmt.Fmt(h.Wallet.Hammers), "white", TextAlignmentOptions.Left);
            hammerText.fontStyle = FontStyles.Bold;
            UiKit.Outline(hammerText, "pp_line", UiKit.L("anvil_count_stroke"));
            UiKit.Anchor(hammerText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-rem * 0.7f, 0f), w * 0.6f, counterH);
            return rt;
        }

        /// <summary>
        /// 모루 그림 — 원작 인라인 SVG(viewBox 132×86 · `ANVIL_SVG`)를 색면으로: 받침(어두운 주철 · 음각 단) · 목 · 상판 앞면 · 상판 윗면(+베벨 엣지) · 뿔(둥근 총알) · 검은 외곽선(stroke 3).
        /// 좌표·색은 전부 catalog.json `anvil_*`(§1). 외부 에셋 없음. 반환: 받침 사각(모루 로컬 px) — 망치 수를 그 위에 얹는다(T61).
        /// </summary>
        static UnityEngine.Rect DrawAnvil(RectTransform rt, float w, float h)
        {
            float vbW = UiKit.L("anvil_vb_w"), vbH = UiKit.L("anvil_vb_h");
            float u = Mathf.Min(w / vbW, h / vbH);
            float ox = (w - vbW * u) * 0.5f, oy = (h - vbH * u) * 0.5f;
            float st = UiKit.L("anvil_stroke") * u;
            // 그리는 순서 = SVG 순서(뒤 → 앞): 받침 → 음각 단 → 목 → 뿔 → 상판 앞면 → 상판 윗면 → 베벨
            UnityEngine.Rect bas = Outlined(rt, "base", ox, oy, u, "anvil_base", "anvil_base", UiKit.L("anvil_base_r") * u, st);
            Outlined(rt, "recess", ox, oy, u, "anvil_recess", "anvil_recess", 3f * u, st * 0.8f);
            Outlined(rt, "neck", ox, oy, u, "anvil_neck", "anvil_neck", 1.5f * u, st);
            Outlined(rt, "horn", ox, oy, u, "anvil_horn", "anvil_horn", UiKit.L("anvil_horn_r") * u, st);
            Outlined(rt, "front", ox, oy, u, "anvil_front", "anvil_front", 2f * u, st);
            Outlined(rt, "top", ox, oy, u, "anvil_top", "anvil_top", 3f * u, st);
            Image bevel = UiKit.Rounded(rt, "bevel", "anvil_bevel", 1.5f * u);
            Color bc = bevel.color; bc.a = UiKit.L("anvil_bevel_alpha"); bevel.color = bc;
            UiKit.Place(bevel.rectTransform, ox + UiKit.L("anvil_bevel_x") * u, oy + UiKit.L("anvil_bevel_y") * u, UiKit.L("anvil_bevel_w") * u, UiKit.L("anvil_bevel_h") * u);
            return bas;
        }

        /// <summary>검은 외곽선(살짝 큰 `anvil_line` 면) 위에 색면 하나 — SVG `stroke` 의 자리. 기하는 `anvil_&lt;part&gt;_x/y/w/h`.</summary>
        static UnityEngine.Rect Outlined(RectTransform parent, string name, float ox, float oy, float u, string part, string colorKey, float r, float stroke)
        {
            float x = ox + UiKit.L(part + "_x") * u, y = oy + UiKit.L(part + "_y") * u, w = UiKit.L(part + "_w") * u, h = UiKit.L(part + "_h") * u;
            Image line = UiKit.Rounded(parent, name + "-line", "anvil_line", r + stroke * 0.5f);
            UiKit.Place(line.rectTransform, x - stroke * 0.5f, y - stroke * 0.5f, w + stroke, h + stroke);
            Image fill = UiKit.Rounded(parent, name, colorKey, r);
            UiKit.Place(fill.rectTransform, x, y, w, h);
            return new UnityEngine.Rect(x, y, w, h);
        }

    }
}
