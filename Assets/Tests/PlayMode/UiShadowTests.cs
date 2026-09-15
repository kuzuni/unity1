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
        public void 표는_딱딱한_턱_다섯과_흐린_그림자_일곱으로_갈린다()
        {
            int hard = 0, soft = 0;
            foreach (string k in UiShadow.Table.Keys) { if (UiShadow.Table.Get(k).IsHard) hard++; else soft++; }
            Assert.AreEqual(5, hard);
            Assert.AreEqual(7, soft);
        }
    }
}
