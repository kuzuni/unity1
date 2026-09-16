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
    /// T368 5회차 — 정본 style.css 2548 `.league-reward-tier`: 리그 보상 **단 사이 대시 줄**(첫 단은 없음 · 2555 `:first-child { background-image: none }`).
    /// 재는 것: ⓐ 첫 단엔 dash 가 없고 둘째부터 있다 ⓑ 한 주기를 구워 Tiled 로 붙였다 ⓒ 타일 가로 = .0625W · 잉크 = .0323W(왼쪽 끝부터) · 두께 2 CSS px ⓓ 행 폭 전체.
    /// </summary>
    public class LeagueTierDashTests
    {
        static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null; yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            yield return null;
        }

        static bool Ink(Color c, Color ink) { return c.a > 0.5f && Mathf.Abs(c.r - ink.r) < 0.02f && Mathf.Abs(c.g - ink.g) < 0.02f && Mathf.Abs(c.b - ink.b) < 0.02f; }

        [UnityTest]
        public IEnumerator 리그_보상_단_사이에_정본_대시_줄이_한_타일로_깔리고_첫_단엔_없다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            LeagueSheet.OpenRewards(h);
            yield return null; yield return null;
            Popup p = PopupLayer.Instance.Find(LeagueSheet.RewardsName);
            Assert.IsNotNull(p, "리그 보상");
            int rows = 0, dashes = 0; Transform first = null, second = null;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (!rt.name.StartsWith("tier-", System.StringComparison.Ordinal)) continue;
                rows++;
                if (first == null) first = rt; else if (second == null) second = rt;
                if (rt.Find("dash") != null) dashes++;
            }
            Assert.GreaterOrEqual(rows, 2, "보상 단이 둘 이상");
            Assert.IsNull(first.Find("dash"), "첫 단(:first-child)엔 대시가 없다(정본 2555)");
            Assert.AreEqual(rows - 1, dashes, "둘째 단부터 단마다 대시 줄 하나");
            RectTransform d = (RectTransform)second.Find("dash");
            Assert.AreEqual(0, d.GetSiblingIndex(), "background — 행의 다른 조각보다 뒤");
            Image img = d.GetComponent<Image>();
            Assert.IsNotNull(img);
            Assert.AreEqual(Image.Type.Tiled, img.type, "되풀이는 Tiled 가 맡는다");
            Assert.IsNotNull(img.sprite, "줄무늬는 구워서 얹는다");
            Assert.IsFalse(img.raycastTarget, "장식은 클릭을 안 먹는다");
            Assert.AreEqual(0f, d.anchorMin.x, 1e-4f); Assert.AreEqual(1f, d.anchorMax.x, 1e-4f);
            Assert.AreEqual(0f, d.offsetMin.x, 0.01f, "position 0 0 — 행 왼쪽 끝부터"); Assert.AreEqual(0f, d.offsetMax.x, 0.01f, "size 100% — 행 오른쪽 끝까지");
            float W = UiKit.RefW;
            float band = Mathf.Max(1f, 2f * KeylineUi.CssPx);
            Assert.AreEqual(band, -d.offsetMin.y, 0.5f, "두께 = 2 CSS px(background-size 100% 2px)");
            Texture2D tex = img.sprite.texture;
            int tw = tex.width;
            Assert.AreEqual(0.0625f * W, tw, 1.0f, "타일 가로 = 정본 주기 .0625W");
            Color ink = UiKit.C("pp_line");
            Assert.IsTrue(Ink(tex.GetPixel(0, 0), ink), "x=0 은 대시(왼쪽 끝부터 잉크)");
            Assert.IsFalse(Ink(tex.GetPixel(tw - 1, 0), ink), "주기 끝은 빈틈(transparent .0323W~.0625W)");
            int inkCount = 0;
            for (int x = 0; x < tw; x++) if (Ink(tex.GetPixel(x, 0), ink)) inkCount++;
            Assert.AreEqual(0.0323f * W, inkCount, 2f, "대시 길이 = .0323W");
            h.Popups.Hide(LeagueSheet.RewardsName);
            yield return null;
        }
    }
}
