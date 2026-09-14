using System;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Render;

namespace Forge.Tests
{
    /// <summary>
    /// T147 1회차 — 캐릭터 윤곽선(후처리 깊이-엣지 아웃라인)의 **셈**을 정본 `web/js/scene3d.js initPost()` 그대로 재현한다.
    /// 화면(URP 패스·셰이더·화소 판정)은 2회차. 여기서는 깊이 배열을 손으로 세워 네 항과 두 함정을 잰다.
    /// </summary>
    public class EdgeOutlineRulesTests
    {
        const int N = 512; // 정본 컴포짓 버퍼 한 변(texel = 1/512) — u,v 환산에만 쓴다
        static EdgeOutlineSpec S()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            string file = Path.Combine(root, "Assets", "Forge", "Resources", "EdgeOutlineUi.json");
            return EdgeOutlineSpec.From(MiniJson.ParseObject(File.ReadAllText(file)));
        }

        static EdgeDepthTaps T(Func<int, int, double> z, int x, int y)
        {
            return new EdgeDepthTaps
            {
                Z0 = z(x, y), Zl = z(x - 1, y), Zr = z(x + 1, y), Zd = z(x, y - 1), Zu = z(x, y + 1),
                Z2l = z(x - 2, y), Z2r = z(x + 2, y), Z2d = z(x, y - 2), Z2u = z(x, y + 2),
                Zdl = z(x - 1, y - 1), Zdr = z(x + 1, y - 1), Zul = z(x - 1, y + 1), Zur = z(x + 1, y + 1),
            };
        }

        /// <summary>proj = (1,1) · texel = 1/N 인 화면에서 화소 (x,y) 의 시야평면 좌표와 화소 폭.</summary>
        static EdgeGeom G(int x, int y)
        {
            return new EdgeGeom { Ux = x * 2.0 / N - 1.0, Uy = y * 2.0 / N - 1.0, Dux = 2.0 / N, Duy = 2.0 / N };
        }

        static EdgeIdTaps NoId() { return new EdgeIdTaps { Has = false }; }

        /// <summary>y=0 줄에서 x=a..b 가 칠해지는지 — `1`/`0` 문자열로 본다(두께를 눈으로 세려고).</summary>
        static string Row(EdgeOutlineSpec s, Func<int, int, double> z, double bufScale, int a, int b)
        {
            var sb = new System.Text.StringBuilder();
            for (int x = a; x <= b; x++) sb.Append(EdgeOutlineRules.Edge(s, T(z, x, 0), NoId(), G(x, 0), bufScale, EdgeOutlineTerms.All) > 0 ? '1' : '0');
            return sb.ToString();
        }

        [Test]
        public void 임계는_전부_표에서_오고_정본_유니폼_그대로다()
        {
            var s = S();
            Assert.AreEqual(0.028, s.EdgeK, 1e-12, "edgeK");
            Assert.AreEqual(0.9, s.NormalK, 1e-12, "normalK ≈ 84°");
            Assert.AreEqual(0.010, s.CreaseK, 1e-12, "creaseK");
            Assert.AreEqual(0.70, s.CreaseHystF, 1e-12, "곡률 이력 비율");
            Assert.AreEqual(22.0, s.EdgeMaxZ, 1e-12, "지평선 컷");
            Assert.AreEqual(32.0, s.IdZFar, 1e-12); Assert.AreEqual(0.35, s.IdTolZ, 1e-12);
            Assert.AreEqual("#000000", s.LineHex, "정본 선 색은 검정");
            // 팽창은 버퍼/CSS 비 2 이상에서만 — DPR 1 은 검출 1px 그대로 두어 전 대역 1 CSS px 로 통일된다.
            Assert.IsFalse(EdgeOutlineRules.DilateOn(s, 1.0), "DPR 1 에서 팽창을 걸면 선이 2 CSS px 이 되어 얇은 파츠를 먹는다");
            Assert.IsTrue(EdgeOutlineRules.DilateOn(s, 2.0)); Assert.IsTrue(EdgeOutlineRules.DilateOn(s, 3.0));
        }

        [Test]
        public void 기울어진_평면은_한_화소도_안_칠한다()
        {
            var s = S();
            // 뷰공간 평면 = 역깊이 q 가 시야평면 좌표의 1차식. 복원 법선은 상수, 2차 차분은 0 이라 ②③ 이 정확히 0 이어야 한다.
            Func<int, int, double> z = (x, y) => 1.0 / (0.09 + 0.01 * (x * 2.0 / N - 1.0) + 0.004 * (y * 2.0 / N - 1.0));
            for (int x = 240; x <= 260; x++)
                for (int y = -1; y <= 1; y++)
                    Assert.AreEqual(0.0, EdgeOutlineRules.Edge(s, T(z, x, y), NoId(), G(x, y), 2.0, EdgeOutlineTerms.All), 1e-12,
                        "평면 위 화소 (" + x + "," + y + ") 가 칠해졌다 — 지면이 검게 얼룩진다");
        }

