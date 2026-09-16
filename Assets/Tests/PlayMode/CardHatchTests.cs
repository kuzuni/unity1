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

        /// <summary>
        /// T178 18회차 — «셀 면 통째 굽기»(정본 7730·8075: 해칭 위에 방사·선형 겹을 알파로 얹는 자리) 단언.
        /// 판은 Simple 한 장 · 전부 불투명 · 크기 = 면(캔버스 px ±2 · 해칭 주기가 안 늘어난다) · 위 행이 아래 행보다 밝다(명암 겹) · 좌상단이 우하단보다 밝다(광택 겹).
        /// <paramref name="hatched"/> 면 가운데 행에 «±4px 이웃 최댓값보다 6 이상 어두운» 홈 화소가 ≈30%(2px/12px 두 겹) 있고, 아니면(탈것 칸) 거의 없다.
        /// </summary>
        static void AssertBakedFace(Transform card, string what, bool hatched)
        {
            Transform face = card.Find("frame/face");
            Assert.IsNotNull(face, what + " — 면(frame/face)");
            Assert.IsNotNull(face.GetComponent<Mask>(), what + " — 둥근 면이 마스크한다");
            Assert.IsNull(face.Find("hatch"), what + " — 18회차부터 타일 해칭 대신 통째 판이다");
            Transform b = face.Find("face-bake");
            Assert.IsNotNull(b, what + " — 통째 판(face-bake)");
            Image img = b.GetComponent<Image>();
            Assert.AreEqual(Image.Type.Simple, img.type, what + " — 통째 판은 Simple");
            Assert.IsNotNull(img.sprite, what + " — 구운 판");
            Assert.IsFalse(img.raycastTarget, "겹은 클릭을 안 먹는다");
            Texture2D tex = img.sprite.texture;
            Rect fr = ((RectTransform)face).rect;
            Assert.AreEqual(fr.width, tex.width, 2f, what + " — 판 가로 = 면 가로(캔버스 px)");
            Assert.AreEqual(fr.height, tex.height, 2f, what + " — 판 세로 = 면 세로");
            int w = tex.width, h = tex.height;
            Color32[] px = tex.GetPixels32();
            int opaque = 0; foreach (Color32 c in px) if (c.a == 255) opaque++;
            Assert.AreEqual(px.Length, opaque, what + " — 미리 합성한 판은 전부 불투명");
            // 위 1/8 ↔ 아래 1/8 행 평균 밝기(텍스처 y 는 아래가 0)
            double top = 0, bot = 0; int band = Mathf.Max(1, h / 8);
            for (int y = 0; y < band; y++) for (int x = 0; x < w; x++) { bot += Lum(px[y * w + x]); top += Lum(px[(h - 1 - y) * w + x]); }
            top /= band * w; bot /= band * w;
            Assert.Greater(top, bot + 8, what + " — 위→아래 명암(정본 위 흰 .16~.22 · 아래 검정 .22~.24): 위 " + top.ToString("0.0") + " ↔ 아래 " + bot.ToString("0.0"));
            // 좌상단(26%,12%) 광택 ↔ 우하단
            double tl = Lum(px[(h - 1 - (int)(h * 0.12)) * w + (int)(w * 0.26)]), br = Lum(px[(int)(h * 0.12) * w + (int)(w * 0.80)]);
            Assert.Greater(tl, br, what + " — 좌상단 광택이 우하단보다 밝다");
            // 해칭 — 같은 기하(표 stripes.cell_hatch · 화소 가운데 · 텍스처 y 는 아래가 0)로 겹 수(0·1·2)를 다시 세어
            //   «겹이 하나 적은 바로 옆 화소» 와의 밝기 차가 잉크 한 번(sRGB 바이트 위 OverSrgb)인지 본다.
            double period = SurfaceArt.StripeNum("cell_hatch", "period_css_px", 0f) * SurfaceArt.CssPx, dash = SurfaceArt.StripeNum("cell_hatch", "dash_css_px", 0f) * SurfaceArt.CssPx;
            double ang = SurfaceArt.StripeNum("cell_hatch", "angle_deg", 45f), ia = SurfaceArt.StripeNum("cell_hatch", "ink_alpha", 0f);
            // 겹 수 지도 → 같은 행 ±5px 안의 «한 겹 적은» 이웃과의 밝기 차를 모은다(런 872 수리: 무리 «평균» 은 격자점이 서너 군데 격자에만 있어
            //   명암 겹의 위치 편향을 타고 2.7 어긋났다 — 이웃 차는 5px 안에서 매끈한 겹이 1~2 밖에 안 변해 편향이 없다).
            double sx = fr.width / w, sy = fr.height / h;
            int[] lv = new int[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    lv[y * w + x] = StripeRules.HatchLayers((x + 0.5) * sx, (h - 1 - y + 0.5) * sy, ang, period, dash);
            double[] sum = new double[3]; int[] cnt = new int[3];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int n = lv[y * w + x];
                    if (n == 0) continue;
                    for (int k = 1; k <= 5; k++)
                    {
                        int xl = x - k, xr = x + k, j = -1;
                        if (xl >= 0 && lv[y * w + xl] == n - 1) j = xl; else if (xr < w && lv[y * w + xr] == n - 1) j = xr;
                        if (j < 0) continue;
                        sum[n] += Lum(px[y * w + j]) - Lum(px[y * w + x]); cnt[n]++;
                        break;
                    }
                }
            if (hatched)
            {
                Assert.Greater(cnt[1], 0, what + " — 한 겹 자리 옆에 빈 자리가 있다"); Assert.Greater(cnt[2], 0, what + " — 격자점(두 겹) 옆에 한 겹 자리가 있다");
                Color32 f = face.GetComponent<Image>().color;
                Color32 once = new Color32(SurfaceBlendRules.OverSrgb(f.r, 0, ia), SurfaceBlendRules.OverSrgb(f.g, 0, ia), SurfaceBlendRules.OverSrgb(f.b, 0, ia), 255);
                Color32 twice = new Color32(SurfaceBlendRules.OverSrgb(once.r, 0, ia), SurfaceBlendRules.OverSrgb(once.g, 0, ia), SurfaceBlendRules.OverSrgb(once.b, 0, ia), 255);
                double d1 = Lum(f) - Lum(once), d2 = Lum(once) - Lum(twice);
                double m1 = sum[1] / cnt[1], m2 = sum[2] / cnt[2];
                Assert.AreEqual(d1, m1, System.Math.Max(2.0, 0.35 * d1), what + " — 한 겹 자리는 옆 빈 자리보다 잉크 한 번만큼 어둡다(실측 차 " + m1.ToString("0.0") + ")");
                Assert.AreEqual(d2, m2, System.Math.Max(2.0, 0.35 * d2), what + " — 격자점은 옆 한 겹 자리보다 또 한 번 어둡다(실측 차 " + m2.ToString("0.0") + ")");
            }
            else
            {
                // 탈것 칸엔 해칭이 없다 — 가운데 행이 매끈하다(이웃 화소 밝기 차 ≤ 2.5 · 겹 셋은 전부 그라디언트다)
                int y0 = h / 2; double worst = 0;
                for (int x = 1; x < w; x++) worst = System.Math.Max(worst, System.Math.Abs(Lum(px[y0 * w + x]) - Lum(px[y0 * w + x - 1])));
                Assert.LessOrEqual(worst, 2.5, what + " — 해칭 없이 매끈하다(이웃 차 최대 " + worst.ToString("0.00") + ")");
            }
        }

        static double Lum(Color32 c) { return (c.r * 299 + c.g * 587 + c.b * 114) / 1000.0; }

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

        /// <summary>
        /// T178 16회차 — 정본 828 `.equip-cell`: 장비 든 칸·빈 칸 둘 다 해칭이고, 탈것 칸(849 `.egg-cell` 은 제 겹 셋으로 덮는다)은 해칭이 없다.
        /// T178 18회차 — 7730 의 위 세 겹(광택·백플레이트·명암)까지 **면 통째 한 판**으로 굽고, 탈것 칸은 8075 의 세 겹(하늘색 선형 바탕·명암·광택)을 같은 길로 받는다.
        /// </summary>
        [UnityTest]
        public IEnumerator 장비_시트_칸은_든_것도_빈_것도_교차_해칭이고_탈것_칸은_아니다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeItem it = h.Engine.RollItem();
            h.Gear.Set(it.Slot, it);
            ForgeSheet.Render(h);
            yield return null;
            // T414 — `cell-<슬롯>`·`egg-cell` 은 화면 셋(`ForgeSheet`·`ForgeInfoPopup`·`DungeonClearPopup`)과
            //   둘(`ForgeSheet`·`PlayerInfoPopup`)이 쓰는 이름이다. 앱 뿌리부터 찾으면 남이 열어 둔 화면의 칸을 잰다 —
            //   `ForgeSheet.Render` 가 그리는 **그 시트 뿌리**에서만 찾는다.
            Transform app = UiRoot.Instance.Sheet;
            Transform full = FindDeep(app, "cell-" + it.Slot);
            Assert.IsNotNull(full, "장비 든 칸 cell-" + it.Slot);
            AssertBakedFace(full, "장비 든 칸", true);
            string emptySlot = null;
            foreach (string slot in h.Defs.Slots) if (h.Gear.Get(slot) == null) { emptySlot = slot; break; }
            Assert.IsNotNull(emptySlot, "빈 칸이 하나는 있다(새 세이브)");
            Transform empty = FindDeep(app, "cell-" + emptySlot);
            Assert.IsNotNull(empty, "빈 칸 cell-" + emptySlot);
            AssertBakedFace(empty, "빈 칸", true);
            Transform egg = FindDeep(app, "egg-cell");
            Assert.IsNotNull(egg, "탈것 칸");
            AssertBakedFace(egg, "탈것 칸(정본 8075 .egg-cell 은 제 겹 셋 · 해칭 없음)", false);
        }

        /// <summary>T178 18회차 — 표가 정본 7730·8075 의 여섯 겹(중심·반지름·각도·정지점)을 그대로 쥔다.</summary>
        [Test]
        public void 장비_칸_여섯_겹_표가_정본_7730_8075_를_그대로_쥔다()
        {
            float cx, cy, rx, ry; Color[] col; float[] pos;
            SurfaceArt.Ellipse("cell_gloss", out cx, out cy, out rx, out ry);
            Assert.AreEqual(0.26f, cx, 1e-4f); Assert.AreEqual(0.12f, cy, 1e-4f); Assert.AreEqual(1.18f, rx, 1e-4f); Assert.AreEqual(0.86f, ry, 1e-4f);
            SurfaceArt.Stops("cell_gloss", out col, out pos);
            Assert.AreEqual(0.30f, col[0].a, 1e-4f); Assert.AreEqual(0f, col[1].a, 1e-4f); Assert.AreEqual(0.56f, pos[1], 1e-4f);
            SurfaceArt.Ellipse("cell_plate", out cx, out cy, out rx, out ry);
            Assert.AreEqual(0.5f, cx, 1e-4f); Assert.AreEqual(0.46f, cy, 1e-4f); Assert.AreEqual(0.62f, rx, 1e-4f); Assert.AreEqual(0.54f, ry, 1e-4f);
            SurfaceArt.Stops("cell_plate", out col, out pos);
            Assert.AreEqual(0.11f, col[0].a, 1e-4f); Assert.AreEqual(0.72f, pos[1], 1e-4f);
            Assert.AreEqual(180f, SurfaceArt.Angle("cell_shade"), 1e-4f);
            SurfaceArt.Stops("cell_shade", out col, out pos);
            Assert.AreEqual(4, col.Length);
            Assert.AreEqual(0.16f, col[0].a, 1e-4f); Assert.AreEqual(0.32f, pos[1], 1e-4f); Assert.AreEqual(0.06f, col[2].a, 1e-4f); Assert.AreEqual(0.64f, pos[2], 1e-4f); Assert.AreEqual(0.22f, col[3].a, 1e-4f);
            SurfaceArt.Ellipse("egg_gloss", out cx, out cy, out rx, out ry);
            Assert.AreEqual(0.26f, cx, 1e-4f); Assert.AreEqual(1.18f, rx, 1e-4f);
            SurfaceArt.Stops("egg_gloss", out col, out pos);
            Assert.AreEqual(0.34f, col[0].a, 1e-4f);
            Assert.AreEqual(180f, SurfaceArt.Angle("egg_shade"), 1e-4f);
            SurfaceArt.Stops("egg_shade", out col, out pos);
            Assert.AreEqual(0.22f, col[0].a, 1e-4f); Assert.AreEqual(0.36f, pos[1], 1e-4f); Assert.AreEqual(0.08f, col[2].a, 1e-4f); Assert.AreEqual(0.66f, pos[2], 1e-4f); Assert.AreEqual(0.24f, col[3].a, 1e-4f);
            Assert.AreEqual(180f, SurfaceArt.Angle("egg_base"), 1e-4f);
            SurfaceArt.Stops("egg_base", out col, out pos);
            Assert.AreEqual(3, col.Length);
            Assert.AreEqual(new Color32(0x6c, 0xc6, 0xf7, 255), (Color32)col[0]); Assert.AreEqual(new Color32(0x4f, 0xb2, 0xee, 255), (Color32)col[1]); Assert.AreEqual(new Color32(0x3e, 0xa0, 0xe0, 255), (Color32)col[2]);
            Assert.AreEqual(0.55f, pos[1], 1e-4f); Assert.AreEqual(1f, pos[2], 1e-4f);
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
