using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Forging;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T434 — 장비 카드의 **안쪽** 패딩이 정본 세 갈래대로인가(<see cref="ForgeUi"/>.ItemCard).
    ///
    /// 정본은 셋으로 적어 뒀고 **위 값은 셋 다 같다**:
    /// 바탕 `.cmp-card { padding: **.9rem** .7rem .7rem }`(style.css 1825) ·
    /// 장착 `.cmp-card-wrap.cur .cmp-card { padding: .9rem .4rem **.4rem** }`(1812) ·
    /// 새 장비 `.cmp-card-wrap.new .cmp-card { padding-bottom: **1.5rem** }`(1815 · 위·좌우는 바탕 그대로).
    ///
    /// ⚑ **이 자가 서 있는 까닭**(1·2회차의 값비싼 교훈 · 결정 753): 그 전엔 이 어긋남을 «위가 모자란다» 로 읽고
    /// **모달 층**의 위 패딩을 키웠다가 화면이 반대로 가서 되돌렸다 — 두 카드가 «내용 높이 + 고정 바닥» 이라
    /// 위를 더하면 내용이 내려가는 게 아니라 **카드가 위로 자란다**. 고칠 자리는 카드 **안쪽**이고
    /// 하는 일은 더하기가 아니라 **재분배**다(위 +0.3rem · 아래 −0.4rem). 그래서 이 자는 위·아래를 **같이** 본다 —
    /// 위만 재면 1회차가 통과시킨 그 상태(위는 맞고 카드가 부푼)를 또 초록으로 넘긴다.
    /// </summary>
    public class CmpCardPadTests
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

        private static Transform FindIn(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        /// <summary>카드 위끝 → 첫 글줄(이름) 상자 위끝 · **내용의 맨 아래끝** → 카드 아래끝을 카드 제 자리에서 잰다.
        /// T476 2차 — 정본 `.cmp-card` 는 flex 행(1826 `align-items: flex-start`)이라 카드 높이 = 패딩 + **두 자식(아이콘 묶음 · 글 블록) 중 큰 쪽**이다.
        /// 옵션이 두 줄이면 글 블록이 크지만, 한 줄이면 아이콘 묶음(타일 + «새로운!» 줄 · 정본 `.cmp-icon-wrap` 5.04rem)이 크다 — 런 1194 가 그 갈래에 걸려
        /// «마지막 글줄 → 카드 아래» 가 1.5rem 이 아니라 2.67rem 으로 났다(글 블록만 보던 자의 가정이 틀렸지 카드가 틀린 게 아니다). 내용 아래끝 = 글줄·타일·«새로운!» 중 가장 아래.</summary>
        private static void AssertPad(RectTransform card, float pt, float pb, string what)
        {
            RectTransform nm = (RectTransform)card.Find("name");
            Assert.IsNotNull(nm, what + ": 이름줄");
            RectTransform last = null;
            for (int i = 0; ; i++)
            {
                Transform s = card.Find("sub-" + i);
                if (s == null) break;
                last = (RectTransform)s;
            }
            Assert.IsNotNull(last, what + ": 부 옵션 줄(글 블록이 있는 갈래여야 잰다)");

            Vector3[] cc = new Vector3[4], nc = new Vector3[4], lc = new Vector3[4];
            card.GetWorldCorners(cc); nm.GetWorldCorners(nc); last.GetWorldCorners(lc);
            float cardTop = card.InverseTransformPoint(cc[1]).y, cardBot = card.InverseTransformPoint(cc[0]).y;
            float nameTop = card.InverseTransformPoint(nc[1]).y, lastBot = card.InverseTransformPoint(lc[0]).y;
            float contentBot = lastBot;
            foreach (string child in new[] { "tile", "newtag" })
            {
                RectTransform c = (RectTransform)card.Find(child);
                if (c == null) continue;
                c.GetWorldCorners(lc);
                contentBot = Mathf.Min(contentBot, card.InverseTransformPoint(lc[0]).y);
            }

            Assert.AreEqual(pt, cardTop - nameTop, 0.6f, what + ": 위 패딩 = 정본 .9rem(1825 · 세 갈래 공통) — 종전 클론은 0.6rem 이었다");
            Assert.AreEqual(pb, contentBot - cardBot, 0.6f, what + ": 아래 패딩 = 정본 값(내용의 맨 아래 자식 → 카드 아래) — 종전 클론은 0.8rem(cur)·2.0rem(new) 이었다");
        }

        /// <summary>T476 — 이 카드의 세 글줄은 691 `.item-stat`(T474 오등재)이 아니라 마크업(ui.js 3224·3236~3237)이 직접 쓰는 `.cmp-name` 1.05(1889) · `.cmp-stat` .95(1890) ·
        /// `.cmp-stat .arrow` .82(1891) · `.cmp-sub` .78rem + line-height 1.5(1894)이고, 글 블록 `.cmp-info` 는 gap .15rem(1888)의 세로 flex 다.
        /// 글자: 이름 = 표값(하한 위 · `Sub` 칸에 크기만) · 주 스탯 = `Sub` 하한(정본 34.6 ↔ 36 · 모양을 안 깨니 예외 칸이 아니다) · 화살·옵션 = `Micro` + 표.
        /// 피치: 이름→스탯 = 1.05 × normal + gap · 스탯→옵션 = .95 × normal + gap · 옵션 사이 = .78 × 1.5 + gap(= 1.32rem · 정본 web 실측 1.321) · normal = 표 `cmp_normal_lh`(정본 web 실측 1.34 · 3차).
        /// 옵션 줄은 T473 이 정본대로 좁힌 폭에서도 **한 줄**(T474 가 연 자리를 안 되돌린다) · 카드 높이 = 위 패딩 + 글 블록 + 아래 패딩(글 블록이 타일보다 클 때).</summary>
        [UnityTest]
        public IEnumerator 장비_카드_세_글줄은_정본_cmp_클래스_크기고_글_블록은_gap_15_의_세로_flex_다()
        {
            float rem = PopupKit.Rem;
            Assert.AreEqual(0.78f, TextSizeUi.Rem("cmp_sub"), 1e-6f, "정본 1894 .cmp-sub .78rem");
            Assert.AreEqual(0.82f, TextSizeUi.Rem("cmp_arrow"), 1e-6f, "정본 1891 .cmp-stat .arrow .82rem");
            Assert.AreEqual(1.05f, CraftStyle.L("cmp_name_font_rem"), 1e-6f, "정본 1889 .cmp-name 1.05rem");
            Assert.AreEqual(0.95f, CraftStyle.L("cmp_stat_font_rem"), 1e-6f, "정본 1890 .cmp-stat .95rem");
            Assert.AreEqual(0.15f, CraftStyle.L("cmp_info_gap_rem"), 1e-6f, "정본 1888 .cmp-info gap .15rem");
            Assert.AreEqual(0.25f, CraftStyle.L("cmp_arrow_ml_rem"), 1e-6f, "정본 1891 .arrow margin-left .25rem");
            Assert.IsFalse(TextSizeUi.Has("item_stat"), "T474 의 `item_stat`(691 · 이 카드의 클래스가 아니다)은 표에서 빠진다");
            float subMin = UiCatalog.Instance.Kind(TextKind.Sub).min;
            Assert.Less(TextSizeUi.Px("cmp_sub"), subMin, "옵션 줄 표값은 하한 Sub 아래라 예외 칸이 필요한 자리다");
            Assert.Less(TextSizeUi.Px("cmp_arrow"), subMin, "화살 표값도 하한 아래");
            Assert.GreaterOrEqual(CraftStyle.Px("cmp_name_font_rem"), subMin, "이름 표값은 하한 위 — 예외 칸이 아니라 Sub 칸에 크기만");
            Assert.Less(CraftStyle.Px("cmp_stat_font_rem"), subMin, "주 스탯 표값은 하한 바로 아래(34.6) — 글자는 하한이 이기고 줄 상자만 표값으로 센다");

            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            h.S.Hammers = 10; h.Pull();
            h.OnCraft();
            float t = 0f;
            while (!h.Meta.Popups.IsOpen(ForgeCraftPopup.Name) && t < 8f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "제작 뒤 비교 팝업이 열린다");
            Canvas.ForceUpdateCanvases();
            Transform root = h.Meta.Popups.Find(ForgeCraftPopup.Name).Root;
            RectTransform newCard = (RectTransform)FindIn(root, "new");
            Assert.IsNotNull(newCard, "새 장비 카드");

            float normal = CraftStyle.L("cmp_normal_lh");   // T476 3차 — 정본 web 실측 normal(1.34) · 자산 비율(1.448)이 아니다
            Assert.AreEqual(1.34f, normal, 1e-6f, "정본 web 실측 normal(.cmp-name 1.359 · .cmp-stat 1.315 → 1.34)");
            Assert.Less(normal, UiFont.Primary.faceInfo.lineHeight / UiFont.Primary.faceInfo.pointSize, "클론 글꼴 자산 비율(1.448)보다 작다 — 그 비율로 세면 카드가 정본보다 0.85%H 자란다(런 1197)");
            float namePx = CraftStyle.Px("cmp_name_font_rem"), statPx = CraftStyle.Px("cmp_stat_font_rem"), subPx = TextSizeUi.Px("cmp_sub");
            float gap = CraftStyle.Px("cmp_info_gap_rem");
            float nameH = namePx * normal, statH = statPx * normal, subH = subPx * (float)LineHeight.Ratio(subPx, "cmp_sub_lh");
            Assert.AreEqual(1.32f * rem, subH + gap, 0.5f, "옵션 줄 피치 = .78 × 1.5 + .15 = 1.32rem(정본 web 실측 1.321)");

            TextMeshProUGUI nm = newCard.Find("name").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI st = newCard.Find("stat").GetComponent<TextMeshProUGUI>();
            Assert.AreEqual(TextKind.Sub, nm.GetComponent<UiTextKindTag>().Kind, "이름 = Sub 칸(하한 위 · 크기만 표)");
            Assert.AreEqual(namePx, nm.fontSize, 0.01f, "이름 글자 = 1.05rem");
            Assert.AreEqual(TextKind.Sub, st.GetComponent<UiTextKindTag>().Kind, "주 스탯 = Sub 칸");
            Assert.AreEqual(UiCatalog.Instance.Kind(TextKind.Sub).size, st.fontSize, 0.01f, "주 스탯 글자 = Sub 하한(정본 .95 = 34.6 ↔ 36 · 예외 칸이 아니다)");
            Assert.AreEqual(nameH, nm.rectTransform.rect.height, 0.5f, "이름 줄 상자 = 1.05rem × normal");
            Assert.AreEqual(statH, st.rectTransform.rect.height, 0.5f, "주 스탯 줄 상자 = .95rem × normal(글자 하한과 무관하게 정본 피치)");
            Assert.AreEqual(nameH + gap, nm.rectTransform.anchoredPosition.y - st.rectTransform.anchoredPosition.y, 0.5f, "이름 → 스탯 피치 = 이름 줄 + gap");
            Transform arrow = newCard.Find("arrow");
            if (arrow != null)
            {
                TextMeshProUGUI ar = arrow.GetComponent<TextMeshProUGUI>();
                Assert.AreEqual(TextKind.Micro, ar.GetComponent<UiTextKindTag>().Kind, "화살 = Micro 칸");
                Assert.AreEqual(TextSizeUi.Px("cmp_arrow"), ar.fontSize, 0.01f, "화살 글자 = .82rem");
                Assert.Less(ar.fontSize, st.fontSize, "화살은 주 스탯보다 작다(정본 .82 < .95)");
                Assert.AreEqual(st.rectTransform.anchoredPosition.x + st.preferredWidth + CraftStyle.Px("cmp_arrow_ml_rem"), ar.rectTransform.anchoredPosition.x, 0.5f, "화살 왼쪽 여백 = margin-left .25rem");
            }
            int subs = 0;
            TextMeshProUGUI prev = st;
            for (int i = 0; ; i++)
            {
                Transform s = newCard.Find("sub-" + i);
                if (s == null) break;
                subs++;
                TextMeshProUGUI tm = s.GetComponent<TextMeshProUGUI>();
                Assert.AreEqual(TextKind.Micro, tm.GetComponent<UiTextKindTag>().Kind, "옵션 줄 " + i + " = Micro");
                Assert.AreEqual(subPx, tm.fontSize, 0.01f, "옵션 줄 " + i + " 글자 = .78rem");
                Assert.Less(tm.fontSize, st.fontSize, "옵션 줄은 주 스탯보다 작다(원작 shot-043224 · 등재 판정 ⓐ)");
                tm.ForceMeshUpdate();
                Assert.AreEqual(1, tm.textInfo.lineCount, "옵션 줄 " + i + " 은 한 줄이다(«" + tm.text + "» · 상자 폭 " + tm.rectTransform.rect.width.ToString("0") + " · 글 폭 " + tm.preferredWidth.ToString("0") + ")");
                Assert.AreEqual(subH, tm.rectTransform.rect.height, 0.5f, "옵션 줄 " + i + " 상자 = .78 × 1.5");
                float want = (i == 0 ? statH : subH) + gap;
                Assert.AreEqual(want, prev.rectTransform.anchoredPosition.y - tm.rectTransform.anchoredPosition.y, 0.5f, "옵션 줄 " + i + " 피치 = 앞 줄 상자 + gap");
                prev = tm;
            }
            float textH = nameH + gap + statH + subs * (gap + subH);
            float pt = CraftStyle.Px("cmp_card_pt_rem"), pb = CraftStyle.Px("cmp_card_pb_new_rem");
            if (textH > rem * 3.6f + rem * 0.5f + PopupKit.FontSize(TextKind.Sub) * normal)
                Assert.AreEqual(pt + textH + pb, newCard.rect.height, 1f, "카드 높이 = 위 패딩 + 글 블록(줄 상자 + gap) + 아래 패딩");
            Debug.Log("[T476] 이름 " + nm.fontSize.ToString("0.0") + " · 스탯 " + st.fontSize.ToString("0.0") + " · 옵션 " + subPx.ToString("0.0") + "px · 옵션 줄 " + subs + " · 옵션 피치 " + ((subH + gap) / rem).ToString("0.00") + "rem · 카드 " + newCard.rect.height.ToString("0.0"));
            h.ResolveCraft("sell"); yield return null;
            if (h.Meta.Popups.IsOpen(ForgeCraftPopup.SellName)) { h.OnSellConfirm(); yield return null; }
        }

        [UnityTest]
        public IEnumerator 장비_카드_안쪽_패딩은_정본_세_갈래대로고_위는_셋이_같다()
        {
            yield return Boot();
            PlayLog log = PlayLog.Start("cmp-card-pad");
            ForgeHost h = ForgeHost.Instance;
            float pt = CraftStyle.Px("cmp_card_pt_rem");

            h.S.Hammers = 10; h.Pull();
            h.OnCraft();
            float t = 0f;
            while (!h.Meta.Popups.IsOpen(ForgeCraftPopup.Name) && t < 8f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "제작 뒤 비교 팝업이 열린다");
            Canvas.ForceUpdateCanvases();
            Transform root = h.Meta.Popups.Find(ForgeCraftPopup.Name).Root;

            // ⓐ 새 장비 카드(`.cmp-card-wrap.new`) — 아래만 1.5rem 으로 넓다(정본 1815 · 그 아래가 [판매][장착] 줄과의 틈이다).
            RectTransform newCard = (RectTransform)FindIn(root, "new");
            Assert.IsNotNull(newCard, "새 장비 카드");
            AssertPad(newCard, pt, CraftStyle.Px("cmp_card_pb_new_rem"), "새 장비 카드");

            // ⓑ 위 카드(`.cur`) — 같은 팝업 안에서 **아래 값만 다르다**. 한 자에서 나란히 봐야 «한 값으로 뭉갰다» 가 걸린다.
            RectTransform curCard = (RectTransform)FindIn(root, "cur");
            if (curCard != null && curCard.Find("sub-0") != null)
                AssertPad(curCard, pt, CraftStyle.Px("cmp_card_pb_cur_rem"), "제작 비교 장착 카드");

            Assert.AreNotEqual(CraftStyle.L("cmp_card_pb_cur_rem"), CraftStyle.L("cmp_card_pb_new_rem"),
                               "정본은 두 갈래의 **아래**를 다르게 적었다(.4rem ↔ 1.5rem) — 한 키로 묶으면 안 된다");

            // T434 4회차 — 두 카드를 벌리는 것은 **각자의 안쪽 패딩**이지 층의 틈이 아니다: 정본 1783 `.cmp-wrap { gap: **0** }`
            //   (윗줄 주석 «하단 앵커라 gap 을 키우면 위 카드가 올라간다 … 안쪽 패딩으로 이미 벌어져 있으므로 0»).
            //   `cmp_card_pt_rem`·`cmp_card_pb_*` 와 **한 벌**이라 여기서 같이 본다 — 누가 «두 카드가 붙었다» 며 틈을 되살리면 이 줄이 먼저 운다.
            VerticalLayoutGroup wrapLg = ((RectTransform)newCard.parent.parent).GetComponent<VerticalLayoutGroup>()
                                         ?? ((RectTransform)FindIn(root, "card")).GetComponent<VerticalLayoutGroup>();
            Assert.IsNotNull(wrapLg, "카드 세로 층");
            Assert.AreEqual(CraftStyle.Px("cmp_wrap_gap_rem"), wrapLg.spacing, 0.5f, "`.cmp-wrap` 의 층 틈 = 정본 gap 0 — 하단 앵커라 이 틈이 곧 카드 위끝을 민다");
            Assert.AreEqual(0f, CraftStyle.L("cmp_wrap_gap_rem"), 1e-4f, "표도 0 이다(정본 1783)");

            ForgeItem item = h.Pending;
            h.ResolveCraft("equip");
            yield return null;
            if (h.Meta.Popups.IsOpen(ForgeCraftPopup.Name)) { h.ResolveCraft("sell"); yield return null; if (h.Meta.Popups.IsOpen(ForgeCraftPopup.SellName)) { h.OnSellConfirm(); yield return null; } }

            // ⓒ 장비 상세도 같은 조각이다(정본 ui.js 3296 이 같은 `.cmp-wrap` 으로 세운다) — `cur` 값이 그대로 걸린다.
            Assert.IsNotNull(item, "제작된 장비");
            GearDetailPopup.Open(h, item.Slot);
            yield return null;
            Canvas.ForceUpdateCanvases();
            RectTransform gearCard = (RectTransform)FindIn(h.Meta.Popups.Find(GearDetailPopup.Name).Root, "cur");
            Assert.IsNotNull(gearCard, "장비 상세 카드");
            if (gearCard.Find("sub-0") != null)
                AssertPad(gearCard, pt, CraftStyle.Px("cmp_card_pb_cur_rem"), "장비 상세 카드");

            GearDetailPopup.Close(h);
            yield return null;
            log.AssertNoRed();
            log.Dispose();
        }
    }
}
