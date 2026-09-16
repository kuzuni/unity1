using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T331 2회차 — 정본 `box-shadow` 의 **딱딱한 턱**이 화면에 실제로 선다(정본 `.qst-row` 2026 · `.equipped-row` 4128).
    ///
    /// 자가 보는 것: 그늘이 **대상 뒤**(첫 형제 = 가장 먼저 그려진다)에 있고 · 상자를 꽉 채우고 · 정본만큼 **아래로** 내려가 있고 ·
    /// 색이 표대로고 · 클릭을 안 먹는다. 흐린 그림자를 달라고 하면 도우미가 **거절한다**(조용히 딱딱하게 그리면 자가 거짓으로 초록이 된다).
    /// </summary>
    public class UiShadowTests
    {
        static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            yield return null;
        }

        static void AssertLip(RectTransform box, string key)
        {
            Transform sh = UiShadow.Find(box, key);
            Assert.IsNotNull(sh, box.name + " 에 그늘 겹이 없다(" + key + ")");
            Assert.AreEqual(0, sh.GetSiblingIndex(), "그늘은 첫 형제라야 대상 **뒤**에 그려진다");
            Image img = sh.GetComponent<Image>();
            Assert.IsNotNull(img);
            Assert.IsFalse(img.raycastTarget, "그늘은 클릭을 안 먹는다");

            ShadowSpec s = UiShadow.Table.Get(key);
            Assert.AreEqual((float)s.A, img.color.a, 1e-3f, "정본 rgba 의 알파");
            Assert.AreEqual((float)s.R, img.color.r, 1e-3f);

            var rt = (RectTransform)sh;
            // ⚠ 런 528 이 가르친 것: 늘어난(stretch) RectTransform 에서 `offsetMin`·`offsetMax` 는 **자리와 크기를 같이 쥔다** —
            //    치우침을 주면 둘 다 그만큼 밀린다(실측 (0, −9.10)). 그러니 «꽉 채운다» 는 그 둘이 0 인가가 아니라
            //    **크기가 상자와 같은가**(`sizeDelta` 0 · 늘어난 앵커)로 봐야 한다. 치우침은 아래에서 따로 잰다.
            Assert.AreEqual(Vector2.zero, rt.anchorMin, "상자에 늘어붙는다(왼·아래 앵커)");
            Assert.AreEqual(Vector2.one, rt.anchorMax, "상자에 늘어붙는다(오른·위 앵커)");
            Assert.AreEqual(Vector2.zero, rt.sizeDelta, "상자와 같은 크기 — 턱은 크기가 아니라 자리만 다르다");
            double x, y;
            UiShadow.Table.OffsetPx(key, PetSkillStyle.RemPx, out x, out y);
            Assert.AreEqual((float)y, rt.anchoredPosition.y, 0.01f, "정본만큼 아래로 — CSS 의 +y 는 화면에서 −y 다");
            Assert.Less(rt.anchoredPosition.y, 0f, key + " 은 아래로 내려간 턱이다");
            Assert.AreEqual((float)x, rt.anchoredPosition.x, 0.01f);
        }

        [UnityTest]
        public IEnumerator 퀘스트_행과_장착_바에_정본_턱이_한_겹_깔린다()
        {
            yield return Boot();
            QuestSheet.Open(MetaHost.Instance);
            yield return null;
            yield return null;
            int rows = 0;
            foreach (RectTransform rt in Object.FindObjectsOfType<RectTransform>(true))
            {
                if (rt.name != "row" || rt.parent == null || !rt.parent.name.StartsWith("q-", System.StringComparison.Ordinal)) continue;
                AssertLip(rt, "qstrow_lip");
                rows++;
            }
            Assert.Greater(rows, 0, "퀘스트 행이 한 줄도 없다 — 그러면 이 자는 아무것도 안 본 것이다");
            QuestSheet.Close(MetaHost.Instance);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 장착_바_턱은_소환_시트의_스킬_칸에도_선다()
        {
            yield return Boot();
            float t = 0f;
            while (!(SkillPetSheet.Instance != null && PetSkillHost.Ready) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다");
            UiRoot.Instance.TabBar.OnTab("summon");
            yield return null;
            SkillPetSheet.Instance.Switch(SkillPetSheet.SubSkills);
            yield return null;
            yield return null;
            int found = 0;
            foreach (RectTransform rt in Object.FindObjectsOfType<RectTransform>(true))
            {
                if (rt.name != "equipped-row") continue;
                AssertLip(rt, "equipped_lip");
                found++;
            }
            Assert.Greater(found, 0, "장착 바(`.equipped-row`)를 못 찾았다");
        }

        [UnityTest]
        public IEnumerator 상단바에는_흐린_그늘이_구워져_상자보다_넓게_깔린다()
        {
            yield return Boot();
            RectTransform bar = null;
            foreach (RectTransform rt in Object.FindObjectsOfType<RectTransform>(true))
                if (rt.name == "topbar") { bar = rt; break; }
            Assert.IsNotNull(bar, "상단바를 못 찾았다");

            Transform sh = UiShadow.Find(bar, "topbar_drop");
            Assert.IsNotNull(sh, "상단바에 그늘 겹이 없다(topbar_drop)");
            Assert.AreEqual(0, sh.GetSiblingIndex(), "그늘은 첫 형제라야 상단바 **뒤**에 그려진다");
            Image img = sh.GetComponent<Image>();
            Assert.IsNotNull(img.sprite, "흐린 그림자는 **구운 판**이라야 한다(딱딱하게 대신 그리면 안 된다)");
            Assert.AreEqual(Color.white, img.color, "색은 구운 화소가 쥔다 — 틴트로 주면 알파가 두 번 곱해진다");

            // 흐림이 잘리지 않게 상자보다 넓다 · 정본만큼 아래로 내려가 있다
            var rt2 = (RectTransform)sh;
            ShadowSpec s = UiShadow.Table.Get("topbar_drop");
            double dx, dy;
            UiShadow.Table.OffsetPx("topbar_drop", PetSkillStyle.RemPx, out dx, out dy);
            float padTop = rt2.offsetMax.y - (float)dy, padBottom = -(rt2.offsetMin.y - (float)dy);
            Assert.Greater(padTop, (float)s.BlurRem * PetSkillStyle.RemPx, "흐림 반지름보다 넓게 구워야 잘리지 않는다");
            Assert.AreEqual(padTop, padBottom, 0.01f, "테두리는 사방 같다");
            Assert.Less(rt2.offsetMin.y + padTop, 0f, "정본만큼 아래로 — CSS 의 +y 는 화면에서 −y 다");
        }

        [UnityTest]
        public IEnumerator 던전_배너에도_정본_턱이_깔린다()
        {
            yield return Boot();
            UiRoot.Instance.TabBar.OnTab("dungeon");
            yield return null;
            yield return null;
            int found = 0;
            foreach (RectTransform rt in Object.FindObjectsOfType<RectTransform>(true))
            {
                if (!rt.name.StartsWith("dg-", System.StringComparison.Ordinal)) continue;
                if (UiShadow.Find(rt, "dgbanner_lip") == null) continue;
                AssertLip(rt, "dgbanner_lip");
                found++;
            }
            Assert.Greater(found, 0, "던전 배너(`dg-…`)에 그늘이 한 장도 없다");
        }

        [UnityTest]
        public IEnumerator 리그_발판의_그늘은_위로_뜬다()
        {
            yield return Boot();
            LeagueSheet.Open(MetaHost.Instance);
            yield return null;
            yield return null;
            RectTransform foot = null;
            foreach (RectTransform rt in Object.FindObjectsOfType<RectTransform>(true))
                if (rt.name == "foot" && UiShadow.Find(rt, "leaguefoot_up") != null) { foot = rt; break; }
            Assert.IsNotNull(foot, "리그 발판(`foot`)의 그늘을 못 찾았다 — 상자 크기가 아직 0 이면 굽기가 조용히 건너뛴다");

            var rt2 = (RectTransform)UiShadow.Find(foot, "leaguefoot_up");
            Assert.AreEqual(0, rt2.GetSiblingIndex(), "그늘은 발판 바탕 **뒤**에 깔린다");
            ShadowSpec s = UiShadow.Table.Get("leaguefoot_up");
            Assert.Less(s.DyRem, 0.0, "정본 `.league-foot` 은 **위로** 뜨는 그늘이다");
            Assert.IsFalse(s.IsHard, "그리고 흐리다 — 구운 판이라야 한다");
            Assert.IsNotNull(rt2.GetComponent<Image>().sprite, "구운 판");

            // 위로 뜬다 = 구운 판의 가운데가 상자 위쪽으로 밀려 있다(넓힘을 뺀 순수 치우침이 양수).
            double dx, dy;
            UiShadow.Table.OffsetPx("leaguefoot_up", PetSkillStyle.RemPx, out dx, out dy);
            Assert.Greater(dy, 0f, "CSS 의 −y 는 화면에서 +y");
            float pad = rt2.offsetMax.y - (float)dy;
            Assert.Greater(pad, (float)s.BlurRem * PetSkillStyle.RemPx, "흐림 반지름보다 넓게 구웠다");
            LeagueSheet.Close(MetaHost.Instance);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 자동_제련_카드는_턱과_앰비언트_두_겹을_쥔다()
        {
            yield return Boot();
            // 정본 `.af-card`(5030)는 그림자가 둘이다 — 공용 아래턱 + 앰비언트. 한 이름으로 깔면 뒤엣것이 앞엣것을 덮는다.
            ForgeHost fh = ForgeHost.Instance;
            fh.S.BestChapter = 3; fh.S.BestStage = 1; fh.Pull();   // 2-10 해금 뒤라야 팝업이 연다(다른 자들과 같은 길)
            ForgeAutoPopup.Open(fh);
            yield return null;
            yield return null;
            RectTransform card = null;
            foreach (RectTransform rt in Object.FindObjectsOfType<RectTransform>(true))
                if (rt.name == "card" && UiShadow.Find(rt, "afcard_drop") != null) { card = rt; break; }
            Assert.IsNotNull(card, "자동 제련 카드의 앰비언트 겹을 못 찾았다");

            Transform lip = UiShadow.Find(card, "card_lip");
            Transform amb = UiShadow.Find(card, "afcard_drop");
            Assert.IsNotNull(lip, "공용 아래턱도 같이 있어야 한다(둘이 겹친다)");
            Assert.AreNotSame(lip, amb, "두 겹은 서로 다른 것이다 — 이름이 하나면 뒤엣것이 앞엣것을 덮는다");
            Assert.Less(amb.GetSiblingIndex(), lip.GetSiblingIndex(), "CSS 목록의 뒤쪽(앰비언트)이 더 뒤에 그려진다");
            Assert.IsTrue(UiShadow.Table.Get("card_lip").IsHard, "아래턱은 딱딱하다");
            Assert.IsFalse(UiShadow.Table.Get("afcard_drop").IsHard, "앰비언트는 흐리다");
            Assert.IsNotNull(amb.GetComponent<Image>().sprite, "흐린 겹은 구운 판");
            ForgeAutoPopup.Close(fh);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 패스_카드는_공용_턱_대신_캐스트_한_겹만_쥔다()
        {
            yield return Boot();
            // 정본 8602 `.modal-card.pass-card { box-shadow: 0 1.05rem 1.6rem -.5rem rgba(0,0,0,.6) }` —
            // `.modal-card`(3518)의 딱딱한 턱을 **갈아 끼운** 자리다(CSS 그림자는 겹치지 않는다).
            // 자동 제련 카드(둘을 겹치는 자리)와 **반대 계약**이라 둘을 같이 세워 둔다.
            MetaHost h = MetaHost.Instance;
            PassPopup.Open(h);
            yield return null;
            yield return null;
            RectTransform card = null;
            foreach (RectTransform rt in Object.FindObjectsOfType<RectTransform>(true))
                if (rt.name == "card" && UiShadow.Find(rt, "passcard_drop") != null) { card = rt; break; }
            Assert.IsNotNull(card, "패스 카드의 캐스트 겹을 못 찾았다");

            Transform cast = UiShadow.Find(card, "passcard_drop");
            Assert.IsNull(UiShadow.Find(card, "card_lip"), "공용 아래턱이 남아 있다 — 정본은 이 카드에서 그것을 갈아 끼웠다(두 겹이면 카드가 두 번 뜬다)");
            Assert.AreEqual(0, cast.GetSiblingIndex(), "그늘은 카드의 맨 뒤에 깔린다");
            Assert.IsFalse(UiShadow.Table.Get("passcard_drop").IsHard, "캐스트는 흐리다");
            Assert.Less(UiShadow.Table.Get("passcard_drop").SpreadRem, 0.0, "번짐이 음수인 유일한 자리다(안으로 줄인다)");
            Image ci = cast.GetComponent<Image>();
            Assert.IsNotNull(ci.sprite, "흐린 겹은 구운 판이다");
            // 판은 카드보다 넓게 굽고 그만큼 밖으로 내민다 — 안 그러면 번짐이 카드 변에서 잘린다.
            Assert.Greater(ci.rectTransform.rect.width, card.rect.width, "구운 판이 카드보다 안 넓다 — 번짐이 잘린다");
            Assert.Greater(ci.rectTransform.rect.height, card.rect.height, "세로도 넓어야 한다");
            // CSS 의 +y 는 아래다 — 1.05rem 내려간 자리라야 한다.
            Assert.Less(ci.rectTransform.anchoredPosition.y, 0f, "캐스트가 아래로 안 내려갔다");
            PassPopup.Close(h);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 자동_제련_팝업의_스피너와_하위_행도_제_턱을_진다()
        {
            yield return Boot();
            // 정본 5007 `.af-spinner` · 4999 `.af-sub-row` — 둘 다 **안쪽 두 겹 + 바깥 한 겹**이다.
            // 안쪽은 클론 관용구로 이미 서 있고(T163 갈래) 이 축이 받는 것은 바깥 겹 하나씩이다.
            ForgeHost fh = ForgeHost.Instance;
            fh.S.BestChapter = 3; fh.S.BestStage = 1; fh.Pull();
            // 하위 행은 필터가 켜져 있을 때만 선다(정본과 같다) — 꺼져 있으면 켜고 연다.
            if (!fh.Engine.AutoForgeConfig().FilterOn) fh.ToggleAutoFilterOn();
            ForgeAutoPopup.Open(fh);
            yield return null;
            yield return null;
            RectTransform spin = null, row = null;
            foreach (RectTransform rt in Object.FindObjectsOfType<RectTransform>(true))
            {
                if (spin == null && rt.name == "af-spinner") spin = rt;
                if (row == null && rt.name.StartsWith("af-sub-")) row = rt;
            }
            Assert.IsNotNull(spin, "망치 수 스피너를 못 찾았다");
            Assert.IsNotNull(row, "필터 하위 행을 못 찾았다(자동 제련 필터가 닫혀 있으면 안 선다)");

            Transform lip = UiShadow.Find(spin, "afspinner_lip");
            Assert.IsNotNull(lip, "스피너의 바깥 턱이 없다");
            Assert.IsTrue(UiShadow.Table.Get("afspinner_lip").IsHard, "스피너 턱은 흐림 0 이라 굽지 않는다");
            Assert.AreEqual(0, lip.GetSiblingIndex(), "턱은 면보다 뒤에 깔린다");
            Assert.Less(lip.GetComponent<Image>().rectTransform.anchoredPosition.y, 0f, "턱이 아래로 안 내려갔다(CSS 의 +y 는 아래다)");

            Transform sh = UiShadow.Find(row, "afsubrow_drop");
            // ⚑ 27회차 — 여기서 한 번 빨갰다(런 921): 이 행은 레이아웃이 크기를 나중에 잡는 자식이라
            //   굽는 길이 «아직 0» 을 보고 조용히 빈손으로 돌아왔다. 이제 크기를 부르는 쪽이 준다.
            Assert.IsNotNull(sh, "하위 행의 그늘이 없다 — 레이아웃 자식이라 굽는 길에 크기를 줘야 한다");
            Assert.IsFalse(UiShadow.Table.Get("afsubrow_drop").IsHard, "하위 행 그늘은 흐리다(구운 판이라야 한다)");
            Assert.IsNotNull(sh.GetComponent<Image>().sprite, "흐린 겹은 구운 판이다");
            // 정본에서 가장 옅은 자리 — «안 보인다» 가 아니라 «두께» 다. 표값이 그대로 서야 한다.
            Assert.AreEqual(0.1, UiShadow.Table.Get("afsubrow_drop").A, 1e-6, "표의 알파(.1)가 아니다");
            // 구운 판은 행보다 넓다 — 넓지 않으면 «0 크기로 구웠다» 는 뜻이다(런 921 의 빨강이 그 갈래였다).
            RectTransform shRt = sh.GetComponent<Image>().rectTransform;
            Assert.Greater(shRt.rect.width, 1f, "구운 판이 0 폭이다");
            Assert.Greater(shRt.rect.height, 1f, "구운 판이 0 높이다");
            ForgeAutoPopup.Close(fh);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 보상_앵커_배지도_제_그늘을_진다()
        {
            yield return Boot();
            // 정본 7540 `.rw-anchor` 는 겹이 둘이다 — 노란 발광(빛 갈래)과 검정 그늘 `0 2px 4px rgba(0,0,0,.45)`.
            // 이 축이 받는 것은 뒤엣것 하나다. 배지는 «도착 pill 이 가려졌을 때만» 서므로 시트를 열어 가린다.
            MetaHost h = MetaHost.Instance;
            QuestSheet.Open(h);
            yield return null;
            var rewards = new System.Collections.Generic.Dictionary<string, double>();
            rewards["coins"] = 1000;
            RewardBurst.Play(rewards, UiRoot.Instance.App);
            yield return null;
            RectTransform an = null;
            foreach (RectTransform rt in Object.FindObjectsOfType<RectTransform>(true))
                if (rt.name == "rw-anchor") { an = rt; break; }
            Assert.IsNotNull(an, "앵커 배지가 안 섰다(시트가 코인 pill 을 안 가렸다면 이 자는 뜻이 없다)");

            Transform sh = UiShadow.Find(an, "rwanchor_drop");
            Assert.IsNotNull(sh, "앵커 배지의 그늘이 없다");
            Assert.AreEqual(0, sh.GetSiblingIndex(), "그늘은 테두리·바탕보다 뒤에 깔린다");
            Assert.IsNotNull(sh.GetComponent<Image>().sprite, "흐린 겹은 구운 판이다");
            // 정본이 이 자리만 px 로 적었다 — 16px 기준으로 옮긴 값이 표에 있다.
            Assert.AreEqual(0.125, UiShadow.Table.Get("rwanchor_drop").DyRem, 1e-9, "2px → .125rem");
            Assert.AreEqual(0.25, UiShadow.Table.Get("rwanchor_drop").BlurRem, 1e-9, "4px → .25rem");
            QuestSheet.Close(h);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 기술_판_정보_버튼에도_정본_그늘이_깔린다()
        {
            yield return Boot();
            // 정본 8174 `.info-btn` — 클론은 이 버튼을 **두 공장**이 만든다(`DungeonPopups.InfoButton` ·
            // `ForgeUi.InfoButton`). 지금 열린 것은 앞엣것이라 그 절반을 여기서 지킨다(자의 `NEED` 가 둘을 센다).
            TechPanel p = TechPanel.OpenTechTree();
            yield return null;
            Assert.IsNotNull(p, "기술 판이 안 열렸다");
            // ⚑ 33회차 — 개요의 가지 머리 원판에도 정본 그늘이 있다(2112 의 둘째 겹).
            RectTransform disc = null;
            foreach (RectTransform rt in Object.FindObjectsOfType<RectTransform>(true))
                if (UiShadow.Find(rt, "techbranch_drop") != null) { disc = rt; break; }
            Assert.IsNotNull(disc, "가지 머리 원판의 그늘이 없다");
            Assert.AreEqual(0, UiShadow.Find(disc, "techbranch_drop").GetSiblingIndex(), "그늘은 원판보다 뒤에 깔린다");
            Assert.IsNotNull(UiShadow.Find(disc, "techbranch_drop").GetComponent<Image>().sprite, "흐린 겹은 구운 판이다");
            // ⚑ 31회차 — 여기서 한 번 빨갰다(런 955): 정보 버튼은 **개요가 아니라 가지 화면**에서 선다
            //   (`TechPanel.RenderBranch` 가 세운다). `OpenTechTree` 는 개요로 여니 한 걸음 더 들어가야 한다.
            p.ShowBranch("power");
            yield return null;
            Assert.AreEqual(TechPanel.View.Branch, p.Current, "가지 화면이라야 정보 버튼이 선다");
            // «버튼이 없다» 와 «그늘이 없다» 를 갈라 말한다 — 둘을 한 줄로 묶으면 다음 사람이 헛다리를 짚는다.
            RectTransform btn = null;
            foreach (RectTransform rt in Object.FindObjectsOfType<RectTransform>(true))
                if (rt.name == "info-btn" && rt.GetComponentInParent<TechPanel>() != null) { btn = rt; break; }
            Assert.IsNotNull(btn, "기술 판에 정보 버튼 자체가 없다");

            Transform sh = UiShadow.Find(btn, "infobtn_drop");
            Assert.IsNotNull(sh, "기술 판 정보 버튼의 그늘이 없다");
            Assert.AreEqual(0, sh.GetSiblingIndex(), "그늘은 원판보다 뒤에 깔린다");
            Assert.IsNotNull(sh.GetComponent<Image>().sprite, "흐린 겹은 구운 판이다");
            Assert.Less(sh.GetComponent<Image>().rectTransform.anchoredPosition.y, 0f, "그늘이 아래로 안 내려갔다");
            Assert.AreEqual(0.38, UiShadow.Table.Get("infobtn_drop").A, 1e-6, "정본 rgba(0,0,0,.38)");
        }

        [UnityTest]
        public IEnumerator 탭_패널의_턱은_위로_뜬다()
        {
            yield return Boot();
            UiRoot.Instance.TabBar.OnTab("summon");
            yield return null;
            RectTransform panel = null;
            foreach (RectTransform rt in Object.FindObjectsOfType<RectTransform>(true))
                if (rt.name.StartsWith("panel-", System.StringComparison.Ordinal) && UiShadow.Find(rt, "panel_lip") != null) { panel = rt; break; }
            Assert.IsNotNull(panel, "탭 패널(`panel-…`)의 위턱을 못 찾았다");

            var sh = (RectTransform)UiShadow.Find(panel, "panel_lip");
            Assert.AreEqual(0, sh.GetSiblingIndex(), "턱은 패널 바탕 **뒤**에 깔린다");
            ShadowSpec spec = UiShadow.Table.Get("panel_lip");
            Assert.Less(spec.DyRem, 0.0, "정본 `.panel` 은 **위로** 내는 턱이다");
            Assert.IsTrue(spec.IsHard, "흐림 0 — 굽지 않는다");
            Assert.Greater(sh.anchoredPosition.y, 0f, "CSS 의 −y 는 화면에서 +y");
            Assert.AreEqual(Vector2.zero, sh.sizeDelta, "패널과 같은 크기 — 자리만 다르다");
        }

        [UnityTest]
        public IEnumerator 반지름을_상자에서_되읽는다()
        {
            yield return Boot();
            // 되읽기는 «UiKit.Rounded 가 준 배수를 거꾸로 나눈다» 이다 — 알고 만든 상자로 왕복을 잰다.
            RectTransform box = UiKit.Box(UiRoot.Instance.App, "shadow-radius-probe");
            UiKit.Place(box, 0f, 0f, 120f, 80f);
            float want = 17f;
            UiKit.Rounded(box, "face", "pp_paper", want);
            Assert.AreEqual(want, UiShadow.RadiusOf(box), 0.01f, "상자가 쓰는 반지름을 그대로 되읽어야 한다");

            // 그늘을 깐 **뒤에도** 같은 값이라야 한다 — 내가 깐 겹을 자기 자신으로 읽으면 회차마다 값이 흘러간다.
            UiShadow.Drop(box, "qstrow_lip");
            Assert.AreEqual(want, UiShadow.RadiusOf(box), 0.01f, "깐 그늘을 원본으로 착각하면 안 된다");

            // 둥근 그림이 없는 상자는 0(각진 그늘)
            RectTransform plain = UiKit.Box(UiRoot.Instance.App, "shadow-radius-plain");
            UiKit.Place(plain, 0f, 0f, 40f, 40f);
            Assert.AreEqual(0f, UiShadow.RadiusOf(plain), 1e-4f);
            Object.Destroy(box.gameObject); Object.Destroy(plain.gameObject);
        }

        [Test]
        public void 표는_딱딱한_턱과_흐린_그림자로_갈린다()
        {
            // 수는 회차마다 자란다(29회차에 남은 열하나의 값을 미리 재 담아 7+19) — EditMode 쪽과 같은 수를 본다.
            int hard = 0, soft = 0;
            foreach (string k in UiShadow.Table.Keys) { if (UiShadow.Table.Get(k).IsHard) hard++; else soft++; }
            Assert.AreEqual(7, hard);
            Assert.AreEqual(19, soft);
        }
    }
}
