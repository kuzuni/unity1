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
    /// 펫 서브탭(원작 ui.js renderPets · openPetDetail · openEggDetail · onSummonPetEgg · onStartHatch · onHatchSkip · onBuyHatchSlot · onTogglePet · onMerge · UI-SPEC 9·12·13 · shot-042356/042449).
    /// 위→아래: 시트 머리(🥚 알약 · «펫 N/250» · 젬 알약) → 5열 타일 그리드(보유 펫 → 미부화 알 · 합성 버튼 행) → «장착됨» 행(사각 미니 3) → 소환 바(xN · 소환 xN 🥚비용 · i Lv.N 게이지) →
    /// 부화장(어두운 남색 · 램프+빛기둥 슬롯 3~5 · 타이머 · 💎스킵 · ◀ · 슬롯 +1 💎). 규칙은 Core <see cref="PetSystem"/> · 변경 뒤 <see cref="PetSkillHost.Sync"/>.
    /// </summary>
    public sealed class PetPanel : MonoBehaviour
    {
        SkillPetSheet sheet;
        float height;
        RectTransform root;
        ScrollRect scroll;
        public const string Kind = "pet";

        PetSkillHost H { get { return sheet.Host; } }
        PetSystem P { get { return H.Pets; } }
        GameDefs Defs { get { return H.Data.Defs; } }

        public Button SummonButton { get; private set; }
        public Button MultButton { get; private set; }
        public Button RatesButton { get; private set; }
        public Button BackButton { get; private set; }
        public Button SlotBuyButton { get; private set; }
        public string Title { get; private set; }
        public int GridCells { get; private set; }
        readonly Dictionary<int, Button> petTiles = new Dictionary<int, Button>();
        readonly Dictionary<int, Button> eggTiles = new Dictionary<int, Button>();
        readonly List<Button> skipButtons = new List<Button>();
        readonly List<TextMeshProUGUI> hatchTimes = new List<TextMeshProUGUI>();
        readonly List<PetHatchCone> hatchCones = new List<PetHatchCone>();
        /// <summary>부화 칸 i 의 빛기둥(빈 칸은 dim) — 테스트가 «실제로 칠해졌는가» 를 픽셀로 본다(T102).</summary>
        public PetHatchCone HatchCone(int i) { return i >= 0 && i < hatchCones.Count ? hatchCones[i] : null; }
        readonly List<TextMeshProUGUI> skipCosts = new List<TextMeshProUGUI>();
        float clockAt;

        public Button PetTile(int i) { Button b; return petTiles.TryGetValue(i, out b) ? b : null; }
        public Button EggTile(int i) { Button b; return eggTiles.TryGetValue(i, out b) ? b : null; }
        public Button SkipButton(int i) { return i < skipButtons.Count ? skipButtons[i] : null; }

        public void Init(SkillPetSheet s, float h)
        {
            sheet = s;
            height = h;
            root = (RectTransform)transform;
        }

        void Update()
        {
            if (!gameObject.activeInHierarchy || hatchTimes.Count == 0 || !PetSkillHost.Ready) return;
            clockAt += Time.unscaledDeltaTime;
            if (clockAt < 0.5f) return;
            clockAt = 0f;
            for (int i = 0; i < hatchTimes.Count && i < P.State.Hatching.Count; i++)
            {
                HatchSlot h = P.State.Hatching[i];
                if (hatchTimes[i] != null) hatchTimes[i].text = PetSkillStyle.FmtTime((h.EndsAt - H.Now()) / 1000);
                if (i < skipCosts.Count && skipCosts[i] != null) skipCosts[i].text = JsNum.ToString(P.GemSkipCost(h));
            }
        }

        // ===== renderPets =====
        public void Render()
        {
            if (!PetSkillHost.Ready) return;
            PetSkillKit.Clear(root);
            petTiles.Clear(); eggTiles.Clear(); skipButtons.Clear(); hatchTimes.Clear(); skipCosts.Clear(); hatchCones.Clear();
            float pad = PetSkillStyle.Px("pad_rem");
            float W = root.rect.width > 0f ? root.rect.width : UiKit.RefW - pad * 2f;
            float appW = UiKit.RefW;
            float gap = PetSkillStyle.Px("gap_rem");
            float y = 0f;

            // ---- sheet-head ----
            float headH = PetSkillStyle.Px("head_h_rem");
            RectTransform head = UiKit.Box(root, "sheet-head");
            UiKit.Place(head, 0f, y, W, headH);
            Title = PetSkillStyle.T("pets_title", P.State.Pets.Count, P.Rules.InvCap);
            TextMeshProUGUI title = PetSkillKit.Stroked(head, "sheet-title", TextKind.Title, Title, PetSkillStyle.C("white"), "sheet_title");   // 정본 h2.sheet-title .11em
            UiKit.TextShadow(title, "paper_emboss");   // T333 2회차 — 정본 8381 한 벌 «밝은 종이 위 글자는 흰 엠보스»(0 1px 0 rgba(255,255,255,.92))
            UiKit.Fill(title.rectTransform);
            float pillH = PetSkillStyle.Px("pill_h_rem");
            PetSkillKit.Pill(head, "pill-egg", PetSkillStyle.C("pill_egg"), "eggCracked", PetSkillStyle.Fmt(H.EggCurrency), pillH, 0f, (headH - pillH) * 0.5f);
            PetSkillKit.Pill(head, "pill-gem", PetSkillStyle.C("pill_gem"), "gem", PetSkillStyle.Fmt(H.Gems), pillH, 0f, (headH - pillH) * 0.5f, true);
            y += headH + gap;

            // ---- 아래부터: 부화장 · 소환 바 · 장착됨 행 ----
            float hatchH = PetSkillStyle.Px("hatchery_min_rem");
            float hatchY = height - hatchH + PetSkillStyle.Rem(0.5f);
            float barH = PetSkillStyle.Px("bar_h");
            float barY = hatchY - gap - barH;
            float eqH = PetSkillStyle.Px("mini_rem") + PetSkillStyle.Px("equipped_pad_y_rem") * 2f + PetSkillKit.Line3 * 2f;
            float eqY = barY - gap - eqH;
            float gridBottom = eqY - gap;

            // ---- grid-scroll ----
            RectTransform content = PetSkillKit.Scroll(root, "grid-scroll", out scroll);
            UiKit.Place((RectTransform)content.parent, 0f, y, W, Mathf.Max(10f, gridBottom - y));
            BuildGrid(content, W);

            // ---- equipped-row ----
            BuildEquippedRow(root, W, eqY, eqH);

            // ---- summon-bar ----
            RectTransform bar = UiKit.Box(root, "summon-bar");
            UiKit.Place(bar, 0f, barY, W, barH);
            int mult = H.SummonMult(Kind);
            float xw = PetSkillStyle.Px("x5_w"), xh = PetSkillStyle.Px("x5_h");
            MultButton = MultToggle(bar, mult, W * (1f - PetSkillStyle.L("x5_right_pet_f")) - xw, barH - PetSkillStyle.Rem(0.22f) - xh, xw, xh);
            float sw = PetSkillStyle.Px("summon_btn_w");
            bool ascend = H.AscendReady("pet");
            bool full = P.State.Eggs.Count >= P.Rules.EggCap;
            if (ascend)
                SummonButton = PetSkillKit.PaperButton(bar, "summon-btn", PetSkillKit.BtnKind.Ascend, PetSkillStyle.T("ascend_ready"), PetSkillStyle.T("ascend_sub"), false, () => PetSkillHost.Say(PetSkillStyle.T("ascend_ready")));
            if (ascend) SkillPanel.SummonBtnLh(SummonButton);   // T354 24회차 — 승천 갈래도 `.summon-btn`(5212 1.2 · 나머지 갈래는 SkillPanel.SummonBtn 안에서)
            else if (full)
                SummonButton = SkillPanel.SummonBtn(bar, PetSkillStyle.T("summon_full"), "eggCracked", PetSkillStyle.T("gauge", P.State.Eggs.Count, P.Rules.EggCap), true, OnSummon);
            else
                SummonButton = SkillPanel.SummonBtn(bar, PetSkillStyle.T("summon_x", P.SummonCount(mult)), "eggCracked", JsNum.ToString(P.SummonCost(mult)), !P.CanSummon(mult), OnSummon);
            UiKit.Place(SummonButton.GetComponent<RectTransform>(), (W - sw) * 0.5f, 0f, sw, barH);
            int lvl = P.SummonLevel();
            bool capped = lvl >= H.Data.Balance.Skills.MaxLevel;
            int cnt = P.State.PetSummonCount;
            RatesButton = SkillPanel.SummonInfo(bar, PetSkillStyle.Px("info_left_w") - pad, barH, lvl, capped ? 1f : (cnt % 5) / 5f,
                capped ? PetSkillStyle.T("gauge_max") : PetSkillStyle.T("gauge", cnt % 5, 5), () => SkillRatesPopup.Open(sheet, Kind));

            // ---- hatchery ----
            BuildHatchery(root, W, appW, pad, hatchY, hatchH);
        }

        Button MultToggle(RectTransform bar, int mult, float x, float yTop, float w, float h)
        {
            bool on = mult > 1;
            Button b = UiKit.Button(bar, "x5-toggle", () => H.CycleSummonMult(Kind));
            RectTransform br = b.GetComponent<RectTransform>();
            UiKit.Place(br, x, yTop, w, h);
            RectTransform skin = PetSkillKit.Framed(br, "skin", PetSkillStyle.C(on ? "pp_blue" : "white"), PetSkillStyle.Px("x5_r_rem") * 0.5f + h * 0.25f, PetSkillKit.Line3);
            UiKit.Fill(skin);
            ((Image)skin.Find("line").GetComponent<Image>()).color = PetSkillStyle.C(on ? "pp_line" : "pp_blue");
            TextMeshProUGUI t = PetSkillKit.Text(br, "t", TextKind.Sub, "x" + mult, PetSkillStyle.C(on ? "white" : "pp_blue"));
            UiKit.Fill(t.rectTransform);
            LineHeight.Apply(t, "panel_btn_xs_lh");   // T354 24회차 — 정본 4230 `.panel .btn.xs { line-height: 1.1 }`(x5 토글 3979·4313 은 `.btn.xs` · 5198 은 줄높이를 안 준다)
            return b;
        }

        void BuildGrid(RectTransform content, float W)
        {
            float colW = PetSkillStyle.Px("pet_col_w"), colGap = PetSkillStyle.Px("pet_col_gap_w"), rowGap = PetSkillStyle.Px("sk_row_gap_h");
            float starH = UiCatalog.Instance.Kind(TextKind.Sub).size;
            int cols = 5;
            float gridW = cols * colW + (cols - 1) * colGap;
            float x0 = (W - gridW) * 0.5f;
            float cellH = colW + PetSkillStyle.Rem(0.1f) + starH;
            int n = P.State.Pets.Count + P.State.Eggs.Count;
            GridCells = n;
            float y = 0f;
            if (n == 0)
            {
                TextMeshProUGUI empty = PetSkillKit.Text(content, "grid-empty", TextKind.Sub, PetSkillStyle.T("pets_empty"), PetSkillStyle.C("muted"), TextAlignmentOptions.Center, false);
                UiKit.Place(empty.rectTransform, 0f, PetSkillStyle.Rem(2.4f), W, starH * 1.5f);
                y = PetSkillStyle.Rem(2.4f) * 2f + starH * 1.5f;
            }
            else
            {
                int rows = (n + cols - 1) / cols;
                for (int i = 0; i < n; i++)
                {
                    float cx = x0 + (i % cols) * (colW + colGap), cy = (i / cols) * (cellH + rowGap);
                    if (i < P.State.Pets.Count) petTiles[i] = PetTileAt(content, i, cx, cy, colW, cellH, starH);
                    else eggTiles[i - P.State.Pets.Count] = EggTileAt(content, i - P.State.Pets.Count, cx, cy, colW, cellH, starH);
                }
                y = rows * cellH + (rows - 1) * rowGap + rowGap;
            }
            // 합성 버튼 행(row center wrap)
            string[] rar = Defs.Rarities;
            var mergeable = new List<string>();
            for (int i = 0; i < rar.Length - 1; i++) if (P.CanMerge(rar[i])) mergeable.Add(rar[i]);
            if (mergeable.Count > 0)
            {
                float bh = UiCatalog.Instance.Kind(TextKind.Button).size * 1.6f;
                float bx = 0f;
                float rowW = 0f;
                var widths = new List<float>();
                foreach (string r in mergeable)
                {
                    float bw = PetSkillKit.TextWidth(TextKind.Button, MergeLabel(r)) + PetSkillStyle.Rem(1.2f);
                    widths.Add(bw);
                    rowW += bw + PetSkillStyle.Rem(0.45f);
                }
                bx = Mathf.Max(0f, (W - rowW) * 0.5f);
                for (int k = 0; k < mergeable.Count; k++)
                {
                    string r = mergeable[k];
                    Button b = PetSkillKit.PaperButton(content, "merge-" + r, PetSkillKit.BtnKind.Gray, MergeLabel(r), null, false, () => OnMerge(r), PetSkillStyle.Rem(0.55f));
                    LineHeight.Apply(b.transform.Find("label").GetComponent<TextMeshProUGUI>(), "panel_btn_xs_lh");   // T354 24회차 — 정본 4230(합성 버튼 ui.js 3961 `.btn.xs`)
                    UiKit.Place(b.GetComponent<RectTransform>(), bx, y, widths[k], bh);
                    bx += widths[k] + PetSkillStyle.Rem(0.45f);
                }
                y += bh + rowGap;
            }
            content.sizeDelta = new Vector2(0f, y);
        }

        string MergeLabel(string r)
        {
            int i = System.Array.IndexOf(Defs.Rarities, r);
            return PetSkillStyle.T("merge", Defs.RarityKr.Get(r, r), Defs.RarityKr.Get(Defs.Rarities[i + 1], Defs.Rarities[i + 1]));
        }

        /// <summary>원작 `.pet-tile`(펫): 검정 테 둥근 사각 · 등급색 60% + 흰 40% 바탕 · 3D 얼굴 · «장착됨» 리본 · Lv 배지 · 별.</summary>
        Button PetTileAt(RectTransform parent, int i, float x, float y, float size, float cellH, float starH)
        {
            Pet pet = P.State.Pets[i];
            bool active = P.State.ActivePets.Contains(i);
            Button b = UiKit.Button(parent, "pet-tile-" + i, () => OpenPetDetail(i));
            RectTransform cell = b.GetComponent<RectTransform>();
            UiKit.Place(cell, x, y, size, cellH);
            RectTransform face = TileFace(cell, pet.Name, pet.Rarity, size, active, pet.Level, true);
            // T331 38회차 — 정본 8131 `.pet-tile .tile-face` 의 드리운 그림자 `0 .16rem .3rem rgba(0,0,0,.22)`(표 pettile_drop · 등급색 광 겹은 T419). 공장 TileFace 는 상세 타일(.petd-tile · 규칙 없음)도 만들므로 격자 세 자리가 부르는 쪽에서 건다 · 틀 안 맨 뒤(면·테 뒤 · «face» 이름 찾기는 그대로).
            UiShadow.Drop(face, "pettile_drop", PetSkillStyle.Px("tile_r_rem"), size, size);
            UiKit.Place(face, 0f, 0f, size, size);
            // T355 9회차 — 정본 4304 `.pet-tile:active .tile-face { translateY(.08rem); brightness(1.07) }`(4301 .08s ease-out) · 탈것 격자(MountSheet)·펫 재료 칸과 같은 표 키.
            //   누름은 버튼(.pet-tile)에 붙고 움직이는 것은 얼굴(.tile-face)이다.
            PressFx.Attach(b.gameObject, face, "pet_tile", MountSheet.TileFaceImage(face));
            if (pet.Stars > 0) SkillPanel.StarRow(cell, pet.Stars, size, size + PetSkillStyle.Rem(0.1f), starH);
            return b;
        }

        /// <summary>펫 타일 얼굴 — 상세·업그레이드 팝업도 같은 그림(원작 `.petd-tile` · `.petup-icon`).</summary>
        public RectTransform TileFace(Transform parent, string name, string rarity, float size, bool active, int level, bool ribbonSmall) { return TileFace(parent, name, rarity, size, active, level, ribbonSmall, null, Forge.Game.Gallery.GalleryKind.Pets, null); }

        public RectTransform TileFace(Transform parent, string name, string rarity, float size, bool active, int level, bool ribbonSmall, string ribbonText, Forge.Game.Gallery.GalleryKind kind) { return TileFace(parent, name, rarity, size, active, level, ribbonSmall, ribbonText, kind, null); }

        /// <summary>
        /// 탈것 화면(<see cref="MountSheet"/>)도 같은 타일 — 리본 글(«탑승 중»/«장착됨»)과 종 갈래만 다르다.
        /// <paramref name="lvKeyline"/> 는 **펫 상세 팝업 타일에만** 준다(T109 10회차): 정본 규칙
        /// `.petd-wrap .petd-tile .sk-lv`(style.css 5467)는 `.petd-wrap`(ui.js 4014 `idet-wrap petd-wrap`) **안**에서만 걸리고,
        /// 격자·알·탈것 상세·업그레이드 팝업은 그 밖이라 기본 `.sk-lv`(4045 검정 알약 · 링 없음) 그대로다
        /// (정본 주석 ui.js 4030 «알·탈것 상세(같은 .petd-name)는 .petd-wrap 밖이라…» 가 그 경계를 못 박는다).
        /// </summary>
        /// <param name="faceRadiusKey">얼굴 모서리 표 키 — 안 주면 격자 타일(정본 4262 `.pet-tile .tile-face` .5rem)이고,
        /// 상세 팝업 타일은 `petd_tile_r_rem`(정본 5536 `.petd-tile` **.55rem**)을 준다(T345 19회차).</param>
        public RectTransform TileFace(Transform parent, string name, string rarity, float size, bool active, int level, bool ribbonSmall, string ribbonText, Forge.Game.Gallery.GalleryKind kind, string lvKeyline, string faceRadiusKey = null)
        {
            Color rc = PetSkillStyle.Rarity(Defs, rarity);
            // T371 7회차 — 면 색은 표 ColorMixUi.json 이 쥔다(전엔 PetSkillUi.json tile_face_mix_f · 값은 같다 · 표 하나로):
            //   격자·알·탈것·업그레이드 타일 = 정본 4262 `.pet-tile .tile-face { background: color-mix(--rc 60%, #fff) }` · 테는 ol3 검정(pp_line) 그대로.
            //   펫 상세 타일(정본 `.petd-wrap` 안 · lvKeyline 을 받는 그 자리) = 5461 `--petd-face: color-mix(--rc 60%, #fff)` 에
            //   `border: color-mix(var(--petd-face) 40%, #000)` — 테 색이 등급색이 아니라 **바로 위에서 만든 면 색**에서 잇는 사슬이다(테 폭 1px 은 T365 축 · 여기선 색만).
            bool petd = lvKeyline != null;
            Color faceC = petd ? ColorMixUi.Mix("petd_face", rc) : ColorMixUi.Mix("pet_tile_face", rc);
            // T365 23회차 — 펫 상세 타일(petd)의 테는 정본 5464 `.petd-wrap .petd-tile { border: 1px solid … }` = 1 CSS px(ol1 · `line1_px`) · 격자·알·탈것·업그레이드 타일은 4262 ol3.
            RectTransform face = PetSkillKit.Framed(parent, "tile-face", faceC, PetSkillStyle.Px(faceRadiusKey ?? "tile_r_rem"), petd ? PetSkillStyle.L("line1_px") : PetSkillKit.Line3);
            if (petd) face.Find("line").GetComponent<Image>().color = ColorMixUi.Mix("petd_line", faceC);
            face.sizeDelta = new Vector2(size, size);
            // T371 13회차 — 정본 8124 `.pet-tile .tile-face` 의 **첫 바깥 겹** `0 0 .5rem -.08rem color-mix(in srgb, var(--rc) 60%, transparent)`:
            //   등급색 60% 광(치우침 0). 색은 표 `pet_tile_shadow_2`, 흐림·번짐은 카탈로그. 격자·업그레이드 재료·탈것 격자가 이 공장을 쓰고,
            //   부르는 쪽이 뒤에 드리운 그림자(`pettile_drop`)를 맨 뒤에 깔아 그림자 0 · 광 1(CSS 앞 겹이 위). 펫 상세 타일(petd)은 `.pet-tile` 이 아니다.
            if (!petd)
                UiShadow.Glow(face, "pettile_glow", PetSkillStyle.Px(faceRadiusKey ?? "tile_r_rem"), size, size,
                    UiKit.L("pettile_glow_blur_rem"), UiKit.L("pettile_glow_spread_rem"), ColorMixUi.Mix("pet_tile_shadow_2", rc));
            if (!petd) TilePlate(face, faceC, size, PetSkillKit.Line3);   // T178 46회차 — 정본 8124 `.pet-tile .tile-face` 네 겹(펫 상세 `.petd-tile` 은 `.pet-tile` 이 아니다)
            RectTransform pf = PetSkillKit.PetFace(face, Defs, name, size * 0.86f, kind);
            UiKit.Anchor(pf, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size * 0.86f, size * 0.86f);
            float lvH = UiCatalog.Instance.Kind(TextKind.Sub).size * 1.15f;
            // T178 45회차 — 리본은 한 공장(EquippedRibbon)으로 뺐다: check_surface_gradients 가 `@메서드` 로 겹을 찾는데 TileFace 는 앞 두 오버로드가 한 줄 위임이라 첫 몸이 잡힌다.
            if (active) EquippedRibbon(face, ribbonText ?? PetSkillStyle.T("equipped"), lvH);
            string lt = PetSkillStyle.T("lv_short", level);
            float lw = PetSkillKit.TextWidth(TextKind.Sub, lt) + PetSkillStyle.Rem(0.5f);
            // T109 10회차 — 정본 `.petd-wrap .petd-tile .sk-lv`(style.css 5467): 알약은 그대로 두고 글자에 2.5px 검정 링
            //   («타일 위 Lv 글자도 원본은 흰 채움 + 검정 키라인이다» — 정본 주석의 리본 글자 y=419 스캔 근거).
            RectTransform lv = PetSkillKit.LvBadge(face, lt, lw, lvH, lvKeyline);
            UiKit.Anchor(lv, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, PetSkillStyle.Rem(0.15f)), lw, lvH);
            return face;
        }

        /// <summary>
        /// 펫 타일 면의 네 겹(정본 **8124** `.pet-tile .tile-face` — 4288 을 덮는 마지막 선언 · 14차 «(5) 타일 — 내부 벽 + 썸네일 접지 타원 그림자»):
        /// 왼쪽 위 방사 광(120% 88% at 26% 14%) · 세로 명암(흰 .28 → 검 .20) · 발밑 접지 타원(44% 13% at 50% 84%) · 45° 미세 빗금(.055 · 2/7). 면 크기는 이 자리가 안다(타일 − 테)라
        /// 바로 굽는다(결정 833). 바탕은 면 색(ColorMixUi pet_tile_face). inset 링·아래턱은 box-shadow 축(T331·T365). 격자·알·탈것·업그레이드 재료 타일이 다 이 공장을 지난다. T178 46회차.
        /// </summary>
        static void TilePlate(RectTransform tileFace, Color faceC, float size, float line)
        {
            Image inner = tileFace.Find("face").GetComponent<Image>();
            SurfaceArt.FillFace(inner, "bg-grad", null, SurfaceArt.PetTileLayers, faceC, size - line * 2f, size - line * 2f);
        }

        /// <summary>
        /// 펫 타일 위 «장착» 리본(정본 `.sk-ribbon` 4072 — 검정 알약 + 위 광·아래 그늘 램프 + 낙하 그림자 · 윗변이 면 위 .2rem). T178 45회차에 TileFace 에서 뺐다(내용 그대로).
        /// </summary>
        static RectTransform EquippedRibbon(RectTransform face, string rb, float lvH)
        {
            float rw = PetSkillKit.TextWidth(TextKind.Sub, rb) + PetSkillStyle.Rem(0.5f);
            RectTransform ribbon = PetSkillKit.LvBadge(face, rb, rw, lvH);
            ribbon.name = "sk-ribbon";
            TextMeshProUGUI rbT = ribbon.GetComponentInChildren<TextMeshProUGUI>(true);
            if (rbT != null) WrapUi.Apply(rbT, "sk_ribbon");   // T361 4회차 — 정본 white-space 표(WrapUi.json) 4075 `.sk-ribbon { nowrap }`
            // 정본 `.sk-ribbon{top:-.2rem}` = 리본 **윗변**이 면 위 .2rem — pivot 을 윗변에 둔다(가운데를 두면 반이 면 밖으로 나가 첫 행이 grid-scroll 마스크에 잘린다 · T102)
            UiKit.Anchor(ribbon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, PetSkillStyle.Rem(0.2f)), rw, lvH);
            // T331 33회차 — 정본 4079 `.sk-ribbon` 의 **둘째 겹** `0 .1rem .18rem rgba(0,0,0,.35)`
            //   (첫 겹 `inset 0 1px 0 rgba(255,255,255,.25)` 은 안쪽 림라이트다 · 정본이 여기서 1px 을 썼다).
            //   반지름은 리본이 실제로 쓰는 것을 그림에서 되읽는다(그 공장을 안 열어도 된다 · T331 5회차).
            UiShadow.Drop(ribbon, "skribbon_drop", UiShadow.RadiusOf(ribbon), rw, lvH);
            // T178 45회차 — 정본 **4072** `.sk-ribbon { background-image: linear-gradient(180deg, 흰 .22 0, 투명 48%, 검 .28 100%) }`(정본 주석 «민짜 검정 스티커 → 위 광·림 + 아래 그늘 · 칠 속성만»):
            //   바탕 #17181a(PetSkillUi ink) 위에 표 sk_ribbon 을 얹는다 — 정지점이 분수라 크기 무관 · 둥근 알약이라 FillMasked(면 색 위 합성). 5546 `.petd-tile .sk-ribbon` 은 글자·패딩만 바꾼다.
            SurfaceArt.FillMasked(ribbon.Find("bg").GetComponent<Image>(), "bg-grad", "sk_ribbon", rw, lvH, PetSkillStyle.C("ink"));
            return ribbon;
        }

        /// <summary>원작 `.pet-tile.egg`: 테 없이 등급색 알 그림(119%) + «알» 라벨.</summary>
        Button EggTileAt(RectTransform parent, int i, float x, float y, float size, float cellH, float starH)
        {
            Egg egg = P.State.Eggs[i];
            Button b = UiKit.Button(parent, "egg-tile-" + i, () => OpenEggDetail(i));
            RectTransform cell = b.GetComponent<RectTransform>();
            UiKit.Place(cell, x, y, size, cellH);
            Image ico = UiKit.Icon(cell, "egg", "egg", PetSkillStyle.RarityHex(Defs, egg.Rarity));
            float es = size * 1.19f;
            UiKit.Anchor(ico.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, (es - size) * 0.5f), es, es);
            // T332 16회차 — 정본 **4307** `.pet-tile.egg .tile-face { … filter: drop-shadow(0 .15rem .1rem rgba(0,0,0,.3)) }`.
            //   같은 줄의 정본 주석이 까닭을 못박았다: «(정본이 경고 표식을 붙여 둔 줄) 알 타일은 배경·테·그림자가 전부 없는 **그림만** 칸이다» — 판이 없으니
            //   알을 바닥에서 띄우는 것이 **이 그림자뿐**이다(없으면 격자에 붙어 스티커처럼 읽힌다).
            DropShadow.Apply(ico, "pet_tile_egg");
            // T355 9회차 — 정본 4307 `.pet-tile.egg .tile-face` 는 배경·테·그림자만 지우고 :active(4304)는 그대로다 — 알 타일의 «얼굴» 은 알 그림이라 거기에 건다(8회차 알 재료 칸과 같은 꼴).
            PressFx.Attach(b.gameObject, ico.rectTransform, "pet_tile", ico);
            TextMeshProUGUI lab = PetSkillKit.Text(cell, "tile-label", TextKind.Sub, PetSkillStyle.T("egg"), PetSkillStyle.C("ink"));
            UiKit.Place(lab.rectTransform, 0f, size + PetSkillStyle.Rem(0.1f), size, starH);
            return b;
        }

        void BuildEquippedRow(RectTransform parent, float W, float yTop, float eqH)
        {
            float w = W * PetSkillStyle.L("equipped_w_f");
            RectTransform row = PetSkillKit.Framed(parent, "equipped-row", PetSkillStyle.C("equipped_bg"), PetSkillStyle.Px("equipped_r_rem"), PetSkillKit.Line3);
            // T178 44회차 — 정본 8415 `.league-row:not(.me), .equipped-row`: 장착됨 바를 리그 행과 같은 어두운 판 처방(옅은 흰 결 + 위 광 → 아래 검 그늘)에 편입 — 면 위 한 판(하드 그림자는 T331 축).
            SurfaceArt.FillFaceWhenSized(row.Find("face").GetComponent<Image>(), "bg-grad", SurfaceArt.LeagueRowLayers, PetSkillStyle.C("equipped_bg"));
            UiShadow.Drop(row, "equipped_lip", PetSkillStyle.Px("equipped_r_rem"));   // 정본 .equipped-row(4128) `0 .22rem 0 rgba(0,0,0,.35)`
            UiKit.Place(row, (W - w) * 0.5f, yTop, w, eqH);
            SkillPanel.EquippedLabel(row, eqH);
            float mini = PetSkillStyle.Px("mini_rem");
            float g = PetSkillStyle.Px("mini_gap_rem");
            float padX = PetSkillStyle.Px("equipped_pad_x_rem");
            var ids = P.State.ActivePets;
            if (ids.Count == 0)
            {
                TextMeshProUGUI none = PetSkillKit.Text(row, "none", TextKind.Sub, PetSkillStyle.T("none"), PetSkillStyle.C("muted"), TextAlignmentOptions.Right, false);
                UiKit.Place(none.rectTransform, w * 0.4f, 0f, w * 0.6f - padX, eqH);
                return;
            }
            float x = w - padX - ids.Count * mini - (ids.Count - 1) * g;
            for (int k = 0; k < ids.Count; k++)
            {
                int i = ids[k];
                if (i < 0 || i >= P.State.Pets.Count) continue;
                Pet pet = P.State.Pets[i];
                Button b = UiKit.Button(row, "sk-mini-" + i, () => OpenPetDetail(i));
                RectTransform br = b.GetComponent<RectTransform>();
                UiKit.Place(br, x + k * (mini + g), (eqH - mini) * 0.5f, mini, mini);
                RectTransform sq = PetSkillKit.Framed(br, "sq", PetSkillStyle.Rarity(Defs, pet.Rarity), PetSkillStyle.Px("mini_r_rem"), PetSkillKit.Line2);
                UiKit.Fill(sq);
                PetSkillKit.MiniPlate(sq, PetSkillStyle.Rarity(Defs, pet.Rarity), mini, PetSkillKit.Line2);   // T178 46회차 — 정본 4150 `.sk-mini` 방사 둘(광·그늘)
                RectTransform pf = PetSkillKit.PetFace(sq, Defs, pet.Name, mini * 0.86f);
                UiKit.Anchor(pf, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, mini * 0.86f, mini * 0.86f);
                SkillPanel.MiniLv(br, PetSkillStyle.T("lv_short", pet.Level), mini);
            }
        }

        // ===== 부화장 =====
        void BuildHatchery(RectTransform parent, float W, float appW, float pad, float yTop, float hatchH)
        {
            RectTransform hatch = UiKit.Box(parent, "hatchery");
            UiKit.Place(hatch, -pad, yTop, appW, hatchH);
            Image bg = UiKit.Panel(hatch, "bg", "pp_paper");
            bg.color = PetSkillStyle.C("hatchery_bg");
            UiKit.Line(hatch, "line", "pp_line", PetSkillKit.Line3, true);
            // ◀
            float bl = PetSkillStyle.Px("hatch_back_left_rem"), bb = PetSkillStyle.Px("hatch_back_bottom_rem");
            BackButton = SkillPanel.BackButtonAt(hatch, bl, hatchH - bb - PetSkillStyle.Px("back_h"));
            // hatch-row
            int slots = P.MaxHatchSlots();
            float rowW = PetSkillStyle.Px("hatch_row_w");
            float rowX = PetSkillStyle.Px("hatch_row_left_w");
            float cellW = PetSkillStyle.Px("hatch_cell_w");
            float cellH = PetSkillStyle.Px("hatch_cell_empty_h_rem");   // 정본 4516 .hatch-cell.empty min-height 8.6rem (T402 · 표)
            float rowY = (hatchH - cellH) * 0.5f;
            for (int i = 0; i < slots; i++)
            {
                RectTransform cell = UiKit.Box(hatch, "hatch-cell-" + i);
                UiKit.Place(cell, rowX + i * cellW, rowY, cellW, cellH);
                HatchSlot h = i < P.State.Hatching.Count ? P.State.Hatching[i] : null;
                BuildHatchCell(cell, i, h, cellW, cellH);
            }
            // slot-buy
            if (P.CanBuySlot())
            {
                float bx = rowX + rowW + PetSkillStyle.Px("slot_buy_left_w");
                float lw = PetSkillStyle.Px("slot_buy_w"), lh = PetSkillStyle.Px("slot_buy_h");
                float labH = UiCatalog.Instance.Kind(TextKind.Sub).size * 1.1f;
                float gapY = PetSkillStyle.Px("slot_buy_gap_w");
                float total = labH + gapY + lh;
                float by = rowY + (cellH - total) * 0.5f + PetSkillStyle.Px("slot_buy_top_w");
                TextMeshProUGUI lab = PetSkillKit.Stroked(hatch, "slot-buy-label", TextKind.Sub, PetSkillStyle.T("slot_plus"), PetSkillStyle.C("white"), "slot_buy_label");   // 정본 .slot-buy-label var(--ol3)
                WrapUi.Apply(lab, "slot_buy_label");
                LineHeight.Apply(lab, "slot_buy_label_lh");   // T354 24회차 — 정본 4558 `.slot-buy-label { line-height: 1 }`   // T361 4회차 — 정본 white-space 표(WrapUi.json) 4558 `.slot-buy-label { nowrap }`
                UiKit.Place(lab.rectTransform, bx - lw * 0.5f, by, lw * 2f, labH);
                SlotBuyButton = PetSkillKit.PaperButton(hatch, "slot-buy", PetSkillKit.BtnKind.Gray, string.Empty, null, false, OnBuySlot, PetSkillStyle.Px("slot_buy_r_rem"));   // T415 9회차 — 정본 4565 `.hatchery .slot-buy { border-radius: .55rem }` · 표 PetSkillUi.json(종전 코드에 박힌 0.55)
                RectTransform br = SlotBuyButton.GetComponent<RectTransform>();
                UiKit.Place(br, bx, by + labH + gapY, lw, lh);
                float ico = PetSkillStyle.Px("slot_buy_icon_w");
                string cost = JsNum.ToString(P.SlotCost());
                float tw = PetSkillKit.TextWidth(TextKind.Sub, cost);
                // T364 6회차 — 정본 4563 `.hatchery .slot-buy { gap: calc(var(--app-w) * .0143) }`(젬 ↔ 빨강 숫자).
                // 종전 `Rem(0.15f)`(= 5.46px@1080)는 정본(15.44px)의 3분의 1이라 알약 안이 촘촘했다 — 축도 앱 높이 기준이었다.
                float g = PetSkillStyle.Px("slot_buy_ico_gap_w");
                float cx = (lw - ico - g - tw) * 0.5f;
                Image gi = UiKit.Icon(br, "ico", "gem");
                UiKit.Place(gi.rectTransform, cx, (lh - ico) * 0.5f, ico, ico);
                // T396 13회차 — 정본 4566 `.hatchery .slot-buy b { color: #e8112d }`. 종전엔 `cost_red`(#f2191d)를 썼는데
                //   그 값은 소환 버튼 값(8668 `.summon-btn .summon-cost b`)의 빨강이다 — 정본이 두 자리를 **다른 리터럴**로 못박았다.
                TextMeshProUGUI ct = PetSkillKit.Text(br, "cost", TextKind.Sub, cost, PetSkillStyle.C("slot_buy_cost_ink"), TextAlignmentOptions.Left);
                LineHeight.Apply(ct, "hatchery_slot_buy_lh");   // T354 24회차 — 정본 4565 `.hatchery .slot-buy { line-height: 1 }`(4230 `.panel .btn.xs` 1.1 을 뒤 선언이 덮는다)
                UiKit.Place(ct.rectTransform, cx + ico + g, 0f, tw + ico, lh);
            }
        }

        void BuildHatchCell(RectTransform cell, int i, HatchSlot h, float w, float cellH)
        {
            // 램프(원작 .hatch-lamp): 기둥 + 갓 + 전구
            // ⚠ 갓·전구 높이는 정본이 **앱 폭** 기준(`calc(var(--app-w) * .0367)`)이라 `_w` 접미다 — `_h` 로 두면 앱 높이를 곱해 1.78배 길어진다(T102 · T95 `sk_eqplate_h` 와 같은 함정)
            float lw = PetSkillStyle.Px("lamp_w"), lh = PetSkillStyle.Px("lamp_h_w");
            // T178 46회차 — 정본 **8026** `.hatch-cell { background-image: radial-gradient(46% 10% at 50% 86%, rgba(255,235,80,.28) 0, 투명 100%) }`(14차 «(6) 스포트라이트 — 바닥 착지점 라디얼 글로우»):
            //   빛기둥이 닿는 바닥의 노란 타원 · 바탕 없는 알파 겹이라 칸 크기로 굽고(표 hatch_cell_glow) CSS 배경답게 **첫 자식**으로 둔다(램프·원뿔·알·글자 뒤).
            SurfaceArt.Fill(cell, "bg-grad", "hatch_cell_glow", w, cellH).transform.SetAsFirstSibling();
            RectTransform lamp = UiKit.Box(cell, "hatch-lamp");
            UiKit.Anchor(lamp, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, lw, lh);
            Image stem = UiKit.Panel(lamp, "stem", "pp_paper");
            stem.color = PetSkillStyle.C("lamp");
            UiKit.Anchor(stem.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), Vector2.zero, PetSkillStyle.Px("lamp_stem_w_rem"), PetSkillStyle.Px("lamp_stem_h_rem"));
            // 갓 = 반타원 돔(정본 `border-radius: 50% 50% .1rem .1rem / 100% 100% .1rem .1rem`) — 알약(반지름 h/2)이 아니다(T102)
            PetHatchCone shade = PetHatchCone.Add(lamp, "shade", PetSkillStyle.C("lamp"), PetSkillStyle.C("lamp"), 1f, PetHatchCone.Kind.Dome);
            UiKit.Fill(shade.rectTransform);
            Image bulb = PetSkillKit.Disc(lamp, "bulb", PetSkillStyle.C("lamp_bulb"));
            bulb.preserveAspect = false;
            UiKit.Anchor(bulb.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, -PetSkillStyle.Px("lamp_bulb_drop_w") + PetSkillStyle.Px("lamp_bulb_h_w") * 0.5f), PetSkillStyle.Px("lamp_bulb_w"), PetSkillStyle.Px("lamp_bulb_h_w"));
            // 빛기둥
            float cw = PetSkillStyle.Px("cone_w"), ch = PetSkillStyle.Px("cone_h");
            float coneTop = PetSkillStyle.Px("cone_top_rem") + PetSkillStyle.Px("cone_top_h");
            bool dim = h == null;
            // 위 변 = 38%~62% → 폭 비율 .24(정본 clip-path) · 구운 스프라이트(T102 — 맨 Graphic 은 이 레포에서 안 칠해진다)
            PetHatchCone cone = PetHatchCone.Add(cell, "hatch-cone", PetSkillStyle.C(dim ? "cone_dim_top" : "cone_top"), PetSkillStyle.C(dim ? "cone_dim_bottom" : "cone_bottom"), 1f - PetSkillStyle.L("cone_top_f") * 2f, 0f);
            UiKit.Anchor(cone.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -coneTop), cw, ch);
            hatchCones.Add(cone);
            float top = PetSkillStyle.Px("hatch_cell_top_rem");
            if (dim)
            {
                TextMeshProUGUI hint = PetSkillKit.Text(cell, "hatch-hint", TextKind.Sub, PetSkillStyle.T("empty_slot"), PetSkillStyle.C("hatch_hint"), TextAlignmentOptions.Center, false);
                float hh = UiCatalog.Instance.Kind(TextKind.Sub).size * 1.2f;
                UiKit.Place(hint.rectTransform, 0f, cellH - hh, w, hh);
                return;
            }
            float egg = PetSkillStyle.Px("hatch_egg_rem");
            Image ei = UiKit.Icon(cell, "hatch-egg", "egg", PetSkillStyle.RarityHex(Defs, h.Rarity));
            float ey = top + PetSkillStyle.Rem(0.5f);
            UiKit.Place(ei.rectTransform, (w - egg) * 0.5f, ey, egg, egg);
            // T332 16회차 — 정본 **4518** `.hatch-cell .hatch-egg { … filter: drop-shadow(0 .15rem .12rem rgba(0,0,0,.45)) }`.
            //   격자 알(4307 · .3/.1rem)보다 **더 진하고 더 번진다** — 부화 원뿔의 밝은 빛기둥(4515) 위에 서기 때문이다. 그래서 한 키로 안 묶는다.
            DropShadow.Apply(ei, "hatch_egg");
            float th = UiCatalog.Instance.Kind(TextKind.Sub).size * 1.2f;
            TextMeshProUGUI time = PetSkillKit.Stroked(cell, "hatch-time", TextKind.Sub, PetSkillStyle.FmtTime((h.EndsAt - H.Now()) / 1000), PetSkillStyle.C("white"), "hatch_time");   // 정본 .hatch-cell .hatch-time var(--ol3)
            WrapUi.Apply(time, "hatch_cell_hatch_time");   // T361 4회차 — 정본 white-space 표(WrapUi.json) 4525 `.hatch-cell .hatch-time { nowrap }`
            UiKit.Place(time.rectTransform, -w * 0.25f, ey + egg + PetSkillStyle.Px("hatch_time_top_rem"), w * 1.5f, th);
            hatchTimes.Add(time);
            // 💎 스킵(xs · 오른쪽 위)
            string cost = JsNum.ToString(P.GemSkipCost(h));
            float ico = UiCatalog.Instance.Kind(TextKind.Sub).size * 0.9f;
            float tw = PetSkillKit.TextWidth(TextKind.Sub, cost);
            float bw = ico + tw + PetSkillStyle.Rem(0.6f), bh = UiCatalog.Instance.Kind(TextKind.Sub).size * 1.35f;
            int idx = i;
            Button skip = PetSkillKit.PaperButton(cell, "hatch-skip", PetSkillKit.BtnKind.Gray, string.Empty, null, false, () => OnHatchSkip(idx), PetSkillStyle.Rem(0.4f));
            RectTransform sr = skip.GetComponent<RectTransform>();
            UiKit.Anchor(sr, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-PetSkillStyle.Px("hatch_skip_right_rem"), -PetSkillStyle.Px("hatch_skip_top_rem") - top * 0.55f), bw, bh);
            Image gi = UiKit.Icon(sr, "ico", "gem");
            UiKit.Place(gi.rectTransform, PetSkillStyle.Rem(0.2f), (bh - ico) * 0.5f, ico, ico);
            TextMeshProUGUI ct = PetSkillKit.Text(sr, "cost", TextKind.Sub, cost, PetSkillStyle.C("ink"), TextAlignmentOptions.Left);
            LineHeight.Apply(ct, "hatch_cell_btn_xs_lh");   // T354 24회차 — 정본 4535 `.hatch-cell > .btn.xs { line-height: 1.1 }`(4230 과 같은 값 · 뒤 선언이 이긴다)
            UiKit.Place(ct.rectTransform, PetSkillStyle.Rem(0.2f) + ico + PetSkillStyle.Rem(0.08f), 0f, tw + ico, bh);
            skipButtons.Add(skip);
            skipCosts.Add(ct);
        }

        // ===== 동작(원작 on*) =====

        void OnSummon()
        {
            int count = H.SummonMult(Kind);
            SummonResult r = P.Summon(count);
            if (r == null)
            {
                PetSkillHost.Say(P.EggSpace() < 1 ? PetSkillStyle.T("toast_egg_full", P.State.Eggs.Count, P.Rules.EggCap) : PetSkillStyle.T("toast_egg_short"));
                return;
            }
            if (r.Clamped) PetSkillHost.Say(PetSkillStyle.T("toast_clamped", r.Summoned, P.State.Eggs.Count, P.Rules.EggCap));
            H.Sync();
            H.Save();
            SkillSummonResultView.Open(sheet, Kind, r, OnSummon);
        }

        public void OnStartHatch(int i)
        {
            if (!P.StartHatch(i)) { PetSkillHost.Say(PetSkillStyle.T("toast_hatch_full", P.MaxHatchSlots())); return; }
            sheet.Modal.Close(DetailModal);
            H.Sync();
            H.Save();
        }

        public void OnHatchSkip(int i)
        {
            List<HatchResult> r = P.GemSkip(i);
            if (r == null) { PetSkillHost.Say(PetSkillStyle.T("toast_gems_short")); H.Sync(); return; }
            H.Announce(r);
            H.Save();
        }

        void OnBuySlot()
        {
            if (!P.BuySlot()) { PetSkillHost.Say(PetSkillStyle.T("toast_gems_short")); return; }
            PetSkillHost.Say(PetSkillStyle.T("toast_slot_bought", P.MaxHatchSlots()));
            H.Sync();
            H.Save();
        }

        public void OnTogglePet(int i)
        {
            if (!P.CanActivate(i) && i >= 0 && i < P.State.Pets.Count)
            {
                PetSkillHost.Say(PetSkillStyle.T("toast_pet_max", P.Rules.MaxActive));
                return;
            }
            if (!P.ToggleActive(i)) PetSkillHost.Say(PetSkillStyle.T("toast_pet_missing"));
            H.RequestRecalc();
            H.Sync();
            H.Save();
        }

        void OnMerge(string r)
        {
            string next;
            MergeOutcome o = P.Merge(r, out next);
            if (o == MergeOutcome.EggCapFull) PetSkillHost.Say(PetSkillStyle.T("toast_merge_full"));
            else if (o == MergeOutcome.Merged) PetSkillHost.Say(PetSkillStyle.T("toast_merge_ok", Defs.RarityKr.Get(next, next)));
            H.Sync();
            H.Save();
        }

        // ===== openPetDetail (UI-SPEC 54 · shot-042449) =====
        public const string DetailModal = "pet-detail";

        public void OpenPetDetail(int i)
        {
            if (i < 0 || i >= P.State.Pets.Count) return;
            Pet pet = P.State.Pets[i];
            bool active = P.State.ActivePets.Contains(i);
            Big atk, hp;
            P.PetPower(pet, out atk, out hp);
            bool maxed = pet.Level >= P.Rules.MaxLevel;
            float wf = PetSkillStyle.L("petd_w_f");
            float w = wf * UiKit.RefW;
            float padX = PetSkillStyle.Px("petd_pad_x_rem"), padT = PetSkillStyle.Px("petd_pad_top_rem"), padB = PetSkillStyle.Px("petd_pad_bottom_rem");
            float inner = w - padX * 2f;
            float tile = inner * PetSkillStyle.L("petd_tile_w_f");
            float starH = UiCatalog.Instance.Kind(TextKind.Sub).size;
            float body = UiCatalog.Instance.Kind(TextKind.Body).size;
            int subLines = Mathf.Max(1, pet.Subs != null ? pet.Subs.Count : 0);
            float headH = Mathf.Max(tile + PetSkillStyle.Rem(0.25f) + starH, body * 1.3f + body * 1.35f * 2f + PetSkillStyle.Rem(0.45f) + starH * 1.45f * subLines);
            float btnH = PetSkillStyle.Px("petd_btn_h_rem");
            float h = padT + headH + PetSkillStyle.Px("petd_head_bottom_rem") + btnH + padB;
            PetSkillModal.Handle m = sheet.Modal.Open(DetailModal, wf, h, PetSkillStyle.L("petd_top_rem"));
            RectTransform c = m.Content;
            // 공유 더미(파란 클립보드)
            float sh = PetSkillStyle.Px("petd_share_rem"), so = PetSkillStyle.Px("petd_share_off_rem");
            Button share = UiKit.Button(c, "petd-share", () => PetSkillHost.Say(PetSkillStyle.T("share_stub")));
            RectTransform shr = share.GetComponent<RectTransform>();
            UiKit.Anchor(shr, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-so, -so), sh, sh);
            RectTransform sk = PetSkillKit.Framed(shr, "skin", PetSkillStyle.C("pp_blue"), PetSkillStyle.Px("petd_share_r_rem"), PetSkillKit.Line2);
            UiKit.Fill(sk);
            Image clip = UiKit.Icon(shr, "ico", "clipboard");
            clip.rectTransform.offsetMin = new Vector2(sh * 0.2f, sh * 0.2f);
            clip.rectTransform.offsetMax = new Vector2(-sh * 0.2f, -sh * 0.2f);
            // head
            float y = padT;
            RectTransform tilecol = UiKit.Box(c, "petd-tilecol");
            UiKit.Place(tilecol, padX, y, tile, headH);
            // 이 자리만 정본 `.petd-wrap`(ui.js 4014) 안이다 → Lv 글자에 2.5px 링(style.css 5467 · T109 10회차)
            RectTransform face = TileFace(tilecol, pet.Name, pet.Rarity, tile, active, pet.Level, false, null, Forge.Game.Gallery.GalleryKind.Pets, "petd_tile_lv", "petd_tile_r_rem");
            UiKit.Place(face, 0f, 0f, tile, tile);
            if (pet.Stars > 0) SkillPanel.StarRow(tilecol, pet.Stars, tile, tile + PetSkillStyle.Rem(0.25f), starH);
            float bx = padX + tile + PetSkillStyle.Px("petd_head_gap_rem");
            float bw = inner - tile - PetSkillStyle.Px("petd_head_gap_rem");
            float by = y + PetSkillStyle.Rem(0.1f);
            TextMeshProUGUI name = PetSkillKit.Stroked(c, "petd-name", TextKind.Body, PetSkillStyle.T("pet_name", Defs.RarityKr.Get(pet.Rarity, pet.Rarity), Defs.PetKr.Get(pet.Name, pet.Name)), PetSkillStyle.Rarity(Defs, pet.Rarity), "petd_name", TextAlignmentOptions.Left);   // 정본 .petd-wrap .petd-name max(1.2px, .125em)
            UiKit.Place(name.rectTransform, bx, by, bw, body * 1.3f);
            by += body * 1.3f + PetSkillStyle.Rem(0.05f);
            TextMeshProUGUI st1 = PetSkillKit.Text(c, "petd-atk", TextKind.Body, PetSkillStyle.T("dmg", PetSkillStyle.Fmt(atk)), PetSkillStyle.C("black"), TextAlignmentOptions.Left);
            UiKit.Place(st1.rectTransform, bx, by, bw, body * 1.35f);
            by += body * 1.35f;
            TextMeshProUGUI st2 = PetSkillKit.Text(c, "petd-hp", TextKind.Body, PetSkillStyle.T("hp", PetSkillStyle.Fmt(hp)), PetSkillStyle.C("black"), TextAlignmentOptions.Left);
            UiKit.Place(st2.rectTransform, bx, by, bw, body * 1.35f);
            by += body * 1.35f + PetSkillStyle.Rem(0.45f);
            if (pet.Subs == null || pet.Subs.Count == 0)
            {
                TextMeshProUGUI ns = PetSkillKit.Text(c, "petd-subs", TextKind.Sub, PetSkillStyle.T("no_subs"), PetSkillStyle.C("subs_ink"), TextAlignmentOptions.Left);
                UiKit.Place(ns.rectTransform, bx, by, bw, starH * 1.45f);
            }
            else
                for (int k = 0; k < pet.Subs.Count; k++)
                {
                    TextMeshProUGUI s = PetSkillKit.Text(c, "petd-sub-" + k, TextKind.Sub, PetSkillStyle.SubText(pet.Subs[k]), PetSkillStyle.C("subs_ink"), TextAlignmentOptions.Left);
                    UiKit.Place(s.rectTransform, bx, by + k * starH * 1.45f, bw, starH * 1.45f);
                }
            // buttons
            float btnY = padT + headH + PetSkillStyle.Px("petd_head_bottom_rem");
            float bgap = PetSkillStyle.Px("petd_btn_gap_rem"), bpad = PetSkillStyle.Px("petd_btn_pad_x_rem");
            float ew = (w - bpad * 2f - bgap) * 0.5f;
            Button up = maxed
                ? PetSkillKit.PaperButton(c, "btn-upgrade", PetSkillKit.BtnKind.Primary, PetSkillStyle.T("upgrade"), PetSkillStyle.T("max_level", P.Rules.MaxLevel), true, null)
                : PetSkillKit.PaperButton(c, "btn-upgrade", PetSkillKit.BtnKind.Primary, PetSkillStyle.T("upgrade"), null, false, () => { sheet.Modal.Close(DetailModal); PetUpgradePopup.Open(sheet, i); });
            UiKit.Place(up.GetComponent<RectTransform>(), bpad, btnY, ew, btnH);
            Button tg;
            if (active || P.CanActivate(i))
                tg = PetSkillKit.PaperButton(c, "btn-toggle", active ? PetSkillKit.BtnKind.Danger : PetSkillKit.BtnKind.Primary, PetSkillStyle.T(active ? "remove" : "equip"), null, false, () => { OnTogglePet(i); OpenPetDetail(i); });
            else
                tg = PetSkillKit.PaperButton(c, "btn-toggle", PetSkillKit.BtnKind.Primary, PetSkillStyle.T("equip"), PetSkillStyle.T("max_active", P.Rules.MaxActive), true, () => OnTogglePet(i));
            UiKit.Place(tg.GetComponent<RectTransform>(), bpad + ew + bgap, btnY, ew, btnH);
        }

        // ===== openEggDetail — 클릭 즉시 부화 금지 · [부화] 버튼으로만 =====
        public void OpenEggDetail(int i)
        {
            if (i < 0 || i >= P.State.Eggs.Count) return;
            Egg egg = P.State.Eggs[i];
            bool slotsFull = P.State.Hatching.Count >= P.MaxHatchSlots();
            double hatchSec = P.HatchTimeSec(egg.Rarity);
            float wf = PetSkillStyle.L("eggd_w_f");
            float w = wf * UiKit.RefW;
            float padX = PetSkillStyle.Px("petd_pad_x_rem"), padT = PetSkillStyle.Px("petd_pad_top_rem"), padB = PetSkillStyle.Px("eggd_pad_bottom_rem");
            float inner = w - padX * 2f;
            float tile = inner * PetSkillStyle.L("petd_tile_w_f");
            float body = UiCatalog.Instance.Kind(TextKind.Body).size;
            float starH = UiCatalog.Instance.Kind(TextKind.Sub).size;
            float headH = Mathf.Max(tile, body * 1.3f + body * 1.35f + PetSkillStyle.Rem(0.45f) + starH * 1.45f);
            float btnH = PetSkillStyle.Px("petd_btn_h_rem") * 0.8f;
            float h = padT + headH + PetSkillStyle.Rem(1f) + btnH + padB;
            PetSkillModal.Handle m = sheet.Modal.Open(DetailModal, wf, h, PetSkillStyle.L("eggd_top_rem"));
            RectTransform c = m.Content;
            float y = padT;
            RectTransform tilecol = UiKit.Box(c, "petd-tilecol");
            UiKit.Place(tilecol, padX, y, tile, tile);
            Image ei = UiKit.Icon(tilecol, "egg", "egg", PetSkillStyle.RarityHex(Defs, egg.Rarity));
            float es = tile * 0.82f;
            UiKit.Anchor(ei.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, es, es);
            float bx = padX + tile + PetSkillStyle.Px("eggd_head_gap_rem");
            float bw = inner - tile - PetSkillStyle.Px("eggd_head_gap_rem");
            float by = y + PetSkillStyle.Rem(0.1f);
            TextMeshProUGUI name = PetSkillKit.Stroked(c, "petd-name", TextKind.Body, PetSkillStyle.T("egg_name", Defs.RarityKr.Get(egg.Rarity, egg.Rarity)), PetSkillStyle.Rarity(Defs, egg.Rarity), "petd_name", TextAlignmentOptions.Left);   // 정본 .petd-wrap .petd-name max(1.2px, .125em)
            UiKit.Place(name.rectTransform, bx, by, bw, body * 1.3f);
            by += body * 1.3f + PetSkillStyle.Rem(0.05f);
            TextMeshProUGUI st = PetSkillKit.Text(c, "petd-stats", TextKind.Body, PetSkillStyle.T("hatch_time", PetSkillStyle.FmtTime(hatchSec)), PetSkillStyle.C("black"), TextAlignmentOptions.Left);
            UiKit.Place(st.rectTransform, bx, by, bw, body * 1.35f);
            by += body * 1.35f + PetSkillStyle.Rem(0.45f);
            TextMeshProUGUI sub = PetSkillKit.Text(c, "petd-subs", TextKind.Sub, slotsFull ? PetSkillStyle.T("hatch_full", P.MaxHatchSlots()) : PetSkillStyle.T("hatch_using", P.State.Hatching.Count, P.MaxHatchSlots()), PetSkillStyle.C("subs_ink"), TextAlignmentOptions.Left);
            UiKit.Place(sub.rectTransform, bx, by, bw, starH * 1.45f);
            float btnY = padT + headH + PetSkillStyle.Rem(1f);
            float bpad = PetSkillStyle.Px("petd_btn_pad_x_rem");
            Button hatchBtn = PetSkillKit.PaperButton(c, "btn-hatch", PetSkillKit.BtnKind.Primary, PetSkillStyle.T("hatch"), null, slotsFull, () => OnStartHatch(i));
            UiKit.Place(hatchBtn.GetComponent<RectTransform>(), bpad, btnY, w - bpad * 2f, btnH);
        }
    }
}
