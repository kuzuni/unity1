using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T117 — 판매 코인 분출(정본 `coinBurst`). 1회차는 연출 자체를 직접 부른다(`ForgeHost` 의 호출 한 줄은 T87 lock 뒤):
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
            Assert.AreEqual(6, cb.Layer.GetComponentsInChildren<UnityEngine.UI.Image>(true).Length, "코인 아이콘 여섯");
            // 착지(delay ≤ 5·26+24 = 154ms · 착지 = +780·.72) 뒤 라벨 여섯 — 전부 «+17»
            yield return WaitSec((float)((s.DelayStepMs * 5 + s.DelayJitterMs + s.FlyMs * s.LandK) / 1000.0) + 0.3f);
            Assert.AreEqual(6, cb.LastLabels.Count, "착지 자리마다 라벨 하나");
            foreach (string l in cb.LastLabels) Assert.AreEqual("+17", l, "라벨은 전부 합÷개수(100/6 → 17) — sell-coin-split-rising");
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
    }
}
