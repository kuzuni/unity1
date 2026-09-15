using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.CraftFx;
using Forge.Core.Forging;
using Forge.Core.Mounts;
using Forge.Game.Gallery;

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
        /// <summary>모루 «그림» 칸(정본 `.anvil-svg`) — 두들기기 반동은 버튼이 아니라 이 칸에 건다(정본 주석 · <see cref="AnvilFxSpec.BumpOriginFrac"/>).</summary>
        static RectTransform anvilArtRt;
        /// <summary>달군 쇳덩이 묶음(정본 `.anv-billet` · 축 = viewBox 55 21.5)과 불투명도가 애니메이션되는 두 겹(`.ab-hot`·`.ab-cool`).</summary>
        static RectTransform billetRt;
        static Image billetHot, billetCool, billetGlow;
        /// <summary>두들기는 동안만 사는 망치 오버레이(정본 `.anvil-fx .af-hammer`)와 그 불투명도 묶음.</summary>
        static RectTransform hammerRt;
        static CanvasGroup hammerGroup;
        /// <summary>타격 링 셋(정본 `.af-ring.h0/h1/h2` · 망치와 **형제**라 같이 움직이지 않는다).</summary>
        static readonly Image[] rings = new Image[3];
        /// <summary>접지 그림자·순백 코어(정본 `.af-shadow.dN`·`.af-core.cN` · 링과 같은 자리 계보).</summary>
        static readonly Image[] shadows = new Image[3];
        static readonly Image[] cores = new Image[3];
        /// <summary>4갈래 섬광 — 타격마다 좌·우 두 조각(정본 `.af-star.sl/.sr`).</summary>
        static readonly Image[] starsL = new Image[3];
        static readonly Image[] starsR = new Image[3];
        /// <summary>타격 섬광 웅덩이·잔열(정본 `.af-flash.fN`·`.af-heat.tN`).</summary>
        static readonly Image[] flashes = new Image[3];
        static readonly Image[] heats = new Image[3];
        /// <summary>블룸(정본 `.af-bloom.bN`) — 타격 순간 버튼 전체를 들어 올린다.</summary>
        static readonly Image[] blooms = new Image[3];

        /// <summary>연기 칸 셋(정본 `.af-smoke.m0/m1/m2`).</summary>
        static readonly Image[] smokes = new Image[3];

        /// <summary>불티 칸(정본 `.af-spark` · 타격마다 7·11·16개 = 34개) 과 그 표.</summary>
        static Image[] sparks = new Image[0];
        static Forge.Core.CraftFx.AutoForgeFxSpec.SparkSpec[] sparkSpecs = new Forge.Core.CraftFx.AutoForgeFxSpec.SparkSpec[0];

        /// <summary>지금 세워진 불티들의 표(테스트가 «화면 변위 ↔ 표» 를 대조할 때 쓴다 · 난수라 테스트가 제 손으로 다시 뽑으면 순서가 어긋난다).</summary>
        public static Forge.Core.CraftFx.AutoForgeFxSpec.SparkSpec[] SparkSpecs { get { return sparkSpecs; } }

        /// <summary>흑피 칸(정본 `.af-scale` · 타격마다 2·3·5개)과 그 표.</summary>
        static Image[] scales = new Image[0];
        static Forge.Core.CraftFx.AutoForgeFxSpec.SparkSpec[] scaleSpecs = new Forge.Core.CraftFx.AutoForgeFxSpec.SparkSpec[0];

        /// <summary>지금 세워진 흑피들의 표.</summary>
        public static Forge.Core.CraftFx.AutoForgeFxSpec.SparkSpec[] ScaleSpecs { get { return scaleSpecs; } }

        /// <summary>모루 오버레이의 «viewBox 한 단위 = 몇 px» (같은 대조에 쓴다).</summary>
        public static float VbUnit { get { return vbUnit; } }
        /// <summary>viewBox 한 단위의 화면 px — 망치 `afswing` 의 translate 는 **viewBox 단위**다(SVG 자식이라 CSS px 가 아니다).</summary>
        static float vbUnit;

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
            hammerText = null; upgText = null; anvilRt = null; anvilArtRt = null;
            billetRt = null; billetHot = null; billetCool = null; billetGlow = null;
            hammerRt = null; hammerGroup = null; vbUnit = 0f;
            for (int i = 0; i < rings.Length; i++) { rings[i] = null; shadows[i] = null; cores[i] = null; starsL[i] = null; starsR[i] = null; flashes[i] = null; heats[i] = null; blooms[i] = null; }
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
                PressFx pc = c.GetComponent<PressFx>();
                if (pc != null) pc.SetBase(c.anchoredPosition, c.localScale);   // T355 — 눌림의 기준 자리는 놓인 뒤
            }
            RectTransform mount = MountCell(grid, h, cell * 2f + colGap, cell);
            UiKit.Place(mount, 3 * (cell + colGap), cell + rowGap, cell * 2f + colGap, cell);
            PressFx pm = mount.GetComponent<PressFx>();
            if (pm != null) pm.SetBase(mount.anchoredPosition, mount.localScale);

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
            Button ab = AutoBtn(right, h, unlocked, unlocked ? (h.AutoOn ? "pp_green" : "pp_blue") : "pp_gray", unlocked ? (h.AutoOn ? "pp_green_dk" : "pp_blue_dk") : "pp_gray_dk", btnW * 0.85f - gap, btnH);
            UiKit.Place(ab.GetComponent<RectTransform>(), btnW * 1.15f + gap, 0f, btnW * 0.85f - gap, btnH);
            if (h.Upgrading)
            {
                upgText = UiKit.Text(right, "equip-upg-time", TextKind.Sub, RemainText(h), "ink");
                upgText.fontStyle = FontStyles.Bold;
                UiKit.Place(upgText.rectTransform, 0f, btnH + rem * 0.25f, W - padX * 2f - rx, PopupKit.FontSize(TextKind.Sub) * 1.3f);
            }
            // 두들기는 도중 다시 그려졌다면(세이브 → Rerender) 러너를 새 모루·시트에 다시 문다 — 흐른 시간은 지킨다.
            if (h.Striking) { AnvilFx fx = AnvilFx.Ensure(sheet); if (fx != null) { fx.Rebind(anvilArtRt, sheet, billetRt, billetHot, billetCool, billetGlow, hammerRt, hammerGroup, rings, shadows, cores, starsL, starsR, flashes, heats, blooms, vbUnit); fx.TakeSparks(sparks, sparkSpecs, scales, scaleSpecs, smokes); } }
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
            if (on) { fx.Play(anvilArtRt, root.Sheet, billetRt, billetHot, billetCool, billetGlow, hammerRt, hammerGroup, rings, shadows, cores, starsL, starsR, flashes, heats, blooms, vbUnit); fx.TakeSparks(sparks, sparkSpecs, scales, scaleSpecs, smokes); }
            else fx.Stop();
        }

        static Button TwoLineBtn(Transform parent, string name, string label, string face, string lip, UnityEngine.Events.UnityAction onClick, float w, float h)
        {
            Button b = PopupKit.Btn(parent, name, label, face, lip, onClick, w, h, "stage_ink", TextKind.Sub);
            TextMeshProUGUI t = b.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) { t.textWrappingMode = TextWrappingModes.Normal; t.lineSpacing = -20f; }
            return b;
        }

        /// <summary>
        /// T108 — 자동 제련 버튼. 정본 `ui.js` 1551 `자동${IconGen.img('autoloop','auto-loop-ico')}<br>${unlocked ? (on ? 'ON' : 'OFF') : IconGen.img('lock')}` —
        /// 윗줄 «자동» + 고리 **아이콘**(글자 ↻ 가 아니다 · 주인 글꼴에 없어 □ 였다) · 아랫줄 ON/OFF 또는 잠금 **아이콘**(글자 🔒 가 아니다).
        /// 두 줄이라 세로 갈래 <see cref="IconTextStack"/>(T110 1회차)로 세우고 아이콘은 T31 아틀라스 키로 직접 단다(정본도 이모지 표가 아니라 `IconGen.img` 다).
        /// 아이콘 치수는 카탈로그(정본 `.auto-loop-ico` 1.15em · `.ico` 1.45em · 마진). 라벨 키라인은 <see cref="PopupKit.Btn"/> 과 같은 면 키 표(<see cref="KeylineUi.BtnFace"/>).
        /// </summary>
        static Button AutoBtn(Transform parent, ForgeHost h, bool unlocked, string face, string lip, float w, float hgt)
        {
            Button b = PopupKit.Btn(parent, "auto-btn", "", face, lip, () => h.OnAutoForgeBtn(), w, hgt, "stage_ink", TextKind.Sub);
            RectTransform rt = b.GetComponent<RectTransform>();
            Transform plain = rt.Find("label");
            if (plain != null) plain.gameObject.SetActive(false);   // Btn 이 세운 빈 한 줄 라벨은 끈다 — 줄은 아래 세로 갈래가 쥔다
            float fs = PopupKit.FontSize(TextKind.Sub);
            RectTransform stack = IconTextStack.Build(rt, "label-stack", TextKind.Sub, unlocked ? "자동\n" + (h.AutoOn ? "ON" : "OFF") : "자동\n", "stage_ink");
            stack.offsetMin = new Vector2(0f, UiKit.H("btn_lip"));   // Btn 라벨과 같은 자리(아래턱 위)
            RectTransform l1 = (RectTransform)stack.Find("line-1"), l2 = (RectTransform)stack.Find("line-2");
            IconTextStack.AppendIcon(l1, "autoloop", fs * UiKit.L("auto_loop_ico_em"), fs, fs * UiKit.L("auto_loop_ico_ml_em"), fs * UiKit.L("ico_mr_em"));
            if (!unlocked) IconTextStack.AppendIcon(l2, "lock", fs * UiKit.L("ico_em"), fs, 0f, fs * UiKit.L("ico_mr_em"));
            string kl = KeylineUi.BtnFace(face);
            foreach (TextMeshProUGUI t in UiKit.RowTexts(stack))
            {
                t.fontStyle = FontStyles.Bold;
                if (!string.IsNullOrEmpty(kl)) PopupKit.Ring(t, kl, "pp_line");
            }
            IconTextStack.Fit(stack);
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
                // T342 ⓐ — 정본 style.css 862 `filter: grayscale(1) brightness(1.75) opacity(.52)`.
                // 여태 알파(.52)만 있었다: 틴트는 곱하기라 «회색 눕히기»·«밝기 올리기» 를 못 한다(마룬 타일 위에서 형태가 안 읽힌다 —
                // 정본 주석 860~861 이 바로 그 까닭으로 밝기를 올려 «어두운 면 위 밝은 실루엣» 으로 뒤집은 자리다).
                UiFilter.ApplyColor(ico, "equip_cell_empty");
                float k = size * 0.72f;
                UiKit.Anchor(ico.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, size * 0.06f), k, k);
                TextMeshProUGUI nm = UiKit.Text(rt, "slot-name", TextKind.Sub, d.SlotKr.Get(slot, slot), "pp_muted");
                nm.fontStyle = FontStyles.Bold;
                UiKit.TextShadow(nm, "slot_name");            // T333 8회차 — 정본 8030 `.equip-cell .slot-name` 두 겹 중 첫째(아래 1px 드롭 · 표 slot_name)
                UiKit.Anchor(nm.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, size * 0.05f), size, nm.fontSize * 1.2f);
                return rt;
            }
            Color ac = ForgeUi.AgeColor(d, it.Age);
            Image f = ForgeUi.Tile(rt, "frame", ForgeUi.CellFace(ac), ForgeUi.CellLine(ac), size * 0.16f, PopupKit.Line3);
            AgePattern.Attach(rt, it.Age, cell: true, mask: false, siblingIndex: 1);   // T124 — 정본 `.equip-cell[data-age]::before`(흐림 .55 · 썸네일 뒤)
            Image img = PopupKit.IconOr(rt, "img", ForgeUi.ItemIconKey(d, it));
            float kk = size * 0.76f;
            UiKit.Anchor(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, size * 0.06f), kk, kk);
            ForgeUi.ApplyThumb(rt, ItemFaces.Get(d, it), size);   // T122 3회차 — 정본 equipCellHTML(ui.js 3103) 의 itemImgHTML: 3D 썸네일이 있으면 그것, 없으면(장신구) 실루엣 그대로
            ForgeUi.LvBadge(rt, it.Level, size);
            ForgeUi.StarBadge(rt, it.Stars, size);
            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = f;
            string s = slot;
            b.onClick.AddListener(() => GearDetailPopup.Open(h, s));
            PressFx.Attach(rt.gameObject, rt, "equip_cell", f);   // T355 ⓑ — 정본 7743·7750 .equip-cell:not(.egg-cell):active { translateY(.08rem); brightness(1.07) · .08s ease-out } · 기준 자리는 Place 뒤 SetBase
            return rt;
        }

        /// <summary>마지막 칸 = 탈것 슬롯 — 정본 `ui.js` 1526~1535 은 **탄 탈것이 있으면** 그 얼굴 + `Lv.N` + 여분 «+N» 을 그리고,
        /// 없을 때만 실루엣 + «탈것» 이다(T381 1회차 · 종전 클론은 빈 갈래 하나뿐이었다). 누르면 탈것 창(원작 `UI.openMounts()`).</summary>
        static RectTransform MountCell(Transform parent, ForgeHost h, float w, float hgt)
        {
            RectTransform rt = UiKit.Box(parent, "egg-cell");
            Image f = ForgeUi.Tile(rt, "frame", new Color(0x4f / 255f, 0xb2 / 255f, 0xee / 255f), Color.black, hgt * 0.16f, PopupKit.Line3);

            // T381 1회차 — 탄 탈것 갈래. 얼굴은 소환 시트와 **같은 썸네일 공장**(PetFaces · 원작 mountFace)을 부르기만 한다(그 파일은 남의 lock).
            //   그림을 못 구우면(에디터 배치·그래픽 없음) 정본의 빈 갈래로 떨어진다 — 칸이 비어 보이는 것보다 실루엣이 낫다.
            MountSystem ms = PetSkillHost.Instance != null ? PetSkillHost.Instance.Mounts : null;
            Mount ridden = ms != null ? ms.RiddenInst() : null;
            Sprite face = ridden != null ? PetFaces.Get(ridden.Name, GalleryKind.Mounts) : null;
            if (ridden != null && face != null)
            {
                PopupKit.IconOr(rt, "img", "horse");                                     // 자리를 먼저 세우고(못 구웠으면 이 실루엣이 남는다)
                ForgeUi.ApplyThumb(rt, face, hgt);                                       // 장비 칸과 **같은 깔때기** — 접지 그림자(T332)까지 그 한 곳이 건다
                ForgeUi.LvBadge(rt, ridden.Level, hgt);                                  // 정본 1532 `<span class="cell-lv">Lv.N</span>`
                int extra = ms.State != null && ms.State.ActiveMounts != null ? Mathf.Max(0, ms.State.ActiveMounts.Count - 1) : 0;
                if (extra > 0) MountCountBadge(rt, extra, w);                            // 정본 1533 «+N»(UI.MOUNT_COUNT_STYLE)
                Button rb = rt.gameObject.AddComponent<Button>();
                rb.targetGraphic = f;
                rb.onClick.AddListener(() => MountSheet.Open());
                PressFx.Attach(rt.gameObject, rt, "egg_cell", f);
                return rt;
            }

            Image ico = PopupKit.IconOr(rt, "mount-sil", "horse");
            UiFilter.ApplyColor(ico, "mount_slot_empty");   // T342 ⓑ — 정본 857 .equip-cell.egg-cell.empty .mount-sil { filter: brightness(0) opacity(.32) } (표 FilterUi · 종전 검정 .32 박힘)
            float k = hgt * 0.55f;
            UiKit.Anchor(ico.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, hgt * 0.08f), k, k);
            TextMeshProUGUI nm = UiKit.Text(rt, "slot-name", TextKind.Sub, "탈것", "stage_ink");
            nm.fontStyle = FontStyles.Bold;
            PopupKit.Ring(nm, "pp_line", 0.2f);
            // T333 8회차 — 정본 874 `.equip-cell.egg-cell .slot-name`(표 slot_name_egg · 위 EquipCell 과 다른 키인 까닭은 특이도다)
            UiKit.TextShadow(nm, "slot_name_egg");
            UiKit.Anchor(nm.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, hgt * 0.06f), w, nm.fontSize * 1.2f);
            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = f;
            b.onClick.AddListener(() => MountSheet.Open());   // 원작 `UI.openMounts()` — T20 탈것 시트(전체 모달)
            PressFx.Attach(rt.gameObject, rt, "egg_cell", f);   // T355 ⓑ — 정본 8087 .equip-cell.egg-cell:active { translateY(.08rem); brightness(1.07) }
            return rt;
        }

        /// <summary>여분 탈것 «+N» 배지 — 정본 `UI.MOUNT_COUNT_STYLE`(ui.js 16~18 · 칸 오른쪽 위 · 검정 .62 알약에 흰 .5 테). 수는 표 `MountCellUi.json`.</summary>
        static void MountCountBadge(RectTransform cell, int extra, float w)
        {
            float rem = PopupKit.Rem, css = KeylineUi.CssPx;
            float line = MountCellUi.F("count_line_px") * css;
            TextMeshProUGUI t = UiKit.Text(cell, "cell-count", TextKind.Sub, "+" + extra, "stage_ink");
            t.fontStyle = FontStyles.Bold;
            float tw = t.GetPreferredValues().x + MountCellUi.F("count_pad_x_rem") * rem * 2f;
            float th = t.fontSize * 1.25f;                                                   // ui.js 18 line-height:1.25
            RectTransform box = UiKit.Box(cell, "cell-count-box");
            UiKit.Anchor(box, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-MountCellUi.F("count_right_rem") * rem, -MountCellUi.F("count_top_rem") * rem), tw, th);
            float r = MountCellUi.F("count_r_rem") * rem;
            Image rim = UiKit.Rounded(box, "line", "pp_line", r);
            rim.color = MountCellUi.C("count_line");
            Image bg = UiKit.Rounded(box, "bg", "pp_ink", Mathf.Max(1f, r - line));
            bg.color = MountCellUi.C("count_bg");
            PopupKit.Inset(bg.rectTransform, line);
            t.transform.SetParent(box, false);
            UiKit.Fill(t.rectTransform);
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
        }

        /// <summary>모루 자리 — 보류 제작품이 있으면 모루 대신 그 카드(더미 두께 = HeldDeckDepth). 망치 수(원작 `small#anvil-hammers`)는 모루일 때 **받침 위**(T61 · shot-042120 실측 61%)에, 카드일 때 카드 아래에.</summary>
        static RectTransform AnvilSlot(Transform parent, ForgeHost h, float w, float hgt)
        {
            // 두들기는 동안은 **모루**가 보여야 한다 — 정본은 `.anvil-btn.striking` 이 1.5초를 다 쓰고 `done()` 에서야 카드를 얹는다.
            // 클론은 `SetPendingCraft` 의 세이브가 `Rerender` 를 불러 그 순간 카드로 갈아 치웠다(런 157: 모루가 파괴돼 연출이 사라졌다).
            ForgeItem held = h.Striking ? null : h.HeldItem;
            float rem = PopupKit.Rem;
            Button b = UiKit.Button(parent, held == null ? "anvil-btn" : "held-slot", held == null ? (UnityEngine.Events.UnityAction)(() => h.OnCraft()) : () => h.OnOpenHeld());
            RectTransform rt = b.GetComponent<RectTransform>();
            float counterH = PopupKit.FontSize(TextKind.Sub) * 1.3f;
            RectTransform counter;
            if (held == null)
            {
                RectTransform anvil = UiKit.Box(rt, "anvil");
                UiKit.Place(anvil, 0f, 0f, w, hgt);
                UnityEngine.Rect baseRect = DrawAnvil(anvil, w, hgt, h.Striking);
                anvilArtRt = anvil;
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
                // T371 4회차 — 정본 1039~1050: 겹마다 **두 장**이다 — `dg*i` 자리에 어두운 틈(`--dgap` 15%, #05060a) · 그 위 `dg*i − 1px` 자리에
                //   밝은 단면(`--dedge`). 뒤 장이 오른쪽 1px 만 드러나 «단면 + 어두운 선» 이 되고, 그것이 정본 주석이 말한 «에지가 여러 겹 보이는
                //   세로 줄무늬» 다. 클론은 밝은 단면만 깔아 겹 사이 선이 없었다(어두운 시대색에서 줄이 통째로 사라진다 — 정본이 겪고 적어 둔 그 병).
                float line1 = UiKit.L("line_px");
                for (int i = depth; i >= 1; i--)
                {
                    Image gap = UiKit.Rounded(rt, "deck-gap-" + i, "pp_paper", rem * 0.7f);
                    gap.color = ColorMixUi.Mix("held_deck_gap", ac);
                    UiKit.Place(gap.rectTransform, dg * i, 0f, w - dg * depth, bodyH);
                    Image edge = UiKit.Rounded(rt, "deck-" + i, "pp_paper", rem * 0.7f);
                    // T371 2회차 — 정본 1013 `--dedge: color-mix(in srgb, var(--rc) 55%, #dfe4ec)`(비율·상대색이 코드에 박혀 있었다).
                    edge.color = ColorMixUi.Mix("held_deck_edge", ac);
                    UiKit.Place(edge.rectTransform, dg * i - line1, 0f, w - dg * depth, bodyH);
                }
                RectTransform card = UiKit.Box(rt, "card");
                UiKit.Place(card, 0f, 0f, w - dg * depth, bodyH);
                // T371 2회차 — 정본 990 `.anvil-btn.held-slot { background: color-mix(… 30%, #17181a); border: color-mix(… 80%, #000) }`.
                //   값은 맞았지만 비율·상대색이 코드에 박혀 있었다(§1) — 표 `ColorMixUi.json` 의 held_face·held_line 로.
                ForgeUi.Tile(card, "frame", ColorMixUi.Mix("held_face", ac), ColorMixUi.Mix("held_line", ac), rem * 0.7f, PopupKit.Line3);
                float ico = rem * 2.1f;
                Image img = PopupKit.IconOr(card, "held-img", ForgeUi.ItemIconKey(h.Defs, held));
                UiKit.Anchor(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, rem * 0.35f), ico, ico);
                TextMeshProUGUI nm = UiKit.Text(card, "held-name", TextKind.Sub, held.Name, "stage_ink");
                nm.fontStyle = FontStyles.Bold;
                // T351 — 정본 1003~1006 .held-name { max-width: 100%; overflow: hidden; text-overflow: ellipsis; white-space: nowrap }:
                //   폭은 카드(= 정본 max-width 100%) · 높이는 실제 줄높이(TextClamp.BoxHeight — 1.2배 상자에 Ellipsis 를 걸면 TMP 가 줄을 통째로 버린다 · 런 528)
                //   · 가운데 정렬이라 상자가 커진 만큼 반을 내려 글자 자리는 그대로.
                TextClamp.Apply(nm, "held_name");
                float nmH = TextClamp.BoxHeight(nm, "held_name"), nmOld = nm.fontSize * 1.2f;
                UiKit.Anchor(nm.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, rem * 0.25f - (nmH - nmOld) * 0.5f), w - dg * depth, nmH);
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
        static UnityEngine.Rect DrawAnvil(RectTransform rt, float w, float h, bool striking)
        {
            float vbW = UiKit.L("anvil_vb_w"), vbH = UiKit.L("anvil_vb_h");
            float u = Mathf.Min(w / vbW, h / vbH);
            float ox = (w - vbW * u) * 0.5f, oy = (h - vbH * u) * 0.5f;
            // T98 2회차 — 옛 «둥근 사각 여섯» 을 걷고 **정본 SVG 폴리곤**으로 바꿨다(`AnvilArt` · 표는 Resources/AnvilArtUi.json).
            // 옛 그림은 상판·앞면·받침이 전부 둥근 사각이고 뿔이 둥근 사각이라 확대 대조에서 «주황 블록 + 로켓 노즈» 로 읽혔다(T98 등재).
            // 겹 순서·꼭짓점·획(#170d0b 3)·상판 위 어휘(스텝·하디 홀·프리첼 홀)는 전부 표가 쥔다 — 여기는 자리와 크기만 준다.
            RectTransform art = AnvilArt.Build(rt, "anvil-art", u);
            UiKit.Place(art, ox, oy, vbW * u, vbH * u);
            UnityEngine.Rect ab = AnvilArt.PartBounds("anv-base");
            UnityEngine.Rect bas = new UnityEngine.Rect(ox + ab.x * u, oy + ab.y * u, ab.width * u, ab.height * u);
            DrawBillet(rt, ox, oy, u, vbW, vbH);
            vbUnit = u;
            // ⚠ 연출 오버레이는 **모루 그림의 형제**다(정본 `.anvil-btn` 안에서 `.anvil-svg` 와 나란히) — 그림의 자식으로 두면
            //   망치·링·섬광이 모루의 반동(`anvilbump`)을 그대로 타고 내려가 «상대변위 0» 이 된다(정본이 망치에 대해 못 박은 함정 · 15회차 자가 잡았다).
            if (striking) DrawHammer(rt.parent as RectTransform, ox, oy, u, vbW, vbH);
            SetFxOrigin(rt, ox, oy, u, vbW, vbH, w, h);
            return bas;
        }

        /// <summary>
        /// 달군 쇳덩이(정본 `ANVIL_SVG` 의 `<g class="anv-billet">`) — 상판 위에 누운 8각 각봉과 그 겹들. **정지 상태에도 있다**(정본도 SVG 에 늘 들어 있다).
        /// 정본 주석이 못 박은 것 셋: 🚨 모서리를 둥글리지 말 것(둥근 알약은 UI 어휘다 · 챔퍼로 깎은 각봉) · 🚨 키라인이 있어야 «상판에 찍힌 얼룩» 이 아니라 물체가 된다
        /// (검정 대신 식은 쇠색 `billet_line`) · 🚨 몸통 색을 노란 단조 온도로 올려 주황 상판과 색상환을 벌린다.
        /// 그리는 순서 = SVG 순서: 키라인 → 몸통 → 백열 → 윗면 띠 → 식은색 → 접합선. (빛 웅덩이 `.ab-glow` 는 방사 그라디언트라 다음 회차.)
        /// </summary>
        static void DrawBillet(RectTransform parent, float ox, float oy, float u, float vbW, float vbH)
        {
            billetRt = UiKit.Box(parent, "billet");
            UiKit.Place(billetRt, ox, oy, vbW * u, vbH * u);
            // 축은 정본 `transform-origin: 55px 21.5px`(빌릿 밑면) — 눌릴 때 밑면이 상판을 파고들지 않게.
            float pxo = (float)(AnvilFxSpec.BilletOriginVb[0] / vbW), pyo = (float)(AnvilFxSpec.BilletOriginVb[1] / vbH);
            billetRt.pivot = new Vector2(pxo, 1f - pyo);
            billetRt.anchoredPosition = new Vector2(ox + vbW * u * pxo, -(oy + vbH * u * pyo));

            Vector2[] bar = VbPoly("billet_bar", 8);
            Vector2[] top = VbPoly("billet_top", 4);
            Vector2[] seam = VbPoly("billet_seam", 4);
            Color[] body = { UiKit.C("billet_g0"), UiKit.C("billet_g1"), UiKit.C("billet_g2"), UiKit.C("billet_g3") };
            float[] bodyOff = { 0f, UiKit.L("billet_g1_off"), UiKit.L("billet_g2_off"), 1f };
            Color[] hot = { UiKit.C("billet_hot0"), UiKit.C("billet_hot1"), UiKit.C("billet_hot2") };
            float[] hotOff = { 0f, UiKit.L("billet_hot1_off"), 1f };
            Vector2 gradTo = new Vector2(UiKit.L("billet_grad_x2"), 1f);

            // 그리는 순서 = 정본 SVG 순서(뒤 → 앞). 맨 뒤는 달군 쇠가 상판에 흘리는 빛 웅덩이다
            // (정본 주석: «빛을 내는 물체는 제 발밑을 어둡게 만들지 않는다» · 크게 깔면 상판이 뿌옇게 떠 모루 형태가 죽는다).
            float grx = UiKit.L("billet_glow_rx"), gry = UiKit.L("billet_glow_ry");
            RectTransform glowRt = UiKit.Box(billetRt, "ab-glow");
            Image glow = glowRt.gameObject.AddComponent<Image>();
            glow.raycastTarget = false;
            glow.sprite = CraftFxPoly.BakeEllipse("ab-glow", grx, gry,
                new Color[] { UiKit.C("billet_glow0"), UiKit.C("billet_glow1"), UiKit.C("billet_glow2") },
                new float[] { 0f, UiKit.L("billet_glow_off1"), 1f });
            glow.type = Image.Type.Simple;
            UiKit.Place(glowRt, (UiKit.L("billet_glow_cx") - grx) * u, (UiKit.L("billet_glow_cy") - gry) * u, grx * 2f * u, gry * 2f * u);
            billetGlow = glow;
            SetOpacity(billetGlow, (float)AnvilFxSpec.BilletGlow.Sample1(0));   // 정지 상태 = 트랙 0%(.5)

            BilletLayer("ab-bar-line", CraftFxPoly.Inflate(bar, UiKit.L("billet_stroke") * 0.5f), null, null, gradTo, UiKit.C("billet_line"), u);
            BilletLayer("ab-bar", bar, body, bodyOff, gradTo, Color.white, u);
            billetHot = BilletLayer("ab-hot", bar, hot, hotOff, new Vector2(0f, 1f), Color.white, u);
            SetOpacity(billetHot, (float)AnvilFxSpec.BilletHot.Sample1(0));   // 정지 상태 = 트랙 0%(노란 단조열 .12)
            BilletLayer("ab-top", top, null, null, gradTo, Alpha(UiKit.C("billet_top"), UiKit.L("billet_top_alpha")), u);
            billetCool = BilletLayer("ab-cool", bar, null, null, gradTo, UiKit.C("billet_cool"), u);
            SetOpacity(billetCool, (float)AnvilFxSpec.BilletCool.Sample1(0)); // 정지 상태 = 0(안 보인다)
            BilletLayer("ab-seam", seam, null, null, gradTo, Alpha(UiKit.C("billet_seam"), UiKit.L("billet_seam_alpha")), u);
        }

        /// <summary>
        /// 망치 오버레이(정본 `ui.js HAMMER_SVG` · `.anvil-fx .af-hammer`) — **두들기는 동안에만** 있다(정본도 `startAnvilStrike` 가 오버레이를 만들었다 끝나면 지운다).
        /// 좌표계: 정본은 `<g transform="translate(55,11) rotate(-20)">` 안에 «타격면 중심이 원점» 인 로컬 좌표로 그린다 — 그 원점이 곧 `.af-hammer` 의
        /// `transform-origin: 55px 11px`(빌릿 윗면)이고 `afswing` 의 회전·스케일 축이다. 그래서 칸의 **피벗을 그 점에 두고** 거치 각(−20°)은 그림 쪽에 건다.
        /// ⚠ `afswing` 의 translate 는 **viewBox 단위**다(SVG 자식의 transform) — 모루·시트의 CSS px 환산(`anvil_fx_px`)과 다른 자다.
        /// 그리는 순서 = SVG 순서: 손잡이(키라인) → 손잡이 → 그립 → 머리(키라인) → 머리 → 어깨 그늘 → 타격면 띠.
        /// </summary>
        static void DrawHammer(RectTransform parent, float ox, float oy, float u, float vbW, float vbH)
        {
            // 정본 SVG 순서 그대로 쌓는다(먼저 그린 것이 뒤 · `ui.js` 2860~2915):
            //   블룸 → 잔열 → 플래시 → 링 → 접지 그림자 → 순백 코어 → 연기 → 흑피 → 불티 → **망치** → 섬광
            // ⚠ 섬광이 망치 **앞**인 것이 요점이다 — 정본 주석: «뒤에 두면 흰 코어가 통째로 머리(반폭 9.4)에 가려
            //   밖으로 나오는 건 주황 스커트뿐이고, 그게 주황 상판에 얹혀 '얼룩'이 된다».
            RectTransform fx = UiKit.Box(parent, "anvil-fx");
            UiKit.Place(fx, ox, oy, vbW * u, vbH * u);
            DrawImpactGradient(fx, u, blooms, "af-bloom", "bloom_rx", "bloom_ry",
                new string[] { "bloom0", "bloom1", "bloom2" }, new float[] { 0f, UiKit.L("bloom_off1"), 1f }, UiKit.L("bloom_dy"));
            DrawImpactGradient(fx, u, heats, "af-heat", "heat_rx", "heat_ry",
                new string[] { "heat0", "heat1", "heat2" }, new float[] { 0f, UiKit.L("heat_off1"), 1f });
            DrawImpactGradient(fx, u, flashes, "af-flash", "flash_rx", "flash_ry",
                new string[] { "flash0", "flash1", "flash2", "flash3" }, new float[] { 0f, UiKit.L("flash_off1"), UiKit.L("flash_off2"), 1f });
            DrawRings(fx, u, vbW, vbH);
            DrawImpactEllipse(fx, u, shadows, "af-shadow", "fx_shadow_rx", "fx_shadow_ry", "fx_shadow_dy", "fx_shadow");
            DrawImpactEllipse(fx, u, cores, "af-core", "fx_core_rx", "fx_core_ry", "fx_core_dy", "fx_core", true);
            DrawSmoke(fx, u);
            // 불티는 **망치보다 앞(= 아래)** 이다 — 정본 SVG 도 그 순서고, 그래서 사거리 하한이 머리 반폭보다 커야 가려지지 않는다(정본 주석).
            DrawSparks(fx, u);

            hammerRt = UiKit.Box(fx, "hammer");
            UiKit.Place(hammerRt, 0f, 0f, vbW * u, vbH * u);
            float pxo = (float)(AutoForgeFxSpec.HitX / vbW), pyo = (float)(AutoForgeFxSpec.HitY / vbH);
            hammerRt.pivot = new Vector2(pxo, 1f - pyo);
            hammerRt.anchoredPosition = new Vector2(vbW * u * pxo, -(vbH * u * pyo));
            hammerGroup = hammerRt.gameObject.AddComponent<CanvasGroup>();
            hammerGroup.blocksRaycasts = false;
            hammerGroup.interactable = false;
            hammerGroup.alpha = (float)AutoForgeFxSpec.SwingOpacity.Sample1(0);   // 0% — 옆에서 들어오기 전이라 안 보인다

            // 거치 각은 그림 쪽(정본의 안쪽 `<g transform="translate(55,11) rotate(-20)">`)이 든다 — 애니메이션 회전은 칸이 든다.
            // ⚠ 그림 칸은 **타격점에 붙은 크기 0 상자**여야 한다: 겹의 좌표가 «타격면 중심이 원점» 이고 거치 각도 그 점을 축으로 돈다.
            //   여기를 `Fill` 로 두면 원점이 viewBox 한가운데(66,43)로 밀려 망치가 모루 받침 쪽에 그려진다(런 191 컷 실측 — 화면을 보고 잡았다).
            RectTransform art = UiKit.Box(hammerRt, "hammer-art");
            UiKit.Anchor(art, new Vector2(pxo, 1f - pyo), new Vector2(0.5f, 0.5f), Vector2.zero, 0f, 0f);
            art.localRotation = Quaternion.Euler(0f, 0f, -UiKit.L("hmr_rest_deg"));   // CSS 의 +각은 시계 방향, 유니티는 반대

            Vector2[] head = LocalPoly("hmr_head");
            Vector2[] handle = LocalPoly("hmr_handle");
            Vector2[] grip = LocalPoly("hmr_grip");
            Vector2[] face = LocalPoly("hmr_face");
            Vector2[] shoulder = LocalPoly("hmr_shoulder");
            float st = UiKit.L("hmr_stroke");
            Color line = UiKit.C("hmr_line");
            Color[] steel = { UiKit.C("hmr_steel0"), UiKit.C("hmr_steel1"), UiKit.C("hmr_steel2"), UiKit.C("hmr_steel3") };
            float[] steelOff = { 0f, UiKit.L("hmr_steel_off1"), UiKit.L("hmr_steel_off2"), 1f };
            Color[] wood = { UiKit.C("hmr_wood0"), UiKit.C("hmr_wood1"), UiKit.C("hmr_wood2") };
            float[] woodOff = { 0f, UiKit.L("hmr_wood_off1"), 1f };
            Color[] leather = { UiKit.C("hmr_grip0"), UiKit.C("hmr_grip1"), UiKit.C("hmr_grip2") };
            float[] leatherOff = { 0f, UiKit.L("hmr_grip_off1"), 1f };
            Vector2 steelFrom = new Vector2(UiKit.L("hmr_steel_gx1"), 0f), steelTo = new Vector2(UiKit.L("hmr_steel_gx2"), 1f);
            Vector2 down = new Vector2(0f, 1f);

            HammerLayer(art, "hm-handle-line", CraftFxPoly.Inflate(handle, st * 0.5f), null, null, Vector2.zero, down, line, u);
            HammerLayer(art, "hm-handle", handle, wood, woodOff, Vector2.zero, down, Color.white, u);
            HammerLayer(art, "hm-grip", grip, leather, leatherOff, Vector2.zero, down, Color.white, u);
            HammerLayer(art, "hm-head-line", CraftFxPoly.Inflate(head, st * 0.5f), null, null, Vector2.zero, down, line, u);
            HammerLayer(art, "hm-head", head, steel, steelOff, steelFrom, steelTo, Color.white, u);
            HammerLayer(art, "hm-shoulder", shoulder, null, null, Vector2.zero, down, Alpha(UiKit.C("hmr_shadow"), UiKit.L("hmr_shoulder_alpha")), u);
            HammerLayer(art, "hm-face", face, null, null, Vector2.zero, down, Alpha(UiKit.C("hmr_face"), UiKit.L("hmr_face_alpha")), u);

            // 섬광은 **망치 앞**(= 마지막 자식)이다 — 정본이 그 이유를 적어 뒀다(뒤에 두면 흰 코어가 머리에 가려 주황 스커트만 남아 «얼룩» 이 된다).
            DrawStars(fx, u);
        }

        /// <summary>
        /// 연기 셋(`af-smoke`) — 정본: «타격마다 훅 오르고 **꼬리(650~900ms)를 채운다** … 92px 에서 **면적**으로 읽히는 몇 안 되는 요소다.
        /// 망치 **뒤**에 둬 머리를 흐리지 않는다». 축은 `transform-origin: 50% 100%`(밑변)이라 김이 **아래에서 피어오른다**.
        /// </summary>
        static void DrawSmoke(RectTransform fx, float u)
        {
            float rx = UiKit.L("smoke_rx"), ry = UiKit.L("smoke_ry");
            float dx = UiKit.L("smoke_dx"), dy = UiKit.L("smoke_dy");
            Color[] stops = { UiKit.C("smoke0"), UiKit.C("smoke1"), UiKit.C("smoke2") };
            float[] offs = { 0f, UiKit.L("smoke_off1"), 1f };
            Sprite sp = CraftFxPoly.BakeEllipse("af-smoke", rx, ry, stops, offs);
            for (int i = 0; i < smokes.Length; i++)
            {
                RectTransform rt = UiKit.Box(fx, "af-smoke-" + i);
                Image img = rt.gameObject.AddComponent<Image>();
                img.raycastTarget = false;
                img.sprite = sp;
                img.type = Image.Type.Simple;
                Color c = Color.white; c.a = 0f;      // 정지 상태는 투명(정본 `opacity: 0`)
                img.color = c;
                float cx = (float)AutoForgeFxSpec.HitCenterX(i) + dx, cy = (float)AutoForgeFxSpec.HitCenterY(i) + dy;
                UiKit.Place(rt, (cx - rx) * u, (cy - ry) * u, rx * 2f * u, ry * 2f * u);
                // 축 = 밑변 가운데(정본 `50% 100%`) — 커질 때 위로만 부푼다
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x + rx * u, rt.anchoredPosition.y - ry * 2f * u);
                smokes[i] = img;
            }
        }

        /// <summary>
        /// 타격 링 셋(정본 `.af-ring.hN`) — 타격마다 **닿는 자리**(`ui.js hx/hy` = 타격점 + 침하 + 걸어가는 dx)에 타원 테두리가 한 번 퍼진다.
        /// 정본 주석: 지연을 타격보다 8ms 앞에 둬야 «소리는 제때 나는데 그림만 메아리로 오는» 조합을 피한다.
        /// </summary>
        static void DrawRings(RectTransform fx, float u, float vbW, float vbH)
        {
            float rx = UiKit.L("ring_rx"), ry = UiKit.L("ring_ry");
            Sprite sp = CraftFxPoly.BakeRing("af-ring", rx, ry, UiKit.L("ring_stroke"));
            Color line = UiKit.C("ring_line");
            for (int i = 0; i < rings.Length; i++)
            {
                RectTransform rt = UiKit.Box(fx, "af-ring-" + i);
                Image img = rt.gameObject.AddComponent<Image>();
                img.raycastTarget = false;
                img.sprite = sp;
                img.type = Image.Type.Simple;
                Color c = line; c.a = 0f;                       // 정본 `.af-ring { opacity: 0 }` — 제 차례에만 켜진다
                img.color = c;
                float cx = (float)AutoForgeFxSpec.HitCenterX(i), cy = (float)AutoForgeFxSpec.HitCenterY(i);
                UiKit.Place(rt, (cx - rx) * u, (cy - ry) * u, rx * 2f * u, ry * 2f * u);
                rings[i] = img;
            }
        }

        /// <summary>방사 그라디언트 웅덩이 셋(잔열 · 플래시) — 타격점에 앉고 제 창에서만 <see cref="AnvilFx"/> 가 배율·불투명도를 바른다.</summary>
        static void DrawImpactGradient(RectTransform fx, float u, Image[] into, string name, string rxKey, string ryKey, string[] colorKeys, float[] offsets, float dy = 0f)
        {
            float rx = UiKit.L(rxKey), ry = UiKit.L(ryKey);
            Color[] stops = new Color[colorKeys.Length];
            for (int k = 0; k < colorKeys.Length; k++) stops[k] = UiKit.C(colorKeys[k]);
            Sprite sp = CraftFxPoly.BakeEllipse(name, rx, ry, stops, offsets);
            for (int i = 0; i < into.Length; i++)
            {
                RectTransform rt = UiKit.Box(fx, name + "-" + i);
                Image img = rt.gameObject.AddComponent<Image>();
                img.raycastTarget = false;
                img.sprite = sp;
                img.type = Image.Type.Simple;
                Color c = Color.white; c.a = 0f;
                img.color = c;
                img.material = CraftFxPoly.Screen();   // 정본: 블룸·플래시·잔열은 전부 `mix-blend-mode: screen`
                float cx = (float)AutoForgeFxSpec.HitCenterX(i), cy = (float)AutoForgeFxSpec.HitCenterY(i) + dy;
                UiKit.Place(rt, (cx - rx) * u, (cy - ry) * u, rx * 2f * u, ry * 2f * u);
                into[i] = img;
            }
        }

        /// <summary>
        /// 4갈래 섬광(정본 `.af-star.sl/.sr`) — 타격점에서 좌·우로 뻗는 두 조각. 정본 주석: «원형 광량만으로는 상판 얼룩과 구별이 안 된다.
        /// 축을 가진 별이 붙어야 *어디를 때렸는지* 와 *지금이 그 순간* 이 같이 읽힌다.»
        /// ⚠ 축은 조각의 **머리 쪽 끝**이다(왼쪽 조각은 bbox 오른쪽 · 오른쪽 조각은 왼쪽) — 가운데를 축으로 두면 커질 때 안쪽 끝이 머리 실루엣을 파고들어 흰 띠가 생긴다(정본 실측).
        /// 그라디언트도 축과 함께 뒤집힌다(안쪽이 흰색 불투명 · 바깥 끝이 투명).
        /// </summary>
        /// <summary>
        /// 불티 34개(7·11·16) — 정본은 `ui.js` 가 개체마다 `<path class="af-spark">` 를 인라인 스타일(`--a`·`--u`·`--v`·`--t`·`--dur`)과 함께 찍는다.
        /// 클론은 표(<see cref="AutoForgeFxSpec.BuildSparks"/>)를 뽑아 칸을 세우고, 도형은 **기준 길이 쐐기 한 장**을 구워 칸 너비로 늘린다.
        /// 축(피벗)은 정본 `transform-origin: 0% 50%` 대로 **꼬리 = 타격점**이다(회전·이동이 그 점을 중심으로 돈다).
        /// </summary>
        static void DrawSparks(RectTransform fx, float u)
        {
            float len = UiKit.L("spark_len"), tail = UiKit.L("spark_tail");
            Sprite sp = CraftFxPoly.BakeSpark("af-spark", len, tail, UiKit.L("spark_tip_top"), UiKit.L("spark_tip_bot"),
                UiKit.L("spark_halo_r"), UiKit.L("spark_glow_r"), UiKit.C("spark_halo"), UiKit.C("spark_glow"));
            // 후광까지 구운 텍스처라 도형보다 넓다 — 늘릴 때 그 여백도 같은 비율로 늘어나므로 칸 크기에 같이 넣는다.
            float pad = Mathf.Max(UiKit.L("spark_halo_r"), UiKit.L("spark_glow_r"));
            Color[] tints = { UiKit.C("spark_hot"), UiKit.C("spark_warm"), UiKit.C("spark_cool") };
            // 난수는 정본 순서(불티 → 흑피)로 먼저 **표만** 뽑고, 칸은 정본 SVG 순서(흑피가 뒤 · 불티가 앞)로 세운다.
            System.Func<double, double, double> rand = SparkRand();
            sparkSpecs = AutoForgeFxSpec.BuildSparks(rand);
            scaleSpecs = AutoForgeFxSpec.BuildScales(rand);
            DrawScales(fx, u);
            sparks = new Image[sparkSpecs.Length];
            for (int k = 0; k < sparkSpecs.Length; k++)
            {
                AutoForgeFxSpec.SparkSpec q = sparkSpecs[k];
                RectTransform rt = UiKit.Box(fx, "af-spark-" + k);
                Image img = rt.gameObject.AddComponent<Image>();
                img.raycastTarget = false;
                img.sprite = sp;
                img.type = Image.Type.Simple;
                Color c = tints[Mathf.Clamp(q.Tint, 0, tints.Length - 1)];
                c.a = 0f;                                   // 정지 상태는 투명(정본 `opacity: 0`)
                img.color = c;
                float scale = (float)q.Len / len;           // 기준 쐐기를 개체 길이로
                float bw = (len + pad * 2f) * scale * u, bh = (tail * 2f + pad * 2f) * scale * u;
                float cx = (float)AutoForgeFxSpec.HitCenterX(q.Strike), cy = (float)AutoForgeFxSpec.HitCenterY(q.Strike);
                UiKit.Place(rt, (cx - pad * scale) * u, (cy - (tail + pad) * scale) * u, bw, bh);
                // 피벗 = 꼬리 끝(= 타격점) · 세로 가운데 → 정본 `0% 50%`
                rt.pivot = new Vector2(pad * scale * u / Mathf.Max(1e-4f, bw), 0.5f);
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x + rt.pivot.x * bw, rt.anchoredPosition.y - 0.5f * bh);
                sparks[k] = img;
            }
        }

        /// <summary>
        /// 흑피 10개(2·3·5) — 정본 `.af-scale`. 불티와 **같은 궤적 문법**(회전 프레임 u·v)을 쓰되 어둡고 무겁고 느리다.
        /// 🚨 후광을 주지 않는다(정본: 어두운 조각에 주황 후광을 씌우면 다시 불티가 된다) — 그래서 스크린 합성도 아니다.
        /// </summary>
        static void DrawScales(RectTransform fx, float u)
        {
            scales = new Image[scaleSpecs.Length];
            float hgt = (float)AutoForgeFxSpec.ScaleHeight, rad = (float)AutoForgeFxSpec.ScaleRadius;
            Color[] tints = { UiKit.C("scale_far"), UiKit.C("scale_near") };
            for (int k = 0; k < scaleSpecs.Length; k++)
            {
                AutoForgeFxSpec.SparkSpec q = scaleSpecs[k];
                RectTransform rt = UiKit.Box(fx, "af-scale-" + k);
                Image img = rt.gameObject.AddComponent<Image>();
                img.raycastTarget = false;
                img.sprite = UiShapes.Rounded;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = UiShapes.RoundedMultiplier(rad * u);
                Color c = tints[Mathf.Clamp(q.Tint, 0, tints.Length - 1)];
                c.a = 0f;
                img.color = c;
                float cx = (float)AutoForgeFxSpec.HitCenterX(q.Strike), cy = (float)AutoForgeFxSpec.HitCenterY(q.Strike);
                float bw = (float)q.Len * u, bh = hgt * u;
                UiKit.Place(rt, cx * u, (cy - hgt * 0.5f) * u, bw, bh);
                // 정본 `transform-origin: 0% 50%` — 꼬리(타격점)가 축이다
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, rt.anchoredPosition.y - 0.5f * bh);
                scales[k] = img;
            }
        }

        /// <summary>불티 난수 — 정본은 `Math.random` 이라 매번 다르지만 클론은 씨앗을 고정해 테스트가 같은 묶음을 본다(결정 244).</summary>
        static System.Func<double, double, double> SparkRand()
        {
            Forge.Core.Data.Rng rng = Forge.Core.Data.Rng.Mulberry((uint)Mathf.RoundToInt(UiKit.L("spark_seed")));
            return delegate(double a, double b) { return rng.Rand(a, b); };
        }

        static void DrawStars(RectTransform fx, float u)
        {
            int n = Mathf.RoundToInt(UiKit.L("star_n"));
            Vector2[] right = new Vector2[n];
            Vector2[] left = new Vector2[n];
            for (int k = 0; k < n; k++)
            {
                float x = UiKit.L("star_x" + k), y = UiKit.L("star_y" + k);
                right[k] = new Vector2(x, y);
                left[k] = new Vector2(-x, y);
            }
            Color[] stops = { UiKit.C("star_in"), UiKit.C("star_mid"), UiKit.C("star_out") };
            float[] offs = { 0f, UiKit.L("star_off1"), 1f };
            // 정본 `x1 0 → x2 1`: 왼쪽 조각은 왼(바깥) → 오른(안쪽)이 흰색, 오른쪽 조각은 그 반대.
            Sprite spL = CraftFxPoly.Bake("af-star-l", left, new Color[] { stops[2], stops[1], stops[0] }, new float[] { 0f, 1f - offs[1], 1f }, Vector2.zero, new Vector2(1f, 0f));
            Sprite spR = CraftFxPoly.Bake("af-star-r", right, stops, offs, Vector2.zero, new Vector2(1f, 0f));
            for (int i = 0; i < starsL.Length; i++)
            {
                starsL[i] = StarPiece(fx, "af-star-l-" + i, left, spL, u, i, true);
                starsR[i] = StarPiece(fx, "af-star-r-" + i, right, spR, u, i, false);
            }
        }

        /// <summary>섬광 조각 하나 — 피벗을 «머리 쪽 끝» 에 두어 커질수록 바깥으로만 뻗게 한다.</summary>
        static Image StarPiece(RectTransform fx, string name, Vector2[] pts, Sprite sp, float u, int i, bool leftSide)
        {
            RectTransform rt = UiKit.Box(fx, name);
            Image img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.sprite = sp;
            img.type = Image.Type.Simple;
            Color c = Color.white; c.a = 0f;
            img.color = c;
            img.material = CraftFxPoly.Screen();   // 정본 `.af-star { mix-blend-mode: screen }`
            UnityEngine.Rect b = CraftFxPoly.Bounds(pts);
            float cx = (float)AutoForgeFxSpec.HitCenterX(i), cy = (float)AutoForgeFxSpec.HitCenterY(i);
            UiKit.Place(rt, (cx + b.xMin) * u, (cy + b.yMin) * u, b.width * u, b.height * u);
            // 축(피벗) = 머리 쪽 끝 · 세로는 정본 55%
            rt.pivot = new Vector2(leftSide ? 1f : 0f, 1f - 0.55f);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x + (leftSide ? b.width * u : 0f), rt.anchoredPosition.y - 0.55f * b.height * u);
            return img;
        }

        /// <summary>
        /// 타격 순간의 타원 겹(접지 그림자 · 순백 코어) 셋 — 링과 같은 «닿는 자리» 계보이고 접점보다 `<키>_dy` 만큼 아래에 앉는다.
        /// 정지 상태에서는 투명하고(정본 `opacity: 0`), 제 창에서만 <see cref="AnvilFx"/> 가 배율·불투명도를 바른다.
        /// </summary>
        static void DrawImpactEllipse(RectTransform fx, float u, Image[] into, string name, string rxKey, string ryKey, string dyKey, string colorKey, bool screen = false)
        {
            float rx = UiKit.L(rxKey), ry = UiKit.L(ryKey), dy = UiKit.L(dyKey);
            Sprite sp = CraftFxPoly.BakeEllipse(name, rx, ry, new Color[] { Color.white }, new float[] { 0f });
            Color tint = UiKit.C(colorKey);
            for (int i = 0; i < into.Length; i++)
            {
                RectTransform rt = UiKit.Box(fx, name + "-" + i);
                Image img = rt.gameObject.AddComponent<Image>();
                img.raycastTarget = false;
                img.sprite = sp;
                img.type = Image.Type.Simple;
                Color c = tint; c.a = 0f;
                img.color = c;
                if (screen) img.material = CraftFxPoly.Screen();
                float cx = (float)AutoForgeFxSpec.HitCenterX(i), cy = (float)AutoForgeFxSpec.HitCenterY(i) + dy;
                UiKit.Place(rt, (cx - rx) * u, (cy - ry) * u, rx * 2f * u, ry * 2f * u);
                into[i] = img;
            }
        }

        /// <summary>망치 겹 하나 — 로컬 좌표(타격면 중심이 원점)라 칸 가운데(피벗)를 원점으로 삼아 얹는다.</summary>
        static void HammerLayer(RectTransform art, string name, Vector2[] pts, Color[] stops, float[] offsets, Vector2 gradFrom, Vector2 gradTo, Color tint, float u)
        {
            RectTransform rt = UiKit.Box(art, name);
            Image img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.sprite = CraftFxPoly.Bake(name, pts, stops, offsets, gradFrom, gradTo);
            img.type = Image.Type.Simple;
            img.color = tint;
            UnityEngine.Rect b = CraftFxPoly.Bounds(pts);
            // 원점(= 칸의 피벗)에서 로컬 좌표만큼 떨어진 자리. CSS 는 아래가 +y 다.
            UiKit.Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), new Vector2(b.xMin * u, -b.yMin * u), b.width * u, b.height * u);
        }

        /// <summary>카탈로그의 로컬 좌표(`<키>_x0`·`_y0` … · 개수는 `<키>_n`).</summary>
        static Vector2[] LocalPoly(string key)
        {
            int n = Mathf.RoundToInt(UiKit.L(key + "_n"));
            Vector2[] pts = new Vector2[n];
            for (int i = 0; i < n; i++) pts[i] = new Vector2(UiKit.L(key + "_x" + i), UiKit.L(key + "_y" + i));
            return pts;
        }

        /// <summary>겹 하나 — 구운 폴리곤 스프라이트를 제 바깥 사각 자리에 얹는다(색·불투명도는 <see cref="Image.color"/> · 정본 원소 `opacity` 가 그 α 다).</summary>
        static Image BilletLayer(string name, Vector2[] pts, Color[] stops, float[] offsets, Vector2 gradTo, Color tint, float u)
        {
            RectTransform rt = UiKit.Box(billetRt, name);
            Image img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.sprite = CraftFxPoly.Bake(name, pts, stops, offsets, Vector2.zero, gradTo);
            img.type = Image.Type.Simple;
            img.color = tint;
            UnityEngine.Rect b = CraftFxPoly.Bounds(pts);
            UiKit.Place(img.rectTransform, b.xMin * u, b.yMin * u, b.width * u, b.height * u);
            return img;
        }

        /// <summary>정본 SVG 원소 `opacity` — 굽힌 색의 α 와 곱해진다(정본도 그렇다).</summary>
        public static void SetOpacity(Graphic g, float a)
        {
            if (g == null) return;
            Color c = g.color;
            c.a = Mathf.Clamp01(a);
            g.color = c;
        }

        /// <summary>카탈로그의 viewBox 좌표(`<키>_x0`·`_y0` …) — 정본 SVG 단위 그대로(132×86).</summary>
        static Vector2[] VbPoly(string key, int n)
        {
            Vector2[] pts = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                pts[i] = new Vector2(UiKit.L(key + "_x" + i), UiKit.L(key + "_y" + i));
            }
            return pts;
        }

        static Color Alpha(Color c, float a)
        {
            c.a *= a;
            return c;
        }

        /// <summary>
        /// 두들기기 반동(`anvilbump`)의 축을 정본과 같은 자리에 둔다 — `transform-box: view-box; transform-origin: 50% 92%`(받침 접지면).
        /// 유니티의 `localScale` 은 **피벗**을 축으로 도니 피벗을 그 점으로 옮기고, 앵커가 점(0,1)이라 배치가 밀리지 않게 위치를 되맞춘다
        /// (자식들은 부모 «사각» 을 기준 삼으므로 피벗을 옮겨도 안 움직인다). 축이 틀리면 눌림이 위로 자라는 그림이 된다.
        /// </summary>
        static void SetFxOrigin(RectTransform rt, float ox, float oy, float u, float vbW, float vbH, float w, float h)
        {
            if (w <= 0f || h <= 0f) return;
            float px = ox + vbW * u * (float)AnvilFxSpec.BumpOriginFrac[0];
            float py = oy + vbH * u * (float)AnvilFxSpec.BumpOriginFrac[1];   // 위에서부터
            rt.pivot = new Vector2(px / w, 1f - py / h);
            rt.anchoredPosition = new Vector2(px, -py);
        }

        // T98 2회차에 `Outlined`(둥근 사각 + 살짝 큰 검은 면)를 걷었다 — 모루는 이제 정본 SVG 폴리곤(`AnvilArt`)이라
        // 획도 `CraftFxPoly.Inflate` 로 «면을 부풀린 사본» 이다. 옛 카탈로그 키(`anvil_base_*`·`anvil_top`·`anvil_line`
        // ·`anvil_bevel_*`·`anvil_stroke`·`anvil_horn_r`)는 이제 아무도 안 읽지만 `catalog.json` 은 남의 lock 이라 안 지운다.

    }
}
