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


        /// <summary>
        /// 위상이 목표에 **닿을 때까지** 기다린다(상한을 둔다) — 벽시계로 «딱 ms» 를 재면 프레임 한 칸에 걸린다.
        ///
        /// 런 589 가 그 실물이다: `WaitMs(s.Ms * 2)` 뒤에 위상이 `0.99988` 이었다. 셈(<see cref="PressRules.Phase"/>)은
        /// `t >= 1` 이면 **정확히** 1 을 내주므로 틀린 것은 셈이 아니라 «누른 프레임의 델타부터 세는» 기다림이었다 —
        /// 코루틴은 누르기 **전에** 이미 흐른 그 프레임을 제 몫으로 세고, 부품은 누른 **다음** 프레임부터 센다.
        /// 판정은 «ms 가 지나면 위상 1» 이지 «정확히 2×ms 창에서 1» 이 아니다 — 상한을 넉넉히 두고 상태를 기다린다.
        /// </summary>
        private static IEnumerator Settle(PressFx fx, double target, double capMs)
        {
            float t = 0f;
            while (System.Math.Abs(fx.Phase - target) > 1e-9 && t * 1000f < capMs) { t += Time.unscaledDeltaTime; yield return null; }
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

        /// <summary>
        /// 눌림을 재는 동안 살아 있는 칸 — 런 600 이 그 실물이다: 배경 전투가 `Meta.Changed`(퀘스트 Bump·자동 저장)를 부르면
        /// `ForgeHost.OnMetaChanged → ForgeSheet.Render` 가 시트 자식을 **전부 지우고 다시 세우므로** 누르던 칸(과 그 PressFx)이 사라져
        /// 위상이 그 자리에서 얼어붙는다(잰 위상 0.673 · Update 가 더는 안 돈다). 칸이 죽었으면 같은 이름의 새 칸을 찾아 다시 누른다 —
        /// 판정은 «살아 있는 칸이 표대로 눌리는가» 이지 «시트가 그동안 안 다시 그려지는가» 가 아니다.
        /// </summary>
        private sealed class Live
        {
            public string Name;
            public RectTransform Cell;
            public PressFx Fx;
            public float BaseY;
            public int Restarts;
        }

        private static void Refind(Live L, string key)
        {
            Transform c = SheetChild(L.Name);
            Assert.IsNotNull(c, "시트를 다시 그린 뒤에도 " + L.Name + " 이 있어야 한다");
            L.Cell = (RectTransform)c;
            L.Fx = L.Cell.GetComponent<PressFx>();
            Assert.IsNotNull(L.Fx, L.Name + " 에 PressFx 가 안 붙었다");
            Assert.AreEqual(key, L.Fx.Spec.Key, L.Name + " 의 표 키");
            Assert.AreSame(L.Cell, L.Fx.Target, "칸 자체가 움직인다(정본 .equip-cell 에 transform)");
            L.BaseY = L.Cell.anchoredPosition.y;
        }

        /// <summary>위상이 목표에 닿을 때까지(상한 capMs) — 도중에 칸이 다시 서면 새 칸에서 다시 누르고(최대 여섯 번) 시계를 되돌린다.</summary>
        private static IEnumerator SettleLive(Live L, string key, bool down, double target, double capMs)
        {
            float t = 0f;
            while (true)
            {
                if (L.Fx == null)
                {
                    Assert.Less(L.Restarts++, 6, "시트가 계속 다시 그려져 눌림을 못 잰다(여섯 번 넘게 새로 섰다)");
                    Refind(L, key);
                    if (down) L.Fx.Press(true);
                    t = 0f;
                }
                if (System.Math.Abs(L.Fx.Phase - target) <= 1e-9 || t * 1000f >= capMs) yield break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static IEnumerator AssertPress(RectTransform cell, string key)
        {
            Live L = new Live { Name = cell.name };
            Refind(L, key);
            PressSpec s = PressFx.Table.Get(key);
            float rem = PopupKit.Rem;
            Assert.IsFalse(L.Fx.Active, "놓인 상태에서 시작");
            L.Fx.Press(true);
            yield return SettleLive(L, key, true, 1.0, s.Ms * 8);
            Assert.IsNotNull(L.Fx, "칸이 살아 있다");
            Assert.AreEqual(1.0, L.Fx.Phase, 1e-6, "ms 가 지나면 위상 1(상한 8×ms 안에) — 잰 위상 " + L.Fx.Phase);
            Assert.AreEqual(L.BaseY - (float)s.DyRem * rem, L.Cell.anchoredPosition.y, 0.5f, "정본 translateY(.08rem) — 놓인 자리(Place 뒤)에서 아래로");
            L.Fx.Press(false);
            yield return SettleLive(L, key, false, 0.0, s.Ms * 8);
            Assert.IsNotNull(L.Fx, "칸이 살아 있다");
            Assert.AreEqual(0.0, L.Fx.Phase, 1e-6, "떼면 위상 0(상한 8×ms 안에) — 잰 위상 " + L.Fx.Phase);
            Assert.AreEqual(L.BaseY, L.Cell.anchoredPosition.y, 0.5f, "제자리로 — Place 뒤 SetBase 가 안 됐으면 (0,0) 으로 튄다");
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
            // 같은 덫(위 Settle 주석) — «딱 ms» 로 재지 말고 **끝날 때까지** 기다리되 상한을 둔다.
            {
                float t2 = 0f;
                while (ps.Sliding && t2 * 1000f < s.Ms * 8) { t2 += Time.unscaledDeltaTime; yield return null; }
            }
            Assert.IsFalse(ps.Sliding, "ms 가 지나면 끝(상한 8×ms 안에)");
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

        static RectTransform FindIn(Transform root, string name, bool prefix = false)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (prefix ? t.name.StartsWith(name, System.StringComparison.Ordinal) : t.name == name) return (RectTransform)t;
            return null;
        }

        /// <summary>ⓒⓓⓔ 자동 제련 팝업 — 스피너(5011 .1rem) · 계속하기 체크(4982 .06rem) · 하위 행(5002 .06rem · 레이아웃 자식이라 기준 자리는 누르는 순간).</summary>
        [UnityTest]
        public IEnumerator 자동_제련_팝업의_스피너_체크_하위_행은_누르면_표대로_내려가고_떼면_놓인_자리로_온다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            h.S.BestChapter = 3; h.S.BestStage = 1; h.Pull();   // 2-10 뒤에만 열린다(ForgeCardWidthTests 가 쓰는 길)
            Assert.IsTrue(h.AutoForgeUnlocked, "2-10 뒤 해금");
            if (!h.Engine.AutoForgeConfig().FilterOn) h.ToggleAutoFilterOn();   // 하위 행은 정본 renderAutoForge 처럼 필터 토글이 켜져야 그려진다(런 578 빨강 원인)
            Assert.IsTrue(h.Engine.AutoForgeConfig().FilterOn, "필터 토글 켬");
            ForgeAutoPopup.Open(h);
            yield return null;
            yield return null;   // 레이아웃 그룹이 하위 행을 제자리에 놓는 프레임
            Popup p = h.Meta.Popups.Find(ForgeAutoPopup.Name);
            Assert.IsNotNull(p, "자동 제련 팝업이 열린다");
            RectTransform sp = FindIn(p.Root, "af-spinner");
            RectTransform ck = FindIn(p.Root, "af-check-continue");
            RectTransform row = FindIn(p.Root, "af-sub-", true);
            Assert.IsNotNull(sp, "스피너"); Assert.IsNotNull(ck, "계속하기 체크"); Assert.IsNotNull(row, "하위 행 하나");
            yield return AssertPress(sp, "af_spinner");
            yield return AssertPress(ck, "af_check");
            Assert.AreNotEqual(0f, row.anchoredPosition.y, "레이아웃이 행을 놓았다(0 이면 아직 안 놓인 것)");
            yield return AssertPress(row, "af_sub_row");
            ForgeAutoPopup.Close(h);
            yield return null;
        }
    }
}
