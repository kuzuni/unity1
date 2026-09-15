using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.Forging;
using Forge.Core.Meta;
using Forge.Core.Mounts;
using Forge.Core.Pets;
using Forge.Game.Gallery;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T65 플레이어 정보 팝업의 표(<c>Assets/Forge/Resources/PlayerInfoUi.json</c> — 정본 CSS `.pinfo-*`·`.equip-cell`·`.sk-orb` 실측 색·배치·ui.js 문구).
    /// T62 lock 이 <c>catalog.json</c> 을 쥐고 있어 T65 몫은 이 파일이 든다(T20 <see cref="PetSkillStyle"/> 과 같은 꼴 · T33 이 합칠 수 있다). 코드에 숫자·색·문구를 박지 않는다(§1).
    /// </summary>
    public static class PlayerInfoStyle
    {
        public const string ResourcePath = "PlayerInfoUi";
        static JsonObject root, colors, layout, text;
        static readonly Dictionary<string, Color> colorCache = new Dictionary<string, Color>();

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T65)");
            root = MiniJson.ParseObject(ta.text);
            colors = J.Obj(root["colors"]);
            layout = J.Obj(root["layout"]);
            text = J.Obj(root["text"]);
        }

        public static void Reset() { root = null; colorCache.Clear(); }

        public static Color C(string key)
        {
            Load();
            Color c;
            if (colorCache.TryGetValue(key, out c)) return c;
            string hex = J.Str(colors[key]);
            if (hex == null) throw new KeyNotFoundException("PlayerInfoUi.json 에 색 «" + key + "» 이 없다");
            if (!ColorUtility.TryParseHtmlString(hex, out c)) throw new FormatException("색 «" + key + "» 의 값 «" + hex + "» 을 못 읽는다");
            colorCache[key] = c;
            return c;
        }

        /// <summary>배치 값 원문(접미 _w · _h · _rem · _f · _n).</summary>
        public static float L(string key)
        {
            Load();
            object v = layout[key];
            if (!J.IsNum(v)) throw new KeyNotFoundException("PlayerInfoUi.json 에 배치 값 «" + key + "» 이 없다");
            return (float)J.Num(v);
        }

        /// <summary>키 접미에 맞춰 기준 px 로(_w 앱 폭 · _h 앱 높이 · _rem catalog rem).</summary>
        public static float Px(string key)
        {
            float v = L(key);
            if (key.EndsWith("_w")) return v * UiKit.RefW;
            if (key.EndsWith("_h")) return v * UiKit.RefH;
            if (key.EndsWith("_rem")) return v * PopupKit.Rem;
            return v;
        }

        public static string T(string key)
        {
            Load();
            string s = J.Str(text[key]);
            if (s == null) throw new KeyNotFoundException("PlayerInfoUi.json 에 문구 «" + key + "» 이 없다");
            return s;
        }
        public static string T(string key, params object[] args) { return string.Format(T(key), args); }
    }

    /// <summary>
    /// 플레이어 정보 팝업(ROUTINE T22 · T65 · 원작 ui.js openPlayerInfo/renderPlayerInfo · shot-043313): 머리줄(아바타 · 이름 [무소속] · 성별 · 서버 1 · 전투력 | Lv.N 대장간 ⭐ · 총 피해 · 총 체력) ·
    /// 미니 전투씬 자리(원작 Scene3D.previewStart — T8/T54 가 꽂는다 · 그 전엔 정본 폴백 = 🛡️ + 스테이지 라벨 + 웨이브 핍) · 장비 8칸 = **장비 시트와 같은 조각**(정본 `equipCellHTML` · ForgeUi 타일·Lv·★) + 와이드 파란 탈것 칸(`pinfo-mount-wide`) ·
    /// 출전 줄 = 스킬·펫·탈것 오브(`sk-cell`+`sk-orb`+`sk-lv` · 누르면 각 상세) · 보유 옵션 목록.
    /// 스탯은 T15 `Forge.heroStats` 자리라 <see cref="MetaHost"/> 의 훅으로 받는다 — 없으면 0. 수치·색·문구는 <see cref="PlayerInfoStyle"/>(T65) 와 catalog(T22).
    /// </summary>
    public static class PlayerInfoPopup
    {
        public const string Name = "player-info";

        /// <summary>T15/T24 가 꽂는다: 총 피해 · 총 체력 · 승천 별 합 · 보유 옵션(문장 목록).</summary>
        public static System.Func<Big> HeroAtk, HeroHp;
        public static System.Func<double> TotalStars;
        public static System.Func<List<string>> SubLines;
        /// <summary>T8 이 꽂는다 — 프리뷰 상자에 미니 씬을 세운다(true 를 돌려주면 폴백 글자를 안 그린다).</summary>
        public static System.Func<RectTransform, bool> PreviewStart;
        public static System.Action PreviewStop;
        /// <summary>폴백 핍의 웨이브(원작 `Combat.wave`/`Combat.totalWaves()` · 던전 중이면 핍 없음). T8 전투 씬이 있으면 그것을 읽고, 없으면 (0,0,false).</summary>
        public static System.Func<WaveInfo> Waves = DefaultWaves;

        public struct WaveInfo { public int Wave, Total; public bool Dungeon; }

        static WaveInfo DefaultWaves()
        {
            var w = new WaveInfo();
            Forge.Game.Battle.BattleScene bs = Forge.Game.Battle.BattleScene.Instance;
            if (bs == null || bs.Battle == null) return w;
            w.Wave = bs.Battle.Wave; w.Total = bs.Battle.TotalWaves();
            w.Dungeon = bs.Battle.Context != null && bs.Battle.Context.Dungeon != null;
            return w;
        }

        public static void Open(MetaHost h)
        {
            h.Popups.Show(Name, null, PopupZUi.AboveTabBar(Name));   // T346 4회차 — 정본 3782 `#player-info-modal { z-index: 40 }` > 탭바 30 · 층은 표 PopupZUi.json 이 정한다
            Render(h);
        }

        public static void Close(MetaHost h)
        {
            h.Popups.Hide(Name);
            if (PreviewStop != null) PreviewStop();
        }

        public static void Render(MetaHost h)
        {
            Popup p = h.Popups.Find(Name);
            if (p == null) return;
            RectTransform root = PopupLayer.Clear(p);
            float rem = PopupKit.Rem, w = UiKit.RefW, H = UiKit.RefH;
            float cardW = UiKit.L("pinfo_w") * w, cardH = UiKit.L("pinfo_h") * H;
            RectTransform card = PopupKit.Card(root, "card", cardW, cardH, "pp_paper", rem);
            float pad = UiKit.H("card_pad");
            float inner = cardW - PopupKit.Line3 * 2f;
            float lineH = PopupKit.FontSize(TextKind.Sub) * 1.25f;

            // ---- 머리줄(.pinfo-header) ----
            float y = pad;
            float av = UiKit.H("pinfo_avatar");
            float hx = pad + w * 0.0224f;
            RectTransform avatar = PopupKit.Avatar(card, "avatar", av, h.AvatarEmoji, rem * 0.4f);
            UiKit.Place(avatar, hx, y, av, av);
            float tx = hx + av + rem * 0.5f;
            float leftW = inner * 0.55f - tx;
            TextMeshProUGUI name = UiKit.Text(card, "name", TextKind.Sub, h.Nickname + " [무소속]", "pp_ink", TextAlignmentOptions.Left);
            name.fontStyle = FontStyles.Bold;
            UiKit.Place(name.rectTransform, tx, y, leftW, lineH);
            // T131 — 정본 ui.js 5195 `<span class="clan">${IconGen.img(S.gender === '♀' ? 'gender_f' : 'gender_m')} · 서버 1</span>`:
            // 성별은 글자 ♂/♀ 가 아니라 아이콘(CSS 3158 `.clan .ico` 1.05em · 오른쪽 여백 .05em · PersonIconsUi.json).
            float gEm = PersonIcons.Px("pinfo_gender_em", PopupKit.FontSize(TextKind.Sub)), gMr = PersonIcons.Px("pinfo_gender_mr_em", PopupKit.FontSize(TextKind.Sub));
            Image clanIco = UiKit.Icon(card, "clan-ico", Chat.GenderIcon(h.Gender));
            UiKit.Place(clanIco.rectTransform, tx, y + lineH + (lineH - gEm) * 0.5f, gEm, gEm);
            TextMeshProUGUI clan = UiKit.Text(card, "clan", TextKind.Sub, " · 서버 1", "pp_muted", TextAlignmentOptions.Left);
            UiKit.Place(clan.rectTransform, tx + gEm + gMr, y + lineH, leftW - gEm - gMr, lineH);
            // T89 — «⚔» 두부 → 정본 표의 `tm_sword` 아이콘 + 수.
            RectTransform cp = UiKit.IconTextRow(card, "cp", TextKind.Sub, "⚔ " + PopupKit.Fmt(h.MyCp), "pp_ink", TextAlignmentOptions.Left);
            foreach (TextMeshProUGUI piece in UiKit.RowTexts(cp))
            {
                piece.fontStyle = FontStyles.Bold;
                UiKit.OutlinePx(piece, "pp_line", KeylineUi.Px("pinfo_cp"));   // 정본 #player-info-modal .pinfo-id-text .cp { 2px var(--pp-line) }
            }
            UiKit.Place(cp, tx, y + lineH * 2f, leftW, lineH);

            double stars = TotalStars != null ? TotalStars() : 0;
            Big atk = HeroAtk != null ? HeroAtk() : Big.Zero;
            Big hp = HeroHp != null ? HeroHp() : Big.Zero;
            float rx = inner * 0.55f, rw = inner - rx - pad - w * 0.0244f;
            TextMeshProUGUI r1 = UiKit.Text(card, "forge-lv", TextKind.Sub, "Lv. " + h.S.ForgeLevel + " 대장간" + (stars > 0 ? " ★" + PopupKit.Fmt(stars) : string.Empty), "pp_ink", TextAlignmentOptions.Right);
            UiKit.Place(r1.rectTransform, rx, y, rw, lineH);
            TextMeshProUGUI r2 = UiKit.Text(card, "atk", TextKind.Sub, PopupKit.Fmt(atk) + " 총 피해", "pp_ink", TextAlignmentOptions.Right);
            UiKit.Place(r2.rectTransform, rx, y + lineH, rw, lineH);
            TextMeshProUGUI r3 = UiKit.Text(card, "hp", TextKind.Sub, PopupKit.Fmt(hp) + " 총 체력", "pp_ink", TextAlignmentOptions.Right);
            UiKit.Place(r3.rectTransform, rx, y + lineH * 2f, rw, lineH);
            y += Mathf.Max(av, lineH * 3f) + PlayerInfoStyle.Px("preview_margin_rem");

            // ---- 미니 씬 프리뷰(.pinfo-preview) — 미니 씬이 못 서면 정본 폴백: 🛡️ + 스테이지 라벨 + 웨이브 핍 ----
            float pvH = Mathf.Max(UiKit.L("pinfo_preview_h") * H, PlayerInfoStyle.Px("preview_min_h_rem"));
            RectTransform preview = UiKit.Box(card, "preview");
            UiKit.Place(preview, pad, y, inner - pad * 2f, pvH);
            bool scene = PreviewStart != null && PreviewStart(preview);
            if (!scene) Fallback(preview, h, inner - pad * 2f, pvH);
            y += pvH + PlayerInfoStyle.Px("preview_margin_rem");

            // ---- 장비 격자(.equip-grid.pinfo-gear): 5열 · 8칸 + 와이드 탈것 2칸 ----
            string[] slots = SaveIo.Data != null && SaveIo.Data.Defs != null && SaveIo.Data.Defs.Slots != null ? SaveIo.Data.Defs.Slots : new string[0];
            int cols = (int)PlayerInfoStyle.L("gear_cols_n");
            int span = (int)PlayerInfoStyle.L("mount_span_n");
            float gx = PopupKit.Line3 + inner * PlayerInfoStyle.L("gear_pad_f");
            float gw = inner - (gx - PopupKit.Line3) * 2f;
            float gapX = gw * PlayerInfoStyle.L("gear_gap_x_f"), gapY = PlayerInfoStyle.Px("gear_gap_y_rem");
            float cell = (gw - gapX * (cols - 1)) / cols;
            ForgeHost fh = ForgeHost.Instance;
            int k = 0;
            for (int i = 0; i < slots.Length; i++, k++)
            {
                RectTransform c = EquipCell(card, fh, slots[i], cell);
                UiKit.Place(c, gx + (k % cols) * (cell + gapX), y + (k / cols) * (cell + gapY), cell, cell);
            }
            // 원본(043313): 장비 2행 우측 와이드 파란 탈것 카드(grid-column: span 2)
            if ((k % cols) + span > cols) k += cols - (k % cols);
            float wideW = cell * span + gapX * (span - 1);
            RectTransform mc = MountWide(card, h, wideW, cell);
            UiKit.Place(mc, gx + (k % cols) * (cell + gapX), y + (k / cols) * (cell + gapY), wideW, cell);
            k += span;
            int rows = (k + cols - 1) / cols;
            y += rows * cell + (rows - 1) * gapY + PlayerInfoStyle.Px("loadout_top_rem");

            // ---- 출전 줄(.pinfo-loadout-row): 스킬 오브 · 펫 오브 · 탈것 오브 — 없으면 «출전 중인 펫 없음» ----
            float orb = PlayerInfoStyle.Px("orb_w"), lgap = PlayerInfoStyle.Px("loadout_gap_w");
            RectTransform row = UiKit.Box(card, "loadout");
            UiKit.Place(row, gx, y, gw, orb + PlayerInfoStyle.Px("sk_lv_drop_rem"));
            int n = Loadout(row, h, orb, lgap);
            if (n == 0)
            {
                TextMeshProUGUI lo = UiKit.Text(row, "none", TextKind.Sub, PlayerInfoStyle.T("no_loadout"), "pp_muted", TextAlignmentOptions.Left);
                UiKit.Place(lo.rectTransform, 0f, 0f, gw, lineH);
            }
            y += orb + PlayerInfoStyle.Px("subs_top_rem");

            // ---- 보유 옵션(.pinfo-subs-list) ----
            List<string> subs = SubLines != null ? SubLines() : null;
            RectTransform subsBox = UiKit.Box(card, "subs");
            UiKit.Place(subsBox, gx, y, gw, Mathf.Max(lineH, cardH - y - pad - rem * 1.5f));
            RectTransform subsList = PopupKit.ScrollList(subsBox, "list", rem * 0.1f, 0f, 0f, TextAnchor.UpperLeft);
            if (subs == null || subs.Count == 0) PopupKit.Label(subsList, "none", TextKind.Sub, PlayerInfoStyle.T("no_subs"), "pp_muted", TextAlignmentOptions.Left);
            else foreach (string s in subs) PopupKit.Label(subsList, "sub", TextKind.Sub, s, "pp_ink", TextAlignmentOptions.Left);

            PopupKit.XButton(card, () => Close(h));
        }

        /// <summary>정본 폴백 `.pinfo-preview`: 마른 흙 두 톤(55%) · 검정 테 · 🛡️ · 스테이지 라벨 · 웨이브 핍(던전 중이면 없음).</summary>
        static void Fallback(RectTransform preview, MetaHost h, float w, float hgt)
        {
            float rem = PopupKit.Rem;
            float radius = PlayerInfoStyle.Px("preview_radius_rem");
            UiKit.Rounded(preview, "line", "pp_line", radius);
            RectTransform faceRt = UiKit.Box(preview, "face");
            PopupKit.Inset(faceRt, PopupKit.Line);
            // 정본 `.pinfo-preview { background: linear-gradient(180deg, #9d8256 55%, #6f5334 55%) }` — 두 판이 아니라 겹 한 장(정지점 둘이 같은 55% 라 경계가 날카롭다 · SurfaceUi.json `pinfo_preview` · T178 3회차).
            Image ground = UiKit.Rounded(faceRt, "ground", "pp_paper", Mathf.Max(1f, radius - PopupKit.Line));
            UiKit.Fill(ground.rectTransform);
            ground.color = PlayerInfoStyle.C("preview_top");
            SurfaceArt.FillMasked(ground, "preview-grad", "pinfo_preview", w, hgt);
            float px = PlayerInfoStyle.Px("preview_pad_x_rem"), gap = PlayerInfoStyle.Px("preview_gap_rem");
            float lh = PopupKit.FontSize(TextKind.Sub) * 1.3f;
            float cy = (hgt - lh) * 0.5f;
            float x = px;
            TextMeshProUGUI sh = UiKit.Text(preview, "shield", TextKind.Sub, PlayerInfoStyle.T("shield"), "stage_ink", TextAlignmentOptions.Left);
            UiKit.Place(sh.rectTransform, x, cy, lh, lh);
            x += lh + gap;
            string label = h.S.StageName(SaveIo.Defs);
            TextMeshProUGUI st = UiKit.Text(preview, "stage", TextKind.Sub, label, "stage_ink", TextAlignmentOptions.Left);
            st.fontStyle = FontStyles.Bold;
            float stW = Mathf.Min(w - x - px, PetSkillKit.TextWidth(TextKind.Sub, label) + rem * 0.2f);
            UiKit.Place(st.rectTransform, x, cy, stW, lh);
            x += stW + gap;
            WaveInfo wi = Waves != null ? Waves() : new WaveInfo();
            if (!wi.Dungeon && wi.Total > 0)
            {
                float pip = PlayerInfoStyle.Px("pip_rem");
                for (int i = 1; i <= wi.Total && x + pip <= w - px; i++)
                {
                    Image d = UiKit.Circle(preview, "pip-" + i, "stage_ink");
                    d.color = i < wi.Wave ? PlayerInfoStyle.C("pip_done") : i == wi.Wave ? PlayerInfoStyle.C("pip_now") : PlayerInfoStyle.C("pip");
                    d.raycastTarget = false;
                    UiKit.Place(d.rectTransform, x, (hgt - pip) * 0.5f, pip, pip);
                    x += pip + gap;
                }
            }
        }

        /// <summary>정본 `equipCellHTML(slot)` — 장비 시트(T19 `ForgeSheet.EquipCell`)와 같은 ForgeUi 조각(시대색 타일 · 아이콘 · Lv · ★ · 누르면 세부정보). 대장간 호스트가 없으면 빈 칸.</summary>
        static RectTransform EquipCell(Transform parent, ForgeHost fh, string slot, float size)
        {
            // 대장간 호스트는 부팅 코루틴이 Data 를 늦게 채운다(CI 런 98: Instance 는 있고 Data 가 null → Defs NRE) — 표는 SaveIo 것을 쓰고 장비만 호스트에서
            GameDefs d = SaveIo.Data != null ? SaveIo.Data.Defs : null;
            bool ready = fh != null && fh.Data != null && fh.Gear != null;
            if (ready && d == null) d = fh.Defs;
            ForgeItem it = ready ? fh.Gear.Get(slot) : null;
            RectTransform rt = UiKit.Box(parent, "slot-" + slot);
            float radius = size * PlayerInfoStyle.L("cell_radius_f");
            if (it == null)
            {
                Color ea = PlayerInfoStyle.C("empty_age");
                ForgeUi.Tile(rt, "frame", ForgeUi.CellFace(ea), ForgeUi.CellLine(ea), radius, PopupKit.Line3);
                Image ico = PopupKit.IconOr(rt, "img", ForgeUi.SlotIconKey(slot));
                // T342 6회차 — 정본 862 `.equip-cell.empty .cell-img.dim { filter: grayscale(1) brightness(1.75) opacity(.52) }`: 플레이어 정보도 같은
                //             `equipCellHTML`(ui.js 3096~3099) 이라 같은 자리다. 틴트 알파만으로는 회색·밝기를 못 낸다 — 표 FilterUi 로 굽고 .52 는 틴트 알파(ForgeSheet 와 같은 길).
                UiFilter.ApplyColor(ico, "equip_cell_empty");
                ico.raycastTarget = false;
                float ek = size * PlayerInfoStyle.L("empty_ink_f");
                UiKit.Anchor(ico.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, size * PlayerInfoStyle.L("cell_ink_lift_f")), ek, ek);
                string kr = d != null && d.SlotKr != null ? d.SlotKr.Get(slot, slot) : slot;
                TextMeshProUGUI nm = UiKit.Text(rt, "slot-name", TextKind.Sub, kr, "pp_muted");
                nm.fontStyle = FontStyles.Bold;
                UiKit.TextShadow(nm, "slot_name");            // T333 8회차 — 정본 8030 `.equip-cell .slot-name` 두 겹 중 첫째(아래 1px 드롭 · 표 slot_name)
                UiKit.Anchor(nm.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, size * PlayerInfoStyle.L("empty_name_y_f")), size, nm.fontSize * 1.2f);
                return rt;
            }
            if (d == null) return rt;
            Color ac = ForgeUi.AgeColor(d, it.Age);
            Image f = ForgeUi.Tile(rt, "frame", ForgeUi.CellFace(ac), ForgeUi.CellLine(ac), radius, PopupKit.Line3);
            AgePattern.Attach(rt, it.Age, cell: true, mask: false, siblingIndex: 1);   // T124 3회차 ⓑ — 정본 equipCellHTML(ui.js 3102) `.equip-cell[data-age]` 의 시대 무늬 층(.55 · 틀 위·아이콘 뒤) · 장비 시트 칸(ForgeSheet.EquipCell)과 같은 한 줄
            Image img = PopupKit.IconOr(rt, "img", ForgeUi.ItemIconKey(d, it));
            img.raycastTarget = false;
            float kk = size * PlayerInfoStyle.L("cell_ink_f");
            UiKit.Anchor(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, size * PlayerInfoStyle.L("cell_ink_lift_f")), kk, kk);
            ForgeUi.ApplyThumb(rt, ItemFaces.Get(d, it), size);   // T122 3회차 — 장비 시트 칸과 같은 정본 equipCellHTML(ui.js 3103) 의 itemImgHTML: 3D 썸네일 · 장신구는 실루엣
            ForgeUi.LvBadge(rt, it.Level, size);
            ForgeUi.StarBadge(rt, it.Stars, size);
            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = f;
            string s = slot;
            b.onClick.AddListener(() => { if (ForgeHost.Instance != null && ForgeHost.Instance.Data != null) GearDetailPopup.Open(ForgeHost.Instance, s); });   // 원작: 세부정보가 플레이어 정보 위에 겹쳐 뜬다
            return rt;
        }

        /// <summary>정본 `.equip-cell.egg-cell.pinfo-mount-wide` — 탑승 탈것 얼굴 + Lv + 여분 «+N» · 빈 상태는 실루엣 + «탈것» · 누르면 탈것 시트(`UI.openMounts`).</summary>
        static RectTransform MountWide(Transform parent, MetaHost h, float w, float hgt)
        {
            RectTransform rt = UiKit.Box(parent, "egg-cell");
            Image f = ForgeUi.Tile(rt, "frame", PlayerInfoStyle.C("mount_face"), PlayerInfoStyle.C("mount_line"), hgt * PlayerInfoStyle.L("cell_radius_f"), PopupKit.Line3);
            PetSkillHost ph = PetSkillHost.Instance;
            MountSystem ms = ph != null ? ph.Mounts : null;
            Mount am = ms != null ? ms.RiddenInst() : null;
            GameDefs d = SaveIo.Data != null ? SaveIo.Data.Defs : null;
            if (am != null && d != null)
            {
                float fs = w * PlayerInfoStyle.L("mount_face_f");
                RectTransform face = PetSkillKit.PetFace(rt, d, am.Name, fs, GalleryKind.Mounts);
                UiKit.Anchor(face, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, hgt * PlayerInfoStyle.L("mount_sil_lift_f")), fs, fs);
                ForgeUi.LvBadge(rt, am.Level, hgt);
                int extra = Math.Max(0, (ms.State != null && ms.State.ActiveMounts != null ? ms.State.ActiveMounts.Count : 0) - 1);
                if (extra > 0)
                {
                    TextMeshProUGUI cnt = UiKit.Text(rt, "cell-count", TextKind.Sub, PlayerInfoStyle.T("count", extra), "stage_ink", TextAlignmentOptions.Right);
                    cnt.fontStyle = FontStyles.Bold;
                    PopupKit.Ring(cnt, "pp_line", 0.2f);
                    UiKit.Anchor(cnt.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-PlayerInfoStyle.Px("mount_count_right_rem"), -PlayerInfoStyle.Px("mount_count_top_rem")), w * 0.5f, cnt.fontSize * 1.2f);
                }
            }
            else
            {
                // T342 6회차 — 정본 `ui.js` 5166 의 플레이어 정보 빈 탈것 칸은 `<div class="equip-cell egg-cell empty pinfo-mount-wide"><span class="slot-name">탈것</span></div>` 뿐이다:
                //             장비 시트(1535)와 달리 **말 실루엣(mount-sil)이 없다**(CSS 3212 `.pinfo-mount-wide` 도 span 2 뿐). 여기 그리던 실루엣은 원작에 없던 것이라 뺐다(§1 · 결정 630).
                TextMeshProUGUI nm = UiKit.Text(rt, "slot-name", TextKind.Sub, PlayerInfoStyle.T("mount_slot"), "stage_ink");
                nm.fontStyle = FontStyles.Bold;
                PopupKit.Ring(nm, "pp_line", 0.2f);
                // T333 8회차 — 알 칸은 정본 874 `.equip-cell.egg-cell .slot-name`(클래스 셋)이 8030(둘)을 특이도로 이긴다: 딱딱한 아래 1px(표 slot_name_egg) · 링 위에 겹친다
                UiKit.TextShadow(nm, "slot_name_egg");
                UiKit.Anchor(nm.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, hgt * PlayerInfoStyle.L("mount_name_y_f")), w, nm.fontSize * 1.2f);
            }
            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = f;
            b.onClick.AddListener(() => MountSheet.Open());
            return rt;
        }

        /// <summary>정본 출전 줄: 장착 스킬 오브(등급색 면 · `sk_<id>` 아이콘 · Lv 알약) → 출전 펫 오브(얼굴) → 장착 탈것 오브(얼굴). 반환 = 칸 수.</summary>
        static int Loadout(RectTransform row, MetaHost h, float orb, float gap)
        {
            PetSkillHost ph = PetSkillHost.Instance;
            SkillPetSheet sheet = SkillPetSheet.Instance;
            GameDefs d = SaveIo.Data != null ? SaveIo.Data.Defs : null;
            float x = 0f; int n = 0;
            List<object> skills = h.S.EquippedSkills;
            if (skills != null && d != null)
            {
                foreach (object o in skills)
                {
                    string id = J.Str(o);
                    SkillDef def = id != null ? d.Skill(id) : null;
                    if (def == null) continue;
                    int lv = ph != null && ph.Skills != null ? ph.Skills.Level(id) : 0;
                    RectTransform cellRt = OrbCell(row, "sk-cell-" + id, x, orb, PetSkillStyle.Rarity(d, def.Rarity), lv, () => { if (sheet != null && sheet.Skills != null) sheet.Skills.OpenSkillDetail(id); });
                    Image ico = PopupKit.IconOr(cellRt.Find("sk-orb"), "ico", "sk_" + id);
                    ico.raycastTarget = false;
                    float ik = orb * PlayerInfoStyle.L("sk_ico_f");
                    UiKit.Anchor(ico.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, ik, ik);
                    x += orb + gap; n++;
                }
            }
            PetState ps = ph != null && ph.Pets != null ? ph.Pets.State : null;
            if (ps != null && ps.ActivePets != null && d != null)
            {
                foreach (int i in ps.ActivePets)
                {
                    if (i < 0 || i >= ps.Pets.Count) continue;
                    Pet pet = ps.Pets[i];
                    int idx = i;
                    RectTransform cellRt = OrbCell(row, "sk-cell-pet-" + i, x, orb, PlayerInfoStyle.C("orb_default"), pet.Level, () => { if (sheet != null && sheet.Pets != null) sheet.Pets.OpenPetDetail(idx); });
                    float fk = orb * PlayerInfoStyle.L("sk_face_f");
                    RectTransform face = PetSkillKit.PetFace(cellRt.Find("sk-orb"), d, pet.Name, fk, GalleryKind.Pets);
                    UiKit.Anchor(face, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, fk, fk);
                    x += orb + gap; n++;
                }
            }
            MountState mst = ph != null && ph.Mounts != null ? ph.Mounts.State : null;
            if (mst != null && mst.ActiveMounts != null && d != null)
            {
                foreach (int i in mst.ActiveMounts)
                {
                    if (i < 0 || i >= mst.Mounts.Count) continue;
                    Mount m = mst.Mounts[i];
                    int idx = i;
                    RectTransform cellRt = OrbCell(row, "sk-cell-mount-" + i, x, orb, PlayerInfoStyle.C("orb_default"), m.Level, () => { if (sheet != null) MountUpgradePopup.Open(sheet, idx); });
                    float fk = orb * PlayerInfoStyle.L("sk_face_f");
                    RectTransform face = PetSkillKit.PetFace(cellRt.Find("sk-orb"), d, m.Name, fk, GalleryKind.Mounts);
                    UiKit.Anchor(face, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, fk, fk);
                    x += orb + gap; n++;
                }
            }
            return n;
        }

        /// <summary>`.sk-cell` + `.sk-orb`(등급색 납작 면 + 검정 키라인) + `.sk-lv`(오브 면 위 흰 글자 + 검정 링 · T129 3회차 · 결정 289).</summary>
        static RectTransform OrbCell(RectTransform row, string name, float x, float orb, Color fill, int level, UnityEngine.Events.UnityAction onClick)
        {
            Button b = UiKit.Button(row, name, onClick);
            RectTransform cell = b.GetComponent<RectTransform>();
            UiKit.Place(cell, x, 0f, orb, orb + PlayerInfoStyle.Px("sk_lv_drop_rem"));
            RectTransform orbRt = PetSkillKit.Orb(cell, "sk-orb", fill, PetSkillKit.Line3);
            UiKit.Place(orbRt, 0f, 0f, orb, orb);
            float lh = PlayerInfoStyle.Px("sk_lv_h_rem");
            string txt = PlayerInfoStyle.T("lv", level);
            RectTransform lv = UiKit.Box(cell, "sk-lv");
            TextMeshProUGUI t = PetSkillKit.Text(lv, "t", TextKind.Sub, txt, PlayerInfoStyle.C("sk_lv_ink"));
            // 폭은 **어림(`TextWidth` 은 라틴 한 자를 0.58em 로 셈한다)이 아니라 TMP 가 실제로 잰 값**으로 잡는다(2회차 · 어림은 «Lv.20» 을 1.18배 부풀린다).
            float lw = t.preferredWidth + PlayerInfoStyle.Px("sk_lv_pad_rem") * 2f;
            UiKit.Fill(t.rectTransform);
            // 3회차(결정 289) — **검정 알약이 아니라 오브 면 위 흰 글자 + 검정 링**이다.
            // 정본 `.sk-lv`(style.css 4045)는 알약이지만 그 알약은 `.6rem` 글자 기준이라 칸 간격(83.6px) 안에 든다(71.6px).
            // 클론은 글자 크기 하한(`TextSizeGateTests` · Sub 36px)을 지켜야 해 같은 알약이 106px 이 되어 **이웃 칸을 통째로 덮었다**(런 259·268 PNG 8배: 검은 띠 하나 · 마지막 자리 안 보임).
            // 정본이 같은 겹침을 스킬 화면에서 고친 길이 그대로 있고(`sk-orb-lv-pill` · `#panel-skills .sk-grid .sk-lv` · style.css 4066: 알약을 걷고 2px 검정 테)
            // 원작 샷 `shot-043313` 의 출전 줄도 **그 꼴**이다 — 그 길로 간다. 불투명한 판이 사라져 이웃을 안 가린다.
            // 폭은 T109 의 정본 폭표(`KeylineUi.json px.sk_lv` = 정본 2px × `css_px`)를 그대로 읽는다 — 같은 시각 워커 H 가 `SkillBar` 의 같은 병을 그 키로 고쳤다(T109 8회차).
            UiKit.OutlinePx(t, "pp_line", KeylineUi.Px("sk_lv"));
            // 세로 자리 — 정본 실측 «잉크 세로중심이 오브 상단에서 72.9%»(style.css 4050 머리말의 픽셀 census · 원작 샷 `shot-043313` 의 출전 줄도 같은 자리).
            // 칸 바닥 기준이라 오브 아래 여백(`sk_lv_drop_rem`)을 더한다.
            float lvh = Mathf.Max(lh, t.preferredHeight);
            float lvy = PlayerInfoStyle.Px("sk_lv_drop_rem") + orb * (1f - PlayerInfoStyle.L("sk_lv_center_f")) - lvh * 0.5f;
            UiKit.Anchor(lv, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, lvy), lw, lvh);
            return cell;
        }
    }
}
