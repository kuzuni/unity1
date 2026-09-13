using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
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
    /// T109 7회차 — 공용 <see cref="PopupKit.Btn"/> 의 라벨 키라인이 정본대로 걸리는가.
    /// 정본 `style.css` 8719: `.btn.btn.primary/on/equip/danger/sell { -webkit-text-stroke: var(--ol2) var(--pp-line) }`(모달 안 3548~3555 에서
    /// primary·on·equip = 파랑 · danger·sell = 빨강) · 8725: `.disabled { -webkit-text-stroke: 0 }` · 회색 `.btn` 은 규칙이 없다.
    /// 클론은 면 색 키가 그 클래스라 `KeylineUi.json` 의 `btn_face` 표가 «면 키 → 폭표 키» 를 잇는다. 폭의 픽셀 자체는 T104 `OutlineTests` 몫 —
    /// 여기서는 «걸렸는가 · 안 걸릴 면·비활성은 안 걸렸는가 · 호출부 키가 표를 이기는가 · 실물(리그 시트 발 «도전»)에도 걸리는가» 를 본다.
    /// </summary>
    public class PopupBtnKeylineTests
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
            while (!(MetaHost.Ready && PopupLayer.Instance != null && UiRoot.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다");
            Assert.IsNotNull(PopupLayer.Instance, "팝업 층이 서지 않았다");
            Assert.IsNotNull(UiRoot.Instance, "UiRoot 가 서지 않았다");
            yield return null;
        }

        private static TextMeshProUGUI Label(Button b)
        {
            Transform t = b.transform.Find("label");
            Assert.IsNotNull(t, b.name + ": 라벨이 없다");
            return t.GetComponent<TextMeshProUGUI>();
        }

        [UnityTest]
        public IEnumerator 색_버튼_셋은_키라인이_걸리고_회색_비활성_끈_것은_민글자다()
        {
            yield return Boot();
            RectTransform host = UiKit.Box(UiRoot.Instance.App, "t109-btn-host");
            try
            {
                float w = UiKit.L("league_challenge_w") * UiRoot.Instance.App.rect.width, h = UiKit.H("btn_h");
                Color ink = UiKit.C("stage_ink"), line = UiKit.C("pp_line");
                string[] faces = { "pp_blue", "pp_green", "pp_red" };
                string[] lips = { "pp_blue_dk", "pp_green_dk", "pp_red_dk" };
                float colored = -1f;
                for (int i = 0; i < faces.Length; i++)
                {
                    TextMeshProUGUI t = Label(PopupKit.Btn(host, "b-" + faces[i], "확인", faces[i], lips[i], null, w, h));
                    Assert.Greater(t.outlineWidth, 0f, faces[i] + ": 정본 8719 var(--ol2) 키라인이 걸려야 한다");
                    Color oc = t.outlineColor;   // TMP 의 outlineColor 는 Color32 — 형이 달라 AreEqual 이 진다(런 262) · 채널로 잰다
                    Assert.AreEqual(line.r, oc.r, 2f / 255f, faces[i] + ": 키라인 색 R = var(--pp-line)");
                    Assert.AreEqual(line.g, oc.g, 2f / 255f, faces[i] + ": 키라인 색 G = var(--pp-line)");
                    Assert.AreEqual(line.b, oc.b, 2f / 255f, faces[i] + ": 키라인 색 B = var(--pp-line)");
                    Assert.AreEqual(ink, t.color, faces[i] + ": 채움(글자색)은 그대로(정본 color #fff)");
                    Assert.IsTrue(t.fontMaterial.IsKeywordEnabled("OUTLINE_ON"), faces[i] + ": 재질 OUTLINE_ON");
                    if (colored < 0f) colored = t.outlineWidth;
                    else Assert.AreEqual(colored, t.outlineWidth, 1e-5f, faces[i] + ": 색 버튼 셋은 같은 2px");
                }

                TextMeshProUGUI gray = Label(PopupKit.Btn(host, "b-gray", "취소", "pp_gray", "pp_gray_dk", null, w, h, "pp_ink"));
                Assert.AreEqual(0f, gray.outlineWidth, 1e-6f, "회색 .btn 은 정본에 키라인 규칙이 없다 — 민글자");
                TextMeshProUGUI dbg = Label(PopupKit.Btn(host, "b-dbg", "이동", "card_bg", "pp_line", null, w, h, "ink"));
                Assert.AreEqual(0f, dbg.outlineWidth, 1e-6f, "표에 없는 면(디버그 card_bg)은 민글자");
                TextMeshProUGUI off = Label(PopupKit.Btn(host, "b-off", "수령", "pp_green", "pp_green_dk", null, w, h, "stage_ink", TextKind.Button, true));
                Assert.AreEqual(0f, off.outlineWidth, 1e-6f, "비활성은 정본 8725 -webkit-text-stroke: 0");
                TextMeshProUGUI none = Label(PopupKit.Btn(host, "b-none", "3", "pp_blue", "pp_blue_dk", null, w, h, "stage_ink", TextKind.Button, false, ""));
                Assert.AreEqual(0f, none.outlineWidth, 1e-6f, "keylineKey \"\" 는 끈다(정본 .btn 이 아닌 파란 칸 — 오토포지 배치 선택)");
                TextMeshProUGUI four = Label(PopupKit.Btn(host, "b-four", "시작", "pp_blue", "pp_blue_dk", null, w, h, "stage_ink", TextKind.Button, false, "af_start"));
                Assert.Greater(four.outlineWidth, colored, "호출부 키(af_start 4px)가 표의 2px 를 이긴다");
                Assert.AreEqual(KeylineUi.Px("btn_colored") * 2f, KeylineUi.Px("af_start"), 1e-4f, "표: af_start 4px = btn_colored 2px 의 두 배(정본 5015 · 8719)");
            }
            finally
            {
                Object.Destroy(host.gameObject);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator 리그_시트_발의_도전_버튼에는_키라인이_걸리고_행의_은색_도전_버튼은_민글자다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            h.OpenLeague();
            yield return null;
            Assert.IsTrue(PopupLayer.Instance.IsOpen(LeagueSheet.Name), "리그 시트");
            Popup p = PopupLayer.Instance.Find(LeagueSheet.Name);
            Assert.IsNotNull(p);
            int foot = 0;
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.name != "label" || t.transform.parent == null || t.transform.parent.name != "challenge") continue;
                foot++;
                Assert.Greater(t.outlineWidth, 0f, "발의 «도전»(pp_green = 정본 .btn.primary)에 var(--ol2) 키라인");
            }
            Assert.AreEqual(1, foot, "리그 시트 발의 «도전» 버튼 하나");

            // 행의 «도전» 은 리그 시트가 아니라 **도전 팝업**(league-challenge · `RenderChallenge` 의 slot/row) 안이다 — 런 262 에서 0개로 잡혔다
            LeagueSheet.OpenChallenge(h);
            yield return null;
            Assert.IsTrue(PopupLayer.Instance.IsOpen(LeagueSheet.ChallengeName), "도전 팝업");
            Popup c = PopupLayer.Instance.Find(LeagueSheet.ChallengeName);
            int rows = 0;
            foreach (TextMeshProUGUI t in c.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.name != "label" || t.transform.parent == null || t.transform.parent.name != "challenge") continue;
                rows++;
                Assert.AreEqual(0f, t.outlineWidth, 1e-6f, "행의 «도전»(challenge_btn 은색 · 정본 .btn.sm 에 규칙 없음)은 민글자");
            }
            Assert.Greater(rows, 0, "도전 팝업의 행 «도전» 버튼이 하나 이상");
            h.Popups.Hide(LeagueSheet.ChallengeName);
            LeagueSheet.Close(h);
            yield return null;
        }
    }
}
