using UnityEngine;
using Forge.Core.CraftFx;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T87 28회차 — 결과 카드가 쓰는 «구운 그림» 하나: 광택 띠(`crsheen` 의 `linear-gradient(105deg, …)`).
    /// 굽는 길은 T87 6회차가 깔아 둔 <see cref="CraftFxPoly"/> 그대로다(짝홀 규칙 + 3×3 초과표본 · 그라디언트 동봉).
    /// 색·각도·정지 위치는 전부 Core <see cref="CraftCardSpec"/> 이 쥔다 — 여기는 굽기 해상도만 정한다.
    /// </summary>
    public static class CraftCardArt
    {
        /// <summary>굽기용 정사각형의 한 변(viewBox 단위) — 화면 크기가 아니라 **텍스처 해상도**를 정하는 값이라 게임 수치가 아니다.</summary>
        private const float BakeUnits = 6f;

        private const string SheenName = "cr-sheen";

        /// <summary>
        /// 광택 띠 스프라이트 — 흰색이 가운데(<see cref="CraftCardSpec.SheenPeakAlpha"/>)에서 가장 진하고 양 끝은 투명하다.
        /// 카드에 늘려 붙이므로 정사각으로 굽는다(정본 `::after { inset: 0 }`).
        /// </summary>
        public static Sprite Sheen()
        {
            float u = BakeUnits;
            Vector2[] pts = { new Vector2(0, 0), new Vector2(u, 0), new Vector2(u, u), new Vector2(0, u) };
            double fx, fy, tx, ty;
            CraftCardSpec.SheenAxis(out fx, out fy, out tx, out ty);
            Color clear = new Color(1f, 1f, 1f, 0f);
            Color peak = new Color(1f, 1f, 1f, (float)CraftCardSpec.SheenPeakAlpha);
            Color[] stops = { clear, peak, clear };
            float[] offsets = new float[CraftCardSpec.SheenStops.Length];
            for (int i = 0; i < offsets.Length; i++) offsets[i] = (float)CraftCardSpec.SheenStops[i];
            return CraftFxPoly.Bake(SheenName, pts, stops, offsets, new Vector2((float)fx, (float)fy), new Vector2((float)tx, (float)ty));
        }
    }
}
