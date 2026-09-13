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
    /// T99 — 채팅 공유 카드의 전투력 자리(정본 `ui.js` 5264·5269 `IconGen.img('power') + U.fmt(cp)`)가 «⚔» 글자(글꼴에 없어 □)가 아니라
    /// `power` 아이콘 + 수로 선다. 채팅 씨앗(`Chat.Seed`)이 공유 카드를 하나 끼우므로 채팅을 열기만 하면 카드가 있다.
    /// 그림이 아니라 **계층**으로 본다 — 촬영이 없는 런에서도 도는 단언이다(T89 `ToastIconTests` 와 같은 길).
    /// </summary>
    public class ChatShareIconTests
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

        [UnityTest]
        public IEnumerator 공유_카드의_전투력은_power_아이콘_더하기_수이고_검_글자는_없다()
        {
            yield return Boot();
            Hud.Instance.ChatButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(PopupLayer.Instance.IsOpen(ChatScreen.Name), "채팅 줄 → 전체화면 채팅");
            Canvas.ForceUpdateCanvases();

            Popup p = PopupLayer.Instance.Find(ChatScreen.Name);
            Assert.IsNotNull(p);
            int cards = 0;
            foreach (RectTransform card in p.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (card.name != "share") continue;
                cards++;
                foreach (string sideName in new[] { "win", "lose" })
                {
                    Transform side = card.Find(sideName);
                    Assert.IsNotNull(side, "공유 카드에 «" + sideName + "» 쪽이 없다");
                    Transform ico = side.Find("cp-ico");
                    Assert.IsNotNull(ico, sideName + ": 전투력 아이콘 칸(cp-ico)이 없다");
                    Image img = ico.GetComponent<Image>();
                    Assert.IsNotNull(img, sideName + ": cp-ico 에 Image 가 없다");
                    Assert.IsNotNull(img.sprite, sideName + ": power 아이콘 스프라이트가 비었다(T31 아틀라스·카탈로그)");
                    RectTransform ir = (RectTransform)ico;
                    Assert.Greater(ir.rect.width, 0f, sideName + ": 아이콘 칸 폭");
                    Assert.AreEqual(ir.rect.width, ir.rect.height, 0.01f, sideName + ": 아이콘 칸은 정사각");

                    Transform cpTr = side.Find("cp");
                    Assert.IsNotNull(cpTr, sideName + ": 전투력 글자(cp)가 없다");
                    TextMeshProUGUI cp = cpTr.GetComponent<TextMeshProUGUI>();
                    Assert.IsNotNull(cp);
                    Assert.IsFalse(string.IsNullOrEmpty(cp.text), sideName + ": 전투력 수가 비었다");
                    Assert.IsFalse(cp.text.Contains("⚔"), sideName + ": 글자 조각에 ⚔ 가 남았다: «" + cp.text + "»");

                    // 아이콘이 수의 왼쪽에 선다(정본 순서: 아이콘 → 수).
                    Vector3[] a = new Vector3[4], b = new Vector3[4];
                    ir.GetWorldCorners(a);
                    cp.rectTransform.GetWorldCorners(b);
                    Assert.LessOrEqual(a[2].x, b[0].x + 0.5f, sideName + ": 아이콘이 수의 왼쪽에 있어야 한다");
                }
            }
            Assert.GreaterOrEqual(cards, 1, "채팅 씨앗(Chat.Seed)에 공유 카드가 하나는 있어야 한다");

            // 채팅 화면 어느 글자에도 ⚔ 가 없다(이 화면에서 그 글자를 쓰던 자리는 공유 카드뿐이었다).
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
                Assert.IsFalse(t.text != null && t.text.Contains("⚔"), t.name + " 에 ⚔ 글자가 남았다: «" + t.text + "»");
        }
    }
}
