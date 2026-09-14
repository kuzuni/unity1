using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Render;

namespace Forge.Tests
{
    /// <summary>
    /// T330 1회차 — 파츠 ID 항의 **번호·부호화·«ID 패스에 넣는가»** 규칙(정본 `scene3d.js` 872~966)을 화면 없이 잰다.
    /// 컴포짓 쪽 셈(`IdKey`·`IdLine`)은 T147 의 `EdgeOutlineRulesTests` 가 이미 재고 있으니 여기서는 그 앞단 — 파츠가 버퍼에 무엇을 남기는가 — 만 본다.
    /// </summary>
    public class EdgePartIdRulesTests
    {
        static EdgeOutlineSpec S()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            string file = Path.Combine(root, "Assets", "Forge", "Resources", "EdgeOutlineUi.json");
            return EdgeOutlineSpec.From(MiniJson.ParseObject(File.ReadAllText(file)));
        }

        [Test]
        public void 번호는_1에서_시작해_65535_뒤_1로_돌고_0은_안_나온다()
        {
            Assert.AreEqual(1, EdgePartIdRules.Next(0), "정본 879: ((seq||0) % 65535) + 1");
            Assert.AreEqual(2, EdgePartIdRules.Next(1));
            Assert.AreEqual(65535, EdgePartIdRules.Next(65534));
            Assert.AreEqual(1, EdgePartIdRules.Next(65535), "16bit 순환 — 0(배경 키)을 건너뛴다");
            int n = 0;
            for (int i = 0; i < 70000; i++) { n = EdgePartIdRules.Next(n); Assert.AreNotEqual(EdgePartIdRules.Background, n); }
            Assert.Throws<ArgumentOutOfRangeException>(() => EdgePartIdRules.Next(-1));
        }

        [Test]
        public void 부호화는_정본_uId_그대로이고_되읽으면_같은_번호다()
        {
            double r, g;
            EdgePartIdRules.Encode(1, out r, out g); Assert.AreEqual(1 / 255.0, r, 1e-12); Assert.AreEqual(0, g, 1e-12);
            EdgePartIdRules.Encode(255, out r, out g); Assert.AreEqual(1, r, 1e-12); Assert.AreEqual(0, g, 1e-12);
            EdgePartIdRules.Encode(256, out r, out g); Assert.AreEqual(0, r, 1e-12); Assert.AreEqual(1 / 255.0, g, 1e-12);
            EdgePartIdRules.Encode(65535, out r, out g); Assert.AreEqual(1, r, 1e-12); Assert.AreEqual(1, g, 1e-12);
            for (int n = 1; n <= 65535; n++)
            {
                EdgePartIdRules.Encode(n, out r, out g);
                // 8bit 텍스처가 양자화한 값으로도 되읽힌다
                double rq = Math.Round(r * 255) / 255, gq = Math.Round(g * 255) / 255;
                Assert.AreEqual(n, EdgePartIdRules.Decode(rq, gq), "번호 " + n);
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => EdgePartIdRules.Encode(0, out r, out g));
            Assert.Throws<ArgumentOutOfRangeException>(() => EdgePartIdRules.Encode(65536, out r, out g));
        }

        [Test]
        public void 버퍼_한_화소는_컴포짓_IdKey_에서_제_번호로_읽히고_깊이가_어긋나면_0이다()
        {
            EdgeOutlineSpec s = S();
            Assert.AreEqual(255, s.IdLoF, 1e-9, "표의 id_lo_f");
            Assert.AreEqual(65280, s.IdHiF, 1e-9, "표의 id_hi_f — 256 × 255");
            foreach (int n in new[] { 1, 7, 255, 256, 4097, 65535 })
            {
                double z = 12.5;
                EdgeIdSample j = EdgePartIdRules.Sample(s, n, z);
                Assert.AreEqual(z / s.IdZFar, j.A, 1e-12, "a = 선형깊이 / idZFar");
                Assert.AreEqual(n, EdgeOutlineRules.IdKey(s, j, z), 1e-6, "키 = r·255 + g·65280 = 번호 " + n);
                Assert.AreEqual(0, EdgeOutlineRules.IdKey(s, j, z + s.IdTolZ + 0.01), 1e-9, "가려진 화소(깊이 어긋남 > tol)는 키 0");
                Assert.AreEqual(n, EdgeOutlineRules.IdKey(s, j, z + s.IdTolZ - 0.01), 1e-6, "허용 오차 안이면 제 번호");
            }
            // idZFar 너머는 a 가 1 로 눌려 어떤 깊이와도 안 맞는다 — 정본 `clamp(vZ/uZFar,0,1)`.
            EdgeIdSample far = EdgePartIdRules.Sample(s, 3, s.IdZFar * 2);
            Assert.AreEqual(1, far.A, 1e-12);
            Assert.AreEqual(0, EdgeOutlineRules.IdKey(s, far, s.IdZFar * 2), 1e-9);
            Assert.AreEqual(0, EdgePartIdRules.Sample(s, 3, -1).A, 1e-12, "음수 깊이는 0 으로 눌린다");
        }

        [Test]
        public void 불투명하고_깊이를_쓰는_파츠만_ID_를_쓴다()
        {
            Assert.IsTrue(EdgePartIdRules.OpaqueDepth(false, true));
            Assert.IsFalse(EdgePartIdRules.OpaqueDepth(true, true), "투명(궤적·오라)");
            Assert.IsFalse(EdgePartIdRules.OpaqueDepth(false, false), "depthWrite off(가산 글로우 — 알파 1 이라도)");
        }

