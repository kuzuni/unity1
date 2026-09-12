using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Forge.Core.Hero
{
    /// <summary>키프레임 하나 `[t, v, ease?]` — t 는 클립 길이 대비 0~1, v 는 베이스 대비 오프셋(배율 채널은 곱셈), Ease 는 «이 키로 들어오는 구간» 의 이징 이름(null = smoothstep).</summary>
    public sealed class ClipKey
    {
        public readonly double T, V;
        public readonly string Ease;
        public ClipKey(double t, double v, string ease) { T = t; V = v; Ease = ease; }
    }

    /// <summary>트랙 하나 — `bone.channel`(rx/ry/rz/px/py/pz/sx/sy/sz · bone 은 본 이름 또는 root).</summary>
    public sealed class ClipTrack
    {
        public readonly string Key, Bone, Channel;
        public readonly ClipKey[] Keys;
        public ClipTrack(string key, ClipKey[] keys)
        {
            Key = key; Keys = keys;
            int dot = key.IndexOf('.');
            Bone = key.Substring(0, dot);
            Channel = key.Substring(dot + 1);
        }
    }

    /// <summary>클립 하나(정본 `ProChar.CLIPS[name]`) — dur(초) · loop · once · groundPose(사망·기상: 거치 자세를 안 얹는다) · tracks · capeFlat.</summary>
    public sealed class HeroClip
    {
        public string Name;
        public double Dur;
        public bool Loop, Once, GroundPose;
        public ClipTrack[] Tracks;
        public ClipKey[] CapeFlat;

        public ClipTrack Track(string key)
        {
            for (int i = 0; i < Tracks.Length; i++) if (Tracks[i].Key == key) return Tracks[i];
            return null;
        }
    }

    /// <summary>
    /// 정본 `prochar.js` 의 클립 표(`CLIPS`)·별칭(`CLIP_ALIAS`)·`resolveClip`·`sample` 을 그대로 옮긴 것. 값은 정본 코드 상수라 JSON(T2)에 없다 —
    /// `tools/hero_vectors.js` 가 정본을 실제로 돌려 뽑은 `t6-hero.json` 과 1:1 로 대조한다(EditMode). 애니는 코드 애니(Animator 안 씀 · 정본이 프레임마다 각을 계산).
    /// </summary>
    public static class HeroClips
    {
        const double H = Math.PI / 2;

        static ClipKey K(double t, double v) { return new ClipKey(t, v, null); }
        static ClipKey K(double t, double v, string ease) { return new ClipKey(t, v, ease); }
        static ClipTrack T(string key, params ClipKey[] keys) { return new ClipTrack(key, keys); }
        static ClipKey[] CapeFlat(params ClipKey[] keys) { return keys; }
        static HeroClip Clip(string name, double dur, bool loop, bool once, bool groundPose, params object[] items)
        {
            var tracks = new List<ClipTrack>();
            ClipKey[] cape = null;
            foreach (var it in items)
            {
                var tr = it as ClipTrack;
                if (tr != null) tracks.Add(tr);
                else cape = (ClipKey[])it;
            }
            return new HeroClip { Name = name, Dur = dur, Loop = loop, Once = once, GroundPose = groundPose, Tracks = tracks.ToArray(), CapeFlat = cape };
        }

        /// <summary>정본 순서 그대로: Idle · Walking · slash · chop · thrust · slam · double · bow · gun · cast · throw · Death · Revive.</summary>
        public static readonly HeroClip[] All =
        {
            Clip("Idle", 2.8, true, false, false,
                T("spine.rx", K(0, 0.02), K(0.5, 0.055), K(1, 0.02)),
                T("spine.py", K(0, 0), K(0.5, 0.012), K(1, 0)),
                T("neck.rx", K(0, 0), K(0.55, -0.05), K(1, 0)),
                T("shoulderL.rz", K(0, 0.06), K(0.5, 0.1), K(1, 0.06)),
                T("shoulderR.rz", K(0, -0.06), K(0.5, -0.1), K(1, -0.06)),
                T("shoulderR.rx", K(0, -0.14), K(0.5, -0.1), K(1, -0.14)),
                T("shoulderL.rx", K(0, -0.1), K(0.5, -0.06), K(1, -0.1)),
                T("cape.rx", K(0, 0.02), K(0.5, 0.05), K(1, 0.02)),
                T("elbowL.rx", K(0, -0.22), K(0.5, -0.16), K(1, -0.22)),
                T("elbowR.rx", K(0, -0.18), K(0.5, -0.25), K(1, -0.18)),
                T("kneeL.rx", K(0, -0.05), K(0.5, -0.08), K(1, -0.05)),
                T("kneeR.rx", K(0, -0.08), K(0.5, -0.05), K(1, -0.08)),
                T("hipL.rx", K(0, 0.04), K(1, 0.04)),
                T("hipR.rx", K(0, 0.06), K(1, 0.06)),
                T("spine.sy", K(0, 1), K(0.5, 1.028, "back"), K(1, 1, "back")),
                T("spine.sx", K(0, 1), K(0.5, 0.986), K(1, 1)),
                T("spine.sz", K(0, 1), K(0.5, 0.986), K(1, 1)),
                T("head.sy", K(0, 1), K(0.5, 0.975, "back"), K(1, 1, "back")),
                T("head.sx", K(0, 1), K(0.5, 1.025), K(1, 1)),
                T("head.rz", K(0, 0), K(0.28, 0.05, "back"), K(0.66, -0.038, "back"), K(1, 0, "back")),
                T("head.ry", K(0, 0), K(0.25, 0.04), K(0.5, 0), K(0.75, -0.04), K(1, 0))),
            Clip("Walking", 0.66, true, false, false,
                T("hipL.rx", K(0, 0.62), K(0.15, 0.1), K(0.3, -0.4), K(0.45, -0.65), K(0.55, -0.55), K(0.75, 0), K(1, 0.62)),
                T("kneeL.rx", K(0, -0.5), K(0.15, -1.25), K(0.35, -0.5), K(0.48, -0.12), K(0.6, -0.45), K(0.75, -0.18), K(0.9, -0.35), K(1, -0.5)),
                T("hipR.rx", K(0, -0.6), K(0.05, -0.55), K(0.25, 0), K(0.5, 0.62), K(0.65, 0.1), K(0.8, -0.4), K(0.95, -0.65), K(1, -0.6)),
                T("kneeR.rx", K(0, -0.3), K(0.1, -0.45), K(0.25, -0.18), K(0.4, -0.35), K(0.5, -0.5), K(0.65, -1.25), K(0.85, -0.5), K(0.98, -0.12), K(1, -0.3)),
                T("shoulderL.rx", K(0, -0.6), K(0.25, -0.2), K(0.5, 0.5), K(0.75, 0.1), K(1, -0.6)),
                T("shoulderR.rx", K(0, 0.5), K(0.25, 0.1), K(0.5, -0.6), K(0.75, -0.2), K(1, 0.5)),
                T("elbowL.rx", K(0, -0.25), K(0.5, -0.8), K(1, -0.25)),
                T("elbowR.rx", K(0, -0.8), K(0.5, -0.25), K(1, -0.8)),
                T("spine.rx", K(0, 0.1), K(1, 0.1)),
                T("spine.ry", K(0, 0.3), K(0.5, -0.3), K(1, 0.3)),
                T("pelvis.ry", K(0, -0.17), K(0.5, 0.17), K(1, -0.17)),
                T("pelvis.rz", K(0, 0.05), K(0.5, -0.05), K(1, 0.05)),
                T("root.py", K(0, 0.075, "back"), K(0.22, 0.012, "bounce"), K(0.45, 0.004), K(0.72, 0.078, "back"), K(0.95, 0.004, "bounce"), K(1, 0.075, "back")),
                T("spine.sy", K(0, 1.05, "back"), K(0.22, 0.95, "bounce"), K(0.45, 0.97), K(0.72, 1.05, "back"), K(0.95, 0.95, "bounce"), K(1, 1.05, "back")),
                T("spine.sx", K(0, 0.975), K(0.22, 1.03), K(0.45, 1.02), K(0.72, 0.975), K(0.95, 1.03), K(1, 0.975)),
                T("spine.sz", K(0, 0.975), K(0.22, 1.03), K(0.45, 1.02), K(0.72, 0.975), K(0.95, 1.03), K(1, 0.975)),
                T("head.sy", K(0, 0.965), K(0.22, 1.04), K(0.45, 1.02), K(0.72, 0.965), K(0.95, 1.04), K(1, 0.965)),
                T("head.sx", K(0, 1.025), K(0.22, 0.97), K(0.45, 0.985), K(0.72, 1.025), K(0.95, 0.97), K(1, 1.025)),
                T("head.py", K(0, 0.006, "back"), K(0.28, -0.004, "bounce"), K(0.72, 0.006, "back"), K(1, 0.006, "back")),
                T("cape.rx", K(0, 0.26), K(0.25, 0.44), K(0.5, 0.3), K(0.75, 0.44), K(1, 0.26)),
                T("cape.rz", K(0, 0.06), K(0.5, -0.06), K(1, 0.06)),
                T("neck.rx", K(0, -0.08), K(1, -0.08))),
            Clip("slash", 0.5, false, false, false,
                T("shoulderR.rx", K(0, -0.14), K(0.075, 0.16), K(0.27, -2.1, "out"), K(0.345, -2.04), K(0.45, -0.3, "in"), K(0.55, 1, "out"), K(0.76, 0.34), K(1, -0.14)),
                T("shoulderR.rz", K(0, -0.06), K(0.0675, -0.02), K(0.27, 0.34, "out"), K(0.345, 0.32), K(0.45, -0.08, "in"), K(0.55, -0.42, "out"), K(0.76, -0.18), K(1, -0.06)),
                T("elbowR.rx", K(0, -0.15), K(0.27, -0.3), K(0.45, -0.06), K(0.55, -0.02), K(1, -0.15)),
                T("spine.ry", K(0, 0), K(0.0675, 0.07), K(0.27, -0.58, "out"), K(0.345, -0.56), K(0.45, 0.2, "in"), K(0.55, 0.55, "out"), K(0.76, 0.22), K(1, 0)),
                T("hipL.rx", K(0, 0.04), K(0.0675, 0.16), K(0.27, 0.24), K(0.45, -0.34, "in"), K(0.58, -0.44, "out"), K(0.79, -0.12), K(1, 0.04)),
                T("hipR.rx", K(0, 0.06), K(0.0675, -0.1), K(0.27, -0.16), K(0.45, 0.3, "in"), K(0.58, 0.4, "out"), K(0.79, 0.14), K(1, 0.06)),
                T("kneeL.rx", K(0, -0.05), K(0.0675, -0.22), K(0.27, -0.32), K(0.45, -0.1), K(0.58, -0.06), K(0.79, -0.14), K(1, -0.05)),
                T("kneeR.rx", K(0, -0.08), K(0.0675, -0.16), K(0.27, -0.26), K(0.45, -0.34), K(0.58, -0.48, "bounce"), K(0.79, -0.18), K(1, -0.08)),
                T("pelvis.ry", K(0, 0), K(0.27, 0.18), K(0.45, -0.14), K(0.58, -0.2), K(1, 0)),
                T("neck.ry", K(0, 0), K(0.0675, -0.06), K(0.27, 0.49, "out"), K(0.345, 0.48), K(0.45, -0.17, "in"), K(0.55, -0.47, "out"), K(0.76, -0.19), K(1, 0)),
                T("spine.rx", K(0, 0.02), K(0.0675, -0.09), K(0.27, -0.16), K(0.45, 0.12, "in"), K(0.55, 0.3, "out"), K(0.76, 0.15), K(1, 0.02)),
                T("shoulderL.rx", K(0, -0.1), K(0.0675, -0.26), K(0.27, 0.34), K(0.45, -0.3), K(0.55, -0.62), K(0.76, -0.28), K(1, -0.1)),
                T("cape.rx", K(0, 0.05), K(0.3, 0.5), K(0.58, 0.22), K(1, 0.05)),
                T("spine.sy", K(0, 1), K(0.15, 1.055, "back"), K(0.27, 1.05), K(0.375, 1), K(0.55, 1), K(0.73, 0.955, "bounce"), K(1, 1, "back")),
                T("spine.sx", K(0, 1), K(0.15, 0.97), K(0.27, 0.975), K(0.375, 1), K(0.55, 1), K(0.73, 1.03), K(1, 1)),
                T("spine.sz", K(0, 1), K(0.15, 0.97), K(0.27, 0.975), K(0.375, 1), K(0.55, 1), K(0.73, 1.03), K(1, 1)),
                T("head.sy", K(0, 1), K(0.15, 0.955), K(0.27, 0.96), K(0.375, 1), K(0.55, 1), K(0.73, 1.045), K(1, 1)),
                T("head.sx", K(0, 1), K(0.15, 1.03), K(0.27, 1.025), K(0.375, 1), K(0.55, 1), K(0.73, 0.97), K(1, 1))),
            Clip("chop", 0.55, false, false, false,
                T("shoulderR.rx", K(0, -0.14), K(0.0758, 0.2), K(0.2881, -2.4, "out"), K(0.3639, -2.34), K(0.47, -0.55, "in"), K(0.57, 0.92, "out"), K(0.785, 0.3), K(1, -0.14)),
                T("shoulderR.rz", K(0, -0.06), K(0.2881, 0.12), K(0.47, -0.02), K(0.57, -0.06), K(1, -0.06)),
                T("elbowR.rx", K(0, -0.15), K(0.2881, -0.32), K(0.47, -0.05), K(0.57, -0.02), K(1, -0.15)),
                T("shoulderL.rx", K(0, -0.1), K(0.0682, -0.26), K(0.2881, 0.3), K(0.47, -0.32), K(0.57, -0.55), K(0.785, -0.26), K(1, -0.1)),
                T("spine.rx", K(0, 0.02), K(0.0682, -0.12), K(0.2881, -0.28), K(0.47, 0.22, "in"), K(0.57, 0.46, "out"), K(0.785, 0.2), K(1, 0.02)),
                T("spine.ry", K(0, 0), K(0.2881, -0.24), K(0.47, 0.12, "in"), K(0.57, 0.22, "out"), K(1, 0)),
                T("neck.ry", K(0, 0), K(0.2881, 0.2), K(0.47, -0.1), K(0.57, -0.19), K(1, 0)),
                T("hipL.rx", K(0, 0.04), K(0.0682, 0.18), K(0.2881, 0.26), K(0.47, -0.3, "in"), K(0.6007, -0.4, "out"), K(0.8157, -0.12), K(1, 0.04)),
                T("hipR.rx", K(0, 0.06), K(0.0682, -0.12), K(0.2881, -0.18), K(0.47, 0.26, "in"), K(0.6007, 0.36, "out"), K(0.8157, 0.12), K(1, 0.06)),
                T("kneeL.rx", K(0, -0.05), K(0.2881, -0.28), K(0.47, -0.3), K(0.6007, -0.44, "bounce"), K(0.8157, -0.16), K(1, -0.05)),
                T("kneeR.rx", K(0, -0.08), K(0.2881, -0.3), K(0.47, -0.4), K(0.6007, -0.54, "bounce"), K(0.8157, -0.18), K(1, -0.08)),
                T("root.py", K(0, 0), K(0.2881, 0.045, "back"), K(0.47, -0.012), K(0.6007, -0.024, "bounce"), K(0.8464, 0.004), K(1, 0)),
                T("cape.rx", K(0, 0.05), K(0.3184, 0.55), K(0.6007, 0.2), K(1, 0.05)),
                T("spine.sy", K(0, 1), K(0.1819, 1.06, "back"), K(0.2881, 1.05), K(0.3942, 1), K(0.57, 1), K(0.785, 0.95, "bounce"), K(1, 1, "back")),
                T("spine.sx", K(0, 1), K(0.1819, 0.968), K(0.2881, 0.975), K(0.3942, 1), K(0.57, 1), K(0.785, 1.03), K(1, 1)),
                T("spine.sz", K(0, 1), K(0.1819, 0.968), K(0.2881, 0.975), K(0.3942, 1), K(0.57, 1), K(0.785, 1.03), K(1, 1)),
                T("head.sy", K(0, 1), K(0.1819, 0.95), K(0.2881, 0.96), K(0.3942, 1), K(0.57, 1), K(0.785, 1.05), K(1, 1)),
                T("head.sx", K(0, 1), K(0.1819, 1.032), K(0.2881, 1.025), K(0.3942, 1), K(0.57, 1), K(0.785, 0.968), K(1, 1))),
            Clip("thrust", 0.45, false, false, false,
                T("shoulderR.rx", K(0, -0.14), K(0.2464, 0.26), K(0.3286, 0.24), K(0.46, -1.72, "in"), K(0.55, -1.88, "out"), K(0.7618, -1.1), K(1, -0.14)),
                T("elbowR.rx", K(0, -0.2), K(0.2464, -0.48), K(0.46, 0.02), K(0.55, 0.06), K(1, -0.2)),
                T("spine.ry", K(0, 0), K(0.2464, 0.34, "out"), K(0.3286, 0.32), K(0.46, -0.42, "in"), K(0.55, -0.5, "out"), K(0.7618, -0.18), K(1, 0)),
                T("neck.ry", K(0, 0), K(0.2464, -0.29), K(0.3286, -0.27), K(0.46, 0.36), K(0.55, 0.43), K(0.7618, 0.15), K(1, 0)),
                T("spine.rx", K(0, 0.02), K(0.2464, -0.14), K(0.46, 0.18, "in"), K(0.55, 0.26, "out"), K(0.7618, 0.12), K(1, 0.02)),
                T("shoulderL.rx", K(0, -0.1), K(0.2464, -0.45), K(0.46, 0.42), K(0.55, 0.52), K(0.7618, 0.2), K(1, -0.1)),
                T("hipL.rx", K(0, 0.04), K(0.2464, 0.3), K(0.46, -0.46, "in"), K(0.5765, -0.54, "out"), K(0.8015, -0.16), K(1, 0.04)),
                T("hipR.rx", K(0, 0.06), K(0.2464, -0.14), K(0.46, 0.4, "in"), K(0.5765, 0.5, "out"), K(0.8015, 0.16), K(1, 0.06)),
                T("kneeL.rx", K(0, -0.05), K(0.2464, -0.36), K(0.46, -0.12), K(0.5765, -0.08), K(0.8015, -0.14), K(1, -0.05)),
                T("kneeR.rx", K(0, -0.08), K(0.2464, -0.22), K(0.46, -0.3), K(0.5765, -0.4), K(0.8015, -0.16), K(1, -0.08)),
                T("spine.sy", K(0, 1), K(0.1643, 1.05, "back"), K(0.2464, 1.045), K(0.3779, 1), K(0.6559, 1), K(0.8147, 0.96, "bounce"), K(1, 1, "back")),
                T("spine.sz", K(0, 1), K(0.1643, 0.972), K(0.2464, 0.976), K(0.3779, 1), K(0.6559, 1), K(0.8147, 1.025), K(1, 1)),
                T("cape.rx", K(0, 0.05), K(0.2793, 0.42), K(0.55, 0.16), K(1, 0.05))),
            Clip("slam", 0.6, false, false, false,
                T("shoulderR.rx", K(0, -0.14), K(0.0859, 0.22), K(0.3125, -2.28, "out"), K(0.3906, -2.22), K(0.5, -0.5, "in"), K(0.6, 0.8, "out"), K(0.8154, 0.26), K(1, -0.14)),
                T("shoulderL.rx", K(0, -0.1), K(0.0859, 0.2), K(0.3125, -2.22, "out"), K(0.3906, -2.16), K(0.5, -0.45, "in"), K(0.6, 0.74, "out"), K(0.8154, 0.24), K(1, -0.1)),
                T("elbowR.rx", K(0, -0.15), K(0.3125, -0.34), K(0.5, -0.05), K(0.6, -0.02), K(1, -0.15)),
                T("elbowL.rx", K(0, -0.15), K(0.3125, -0.34), K(0.5, -0.05), K(0.6, -0.02), K(1, -0.15)),
                T("spine.rx", K(0, 0.02), K(0.0781, -0.14), K(0.3125, -0.34), K(0.5, 0.26, "in"), K(0.6, 0.5, "out"), K(0.8154, 0.22), K(1, 0.02)),
                T("root.py", K(0, 0), K(0.3125, 0.075, "back"), K(0.4844, -0.014), K(0.6308, -0.03, "bounce"), K(0.8462, 0.006), K(1, 0)),
                T("hipL.rx", K(0, 0.04), K(0.0781, 0.2), K(0.3125, 0.24), K(0.5, -0.26, "in"), K(0.6308, -0.34, "out"), K(0.8462, -0.1), K(1, 0.04)),
                T("hipR.rx", K(0, 0.06), K(0.0781, 0.18), K(0.3125, 0.22), K(0.5, -0.2, "in"), K(0.6308, -0.28, "out"), K(0.8462, -0.08), K(1, 0.06)),
                T("kneeL.rx", K(0, -0.05), K(0.3125, -0.2), K(0.5, -0.46), K(0.6308, -0.64, "bounce"), K(0.8462, -0.18), K(1, -0.05)),
                T("kneeR.rx", K(0, -0.08), K(0.3125, -0.22), K(0.5, -0.48), K(0.6308, -0.66, "bounce"), K(0.8462, -0.2), K(1, -0.08)),
                T("cape.rx", K(0, 0.05), K(0.3594, 0.6), K(0.6615, 0.2), K(1, 0.05)),
                T("spine.sy", K(0, 1), K(0.2031, 1.062, "back"), K(0.3125, 1.055), K(0.4219, 1), K(0.6, 1), K(0.8, 0.945, "bounce"), K(1, 1, "back")),
                T("spine.sx", K(0, 1), K(0.2031, 0.966), K(0.3125, 0.972), K(0.4219, 1), K(0.6, 1), K(0.8, 1.032), K(1, 1)),
                T("spine.sz", K(0, 1), K(0.2031, 0.966), K(0.3125, 0.972), K(0.4219, 1), K(0.6, 1), K(0.8, 1.032), K(1, 1)),
                T("head.sy", K(0, 1), K(0.2031, 0.948), K(0.3125, 0.958), K(0.4219, 1), K(0.6, 1), K(0.8, 1.052), K(1, 1)),
                T("head.sx", K(0, 1), K(0.2031, 1.034), K(0.3125, 1.026), K(0.4219, 1), K(0.6, 1), K(0.8, 0.966), K(1, 1))),
            Clip("double", 0.55, false, false, false,
                T("shoulderR.rx", K(0, -0.14), K(0.0655, 0.22), K(0.18, -1.48, "out"), K(0.2945, 0.62, "in"), K(0.36, 0.8, "out"), K(0.5, -1.4, "out"), K(0.6273, 0.55, "in"), K(0.7, 0.74, "out"), K(0.8636, 0.16), K(1, -0.14)),
                T("shoulderR.rz", K(0, -0.06), K(0.18, 0.34), K(0.2945, -0.36), K(0.36, -0.44), K(0.5, 0.3), K(0.6273, -0.4), K(0.7, -0.46), K(0.8636, -0.14), K(1, -0.06)),
                T("spine.ry", K(0, 0), K(0.18, -0.38), K(0.3273, 0.34), K(0.5, -0.3), K(0.6636, 0.28), K(1, 0)),
                T("neck.ry", K(0, 0), K(0.18, 0.32), K(0.3273, -0.29), K(0.5, 0.26), K(0.6636, -0.24), K(1, 0)),
                T("elbowR.rx", K(0, -0.15), K(0.18, -0.26), K(0.2945, -0.04), K(0.5, -0.24), K(0.6273, -0.04), K(1, -0.15)),
                T("hipL.rx", K(0, 0.04), K(0.1145, 0.18), K(0.2945, -0.28, "in"), K(0.43, 0.1), K(0.6273, -0.3, "in"), K(0.8091, -0.1), K(1, 0.04)),
                T("hipR.rx", K(0, 0.06), K(0.1145, -0.1), K(0.2945, 0.26, "in"), K(0.43, -0.04), K(0.6273, 0.28, "in"), K(0.8091, 0.1), K(1, 0.06)),
                T("kneeL.rx", K(0, -0.05), K(0.18, -0.24), K(0.3273, -0.1), K(0.5364, -0.24), K(0.7, -0.1), K(1, -0.05)),
                T("kneeR.rx", K(0, -0.08), K(0.18, -0.28), K(0.3273, -0.18), K(0.5364, -0.3), K(0.7, -0.2), K(1, -0.08)),
                T("spine.rx", K(0, 0.02), K(0.18, -0.1), K(0.3273, 0.18), K(0.5, -0.08), K(0.6636, 0.22), K(0.8364, 0.1), K(1, 0.02)),
                T("cape.rx", K(0, 0.05), K(0.2455, 0.4), K(0.5545, 0.42), K(0.7955, 0.14), K(1, 0.05))),
            Clip("bow", 0.62, false, false, false,
                T("shoulderL.rx", K(0, -0.1), K(0.25, -1.5), K(0.8, -1.5), K(1, -0.1)),
                T("shoulderL.ry", K(0, 0), K(0.25, -0.15), K(0.8, -0.15), K(1, 0)),
                T("shoulderR.rx", K(0, -0.14), K(0.3, -1.35), K(0.62, -1.3), K(0.75, -1.55), K(1, -0.14)),
                T("elbowR.rx", K(0, -0.15), K(0.3, -1.15), K(0.62, -1.2), K(0.75, -0.2), K(1, -0.15)),
                T("spine.ry", K(0, 0), K(0.3, 0.35), K(0.8, 0.35), K(1, 0)),
                T("neck.ry", K(0, 0), K(0.3, -0.3), K(0.8, -0.3), K(1, 0))),
            Clip("gun", 0.4, false, false, false,
                T("shoulderR.rx", K(0, -0.14), K(0.25, -1.55), K(0.5, -1.75), K(0.65, -1.5), K(1, -0.14)),
                T("elbowR.rx", K(0, -0.15), K(0.25, -0.05), K(0.5, -0.3), K(1, -0.15)),
                T("spine.ry", K(0, 0), K(0.25, 0.2), K(0.5, 0.32), K(1, 0)),
                T("neck.ry", K(0, 0), K(0.25, -0.17), K(0.5, -0.27), K(1, 0)),
                T("spine.rx", K(0, 0.02), K(0.5, -0.06), K(1, 0.02))),
            Clip("cast", 0.66, false, false, false,
                T("shoulderR.rx", K(0, -0.2), K(0.35, -2.6), K(0.65, -1.3), K(1, -0.2)),
                T("elbowR.rx", K(0, -0.15), K(0.35, -0.35), K(0.65, -0.1), K(1, -0.15)),
                T("shoulderL.rx", K(0, -0.1), K(0.35, -0.9), K(0.65, -0.4), K(1, -0.1)),
                T("spine.rx", K(0, 0.02), K(0.35, -0.12), K(0.65, 0.14), K(1, 0.02)),
                T("root.py", K(0, 0), K(0.35, 0.045), K(1, 0)),
                T("cape.rx", K(0, 0.05), K(0.45, 0.4), K(1, 0.05))),
            Clip("throw", 0.5, false, false, false,
                T("shoulderR.rx", K(0, -0.2), K(0.32, -2.75), K(0.58, 0.85), K(1, -0.2)),
                T("elbowR.rx", K(0, -0.15), K(0.32, -0.85), K(0.58, -0.05), K(1, -0.15)),
                T("spine.ry", K(0, 0), K(0.32, -0.5), K(0.58, 0.45), K(1, 0)),
                T("neck.ry", K(0, 0), K(0.32, 0.43), K(0.58, -0.38), K(1, 0)),
                T("spine.rx", K(0, 0.02), K(0.58, 0.24), K(1, 0.02)),
                T("kneeL.rx", K(0, 0), K(0.58, -0.2), K(1, 0))),
            Clip("Death", 1.45, false, true, true,
                T("spine.rx", K(0, 0), K(0.07, -0.55), K(0.2, -0.3), K(0.38, 0.34), K(0.62, 0.22), K(0.8, 0.1), K(1, 0.06)),
                T("spine.ry", K(0, 0), K(0.16, 0.28), K(0.6, 0.14), K(1, 0.1)),
                T("spine.rz", K(0, 0), K(0.55, 0.1), K(0.8, 0.26), K(1, 0.22)),
                T("neck.rx", K(0, 0), K(0.07, -0.52), K(0.2, -0.3), K(0.38, 0.3), K(0.78, 0.04), K(1, 0.16)),
                T("neck.ry", K(0, 0), K(0.5, 0.16), K(0.82, 0.5), K(1, 0.62)),
                T("neck.rz", K(0, 0), K(0.62, 0.12), K(0.82, 0.42), K(1, 0.36)),
                T("hipL.rx", K(0, 0), K(0.38, -0.75), K(0.6, -0.6), K(0.85, 0.1), K(1, 0.06)),
                T("hipR.rx", K(0, 0), K(0.38, -0.62), K(0.6, -0.45), K(0.85, 0.5), K(1, 0.6)),
                T("kneeL.rx", K(0, 0), K(0.38, 2.3), K(0.6, 1.9), K(0.85, -0.22), K(1, -0.16)),
                T("kneeR.rx", K(0, 0), K(0.38, 2.15), K(0.6, 1.75), K(0.85, -0.7), K(1, -0.82)),
                T("hipL.rz", K(0, 0), K(0.7, -0.06), K(1, -0.1)),
                T("hipR.rz", K(0, 0), K(0.7, -0.12), K(1, -0.38)),
                T("root.rx", K(0, 0), K(0.07, 0.14), K(0.22, 0.1), K(0.38, 0.16), K(0.6, -0.06), K(0.78, -0.42), K(1, -0.45)),
                T("root.rz", K(0, 0), K(0.38, 0.05), K(0.55, 0.3), K(0.72, 1.12), K(0.78, 1.5), K(0.86, 1.36), K(1, 1.42)),
                T("root.py", K(0, 0), K(0.16, 0), K(0.38, -0.25), K(0.6, 0), K(0.7, 0.19), K(0.78, 0.37), K(0.86, 0.31), K(1, 0.345)),
                T("shoulderL.rx", K(0, -0.1), K(0.07, -0.42), K(0.26, -0.55), K(0.6, -1.1), K(0.85, -2), K(1, -2.1)),
                T("shoulderR.rx", K(0, -0.14), K(0.26, -0.62), K(0.6, -0.3), K(0.85, 0.06), K(1, 0.12)),
                T("shoulderL.rz", K(0, 0), K(0.5, -0.18), K(1, -0.3)),
                T("shoulderR.rz", K(0, 0), K(0.5, 0.1), K(0.85, -0.26), K(1, -0.32)),
                T("elbowL.rx", K(0, -0.15), K(0.6, -0.42), K(1, -0.24)),
                T("elbowR.rx", K(0, -0.15), K(0.6, -0.6), K(0.85, -0.35), K(1, -0.25)),
                T("cape.rx", K(0, 0.05), K(0.38, 0.5), K(0.8, -0.02), K(1, -0.1)),
                T("cape.rz", K(0, 0), K(0.8, -0.16), K(1, -0.22)),
                CapeFlat(K(0, 0), K(0.6, 0), K(0.84, 0.85), K(1, 1))),
            Clip("Revive", 0.85, false, true, true,
                T("root.rz", K(0, 1.42), K(0.3, 1.05), K(0.62, 0.3), K(0.85, -0.06), K(1, 0)),
                T("root.rx", K(0, -0.45), K(0.35, -0.25), K(0.7, 0.14), K(1, 0)),
                T("root.py", K(0, 0.345), K(0.3, 0.05), K(0.55, -0.22), K(0.9, 0.03), K(1, 0)),
                T("hipL.rx", K(0, 0.06), K(0.4, -0.7), K(0.62, -0.8), K(1, 0)),
                T("hipR.rx", K(0, 0.6), K(0.4, -0.5), K(0.62, -0.62), K(1, 0)),
                T("kneeL.rx", K(0, -0.16), K(0.4, 1.9), K(0.62, 2), K(1, 0)),
                T("kneeR.rx", K(0, -0.82), K(0.4, 1.6), K(0.62, 1.75), K(1, 0)),
                T("hipL.rz", K(0, -0.1), K(0.6, -0.04), K(1, 0)),
                T("hipR.rz", K(0, -0.38), K(0.6, -0.14), K(1, 0)),
                T("spine.rx", K(0, 0.06), K(0.45, 0.34), K(0.85, -0.12), K(1, 0)),
                T("spine.ry", K(0, 0.1), K(1, 0)),
                T("spine.rz", K(0, 0.22), K(0.6, 0.08), K(1, 0)),
                T("shoulderL.rx", K(0, -2.1), K(0.35, -1.3), K(0.7, -0.5), K(1, -0.1)),
                T("shoulderR.rx", K(0, 0.12), K(0.35, -0.55), K(0.7, -0.4), K(1, -0.14)),
                T("shoulderL.rz", K(0, -0.3), K(0.6, -0.14), K(1, 0)),
                T("shoulderR.rz", K(0, -0.32), K(0.6, -0.12), K(1, 0)),
                T("elbowL.rx", K(0, -0.22), K(0.35, -0.95), K(0.7, -0.5), K(1, -0.15)),
                T("elbowR.rx", K(0, -0.25), K(0.35, -0.7), K(1, -0.15)),
                T("neck.rx", K(0, 0.16), K(0.5, -0.22), K(1, 0)),
                T("neck.ry", K(0, 0.62), K(0.6, 0.2), K(1, 0)),
                T("neck.rz", K(0, 0.36), K(0.6, 0.1), K(1, 0)),
                T("cape.rx", K(0, -0.1), K(0.5, 0.18), K(1, 0.02)),
                T("cape.rz", K(0, -0.22), K(0.6, -0.06), K(1, 0)),
                CapeFlat(K(0, 1), K(0.4, 0.4), K(1, 0)))
        };

        static readonly Dictionary<string, HeroClip> byName = Build();
        static Dictionary<string, HeroClip> Build()
        {
            var d = new Dictionary<string, HeroClip>();
            foreach (var c in All) d[c.Name] = c;
            return d;
        }

        public static HeroClip Get(string name)
        {
            HeroClip c;
            return name != null && byName.TryGetValue(name, out c) ? c : null;
        }

        /// <summary>GLB 클립 이름 → 프로시저럴 클립 매핑(정본 `CLIP_ALIAS` · 순서가 곧 우선순위).</summary>
        public static readonly KeyValuePair<Regex, string>[] Alias =
        {
            A("Slice_Diagonal|Slice_Horizontal", "slash"),
            A("1H_Melee_Attack_Chop", "chop"),
            A("Stab", "thrust"),
            A("2H_Melee", "slam"),
            A("Dualwield", "double"),
            A("2H_Ranged", "bow"),
            A("1H_Ranged", "gun"),
            A("Spellcast", "cast"),
            A("^Throw$", "throw"),
            A("Death", "Death"),
            A("Revive|GetUp", "Revive"),
            A("Walking|Running", "Walking"),
            A("Idle", "Idle"),
        };
        static KeyValuePair<Regex, string> A(string re, string name) { return new KeyValuePair<Regex, string>(new Regex(re), name); }

        /// <summary>정본 `resolveClip(cands)` — 후보마다 «표에 있으면 그것 · 아니면 별칭 순서대로» · 없으면 null.</summary>
        public static string Resolve(IList<string> cands)
        {
            if (cands == null) return null;
            for (int i = 0; i < cands.Count; i++)
            {
                string c = cands[i];
                if (c == null) continue;
                if (byName.ContainsKey(c)) return c;
                for (int j = 0; j < Alias.Length; j++) if (Alias[j].Key.IsMatch(c)) return Alias[j].Value;
            }
            return null;
        }

        /// <summary>정본 `sample(keys, t)` — 키 사이는 그 키의 이징(없으면 smoothstep) · 범위 밖은 끝값.</summary>
        public static double Sample(ClipKey[] keys, double t)
        {
            if (t <= keys[0].T) return keys[0].V;
            for (int i = 1; i < keys.Length; i++)
            {
                if (t <= keys[i].T)
                {
                    double t0 = keys[i - 1].T, v0 = keys[i - 1].V, t1 = keys[i].T, v1 = keys[i].V;
                    double u = (t - t0) / Math.Max(0.0001, t1 - t0);
                    double k = HeroEase.ByName(keys[i].Ease, u);
                    return v0 + (v1 - v0) * k;
                }
            }
            return keys[keys.Length - 1].V;
        }
    }
}
