using System;
using Forge.Core.Data;
using Forge.Core.Hero;

namespace Forge.Core.Mounts
{
    /// <summary>정본 `barReach`/`reinReach` — 빈 손이 핸들바/고삐를 잡는 팔 각 `{shoulder, shoulderZ, elbow}`. T6 <see cref="FreeHandReach"/> 로 넘긴다.</summary>
    public sealed class MountReach
    {
        public double Shoulder, ShoulderZ, Elbow;

        public static MountReach From(JsonObject o)
        {
            if (o == null) return null;
            return new MountReach { Shoulder = J.Num(o["shoulder"]), ShoulderZ = J.Num(o["shoulderZ"]), Elbow = J.Num(o["elbow"]) };
        }

        public FreeHandReach ToReach() { return new FreeHandReach { Shoulder = Shoulder, ShoulderZ = ShoulderZ, Elbow = Elbow }; }
    }

    /// <summary>
    /// 정본 `Scene3D.MOUNT_FORMS` 한 계열(flat · wheeled · fly · quad · biped) — saddle(안장 윗면 로컬 높이 · 칸 크기의 기준) · hover(뜨는 높이) · stand(발판 위에 서는 유일한 계열 = flat) ·
    /// bulk(비행형 몸집 배수) · seatByLeg(자전거 피팅 — 안장 높이를 다리 길이로) · noNarrow(가로 좁히기 끔 = biped) · pose(탑승 하체 자세 · 서서 타기가 아닐 때만 쓴다) · barReach/reinReach.
    /// 없는 칸은 JS undefined 와 같다(`HasBulk` 가 그 구분 · `form.bulk || 1.7`).
    /// </summary>
    public sealed class MountForm
    {
        public string Key;
        public double Saddle, Hover;
        public bool Stand, SeatByLeg, NoNarrow;
        public bool HasBulk;
        public double Bulk;
        public PoseAdd Pose;
        public MountReach BarReach, ReinReach;
        public JsonObject Raw;

        public static MountForm From(string key, JsonObject o)
        {
            var f = new MountForm { Key = key, Raw = o };
            object v;
            f.Saddle = J.Num(o["saddle"]);
            f.Hover = J.Num(o["hover"]);
            f.Stand = J.Bool(o["stand"]);
            f.SeatByLeg = J.Bool(o["seatByLeg"]);
            f.NoNarrow = J.Bool(o["noNarrow"]);
            if (o.TryGet("bulk", out v) && v is double) { f.HasBulk = true; f.Bulk = (double)v; }
            f.Pose = MountRideDefs.ParsePose(J.Obj(o["pose"]));
            f.BarReach = MountReach.From(J.Obj(o["barReach"]));
            f.ReinReach = MountReach.From(J.Obj(o["reinReach"]));
            return f;
        }

        /// <summary>정본 `mountFormOf` 의 `Object.assign({}, form, { saddle: sd })` — 얕은 복사에 안장만 덮는다(호출부는 전부 읽기 전용).</summary>
        public MountForm WithSaddle(double saddle)
        {
            return new MountForm
            {
                Key = Key, Saddle = saddle, Hover = Hover, Stand = Stand, SeatByLeg = SeatByLeg, NoNarrow = NoNarrow,
                HasBulk = HasBulk, Bulk = Bulk, Pose = Pose, BarReach = BarReach, ReinReach = ReinReach, Raw = Raw,
            };
        }
    }

    /// <summary>
    /// 탑승 상수표(정본 `scene3d.js` 10420~10630 · 10735) — `scene.json`(T2 추출기 scene 갈래 · T11 9키) 강타입. 값은 전부 여기서 읽는다 — 코드에는 안 박는다(§1).
    /// `MOUNT_FORMS` 계열 표 · `MOUNT_FORM_OF` 종→계열(없는 종은 quad) · `MOUNT_SADDLE_OF` 종별 안장(사족형만 · 없으면 계열값) · `RIDE_STAND_POSE`/`RIDE_STAND_BULK`(서서 타기 · 주인 지시 2026-08-21) ·
    /// `RIDE_SEAT_RATIO`(안장 ÷ 골반 1.45) · `RIDE_WIDTH_RATIO`(지상형 가로 배율 0.74) · `MOUNT_GAIT`/`MOUNT_IDLE_GAIT`(걸음 각속도).
    /// 인라인 리터럴(규칙)은 <see cref="MountRideRules"/>·<see cref="MountPartDriver"/>.
    /// </summary>
    public sealed class MountRideDefs
    {
        public const string DefaultForm = "quad";