        // 뷰공간 AABB 여덟 꼭짓점 — 중심 (cx, cy, zv) · 반폭 (hx, hy, hz)
        static void Box(double cx, double cy, double zv, double hx, double hy, double hz, out double[] vx, out double[] vy, out double[] vz)
        {
            vx = new double[8]; vy = new double[8]; vz = new double[8];
            for (int b = 0; b < 8; b++)
            {
                vx[b] = cx + ((b & 1) != 0 ? hx : -hx);
                vy[b] = cy + ((b & 2) != 0 ? hy : -hy);
                vz[b] = zv + ((b & 4) != 0 ? hz : -hz);
            }
        }

        [Test]
        public void 화면_크기는_투영_AABB_의_짧은_변이고_문턱_6_CSS_px_아래는_ID_패스에서_뺀다()
        {
            EdgeOutlineSpec s = S();
            Assert.AreEqual(6, s.IdMinCssPx, 1e-9, "표의 id_min_css_px — 흰자 4.2 · 옆구리 4.0 · 팔 ≈11");
            double cssH = 960, tanH = Math.Tan(60 * Math.PI / 360), aspect = 9.0 / 16;
            double kx = cssH / (2 * tanH * aspect), ky = cssH / (2 * tanH);   // 정본 957 배율 그대로
            double[] vx, vy, vz; bool near;
            // 납작한 판(가로 1 · 세로 3 · 두께 .2 · 10 앞) — 짧은 변은 가로이고 가까운 면이 가장 크게 찍힌다
            Box(0, 0, 10, 0.5, 1.5, 0.1, out vx, out vy, out vz);
            double side = EdgePartIdRules.ShortSideCssPx(vx, vy, vz, cssH, tanH, aspect, out near);
            Assert.IsFalse(near);
            Assert.AreEqual(2 * 0.5 / 9.9 * kx, side, 1e-9, "짧은 변 = 가로 · 가까운 면(zv 9.9)의 폭");
            Assert.Less(side, 2 * 1.5 / 9.9 * ky, "세로가 아니라 가로가 판정 변이다");
            Assert.IsTrue(EdgePartIdRules.UseId(s, side, near), "팔 크기의 파츠는 남는다");
            // 같은 판을 멀리 보내 짧은 변이 6 아래로 내려가면 뺀다(구 지름으로 재면 대각선 √(1²+3²) 이 통과시킨다 — 정본 941~949)
            double zFar = 2 * 0.5 * kx / 5.9 + 0.1;   // 가까운 면에서 폭 5.9px 이 되는 거리
            Box(0, 0, zFar, 0.5, 1.5, 0.1, out vx, out vy, out vz);
            side = EdgePartIdRules.ShortSideCssPx(vx, vy, vz, cssH, tanH, aspect, out near);
            Assert.AreEqual(5.9, side, 1e-9);
            Assert.IsFalse(EdgePartIdRules.UseId(s, side, near), "5.9 < 6 → 뺀다(0 을 심지 않는다)");
            Box(0, 0, 2 * 0.5 * kx / 6.0 + 0.1 - 1e-6, 0.5, 1.5, 0.1, out vx, out vy, out vz);   // 가까운 면에서 폭이 딱 6.0 이 되는 거리보다 조금 앞
            side = EdgePartIdRules.ShortSideCssPx(vx, vy, vz, cssH, tanH, aspect, out near);
            Assert.GreaterOrEqual(side, 6);
            Assert.IsTrue(EdgePartIdRules.UseId(s, side, near), "6 이상은 남는다");
            // 옆으로 치우친 판은 원근 기울기만큼 넓어진다 — 투영 AABB 는 여덟 꼭짓점의 가장 바깥(가까운 면 오른쪽 · 먼 면 왼쪽)을 잡는다(정본 954~962)
            Box(3, -2, 10, 0.5, 1.5, 0.1, out vx, out vy, out vz);
            Assert.AreEqual((3.5 / 9.9 - 2.5 / 10.1) * kx, EdgePartIdRules.ShortSideCssPx(vx, vy, vz, cssH, tanH, aspect, out near), 1e-6);
        }

        [Test]
        public void 카메라_코앞_꼭짓점은_크다고_보고_무조건_넣는다()
        {
            EdgeOutlineSpec s = S();
            double[] vx, vy, vz; bool near;
            Box(0, 0, 0.1, 0.01, 0.01, 0.06, out vx, out vy, out vz);   // 뒤 꼭짓점 zv .16 · 앞 꼭짓점 zv .04 ≤ .05
            double side = EdgePartIdRules.ShortSideCssPx(vx, vy, vz, 960, Math.Tan(60 * Math.PI / 360), 9.0 / 16, out near);
            Assert.IsTrue(near, "정본 959: zv ≤ 0.05 → near");
            Assert.AreEqual(0, side, 1e-12);
            Assert.IsTrue(EdgePartIdRules.UseId(s, side, near), "near 는 문턱을 안 본다");
            Assert.Throws<ArgumentException>(() => EdgePartIdRules.ShortSideCssPx(new double[7], vy, vz, 960, 1, 1, out near));
        }
    }
}
