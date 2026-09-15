using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T371 2회차 — 모루 «들고 있는 장비» 카드의 세 색이 **표에서 섞여** 나온다: 정본 `style.css` 990 `.anvil-btn.held-slot`
    /// (면 `color-mix(in srgb, --rc 30%, #17181a)` · 테 `80%, #000`)과 1013 `.deck --dedge`(`55%, #dfe4ec`).
    /// 종전에는 비율(.7·.2·.45)과 상대색이 코드에 박혀 있었고, 가장자리 색은 `#DEE3ED` 로 정본 `#dfe4ec` 와 채널마다 1 어긋났다.
    /// </summary>
    public class ColorMixSitesTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && UiRoot.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 안 섰다");
            yield return null;
        }

        static void AssertMix(Color got, Color a, double f, int wr, int wg, int wb, string what)
        {
            double r, g, b;
            Forge.Core.Ui.ColorMixRules.SrgbOpaque(a.r * 255.0, a.g * 255.0, a.b * 255.0, wr, wg, wb, f, out r, out g, out b);
            Assert.AreEqual(r, got.r * 255.0, 1.0, what + ": R(정본 바이트 ±1)");
            Assert.AreEqual(g, got.g * 255.0, 1.0, what + ": G");
            Assert.AreEqual(b, got.b * 255.0, 1.0, what + ": B");
        }

        [UnityTest]
        public IEnumerator 표가_정본_비율과_상대색을_그대로_쥔다()
        {
            yield return Boot();
            Color rc = new Color(0x6b / 255f, 0x35 / 255f, 0x38 / 255f, 1f);      // 정본 var(--rc) 의 기본값
            AssertMix(ColorMixUi.Mix("held_face", rc), rc, 0.30, 0x17, 0x18, 0x1a, "모루 카드 면(정본 990 30%)");
            AssertMix(ColorMixUi.Mix("held_line", rc), rc, 0.80, 0, 0, 0, "모루 카드 테(정본 990 80%)");
            AssertMix(ColorMixUi.Mix("held_deck_edge", rc), rc, 0.55, 0xdf, 0xe4, 0xec, "겹친 장 가장자리(정본 1013 55%, #dfe4ec)");

            // 옛 손입력 색(#DEE3ED)과의 차이는 **바이트 아래**다 — 45% 로 눌리면 0.52 라 반올림하면 같은 수가 된다(런 641 이 그것을 보여 줬다).
            //   그러니 «섞은 뒤 색이 다르다» 가 아니라 **표가 정본 색을 그대로 쥐는가** 로 잰다(고친 것이 그것이다).
            double r0, g0, b0;
            Forge.Core.Ui.ColorMixRules.SrgbOpaque(rc.r * 255.0, rc.g * 255.0, rc.b * 255.0, 0.87 * 255, 0.89 * 255, 0.93 * 255, 0.55, out r0, out g0, out b0);
            double rNew = ColorMixUi.Mix("held_deck_edge", rc).r * 255.0;
            Assert.Greater(System.Math.Abs(r0 - rNew), 1e-6, "표를 안 고쳤으면 이 회차가 한 일이 없다(차이는 바이트 아래라도 값은 달라야 한다)");
            Assert.Less(System.Math.Abs(r0 - rNew), 1.0, "옛 값과의 차이는 바이트 아래다 — 눈으로는 못 가린다(기록에 그대로 적었다)");

            // 투명과 섞는 자리(그림자)는 색이 그대로고 알파만 준다 — 정본 8538 `.equip-cell:not(.egg-cell)` 62%.
            Color sh = ColorMixUi.Mix("cell_shadow_2", rc);
            Assert.AreEqual(0.62f, sh.a, 0.002f, "그림자는 알파를 만드는 섞기다");
            Assert.AreEqual(rc.r, sh.r, 0.004f, "색은 그대로");
        }

        [UnityTest]
        public IEnumerator 모루_들고_있는_장비_카드가_표_색으로_선다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            Forge.Core.Forging.ForgeItem it = h.Engine.RollItem();
            h.SetPendingCraft(it);
            ForgeSheet.Render(h);
            yield return null;

            // 대기품 카드는 «들고 있는 장비» 그림(held-img)을 쥔 상자다 — 이름이 "card" 인 상자는 팝업에도 있으므로 그림으로 집는다.
            Transform card = null;
            foreach (Transform t in UiRoot.Instance.Sheet.GetComponentsInChildren<Transform>(true))
                if (t.name == "held-img" && t.parent != null && t.parent.name == "card") { card = t.parent; break; }
            Assert.IsNotNull(card, "모루 자리에 대기품 카드가 서야 한다");
            // `ForgeUi.Tile(parent, "frame", …)` 은 상자 "frame" 안에 테("line")와 면("face")을 세운다 — 색은 그 **두 자식**에 있다(런 641 NRE 가 그것이었다).
            Transform frame = card.Find("frame");
            Assert.IsNotNull(frame, "카드에 frame 상자가 있어야 한다");
            Image face = frame.Find("face").GetComponent<Image>();
            Image line = frame.Find("line").GetComponent<Image>();
            Color ac = ForgeUi.AgeColor(h.Defs, it.Age);
            AssertMix(face.color, ac, 0.30, 0x17, 0x18, 0x1a, "카드 면이 표대로 섞였다(정본 990 30%, #17181a)");
            AssertMix(line.color, ac, 0.80, 0, 0, 0, "카드 테가 표대로 섞였다(정본 990 80%, #000)");
        }

        /// <summary>T371 6회차 — 장비 칸(정본 828 `.equip-cell` 면 58% #17181a · 테 80% #000)과 비교 카드 그림 바탕(1852 `.cmp-img` 58% #17181a)이 **표에서 섞여** 나온다.
        /// 종전 `ForgeUi.CellFace/CellLine` 은 `.42`·`.2` 를 코드에 박고 있었다(값은 같다 — 이 칸은 «표를 부르는가» 를 잰다).</summary>
        [UnityTest]
        public IEnumerator 장비_칸_면_테와_비교_카드_그림_바탕이_표_색으로_선다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            Forge.Core.Forging.ForgeItem it = h.Engine.RollItem();
            h.Gear.Set(it.Slot, it);
            ForgeSheet.Render(h);
            yield return null;
            Color ac = ForgeUi.AgeColor(h.Defs, it.Age);
            Transform cell = null;
            foreach (Transform t in UiRoot.Instance.Sheet.GetComponentsInChildren<Transform>(true)) if (t.name == "cell-" + it.Slot) { cell = t; break; }
            Assert.IsNotNull(cell, "장비 칸 cell-" + it.Slot);
            Transform frame = cell.Find("frame");
            AssertMix(frame.Find("face").GetComponent<Image>().color, ac, 0.58, 0x17, 0x18, 0x1a, "장비 칸 면(정본 828 58%, #17181a)");
            AssertMix(frame.Find("line").GetComponent<Image>().color, ac, 0.80, 0, 0, 0, "장비 칸 테(정본 828 80%, #000)");
            AssertMix(ForgeUi.CellFace(ac), ac, 0.58, 0x17, 0x18, 0x1a, "CellFace 가 표 cell_face 를 부른다");
            AssertMix(ForgeUi.CellLine(ac), ac, 0.80, 0, 0, 0, "CellLine 이 표 cell_line 을 부른다");

            ForgeCraftPopup.Show(h, it);
            yield return null; yield return null;
            Popup p = h.Meta.Popups.Find(ForgeCraftPopup.Name);
            Assert.IsNotNull(p, "비교 팝업");
            Transform cmpFace = p.Root.Find("card/lower/new/tile/frame/face");
            Assert.IsNotNull(cmpFace, "비교 카드(new) 그림 타일의 면");
            AssertMix(cmpFace.GetComponent<Image>().color, ac, 0.58, 0x17, 0x18, 0x1a, "비교 카드 그림 바탕(정본 1852 .cmp-img 58%, #17181a · 표 cmp_img_face)");
            ForgeCraftPopup.Hide(h);
            yield return null;
        }

        /// <summary>T371 3회차 — 기술 트리 분기 원판은 **가지색 원색이 아니다**: 정본 2110 바탕 `color-mix(… 78%, #fff)` · 2111 테 `color-mix(… 45%, #000)`.
        /// 클론은 둘 다 원색·공용 선색이라 힘 갈래에서 (226,87,76) ↔ 정본 (232,124,115) 로 갈려 있었다.</summary>
        [UnityTest]
        public IEnumerator 기술_분기_원판은_흰색을_섞어_밝힌_가지색으로_선다()
        {
            yield return Boot();
            float t = 0f;
            while (!(MetaHost.Ready && UiRoot.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            UiRoot.Instance.TabBar.OnTab("summon");
            yield return null;
            SkillPetSheet sheet = SkillPetSheet.Instance;
            Assert.IsNotNull(sheet, "소환 시트");
            sheet.Switch(SkillPetSheet.SubTech);
            yield return null;
            yield return null;

            Transform disc = null;
            foreach (Transform x in UiRoot.Instance.App.GetComponentsInChildren<Transform>(true))
                if (x.name == "icon-bg" && x.Find("face") != null) { disc = x; break; }
            Assert.IsNotNull(disc, "분기 원판(icon-bg)");
            Image rim = disc.GetComponent<Image>();
            Image face = disc.Find("face").GetComponent<Image>();
            Color bc = UiKit.C("tech_branch_power");                      // 첫 가지(힘) — catalog 의 --bc 짝
            AssertMix(face.color, bc, 0.78, 0xff, 0xff, 0xff, "원판 바탕(정본 2110 78%, #fff)");
            AssertMix(rim.color, bc, 0.45, 0, 0, 0, "원판 테(정본 2111 45%, #000)");
            Assert.Greater(face.color.g * 255.0, bc.g * 255.0 + 20, "흰색을 섞었으니 원색보다 밝다(원색이면 이 회차가 한 일이 없다)");
        }

        /// <summary>T371 4회차 — 보류가 여럿이면 «겹쳐 쌓인 덱» 이고, 정본 1039~1050 은 겹마다 **두 장**을 깐다:
        /// `dg*i` 자리에 어두운 틈(`--dgap` 15%, #05060a) · 그 위 `dg*i − 1px` 에 밝은 단면(`--dedge` 55%, #dfe4ec).
        /// 뒤 장이 오른쪽 1px 만 드러나 겹 사이에 어두운 선이 남는다 — 클론은 밝은 단면만 깔아 그 선이 없었다.</summary>
        [UnityTest]
        public IEnumerator 겹쳐_쌓인_덱은_겹마다_밝은_단면과_어두운_틈_두_장이다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            // 덱은 «보류가 둘 이상» 일 때만 선다(HeldDeckDepth) — 대기품 하나 + 대기열 둘로 세 장을 만든다.
            h.SetPendingCraft(h.Engine.RollItem());
            for (int i = 0; i < 2; i++) h.AutoQueue.Add(h.Engine.RollItem());
            ForgeSheet.Render(h);
            yield return null;
            if (ForgeHost.HeldDeckDepth(h.HeldCount) < 1)
                Assert.Ignore("이 세이브에선 덱이 안 선다(보류 " + h.HeldCount + "장 · 덱은 둘부터) — 잴 자리가 없다");

            Transform deckEdge = null, deckGap = null;
            foreach (Transform t in UiRoot.Instance.Sheet.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "deck-1") deckEdge = t;
                else if (t.name == "deck-gap-1") deckGap = t;
            }
            Assert.IsNotNull(deckGap, "겹마다 어두운 틈 장이 있어야 한다(정본 --dgap)");
            Assert.IsNotNull(deckEdge, "겹마다 밝은 단면 장이 있어야 한다(정본 --dedge)");

            Color ac = ForgeUi.AgeColor(h.Defs, h.Pending.Age);
            AssertMix(deckGap.GetComponent<Image>().color, ac, 0.15, 0x05, 0x06, 0x0a, "겹 사이 어두운 틈");
            AssertMix(deckEdge.GetComponent<Image>().color, ac, 0.55, 0xdf, 0xe4, 0xec, "겹 단면");
            Assert.Greater(deckEdge.GetSiblingIndex(), deckGap.GetSiblingIndex(), "단면이 틈 위에 깔려 틈은 오른쪽 1px 만 드러난다");
            float line1 = UiKit.L("line_px");
            Assert.AreEqual(line1, ((RectTransform)deckGap).anchoredPosition.x - ((RectTransform)deckEdge).anchoredPosition.x, 0.01f,
                "단면은 틈보다 1 CSS px 왼쪽이다(정본 calc(var(--dg) * i − 1px))");
        }

        /// <summary>T371 5회차 — 정본 1063 `.auto-drop-card` · 1131 `.craft-batch .cb-card` 는 면 `color-mix(… 58%, #17181a)` · 테 `… 80%, #000`.
        /// `CraftCard` 가 `ItemTile`(ForgeUi.CellFace/CellLine · 비율 박힘) 위에 표 색을 덮는다 — 묶음 카드판을 띄워 frame 의 두 자식 색을 정본 바이트로 잰다.</summary>
        [UnityTest]
        public IEnumerator 제작_묶음_카드와_리빌_카드는_표_색으로_선다()
        {
            yield return Boot();
            float t0 = 0f;
            while (!ForgeHost.Ready && t0 < 20f) { t0 += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost");
            ForgeHost F = ForgeHost.Instance;
            Color rc = new Color(0.8f, 0.5f, 0.2f);
            AssertMix(ColorMixUi.Mix("batch_card_face", rc), rc, 0.58, 0x17, 0x18, 0x1a, "묶음 카드 면(정본 1131 58%, #17181a)");
            AssertMix(ColorMixUi.Mix("batch_card_line", rc), rc, 0.80, 0, 0, 0, "묶음 카드 테(정본 1131 80%, #000)");
            AssertMix(ColorMixUi.Mix("drop_card_face", rc), rc, 0.58, 0x17, 0x18, 0x1a, "탈락·리빌 카드 면(정본 1063 58%, #17181a)");
            AssertMix(ColorMixUi.Mix("drop_card_line", rc), rc, 0.80, 0, 0, 0, "탈락·리빌 카드 테(정본 1063 80%, #000)");

            // 런 692 — `ShowBatch` 는 «탭이 없고 팝업 0»(ForgeScreenVisible) 일 때만 띄운다. 이 파일의 앞 칸이 소환 탭을 열어 두거나 부팅 팝업이 남아 있으면
            //           조기 반환해 겹이 안 뜬다 — 대장간 화면을 먼저 보이게 한다(CraftCardInkTests 는 세이브를 지우고 부팅해 그 조건이 저절로 맞았다).
            float t1 = 0f;
            while (!MetaHost.Ready && t1 < 20f) { t1 += Time.unscaledDeltaTime; yield return null; }
            if (UiRoot.Instance.TabBar.ActiveTab != null) UiRoot.Instance.TabBar.Switch(null);
            F.Meta.Popups.HideAll();
            yield return null; yield return null;
            var items = new System.Collections.Generic.List<Forge.Core.Forging.ForgeItem>();
            for (int i = 0; i < 2; i++) items.Add(F.Engine.RollItem());
            ForgeCraftPopup.ShowBatch(F, items, () => { });
            yield return null; yield return null;
            Transform batch = UiRoot.Instance.App.Find("craft-batch");
            Assert.IsNotNull(batch, "묶음 겹(craft-batch)이 떴다 — 탭 " + (UiRoot.Instance.TabBar.ActiveTab ?? "없음") + " · 열린 팝업 " + F.Meta.Popups.OpenCount);
            int seen = 0;
            for (int i = 0; i < items.Count; i++)
            {
                Transform card = null;
                foreach (Transform t in batch.GetComponentsInChildren<Transform>(true)) if (t.name == "cb-card-" + i) { card = t; break; }
                Assert.IsNotNull(card, "cb-card-" + i);
                Transform frame = card.Find("frame");
                Assert.IsNotNull(frame, "카드의 frame");
                Image face = frame.Find("face").GetComponent<Image>();
                Image line = frame.Find("line").GetComponent<Image>();
                Color ac = ForgeUi.AgeColor(F.Defs, items[i].Age);
                AssertMix(face.color, ac, 0.58, 0x17, 0x18, 0x1a, "묶음 카드 면이 표대로 섞였다");
                AssertMix(line.color, ac, 0.80, 0, 0, 0, "묶음 카드 테가 표대로 섞였다");
                seen++;
            }
            Assert.AreEqual(items.Count, seen);
            ForgeCraftPopup.DismissBatch();
            yield return null;
        }
    }
}
