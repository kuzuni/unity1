using System.Collections.Generic;

namespace Forge.Core.Data
{
    /// <summary>
    /// 조형 표 한 종 — 정본 `mobs-pets.js`·`mobs-mounts.js`·`mobs-enemies.js`·`mobs-skillfx.js` 의 항목을 T2 가 JSON 으로 낸 것.
    /// 좌표·색은 손대지 않고 그대로 든다(paint 의 음수 인덱스·mx 거울도 안 푼다 — T4 `VoxelMob` 몫). 종 좌표는 유니티에서 고치지 않는다(§1 조형 계약).
    /// 탈것 표에는 `cell` 이 없다(칸 크기는 씬 쪽 `MOUNT_FORMS` 가 정한다 · T2 완료 기록 ⓐ) — 그때 <see cref="Cell"/> 은 0.
    /// </summary>
    public sealed class MobModel
    {
        public string Name;
        /// <summary>칸 한 변(세계 단위). 없으면 0(탈것).</summary>
        public double Cell;
        public List<MobPart> Parts = new List<MobPart>();

        // ---- 탈것 전용 ----
        public double? Seat;
        /// <summary>quad / fly / wheeled / biped … (탈것).</summary>
        public string Form;
        public string Harness;
        public MobHead Head;
        public bool NoBridle;
        public bool Flat;
        public double[] Bar;

        // ---- 적 전용 머리 키 ----
        public bool Jelly;
        public bool Fly;
        public bool Hop;

        /// <summary>원문 객체 — 위에 안 옮긴 키는 여기서 읽는다.</summary>
        public JsonObject Raw;

        public MobPart Part(string id)
        {
            for (int i = 0; i < Parts.Count; i++) if (Parts[i].Id == id) return Parts[i];
            return null;
        }

        public static MobModel From(string name, JsonObject o)
        {
            var m = new MobModel { Name = name, Raw = o };
            m.Cell = J.Num(o["cell"]);
            m.Seat = J.NumOrNull(o["seat"]);
            m.Form = J.Str(o["form"]);
            m.Harness = J.Str(o["harness"]);
            var head = J.Obj(o["head"]);
            if (head != null) m.Head = new MobHead { Hw = J.Num(head["hw"]), Y = J.Num(head["y"]), Z = J.Num(head["z"]), D = J.Num(head["d"]), Raw = head };
            m.NoBridle = J.Bool(o["noBridle"]);
            m.Flat = J.Bool(o["flat"]);
            m.Bar = J.NumArr(o["bar"]);
            m.Jelly = J.Bool(o["jelly"]);
            m.Fly = J.Bool(o["fly"]);
            m.Hop = J.Bool(o["hop"]);
            var parts = J.Arr(o["parts"]);
            if (parts != null) for (int i = 0; i < parts.Count; i++) m.Parts.Add(MobPart.From(J.Obj(parts[i])));
            return m;
        }
    }

    /// <summary>탈것 머리 자리(`head:{hw,y,z,d}`) — 고삐·눈 배치를 씬이 쓴다.</summary>
    public sealed class MobHead
    {
        public double Hw, Y, Z, D;
        public JsonObject Raw;
    }

    /// <summary>
    /// 파츠 하나 = 축정렬 직육면체 하나. `box`(칸 수) · `at`(중심 칸 좌표) · `c`(0xRRGGBB) · `paint`(칸 색) · `pivot`(관절 원점) · `parent`(부모 파츠 id) ·
    /// `joint`(제너릭 관절 드라이버) · `tag`/`gait`/`s`/`head` 는 애니 계약(§1 — 이름을 바꾸면 보행·공격이 조용히 죽는다).
    /// </summary>
    public sealed class MobPart
    {
        public string Id;
        public int[] Box;
        public double[] At;
        public int C;
        public List<MobPaint> Paint = new List<MobPaint>();
        public MobMat Mat;
        public string Parent;
        public double[] Pivot;
        public double[] Rot;
        public MobJoint Joint;
        public string Tag;
        public double? S;
        public double? Gait;
        public bool Head;
        public JsonObject Raw;

