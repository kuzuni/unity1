using UnityEngine;

namespace Forge.Game.Hero
{
    /// <summary>
    /// T6 의 «막대 한 자루» — 무기 8종 형상(정본 `makeWeapon` · 시대·등급 복셀)은 T15 가 잇는다. 여기서는 파지각·타이밍만 맞추려고 자루를 로컬 +y 로 세운 막대 하나다
    /// (정본 무기 메시 규약: 자루/검신 = 로컬 +y · 날 폭 = 로컬 x · 두께 = 로컬 z · 원점 = 파지점). 굵기는 정본 기본 자루 반경(`shaftR` 0.033)의 지름, 길이는 검 끝 y(≈0.78)·손잡이 0.10.
    /// </summary>
    public static class HeroWeapon
    {
        public const double ShaftR = 0.033;
        public const double TipY = 0.78, HiltY = -0.10;
        /// <summary>정본 heroAttack 의 무기 없음 색(0xcfd8dc).</summary>
        public const int StickColor = 0xcfd8dc;

        static Mesh stick;
        public static Mesh Stick()
        {
            if (stick != null) return stick;
            double len = TipY - HiltY;
            var m = HeroMeshes.Box(ShaftR * 2, len, ShaftR * 2, StickColor);
            // 원점(파지점)이 자루 아래 HiltY 에 오도록 정점을 올린다 — 공유 캐시 메시를 건드리지 않게 사본에
            var v = m.vertices;
            float dy = (float)(HiltY + len / 2);
            for (int i = 0; i < v.Length; i++) v[i].y += dy;
            stick = new Mesh { name = "HeroStick" };
            stick.SetVertices(v);
            stick.SetNormals(m.normals);
            stick.SetColors(m.colors);
            stick.SetTriangles(m.triangles, 0);
            stick.RecalculateBounds();
            return stick;
        }
    }
}
