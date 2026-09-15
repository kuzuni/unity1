using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Forge.Core.Data;
using Forge.Core.Ui;
using Forge.Core.Voxel;
using Forge.Game.Render;

namespace Forge.Game.Voxel
{
    /// <summary>정본 `deco.userData.ascendDecorRoot = tier` — 데코 뿌리(몸 메시 뿌리의 자식 «ascend-decor»).</summary>
    public sealed class AscendDecorRoot : MonoBehaviour
    {
        public int Tier;
        /// <summary>얹을 때 잰 몸의 경계 상자(three 좌표 · 뿌리 로컬 · **정지 자세**) 와 그로부터 셈한 r·bandY — 정본도 `applyAscendDecor` 를 부른 그 순간의 bbox 다(그 뒤 관절이 돌아도 데코는 안 옮긴다).</summary>
        public double[] BoundsMin, BoundsMax;
        public double R, BandY;
        /// <summary>얹힌 층의 수(서로 다른 Layer) — 정본 «누적 단조 계약»(probe-ascend-tiers): tier 와 같다.</summary>
        public int LayerCount
        {
            get
            {
                var seen = new HashSet<int>();
                foreach (AscendDecorTag t in GetComponentsInChildren<AscendDecorTag>(true)) seen.Add(t.Layer);
                return seen.Count;
            }
        }
    }

    /// <summary>정본 `m.userData.ascendDecor = layer` — 조각 하나의 층 번호(1 밴드 · 2 가시 · 3 룬 링 · 4 스터드 · 5 왕관).</summary>
    public sealed class AscendDecorTag : MonoBehaviour
    {
        public int Layer;
    }

    /// <summary>
    /// T399 — 펫·탈것 승천 데코(정본 `scene3d.js` 9670 `applyAscendDecor(root, tier, kind)` · 🧊 «승천 데코 = voxel» 화풍 확정 2026-08-20).
    /// 셈은 전부 Core <see cref="AscendDecorPlan"/>(표 `Resources/AscendDecorUi.json`) — 여기는 경계 상자를 재고 조각을 메시·재질로 세울 뿐이다.
    /// ⚠ 정본과 같이 **스케일·배치 전의 원본 메시**에 부른다(bbox 를 로컬 좌표로 쓴다) — 부르는 자리 셋: 전투 펫(<c>PetView</c>) · 전투 탈것(<c>MountView</c>) · 얼굴 썸네일(<c>PetFaces</c>).
    /// </summary>
    public static class AscendDecor
    {
        public const string ResourcePath = "AscendDecorUi";
        public const string RootName = "ascend-decor";

        static AscendDecorSpec spec;
        public static AscendDecorSpec Spec
        {
            get
            {
                if (spec == null)
                {
                    TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
                    if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T399 승천 데코)");
                    spec = AscendDecorSpec.From(MiniJson.ParseObject(ta.text));
                }
                return spec;
            }
        }

        /// <summary>정본 `ascendTier(stars)`.</summary>
        public static int Tier(int stars) { return Spec.Tier(stars); }

        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        static readonly int MetallicId = Shader.PropertyToID("_Metallic");

