using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Save;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T133 — 메인 화면 오프라인 보상 버튼(정본 `index.html` 75 `#offline-btn`)이 **실제로 서고** 정본대로 움직이는가.
    /// 자기 파일인 이유: `UiSmokeTests.cs`·`OfflinePopupTests.cs` 는 다른 작업이 쥐고 있어(check_claim_scope) 뒤 번호가 비켜 간다.
    /// «오브젝트가 섰는가» 만 보지 않는다 — 상자 스프라이트가 **실제로 걸렸는가** · 졸음 글자가 **칠해지는가**(α)를 단언한다(T56 에서 배운 자리).
    /// </summary>
    public class OfflineButtonTests
    {
        private static void DeleteSave()
        {
            try
            {
                string p = System.IO.Path.Combine(Application.persistentDataPath,
                    (SaveIo.Defs != null ? SaveIo.Defs.SaveKey : "forgeclone_save") + ".json");
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
            while (!(MetaHost.Ready && OfflineButton.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 준비되지 않았다");
            Assert.IsNotNull(OfflineButton.Instance, "오프라인 보상 버튼이 서지 않았다 (정본 index.html 75 · UiRoot 가 Ensure 를 부른다)");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 버튼이_왼쪽_아래에_상자_그림으로_선다()
        {
            yield return Boot();

            OfflineButton ob = OfflineButton.Instance;
            OfflineButtonSpec s = OfflineButton.Spec;
            float rem = PopupKit.Rem;

            RectTransform rt = (RectTransform)ob.Btn.transform;
            Assert.AreEqual((float)s.BtnRem * rem, rt.rect.width, 1f, "정본 #offline-btn width 2.9rem");
            Assert.AreEqual((float)s.BtnRem * rem, rt.rect.height, 1f, "정본 #offline-btn height 2.9rem");
            Assert.AreEqual(Vector2.zero, rt.anchorMin, "정본 bottom/left — 왼쪽 아래 모서리 기준이다");
            Assert.AreEqual((float)s.LeftRem * rem, rt.anchoredPosition.x, 1f, "정본 left .5rem");
            Assert.AreEqual((float)s.BottomRem * rem, rt.anchoredPosition.y, 1f, "정본 bottom .6rem");

            // 상자 그림이 **실제로 걸렸는가** — 빈 칸이면 정본 paintStaticIcons 와 다르다
            Image ico = ob.Chest.GetComponentInChildren<Image>(true);
            Assert.IsNotNull(ico, "상자 칸에 그림이 없다");
            Assert.IsNotNull(ico.sprite, "상자 스프라이트가 안 걸렸다 (정본 IconGen.img('chest') · T31 아틀라스)");
            Assert.AreSame(UiIcons.Get(OfflineButtonStyle.T("chest_icon")), ico.sprite, "정본과 같은 chest 아이콘이어야 한다");

            // 졸음 글자 셋(정본 .ob-zzz i × 3) — 글자가 실제로 칠해지는가
            Assert.AreEqual(s.ZzzCount, ob.Zzz.Count, "정본은 z 가 셋이다");
            foreach (TextMeshProUGUI z in ob.Zzz)
            {
                Assert.AreEqual(OfflineButtonStyle.T("zzz_letter"), z.text, "정본 글자는 «z» 다");
                z.ForceMeshUpdate();
                Assert.Greater(z.textInfo.characterCount, 0, "졸음 글자가 한 자도 안 그려진다 (상자만 뜬다)");
            }

            // 층은 HUD 아래다(정본 z4 < 상단바 z5) — 위에 있으면 상단바를 가린다
            Transform layer = ob.transform;
            Assert.Less(layer.GetSiblingIndex(), UiRoot.Instance.HudLayer.GetSiblingIndex() + 1,
                        "정본 #offline-btn 은 z4 라 상단바(z5) 아래다");
        }

        [UnityTest]
        public IEnumerator 보상이_쌓이면_들썩이고_방금_수령했으면_가만히_있다()
        {
            yield return Boot();

            OfflineButton ob = OfflineButton.Instance;
            SaveState st = SaveIo.State;

            // 방금 수령한 상태 — 정본 ui.js 5944 가 ready 를 끈다
            st.LastOfflineClaim = SaveIo.NowMs();
            ob.Refresh();
            Assert.IsFalse(ob.IsReady, "방금 수령했는데 상자가 들썩인다");
            yield return null;
            Assert.AreEqual(Vector3.one, ob.Chest.localScale, "안 들썩일 때는 제자리·제크기여야 한다");

            // 충분히 묵힌 상태 — 정본 6085 (now − lastOfflineClaim)/1000 ≥ 60
            st.LastOfflineClaim = SaveIo.NowMs() - (OfflineButton.Spec.ReadySec + 5) * 1000.0;
            ob.Refresh();
            Assert.IsTrue(ob.IsReady, "60초가 지났는데 상자가 안 들썩인다 (정본 ui.js 6085)");

            // 한 바퀴(1.6초) 도는 동안 크기가 1 을 넘는 순간이 있어야 한다(정본 50% scale 1.04)
            bool grew = false;
            float t = 0f;
            while (t < 2.0f && !grew)
            {
                t += Time.unscaledDeltaTime;
                if (ob.Chest.localScale.x > 1.001f) grew = true;
                yield return null;
            }
            Assert.IsTrue(grew, "들썩임(ob-bob)이 실제로 안 돈다 — 크기가 한 번도 1을 안 넘었다");
        }

        [UnityTest]
        public IEnumerator 누적이_없으면_토스트만_있으면_팝업을_연다()
        {
            yield return Boot();

            OfflineButton ob = OfflineButton.Instance;
            SaveState st = SaveIo.State;
            MetaHost h = MetaHost.Instance;

            // 부팅 때 뜬 팝업이 있으면 닫고 시작한다
            if (h.Popups.IsOpen(OfflinePopup.Name)) { OfflinePopup.Close(h); yield return null; }

            // ⓐ 누적 0 — 정본 5930: 토스트만, 팝업은 안 연다
            st.LastOfflineClaim = SaveIo.NowMs();
            ob.Btn.onClick.Invoke();
            yield return null;
            Assert.AreEqual("empty", ob.LastTap, "누적이 없으면 정본은 토스트만 띄운다");
            Assert.IsFalse(h.Popups.IsOpen(OfflinePopup.Name), "누적이 없는데 팝업이 열렸다");

            // ⓑ 누적 있음 — 팝업이 열린다(지급은 팝업의 [수집] 이 한다)
            st.LastOfflineClaim = SaveIo.NowMs() - 3600.0 * 1000.0;
            ob.Btn.onClick.Invoke();
            yield return null;
            Assert.AreEqual("open", ob.LastTap, "누적이 있으면 정본은 팝업을 연다 (정본 showOffline)");
            Assert.IsTrue(h.Popups.IsOpen(OfflinePopup.Name), "오프라인 팝업이 안 열렸다");
            OfflinePopup.Close(h);
            yield return null;
        }
    }
}
