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
    /// T379 — 채팅 공유 카드 한 쪽은 정본 style.css 3396 `.chat-share-side` 대로 **세로 3단(타일 → 이름 → 전투력) · 가운데 정렬**이고
    /// 틈은 앱 폭 × .010 · 패딩 .014W/.016W · 타일 .0882W · «승리» 라벨은 타일 하단 모서리에 걸터앉는다(아래끝이 타일 아래끝보다 .020W 아래 · 3424).
    /// 종전 클론은 «아바타 왼쪽 + 글 오른쪽» 가로 배치였다. 여기서는 세 단의 x 중심이 같은가 · 위아래 틈이 표대로인가 · 라벨 자리 · 카드 높이 = 쌓임인가를 잰다.
    /// </summary>
    public class ChatShareLayoutTests
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

        static IEnumerator Boot()
        {
            DeleteSave();
            SceneManager.LoadScene("SampleScene");
            yield return null; yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다");
            yield return null;
        }

        static RectTransform FindIn(Transform root, string name)
        {
            foreach (RectTransform r in root.GetComponentsInChildren<RectTransform>(true)) if (r.name == name) return r;
            return null;
        }

        static float CenterX(RectTransform r) { return r.anchoredPosition.x + r.sizeDelta.x * 0.5f; }
        static float Top(RectTransform r) { return -r.anchoredPosition.y; }

        [UnityTest]
        public IEnumerator 공유_카드_한_쪽은_타일_이름_전투력이_세로로_가운데_정렬이고_승리는_타일_모서리에_걸터앉는다()
        {
            yield return Boot();
            Hud.Instance.ChatButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(PopupLayer.Instance.IsOpen(ChatScreen.Name), "채팅 줄 → 전체화면 채팅");
            Canvas.ForceUpdateCanvases();
            yield return null;
            Popup p = PopupLayer.Instance.Find(ChatScreen.Name);
            RectTransform card = FindIn(p.Root, "share");
            Assert.IsNotNull(card, "채팅 씨앗(Chat.Seed)에 공유 카드가 하나는 있어야 한다");

            float aw = UiKit.RefW, rem = PopupKit.Rem, lineH = PopupKit.FontSize(TextKind.Sub) * 1.2f, tol = 1.0f;
            float tile = StaticIconsUi.L("chat_share_tile_aw") * aw, gap = StaticIconsUi.L("chat_share_side_gap_aw") * aw;
            float padT = StaticIconsUi.L("chat_share_side_pad_top_aw") * aw, padB = StaticIconsUi.L("chat_share_side_pad_bot_aw") * aw;
            Assert.AreEqual(0.0882f * aw, tile, 0.01f, "정본 3402 타일 .0882W");
            Assert.AreEqual(0.010f * aw, gap, 0.01f, "정본 3397 gap .010W");
            Assert.AreEqual(padT + tile + gap * 2f + lineH * 2f + padB, card.sizeDelta.y, tol, "카드 높이 = 한 쪽의 세로 쌓임(라벨은 세로를 안 먹는다)");

            foreach (string sideName in new[] { "win", "lose" })
            {
                RectTransform side = (RectTransform)card.Find(sideName);
                Assert.IsNotNull(side, sideName);
                RectTransform av = (RectTransform)side.Find("avatar"), nm = (RectTransform)side.Find("name");
                RectTransform ico = (RectTransform)side.Find("cp-ico"), cp = (RectTransform)side.Find("cp");
                Assert.IsNotNull(av, sideName + " 타일"); Assert.IsNotNull(nm, sideName + " 이름"); Assert.IsNotNull(ico, sideName + " 아이콘"); Assert.IsNotNull(cp, sideName + " 전투력");
                float cx = side.sizeDelta.x * 0.5f;
                Assert.AreEqual(cx, CenterX(av), tol, sideName + ": 타일 x 중심 = 한 쪽 가운데(align-items:center)");
                Assert.AreEqual(cx, CenterX(nm), tol, sideName + ": 이름 상자 x 중심 = 가운데");
                float groupL = ico.anchoredPosition.x, groupR = cp.anchoredPosition.x + cp.sizeDelta.x;
                Assert.AreEqual(cx, (groupL + groupR) * 0.5f, tol, sideName + ": 아이콘+수 묶음의 x 중심 = 가운데");
                Assert.Less(groupL + ico.sizeDelta.x, cp.anchoredPosition.x + 0.01f, sideName + ": 아이콘이 수의 왼쪽");
                Assert.AreEqual(av.sizeDelta.x, tile, tol, sideName + ": 타일 한 변 = .0882W");
                Assert.AreEqual(padT, Top(av), tol, sideName + ": 타일 위 = 위 패딩 .014W");
                Assert.AreEqual(Top(av) + tile + gap, Top(nm), tol, sideName + ": 이름은 타일 아래 gap 뒤(세로 2단)");
                Assert.AreEqual(Top(nm) + lineH + gap, Top(cp), tol, sideName + ": 전투력은 이름 아래 gap 뒤(세로 3단)");
                RectTransform lb = (RectTransform)side.Find("label");
                if (sideName == "win")
                {
                    Assert.IsNotNull(lb, "이긴 쪽엔 «승리» 라벨");
                    Assert.AreEqual(cx, CenterX(lb), tol, "라벨 x 중심 = 타일 중심(left:50% · translateX(-50%))");
                    Assert.AreEqual(Top(av) + tile + 0.020f * aw, Top(lb) + lb.sizeDelta.y, tol, "라벨 아래끝 = 타일 아래끝 + .020W(bottom: -.020W · 모서리에 걸터앉는다)");
                    Assert.Greater(lb.GetSiblingIndex(), av.GetSiblingIndex(), "라벨은 타일 위에 겹친다");
                }
                else Assert.IsNull(lb, "진 쪽엔 라벨이 없다(정본 5266)");
            }
            PopupLayer.Instance.Hide(ChatScreen.Name);
            yield return null;
        }
    }
}
