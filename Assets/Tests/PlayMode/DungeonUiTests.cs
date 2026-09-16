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
            {   // T404 ⓑ — 난이도 수(.dgd-stage b 1.15rem = 41.9px)는 모달 제목 단 Title2 42 로 선다(전엔 Body 40)
                TMPro.TextMeshProUGUI num = null;
                foreach (TMPro.TextMeshProUGUI t in UiRoot.Instance.App.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true)) if (t.name == "stage-num") { num = t; break; }
                Assert.IsNotNull(num, "난이도 수(stage-num)");
                Assert.AreEqual(UiCatalog.Instance.Kind(TextKind.Title2).size, num.fontSize, 0.01f, "정본 5310 .dgd-stage b 1.15rem → Title2");
            }
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
        /// T413 — 정본 `ui.js` **5603~5604** 는 노드 상세 머리를 **두 줄**로 둔다:
        /// `<div class="idet-name">${name} <small class="tn-lv">${roman}단계 · Lv.x/y</small></div>` · `<div class="idet-main">+총합 <small class="tn-gain">(…)</small></div>`.
        /// `<small>` 은 인라인이라 레벨이 **이름과 같은 줄**에 붙는다. 클론은 그것을 제 줄 하나로 빼 **세 줄**이었고, 머리 잉크가 40px 이어야 할 자리에서 61px 였다(런 848 실측 · 그 아래는 다 맞았다).
        /// 판정은 «몇 줄인가» 다 — 이름과 레벨이 **같은 y 밴드**에 있고 레벨이 이름 **오른쪽**에 서며, 총합 줄이 그 **바로 아래**인지 본다.
        /// </summary>
        [UnityTest]
        public IEnumerator 기술_노드_머리는_이름과_레벨이_한_줄이고_총합이_그_아래다()
        {
            yield return Boot();
            TechTree tree = H.Tech;
            string id = tree.NodesOf("power")[0];
            TechPopups.OpenNode(id);
            yield return null;
            Canvas.ForceUpdateCanvases();

            RectTransform card = null;
            foreach (RectTransform rt in UiRoot.Instance.App.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "card" && rt.gameObject.activeInHierarchy && rt.Find("name") != null && rt.Find("lv") != null) { card = rt; break; }
            Assert.IsNotNull(card, "노드 상세 카드");
            RectTransform name = (RectTransform)card.Find("name"), lvl = (RectTransform)card.Find("lv"), main = (RectTransform)card.Find("main");
            Assert.IsNotNull(name, "이름"); Assert.IsNotNull(lvl, "레벨"); Assert.IsNotNull(main, "총합");

            // ⓐ 같은 줄 — 두 상자의 세로 가운데가 같다(정본 `<small>` 은 인라인이라 이름 줄 안에 있다).
            Assert.AreEqual(name.anchoredPosition.y, lvl.anchoredPosition.y, 1f,
                "레벨이 이름과 다른 줄에 있다(정본 5603 은 `<small>` 로 같은 줄에 둔다) · 이름 y " + name.anchoredPosition.y + " · 레벨 y " + lvl.anchoredPosition.y);
            Assert.AreEqual(name.rect.height, lvl.rect.height, 1f, "같은 줄이면 상자 높이도 같다");
            // ⓑ 레벨은 이름 **오른쪽** · 틈은 표(정본 3695 `.tn-lv { margin-left: .15rem }`)
            TMPro.TextMeshProUGUI nt = name.GetComponent<TMPro.TextMeshProUGUI>();
            Assert.IsNotNull(nt, "이름 글자");
            float want = name.anchoredPosition.x + nt.preferredWidth + PopupKit.Rem * TechStyle.L("tn_lv_margin_left_rem");
            Assert.AreEqual(0.15f, TechStyle.L("tn_lv_margin_left_rem"), 1e-6f, "표 = 정본 3695 .15rem");
            Assert.AreEqual(want, lvl.anchoredPosition.x, 1f, "레벨은 이름 잉크 바로 뒤 + 표 틈 · 실측 " + lvl.anchoredPosition.x + " · 기대 " + want);
            // ⓒ 총합은 그 **바로 아래** 한 줄 — 세 줄이면 이 거리가 두 줄만큼이다.
            //   ⚠ `UiKit.Place(rt, x, yTop, …)` 는 `anchoredPosition.y = -yTop` 다(피벗 좌상단) — **아래로 갈수록 y 가 작아진다**.
            //   2회차가 이 부호를 거꾸로 적어 런 872 에서 «기대 >0 · 실측 −50» 으로 넘어졌다(배치는 맞았다 · −50 = 딱 한 줄).
            float drop = name.anchoredPosition.y - main.anchoredPosition.y;
            Assert.Greater(drop, 0f, "총합이 이름보다 위다 · 실측 " + drop);
            Assert.Less(drop, name.rect.height * 1.5f, "총합이 이름에서 두 줄 아래다 — 가운데 한 줄(레벨)이 아직 끼어 있다 · 실측 " + drop);

            TechPopups.Close();
            yield return null;
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
        /// T428 1회차 — **잠긴 노드 카드가 버튼 높이를 옛 키로 재서 안내줄이 카드 밖에서 잘렸다.**
        /// `ActionHeight(Locked)` 는 `btn_sm_h_rem`(2rem)으로 재고 `RenderAction` 은 `tech_btn_h_rem`(3.6rem)으로 그렸다 —
        /// 카드 높이가 **1.6rem** 짧아 안내줄 상자가 카드 바닥 아래로 내려갔다(런 943 `screen_tech-node.png`: «열립니다» 가 «열립니」 로 끊긴다).
        /// 이 자는 **재는 수와 그리는 수가 같은가** 를 화면에서 묻는다: 안내줄 아래끕이 카드 안이고, 남는 여백이 카드 패딩만큼이다.
        /// </summary>
        [UnityTest]
        public IEnumerator 기술_노드_잠김_카드는_안내줄까지_품고_선다()
        {
            yield return Boot();
            TechTree tree = H.Tech;
            string locked = null;
            foreach (string id in tree.NodesOf("power")) if (!tree.IsUnlocked(id)) { locked = id; break; }
            Assert.IsNotNull(locked, "잠긴 노드가 하나는 있다(1단계 위)");
            TechPopups.OpenNode(locked);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Assert.AreEqual(TechPopups.NodeState.Locked, TechPopups.State, "잠긴 노드를 열었다");

            RectTransform lockedBtn = DungeonPopups.Root(TechPopups.ActionButton);
            RectTransform card = (RectTransform)lockedBtn.parent;
            Assert.AreEqual("card", card.name, "[잠김] 알약은 노드 상세 카드의 자식이다");
            RectTransform hint = (RectTransform)card.Find("hint");
            Assert.IsNotNull(hint, "잠긴 카드의 안내줄(hint)");

            Rect rc = World(card), rh = World(hint);
            float over = rc.yMin - rh.yMin;   // 양수 = 안내줄이 카드 안
            Assert.GreaterOrEqual(over, -0.5f,
                "안내줄 아래끕이 카드 안이다 · 실측 여백 " + over.ToString("0.0") + "px(음수 = 카드 밖으로 튀어나왔다)");
            // 고침 전에는 재는 수가 1.6rem 작아 이 여백이 음수였다. 또 반대로 너무 많이 남아도 안 된다 —
            // 카드는 아래 패딩 한 칸만 남기므로(ch = … + pad * 2) 그 값에 서야 «재는 수 = 그리는 수» 가 증명된다.
            float pad = rc.width * UiKit.L("idet_pad");
            Assert.AreEqual(pad, over, Mathf.Max(1.5f, pad * 0.12f),
                "남는 여백 = 카드 아래 패딩(idet_pad) · 실측 " + over.ToString("0.0") + "px · 표 " + pad.ToString("0.0") + "px");
            Debug.Log("[T428] 잠긴 카드 여백 " + over.ToString("0.0") + "px · 패딩 " + pad.ToString("0.0") + "px · 카드 높이 " + rc.height.ToString("0.0"));
            TechPopups.Close();
            yield return null;
        }

        private static Rect World(RectTransform rt)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y);
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
            // «sweep» 은 DungeonPopups.Pill 이 세운 뿌리 상자(UiKit.Place 가 놓는 그것)라 그 자체가 Root 다 — 런 840: 거기서 Button 을 다시 찾아 NRE.
            RectTransform sweep = sweepT as RectTransform;
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
