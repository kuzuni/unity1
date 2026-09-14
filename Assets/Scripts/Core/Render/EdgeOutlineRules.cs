using System;
using Forge.Core.Data;

namespace Forge.Core.Render
{
    /// <summary>
    /// 캐릭터 윤곽선(후처리 깊이-엣지 아웃라인)의 **수치표** — `Resources/EdgeOutlineUi.json`.
    /// 정본 `web/js/scene3d.js` `initPost()` 컴포짓 재질의 유니폼 그대로다. UnityEngine 참조 0.
    /// </summary>
    public sealed class EdgeOutlineSpec
    {
        /// <summary>상대 깊이 임계 — 이웃이 나보다 `EdgeK × 내 깊이` 이상 멀면 나를 칠한다.</summary>
        public double EdgeK;
        /// <summary>법선 불연속 임계 — 복원 법선의 `1 - dot`.</summary>
        public double NormalK;
        /// <summary>곡률 임계 — 역깊이(1/z) 2차 차분.</summary>
        public double CreaseK;
        /// <summary>곡률 이력 비율 — 이웃에 강한 곡률선이 있을 때 쓰는 완화 임계 배수.</summary>
        public double CreaseHystF;
        /// <summary>지평선 컷 — 이 선형깊이를 넘는 화소엔 선을 안 그린다. 🚨 두께 판정엔 절대 안 쓴다.</summary>
        public double EdgeMaxZ;
        /// <summary>파츠 ID 버퍼 a 채널의 깊이 스케일.</summary>
        public double IdZFar;
        /// <summary>ID 화소가 «보이는 표면» 인지 보는 깊이 허용오차.</summary>
        public double IdTolZ;
        /// <summary>화면 지름이 이 CSS px 미만인 파츠는 ID 를 안 쓴다.</summary>
        public double IdMinCssPx;
        /// <summary>버퍼/CSS 비가 이 값 이상일 때만 팽창을 건다.</summary>
        public double DilateMinBufF;
        /// <summary>정본 앱 상자 폭(CSS px) — 화면 가로 픽셀을 이것으로 나눈 것이 «CSS 화소당 버퍼 화소»(정본 devicePixelRatio 자리).</summary>
        public double CssAppWPx;
        /// <summary>대각 탭 거리 환산(1/√2) · 반경 2 탭 거리 환산(1/2).</summary>
        public double DiagF, R2F;
        /// <summary>ID 키 = `r × IdLoF + g × IdHiF`.</summary>
        public double IdLoF, IdHiF;
        /// <summary>선 색(정본 검정).</summary>
        public string LineHex;

        public static EdgeOutlineSpec From(JsonObject root)
        {
            var T = J.Obj(J.Require(root, "thresholds"));
            var P = J.Obj(J.Require(root, "taps"));
            var C = J.Obj(J.Require(root, "colors"));
            Func<string, double> t = k => J.Num(J.Require(T, k));
            Func<string, double> p = k => J.Num(J.Require(P, k));
            var s = new EdgeOutlineSpec
            {
                EdgeK = t("edge_k"), NormalK = t("normal_k"), CreaseK = t("crease_k"), CreaseHystF = t("crease_hyst_f"),
                EdgeMaxZ = t("edge_max_z"), IdZFar = t("id_z_far"), IdTolZ = t("id_tol_z"), IdMinCssPx = t("id_min_css_px"),
                DilateMinBufF = t("dilate_min_buf_f"), CssAppWPx = t("css_app_w_px"),
                DiagF = p("diag_f"), R2F = p("r2_f"), IdLoF = p("id_lo_f"), IdHiF = p("id_hi_f"),
                LineHex = J.Str(J.Require(C, "line")),
            };
            if (s.EdgeK <= 0 || s.CreaseK <= 0 || s.EdgeMaxZ <= 0 || s.IdZFar <= 0 || s.CssAppWPx <= 0)
                throw new FormatException("EdgeOutlineUi 임계는 모두 양수여야 한다");
            return s;
        }
    }

