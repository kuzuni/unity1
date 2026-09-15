using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T371 2회차 — 모루 «들고 있는 장비» 카드의 세 색이 **표에서 섞여** 나온다: 정본 `style.css` 990 `.anvil-btn.held-slot`
    /// (면 `color-mix(in srgb, --rc 30%, #17181a)` · 테 `80%, #000`)과 1013 `.deck --dedge`(`55%, #dfe4ec`).
    /// 종전에는 비율(.7·.2·.45)과 상대색이 코드에 박혀 있었고, 가장자리 색은 `#DEE3ED` 로 정본 `#dfe4ec` 와 채널마다 1 어긋났다.
    /// </summary>
    public class ColorMixSitesTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && UiRoot.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 안 섰다");
            yield return null;
        }

        static void AssertMix(Color got, Color a, double f, int wr, int wg, int wb, string what)
        {
            double r, g, b;
            Forge.Core.Ui.ColorMixRules.SrgbOpaque(a.r * 255.0, a.g * 255.0, a.b * 255.0, wr, wg, wb, f, out r, out g, out b);
            Assert.AreEqual(r, got.r * 255.0, 1.0, what + ": R(정본 바이트 ±1)");
            Assert.AreEqual(g, got.g * 255.0, 1.0, what + ": G");
            Assert.AreEqual(b, got.b * 255.0, 1.0, what + ": B");
        }

        [UnityTest]
        public IEnumerator 표가_정본_비율과_상대색을_그대로_쥔다()
        {
            yield return Boot();
            Color rc = new Color(0x6b / 255f, 0x35 / 255f, 0x38 / 255f, 1f);      // 정본 var(--rc) 의 기본값
            AssertMix(ColorMixUi.Mix("held_face", rc), rc, 0.30, 0x17, 0x18, 0x1a, "모루 카드 면(정본 990 30%)");
            AssertMix(ColorMixUi.Mix("held_line", rc), rc, 0.80, 0, 0, 0, "모루 카드 테(정본 990 80%)");
            AssertMix(ColorMixUi.Mix("held_deck_edge", rc), rc, 0.55, 0xdf, 0xe4, 0xec, "겹친 장 가장자리(정본 1013 55%, #dfe4ec)");

            // 손으로 적었던 옛 색(#DEE3ED)과는 **다르다** — 그것이 이 회차가 고친 것이다.
            double r, g, b;
            Forge.Core.Ui.ColorMixRules.SrgbOpaque(rc.r * 255.0, rc.g * 255.0, rc.b * 255.0, 0.87 * 255, 0.89 * 255, 0.93 * 255, 0.55, out r, out g, out b);
            Assert.AreNotEqual(Mathf.RoundToInt((float)r), Mathf.RoundToInt(ColorMixUi.Mix("held_deck_edge", rc).r * 255f), "옛 손입력 색이면 이 회차가 한 일이 없다");

            // 투명과 섞는 자리(그림자)는 색이 그대로고 알파만 준다 — 정본 8538 `.equip-cell:not(.egg-cell)` 62%.
            Color sh = ColorMixUi.Mix("cell_shadow_2", rc);
            Assert.AreEqual(0.62f, sh.a, 0.002f, "그림자는 알파를 만드는 섞기다");
            Assert.AreEqual(rc.r, sh.r, 0.004f, "색은 그대로");
        }

        [UnityTest]
        public IEnumerator 모루_들고_있는_장비_카드가_표_색으로_선다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            Forge.Core.Forging.ForgeItem it = h.Engine.RollItem();
            h.SetPendingCraft(it);
            ForgeSheet.Render(h);
            yield return null;

            // 대기품 카드는 «들고 있는 장비» 그림(held-img)을 쥔 상자다 — 이름이 "card" 인 상자는 팝업에도 있으므로 그림으로 집는다.
            Transform card = null;
            foreach (Transform t in UiRoot.Instance.Sheet.GetComponentsInChildren<Transform>(true))
                if (t.name == "held-img" && t.parent != null && t.parent.name == "card") { card = t.parent; break; }
            Assert.IsNotNull(card, "모루 자리에 대기품 카드가 서야 한다");
            Image frame = card.Find("frame").GetComponent<Image>();
            Color ac = ForgeUi.AgeColor(h.Defs, it.Age);
            AssertMix(frame.color, ac, 0.30, 0x17, 0x18, 0x1a, "카드 면이 표대로 섞였다");
        }
    }
}
