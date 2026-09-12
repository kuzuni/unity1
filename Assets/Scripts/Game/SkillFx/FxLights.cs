using System.Collections.Generic;
using UnityEngine;
using Forge.Game.Voxel;

namespace Forge.Game.SkillFx
{
    /// <summary>원작 `fxLight`(포인트 라이트 풀 · 리스 토큰) + `flashLight`. 개수를 늘리지 않는다(원작: «직접 생성 금지 · 개수 변화 = 재컴파일»).</summary>
    public sealed class FxLights
    {
        public const int PoolSize = 4;
        public const float DefaultRange = 7f;

        public sealed class Lease
        {
            readonly FxLights owner; readonly Light l; readonly long token;
            internal Lease(FxLights owner, Light l, long token) { this.owner = owner; this.l = l; this.token = token; }
            bool Mine { get { long t; return l != null && owner.lease.TryGetValue(l, out t) && t == token; } }
            public void Set(double intensity) { if (Mine) l.intensity = (float)intensity; }
            public double Get() { return Mine ? l.intensity : 0; }
            public void Pos(double x, double y, double z) { if (Mine) l.transform.localPosition = ThreeSpace.Pos(x, y, z); }
            public void Release() { if (!Mine) return; l.intensity = 0; owner.lease.Remove(l); }
        }

        readonly List<Light> lights = new List<Light>();
        readonly Dictionary<Light, long> lease = new Dictionary<Light, long>();
        readonly Dictionary<Light, long> lastToken = new Dictionary<Light, long>();
        long seq;

        public FxLights(Transform parent)
        {
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("FxLight " + i);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = new Vector3(0, -50, 0);
                var l = go.AddComponent<Light>();
                l.type = LightType.Point; l.intensity = 0; l.range = DefaultRange; l.shadows = LightShadows.None;
                lights.Add(l); lastToken[l] = 0;
            }
        }

        public int Busy { get { return lease.Count; } }

        /// <summary>`fxLight(colorHex, distance)` — 빈 라이트, 없으면 가장 오래된 리스를 뺏는다.</summary>
        public Lease Take(int hex, double range = DefaultRange)
        {
            Light pick = null;
            foreach (var l in lights) if (!lease.ContainsKey(l)) { pick = l; break; }
            if (pick == null) foreach (var l in lights) if (pick == null || lastToken[l] < lastToken[pick]) pick = l;
            long token = ++seq;
            lease[pick] = token; lastToken[pick] = token;
            pick.color = VoxelMaterials.ToColor(hex);
            pick.intensity = 0; pick.range = (float)range;
            return new Lease(this, pick, token);
        }

        /// <summary>전부 반납 — 씬이 내려가는 중이면 라이트가 먼저 파괴돼 있을 수 있어 «살아 있는 것만» 만진다.</summary>
        public void ReleaseAll() { foreach (var l in lights) if (l != null) l.intensity = 0; lease.Clear(); }
    }
}
