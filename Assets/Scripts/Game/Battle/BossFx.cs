using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Forge.Core.Battle;
using Forge.Core.BattleFx;
using Forge.Core.Voxel;
using Forge.Game.Audio;
using Forge.Game.Ui;
using Forge.Game.Voxel;

namespace Forge.Game.Battle
{
    /// <summary>
    /// 보스 외형(정본 `bossMaterialTell` 11362 · `bossRegalia` 11415). 재질은 살을 ×0.68 로 내리고(흰자는 안 건드린다 — 정점색 팔레트라 재질 배율 한 번), 레갈리아(관·등가시·견갑뿔)는
    /// **몸 정점에서 좌표를 뽑아** 앉힌다: 12방위 섹터 하위 30% 분위 반경 · 슬라이스 14 · 방위 60% 이상 찬 «진짜 단면» · 그 높이 정점의 xz 중심. 조형은 큐브 링·계단 테이퍼·큐브 젬(voxel.js
    /// `ring/taper/gem`)을 T4 <see cref="VoxelGeometry"/> 로 굽는다. ⚠ 배율(1.9)을 걸기 **전에** 부른다 · 반환값이 새 topY(관이 자란 만큼 HP 바를 올린다).
    /// 계산은 three 좌표(z = −유니티 z)로 하고 놓을 때 <see cref="ThreeSpace"/> 로 옮긴다.
    /// </summary>
    public static class BossLook
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"), EmissionId = Shader.PropertyToID("_EmissionColor"), MetalId = Shader.PropertyToID("_Metallic"), SmoothId = Shader.PropertyToID("_Smoothness");

        sealed class Rad { public double R; public int Full; }

        /// <summary>정본 `Voxel.ellipse(rx, rz, h, {rix, riz, y0, color})`(주름 없음).</summary>
        public static List<VoxelCell> Ellipse(double rx, double rz, int h, double rix, double riz, int y0, int color)
        {
            var o = new List<VoxelCell>();
            if (rx <= 0 || rz <= 0 || h <= 0) return o;
            int mx = (int)Math.Floor(rx), mz = (int)Math.Floor(rz);
            for (int x = -mx; x <= mx; x++)
                for (int z = -mz; z <= mz; z++)
                {
                    double ox = x / rx, oz = z / rz;
                    if (ox * ox + oz * oz > 1.0000001) continue;
                    if (rix > 0 && riz > 0) { double ix = x / rix, iz = z / riz; if (ix * ix + iz * iz <= 1.0000001) continue; }
                    for (int y = 0; y < h; y++) o.Add(new VoxelCell(x, y0 + y, z, color));
                }
            return o;
        }
        /// <summary>`Voxel.ring(rOut, t, h)`.</summary>
        public static List<VoxelCell> Ring(double rOut, double t, int h, int color) { return Ellipse(rOut, rOut, h, rOut - t, rOut - t, 0, color); }
        /// <summary>`Voxel.taper(r0, r1, h)` — 층마다 반지름을 선형 보간(0.5 미만 층은 건너뛴다).</summary>
        public static List<VoxelCell> Taper(double r0, double r1, int h, int color)
        {
            var o = new List<VoxelCell>();
            if (h <= 0) return o;
            int hh = Math.Max(1, h);
            for (int y = 0; y < hh; y++)
            {
                double k = hh == 1 ? 0 : (double)y / (hh - 1);
                double rr = r0 + (r1 - r0) * k;
                if (rr < 0.5) continue;
                o.AddRange(Ellipse(rr, rr, 1, 0, 0, y, color));
            }
            return o;
        }
        /// <summary>`Voxel.gem(r)` — |x|+|y|+|z| ≤ r.</summary>
        public static List<VoxelCell> Gem(double r, int color)
        {
            var o = new List<VoxelCell>();
            int m = (int)Math.Floor(r);
            for (int x = -m; x <= m; x++) for (int y = -m; y <= m; y++) for (int z = -m; z <= m; z++)
                if (Math.Abs(x) + Math.Abs(y) + Math.Abs(z) <= r) o.Add(new VoxelCell(x, y, z, color));
            return o;
        }

