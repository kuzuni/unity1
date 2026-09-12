using UnityEngine;

namespace Forge.Game.Voxel
{
    /// <summary>
    /// three(오른손 · +z 정면) ↔ 유니티(왼손) 좌표 변환 — **z 부호만 뒤집는다**(결정 4 · x·y 그대로).
    /// 거울 변환이라 x·y 축 회전은 부호가 바뀌고 z 축 회전은 그대로다(M·Rx(θ)·M = Rx(−θ)). three 의 XYZ 오일러는 R = Rx·Ry·Rz 라
    /// 유니티 쿼터니언으로는 Qx(−rx)·Qy(−ry)·Qz(rz) 순서 곱이다(유니티 `Quaternion.Euler` 는 Z·X·Y 순이라 쓰지 않는다).
    /// </summary>
    public static class ThreeSpace
    {
        public static Vector3 Pos(double[] p)
        {
            return new Vector3((float)p[0], (float)p[1], -(float)p[2]);
        }

        public static Vector3 Pos(double x, double y, double z)
        {
            return new Vector3((float)x, (float)y, -(float)z);
        }

        /// <summary>three XYZ 오일러(라디안) → 유니티 로컬 회전.</summary>
        public static Quaternion Rot(double[] rot)
        {
            double rx = rot != null && rot.Length > 0 ? rot[0] : 0;
            double ry = rot != null && rot.Length > 1 ? rot[1] : 0;
            double rz = rot != null && rot.Length > 2 ? rot[2] : 0;
            return Rot(rx, ry, rz);
        }

        public static Quaternion Rot(double rx, double ry, double rz)
        {
            return Quaternion.AngleAxis(-(float)(rx * Mathf.Rad2Deg), Vector3.right)
                 * Quaternion.AngleAxis(-(float)(ry * Mathf.Rad2Deg), Vector3.up)
                 * Quaternion.AngleAxis((float)(rz * Mathf.Rad2Deg), Vector3.forward);
        }

        /// <summary>three 회전 값 배열(rx, ry, rz 라디안)을 노드에 적용한다 — 관절 드라이버(T10·T11)가 `rot[axis] = …` 뒤에 부른다.</summary>
        public static void Apply(Transform node, double[] rot)
        {
            node.localRotation = Rot(rot);
        }
    }
}
