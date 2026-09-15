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
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 소환 결과 연출 팝업(원작 ui.js openSummonResult · summonEntries · groupSummonEntries · tickSummonResult · fireSummonHero · finishSummonResult · onSummonResultTap · summonSoloInfo · buildSummonReflection · 스킬·펫·탈것 공용).
    /// 원작 타임라인 그대로: ① 빛 모임(SR_CHARGE 240ms) → ② 등급 오름차순으로 셀 팝(≤10 = 125ms 간격 · &gt;10 = 행 웨이브 300/40ms + 등급 경계 200ms 정지) →
    /// ③ 최고 등급(전설↑ · 단독)은 150ms 홀드백 뒤 주역 비트 + 화면 섬광 → ④ 150ms 여운 뒤 [확인] · 등급 집계 칩 · x1 은 요약+[다시 소환]. 탭 = 스킵 · 끝난 뒤 탭 = 닫기.
    /// 11개부터 같은 항목을 한 셀로 묶는다(×N · 조각 +N · NEW). 그림 = 슬롯의 그림(스킬 sk_* · 알 등급색 egg · summon-result-image-unify).
    /// 광채·소환진·먼지는 CSS 다층 그라데이션이라 정점 색 원판·섬광·바닥 타원으로 줄였다(결정 기록 · 주인 눈 확인). 천개·광선·별·바닥 반사는 T179 가 <see cref="SummonFx"/> 로 세운다(무대판). 효과음은 <see cref="PetSkillHost"/> 훅(T30).
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
            /// <summary>T334 3회차 ⓑ — 흡기 전 제자리(anchoredPosition)와 «슬롯 → 광원» 벡터(정본 `--dx/--dy`).</summary>
            public Vector2 Home, ToLight;
            /// <summary>T334 5회차 — 아이들 호흡 전 구체 래퍼의 제자리.</summary>
            public Vector2 OrbHome;
            /// <summary>T334 7회차 — 물러남 전 이 셀 그림들의 제 색(한 번만 담는다).</summary>
            public Graphic[] Tint;
            public Color[] TintHome;
            /// <summary>T334 12회차 — 착지 스파크(정본 `.sr-spark`) — 구체 래퍼 한가운데에 겹친다.</summary>
            public Image Spark;
            /// <summary>T334 15회차 — 비행 잔상(정본 `.sr-ghost`) — 구체 **뒤**(z 0)에 깔리는 흐린 복제.</summary>
            public Image Ghost;
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
        /// <summary>T334 2회차 — 정본 `tickSummonResult` 의 상태 기계(Core · UnityEngine 0). 시각·판정은 전부 이것이 쥔다.</summary>
        SummonSeqRun seq;
        SummonFx fx;   // T179 연출 겹(무대판에서만)
        public SummonFx Fx { get { return fx; } }

        // ── T334 2회차 — 정본 다섯 상태를 그대로 연다(3회차가 이 위에 겹을 얹는다) ──
        /// <summary>`charging` — 홀드백에서 마지막 한 칸을 남긴 구간(정지가 아니라 축적).</summary>
        public bool Charging { get { return seq != null && seq.Charging; } }
        /// <summary>`hero` — 주역(최고 등급) 셀이 착지했다.</summary>
        public bool Hero { get { return seq != null && seq.Hero; } }
        /// <summary>`flash` — 뜸들인 단독 등장의 전 화면 섬광(홀드백일 때만).</summary>
        public bool Flash { get { return seq != null && seq.Flash; } }
        /// <summary>`wipe` — 홀드백이 없는 주역의 셀 중심 가산 원형 와이프(대량 소환).</summary>
        public bool Wipe { get { return seq != null && seq.Wipe; } }
        // 주역 비트(`HeroKicking`)는 아직 안 연다 — 그 길이가 3회차에 정해진다(위 Open 의 주석).
        int heroIdx = -1;
        string best;
        Action repeat;
        Image flash;
        /// <summary>T334 3회차 — 주역 와이프(정본 `.sr-wipe` · 홀드백이 **없는** 주역에서만). 가산 혼합이라 씬을 안 죽인다.</summary>
        Image wipe;
        float wipeAt = -1f;
        /// <summary>T334 6회차 — 화면 킥(정본 `srshakehit`)이 시작한 시각 · 흔들 판과 그 제자리.</summary>
        float kickAt = -1f;
        /// <summary>T334 9회차 — 주역 충격파 링(정본 `.sr-cell.heroic::after`)과 그 최대 배율(정본 `--ringmax` · 배치마다 다르다).</summary>
        Image heroRing;
        float ringMax = 3.2f;
        /// <summary>T334 10회차 — 주역 광창(정본 `.sr-beam`) · 클론에 없던 겹이다.</summary>
        Image heroBeam;
        /// <summary>T334 11회차 — 셀마다 하나씩 광원 자리에서 켜지는 재점화 플래시(정본 `.sr-relight`).</summary>
        readonly List<Image> relights = new List<Image>();
        /// <summary>주역이 착지한 벽시계 시각(물러남·킥이 같이 쓴다).</summary>
        float heroAtWall = -1f;
        RectTransform wrap;
        Vector2 wrapHome;
        float flashAt = -1f;
        /// <summary>T334 3회차 ⓑ — 충전 구간이 움직이는 것들: 소환진·중앙 광원·비네트(정본 `.sr-floor`·`.sr-halo`·`.sr-wrap::before`).</summary>
        Image floorImg, haloImg, vigImg;
        /// <summary>T334 4회차 — 소환진의 룬 눈금 띠(정본 `.sr-floor::after`). 충전 중엔 `steps(9)` 로 점등하고 그 밖에는 느리게 호흡한다.</summary>
        Image tickImg;
        Color tickBase;
        Color floorBase, haloBase;
        Vector3 floorHome;
        float chargeAt = -1f;
        RectTransform foot;
        GameObject hint, ok, chips, solo;
        string kind;

        public bool Done { get { return done; } }
        public int CellCount { get { return cells.Count; } }
        public int OnCount { get { int n = 0; foreach (Cell c in cells) if (c.On) n++; return n; } }
        // ── T334 3회차 ⓑ — 충전 겹을 자가 수로 잰다(정지 프레임이 없다는 것은 «움직였다» 로만 증명된다) ──
        /// <summary>비네트(`.sr-wrap::before`)의 지금 불투명도.</summary>
        public float VigAlpha { get { return vigImg != null ? vigImg.color.a : -1f; } }
        /// <summary>소환진(`.sr-floor`)의 지금 배율(제자리 대비).</summary>
        public float FloorScale { get { return floorImg != null && floorHome.x != 0f ? floorImg.rectTransform.localScale.x / floorHome.x : -1f; } }
        /// <summary>
        /// 소환진(`.sr-floor`)의 지금 밝기 — 세 성분의 **합**이다.
        ///
        /// ⚠ 최댓값으로 재면 안 된다(런 512 가 가르친 것): 등급색이 `#ff1c1c`(ultimate)처럼 **한 성분이 이미 1.0** 이면
        ///    CSS `filter: brightness()` 도 거기서 잘려 그 성분은 안 움직인다 — 오르는 것은 나머지 두 성분이고,
        ///    화면에서는 «붉은 판이 흰 쪽으로 씻긴다» 로 보인다. 합은 그 두 갈래를 다 담는다.
        /// </summary>
        public float FloorBright { get { if (floorImg == null) return -1f; Color c = floorImg.color; return c.r + c.g + c.b; } }
        /// <summary>주역 충격파 링 — 자가 본다.</summary>
        public Image HeroRing { get { return heroRing; } }
        /// <summary>그 링의 최대 배율(정본 `--ringmax` · 배치마다 다르다).</summary>
        public float RingMax { get { return ringMax; } }

        /// <summary>룬 눈금 띠(정본 `.sr-floor::after`)의 지금 불투명도 — 없으면 −1.</summary>
        public float TickAlpha { get { return tickImg != null ? tickImg.color.a : -1f; } }
        /// <summary>룬 눈금 띠가 구운 판을 쥐고 있는가(원판을 늘려 쓰면 눈금 굵기가 각도마다 달라진다).</summary>
        public bool TickBaked { get { return tickImg != null && tickImg.sprite != null; } }

        /// <summary>소환진의 지금 색 — 등급색으로 물들었는지 자가 본다(정본은 `.done` 에서만 물든다).</summary>
        public Color FloorColor { get { return floorImg != null ? floorImg.color : Color.clear; } }

        /// <summary>중앙 광원(`.sr-halo`)의 지금 불투명도.</summary>
        public float HaloAlpha { get { return haloImg != null ? haloImg.color.a : -1f; } }
        /// <summary>셀 하나가 제자리에서 광원 쪽으로 빨려든 거리(흡기 · 정본 `srinhale`).</summary>
        public float CellPulledIn(int i) { return i < 0 || i >= cells.Count ? -1f : (cells[i].Root.anchoredPosition - cells[i].Home).magnitude; }

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
        /// <summary>순위 → 등급 이름(T334 2회차 — 상태 기계가 돌려주는 «이번 프레임 최고 순위» 를 소리 이름으로).</summary>
        static string RarityOf(int rank) { return rank >= 0 && rank < Defs.Rarities.Length ? Defs.Rarities[rank] : null; }
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
            wrap = c; wrapHome = c.anchoredPosition;   // 정본 `.sr-wrap` — 주역 착지에 이 판이 흔들린다

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
            haloImg = halo; haloBase = halo.color;
            // 비네트(정본 `.sr-wrap::before` z 2) — 고정 그라디언트 한 장, 움직이는 것은 불투명도·배율뿐.
            RectTransform vigRt = UiKit.Box(c, "sr-vig");
            UiKit.Fill(vigRt);
            vigImg = vigRt.gameObject.AddComponent<Image>();
            vigImg.sprite = SummonFx.BakeVig("sr-vig");
            vigImg.preserveAspect = false;
            vigImg.raycastTarget = false;
            vigImg.color = new Color(1f, 1f, 1f, 0f);

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
                    // 정본 `.sr-floor`(style.css 5809~5812)의 기본 바탕은 **푸른색**이고, 등급색으로 물드는 것은
                    // `#summon-result-modal.done` 한 줄(7182~7185)에서다 — ui.js 548 주석이 까닭을 댄다:
                    // «원색 그대로는 배경에 묻혀 소환진이 사라진다». 처음부터 등급 원색으로 칠하면 `charging` 의
                    // 밝기 램프도 화면에서 안 읽힌다(궁극의 #ff1c1c 는 빨강이 이미 1.0 이라 CSS 도 거기서 자른다).
                    Image floor = PetSkillKit.Disc(body, "sr-floor", SummonFxStyle.C("floor_fill"));
                    floor.preserveAspect = false;
                    float fw = gw * (one ? 0.64f : 0.88f);
                    UiKit.Anchor(floor.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -totalH * 0.5f + PetSkillStyle.Rem(1.4f)), fw, fw / (one ? 2.6f : 2.5f));
                    floor.transform.SetAsFirstSibling();
                    floorImg = floor; floorBase = floor.color; floorHome = floor.rectTransform.localScale;
                    // 룬 눈금 띠 — 소환진 위에 같은 상자로 얹는다(정본은 `::after` 라 같은 자리·같은 크기다).
                    RectTransform tickRt = UiKit.Box(floor.rectTransform, "sr-floor-ticks");
                    UiKit.Fill(tickRt);
                    tickImg = tickRt.gameObject.AddComponent<Image>();
                    tickImg.sprite = SummonFx.BakeFloorTicks("sr-floor-ticks", fw, fw / (one ? 2.6f : 2.5f));
                    tickImg.preserveAspect = false;
                    tickImg.raycastTarget = false;
                    tickBase = SummonFxStyle.C("floor_line");
                    tickImg.color = tickBase;
                    // T179 — 연출 겹(정본 sr-canopy 아치+빛발+스필 · sr-rays · sr-stars · ui.js 491~494: canopy = stage · compact = herorow) — 그리드 위 밴드·배경·별
                    fx = SummonFx.Build(body, floor.rectTransform, (bodyH - totalH) * 0.5f, gw, one, heroRow);
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
            // T179 3회차 — 바닥 반사의 원본(정본 buildSummonReflection: done 에서 이 그리드를 복제) · 셀별 구체 배율(--sz · heroic 확대는 복제본에서 뗀다)
            if (fx != null)
            {
                var orbSz = new List<float>(cells.Count);
                foreach (Cell cc in cells) orbSz.Add(cc.BaseScale);
                fx.SetReflectSource(grid, orbSz);
            }

            // ---- 셀별 광원 재점화(정본 `.sr-relights`/`.sr-relight` 6364~6389) ----
            // 정본 실측이 이 겹의 근거다: 2~4번 셀이 사출되는 900ms 내내 광원 ±20px 평균 휘도가 45.1~55.2 로
            // **시작 프레임 baseline 53.3 보다도 낮았다** — 빛 → 아이템의 인과가 첫 셀과 주역에만 걸려 있었다.
            // 그래서 셀이 뜰 때마다 광원 자리(= `.sr-wrap` 중심)에 그 셀 등급색으로 짧은 플래시를 한 번 켠다.
            // ⚠ 정본 주석: 이 층을 **셀 안에 넣으면 안 된다** — 셀의 배율·비행 이동·`opacity:0` 이 전부 곱해져
            //    광원에 서지도 제 밝기로 켜지지도 않는다. 그래서 여기서도 셀이 아니라 판(`body`)에 붙이고
            //    자리는 `wrap` 의 한가운데를 그대로 받는다(정본 «광원 좌표가 그냥 가운데»).
            // ⚠ 사다리(정본 z): 소환진 10 · 천개 20 · **재점화 26** · 충격파 30 · 격자 40 — 그래서 격자 **바로 아래**에 끼운다.
            //    (`SummonFx.Build` 의 겹들이 뒤에 붙으므로 여기서 자리를 정해야 한다.)
            if (grid != null && cells.Count > 0)
            {
                SummonRelightSpec rsp = SummonFxStyle.Relight;
                float rw = Mathf.Min(PetSkillStyle.Rem((float)rsp.WRem), (float)rsp.WVwF * W);
                RectTransform gp = (RectTransform)grid.parent;   // 넘치는 판은 그리드가 스크롤 뷰포트 안에 있다
                RectTransform host = UiKit.Box(gp, "sr-relights");
                UiKit.Fill(host);
                host.SetSiblingIndex(grid.GetSiblingIndex());   // 언제나 그리드 **바로 아래**
                for (int i = 0; i < cells.Count; i++)
                {
                    Entry e = cells[i].Entry;
                    Color rc = PetSkillStyle.Rarity(Defs, e.Rarity);
                    int tier = RarityIdx(e.Rarity);
                    double amt = SummonFxStyle.Hero.HiliteAmount(rc.r * 255.0, rc.g * 255.0, rc.b * 255.0, tier);
                    Color lite = Shade(rc, (float)amt);   // 정본 `--rc-lite` = srHilite(rc, tier)
                    RectTransform rt = UiKit.Box(host, "sr-relight");
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(rw, rw);
                    rt.anchoredPosition = Vector2.zero;
                    rt.position = wrap.TransformPoint(wrap.rect.center);   // 광원 = wrap 한가운데
                    Image ri = rt.gameObject.AddComponent<Image>();
                    ri.raycastTarget = false;
                    ri.preserveAspect = false;
                    ri.sprite = SummonFx.BakeRelight(
                        "sr-relight-" + ColorUtility.ToHtmlStringRGB(rc) + "-" + ColorUtility.ToHtmlStringRGB(lite), rc, lite);
                    Material rmat = CraftFxPoly.Screen();   // ⚠ 가산이라야 이웃을 «밝힌다»(정본 주석 · 알파 오버레이는 탁하게 죽인다)
                    if (rmat != null) ri.material = rmat;
                    ri.color = new Color(1f, 1f, 1f, 0f);
                    ri.rectTransform.localScale = Vector3.one * (float)SummonFxStyle.Relight.Relight.Sample(0, "scale", null);
                    relights.Add(ri);
                }
            }

            // ---- 섬광 ----
            flash = UiKit.Panel(c, "sr-flash", "pp_line");
            flash.color = new Color(1f, 1f, 1f, 0f);
            flash.raycastTarget = false;

            // ---- 주역 충격파 링(정본 `.sr-cell.heroic::after` 6754~6764) ----
            // 셀 **뒤**(z-index −1)에 폭 100% 정사각으로, 셀 위 가운데에 깔린다. 가산 혼합(screen)이라 와이프와 같은 재질.
            // 최대 배율은 배치마다 다르다(정본 `--ringmax`): 주역 단독 행 2.4 · 민 무대 격자 1.8 · 그 밖 3.2.
            if (heroIdx >= 0 && heroIdx < cells.Count && cells[heroIdx].Root != null)
            {
                RectTransform hc = cells[heroIdx].Root;
                float cw = hc.rect.width;
                RectTransform rr = UiKit.Box(hc, "sr-heroring");
                rr.anchorMin = new Vector2(0.5f, 1f); rr.anchorMax = new Vector2(0.5f, 1f);
                rr.pivot = new Vector2(0.5f, 1f);
                rr.sizeDelta = new Vector2(cw, cw);
                rr.anchoredPosition = Vector2.zero;
                rr.SetAsFirstSibling();
                heroRing = rr.gameObject.AddComponent<Image>();
                heroRing.raycastTarget = false;
                Color rcr = PetSkillStyle.Rarity(Defs, best);
                heroRing.sprite = SummonFx.BakeHeroRing("sr-heroring-" + ColorUtility.ToHtmlStringRGB(rcr), rcr);
                Material rm = CraftFxPoly.Screen();
                if (rm != null) heroRing.material = rm;
                heroRing.color = new Color(1f, 1f, 1f, 0f);
                // 광창(정본 `.sr-beam`) — 셀 가운데에 폭 100% 정사각. 링과 달리 셀 **앞**에 뜬다(정본은 `::after` 가 아니라 제 요소다).
                RectTransform bm = UiKit.Box(hc, "sr-beam");
                bm.anchorMin = bm.anchorMax = new Vector2(0.5f, 0.5f);
                bm.pivot = new Vector2(0.5f, 0.5f);
                bm.sizeDelta = new Vector2(cw, cw);
                bm.anchoredPosition = Vector2.zero;
                heroBeam = bm.gameObject.AddComponent<Image>();
                heroBeam.raycastTarget = false;
                int tier = RarityIdx(best);
                double amt = SummonFxStyle.Hero.HiliteAmount(rcr.r * 255.0, rcr.g * 255.0, rcr.b * 255.0, tier);
                Color lite = Shade(rcr, (float)amt);   // 정본 srHilite — 목표 휘도까지 흰 쪽으로 당긴다
                heroBeam.sprite = SummonFx.BakeBeam("sr-beam-" + ColorUtility.ToHtmlStringRGB(lite), lite);
                Material bmat = CraftFxPoly.Screen();
                if (bmat != null) heroBeam.material = bmat;
                heroBeam.color = new Color(1f, 1f, 1f, 0f);

                ringMax = heroRow ? SummonFxStyle.H("ringmax_herorow")
                    : (!mid && !dense ? SummonFxStyle.H("ringmax_stage") : SummonFxStyle.H("ringmax_default"));
            }

            // ---- 주역 와이프(정본 `.sr-wipe` 6184~6198) ----
            // 정본이 이 겹을 따로 둔 까닭이 주석에 있다: 홀드백이 없는 대량 소환에 **전 화면 섬광을 쓰면 안 된다**
            // — «x75 는 위쪽 20셀이 같이 하얗게 떠 등급 구분이 무너진다». 그래서 주역 셀 중심에서 번지는
            // **가산 원형 와이프**라 정점 순간에도 반경이 아직 작다. 혼합은 T173 이 세운 `CraftFxPoly.Screen()`.
            if (heroIdx >= 0)
            {
                RectTransform wr = UiKit.Box(c, "sr-wipe");
                UiKit.Fill(wr);
                wipe = wr.gameObject.AddComponent<Image>();
                wipe.raycastTarget = false;
                wipe.type = Image.Type.Simple;
                Color rc = PetSkillStyle.Rarity(Defs, best);
                wipe.sprite = SummonFx.BakeWipe("sr-wipe-" + ColorUtility.ToHtmlStringRGB(rc), rc);
                Material sm = CraftFxPoly.Screen();
                if (sm != null) wipe.material = sm;   // 못 찾으면 보통 알파로 그린다(연출이 사라지는 것보다 낫다 · T173 꼴)
                wipe.color = new Color(1f, 1f, 1f, 0f);
                wr.SetAsLastSibling();
            }

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
            // T334 2회차 — 시각·판정을 Core 상태 기계에 넘긴다(정본 tickSummonResult 그대로).
            // 등급 순위는 정본 `RARITIES.indexOf` 자리 — 이 화면이 이미 쓰는 RarityIdx 다.
            List<int> rank = new List<int>(cells.Count);
            for (int i = 0; i < cells.Count; i++) rank.Add(RarityIdx(cells[i].Entry.Rarity));
            List<double> ds = new List<double>(delays.Count);
            for (int i = 0; i < delays.Count; i++) ds.Add(delays[i]);
            // ⚠ 주역 비트 길이는 **0 으로 둔다**(3회차 몫). 정본에 «350ms» 같은 수는 **없다** — 그 비트는 CSS 가
            //   쥐고 있고 후보가 셋이다: `srshakehit .44s`(5676 · 판 흔들기) · `srheropop .52s`(6727 · 주역 셀 팝) ·
            //   `srrecede .68s`(6720 · 나머지 셀 물러남). 어느 것이 «비트» 인지는 그 겹을 실제로 얹는 회차가
            //   화면을 보고 고른다 — 지금 하나를 골라 표에 박으면 근거 없는 수가 굳는다.
            seq = new SummonSeqRun(ds, rank, holdback, heroIdx, PetSkillStyle.L("sr_tail_ms"), 0.0);
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
            var c = new Cell { Root = cell, Group = cg, Entry = e, Heroic = heroic, Home = cell.anchoredPosition };
            c.BaseScale = heroic ? 1f : PetSkillStyle.L("sr_sz_" + Mathf.Clamp(tier, 0, 5)) * (peer ? PetSkillStyle.L("sr_peer_sz") : 1f);
            c.Pop = PetSkillStyle.L("sr_pop_" + Mathf.Clamp(tier, 0, 5));
            // orbwrap
            RectTransform wrap = UiKit.Box(cell, "sr-orbwrap");
            UiKit.Place(wrap, 0f, 0f, cw, cw);
            wrap.pivot = new Vector2(0.5f, 0.5f);
            wrap.anchoredPosition = new Vector2(cw * 0.5f, -cw * 0.5f);
            c.OrbWrap = wrap;
            c.OrbHome = wrap.anchoredPosition;
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
                    Shrink(qb, qt, PetSkillStyle.Rem(0.6f), bh);
                    UiKit.Fill(qt.rectTransform);
                }
                if (!string.IsNullOrEmpty(e.Extra))
                {
                    float bw = PetSkillKit.TextWidth(TextKind.Sub, e.Extra) + PetSkillStyle.Rem(0.6f);
                    RectTransform db = PetSkillKit.Framed(wrap, "sr-dup", PetSkillStyle.C("sr_badge_bg"), PetSkillStyle.Px("sr_qty_r_rem"), PetSkillStyle.L("line1_px"));
                    ((Image)db.Find("line").GetComponent<Image>()).color = PetSkillStyle.C("sr_hilite");
                    UiKit.Anchor(db, new Vector2(0.06f, 0.93f), new Vector2(0f, 1f), Vector2.zero, bw, bh);
                    TextMeshProUGUI dt = PetSkillKit.Text(db, "t", TextKind.Sub, e.Extra, PetSkillStyle.C("white"));
                    WrapUi.Apply(dt, "sr_dup");   // T361 배선 — 이 파일이 내 lock 뒤라 3회차가 못 걸었다(결정 661)
                    Shrink(db, dt, PetSkillStyle.Rem(0.6f), bh);
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
                    Shrink(nb, nt, PetSkillStyle.Rem(0.52f), bh);   // 자간까지 먹인 **뒤** 잰다
                    UiKit.Fill(nt.rectTransform);
                }
                // 이름판 · 등급 칩
                float ny = cw + PetSkillStyle.Px("sr_name_mt_rem");
                float nw2 = cw * PetSkillStyle.L("sr_name_w_f");
                RectTransform nameBox = UiKit.Box(cell, "sr-name");
                UiKit.Place(nameBox, (cw - nw2) * 0.5f, ny, nw2, nameH);
                PetSkillKit.Fill(nameBox, "bg", PetSkillStyle.C("sr_name_bg"), PetSkillStyle.Px("sr_name_r_rem"));
                TextMeshProUGUI nt2 = PetSkillKit.Text(nameBox, "t", TextKind.Sub, e.Name, PetSkillStyle.C("white"));
                WrapUi.Apply(nt2, "sr_name");   // 정본 `.sr-name` 은 **접는다**(두 줄까지 · style.css 7032~7036)
                UiKit.Fill(nt2.rectTransform);
                float sy = ny + nameH + PetSkillStyle.Px("sr_sub_mt_rem");
                float rkW = PetSkillKit.TextWidth(TextKind.Sub, e.Sub) + PetSkillStyle.Px("sr_rk_pad_x_rem") * 2f;
                RectTransform rk = UiKit.Box(cell, "sr-sub");
                UiKit.Place(rk, (cw - rkW) * 0.5f, sy, rkW, subH);
                PetSkillKit.Fill(rk, "bg", rc, PetSkillStyle.Px("sr_rk_r_rem"));
                TextMeshProUGUI rt = PetSkillKit.Text(rk, "t", TextKind.Sub, e.Sub, ChipInk(rc));
                LetterSpacing.Apply(rt, "sr_sub_ls_em");   // T168 3회차 — 정본 7059 `.sr-sub`
                WrapUi.Apply(rt, "sr_sub");   // 정본 nowrap
                UiKit.Fill(rt.rectTransform);
            }
            // ---- 비행 잔상(정본 `.sr-ghost` 6448~6474 · z 0) ----
            // 정본 주석: «팝이 아래에서 올라오는데 궤적이 없으면 «순간이동 후 튕김» 으로 보인다» ·
            //   «잔상은 «아래에서 솟은 자국» 이 아니라 **비행 경로에 끌리는 꼬리** 다 — 셀이 광원 쪽에서 날아오므로
            //    잔상은 그 뒤쪽(광원 쪽)에 남아 따라붙는다. 셀 안에 있어서 셀의 이동이 이미 곱해진 상태라,
            //    여기서는 «뒤처진 만큼» 만 더 민다» — 그래서 이 판은 셀 **안**(구체 래퍼)에 산다(스파크·재점화와 다른 자리다).
            // 상자는 `inset: 0`(래퍼와 같은 칸) · 구체 **뒤**라 맨 앞 형제로 넣는다.
            {
                RectTransform gh = UiKit.Box(wrap, "sr-ghost");
                UiKit.Fill(gh);
                gh.SetAsFirstSibling();
                Image gi = gh.gameObject.AddComponent<Image>();
                gi.raycastTarget = false;
                gi.preserveAspect = false;
                double gamt = SummonFxStyle.Hero.HiliteAmount(rc.r * 255.0, rc.g * 255.0, rc.b * 255.0, tier);
                Color glite = Shade(rc, (float)gamt);   // 정본 `--rc-lite`
                gi.sprite = SummonFx.BakeGhost(
                    "sr-ghost-" + ColorUtility.ToHtmlStringRGB(rc) + "-" + ColorUtility.ToHtmlStringRGB(glite), rc, glite);
                gi.color = new Color(1f, 1f, 1f, 0f);
                c.Ghost = gi;
            }
            // ---- 착지 스파크(정본 `.sr-spark` 6463~6484 · z 2) ----
            // 정본 주석: «링 하나로는 «내려앉았다» 만 말하고 «부딪혔다» 를 말하지 못한다 —
            //   box-shadow 8방향 복제를 transform: scale 로 바깥으로 날린다(오프셋도 함께 확대된다)».
            // 그래서 여기서도 복제 여덟 + 심 하나를 **한 장에** 굽고, 날리기는 판의 배율이 한다.
            // 가산(screen)이라야 구체 위에서 «튄 빛» 으로 읽힌다 — 알파로 덮으면 구체에 흰 점을 찍은 것이 된다.
            {
                SummonSparkSpec ssp = SummonFxStyle.Spark;
                float sz = PetSkillStyle.Rem((float)ssp.BoxRem);
                RectTransform spk = UiKit.Box(wrap, "sr-spark");
                spk.anchorMin = spk.anchorMax = new Vector2(0.5f, 0.5f);
                spk.pivot = new Vector2(0.5f, 0.5f);
                spk.sizeDelta = new Vector2(sz, sz);
                spk.anchoredPosition = Vector2.zero;
                Image si = spk.gameObject.AddComponent<Image>();
                si.raycastTarget = false;
                si.preserveAspect = false;
                si.sprite = SummonFx.BakeSpark("sr-spark-" + ColorUtility.ToHtmlStringRGB(rc), rc);
                Material sm = CraftFxPoly.Screen();
                if (sm != null) si.material = sm;
                si.color = new Color(1f, 1f, 1f, 0f);
                spk.localScale = Vector3.one * (float)(ssp.Scale.Sample(0, "base", null) + ssp.Glow(tier) * ssp.Scale.Sample(0, "glow", null));
                c.Spark = si;
            }
            cell.localScale = Vector3.one * 0.35f;
            return c;
        }

        /// <summary>
        /// 배지 알약을 **제 글자에 맞춰 줄인다**(정본은 `position: absolute` + `padding` 이라 폭이 잉크에 딱 맞는 shrink-to-fit 이다).
        ///
        /// ⚑ 13회차 판정(런 754 `screen_t179-summon` 8배 확대)에서 나온 자리다: «NEW» 가 «NE / W» 로 **접혀 알약 밖으로 넘쳤다**.
        /// 까닭은 알약 폭을 <see cref="PetSkillKit.TextWidth"/>(ASCII 를 한 자 .58em 로 어림하는 자)로 잡는데
        /// 대문자 N·E·W 의 실제 폭이 그보다 훨씬 넓고(자간 .04em 도 안 센다), T361 3회차가 줄바꿈 기본값을
        /// 정본대로 «접는다» 로 뒤집으면서 그 어림이 **넘침에서 접힘으로** 바뀐 것이다(결정 661 이 예고한 자리).
        /// 어림을 고치는 대신(그 자는 이 파일 밖이고 다른 자리까지 한꺼번에 움직인다) **정본의 shrink-to-fit 을 그대로 옮긴다** —
        /// 글자를 세운 뒤(자간까지 먹인 뒤) 제 선호 폭을 재어 알약을 그 폭으로 다시 앉힌다. 상자가 잉크보다 넓으니 접힐 일이 없다.
        /// </summary>
        static void Shrink(RectTransform pill, TextMeshProUGUI t, float padX, float h)
        {
            Vector2 pv = t.GetPreferredValues();
            float w = pv.x + padX;
            if (w <= pill.sizeDelta.x) return;   // 어림이 이미 넉넉하면 그대로 둔다(자리가 안 움직인다)
            pill.sizeDelta = new Vector2(w, h);
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
                // ⚑ 13회차 · §1 «실제 화면을 본다» — 이 줄은 **아이콘 길을 거쳐야 한다.**
                //   런 743 `screen_t179-summon` 을 열어 보니 «✨ 신규 스킬 획득!» 의 머리가 **두부(□)** 였다.
                //   문구는 표(`sr_solo_skill_new`)에서 오고 정본 `TOAST_ICON` 이 «✨ → sparkle» 로 쥐고 있는데
                //   여기서 글자 그대로 세우고 있었다 — 주인 글꼴에도 이모지 폴백에도 U+2728 이 없으니 □ 다.
                //   `check_text_glyphs` 가 rc 0 인 까닭은 그 자가 «TOAST_ICON 에 있으면 아이콘으로 치환된다» 로 빼기 때문이고,
                //   자기 머리 주석이 바로 그 함정을 경고한다(«표에 있다» 가 «그 자리가 아이콘을 거친다» 는 뜻이 아니다).
                RectTransform lt = UiKit.IconTextRow(sb, "line", TextKind.Sub, line, "white");
                UiKit.Place(lt, 0f, 0f, fw, sub * 1.3f);
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
            if (!done && seq != null)
            {
                float elapsed = (Time.unscaledTime - start) * 1000f;
                int loudRank = seq.Tick(elapsed);          // 밀린 셀을 다 띄우고 상태를 옮긴다(정본 tickSummonResult)
                while (idx < seq.Revealed && idx < cells.Count) { TurnOn(cells[idx]); idx++; }
                if (loudRank >= 0)
                {
                    // 효과음은 이번 프레임 **최고 등급 하나**(정본 주석 — 한 프레임에 여럿이 몰려도 한 번).
                    string loud = RarityOf(loudRank);
                    var sr = PetSkillHost.SfxSummonReveal;
                    if (sr != null && loud != null) sr(loud);
                }
                if (seq.Hero && !heroFired) FireHero();
                if (seq.Done) Finish();
            }
            AnimateCells();
            AnimateTicks();
            AnimateCharge();
            AnimateFlash();
            AnimateWipe();
            AnimateKick();
            AnimateHeroRing();
            AnimateBeam();
            AnimateRelights();
            AnimateSparks();
            AnimateGhosts();
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
                // T334 5회차 — 아이들 호흡은 **등급에 가중**된다(정본 `--idle` 계단 · style.css 6327~6332).
                //   정본 주석: «예전엔 전 등급이 똑같이 −.16rem / ×1.055 였고 실측상 등급 간 차이는 광채에서만 나왔다 —
                //   즉 위계가 구조가 아니라 부산물이었다». 수치는 표(`SummonFxUi.json` 의 `idle` 절)가 쥔다.
                float b = 1f, ty = 0f;
                if (done && t >= 1f && doneAt >= 0f)
                {
                    SummonIdleSpec sp = SummonFxStyle.Idle;
                    double tyRem, add;
                    sp.At((tt - doneAt) * 1000f, i, RarityIdx(c.Entry.Rarity), out tyRem, out add);
                    b = 1f + (float)add;
                    ty = (float)tyRem * PetSkillStyle.RemPx;
                }
                // T334 7회차 — 주역이 착지하면 **조연은 물러난다**(정본 `srrecede` · 절 머리 주석: «나머지를 물리고(후퇴)
                //   광창 → 충격파 → 화면 킥을 몰아 «다른 사건» 으로 만든다»). 배율은 셀에, 채도·밝기는 그 그림들에 건다.
                float rs = 1f;
                if (heroFired && heroAtWall >= 0f && !c.Heroic) rs = Recede(c, (tt - heroAtWall) * 1000f);
                if (c.Heroic && heroFired && heroAtWall >= 0f)
                {
                    // T334 8회차 — 주역은 착지에서 제 등장(정본 `srheropop`)을 새로 탄다: 정본이 `animation` 단축 속성을
                    //   통째로 갈아 끼우므로 앞의 팝은 그 순간 사라진다. «더 길고 더 크게 넘치고, 끝에서 원래 크기로
                    //   안 돌아온다(무대에 남는다)».
                    SummonHeroSpec hp = SummonFxStyle.Hero;
                    double backF, tyRem, sc, op;
                    hp.HeroPopAt((tt - heroAtWall) * 1000f, out backF, out tyRem, out sc, out op);
                    c.Group.alpha = (float)op;
                    c.Root.localScale = Vector3.one * (float)sc;
                    Vector2 back = c.ToLight * (float)backF;
                    if (back == Vector2.zero && backF > 0f) back = new Vector2(0f, -(float)hp.HeroPopDy0Rem * PetSkillStyle.RemPx);
                    c.Root.anchoredPosition = c.Home + back + new Vector2(0f, -(float)tyRem * PetSkillStyle.RemPx);
                }
                else c.Root.localScale = Vector3.one * s * rs;
                // ⚠ 정본 6729 주석이 실측으로 못 박았다: «정착 스케일을 1보다 크게 두면 셀 폭을 넘는 이름판이 옆 셀
                //   이름과 겹친다 … 주역의 «큰 몸집» 은 등급 계단이 이미 맡는다». 그래서 여기 배수(옛 1.18)를 뺐다.
                c.OrbWrap.localScale = Vector3.one * c.BaseScale * b;
                c.OrbWrap.anchoredPosition = c.OrbHome + new Vector2(0f, -ty);   // 표의 ty_rem 은 CSS 부호(음수 = 위로)
            }
        }


        /// <summary>
        /// T334 3회차 ⓑ — 홀드백 대기 구간(정본 `#summon-result-modal.charging` · style.css 6815~6879).
        /// 정본 주석이 못 박은 것: 이 구간은 **정지가 아니라 축적**이고, 예전엔 480ms 가 통째로 정지 프레임이라
        /// «긴장» 이 아니라 «렌더 멈춤» 으로 읽혔다. 그래서 다섯이 한꺼번에 움직인다 —
        /// 소환진이 부풀며 밝아지다 **마지막 12%에 수축**(그 반동으로 주역이 터진다) · 눈금이 `steps(9)` 로 순차 점등 ·
        /// 중앙 광원이 점점 좁은 간격으로 뛰고 · 비네트가 조여들고 · 정착한 조연 셀이 광원 쪽으로 빨려든다(사출의 역재생).
        ///
        /// 값·곡선은 전부 <see cref="SummonChargeSpec"/>(Core · 표 `charge` 절)이 쥔다 — 여기는 그것을 화면에 거는 손일 뿐이다.
        /// 구간이 끝나면(주역 착지) 정본이 클래스를 떼는 것과 같게 **제자리로 되돌린다** — 그 순간은 섬광/와이프가 덮는다.
        /// </summary>
        void AnimateCharge()
        {
            bool on = seq != null && seq.Charging;
            if (!on)
            {
                if (chargeAt < 0f) return;
                chargeAt = -1f;
                if (floorImg != null) { floorImg.color = floorBase; floorImg.rectTransform.localScale = floorHome; }
                if (haloImg != null) haloImg.color = haloBase;
                if (vigImg != null) { vigImg.color = new Color(1f, 1f, 1f, 0f); vigImg.rectTransform.localScale = Vector3.one; }
                for (int i = 0; i < cells.Count; i++) cells[i].Root.anchoredPosition = cells[i].Home;
                return;
            }
            if (chargeAt < 0f)
            {
                chargeAt = Time.unscaledTime;
                // «슬롯 → 광원» 벡터(정본 `--dx/--dy` 는 setSummonEjectPaths 가 심어 둔다 — 클론엔 없어 여기서 잰다).
                // ⚑ 15회차 — **62%만 되짚는다**(정본 `SR_EJECT = 0.62` · ui.js 604). 3회차가 재는 길은 옮겼지만
                //   그 한 줄을 빠뜨려 클론은 벡터를 **100%** 되짚고 있었다. 정본 주석이 그것을 이름으로 금지한다:
                //   «⚠️ 벡터를 100% 되짚으면 전 셀이 한 점에서 겹쳐 나와 5개가 한 덩어리로 보인다 —
                //    일부(EJECT)만 되짚어 «광원 쪽에서 밀려 나온» 인상만 남긴다».
                //   이 벡터를 쓰는 자리 셋(주역 등장 `srheropop` · 조연 흡기 `srinhale` · 잔상 `srghost`)이 전부 62% 벡터다.
                float eject = SummonFxStyle.L("eject_f");
                for (int i = 0; i < cells.Count; i++)
                {
                    Cell c = cells[i];
                    c.Home = c.Root.anchoredPosition;
                    // 두 자리 다 **같은 부모의 지역 좌표**로 재야 한다 — anchoredPosition 과 localPosition 은 상수만큼
                    // 어긋나 있어서(앵커·피벗) 차이(벡터)는 같지만 한쪽을 다른 쪽에서 빼면 그 상수가 섞여 들어간다.
                    c.ToLight = haloImg != null
                        ? ((Vector2)c.Root.parent.InverseTransformPoint(haloImg.rectTransform.position) - (Vector2)c.Root.localPosition) * eject
                        : Vector2.zero;
                }
            }
            SummonChargeSpec sp = SummonFxStyle.Charge;
            float ms = (Time.unscaledTime - chargeAt) * 1000f;
            if (floorImg != null)
            {
                float sc = (float)sp.FloorAt(ms, "scale"), br = (float)sp.FloorAt(ms, "bright"), sa = (float)sp.FloorAt(ms, "sat");
                floorImg.rectTransform.localScale = floorHome * sc;
                // ⚠ 눈금(`srtickup`)은 **아직 안 건다**: 정본에서 그것은 `.sr-floor::after` 의 룬 띠인데 클론의 소환진은
                //    민 원판 한 장이고 룬 눈금은 천개 아치에 구워져 있다(`BakeArch`). 없는 겹에 얹으면 «소환진이 통째로
                //    짙어진다» 가 되어 정본과 다른 그림이 된다 — 수치(`TickAlpha`)는 표·Core 에 세워 두고 겹은 다음 회차에 낸다.
                floorImg.color = Brighten(floorBase, br, sa, floorBase.a);
            }
            if (haloImg != null)
            {
                float a = (float)sp.HaloAt(ms, "alpha"), sc = (float)sp.HaloAt(ms, "scale"), br = (float)sp.HaloAt(ms, "bright");
                haloImg.rectTransform.localScale = Vector3.one * sc;
                haloImg.color = Brighten(haloBase, br, 1f, a);
            }
            if (vigImg != null)
            {
                vigImg.color = new Color(1f, 1f, 1f, (float)sp.VigAt(ms, "alpha"));
                vigImg.rectTransform.localScale = Vector3.one * (float)sp.VigAt(ms, "scale");
            }
            float back = (float)sp.InhaleAt(ms, "back_f"), isc = (float)sp.InhaleAt(ms, "scale");
            for (int i = 0; i < cells.Count; i++)
            {
                Cell c = cells[i];
                if (!c.On || c.Heroic) continue;          // 정본 `.sr-cell.on:not(.heroic)`
                c.Root.anchoredPosition = c.Home + c.ToLight * back;
                c.Root.localScale *= isc;                 // AnimateCells 가 이미 판 팝을 얹었다 — 그 위에 흡기를 곱한다
            }
        }

        /// <summary>CSS `filter: brightness(b) saturate(s)` 를 틴트 한 색으로 — 알파는 따로 준다(합성 단계라 곱하기가 맞다).</summary>
        static Color Brighten(Color c, float bright, float sat, float alpha)
        {
            float g = c.r * 0.2126f + c.g * 0.7152f + c.b * 0.0722f;
            return new Color(
                Mathf.Clamp01((g + (c.r - g) * sat) * bright),
                Mathf.Clamp01((g + (c.g - g) * sat) * bright),
                Mathf.Clamp01((g + (c.b - g) * sat) * bright),
                Mathf.Clamp01(alpha));
        }


        /// <summary>
        /// 룬 눈금 띠 — 충전 구간에서는 `srtickup`(steps(9) · .45 → 1), 그 밖에는 `srfloorbreath`(4.2s · .5 ↔ .9).
        /// 정본이 «움직임은 호흡뿐» 이라 못 박은 자리라 돌리지도 넓히지도 않는다.
        /// </summary>
        void AnimateTicks()
        {
            if (tickImg == null) return;
            float a;
            if (seq != null && seq.Charging && chargeAt >= 0f)
            {
                a = (float)SummonFxStyle.Charge.TickAlpha((Time.unscaledTime - chargeAt) * 1000f);
            }
            else
            {
                float per = SummonFxStyle.L("floor_breath_s"), lo = SummonFxStyle.L("floor_breath_a_lo"), hi = SummonFxStyle.L("floor_breath_a_hi");
                float t = per <= 0f ? 0f : Mathf.Repeat(Time.unscaledTime - start, per) / per;   // start 는 이미 초다(ms 로 나누지 않는다)
                float k = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f);      // ease-in-out 왕복(0 → 1 → 0)
                a = Mathf.Lerp(lo, hi, k);
            }
            tickImg.color = new Color(tickBase.r, tickBase.g, tickBase.b, tickBase.a * a);
        }

        /// <summary>정본 `srShade(hex, amt)` — 양수면 흰 쪽으로 amt 만큼 띄운다(`c + (255-c)*amt`).</summary>
        static Color Shade(Color c, float amt)
        {
            return new Color(c.r + (1f - c.r) * amt, c.g + (1f - c.g) * amt, c.b + (1f - c.b) * amt, c.a);
        }


        /// <summary>
        /// 주역 착지의 화면 킥(정본 `srshakehit` · 5675~5684) — 판을 제 크기의 비율만큼 흔든다.
        /// 치우침이 퍼센트라 화면 크기가 달라져도 같은 세기로 읽힌다. 끝나면 제자리(정본 `both` 필의 마지막 키가 «없음» 이다).
        /// </summary>
        void AnimateKick()
        {
            if (wrap == null || kickAt < 0f) return;
            SummonHeroSpec sp = SummonFxStyle.Hero;
            double ms = (Time.unscaledTime - kickAt) * 1000f;
            double tx, ty, sc;
            sp.At(ms, out tx, out ty, out sc);
            Rect r = wrap.rect;
            wrap.anchoredPosition = wrapHome + new Vector2((float)(tx * r.width), -(float)(ty * r.height));   // CSS 의 +y 는 아래
            wrap.localScale = Vector3.one * (float)sc;
            if (!sp.Kicking(ms)) kickAt = -1f;      // 다 흔들었으면 손을 뗀다(아이들이 이 판을 다시 안 잡는다)
        }

        /// <summary>화면 킥이 도는 중인가 — 자가 본다.</summary>
        public bool Kicking { get { return kickAt >= 0f; } }


        /// <summary>
        /// 조연 셀 하나의 물러남(정본 `srrecede`) — 배율을 돌려주고 채도·밝기는 그 셀의 그림들에 건다.
        /// 제 색은 **처음 한 번만** 담는다(매 프레임 담으면 어두워진 색이 새 «제 색» 이 되어 회차마다 더 어두워진다).
        /// </summary>
        float Recede(Cell c, double ms)
        {
            SummonHeroSpec sp = SummonFxStyle.Hero;
            double sc, sat, br;
            sp.RecedeAt(ms, out sc, out sat, out br);
            if (c.Tint == null)
            {
                c.Tint = c.Root.GetComponentsInChildren<Graphic>(true);
                c.TintHome = new Color[c.Tint.Length];
                for (int i = 0; i < c.Tint.Length; i++) c.TintHome[i] = c.Tint[i].color;
            }
            for (int i = 0; i < c.Tint.Length; i++)
            {
                if (c.Tint[i] == null) continue;
                Color h = c.TintHome[i];
                c.Tint[i].color = Brighten(h, (float)br, (float)sat, h.a);
            }
            return (float)sc;
        }


        /// <summary>
        /// 주역 충격파 링(정본 `srheroring` .62s) — 알파 .95 → 0, 배율 .5 → `--ringmax`.
        /// 정본이 «일반 착지 링과 **별개 레이어**» 라 적어 둔 겹이라 셀의 다른 연출과 따로 돈다.
        /// </summary>
        void AnimateHeroRing()
        {
            if (heroRing == null || heroAtWall < 0f) return;
            float ms = (Time.unscaledTime - heroAtWall) * 1000f;
            float dur = SummonFxStyle.H("heroring_ms");
            float u = dur <= 0f ? 1f : Mathf.Clamp01(ms / dur);
            float k = (float)SummonFxStyle.HeroRingEase.Ease(u);
            float a0 = SummonFxStyle.H("heroring_a0"), s0 = SummonFxStyle.H("heroring_scale0");
            heroRing.color = new Color(1f, 1f, 1f, Mathf.Lerp(a0, 0f, k));
            heroRing.rectTransform.localScale = Vector3.one * Mathf.Lerp(s0, ringMax, k);
        }


        /// <summary>
        /// 주역 광창(정본 `srbeam` .52s) — 알파 0 → .95(24%) → 0 · 배율 .16 → 1.05 → 2.1 · 회전 −9° → 0° → 9°.
        /// 떠올랐다 **사라진다**(표가 그것을 강제한다) — 안 그러면 결과 화면에 빛기둥이 남는다.
        /// </summary>
        void AnimateBeam()
        {
            if (heroBeam == null || heroAtWall < 0f) return;
            SummonHeroSpec sp = SummonFxStyle.Hero;
            double a, sc, rot;
            sp.BeamAt((Time.unscaledTime - heroAtWall) * 1000f, out a, out sc, out rot);
            heroBeam.color = new Color(1f, 1f, 1f, (float)a);
            heroBeam.rectTransform.localScale = Vector3.one * (float)sc;
            heroBeam.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -(float)rot);   // CSS 의 +각은 시계 방향
        }

        /// <summary>
        /// 셀별 광원 재점화(정본 `srrelight` .34s) — 셀이 뜬 그 순간부터 광원 자리에서 켜졌다 꺼진다.
        /// 정본은 인라인 `animation-delay` 로 시각을 주지만 그 지연은 곧 «그 셀이 뜨는 시각» 이라, 여기서는
        /// 셀의 점등 시각(<c>OnAt</c>)에서 잰다 — 탭 건너뛰기로 셀이 한꺼번에 떠도 인과가 안 끊긴다.
        /// </summary>
        void AnimateRelights()
        {
            if (relights.Count == 0) return;
            SummonRelightSpec sp = SummonFxStyle.Relight;
            float tt = Time.unscaledTime;
            for (int i = 0; i < relights.Count && i < cells.Count; i++)
            {
                Image ri = relights[i];
                if (ri == null) continue;
                Cell c = cells[i];
                if (!c.On) continue;
                double a, sc;
                sp.At((tt - c.OnAt) * 1000f, RarityIdx(c.Entry.Rarity), out a, out sc);
                ri.color = new Color(1f, 1f, 1f, (float)a);
                ri.rectTransform.localScale = Vector3.one * (float)sc;
            }
        }

        /// <summary>
        /// 착지 스파크(정본 `srspark` .42s) — 셀이 뜬 그 순간부터 심에서 터져 바깥으로 날아가며 꺼진다.
        /// 알파는 0→62→100 **두 구간**이고 배율은 한 구간이다(표가 트랙을 둘로 나눠 쥔다).
        /// </summary>
        void AnimateSparks()
        {
            SummonSparkSpec sp = null;
            float tt = Time.unscaledTime;
            for (int i = 0; i < cells.Count; i++)
            {
                Cell c = cells[i];
                if (c.Spark == null || !c.On) continue;
                if (sp == null) sp = SummonFxStyle.Spark;
                double a, sc;
                sp.At((tt - c.OnAt) * 1000f, RarityIdx(c.Entry.Rarity), out a, out sc);
                c.Spark.color = new Color(1f, 1f, 1f, (float)a);
                c.Spark.rectTransform.localScale = Vector3.one * (float)sc;
            }
        }

        /// <summary>
        /// 비행 잔상(정본 `srghost` · 길이는 그 셀의 `var(--pop)`) — 셀이 날아온 **뒤쪽**(광원 쪽)에 남아 따라붙는다.
        ///
        /// 잔상은 셀 안에 있어 셀의 이동이 이미 곱해져 있다 — 그래서 여기서는 «뒤처진 만큼» 만 더 민다(정본 주석 그대로).
        /// 미는 방향은 «슬롯 → 광원» 사출 벡터(`--dx/--dy` · `SR_EJECT` 를 이미 곱한 값)다.
        /// </summary>
        void AnimateGhosts()
        {
            SummonGhostSpec sp = null;
            float tt = Time.unscaledTime;
            for (int i = 0; i < cells.Count; i++)
            {
                Cell c = cells[i];
                if (c.Ghost == null || !c.On) continue;
                if (sp == null) sp = SummonFxStyle.Ghost;
                double back, sc, a;
                sp.At((tt - c.OnAt) * 1000f, c.Pop * 1000f, RarityIdx(c.Entry.Rarity), out back, out sc, out a);
                c.Ghost.color = new Color(1f, 1f, 1f, (float)a);
                c.Ghost.rectTransform.localScale = Vector3.one * (float)sc;
                c.Ghost.rectTransform.anchoredPosition = c.ToLight * (float)back;
            }
        }

        /// <summary>그 셀의 비행 잔상 — 자가 본다.</summary>
        public Image GhostOf(int i) { return i >= 0 && i < cells.Count ? cells[i].Ghost : null; }

        /// <summary>그 셀의 착지 스파크 — 자가 본다.</summary>
        public Image SparkOf(int i) { return i >= 0 && i < cells.Count ? cells[i].Spark : null; }

        /// <summary>그 셀의 «슬롯 → 광원» 사출 벡터(정본 `--dx/--dy` · 전체 벡터의 `SR_EJECT` 만큼) — 자가 본다.</summary>
        public Vector2 EjectOf(int i) { return i >= 0 && i < cells.Count ? cells[i].ToLight : Vector2.zero; }

        /// <summary>셀별 재점화 플래시 — 자가 본다.</summary>
        public Image RelightOf(int i) { return i >= 0 && i < relights.Count ? relights[i] : null; }

        /// <summary>재점화 플래시 수 — 자가 본다.</summary>
        public int RelightCount { get { return relights.Count; } }

        /// <summary>주역 광창 — 자가 본다.</summary>
        public Image HeroBeam { get { return heroBeam; } }

        void FireHero()
        {
            if (heroFired) return;
            heroFired = true;
            // 정본 fireSummonHero 748~757: 홀드백이면 `.flash`(전 화면), **아니면 `.wipe`** — 그 갈래를 그대로.
            // 예전 클론은 둘 다 전 화면 섬광이고 홀드백이 아닐 때만 옅게 했다(0.45) — 정본이 «쓰면 안 된다» 고
            // 못 박은 자리다(x75 는 위쪽 20셀이 같이 하얗게 뜬다).
            if (holdback || wipe == null)
            {
                flash.color = PetSkillStyle.Rarity(Defs, best);
                flashAt = Time.unscaledTime;
            }
            else
            {
                // 가운데를 주역 셀로 옮긴다(정본 `--fx`/`--fy`). 스프라이트는 가운데로 구웠으니 자리만 잡는다.
                RectTransform wr = (RectTransform)wipe.transform;
                if (heroIdx >= 0 && heroIdx < cells.Count && cells[heroIdx].Root != null)
                    wr.pivot = HeroPivot(wr, cells[heroIdx].Root);
                wipeAt = Time.unscaledTime;
            }
            // 정본 5675: 화면 킥은 **홀드백 여부와 무관하게** `.hero` 에 건다 — 섬광이든 와이프든 판은 똑같이 흔들린다.
            kickAt = Time.unscaledTime;
            heroAtWall = kickAt;
            var g = PetSkillHost.SfxGacha;
            if (g != null) g(best);
        }

        /// <summary>주역 셀의 가운데를 와이프 상자 안의 피벗(0~1)으로 — 정본 `transform-origin: var(--fx) var(--fy)`.</summary>
        static Vector2 HeroPivot(RectTransform box, RectTransform cell)
        {
            Vector2 c = (Vector2)box.InverseTransformPoint(cell.TransformPoint(cell.rect.center));
            Rect r = box.rect;
            if (r.width <= 0f || r.height <= 0f) return new Vector2(0.5f, 0.5f);
            return new Vector2(Mathf.Clamp01((c.x - r.xMin) / r.width), Mathf.Clamp01((c.y - r.yMin) / r.height));
        }

        /// <summary>정본 `@keyframes srwipe`(6194~6198): 불투명도 0 → .98(15%% ≈ 70ms 정점) → 0 · 배율 .55 → 1.12 → 2.2.</summary>
        void AnimateWipe()
        {
            if (wipe == null) return;
            if (wipeAt < 0f) return;
            float ms = SummonFxStyle.L("wipe_ms");
            float t = ms <= 0f ? 1f : (Time.unscaledTime - wipeAt) * 1000f / ms;
            float pk = SummonFxStyle.L("wipe_peak_f"), pa = SummonFxStyle.L("wipe_peak_a");
            float s0 = SummonFxStyle.L("wipe_scale0_f"), sp = SummonFxStyle.L("wipe_scale_peak_f"), s1 = SummonFxStyle.L("wipe_scale1_f");
            float a, sc;
            if (t <= pk) { float k = pk <= 0f ? 1f : t / pk; a = Mathf.Lerp(0f, pa, k); sc = Mathf.Lerp(s0, sp, k); }
            else { float k = Mathf.Clamp01((t - pk) / Mathf.Max(1e-4f, 1f - pk)); a = Mathf.Lerp(pa, 0f, k); sc = Mathf.Lerp(sp, s1, k); }
            if (t >= 1f) { a = 0f; sc = s1; wipeAt = -1f; }
            Color c = wipe.color; wipe.color = new Color(c.r, c.g, c.b, a);
            wipe.transform.localScale = new Vector3(sc, sc, 1f);
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

        float doneAt = -1f;

        void Finish()
        {
            done = true;
            doneAt = Time.unscaledTime;
            if (fx != null) fx.SetDone();   // T179 — 정본 #summon-result-modal.done: 별 켜기 · 광선 done 마스크
            // 소환진이 등급색으로 물드는 것은 **이 순간뿐**이다(정본 ui.js 551~552 · style.css 7182~7185) — 알파도 .2 → .26.
            if (floorImg != null)
            {
                Color rc = PetSkillStyle.Rarity(Defs, best);
                floorImg.color = new Color(rc.r, rc.g, rc.b, SummonFxStyle.L("floor_done_a"));
                floorBase = floorImg.color;
                if (tickImg != null)
                {
                    // 정본 ui.js 548~551: 선은 **등급색 원본이 아니라 밝게 띄운 파생색**이다 —
                    // «배경까지 그 등급색으로 물든 뒤라 원색 그대로는 배경에 묻혀 소환진이 사라진다».
                    Color lr = Shade(rc, SummonFxStyle.L("floor_line_hi_shade_f"));
                    tickBase = new Color(lr.r, lr.g, lr.b, SummonFxStyle.L("floor_line_hi_a"));
                    tickImg.color = tickBase;
                }
            }
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