        public OrderedMap<MountForm> Forms;
        public OrderedMap<string> FormOf;
        public OrderedMap<double> SaddleOf;
        public PoseAdd StandPose;
        public double StandBulk, SeatRatio, WidthRatio, Gait, IdleGait;

        /// <summary>scene.json 최상위 객체(<c>SceneDefs.Raw</c>)에서 읽는다 — 9키가 없으면 정본이 바뀐 것이라 예외.</summary>
        public static MountRideDefs From(JsonObject scene)
        {
            var d = new MountRideDefs();
            var forms = J.Obj(J.Require(scene, "MOUNT_FORMS"));
            d.Forms = new OrderedMap<MountForm>();
            foreach (string k in forms.Keys) d.Forms.Add(k, MountForm.From(k, J.Obj(forms[k])));
            var of = J.Obj(J.Require(scene, "MOUNT_FORM_OF"));
            d.FormOf = new OrderedMap<string>();
            foreach (string k in of.Keys) d.FormOf.Add(k, J.Str(of[k]));
            var sd = J.Obj(J.Require(scene, "MOUNT_SADDLE_OF"));
            d.SaddleOf = new OrderedMap<double>();
            foreach (string k in sd.Keys) d.SaddleOf.Add(k, J.Num(sd[k]));
            d.StandPose = ParsePose(J.Obj(J.Require(scene, "RIDE_STAND_POSE")));
            d.StandBulk = J.Num(J.Require(scene, "RIDE_STAND_BULK"));
            d.SeatRatio = J.Num(J.Require(scene, "RIDE_SEAT_RATIO"));
            d.WidthRatio = J.Num(J.Require(scene, "RIDE_WIDTH_RATIO"));
            d.Gait = J.Num(J.Require(scene, "MOUNT_GAIT"));
            d.IdleGait = J.Num(J.Require(scene, "MOUNT_IDLE_GAIT"));
            MountForm quad; if (!d.Forms.TryGet(DefaultForm, out quad)) throw new ArgumentException("MOUNT_FORMS 에 기본 계열 «" + DefaultForm + "» 이 없다");
            return d;
        }

        /// <summary>정본 `mountFormOf(name)` — `MOUNT_FORMS[MOUNT_FORM_OF[name] || 'quad']` 에 종별 안장(`MOUNT_SADDLE_OF`)이 있고 계열값과 다르면 안장만 덮은 사본.</summary>
        public MountForm FormOfName(string name)
        {
            string key = null;
            if (name != null) FormOf.TryGet(name, out key);
            MountForm form;
            if (string.IsNullOrEmpty(key) || !Forms.TryGet(key, out form)) form = Forms[DefaultForm];
            double sd;
            if (name != null && SaddleOf.TryGet(name, out sd) && sd != form.Saddle) return form.WithSaddle(sd);
            return form;
        }

        /// <summary>정본 자세 객체 `{hipL:{rx,ry,rz}, …}` → <see cref="PoseAdd"/>(본 순서 보존 · 칸은 전부 객체라 축별 가산).</summary>
        public static PoseAdd ParsePose(JsonObject o)
        {
            if (o == null) return null;
            var p = new PoseAdd();
            foreach (string bone in o.Keys)
            {
                var d = J.Obj(o[bone]);
                if (d == null) continue;
                p.Add(bone, PoseDelta.Of(J.Num(d["rx"]), J.Num(d["ry"]), J.Num(d["rz"])));
            }
            return p;
        }
    }
}
