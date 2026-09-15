using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;
using Forge.Core.Voxel;

namespace Forge.Tests
{
    /// <summary>
    /// T399 — 펫·탈것 승천 데코의 셈이 정본 `scene3d.js` 9653~9745 와 같은가: tier = stars % 6 순환 · 모티프 5 · 층별 조각 수와 자리 · 누적 단조.
    /// </summary>
    public class AscendDecorRulesTests
    {
        static string File_()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            return Path.Combine(root, "Assets", "Forge", "Resources", "AscendDecorUi.json");
        }
        static AscendDecorSpec S() { return AscendDecorSpec.From(MiniJson.ParseObject(File.ReadAllText(File_()))); }
        // 몸: 폭 .6 · 높이 1.0 · 깊이 .4 · 바닥 y 0 · 중심 x .1 z −.2 (정본 r = max(.6, .4)/2 = .3)
        static readonly double[] Min = { -0.2, 0.0, -0.4 }, Max = { 0.4, 1.0, 0.0 };

        [Test]
        public void 티어는_별을_6으로_순환하고_모티프_다섯이_정본_색이다()
        {
            var s = S();
            Assert.AreEqual(6, s.Cycle, "9653 `% 6`");
            int[] stars = { 0, 1, 2, 3, 4, 5, 6, 7, 11, 12, -1 };
            int[] tier = { 0, 1, 2, 3, 4, 5, 0, 1, 5, 0, 5 };
            for (int i = 0; i < stars.Length; i++) Assert.AreEqual(tier[i], s.Tier(stars[i]), "ascendTier(" + stars[i] + ")");
            Assert.IsNull(s.MotifOf(0), "tier 0 은 모티프 없음");
            Assert.AreEqual("fire", s.MotifOf(1).Name); Assert.AreEqual(0xff7a2a, s.MotifOf(1).Color);
            Assert.AreEqual("ice", s.MotifOf(2).Name); Assert.AreEqual(0x9fd8ff, s.MotifOf(2).Color);
            Assert.AreEqual("storm", s.MotifOf(3).Name); Assert.AreEqual(0xc9a0ff, s.MotifOf(3).Color);
            Assert.AreEqual("holy", s.MotifOf(4).Name); Assert.AreEqual(0xffe9a8, s.MotifOf(4).Color);
            Assert.AreEqual("abyss", s.MotifOf(5).Name); Assert.AreEqual(0x8a4dff, s.MotifOf(5).Color);
            Assert.AreEqual(0.5, s.Metalness, 1e-12, "9689 metalness .5"); Assert.AreEqual(0.5, s.Roughness, 1e-12, "roughness .5");
        }

        [Test]
        public void 층은_티어만큼_누적으로_얹히고_조각_수는_1_6_1_4_6_이다()
        {
            var s = S();
            int[] perLayer = { 0, 1, 6, 1, 4, 6 };   // L1 밴드 1 · L2 가시 6 · L3 룬 링 1 · L4 스터드 4 · L5 왕관 링 1 + 뿔 5
            for (int tier = 0; tier <= 5; tier++)
            {
                var plan = AscendDecorPlan.Make(s, tier, Min, Max);
                Assert.AreEqual(tier, plan.Tier);
                Assert.AreEqual(tier, plan.LayerCount, "tier " + tier + " — 층 수 = tier(누적 단조 계약)");
                int total = 0;
                for (int l = 1; l <= 5; l++)
                {
                    int want = l <= tier ? perLayer[l] : 0;
                    Assert.AreEqual(want, plan.CountOf(l), "tier " + tier + " 층 " + l + " 조각 수");
                    total += want;
                }
                Assert.AreEqual(total, plan.Pieces.Count);
                if (tier > 0) for (int i = 0; i < plan.Pieces.Count; i++) Assert.LessOrEqual(plan.Pieces[i].Layer, tier, plan.Pieces[i].Name + " 은 tier 위 층");
            }
            // 누적: tier n 의 앞 조각들은 tier n−1 의 조각과 같은 이름·자리다
            var p4 = AscendDecorPlan.Make(s, 4, Min, Max);
            var p5 = AscendDecorPlan.Make(s, 5, Min, Max);
            for (int i = 0; i < p4.Pieces.Count; i++)
            {
                Assert.AreEqual(p4.Pieces[i].Name, p5.Pieces[i].Name);
                Assert.AreEqual(p4.Pieces[i].Pos[1], p5.Pieces[i].Pos[1], 1e-12, p4.Pieces[i].Name + " y");
                Assert.AreEqual(p4.Pieces[i].CellCount, p5.Pieces[i].CellCount, p4.Pieces[i].Name + " 칸 수");
            }
            // 6승천 = 0승천 디자인(순환) · 빈 상자면 조각 0
            Assert.AreEqual(0, AscendDecorPlan.Make(s, 6, Min, Max).Pieces.Count);
            Assert.AreEqual(0, AscendDecorPlan.Make(s, 3, Min, Min).Pieces.Count, "box.isEmpty()");
        }

