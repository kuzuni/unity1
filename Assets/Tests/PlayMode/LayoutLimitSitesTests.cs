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

        /// <summary>
        /// T388 4회차 — 정본 **5066** `.fi-pill { min-width: 5.5rem }`(+ `display: inline-flex` 라 내용만큼 좁아진다).
        /// 클론은 이 자리를 `inner * 0.4`(= 8.31rem)로 **고정**해 정본 하한의 1.51배로 섰다.
        /// 하한은 «최소» 지 «폭» 이 아니므로 **하한 이상**과 **내용이 안 잘린다**를 같이 본다 — 하한을 그냥 폭으로 박으면 큰 수에서 글자가 잘린다.
        /// </summary>
        [UnityTest]
        public IEnumerator 확률_정보_알약은_정본_하한_5_5rem_아래로_안_내려가고_내용을_안_자른다()
        {
            yield return Boot();
            float t0 = Time.realtimeSinceStartup;
            while (!ForgeHost.Ready)
            {
                Assert.Less(Time.realtimeSinceStartup - t0, 20f, "ForgeHost 가 20초 안에 준비되지 않았다");
                yield return null;
            }
            ForgeHost h = ForgeHost.Instance;
            ForgeInfoPopup.Open(h);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            Popup p = h.Meta.Popups.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(p, "확률 정보 팝업");
            Transform pills = Find(p.Root, "pills");
            Assert.IsNotNull(pills, "알약 줄(pills)");
            Assert.AreEqual(5.5f, ForgeInfoStyle.L("fi_pill_min_w_rem"), 1e-6f, "표 = 정본 5066 5.5rem");
            float min = PopupKit.Rem * ForgeInfoStyle.L("fi_pill_min_w_rem");

            int seen = 0;
            float total = 0f;
            foreach (string n in new[] { "coin", "gem" })
            {
                RectTransform pill = (RectTransform)pills.Find(n);
                Assert.IsNotNull(pill, n + " 알약");
                float w = pill.rect.width;
                Assert.GreaterOrEqual(w, min - 0.5f, n + " 알약이 정본 하한 5.5rem 밑으로 내려갔다 · 실측 " + (w / PopupKit.Rem).ToString("0.00") + "rem");
                // 종전 꼴(= 카드 안쪽 폭의 40%)이면 이 줄이 깨진다 — 하한의 1.51배였다.
                Assert.Less(w, min * 1.4f, n + " 알약이 정본 하한보다 한참 넓다(고정 폭을 쓰고 있다) · 실측 " + (w / PopupKit.Rem).ToString("0.00") + "rem");
                TMPro.TextMeshProUGUI lbl = pill.Find("amt").GetComponent<TMPro.TextMeshProUGUI>();
                Assert.IsNotNull(lbl, n + " 알약 숫자");
                Assert.LessOrEqual(lbl.preferredWidth, lbl.rectTransform.rect.width + 0.5f, n + " 알약 숫자가 잘린다 — 하한은 «최소» 지 «폭» 이 아니다");
                seen++; total += w;
            }
            Assert.AreEqual(2, seen, "코인·젬 두 알약");
            // ⚠ 줄의 «가운데 정렬»·틈(.8rem)은 **T364 축**이라 여기서 안 잰다 — 이 칸이 보는 것은 하한 하나다.
            RectTransform c = (RectTransform)pills.Find("coin"), g = (RectTransform)pills.Find("gem");
            Debug.Log("[T388] 알약 " + (c.rect.width / PopupKit.Rem).ToString("0.00") + "rem · " + (g.rect.width / PopupKit.Rem).ToString("0.00") + "rem (하한 5.5 · 종전 8.31) · 줄 폭 " + (total / PopupKit.Rem).ToString("0.00") + "rem");

            h.Meta.Popups.Hide(ForgeInfoPopup.Name);
            yield return null;
        }
    }
}
