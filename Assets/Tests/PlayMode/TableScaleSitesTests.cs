using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T378 — «표값 × 박힌 상수» 자리를 걷은 뒤 그 자리가 **표값 그대로** 서는지(자 `tools/check_table_scale.py` 는 코드 글자를 보고,
    /// 이 자는 실제 RectTransform 을 본다). 자리를 걷을 때마다 칸을 하나씩 더한다.
    /// </summary>
    public class TableScaleSitesTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = Time.realtimeSinceStartup;
            while (!(MetaHost.Ready && PopupLayer.Instance != null && UiRoot.Instance != null))
            {
                Assert.Less(Time.realtimeSinceStartup - t, 20f, "MetaHost/PopupLayer 가 20초 안에 준비되지 않았다");
                yield return null;
            }
            yield return null;
        }

        /// <summary>3회차 — 프로필 연필 버튼: 정본 3008 `.profile-edit-btn { width: 1.5rem; height: 1.5rem }` = 표 `profile_edit`(0.0284H) 그대로(전엔 ×1.3).</summary>
        [UnityTest]
        public IEnumerator 프로필_연필_버튼은_표_profile_edit_그대로_1_5rem_정사각이다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            ProfilePopup.Open(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(ProfilePopup.Name);
            Assert.IsNotNull(p, "프로필 팝업");
            Button avEdit = null;
            foreach (Button b in p.Root.GetComponentsInChildren<Button>(true)) if (b.name == "avatar-edit") avEdit = b;
            Assert.IsNotNull(avEdit, "연필 버튼(avatar-edit)");
            Rect r = avEdit.GetComponent<RectTransform>().rect;
            float expect = UiKit.H("profile_edit");
            Assert.AreEqual(expect, r.width, 0.5f, "폭 = 표 profile_edit(곱 없이)");
            Assert.AreEqual(expect, r.height, 0.5f, "높이 = 표 profile_edit(곱 없이)");
            Assert.AreEqual(PopupKit.Rem * 1.5f, r.width, 0.5f, "정본 1.5rem — 표 0.0284 가 그 값이다(1.5 × rem_h)");
            Assert.Less(r.width, UiKit.H("profile_edit") * 1.3f - 1f, "옛 ×1.3 크기가 아니다");
            PopupLayer.Instance.Hide(ProfilePopup.Name);
            yield return null;
        }

        /// <summary>6회차 — 상점 머리 재화 알약: 정본 3968~3975 `.shop-sheet .sheet-head .cur-pill { width: .155W; height: .0239H }` = 표 `shop_cur_w`·`shop_cur_h` 그대로(전엔 높이 ×1.6).</summary>
        [UnityTest]
        public IEnumerator 상점_재화_알약은_표_shop_cur_h_그대로다()
        {
            yield return Boot();
            UiRoot.Instance.TabBar.OnTab("shop");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(ShopSheet.Name);
            Assert.IsNotNull(p, "상점 시트");
            int bars = 0;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name != "coin-bar" && rt.name != "gem-bar") continue;
                bars++;
                Assert.AreEqual(UiKit.H("shop_cur_h"), rt.rect.height, 0.5f, rt.name + ": 높이 = 표 shop_cur_h(곱 없이)");
                Assert.AreEqual(UiKit.L("shop_cur_w") * UiKit.RefW, rt.rect.width, 0.5f, rt.name + ": 폭 = 표 shop_cur_w");
                Assert.Less(rt.rect.height, UiKit.H("shop_cur_h") * 1.6f - 1f, rt.name + ": 옛 ×1.6 높이가 아니다");
            }
            Assert.AreEqual(2, bars, "코인·젬 알약 둘");
            UiRoot.Instance.TabBar.OnTab("shop");
            yield return null;
        }

        /// <summary>6회차 — 채팅 목록 상자: 정본 3265 `.chat-card { padding: 0 }` · 3294 `.chat-list { padding: 0 }` — 카드 위끝에서 바로 시작한다(전엔 상단바×0.3 인셋).</summary>
        [UnityTest]
        public IEnumerator 채팅_목록은_카드_위끝에서_인셋_없이_시작한다()
        {
            yield return Boot();
            Hud.Instance.ChatButton.onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(ChatScreen.Name);
            Assert.IsNotNull(p, "채팅 화면");
            RectTransform listBox = null;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true)) if (rt.name == "list-box") { listBox = rt; break; }
            Assert.IsNotNull(listBox, "list-box");
            Assert.AreEqual(1f, listBox.anchorMax.y, 1e-4f, "위끝 앵커");
            Assert.AreEqual(0f, listBox.offsetMax.y, 0.5f, "위 인셋 0(정본 padding 0 · 전엔 topbar_h×0.3)");
            PopupLayer.Instance.Hide(ChatScreen.Name);
            yield return null;
        }

        /// <summary>7회차 — 설정 실동작 버튼(.settings-act): 정본 3121~3125 는 높이 선언 없이 line-height 1.15rem + padding .1rem×2 + ol2×2 = 1.6rem = 곁 표 `settings_act_h`(0.0303H) 그대로(전엔 토글 높이 ×1.1).</summary>
        [UnityTest]
        public IEnumerator 설정_실동작_버튼_높이는_곁_표_settings_act_h_그대로_1_6rem_이다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            ProfilePopup.Open(h);
            yield return null;
            ProfilePopup.SwitchView(h, "settings");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(ProfilePopup.Name);
            Assert.IsNotNull(p, "프로필 팝업(설정)");
            float expect = ProfileUi.H("settings_act_h");
            int acts = 0;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name != "act" || rt.Find("line") == null || rt.Find("face") == null) continue;
                acts++;
                Assert.AreEqual(expect, rt.rect.height, 0.5f, "높이 = 곁 표 settings_act_h(곱 없이)");
                Assert.AreEqual(PopupKit.Rem * 1.6f, rt.rect.height, 0.5f, "정본 1.6rem — 표 0.0303 이 그 값이다(1.6 × rem_h)");
                Assert.Greater(rt.rect.height, UiKit.H("settings_toggle_h") * 1.1f + 1f, "옛 토글×1.1 높이(1.485rem)가 아니다");
                Assert.Less(rt.rect.height, UiKit.H("settings_row_h") - 1f, "행(4.75%H) 안에 든다");
            }
            Assert.AreEqual(2, acts, "실동작 버튼 둘(수동 저장 · 게임 초기화)");
            ProfilePopup.Close(h);
            yield return null;
        }

        /// <summary>8회차 — 제작 비교 팝업 [판매][장착]: 정본 3566 `#craft-modal .row .btn { min-height: 4.2rem }`(border-box · 글이 그 아래라 하한이 곧 높이) = 곁 표 `cmp_row_btn_h_rem` 그대로(전엔 btn_h ×1.7 = 4.08rem).</summary>
        [UnityTest]
        public IEnumerator 제작_비교_판매_장착_버튼_높이는_곁_표_cmp_row_btn_h_rem_그대로_4_2rem_이다()
        {
            yield return Boot();
            float t0 = Time.realtimeSinceStartup;
            while (!ForgeHost.Ready && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            ForgeHost fh = ForgeHost.Instance;
            Assert.IsNotNull(fh, "ForgeHost");
            Forge.Core.Forging.ForgeItem it = fh.Engine.RollItem();
            ForgeCraftPopup.Show(fh, it);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(ForgeCraftPopup.Name);
            Assert.IsNotNull(p, "제작 비교 팝업");
            float expect = CraftStyle.Px("cmp_row_btn_h_rem");
            Assert.AreEqual(PopupKit.Rem * 4.2f, expect, 0.01f, "표 4.2rem × rem_h");
            int found = 0;
            foreach (Button b in p.Root.GetComponentsInChildren<Button>(true))
            {
                if (b.name != "sell" && b.name != "equip") continue;
                found++;
                Rect r = b.GetComponent<RectTransform>().rect;
                Assert.AreEqual(expect, r.height, 0.5f, b.name + ": 높이 = 곁 표 cmp_row_btn_h_rem(곱 없이)");
                Assert.Greater(r.height, UiKit.H("btn_h") * 1.7f + 1f, b.name + ": 옛 btn_h×1.7(4.08rem)이 아니다");
            }
            Assert.AreEqual(2, found, "[판매][장착] 둘");
            PopupLayer.Instance.Hide(ForgeCraftPopup.Name);
            yield return null;
        }
        /// <summary>10회차 — 리그 시트·보상·도전의 아홉 자리(`LeagueSheet.cs`): 시즌 바(정본 2308 .4217W×.0257H) · 행 피치(2320·2326 6.36%H) ·
        /// 티어 행(2540 8.61%H) · 등수 칸(2556 2.4rem) · 티켓 알약(2594 .1392W×.0411W) — 표값 그대로(전엔 ×1.1~×1.6 곱).</summary>
        [UnityTest]
        public IEnumerator 리그_시즌_바_행_티어_등수_티켓_알약은_표값_그대로다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            float W = UiKit.RefW;
            LeagueSheet.Open(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(LeagueSheet.Name);
            Assert.IsNotNull(p, "리그 시트");
            RectTransform bar = null, pinned = null;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true)) { if (rt.name == "season-bar") bar = rt; else if (rt.name == "pinned") pinned = rt; }
            Assert.IsNotNull(bar, "시즌 바(season-bar)"); Assert.IsNotNull(pinned, "내 행(pinned)");
            Assert.AreEqual(UiKit.L("league_bar_w") * W, bar.rect.width, 0.5f, "시즌 바 폭 = 표 league_bar_w(곱 없이 · 정본 .4217W)");
            Assert.AreEqual(UiKit.H("league_bar_h"), bar.rect.height, 0.5f, "시즌 바 높이 = 표 league_bar_h(정본 .0257H · 전엔 ×1.6)");
            Assert.AreEqual(UiKit.H("league_row_h"), pinned.rect.height, 0.5f, "행 높이 = 표 league_row_h(정본 6.36%H · 전엔 ×1.1)");
            PopupLayer.Instance.Hide(LeagueSheet.Name);
            yield return null;
            LeagueSheet.OpenRewards(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            p = PopupLayer.Instance.Find(LeagueSheet.RewardsName);
            Assert.IsNotNull(p, "리그 보상");
            int tiers = 0, ranks = 0;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name.StartsWith("tier-")) { tiers++; Assert.AreEqual(UiKit.H("lgr_tier_h"), rt.rect.height, 0.5f, rt.name + " 높이 = 표 lgr_tier_h(정본 8.61%H · 전엔 ×1.15)"); }
                else if (rt.name == "rank" && rt.parent != null && rt.parent.name.StartsWith("tier-")) { ranks++; Assert.AreEqual(UiKit.H("lgr_rank_w"), rt.rect.width, 0.5f, "등수 칸 폭 = 표 lgr_rank_w(정본 2.4rem · 전엔 ×1.5)"); }
            }
            Assert.Greater(tiers, 0, "티어 행"); Assert.Greater(ranks, 0, "등수 칸");
            PopupLayer.Instance.Hide(LeagueSheet.RewardsName);
            yield return null;
            LeagueSheet.OpenChallenge(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            p = PopupLayer.Instance.Find(LeagueSheet.ChallengeName);
            Assert.IsNotNull(p, "상대 선택");
            RectTransform pill = null;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true)) if (rt.name == "pill" && rt.parent != null && rt.parent.name == "ticket-row") { pill = rt; break; }
            Assert.IsNotNull(pill, "티켓 알약(pill)");
            Assert.AreEqual(UiKit.L("lc_pill_w") * W, pill.rect.width, 0.5f, "티켓 알약 폭 = 표 lc_pill_w(정본 .1392W · 전엔 ×1.3)");
            Assert.AreEqual(UiKit.L("lc_pill_h") * W, pill.rect.height, 0.5f, "티켓 알약 높이 = 표 lc_pill_h(정본 .0411W · 전엔 ×1.4)");
            PopupLayer.Instance.Hide(LeagueSheet.ChallengeName);
            yield return null;
        }
    }
}
