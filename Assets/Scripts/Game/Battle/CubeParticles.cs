using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Forge.Core.World;
using Forge.Game.Hero;
using Forge.Game.Voxel;

namespace Forge.Game.Battle
{
    /// <summary>
    /// 원작 `spawnShards`(12670) · `spawnSparks`(17204) · `update` 의 파티클 루프(18743) — **큐브 파티클**(화풍 ⓒ · 스프라이트 금지). 정본 규칙 그대로:
    /// 파편 = 2:1 납작 상자 · 진행각 ±spread · 속도 rand(1.8, 3.6)·speed · 텀블 3축 · 수명 0.28~0.5 · 상한 300. 불티 = 큐브 rand(0.09, 0.19)·scale · 명도 지터 · 수명 0.35~0.7 · 붐비면 45% · 상한 320.
    /// 매 프레임: 위치 += vel·dt · 중력 −9 · 텀블 · 불투명도 = 남은 수명 · 불티는 크기 `0.4 + 0.6·lifeK`. 좌표는 three(x, y, z) 로 받아 z 를 뒤집는다(결정 4).
    /// 난수는 연출용이라 UnityEngine.Random 이다(전투 규칙 난수는 Core Rng).
    /// </summary>
    public sealed class CubeParticles : MonoBehaviour
    {
        public const int ShardCap = 300, SparkCap = 320, SparkCrowd = 180;
        public const double Gravity = 9;

        sealed class P
        {
            public Transform T;
            public MeshRenderer R;
            public Material Mat;
            public Vector3 Vel, Tumble;
            public float Life, Age, BaseScale;
            public bool NoGravity;
        }

        readonly List<P> live = new List<P>();
        readonly Stack<GameObject> pool = new Stack<GameObject>();
        Mesh cube;

        public int Count { get { return live.Count; } }

        static float R(double a, double b) { return (float)(a + UnityEngine.Random.value * (b - a)); }

        GameObject Take()
        {
            GameObject go = pool.Count > 0 ? pool.Pop() : null;
            if (go == null)
            {
                go = new GameObject("cube");
                go.transform.SetParent(transform, false);
                if (cube == null) cube = HeroMeshes.Box(1, 1, 1, 0xffffff);
                go.AddComponent<MeshFilter>().sharedMesh = cube;
                var mr = go.AddComponent<MeshRenderer>();
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            go.SetActive(true);
            return go;
        }

        static int Lightness(int hex, double kMin, double kMax)
        {
            double h, s, l;
            Col.FromHex(hex).GetHsl(out h, out s, out l);
            double l2 = Col.Clamp(l * R(kMin, kMax), 0.05, kMax > 1.19 ? 0.97 : 0.95);
            // 재질 공유를 위해 명도를 1/16 단위로 양자화한다(색상·채도는 그대로 — 원작이 명도만 흔든다).
            l2 = Math.Round(l2 * 16) / 16;
            return Col.FromHsl(h, s, l2).Hex;
        }

        void Add(GameObject go, Material mat, Vector3 pos, Vector3 scale, Vector3 rot, Vector3 vel, Vector3 tumble, float life, float baseScale)
        {
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.transform.localRotation = ThreeSpace.Rot(rot.x, rot.y, rot.z);
            live.Add(new P { T = go.transform, R = mr, Mat = mat, Vel = vel, Tumble = tumble, Life = life, Age = 0, BaseScale = baseScale });
        }

        /// <summary>파편 — three 좌표 pos · 색 hex · dir/spread(라디안) · speed/scale 배율.</summary>
        public void Shards(Vector3 threePos, int count, int hex, double dir, double spread, double speed, double scale)
        {
            if (live.Count > ShardCap) return;
            for (int i = 0; i < count; i++)
            {
                float w = R(0.13, 0.21) * (float)scale, h = R(0.055, 0.1) * (float)scale;
                int c = Lightness(hex, 0.72, 1.18);
                float ang = (float)dir + R(-spread, spread);
                float spd = R(1.8, 3.6) * (float)speed;
                Vector3 p3 = threePos + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0) * 0.22f;
                var go = Take();
                Add(go, FxMaterials.Shared(c), ThreeSpace.Pos(p3.x, p3.y, p3.z), new Vector3(w, h, h * 0.7f),
                    new Vector3(R(-0.5, 0.5), R(-0.5, 0.5), ang),
                    ThreeSpace.Pos(Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd + R(0.5, 2.2), R(-1, 1)),
                    new Vector3(R(-9, 9), R(-9, 9), R(-14, 14)), R(0.28, 0.5), 0);
            }
        }

        /// <summary>불티 — three 좌표 pos · 색 hex · speed/scale 배율.</summary>
        public void Sparks(Vector3 threePos, int count, int hex, double speed = 1, double scale = 1)
        {
            int n = live.Count;
            if (n > SparkCap) return;
            if (n > SparkCrowd) count = Math.Max(1, (int)Math.Round(count * 0.45));
            for (int i = 0; i < count; i++)
            {
                int c = Lightness(hex, 0.80, 1.20);
                float e = R(0.09, 0.19) * (float)scale;
                var go = Take();
                Add(go, FxMaterials.Shared(c), ThreeSpace.Pos(threePos.x, threePos.y, threePos.z), Vector3.one * e,
                    new Vector3(R(0, 6.28), R(0, 6.28), R(0, 6.28)),
                    ThreeSpace.Pos(R(-2.5, 2.5) * speed, R(1.5, 4.5) * speed, R(-1.5, 1.5) * speed),
                    new Vector3(R(-8, 8), R(-8, 8), R(-8, 8)), R(0.35, 0.7), e);
            }
        }

        public void Step(float dt)
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                P p = live[i];
                p.Age += dt;
                if (p.Age >= p.Life || p.T == null)
                {
                    if (p.T != null) { p.T.gameObject.SetActive(false); pool.Push(p.T.gameObject); }
                    live.RemoveAt(i);
                    continue;
                }
                p.T.position += p.Vel * dt;
                if (!p.NoGravity) p.Vel.y -= (float)Gravity * dt;
                // 텀블(three 라디안/초) — 유니티 축으로는 z 반전 거울이라 x·y 축 회전 부호가 뒤집힌다(ThreeSpace.Rot 과 같은 규약).
                p.T.Rotate(new Vector3(-p.Tumble.x, -p.Tumble.y, p.Tumble.z) * (dt * Mathf.Rad2Deg), Space.Self);
                float lifeK = 1 - p.Age / p.Life;
                if (p.BaseScale > 0) p.T.localScale = Vector3.one * (p.BaseScale * (0.4f + 0.6f * lifeK));
            }
        }

        public void Clear()
        {
            for (int i = 0; i < live.Count; i++) if (live[i].T != null) { live[i].T.gameObject.SetActive(false); pool.Push(live[i].T.gameObject); }
            live.Clear();
        }
    }
}
