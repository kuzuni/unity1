using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Forging;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T352 ⓐ 10회차 — 굵기 축의 **안전망**. 정본 `style.css` 는 `font-weight` 233 선언 중 **225 가 bold** 고
    /// regular 는 **여덟뿐**이라 공장 기본이 bold 여야 하는데, 클론은 부호가 반대다(기본 regular + 120 자리 손박음).
    /// 그 뒤집기를 아직 **안 했다** — regular 로 남아야 할 자리 둘이 남의 산 lock 뒤라(`.btn small` `ForgeCraftPopup.cs` T377 ·
    /// `.age-tag small` `ForgeUi.cs` T453) 배선 없이 뒤집으면 **그 둘이 되레 틀린다**.
    /// 이 칸은 그때를 위한 자다: 지금 «우연히 맞는» 자리를 **표로 못 박아** 두어, 누가 공장 기본을 뒤집는 순간
    /// 배선이 빠진 자리가 **조용히가 아니라 빨갛게** 드러나게 한다.
    /// </summary>
    public class TextWeightSitesTests
    {
        static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null; yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
        }

        /// <summary>T352 13회차 — **공장 기본이 bold 다**(이 회차에 뒤집었다). 정본 `font-weight` 233 선언 중 **225 가 bold** 고
        /// regular 는 여덟뿐이라 «기본 bold + 표가 쥔 예외» 가 옳은 부호다. 여태는 반대였다 —
        /// 공장이 굵기를 안 줘서 **새 화면에서 손박음 한 줄을 잊으면 조용히 regular** 가 됐고 아무 자도 안 울었다.
        /// 이 칸이 그 부호를 지킨다: 되돌리면 여기서 먼저 빨개진다.</summary>
        [UnityTest]
        public IEnumerator 공장이_만든_글자는_기본이_bold_고_표의_색_키만_regular_다()
        {
            yield return Boot();
            var go = new GameObject("weight-probe");
            go.AddComponent<RectTransform>();
            TextMeshProUGUI plain = UiKit.Text(go.transform, "plain", TextKind.Body, "가", null);
            Assert.IsTrue((plain.fontStyle & FontStyles.Bold) != 0,
                "색 키를 안 준 글자는 **bold** 다 — 정본 233 중 225 가 bold 라 그것이 기본이다");
            TextMeshProUGUI ink = UiKit.Text(go.transform, "ink", TextKind.Body, "가", "pp_ink");
            Assert.IsTrue((ink.fontStyle & FontStyles.Bold) != 0, "일반 잉크도 bold 다");
            TextMeshProUGUI muted = UiKit.Text(go.transform, "muted", TextKind.Body, "가", "pp_muted");
            Assert.IsFalse((muted.fontStyle & FontStyles.Bold) != 0,
                "`pp_muted` 는 regular 다 — 정본 657 `.muted { color:#78909c; font-weight:400 }` 가 색과 굵기를 한 클래스로 묶었다");
            TextMeshProUGUI srv = UiKit.Text(go.transform, "srv", TextKind.Sub, "서버 1", "league_server");
            Assert.IsFalse((srv.fontStyle & FontStyles.Bold) != 0, "`league_server` 는 regular 다 — 정본 8633");
            Object.Destroy(go);
        }

        /// <summary>표가 실리고, 정본이 «색과 굵기를 한 클래스에 묶어 둔» 자리만 색 키로 걸린다.</summary>
        [UnityTest]
        public IEnumerator 굵기_표가_실리고_regular_는_정본이_묶어_둔_색_키로만_걸린다()
        {
            yield return Boot();
            TextWeightUi.Reset();
            // 정본 657 `.muted { color:#78909c; font-weight:400 }` · 8633 `.league-server { font-weight:500 }`
            Assert.IsTrue(TextWeightUi.RegularByColor("pp_muted"), "pp_muted 는 regular(정본 657 이 색과 굵기를 한 클래스로 묶었다)");
            Assert.IsTrue(TextWeightUi.RegularByColor("league_server"), "league_server 는 regular(정본 8633)");
            // 그 밖의 색은 bold 쪽이다 — 정본 233 선언 중 225 가 bold 라 «기본이 bold» 가 옳은 이식이다.
            Assert.IsFalse(TextWeightUi.RegularByColor("pp_ink"), "일반 잉크는 regular 가 아니다");
            Assert.IsFalse(TextWeightUi.RegularByColor(null), "색 키가 없으면 regular 가 아니다");
            Assert.IsFalse(TextWeightUi.RegularByColor("없는_키"), "표에 없는 색 키는 regular 가 아니다");
        }

        /// <summary>이름난 regular 자리 넷이 표에 있고, 없는 키는 **조용히 넘어가지 않는다**.</summary>
        [UnityTest]
        public IEnumerator 이름난_regular_자리는_표에_있고_없는_키는_던진다()
        {
            yield return Boot();
            TextWeightUi.Reset();
            foreach (string k in new[] { "rates_tip", "forge_item_cell_small", "btn_small", "pass_desc" })
                Assert.IsTrue(TextWeightUi.HasSite(k), "표에 regular 자리 «" + k + "» 이 있다(정본 8633·667)");
            // 11회차 — `age_tag_small` 은 «자리» 가 아니라 «죽음» 이다: 정본이 `age-tag` 클래스를 js·index.html 어디에서도 안 붙인다
            //   (ui.js 2109 의 forge-age-section 안은 fi-age-* 와 forge-item-grid 뿐 · 클래스는 전부 리터럴).
            Assert.IsFalse(TextWeightUi.HasSite("age_tag_small"), "age_tag_small 은 배선할 자리가 아니라 죽은 선언이다(표의 _dead 칸)");
            Assert.IsFalse(TextWeightUi.HasSite("없는_자리"), "표에 없는 자리는 없다고 답한다");
            var go = new GameObject("t");
            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            t.fontStyle = FontStyles.Bold;
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => TextWeightUi.Regular(t, "없는_자리"),
                "표에 없는 자리 키로 부르면 던진다 — 오타가 조용히 regular 를 못 만들게");
            TextWeightUi.Regular(t, "btn_small");
            Assert.IsFalse((t.fontStyle & FontStyles.Bold) != 0, "Regular() 는 Bold 비트만 걷는다");
            Object.Destroy(go);
        }

        /// <summary>T352 11회차 — **실제로 틀렸던 자리**: 패스 화면 안내문. 정본 2734 는 `font-weight: 800` 이지만
        /// **8633** 이 같은 특정도로 뒤에 와서 `500` 으로 덮고, 폴백 sans 는 regular/bold 두 축뿐이라 500 은 **보통 굵기로 내려간다**
        /// (정본 8624~8631 이 스스로 «한 단 내려가려면 500 이어야 한다» 고 적었다). 클론은 `FontStyles.Bold` 를 박아 두어 정본보다 굵었다.</summary>
        [UnityTest]
        public IEnumerator 패스_안내문은_정본_8633_대로_regular_다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            Assert.IsNotNull(h, "메타");
            PassPopup.Open(h);   // 기존 자(BossWarnArtTests)가 쓰는 그 길
            yield return null; yield return null;
            Popup p = h.Popups.Find(PassPopup.Name);
            Assert.IsNotNull(p, "패스 팝업이 열렸다");
            Transform t = p.Root.Find("card/desc-row/desc");
            Assert.IsNotNull(t, "안내문(card/desc-row/desc)");
            TextMeshProUGUI d = t.GetComponent<TextMeshProUGUI>();
            Assert.IsNotNull(d, "안내문 글자");
            Assert.IsFalse((d.fontStyle & FontStyles.Bold) != 0,
                "«" + d.text.Replace("\n", " ") + "» 는 regular 다 — 2734 의 800 을 8633 이 500 으로 덮고 500 은 보통 굵기로 내려간다");
            h.Popups.Hide(PassPopup.Name);
        }

        /// <summary>T352 12회차 — **버튼 잔글씨**. 정본은 `<button>판매<small>🪙 +N</small></button>`(ui.js 3266)이고
        /// **667** `.btn small { font-weight: 400 }` 이 그 잔글씨만 보통 굵기로 둔다. 클론은 `IconTextStack.ReplaceLabel` 이
        /// **모든 줄에 Bold 를 박아** 둘째 줄까지 굵었다 — 곧 이 자리는 «남의 lock 뒤» 가 아니라 **공용 도우미 한 곳**이 쥐고 있었다.
        /// 첫 줄(본문)은 그대로 bold 여야 한다 — 두 줄의 굵기가 **서로 달라야** 정본과 같다.</summary>
        [UnityTest]
        public IEnumerator 버튼_잔글씨는_첫_줄과_달리_regular_다()
        {
            yield return Boot();
            ForgeHost F = ForgeHost.Instance;
            ForgeItem it = F.Engine.RollItem();
            ForgeCraftPopup.Show(F, it);
            yield return null; yield return null;
            Popup p = F.Meta.Popups.Find(ForgeCraftPopup.Name);
            Assert.IsNotNull(p, "비교 팝업이 열렸다");
            Transform stack = p.Root.Find("card/lower/row/sell/label-stack");
            Assert.IsNotNull(stack, "판매 버튼의 줄 상자(label-stack)");
            var lines = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < stack.childCount; i++)
                if (stack.GetChild(i).name.StartsWith("line-")) lines.Add(stack.GetChild(i));
            Assert.GreaterOrEqual(lines.Count, 2, "정본처럼 본문 + 잔글씨 두 줄이다(ui.js 3266)");
            foreach (TextMeshProUGUI t in UiKit.RowTexts(lines[0] as RectTransform))
                Assert.IsTrue((t.fontStyle & FontStyles.Bold) != 0, "첫 줄(«" + t.text + "»)은 bold 다 — 버튼 본문");
            int sub = 0;
            for (int i = 1; i < lines.Count; i++)
                foreach (TextMeshProUGUI t in UiKit.RowTexts(lines[i] as RectTransform))
                {
                    sub++;
                    Assert.IsFalse((t.fontStyle & FontStyles.Bold) != 0,
                        "잔글씨(«" + t.text + "»)는 regular 다 — 정본 667 `.btn small { font-weight: 400 }`");
                }
            Assert.Greater(sub, 0, "잔글씨 줄에 글자가 있다");
            ForgeCraftPopup.Hide(F);
            yield return null;
        }

        /// <summary>실물 자리 — 장비 목록 칸 아래 «0.0000%» 라벨은 regular 다(정본 790 은 800 이지만 **8633** 이 500 으로 덮는다).
        /// 지금은 공장 기본이 regular 라 «우연히» 맞는 값이지만, `ForgeInfoPopup` 이 표로 못 박아 두었으므로
        /// 공장 기본이 bold 로 뒤집혀도 이 자리는 regular 로 남는다 — 그것을 여기서 잰다.</summary>
        [UnityTest]
        public IEnumerator 장비_목록_확률_라벨은_정본_8633_대로_regular_다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeInfoPopup.OpenList(h);
            yield return null; yield return null;
            Popup p = h.Meta.Popups.Find(ForgeInfoPopup.Name) ?? PopupLayer.Instance.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(p, "장비 목록 팝업이 열렸다");
            int seen = 0;
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.name != "pct") continue;
                seen++;
                Assert.IsFalse((t.fontStyle & FontStyles.Bold) != 0,
                    "«" + t.text + "» 는 regular 다 — 정본 790 `font-weight: 800` 을 **8633** 이 500 으로 덮고, 폴백 sans 는 두 축뿐이라 500 은 보통 굵기로 내려간다");
            }
            Assert.Greater(seen, 0, "확률 라벨을 찾았다(자리 자체가 사라지면 이 칸이 먼저 운다)");
        }
    }
}