    /// <summary>
    /// 반경 2 원반 12탭 + 중심 = 셰이더가 쓰는 **선형깊이(뷰공간 유닛)** 열셋.
    /// 이름은 정본 그대로: `l`=왼 · `r`=오른 · `d`=아래 · `u`=위 · `2x`=반경 2 · `dl`~`ur`=대각.
    /// </summary>
    public struct EdgeDepthTaps
    {
        public double Z0, Zl, Zr, Zd, Zu, Z2l, Z2r, Z2d, Z2u, Zdl, Zdr, Zul, Zur;
    }

    /// <summary>파츠 ID 버퍼 한 화소 — `rgb` = 16bit 파츠 번호, `a` = 그 화소의 선형깊이 / `IdZFar`.</summary>
    public struct EdgeIdSample { public double R, G, A; }

    /// <summary>ID 항이 보는 다섯 화소(중심·상하좌우)와 «이 프레임에 ID 버퍼가 채워졌는가».</summary>
    public struct EdgeIdTaps
    {
        public EdgeIdSample J0, Jl, Jr, Jd, Ju;
        public bool Has;
    }

    /// <summary>
    /// 깊이→뷰공간 복원에 필요한 화면 기하 — `Ux,Uy` = 이 화소의 시야평면 좌표 `(vUv*2-1)*proj`,
    /// `Dux,Duy` = 화소 하나가 차지하는 `(u,v)` 폭 `2*texel*proj`.
    /// </summary>
    public struct EdgeGeom { public double Ux, Uy, Dux, Duy; }

    /// <summary>
    /// 🚨 **판정기의 «off 프레임» 은 네 항을 다 꺼야 한다**(정본 scene3d.js 693 경고 · 저장소가 두 번 밟은 함정).
    /// 하나라도 켜 두면 그 선이 on/off 양쪽에 똑같이 찍혀 차분 마스크에서 통째로 지워진다.
    /// 그래서 끄는 자리를 <see cref="Off"/> 하나로 묶어 둔다 — 항을 따로 끄는 길을 열지 않는다.
    /// </summary>
    public struct EdgeOutlineTerms
    {
        public bool Silhouette, Normal, Crease, Id;
        public static EdgeOutlineTerms All { get { return new EdgeOutlineTerms { Silhouette = true, Normal = true, Crease = true, Id = true }; } }
        public static EdgeOutlineTerms Off { get { return new EdgeOutlineTerms(); } }
        public bool AnyOn { get { return Silhouette || Normal || Crease || Id; } }
    }

    /// <summary>
    /// 정본 `scene3d.js initPost()` 컴포짓 프래그먼트의 아웃라인 넷(실루엣 · 법선 · 곡률 · 파츠 ID)의 **셈**.
    /// 셰이더가 할 일을 그대로 옮긴 순수 함수라 EditMode 에서 화면 없이 잴 수 있다(화면·URP 패스는 2회차).
    /// </summary>
    public static class EdgeOutlineRules
    {
        /// <summary>GLSL `step(edge, x)` — `x >= edge` 면 1.</summary>
        public static double Step(double edge, double x) { return x >= edge ? 1.0 : 0.0; }
        static double Max(double a, double b) { return a > b ? a : b; }
        static double Max4(double a, double b, double c, double d) { return Max(Max(a, b), Max(c, d)); }

        /// <summary>
        /// 팽창을 거는가 — 버퍼/CSS 비가 `dilate_min_buf_f`(2) 이상일 때만.
        /// DPR 1 에서 켜면 선이 2 CSS px 이 되어 얇은 파츠를 통째로 먹는다(정본 실측: 펫 몸통 검정 52.0%).
        /// </summary>
        public static bool DilateOn(EdgeOutlineSpec s, double bufScale) { return bufScale >= s.DilateMinBufF; }

        /// <summary>
        /// 화면 가로 픽셀 → «CSS 화소당 버퍼 화소». 정본은 브라우저 `devicePixelRatio`(1 또는 2 로 스냅)로 재는데,
        /// 클론엔 CSS 가 없으므로 **정본 앱 상자 폭**(499 CSS px)을 자로 삼는다 — 같은 뜻의 수가 된다.
        /// </summary>
        public static double BufScale(EdgeOutlineSpec s, double screenWidthPx) { return screenWidthPx / s.CssAppWPx; }

