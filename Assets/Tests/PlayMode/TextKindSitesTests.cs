using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T391 2회차 — 종류표의 새 단 Head(정본 1.22~1.5rem ≈ 48px)가 자리에 앉았는가.
    /// 리그 시트 제목(정본 3805 `.sheet-title` 1.35rem = 49.1px) · 리그 보상 단 순위(2573 `.lgr-rank-n` 1.22rem = 44.4 → Button · 2563 `.league-tier-rank.text` 1.48rem = 53.9 → Head).
    /// </summary>
    public class TextKindSitesTests
    {
        static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null; yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            yield return null;
        }

        [Test]
        public void 종류표의_Head_단은_하한_위_제목_아래_48px_다()
        {
            float head = UiCatalog.Instance.Kind(TextKind.Head).size, btn = UiCatalog.Instance.Kind(TextKind.Button).size, title = UiCatalog.Instance.Kind(TextKind.Title).size;
            Assert.AreEqual(48f, head, 0.01f, "정본 1.3rem × 36.4 ≈ 47.3 → 48");
            Assert.Greater(head, btn, "버튼(44) 위");
            Assert.Less(head, title, "제목(60) 아래");
        }

        [UnityTest]
        public IEnumerator 리그_시트_제목과_보상_단_순위는_정본_크기_단으로_선다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            float head = UiCatalog.Instance.Kind(TextKind.Head).size, btn = UiCatalog.Instance.Kind(TextKind.Button).size;
            LeagueSheet.Open(h);
            yield return null;
            Popup p = PopupLayer.Instance.Find(LeagueSheet.Name);
            Assert.IsNotNull(p, "리그 시트");
            TextMeshProUGUI title = null;
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true)) if (t.name == "title") { title = t; break; }
            Assert.IsNotNull(title, "리그 제목");
            Assert.AreEqual(head, title.fontSize, 0.01f, "정본 3805 .sheet-title 1.35rem → Head(전엔 Title 60)");
            h.Popups.Hide(LeagueSheet.Name);
            yield return null;
            LeagueSheet.OpenRewards(h);
            yield return null;
            p = PopupLayer.Instance.Find(LeagueSheet.RewardsName);
            Assert.IsNotNull(p, "리그 보상");
            int top = 0, text = 0;
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.name != "label" || t.transform.parent == null || t.transform.parent.name != "rank") continue;
                int n; bool isNum = int.TryParse(t.text, out n);
                if (isNum) { top++; Assert.AreEqual(btn, t.fontSize, 0.01f, "1~3위 숫자(.lgr-rank-n 1.22rem) → Button"); }
                else { text++; Assert.AreEqual(head, t.fontSize, 0.01f, "4위 아래 글자(.league-tier-rank.text 1.48rem) → Head"); }
            }
            Assert.Greater(top, 0, "1~3위 숫자 라벨"); Assert.Greater(text, 0, "4위 아래 글자 라벨");
            h.Popups.Hide(LeagueSheet.RewardsName);
            yield return null;
        }
    }
}