        /// <summary>
        /// 몸(조립 결과 <paramref name="rig"/>)에 별 수대로 데코를 얹는다 — tier 0 이면 아무것도 안 세우고 null. 이미 얹혀 있으면 걷고 다시 세운다.
        /// 경계 상자는 뿌리 로컬 좌표(스케일 전)로 잰다 — 부모 변환이 걸려 있어도 어긋나지 않는다(정본은 «스케일 전에 부를 것» 만 요구한다).
        /// </summary>
        public static AscendDecorRoot Apply(VoxelMobRig rig, int stars)
        {
            if (rig == null || rig.Root == null) return null;
            Transform root = rig.Root.transform;
            Transform old = root.Find(RootName);
            if (old != null) UnityEngine.Object.Destroy(old.gameObject);
            AscendDecorSpec s = Spec;
            int tier = s.Tier(stars);
            if (tier == 0) return null;
            double[] min, max;
            if (!LocalBoundsThree(root, rig.Meshes, out min, out max)) return null;   // 정본 `box.isEmpty()`
            AscendDecorPlan plan = AscendDecorPlan.Make(s, stars, min, max);
            if (plan.Pieces.Count == 0) return null;

            var go = new GameObject(RootName);
            go.transform.SetParent(root, false);
            AscendDecorRoot deco = go.AddComponent<AscendDecorRoot>();
            deco.Tier = tier; deco.BoundsMin = min; deco.BoundsMax = max; deco.R = plan.R; deco.BandY = plan.BandY;
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            for (int i = 0; i < plan.Pieces.Count; i++)
            {
                AscendDecorPiece p = plan.Pieces[i];
                var pg = new GameObject(p.Name);
                pg.transform.SetParent(go.transform, false);
                pg.transform.localPosition = ThreeSpace.Pos(p.Pos);
                pg.transform.localRotation = ThreeSpace.Rot(p.Rot);
                pg.AddComponent<AscendDecorTag>().Layer = p.Layer;
                var mf = pg.AddComponent<MeshFilter>();
                var mr = pg.AddComponent<MeshRenderer>();
                if (p.IsRune)
                {
                    mf.sharedMesh = RuneMesh(p, linear);
                    mr.sharedMaterial = RuneMaterial(p);
                    mr.shadowCastingMode = ShadowCastingMode.Off;   // 정본 RingGeometry 데칼 — 그림자 없는 투명 글로우
                    mr.receiveShadows = false;
                }
                else
                {
                    var vm = VoxelGeometry.Build(p.Cells, new VoxelBuildOptions
                    {
                        Size = p.Size, Color = p.Color, Center = p.Center, Jitter = p.Jitter, Ao = 1, LeftHanded = true,
                    });
                    mf.sharedMesh = VoxelMob.ToMesh(vm, "ascend/" + p.Name, linear);
                    mr.sharedMaterial = CubeMaterial(s, p);
                    mr.shadowCastingMode = ShadowCastingMode.On;   // 정본 Voxel.build: castShadow · receiveShadow
                    mr.receiveShadows = true;
                    EdgePartId.Tag(mr);   // 윤곽선 파츠 번호(T330) — 몸 파츠와 같은 규약
                }
            }
            return deco;
        }

        /// <summary>뿌리 로컬 좌표의 경계 상자 → three 좌표(z 부호 반전 · 결정 4). 메시가 하나도 없으면 false(정본 `box.isEmpty()`).</summary>
        public static bool LocalBoundsThree(Transform root, IList<MeshFilter> meshes, out double[] min, out double[] max)
        {
            min = null; max = null;
            Vector3 lo = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 hi = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            bool any = false;
            Matrix4x4 toRoot = root.worldToLocalMatrix;
            for (int i = 0; i < meshes.Count; i++)
            {
                MeshFilter mf = meshes[i];
                if (mf == null || mf.sharedMesh == null) continue;
                Bounds b = mf.sharedMesh.bounds;
                Matrix4x4 m = toRoot * mf.transform.localToWorldMatrix;
                for (int c = 0; c < 8; c++)
                {
                    Vector3 corner = new Vector3((c & 1) == 0 ? b.min.x : b.max.x, (c & 2) == 0 ? b.min.y : b.max.y, (c & 4) == 0 ? b.min.z : b.max.z);
                    Vector3 w = m.MultiplyPoint3x4(corner);
                    lo = Vector3.Min(lo, w); hi = Vector3.Max(hi, w);
                    any = true;
                }
            }
            if (!any) return false;
            min = new double[] { lo.x, lo.y, -hi.z };
            max = new double[] { hi.x, hi.y, -lo.z };
            return true;
        }

