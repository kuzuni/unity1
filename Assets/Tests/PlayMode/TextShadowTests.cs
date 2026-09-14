using System.Collections;
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
    /// T333 1회차 — 공용 <see cref="PopupKit.Btn"/> 라벨의 글자 그림자(정본 `text-shadow` → TMP 언더레이 · 표 `TextShadowUi.json`)가 cascade 대로 걸리는가:
    /// 색 버튼(카드·패널 안 8504 `0 1px 1px rgba(4,18,52,.62)` 가 8719 none 을 이긴다) = 남색 한 겹 · 색 버튼 비활성 = 없음(8727) · 회색 비활성 = 흰 엠보스(8356) ·
    /// 회색·종이·디버그 면(.silver 계열) = 없음 · 소환 버튼(8661 none) = 없음. 값은 <see cref="UnderlaySdf"/> 식(재질 G·R_C · 폰트 샘플링 · 글자 크기)과 같아야 한다.
    /// </summary>
    public class TextShadowTests
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

        private static TextMeshProUGUI Label(Button b)
        {
            Transform t = b.transform.Find("label");
            Assert.IsNotNull(t, b.name + ": 라벨이 없다");
            return t.GetComponent<TextMeshProUGUI>();
        }

        private static void AssertShadow(TextMeshProUGUI t, string key, string what)
        {
            Material m = t.fontMaterial;
            Assert.IsTrue(m.IsKeywordEnabled("UNDERLAY_ON"), what + ": UNDERLAY_ON");
            float g = m.GetFloat("_GradientScale"), rc = m.HasProperty("_ScaleRatioC") ? m.GetFloat("_ScaleRatioC") : 0f;
            if (rc <= 0f) rc = 1f;
            float css = KeylineUi.CssPx;
            UnderlaySdf want = UnderlaySdf.FromPx(TextShadowUi.Px(key, "dx_px") * css, TextShadowUi.Px(key, "dy_px") * css, TextShadowUi.Px(key, "blur_px") * css, t.fontSize, g, rc, t.font.faceInfo.pointSize);
            Assert.IsFalse(want.Clipped, what + ": 표 값이 여백 안이어야 한다(단위 " + want.UnitPx.ToString("0.0") + "px)");
            Assert.AreEqual(want.OffsetX01, m.GetFloat("_UnderlayOffsetX"), 1e-4, what + ": _UnderlayOffsetX = 식");
            Assert.AreEqual(want.OffsetY01, m.GetFloat("_UnderlayOffsetY"), 1e-4, what + ": _UnderlayOffsetY = 식(CSS 아래 = TMP 음수)");
            Assert.AreEqual(want.Softness01, m.GetFloat("_UnderlaySoftness"), 1e-4, what + ": _UnderlaySoftness = 흐림/단위");
            Assert.AreEqual(0f, m.GetFloat("_UnderlayDilate"), 1e-6, what + ": _UnderlayDilate 0");
            Color c = m.GetColor("_UnderlayColor"), tc = TextShadowUi.C(key);
            Assert.AreEqual(tc.r, c.r, 2f / 255f, what + ": 그림자 R"); Assert.AreEqual(tc.g, c.g, 2f / 255f, what + ": 그림자 G");
            Assert.AreEqual(tc.b, c.b, 2f / 255f, what + ": 그림자 B"); Assert.AreEqual(tc.a, c.a, 2f / 255f, what + ": 그림자 alpha");
            double dx, dy, blur;
            UnderlaySdf.ToPx(m.GetFloat("_UnderlayOffsetX"), m.GetFloat("_UnderlayOffsetY"), m.GetFloat("_UnderlaySoftness"), want.UnitPx, out dx, out dy, out blur);
            Assert.AreEqual(TextShadowUi.Px(key, "dy_px") * css, dy, 0.05, what + ": 되짚은 dy = 정본 px × css_px(아래로)");
        }

        private static void AssertNoShadow(TextMeshProUGUI t, string what)
        {
            Assert.IsFalse(t.fontMaterial.IsKeywordEnabled("UNDERLAY_ON"), what + ": 그림자가 없어야 한다");
        }

        [UnityTest]
        public IEnumerator 색_버튼_라벨은_남색_한_겹_회색_비활성은_흰_엠보스_그_밖은_없다()
        {
            yield return Boot();
            RectTransform host = UiKit.Box(UiRoot.Instance.App, "t333-btn-host");
            try
            {
                float w = UiKit.L("league_challenge_w") * UiRoot.Instance.App.rect.width, h = UiKit.H("btn_h");
                string[] faces = { "pp_blue", "pp_green", "pp_red" };
                string[] lips = { "pp_blue_dk", "pp_green_dk", "pp_red_dk" };
                for (int i = 0; i < faces.Length; i++)
                {
                    TextMeshProUGUI t = Label(PopupKit.Btn(host, "b-" + faces[i], "확인", faces[i], lips[i], null, w, h));
                    AssertShadow(t, "btn_label", faces[i] + "(색 버튼 · 8504)");
                    Assert.Greater(t.outlineWidth, 0f, faces[i] + ": 키라인도 그대로(T109 7회차 · 같은 재질 인스턴스)");
                    Assert.Less(t.fontMaterial.GetFloat("_UnderlayOffsetY"), 0f, faces[i] + ": 그림자는 아래로");
                }
                TextMeshProUGUI gray = Label(PopupKit.Btn(host, "b-gray", "취소", "pp_gray", "pp_gray_dk", null, w, h, "pp_ink"));
                AssertNoShadow(gray, "회색 .btn(.silver 계열)");
                TextMeshProUGUI dbg = Label(PopupKit.Btn(host, "b-dbg", "이동", "card_bg", "pp_line", null, w, h, "ink"));
                AssertNoShadow(dbg, "표에 없는 면(디버그)");
                TextMeshProUGUI offColored = Label(PopupKit.Btn(host, "b-off-c", "수령", "pp_green", "pp_green_dk", null, w, h, "stage_ink", TextKind.Button, true));
                AssertNoShadow(offColored, "색 버튼 비활성(8727 none)");
                TextMeshProUGUI offGray = Label(PopupKit.Btn(host, "b-off-g", "닫힘", "pp_gray", "pp_gray_dk", null, w, h, "pp_ink", TextKind.Button, true));
                AssertShadow(offGray, "btn_label_disabled", "회색 비활성(8356 흰 엠보스)");
                Assert.AreEqual(0f, offGray.fontMaterial.GetFloat("_UnderlaySoftness"), 1e-6, "엠보스는 흐림 0");
                TextMeshProUGUI summon = Label(PopupKit.Btn(host, "b-summon", "소환", "pp_blue", "pp_blue_dk", null, w, h, "stage_ink", TextKind.Button, false, "summon_btn"));
                AssertNoShadow(summon, "소환 버튼(8661 none)");
                Assert.Greater(summon.outlineWidth, 0f, "소환 버튼의 4px 키라인은 그대로");
            }
            finally { Object.Destroy(host.gameObject); }
            yield return null;
        }

        /// <summary>
        /// T333 2회차 — 정본 8381 의 한 벌: **밝은 종이 위 글자는 흰 엠보스**(`0 1px 0 rgba(255,255,255,.92)`).
        /// 시트 제목이 그 규칙의 첫 선택자(`.sheet-title`)다. 공용 자리(`UiKit`·`Popups.cs`·`QuestSheet`)는 남의 lock 이라
        /// 이번 회차는 **시트 제목 다섯 자리**만 걸었다(펫·스킬·탈것·상점·승천) — 나머지 선택자는 그 lock 들이 풀린 뒤다.
        /// </summary>
        [UnityTest]
        public IEnumerator 시트_제목은_정본_흰_엠보스_한_겹을_쓴다()
        {
            yield return Boot();
            // 시트는 탭을 안 눌러도 서 있다(꺼져 있을 뿐) — 재질은 꺼진 칸에서도 읽힌다.
            TextMeshProUGUI title = null;
            for (int i = 0; i < 600 && title == null; i++)
            {
                foreach (TextMeshProUGUI t in UiRoot.Instance.App.GetComponentsInChildren<TextMeshProUGUI>(true))
                    if (t.name == "sheet-title") { title = t; break; }
                if (title == null) yield return null;
            }
            Assert.IsNotNull(title, "시트 제목(sheet-title)이 600프레임 안에 안 섰다");
            Canvas.ForceUpdateCanvases();
            AssertShadow(title, "paper_emboss", "시트 제목");
            // 정본 규칙의 뜻이 «흰 엠보스» 다 — 색이 어두우면 옮긴 것이 아니다.
            Color c = title.fontMaterial.GetColor("_UnderlayColor");
            Assert.Greater(c.r + c.g + c.b, 2.7f, "엠보스는 흰색이어야 한다(정본 rgba(255,255,255,.92))");
            yield return null;
        }
    }
}
