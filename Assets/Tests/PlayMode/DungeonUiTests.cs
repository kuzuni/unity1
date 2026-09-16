using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Dungeon;
using Forge.Core.Tech;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T21 — 던전 목록·상세·클리어 팝업 · 기술 트리 개요/분기/노드/총 보너스 · 승천 팝업이 세이브 위에 서고 콘솔 빨강이 0 인가.
    /// 빨간 로그는 러너가 실패시킨다(§1 «플레이 콘솔 에러 0»). 세이브는 매 테스트 기본 상태에서 시작하도록 디스크 세이브를 지우고 부팅한다.
    /// </summary>
    public class DungeonUiTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = Time.realtimeSinceStartup;
            while (!DungeonUiHost.Ready)
            {
                Assert.Less(Time.realtimeSinceStartup - t, 20f, "DungeonUiHost 가 20초 안에 Ready 되지 않았다 (SaveIo · tech.json)");
                yield return null;
            }
            yield return null;
        }

        static DungeonUiHost H { get { return DungeonUiHost.Instance; } }

        [UnityTest]
        public IEnumerator 던전_탭은_흰_시트에_배너_4개를_세우고_잠금_해금을_가른다()
        {
            yield return Boot();
            TabBar tb = UiRoot.Instance.TabBar;
            Assert.IsNotNull(DungeonSheet.Instance, "던전 시트가 접착층 Ready 뒤에 서지 않았다");
            Assert.IsFalse(DungeonSheet.Instance.IsOpen);

            tb.OnTab("dungeon");
            yield return null;
            DungeonSheet s = DungeonSheet.Instance;
            Assert.IsTrue(s.IsOpen, "던전 탭 → 목록 시트");
            Assert.AreEqual(4, s.BannerCount, "원작 DEFS 4 = 망치 도둑·유령 마을·침략·좀비 러시");
            Assert.IsTrue(tb.IsX("dungeon"), "열린 popup 탭은 빨간 ✕");
            Assert.IsNull(tb.ActiveTab, "시트 탭(소환)은 안 열렸다");
            // 새 세이브(최고 1-1)는 전부 잠김 — 해금 2-8 · 2-10 · 3-1 · 4-1
            foreach (DungeonDef d in DungeonDefs.All)
            {
                Assert.IsFalse(H.Dungeons.Unlocked(d.Id), d.Id + " 는 새 세이브에서 잠겨 있어야 한다");
                Assert.IsFalse(s.OpenButton(d.Id).interactable, d.Id + " [열기] 는 비활성");
            }
            DungeonDetailPopup.Open("hammer");
            yield return null;
            Assert.IsFalse(DungeonDetailPopup.IsOpen, "잠긴 던전은 상세가 안 열리고 토스트");
            StringAssert.Contains("도달 시 해금", DungeonToast.Last);

            tb.OnTab("dungeon");
            yield return null;
            Assert.IsFalse(s.IsOpen, "✕ 를 누르면 닫힌다");
            Assert.IsFalse(tb.IsX("dungeon"));
        }

        [UnityTest]
        public IEnumerator 던전_상세는_난이도_보상_열쇠를_보이고_입장하면_판이_선다()
        {
            yield return Boot();
            H.S.BestChapter = 5;
            H.S.BestStage = 1;
            TabBar tb = UiRoot.Instance.TabBar;
            tb.OnTab("dungeon");
            yield return null;
            DungeonSheet s = DungeonSheet.Instance;
            Assert.IsTrue(H.Dungeons.Unlocked("hammer"));
            Assert.IsTrue(s.OpenButton("hammer").interactable);

            DungeonDetailPopup.Open("hammer");
            yield return null;
            Assert.IsTrue(DungeonDetailPopup.IsOpen);
            Assert.AreEqual("hammer", DungeonDetailPopup.Id);
            Assert.AreEqual(1, DungeonDetailPopup.Stage, "최고 0 → 도전 단계 1");
            Assert.AreEqual("1-1", DungeonDetailPopup.StageText, "원작 dgStageText");
            Assert.AreEqual("2/2", DungeonDetailPopup.KeysText, "새 세이브 열쇠 2/2");
            StringAssert.Contains("hammers", DungeonDetailPopup.RewardText, "망치 도둑 보상은 해머·코인");
            StringAssert.Contains("coins", DungeonDetailPopup.RewardText);
            Assert.IsFalse(DungeonDetailPopup.SweepButton.interactable, "최고 0 이면 소탕 없음");
            Assert.IsTrue(DungeonDetailPopup.EnterButton.interactable);
            Assert.IsFalse(DungeonDetailPopup.PrevButton.transform.parent.gameObject.activeSelf, "1단계에서 ◀ 숨김");
            Assert.AreEqual("2-3", DungeonDetailPopup.DgStageText(13));
            Assert.AreEqual("1-10", DungeonDetailPopup.DgStageText(10));

            DungeonDetailPopup.Enter();
            yield return null;
            Assert.IsTrue(H.Dungeons.InRun, "입장하면 세이브에 dungeonRun 이 선다");
            Assert.IsFalse(DungeonDetailPopup.IsOpen, "입장 뒤 상세·목록이 닫힌다");
            Assert.IsFalse(s.IsOpen);
            Assert.AreEqual(2, (int)H.Dungeons.Keys("hammer"), "열쇠는 입장이 아니라 완료 때 소모(원작 안내문)");
            StringAssert.Contains("망치 도둑", Hud.Instance.StageLabel, "스테이지 라벨이 던전을 가리킨다");

            // 클리어 팝업(Core 이벤트 → 화면)
            H.Dungeons.OnClear();
            yield return null;
            Assert.IsTrue(DungeonClearPopup.IsOpen, "onClear → showDungeonClear");
            Assert.AreEqual("클리어!", DungeonClearPopup.Title);
            StringAssert.Contains("망치 도둑 1단계", DungeonClearPopup.Sub);
            Assert.GreaterOrEqual(DungeonClearPopup.CellCount, 1, "보상 칸");
            DungeonRewards got = null;
            DungeonClearPopup.Confirmed += r => got = r;
            DungeonClearPopup.Confirm();
            yield return null;
            Assert.IsFalse(DungeonClearPopup.IsOpen);
            Assert.IsNotNull(got, "[보상 수령] 뒤 전투 씬으로 넘기는 이벤트");
            Assert.IsFalse(H.Dungeons.InRun, "클리어 뒤 run 은 비어 있다");
            Assert.AreEqual(1, (int)H.Dungeons.Best("hammer"));
            Assert.AreEqual(1, (int)H.Dungeons.Keys("hammer"), "클리어가 열쇠 1 을 소모");
        }

        [UnityTest]
        public IEnumerator 기술트리_개요_분기_노드_팝업_연구_시작_완료()
        {
            yield return Boot();
            TabBar tb = UiRoot.Instance.TabBar;
            TechPanel p = TechPanel.OpenTechTree();
            yield return null;
            Assert.IsNotNull(p);
            Assert.AreEqual("summon", tb.ActiveTab, "기술 트리는 소환 시트 안(원작 switchSummonSub('tech'))");
            Assert.AreEqual(TechPanel.View.Overview, p.Current);
            Assert.AreEqual(3, p.CardCount, "분기 3 = 힘·탈것 / 대장간 / 스킬, 펫 & 기술");
            Assert.AreEqual("기술 트리", p.TitleText);

            TechTree tree = H.Tech;
            p.ShowBranch("power");
            yield return null;
            Assert.AreEqual(TechPanel.View.Branch, p.Current);
            int expected = 0;
            foreach (TechRow r in tree.Rows("power")) expected += r.Ids.Length;
            Assert.AreEqual(expected, p.NodeCount, "노드 수 = 행의 id 수 합(5단계 × 7타입)");
            Assert.Greater(p.LinkCount, 0, "골격 선");
            Assert.AreEqual("0.0%", p.PctText);

            string id = p.NodeIds[0];
            Assert.IsTrue(tree.IsUnlocked(id), "1단계 첫 노드는 열려 있다");
            Assert.AreEqual("0/" + tree.Table.MaxLevel, p.NodeLabel(id));
            TechPopups.OpenNode(id);
            yield return null;
            Assert.IsTrue(TechPopups.IsNodeOpen);
            Assert.AreEqual(TechPopups.NodeState.Idle, TechPopups.State);
            Assert.AreEqual(tree.Def(id).Name, TechPopups.NameText);
            Assert.IsFalse(TechPopups.ActionButton.interactable, "물약 0 이면 [연구 시작] 비활성");

            H.S.Potions = 1e9;
            TechPopups.OpenNode(id);
            yield return null;
            Assert.IsTrue(TechPopups.ActionButton.interactable);
            TechPopups.OnStart();
            yield return null;
            Assert.AreEqual(id, tree.ResearchingId(), "연구 시작");
            Assert.AreEqual(TechPopups.NodeState.Researching, TechPopups.State);
            Assert.Less(H.S.Potions, 1e9, "물약이 빠졌다");
            Assert.IsNotNull(H.S.Obj("techResearch"), "세이브 트리에 techResearch");
            StringAssert.DoesNotContain("/", p.NodeLabel(id), "연구 중 노드 라벨은 남은 시간");

            // 시간을 끝내고 [완료]
            tree.State.Research.EndsAt = H.Now() - 1;
            TechPopups.OpenNode(id);
            yield return null;
            Assert.AreEqual(TechPopups.NodeState.Ready, TechPopups.State);
            TechPopups.OnClaim();
            yield return null;
            Assert.AreEqual(1, tree.Level(id), "완료 → Lv.1");
            Assert.IsNull(tree.ResearchingId());
            Assert.AreEqual("1/" + tree.Table.MaxLevel, p.NodeLabel(id));
            Assert.AreEqual(1.0, H.S.Obj("tech")[id], "세이브 트리 tech 에 레벨");

            TechPopups.OpenBonuses();
            yield return null;
            Assert.IsTrue(TechPopups.IsBonusesOpen);
            Assert.AreEqual(tree.TotalBonuses().Count, TechPopups.BonusRowCount);
            Assert.Greater(TechPopups.BonusRowCount, 0);
            TechPopups.Close();
            p.ShowOverview();
            yield return null;
            Assert.AreEqual(TechPanel.View.Overview, p.Current);
        }

        [UnityTest]
        public IEnumerator 승천_팝업은_라인_4행과_조건_미달_비활성을_보인다()
        {
            yield return Boot();
            AscendPopup.Open();
            yield return null;
            Assert.IsTrue(AscendPopup.IsOpen);
            Assert.AreEqual(4, AscendPopup.RowCount, "장비·스킬·펫·탈것");
            Assert.IsNull(AscendPopup.Line);
            StringAssert.Contains("보유 별 합계 0", AscendPopup.TitleText);
            Assert.IsNull(AscendPopup.AscendButton, "라인을 안 고르면 [닫기] 만");

            AscendPopup.Open("forge");
            yield return null;
            Assert.AreEqual("forge", AscendPopup.Line);
            Assert.IsNotNull(AscendPopup.AscendButton);
            Assert.IsFalse(AscendPopup.AscendButton.interactable, "대장간 Lv.1/35 — 조건 미달");
            AscendPopup.OnAscend("forge");
            yield return null;
            Assert.AreEqual(0, H.Asc.Count(H.AscState, "forge"), "미달이면 승천 안 된다");
            StringAssert.Contains("조건", DungeonToast.Last);

            // 조건을 채우면 승천 — 대장간 레벨 1 · 횟수 1 · 세이브 lineAscend
            H.S.ForgeLevel = H.Asc.Table.ForgeLevel;
            AscendPopup.Open("forge");
            yield return null;
            Assert.IsTrue(AscendPopup.AscendButton.interactable);
            string ascended = null;
            AscendPopup.Ascended += l => ascended = l;
            AscendPopup.OnAscend("forge");
            yield return null;
            Assert.AreEqual("forge", ascended);
            Assert.AreEqual(1, H.Asc.Count(H.AscState, "forge"));
            Assert.AreEqual(1, H.S.ForgeLevel, "승천 뒤 대장간 레벨 1");
            Assert.AreEqual(1.0, H.S.Obj("lineAscend")["forge"]);
            Assert.IsFalse(AscendPopup.IsOpen);
            AscendPopup.Close();
        }

        /// <summary>
        /// T401 1회차 — 정본 **1750** `.item-detail[data-tech-node] .btn { min-height: 3.6rem }` 은 노드 상세의 **모든** 버튼을 덮는다.
        /// 클론은 [잠김]만 `btn_sm_h_rem`(2rem)이라 같은 팝업의 형제 버튼(3.4rem)보다 −41% 였다 — 한 키를 쓰게 맞췄다.
        /// 마지막 −6%(표 3.4 ↔ 정본 3.6)는 `catalog.json` 이 열리는 회차 몫이라 여기서는 **형제와 같은가** 만 잰다.
        /// </summary>
        [UnityTest]
        public IEnumerator 기술_노드_잠김_버튼은_같은_팝업_형제와_같은_높이다()
        {
            yield return Boot();
            TechTree tree = H.Tech;
            string locked = null;
            foreach (string id in tree.NodesOf("power")) if (!tree.IsUnlocked(id)) { locked = id; break; }
            Assert.IsNotNull(locked, "잠긴 노드가 하나는 있다(1단계 위)");
            TechPopups.OpenNode(locked);
            yield return null;
            Assert.AreEqual(TechPopups.NodeState.Locked, TechPopups.State, "잠긴 노드를 열었다");
            RectTransform lockedBtn = DungeonPopups.Root(TechPopups.ActionButton);
            Assert.AreEqual(3.6, UiKit.L("tech_btn_h_rem"), 1e-6,
                "T401 3회차 — 정본 1750 `.item-detail[data-tech-node] .btn { min-height: 3.6rem }`(4612 의 .tech-btns .btn 3.4 보다 구체적이라 이 자리에선 이긴다)");
            float want = DungeonPopups.RemL("tech_btn_h_rem");
            Assert.AreEqual(want, lockedBtn.rect.height, 0.6f,
                "[잠김] 높이 = 형제 버튼과 같은 표 키(tech_btn_h_rem) · 전엔 btn_sm_h_rem 2rem 이라 −41% 였다");
            Debug.Log("[T401] 잠김 버튼 높이 " + lockedBtn.rect.height.ToString("0.0") + " · 표 " + want.ToString("0.0"));
            TechPopups.Close();
            yield return null;
        }

        /// <summary>
        /// T401 2회차 — 정본 **2255** `.panel .btn.tech-tree-back { width: 2.5rem; height: 2.5rem }`: 기술 트리 뒤로 버튼은 **정사각**이다.
        /// 클론은 리그 뒤로 버튼 치수(2.1×1.75rem)를 공용해 가로로 납작했다. 부르는 쪽이 남의 lock 이라 **반지름 키의 앞자리**로 치수 키를 함께 읽게 했으니,
        /// 이 자는 «그 규칙이 실제로 먹었는가» 를 잰다(다른 화면의 뒤로 버튼은 종전 치수 그대로여야 한다).
        /// </summary>
        [UnityTest]
        public IEnumerator 기술_트리_뒤로_버튼은_정본_2_5rem_정사각이다()
        {
            yield return Boot();
            UiRoot.Instance.TabBar.OnTab("dungeon");
            yield return null;
            TechPanel.OpenTechTree();
            yield return null;
            Canvas.ForceUpdateCanvases();
            RectTransform back = null;
            foreach (RectTransform rt in TechPanel.Instance.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "back-btn") { back = rt; break; }
            Assert.IsNotNull(back, "기술 트리 뒤로 버튼(back-btn)");
            float want = DungeonPopups.RemL("tech_back_w_rem");
            Assert.AreEqual(2.5, UiKit.L("tech_back_w_rem"), 1e-6, "정본 2255 width 2.5rem");
            Assert.AreEqual(2.5, UiKit.L("tech_back_h_rem"), 1e-6, "정본 2255 height·min-height 2.5rem");
            Assert.AreEqual(want, back.rect.width, 0.6f, "가로 = tech_back_w_rem × rem");
            Assert.AreEqual(want, back.rect.height, 0.6f, "세로도 같다 — 정사각(전엔 2.1×1.75 로 납작했다)");
            Debug.Log("[T401] 기술 뒤로 버튼 " + back.rect.width.ToString("0.0") + "×" + back.rect.height.ToString("0.0"));
        }

        static Transform FindDeep_(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++) { Transform r = FindDeep_(t.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        /// <summary>T409 — 정본 style.css 5338 주석: 카드의 남는 세로 공간은 «열쇠 줄 위» 와 «열쇠 줄 아래(버튼 위)» 로 반반이다(`.dgd-keys` 와 `.dgd-btns` 둘 다 margin-top: auto).
        /// 클론은 버튼만 바닥에 붙여 남는 공간이 전부 열쇠 아래로 몰렸다(런 813 실측: 위 18px ↔ 아래 76px). 이 자는 그 두 여백의 차가 카드 높이의 2%p 안인지 잰다.</summary>
        [UnityTest]
        public IEnumerator 던전_상세_열쇠_줄은_남는_공간을_위아래_반반으로_가른다()
        {
            yield return Boot();
            H.S.BestChapter = 5;
            H.S.BestStage = 1;
            UiRoot.Instance.TabBar.OnTab("dungeon");
            yield return null;
            DungeonDetailPopup.Open("hammer");
            yield return null; yield return null;
            Assert.IsTrue(DungeonDetailPopup.IsOpen, "던전 상세가 열린다");
            Canvas.ForceUpdateCanvases();
            Transform app = UiRoot.Instance.App;
            RectTransform card = FindDeep_(app, "modal-dungeon-detail").Find("card") as RectTransform;
            Assert.IsNotNull(card, "상세 카드");
            RectTransform pill = card.Find("reward-pill") as RectTransform;
            RectTransform keys = card.Find("keys") as RectTransform;
            Transform sweepT = FindDeep_(card, "sweep");
            Assert.IsNotNull(pill, "보상 알약"); Assert.IsNotNull(keys, "열쇠 글"); Assert.IsNotNull(sweepT, "소탕 버튼");
            RectTransform sweep = DungeonPopups.Root(sweepT.GetComponent<UnityEngine.UI.Button>());
            // UiKit.Place 는 좌상단 앵커 · anchoredPosition.y = −위끝
            float pillBottom = -pill.anchoredPosition.y + pill.rect.height;
            float keysTop = -keys.anchoredPosition.y, keysBottom = keysTop + keys.rect.height;
            float btnTop = -sweep.anchoredPosition.y;
            float gapTop = keysTop - pillBottom, gapBottom = btnTop - keysBottom;
            float ch = card.rect.height;
            Debug.Log("[T409] 카드 " + ch.ToString("0.0") + " · 알약~열쇠 " + gapTop.ToString("0.0") + " · 열쇠~버튼 " + gapBottom.ToString("0.0"));
            // 남는 공간이 실제로 있어야 이 자가 뜻이 있다(런 813 실측 위 18 ↔ 아래 76 샷px = 남는 공간 58) — 위 여백이 고정 .55rem 보다 뚜렷이 커야 한다
            Assert.Greater(gapTop, DungeonPopups.RemL("dgd_pill_mb_rem") + 1f, "카드 min-height(49.6%H)가 이겨 남는 공간이 생기는 자리인데 열쇠가 그 몫을 안 받았다");
            Assert.GreaterOrEqual(gapTop, DungeonPopups.RemL("dgd_pill_mb_rem") - 0.5f, "위 여백은 정본 알약 아래 여백(.55rem) 이상");
            Assert.GreaterOrEqual(gapBottom, DungeonPopups.RemL("dgd_keys_mb_rem") - 0.5f, "아래 여백은 정본 열쇠 아래 여백(.7rem) 이상");
            // auto 둘 = 남는 공간 반반: 고정 여백(.55 위 · .7 아래)을 뺀 나머지가 같다 → 두 여백의 차 = .15rem 뿐이다
            float fixedDiff = DungeonPopups.RemL("dgd_keys_mb_rem") - DungeonPopups.RemL("dgd_pill_mb_rem");
            Assert.AreEqual(fixedDiff, gapBottom - gapTop, ch * 0.02f, "열쇠 위·아래 여백이 반반이 아니다(정본 5338: 원작 26px ↔ 27px)");
        }
    }
}
