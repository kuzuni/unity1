using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Forging;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T484 — 가로 흐름(flex row)의 틈이 코드에 박혀 정본과 어긋나던 다섯 자리를 «표 키 + 앞 형제 오른변 + gap» 으로 세운 뒤 되재는 자.
    /// 여섯째(리그 시즌 막대 2300)는 선물 아이콘이 `position: absolute` 라 gap 이 닿는 흐름 자식이 하나 — 잴 것이 없다(결정 805).
    /// 자리마다 «앞 형제의 오른변 + 표 gap = 뒤 형제의 왼변» 을 앵커 좌표(UiKit.Place = 왼위 앵커)로 잰다.
    /// </summary>
    public class RowGapSitesTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && DungeonUiHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready && MetaHost.Ready && DungeonUiHost.Ready, "호스트 셋이 20초 안에 준비되지 않았다");
            yield return null;
        }

        [TearDown]
        public void CleanSave()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
        }

        static Transform FindIn(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
        static float Left(RectTransform rt) { return rt.anchoredPosition.x; }
        static float Right(RectTransform rt) { return rt.anchoredPosition.x + rt.rect.width; }

        [Test]
        public void 표는_다섯_틈을_정본_rem_그대로_쥔다()
        {
            Assert.AreEqual(0.40f, UiKit.L("dg_rw_mr_em"), 1e-6f, "style.css 1992 `.dg-rw { margin-right: .40em }`");
            Assert.AreEqual(0.2f, UiKit.L("qst_reward_gap_rem"), 1e-6f, "style.css 2056 `.qst-reward { gap: .2rem }`");
            Assert.AreEqual(0.45f, CraftStyle.L("sellwarn_cmp_gap_rem"), 1e-6f, "style.css 2221 `.sellwarn-cmp { gap: .45rem }`");
            Assert.AreEqual(0.3f, UiKit.L("league_ticket_gap_rem"), 1e-6f, "style.css 2591 `.league-ticket-pill { gap: .3rem }`");
            Assert.AreEqual(1.15f, UiKit.L("league_ticket_ico_rem"), 1e-6f, "style.css 2598 `.league-ticket-pill .ico { 1.15rem }`");
            Assert.AreEqual(0.4f, UiKit.L("asc_row_gap_rem"), 1e-6f, "style.css 5620 `.asc-row { gap: .4rem }`");
        }

        [UnityTest]
        public IEnumerator 던전_배너의_보상_슬롯은_아이콘_폭_더하기_40em_씩_서고_이름은_마지막_슬롯_뒤에_선다()
        {
            yield return Boot();
            UiRoot.Instance.TabBar.OnTab("dungeon");
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            DungeonSheet s = DungeonSheet.Instance;
            Assert.IsTrue(s.IsOpen, "던전 시트");
            // 망치 도둑 배너 = 슬롯 둘(hammer · coin) — 두 슬롯 사이와 슬롯 → 이름 둘 다 «아이콘 폭 + .40em».
            Transform a = FindIn(s.transform, "rw-hammer"), b = FindIn(s.transform, "rw-coin");
            Assert.IsNotNull(a, "망치 슬롯"); Assert.IsNotNull(b, "코인 슬롯");
            Assert.AreEqual(a.parent, b.parent, "같은 배너");
            RectTransform ra = (RectTransform)a, rb = (RectTransform)b;
            RectTransform name = (RectTransform)a.parent.Find("name");
            Assert.IsNotNull(name, "배너 이름");
            float fs = name.GetComponent<TextMeshProUGUI>().fontSize;
            float mr = fs * UiKit.L("dg_rw_mr_em");
            Assert.AreEqual(Right(ra) + mr, Left(rb), 0.5f, "슬롯 둘 사이 = .40em(전엔 38% 겹쳤다)");
            Assert.AreEqual(Right(rb) + mr, Left(name), 0.5f, "마지막 슬롯 → 이름 = .40em");
            UiRoot.Instance.TabBar.OnTab("dungeon");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 퀘스트_보상의_아이콘과_수_사이는_2rem_이다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            QuestSheet.Open(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Popup p = h.Popups.Find(QuestSheet.Name);
            Assert.IsNotNull(p, "퀘스트 시트");
            Transform rw = FindIn(p.Root, "reward");
            Assert.IsNotNull(rw, "보상 상자");
            RectTransform ico = (RectTransform)rw.Find("ico"), amt = (RectTransform)rw.Find("amt");
            Assert.IsNotNull(ico); Assert.IsNotNull(amt);
            float gap = DungeonPopups.RemL("qst_reward_gap_rem");
            Assert.AreEqual(Right(ico) + gap, amt.offsetMin.x, 0.5f, "수의 왼 인셋 = 아이콘 오른변 + .2rem(전엔 icon×.05)");
            h.Popups.Hide(QuestSheet.Name);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 판매_경고_비교줄은_열_화살_열이_45rem_씩_띄고_화살은_글자_폭이다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeItem a = h.Engine.RollItem(), b = h.Engine.RollItem();
            ForgeCraftPopup.ShowSellConfirm(h, a, b);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Popup p = PopupLayer.Instance.Find(ForgeCraftPopup.SellName);
            Assert.IsNotNull(p, "판매 경고 팝업");
            RectTransform cmp = (RectTransform)FindIn(p.Root, "cmp");
            Assert.IsNotNull(cmp, "비교 줄(cmp)");
            RectTransform sold = (RectTransform)cmp.Find("sold"), gt = (RectTransform)cmp.Find("gt"), kept = (RectTransform)cmp.Find("kept");
            Assert.IsNotNull(sold); Assert.IsNotNull(gt); Assert.IsNotNull(kept);
            float gap = CraftStyle.Px("sellwarn_cmp_gap_rem");
            Assert.AreEqual(Right(sold) + gap, Left(gt), 0.5f, "파는 것 → 화살 = .45rem");
            Assert.AreEqual(Right(gt) + gap, Left(kept), 0.5f, "화살 → 남는 것 = .45rem");
            Assert.AreEqual(sold.rect.width, kept.rect.width, 0.5f, "두 열은 같은 폭(flex 1 1 0)");
            Assert.AreEqual(PetSkillKit.TextWidth(TextKind.Body, ">"), gt.rect.width, 0.5f, "화살 폭 = 글자 폭(auto · 전엔 2rem)");
            Assert.Less(gt.rect.width, PopupKit.Rem * 2f, "2rem 상자가 아니다");
            h.Meta.Popups.Hide(ForgeCraftPopup.SellName);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 리그_티켓_알약은_아이콘_3rem_글이_한_묶음으로_가운데다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            LeagueSheet.OpenChallenge(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Popup p = PopupLayer.Instance.Find(LeagueSheet.ChallengeName);
            Assert.IsNotNull(p, "도전 상대 선택 팝업");
            RectTransform pill = (RectTransform)FindIn(p.Root, "pill");
            Assert.IsNotNull(pill, "티켓 알약");
            RectTransform ico = (RectTransform)pill.Find("ico"), txt = (RectTransform)pill.Find("tickets");
            Assert.IsNotNull(ico); Assert.IsNotNull(txt);
            float gap = DungeonPopups.RemL("league_ticket_gap_rem"), icoW = DungeonPopups.RemL("league_ticket_ico_rem");
            Assert.AreEqual(icoW, ico.rect.width, 0.5f, "아이콘 1.15rem");
            Assert.AreEqual(Right(ico) + gap, Left(txt), 0.5f, "아이콘 → 글 = .3rem");
            float leftPad = Left(ico), rightPad = pill.rect.width - Right(txt);
            Assert.AreEqual(leftPad, rightPad, 0.5f, "묶음이 알약 가운데(justify-content: center)");
            Assert.Greater(leftPad, 0f, "묶음이 알약 안에 든다");
            h.Popups.Hide(LeagueSheet.ChallengeName);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 승천_줄은_이름_칸_5_2rem_뒤_4rem_에_진행이_서고_진행_뒤_4rem_에_개수가_선다()
        {
            yield return Boot();
            AscendPopup.Open();
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Assert.IsTrue(AscendPopup.IsOpen, "승천 팝업");
            Transform root = AscendPopup.Root;
            Transform row = null;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("row-") && t.Find("name") != null && t.Find("arrow") == null) { row = t; break; }
            Assert.IsNotNull(row, "준비 안 된 승천 줄(화살 없음)");
            RectTransform ico = (RectTransform)row.Find("ico"), name = (RectTransform)row.Find("name"), prog = (RectTransform)row.Find("prog"), cnt = (RectTransform)row.Find("cnt");
            Assert.IsNotNull(ico); Assert.IsNotNull(name); Assert.IsNotNull(prog); Assert.IsNotNull(cnt);
            float px = DungeonPopups.RemL("asc_row_pad_x_rem"), nameW = DungeonPopups.RemL("asc_name_w_rem"), gap = DungeonPopups.RemL("asc_row_gap_rem");
            Assert.AreEqual(px, Left(ico), 0.5f, "아이콘은 패딩 안쪽 — 이름 칸(5.2rem)의 첫 자식");
            Assert.AreEqual(px + nameW, Right(name), 0.5f, "이름 글 상자의 오른변 = 칸 오른변(아이콘 + 이름이 5.2rem 안)");
            Assert.AreEqual(px + nameW + gap, Left(prog), 0.5f, "이름 칸 → 진행 = .4rem(전엔 ≈1.74em)");
            Assert.AreEqual(Right(prog) + gap, Left(cnt), 0.5f, "진행 → 개수 = .4rem");
            AscendPopup.Close();
            yield return null;
        }
    }
}
