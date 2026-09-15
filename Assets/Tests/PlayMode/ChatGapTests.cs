using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T364 6회차 — 채팅 화면에서 정본이 **앱 폭 비율**로 못 박은 틈 셋. 정본 `style.css` 3309 `.chat-row { gap: .008W }` ·
    /// 3442·3445 `.chat-input-bar { gap: .022W · padding … .024W(우) … .02W(좌) }`. 클론은 셋 다 `rem`(앱 **높이** 기준) 어림이었다.
    /// 재는 것: ⓐ 아바타 오른끝 ↔ 이름줄 왼끝 = 표 ⓑ 뒤로 버튼 왼끝 = 표(좌 인셋) ⓒ 버튼 오른끝 ↔ 입력칸 왼끝 = 표(틈)
    /// ⓓ 입력칸 오른끝 ↔ 바 오른끝 = 표(우 인셋) ⓔ 그래서 정본 주석이 «자동으로 따라온다» 고 한 **입력칸 좌 11.82%W · 폭 85.37%W** 가 선다.
    /// </summary>
    public class ChatGapTests
    {
        static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null; yield return null;
            float t = 0f;
            while (!(UiRoot.Instance != null && UiRoot.Instance.App != null && MetaHost.Ready) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            yield return null;
        }

        static Transform FindActive(Transform root, string name)
        {
            if (!root.gameObject.activeInHierarchy) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { Transform r = FindActive(root.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        /// <summary>앱 상자 기준 가로 자리(왼끝, 오른끝) — 기준 폭(RefW)으로 환산한 px.</summary>
        static Vector2 SpanX(RectTransform app, RectTransform rt)
        {
            var a = new Vector3[4]; var c = new Vector3[4];
            app.GetWorldCorners(a); rt.GetWorldCorners(c);
            float appW = a[2].x - a[1].x;
            Assert.Greater(appW, 0f, "앱 상자 폭");
            return new Vector2((c[1].x - a[1].x) / appW * UiKit.RefW, (c[2].x - a[1].x) / appW * UiKit.RefW);
        }

        [UnityTest]
        public IEnumerator 채팅_틈_셋이_정본_앱_폭_비율대로_선다()
        {
            yield return Boot();
            // 채팅은 **탭이 아니다** — HUD 의 채팅 줄을 눌러 전체화면 팝업으로 연다(`ChatShareIconTests` 와 같은 길).
            // 런 712 에서 내 첫 판이 `TabBar.OnTab("chat")` 로 열려다 «모르는 탭» 으로 넘어졌다.
            Hud.Instance.ChatButton.onClick.Invoke();
            yield return null; yield return null;
            Assert.IsTrue(PopupLayer.Instance.IsOpen(ChatScreen.Name), "채팅 줄 → 전체화면 채팅");
            Canvas.ForceUpdateCanvases();

            RectTransform app = (RectTransform)UiRoot.Instance.App;
            float W = UiKit.RefW;
            // 찾는 자리는 **채팅 팝업 안**이다(같은 이름 상자가 다른 화면에도 있다) — 앱 상자는 «가로 자리를 재는 자» 로만 쓴다.
            Popup pop = PopupLayer.Instance.Find(ChatScreen.Name);
            Assert.IsNotNull(pop, "채팅 팝업");
            Transform croot = pop.Root;

            Transform bar = FindActive(croot, "input-bar");
            Assert.IsNotNull(bar, "입력 바");
            Vector2 barX = SpanX(app, (RectTransform)bar);
            // 이름으로 찾되 **그 상자 안**에서 찾는다 — `avatar`·`close` 는 상단바·팝업에도 있는 흔한 이름이다
            Transform back = FindActive(bar, "close"), input = FindActive(bar, "input");
            Assert.IsNotNull(back, "뒤로 버튼"); Assert.IsNotNull(input, "입력칸");
            Vector2 backX = SpanX(app, (RectTransform)back);
            Vector2 inX = SpanX(app, (RectTransform)input);

            Assert.AreEqual(ChatUi.W("chat_bar_pad_l_w"), backX.x - barX.x, 1.5f, "정본 3445 padding-left .02W — 좌 인셋");
            Assert.AreEqual(ChatUi.W("chat_bar_gap_w"), inX.x - backX.y, 1.5f, "정본 3442 gap .022W — 버튼 ↔ 입력칸");
            Assert.AreEqual(ChatUi.W("chat_bar_pad_r_w"), barX.y - inX.y, 1.5f, "정본 3445 padding-right .024W — 우 인셋");
            // 정본 주석 3441: 이 셋이 맞으면 입력칸 좌 11.82%W · 폭 85.37%W 가 «자동으로 따라온다».
            // ⚠ 그 주석의 «간격 13px=2.60%W» 는 **찍힌 화면 실측**이고 선언은 `.022`(= 10.98px)다 — 둘이 2px 어긋난다.
            //   §1 대로 **선언**을 따랐으므로 따라오는 두 수는 11.41%W · 86.19%W 로 주석보다 0.4~0.8%p 씩 빗나간다(결정 · 회차 기록).
            //   그래서 여기 허용은 ±1.0%p 다 — 주석을 닻으로 남기되 선언을 따른 결과를 거짓으로 통과시키지 않는 폭이다.
            Assert.AreEqual(0.1182f * W, inX.x, W * 0.010f, "정본 주석 3441 «입력칸 좌 11.82%W»(선언을 따르면 11.41%W)");
            Assert.AreEqual(0.8537f * W, inX.y - inX.x, W * 0.010f, "정본 주석 3441 «입력칸 폭 85.37%W»(선언을 따르면 86.19%W)");

            Transform row = FindActive(croot, "msg");
            Assert.IsNotNull(row, "말풍선 줄 — 세이브에 채팅 줄이 하나도 없으면 이 자를 못 잰다");
            Transform avatar = FindActive(row, "avatar"), nameLine = FindActive(row, "name-line");
            Assert.IsNotNull(avatar, "그 줄의 아바타"); Assert.IsNotNull(nameLine, "그 줄의 이름줄");
            Vector2 avX = SpanX(app, (RectTransform)avatar);
            Vector2 nmX = SpanX(app, (RectTransform)nameLine);
            Assert.AreEqual(ChatUi.W("chat_row_gap_w"), nmX.x - avX.y, 1.5f, "정본 3309 .chat-row gap .008W — 아바타 ↔ 말풍선");
        }
    }
}
