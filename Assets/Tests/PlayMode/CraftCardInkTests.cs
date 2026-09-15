using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core;
using Forge.Core.Forging;
using Forge.Core.Pets;
using CoreRng = Forge.Core.Data.Rng;
using Forge.Game;
using Forge.Game.Ui;
using System.Collections.Generic;
using Forge.Core.Save;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T382 — 제작 묶음 카드(정본 `.craft-batch .cb-card` · ui.js 1972 `itemImgHTML(it, 'adc-img cell-img')`)의 잉크는 슬롯과 같은 fit-ink **THUMB_INK .76**(3145)다.
    /// 클론은 `CraftCard` 가 `.9` 를 박아 넘겼다 — 3D 썸네일이 있는 슬롯은 `ApplyThumb` 가 표 `img_frac` 로 다시 잡아 안 보였고, **썸네일 없는 슬롯(실루엣)** 에서만 18% 컸다.
    /// 그래서 두 갈래를 다 잰다: 실루엣 카드의 `img` 한 변 = 카드 × .76 · 썸네일 카드 = 카드 × `img_frac`.
    /// </summary>
    public class CraftCardInkTests
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

        [UnityTest]
        public IEnumerator 제작_묶음_카드의_실루엣_잉크는_슬롯과_같은_76_퍼센트다()
        {
            yield return Boot();
            ForgeHost F = ForgeHost.Instance;
            // 실루엣 갈래(썸네일 공장이 안 다루는 슬롯)와 썸네일 갈래를 하나씩은 넣는다
            var items = new List<ForgeItem>();
            string silSlot = null, thumbSlot = null;
            foreach (string slot in F.Defs.Slots)
            {
                if (!ItemFaces.Supports(slot) && silSlot == null) silSlot = slot;
                if (ItemFaces.Supports(slot) && thumbSlot == null) thumbSlot = slot;
            }
            Assert.IsNotNull(silSlot, "썸네일이 없는 슬롯이 하나는 있다(장신구 갈래)");
            ForgeItem a = F.Engine.RollItem(); a.Slot = silSlot; items.Add(a);
            if (thumbSlot != null) { ForgeItem b = F.Engine.RollItem(); b.Slot = thumbSlot; items.Add(b); }

            bool done = false;
            ForgeCraftPopup.ShowBatch(F, items, () => { done = true; });
            yield return null; yield return null;
            Transform batch = UiRoot.Instance.App.Find("craft-batch");
            Assert.IsNotNull(batch, "묶음 겹(craft-batch)이 떴다 — 대장간 화면이 보이고 팝업이 0 이어야 한다");

            int sil = 0, thumb = 0;
            for (int i = 0; i < items.Count; i++)
            {
                RectTransform card = FindDeep(batch, "cb-card-" + i) as RectTransform;
                Assert.IsNotNull(card, "cb-card-" + i);
                RectTransform img = card.Find("img") as RectTransform;
                Assert.IsNotNull(img, "카드 안 그림(img)");
                float size = card.sizeDelta.x;
                Assert.Greater(size, 0f, "카드 한 변");
                Assert.AreEqual(img.sizeDelta.x, img.sizeDelta.y, 0.5f, "그림은 정사각");
                bool hasThumb = ItemFaces.Get(F.Defs, items[i]) != null;
                if (hasThumb)
                {
                    thumb++;
                    Assert.AreEqual(ItemFacesStyle.L("img_frac"), img.sizeDelta.x / size, 0.005f, "썸네일 카드 — ApplyThumb 가 표 img_frac 로 잡는다");
                }
                else
                {
                    sil++;
                    Assert.AreEqual(0.76f, img.sizeDelta.x / size, 0.005f, "실루엣 카드 — 정본 THUMB_INK .76(슬롯과 같다) · 종전 .9");
                }
            }
            Assert.Greater(sil, 0, "실루엣 갈래를 하나는 쟀다");

            ForgeCraftPopup.DismissBatch();
            yield return null;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { Transform r = FindDeep(root.GetChild(i), name); if (r != null) return r; }
            return null;
        }
    }
}
