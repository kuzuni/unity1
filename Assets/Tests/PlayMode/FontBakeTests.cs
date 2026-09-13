using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TextCore;
using TMPro;
using Forge.Core.Ui;
using Forge.Game.Gallery;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T121 — 런타임 글꼴 애셋의 SDF 패딩이 정본 최대 키라인(`.offline-total` 의 `.2em` = 바깥 .1em)을 담는가.
    /// TMP 기본(90pt · 패딩 9)은 36px 글자에서 최대 바깥 띠 3.60px = 요청 3.6px 과 같아 W = 1 → 링이 여백을 다 덮어 회색 «면» 이 됐다(T109 3회차 실측).
    /// 표(`Resources/UiFontBake.json`)대로 구웠는지 실제 애셋·재질 값으로 검산한다 — 그림은 다음 런 `screen_offline.png` 를 눈으로.
    /// 2회차: 패딩을 15 로 키우니 런 234 `OutlineTests` 의 실측 띠가 식(G = 패딩 + 1)의 1.77배였다 — 그래서 아틀라스의 **실제 램프**(글리프 «I» 줄기를 가로지르는 텍셀 알파)를
    /// `ui-screens/t121-ramp.txt` 로 남긴다(진단 · 단언 아님 · screens 브랜치로 올라간다).
    /// </summary>
    public class FontBakeTests
    {
        const double OfflineTotalPx = 36;   // T121 실측 자리(정본 .offline-total · 36px 글자)
        const double MaxStrokeEm = 0.2;     // 정본 최대 -webkit-text-stroke-width

        [Test]
        public void 굽기_표가_정본_최대_키라인_2em_을_잘리지_않게_담는다()
        {
            TMP_FontAsset fa = UiFont.Primary;
            Assert.IsNotNull(fa, "런타임 글꼴 애셋");
            Assert.Greater(UiFont.PaddingPx, 0, "표의 padding_px");
            Assert.AreEqual(UiFont.SamplingPt, fa.faceInfo.pointSize, "샘플링 크기가 표(sampling_pt)대로 구워졌다");

            Material m = fa.material;
            Assert.IsNotNull(m, "애셋 재질");
            Assert.IsTrue(m.HasProperty("_GradientScale"), "SDF 재질(_GradientScale)");
            float g = m.GetFloat("_GradientScale");
            Assert.AreEqual(UiFont.PaddingPx + 1, g, 0.01f, "_GradientScale = 패딩 + 1 (TMP 굽기 규칙)");
            float r = m.HasProperty("_ScaleRatioA") ? m.GetFloat("_ScaleRatioA") : 0f;
            if (r <= 0f) r = 1f;   // 재질 기본(비율 미계산)이면 1 — UiKit.OutlinePx 와 같은 읽기

            double unit = OutlineSdf.UnitPx(OfflineTotalPx, g, r, fa.faceInfo.pointSize);
            Assert.GreaterOrEqual(unit, 5.0, "36px 글자에서 낼 수 있는 최대 바깥 띠가 5px 이상이어야 한다(T121 판정) — 지금 " + unit.ToString("0.00"));

            OutlineSdf o = OutlineSdf.FromStroke(MaxStrokeEm * OfflineTotalPx, OfflineTotalPx, g, r, fa.faceInfo.pointSize);
            Assert.IsFalse(o.Clipped, "정본 .2em 획이 SDF 여백 안이어야 한다(Clipped 0)");
            Assert.Less(o.Width01, 0.85, "천장(W 1)에서 떨어져 있어야 외곽선이 면이 아니라 링이 된다 — W " + o.Width01.ToString("0.00"));
            Assert.AreEqual(MaxStrokeEm * OfflineTotalPx * 0.5, o.VisiblePx, 1e-6, "보이는 바깥 띠 = 획의 절반");

            WriteRampDiag(fa, g, r);
        }

        /// <summary>진단(실패 없음): 글리프 «I» 의 줄기 한가운데 행을 아틀라스에서 읽어 알파 램프(0→255 에 몇 텍셀 드는가)를 남긴다 — 식의 «1 알파 = G 텍셀» 이 맞는지 다음 사람이 본다.</summary>
        private static void WriteRampDiag(TMP_FontAsset fa, float g, float r)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                sb.Append("# T121 램프 진단 — 애셋 ").Append(fa.name).Append(" · 샘플링 ").Append(fa.faceInfo.pointSize).Append("pt · atlasPadding ").Append(fa.atlasPadding)
                  .Append(" · renderMode ").Append(fa.atlasRenderMode).Append(" · 아틀라스 ").Append(fa.atlasWidth).Append('×').Append(fa.atlasHeight)
                  .Append(" · 장 수 ").Append(fa.atlasTextures != null ? fa.atlasTextures.Length : 0).Append(" · 재질 G ").Append(g).Append(" R ").Append(r).Append('\n');
                fa.TryAddCharacters("I");
                TMP_Character ch;
                if (fa.characterLookupTable == null || !fa.characterLookupTable.TryGetValue('I', out ch) || ch.glyph == null)
                {
                    sb.Append("«I» 글리프를 못 찾았다\n");
                }
                else
                {
                    GlyphRect gr = ch.glyph.glyphRect;
                    int ai = ch.glyph.atlasIndex;
                    Texture2D tex = fa.atlasTextures != null && ai >= 0 && ai < fa.atlasTextures.Length ? fa.atlasTextures[ai] : null;
                    sb.Append("«I» glyphRect x ").Append(gr.x).Append(" y ").Append(gr.y).Append(" w ").Append(gr.width).Append(" h ").Append(gr.height).Append(" · atlasIndex ").Append(ai).Append('\n');
                    if (tex == null) sb.Append("아틀라스 텍스처가 없다\n");
                    else
                    {
                        Color32[] px = tex.GetPixels32();
                        int tw = tex.width;
                        int y = gr.y + gr.height / 2;
                        int pad = Mathf.Max(fa.atlasPadding, 0) + 3;
                        int x0 = Mathf.Max(0, gr.x - pad), x1 = Mathf.Min(tw - 1, gr.x + gr.width + pad);
                        sb.Append("행 y=").Append(y).Append(" x ").Append(x0).Append('~').Append(x1).Append(" 알파: ");
                        List<int> row = new List<int>();
                        for (int x = x0; x <= x1; x++) { int a = px[y * tw + x].a; row.Add(a); sb.Append(a).Append(' '); }
                        sb.Append('\n');
                        // 왼쪽 가장자리의 램프: 처음 a>8 부터 처음 a>=247 까지의 텍셀 수(경계 양끝 포함 대략) — «1 알파 = 몇 텍셀» 의 실측
                        int first = -1, full = -1;
                        for (int i = 0; i < row.Count; i++) { if (first < 0 && row[i] > 8) first = i; if (first >= 0 && row[i] >= 247) { full = i; break; } }
                        int half = -1;
                        for (int i = 0; i < row.Count; i++) if (row[i] >= 128) { half = i; break; }
                        sb.Append("왼쪽 램프(>8 → ≥247) 텍셀 ").Append(first >= 0 && full >= 0 ? (full - first).ToString() : "?")
                          .Append(" · 바깥쪽(>8 → ≥128) ").Append(first >= 0 && half >= 0 ? (half - first).ToString() : "?")
                          .Append(" · 식의 가정 1 알파 = G 텍셀 = ").Append(g).Append('\n');
                    }
                }
            }
            catch (System.Exception e) { sb.Append("진단 실패: ").Append(e.GetType().Name).Append(' ').Append(e.Message).Append('\n'); }
            string text = sb.ToString();
            Debug.Log("[T121] " + text);
            try
            {
                string dir = Path.Combine(Directory.GetCurrentDirectory(), GallerySheet.OutDir);
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "t121-ramp.txt"), text, new UTF8Encoding(false));
            }
            catch (System.Exception e) { Debug.Log("[T121] 진단 파일을 못 썼다: " + e.Message); }
        }
    }
}
