using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Forging;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T109 11회차 — KNOWN 빈자리 중 lock 이 다 풀린 셋이 **실물에** 걸리는가.
    /// ⓐ 자동 제련 제목 `af-title`: 정본 `style.css` 3846 `h3.af-title { -webkit-text-stroke: .11em var(--pp-line) }`(5033 `.af-title 4px #fff` 는 특이도가 낮아 진다)
    /// ⓑ 자동 제련 [시작] `af-start`: 정본 5015 `.af-start { 4px #000 }` — 공용 Btn 표(2px)가 아니라 호출부 keylineKey 로 4px
    /// ⓒ 던전 상세 은알약(`Pill(Skin.DgdSilver)`): 정본 5363 `.dgd-btn.silver { 2px var(--pp-line) }` · 회색(잠김) 알약은 규칙 없음
    /// 폭의 픽셀 자체는 T104 `OutlineTests` 몫 — 여기서는 «걸렸는가 · 굵기 관계 · 색 · 안 걸릴 것은 안 걸렸는가» 를 본다.
    /// (rw-amt·rw-tick 은 T134 `RewardBurstTests` 가 이미 키라인까지 잰다 — 자 표만 그 자리로 옮겼다.)
    /// </summary>
    public class KeylineSpotsTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            yield return null;
        }

        [TearDown]
        public void CleanSave()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
        }

        private static Transform FindIn(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        private static void AssertLine(TextMeshProUGUI t, string what)
        {
            Color line = UiKit.C("pp_line");
            Color oc = t.outlineColor;   // Color32 — 채널로 잰다(런 262)
            Assert.AreEqual(line.r, oc.r, 2f / 255f, what + ": 키라인 색 R = var(--pp-line)");
            Assert.AreEqual(line.g, oc.g, 2f / 255f, what + ": 키라인 색 G");
            Assert.AreEqual(line.b, oc.b, 2f / 255f, what + ": 키라인 색 B");
            Assert.IsTrue(t.fontMaterial.IsKeywordEnabled("OUTLINE_ON"), what + ": 재질 OUTLINE_ON");
        }

        [UnityTest]
        public IEnumerator 자동_제련_제목은_11em_링_시작_버튼은_공용_2px_보다_굵은_4px_다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            h.S.BestChapter = 3; h.S.BestStage = 1;   // 2-10 해금 뒤
            Assert.IsTrue(h.AutoForgeUnlocked);
            h.OnAutoForgeBtn();
            yield return null;
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeAutoPopup.Name), "설정 팝업이 열린다");
            Transform root = h.Meta.Popups.Find(ForgeAutoPopup.Name).Root;
            RectTransform host = UiKit.Box(UiRoot.Instance.App, "t109-11-host");
            try
            {
                TextMeshProUGUI title = FindIn(root, "af-title").GetComponent<TextMeshProUGUI>();
                Assert.Greater(title.outlineWidth, 0f, "af-title: 정본 3846 .11em var(--pp-line)");
                AssertLine(title, "af-title");
                // 같은 크기 글자에 표 그대로 걸어 본 참조와 같은 폭이어야 한다(sheet_title = .11em)
                TextMeshProUGUI refT = UiKit.Text(host, "ref-title", TextKind.Title, "자동 제련", "pp_ink");
                UiKit.OutlinePx(refT, "pp_line", KeylineUi.Em("sheet_title", refT.fontSize));
                Assert.AreEqual(refT.outlineWidth, title.outlineWidth, 1e-5f, "af-title 폭 = sheet_title .11em × 글자 크기");

                Transform start = FindIn(root, "af-start");
                Assert.IsNotNull(start, "af-start 버튼");
                TextMeshProUGUI sl = start.Find("label").GetComponent<TextMeshProUGUI>();
                Assert.Greater(sl.outlineWidth, 0f, "af-start: 정본 5015 4px #000");
                AssertLine(sl, "af-start");
                float w = UiKit.L("league_challenge_w") * UiRoot.Instance.App.rect.width, bh = UiKit.H("btn_h");
                TextMeshProUGUI plain = PopupKit.Btn(host, "b-plain", "시작", "pp_blue", "pp_blue_dk", null, w, bh).transform.Find("label").GetComponent<TextMeshProUGUI>();
                Assert.Greater(sl.outlineWidth, plain.outlineWidth, "af-start 4px 는 공용 파란 버튼 2px 보다 굵다(같은 글자 종류)");
                TextMeshProUGUI ref4 = PopupKit.Btn(host, "b-ref", "시작", "pp_blue", "pp_blue_dk", null, w, bh, "stage_ink", TextKind.Button, false, "af_start").transform.Find("label").GetComponent<TextMeshProUGUI>();
                Assert.AreEqual(ref4.outlineWidth, sl.outlineWidth, 1e-5f, "af-start 폭 = 폭표 af_start(4px)");
            }
            finally
            {
                Object.Destroy(host.gameObject);
                h.Meta.Popups.Hide(ForgeAutoPopup.Name);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator 던전_은알약_라벨은_2px_링_회색_잠김_알약은_민글자다()
        {
            yield return Boot();
            RectTransform host = UiKit.Box(UiRoot.Instance.App, "t109-11-pill-host");
            try
            {
                Button silver = DungeonPopups.Pill(host, "enter", "입장", DungeonPopups.Skin.DgdSilver, TextKind.Button, null);
                TextMeshProUGUI sl = DungeonPopups.Root(silver).Find("label").GetComponent<TextMeshProUGUI>();
                Assert.Greater(sl.outlineWidth, 0f, "은알약: 정본 5363 2px var(--pp-line)");
                AssertLine(sl, "dgd-btn.silver");
                TextMeshProUGUI refT = UiKit.Text(host, "ref", TextKind.Button, "입장", "white");
                UiKit.OutlinePx(refT, "pp_line", KeylineUi.Px("dgd_btn_silver"));
                Assert.AreEqual(refT.outlineWidth, sl.outlineWidth, 1e-5f, "은알약 폭 = 폭표 dgd_btn_silver(2px)");

                Button gray = DungeonPopups.Pill(host, "enter-locked", "입장", DungeonPopups.Skin.Gray, TextKind.Button, null, -1f, false);
                TextMeshProUGUI gl = DungeonPopups.Root(gray).Find("label").GetComponent<TextMeshProUGUI>();
                Assert.AreEqual(0f, gl.outlineWidth, 1e-6f, "회색 잠김 알약은 정본에 규칙이 없다 — 민글자");
            }
            finally { Object.Destroy(host.gameObject); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator 판매_경고_제목에_정본_제목_묶음_키라인이_걸린다()
        {
            // 정본 style.css 3846 의 제목 묶음에 `h3.sellwarn-title` 이 들어 있다(.11em var(--pp-line)) ·
            // ui.js 3849 가 «정말 판매할까요?» 를 그 클래스로 찍는다(T109 13회차).
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeItem sold = h.Engine.RollItem();
            ForgeItem kept = h.Engine.RollItem();
            ForgeCraftPopup.ShowSellConfirm(h, sold, kept);
            yield return null;
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.SellName), "판매 경고가 열린다");
            Transform root = h.Meta.Popups.Find(ForgeCraftPopup.SellName).Root;
            RectTransform host = UiKit.Box(UiRoot.Instance.App, "t109-13-host");
            try
            {
                TextMeshProUGUI title = FindIn(root, "title").GetComponent<TextMeshProUGUI>();
                Assert.AreEqual("정말 판매할까요?", title.text);
                Assert.Greater(title.outlineWidth, 0f, "sellwarn-title: 정본 3846 .11em var(--pp-line)");
                AssertLine(title, "sellwarn-title");
                TextMeshProUGUI refT = UiKit.Text(host, "ref-sellwarn", TextKind.Body, "정말 판매할까요?", "pp_ink");
                UiKit.OutlinePx(refT, "pp_line", KeylineUi.Em("sheet_title", refT.fontSize));
                Assert.AreEqual(refT.outlineWidth, title.outlineWidth, 1e-5f, "폭 = sheet_title .11em × 글자 크기");
            }
            finally
            {
                Object.Destroy(host.gameObject);
                ForgeCraftPopup.HideSellConfirm(h);
            }
            yield return null;
        }
            [UnityTest]
        public IEnumerator 확률_정보_제목은_11em_링_건너뛰기_버튼은_4px_다()
        {
            // 정본 style.css 3846 제목 묶음의 `h3.fi-title`(.11em var(--pp-line) · ui.js 2054) · 5150 `.fi-card .fi-skip { 4px #000 }`(T109 14회차 · 마지막 KNOWN 둘).
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            h.S.Coins = 1000; h.S.Gems = 100; h.Pull();
            h.OnStartUpgrade();   // 업그레이드 중이어야 건너뛰기 버튼이 선다 · 시작하면 확률 정보 팝업이 열린다
            yield return null;
            Assert.IsTrue(h.Upgrading, "업그레이드 타이머");
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeInfoPopup.Name), "확률 정보 팝업");
            Transform root = h.Meta.Popups.Find(ForgeInfoPopup.Name).Root;
            RectTransform host = UiKit.Box(UiRoot.Instance.App, "t109-14-host");
            try
            {
                TextMeshProUGUI title = FindIn(root, "title").GetComponent<TextMeshProUGUI>();
                Assert.AreEqual("확률 정보", title.text);
                Assert.Greater(title.outlineWidth, 0f, "fi-title: 정본 3846 .11em var(--pp-line)");
                AssertLine(title, "fi-title");
                TextMeshProUGUI refT = UiKit.Text(host, "ref-fi-title", TextKind.Title, "확률 정보", "pp_ink");
                UiKit.OutlinePx(refT, "pp_line", KeylineUi.Em("sheet_title", refT.fontSize));
                Assert.AreEqual(refT.outlineWidth, title.outlineWidth, 1e-5f, "fi-title 폭 = sheet_title .11em × 글자 크기");

                Transform skip = FindIn(root, "fi-skip");
                Assert.IsNotNull(skip, "건너뛰기 버튼");
                Transform stack = skip.Find("label-stack");
                Assert.IsNotNull(stack, "세로 갈래 라벨(T110)");
                int pieces = 0;
                float w = UiKit.L("league_challenge_w") * UiRoot.Instance.App.rect.width, bh = UiKit.H("btn_h");
                TextMeshProUGUI ref4 = PopupKit.Btn(host, "b-ref", "건너뛰기", "pp_gray", "pp_gray_dk", null, w, bh, "stage_ink", TextKind.Sub, false, "fi_skip").transform.Find("label").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI plain = PopupKit.Btn(host, "b-plain", "건너뛰기", "pp_gray", "pp_gray_dk", null, w, bh, "stage_ink", TextKind.Sub).transform.Find("label").GetComponent<TextMeshProUGUI>();
                foreach (TextMeshProUGUI piece in stack.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    pieces++;
                    Assert.Greater(piece.outlineWidth, 0f, "fi-skip 조각 «" + piece.text + "»: 정본 5150 4px #000");
                    AssertLine(piece, "fi-skip");
                    Assert.AreEqual(ref4.outlineWidth, piece.outlineWidth, 1e-5f, "fi-skip 폭 = 폭표 fi_skip(4px)");
                }
                Assert.Greater(pieces, 0, "건너뛰기 글자 조각");
                Assert.AreEqual(0f, plain.outlineWidth, 1e-5f, "회색 면의 공용 버튼은 면 표대로 민글자(fi-skip 만 제 키로 4px)");
            }
            finally
            {
                Object.Destroy(host.gameObject);
                h.Meta.Popups.Hide(ForgeInfoPopup.Name);
            }
            yield return null;
        }
    }
}
