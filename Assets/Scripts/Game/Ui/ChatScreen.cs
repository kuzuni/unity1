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

        public static void Open(MetaHost h)
        {
            h.Chat.Ensure(h.ChatState, h.NowMs);
            Popup p = h.Popups.Show(Name);
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
            scroll = listBox.GetComponent<ScrollRect>();

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
            UiKit.Rounded(brt, "line", "pp_line", bh * 0.3f);
            Image bface = UiKit.Rounded(brt, "face", "pp_red", bh * 0.3f - PopupKit.Line3);
            PopupKit.Inset(bface.rectTransform, PopupKit.Line3);
            Image tri = PopupKit.Tri(brt, "tri", "stage_ink");
            float tw = UiKit.RefH * 0.0179f;
            UiKit.Anchor(tri.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, tw, tw);

            float ix = rem * 0.5f + bw + rem * 0.5f;
            RectTransform ibox = UiKit.Box(bar, "input");
            UiKit.Place(ibox, ix, (inputH - bh * 1.15f) * 0.5f, w - ix - rem * 0.5f, bh * 1.15f);
            UiKit.Rounded(ibox, "line", "pp_line", bh * 0.3f);
            Image iface = UiKit.Rounded(ibox, "face", "pp_panel", bh * 0.3f - PopupKit.Line);
            PopupKit.Inset(iface.rectTransform, PopupKit.Line);
            iface.raycastTarget = true;
            RectTransform viewport = UiKit.Box(ibox, "viewport");
            PopupKit.Inset(viewport, rem * 0.4f);
            viewport.gameObject.AddComponent<RectMask2D>();
            TextMeshProUGUI txt = UiKit.Text(viewport, "text", TextKind.Sub, string.Empty, "pp_ink", TextAlignmentOptions.Left);
            TextMeshProUGUI ph = UiKit.Text(viewport, "placeholder", TextKind.Sub, "메시지 보내기...", "pp_muted", TextAlignmentOptions.Left);
            input = ibox.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = viewport;
            input.textComponent = txt;
            input.placeholder = ph;
            input.characterLimit = Chat.MaxPlayerTextLen;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.onSubmit.AddListener(_ => Send(h));

            RenderList(h);
        }

        public static void Close(MetaHost h)
        {
            h.Popups.Hide(Name);
            input = null;
            list = null;
            scroll = null;
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
            for (int i = list.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(list.GetChild(i).gameObject);
            List<ChatMessage> msgs = h.ChatState.Messages ?? new List<ChatMessage>();
            foreach (ChatMessage m in msgs) Row(list, m);
            if (scroll != null) scroll.verticalNormalizedPosition = 0f;
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
            nm.rectTransform.offsetMax = new Vector2(-rem * 3f, 0f);
            TextMeshProUGUI gender = UiKit.Text(nameLine, "gender", TextKind.Sub, m.Gender ?? string.Empty, "pp_muted", TextAlignmentOptions.Right);
            gender.rectTransform.offsetMin = new Vector2(bubbleW - rem * 3f, 0f);
            gender.rectTransform.offsetMax = new Vector2(-rem * 1.8f, 0f);
            TextMeshProUGUI time = UiKit.Text(nameLine, "time", TextKind.Sub, Time(m.At), "chat_time", TextAlignmentOptions.Right);
            time.fontStyle = FontStyles.Bold;
            time.rectTransform.offsetMin = new Vector2(bubbleW - rem * 3.2f, 0f);
            time.rectTransform.offsetMax = new Vector2(rem * 3.2f, 0f);

            if (share)
            {
                RectTransform card = UiKit.Box(row, "share");
                UiKit.Place(card, x, nameH + rem * 0.2f, bubbleW, bodyH);
                PopupKit.Outlined(card, "face", "pp_panel", rem * 0.6f, PopupKit.Line);
                string winName = m.Win ? m.MyName : m.OppName, loseName = m.Win ? m.OppName : m.MyName;
                string winAv = m.Win ? m.MyAvatar : m.OppAvatar, loseAv = m.Win ? m.OppAvatar : m.MyAvatar;
                string winCp = PopupKit.Fmt(m.Win ? m.MyCp : m.OppCp), loseCp = PopupKit.Fmt(m.Win ? m.OppCp : m.MyCp);
                Side(card, "win", 0f, bubbleW * 0.5f, bodyH, winAv, winName, winCp, "chat_share_win", "승리");
                Side(card, "lose", bubbleW * 0.5f, bubbleW * 0.5f, bodyH, loseAv, loseName, loseCp, "chat_share_lose", null);
            }
            else
            {
                RectTransform bubble = UiKit.Box(row, "bubble");
                UiKit.Place(bubble, x, nameH + rem * 0.2f, bubbleW, bodyH);
                UiKit.Rounded(bubble, "bg", m.Mine ? "chat_bubble_mine" : "chat_bubble", rem * 0.6f);
                TextMeshProUGUI t = UiKit.Text(bubble, "text", TextKind.Sub, m.Text ?? string.Empty, "pp_ink", TextAlignmentOptions.Left);
                t.textWrappingMode = TextWrappingModes.Normal;
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
            RectTransform tile = PopupKit.Avatar(side, "avatar", av, avatar, av * 0.5f);
            UiKit.Place(tile, rem * 0.4f, (h - av) * 0.5f, av, av);
            if (label != null)
            {
                TextMeshProUGUI lb = UiKit.Text(side, "label", TextKind.Sub, label, colorKey);
                lb.fontStyle = FontStyles.Bold;
                UiKit.Place(lb.rectTransform, 0f, h - PopupKit.FontSize(TextKind.Sub) * 1.2f, av + rem * 0.8f, PopupKit.FontSize(TextKind.Sub) * 1.2f);
            }
            float tx = rem * 0.4f + av + rem * 0.3f;
            TextMeshProUGUI n = UiKit.Text(side, "name", TextKind.Sub, who ?? string.Empty, "pp_ink", TextAlignmentOptions.Left);
            n.fontStyle = FontStyles.Bold;
            UiKit.Place(n.rectTransform, tx, h * 0.15f, w - tx, h * 0.35f);
            TextMeshProUGUI c = UiKit.Text(side, "cp", TextKind.Sub, "⚔ " + cp, colorKey, TextAlignmentOptions.Left);
            c.fontStyle = FontStyles.Bold;
            UiKit.Place(c.rectTransform, tx, h * 0.5f, w - tx, h * 0.35f);
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
