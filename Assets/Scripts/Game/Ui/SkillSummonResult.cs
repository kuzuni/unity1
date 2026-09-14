using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.Mounts;
using Forge.Core.Pets;
using Forge.Game.Gallery;
using Forge.Core.Skills;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 소환 결과 연출 팝업(원작 ui.js openSummonResult · summonEntries · groupSummonEntries · tickSummonResult · fireSummonHero · finishSummonResult · onSummonResultTap · summonSoloInfo · buildSummonReflection · 스킬·펫·탈것 공용).
    /// 원작 타임라인 그대로: ① 빛 모임(SR_CHARGE 240ms) → ② 등급 오름차순으로 셀 팝(≤10 = 125ms 간격 · &gt;10 = 행 웨이브 300/40ms + 등급 경계 200ms 정지) →
    /// ③ 최고 등급(전설↑ · 단독)은 150ms 홀드백 뒤 주역 비트 + 화면 섬광 → ④ 150ms 여운 뒤 [확인] · 등급 집계 칩 · x1 은 요약+[다시 소환]. 탭 = 스킵 · 끝난 뒤 탭 = 닫기.
    /// 11개부터 같은 항목을 한 셀로 묶는다(×N · 조각 +N · NEW). 그림 = 슬롯의 그림(스킬 sk_* · 알 등급색 egg · summon-result-image-unify).
    /// 광채·광선·소환진·먼지·반사는 CSS 다층 그라데이션이라 정점 색 원판·섬광·바닥 타원으로 줄였다(결정 기록 · 주인 눈 확인). 효과음은 <see cref="PetSkillHost"/> 훅(T30).
    /// </summary>
    public sealed class SkillSummonResultView : MonoBehaviour
    {
        public const string ModalName = "summon-result";

        public sealed class Entry
        {
            public string Key, IconKey, IconTint, Rarity, Name, Sub, Extra;
            public bool IsNew, Bonus;
            public int Qty = 1, NewQty;
            public string FaceName;
            public GalleryKind FaceKind = GalleryKind.Pets;
        }

        sealed class Cell
        {
            public RectTransform Root;
            public CanvasGroup Group;
            public RectTransform OrbWrap;
            public Entry Entry;
            public float BaseScale;
            public float Pop;
            public bool On;
            public float OnAt;
            public bool Heroic;
        }

        public static SkillSummonResultView Current { get; private set; }

        SkillPetSheet sheet;
        PetSkillModal.Handle handle;
        readonly List<Cell> cells = new List<Cell>();
        readonly List<float> delays = new List<float>();
        List<Entry> entries;
        List<Entry> rollList;
        int rolls;
        float start;
        int idx;
        bool done, holdback, heroFired;
        int heroIdx = -1;
        string best;
        Action repeat;
        Image flash;
        float flashAt = -1f;
        RectTransform foot;
        GameObject hint, ok, chips, solo;
        string kind;

        public bool Done { get { return done; } }
        public int CellCount { get { return cells.Count; } }
        public int OnCount { get { int n = 0; foreach (Cell c in cells) if (c.On) n++; return n; } }
        public Button OkButton { get; private set; }
        public Button AgainButton { get; private set; }

        static GameDefs Defs { get { return PetSkillHost.Instance.Data.Defs; } }

        // ===== 진입(원작 openSummonResult · summonEntries) =====

        public static SkillSummonResultView Open(SkillPetSheet sheet, string kind, SkillSummonResult r, Action repeat)
        {
            var list = new List<Entry>();
            foreach (SkillSummonResult.Item it in r.Results)
                list.Add(new Entry { Key = "sk:" + it.Def.Id, IconKey = "sk_" + it.Def.Id, Rarity = it.Def.Rarity, Name = it.Def.Name, IsNew = it.IsNew });
            return Open(sheet, kind, list, r.BestRarity, repeat);
        }

        public static SkillSummonResultView Open(SkillPetSheet sheet, string kind, SummonResult r, Action repeat)
        {
            var list = new List<Entry>();
            foreach (SummonResult.Item it in r.Results)
            {
                string kr = Defs.RarityKr.Get(it.Rarity, it.Rarity);
                list.Add(new Entry
                {
                    Key = "eg:" + it.Rarity + (it.Extra ? ":x" : ""), IconKey = "egg", IconTint = PetSkillStyle.RarityHex(Defs, it.Rarity), Rarity = it.Rarity,
                    Name = it.Extra ? PetSkillStyle.T("sr_egg_bonus", kr) : PetSkillStyle.T("sr_egg", kr), IsNew = false, Bonus = it.Extra,
                });
            }
            return Open(sheet, kind, list, r.BestRarity, repeat);
        }

        /// <summary>원작 summonEntries('mount') — 키 `mt:이름` · 3D 얼굴(`mountFace`) · MOUNT_KR 이름 · 신규/중복.</summary>
        public static SkillSummonResultView Open(SkillPetSheet sheet, string kind, MountSummonResult r, Action repeat)
        {
            var list = new List<Entry>();
            foreach (MountSummonResult.Item it in r.Results)
                list.Add(new Entry { Key = "mt:" + it.Name, IconKey = "winder", Rarity = it.Rarity, Name = Defs.MountKr.Get(it.Name, it.Name), IsNew = it.IsNew, FaceName = it.Name, FaceKind = GalleryKind.Mounts });
            return Open(sheet, kind, list, r.BestRarity, repeat);
        }

        public static SkillSummonResultView Open(SkillPetSheet sheet, string kind, List<Entry> rollsList, string bestRarity, Action repeat)
        {
            if (rollsList == null || rollsList.Count == 0) return null;
            if (Current != null) Current.Close();
            PetSkillModal.Handle h = sheet.Modal.OpenFull(ModalName, () => { if (Current != null) Current.OnTap(); });
            var v = h.Root.gameObject.AddComponent<SkillSummonResultView>();
            Current = v;
            v.sheet = sheet;
            v.handle = h;
            v.kind = kind;
            v.repeat = repeat;
            v.Build(rollsList);
            return v;
        }

        // ===== groupSummonEntries =====
        static List<Entry> Group(string kind, List<Entry> rollsList, bool merge)
        {
            string dup = kind == "skill" ? PetSkillStyle.T("sr_dup_skill") : kind == "mount" ? PetSkillStyle.T("sr_dup_mount") : null;
            var list = new List<Entry>();
            if (merge)
            {
                var map = new Dictionary<string, Entry>();
                foreach (Entry e in rollsList)
                {
                    Entry g;
                    if (map.TryGetValue(e.Key, out g)) { g.Qty++; if (e.IsNew) g.NewQty++; }
                    else
                    {
                        g = new Entry { Key = e.Key, IconKey = e.IconKey, IconTint = e.IconTint, Rarity = e.Rarity, Name = e.Name, Bonus = e.Bonus, Qty = 1, NewQty = e.IsNew ? 1 : 0, FaceName = e.FaceName, FaceKind = e.FaceKind };
                        map[e.Key] = g;
                        list.Add(g);
                    }
                }
            }
            else
                foreach (Entry e in rollsList)
                    list.Add(new Entry { Key = e.Key, IconKey = e.IconKey, IconTint = e.IconTint, Rarity = e.Rarity, Name = e.Name, Bonus = e.Bonus, Qty = 1, NewQty = e.IsNew ? 1 : 0, FaceName = e.FaceName, FaceKind = e.FaceKind });
            foreach (Entry g in list)
            {
                g.IsNew = g.NewQty > 0;
                g.Sub = Defs.RarityKr.Get(g.Rarity, g.Rarity);
                g.Extra = dup != null && g.NewQty == 0 ? PetSkillStyle.T("sr_dup", dup, g.Qty) : string.Empty;
            }
            // 등급 오름차순(안정) — 마지막이 최고 등급
            var stable = new List<Entry>(list);
            for (int i = 1; i < stable.Count; i++)
            {
                Entry x = stable[i];
                int j = i - 1;
                while (j >= 0 && RarityIdx(stable[j].Rarity) > RarityIdx(x.Rarity)) { stable[j + 1] = stable[j]; j--; }
                stable[j + 1] = x;
            }
            return stable;
        }

        static int RarityIdx(string r) { return Array.IndexOf(Defs.Rarities, r); }
        static bool Hi(string r) { return r == "legendary" || r == "ultimate" || r == "mythic"; }

        /// <summary>원작 srCols — 마지막 행 충전율이 가장 높은 열 수.</summary>
        public static int Cols(int n)
        {
            int[] cand = n >= 6 && n <= 10 ? new[] { 3, 4, 5 } : new[] { 5, 4, 6 };
            int bestC = cand[0];
            float bestFill = -1f;
            foreach (int c in cand)
            {
                float fill = (n % c == 0 ? c : n % c) / (float)c;
                if (fill > bestFill + 1e-9f) { bestFill = fill; bestC = c; }
            }
            return bestC;
        }

        // ===== 그리기 =====
        void Build(List<Entry> rollsList)
        {
            rolls = rollsList.Count;
            rollList = rollsList;
            int mergeFrom = Mathf.RoundToInt(PetSkillStyle.L("sr_merge_from"));
            entries = Group(kind, rollsList, rolls >= mergeFrom);
            int n = entries.Count;
            best = entries[n - 1].Rarity;
            holdback = Hi(best) && entries[n - 1].Qty == 1;
            heroIdx = Hi(best) ? n - 1 : -1;
            bool heroRow = heroIdx > 0 && n <= 10;
            int cols = Cols(n);
            bool one = n == 1, dense = n > 24, mid = !dense && n > 10;
            bool stage = n <= 10;
            float W = UiKit.RefW, Hh = UiKit.RefH;
            RectTransform c = handle.Content;
            UiKit.Fill(c);

            // ---- 배경(남색 방사 → 검정) ----
            Image bg = UiKit.Panel(c, "bg", "pp_line");
            bg.color = PetSkillStyle.C("sr_bg_d");
            Image glowB = PetSkillKit.Disc(c, "bg-b", PetSkillStyle.C("sr_bg_c"));
            glowB.preserveAspect = false;
            UiKit.Anchor(glowB.rectTransform, new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.5f), Vector2.zero, W * 2.4f, Hh * 1.5f);
            Image glowA = PetSkillKit.Disc(c, "bg-a", Color.Lerp(PetSkillStyle.C("sr_bg_a"), PetSkillStyle.Rarity(Defs, best), 0.24f));
            glowA.preserveAspect = false;
            UiKit.Anchor(glowA.rectTransform, new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.5f), Vector2.zero, W * 1.5f, Hh * 0.9f);
            Image halo = PetSkillKit.Disc(c, "halo", PetSkillStyle.Rarity(Defs, best));
            halo.color = new Color(halo.color.r, halo.color.g, halo.color.b, 0.18f);
            halo.preserveAspect = false;
            UiKit.Anchor(halo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, PetSkillStyle.Rem(26f), PetSkillStyle.Rem(16f));

            // ---- 머리 ----
            float padT = PetSkillStyle.Px("sr_pad_top_rem"), padX = PetSkillStyle.Px("sr_pad_x_rem"), padB = PetSkillStyle.Px("sr_pad_bottom_rem");
            float title = UiCatalog.Instance.Kind(TextKind.Title).size;
            float headH = title * 1.4f + PetSkillStyle.Px("sr_title_pad_y_rem") * 2f;
            float headY = padT + Hh * PetSkillStyle.L("sr_head_top_f");
            RectTransform head = UiKit.Box(c, "sr-head");
            UiKit.Place(head, 0f, headY, W, headH);
            string tt = PetSkillStyle.T("sr_title_x", PetSkillStyle.T(kind == "pet" ? "sr_title_pet" : kind == "mount" ? "sr_title_mount" : "sr_title_skill"), rolls);
            float tw = PetSkillKit.TextWidth(TextKind.Title, tt) + title * 1.2f + PetSkillStyle.Px("sr_title_pad_x_rem") * 2f;
            RectTransform band = UiKit.Box(head, "sr-title");
            UiKit.Anchor(band, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, tw, headH);
            Image bandBg = UiKit.Panel(band, "bg", "pp_line");
            bandBg.color = PetSkillStyle.C("sr_title_band");
            Image tico = UiKit.Icon(band, "ico", kind == "pet" ? "egg" : kind == "mount" ? "winder" : "ticket");
            UiKit.Place(tico.rectTransform, PetSkillStyle.Px("sr_title_pad_x_rem"), (headH - title * 1.05f) * 0.5f, title * 1.05f, title * 1.05f);
            TextMeshProUGUI tx = PetSkillKit.Text(band, "t", TextKind.Title, tt, PetSkillStyle.C("white"), TextAlignmentOptions.Left);
            LetterSpacing.Apply(tx, "sr_title_ls_em");   // T168 3회차 — 정본 style.css 6207 `.sr-title { letter-spacing: .07em }`
            UiKit.Place(tx.rectTransform, PetSkillStyle.Px("sr_title_pad_x_rem") + title * 1.2f, 0f, tw, headH);

            // ---- 발(집계 · 힌트 · 확인) ----
            float footH = Mathf.Max(PetSkillStyle.Px("sr_foot_min_rem"), PetSkillStyle.Px("sr_ok_h_rem") + UiCatalog.Instance.Kind(TextKind.Sub).size * 1.6f + PetSkillStyle.Px("sr_foot_gap_rem") * 2f);
            foot = UiKit.Box(c, "sr-foot");
            UiKit.Place(foot, padX, Hh - padB - footH, W - padX * 2f, footH);
            BuildFoot(footH, W - padX * 2f);

            // ---- 몸(그리드) ----
            float bodyY = headY + headH;
            float bodyH = Hh - padB - footH - bodyY;
            RectTransform body = UiKit.Box(c, "sr-body");
            UiKit.Place(body, padX, bodyY, W - padX * 2f, bodyH);
            float gw = W - padX * 2f;
            float gapX = PetSkillStyle.Px("sr_grid_gap_x_rem"), gapY = PetSkillStyle.Px("sr_grid_gap_y_rem");
            float cellW;
            if (one) cellW = Mathf.Min(PetSkillStyle.Px("sr_cell_one_max_rem"), W * 0.58f);
            else if (dense) cellW = (gw - PetSkillStyle.Px("sr_dense_gap_rem")) / PetSkillStyle.L("sr_dense_cols");
            else if (mid) cellW = Mathf.Min(PetSkillStyle.Px("sr_cell_mid_max_rem"), (gw - (cols - 1) * gapX - PetSkillStyle.Rem(1.2f)) / cols);
            else if (stage) cellW = Mathf.Min(PetSkillStyle.Px("sr_cell_stage_max_rem"), (gw - (cols - 1) * gapX - PetSkillStyle.Rem(0.4f)) / cols);
            else cellW = Mathf.Min(PetSkillStyle.Px("sr_cell_max_rem"), (gw - (cols - 1) * gapX - PetSkillStyle.Rem(0.4f)) / cols);
            if (dense) cols = Mathf.RoundToInt(PetSkillStyle.L("sr_dense_cols"));
            float sub = UiCatalog.Instance.Kind(TextKind.Sub).size;
            float nameH = dense ? 0f : sub * PetSkillStyle.L("sr_name_h_em") * 0.62f;
            float subH = dense ? 0f : sub * 1.35f;
            float cellH = cellW + (dense ? 0f : PetSkillStyle.Px("sr_name_mt_rem") + nameH + PetSkillStyle.Px("sr_sub_mt_rem") + subH);
            float heroOrb = heroRow ? Mathf.Min(PetSkillStyle.Px("sr_hero_orb_rem"), W * 0.36f) : mid ? Mathf.Min(PetSkillStyle.Px("sr_hero_mid_orb_rem"), W * 0.3f) : dense ? Mathf.Min(PetSkillStyle.Px("sr_hero_dense_orb_rem"), W * 0.24f) : cellW;
            bool heroOwnRow = heroIdx >= 0 && (heroRow || mid || dense);
            int normal = heroOwnRow ? n - 1 : n;
            int rows = (normal + cols - 1) / cols;
            float gridH = rows * cellH + Mathf.Max(0, rows - 1) * gapY;
            float heroH = heroOwnRow ? heroOrb + PetSkillStyle.Px("sr_hero_top_rem") + PetSkillStyle.Px("sr_name_mt_rem") + sub * 1.6f + PetSkillStyle.Px("sr_sub_mt_rem") + sub * 1.5f + gapY : 0f;
            float totalH = gridH + heroH;
            ScrollRect scroll = null;
            RectTransform grid;
            if (totalH > bodyH)
            {
                grid = PetSkillKit.Scroll(body, "sr-grid", out scroll);
                UiKit.Fill((RectTransform)grid.parent);
                grid.sizeDelta = new Vector2(0f, totalH + PetSkillStyle.Rem(1f));
            }
            else
            {
                grid = UiKit.Box(body, "sr-grid");
                UiKit.Place(grid, 0f, (bodyH - totalH) * 0.5f, gw, totalH);
                if (stage)
                {
                    // 소환진(바닥 타원) — 그리드 아래
                    Image floor = PetSkillKit.Disc(body, "sr-floor", PetSkillStyle.Rarity(Defs, best));
                    floor.color = new Color(floor.color.r, floor.color.g, floor.color.b, 0.22f);
                    floor.preserveAspect = false;
                    float fw = gw * (one ? 0.64f : 0.88f);
                    UiKit.Anchor(floor.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -totalH * 0.5f + PetSkillStyle.Rem(1.4f)), fw, fw / (one ? 2.6f : 2.5f));
                    floor.transform.SetAsFirstSibling();
                }
            }
            for (int i = 0; i < n; i++)
            {
                Entry e = entries[i];
                bool heroic = i == heroIdx;
                bool ownRow = heroic && heroOwnRow;
                int k = ownRow ? 0 : i;
                float x, y, cw;
                if (ownRow)
                {
                    cw = heroOrb;
                    x = (gw - cw) * 0.5f;
                    y = gridH + gapY + PetSkillStyle.Px("sr_hero_top_rem");
                }
                else
                {
                    int rowN = Mathf.Min(cols, normal - (k / cols) * cols);
                    float rowW = rowN * cellW + (rowN - 1) * gapX;
                    cw = cellW;
                    x = (gw - rowW) * 0.5f + (k % cols) * (cellW + gapX);
                    y = (k / cols) * (cellH + gapY);
                }
                bool peer = heroIdx >= 0 && !heroic && e.Rarity == best;
                cells.Add(BuildCell(grid, e, i, x, y, cw, heroic, peer, dense, one));
            }

            // ---- 섬광 ----
            flash = UiKit.Panel(c, "sr-flash", "pp_line");
            flash.color = new Color(1f, 1f, 1f, 0f);
            flash.raycastTarget = false;

            // ---- 시각표(원작 tickSummonResult 의 지연) ----
            float charge = PetSkillStyle.L("sr_charge_ms"), slow = PetSkillStyle.L("sr_slow_step_ms"), rowMs = PetSkillStyle.L("sr_row_ms"), stag = PetSkillStyle.L("sr_row_stag_ms"), pause = PetSkillStyle.L("sr_tier_pause_ms"), hold = PetSkillStyle.L("sr_holdback_ms");
            if (n <= 10)
                for (int i = 0; i < n; i++) delays.Add(charge + i * slow + (holdback && i == n - 1 ? hold : 0f));
            else
            {
                float acc = 0f;
                for (int i = 0; i < n; i++)
                {
                    bool boundary = i > 0 && RarityIdx(entries[i].Rarity) > RarityIdx(entries[i - 1].Rarity);
                    if (boundary) acc += pause;
                    delays.Add(charge + Mathf.Floor(i / (float)cols) * rowMs + (i % cols) * stag + acc + (holdback && i == n - 1 ? hold : 0f));
                }
            }
            var sc = PetSkillHost.SfxSummonCharge;
            if (sc != null) sc(best);
            start = Time.unscaledTime;
            idx = 0;
            done = false;
        }

        Cell BuildCell(RectTransform grid, Entry e, int i, float x, float y, float cw, bool heroic, bool peer, bool dense, bool one)
        {
            int tier = RarityIdx(e.Rarity);
            Color rc = PetSkillStyle.Rarity(Defs, e.Rarity);
            float sub = UiCatalog.Instance.Kind(TextKind.Sub).size;
            float nameH = dense ? 0f : sub * PetSkillStyle.L("sr_name_h_em") * 0.62f;
            float subH = dense ? 0f : sub * 1.35f;
            float ch = cw + (dense ? 0f : PetSkillStyle.Px("sr_name_mt_rem") + nameH + PetSkillStyle.Px("sr_sub_mt_rem") + subH);
            RectTransform cell = UiKit.Box(grid, "sr-cell-" + i);
            UiKit.Place(cell, x, y, cw, ch);
            var cg = cell.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            var c = new Cell { Root = cell, Group = cg, Entry = e, Heroic = heroic };
            c.BaseScale = heroic ? 1f : PetSkillStyle.L("sr_sz_" + Mathf.Clamp(tier, 0, 5)) * (peer ? PetSkillStyle.L("sr_peer_sz") : 1f);
            c.Pop = PetSkillStyle.L("sr_pop_" + Mathf.Clamp(tier, 0, 5));
            // orbwrap
            RectTransform wrap = UiKit.Box(cell, "sr-orbwrap");
            UiKit.Place(wrap, 0f, 0f, cw, cw);
            wrap.pivot = new Vector2(0.5f, 0.5f);
            wrap.anchoredPosition = new Vector2(cw * 0.5f, -cw * 0.5f);
            c.OrbWrap = wrap;
            // 광채(고등급) · 그림자 · 구체 · 하이라이트
            if (Hi(e.Rarity) || peer)
            {
                Image glow = PetSkillKit.Disc(wrap, "glow", rc);
                glow.color = new Color(rc.r, rc.g, rc.b, 0.35f + 0.1f * tier);
                float gs = cw * (1.25f + 0.1f * tier);
                UiKit.Anchor(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, gs, gs);
            }
            Image shadow = PetSkillKit.Disc(wrap, "shadow", PetSkillStyle.C("black"));
            shadow.color = new Color(0f, 0f, 0f, 0.5f);
            shadow.preserveAspect = false;
            UiKit.Anchor(shadow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, -cw * 0.02f), cw * 0.74f, cw * 0.13f);
            Image deep = PetSkillKit.Disc(wrap, "sr-orb-deep", Color.Lerp(rc, PetSkillStyle.C("sr_orb_deep"), 0.62f));
            UiKit.Fill(deep.rectTransform);
            Image orb = PetSkillKit.Disc(wrap, "sr-orb", tier <= 1 ? Color.Lerp(rc, PetSkillStyle.C("black"), 0.3f) : rc);
            orb.rectTransform.offsetMin = new Vector2(cw * 0.04f, cw * 0.09f);
            orb.rectTransform.offsetMax = new Vector2(-cw * 0.09f, -cw * 0.04f);
            Image hi = PetSkillKit.Disc(wrap, "sr-hilite", PetSkillStyle.C("sr_hilite"));
            UiKit.Anchor(hi.rectTransform, new Vector2(0.36f, 0.81f), new Vector2(0.5f, 0.5f), Vector2.zero, cw * 0.28f, cw * 0.2f);
            // 아이콘(슬롯의 그림)
            float isz = cw * (one ? 0.62f : 0.6f);
            if (!string.IsNullOrEmpty(e.FaceName))
            {
                RectTransform pf = PetSkillKit.PetFace(wrap, Defs, e.FaceName, isz, e.FaceKind);
                UiKit.Anchor(pf, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, isz, isz);
            }
            else
            {
                Image ico = UiKit.Icon(wrap, "sr-ico", e.IconKey, e.IconTint);
                UiKit.Anchor(ico.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, isz, isz);
            }
            if (!dense)
            {
                float bh = sub * 1.2f;
                if (e.Qty > 1)
                {
                    string q = PetSkillStyle.T("sr_qty", e.Qty);
                    float bw = PetSkillKit.TextWidth(TextKind.Sub, q) + PetSkillStyle.Rem(0.6f);
                    RectTransform qb = PetSkillKit.Framed(wrap, "sr-qty", PetSkillStyle.C("sr_badge_bg"), PetSkillStyle.Px("sr_qty_r_rem"), PetSkillStyle.L("line1_px"));
                    ((Image)qb.Find("line").GetComponent<Image>()).color = rc;
                    UiKit.Anchor(qb, new Vector2(0.95f, 0.06f), new Vector2(1f, 0f), Vector2.zero, bw, bh);
                    TextMeshProUGUI qt = PetSkillKit.Text(qb, "t", TextKind.Sub, q, PetSkillStyle.C("white"));
                    UiKit.Fill(qt.rectTransform);
                }
                if (!string.IsNullOrEmpty(e.Extra))
                {
                    float bw = PetSkillKit.TextWidth(TextKind.Sub, e.Extra) + PetSkillStyle.Rem(0.6f);
                    RectTransform db = PetSkillKit.Framed(wrap, "sr-dup", PetSkillStyle.C("sr_badge_bg"), PetSkillStyle.Px("sr_qty_r_rem"), PetSkillStyle.L("line1_px"));
                    ((Image)db.Find("line").GetComponent<Image>()).color = PetSkillStyle.C("sr_hilite");
                    UiKit.Anchor(db, new Vector2(0.06f, 0.93f), new Vector2(0f, 1f), Vector2.zero, bw, bh);
                    TextMeshProUGUI dt = PetSkillKit.Text(db, "t", TextKind.Sub, e.Extra, PetSkillStyle.C("white"));
                    UiKit.Fill(dt.rectTransform);
                }
                if (e.IsNew)
                {
                    string nw = PetSkillStyle.T("sr_new");
                    float bw = PetSkillKit.TextWidth(TextKind.Sub, nw) + PetSkillStyle.Rem(0.52f);
                    RectTransform nb = PetSkillKit.Framed(wrap, "sr-new", PetSkillStyle.C("sr_new"), PetSkillStyle.Px("sr_new_r_rem"), PetSkillKit.Line2);
                    UiKit.Anchor(nb, new Vector2(0.93f, 0.92f), new Vector2(1f, 1f), Vector2.zero, bw, bh);
                    TextMeshProUGUI nt = PetSkillKit.Text(nb, "t", TextKind.Sub, nw, PetSkillStyle.C("white"));
                    LetterSpacing.Apply(nt, "sr_new_ls_em");   // T168 3회차 — 정본 6980 `.sr-new`
                    UiKit.Fill(nt.rectTransform);
                }
                // 이름판 · 등급 칩
                float ny = cw + PetSkillStyle.Px("sr_name_mt_rem");
                float nw2 = cw * PetSkillStyle.L("sr_name_w_f");
                RectTransform nameBox = UiKit.Box(cell, "sr-name");
                UiKit.Place(nameBox, (cw - nw2) * 0.5f, ny, nw2, nameH);
                PetSkillKit.Fill(nameBox, "bg", PetSkillStyle.C("sr_name_bg"), PetSkillStyle.Px("sr_name_r_rem"));
                TextMeshProUGUI nt2 = PetSkillKit.Text(nameBox, "t", TextKind.Sub, e.Name, PetSkillStyle.C("white"));
                UiKit.Fill(nt2.rectTransform);
                float sy = ny + nameH + PetSkillStyle.Px("sr_sub_mt_rem");
                float rkW = PetSkillKit.TextWidth(TextKind.Sub, e.Sub) + PetSkillStyle.Px("sr_rk_pad_x_rem") * 2f;
                RectTransform rk = UiKit.Box(cell, "sr-sub");
                UiKit.Place(rk, (cw - rkW) * 0.5f, sy, rkW, subH);
                PetSkillKit.Fill(rk, "bg", rc, PetSkillStyle.Px("sr_rk_r_rem"));
                TextMeshProUGUI rt = PetSkillKit.Text(rk, "t", TextKind.Sub, e.Sub, ChipInk(rc));
                LetterSpacing.Apply(rt, "sr_sub_ls_em");   // T168 3회차 — 정본 7059 `.sr-sub`
                UiKit.Fill(rt.rectTransform);
            }
            cell.localScale = Vector3.one * 0.35f;
            return c;
        }

        /// <summary>원작 chipFill — 등급색 필 위에 검정/흰색 중 대비 큰 쪽.</summary>
        static Color ChipInk(Color bg)
        {
            float lum = 0.2126f * bg.r + 0.7152f * bg.g + 0.0722f * bg.b;
            return lum > 0.5f ? PetSkillStyle.C("ink") : PetSkillStyle.C("white");
        }

        void BuildFoot(float footH, float fw)
        {
            float sub = UiCatalog.Instance.Kind(TextKind.Sub).size;
            float gap = PetSkillStyle.Px("sr_foot_gap_rem");
            float okW = PetSkillStyle.Px("sr_ok_w_rem"), okH = PetSkillStyle.Px("sr_ok_h_rem");
            // 집계 칩(원작 summonSummary · done 에서만)
            chips = UiKit.Box(foot, "sr-sum").gameObject;
            var cnt = new Dictionary<string, int>();
            foreach (Entry e in rollList) { int v; cnt.TryGetValue(e.Rarity, out v); cnt[e.Rarity] = v + 1; }
            float chipH = sub * 1.5f;
            var list = new List<KeyValuePair<string, int>>();
            foreach (string r in Defs.Rarities) if (cnt.ContainsKey(r)) list.Add(new KeyValuePair<string, int>(r, cnt[r]));
            float x = 0f, total = 0f;
            var widths = new List<float>();
            foreach (var kv in list)
            {
                float w = PetSkillKit.TextWidth(TextKind.Sub, PetSkillStyle.T("sr_chip", Defs.RarityKr.Get(kv.Key, kv.Key), kv.Value)) + PetSkillStyle.Px("sr_chip_pad_x_rem") * 2f;
                widths.Add(w);
                total += w + PetSkillStyle.Px("sr_chip_gap_rem");
            }
            x = (fw - total) * 0.5f;
            RectTransform chipRow = (RectTransform)chips.transform;
            UiKit.Place(chipRow, 0f, footH - okH - gap - chipH - gap, fw, chipH);
            for (int i = 0; i < list.Count; i++)
            {
                Color rc = PetSkillStyle.Rarity(Defs, list[i].Key);
                RectTransform chip = UiKit.Box(chipRow, "sr-chip-" + list[i].Key);
                UiKit.Place(chip, x, 0f, widths[i], chipH);
                PetSkillKit.Fill(chip, "bg", rc, PetSkillStyle.Px("sr_chip_r_rem"));
                TextMeshProUGUI t = PetSkillKit.Text(chip, "t", TextKind.Sub, PetSkillStyle.T("sr_chip", Defs.RarityKr.Get(list[i].Key, list[i].Key), list[i].Value), ChipInk(rc));
                UiKit.Fill(t.rectTransform);
                x += widths[i] + PetSkillStyle.Px("sr_chip_gap_rem");
            }
            chips.SetActive(false);
            // 힌트
            string ht = PetSkillStyle.T("sr_hint");
            float hw = PetSkillKit.TextWidth(TextKind.Sub, ht) + PetSkillStyle.Px("sr_hint_pad_x_rem") * 2f;
            RectTransform hintBox = PetSkillKit.Framed(foot, "sr-hint", new Color(0f, 0f, 0f, 0f), PetSkillStyle.Px("sr_hint_r_rem"), PetSkillStyle.L("line1_px"));
            ((Image)hintBox.Find("line").GetComponent<Image>()).color = new Color(1f, 1f, 1f, 0.22f);
            ((Image)hintBox.Find("face").GetComponent<Image>()).color = PetSkillStyle.C("sr_bg_d");
            UiKit.Anchor(hintBox, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, (okH - sub * 1.6f) * 0.5f), hw, sub * 1.6f);
            TextMeshProUGUI hint2 = PetSkillKit.Text(hintBox, "t", TextKind.Sub, ht, PetSkillStyle.C("sr_hint"), TextAlignmentOptions.Center, false);
            UiKit.Fill(hint2.rectTransform);
            hint = hintBox.gameObject;
            // 확인(금색 · done 에서만)
            OkButton = UiKit.Button(foot, "sr-ok", () => Close());
            RectTransform okr = OkButton.GetComponent<RectTransform>();
            UiKit.Anchor(okr, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, okW, okH);
            RectTransform okSkin = PetSkillKit.Framed(okr, "skin", PetSkillStyle.C("sr_ok_ink"), PetSkillStyle.Px("sr_ok_r_rem"), PetSkillKit.Line2);
            UiKit.Fill(okSkin);
            Image okFace = PetSkillKit.Fill(okSkin, "top", PetSkillStyle.C("sr_ok"), PetSkillStyle.Px("sr_ok_r_rem") - PetSkillKit.Line2);
            okFace.rectTransform.offsetMin = new Vector2(PetSkillKit.Line2, PetSkillKit.Line2 + PetSkillStyle.Px("sr_ok_inset_rem"));
            okFace.rectTransform.offsetMax = new Vector2(-PetSkillKit.Line2, -PetSkillKit.Line2);
            TextMeshProUGUI okt = PetSkillKit.Text(okr, "t", TextKind.Button, PetSkillStyle.T("sr_ok"), PetSkillStyle.C("sr_ok_ink"));
            LetterSpacing.Apply(okt, "sr_ok_ls_em");   // T168 3회차 — 정본 7139 `.sr-ok`
            UiKit.Fill(okt.rectTransform);
            ok = okr.gameObject;
            ok.SetActive(false);
            // x1 요약(원작 summonSoloInfo · done 에서만)
            if (entries.Count == 1)
            {
                Entry e = entries[0];
                string line, own;
                if (kind == "skill")
                {
                    string id = e.Key.Substring(3);
                    SkillEntry st = PetSkillHost.Instance.Skills.State.Get(id);
                    line = PetSkillStyle.T(e.IsNew ? "sr_solo_skill_new" : "sr_solo_skill_dup");
                    own = PetSkillStyle.T("sr_solo_skill_own", st != null ? st.Level : 1, st != null ? st.Dupes : 0);
                }
                else
                {
                    int eggs = 0;
                    foreach (Egg g in PetSkillHost.Instance.Pets.State.Eggs) if (g.Rarity == e.Rarity) eggs++;
                    line = PetSkillStyle.T("sr_solo_egg");
                    own = PetSkillStyle.T("sr_solo_egg_own", Defs.RarityKr.Get(e.Rarity, e.Rarity), eggs);
                }
                float sg = PetSkillStyle.Px("sr_solo_gap_rem");
                float aw = PetSkillStyle.Px("sr_again_w_rem"), ah = PetSkillStyle.Px("sr_again_h_rem");
                float soloH = sub * 1.3f + sg + sub * 1.5f + sg + ah;
                RectTransform sb = UiKit.Box(foot, "sr-solo");
                UiKit.Place(sb, 0f, footH - okH - gap - soloH, fw, soloH);
                TextMeshProUGUI lt = PetSkillKit.Text(sb, "line", TextKind.Sub, line, PetSkillStyle.C("white"));
                UiKit.Place(lt.rectTransform, 0f, 0f, fw, sub * 1.3f);
                float ow = PetSkillKit.TextWidth(TextKind.Sub, own) + PetSkillStyle.Rem(1.4f);
                RectTransform ob = PetSkillKit.Framed(sb, "own", PetSkillStyle.C("sr_solo_own"), PetSkillStyle.Px("sr_solo_own_r_rem"), PetSkillStyle.L("line1_px"));
                ((Image)ob.Find("line").GetComponent<Image>()).color = new Color(0.47f, 0.55f, 0.78f, 0.35f);
                UiKit.Place(ob, (fw - ow) * 0.5f, sub * 1.3f + sg, ow, sub * 1.5f);
                TextMeshProUGUI ot = PetSkillKit.Text(ob, "t", TextKind.Sub, own, new Color(0.84f, 0.88f, 0.97f, 0.85f), TextAlignmentOptions.Center, false);
                UiKit.Fill(ot.rectTransform);
                AgainButton = UiKit.Button(sb, "sr-again", () => { Action a = repeat; Close(); if (a != null) a(); });
                RectTransform ar = AgainButton.GetComponent<RectTransform>();
                UiKit.Place(ar, (fw - aw) * 0.5f, sub * 1.3f + sg + sub * 1.5f + sg, aw, ah);
                RectTransform askin = PetSkillKit.Framed(ar, "skin", PetSkillStyle.C("sr_again"), PetSkillStyle.Px("sr_again_r_rem"), PetSkillStyle.L("line1_px"));
                UiKit.Fill(askin);
                ((Image)askin.Find("line").GetComponent<Image>()).color = new Color(0.59f, 0.67f, 0.92f, 0.55f);
                TextMeshProUGUI at = PetSkillKit.Text(ar, "t", TextKind.Sub, PetSkillStyle.T("sr_again"), PetSkillStyle.C("sr_again_ink"));
                LetterSpacing.Apply(at, "sr_again_ls_em");   // T168 3회차 — 정본 5791 `.sr-again`
                UiKit.Fill(at.rectTransform);
                solo = sb.gameObject;
                solo.SetActive(false);
            }
        }

        // ===== 시계(원작 tickSummonResult · rAF) =====
        void Update()
        {
            if (!done)
            {
                float elapsed = (Time.unscaledTime - start) * 1000f;
                string loud = null;
                while (idx < cells.Count && delays[idx] <= elapsed)
                {
                    TurnOn(cells[idx]);
                    if (loud == null || RarityIdx(cells[idx].Entry.Rarity) > RarityIdx(loud)) loud = cells[idx].Entry.Rarity;
                    idx++;
                }
                if (loud != null)
                {
                    var sr = PetSkillHost.SfxSummonReveal;
                    if (sr != null) sr(loud);
                    if (idx >= cells.Count && heroIdx >= 0) FireHero();
                }
                if (idx >= cells.Count && elapsed >= delays[delays.Count - 1] + PetSkillStyle.L("sr_tail_ms")) Finish();
            }
            AnimateCells();
            AnimateFlash();
        }

        void TurnOn(Cell c)
        {
            if (c.On) return;
            c.On = true;
            c.OnAt = Time.unscaledTime;
        }

        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            t = Mathf.Clamp01(t);
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        /// <summary>셀 팝(원작 srpop · 스케일 .35→1 오버슛) · 끝난 뒤에는 숨쉬기(srbreath).</summary>
        void AnimateCells()
        {
            float tt = Time.unscaledTime;
            for (int i = 0; i < cells.Count; i++)
            {
                Cell c = cells[i];
                if (!c.On) continue;
                float t = (tt - c.OnAt) / c.Pop;
                float s = t >= 1f ? 1f : Mathf.Lerp(0.35f, 1f, EaseOutBack(t));
                c.Group.alpha = Mathf.Clamp01(t * 3f);
                c.Root.localScale = Vector3.one * s;
                float b = done && t >= 1f ? 1f + 0.025f * Mathf.Sin((tt - i * 0.21f) * 2.4f) : 1f;
                c.OrbWrap.localScale = Vector3.one * c.BaseScale * (c.Heroic && heroFired ? 1.18f : 1f) * b;
            }
        }

        void FireHero()
        {
            if (heroFired) return;
            heroFired = true;
            flash.color = PetSkillStyle.Rarity(Defs, best);
            flashAt = Time.unscaledTime;
            var g = PetSkillHost.SfxGacha;
            if (g != null) g(best);
        }

        void AnimateFlash()
        {
            if (flashAt < 0f || flash == null) return;
            float t = (Time.unscaledTime - flashAt) / PetSkillStyle.L("sr_flash_sec");
            float a = holdback ? Mathf.Clamp01(1f - t) * 0.85f : Mathf.Clamp01(1f - t) * 0.45f;
            Color c = flash.color;
            flash.color = new Color(c.r, c.g, c.b, a);
            if (t >= 1f) flashAt = -1f;
        }

        void Finish()
        {
            done = true;
            if (hint != null) hint.SetActive(false);
            if (ok != null) ok.SetActive(true);
            if (chips != null && rolls > 1) chips.SetActive(true);
            if (solo != null) solo.SetActive(true);
        }

        /// <summary>오버레이 탭(원작 onSummonResultTap): 연출 중이면 스킵(전부 즉시) · 끝났으면 닫기.</summary>
        public void OnTap()
        {
            if (done) { Close(); return; }
            foreach (Cell c in cells) TurnOn(c);
            idx = cells.Count;
            if (heroIdx >= 0) FireHero();
            Finish();
        }

        public void Close()
        {
            if (Current == this) Current = null;
            if (sheet != null) sheet.Modal.Close(handle);
        }

        void OnDestroy() { if (Current == this) Current = null; }
    }
}