        /// <summary>정본 `decoMat(hex, emiHex, emiI)` = MeshStandardMaterial{ metalness .5 · roughness .5 · vertexColors · flatShading (+ emissive) } — 공용 복셀 재질(흰 바탕 · 정점색)에 metalness 만 얹는다.</summary>
        static Material CubeMaterial(AscendDecorSpec s, AscendDecorPiece p)
        {
            var mm = new MobMat { Rough = s.Roughness };
            if (p.Emissive >= 0) { mm.Emissive = p.Emissive; mm.EmissiveIntensity = p.EmissiveIntensity; }
            string key = "ascend|" + MobBuilder.MatKey(mm) + "|m" + s.Metalness;
            Material m;
            if (mats.TryGetValue(key, out m) && m != null) return m;
            m = new Material(VoxelMaterials.Get(key, mm));
            m.name = "Voxel " + key;
            if (m.HasProperty(MetallicId)) m.SetFloat(MetallicId, (float)s.Metalness);
            mats[key] = m;
            return m;
        }

        /// <summary>정본 L3 `MeshBasicMaterial{ color: motif, transparent, opacity .5, DoubleSide, depthWrite false }` — 무조명·반투명 공용 재질(정점색 = 모티프색).</summary>
        static Material RuneMaterial(AscendDecorPiece p)
        {
            var raw = new JsonObject();
            raw["basic"] = true;
            var mm = new MobMat { Opacity = p.Opacity, Raw = raw };
            return VoxelMaterials.Get("ascend-rune|" + MobBuilder.MatKey(mm), mm);
        }

        /// <summary>정본 `RingGeometry(inner, outer, segments)` — XY 평면의 고리(회전은 조각의 Rot 이 건다) · 양면(DoubleSide) 이라 삼각형을 두 감김으로 둘 다 넣는다.</summary>
        public static Mesh RuneMesh(AscendDecorPiece p, bool linearColorSpace)
        {
            int n = Math.Max(3, p.RuneSegments);
            var verts = new Vector3[(n + 1) * 2];
            var cols = new Color[verts.Length];
            var norms = new Vector3[verts.Length];
            Color c = VoxelMaterials.ToColor(p.Color);
            if (linearColorSpace) c = c.linear;
            for (int i = 0; i <= n; i++)
            {
                double a = (double)i / n * Math.PI * 2;
                float cx = (float)Math.Cos(a), sy = (float)Math.Sin(a);
                verts[i * 2] = new Vector3(cx * (float)p.RuneInner, sy * (float)p.RuneInner, 0f);
                verts[i * 2 + 1] = new Vector3(cx * (float)p.RuneOuter, sy * (float)p.RuneOuter, 0f);
                cols[i * 2] = c; cols[i * 2 + 1] = c;
                norms[i * 2] = Vector3.forward; norms[i * 2 + 1] = Vector3.forward;
            }
            var tris = new int[n * 12];
            int t = 0;
            for (int i = 0; i < n; i++)
            {
                int a0 = i * 2, b0 = i * 2 + 1, a1 = (i + 1) * 2, b1 = (i + 1) * 2 + 1;
                tris[t++] = a0; tris[t++] = b0; tris[t++] = a1; tris[t++] = b0; tris[t++] = b1; tris[t++] = a1;   // 앞면
                tris[t++] = a0; tris[t++] = a1; tris[t++] = b0; tris[t++] = b0; tris[t++] = a1; tris[t++] = b1;   // 뒷면(DoubleSide)
            }
            var mesh = new Mesh { name = "ascend/rune" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetColors(cols);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>몸에 얹힌 데코 뿌리(없으면 null).</summary>
        public static AscendDecorRoot Of(Transform meshRoot)
        {
            if (meshRoot == null) return null;
            Transform t = meshRoot.Find(RootName);
            return t == null ? null : t.GetComponent<AscendDecorRoot>();
        }
    }
}
