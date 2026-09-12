using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Forge.Core.BattleFx;
using Forge.Game.Voxel;

namespace Forge.Game.Battle
{
    /// <summary>
    /// 무기 궤적 리본(정본 `trailStart`·`updateTrail`·`trailImpact` 17053~17160) — 48세그 × 6정점 · 날 밑동(0.12)~날끝(TRAIL_TIP[모양]) · 영웅 로컬에서 날끝이 실제로 휘둘러질 때만 기록 ·
    /// 방향 반전은 스트로크 경계 · 프레임 사이 보간 ≤12 · 수명 0.22초 · 나이 기반 알파(pow 1.6 × 0.72 × gain) · 신선한 구간만 백색 코어(pow 4 × core). 접촉 순간 `Impact(tier)` 가 남은
    /// 리본을 티어 색·세기로 물들이고 0.22초에 걸쳐 되돌린다. 정본 ShaderMaterial 의 aFade 는 정점색 알파로 옮겼다(`Forge/FxUnlit` · 보통 블렌드 · 양면).
    /// </summary>
    public sealed class WeaponTrail
    {
        sealed class Pt { public Vector3 B, T; public double Age; public Vector3? Dir; public bool Brk; }
        readonly List<Pt> pts = new List<Pt>();
        /// <summary>점 레코드 풀(T50) — 스윙마다 12개까지 새로 만들던 것.</summary>
        readonly Stack<Pt> ptPool = new Stack<Pt>();
        readonly Mesh mesh;
        readonly MeshRenderer mr;
        readonly Material mat;
        readonly FxAnims anims;
        readonly Vector3[] verts = new Vector3[FxRules.TrailSegments * 6];
        readonly Color[] cols = new Color[FxRules.TrailSegments * 6];
        readonly int[] tris = new int[FxRules.TrailSegments * 6];
        Vector3? prevLocal;
        int color = 0xffffff;
        double gain = 1, core = FxRules.TrailCoreDefault;
        int impactSeq;
        public bool On { get; private set; }
        public int Points { get { return pts.Count; } }
        public bool Visible { get { return mr != null && mr.enabled; } }
        public int Color { get { return color; } }
        public double Gain { get { return gain; } }

        public WeaponTrail(Transform parent, FxAnims anims)
        {
            this.anims = anims;
            mesh = new Mesh { name = "weaponTrail" };
            mesh.MarkDynamic();
            for (int i = 0; i < tris.Length; i++) tris[i] = i;
            var go = new GameObject("WeaponTrail");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            mr = go.AddComponent<MeshRenderer>();
            mat = FxUnlitMaterials.Make(0xffffff, 1, false, true, null, true);
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.enabled = false;
        }

        /// <summary>`trailStart(colorHex)` — 무기색(채도 +0.35 · 명도 −0.04) · 증폭·코어를 기본으로.</summary>
        public void Start(int weaponHex)
        {
            color = FxRules.TrailStartColor(weaponHex);
            gain = 1; core = FxRules.TrailCoreDefault;
            prevLocal = null;
            impactSeq++;
            On = true;
        }

        public void Stop() { On = false; }

        Pt NewPt(Vector3 b, Vector3 t, double age, Vector3? dir)
        {
            Pt p = ptPool.Count > 0 ? ptPool.Pop() : new Pt();
            p.B = b; p.T = t; p.Age = age; p.Dir = dir; p.Brk = false;
            return p;
        }

        void DropFirst() { ptPool.Push(pts[0]); pts.RemoveAt(0); }

        /// <summary>`trailImpact(tier)` — normal/crit/kill.</summary>
        public void Impact(string tier)
        {
            var T = FxRules.TrailTierOf(tier);
            color = T.Color; gain = T.Gain; core = T.Core;
            int seq = ++impactSeq;
            anims.Add(FxRules.TrailImpactRelax, k =>
            {
                if (seq != impactSeq) return;
                gain = T.Gain + (1 - T.Gain) * k;
                core = T.Core + (FxRules.TrailCoreDefault - T.Core) * k;
            });
        }

