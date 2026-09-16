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
            RectTransform list = null;
            foreach (RectTransform rt in UiRoot.Instance.Sheet.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "list") { list = rt; break; }
            Assert.IsNotNull(list, "상점 시트의 스크롤 목록(list)");
            var deals = new System.Collections.Generic.List<RectTransform>();
            foreach (RectTransform rt in list.GetComponentsInChildren<RectTransform>(true))
                if (rt.parent == list && rt.name.StartsWith("deal-")) deals.Add(rt);
            if (deals.Count < 2) Assert.Ignore("이 세이브의 상점에 특가 카드가 둘 미만이다 — 틈을 잴 자리가 없다");
            deals.Sort((a, b) => World(b).yMax.CompareTo(World(a).yMax));
            Rect app = World(UiRoot.Instance.App), a0 = World(deals[0]), a1 = World(deals[1]);
            float gap = (a0.yMin - a1.yMax) / app.height;
            Assert.AreEqual(0.0091f, gap, 0.002f, "특가 카드 사이 = 앱 높이 × .0091(표 shop_deal_gap) · 실측 " + gap.ToString("0.0000"));
            // 표를 부르는지: rem*0.5(= 앱높이/844×16의 절반 ≈ .00948H)와 갈라지는 폭이라 ±0.0002 로 좁혀 한 번 더 본다
            Assert.AreEqual(UiKit.H("shop_deal_gap"), (a0.yMin - a1.yMax), 1.2f, "실측 px = 표 shop_deal_gap × 앱 높이");
            Debug.Log("[T364] 상점 특가 카드 틈 " + gap.ToString("0.0000") + "H · 표 " + UiKit.H("shop_deal_gap").ToString("0.00") + "px");
            yield return null;
        }
    }
}
