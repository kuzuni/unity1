using System;

namespace Forge.Core.Hero
{
    /// <summary>
    /// three r128 의 `Quaternion`/`Euler(XYZ)` 산술 중 정본 영웅 파지·스윙이 쓰는 면만(순수 · 오른손 three 좌표) — `setFromEuler(XYZ)` ·
    /// `setFromAxisAngle` · `multiply` · `slerp` · `Euler.setFromQuaternion(XYZ)`. 유니티 좌표로의 변환은 Game 의 `ThreeSpace` 몫이다.
    /// 값 배열은 [x, y, z, w].
    /// </summary>
    public static class ThreeQuat
    {
        public static double[] Identity() { return new[] { 0.0, 0.0, 0.0, 1.0 }; }

        /// <summary>`Quaternion.setFromEuler(new Euler(x, y, z, 'XYZ'))`.</summary>
        public static double[] FromEulerXYZ(double x, double y, double z)
        {
            double c1 = Math.Cos(x / 2), c2 = Math.Cos(y / 2), c3 = Math.Cos(z / 2);
            double s1 = Math.Sin(x / 2), s2 = Math.Sin(y / 2), s3 = Math.Sin(z / 2);
            return new[]
            {
                s1 * c2 * c3 + c1 * s2 * s3,
                c1 * s2 * c3 - s1 * c2 * s3,
                c1 * c2 * s3 + s1 * s2 * c3,
                c1 * c2 * c3 - s1 * s2 * s3,
            };
        }

        public static double[] FromEulerXYZ(double[] e) { return FromEulerXYZ(e[0], e[1], e[2]); }

        /// <summary>`Quaternion.setFromAxisAngle(axis, angle)` — 축은 단위 벡터여야 한다.</summary>
        public static double[] FromAxisAngle(double ax, double ay, double az, double angle)
        {
            double h = angle / 2, s = Math.Sin(h);
            return new[] { ax * s, ay * s, az * s, Math.Cos(h) };
        }

        /// <summary>`a.multiply(b)` = a × b(b 를 로컬 축에서 «뒤에» 적용 — 정본 날 롤의 우측곱).</summary>
        public static double[] Multiply(double[] a, double[] b)
        {
            double qax = a[0], qay = a[1], qaz = a[2], qaw = a[3];
            double qbx = b[0], qby = b[1], qbz = b[2], qbw = b[3];
            return new[]
            {
                qax * qbw + qaw * qbx + qay * qbz - qaz * qby,
                qay * qbw + qaw * qby + qaz * qbx - qax * qbz,
                qaz * qbw + qaw * qbz + qax * qby - qay * qbx,
                qaw * qbw - qax * qbx - qay * qby - qaz * qbz,
            };
        }

        /// <summary>`Euler.setFromQuaternion(q, 'XYZ')` — three 는 회전 행렬을 거쳐 푼다(같은 식).</summary>
        public static double[] ToEulerXYZ(double[] q)
        {
            double x = q[0], y = q[1], z = q[2], w = q[3];
            double x2 = x + x, y2 = y + y, z2 = z + z;
            double xx = x * x2, xy = x * y2, xz = x * z2;
            double yy = y * y2, yz = y * z2, zz = z * z2;
            double wx = w * x2, wy = w * y2, wz = w * z2;
            double m11 = 1 - (yy + zz), m12 = xy - wz, m13 = xz + wy;
            double m22 = 1 - (xx + zz), m23 = yz - wx;
            double m32 = yz + wx, m33 = 1 - (xx + yy);
            double ey = Math.Asin(Math.Max(-1, Math.Min(1, m13)));
            double ex, ez;
            if (Math.Abs(m13) < 0.9999999)
            {
                ex = Math.Atan2(-m23, m33);
                ez = Math.Atan2(-m12, m11);
            }
            else
            {
                ex = Math.Atan2(m32, m22);
                ez = 0;
            }
            return new[] { ex, ey, ez };
        }

        /// <summary>`a.copy(from).slerp(to, t)` — three r128 의 slerp 그대로(부호 뒤집기 · 작은 각 선형 폴백 포함).</summary>
        public static double[] Slerp(double[] from, double[] to, double t)
        {
            var r = new[] { from[0], from[1], from[2], from[3] };
            if (t == 0) return r;
            if (t == 1) return new[] { to[0], to[1], to[2], to[3] };
            double x = r[0], y = r[1], z = r[2], w = r[3];
            double cosHalfTheta = w * to[3] + x * to[0] + y * to[1] + z * to[2];
            if (cosHalfTheta < 0)
            {
                r[3] = -to[3]; r[0] = -to[0]; r[1] = -to[1]; r[2] = -to[2];
                cosHalfTheta = -cosHalfTheta;
            }
            else
            {
                r[0] = to[0]; r[1] = to[1]; r[2] = to[2]; r[3] = to[3];
            }
            if (cosHalfTheta >= 1.0)
            {
                r[3] = w; r[0] = x; r[1] = y; r[2] = z;
                return r;
            }
            double sqrSinHalfTheta = 1.0 - cosHalfTheta * cosHalfTheta;
            if (sqrSinHalfTheta <= 2.220446049250313e-16)   // Number.EPSILON
            {
                double s = 1 - t;
                r[3] = s * w + t * r[3];
                r[0] = s * x + t * r[0];
                r[1] = s * y + t * r[1];
                r[2] = s * z + t * r[2];
                double len = Math.Sqrt(r[0] * r[0] + r[1] * r[1] + r[2] * r[2] + r[3] * r[3]);
                if (len == 0) { r[0] = 0; r[1] = 0; r[2] = 0; r[3] = 1; }
                else { r[0] /= len; r[1] /= len; r[2] /= len; r[3] /= len; }
                return r;
            }
            double sinHalfTheta = Math.Sqrt(sqrSinHalfTheta);
            double halfTheta = Math.Atan2(sinHalfTheta, cosHalfTheta);
            double ratioA = Math.Sin((1 - t) * halfTheta) / sinHalfTheta;
            double ratioB = Math.Sin(t * halfTheta) / sinHalfTheta;
            r[3] = w * ratioA + r[3] * ratioB;
            r[0] = x * ratioA + r[0] * ratioB;
            r[1] = y * ratioA + r[1] * ratioB;
            r[2] = z * ratioA + r[2] * ratioB;
            return r;
        }
    }
}