        [Test]
        public void 자리와_치수가_경계_상자_비율_그대로다()
        {
            var s = S();
            var plan = AscendDecorPlan.Make(s, 5, Min, Max);
            double r = 0.3, cx = 0.1, cz = -0.2, bandY = 0.55;
            Assert.AreEqual(r, plan.R, 1e-12, "r = max(sx, sz) / 2"); Assert.AreEqual(bandY, plan.BandY, 1e-12, "bandY = min.y + h × .55");
            var band = plan.Pieces.Find(p => p.Name == "band");
            Assert.AreEqual(1, band.Layer);
            Assert.AreEqual(System.Math.Max(0.02, r * 0.11), band.Size, 1e-12, "bs = max(.02, r×.11)");
            Assert.AreEqual(cx, band.Pos[0], 1e-12); Assert.AreEqual(bandY, band.Pos[1], 1e-12); Assert.AreEqual(cz, band.Pos[2], 1e-12);
            Assert.AreEqual(0x8b95a3, band.Color); Assert.AreEqual(-1, band.Emissive, "밴드는 발광 없음"); Assert.IsTrue(band.Center); Assert.AreEqual(0.05, band.Jitter, 1e-12);
            Assert.Greater(band.CellCount, 0, "링 칸");
            // 링은 속이 비어 있다(원점 칸 없음)
            Assert.IsFalse(band.Cells.Exists(c => c.X == 0 && c.Z == 0), "큐브 링은 속을 판다");

            var spike = plan.Pieces.Find(p => p.Name == "spike0");
            Assert.AreEqual(2, spike.Layer);
            double ss = System.Math.Max(0.015, r * 0.055);
            Assert.AreEqual(ss, spike.Size, 1e-12, "ss = max(.015, r×.055)");
            Assert.AreEqual(cx + r, spike.Pos[0], 1e-12, "가시 0 은 +x 방사 · r×1.0"); Assert.AreEqual(bandY, spike.Pos[1], 1e-12); Assert.AreEqual(cz, spike.Pos[2], 1e-12);
            Assert.AreEqual(-System.Math.PI / 2, spike.Rot[2], 1e-12, "rotation.z = −π/2"); Assert.AreEqual(0, spike.Rot[1], 1e-12, "rotation.y = −a (a = 0)");
            var spike3 = plan.Pieces.Find(p => p.Name == "spike3");
            Assert.AreEqual(-System.Math.PI, spike3.Rot[1], 1e-12, "가시 3 은 a = π → rotation.y = −π");
            Assert.AreEqual(cx - r, spike3.Pos[0], 1e-9);
            Assert.AreEqual(0xcfd6df, spike.Color);

            var rune = plan.Pieces.Find(p => p.Name == "rune");
            Assert.AreEqual(3, rune.Layer); Assert.IsTrue(rune.IsRune);
            Assert.AreEqual(r * 1.08, rune.RuneInner, 1e-12); Assert.AreEqual(r * 1.34, rune.RuneOuter, 1e-12); Assert.AreEqual(24, rune.RuneSegments);
            Assert.AreEqual(0.5, rune.Opacity, 1e-12); Assert.AreEqual(0x8a4dff, rune.Color, "tier 5 모티프 abyss");
            Assert.AreEqual(0.02, rune.Pos[1], 1e-12, "발밑 min.y + .02"); Assert.AreEqual(-System.Math.PI / 2, rune.Rot[0], 1e-12, "rotation.x = −π/2");

            var stud = plan.Pieces.Find(p => p.Name == "stud0");
            Assert.AreEqual(4, stud.Layer);
            double a0 = 0.4;
            Assert.AreEqual(cx + System.Math.Cos(a0) * r * 0.82, stud.Pos[0], 1e-12); Assert.AreEqual(0.8, stud.Pos[1], 1e-12, "min.y + h×.8"); Assert.AreEqual(cz + System.Math.Sin(a0) * r * 0.82, stud.Pos[2], 1e-12);
            Assert.AreEqual(0xffd54f, stud.Color); Assert.AreEqual(0xffb300, stud.Emissive); Assert.AreEqual(0.5, stud.EmissiveIntensity, 1e-12);

            var ring = plan.Pieces.Find(p => p.Name == "crown-ring");
            var horn = plan.Pieces.Find(p => p.Name == "crown-horn0");
            double cr = System.Math.Max(0.05, r * 0.34), cs = System.Math.Max(0.03, cr * 0.16);
            Assert.AreEqual(5, ring.Layer); Assert.AreEqual(5, horn.Layer);
            Assert.AreEqual(cs, ring.Size, 1e-12); Assert.AreEqual(cs, horn.Size, 1e-12, "왕관 전용 칸");
            Assert.AreEqual(1.0 + cr * 0.35, ring.Pos[1], 1e-12, "그룹 자리 max.y + cr×.35"); Assert.IsTrue(ring.Center);
            Assert.AreEqual(cx + cr, horn.Pos[0], 1e-12, "뿔 0 = 그룹 + (cr, cr×.25, 0)"); Assert.AreEqual(1.0 + cr * 0.35 + cr * 0.25, horn.Pos[1], 1e-12); Assert.IsFalse(horn.Center, "뿔은 center:false");
            Assert.AreEqual(0xffca28, ring.Emissive); Assert.AreEqual(0.35, ring.EmissiveIntensity, 1e-12);
        }

