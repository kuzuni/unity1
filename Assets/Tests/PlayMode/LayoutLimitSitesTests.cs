using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T388 — 정본이 앱 크기 비율로 못 박은 목록 상한·버튼 하한이 **실제 자리**에서 서는지(EditMode `LayoutLimitsTests` 는 표값만 본다).
    /// 2회차: 펫 업그레이드 팝업의 재료 격자 — 정본 802 `.mat-grid { max-height: calc(var(--app-h) * .4) }`.
    /// 새 세이브에서 펫 하나를 만들어 팝업을 열면 카드 나머지(1회차 실측 45%H)가 상한(40%H)보다 크므로 격자 높이 = 상한이어야 한다.
    /// 3회차: 「모든 장비의 목록」 — 정본 **722** `.forge-age-list { max-height: calc(var(--app-h) * .59) }`(1회차 실측 클론 **67%H** = +13%).
    /// </summary>
    public class LayoutLimitSitesTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = Time.realtimeSinceStartup;
            while (!(MetaHost.Ready && PopupLayer.Instance != null && UiRoot.Instance != null))
            {
                Assert.Less(Time.realtimeSinceStartup - t, 20f, "MetaHost/PopupLayer 가 20초 안에 준비되지 않았다");
                yield return null;
            }
            yield return null;
        }

        static Transform Find(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++) { Transform r = Find(t.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        [UnityTest]
        public IEnumerator 펫_재료_격자는_카드_나머지가_커도_표_petup_grid_max_f_를_안_넘는다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(PetSkillHost.Ready && SkillPetSheet.Instance != null && SkillBar.Instance != null); i++) yield return null;
            PetSkillHost host = PetSkillHost.Instance;
            SkillPetSheet sheet = SkillPetSheet.Instance;
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            yield return null;
            sheet.Switch(SkillPetSheet.SubPets);
            yield return null;
            while (host.SummonMult("pet") != 1) host.CycleSummonMult("pet");
            host.EggCurrency = 100000;
            host.Gems = 100000;
            host.Sync();
            yield return null;
            Assert.AreEqual(0, host.Pets.State.Pets.Count, "새 세이브 · 펫 0");
            sheet.Pets.SummonButton.onClick.Invoke();
            yield return null;
            for (int k = 0; k < 4 && SkillSummonResultView.Current != null; k++) { SkillSummonResultView.Current.OnTap(); yield return null; }
            Assert.GreaterOrEqual(host.Pets.State.Eggs.Count, 1, "x1 소환 = 알 하나 이상");
            int hatching = host.Pets.State.Hatching.Count;
            sheet.Pets.OpenEggDetail(0);
            yield return null;
            sheet.Modal.Find(PetPanel.DetailModal).Content.Find("btn-hatch").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.IsNotNull(sheet.Pets.SkipButton(hatching), "부화 칸의 스킵");
            sheet.Pets.SkipButton(hatching).onClick.Invoke();
            yield return null;
            Assert.AreEqual(1, host.Pets.State.Pets.Count, "즉시 부화 → 펫 하나");
            sheet.Pets.OpenPetDetail(0);
            yield return null;
            sheet.Modal.Find(PetPanel.DetailModal).Content.Find("btn-upgrade").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.IsTrue(sheet.Modal.IsOpen(PetUpgradePopup.ModalName), "업그레이드 팝업");
            Transform grid = Find(sheet.Modal.Find(PetUpgradePopup.ModalName).Content, "mat-grid");
            Assert.IsNotNull(grid, "재료 격자(mat-grid · 스크롤 뷰)");
            float cap = UiKit.RefH * PetSkillStyle.L("petup_grid_max_f");
            float gh = ((RectTransform)grid).rect.height;
            Assert.AreEqual(0.4f, PetSkillStyle.L("petup_grid_max_f"), 1e-6f, "표 = 정본 802 .4");
            Assert.LessOrEqual(gh, cap + 0.5f, "격자 높이가 정본 상한 .4H 를 넘지 않는다");
            // 1회차 실측: 카드 나머지 ≈ 45%H > 40%H — 그러니 상한이 실제로 «작용» 해 격자 = 상한이어야 한다(작용 안 하면 이 자는 아무것도 안 잰다)
            Assert.AreEqual(cap, gh, 0.5f, "카드 나머지(≈45%H)가 상한보다 크므로 격자 = 상한(.4H)");
            PetUpgradePopup.Close();
            yield return null;
        }

        /// <summary>
        /// T388 3회차 — 정본 **722** `.forge-age-list { max-height: calc(var(--app-h) * .59); overflow-y: auto }`.
        /// 클론은 **카드를 `fl_card_h_f` .76 으로 고정하고 목록을 그 나머지로 채워** 목록이 67%H 로 섰다(1회차 실측 · 런 704).
        /// 정본은 반대다 — `.fl-card` 에 높이 선언이 **없고** 상한은 **안쪽 목록**이 쥔다. 그래서 목록이 상한에서 잘리는가와
        /// **카드도 그만큼 줄었는가**(안 줄이면 카드 아래에 정본에 없는 빈 자리가 남는다)를 같이 본다.
        /// </summary>
        [UnityTest]
        public IEnumerator 장비_목록의_안쪽_목록은_표_fl_list_max_h_f_를_안_넘고_카드도_그만큼_줄어든다()
        {
            yield return Boot();
            float t0 = Time.realtimeSinceStartup;
            while (!ForgeHost.Ready)
            {
                Assert.Less(Time.realtimeSinceStartup - t0, 20f, "ForgeHost 가 20초 안에 준비되지 않았다");
                yield return null;
            }
            ForgeHost h = ForgeHost.Instance;
            ForgeInfoPopup.OpenList(h);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            Popup p = h.Meta.Popups.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(p, "「모든 장비의 목록」 팝업");
            Transform card = Find(p.Root, "card");
            Assert.IsNotNull(card, "목록 카드");
            Transform list = Find(card, "forge-age-list");
            Assert.IsNotNull(list, "안쪽 목록(forge-age-list)");

            Assert.AreEqual(0.59f, ForgeInfoStyle.L("fl_list_max_h_f"), 1e-6f, "표 = 정본 722 .59");
            float cap = UiKit.RefH * ForgeInfoStyle.L("fl_list_max_h_f");
            float lh = ((RectTransform)list).rect.height, ch = ((RectTransform)card).rect.height;
            Assert.LessOrEqual(lh, cap + 0.5f, "목록이 정본 상한 .59H 를 넘지 않는다 · 실측 " + (lh / UiKit.RefH).ToString("0.000") + "H");
            // 상한이 실제로 «작용» 하는가 — 1회차 실측이 67%H 였으니 잘려야 한다(안 잘리면 이 자는 아무것도 안 잰다).
            Assert.AreEqual(cap, lh, 0.5f, "카드 나머지(1회차 실측 67%H)가 상한보다 크므로 목록 = 상한(.59H)");
            // 카드도 줄었다 — 목록 + 카드 안 나머지(패딩·제목·틈·테)가 카드 높이다.
            Assert.Less(ch, UiKit.RefH * ForgeInfoStyle.L("fl_card_h_f"), "카드가 종전 고정 높이(.76H)보다 작아졌다 · 실측 " + (ch / UiKit.RefH).ToString("0.000") + "H");
            Assert.Greater(ch, lh, "카드는 목록보다 크다(제목·패딩 몫)");
            Assert.Less(ch - lh, UiKit.RefH * 0.12f, "카드 아래에 정본에 없는 빈 자리가 남지 않는다 — 카드 − 목록 = 제목·패딩 몫뿐 · 실측 " + ((ch - lh) / UiKit.RefH).ToString("0.000") + "H");
            Debug.Log("[T388] 목록 " + (lh / UiKit.RefH).ToString("0.000") + "H(상한 .59) · 카드 " + (ch / UiKit.RefH).ToString("0.000") + "H(종전 .76)");

            h.Meta.Popups.Hide(ForgeInfoPopup.Name);
            yield return null;
        }
    }
}