        /// <summary>지평선 컷 — `step(z0, edgeMaxZ)`. 🚨 두께 판정엔 안 쓴다(하늘 경계만 1px 이 되는 함정).</summary>
        public static bool NearCut(EdgeOutlineSpec s, double z0) { return z0 <= s.EdgeMaxZ; }

        /// <summary>이웃이 나와 같은 면인가(계단이 아닌가) — 팽창·ID 를 받는 방향별 가드 `cN`.</summary>
        public static double Continuous(EdgeOutlineSpec s, double z0, double zn) { return 1.0 - Step(s.EdgeK * z0, Math.Abs(zn - z0)); }

        /// <summary>
        /// ① 실루엣 — **비대칭**: 반경 1 이웃 중 나보다 유의하게 **먼** 화소가 있으면 나를 칠한다.
        /// 검정은 항상 **가까운 쪽**에만 깔리므로 하늘 경계든 겹침이든 두께가 같다.
        /// </summary>
        public static double Detect(EdgeOutlineSpec s, double z, double a, double b, double c, double d)
        {
            return Step(s.EdgeK * z, Max4(a, b, c, d) - z);
        }

        /// <summary>
        /// ① 실루엣 = **반경 1 검출 + 상하좌우 한 칸 팽창**(팽창은 깊이가 이어진 이웃에서만).
        /// 반경 2 탭을 곧장 임계와 재면 두께가 계단 크기를 따라가고, 팽창을 무조건 걸면 계단을 건너뛰어 3px 이 된다.
        /// </summary>
        public static double Silhouette(EdgeOutlineSpec s, EdgeDepthTaps t, bool dilate)
        {
            double e0 = Detect(s, t.Z0, t.Zl, t.Zr, t.Zd, t.Zu);
            if (!dilate) return e0;
            double eR = Detect(s, t.Zr, t.Z0, t.Z2r, t.Zur, t.Zdr);
            double eL = Detect(s, t.Zl, t.Z0, t.Z2l, t.Zul, t.Zdl);
            double eU = Detect(s, t.Zu, t.Z0, t.Z2u, t.Zul, t.Zur);
            double eD = Detect(s, t.Zd, t.Z0, t.Z2d, t.Zdl, t.Zdr);
            double g = Max4(eL * Continuous(s, t.Z0, t.Zl), eR * Continuous(s, t.Z0, t.Zr),
                            eU * Continuous(s, t.Z0, t.Zu), eD * Continuous(s, t.Z0, t.Zd));
            return Max(e0, g);
        }

        /// <summary>큰 계단 억제용 — 이웃 12탭의 깊이차를 **탭거리로 나눠** 화소당 기울기로 환산한 최댓값.</summary>
        public static double AMax(EdgeOutlineSpec s, EdgeDepthTaps t)
        {
            double z0 = t.Z0;
            double a1 = Max4(Math.Abs(t.Zl - z0), Math.Abs(t.Zr - z0), Math.Abs(t.Zd - z0), Math.Abs(t.Zu - z0));
            double ad = Max4(Math.Abs(t.Zdl - z0), Math.Abs(t.Zdr - z0), Math.Abs(t.Zul - z0), Math.Abs(t.Zur - z0)) * s.DiagF;
            double a2 = Max4(Math.Abs(t.Z2l - z0), Math.Abs(t.Z2r - z0), Math.Abs(t.Z2d - z0), Math.Abs(t.Z2u - z0)) * s.R2F;
            return Max(a1, Max(ad, a2));
        }

        /// <summary>큰 계단 반경 2 안인가 — 여기서는 ②③ 을 끈다(안 끄면 계단 먼 쪽까지 칠해 3px 이 된다).</summary>
        public static double StepGuard(EdgeOutlineSpec s, EdgeDepthTaps t) { return 1.0 - Step(s.EdgeK * t.Z0, AMax(s, t)); }

        /// <summary>
        /// 깊이만으로 뷰공간 법선 복원 — 평면 위에서 역깊이 `q=1/z` 가 시야평면 좌표의 1차식인 성질을 쓴다.
        /// `n ∝ (q_u, q_v, -(q - u·q_u - v·q_v))`.
        /// </summary>
        public static void Normal(double q, double gu, double gv, double ux, double uy, out double nx, out double ny, out double nz)
        {
            nx = gu; ny = gv; nz = -(q - ux * gu - uy * gv);
            double len = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (len <= 0) { nx = 0; ny = 0; nz = -1; return; }
            nx /= len; ny /= len; nz /= len;
        }

