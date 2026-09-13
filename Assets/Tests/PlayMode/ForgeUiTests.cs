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
using Forge.Core.CraftFx;

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
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "망치질(정본 1.5s) + 리빌(0.56s) 뒤 비교 팝업이 떠야 한다");
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

        /// <summary>T61 — 원작 shot-042120: «🔨 41307» 은 모루의 어두운 받침 위에 흰 글자 + 검정 외곽선(style.css 1625)로 놓여 밝은 시트 바닥에 흘러내리지 않는다.</summary>
        [UnityTest]
        public IEnumerator 모루_망치_수는_흰_글자_검정_외곽선으로_어두운_받침_위에_놓인다()
        {
            yield return Boot();
            Transform anvil = SheetChild("anvil");
            Assert.IsNotNull(anvil, "모루 그림");
            Transform baseT = null; foreach (Transform t in anvil) if (t.name == "base") baseT = t;
            Assert.IsNotNull(baseT, "받침 면");
            Transform counter = SheetChild("anvil-hammers");
            Assert.IsNotNull(counter, "해머 카운터");
            TextMeshProUGUI count = counter.GetComponentInChildren<TextMeshProUGUI>(true);
            Assert.IsNotNull(count, "망치 수 글자");
            yield return null;
            // ⓐ 글자 = 흰색 + 검정 외곽선(TMP OUTLINE_ON · 너비 > 0)
            Assert.Greater(count.outlineWidth, 0f, "검정 외곽선 너비");
            Assert.Less(Luma(count.outlineColor), 0.2f, "외곽선은 검정 계열");
            Assert.Greater(Luma(count.color), 0.9f, "글자는 흰색");
            // ⓑ 글자 중심이 받침 면 안에 있고, 받침은 어두운 주철이라 대비(밝기 차)가 문턱 이상
            Image baseImg = baseT.GetComponent<Image>();
            Assert.IsNotNull(baseImg);
            Vector3[] bc = new Vector3[4]; ((RectTransform)baseT).GetWorldCorners(bc);
            Vector3[] cc = new Vector3[4]; count.rectTransform.GetWorldCorners(cc);
            Vector3 center = (cc[0] + cc[2]) * 0.5f;
            Assert.IsTrue(center.x >= bc[0].x && center.x <= bc[2].x && center.y >= bc[0].y && center.y <= bc[2].y,
                "글자 중심 " + center + " 이 받침 " + bc[0] + "~" + bc[2] + " 안에 있어야 한다(원작 61%)");
            float contrast = Luma(count.color) - Luma(baseImg.color);
            Assert.GreaterOrEqual(contrast, 0.5f, "글자 밝기 − 받침 밝기 ≥ 0.5 (받침 " + baseImg.color + ")");
            Assert.Less(Luma(baseImg.color), 0.35f, "받침은 어두운 주철(catalog anvil_base)");
            AssertTextGate();
        }

        private static float Luma(Color c) { return 0.299f * c.r + 0.587f * c.g + 0.114f * c.b; }

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
            AssertCovers(ForgeInfoPopup.Name, "forge-list");                                   // T78
            AssertCovers(ForgeInfoPopup.ItemName, "forge-detail");                             // T78
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
            AssertCovers(ForgeAutoPopup.Name, "autoforge");                                    // T78
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

        /// <summary>
        /// T78 — 팝업 한 장이 «판을 덮었는가»: ⓐ 딤이 카탈로그 `modal_dim` 색 그대로 **앱 상자 네 귀퉁이**를 덮고
        /// ⓑ 카드 아래턱에 걸친 ✕ 가 **탭바 위**에 남는다(런 127 `screen_autoforge.png` 에서 ✕ 가 탭바에 반쯤 가렸다).
        /// 픽셀이 아니라 사각형으로 본다 — 촬영이 없는 런에서도 도는 단언이다.
        /// </summary>
        private static void AssertCovers(string popupName, string what)
        {
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(popupName);
            Assert.IsNotNull(p, what + ": 팝업이 안 열렸다");
            Transform dimT = FindIn(p.Root, "dim");
            Assert.IsNotNull(dimT, what + ": 딤 층이 없다");
            Image dim = dimT.GetComponent<Image>();
            Assert.IsNotNull(dim, what + ": 딤에 Image 가 없다");
            Assert.AreEqual(UiKit.C("modal_dim"), dim.color, what + ": 딤 색이 카탈로그 modal_dim 이 아니다");

            RectTransform app = UiRoot.Instance.App;
            Rect a = RectOf(app), d = RectOf(dim.rectTransform);
            Assert.LessOrEqual(d.xMin, a.xMin + 0.5f, what + ": 딤이 앱 상자 왼쪽을 덜 덮는다");
            Assert.GreaterOrEqual(d.xMax, a.xMax - 0.5f, what + ": 딤이 앱 상자 오른쪽을 덜 덮는다");
            Assert.LessOrEqual(d.yMin, a.yMin + 0.5f, what + ": 딤이 앱 상자 아래를 덜 덮는다");
            Assert.GreaterOrEqual(d.yMax, a.yMax - 0.5f, what + ": 딤이 앱 상자 위를 덜 덮는다");

            // ⓓ T78 2회차 — 정본 slug `modal-dim-tabbar`(style.css 3777~3784)가 이 셋을 «ⓑ 진짜 팝업» 으로
            //     가른다: 딤이 탭바까지 덮고(z 40/42) 닫기는 카드의 ✕ 다. 그래서 탭바 위 층에 서야 한다.
            Assert.IsTrue(p.AboveTabBar, what + ": 딤이 탭바 아래 층이라 탭바가 안 덮인다(정본 z-index 40)");
            Assert.Greater(PopupLayer.Instance.OverLayer.GetSiblingIndex(), UiRoot.Instance.TabBand.GetSiblingIndex(),
                what + ": modals-over 가 탭바보다 아래에 꽂혔다");

            Transform x = FindIn(p.Root, "x-btn");
            if (x == null) return;
            Rect xr = RectOf((RectTransform)x);
            float tabTopY = a.yMin + (1f - UiKit.L("tabbar_top")) * a.height;   // 화면 좌표는 아래가 0
            Assert.GreaterOrEqual(xr.yMin, tabTopY - 0.5f,
                what + ": 닫기 ✕ 아래턱이 탭바 위쪽(" + tabTopY.ToString("0.0") + ")보다 아래다 — 탭바가 ✕ 를 가린다");
        }

        /// <summary>월드 코너 넷 → 화면 사각형.</summary>
        private static Rect RectOf(RectTransform rt)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            float x0 = Mathf.Min(c[0].x, c[2].x), x1 = Mathf.Max(c[0].x, c[2].x);
            float y0 = Mathf.Min(c[0].y, c[2].y), y1 = Mathf.Max(c[0].y, c[2].y);
            return new Rect(x0, y0, x1 - x0, y1 - y0);
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
        /// <summary>
        /// T87 2·3회차 — 두들기는 동안 모루와 시트가 **정본 키프레임(`AnvilFxSpec`)** 대로 움직이는가.
        /// 정본은 `.anvil-btn.striking`(`anvilbump`)과 `#equip-sheet.shaking`(`sheetshake`)을 같은 클럭(`ANVIL_FX_MS` 1500ms)으로 돌리고,
        /// **두들기는 1.5초 동안 모루가 그대로 보인다**(카드는 `done()` 에서야 얹힌다) — 클론은 세이브가 부른 `Rerender` 가 모루를 카드로 갈아 치워
        /// 연출이 통째로 사라졌었다(런 157). 시트는 다시 그려질 수 있으므로 **칸은 매번 다시 찾는다**.
        /// </summary>
        [UnityTest]
        public IEnumerator 두들기면_모루와_시트가_정본_키프레임대로_움직인다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            h.S.Hammers = 30;
            ForgeSheet.Render(h);
            yield return null;

            RectTransform sheet = UiRoot.Instance.Sheet;
            Vector2 sheetHome = sheet.anchoredPosition;
            RectTransform rest = Anvil(sheet);
            Assert.IsNotNull(rest, "쉬는 화면에 모루가 없다");
            float restY = rest.anchoredPosition.y, restScaleY = rest.localScale.y;

            h.OnCraft();
            yield return null;
            AnvilFx fx = sheet.GetComponent<AnvilFx>();
            Assert.IsNotNull(fx, "두들기기 러너가 안 붙었다");
            Assert.IsTrue(fx.Running, "두들기는 동안은 돈다");
            Assert.IsTrue(h.Striking, "두들기는 중");
            Assert.IsNotNull(Anvil(sheet), "두들기는 동안 모루가 화면에서 사라졌다 — 정본은 1.5초 내내 모루를 보여 준다");

            // 정본 키프레임의 px 는 **절대 CSS px** 이라 촬영 배율로 환산해 바른다(카탈로그 `anvil_fx_px` · 5회차).
            float px = AnvilFx.PxScale;
            Assert.Greater(px, 1f, "CSS px → 기준 캔버스 px 환산이 카탈로그에 있다");

            double[] bump = new double[3];
            double[] shake = new double[2];
            for (int i = 0; i < AnvilFxSpec.StrikeMs.Length; i++)
            {
                RectTransform anvil = Anvil(sheet);
                Assert.IsNotNull(anvil, i + "타: 모루 칸이 없다");

                fx.SampleTo(AnvilFxSpec.StrikeMs[i]);
                AnvilFxSpec.Bump.Sample(AnvilFxSpec.StrikeStop[i], bump);
                AnvilFxSpec.SheetShake.Sample(AnvilFxSpec.StrikeStop[i], shake);
                // 러너의 기준은 «쉬는 자리»(다시 그려져도 같은 배치다) — CSS translateY 는 아래가 + 이고 유니티 UI 는 위가 + 라 부호가 뒤집힌다.
                Assert.AreEqual(restY - (float)bump[0] * px, anvil.anchoredPosition.y, 0.05f, i + "타: 모루가 눌린다");
                Assert.AreEqual(restScaleY * (float)bump[2], anvil.localScale.y, 0.01f, i + "타: 모루 scaleY 가 표값");
                Assert.AreEqual(sheetHome.y - (float)shake[1] * px, sheet.anchoredPosition.y, 0.05f, i + "타: 시트가 아래로 꽂힌다");
                Assert.Less(anvil.localScale.y, restScaleY, i + "타 순간엔 눌려 있다");
                Assert.Greater(Mathf.Abs(restY - anvil.anchoredPosition.y), (float)bump[0], i + "타: 환산 없이 CSS px 를 그대로 바르지 않았다");
            }

            // 정본 주석 «반동은 모루 그림에만» — 버튼(.anvil-btn)이 같이 움직이면 타격 오버레이가 반동을 타고 내려가 상대변위가 0 이 된다.
            RectTransform art = Anvil(sheet);
            RectTransform btn = art.parent as RectTransform;
            Assert.AreEqual("anvil-btn", btn.name, "모루 그림은 모루 버튼의 자식이다");
            Assert.AreEqual(1f, btn.localScale.y, 1e-4f, "버튼은 안 눌린다 — 반동은 그림 칸에만");
            // 축은 정본 `transform-origin: 50% 92%`(받침 접지면) — 피벗이 그 자리다(위에서부터 92% → 유니티 피벗 y = 1 − 0.92 를 viewBox 안에서).
            Assert.AreEqual(AnvilFxSpec.BumpOriginFrac[0], art.pivot.x, 0.02f, "축 x = 그림 가운데");
            Assert.Less(art.pivot.y, 0.5f, "축 y 는 받침 쪽(아래)이다 — 눌림이 위로 자라면 안 된다");

            fx.Stop();
            yield return null;
            Assert.AreEqual(sheetHome.x, sheet.anchoredPosition.x, 0.05f, "시트 제자리(x)");
            Assert.AreEqual(sheetHome.y, sheet.anchoredPosition.y, 0.05f, "시트 제자리(y)");
            h.CancelAnvilStrike();
            yield return null;
        }

        /// <summary>
        /// T87 6회차 — 하중을 먹는 것은 **강철 모루가 아니라 달군 쇳덩이**여야 한다(정본 `ui.js`·`style.css` 주석: «이게 없으면 하중을 모루가 대신 먹는다 —
        /// 강철 모루가 세로로 11% 눌리는 *고무 모루* 그림이었다»). 정지 상태에도 쇳덩이가 있고(정본 SVG 에 늘 들어 있다), 두들기면
        /// `anvilbillet`·`anvilbillethot`·`anvilbilletcool` 표대로 눌리고 달았다 식는다.
        /// </summary>
        [UnityTest]
        public IEnumerator 쇳덩이가_모루_대신_눌리고_백열이_켜졌다_식는다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            h.S.Hammers = 30;
            ForgeSheet.Render(h);
            yield return null;

            RectTransform sheet = UiRoot.Instance.Sheet;
            RectTransform billet = Named(sheet, "billet");
            Assert.IsNotNull(billet, "정지 상태에도 달군 쇳덩이가 상판 위에 있다(정본 SVG 에 늘 들어 있다)");
            // 축은 정본 `transform-origin: 55px 21.5px`(viewBox 132×86) — 밑면이 상판을 파고들지 않게.
            Assert.AreEqual(AnvilFxSpec.BilletOriginVb[0] / UiKit.L("anvil_vb_w"), billet.pivot.x, 0.01f, "쇳덩이 축 x");
            Assert.AreEqual(1.0 - AnvilFxSpec.BilletOriginVb[1] / UiKit.L("anvil_vb_h"), billet.pivot.y, 0.01f, "쇳덩이 축 y = 밑면");
            Assert.IsNotNull(Named(sheet, "ab-bar"), "쇳덩이 몸통");
            Assert.IsNotNull(Named(sheet, "ab-bar-line"), "키라인(식은 쇠색) — 없으면 상판에 찍힌 얼룩으로 읽힌다");
            Assert.IsNotNull(Named(sheet, "ab-top"), "윗면 띠");
            Assert.IsNotNull(Named(sheet, "ab-seam"), "접합선");

            Graphic hot = Poly(sheet, "ab-hot"), cool = Poly(sheet, "ab-cool");
            Assert.IsNotNull(hot, "백열 겹");
            Assert.IsNotNull(cool, "식은색 겹");
            Assert.AreEqual((float)AnvilFxSpec.BilletHot.Sample1(0), hot.color.a, 1e-3f, "정지 백열 = 트랙 0%(노란 단조열)");
            Assert.AreEqual(0f, cool.color.a, 1e-3f, "정지 식은색은 안 보인다");

            h.OnCraft();
            yield return null;
            AnvilFx fx = sheet.GetComponent<AnvilFx>();
            Assert.IsNotNull(fx, "두들기기 러너");

            double[] bump = new double[3], bil = new double[2];
            for (int i = 0; i < AnvilFxSpec.StrikeMs.Length; i++)
            {
                RectTransform bl = Named(sheet, "billet");
                RectTransform anvil = Anvil(sheet);
                fx.SampleTo(AnvilFxSpec.StrikeMs[i]);
                AnvilFxSpec.Billet.Sample(AnvilFxSpec.StrikeStop[i], bil);
                AnvilFxSpec.Bump.Sample(AnvilFxSpec.StrikeStop[i], bump);
                Assert.AreEqual((float)bil[0], bl.localScale.x, 0.01f, i + "타: 쇳덩이가 옆으로 퍼진다");
                Assert.AreEqual((float)bil[1], bl.localScale.y, 0.01f, i + "타: 쇳덩이가 눌린다");
                // 하중 귀속 — 쇳덩이의 압축이 모루 압축보다 한 자리 크다(정본이 «고무 모루» 라 부른 그림을 막는다)
                Assert.Greater(1f - bl.localScale.y, (1f - (float)bump[2]) * 10f, i + "타: 눌리는 것은 쇳덩이 쪽이다");
                Assert.AreEqual((float)AnvilFxSpec.BilletHot.Sample1(AnvilFxSpec.StrikeStop[i]), Op(sheet, "ab-hot"), 1e-3f, i + "타: 백열 피크");
                Assert.AreEqual((float)AnvilFxSpec.BilletCool.Sample1(AnvilFxSpec.StrikeStop[i]), Op(sheet, "ab-cool"), 1e-3f, i + "타: 식은색");
            }

            // 끝값 — 단조는 비가역이다(납작한 채 남는다) · 식은색이 가장 진하다
            fx.SampleTo(AnvilFxSpec.DurationMs);
            RectTransform last = Named(sheet, "billet");
            AnvilFxSpec.Billet.Sample(100, bil);
            Assert.AreEqual((float)bil[1], last.localScale.y, 0.01f, "끝: 눌린 채 남는다(.46)");
            Assert.AreEqual((float)AnvilFxSpec.BilletCool.Sample1(100), Op(sheet, "ab-cool"), 1e-3f, "끝: 식은 쇠색 .82");

            fx.Stop();
            yield return null;
            RectTransform rest = Named(sheet, "billet");
            Assert.AreEqual(1f, rest.localScale.y, 1e-3f, "연출을 걷으면 제 자세로");
            Assert.AreEqual((float)AnvilFxSpec.BilletHot.Sample1(0), Op(sheet, "ab-hot"), 1e-3f, "백열도 정지값으로");
            Assert.AreEqual(0f, Op(sheet, "ab-cool"), 1e-3f, "식은색도 0 으로");
            h.CancelAnvilStrike();
            yield return null;
        }

        /// <summary>
        /// T87 7회차 — **화면에 정말 칠해지는가**. 6회차는 «칸이 있다 · 값이 맞다» 만 재서 초록이었는데 런 173 샷의 모루 상자 안 픽셀 변화는 **0** 이었다
        /// (그때의 정점 메시 그래픽이 한 픽셀도 안 그렸다). 그래서 이 자는 UI 를 실제로 한 장 그려서 쇳덩이 자리에 «달군 쇠» 색이 있는지 센다.
        /// 그리는 길은 T27 `UiShotsTests.Capture` 와 같다(카메라 사본 → RenderTexture → ReadPixels · 배치모드에서 옳은 유일한 길).
        /// </summary>
        [UnityTest]
        public IEnumerator 쇳덩이가_화면에_실제로_칠해진다()
        {
            yield return Boot();
            if (NoGraphics()) Assert.Ignore("그래픽 장치가 없다 — 픽셀은 CI 의 유니티 잡이 본다");
            ForgeHost h = ForgeHost.Instance;
            ForgeSheet.Render(h);
            yield return null;

            RectTransform bar = Named(UiRoot.Instance.Sheet, "ab-bar");
            Assert.IsNotNull(bar, "쇳덩이 몸통 칸이 없다");
            int area; string info;
            // 달군 쇠 = 붉은 주황~노랑(파랑이 확연히 낮고 밝다) · 모루 상판보다 밝아야 한다
            int warm = CountPixels(bar, delegate(Color32 c) { return c.r > 170 && c.r > c.b + 60 && c.g >= c.b; }, out area, out info);
            Assert.Greater(warm, area / 5, "쇳덩이가 화면에 안 칠해졌다 — " + info);
            yield return null;
        }

        /// <summary>
        /// T87 9회차 — 두들기는 동안 **망치가 화면에 있다**. 값(자세)은 아래 테스트가 보고, 이 자는 «강철 색이 실제로 칠해졌는가» 만 본다
        /// (6~7회차의 교훈: 칸이 있고 값이 맞아도 한 픽셀도 안 그려질 수 있다).
        /// </summary>
        [UnityTest]
        public IEnumerator 두들기는_동안_망치가_화면에_보인다()
        {
            yield return Boot();
            if (NoGraphics()) Assert.Ignore("그래픽 장치가 없다 — 픽셀은 CI 의 유니티 잡이 본다");
            ForgeHost h = ForgeHost.Instance;
            h.S.Hammers = 30;
            ForgeSheet.Render(h);
            yield return null;
            Assert.IsNull(Named(UiRoot.Instance.Sheet, "hammer"), "쉬는 화면에는 망치가 없다(정본도 오버레이를 그때 만든다)");

            h.OnCraft();
            yield return null;
            RectTransform hammer = Named(UiRoot.Instance.Sheet, "hammer");
            Assert.IsNotNull(hammer, "두들기는 동안 망치 오버레이가 있어야 한다");
            AnvilFx fx = UiRoot.Instance.Sheet.GetComponent<AnvilFx>();
            Assert.IsNotNull(fx, "두들기기 러너");

            // 3타 직전(가장 크게 감아올린 뒤 내려오는 자리) — 망치가 확실히 화면 안이다
            fx.SampleTo(AutoForgeFxSpec.HitMs[2] - 40);
            yield return null;
            RectTransform head = Named(UiRoot.Instance.Sheet, "hm-head");
            Assert.IsNotNull(head, "망치 머리 칸");
            int area; string info;
            // 강철 = 채도 낮은 회색(빨강≈파랑) · 시트 바탕(밝은 회색)보다 어둡고 키라인(거의 검정)보다 밝다
            int steel = CountPixels(head, delegate(Color32 c) { return Mathf.Abs(c.r - c.b) < 40 && Mathf.Abs(c.g - c.b) < 40 && c.r > 45 && c.r < 205; }, out area, out info);
            Assert.Greater(steel, area / 6, "망치가 화면에 안 칠해졌다 — " + info);

            fx.Stop();
            yield return null;
            h.CancelAnvilStrike();
            yield return null;
        }

        /// <summary>
        /// T87 9회차 — 망치가 `afswing`·`afexit` 표대로 움직이는가. 정본은 `.af-hammer` 의 축을 **빌릿 윗면(viewBox 55,11)** 에 두고
        /// translate 를 **viewBox 단위**로 준다(SVG 자식이라 CSS px 가 아니다) · CSS 의 +y 는 아래, +각은 시계 방향이다.
        /// </summary>
        [UnityTest]
        public IEnumerator 망치가_정본_스윙표대로_움직이고_퇴장한다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            h.S.Hammers = 30;
            ForgeSheet.Render(h);
            yield return null;

            RectTransform sheet = UiRoot.Instance.Sheet;
            h.OnCraft();
            yield return null;
            AnvilFx fx = sheet.GetComponent<AnvilFx>();
            RectTransform hammer = Named(sheet, "hammer");
            Assert.IsNotNull(hammer, "망치 오버레이");
            Assert.IsNotNull(Named(sheet, "hm-head"), "머리");
            Assert.IsNotNull(Named(sheet, "hm-handle"), "손잡이");
            CanvasGroup cg = hammer.GetComponent<CanvasGroup>();
            Assert.IsNotNull(cg, "불투명도 묶음");

            // 축은 빌릿 윗면(55, 11) — 모루 그림 칸(viewBox 132×86) 안의 자리다.
            Assert.AreEqual(AutoForgeFxSpec.HitX / UiKit.L("anvil_vb_w"), hammer.pivot.x, 0.01f, "망치 축 x");
            Assert.AreEqual(1.0 - AutoForgeFxSpec.HitY / UiKit.L("anvil_vb_h"), hammer.pivot.y, 0.01f, "망치 축 y = 빌릿 윗면");

            float unit = hammer.rect.width / UiKit.L("anvil_vb_w");
            Assert.Greater(unit, 0.5f, "viewBox 한 단위가 화면 px 로 잡힌다");
            Vector2 home = HammerHome(hammer, unit);

            double[] v = new double[4];
            for (int i = 0; i < AutoForgeFxSpec.HitMs.Length; i++)
            {
                fx.SampleTo(AutoForgeFxSpec.HitMs[i]);
                AutoForgeFxSpec.SampleSwing(AutoForgeFxSpec.HitMs[i], v);
                Assert.AreEqual(home.x + (float)v[0] * unit, hammer.anchoredPosition.x, 0.5f, i + "타 접촉 x(타격마다 자리가 걸어간다)");
                Assert.AreEqual(home.y - (float)v[1] * unit, hammer.anchoredPosition.y, 0.5f, i + "타 접촉 y");
                Assert.AreEqual(-(float)v[2], hammer.localEulerAngles.z > 180f ? hammer.localEulerAngles.z - 360f : hammer.localEulerAngles.z, 0.5f, i + "타 각도(CSS 시계 방향 → 유니티 반시계)");
                Assert.AreEqual(1f, cg.alpha, 1e-3f, i + "타에는 다 보인다");

                // 스미어 — 접촉 직전 프레임에는 머리가 뒤로 늘어난다
                fx.SampleTo(AutoForgeFxSpec.SmearStop[i] / 100.0 * AnvilFxSpec.DurationMs);
                Assert.Greater(hammer.localScale.y, 1.1f, i + "타 스미어");
            }

            // 퇴장 — 1170ms 부터 왼쪽 위로 빠지며 사라진다
            fx.SampleTo(AutoForgeFxSpec.ExitStartMs + AutoForgeFxSpec.ExitDurMs);
            double[] e = new double[4];
            AutoForgeFxSpec.SampleExit(AutoForgeFxSpec.ExitStartMs + AutoForgeFxSpec.ExitDurMs, e);
            Assert.AreEqual(home.y - (float)e[1] * unit, hammer.anchoredPosition.y, 0.5f, "퇴장 끝 y(위로 빠진다)");
            Assert.AreEqual(0f, cg.alpha, 1e-3f, "퇴장 끝에는 안 보인다");
            Assert.Greater(hammer.anchoredPosition.y, home.y, "퇴장은 위쪽이다");

            fx.Stop();
            yield return null;
            h.CancelAnvilStrike();
            yield return null;
        }

        /// <summary>망치 칸의 «쉬는 자리» — 지금 자세에서 표값을 빼서 되찾는다(러너가 이미 한 번 발랐기 때문).</summary>
        private static Vector2 HammerHome(RectTransform hammer, float unit)
        {
            AnvilFx fx = UiRoot.Instance.Sheet.GetComponent<AnvilFx>();
            double[] v = new double[4];
            AutoForgeFxSpec.SampleSwing(fx.ElapsedMs, v);
            return new Vector2(hammer.anchoredPosition.x - (float)v[0] * unit, hammer.anchoredPosition.y + (float)v[1] * unit);
        }

        private static bool NoGraphics()
        {
            return SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
        }

        /// <summary>
        /// UI 를 한 장 그려(카메라 사본 → RenderTexture → `ReadPixels` · T27 `UiShotsTests.Capture` 와 같은 길) <paramref name="target"/> 의 화면 사각 안에서
        /// <paramref name="match"/> 를 만족하는 픽셀을 센다. 실패 문구에 쓸 정보(<paramref name="info"/>)도 같이 만든다.
        /// </summary>
        private static int CountPixels(RectTransform target, System.Func<Color32, bool> match, out int area, out string info)
        {
            UiRoot root = UiRoot.Instance;
            Canvas canvas = root.Canvas;
            RenderMode prevMode = canvas.renderMode;
            Camera prevCam = canvas.worldCamera;
            float prevPlane = canvas.planeDistance;
            RenderTexture prevActive = RenderTexture.active;
            int w = Mathf.Max(64, Screen.width), ht = Mathf.Max(64, Screen.height);
            RenderTexture rt = new RenderTexture(w, ht, 24, RenderTextureFormat.ARGB32);
            GameObject camGo = new GameObject("t87-pixel-cam");
            Camera cam = camGo.AddComponent<Camera>();
            Texture2D shot = null;
            area = 0; info = "";
            try
            {
                int uiLayer = canvas.gameObject.layer;
                if (Camera.main != null) cam.CopyFrom(Camera.main);
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.targetTexture = rt;
                cam.ResetProjectionMatrix();
                cam.cullingMask = 1 << uiLayer;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                root.Layout();
                Canvas.ForceUpdateCanvases();
                cam.Render();

                RenderTexture.active = rt;
                shot = new Texture2D(w, ht, TextureFormat.RGB24, false);
                shot.ReadPixels(new Rect(0f, 0f, w, ht), 0, 0);
                shot.Apply(false);

                Vector3[] corners = new Vector3[4];
                target.GetWorldCorners(corners);
                Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
                Vector2 b = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
                int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, b.x)), 0, w - 1);
                int x1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, b.x)), 0, w - 1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, b.y)), 0, ht - 1);
                int y1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, b.y)), 0, ht - 1);
                area = (x1 - x0 + 1) * (y1 - y0 + 1);
                Assert.Greater(area, 8, target.name + " 칸이 화면에서 너무 작다(" + (x1 - x0 + 1) + "×" + (y1 - y0 + 1) + ")");

                Color32[] px = shot.GetPixels32();
                int hit = 0;
                Color32 brightest = new Color32(0, 0, 0, 255);
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        Color32 c = px[y * w + x];
                        if (match(c)) hit++;
                        if (c.r + c.g + c.b > brightest.r + brightest.g + brightest.b) brightest = c;
                    }
                }
                info = target.name + " 칸 " + area + "픽셀(" + (x1 - x0 + 1) + "×" + (y1 - y0 + 1) + ") 중 맞는 색 " + hit
                       + "개 · 가장 밝은 픽셀 rgb " + brightest.r + "," + brightest.g + "," + brightest.b;
                return hit;
            }
            finally
            {
                RenderTexture.active = prevActive;
                canvas.renderMode = prevMode;
                canvas.worldCamera = prevCam;
                canvas.planeDistance = prevPlane;
                if (root != null) root.Layout();
                Canvas.ForceUpdateCanvases();
                if (shot != null) Object.Destroy(shot);
                Object.Destroy(camGo);
                rt.Release();
            }
        }

        /// <summary>시트 안에서 이름으로 칸 찾기(다시 그려질 수 있어 매번 찾는다).</summary>
        private static RectTransform Named(RectTransform sheet, string name)
        {
            foreach (RectTransform rt in sheet.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == name) return rt;
            return null;
        }

        /// <summary>겹 하나의 그래픽(불투명도는 그 색의 α · 정본 SVG 원소 `opacity`).</summary>
        private static Graphic Poly(RectTransform sheet, string name)
        {
            RectTransform rt = Named(sheet, name);
            return rt == null ? null : rt.GetComponent<Graphic>();
        }

        private static float Op(RectTransform sheet, string name)
        {
            Graphic g = Poly(sheet, name);
            return g == null ? -1f : g.color.a;
        }

        /// <summary>시트가 다시 그려지면 모루 칸도 새로 생긴다 — 이름으로 매번 찾는다(없으면 null).</summary>
        private static RectTransform Anvil(RectTransform sheet)
        {
            foreach (RectTransform rt in sheet.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "anvil") return rt;
            return null;
        }

    }
}
