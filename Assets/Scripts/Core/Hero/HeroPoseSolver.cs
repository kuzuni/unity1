using System;
using System.Collections.Generic;

namespace Forge.Core.Hero
{
    /// <summary>본 하나의 포즈(three 로컬: 오일러 XYZ 라디안 · 위치 · 배율) — 정본 `rec(o)` 의 {rx..sz}.</summary>
    public sealed class BonePose
    {
        public double Rx, Ry, Rz, Px, Py, Pz, Sx = 1, Sy = 1, Sz = 1;

        public BonePose() { }
        public BonePose(double rx, double ry, double rz, double px, double py, double pz, double sx, double sy, double sz)
        {
            Rx = rx; Ry = ry; Rz = rz; Px = px; Py = py; Pz = pz; Sx = sx; Sy = sy; Sz = sz;
        }

        public void Set(BonePose b)
        {
            Rx = b.Rx; Ry = b.Ry; Rz = b.Rz; Px = b.Px; Py = b.Py; Pz = b.Pz; Sx = b.Sx; Sy = b.Sy; Sz = b.Sz;
        }

        public BonePose Clone() { return new BonePose(Rx, Ry, Rz, Px, Py, Pz, Sx, Sy, Sz); }
    }

    /// <summary>거치 자세 한 칸 — 정본은 «숫자면 rx 가산 · 객체면 {rx, ry, rz} 축별 가산». <see cref="IsNumber"/> 가 그 구분(applyWeaponGrip 의 elbowFix 가 본다).</summary>
    public sealed class PoseDelta
    {
        public double Rx, Ry, Rz;
        public bool IsNumber;
        public static PoseDelta Number(double rx) { return new PoseDelta { Rx = rx, IsNumber = true }; }
        public static PoseDelta Of(double rx, double ry, double rz) { return new PoseDelta { Rx = rx, Ry = ry, Rz = rz }; }
        public PoseDelta Clone() { return new PoseDelta { Rx = Rx, Ry = Ry, Rz = Rz, IsNumber = IsNumber }; }
    }

    /// <summary>본 이름 → <see cref="PoseDelta"/>(정본 `restPose`·`ridePose`·플린치 가산 객체). 순서 보존(Object.assign 합성 순서).</summary>
    public sealed class PoseAdd
    {
        public readonly List<KeyValuePair<string, PoseDelta>> Items = new List<KeyValuePair<string, PoseDelta>>();

        public PoseAdd Add(string bone, PoseDelta d)
        {
            for (int i = 0; i < Items.Count; i++)
                if (Items[i].Key == bone) { Items[i] = new KeyValuePair<string, PoseDelta>(bone, d); return this; }
            Items.Add(new KeyValuePair<string, PoseDelta>(bone, d));
            return this;
        }

        public PoseAdd Add(string bone, double rx) { return Add(bone, PoseDelta.Number(rx)); }
        public PoseAdd Add(string bone, double rx, double ry, double rz) { return Add(bone, PoseDelta.Of(rx, ry, rz)); }

        public PoseDelta Get(string bone)
        {
            for (int i = 0; i < Items.Count; i++) if (Items[i].Key == bone) return Items[i].Value;
            return null;
        }

        /// <summary>정본 `Object.assign({}, a, b)` — b 의 키가 a 를 덮는다(null 은 빈 것). 둘 다 null 이면 null.</summary>
        public static PoseAdd Merge(PoseAdd a, PoseAdd b)
        {
            if (a == null && b == null) return null;
            var r = new PoseAdd();
            if (a != null) foreach (var kv in a.Items) r.Add(kv.Key, kv.Value);
            if (b != null) foreach (var kv in b.Items) r.Add(kv.Key, kv.Value);
            return r;
        }
    }

    /// <summary>
    /// 정본 `ProChar` 의 재생기(`play` · `update` · `hit`)를 순수 C# 으로 — 매 프레임 베이스 포즈에서 시작해 restX → 거치/탑승 자세 → 클립 트랙 오프셋 →
    /// 어깨 클램프(통짜 박스 팔) → 피격 플린치를 얹는다. 유니티 Transform 은 모른다(Game <c>HeroRig</c> 가 <see cref="Bones"/> 를 읽어 적용).
    /// 본 이름·순서는 정본 `R.bones`(pelvis · hipL · kneeL · hipR · kneeR · spine · cape · shoulderL · elbowL · shoulderR · elbowR · neck · head) + root.
    /// </summary>
    public sealed class HeroPoseSolver
    {
        public const double ClampRxMin = -2.95, ClampRxMax = 2.35, ClampRzMin = -0.5, ClampRzMax = 0.6;
        public const double HitDuration = 0.30, HitRise = 0.06;
        public const double GripBlendIn = 0.16, GripBlendOut = 0.74;

