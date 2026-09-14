using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Forge.Core.Render;

namespace Forge.Game.Render
{
    /// <summary>
    /// T330 1회차 — 파츠 하나의 ID 번호. **생성 시각에 굳는다**(정본 `idMatFor` 878~890 · 모델 행렬 해시는 걷기마다 흔들려 선이 떨린다).
    /// 클론의 복셀 몹은 파츠마다 렌더러 하나라(T4 `VoxelMob.Build` · `VoxelPartTag` 옆) 번호는 렌더러에 붙이면 되고 정점 채널은 필요 없다.
    /// 살아 있는 태그는 <see cref="EdgePartId.Live"/> 에 모여 2회차의 ID 패스가 그것만 돈다(정본 `ID_LAYER` 자리).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EdgePartIdTag : MonoBehaviour
    {
        /// <summary>1~65535 · 0 은 배경 키라 절대 안 붙는다.</summary>
        public int Id;
        public Renderer Target;
        /// <summary>ID 보조 패스가 세우는 쌍둥이 렌더러(같은 메시 · 공유 ID 재질 + 번호 MPB · 보조 카메라가 그리는 순간에만 켜진다) — <see cref="EdgeIdPass"/>.</summary>
        public MeshRenderer Twin;

        // 🚨 T350 — 아래 셋은 **프레임마다 다시 찾지 않으려고** 태그가 쥔다.
        //    `EdgeIdPass.Sync` 는 `LateUpdate` 마다 살아 있는 파츠 전부를 훑으므로, 거기서 `GetComponent` 를 부르거나
        //    `sharedMaterials`(게터가 배열을 **새로 만든다**)를 읽으면 부하 장면(렌더러 705)에서 프레임당
        //    GetComponent 1410 번 + 배열 705 개가 된다 — §1 «Update 에서 GetComponent 금지 · 프레임당 GC 할당 0» 위반이다.
        /// <summary>대상의 메시 필터(한 번만 찾는다 · 없으면 null).</summary>
        public MeshFilter TargetFilter;
        /// <summary>쌍둥이의 메시 필터(세울 때 잡는다).</summary>
        public MeshFilter TwinFilter;
        /// <summary>쌍둥이에 마지막으로 얹은 메시 — 이것이 바뀐 프레임에만 재질 배열을 다시 만든다.</summary>
        public Mesh TwinMesh;

        private void OnEnable() { EdgePartId.Live.Add(this); }
        private void OnDisable() { EdgePartId.Live.Remove(this); }
        private void OnDestroy() { Twin = null; }
    }

    public static class EdgePartId
    {
        /// <summary>ID 패스가 파츠마다 싣는 전역 벡터 이름 — (r, g, 0, 0). 2회차 셰이더 선언과 같아야 한다.</summary>
        public const string IdProp = "_PartId";
        static int seq;
        /// <summary>마지막으로 준 번호(테스트·진단용).</summary>
        public static int LastId { get { return seq; } }
        /// <summary>지금 살아 있는 태그 전부(활성 순서 · 파괴되면 빠진다).</summary>
        public static readonly List<EdgePartIdTag> Live = new List<EdgePartIdTag>();

        /// <summary>
        /// 렌더러에 다음 번호를 굳힌다. 이미 붙어 있으면 그것을 돌려준다(번호는 한 번 정해지면 안 바뀐다).
        /// 🚨 `MaterialPropertyBlock` 에 싣지 않는다 — URP SRP 배처는 MPB 가 붙은 렌더러를 배치에서 빼서 본 패스 드로우콜이 는다(§1 60fps).
        /// 번호는 태그가 쥐고, ID 패스가 `DrawRenderer` 직전에 <see cref="Uniform"/> 을 전역으로 싣는다(정본도 ID 패스 안에서만 재질을 바꿔 끼운다).
        /// </summary>
        public static EdgePartIdTag Tag(Renderer r)
        {
            if (r == null) return null;
            EdgePartIdTag t = r.GetComponent<EdgePartIdTag>();
            if (t != null) return t;
            seq = EdgePartIdRules.Next(seq);
            t = r.gameObject.AddComponent<EdgePartIdTag>();
            t.Id = seq;
            t.Target = r;
            return t;
        }

        /// <summary>번호 → 셰이더 벡터 (r, g, 0, 0)(정본 `uId`).</summary>
        public static Vector4 Uniform(int id)
        {
            double r, g;
            EdgePartIdRules.Encode(id, out r, out g);
            return new Vector4((float)r, (float)g, 0f, 0f);
        }

        /// <summary>정본 930 «불투명 + 깊이 쓰기» — 투명 큐(≥ 3000)이거나 `_ZWrite` 0 이면 ID 를 안 쓴다.</summary>
        public static bool OpaqueDepth(Material m)
        {
            if (m == null) return false;
            bool transparent = m.renderQueue >= (int)RenderQueue.Transparent;
            bool depthWrite = !m.HasProperty("_ZWrite") || m.GetFloat("_ZWrite") != 0f;
            return EdgePartIdRules.OpaqueDepth(transparent, depthWrite);
        }

        static readonly double[] vx = new double[8], vy = new double[8], vz = new double[8];

        /// <summary>
        /// 이 프레임 ID 패스에 이 파츠를 넣는가 — 정본 `tag` 클로저(920~966): 보이는 메시 · 불투명 + 깊이 쓰기 · 투영 AABB 짧은 변 ≥ `id_min_css_px`(코앞이면 무조건).
        /// 꼭짓점 여덟은 **로컬 AABB × 월드 행렬 × 카메라 행렬**(정본 953~956 · 렌더러의 월드 AABB 는 회전한 파츠를 부풀린다).
        /// `cssHeightPx` 는 정본 `clientHeight`(CSS px) 자리 — 클론은 `css_px` 환산 뒤 값을 준다.
        /// </summary>
        public static bool UseId(EdgePartIdTag t, Camera cam, EdgeOutlineSpec s, double cssHeightPx)
        {
            if (t == null || cam == null || t.Target == null) return false;
            Renderer r = t.Target;
            if (!r.enabled || !r.gameObject.activeInHierarchy) return false;
            if (!OpaqueDepth(r.sharedMaterial)) return false;
            // 🚨 T350 — 이 함수도 **프레임마다 파츠 전부**에 대해 돈다(ID 패스의 `Tag()`), 그러니 참조는 태그가 쥔 것을 쓴다.
            if (t.TargetFilter == null) t.TargetFilter = r.GetComponent<MeshFilter>();
            Mesh mesh = t.TargetFilter != null ? t.TargetFilter.sharedMesh : null;
            if (mesh == null) return false;
            Bounds bb = mesh.bounds;
            Matrix4x4 mv = cam.worldToCameraMatrix * r.localToWorldMatrix;
            for (int b = 0; b < 8; b++)
            {
                Vector3 c = new Vector3((b & 1) != 0 ? bb.max.x : bb.min.x, (b & 2) != 0 ? bb.max.y : bb.min.y, (b & 4) != 0 ? bb.max.z : bb.min.z);
                Vector3 v = mv.MultiplyPoint3x4(c);
                vx[b] = v.x; vy[b] = v.y; vz[b] = -v.z;   // 유니티 뷰공간도 -z 가 앞이다(정본 `zv = -c.z`)
            }
            double tanH = System.Math.Tan(cam.fieldOfView * System.Math.PI / 360.0);
            bool near;
            double side = EdgePartIdRules.ShortSideCssPx(vx, vy, vz, cssHeightPx, tanH, cam.aspect, out near);
            return EdgePartIdRules.UseId(s, side, near);
        }
    }
}
