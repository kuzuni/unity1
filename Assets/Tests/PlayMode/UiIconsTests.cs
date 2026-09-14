using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>T31 — 원작 아이콘 아틀라스(Resources/Icons)가 유니티에서 실제로 스프라이트로 서는가: 키 전부 Get → null 0 · rect = atlas.json · 부팅 HUD 가 그것을 쓴다 · 콘솔 빨강 0(러너가 막는다).</summary>
    public class UiIconsTests
    {
        [Test]
        public void 키_전부_Get_이_null_0_이고_rect_가_표와_같다()
        {
            IconAtlas atlas = UiIcons.Atlas;
            Assert.GreaterOrEqual(atlas.Entries.Count, 160);
            Assert.AreEqual(atlas.Sheets.Count, UiIcons.SheetCount);
            int n = 0;
            foreach (IconAtlasEntry e in atlas.Entries)
            {
                Sprite sp = UiIcons.Get(e.Name, e.Tint);
                Assert.IsNotNull(sp, "Get 이 null: " + e.Key);
                Assert.AreEqual(e.W, sp.rect.width, 0.01f, e.Key + " 폭");
                Assert.AreEqual(e.H, sp.rect.height, 0.01f, e.Key + " 높이");
                Assert.AreEqual(e.X, sp.rect.x, 0.01f, e.Key + " x");
                Assert.AreEqual(atlas.BottomUpY(e), sp.rect.y, 0.01f, e.Key + " y(아래에서)");
                Assert.AreEqual(atlas.Sheets[e.Atlas].W, sp.texture.width, e.Key + " 장 폭");
                Assert.AreEqual(atlas.Sheets[e.Atlas].H, sp.texture.height, e.Key + " 장 높이");
                Assert.AreSame(sp, UiIcons.Get(e.Name, e.Tint), "같은 키는 같은 스프라이트(캐시)");
                n++;
            }
            Assert.AreEqual(atlas.Entries.Count, n);
            Assert.IsNull(UiIcons.Get("no_such_icon"), "없는 키는 null");
        }

        [Test]
        public void 원작_도우미_이름이_같다()
        {
            Assert.IsNotNull(UiIcons.Skill("powerStrike"), "IconGen.skill(id) = sk_<id>");
            Assert.IsNotNull(UiIcons.Tab("pvp"), "IconGen.tab(name) = tab_<name>");
            Assert.IsNotNull(UiIcons.Avatar("🛡️"), "IconGen.avatar(emoji) = avatar_<코드포인트>");
            Assert.IsNotNull(UiIcons.Avatar("🧑‍🚀"), "ZWJ 아바타");
            Assert.IsNotNull(UiIcons.Get("egg", "#1cafff"), "등급색 알(rare)");
            Assert.IsNull(UiIcons.Avatar(null));
            var keys = new List<string>(UiIcons.Keys);
            Assert.AreEqual(UiIcons.Atlas.Entries.Count, keys.Count);
        }

        [UnityTest]
        public IEnumerator 부팅_HUD_아이콘이_원작_아틀라스에서_온다()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            Assert.IsNotNull(UiRoot.Instance, "UiRoot 가 서지 않았다");
            int fromAtlas = 0;
            bool coin = false;
            foreach (Image img in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (img.sprite == null || !img.sprite.name.StartsWith("ico:")) continue;
                fromAtlas++;
                if (img.sprite.name == "ico:coin") coin = true;
            }
            Assert.Greater(fromAtlas, 0, "UiKit.Icon 이 아틀라스 스프라이트를 하나도 안 썼다");
            Assert.IsTrue(coin, "상단바 코인 알약은 원작 IconGen coin");
        }
        /// <summary>T132 — 정본이 `IconGen` 아이콘으로 그리는데 클론이 안 부르던 자리 셋: 채팅 미리보기 아바타 `chatbubble`(ui.js 5294) ·
        /// 준비 중 팝업 바리케이드 `barrier`(1257 · 가로로 긴 틀) · 패스 칼 `passsword`(4924 · 카드 윗변 위로 솟고 리본이 덮는다).</summary>
        [UnityTest]
        public IEnumerator T132_정본이_아이콘으로_그리는_자리_셋이_아틀라스_스프라이트를_쥔다()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다");
            Assert.IsNotNull(Hud.Instance, "HUD 가 서지 않았다");

            Image av = FindImage(Hud.Instance.transform, "chat-preview-avatar");
            Assert.IsNotNull(av, "채팅 미리보기 아바타 칸이 없다");
            Assert.IsNotNull(av.sprite, "채팅 미리보기 아바타 스프라이트가 비었다");
            Assert.AreEqual("ico:chatbubble", av.sprite.name, "정본 ui.js 5294 = IconGen.img('chatbubble')");

            Popup stub = PopupLayer.Instance.ShowStub("시험", "설명");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Image bar = FindImage(stub.Root, "barrier");
            Assert.IsNotNull(bar, "준비 중 팝업에 바리케이드 칸이 없다");
            Assert.IsNotNull(bar.sprite);
            Assert.AreEqual("ico:barrier", bar.sprite.name, "정본 ui.js 1257 = IconGen.img('barrier')");
            LayoutElement le = bar.GetComponent<LayoutElement>();
            Assert.IsNotNull(le);
            Assert.Greater(le.preferredWidth, le.preferredHeight, "바리케이드 틀은 가로가 길다(.stub-ico.wide)");
            Assert.IsNotNull(FindImage(stub.Root, "barrier").transform.parent.Find("soon"), "글자가 같은 줄에 있다");
            PopupLayer.Instance.Hide("stub");
            yield return null;

            PassPopup.Open(MetaHost.Instance);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup pass = PopupLayer.Instance.Find(PassPopup.Name);
            Assert.IsNotNull(pass, "패스 팝업이 안 열렸다");
            Image sword = FindImage(pass.Root, "pass-sword");
            Assert.IsNotNull(sword, "패스 칼 칸이 없다");
            Assert.IsNotNull(sword.sprite);
            Assert.AreEqual("ico:passsword", sword.sprite.name, "정본 ui.js 4924 = IconGen.img('passsword')");
            RectTransform card = FindRt(pass.Root, "card");
            RectTransform banner = FindRt(pass.Root, "banner");
            Assert.IsNotNull(card); Assert.IsNotNull(banner);
            Vector3[] sc = new Vector3[4], cc = new Vector3[4];
            sword.rectTransform.GetWorldCorners(sc);
            card.GetWorldCorners(cc);
            Assert.Greater(sc[1].y, cc[1].y, "칼 윗변은 카드 윗변보다 위(정본 top −4.81rem)");
            Assert.Less(sc[0].y, cc[1].y, "칼 아랫부분은 카드에 걸친다(6.72rem 높이)");
            Assert.AreEqual(sword.transform.parent, banner.parent, "칼과 리본은 카드의 형제");
            Assert.Less(sword.transform.GetSiblingIndex(), banner.GetSiblingIndex(), "리본이 칼자루 위를 덮는다(정본 DOM 순서)");
            Assert.Greater(sword.rectTransform.rect.height, sword.rectTransform.rect.width, "칼은 세로로 길다(3.72×6.72rem)");
            PopupLayer.Instance.Hide(PassPopup.Name);
            yield return null;
        }

        private static Image FindImage(Transform root, string name)
        {
            foreach (Image img in root.GetComponentsInChildren<Image>(true)) if (img.name == name) return img;
            return null;
        }

        private static RectTransform FindRt(Transform root, string name)
        {
            foreach (RectTransform rt in root.GetComponentsInChildren<RectTransform>(true)) if (rt.name == name) return rt;
            return null;
        }
    }
}