        public readonly List<string> BoneNames = new List<string>();
        public readonly Dictionary<string, BonePose> Bones = new Dictionary<string, BonePose>();
        public readonly Dictionary<string, BonePose> Base = new Dictionary<string, BonePose>();
        public readonly BonePose Root = new BonePose();
        public readonly BonePose BaseRoot = new BonePose();

        /// <summary>무기별 오른어깨 거치 rx(정본 `R.restX` · refreshHeroEquip 이 `WEAPON_TYPES.restX + 0.25` 로 준다).</summary>
        public double RestX;
        /// <summary>무기 거치 + 탑승 합성 자세(정본 `R.restPose`).</summary>
        public PoseAdd RestPose;
        /// <summary>탑승 하체 자세(정본 `R.ridePose` · 공격 클립 중에도 유지).</summary>
        public PoseAdd RidePose;
        /// <summary>통짜 박스 리그(정본 `R._simple`) — 어깨 클램프를 건다.</summary>
        public bool Simple = true;

        public HeroClip Clip { get; private set; }
        public double T { get; private set; }
        public bool Once { get; private set; }
        public double Speed { get; private set; } = 1;
        public string State { get; private set; } = "";
        Action onDone;

        public double HitT, HitDur, HitAmp;

        /// <summary>마지막 update 의 정규화 시각(0~1 · once 는 1 에서 멈춘다).</summary>
        public double ClipT { get; private set; }

        public HeroPoseSolver(HeroRigSpec spec)
        {
            foreach (var b in spec.Bones)
            {
                BoneNames.Add(b.Name);
                Base[b.Name] = b.Base.Clone();
                Bones[b.Name] = b.Base.Clone();
            }
            BaseRoot.Set(spec.RootBase);
            Root.Set(spec.RootBase);
        }

        public BonePose Bone(string name)
        {
            BonePose b;
            return Bones.TryGetValue(name, out b) ? b : null;
        }

        /// <summary>정본 `play(R, cands, once, timeScale, onDone)` — 루프 클립은 같은 상태면 재시작하지 않는다.</summary>
        public bool Play(IList<string> cands, bool once = false, double timeScale = 0, Action done = null)
        {
            string name = HeroClips.Resolve(cands);
            if (name == null) return false;
            var clip = HeroClips.Get(name);
            if (!once && !clip.Once && State == name) return false;
            Clip = clip;
            T = 0;
            Once = once || clip.Once;
            Speed = timeScale != 0 ? timeScale : 1;
            onDone = done;
            State = Once ? "" : name;
            return true;
        }

        public bool Play(string cand, bool once = false, double timeScale = 0, Action done = null)
        {
            return Play(new[] { cand }, once, timeScale, done);
        }

        /// <summary>정본 `hit(R, sev)` — sev = 최대 HP 대비 피해 비중.</summary>
        public void Hit(double sev)
        {
            HitT = HitDur = HitDuration;
            HitAmp = 0.5 + Math.Min(0.5, sev * 1.6);
        }

        void AddPose(PoseAdd pose, double s)
        {
            if (s == 0 || pose == null) return;
            foreach (var kv in pose.Items)
            {
                BonePose b;
                if (!Bones.TryGetValue(kv.Key, out b)) continue;
                var v = kv.Value;
                if (v.IsNumber) b.Rx += v.Rx * s;
                else
                {
                    if (v.Rx != 0) b.Rx += v.Rx * s;
                    if (v.Ry != 0) b.Ry += v.Ry * s;
                    if (v.Rz != 0) b.Rz += v.Rz * s;
                }
            }
        }

