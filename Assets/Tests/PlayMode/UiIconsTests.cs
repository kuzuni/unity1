using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Ui;
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
    }
}
