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
using Forge.Core.Data;
using Forge.Core.Skills;
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


        /// <summary>그 글자가 **실제로 그리는** 세로 구간(글자 조각의 글리프 상자 · SDF 여백 없음) — 칸 안 좌표.
        /// 면 지표(`lineInfo.ascender`)는 글꼴 줄 상자(1.448em)라 «칠해지는 칸» 이 아니다(T354 18회차 · 결정 738).</summary>
        static void InkSpan(TextMeshProUGUI t, out float top, out float bottom)
        {
            t.ForceMeshUpdate();
            TMP_TextInfo ti = t.textInfo;
            top = float.NegativeInfinity; bottom = float.PositiveInfinity;
            for (int i = 0; i < ti.characterCount; i++)
            {
                TMP_CharacterInfo ci = ti.characterInfo[i];
                if (!ci.isVisible) continue;
                top = Mathf.Max(top, ci.topRight.y);
                bottom = Mathf.Min(bottom, ci.bottomLeft.y);
            }
            if (top < bottom) { top = 0f; bottom = 0f; }
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
            /// <summary>T354 17회차 — 퀘스트 시트 안내(정본 3854 `.sheet-sub { line-height: 1.4 }` · 던전 5회차와 같은 선택자 · T178 반납으로 `QuestSheet.cs` 가 열렸다).
        /// 시트 폭 안에 한 줄로 서면 줄 간격은 눈에 안 보이지만 표가 lineSpacing 을 쥔다 — 꺾이면 실제 간격도 잰다.</summary>
        [UnityTest]
        public IEnumerator 퀘스트_시트_안내는_정본_1_4_배수로_선다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(MetaHost.Ready && MetaHost.Instance != null); i++) yield return null;
            MetaHost h = MetaHost.Instance;
            QuestSheet.Open(h);
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = h.Popups.Find(QuestSheet.Name);
            Assert.IsNotNull(p, "퀘스트 시트가 열려 있다");
            Transform sub = Find(p.Root, "sub");
            Assert.IsNotNull(sub, "안내 글(.sheet-sub)");
            TextMeshProUGUI t = sub.GetComponent<TextMeshProUGUI>();
            AssertSpacing(t, "sheet_sub_lh", "퀘스트 안내");
            double r = LineHeight.Table.Get("sheet_sub_lh");
            Assert.AreEqual(1.4, r, 1e-9, "정본 3854");
            if (t.textInfo.lineCount > 1) Assert.AreEqual(r, LineHeight.MeasuredRatio(t), 0.02, "두 줄로 꺾이면 실제 줄 간격도 1.4");
            h.Popups.Hide(QuestSheet.Name);
            yield return null;
        }

        /// <summary>T354 17회차 — 장착 스킬 Lv 배지(정본 4162 `.sk-mini small { line-height: 1.25; border: var(--ol1) solid }`):
        /// 한 줄 배지라 줄 간격은 안 보이지만 **배지 높이**가 «줄높이 × 글자 + 테 두 겹» 이라 표값이 곧 화면 치수다(종전 ×1.15 박힌 수).
        /// 장착 목록은 `MissingToastTests` 의 채비를 베껴 세이브에 직접 넣는다.</summary>
        [UnityTest]
        public IEnumerator 장착_스킬_Lv_배지는_정본_1_25_줄높이로_선다()
        {
            PetSkillHost.SuppressSave = true;
            yield return Boot();
            for (int i = 0; i < 600 && !(SkillPetSheet.Instance != null && PetSkillHost.Ready && UiRoot.Instance != null && UiRoot.Instance.TabBar != null); i++) yield return null;
            PetSkillHost host = PetSkillHost.Instance;
            SkillSystem sk = host.Skills;
            if (sk.State.Skills.Count == 0)
                foreach (SkillDef d in host.Data.Defs.SkillDefs) { sk.State.Skills.Add(d.Id, new SkillEntry()); break; }
            Assert.Greater(sk.State.Skills.Count, 0, "보유 스킬 하나");
            string id = sk.State.Skills.KeyAt(0);
            sk.State.Equipped.Clear();
            sk.State.Equipped.Add(id);
            UiRoot.Instance.TabBar.OnTab("summon");
            yield return null;
            SkillPetSheet.Instance.Switch(SkillPetSheet.SubSkills);
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            // T414 — 「sk-mini-」 는 펫 패널도 짓는 이름이라 스킬 패널 뿌리에서만 찾는다.
            Transform mini = Find(SkillPetSheet.Instance.Skills.transform, "sk-mini-" + id);
            Assert.IsNotNull(mini, "장착 줄의 스킬 조각 sk-mini-" + id);
            Transform small = mini.Find("small");
            Assert.IsNotNull(small, "Lv 배지(.sk-mini small)");
            TextMeshProUGUI t = small.Find("t").GetComponent<TextMeshProUGUI>();
            AssertSpacing(t, "sk_mini_small_lh", "Lv 배지");
            double r = LineHeight.Table.Get("sk_mini_small_lh");
            Assert.AreEqual(1.25, r, 1e-9, "정본 4162");
            float fs = UiCatalog.Instance.Kind(TextKind.Sub).size;
            float expect = fs * (float)r + PetSkillStyle.L("line1_px") * 2f;
            float got = ((RectTransform)small).rect.height;
            Assert.AreEqual(expect, got, 0.5f, "배지 높이 = 줄높이 × 글자 + 테 두 겹");
            Assert.Greater(Mathf.Abs(got - fs * 1.15f), 0.5f, "종전 ×1.15 박힌 수로 되돌아갔다");
            UiRoot.Instance.TabBar.OnTab("summon");
            yield return null;
        }
        /// <summary>T354 18회차 — 플레이어 정보 **머리줄 두 칸**의 줄 피치: 정본 3155 `.pinfo-id-text { line-height: 1.35 }`(왼쪽 이름·소속·전투력)과
        /// 3178 `.pinfo-right { line-height: 1.16 }`(오른쪽 대장간·총 피해·총 체력). 클론은 두 칸을 **한 수**(글자 × 1.25 · 코드에 박힌 수)로 그렸다 —
        /// 표가 두 수를 쥐고 있는데 부르는 곳이 0 이었다. 정본 3163~3177 주석이 오른쪽 칸을 두고 «폰트와 피치 중 하나만 만지면 안 된다» 고 경고하므로
        /// 잉크가 피치를 넘지 않는 것까지 같이 잰다(잉크 ≤ 피치).</summary>
        [UnityTest]
        public IEnumerator 플레이어_정보_머리줄은_왼쪽_1_35_오른쪽_1_16_피치로_갈린다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(MetaHost.Ready && UiRoot.Instance != null); i++) yield return null;
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            MetaHost h = MetaHost.Instance;
            PlayerInfoPopup.Open(h);
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(PlayerInfoPopup.Name);
            Assert.IsNotNull(p, "플레이어 정보 팝업");

            Assert.AreEqual(1.35, LineHeight.Table.Get("pinfo_id_text_lh"), 1e-9, "정본 3155 .pinfo-id-text");
            Assert.AreEqual(1.16, LineHeight.Table.Get("pinfo_right_lh"), 1e-9, "정본 3178 .pinfo-right");

            RectTransform name = (RectTransform)Find(p.Root, "name");
            RectTransform clan = (RectTransform)Find(p.Root, "clan");
            RectTransform cp = (RectTransform)Find(p.Root, "cp");
            RectTransform r1 = (RectTransform)Find(p.Root, "forge-lv");
            RectTransform r2 = (RectTransform)Find(p.Root, "atk");
            RectTransform r3 = (RectTransform)Find(p.Root, "hp");
            foreach (RectTransform rt in new[] { name, clan, cp, r1, r2, r3 })
                Assert.IsNotNull(rt, "머리줄의 여섯 줄이 다 서 있다");

            TextMeshProUGUI nt = name.GetComponent<TextMeshProUGUI>();
            float fs = nt.fontSize;
            float wantL = (float)(LineHeight.Ratio(fs, "pinfo_id_text_lh") * fs);
            float wantR = (float)(LineHeight.Ratio(fs, "pinfo_right_lh") * fs);
            Assert.Greater(wantL, wantR, "왼쪽 칸이 오른쪽보다 성글다 — 한 수로 그리던 때는 둘이 같았다");

            // 칸 높이 = 그 칸의 피치
            Assert.AreEqual(wantL, name.rect.height, 0.01f, "이름 줄 상자");
            Assert.AreEqual(wantL, cp.rect.height, 0.01f, "전투력 줄 상자");
            Assert.AreEqual(wantR, r1.rect.height, 0.01f, "대장간 줄 상자");
            Assert.AreEqual(wantR, r3.rect.height, 0.01f, "총 체력 줄 상자");

            // 줄 사이 피치도 같은 수다(UiKit.Place 는 위끝 기준이라 y 차가 곧 피치).
            Assert.AreEqual(wantL, name.anchoredPosition.y - clan.anchoredPosition.y, 0.01f, "이름 → 소속 피치");
            Assert.AreEqual(wantL, clan.anchoredPosition.y - cp.anchoredPosition.y, 0.01f, "소속 → 전투력 피치");
            Assert.AreEqual(wantR, r1.anchoredPosition.y - r2.anchoredPosition.y, 0.01f, "대장간 → 총 피해 피치");
            Assert.AreEqual(wantR, r2.anchoredPosition.y - r3.anchoredPosition.y, 0.01f, "총 피해 → 총 체력 피치");

            TextMeshProUGUI rt1 = r1.GetComponent<TextMeshProUGUI>();
            AssertSpacing(nt, "pinfo_id_text_lh", "왼쪽 이름 줄");
            AssertSpacing(rt1, "pinfo_right_lh", "오른쪽 대장간 줄");

            // 정본 주석의 함정 — 좁은 쪽(1.16) 피치가 **칠해지는 글자**보다 좁으면 «줄이 붙는다»(정본 3170).
            // ⚠ 1회차(런 985)는 그 잉크를 `lineInfo.ascender − descender` 로 쟀다가 빨갰다: 그 둘은 **글꼴 면 지표**
            //   (NotoSansKR 1.448em = 52.1px)라 «칠해지는 칸» 이 아니다 — 정본 주석이 잰 9.7px/12.0px 은 확대 크롭의 잉크다.
            //   값을 낮추지 말고 **재는 법**을 고친다(결정 677): 글자 조각의 `topRight`·`bottomLeft`(SDF 여백이 안 붙은 글리프 상자)로 재고,
            //   묻는 것도 «잉크 ≤ 피치» 가 아니라 **정본이 걱정한 그것 — 윗줄 잉크와 아랫줄 잉크가 겹치는가** 로 바꾼다.
            TextMeshProUGUI rt2 = r2.GetComponent<TextMeshProUGUI>();
            float t1, b1, t2, b2;
            InkSpan(rt1, out t1, out b1);
            InkSpan(rt2, out t2, out b2);
            Assert.Greater(t1 - b1, 0f, "오른쪽 첫 줄의 잉크를 못 쟀다");
            Assert.Greater(t2 - b2, 0f, "오른쪽 둘째 줄의 잉크를 못 쟀다");
            // 두 줄은 같은 카드 안에 위끝 기준으로 놓였다 — 칸의 y 에 그 칸 안 잉크 위치를 더하면 같은 자로 잴 수 있다.
            float bottom1 = r1.anchoredPosition.y + b1, top2 = r2.anchoredPosition.y + t2;
            Assert.Less(top2, bottom1,
                "오른쪽 두 줄의 잉크가 겹친다(윗줄 바닥 " + bottom1.ToString("0.0") + " ↔ 아랫줄 꼭대기 " + top2.ToString("0.0")
                + ") — 정본 3170 주석의 «피치만 줄이면 줄이 붙는다»");
            float ink = t1 - b1;
            Debug.Log("[T354] 머리줄 피치 왼 " + wantL.ToString("0.0") + "px · 오른 " + wantR.ToString("0.0") + "px · 글자 " + fs.ToString("0.0") + " · 오른쪽 잉크 " + ink.ToString("0.0"));
        }

    
        /// <summary>T354 20회차 — 상점 시트 안내(정본 3854 `.sheet-sub { line-height: 1.4 }` · ui.js 4960): 던전·퀘스트 시트와 같은 선택자·같은 키.</summary>
        [UnityTest]
        public IEnumerator 상점_시트_안내는_정본_1_4_배수로_선다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(MetaHost.Ready && MetaHost.Instance != null); i++) yield return null;
            MetaHost h = MetaHost.Instance;
            ShopSheet.Open(h);
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = h.Popups.Find(ShopSheet.Name);
            Assert.IsNotNull(p, "상점 시트가 열려 있다");
            Transform sub = Find(p.Root, "sub");
            Assert.IsNotNull(sub, "안내 글(.sheet-sub)");
            TextMeshProUGUI t = sub.GetComponent<TextMeshProUGUI>();
            AssertSpacing(t, "sheet_sub_lh", "상점 안내");
            Assert.AreEqual(1.4, LineHeight.Table.Get("sheet_sub_lh"), 1e-9, "정본 3854");
            if (t.textInfo.lineCount > 1) Assert.AreEqual(1.4, LineHeight.MeasuredRatio(t), 0.02, "두 줄로 꺾이면 실제 줄 간격도 1.4");
            h.Popups.Hide(ShopSheet.Name);
            yield return null;
        }

        /// <summary>T354 20회차 — 장비 상세(#forge-item-modal)의 lead 와 substat 행: 정본은 같은 선택자를 두 번 적어(3680·3681 → 3726·3729) **뒤 규칙**이 산다 —
        /// lead 1.13 · substat-row 1.21(표 `_2_lh`). lead 는 두 줄로 꺾이는 글이라 실제 줄 간격까지 잰다.</summary>
        [UnityTest]
        public IEnumerator 장비_상세_lead_와_substat_행은_정본_뒤_규칙_1_13_과_1_21_로_선다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && ForgeHost.Instance == null; i++) yield return null;
            ForgeHost h = ForgeHost.Instance;
            Assert.IsNotNull(h, "ForgeHost");
            ForgeInfoPopup.OpenList(h);
            yield return null;
            string age = h.Defs.Ages[0];
            string wt = h.Engine.WeaponsOfAge(age)[0];
            ForgeInfoPopup.OpenDetail(h, age, "weapon", 0, wt);
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = h.Meta.Popups.Find(ForgeInfoPopup.ItemName);
            Assert.IsNotNull(p, "장비 상세 팝업");
            Transform lead = Find(p.Root, "idet-lead");
            Assert.IsNotNull(lead, "lead(.idet-lead)");
            TextMeshProUGUI lt = lead.GetComponent<TextMeshProUGUI>();
            Assert.AreEqual(1.13, LineHeight.Table.Get("forge_item_modal_idet_lead_2_lh"), 1e-9, "정본 3726 — 3680 의 1.25 를 뒤 규칙이 덮는다");
            AssertSpacing(lt, "forge_item_modal_idet_lead_2_lh", "장비 상세 lead");
            lt.ForceMeshUpdate(true, true);
            if (lt.textInfo.lineCount > 1) Assert.AreEqual(1.13, LineHeight.MeasuredRatio(lt), 0.02, "두 줄 lead 의 실제 줄 간격 = 1.13");
            int rows = 0;
            foreach (TextMeshProUGUI r in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (!r.name.StartsWith("substat-")) continue;
                rows++;
                AssertSpacing(r, "forge_item_modal_idet_subs_substat_row_2_lh", "substat 행 " + r.name);
            }
            Assert.Greater(rows, 0, "substat 행이 하나도 없다");
            Assert.AreEqual(1.21, LineHeight.Table.Get("forge_item_modal_idet_subs_substat_row_2_lh"), 1e-9, "정본 3729 — 3681 의 1.264 를 뒤 규칙이 덮는다");
            ForgeInfoPopup.Close(h);
            yield return null;
        }

        /// <summary>T354 21회차 — 상단바 프로필 카드의 닉네임·전투력(정본 106 `.profile-info { line-height: 1.2 }` · 두 줄 세로 묶음)과
        /// 설정 실동작 버튼 라벨(정본 3124 `.settings-act { line-height: 1.15rem }` · rem 키라 글자 크기로 나눠 배수가 된다)이 표를 읽는다.</summary>
        [UnityTest]
        public IEnumerator 상단바_프로필_두_줄과_설정_실동작_라벨이_표의_줄높이를_읽는다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(MetaHost.Ready && PopupLayer.Instance != null && Hud.Instance != null); i++) yield return null;
            Assert.IsNotNull(Hud.Instance, "Hud");
            Transform nick = Find(Hud.Instance.transform, "nickname");
            Assert.IsNotNull(nick, "닉네임");
            Transform cpRow = Find(Hud.Instance.transform, "cp");
            Assert.IsNotNull(cpRow, "전투력 줄");
            Transform cpv = Find(cpRow, "value");
            Assert.IsNotNull(cpv, "전투력 수");
            Assert.AreEqual(1.2, LineHeight.Table.Get("profile_info_lh"), 1e-9, "정본 106");
            AssertSpacing(nick.GetComponent<TextMeshProUGUI>(), "profile_info_lh", "닉네임");
            AssertSpacing(cpv.GetComponent<TextMeshProUGUI>(), "profile_info_lh", "전투력");

            MetaHost h = MetaHost.Instance;
            ProfilePopup.Open(h);
            yield return null;
            ProfilePopup.SwitchView(h, "settings");
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = h.Popups.Find(ProfilePopup.Name);
            Assert.IsNotNull(p, "프로필 팝업");
            int acts = 0;
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.name != "label" || t.transform.parent == null || t.transform.parent.name != "act") continue;
                acts++;
                AssertSpacing(t, "settings_act_lh_rem", "설정 실동작 라벨 «" + t.text + "»");
                // rem 키: 배수 = 1.15rem(px) ÷ 글자 크기 — 표값을 그대로 배수로 쓰면 틀린다(정본은 절대 줄높이다)
                Assert.AreEqual(1.15 * PopupKit.Rem / t.fontSize, LineHeight.Ratio(t, "settings_act_lh_rem"), 1e-6, "rem 키의 배수");
            }
            Assert.Greater(acts, 0, "설정 갈래에 실동작 버튼 라벨이 하나도 없다");
            Assert.AreEqual(1.15, LineHeight.Table.Get("settings_act_lh_rem"), 1e-9, "정본 3124 · rem");
            ProfilePopup.Close(h);
            yield return null;
        }

        /// <summary>T354 21회차 — 데미지 숫자 조각(정본 ui.js 18 인라인 `line-height:1.25`)은 풀에서 만들 때 표를 읽는다.</summary>
        [UnityTest]
        public IEnumerator 데미지_숫자_조각은_정본_인라인_1_25_를_읽는다()
        {
            yield return Boot();
            UiRoot root = UiRoot.Instance;
            Assert.IsNotNull(root, "UiRoot");
            Forge.Game.Battle.DamageNumbers.ResetMaterials();
            var nums = new Forge.Game.Battle.DamageNumbers();
            nums.Spawn(new Vector3(0.1f, 1.0f, 0f), "354", "dmg-crit", 0, -40, 1);
            yield return null;
            RectTransform layer = Forge.Game.Battle.DamageNumbers.Layer(root);
            TextMeshProUGUI n = null;
            foreach (TextMeshProUGUI t in layer.GetComponentsInChildren<TextMeshProUGUI>(true)) if (t.text == "354") { n = t; break; }
            Assert.IsNotNull(n, "숫자 조각 «354»");
            Assert.AreEqual(1.25, LineHeight.Table.Get("ui_js_lh"), 1e-9, "정본 ui.js 18");
            AssertSpacing(n, "ui_js_lh", "데미지 숫자");
            nums.Clear();
            yield return null;
        }


        /// <summary>T354 22회차 — 장비 시트: 대장간 두 줄 버튼(정본 1637 `.forge-actions .btn { line-height: 1.15 }`)은 전에
        /// `lineSpacing = -20f` 가 코드에 박혀 있었다(T361 7회차). 표로 옮겨 실제 줄 간격이 1.15 인지 두 줄에서 잰다.
        /// 모루 망치 수(정본 984 `.anvil-btn { line-height: 1.05 }` 를 `small` 이 물려받는다)는 한 줄이라 lineSpacing 만 본다.</summary>
        [UnityTest]
        public IEnumerator 장비_시트_대장간_두_줄_버튼은_박힌_수가_아니라_표의_1_15_로_서고_모루_망치_수도_표를_읽는다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && ForgeHost.Instance == null; i++) yield return null;
            ForgeHost h = ForgeHost.Instance;
            Assert.IsNotNull(h, "ForgeHost");
            ForgeSheet.Render(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Transform fb = Find(UiRoot.Instance.Sheet, "forge-btn");
            Assert.IsNotNull(fb, "대장간 버튼(forge-btn)");
            TextMeshProUGUI t = fb.GetComponentInChildren<TextMeshProUGUI>();
            Assert.IsNotNull(t, "대장간 버튼 글");
            Assert.AreEqual(1.15, LineHeight.Table.Get("forge_actions_btn_lh"), 1e-9, "정본 1637");
            AssertSpacing(t, "forge_actions_btn_lh", "대장간 버튼");
            Assert.AreNotEqual(-20f, t.lineSpacing, "박힌 수 -20f 가 아니다");
            t.ForceMeshUpdate();
            yield return null;
            Assert.Greater(t.textInfo.lineCount, 1, "«대장간\\n레벨 N» 두 줄");
            Assert.AreEqual(LineHeight.Table.Get("forge_actions_btn_lh"), LineHeight.MeasuredRatio(t), 0.02, "실제 줄 간격 = 1.15(자산 기본 1.448 도 -20f 의 1.248 도 아니라)");

            Transform counter = Find(UiRoot.Instance.Sheet, "anvil-hammers");
            Assert.IsNotNull(counter, "모루 망치 수 칸(anvil-hammers)");
            Transform cnt = Find(counter, "count");
            Assert.IsNotNull(cnt, "망치 수 글(count)");
            Assert.AreEqual(1.05, LineHeight.Table.Get("anvil_btn_lh"), 1e-9, "정본 984");
            AssertSpacing(cnt.GetComponent<TextMeshProUGUI>(), "anvil_btn_lh", "망치 수");
        }

        /// <summary>T354 22회차 — 탭바 라벨(정본 index.html 의 `<span>` · 1707 `line-height: 1`)과 채팅 미리보기
        /// 두 줄(3251 `.chat-preview-lines { line-height: 1.25 }`)·뱃지(3248 `line-height: 1`)가 표를 읽는다. 셋 다 한 줄 글이라
        /// 화면은 안 변한다 — «표를 읽는 자리» 가 서는지만 본다(9회차의 길).</summary>
        [UnityTest]
        public IEnumerator 탭바_라벨과_채팅_미리보기_두_줄_뱃지가_표의_줄높이를_읽는다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(MetaHost.Ready && Hud.Instance != null && UiRoot.Instance.TabBar != null); i++) yield return null;
            Assert.IsNotNull(UiRoot.Instance.TabBar, "TabBar");
            Transform label = Find(UiRoot.Instance.TabBar.transform, "label");
            Assert.IsNotNull(label, "탭 라벨");
            Assert.AreEqual(1.0, LineHeight.Table.Get("tabbar_button_span_lh"), 1e-9, "정본 1707");
            AssertSpacing(label.GetComponent<TextMeshProUGUI>(), "tabbar_button_span_lh", "탭 라벨");

            // 런 1139: 띠는 Hud 오브젝트가 아니라 UiRoot.App 아래에 선다(PinnedColorSitesTests 531 과 같은 뿌리) — Hud 아래서 찾으면 null.
            Transform name = Find(UiRoot.Instance.App, "chat-preview-name");
            Transform msg = Find(UiRoot.Instance.App, "chat-preview-msg");
            Transform badge = Find(UiRoot.Instance.App, "chat-preview-badge");
            Assert.IsNotNull(name, "미리보기 이름"); Assert.IsNotNull(msg, "미리보기 글"); Assert.IsNotNull(badge, "뱃지");
            Transform n = Find(badge, "n");
            Assert.IsNotNull(n, "뱃지 수");
            Assert.AreEqual(1.25, LineHeight.Table.Get("chat_preview_lines_lh"), 1e-9, "정본 3251");
            Assert.AreEqual(1.0, LineHeight.Table.Get("chat_preview_badge_lh"), 1e-9, "정본 3248");
            AssertSpacing(name.GetComponent<TextMeshProUGUI>(), "chat_preview_lines_lh", "미리보기 이름");
            AssertSpacing(msg.GetComponent<TextMeshProUGUI>(), "chat_preview_lines_lh", "미리보기 글");
            AssertSpacing(n.GetComponent<TextMeshProUGUI>(), "chat_preview_badge_lh", "뱃지 수");
        }

        /// <summary>T354 23회차 — 패스 팝업의 나머지 다섯 글자 자리(정본 2711 `.pass-banner` 1.15 · 2743 `.pass-price` 1.3 ·
        /// 2764~2765 `.pass-header-row span` 1·1 · 2809 `.pass-milestone-label` 1). `pass_sword_lh`(2697 · 0)는 그림이라 글자 자리가 없다.</summary>
        [UnityTest]
        public IEnumerator 패스_팝업의_리본_가격_탭_두_라벨_마일스톤_필은_표의_줄높이를_읽는다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(MetaHost.Ready && UiRoot.Instance != null); i++) yield return null;
            PassPopup.Open(MetaHost.Instance);
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            Popup pass = PopupLayer.Instance.Find(PassPopup.Name);
            Assert.IsNotNull(pass, "패스 팝업이 안 열렸다");
            Transform band = Find(pass.Root, "band"); Assert.IsNotNull(band, "리본 띠");
            Transform title = band.Find("title"); Assert.IsNotNull(title, "리본 제목(.pass-banner)");
            Assert.AreEqual(1.15, LineHeight.Table.Get("pass_banner_lh"), 1e-9, "정본 2711");
            AssertSpacing(title.GetComponent<TextMeshProUGUI>(), "pass_banner_lh", "리본 제목");
            Transform price = Find(pass.Root, "price"); Assert.IsNotNull(price, "가격 페넌트");
            Transform pl = price.Find("label"); Assert.IsNotNull(pl, "가격 글(.pass-price)");
            Assert.AreEqual(1.3, LineHeight.Table.Get("pass_price_lh"), 1e-9, "정본 2743");
            AssertSpacing(pl.GetComponent<TextMeshProUGUI>(), "pass_price_lh", "가격 글");
            Transform free = Find(pass.Root, "free"), prem = Find(pass.Root, "premium");
            Assert.IsNotNull(free, "[무료] 탭"); Assert.IsNotNull(prem, "[프리미엄] 탭");
            Assert.AreEqual(1.0, LineHeight.Table.Get("pass_header_row_span_first_child_lh"), 1e-9, "정본 2764");
            Assert.AreEqual(1.0, LineHeight.Table.Get("pass_header_row_span_last_child_lh"), 1e-9, "정본 2765");
            AssertSpacing(free.Find("label").GetComponent<TextMeshProUGUI>(), "pass_header_row_span_first_child_lh", "[무료] 라벨");
            AssertSpacing(prem.Find("label").GetComponent<TextMeshProUGUI>(), "pass_header_row_span_last_child_lh", "[프리미엄] 라벨");
            Transform milestone = null;
            foreach (Transform tr in pass.Root.GetComponentsInChildren<Transform>(true))
                if (tr.name == "label" && tr.Find("text") != null) { milestone = tr.Find("text"); break; }
            Assert.IsNotNull(milestone, "마일스톤 필 글(.pass-milestone-label)");
            Assert.AreEqual(1.0, LineHeight.Table.Get("pass_milestone_label_lh"), 1e-9, "정본 2809");
            AssertSpacing(milestone.GetComponent<TextMeshProUGUI>(), "pass_milestone_label_lh", "마일스톤 필 글");
        }

        /// <summary>T354 23회차 — 기술 노드 팝업의 버튼(정본 4612 `.tech-btns .btn { line-height: 1.2 }` · ui.js 5586).
        /// 라벨은 공용 `DungeonPopups.Pill` 이 만들고 `TechPopups.BtnLh` 가 자식을 집어 건다 — [건너뛰기 ◆ N] 은 두 줄이라 눈에 보이는 자리다.</summary>
        [UnityTest]
        public IEnumerator 기술_노드_팝업의_버튼_라벨은_정본_1_2_배수로_선다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(MetaHost.Ready && UiRoot.Instance != null && UiRoot.Instance.TabBar != null); i++) yield return null;
            TechPanel p = TechPanel.OpenTechTree();
            yield return null;
            Assert.IsNotNull(p, "기술 트리");
            p.ShowBranch("power");
            yield return null;
            Assert.Greater(p.NodeIds.Count, 0, "노드");
            TechPopups.OpenNode(p.NodeIds[0]);
            yield return null;
            Assert.IsTrue(TechPopups.IsNodeOpen, "노드 팝업");
            Button b = TechPopups.ActionButton;
            Assert.IsNotNull(b, "[연구 시작] 버튼");
            Transform label = DungeonPopups.Root(b).Find("label");
            Assert.IsNotNull(label, "버튼 라벨");
            Assert.AreEqual(1.2, LineHeight.Table.Get("tech_btns_btn_lh"), 1e-9, "정본 4612");
            AssertSpacing(label.GetComponent<TextMeshProUGUI>(), "tech_btns_btn_lh", "기술 버튼 라벨");
            TechPopups.Close();
        }

        /// <summary>T354 23회차 — 제작 비교 팝업의 [장착] 두 줄 버튼(정본 3566 `#craft-modal .row .btn { line-height: 1.15 }`).
        /// 전엔 `ForgeCraftPopup.TwoLine` 이 `lineSpacing = -20f` 를 박고 있었다(ForgeSheet 22회차와 같은 자리 · §1).</summary>
        [UnityTest]
        public IEnumerator 제작_비교_팝업의_장착_버튼_라벨은_정본_1_15_배수로_선다()
        {
            yield return Boot();
            float t0 = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready) && t0 < 20f) { t0 += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost");
            ForgeHost h = ForgeHost.Instance;
            h.S.Hammers = 10; h.Pull();
            h.OnCraft();
            float t = 0f;
            while (!h.Meta.Popups.IsOpen(ForgeCraftPopup.Name) && t < 8f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "제작 뒤 비교 팝업이 열린다");
            Canvas.ForceUpdateCanvases();
            Transform equip = Find(h.Meta.Popups.Find(ForgeCraftPopup.Name).Root, "equip");
            Assert.IsNotNull(equip, "[장착] 버튼");
            TextMeshProUGUI label = equip.GetComponentInChildren<TextMeshProUGUI>();
            Assert.IsNotNull(label, "[장착] 라벨");
            Assert.AreEqual(1.15, LineHeight.Table.Get("craft_modal_row_btn_lh"), 1e-9, "정본 3566");
            AssertSpacing(label, "craft_modal_row_btn_lh", "[장착] 라벨");
            Assert.AreNotEqual(-20f, label.lineSpacing, "박힌 -20f 가 아니다");
            h.ResolveCraft("equip");
            yield return null;
        }

        /// <summary>T354 24회차 — 소환 바의 네 글자 자리(정본 5212 `.summon-btn` 1.2 · 5221 `.info-dot` 1 · 5217 `.summon-info` 1 · 4230 `.panel .btn.xs` 1.1 = x5 토글).</summary>
        [UnityTest]
        public IEnumerator 소환_바의_소환_버튼_정보_점_레벨_글_x5_토글은_표의_줄높이를_읽는다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(MetaHost.Ready && UiRoot.Instance != null && UiRoot.Instance.TabBar != null); i++) yield return null;
            UiRoot.Instance.TabBar.OnTab("summon");
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            // T414 — `summon-btn`·`x5-toggle` 은 스킬·펫·탈것 셋이 쓰는 이름이라 **스킬 패널 뿌리**에서만 찾는다.
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트");
            Transform skills = SkillPetSheet.Instance.Skills.transform;
            Transform btn = Find(skills, "summon-btn");
            Assert.IsNotNull(btn, "소환 버튼(.summon-btn)");
            Transform lab = btn.Find("label"); Assert.IsNotNull(lab, "소환 버튼 라벨");
            Assert.AreEqual(1.2, LineHeight.Table.Get("summon_btn_lh"), 1e-9, "정본 5212");
            AssertSpacing(lab.GetComponent<TextMeshProUGUI>(), "summon_btn_lh", "소환 버튼 라벨");
            Transform sub = btn.Find("sub");
            if (sub != null) AssertSpacing(sub.GetComponent<TextMeshProUGUI>(), "summon_btn_lh", "소환 버튼 small");
            Transform dot = Find(skills, "info-dot"); Assert.IsNotNull(dot, "정보 점(.info-dot)");
            Assert.AreEqual(1.0, LineHeight.Table.Get("info_dot_lh"), 1e-9, "정본 5221");
            AssertSpacing(dot.Find("t").GetComponent<TextMeshProUGUI>(), "info_dot_lh", "정보 점 i");
            Transform info = Find(skills, "summon-info"); Assert.IsNotNull(info, "소환 정보 칸(.summon-info)");
            Assert.AreEqual(1.0, LineHeight.Table.Get("summon_info_lh"), 1e-9, "정본 5217");
            AssertSpacing(info.Find("lv").GetComponent<TextMeshProUGUI>(), "summon_info_lh", "소환 레벨 글");
            Transform x5 = Find(skills, "x5-toggle"); Assert.IsNotNull(x5, "x5 토글(.btn.xs)");
            Assert.AreEqual(1.1, LineHeight.Table.Get("panel_btn_xs_lh"), 1e-9, "정본 4230");
            AssertSpacing(x5.Find("t").GetComponent<TextMeshProUGUI>(), "panel_btn_xs_lh", "x5 토글 글");
        }

        /// <summary>T354 24회차 — 리그 시트 제목(정본 2298 `.league-title` 1)과 수집 알약(2523 `.league-collect-pill` 1.3 · 보상이 있을 때만 선다).</summary>
        [UnityTest]
        public IEnumerator 리그_시트_제목과_수집_알약은_표의_줄높이를_읽는다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(MetaHost.Ready && UiRoot.Instance != null); i++) yield return null;
            LeagueSheet.Open(MetaHost.Instance);
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            Popup league = PopupLayer.Instance.Find(LeagueSheet.Name);
            Assert.IsNotNull(league, "리그 시트가 안 열렸다");
            TextMeshProUGUI title = null;
            foreach (TextMeshProUGUI t in league.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.name == "title" && t.text == "플래티넘 리그") { title = t; break; }
            Assert.IsNotNull(title, "리그 제목(.league-title)");
            Assert.AreEqual(1.0, LineHeight.Table.Get("league_title_lh"), 1e-9, "정본 2298");
            AssertSpacing(title, "league_title_lh", "리그 제목");
            Assert.AreEqual(1.3, LineHeight.Table.Get("league_collect_pill_lh"), 1e-9, "정본 2523");
            Transform collect = Find(league.Root, "collect");   // T414 — `collect` 는 오프라인 팝업도 쓰는 이름이라 리그 뿌리에서만
            if (collect != null && collect.Find("label") != null)
            {
                AssertSpacing(collect.Find("label").GetComponent<TextMeshProUGUI>(), "league_collect_pill_lh", "수집 알약 글");
                AssertSpacing(collect.Find("time").GetComponent<TextMeshProUGUI>(), "league_collect_pill_lh", "수집 알약 시간");
            }
        }

        /// <summary>T354 24회차 — 제작 정보의 i 원판(정본 5062 `.fi-info-btn` 1)만 걸고, 모루 줄의 같은 공장 원판(971 `.info-btn` · 줄높이 선언 없음)은 안 건다 — 이름으로 가른다.</summary>
        [UnityTest]
        public IEnumerator 제작_정보의_i_원판은_정본_1_배수이고_모루_줄의_i_원판은_안_건다()
        {
            yield return Boot();
            float t0 = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready) && t0 < 20f) { t0 += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost");
            ForgeHost h = ForgeHost.Instance;
            ForgeInfoPopup.Open(h);
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            Popup info = h.Meta.Popups.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(info, "제작 정보 팝업");
            Transform fi = Find(info.Root, "fi-info-btn"); Assert.IsNotNull(fi, "i 원판(.fi-info-btn)");
            Assert.AreEqual(1.0, LineHeight.Table.Get("fi_info_btn_lh"), 1e-9, "정본 5062");
            AssertSpacing(fi.Find("glyph").GetComponent<TextMeshProUGUI>(), "fi_info_btn_lh", "제작 정보 i");
            Transform anvilRow = Find(UiRoot.Instance.App, "anvil-row");   // T414 — `info-btn` 은 던전 팝업도 쓰는 이름이라 모루 줄 아래서만
            Transform anvil = anvilRow != null ? anvilRow.Find("info-btn") : null;
            if (anvil != null) Assert.AreEqual(0f, anvil.Find("glyph").GetComponent<TextMeshProUGUI>().lineSpacing, 1e-4f, "모루 줄의 i(.info-btn 971)는 줄높이 선언이 없어 손대지 않는다");
        }

        /// <summary>T354 25회차 — 산 lock 밖 파일의 마지막 글자 자리 넷: 소환 결과 등급 배지(정본 7067 `.sr-sub` 1.25) · 등급 칩(7127 `.sr-chip` 1.3) ·
        /// 리그 1~3위 순위 숫자(2576 `.lgr-rank-n` 1) · 확률 팝업 ⓘ(4648 `.rates-i` 1). 넷 다 한 줄 글이라 화면은 안 변한다 — «표를 읽는 자리» 가 서는지 본다.</summary>
        [UnityTest]
        public IEnumerator 소환_결과_배지_칩과_리그_순위_숫자와_확률_정보_i_가_표의_줄높이를_읽는다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(MetaHost.Ready && SkillPetSheet.Instance != null && PetSkillHost.Ready && PopupLayer.Instance != null); i++) yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트");
            Assert.AreEqual(1.25, LineHeight.Table.Get("sr_sub_lh"), 1e-9, "정본 7067");
            Assert.AreEqual(1.3, LineHeight.Table.Get("sr_chip_lh"), 1e-9, "정본 7127");
            Assert.AreEqual(1.0, LineHeight.Table.Get("lgr_rank_n_lh"), 1e-9, "정본 2576");
            Assert.AreEqual(1.0, LineHeight.Table.Get("rates_i_lh"), 1e-9, "정본 4648");

            // ① 소환 결과 — 항목 둘(등급 배지 글 Sub 있음) → 셀의 sr-sub 글과 발의 등급 칩 글
            var rolls = new System.Collections.Generic.List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = "가", Sub = "C", Qty = 1 },
                new SkillSummonResultView.Entry { Key = "sk:b", IconKey = "sk_fireball", Rarity = "rare", Name = "나", Sub = "R", Qty = 1 },
            };
            SkillSummonResultView v = SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", rolls, "rare", null);
            Assert.IsNotNull(v, "소환 결과 창");
            yield return null;
            Transform sub = Find(v.transform, "sr-sub");
            Assert.IsNotNull(sub, "등급 배지(sr-sub)");
            AssertSpacing(sub.Find("t").GetComponent<TextMeshProUGUI>(), "sr_sub_lh", "등급 배지 글");
            Transform chip = Find(v.transform, "sr-chip");
            Assert.IsNotNull(chip, "등급 칩(sr-chip)");
            AssertSpacing(chip.Find("t").GetComponent<TextMeshProUGUI>(), "sr_chip_lh", "등급 칩 글");
            for (int k = 0; k < 4 && SkillSummonResultView.Current != null; k++) { SkillSummonResultView.Current.OnTap(); yield return null; }

            // ② 확률 팝업 ⓘ
            SkillRatesPopup.Open(SkillPetSheet.Instance, "skill");
            yield return null;
            Assert.IsTrue(SkillPetSheet.Instance.Modal.IsOpen(SkillRatesPopup.ModalName), "확률 팝업");
            Transform ib = Find(SkillPetSheet.Instance.Modal.Find(SkillRatesPopup.ModalName).Content, "rates-i");
            Assert.IsNotNull(ib, "ⓘ 버튼(rates-i)");
            AssertSpacing(ib.Find("t").GetComponent<TextMeshProUGUI>(), "rates_i_lh", "ⓘ 글자");
            SkillPetSheet.Instance.Modal.Close(SkillRatesPopup.ModalName);
            yield return null;

            // ③ 리그 보상 1~3위 순위 숫자
            MetaHost h = MetaHost.Instance;
            LeagueSheet.OpenRewards(h);
            yield return null;
            Popup p = h.Popups.Find(LeagueSheet.RewardsName);
            Assert.IsNotNull(p, "리그 보상 팝업");
            int seen = 0;
            foreach (RectTransform tier in p.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (!tier.name.StartsWith("tier-")) continue;
                int rank = int.Parse(tier.name.Substring(5));
                if (rank > 3) continue;
                TextMeshProUGUI n = tier.Find("rank").Find("label").GetComponent<TextMeshProUGUI>();
                AssertSpacing(n, "lgr_rank_n_lh", tier.name + " 순위 숫자");
                seen++;
            }
            Assert.AreEqual(3, seen, "1·2·3위 셋");
        }
    }
}
