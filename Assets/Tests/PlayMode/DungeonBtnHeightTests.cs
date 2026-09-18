using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T481 — 던전 상세 버튼 둘: 정본 5355 `.dgd-btns .btn { min-height: 3.4rem }` 은 **최솟값**이고 5356 `.dgd-btn { font-size: .92rem; padding: .6rem .2rem; line-height: 1.25 }` 이 높이를 정한다.
    /// 종전엔 3.4rem 을 고정 높이로 쓰고 글자는 종류 칸 `Button`(1.209rem · +31%)이라 «글자는 크고 상자는 작은» 상쇄였다(런 1219 · 원작 상자 7.80%H ↔ 클론 6.35).
    /// 이제 글자 = `Micro` + 표 `dgd_btn` .92 · 높이 = max(3.4, 줄수 × 글자 × 1.25 + .6 × 2 + 키라인 × 2) · 두 버튼은 같은 높이(`flex: 1 1 0`).
    /// </summary>
    public class DungeonBtnHeightTests
    {
        static float Rem { get { return UiKit.L("rem_h") * UiKit.RefH; } }

        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = Time.realtimeSinceStartup;
            while (!DungeonUiHost.Ready)
            {
                Assert.Less(Time.realtimeSinceStartup - t, 20f, "DungeonUiHost 가 20초 안에 Ready 되지 않았다");
                yield return null;
            }
            yield return null;
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++) { Transform r = FindDeep(t.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        [Test]
        public void 표는_글자_92rem_과_세로_패딩_6rem_을_쥐고_글자는_하한_아래라_예외_칸이다()
        {
            Assert.AreEqual(0.92f, TextSizeUi.Rem("dgd_btn"), 1e-6f, "정본 5356 .dgd-btn .92rem");
            Assert.Less(TextSizeUi.Px("dgd_btn"), UiCatalog.Instance.Kind(TextKind.Sub).min, "33.5px 는 하한 36 아래 — Micro + 표(§1 예외 열넷째 자리)");
            Assert.AreEqual(0.6f, DungeonStyle.L("dgd_btn_pad_y_rem"), 1e-6f, "정본 5356 padding .6rem");
            Assert.AreEqual(3.4f, UiKit.L("dgd_btn_h_rem"), 1e-6f, "정본 5355 min-height 3.4rem 은 그대로 최솟값으로 남는다");
            float two = 2f * TextSizeUi.Px("dgd_btn") * 1.25f + 0.6f * Rem * 2f;
            Assert.Greater(two, 3.4f * Rem, "두 줄 내용(3.50rem)이 최솟값 3.4rem 을 넘는다 — 정본 자신의 셈");
        }

        [UnityTest]
        public IEnumerator 던전_상세_버튼_둘은_같은_높이이고_내용이_최솟값을_넘어_높이를_정한다()
        {
            yield return Boot();
            DungeonUiHost H = DungeonUiHost.Instance;
            H.S.BestChapter = 5; H.S.BestStage = 1;
            UiRoot.Instance.TabBar.OnTab("dungeon");
            yield return null;
            DungeonDetailPopup.Open("hammer");
            yield return null; yield return null;
            Assert.IsTrue(DungeonDetailPopup.IsOpen, "던전 상세가 열린다");
            Canvas.ForceUpdateCanvases();
            Transform app = UiRoot.Instance.App;
            RectTransform card = FindDeep(app, "modal-dungeon-detail").Find("card") as RectTransform;
            Assert.IsNotNull(card, "상세 카드");
            RectTransform sweep = FindDeep(card, "sweep") as RectTransform, enter = FindDeep(card, "enter") as RectTransform;
            Assert.IsNotNull(sweep, "소탕 버튼"); Assert.IsNotNull(enter, "입장 버튼");
            float font = TextSizeUi.Px("dgd_btn");
            float want = Mathf.Max(DungeonPopups.RemL("dgd_btn_h_rem"),
                2f * font * (float)LineHeight.Ratio(font, "dgd_btn_lh") + DungeonPopups.Rem(DungeonStyle.L("dgd_btn_pad_y_rem")) * 2f + DungeonPopups.Line3 * 2f);
            Assert.AreEqual(want, sweep.rect.height, 0.5f, "소탕 버튼 높이 = max(3.4rem, 2줄 × .92 × 1.25 + .6 × 2 + 키라인 둘)");
            Assert.AreEqual(sweep.rect.height, enter.rect.height, 0.5f, "두 버튼은 같은 높이(정본 flex: 1 1 0)");
            Assert.Greater(sweep.rect.height, DungeonPopups.RemL("dgd_btn_h_rem") + 0.5f, "두 줄 내용이 최솟값 3.4rem 을 넘는다 — 종전 고정 3.4 가 아니다");
            Assert.AreEqual(sweep.anchoredPosition.y, enter.anchoredPosition.y, 0.5f, "두 버튼은 같은 줄");
            TextMeshProUGUI sl = sweep.Find("label").GetComponent<TextMeshProUGUI>(), el = enter.Find("label").GetComponent<TextMeshProUGUI>();
            Assert.AreEqual(TextKind.Micro, sl.GetComponent<UiTextKindTag>().Kind, "라벨 = Micro 칸(하한 아래 · 표)");
            Assert.AreEqual(font, sl.fontSize, 0.01f, "소탕 라벨 글자 = .92rem");
            Assert.AreEqual(font, el.fontSize, 0.01f, "입장 라벨 글자 = .92rem");
            sl.ForceMeshUpdate();
            Assert.AreEqual(2, sl.textInfo.lineCount, "«이전 스테이지 / 소탕» 은 두 줄(정본 <br>) — 접히거나 잘리지 않는다");
            Assert.GreaterOrEqual(sl.rectTransform.rect.height, 2f * font * (float)LineHeight.Ratio(font, "dgd_btn_lh") - 0.5f, "두 줄 라벨 상자가 두 줄 피치보다 낮지 않다(넘침 없음)");
            Assert.AreEqual(1.25, LineHeight.MeasuredRatio(sl), 0.03, "줄 피치 = 글자 × 1.25(정본 5356)");
            Debug.Log("[T481] 버튼 " + sweep.rect.height.ToString("0.0") + "px(" + (sweep.rect.height / Rem).ToString("0.00") + "rem · " + (sweep.rect.height / UiKit.RefH * 100f).ToString("0.00") + "%H) · 글자 " + font.ToString("0.0") + "px · 카드 " + card.rect.height.ToString("0.0"));
            DungeonDetailPopup.Close();
            yield return null;
        }
    }
}
