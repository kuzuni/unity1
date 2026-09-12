using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.Mounts;
using Forge.Game.Gallery;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 탈것 업그레이드 팝업(원작 ui.js openMountUpgrade · renderMountUpgrade · onToggleMountUpgradeMat · onConfirmMountUpgrade · index.html `#mount-upgrade-modal` · css `.modal-card.wide` + `.mat-grid`/`.mat-chip`):
    /// 제목 «이름 업그레이드» → 행(둥근 얼굴 2.4rem · Lv.N ★ · 경험치 N/M (+예정)) → 안내 한 줄 → 재료 칩 5열 격자(최대 5개 · 장착 중 개체는 노란 테 · 고른 칩은 초록 링 · 앱 높이 40% 안에서 스크롤) → [업그레이드] · [닫기].
    /// 재료도 **개체 인덱스**다(같은 이름이 여럿). 대상을 못 잡으면 열지 않는다(원작 upgrade-modal-empty-guard). 규칙은 Core <see cref="MountSystem.AbsorbMaterials"/>(재료 흡수 뒤 대상 인덱스 보정).
    /// </summary>
    public static class MountUpgradePopup
    {
        public const string ModalName = "mount-upgrade";
        static SkillPetSheet sheet;
        static int target = -1;
        static readonly List<int> mats = new List<int>();
        static readonly Dictionary<int, Button> chips = new Dictionary<int, Button>();

        public static int Target { get { return target; } }
        public static int SelectedCount { get { return mats.Count; } }
        public static Button ConfirmButton { get; private set; }
        public static Button CloseButton { get; private set; }
        public static Button Chip(int idx) { Button b; return chips.TryGetValue(idx, out b) ? b : null; }

        static PetSkillHost H { get { return sheet.Host; } }
        static MountSystem M { get { return H.Mounts; } }
        static GameDefs Defs { get { return H.Data.Defs; } }
        static int MatMax { get { return Mathf.RoundToInt(PetSkillStyle.L("mtup_mat_max")); } }

        public static void Open(SkillPetSheet s, int idx)
        {
            sheet = s;
            target = idx;
            mats.Clear();
            if (!Render()) PetSkillHost.Say(PetSkillStyle.T("toast_no_upgrade_mount"));
        }

        public static void Close()
        {
            if (sheet != null && sheet.Modal != null) sheet.Modal.Close(ModalName);
            target = -1;
        }

        /// <summary>원작 `onToggleMountUpgradeMat` — 최대 5개.</summary>
        public static void ToggleMat(int idx)
        {
            if (!mats.Remove(idx) && mats.Count < MatMax) mats.Add(idx);
            Render();
        }

        /// <summary>원작 `onConfirmMountUpgrade` — 재료를 지우면 뒤 인덱스가 당겨져 대상 인덱스도 움직인다: 보정값으로 대상을 다시 잡는다.</summary>
        public static void Confirm()
        {
            Mount t = M.Inst(target);
            if (t != null && t.Level >= M.Rules.IndivMaxLevel) { PetSkillHost.Say(PetSkillStyle.T("toast_maxed")); return; }
            if (mats.Count == 0 || t == null) return;
            string name = t.Name;
            int newIdx = target;
            foreach (int i in mats) if (i < target) newIdx--;
            int consumed;
            if (!M.AbsorbMaterials(target, mats, out consumed)) return;
            target = newIdx;
            Mount nt = M.Inst(newIdx) ?? t;
            PetSkillHost.Say(PetSkillStyle.T("toast_mount_lv", Defs.MountKr.Get(name, name), nt.Level));
            mats.Clear();
            H.MountsChanged();
            H.Save();
            Render();
            MountSheet.Refresh();   // 재료로 소모된 탈것이 뒤에 깔린 목록에서도 바로 사라지게
        }

        /// <summary>renderMountUpgrade — 대상이 없으면 닫고 false.</summary>
        public static bool Render()
        {
            Mount t = M.Inst(target);
            if (t == null)
            {
                Debug.LogWarning("renderMountUpgrade: 대상 탈것이 없다 — 팝업을 열지 않는다 (idx: " + target + " 보유: " + M.Count() + ")");
                Close();
                return false;
            }
            chips.Clear();
            double need = M.XpNeeded(t.Level);
            bool maxed = t.Level >= M.Rules.IndivMaxLevel;
            double preview = 0;
            foreach (int i in mats) { Mount mm = M.Inst(i); if (mm != null) preview += M.XpValue(mm.Rarity) * M.LevelMult(mm); }
            var pool = new List<int>();
            for (int i = 0; i < M.Count(); i++) if (i != target) pool.Add(i);

            float wf = PetSkillStyle.L("mtup_w_f");
            float w = wf * UiKit.RefW;
            float pad = PetSkillStyle.Px("mtup_pad_rem");
            float inner = w - pad * 2f;
            float sub = UiCatalog.Instance.Kind(TextKind.Sub).size, body = UiCatalog.Instance.Kind(TextKind.Body).size;
            float h3H = body * 1.4f;
            float faceR = PetSkillStyle.Px("mtup_face_rem");
            float rowMy = PetSkillStyle.Px("mtup_row_my_rem"), rowGap = PetSkillStyle.Px("mtup_row_gap_rem");
            float rowH = Mathf.Max(faceR, sub * 1.3f * 2f);
            float pH = sub * 1.35f * 2f;
            float gridMy = PetSkillStyle.Px("mtup_grid_my_rem"), gridGap = PetSkillStyle.Px("mtup_grid_gap_rem");
            int cols = Mathf.RoundToInt(PetSkillStyle.L("mtup_grid_cols"));
            float chipW = (inner - (cols - 1) * gridGap) / cols;
            float chipPad = PetSkillStyle.Px("mtup_chip_pad_rem");
            float chipH = chipPad * 2f + chipW * 0.62f + PetSkillStyle.Rem(0.1f) + sub;
            int rows = pool.Count == 0 ? 1 : (pool.Count + cols - 1) / cols;
            float gridFull = pool.Count == 0 ? PetSkillStyle.Rem(2.2f) + sub * 1.5f : rows * chipH + (rows - 1) * gridGap;
            float gridH = Mathf.Min(gridFull, UiKit.RefH * PetSkillStyle.L("mtup_grid_max_f"));
            float btnH = PetSkillStyle.Px("mtup_btn_h_rem"), btnGap = PetSkillStyle.Px("mtup_btn_gap_rem");
            float h = pad + h3H + rowMy + rowH + rowMy + pH + gridMy + gridH + gridMy + btnH + btnGap + btnH + pad;
            PetSkillModal.Handle m = sheet.Modal.Open(ModalName, wf, h, 0f, true, () => { target = -1; });
            RectTransform c = m.Content;
            float y = pad;

            // ---- h3 ----
            TextMeshProUGUI h3 = PetSkillKit.Text(c, "mtup-title", TextKind.Body, PetSkillStyle.T("mtup_title", Defs.MountKr.Get(t.Name, t.Name)), PetSkillStyle.C("ink"), TextAlignmentOptions.Left);
            UiKit.Place(h3.rectTransform, pad, y, inner, h3H);
            y += h3H + rowMy;
            // ---- row: 둥근 얼굴 + Lv/★ + 경험치 ----
            RectTransform orb = PetSkillKit.Framed(c, "mtup-face", PetSkillStyle.C("white"), faceR * 0.5f, PetSkillKit.Line2);
            UiKit.Place(orb, pad, y + (rowH - faceR) * 0.5f, faceR, faceR);
            ((Image)orb.Find("line").GetComponent<Image>()).color = PetSkillStyle.Rarity(Defs, t.Rarity);
            RectTransform pf = PetSkillKit.PetFace(orb, Defs, t.Name, faceR * 0.78f, GalleryKind.Mounts);
            UiKit.Anchor(pf, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, faceR * 0.78f, faceR * 0.78f);
            float tx = pad + faceR + rowGap;
            string lv = PetSkillStyle.T("lv_short", t.Level) + (t.Stars > 0 ? " ★" + t.Stars : string.Empty);
            TextMeshProUGUI lvT = PetSkillKit.Text(c, "item-name", TextKind.Sub, lv, PetSkillStyle.C("ink"), TextAlignmentOptions.Left);
            UiKit.Place(lvT.rectTransform, tx, y, inner - faceR - rowGap, sub * 1.3f);
            string xp = maxed ? PetSkillStyle.T("petup_maxed") : PetSkillStyle.T("mtup_xp", PetSkillStyle.Fmt(t.Xp), PetSkillStyle.Fmt(need)) + (preview > 0 ? PetSkillStyle.T("mtup_xp_preview", PetSkillStyle.Fmt(preview)) : string.Empty);
            TextMeshProUGUI xpT = PetSkillKit.Text(c, "muted", TextKind.Sub, xp, PetSkillStyle.C("muted"), TextAlignmentOptions.Left, false);
            UiKit.Place(xpT.rectTransform, tx, y + sub * 1.3f, inner - faceR - rowGap, sub * 1.3f);
            y += rowH + rowMy;
            // ---- p.muted ----
            TextMeshProUGUI p = PetSkillKit.Text(c, "mtup-hint", TextKind.Sub, PetSkillStyle.T("mtup_select"), PetSkillStyle.C("muted"), TextAlignmentOptions.Left, false);
            p.enableWordWrapping = true;
            UiKit.Place(p.rectTransform, pad, y, inner, pH);
            y += pH + gridMy;
            // ---- mat-grid ----
            ScrollRect sr;
            RectTransform content = PetSkillKit.Scroll(c, "mat-grid", out sr);
            UiKit.Place((RectTransform)content.parent, pad, y, inner, gridH);
            if (pool.Count == 0)
            {
                TextMeshProUGUI empty = PetSkillKit.Text(content, "mat-empty", TextKind.Sub, PetSkillStyle.T("mtup_empty"), PetSkillStyle.C("muted"), TextAlignmentOptions.Center, false);
                UiKit.Place(empty.rectTransform, 0f, PetSkillStyle.Rem(1.1f), inner, sub * 1.5f);
                content.sizeDelta = new Vector2(0f, gridFull);
            }
            else
            {
                content.sizeDelta = new Vector2(0f, gridFull);
                for (int k = 0; k < pool.Count; k++)
                {
                    int idx = pool[k];
                    Mount mm = M.Inst(idx);
                    float cx = (k % cols) * (chipW + gridGap), cy = (k / cols) * (chipH + gridGap);
                    bool on = mats.Contains(idx);
                    bool active = M.IsActive(idx);
                    Button b = UiKit.Button(content, "mat-chip-" + idx, () => ToggleMat(idx));
                    RectTransform cr = b.GetComponent<RectTransform>();
                    UiKit.Place(cr, cx, cy, chipW, chipH);
                    RectTransform skin = PetSkillKit.Framed(cr, "skin", PetSkillStyle.C(on ? "mtup_chip_on" : "mtup_chip_bg"), PetSkillStyle.Px("mtup_chip_r_rem"), PetSkillKit.Line2);
                    UiKit.Fill(skin);
                    ((Image)skin.Find("line").GetComponent<Image>()).color = on ? PetSkillStyle.C("mtup_chip_on_ring") : active ? PetSkillStyle.C("mtup_chip_active_line") : PetSkillStyle.Rarity(Defs, mm.Rarity);
                    float fs = chipW * 0.62f;
                    RectTransform f = PetSkillKit.PetFace(cr, Defs, mm.Name, fs, GalleryKind.Mounts);
                    UiKit.Place(f, (chipW - fs) * 0.5f, chipPad, fs, fs);
                    string small = PetSkillStyle.T("lv_short", mm.Level) + (mm.Stars > 0 ? " ★" + mm.Stars : string.Empty);
                    TextMeshProUGUI st = PetSkillKit.Text(cr, "small", TextKind.Sub, small, PetSkillStyle.C(on ? "mtup_chip_on_ink" : "muted"), TextAlignmentOptions.Center, false);
                    UiKit.Place(st.rectTransform, 0f, chipPad + fs + PetSkillStyle.Rem(0.1f), chipW, sub);
                    chips[idx] = b;
                }
            }
            y += gridH + gridMy;
            // ---- buttons ----
            bool canConfirm = mats.Count > 0 && !maxed;
            ConfirmButton = PetSkillKit.PaperButton(c, "btn-confirm", PetSkillKit.BtnKind.Primary, PetSkillStyle.T("upgrade"), null, !canConfirm, Confirm);
            UiKit.Place(ConfirmButton.GetComponent<RectTransform>(), pad, y, inner, btnH);
            y += btnH + btnGap;
            CloseButton = PetSkillKit.PaperButton(c, "btn-close", PetSkillKit.BtnKind.Gray, PetSkillStyle.T("close"), null, false, Close);
            UiKit.Place(CloseButton.GetComponent<RectTransform>(), pad, y, inner, btnH);
            return true;
        }
    }
}
