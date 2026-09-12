using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>T18 — 부팅 씬을 열면 UI 껍데기(앱 상자 9:16 · HUD · 장비 시트 자리 · 채팅 줄 · 탭 5개)가 서고 콘솔 빨강이 0 인가. 빨간 로그는 러너가 실패시킨다.</summary>
    public class UiSmokeTests
    {
        // 앞서 돈 다른 PlayMode 테스트(예: ShopUiTests 의 닉네임 변경)가 persistentDataPath 에 남긴 세이브를 SaveIo 가 부팅 때 읽으면
        // «부팅 닉네임 = 원작 defaultState» 가 깨진다(CI 런 27·38 실측). 부팅 스모크는 새 게임에서 시작해야 하므로 세이브를 비켜 두고 끝나면 되돌린다.
        private static readonly List<string> aside = new List<string>();

        [SetUp]
        public void SaveAside()
        {
            aside.Clear();
            string dir = Application.persistentDataPath;
            if (!Directory.Exists(dir)) return;
            foreach (string f in Directory.GetFiles(dir, "*.json"))
            {
                try { File.Copy(f, f + ".uismoke-bak", true); File.Delete(f); aside.Add(f); }
                catch (System.Exception e) { Debug.LogWarning("[UiSmokeTests] 세이브를 비켜 두지 못했다: " + f + " — " + e.Message); }
            }
        }

        [TearDown]
        public void SaveRestore()
        {
            foreach (string f in aside)
            {
                string bak = f + ".uismoke-bak";
                try { if (File.Exists(bak)) { File.Copy(bak, f, true); File.Delete(bak); } }
                catch (System.Exception e) { Debug.LogWarning("[UiSmokeTests] 세이브를 되돌리지 못했다: " + f + " — " + e.Message); }
            }
            aside.Clear();
        }

        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator 부팅_후_HUD_시트_채팅줄_탭바가_선다()
        {
            yield return Boot();

            UiRoot root = UiRoot.Instance;
            Assert.IsNotNull(root, "UiRoot 가 Bootstrap 아래에 서지 않았다");
            Assert.IsNotNull(Object.FindAnyObjectByType<Bootstrap>(), "Bootstrap 이 없다");
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, root.Canvas.renderMode);

            Rect app = root.AppScreenRect;
            Assert.AreEqual(Bootstrap.PortraitAspect, app.width / app.height, 0.01f, "앱 상자는 9:16");
            Assert.AreEqual(UiKit.RefW, root.App.sizeDelta.x, 0.01f);
            Assert.AreEqual(UiKit.RefH, root.App.sizeDelta.y, 0.01f);
            Rect sa = Screen.safeArea;
            if (Mathf.Approximately(sa.width, Screen.width) && Mathf.Approximately(sa.height, Screen.height))
            {
                Rect cam = Camera.main.pixelRect;
                Assert.AreEqual(cam.x, app.x, 2f, "세이프에어리어가 전체 화면이면 앱 상자 = 카메라 레터박스");
                Assert.AreEqual(cam.y, app.y, 2f);
                Assert.AreEqual(cam.width, app.width, 2f);
                Assert.AreEqual(cam.height, app.height, 2f);
            }

            Hud hud = root.Hud;
            Assert.IsNotNull(hud);
            Assert.AreSame(hud, Hud.Instance);
            Assert.AreEqual(UiCatalog.Instance.Boot.nickname, hud.Nickname, "부팅 닉네임 = 원작 defaultState");
            Assert.AreEqual(UiCatalog.Instance.Boot.stage, hud.StageLabel);
            Assert.AreEqual(UiCatalog.Instance.Boot.waves, hud.WaveCount, "메인 웨이브 노드 수");
            Assert.IsTrue(root.Sheet.gameObject.activeInHierarchy, "장비 시트 자리");
            Assert.IsTrue(root.Chat.gameObject.activeInHierarchy, "채팅 미리보기 줄");
            Assert.IsNotNull(hud.ProfileButton);
            Assert.IsNotNull(hud.ChatButton);

            TabBar tb = root.TabBar;
            Assert.IsNotNull(tb);
            CollectionAssert.AreEqual(new[] { "pvp", "dungeon", "summon", "quest", "shop" }, tb.Keys, "원작 탭 순서(주인 지시 2026-08-18)");
            Assert.AreEqual("PVP", tb.Label("pvp"));
            Assert.AreEqual("던전", tb.Label("dungeon"));
            Assert.AreEqual("소환", tb.Label("summon"));
            Assert.AreEqual("퀘스트", tb.Label("quest"));
            Assert.AreEqual("상점", tb.Label("shop"));
            Assert.IsNull(tb.ActiveTab, "부팅은 홈(전투 화면)");
            Assert.IsNotNull(tb.Panel("summon"), "소환 시트 패널");
            Assert.IsFalse(tb.Panel("summon").gameObject.activeSelf);
            Assert.IsNull(tb.Panel("shop"), "popup 탭은 시트가 없다");

            Assert.IsNotNull(UiFont.Primary, "NotoSans 런타임 폰트 애셋");
            Assert.AreEqual(UiCatalog.Instance.font.name + " (runtime)", UiFont.Primary.name);
        }

        [UnityTest]
        public IEnumerator 소환_탭은_흰_시트를_토글하고_열린_동안_빨간_X가_된다()
        {
            yield return Boot();
            TabBar tb = UiRoot.Instance.TabBar;
            string switched = "(none)";
            tb.Switched += t => switched = t ?? "(home)";

            tb.OnTab("summon");
            yield return null;
            Assert.AreEqual("summon", tb.ActiveTab);
            Assert.AreEqual("summon", switched);
            Assert.IsTrue(tb.Panel("summon").gameObject.activeInHierarchy, "흰 전체화면 시트가 열린다");
            Assert.IsTrue(tb.IsX("summon"), "열린 탭은 빨간 ✕");
            Assert.IsFalse(tb.IsX("shop"));

            tb.OnTab("summon");
            yield return null;
            Assert.IsNull(tb.ActiveTab, "다시 누르면 닫힌다(홈)");
            Assert.AreEqual("(home)", switched);
            Assert.IsFalse(tb.Panel("summon").gameObject.activeInHierarchy);
            Assert.IsFalse(tb.IsX("summon"));
        }

        [UnityTest]
        public IEnumerator popup_탭은_홈으로_돌아간_뒤_열기_요청만_보낸다()
        {
            yield return Boot();
            TabBar tb = UiRoot.Instance.TabBar;
            tb.OnTab("summon");
            yield return null;
            string asked = null;
            tb.OpenRequested += k => asked = k;

            tb.OnTab("shop");
            yield return null;
            Assert.AreEqual("shop", asked, "상점은 팝업 — T22 가 받는다");
            Assert.IsNull(tb.ActiveTab, "다른 탭 것을 열기 전에 시트를 닫는다(상호 배타)");
            Assert.IsFalse(tb.Panel("summon").gameObject.activeInHierarchy);
        }

        [UnityTest]
        public IEnumerator HUD_Set_표면이_글자를_바꾼다()
        {
            yield return Boot();
            Hud hud = Hud.Instance;
            hud.SetProfile("moonzzanf", "20.7b");
            hud.SetCurrency("782k", "62");
            hud.SetStage("어려움 4-1");
            hud.SetChatPreview("MilkMessiah: Ligma");
            hud.SetWaves(3, 2, 3);
            yield return null;
            Assert.AreEqual("moonzzanf", hud.Nickname);
            Assert.AreEqual("어려움 4-1", hud.StageLabel);
            Assert.AreEqual(3, hud.WaveCount, "던전은 노드 1~3 — 개수가 판을 따라간다");
            hud.SetWaves(5, 1, -1);
            yield return null;
            Assert.AreEqual(5, hud.WaveCount);
        }
    }
}
