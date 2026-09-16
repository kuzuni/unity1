using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Core.Forging;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T383 2회차 — 정본 `ui.js` 가 `<br>` 로 줄 수를 못박은 자리(`tools/check_br_lines.py` 의 표 22 → 클론 자리 21)가 **화면에서** 그 줄 수로 서는가.
    /// 자(파이썬)는 코드가 줄 수를 아는지만 본다 — 하한(Sub 36)이 한 번 더 접는 꼴(런 666 pass-desc 세 줄)은 여기서만 보인다.
    /// 한 상자(`\n`)는 <c>textInfo.lineCount</c> · 아이콘 줄 갈래(<see cref="IconTextStack"/>)는 «line-N» 행 수 = 정본 줄 수이고 행마다 한 줄.
    /// 새 세이브로 열리는 자리부터(펫·탈것 상세 · 도전 행 · 판매 경고 밖 자리는 다음 회차).
    /// </summary>
    public class BrLinesTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && DungeonUiHost.Ready && UiRoot.Instance != null && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready && MetaHost.Ready && DungeonUiHost.Ready, "호스트 셋이 20초 안에 준비되지 않았다");
            yield return null;
        }

        static Transform Find(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        static int Lines(TextMeshProUGUI t)
        {
            Canvas.ForceUpdateCanvases();
            t.ForceMeshUpdate();
            return t.textInfo.lineCount;
        }

        /// <summary>아이콘 줄 갈래: «line-N» 행 수와 행마다 한 줄인지.</summary>
        static int StackRows(Transform stack, string what)
        {
            int rows = 0;
            for (int i = 0; i < stack.childCount; i++)
            {
                Transform c = stack.GetChild(i);
                if (!c.name.StartsWith("line-")) continue;
                rows++;
                foreach (TextMeshProUGUI t in c.GetComponentsInChildren<TextMeshProUGUI>(true))
                    if (t.gameObject.activeInHierarchy && !string.IsNullOrEmpty(t.text)) Assert.AreEqual(1, Lines(t), what + ": 행 «" + c.name + "» 의 글 «" + t.text + "» 은 한 줄");
            }
            return rows;
        }

        /// <summary>정본 1511 «대장간<br>레벨 N»(새 세이브 = 업그레이드 가능) · 1551 «자동<br>OFF/🔒» — 장비 시트 두 버튼.</summary>
        [UnityTest]
        public IEnumerator 장비_시트_대장간_버튼과_자동_버튼은_정본대로_두_줄이다()
        {
            yield return Boot();
            Transform fb = Find(UiRoot.Instance.Sheet.transform, "forge-btn");
            Assert.IsNotNull(fb, "대장간 버튼(forge-btn)");
            TextMeshProUGUI ft = fb.GetComponentInChildren<TextMeshProUGUI>(true);
            Assert.IsNotNull(ft, "대장간 버튼 글");
            Assert.IsTrue(ft.text.Contains("\n"), "코드가 두 줄을 안다: " + ft.text);
            Assert.AreEqual(2, Lines(ft), "정본 1506·1507·1511 `<br>` = 두 줄 — 실제 «" + ft.text.Replace("\n", "⏎") + "»");
            Transform stack = Find(UiRoot.Instance.Sheet.transform, "label-stack");
            Assert.IsNotNull(stack, "자동 버튼의 줄 갈래(label-stack)");
            Assert.AreEqual(2, StackRows(stack, "자동 버튼"), "정본 1551 «자동<br>…» = 두 행");
        }

        /// <summary>정본 2047 «레벨 N 업그레이드<br><small>코인 · 시간</small>» — 대장간 정보 팝업의 업그레이드 버튼(새 세이브 = 업그레이드 가능 갈래).</summary>
        [UnityTest]
        public IEnumerator 대장간_정보_업그레이드_버튼은_정본대로_두_행이다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeInfoPopup.Open(h);
            yield return null;
            Popup p = PopupLayer.Instance.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(p, "대장간 정보 팝업");
            Transform up = Find(p.Root, "fi-upgrade");
            Assert.IsNotNull(up, "업그레이드 버튼(fi-upgrade)");
            Transform stack = Find(up, "label-stack");
            Assert.IsNotNull(stack, "업그레이드 버튼의 줄 갈래");
            Assert.AreEqual(2, StackRows(stack, "업그레이드 버튼"), "정본 2047 = 두 행");
            PopupLayer.Instance.Hide(ForgeInfoPopup.Name);
            yield return null;
        }

        /// <summary>정본 4678 «이전 스테이지<br>소탕» — 던전 상세 소탕 버튼.</summary>
        [UnityTest]
        public IEnumerator 던전_상세_소탕_버튼은_정본대로_두_줄이다()
        {
            yield return Boot();
            // 런 688: 새 세이브는 망치 던전이 잠겨 있어 Open 이 토스트만 하고 돌아온다(SweepButton null) — 해금 상태를 먼저 만든다(DropShadowTests 의 길).
            ForgeHost fh = ForgeHost.Instance;
            fh.S.BestChapter = 5; fh.S.BestStage = 1; fh.Pull();
            UiRoot.Instance.TabBar.OnTab("dungeon");
            yield return null;
            DungeonDetailPopup.Open("hammer");
            yield return null;
            Assert.IsTrue(DungeonDetailPopup.IsOpen, "던전 상세가 열린다(해금 뒤)");
            Assert.IsNotNull(DungeonDetailPopup.SweepButton, "소탕 버튼");
            TextMeshProUGUI t = DungeonPopups.Root(DungeonDetailPopup.SweepButton).GetComponentInChildren<TextMeshProUGUI>(true);
            Assert.IsNotNull(t, "소탕 버튼 글");
            Assert.AreEqual(2, Lines(t), "정본 4678 = 두 줄 — 실제 «" + t.text.Replace("\n", "⏎") + "»");
            DungeonDetailPopup.Close();
            yield return null;
        }

        /// <summary>정본 4928 «전투를 진행하여 보상을 받<br>으세요!» — 패스 안내문 두 줄(9회차: Micro + 표 크기 .78rem · 정본 2734).</summary>
        [UnityTest]
        public IEnumerator 패스_안내문은_정본대로_두_줄이다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            PassPopup.Open(h);
            yield return null;
            Popup p = PopupLayer.Instance.Find(PassPopup.Name);
            Assert.IsNotNull(p, "패스 팝업");
            Transform d = Find(p.Root, "desc");
            Assert.IsNotNull(d, "안내 글(desc)");
            TextMeshProUGUI t = d.GetComponent<TextMeshProUGUI>();
            // 런 666 은 **세 줄**이었다(하한 Sub 36 이 「보상을 받」 을 반 칸에서 한 번 더 접었다) — 9회차가 종류 Micro + 표 크기(정본 .78rem · TextSizeUi)로 고쳐 정본 두 줄로 돌아온다.
            Assert.AreEqual(TextSizeUi.Px("pass_desc"), t.fontSize, 0.5f, "크기는 표(정본 .78rem)에서 — 하한 36 이 아니다");
            Assert.AreEqual(2, Lines(t), "정본 4928 = 두 줄 — 실제 «" + t.text.Replace("\n", "⏎") + "»");
            PopupLayer.Instance.Hide(PassPopup.Name);
            yield return null;
        }

        /// <summary>정본 4812 «…유지하면 시즌 종료 시<br>다음 보상을 받을 수 있습니다:» — 리그 보상 안내.</summary>
        [UnityTest]
        public IEnumerator 리그_보상_안내는_정본대로_두_줄이다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            LeagueSheet.OpenRewards(h);
            yield return null;
            Popup p = PopupLayer.Instance.Find(LeagueSheet.RewardsName);
            Assert.IsNotNull(p, "리그 보상 팝업");
            Transform d = Find(p.Root, "desc");
            Assert.IsNotNull(d, "안내 글(desc)");
            TextMeshProUGUI t = d.GetComponent<TextMeshProUGUI>();
            Assert.AreEqual(2, Lines(t), "정본 4812 = 두 줄 — 실제 «" + t.text.Replace("\n", "⏎") + "»");
            PopupLayer.Instance.Hide(LeagueSheet.RewardsName);
            yield return null;
        }

        /// <summary>정본 5848~5849 «…됩니다<br>· ⚠ 보유 중인 기존 …<br>· 이후 새로 …» — 승천 초점 카드 효과 글줄 세 줄(5회차: Micro + 표 크기 .76rem).</summary>
        [UnityTest]
        public IEnumerator 승천_효과_글줄은_정본대로_세_줄이다()
        {
            yield return Boot();
            AscendPopup.Open("forge");
            yield return null;
            Transform eff = Find(AscendPopup.Root, "eff");
            Assert.IsNotNull(eff, "효과 글줄(eff)");
            TextMeshProUGUI t = eff.GetComponent<TextMeshProUGUI>();
            int n = Lines(t);
            // 런 688 은 **4줄**이었다(하한 Sub 36 이 셋째 항목을 접었다) — 5회차가 종류 Micro + 표 크기(정본 .76rem · TextSizeUi)로 고쳐 정본 세 줄로 돌아온다.
            Assert.AreEqual(3, n, "정본 5848·5849 = 세 줄 — 실제 «" + t.text.Replace("\n", "⏎") + "»");
            Assert.AreEqual(TextSizeUi.Px("asc_focus_eff"), t.fontSize, 0.5f, "크기는 표(정본 .76rem)에서 — 하한 36 이 아니다");
            AscendPopup.Close();
            yield return null;
        }

        /// <summary>정본 3863 «…더 최신입니다.<br>같거나 이전 시대면 …» — 판매 경고 안내.</summary>
        [UnityTest]
        public IEnumerator 판매_경고_안내는_정본대로_두_줄이다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeItem a = h.Engine.RollItem(), b = h.Engine.RollItem();
            ForgeCraftPopup.ShowSellConfirm(h, a, b);
            yield return null;
            Popup p = PopupLayer.Instance.Find(ForgeCraftPopup.SellName);
            Assert.IsNotNull(p, "판매 경고 팝업");
            Transform n = Find(p.Root, "note");
            Assert.IsNotNull(n, "안내 글(note)");
            TextMeshProUGUI t = n.GetComponent<TextMeshProUGUI>();
            Assert.AreEqual(2, Lines(t), "정본 3863 = 두 줄 — 실제 «" + t.text.Replace("\n", "⏎") + "»");
            PopupLayer.Instance.Hide(ForgeCraftPopup.SellName);
            yield return null;
        }

        // ── T383 7회차 — 상태를 만들어야 열리는 자리(새 세이브 + 소환 한 번으로 서는 아홉) ─────────────────────────────
        // 정본이 한 요소 안에 `<br>` 로 두 줄을 못박은 자리를 클론은 **두 상자**로 가른 곳이 많다(자 표의 kind «split»).
        // 그 자리의 계약은 «각 상자가 한 줄이고 위아래로 선다» 다 — 한 상자가 접혀 두 줄이 되면 정본의 두 줄이 세 줄이 된다.

        static TextMeshProUGUI TextIn(Transform root, string name, string what)
        {
            Transform t = Find(root, name);
            Assert.IsNotNull(t, what + ": «" + name + "» 상자가 있다");
            TextMeshProUGUI tm = t.GetComponent<TextMeshProUGUI>() ?? t.GetComponentInChildren<TextMeshProUGUI>(true);
            Assert.IsNotNull(tm, what + ": «" + name + "» 안에 글자가 있다");
            return tm;
        }

        /// <summary>«위 상자 한 줄 + 아래 상자 한 줄» = 정본 `<br>` 두 줄.</summary>
        static void SplitTwoLines(Transform root, string top, string bottom, string what)
        {
            TextMeshProUGUI a = TextIn(root, top, what), b = TextIn(root, bottom, what);
            Assert.AreEqual(1, Lines(a), what + ": «" + a.text + "» 은 한 줄");
            Assert.AreEqual(1, Lines(b), what + ": «" + b.text + "» 은 한 줄");
            Assert.Greater(a.rectTransform.position.y, b.rectTransform.position.y, what + ": «" + top + "» 이 «" + bottom + "» 위에 선다(정본 <br> 의 위아래)");
        }

        /// <summary>옵션 줄(정본 `subs.join('<br>')` · 줄마다 한 줄) — 없으면 «옵션 없음» 한 줄.</summary>
        static void SubLines(Transform root, string what)
        {
            int n = 0;
            for (int k = 0; k < 8; k++)
            {
                Transform t = Find(root, "petd-sub-" + k);
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                n++;
                Assert.AreEqual(1, Lines(t.GetComponent<TextMeshProUGUI>()), what + ": 옵션 줄 " + k + " 은 한 줄");
            }
            if (n == 0)
            {
                TextMeshProUGUI ns = TextIn(root, "petd-subs", what);
                Assert.AreEqual(1, Lines(ns), what + ": «옵션 없음» 은 한 줄");
            }
        }

        static IEnumerator PetReady()
        {
            for (int i = 0; i < 600 && !(PetSkillHost.Ready && SkillPetSheet.Instance != null && SkillBar.Instance != null); i++) yield return null;
            Assert.IsTrue(PetSkillHost.Ready, "PetSkillHost");
        }

        /// <summary>정본 4033 «피해 N<br>체력 N»(펫 상세 · split) · 4010 옵션 줄(`join('<br>')`) · 4199 업그레이드 머리 «피해<br>체력»(split).
        /// 새 세이브 → 알 소환 → 즉시 부화 → 펫 하나(LineHeightTests 의 길).</summary>
        [UnityTest]
        public IEnumerator 펫_상세와_업그레이드_머리의_피해_체력은_정본대로_위아래_한_줄씩이다()
        {
            yield return Boot();
            yield return PetReady();
            PetSkillHost host = PetSkillHost.Instance;
            SkillPetSheet sheet = SkillPetSheet.Instance;
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            yield return null;
            sheet.Switch(SkillPetSheet.SubPets);
            yield return null;
            while (host.SummonMult("pet") != 1) host.CycleSummonMult("pet");
            host.EggCurrency = 100000; host.Gems = 100000; host.Sync();
            yield return null;
            sheet.Pets.SummonButton.onClick.Invoke();
            yield return null;
            for (int k = 0; k < 4 && SkillSummonResultView.Current != null; k++) { SkillSummonResultView.Current.OnTap(); yield return null; }
            Assert.GreaterOrEqual(host.Pets.State.Eggs.Count, 1, "x1 소환 = 알 하나 이상");
            int hatching = host.Pets.State.Hatching.Count;
            sheet.Pets.OpenEggDetail(0);
            yield return null;
            sheet.Modal.Find(PetPanel.DetailModal).Content.Find("btn-hatch").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.IsNotNull(sheet.Pets.SkipButton(hatching), "부화 칸의 스킵");
            sheet.Pets.SkipButton(hatching).onClick.Invoke();
            yield return null;
            Assert.AreEqual(1, host.Pets.State.Pets.Count, "즉시 부화 → 펫 하나");

            sheet.Pets.OpenPetDetail(0);
            yield return null;
            Transform detail = sheet.Modal.Find(PetPanel.DetailModal).Content;
            Assert.IsNotNull(detail, "펫 상세");
            SplitTwoLines(detail, "petd-atk", "petd-hp", "펫 상세(정본 4033)");
            SubLines(detail, "펫 상세 옵션(정본 4010)");

            detail.Find("btn-upgrade").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.IsTrue(sheet.Modal.IsOpen(PetUpgradePopup.ModalName), "업그레이드 팝업");
            Transform up = sheet.Modal.Find(PetUpgradePopup.ModalName).Content;
            SplitTwoLines(up, "idet-atk", "idet-hp", "펫 업그레이드 머리(정본 4199)");
            PetUpgradePopup.Close();
            yield return null;
        }

        /// <summary>정본 5704 «피해<br>체력»(탈것 상세 · split) · 5689 옵션 줄 · 5707 «옵션 없음 / 같은 종류 보유»(split · 있을 때만).</summary>
        [UnityTest]
        public IEnumerator 탈것_상세의_피해_체력과_옵션_줄은_정본대로_한_줄씩이다()
        {
            yield return Boot();
            yield return PetReady();
            PetSkillHost host = PetSkillHost.Instance;
            Assert.IsNotNull(host.Mounts, "탈것");
            while (host.SummonMult("mount") != 1) host.CycleSummonMult("mount");
            host.Winders = 100000; host.Sync();
            yield return null;
            MountSheet.Open();
            yield return null;
            Assert.IsTrue(MountSheet.IsOpen, "탈것 시트");
            MountSheet.SummonButton.onClick.Invoke();
            yield return null;
            for (int k = 0; k < 4 && SkillSummonResultView.Current != null; k++) { SkillSummonResultView.Current.OnTap(); yield return null; }
            Assert.GreaterOrEqual(host.Mounts.Count(), 1, "x1 소환 = 한 마리 이상");
            MountSheet.OpenDetail(0);
            yield return null;
            Transform detail = SkillPetSheet.Instance.Modal.Find(MountSheet.DetailModal).Content;
            Assert.IsNotNull(detail, "탈것 상세");
            SplitTwoLines(detail, "petd-atk", "petd-hp", "탈것 상세(정본 5704)");
            SubLines(detail, "탈것 상세 옵션(정본 5689)");
            Transform same = Find(detail, "petd-same");
            if (same != null && same.gameObject.activeInHierarchy)
                Assert.AreEqual(1, Lines(same.GetComponent<TextMeshProUGUI>()), "«같은 종류 보유» 는 한 줄(정본 5707)");
        }

        /// <summary>정본 4740 «이름<br>⚔ 전투력»(리그 행 · split) · 4859 «이름<br>전투력»(도전 행) · 4862 «도전<br><small>티켓»(도전 버튼 라벨).</summary>
        [UnityTest]
        public IEnumerator 리그_행과_도전_행의_이름_전투력은_정본대로_위아래_한_줄씩이다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            h.OpenLeague();
            yield return null;
            Popup lg = PopupLayer.Instance.Find(LeagueSheet.Name);
            Assert.IsNotNull(lg, "리그 시트");
            int rows = 0;
            foreach (Transform row in lg.Root.GetComponentsInChildren<Transform>(true))
            {
                if (!row.name.StartsWith("row-") || !row.gameObject.activeInHierarchy) continue;
                rows++;
                SplitTwoLines(row, "name", "cp", "리그 행 «" + row.name + "»(정본 4740)");
            }
            Assert.Greater(rows, 0, "리그 행이 있다");

            LeagueSheet.OpenChallenge(h);
            yield return null;
            Popup ch = PopupLayer.Instance.Find(LeagueSheet.ChallengeName);
            Assert.IsNotNull(ch, "도전 팝업");
            int crows = 0;
            foreach (Transform row in ch.Root.GetComponentsInChildren<Transform>(true))
            {
                if (!row.name.StartsWith("row") || !row.gameObject.activeInHierarchy || row.Find("cp-ico") == null) continue;
                crows++;
                // ⚑ 이 단언이 **가끔** 빨갛다면 자가 깨진 것이 아니다 — **T397** 이다.
                //   봇 이름은 `meta.json` `league.NAME_POOL`(20개)을 `League.GenBots` 가 섞어 나눠 주는데
                //   15자짜리 «BlandBuddy22667» 하나만 유독 길다(다음이 9자). 그것이 도전 목록에 드는 런에서만
                //   이름이 둘째 줄로 접힌다 — 정본 2632 `.league-challenge-name` 은 **.85rem(30.9px)** 인데
                //   클론은 하한 `TextKind.Sub`(36) 라 **1.164배**이기 때문이다(T136·T372·T383·T389 갈래).
                //   **이 줄을 느슨하게 고치지 마라** — 정본 `<br>` 이 못박은 모양 그대로다. 고칠 자리는 `LeagueSheet.cs` 다.
                SplitTwoLines(row, "name", "cp", "도전 행 «" + row.name + "»(정본 4859)");
                Transform btn = row.Find("challenge");
                Assert.IsNotNull(btn, "도전 버튼(정본 4862)");
                TextMeshProUGUI lab = TextIn(btn, "label", "도전 버튼");
                Assert.AreEqual(1, Lines(lab), "도전 버튼 라벨 «" + lab.text + "» 은 한 줄(정본 «도전<br><small>» 의 윗줄)");
            }
            Assert.Greater(crows, 0, "도전 상대 행이 있다");
            PopupLayer.Instance.Hide(LeagueSheet.ChallengeName);
            yield return null;
        }

        /// <summary>
        /// T383 8회차 — 상태가 필요한 자리 셋. 정본 1506 «⭐ 승천<br>가능»(만렙 = `!info` 이고 `Ascension.ready('forge')` — FORGE_LEVEL 35 = 만렙이라 만렙이면 곧 승천 가능) ·
        /// 2036 «승천<br><small>»(대장간 정보 · 같은 상태) · 2043 «건너뛰기<br>»(업그레이드 진행 중). 1507 «대장간<br>최고 레벨» 은 정본도 `!info && !ready` 인데
        /// FORGE_LEVEL(35) = 만렙(35) 이라 **정본에서도 닿지 않는 갈래**다 — 상태로 못 만들고 코드 글자만 자(`check_br_lines`)가 본다.
        /// </summary>
        [UnityTest]
        public IEnumerator 장비_시트_대장간_버튼은_만렙이면_정본대로_승천_가능_두_줄이다()
        {
            yield return Boot();
            ForgeHost fh = ForgeHost.Instance;
            fh.Forge.UpgradeEndsAt = null;
            fh.Forge.ForgeLevel = fh.Ascension.Table.ForgeLevel;   // 승천 도달 레벨 = 만렙(35) — DungeonUiTests 의 길
            Assert.IsNull(fh.UpgradeInfo(), "만렙이면 다음 업그레이드가 없다(정본 `!info`)");
            Assert.IsTrue(fh.AscendReady, "만렙이면 승천 가능(정본 Ascension.ready)");
            ForgeSheet.Render(fh);
            yield return null;
            Transform fb = Find(UiRoot.Instance.Sheet.transform, "forge-btn");
            Assert.IsNotNull(fb, "대장간 버튼(forge-btn)");
            TextMeshProUGUI ft = fb.GetComponentInChildren<TextMeshProUGUI>(true);
            Assert.IsNotNull(ft, "대장간 버튼 글");
            Assert.IsTrue(ft.text.StartsWith("★ 승천", System.StringComparison.Ordinal), "정본 1506 «⭐ 승천<br>가능» 갈래 — 실제 «" + ft.text.Replace("\n", "⏎") + "»");
            Assert.AreEqual(2, Lines(ft), "정본 1506 `<br>` = 두 줄 — 실제 «" + ft.text.Replace("\n", "⏎") + "»");
            fh.Forge.ForgeLevel = 1;
            ForgeSheet.Render(fh);
            yield return null;
        }

        /// <summary>정본 2036 «⭐ 승천<br><small>대장간 Lv.35 도달 · 이후 제작 장비 ⭐N</small>» — 대장간 정보 팝업의 승천 버튼(만렙 상태).</summary>
        [UnityTest]
        public IEnumerator 대장간_정보_승천_버튼은_정본대로_두_줄이다()
        {
            yield return Boot();
            ForgeHost fh = ForgeHost.Instance;
            fh.Forge.UpgradeEndsAt = null;
            fh.Forge.ForgeLevel = fh.Ascension.Table.ForgeLevel;
            Assert.IsTrue(fh.AscendReady, "승천 가능 상태");
            ForgeInfoPopup.Open(fh);
            yield return null;
            Popup p = PopupLayer.Instance.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(p, "대장간 정보 팝업");
            Transform asc = Find(p.Root, "fi-upgrade");
            Assert.IsNotNull(asc, "승천 버튼(fi-upgrade · 승천 갈래)");
            TextMeshProUGUI t = asc.GetComponentInChildren<TextMeshProUGUI>(true);
            Assert.IsNotNull(t, "승천 버튼 글");
            Assert.IsTrue(t.text.StartsWith("★ 승천", System.StringComparison.Ordinal), "정본 2036 승천 갈래 — 실제 «" + t.text.Replace("\n", "⏎") + "»");
            Assert.IsTrue(t.text.Contains("\n"), "코드가 두 줄을 안다: " + t.text);
            Assert.AreEqual(2, Lines(t), "정본 2036 `<br>` = 두 줄 — 실제 «" + t.text.Replace("\n", "⏎") + "»");
            PopupLayer.Instance.Hide(ForgeInfoPopup.Name);
            fh.Forge.ForgeLevel = 1;
            yield return null;
        }

        /// <summary>정본 2043 «건너뛰기<br><span class="fi-skip-gem">💎 N</span>» — 대장간 정보 팝업의 건너뛰기 버튼(업그레이드 진행 중 상태 · 아랫줄은 젬 아이콘 + 수 = 줄 갈래).</summary>
        [UnityTest]
        public IEnumerator 대장간_정보_건너뛰기_버튼은_정본대로_두_행이다()
        {
            yield return Boot();
            ForgeHost fh = ForgeHost.Instance;
            Assert.IsNotNull(fh.UpgradeInfo(), "새 세이브(Lv.1)는 다음 업그레이드가 있다");
            fh.Forge.UpgradeEndsAt = SaveIo.NowMs() + 60 * 60e3;   // 진행 중(IconTextStackTests ⓒ 의 길)
            Assert.IsTrue(fh.Upgrading, "업그레이드 진행 중 상태");
            ForgeInfoPopup.Open(fh);
            yield return null;
            Popup p = PopupLayer.Instance.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(p, "대장간 정보 팝업");
            Transform skip = Find(p.Root, "fi-skip");
            Assert.IsNotNull(skip, "건너뛰기 버튼(fi-skip)");
            Transform stack = Find(skip, "label-stack");
            Assert.IsNotNull(stack, "건너뛰기 버튼의 줄 갈래(label-stack)");
            Assert.AreEqual(2, StackRows(stack, "건너뛰기 버튼"), "정본 2043 = 두 행");
            PopupLayer.Instance.Hide(ForgeInfoPopup.Name);
            fh.Forge.UpgradeEndsAt = null;
            yield return null;
        }
    }
}
