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
    /// T387 — 퀘스트 시트의 치수가 **표값 그대로**인가(코드가 곱을 얹지 않았는가).
    /// 정본 `style.css` **2058** `.qst-right .btn { min-width: 3.9rem; min-height: 1.9rem }` 이고
    /// 클론 표는 그 수를 이미 정확히 쥔다(`quest_btn_w` 0.0739 = 3.9rem/H · `quest_btn_h` 0.036 = 1.9rem/H)는데
    /// `QuestSheet.Render` 가 ×1.6·×1.3 을 얹어 6.24rem·2.47rem 으로 그려 왔다.
    /// 진행 바도 같다 — 정본 **2039** `.qst-bar { height: .95rem }` 에 ×1.6 이 얹혀 1.28rem 이었다.
    ///
    /// 정본 수는 «하한» 이지만 [수령] 글자(«수령» 두 자)와 패딩이 3.9rem·1.9rem 을 못 넘으므로
    /// **하한이 곧 실제 크기**다 — 그래서 고정 폭으로 겨눠도 된다.
    /// [일괄수령]만 다르다: 정본 `.qst-allbar .btn.sm`(2020·674)은 하한이 없고 `padding: .3rem .6rem` 이라
    /// **글자 폭 + 패딩**이 곧 폭이다 — 그것을 [수령] 폭의 ×1.6 으로 쓰던 것도 걷었다.
    /// </summary>
    public class QuestBtnSizeTests
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

        static Rect World(RectTransform rt)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y);
        }

        [UnityTest]
        public IEnumerator 퀘스트_수령_버튼과_진행_바가_표값_그대로_선다()
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

            float rem = PopupKit.Rem;
            float wantW = UiKit.H("quest_btn_w"), wantH = UiKit.H("quest_btn_h");
            float wantBar = QuestStyle.L("bar_h_rem") * rem;

            // 표값이 정본 수 그대로인지 먼저 — 곱을 걷어도 표가 틀리면 그림은 여전히 틀리다.
            Assert.AreEqual(3.9f, wantW / rem, 0.02f, "quest_btn_w 는 정본 .qst-right .btn 의 min-width 3.9rem 이다");
            Assert.AreEqual(1.9f, wantH / rem, 0.02f, "quest_btn_h 는 정본 min-height 1.9rem 이다");
            Assert.AreEqual(0.95f, wantBar / rem, 0.02f, "QuestUi.bar_h_rem 은 정본 .qst-bar 의 .95rem 이다");

            RectTransform claim = null, bar = null, all = null;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (!rt.gameObject.activeInHierarchy) continue;
                if (claim == null && rt.name == "claim") claim = rt;
                if (bar == null && rt.name == "bar") bar = rt;
                if (all == null && rt.name == "claim-all") all = rt;
            }
            Assert.IsNotNull(claim, "[수령] 버튼을 못 찾았다 — 퀘스트 행이 안 그려졌다");
            Assert.IsNotNull(bar, "진행 바를 못 찾았다");
            Assert.IsNotNull(all, "[일괄수령] 버튼을 못 찾았다");

            Rect cr = World(claim), br = World(bar), ar = World(all);
            // 캔버스 → 화면 배율은 **재는 대상과 무관한 것**에서 뽑는다(버튼 높이로 뽑으면 그 단언이 항등식이 된다).
            float px = World(p.Root).width / UiKit.RefW;
            Assert.Greater(px, 0f, "배율이 0 이면 잰 값이 없다");

            Assert.AreEqual(wantW * px, cr.width, wantW * px * 0.04f, "[수령] 폭은 표값 3.9rem 그대로다(×1.6 이 걷혔다)");
            Assert.AreEqual(wantH * px, cr.height, wantH * px * 0.04f, "[수령] 높이는 표값 1.9rem 그대로다(×1.3 이 걷혔다)");
            Assert.AreEqual(wantBar * px, br.height, wantBar * px * 0.06f, "진행 바 높이는 정본 .95rem 이다(×1.6 이 걷혔다)");

            // [일괄수령]은 [수령] 폭과 **아무 관계가 없다** — 정본에서 하한 없이 내용 크기로 선다.
            Assert.Less(ar.width, wantW * px * 1.6f * 0.99f,
                "[일괄수령] 폭이 아직 [수령] 폭의 ×1.6 이다 — 정본은 글자 폭 + 패딩이다");
            float pad = QuestStyle.L("allbar_btn_pad_x_rem") * rem * px;
            Assert.Greater(ar.width, pad * 2f, "[일괄수령] 폭은 적어도 좌우 패딩보다는 넓다");
        }
    }
}
