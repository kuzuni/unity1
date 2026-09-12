using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Forge.Core.BattleFx;
using Forge.Core.Voxel;
using Forge.Game.Hero;
using Forge.Game.Voxel;

namespace Forge.Game.Battle
{
    /// <summary>원작 `addAnim(dur, tick(k), done)` — 씬이 프레임마다 민다(T8 은 개체별 시계로 풀었고 T39 의 일회성 연출은 이 목록이 쥔다).</summary>
    public sealed class FxAnims
    {
        sealed class A { public double Dur, T; public Action<double> Tick; public Action Done; }
        readonly List<A> live = new List<A>();
        public int Count { get { return live.Count; } }

        public void Add(double dur, Action<double> tick, Action done = null)
        {
            var a = new A { Dur = Math.Max(1e-6, dur), Tick = tick, Done = done };
            live.Add(a);
            if (tick != null) tick(0);
        }

        /// <summary>정본 update 는 역순으로 돈다 — 같은 규약.</summary>
        public void Step(float dt)
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                A a = live[i];
                a.T += dt;
                double k = Math.Min(1, a.T / a.Dur);
                if (a.Tick != null) a.Tick(k);
                if (k >= 1)
                {
                    live.RemoveAt(i);
                    if (a.Done != null) a.Done();
                }
            }
        }

        public void Clear() { live.Clear(); }
    }

    /// <summary>`Forge/FxUnlit` 재질 공장 — 정본 `MeshBasicMaterial` 의 (color · map · transparent · blending · depthTest · side) 조합.</summary>
    public static class FxUnlitMaterials
    {
        public const string ShaderName = "Forge/FxUnlit";
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int SrcId = Shader.PropertyToID("_SrcBlend"), DstId = Shader.PropertyToID("_DstBlend"), ZTestId = Shader.PropertyToID("_ZTest"), CullId = Shader.PropertyToID("_Cull");
        static Shader shader;

        public static Shader Find()
        {
            if (shader == null) shader = Shader.Find(ShaderName) ?? Shader.Find(VoxelMaterials.UnlitShader) ?? Shader.Find("Unlit/Color");
            return shader;
        }

        public static Material Make(int hex, double opacity, bool additive, bool depthTest, Texture tex = null, bool doubleSide = true)
        {
            var m = new Material(Find());
            m.name = "fx " + hex.ToString("x6") + (additive ? " add" : "");
            var c = VoxelMaterials.ToColor(hex); c.a = (float)opacity;
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c); else m.color = c;
            if (m.HasProperty(SrcId)) m.SetFloat(SrcId, (float)BlendMode.SrcAlpha);
            if (m.HasProperty(DstId)) m.SetFloat(DstId, (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            if (m.HasProperty(ZTestId)) m.SetFloat(ZTestId, (float)(depthTest ? CompareFunction.LessEqual : CompareFunction.Always));
            if (m.HasProperty(CullId)) m.SetFloat(CullId, (float)(doubleSide ? CullMode.Off : CullMode.Back));
            if (tex != null) m.mainTexture = tex;
            m.renderQueue = (int)RenderQueue.Transparent + (depthTest ? 0 : 10);
            return m;
        }

        public static void SetOpacity(Material m, double a)
        {
            if (m == null) return;
            Color c = m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : m.color;
            c.a = (float)a;
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c); else m.color = c;
        }

        public static void SetColor(Material m, int hex, double a)
        {
            if (m == null) return;
            var c = VoxelMaterials.ToColor(hex); c.a = (float)a;
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c); else m.color = c;
        }
    }

    /// <summary>
    /// 접점·처치·등장 임팩트 프리미티브(정본 `impactFlare`·`impactSpikes`·`impactRing`·`expandRing`·`flashLight`·`scorchDecal`·`swoosh`·`bossWarnPillar`).
    /// 좌표 인자는 전부 three 좌표(<see cref="ThreeSpace.Pos"/> 로 옮긴다). 카메라를 향한 쿼드는 `LookAt(camera)` · 지면 원반은 눕힌다.
    /// 텍스처(플레어 128² · 그을음 64²)는 정본 캔버스 그라디언트를 픽셀로 굽는다 — 외부 그림 없음.
    /// </summary>
    public sealed class ImpactFx
    {
        public readonly Transform Parent;
        public readonly FxAnims Anims;
        public Camera Cam;
        public int Live { get; private set; }

        static Texture2D flareTex, scorchTex;
        static Mesh quad, ringMesh, ringThickMesh, pillarMesh;
        readonly List<Light> lights = new List<Light>();
        readonly List<int> lightLease = new List<int>();
        int lightSeq;

        public ImpactFx(Transform parent, FxAnims anims, Camera cam) { Parent = parent; Anims = anims; Cam = cam; }

        static Mesh Quad { get { return quad ?? (quad = HeroMeshes.Quad(1, 1, 0xffffff)); } }

        /// <summary>`flareTex()` — 방사 그라디언트 pow(1−r, 3) + 십자 스파이크(가로세로 62 · 대각 40).</summary>
        public static Texture2D FlareTex
        {
            get
            {
                if (flareTex != null) return flareTex;
                const int N = 128; const double R = 64;
                flareTex = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "flare128", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[N * N];
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        double dx = x + 0.5 - R, dy = y + 0.5 - R;
                        double r = Math.Min(1, Math.Sqrt(dx * dx + dy * dy) / R);
                        double a = Math.Pow(1 - r, 3);
                        // 십자·대각 스파이크(lighter 합성 = 더하기)
                        foreach (double rot in new[] { 0, Math.PI / 2, Math.PI / 4, -Math.PI / 4 })
                        {
                            double len = (rot == 0 || rot == Math.PI / 2) ? 62 : 40;
                            double c = Math.Cos(rot), s = Math.Sin(rot);
                            double u = dx * c + dy * s, v = -dx * s + dy * c;
                            if (Math.Abs(v) <= 2.2 && Math.Abs(u) <= len) a += 0.85 * (1 - Math.Abs(u) / len);
                        }
                        px[y * N + x] = new Color32(255, 255, 255, (byte)Math.Round(Math.Min(1, a) * 255));
                    }
                flareTex.SetPixels32(px);
                flareTex.Apply(false, false);
                return flareTex;
            }
        }

        /// <summary>`scorchTex()` — 64² 방사 그라디언트(.95 → .7@55% → 0).</summary>
        public static Texture2D ScorchTex
        {
            get
            {
                if (scorchTex != null) return scorchTex;
                const int N = 64; const double R = 32;
                scorchTex = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "scorch64", wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[N * N];
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        double dx = x + 0.5 - R, dy = y + 0.5 - R;
                        double r = Math.Min(1, Math.Sqrt(dx * dx + dy * dy) / R);
                        double a = r < 0.55 ? 0.95 + (0.7 - 0.95) * (r / 0.55) : 0.7 * (1 - (r - 0.55) / 0.45);
                        px[y * N + x] = new Color32(255, 255, 255, (byte)Math.Round(Math.Max(0, a) * 255));
                    }
                scorchTex.SetPixels32(px);
                scorchTex.Apply(false, false);
                return scorchTex;
            }
        }

        /// <summary>`pixelRingGeo(thick)` — XY 평면 격자 12칸 계단 링(정점색 흰색 · jitter 0.08 · ao 0).</summary>
        public static Mesh PixelRing(bool thick)
        {
            Mesh m = thick ? ringThickMesh : ringMesh;
            if (m != null) return m;
            var cells = new List<VoxelCell>();
            int H = FxRules.PxRingHalf;
            for (int gx = -H; gx <= H; gx++)
                for (int gy = -H; gy <= H; gy++)
                {
                    double d = Math.Sqrt(gx * gx + gy * gy);
                    if (d <= FxRules.PxRingOuter && d >= (thick ? FxRules.PxRingInnerThick : FxRules.PxRingInner)) cells.Add(new VoxelCell(gx, gy, 0, 0xffffff));
                }
            var o = VoxelBuildOptions.Default; o.Size = FxRules.PxRingCell; o.Jitter = 0.08; o.Ao = 0; o.Center = false; o.LeftHanded = true;
            m = VoxelMob.ToMesh(VoxelGeometry.Build(cells, o), thick ? "pixelRingThick" : "pixelRing", QualitySettings.activeColorSpace == ColorSpace.Linear);
            if (thick) ringThickMesh = m; else ringMesh = m;
            return m;
        }

        /// <summary>`bossWarnPillar` 의 복셀 지구라트 통(캐시) — 아래 4층 반폭 2 · 위 4층 반폭 1 · 층별 밝기 (1−y/7)^1.8 · 원점을 밑동으로.</summary>
        public static Mesh PillarMesh
        {
            get
            {
                if (pillarMesh != null) return pillarMesh;
                var cells = new List<VoxelCell>();
                for (int y = 0; y < FxRules.PillarLayers; y++)
                {
                    int r = y < FxRules.PillarWideLayers ? FxRules.PillarWideR : FxRules.PillarNarrowR;
                    double b = Math.Pow(1 - (double)y / (FxRules.PillarLayers - 1), FxRules.PillarBrightPow);
                    int g = (int)Math.Round(b * 255);
                    int c = (g << 16) | (g << 8) | g;
                    for (int x = -r; x <= r; x++)
                        for (int z = -r; z <= r; z++)
                            if (Math.Max(Math.Abs(x), Math.Abs(z)) == r) cells.Add(new VoxelCell(x, y, z, c));
                }
                var o = VoxelBuildOptions.Default; o.Size = FxRules.PillarCell; o.Jitter = FxRules.PillarJitter; o.Ao = 0; o.Center = true; o.LeftHanded = true;
                VoxelMesh vm = VoxelGeometry.Build(cells, o);
                for (int i = 1; i < vm.Positions.Length; i += 3) vm.Positions[i] += (float)FxRules.PillarLift;
                pillarMesh = VoxelMob.ToMesh(vm, "bossWarnPillar", QualitySettings.activeColorSpace == ColorSpace.Linear);
                return pillarMesh;
            }
        }

        MeshRenderer Spawn(string name, Mesh mesh, Material mat, Vector3 threePos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Parent, false);
            go.transform.position = ThreeSpace.Pos(threePos.x, threePos.y, threePos.z);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            Live++;
            return mr;
        }

        void Kill(MeshRenderer mr)
        {
            if (mr == null) return;
            Live--;
            UnityEngine.Object.Destroy(mr.sharedMaterial);
            UnityEngine.Object.Destroy(mr.gameObject);
        }

        void FaceCamera(Transform t)
        {
            if (Cam != null) t.LookAt(Cam.transform.position);
        }

        /// <summary>`impactFlare(pos, colorHex, size, dur, spin, peak)` — 카메라를 향한 가산 쿼드 · 첫 프레임부터 0.9 → 1.35 배.</summary>
        public void Flare(Vector3 pos, int hex, double size, double dur, double spin, double peak = FxRules.FlareDefaultPeak)
        {
            var mr = Spawn("flare", Quad, FxUnlitMaterials.Make(hex, peak, true, false, FlareTex), pos);
            Transform t = mr.transform;
            FaceCamera(t);
            t.Rotate(0, 0, (float)(spin * Mathf.Rad2Deg), Space.Self);
            t.localScale = Vector3.one * (float)(size * FxRules.FlareStart);
            Material m = mr.sharedMaterial;
            Anims.Add(dur, k =>
            {
                if (t == null) return;
                t.localScale = Vector3.one * (float)(size * (FxRules.FlareStart + FxRules.FlareGrow * k));
                FxUnlitMaterials.SetOpacity(m, peak * FxRules.FadeSq(k));
            }, () => Kill(mr));
        }

        /// <summary>`impactSpikes(pos, count, colorHex, size, dur, crit)` — 방사형 스파이크(뻗었다가 뿌리부터 사라진다).</summary>
        public void Spikes(Vector3 pos, int count, int hex, double size, double dur, bool crit)
        {
            int n = Math.Max(3, count);
            double baseA = UnityEngine.Random.value * Math.PI;
            double op = FxRules.SpikeOpacity(crit);
            double w = FxRules.SpikeWidth(crit, size);
            for (int i = 0; i < n; i++)
            {
                double ang = baseA + (double)i / n * Math.PI * 2 + (UnityEngine.Random.value * 2 - 1) * FxRules.SpikeJitter;
                double len = size * (FxRules.SpikeLenMin + UnityEngine.Random.value * (FxRules.SpikeLenMax - FxRules.SpikeLenMin));
                var mr = Spawn("spike", Quad, FxUnlitMaterials.Make(hex, op, true, false, FlareTex), pos);
                Transform t = mr.transform;
                FaceCamera(t);
                t.Rotate(0, 0, (float)(ang * Mathf.Rad2Deg), Space.Self);
                Vector3 origin = t.position;
                Vector3 up = t.up;
                t.localScale = new Vector3((float)w, (float)(len * FxRules.SpikeStartLen), 1);
                Material m = mr.sharedMaterial;
                Anims.Add(dur, k =>
                {
                    if (t == null) return;
                    double g = 1 - Math.Pow(1 - k, FxRules.SpikeGrowPow);
                    t.localScale = new Vector3((float)(w * (1 - FxRules.SpikeShrinkW * k)), (float)(len * (FxRules.SpikeStartLen + FxRules.SpikeGrowLen * g)), 1);
                    t.position = origin + up * (float)(len * (FxRules.SpikeOffset0 + FxRules.SpikeOffsetK * g));
                    FxUnlitMaterials.SetOpacity(m, op * FxRules.FadeSq(k));
                }, () => Kill(mr));
            }
        }

        /// <summary>`impactRing(pos, colorHex, size, dur, crit)` — 카메라를 향한 픽셀 계단 링(끝까지 퍼진다).</summary>
        public void Ring(Vector3 pos, int hex, double size, double dur, bool crit)
        {
            double op = FxRules.RingOpacity(crit);
            var mr = Spawn("impactRing", PixelRing(crit), FxUnlitMaterials.Make(hex, op, true, false), pos);
            Transform t = mr.transform;
            FaceCamera(t);
            t.Rotate(0, 0, UnityEngine.Random.value * 90f, Space.Self);
            double F = FxRules.PxRingF, grow = FxRules.RingGrow(crit);
            t.localScale = Vector3.one * (float)(size * FxRules.RingStart * F);
            Material m = mr.sharedMaterial;
            Anims.Add(dur, k =>
            {
                if (t == null) return;
                double g = 1 - Math.Pow(1 - k, FxRules.RingGrowPow);
                t.localScale = Vector3.one * (float)(size * (FxRules.RingStart + g * grow) * F);
                FxUnlitMaterials.SetOpacity(m, op * FxRules.FadeSq(k));
            }, () => Kill(mr));
        }

        /// <summary>`expandRing(pos, color, maxR)` — 지면에 눕는 충격파(어두운 밑링 + 색 링 · 0.17초).</summary>
        public void ExpandRing(Vector3 pos, int hex, double maxR)
        {
            var under = Spawn("shockUnder", PixelRing(false), FxUnlitMaterials.Make(FxRules.ExpandUnderColor, 0.5, false, true), new Vector3(pos.x, (float)FxRules.ExpandUnderY, pos.z));
            var ring = Spawn("shockRing", PixelRing(false), FxUnlitMaterials.Make(hex, 0.9, false, true), new Vector3(pos.x, (float)FxRules.ExpandRingY, pos.z));
            foreach (var r in new[] { under, ring }) r.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Material mu = under.sharedMaterial, mr2 = ring.sharedMaterial;
            Transform tu = under.transform, tr = ring.transform;
            Anims.Add(FxRules.ExpandDur, k =>
            {
                if (tr == null || tu == null) return;
                double s = FxRules.ExpandScale(k, maxR);
                tr.localScale = Vector3.one * (float)s;
                tu.localScale = Vector3.one * (float)(s * FxRules.ExpandUnderScale);
                double a = FxRules.FadeSq(k);
                FxUnlitMaterials.SetOpacity(mr2, FxRules.ExpandRingOpacity * a);
                FxUnlitMaterials.SetOpacity(mu, FxRules.ExpandUnderOpacity * a);
            }, () => { Kill(under); Kill(ring); });
        }

        /// <summary>`fxLight` 풀 — 4개 · 임차 토큰으로 겹침을 가른다.</summary>
        Light Lease(int hex, out int token)
        {
            if (lights.Count == 0)
                for (int i = 0; i < 4; i++)
                {
                    var go = new GameObject("fxLight " + i);
                    go.transform.SetParent(Parent, false);
                    var l = go.AddComponent<Light>();
                    l.type = LightType.Point; l.intensity = 0; l.shadows = LightShadows.None;
                    lights.Add(l); lightLease.Add(0);
                }
            int pick = -1;
            for (int i = 0; i < lights.Count; i++) if (lightLease[i] == 0) { pick = i; break; }
            if (pick < 0) { pick = 0; for (int i = 1; i < lights.Count; i++) if (lightLease[i] < lightLease[pick]) pick = i; }
            token = ++lightSeq;
            lightLease[pick] = token;
            Light L = lights[pick];
            L.color = VoxelMaterials.ToColor(hex);
            L.intensity = 0;
            L.range = (float)FxRules.LightDistance;
            return L;
        }

        /// <summary>`flashLight(pos, colorHex, dur)` — 순간 점광(2.8 → 0).</summary>
        public void FlashLight(Vector3 pos, int hex, double dur = FxRules.LightDefaultDur)
        {
            int token;
            Light L = Lease(hex, out token);
            int idx = lights.IndexOf(L);
            L.transform.position = ThreeSpace.Pos(pos.x, pos.y + FxRules.LightDy, pos.z + FxRules.LightDz);
            Anims.Add(dur, k => { if (lightLease[idx] == token) L.intensity = (float)(FxRules.LightPeak * (1 - k)); },
                () => { if (lightLease[idx] == token) { L.intensity = 0; lightLease[idx] = 0; } });
        }

        /// <summary>`scorchDecal(pos, radius, dur)` — 지면 그을음(유일한 어두운 값 · 밝은 파편이 꺼진 뒤에도 남는다).</summary>
        public void Scorch(Vector3 pos, double radius, double dur)
        {
            var mr = Spawn("scorch", Quad, FxUnlitMaterials.Make(FxRules.ScorchColor, 0, false, true, ScorchTex), new Vector3(pos.x, (float)FxRules.ScorchY, pos.z));
            Transform t = mr.transform;
            t.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Material m = mr.sharedMaterial;
            Anims.Add(dur, k =>
            {
                if (t == null) return;
                FxUnlitMaterials.SetOpacity(m, FxRules.ScorchOpacity(k));
                t.localScale = Vector3.one * (float)(radius * 2 * FxRules.ScorchScale(k));
            }, () => Kill(mr));
        }

        /// <summary>`swoosh(colorHex)` — 영웅 쪽 반투명 호(토러스 0.55π) · 0.1초 동안 커지며 휘둘러진다.</summary>
        public void Swoosh(int hex, double heroX)
        {
            var mr = Spawn("swoosh", ArcMesh, FxUnlitMaterials.Make(hex, FxRules.SwooshOpacity, false, true), new Vector3((float)(heroX + FxRules.SwooshDx), (float)FxRules.SwooshY, (float)FxRules.SwooshZ));
            Transform t = mr.transform;
            double[] rot = { 0, FxRules.SwooshRotY, FxRules.SwooshRotZ };
            ThreeSpace.Apply(t, rot);
            Material m = mr.sharedMaterial;
            Anims.Add(FxRules.SwooshDur, k =>
            {
                if (t == null) return;
                t.localScale = Vector3.one * (float)(1 + k * FxRules.SwooshGrow);
                rot[2] = FxRules.SwooshRotZ - k * FxRules.SwooshSweep;
                ThreeSpace.Apply(t, rot);
                FxUnlitMaterials.SetOpacity(m, FxRules.SwooshPeak * (1 - k));
            }, () => Kill(mr));
        }

        static Mesh arcMesh;
        /// <summary>토러스 호(R 0.34 · 튜브 0.03 · 0.55π) 를 XY 평면 리본 6×16 으로 — three 프레임(θ=0 이 +X · CCW).</summary>
        static Mesh ArcMesh
        {
            get
            {
                if (arcMesh != null) return arcMesh;
                const int seg = 16, tube = 6;
                var verts = new List<Vector3>(); var tris = new List<int>(); var cols = new List<Color>();
                for (int i = 0; i <= seg; i++)
                {
                    double th = FxRules.SwooshArc * i / seg;
                    for (int j = 0; j <= tube; j++)
                    {
                        double ph = Math.PI * 2 * j / tube;
                        double r = FxRules.SwooshR + FxRules.SwooshTube * Math.Cos(ph);
                        verts.Add(new Vector3((float)(r * Math.Cos(th)), (float)(r * Math.Sin(th)), (float)(-FxRules.SwooshTube * Math.Sin(ph))));
                        cols.Add(Color.white);
                    }
                }
                int W = tube + 1;
                for (int i = 0; i < seg; i++)
                    for (int j = 0; j < tube; j++)
                    {
                        int a = i * W + j, b = a + W;
                        tris.Add(a); tris.Add(b); tris.Add(a + 1);
                        tris.Add(a + 1); tris.Add(b); tris.Add(b + 1);
                    }
                arcMesh = new Mesh { name = "swooshArc" };
                arcMesh.SetVertices(verts); arcMesh.SetColors(cols); arcMesh.SetTriangles(tris, 0);
                arcMesh.RecalculateNormals(); arcMesh.RecalculateBounds();
                return arcMesh;
            }
        }

        /// <summary>`bossWarnPillar(px)` — 붉은 가산 복셀 기둥(opacity 0 에서 시작 · 호출부가 박에 맞춰 밝힌다).</summary>
        public MeshRenderer Pillar(double px)
        {
            var mr = Spawn("bossWarnPillar", PillarMesh, FxUnlitMaterials.Make(FxRules.BossPillarColor, 0, true, true, null, false), new Vector3((float)px, 0, 0));
            mr.transform.localScale = new Vector3(1, (float)FxRules.PillarScale0, 1);
            return mr;
        }

        public void Remove(MeshRenderer mr) { Kill(mr); }
    }

    /// <summary>
    /// 적 한 마리의 몸 재질(T39) — T4 가 준 공유 재질을 `Forge/EnemyBody`(림 + 플래시 + 디졸브) 개체 재질로 갈아 끼운다(정본 `applyRimLight(ENEMY_RIM)` → `installDissolve`).
    /// 투명 파츠(젤리 0.82 · 막날개)는 원 재질을 두고 디졸브 때 불투명도로 접는다(정본 «훅이 안 걸린 재질» 갈래). 플래시(`flashMesh`)는 emissive 가산 — 파츠 명도(정점색 평균)에
    /// 비례시키는 shapeK 규약 그대로. 처치 디졸브(`setDissolve`)는 f→임계값 매핑을 <see cref="FxRules.DissolveValue"/> 가 쥔다.
    /// </summary>
    public sealed class EnemyBodyFx
    {
        public const string ShaderName = "Forge/EnemyBody";
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"), TintId = Shader.PropertyToID("_Tint"), SmoothId = Shader.PropertyToID("_Smoothness"), MetalId = Shader.PropertyToID("_Metallic");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor"), FlashId = Shader.PropertyToID("_Flash"), FlashColorId = Shader.PropertyToID("_FlashColor"), DissolveId = Shader.PropertyToID("_Dissolve");
        static readonly int RimColorId = Shader.PropertyToID("_RimColor"), RimStrId = Shader.PropertyToID("_RimStr"), RimPowId = Shader.PropertyToID("_RimPow"), RimDarkId = Shader.PropertyToID("_RimDark"), RimDarkStrId = Shader.PropertyToID("_RimDarkStr"), RimDarkPowId = Shader.PropertyToID("_RimDarkPow");
        static Shader shader;

        public static Shader Find() { return shader ?? (shader = Shader.Find(ShaderName)); }
        public static bool Available { get { Shader s = Find(); return s != null && s.isSupported; } }

        sealed class Part { public MeshRenderer R; public Material M; public double Lum; public double BaseA; public bool Lit; }
        readonly List<Part> parts = new List<Part>();
        readonly FxAnims anims;
        int flashSeq;
        public int LitCount { get { int n = 0; foreach (var p in parts) if (p.Lit) n++; return n; } }
        public double Dissolve { get; private set; }
        public double FlashNow { get; private set; }

        public EnemyBodyFx(FxAnims anims) { this.anims = anims; }

        /// <summary>리그의 렌더러 전부를 등록한다(레갈리아를 나중에 더하면 <see cref="Add"/>).</summary>
        public void Install(IEnumerable<MeshRenderer> renderers)
        {
            foreach (var r in renderers) Add(r);
        }

        public void Add(MeshRenderer r)
        {
            if (r == null) return;
            Material src = r.sharedMaterial;
            if (src == null) return;
            Color baseC = src.HasProperty(BaseColorId) ? src.GetColor(BaseColorId) : src.color;
            bool transparent = baseC.a < 0.999f || src.renderQueue >= (int)RenderQueue.Transparent;
            if (transparent || !Available)
            {
                parts.Add(new Part { R = r, M = src, Lum = 1, BaseA = baseC.a, Lit = false });
                return;
            }
            var m = new Material(Find());
            m.name = "EnemyBody " + src.name;
            m.SetColor(BaseColorId, new Color(baseC.r, baseC.g, baseC.b, 1));
            m.SetColor(TintId, Color.white);
            if (src.HasProperty(SmoothId)) m.SetFloat(SmoothId, src.GetFloat(SmoothId));
            if (src.HasProperty(MetalId)) m.SetFloat(MetalId, src.GetFloat(MetalId));
            m.SetColor(EmissionId, src.HasProperty(EmissionId) && src.IsKeywordEnabled("_EMISSION") ? src.GetColor(EmissionId) : Color.black);
            m.SetColor(RimColorId, VoxelMaterials.ToColor(FxRules.RimColor));
            m.SetFloat(RimStrId, (float)FxRules.RimStrength);
            m.SetFloat(RimPowId, (float)FxRules.RimPower);
            m.SetColor(RimDarkId, VoxelMaterials.ToColor(FxRules.RimDark));
            m.SetFloat(RimDarkStrId, (float)FxRules.EnemyRimDarkStrength);
            m.SetFloat(RimDarkPowId, (float)FxRules.EnemyRimDarkPower);
            m.SetFloat(FlashId, 0);
            m.SetFloat(DissolveId, 0);
            r.sharedMaterial = m;
            parts.Add(new Part { R = r, M = m, Lum = MeshLuma(r), BaseA = 1, Lit = true });
        }

        /// <summary>파츠 albedo 명도 — 팔레트는 정점색이라 메시 정점색 평균으로 잰다(정본 `litLum`).</summary>
        static double MeshLuma(MeshRenderer r)
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return 1;
            var cols = mf.sharedMesh.colors;
            if (cols == null || cols.Length == 0) return 1;
            double acc = 0;
            for (int i = 0; i < cols.Length; i++) acc += FxRules.Luma(cols[i].r, cols[i].g, cols[i].b);
            return acc / cols.Length;
        }

        /// <summary>`bossMaterialTell` — 몸 재질 배율(레갈리아는 자기 색을 그대로 쓴다 · 부르는 순서가 그것을 보장한다).</summary>
        public void SetTint(int hex)
        {
            foreach (var p in parts) if (p.Lit) p.M.SetColor(TintId, VoxelMaterials.ToColor(hex));
        }

        /// <summary>`flashMesh(m, peak, dur, color, olK, shapeK, emHex)` — emissive 가산 · 연타는 최신 것만.</summary>
        public void Flash(double peak, double dur, int emHex, double shapeK)
        {
            int seq = ++flashSeq;
            Color c = VoxelMaterials.ToColor(emHex);
            foreach (var p in parts) if (p.Lit) { p.M.SetColor(FlashColorId, c); p.M.SetFloat(FlashId, (float)(peak * FxRules.FlashEmScale(shapeK, p.Lum))); }
            FlashNow = peak;
            anims.Add(dur, k =>
            {
                if (flashSeq != seq) return;
                FlashNow = peak * (1 - k);
                foreach (var p in parts) if (p.Lit && p.M != null) p.M.SetFloat(FlashId, (float)(peak * FxRules.FlashEmScale(shapeK, p.Lum) * (1 - k)));
            }, () => { if (flashSeq == seq) { FlashNow = 0; foreach (var p in parts) if (p.Lit && p.M != null) p.M.SetFloat(FlashId, 0); } });
        }

        /// <summary>`setDissolve(root, f)`.</summary>
        public void SetDissolve(double f)
        {
            Dissolve = f;
            double v = FxRules.DissolveValue(f);
            foreach (var p in parts)
            {
                if (p.M == null) continue;
                if (p.Lit) p.M.SetFloat(DissolveId, (float)v);
                else if (p.R != null)
                {
                    // 훅이 안 걸린(투명) 재질은 개체 사본으로 불투명도만 접는다
                    if (p.R.sharedMaterial == p.M) { p.M = new Material(p.M); p.R.sharedMaterial = p.M; }
                    Color c = p.M.HasProperty(BaseColorId) ? p.M.GetColor(BaseColorId) : p.M.color;
                    c.a = (float)(p.BaseA * Math.Max(0, 1 - f));
                    if (p.M.HasProperty(BaseColorId)) p.M.SetColor(BaseColorId, c); else p.M.color = c;
                }
            }
        }

        public void Dispose()
        {
            flashSeq++;
            foreach (var p in parts) if (p.Lit && p.M != null) UnityEngine.Object.Destroy(p.M);
            parts.Clear();
        }
    }
}
