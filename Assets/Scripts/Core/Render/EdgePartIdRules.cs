using System;
using System.Collections.Generic;

namespace Forge.Core.Render
{
    /// <summary>
    /// T330 1회차 — 캐릭터 윤곽선 넷째 항(파츠 ID)의 **번호·부호화·«ID 패스에 넣는가» 규칙**.
    /// 정본 `web/js/scene3d.js` 872~966(`ID_LAYER`·`ID_MIN_CSS_PX`·`idMatFor`·`renderIdPass` 의 `tag` 클로저) 그대로.
    /// UnityEngine 참조 0 — 렌더러·카메라에서 수를 뽑는 일은 Game `EdgePartId` 가 하고, 판정은 전부 여기서 한다.
    ///
    /// 정본이 이 규칙을 세운 까닭(줄 861~868): 오버라이드 재질 하나로는 파츠를 구분할 유니폼 자리가 없고,
    /// 모델 행렬로 해시하면 걷기마다 ID 가 달라져 선이 1px 씩 떨린다 → **파츠마다 생성 시각에 번호를 굳힌다**.
    /// </summary>
    public static class EdgePartIdRules
    {
        /// <summary>16bit 순환의 끝 — «동시에 살아 있는 파츠가 65535 개일 리 없다»(정본 878).</summary>
        public const int MaxId = 65535;
        /// <summary>배경 키 — ID 패스에 없는 것(지면·하늘)이 읽히는 값. 파츠 번호로는 쓰지 않는다.</summary>
        public const int Background = 0;
        /// <summary>카메라 코앞 판정 — 뷰공간 깊이가 이 이하면 «크다» 고 보고 투영을 건너뛴다(정본 959 · 나눗셈이 터진다).</summary>
        public const double NearZ = 0.05;

        /// <summary>다음 번호 — `((seq % 65535) + 1)` · 0 은 절대 안 나온다(정본 879).</summary>
        public static int Next(int prev)
        {
            if (prev < 0) throw new ArgumentOutOfRangeException("prev", "파츠 번호 열은 0 에서 시작한다");
            return (prev % MaxId) + 1;
        }

        /// <summary>번호 → ID 버퍼의 (r, g) = ((n &amp; 255)/255, ((n &gt;&gt; 8) &amp; 255)/255)(정본 882).</summary>
        public static void Encode(int n, out double r, out double g)
        {
            if (n < 1 || n > MaxId) throw new ArgumentOutOfRangeException("n", "파츠 번호는 1~65535 다(0 은 배경 키): " + n);
            r = (n & 255) / 255.0;
            g = ((n >> 8) & 255) / 255.0;
        }

        /// <summary>ID 버퍼 (r, g) → 번호. 8bit 텍스처에서 읽은 값이라 반올림한다.</summary>
        public static int Decode(double r, double g)
        {
            return (int)Math.Round(r * 255.0) + ((int)Math.Round(g * 255.0) << 8);
        }

        /// <summary>
        /// 이 파츠가 그 화소에 남기는 ID 버퍼 값 — rgb 는 번호, a 는 선형깊이 / `IdZFar`(정본 프래그먼트 889 `clamp(vZ/uZFar,0,1)`).
        /// 컴포짓의 `EdgeOutlineRules.IdKey` 가 이 a 로 «실제로 보이는 표면인가» 를 검증한다.
        /// </summary>
        public static EdgeIdSample Sample(EdgeOutlineSpec s, int n, double z)
        {
            double r, g;
            Encode(n, out r, out g);
            double a = z / s.IdZFar;
            if (a < 0) a = 0; else if (a > 1) a = 1;
            return new EdgeIdSample { R = r, G = g, A = a };
        }

        /// <summary>불투명 + 깊이를 쓰는 파츠만 ID 를 쓴다(정본 930) — 궤적·오라·플래시 같은 연출 메시는 뒤가 비쳐 유령선이 된다.</summary>
        public static bool OpaqueDepth(bool transparent, bool depthWrite) { return !transparent && depthWrite; }

        /// <summary>
        /// 파츠의 로컬 AABB 여덟 꼭짓점(뷰공간 · x 오른쪽 · y 위 · zv = 카메라 앞 거리)을 CSS px 로 투영해 **짧은 변**을 잰다(정본 950~965).
        /// 구(boundingSphere)가 아니라 AABB 의 짧은 변이어야 하는 이유는 정본 941~949 — 구는 납작한 판을 √2~√3 배 부풀려 흰자(4.2px)를 통과시켰다.
        /// 배율은 정본 그대로 `kx = cssH/(2·tanH·aspect)` · `ky = cssH/(2·tanH)`(957). 꼭짓점 하나라도 `zv ≤ NearZ` 면 `near` = true 로 끊고 0 을 돌려준다.
        /// </summary>
        public static double ShortSideCssPx(IList<double> vx, IList<double> vy, IList<double> vz, double cssH, double tanHalfFov, double aspect, out bool near)
        {
            if (vx == null || vy == null || vz == null || vx.Count != 8 || vy.Count != 8 || vz.Count != 8) throw new ArgumentException("꼭짓점 여덟이어야 한다");
            if (cssH <= 0 || tanHalfFov <= 0) throw new ArgumentOutOfRangeException("cssH", "CSS 높이·tan(fov/2) 는 양수다");
            if (aspect <= 0) aspect = 1;
            double kx = cssH / (2.0 * tanHalfFov * aspect), ky = cssH / (2.0 * tanHalfFov);
            double x0 = 1e9, x1 = -1e9, y0 = 1e9, y1 = -1e9;
            near = false;
            for (int b = 0; b < 8; b++)
            {
                double zv = vz[b];
                if (zv <= NearZ) { near = true; return 0; }
                double sx = vx[b] / zv * kx, sy = vy[b] / zv * ky;
                if (sx < x0) x0 = sx; if (sx > x1) x1 = sx;
                if (sy < y0) y0 = sy; if (sy > y1) y1 = sy;
            }
            return Math.Min(x1 - x0, y1 - y0);
        }

        /// <summary>
        /// 화면에서 작은 파츠는 ID 패스에 **넣지 않는다**(정본 963 · 문턱 `id_min_css_px` 6) — 눈 흰자(4.2)·탈것 옆구리 조각(4.0)은 빠지고 영웅 팔(≈11)은 남는다.
        /// 빠진 파츠의 화소엔 뒤의 큰 파츠 ID 가 그대로 남아 이웃과 같은 키가 된다(0 을 심으면 되레 선이 선다 · 정본 936).
        /// 카메라 코앞(`near`)은 크다고 본다.
        /// </summary>
        public static bool UseId(EdgeOutlineSpec s, double shortSideCssPx, bool near)
        {
            return near || shortSideCssPx >= s.IdMinCssPx;
        }
    }
}
