using System.Collections;
using NUnit.Framework;
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
    /// T345 3회차 — 채팅 화면의 둥근 모서리 다섯 자리가 **표(`RadiusUi.json`)** 를 읽고 정본 값으로 선다:
    /// 말풍선 `.chat-bubble` .42rem · 공유 카드 `.chat-share-card` **0(각진)** · 입력칸 `.chat-input-bar input` .3rem ·
    /// 둥근 버튼 `.chat-input-bar .btn.round` .35rem · 공유 아바타 `.chat-share-side .icon-circle.sm` .28rem.
    /// 반지름은 9-슬라이스 배율(<see cref="UiShapes.RoundedMultiplier"/>)로 읽는다 — 화면 반지름 = 표값 × 1rem px.
    /// 채팅 씨앗이 공유 카드와 말풍선을 끼우므로 채팅을 열기만 하면 다섯 자리가 다 있다(T99 `ChatShareIconTests` 와 같은 길).
    /// </summary>
    public class ChatRadiusTests
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
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다");
            Assert.IsNotNull(PopupLayer.Instance, "팝업 층이 서지 않았다");
            yield return null;
        }

        static float Mult(string key) { return UiShapes.RoundedMultiplier(RadiusUi.Px(key)); }

        static void AssertRounded(Image img, string key, string what)
        {
            Assert.IsNotNull(img, what + ": Image 가 없다");
            Assert.AreEqual(UiShapes.Rounded, img.sprite, what + ": 둥근 9-슬라이스 스프라이트여야 한다");
            Assert.AreEqual(Mult(key), img.pixelsPerUnitMultiplier, 1e-3f, what + ": 반지름이 표 «" + key + "» 와 다르다");
        }

        [Test]
        public void 표의_채팅_다섯_값은_정본_rem_그대로다()
        {
            float px = RadiusUi.PxPerRem;
            Assert.Greater(px, 0f, "1rem px");
            Assert.AreEqual(0.42f * px, RadiusUi.Px("chat_bubble_r_rem"), 1e-3f, "정본 3371 .chat-bubble .42rem");
            Assert.AreEqual(0f, RadiusUi.Px("chat_share_card_r_rem"), 1e-6f, "정본 3389 .chat-share-card 0");
            Assert.IsTrue(RadiusUi.IsSquare("chat_share_card_r_rem"), "공유 카드는 각진 자리");
            Assert.AreEqual(0.3f * px, RadiusUi.Px("chat_input_r_rem"), 1e-3f, "정본 3449 input .3rem");
            Assert.AreEqual(0.35f * px, RadiusUi.Px("chat_round_btn_r_rem"), 1e-3f, "정본 3270 .btn.round .35rem");
            Assert.AreEqual(0.28f * px, RadiusUi.Px("chat_share_avatar_r_rem"), 1e-3f, "정본 3401 .icon-circle.sm .28rem");
        }

        [UnityTest]
        public IEnumerator 채팅의_다섯_자리가_표의_반지름으로_서고_공유_카드는_각지다()
        {
            yield return Boot();
            Hud.Instance.ChatButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(PopupLayer.Instance.IsOpen(ChatScreen.Name), "채팅 줄 → 전체화면 채팅");
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(ChatScreen.Name);
            Assert.IsNotNull(p);

            int bubbles = 0, cards = 0, avatars = 0;
            Transform close = null, input = null;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name == "bubble")
                {
                    bubbles++;
                    AssertRounded(rt.Find("bg").GetComponent<Image>(), "chat_bubble_r_rem", "말풍선");
                }
                else if (rt.name == "share")
                {
                    cards++;
                    // RadiusUi.Outlined(card, "face", …) 는 PopupKit.Outlined 와 같은 계층 — 상자 «face» 아래에 테 «line» 과 안쪽 면 «face» (런 554 의 NRE 는 이 한 단계를 빼먹은 자 탓)
                    Transform box = rt.Find("face");
                    Assert.IsNotNull(box, "공유 카드의 테·면 상자(face)");
                    Image line = box.Find("line").GetComponent<Image>(), face = box.Find("face").GetComponent<Image>();
                    Assert.AreNotEqual(UiShapes.Rounded, line.sprite, "공유 카드 테는 각져야 한다(정본 border-radius: 0)");
                    Assert.AreNotEqual(UiShapes.Rounded, face.sprite, "공유 카드 면은 각져야 한다(정본 border-radius: 0)");
                    foreach (string sideName in new[] { "win", "lose" })
                    {
                        Transform av = rt.Find(sideName + "/avatar");
                        Assert.IsNotNull(av, sideName + " 아바타");
                        avatars++;
                        AssertRounded(av.Find("line").GetComponent<Image>(), "chat_share_avatar_r_rem", sideName + " 아바타 테");
                    }
                }
                else if (rt.name == "close" && close == null) close = rt;
                else if (rt.name == "input" && input == null) input = rt;
            }
            Assert.Greater(bubbles, 0, "채팅 씨앗의 말풍선이 하나도 없다");
            Assert.Greater(cards, 0, "채팅 씨앗의 공유 카드가 없다");
            Assert.AreEqual(cards * 2, avatars, "공유 카드마다 아바타 둘");
            Assert.IsNotNull(close, "뒤로(둥근) 버튼");
            AssertRounded(close.Find("line").GetComponent<Image>(), "chat_round_btn_r_rem", "둥근 버튼 테");
            Assert.IsNotNull(input, "입력칸");
            AssertRounded(input.Find("line").GetComponent<Image>(), "chat_input_r_rem", "입력칸 테");
        }
    }
}
