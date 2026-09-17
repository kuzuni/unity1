using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T395 — 프로필/설정 카드는 **앱 가운데**에 선다. 정본 `#profile-modal .idet-wrap { top: .8rem }`(style.css 3042)은 CSS 가 만든 위쪽 밀림을 되돌리는 보정값이지
    /// 자리가 아니다 — 클론 `PopupKit.Card` 는 카드 자체를 가운데 두므로 그 값을 옮기면 카드가 .8rem 아래로 한 번 더 간다(런 743 · 위끝 16.88%H ↔ 원작 14.73%H).
    /// </summary>
    public class ProfileCardTests
    {
        private static void DeleteSave()
        {
            try
            {
                string p = System.IO.Path.Combine(Application.persistentDataPath, (SaveIo.Defs != null ? SaveIo.Defs.SaveKey : "forgeclone_save_v1") + ".json");
                if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
            }
            catch (System.Exception) { }
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
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다");
            Assert.IsNotNull(PopupLayer.Instance, "팝업 층이 서지 않았다");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 프로필_카드는_정본_보정값_없이_앱_가운데에_선다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            ProfilePopup.Open(h);
            yield return null;
            Popup p = h.Popups.Find(ProfilePopup.Name);
            Assert.IsNotNull(p, "프로필 팝업");
            RectTransform card = null;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true)) if (rt.name == "card") { card = rt; break; }
            Assert.IsNotNull(card, "카드(card)");
            Assert.AreEqual(0f, card.anchoredPosition.y, 0.5f, "카드 세로 오프셋 0 — 정본 .8rem 은 CSS 보정값이라 옮기지 않는다");
            Assert.AreEqual(0.5f, card.anchorMin.y, 1e-4f); Assert.AreEqual(0.5f, card.pivot.y, 1e-4f, "가운데 앵커·피벗");
            Assert.AreEqual(UiKit.L("profile_h") * UiKit.RefH, card.rect.height, 0.5f, "카드 높이는 표(profile_h)대로 — 건드리지 않는다");
            Assert.AreEqual(UiKit.L("profile_w") * UiKit.RefW, card.rect.width, 0.5f, "카드 폭은 표(profile_w)대로");
            ProfilePopup.Close(h);
            yield return null;
        }

        /// <summary>
        /// T432 — 정본 **3060** `.profile-rank-row .btn { width: calc(var(--app-w) * .2177 + 6.6px) }` · 바로 위 주석(3058)이 «**+6.6px 는 키라인 몫**» 이라 적어 둔다.
        /// 곧 표값 `.2177` 은 **파랑 채움** 폭(원작 042724 실측 108px)이고 상자는 그보다 키라인 두 겹만큼 넓다.
        /// 이 칸은 **무엇의 폭인가** 를 묻는다: 면(`face`) 폭 = `RefW × .2177` · 상자 폭 = 그것 + `line3_px × 2` · 두 채움 사이 텀은 안 움직인다.
        /// 카너스 지역 단위(`rect`)로 재다 — 표값과 같은 기준 px 단위라 되돌릴 것이 없다(결정 729).
        /// </summary>
        [UnityTest]
        public IEnumerator 랭킹_버튼은_표값이_상자가_아니라_파랑_채움_폭이다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            ProfilePopup.Open(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Popup p = h.Popups.Find(ProfilePopup.Name);
            Assert.IsNotNull(p, "프로필 팝업");
            RectTransform b1 = null, b2 = null;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name == "power-rank") b1 = rt; else if (rt.name == "clan-rank") b2 = rt;
            }
            Assert.IsNotNull(b1, "파워 랭킹 버튼(power-rank)");
            Assert.IsNotNull(b2, "클랜 랭킹 버튼(clan-rank)");
            RectTransform f1 = (RectTransform)b1.Find("face"), f2 = (RectTransform)b2.Find("face");
            Assert.IsNotNull(f1, "파랑 면(face)");
            Assert.IsNotNull(f2, "둘째 버튼의 면");

            float want = UiKit.RefW * UiKit.L("profile_rank_btn_w");
            Assert.AreEqual(0.2177f, UiKit.L("profile_rank_btn_w"), 1e-6f, "표값은 정본 3060 그대로 .2177 — 이 절은 값이 아니라 «무엇의 폭인가» 를 고친다");
            Assert.AreEqual(want, f1.rect.width, 1f,
                "파랑 채움 폭 = 앱 폭 × .2177(정본의 «원본 채움 108px») · 실측 " + f1.rect.width.ToString("0.0"));
            Assert.AreEqual(want + PopupKit.Line3 * 2f, b1.rect.width, 1f,
                "상자 폭 = 채움 + 키라인 두 겹(정본의 +6.6px) · 실측 " + b1.rect.width.ToString("0.0"));
            Assert.AreEqual(f1.rect.width, f2.rect.width, 0.5f, "두 버튼의 채움은 같다");

            // 틈은 안 움직인다: 정본의 «채움 사이 16px» = `gap: .5rem` + 키라인 두 겹. 상자 틈은 그대로 `.5rem` 이어야 한다.
            // ⚠ 한 자에서 **세계 좌표(`position`)와 캐너스 지역 치수(`rect`)를 섞지 않는다**(결정 729) — 둘 다 지역 단위인 `anchoredPosition`·`rect` 로만 잰다.
            float boxGap = b2.anchoredPosition.x - (b1.anchoredPosition.x + b1.rect.width);
            Assert.AreEqual(PopupKit.Rem * 0.5f, boxGap, 1f,
                "상자 틈은 .5rem 그대로다 — 폭을 넓혔다고 틈까지 밀리면 정본의 «틈 16px» 이 깨진다 · 실측 " + boxGap.ToString("0.0") + "px");
            Debug.Log("[T432] 채움 " + f1.rect.width.ToString("0.0") + "px · 상자 " + b1.rect.width.ToString("0.0") + "px · 표 " + want.ToString("0.0"));
            ProfilePopup.Close(h);
            yield return null;
        }

        /// <summary>T395 2회차 — 리그 보상 카드도 같다: 정본 `.lgr-overlay .idet-wrap { top: .76rem }` 은 CSS 보정값이라 옮기지 않는다.
        /// 종전 `rem * 0.76f` 는 PopupKit.Card 에서 «양수 = 위» 라 부호까지 반대로 베껴져 카드가 원작보다 1%p 위였다(런 754: 18.23 ↔ 원작 19.21%H).</summary>
        [UnityTest]
        public IEnumerator 리그_보상_카드는_정본_보정값_없이_앱_가운데에_선다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            LeagueSheet.OpenRewards(h);
            yield return null;
            Popup p = h.Popups.Find(LeagueSheet.RewardsName);
            Assert.IsNotNull(p, "리그 보상 팝업");
            RectTransform card = null;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true)) if (rt.name == "card") { card = rt; break; }
            Assert.IsNotNull(card, "카드(card)");
            Assert.AreEqual(0f, card.anchoredPosition.y, 0.5f, "카드 세로 오프셋 0 — 정본 .76rem 은 CSS 보정값이라 옮기지 않는다(전엔 +.76rem 위)");
            Assert.AreEqual(0.5f, card.anchorMin.y, 1e-4f); Assert.AreEqual(0.5f, card.pivot.y, 1e-4f, "가운데 앵커·피벗");
            Assert.AreEqual(UiKit.L("modal_wide_w") * UiKit.RefW, card.rect.width, 0.5f, "카드 폭은 표(modal_wide_w)대로 — 건드리지 않는다");
            h.Popups.Hide(LeagueSheet.RewardsName);
            yield return null;
        }

        /// <summary>
        /// T445 — 정본 3038 `.profile-sheet .profile-tabs { margin-top: auto; margin-bottom: 2.2rem }` + 1754 `.modal-card { padding: 1.1rem }`: CSS 마진은 패딩 상자 안에
        /// 놓이므로 탭 아래끝 → 카드 바닥 = 3.3rem(정본 주석 3036 «원본은 55px(6.2%H)»). 클론은 2.2rem 만 띄워 패딩 몫이 빠졌다(런 1048 37px ↔ 55px). 두 화면(프로필·설정) 다.
        /// </summary>
        [UnityTest]
        public IEnumerator 프로필과_설정의_탭_줄은_카드_바닥에서_패딩_더하기_2_2rem_위에_선다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            ProfilePopup.Open(h);
            yield return null; Canvas.ForceUpdateCanvases();
            Popup p = h.Popups.Find(ProfilePopup.Name);
            Assert.IsNotNull(p, "프로필 팝업");
            float rem = PopupKit.Rem, pad = UiKit.H("card_pad");
            float want = pad + rem * 2.2f;
            foreach (string view in new[] { "profile", "settings" })
            {
                if (view == "settings")
                {
                    Transform tb = p.Root.GetComponentInChildren<RectTransform>(true).GetComponentsInChildren<UnityEngine.UI.Button>(true)
                        .FirstOrDefault(b => b.name == "tab-settings")?.transform;
                    Assert.IsNotNull(tb, "설정 탭 버튼");
                    tb.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                    yield return null; Canvas.ForceUpdateCanvases();
                    p = h.Popups.Find(ProfilePopup.Name);
                    Assert.IsNotNull(p, "설정 화면");
                }
                RectTransform card = (RectTransform)p.Root.Find("card");
                RectTransform tabs = (RectTransform)card.Find("tabs");
                Assert.IsNotNull(tabs, view + " — 탭 줄");
                Assert.AreEqual(want, tabs.anchoredPosition.y, 0.5f, view + " — 탭 줄 아래끝 → 카드 바닥 = card_pad(1.1rem) + 2.2rem(정본 3038 · 55px@960 ≈ " + (want * 960f / UiKit.RefH).ToString("0.0") + ")");
                Vector3[] cc = new Vector3[4], tc = new Vector3[4]; card.GetWorldCorners(cc); tabs.GetWorldCorners(tc);
                float gap = card.InverseTransformPoint(tc[0]).y - card.InverseTransformPoint(cc[0]).y;
                Assert.AreEqual(want, gap, 1.0f, view + " — 실물 틈도 같다");
                Assert.Greater(gap, rem * 2.2f + 1f, view + " — 2.2rem 만 띄우던 옛 값보다 패딩 몫만큼 높다");
            }
            ProfilePopup.Close(h);
            yield return null;
        }
    }
}
