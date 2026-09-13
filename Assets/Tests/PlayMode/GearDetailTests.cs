using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Forging;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T111 — 장비 상세 팝업 카드가 정본(`style.css` 1737~1738 · shot-043244)대로 **하단 앵커**(카드 바닥 = 앱 위에서 77.7%H · 원본 실측 77.15%H)이고 폭이 `.gd-card` **70%W** 인가 ·
    /// ✕ 는 카드 아래턱에 반쯤 걸친다(정본대로 · 건드리지 않는다) · 글자 하한 · 콘솔 빨강 0.
    /// 자기 파일인 이유: `ForgeUiTests.cs` 는 T87 lock 이 쥐고 있어(check_claim_scope) 뒤 번호가 손대지 않는다(T68 꼴 · 결정 249).
    /// </summary>
    public class GearDetailTests
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
            Assert.IsNotNull(ForgeHost.Instance.Engine, "ForgeHost 가 Ready 인데 엔진이 없다");
            yield return null;
        }

        [TearDown]
        public void CleanSave()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
        }

        private static Rect World(RectTransform rt)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y);
        }

        private static RectTransform FindUnder(RectTransform parent, string objName)
        {
            foreach (RectTransform rt in parent.GetComponentsInChildren<RectTransform>(true))
                if (rt != parent && rt.name == objName) return rt;
            Assert.Fail(parent.name + " 아래에 «" + objName + "» 이 없다");
            return null;
        }

        private static void AssertTextGate()
        {
            UiCatalog cat = UiCatalog.Instance;
            foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            {
                if (!t.gameObject.activeInHierarchy) continue;
                UiTextKindTag tag = t.GetComponent<UiTextKindTag>();
                Assert.IsNotNull(tag, t.name + " 은 UiKit.Text 를 거치지 않았다");
                Assert.GreaterOrEqual(t.fontSize, cat.Kind(tag.Kind).min, t.name + " 글자 하한");
            }
        }

        [UnityTest]
        public IEnumerator 장비_상세_카드는_하단_앵커_77H_폭_70W_이고_X_는_아래턱에_걸친다()
        {
            yield return Boot();
            PlayLog log = PlayLog.Start("gear-detail");
            ForgeHost h = ForgeHost.Instance;

            // 표값 자체가 정본과 같은가(수치는 GearDetailUi.json 에 · 정본 style.css 1737~1738)
            Assert.AreEqual(0.70f, GearDetailStyle.L("card_w"), 1e-6f, ".gd-card 폭 70%");
            Assert.AreEqual(0.223f, GearDetailStyle.L("bottom_h"), 1e-6f, "padding-bottom .223·H");

            // 장비 하나를 제작해 장착한다(빈 세이브 → 빈 부위라 비교 팝업은 바로 닫힌다)
            h.S.Hammers = 10; h.Pull();
            h.OnCraft();
            float t = 0f;
            while (!h.Meta.Popups.IsOpen(ForgeCraftPopup.Name) && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "제작 뒤 비교 팝업이 떠야 한다");
            ForgeItem item = h.Pending;
            h.ResolveCraft("equip");
            yield return null;
            if (h.Meta.Popups.IsOpen(ForgeCraftPopup.Name)) { h.ResolveCraft("sell"); yield return null; if (h.Meta.Popups.IsOpen(ForgeCraftPopup.SellName)) { h.OnSellConfirm(); yield return null; } }
            Assert.IsNotNull(h.Gear.Get(item.Slot), "장착됐다");

            log.Mark("gear-detail");
            GearDetailPopup.Open(h, item.Slot);
            yield return null;
            yield return null;
            Popup p = h.Meta.Popups.Find(GearDetailPopup.Name);
            Assert.IsNotNull(p, "장비 세부정보 팝업이 열려 있다");
            RectTransform card = FindUnder(p.Root, "card");
            Rect app = World(UiRoot.Instance.App), rc = World(card);
            Assert.Greater(app.height, 0f, "앱 상자");

            // ⓐ 폭 — 앱 폭의 70% (±1%p)
            Assert.AreEqual(0.70f, rc.width / app.width, 0.01f, "카드 폭 = 앱 폭 × 70% (정본 .gd-card · 공용 74% 가 아니다) · 실측 " + (rc.width / app.width));
            // ⓑ 세로 — 카드 바닥이 앱 위에서 77.7%H(= 1 − .223 · 원본 실측 77.15%H · 지시서 판정 77±1.5%p)
            float bottomFromTop = (app.yMax - rc.yMin) / app.height;
            Assert.AreEqual(1f - 0.223f, bottomFromTop, 0.015f, "카드 바닥 = 앱 위에서 77.7%H(정본 padding-bottom .223·H) · 실측 " + bottomFromTop);
            Assert.AreEqual(app.center.x, rc.center.x, app.width * 0.01f, "카드는 가로 가운데");
            Assert.Less(rc.yMax, app.yMax, "카드 위 끝이 앱 안에 있다");

            // ⓒ ✕ 는 카드 아래턱에 반쯤 걸친다(정본대로 · 카드 바닥 아래로 내려간다)
            RectTransform x = FindUnder(card, "x-btn");
            Rect rx = World(x);
            Assert.Less(rx.yMin, rc.yMin, "✕ 아래 끝이 카드 바닥보다 아래");
            Assert.Greater(rx.yMax, rc.yMin, "✕ 위 끝은 카드 안에 걸친다");

            AssertTextGate();
            GearDetailPopup.Close(h);
            yield return null;
            Assert.IsFalse(h.Meta.Popups.IsOpen(GearDetailPopup.Name));
            log.AssertNoRed();
            log.Dispose();
        }
    }
}
