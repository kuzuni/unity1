using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T395 — 프로필/설정 카드는 **앱 가운데**에 선다. 정본 `#profile-modal .idet-wrap { top: .8rem }`(style.css 3042)은 CSS 가 만든 위쪽 밀림을 되돌리는 보정값이지
    /// 자리가 아니다 — 클론 `PopupKit.Card` 는 카드 자체를 가운데 두므로 그 값을 옮기면 카드가 .8rem 아래로 한 번 더 간다(런 743 · 위끝 16.88%H ↔ 원작 14.73%H).
    /// </summary>
    public class ProfileCardTests
    {
        private static void DeleteSave()
        {
            try
            {
                string p = System.IO.Path.Combine(Application.persistentDataPath, (SaveIo.Defs != null ? SaveIo.Defs.SaveKey : "forgeclone_save_v1") + ".json");
                if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
            }
            catch (System.Exception) { }
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
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다");
            Assert.IsNotNull(PopupLayer.Instance, "팝업 층이 서지 않았다");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 프로필_카드는_정본_보정값_없이_앱_가운데에_선다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            ProfilePopup.Open(h);
            yield return null;
            Popup p = h.Popups.Find(ProfilePopup.Name);
            Assert.IsNotNull(p, "프로필 팝업");
            RectTransform card = null;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true)) if (rt.name == "card") { card = rt; break; }
            Assert.IsNotNull(card, "카드(card)");
            Assert.AreEqual(0f, card.anchoredPosition.y, 0.5f, "카드 세로 오프셋 0 — 정본 .8rem 은 CSS 보정값이라 옮기지 않는다");
            Assert.AreEqual(0.5f, card.anchorMin.y, 1e-4f); Assert.AreEqual(0.5f, card.pivot.y, 1e-4f, "가운데 앵커·피벗");
            Assert.AreEqual(UiKit.L("profile_h") * UiKit.RefH, card.rect.height, 0.5f, "카드 높이는 표(profile_h)대로 — 건드리지 않는다");
            Assert.AreEqual(UiKit.L("profile_w") * UiKit.RefW, card.rect.width, 0.5f, "카드 폭은 표(profile_w)대로");
            ProfilePopup.Close(h);
            yield return null;
        }
    }
}
