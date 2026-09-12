using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Forging;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T19 — 장비 시트(격자 8+탈것·모루·대장간/자동 버튼)가 서고, 제작 → 리빌 → 비교 팝업 → 판매/장착/보류가 원작 흐름대로 돌며, 확률 정보·목록·상세·자동 제련·장비 세부정보 팝업이 열리고 닫히고,
    /// 업그레이드·젬 스킵이 세이브에 반영되는가. 열 때마다 T18 글자 하한 게이트를 다시 건다. 콘솔 빨강 0(유니티 러너가 예상 밖 에러 로그를 실패로 센다).
    /// </summary>
    public class ForgeUiTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다 (MetaHost → tech.json)");
            Assert.IsNotNull(ForgeHost.Instance.Engine, "ForgeHost 가 Ready 인데 엔진이 없다 — 앞 씬 호스트의 정적 상태가 남았다");
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

        private static Transform SheetChild(string name)
        {
            foreach (Transform t in UiRoot.Instance.Sheet.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        private static IEnumerator WaitCraftPopup(ForgeHost h)
        {
            float t = 0f;
            while (!h.Meta.Popups.IsOpen(ForgeCraftPopup.Name) && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "망치질(0.72s) + 리빌(0.56s) 뒤 비교 팝업이 떠야 한다");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 장비_시트에_격자_8칸_탈것칸_모루_대장간_자동_버튼이_선다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            foreach (string slot in h.Defs.Slots) Assert.IsNotNull(SheetChild("cell-" + slot), "장비 칸 " + slot);
            Assert.IsNotNull(SheetChild("egg-cell"), "탈것 칸");
            Assert.IsNotNull(SheetChild("anvil-btn"), "모루 버튼");
            Assert.IsNotNull(SheetChild("forge-btn"), "대장간 레벨 버튼");
            Assert.IsNotNull(SheetChild("auto-btn"), "자동 버튼");
            Assert.IsNotNull(SheetChild("info-btn"), "i 버튼");
            Assert.IsNotNull(SheetChild("anvil-hammers"), "해머 카운터");
            Assert.AreEqual(h.S.ForgeLevel, h.Forge.ForgeLevel, "대장간 레벨은 세이브와 같다");
            Assert.IsTrue(h.Meta.ForgeUpgradable != null && h.Meta.ForgeUpgradable(), "MetaHost.ForgeUpgradable 훅(퀘스트 FORGE_LOCKED 판정)");
            AssertTextGate();
        }

        [UnityTest]
        public IEnumerator 제작은_망치를_쓰고_비교_팝업을_띄우며_판매하면_코인이_는다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            h.S.Hammers = 10; h.Pull();
            double coins = h.S.Coins;
            h.OnCraft();
            Assert.IsTrue(h.AnvilBusy, "망치질 중엔 모루가 잠긴다");
            Assert.IsNotNull(h.Pending, "대기품은 연출 전에 세이브에 남는다");
            Assert.AreEqual(9, h.S.Hammers, "해머 1 소모가 세이브에 즉시 쓰였다");
            yield return WaitCraftPopup(h);
            Assert.IsFalse(h.AnvilBusy);
            AssertTextGate();
            // T57 — 비교 카드 둘 다 판 위에 있다(새 장비 카드가 3D 배경 위에 떠 있던 자리).
            AssertDressed(h.Meta.Popups.Find(ForgeCraftPopup.Name).Root, "craft-compare");
            ForgeItem item = h.Pending;
            double price = h.GearSys.SellPrice(item);
            h.ResolveCraft("sell");
            yield return null;
            if (h.Meta.Popups.IsOpen(ForgeCraftPopup.SellName))
            {
                h.OnSellConfirm();
                yield return null;
            }
            Assert.IsFalse(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "[판매] 는 팝업을 닫는다");
            Assert.IsNull(h.Pending);
            Assert.AreEqual(coins + price, h.S.Coins, 0.5, "판매가만큼 코인이 세이브에 들어갔다");
            Assert.IsNull(h.S[ForgeSave.KeyPending], "세이브의 pendingCraft 가 비었다");
        }

        [UnityTest]
        public IEnumerator 장착은_칸을_채우고_기존_장비가_있으면_두_카드를_맞바꾼다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            h.S.Hammers = 10; h.Pull();
            h.OnCraft();
            yield return WaitCraftPopup(h);
            ForgeItem item = h.Pending;
            ForgeItem prev = h.Gear.Get(item.Slot);
            h.ResolveCraft("equip");
            yield return null;
            Assert.AreSame(item, h.Gear.Get(item.Slot), "장착됐다");
            Assert.IsNotNull(h.S.Equipment[item.Slot], "세이브 equipment 칸에 쓰였다");
            if (prev == null) Assert.IsFalse(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "빈 부위면 팝업이 닫힌다");
            else
            {
                Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "옛 장비가 있으면 두 카드가 맞바뀐 채 열려 있다");
                Assert.AreSame(prev, h.Pending);
                Assert.IsTrue(h.PendingSwapped, "내려온 옛 장비는 «교체됨»");
                AssertTextGate();
                h.ResolveCraft("sell");
                yield return null;
                if (h.Meta.Popups.IsOpen(ForgeCraftPopup.SellName)) { h.OnSellConfirm(); yield return null; }
                Assert.IsFalse(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name));
            }
            Assert.IsNotNull(SheetChild("cell-" + item.Slot), "격자 칸");
            Assert.IsNotNull(SheetChild("cell-" + item.Slot).Find("lv"), "장착 칸에 Lv 배지");
            GearDetailPopup.Open(h, item.Slot);
            yield return null;
            Assert.IsTrue(h.Meta.Popups.IsOpen(GearDetailPopup.Name), "장비 세부정보 팝업");
            AssertTextGate();
            // T57 — 흰 카드 한 장 위에 아이템 한 줄(원작 shot-043244) · 카드 위 빈 막대 0.
            AssertDressed(h.Meta.Popups.Find(GearDetailPopup.Name).Root, "gear-detail");
            Assert.IsNull(FindIn(h.Meta.Popups.Find(GearDetailPopup.Name).Root, "cur").Find("face"), "장착 카드는 제 상자를 다시 그리지 않는다(원작 .cmp-card-wrap.cur .cmp-card{border:none})");
            GearDetailPopup.Close(h);
            yield return null;
            Assert.IsFalse(h.Meta.Popups.IsOpen(GearDetailPopup.Name));
        }

        [UnityTest]
        public IEnumerator 딤_클릭은_보류_모루_자리에_카드가_놓이고_누르면_다시_고른다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            h.S.Hammers = 10; h.Pull();
            h.OnCraft();
            yield return WaitCraftPopup(h);
            ForgeItem item = h.Pending;
            h.OnCraftDimClick();
            yield return null;
            Assert.IsFalse(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "딤 = 팝업만 닫는다");
            Assert.AreSame(item, h.HeldItem, "장비는 대기품 그대로");
            Assert.IsNotNull(SheetChild("held-slot"), "모루 자리에 보류 카드");
            Assert.IsNull(SheetChild("anvil-btn"), "모루는 카드에 가려진다");
            Assert.IsNotNull(h.S[ForgeSave.KeyPending], "보류품은 세이브에 남는다");
            AssertTextGate();
            h.OnCraft();
            yield return null;
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "보류 카드가 있으면 모루 클릭 = 그 카드 다시 열기");
            Assert.AreEqual(10 - 1, h.S.Hammers, "다시 열기는 해머를 안 쓴다");
            h.ResolveCraft("sell");
            yield return null;
            if (h.Meta.Popups.IsOpen(ForgeCraftPopup.SellName)) { h.OnSellConfirm(); yield return null; }
            Assert.IsNotNull(SheetChild("anvil-btn"), "처리하면 모루가 돌아온다");
        }

        [UnityTest]
        public IEnumerator 확률_정보_목록_상세_팝업이_열리고_닫힌다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeInfoPopup.Open(h);
            yield return null;
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeInfoPopup.Name));
            Assert.AreEqual("level", ForgeInfoPopup.View);
            Popup p = h.Meta.Popups.Find(ForgeInfoPopup.Name);
            int bars = 0;
            foreach (Transform t in p.Root.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("age-")) bars++;
            Assert.AreEqual(h.Defs.Ages.Length, bars, "시대 막대 10 (0% 시대도 표시)");
            Assert.IsNotNull(FindIn(p.Root, "fi-upgrade"), "업그레이드 버튼");
            AssertTextGate();

            ForgeInfoPopup.OpenList(h);
            yield return null;
            Assert.AreEqual("list", ForgeInfoPopup.View);
            int sections = 0;
            foreach (Transform t in p.Root.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("section-")) sections++;
            Assert.AreEqual(h.Defs.Ages.Length, sections, "시대별 절 10");
            AssertTextGate();

            string age = h.Defs.Ages[0];
            string wt = h.Engine.WeaponsOfAge(age)[0];
            ForgeInfoPopup.OpenDetail(h, age, "weapon", 0, wt);
            yield return null;
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeInfoPopup.ItemName), "장비 상세는 목록 위에 겹쳐 뜬다");
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeInfoPopup.Name), "뒤 목록이 그대로");
            int subs = 0;
            foreach (Transform t in h.Meta.Popups.Find(ForgeInfoPopup.ItemName).Root.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("substat-")) subs++;
            Assert.AreEqual(h.Defs.Substats.Count, subs, "옵션 13종 범위");
            AssertTextGate();
            // T57 — 상세는 제 판 위에 그려지고(목록 격자에 겹치지 않는다) ✕ 는 화면에 하나다(목록 것).
            AssertDressed(h.Meta.Popups.Find(ForgeInfoPopup.ItemName).Root, "forge-detail");
            Assert.AreEqual(1, XButtons(), "상세가 열려도 ✕ 는 하나(원작 shot-042931)");
            ForgeInfoPopup.CloseItemDetail(h);
            yield return null;
            Assert.IsFalse(h.Meta.Popups.IsOpen(ForgeInfoPopup.ItemName));
            ForgeInfoPopup.Close(h);
            yield return null;
            Assert.IsFalse(h.Meta.Popups.IsOpen(ForgeInfoPopup.Name));
        }

        [UnityTest]
        public IEnumerator 업그레이드_시작은_코인을_빼고_타이머를_걸며_젬_스킵은_레벨을_올린다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            h.S.Coins = 1000; h.S.Gems = 100; h.Pull();
            int lv = h.Forge.ForgeLevel;
            double cost = h.Engine.UpgradeCost(h.UpgradeInfo());
            h.OnStartUpgrade();
            yield return null;
            Assert.IsTrue(h.Upgrading, "타이머가 걸렸다");
            Assert.AreEqual(1000 - cost, h.S.Coins, 0.5, "코인이 세이브에서 빠졌다");
            Assert.IsTrue(h.S.ForgeUpgradeEndsAt.HasValue, "forgeUpgradeEndsAt 이 세이브에 쓰였다");
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeInfoPopup.Name), "시작하면 확률 정보 팝업이 열린다");
            Assert.IsNotNull(FindIn(h.Meta.Popups.Find(ForgeInfoPopup.Name).Root, "fi-skip"), "건너뛰기 버튼");
            Assert.IsNotNull(SheetChild("equip-upg-time"), "시트에 남은 시간 줄");
            AssertTextGate();
            h.OnGemSkipForge();
            yield return null;
            Assert.AreEqual(lv + 1, h.Forge.ForgeLevel, "젬 스킵으로 레벨 +1");
            Assert.AreEqual(lv + 1, h.S.ForgeLevel, "세이브에도");
            Assert.IsFalse(h.Upgrading);
            Assert.Less(h.S.Gems, 100, "젬이 나갔다");
            Assert.AreEqual("⚒️ 대장간 레벨 " + (lv + 1) + " 달성!", h.Meta.Popups.LastToast);
            ForgeInfoPopup.Close(h);
        }

        [UnityTest]
        public IEnumerator 자동_제련은_해금_뒤_설정을_저장하고_시작하면_망치를_배치로_쓴다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            h.S.Hammers = 30; h.Pull();
            h.OnAutoForgeBtn();
            yield return null;
            Assert.IsFalse(h.Meta.Popups.IsOpen(ForgeAutoPopup.Name), "2-10 전엔 잠겨 있다");
            Assert.AreEqual("🔒 스테이지 2-10 도달 시 해금됩니다", h.Meta.Popups.LastToast);
            h.S.BestChapter = 3; h.S.BestStage = 1;
            Assert.IsTrue(h.AutoForgeUnlocked);
            h.OnAutoForgeBtn();
            yield return null;
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeAutoPopup.Name), "설정 팝업");
            AssertTextGate();
            string age = h.Defs.Ages[0];
            h.ToggleKeepAge(age);
            yield return null;
            Assert.IsTrue(h.Engine.AutoForgeConfig().KeepAges.Contains(age));
            Assert.IsTrue(h.Engine.HasAutoTarget());
            h.ToggleAutoFilterOn();
            yield return null;
            Assert.IsTrue(h.Engine.AutoForgeConfig().FilterOn);
            Assert.IsNotNull(FindIn(h.Meta.Popups.Find(ForgeAutoPopup.Name).Root, "af-sub-critCh"), "필터가 켜지면 옵션 행 13");
            AssertDressed(h.Meta.Popups.Find(ForgeAutoPopup.Name).Root, "autoforge-filter");   // T57
            h.PickHammers(3);
            yield return null;
            Assert.AreEqual(3, h.Engine.AutoForgeConfig().HammersPerBatch);
            Assert.AreEqual(3.0, Forge.Core.Data.J.Num(h.S.Obj(ForgeSave.KeyAutoForge)["hammersPerBatch"]), "설정이 세이브 autoForge 칸에 쓰였다");
            ForgeAutoPopup.ToggleDropdown(h);
            yield return null;
            Assert.IsNotNull(FindIn(h.Meta.Popups.Find(ForgeAutoPopup.Name).Root, "af-dd-list"), "망치 수 목록");
            AssertTextGate();
            ForgeAutoPopup.ToggleDropdown(h);
            yield return null;

            h.OnToggleAutoForge();
            yield return null;
            Assert.IsTrue(h.AutoOn);
            Assert.IsTrue(h.S.AutoForgeOn, "autoForgeOn 이 세이브에");
            Assert.IsFalse(h.Meta.Popups.IsOpen(ForgeAutoPopup.Name), "시작하면 설정 팝업은 닫힌다");
            float t = 0f;
            while (h.S.Hammers >= 30 && t < 6f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.Less(h.S.Hammers, 30, "망치질 뒤 배치가 해머를 썼다");
            AssertTextGate();
            h.OnAutoForgeBtn();
            yield return null;
            Assert.IsFalse(h.AutoOn, "자동 모드에서 버튼 = 끄기");
            Assert.AreEqual("⏹ 자동 제련을 종료했습니다", h.Meta.Popups.LastToast);
            yield return new WaitForSecondsRealtime(2.2f);
            h.ResolvePendingCraft();
            yield return null;
            Assert.IsNull(h.Pending, "탭 전환 규칙으로 남은 제작품은 자동 판정된다");
        }

        /// <summary>
        /// 한 상자의 배경(line·face·bg)이 그 상자를 채우는가. 테 두께(`Inset`)만큼 안쪽으로 들어가는 것은
        /// 정상이라 `Line3` 두 배 + 2px 를 봐준다(CI 런 95 실측: 카드 면이 정확히 12px = 2×6 작았다).
        /// </summary>
        private static int AssertFills(Transform box, string what, bool needIgnoreLayout)
        {
            RectTransform brt = (RectTransform)box;
            float slack = PopupKit.Line3 * 2f + 2f;
            int n = 0;
            foreach (Transform ch in box)
            {
                if (ch.name != "line" && ch.name != "face" && ch.name != "bg") continue;
                RectTransform crt = (RectTransform)ch;
                if (needIgnoreLayout)
                {
                    LayoutElement le = ch.GetComponent<LayoutElement>();
                    Assert.IsTrue(le != null && le.ignoreLayout, what + ": " + box.name + "/" + ch.name + " 이 레이아웃 칸이다 — 배경이 빈 막대가 된다");
                }
                Assert.GreaterOrEqual(crt.rect.height, brt.rect.height - slack, what + ": " + box.name + "/" + ch.name + " 이 부모를 안 채운다(판 없음)");
                Assert.GreaterOrEqual(crt.rect.width, brt.rect.width - slack, what + ": " + box.name + "/" + ch.name + " 폭이 부모보다 좁다");
                n++;
            }
            return n;
        }

        /// <summary>
        /// T57 — «카드가 제 판을 입었는가». 컨테이너의 배경(line·face·bg)은 레이아웃 칸이 아니라
        /// 부모를 채워야 한다: 레이아웃 칸이 되면 <see cref="Image"/> 가 스프라이트 크기만큼의 **빈 막대**로
        /// 한 줄 차지하고(원작에 없는 검은·흰 막대) 카드에는 판이 없어져 글자가 뒷화면 위에 겹쳐 보였다.
        /// 레이아웃 그룹이 하나도 없는 팝업(자리를 손으로 놓는 꼴)은 카드 한 장만 본다.
        /// </summary>
        private static void AssertDressed(Transform root, string what)
        {
            Canvas.ForceUpdateCanvases();
            int checkedBg = 0;
            foreach (Transform box in root.GetComponentsInChildren<Transform>(true))
            {
                if (box.GetComponent<LayoutGroup>() == null) continue;
                checkedBg += AssertFills(box, what, true);
            }
            if (checkedBg > 0) return;
            // 자리를 손으로 놓는 팝업(ForgeAutoPopup: 카드에 레이아웃 그룹이 없고 목록만 Column)은 카드 한 장을 본다.
            Transform card = FindIn(root, "card");
            Assert.IsNotNull(card, what + ": 레이아웃 칸 안의 배경도 card 도 없다 — 자가 헛돈다");
            Assert.Greater(AssertFills(card, what, false), 0, what + ": 카드에 판(line·face)이 없다");
        }

        /// <summary>화면에 보이는 ✕ 개수(원작은 화면당 하나 · T57).</summary>
        private static int XButtons()
        {
            int n = 0;
            foreach (Transform t in UiRoot.Instance.App.GetComponentsInChildren<Transform>(false))
                if (t.name == "x-btn") n++;
            return n;
        }

        private static Transform FindIn(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
    }
}