        /// <summary>정본 `update(R, dt)`.</summary>
        public void Update(double dt)
        {
            if (Clip == null) return;
            T += dt * Speed;
            double t = T / Clip.Dur;
            if (t >= 1)
            {
                if (Once)
                {
                    t = 1;
                    if (onDone != null) { var f = onDone; onDone = null; f(); }
                }
                else t -= Math.Floor(t);
            }
            ClipT = t;
            // 베이스 포즈에서 시작
            foreach (var name in BoneNames) Bones[name].Set(Base[name]);
            Root.Set(BaseRoot);
            if (RestX != 0) Bones["shoulderR"].Rx += RestX;
            // (onDone 이 play 를 불러 클립이 바뀌었을 수 있다 — 정본도 그 뒤로는 새 R._clip/R._once 를 읽는다)
            if (!Once) { if (RestPose != null) AddPose(RestPose, 1); }
            else if (Clip.GroundPose) { }
            else if (RidePose != null) AddPose(RidePose, 1);
            else
            {
                double u = t < GripBlendIn ? 1 - t / GripBlendIn : (t > GripBlendOut ? (t - GripBlendOut) / (1 - GripBlendOut) : 0);
                if (u > 0 && RestPose != null) AddPose(RestPose, HeroEase.Smooth(u));
            }
            bool rideLower = RidePose != null && Once && !Clip.GroundPose;
            var tracks = Clip.Tracks;
            for (int i = 0; i < tracks.Length; i++)
            {
                var tr = tracks[i];
                BonePose bone;
                if (tr.Bone == "root") bone = Root;
                else if (!Bones.TryGetValue(tr.Bone, out bone)) continue;
                if (rideLower && (tr.Bone == "hipL" || tr.Bone == "hipR" || tr.Bone == "kneeL" || tr.Bone == "kneeR")) continue;
                double v = HeroClips.Sample(tr.Keys, t);
                switch (tr.Channel)
                {
                    case "rx": bone.Rx += v; break;
                    case "ry": bone.Ry += v; break;
                    case "rz": bone.Rz += v; break;
                    case "px": bone.Px += v; break;
                    case "py": bone.Py += v; break;
                    case "pz": bone.Pz += v; break;
                    case "sx": bone.Sx *= v; break;
                    case "sy": bone.Sy *= v; break;
                    case "sz": bone.Sz *= v; break;
                }
            }
            // 통짜 박스 팔 — 어깨가 몸 안쪽으로 크게 돌면 머리를 관통한다(rz 클램프 · rx 는 정본이 풀어 둔 범위).
            if (Simple && !(Clip != null && Clip.GroundPose))
            {
                foreach (var bn in ShoulderNames)
                {
                    BonePose b;
                    if (!Bones.TryGetValue(bn, out b)) continue;
                    b.Rx = Math.Max(ClampRxMin, Math.Min(ClampRxMax, b.Rx));
                    b.Rz = Math.Max(ClampRzMin, Math.Min(ClampRzMax, b.Rz));
                }
            }
            // 피격 플린치 — 클립 뒤에 가산(공격 중에 맞아도 반동이 산다) · 쓰러진 시체는 제외
            if (HitT > 0 && !(Clip != null && Clip.GroundPose))
            {
                HitT = Math.Max(0, HitT - dt);
                double v = 1 - HitT / HitDur;
                double w;
                if (v < HitRise) w = v / HitRise;
                else { double uu = (v - HitRise) / (1 - HitRise); w = Math.Exp(-3.6 * uu) * Math.Cos(uu * Math.PI * 2.4); }
                double a = HitAmp * w;
                if (a != 0) AddPose(Flinch(a), 1);
            }
        }

        static readonly string[] ShoulderNames = { "shoulderL", "shoulderR" };

        /// <summary>정본 플린치 가산표(적은 +x 에 서므로 영웅은 왼쪽으로 접힌다).</summary>
        public static PoseAdd Flinch(double a)
        {
            return new PoseAdd()
                .Add("spine", -a * 0.15, 0, a * 0.20)
                .Add("neck", -a * 0.22, 0, a * 0.30)
                .Add("head", 0, 0, a * 0.16)
                .Add("shoulderL", a * 0.46, 0, a * 0.30)
                .Add("shoulderR", a * 0.34, 0, a * 0.24)
                .Add("elbowL", -a * 0.30, 0, 0)
                .Add("elbowR", -a * 0.18, 0, 0)
                .Add("hipL", -a * 0.13, 0, 0).Add("hipR", -a * 0.13, 0, 0)
                .Add("kneeL", -a * 0.28, 0, 0).Add("kneeR", -a * 0.24, 0, 0)
                .Add("cape", a * 0.30, 0, 0);
        }
    }
}
