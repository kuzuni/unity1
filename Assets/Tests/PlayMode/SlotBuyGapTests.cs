using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T364 6회차 ② — 부화장 «칸 사기» 알약 안의 **젬 ↔ 빨강 숫자 틈**. 정본 `style.css` 4563
    /// `.hatchery .slot-buy { gap: calc(var(--app-w) * .0143) }` 인데 클론은 `Rem(0.15f)`(앱 **높이** 기준 · 정본의 3분의 1)로 박혀 있었다.
    /// 알약 폭·젬 크기는 이미 표(`slot_buy_w`·`slot_buy_icon_w`)였고 그 사이 틈만 코드에 남아 있던 자리다.
    /// </summary>
    public class SlotBuyGapTests
    {
        static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null; yield return null;
            float t = 0f;
            while (!(UiRoot.Instance != null && UiRoot.Instance.App != null && MetaHost.Ready) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            yield return null;
        }

        static Transform FindActive(Transform root, string name)
        {
            if (!root.gameObject.activeInHierarchy) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { Transform r = FindActive(root.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        [UnityTest]
        public IEnumerator 칸_사기_알약_안_젬과_값의_틈이_정본_앱_폭_비율이다()
        {
            yield return Boot();
            UiRoot.Instance.TabBar.OnTab("summon");
            yield return null;
            SkillPetSheet sheet = SkillPetSheet.Instance;
            Assert.IsNotNull(sheet, "소환 시트");
            sheet.SubButton(SkillPetSheet.SubPets).onClick.Invoke();
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();

            Transform pill = FindActive(UiRoot.Instance.App, "slot-buy");
            if (pill == null) Assert.Ignore("이 세이브에선 «칸 사기» 알약이 안 선다(P.CanBuySlot() 이 거짓) — 잴 자리가 없다");
            Transform gem = FindActive(pill, "ico"), cost = FindActive(pill, "cost");
            Assert.IsNotNull(gem, "젬 아이콘"); Assert.IsNotNull(cost, "값 글자");

            RectTransform g = (RectTransform)gem, c = (RectTransform)cost;
            float gapPx = c.anchoredPosition.x - (g.anchoredPosition.x + g.rect.width);
            Assert.AreEqual(PetSkillStyle.Px("slot_buy_ico_gap_w"), gapPx, 0.5f,
                            "정본 4563 .hatchery .slot-buy gap .0143W — 젬 오른끝 ↔ 값 왼끝");
            // 정본은 `justify-content: center` 라 «젬 + 틈 + 글자» 덩어리가 알약 한가운데다
            RectTransform pr = (RectTransform)pill;
            float left = g.anchoredPosition.x;
            float right = pr.rect.width - (c.anchoredPosition.x + PetSkillKit.TextWidth(TextKind.Sub, ((TMPro.TextMeshProUGUI)c.GetComponent<TMPro.TextMeshProUGUI>()).text));
            Assert.AreEqual(left, right, 1.0f, "정본 justify-content: center — 덩어리가 알약 한가운데");
        }
    }
}
