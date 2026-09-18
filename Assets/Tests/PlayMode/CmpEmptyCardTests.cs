using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Forging;
using Forge.Core.Pets;
using Forge.Core.Ui;
using CoreRng = Forge.Core.Data.Rng;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T472 — 제작 비교의 **빈 슬롯 카드**(정본 `style.css` 1830 `.cmp-card.empty { border-style: dashed; … }` · 1826 `.cmp-card` 엔 `background` 선언이 없다).
    /// 클론은 `PopupKit.Outlined`(실선 테 + 종이 면)였다 — 이제 면이 없고 둥근 점선 테 한 겹(`SurfaceArt.DashedFrame` · ol3 · 반지름 .8rem)만 선다.
    /// 화면 캡처(craft-compare)는 두 슬롯이 다 장착이라 빈 카드가 안 찍힌다 — 그래서 **구운 판의 화소**로 «끊긴다» 를 직접 잰다.
    /// </summary>
    public class CmpEmptyCardTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            yield return null;
        }

        private static Transform FindIn(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        [UnityTest]
        public IEnumerator 빈_슬롯_카드는_종이_면이_없고_둥근_점선_테_한_겹이_끊겨_돈다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeItem it = h.Engine.RollItem();
            it.Subs = SubstatRoll.Roll(h.Defs, CoreRng.Mulberry(43224), 2);
            h.Gear.Set(it.Slot, null);                       // 그 슬롯을 비운다 → 위 카드가 정본 `.cmp-card.empty` 갈래
            ForgeCraftPopup.Show(h, it);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "비교 팝업");
            Transform root = h.Meta.Popups.Find(ForgeCraftPopup.Name).Root;
            RectTransform cur = (RectTransform)FindIn(root, "cur");
            Assert.IsNotNull(cur, "장착 자리 카드(cur)");
            Assert.IsNotNull(cur.Find("empty"), "«빈 슬롯» 글");

            // ⓐ 면 없음 — 정본 `.cmp-card` 엔 background 가 없다(종이 면 `face` 가 서면 안 된다)
            Assert.IsNull(cur.Find("face"), "종이 면(face)이 없어야 한다(정본 1826 에 background 없음)");
            // ⓑ 점선 겹 하나
            Transform line = cur.Find("line");
            Assert.IsNotNull(line, "점선 테 겹(line)");
            Image img = line.GetComponent<Image>();
            Assert.IsNotNull(img, "테 겹 Image");
            Assert.IsNotNull(img.sprite, "구운 판");
            StringAssert.StartsWith("sf-dashed-", img.sprite.name, "SurfaceArt.BakeDashedFrame 판");

            // ⓒ 화소 — 위 변 띠 가운데 줄이 끊겼다 이어졌다 하고 · 대시 길이는 Core 셈과 같고 · 한가운데는 투명 · 굵기는 ol3
            Texture2D tex = img.sprite.texture;
            int w = tex.width, hh = tex.height;
            Color32[] px = tex.GetPixels32();
            float thick = PopupKit.Line3;
            int r = Mathf.RoundToInt(PopupKit.Rem * 0.8f);
            int rowY = hh - 1 - Mathf.RoundToInt(thick * 0.5f);          // 텍스처 y 는 아래가 0 — 위 변은 높은 y
            int flips = 0, run = 0, maxRun = 0; bool prev = false;
            for (int x = r; x < w - r; x++)
            {
                bool ink = px[rowY * w + x].a > 127;
                if (ink) { run++; if (run > maxRun) maxRun = run; } else run = 0;
                if (x > r && ink != prev) flips++;
                prev = ink;
            }
            Assert.GreaterOrEqual(flips, 4, "위 변 띠가 적어도 두 번 끊긴다(점선) — 실선이면 0");
            double dashFit;
            DashedFrameRules.FitPeriod(DashedFrameRules.Perimeter(w, hh, r), thick * CraftStyle.L("cmp_empty_dash_ratio"), thick * CraftStyle.L("cmp_empty_gap_ratio"), out dashFit);
            Assert.AreEqual(dashFit, maxRun, 2.0, "대시 한 토막 길이 = 둘레에 정수 개로 맞춘 주기 × 대시 비율(표 ×3)");
            Assert.AreEqual(0, px[(hh / 2) * w + w / 2].a, "한가운데는 투명(면 없음)");
            int inkX = -1;
            for (int x = r + 1; x < w - r; x++) if (px[rowY * w + x].a > 250) { inkX = x; break; }
            Assert.GreaterOrEqual(inkX, 0, "위 변에 꽉 찬 잉크 화소");
            int depth = 0;
            for (int y = hh - 1; y >= 0 && px[y * w + inkX].a > 127; y--) depth++;
            Assert.AreEqual(thick, depth, 1.01f, "띠 굵기 = ol3(line3_px)");

            ForgeCraftPopup.Hide(h);
        }
    }
}
