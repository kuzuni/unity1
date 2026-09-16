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
    /// T426 — 던전 상세 **스테이지 줄**의 정본 세 값(`css/style.css` 5304 `.dgd-stage-row { margin: .3rem 0 .5rem }` · 5308 `.dgd-stage { line-height: 1.15 }`).
    /// 종전엔 셋 다 없었는데 오차가 서로 지워 아래 줄들만 맞았다(T28 89회차) — 여기서는 줄 **자체**를 잰다:
    /// 줄 위끝 = 배너 아래 + (.6 + .3)rem · 라벨·숫자 상자 = 글자 크기 × 1.15 · 알약 위끝 = 줄 아래 + .5rem. 값은 곁 표 `DungeonUi.json`(결정 731)에서 온다.
    /// </summary>
    public class DungeonStageRowTests
    {
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
            foreach (Transform c in t.GetComponentsInChildren<Transform>(true)) if (c.name == name) return c;
            return null;
        }

        [UnityTest]
        public IEnumerator 던전_상세_스테이지_줄은_정본_세_값_위_3rem_줄높이_1_15_아래_5rem_으로_선다()
        {
            // 표 ↔ 정본 왕복
            Assert.AreEqual(0.3f, DungeonStyle.L("dgd_stage_mt_rem"), 1e-6f, "정본 5304 margin-top .3rem");
            Assert.AreEqual(0.5f, DungeonStyle.L("dgd_stage_mb_rem"), 1e-6f, "정본 5304 margin-bottom .5rem");
            Assert.AreEqual(1.15f, DungeonStyle.L("dgd_stage_lh"), 1e-6f, "정본 5308 line-height 1.15");

            yield return Boot();
            DungeonDetailPopup.Open("hammer");
            yield return null;
            yield return null;
            RectTransform app = UiRoot.Instance.App;
            Transform overlay = FindDeep(app, "modal-dungeon-detail");
            Assert.IsNotNull(overlay, "던전 상세 팝업");
            RectTransform card = overlay.Find("card") as RectTransform;
            Assert.IsNotNull(card, "카드");
            RectTransform hero = card.Find("hero") as RectTransform;
            RectTransform lab = FindDeep(card, "stage-label") as RectTransform;
            RectTransform num = FindDeep(card, "stage-num") as RectTransform;
            RectTransform pill = card.Find("reward-pill") as RectTransform;
            Assert.IsNotNull(hero, "배너"); Assert.IsNotNull(lab, "「난이도」"); Assert.IsNotNull(num, "스테이지 수"); Assert.IsNotNull(pill, "보상 알약");

            // UiKit.Place 는 좌상단 앵커 · anchoredPosition.y = −위끝(기준 px)
            float heroBottom = -hero.anchoredPosition.y + hero.rect.height;
            float labTop = -lab.anchoredPosition.y, labBottom = labTop + lab.rect.height;
            float numTop = -num.anchoredPosition.y, numBottom = numTop + num.rect.height;
            float pillTop = -pill.anchoredPosition.y;
            float rem = DungeonPopups.Rem(1f);
            float lh = DungeonStyle.L("dgd_stage_lh");

            Assert.AreEqual(DungeonPopups.RemL("dgd_hero_mb_rem") + DungeonPopups.Rem(DungeonStyle.L("dgd_stage_mt_rem")), labTop - heroBottom, 0.5f,
                "줄 위끝 = 배너 아래 + (.6 + .3)rem — 위 마진은 배너 마진과 안 합쳐진다(flex column) · 실측 " + (labTop - heroBottom) / rem + "rem");
            Assert.AreEqual(DungeonPopups.Kind(TextKind.Sub) * lh, lab.rect.height, 0.1f, "「난이도」 상자 = Sub 글자 × 1.15(바탕 1.25 가 아니다)");
            Assert.AreEqual(DungeonPopups.Kind(TextKind.Title2) * lh, num.rect.height, 0.1f, "스테이지 수 상자 = Title2 글자 × 1.15");
            Assert.AreEqual(labBottom, numTop, 0.1f, "두 줄은 붙어 있다(정본 .dgd-stage span { display: block })");
            Assert.AreEqual(DungeonPopups.Rem(DungeonStyle.L("dgd_stage_mb_rem")), pillTop - numBottom, 0.5f,
                "알약 위끝 = 줄 아래 + .5rem(공용 card_gap .45 가 아니다) · 실측 " + (pillTop - numBottom) / rem + "rem");
            Assert.Less(lab.rect.height + num.rect.height, DungeonPopups.LineH(TextKind.Sub) + DungeonPopups.LineH(TextKind.Title2) - 0.1f,
                "줄 상자가 종전(1.25 배)보다 낮다");
            Debug.Log("[T426] 배너 아래→줄 " + ((labTop - heroBottom) / rem).ToString("0.000") + "rem · 줄 상자 " + (lab.rect.height + num.rect.height).ToString("0.0") + "px(기준) · 줄→알약 " + ((pillTop - numBottom) / rem).ToString("0.000") + "rem");
            DungeonDetailPopup.Close();
            yield return null;
        }
    }
}
