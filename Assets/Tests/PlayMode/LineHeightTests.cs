using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T354 3회차 — 표의 줄높이가 **실제 줄 간격**으로 나오는가(단위 옮김의 실물 검산): TMP `lineSpacing` 은 em/100 이라
    /// `(배수 − 자산 줄높이 비율) × 100` 이어야 첫·둘째 줄 기준선 차 ÷ 글자 크기 = 정본 배수가 된다. 값은 표에서 읽는다.
    /// </summary>
    public class LineHeightTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = Time.realtimeSinceStartup;
            while (!DungeonUiHost.Ready || UiRoot.Instance == null)
            {
                Assert.Less(Time.realtimeSinceStartup - t, 20f, "DungeonUiHost/UiRoot 가 20초 안에 준비되지 않았다");
                yield return null;
            }
            yield return null;
        }

        static Transform Find(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++) { Transform r = Find(t.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        [UnityTest]
        public IEnumerator 표의_배수가_실제_줄_간격이_된다_합성_글자_셋()
        {
            yield return Boot();
            RectTransform box = UiKit.Box(UiRoot.Instance.App, "t354-box");
            UiKit.Place(box, 0f, 0f, UiKit.RefW * 0.6f, UiKit.RefH * 0.5f);
            string longText = "정본 줄높이 검산 — 이 글은 상자 폭을 넘어 두 줄 이상으로 꺾여야 한다. 줄 간격은 표가 정한다. 첫째 둘째 셋째 넷째.";
            string[] keys = { "asc_focus_eff_lh", "rates_tip_lh", "chat_preview_lines_lh" };   // 1.5 · 1.4 · 1.25 — 기본(1.448)보다 넓게·좁게·더 좁게
            foreach (string key in keys)
            {
                TextMeshProUGUI t = UiKit.Text(box, "t-" + key, TextKind.Sub, longText, "pp_ink", TextAlignmentOptions.TopLeft);
                t.textWrappingMode = TextWrappingModes.Normal;
                UiKit.Place(t.rectTransform, 0f, 0f, UiKit.RefW * 0.6f, UiKit.RefH * 0.5f);
                double r = LineHeight.Apply(t, key);
                Assert.AreEqual(LineHeight.Table.Get(key), r, 1e-9, key + ": 배수 키는 표값 그대로");
                FaceInfo f = t.font.faceInfo;
                Assert.AreEqual(LineHeightRules.TmpLineSpacing(r, f.lineHeight, f.pointSize), t.lineSpacing, 1e-4f, key + ": lineSpacing = (r − 자산 비율) × 100");
                yield return null;
                double measured = LineHeight.MeasuredRatio(t);
                Assert.Greater(t.textInfo.lineCount, 1, key + ": 두 줄 이상이어야 잰다");
                Assert.AreEqual(r, measured, 0.02, key + ": 첫·둘째 줄 기준선 차 ÷ 글자 크기 = 정본 배수 (자산 기본 1.448 이 아니라)");
                Object.Destroy(t.gameObject);
            }
            Object.Destroy(box.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 승천_초점_효과_글줄은_정본_1_5_배수로_선다()
        {
            yield return Boot();
            AscendPopup.Open("forge");
            yield return null;
            Transform eff = Find(AscendPopup.Root, "eff");
            Assert.IsNotNull(eff, "초점 카드의 효과 글줄");
            TextMeshProUGUI t = eff.GetComponent<TextMeshProUGUI>();
            double r = LineHeight.Table.Get("asc_focus_eff_lh");
            FaceInfo f = t.font.faceInfo;
            Assert.AreEqual(LineHeightRules.TmpLineSpacing(r, f.lineHeight, f.pointSize), t.lineSpacing, 1e-4f, "정본 5635 .asc-focus-eff { line-height: 1.5 }");
            double measured = LineHeight.MeasuredRatio(t);
            Assert.Greater(t.textInfo.lineCount, 1, "효과 글줄은 세 줄(· 셋)");
            Assert.AreEqual(r, measured, 0.02, "실제 줄 간격이 표대로");
            AscendPopup.Close();
            yield return null;
        }

        static void AssertSpacing(TextMeshProUGUI t, string key, string what)
        {
            double r = LineHeight.Ratio(t, key);
            FaceInfo f = t.font.faceInfo;
            Assert.AreEqual(LineHeightRules.TmpLineSpacing(r, f.lineHeight, f.pointSize), t.lineSpacing, 1e-4f, what + ": lineSpacing = (표 배수 − 자산 비율) × 100");
        }

        /// <summary>T354 4회차 — 채팅 말풍선은 표 81 중 **앱 폭 키의 유일한 자리**(정본 3380 `calc(var(--app-w) * .0351)`):
        /// 배수가 아니라 px 라 글자 크기로 나눠 배수가 되는 길(<see cref="LineHeightRules.Ratio"/>)이 실물에서 도는지 본다.</summary>
        [UnityTest]
        public IEnumerator 채팅_말풍선은_정본_앱폭_0351_을_글자_크기로_나눈_배수로_선다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(MetaHost.Ready && PopupLayer.Instance != null && Hud.Instance != null); i++) yield return null;
            MetaHost h = MetaHost.Instance;
            Assert.IsNotNull(h, "MetaHost");
            Hud.Instance.ChatButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(PopupLayer.Instance.IsOpen(ChatScreen.Name), "채팅 줄 → 전체화면 채팅");
            // 두 줄 이상으로 꺾이는 긴 말을 하나 보낸다(실제 줄 간격을 재려면 둘째 줄이 있어야 한다)
            string longText = "줄높이를 재는 긴 말이다 · 말풍선 폭을 넘겨 둘째 줄로 꺾이도록 같은 말을 되풀이한다 · 줄높이를 재는 긴 말이다 · 말풍선 폭을 넘겨 둘째 줄로 꺾이도록";
            Assert.IsTrue(h.Chat.SendPlayer(h.ChatState, longText, h.Nickname, h.AvatarEmoji, h.Gender, h.NowMs), "플레이어 말 보내기");
            ChatScreen.OnChanged(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(ChatScreen.Name);
            TextMeshProUGUI mine = null; int bubbles = 0;
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.name != "text" || t.transform.parent == null || t.transform.parent.name != "bubble") continue;
                bubbles++;
                AssertSpacing(t, "chat_bubble_lh_w", "말풍선");
                if (t.text == longText) mine = t;
            }
            Assert.Greater(bubbles, 0, "말풍선이 하나는 있다");
            Assert.IsNotNull(mine, "방금 보낸 긴 말의 말풍선");
            double r = LineHeight.Ratio(mine, "chat_bubble_lh_w");
            Assert.AreEqual(LineHeightRules.RatioFromPx(LineHeight.Table.Get("chat_bubble_lh_w") * UiKit.RefW, mine.fontSize), r, 1e-9, "앱 폭 × .0351 ÷ 글자 크기");
            double measured = LineHeight.MeasuredRatio(mine);
            Assert.Greater(mine.textInfo.lineCount, 1, "긴 말은 두 줄 이상으로 꺾인다");
            Assert.AreEqual(r, measured, 0.02, "실제 줄 간격 = 앱 폭 배수 (자산 기본 1.448 이 아니라)");
            ChatScreen.Close(h);
            yield return null;
        }

        /// <summary>T354 4회차 — 탈것 업그레이드 팝업의 «재료 없음» 글(정본 805 `.mat-grid > .mat-empty { line-height: 1.35 }`):
        /// 새 세이브에서 한 마리만 소환하면 재료 후보가 0 이라 그 글이 선다.</summary>
        [UnityTest]
        public IEnumerator 탈것_업그레이드_팝업의_재료_없음_글은_정본_1_35_배수로_선다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(PetSkillHost.Ready && SkillPetSheet.Instance != null && SkillBar.Instance != null); i++) yield return null;
            PetSkillHost host = PetSkillHost.Instance;
            Assert.IsNotNull(host, "PetSkillHost");
            Assert.IsNotNull(host.Mounts, "탈것");
            while (host.SummonMult("mount") != 1) host.CycleSummonMult("mount");
            host.Winders = 100000;
            host.Sync();
            yield return null;
            MountSheet.Open();
            yield return null;
            Assert.IsTrue(MountSheet.IsOpen, "탈것 시트");
            MountSheet.SummonButton.onClick.Invoke();
            yield return null;
            for (int k = 0; k < 4 && SkillSummonResultView.Current != null; k++) { SkillSummonResultView.Current.OnTap(); yield return null; }
            Assert.AreEqual(1, host.Mounts.Count(), "새 세이브 · x1 소환 = 한 마리(재료 후보 0)");
            MountSheet.OpenDetail(0);
            yield return null;
            SkillPetSheet sheet = SkillPetSheet.Instance;
            sheet.Modal.Find(MountSheet.DetailModal).Content.Find("btn-upgrade").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.IsTrue(sheet.Modal.IsOpen(MountUpgradePopup.ModalName), "업그레이드 팝업");
            Transform empty = Find(sheet.Modal.Find(MountUpgradePopup.ModalName).Content, "mat-empty");
            Assert.IsNotNull(empty, "재료 없음 글(.mat-empty)");
            AssertSpacing(empty.GetComponent<TextMeshProUGUI>(), "mat_grid_mat_empty_lh", "재료 없음 글");
            Assert.AreEqual(1.35, LineHeight.Table.Get("mat_grid_mat_empty_lh"), 1e-9, "정본 805");
            MountUpgradePopup.Close();
            yield return null;
        }

        /// <summary>T354 5회차 — 던전 시트의 안내 두 줄(정본 3854 `.sheet-sub { line-height: 1.4 }` · ui.js 4543): 폭 `sheet_sub_maxw` 안에서 둘째 줄로 꺾이므로 실제 줄 간격도 잰다.</summary>
        [UnityTest]
        public IEnumerator 던전_시트_안내_두_줄은_정본_1_4_배수로_선다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(DungeonSheet.Instance != null && UiRoot.Instance != null && UiRoot.Instance.TabBar != null); i++) yield return null;
            UiRoot.Instance.TabBar.OnTab("dungeon");
            yield return null;
            Assert.IsTrue(DungeonSheet.Instance.IsOpen, "던전 시트");
            Canvas.ForceUpdateCanvases();
            Transform sub = Find(DungeonSheet.Instance.transform, "sub");
            Assert.IsNotNull(sub, "안내 글(.sheet-sub)");
            TextMeshProUGUI t = sub.GetComponent<TextMeshProUGUI>();
            AssertSpacing(t, "sheet_sub_lh", "던전 안내");
            double r = LineHeight.Table.Get("sheet_sub_lh");
            Assert.AreEqual(1.4, r, 1e-9, "정본 3854");
            double measured = LineHeight.MeasuredRatio(t);
            Assert.Greater(t.textInfo.lineCount, 1, "안내는 두 줄로 꺾인다(폭 sheet_sub_maxw)");
            Assert.AreEqual(r, measured, 0.02, "실제 줄 간격 = 1.4 (자산 기본 1.448 이 아니라)");
            UiRoot.Instance.TabBar.OnTab("dungeon");
            yield return null;
        }

        /// <summary>T354 15회차 — 패스 팝업 안내문(정본 2734 `.pass-desc { line-height: 1.4 }`).
        /// 정본이 `<br>` 로 나눈 **두 줄**이라 줄 간격이 눈에 보이는 자리다(T383 이 그 줄 수를 지키는 자를 따로 세워 뒀다).</summary>
        [UnityTest]
        public IEnumerator 패스_안내문은_정본_1_4_배수로_선다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(MetaHost.Ready && UiRoot.Instance != null); i++) yield return null;
            PassPopup.Open(MetaHost.Instance);   // 다른 자들이 쓰는 꼴(UiIconsTests)
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            // T414 — `desc` 는 화면 넷(`PassPopup`·`Popups`·`LeagueSheet`·`TechPopups`)이 쓰는 이름이다.
            //   앱 뿌리부터 찾으면 남이 열어 둔 화면의 글줄을 잰다 — **패스 팝업 뿌리**에서만 찾는다.
            Popup pass = PopupLayer.Instance.Find(PassPopup.Name);
            Assert.IsNotNull(pass, "패스 팝업이 안 열렸다");
            Transform desc = Find(pass.Root, "desc");
            Assert.IsNotNull(desc, "패스 안내문(.pass-desc)");
            TextMeshProUGUI t = desc.GetComponent<TextMeshProUGUI>();
            AssertSpacing(t, "pass_desc_lh", "패스 안내문");
            Assert.AreEqual(1.4, LineHeight.Table.Get("pass_desc_lh"), 1e-9, "정본 2734");
            Assert.Greater(t.textInfo.lineCount, 1, "정본이 <br> 로 나눈 두 줄이다");
            Assert.AreEqual(LineHeight.Table.Get("pass_desc_lh"), LineHeight.MeasuredRatio(t), 0.02,
                            "실제 줄 간격 = 1.4(자산 기본 1.448 이 아니라)");
        }

        /// <summary>T354 14회차 — 던전 상세의 왼쪽 버튼 라벨 «이전 스테이지 / 소탕»(정본 5356 `.dgd-btn { line-height: 1.25 }`).
        /// 라벨을 만드는 `DungeonPopups.Pill` 은 T345 산 lock 이라 `DungeonDetailPopup` 이 그 자식을 집어 건다 — 두 줄이라 눈에 보이는 자리다.</summary>
        [UnityTest]
        public IEnumerator 던전_상세_소탕_버튼_라벨은_정본_1_25_배수로_선다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(DungeonSheet.Instance != null && UiRoot.Instance != null && UiRoot.Instance.TabBar != null); i++) yield return null;
            // 새 세이브로는 그 던전이 잠겨 팝업이 안 선다 — `DropShadowTests` 처럼 진행도를 먼저 올린다(런 790: «소탕 버튼 상자» 가 null 이었다).
            ForgeHost fh = ForgeHost.Instance;
            fh.S.BestChapter = 5; fh.S.BestStage = 1; fh.Pull();
            UiRoot.Instance.TabBar.OnTab("dungeon");
            yield return null;
            Assert.IsTrue(DungeonSheet.Instance.IsOpen, "던전 시트");
            DungeonDetailPopup.Open("hammer");   // 다른 자들이 쓰는 던전 id 그대로
            yield return null; yield return null;
            Assert.IsTrue(DungeonDetailPopup.IsOpen, "던전 상세가 열린다");
            Canvas.ForceUpdateCanvases();
            // ⚠ `DungeonDetailPopup.SweepButton` 은 **정적 참조**라 팝업이 다시 서면 죽은 객체를 가리킨다
            //   (런 782: `MissingReferenceException` — C# 참조는 null 이 아니어서 IsNotNull 을 통과한 뒤 `.transform` 에서 터졌다).
            //   그래서 **살아 있는 나무에서** 찾는다(`DropShadowTests` 가 쓰는 길).
            Transform sweep = Find(UiRoot.Instance.App, "sweep");
            Assert.IsNotNull(sweep, "소탕 버튼 상자");
            Transform lab = Find(sweep, "label");
            Assert.IsNotNull(lab, "그 버튼의 라벨");
            TextMeshProUGUI t = lab.GetComponent<TextMeshProUGUI>();
            AssertSpacing(t, "dgd_btn_lh", "던전 상세 소탕 버튼");
            double r = LineHeight.Table.Get("dgd_btn_lh");
            Assert.AreEqual(1.25, r, 1e-9, "정본 5356");
            Assert.Greater(t.textInfo.lineCount, 1, "«이전 스테이지 / 소탕» 은 두 줄이다");
            Assert.AreEqual(r, LineHeight.MeasuredRatio(t), 0.02, "실제 줄 간격 = 1.25(자산 기본 1.448 이 아니라)");
        }

        /// <summary>T354 6회차 — 펫 업그레이드 팝업의 «재료 없음» 글(정본 805 · 탈것 쪽과 같은 자리): 새 세이브에서 알 하나 → 부화 → 즉시 부화면
        /// 다른 펫 0 · 알 0 이라 그 글이 선다(PetUiTests 의 길 그대로).</summary>
        [UnityTest]
        public IEnumerator 펫_업그레이드_팝업의_재료_없음_글은_정본_1_35_배수로_선다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(PetSkillHost.Ready && SkillPetSheet.Instance != null && SkillBar.Instance != null); i++) yield return null;
            PetSkillHost host = PetSkillHost.Instance;
            SkillPetSheet sheet = SkillPetSheet.Instance;
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            yield return null;
            sheet.Switch(SkillPetSheet.SubPets);
            yield return null;
            while (host.SummonMult("pet") != 1) host.CycleSummonMult("pet");
            host.EggCurrency = 100000;
            host.Gems = 100000;
            host.Sync();
            yield return null;
            Assert.AreEqual(0, host.Pets.State.Pets.Count, "새 세이브 · 펫 0");
            sheet.Pets.SummonButton.onClick.Invoke();
            yield return null;
            for (int k = 0; k < 4 && SkillSummonResultView.Current != null; k++) { SkillSummonResultView.Current.OnTap(); yield return null; }
            // 런 614: 새 세이브의 x1 소환도 알이 **둘** 나왔다(보너스 알) — «알 하나» 는 내 전제였지 규칙이 아니다. 하나만 부화시키고 나머지 알은 상태에서 비운다
            // (StartHatch 가 알을 목록에서 빼므로 알 목록을 가리키는 것은 없다 · Hatching 은 등급·시각만 쥔다).
            Assert.GreaterOrEqual(host.Pets.State.Eggs.Count, 1, "x1 소환 = 알 하나 이상");
            int hatching = host.Pets.State.Hatching.Count;
            sheet.Pets.OpenEggDetail(0);
            yield return null;
            sheet.Modal.Find(PetPanel.DetailModal).Content.Find("btn-hatch").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.IsNotNull(sheet.Pets.SkipButton(hatching), "부화 칸의 스킵");
            sheet.Pets.SkipButton(hatching).onClick.Invoke();
            yield return null;
            Assert.AreEqual(1, host.Pets.State.Pets.Count, "즉시 부화 → 펫 하나");
            host.Pets.State.Eggs.Clear();   // 남은 보너스 알은 재료 후보가 되므로 비운다 — 빈 글이 서는 조건 = 다른 펫 0 · 알 0
            host.Sync();
            yield return null;
            Assert.AreEqual(0, host.Pets.State.Eggs.Count, "알 0 → 재료 후보 0");
            sheet.Pets.OpenPetDetail(0);
            yield return null;
            sheet.Modal.Find(PetPanel.DetailModal).Content.Find("btn-upgrade").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.IsTrue(sheet.Modal.IsOpen(PetUpgradePopup.ModalName), "업그레이드 팝업");
            Transform empty = Find(sheet.Modal.Find(PetUpgradePopup.ModalName).Content, "mat-empty");
            Assert.IsNotNull(empty, "재료 없음 글(.mat-empty)");
            AssertSpacing(empty.GetComponent<TextMeshProUGUI>(), "mat_grid_mat_empty_lh", "펫 재료 없음 글");
            PetUpgradePopup.Close();
            yield return null;
        }

        /// <summary>
        /// T354 9회차 — 상점 특가 카드의 가격 단추 라벨은 정본 2959 `.shop-price-btn { line-height: 1.15 }` 로 선다.
        /// 산 lock 이 없는 파일만 골라 잇는 회차라, 이 자리는 `ShopSheet` 한 줄(라벨 TMP 를 꺼내 표를 건다)이다.
        /// </summary>
        [UnityTest]
        public IEnumerator 상점_가격_단추_라벨은_정본_1_15_배수로_선다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(UiRoot.Instance != null && UiRoot.Instance.TabBar != null); i++) yield return null;
            UiRoot.Instance.TabBar.OnTab("shop");
            yield return null;
            Canvas.ForceUpdateCanvases();
            // T414 — `price` 는 `PassPopup` 과 `ShopSheet` 둘이 쓴다 — **상점 시트 뿌리**에서만 찾는다.
            Popup shop = PopupLayer.Instance.Find(ShopSheet.Name);
            Assert.IsNotNull(shop, "상점 시트가 안 열렸다");
            Transform price = Find(shop.Root, "price");
            Assert.IsNotNull(price, "특가 카드의 가격 단추(price)");
            TextMeshProUGUI t = price.GetComponentInChildren<TextMeshProUGUI>();
            Assert.IsNotNull(t, "가격 단추 라벨");
            AssertSpacing(t, "shop_price_btn_lh", "상점 가격 단추");
            Assert.AreEqual(1.15, LineHeight.Table.Get("shop_price_btn_lh"), 1e-9, "정본 2959 .shop-price-btn { line-height: 1.15 }");
            Debug.Log("[T354] 상점 가격 단추 lineSpacing " + t.lineSpacing.ToString("0.000"));
        }

        /// <summary>
        /// T354 11회차 — 리그 목록 행의 이름은 정본 2337 `.league-name { line-height: 1.3 }` 로 선다.
        /// 목록은 탭만 열면 서므로 이 축에서 가장 싸게 잴 수 있는 자리다(보상 카드 안내 1.4 · 도전 행 이름 1.3 은 같은 회차의 다른 두 줄).
        /// </summary>
        [UnityTest]
        public IEnumerator 리그_행_이름은_정본_1_3_배수로_선다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(MetaHost.Ready && UiRoot.Instance != null && UiRoot.Instance.TabBar != null); i++) yield return null;
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            // 🚨 리그는 «pvp» 탭이 연다 — `OnTab("league")` 은 **없는 키**라 던진다(런 680 에서 내가 그렇게 깨뜨렸다 ·
            //    `ShopUiTests` 가 같은 자리에 주석까지 남겨 뒀다 · 런 191 실측). 여는 길은 `MetaHost.OpenLeague()` 다.
            MetaHost.Instance.OpenLeague();
            yield return null;
            Canvas.ForceUpdateCanvases();
            // 리그 **시트 뿌리 아래**에서만 찾는다 — 앱 전체를 뒤지면 다른 화면의 «name» 이 먼저 잡힌다.
            Popup lp = MetaHost.Instance.Popups.Find(LeagueSheet.Name);
            Assert.IsNotNull(lp, "리그 시트");
            Assert.IsTrue(lp.IsOpen, "리그 시트가 열려 있다");
            Transform name = Find(lp.Root, "name");
            Assert.IsNotNull(name, "리그 행 이름(name)");
            TextMeshProUGUI t = name.GetComponent<TextMeshProUGUI>();
            Assert.IsNotNull(t, "이름은 글자 하나다");
            AssertSpacing(t, "league_name_lh", "리그 행 이름");
            Assert.AreEqual(1.3, LineHeight.Table.Get("league_name_lh"), 1e-9, "정본 2337 .league-name { line-height: 1.3 }");
            Assert.AreEqual(1.4, LineHeight.Table.Get("league_reward_desc_lh"), 1e-9, "정본 2514 .league-reward-desc { line-height: 1.4 }");
            Assert.AreEqual(1.3, LineHeight.Table.Get("league_challenge_name_lh"), 1e-9, "정본 2632 .league-challenge-name { line-height: 1.3 }");
            // T354 12회차 — 스킬 상세 두 자리도 같은 칸에서 표값을 못 박는다(그 팝업을 여는 값이 커서 화면 자는 다음 회차 몫이다).
            Assert.AreEqual(1.45, LineHeight.Table.Get("skd_desc_lh"), 1e-9, "정본 5248 .skd-desc { line-height: 1.45 }");
            Assert.AreEqual(1.15, LineHeight.Table.Get("skd_passive_lh"), 1e-9, "정본 5255 .skd-passive { line-height: 1.15 }");
            Debug.Log("[T354] 리그 행 이름 lineSpacing " + t.lineSpacing.ToString("0.000"));
        }

        /// <summary>T354 16회차 — 플레이어 정보의 «보유 옵션» 목록은 **줄 피치**가 정본 값이다:
        /// `.pinfo-subs-list`(5600) `line-height: 1.14`. 같은 선택자가 3217 에도 있고(1.2) 구체성이 같아 **뒤 규칙이 이긴다** —
        /// 표의 두 키 중 임자는 `pinfo_subs_list_2_lh` 다. 클론은 `PopupKit.Label` 의 기본 줄 상자(글자 × 1.3)를 쓰고 있었다(+14%).
        /// 정본 3219 주석이 까닭을 적어 뒀다: «1.9→1.2: 종전 2.6%H 로 목록이 9%p 비대».</summary>
        [UnityTest]
        public IEnumerator 플레이어_정보_보유_옵션_줄은_정본_1_14_피치로_선다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(MetaHost.Ready && UiRoot.Instance != null); i++) yield return null;
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            MetaHost h = MetaHost.Instance;
            var saved = PlayerInfoPopup.SubLines;
            PlayerInfoPopup.SubLines = () => new List<string>
                { "공격력 +12.5%", "치명타 확률 +3.1%", "체력 +8.0%", "골드 획득 +4.4%" };
            try
            {
                PlayerInfoPopup.Open(h);
                yield return null; yield return null;
                Canvas.ForceUpdateCanvases();
                Popup p = PopupLayer.Instance.Find(PlayerInfoPopup.Name);
                Assert.IsNotNull(p, "플레이어 정보 팝업");
                Transform line = Find(p.Root, "sub");
                Assert.IsNotNull(line, "보유 옵션 줄(sub)");
                TextMeshProUGUI t = line.GetComponent<TextMeshProUGUI>();
                Assert.IsNotNull(t, "그 줄은 글자 하나다");
                double want = LineHeight.Ratio(t, "pinfo_subs_list_2_lh");
                Assert.AreEqual(1.14, LineHeight.Table.Get("pinfo_subs_list_2_lh"), 1e-9,
                    "정본 5600 .pinfo-subs-list { line-height: 1.14 } — 3217 의 1.2 는 같은 선택자의 앞 규칙이라 진다");
                LayoutElement le = line.GetComponent<LayoutElement>();
                Assert.IsNotNull(le, "줄 상자 높이를 쥔 LayoutElement");
                Assert.AreEqual((float)(want * t.fontSize), le.preferredHeight, 0.01f,
                    "보유 옵션 줄 상자 = 표 배수 × 글자 크기(전엔 공장 기본 1.3 이었다)");
                AssertSpacing(t, "pinfo_subs_list_2_lh", "보유 옵션 줄");
                Debug.Log("[T354] 보유 옵션 줄 상자 " + le.preferredHeight.ToString("0.00") + "px · 글자 " + t.fontSize.ToString("0.0"));
            }
            finally { PlayerInfoPopup.SubLines = saved; }
        }
    }
}
