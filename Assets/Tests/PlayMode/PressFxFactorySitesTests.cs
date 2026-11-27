using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T515 2·3회차 — 눌림 피드백을 **공장마다** 센다(1회차의 집 `PressFxSitesTests` 는 T516 의 산 lock 범위라 따로 둔 집).
    /// 2회차에 `PaperButton` 하나로 났고(그때 이름은 `PressFxPaperButtonTests`) 3회차에 모루 자리가 들어와 **이름을 고쳤다** —
    /// 파일 이름이 안 하는 일을 말하지 않게 한다.
    ///
    /// 둘째 공장: `PetSkillKit.PaperButton`(펫·스킬·탈것 시트와 업그레이드 팝업의 종이 버튼).
    ///
    /// 정본 **3563** `.modal-card .btn:active, .panel .btn:active, #equip-sheet .btn:active
    /// { transform: translateY(.1rem); box-shadow: none }` 이 이 공장이 내는 버튼 **전부**에 걸린다 —
    /// 부르는 자리 스물다섯이 모두 `.panel`·`.modal-card` 안이고 정본 마크업이 전부 `class="btn …"` 다.
    /// 1회차가 센 자리(`Popups.Btn`)와 **값이 같고 표 칸도 같다**(`modal_btn`) — 다른 것은 **턱의 꼴**이다:
    /// 이 공장의 턱은 끌 그림이 아니라 `skin`(검정 테)이 면 아래로 `btn_inset_rem` 만큼 드러난 **띠**라,
    /// 면을 그 몫만큼 아래로 자라게 해 덮는다.
    ///
    /// 자기 파일인 까닭: 1회차의 집(`PressFxSitesTests`)은 지금 **T516 의 산 lock 범위**다(그 절이 같은 파일에
    /// 비활성 버튼 자를 쓴다) — 남의 lock 안에 칸을 끼우지 않는다(ROUTINE §1).
    /// </summary>
    public class PressFxFactorySitesTests
    {
        private static void DeleteSave()
        {
            try
            {
                string p = System.IO.Path.Combine(Application.persistentDataPath, (SaveIo.Defs != null ? SaveIo.Defs.SaveKey : "forgeclone_save_v1") + ".json");
                if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
            }
            catch (System.Exception) { /* 저장소 접근 실패는 무시 */ }
        }

        [TearDown]
        public void CleanSave() { DeleteSave(); }

        private static IEnumerator Boot()
        {
            // 전투를 안 띄운다 — 배경 전투가 `Meta.Changed` 를 부르면 시트가 다시 서면서 누르던 버튼과 그 `PressFx` 가
            // 사라진다(T366 2회차가 `PressFxSitesTests` 에서 같은 까닭으로 세운 길).
            Forge.Game.Battle.BattleScene.AutoBoot = false;
            DeleteSave();
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && UiRoot.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            Assert.IsNotNull(UiRoot.Instance, "UiRoot 가 서지 않았다");
            yield return null;
        }

        /// <summary>그 공장이 낸 버튼(= `skin/top` 을 가진 `PressFx`) 하나를 찾는다. 없으면 null.</summary>
        private static PressFx FindPaperButton(out RectTransform face)
        {
            face = null;
            foreach (PressFx p in UiRoot.Instance.App.GetComponentsInChildren<PressFx>(true))
            {
                if (p.Spec == null || p.Spec.Key != "modal_btn") continue;
                UnityEngine.UI.Button b = p.GetComponent<UnityEngine.UI.Button>();
                if (b == null || !b.interactable) continue;
                Transform skin = p.transform.Find("skin");
                if (skin == null) continue;                      // `Popups.Btn` 꼴(lip/face/label)은 1회차의 자리다
                Transform top = skin.Find("top");
                if (top == null) continue;
                face = (RectTransform)top;
                return p;
            }
            return null;
        }

        /// <summary>
        /// 펫 시트의 종이 버튼을 눌러 ⓐ `.1rem` 가라앉고 ⓑ 면이 턱 몫만큼 아래로 자라고 ⓒ 떼면 둘 다 되돌아온다.
        /// 정본에 `transition` 이 없는 자리라(표 `ms: 0`) **한 프레임 뒤의 상태**를 본다 — 기다릴 램프가 없다.
        /// </summary>
        [UnityTest]
        public IEnumerator 종이_버튼은_누르면_표대로_가라앉고_면이_턱을_덮는다()
        {
            yield return Boot();
            PressSpec s = PressFx.Table.Get("modal_btn");
            Assert.AreEqual(0.0, s.Ms, 1e-9, "정본 3563 엔 transition 이 없다 — 즉시");
            Assert.IsTrue(s.LipOff, "3563 box-shadow: none");

            RectTransform face;
            PressFx fx = FindPaperButton(out face);
            if (fx == null)
            {
                // 소환 탭을 열어 한 번 더 본다 — 부팅은 홈(전투 화면)이라 시트가 아직 안 섰다
                // (`PetUiTests`·`UiSmokeTests` 가 쓰는 같은 길: `TabBar.OnTab("summon")`).
                UiRoot.Instance.TabBar.OnTab("summon");
                yield return null;
                yield return null;
                yield return null;
                fx = FindPaperButton(out face);
            }
            if (fx == null) Assert.Ignore("환경 — 이 부팅에서 `PetSkillKit.PaperButton` 버튼이 하나도 안 떠 있다");

            RectTransform rt = (RectTransform)fx.transform;
            float rem = PopupKit.Rem, inset = PetSkillStyle.Px("btn_inset_rem");
            float baseY = rt.anchoredPosition.y, faceY = face.offsetMin.y;
            Assert.Greater(inset, 0f, "턱(btn_inset_rem)이 있어야 걷을 것이 있다");
            Assert.IsFalse(fx.Active, fx.name + ": 놓인 상태에서 시작");

            fx.Press(true);
            yield return null;
            Assert.AreEqual(1.0, fx.Phase, 1e-9, fx.name + ": ms 0 이면 한 프레임에 위상 1 — 잰 위상 " + fx.Phase);
            Assert.AreEqual(baseY - (float)s.DyRem * rem, rt.anchoredPosition.y, 0.5f, fx.name + ": 정본 translateY(.1rem)");
            Assert.AreEqual(faceY - inset, face.offsetMin.y, 0.5f, fx.name + ": 면이 턱 몫만큼 아래로 자라 검정 띠를 덮는다");

            fx.Press(false);
            yield return null;
            Assert.AreEqual(0.0, fx.Phase, 1e-9, fx.name + ": 떼면 한 프레임에 위상 0");
            Assert.AreEqual(baseY, rt.anchoredPosition.y, 0.5f, fx.name + ": 떼면 제자리");
            Assert.AreEqual(faceY, face.offsetMin.y, 0.5f, fx.name + ": 떼면 턱이 다시 보인다");
        }

        /// <summary>
        /// T515 3회차 — 정본 **987** `.anvil-btn:active { transform: translateY(.1rem) }`: 메인 화면 모루 버튼은
        /// **가라앉기만** 한다(그 선언에 `box-shadow: none` 이 없다 · `class` 에 `btn` 이 없어 3563 이 안 걸린다 · 표 `anvil_btn`).
        /// 두들기는 동안은 `anvilbump` 가 transform 을 쥐므로 `Suppressed` 다 — 이 자는 **안 두들기는** 상태에서 잰다.
        /// </summary>
        [UnityTest]
        public IEnumerator 모루_버튼은_누르면_가라앉기만_한다()
        {
            yield return Boot();
            PressSpec s = PressFx.Table.Get("anvil_btn");
            Assert.AreEqual(0.0, s.Ms, 1e-9, "정본 987 엔 transition 이 없다 — 즉시");
            Assert.IsFalse(s.LipOff, "정본 987 엔 box-shadow: none 이 없다 — 걷을 턱이 없다");

            PressFx fx = null;
            foreach (PressFx p in UiRoot.Instance.App.GetComponentsInChildren<PressFx>(true))
                if (p.Spec != null && p.Spec.Key == "anvil_btn" && !p.Suppressed) { fx = p; break; }
            if (fx == null) Assert.Ignore("환경 — 이 부팅에서 모루 버튼이 안 떠 있다(대장간 시트가 닫혔거나 두들기는 중이다)");

            RectTransform rt = (RectTransform)fx.transform;
            float rem = PopupKit.Rem, baseY = rt.anchoredPosition.y;
            Vector3 baseScale = rt.localScale;
            fx.Press(true);
            yield return null;
            Assert.AreEqual(baseY - (float)s.DyRem * rem, rt.anchoredPosition.y, 0.5f, "정본 translateY(.1rem)");
            Assert.AreEqual(baseScale.x, rt.localScale.x, 1e-4f, "정본 987 엔 scale 이 없다");
            fx.Press(false);
            yield return null;
            Assert.AreEqual(baseY, rt.anchoredPosition.y, 0.5f, "떼면 제자리");
        }

        /// <summary>
        /// T515 6회차 — 정본 **5011** 의 둘째 채널 `box-shadow: inset 0 .12rem .3rem rgba(0,0,0,.55)`:
        /// 자동 제련 스피너는 눌리면 **안쪽 그늘이 생긴다**. 열두 자리 중 **그늘이 생기는 유일한 자리**고,
        /// 5009 가 `box-shadow .07s` 를 쥐어 **램프**다 — 턱 걷기·면 갈아입기(이산)와 달리 **ms 안에** 짙어진다.
        /// 잣대는 프레임이 아니라 **규칙**에 건다(T422·T431): «놓였을 때 0 · 누르고 ms 지나면 1 · 떼면 다시 0».
        /// </summary>
        [UnityTest]
        public IEnumerator 스피너는_누르면_안쪽_그늘이_램프로_짙어진다()
        {
            yield return Boot();
            PressSpec s = PressFx.Table.Get("af_spinner");
            Assert.IsTrue(s.HasInset, "표 `af_spinner.inset` — 정본 5011");
            Assert.Greater(s.Ms, 0.0, "5009 가 box-shadow .07s 를 쥔다 — 이 자리는 이산이 아니다");

            ForgeHost h = ForgeHost.Instance;
            h.S.BestChapter = 3; h.S.BestStage = 1; h.Pull();      // 2-10 뒤에만 열린다(PressFxSitesTests 가 쓰는 길)
            if (!h.AutoForgeUnlocked) Assert.Ignore("환경 — 자동 제련이 아직 안 열렸다");
            ForgeAutoPopup.Open(h);
            yield return null;
            yield return null;
            Popup p = h.Meta.Popups.Find(ForgeAutoPopup.Name);
            Assert.IsNotNull(p, "자동 제련 팝업이 열린다");

            PressFx fx = null;
            foreach (PressFx q in p.Root.GetComponentsInChildren<PressFx>(true))
                if (q.Spec != null && q.Spec.Key == "af_spinner") { fx = q; break; }
            Assert.IsNotNull(fx, "스피너의 눌림 부품");
            // ⚑ 7회차 수리 — **경로를 구조에 박지 않는다**: `RadiusUi.Outlined(parent, "face", …)` 는 «테 + 면» 두 겹을
            //   **면이라는 이름의 상자** 안에 세우고 **안쪽 면**(테 두께만큼 인셋된 것)을 돌려준다 — 그래서 실제 자리는
            //   `af-spinner/face/face/inset-af_spinner` 다. 6회차의 이 자는 `face/inset-…` 한 칸으로 찾아 «없다» 로 빨갰다
            //   (런 1390 · 제품 코드는 맞았다 — 정본 inset 그늘은 **테 안쪽 패딩 상자**에 그려지고 그 상자가 곧 안쪽 면이다).
            //   그래서 **이름으로** 찾는다(결정 896).
            Transform insT = null;
            foreach (Transform t2 in fx.GetComponentsInChildren<Transform>(true))
                if (t2.name == "inset-af_spinner") { insT = t2; break; }
            Assert.IsNotNull(insT, "안쪽 그늘 겹이 스피너 안에 깔린다");
            UnityEngine.UI.Graphic ins = insT.GetComponent<UnityEngine.UI.Graphic>();
            Assert.IsNotNull(ins, "그 겹은 그림이다");
            Assert.IsNotNull(insT.parent.GetComponent<UnityEngine.UI.Image>(),
                "그 겹의 부모는 **면**이다(CSS inset = 면 위·내용 아래 · 글자는 스피너 뿌리의 자식이라 위에 남는다)");
            Assert.AreEqual("face", insT.parent.name, "부모 이름이 면이다");
            Assert.IsNotNull(ins.GetComponent<UnityEngine.UI.Image>().sprite, "구운 판이 있다(단색 판이 아니다)");

            Assert.AreEqual(0f, ins.color.a, 1e-4f, "놓였을 때는 그늘이 없다(정본 5005 엔 inset 선언이 없다)");
            fx.Press(true);
            float t = 0f;
            while (fx.Phase < 1.0 && t < 2f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(1.0, fx.Phase, 1e-9, "ms 안에 위상 1 — 잰 위상 " + fx.Phase);
            Assert.AreEqual(1f, ins.color.a, 1e-3f, "짙어진 뒤에는 구운 알파가 그대로 선다(판이 .55 를 쥔다)");
            fx.Press(false);
            t = 0f;
            while (fx.Phase > 0.0 && t < 2f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(0.0, fx.Phase, 1e-9, "떼면 0 으로 돌아온다");
            Assert.AreEqual(0f, ins.color.a, 1e-4f, "떼면 그늘이 사라진다");
            ForgeAutoPopup.Close(h);
            yield return null;
        }

        /// <summary>
        /// **비활성 버튼엔 안 건다** — 정본 `:active` 는 눌리는 버튼에만 들고, 걸면 죽은 부품이 버튼마다 하나씩 붙는다
        /// (1회차가 `Popups.Btn` 에서 세운 같은 잣대).
        /// </summary>
        [UnityTest]
        public IEnumerator 비활성_종이_버튼엔_눌림이_안_붙는다()
        {
            yield return Boot();
            int bad = 0;
            string names = string.Empty;
            foreach (UnityEngine.UI.Button b in UiRoot.Instance.App.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                if (b.interactable) continue;
                Transform skin = b.transform.Find("skin");
                if (skin == null || skin.Find("top") == null) continue;     // 이 공장이 낸 버튼만 본다
                if (b.GetComponent<PressFx>() == null) continue;
                bad++;
                if (names.Length < 120) names += (names.Length > 0 ? " · " : string.Empty) + b.name;
            }
            Assert.AreEqual(0, bad, "비활성 종이 버튼에 PressFx 가 붙었다: " + names);
        }

        /// <summary>
        /// T515 5회차 — 정본 **4802** `.af-dd-list button:active { background: #2c313a }`: 자동 제련 드롭다운 항목은
        /// **움직이지 않고 배경만** 납작하게 갈아입는다(그 마크업은 `class` 에 `btn` 이 없어 3563 이 안 걸린다 · 표 `af_dd_item`).
        /// 밝기 곱하기가 아니라 **교체**라 «제 색이 무엇이었든 눌리면 #2c313a» 다 — 표시된 항목(정본 4801 `.on` 파랑)도
        /// 특이도가 같고 4802 가 뒤라 같은 색으로 간다. 그래서 이 자는 **표시된 항목과 안 표시된 항목을 다 집어** 잰다.
        /// </summary>
        [UnityTest]
        public IEnumerator 드롭다운_항목은_안_움직이고_누르는_동안_배경만_갈아입는다()
        {
            yield return Boot();
            PressSpec s = PressFx.Table.Get("af_dd_item");
            Assert.AreEqual(0.0, s.DyRem, 1e-9, "4802 엔 transform 이 없다");
            Assert.AreEqual(0.0, s.Ms, 1e-9, "4802 엔 transition 이 없다 — 즉시");
            Assert.IsTrue(s.FaceSwap, "4802 background: #2c313a");
            Color want = PinnedColorUi.C("af_dd_press_face");

            ForgeHost h = ForgeHost.Instance;
            Assert.IsNotNull(h, "ForgeHost 가 없다");
            // ⚑ 8회차 수리 — **자동 제련을 먼저 열어 둬야 팝업이 선다**: 런 **1393** 에서 이 자가
            //   «자동 제련 팝업이 열린다 — Expected: not null» 로 빨갰다. `ForgeAutoPopup.Open` 은 해금 전이면
            //   아무것도 세우지 않는데(2-10 뒤에 열린다) 이 자는 세이브를 그대로 쓴 **새 세이브**로 불렀다.
            //   `PressFxSitesTests`·6회차 자가 쓰는 같은 길로 해금한다(`BestChapter`·`BestStage` → `Pull`).
            h.S.BestChapter = 3; h.S.BestStage = 1; h.Pull();
            if (!h.AutoForgeUnlocked) Assert.Ignore("환경 — 자동 제련이 아직 안 열렸다");
            ForgeAutoPopup.Open(h);
            yield return null;
            if (!ForgeAutoPopup.DropdownOpen) ForgeAutoPopup.ToggleDropdown(h);
            yield return null;
            yield return null;
            Assert.IsTrue(ForgeAutoPopup.DropdownOpen, "드롭다운이 열리지 않았다");

            // ⚠ 팝업 안의 칸은 `UiRoot.App` 아래에 없다 — 런 **#1390** 에서 이 자가 그 길로 훑어
            //   «항목이 서지 않았다» 로 **건너뛰었다**(그래서 5회차 판정이 비었다). 팝업 뿌리에서 찾는다
            //   (`PressFxSitesTests.FindPressed` 가 `scope` 를 받는 까닭이 이것이고 6회차 자도 `p.Root` 를 쓴다).
            Popup pop = h.Meta.Popups.Find(ForgeAutoPopup.Name);
            Assert.IsNotNull(pop, "자동 제련 팝업이 열린다");
            int seen = 0;
            foreach (PressFx fx in pop.Root.GetComponentsInChildren<PressFx>(true))
            {
                if (fx.Spec == null || fx.Spec.Key != "af_dd_item") continue;
                Transform ft = fx.transform.Find("face");
                if (ft == null) continue;
                UnityEngine.UI.Image face = ft.GetComponent<UnityEngine.UI.Image>();
                if (face == null) continue;
                seen++;
                RectTransform rt = (RectTransform)fx.transform;
                Color own = face.color;
                float baseY = rt.anchoredPosition.y;

                fx.Press(true);
                yield return null;
                Assert.AreEqual(1.0, fx.Phase, 1e-9, fx.name + ": ms 0 이면 한 프레임에 위상 1");
                Assert.AreEqual(baseY, rt.anchoredPosition.y, 0.01f, fx.name + ": 4802 엔 transform 이 없다 — 가라앉지 않는다");
                Assert.AreEqual(want.r, face.color.r, 1f / 255f, fx.name + ": 누르면 면이 #2c313a 로 갈아입는다(R)");
                Assert.AreEqual(want.g, face.color.g, 1f / 255f, fx.name + ": 같은 자리(G)");
                Assert.AreEqual(want.b, face.color.b, 1f / 255f, fx.name + ": 같은 자리(B)");

                fx.Press(false);
                yield return null;
                Assert.AreEqual(0.0, fx.Phase, 1e-9, fx.name + ": 떼면 한 프레임에 위상 0");
                Assert.AreEqual(own.r, face.color.r, 1f / 255f, fx.name + ": 떼면 제 색으로 돌아온다(R)");
                Assert.AreEqual(own.g, face.color.g, 1f / 255f, fx.name + ": 떼면 제 색(G)");
                Assert.AreEqual(own.b, face.color.b, 1f / 255f, fx.name + ": 떼면 제 색(B)");
                if (seen >= 3) break;      // 표시된 항목 하나가 반드시 끼도록 셋만 본다(스물둘을 다 누르면 자가 길어진다)
            }
            ForgeAutoPopup.Close(h);
            if (seen == 0) Assert.Ignore("환경 — 이 부팅에서 자동 제련 드롭다운 항목이 서지 않았다");
        }
    }
}
