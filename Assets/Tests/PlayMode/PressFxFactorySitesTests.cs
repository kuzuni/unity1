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
    }
}
