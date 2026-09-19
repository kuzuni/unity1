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
    /// T482 4회차 — 시대 막대 공장(`ForgeUi.AgeBar`)의 두 틈: 자동 제련 막대는 정본 4701 `.af-age-bar { gap: .45rem }`(체크 ↔ 이름) ·
    /// 두 막대 공통으로 4717 `.af-age-name .ico, .fi-age-name .ico { margin-right: .14rem }`(아이콘 ↔ 이름 글자 · 아이콘은 이름 안 인라인 조각).
    /// 확률 정보 막대(5083 `.fi-age-bar`)엔 gap 이 없고 이름의 `padding-left: .5rem` 이 아이콘의 시작이다. 전엔 `rem*0.4f`·`rem*0.35f` 가 박혀 있었다.
    /// </summary>
    public class AgeBarGapTests
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

        [Test]
        public void 표는_두_틈을_정본대로_쥔다()
        {
            Assert.AreEqual(0.45f, UiKit.L("af_age_bar_gap_rem"), 1e-6f, "4701 .af-age-bar gap .45rem");
            Assert.AreEqual(0.14f, UiKit.L("age_bar_ico_mr_rem"), 1e-6f, "4717 .ico margin-right .14rem");
        }

        [UnityTest]
        public IEnumerator 자동_제련_막대는_체크_이름_틈이_45rem_이고_두_막대의_아이콘_이름_틈은_14rem_이다()
        {
            yield return Boot();
            ForgeHost fh = ForgeHost.Instance;
            float rem = PopupKit.Rem;
            float gap = rem * UiKit.L("af_age_bar_gap_rem"), mr = rem * UiKit.L("age_bar_ico_mr_rem");

            fh.S.BestChapter = 3; fh.S.BestStage = 1; fh.Pull();
            ForgeAutoPopup.Open(fh);
            yield return null; yield return null;
            RectTransform af = null;
            foreach (RectTransform rt in Object.FindObjectsOfType<RectTransform>(true)) if (rt.name.StartsWith("af-age-") && rt.Find("ico") != null) { af = rt; break; }
            Assert.IsNotNull(af, "자동 제련 시대 막대(af-age-*)");
            RectTransform chk = (RectTransform)af.Find("check"), ico = (RectTransform)af.Find("ico"), nm = (RectTransform)af.Find("name");
            Assert.IsNotNull(chk, "체크 상자"); Assert.IsNotNull(ico, "시대 아이콘"); Assert.IsNotNull(nm, "이름");
            Assert.AreEqual(gap, ico.anchoredPosition.x - (chk.anchoredPosition.x + chk.rect.width), 0.5f, "체크 ↔ 아이콘(이름의 시작) = 표 .45rem — 전엔 .4rem 박힘");
            Assert.AreEqual(mr, nm.anchoredPosition.x - (ico.anchoredPosition.x + ico.rect.width), 0.5f, "아이콘 ↔ 이름 글자 = 표 .14rem — 전엔 .35rem 박힘");
            ForgeAutoPopup.Close(fh);
            yield return null;

            ForgeInfoPopup.Open(fh);
            yield return null; yield return null;
            Popup p = fh.Meta.Popups.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(p, "확률 정보 팝업");
            RectTransform fi = null;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true)) if (rt.name.StartsWith("age-") && rt.Find("ico") != null) { fi = rt; break; }
            Assert.IsNotNull(fi, "확률 정보 시대 막대(age-*)");
            RectTransform fico = (RectTransform)fi.Find("ico"), fnm = (RectTransform)fi.Find("name");
            Assert.IsNull(fi.Find("check"), "확률 정보 막대엔 체크가 없다");
            Assert.AreEqual(rem * 0.5f, fico.anchoredPosition.x, 0.5f, "아이콘은 이름의 padding-left .5rem 에서 시작(5088)");
            Assert.AreEqual(mr, fnm.anchoredPosition.x - (fico.anchoredPosition.x + fico.rect.width), 0.5f, "아이콘 ↔ 이름 글자 = 표 .14rem(두 막대 공통 4717)");
            ForgeInfoPopup.Close(fh);
            yield return null;
        }
    }
}
