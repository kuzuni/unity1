using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core;
using Forge.Core.Forging;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T117 — 판매 코인 분출(정본 `coinBurst`). 앞 둘은 연출 자체를 직접 부르고, 셋째(2회차)는 `ForgeHost.DoResolveCraft` 의 실판매로 돈다:
    /// 조각 수가 금액 눈금대로(3~10) · 라벨이 전부 «+합÷개수» 같은 값 · 층이 장비 시트 위·패널 아래 · 팝업/탭 패널이 열려 있으면 0 · 수명이 끝나면 다 걷힌다.
    /// </summary>
    public class CoinBurstTests
    {
        static IEnumerator Boot()
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

        static IEnumerator WaitSec(float sec) { float t = 0f; while (t < sec) { t += Time.unscaledDeltaTime; yield return null; } }

        [UnityTest]
        public IEnumerator 판매_코인은_금액_눈금대로_3에서_10개가_격자로_날아가_같은_값을_단다()
        {
            yield return Boot();
            UiRoot root = UiRoot.Instance;
            Assert.IsNotNull(root);
            Assert.IsNotNull(CoinBurst.AnvilButton(), "장비 시트의 anvil-btn(정본 .anvil-btn)");
            Assert.IsFalse(CoinBurst.Covered(), "메인 화면 — 팝업·탭 패널 없음");
            CoinBurstSpec s = CoinBurst.Spec;
            int n = CoinBurst.Play(100);
            Assert.AreEqual(6, n, "100 코인 → 3 + round(log10(100)·1.7) = 6");
            CoinBurst cb = CoinBurst.Instance;
            Assert.IsNotNull(cb);
            Assert.AreEqual(root.Sheet.GetSiblingIndex() + 1, cb.Layer.GetSiblingIndex(), "층은 장비 시트 바로 위(정본 z 6) · 패널 아래");
            Assert.Less(cb.Layer.GetSiblingIndex(), root.PanelHost.GetSiblingIndex());
            yield return null;
            Assert.AreEqual(6, cb.Pieces, "조각 여섯이 층에 섰다");
            // T411 1회차 — 코인마다 금색 글로우 한 장이 **뒤에** 선다(정본 7394 drop-shadow · 표 DropShadowUi `coin_fly`): 그림 여섯 + 글로우 여섯
            int coins = 0, glows = 0;
            foreach (UnityEngine.UI.Image im in cb.Layer.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (im.name == "coin-fly-img") { coins++; continue; }
                Transform coinT = im.transform.parent != null ? im.transform.parent.Find("coin-fly-img") : null;
                UnityEngine.UI.Image coin = coinT != null ? coinT.GetComponent<UnityEngine.UI.Image>() : null;
                if (coin == null || im.name != DropShadow.NameFor(coin)) continue;
                Assert.Greater(im.rectTransform.rect.width, coin.rectTransform.rect.width, "글로우 상자는 코인 상자보다 커널 반경만큼 넓다 — 정본 drop-shadow 의 번짐은 요소 상자 밖으로 나간다(T411 3회차)");
                glows++;
                Assert.Less(im.transform.GetSiblingIndex(), coin.transform.GetSiblingIndex(), "글로우는 코인 뒤에 그린다");
                Assert.AreNotEqual(coin.sprite, im.sprite, "글로우는 번지게 구운 판이다(코인 그림 그대로가 아니다)");
                Color gc = DropShadowUi.C("coin_fly");
                Assert.AreEqual(gc.r, im.color.r, 2f / 255f, "글로우 색 R = 정본 #ffc107"); Assert.AreEqual(gc.g, im.color.g, 2f / 255f, "G"); Assert.AreEqual(gc.b, im.color.b, 2f / 255f, "B");
                Assert.Greater(im.color.a, 0.5f, "나는 동안 글로우 알파 ≈ .95 × 코인 알파");
            }
            Assert.AreEqual(6, coins, "코인 아이콘 여섯");
            Assert.AreEqual(6, glows, "코인마다 글로우 하나");
            Assert.AreEqual(0f, DropShadowUi.Px("coin_fly", "dx_px"), 1e-6f, "정본 0 0 .3rem — 오프셋 0");
            Assert.AreEqual(4.8f, DropShadowUi.Px("coin_fly", "blur_px"), 1e-6f, "정본 .3rem = 4.8 CSS px(rem 16)");
            // 착지(delay ≤ 5·26+24 = 154ms · 착지 = +780·.72) 뒤 라벨 여섯 — 전부 «+17»
            yield return WaitSec((float)((s.DelayStepMs * 5 + s.DelayJitterMs + s.FlyMs * s.LandK) / 1000.0) + 0.3f);
            Assert.AreEqual(6, cb.LastLabels.Count, "착지 자리마다 라벨 하나");
            foreach (string l in cb.LastLabels) Assert.AreEqual("+17", l, "라벨은 전부 합÷개수(100/6 → 17) — sell-coin-split-rising");
            // T333 9회차 — 라벨의 8방향 검정 링(정본 7428 `--ol: max(1.4px, .075rem)` · rgba(0,0,0,.92)): SDF 스트로크(T104) + 제 표 색(amt_outline)
            TMPro.TextMeshProUGUI amt = null;
            foreach (Transform ch in cb.Layer) if (ch.name == "coin-amt") { amt = ch.GetComponent<TMPro.TextMeshProUGUI>(); if (amt != null) break; }
            Assert.IsNotNull(amt, "coin-amt 라벨(TMP)");
            Assert.IsTrue(amt.fontMaterial.IsKeywordEnabled("OUTLINE_ON"), "링(SDF 스트로크)이 켜져 있다");
            float wantPx = (float)CoinBurstRules.AmtRingCanvasPx(s, UiKit.L("anvil_fx_px"), PopupKit.Rem);
            Assert.Greater(wantPx, 0f, "표의 링 두께(캔버스 px)");
            Assert.Greater(amt.outlineWidth, 0f, "링 두께가 실제로 얹혔다");
            Color oc = CoinBurstStyle.C("amt_outline");
            Color got = amt.outlineColor;
            Assert.AreEqual(oc.r, got.r, 1e-2f, "링 색 R(검정)");
            Assert.AreEqual(oc.g, got.g, 1e-2f, "링 색 G");
            Assert.AreEqual(oc.b, got.b, 1e-2f, "링 색 B");
            Assert.AreEqual(oc.a, got.a, 1e-2f, "링 알파(.92) — 정본 --olc rgba(0,0,0,.92)");
            // 수명 끝(조각 780+154+120 · 라벨 착지+2000+60) → 다 걷힌다
            yield return WaitSec((float)((s.DelayStepMs * 5 + s.DelayJitterMs + s.FlyMs * s.LandK + s.AmtMs + s.AmtSlackMs) / 1000.0) + 0.5f);
            Assert.AreEqual(0, cb.Pieces, "조각은 다 걷혔다");
            Assert.AreEqual(0, cb.Labels, "라벨도 다 걷혔다");
            Assert.AreEqual(0, cb.Layer.childCount, "층은 비었다(정본 el.remove)");
            // 큰 금액은 상한 10 · 작은 금액은 하한 3
            Assert.AreEqual(10, CoinBurst.Play(1e9));
            yield return null;
            Assert.AreEqual(10, cb.Pieces);
            Assert.AreEqual(3, CoinBurstRules.Count(s, 1));
            Assert.AreEqual(0, CoinBurst.Play(0), "0 코인이면 연출 없음");
        }

        [UnityTest]
        public IEnumerator 팝업이나_탭_패널이_모루를_덮고_있으면_연출도_소리도_통째로_생략한다()
        {
            yield return Boot();
            UiRoot root = UiRoot.Instance;
            MetaHost h = MetaHost.Instance;
            PlayerInfoPopup.Open(h);
            yield return null;
            Assert.IsTrue(CoinBurst.Covered(), "팝업(.modal)이 열렸다");
            Assert.AreEqual(0, CoinBurst.Play(100), "coin-burst-over-modal — 조각 0");
            PlayerInfoPopup.Close(h);
            yield return null;
            Assert.IsFalse(CoinBurst.Covered());
            root.TabBar.Switch("summon");
            yield return null;
            Assert.IsTrue(CoinBurst.Covered(), "탭 패널(.panel.open)이 열렸다");
            Assert.AreEqual(0, CoinBurst.Play(100));
            root.TabBar.Switch(null);
            yield return null;
            Assert.IsFalse(CoinBurst.Covered());
            Assert.AreEqual(6, CoinBurst.Play(100), "덮개가 걷히면 다시 뜬다");
        }
            static IEnumerator WaitCraftPopup(ForgeHost h)
        {
            float t = 0f;
            while (!h.Meta.Popups.IsOpen(ForgeCraftPopup.Name) && t < 8f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "망치질 + 리빌 뒤 비교 팝업이 떠야 한다");
            yield return null;
        }

        /// <summary>T117 2회차 — 실판매: 비교 팝업의 [판매] 가 팝업을 접은 **그 프레임**에 분출이 돈다(정본 3901 · 팝업은 `Destroy` 라 프레임 끝까지 남지만 «열린 목록» 으로 가드를 본다 · 결정 310) · 조각 수·라벨은 판매가 눈금대로 · 탭을 옮긴 정리(`ResolvePendingCraft`)는 정본대로 연출 0.</summary>
        [UnityTest]
        public IEnumerator 실판매_비교_팝업의_판매는_팝업을_접은_그_프레임에_판매가_눈금대로_코인이_튄다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            h.S.Hammers = 20; h.Pull();
            h.OnCraft();
            yield return WaitCraftPopup(h);
            ForgeItem item = h.Pending;
            Assert.IsNotNull(item);
            double price = h.GearSys.SellPrice(item);
            double coins = h.S.Coins;
            int before = CoinBurst.Instance != null ? CoinBurst.Instance.PlayCount : 0;
            h.ResolveCraft("sell");
            if (h.Meta.Popups.IsOpen(ForgeCraftPopup.SellName)) h.OnSellConfirm();   // 옛 장비보다 좋은 것을 팔 때의 확인 — 확인도 팝업을 접은 뒤 같은 프레임에 판다
            // 같은 프레임 — 팝업 판은 아직 modals 아래 있다(Destroy 는 프레임 끝) · 그래도 연출은 돌아야 한다
            Assert.IsFalse(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "[판매] 는 팝업을 닫는다");
            CoinBurst cb = CoinBurst.Instance;
            Assert.IsNotNull(cb, "판매 뒤 코인 분출 층이 선다");
            Assert.AreEqual(before + 1, cb.PlayCount, "실판매 한 번 = 분출 한 번(팝업을 방금 접은 프레임 · 정본 3901)");
            CoinBurstSpec s = CoinBurst.Spec;
            int n = CoinBurstRules.Count(s, price);
            Assert.AreEqual(n, cb.LastCount, "조각 수는 판매가 눈금대로");
            yield return null;
            Assert.AreEqual(coins + price, h.S.Coins, 0.5, "판매가만큼 코인이 들어갔다");
            Assert.Greater(cb.Pieces, 0, "조각이 날고 있다");
            // 라벨은 조각이 **착지할 때** 붙는다(Amount 코루틴) — 런 318 실측: 같은 프레임에 세면 0 이다. 다 내려앉을 때까지 기다린다.
            float tw = 0f;
            while (cb.Pieces > 0 && tw < 6f) { tw += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(0, cb.Pieces, "조각이 전부 착지했다");
            Assert.AreEqual(n, cb.LastLabels.Count, "착지 자리마다 라벨 하나");
            string want = "+" + NumFmt.Fmt(CoinBurstRules.Per(price, n));
            foreach (string l in cb.LastLabels) Assert.AreEqual(want, l, "라벨은 전부 합÷개수");
            // 탭을 옮긴 정리는 정본 resolvePendingCraft 에 coinBurst 가 없다 — 대기품을 세우고 정리해도 분출 0
            yield return WaitSec(2.5f);
            h.S.Hammers = 20; h.Pull();
            h.OnCraft();
            yield return WaitCraftPopup(h);
            int plays = cb.PlayCount;
            h.ResolvePendingCraft();
            yield return null;
            Assert.AreEqual(plays, cb.PlayCount, "탭 이동 정리(resolvePendingCraft)는 정본대로 연출 0");
            Assert.IsNull(h.Pending, "대기품은 자동 판정(판매)됐다");
        }
    }
}
