using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T365 4회차 — 정본 `border` 폭 단이 ol2 인 자리 여섯이 클론에서 `line2_px`(4) 로 선다(전엔 ol1 = `line_px` 2 · 둥근 버튼은 ol3 = 6).
    /// 클론의 테는 «바깥 고리 + 안쪽 면 r − 폭» 두 장이고 안쪽 면은 <c>PopupKit.Inset(face, 폭)</c> 으로 앉으므로 **안쪽 면의 offset** 이 곧 폭이다.
    /// 프로필 팝업: 칸(`.profile-field` 3047) · 아바타 고르기(`.avatar-pick-btn` 3061) · 설정 행 버튼(`.settings-act` 3121) — 채팅 화면: 입력줄 위 테(3444) · 입력칸(3450) · 둥근 버튼(3284).
    /// 6회차: 리그 행(`.league-row` 2328) · 리그 보상 표(`.league-reward-table` 2537) · 플레이어 정보 폴백 미리보기(`.pinfo-preview` 3185).
    /// </summary>
    public class BoxBorderSitesTests
    {
        private static void DeleteSave()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
        }

        private static IEnumerator Boot()
        {
            DeleteSave();
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null && Hud.Instance != null) && t < 30f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 30초 안에 서지 않았다");
            Assert.IsNotNull(PopupLayer.Instance, "팝업 층이 서지 않았다");
            yield return null;
        }

        /// <summary>«line» 고리와 «face» 안쪽 면을 둘 다 가진 상자 — 안쪽 면 offset(= Inset 폭)을 돌려준다.</summary>
        private static float RingWidth(Transform box, string what)
        {
            Transform line = box.Find("line"), face = box.Find("face");
            Assert.IsNotNull(line, what + ": 고리(line)가 없다");
            Assert.IsNotNull(face, what + ": 안쪽 면(face)이 없다");
            RectTransform frt = (RectTransform)face;
            Assert.AreEqual(frt.offsetMin.x, -frt.offsetMax.x, 0.01f, what + ": 안쪽 면은 좌우 같은 만큼 들어간다");
            return frt.offsetMin.x;
        }

        /// <summary>
        /// T365 15회차 — 메인 화면의 남은 넷. 정본은 **같은 화면에서도 단이 섞인다**:
        /// 상단바 카드(92)는 ol1 인데 그 안의 아바타(100)는 **ol2** · 웨이브 핍(189)과 트랙(185)도 **ol2** ·
        /// 채팅 띠 위 줄은 3229 의 ol1 을 **3639 가 ol2 로 덮는다**(뒤 규칙이 이긴다).
        /// 클론은 넷 다 `line_px`(ol1) 한 값으로 그려 한 단 얇았다.
        /// </summary>
        [UnityTest]
        public IEnumerator 상단바_아바타와_웨이브_핍_트랙과_채팅_띠는_카드와_달리_정본_ol2_다()
        {
            yield return Boot();
            Canvas.ForceUpdateCanvases();
            float ol1 = UiKit.L("line_px"), ol2 = UiKit.L("line2_px");
            Assert.Greater(ol2, ol1, "ol2 는 ol1 보다 두껍다 — 아니면 이 자는 아무것도 못 가른다");

            // 상단바: 카드는 ol1(정본 92) · 그 안 아바타는 ol2(정본 100)
            GameObject card = GameObject.Find("profile-card");
            Assert.IsNotNull(card, "프로필 카드");
            // ⚠ 이 자리는 «고리 상자 + line/face 자식» 꼴이 아니다 — 상자 «avatar» **자체가 고리**고 그 안에 면 하나다
            //   (`Hud.Build`: `UiKit.Rounded(card, "avatar", "pp_line", …)` + `UiKit.Rounded(avRing.transform, "face", …)`).
            //   그래서 `RingWidth`(line+face 를 찾는다)로는 못 잰다 — 런 940 이 그렇게 빨갰다(16회차 수리).
            Transform avatar = card.transform.Find("avatar");
            Assert.IsNotNull(avatar, "카드 안 아바타 타일");
            RectTransform avFace = (RectTransform)avatar.Find("face");
            Assert.IsNotNull(avFace, "아바타 안쪽 면(face)");
            Assert.AreEqual(avFace.offsetMin.x, -avFace.offsetMax.x, 0.01f, "아바타 면은 좌우 같은 만큼 들어간다");
            Assert.AreEqual(ol2, avFace.offsetMin.x, 0.01f,
                "정본 100 `.profile-card .avatar { border: var(--ol2) solid #000 }`");

            // 웨이브 핍: 고리 두께 = (고리 크기 − 면 크기) / 2
            Hud.Instance.SetWaves(5, 2, -1);
            yield return null;
            GameObject row = GameObject.Find("wave-pips");
            Assert.IsNotNull(row, "웨이브 노드 줄");
            RectTransform ring = (RectTransform)row.transform.Find("pip-1/ring");
            RectTransform fill = (RectTransform)row.transform.Find("pip-1/fill");
            Assert.IsNotNull(ring, "핍 고리"); Assert.IsNotNull(fill, "핍 면");
            Assert.AreEqual(ol2, (ring.sizeDelta.x - fill.sizeDelta.x) * 0.5f, 0.01f,
                "정본 189 `.pip { border: var(--ol2) solid var(--pp-line) }`");

            // 트랙: 흰 심(core)이 검정 판(edge) 안으로 들어간 만큼이 위·아래 두 줄의 두께다
            RectTransform core = (RectTransform)row.transform.Find("track/core");
            Assert.IsNotNull(core, "트랙 심(core)");
            Assert.AreEqual(ol2, core.offsetMin.y, 0.01f,
                "정본 185 `#wave-pips::before { border-top/bottom: var(--ol2) … }`");

            // 채팅 띠 위 줄
            Transform chatLine = UiRoot.Instance.Chat.Find("line");
            Assert.IsNotNull(chatLine, "채팅 띠 위 줄");
            Assert.AreEqual(ol2, ((RectTransform)chatLine).sizeDelta.y, 0.01f,
                "정본 3639 `#chat-preview { border-top: var(--ol2) … }` 가 3229 의 ol1 을 덮는다");
        }

        [UnityTest]
        public IEnumerator 프로필_칸_아바타_고르기_설정_버튼의_테는_정본_ol2_다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            float ol2 = UiKit.L("line2_px"), ol1 = UiKit.L("line_px");
            Assert.Greater(ol2, ol1, "line2_px 는 line_px 보다 두껍다(4 > 2)");
            ProfilePopup.Open(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(ProfilePopup.Name);
            Assert.IsNotNull(p, "프로필 팝업");
            int fields = 0, picks = 0;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.Find("line") == null || rt.Find("face") == null) continue;
                if (rt.Find("text") != null || rt.Find("ico") != null) { fields++; Assert.AreEqual(ol2, RingWidth(rt, "프로필 칸 " + rt.name), 0.01f); }
            }
            Assert.Greater(fields, 0, "프로필 칸(.profile-field)을 못 찾았다");
            // 아바타 고르기 격자(.avatar-pick-btn)는 연필(avatar-edit)을 눌러야 펼쳐진다(정본 profile.js picking 토글)
            Button avEdit = null;
            foreach (Button b in p.Root.GetComponentsInChildren<Button>(true)) if (b.name == "avatar-edit") avEdit = b;
            Assert.IsNotNull(avEdit, "아바타 연필 버튼(avatar-edit)을 못 찾았다");
            avEdit.onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();
            p = PopupLayer.Instance.Find(ProfilePopup.Name);
            Assert.IsNotNull(p, "프로필 팝업(연필 뒤)");
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.Find("line") == null || rt.Find("face") == null) continue;
                if (rt.name.StartsWith("av-", System.StringComparison.Ordinal)) { picks++; Assert.AreEqual(ol2, RingWidth(rt, "아바타 고르기 " + rt.name), 0.01f); }
            }
            Assert.Greater(picks, 0, "아바타 고르기 칸(.avatar-pick-btn)을 못 찾았다");
            // 설정 화면의 행 버튼(.settings-act)
            ProfilePopup.Close(h);
            yield return null;
            ProfilePopup.Open(h);
            yield return null;
            ProfilePopup.SwitchView(h, "settings");
            yield return null;
            Canvas.ForceUpdateCanvases();
            p = PopupLayer.Instance.Find(ProfilePopup.Name);
            int acts = 0;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "act" && rt.Find("line") != null && rt.Find("face") != null) { acts++; Assert.AreEqual(ol2, RingWidth(rt, "설정 행 버튼"), 0.01f); }
            Assert.Greater(acts, 0, "설정 행 버튼(.settings-act)을 못 찾았다");
            ProfilePopup.Close(h);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 채팅_입력줄의_위_테_입력칸_둥근_버튼_테는_정본_ol2_다()
        {
            yield return Boot();
            float ol2 = UiKit.L("line2_px");
            Hud.Instance.ChatButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(PopupLayer.Instance.IsOpen(ChatScreen.Name), "채팅 줄 → 전체화면 채팅");
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(ChatScreen.Name);
            Transform bar = null, close = null, input = null;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name == "close" && rt.Find("line") != null) close = rt;
                else if (rt.name == "input" && rt.Find("line") != null) input = rt;
                else if (rt.name == "line" && rt.parent != null && rt.parent.Find("bg") != null && rt.parent.Find("close") != null) bar = rt;
            }
            Assert.IsNotNull(bar, "입력줄 위 테(line)를 못 찾았다");
            Assert.IsNotNull(close, "둥근 버튼(close)을 못 찾았다");
            Assert.IsNotNull(input, "입력칸(input)을 못 찾았다");
            Assert.AreEqual(ol2, ((RectTransform)bar).rect.height, 0.01f, "입력줄 위 테 = ol2(정본 3444)");
            Assert.AreEqual(ol2, RingWidth(close, "둥근 버튼"), 0.01f);
            Assert.AreEqual(ol2, RingWidth(input, "입력칸"), 0.01f);
            yield return null;
        }
        [UnityTest]
        public IEnumerator 리그_행과_보상_표의_테는_정본_ol2_다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            float ol2 = UiKit.L("line2_px");
            LeagueSheet.Open(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(LeagueSheet.Name);
            Assert.IsNotNull(p, "리그 시트");
            // 행의 테는 `PopupKit.Outlined(row, "face", …)` — 행 아래 «face» 상자가 고리(line)·안쪽 면(face)을 품는다(7회차 수리 · 6회차 자는 행 바로 아래에서 찾아 0).
            // 이름을 «face» 로 못 박는다 — 행 안의 아바타(`PopupKit.Avatar`)도 line+face 를 품는데 그 고리는 ol1 이고 자 KNOWN(`.league-avatar` · Popups.cs T331 lock)의 몫이다(8회차 수리 · 런 688 이 2.0 을 봤다).
            int rows = 0;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "face" && rt.parent != null && rt.parent.name.StartsWith("row-", System.StringComparison.Ordinal) && rt.Find("line") != null && rt.Find("face") != null) { rows++; Assert.AreEqual(ol2, RingWidth(rt, "리그 행 " + rt.parent.name), 0.01f); }
            Assert.Greater(rows, 0, "리그 행(.league-row)을 못 찾았다");
            h.Popups.Hide(LeagueSheet.Name);
            yield return null;
            LeagueSheet.OpenRewards(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            p = PopupLayer.Instance.Find(LeagueSheet.RewardsName);
            Assert.IsNotNull(p, "리그 보상");
            Transform table = null;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "face" && rt.parent != null && rt.parent.name == "table" && rt.Find("line") != null && rt.Find("face") != null) table = rt;
            Assert.IsNotNull(table, "보상 표(.league-reward-table)를 못 찾았다");
            Assert.AreEqual(ol2, RingWidth(table, "보상 표"), 0.01f);
            h.Popups.Hide(LeagueSheet.RewardsName);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 플레이어_정보_폴백_미리보기의_테는_정본_ol2_다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            float ol2 = UiKit.L("line2_px");
            // 미니 씬이 서면 폴백이 안 그려지므로(정본도 같다) 이 자에서만 미니 씬 시작을 떼어 폴백을 강제한다
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
                    if (rt.name == "preview" && rt.Find("line") != null && rt.Find("face") != null) preview = rt;
                Assert.IsNotNull(preview, "폴백 미리보기(.pinfo-preview)를 못 찾았다");
                Assert.AreEqual(ol2, RingWidth(preview, "폴백 미리보기"), 0.01f);
                h.Popups.Hide(PlayerInfoPopup.Name);
                yield return null;
            }
            finally { PlayerInfoPopup.PreviewStart = saved; }
        }
        /// <summary>T365 10회차 — T331 범위 축소로 열린 자리: 설정 토글(정본 3108 ol2) · 프로필 큰 아바타(3002 ol3) · 리그 행 아바타(작은 아바타 ol2) · 퀘스트 막대(2040 ol2 · 전엔 테 없음).</summary>
        [UnityTest]
        public IEnumerator 설정_토글과_아바타_둘과_퀘스트_막대의_테는_정본_단이다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            float ol2 = UiKit.L("line2_px"), ol3 = UiKit.L("line3_px");
            ProfilePopup.Open(h);
            yield return null;
            Popup p = PopupLayer.Instance.Find(ProfilePopup.Name);
            Assert.IsNotNull(p, "프로필");
            Transform big = null;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "avatar" && rt.Find("line") != null && rt.Find("face") != null) { big = rt; break; }
            Assert.IsNotNull(big, "프로필 큰 아바타(.profile-avatar-big)");
            Assert.AreEqual(ol3, RingWidth(big, "큰 아바타"), 0.01f);
            ProfilePopup.SwitchView(h, "settings");
            yield return null;
            p = PopupLayer.Instance.Find(ProfilePopup.Name);
            int toggles = 0;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
                if (rt.Find("knob") != null && rt.Find("line") != null && rt.Find("face") != null) { toggles++; Assert.AreEqual(ol2, RingWidth(rt, "설정 토글 " + rt.name), 0.01f); }
            Assert.Greater(toggles, 0, "설정 토글(.settings-toggle)을 못 찾았다");
            ProfilePopup.Close(h);
            yield return null;
            LeagueSheet.Open(h);
            yield return null;
            p = PopupLayer.Instance.Find(LeagueSheet.Name);
            int avatars = 0;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "avatar" && rt.parent != null && rt.parent.name.StartsWith("row-", System.StringComparison.Ordinal) && rt.Find("line") != null && rt.Find("face") != null) { avatars++; Assert.AreEqual(ol2, RingWidth(rt, "리그 행 아바타"), 0.01f); }
            Assert.Greater(avatars, 0, "리그 행 아바타(.league-avatar)를 못 찾았다");
            h.Popups.Hide(LeagueSheet.Name);
            yield return null;
            QuestSheet.Open(h);
            yield return null;
            p = PopupLayer.Instance.Find(QuestSheet.Name);
            Assert.IsNotNull(p, "퀘스트 시트");
            int bars = 0;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "bar" && rt.Find("line") != null && rt.Find("face") != null) { bars++; Assert.AreEqual(ol2, RingWidth(rt, "퀘스트 막대"), 0.01f); }
            Assert.Greater(bars, 0, "퀘스트 막대(.qst-bar)를 못 찾았다");
            h.Popups.Hide(QuestSheet.Name);
            yield return null;
        }

        static RectTransform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root as RectTransform;
            for (int i = 0; i < root.childCount; i++) { RectTransform r = FindDeep(root.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        /// <summary>«테 없는 검정 원판 + 흰 i» 한 자리를 끝까지 재어 본다 — 면 색을 돌려준다.</summary>
        static Color AssertInfoButton(RectTransform btn, string what)
        {
            Assert.IsNotNull(btn, what + ": 정보 버튼 버튼을 못 찾았다");
            Assert.IsNull(btn.Find("ring"), what + ": 정본은 `border: none` 이다 — 고리를 그리면 안 된다");
            Assert.IsNull(btn.Find("line"), what + ": 정본은 `border: none` 이다 — 고리를 그리면 안 된다");
            RectTransform face = btn.Find("face") as RectTransform;
            Assert.IsNotNull(face, what + ": 면(face)이 없다");
            // 고리 짝이 돌아오면 면이 `PopupKit.Inset` 으로 안으로 들어간다 — 0 이 곧 «테 없음» 이다.
            Assert.AreEqual(0f, face.offsetMin.x, 0.01f, what + ": 면이 들어가 있다 — 고리 짝(Inset)이 돌아왔다");
            Assert.AreEqual(0f, face.offsetMax.x, 0.01f, what + ": 면이 들어가 있다 — 고리 짝(Inset)이 돌아왔다");
            Image fi = face.GetComponent<Image>();
            Assert.IsNotNull(fi, what + ": 면 Image");
            TMPro.TextMeshProUGUI g = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            Assert.IsNotNull(g, what + ": 글자(i)");
            Assert.AreEqual("i", g.text, what + ": 정본 글리프는 소문자 i 다(ui.js 1544·2053)");
            Color ink = UiKit.C("pp_paper");
            Assert.AreEqual(ink.r, g.color.r, 0.02f, what + ": 흰 i(정본 3634 `color: var(--pp-paper)` · 5061 `color: #fff`)");
            Assert.AreEqual(ink.g, g.color.g, 0.02f, what + ": 흰 i");
            Assert.AreEqual(ink.b, g.color.b, 0.02f, what + ": 흰 i");
            Assert.Less(fi.color.r + fi.color.g + fi.color.b, 0.4f, what + ": 면은 검정으로 껉 참 원이다(정본 3634·5060)");
            return fi.color;
        }

        /// <summary>
        /// T365 13회차 — 정본은 정보 버튼 를 **두 얼굴**로 쓰고 둘 다 «검정 면 · 테 없음 · 흰 i» 다:
        /// 장비 시트 3634(`--pp-line` = #000) · 대장간 정본 5060(#17181a). 기본 규칙 971(흰 면 + ol1 고리)은 실물에 안 선다.
        /// 두 면을 **나란히** 본다 — 표를 한 키로 뭉뚝그리면 이 자가 먼저 넘어진다.
        /// </summary>
        [UnityTest]
        public IEnumerator 정보_버튼_둘은_테_없는_검정_원판에_흰_i_다()
        {
            yield return Boot();
            float t = 0f;
            while (ForgeHost.Instance == null && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            ForgeHost h = ForgeHost.Instance;
            Assert.IsNotNull(h, "ForgeHost");
            yield return null;

            // ⓐ 장비 시트의 정보 버튼 (ui.js 1544 · style.css 3634)
            Color sheetFace = AssertInfoButton(FindDeep(UiRoot.Instance.Sheet, "info-btn"), "장비 시트 정보 버튼");
            Color wantSheet = UiKit.C(InfoButtonUi.FaceKey("info-btn"));
            Assert.AreEqual(wantSheet.r, sheetFace.r, 0.02f, "장비 시트 정보 버튼 면 = 표값(pp_line)");
            Assert.AreEqual(wantSheet.g, sheetFace.g, 0.02f, "장비 시트 정보 버튼 면 = 표값(pp_line)");
            Assert.AreEqual(wantSheet.b, sheetFace.b, 0.02f, "장비 시트 정보 버튼 면 = 표값(pp_line)");

            // ⓑ 대장간 정본 팝업의 정보 버튼 (ui.js 2053 · style.css 5059)
            ForgeInfoPopup.Open(h);
            yield return null; yield return null;
            Popup info = h.Meta.Popups.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(info, "대장간 정본 팝업이 열렸다");
            Color fiFace = AssertInfoButton(FindDeep(info.Root, "fi-info-btn"), "대장간 정본 정보 버튼");
            Color wantFi = UiKit.C(InfoButtonUi.FaceKey("fi-info-btn"));
            Assert.AreEqual(wantFi.r, fiFace.r, 0.02f, "대장간 정본 정보 버튼 면 = 표값(pp_ink)");
            Assert.AreEqual(wantFi.g, fiFace.g, 0.02f, "대장간 정본 정보 버튼 면 = 표값(pp_ink)");
            Assert.AreEqual(wantFi.b, fiFace.b, 0.02f, "대장간 정본 정보 버튼 면 = 표값(pp_ink)");

            // 두 면은 같은 검정이 아니다 — 정본이 #000 ↔ #17181a 로 한 단 갈라 적어 둔 자리다.
            Assert.Greater(fiFace.g + fiFace.b, sheetFace.g + sheetFace.b + 0.02f,
                           "대장간 정보 버튼 면(#17181a)은 장비 시트 정보 버튼 면(#000)보다 살짝 밝다 — 표를 한 키로 뭉뚝그리면 여기서 넘어진다");
            h.Meta.Popups.Hide(ForgeInfoPopup.Name);
            yield return null;
        }
    }
}
