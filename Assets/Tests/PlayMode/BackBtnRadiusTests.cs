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
    /// T345 20회차 — 시트 좌하단 ◀ 버튼의 모서리는 정본 5189 `.btn.back-btn { border-radius: calc(var(--app-w) * .0094) }` 다.
    /// 정본 주석이 그 수를 못 박는다: «원본 바깥 상자 35×25px 의 모서리 반경 ≈5.5px … 클론 상자 높이 16.7px 기준 3.7px = 앱 폭의 0.94%.
    /// 종전 50%/.6rem 은 세로 22.6px 상자에서 통짜 알약으로 읽혔다.» 클론은 리그 뒤로 버튼 값 `Rem * 0.45f`(≈1.6배)를 쓰고 있었다.
    ///
    /// **앱 폭 비율**이라 rem 으로 옮겨 적으면 안 된다 — 표 꼬리 `_r_w` 가 그 단위를 쥐고 환산은 <see cref="RadiusUi.Px"/> 한 군데다.
    /// 같은 회차의 퀘스트 줄(정본 2023 `.qst-row` .8rem · 표 `qst_row_r_rem`)도 같은 시트에서 함께 본다.
    /// </summary>
    public class BackBtnRadiusTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 준비되지 않았다");
            yield return null;
        }

        [TearDown]
        public void CleanSave()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
        }

        static float Mult(string key) { return UiShapes.RoundedMultiplier(RadiusUi.Px(key)); }

        [Test]
        public void 뒤로_버튼_값은_rem_이_아니라_앱_폭_비율이다()
        {
            Assert.AreEqual(0.0094f * UiKit.RefW, RadiusUi.Px("back_btn_r_w"), 1e-3f,
                "정본 5189 `calc(var(--app-w) * .0094)`");
            // 종전 값(Rem * 0.45)보다 뚜렷이 작다 — 같아지면 리터럴로 되돌아간 것이다
            Assert.Less(RadiusUi.Px("back_btn_r_w"), 0.45f * RadiusUi.PxPerRem * 0.8f,
                "정본 주석이 말하는 «통짜 알약» 으로 되돌아갔다");
            Assert.AreEqual(0.8f * RadiusUi.PxPerRem, RadiusUi.Px("qst_row_r_rem"), 1e-3f,
                "정본 2023 `.qst-row { border-radius: .8rem }`");
        }

        [UnityTest]
        public IEnumerator 퀘스트_시트의_뒤로_버튼과_줄_카드가_표의_반지름으로_선다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            QuestSheet.Open(h);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            Popup p = h.Popups.Find(QuestSheet.Name);
            Assert.IsNotNull(p, "퀘스트 시트가 열려 있다");

            Transform back = null;
            int rows = 0;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name == "back-btn" && back == null) back = rt;
                // 퀘스트 줄 = 칸 `q-<id>` 안의 `row` 안의 테·면 상자 `face`(PopupKit.Outlined 계층)
                else if (rt.name == "face" && rt.parent != null && rt.parent.name == "row"
                         && rt.parent.parent != null && rt.parent.parent.name.StartsWith("q-"))
                {
                    Image line = rt.Find("line").GetComponent<Image>();
                    Assert.AreEqual(UiShapes.Rounded, line.sprite, "퀘스트 줄 테: 둥근 9-슬라이스");
                    Assert.AreEqual(Mult("qst_row_r_rem"), line.pixelsPerUnitMultiplier, 1e-3f,
                        "퀘스트 줄 테: 정본 2023 .8rem(표 qst_row_r_rem)");
                    rows++;
                }
            }
            Assert.Greater(rows, 0, "퀘스트 줄이 하나도 없다");

            Assert.IsNotNull(back, "시트 좌하단 뒤로 버튼(back-btn)");
            Image bLine = back.Find("line").GetComponent<Image>();
            Assert.AreEqual(UiShapes.Rounded, bLine.sprite, "뒤로 버튼 테: 둥근 9-슬라이스");
            Assert.AreEqual(Mult("back_btn_r_w"), bLine.pixelsPerUnitMultiplier, 1e-3f,
                "뒤로 버튼 테: 정본 5189 앱 폭 × .0094(표 back_btn_r_w)");
            h.Popups.Hide(QuestSheet.Name);
            yield return null;
        }
    }
}
