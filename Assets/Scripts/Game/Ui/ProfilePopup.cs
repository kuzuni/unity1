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
            h.Popups.Show(Name, null, PopupZUi.AboveTabBar(Name));   // T346 — 정본 `#profile-modal` z 40 > 탭바 30(표 PopupZUi)
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
            // T395 — 정본 `#profile-modal .idet-wrap { top: .8rem }`(style.css 3042)은 **CSS 보정값**이다(래퍼가 ✕ 까지 끌어안은 채 세로 중앙에 놓여 카드가 위로 밀린 것을
            //   되돌리는 값 · 3039~3041 주석). `PopupKit.Card` 는 카드 자체를 가운데 두므로 그 문제가 없다 — 옮기면 카드가 .8rem 아래로 한 번 더 간다(런 743 실측 +14.5px). 옮기지 않는다.
            RectTransform card = PopupKit.Card(root, "card", cardW, cardH, "pp_paper", rem, "pp_line", 0f);
            float pad = UiKit.H("card_pad");
            float inner = cardW - PopupKit.Line3 * 2f;
            if (view == "settings") RenderSettings(h, card, inner, cardH, pad);
            else RenderProfile(h, card, inner, cardH, pad);

            // [프로필 | 설정] 탭 줄 — T445: 정본 3038 `.profile-sheet .profile-tabs { margin-top: auto; margin-bottom: 2.2rem }` 은 **카드 패딩 상자 안**의 마진이라
            //   카드 바닥에서 탭 아래끝까지 = 패딩 1.1rem(1754 · `card_pad`) + 2.2rem = **3.3rem**(정본 주석 3036 «원본은 카드 바닥까지 55px(6.2%H)»).
            //   `PopupKit.Card` 의 RectTransform 은 테 상자라 전엔 2.2rem 만 띄워 패딩 몫 1.1rem 이 통째로 빠졌다(런 1048 실측 37px ↔ 원작 55px).
            float tabsW = (inner - pad * 2f) * UiKit.L("profile_tabs_w");
            float tabsH = PopupKit.FontSize(TextKind.Sub) * 1.3f + rem * 1.1f;
            RectTransform tabs = UiKit.Box(card, "tabs");
            UiKit.Anchor(tabs, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, pad + rem * 2.2f), tabsW, tabsH);
            UiKit.Rounded(tabs, "line", "pp_line", RadiusUi.Px("profile_tabs_r_rem"));   // 정본 3068 `.profile-tabs { border-radius: .5rem }`(T345 20회차)
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
            if (on) PopupKit.Ring(t, "profile_tab_on", "pp_line");   // 정본 .profile-tabs button.on 2px
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
            PopupKit.Ring(title, "sheet_title", "pp_line");   // 정본 .profile-title .11em
            y += titleH + rem * 0.2f;

            float av = UiKit.L("profile_avatar") * w;
            RectTransform avatar = PopupKit.Avatar(card, "avatar", av, h.AvatarEmoji, RadiusUi.Px("profile_avatar_big_r_rem"), PopupKit.Line3);   // T345 11회차 — 정본 2997 `.profile-avatar-big` .5rem(값은 맞았고 수만 표로)
            UiKit.Place(avatar, pad, y, av, av);
            float edit = UiKit.H("profile_edit");   // T378 3회차 — 정본 3008 `.profile-edit-btn { width: 1.5rem; height: 1.5rem }` = 표 0.0284 그대로 · 여태 ×1.3 이 얹혀 30% 컸다
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
            // T131 — 정본 ui.js 5043 `<span class="profile-field">${IconGen.img(S.gender === '♀' ? 'gender_f' : 'gender_m')}</span>`: 성별은 글자 ♂/♀ 가 아니라 아이콘(칸 안 1.15em · style.css 3159).
            IconField(card, "gender-field", Chat.GenderIcon(h.Gender), fx, fy, fw - edit - rem * 0.4f, fieldH);
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
                    UiKit.Rounded(rt, "line", on ? "pp_blue" : "pp_line", RadiusUi.Px("avatar_pick_r_rem"));   // T345 11회차 — 같은 정본 줄(3062)의 테
                    float pickLine = UiKit.L("line2_px");   // T365 4회차 — 정본 3061 `.avatar-pick-btn { border: var(--ol2) … }` = ol2(전엔 ol1)
                    Image face = UiKit.Rounded(rt, "face", on ? "avatar_pick_on" : "avatar_bg", RadiusUi.Px("avatar_pick_r_rem") - pickLine);   // T345 11회차 — 정본 3062 `.avatar-pick-btn` .4rem
                    PopupKit.Inset(face.rectTransform, pickLine);
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
            // T432 — 정본 3060 `.profile-rank-row .btn { width: calc(var(--app-w) * .2177 + 6.6px) }` · 그 위 주석(3058)이 «+6.6px 는 **키라인 몫**» 이라 적어 둔다.
            //   곧 표값 `.2177` 은 **파랑 채움** 폭(원작 042724 실측 108px)이지 상자 폭이 아니다. `PopupKit.Btn` 은 테(`line`) 안에 면(`face`)을 `Line3` 만큼 들여 그리므로,
            //   상자에 `.2177` 을 그대로 주면 채움이 키라인 두 겹만큼 안으로 먹힌다 — 런 968 실측 **111px**(기대 117.6 · −6.6 = `line3_px` × 2).
            //   틈은 안 움직인다: 정본의 «틈 16px» 도 `gap: .5rem` + 키라인 두 겹이고 클론도 같은 셈이다.
            float bw = UiKit.L("profile_rank_btn_w") * w + PopupKit.Line3 * 2f, bh = UiKit.H("btn_h");
            float bx = (inner - bw * 2f - rem * 0.5f) * 0.5f;
            Button r1 = PopupKit.Btn(card, "power-rank", "파워 랭킹", "pp_blue", "pp_blue_dk", () => h.OpenStub("파워 랭킹", "서버 내 전투력 랭킹은 준비 중입니다."), bw, bh, "stage_ink", TextKind.Sub);
            UiKit.Place(r1.GetComponent<RectTransform>(), bx, y, bw, bh);
            Button r2 = PopupKit.Btn(card, "clan-rank", "클랜 랭킹", "pp_blue", "pp_blue_dk", () => h.OpenStub("클랜 랭킹", "클랜 시스템은 준비 중입니다."), bw, bh, "stage_ink", TextKind.Sub);
            UiKit.Place(r2.GetComponent<RectTransform>(), bx + bw + rem * 0.5f, y, bw, bh);
        }

        /// <summary>글자 대신 아이콘 하나가 든 칸(원작 `.profile-field .ico` — T131 성별). 상자·왼쪽 여백은 <see cref="Field"/> 와 같다.</summary>
        private static void IconField(RectTransform card, string name, string iconKey, float x, float y, float w, float h)
        {
            RectTransform f = UiKit.Box(card, name);
            UiKit.Place(f, x, y, w, h);
            UiKit.Rounded(f, "line", "pp_line", RadiusUi.Px("profile_field_r_rem"));
            float fieldLine = UiKit.L("line2_px");   // T365 4회차 — 정본 3047 `.profile-field { border: var(--ol2) … }` = ol2(전엔 line_px = ol1)
            Image face = UiKit.Rounded(f, "face", "pp_panel", RadiusUi.Px("profile_field_r_rem") - fieldLine);
            PopupKit.Inset(face.rectTransform, fieldLine);
            float em = PersonIcons.Px("profile_gender_em", PopupKit.FontSize(TextKind.Sub));
            Image ico = UiKit.Icon(f, "ico", iconKey);
            UiKit.Anchor(ico.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(PopupKit.Rem * 0.55f, 0f), em, em);
        }

        private static void Field(RectTransform card, string name, string text, float x, float y, float w, float h)
        {
            RectTransform f = UiKit.Box(card, name);
            UiKit.Place(f, x, y, w, h);
            UiKit.Rounded(f, "line", "pp_line", RadiusUi.Px("profile_field_r_rem"));
            float fieldLine = UiKit.L("line2_px");   // T365 4회차 — 정본 3047 `.profile-field { border: var(--ol2) … }` = ol2(전엔 line_px = ol1)
            Image face = UiKit.Rounded(f, "face", "pp_panel", RadiusUi.Px("profile_field_r_rem") - fieldLine);
            PopupKit.Inset(face.rectTransform, fieldLine);
            TextMeshProUGUI t = UiKit.Text(f, "text", TextKind.Sub, text, "pp_ink", TextAlignmentOptions.Left);
            WrapUi.Apply(t, "profile_field");   // T361 2회차 — 정본 white-space 표(WrapUi.json) 3052 `.profile-field { nowrap }`
            t.fontStyle = FontStyles.Bold;
            t.rectTransform.offsetMin = new Vector2(PopupKit.Rem * 0.55f, 0f);
            TextClamp.Apply(t, "profile_field");   // T351 — 정본 3052 .profile-field { overflow: hidden; text-overflow: ellipsis; white-space: nowrap }
            LineHeight.Apply(t, "profile_field_lh");   // T354 9회차 — 정본 3051 같은 규칙의 `line-height: 1.32`(한 줄로 잘리는 칸이라 눈에는 안 보이지만, 표의 81 자리를 코드가 읽게 둔다)
        }

        /// <summary>파란 라운드 사각 편집 버튼(원작 .profile-edit-btn · 연필).</summary>
        private static Button EditButton(RectTransform card, string name, UnityEngine.Events.UnityAction onClick)
        {
            Button b = UiKit.Button(card, name, onClick);
            RectTransform rt = b.GetComponent<RectTransform>();
            UiKit.Rounded(rt, "line", "pp_line", RadiusUi.Px("profile_edit_r_rem"));   // T345 11회차 — 정본 3007 `.profile-edit-btn` .38rem(안쪽 면·아래턱 0.3 은 클론이 만든 겹이라 그대로)
            Image lip = UiKit.Rounded(rt, "lip", "pp_blue_dk", PopupKit.Rem * 0.3f);
            PopupKit.Inset(lip.rectTransform, PopupKit.Line);
            Image face = UiKit.Rounded(rt, "face", "pp_blue", PopupKit.Rem * 0.3f);
            face.rectTransform.offsetMin = new Vector2(PopupKit.Line, PopupKit.Line + PopupKit.Rem * 0.15f);
            face.rectTransform.offsetMax = new Vector2(-PopupKit.Line, -PopupKit.Line);
            // T89 — 정본 `ui.js` 5033·5039·5044 의 `.profile-edit-btn` 은 `IconGen.img('pencil')` 이다.
            // «✎»(U+270E)는 글꼴에 없어 □ 로 찍혔다 — T31 아이콘으로.
            Image t = UiKit.Icon(rt, "mark", "pencil");
            PopupKit.Inset(t.rectTransform, PopupKit.Rem * 0.22f);
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
            UiKit.Rounded(ibox, "line", "pp_line", RadiusUi.Px("profile_field_r_rem"));
            Image face = UiKit.Rounded(ibox, "face", "pp_panel", RadiusUi.Px("profile_field_r_rem") - PopupKit.Line);
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
            PopupKit.Ring(title, "sheet_title", "pp_line");   // 정본 .profile-title .11em
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
            StaticRow(h, list, n++, "언어", false);
            StaticRow(h, list, n++, "계정", true);
            StaticRow(h, list, n++, "차단 목록", false);
            StaticRow(h, list, n++, "개인정보 보호", false);
            ActRow(list, n++, "수동 저장", "저장", "pp_blue", () => { h.Save(); h.Toast("💾 저장 완료"); });
            ActRow(list, n++, "게임 초기화", "초기화", "settings_act_danger", () => Confirm(h));
        }

        private static RectTransform SettingsRow(RectTransform list, int i, string label)
        {
            // T140 — 카탈로그 `settings_row_h`(.0475)가 **이미 정본 값**이다: 정본 `style.css` 3098~3103 의 주석이
            // «세로 .5rem 은 39.5px 라 원본 42px(4.75%H)보다 낮았다» 며 그 4.75%H 를 기준으로 여백을 잡았다.
            // 거기에 1.25 를 한 번 더 곱하고 있어 행이 42 → 57px 로 부풀었고, 상자(45.6%H)는 정본대로라
            // 같은 자리에 원작은 **열 줄**, 클론은 **여덟 줄**만 들어 «차단 목록»·«개인정보 보호» 가 첫 화면에서 사라졌다.
            float rowH = UiKit.H("settings_row_h");
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

        private static void StaticRow(MetaHost h, RectTransform list, int i, string label, bool check)
        {
            RectTransform row = SettingsRow(list, i, label);
            UiKit.Button(row, "hit", () => h.Toast("데모 버전에서는 지원하지 않습니다"));
            if (check)
            {
                // T100 — 정본 `ui.js` 5078~5081 의 `checkRow` 는 `<span class="settings-check">${IconGen.img('check')}</span>` 다.
                // 클론은 «✓»(U+2713)를 **글자로** 찍어 주인 한글 글꼴에 없어 □ 였다(T89 막이를 데이터·변수까지 넓혀서 드러났다).
                float sz = PopupKit.Rem * 1.25f;
                Image c = UiKit.Icon(row, "check", "check");
                UiKit.Anchor(c.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                             new Vector2(-PopupKit.Rem * 1.95f - sz * 0.5f, 0f), sz, sz);
            }
        }

        private static void ActRow(RectTransform list, int i, string label, string act, string inkKey, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform row = SettingsRow(list, i, label);
            // T378 7회차 — 정본 3121~3125 `.settings-act` 는 높이 선언이 없고 line-height 1.15rem + padding .1rem×2 + ol2×2 = 1.6rem(0.0303H)이다.
            // 전엔 토글 높이(1.35rem)×1.1 = 1.485rem 으로 8% 낮았다(«표값 × 박힌 상수» 자리 · 곁 표 ProfileUi.json).
            float bw = PopupKit.Rem * 3.2f, bh = ProfileUi.H("settings_act_h");
            Button b = UiKit.Button(row, "act", onClick);
            RectTransform rt = b.GetComponent<RectTransform>();
            UiKit.Anchor(rt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-PopupKit.Rem * 1.95f, 0f), bw, bh);
            UiKit.Rounded(rt, "line", "pp_line", RadiusUi.Px("settings_act_r_rem"));   // 정본 3121 `.settings-act { border-radius: .5rem }`(T345 20회차)
            float actLine = UiKit.L("line2_px");   // T365 4회차 — 정본 3121 `.settings-act { border: var(--ol2) … }` = ol2(전엔 ol1)
            Image face = UiKit.Rounded(rt, "face", "pp_paper", RadiusUi.Px("settings_act_r_rem") - actLine);
            PopupKit.Inset(face.rectTransform, actLine);
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