        [Test]
        public void 실루엣은_계단의_가까운_쪽만_칠하고_두께는_팽창이_정한다()
        {
            var s = S();
            Func<int, int, double> z = (x, y) => x < 10 ? 10.0 : 12.0; // 근측 10 · 원측 12
            Assert.AreEqual("0011000", Row(s, z, 2.0, 6, 12), "DPR 2+ → 근측 2px(8·9), 원측은 0px");
            Assert.AreEqual("0001000", Row(s, z, 1.0, 6, 12), "DPR 1 → 팽창 없이 1px(9) — 그래도 1.00 CSS px");
            // 비대칭 규칙: 먼 쪽은 자기 이웃 중 더 먼 화소가 없어 0 이고, 팽창도 계단을 건너뛰지 못한다(cN 가드).
            Assert.AreEqual(0.0, EdgeOutlineRules.Silhouette(s, T(z, 10, 0), true), 1e-12, "계단의 먼 쪽에 선이 붙으면 두께가 개체마다 달라진다");
        }

        [Test]
        public void 지평선_컷은_두께에_절대_끼어들지_않는다()
        {
            var s = S();
            // 🚨 함정 ⑴(정본 2026-08-25 실측): `step(z0, edgeMaxZ)` 를 대칭 판정에 곱하면 하늘 쪽 화소가 컷에 걸려
            //    지워지는 바람에 **하늘 경계만 1px, 나머지는 2px** 이 됐다 — 두께가 '깊이 계단의 종류' 를 따라갔다.
            Func<int, int, double> sky = (x, y) => x < 10 ? 15.0 : 60.0;   // 컷(22) 너머 = 하늘
            Func<int, int, double> obj = (x, y) => x < 10 ? 15.0 : 18.0;   // 둘 다 컷 안 = 개체끼리 겹침
            Assert.AreEqual(Row(s, obj, 2.0, 6, 12), Row(s, sky, 2.0, 6, 12), "하늘 경계와 개체 경계의 두께가 다르다");
            Assert.AreEqual("0011000", Row(s, sky, 2.0, 6, 12));
            // 컷 자체는 산다 — 화소가 통째로 컷 너머면 선을 안 그린다(지평선이 화면을 가로지르는 검정선 방지).
            Func<int, int, double> far = (x, y) => x < 10 ? 40.0 : 48.0;
            Assert.AreEqual("0000000", Row(s, far, 2.0, 6, 12), "컷 너머 계단은 선이 없어야 한다");
        }

        [Test]
        public void 작은_계단은_곡률이_잡고_법선항은_그_옆에_한_줄을_덧대지_않는다()
        {
            var s = S();
            // 0.15 유닛 계단 — 실루엣 임계(0.028×10=0.28)에 못 미치고 두 면은 나란해 법선 차도 0 인 자리.
            Func<int, int, double> z = (x, y) => x < 10 ? 10.0 : 10.15;
            Assert.AreEqual(0.0, EdgeOutlineRules.Silhouette(s, T(z, 9, 0), true), 1e-12, "이 계단은 실루엣이 못 잡는다(그래서 곡률항이 있다)");
            Assert.AreEqual("0001100", Row(s, z, 2.0, 6, 12), "곡률선은 접힘 두 날개(9·10) 2px — 비접촉 게이트가 법선항의 덧줄을 막아야 한다");
            // 🚨 게이트 방향이 반대면 턱↔가슴 선이 죽는다: 곡률선이 있는 자리에서 법선항만 꺼진다.
            var offNormal = new EdgeOutlineTerms { Silhouette = true, Crease = true, Id = true };
            Assert.AreEqual("0001100", Row2(s, z, 2.0, 6, 12, offNormal), "법선항을 꺼도 곡률선은 그대로 — 이 선의 임자는 곡률항이다");
        }

        static string Row2(EdgeOutlineSpec s, Func<int, int, double> z, double bufScale, int a, int b, EdgeOutlineTerms on)
        {
            var sb = new System.Text.StringBuilder();
            for (int x = a; x <= b; x++) sb.Append(EdgeOutlineRules.Edge(s, T(z, x, 0), NoId(), G(x, 0), bufScale, on) > 0 ? '1' : '0');
            return sb.ToString();
        }

