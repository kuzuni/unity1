using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.Skills;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 스킬 서브탭(원작 ui.js renderSkills · openSkillDetail · onSummon · onUpgradeSkill · onUpgradeAllSkills · onQuickEquipSkills · UI-SPEC 8·11·14 · shot-042340/042426).
    /// 위→아래: 시트 머리(티켓 알약 · «스킬 N/18») → 패시브 배너 → 5열 원형 오브 그리드(Lv · 별 · 조각 게이지 · 장착 오브는 어둡게 + «장착됨» 타원) →
    /// «장착됨» 행(미니 오브 3) → [모두 업그레이드][빠른 장착] → 소환 바(◀ · xN · 소환 xN 🎫비용 · i Lv.N 게이지).
    /// 규칙은 전부 Core <see cref="SkillSystem"/> · 상태 변경 뒤 <see cref="PetSkillHost.Sync"/>.
    /// </summary>
    public sealed class SkillPanel : MonoBehaviour
    {
        SkillPetSheet sheet;
        float height;
        RectTransform root;
        ScrollRect scroll;
        public const string Kind = "skill";

        PetSkillHost H { get { return sheet.Host; } }
        SkillSystem Sk { get { return H.Skills; } }
        GameDefs Defs { get { return H.Data.Defs; } }

        public Button SummonButton { get; private set; }
        public Button MultButton { get; private set; }
        public Button RatesButton { get; private set; }
        public Button UpgradeAllButton { get; private set; }
        public Button QuickEquipButton { get; private set; }
        public Button BackButton { get; private set; }
        public string Title { get; private set; }
        public int GridCells { get; private set; }
        readonly Dictionary<string, Button> cellButtons = new Dictionary<string, Button>();
        public Button CellButton(string id) { Button b; return cellButtons.TryGetValue(id, out b) ? b : null; }

        public void Init(SkillPetSheet s, float h)
        {
            sheet = s;
            height = h;
            root = (RectTransform)transform;
        }

        // ===== renderSkills =====
        public void Render()
        {
            if (!PetSkillHost.Ready) return;
            PetSkillKit.Clear(root);
            cellButtons.Clear();
            float W = root.rect.width > 0f ? root.rect.width : UiKit.RefW - PetSkillStyle.Px("pad_rem") * 2f;
            float gap = PetSkillStyle.Px("gap_rem");
            float y = 0f;

            int lvl = Sk.SummonLevel();
            PassiveBonus pb = Sk.OwnedPassive();
            int mult = H.SummonMult(Kind);
            bool capped = lvl >= Sk.Rules.SummonLevelCap;

            // ---- sheet-head ----
            float headH = PetSkillStyle.Px("head_h_rem");
            RectTransform head = UiKit.Box(root, "sheet-head");
            UiKit.Place(head, 0f, y, W, headH);
            Title = PetSkillStyle.T("skills_title", Sk.State.Skills.Count, Defs.SkillDefs.Count);
            TextMeshProUGUI title = PetSkillKit.Stroked(head, "sheet-title", TextKind.Title, Title, PetSkillStyle.C("white"), "sheet_title");   // 정본 h2.sheet-title .11em
            UiKit.TextShadow(title, "paper_emboss");   // T333 2회차 — 정본 8381 한 벌 «밝은 종이 위 글자는 흰 엠보스»(0 1px 0 rgba(255,255,255,.92))
            UiKit.Fill(title.rectTransform);
            float pillH = PetSkillStyle.Px("pill_h_rem");
            PetSkillKit.Pill(head, "pill-ticket", PetSkillStyle.C("pill_ticket"), "ticket", PetSkillStyle.Fmt(H.Tickets), pillH, 0f, (headH - pillH) * 0.5f);
            y += headH + gap;

            // ---- passive-banner ----
            float bw = PetSkillStyle.Px("passive_w"), bh = PetSkillStyle.Px("passive_h");
            RectTransform banner = UiKit.Box(root, "passive-banner");
            UiKit.Place(banner, (W - bw) * 0.5f, y, bw, bh);
            PetSkillKit.Fill(banner, "bg", PetSkillStyle.C("passive_bg"), PetSkillStyle.Px("passive_r_rem"));
            TextMeshProUGUI bt = PetSkillKit.Text(banner, "t", TextKind.Sub, PetSkillStyle.T("passive_banner", PetSkillStyle.Fmt(pb.Atk), PetSkillStyle.Fmt(pb.Hp)), PetSkillStyle.C("ink"));
            UiKit.Fill(bt.rectTransform);
            WrapUi.Apply(bt, "passive_banner");   // T361 2회차 — 정본 white-space 표(WrapUi.json) 4010 `.passive-banner { nowrap }`

            // T369 2회차 — **격자가 시작하는 자리는 쌓는 순서가 아니라 표가 쥔다**: 정본 `style.css` 4013~4015 `.sk-grid` 머리말이
            // «1행 오브 상단 92px(10.34%H)» 라고 값을 글자로 못 박아 두었다(원본 shot-042340 · 앱 496×890 · 화소 재확인 y92).
            // 클론은 머리·배너를 쌓은 나머지로만 정해져 8.96%H 였다(−13px · 부제 띠 ↔ 오브 틈이 정본 21px ↔ 클론 9px).
            // 표값은 **화면 위끝** 기준이고 이 패널은 시트 padding 만큼 내려와 앉으므로 그만큼 뺀 자리가 지역 y 다.
            // 머리·배너의 높이는 건드리지 않는다(둘 다 자리가 맞다 · 틈만 맞춘다).
            // (종전 `y += bh + gap` 은 이 한 줄이 덮어쓰므로 지웠다 — 두 자리가 다투면 나중 것이 이기는 줄을 남기지 않는다.)
            y = PetSkillStyle.Px("sk_grid_top_h") - PetSkillStyle.Px("pad_rem");

            // ---- 아래부터 위로 자리 잡기: summon-bar · 버튼 행 · 장착됨 행 ----
            float barH = PetSkillStyle.Px("bar_h");
            float barTop = PetSkillStyle.Px("bar_top_rem");
            float actH = PetSkillStyle.Px("action_h");
            float eqH = PetSkillStyle.Px("mini_skill_rem") + PetSkillStyle.Px("equipped_pad_y_skill_rem") * 2f + PetSkillKit.Line3 * 2f;
            float eqMb = PetSkillStyle.Rem(0.32f);
            float bottom = height;
            float barY = bottom - barH;
            float actY = barY - barTop - actH;
            float eqY = actY - gap - eqMb - eqH;
            float gridBottom = eqY - gap;

            // ---- grid-scroll ----
            RectTransform view = PetSkillKit.Scroll(root, "grid-scroll", out scroll);
            UiKit.Place((RectTransform)view.parent, 0f, y, W, Mathf.Max(10f, gridBottom - y));
            BuildGrid(view, W);

            // ---- equipped-row ----
            BuildEquippedRow(root, W, eqY, eqH);

            // ---- row.center: 모두 업그레이드 · 빠른 장착 ----
            // T90 — 원작 `.sk-action-btn` 폭(20.97%W)은 하한이다: 한글 글꼴이 들어와 진짜 폭으로 그려지자 «모두 업그레이드» 가 버튼 밖으로 넘쳤다.
            // §1 글자 하한(버튼 44)은 그대로 두고 **칸을 글자에 맞춰 키운다**(원작 .btn.sm 좌우 패딩 .6rem · 두 버튼을 가운데 정렬).
            float ag = PetSkillStyle.Px("action_gap_w");
            float aw1 = ActionWidth(PetSkillStyle.T("upgrade_all")), aw2 = ActionWidth(PetSkillStyle.T("quick_equip"));
            float ax = (W - aw1 - aw2 - ag) * 0.5f;
            UpgradeAllButton = PetSkillKit.PaperButton(root, "btn-upgrade-all", PetSkillKit.BtnKind.Primary, PetSkillStyle.T("upgrade_all"), null, false, OnUpgradeAll);
            UiKit.Place(UpgradeAllButton.GetComponent<RectTransform>(), ax, actY, aw1, actH);
            QuickEquipButton = PetSkillKit.PaperButton(root, "btn-quick-equip", PetSkillKit.BtnKind.Primary, PetSkillStyle.T("quick_equip"), null, false, OnQuickEquip);
            UiKit.Place(QuickEquipButton.GetComponent<RectTransform>(), ax + aw1 + ag, actY, aw2, actH);
            // T361 2회차 — 정본 677 `.sk-action-btn { nowrap }`: 종이 버튼의 라벨(PetSkillKit 이 만든 «label»)에 표대로 건다
            foreach (Button ab in new[] { UpgradeAllButton, QuickEquipButton }) { TextMeshProUGUI al = ab.GetComponentInChildren<TextMeshProUGUI>(true); if (al != null) WrapUi.Apply(al, "sk_action_btn"); }

            // ---- summon-bar ----
            RectTransform bar = UiKit.Box(root, "summon-bar");
            UiKit.Place(bar, 0f, barY, W, barH);
            SummonDash(bar);   // T368 3회차 — 정본 4205 `#panel-skills .summon-bar::before` 풀블리드 대시 줄(표 skills_summon_dash · 구운 타일 한 장)
            float padRem = PetSkillStyle.Px("pad_rem");
            // back-btn
            BackButton = BackButtonAt(bar, PetSkillStyle.Px("back_left_w") - padRem, barH - PetSkillStyle.Px("back_bottom_h") - PetSkillStyle.Px("back_h"));
            // x5-toggle
            float xw = PetSkillStyle.Px("x5_w"), xh = PetSkillStyle.Px("x5_h");
            MultButton = MultToggle(bar, mult, PetSkillStyle.Px("x5_left_skill_w") - padRem, barH - PetSkillStyle.Px("x5_bottom_h") - xh, xw, xh);
            // summon-btn (가운데)
            float sw = PetSkillStyle.Px("summon_btn_w");
            bool ascend = H.AscendReady("skill");
            if (ascend)
                SummonButton = PetSkillKit.PaperButton(bar, "summon-btn", PetSkillKit.BtnKind.Ascend, PetSkillStyle.T("ascend_ready"), PetSkillStyle.T("ascend_sub"), false, OnAscend);
            else
                SummonButton = SummonBtn(bar, PetSkillStyle.T("summon_x", mult), "ticket", JsNum.ToString(Sk.TicketCost(mult)), !Sk.CanSummon(false, mult), () => OnSummon(false));
            UiKit.Place(SummonButton.GetComponent<RectTransform>(), (W - sw) * 0.5f, 0f, sw, barH);
            // summon-info
            int cnt = Sk.State.SummonCount;
            RatesButton = SummonInfo(bar, PetSkillStyle.Px("info_left_w") - padRem, barH, lvl, capped ? 1f : (cnt % Sk.Rules.SummonsPerLevel) / (float)Sk.Rules.SummonsPerLevel,
                capped ? PetSkillStyle.T("gauge_max") : PetSkillStyle.T("gauge", cnt % Sk.Rules.SummonsPerLevel, Sk.Rules.SummonsPerLevel), () => SkillRatesPopup.Open(sheet, Kind));
        }

        /// <summary>T90 — 액션 버튼 폭: 원작 고정폭(`action_w`)과 «글자 폭 + 좌우 패딩(.btn.sm .6rem)» 중 큰 쪽.</summary>
        public static float ActionWidth(string label)
        {
            float pad = PetSkillStyle.Px("action_pad_x_rem");
            return Mathf.Max(PetSkillStyle.Px("action_w"), PetSkillKit.TextWidth(TextKind.Button, label) + pad * 2f);
        }

        /// <summary>격자 칸(테스트가 본다). 없으면 null.</summary>
        public RectTransform Cell(string id) { Button b; return cellButtons.TryGetValue(id, out b) ? b.GetComponent<RectTransform>() : null; }

        void BuildGrid(RectTransform content, float W)
        {
            float colW = PetSkillStyle.Px("sk_col_w"), colGap = PetSkillStyle.Px("sk_col_gap_w"), rowGap = PetSkillStyle.Px("sk_row_gap_h");
            float orb = PetSkillStyle.Px("sk_orb_w");
            float cellGap = PetSkillStyle.Px("sk_cell_gap_h");
            float shardH = PetSkillStyle.Px("sk_shard_h");
            float starH = UiCatalog.Instance.Kind(TextKind.Sub).size;
            int cols = 5;
            float gridW = cols * colW + (cols - 1) * colGap;
            float x0 = (W - gridW) * 0.5f;
            var owned = new List<SkillDef>();
            foreach (SkillDef d in Defs.SkillDefs) if (Sk.State.Get(d.Id) != null) owned.Add(d);
            GridCells = owned.Count;
            if (owned.Count == 0)
            {
                TextMeshProUGUI empty = PetSkillKit.Text(content, "grid-empty", TextKind.Sub, PetSkillStyle.T("skills_empty"), PetSkillStyle.C("muted"), TextAlignmentOptions.Center, false);
                UiKit.Place(empty.rectTransform, 0f, PetSkillStyle.Rem(2.4f), W, starH * 1.5f);
                content.sizeDelta = new Vector2(0f, PetSkillStyle.Rem(2.4f) * 2f + starH * 1.5f);
                return;
            }
            // T90 — 별 줄(.sk-star)은 별이 있을 때만 선다(원작 `${sk.stars ? … : ''}`). 별이 없는 행은 «오브 + 간격 + 게이지» 뿐 —
            // 종전엔 별 줄을 항상 비워 둬 오브→게이지 간격이 원작 1.01%H 의 4배였다(런 148·149 실측). 행 높이 = 그 행에서 가장 큰 칸(CSS grid auto rows).
            int rows = (owned.Count + cols - 1) / cols;
            float[] rowH = new float[rows];
            for (int i = 0; i < owned.Count; i++)
            {
                SkillEntry e = Sk.State.Get(owned[i].Id);
                float ch = orb + cellGap + (e.Stars > 0 ? starH + cellGap : 0f) + shardH;
                if (ch > rowH[i / cols]) rowH[i / cols] = ch;
            }
            float[] rowY = new float[rows];
            float total = 0f;
            for (int r = 0; r < rows; r++) { rowY[r] = total; total += rowH[r] + rowGap; }
            content.sizeDelta = new Vector2(0f, total);
            for (int i = 0; i < owned.Count; i++)
            {
                SkillDef d = owned[i];
                SkillEntry sk = Sk.State.Get(d.Id);
                bool equipped = Sk.State.Equipped.Contains(d.Id);
                bool maxed = sk.Level >= Sk.Rules.MaxLevel;
                int need = Sk.ShardsRequired(maxed ? Sk.Rules.MaxLevel : sk.Level);
                float ratio = Mathf.Clamp01(sk.Dupes / (float)need);
                string id = d.Id;
                Button b = UiKit.Button(content, "sk-cell-" + id, () => OpenSkillDetail(id));
                RectTransform cell = b.GetComponent<RectTransform>();
                float cellH = rowH[i / cols];
                UiKit.Place(cell, x0 + (i % cols) * (colW + colGap), rowY[i / cols], colW, cellH);
                cellButtons[id] = b;
                Color rc = PetSkillStyle.Rarity(Defs, d.Rarity);
                RectTransform orbRt = PetSkillKit.Orb(cell, "sk-orb", rc, PetSkillKit.Line3);
                UiKit.Place(orbRt, (colW - orb) * 0.5f, 0f, orb, orb);
                Image ico = UiKit.Icon(orbRt, "ico", "sk_" + id);
                float ip = orb * 0.05f;
                ico.rectTransform.offsetMin = new Vector2(ip, ip);
                ico.rectTransform.offsetMax = new Vector2(-ip, -ip);
                // T95 — 장착 오브: 원작 `.sk-orb.equipped::after` 어둠 막 rgba(0,0,0,.58)(선형 색 공간이라 지각값으로 · 결정 191) →
                // `.sk-eqplate` 정중앙 검정 타원(앱 폭 14.92% × 2.82% · 글자 하한이 더 크면 칸을 키운다) → `.sk-lv` 는 그 위(z 2) · 잉크 위끝이 타원 아래끝에 닿는다(원작 실측 36~64% ↔ 64~81%).
                float body = UiCatalog.Instance.Kind(TextKind.Body).size;
                float lvCenter = orb * PetSkillStyle.L("sk_lv_center_f");
                if (equipped)
                {
                    Image dimm = PetSkillKit.Disc(orbRt, "equipped", UiKit.PerceivedDim(PetSkillStyle.C("orb_dim")));
                    UiKit.Fill(dimm.rectTransform);
                    float pw = PetSkillStyle.Px("sk_eqplate_w"), ph = Mathf.Max(PetSkillStyle.Px("sk_eqplate_h_w"), starH * 1.1f);   // 정본 2.82%W(앱 폭 기준 · 종전 키는 _h 라 높이 기준 54px 로 컸다)
                    lvCenter = Mathf.Max(lvCenter, orb * 0.5f + ph * 0.5f + body * PetSkillStyle.L("sk_lv_ink_half_f"));
                    RectTransform plate = UiKit.Box(orbRt, "sk-eqplate");
                    UiKit.Anchor(plate, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, pw, ph);
                    Image pimg = PetSkillKit.Disc(plate, "bg", PetSkillStyle.C("eqplate"));
                    pimg.preserveAspect = false;
                    TextMeshProUGUI pt = PetSkillKit.Text(plate, "t", TextKind.Sub, PetSkillStyle.T("equipped"), PetSkillStyle.C("white"));
                    UiKit.Fill(pt.rectTransform);
                }
                // Lv — #panel-skills 의 sk-lv 는 배경 없이 흰 글자 + 검정 외곽선
                // T90 — 원작 실측(css `#panel-skills .sk-grid .sk-lv` 주석): 잉크 세로중심 = 오브 위에서 72.9% · 잉크 폭 = 지름의 90%.
                // 종전엔 오브 바닥에 걸쳐(중심 ≈ 82~100%) 아래가 잘려 보였다(런 148·149). 링은 원작 2px/41px ≈ 5% 꼴로 얇게.
                TextMeshProUGUI lv = PetSkillKit.Stroked(orbRt, "sk-lv", TextKind.Body, PetSkillStyle.T("lv_short", sk.Level), PetSkillStyle.C("white"), "sk_lv");   // 정본 #panel-skills .sk-grid .sk-lv 2px
                WrapUi.Apply(lv, "sk_lv");   // T361 2회차 — 정본 white-space 표(WrapUi.json) 4048 `.sk-lv { nowrap }`
                float lvH = UiCatalog.Instance.Kind(TextKind.Body).size * 1.1f;
                UiKit.Anchor(lv.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -lvCenter), orb * PetSkillStyle.L("sk_lv_w_f"), lvH);
                // 별(있을 때만 · 줄을 차지한다)
                float below = orb + cellGap;
                if (sk.Stars > 0) { StarRow(cell, sk.Stars, colW, below, starH); below += starH + cellGap; }
                // 조각 게이지
                RectTransform shard = PetSkillKit.Gauge(cell, "sk-shard", colW, shardH, ratio, PetSkillStyle.T("gauge", sk.Dupes, need), PetSkillStyle.C("shard_bg"), PetSkillStyle.Px("sk_shard_r_rem"), PetSkillKit.Line2, TextKind.Sub);
                ((Image)shard.Find("face/fill").GetComponent<Image>()).color = PetSkillStyle.C("white");
                UiKit.Place(shard, 0f, below, colW, shardH);
            }
        }

        public static void StarRow(Transform parent, int stars, float w, float yTop, float h)
        {
            RectTransform row = UiKit.Box(parent, "sk-star");
            UiKit.Place(row, 0f, yTop, w, h);
            Image star = UiKit.Icon(row, "ico", "star");
            TextMeshProUGUI n = PetSkillKit.Text(row, "n", TextKind.Sub, stars.ToString(), PetSkillStyle.C("ink"), TextAlignmentOptions.Left);
            float tw = PetSkillKit.TextWidth(TextKind.Sub, stars.ToString());
            float total = h + tw;
            UiKit.Place(star.rectTransform, (w - total) * 0.5f, 0f, h, h);
            UiKit.Place(n.rectTransform, (w - total) * 0.5f + h, 0f, tw + h, h);
        }

        void BuildEquippedRow(RectTransform parent, float W, float yTop, float eqH)
        {
            float w = W * PetSkillStyle.L("equipped_w_f");
            RectTransform row = PetSkillKit.Framed(parent, "equipped-row", PetSkillStyle.C("equipped_bg"), PetSkillStyle.Px("equipped_r_rem"), PetSkillKit.Line3);
            UiShadow.Drop(row, "equipped_lip", PetSkillStyle.Px("equipped_r_rem"));   // 정본 .equipped-row(4128) `0 .22rem 0 rgba(0,0,0,.35)`
            UiKit.Place(row, (W - w) * 0.5f, yTop, w, eqH);
            EquippedLabel(row, eqH);
            float mini = PetSkillStyle.Px("mini_skill_rem");
            float g = PetSkillStyle.Px("mini_gap_skill_w");
            float padX = PetSkillStyle.Px("equipped_pad_x_rem");
            var ids = Sk.State.Equipped;
            if (ids.Count == 0)
            {
                TextMeshProUGUI none = PetSkillKit.Text(row, "none", TextKind.Sub, PetSkillStyle.T("none"), PetSkillStyle.C("muted"), TextAlignmentOptions.Right, false);
                UiKit.Place(none.rectTransform, w * 0.4f, 0f, w * 0.6f - padX, eqH);
                return;
            }
            float x = w - padX - ids.Count * mini - (ids.Count - 1) * g;
            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                SkillDef d = Sk.Def(id);
                SkillEntry sk = Sk.State.Get(id);
                if (d == null || sk == null) continue;
                Button b = UiKit.Button(row, "sk-mini-" + id, () => OpenSkillDetail(id));
                RectTransform br = b.GetComponent<RectTransform>();
                UiKit.Place(br, x + i * (mini + g), (eqH - mini) * 0.5f, mini, mini);
                RectTransform orbRt = PetSkillKit.Orb(br, "orb", PetSkillStyle.Rarity(Defs, d.Rarity), PetSkillKit.Line2);
                UiKit.Fill(orbRt);
                Image ico = UiKit.Icon(orbRt, "ico", "sk_" + id);
                float ip = mini * 0.06f;
                ico.rectTransform.offsetMin = new Vector2(ip, ip);
                ico.rectTransform.offsetMax = new Vector2(-ip, -ip);
                MiniLv(br, PetSkillStyle.T("lv_short", sk.Level), mini);
            }
        }

        public static void EquippedLabel(RectTransform row, float rowH)
        {
            float lw = PetSkillStyle.Px("equipped_label_w") + PetSkillKit.Line3 * 2f;
            float lh = UiCatalog.Instance.Kind(TextKind.Sub).size * 1.35f;
            RectTransform label = PetSkillKit.Framed(row, "equipped-label", PetSkillStyle.C("white"), 0f, PetSkillKit.Line3);
            float lift = PetSkillStyle.Px("equipped_label_lift_h");
            UiKit.Place(label, PetSkillStyle.Px("equipped_label_left_rem"), (rowH - lh) * 0.5f - lift, lw, lh);
            // 오른쪽 홈(clip-path 화살) — 종이색 삼각을 검정 테 위에 얹는다
            float notch = PetSkillStyle.Px("equipped_label_notch_rem");
            PetHatchCone tri = PetHatchCone.Add(label, "notch", PetSkillStyle.C("equipped_bg"), PetSkillStyle.C("equipped_bg"), 1f, 0f);
            RectTransform tr = tri.rectTransform;
            tr.anchorMin = new Vector2(1f, 0f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.pivot = new Vector2(1f, 0.5f);
            tr.anchoredPosition = Vector2.zero;
            tr.sizeDelta = new Vector2(notch, 0f);
            tri.Shape = PetHatchCone.Kind.NotchRight;
            TextMeshProUGUI t = PetSkillKit.Text(label, "t", TextKind.Sub, PetSkillStyle.T("equipped"), PetSkillStyle.C("ink"), TextAlignmentOptions.Left);
            UiKit.Place(t.rectTransform, PetSkillStyle.Rem(0.6f), 0f, lw - PetSkillStyle.Rem(0.6f) - notch, lh);
        }

        public static void MiniLv(RectTransform parent, string text, float mini)
        {
            float lh = UiCatalog.Instance.Kind(TextKind.Sub).size * 1.15f;
            float lw = PetSkillKit.TextWidth(TextKind.Sub, text) + PetSkillStyle.Rem(0.44f);
            RectTransform box = PetSkillKit.Framed(parent, "small", PetSkillStyle.C("white"), lh * 0.5f, PetSkillStyle.L("line1_px"));
            UiKit.Anchor(box, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, -PetSkillStyle.Rem(0.3f) + lh * 0.5f), lw, lh);
            TextMeshProUGUI t = PetSkillKit.Text(box, "t", TextKind.Sub, text, PetSkillStyle.C("ink"));
            UiKit.Fill(t.rectTransform);
            WrapUi.Apply(t, "sk_mini_small");   // T361 2회차 — 정본 white-space 표(WrapUi.json) 4162 `.sk-mini small { nowrap }`
        }

        // ---- summon-bar 조각(펫 패널도 쓴다) ----

        /// <summary>T368 3회차 — 정본 style.css 4205 `#panel-skills .summon-bar::before`: 소환 바 **위** 풀블리드 대시 줄 한 장(스코프가 #panel-skills 라 펫 패널(4404)은 안 그린다).
        /// 수는 전부 표 `SurfaceUi.json` stripes.skills_summon_dash 의 **앱 폭/높이 비율**이라 RefW·RefH 를 곱한다(rem 으로 옮기면 T364 가 잡은 함정 · 정본 2547 주석).
        /// 절대 배치 기준이 소환 바의 패딩 상자라 `left_w`(−.027W) 만큼 되밀어 `width_w`(1.0W) 로 앱 폭 전체 · `top_h`(−.0242H) 위에 `height_h`(.00225H) 두께 ·
        /// 한 타일(주기 `period_w` · 대시 `dash_ratio` · 당김 `phase_w` = 반 대시라 x=0 에 대시 중심)을 굽고 `Tiled` 로 되풀이한다 — 종전(T20)엔 대시 조각을 늘어놓았고 왼쪽 되밀기가 리터럴 .8rem 이었다.</summary>
        public static Image SummonDash(RectTransform bar)
        {
            const string key = "skills_summon_dash";
            float period = SurfaceArt.StripeNum(key, "period_w", 0f) * UiKit.RefW;
            float dash = (float)Forge.Core.Ui.StripeRules.DashFromRatio(period, SurfaceArt.StripeNum(key, "dash_ratio", 0.5f));
            float phase = SurfaceArt.StripeNum(key, "phase_w", 0f) * UiKit.RefW;
            float x = SurfaceArt.StripeNum(key, "left_w", 0f) * UiKit.RefW, w = SurfaceArt.StripeNum(key, "width_w", 1f) * UiKit.RefW;
            float yTop = SurfaceArt.StripeNum(key, "top_h", 0f) * UiKit.RefH, h = Mathf.Max(1f, SurfaceArt.StripeNum(key, "height_h", 0f) * UiKit.RefH);
            RectTransform rt = UiKit.Box(bar, "summon-dash");
            rt.SetAsFirstSibling();                                   // ::before — 바의 다른 조각보다 뒤(자리는 바 밖 위쪽이라 겹치지 않는다)
            UiKit.Place(rt, x, yTop, w, h);
            Image img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;                                // pointer-events: none
            img.type = Image.Type.Tiled;
            img.sprite = SurfaceArt.BakeStripe(key, period, dash, phase, h);
            return img;
        }

        public static Button BackButtonAt(RectTransform bar, float x, float yTop, UnityEngine.Events.UnityAction onBack = null)
        {
            float w = PetSkillStyle.Px("back_w"), h = PetSkillStyle.Px("back_h");
            if (onBack == null) onBack = () => { if (UiRoot.Instance != null) UiRoot.Instance.TabBar.Switch(null); };
            Button b = PetSkillKit.PaperButton(bar, "back-btn", PetSkillKit.BtnKind.Danger, string.Empty, null, false, onBack, PetSkillStyle.Px("back_r_w"));
            RectTransform br = b.GetComponent<RectTransform>();
            UiKit.Place(br, x, yTop, w, h);
            Image ico = UiKit.Icon(br, "ico", "tri_left");
            float ih = PetSkillStyle.Px("back_icon_h");
            UiKit.Anchor(ico.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, ih, ih);
            return b;
        }

        public Button MultToggle(RectTransform bar, int mult, float x, float yTop, float w, float h)
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
            return b;
        }

        public static Button SummonBtn(RectTransform bar, string label, string costIcon, string cost, bool disabled, UnityEngine.Events.UnityAction onClick)
        {
            Button b = PetSkillKit.PaperButton(bar, "summon-btn", PetSkillKit.BtnKind.Silver, label, " ", disabled, onClick);
            RectTransform br = b.GetComponent<RectTransform>();
            ((RectTransform)br.Find("skin/line")).GetComponent<Image>().color = PetSkillStyle.C("pp_line");
            TextMeshProUGUI sub = br.Find("sub").GetComponent<TextMeshProUGUI>();
            sub.text = cost;
            sub.color = PetSkillStyle.C(disabled ? "disabled_ink" : "cost_red");
            sub.alignment = TextAlignmentOptions.Left;
            float ico = UiCatalog.Instance.Kind(TextKind.Sub).size * 1.55f;
            float tw = PetSkillKit.TextWidth(TextKind.Sub, cost);
            float total = ico + PetSkillStyle.Rem(0.25f) + tw;
            RectTransform sr = sub.rectTransform;
            sr.anchorMin = new Vector2(0.5f, 0.06f);
            sr.anchorMax = new Vector2(0.5f, 0.5f);
            sr.pivot = new Vector2(0f, 0.5f);
            sr.anchoredPosition = new Vector2(-total * 0.5f + ico + PetSkillStyle.Rem(0.25f), 0f);
            sr.sizeDelta = new Vector2(tw + ico, 0f);
            Image ci = UiKit.Icon(br, "cost-ico", costIcon);
            UiKit.Anchor(ci.rectTransform, new Vector2(0.5f, 0.28f), new Vector2(0f, 0.5f), new Vector2(-total * 0.5f, 0f), ico, ico);
            return b;
        }

        public static Button SummonInfo(RectTransform bar, float x, float barH, int lvl, float ratio, string gaugeText, UnityEngine.Events.UnityAction onInfo)
        {
            float w = PetSkillStyle.Px("info_w");
            float dot = PetSkillStyle.Px("info_dot_rem");
            float lvH = UiCatalog.Instance.Kind(TextKind.Sub).size * 1.1f;
            float gw = PetSkillStyle.Px("gauge_w_rem"), gh = PetSkillStyle.Px("gauge_h_rem");
            float gap = PetSkillStyle.Rem(0.12f);
            float total = dot + gap + lvH + gap + gh;
            RectTransform info = UiKit.Box(bar, "summon-info");
            UiKit.Place(info, x, (barH - total) * 0.5f, w, total);
            Button b = UiKit.Button(info, "info-dot", onInfo);
            RectTransform br = b.GetComponent<RectTransform>();
            UiKit.Place(br, (w - dot) * 0.5f, 0f, dot, dot);
            Image disc = PetSkillKit.Disc(br, "bg", PetSkillStyle.C("ink"));
            UiKit.Fill(disc.rectTransform);
            TextMeshProUGUI it = PetSkillKit.Text(br, "t", TextKind.Sub, PetSkillStyle.T("info_i"), PetSkillStyle.C("white"));
            UiKit.Fill(it.rectTransform);
            TextMeshProUGUI lt = PetSkillKit.Text(info, "lv", TextKind.Sub, PetSkillStyle.T("lv", lvl), PetSkillStyle.C("ink"));
            UiKit.Place(lt.rectTransform, -gw, dot + gap, w + gw * 2f, lvH);
            RectTransform g = PetSkillKit.Gauge(info, "summon-gauge", gw, gh, ratio, gaugeText, PetSkillStyle.C("gauge_bg"), PetSkillStyle.Px("gauge_r_rem"), PetSkillKit.Line2, TextKind.Sub);
            UiKit.Place(g, (w - gw) * 0.5f, dot + gap + lvH + gap, gw, gh);
            return b;
        }

        // ===== 동작(원작 on*) =====

        void OnSummon(bool useGems)
        {
            int count = H.SummonMult(Kind);
            SkillSummonResult r = Sk.Summon(useGems, count);
            if (r == null) { PetSkillHost.Say(PetSkillStyle.T(useGems ? "toast_gems_short" : "toast_tickets_short")); return; }
            H.Sync();
            H.Save();
            SkillSummonResultView.Open(sheet, Kind, r, () => OnSummon(useGems));
        }

        void OnUpgrade(string id)
        {
            if (!Sk.Upgrade(id)) { PetSkillHost.Say(PetSkillStyle.T("toast_shards_short")); return; }
            PetSkillHost.Say(PetSkillStyle.T("toast_skill_lv", Sk.Def(id).Name, Sk.Level(id)));
            H.Sync();
            H.Save();
        }

        void OnUpgradeAll()
        {
            int n = Sk.UpgradeAll();
            PetSkillHost.Say(n > 0 ? PetSkillStyle.T("toast_upgraded_n", n) : PetSkillStyle.T("toast_no_upgrade"));
            H.Sync();
            H.Save();
        }

        void OnQuickEquip()
        {
            Sk.QuickEquip();
            PetSkillHost.Say(PetSkillStyle.T("toast_quick_equip"));
            H.Sync();
            H.Save();
        }

        /// <summary>T143 ⓐ — 정본 `ui.js` 4462 `if (!Skills.toggleEquip(id)) this.toast('스킬은 최대 N개 장착 가능합니다')`:
        /// 슬롯이 차서 못 끼면 **말한다**(같은 화면의 펫 쪽 <see cref="PetPanel.OnTogglePet"/> 과 같은 길 · 문구는 `PetSkillUi.json`). 시험이 부르므로 public.</summary>
        public void OnToggle(string id)
        {
            if (!Sk.ToggleEquip(id)) { PetSkillHost.Say(PetSkillStyle.T("toast_skill_max", Sk.Rules.MaxActive)); return; }
            H.Sync();
            H.Save();
        }

        void OnAscend() { PetSkillHost.Say(PetSkillStyle.T("ascend_ready")); }

        // ===== openSkillDetail (UI-SPEC 46 · shot-042426) =====
        public const string DetailModal = "skill-detail";

        public void OpenSkillDetail(string id)
        {
            SkillDef d = Sk.Def(id);
            SkillEntry sk = Sk.State.Get(id);
            if (d == null || sk == null) return;
            bool equipped = Sk.State.Equipped.Contains(id);
            bool maxed = sk.Level >= Sk.Rules.MaxLevel;
            int need = Sk.ShardsRequired(maxed ? Sk.Rules.MaxLevel : sk.Level);
            PassiveBonus pb = Sk.PassiveOf(id);
            string desc = d.Type == "heal" ? PetSkillStyle.T("skd_heal", JsNum.ToString(d.Dur ?? 0), PetSkillStyle.Fmt(Sk.HealAmt(id)))
                : d.Type == "buff" ? PetSkillStyle.T("skd_buff", JsNum.ToString(d.Dur ?? 0), PetSkillStyle.Fmt(Sk.BuffAtk(id)))
                : d.Type == "aoe" ? PetSkillStyle.T("skd_aoe", PetSkillStyle.Fmt(Sk.Dmg(id)))
                : PetSkillStyle.T("skd_single", PetSkillStyle.Fmt(Sk.Dmg(id)));
            float ratio = Mathf.Clamp01(sk.Dupes / (float)need);

            float w = PetSkillStyle.L("skd_w_f") * UiKit.RefW;
            float h = PetSkillStyle.Px("skd_min_h_rem");
            PetSkillModal.Handle m = sheet.Modal.Open(DetailModal, PetSkillStyle.L("skd_w_f"), h, PetSkillStyle.L("skd_top_rem"));
            RectTransform c = m.Content;
            float padX = PetSkillStyle.Px("skd_pad_x_rem"), padT = PetSkillStyle.Px("skd_pad_top_rem"), padB = PetSkillStyle.Px("skd_pad_bottom_rem");
            float inner = w - padX * 2f;
            float y = padT;
            // head: orbcol + body
            float orbW = inner * PetSkillStyle.L("skd_orb_w_f");
            float gap = PetSkillStyle.Px("skd_head_gap_rem");
            float starH = UiCatalog.Instance.Kind(TextKind.Sub).size;
            float shardH = PetSkillStyle.Px("skd_shard_h_rem");
            RectTransform orbcol = UiKit.Box(c, "skd-orbcol");
            float colH = orbW + PetSkillStyle.Rem(0.22f) * 2f + (sk.Stars > 0 ? starH : 0f) + shardH + UiCatalog.Instance.Kind(TextKind.Body).size * 0.4f;
            UiKit.Place(orbcol, padX, y, orbW, colH);
            RectTransform orbRt = PetSkillKit.Orb(orbcol, "sk-orb", PetSkillStyle.Rarity(Defs, d.Rarity), PetSkillKit.Line3);
            UiKit.Place(orbRt, 0f, 0f, orbW, orbW);
            Image ico = UiKit.Icon(orbRt, "ico", "sk_" + id);
            float ip = orbW * 0.05f;
            ico.rectTransform.offsetMin = new Vector2(ip, ip);
            ico.rectTransform.offsetMax = new Vector2(-ip, -ip);
            float lvH = UiCatalog.Instance.Kind(TextKind.Sub).size * 1.15f;
            RectTransform lv = PetSkillKit.LvBadge(orbRt, PetSkillStyle.T("lv_short", sk.Level), PetSkillKit.TextWidth(TextKind.Sub, PetSkillStyle.T("lv_short", sk.Level)) + PetSkillStyle.Rem(0.5f), lvH);
            UiKit.Anchor(lv, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, PetSkillStyle.Rem(0.15f)), lv.sizeDelta.x, lvH);
            float cy = orbW + PetSkillStyle.Rem(0.22f);
            if (sk.Stars > 0) { StarRow(orbcol, sk.Stars, orbW, cy, starH); cy += starH + PetSkillStyle.Rem(0.22f); }
            RectTransform shard = PetSkillKit.Gauge(orbcol, "sk-shard", orbW, shardH, ratio, PetSkillStyle.T("gauge", sk.Dupes, need), PetSkillStyle.C("shard_bg"), PetSkillStyle.Px("sk_shard_r_rem"), PetSkillKit.Line2, TextKind.Sub);
            ((Image)shard.Find("face/fill").GetComponent<Image>()).color = PetSkillStyle.C("white");
            UiKit.Place(shard, 0f, cy, orbW, shardH);

            float bx = padX + orbW + gap;
            float bw = inner - orbW - gap;
            float nameH = UiCatalog.Instance.Kind(TextKind.Body).size * 1.3f;
            TextMeshProUGUI name = PetSkillKit.Text(c, "skd-name", TextKind.Body, PetSkillStyle.T("skd_name", Defs.RarityKr.Get(d.Rarity, d.Rarity), d.Name), PetSkillStyle.C("ink"), TextAlignmentOptions.Left);
            UiKit.Place(name.rectTransform, bx, y + PetSkillStyle.Rem(0.15f), bw, nameH);
            TextMeshProUGUI dt = PetSkillKit.Text(c, "skd-desc", TextKind.Body, desc, PetSkillStyle.C("ink"), TextAlignmentOptions.TopLeft);
            LineHeight.Apply(dt, "skd_desc_lh");   // T354 12회차 — 정본 5248 `.skd-desc { line-height: 1.45 }`(스킬 설명은 상자에서 여러 줄로 접힌다)
            dt.textWrappingMode = TextWrappingModes.Normal;
            float descH = UiCatalog.Instance.Kind(TextKind.Body).size * 1.45f * 3f;
            UiKit.Place(dt.rectTransform, bx, y + PetSkillStyle.Rem(0.15f) + nameH + PetSkillStyle.Rem(0.3f), bw, descH);
            TextMeshProUGUI cd = PetSkillKit.Text(c, "skd-cd", TextKind.Sub, PetSkillStyle.T("skd_cd", JsNum.ToString(d.Cd)), PetSkillStyle.C("muted"), TextAlignmentOptions.Left, false);
            UiKit.Place(cd.rectTransform, bx, y + PetSkillStyle.Rem(0.15f) + nameH + PetSkillStyle.Rem(0.3f) + descH, bw, starH * 1.2f);

            // 버튼 행(아래에서) · 패시브(그 위)
            float btnH = PetSkillStyle.Px("skd_btn_h_rem");
            float btnY = h - padB - btnH;
            float bgap = PetSkillStyle.Px("skd_btn_gap_rem"), bpad = PetSkillStyle.Px("skd_btn_pad_x_rem");
            float ew = (w - bpad * 2f - bgap) * 0.5f;
            Button up = maxed
                ? PetSkillKit.PaperButton(c, "btn-upgrade", PetSkillKit.BtnKind.Silver, PetSkillStyle.T("upgrade"), PetSkillStyle.T("max_level", Sk.Rules.MaxLevel), true, null)
                : PetSkillKit.PaperButton(c, "btn-upgrade", PetSkillKit.BtnKind.Silver, PetSkillStyle.T("upgrade"), null, !Sk.CanUpgrade(id), () => { OnUpgrade(id); OpenSkillDetail(id); });
            UiKit.Place(up.GetComponent<RectTransform>(), bpad, btnY, ew, btnH);
            Button eq = PetSkillKit.PaperButton(c, "btn-equip", PetSkillKit.BtnKind.Primary, PetSkillStyle.T(equipped ? "unequip" : "equip"), null, false, () => { OnToggle(id); OpenSkillDetail(id); });
            UiKit.Place(eq.GetComponent<RectTransform>(), bpad + ew + bgap, btnY, ew, btnH);

            float pillH = UiCatalog.Instance.Kind(TextKind.Sub).size * 1.3f;
            float pmx = PetSkillStyle.Px("skd_passive_mx_rem");
            float pillY = btnY - PetSkillStyle.Rem(0.9f) - pillH;
            RectTransform pill = UiKit.Box(c, "skd-passive");
            UiKit.Place(pill, pmx, pillY, w - pmx * 2f, pillH);
            PetSkillKit.Fill(pill, "bg", PetSkillStyle.C("passive_bg"), PetSkillStyle.Px("skd_passive_r_rem"));
            TextMeshProUGUI pt = PetSkillKit.Text(pill, "t", TextKind.Sub, PetSkillStyle.T("skd_passive", PetSkillStyle.Fmt(pb.Atk), PetSkillStyle.Fmt(pb.Hp)), PetSkillStyle.C("ink"));
            LineHeight.Apply(pt, "skd_passive_lh");   // T354 12회차 — 정본 5255 `.skd-passive { line-height: 1.15 }`(알약 안 글자)
            UiKit.Fill(pt.rectTransform);
            TextMeshProUGUI pl = PetSkillKit.Text(c, "skd-passive-label", TextKind.Sub, PetSkillStyle.T("skd_passive_label"), PetSkillStyle.C("ink"), TextAlignmentOptions.Left);
            UiKit.Place(pl.rectTransform, PetSkillStyle.Px("skd_passive_pad_rem"), pillY - PetSkillStyle.Rem(0.25f) - starH * 1.2f, w, starH * 1.2f);
        }
    }
}