        /// <summary>`updateTrail(dt)` — weaponMount 의 로컬 +y 가 날 방향(HeroRig 파지 규약).</summary>
        public void Update(float dt, Transform weaponMount, string shape, Transform heroRoot)
        {
            foreach (var p in pts) p.Age += dt;
            while (pts.Count > 0 && pts[0].Age >= FxRules.TrailLife) DropFirst();
            if (On && weaponMount != null && weaponMount.gameObject.activeInHierarchy)
            {
                double tipLen = FxRules.TrailTip(shape);
                Vector3 b = weaponMount.TransformPoint(new Vector3(0, (float)FxRules.TrailBase, 0));
                Vector3 t = weaponMount.TransformPoint(new Vector3(0, (float)tipLen, 0));
                Vector3 lt = heroRoot != null ? heroRoot.InverseTransformPoint(t) : t;
                bool swung = !prevLocal.HasValue || Vector3.Distance(lt, prevLocal.Value) > FxRules.TrailMinStep;
                prevLocal = lt;
                if (swung)
                {
                    Pt last = pts.Count > 0 ? pts[pts.Count - 1] : null;
                    if (last != null && last.Dir.HasValue)
                    {
                        Vector3 dirNew = t - last.T;
                        if (Vector3.Dot(dirNew, last.Dir.Value) < 0) { last.Brk = true; last = null; }
                    }
                    if (last != null)
                    {
                        int n = Math.Min(FxRules.TrailInterpMax, (int)Math.Floor(Vector3.Distance(last.T, t) / FxRules.TrailInterpStep));
                        for (int j = 1; j <= n; j++)
                        {
                            float k = (float)j / (n + 1);
                            pts.Add(NewPt(Vector3.Lerp(last.B, b, k), Vector3.Lerp(last.T, t, k), last.Age * (1 - k), null));
                        }
                    }
                    pts.Add(NewPt(b, t, 0, last != null ? (Vector3?)(t - last.T) : null));
                    while (pts.Count > FxRules.TrailSegments - 1) DropFirst();
                }
            }
            if (pts.Count < 2) { mr.enabled = false; return; }
            mr.enabled = true;
            Color baseC = VoxelMaterials.ToColor(color);
            int vi = 0;
            for (int i = 0; i < pts.Count - 1 && vi + 6 <= verts.Length; i++)
            {
                Pt p0 = pts[i], p1 = pts[i + 1];
                if (p0.Brk) continue;
                double f0 = Math.Max(0, 1 - p0.Age / FxRules.TrailLife), f1 = Math.Max(0, 1 - p1.Age / FxRules.TrailLife);
                Color c0 = Col(baseC, f0), c1 = Col(baseC, f1), cb = Col(baseC, 0);
                verts[vi] = p0.B; cols[vi++] = cb;
                verts[vi] = p0.T; cols[vi++] = c0;
                verts[vi] = p1.T; cols[vi++] = c1;
                verts[vi] = p0.B; cols[vi++] = cb;
                verts[vi] = p1.T; cols[vi++] = c1;
                verts[vi] = p1.B; cols[vi++] = cb;
            }
            for (int i = vi; i < verts.Length; i++) { verts[i] = vi > 0 ? verts[0] : Vector3.zero; cols[i] = new Color(0, 0, 0, 0); }
            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetTriangles(tris, 0, false);
            mesh.RecalculateBounds();
        }

        Color Col(Color baseC, double f)
        {
            double ff = f < 0 ? 0 : f > 1 ? 1 : f;
            double mix = Math.Pow(ff, FxRules.TrailCorePow) * core;
            double a = Math.Pow(ff, FxRules.TrailFadePow) * FxRules.TrailAlpha * gain;
            return new Color((float)(baseC.r + (1 - baseC.r) * mix), (float)(baseC.g + (1 - baseC.g) * mix), (float)(baseC.b + (1 - baseC.b) * mix), (float)Math.Max(0, Math.Min(1, a)));
        }

        public void Destroy()
        {
            if (mr != null) UnityEngine.Object.Destroy(mr.gameObject);
            UnityEngine.Object.Destroy(mat);
            UnityEngine.Object.Destroy(mesh);
        }
    }
}
