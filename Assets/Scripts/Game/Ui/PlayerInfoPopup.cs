using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Data;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 플레이어 정보 팝업(ROUTINE T22 · 원작 ui.js openPlayerInfo/renderPlayerInfo · shot-043313): 머리줄(아바타 · 이름 [무소속] · 성별 · 서버 1 · 전투력 | Lv.N 대장간 ⭐ · 총 피해 · 총 체력) ·
    /// 미니 전투씬 자리(원작 Scene3D.previewStart — T8 이 꽂는다 · 그 전엔 스테이지 라벨 폴백) · 장비 8칸 격자 + 탈것 칸(T15·T11 이 채운다) · 출전 스킬/펫 줄(T17·T16) · 보유 옵션 목록.
    /// 스탯은 T15 `Forge.heroStats` 자리라 <see cref="MetaHost"/> 의 훅으로 받는다 — 없으면 0.
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

        public static void Open(MetaHost h)
        {
            h.Popups.Show(Name);
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

            // ---- 머리줄 ----
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
            TextMeshProUGUI clan = UiKit.Text(card, "clan", TextKind.Sub, h.Gender + " · 서버 1", "pp_muted", TextAlignmentOptions.Left);
            UiKit.Place(clan.rectTransform, tx, y + lineH, leftW, lineH);
            TextMeshProUGUI cp = UiKit.Text(card, "cp", TextKind.Sub, "⚔ " + PopupKit.Fmt(h.MyCp), "pp_ink", TextAlignmentOptions.Left);
            cp.fontStyle = FontStyles.Bold;
            UiKit.Place(cp.rectTransform, tx, y + lineH * 2f, leftW, lineH);

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
            y += Mathf.Max(av, lineH * 3f) + rem * 0.5f;

            // ---- 미니 씬 프리뷰 자리 ----
            float pvH = UiKit.L("pinfo_preview_h") * H;
            RectTransform preview = UiKit.Box(card, "preview");
            UiKit.Place(preview, pad, y, inner - pad * 2f, pvH);
            PopupKit.Outlined(preview, "face", "pp_panel", rem * 0.5f, PopupKit.Line);
            bool scene = PreviewStart != null && PreviewStart(preview);
            if (!scene)
            {
                TextMeshProUGUI st = UiKit.Text(preview, "stage", TextKind.Sub, "🛡 " + h.S.StageName(SaveIo.Defs), "pp_ink");
                st.fontStyle = FontStyles.Bold;
            }
            y += pvH + rem * 0.5f;

            // ---- 장비 8칸 + 탈것 ----
            string[] slots = SaveIo.Data != null && SaveIo.Data.Defs != null && SaveIo.Data.Defs.Slots != null ? SaveIo.Data.Defs.Slots : new string[0];
            int cols = 4;
            float gx = pad + inner * 0.0575f;
            float gw = inner - gx * 2f;
            float gap = rem * 0.3f;
            float cell = (gw - gap * (cols - 1)) / cols;
            JsonObject equipment = h.S.Equipment;
            for (int i = 0; i < slots.Length; i++)
            {
                RectTransform c = UiKit.Box(card, "slot-" + slots[i]);
                UiKit.Place(c, gx + (i % cols) * (cell + gap), y + (i / cols) * (cell + gap), cell, cell);
                PopupKit.Outlined(c, "face", "pp_panel", rem * 0.4f, PopupKit.Line);
                object item = equipment != null ? equipment[slots[i]] : null;
                JsonObject it = J.Obj(item);
                string label = it != null ? J.Str(it["name"], slots[i]) : slots[i];
                TextMeshProUGUI t = UiKit.Text(c, "label", TextKind.Sub, label, it != null ? "pp_ink" : "pp_muted");
                t.fontStyle = FontStyles.Bold;
                t.textWrappingMode = TextWrappingModes.Normal;
                if (it != null && it.Has("level"))
                {
                    TextMeshProUGUI lv = UiKit.Text(c, "lv", TextKind.Sub, "Lv." + J.Int(it["level"]), "pp_ink", TextAlignmentOptions.Right);
                    UiKit.Anchor(lv.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-rem * 0.2f, rem * 0.1f), cell * 0.6f, lineH);
                }
            }
            int rows = (slots.Length + cols - 1) / cols;
            y += rows * (cell + gap) + rem * 1.25f;

            // ---- 출전 줄 ----
            List<object> pets = h.S.ActivePets;
            List<object> skills = h.S.EquippedSkills;
            int n = (pets != null ? pets.Count : 0) + (skills != null ? skills.Count : 0);
            TextMeshProUGUI lo = UiKit.Text(card, "loadout", TextKind.Sub, n > 0 ? "출전 " + n + " (스킬 " + (skills != null ? skills.Count : 0) + " · 펫 " + (pets != null ? pets.Count : 0) + ")" : "출전 중인 펫 없음", "pp_muted", TextAlignmentOptions.Left);
            UiKit.Place(lo.rectTransform, gx, y, gw, lineH);
            y += lineH + rem * 0.5f;

            // ---- 보유 옵션 ----
            List<string> subs = SubLines != null ? SubLines() : null;
            RectTransform subsBox = UiKit.Box(card, "subs");
            UiKit.Place(subsBox, gx, y, gw, Mathf.Max(lineH, cardH - y - pad - rem * 1.5f));
            RectTransform subsList = PopupKit.ScrollList(subsBox, "list", rem * 0.1f, 0f, 0f, TextAnchor.UpperLeft);
            if (subs == null || subs.Count == 0) PopupKit.Label(subsList, "none", TextKind.Sub, "보유한 옵션 없음", "pp_muted", TextAlignmentOptions.Left);
            else foreach (string s in subs) PopupKit.Label(subsList, "sub", TextKind.Sub, s, "pp_ink", TextAlignmentOptions.Left);

            PopupKit.XButton(card, () => Close(h));
        }
    }
}