        /// <summary>② 법선 불연속의 재료 — 중심과 상하좌우 법선의 `dot` 최솟값. 추가 탭은 0개다.</summary>
        public static double NormalDotMin(EdgeDepthTaps t, EdgeGeom g)
        {
            double qc = 1.0 / t.Z0, qL = 1.0 / t.Zl, qR = 1.0 / t.Zr, qD = 1.0 / t.Zd, qU = 1.0 / t.Zu;
            double q2L = 1.0 / t.Z2l, q2R = 1.0 / t.Z2r, q2D = 1.0 / t.Z2d, q2U = 1.0 / t.Z2u;
            double qDL = 1.0 / t.Zdl, qDR = 1.0 / t.Zdr, qUL = 1.0 / t.Zul, qUR = 1.0 / t.Zur;
            double du = 2.0 * g.Dux, dv = 2.0 * g.Duy;
            double n0x, n0y, n0z, nx, ny, nz, dmin = 1.0;
            Normal(qc, (qR - qL) / du, (qU - qD) / dv, g.Ux, g.Uy, out n0x, out n0y, out n0z);
            Normal(qR, (q2R - qc) / du, (qUR - qDR) / dv, g.Ux + g.Dux, g.Uy, out nx, out ny, out nz);
            dmin = Math.Min(dmin, n0x * nx + n0y * ny + n0z * nz);
            Normal(qL, (qc - q2L) / du, (qUL - qDL) / dv, g.Ux - g.Dux, g.Uy, out nx, out ny, out nz);
            dmin = Math.Min(dmin, n0x * nx + n0y * ny + n0z * nz);
            Normal(qU, (qUR - qUL) / du, (q2U - qc) / dv, g.Ux, g.Uy + g.Duy, out nx, out ny, out nz);
            dmin = Math.Min(dmin, n0x * nx + n0y * ny + n0z * nz);
            Normal(qD, (qDR - qDL) / du, (qc - q2D) / dv, g.Ux, g.Uy - g.Duy, out nx, out ny, out nz);
            dmin = Math.Min(dmin, n0x * nx + n0y * ny + n0z * nz);
            return dmin;
        }

        /// <summary>③ 곡률 한 화소 — 역깊이 2차 차분이 `CreaseK × q` 를 넘는가(`k` 배수로 이력 완화).</summary>
        public static double Curvature(EdgeOutlineSpec s, double q, double qa, double qb, double qc2, double qd, double mul)
        {
            double cx = Math.Abs(qa + qb - 2.0 * q), cy = Math.Abs(qc2 + qd - 2.0 * q);
            return Step(s.CreaseK * q * mul, Max(cx, cy));
        }

        /// <summary>
        /// ②③ 합 — 곡률선을 **기준선**으로 두고 법선항은 반경 1 안에 곡률선이 없는 자리에서만 그린다(비접촉 게이트).
        /// 곡률은 이웃에 강한 곡률선이 있으면 `CreaseHystF` 배 임계만 넘어도 통과시켜 반대편 날개를 살린다.
        /// 둘 다 큰 계단 반경 2 안에서는 꺼진다.
        /// </summary>
        public static double Crease(EdgeOutlineSpec s, EdgeDepthTaps t, EdgeGeom g, bool dilate, EdgeOutlineTerms on)
        {
            double qc = 1.0 / t.Z0, qL = 1.0 / t.Zl, qR = 1.0 / t.Zr, qD = 1.0 / t.Zd, qU = 1.0 / t.Zu;
            double q2L = 1.0 / t.Z2l, q2R = 1.0 / t.Z2r, q2D = 1.0 / t.Z2d, q2U = 1.0 / t.Z2u;
            double qDL = 1.0 / t.Zdl, qDR = 1.0 / t.Zdr, qUL = 1.0 / t.Zul, qUR = 1.0 / t.Zur;
            double crv0 = on.Crease ? Curvature(s, qc, qL, qR, qD, qU, 1.0) : 0.0;
            double crvL = on.Crease ? Curvature(s, qL, q2L, qc, qDL, qUL, 1.0) : 0.0;
            double crvR = on.Crease ? Curvature(s, qR, qc, q2R, qDR, qUR, 1.0) : 0.0;
            double crvU = on.Crease ? Curvature(s, qU, qUL, qUR, qc, q2U, 1.0) : 0.0;
            double crvD = on.Crease ? Curvature(s, qD, qDL, qDR, q2D, qc, 1.0) : 0.0;
            double crvNbr = Max4(crvL, crvR, crvU, crvD);
            double crvNear = Max(crv0, crvNbr);
            double hyst = on.Crease && dilate
                ? Curvature(s, qc, qL, qR, qD, qU, s.CreaseHystF) * crvNbr
                : 0.0;
            double crvHy = Max(crv0, hyst);
            double nrm = on.Normal ? Step(s.NormalK, 1.0 - NormalDotMin(t, g)) * (1.0 - crvNear) : 0.0;
            return Max(crvHy, nrm) * StepGuard(s, t);
        }

