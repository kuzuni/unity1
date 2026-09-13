using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T22 — 상점·퀘스트·리그(목록·보상·상대 선택)·패스·프로필/설정·플레이어 정보·채팅·오프라인·디버그·스텁·토스트가 열리고 닫히며 콘솔 빨강 0 인가.
    /// popup 탭은 열린 동안 빨간 ✕ 가 되고 다시 누르면 닫힌다(원작 MODAL_TAB). 열린 화면의 글자는 전부 UiKit 을 거쳐 종류 하한 이상이다(T18 게이트를 팝업에도 건다).
    /// </summary>
    public class ShopUiTests
    {
        /// <summary>세이브 파일을 지운다 — 이 테스트들은 이름·재화·리그·채팅을 바꾸고 저장하므로, 남기면 뒤 테스트 클래스(UiSmokeTests 의 «부팅 닉네임 = 원작 defaultState» 등)가 빨개진다(CI 런 35 실측). 앞 클래스가 남긴 세이브도 같은 이유로 지우고 시작한다.</summary>
        private static void DeleteSave()
        {
            try
            {
                string p = System.IO.Path.Combine(Application.persistentDataPath, (SaveIo.Defs != null ? SaveIo.Defs.SaveKey : "forgeclone_save_v1") + ".json");
                if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
                if (SaveIo.Defs == null)
                    foreach (string f in System.IO.Directory.GetFiles(Application.persistentDataPath, "forgeclone_save*.json")) System.IO.File.Delete(f);
            }
            catch (System.Exception) { /* 저장소 접근 실패는 무시 */ }
        }

        [TearDown]
        public void CleanSave() { DeleteSave(); }

        private static IEnumerator Boot()
        {
            DeleteSave();
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다 (SaveIo → meta.json)");
            Assert.IsNotNull(PopupLayer.Instance, "팝업 층이 서지 않았다");
            Assert.IsNotNull(MetaHost.Instance.Shop, "MetaHost 가 T25 Core 를 세우지 않았다");
            yield return null;
        }

        private static void AssertTextGate()
        {
            UiCatalog cat = UiCatalog.Instance;
            foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            {
                if (!t.gameObject.activeInHierarchy) continue;
                UiTextKindTag tag = t.GetComponent<UiTextKindTag>();
                Assert.IsNotNull(tag, t.name + " 은 UiKit.Text 를 거치지 않았다");
                Assert.GreaterOrEqual(t.fontSize, cat.Kind(tag.Kind).min, t.name + " 글자 하한");
            }
        }

        private static RectTransform Find(string popupName, string objName)
        {
            Popup p = PopupLayer.Instance.Find(popupName);
            Assert.IsNotNull(p, popupName + " 이 열려 있지 않다");
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == objName) return rt;
            Assert.Fail(popupName + " 안에 «" + objName + "» 이 없다");
            return null;
        }

        /// <summary>world 코너로 «보이는 칸(스크롤 뷰포트)» 사각형을 만든다.</summary>
        private static Rect WorldRect(RectTransform rt)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y);
        }

        private static Button FindButton(string popupName, string buttonName)
        {
            Popup p = PopupLayer.Instance.Find(popupName);
            Assert.IsNotNull(p, popupName + " 이 열려 있지 않다");
            foreach (Button b in p.Root.GetComponentsInChildren<Button>(true))
                if (b.name == buttonName) return b;
            Assert.Fail(popupName + " 안에 «" + buttonName + "» 버튼이 없다");
            return null;
        }

        [UnityTest]
        public IEnumerator popup_탭은_팝업을_열고_빨간_X가_되며_다시_누르면_닫힌다()
        {
            yield return Boot();
            TabBar tb = UiRoot.Instance.TabBar;
            PopupLayer popups = PopupLayer.Instance;

            tb.OnTab("shop");
            yield return null;
            Assert.IsTrue(popups.IsOpen(ShopSheet.Name), "상점 시트");
            Assert.IsTrue(tb.IsX("shop"), "열린 popup 탭은 빨간 ✕");
            AssertTextGate();
            tb.OnTab("shop");
            yield return null;
            Assert.IsFalse(popups.IsOpen(ShopSheet.Name), "✕ 를 누르면 닫힌다");
            Assert.IsFalse(tb.IsX("shop"));

            tb.OnTab("quest");
            yield return null;
            Assert.IsTrue(popups.IsOpen(QuestSheet.Name), "퀘스트 시트");
            Assert.IsTrue(tb.IsX("quest"));
            AssertTextGate();
            tb.OnTab("pvp");
            yield return null;
            Assert.IsFalse(popups.IsOpen(QuestSheet.Name), "다른 탭을 누르면 앞 팝업은 접힌다(상호 배타)");
            Assert.IsTrue(popups.IsOpen(LeagueSheet.Name), "리그 시트");
            Assert.IsTrue(tb.IsX("pvp"));
            AssertTextGate();

            MetaHost h = MetaHost.Instance;
            LeagueSheet.OpenRewards(h);
            yield return null;
            Assert.IsTrue(popups.IsOpen(LeagueSheet.RewardsName), "보상 팝업은 시트 위에 겹친다");
            Assert.IsTrue(popups.IsOpen(LeagueSheet.Name));
            AssertTextGate();
            FindButton(LeagueSheet.RewardsName, "x-btn").onClick.Invoke();
            yield return null;
            Assert.IsFalse(popups.IsOpen(LeagueSheet.RewardsName));
            Assert.IsTrue(popups.IsOpen(LeagueSheet.Name), "✕ 는 리그 목록으로 돌아간다");

            LeagueSheet.OpenChallenge(h);
            yield return null;
            Assert.IsTrue(popups.IsOpen(LeagueSheet.ChallengeName));
            AssertTextGate();
            int ticketsBefore = h.LeagueState.Tickets;
            FindButton(LeagueSheet.ChallengeName, "challenge").onClick.Invoke();
            yield return null;
            Assert.AreEqual(ticketsBefore - 1, h.LeagueState.Tickets, "도전 = 티켓 1");
            Assert.IsTrue(h.ChatState.Messages[h.ChatState.Messages.Count - 1].Type == Forge.Core.Meta.ChatMessage.TypeShare, "리그 결과는 채팅에 공유 카드로");

            tb.OnTab("summon");
            yield return null;
            Assert.AreEqual(0, popups.OpenCount, "시트 탭을 열면 팝업은 전부 접힌다");
            Assert.IsFalse(tb.IsX("pvp"));
            tb.OnTab("summon");
            yield return null;
        }

        /// <summary>T130 — 리그 보상 표의 1·2·3위 배지는 정본 아틀라스 `rank1`~`rank3`(ui.js 4790 · 1·2위 왕관 · 3위 벽돌색 마름모)이고 4위 이하는 배지 없이 글자 라벨이다.
        /// 종전 `crown`/`badge` 는 GUI PRO Kit 데모 스프라이트라 정본 그림이 아니었다(T33 4회차 실측).</summary>
        [UnityTest]
        public IEnumerator T130_리그_보상_1_2_3위_배지는_정본_rank_아이콘이고_4위_이하는_배지가_없다()
        {
            yield return Boot();
            PopupLayer popups = PopupLayer.Instance;
            UiRoot.Instance.TabBar.OnTab("pvp");
            yield return null;
            MetaHost h = MetaHost.Instance;
            LeagueSheet.OpenRewards(h);
            yield return null;
            Popup p = popups.Find(LeagueSheet.RewardsName);
            Assert.IsNotNull(p, "보상 팝업");
            int badges = 0, tiers = 0;
            foreach (RectTransform tier in p.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (!tier.name.StartsWith("tier-")) continue;
                tiers++;
                int rank = int.Parse(tier.name.Substring(5));
                Transform rk = tier.Find("rank");
                Assert.IsNotNull(rk, tier.name + ": rank 칸");
                Transform b = rk.Find("badge");
                if (rank <= 3)
                {
                    Assert.IsNotNull(b, tier.name + ": 1·2·3위는 배지가 있어야 한다");
                    Image img = b.GetComponent<Image>();
                    Assert.IsNotNull(img);
                    Assert.IsNotNull(img.sprite, tier.name + ": 배지 스프라이트");
                    Assert.AreEqual("ico:rank" + rank, img.sprite.name, tier.name + ": 정본 아틀라스 rank" + rank + "(crown/badge 데모 스프라이트가 아니다)");
                    Assert.AreSame(UiIcons.Get("rank" + rank), img.sprite, tier.name + ": UiIcons 캐시의 그 스프라이트");
                    TextMeshProUGUI n = rk.Find("label").GetComponent<TextMeshProUGUI>();
                    Assert.AreEqual(rank.ToString(), n.text, tier.name + ": 배지 위 흰 숫자");
                    Assert.Greater(n.outlineWidth, 0f, tier.name + ": 숫자의 검정 링(.lgr-rank-n)");
                    badges++;
                }
                else Assert.IsNull(b, tier.name + ": 4위 이하는 배지 없이 글자 라벨(정본 t.label)");
            }
            Assert.AreEqual(3, badges, "배지 셋(1·2·3위)");
            Assert.Greater(tiers, 3, "4위 이하 줄이 있어야 한다");
            FindButton(LeagueSheet.RewardsName, "x-btn").onClick.Invoke();
            yield return null;
            Assert.IsFalse(popups.IsOpen(LeagueSheet.RewardsName));
        }

        [UnityTest]
        public IEnumerator 상점_특가는_하루_한_번_무료_수령이고_젬은_안_준다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            double hammersBefore = h.S.Hammers;
            double gemsBefore = h.S.Gems;
            ShopSheet.Open(h);
            yield return null;
            Forge.Core.Meta.ShopDeal deal = h.Meta.Shop.Deals[0];
            FindButton(ShopSheet.Name, "price").onClick.Invoke();
            yield return null;
            Assert.IsTrue(h.Shop.Claimed(h.ShopState, deal.Key), "수령 표시");
            Assert.AreEqual(hammersBefore + deal.Reward.Get("hammers", 0), h.S.Hammers, 0.001, "해머 지급");
            Assert.AreEqual(gemsBefore, h.S.Gems, 0.001, "젬은 어떤 경로로도 안 준다");
            Assert.IsFalse(FindButton(ShopSheet.Name, "price").interactable, "수령 완료 버튼은 비활성");
            Assert.AreEqual(UiCatalog.Instance.Boot.nickname, Hud.Instance.Nickname);
            Assert.AreEqual(PopupKit.Fmt(h.S.Hammers), PopupKit.Fmt(hammersBefore + deal.Reward.Get("hammers", 0)));
            StringAssert.Contains(PopupKit.Fmt(h.S.Coins), Hud.Instance.transform.Find("topbar/pill-coin/value").GetComponent<TMP_Text>().text, "HUD 코인이 동기화된다");
            ShopSheet.Close(h);
            yield return null;
            Assert.AreEqual(0, PopupLayer.Instance.OpenCount);
        }

        [UnityTest]
        public IEnumerator 프로필_설정_채팅_패스_오프라인_디버그_스텁_토스트가_열리고_닫힌다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            PopupLayer popups = PopupLayer.Instance;
            Hud hud = Hud.Instance;

            hud.ProfileButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(popups.IsOpen(ProfilePopup.Name), "상단바 프로필 카드 → 프로필");
            AssertTextGate();
            ProfilePopup.SwitchView(h, "settings");
            yield return null;
            Assert.AreEqual("settings", ProfilePopup.View);
            AssertTextGate();
            bool sfx = h.S.SfxOn;
            bool vib = Forge.Core.Data.J.Bool(h.SettingsDummy["vibration"]);
            FindButton(ProfilePopup.Name, "toggle").onClick.Invoke();
            yield return null;
            Assert.AreEqual(!vib, Forge.Core.Data.J.Bool(h.SettingsDummy["vibration"]), "첫 토글(진동)이 뒤집힌다");
            ProfilePopup.SetNickname(h, "moonzzanf");
            yield return null;
            Assert.AreEqual("moonzzanf", hud.Nickname, "이름을 바꾸면 HUD 가 따라온다");
            Assert.AreEqual(sfx, h.S.SfxOn, "진동 토글은 효과음 설정을 건드리지 않는다");
            FindButton(ProfilePopup.Name, "x-btn").onClick.Invoke();
            yield return null;
            Assert.IsFalse(popups.IsOpen(ProfilePopup.Name));

            hud.ChatButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(popups.IsOpen(ChatScreen.Name), "채팅 줄 → 전체화면 채팅");
            AssertTextGate();
            int before = h.ChatState.Messages.Count;
            Assert.IsTrue(h.Chat.SendPlayer(h.ChatState, "안녕", h.Nickname, h.AvatarEmoji, h.Gender, h.NowMs));
            h.Touch();
            yield return null;
            Assert.AreEqual(before + 1, h.ChatState.Messages.Count);
            // 정본 `renderChatPreview`(ui.js 5288~5299)는 이름 줄 / 메시지 줄 **두 줄**이다(T63) —
            // «이름: 메시지» 한 줄을 다시 만들지 않고 두 줄을 따로 본다.
            Assert.AreEqual("moonzzanf", hud.ChatName, "미리보기 이름 줄 = 마지막 메시지의 보낸이");
            Assert.AreEqual("안녕", hud.ChatMessage, "미리보기 메시지 줄 = 마지막 메시지");
            FindButton(ChatScreen.Name, "close").onClick.Invoke();
            yield return null;
            Assert.IsFalse(popups.IsOpen(ChatScreen.Name));

            PassPopup.Open(h);
            yield return null;
            Assert.IsTrue(popups.IsOpen(PassPopup.Name));
            AssertTextGate();
            FindButton(PassPopup.Name, "x-btn").onClick.Invoke();
            yield return null;
            Assert.IsFalse(popups.IsOpen(PassPopup.Name));

            PlayerInfoPopup.Open(h);
            yield return null;
            Assert.IsTrue(popups.IsOpen(PlayerInfoPopup.Name));
            AssertTextGate();
            PlayerInfoPopup.Close(h);
            yield return null;

            OfflinePopup.Show(h, new OfflineReward { Elapsed = 5000, Counted = 3600, Coins = 3600, Hammers = 60, CoinRate = 1, HammerRate = 1 });
            yield return null;
            Assert.IsTrue(popups.IsOpen(OfflinePopup.Name));
            AssertTextGate();
            FindButton(OfflinePopup.Name, "x-btn").onClick.Invoke();
            yield return null;
            Assert.IsFalse(popups.IsOpen(OfflinePopup.Name), "✕ 는 단순 닫힘 — 보상은 남는다");

            DebugPanel.Open(h);
            yield return null;
            Assert.IsTrue(popups.IsOpen(DebugPanel.Name));
            AssertTextGate();
            double coins = h.S.Coins;
            DebugPanel.AddCurrency(h, "coins");
            Assert.AreEqual(coins + DebugPanel.AddAmount, h.S.Coins, 0.001);
            DebugPanel.Step(h, 1);
            Assert.AreEqual(2, h.S.Stage, "다음 스테이지");
            DebugPanel.Step(h, -1);
            Assert.AreEqual(1, h.S.Stage);
            DebugPanel.Close(h);
            yield return null;

            h.OpenStub("파워 랭킹", "서버 내 전투력 랭킹은 준비 중입니다.");
            yield return null;
            Assert.IsTrue(popups.IsOpen("stub"));
            AssertTextGate();
            FindButton("stub", "close").onClick.Invoke();
            yield return null;
            Assert.IsFalse(popups.IsOpen("stub"));

            h.Toast("💎 데모 버전에서는 결제를 지원하지 않습니다");
            yield return null;
            Assert.AreEqual("💎 데모 버전에서는 결제를 지원하지 않습니다", popups.LastToast);
            AssertTextGate();
        }
        /// <summary>
        /// T62 — «보석» 배너와 젬 카드 3장이 **스크롤하지 않고** 보이는 칸 안에 있는가(원작 shot-042632 은 배너 69.4%H · 카드 76.3%H 에 둘 다 화면 안이다).
        /// 특가 카드가 정본 비율(15.28%H)보다 높으면 이 절이 탭바 아래로 밀려 화면에서 사라진다 — 그것이 T62 가 잡은 결함이다.
        /// </summary>
        [UnityTest]
        public IEnumerator 보석_절은_스크롤_없이_보이는_칸_안에_있다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            ShopSheet.Open(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            Rect view = WorldRect(Find(ShopSheet.Name, "scroll"));
            Rect banner = WorldRect(Find(ShopSheet.Name, "banner-보석"));
            Assert.GreaterOrEqual(banner.yMin, view.yMin - 0.51f, "«보석» 배너가 보이는 칸 아래로 밀렸다 (배너 " + banner + " · 칸 " + view + ")");
            Assert.LessOrEqual(banner.yMax, view.yMax + 0.51f, "«보석» 배너가 칸 위로 넘쳤다");

            // 정본 표는 젬 상품 4개인데 격자가 3열이라 넷째는 **둘째 줄**이다 — 원작 샷(042632)에서도 그 줄은 탭바 아래라 안 보인다.
            // 화면 안이어야 하는 것은 첫 줄 셋이다(그것이 T62 가 잡은 «아예 안 보인다» 의 반대말).
            int gems = h.Meta.Shop.GemPacks.Count;
            Assert.Greater(gems, 0, "정본 표에 젬 상품이 없다");
            int firstRow = gems < 3 ? gems : 3;
            for (int i = 0; i < firstRow; i++)
            {
                Rect card = WorldRect(Find(ShopSheet.Name, "gem-" + i));
                Assert.Less(card.yMax, view.yMax + 0.51f, "젬 카드 " + i + " 가 칸 위로 넘쳤다");
                Assert.Greater(card.yMax, view.yMin, "젬 카드 " + i + " 가 보이는 칸 아래로 완전히 밀렸다 (카드 " + card + " · 칸 " + view + ")");
                float hidden = view.yMin - card.yMin;
                Assert.Less(hidden, card.height * 0.25f, "젬 카드 " + i + " 가 1/4 넘게 잘렸다 — 정본은 바닥만 탭바에 덮인다 (잘린 " + hidden + " / 높이 " + card.height + ")");
            }

            RectTransform deal = Find(ShopSheet.Name, "card");
            Assert.AreEqual(UiKit.H("shop_deal_h"), deal.rect.height, 0.51f, "특가 카드 높이는 정본 .shop-deal-card min-height(15.28%H) 그대로여야 한다");
            AssertTextGate();
            ShopSheet.Close(h);
            yield return null;
        }


        [UnityTest]
        public IEnumerator T75_보석_카드_안쪽_세로_배분이_정본_배분과_같다()
        {
            yield return Boot();
            UiRoot.Instance.TabBar.OnTab("shop");
            yield return null;
            Assert.IsTrue(PopupLayer.Instance.IsOpen(ShopSheet.Name), "상점 시트가 열려야 한다");

            RectTransform card = null;
            foreach (RectTransform rt in PopupLayer.Instance.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "gem-0") { card = rt; break; }
            Assert.IsNotNull(card, "보석 카드(gem-0)가 없다");

            // 정본 `.shop-gem-card` 주석의 실측(894px 캡처 · 카드 상단 기준): 수량 +8~36 · 그림 +38~98 · 가격 +101~124.
            // ⚠ 높이는 3px 여유를 둔다: 같은 정본이 두 가지로 적혀 있다 — 주석의 «밴드»(그림 60px = 6.71%H)와
            //   규칙의 «그림 자체»(`.shop-gem-icon .ico` = 6.84%H = 61.2px). 그 2.5px 차는 캡처 반올림이지 어긋남이 아니다
            //   (런 137 이 그 차로 빨갰다). **자리(top)** 는 이 작업이 실제로 옮긴 값이라 1px 로 조인다.
            float h = UiKit.RefH;
            Check(card, "amt-row", 8f / 894f * h, 28f / 894f * h);
            Check(card, "icon", 38f / 894f * h, 60f / 894f * h);
            Check(card, "buy", 101f / 894f * h, 23f / 894f * h);
        }

        /// <summary>카드 상단 기준 자식의 top·높이가 정본 값과 1px 안에서 같은가(UiKit.Place 는 y 를 위에서 아래로 음수로 쓴다).</summary>
        private static void Check(RectTransform card, string child, float wantTop, float wantH)
        {
            RectTransform rt = null;
            foreach (RectTransform t in card.GetComponentsInChildren<RectTransform>(true))
                if (t.name == child) { rt = t; break; }
            Assert.IsNotNull(rt, "보석 카드에 " + child + " 가 없다");
            float top = -rt.anchoredPosition.y;
            Assert.AreEqual(wantTop, top, 1f, child + " 의 카드 상단 기준 y (정본 배분)");
            Assert.AreEqual(wantH, rt.sizeDelta.y, 3f, child + " 의 높이 (정본 배분 · 주석 밴드 ↔ CSS 규칙 2.5px 차 허용)");
        }

        [UnityTest]
        public IEnumerator T91_부팅_직후에_채팅_미리보기_두_줄이_차_있다()
        {
            yield return Boot();
            float t = 0f;
            while (!MetaHost.Ready && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 준비되지 않았다");

            // 원작은 부팅 UI 초기화에서 renderChatPreview() 를 한 번 부른다(ui.js:849) — 새 봇 메시지를 기다리지 않는다.
            // 그것이 없어서 촬영(런 147·149)이 «빈 채팅 띠» 를 찍었다(T91).
            Hud hud = Hud.Instance;
            Assert.IsNotNull(hud, "HUD 가 없다");
            Assert.IsFalse(string.IsNullOrEmpty(hud.ChatName), "부팅 직후 채팅 미리보기 이름 줄이 비어 있다");
            Assert.IsFalse(string.IsNullOrEmpty(hud.ChatMessage), "부팅 직후 채팅 미리보기 메시지 줄이 비어 있다");

            // 마지막 메시지를 그대로 담는가(정본 renderChatPreview 는 Chat.lastMessage() 를 쓴다).
            MetaHost h = MetaHost.Instance;
            Forge.Core.Meta.ChatMessage last = h.Chat.LastMessage(h.ChatState, h.NowMs);
            Assert.IsNotNull(last, "채팅 상태에 마지막 메시지가 있어야 한다(Chat.Ensure 가 씨를 뿌린다)");
            string wantMsg = last.Type == Forge.Core.Meta.ChatMessage.TypeShare ? "전투 결과를 공유했습니다" : last.Text;
            Assert.AreEqual(wantMsg, hud.ChatMessage);

            // 촬영(런 147~155)은 글자가 **안 보이는데** 위 단언은 통과했다 — 그래서 «글자가 들어갔나» 말고
            // «그 글자가 그려질 수 있는 상태인가» 를 같이 본다: 켜져 있고 · 칸이 0 이 아니고 · 글자 크기가 하한 이상이고 · 알파가 0 이 아니다.
            foreach (string n in new[] { "chat-preview-name", "chat-preview-msg" })
            {
                TMPro.TextMeshProUGUI lbl = null;
                foreach (TMPro.TextMeshProUGUI c in UiRoot.Instance.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
                    if (c.name == n) { lbl = c; break; }
                Assert.IsNotNull(lbl, n + " 이 없다");
                Assert.IsTrue(lbl.isActiveAndEnabled, n + " 이 꺼져 있다");
                Rect r = lbl.rectTransform.rect;
                Assert.Greater(r.width, 1f, n + " 의 칸 폭이 0 이다");
                Assert.Greater(r.height, 1f, n + " 의 칸 높이가 0 이다");
                Assert.Greater(lbl.fontSize, 1f, n + " 의 글자 크기가 0 이다");
                Assert.Greater(lbl.color.a, 0.01f, n + " 이 투명하다");
                // 실제로 글리프가 배치됐는가 — TMP 가 칸이 좁아 한 글자도 못 그리면 0 이다.
                lbl.ForceMeshUpdate();
                Assert.Greater(lbl.textInfo.characterCount, 0, n + " 이 글리프를 하나도 못 그렸다(칸이 좁거나 글꼴에 글자가 없다)");
            }
        }

        [UnityTest]
        public IEnumerator T101_리그_도전_행은_별점이_버튼_위에_얹힌_세로_한_칸이다()
        {
            yield return Boot();
            float t0 = 0f;
            // 상대 목록(`League.Ensure` 가 뿌리는 봇)은 전투력을 보고 만들어지므로 전투가 설 때까지 기다린다 —
            // 그 전엔 목록이 비어 행이 하나도 없다(런 199 실측: «상대 행이 없다»).
            while ((!MetaHost.Ready || Forge.Game.Battle.BattleScene.Instance == null
                    || !Forge.Game.Battle.BattleScene.Instance.Ready) && t0 < 25f)
            { t0 += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 준비되지 않았다");
            MetaHost h = MetaHost.Instance;
            h.OpenLeague();   // 리그는 «pvp» 탭이 연다 — `OnTab("league")` 은 없는 키다(런 191 실측)
            yield return null;
            LeagueSheet.OpenChallenge(h);
            yield return null;
            Assert.IsTrue(PopupLayer.Instance.IsOpen(LeagueSheet.ChallengeName), "상대 선택 팝업이 열려야 한다");

            // ⚠ 리그 **시트**(팝업 아래에 그대로 열려 있다)에도 «도전» 버튼이 있어서 팝업 밖까지 뒤지면 그것을 집는다
            //   (런 208 실측: 그 버튼의 부모엔 별이 없어 «별점 칸이 없다» 로 빨갰다). 그래서 **이 팝업 뿌리 아래만** 본다.
            // 뿌리는 `PopupLayer.Find(name).Root` 로 잡는다 — 팝업 상자(`modals`/`modals-over`)는 `PopupLayer` 가 아니라
            // **UiRoot 의 앱 상자** 아래에 있어서 `PopupLayer.Instance.GetComponentsInChildren` 으로는 안 걸린다(런 214 실측).
            Popup popup = PopupLayer.Instance.Find(LeagueSheet.ChallengeName);
            Assert.IsNotNull(popup, "상대 선택 팝업이 PopupLayer 에 없다");
            RectTransform popupRoot = popup.Root;
            Assert.IsNotNull(popupRoot, "상대 선택 팝업 뿌리가 없다");

            // 행 이름에 기대지 않고 «별과 도전 버튼을 함께 가진 칸» 을 행으로 삼는다(슬롯 이름이 바뀌어도 안 깨진다).
            RectTransform row = null, star = null, btn = null;
            foreach (RectTransform rt in popupRoot.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name != "challenge") continue;
                RectTransform parent = rt.parent as RectTransform;
                if (parent == null) continue;
                foreach (RectTransform c in parent.GetComponentsInChildren<RectTransform>(true))
                    if (c.name == "star") { star = c; break; }
                if (star != null) { btn = rt; row = parent; break; }
                star = null;
            }
            Assert.IsNotNull(btn, "상대 행이 하나도 없다 — League.Ensure 가 봇을 안 뿌렸거나 행 꼴이 바뀌었다");
            Assert.IsNotNull(star, "별점 칸이 없다");

            // 정본 `.league-challenge-side { flex-direction: column }` — 별이 **버튼 위**다(가로로 나란히가 아니다).
            float starBottom = -star.anchoredPosition.y + star.rect.height;
            float btnTop = -btn.anchoredPosition.y;
            Assert.LessOrEqual(starBottom, btnTop + 1f, "별점이 도전 버튼 위에 있어야 한다(정본은 세로 한 칸)");
            Assert.AreEqual(star.anchoredPosition.x, btn.anchoredPosition.x, 1f, "별점 칸과 버튼은 같은 세로줄에 선다");
            Assert.AreEqual(star.rect.width, btn.rect.width, 1f, "별점 칸 폭은 버튼 폭과 같다(가운데 정렬 기준)");
        }
    }
}
