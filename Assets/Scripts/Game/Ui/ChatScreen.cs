using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Meta;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 채팅(ROUTINE T22 · 원작 ui.js openChat/renderChatFull/renderChatList/renderChatPreview/chatMsgHtml · shot-043500):
    /// 하단 미리보기 한 줄(HUD 채팅 띠 · T18 <see cref="Hud.SetChatPreview"/>) + 탭하면 흰 전체화면(메시지 목록 · 입력 바 = 둥근 빨간 ◀ + 입력칸 · Enter 로 보냄).
    /// 봇 메시지는 <see cref="MetaHost"/> 의 1초 틱이 만들고, 여기서는 목록만 다시 그린다(입력칸은 처음 한 번만 만들어 포커스를 안 잃는다 — 원작 QA 13차).
    /// </summary>
    public static class ChatScreen
    {
        public const string Name = "chat";
        private static TMP_InputField input;
        private static RectTransform list;
        private static ScrollRect scroll;
        /// <summary>정본 `_chatStick` — 바닥에 붙어 있는가(true 면 재렌더가 바닥을 따라간다).</summary>
        private static bool stick = true;
        private static bool rendering;
        /// <summary>«바닥» 판정의 부동소수 여유(설계 수치가 아니다 · 정규화 0~1 에서 0 근처).</summary>
        private const float BottomEps = 1e-3f;

        public static void Open(MetaHost h)
        {
            h.Chat.Ensure(h.ChatState, h.NowMs);
            Popup p = h.Popups.Show(Name, null, PopupZUi.AboveTabBar(Name));   // T346 3회차 — 정본 3787 `#chat-modal { z-index: 40 }` > 탭바 30 · 층은 표 PopupZUi.json 이 정한다
            if (p.Root.childCount > 1) { RenderList(h); return; }
            float rem = PopupKit.Rem, w = UiKit.RefW;
            RectTransform card = PopupKit.Sheet(p.Root, "card", "pp_paper");
            float inputH = UiKit.H("chat_input_h");
            float bottom = UiKit.RefH - PopupKit.TabTop;

            RectTransform listBox = UiKit.Box(card, "list-box");
            listBox.anchorMin = new Vector2(0f, 0f);
            listBox.anchorMax = new Vector2(1f, 1f);
            listBox.offsetMin = new Vector2(0f, bottom + inputH);
            // T378 6회차 — 정본 3265 `.chat-card { padding: 0 }` · 3294 `.chat-list { padding: 0 }`: 목록은 카드 위끝에서 바로 시작한다(모달 z 40 이 상단바 z 5 를 덮는다) — 전엔 상단바 높이×0.3 의 근거 없는 인셋이 있었다
            listBox.offsetMax = new Vector2(0f, 0f);
            list = PopupKit.ScrollList(listBox, "list", rem * 0.41f, rem * 0.5f, rem * 0.5f, TextAnchor.LowerLeft);
            // ScrollRect 는 list-box 가 아니라 ScrollList 가 그 안에 세운 «list» 상자(content 의 부모)에 붙는다 —
            // 옛 코드는 list-box 에서 찾아 늘 null 이었고 그래서 «바닥으로» 가 한 번도 안 돌았다(런 179 채팅 샷 · 런 186 NRE).
            scroll = list.parent != null ? list.parent.GetComponent<ScrollRect>() : null;
            // 정본 onChatScroll — 사용자가 위로 올려 옛 메시지를 읽는 중이면 새 메시지가 와도 끌어내리지 않는다. 바닥이면 따라간다.
            if (scroll != null) scroll.onValueChanged.AddListener(_ => { if (!rendering && scroll != null) stick = scroll.verticalNormalizedPosition <= BottomEps; });

            RectTransform bar = UiKit.Box(card, "input-bar");
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = new Vector2(0f, bottom);
            bar.sizeDelta = new Vector2(0f, inputH);
            // T377 10회차 — 정본 3438 `.chat-input-bar { background: #0e111b }` 는 못박은 리터럴이다. 바로 위 3439 주석이 까닭까지 적었다:
            //   «카드가 흰색이 됐으므로 밴드는 **자기 배경 #0e111b 를 직접 갖는다**». 곧 «흰 카드» 로 바뀐 뒤에도 입력 밴드만은 어둡게 남긴 자리다.
            //   클론은 전역 `pp_paper`(#ffffff)로 찍어 밴드가 카드와 한 덩어리로 희었다(1회차 곁다리 실측 · 런 720 `screen_chat.png` 입력 바 (255,255,255)).
            //   키 인수는 그대로 두고(키라인·글자 그림자 갈래가 «종이» 를 그 키로 가른다) 면 `Image` 색만 표로 덮는다 — 판매 버튼·뒤로 버튼과 같은 길.
            Image barBg = UiKit.Panel(bar, "bg", "pp_paper");
            barBg.color = PinnedColorUi.C("chat_bar_face");
            float barLine = UiKit.L("line2_px");   // T365 4회차 — 정본 3444 `.chat-input-bar { border-top: var(--ol2) solid #000 }` = ol2(전엔 ol1)
            UiKit.Line(bar, "line", "pp_line", barLine, true);
            float bw = w * 0.0721f, bh = w * 0.0581f;
            Button back = UiKit.Button(bar, "close", () => Close(h));
            RectTransform brt = back.GetComponent<RectTransform>();
            // T364 6회차 — 정본 3445 `.chat-input-bar { padding: … calc(var(--app-w) * .02) }` 의 **왼쪽 인셋**(주석 «좌 인셋 10px=2.00%W»).
            // 종전 `rem * 0.5`(= 8.0px)는 앱 **높이** 기준 어림이라 화면비가 바뀌면 가로가 틀어진다(이 작업의 갈래).
            UiKit.Place(brt, ChatUi.W("chat_bar_pad_l_w"), (inputH - bh) * 0.5f, bw, bh);
            // T345 — 정본 3270 `.chat-input-bar .btn.round { border-radius: .35rem }`(표 `chat_round_btn_r_rem` · 전엔 높이×.3 = .52rem 이었다)
            float backR = RadiusUi.Px("chat_round_btn_r_rem");
            UiKit.Rounded(brt, "line", "pp_line", backR);
            // T365 4회차 — 정본 3284 `.chat-input-bar .btn.danger.round { border: var(--ol2) … }` = ol2(전엔 Line3 = ol3)
            // T469 — 그런데 같은 블록 3289 `border-width: var(--ol1)` 이 그 단축의 굵기를 **덮는다**(정본 주석 «테두리도 2px 이 아니라 1px 이다» · 실측 y749 검정 1px). 곧 ol1.
            float backLine = UiKit.L("line_px");
            Image bface = UiKit.Rounded(brt, "face", "pp_red", Mathf.Max(1f, backR - backLine));
            // T377 — 정본 3283 `.chat-input-bar .btn.danger.round { background: #ff1017 }` 는 전역 --pp-red(#e8362f)가 아니라 **못박은 리터럴**이다
            // (8692 «토큰을 옮기지 말 것 · 버튼 규칙에만 리터럴»). 자리 전용 키로 받는다 — 값은 PinnedColorUi.json · check_pinned_colors 가 지킨다.
            bface.color = PinnedColorUi.C("chat_back_face");
            PopupKit.Inset(bface.rectTransform, backLine);
            Image tri = PopupKit.Tri(brt, "tri", "stage_ink");
            float tw = UiKit.RefH * 0.0179f;
            UiKit.Anchor(tri.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, tw, tw);
            // T453 — 정본 3283 `.chat-input-bar .btn.danger.round { text-shadow: 8방 1px #000 }`(주석 3281 «삼각 글리프의 검정 외곽선도 여기서 준다»).
            //   클론의 ◀ 는 아틀라스 아이콘이라 언더레이·SDF 스트로크가 안 닿는다 — 같은 그림을 표(IconShadowUi rings.chat_back)의 오프셋만큼 뒤에 깐다.
            IconShadow.Ring(tri, "chat_back");

            // T364 6회차 — 왼쪽 인셋 + 버튼 + **틈**(정본 3442 `gap: calc(var(--app-w) * .022)` · 주석 «버튼→입력칸 간격 13px=2.60%W»).
            float ix = ChatUi.W("chat_bar_pad_l_w") + bw + ChatUi.W("chat_bar_gap_w");
            RectTransform ibox = UiKit.Box(bar, "input");
            // 오른쪽 인셋도 표로(정본 3445 `calc(var(--app-w) * .024)`) — 정본 주석 3441 이 «입력칸 좌 11.82%W · 폭 85.37%W 가
            // **자동으로 따라온다**» 고 적어 둔 자리라, 이 셋 중 하나만 어림이어도 입력칸 폭이 통째로 밀린다.
            UiKit.Place(ibox, ix, (inputH - bh * 1.15f) * 0.5f, w - ix - ChatUi.W("chat_bar_pad_r_w"), bh * 1.15f);
            // T345 — 정본 3449 `.chat-input-bar input { border-radius: .3rem }`(표 `chat_input_r_rem` · 전엔 높이×.3 = .52rem)
            float inputR = RadiusUi.Px("chat_input_r_rem");
            UiKit.Rounded(ibox, "line", "pp_line", inputR);
            // T365 4회차 — 정본 3450 `.chat-input-bar input { border: var(--ol2) … }` = ol2(전엔 ol1)
            Image iface = UiKit.Rounded(ibox, "face", "pp_panel", Mathf.Max(1f, inputR - barLine));
            PopupKit.Inset(iface.rectTransform, barLine);
            iface.raycastTarget = true;
            RectTransform viewport = UiKit.Box(ibox, "viewport");
            PopupKit.Inset(viewport, rem * 0.4f);
            viewport.gameObject.AddComponent<RectMask2D>();
            TextMeshProUGUI txt = UiKit.Text(viewport, "text", TextKind.Sub, string.Empty, "pp_ink", TextAlignmentOptions.Left);
            TextMeshProUGUI ph = UiKit.Text(viewport, "placeholder", TextKind.Sub, "메시지 보내기...", "pp_muted", TextAlignmentOptions.Left);
            // T396 6회차 — 정본 3452 `.chat-input-bar input::placeholder { color: #6b6b6b }` 는 «브라우저 기본 #8e8e93 이 흐려서» 일부러 진하게 못박은 리터럴이다.
            //   전역 pp_muted(#8a8a8a)는 그보다 옅다 — 자리 전용 키(PinnedColorUi · check_pinned_colors 가 정본과 같은지 지킨다).
            ph.color = PinnedColorUi.C("chat_placeholder_ink");
            // T168 3회차 — 정본 style.css 3460 `.chat-input-bar input::placeholder { letter-spacing: -.06em }`(음수 · 안내 글자만 좁힌다).
            // 정본 3459 주석: «굵기를 되살리고 폭은 letter-spacing 으로 원본값에 맞춘다(두 지표를 동시에 통과시키는 유일한 조합)».
            LetterSpacing.Apply(ph, "chat_placeholder_ls_em");
            input = ibox.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = viewport;
            input.textComponent = txt;
            input.placeholder = ph;
            input.characterLimit = Chat.MaxPlayerTextLen;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.onSubmit.AddListener(_ => Send(h));

            RenderList(h);
            // 정본 openChat: renderChatFull 뒤 보이게 만들고 `pinChatBottom()` 을 한 번 더 — «최신 메시지가 입력바 바로 위».
            PinBottom();
        }

        public static void Close(MetaHost h)
        {
            h.Popups.Hide(Name);
            input = null;
            list = null;
            scroll = null;
            stick = true;
        }

        /// <summary>정본 `pinChatBottom` — 목록을 최신 메시지에 붙인다. 행 높이는 전부 LayoutElement 고정값이라 레이아웃을 즉시 다시 재고 바닥(0)으로 민다
        /// (재기 전에 0 을 넣으면 ScrollRect 가 옛 content 높이로 정규화해 헛값이 된다 — 런 179 채팅 샷이 11:05~11:20 에 멈춰 있던 이유).</summary>
        public static void PinBottom()
        {
            if (list == null || scroll == null) return;
            stick = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(list);
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;
        }

        /// <summary>원작 onSendChat — 입력값을 보내고 비운다.</summary>
        public static void Send(MetaHost h)
        {
            if (input == null) return;
            string text = input.text;
            if (h.Chat.SendPlayer(h.ChatState, text, h.Nickname, h.AvatarEmoji, h.Gender, h.NowMs))
            {
                input.text = string.Empty;
                h.Touch();
            }
        }

        /// <summary>상태가 바뀌었다 — 미리보기 줄 + (열려 있으면) 목록.</summary>
        public static void OnChanged(MetaHost h)
        {
            RenderPreview(h);
            if (h.Popups.IsOpen(Name) && list != null) RenderList(h);
        }

        /// <summary>원작 renderChatPreview — 마지막 메시지의 «이름: 글» 한 줄(공유 카드는 «전투 결과를 공유했습니다»).</summary>
        public static void RenderPreview(MetaHost h)
        {
            Hud hud = Hud.Instance;
            if (hud == null || h.ChatState == null) return;
            ChatMessage last = h.Chat.LastMessage(h.ChatState, h.NowMs);
            if (last == null) return;
            string name = last.Type == ChatMessage.TypeShare ? last.MyName : last.Name;
            string msg = last.Type == ChatMessage.TypeShare ? "전투 결과를 공유했습니다" : last.Text;
            hud.SetChatPreview(name, msg);   // 정본 renderChatPreview 는 이름 줄 / 메시지 줄 두 줄이다
        }

        private static void RenderList(MetaHost h)
        {
            if (list == null) return;
            bool wasStick = stick;
            rendering = true;
            for (int i = list.childCount - 1; i >= 0; i--)
            {
                GameObject old = list.GetChild(i).gameObject;
                old.SetActive(false);                 // Destroy 는 프레임 끝에 되므로 레이아웃이 옛 줄을 안 세게 먼저 끈다
                UnityEngine.Object.Destroy(old);
            }
            List<ChatMessage> msgs = h.ChatState.Messages ?? new List<ChatMessage>();
            foreach (ChatMessage m in msgs) Row(list, m);
            rendering = false;
            if (wasStick) PinBottom();               // 정본 renderChatList: 바닥에 붙어 있었으면 새 메시지를 따라간다
        }

        private static string Time(double at)
        {
            DateTime d = DateTimeOffset.FromUnixTimeMilliseconds((long)at).ToLocalTime().DateTime;
            return d.ToString("HH:mm");
        }

        /// <summary>한 줄(원작 chatMsgHtml): 아바타 · [태그] 이름 · 성별/클랜 · 시각 · 말풍선 또는 공유 카드.</summary>
        private static void Row(RectTransform parent, ChatMessage m)
        {
            float rem = PopupKit.Rem, w = UiKit.RefW;
            float av = UiKit.H("chat_avatar_big");
            float nameH = PopupKit.FontSize(TextKind.Sub) * 1.3f;
            bool share = m.Type == ChatMessage.TypeShare;
            float bubbleW = UiKit.L("chat_bubble_w") * w;
            float bodyH = share ? ShareBodyH() : Mathf.Max(nameH, EstimateLines(m.Text, bubbleW) * PopupKit.FontSize(TextKind.Sub) * 1.25f + rem * 0.6f);   // T379 — 공유 카드는 한 쪽의 세로 쌓임이 높이를 정한다(종전 rem*4.2 박힘)
            float rowH = nameH + rem * 0.2f + bodyH;
            RectTransform row = PopupKit.Item(parent, "msg", -1f, rowH);
            string avatar = share ? m.MyAvatar : m.Avatar;
            RectTransform tile = PopupKit.Avatar(row, "avatar", av, avatar, RadiusUi.Px("chat_avatar_r_rem"));   // T345 16회차 — 정본 3311 `.chat-avatar` .4rem
            UiKit.Place(tile, 0f, 0f, av, av);
            // T364 6회차 — 정본 3309 `.chat-row { gap: calc(var(--app-w) * .008) }`(주석 «간격 4px=0.80%W» · 499px 실측).
            // 종전 `rem * 0.4`(= 6.4px = 1.28%W)는 2.4px 넓고 축도 높이 기준이라, 긴 이름줄에서 말풍선 우끝(정본 87.17%W)이 밀렸다.
            float x = av + ChatUi.W("chat_row_gap_w");
            string name = share ? m.MyName : m.Name;
            RectTransform nameLine = UiKit.Box(row, "name-line");
            UiKit.Place(nameLine, x, 0f, bubbleW, nameH);
            TextMeshProUGUI nm = UiKit.Text(nameLine, "name", TextKind.Sub, (m.Tag != null ? "[" + m.Tag + "] " : string.Empty) + name, "chat_name", TextAlignmentOptions.Left);
            nm.fontStyle = FontStyles.Bold;
            // T333 3회차 — 정본 style.css 3344 `.chat-name, .chat-tag` 의 20겹 링: 변당 2px 순검정이다(정본 주석이 «1px 8방향으로는 절반» 이라 2px 을 겹쳐 둘렀다고 적어 둔 그 자리 ·
            // --olc 오타로 한 번도 안 그려졌던 4겹을 #000 으로 되살린 규칙). 흰 목록 위 주황 닉네임이 떠 보이는 까닭이 이 키라인이다 — 언더레이 한 겹으로는 못 내니 SDF 스트로크로.
            UiKit.OutlinePx(nm, "pp_line", TextShadowUi.RingPx("chat_name"));
            nm.rectTransform.offsetMax = new Vector2(-rem * 3f, 0f);
            // T131 — 정본 ui.js `chatNameIcons(m)`(5259·5281 두 줄 다): 이름 뒤에 [성별 아이콘][클랜 배지]. 성별은 글자 ♂/♀ 가 아니라 gender_m/f 아이콘,
            // 배지는 이름 해시가 h%3≠0 인 이름에만(Core Chat.ClanBadge). 치수는 정본 style.css 3336~3341(PersonIconsUi.json).
            float gw = PersonIcons.Px("chat_gender_w"), cw = PersonIcons.Px("chat_clan_w");
            float gx = bubbleW - rem * 3f + PersonIcons.Px("chat_ico_mr_em", PopupKit.FontSize(TextKind.Sub));
            Image gender = UiKit.Icon(nameLine, "gender", Chat.GenderIcon(m.Gender));
            UiKit.Anchor(gender.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(gx, 0f), gw, gw);
            if (Chat.ClanBadge(m.Name))   // 정본은 m.name 을 해시한다 — 내 공유 카드(name 없음)엔 배지가 안 붙는다
            {
                Image clan = UiKit.Icon(nameLine, "clan", "clanbadge");
                UiKit.Anchor(clan.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(gx + gw + PersonIcons.Px("chat_clan_ml_w"), 0f), cw, cw);
            }
            TextMeshProUGUI time = UiKit.Text(nameLine, "time", TextKind.Sub, Time(m.At), "chat_time", TextAlignmentOptions.Right);
            time.fontStyle = FontStyles.Bold;
            UiKit.OutlinePx(time, "chat_time", KeylineUi.Px("chat_time"));   // 정본 .chat-time { .5px currentColor }
            time.outlineColor = time.color;
            time.rectTransform.offsetMin = new Vector2(bubbleW - rem * 3.2f, 0f);
            time.rectTransform.offsetMax = new Vector2(rem * 3.2f, 0f);

            if (share)
            {
                RectTransform card = UiKit.Box(row, "share");
                UiKit.Place(card, x, nameH + rem * 0.2f, bubbleW, bodyH);
                // T345 — 정본 3389 `.chat-share-card { border-radius: 0 }`(«원본 카드는 모서리가 각져 있어» · 표 `chat_share_card_r_rem` = 0 → 각진 테·면 · 전엔 .6rem)
                RadiusUi.Outlined(card, "face", "pp_panel", "chat_share_card_r_rem", PopupKit.Line);
                string winName = m.Win ? m.MyName : m.OppName, loseName = m.Win ? m.OppName : m.MyName;
                string winAv = m.Win ? m.MyAvatar : m.OppAvatar, loseAv = m.Win ? m.OppAvatar : m.MyAvatar;
                string winCp = PopupKit.Fmt(m.Win ? m.MyCp : m.OppCp), loseCp = PopupKit.Fmt(m.Win ? m.OppCp : m.MyCp);
                // T396 6회차 — 정본 3409 `.chat-share-side small:last-child { color: #ff880f }` · 3425 `.chat-share-label { color: #ff880f }`:
                //   **양쪽** 전투력 수와 «승리» 라벨이 같은 주황(+ 검정 키라인)이다(주석 «주황 글자의 테가 갈리면 안 된다»). 전엔 이긴 쪽을 초록(chat_share_win #35c04f) ·
                //   진 쪽을 회색(chat_share_lose #8a8a8a)으로 찍었다 — 정본엔 그런 갈래가 없다(`.lose` 는 바탕 #cecece 만). 값은 카탈로그 chat_name(#ff880f · 같은 리터럴).
                // T377 11회차 — 정본은 **쪽마다 제 면**을 준다: 3397 `.chat-share-side { background: #39ab36 }`(이긴 쪽 초록) ·
                //   3412 `.chat-share-side.lose { background: #cecece }`(진 쪽은 «말풍선과 같은 회색이라 목록에 녹아든다»).
                //   카드 자신은 3389 `background: transparent` 다. 클론은 쪽 면을 **하나도 안 칠하고** 카드 한 장(`pp_panel` #efefef)으로 덮고 있었다 —
                //   원작 shot-043500 에 초록 #39ab36 이 12,308화소인데 클론 값 #35c04f 는 0화소다.
                Side(card, "win", 0f, bubbleW * 0.5f, bodyH, winAv, winName, winCp, "chat_name", "승리", "chat_share_win");
                Side(card, "lose", bubbleW * 0.5f, bubbleW * 0.5f, bodyH, loseAv, loseName, loseCp, "chat_name", null, "chat_share_lose");
                // T132 2회차 — 정본 ui.js 5271 `<span class="chat-share-cam">${IconGen.img('chatcam')}</span>` · style.css 3433~3438:
                // 카드 오른쪽 위 **밖으로 걸치는** 정사각 배지(.0381W · top −.008W · right −.030W · 앱 폭 배수 = StaticIconsUi `_aw`).
                // 정본은 카드에 overflow:hidden 을 일부러 안 준다(3392 주석 «주면 배지가 통째로 잘린다») — 클론 카드도 마스크가 없다.
                float camW = StaticIconsUi.L("chat_share_cam_w_aw") * w;
                Image cam = PopupKit.IconOr(card, "cam", "chatcam");
                UiKit.Anchor(cam.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-StaticIconsUi.L("chat_share_cam_right_aw") * w, -StaticIconsUi.L("chat_share_cam_top_aw") * w), camW, camW);
            }
            else
            {
                RectTransform bubble = UiKit.Box(row, "bubble");
                UiKit.Place(bubble, x, nameH + rem * 0.2f, bubbleW, bodyH);
                // T345 — 정본 3371 `.chat-bubble { border-radius: .42rem }`(표 `chat_bubble_r_rem` · 전엔 .6rem)
                // T377 11회차 — 정본 3372 `.chat-bubble { background: #cecece }` **한 줄**이 모든 말풍선의 면이다.
                //   `style.css` 전체에서 `.mine` 규칙은 3324(이름 색) 하나뿐이고 말풍선 면을 가르는 줄은 0 — 클론의 파란 «내 말풍선»(#dbe9ff)은
                //   정본에 없는 갈래였다(원작 shot-043500 에 그 색 0화소 · #cecece 는 100,343화소). 양쪽 다 같은 키를 부른다(결정 747).
                RadiusUi.Rounded(bubble, "bg", "chat_bubble", "chat_bubble_r_rem");
                TextMeshProUGUI t = UiKit.Text(bubble, "text", TextKind.Sub, m.Text ?? string.Empty, "pp_ink", TextAlignmentOptions.Left);
                // 정본 .chat-bubble { -webkit-text-stroke: .5px currentColor } — `currentColor` 라 글자색과 같은 키라인이다(색 키가 아니라 제 색).
                UiKit.OutlinePx(t, "pp_ink", KeylineUi.Px("chat_bubble"));
                t.outlineColor = t.color;
                t.textWrappingMode = TextWrappingModes.Normal;
                LineHeight.Apply(t, "chat_bubble_lh_w");   // T354 4회차 — 정본 3380 `.chat-bubble { line-height: calc(var(--app-w) * .0351) }` · 앱 폭 키의 유일한 자리(글자 크기로 나눠 배수가 된다)
                t.rectTransform.offsetMin = new Vector2(rem * 0.5f, rem * 0.2f);
                t.rectTransform.offsetMax = new Vector2(-rem * 0.5f, -rem * 0.2f);
            }
        }

        /// <summary>
        /// T379 — 정본 style.css 3396 `.chat-share-side { display:flex; flex-direction:column; align-items:center; gap: calc(var(--app-w) * .010);
        /// padding: calc(var(--app-w) * .014) .3rem calc(var(--app-w) * .016) }`: **세로 3단 · 가운데 정렬** — 아바타 타일(3402 `.icon-circle.sm` .0882W 정사각) → `<small>` 이름 → `<small>` 전투력(power 아이콘 + 수).
        /// «승리» 라벨(3424 `.chat-share-label`)은 절대 배치라 세로를 안 먹고 타일 하단 모서리에 걸터앉는다(`left:50%; bottom: calc(var(--app-w) * -.020)` = 라벨 아래끝이 타일 아래끝보다 .020W 아래).
        /// 치수는 전부 앱 폭 배수(표 StaticIconsUi `_aw` · 정본 주석 «가로 치수라 rem 금지» 갈래). 종전(T25)엔 «아바타 왼쪽 + 글 오른쪽» 가로 배치였고 라벨은 한 쪽 상자의 바닥 왼쪽이었다.
        /// </summary>
        private static void Side(RectTransform card, string name, float x, float w, float h, string avatar, string who, string cp, string colorKey, string label, string faceKey)
        {
            float rem = PopupKit.Rem, aw = UiKit.RefW;
            RectTransform side = UiKit.Box(card, name);
            UiKit.Place(side, x, 0f, w, h);
            // T377 11회차 — 쪽 제 면(정본 3397·3412). 첫 자식이라 아바타·글자 뒤에 깔린다. 정본 `border-radius: 0` 이라 각진 판이다.
            UiKit.Panel(side, "bg", faceKey);
            float tile = StaticIconsUi.L("chat_share_tile_aw") * aw;
            float padT = StaticIconsUi.L("chat_share_side_pad_top_aw") * aw, padX = StaticIconsUi.L("chat_share_side_pad_x_rem") * rem;
            float gap = StaticIconsUi.L("chat_share_side_gap_aw") * aw;
            float lineH = PopupKit.FontSize(TextKind.Sub) * 1.2f;
            float cx = w * 0.5f, innerW = w - padX * 2f;
            float y = padT;
            // 1단 — 아바타 타일(정본 3401 `.icon-circle.sm { border-radius: .28rem }` · 표 chat_share_avatar_r_rem · T345)
            RectTransform tileRt = PopupKit.Avatar(side, "avatar", tile, avatar, RadiusUi.Px("chat_share_avatar_r_rem"));
            UiKit.Place(tileRt, cx - tile * 0.5f, y, tile, tile);
            if (label != null)
            {
                TextMeshProUGUI lb = UiKit.Text(side, "label", TextKind.Sub, label, colorKey);
                lb.fontStyle = FontStyles.Bold;
                WrapUi.Apply(lb, "chat_share_label");   // T361 5회차 — 정본 white-space 표(WrapUi.json) 3425 `.chat-share-label { nowrap }`
                UiKit.OutlinePx(lb, "pp_line", KeylineUi.Px("chat_share_label"));   // 정본 .chat-share-label { var(--ol2) #000 }
                float lbW = Mathf.Max(lb.preferredWidth + rem * 0.4f, tile);
                float lbBottom = y + tile + StaticIconsUi.L("chat_share_label_bottom_aw") * aw;
                UiKit.Place(lb.rectTransform, cx - lbW * 0.5f, lbBottom - lineH, lbW, lineH);
                lb.transform.SetAsLastSibling();   // 타일 위에 겹쳐 그린다(오버레이)
            }
            y += tile + gap;
            // 2단 — 이름(정본 `.chat-share-side small { font-weight: 800 }` · 색은 3399 `color: #000`)
            TextMeshProUGUI n = UiKit.Text(side, "name", TextKind.Sub, who ?? string.Empty, "pp_ink");
            n.fontStyle = FontStyles.Bold;
            UiKit.Place(n.rectTransform, padX, y, innerW, lineH);
            y += lineH + gap;
            // 3단 — T99 — 정본 `ui.js` 5264·5269: `<small>${IconGen.img('power')} ${U.fmt(cp)}</small>` — «⚔» 글자가 아니라 `power` 아이콘 + 수(아이콘 한 칸은 글자 크기의 정사각).
            //        아이콘과 수를 한 묶음으로 재어 가운데에 놓는다(정본 align-items:center · 글자 폭은 TMP preferredWidth).
            float cpIco = PopupKit.FontSize(TextKind.Sub), cpGap = rem * 0.15f;
            TextMeshProUGUI c = UiKit.Text(side, "cp", TextKind.Sub, cp, colorKey, TextAlignmentOptions.Left);
            c.fontStyle = FontStyles.Bold;
            UiKit.OutlinePx(c, "pp_line", KeylineUi.Px("chat_share_small"));   // 정본 .chat-share-side small:last-child { var(--ol2) #000 }
            float cpTextW = Mathf.Clamp(c.preferredWidth, 1f, innerW - cpIco - cpGap);
            float groupW = cpIco + cpGap + cpTextW, gx = cx - groupW * 0.5f;
            Image cpI = PopupKit.IconOr(side, "cp-ico", "power");
            UiKit.Place(cpI.rectTransform, gx, y + (lineH - cpIco) * 0.5f, cpIco, cpIco);
            UiKit.Place(c.rectTransform, gx + cpIco + cpGap, y, cpTextW, lineH);
        }

        /// <summary>T379 — 공유 카드 한 쪽의 세로 쌓임 = 위 패딩 + 타일 + 틈 + 이름 줄 + 틈 + 전투력 줄 + 아래 패딩(정본 3396~3398 · 라벨은 절대 배치라 세로를 안 먹는다).
        /// 정본 실측은 95px(18.8%W)이고 클론은 §1 글자 하한(Sub 36)으로 줄이 더 높아 그보다 큰 것이 맞다 — 값을 박지 않고 쌓임으로 셈한다.</summary>
        public static float ShareBodyH()
        {
            float aw = UiKit.RefW, lineH = PopupKit.FontSize(TextKind.Sub) * 1.2f;
            return StaticIconsUi.L("chat_share_side_pad_top_aw") * aw + StaticIconsUi.L("chat_share_tile_aw") * aw
                 + StaticIconsUi.L("chat_share_side_gap_aw") * aw * 2f + lineH * 2f + StaticIconsUi.L("chat_share_side_pad_bot_aw") * aw;
        }

        /// <summary>말풍선 줄 수 어림(글자당 폭 ≈ 0.6em · 한글은 1em) — 레이아웃 전에 행 높이를 잡기 위한 것.</summary>
        private static int EstimateLines(string text, float width)
        {
            if (string.IsNullOrEmpty(text)) return 1;
            float em = PopupKit.FontSize(TextKind.Sub);
            float x = 0f;
            int lines = 1;
            foreach (char ch in text)
            {
                float cw = ch == '\n' ? width : (ch > 0x2E7F ? em : em * 0.58f);
                if (ch == '\n' || x + cw > width - PopupKit.Rem) { lines++; x = 0f; }
                if (ch != '\n') x += cw;
            }
            return lines;
        }
    }
}
