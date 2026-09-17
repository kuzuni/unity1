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

        /// <summary>T333 15회차 — 정본 흐림이 글꼴 SDF 여백(단위 px)을 넘는 자리도 잰다: 재질 값은 «식을 상한 1 에서 자른 값» 이어야 하고, 안 잘린 자리는 AssertShadow 와 같다(결정 738).</summary>
        private static void AssertShadowMaybeClipped(TextMeshProUGUI t, string key, string what)
        {
            Material m = t.fontMaterial;
            Assert.IsTrue(m.IsKeywordEnabled("UNDERLAY_ON"), what + ": UNDERLAY_ON");
            float g = m.GetFloat("_GradientScale"), rc = m.HasProperty("_ScaleRatioC") ? m.GetFloat("_ScaleRatioC") : 0f;
            if (rc <= 0f) rc = 1f;
            // T333 16회차 — TMP 는 언더레이 값을 넣은 **뒤** `ShaderUtilities.UpdateShaderRatios` 로 `_ScaleRatioC` 를 다시 센다:
            //   ratio_C = (G − 1) / (G × max(1, max(|offX|, |offY|) + dilate + softness)).
            // 그러니 «걸 때 읽은 R_C»(UiKit.TextShadow 가 식에 넣은 값)와 «지금 읽는 R_C» 는 상한을 넘긴 자리에서 그 max(1, …) 배만큼 다르다
            // (런 987·999 실측: 제목 −0.582 기대 ↔ −0.412 실물 = 1/(0.412 + 1.0) · 정확히 그 배). 안 잘린 자리는 max 가 1 이라 같다(AssertShadow 가 초록인 까닭).
            // 식을 되짚으려면 지금 값에서 그 배를 곱해 «걸 때의 R_C» 로 돌린다 — 재질 값(잘린 그림자의 실제 렌더 크기)은 안 건드린다(결정 738 그대로).
            float offX = m.GetFloat("_UnderlayOffsetX"), offY = m.GetFloat("_UnderlayOffsetY"), soft = m.GetFloat("_UnderlaySoftness"), dil = m.GetFloat("_UnderlayDilate");
            float ratioCt = Mathf.Max(1f, Mathf.Max(Mathf.Abs(offX), Mathf.Abs(offY)) + dil + soft);
            float rcApply = rc * ratioCt;
            float css = KeylineUi.CssPx;
            UnderlaySdf want = UnderlaySdf.FromPx(TextShadowUi.Px(key, "dx_px") * css, TextShadowUi.Px(key, "dy_px") * css, TextShadowUi.Px(key, "blur_px") * css, t.fontSize, g, rcApply, t.font.faceInfo.pointSize);
            Assert.AreEqual(want.OffsetX01, offX, 1e-4, what + ": _UnderlayOffsetX = 식(걸 때의 R_C 로)");
            Assert.AreEqual(want.OffsetY01, offY, 1e-4, what + ": _UnderlayOffsetY = 식(CSS 아래 = TMP 음수 · 걸 때의 R_C 로)");
            Assert.AreEqual(want.Softness01, soft, 1e-4, what + ": _UnderlaySoftness = 흐림/단위(상한 1)");
            if (want.Clipped) Assert.Greater(ratioCt, 1f, what + ": 잘린 자리는 TMP 가 R_C 를 줄였어야 한다(max(1, |off| + softness) > 1)");
            else Assert.AreEqual(1f, ratioCt, 1e-6, what + ": 안 잘린 자리는 R_C 가 그대로다");
            Assert.Less(offY, 0f, what + ": 그림자는 아래로 내린다");
            Assert.Greater(m.GetFloat("_UnderlaySoftness"), 0f, what + ": 흐림이 있는 겹이다");
            Color c = m.GetColor("_UnderlayColor"), tc = TextShadowUi.C(key);
            Assert.AreEqual(tc.r, c.r, 2f / 255f, what + ": 그림자 R"); Assert.AreEqual(tc.g, c.g, 2f / 255f, what + ": 그림자 G");
            Assert.AreEqual(tc.b, c.b, 2f / 255f, what + ": 그림자 B"); Assert.AreEqual(tc.a, c.a, 2f / 255f, what + ": 그림자 alpha");
            if (!want.Clipped)
            {
                double dx, dy, blur;
                UnderlaySdf.ToPx(m.GetFloat("_UnderlayOffsetX"), m.GetFloat("_UnderlayOffsetY"), m.GetFloat("_UnderlaySoftness"), want.UnitPx, out dx, out dy, out blur);
                Assert.AreEqual(TextShadowUi.Px(key, "dy_px") * css, dy, 0.05, what + ": 되짚은 dy = 정본 px × css_px(아래로)");
            }
            else Debug.Log("[T333] " + what + ": 정본 흐림 " + (TextShadowUi.Px(key, "blur_px") * css).ToString("0.0") + "px 이 SDF 단위 " + want.UnitPx.ToString("0.0") + "px 를 넘어 상한에서 잘린다(결정 738) · TMP 가 R_C 를 " + rcApply.ToString("0.000") + " → " + rc.ToString("0.000") + " 로 줄여 실제 그림자는 그 배만큼 더 작다(16회차)");
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
            // 런 547 수리: `DungeonPopups.Bordered` 는 테 상자(gold)의 **안쪽 면**을 돌려주므로 제목의 부모 이름은 gold 가 아니다 — 팝업 뿌리 안의 «title» 로 찾는다.
            Transform root = null;
            foreach (Transform tr in UiRoot.Instance.App.GetComponentsInChildren<Transform>(true)) if (tr.name == "modal-dungeon-clear") { root = tr; break; }
            Assert.IsNotNull(root, "클리어 팝업 뿌리(modal-dungeon-clear)를 못 찾았다");
            TextMeshProUGUI title = null;
            foreach (TextMeshProUGUI t in root.GetComponentsInChildren<TextMeshProUGUI>(true)) if (t.name == "title") { title = t; break; }
            Assert.IsNotNull(title, "클리어 팝업 제목(title)을 못 찾았다");
            Assert.AreEqual("클리어!", title.text, "정본 showDungeonClear 의 제목");
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

        /// <summary>T333 8회차 — 빈 장비 칸의 이름표: 정본 8030 `.equip-cell .slot-name` ↔ 874 `.equip-cell.egg-cell .slot-name`.
        /// 알 칸(탈것)은 클래스 셋이라 8030(둘)을 **특이도로** 이겨 딱딱한 한 겹(흐림 0)을 받는다 — 두 자리가 같은 키를 쓰면 그 갈림이 사라진다.</summary>
        /// <summary>T333 12회차 — 정본 3187 `.pinfo-preview { text-shadow: 0 1px 2px rgba(0,0,0,.5) }`: 폴백 미리보기 안 스테이지 라벨이 표 `pinfo_preview` 한 겹을 받는다.
        /// 미니 씬이 서면 폴백이 안 그려지므로(정본도 같다) 이 자에서만 미니 씬 시작을 떼어 폴백을 강제한다(BoxBorderSitesTests 와 같은 길).</summary>
        [UnityTest]
        public IEnumerator 플레이어_정보_폴백_미리보기_글자는_정본_아래_1px_흐림_2px_한_겹을_쓴다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            var saved = PlayerInfoPopup.PreviewStart;
            PlayerInfoPopup.PreviewStart = null;
            try
            {
                PlayerInfoPopup.Open(h);
                yield return null;
                Canvas.ForceUpdateCanvases();
                Popup p = PopupLayer.Instance.Find(PlayerInfoPopup.Name);
                Assert.IsNotNull(p, "플레이어 정보");
                Transform preview = null;
                foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
                    if (rt.name == "preview" && rt.Find("stage") != null) preview = rt;
                Assert.IsNotNull(preview, "폴백 미리보기(.pinfo-preview)를 못 찾았다");
                TextMeshProUGUI stage = preview.Find("stage").GetComponent<TextMeshProUGUI>();
                Assert.IsNotNull(stage, "스테이지 라벨");
                AssertShadow(stage, "pinfo_preview", "폴백 미리보기 스테이지 라벨");
                Assert.AreEqual(1f, TextShadowUi.Px("pinfo_preview", "dy_px"), 1e-6f, "정본 0 1px 2px — dy 1");
                Assert.AreEqual(2f, TextShadowUi.Px("pinfo_preview", "blur_px"), 1e-6f, "정본 0 1px 2px — 흐림 2");
                Assert.AreEqual(0.5f, TextShadowUi.C("pinfo_preview").a, 2f / 255f, "정본 rgba(0,0,0,.5)");
                h.Popups.Hide(PlayerInfoPopup.Name);
                yield return null;
            }
            finally { PlayerInfoPopup.PreviewStart = saved; }
        }

        [UnityTest]
        public IEnumerator 빈_장비_칸_이름표는_드롭을_받고_알_칸은_딱딱한_한_겹을_받는다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            PlayerInfoPopup.Open(h);
            yield return null;
            yield return null;
            Popup p = PopupLayer.Instance.Find(PlayerInfoPopup.Name);
            Assert.IsNotNull(p, "플레이어 정보 팝업이 안 열렸다");

            int plain = 0, egg = 0;
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.name != "slot-name" || t.transform.parent == null) continue;
                bool isEgg = t.transform.parent.name == "egg-cell";
                AssertShadow(t, isEgg ? "slot_name_egg" : "slot_name", (isEgg ? "알 칸" : "빈 장비 칸") + " 이름표");
                Assert.Less(t.fontMaterial.GetFloat("_UnderlayOffsetY"), 0f, "정본 둘 다 아래로 1px");
                if (isEgg) { egg++; Assert.AreEqual(0f, t.fontMaterial.GetFloat("_UnderlaySoftness"), 1e-4f, "알 칸은 흐림 0(정본 874 `0 1px 0`)"); }
                else { plain++; Assert.Greater(t.fontMaterial.GetFloat("_UnderlaySoftness"), 0f, "빈 칸은 흐림 2px(정본 8030 `0 1px 2px`)"); }
            }
            Assert.Greater(plain, 0, "빈 장비 칸 이름표를 못 찾았다(첫 세이브는 칸이 비어 있다)");
            Assert.AreEqual(1, egg, "탈것 칸은 하나다");
            PlayerInfoPopup.Close(h);
            yield return null;
        }

        /// <summary>T333 13회차 — 정본 8069 `#chat-preview .chat-preview-name { text-shadow: 0 1px 1px rgba(0,0,0,.5) }`(8392 묶음 .75 보다 특이도가 높다) ·
        /// 2053 `.qst-bar em { text-shadow: 0 1px 1px rgba(0,0,0,.75) }` — 둘 다 한 겹이라 그대로.</summary>
        [UnityTest]
        public IEnumerator HUD_채팅_미리보기_이름과_퀘스트_진행_막대_글은_정본_아래_1px_흐림_1px_한_겹을_쓴다()
        {
            yield return Boot();
            float t0 = Time.realtimeSinceStartup;
            while (Hud.Instance == null && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            Assert.IsNotNull(Hud.Instance, "HUD");
            MetaHost h = MetaHost.Instance;
            // 채팅 띠는 Hud 의 자식이 아니라 UiRoot.Chat 아래다(Hud.BuildChat(chatBand, …)) — 런 833 이 «null» 로 가르쳐 준 자리 · 띠 = ChatButton 의 부모
            Transform band = Hud.Instance.ChatButton.transform.parent;
            TextMeshProUGUI nm = null;
            foreach (TextMeshProUGUI t in band.GetComponentsInChildren<TextMeshProUGUI>(true)) if (t.name == "chat-preview-name") { nm = t; break; }
            Assert.IsNotNull(nm, "채팅 미리보기 이름(chat-preview-name · 채팅 띠 = UiRoot.Chat)");
            AssertShadow(nm, "chat_preview_name", "채팅 미리보기 이름");
            Assert.AreEqual(0.5f, TextShadowUi.C("chat_preview_name").a, 1e-3f, "정본 8069 알파 .5 — 8392 묶음의 .75 가 아니다(특이도)");

            QuestSheet.Open(h);
            yield return null;
            yield return null;
            Popup p = PopupLayer.Instance.Find(QuestSheet.Name);
            Assert.IsNotNull(p, "퀘스트 시트");
            int n = 0;
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.name != "prog") continue;
                n++;
                AssertShadow(t, "qst_bar_em", "퀘스트 진행 막대 글");
            }
            Assert.Greater(n, 0, "진행 막대 글(prog)이 있다");
            h.Popups.Hide(QuestSheet.Name);
            yield return null;
        }

        /// <summary>T333 14회차 — 정본 3890·3902 던전 배너 제목·열쇠의 8방 1px 순검정 링(표 rings · SDF 스트로크) · 5005 자동 제련 스피너 글 `0 .07rem 0 .5` 한 겹.</summary>
        [UnityTest]
        public IEnumerator 던전_배너_제목과_열쇠는_8방_1px_링을_스트로크로_내고_자동_제련_스피너_글은_아래_한_겹을_쓴다()
        {
            yield return Boot();
            // 런 918: 열쇠 «N/M» 은 해금된 던전 배너에만 선다(DungeonSheet `if (ok)`) — 새 세이브는 전부 잠겨 keys 가 0 이었다 → 해금 상태를 먼저 만든다(BrLinesTests 의 길).
            ForgeHost fh = ForgeHost.Instance;
            fh.S.BestChapter = 5; fh.S.BestStage = 1; fh.Pull();
            UiRoot.Instance.TabBar.OnTab("dungeon");
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.IsNotNull(DungeonSheet.Instance, "던전 시트");
            int names = 0, keys = 0;
            foreach (TextMeshProUGUI t in DungeonSheet.Instance.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.name != "name" && t.name != "keys") continue;
                string key = t.name == "name" ? "dg_banner_name" : "dg_banner_keys";
                float want = TextShadowUi.RingPx(key);
                Assert.Greater(want, 1.5f, "표 rings(1 CSS px) × css_px(2.164) 는 1.5 캔버스 px 를 넘어야 한다");
                Material m = t.fontMaterial;
                OutlineSdf o = OutlineSdf.FromStroke(want, t.fontSize, m.GetFloat("_GradientScale"),
                    m.HasProperty("_ScaleRatioA") ? m.GetFloat("_ScaleRatioA") : 1f, t.font.faceInfo.pointSize);
                Assert.AreEqual((float)o.Width01, m.GetFloat("_OutlineWidth"), 1e-3f, "배너 «" + t.text + "»: 링 두께 = 표 × css_px 의 SDF 환산(전엔 카탈로그 dg_name_outline .15)");
                Assert.Greater(t.outlineWidth, 0f, "배너 «" + t.text + "»: 링이 실제로 켜져야 한다");
                Color oc = m.GetColor("_OutlineColor");
                Assert.Less(oc.r + oc.g + oc.b, 0.1f, "배너 «" + t.text + "»: 링은 순검정(정본 #000)");
                if (t.name == "name") names++; else keys++;
            }
            Assert.Greater(names, 0, "배너 제목(name)이 있다");
            Assert.Greater(keys, 0, "배너 열쇠(keys)가 있다");

            fh.S.BestChapter = 3; fh.S.BestStage = 1;   // AgePatternTests 의 길 — 새 세이브는 2-10 전이라 Open 이 🔒 토스트만 낸다(런 299)
            fh.S.ForgeLevel = 29;
            fh.Pull();
            Assert.IsTrue(fh.AutoForgeUnlocked, "2-10 뒤 해금");
            ForgeAutoPopup.Open(fh);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(ForgeAutoPopup.Name);
            Assert.IsNotNull(p, "자동 제련 팝업");
            TextMeshProUGUI v = null;
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true)) if (t.name == "value" && t.transform.parent != null && t.transform.parent.name == "af-spinner") { v = t; break; }
            Assert.IsNotNull(v, "스피너 글(af-spinner/value)");
            AssertShadow(v, "af_spinner", "자동 제련 스피너 글");
            Assert.AreEqual(0f, v.fontMaterial.GetFloat("_UnderlaySoftness"), 1e-4f, "정본 5005 흐림 0");
            PopupLayer.Instance.Hide(ForgeAutoPopup.Name);
            yield return null;
        }
    

        [UnityTest]
        public IEnumerator 소환_결과의_제목_이름판_x1_요약_줄은_정본_아래_흐림_한_겹을_쓴다()
        {
            yield return Boot();
            float tw = 0f;
            while (!(SkillPetSheet.Instance != null && PetSkillHost.Ready) && tw < 20f) { tw += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다");
            var two = new List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = "가" },
                new SkillSummonResultView.Entry { Key = "sk:b", IconKey = "sk_fireball", Rarity = "mythic", Name = "나" },
            };
            SkillSummonResultView v = SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", two, "mythic", null);
            Assert.IsNotNull(v, "결과 연출 팝업(여럿)이 서지 않았다");
            yield return null; yield return null;
            int titles = 0, names = 0;
            foreach (TextMeshProUGUI t in v.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.name != "t" || t.transform.parent == null) continue;
                string pn = t.transform.parent.name;
                if (pn == "sr-title") { AssertShadowMaybeClipped(t, "sr_title", "소환 결과 제목(6207 검정 낙하 겹)"); titles++; }
                else if (pn == "sr-name") { AssertShadowMaybeClipped(t, "sr_name", "이름판 글(여럿 · 7032)"); names++; }
            }
            Assert.AreEqual(1, titles, "제목 띠 글 하나");
            Assert.AreEqual(2, names, "이름판 글 둘(셀마다)");
            v.OnTap(); v.OnTap();
            yield return null;

            var one = new List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:z", IconKey = "sk_fireball", Rarity = "common", Name = "하나", IsNew = true },
            };
            SkillSummonResultView v1 = SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", one, "common", null);
            Assert.IsNotNull(v1, "결과 연출 팝업(x1)이 서지 않았다");
            yield return null; yield return null;
            int ones = 0, solos = 0;
            foreach (TextMeshProUGUI t in v1.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                Transform p = t.transform.parent;
                if (p == null) continue;
                if (t.name == "t" && p.name == "sr-name") { AssertShadowMaybeClipped(t, "sr_name_one", "이름판 글(x1 · 7084)"); ones++; }
                else if (p.name == "line" && p.parent != null && p.parent.name == "sr-solo") { AssertShadowMaybeClipped(t, "sr_solo_line", "x1 요약 줄 글 조각(5784)"); solos++; }
            }
            Assert.AreEqual(1, ones, "x1 이름판 글 하나");
            Assert.GreaterOrEqual(solos, 1, "x1 요약 줄의 글 조각(아이콘 조각 사이)이 하나 이상");
            v1.OnTap(); v1.OnTap();
            yield return null;
        }

        /// <summary>
        /// T333 17회차 — 정본 **8371** `.fi-age-name, .fi-age-cur, .fi-age-next, .af-age-name, .af-age-cur { text-shadow: 0 1px 0 rgba(255,255,255,.34) }`.
        ///
        /// ⚑ **선택자가 다섯이고 `.af-age-next` 만 빠져 있다** — 확률 정보(`.fi-*`)는 셋 다 이 겹을 지고 자동 제련(`.af-*`)은 **«다음» 칸만 안 진다**.
        /// 클론은 한 함수(<see cref="ForgeUi"/>.AgeBar)가 두 화면을 다 세우므로 그 한 자리를 `autoForge` 로 가른다 —
        /// 이 자는 **두 화면을 나란히** 보고 «자동 제련의 다음 칸에는 겹이 **없다**» 까지 못 박는다.
        /// 한 줄로 뭉쳐 다섯을 다 걸면 정본에 없는 겹이 하나 생기는데, 그것은 «있다» 만 재는 자로는 안 걸린다.
        /// </summary>
        [UnityTest]
        public IEnumerator 시대_막대_글_셋은_흰_양각을_지고_자동_제련의_다음_칸만_안_진다()
        {
            yield return Boot();
            PlayLog log = PlayLog.Start("text-shadow-fi-age");
            ForgeHost h = ForgeHost.Instance;
            float t0 = 0f;
            while (!ForgeHost.Ready && t0 < 20f) { t0 += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            // 자동 제련은 2-10 해금 + 제련 레벨이 낮으면 뒤 시대 행이 아예 안 선다(AgePatternTests 가 겪은 자리) — 둘 다 맞춰 둔다.
            h.S.BestChapter = 3; h.S.BestStage = 1; h.S.ForgeLevel = 29; h.Pull();

            ForgeInfoPopup.Open(h);
            yield return null; yield return null;
            Popup info = h.Meta.Popups.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(info, "확률 정보 팝업");
            AssertAgeBar(info.Root, true, "확률 정보");
            ForgeInfoPopup.Close(h);
            yield return null;

            Assert.IsTrue(h.AutoForgeUnlocked, "2-10 뒤 해금");
            ForgeAutoPopup.Open(h);
            yield return null; yield return null;
            Popup auto = h.Meta.Popups.Find(ForgeAutoPopup.Name);
            Assert.IsNotNull(auto, "자동 제련 팝업");
            AssertAgeBar(auto.Root, false, "자동 제련");

            log.AssertNoRed();
            log.Dispose();
        }

        /// <summary>시대 막대 하나를 집어 이름·현재는 늘, «다음» 은 <paramref name="nextHasShadow"/> 대로 본다.</summary>
        private static void AssertAgeBar(Transform root, bool nextHasShadow, string what)
        {
            int seen = 0;
            foreach (Transform bar in root.GetComponentsInChildren<Transform>(true))
            {
                Transform nmT = bar.Find("name"), curT = bar.Find("cur");
                if (nmT == null || curT == null) continue;
                TextMeshProUGUI nm = nmT.GetComponent<TextMeshProUGUI>(), cur = curT.GetComponent<TextMeshProUGUI>();
                if (nm == null || cur == null) continue;
                seen++;
                AssertShadow(nm, "fi_age_line", what + " 시대 이름");
                AssertShadow(cur, "fi_age_line", what + " 현재 %");
                Transform nextT = bar.Find("next");
                if (nextT != null)
                {
                    Transform pctT = nextT.Find("pct");
                    if (pctT != null)
                    {
                        TextMeshProUGUI pct = pctT.GetComponent<TextMeshProUGUI>();
                        if (nextHasShadow) AssertShadow(pct, "fi_age_line", what + " 다음 %");
                        else Assert.IsFalse(pct.fontMaterial.IsKeywordEnabled("UNDERLAY_ON"),
                                            what + " 다음 %: 정본 8371 의 다섯 선택자에 `.af-age-next` 가 **없다** — 이 칸엔 겹이 안 붙는다");
                    }
                }
                // T333 18회차 — 별이 있으면 **이름과 다른 조각**이어야 한다(정본 5138: 색 #ffb300 · .88rem · 4방향 1px 검정 링).
                //   클론은 «이름 ★★» 로 한 문자열이었다 — 그러면 셋 다 못 건다. 이 줄이 그 되돌아감을 막는다.
                Transform starT = bar.Find("star");
                if (starT != null)
                {
                    TextMeshProUGUI star = starT.GetComponent<TextMeshProUGUI>();
                    Assert.IsNotNull(star, what + ": 별 조각");
                    Assert.IsFalse(nm.text.Contains("★"), what + ": 별이 이름 문자열에 남아 있다 — 떼어야 색·크기·링을 따로 건다");
                    Assert.AreEqual(PinnedColorUi.C("fi_age_star_ink"), star.color, what + " 별: 정본 5138 의 리터럴 #ffb300(이름의 잉크가 아니다)");
                    Assert.Less(star.fontSize, nm.fontSize, what + " 별: 정본은 별이 이름보다 **작다**(.88rem) — 하한 36 을 주면 되레 커진다");
                    Assert.Greater(star.outlineWidth, 0f, what + " 별: 4방향 1px 검정 링(SDF 스트로크)");
                }
                break;   // 막대 하나면 규칙이 드러난다(행 수는 제련 레벨이 정한다 · 이 절의 몫이 아니다)
            }
            Assert.GreaterOrEqual(seen, 1, what + ": 시대 막대가 한 줄은 선다");
        }

        /// <summary>
        /// T333 19회차 — 정본 8381 한 벌(«밝은 종이 위 글자는 흰 엠보스» `0 1px 0 rgba(255,255,255,.92)`)의 선택자에 `.modal-card .idet-name` 이 있는데,
        /// 그 이름은 정본에서 **세 자리**에 선다: `ui.js` **2246** 장비 상세 · **4198** 펫 강화 · **5603** 기술 노드(셋 다 `<div class="modal-card paper …">` 안).
        /// 14~18회차의 자 표에는 장비 상세 하나만 적혀 있어 «자리 초록» 이 그 선언을 다 덮은 것처럼 보였다 — 이 자가 나머지 둘 중 잡을 수 있는 것을 지킨다
        /// (펫 강화는 T354 산 lock 뒤라 자 KNOWN 에 임자와 함께 적어 두었다).
        ///
        /// ⚑ 기술 노드는 **두 조각**이다: 정본 5603 은 `<div class="idet-name">이름 <small class="tn-lv">N단계 · Lv.x/y</small></div>` 로 한 상자인데
        /// 클론은 T413 에서 그것을 `name`·`lv` 두 조각으로 떼어 놓았다. `text-shadow` 는 상속되므로 **떼어 놓은 쪽에도 같은 겹**이 서야 정본과 같은 그림이다.
        /// </summary>
        [UnityTest]
        public IEnumerator 장비_상세와_기술_노드_이름도_8381_한_벌의_흰_엠보스를_진다()
        {
            yield return Boot();
            float bt = 0f;
            while (!ForgeHost.Ready && bt < 15f) { bt += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 15초 안에 준비되지 않았다");
            ForgeHost h = ForgeHost.Instance;

            // ⓐ 장비 상세(`ui.js` 2246 `.idet-name`) — 목록에서 한 칸을 열어야 그려진다(ForgeUiTests 와 같은 길).
            string age = h.Defs.Ages[0];
            string wt = h.Engine.WeaponsOfAge(age)[0];
            ForgeInfoPopup.OpenDetail(h, age, "weapon", 0, wt);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup item = h.Meta.Popups.Find(ForgeInfoPopup.ItemName);
            Assert.IsNotNull(item, "장비 상세 팝업");
            TextMeshProUGUI idet = null;
            foreach (TextMeshProUGUI t in item.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.name == "idet-name") { idet = t; break; }
            Assert.IsNotNull(idet, "장비 상세 이름(idet-name)");
            AssertShadow(idet, "paper_emboss", "장비 상세 이름");
            Color ic = idet.fontMaterial.GetColor("_UnderlayColor");
            Assert.Greater(ic.r + ic.g + ic.b, 2.7f, "장비 상세 이름: 엠보스는 흰색이어야 한다(정본 rgba(255,255,255,.92))");
            ForgeInfoPopup.Close(h);
            yield return null;

            // ⓑ 기술 노드(`ui.js` 5603) — 이름과 그 안의 `<small class="tn-lv">` 둘 다.
            float dt = 0f;
            while (!DungeonUiHost.Ready && dt < 15f) { dt += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(DungeonUiHost.Ready, "DungeonUiHost 가 15초 안에 준비되지 않았다");
            string id = DungeonUiHost.Instance.Tech.NodesOf("power")[0];
            TechPopups.OpenNode(id);
            yield return null;
            Canvas.ForceUpdateCanvases();
            RectTransform card = null;
            foreach (RectTransform rt in UiRoot.Instance.App.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "card" && rt.gameObject.activeInHierarchy && rt.Find("name") != null && rt.Find("lv") != null) { card = rt; break; }
            Assert.IsNotNull(card, "기술 노드 상세 카드");
            TextMeshProUGUI tn = card.Find("name").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI tl = card.Find("lv").GetComponent<TextMeshProUGUI>();
            Assert.IsNotNull(tn, "노드 이름 글자"); Assert.IsNotNull(tl, "노드 단계 글자");
            AssertShadow(tn, "paper_emboss", "기술 노드 이름");
            AssertShadow(tl, "paper_emboss", "기술 노드 단계(정본 5603 의 `<small>` 은 같은 `.idet-name` 안이라 같은 겹을 물려받는다)");
            TechPopups.Close();
            yield return null;
        }

    }
}
