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
    /// T346 — 정본이 탭바 위(z 40 > `#tabbar` 30)에 띄우는 팝업이 클론에서도 위 층(`modals-over`)에 서는가.
    /// 기준은 코드의 수가 아니라 표 <c>PopupZUi.json</c> — 팝업을 실제로 열어 뿌리의 부모 층을 표와 견준다.
    /// 자기 파일인 이유: `OfflinePopupTests`·`ChatShareIconTests` 는 다른 절의 자리다(check_claim_scope).
    /// </summary>
    public class PopupZTests
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
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다 (SaveIo → meta.json)");
            Assert.IsNotNull(PopupLayer.Instance, "팝업 층이 서지 않았다");
            yield return null;
        }

        /// <summary>열린 팝업의 뿌리가 표대로의 층에 있는가 — 위 층이면 `modals-over` 의 자식, 아니면 그 밖.</summary>
        private static void AssertLayer(string name)
        {
            Popup p = PopupLayer.Instance.Find(name);
            Assert.IsNotNull(p, name + " 팝업이 안 열렸다");
            bool wantAbove = PopupZUi.AboveTabBar(name);
            bool isAbove = p.Root.parent == PopupLayer.Instance.OverLayer;
            Assert.AreEqual(wantAbove, isAbove,
                name + ": 정본 z " + PopupZUi.Z(name) + " ↔ 탭바 z " + PopupZUi.TabbarZ + " 이므로 " + (wantAbove ? "탭바 위 층(modals-over)" : "탭바 아래 층(modals)") + " 이어야 한다");
            Assert.AreEqual(wantAbove, p.AboveTabBar, name + ": Popup.AboveTabBar 표시가 층과 다르다");
        }

        [UnityTest]
        public IEnumerator 오프라인_프로필_팝업은_정본_z_표대로_탭바_위_층에_선다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;

            // 표 자체 — 정본 3781 이 이 둘을 탭바(3766) 위에 둔다 · 장비 상세(1726)는 아래
            Assert.IsTrue(PopupZUi.AboveTabBar(OfflinePopup.Name), "표: #offline-modal 은 탭바 위");
            Assert.IsTrue(PopupZUi.AboveTabBar(ProfilePopup.Name), "표: #profile-modal 은 탭바 위");
            Assert.IsFalse(PopupZUi.AboveTabBar(GearDetailPopup.Name), "표: #gear-detail-modal 은 탭바 아래");
            Assert.Greater(PopupLayer.Instance.OverLayer.GetSiblingIndex(), UiRoot.Instance.TabBand.GetSiblingIndex(), "modals-over 는 탭 띠보다 뒤(위) 형제");

            OfflinePopup.Show(h, Offline.RewardFor(SaveIo.Defs, 3600, SaveIo.Mults));
            yield return null;
            AssertLayer(OfflinePopup.Name);
            OfflinePopup.Close(h);
            yield return null;
            Assert.IsFalse(h.Popups.IsOpen(OfflinePopup.Name));

            ProfilePopup.Open(h);
            yield return null;
            AssertLayer(ProfilePopup.Name);
            ProfilePopup.Close(h);
            yield return null;
            Assert.IsFalse(h.Popups.IsOpen(ProfilePopup.Name));
        }
    }
}