        static Material Metal(int color, int emissive, double emissiveI, double metal, double rough)
        {
            Shader sh = EnemyBodyFx.Find() ?? Shader.Find(VoxelMaterials.LitShader) ?? Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(sh);
            m.name = "regalia " + color.ToString("x6");
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, VoxelMaterials.ToColor(color)); else m.color = VoxelMaterials.ToColor(color);
            if (m.HasProperty(MetalId)) m.SetFloat(MetalId, (float)metal);
            if (m.HasProperty(SmoothId)) m.SetFloat(SmoothId, (float)(1 - rough));
            if (m.HasProperty(EmissionId)) { m.EnableKeyword("_EMISSION"); m.SetColor(EmissionId, VoxelMaterials.ToColor(emissive) * (float)emissiveI); }
            return m;
        }

        static MeshRenderer Put(Transform parent, string tag, List<VoxelCell> cells, double cell, Material mat, EnemyBodyFx body, List<MeshRenderer> sink)
        {
            var o = VoxelBuildOptions.Default; o.Size = cell; o.Jitter = FxRules.RegaliaVoxelJitter; o.Ao = FxRules.RegaliaVoxelAo; o.Center = true; o.Color = 0xffffff; o.LeftHanded = true;
            Mesh mesh = VoxelMob.ToMesh(VoxelGeometry.Build(cells, o), "regalia " + tag, QualitySettings.activeColorSpace == ColorSpace.Linear);
            var go = new GameObject("regalia " + tag);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.On;
            if (body != null) body.Add(mr);
            if (sink != null) sink.Add(mr);
            return mr;
        }

        /// <summary>
        /// 보스 외형을 세운다 — 재질 배율(`bossMaterialTell`) + 레갈리아(`bossRegalia`). <paramref name="g"/> 는 아직 배율 1 인 리그 루트.
        /// 반환 = 새 topY(HP 바 높이). 만든 파츠 수는 <paramref name="made"/> 로.
        /// </summary>
        public static double Apply(Transform g, IList<MeshFilter> meshes, EnemyBodyFx body, int baseHex, double topY, bool humanoid, double cell, List<MeshRenderer> sink, out int made)
        {
            made = 0;
            if (body != null) body.SetTint(FxRules.BossTintHex());
            // 정점 월드 좌표 1회 수집 → 리그 로컬(three 좌표: z 반전)
            var V = new List<Vector3>();
            foreach (var mf in meshes)
            {
                if (mf == null || mf.sharedMesh == null) continue;
                var vs = mf.sharedMesh.vertices;
                for (int i = 0; i < vs.Length; i++)
                {
                    Vector3 p = g.InverseTransformPoint(mf.transform.TransformPoint(vs[i]));
                    V.Add(new Vector3(p.x, p.y, -p.z));
                }
            }
            if (V.Count < 10) return topY;
            double bodyTop = double.NegativeInfinity, bodyBot = double.PositiveInfinity;
            foreach (var p in V) { if (p.y > bodyTop) bodyTop = p.y; if (p.y < bodyBot) bodyBot = p.y; }
            double bodyH = Math.Max(bodyTop - bodyBot, 0.2);
            int SECT = FxRules.RegaliaSectors;

            Func<double, double, Vector2, Rad> radAt = (y, band, c) =>
            {
                var s = new double[SECT];
                foreach (var p in V)
                {
                    if (Math.Abs(p.y - y) > band) continue;
                    double dx = p.x - c.x, dz = p.z - c.y;
                    double d = Math.Sqrt(dx * dx + dz * dz);
                    int k = (int)Math.Floor((Math.Atan2(dz, dx) + Math.PI) / (Math.PI * 2) * SECT);
                    if (k >= SECT) k = SECT - 1; else if (k < 0) k = 0;
                    if (d > s[k]) s[k] = d;
                }
                var nz = new List<double>();
                foreach (double x in s) if (x > 0) nz.Add(x);
                nz.Sort();
                return new Rad { R = nz.Count > 0 ? nz[(int)Math.Floor(nz.Count * 0.3)] : 0, Full = nz.Count };
            };
            Func<double, double, Vector2> centroidAt = (y, band) =>
            {
                double sx = 0, sz = 0; int n = 0;
                foreach (var p in V) { if (Math.Abs(p.y - y) > band) continue; sx += p.x; sz += p.z; n++; }
                return n > 0 ? new Vector2((float)(sx / n), (float)(sz / n)) : Vector2.zero;
            };
            Func<double, double, double, double> topAt = (x, z, rad) =>
            {
                double y = double.NegativeInfinity;
                foreach (var p in V) { double dx = p.x - x, dz = p.z - z; if (Math.Sqrt(dx * dx + dz * dz) > rad) continue; if (p.y > y) y = p.y; }
                return y;
            };
            Func<double, double, double, double> backAt = (y, band, half) =>
            {
                double z = 0;
                foreach (var p in V) { if (Math.Abs(p.y - y) > band || Math.Abs(p.x) > half) continue; if (p.z < z) z = p.z; }
                return z;
            };
            Func<double, double, double> sideAt = (y, band) =>
            {
                double x = 0;
                foreach (var p in V) { if (Math.Abs(p.y - y) > band) continue; if (Math.Abs(p.x) > x) x = Math.Abs(p.x); }
                return x;
            };

            Material gold = Metal(FxRules.RegaliaGold, FxRules.RegaliaGoldEmissive, FxRules.RegaliaGoldEmissiveI, 0.85, 0.3);
            Material goldD = Metal(FxRules.RegaliaGoldDark, FxRules.RegaliaGoldDarkEmissive, FxRules.RegaliaGoldDarkEmissiveI, 0.85, 0.42);
            int gemC = FxRules.GemColor(baseHex);
            Material gemM = Metal(gemC, gemC, FxRules.RegaliaGemEmissiveI, 0.15, 0.18);
            double VS = cell > 0 ? cell : EnemyGait.EnemyVs;
            Func<double, double> vr = r => Math.Max(0.5, r / VS);
            Func<double, int> vh = h => Math.Max(1, (int)Math.Round(h / VS));

            // ⑴ 관 — 위에서 슬라이스를 내려오며 몸이 실제로 굵어지는 첫 진짜 단면에
            int SLICES = FxRules.RegaliaSlices;
            double yLo = bodyTop - bodyH * FxRules.RegaliaSliceSpan, dY = (bodyTop - yLo) / SLICES;
            var prof = new List<KeyValuePair<double, Vector2>>();
            var profR = new List<Rad>();
            for (int i = 0; i < SLICES; i++)
            {
                double y = bodyTop - (i + 0.5) * dY;
                Vector2 c = centroidAt(y, dY * 0.75);
                prof.Add(new KeyValuePair<double, Vector2>(y, c));
                profR.Add(radAt(y, dY * 0.75, c));
            }
            int need = (int)Math.Ceiling(SECT * FxRules.RegaliaSolidFrac);
            var pool = new List<int>();
            for (int i = 0; i < SLICES; i++) if (profR[i].Full >= need) pool.Add(i);
            if (pool.Count == 0) for (int i = 0; i < SLICES; i++) pool.Add(i);
            double rMax = 0; foreach (int i in pool) if (profR[i].R > rMax) rMax = profR[i].R;
            int seat = pool[pool.Count - 1];
            foreach (int i in pool) if (profR[i].R >= rMax * FxRules.RegaliaSeatFrac) { seat = i; break; }
            double bandY = prof[seat].Key;
            Vector2 ctr = prof[seat].Value;
            double headR = Math.Max(profR[seat].R, bodyH * FxRules.RegaliaHeadRMin);
            var crownG = new GameObject("regalia crownG").transform;
            crownG.SetParent(g, false);
            crownG.localPosition = ThreeSpace.Pos(ctr.x, bandY, ctr.y);
            double bandR = headR * FxRules.RegaliaBandR;
            Put(crownG, "crown", Ring(vr(bandR + headR * FxRules.RegaliaRingTube), vh(headR * FxRules.RegaliaRingH), vh(headR * FxRules.RegaliaRingH), 0xffffff), VS, gold, body, sink); made++;
            double orn = Math.Max(headR, bodyH * FxRules.RegaliaOrnMin);
            double spikeH = orn * FxRules.RegaliaSpikeH, crownTop = 0;
            for (int i = 0; i < FxRules.RegaliaSpikes; i++)
            {
                double a = (double)i / FxRules.RegaliaSpikes * Math.PI * 2;
                bool front = i == 0;
                double h = spikeH * (front ? FxRules.RegaliaFrontK : 1);
                var sp = Put(crownG, "crown", Taper(vr(headR * FxRules.RegaliaSpikeR * (front ? FxRules.RegaliaFrontR : 1)), 0.5, vh(h), 0xffffff), VS, front ? gold : goldD, body, sink); made++;
                sp.transform.localPosition = ThreeSpace.Pos(Math.Sin(a) * bandR * FxRules.RegaliaSpikeRing, headR * FxRules.RegaliaSpikeLift + h / 2, Math.Cos(a) * bandR * FxRules.RegaliaSpikeRing);
                ThreeSpace.Apply(sp.transform, new[] { Math.Cos(a) * FxRules.RegaliaSpikeTilt, 0, -Math.Sin(a) * FxRules.RegaliaSpikeTilt });
                crownTop = Math.Max(crownTop, headR * FxRules.RegaliaSpikeLift + h);
            }
            var jewel = Put(crownG, "crown", Gem(vr(Math.Min(orn * FxRules.RegaliaGemR, headR * FxRules.RegaliaGemRMax)), 0xffffff), VS, gemM, body, sink); made++;
            jewel.transform.localPosition = ThreeSpace.Pos(0, headR * FxRules.RegaliaGemLift, bandR);
            jewel.transform.localScale = new Vector3(1, 1.25f, 0.55f);
            crownTop += bandY;

            // ⑵ 등가시 — 머리 자리에서 몸 뒤끝까지 등 위를 따라
            double zRear = backAt(bodyBot + bodyH * FxRules.RidgeY, bodyH * FxRules.RidgeBand, Math.Max(headR, bodyH * FxRules.RidgeHalfMin));
            for (int i = 0; i < FxRules.RidgeK.Length; i++)
            {
                double t = (double)(i + 1) / FxRules.RidgeK.Length;
                double zx = ctr.x * (1 - t), zz = ctr.y + (zRear - ctr.y) * t;
                double yTop = topAt(zx, zz, Math.Max(headR * FxRules.RidgeTopRad, bodyH * 0.05));
                if (double.IsInfinity(yTop)) continue;
                double h = headR * FxRules.RidgeH * FxRules.RidgeK[i];
                var sp = Put(g, "ridge", Taper(vr(headR * FxRules.RidgeR * FxRules.RidgeK[i]), 0.5, vh(h), 0xffffff), VS, i % 2 == 1 ? goldD : gold, body, sink); made++;
                sp.transform.localPosition = ThreeSpace.Pos(zx, yTop - h * FxRules.RidgeSink, zz);
                ThreeSpace.Apply(sp.transform, new[] { FxRules.RidgeTilt, 0, 0 });
            }

            // ⑶ 견갑 뿔 — 이족(팔 관절이 있는 종)만
            if (humanoid)
            {
                double y = bodyBot + bodyH * FxRules.HornY;
                double sx = sideAt(y, bodyH * FxRules.HornBand);
                if (sx > FxRules.HornMinSide)
                    foreach (int s in new[] { -1, 1 })
                    {
                        var horn = Put(g, "horn", Taper(vr(headR * FxRules.HornR), 0.5, vh(headR * FxRules.HornH), 0xffffff), VS, gold, body, sink); made++;
                        horn.transform.localPosition = ThreeSpace.Pos(s * sx * FxRules.HornX, y, 0);
                        ThreeSpace.Apply(horn.transform, new[] { 0, 0, -s * FxRules.HornTilt });
                    }
            }
            return Math.Max(topY, crownTop) + FxRules.RegaliaTopPad;
        }
    }

    /// <summary>
    /// 보스 등장 워닝(정본 `bossEntrance` 13480) — 배너(`UI.bossWarning` → <see cref="BattleOverlay"/>) · 사이렌(T30 <see cref="Sfx.BossSiren"/>) · 착지 지점 붉은 경고 기둥 ·
    /// 0.42초 박마다 경고 링/점광/흔들림 3회 · 카메라 돌리 인(`camPush` 0.9 ease-out) · 1.55초 착지 임팩트(흔들림 0.5 · fov 펀치 · 링 2 · 불티 22 · 점광 · 카메라 릴리즈 0.45초).
    /// </summary>
    public sealed class BossEntrance
    {
        readonly BattleScene scene;
        public bool Impacted { get; private set; }
        public bool Done { get; private set; }
        public int Beat { get; private set; } = -1;
        public MeshRenderer Pillar { get; private set; }

        public BossEntrance(BattleScene scene) { this.scene = scene; }

        public void Start()
        {
            double px = BattleRules.BossSpawnX + scene.WorldX;
            var overlay = BattleOverlay.Ensure();
            if (overlay != null) overlay.BossWarning(FxRules.BossWarnDur);
            Sfx.BossSiren();
            Pillar = scene.Impact.Pillar(px);
            Material pm = Pillar.sharedMaterial;
            Transform pt = Pillar.transform;
            scene.Anims.Add(FxRules.BossWarnDur, k =>
            {
                double t = k * FxRules.BossWarnDur;
                if (!Impacted) scene.CamPush = FxRules.BossCamPushAt(t, BattleRules.BossImpact);
                int b = (int)Math.Floor(t / FxRules.BossBeat);
                if (b != Beat && b < FxRules.BossBeats)
                {
                    Beat = b;
                    scene.Impact.ExpandRing(new Vector3((float)px, 0, 0), FxRules.BossRingColor, FxRules.BossBeatRing(b));
                    scene.Impact.FlashLight(new Vector3((float)px, 0.3f, 0), FxRules.BossLightColor, FxRules.BossBeatLightDur);
                    scene.Shake(FxRules.BossBeatShake(b));
                }
                if (pt != null)
                {
                    double pulse = FxRules.PillarPulse(t);
                    FxUnlitMaterials.SetOpacity(pm, Impacted ? 0 : FxRules.PillarOpacity * pulse * Math.Min(1, t / FxRules.PillarFadeIn));
                    pt.localScale = new Vector3(1, (float)(FxRules.PillarScale0 + FxRules.PillarScaleK * Math.Min(1, t / BattleRules.BossImpact)), 1);
                    pt.localRotation = Quaternion.Euler(0, (float)(-t * FxRules.PillarSpin * Mathf.Rad2Deg), 0);
                }
                if (!Impacted && t >= BattleRules.BossImpact)
                {
                    Impacted = true;
                    scene.Shake(FxRules.BossImpactShake);
                    scene.FovPunch(FxRules.BossImpactFov, FxRules.BossImpactFovDur);
                    scene.Impact.ExpandRing(new Vector3((float)px, 0, 0), FxRules.BossImpactRing, FxRules.BossImpactRingR);
                    scene.Impact.ExpandRing(new Vector3((float)px, 0, 0), FxRules.BossImpactRing2, FxRules.BossImpactRing2R);
                    scene.Fx.Sparks(new Vector3((float)px, 0.25f, 0), (int)FxRules.BossImpactSparks, FxRules.BossImpactSpark, FxRules.BossImpactSparkSpeed);
                    scene.Impact.FlashLight(new Vector3((float)px, 0.6f, 0), FxRules.BossImpactLight, FxRules.BossImpactLightDur);
                    double push0 = scene.CamPush;
                    scene.Anims.Add(FxRules.BossCamRelease, kk => { scene.CamPush = push0 * (1 - kk); }, () => { scene.CamPush = 0; });
                }
            }, () =>
            {
                scene.CamPush = 0;
                Done = true;
                if (Pillar != null) scene.Impact.Remove(Pillar);
                Pillar = null;
            });
        }
    }
}
