using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T355 — 눌림 피드백: 오프라인 버튼을 포인터로 누르면 상자(`.ob-chest`)가 표 `offline_chest` 대로 아래로 밀리고 줄어들며, 떼면 표의 ms 안에 제자리로 온다.
    /// 값은 표에서 읽어 견준다(코드에 수 없음). 자기 파일인 이유: `OfflineButtonTests` 는 T133 절의 자리다.
    /// </summary>
    public class PressFxTests
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
            DeleteSave();
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && OfflineButton.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다");
            Assert.IsNotNull(OfflineButton.Instance, "오프라인 버튼이 서지 않았다");
            yield return null;
        }

        private static IEnumerator WaitMs(double ms)
        {
            float t = 0f;
            while (t < ms / 1000f) { t += Time.unscaledDeltaTime; yield return null; }
            yield return null;
        }

        [UnityTest]
        public IEnumerator 오프라인_상자는_누르면_표대로_내려가_줄고_떼면_ms_안에_제자리로_온다()
        {
            yield return Boot();
            OfflineButton ob = OfflineButton.Instance;
            PressFx fx = ob.Press;
            Assert.IsNotNull(fx, "오프라인 버튼에 PressFx 가 안 붙었다");
            Assert.AreSame(ob.Chest, fx.Target, "움직이는 상자는 .ob-chest 다(정본 216)");
            PressSpec s = PressFx.Table.Get("offline_chest");
            Assert.AreEqual(s, fx.Spec);
            Assert.IsFalse(ob.IsReady, "새 세이브는 보상이 안 쌓여 들썩이지 않는다 — 눌림이 보이는 상태");
            float rem = PopupKit.Rem;

            PointerEventData ev = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(ob.Btn.gameObject, ev, ExecuteEvents.pointerDownHandler);
            Assert.IsTrue(fx.Pressed, "포인터 다운이 PressFx 에 닿는다");
            yield return WaitMs(s.Ms * 2);
            Assert.AreEqual(1.0, fx.Phase, 1e-6, "ms 가 지나면 위상 1");
            Assert.AreEqual(-(float)s.DyRem * rem, ob.Chest.anchoredPosition.y, 0.5f, "정본 translateY(.08rem) — 아래로");
            Assert.AreEqual((float)s.Scale, ob.Chest.localScale.x, 1e-3f, "정본 scale(.94)");
            Assert.AreEqual((float)s.Scale, ob.Chest.localScale.y, 1e-3f);

            ExecuteEvents.Execute(ob.Btn.gameObject, ev, ExecuteEvents.pointerUpHandler);
            Assert.IsFalse(fx.Pressed);
            yield return WaitMs(s.Ms * 2);
            Assert.AreEqual(0.0, fx.Phase, 1e-6, "떼고 ms 가 지나면 위상 0");
            Assert.AreEqual(0f, ob.Chest.anchoredPosition.y, 0.5f, "제자리");
            Assert.AreEqual(1f, ob.Chest.localScale.x, 1e-3f);
            Assert.IsFalse(fx.Active);

            // 중간에 떼면 그 위상에서 되돌아간다(CSS transition 되감기) — 다운 뒤 한 프레임 만에 업
            ExecuteEvents.Execute(ob.Btn.gameObject, ev, ExecuteEvents.pointerDownHandler);
            yield return null;
            ExecuteEvents.Execute(ob.Btn.gameObject, ev, ExecuteEvents.pointerUpHandler);
            double mid = fx.Phase;
            Assert.Less(mid, 1.0);
            yield return WaitMs(s.Ms * 2);
            Assert.AreEqual(0.0, fx.Phase, 1e-6);
            Assert.AreEqual(0f, ob.Chest.anchoredPosition.y, 0.5f);
        }
    }
}