        [Test]
        public void 파츠_ID_는_같은_평면_경계를_잡고_가려진_파츠는_유령선을_안_그린다()
        {
            var s = S();
            Func<int, int, double> z = (x, y) => 10.0; // 깊이 계단 0 · 법선 차 0 — ①②③ 이 원리상 못 잡는 자리
            Func<int, EdgeIdSample> part = x => new EdgeIdSample { R = (x < 10 ? 5.0 : 9.0) / 255.0, G = 0.0, A = 10.0 / 32.0 };
            Func<int, EdgeIdTaps> id = x => new EdgeIdTaps { Has = true, J0 = part(x), Jl = part(x - 1), Jr = part(x + 1), Jd = part(x), Ju = part(x) };
            Assert.AreEqual(5.0, EdgeOutlineRules.IdKey(s, part(9), 10.0), 1e-9, "키 = r*255 + g*65280");
            Assert.AreEqual(9.0, EdgeOutlineRules.IdKey(s, part(10), 10.0), 1e-9);
            // 팽창 off = 키가 큰 쪽 한 줄만(1 버퍼px) · on = 양쪽(2 버퍼px). 두 대역 다 1.00 CSS px.
            Assert.AreEqual(0.0, EdgeOutlineRules.Edge(s, T(z, 9, 0), id(9), G(9, 0), 1.0, EdgeOutlineTerms.All), 1e-12);
            Assert.AreEqual(1.0, EdgeOutlineRules.Edge(s, T(z, 10, 0), id(10), G(10, 0), 1.0, EdgeOutlineTerms.All), 1e-12, "한쪽을 고르는 기준은 ID 키가 큰 쪽");
            Assert.AreEqual(1.0, EdgeOutlineRules.Edge(s, T(z, 9, 0), id(9), G(9, 0), 2.0, EdgeOutlineTerms.All), 1e-12);
            Assert.AreEqual(1.0, EdgeOutlineRules.Edge(s, T(z, 10, 0), id(10), G(10, 0), 2.0, EdgeOutlineTerms.All), 1e-12);
            // 가시성 검증 — ID 패스는 자기 깊이버퍼로 그리므로 언덕 뒤 펫도 ID 를 쓴다. 씬 깊이와 어긋나면 키 0.
            var ghost = new EdgeIdSample { R = 9.0 / 255.0, G = 0.0, A = 5.0 / 32.0 };
            Assert.AreEqual(0.0, EdgeOutlineRules.IdKey(s, ghost, 10.0), 1e-12, "가려진 액터 화소는 키 0(=배경) 으로 떨어져야 한다");
            var ghosts = new EdgeIdTaps { Has = true, J0 = ghost, Jl = ghost, Jr = ghost, Jd = ghost, Ju = ghost };
            Assert.AreEqual(0.0, EdgeOutlineRules.Edge(s, T(z, 10, 0), ghosts, G(10, 0), 2.0, EdgeOutlineTerms.All), 1e-12, "지면 위에 유령선이 떴다");
            // 액터가 하나도 없는 프레임(ID 버퍼가 안 채워짐)이면 항이 통째로 0 이다.
            Assert.AreEqual(0.0, EdgeOutlineRules.IdLine(s, T(z, 10, 0), new EdgeIdTaps { Has = false, J0 = part(10), Jl = part(9) }, true, EdgeOutlineTerms.All), 1e-12);
        }

        [Test]
        public void off_프레임은_네_항을_한꺼번에_끈다()
        {
            var s = S();
            // 🚨 함정 ⑵(정본 scene3d.js 693 · 이 저장소가 두 번 밟았다): 하나라도 켜 두면 그 선이 on/off 양쪽에
            //    똑같이 찍혀 차분 마스크에서 통째로 지워진다. 그래서 끄는 자리를 `Off` 하나로만 열어 둔다.
            Func<int, int, double> big = (x, y) => x < 10 ? 10.0 : 12.0;
            Func<int, int, double> small = (x, y) => x < 10 ? 10.0 : 10.15;
            Func<int, EdgeIdSample> part = x => new EdgeIdSample { R = (x < 10 ? 5.0 : 9.0) / 255.0, G = 0.0, A = 10.0 / 32.0 };
            var id = new EdgeIdTaps { Has = true, J0 = part(10), Jl = part(9), Jr = part(11), Jd = part(10), Ju = part(10) };
            Assert.IsFalse(EdgeOutlineTerms.Off.AnyOn);
            Assert.IsTrue(EdgeOutlineTerms.All.AnyOn);
            for (int x = 6; x <= 12; x++)
            {
                Assert.AreEqual(0.0, EdgeOutlineRules.Edge(s, T(big, x, 0), NoId(), G(x, 0), 2.0, EdgeOutlineTerms.Off), 1e-12, "실루엣이 off 프레임에 남았다");
                Assert.AreEqual(0.0, EdgeOutlineRules.Edge(s, T(small, x, 0), NoId(), G(x, 0), 2.0, EdgeOutlineTerms.Off), 1e-12, "곡률·법선이 off 프레임에 남았다");
            }
            Func<int, int, double> flat = (x, y) => 10.0;
            Assert.AreEqual(0.0, EdgeOutlineRules.Edge(s, T(flat, 10, 0), id, G(10, 0), 2.0, EdgeOutlineTerms.Off), 1e-12, "ID 항이 off 프레임에 남았다");
        }
    }
}
