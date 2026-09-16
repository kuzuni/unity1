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
    /// T364 — 정본이 앱 **폭** 비율로 못 박은 `gap`(`calc(var(--app-w) * k)`)이 클론에서도 폭 비율인가.
    /// ⓐ 상단바 프로필 카드(`.profile-card` style.css 91 `gap: calc(var(--app-w) * .0261)` · ui.js 1293): 아바타 오른끝 → 닉네임 왼끝 = 카탈로그 `card_gap` × 앱 폭.
    /// (등재문이 짚은 `ProfilePopup.cs:219·372` 는 닉네임 입력·초기화 확인 대화상자(정본 `prompt()`/`confirm()`)라 그 CSS 의 자리가 아니다.)
    /// ⓑ 웨이브 점 줄(`#wave-pips` 169 `.0363`): 점 사이 = `pip_gap` × 앱 폭. 값은 카탈로그에서 읽는다.
    /// ⓒ 「모든 장비의 목록」 격자(`.forge-item-grid` **728** `gap: calc(var(--app-h) * .0126) calc(var(--app-w) * .0395)` ·
    ///   `padding: calc(var(--app-w) * .0229) calc(var(--app-w) * .0249)`): 1회차가 «값은 맞으나 **코드에 박힘**» 으로 남긴 넷이다(5회차가 곁 표 `ForgeInfoUi.json` 으로 옮겼다).
    ///   여기서는 표값이 정본과 같은지(왕복)와, **화면에 선 칸이 실제로 그 틈·패딩으로 서는지**를 같이 묻는다 —
    ///   표만 물으면 배선이 끊겨도 초록이고, 화면만 물으면 다음 사람이 다시 숫자를 박아도 안 걸린다.
    /// </summary>
    public class GapRatioTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = Time.realtimeSinceStartup;
            while (!MetaHost.Ready || Hud.Instance == null)
            {
                Assert.Less(Time.realtimeSinceStartup - t, 20f, "MetaHost/Hud 가 20초 안에 준비되지 않았다");
                yield return null;
            }
            yield return null;
        }

        static RectTransform Child(Transform t, string name)
        {
            Transform c = t.Find(name);
            Assert.IsNotNull(c, t.name + " 안에 " + name + " 이 없다");
            return (RectTransform)c;
        }

        [UnityTest]
        public IEnumerator 상단바_프로필_카드의_아바타와_닉네임_사이는_앱_폭의_2_61_퍼센트다()
        {
            yield return Boot();
            RectTransform card = (RectTransform)Hud.Instance.ProfileButton.transform;
            RectTransform avatar = Child(card, "avatar");
            RectTransform nick = Child(card, "nickname");
            float avatarRight = avatar.anchoredPosition.x + avatar.sizeDelta.x;
            float gap = nick.anchoredPosition.x - avatarRight;
            Assert.AreEqual(UiKit.W("card_gap"), gap, 0.5f, "틈 = 카탈로그 card_gap × 앱 폭(정본 .profile-card gap: calc(var(--app-w) * .0261))");
            Assert.AreEqual(0.0261f, UiKit.W("card_gap") / UiKit.RefW, 1e-4f, "카탈로그 card_gap 은 정본 .0261 (style.css 91)");
            Assert.Greater(gap, 0f);
        }

        [UnityTest]
        public IEnumerator 웨이브_점_사이는_앱_폭의_3_63_퍼센트다()
        {
            yield return Boot();
            Assert.AreEqual(0.0363f, UiKit.W("pip_gap") / UiKit.RefW, 1e-4f, "카탈로그 pip_gap 은 정본 .0363 (style.css 169 #wave-pips)");
            yield return null;
        }

        private static Rect World(RectTransform rt)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y);
        }

        [UnityTest]
        public IEnumerator 장비_목록_격자의_틈과_패딩은_정본_비율로_표에서_온다()
        {
            // ⓒ-1 표 ↔ 정본 (style.css 728) — 왕복. 이 넷이 종전엔 ForgeInfoPopup.cs 에 숫자로 박혀 있었다.
            Assert.AreEqual(0.0126f, ForgeInfoStyle.L("fl_grid_gap_y_h"), 1e-6f, "row-gap = --app-h × .0126");
            Assert.AreEqual(0.0395f, ForgeInfoStyle.L("fl_grid_gap_x_w"), 1e-6f, "column-gap = --app-w × .0395");
            Assert.AreEqual(0.0229f, ForgeInfoStyle.L("fl_grid_pad_y_w"), 1e-6f, "padding 세로 = --app-w × .0229 (CSS padding: A B 의 A)");
            Assert.AreEqual(0.0249f, ForgeInfoStyle.L("fl_grid_pad_x_w"), 1e-6f, "padding 가로 = --app-w × .0249 (그 B)");

            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            Assert.IsNotNull(h, "ForgeHost");
            float t0 = Time.realtimeSinceStartup;
            while (!ForgeHost.Ready || PopupLayer.Instance == null)
            {
                Assert.Less(Time.realtimeSinceStartup - t0, 20f, "ForgeHost 가 20초 안에 준비되지 않았다");
                yield return null;
            }
            ForgeInfoPopup.OpenList(h);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            Popup p = h.Meta.Popups.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(p, "「모든 장비의 목록」 팝업이 열려 있다");
            RectTransform grid = null;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "forge-item-grid" && rt.gameObject.activeInHierarchy) { grid = rt; break; }
            Assert.IsNotNull(grid, "첫 시대의 forge-item-grid 를 못 찾았다");
            RectTransform c0 = null, c1 = null, c5 = null;
            foreach (RectTransform rt in grid.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.parent != grid) continue;
                if (rt.name == "cell-0") c0 = rt; else if (rt.name == "cell-1") c1 = rt; else if (rt.name == "cell-5") c5 = rt;
            }
            Assert.IsNotNull(c0, "cell-0"); Assert.IsNotNull(c1, "cell-1"); Assert.IsNotNull(c5, "cell-5(둘째 줄 첫 칸)");

            Rect app = World(UiRoot.Instance.App), rg = World(grid), r0 = World(c0), r1 = World(c1), r5 = World(c5);
            Assert.Greater(app.width, 0f, "앱 상자");

            // ⓒ-2 가로 틈 — 이웃 칸 사이는 앱 폭의 3.95%
            float gapX = (r1.xMin - r0.xMax) / app.width;
            Assert.AreEqual(0.0395f, gapX, 0.002f, "칸 사이 가로 틈 = 앱 폭 × .0395 · 실측 " + gapX.ToString("0.0000"));
            // ⓒ-3 세로 틈 — 줄 사이는 앱 **높이**의 1.26% (정본이 gap 의 앞 값만 --app-h 로 준 자리다)
            float gapY = (r0.yMin - r5.yMax) / app.height;
            Assert.AreEqual(0.0126f, gapY, 0.002f, "줄 사이 세로 틈 = 앱 높이 × .0126 · 실측 " + gapY.ToString("0.0000"));
            // ⓒ-4 패딩 — 회색 판 왼끝 → 첫 칸 왼끝 = 폭 × .0249 · 판 위 → 첫 칸 위 = 폭 × .0229(높이가 아니다)
            float padX = (r0.xMin - rg.xMin) / app.width;
            float padY = (rg.yMax - r0.yMax) / app.width;
            Assert.AreEqual(0.0249f, padX, 0.002f, "격자 좌우 패딩 = 앱 폭 × .0249 · 실측 " + padX.ToString("0.0000"));
            Assert.AreEqual(0.0229f, padY, 0.002f, "격자 위아래 패딩 = 앱 **폭** × .0229(정본도 --app-w 다) · 실측 " + padY.ToString("0.0000"));
            Debug.Log("[T364] 목록 격자 틈 x " + gapX.ToString("0.0000") + "W · y " + gapY.ToString("0.0000") + "H · 패딩 x " + padX.ToString("0.0000") + "W · y " + padY.ToString("0.0000") + "W");

            h.Meta.Popups.Hide(ForgeInfoPopup.Name);
            yield return null;
        }

        /// <summary>T364 9회차 ⑧ — 상점 특가 카드 사이 틈(정본 2913 `.shop-deals { gap: calc(var(--app-h) * .0091) }`).
        /// 표 `shop_deal_gap` 이 있는데 부르는 데가 0곳이라 `rem * 0.5` 로 그리던 자리다 — 표를 부르는지 화면에서 잰다.</summary>
        [UnityTest]
        public IEnumerator 상점_특가_카드_사이_틈은_표_shop_deal_gap_대로_선다()
        {
            yield return Boot();
            UiRoot.Instance.TabBar.OnTab("shop");
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            // ⚠ 상점 시트는 `UiRoot.Sheet`(대장간 시트 자리)가 아니라 **팝업 층**이다(`PopupLayer` · `ShopSheet.Render` 가 `PopupLayer.Clear(p)` 위에 세운다).
            //   9회차 1판은 `UiRoot.Sheet` 를 뒤져 «list 없음» 으로 빨갰다 — 자리 찾기를 팝업 뿌리로 옮긴다.
            Popup sp = MetaHost.Instance.Popups.Find(ShopSheet.Name);
            Assert.IsNotNull(sp, "상점 시트 팝업이 열렸다");
            RectTransform list = null;
            foreach (RectTransform rt in sp.Root.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "list") { list = rt; break; }
            Assert.IsNotNull(list, "상점 시트의 스크롤 목록(list)");
            // ⚠ `PopupKit.ScrollList(parent, "list", …)` 가 돌려주는 것은 이름이 "list" 인 상자가 아니라 **그 안의 "content"** 다
            //   (Popups.cs 342–366 · "list" 는 RectMask2D·ScrollRect 를 달은 보기창이고 칸을 쌓는 VerticalLayoutGroup 은 "content" 에 붙는다).
            //   9회차 2판은 `rt.parent == list` 로 걸러 특가 카드를 0장 잡았고 «둘 미만» 으로 스스로 건너뛰었다.
            RectTransform content = (RectTransform)list.Find("content");
            Assert.IsNotNull(content, "스크롤 목록의 내용 칸(list/content)");
            // 10회차 — 카드는 이제 시트 열이 아니라 그 안의 전용 열("deals" · 정본 2913 `.shop-deals`)에 쌓인다.
            RectTransform box = (RectTransform)content.Find("deals");
            Assert.IsNotNull(box, "특가 카드 열(list/content/deals · 정본 .shop-deals)");
            var deals = new System.Collections.Generic.List<RectTransform>();
            foreach (RectTransform rt in box.GetComponentsInChildren<RectTransform>(true))
                if (rt.parent == box && rt.name.StartsWith("deal-")) deals.Add(rt);
            // 표(meta.json shop.DEALS)가 특가 셋을 쥐고 `ShopSheet` 는 산 것·안 산 것 가리지 않고 세 장을 다 세운다 —
            //   둘보다 적게 잡혔다면 세이브 탓이 아니라 자리 찾기가 틀린 것이다. 건너뛰지 않고 빨간다.
            Assert.GreaterOrEqual(deals.Count, 2, "상점 특가 카드(deal-*)가 전용 열에 둘 이상 선다 · 실측 " + deals.Count);
            deals.Sort((a, b) => World(b).yMax.CompareTo(World(a).yMax));
            Rect app = World(UiRoot.Instance.App), a0 = World(deals[0]), a1 = World(deals[1]);
            float gap = (a0.yMin - a1.yMax) / app.height;
            Assert.AreEqual(0.0091f, gap, 0.002f, "특가 카드 사이 = 앱 높이 × .0091(표 shop_deal_gap) · 실측 " + gap.ToString("0.0000"));
            // 표를 부르는지: rem*0.5(= 앱높이/844×16의 절반 ≈ .00948H)와 갈라지는 폭이라 px 로 한 번 더 본다.
            // ⚠ `GetWorldCorners` 는 **세계 단위**라 캐너스 배율(이 런에선 0.25)만큼 표값(=기준 px)과 다르다 — 런 928 이 그래서 빨갔다(4.368 ↔ 17.472 = 딱 1/4).
            //   재 값을 **기준 px 로 되돌려** 견준다(앱 높이가 계약상 `UiKit.RefH` 에 맞게 서있으므로 비는 배율 그대로다).
            float toRef = UiKit.RefH / app.height;
            float dealPx = (a0.yMin - a1.yMax) * toRef;
            //   텀은 레이아웃 간격 그대로라 재기가 정확하다 — 두 값이 0.72px 밖에 안 벌어져 틀을 ±0.4px 로 좁혀야 «표를 부른다» 가 가려지지 않는다.
            Assert.AreEqual(UiKit.H("shop_deal_gap"), dealPx, 0.4f, "실측 px(기준 단위) = 표 shop_deal_gap × 앱 높이 · rem*0.5 는 " + (PopupKit.Rem * 0.5f).ToString("0.00") + "px 라 갈라진다");
            // 10회차 — **시트 열은 그 값이 아니다**: 정본 3792 `.modal-card.sheet { gap: .5rem }` · 머리 ↔ 첫 배너 사이로 재 둘을 갈라 둔다.
            //   이 칸이 없으면 다음 사람이 두 상자를 다시 하나로 접어도 위 칸은 그대로 초록이다.
            RectTransform head = (RectTransform)content.Find("head");
            RectTransform banner = (RectTransform)content.Find("banner-오늘의 특가");
            Assert.IsNotNull(head, "시트 머리(head)"); Assert.IsNotNull(banner, "첫 배너(banner-오늘의 특가)");
            float sheetGap = (World(head).yMin - World(banner).yMax) * toRef;
            Assert.AreEqual(PopupKit.Rem * 0.5f, sheetGap, 0.4f, "시트 열 간격 = .5rem(정본 3792) · 실측 " + sheetGap.ToString("0.00") + "px · 카드 열의 " + dealPx.ToString("0.00") + "px 와 다른 상자다");
            Debug.Log("[T364] 상점 특가 카드 틈 " + gap.ToString("0.0000") + "H · 표 " + UiKit.H("shop_deal_gap").ToString("0.00") + "px");
            yield return null;
        }

        /// <summary>T364 11회차 ⑥⑦ — 정본 722 `.forge-age-list { gap: calc(var(--app-h) * .0492) }` · 738 `.forge-item-cell { gap: calc(var(--app-h) * .0069) }`.
        /// ⑥ 은 클론이 같은 값에 ×0.5 를 얹어 시대 구획 틈이 정본의 절반이었다(8회차 실측) · ⑦ 은 값은 맞았으나 두 자리에 숫자로 박혀 있었다.
        /// 표 왕복 + 화면에 선 구획·칸으로 잰다(표만 물으면 배선이 끊겨도 초록이고 화면만 물으면 다시 숫자를 박아도 안 걸린다).</summary>
        [UnityTest]
        public IEnumerator 장비_목록의_시대_구획_틈과_칸_아래_글자_틈은_앱_높이_비율로_표에서_온다()
        {
            Assert.AreEqual(0.0492f, ForgeInfoStyle.L("fl_age_gap_h"), 1e-6f, "구획 사이 gap = --app-h × .0492 (style.css 722)");
            Assert.AreEqual(0.0069f, ForgeInfoStyle.L("fl_cell_gap_h"), 1e-6f, "칸 바닥 → % 글자 = --app-h × .0069 (style.css 738)");

            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            Assert.IsNotNull(h, "ForgeHost");
            float t0 = Time.realtimeSinceStartup;
            while (!ForgeHost.Ready || PopupLayer.Instance == null)
            {
                Assert.Less(Time.realtimeSinceStartup - t0, 20f, "ForgeHost 가 20초 안에 준비되지 않았다");
                yield return null;
            }
            ForgeInfoPopup.OpenList(h);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            Popup p = h.Meta.Popups.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(p, "「모든 장비의 목록」 팝업이 열려 있다");
            RectTransform content = null;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "content" && rt.parent != null && rt.parent.name == "list" && rt.gameObject.activeInHierarchy) { content = rt; break; }
            Assert.IsNotNull(content, "목록 내용 칸(list/content)");
            UnityEngine.UI.VerticalLayoutGroup lay = content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            Assert.IsNotNull(lay, "목록은 세로 레이아웃");
            Assert.AreEqual(UiKit.RefH * ForgeInfoStyle.L("fl_age_gap_h"), lay.spacing, 0.01f, "⑥ 목록 간격 = 앱 높이 × .0492 그대로(×0.5 없음)");

            RectTransform s0 = null, s1 = null;
            foreach (Transform c in content)
            {
                if (!c.name.StartsWith("section-")) continue;
                if (s0 == null) s0 = (RectTransform)c; else if (s1 == null) { s1 = (RectTransform)c; break; }
            }
            Assert.IsNotNull(s0, "첫 시대 구획"); Assert.IsNotNull(s1, "둘째 시대 구획");
            Rect app = World(UiRoot.Instance.App), r0 = World(s0), r1 = World(s1);
            Assert.Greater(app.height, 0f, "앱 상자");
            float gap = (r0.yMin - r1.yMax) / app.height;
            Assert.AreEqual(0.0492f, gap, 0.002f, "⑥ 화면의 구획 사이 틈 = 앱 높이 × .0492 · 실측 " + gap.ToString("0.0000") + "(종전 ≈ .0246)");

            RectTransform cell0 = null, pct = null;
            foreach (RectTransform rt in s0.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "cell-0") { cell0 = rt; break; }
            Assert.IsNotNull(cell0, "첫 칸 cell-0");
            foreach (RectTransform rt in cell0.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "pct") { pct = rt; break; }
            Assert.IsNotNull(pct, "칸의 % 라벨");
            // UiKit.Place 는 좌상단 앵커 · anchoredPosition.y = −위끝 — 타일은 칸 폭만큼의 정사각(0..size) 이고 라벨은 그 아래 gap 만큼 떨어져 선다
            float size = cell0.rect.width;
            float labelTop = -pct.anchoredPosition.y;
            float cellGap = (labelTop - size) / UiKit.RefH;
            Assert.AreEqual(0.0069f, cellGap, 0.0005f, "⑦ 칸 바닥 → % 글자 = 앱 높이 × .0069 · 실측 " + cellGap.ToString("0.0000"));
            Debug.Log("[T364] 구획 틈 " + gap.ToString("0.0000") + "H · 칸→글자 " + cellGap.ToString("0.0000") + "H");

            h.Meta.Popups.Hide(ForgeInfoPopup.Name);
            yield return null;
        }

        /// <summary>T364 12회차 ① — 던전 상세 `◀ 난이도 N ▶` 줄. 정본 5304 `.dgd-stage-row { display:flex; align-items:center; justify-content:center; gap: calc(var(--app-w) * .1237) }` ·
        /// 4583 `.tri-btn { width: 1.77rem; height: 2.2rem }` · 5307 아이콘 4%H. 클론은 «반 틈(gap × 0.5) + 카드 폭 30% 고정 상자» 라 **모델**이 달랐다 —
        /// 두 어긋남이 서로를 지워 한 스테이지에서만 잉크가 맞았다(3회차의 «값만 바꾸면 되레 벌어진다»). 표 왕복 + 화면에 선 상자로 둘 다 묻는다:
        /// 틈이 **온전한지**(반틈이면 빨강) · 가운데 상자가 **글자 폭**인지(30% 고정이면 빨강) · 단추 상자가 1.77×2.2rem 인지 · 좌우 대칭인지.</summary>
        [UnityTest]
        public IEnumerator 던전_상세_삼각단추_틈은_앱_폭_1237_의_온전한_틈이고_가운데_상자는_글자_폭이다()
        {
            // 표 ↔ 정본 왕복
            Assert.AreEqual(0.1237f, UiKit.L("dgd_tri_gap"), 1e-6f, "정본 5304 gap = calc(var(--app-w) * .1237)");
            Assert.AreEqual(1.77f, UiKit.L("tri_btn_w_rem"), 1e-6f, "정본 4583 .tri-btn width 1.77rem");
            Assert.AreEqual(2.2f, UiKit.L("tri_btn_h_rem"), 1e-6f, "정본 4583 .tri-btn height 2.2rem");
            Assert.AreEqual(0.04f, UiKit.L("dgd_tri"), 1e-6f, "정본 5307 .tri-btn .ico 4%H");

            yield return Boot();
            float t0 = Time.realtimeSinceStartup;
            while (!DungeonUiHost.Ready)
            {
                Assert.Less(Time.realtimeSinceStartup - t0, 20f, "DungeonUiHost 가 20초 안에 Ready 되지 않았다");
                yield return null;
            }
            // 새 세이브에선 «hammer» 가 잠겨 Open 이 토스트만 내고 돌아간다(DungeonStageRowTests 와 같은 길).
            DungeonUiHost.Instance.S.BestChapter = 5;
            DungeonUiHost.Instance.S.BestStage = 1;
            UiRoot.Instance.TabBar.OnTab("dungeon");
            yield return null;
            DungeonDetailPopup.Open("hammer");
            yield return null;
            yield return null;
            Assert.IsTrue(DungeonDetailPopup.IsOpen, "던전 상세가 열린다(해금 뒤)");
            Canvas.ForceUpdateCanvases();

            RectTransform card = null;
            foreach (RectTransform rt in UiRoot.Instance.App.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "card" && rt.parent != null && rt.parent.name == "modal-dungeon-detail") { card = rt; break; }
            Assert.IsNotNull(card, "던전 상세 카드");
            RectTransform prev = (RectTransform)DungeonPopups.Root(DungeonDetailPopup.PrevButton);
            RectTransform next = (RectTransform)DungeonPopups.Root(DungeonDetailPopup.NextButton);
            TMP_Text lab = Child(card, "stage-label").GetComponent<TMP_Text>();
            TMP_Text num = Child(card, "stage-num").GetComponent<TMP_Text>();
            Assert.IsNotNull(lab, "「난이도」"); Assert.IsNotNull(num, "스테이지 수");

            float triW = DungeonPopups.RemL("tri_btn_w_rem"), triH = DungeonPopups.RemL("tri_btn_h_rem");
            Assert.AreEqual(triW, prev.rect.width, 0.1f, "◀ 상자 폭 = 1.77rem(정본 4583) · 아이콘(4%H)이 상자보다 넓어도 상자는 이 값이다");
            Assert.AreEqual(triH, prev.rect.height, 0.1f, "◀ 상자 높이 = 2.2rem(정본 4583)");
            Assert.AreEqual(triW, next.rect.width, 0.1f, "▶ 상자 폭 = 1.77rem");

            // UiKit.Place 는 좌상단 앵커 — x 는 카드 안 기준 px.
            float gapPx = UiKit.W("dgd_tri_gap");
            float stageW = Mathf.Max(lab.preferredWidth, num.preferredWidth);
            float cx = card.rect.width * 0.5f;
            float leftGap = (cx - stageW * 0.5f) - (prev.anchoredPosition.x + triW);
            float rightGap = next.anchoredPosition.x - (cx + stageW * 0.5f);
            Assert.AreEqual(gapPx, leftGap, 0.5f, "◀ ↔ 글자 = 앱 폭 × .1237 **온전히** · 실측 " + leftGap.ToString("0.0") + "px · 반틈이면 " + (gapPx * 0.5f).ToString("0.0") + "px");
            Assert.AreEqual(gapPx, rightGap, 0.5f, "글자 ↔ ▶ 도 같은 온전한 틈 · 실측 " + rightGap.ToString("0.0") + "px");
            Assert.AreEqual(leftGap, rightGap, 0.5f, "좌우 대칭(정본 justify-content: center)");
            // 가운데 상자가 «글자 폭» 인가 — 카드 폭 30% 고정 상자로 되돌아가면 삼각형이 그만큼 벌어진다.
            float measured = (next.anchoredPosition.x - (prev.anchoredPosition.x + triW)) - gapPx * 2f;
            Assert.AreEqual(stageW, measured, 0.5f, "가운데 자리 = 글자 폭(정본 .dgd-stage shrink-to-fit) · 실측 " + measured.ToString("0.0") + "px · 카드 폭 30% 는 " + (card.rect.width * 0.3f).ToString("0.0") + "px");
            Assert.Less(measured, card.rect.width * 0.3f - 1f, "30% 고정 상자가 아니다");
            Debug.Log("[T364] ◀▶ 틈 " + leftGap.ToString("0.0") + "px(= 앱 폭 × " + (leftGap / UiKit.RefW).ToString("0.0000") + ") · 가운데 글자 폭 " + stageW.ToString("0.0") + "px · 단추 상자 " + prev.rect.width.ToString("0.0") + "×" + prev.rect.height.ToString("0.0"));
            DungeonDetailPopup.Close();
            yield return null;
        }
    }
}
