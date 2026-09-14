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
    /// T140 — 설정 목록의 행 높이. 정본 `style.css` 3098~3103 은 행을 **4.75%H**(원본 42px)로 잡고
    /// 목록 상자를 45.6%H 로 잘라 «열 줄 + 열한째 줄 반» 이 보이게 둔다(그 잘린 행이 «더 있다» 는 단서다).
    /// 클론은 카탈로그의 그 값(`settings_row_h`)에 1.25 를 한 번 더 곱해 행이 부풀었고, 같은 상자에
    /// **여덟 줄**도 못 들어 «차단 목록»·«개인정보 보호» 가 첫 화면에서 사라졌다(검수 Q · 런 284 실측).
    /// 그림이 아니라 **계층과 수**로 본다 — 촬영이 없는 런에서도 도는 단언이다.
    /// </summary>
    public class SettingsRowTests
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
            while (t < 30f && !(MetaHost.Ready && PopupLayer.Instance != null))
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 30초 안에 서지 않았다");
            Assert.IsNotNull(PopupLayer.Instance, "팝업 층이 서지 않았다");
        }

        [UnityTest]
        public IEnumerator 설정_행_높이는_정본_4_75퍼센트H_이고_첫_화면에_열_줄이_든다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            ProfilePopup.Open(h);
            ProfilePopup.SwitchView(h, "settings");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(ProfilePopup.Name);
            Assert.IsNotNull(p, "프로필·설정 팝업이 안 열렸다");

            RectTransform row0 = FindIn(p.Root, "row-0");
            Assert.IsNotNull(row0, "설정 목록 첫 행(row-0)이 없다");
            float want = UiKit.H("settings_row_h");
            Assert.AreEqual(want, row0.rect.height, 0.6f,
                "설정 행 높이가 정본 4.75%H(" + want.ToString("0.0") + ") 가 아니다 — 카탈로그 값에 배수를 곱하지 않는다(T140)");

            RectTransform box = FindIn(p.Root, "list-box");
            Assert.IsNotNull(box, "설정 목록 상자(list-box)가 없다");
            float boxH = box.rect.height;
            Assert.AreEqual(UiKit.L("settings_h") * UiKit.RefH, boxH, 0.6f, "목록 상자는 정본 45.6%H 그대로다");
            // 정본 비 = .456 / .0475 = **9.6** — 원작 샷(`ref/screens/shot-042744.png`)을 세어 보면
            // «진동·음악·사운드 효과·채팅 표시·채팅 다크 모드·클랜 채팅 미리보기·언어·계정·차단 목록» **아홉 줄이 다 들고**
            // 열째(«개인정보 보호»)가 반쯤 잘린다(그 잘린 행이 «더 있다» 는 단서다 · 정본 style.css 3090 주석).
            // ⚠ 1회차에 나는 등재 문구의 «열 줄» 을 그대로 단언으로 옮겼다가 빨강을 냈다(런 299: 876/91 = 9.6 → 9).
            //   수는 남의 문장이 아니라 **정본 값과 원작 샷**에서 온다(T140 2회차 · 결정).
            float ratio = boxH / row0.rect.height;
            Assert.AreEqual(UiKit.L("settings_h") / UiKit.L("settings_row_h"), ratio, 0.1f,
                "상자/행 비가 정본 9.6 이 아니다 — 상자 " + boxH.ToString("0") + " / 행 " + row0.rect.height.ToString("0"));
            Assert.GreaterOrEqual(Mathf.FloorToInt(ratio), 9,
                "목록 상자에 아홉 줄이 안 든다(행이 부풀었다) — 상자 " + boxH.ToString("0") + " / 행 " + row0.rect.height.ToString("0"));

            ProfilePopup.Close(h);
            yield return null;
        }

        private static RectTransform FindIn(Transform root, string name)
        {
            foreach (RectTransform rt in root.GetComponentsInChildren<RectTransform>(true)) if (rt.name == name) return rt;
            return null;
        }
    }
}
