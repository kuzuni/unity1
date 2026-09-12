using System;
using Forge.Core.World;

namespace Forge.Core.Pets
{
    /// <summary>
    /// 출전 대열 좌표 — 정본 `scene3d.js` `formationSpot(i, arc, row0)`(9627~9650) 그대로. i 번째 개체가 설 영웅 기준 상대 [x, z](three 좌표 · z 는 그대로 두고 Game 이 반전한다).
    /// row0 은 손으로 맞춘 고정 자리(`PET_ROW0`), 그 뒤는 arc 로 생성: `rear` 면 후방(−x) 격자 종대(펫 · 홀수 행은 z 반 칸 지그재그), 아니면 좌우 짝수 쌍의 호(탈것 무리 · T11).
    /// 표 값은 <see cref="SceneDefs"/>(scene.json) 이 쥔다 — 여기에 숫자는 없다.
    /// </summary>
    public static class PetFormation
    {
        /// <summary>정본 `formationSpot` — row0 이 있고 i 가 그 안이면 row0[i], 아니면 n = i − row0.length 번째 생성 자리.</summary>
        public static double[] Spot(int i, ArcSpec arc, double[][] row0)
        {
            if (row0 != null && i < row0.Length) return new[] { row0[i][0], row0[i][1] };
            int n = i - (row0 != null ? row0.Length : 0);
            if (arc.Rear)
            {
                // 후방(−x) 격자 종대 — cols 개 z 슬롯을 채우고 다음 행은 x 로 rxStep 더 물러난다
                int r = n / arc.Cols, k = n % arc.Cols;
                return new[] { -(arc.Rx0 + r * arc.RxStep), arc.Rz0 + k * arc.RzStep + (r % 2 != 0 ? arc.RzStep * 0.5 : 0) };
            }
            double span = arc.Hmax - arc.Hmin;
            for (int r = 0; r < 64; r++)
            {
                double rx = arc.Rx0 + r * arc.RxStep, rz = arc.Rz0 + r * arc.RzStep;
                // 이 행의 한쪽 정원 = 호 길이(span·rx) ÷ 개체 간격 · 전체 정원은 그 2배(좌우 대칭). JS Math.round = 반올림(.5 는 +∞ 쪽)
                int half = Math.Max(1, (int)Math.Floor(span * rx / arc.Gap + 0.5));
                if (n >= half * 2) { n -= half * 2; continue; }
                int side = n % 2 != 0 ? 1 : -1, k = (n - (n % 2)) / 2;
                double a = arc.Hmin + (k + 0.5) * (span / half);   // 칸 중앙
                double z = !arc.HasZBase ? rz * Math.Cos(a) : arc.ZBase - rz * Math.Cos(a);
                return new[] { side * rx * Math.Sin(a), z };
            }
            return new[] { 0.0, !arc.HasZBase ? arc.Rz0 : arc.ZBase - arc.Rz0 };   // 도달 불가(64행)
        }

        /// <summary>정본 `refreshPets` 의 대열 입력 — 탑승 중이면 row0 = mounted · 호는 rx0/rz0 을 mountedRx/mountedRz 만큼 민 것.</summary>
        public static double[] PetSpot(SceneDefs defs, int i, bool mounted)
        {
            ArcSpec arc = mounted ? defs.PetArc.ShiftedForMount() : defs.PetArc;
            return Spot(i, arc, mounted ? defs.PetRow0Mounted : defs.PetRow0Unmounted);
        }
    }
}
