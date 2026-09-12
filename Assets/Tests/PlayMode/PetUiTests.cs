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
    /// T20 — 소환 시트(스킬·펫 서브탭)가 실제 세이브·Core 위에서 서고, 소환 → 결과 연출 → 상세 → 장착/부화/젬 스킵/업그레이드 팝업이 열리며 콘솔 빨강 0 인가(빨간 로그는 러너가 실패시킨다).
    /// 글자는 전부 UiKit 표식 + 종류 하한 이상(T18 TextSizeGate 규칙을 이 화면들에도 적용). 디스크 세이브는 끈다(SuppressSave).
    /// </summary>
    public class PetUiTests
    {
        static IEnumerator Boot()
        {
            PetSkillHost.SuppressSave = true;
            PetSkillHost.Seed = 20260912;
            SceneManager.LoadScene("SampleScene");
            for (int i = 0; i < 600 && SkillPetSheet.Instance == null; i++) yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다(SaveIo/PetSkillHost 부팅)");
            Assert.IsTrue(PetSkillHost.Ready);
            yield return null;
        }

        static SkillPetSheet Sheet { get { return SkillPetSheet.Instance; } }
        static PetSkillHost Host { get { return PetSkillHost.Instance; } }

        static void OpenSummon()
        {
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
        }

        static void AssertTextGate(string where)
        {
            UiCatalog cat = UiCatalog.Instance;
            int seen = 0;
            foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            {
                if (!t.gameObject.activeInHierarchy) continue;
                seen++;
                UiTextKindTag tag = t.GetComponent<UiTextKindTag>();
                Assert.IsNotNull(tag, where + ": " + t.name + " 은 UiKit.Text 를 거치지 않았다");
                Assert.GreaterOrEqual(t.fontSize, cat.Kind(tag.Kind).min, where + ": " + t.name + " 글자 " + t.fontSize + " < 하한");
            }
            Assert.Greater(seen, 0, where + ": 활성 글자가 없다");
        }

        [UnityTest]
        public IEnumerator 소환_시트에_서브탭_셋과_스킬_펫_패널이_선다()
        {
            yield return Boot();
            OpenSummon();
            yield return null;
            Assert.AreEqual(SkillPetSheet.SubSkills, Sheet.ActiveSub, "원작 첫 서브탭 = 스킬");
            Assert.IsTrue(Sheet.SubVisible(SkillPetSheet.SubSkills));
            Assert.IsFalse(Sheet.SubVisible(SkillPetSheet.SubPets));
            StringAssert.StartsWith("스킬 ", Sheet.Skills.Title);
            Assert.IsNotNull(Sheet.Skills.SummonButton);
            Assert.IsNotNull(Sheet.Skills.UpgradeAllButton);
            Assert.IsNotNull(Sheet.Skills.QuickEquipButton);
            Assert.IsNotNull(Sheet.Skills.RatesButton);
            Assert.GreaterOrEqual(Sheet.Skills.GridCells, 1, "새 게임은 표창 난무 1개 보유");
            AssertTextGate("스킬 패널");

            Sheet.SubButton(SkillPetSheet.SubPets).onClick.Invoke();
            yield return null;
            Assert.AreEqual(SkillPetSheet.SubPets, Sheet.ActiveSub);
            Assert.IsTrue(Sheet.SubVisible(SkillPetSheet.SubPets));
            Assert.IsFalse(Sheet.SubVisible(SkillPetSheet.SubSkills));
            StringAssert.StartsWith("펫 ", Sheet.Pets.Title);
            Assert.IsNotNull(Sheet.Pets.SummonButton);
            Assert.IsNotNull(Sheet.Pets.BackButton, "부화장 ◀");
            AssertTextGate("펫 패널");

            Sheet.SubButton(SkillPetSheet.SubTech).onClick.Invoke();
            yield return null;
            Assert.IsTrue(Sheet.SubVisible(SkillPetSheet.SubTech), "기술 트리 자리(T21)");

            UiRoot.Instance.TabBar.OnTab("summon");
            yield return null;
            Assert.IsNull(UiRoot.Instance.TabBar.ActiveTab, "다시 누르면 홈");
        }

        [UnityTest]
        public IEnumerator 스킬_소환은_결과_연출을_열고_스킵하면_확인이_뜬다()
        {
            yield return Boot();
            OpenSummon();
            Sheet.Switch(SkillPetSheet.SubSkills);
            yield return null;
            while (Host.SummonMult("skill") != 1) Host.CycleSummonMult("skill");
            Host.Tickets = 10000;
            Host.Sync();
            yield return null;
            int before = Host.Skills.State.SummonCount;
            double tickets = Host.Tickets;
            double cost = Host.Skills.TicketCost(1);
            Sheet.Skills.SummonButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(before + 1, Host.Skills.State.SummonCount, "x1 소환 = 굴림 1");
            Assert.AreEqual(tickets - cost, Host.Tickets, 1e-9, "티켓 선결제");
            Assert.IsTrue(Sheet.Modal.IsOpen(SkillSummonResultView.ModalName), "결과 연출 팝업");
            SkillSummonResultView v = SkillSummonResultView.Current;
            Assert.IsNotNull(v);
            Assert.AreEqual(1, v.CellCount);
            Assert.IsFalse(v.Done);
            v.OnTap();
            yield return null;
            Assert.IsTrue(v.Done, "탭 = 스킵 → 완료");
            Assert.AreEqual(1, v.OnCount);
            Assert.IsTrue(v.OkButton.gameObject.activeInHierarchy, "[확인]");
            Assert.IsNotNull(v.AgainButton, "x1 은 [다시 소환]");
            AssertTextGate("소환 결과");
            v.OkButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(Sheet.Modal.IsOpen(SkillSummonResultView.ModalName));

            // 대량(x25) — 11개부터 묶음 · 스킵 시 전부 켜진다
            while (Host.SummonMult("skill") != 25) Host.CycleSummonMult("skill");
            yield return null;
            Sheet.Skills.SummonButton.onClick.Invoke();
            yield return null;
            v = SkillSummonResultView.Current;
            Assert.IsNotNull(v, "x25 결과");
            Assert.LessOrEqual(v.CellCount, 18, "같은 스킬은 한 셀로 묶인다(스킬 18종)");
            v.OnTap();
            yield return null;
            Assert.AreEqual(v.CellCount, v.OnCount);
            v.OnTap();
            yield return null;
            Assert.IsNull(SkillSummonResultView.Current, "끝난 뒤 탭 = 닫기");
            while (Host.SummonMult("skill") != 1) Host.CycleSummonMult("skill");
        }

        [UnityTest]
        public IEnumerator 스킬_상세에서_장착을_토글하고_확률_팝업이_넘긴다()
        {
            yield return Boot();
            OpenSummon();
            Sheet.Switch(SkillPetSheet.SubSkills);
            yield return null;
            string id = Host.Skills.State.Skills.KeyAt(0);
            Sheet.Skills.OpenSkillDetail(id);
            yield return null;
            Assert.IsTrue(Sheet.Modal.IsOpen(SkillPanel.DetailModal));
            AssertTextGate("스킬 상세");
            bool was = Host.Skills.State.Equipped.Contains(id);
            PetSkillModal.Handle h = Sheet.Modal.Find(SkillPanel.DetailModal);
            var eq = h.Content.Find("btn-equip").GetComponent<UnityEngine.UI.Button>();
            eq.onClick.Invoke();
            yield return null;
            Assert.AreNotEqual(was, Host.Skills.State.Equipped.Contains(id), "장착 토글");
            eq = Sheet.Modal.Find(SkillPanel.DetailModal).Content.Find("btn-equip").GetComponent<UnityEngine.UI.Button>();
            eq.onClick.Invoke();
            yield return null;
            Assert.AreEqual(was, Host.Skills.State.Equipped.Contains(id), "되돌림");
            Sheet.Modal.Find(SkillPanel.DetailModal).XButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(Sheet.Modal.IsOpen(SkillPanel.DetailModal));

            Sheet.Skills.RatesButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(Sheet.Modal.IsOpen(SkillRatesPopup.ModalName));
            int lv = SkillRatesPopup.LevelNow;
            SkillRatesPopup.NextButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(Mathf.Min(lv + 1, 100), SkillRatesPopup.LevelNow, "▶ 는 레벨 +1");
            AssertTextGate("확률 팝업");
            Sheet.Modal.Close(SkillRatesPopup.ModalName);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 펫_소환_알_부화_젬스킵_상세_업그레이드_팝업()
        {
            yield return Boot();
            OpenSummon();
            Sheet.Switch(SkillPetSheet.SubPets);
            yield return null;
            while (Host.SummonMult("pet") != 1) Host.CycleSummonMult("pet");
            Host.EggCurrency = 100000;
            Host.Gems = 100000;
            Host.Sync();
            yield return null;
            int eggs = Host.Pets.State.Eggs.Count;
            Sheet.Pets.SummonButton.onClick.Invoke();
            yield return null;
            Assert.GreaterOrEqual(Host.Pets.State.Eggs.Count, eggs + 1, "알 +1(보너스 알은 더 될 수 있다)");
            Assert.IsTrue(Sheet.Modal.IsOpen(SkillSummonResultView.ModalName));
            SkillSummonResultView.Current.OnTap();
            yield return null;
            SkillSummonResultView.Current.OnTap();
            yield return null;
            Assert.IsFalse(Sheet.Modal.IsOpen(SkillSummonResultView.ModalName));

            // 알 상세 → [부화]
            int hatching = Host.Pets.State.Hatching.Count;
            Assert.Less(hatching, Host.Pets.MaxHatchSlots(), "부화장에 자리가 있어야 한다");
            Sheet.Pets.OpenEggDetail(0);
            yield return null;
            Assert.IsTrue(Sheet.Modal.IsOpen(PetPanel.DetailModal));
            AssertTextGate("알 상세");
            Sheet.Modal.Find(PetPanel.DetailModal).Content.Find("btn-hatch").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.AreEqual(hatching + 1, Host.Pets.State.Hatching.Count, "부화 시작");
            Assert.IsFalse(Sheet.Modal.IsOpen(PetPanel.DetailModal), "상세는 닫힌다");
            Assert.IsNotNull(Sheet.Pets.SkipButton(hatching), "부화 칸의 💎 스킵");

            // 젬 스킵 → 펫 +1
            int pets = Host.Pets.State.Pets.Count;
            Sheet.Pets.SkipButton(hatching).onClick.Invoke();
            yield return null;
            Assert.AreEqual(pets + 1, Host.Pets.State.Pets.Count, "즉시 부화");
            Assert.Greater(Sheet.Modal.ToastCount, 0, "부화 토스트");

            // 펫 상세 → 업그레이드 팝업
            int last = Host.Pets.State.Pets.Count - 1;
            Sheet.Pets.OpenPetDetail(last);
            yield return null;
            Assert.IsTrue(Sheet.Modal.IsOpen(PetPanel.DetailModal));
            AssertTextGate("펫 상세");
            Sheet.Modal.Find(PetPanel.DetailModal).Content.Find("btn-upgrade").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.IsTrue(Sheet.Modal.IsOpen(PetUpgradePopup.ModalName), "업그레이드 팝업");
            Assert.AreEqual(last, PetUpgradePopup.Target);
            AssertTextGate("펫 업그레이드");
            if (Host.Pets.State.Eggs.Count > 0)
            {
                PetUpgradePopup.ToggleMat(true, 0);
                yield return null;
                Assert.AreEqual(1, PetUpgradePopup.SelectedCount, "알 재료 선택");
                double xp = Host.Pets.State.Pets[last].Xp;
                int lv = Host.Pets.State.Pets[last].Level;
                int eggN = Host.Pets.State.Eggs.Count;
                PetUpgradePopup.Confirm();
                yield return null;
                Assert.AreEqual(eggN - 1, Host.Pets.State.Eggs.Count, "재료 알 소모");
                Assert.IsTrue(Host.Pets.State.Pets[last].Xp > xp || Host.Pets.State.Pets[last].Level > lv, "경험치 흡수");
            }
            PetUpgradePopup.Close();
            yield return null;
            Assert.IsFalse(Sheet.Modal.IsOpen(PetUpgradePopup.ModalName));

            // 출전 토글(상세 [제거]/[장착])
            bool active = Host.Pets.State.ActivePets.Contains(last);
            Sheet.Pets.OnTogglePet(last);
            yield return null;
            Assert.AreNotEqual(active, Host.Pets.State.ActivePets.Contains(last));
            Sheet.Pets.OnTogglePet(last);
            yield return null;
            Assert.AreEqual(active, Host.Pets.State.ActivePets.Contains(last));

            // 시트를 닫으면 모달 전부 닫힘
            Sheet.Pets.OpenPetDetail(last);
            yield return null;
            UiRoot.Instance.TabBar.OnTab("summon");
            yield return null;
            Assert.AreEqual(0, Sheet.Modal.OpenCount, "시트 닫힘 = 모달 전부 닫힘(원작 closeAllTabSurfaces)");
        }
    }
}
