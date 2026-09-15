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
            // ⚠ 시트는 **열어야 그려진다** — 런 469 에서 «탭을 안 눌러도 서 있다» 고 보고 600프레임을 기다리다 빨갰다.
            //   상점은 자에서 여는 길이 하나(`MetaHost.OpenShop` · 촬영 자도 그 길로 연다)라 여기서 쓴다.
            MetaHost.Instance.OpenShop();
            TextMeshProUGUI title = null;
            for (int i = 0; i < 600 && title == null; i++)
            {
                yield return null;
                Popup sheet = MetaHost.Instance.Popups.Find(ShopSheet.Name);
                if (sheet == null) continue;
                foreach (TextMeshProUGUI t in sheet.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
                    if (t.name == "title") { title = t; break; }
            }
            Assert.IsNotNull(title, "상점 시트 제목이 600프레임 안에 안 섰다");
            Canvas.ForceUpdateCanvases();
            AssertShadow(title, "paper_emboss", "상점 시트 제목");
            // 정본 규칙의 뜻이 «흰 엠보스» 다 — 색이 어두우면 옮긴 것이 아니다.
            Color c = title.fontMaterial.GetColor("_UnderlayColor");
            Assert.Greater(c.r + c.g + c.b, 2.7f, "엠보스는 흰색이어야 한다(정본 rgba(255,255,255,.92))");
            yield return null;
        }

        /// <summary>
        /// T333 3회차 — 정본 8392 `.league-row .league-name, .league-row .league-rank, .league-score … { 0 1px 1px rgba(0,0,0,.75), 0 0 6px rgba(0,0,0,.35) }`.
        /// 두 겹인데 TMP 언더레이는 한 겹뿐이라 **읽히게 만드는 첫 겹**(아래 1px 드롭)만 낸다(표 `league_row` 주석). 같은 글자의 2px 키라인(T109 5회차 · 8403)은
        /// 같은 재질 인스턴스에 얹히니 **둘 다 살아 있어야** 한다 — 하나가 다른 하나를 지우면 그것이 이 자의 빨강이다.
        /// </summary>
        [UnityTest]
        public IEnumerator 리그_행_글자는_키라인_위에_정본_첫_겹_그림자를_더_받는다()
        {
            yield return Boot();
            MetaHost.Instance.OpenLeague();
            yield return null;
            Popup p = MetaHost.Instance.Popups.Find(LeagueSheet.Name);
            Assert.IsNotNull(p, "리그 시트가 안 열렸다");
            int rows = 0;
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.transform.parent == null || !t.transform.parent.name.StartsWith("row-", System.StringComparison.Ordinal)) continue;
                if (t.name != "rank" && t.name != "name") continue;
                rows++;
                AssertShadow(t, "league_row", "리그 행 «" + t.name + "»");
                Assert.Greater(t.outlineWidth, 0f, "리그 행 «" + t.name + "»: 정본 8403 의 2px 키라인이 그림자에 지워지면 안 된다");
                Assert.Less(t.fontMaterial.GetFloat("_UnderlayOffsetY"), 0f, "리그 행 «" + t.name + "»: 그림자는 아래로(CSS 0 1px)");
            }
            Assert.Greater(rows, 1, "랭킹 행의 순위·이름 글자를 못 찾았다");
            yield return null;
        }

        /// <summary>
        /// T333 3회차 ⓒ — 정본 3344 `.chat-name, .chat-tag` 의 20겹 링. 정본 주석이 «1px 8방향으로는 절반밖에 안 나오므로 2px 링을 겹쳐 두른다» 고 적어 둔
        /// **변당 2px 순검정**이라, 언더레이 한 겹이 아니라 SDF 스트로크(`UiKit.OutlinePx` · T104)로 낸다. 두께는 표 `rings.chat_name`(CSS px) ×
        /// <see cref="KeylineUi.CssPx"/> 이고, 링이므로 언더레이는 안 켜져 있어야 한다(켜 두면 같은 자리에 겹이 둘이 된다).
        /// </summary>
        [UnityTest]
        public IEnumerator 채팅_닉네임은_20겹_링을_변당_2px_스트로크로_낸다()
        {
            yield return Boot();
            MetaHost.Instance.OpenChat();
            yield return null;
            Popup p = MetaHost.Instance.Popups.Find(ChatScreen.Name);
            Assert.IsNotNull(p, "채팅 화면이 안 열렸다");
            int names = 0;
            float want = TextShadowUi.RingPx("chat_name");
            Assert.Greater(want, 4f, "표 rings.chat_name(2 CSS px) × css_px(2.164) 는 4 캔버스 px 를 넘어야 한다");
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.name != "name" || t.transform.parent == null || t.transform.parent.name != "name-line") continue;
                names++;
                Material m = t.fontMaterial;
                OutlineSdf o = OutlineSdf.FromStroke(want, t.fontSize, m.GetFloat("_GradientScale"),
                    m.HasProperty("_ScaleRatioA") ? m.GetFloat("_ScaleRatioA") : 1f, t.font.faceInfo.pointSize);
                Assert.AreEqual((float)o.Width01, m.GetFloat("_OutlineWidth"), 1e-3f, "닉네임 «" + t.text + "»: 링 두께 = 표 × css_px 의 SDF 환산");
                Assert.Greater(t.outlineWidth, 0f, "닉네임 «" + t.text + "»: 링이 실제로 켜져야 한다");
                Color oc = m.GetColor("_OutlineColor");
                Assert.Less(oc.r + oc.g + oc.b, 0.1f, "닉네임 «" + t.text + "»: 링은 순검정(정본 #000 · 카탈로그 pp_line)");
                Assert.IsFalse(m.IsKeywordEnabled("UNDERLAY_ON"), "닉네임 «" + t.text + "»: 링 자리에 언더레이까지 켜면 겹이 둘이 된다");
            }
            Assert.Greater(names, 1, "채팅 목록의 닉네임을 못 찾았다");
            yield return null;
        }
        /// <summary>
        /// T333 5회차 — 정본 5375 `.dgclear-title { text-shadow: 0 2px 0 #b8860b, 0 4px 10px rgba(0,0,0,.55) }` 의 첫 겹(금색 양각). 던전을 실제로 클리어해
        /// 팝업을 띄우고(DungeonFxTests 와 같은 길) 제목 글자의 재질을 표(`dgclear_title`)와 식으로 맞춘다. T168 의 자간과 같은 글자에 얹힌다.
        /// </summary>
        [UnityTest]
        public IEnumerator 던전_클리어_제목은_정본_금색_양각_한_겹을_쓴다()
        {
            yield return Boot();
            float w = 0f;
            while (!DungeonUiHost.Ready && w < 20f) { w += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(DungeonUiHost.Ready, "DungeonUiHost 가 20초 안에 준비되지 않았다");
            DungeonUiHost h = DungeonUiHost.Instance;
            h.S.BestChapter = 5; h.S.BestStage = 1;
            UiRoot.Instance.TabBar.OnTab("dungeon");
            yield return null;
            DungeonDetailPopup.Open("hammer");
            yield return null;
            DungeonDetailPopup.Enter();
            yield return null;
            h.Dungeons.OnClear();
            yield return null;
            Assert.IsTrue(DungeonClearPopup.IsOpen, "onClear → showDungeonClear");
            TextMeshProUGUI title = null;
            foreach (TextMeshProUGUI t in UiRoot.Instance.App.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.name == "title" && t.transform.parent != null && t.transform.parent.name == "gold") { title = t; break; }
            Assert.IsNotNull(title, "클리어 팝업 제목(gold/title)을 못 찾았다");
            AssertShadow(title, "dgclear_title", "던전 클리어 제목");
            Assert.Less(title.fontMaterial.GetFloat("_UnderlayOffsetY"), 0f, "양각은 아래로(CSS 0 2px)");
            Assert.AreEqual(0f, title.fontMaterial.GetFloat("_UnderlaySoftness"), 1e-6, "하드 겹(흐림 0)");
            DungeonClearPopup.Close();
            yield return null;
        }

        /// <summary>
        /// T333 5회차 — 보스 워닝: 정본 402 `.bw-track span`(세 겹) · 411 `.bw-sub`(두 겹) 중 «아래 2px 검정 하드 드롭» 한 겹. 마퀴는 T104 의 붉은 키라인과
        /// 같은 재질에 얹히므로 키라인이 남아 있어야 한다(리그 행 자와 같은 단언).
        /// </summary>
        [UnityTest]
        public IEnumerator 보스_워닝_마퀴와_부제는_정본_아래_2px_검정_한_겹을_쓴다()
        {
            Forge.Game.Battle.BattleScene.AutoBoot = false;
            yield return Boot();
            BattleOverlay ov = BattleOverlay.Ensure();
            Assert.IsNotNull(ov, "오버레이가 없다");
            ov.BossWarning(2.0);
            yield return null;
            TextMeshProUGUI marquee = null, sub = null;
            foreach (TextMeshProUGUI t in ov.Layer.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.name == "text" && t.transform.parent != null && t.transform.parent.name == "bw-track") marquee = t;
                else if (t.name == "bw-sub") sub = t;
            }
            Assert.IsNotNull(marquee, "마퀴 글자(bw-track/text)를 못 찾았다");
            Assert.IsNotNull(sub, "부제(bw-sub)를 못 찾았다");
            AssertShadow(marquee, "bw_marquee", "보스 워닝 마퀴");
            Assert.Greater(marquee.outlineWidth, 0f, "마퀴: 붉은 키라인(T104)이 그림자에 지워지면 안 된다");
            AssertShadow(sub, "bw_sub", "보스 워닝 부제");
            Assert.Less(sub.fontMaterial.GetFloat("_UnderlayOffsetY"), 0f, "부제: 아래로(CSS 0 2px)");
            yield return null;
        }
    }
}
