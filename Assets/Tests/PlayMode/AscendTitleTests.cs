using System.Collections;
using System.Collections.Generic;
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

        /// <summary>T446 1회차 — 승천 줄(정본 5620 `.asc-row`)의 아이콘은 `.ico` 기본 **1.45em**(7194)이고 줄 상자는 «글자 줄 ↔ 1.45em» 중 큰 쪽이다.
        /// 종전엔 아이콘이 `subH * 0.9`(= 1.125em)라 22% 작았고 줄 상자도 글자 줄만 셌다. 디센더 몫은 **안 더한다** — 같은 규칙의 위아래 `margin: -.32em` 이
        /// 1.45em 을 0.81em 짜리 margin box 로 줄여 세로로는 거의 안 민다(T433 의 던전 알약과 갈리는 자리 · 표 `AscendUi.json` `_ico_em`).</summary>
        [UnityTest]
        public IEnumerator 승천_줄의_아이콘은_정본_1_45em_이고_줄_상자가_그것을_센다()
        {
            Assert.AreEqual(1.45f, AscendUi.Num("ico_em"), 1e-6f, "정본 7194 `.ico` 1.45em");
            yield return Boot();
            AscendPopup.Open();
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            RectTransform card = null;
            foreach (RectTransform rt in UiRoot.Instance.App.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "card" && rt.parent != null && rt.parent.name == "modal-ascend") { card = rt; break; }
            Assert.IsNotNull(card, "승천 카드");
            RectTransform row = null, ico = null;
            foreach (RectTransform rt in card.GetComponentsInChildren<RectTransform>(true))
                if (rt.name.StartsWith("row-", System.StringComparison.Ordinal)) { row = rt; break; }
            Assert.IsNotNull(row, "승천 줄(row-*)");
            foreach (RectTransform rt in row.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "ico") { ico = rt; break; }
            Assert.IsNotNull(ico, "줄 아이콘");

            // T464 — em 의 기준은 줄 글자 = 정본 5620 `.asc-row` .82rem(표 TextSizeUi `asc_row`) · 종전 하한 `Sub`(36)가 아니다
            float fs = TextSizeUi.Px("asc_row"), em = AscendUi.Num("ico_em");
            TextMeshProUGUI nameT = Find(row, "name").GetComponent<TextMeshProUGUI>();
            Assert.AreEqual(fs, nameT.fontSize, 0.01f, "줄 글자 = 표 .82rem");
            Assert.AreEqual(fs * em, ico.rect.width, 0.5f, "아이콘 = 글자 × 1.45(종전 0.9 = 1.125em 이 아니다)");
            Assert.AreEqual(ico.rect.width, ico.rect.height, 0.01f, "정사각");
            float padY = DungeonPopups.RemL("asc_row_pad_y_rem");
            float want = Mathf.Max(fs * 1.25f, fs * em) + padY * 2f;
            Assert.AreEqual(want, row.rect.height, 0.5f, "줄 상자 = max(글자 줄, 1.45em) + 세로 패딩 × 2 · 실측 " + row.rect.height.ToString("0.0"));
            Assert.Greater(row.rect.height, fs * 1.25f + padY * 2f + 0.5f, "글자 줄만 세던 옛 값보다 크다");
            Debug.Log("[T446] 줄 상자 " + row.rect.height.ToString("0.0") + "px · 아이콘 " + ico.rect.width.ToString("0.0") + "px(= " + (ico.rect.width / fs).ToString("0.00") + "em)");
            AscendPopup.Close();
            yield return null;
        }
        /// <summary>T464 ⓐ — 승천 카드의 안내 문단(정본 657 `.muted` .78rem)과 줄 글자(5620 `.asc-row` .82rem)는 §1 예외 칸 `Micro` + 표 `TextSizeUi.json` 크기다 —
        /// 둘 다 하한 `Sub`(36)로 찍혀 +27%·+21% 였다(런 1143 · 같은 카드 안 제목 `<small class=muted>` 와의 비 1.20 ↔ 원작 1.05). 안내 상자는 «크기 × 1.25 × 두 줄» 이고 문단은 여전히 두 줄이다.</summary>
        [UnityTest]
        public IEnumerator 승천_안내_문단과_줄_글자는_정본_78_82rem_이고_안내는_두_줄이다()
        {
            Assert.AreEqual(0.78f, TextSizeUi.Rem("asc_guide"), 1e-6f, "정본 657 .muted .78rem");
            Assert.AreEqual(0.82f, TextSizeUi.Rem("asc_row"), 1e-6f, "정본 5620 .asc-row .82rem");
            Assert.AreEqual(0.7f, TextSizeUi.Rem("asc_row_arrow"), 1e-6f, "정본 5624 .asc-row.ready::after .7rem");
            Assert.Less(TextSizeUi.Px("asc_row"), UiCatalog.Instance.Kind(TextKind.Sub).min, "표값이 하한 Sub 아래라서 예외 칸이 필요한 자리다");
            yield return Boot();
            AscendPopup.Open();
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Transform root = AscendPopup.Root;
            TextMeshProUGUI guide = Find(root, "guide").GetComponent<TextMeshProUGUI>();
            Assert.IsNotNull(guide, "안내 문단");
            Assert.AreEqual(TextKind.Micro, guide.GetComponent<UiTextKindTag>().Kind, "안내 문단은 예외 칸 Micro(§1 아홉째 자리)");
            float gpx = TextSizeUi.Px("asc_guide");
            Assert.AreEqual(gpx, guide.fontSize, 0.01f, "안내 글자 = 표 .78rem(하한 36 이 아니다)");
            Assert.AreEqual(gpx * 1.25f * 2f, guide.rectTransform.rect.height, 0.5f, "안내 상자 = 크기 × 1.25 × 두 줄");
            guide.ForceMeshUpdate();
            Assert.AreEqual(2, guide.textInfo.lineCount, "정본 안내 문단은 카드 폭에서 두 줄 — 세 줄로 접히거나 한 줄로 펴지면 실패");

            float rpx = TextSizeUi.Px("asc_row");
            int rows = 0;
            foreach (Transform row in root.GetComponentsInChildren<Transform>(true))
            {
                if (!row.name.StartsWith("row-", System.StringComparison.Ordinal)) continue;
                rows++;
                foreach (string n in new[] { "name", "prog", "cnt" })
                {
                    TextMeshProUGUI t = Find(row, n).GetComponent<TextMeshProUGUI>();
                    Assert.IsNotNull(t, row.name + "/" + n);
                    Assert.AreEqual(TextKind.Micro, t.GetComponent<UiTextKindTag>().Kind, row.name + "/" + n + " 은 예외 칸 Micro(§1 열째 자리)");
                    Assert.AreEqual(rpx, t.fontSize, 0.01f, row.name + "/" + n + " = 표 .82rem");
                }
                Transform ar = Find(row, "arrow");
                if (ar != null)
                {
                    TextMeshProUGUI at = ar.GetComponent<TextMeshProUGUI>();
                    Assert.AreEqual(TextKind.Micro, at.GetComponent<UiTextKindTag>().Kind, "화살은 예외 칸 Micro(§1 열한째 자리)");
                    Assert.AreEqual(TextSizeUi.Px("asc_row_arrow"), at.fontSize, 0.01f, "화살 = 표 .7rem");
                }
            }
            Assert.AreEqual(4, rows, "줄 넷");
            AscendPopup.Close();
            yield return null;
        }

        /// <summary>T464 ⓑⓒ — 정본 1752~1756 `.modal-card { gap: .45rem }` 은 자식 일곱 사이 여섯 자리 전부에 든다: 줄 사이 셋과 마지막 줄 → 버튼 하나가 빠져 있었다(1.80rem).
        /// 줄 피치 = 줄 상자 + 마진 × 2 + gap · 마지막 줄 아래변 → 버튼 위변 = 마진 + gap + `.asc-focus` margin-top(.7rem) · 카드 높이 = 그 합.</summary>
        [UnityTest]
        public IEnumerator 승천_줄_사이_셋과_마지막_줄_뒤에_카드_gap_이_들고_카드_높이가_그_합이다()
        {
            yield return Boot();
            AscendPopup.Open();
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Transform root = AscendPopup.Root;
            RectTransform card = (RectTransform)Find(root, "card");
            Assert.IsNotNull(card, "카드");
            var rows = new List<RectTransform>();
            foreach (Transform t in card.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("row-", System.StringComparison.Ordinal) && t.parent == card) rows.Add((RectTransform)t);
            Assert.AreEqual(4, rows.Count, "줄 넷");
            float gap = DungeonPopups.RemL("card_gap_rem");
            float rowMy = DungeonPopups.RemL("asc_row_my_rem");
            float rowH = rows[0].rect.height;
            float pitch = rowH + rowMy * 2f + gap;
            for (int i = 1; i < rows.Count; i++)
            {
                float d = rows[i - 1].anchoredPosition.y - rows[i].anchoredPosition.y;
                Assert.AreEqual(pitch, d, 0.5f, "줄 " + i + " 피치 = 상자 + 마진 × 2 + gap(.45rem) · 실측 " + d.ToString("0.0") + " ↔ gap 없이 " + (rowH + rowMy * 2f).ToString("0.0"));
            }
            RectTransform close = (RectTransform)Find(card, "close");
            Assert.IsNotNull(close, "[닫기]");
            RectTransform last = rows[rows.Count - 1];
            float lastBottom = -last.anchoredPosition.y + last.rect.height;
            float btnTop = -close.anchoredPosition.y;
            float wantGap = rowMy + gap + DungeonPopups.RemL("asc_focus_mt_rem");
            Assert.AreEqual(wantGap, btnTop - lastBottom, 0.5f, "마지막 줄 → 버튼 = 마진 + gap + .7rem · 실측 " + (btnTop - lastBottom).ToString("0.0"));

            float pad = DungeonPopups.RemL("card_pad_rem");
            float guideH = TextSizeUi.Px("asc_guide") * 1.25f * 2f;
            float rowsH = rows.Count * (rowH + rowMy * 2f) + (rows.Count - 1) * gap;
            float want = pad * 2f + DungeonPopups.LineH(TextKind.Button) + gap + guideH + gap + rowsH + gap + DungeonPopups.RemL("asc_focus_mt_rem") + DungeonPopups.RemL("asc_btn_h_rem");
            Assert.AreEqual(want, card.rect.height, 0.5f, "카드 높이 = 패딩 + 제목 + gap + 안내 두 줄 + gap + 줄 넷(사이 gap 셋) + gap + .7rem + 버튼 · 실측 " + card.rect.height.ToString("0.0"));
            Debug.Log("[T464] 카드 " + card.rect.height.ToString("0.0") + "px = " + (card.rect.height / UiKit.RefH * 100f).ToString("0.00") + "%H · 줄 피치 " + pitch.ToString("0.0") + "px");
            AscendPopup.Close();
            yield return null;
        }

        /// <summary>T457 — 정본 ui.js 5861~5869 `.idet-wrap`: 모달이 세로 가운데 두는 것은 «카드 + 카드 아래로 삐져나온 ✕» 덩어리다. 승천엔 다른 여덟 모달의
        /// top 보정값이 없어 카드가 ✕ 삐져나온 몫의 **절반만큼 위**에 선다(원작 카드 가운데 48.36%H ↔ 카드만 가운데 둔 종전 클론 49.95 · 런 1107 실측).</summary>
        [UnityTest]
        public IEnumerator 승천_카드는_카드와_삐져나온_X_덩어리가_모달_세로_가운데다()
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
            Assert.IsNotNull(card, "카드");
            RectTransform x = (RectTransform)Find(card, "x-btn");
            Assert.IsNotNull(x, "✕");
            RectTransform overlay = card.parent as RectTransform;
            Assert.IsNotNull(overlay, "모달 겹");
            // T465 — 카드 rect 는 이제 패딩 상자(테가 위·아래로 line3 만큼 밖) · 정본이 가운데 두는 «카드» 는 테까지의 몸(border-box)이라 테 상자(bg)로 잰다
            RectTransform body = (RectTransform)card.Find("bg");
            Assert.IsNotNull(body, "카드 몸(bg · 테 상자)");
            Rect rc = World(body), rx = World(x), ro = World(overlay);
            Assert.Greater(rc.height, 0f, "카드 높이");
            Assert.Less(rx.yMin, rc.yMin, "✕ 가 카드 아래로 삐져나온다");
            float overhang = rc.yMin - rx.yMin;
            float tol = rc.height * 0.005f;
            float lump = (rc.yMax + rx.yMin) * 0.5f;   // 덩어리(카드 위끝 ~ ✕ 아래끝)의 가운데
            Assert.AreEqual(ro.center.y, lump, tol,
                "«카드 + ✕» 덩어리의 가운데가 모달 가운데다 · 어긋남 " + ((lump - ro.center.y) / rc.height * 100f).ToString("0.00") + "%카드높이");
            Assert.AreEqual(ro.center.y + overhang * 0.5f, rc.center.y, tol,
                "카드 가운데는 삐져나온 몫의 절반만큼 위다(종전 클론은 카드 자체가 가운데였다)");
            AscendPopup.Close();
            yield return null;
        }

    }
}
