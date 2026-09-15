using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T33 24회차 — 정본 `pointer-events: none` 67 선언(연출 층 `#fx-layer`·`#boss-warning`·`#toasts`·`#coin-burst`… 와 장식 겹)의 뜻은 하나다:
    /// **연출이 떠 있어도 그 아래 버튼이 눌린다.** 클론은 장식 공장(`UiKit.Panel`·`Icon`·`Text`)이 `raycastTarget=false` 를 기본으로 두고
    /// 누르는 면(`Button`·hit·모달 딤)만 `true` 로 켠다 — 이 자는 그 정책이 화면에서 실제로 서는지를 **레이캐스트로** 잰다:
    /// 연출 한가운데 프레임에 탭바 버튼 중심으로 쏜 레이캐스트의 맨 위가 그 버튼이고, 연출 층에는 한 건도 안 걸린다.
    /// </summary>
    public class PointerPassTests
    {
        static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(UiRoot.Instance != null && UiRoot.Instance.App != null && MetaHost.Ready && PopupLayer.Instance != null) && t < 20f)
            { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(UiRoot.Instance, "UiRoot 가 20초 안에 안 섰다");
            yield return null;
        }

        static Vector2 ScreenCenterOf(RectTransform rt)
        {
            return RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));   // ScreenSpaceOverlay — 카메라 없음
        }

        /// <summary>앱 캔버스의 레이캐스터로 쏜 결과(맨 위가 첫째 — GraphicRaycaster 가 depth 로 정렬해 붙인다).</summary>
        static List<RaycastResult> Cast(Vector2 screen)
        {
            var results = new List<RaycastResult>();
            GraphicRaycaster gr = UiRoot.Instance.GetComponent<GraphicRaycaster>();
            Assert.IsNotNull(gr, "앱 캔버스에 GraphicRaycaster 가 있다(UiRoot.Create)");
            var ped = new PointerEventData(EventSystem.current) { position = screen };
            gr.Raycast(ped, results);
            return results;
        }

        static Button FirstTab()
        {
            TabBar tb = UiRoot.Instance.GetComponentInChildren<TabBar>(true);
            Assert.IsNotNull(tb, "탭바가 앱 캔버스 아래 있다");
            Assert.Greater(tb.Keys.Count, 0, "탭이 있다");
            Button b = tb.ButtonOf(tb.Keys[0]);
            Assert.IsNotNull(b, "첫 탭의 버튼");
            return b;
        }

        static void AssertTopIsButton(Button b, string when)
        {
            List<RaycastResult> hits = Cast(ScreenCenterOf(b.GetComponent<RectTransform>()));
            Assert.Greater(hits.Count, 0, when + " — 탭 버튼 중심에 레이캐스트가 아무것도 안 맞는다");
            Transform top = hits[0].gameObject.transform;
            Assert.IsTrue(top == b.transform || top.IsChildOf(b.transform),
                when + " — 맨 위가 탭 버튼이 아니라 «" + Path(top) + "» 다: 정본 pointer-events: none 자리가 터치를 가로챈다");
        }

        static void AssertNoneUnder(List<RaycastResult> hits, Transform root, string when)
        {
            foreach (RaycastResult r in hits)
                Assert.IsFalse(r.gameObject.transform == root || r.gameObject.transform.IsChildOf(root),
                    when + " — 연출 층 «" + Path(r.gameObject.transform) + "» 이 레이캐스트에 걸린다(정본 pointer-events: none)");
        }

        static string Path(Transform t)
        {
            string s = t.name;
            for (Transform p = t.parent; p != null && p != UiRoot.Instance.transform; p = p.parent) s = p.name + "/" + s;
            return s;
        }

        [UnityTest]
        public IEnumerator 평시에_탭_버튼_중심의_맨_위는_그_버튼이다()
        {
            yield return Boot();
            AssertTopIsButton(FirstTab(), "평시");
        }

        [UnityTest]
        public IEnumerator 보스_경고_한가운데_프레임에도_탭_버튼이_눌리고_경고_층은_한_건도_안_걸린다()
        {
            yield return Boot();
            Button tab = FirstTab();
            BattleOverlay o = BattleOverlay.Ensure();
            Assert.IsNotNull(o, "전투 오버레이");
            o.BossWarning(2.0);
            o.Tick(0.5f);                  // 딤 가득 · 점멸 정점(BossWarnArtTests 와 같은 프레임)
            yield return null;
            Assert.IsTrue(o.WarningActive, "경고가 떠 있다");
            AssertTopIsButton(tab, "보스 경고 중");
            // 경고 층은 화면 어디를 눌러도 안 걸린다 — 탭 중심 · 화면 한가운데 두 점
            AssertNoneUnder(Cast(ScreenCenterOf(tab.GetComponent<RectTransform>())), o.Layer, "보스 경고 중 · 탭 중심");
            AssertNoneUnder(Cast(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)), o.Layer, "보스 경고 중 · 화면 가운데");
            // 정리 — 남은 프레임을 밀어 경고를 끝낸다(다른 자의 촬영에 안 남게)
            for (int i = 0; i < 8 && o.WarningActive; i++) { o.Tick(0.5f); yield return null; }
        }

        [UnityTest]
        public IEnumerator 토스트_위를_눌러도_토스트는_안_걸린다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            h.Popups.Toast("정본 #toasts { pointer-events: none }");
            yield return null;
            RectTransform toast = null;
            foreach (RectTransform rt in UiRoot.Instance.GetComponentsInChildren<RectTransform>(true)) if (rt.name == "toast") toast = rt;
            Assert.IsNotNull(toast, "토스트가 섰다");
            List<RaycastResult> hits = Cast(ScreenCenterOf(toast));
            AssertNoneUnder(hits, toast, "토스트 위");
        }
    }
}
