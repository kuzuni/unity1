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
            listBox.offsetMax = new Vector2(0f, -UiKit.H("topbar_h") * 0.3f);
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
            UiKit.Panel(bar, "bg", "pp_paper");
            UiKit.Line(bar, "line", "pp_line", PopupKit.Line, true);
            float bw = w * 0.0721f, bh = w * 0.0581f;
            Button back = UiKit.Button(bar, "close", () => Close(h));
            RectTransform brt = back.GetComponent<RectTransform>();
            UiKit.Place(brt, rem * 0.5f, (inputH - bh) * 0.5f, bw, bh);
            // T345 — 정본 3270 `.chat-input-bar .btn.round { border-radius: .35rem }`(표 `chat_round_btn_r_rem` · 전엔 높이×.3 = .52rem 이었다)
            float backR = RadiusUi.Px("chat_round_btn_r_rem");
            UiKit.Rounded(brt, "line", "pp_line", backR);
            Image bface = UiKit.Rounded(brt, "face", "pp_red", Mathf.Max(1f, backR - PopupKit.Line3));
            PopupKit.Inset(bface.rectTransform, PopupKit.Line3);
            Image tri = PopupKit.Tri(brt, "tri", "stage_ink");
            float tw = UiKit.RefH * 0.0179f;
            UiKit.Anchor(tri.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, tw, tw);

            float ix = rem * 0.5f + bw + rem * 0.5f;
            RectTransform ibox = UiKit.Box(bar, "input");
            UiKit.Place(ibox, ix, (inputH - bh * 1.15f) * 0.5f, w - ix - rem * 0.5f, bh * 1.15f);
            // T345 — 정본 3449 `.chat-input-bar input { border-radius: .3rem }`(표 `chat_input_r_rem` · 전엔 높이×.3 = .52rem)
            float inputR = RadiusUi.Px("chat_input_r_rem");
            UiKit.Rounded(ibox, "line", "pp_line", inputR);
            Image iface = UiKit.Rounded(ibox, "face", "pp_panel", Mathf.Max(1f, inputR - PopupKit.Line));
            PopupKit.Inset(iface.rectTransform, PopupKit.Line);
            iface.raycastTarget = true;
            RectTransform viewport = UiKit.Box(ibox, "viewport");
            PopupKit.Inset(viewport, rem * 0.4f);
            viewport.gameObject.AddComponent<RectMask2D>();
            TextMeshProUGUI txt = UiKit.Text(viewport, "text", TextKind.Sub, string.Empty, "pp_ink", TextAlignmentOptions.Left);
            TextMeshProUGUI ph = UiKit.Text(viewport, "placeholder", TextKind.Sub, "메시지 보내기...", "pp_muted", TextAlignmentOptions.Left);
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
            float bodyH = share ? rem * 4.2f : Mathf.Max(nameH, EstimateLines(m.Text, bubbleW) * PopupKit.FontSize(TextKind.Sub) * 1.25f + rem * 0.6f);
            float rowH = nameH + rem * 0.2f + bodyH;
            RectTransform row = PopupKit.Item(parent, "msg", -1f, rowH);
            string avatar = share ? m.MyAvatar : m.Avatar;
            RectTransform tile = PopupKit.Avatar(row, "avatar", av, avatar, rem * 0.4f);
            UiKit.Place(tile, 0f, 0f, av, av);
            float x = av + rem * 0.4f;
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
                Side(card, "win", 0f, bubbleW * 0.5f, bodyH, winAv, winName, winCp, "chat_share_win", "승리");
                Side(card, "lose", bubbleW * 0.5f, bubbleW * 0.5f, bodyH, loseAv, loseName, loseCp, "chat_share_lose", null);
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
                RadiusUi.Rounded(bubble, "bg", m.Mine ? "chat_bubble_mine" : "chat_bubble", "chat_bubble_r_rem");
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

        private static void Side(RectTransform card, string name, float x, float w, float h, string avatar, string who, string cp, string colorKey, string label)
        {
            float rem = PopupKit.Rem;
            RectTransform side = UiKit.Box(card, name);
            UiKit.Place(side, x, 0f, w, h);
            float av = rem * 2f;
            // T345 — 정본 3401 `.chat-share-side .icon-circle.sm { border-radius: .28rem }`(.icon-circle 의 50% 를 덮는 둥근 네모 · 표 `chat_share_avatar_r_rem` · 전엔 폭×.5 = 거의 원)
            RectTransform tile = PopupKit.Avatar(side, "avatar", av, avatar, RadiusUi.Px("chat_share_avatar_r_rem"));
            UiKit.Place(tile, rem * 0.4f, (h - av) * 0.5f, av, av);
            if (label != null)
            {
                TextMeshProUGUI lb = UiKit.Text(side, "label", TextKind.Sub, label, colorKey);
                lb.fontStyle = FontStyles.Bold;
                UiKit.OutlinePx(lb, "pp_line", KeylineUi.Px("chat_share_label"));   // 정본 .chat-share-label { var(--ol2) #000 }
                UiKit.Place(lb.rectTransform, 0f, h - PopupKit.FontSize(TextKind.Sub) * 1.2f, av + rem * 0.8f, PopupKit.FontSize(TextKind.Sub) * 1.2f);
            }
            float tx = rem * 0.4f + av + rem * 0.3f;
            TextMeshProUGUI n = UiKit.Text(side, "name", TextKind.Sub, who ?? string.Empty, "pp_ink", TextAlignmentOptions.Left);
            n.fontStyle = FontStyles.Bold;
            UiKit.Place(n.rectTransform, tx, h * 0.15f, w - tx, h * 0.35f);
            // T99 — 정본 `ui.js` 5264·5269: `<small>${IconGen.img('power')} ${U.fmt(cp)}</small>` — 전투력은 «⚔» 글자가 아니라
            // `power` 아이콘 + 수다(글꼴에 ⚔ 가 없어 □ 로 찍히던 자리 · HUD `cp` 줄·리그 도전 행과 같은 길). 아이콘 한 칸은 글자 크기의 정사각.
            float cpH = h * 0.35f, cpIco = Mathf.Min(PopupKit.FontSize(TextKind.Sub), cpH);
            Image cpI = PopupKit.IconOr(side, "cp-ico", "power");
            UiKit.Place(cpI.rectTransform, tx, h * 0.5f + (cpH - cpIco) * 0.5f, cpIco, cpIco);
            TextMeshProUGUI c = UiKit.Text(side, "cp", TextKind.Sub, cp, colorKey, TextAlignmentOptions.Left);
            c.fontStyle = FontStyles.Bold;
            UiKit.OutlinePx(c, "pp_line", KeylineUi.Px("chat_share_small"));   // 정본 .chat-share-side small:last-child { var(--ol2) #000 }
            UiKit.Place(c.rectTransform, tx + cpIco + rem * 0.15f, h * 0.5f, w - tx - cpIco - rem * 0.15f, cpH);
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
