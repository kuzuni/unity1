using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Tech;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 기술 트리 패널(ROUTINE T21 · 원작 ui.js openTechTree/openTechOverview/openTechBranch/renderTechTree/renderTechOverview/renderTechBranchView/drawTechLinks · 샷 042546).
    /// 소환 시트(탭바 «소환» 의 흰 전체화면)의 서브탭 «기술 트리» 자리(원작 #panel-tech.summon-sub)에 선다 — 서브탭 줄 자체는 T20(펫·스킬 시트)이 세우고 <see cref="Show"/>/<see cref="Hide"/> 를 부른다.
    /// 개요 = 물약/젬 알약 + «기술 트리» + 분기 카드 3 → 분기 = 다이아몬드 골격 트리(단계별 한 점→두 열→한 점 · 선은 원 테두리에서 끝난다) + ⓘ 총 보너스 + 뒤로.
    /// </summary>
    public sealed class TechPanel : MonoBehaviour
    {
        public static TechPanel Instance { get; private set; }

        public enum View { Overview, Branch }

        public View Current { get; private set; }
        public string BranchId { get; private set; }
        public int CardCount { get { return cards.Count; } }
        public int NodeCount { get { return nodes.Count; } }
        public int LinkCount { get; private set; }
        public IReadOnlyList<string> NodeIds { get { return nodeIds; } }
        public string TitleText { get { return title != null ? title.text : null; } }
        public string PctText { get { return pct != null ? pct.text : null; } }
        public string NodeLabel(string id) { TextMeshProUGUI t; return nodeLabels.TryGetValue(id, out t) ? t.text : null; }
        /// <summary>연구 중·완료 노드의 시간 배지(정본 `.tech-tree-node-time`) — 없으면 null(«lv/5» 는 민글자).</summary>
        public RectTransform NodeTimePill(string id) { RectTransform r; return nodePills.TryGetValue(id, out r) ? r : null; }
        public Button NodeButton(string id) { Button b; return nodes.TryGetValue(id, out b) ? b : null; }
        public Button CardButton(string branchId) { Button b; return cards.TryGetValue(branchId, out b) ? b : null; }

        static readonly Dictionary<string, string> BranchIcon = new Dictionary<string, string> { { "power", "power" }, { "forge", "hammer" }, { "skillpet", "tm_sparkle" } };
        /// <summary>원작 TECH_ICON — 타입 → IconGen 키.</summary>
        static readonly Dictionary<string, string> TypeIcon = new Dictionary<string, string>
        {
            { "weaponMastery", "power" }, { "armorMastery", "tm_shield" }, { "gearMaxLevel", "uptri" },
            { "mountDmg", "horse" }, { "mountHp", "heart" }, { "mountCost", "winder" }, { "extraMount", "dice" },
            { "forgeTimer", "tm_hourglass" }, { "forgeCost", "coin" }, { "sellPrice", "moneybag" },
            { "thiefHammer", "hammer" }, { "thiefCoin", "trophy" }, { "autoForgeSlot", "robot" },
            { "freeForge", "clover" }, { "offlineCap", "winder" }, { "offlineCoin", "coin" }, { "offlineHammer", "hammer" },
            { "techTimer", "tm_hourglass" }, { "skillDmg", "tm_burst" }, { "skillPassiveDmg", "tm_sword" }, { "skillPassiveHp", "tm_cross" },
            { "techCost", "potion" }, { "petHp", "paw" }, { "petDmg", "paw" }, { "skillSummonCost", "ticket" },
            { "hatchTimer", "egg" }, { "extraEgg", "gift" }, { "dungeonTicket", "ticket" }, { "dungeonPotion", "potion" },
        };
        static readonly Dictionary<string, string> TypeTint = new Dictionary<string, string> { { "petDmg", "#e8544a" } };

        RectTransform body;
        TextMeshProUGUI title, pct, potionLabel, gemLabel;
        readonly Dictionary<string, Button> cards = new Dictionary<string, Button>();
        readonly Dictionary<string, TextMeshProUGUI> cardPct = new Dictionary<string, TextMeshProUGUI>();
        readonly Dictionary<string, Button> nodes = new Dictionary<string, Button>();
        readonly Dictionary<string, TextMeshProUGUI> nodeLabels = new Dictionary<string, TextMeshProUGUI>();
        // T345 ⓒ — 연구 중·완료 노드의 시간 배지(정본 .tech-tree-node-time 알약) · 값 = (가운데 x, 위 y, 높이) — 글자가 바뀌면 폭을 다시 맞춘다
        readonly Dictionary<string, RectTransform> nodePills = new Dictionary<string, RectTransform>();
        readonly Dictionary<string, Vector3> nodePillPos = new Dictionary<string, Vector3>();
        readonly List<string> nodeIds = new List<string>();
        float tickAt;

        static DungeonUiHost Host { get { return DungeonUiHost.Instance; } }
        static TechTree Tree { get { return Host.Tech; } }

        /// <summary>아이콘(타입 → IconGen · 틴트) — 원작 techIcon(id).</summary>
        public static Image TechIcon(RectTransform parent, string name, string nodeId)
        {
            string type = Tree.TypeOf(nodeId);
            string key; string tint;
            if (!TypeIcon.TryGetValue(type, out key)) key = "potion";
            TypeTint.TryGetValue(type, out tint);
            return UiKit.Icon(parent, name, key, tint);
        }

        public static TechPanel Ensure()
        {
            if (Instance != null) return Instance;
            UiRoot root = UiRoot.Instance;
            if (root == null || !DungeonUiHost.Ready) return null;
            RectTransform summon = root.TabBar.Panel("summon");
            if (summon == null) return null;
            RectTransform rt = UiKit.Box(summon, "panel-tech");
            TechPanel p = rt.gameObject.AddComponent<TechPanel>();
            UiKit.Panel(rt, "bg", "pp_paper");
            p.body = UiKit.Box(rt, "body");
            rt.gameObject.SetActive(false);
            return p;
        }

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>원작 openTechTree — 다른 화면에서 들어오는 진입점(메뉴 등): 소환 시트를 열고 개요를 보인다.</summary>
        public static TechPanel OpenTechTree()
        {
            TechPanel p = Ensure();
            if (p == null) return null;
            UiRoot.Instance.TabBar.Switch("summon");
            p.Current = View.Overview;
            p.Show();
            return p;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Render();
        }

        public void Hide() { gameObject.SetActive(false); }

        public void ShowOverview() { Current = View.Overview; Render(); }
        public void ShowBranch(string id) { Current = View.Branch; BranchId = id; Render(); }

        /// <summary>원작 renderTechTree — 소환 시트가 열려 있고 이 패널이 보일 때만.</summary>
        public void Render()
        {
            if (!gameObject.activeSelf) return;
            Tree.Ensure(Host);
            Clear();
            if (Current == View.Branch && Tree.Table.Branch(BranchId) != null) RenderBranch();
            else RenderOverview();
        }

        void Clear()
        {
            for (int i = body.childCount - 1; i >= 0; i--) Destroy(body.GetChild(i).gameObject);
            cards.Clear(); cardPct.Clear(); nodes.Clear(); nodeLabels.Clear(); nodeIds.Clear(); nodePills.Clear(); nodePillPos.Clear();
            title = pct = potionLabel = gemLabel = null;
            LinkCount = 0;
        }

        float PanelH { get { return UiKit.L("tabbar_top") * UiKit.RefH; } }

        /// <summary>물약 알약 · 제목 · 젬 알약 한 줄(원작 .sheet-head). 머리 아래 y 를 돌려준다.</summary>
        float Head(string text)
        {
            float W = UiKit.RefW;
            float padTop = DungeonPopups.RemL("sheet_pad_top_rem");
            float padX = DungeonPopups.RemL("sheet_pad_x_rem");
            RectTransform pp = DungeonPopups.CurPill(body, "pill-potion", "potion", "pill_potion", NumFmt.Fmt(Host.S.Potions), out potionLabel);
            float ph = pp.sizeDelta.y, pw = pp.sizeDelta.x;
            float titleH = DungeonPopups.LineH(TextKind.Button);   // T391 ⓑ — 정본 2242 `.tb-title { 1.25rem }` = 45.5px → Button 44(전엔 Title 60)
            float rowH = Mathf.Max(ph, titleH);
            UiKit.Place(pp, padX, padTop + (rowH - ph) * 0.5f, pw, ph);
            RectTransform gp = DungeonPopups.CurPill(body, "pill-gem", "gem", "pill_gem", NumFmt.Fmt(Host.S.Gems), out gemLabel);
            UiKit.Place(gp, W - padX - pw, padTop + (rowH - ph) * 0.5f, pw, ph);
            title = DungeonPopups.Bold(body, "title", TextKind.Button, text, "pp_ink");
            UiKit.OutlinePx(title, "pp_line", KeylineUi.Em("sheet_title", title.fontSize));   // 정본 h3.tb-title(3846 묶음) { -webkit-text-stroke: .11em var(--pp-line) }
            UiKit.Place(title.rectTransform, padX + pw, padTop + (rowH - titleH) * 0.5f, W - (padX + pw) * 2f, titleH);
            return padTop + rowH;
        }

        void RenderOverview()
        {
            float W = UiKit.RefW;
            float y = Head("기술 트리") + DungeonPopups.RemL("tb_grid_top_rem");
            float mx = W * UiKit.L("tb_grid_mx");
            float gx = W * UiKit.L("tb_grid_gap_x");
            float gy = DungeonPopups.RemL("tb_grid_gap_y_rem");
            float cw = (W - mx * 2f - gx) * 0.5f;
            float chh = DungeonPopups.RemL("tb_card_h_rem");
            var branches = Tree.Table.Branches;
            for (int i = 0; i < branches.Count; i++)
            {
                TechBranch b = branches[i];
                RectTransform card = BranchCard(b, cw, chh);
                UiKit.Place(card, mx + (i % 2) * (cw + gx), y + (i / 2) * (chh + gy), cw, chh);
            }
            DungeonPopups.BackButton(body, () => UiRoot.Instance.TabBar.Switch(null), "tech_back_r_rem");   // T345 7회차 — 정본 2255 `.panel .btn.tech-tree-back` .6rem(공용 .45 를 덮는다)
        }

        RectTransform BranchCard(TechBranch b, float cw, float chh)
        {
            float line3 = UiKit.L("line3_px");
            string id = b.Id;
            RectTransform rt = UiKit.Box(body, "branch-" + id);
            RectTransform face = DungeonPopups.Bordered(rt, "bg", "pp_paper", DungeonPopups.RemL("tb_card_r_rem"), line3);
            // T394 — 정본 계약(style.css 2072~2087) «헤더 3.15%H» = 표 `tb_head_h_rem`. 글꼴 줄 상자(`LineH(Sub)` = 크기×1.25)에서 뽑으면 정본 .82rem 의 1.65배라
            //   헤더가 4.27%H 로 자라 카드(표로 고정)의 아이콘 예산 3.9rem 을 먹는다 — 정본이 🚨 로 못 박은 자리(2096~2099). 패딩 `tb_head_pad_rem` 은 그 안에 든다.
            float headH = DungeonPopups.RemL("tb_head_h_rem");
            Image head = UiKit.Panel(face, "head", "pp_ink");
            UiKit.Place(head.rectTransform, 0f, 0f, cw - line3 * 2f, headH);
            UiKit.Line(head.rectTransform, "line", "pp_line", line3, false);
            TextMeshProUGUI name = DungeonPopups.Bold(head.rectTransform, "name", TextKind.Sub, b.Name, "white");
            UiKit.Fill(name.rectTransform);

            string colorKey = "tech_branch_" + id;
            bool hasColor = true;
            try { UiKit.C(colorKey); } catch (KeyNotFoundException) { hasColor = false; }
            float bgD = DungeonPopups.RemL("tb_icon_bg_rem");
            float icoD = DungeonPopups.RemL("tb_icon_rem");
            float icoCy = headH + DungeonPopups.RemL("tb_icon_top_rem");
            RectTransform circle = DungeonPopups.BorderedCircle(face, "icon-bg", hasColor ? colorKey : "tech_branch_default", DungeonPopups.Line2, "pp_line");
            UiKit.Anchor(circle.parent as RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -icoCy), bgD, bgD);
            // T331 33회차 — 정본 2112 `.tech-branch-icon::before` 의 **둘째 겹** `0 .12rem .2rem rgba(0,0,0,.2)`
            //   (첫 겹 `inset 0 -.18rem .12rem` 은 안쪽이다). 원판이라 반지름은 지름의 절반이고,
            //   크기는 부르는 쪽이 준다(이 상자는 방금 `Anchor` 로 잡혔지만 한 줄로 못 박아 둔다 · 28회차).
            UiShadow.Drop(circle.parent as RectTransform, "techbranch_drop", bgD * 0.5f, bgD, bgD);
            // T371 3회차 — 정본 2110~2111: 원판은 가지색 **원색이 아니다** — 바탕 `color-mix(in srgb, var(--bc) 78%, #fff)` · 테 `color-mix(… 45%, #000)`.
            //   클론은 둘 다 원색·공용 선색이라 힘 갈래에서 (226,87,76) ↔ 정본 (232,124,115) 로 갈렸다. 비율·상대색은 표 `ColorMixUi.json` 이 쥔다(§1).
            Color branchColor = UiKit.C(hasColor ? colorKey : "tech_branch_default");
            Image circleRim = (circle.parent as RectTransform).GetComponent<Image>();
            if (circleRim != null) circleRim.color = ColorMixUi.Mix("tech_branch_line", branchColor);
            // T178 9회차 — 정본 2107~2109: 카테고리색 원판 위에 겹 둘이 더 깔린다(왼쪽 위 방사형 광택 .5 → 0 at 55% · 위 .18 → 아래 −.16 세로 명암).
            //   색 한 칸이면 «납작한 원» 이고 정본은 «구슬» 로 읽힌다(정본 주석 «빈 서류가 아니라 노드 버튼으로 읽히게»).
            Image circleFace = circle.GetComponent<Image>();
            if (circleFace != null)
            {
                circleFace.color = ColorMixUi.Mix("tech_branch_face", branchColor);   // T371 3회차 — 정본 2110 바탕(흰색을 섞어 밝힌 가지색)
                float d = bgD - DungeonPopups.Line2 * 2f;
                SurfaceArt.FillMasked(circleFace, "tb-icon-shade", "tech_branch_shade", d, d);
                SurfaceArt.Fill(circle, "tb-icon-gloss", "tech_branch_gloss", d, d);
            }
            string ik; BranchIcon.TryGetValue(id, out ik);
            Image ico = UiKit.Icon(face, "icon", ik ?? "potion");
            UiKit.Anchor(ico.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -icoCy), icoD, icoD);

            TextMeshProUGUI p = DungeonPopups.Bold(face, "pct", TextKind.Sub, BranchPctText(id), IsResearching(id) ? "pp_green_dk" : "pp_ink");
            float ph = DungeonPopups.LineH(TextKind.Sub);
            UiKit.Place(p.rectTransform, 0f, chh - line3 * 2f - DungeonPopups.RemL("tb_pct_pb_rem") - ph, cw - line3 * 2f, ph);
            cardPct[id] = p;
            cards[id] = UiKit.Button(rt, "hit", () => ShowBranch(id));
            return rt;
        }

        bool IsResearching(string branchId)
        {
            string rid = Tree.ResearchingId();
            return rid != null && Tree.BranchOf(rid) != null && Tree.BranchOf(rid).Id == branchId;
        }

        string BranchPctText(string branchId)
        {
            string s = Tree.BranchProgress(branchId).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "%";
            if (IsResearching(branchId))
                s += Tree.IsDone(Host.Now()) ? " (완료!)" : " (" + NumFmt.FmtTime((Tree.State.Research.EndsAt - Host.Now()) / 1000) + ")";
            return s;
        }

        void RenderBranch()
        {
            float W = UiKit.RefW, H = UiKit.RefH;
            TechBranch b = Tree.Table.Branch(BranchId);
            float y = Head(b.Name);
            pct = DungeonPopups.Bold(body, "pct", TextKind.Sub, Tree.BranchProgress(b.Id).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "%", "pp_ink");
            float ph = DungeonPopups.LineH(TextKind.Sub);
            UiKit.Place(pct.rectTransform, 0f, y, W, ph);
            y += ph;
            DungeonPopups.InfoButton(body, TechPopups.OpenBonuses);

            // 스크롤 뷰포트: 퍼센트 아래 ~ 뒤로 버튼 위
            float viewTop = y + DungeonPopups.RemL("tt_top_rem");
            float viewBottom = PanelH - DungeonPopups.RemL("back_bottom_rem") - DungeonPopups.RemL("back_h_rem") - DungeonPopups.RemL("tt_bottom_rem");
            RectTransform viewport = UiKit.Box(body, "tree-viewport");
            UiKit.Place(viewport, 0f, viewTop, W, Mathf.Max(1f, viewBottom - viewTop));
            viewport.gameObject.AddComponent<RectMask2D>();
            Image vhit = viewport.gameObject.AddComponent<Image>();
            vhit.color = new Color(0f, 0f, 0f, 0f);
            RectTransform content = UiKit.Box(viewport, "tree");
            ScrollRect sr = viewport.gameObject.AddComponent<ScrollRect>();
            sr.viewport = viewport; sr.content = content; sr.horizontal = false; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Clamped;

            float node = H * UiKit.L("tt_node");
            float span = H * UiKit.L("tt_span");
            float pitch = H * UiKit.L("tt_pitch");
            float labelH = DungeonPopups.RemL("tt_label_rem");
            float cx = W * 0.5f + W * UiKit.L("tt_shift");
            float lineW = UiKit.L("tt_line_px");
            List<TechRow> rows = Tree.Rows(b.Id);
            float contentH = rows.Count > 0 ? (rows.Count - 1) * pitch + node + labelH : 0f;
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(0f, contentH);

            // 선을 먼저(아래 층) — 원 테두리에서 끝나는 세로 레일 · 폭이 다른 행 사이는 가로 바(fork/converge).
            var centers = new List<List<Vector2>>();
            for (int r = 0; r < rows.Count; r++)
            {
                var cs = new List<Vector2>();
                float top = r * pitch;
                for (int k = 0; k < rows[r].Ids.Length; k++)
                {
                    float x = rows[r].Ids.Length == 1 ? cx : cx + (k == 0 ? -span * 0.5f : span * 0.5f);
                    cs.Add(new Vector2(x, top + node * 0.5f));
                }
                centers.Add(cs);
            }
            for (int i = 0; i + 1 < rows.Count; i++)
            {
                List<Vector2> A = centers[i], B = centers[i + 1];
                bool dim = true;
                for (int k = 0; k < rows[i + 1].Ids.Length; k++) if (Tree.IsUnlocked(rows[i + 1].Ids[k])) { dim = false; break; }
                string lc = dim ? "tech_link_dim" : "pp_line";
                float yTop = float.MinValue, yBot = float.MaxValue;
                foreach (Vector2 a in A) yTop = Mathf.Max(yTop, a.y + node * 0.5f);
                foreach (Vector2 c in B) yBot = Mathf.Min(yBot, c.y - node * 0.5f);
                float yMid = (yTop + yBot) * 0.5f;
                if (A.Count == B.Count)
                {
                    for (int k = 0; k < A.Count; k++)
                    {
                        Vector2 a = A[k], c = B[k];
                        if (Mathf.Abs(a.x - c.x) < 0.5f) VLine(content, lc, a.x, a.y + node * 0.5f, c.y - node * 0.5f, lineW);
                        else
                        {
                            VLine(content, lc, a.x, a.y + node * 0.5f, yMid, lineW);
                            HLine(content, lc, Mathf.Min(a.x, c.x), Mathf.Max(a.x, c.x), yMid, lineW);
                            VLine(content, lc, c.x, yMid, c.y - node * 0.5f, lineW);
                        }
                    }
                    continue;
                }
                float xMin = float.MaxValue, xMax = float.MinValue;
                foreach (Vector2 v in A) { xMin = Mathf.Min(xMin, v.x); xMax = Mathf.Max(xMax, v.x); }
                foreach (Vector2 v in B) { xMin = Mathf.Min(xMin, v.x); xMax = Mathf.Max(xMax, v.x); }
                HLine(content, lc, xMin, xMax, yMid, lineW);
                foreach (Vector2 a in A) VLine(content, lc, a.x, a.y + node * 0.5f, yMid, lineW);
                foreach (Vector2 c in B) VLine(content, lc, c.x, yMid, c.y - node * 0.5f, lineW);
            }

            // 노드 + 단계 표식
            for (int r = 0; r < rows.Count; r++)
            {
                TechRow row = rows[r];
                float top = r * pitch;
                bool rowDim = true;
                for (int k = 0; k < row.Ids.Length; k++) if (Tree.IsUnlocked(row.Ids[k])) { rowDim = false; break; }
                if (row.First)
                {
                    float tw = DungeonPopups.RemL("tt_tag_w_rem"), th = DungeonPopups.RemL("tt_tag_h_rem");
                    float tx = cx - span * 0.5f - node * 0.5f - DungeonPopups.RemL("tt_tag_gap_rem") - tw;
                    RectTransform tag = UiKit.Box(content, "tier-" + row.Tier);
                    UiKit.Place(tag, tx, top + node * 0.5f - DungeonPopups.Rem(0.6f), tw, th);
                    DungeonPopups.Bordered(tag, "bg", "pp_paper", DungeonPopups.RemL("tt_tag_r_rem"), DungeonPopups.Line2);
                    TextMeshProUGUI tt = DungeonPopups.Bold(tag, "roman", TextKind.Sub, Tree.Roman(row.Tier), "pp_ink");
                    UiKit.Fill(tt.rectTransform);
                    tag.gameObject.AddComponent<CanvasGroup>().alpha = rowDim ? UiKit.L("tt_tag_dim_alpha") : UiKit.L("tt_tag_alpha");
                }
                for (int k = 0; k < row.Ids.Length; k++)
                {
                    Vector2 c = centers[r][k];
                    Node(content, row.Ids[k], c.x - node * 0.5f, top, node, labelH);
                }
            }

            DungeonPopups.BackButton(body, ShowOverview, "tech_back_r_rem");   // T345 7회차 — 같은 정본 규칙(가지 화면의 뒤로 버튼도 .tech-tree-back 이다)
        }

        void VLine(RectTransform parent, string colorKey, float x, float y1, float y2, float w)
        {
            if (y2 <= y1) return;
            Image img = UiKit.Panel(parent, "vlink", colorKey);
            UiKit.Place(img.rectTransform, x - w * 0.5f, y1, w, y2 - y1);
            LinkCount++;
        }

        void HLine(RectTransform parent, string colorKey, float x1, float x2, float y, float w)
        {
            if (x2 <= x1) return;
            Image img = UiKit.Panel(parent, "hlink", colorKey);
            UiKit.Place(img.rectTransform, x1, y - w * 0.5f, x2 - x1, w);
            LinkCount++;
        }

        /// <summary>
        /// T163 — 원판 바닥의 눌림 띠(정본 `inset 0 -Npx 0 rgba(0,0,0,α)` 를 **원** 위에 얹은 꼴 = 바닥 초승달).
        /// 알약의 <see cref="DungeonPopups.BottomShade"/> 는 둥근 사각 띠라 원에는 안 맞는다 — 면(원 스프라이트)을 <see cref="Mask"/> 로 삼고
        /// 같은 크기의 검정 원을 <paramref name="px"/> 만큼 아래로 내려 마스크 밖으로 빠진 위쪽을 잘라내면 바닥 띠만 남는다.
        /// 검정 덮개라 <see cref="UiKit.PerceivedDim"/> 로 정본(sRGB 혼합)과 같은 밝기가 나게 한다(T79 · 선형 색 공간).
        /// </summary>
        public static Image NodeShade(RectTransform face, string colorKey, float px)
        {
            Mask mask = face.gameObject.GetComponent<Mask>();
            if (mask == null) mask = face.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            Image s = UiKit.Circle(face, "shade", colorKey);
            s.color = UiKit.PerceivedDim(s.color);
            s.raycastTarget = false;
            RectTransform rt = s.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = new Vector2(0f, -px);
            return s;
        }

        /// <summary>원작 nodeCol — 원(상태 색) + 아이콘(만렙이면 체크) + 아래 «lv/5»(연구 중이면 남은 시간 · 끝났으면 «완료!»).</summary>
        void Node(RectTransform parent, string id, float x, float top, float d, float labelH)
        {
            int lv = Tree.Level(id);
            bool max = Tree.IsMax(id);
            bool researching = Tree.ResearchingId() == id;
            bool open = Tree.IsUnlocked(id);
            bool ready = researching && Tree.IsDone(Host.Now());
            string fill, border;
            if (ready || researching || (open && lv > 0)) { fill = "tech_node"; border = "tech_node_border"; }
            else if (max) { fill = "tech_done"; border = "tech_done_border"; }
            else if (!open) { fill = "tech_tlocked"; border = "tech_tlocked_border"; }
            else { fill = "tech_locked"; border = "tech_locked_border"; }
            if (max) { fill = "tech_done"; border = "tech_done_border"; }
            // T163 — 정본 `.tech-tree-node { box-shadow: inset 0 -.3rem 0 rgba(0,0,0,.22) }`(style.css 2185) · `.locked` .08(2188) · `.tlocked` .12(2190):
            //        원판 바닥의 «눌림 띠». 면 색 키가 곧 상태다(locked/tlocked 만 옅다 · active·researching·done 은 기본).
            string shade = fill == "tech_locked" ? "tt_shade_locked" : fill == "tech_tlocked" ? "tt_shade_tlocked" : "tt_shade";

            RectTransform rt = UiKit.Box(parent, "node-" + id);
            UiKit.Place(rt, x, top, d, d);
            RectTransform faceRt = DungeonPopups.BorderedCircle(rt, "circle", fill, DungeonPopups.RemL("tt_border_rem"), border);
            NodeShade(faceRt, shade, DungeonPopups.RemL("tt_shade_rem"));
            // T335 ⓒ — 정본 style.css 4622 `.tech-tree-node.researching.ready { animation: tt-ready 1.1s ease-in-out infinite alternate }`(ui.js 5414 `researching ready`):
            //           눌러야 할 노드라 테 색이 브론즈 ↔ 초록으로 맥동한다(면 색은 그대로 · 글로우는 T331 뒤). 테 = BorderedCircle 의 바깥 원.
            if (ready)
            {
                Image ring = rt.Find("circle").GetComponent<Image>();
                TechReadyPulse.Begin(ring, UiKit.C(DungeonClearFx.Spec.ReadyFromKey), UiKit.C(DungeonClearFx.Spec.ReadyToKey));
            }
            float ico = d * UiKit.L("tt_icon");
            Image face = max ? UiKit.Icon(rt, "face", "check") : TechIcon(rt, "face", id);
            UiKit.Anchor(face.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, ico, ico);
            if (!open && !max)
            {
                rt.gameObject.AddComponent<CanvasGroup>().alpha = UiKit.L("tt_tlocked_alpha");
                // T342 ⓒ — 정본 `.tech-tree-node.tlocked .ico, … img { filter: grayscale(.55) }`(style.css 2193).
                //          판의 옅어짐(위 알파)과 **다른 것**이다: 그림만 반쯤 회색으로 눕는다. 틴트는 곱하기라 채도를 못 낮춘다 —
                //          그래서 1회차가 세운 굽는 도우미로 스프라이트 사본을 만든다(자리 키가 곧 표의 줄이다).
                UiFilter.ApplyColor(face, "tech_node_locked");
            }
            string badge = ready ? "완료!" : researching ? NumFmt.FmtTime((Tree.State.Research.EndsAt - Host.Now()) / 1000) : lv + "/" + Tree.Table.MaxLevel;
            TextMeshProUGUI label;
            if (ready || researching)
            {
                // T345 ⓒ — 정본 2209 `.tech-tree-label .tech-tree-node-time { display: inline-block; background: var(--pp-ink); color: var(--pp-green);
                //   border-radius: .6rem; padding: .05rem .45rem; font-weight: 800; margin-top: .1rem }`(ui.js 5420·5422 — 연구 중·완료 배지만 알약 · «lv/5» 는 민글자).
                //   전엔 초록 민글자만 있었다(pp_green_dk). 반지름은 표 `tech_node_time_r_rem` · 여백은 catalog `tt_time_*`.
                RectTransform pill = UiKit.Box(parent, "time-" + id);
                Image bg = RadiusUi.Rounded(pill, "bg", "pp_ink", "tech_node_time_r_rem");
                UiKit.Fill(bg.rectTransform);
                label = DungeonPopups.Bold(pill, "label-" + id, TextKind.Sub, badge, "pp_green");
                UiKit.Fill(label.rectTransform);
                float mt = DungeonPopups.RemL("tt_time_mt_rem");
                nodePills[id] = pill;
                nodePillPos[id] = new Vector3(x + d * 0.5f, top + d + mt, labelH - mt);
                PlacePill(id, badge);
            }
            else
            {
                label = DungeonPopups.Bold(parent, "label-" + id, TextKind.Sub, badge, "pp_ink");
                UiKit.Place(label.rectTransform, x - d, top + d, d * 3f, labelH);
            }
            string nid = id;
            nodes[id] = UiKit.Button(rt, "hit", () => TechPopups.OpenNode(nid));
            WrapUi.Apply(label, "tech_tree_label");   // T361 7회차 — 정본 white-space 표(WrapUi.json) 2207 `.tech-tree-label { nowrap }`
            nodeLabels[id] = label;
            nodeIds.Add(id);
        }

        /// <summary>시간 배지를 글자 폭 + 정본 패딩(.45rem 양쪽)으로 노드 아래 가운데에 놓는다 — 글자가 바뀔 때마다(정본 inline-block 이 그렇게 준다).</summary>
        void PlacePill(string id, string text)
        {
            RectTransform pill; Vector3 pos;
            if (!nodePills.TryGetValue(id, out pill) || !nodePillPos.TryGetValue(id, out pos)) return;
            float padX = DungeonPopups.RemL("tt_time_pad_x_rem");
            float pw = PetSkillKit.TextWidth(TextKind.Sub, text) + padX * 2f;
            UiKit.Place(pill, pos.x - pw * 0.5f, pos.y, pw, pos.z);
        }

        /// <summary>1초마다 — 연구 남은 시간 표기 갱신(원작 백그라운드 틱의 화면 몫). 완료로 넘어가는 순간은 다시 그린다.</summary>
        private void Update()
        {
            if (!DungeonUiHost.Ready || Tree == null) return;
            if (Time.unscaledTime < tickAt) return;
            tickAt = Time.unscaledTime + 1f;
            string rid = Tree.ResearchingId();
            if (rid == null) { TechPopups.Tick(); return; }
            bool done = Tree.IsDone(Host.Now());
            if (Current == View.Branch)
            {
                TextMeshProUGUI l;
                if (nodeLabels.TryGetValue(rid, out l))
                {
                    string want = done ? "완료!" : NumFmt.FmtTime((Tree.State.Research.EndsAt - Host.Now()) / 1000);
                    if (done && l.text != "완료!") Render();
                    else if (l.text != want) { l.text = want; PlacePill(rid, want); }
                }
            }
            else
            {
                foreach (KeyValuePair<string, TextMeshProUGUI> kv in cardPct) kv.Value.text = BranchPctText(kv.Key);
            }
            TechPopups.Tick();
        }

        /// <summary>연구 시작·건너뛰기·완료 뒤 — 알약·트리·상단바(원작 renderTechBranchView + renderTopBar).</summary>
        public void RefreshAfterChange()
        {
            Host.SaveTechState();
            if (gameObject.activeSelf) Render();
            Host.RenderTopBar();
        }
    }
}
