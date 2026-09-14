using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Save;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T134 3회차 — 공통 수령 연출(<see cref="RewardBurst"/>)이 실제 수령 자리에서 불리는가. 연출 자체(개수·비행·앵커)는 1회차 `RewardBurstTests` 가 지킨다 —
    /// 여기서는 «상점 무료칸을 누르면 한 번 터지고(정본 ui.js 4941) 젬은 빠지며 시작점이 그 버튼인가 · 수령할 것이 없는 일괄수령은 안 터지는가» 를 본다.
    /// </summary>
    public class RewardBurstWiringTests
    {
        private static void DeleteSave()
        {
            try
            {
                string p = System.IO.Path.Combine(Application.persistentDataPath, (SaveIo.Defs != null ? SaveIo.Defs.SaveKey : "forgeclone_save_v1") + ".json");
                if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
            }
            catch (System.Exception) { /* 저장소 접근 실패는 무시 */ }
        }

        [TearDown]
        public void CleanSave() { DeleteSave(); }

        private static IEnumerator Boot()
        {
            DeleteSave();
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && PopupLayer.Instance != null && UiRoot.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 준비되지 않았다");
            Assert.IsNotNull(PopupLayer.Instance, "팝업 층이 서지 않았다");
            yield return null;
        }

        private static Button FindButton(string popupName, string name, bool interactableOnly)
        {
            Popup p = PopupLayer.Instance.Find(popupName);
            Assert.IsNotNull(p, popupName + " 이 열려 있지 않다");
            foreach (Button b in p.Root.GetComponentsInChildren<Button>(true))
                if (b.name == name && (!interactableOnly || b.interactable)) return b;
            return null;
        }

        [UnityTest]
        public IEnumerator 상점_무료칸_수령은_그_버튼에서_한_번_터지고_젬은_빠지며_수령할_것_없는_일괄수령은_안_터진다()
        {
            yield return Boot();
            UiRoot.Instance.TabBar.OnTab("shop");
            yield return null;
            Button price = FindButton(ShopSheet.Name, "price", true);
            Assert.IsNotNull(price, "새 세이브의 상점에 아직 안 받은 무료칸이 하나는 있어야 한다");
            int before = RewardBurst.Instance != null ? RewardBurst.Instance.PlayCount : 0;
            price.onClick.Invoke();
            yield return null;
            RewardBurst rb = RewardBurst.Instance;
            Assert.IsNotNull(rb, "수령 뒤 연출 층이 서야 한다(정본 4941 rewardBurst)");
            Assert.AreEqual(before + 1, rb.PlayCount, "무료칸 수령 = 연출 한 번");
            Assert.Greater(rb.LastEntries.Count, 0, "재화가 하나는 터져야 한다");
            foreach (RewardEntry e in rb.LastEntries) Assert.AreNotEqual("gems", e.Currency, "젬은 claimDeal 이 안 주니 연출에서도 뺀다(정본 4941 delete r.gems)");

            // 같은 버튼을 다시 — 오늘은 이미 수령 → 토스트만 · 연출 없음
            price.onClick.Invoke();
            yield return null;
            Assert.AreEqual(before + 1, rb.PlayCount, "이미 받은 칸은 안 터진다");

            UiRoot.Instance.TabBar.OnTab("quest");
            yield return null;
            Button all = FindButton(QuestSheet.Name, "claim-all", false);
            Assert.IsNotNull(all, "일괄수령 버튼");
            all.onClick.Invoke();
            yield return null;
            Assert.AreEqual(before + 1, rb.PlayCount, "수령할 퀘스트가 없으면 연출 없이 토스트만(정본 4623 앞줄)");
            UiRoot.Instance.TabBar.OnTab("quest");
            yield return null;
        }
    }
}
