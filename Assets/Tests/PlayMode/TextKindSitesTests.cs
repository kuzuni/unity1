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

        [TearDown]
        public void CleanSave()
        {
            PetSkillHost.SuppressSave = false;
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
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
        /// <summary>T391 4회차 — 남은 셋 중 파일이 열린 둘: 확률 팝업 머리(정본 4580 `.rates-head h3` 1.3rem = 47.3px → Head 48) ·
        /// 자동 제련 제목(정본 4695 `.af-title` 1.12rem · 5033 덮음 1.26rem = 45.9px → Button 44). 둘 다 전엔 Title 60 이었다.</summary>
        /// <summary>T404 2회차 — 모달 제목 단 Title2(1.12~1.2rem ≈ 42px)에 드는 남은 둘: «모든 장비의 목록»(ui.js 2125 `&lt;h3 class="fi-title"&gt;` · 5056 1.12rem) ·
        /// 오프라인 합계 수 둘(`.offline-total` 305 1.15rem · 전엔 Sub 36 이라 −14%). 1회차의 다섯 자리와 같은 단.</summary>
        [UnityTest]
        public IEnumerator 장비_목록_제목과_오프라인_합계는_모달_제목_단으로_선다()
        {
            yield return Boot();
            float t0 = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready) && t0 < 20f) { t0 += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost"); Assert.IsTrue(MetaHost.Ready, "MetaHost");
            float t2 = UiCatalog.Instance.Kind(TextKind.Title2).size;

            ForgeHost fh = ForgeHost.Instance;
            ForgeInfoPopup.OpenList(fh);
            yield return null; yield return null;
            Popup lp = fh.Meta.Popups.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(lp, "「모든 장비의 목록」 팝업");
            TextMeshProUGUI lt = null;
            foreach (TextMeshProUGUI t in lp.Root.GetComponentsInChildren<TextMeshProUGUI>(true)) if (t.name == "title" && t.text == "모든 장비의 목록") { lt = t; break; }
            Assert.IsNotNull(lt, "목록 제목");
            Assert.AreEqual(t2, lt.fontSize, 0.01f, "정본 <h3 class=fi-title> 1.12rem → Title2(전엔 Title 60)");
            fh.Meta.Popups.Hide(ForgeInfoPopup.Name);
            yield return null;

            MetaHost mh = MetaHost.Instance;
            OfflinePopup.Show(mh, new Forge.Core.Save.OfflineReward { Elapsed = 5000, Counted = 3600, Coins = 8870, Hammers = 149.05, CoinRate = 1.13, HammerRate = 1.14 });
            yield return null; yield return null;
            Popup op = mh.Popups.Find(OfflinePopup.Name);
            Assert.IsNotNull(op, "오프라인 보상 팝업");
            int seen = 0;
            foreach (TextMeshProUGUI t in op.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.name == "coins" || t.name == "hammers") { Assert.AreEqual(t2, t.fontSize, 0.01f, "정본 .offline-total 1.15rem → Title2(전엔 Sub 36) · " + t.name); seen++; }
            Assert.AreEqual(2, seen, "합계 수 둘(coins · hammers)");
            mh.Popups.Hide(OfflinePopup.Name);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 확률_머리와_자동_제련_제목은_정본_크기_단으로_선다()
        {
            PetSkillHost.SuppressSave = true;
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            yield return Boot();
            float t0 = 0f;
            while (!(ForgeHost.Ready && PetSkillHost.Ready && SkillPetSheet.Instance != null) && t0 < 20f) { t0 += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost"); Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트");
            float head = UiCatalog.Instance.Kind(TextKind.Head).size, btn = UiCatalog.Instance.Kind(TextKind.Button).size;

            SkillRatesPopup.Open(SkillPetSheet.Instance, "pet");
            yield return null; yield return null;
            Assert.IsTrue(SkillPetSheet.Instance.Modal.IsOpen(SkillRatesPopup.ModalName), "확률 팝업이 열린다");
            TextMeshProUGUI h3 = null;
            foreach (TextMeshProUGUI t in UiRoot.Instance.App.GetComponentsInChildren<TextMeshProUGUI>(true)) if (t.name == "rates-h3") { h3 = t; break; }
            Assert.IsNotNull(h3, "확률 머리 h3");
            Assert.AreEqual(head, h3.fontSize, 0.01f, "정본 4580 .rates-head h3 1.3rem → Head(전엔 Title 60)");
            SkillPetSheet.Instance.Modal.Close(SkillRatesPopup.ModalName);
            yield return null;

            // 자동 제련은 2-10 뒤에만 열린다(ForgeCardWidthTests 와 같은 길로 해금)
            ForgeHost fh = ForgeHost.Instance;
            fh.S.BestChapter = 3; fh.S.BestStage = 1; fh.Pull();
            Assert.IsTrue(fh.AutoForgeUnlocked, "2-10 뒤 해금");
            ForgeAutoPopup.Open(fh);
            yield return null;
            Popup p = fh.Meta.Popups.Find(ForgeAutoPopup.Name);
            Assert.IsNotNull(p, "자동 제련 팝업");
            TextMeshProUGUI af = null;
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true)) if (t.name == "af-title") { af = t; break; }
            Assert.IsNotNull(af, "자동 제련 제목");
            Assert.AreEqual(btn, af.fontSize, 0.01f, "정본 5033 .af-title 1.26rem = 45.9 → Button 44(전엔 Title 60)");
            ForgeAutoPopup.Close(fh);
            yield return null;
        }
        /// <summary>T404 3회차 — 여덟 중 마지막: 판매 경고 제목(정본 2219 `.sellwarn-title { 1.15rem }` = 41.9px → Title2 42 · 전엔 Body 40).</summary>
        [UnityTest]
        public IEnumerator 판매_경고_제목은_모달_제목_단으로_선다()
        {
            yield return Boot();
            float t0 = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready) && t0 < 20f) { t0 += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost");
            ForgeHost fh = ForgeHost.Instance;
            ForgeCraftPopup.ShowSellConfirm(fh, fh.Engine.RollItem(), fh.Engine.RollItem());
            yield return null;
            Popup p = fh.Meta.Popups.Find(ForgeCraftPopup.SellName);
            Assert.IsNotNull(p, "판매 경고");
            TextMeshProUGUI title = null;
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true)) if (t.name == "title" && t.text == "정말 판매할까요?") { title = t; break; }
            Assert.IsNotNull(title, "판매 경고 제목");
            Assert.AreEqual(UiCatalog.Instance.Kind(TextKind.Title2).size, title.fontSize, 0.01f, "정본 2219 .sellwarn-title 1.15rem → Title2(전엔 Body 40)");
            fh.Meta.Popups.Hide(ForgeCraftPopup.SellName);
            yield return null;
        }

        /// <summary>T391 5회차 — 자의 마지막 KNOWN: x1 소환 결과의 이름(정본 7084 `.sr-grid.one .sr-name { 1.25rem }` = 45.5px → Button 44 · 여태 Sub 36 으로 −21%).
        /// 여럿 뽑은 판은 정본이 기본 `.sr-name`(.7rem)이라 하한 `Sub` 가 맞다 — **한 개 판에서만** 한 단 크다.</summary>
        [UnityTest]
        public IEnumerator x1_소환_결과의_이름만_한_단_크다()
        {
            PetSkillHost.SuppressSave = true;
            PetSkillHost.Seed = 20260916;
            SceneManager.LoadScene("SampleScene");
            yield return null; yield return null;
            Scene active = SceneManager.GetActiveScene();
            for (int i = 0; i < 600 && !(SkillPetSheet.Instance != null && SkillPetSheet.Instance.gameObject.scene == active && PetSkillHost.Ready); i++) yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트");
            Assert.IsTrue(PetSkillHost.Ready);
            yield return null;

            float one = NameSize(1);
            Assert.AreEqual(UiCatalog.Instance.Kind(TextKind.Button).size, one, 0.01f, "정본 7084 .sr-grid.one .sr-name 1.25rem = 45.5px → Button 44");
            float many = NameSize(3);
            Assert.AreEqual(UiCatalog.Instance.Kind(TextKind.Sub).size, many, 0.01f, "여럿 판은 정본 기본 .sr-name(.7rem) — 하한 Sub 가 맞다");
            Assert.Greater(one, many, "한 개 판이 한 단 크다");
        }

        /// <summary>소환 결과를 <paramref name="n"/> 개로 열고 첫 셀 이름 글자의 크기를 돌려준다(창은 닫는다).</summary>
        static float NameSize(int n)
        {
            var list = new System.Collections.Generic.List<SkillSummonResultView.Entry>();
            for (int i = 0; i < n; i++)
                list.Add(new SkillSummonResultView.Entry { Key = "sk:" + i, IconKey = "sk_fireball", Rarity = "common", Name = "화살비" });
            SkillSummonResultView v = SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", list, "common", null);
            Assert.IsNotNull(v, "소환 결과 창(" + n + "개)이 안 열렸다");
            Canvas.ForceUpdateCanvases();
            foreach (Transform x in v.GetComponentsInChildren<Transform>(true))
                if (x.name == "sr-name")
                {
                    Transform t = x.Find("t");
                    Assert.IsNotNull(t, "이름판 안의 글자");
                    float size = t.GetComponent<TextMeshProUGUI>().fontSize;
                    Object.Destroy(v.gameObject);
                    return size;
                }
            Object.Destroy(v.gameObject);
            Assert.Fail("이름판(sr-name)을 못 찾았다 — " + n + "개 판");
            return 0f;
        }
    }
}
