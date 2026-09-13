using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game.Battle;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T76 — 전투 데미지 숫자는 원작 `#game-area > #fx-layer` 처럼 앱 상자의 **맨 아래** 층에 붙는다. 시트(상점)를 열어도 숫자가 팝업 층 위로 오지 않는다.
    /// 같은 캔버스 안에서는 형제 순서가 곧 그리기 순서다 — 앱 상자 바로 아래 조상의 형제 인덱스로 «누가 위인가» 를 잰다. 빨간 로그는 러너가 실패시킨다.
    /// </summary>
    public class DamageLayerTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(UiRoot.Instance, "UiRoot 가 서지 않았다");
            Assert.IsNotNull(PopupLayer.Instance, "팝업 층이 서지 않았다");
            yield return null;
        }

        /// <summary>앱 상자 바로 아래 조상의 형제 인덱스(같은 캔버스 · 클수록 위에 그린다).</summary>
        private static int TopIndexUnderApp(Transform t, Transform app)
        {
            Transform cur = t;
            while (cur != null && cur.parent != app) cur = cur.parent;
            Assert.IsNotNull(cur, t.name + " 은 앱 상자 아래에 없다");
            return cur.GetSiblingIndex();
        }

        [UnityTest]
        public IEnumerator 시트가_열려도_데미지_숫자는_팝업_층_아래에_남는다()
        {
            yield return Boot();
            UiRoot root = UiRoot.Instance;
            RectTransform app = root.App;

            var nums = new DamageNumbers();
            // 카메라가 보는 자리(three 좌표 · z 는 부호 반전 · 영웅 자리 근처)
            nums.Spawn(new Vector3(0.15f, 1.0f, 0f), "745", "dmg-hero", 0, -40, 1);
            Assert.AreEqual(1, nums.Count, "숫자가 하나 떠야 한다");

            RectTransform layer = DamageNumbers.Layer(root);
            Assert.IsNotNull(layer);
            Assert.AreEqual(DamageNumbers.LayerName, layer.name);
            Assert.AreSame(app, layer.parent, "fx-layer 는 앱 상자의 자식");
            Assert.AreEqual(0, layer.GetSiblingIndex(), "fx-layer 는 앱 상자의 첫 자식(원작 #game-area 가 #app 의 첫 자식)");
            Assert.AreEqual(1, layer.childCount, "숫자 글자는 fx-layer 안에만 선다");
            Transform number = layer.GetChild(0);
            Assert.IsTrue(number.gameObject.activeSelf);

            // HUD·시트 자리·채팅줄·탭바가 전부 그 위에 있다
            Assert.Less(layer.GetSiblingIndex(), root.HudLayer.GetSiblingIndex());
            Assert.Less(layer.GetSiblingIndex(), root.Sheet.GetSiblingIndex());
            Assert.Less(layer.GetSiblingIndex(), root.TabBand.GetSiblingIndex());

            TabBar tb = root.TabBar;
            tb.OnTab("shop");
            yield return null;
            PopupLayer popups = PopupLayer.Instance;
            Assert.IsTrue(popups.IsOpen(ShopSheet.Name), "상점 시트가 열려야 한다");
            Popup shop = popups.Find(ShopSheet.Name);
            Assert.IsNotNull(shop.Root);

            int numberTop = TopIndexUnderApp(number, app);
            int shopTop = TopIndexUnderApp(shop.Root, app);
            Assert.Less(numberTop, shopTop, "열린 시트가 데미지 숫자 위를 덮어야 한다(형제 순서)");

            // 시트가 열린 동안 새로 뜨는 숫자도 같은 층에 · 여전히 아래
            nums.Spawn(new Vector3(0.15f, 1.0f, 0f), "12", "dmg", 20, -40, 1);
            nums.Step(0.05f);
            yield return null;
            Assert.AreEqual(2, nums.Count);
            for (int i = 0; i < layer.childCount; i++)
                Assert.Less(TopIndexUnderApp(layer.GetChild(i), app), shopTop, "숫자 " + i + " 가 시트 위로 왔다");

            // 수명이 다하면 풀로 돌아가고(꺼진다) 층은 그대로 첫 자식
            nums.Step(10f);
            Assert.AreEqual(0, nums.Count);
            Assert.AreEqual(2, nums.Pooled);
            Assert.AreEqual(0, layer.GetSiblingIndex());

            tb.OnTab("shop");
            yield return null;
            nums.Clear();
        }
    }
}
