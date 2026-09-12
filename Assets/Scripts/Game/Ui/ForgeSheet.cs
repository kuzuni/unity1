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
        public static void SetStriking(ForgeHost h, bool on)
        {
            if (anvilRt == null) return;
            anvilRt.localScale = on ? new Vector3(1f, 0.94f, 1f) : Vector3.one;
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
            b.onClick.AddListener(() => UiRoot.Instance.TabBar.OnTab("summon"));
            return rt;
        }

        /// <summary>모루 자리 — 보류 제작품이 있으면 모루 대신 그 카드(더미 두께 = HeldDeckDepth).</summary>
        static RectTransform AnvilSlot(Transform parent, ForgeHost h, float w, float hgt)
        {
            ForgeItem held = h.HeldItem;
            float rem = PopupKit.Rem;
            Button b = UiKit.Button(parent, held == null ? "anvil-btn" : "held-slot", held == null ? (UnityEngine.Events.UnityAction)(() => h.OnCraft()) : () => h.OnOpenHeld());
            RectTransform rt = b.GetComponent<RectTransform>();
            float counterH = PopupKit.FontSize(TextKind.Sub) * 1.3f;
            float bodyH = Mathf.Max(rem * 2f, hgt - counterH - rem * 0.2f);
            if (held == null)
            {
                RectTransform anvil = UiKit.Box(rt, "anvil");
                UiKit.Place(anvil, 0f, 0f, w, bodyH);
                DrawAnvil(anvil, w, bodyH);
            }
            else
            {
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
            }
            RectTransform counter = UiKit.Box(rt, "anvil-hammers");
            UiKit.Place(counter, 0f, bodyH + rem * 0.1f, w, counterH);
            Image hi = PopupKit.IconOr(counter, "ico", "hammer");
            UiKit.Anchor(hi.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-rem * 0.9f, 0f), counterH * 0.9f, counterH * 0.9f);
            hammerText = UiKit.Text(counter, "count", TextKind.Sub, NumFmt.Fmt(h.Wallet.Hammers), "ink", TextAlignmentOptions.Left);
            hammerText.fontStyle = FontStyles.Bold;
            UiKit.Anchor(hammerText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-rem * 0.7f, 0f), w * 0.6f, counterH);
            return rt;
        }

        /// <summary>모루 그림 — 원작 인라인 SVG(viewBox 132×86 · 뿔/상판/허리/받침 4단)를 색면 넷으로. 외부 에셋 없음.</summary>
        static void DrawAnvil(RectTransform rt, float w, float h)
        {
            float u = Mathf.Min(w / 132f, h / 86f);
            float ox = (w - 132f * u) * 0.5f, oy = (h - 86f * u) * 0.5f;
            Color top = new Color(0xb0 / 255f, 0x3f / 255f, 0x18 / 255f), front = new Color(0x5a / 255f, 0x1e / 255f, 0x11 / 255f);
            Color neck = new Color(0x2b / 255f, 0x1c / 255f, 0x18 / 255f), bas = new Color(0x6d / 255f, 0x2c / 255f, 0x1e / 255f);
            Rect(rt, "base", ox + 12f * u, oy + 44f * u, 108f * u, 39f * u, bas, 6f * u);
            Rect(rt, "neck", ox + 36f * u, oy + 36f * u, 60f * u, 14f * u, neck, 2f * u);
            Rect(rt, "front", ox + 18f * u, oy + 22f * u, 80f * u, 16f * u, front, 3f * u);
            Rect(rt, "top", ox + 14f * u, oy + 10f * u, 88f * u, 14f * u, top, 4f * u);
            Rect(rt, "horn", ox + 98f * u, oy + 12f * u, 24f * u, 11f * u, top, 5f * u);
        }

        static void Rect(RectTransform parent, string name, float x, float y, float w, float h, Color c, float r)
        {
            Image i = UiKit.Rounded(parent, name, "pp_paper", r);
            i.color = c;
            UiKit.Place(i.rectTransform, x, y, w, h);
        }
    }
}
