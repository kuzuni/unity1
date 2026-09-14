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

        /// <summary>그 버튼이 어느 딜 카드(`deal-<key>`) 안에 있는가 — 수령 뒤 `Touch` 가 시트를 다시 그려 버튼 객체가 바뀌므로 이름으로 다시 찾는다(런 313 MissingReference).</summary>
        private static string DealOf(Transform t)
        {
            for (Transform x = t; x != null; x = x.parent) if (x.name.StartsWith("deal-")) return x.name;
            return null;
        }

        private static Button PriceOf(string popupName, string dealName)
        {
            Popup p = PopupLayer.Instance.Find(popupName);
            Assert.IsNotNull(p, popupName + " 이 열려 있지 않다");
            foreach (Button b in p.Root.GetComponentsInChildren<Button>(true))
                if (b.name == "price" && DealOf(b.transform) == dealName) return b;
            return null;
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
            string deal = DealOf(price.transform);
            Assert.IsNotNull(deal, "가격 버튼은 deal-<key> 카드 안에 있다");
            int before = RewardBurst.Instance != null ? RewardBurst.Instance.PlayCount : 0;
            price.onClick.Invoke();
            yield return null;
            RewardBurst rb = RewardBurst.Instance;
            Assert.IsNotNull(rb, "수령 뒤 연출 층이 서야 한다(정본 4941 rewardBurst)");
            Assert.AreEqual(before + 1, rb.PlayCount, "무료칸 수령 = 연출 한 번");
            Assert.Greater(rb.LastEntries.Count, 0, "재화가 하나는 터져야 한다");
            foreach (RewardEntry e in rb.LastEntries) Assert.AreNotEqual("gems", e.Currency, "젬은 claimDeal 이 안 주니 연출에서도 뺀다(정본 4941 delete r.gems)");

            // 수령 뒤 Touch 가 시트를 다시 그린다 — 같은 딜의 **새** 버튼은 «수령 완료»(비활성)이고 눌러도 토스트만 · 연출 없음
            Button again = PriceOf(ShopSheet.Name, deal);
            Assert.IsNotNull(again, "다시 그린 시트에도 같은 딜의 가격 버튼이 있다");
            Assert.IsFalse(again.interactable, "받은 칸은 «수령 완료» 비활성");
            again.onClick.Invoke();
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
