using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T453 — 정본이 **글자**에 건 `text-shadow` 를 클론이 **아이콘**으로 세운 세 자리에 <see cref="IconShadow"/> 로 옮겼는가:
    /// 채팅 [뒤로] ◀(3283 · 8방 1px #000 링 = 오프셋 사본 8장) · 켜진 ✓(4978 · `0 .06rem .1rem rgba(0,0,0,.6)` = 번진 사본 한 장) ·
    /// 자동 제련 시대 막대 ★(4858 · 대각 4방 1px **#17181a** — 확률 정보의 #000 과 다르다 · 별은 글자라 SDF 스트로크).
    /// 값은 표 `IconShadowUi.json` 에서 읽어 견준다 — 사본 수·색·오프셋·형제 순서(원본이 맨 뒤 = 맨 앞에 그려진다).
    /// </summary>
    public class IconShadowTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null && UiRoot.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다");
            Assert.IsNotNull(UiRoot.Instance, "UiRoot 가 서지 않았다");
            yield return null;
        }

        private static List<Image> Named(Transform root, string name, string parentName = null)
        {
            List<Image> r = new List<Image>();
            foreach (Image i in root.GetComponentsInChildren<Image>(true))
                if (i.name == name && (parentName == null || (i.transform.parent != null && i.transform.parent.name == parentName))) r.Add(i);
            return r;
        }

        /// <summary>켜진 체크표의 ✓ 둘 — 「계속하기」 상자(`af-check-continue`)와 부옵션 행의 상자(`check`). 닫기 버튼(`x-btn`)의 «mark»(xmark)는 다른 그림이라 뺀다(런 1094 가 그것을 잡았다).</summary>
        private static List<Image> CheckMarks(Transform root)
        {
            List<Image> r = Named(root, "mark", "af-check-continue");
            r.AddRange(Named(root, "mark", "check"));
            return r;
        }

        [UnityTest]
        public IEnumerator 채팅_뒤로_삼각_아이콘_뒤에_표의_8방_링_사본이_깔린다()
        {
            yield return Boot();
            MetaHost.Instance.OpenChat();
            yield return null;
            Popup p = MetaHost.Instance.Popups.Find(ChatScreen.Name);
            Assert.IsNotNull(p, "채팅 화면이 안 열렸다");
            List<Image> tris = Named(p.Root, "tri");
            Assert.AreEqual(1, tris.Count, "[뒤로] 의 ◀ 는 하나");
            Image tri = tris[0];
            Assert.IsNotNull(tri.sprite, "◀ 는 아틀라스 아이콘(tri_left)이어야 한다 — 스프라이트 없음");
            List<Vector2> offs = IconShadowUi.RingOffsets("chat_back");
            Assert.AreEqual(8, offs.Count, "표 rings.chat_back = 정본 3283 의 8방");
            Color want = IconShadowUi.RingColor("chat_back");
            float css = KeylineUi.CssPx;
            Transform parent = tri.transform.parent;
            int triIdx = tri.transform.GetSiblingIndex();
            for (int i = 0; i < offs.Count; i++)
            {
                Transform t = parent.Find(IconShadow.RingNameFor(tri, i));
                Assert.IsNotNull(t, "링 사본 " + i + " 이 없다");
                Image cp = t.GetComponent<Image>();
                Assert.AreSame(tri.sprite, cp.sprite, "사본 " + i + ": 같은 그림");
                Assert.IsFalse(cp.raycastTarget, "사본 " + i + ": 클릭을 안 먹는다");
                Assert.Less(t.GetSiblingIndex(), triIdx, "사본 " + i + ": 원본 **뒤**(형제 순서 앞)에 선다");
                Assert.AreEqual(want.r, cp.color.r, 1e-3f, "사본 " + i + " R"); Assert.AreEqual(want.g, cp.color.g, 1e-3f, "사본 " + i + " G");
                Assert.AreEqual(want.b, cp.color.b, 1e-3f, "사본 " + i + " B"); Assert.AreEqual(want.a, cp.color.a, 1e-3f, "사본 " + i + " A");
                Vector2 d = cp.rectTransform.anchoredPosition - tri.rectTransform.anchoredPosition;
                Assert.AreEqual(offs[i].x * css, d.x, 0.01f, "사본 " + i + ": dx = 표 px × css_px");
                Assert.AreEqual(-offs[i].y * css, d.y, 0.01f, "사본 " + i + ": dy = 표 px × css_px(CSS 아래 + → 캔버스 위 +)");
                Assert.AreEqual(tri.rectTransform.sizeDelta, cp.rectTransform.sizeDelta, "사본 " + i + ": 같은 크기");
            }
        }

        [UnityTest]
        public IEnumerator 자동_제련_켜진_체크의_아이콘_아래에_표의_흐린_그림자_사본이_깔린다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            float t0 = 0f;
            while (!ForgeHost.Ready && t0 < 20f) { t0 += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            h.S.BestChapter = 3; h.S.BestStage = 1; h.S.ForgeLevel = 29; h.Pull();
            ForgeAutoPopup.Open(h);
            yield return null; yield return null;
            Popup auto = h.Meta.Popups.Find(ForgeAutoPopup.Name);
            Assert.IsNotNull(auto, "자동 제련 팝업");
            List<Image> marks = CheckMarks(auto.Root);
            if (marks.Count == 0)
            {
                h.ToggleStopOnTarget();   // «계속하기» 체크를 켠다(켜졌을 때만 ✓ 가 선다)
                yield return null; yield return null;
                auto = h.Meta.Popups.Find(ForgeAutoPopup.Name);
                marks = CheckMarks(auto.Root);
            }
            Assert.Greater(marks.Count, 0, "켜진 ✓ 가 하나는 있어야 잰다");
            Color want = IconShadowUi.ShadowColor("af_check_on");
            float rem = PopupKit.Rem;
            foreach (Image mk in marks)
            {
                Transform parent = mk.transform.parent;
                Transform t = parent.Find(IconShadow.DropNameFor(mk));
                Assert.IsNotNull(t, mk.transform.parent.name + ": ✓ 아래 그림자 사본이 없다");
                Image sh = t.GetComponent<Image>();
                Assert.IsNotNull(sh.sprite, "그림자 사본에 그림이 없다");
                Assert.AreNotSame(mk.sprite, sh.sprite, "흐림 반지름 .1rem 이 0 이 아니니 **번진 판**이어야 한다(원본 그대로면 안 번진 것)");
                Assert.IsFalse(sh.raycastTarget, "그림자는 클릭을 안 먹는다");
                Assert.Less(t.GetSiblingIndex(), mk.transform.GetSiblingIndex(), "그림자는 ✓ **뒤**에 선다");
                Assert.AreEqual(want.r, sh.color.r, 1e-3f, "그림자 R"); Assert.AreEqual(want.g, sh.color.g, 1e-3f, "그림자 G");
                Assert.AreEqual(want.b, sh.color.b, 1e-3f, "그림자 B"); Assert.AreEqual(want.a, sh.color.a, 1e-3f, "그림자 alpha = .6");
                // 피벗이 (½,½) 이라 여유 보정이 0 — 가운데 차이가 곧 오프셋이다.
                Vector2 d = sh.rectTransform.anchoredPosition - mk.rectTransform.anchoredPosition;
                Assert.AreEqual(IconShadowUi.ShadowRem("af_check_on", "dx_rem") * rem, d.x, 0.05f, "dx = 0");
                Assert.AreEqual(-IconShadowUi.ShadowRem("af_check_on", "dy_rem") * rem, d.y, 0.05f, "dy = .06rem 아래로");
                Assert.GreaterOrEqual(sh.rectTransform.sizeDelta.x, mk.rectTransform.sizeDelta.x, "번진 판의 상자는 원본보다 작지 않다(번짐이 상자 끝에서 안 잘린다)");
            }
        }

        [UnityTest]
        public IEnumerator 시대_막대_별_링은_자동_제련에서만_잉크색이고_두께는_표값이다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            float t0 = 0f;
            while (!ForgeHost.Ready && t0 < 20f) { t0 += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            h.S.BestChapter = 3; h.S.BestStage = 1; h.S.ForgeLevel = 29; h.Pull();
            ForgeAutoPopup.Open(h);
            yield return null; yield return null;
            Popup auto = h.Meta.Popups.Find(ForgeAutoPopup.Name);
            Assert.IsNotNull(auto, "자동 제련 팝업");
            // 시대 키는 팝업이 세운 막대 이름(«af-age-<age>»)에서 읽는다 — 표본 세이브에 별이 없어도 막대는 직접 세워 잰다.
            string age = null;
            foreach (Transform tr in auto.Root.GetComponentsInChildren<Transform>(true))
                if (tr.name.StartsWith("af-age-")) { age = tr.name.Substring("af-age-".Length); break; }
            Assert.IsNotNull(age, "자동 제련 팝업에 시대 막대가 하나는 서야 한다");
            RectTransform box = UiKit.Box(UiRoot.Instance.App, "t453-agebar");
            float w = UiKit.RefW * 0.6f, bh = PopupKit.Rem * 2f;
            RectTransform a = ForgeUi.AgeBar(box, "auto", w, bh, h.Defs, age, "1%", null, 2, null, null, true);
            RectTransform f = ForgeUi.AgeBar(box, "info", w, bh, h.Defs, age, "1%", "2%", 2, null, null, false);
            yield return null;
            TextMeshProUGUI sa = a.Find("star").GetComponent<TextMeshProUGUI>(), sf = f.Find("star").GetComponent<TextMeshProUGUI>();
            Color ca = sa.fontMaterial.GetColor("_OutlineColor"), cf = sf.fontMaterial.GetColor("_OutlineColor");
            Color ink = UiKit.C("pp_ink"), line = UiKit.C("pp_line");
            Assert.AreEqual(ink.r, ca.r, 2f / 255f, "자동 제련 ★ 링 R = pp_ink(#17181a · 정본 4858)"); Assert.AreEqual(ink.g, ca.g, 2f / 255f, "자동 제련 ★ 링 G"); Assert.AreEqual(ink.b, ca.b, 2f / 255f, "자동 제련 ★ 링 B");
            Assert.AreEqual(line.r, cf.r, 2f / 255f, "확률 정보 ★ 링 = pp_line(#000 · 정본 5138) — 그대로"); Assert.AreEqual(line.g, cf.g, 2f / 255f, "확률 정보 ★ 링 G"); Assert.AreEqual(line.b, cf.b, 2f / 255f, "확률 정보 ★ 링 B");
            Assert.Greater(sa.outlineWidth, 0f, "자동 제련 ★: 링이 실제로 켜져 있다");
            Material m = sa.fontMaterial;
            OutlineSdf o = OutlineSdf.FromStroke(IconShadowUi.RingPx("af_age_star"), sa.fontSize, m.GetFloat("_GradientScale"),
                m.HasProperty("_ScaleRatioA") ? m.GetFloat("_ScaleRatioA") : 1f, sa.font.faceInfo.pointSize);
            Assert.AreEqual((float)o.Width01, m.GetFloat("_OutlineWidth"), 1e-3f, "자동 제련 ★: 링 두께 = 표 rings.af_age_star(1 CSS px) × css_px 의 SDF 환산");
            Assert.AreEqual(IconShadowUi.RingPx("af_age_star"), TextShadowUi.RingPx("fi_age_star"), 1e-3f, "두 별의 두께는 같다(정본 둘 다 1px) — 색만 다르다");
            Object.Destroy(box.gameObject);
        }
    }
}