        [Test]
        public void 복셀_원형_넷은_정본_voxel_js_대로_깎인다()
        {
            // ring(3, 1, 1): 바깥 반지름 3 · 두께 1 → 안쪽 2 를 판다 · 원점 없음 · y 한 층
            var ring = Voxel.Ring(3, 1, 1, 0x123456);
            Assert.Greater(ring.Count, 0);
            Assert.IsFalse(ring.Exists(c => c.X == 0 && c.Z == 0));
            Assert.IsTrue(ring.TrueForAll(c => c.Y == 0 && c.C == 0x123456));
            Assert.IsTrue(ring.Exists(c => c.X == 3 && c.Z == 0), "바깥 반지름 3 의 칸");
            Assert.IsFalse(ring.Exists(c => c.X == 1 && c.Z == 0), "안쪽 반지름 2 안은 비었다");
            // taper(2, .5, 4): 네 층 · 밑 반지름 2 → 위 .5 · 맨 위 층은 한 칸
            var taper = Voxel.Taper(2, 0.5, 4, 0xffffff);
            Assert.AreEqual(4, new System.Collections.Generic.HashSet<int>(taper.ConvertAll(c => c.Y)).Count, "층 4");
            Assert.AreEqual(1, taper.FindAll(c => c.Y == 3).Count, "꼭대기 한 칸");
            Assert.Greater(taper.FindAll(c => c.Y == 0).Count, taper.FindAll(c => c.Y == 3).Count);
            Assert.AreEqual(0, Voxel.Taper(2, 1, 0, 0).Count, "h 0 은 빈 목록");
            // gem(2): |x|+|y|+|z| ≤ 2 → 25 칸(팔면체 계단)
            Assert.AreEqual(25, Voxel.Gem(2, 0).Count);
            Assert.AreEqual(7, Voxel.Gem(1, 0).Count);
            // ellipse(1, 1, 1) = 원점 + 4 이웃(반지름 1 판정은 칸 중심)
            Assert.AreEqual(5, Voxel.Ellipse(1, 1, 1, 0).Count);
        }
    }
}
