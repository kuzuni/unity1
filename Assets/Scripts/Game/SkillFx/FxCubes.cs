using UnityEngine;
using UnityEngine.Rendering;
using Forge.Game.Battle;
using Forge.Game.Voxel;

namespace Forge.Game.SkillFx
{
    /// <summary>
    /// 연출용 단위 큐브(원작 `fxGeo('box',1,1,1)` + `MeshBasicMaterial`) — 블록 스트림·트레일·시전 모트·차지 코어가 쓴다.
    /// 메시는 내장 큐브 하나를 공유하고 재질은 개체마다(불투명도를 매 프레임 바꾼다) — T74: 조합(투명·가산)별 풀(<see cref="FxMaterials.Take"/>)에서 꺼내고 지울 때 돌려준다(시전마다 새 Material 을 만들고 버리던 것 · 런 113·118 실측 ±135).
    /// </summary>
    public sealed class FxCube
    {
        public readonly GameObject G;
        public readonly Transform T;
        public readonly Material Mat;
        /// <summary>풀 키(<see cref="FxMaterials.RecipeKey"/>) — 돌려줄 때 같은 칸으로.</summary>
        public readonly int MatKey;
        readonly double[] rot = { 0, 0, 0 };
        public Vector3 Pos { get; private set; }
        public bool Gone { get; private set; }

        internal FxCube(GameObject g, Material m, int matKey) { G = g; T = g.transform; Mat = m; MatKey = matKey; }

        public void SetPos(Vector3 threePos) { Pos = threePos; T.localPosition = ThreeSpace.Pos(threePos.x, threePos.y, threePos.z); }
        public void SetPos(double x, double y, double z) { SetPos(new Vector3((float)x, (float)y, (float)z)); }
        public void SetScale(double s) { T.localScale = Vector3.one * (float)s; }
        public void SetScale3(double sx, double sy, double sz) { T.localScale = new Vector3((float)sx, (float)sy, (float)sz); }
        public void SetRot(double rx, double ry, double rz) { rot[0] = rx; rot[1] = ry; rot[2] = rz; ThreeSpace.Apply(T, rot); }
        public void AddRot(int axis, double d) { rot[axis] += d; ThreeSpace.Apply(T, rot); }
        public void SetOpacity(double a) { Color c = FxMaterials.GetColor(Mat); c.a = (float)a; FxMaterials.SetColor(Mat, c); }

        public void Destroy()
        {
            if (Gone) return;
            Gone = true;
            if (G != null) Object.Destroy(G);
            FxMaterials.Release(Mat, MatKey);   // 재질은 지우지 않고 풀로(T74)
        }
    }

    public static class FxCubes
    {
        static Mesh unit;
        public static int Alive { get; private set; }

        public static Mesh UnitMesh
        {
            get
            {
                if (unit != null) return unit;
                unit = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                if (unit == null)
                {
                    var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    unit = tmp.GetComponent<MeshFilter>().sharedMesh;
                    Object.Destroy(tmp);
                }
                return unit;
            }
        }

        /// <summary>큐브 하나(three 좌표 · 한 변 scale). additive = 원작 `AdditiveBlending`(불티·트레일) · 아니면 보통 알파.</summary>
        public static FxCube Make(Transform parent, int hex, double opacity, bool additive, string name = "FxCube")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = UnitMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
            int key;
            Material m = FxMaterials.Take(hex, opacity, additive, out key);   // 색·불투명도·가산은 Take 가 칠한다
            mr.sharedMaterial = m;
            var c = new FxCube(go, m, key);
            Alive++;
            return c;
        }

        public static void Kill(FxCube c) { if (c == null || c.Gone) return; c.Destroy(); Alive--; }
    }
}
