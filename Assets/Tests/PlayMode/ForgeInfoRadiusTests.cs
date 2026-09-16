using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T345 21회차 — T33 20회차가 «어긋난 리터럴» 로 센 열 자리의 **마지막 둘**:
    /// 정본 3703 `.idet-subs { border-radius: .8rem }`(클론은 `rem * 0.6f` 였다 · `#forge-item-modal` 3722 는 반지름을 안 덮는다) ·
    /// 정본 694 `.upg-progress { border-radius: .55rem }`(클론은 `rem * 0.5f` 였다).
    /// 둘 다 `ForgeInfoPopup.cs` 안이라 T332·T339 lock 이 풀릴 때까지 자의 KNOWN 으로 기다린 자리다.
    ///
    /// 업그레이드 막대는 «진행 중» 일 때만 그려지므로 여기서는 표값만 겨눈다 — 배선(그 자리가 표 키를 부르는가)은
    /// `tools/check_border_radius.py` 의 세 겹이 본다.
    /// </summary>
    public class ForgeInfoRadiusTests
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

        [TearDown]
        public void CleanSave()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
        }

        static IEnumerator SettleCardPop()
        {
            for (int i = 0; i < 600 && Object.FindObjectsByType<CardPop>(FindObjectsSortMode.None).Length > 0; i++) yield return null;
            Assert.AreEqual(0, Object.FindObjectsByType<CardPop>(FindObjectsSortMode.None).Length, "카드 팝이 600프레임 안에 안 끝났다(T149)");
        }

        [Test]
        public void 표는_정본_8rem_과_55rem_을_쥐고_옛_리터럴과_다르다()
        {
            float px = RadiusUi.PxPerRem;
            Assert.AreEqual(0.8f * px, RadiusUi.Px("idet_subs_r_rem"), 1e-3f, "정본 3703 `.idet-subs` .8rem");
            Assert.AreEqual(0.55f * px, RadiusUi.Px("upg_progress_r_rem"), 1e-3f, "정본 694 `.upg-progress` .55rem");
            // 옛 리터럴(.6 · .5)로 되돌아가면 여기서 걸린다
            Assert.Greater(Mathf.Abs(0.6f * px - RadiusUi.Px("idet_subs_r_rem")), 1e-3f, "회색 판이 옛 리터럴 .6rem 으로 되돌아갔다");
            Assert.Greater(Mathf.Abs(0.5f * px - RadiusUi.Px("upg_progress_r_rem")), 1e-3f, "업그레이드 막대가 옛 리터럴 .5rem 으로 되돌아갔다");
        }

        [UnityTest]
        public IEnumerator 장비_상세의_회색_판이_표의_8rem_으로_선다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeInfoPopup.OpenList(h);
            yield return null;
            string age = h.Defs.Ages[0];
            string wt = h.Engine.WeaponsOfAge(age)[0];
            ForgeInfoPopup.OpenDetail(h, age, "weapon", 0, wt);
            yield return null;
            yield return null;
            Popup p = h.Meta.Popups.Find(ForgeInfoPopup.ItemName);
            Assert.IsNotNull(p, "장비 상세 팝업이 열려 있다");
            yield return SettleCardPop();
            Canvas.ForceUpdateCanvases();
            yield return null;

            RectTransform subs = null;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "idet-subs") { subs = rt; break; }
            Assert.IsNotNull(subs, "회색 판(idet-subs)");
            Image bg = subs.Find("bg").GetComponent<Image>();
            Assert.AreEqual(UiShapes.Rounded, bg.sprite, "회색 판: 둥근 9-슬라이스");
            Assert.AreEqual(UiShapes.RoundedMultiplier(RadiusUi.Px("idet_subs_r_rem")), bg.pixelsPerUnitMultiplier, 1e-3f,
                "회색 판 모서리: 정본 3703 .8rem(표 idet_subs_r_rem)");
        }
    }
}
