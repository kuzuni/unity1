using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using TMPro;
using Forge.Core.Ui;
using Forge.Game.Gallery;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T121 — 런타임 글꼴 애셋의 SDF 여백이 정본 최대 키라인(`.offline-total` 의 `.2em` = 바깥 .1em)을 담는가, 그리고 재질의 «1 알파 = 몇 텍셀»(`_GradientScale`)이
    /// 아틀라스의 실제 램프와 같은가. TMP 기본(90pt · 패딩 9 · G 10)은 36px 글자에서 최대 바깥 띠 3.6px = 요청 3.6px 이라 W = 1 → 링이 여백을 다 덮어 회색 «면» 이 됐고(T109 3회차 실측),
    /// 여백을 넓히자 TMP 가 박는 G(패딩+1)가 실제 램프(런 239 실측 19.9 텍셀)의 절반이라 링이 두 배였다(런 234·239). 그래서 `UiFont.Build` 가 램프를 아틀라스에서 재 G 로 세운다 —
    /// 이 자는 그 값을 다시 재 표(`alpha_texels` · 마지막 실측)와 대조하고 `ui-screens/t121-ramp.txt` 로 남긴다(screens 브랜치). 그림은 `screen_offline.png` 를 눈으로.
    /// </summary>
    public class FontBakeTests
    {
        const double OfflineTotalPx = 36;   // T121 실측 자리(정본 .offline-total · 36px 글자)
        const double MaxStrokeEm = 0.2;     // 정본 최대 -webkit-text-stroke-width

        [Test]
        public void 굽기_표가_정본_최대_키라인_2em_을_잘리지_않게_담고_재질_G_는_아틀라스_램프와_같다()
        {
            TMP_FontAsset fa = UiFont.Primary;
            Assert.IsNotNull(fa, "런타임 글꼴 애셋");
            Assert.Greater(UiFont.PaddingPx, 0, "표의 padding_px");
            Assert.AreEqual(UiFont.SamplingPt, fa.faceInfo.pointSize, "샘플링 크기가 표(sampling_pt)대로 구워졌다");

            StringBuilder log = new StringBuilder();
            log.Append("# T121 램프 진단 — UiFont.AlphaTexels ").Append(UiFont.AlphaTexels.ToString("0.0")).Append(UiFont.AlphaTexelsFromTable ? " (표 폴백!)" : " (실측)")
               .Append(" · 표 alpha_texels ").Append(UiFont.AlphaTexelsExpected).Append(" · 옛 갈래 배율 ").Append(UiFont.LegacyWidthScale.ToString("0.000")).Append('\n');
            double ramp = UiFont.MeasureAlphaTexels(fa, log);
            WriteDiag(log.ToString());
            Assert.IsFalse(UiFont.AlphaTexelsFromTable, "부팅 때 아틀라스 램프를 못 재 표로 이었다 — ui-screens/t121-ramp.txt");
            Assert.Greater(ramp, 0, "아틀라스 «I» 행에서 램프 기울기를 못 읽었다 — ui-screens/t121-ramp.txt");
            Assert.AreEqual(UiFont.AlphaTexels, ramp, 1e-6, "부팅 때 잰 램프와 지금 잰 램프가 같다");
            Assert.AreEqual(UiFont.AlphaTexelsExpected, ramp, UiFont.AlphaTexelsExpected * 0.15, "실측 램프가 표 alpha_texels 와 ±15% 안이어야 한다 — 굽기 값을 바꿨으면 표도 다시 재라(폴백 값이 틀리면 안 된다)");

            Material m = fa.material;
            Assert.IsNotNull(m, "애셋 재질");
            Assert.IsTrue(m.HasProperty("_GradientScale"), "SDF 재질(_GradientScale)");
            float g = m.GetFloat("_GradientScale");
            Assert.AreEqual(ramp, g, 0.01f, "_GradientScale = 아틀라스에서 잰 램프(TMP 기본 패딩+1 이 아니다)");
            float r = m.HasProperty("_ScaleRatioA") ? m.GetFloat("_ScaleRatioA") : 0f;
            if (r <= 0f) r = 1f;   // 재질 기본(비율 미계산)이면 1 — UiKit.OutlinePx 와 같은 읽기

            double unit = OutlineSdf.UnitPx(OfflineTotalPx, g, r, fa.faceInfo.pointSize);
            Assert.GreaterOrEqual(unit, 5.0, "36px 글자에서 낼 수 있는 최대 바깥 띠가 5px 이상이어야 한다(T121 판정) — 지금 " + unit.ToString("0.00"));

            OutlineSdf o = OutlineSdf.FromStroke(MaxStrokeEm * OfflineTotalPx, OfflineTotalPx, g, r, fa.faceInfo.pointSize);
            Assert.IsFalse(o.Clipped, "정본 .2em 획이 SDF 여백 안이어야 한다(Clipped 0)");
            Assert.Less(o.Width01, 0.85, "천장(W 1)에서 떨어져 있어야 외곽선이 면이 아니라 링이 된다 — W " + o.Width01.ToString("0.00"));
            Assert.AreEqual(MaxStrokeEm * OfflineTotalPx * 0.5, o.VisiblePx, 1e-6, "보이는 바깥 띠 = 획의 절반");

            // 옛 갈래(Outline(width01))는 TMP 기본 애셋에서 보이던 두께를 지킨다: 배율 = (샘플링/90) × (10/램프)
            Assert.AreEqual((UiFont.SamplingPt / UiFont.TmpDefaultSampling) * (UiFont.TmpDefaultRampTexels / ramp), UiFont.LegacyWidthScale, 1e-4, "옛 갈래 환산 배율");
        }

        private static void WriteDiag(string text)
        {
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
