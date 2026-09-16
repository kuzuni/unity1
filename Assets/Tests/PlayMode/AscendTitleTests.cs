using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T398 — 승천 팝업 제목은 정본 `ui.js` 5864 대로 **두 토막**이다: 굵은 잉크 «승천»(h3 1.15rem) + 작고 흐린 «보유 별 합계 ⭐ N»(.muted .78rem · #78909c · 400)
    /// · 별 아이콘 앞뒤로 둘 · 원작에 없는 가운뎃점 «·» 0.
    /// </summary>
    public class AscendTitleTests
    {
        static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && DungeonUiHost.Ready && UiRoot.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready && MetaHost.Ready && DungeonUiHost.Ready, "호스트 셋이 20초 안에 준비되지 않았다");
            yield return null;
        }

        static Transform Find(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        [UnityTest]
        public IEnumerator 승천_제목은_큰_승천과_작고_흐린_보유_별_합계_두_토막이고_별이_둘이다()
        {
            yield return Boot();
            AscendPopup.Open();
            yield return null;
            Assert.IsTrue(AscendPopup.IsOpen, "승천 팝업");
            Transform root = AscendPopup.Root;
            Assert.IsNotNull(root);
            TextMeshProUGUI big = Find(root, "title").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI small = Find(root, "title-small").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI smallN = Find(root, "title-small-n").GetComponent<TextMeshProUGUI>();
            Assert.IsNotNull(big, "큰 토막 «승천»"); Assert.IsNotNull(small, "작은 토막 «보유 별 합계»"); Assert.IsNotNull(smallN, "작은 토막 «N»");
            Assert.AreEqual("승천", big.text.Trim(), "큰 토막은 «승천» 뿐");
            StringAssert.Contains("보유 별 합계", small.text);
            Assert.AreEqual("0", smallN.text.Trim(), "새 세이브 · 보유 별 합계 0");

            // 크기: 작은 / 큰 = .78 / 1.15 (표 AscendUi.json) ± 3%
            float want = 0.78f / 1.15f;
            Assert.AreEqual(want, AscendUi.TitleSmallRatio(), want * 0.03f, "표의 비율 = 정본 .78rem / 1.15rem");
            Assert.AreEqual(want, small.fontSize / big.fontSize, want * 0.03f, "작은 토막 / 큰 토막 = .78/1.15");
            Assert.AreEqual(small.fontSize, smallN.fontSize, 1e-3f, "숫자도 같은 작은 크기");
            Assert.GreaterOrEqual(small.fontSize, UiCatalog.Instance.Kind(TextKind.Micro).min, "Micro 하한 위");

            // 색·굵기: 작은 쪽은 표의 muted(#78909c) · 400 — 큰 쪽은 잉크 · 굵게
            Color muted = UiKit.C("muted2");
            Assert.AreEqual(muted.r, small.color.r, 0.01f); Assert.AreEqual(muted.g, small.color.g, 0.01f); Assert.AreEqual(muted.b, small.color.b, 0.01f);
            Assert.AreEqual(0, (int)(small.fontStyle & FontStyles.Bold), "작은 토막은 굵지 않다(정본 .muted 400)");
            Assert.AreNotEqual(0, (int)(big.fontStyle & FontStyles.Bold), "큰 토막은 굵다");

            // 별 둘: 제목 앞 «star» · 합계 앞 «star-2»(작은 글자 크기)
            Image s1 = Find(root, "star").GetComponent<Image>(), s2 = Find(root, "star-2").GetComponent<Image>();
            Assert.IsNotNull(s1, "앞 별"); Assert.IsNotNull(s2, "합계 앞 별");
            Assert.IsNotNull(s1.sprite, "앞 별 그림"); Assert.IsNotNull(s2.sprite, "합계 앞 별 그림");
            LayoutElement le = s2.GetComponent<LayoutElement>();
            Assert.IsNotNull(le); Assert.AreEqual(small.fontSize, le.preferredWidth, 1e-3f, "합계 앞 별은 작은 글자 크기");
            Canvas.ForceUpdateCanvases();
            Assert.Less(big.rectTransform.position.x, small.rectTransform.position.x, "«승천» 이 왼쪽 · «보유 별 합계» 가 오른쪽(한 줄)");
            Assert.Less(s2.rectTransform.position.x, smallN.rectTransform.position.x, "합계 앞 별이 숫자 왼쪽");

            // 원작에 없는 가운뎃점 0
            StringAssert.DoesNotContain("·", AscendPopup.TitleText);
            StringAssert.DoesNotContain("·", big.text + small.text + smallN.text);
            StringAssert.Contains("보유 별 합계 0", AscendPopup.TitleText, "자가 읽는 글자값은 그대로(DungeonUiTests)");
            AscendPopup.Close();
            yield return null;
        }

        static Rect World(RectTransform rt)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y);
        }

        /// <summary>T420 — 정본 5619 `.asc-card { text-align: center }`: 제목 줄(h3)도 카드 가운데다. 앞 별은 줄 안 첫 자식이고
        /// 제목 잉크(앞 별 왼끝 ~ 숫자 오른끝)의 중심이 카드 중심과 같다(원작 +0.12%W · 종전 클론 −17.9%W).</summary>
        [UnityTest]
        public IEnumerator 승천_제목_줄은_카드_가운데에_선다_앞_별도_줄_안이다()
        {
            yield return Boot();
            AscendPopup.Open();
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Assert.IsTrue(AscendPopup.IsOpen, "승천 팝업");
            Transform root = AscendPopup.Root;
            RectTransform card = (RectTransform)Find(root, "card");
            RectTransform row = (RectTransform)Find(root, "title-row");
            Transform star = Find(root, "star");
            RectTransform smallN = (RectTransform)Find(root, "title-small-n");
            Assert.IsNotNull(card, "카드"); Assert.IsNotNull(row, "제목 줄"); Assert.IsNotNull(star, "앞 별"); Assert.IsNotNull(smallN, "합계 숫자");

            HorizontalLayoutGroup lay = row.GetComponent<HorizontalLayoutGroup>();
            Assert.IsNotNull(lay, "제목 줄은 가로 레이아웃");
            Assert.AreEqual(TextAnchor.MiddleCenter, lay.childAlignment, "정본 5619 .asc-card text-align: center — 줄 안의 것들이 가운데로 모인다");
            Assert.AreEqual(row, star.parent, "앞 별은 카드 여백이 아니라 제목 줄 안이다(정본 `${star} 승천`)");
            Assert.AreEqual(0, star.GetSiblingIndex(), "앞 별이 줄의 첫 자식");

            Rect rc = World(card), rs = World((RectTransform)star), rn = World(smallN), rr = World(row);
            Assert.Greater(rc.width, 0f, "카드 폭");
            float inkCenter = (rs.xMin + rn.xMax) * 0.5f;
            float off = (inkCenter - rc.center.x) / rc.width;
            Assert.AreEqual(0f, off, 0.01f, "제목 잉크 중심 ↔ 카드 중심 · 실측 " + (off * 100f).ToString("0.00") + "%W (원작 +0.12%W · 종전 클론 −17.9%W)");
            Assert.AreEqual(rc.center.x, rr.center.x, rc.width * 0.01f, "제목 줄 상자도 카드 가운데(카드 안쪽 폭 전체)");
            Assert.Greater(rs.xMin, rc.xMin, "앞 별이 카드 안에 있다");
            Assert.Less(rn.xMax, rc.xMax, "숫자가 카드 안에 있다");

            // 한 카드 안의 두 줄이 같은 규칙으로 선다 — 안내문 중심도 카드 중심
            TextMeshProUGUI guide = Find(root, "guide").GetComponent<TextMeshProUGUI>();
            Assert.IsNotNull(guide, "안내문");
            Rect rg = World(guide.rectTransform);
            Assert.AreEqual(rc.center.x, rg.center.x, rc.width * 0.01f, "안내문 상자도 카드 가운데");
            AscendPopup.Close();
            yield return null;
        }
    }
}
