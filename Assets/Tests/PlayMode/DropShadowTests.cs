using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T332 — 정본 `filter: drop-shadow(dx dy blur color)` 중 **흐림이 있는** 자리(<see cref="DropShadow"/>).
    /// 기구가 앞의 둘과 다르다: `UnityEngine.UI.Shadow` 는 흐림이 없고 `UiShadow.Drop`(T331)은 둥근 상자 전용이라,
    /// 스프라이트 알파를 <see cref="UiFilter.Blur"/>(T342)로 흐려 구운 사본을 **뒤에 깐다**.
    /// 자기 파일인 이유: `PassPopupTests` 는 없고 `PopupZTests`·`TabularSitesTests` 는 다른 절의 자리다(check_claim_scope).
    /// </summary>
    public class DropShadowTests
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
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다");
            yield return null;
        }

        /// <summary>
        /// 정본 `style.css` 2698 `.pass-sword { … filter: drop-shadow(.14rem .18rem .16rem rgba(0,0,0,.45)) }`
        /// — 진행 패스 카드 위에 꽂힌 검. **가로 오프셋이 있는 유일한 자리**라 dx 도 같이 본다.
        /// </summary>
        [UnityTest]
        public IEnumerator 진행_패스의_검은_정본_흐린_그림자를_뒤에_깐다()
        {
            yield return Boot();
            PlayLog log = PlayLog.Start("drop-shadow-pass");
            MetaHost h = MetaHost.Instance;
            PassPopup.Open(h);
            yield return null;
            Popup p = PopupLayer.Instance.Find(PassPopup.Name);
            Assert.IsNotNull(p, "진행 패스 팝업이 열린다");
            Transform sword = FindIn(p.Root, "pass-sword");
            Assert.IsNotNull(sword, "패스 검");
            Transform sh = sword.parent.Find(DropShadow.Name);
            Assert.IsNotNull(sh, "검 뒤에 흐린 그림자를 깔았다(정본 2698)");

            Image si = sh.GetComponent<Image>();
            Image swi = sword.GetComponent<Image>();
            Assert.IsNotNull(si.sprite, "그림자도 그림이 있다(흐려 구운 사본)");
            Assert.AreNotSame(swi.sprite, si.sprite, "흐린 사본이지 원본 그대로가 아니다");
            Assert.Less(sh.GetSiblingIndex(), sword.GetSiblingIndex(), "그림자는 검 **뒤**에 그린다");

            float css = KeylineUi.CssPx;
            Vector2 d = ((RectTransform)sh).anchoredPosition - ((RectTransform)sword).anchoredPosition;
            Assert.AreEqual(DropShadowUi.Px("pass_sword", "dx_px") * css, d.x, 0.01f, "가로 오프셋 = 표 dx(정본 .14rem)");
            Assert.AreEqual(-DropShadowUi.Px("pass_sword", "dy_px") * css, d.y, 0.01f, "세로 오프셋 = 표 dy 만큼 **아래**(유니티 −y)");
            Color want = DropShadowUi.C("pass_sword");
            Assert.AreEqual(want.a, si.color.a, 2f / 255f, "알파 = 표 pass_sword(.45)");
            Assert.Less(si.color.r + si.color.g + si.color.b, 0.05f, "색은 검정");

            // 흐림이 실제로 걸렸는가 — 구운 사본은 커널 반경만큼 테두리가 넓어진다(UiFilter.Blur 주석).
            Assert.Greater(si.sprite.rect.width, swi.sprite.rect.width, "흐린 사본은 번짐이 안 잘리게 원본보다 넓게 구워진다");

            PassPopup.Close(h);
            yield return null;
            log.AssertNoRed();
            log.Dispose();
        }

        private static Transform FindIn(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
    }
}
