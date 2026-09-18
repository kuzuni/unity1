using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T177 — 장비 시대 상세(`#forge-item-modal`) 카드가 정본 화면별 덮어쓰기대로 서는가. 정본 `style.css` 3649 `width: 68.9%` · 3720 `padding: 1.7% 2.7%` ·
    /// 3723 `.idet-subs { margin-top: 12.9%; padding: 3.3% 4% 4.4%; gap: 0 }` · 3714 «행 피치 1.93%H» — 표는 `Resources/ForgeItemUi.json`.
    /// 원본 실측(shot-042931): 카드 폭 68.90%W · 높이 47.56%H(25.20~72.76) · 회색 판 38.37~71.40%H. 클론(런 413)은 73.70%W · 53.54%H 였다(공용 75% + 행 피치 2.47%H).
    /// 판정: 폭 ±1%p · 카드 위 → 판 위 13.17%H ±1%p · 행 피치 1.93%H ±0.1%p · 높이 47.56%H ±2.5%p(lead 글자가 §1 하한 Sub 라 정본 .92rem 보다 커서 두 줄 높이가 조금 남는다).
    /// </summary>
    public class ForgeItemDetailTests
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

        private static Rect World(RectTransform rt)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y);
        }

        private static RectTransform FindUnder(RectTransform parent, string objName)
        {
            foreach (RectTransform rt in parent.GetComponentsInChildren<RectTransform>(true))
                if (rt != parent && rt.name == objName) return rt;
            Assert.Fail(parent.name + " 아래에 «" + objName + "» 이 없다");
            return null;
        }

        [UnityTest]
        public IEnumerator 상세_카드는_폭_68_9퍼센트W_판_위치와_행_피치가_정본이다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;

            // 표값 자체가 정본과 같은가(수치는 ForgeItemUi.json 에 · 정본 style.css 3649 · 3714 · 3720 · 3723)
            Assert.AreEqual(0.689f, ForgeItemStyle.L("card_w"), 1e-6f, "3649 width 68.9%");
            Assert.AreEqual(0.0193f, ForgeItemStyle.L("row_pitch_h"), 1e-6f, "3714 행 피치 1.93%H");
            Assert.AreEqual(0.129f, ForgeItemStyle.L("subs_margin_top_wrap_f"), 1e-6f, "3723 margin-top 12.9%");
            Assert.AreEqual(0f, ForgeItemStyle.L("card_gap_rem"), 1e-6f, "3648 gap 0");

            ForgeInfoPopup.OpenList(h);
            yield return null;
            string age = h.Defs.Ages[0];
            string wt = h.Engine.WeaponsOfAge(age)[0];
            ForgeInfoPopup.OpenDetail(h, age, "weapon", 0, wt);
            yield return null;
            yield return null;
            Popup p = h.Meta.Popups.Find(ForgeInfoPopup.ItemName);
            Assert.IsNotNull(p, "장비 상세 팝업이 열려 있다");
            yield return SettleCardPop();   // T149 — 팝이 도는 동안 재면 폭이 연출 중간값이다
            Canvas.ForceUpdateCanvases();
            yield return null;

            RectTransform card = FindUnder(p.Root, "card");
            RectTransform subs = FindUnder(card, "idet-subs");
            Rect app = World(UiRoot.Instance.App), rc = World(card), rs = World(subs);
            Assert.Greater(app.height, 0f, "앱 상자");

            // ⓐ 폭 — 앱 폭의 68.9% (±1%p) · 정본 «원본 실측 339/492»
            // T473 — 카드 rect 는 패딩 상자 · 정본 3649 width 68.9% 는 카드 몸(테 포함)이라 테 두 겹을 더해 견준다
            float bodyW = rc.width / app.width + PopupKit.Line3 * 2f / UiKit.RefW;
            Assert.AreEqual(0.689f, bodyW, 0.01f, "카드 몸 폭 = 앱 폭 × 68.9%(정본 3649 · 공용 75% 가 아니다) · 실측 " + bodyW);
            Assert.AreEqual(app.center.x, rc.center.x, app.width * 0.01f, "카드는 가로 가운데");
            // ⓑ 회색 판 위치 — 카드 위에서 판 위까지 = 원본 38.37 − 25.20 = 13.17%H (±1%p) · 판은 카드 안
            float panelTop = (rc.yMax - rs.yMax) / app.height;
            Assert.AreEqual(0.1317f, panelTop, 0.01f, "카드 위 → 회색 판 위 13.17%H(원본 25.20→38.37) · 실측 " + panelTop);
            Assert.Greater(rs.yMin, rc.yMin - 1f, "판 바닥은 카드 안");
            Assert.Less(rs.xMin - rc.xMin, app.width * 0.05f, "판은 카드 좌우 패딩(2.7%W) 안쪽");
            // ⓒ 행 피치 — 이웃한 하위 스탯 행의 세로 간격 = 1.93%H (±0.1%p) · 정본이 «2.47%H 로 13행 누적 +6.14%p» 라 적은 그 병의 자리
            var rows = new List<RectTransform>();
            foreach (RectTransform rt in subs.GetComponentsInChildren<RectTransform>(true)) if (rt.name.StartsWith("substat-")) rows.Add(rt);
            Assert.GreaterOrEqual(rows.Count, 2, "하위 스탯 행이 둘 이상");
            float pitch = (World(rows[0]).center.y - World(rows[1]).center.y) / app.height;
            Assert.AreEqual(0.0193f, pitch, 0.001f, "행 피치 1.93%H · 실측 " + pitch);
            // ⓓ 카드 높이 — 원본 47.56%H(25.20~72.76) · lead 글자가 §1 하한(Sub)이라 정본 .92rem 보다 커서 ±2.5%p
            float cardH = rc.height / app.height;
            Assert.AreEqual(0.4756f, cardH, 0.025f, "카드 높이 47.56%H(원본 25.20~72.76) · 실측 " + cardH);
            Debug.Log("[T177] 카드 폭 " + (rc.width / app.width).ToString("0.000") + "W · 높이 " + cardH.ToString("0.000") + "H · 판 위 " + panelTop.ToString("0.000") + "H · 행 피치 " + pitch.ToString("0.0000") + "H · 행 " + rows.Count);

            ForgeInfoPopup.CloseItemDetail(h);
            h.Meta.Popups.Hide(ForgeInfoPopup.Name);
            yield return null;
        }
    }
}
