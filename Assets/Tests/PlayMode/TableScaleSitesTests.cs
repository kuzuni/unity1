using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T378 — «표값 × 박힌 상수» 자리를 걷은 뒤 그 자리가 **표값 그대로** 서는지(자 `tools/check_table_scale.py` 는 코드 글자를 보고,
    /// 이 자는 실제 RectTransform 을 본다). 자리를 걷을 때마다 칸을 하나씩 더한다.
    /// </summary>
    public class TableScaleSitesTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = Time.realtimeSinceStartup;
            while (!(MetaHost.Ready && PopupLayer.Instance != null && UiRoot.Instance != null))
            {
                Assert.Less(Time.realtimeSinceStartup - t, 20f, "MetaHost/PopupLayer 가 20초 안에 준비되지 않았다");
                yield return null;
            }
            yield return null;
        }

        /// <summary>3회차 — 프로필 연필 버튼: 정본 3008 `.profile-edit-btn { width: 1.5rem; height: 1.5rem }` = 표 `profile_edit`(0.0284H) 그대로(전엔 ×1.3).</summary>
        [UnityTest]
        public IEnumerator 프로필_연필_버튼은_표_profile_edit_그대로_1_5rem_정사각이다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            ProfilePopup.Open(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(ProfilePopup.Name);
            Assert.IsNotNull(p, "프로필 팝업");
            Button avEdit = null;
            foreach (Button b in p.Root.GetComponentsInChildren<Button>(true)) if (b.name == "avatar-edit") avEdit = b;
            Assert.IsNotNull(avEdit, "연필 버튼(avatar-edit)");
            Rect r = avEdit.GetComponent<RectTransform>().rect;
            float expect = UiKit.H("profile_edit");
            Assert.AreEqual(expect, r.width, 0.5f, "폭 = 표 profile_edit(곱 없이)");
            Assert.AreEqual(expect, r.height, 0.5f, "높이 = 표 profile_edit(곱 없이)");
            Assert.AreEqual(PopupKit.Rem * 1.5f, r.width, 0.5f, "정본 1.5rem — 표 0.0284 가 그 값이다(1.5 × rem_h)");
            Assert.Less(r.width, UiKit.H("profile_edit") * 1.3f - 1f, "옛 ×1.3 크기가 아니다");
            PopupLayer.Instance.Hide(ProfilePopup.Name);
            yield return null;
        }

        /// <summary>6회차 — 상점 머리 재화 알약: 정본 3968~3975 `.shop-sheet .sheet-head .cur-pill { width: .155W; height: .0239H }` = 표 `shop_cur_w`·`shop_cur_h` 그대로(전엔 높이 ×1.6).</summary>
        [UnityTest]
        public IEnumerator 상점_재화_알약은_표_shop_cur_h_그대로다()
        {
            yield return Boot();
            UiRoot.Instance.TabBar.OnTab("shop");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(ShopSheet.Name);
            Assert.IsNotNull(p, "상점 시트");
            int bars = 0;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name != "coin-bar" && rt.name != "gem-bar") continue;
                bars++;
                Assert.AreEqual(UiKit.H("shop_cur_h"), rt.rect.height, 0.5f, rt.name + ": 높이 = 표 shop_cur_h(곱 없이)");
                Assert.AreEqual(UiKit.L("shop_cur_w") * UiKit.RefW, rt.rect.width, 0.5f, rt.name + ": 폭 = 표 shop_cur_w");
                Assert.Less(rt.rect.height, UiKit.H("shop_cur_h") * 1.6f - 1f, rt.name + ": 옛 ×1.6 높이가 아니다");
            }
            Assert.AreEqual(2, bars, "코인·젬 알약 둘");
            UiRoot.Instance.TabBar.OnTab("shop");
            yield return null;
        }

        /// <summary>6회차 — 채팅 목록 상자: 정본 3265 `.chat-card { padding: 0 }` · 3294 `.chat-list { padding: 0 }` — 카드 위끝에서 바로 시작한다(전엔 상단바×0.3 인셋).</summary>
        [UnityTest]
        public IEnumerator 채팅_목록은_카드_위끝에서_인셋_없이_시작한다()
        {
            yield return Boot();
            Hud.Instance.ChatButton.onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(ChatScreen.Name);
            Assert.IsNotNull(p, "채팅 화면");
            RectTransform listBox = null;
            foreach (RectTransform rt in p.Root.GetComponentsInChildren<RectTransform>(true)) if (rt.name == "list-box") { listBox = rt; break; }
            Assert.IsNotNull(listBox, "list-box");
            Assert.AreEqual(1f, listBox.anchorMax.y, 1e-4f, "위끝 앵커");
            Assert.AreEqual(0f, listBox.offsetMax.y, 0.5f, "위 인셋 0(정본 padding 0 · 전엔 topbar_h×0.3)");
            PopupLayer.Instance.Hide(ChatScreen.Name);
            yield return null;
        }
    }
}
