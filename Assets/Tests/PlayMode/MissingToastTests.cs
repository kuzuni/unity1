using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Data;
using Forge.Core.Skills;
using Forge.Core.Tech;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T143 — 정본이 말하는 자리에서 클론이 조용했던 셋(T33 7회차 · 토스트 문구 71개 전수 대조):
    /// ⓐ 스킬 슬롯 가득(`ui.js` 4462) ⓑ 기술 연구 완료(`techtree.js` 383) ⓒ 리그 시즌 종료 문구(`league.js` 64 · 원작에 없는 순위 숫자를 뺀다).
    /// </summary>
    public class MissingToastTests
    {
        static void DeleteSave() { try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (Exception) { } }

        [UnityTest]
        public IEnumerator 스킬_슬롯이_꽉_찼는데_또_끼우면_최대_개수_토스트가_뜨고_장착은_안_변한다()
        {
            PetSkillHost.SuppressSave = true;
            PetSkillHost.Seed = 20260912;
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            Scene active = SceneManager.GetActiveScene();
            for (int i = 0; i < 600 && !(SkillPetSheet.Instance != null && SkillPetSheet.Instance.gameObject.scene == active && PetSkillHost.Ready && SkillBar.Instance != null); i++) yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다");
            Assert.IsTrue(PetSkillHost.Ready);
            PetSkillHost host = PetSkillHost.Instance;
            SkillSystem sk = host.Skills;
            int max = sk.Rules.MaxActive;

            // 보유 스킬을 최대 장착 수 + 1 이상으로 채우고 앞 max 개를 끼운다
            foreach (SkillDef d in host.Data.Defs.SkillDefs)
            {
                if (sk.State.Skills.Count > max) break;
                if (!sk.State.Skills.Has(d.Id)) sk.State.Skills.Add(d.Id, new SkillEntry());
            }
            Assert.Greater(sk.State.Skills.Count, max, "정의된 스킬이 최대 장착 수보다 많다");
            sk.State.Equipped.Clear();
            for (int i = 0; i < max; i++) sk.State.Equipped.Add(sk.State.Skills.KeyAt(i));
            string extra = sk.State.Skills.KeyAt(max);
            Assert.IsFalse(sk.State.Equipped.Contains(extra));

            Action<string> prev = PetSkillHost.Toast;
            string last = null;
            PetSkillHost.Toast = m => { last = m; if (prev != null) prev(m); };
            try
            {
                SkillPetSheet.Instance.Skills.OnToggle(extra);
                yield return null;
                Assert.AreEqual(PetSkillStyle.T("toast_skill_max", max), last, "정본 ui.js 4462 문구(개수는 규칙표)");
                StringAssert.Contains("스킬은 최대 " + max + "개 장착 가능합니다", last);
                Assert.AreEqual(max, sk.State.Equipped.Count, "장착 수는 안 변한다");
                Assert.IsFalse(sk.State.Equipped.Contains(extra), "못 낀 스킬은 장착 목록에 없다");

                // 낀 것을 빼는 토글은 말이 없다
                last = null;
                string first = sk.State.Equipped[0];
                SkillPetSheet.Instance.Skills.OnToggle(first);
                yield return null;
                Assert.IsNull(last, "해제는 토스트가 없다");
                Assert.AreEqual(max - 1, sk.State.Equipped.Count);
            }
            finally { PetSkillHost.Toast = prev; }
            yield return null;
        }

        [UnityTest]
        public IEnumerator 기술_연구를_완료하면_이름_단계_레벨_토스트가_뜬다()
        {
            DeleteSave();
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t0 = Time.realtimeSinceStartup;
            while (!DungeonUiHost.Ready)
            {
                Assert.Less(Time.realtimeSinceStartup - t0, 20f, "DungeonUiHost 가 20초 안에 Ready 되지 않았다");
                yield return null;
            }
            yield return null;
            DungeonUiHost H = DungeonUiHost.Instance;
            TechTree tree = H.Tech;
            TechPanel p = TechPanel.OpenTechTree();
            yield return null;
            Assert.IsNotNull(p);
            p.ShowBranch("power");
            yield return null;
            string id = p.NodeIds[0];
            Assert.IsTrue(tree.IsUnlocked(id));

            H.S.Potions = 1e9;
            TechPopups.OpenNode(id);
            yield return null;
            TechPopups.OnStart();
            yield return null;
            Assert.AreEqual(id, tree.ResearchingId(), "연구 시작");
            tree.State.Research.EndsAt = H.Now() - 1;
            TechPopups.OpenNode(id);
            yield return null;
            TechPopups.OnClaim();
            yield return null;
            Assert.AreEqual(1, tree.Level(id), "완료 → Lv.1");
            string want = "🔬 " + tree.Def(id).Name + " " + tree.TierLabel(id) + "단계 Lv." + tree.Level(id) + " 연구 완료!";
            Assert.AreEqual(want, DungeonToast.Last, "정본 techtree.js 383 문구(이름 · 로마 단계 · 오른 뒤 레벨)");
            TechPopups.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator 리그_시즌이_끝나면_정본_문구_그대로_순위_숫자_없이_말한다()
        {
            DeleteSave();
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다");
            MetaHost h = MetaHost.Instance;
            h.OpenLeague();            // 시즌이 없으면 여기서 시작된다(봇 생성)
            yield return null;
            Assert.IsTrue(PopupLayer.Instance.IsOpen(LeagueSheet.Name), "리그 시트");
            Assert.IsNotNull(h.LeagueState.Bots, "시즌이 섰다");
            h.LeagueState.SeasonEndsAt = h.NowMs - 1;   // 시즌을 끝낸다
            h.OpenLeague();            // Ensure → 시즌 종료 정산 → 토스트
            yield return null;
            Assert.AreEqual("🏆 리그 시즌 종료! 순위 보상을 획득했습니다", PopupLayer.Instance.LastToast, "정본 league.js 64 그대로");
            Assert.Greater(h.LeagueState.SeasonEndsAt, h.NowMs, "새 시즌이 시작됐다");
            yield return null;
        }
    }
}