        public static MobPart From(JsonObject o)
        {
            var p = new MobPart { Raw = o };
            p.Id = J.Str(o["id"]);
            p.Box = J.IntArr(o["box"]);
            p.At = J.NumArr(o["at"]);
            p.C = J.Int(o["c"]);
            var paint = J.Arr(o["paint"]);
            if (paint != null) for (int i = 0; i < paint.Count; i++) p.Paint.Add(MobPaint.From(J.Obj(paint[i])));
            var mat = J.Obj(o["mat"]);
            if (mat != null) p.Mat = MobMat.From(mat);
            p.Parent = J.Str(o["parent"]);
            p.Pivot = J.NumArr(o["pivot"]);
            p.Rot = J.NumArr(o["rot"]);
            var joint = J.Obj(o["joint"]);
            if (joint != null) p.Joint = MobJoint.From(joint);
            p.Tag = J.Str(o["tag"]);
            p.S = J.NumOrNull(o["s"]);
            p.Gait = J.NumOrNull(o["gait"]);
            p.Head = J.Bool(o["head"]);
            return p;
        }
    }

    /// <summary>칸 색칠 한 줄: 색 `c` + 축별 범위(x/y/z · 수 하나 또는 [lo,hi] · 음수는 끝에서) + `mx`(x 거울). 값은 원문 그대로다.</summary>
    public sealed class MobPaint
    {
        public int C;
        public PaintAxis X, Y, Z;
        public bool Mx;

        public static MobPaint From(JsonObject o)
        {
            return new MobPaint { C = J.Int(o["c"]), X = PaintAxis.From(o["x"]), Y = PaintAxis.From(o["y"]), Z = PaintAxis.From(o["z"]), Mx = J.Bool(o["mx"]) };
        }
    }

    /// <summary>paint 축 지정 — 수 하나(`y: 0`)면 <see cref="IsRange"/> 가 false 이고 Lo = Hi · `[lo, hi]` 면 true. 없는 축은 null.</summary>
    public sealed class PaintAxis
    {
        public int Lo, Hi;
        public bool IsRange;

        public static PaintAxis From(object v)
        {
            if (v is double) { int n = (int)(double)v; return new PaintAxis { Lo = n, Hi = n, IsRange = false }; }
            var a = J.IntArr(v);
            if (a == null || a.Length == 0) return null;
            return new PaintAxis { Lo = a[0], Hi = a.Length > 1 ? a[1] : a[0], IsRange = true };
        }
    }

    /// <summary>재질 성질(`mat`) — 없는 칸은 null. 같은 성질끼리 Material 을 공유한다(T4).</summary>
    public sealed class MobMat
    {
        public double? Opacity;
        public double? Rough;
        public int? Emissive;
        public double? EmissiveIntensity;
        public JsonObject Raw;

        public static MobMat From(JsonObject o)
        {
            var e = J.NumOrNull(o["emissive"]);
            return new MobMat
            {
                Opacity = J.NumOrNull(o["opacity"]),
                Rough = J.NumOrNull(o["rough"]),
                Emissive = e.HasValue ? (int?)(int)e.Value : null,
                EmissiveIntensity = J.NumOrNull(o["emissiveIntensity"]),
                Raw = o
            };
        }
    }

    /// <summary>제너릭 관절 드라이버 서술(`joint:{axis, amp, f, ph, gain, abs, spin}`) — 없는 칸은 null(씬이 원작 기본값을 넣는다).</summary>
    public sealed class MobJoint
    {
        public string Axis;
        public double? Amp, F, Ph, Gain;
        public bool Abs, Spin;
        public JsonObject Raw;

        public static MobJoint From(JsonObject o)
        {
            return new MobJoint
            {
                Axis = J.Str(o["axis"]),
                Amp = J.NumOrNull(o["amp"]), F = J.NumOrNull(o["f"]), Ph = J.NumOrNull(o["ph"]), Gain = J.NumOrNull(o["gain"]),
                Abs = J.Bool(o["abs"]), Spin = J.Bool(o["spin"]),
                Raw = o
            };
        }
    }

    /// <summary>종 표 하나(펫 25 · 탈것 29 · 적 7 · 스킬 오브젝트 20) — 정본 삽입 순서 그대로.</summary>
    public sealed class MobTable
    {
        public readonly OrderedMap<MobModel> Models = new OrderedMap<MobModel>();
        public int Count { get { return Models.Count; } }
        public IReadOnlyList<string> Names { get { return Models.Keys; } }
        public MobModel Get(string name) { return Models.Get(name, null); }
        public MobModel this[string name] { get { return Models[name]; } }

        public static MobTable From(JsonObject o)
        {
            var t = new MobTable();
            foreach (var kv in o) t.Models.Add(kv.Key, MobModel.From(kv.Key, J.Obj(kv.Value)));
            return t;
        }
    }
}
