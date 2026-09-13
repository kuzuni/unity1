using NUnit.Framework;
using UnityEngine;
using TMPro;
using Forge.Core.Ui;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T121 — 런타임 글꼴 애셋의 SDF 패딩이 정본 최대 키라인(`.offline-total` 의 `.2em` = 바깥 .1em)을 담는가.
    /// TMP 기본(90pt · 패딩 9)은 36px 글자에서 최대 바깥 띠 3.60px = 요청 3.6px 과 같아 W = 1 → 링이 여백을 다 덮어 회색 «면» 이 됐다(T109 3회차 실측).
    /// 표(`Resources/UiFontBake.json`)대로 구웠는지 실제 애셋·재질 값으로 검산한다 — 그림은 다음 런 `screen_offline.png` 를 눈으로.
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
        }
    }
}
