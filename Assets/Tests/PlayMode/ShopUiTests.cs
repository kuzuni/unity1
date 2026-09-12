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
        private static IEnumerator Boot()
        {
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
            StringAssert.Contains("moonzzanf: 안녕", hud.transform.Find("chat-preview-msg") != null ? hud.transform.Find("chat-preview-msg").GetComponent<TMP_Text>().text : UiRoot.Instance.Chat.Find("chat-preview-msg").GetComponent<TMP_Text>().text, "미리보기 줄 = 마지막 메시지");
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
    }
}
