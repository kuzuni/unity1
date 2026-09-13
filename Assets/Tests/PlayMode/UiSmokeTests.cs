using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
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
                // T54 3회차 되돌림 — 카메라 rect 는 앱 상자(레터박스) 그대로다(띠 rect 는 URP 가 세계를 안 그렸다 · 런 116).
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

        /// <summary>T65 — 플레이어 정보 팝업이 정본 `renderPlayerInfo` 의 뼈대를 낸다: 장비 8칸(장비 시트 조각 · 칸마다 아이콘) · 와이드 탈것 칸 · 출전 줄(오브 또는 «없음») · 미니 씬 폴백(🛡️ + 라벨 + 웨이브 핍) · 보유 옵션 · 콘솔 빨강 0.</summary>
        [UnityTest]
        public IEnumerator 플레이어_정보_팝업은_장비_칸_탈것_와이드_칸_출전_줄_폴백_핍을_낸다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            Assert.IsNotNull(h, "MetaHost 가 서지 않았다");
            PlayerInfoPopup.Open(h);
            yield return null;
            Popup p = h.Popups.Find(PlayerInfoPopup.Name);
            Assert.IsNotNull(p, "플레이어 정보 팝업이 열리지 않았다");
            Assert.IsNotNull(p.Root);
            Transform card = p.Root.Find("card");
            Assert.IsNotNull(card, "카드");
            // 장비 8칸 — 정본 SLOTS 순서 · 칸마다 아이콘(img)이 있다(빈 칸은 실루엣 · 찬 칸은 장비 아이콘 + Lv)
            string[] slots = Forge.Game.SaveIo.Data.Defs.Slots;
            Assert.AreEqual(8, slots.Length, "장비 부위 8");
            foreach (string slot in slots)
            {
                Transform c = card.Find("slot-" + slot);
                Assert.IsNotNull(c, "장비 칸 " + slot);
                Assert.IsNotNull(c.Find("frame"), slot + " 시대색 타일");
                Assert.IsNotNull(c.Find("img"), slot + " 아이콘");
                Assert.IsTrue(c.Find("slot-name") != null || c.Find("lv") != null, slot + " 빈 칸 라벨 또는 Lv 배지");
            }
            // 와이드 탈것 칸(pinfo-mount-wide) — 장비 칸 두 배 폭 · 누르면 탈것 시트
            RectTransform egg = card.Find("egg-cell") as RectTransform;
            Assert.IsNotNull(egg, "탈것 와이드 칸");
            RectTransform s0 = card.Find("slot-" + slots[0]) as RectTransform;
            Assert.Greater(egg.sizeDelta.x, s0.sizeDelta.x * 1.9f, "탈것 칸은 2칸 폭");
            Assert.IsNotNull(egg.GetComponent<UnityEngine.UI.Button>(), "탈것 칸은 버튼");
            Assert.IsTrue(egg.Find("slot-name") != null || egg.Find("face") != null, "빈 «탈것» 라벨 또는 탑승 얼굴");
            // 출전 줄 — 오브(sk-cell-*) 가 하나 이상이거나 «출전 중인 펫 없음»
            Transform row = card.Find("loadout");
            Assert.IsNotNull(row, "출전 줄");
            int orbs = 0;
            foreach (Transform t in row) if (t.name.StartsWith("sk-cell-")) { orbs++; Assert.IsNotNull(t.Find("sk-orb"), t.name + " 오브"); Assert.IsNotNull(t.Find("sk-lv"), t.name + " Lv 알약"); }
            Assert.IsTrue(orbs > 0 || row.Find("none") != null, "오브가 있거나 «출전 중인 펫 없음»");
            // 미니 씬 폴백(T54 전) — 🛡️ · 스테이지 라벨 · 웨이브 핍(전투 씬이 있고 던전이 아니면 총 웨이브 수만큼)
            Transform pv = card.Find("preview");
            Assert.IsNotNull(pv, "프리뷰 상자");
            if (pv.Find("stage") != null)
            {
                Assert.IsNotNull(pv.Find("shield"), "🛡️");
                var st = pv.Find("stage").GetComponent<TMPro.TextMeshProUGUI>();
                Assert.AreEqual(h.S.StageName(Forge.Game.SaveIo.Defs), st.text, "스테이지 라벨 = 세이브 스테이지");
                PlayerInfoPopup.WaveInfo wi = PlayerInfoPopup.Waves();
                int pips = 0;
                foreach (Transform t in pv) if (t.name.StartsWith("pip-")) pips++;
                Assert.AreEqual(!wi.Dungeon && wi.Total > 0 ? wi.Total : 0, pips, "웨이브 핍 수 = 총 웨이브(던전이면 0)");
            }
            Assert.IsNotNull(card.Find("subs"), "보유 옵션 목록");
            PlayerInfoPopup.Close(h);
            yield return null;
            Assert.IsFalse(h.Popups.IsOpen(PlayerInfoPopup.Name));
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

        [UnityTest]
        public IEnumerator T63_상단바_전투력이_0_이_아니고_채팅줄이_두_줄에_뱃지를_단다()
        {
            yield return Boot();

            // ⓐ 전투력 — 실측(런 83 screen_main.png)은 장비 8부위를 낀 채로도 «⚔ 0» 이었다.
            // 원인은 MetaHost.CombatPower 의 기본 대리자가 () => Big.Zero 인데 **아무도 안 꽂은 것**.
            // 맨몸(BareHeroStats Atk 15 · Hp 150)이라도 정본 식이면 0 이 아니다.
            float t = 0f;
            while (!(MetaHost.Ready && Forge.Game.Battle.BattleScene.Instance != null
                     && Forge.Game.Battle.BattleScene.Instance.Ready) && t < 20f)
            { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 준비되지 않았다");
            Forge.Game.Battle.BattleScene bs = Forge.Game.Battle.BattleScene.Instance;
            Assert.IsNotNull(bs, "전투 씬이 서지 않았다 — 전투력을 읽을 곳이 없다");

            MetaHost h = MetaHost.Instance;
            Assert.IsTrue(h.MyCp.Cmp(Forge.Core.Big.Zero) > 0,
                "상단바 전투력이 0 이다 — MetaHost.CombatPower 가 살아 있는 전투에 안 꽂혔다 (정본 Combat.combatPower)");
            Assert.AreEqual(0, h.MyCp.Cmp(bs.Battle.CombatPower()),
                "상단바 전투력은 Core Battle.CombatPower() 와 같은 값이어야 한다 — 식을 두 군데서 세지 않는다");

            // ⓑ 채팅 프리뷰 — 정본 renderChatPreview 는 말풍선 + «99» 뱃지 + 이름/메시지 두 줄이다.
            Hud hud = Hud.Instance;
            Assert.IsNotNull(hud, "HUD 가 없다");
            Transform badge = FindDeep(UiRoot.Instance.transform, "chat-preview-badge");
            Assert.IsNotNull(badge, "채팅줄에 «99» 뱃지가 없다 (정본 .chat-preview-badge)");
            TMPro.TextMeshProUGUI badgeText = badge.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            Assert.IsNotNull(badgeText, "뱃지에 글자가 없다");
            Assert.AreEqual(Hud.ChatBadgeText, badgeText.text);

            // 전투력은 전투 중 매초 바뀐다 — 그 갱신이 닉네임까지 다시 쓰면 안 된다(런 108 `HUD_Set` 이 그렇게 빨갰다).
            hud.SetProfile("moonzzanf", "20.7b");
            h.SyncHud();
            yield return null;
            Assert.AreEqual("moonzzanf", hud.Nickname,
                "전투력 갱신이 닉네임을 덮었다 — SyncHud 는 닉네임과 전투력을 따로 밀어야 한다");

            hud.SetChatPreview("Zephyr", "anyone want to trade tickets?");
            Assert.AreEqual("Zephyr", hud.ChatName, "이름 줄이 따로 서야 한다");
            Assert.AreEqual("anyone want to trade tickets?", hud.ChatMessage, "메시지 줄이 따로 서야 한다");
            Assert.IsFalse(hud.ChatMessage.Contains(":"), "한 줄로 이어 붙이던 «이름: 메시지» 꼴이 남아 있다");
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        [UnityTest]
        public IEnumerator T60_재화_알약에_상점을_여는_초록_플러스가_붙는다()
        {
            yield return Boot();
            float t = 0f;
            while (!MetaHost.Ready && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 준비되지 않았다");

            Hud hud = Hud.Instance;
            Assert.IsNotNull(hud.CoinPlusButton, "코인 알약에 «+» 가 없다 (정본 curIcoPlus)");
            Assert.IsNotNull(hud.GemPlusButton, "젬 알약에 «+» 가 없다");

            // 알약 안에 얹히고(아이콘 오른쪽 아래 모서리) 화면 밖으로 안 나간다.
            foreach (Button b in new[] { hud.CoinPlusButton, hud.GemPlusButton })
            {
                RectTransform rt = b.GetComponent<RectTransform>();
                Assert.IsNotNull(rt.GetComponentInChildren<Image>(true), "«+» 아이콘 그림이 없다");
                Vector3[] c = new Vector3[4];
                rt.GetWorldCorners(c);
                foreach (Vector3 p in c)
                {
                    Vector2 sp = RectTransformUtility.WorldToScreenPoint(null, p);
                    Assert.GreaterOrEqual(sp.x, 0f, b.name + " 가 화면 왼쪽 밖이다");
                    Assert.LessOrEqual(sp.x, Screen.width, b.name + " 가 화면 오른쪽 밖이다");
                }
            }

            // 누르면 상점이 열린다(원작 onclick="UI.openShop()").
            MetaHost h = MetaHost.Instance;
            PopupLayer popups = h.Popups;
            Assert.IsFalse(popups.IsOpen(ShopSheet.Name), "누르기 전에는 상점이 닫혀 있어야 한다");
            hud.CoinPlusButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(popups.IsOpen(ShopSheet.Name), "코인 «+» 를 누르면 상점이 열려야 한다");
        }
    

        /// <summary>
        /// T85 — 프로필/설정 팝업의 딤은 정본 `.modal { background: rgba(0,0,0,.5) }`(`style.css` 1721 · 주인 지시 «투명도 50%») **한 겹**이다.
        /// 촬영 런 141 의 «설정 뒤가 검다» 는 팝업 딤이 아니라 전투 사망 암전(T39 `BattleOverlay`)이 상단바까지 덮은 것이었다 — 그 띠는 이제 상단바 아래에서 시작한다.
        /// </summary>
        [UnityTest]
        public IEnumerator 프로필_설정_팝업의_딤은_정본_modal_dim_한_겹이고_전투_암전_띠는_상단바를_비켜_간다()
        {
            yield return Boot();
            float t = 0f;
            while (!MetaHost.Ready && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 준비되지 않았다");
            MetaHost h = MetaHost.Instance;
            h.OpenProfile();
            yield return null;
            Popup p = h.Popups.Find(ProfilePopup.Name);
            Assert.IsNotNull(p, "프로필 팝업");
            ProfilePopup.SwitchView(h, "settings");
            yield return null;
            Assert.AreEqual("settings", ProfilePopup.View);
            int dims = 0; Image dim = null;
            for (int i = 0; i < p.Root.childCount; i++)
            {
                Transform c = p.Root.GetChild(i);
                if (c.name == "dim") { dims++; dim = c.GetComponent<Image>(); }
            }
            Assert.AreEqual(1, dims, "딤은 한 겹(정본 .modal 하나)");
            Assert.IsNotNull(dim);
            Assert.AreEqual(UiKit.C("modal_dim").a, dim.color.a, 1e-3f, "딤 α = 카탈로그 modal_dim(정본 rgba(0,0,0,.5))");
            BattleOverlay ov = BattleOverlay.Ensure();
            if (ov != null)
            {
                Assert.AreEqual(1f - UiKit.L("topbar_h"), ov.Layer.anchorMax.y, 1e-4f, "전투 암전 띠는 상단바 아래에서 시작한다(정본 #fx-layer ⊂ #game-area · #topbar 는 밖)");
                Assert.IsFalse(ov.DeathActive, "팝업을 열었을 뿐 사망 암전은 안 돈다");
            }
            ProfilePopup.Close(h);
            yield return null;
            Assert.IsFalse(h.Popups.IsOpen(ProfilePopup.Name));
        }
    

        /// <summary>
        /// T97 — 플레이어 정보 팝업의 미니 씬: <c>BattlePreview</c> 가 <c>PlayerInfoPopup.PreviewStart/Stop</c> 훅에 꽂혀(원작 `Scene3D.previewStart`)
        /// 프리뷰 상자에 RT 카메라 그림(RawImage)을 채우고 폴백(🛡️·스테이지 라벨·핍)은 안 그린다 · 닫으면 카메라·RT 를 반납한다. 빨간 로그 0.
        /// </summary>
        [UnityTest]
        public IEnumerator 플레이어_정보_팝업의_미니_씬은_RT_카메라로_서고_닫으면_걷힌다()
        {
            yield return Boot();
            float t = 0f;
            while (!(MetaHost.Ready && Forge.Game.Battle.BattleScene.Instance != null && Forge.Game.Battle.BattleScene.Instance.Ready) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 준비되지 않았다");
            Assert.IsNotNull(Forge.Game.Battle.BattleScene.Instance, "전투 씬");
            Forge.Game.Battle.BattlePreview pv = Forge.Game.Battle.BattlePreview.Instance;
            Assert.IsNotNull(pv, "BattlePreview 가 Bootstrap 아래에 서지 않았다");
            Assert.IsNotNull(PlayerInfoPopup.PreviewStart, "PreviewStart 훅이 비었다(T97 이전 상태)");
            Assert.IsNotNull(PlayerInfoPopup.PreviewStop);
            Assert.IsFalse(pv.Active);
            MetaHost h = MetaHost.Instance;
            PlayerInfoPopup.Open(h);
            yield return null;
            yield return null;
            Popup p = h.Popups.Find(PlayerInfoPopup.Name);
            Assert.IsNotNull(p, "플레이어 정보 팝업");
            Transform preview = p.Root.Find("card/preview");
            Assert.IsNotNull(preview, "프리뷰 상자");
            Assert.IsTrue(pv.Active, "미니 씬 카메라가 돈다");
            Assert.IsNull(preview.Find("shield"), "미니 씬이 섰으면 폴백 🛡️ 는 없다");
            Assert.IsNull(preview.Find("stage"), "폴백 스테이지 라벨 없음");
            Transform canvasT = preview.Find("pinfo-scene-canvas");
            Assert.IsNotNull(canvasT, "RT 그림(RawImage)");
            var raw = canvasT.GetComponent<UnityEngine.UI.RawImage>();
            Assert.IsNotNull(raw);
            Assert.IsNotNull(raw.texture, "RawImage 에 RT 가 꽂혔다");
            Assert.AreSame(pv.Texture, raw.texture);
            Assert.IsNotNull(pv.PreviewCamera);
            Assert.AreSame(pv.Texture, pv.PreviewCamera.targetTexture);
            RectTransform prt = (RectTransform)preview;
            Assert.AreEqual(Mathf.RoundToInt(prt.rect.width), pv.Texture.width, 1, "RT 폭 = 상자 폭(화면 픽셀)");
            Assert.AreEqual(Mathf.RoundToInt(prt.rect.height), pv.Texture.height, 1, "RT 높이 = 상자 높이");
            Assert.AreEqual(Camera.main.transform.position, pv.PreviewCamera.transform.position, "본 카메라 자리에서 같은 씬");
            Assert.AreEqual(Camera.main.cullingMask, pv.PreviewCamera.cullingMask);
            PlayerInfoPopup.Close(h);
            yield return null;
            Assert.IsFalse(h.Popups.IsOpen(PlayerInfoPopup.Name));
            Assert.IsFalse(pv.Active, "닫으면 카메라·RT 반납");
            Assert.IsNull(pv.PreviewCamera);
            Assert.IsNull(pv.Texture);
            // 훅을 안 거치고 팝업이 통째로 파괴돼도(HideAll) 다음 프레임에 스스로 걷는다
            PlayerInfoPopup.Open(h);
            yield return null;
            Assert.IsTrue(pv.Active);
            h.Popups.HideAll();
            yield return null;
            yield return null;
            Assert.IsFalse(pv.Active, "상자가 사라지면 LateUpdate 가 걷는다");
        }
    }
}
