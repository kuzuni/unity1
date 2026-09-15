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
    /// T355 2회차 — 눌림 피드백 자리 배선: ⓑ 장비 칸·탈것(알) 칸(정본 7743·7750·8087 `.equip-cell:active { translateY(.08rem) }`)과
    /// ⓖ 탭 패널 슬라이드(정본 642 `.panel { translateY(105%) → none · .22s ease-out }`). 도우미 자체는 <c>PressFxTests</c>(1회차)가 본다.
    /// 자기 파일인 이유: `ForgeUiTests`·`UiSmokeTests` 는 다른 절의 자리다(check_claim_scope).
    /// </summary>
    public class PressFxSitesTests
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
            while (!(ForgeHost.Ready && MetaHost.Ready && UiRoot.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            Assert.IsNotNull(UiRoot.Instance, "UiRoot 가 서지 않았다");
            yield return null;
        }

        private static IEnumerator WaitMs(double ms)
        {
            float t = 0f;
            while (t < ms / 1000f) { t += Time.unscaledDeltaTime; yield return null; }
            yield return null;
        }

        private static Transform SheetChild(string name)
        {
            foreach (Transform t in UiRoot.Instance.Sheet.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        private static IEnumerator AssertPress(RectTransform cell, string key)
        {
            PressFx fx = cell.GetComponent<PressFx>();
            Assert.IsNotNull(fx, cell.name + " 에 PressFx 가 안 붙었다");
            Assert.AreEqual(key, fx.Spec.Key, cell.name + " 의 표 키");
            Assert.AreSame(cell, fx.Target, "칸 자체가 움직인다(정본 .equip-cell 에 transform)");
            PressSpec s = PressFx.Table.Get(key);
            float rem = PopupKit.Rem;
            float baseY = cell.anchoredPosition.y;
            Assert.IsFalse(fx.Active, "놓인 상태에서 시작");
            fx.Press(true);
            yield return WaitMs(s.Ms * 2);
            Assert.AreEqual(1.0, fx.Phase, 1e-6, "ms 가 지나면 위상 1");
            Assert.AreEqual(baseY - (float)s.DyRem * rem, cell.anchoredPosition.y, 0.5f, "정본 translateY(.08rem) — 놓인 자리(Place 뒤)에서 아래로");
            fx.Press(false);
            yield return WaitMs(s.Ms * 2);
            Assert.AreEqual(0.0, fx.Phase, 1e-6);
            Assert.AreEqual(baseY, cell.anchoredPosition.y, 0.5f, "제자리로 — Place 뒤 SetBase 가 안 됐으면 (0,0) 으로 튄다");
        }

        [UnityTest]
        public IEnumerator 장비_칸과_탈것_칸은_누르면_놓인_자리에서_표대로_내려가고_떼면_돌아온다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            // 런 559: 새 세이브는 장착 장비가 0 이라(빈 칸은 버튼이 없어 눌림도 없다) 하나 굴려 장착하고 시트를 다시 그린다.
            Forge.Core.Forging.ForgeItem it = h.Engine.RollItem();
            h.Gear.Set(it.Slot, it);
            ForgeSheet.Render(h);
            yield return null;
            string slot = it.Slot;
            RectTransform cell = (RectTransform)SheetChild("cell-" + slot);
            Assert.IsNotNull(cell, "장비 칸 cell-" + slot);
            yield return AssertPress(cell, "equip_cell");
            RectTransform egg = (RectTransform)SheetChild("egg-cell");
            Assert.IsNotNull(egg, "탈것(알) 칸");
            yield return AssertPress(egg, "egg_cell");
        }

        [UnityTest]
        public IEnumerator 소환_시트는_열릴_때_아래에서_미끄러져_올라오고_표의_ms_안에_제자리에_선다()
        {
            yield return Boot();
            TabBar tb = UiRoot.Instance.TabBar;
            RectTransform panel = tb.Panel("summon");
            Assert.IsNotNull(panel);
            PanelSlide ps = panel.GetComponent<PanelSlide>();
            Assert.IsNotNull(ps, "소환 시트에 PanelSlide 가 안 붙었다(UiRoot 가 PanelHost 의 패널마다 붙인다)");
            PanelSlideSpec s = PanelSlide.Spec;
            Assert.AreEqual(105.0, s.DyPct, 1e-9, "정본 642 translateY(105%)");
            Assert.AreEqual(220.0, s.Ms, 1e-9, "정본 642 .22s");

            tb.OnTab("summon");
            Assert.IsTrue(panel.gameObject.activeInHierarchy, "열리는 즉시 활성(자들이 그렇게 본다)");
            Assert.IsTrue(ps.Sliding, "켜지는 순간 슬라이드가 시작된다");
            float below = panel.anchoredPosition.y;
            Assert.Less(below, ps.BasePos.y - 1f, "첫 프레임은 아래에 있다(105%)");
            Assert.AreEqual(ps.BasePos.y - panel.rect.height * (float)(s.DyPct / 100.0), below, 1f, "시작 자리 = 높이 × 105%");
            yield return WaitMs(s.Ms * 0.5);
            float mid = panel.anchoredPosition.y;
            Assert.Greater(mid, below, "올라오는 중");
            Assert.Less(mid, ps.BasePos.y, "아직 제자리 전");
            yield return WaitMs(s.Ms * 1.5);
            Assert.IsFalse(ps.Sliding, "ms 가 지나면 끝");
            Assert.AreEqual(ps.BasePos.y, panel.anchoredPosition.y, 0.5f, "정본 .open { transform: none }");

            tb.OnTab("summon");
            yield return null;
            Assert.IsFalse(panel.gameObject.activeInHierarchy, "닫힘은 즉시(자들이 단언하는 대로)");

            // 정지 촬영 길 — 열자마자 SettleAll 이면 그 프레임에 제자리
            tb.OnTab("summon");
            Assert.IsTrue(ps.Sliding);
            PanelSlide.SettleAll();
            Assert.IsFalse(ps.Sliding);
            Assert.AreEqual(ps.BasePos.y, panel.anchoredPosition.y, 0.5f, "SettleAll 은 그 자리에서 끝낸다(UiShotsTests 가 찍기 전에 부른다)");
            tb.OnTab("summon");
            yield return null;
        }
    }
}
