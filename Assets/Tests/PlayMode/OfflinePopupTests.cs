using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T68 — 오프라인 보상 팝업 머리가 정본 `.offline-top` 대로인가: 어두운 판(카드 높이의 42.8%) · 흰 제목 · «수집 시간:» 회색 · 시간·요율 초록 ·
    /// 요율은 원형 아이콘 **위** 글자 **아래**(세로 · 가운데 정렬) · 수집 버튼 우상단 빨간 점(흰 테) · 글자 하한 · 콘솔 빨강 0.
    /// 자기 파일인 이유: `UiSmokeTests.cs` 는 T54·T63·T65 lock 이 같은 파일로 쥐고 있어(check_claim_scope) 뒤 번호가 손대지 않는다.
    /// </summary>
    public class OfflinePopupTests
    {
        private static void DeleteSave()
        {
            try
            {
                string p = System.IO.Path.Combine(Application.persistentDataPath, (SaveIo.Defs != null ? SaveIo.Defs.SaveKey : "forgeclone_save_v1") + ".json");
                if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
            }
            catch (System.Exception) { /* 저장소 접근 실패는 무시 */ }
        }

        [TearDown]
        public void CleanSave() { DeleteSave(); }

        private static IEnumerator Boot()
        {
            DeleteSave();
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다 (SaveIo → meta.json)");
            Assert.IsNotNull(PopupLayer.Instance, "팝업 층이 서지 않았다");
            yield return null;
        }

        private static RectTransform Find(string objName)
        {
            Popup p = PopupLayer.Instance.Find(OfflinePopup.Name);
            Assert.IsNotNull(p, "오프라인 팝업이 열려 있지 않다");
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == objName) return rt;
            Assert.Fail("오프라인 팝업 안에 «" + objName + "» 이 없다");
            return null;
        }

        private static RectTransform FindUnder(RectTransform parent, string objName)
        {
            foreach (RectTransform rt in parent.GetComponentsInChildren<RectTransform>(true))
                if (rt != parent && rt.name == objName) return rt;
            Assert.Fail(parent.name + " 아래에 «" + objName + "» 이 없다");
            return null;
        }

        private static Rect World(RectTransform rt)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y);
        }

        /// <summary>T396 3회차 — **못박은 잉크**(정본이 선택자에만 리터럴로 준 값)는 카탈로그가 아니라 곁 표가 쥔다.</summary>
        private static void AssertPinnedColor(Color got, string key, string what)
        {
            Color want = PinnedColorUi.C(key);
            Assert.Less(Mathf.Abs(got.r - want.r) + Mathf.Abs(got.g - want.g) + Mathf.Abs(got.b - want.b), 0.02f, what + " 색이 표 «" + key + "» 가 아니다: " + got);
        }

        private static void AssertColor(Color got, string key, string what)
        {
            Color want = UiKit.C(key);
            Assert.Less(Mathf.Abs(got.r - want.r) + Mathf.Abs(got.g - want.g) + Mathf.Abs(got.b - want.b), 0.02f, what + " 색이 «" + key + "» 가 아니다: " + got);
        }

        [UnityTest]
        public IEnumerator 머리_판은_어두운_판_흰_제목_회색_라벨_초록_수치_요율은_세로_수집에_빨간_점()
        {
            yield return Boot();
            PlayLog log = PlayLog.Start("offline");
            MetaHost h = MetaHost.Instance;
            OfflinePopup.Show(h, new OfflineReward { Elapsed = 5000, Counted = 3600, Coins = 8870, Hammers = 149.05, CoinRate = 1.13, HammerRate = 1.14 });
            yield return null;
            Assert.IsTrue(h.Popups.IsOpen(OfflinePopup.Name));

            // 머리 판 — 카드 높이의 42.8% · 어두운 판
            RectTransform card = Find("card"), top = Find("top");
            Rect rc = World(card), rtop = World(top);
            Assert.AreEqual(OfflinePopup.TopFrac, rtop.height / rc.height, 0.03f, "머리 판 높이 = 카드 높이 × 42.8%");
            Assert.AreEqual(rc.yMax, rtop.yMax, rc.height * 0.02f, "머리 판은 카드 위 끝에 붙는다");
            // T377 22회차 — 머리 판은 정본 256 `.offline-top #0e111b` 의 자리 전용 키(PinnedColorUi)다 · 전엔 전역 pp_ink 였다.
            Color dark = PinnedColorUi.C(OfflinePopup.TopFaceKey);
            Assert.AreEqual("#0E111B", "#" + ColorUtility.ToHtmlStringRGB(dark), "정본 256 .offline-top #0e111b");
            Color gotTop = FindUnder(top, "bg").GetComponent<Image>().color;
            Assert.Less(Mathf.Abs(gotTop.r - dark.r) + Mathf.Abs(gotTop.g - dark.g) + Mathf.Abs(gotTop.b - dark.b), 0.02f, "머리 판 색이 표 offline_top_face 가 아니다: " + gotTop);
            Assert.Less(dark.r + dark.g + dark.b, 0.5f, "머리 판은 어두운 색이어야 한다(정본 #0e111b)");

            // 글자 색 — 제목 흰 · «수집 시간:» 회색 · 시간 초록
            AssertColor(FindUnder(top, "title").GetComponent<TextMeshProUGUI>().color, "stage_ink", "제목");
            // T396 3회차 — 정본 267 `.offline-sub { color: #ccc }` 는 **선택자에 못박은 잉크**다. 종전 이 줄은 클론이 고른 «가장 가까운 토큰»
            //   `pp_gray`(#c4c4c4)를 단언해 **근사를 못 박고 있었다** — 정본 값으로 옮기니 이 자가 빨개졌다(런 786). 표 키로 바꾼다.
            AssertPinnedColor(FindUnder(top, "sub").GetComponent<TextMeshProUGUI>().color, OfflinePopup.SubPinnedInk, "«수집 시간:»");
            TextMeshProUGUI counted = FindUnder(top, "counted").GetComponent<TextMeshProUGUI>();
            AssertColor(counted.color, OfflinePopup.GreenKey, "수집 시간 수치");
            Assert.AreEqual("1시 0분", counted.text, "3600초 = 원작 fmtTime «1시 0분»");
            Assert.AreEqual("(최대)", FindUnder(top, "max").GetComponent<TextMeshProUGUI>().text, "경과 > 수집이면 (최대)");

            // 요율 — 원형 아이콘 위 · 글자 아래 · 가운데 · 두 칸 같은 높이
            RectTransform coin = FindUnder(top, "coin"), hammer = FindUnder(top, "hammer");
            foreach (RectTransform rate in new[] { coin, hammer })
            {
                Rect circle = World(FindUnder(rate, "circle")), text = World(FindUnder(rate, "text"));
                Assert.Greater(circle.yMin, text.yMax - 1f, rate.name + ": 원형 아이콘이 글자 위에 있다");
                Assert.AreEqual(circle.center.x, text.center.x, 2f, rate.name + ": 아이콘과 글자가 같은 세로축");
                AssertColor(FindUnder(rate, "text").GetComponent<TextMeshProUGUI>().color, OfflinePopup.GreenKey, rate.name + " 요율 글자");
                // 판 안인가는 배치 좌표(UiKit.Place · 부모 왼쪽 위 원점)로 본다 — 세계 좌표는 CI 런 113·121 에서 같은 값(257.5)을 내며 여백 변경에 반응하지 않았다(결정 173 기록)
                float rateBottom = -rate.anchoredPosition.y + rate.sizeDelta.y;
                Assert.LessOrEqual(rateBottom, top.sizeDelta.y + 0.5f, rate.name + ": 요율 칸 아래 끝(" + rateBottom + ") 이 머리 판 높이(" + top.sizeDelta.y + ") 안에 있다");
                Assert.Greater(rate.anchoredPosition.y, -top.sizeDelta.y, rate.name + ": 요율 칸이 판 안에서 시작한다");
            }
            Assert.AreEqual(World(coin).yMin, World(hammer).yMin, 1f, "두 요율 칸은 같은 높이");
            Assert.Less(World(coin).xMax, World(hammer).xMin, "코인 칸이 해머 칸 왼쪽");
            Assert.AreEqual("1.13/초", FindUnder(coin, "text").GetComponent<TextMeshProUGUI>().text);
            Assert.AreEqual("1.14/분", FindUnder(hammer, "text").GetComponent<TextMeshProUGUI>().text);

            // 수집 버튼 우상단 빨간 점(흰 테)
            RectTransform collect = Find("collect"), dot = FindUnder(collect, "dot");
            Rect rb = World(collect), rd = World(dot);
            AssertColor(FindUnder(collect, "face").GetComponent<Image>().color, "pp_blue", "[수집] 버튼 면(정본 3548 .modal-card .btn.primary = --pp-blue · T144)");
            AssertColor(FindUnder(dot, "face").GetComponent<Image>().color, "pp_red", "빨간 점");
            AssertColor(FindUnder(dot, "line").GetComponent<Image>().color, "white", "빨간 점 테");
            Assert.Greater(rd.center.x, rb.xMax - rd.width, "점은 버튼 오른쪽 끝에");
            Assert.Greater(rd.center.y, rb.yMax - rd.height, "점은 버튼 위 끝에");
            Assert.Less(rd.xMin, rb.xMax, "점이 버튼에 걸친다(완전히 밖이 아니다)");

            // 글자 하한 · 콘솔 빨강 0 · ✕ 닫힘
            UiCatalog cat = UiCatalog.Instance;
            foreach (TMP_Text t in PopupLayer.Instance.Find(OfflinePopup.Name).Root.GetComponentsInChildren<TMP_Text>(true))
            {
                UiTextKindTag tag = t.GetComponent<UiTextKindTag>();
                Assert.IsNotNull(tag, t.name + " 은 UiKit.Text 를 거치지 않았다");
                Assert.GreaterOrEqual(t.fontSize, cat.Kind(tag.Kind).min, t.name + " 글자 하한");
            }
            Button x = null;
            foreach (Button b in PopupLayer.Instance.Find(OfflinePopup.Name).Root.GetComponentsInChildren<Button>(true)) if (b.name == "x-btn") x = b;
            Assert.IsNotNull(x, "✕ 가 없다");
            x.onClick.Invoke();
            yield return null;
            Assert.IsFalse(h.Popups.IsOpen(OfflinePopup.Name), "✕ 는 단순 닫힘");
            log.AssertNoRed();
            log.Dispose();
        }
        /// <summary>T417 — 정본 7268~7269(파일 끝 · 최종값) `.offline-rate-icon.coin, .hammer { background: none; border: none }` · `.ico { width/height: 100% }`:
        /// 요율 배지에 면·테 원이 없고 아이콘이 배지 칸을 꽉 채운다(전엔 line·face 두 원 + 인셋 20% · 런 872 코인 금테가 먹히고 초록 원이 해머 뒤에 섰다).</summary>
        [UnityTest]
        public IEnumerator 요율_배지는_정본_최종값대로_색_원이_없고_아이콘이_칸을_꽉_채운다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            OfflinePopup.Show(h, new OfflineReward { Elapsed = 5000, Counted = 3600, Coins = 8870, Hammers = 149.05, CoinRate = 1.13, HammerRate = 1.14 });
            yield return null;
            Canvas.ForceUpdateCanvases();
            foreach (string name in new[] { "coin", "hammer" })
            {
                RectTransform circle = FindUnder(Find(name), "circle");
                int rings = 0;
                foreach (Image im in circle.GetComponentsInChildren<Image>(true))
                    if (im.name == "line" || im.name == "face") rings++;
                Assert.AreEqual(0, rings, name + " — 정본 7268 이 끈 면·테 원이 클론에 남아 있다");
                RectTransform ico = FindUnder(circle, "ico");
                Assert.AreEqual(Vector2.zero, ico.offsetMin, name + " — 아이콘 인셋 0(정본 .ico 100%)");
                Assert.AreEqual(Vector2.zero, ico.offsetMax, name + " — 아이콘 인셋 0(정본 .ico 100%)");
                Assert.AreEqual(circle.rect.width, ico.rect.width, 0.01f, name + " — 아이콘 사각 = 배지 사각(2.6rem)");
                Assert.AreEqual(circle.rect.height, ico.rect.height, 0.01f, name + " — 아이콘 사각 = 배지 사각(2.6rem)");
            }
            h.Popups.Hide(OfflinePopup.Name);
            yield return null;
        }
    }
}
