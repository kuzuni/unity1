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
    /// T482 2회차 — 확률 정보 팝업의 알약 줄(정본 `ui.js` `.fi-pills` · 코인 · 젬)은 정본 `style.css` 5064
    /// `.fi-pills { display: flex; justify-content: center; gap: .8rem }` 의 가로 흐름이다: 두 알약 사이 틈은 표 `fi_pills_gap_rem`(.8rem).
    /// 전엔 `rem * 1f` 가 코드에 박혀 있었다(T33 57회차 실측 · +7.2px · T388 4회차가 T364 축이라 미룬 자리).
    /// </summary>
    public class FiPillsGapTests
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
            yield return null;
        }

        private static Transform FindIn(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        [Test]
        public void 표는_알약_줄의_틈을_정본_8rem_으로_쥔다()
        {
            Assert.AreEqual(0.8f, ForgeInfoStyle.L("fi_pills_gap_rem"), 1e-6f, "style.css 5064 `.fi-pills { gap: .8rem }`");
        }

        [UnityTest]
        public IEnumerator 확률_정보의_코인_젬_알약_사이_틈은_표값이고_줄은_가운데다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeInfoPopup.Open(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Popup p = h.Meta.Popups.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(p, "확률 정보 팝업");
            RectTransform pills = (RectTransform)FindIn(p.Root, "pills");
            Assert.IsNotNull(pills, "알약 줄(pills)");
            RectTransform coin = (RectTransform)pills.Find("coin"), gem = (RectTransform)pills.Find("gem");
            Assert.IsNotNull(coin, "코인 알약"); Assert.IsNotNull(gem, "젬 알약");
            float gap = PopupKit.Rem * ForgeInfoStyle.L("fi_pills_gap_rem");
            float coinRight = coin.anchoredPosition.x + coin.rect.width;
            Assert.AreEqual(gap, gem.anchoredPosition.x - coinRight, 0.5f, "두 알약 사이 = 표 fi_pills_gap_rem(정본 .8rem) — 전엔 1rem 박힘");
            float pillMin = PopupKit.Rem * ForgeInfoStyle.L("fi_pill_min_w_rem");
            Assert.GreaterOrEqual(coin.rect.width + 0.5f, pillMin, "코인 알약은 하한(5.5rem) 이상");
            Assert.GreaterOrEqual(gem.rect.width + 0.5f, pillMin, "젬 알약은 하한(5.5rem) 이상");
            // 정본 `justify-content: center` — 줄 전체가 가운데: 왼쪽 여백 = 오른쪽 여백(±1px)
            float left = coin.anchoredPosition.x, right = pills.rect.width - (gem.anchoredPosition.x + gem.rect.width);
            Assert.AreEqual(left, right, 1.0f, "알약 줄은 가운데(왼 여백 = 오른 여백)");
            ForgeInfoPopup.Close(h);
            yield return null;
        }
    }
}
