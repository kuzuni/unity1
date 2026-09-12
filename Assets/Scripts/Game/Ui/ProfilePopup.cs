using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.Meta;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 프로필/설정 팝업(ROUTINE T22 · 원작 ui.js openProfile/renderProfileView/renderSettingsView · shot-042724·042744).
    /// 프로필: 큰 아바타 + 편집(24종 격자) · 이름(편집 = 입력칸 팝업 · 원작 prompt) · 성별(♂↔♀) · «서버 랭킹» 버튼 둘(스텁) · [프로필|설정] 탭.
    /// 설정: 서버 시간 · 토글 행(진동·음악·사운드 효과·채팅 표시·채팅 다크 모드·클랜 채팅 미리보기) · 정적 행(언어·계정✓·차단 목록·개인정보 보호) · 실동작 행(수동 저장·게임 초기화).
    /// </summary>
    public static class ProfilePopup
    {
        public const string Name = "profile";
        private static string view = "profile";
        private static bool picking;

        public static void Open(MetaHost h)
        {
            view = "profile";
            picking = false;
            h.Popups.Show(Name);
            Render(h);
        }

        public static void Close(MetaHost h) { h.Popups.Hide(Name); h.Popups.Hide("confirm"); h.Popups.Hide("nickname"); }

        public static string View { get { return view; } }

        public static void Render(MetaHost h)
        {
            Popup p = h.Popups.Find(Name);
            if (p == null) return;
            RectTransform root = PopupLayer.Clear(p);
            float rem = PopupKit.Rem, w = UiKit.RefW, H = UiKit.RefH;
            float cardW = UiKit.L("profile_w") * w, cardH = UiKit.L("profile_h") * H;
            RectTransform card = PopupKit.Card(root, "card", cardW, cardH, "pp_paper", rem, "pp_line", -rem * 0.8f);
            float pad = UiKit.H("card_pad");
            float inner = cardW - PopupKit.Line3 * 2f;
            if (view == "settings") RenderSettings(h, card, inner, cardH, pad);
            else RenderProfile(h, card, inner, cardH, pad);

            // [프로필 | 설정] 탭 줄(카드 바닥 위 2.2rem)
            float tabsW = (inner - pad * 2f) * UiKit.L("profile_tabs_w");
            float tabsH = PopupKit.FontSize(TextKind.Sub) * 1.3f + rem * 1.1f;
            RectTransform tabs = UiKit.Box(card, "tabs");
            UiKit.Anchor(tabs, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, rem * 2.2f), tabsW, tabsH);
            UiKit.Rounded(tabs, "line", "pp_line", rem * 0.5f);
            Tab(tabs, "profile", "프로필", 0f, tabsW * 0.5f, tabsH, view == "profile", () => SwitchView(h, "profile"));
            Tab(tabs, "settings", "설정", tabsW * 0.5f, tabsW * 0.5f, tabsH, view == "settings", () => SwitchView(h, "settings"));

            PopupKit.XButton(card, () => Close(h));
        }

        private static void Tab(RectTransform tabs, string name, string label, float x, float w, float h, bool on, UnityEngine.Events.UnityAction onClick)
        {
            Button b = UiKit.Button(tabs, "tab-" + name, onClick);
            RectTransform rt = b.GetComponent<RectTransform>();
            UiKit.Place(rt, x + PopupKit.Line3, PopupKit.Line3, w - PopupKit.Line3 * 1.5f, h - PopupKit.Line3 * 2f);
            UiKit.Panel(rt, "face", on ? "pp_blue" : "pp_ink");
            TextMeshProUGUI t = UiKit.Text(rt, "label", TextKind.Sub, label, "stage_ink");
            t.fontStyle = FontStyles.Bold;
            if (on) PopupKit.Ring(t, "pp_line", 0.15f);
        }

        public static void SwitchView(MetaHost h, string v)
        {
            view = v;
            picking = false;
            Render(h);
        }

        // ---- 프로필 ----
        private static void RenderProfile(MetaHost h, RectTransform card, float inner, float cardH, float pad)
        {
            float rem = PopupKit.Rem, w = UiKit.RefW;
            float y = pad;
            TextMeshProUGUI title = UiKit.Text(card, "title", TextKind.Title, "프로필", "stage_ink");
            title.fontStyle = FontStyles.Bold;
            float titleH = PopupKit.FontSize(TextKind.Title) * 1.25f;
            UiKit.Place(title.rectTransform, 0f, y, inner, titleH);
            PopupKit.Ring(title);
            y += titleH + rem * 0.2f;

            float av = UiKit.L("profile_avatar") * w;
            RectTransform avatar = PopupKit.Avatar(card, "avatar", av, h.AvatarEmoji, rem * 0.5f);
            UiKit.Place(avatar, pad, y, av, av);
            float edit = UiKit.H("profile_edit") * 1.3f;
            Button avEdit = EditButton(card, "avatar-edit", () => { picking = !picking; Render(h); });
            UiKit.Place(avEdit.GetComponent<RectTransform>(), pad + (av - edit) * 0.5f, y + av + rem * 0.15f, edit, edit);

            float fx = pad + av + rem * 0.8f;
            float fw = inner - fx - pad;
            float labelH = PopupKit.FontSize(TextKind.Sub) * 1.2f;
            float fieldH = PopupKit.FontSize(TextKind.Sub) * 1.5f;
            float fy = y;
            TextMeshProUGUI l1 = UiKit.Text(card, "name-label", TextKind.Sub, "이름:", "pp_muted", TextAlignmentOptions.Left);
            l1.fontStyle = FontStyles.Bold;
            UiKit.Place(l1.rectTransform, fx, fy, fw, labelH);
            fy += labelH + rem * 0.1f;
            Field(card, "name-field", h.Nickname, fx, fy, fw - edit - rem * 0.4f, fieldH);
            Button nameEdit = EditButton(card, "name-edit", () => OpenNickname(h));
            UiKit.Place(nameEdit.GetComponent<RectTransform>(), fx + fw - edit, fy + (fieldH - edit) * 0.5f, edit, edit);
            fy += fieldH + rem * 0.25f;
            TextMeshProUGUI l2 = UiKit.Text(card, "gender-label", TextKind.Sub, "성별:", "pp_muted", TextAlignmentOptions.Left);
            l2.fontStyle = FontStyles.Bold;
            UiKit.Place(l2.rectTransform, fx, fy, fw, labelH);
            fy += labelH + rem * 0.1f;
            Field(card, "gender-field", h.Gender, fx, fy, fw - edit - rem * 0.4f, fieldH);
            Button genderEdit = EditButton(card, "gender-edit", () => OnToggleGender(h));
            UiKit.Place(genderEdit.GetComponent<RectTransform>(), fx + fw - edit, fy + (fieldH - edit) * 0.5f, edit, edit);
            y = Mathf.Max(y + av + rem * 0.15f + edit, fy + fieldH);

            if (picking)
            {
                int cols = Mathf.Max(1, Mathf.RoundToInt(UiKit.L("avatar_pick_cols")));
                float gap = rem * 0.3f;
                float cell = (inner - pad * 2f - gap * (cols - 1)) / cols;
                string[] pool = h.Meta.Avatars.Pool;
                int rows = (pool.Length + cols - 1) / cols;
                y += rem * 0.5f;
                for (int i = 0; i < pool.Length; i++)
                {
                    string e = pool[i];
                    bool on = e == h.AvatarEmoji;
                    Button b = UiKit.Button(card, "av-" + i, () => OnPickAvatar(h, e));
                    RectTransform rt = b.GetComponent<RectTransform>();
                    UiKit.Place(rt, pad + (i % cols) * (cell + gap), y + (i / cols) * (cell + gap), cell, cell);
                    UiKit.Rounded(rt, "line", on ? "pp_blue" : "pp_line", rem * 0.4f);
                    Image face = UiKit.Rounded(rt, "face", on ? "avatar_pick_on" : "avatar_bg", rem * 0.4f - PopupKit.Line);
                    PopupKit.Inset(face.rectTransform, PopupKit.Line);
                    RectTransform tile = PopupKit.Avatar(rt, "tile", cell * 0.8f, e, rem * 0.3f);
                    UiKit.Anchor(tile, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, cell * 0.8f, cell * 0.8f);
                }
                y += rows * (cell + gap);
            }
            else y += UiKit.H("profile_rank_gap");

            TextMeshProUGUI rankL = UiKit.Text(card, "rank-label", TextKind.Sub, "서버 랭킹", "pp_ink");
            rankL.fontStyle = FontStyles.Bold;
            UiKit.Place(rankL.rectTransform, 0f, y, inner, labelH);
            y += labelH + rem * 0.35f;
            float bw = UiKit.L("profile_rank_btn_w") * w, bh = UiKit.H("btn_h");
            float bx = (inner - bw * 2f - rem * 0.5f) * 0.5f;
            Button r1 = PopupKit.Btn(card, "power-rank", "파워 랭킹", "pp_blue", "pp_blue_dk", () => h.OpenStub("파워 랭킹", "서버 내 전투력 랭킹은 준비 중입니다."), bw, bh, "stage_ink", TextKind.Sub);
            UiKit.Place(r1.GetComponent<RectTransform>(), bx, y, bw, bh);
            Button r2 = PopupKit.Btn(card, "clan-rank", "클랜 랭킹", "pp_blue", "pp_blue_dk", () => h.OpenStub("클랜 랭킹", "클랜 시스템은 준비 중입니다."), bw, bh, "stage_ink", TextKind.Sub);
            UiKit.Place(r2.GetComponent<RectTransform>(), bx + bw + rem * 0.5f, y, bw, bh);
        }

        private static void Field(RectTransform card, string name, string text, float x, float y, float w, float h)
        {
            RectTransform f = UiKit.Box(card, name);
            UiKit.Place(f, x, y, w, h);
            UiKit.Rounded(f, "line", "pp_line", PopupKit.Rem * 0.4f);
            Image face = UiKit.Rounded(f, "face", "pp_panel", PopupKit.Rem * 0.4f - PopupKit.Line);
            PopupKit.Inset(face.rectTransform, PopupKit.Line);
            TextMeshProUGUI t = UiKit.Text(f, "text", TextKind.Sub, text, "pp_ink", TextAlignmentOptions.Left);
            t.fontStyle = FontStyles.Bold;
            t.rectTransform.offsetMin = new Vector2(PopupKit.Rem * 0.55f, 0f);
        }

        /// <summary>파란 라운드 사각 편집 버튼(원작 .profile-edit-btn · 연필).</summary>
        private static Button EditButton(RectTransform card, string name, UnityEngine.Events.UnityAction onClick)
        {
            Button b = UiKit.Button(card, name, onClick);
            RectTransform rt = b.GetComponent<RectTransform>();
            UiKit.Rounded(rt, "line", "pp_line", PopupKit.Rem * 0.38f);
            Image lip = UiKit.Rounded(rt, "lip", "pp_blue_dk", PopupKit.Rem * 0.3f);
            PopupKit.Inset(lip.rectTransform, PopupKit.Line);
            Image face = UiKit.Rounded(rt, "face", "pp_blue", PopupKit.Rem * 0.3f);
            face.rectTransform.offsetMin = new Vector2(PopupKit.Line, PopupKit.Line + PopupKit.Rem * 0.15f);
            face.rectTransform.offsetMax = new Vector2(-PopupKit.Line, -PopupKit.Line);
            TextMeshProUGUI t = UiKit.Text(rt, "mark", TextKind.Sub, "✎", "stage_ink");
            t.fontStyle = FontStyles.Bold;
            return b;
        }

        private static void OnPickAvatar(MetaHost h, string e)
        {
            h.S["avatarEmoji"] = e;
            picking = false;
            h.Touch();
        }

        private static void OnToggleGender(MetaHost h)
        {
            h.S["gender"] = h.Gender == Chat.GenderMale ? Chat.GenderFemale : Chat.GenderMale;
            h.Touch();
        }

        /// <summary>원작 `prompt('새 이름을 입력하세요 (최대 12자)')` — 입력칸 팝업.</summary>
        private static void OpenNickname(MetaHost h)
        {
            Popup p = h.Popups.Show("nickname");
            PopupLayer.Clear(p);
            float rem = PopupKit.Rem;
            RectTransform card = PopupKit.Card(p.Root, "card", UiKit.L("modal_card_w") * UiKit.RefW, -1f, "pp_paper", rem);
            PopupKit.Column(card, UiKit.H("card_pad"), rem * 0.5f);
            PopupKit.Label(card, "title", TextKind.Body, "새 이름을 입력하세요 (최대 12자)", "pp_ink", TextAlignmentOptions.Center, true);
            float ih = PopupKit.FontSize(TextKind.Body) * 1.6f;
            RectTransform ibox = PopupKit.Item(card, "input", -1f, ih);
            UiKit.Rounded(ibox, "line", "pp_line", rem * 0.4f);
            Image face = UiKit.Rounded(ibox, "face", "pp_panel", rem * 0.4f - PopupKit.Line);
            PopupKit.Inset(face.rectTransform, PopupKit.Line);
            face.raycastTarget = true;
            RectTransform viewport = UiKit.Box(ibox, "viewport");
            PopupKit.Inset(viewport, rem * 0.4f);
            viewport.gameObject.AddComponent<RectMask2D>();
            TextMeshProUGUI txt = UiKit.Text(viewport, "text", TextKind.Body, h.Nickname, "pp_ink", TextAlignmentOptions.Left);
            TMP_InputField input = ibox.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = viewport;
            input.textComponent = txt;
            input.characterLimit = 12;
            input.text = h.Nickname;
            RectTransform btns = PopupKit.Item(card, "buttons", -1f, UiKit.H("btn_h"));
            PopupKit.Row(btns, 0f, rem * 0.5f, TextAnchor.MiddleCenter);
            PopupKit.Btn(btns, "cancel", "취소", "pp_gray", "pp_gray_dk", () => h.Popups.Hide("nickname"), -1f, UiKit.H("btn_h"), "pp_ink", TextKind.Sub);
            PopupKit.Btn(btns, "ok", "확인", "pp_blue", "pp_blue_dk", () => { SetNickname(h, input.text); h.Popups.Hide("nickname"); }, -1f, UiKit.H("btn_h"), "stage_ink", TextKind.Sub);
        }

        public static void SetNickname(MetaHost h, string name)
        {
            if (name == null || name.Trim().Length == 0) return;
            string n = name.Trim();
            if (n.Length > 12) n = n.Substring(0, 12);
            h.S.Nickname = n;
            h.Touch();
        }

        // ---- 설정 ----
        private static void RenderSettings(MetaHost h, RectTransform card, float inner, float cardH, float pad)
        {
            float rem = PopupKit.Rem;
            float y = pad;
            TextMeshProUGUI title = UiKit.Text(card, "title", TextKind.Title, "설정", "stage_ink");
            title.fontStyle = FontStyles.Bold;
            float titleH = PopupKit.FontSize(TextKind.Title) * 1.25f;
            UiKit.Place(title.rectTransform, 0f, y, inner, titleH);
            PopupKit.Ring(title);
            y += titleH;
            DateTime d0 = DateTime.Now;
            TextMeshProUGUI sub = UiKit.Text(card, "sub", TextKind.Sub, "서버 시간: " + d0.Day + ". " + d0.Month + "월, " + d0.ToString("HH:mm"), "pp_muted");
            sub.fontStyle = FontStyles.Bold;
            float subH = PopupKit.FontSize(TextKind.Sub) * 1.3f;
            UiKit.Place(sub.rectTransform, 0f, y, inner, subH);
            y += subH + rem * 0.3f;

            float listH = UiKit.L("settings_h") * UiKit.RefH;
            RectTransform listBox = UiKit.Box(card, "list-box");
            UiKit.Place(listBox, PopupKit.Line3, y, inner - PopupKit.Line3 * 2f, listH);
            UiKit.Panel(listBox, "bg", "settings_even");
            RectTransform list = PopupKit.ScrollList(listBox, "list", 0f, 0f, 0f);
            JsonObject d = h.SettingsDummy;
            int n = 0;
            ToggleRow(h, list, n++, "진동", J.Bool(d["vibration"]), () => ToggleDummy(h, "vibration"));
            bool music = h.MusicEnabled != null ? h.MusicEnabled() : h.S.MusicOn;
            ToggleRow(h, list, n++, "음악", music, () => OnToggleMusic(h));
            ToggleRow(h, list, n++, "사운드 효과", h.S.SfxOn, () => OnToggleSfx(h));
            ToggleRow(h, list, n++, "채팅 표시", J.Bool(d["chatShow"]), () => ToggleDummy(h, "chatShow"));
            ToggleRow(h, list, n++, "채팅 다크 모드", J.Bool(d["chatDark"]), () => ToggleDummy(h, "chatDark"));
            ToggleRow(h, list, n++, "클랜 채팅 미리보기", J.Bool(d["clanChatPreview"]), () => ToggleDummy(h, "clanChatPreview"));
            StaticRow(h, list, n++, "언어", null);
            StaticRow(h, list, n++, "계정", "✓");
            StaticRow(h, list, n++, "차단 목록", null);
            StaticRow(h, list, n++, "개인정보 보호", null);
            ActRow(list, n++, "수동 저장", "저장", "pp_blue", () => { h.Save(); h.Toast("💾 저장 완료"); });
            ActRow(list, n++, "게임 초기화", "초기화", "settings_act_danger", () => Confirm(h));
        }

        private static RectTransform SettingsRow(RectTransform list, int i, string label)
        {
            float rowH = UiKit.H("settings_row_h") * 1.25f;
            RectTransform row = PopupKit.Item(list, "row-" + i, -1f, rowH);
            UiKit.Panel(row, "bg", i % 2 == 0 ? "settings_odd" : "settings_even");
            TextMeshProUGUI t = UiKit.Text(row, "label", TextKind.Sub, label, "pp_ink", TextAlignmentOptions.Left);
            t.fontStyle = FontStyles.Bold;
            t.rectTransform.offsetMin = new Vector2(PopupKit.Rem * 1.1f, 0f);
            return row;
        }

        private static void ToggleRow(MetaHost h, RectTransform list, int i, string label, bool on, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform row = SettingsRow(list, i, label);
            Button tg = PopupKit.Toggle(row, "toggle", on, onClick);
            RectTransform rt = tg.GetComponent<RectTransform>();
            float w = UiKit.H("settings_toggle_w"), hh = UiKit.H("settings_toggle_h");
            UiKit.Anchor(rt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-PopupKit.Rem * 1.95f, 0f), w, hh);
        }

        private static void StaticRow(MetaHost h, RectTransform list, int i, string label, string check)
        {
            RectTransform row = SettingsRow(list, i, label);
            UiKit.Button(row, "hit", () => h.Toast("데모 버전에서는 지원하지 않습니다"));
            if (check != null)
            {
                TextMeshProUGUI c = UiKit.Text(row, "check", TextKind.Body, check, "pp_green", TextAlignmentOptions.Right);
                c.fontStyle = FontStyles.Bold;
                c.rectTransform.offsetMax = new Vector2(-PopupKit.Rem * 1.95f, 0f);
            }
        }

        private static void ActRow(RectTransform list, int i, string label, string act, string inkKey, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform row = SettingsRow(list, i, label);
            float bw = PopupKit.Rem * 3.2f, bh = UiKit.H("settings_toggle_h") * 1.1f;
            Button b = UiKit.Button(row, "act", onClick);
            RectTransform rt = b.GetComponent<RectTransform>();
            UiKit.Anchor(rt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-PopupKit.Rem * 1.95f, 0f), bw, bh);
            UiKit.Rounded(rt, "line", "pp_line", PopupKit.Rem * 0.5f);
            Image face = UiKit.Rounded(rt, "face", "pp_paper", PopupKit.Rem * 0.5f - PopupKit.Line);
            PopupKit.Inset(face.rectTransform, PopupKit.Line);
            TextMeshProUGUI t = UiKit.Text(rt, "label", TextKind.Sub, act, inkKey);
            t.fontStyle = FontStyles.Bold;
        }

        private static void ToggleDummy(MetaHost h, string key)
        {
            JsonObject d = h.SettingsDummy;
            d[key] = !J.Bool(d[key]);
            h.Touch();
        }

        private static void OnToggleMusic(MetaHost h)
        {
            if (h.ToggleMusic != null) h.ToggleMusic();
            else h.S.MusicOn = !h.S.MusicOn;
            h.Touch();
        }

        private static void OnToggleSfx(MetaHost h)
        {
            h.S.SfxOn = !h.S.SfxOn;
            if (h.S.SfxOn && h.SfxResumeAndCraft != null) h.SfxResumeAndCraft();
            h.Touch();
        }

        /// <summary>원작 `confirm('정말 처음부터 시작할까요?')` → resetGame().</summary>
        private static void Confirm(MetaHost h)
        {
            Popup p = h.Popups.Show("confirm");
            PopupLayer.Clear(p);
            float rem = PopupKit.Rem;
            RectTransform card = PopupKit.Card(p.Root, "card", UiKit.L("modal_card_w") * UiKit.RefW, -1f, "pp_paper", rem);
            PopupKit.Column(card, UiKit.H("card_pad"), rem * 0.5f);
            PopupKit.Label(card, "q", TextKind.Body, "정말 처음부터 시작할까요?", "pp_ink", TextAlignmentOptions.Center, true);
            RectTransform btns = PopupKit.Item(card, "buttons", -1f, UiKit.H("btn_h"));
            PopupKit.Row(btns, 0f, rem * 0.5f, TextAnchor.MiddleCenter);
            PopupKit.Btn(btns, "cancel", "취소", "pp_gray", "pp_gray_dk", () => h.Popups.Hide("confirm"), -1f, UiKit.H("btn_h"), "pp_ink", TextKind.Sub);
            PopupKit.Btn(btns, "ok", "초기화", "pp_red", "pp_red_dk", () => { if (SaveIo.Instance != null) SaveIo.Instance.ResetGame(); }, -1f, UiKit.H("btn_h"), "stage_ink", TextKind.Sub);
        }
    }
}
