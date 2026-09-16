using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 전투 HUD(ROUTINE T18 · 원작 ui.js renderTopBar · updateStageLabel · updateWavePips · #chat-preview).
    /// 상단바(프로필 카드 + 코인·젬 알약) · 스테이지 라벨 · 웨이브 노드 · 채팅 미리보기(말풍선 + «99» 뱃지 + 이름/메시지 두 줄).
    /// 규칙을 계산하지 않는다 — 뒤 작업(T7 전투 · T13 세이브 · T22 채팅)이 <c>Set*</c> 로 글자를 준다. 부팅 첫 프레임은 카탈로그 boot 값.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        public static Hud Instance { get; private set; }

        private sealed class Pip
        {
            public RectTransform Slot;
            public Image Ring;
            public Image Fill;
        }

        private TextMeshProUGUI nickname;
        private TextMeshProUGUI cp;
        private TextMeshProUGUI coins;
        private TextMeshProUGUI gems;
        private TextMeshProUGUI stage;
        private TextMeshProUGUI chat;
        private TextMeshProUGUI chatName;
        private RectTransform pipsRow;
        private RectTransform track;
        private RectTransform avatarTile;
        private Image avatarPortrait;
        private readonly List<Pip> pips = new List<Pip>();

        /// <summary>프로필 카드를 눌렀을 때(프로필 팝업 · 뒤 작업).</summary>
        public Button ProfileButton { get; private set; }
        /// <summary>채팅 줄을 눌렀을 때(전체화면 채팅 · T22).</summary>
        public Button ChatButton { get; private set; }
        /// <summary>코인 알약의 초록 «+»(원작 `curIcoPlus` — 상점 열기). 배선은 <see cref="MetaHost"/>.</summary>
        public Button CoinPlusButton { get; private set; }
        /// <summary>젬 알약의 초록 «+».</summary>
        public Button GemPlusButton { get; private set; }

        public string Nickname { get { return nickname.text; } }
        public string StageLabel { get { return stage.text; } }
        public int WaveCount { get { return pips.Count; } }

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <param name="hudLayer">앱 상단~장비 시트 위까지의 층(기준 캔버스 단위)</param>
        /// <param name="chatBand">채팅 미리보기 띠</param>
        public void Build(RectTransform hudLayer, RectTransform chatBand)
        {
            UiCatalog cat = UiCatalog.Instance;
            UiCatalog.BootEntry boot = cat.Boot;
            float w = UiKit.RefW;
            float rem = UiKit.H("rem_h");
            float line = UiKit.L("line_px");

            // ---- 상단바 밴드 ----
            RectTransform bar = UiKit.Box(hudLayer, "topbar");
            float barH = UiKit.H("topbar_h");
            UiKit.Place(bar, 0f, 0f, w, barH);
            // 정본 #topbar(style.css 7905) `0 .18rem .4rem rgba(0,0,0,.34)` — 상단바가 3D 화면 위에 한 겹 떠 있다.
            // 자리를 잡은 **뒤**에 부른다(굽는 판이 상자 크기를 읽는다).
            UiShadow.Drop(bar, "topbar_drop", 0f);
            UiKit.Panel(bar, "bg", "topbar_bg");
            UiKit.Line(bar, "line", "topbar_line", line, false);
            float padX = UiKit.W("topbar_pad_x");

            // 프로필 카드 (아바타 타일은 알약에 걸쳐 위아래로 넘친다 — 원작 실측)
            float cardW = UiKit.W("card_min_w");
            float cardH = UiKit.H("card_h");
            float cardR = UiKit.H("card_radius");
            ProfileButton = UiKit.Button(bar, "profile-card", null);
            RectTransform card = ProfileButton.GetComponent<RectTransform>();
            UiKit.Place(card, padX, (barH - cardH) * 0.5f, cardW, cardH);
            UiKit.Rounded(card, "outline", "topbar_line", cardR);
            Image cardBg = UiKit.Rounded(card, "bg", "card_bg", cardR - line);
            Inset(cardBg.rectTransform, line);

            float av = UiKit.W("avatar");
            float avR = UiKit.H("avatar_radius");
            Image avRing = UiKit.Rounded(card, "avatar", "pp_line", avR);
            UiKit.Place(avRing.rectTransform, 0f, (cardH - av) * 0.5f, av, av);
            avatarTile = avRing.rectTransform;
            Image avFace = UiKit.Rounded(avRing.transform, "face", "avatar_bg", avR - line);
            Inset(avFace.rectTransform, line);

            float tx = av + UiKit.W("card_gap");
            float tw = cardW - tx - UiKit.W("card_pad_r");
            nickname = UiKit.Text(card, "nickname", TextKind.Sub, boot.nickname, "ink", TextAlignmentOptions.Left);
            nickname.fontStyle = FontStyles.Bold;
            UiKit.Place(nickname.rectTransform, tx, 0f, tw, cardH * 0.5f);

            RectTransform cpRow = UiKit.Box(card, "cp");
            UiKit.Place(cpRow, tx, cardH * 0.5f, tw, cardH * 0.5f);
            float ico = cardH * 0.5f;
            Image power = UiKit.Icon(cpRow, "ico", "power");
            UiKit.Place(power.rectTransform, 0f, 0f, ico, ico);
            cp = UiKit.Text(cpRow, "value", TextKind.Sub, "0", "cp", TextAlignmentOptions.Left);
            cp.fontStyle = FontStyles.Bold;
            UiKit.Place(cp.rectTransform, ico + rem * 0.2f, 0f, tw - ico - rem * 0.2f, ico);

            // 재화 알약 (고정 폭 · 아이콘 좌단 · 숫자 우측 정렬)
            float pillW = UiKit.W("pill_w");
            float pillH = UiKit.H("pill_h");
            float gap = UiKit.W("pill_gap");
            float inset = UiKit.W("pill_inset_r");
            float py = (barH - pillH) * 0.5f;
            coins = Pill(bar, "pill-coin", "coin", "coin", w - inset - pillW * 2f - gap, py, pillW, pillH, rem);
            gems = Pill(bar, "pill-gem", "gem", "gem", w - inset - pillW, py, pillW, pillH, rem);
            CoinPlusButton = PlusBadge(bar.Find("pill-coin"), rem);
            GemPlusButton = PlusBadge(bar.Find("pill-gem"), rem);

            // ---- 스테이지 라벨 ----
            stage = UiKit.Text(hudLayer, "stage-label", TextKind.Body, boot.stage, "stage_ink", TextAlignmentOptions.Center);
            stage.fontStyle = FontStyles.Bold;
            UiKit.Place(stage.rectTransform, 0f, UiKit.H("stage_top"), w, UiKit.H("stage_h"));
            UiKit.Outline(stage, "stage_outline", 0.25f);

            // ---- 웨이브 노드 (검정 키라인 노드를 흰 트랙으로 이은 위젯) ----
            pipsRow = UiKit.Box(hudLayer, "wave-pips");
            SetWaves(boot.waves, 1, -1);

            // ---- 채팅 미리보기 ----
            BuildChat(chatBand, rem);
        }

        private static void Inset(RectTransform rt, float px)
        {
            rt.offsetMin = new Vector2(px, px);
            rt.offsetMax = new Vector2(-px, -px);
        }

        private TextMeshProUGUI Pill(Transform parent, string name, string iconKey, string colorKey, float x, float y, float pw, float ph, float rem)
        {
            RectTransform pill = UiKit.Box(parent, name);
            UiKit.Place(pill, x, y, pw, ph);
            RadiusUi.Rounded(pill, "bg", "card_bg", "currency_pill_r_rem");   // 정본 115 `.currency-pills .pill { border-radius: 1rem }` — 값은 맞았지만 코드에 박혀 있었다(T345 18회차)
            float ico = UiKit.H("pill_icon");
            float padL = UiKit.W("pill_pad_l");
            float padR = UiKit.W("pill_pad_r");
            Image img = UiKit.Icon(pill, "ico", iconKey);
            UiKit.Place(img.rectTransform, (padL - ico) * 0.5f, (ph - ico) * 0.5f, ico, ico);
            TextMeshProUGUI t = UiKit.Text(pill, "value", TextKind.Sub, "0", colorKey, TextAlignmentOptions.Right);
            t.fontStyle = FontStyles.Bold;
            WrapUi.Apply(t, "currency_pills_pill");   // T361 4회차 — 정본 white-space 표(WrapUi.json) 115 `.currency-pills .pill { nowrap }` — 런 731 에서 «27.1m» 이 «27.1 / m» 으로 접혔다
            UiKit.Place(t.rectTransform, padL, 0f, pw - padL - padR, ph);
            return t;
        }

        /// <summary>
        /// 재화 알약 아이콘의 오른쪽 아래 초록 «+» — 원작 `curIcoPlus`(`ui.js:1285`)가 코인·젬 알약 둘 다에 다는 상점 열기 버튼이다.
        /// 치수는 정본 `.pill-plus`(`style.css:131` · `.74rem` 정사각 · `right:-.38rem` · `bottom:-.11rem`) 그대로 rem 배수로.
        /// 초록 원은 T31 아틀라스의 `plus` 아이콘 그림이 쥔다(원작도 그렇다 — CSS 는 판을 안 그린다).
        /// </summary>
        private Button PlusBadge(Transform pill, float rem)
        {
            if (pill == null) return null;
            float ico = UiKit.H("pill_icon");
            float padL = UiKit.W("pill_pad_l");
            float ph = UiKit.H("pill_h");
            float s = rem * 0.74f;
            // 아이콘 상자의 오른쪽 아래 모서리에 걸친다(정본 offset).
            float icoX = (padL - ico) * 0.5f;
            float icoY = (ph - ico) * 0.5f;
            Button b = UiKit.Button(pill, "pill-plus", null);
            UiKit.Place(b.GetComponent<RectTransform>(), icoX + ico - s + rem * 0.38f, icoY + ico - s + rem * 0.11f, s, s);
            Image img = UiKit.Icon(b.transform, "ico", "plus");
            UiKit.Place(img.rectTransform, 0f, 0f, s, s);
            return b;
        }

        private void BuildChat(RectTransform band, float rem)
        {
            float bandH = (UiKit.L("tabbar_top") - UiKit.L("chat_top")) * UiKit.RefH;
            float padX = UiKit.W("chat_pad_x");
            float av = UiKit.H("chat_avatar");
            ChatButton = UiKit.Button(band, "hit", null);
            // T132 — 정본 ui.js 5294 `<span class="chat-preview-avatar">${IconGen.img('chatbubble')}…` : 아틀라스에 «chat» 키는 없어
            // 카탈로그 스프라이트로 물러나던 자리 — 정본이 그리는 chatbubble 로.
            Image ico = UiKit.Icon(band, "chat-preview-avatar", "chatbubble");
            UiKit.Place(ico.rectTransform, padX, (bandH - av) * 0.5f, av, av);

            // 말풍선 오른쪽 위 «99» 뱃지 — 정본 `.chat-preview-badge`(top/right −.42rem · 빨강 판 + 흰 글자 + 테).
            float badgeH = rem * 0.72f;
            float badgeW = rem * 1.02f;
            Image badgeLine = UiKit.Rounded(band, "chat-preview-badge", "pp_line", badgeH * 0.5f);
            UiKit.Place(badgeLine.rectTransform,
                padX + av + rem * 0.42f - badgeW,
                (bandH - av) * 0.5f - rem * 0.42f, badgeW, badgeH);
            Image badgeBg = UiKit.Rounded(badgeLine.transform, "bg", "pp_red", badgeH * 0.5f - UiKit.L("line_px"));
            Inset(badgeBg.rectTransform, UiKit.L("line_px"));
            TextMeshProUGUI badge = UiKit.Text(badgeLine.transform, "n", TextKind.Sub, ChatBadgeText, "white", TextAlignmentOptions.Center);
            badge.fontStyle = FontStyles.Bold;
            UiKit.Place(badge.rectTransform, 0f, 0f, badgeW, badgeH);

            // 이름 줄 / 메시지 줄 두 줄 — 정본 `.chat-preview-lines`(세로 쌓기 · 이름은 굵게).
            float tx = padX + av + rem * 0.4f;
            float tw = UiKit.RefW - tx - padX;
            // 정본 `.chat-preview-name`·`.chat-preview-msg` 는 **`white-space: nowrap`** 이다 — 한 줄에 가로 말줄임.
            // ⚠ `textWrappingMode` 를 안 끄면 줄바꿈이 켜진 채라 긴 메시지가 두 줄이 되고, 반쪽 띠 높이에 안 맞아
            //   TMP 가 Ellipsis 로 **글리프를 통째로 버린다**(글자는 들어 있는데 화면엔 아무것도 안 나온다 ·
            //   런 159 실측: `textInfo.characterCount == 0` · 그것이 T91 «채팅 띠가 비었다» 의 뿌리였다).
            // 정본 `#chat-preview { padding: .3rem .7rem }` 의 세로 여백 — 없으면 이름 줄이 띠 윗변에 닿는다(런 170 캡처 실측).
            float padY = rem * 0.3f;
            // 두 줄을 «띠 높이의 절반» 칸에 각각 담으면 안 된다: 그 칸(51.6)이 글꼴 줄높이보다 조금이라도 낮으면
            // TMP 가 Ellipsis 규칙으로 **글리프를 통째로 버려** 글자가 통째로 사라진다(런 159·163 실측 characterCount 0).
            // 그래서 칸은 **띠 전체 높이**로 주고 위/아래 정렬로 두 줄 자리를 낸다 — 글꼴 줄높이가 바뀌어도 안 무너진다.
            chatName = UiKit.Text(band, "chat-preview-name", TextKind.Sub, string.Empty, "chat_name", TextAlignmentOptions.TopLeft);
            chatName.fontStyle = FontStyles.Bold;
            UiKit.TextShadow(chatName, "chat_preview_name");   // T333 13회차 — 정본 8069 `#chat-preview .chat-preview-name { text-shadow: 0 1px 1px rgba(0,0,0,.5) }`(8392 묶음보다 특이도가 높다)
            WrapUi.Apply(chatName, "chat_preview_name");   // T361 4회차 — 정본 white-space 표(WrapUi.json) 3252 `.chat-preview-name { nowrap }`(전엔 박힘)
            chatName.overflowMode = TextOverflowModes.Ellipsis;     // 정본 `text-overflow: ellipsis`(가로)
            UiKit.Place(chatName.rectTransform, tx, padY, tw, bandH - padY * 2f);
            chat = UiKit.Text(band, "chat-preview-msg", TextKind.Sub, string.Empty, "chat_ink", TextAlignmentOptions.BottomLeft);
            WrapUi.Apply(chat, "chat_preview_msg");   // T361 4회차 — 정본 white-space 표(WrapUi.json) 3253 `.chat-preview-msg { nowrap }`(전엔 박힘)
            chat.overflowMode = TextOverflowModes.Ellipsis;
            UiKit.Place(chat.rectTransform, tx, padY, tw, bandH - padY * 2f);
        }

        // ---- 뒤 작업이 부르는 표면 ----

        public void SetProfile(string nick, string combatPower)
        {
            nickname.text = nick ?? string.Empty;
            cp.text = combatPower ?? string.Empty;
        }

        /// <summary>
        /// 전투력만 갈아끼운다. 전투력은 전투가 도는 동안 **매초 바뀌므로**(T63 이 훅을 살린 뒤로) 그때마다
        /// <see cref="SetProfile"/> 로 닉네임까지 다시 쓰면 닉네임을 직접 준 쪽(테스트·디버그 표면)의 글자를 되돌린다
        /// (CI 런 108 `HUD_Set_표면이_글자를_바꾼다` 가 그렇게 빨갰다 — 기대 «moonzzanf», 실제 «용사»).
        /// </summary>
        public void SetCombatPower(string combatPower) { cp.text = combatPower ?? string.Empty; }

        /// <summary>지금 상단바에 걸린 전투력 글자 — 테스트·검증용.</summary>
        public string CombatPowerText { get { return cp.text; } }

        /// <summary>
        /// 원작 <c>renderTopBar</c> 의 <c>IconGen.avatar(S.avatarEmoji)</c> — 프로필 카드의 도트 초상(T31 아틀라스).
        /// 원작은 아바타를 고치면 상단바를 다시 그리므로(<c>onPickAvatar</c>) 다시 불러 갈아끼울 수 있다.
        /// 아틀라스에 없는 이모지면 초상을 감춘다(지금까지의 빈 흰 타일 그대로 · 원작은 이모지 글자로 폰트 폴백한다).
        /// </summary>
        public void SetAvatar(string emoji)
        {
            if (avatarTile == null) return;
            Sprite portrait = UiIcons.Avatar(emoji);
            if (portrait == null)
            {
                if (avatarPortrait != null) avatarPortrait.enabled = false;
                return;
            }
            if (avatarPortrait == null)
            {
                avatarPortrait = UiKit.Panel(avatarTile, "portrait", "avatar_bg");
                avatarPortrait.type = Image.Type.Simple;
                avatarPortrait.preserveAspect = true;
                Inset(avatarPortrait.rectTransform, UiKit.L("line_px") * 2f);
            }
            avatarPortrait.enabled = true;
            avatarPortrait.sprite = portrait;
        }

        /// <summary>지금 상단바에 걸린 초상(없으면 null) — 테스트·검증용.</summary>
        public Sprite AvatarPortrait { get { return avatarPortrait != null && avatarPortrait.enabled ? avatarPortrait.sprite : null; } }

        public void SetCurrency(string coinText, string gemText)
        {
            coins.text = coinText ?? string.Empty;
            gems.text = gemText ?? string.Empty;
        }

        public void SetStage(string label) { stage.text = label ?? string.Empty; }

        /// <summary>정본 `.chat-preview-badge` 의 «99» — 원작이 HTML 에 그대로 박아 둔 자리표다(읽지 않은 수를 세지 않는다).</summary>
        public const string ChatBadgeText = "99";

        /// <summary>이름 줄 / 메시지 줄 두 줄(정본 `renderChatPreview`).</summary>
        public void SetChatPreview(string name, string message)
        {
            if (chatName != null) chatName.text = name ?? string.Empty;
            chat.text = message ?? string.Empty;
        }

        /// <summary>한 줄만 아는 자리(옛 표면) — 메시지 줄에만 쓴다.</summary>
        public void SetChatPreview(string line) { SetChatPreview(chatName != null ? chatName.text : null, line); }

        public string ChatName { get { return chatName != null ? chatName.text : null; } }
        public string ChatMessage { get { return chat.text; } }

        /// <summary>웨이브 노드. <paramref name="total"/> 개(메인 5 · 던전 1~3) · <paramref name="now"/> = 진행 중 웨이브(1부터) · <paramref name="bossWave"/> = 보스 웨이브 번호(없으면 -1).</summary>
        public void SetWaves(int total, int now, int bossWave)
        {
            if (total < 1) total = 1;
            if (pips.Count != total) RebuildPips(total);
            float line = UiKit.L("line_px");
            float d = UiKit.W("pip");
            float dNow = UiKit.W("pip_now");
            for (int i = 0; i < pips.Count; i++)
            {
                int wave = i + 1;
                bool isNow = wave == now;
                bool isDone = wave < now;
                bool isBoss = wave == bossWave;
                Pip p = pips[i];
                float size = isNow ? dNow : d;
                p.Ring.rectTransform.sizeDelta = new Vector2(size, size);
                p.Fill.rectTransform.sizeDelta = new Vector2(size - line * 2f, size - line * 2f);
                // 정본 189 `.pip { border-radius: 50% }` · 195 `.pip.boss { border-radius: 2px; transform: rotate(45deg) }` —
                // 보스 핍은 «각진 마름모» 가 아니라 **모서리를 2 CSS px 만 깎은** 마름모다(T345 18회차 · 표 pip_boss_r_px).
                float bossR = RadiusUi.Px("pip_boss_r_px");
                PipShape(p.Ring, isBoss, bossR);
                PipShape(p.Fill, isBoss, bossR);
                p.Ring.transform.localRotation = isBoss ? Quaternion.Euler(0f, 0f, 45f) : Quaternion.identity;
                p.Fill.transform.localRotation = p.Ring.transform.localRotation;
                p.Fill.color = isBoss && isNow ? UiKit.C("pip_boss") : (isDone ? UiKit.C("pip_done") : UiKit.C("pip"));
            }
        }

        /// <summary>핍 한 조각의 모양 — 보통은 원(정본 189 `50%`) · 보스는 모서리를 <paramref name="bossRadiusPx"/> 만 깎은 네모(정본 195 `2px`)다.</summary>
        private static void PipShape(Image img, bool isBoss, float bossRadiusPx)
        {
            if (isBoss)
            {
                img.sprite = UiShapes.Rounded;
                img.type = Image.Type.Sliced;
                img.preserveAspect = false;
                img.pixelsPerUnitMultiplier = UiShapes.RoundedMultiplier(bossRadiusPx);
                return;
            }
            img.sprite = UiShapes.Circle;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.pixelsPerUnitMultiplier = 1f;
        }

        private void RebuildPips(int total)
        {
            foreach (Pip p in pips) Destroy(p.Slot.gameObject);
            pips.Clear();
            if (track != null) Destroy(track.gameObject);

            float slot = UiKit.W("pip_now");
            float gap = UiKit.W("pip_gap");
            float rowW = total * slot + (total - 1) * gap;
            float rowH = UiKit.H("pips_h");
            UiKit.Anchor(pipsRow, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -UiKit.H("pips_top")), rowW, rowH);

            float line = UiKit.L("line_px");
            float trackH = UiKit.W("pip_track");
            track = UiKit.Box(pipsRow, "track");
            UiKit.Place(track, slot * 0.5f, (rowH - trackH) * 0.5f, rowW - slot, trackH);
            UiKit.Panel(track, "edge", "pp_line");
            Image core = UiKit.Panel(track, "core", "pip");
            Inset(core.rectTransform, line);

            for (int i = 0; i < total; i++)
            {
                Pip p = new Pip();
                p.Slot = UiKit.Box(pipsRow, "pip-" + (i + 1));
                UiKit.Place(p.Slot, i * (slot + gap), 0f, slot, rowH);
                p.Ring = UiKit.Circle(p.Slot, "ring", "pp_line");
                UiKit.Anchor(p.Ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, slot, slot);
                p.Fill = UiKit.Circle(p.Slot, "fill", "pip");
                UiKit.Anchor(p.Fill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, slot, slot);
                pips.Add(p);
            }
        }
    }
}
