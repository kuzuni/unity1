using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.Pets;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 펫 업그레이드 팝업(원작 ui.js openPetUpgrade · renderPetUpgrade · onToggleUpgradeMat · onBulkSelectMat · onConfirmPetUpgrade · UI-SPEC 55 · shot-042503):
    /// 대상 카드(장착됨+Lv+⭐+피해/체력) → 경험치 바 → «합칠 펫 선택» + [업그레이드] → 등급별 일괄 선택 칩 → 구분선 → 보유 펫·알 타일 그리드(✓ 선택 · 무제한 · 장착 중 펫은 잠금).
    /// 대상을 못 잡으면 열지 않는다(원작 upgrade-modal-empty-guard). 규칙은 Core <see cref="PetSystem.AbsorbMaterials"/>.
    /// </summary>
    public static class PetUpgradePopup
    {
        public const string ModalName = "pet-upgrade";
        static SkillPetSheet sheet;
        static int target = -1;
        static readonly List<int> matPets = new List<int>();
        static readonly List<int> matEggs = new List<int>();

        public static int Target { get { return target; } }
        public static int SelectedCount { get { return matPets.Count + matEggs.Count; } }
        public static Button ConfirmButton { get; private set; }
        public static Button TileButton(bool egg, int idx) { Button b; return (egg ? eggTiles : petTiles).TryGetValue(idx, out b) ? b : null; }
        static readonly Dictionary<int, Button> petTiles = new Dictionary<int, Button>();
        static readonly Dictionary<int, Button> eggTiles = new Dictionary<int, Button>();

        static PetSkillHost H { get { return sheet.Host; } }
        static PetSystem P { get { return H.Pets; } }
        static GameDefs Defs { get { return H.Data.Defs; } }

        public static void Open(SkillPetSheet s, int idx)
        {
            sheet = s;
            target = idx;
            matPets.Clear(); matEggs.Clear();
            if (!Render()) PetSkillHost.Say(PetSkillStyle.T("toast_no_upgrade_pet"));
        }

        public static void Close()
        {
            if (sheet != null) sheet.Modal.Close(ModalName);
            target = -1;
        }

        static List<int> MatPool(bool egg, string rarity)
        {
            var pool = new List<int>();
            if (egg) { for (int i = 0; i < P.State.Eggs.Count; i++) if (P.State.Eggs[i].Rarity == rarity) pool.Add(i); }
            else for (int i = 0; i < P.State.Pets.Count; i++) if (i != target && !P.State.ActivePets.Contains(i) && P.State.Pets[i].Rarity == rarity) pool.Add(i);
            return pool;
        }

        public static void ToggleMat(bool egg, int idx)
        {
            if (!egg && P.State.ActivePets.Contains(idx)) { PetSkillHost.Say(PetSkillStyle.T("toast_mat_locked")); return; }
            var arr = egg ? matEggs : matPets;
            if (!arr.Remove(idx)) arr.Add(idx);
            Render();
        }

        public static void BulkSelect(bool egg, string rarity)
        {
            var arr = egg ? matEggs : matPets;
            List<int> pool = MatPool(egg, rarity);
            if (pool.Count == 0) return;
            bool all = true;
            foreach (int i in pool) if (!arr.Contains(i)) { all = false; break; }
            if (all) foreach (int i in pool) arr.Remove(i);
            else foreach (int i in pool) if (!arr.Contains(i)) arr.Add(i);
            Render();
        }

        public static void Confirm()
        {
            if (target < 0 || target >= P.State.Pets.Count) return;
            Pet t = P.State.Pets[target];
            if (t.Level >= P.Rules.MaxLevel) { PetSkillHost.Say(PetSkillStyle.T("toast_maxed")); return; }
            var mp = new List<Pet>();
            foreach (int i in matPets) if (i >= 0 && i < P.State.Pets.Count) mp.Add(P.State.Pets[i]);
            var me = new List<Egg>();
            foreach (int i in matEggs) if (i >= 0 && i < P.State.Eggs.Count) me.Add(P.State.Eggs[i]);
            if (mp.Count == 0 && me.Count == 0) return;
            if (!P.AbsorbMaterials(t, mp, me)) return;
            PetSkillHost.Say(PetSkillStyle.T("toast_pet_lv", Defs.PetKr.Get(t.Name, t.Name), t.Level));
            target = P.State.Pets.IndexOf(t);
            matPets.Clear(); matEggs.Clear();
            H.RequestRecalc();
            H.Sync();
            H.Save();
            if (target >= 0) Render(); else Close();
        }

        /// <summary>renderPetUpgrade — 대상이 없으면 닫고 false.</summary>
        public static bool Render()
        {
            if (target < 0 || target >= P.State.Pets.Count)
            {
                Debug.LogWarning("renderPetUpgrade: 대상 펫이 없다 — 팝업을 열지 않는다 (idx: " + target + " 보유: " + P.State.Pets.Count + ")");
                Close();
                return false;
            }
            petTiles.Clear(); eggTiles.Clear();
            Pet t = P.State.Pets[target];
            double need = P.XpNeeded(t.Level);
            bool maxed = t.Level >= P.Rules.MaxLevel;
            bool active = P.State.ActivePets.Contains(target);
            Big atk, hp;
            P.PetPower(t, out atk, out hp);
            double preview = 0;
            foreach (int i in matPets) if (i >= 0 && i < P.State.Pets.Count) preview += P.XpValue(P.State.Pets[i].Rarity) * P.LevelMult(P.State.Pets[i]);
            foreach (int i in matEggs) if (i >= 0 && i < P.State.Eggs.Count) preview += P.XpValue(P.State.Eggs[i].Rarity);
            float ratio = maxed ? 1f : (float)JsonTreeClamp(t.Xp / need);

            float wf = PetSkillStyle.L("petup_w_f");
            float w = wf * UiKit.RefW;
            float h = PetSkillStyle.Px("petup_h");
            PetSkillModal.Handle m = sheet.Modal.Open(ModalName, wf, h, PetSkillStyle.L("petup_top_rem"), true, () => { target = -1; });
            RectTransform c = m.Content;
            float pad = w * PetSkillStyle.L("petup_pad_f");
            float inner = w - pad * 2f;
            float y = pad;
            float sub = UiCatalog.Instance.Kind(TextKind.Sub).size, body = UiCatalog.Instance.Kind(TextKind.Body).size;

            // ---- petup-panel(회색 판) ----
            float ppad = PetSkillStyle.Px("petup_panel_pad_rem");
            float icon = inner * PetSkillStyle.L("petup_icon_w_f");
            float headH = Mathf.Max(icon, body * 1.3f + body * 1.35f * 2f);
            float xpH = PetSkillStyle.Px("petup_xp_h_rem"), xpMy = PetSkillStyle.Px("petup_xp_my_rem");
            float selH = Mathf.Max(sub * 1.3f, UiCatalog.Instance.Kind(TextKind.Button).size * 1.5f);
            float panelH = ppad + headH + xpMy + xpH + xpMy + selH + ppad;
            RectTransform panel = PetSkillKit.Framed(c, "petup-panel", PetSkillStyle.C("petup_panel"), PetSkillStyle.Px("petup_panel_r_rem"), PetSkillKit.Line3);
            UiKit.Place(panel, pad, y, inner, panelH);
            float py = ppad;
            RectTransform face = sheet.Pets.TileFace(panel, t.Name, t.Rarity, icon, active, t.Level, true);
            UiKit.Place(face, ppad, py, icon, icon);
            float tx = ppad + icon + inner * PetSkillStyle.L("petup_head_gap_f");
            float tw = inner - tx - ppad;
            TextMeshProUGUI name = PetSkillKit.Stroked(panel, "idet-name", TextKind.Body, PetSkillStyle.T("pet_name", Defs.RarityKr.Get(t.Rarity, t.Rarity), Defs.PetKr.Get(t.Name, t.Name)), PetSkillStyle.Rarity(Defs, t.Rarity), 0.25f, TextAlignmentOptions.Left);
            UiKit.Place(name.rectTransform, tx, py, tw, body * 1.3f);
            TextMeshProUGUI s1 = PetSkillKit.Text(panel, "idet-atk", TextKind.Body, PetSkillStyle.T("dmg", PetSkillStyle.Fmt(atk)), PetSkillStyle.C("black"), TextAlignmentOptions.Left);
            UiKit.Place(s1.rectTransform, tx, py + body * 1.3f, tw, body * 1.35f);
            TextMeshProUGUI s2 = PetSkillKit.Text(panel, "idet-hp", TextKind.Body, PetSkillStyle.T("hp", PetSkillStyle.Fmt(hp)), PetSkillStyle.C("black"), TextAlignmentOptions.Left);
            UiKit.Place(s2.rectTransform, tx, py + body * 1.3f + body * 1.35f, tw, body * 1.35f);
            py += headH + xpMy;
            string xpText = maxed ? PetSkillStyle.T("petup_maxed") : PetSkillStyle.T("petup_xp", PetSkillStyle.Fmt(t.Xp), PetSkillStyle.Fmt(need)) + (preview > 0 ? PetSkillStyle.T("petup_xp_preview", PetSkillStyle.Fmt(preview)) : string.Empty);
            RectTransform xp = PetSkillKit.Gauge(panel, "petup-xpbar", inner - ppad * 2f, xpH, ratio, xpText, PetSkillStyle.C("xpbar_bg"), PetSkillStyle.Px("petup_xp_r_rem"), PetSkillKit.Line3, TextKind.Sub);
            UiKit.Place(xp, ppad, py, inner - ppad * 2f, xpH);
            py += xpH + xpMy;
            TextMeshProUGUI sel = PetSkillKit.Text(panel, "petup-sellabel", TextKind.Sub, PetSkillStyle.T("petup_select"), PetSkillStyle.C("black"), TextAlignmentOptions.Left);
            UiKit.Place(sel.rectTransform, ppad, py, inner * 0.5f, selH);
            float cbw = PetSkillStyle.Px("petup_sel_btn_w");
            bool canConfirm = SelectedCount > 0 && !maxed;
            ConfirmButton = PetSkillKit.PaperButton(panel, "btn-confirm", PetSkillKit.BtnKind.Silver, PetSkillStyle.T("upgrade"), null, !canConfirm, Confirm, PetSkillStyle.Px("petup_sel_btn_r_rem"));
            UiKit.Place(ConfirmButton.GetComponent<RectTransform>(), inner - ppad - cbw, py, cbw, selH);
            y += panelH + PetSkillStyle.Px("petup_panel_mb_rem");

            // ---- bulk row ----
            float bulkMx = PetSkillStyle.Px("petup_bulk_mx_rem"), bulkGap = PetSkillStyle.Px("petup_bulk_gap_rem");
            float chipH = Mathf.Max(PetSkillStyle.Px("petup_bulk_min_rem"), sub * 1.4f);
            float bx = pad + bulkMx;
            float rowRight = pad + inner - bulkMx;
            float rowY = y;
            foreach (string r in Defs.Rarities)
            {
                for (int kind = 0; kind < 2; kind++)
                {
                    bool egg = kind == 0;
                    List<int> pool = MatPool(egg, r);
                    if (pool.Count == 0) continue;
                    var arr = egg ? matEggs : matPets;
                    bool all = true;
                    foreach (int i in pool) if (!arr.Contains(i)) { all = false; break; }
                    string n = pool.Count.ToString();
                    float cw = chipH * 0.9f + PetSkillKit.TextWidth(TextKind.Sub, n) + PetSkillStyle.Px("petup_bulk_pad_rem") * 2f + PetSkillStyle.Rem(0.18f);
                    if (bx + cw > rowRight) { bx = pad + bulkMx; rowY += chipH + bulkGap; }
                    string rr = r;
                    Button chip = UiKit.Button(c, "petup-bulk-" + (egg ? "egg-" : "pet-") + r, () => BulkSelect(egg, rr));
                    RectTransform cr = chip.GetComponent<RectTransform>();
                    UiKit.Place(cr, bx, rowY, cw, chipH);
                    Color rc = PetSkillStyle.Rarity(Defs, r);
                    RectTransform skin = PetSkillKit.Framed(cr, "skin", all ? rc : PetSkillStyle.C("bulk_bg"), PetSkillStyle.Px("petup_bulk_r_rem"), PetSkillKit.Line2);
                    UiKit.Fill(skin);
                    ((Image)skin.Find("line").GetComponent<Image>()).color = rc;
                    Image sil = UiKit.Icon(cr, "bulk-sil", egg ? "egg" : "paw");
                    sil.color = all ? PetSkillStyle.C("white") : rc;
                    float ip = PetSkillStyle.Px("petup_bulk_pad_rem");
                    UiKit.Place(sil.rectTransform, ip, chipH * 0.1f, chipH * 0.8f, chipH * 0.8f);
                    TextMeshProUGUI nt = PetSkillKit.Text(cr, "bulk-n", TextKind.Sub, n, all ? PetSkillStyle.C("white") : PetSkillStyle.C("ink"), TextAlignmentOptions.Left);
                    UiKit.Place(nt.rectTransform, ip + chipH * 0.8f + PetSkillStyle.Rem(0.18f), 0f, cw, chipH);
                    bx += cw + bulkGap;
                }
            }
            y = rowY + chipH + bulkGap;
            // ---- divider ----
            Image div = UiKit.Panel(c, "petup-divider", "pp_paper");
            div.color = PetSkillStyle.C("divider");
            UiKit.Place(div.rectTransform, pad, y, inner, PetSkillStyle.L("petup_divider_px"));
            y += PetSkillStyle.L("petup_divider_px") + PetSkillStyle.Px("petup_divider_mb_rem");

            // ---- mat-grid(스크롤) ----
            ScrollRect sr;
            RectTransform content = PetSkillKit.Scroll(c, "mat-grid", out sr);
            float gridPadX = PetSkillStyle.Px("petup_grid_pad_x_rem");
            UiKit.Place((RectTransform)content.parent, pad + gridPadX, y, inner - gridPadX * 2f, Mathf.Max(10f, h - y - pad));
            BuildMatGrid(content, inner - gridPadX * 2f);
            return true;
        }

        static double JsonTreeClamp(double v) { return double.IsNaN(v) ? 0 : System.Math.Min(1, System.Math.Max(0, v)); }

        static void BuildMatGrid(RectTransform content, float W)
        {
            int cols = Mathf.RoundToInt(PetSkillStyle.L("petup_grid_cols"));
            float gx = PetSkillStyle.Px("petup_grid_gap_x_rem"), gy = PetSkillStyle.Px("petup_grid_gap_y_rem");
            float colW = (W - (cols - 1) * gx) / cols;
            float sub = UiCatalog.Instance.Kind(TextKind.Sub).size;
            float cellH = colW + PetSkillStyle.Rem(0.1f) + sub;
            var items = new List<KeyValuePair<bool, int>>();
            for (int i = 0; i < P.State.Pets.Count; i++) if (i != target) items.Add(new KeyValuePair<bool, int>(false, i));
            for (int i = 0; i < P.State.Eggs.Count; i++) items.Add(new KeyValuePair<bool, int>(true, i));
            if (items.Count == 0)
            {
                TextMeshProUGUI empty = PetSkillKit.Text(content, "mat-empty", TextKind.Sub, PetSkillStyle.T("petup_empty"), PetSkillStyle.C("muted"), TextAlignmentOptions.Center, false);
                UiKit.Place(empty.rectTransform, 0f, PetSkillStyle.Rem(1.1f), W, sub * 1.5f);
                content.sizeDelta = new Vector2(0f, PetSkillStyle.Rem(2.2f) + sub * 1.5f);
                return;
            }
            int rows = (items.Count + cols - 1) / cols;
            content.sizeDelta = new Vector2(0f, rows * cellH + (rows - 1) * gy + gy);
            for (int k = 0; k < items.Count; k++)
            {
                bool egg = items[k].Key;
                int idx = items[k].Value;
                float x = (k % cols) * (colW + gx), y = (k / cols) * (cellH + gy);
                Button b = UiKit.Button(content, (egg ? "egg-" : "pet-") + idx, () => ToggleMat(egg, idx));
                RectTransform cell = b.GetComponent<RectTransform>();
                UiKit.Place(cell, x, y, colW, cellH);
                bool on = (egg ? matEggs : matPets).Contains(idx);
                if (egg)
                {
                    eggTiles[idx] = b;
                    Egg e = P.State.Eggs[idx];
                    Image ico = UiKit.Icon(cell, "egg", "egg", PetSkillStyle.RarityHex(Defs, e.Rarity));
                    UiKit.Place(ico.rectTransform, 0f, 0f, colW, colW);
                    TextMeshProUGUI lab = PetSkillKit.Text(cell, "tile-label", TextKind.Sub, PetSkillStyle.T("egg"), PetSkillStyle.C("ink"));
                    UiKit.Place(lab.rectTransform, 0f, colW + PetSkillStyle.Rem(0.1f), colW, sub);
                    if (on) Check(cell, colW, "tile_check_egg", PetSkillStyle.Px("tile_r_rem"));
                }
                else
                {
                    petTiles[idx] = b;
                    Pet p = P.State.Pets[idx];
                    bool locked = P.State.ActivePets.Contains(idx);
                    RectTransform face = sheet.Pets.TileFace(cell, p.Name, p.Rarity, colW, locked, p.Level, true);
                    UiKit.Place(face, 0f, 0f, colW, colW);
                    if (locked)
                    {
                        CanvasGroup cg = face.gameObject.AddComponent<CanvasGroup>();
                        cg.alpha = 0.5f;
                    }
                    if (on) Check(cell, colW, "tile_check", PetSkillStyle.Rem(0.3f));
                }
            }
        }

        static void Check(RectTransform cell, float size, string colorKey, float r)
        {
            Image ov = PetSkillKit.Fill(cell, "tile-check", PetSkillStyle.C(colorKey), r);
            UiKit.Place(ov.rectTransform, 0f, 0f, size, size);
            Image ck = UiKit.Icon(ov.rectTransform, "ico", "check");
            ck.color = PetSkillStyle.C("white");
            float cs = size * 0.42f;
            UiKit.Anchor(ck.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, cs, cs);
        }
    }
}