        /// <summary>
        /// ID 화소 → 정수 키. 앞의 `step` 이 **가시성 검증**이다 — 가려진 액터 화소는 키 0(=배경)으로 떨어진다.
        /// </summary>
        public static double IdKey(EdgeOutlineSpec s, EdgeIdSample j, double z)
        {
            return Step(Math.Abs(j.A * s.IdZFar - z), s.IdTolZ) * (j.R * s.IdLoF + j.G * s.IdHiF);
        }

        /// <summary>
        /// ④ 파츠 ID 불연속 — 깊이도 법선도 0 인 «같은 평면 파츠 경계»(팔↔몸통 · 발굽↔지면) 전용.
        /// 두께 규율은 실루엣과 같다: 팽창 off 면 **키가 큰 쪽만**(1 버퍼px), on 이면 **양쪽**(2 버퍼px).
        /// 🚨 깊이가 이어진 이웃(`cN`)에만 건다 — 계단 너머까지 보면 먼 쪽 개체에 두 번째 선이 붙는다.
        /// </summary>
        public static double IdLine(EdgeOutlineSpec s, EdgeDepthTaps t, EdgeIdTaps id, bool dilate, EdgeOutlineTerms on)
        {
            if (!on.Id || !id.Has) return 0.0;
            double k0 = IdKey(s, id.J0, t.Z0);
            double kl = IdKey(s, id.Jl, t.Zl), kr = IdKey(s, id.Jr, t.Zr);
            double kd = IdKey(s, id.Jd, t.Zd), ku = IdKey(s, id.Ju, t.Zu);
            double cL = Continuous(s, t.Z0, t.Zl), cR = Continuous(s, t.Z0, t.Zr);
            double cU = Continuous(s, t.Z0, t.Zu), cD = Continuous(s, t.Z0, t.Zd);
            const double half = 0.5; // ID 키 간격은 1.0 이라 «다르다» 의 경계는 그 절반이다(정본 step(0.5, …))
            if (!dilate)
                return Max4(cL * Step(half, k0 - kl), cR * Step(half, k0 - kr), cU * Step(half, k0 - ku), cD * Step(half, k0 - kd));
            return Max4(cL * Step(half, Math.Abs(k0 - kl)), cR * Step(half, Math.Abs(k0 - kr)),
                        cU * Step(half, Math.Abs(k0 - ku)), cD * Step(half, Math.Abs(k0 - kd)));
        }

        /// <summary>
        /// 네 항의 합 — `max(sil, crs, idl) × 지평선 컷`. 1 이면 그 화소를 선 색으로 덮는다.
        /// </summary>
        public static double Edge(EdgeOutlineSpec s, EdgeDepthTaps t, EdgeIdTaps id, EdgeGeom g, double bufScale, EdgeOutlineTerms on)
        {
            if (!on.AnyOn) return 0.0;
            bool dilate = DilateOn(s, bufScale);
            double sil = on.Silhouette ? Silhouette(s, t, dilate) : 0.0;
            double crs = Crease(s, t, g, dilate, on);
            double idl = IdLine(s, t, id, dilate, on);
            return NearCut(s, t.Z0) ? Max(sil, Max(crs, idl)) : 0.0;
        }
    }
}
