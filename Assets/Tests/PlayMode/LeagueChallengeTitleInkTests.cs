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

        /// <summary>T462 — 같은 제목의 **크기·마진**: 정본 `.profile-title { font-size: 1.5rem; margin: 0 0 .2rem }`(2991). 전엔 `Title2`(1.15rem 단 · −23%)였고 마진이 없어 카드가 짧았다.
        /// 단은 `ProfilePopup` 이 같은 선택자에 쓰는 `Title`(60 · 1.5rem 의 +10% · check_text_kinds ±12% 안) — 새 단을 더하지 않는다.</summary>
        [UnityTest]
        public IEnumerator 상대_선택_제목은_profile_title_단이고_아래_2rem_마진이_카드에_든다()
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
            UiTextKindTag tag = titleT.GetComponent<UiTextKindTag>();
            Assert.IsNotNull(tag, "UiKit 종류 표식");
            Assert.AreEqual(TextKind.Title, tag.Kind, "정본 .profile-title 1.5rem — ProfilePopup 과 같은 Title 단(전엔 Title2 1.15rem 단)");
            float rem = PopupKit.Rem, want = 1.5f * rem;
            Assert.AreEqual(UiCatalog.Instance.Kind(TextKind.Title).size, title.fontSize, 0.01f, "글자 = 종류표 Title 크기");
            Assert.That(title.fontSize / want, Is.InRange(0.9f, 1.11f), "1.5rem(" + want.ToString("0.0") + "px) 의 ±10% 창 — 지금 " + (title.fontSize / want).ToString("0.000"));
            // 아래 마진 .2rem — 제목 바로 다음 형제가 그 높이의 spacer 다(카드 높이 셈에도 같은 값이 든다).
            int ti = titleT.GetSiblingIndex();
            Assert.Less(ti + 1, card.childCount, "제목 뒤에 형제가 있다");
            Transform next = card.GetChild(ti + 1);
            Assert.AreEqual("spacer", next.name, "제목 바로 아래는 마진 spacer(정본 2991 margin: 0 0 .2rem)");
            LayoutElement le = next.GetComponent<LayoutElement>();
            Assert.IsNotNull(le, "spacer 는 LayoutElement 로 높이를 쥔다");
            float gap = UiKit.L("card_gap_rem") * rem;   // T463 — 정본 1752 .modal-card gap .45rem 이 마진 Spacer 에 접혀 있다
            Assert.AreEqual(0.2f * rem + gap, le.preferredHeight, 0.01f, "spacer 높이 = .2rem 마진 + .45rem 카드 gap(T463)");
            // 카드 높이가 제목 줄(Title × 1.3)과 마진을 담고 있다 — 제목 줄 + 마진 + desc 줄 이 카드 안쪽에 든다.
            RectTransform cardRt = (RectTransform)card;
            LayoutElement tle = titleT.GetComponent<LayoutElement>();
            Assert.IsNotNull(tle, "제목 줄 상자");
            Assert.Greater(cardRt.rect.height, tle.preferredHeight + le.preferredHeight + PopupKit.FontSize(TextKind.Sub) * 1.5f, "카드가 제목 줄 + 마진 + 안내문 줄보다 높다");
            Debug.Log("[T462] 제목 " + title.fontSize.ToString("0.0") + "px(1.5rem = " + want.ToString("0.0") + " · 비 " + (title.fontSize / want).ToString("0.000") + ") · 마진 " + le.preferredHeight.ToString("0.0") + " · 카드 " + cardRt.rect.height.ToString("0.0"));
            PopupLayer.Instance.Hide(LeagueSheet.ChallengeName);
        }

        /// <summary>T463 — 정본 1752 `.modal-card { gap: .45rem }`: 카드의 네 자식(제목·안내문·알약·목록) 사이마다 .45rem. 클론은 그 gap 을
        /// 각 자식의 아래 마진 Spacer(.2 · 1.05 · 1.9rem)에 접어 세운다 — 세 Spacer 가 «마진 + gap» 이고 gap 값은 표 `card_gap_rem`(.45)이다.</summary>
        [UnityTest]
        public IEnumerator 상대_선택_카드의_세_틈은_정본_마진에_카드_gap_45rem_을_더한_값이다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            LeagueSheet.OpenChallenge(h);
            yield return null;
            Popup p = PopupLayer.Instance.Find(LeagueSheet.ChallengeName);
            Assert.IsNotNull(p, "도전 상대 선택 팝업");
            Transform card = Find(p.Root, "card");
            Assert.IsNotNull(card, "카드");
            float rem = PopupKit.Rem, gap = UiKit.L("card_gap_rem") * rem;
            Assert.AreEqual(0.45f * rem, gap, 0.01f, "표 card_gap_rem = 정본 1752 .45rem");
            var spacers = new List<float>();
            for (int i = 0; i < card.childCount; i++)
            {
                Transform ch = card.GetChild(i);
                if (ch.name != "spacer") continue;
                LayoutElement le = ch.GetComponent<LayoutElement>();
                Assert.IsNotNull(le, "spacer 는 LayoutElement 로 높이를 쥔다");
                spacers.Add(le.preferredHeight);
            }
            Assert.AreEqual(3, spacers.Count, "카드 자식 사이 틈 셋(제목 아래 · 안내문 아래 · 알약 아래)");
            Assert.AreEqual(0.2f * rem + gap, spacers[0], 0.01f, "제목 아래 = .2rem(2991) + gap");
            Assert.AreEqual(1.05f * rem + gap, spacers[1], 0.01f, "안내문 아래 = 1.05rem(2587) + gap");
            Assert.AreEqual(1.9f * rem + gap, spacers[2], 0.01f, "알약 아래 = 1.9rem(2593) + gap");
            VerticalLayoutGroup col = card.GetComponent<VerticalLayoutGroup>();
            Assert.IsNotNull(col, "카드 세로 배치");
            Assert.AreEqual(0f, col.spacing, 1e-4f, "Column 틈은 0 — gap 은 Spacer 에 접혀 있다(Column 에 걸면 Spacer 앞뒤로 두 번 든다)");
            Debug.Log("[T463] 틈 셋 " + spacers[0].ToString("0.0") + " · " + spacers[1].ToString("0.0") + " · " + spacers[2].ToString("0.0") + " · gap " + gap.ToString("0.0") + " · 카드 " + ((RectTransform)card).rect.height.ToString("0.0"));
            PopupLayer.Instance.Hide(LeagueSheet.ChallengeName);
        }
    }
}
