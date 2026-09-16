using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Forging;
using Forge.Core.Pets;
using Forge.Core.Save;
using CoreRng = Forge.Core.Data.Rng;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T377 3회차 — 정본 `style.css` 8686 `.btn.btn.danger.danger, .btn.btn.sell.sell { background-color: #ff1017; box-shadow: inset 0 -.22rem 0 #4e0507 }` 는
    /// 전역 토큰(`pp_red` #e8362f · `pp_red_dk`)이 아니라 버튼 규칙에만 준 리터럴이다(8692). 판매 버튼 두 자리(비교 팝업 · 판매 경고)의
    /// 면·턱 `Image` 가 표 `PinnedColorUi.json` 의 값으로 서고, 전역 토큰 값이 아닌지 잰다. 표값이 정본과 같은지는 `tools/check_pinned_colors.py` 가 본다.
    /// 눈 확인은 `screen_craft-compare.png` 판매 버튼 면 (255,16,23)±2 · 턱 (78,5,7)±4.
    /// </summary>
    public class PinnedColorSitesTests
    {
        static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null; yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            yield return null;
        }

        static void AssertPinned(Transform sell, string what)
        {
            Assert.IsNotNull(sell, what + " — 판매 버튼(sell)");
            Image face = sell.Find("face").GetComponent<Image>();
            Image lip = sell.Find("lip").GetComponent<Image>();
            Color wantFace = PinnedColorUi.C("sell_btn_face"), wantLip = PinnedColorUi.C("sell_btn_lip");
            Assert.AreEqual(wantFace, face.color, what + " — 면 = 표 sell_btn_face(정본 8686 #ff1017)");
            Assert.AreEqual(wantLip, lip.color, what + " — 턱 = 표 sell_btn_lip(정본 8686 #4e0507)");
            Assert.AreNotEqual(UiKit.C("pp_red"), face.color, what + " — 면이 전역 토큰 pp_red 가 아니다(8692 «토큰을 옮기지 말 것»)");
            Assert.AreNotEqual(UiKit.C("pp_red_dk"), lip.color, what + " — 턱이 전역 토큰 pp_red_dk 가 아니다");
        }

        /// <summary>비교 팝업(정본 ui.js 3266 `.btn.sell`) — 촬영 자 `craft-compare` 와 같은 열기.</summary>
        [UnityTest]
        public IEnumerator 비교_팝업_판매_버튼의_면과_턱은_못박은_리터럴이다()
        {
            yield return Boot();
            ForgeHost F = ForgeHost.Instance;
            ForgeItem it = F.Engine.RollItem();
            it.Subs = SubstatRoll.Roll(F.Defs, CoreRng.Mulberry(43224), 2);
            ForgeCraftPopup.Show(F, it);
            yield return null; yield return null;
            Popup p = F.Meta.Popups.Find(ForgeCraftPopup.Name);
            Assert.IsNotNull(p, "비교 팝업이 열렸다");
            AssertPinned(p.Root.Find("card/lower/row/sell"), "비교 팝업");
            ForgeCraftPopup.Hide(F);
            yield return null;
        }

        /// <summary>판매 경고(정본 ui.js 3865 `.btn.danger`).</summary>
        [UnityTest]
        public IEnumerator 판매_경고_판매_버튼의_면과_턱은_못박은_리터럴이다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeItem a = h.Engine.RollItem(), b = h.Engine.RollItem();
            ForgeCraftPopup.ShowSellConfirm(h, a, b);
            yield return null;
            Popup p = PopupLayer.Instance.Find(ForgeCraftPopup.SellName);
            Assert.IsNotNull(p, "판매 경고 팝업");
            AssertPinned(p.Root.Find("card/row/sell"), "판매 경고");
            PopupLayer.Instance.Hide(ForgeCraftPopup.SellName);
            yield return null;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform t = FindDeep(root.GetChild(i), name);
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>T377 4회차 — 모루 «보류» 배지(정본 `style.css` 999 `.anvil-btn.held-slot .held-tag { background: #f0a020 }`).
        /// 전역 노랑 토큰 `coin`(#ffd54f)과 다른 앰버라 자리 전용 키가 쥔다.</summary>
        [UnityTest]
        public IEnumerator 모루_보류_배지의_면은_못박은_앰버지_전역_노랑_토큰이_아니다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeItem it = h.Engine.RollItem();
            h.SetPendingCraft(it);
            ForgeSheet.Render(h);
            yield return null;
            Transform tagBox = FindDeep(UiRoot.Instance.Sheet, "held-tag-bg");
            Assert.IsNotNull(tagBox, "보류 배지 상자(held-tag-bg) — 대기품을 세웠으니 모루 자리에 선다");
            Image bg = tagBox.Find("bg").GetComponent<Image>();
            Color want = PinnedColorUi.C("held_tag_face");
            Assert.AreEqual(want, bg.color, "배지 면 = 표 held_tag_face(정본 999 #f0a020)");
            Assert.AreNotEqual(UiKit.C("coin"), bg.color, "전역 노랑 토큰 coin(#ffd54f)이 아니다 — 정본이 선택자에만 못박은 값이다");
            h.ClearPendingCraft();
            ForgeSheet.Render(h);
            yield return null;
        }
    }
}
