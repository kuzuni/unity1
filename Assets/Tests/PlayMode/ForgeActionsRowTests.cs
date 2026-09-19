using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T482 3회차 — 장비 시트 버튼 줄(정본 `.forge-actions` 1629~1633): `gap: .3rem` · `margin-left: −.0222W` · `margin-right: .092W` ·
    /// 자동 버튼은 **고정 폭** `.0922W + 6.4px`(499px 폭 기준 → 기준 폭 환산 · 카탈로그 `forge_auto_btn_w_px`) · 대장간 버튼이 나머지(flex: 1).
    /// 전엔 «틈 .35rem · 균등 반분 × 1.15/.85» 가 박혀 자동이 +6.4%p 넓고 우단이 시트 끝까지 갔다(T33 57·58회차 실측).
    /// </summary>
    public class ForgeActionsRowTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && UiRoot.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            yield return null;
        }

        private static Transform FindIn(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        [Test]
        public void 표는_버튼_줄의_틈_마진_자동_버튼_폭을_정본대로_쥔다()
        {
            Assert.AreEqual(0.3f, UiKit.L("forge_actions_gap_rem"), 1e-6f, "1629 gap .3rem");
            Assert.AreEqual(-0.0222f, UiKit.L("forge_actions_ml_f"), 1e-6f, "1629 margin-left −.0222W");
            Assert.AreEqual(0.092f, UiKit.L("forge_actions_mr_f"), 1e-6f, "1629 margin-right .092W");
            Assert.AreEqual(0.0922f, UiKit.L("forge_auto_btn_w_f"), 1e-6f, "1630 자동 버튼 .0922W");
            Assert.AreEqual(6.4f * UiKit.RefW / 499f, UiKit.L("forge_auto_btn_w_px"), 0.05f, "1630 의 6.4px(499 폭) → 기준 폭 환산");
        }

        [UnityTest]
        public IEnumerator 자동_버튼은_고정_폭이고_대장간_버튼이_나머지를_채우며_틈과_마진은_표값이다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeSheet.Render(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            RectTransform right = (RectTransform)FindIn(UiRoot.Instance.Sheet.transform, "anvil-side-right");
            Assert.IsNotNull(right, "오른 열(anvil-side-right)");
            RectTransform fb = (RectTransform)right.Find("forge-btn"), ab = (RectTransform)right.Find("auto-btn");
            Assert.IsNotNull(fb, "대장간 버튼(forge-btn)"); Assert.IsNotNull(ab, "자동 버튼(auto-btn)");
            float W = UiKit.RefW, rem = PopupKit.Rem;
            float gap = rem * UiKit.L("forge_actions_gap_rem");
            float autoW = W * UiKit.L("forge_auto_btn_w_f") + UiKit.L("forge_auto_btn_w_px");
            Assert.AreEqual(autoW, ab.rect.width, 0.5f, "자동 버튼 = 고정 폭 .0922W + 6.4px(환산) — 전엔 균등 반분 × .85");
            Assert.AreEqual(W * UiKit.L("forge_actions_ml_f"), fb.anchoredPosition.x, 0.5f, "대장간 버튼은 열 시작보다 −.0222W 왼쪽에서 시작");
            float fbRight = fb.anchoredPosition.x + fb.rect.width;
            Assert.AreEqual(gap, ab.anchoredPosition.x - fbRight, 0.5f, "두 버튼 사이 = 표 gap(.3rem)");
            float abRight = ab.anchoredPosition.x + ab.rect.width;
            Assert.AreEqual(right.rect.width - W * UiKit.L("forge_actions_mr_f"), abRight, 1.0f, "자동 버튼 우단 = 열 우단 − .092W(margin-right)");
            Assert.Greater(fb.rect.width, ab.rect.width, "대장간 버튼이 나머지를 채운다(자동보다 넓다)");
        }
    }
}
