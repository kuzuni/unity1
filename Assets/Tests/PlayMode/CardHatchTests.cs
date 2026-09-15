using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Forging;
using Forge.Core.Save;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T178 15회차 — 제작 묶음 카드(정본 1131 `.craft-batch .cb-card`)와 자동 폐기/결과 카드(1063 `.auto-drop-card`)의 면 위에
    /// `.equip-cell` 과 같은 45°/−45° 교차 해칭(rgba(0,0,0,.13) 2px / 12px)이 깔리는가. 굽은 타일은 면 색 위에 정본 길(sRGB 바이트)로 미리 합성돼
    /// 불투명하고, 덮이는 화소 ≈ 30.6%, 격자점은 두 번 어둡다. 눈 확인은 `screen_craft-batch`·`craft-reveal` 촬영이 있으면 그것으로.
    /// </summary>
    public class CardHatchTests
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

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { Transform r = FindDeep(root.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        static void AssertHatched(Transform card, string what)
        {
            Transform face = card.Find("frame/face");
            Assert.IsNotNull(face, what + " — 면(frame/face)");
            Assert.IsNotNull(face.GetComponent<Mask>(), what + " — 둥근 면이 마스크한다(정본 border-radius 가 background 를 자른다)");
            Transform h = face.Find("hatch");
            Assert.IsNotNull(h, what + " — 해칭 겹(hatch)");
            Image img = h.GetComponent<Image>();
            Assert.AreEqual(Image.Type.Tiled, img.type, what + " — 되풀이는 Tiled");
            Assert.IsNotNull(img.sprite, what + " — 구운 타일");
            Assert.IsFalse(img.raycastTarget, "겹은 클릭을 안 먹는다");
            Texture2D tex = img.sprite.texture;
            Color32[] px = tex.GetPixels32();
            Color32 f = face.GetComponent<Image>().color;
            byte fr = (byte)Mathf.RoundToInt(f.r), fg = (byte)Mathf.RoundToInt(f.g), fb = (byte)Mathf.RoundToInt(f.b);
            int dark = 0, gap = 0, opaque = 0; byte minR = 255;
            foreach (Color32 c in px)
            {
                if (c.a == 255) opaque++;
                if (c.r == fr && c.g == fg && c.b == fb) gap++; else dark++;
                if (c.r < minR) minR = c.r;
            }
            Assert.AreEqual(px.Length, opaque, what + " — 미리 합성한 타일은 전부 불투명");
            double frac = (double)dark / px.Length, want = 1 - System.Math.Pow(1 - 2.0 / 12.0, 2);
            Assert.AreEqual(want, frac, 0.06, what + " — 덮이는 화소 ≈ 30.6%(2px/12px 두 겹)");
            Assert.Greater(gap, 0, "빈 자리는 면 색 그대로");
            byte once = SurfaceBlendRules.OverSrgb(fr, 0, 0.13), twice = SurfaceBlendRules.OverSrgb(once, 0, 0.13);
            Assert.AreEqual(twice, minR, what + " — 격자점은 두 번 어둡다(정본이 위 겹을 아래 결과 위에 또 섞는다)");
        }

        [UnityTest]
        public IEnumerator 제작_묶음_카드_면에_교차_해칭이_깔린다()
        {
            yield return Boot();
            ForgeHost F = ForgeHost.Instance;
            var items = new List<ForgeItem> { F.Engine.RollItem(), F.Engine.RollItem() };
            bool done = false;
            ForgeCraftPopup.ShowBatch(F, items, () => { done = true; });
            yield return null; yield return null;
            Transform batch = UiRoot.Instance.App.Find("craft-batch");
            Assert.IsNotNull(batch, "묶음 겹(craft-batch)");
            AssertHatched(FindDeep(batch, "cb-card-0"), "묶음 카드");
            ForgeCraftPopup.DismissBatch();
            yield return null;
        }

        /// <summary>T178 16회차 — 정본 828 `.equip-cell`: 장비 든 칸·빈 칸 둘 다 해칭이고, 탈것 칸(849 `.egg-cell` 은 제 겹 셋으로 덮는다)은 해칭이 없다.</summary>
        [UnityTest]
        public IEnumerator 장비_시트_칸은_든_것도_빈_것도_교차_해칭이고_탈것_칸은_아니다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeItem it = h.Engine.RollItem();
            h.Gear.Set(it.Slot, it);
            ForgeSheet.Render(h);
            yield return null;
            Transform app = UiRoot.Instance.App;
            Transform full = FindDeep(app, "cell-" + it.Slot);
            Assert.IsNotNull(full, "장비 든 칸 cell-" + it.Slot);
            AssertHatched(full, "장비 든 칸");
            string emptySlot = null;
            foreach (string slot in h.Defs.Slots) if (h.Gear.Get(slot) == null) { emptySlot = slot; break; }
            Assert.IsNotNull(emptySlot, "빈 칸이 하나는 있다(새 세이브)");
            Transform empty = FindDeep(app, "cell-" + emptySlot);
            Assert.IsNotNull(empty, "빈 칸 cell-" + emptySlot);
            AssertHatched(empty, "빈 칸");
            Transform egg = FindDeep(app, "egg-cell");
            Assert.IsNotNull(egg, "탈것 칸");
            Transform eggFace = egg.Find("frame/face");
            Assert.IsNotNull(eggFace, "탈것 칸 면");
            Assert.IsNull(eggFace.Find("hatch"), "탈것 칸(정본 849 .egg-cell 은 제 겹 셋)엔 해칭이 없다");
        }

        [UnityTest]
        public IEnumerator 제작_결과_카드_면에_교차_해칭이_깔린다()
        {
            yield return Boot();
            ForgeHost F = ForgeHost.Instance;
            ForgeItem it = F.Engine.RollItem();
            ForgeCraftPopup.ShowReveal(F, it, () => { });
            yield return null; yield return null;
            Transform reveal = UiRoot.Instance.App.Find("craft-reveal");
            Assert.IsNotNull(reveal, "결과 겹(craft-reveal)");
            AssertHatched(FindDeep(reveal, "card"), "결과 카드");
            ForgeCraftPopup.DismissReveal();
            yield return null;
        }
    }
}
