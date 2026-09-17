using System.Collections;
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
    /// T456 — 리그 «상대 선택» 팝업 제목은 정본 `.profile-title`(style.css 2992 · ui.js 4868) 대로 **`--pp-ink`**(흰 카드 위 진한 잉크)다.
    /// 전엔 `stage_ink`(#ffffff) 라 흰 카드 위 흰 글자였다(런 1102 실측: 제목 띠의 가장 어두운 화소 189 · 원작 0).
    /// 같은 팝업의 티켓 알약 글자는 어두운 알약(`pp_ink` 바탕) 위라 **흰색 그대로**여야 한다 — 이 고침이 색 한 자리만 바꿨다는 증거로 같이 본다.
    /// </summary>
    public class LeagueChallengeTitleInkTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다");
            yield return null;
        }

        static Transform Find(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++) { Transform r = Find(t.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        [UnityTest]
        public IEnumerator 상대_선택_제목은_흰_카드_위_pp_ink_이고_티켓_알약_글자는_흰색_그대로다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            LeagueSheet.OpenChallenge(h);
            yield return null;
            Popup p = PopupLayer.Instance.Find(LeagueSheet.ChallengeName);
            Assert.IsNotNull(p, "도전 상대 선택 팝업");
            Transform card = Find(p.Root, "card");
            Assert.IsNotNull(card, "카드");
            Transform titleT = card.Find("title");
            Assert.IsNotNull(titleT, "제목(title)");
            TextMeshProUGUI title = titleT.GetComponent<TextMeshProUGUI>();
            Assert.IsNotNull(title, "제목은 글자 하나다");
            Assert.AreEqual("상대 선택", title.text, "제목 문구(정본 ui.js 4868)");
            Color ink = UiKit.C("pp_ink"), paper = UiKit.C("pp_paper");
            Assert.AreEqual(ink, title.color, "제목 잉크 = pp_ink(정본 2992 .profile-title { color: var(--pp-ink) }) — 전엔 stage_ink 흰색");
            Assert.AreNotEqual(UiKit.C("stage_ink"), title.color, "흰 카드 위 흰 글자가 아니다");
            Assert.Greater(Mathf.Abs(paper.grayscale - title.color.grayscale), 0.5f, "제목 잉크와 카드 종이의 밝기 차가 크다(읽힌다)");

            // ⓒ 티켓 알약 글자 — 어두운 알약(pp_ink 바탕) 위라 흰색 그대로.
            Transform pill = Find(card, "pill");
            Assert.IsNotNull(pill, "티켓 알약");
            TextMeshProUGUI tk = pill.Find("text").GetComponent<TextMeshProUGUI>();
            Assert.AreEqual(UiKit.C("stage_ink"), tk.color, "티켓 알약 글자는 흰색 그대로(어두운 알약 위) — 이 고침은 제목 한 자리만 바꿨다");
            Debug.Log("[T456] 제목 색 " + title.color + " · 알약 글자 " + tk.color);
            PopupLayer.Instance.Hide(LeagueSheet.ChallengeName);
        }
    }
}
