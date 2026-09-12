using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Forge.Core.Battle;
using Forge.Core.Data;
using Forge.Core.Voxel;
using Forge.Game.Audio;
using Forge.Game.Voxel;

namespace Forge.Game.SkillFx
{
    /// <summary>연출이 전투 무대에서 읽고 미는 것(원작 `Scene3D` 의 `heroG`·`enemyMap`·`spawnSparks`·`shake`·`fovPunch`). 좌표는 전부 three(정본) 좌표.</summary>
    public interface ISkillFxStage
    {
        Vector3 HeroPos { get; }
        /// <summary>살아 있는 적의 자리(원작 `enemyMap.get(id)`) — 죽었거나 없으면 false.</summary>
        bool TryEnemyPos(int id, out Vector3 threePos);
        /// <summary>시전 순간의 표적 — `single` 은 우선 표적 하나 · `aoe` 는 사거리 안 전부(원작 `Combat.tryCast`).</summary>
        List<int> Targets(string type);
        void Sparks(Vector3 threePos, int count, int hex, double speed, double scale);
        void Shards(Vector3 threePos, int count, int hex, double dir, double spread, double speed, double scale);
        void Shake(double mag);
        void FovPunch(double amount, double dur);
        /// <summary>살아 있는 파편 수(원작 `particles.length` — 붐비면 모트 수를 줄인다).</summary>
        int ParticleCount { get; }
        /// <summary>T52 영웅 젖힘 채널(원작 `heroG.rotation.z`) — 평타·넉백이 쥐고 있으면 <see cref="HeroLeanBusy"/>(원작 `_attacking` 양보).</summary>
        bool HeroLeanBusy { get; }
        double HeroLeanZ { get; }
        void HeroLean(double z);
    }

    public enum FxLogKind { Cast, Payload, Actor, Hit, Weight, Free }

    public sealed class FxLog
    {
        public double T; public FxLogKind Kind; public string Tag; public Vector3 Pos; public bool Big;
        public override string ToString() { return T.ToString("0.000", CultureInfo.InvariantCulture) + " " + Kind + " " + Tag + (Big ? " big" : ""); }
    }

    /// <summary>시전 한 번의 기록(테스트·검증용) — 시전 시각 · 페이로드 시각 · 첫/결정적 타격 · 무게 시각.</summary>
    public sealed class CastRecord
    {
        public string Id, Fx; public int Tier; public double T0 = -1, PayloadAt = -1, FirstHit = -1, BigHit = -1, WeightAt = -1;
        public int Hits; public readonly HashSet<string> Actors = new HashSet<string>();
    }

    /// <summary>
    /// 원작 `web/js/scene3d-skillfx.js`(마인크래프트식 스킬 액터 안무 22개) + `scene3d.js` 의 스킬 디스패처(`skillEffect`·`skillCastBeat`·`castMsFor`·
    /// `skillPayload`·`skillImpactWeight`·`projectileBolt`·`flashLight`·`fxTrailCube`)를 **함수 이름·시각·수치 그대로** 옮긴 것(T12).
    /// 시간은 <see cref="FxTimeline"/>(게임 시간) 위에서 돈다. 액터는 <see cref="SkillActorPool"/>(프로토타입 복제) · 큐브는 <see cref="FxCubes"/> · 광원은 <see cref="FxLights"/>.
    /// 🚨 타이밍은 계약이다 — 시전 박자(`castBeatMs`)·페이로드 지연(`castMsFor`)·무게 시각(`*_IMPACT_MS`)·착탄(`impactAt`)은 한 값도 바꾸지 않는다.
    /// </summary>
    public sealed class SkillFxDirector
    {
        // ── 원작 상수(scene3d.js · scene3d-skillfx.js) ──
        public const double CreatureYaw = 0.55;
        public const double SupportStageX = 1.8, SupportStageDz = 0.6;
        public const double SkillOffscreenX = -8.5;
        public const int CastMs = 130, CastMsMeteor = 0, StormGatherMinMs = 190;
        public const int DragonfireImpactMs = 820, GodspearImpactMs = 500, NovaImpactMs = 560, GuillotineImpactMs = 430, VoidriftImpactMs = 540;
        public const double ThunderTellMaxAge = 2.6;
        /// <summary>T52 시전 포즈(`skillCastBeat` 13,706·13,722~13,728행): 1박 동안 z = −0.14·k²(지원계 −0.07) · 릴리즈는 0.14초 동안 앞 30% 에 +0.30 내지르고 나머지 70% 로 0 까지 감쇠.</summary>
        public const double CastLeanZ = 0.14, CastLeanSupportZ = 0.07, CastReleaseSnap = 0.30, CastReleaseDur = 0.14, CastReleaseRise = 0.3;
        public static readonly string[] Rarities = { "common", "rare", "epic", "legendary", "ultimate", "mythic" };
        /// <summary>`ASCEND_MOTIF` — 티어 1 불 · 2 얼음 · 3 뇌전 · 4 신성 · 5 심연(0 은 없음).</summary>
        public static readonly int[] AscendMotif = { -1, 0xff7a2a, 0x9fd8ff, 0xc9a0ff, 0xffe9a8, 0x8a4dff };
        public const double AscendLerp = 0.55;

        readonly GameData data;
        readonly ISkillFxStage stage;
        readonly Transform parent;
        readonly Rng rng;
        public readonly FxTimeline Timeline = new FxTimeline();
        public readonly SkillActorPool Pool;
        public readonly FxLights Lights;
        /// <summary>승천 별 수(`S.skills[id].stars`) — null 이면 승천 색 변주 없음(T17/T24 접착부가 꽂는다).</summary>
        public Func<string, int> StarsOf;
        /// <summary>`skillScreenFlash(color, tier)` — 화면 플래시는 UI 층(T22/T27) 몫이라 훅으로만 알린다.</summary>
        public Action<int, int> ScreenFlash;

        public readonly List<FxLog> Log = new List<FxLog>();
        public readonly List<CastRecord> Casts = new List<CastRecord>();
        CastRecord cur;
        readonly List<FxCube> cubes = new List<FxCube>();
        readonly Dictionary<string, Mesh> arrowMesh = new Dictionary<string, Mesh>();
        string pendingCutin;

        public double Now { get { return Timeline.Now; } }
        public int CubeCount { get { return cubes.Count; } }
        public bool Idle { get { return Timeline.Idle && Pool.Active == 0 && cubes.Count == 0; } }

        public SkillFxDirector(GameData data, ISkillFxStage stage, Transform parent, Rng rng)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (stage == null) throw new ArgumentNullException("stage");
            if (rng == null) throw new ArgumentNullException("rng");
            this.data = data; this.stage = stage; this.parent = parent; this.rng = rng;
            Pool = new SkillActorPool(data.SkillFx, parent);
            Lights = new FxLights(parent);
        }

        // ── 진입 ──

        /// <summary>T7 이벤트 → 원작 `Combat.tryCast` 가 `Scene3D.skillEffect(d.fx, d.color, targetIds, d)` 를 부르던 자리.</summary>
        public void Handle(BattleEvent e)
        {
            switch (e.Kind)
            {
                case BattleEventKind.SkillCutin: pendingCutin = e.Tag; break;
                case BattleEventKind.SkillEffect:
                    {
                        SkillDef d = DefByFx(e.Tag) ?? DefById(pendingCutin);
                        if (d == null) return;
                        List<int> targets = (d.Type == "heal" || d.Type == "buff") ? new List<int>() : stage.Targets(d.Type);
                        Cast(d, targets);
                        break;
                    }
                default: break;
            }
        }

        public SkillDef DefByFx(string fx)
        {
            if (string.IsNullOrEmpty(fx) || data.Defs == null || data.Defs.SkillDefs == null) return null;
            foreach (SkillDef d in data.Defs.SkillDefs) if (d.Fx == fx) return d;
            return null;
        }
        public SkillDef DefById(string id)
        {
            if (string.IsNullOrEmpty(id) || data.Defs == null) return null;
            return data.Defs.Skill(id);
        }

        /// <summary>`skillEffect(fx, colorHex, targetIds, def)`.</summary>
        public CastRecord Cast(SkillDef def, List<int> targetIds)
        {
            if (def == null) throw new ArgumentNullException("def");
            string fx = def.Fx;
            if (string.IsNullOrEmpty(fx)) fx = def.Type == "buff" ? "aura" : "heal";
            int hex = AscendSkillColor(ParseHex(def.Color), def);
            int tier = SkillTier(def);
            var rec = new CastRecord { Id = def.Id, Fx = fx, Tier = tier, T0 = Now };
            Casts.Add(rec); cur = rec;
            Push(FxLogKind.Cast, fx, stage.HeroPos);
            SkillCastBeat(hex, fx, tier);
            ThunderHandle tell = fx == "bolt" ? McThunderTell(targetIds, hex, tier) : null;
            double wait = CastMsFor(fx, tier) / 1000.0;
            if (wait > 0) Timeline.After(wait, () => { cur = rec; SkillPayload(fx, hex, targetIds, tier, tell); });
            else SkillPayload(fx, hex, targetIds, tier, tell);
            return rec;
        }

        public void Step(double dt) { Timeline.Step(dt); }

        /// <summary>씬 리셋 — 액터·큐브·광원 전부 즉시 정리(onDone 을 부르지 않는다).</summary>
        public void Clear()
        {
            Timeline.Clear();
            Pool.FreeAll();
            foreach (var c in new List<FxCube>(cubes)) FxCubes.Kill(c);
            cubes.Clear();
            Lights.ReleaseAll();
            if (!stage.HeroLeanBusy) stage.HeroLean(0);   // 릴리즈 애니를 버렸으니 젖힘도 되돌린다
        }

        // ── 등급 · 시각 ──

        /// <summary>`skillTier(def)` — common0 rare1 epic2 legendary3 ultimate4 mythic5(없으면 1).</summary>
        public static int SkillTier(SkillDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.Rarity)) return 1;
            int i = Array.IndexOf(Rarities, def.Rarity);
            return i < 0 ? 1 : i;
        }
        /// <summary>`castBeatMs(tier)` = 90 + 26·tier.</summary>
        public static int CastBeatMs(int tier) { return 90 + tier * 26; }
        /// <summary>`castMsFor(fx, tier)` — 메테오·용은 0(낙하가 2박) · 낙뢰는 하한 190.</summary>
        public static int CastMsFor(string fx, int tier)
        {
            if (fx == "meteor" || fx == "dragonfire") return CastMsMeteor;
            if (fx == "bolt") return Math.Max(StormGatherMinMs, CastBeatMs(tier));
            return CastBeatMs(tier);
        }
        /// <summary>`skillPayload` 의 무게 지연(ms) — fx 별 `*_IMPACT_MS` · 커먼 3종은 오브젝트 착탄 시각 · 그 밖 30.</summary>
        public static int WeightDelayMs(string fx)
        {
            switch (fx)
            {
                case "dragonfire": return DragonfireImpactMs;
                case "spear": return GodspearImpactMs;
                case "nova": return NovaImpactMs;
                case "guillotine": return GuillotineImpactMs;
                case "voidrift": return VoidriftImpactMs;
                case "shurikenrun": return 1550;
                case "arrowrain": return 1350;
                case "burrowworm": return 1350;
                default: return 30;
            }
        }
        /// <summary>`arrowGapMs(tier)` = 62 − 4·tier.</summary>
        public static int ArrowGapMs(int tier) { return 62 - Math.Max(0, Math.Min(5, tier)) * 4; }
        static int T5(int tier) { return Math.Max(0, Math.Min(5, tier)); }

        /// <summary>`ascendTier(stars)` = stars mod 6.</summary>
        public static int AscendTier(int stars) { return ((stars % 6) + 6) % 6; }
        /// <summary>`ascendSkillColor` — 정의 색을 티어 모티프 색으로 55% 끌어당긴다.</summary>
        public int AscendSkillColor(int hex, SkillDef def)
        {
            if (StarsOf == null || def == null) return hex;
            int m = AscendMotif[AscendTier(StarsOf(def.Id))];
            return m < 0 ? hex : LerpHex(hex, m, AscendLerp);
        }
        public static int ParseHex(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0xffffff;
            string t = s.StartsWith("#", StringComparison.Ordinal) ? s.Substring(1) : s;
            int v; return int.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v) ? v : 0xffffff;
        }
        public static int LerpHex(int a, int b, double k)
        {
            int r = (int)Math.Round(((a >> 16) & 255) + (((b >> 16) & 255) - ((a >> 16) & 255)) * k);
            int g = (int)Math.Round(((a >> 8) & 255) + (((b >> 8) & 255) - ((a >> 8) & 255)) * k);
            int bl = (int)Math.Round((a & 255) + ((b & 255) - (a & 255)) * k);
            return (r << 16) | (g << 8) | bl;
        }

        // ── 도우미(원작 이름 그대로) ──

        void Push(FxLogKind kind, string tag, Vector3 pos, bool big = false) { Log.Add(new FxLog { T = Now, Kind = kind, Tag = tag, Pos = pos, Big = big }); }
        double R(double a, double b) { return rng.Rand(a, b); }
        static Vector3 V(double x, double y, double z) { return new Vector3((float)x, (float)y, (float)z); }
        static double Ease3(double k) { return 1 - Math.Pow(1 - k, 3); }
        Vector3 Hero { get { return stage.HeroPos; } }

        SkillActor Actor(string id, double scale, Vector3 pos, double yaw)
        {
            SkillActor a = Pool.Spawn(id, scale, pos, yaw);
            if (a != null) { Push(FxLogKind.Actor, id, pos); if (cur != null) cur.Actors.Add(id); }
            return a;
        }
        void Free(SkillActor a) { if (a == null || a.Freed) return; Push(FxLogKind.Free, a.Id, a.Pos); Pool.Free(a); }

        FxCube Cube(int hex, double opacity, bool additive, string name = "FxCube")
        {
            FxCube c = FxCubes.Make(parent, hex, opacity, additive, name);
            cubes.Add(c);
            return c;
        }
        void KillCube(FxCube c) { if (c == null) return; cubes.Remove(c); FxCubes.Kill(c); }

        /// <summary>`supportSpot(hero, i, n)` — 지원 소환체 정박 자리(전방-우측 안전지대 · z 부채꼴).</summary>
        public Vector3 SupportSpot(Vector3 hero, int i, int n)
        {
            double mid = (n - 1) / 2.0;
            return V(hero.x + SupportStageX + i * 0.35, 0, hero.z + (i - mid) * SupportStageDz);
        }

        /// <summary>`mcSpots(targetIds)` — 살아 있는 표적 자리 · 다 죽었으면 영웅 앞(1.9).</summary>
        public List<Vector3> McSpots(List<int> targetIds)
        {
            var live = new List<Vector3>();
            if (targetIds != null) foreach (int id in targetIds) { Vector3 p; if (stage.TryEnemyPos(id, out p)) live.Add(p); }
            if (live.Count > 0) return live;
            Vector3 h = Hero;
            return new List<Vector3> { V(h.x + 1.9, h.y, h.z) };
        }
        List<Vector3> LiveSpots(List<int> targetIds)
        {
            var live = new List<Vector3>();
            if (targetIds != null) foreach (int id in targetIds) { Vector3 p; if (stage.TryEnemyPos(id, out p)) live.Add(p); }
            return live;
        }

        /// <summary>`mcDust(pos, n)` — 흙색 큐브.</summary>
        public void McDust(Vector3 pos, int n) { stage.Sparks(V(pos.x, 0.12, pos.z), n <= 0 ? 6 : n, 0x9c8466, 0.7, 0.9); }

        /// <summary>`mcHit(pos, color, tier, big)` — 파편 + 셰이크 + 짧은 광원 + SFX.hit.</summary>
        public void McHit(Vector3 pos, int hex, int tier, bool big)
        {
            int t = T5(tier);
            stage.Sparks(pos, (int)Math.Round((big ? 15 : 8) + t * 3.0), hex, (big ? 1.5 : 1.1) + t * 0.14, big ? 1.15 : 0.95);
            stage.Shake((big ? 0.28 : 0.16) + t * 0.045);
            FlashLight(pos, hex, big ? 0.22 : 0.15);
            Sfx.Hit(big);
            Push(FxLogKind.Hit, cur != null ? cur.Fx : "", pos, big);
            if (cur != null) { cur.Hits++; if (cur.FirstHit < 0) cur.FirstHit = Now; if (big && cur.BigHit < 0) cur.BigHit = Now; }
        }

        /// <summary>`mcFlap(a, phase, amp)` — 날개 z 회전 = s·amp·sin(phase).</summary>
        public static void McFlap(SkillActor a, double phase, double amp = 0.5)
        {
            foreach (string k in new[] { "wingL", "wingR" })
            {
                if (!a.Has(k)) continue;
                a.RotZ(k, a.WingS(k) * amp * Math.Sin(phase));
            }
        }

        /// <summary>`flashLight(pos, colorHex, dur)` — 광원이 내려가는 곡선(2.8 → 0).</summary>
        public void FlashLight(Vector3 pos, int hex, double dur = 0.3)
        {
            FxLights.Lease h = Lights.Take(hex, 7);
            h.Pos(pos.x, pos.y + 0.8, pos.z + 0.5);
            Timeline.Add(dur, k => h.Set(2.8 * (1 - k)), h.Release);
        }

        /// <summary>`mcBlockStream(from, to, n, hex, ms, {size, arc})` — 청키 큐브 n 개가 from → to 로 흐른다(회복·버프의 «전달»).</summary>
        public void McBlockStream(Vector3 from, Vector3 to, int n, int hex, double ms, double size = 0, double arc = 0.32)
        {
            for (int i = 0; i < n; i++)
            {
                double d = (i / (double)n) * (ms * 0.55) / 1000.0;
                Timeline.After(d, () =>
                {
                    FxCube m = Cube(hex, 0.95, false, "stream");
                    double s0 = size > 0 ? size : R(0.09, 0.15);
                    m.SetScale(s0);
                    Vector3 p0 = from + V(R(-0.28, 0.28), R(-0.2, 0.2), R(-0.28, 0.28));
                    Vector3 p1 = to + V(R(-0.2, 0.2), R(-0.15, 0.25), R(-0.2, 0.2));
                    m.SetPos(p0);
                    double sx = R(-6, 6), sy = R(-6, 6), sz = R(-6, 6);
                    double sz0 = size > 0 ? size : 0.12;
                    Timeline.Add((ms * 0.45) / 1000.0, k =>
                    {
                        Vector3 p = Vector3.Lerp(p0, p1, (float)k);
                        p.y += (float)(Math.Sin(k * Math.PI) * arc);
                        m.SetPos(p);
                        m.SetRot(sx * k, sy * k, sz * k);
                        m.SetScale(sz0 * (1 - 0.55 * k * k));
                    }, () => KillCube(m));
                });
            }
        }

        /// <summary>`fxTrailCube(pos, hex, size, life)` — 투사체 잔상 큐브(가산 · 0.28s 줄며 사라짐).</summary>
        public void FxTrailCube(Vector3 pos, int hex, double size, double life = 0.28)
        {
            FxCube m = Cube(hex, 0.8, true, "trail");
            m.SetPos(pos); m.SetScale(size);
            m.SetRot(R(0, 3), R(0, 3), R(0, 3));
            Timeline.Add(life, k => { m.SetOpacity(0.8 * (1 - k)); m.SetScale(size * (1 - 0.55 * k)); }, () => KillCube(m));
        }

        // ── 화살(projectileBolt · voxArrowGeo) ──

        Mesh ArrowMesh(string part)
        {
            Mesh m;
            if (arrowMesh.TryGetValue(part, out m)) return m;
            var vox = new List<VoxelCell>();
            if (part == "head")
            {
                for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++) vox.Add(new VoxelCell(x, 0, z, -1));
                vox.Add(new VoxelCell(0, 1, 0, -1)); vox.Add(new VoxelCell(1, 1, 0, -1)); vox.Add(new VoxelCell(-1, 1, 0, -1));
                vox.Add(new VoxelCell(0, 1, 1, -1)); vox.Add(new VoxelCell(0, 1, -1, -1));
                vox.Add(new VoxelCell(0, 2, 0, -1));
            }
            else
            {
                vox.Add(new VoxelCell(0, 9, 0, -1)); vox.Add(new VoxelCell(1, 9, 0, -1)); vox.Add(new VoxelCell(-1, 9, 0, -1));
                vox.Add(new VoxelCell(0, 9, 1, -1)); vox.Add(new VoxelCell(0, 9, -1, -1));
                foreach (int y in new[] { 8, 7, 6, 5, 4, 3, 1 }) vox.Add(new VoxelCell(0, y, 0, -1));
            }
            var o = VoxelBuildOptions.Default;
            o.Size = part == "head" ? 0.115 : 0.105; o.Color = 0xffffff; o.Jitter = 0.07; o.Ao = 0; o.Center = true; o.LeftHanded = true;
            VoxelMesh vm = VoxelGeometry.Build(vox, o);
            m = VoxelMob.ToMesh(vm, "arrow " + part, QualitySettings.activeColorSpace == ColorSpace.Linear);
            arrowMesh[part] = m;
            return m;
        }

        FxCube ArrowPiece(string part, int hex, double opacity, bool additive)
        {
            FxCube c = Cube(hex, opacity, additive, "arrow " + part);
            c.G.GetComponent<MeshFilter>().sharedMesh = ArrowMesh(part);
            return c;
        }

        /// <summary>`projectileBolt(from, to, color, tier, dur0, arcH, trail)` — 계단 촉 + 블록 꼬리 + 글로우 꼬리 · 착탄 파편·불티·플래시.</summary>
        public void ProjectileBolt(Vector3 from, Vector3 to, int hex, int tier, double dur0 = 0, double arcH = 0, bool trail = false)
        {
            Vector3 delta = to - from; double dist = delta.magnitude;
            Vector3 dir0 = dist > 1e-6 ? delta / (float)dist : Vector3.right;
            double arc = arcH;
            double dur = dur0 > 0 ? dur0 : (0.09 + tier * 0.004);
            double tailLen = Math.Min(dist * 0.55, 1.7 + tier * 0.15);
            double headR = 0.08 + tier * 0.012;
            FxCube head = ArrowPiece("head", 0xffffff, 1, false);
            FxCube tail = ArrowPiece("tail", hex, 0.9, true);
            FxCube glow = ArrowPiece("tail", hex, 0.4, true);
            double wf = headR / 0.13;
            head.SetScale3(wf, 1, wf);
            Func<double, Vector3> posAt = k => { Vector3 p = Vector3.Lerp(from, to, (float)k); if (arc > 0) p.y += (float)(arc * 4 * k * (1 - k)); return p; };
            Action<FxCube, Vector3, Vector3, double, double> placeTail = (m, headPos, dir, len, w) =>
            {
                m.SetPos(headPos + dir * (float)(-len / 2 - 0.17));
                m.SetScale3(w, Math.Max(0.001, len), w);
            };
            Action<Vector3> orient = dir =>
            {
                Quaternion q = Quaternion.FromToRotation(Vector3.up, ThreeSpace.Pos(dir.x, dir.y, dir.z));
                head.T.localRotation = q; tail.T.localRotation = q; glow.T.localRotation = q;
            };
            Vector3 impactDir = arc > 0 ? (posAt(1) - posAt(0.97)).normalized : dir0;
            int tframe = 0;
            if (arc <= 0) orient(dir0);
            Timeline.Add(dur, k =>
            {
                Vector3 dir = dir0;
                if (arc > 0) { dir = (posAt(Math.Min(1, k + 0.03)) - posAt(Math.Max(0, k - 0.03))).normalized; orient(dir); }
                Vector3 headPos = posAt(k) + dir * 0.17f;
                head.SetPos(headPos);
                double len = tailLen * Math.Min(1, k / 0.35);
                placeTail(tail, headPos, dir, len, wf * 0.85); placeTail(glow, headPos, dir, len * 1.05, wf * 1.7);
                if (trail && (tframe++ % 2) == 0) FxTrailCube(headPos, hex, 0.11 + tier * 0.015, 0.26);
            }, () =>
            {
                KillCube(head); KillCube(tail); KillCube(glow);
                stage.Shards(to, tier >= 2 ? 12 : 8, hex, Math.Atan2(impactDir.y, impactDir.x), 0.7, 1.3 + tier * 0.15, 1);
                stage.Sparks(to, 10 + tier * 3, hex, 1.6, 1);
                FlashLight(to, hex, 0.26);
            });
        }

        // ── 1박: 시전(skillCastBeat) — 큐브 모트가 가슴으로 빨려들고 차지 코어가 부풀다 터진다 · 광원은 올라간다 ──
        // (수렴 룬 링은 원작이 2026-08-21 에 뺐다 · 시전 포즈(heroG.rotation.z)는 T52 가 HeroView 의 젖힘 채널로 — 평타·넉백 중엔 양보)
        public static bool IsSupportFx(string fx)
        {
            return fx == "heal" || fx == "aura" || fx == "firstaid" || fx == "wardshield" || fx == "warcry" || fx == "timewarp";
        }
        public void SkillCastBeat(int hex, string fx, int tier)
        {
            int t = T5(tier); double pw = t / 5.0;
            bool support = IsSupportFx(fx);
            Vector3 hero = Hero;
            Vector3 chest = V(hero.x, hero.y + 1.05, hero.z);
            int color = fx == "explode" ? LerpHex(hex, 0xff5a1a, 0.55) : hex;
            double dur = CastBeatMs(t) / 1000.0;
            int n = (stage.ParticleCount > 200 ? 4 : 8) + (int)Math.Round(pw * 14);
            var motes = new List<FxCube>(); var from = new List<Vector3>(); var s0 = new List<double>(); var r0 = new List<double[]>(); var tumble = new List<double[]>();
            for (int i = 0; i < n; i++)
            {
                double a = (i / (double)n) * Math.PI * 2 + R(-0.2, 0.2);
                double rad = R(0.85, 1.2);
                Vector3 f = V(hero.x + Math.Cos(a) * rad, hero.y + (support ? R(0.0, 0.5) : R(0.5, 1.8)), hero.z + Math.Sin(a) * rad * 0.6);
                FxCube sp = Cube(color, 0.95, true, "mote");
                double s = R(0.105, 0.185);
                sp.SetScale(s); sp.SetPos(f);
                var rr = new[] { R(0, 6.28), R(0, 6.28), R(0, 6.28) };
                sp.SetRot(rr[0], rr[1], rr[2]);
                motes.Add(sp); from.Add(f); s0.Add(s); r0.Add(rr); tumble.Add(new[] { R(-7, 7), R(-7, 7), R(-7, 7) });
            }
            FxCube core = Cube(fx == "explode" ? 0xffc46a : 0xffffff, 0, true, "core");
            core.SetPos(chest); core.SetScale(0.01);
            FxLights.Lease light = Lights.Take(color, 6);
            light.Pos(chest.x, chest.y, chest.z + 0.4);
            Timeline.Add(dur, k =>
            {
                double ease = k * k;
                for (int i = 0; i < motes.Count; i++)
                {
                    FxCube m = motes[i];
                    m.SetPos(Vector3.Lerp(from[i], chest, (float)ease));
                    m.SetScale(s0[i] * (1 - 0.55 * k));
                    m.SetRot(r0[i][0] + tumble[i][0] * k, r0[i][1] + tumble[i][1] * k, r0[i][2] + tumble[i][2] * k);
                    m.SetOpacity(0.95 * (1 - k * k * k));
                }
                double pop = k < 0.82 ? k / 0.82 : 1 + (k - 0.82) / 0.18 * 0.9;
                double cs = (0.05 + pop * (support ? 0.34 : 0.46) * (0.78 + pw * 0.62)) * 0.62;
                double sq = k < 0.82 ? 1 + 0.20 * (k / 0.82) : 1 - 0.42 * ((k - 0.82) / 0.18);
                core.SetScale3(cs / Math.Sqrt(sq), cs * sq, cs / Math.Sqrt(sq));
                core.SetRot(k * 2.1, k * 2.7, k * 1.3);
                core.SetOpacity(k < 0.82 ? 0.35 + k * 0.75 : 1 - (k - 0.82) / 0.18);
                light.Set((1.5 + pw * 1.9) * ease);
                // 🧍 시전 포즈 — 리그 관절을 안 건드리고 젖힘 채널만 · 평타 중엔 양보 · 지원계는 위로 모으는 문법이라 절반
                if (!stage.HeroLeanBusy) stage.HeroLean(-(support ? CastLeanSupportZ : CastLeanZ) * ease);
            }, () =>
            {
                foreach (var m in motes) KillCube(m);
                KillCube(core);
                light.Release();
                // 시전 포즈 릴리즈 — 젖힘(−0.14)에서 앞으로 «휙» 내지르고(+0.16 부근) 제자리로. 릴리즈 시각 = 2박 발화 시각이라 몸의 스냅과 이펙트가 같은 프레임에서 만난다.
                if (!stage.HeroLeanBusy)
                {
                    double z0 = stage.HeroLeanZ;
                    Timeline.Add(CastReleaseDur, k2 =>
                    {
                        if (stage.HeroLeanBusy) return;   // 평타가 끼어들면 양보
                        double s = k2 < CastReleaseRise ? k2 / CastReleaseRise : 1;
                        double decay = k2 < CastReleaseRise ? 1 : 1 - (k2 - CastReleaseRise) / (1 - CastReleaseRise);
                        stage.HeroLean((z0 + CastReleaseSnap * s) * decay);
                    }, () => { if (!stage.HeroLeanBusy) stage.HeroLean(0); });
                }
            });
        }

        // ── 2·3박: skillPayload — fx → 안무 ──
        public void SkillPayload(string fx, int hex, List<int> targetIds, int tier, ThunderHandle tell)
        {
            if (cur != null && cur.PayloadAt < 0) cur.PayloadAt = Now;
            Push(FxLogKind.Payload, fx, Hero);
            CastRecord rec = cur;
            if (fx != "meteor")
            {
                Timeline.After(WeightDelayMs(fx) / 1000.0, () => { cur = rec; SkillImpactWeight(fx, hex, targetIds, tier, null); });
            }
            switch (fx)
            {
                case "dragonfire": McFireDragon(targetIds, hex, tier); break;
                case "meteor": McRockGolemFall(targetIds, hex, tier, true); break;
                case "breath": McWyvernBite(targetIds, hex, tier); break;
                case "nova": McStarBotNova(targetIds, hex, tier); break;
                case "explode": McImpFireball(targetIds, hex, tier); break;
                case "ring": McShurikenStorm(targetIds, hex, tier); FlashLight(Hero, hex, 0.3); break;
                case "beam": McArcherLine(targetIds, hex, tier); break;
                case "voidrift": McVoidKnight(targetIds, hex, tier); break;
                case "spear": McSpearKnight(targetIds, hex, tier); break;
                case "bolt": McThunderStrike(tell, targetIds, hex, tier); break;
                case "guillotine": McExecutioner(targetIds, hex, tier); break;
                case "shurikenrun": McShurikenBarrage(targetIds, hex, tier); break;
                case "arrowrain": McArrowRain(targetIds, hex, tier); break;
                case "burrowworm": McBurrowWorm(targetIds, hex, tier); break;
                case "slash": McSwordBots(targetIds, hex, tier); break;
                case "firstaid": McMedicSprite(hex, tier); FlashLight(Hero, hex, 0.3); break;
                case "wardshield": McShieldGolem(hex, tier); FlashLight(Hero, hex, 0.4); break;
                case "warcry": McOrcHorn(hex, tier); break;
                case "timewarp": McClockBot(hex, tier); FlashLight(Hero, hex, 0.4); break;
                case "heal": McAngelBless(hex, tier); FlashLight(Hero, hex, 0.4); break;
                case "aura": McGuardianStatues(hex, tier); FlashLight(Hero, hex, 0.4); break;
                default: break;
            }
        }

        /// <summary>`skillImpactWeight(fx, color, targetIds, tier, at)` — 파편·지면 파쇄·셰이크·FOV(전 등급 · 하한 없음) + 화면 플래시 훅.</summary>
        public void SkillImpactWeight(string fx, int hex, List<int> targetIds, int tier, List<Vector3> at)
        {
            int t = T5(tier); double pw = t / 5.0;
            List<Vector3> anchors = (at != null && at.Count > 0) ? at : LiveSpots(targetIds);
            if (anchors.Count == 0) anchors = new List<Vector3> { Hero };
            for (int i = 0; i < anchors.Count && i < 4; i++)
            {
                Vector3 p = anchors[i];
                stage.Sparks(p + V(0, 0.55, 0), (int)Math.Round(13 + pw * 26), hex, 1.5 + pw * 1.1, 1.05 + pw * 0.25);
                stage.Sparks(V(p.x, 0.14, p.z), (int)Math.Round(6 + pw * 10), 0x9c8466, 0.9 + pw * 0.5, 1.1);
            }
            stage.Shake(0.26 + pw * 0.42);
            stage.FovPunch(0.018 + pw * 0.030, 0.16 + pw * 0.10);
            if (ScreenFlash != null) ScreenFlash(hex, t);
            Push(FxLogKind.Weight, fx, anchors[0]);
            if (cur != null && cur.WeightAt < 0) cur.WeightAt = Now;
        }

        // ── 근접 액터 공통 안무(mcMeleeStrike) ──
        public sealed class MeleeCfg
        {
            public string Model; public double Scale = 1; public Vector3 From, To; public double Yaw = CreatureYaw;
            public int InMs = 170, SwingMs = 150, OutMs = 260, Hold = 90; public int Hex; public int Tier;
            public string Arm = "armR"; public double Wind = -2.3, Swing = 1.0, Lunge = 0.28;
            public bool Drop, Sink, Big, FaceBack;
            public Action<Vector3, SkillActor> OnImpact; public Action OnExit;
        }
        public SkillActor McMeleeStrike(MeleeCfg cfg)
        {
            SkillActor a = Actor(cfg.Model, cfg.Scale, cfg.From, cfg.Yaw);
            if (a == null) return null;
            string arm = cfg.Arm, armO = cfg.Arm == "armR" ? "armL" : "armR";
            Vector3 to = cfg.To, from = cfg.From;
            double wind = cfg.Wind, swing = cfg.Swing; int hex = cfg.Hex, t = T5(cfg.Tier);
            Timeline.Add(cfg.InMs / 1000.0, k =>
            {
                double e = cfg.Drop ? k * k : Ease3(k);
                a.SetPos(Vector3.Lerp(from, to, (float)e));
                a.RotX(arm, wind * e); a.RotX(armO, -0.35 * e);
                a.RotX("legL", 0.55 * (1 - e)); a.RotX("legR", -0.55 * (1 - e));
                a.RotX("head", 0.18 * e);
            }, () =>
            {
                McDust(to, cfg.Drop ? 10 : 5);
                if (cfg.Drop) stage.Shake(0.12 + t * 0.03);
                Timeline.Add(cfg.SwingMs / 1000.0, k =>
                {
                    double e = k * k;
                    a.RotX(arm, wind + (swing - wind) * e); a.RotX(armO, -0.35 + 0.5 * e);
                    a.SetX(to.x + cfg.Lunge * e * (cfg.FaceBack ? -1 : 1));
                    a.RotX("head", 0.18 - 0.3 * e);
                }, () =>
                {
                    Vector3 hp = V(to.x + (cfg.FaceBack ? -0.6 : 0.6), 0.75, to.z);
                    if (cfg.OnImpact != null) cfg.OnImpact(hp, a); else McHit(hp, hex, t, cfg.Big);
                    Timeline.After(cfg.Hold / 1000.0, () =>
                    {
                        double y0 = a.Pos.y, sc0 = a.Scale;
                        Timeline.Add(cfg.OutMs / 1000.0, k =>
                        {
                            if (cfg.Sink) { a.SetY(y0 - k * 2.6); return; }
                            double e = k * k;
                            a.SetPos(to.x - (cfg.FaceBack ? -0.5 : 0.5) * e, y0 + e * 2.2, a.Pos.z);
                            a.SetScale(sc0 * (1 - 0.75 * e));
                            a.RotX(arm, swing - 1.4 * e);
                            a.RotX("legL", -0.7 * e); a.RotX("legR", 0.7 * e);
                        }, () => { Free(a); if (cfg.OnExit != null) cfg.OnExit(); });
                    });
                });
            });
            return a;
        }

        // ── ① 연속 참격(slash) — 검사 로봇 2~3기 교차 ──
        public void McSwordBots(List<int> targetIds, int hex, int tier)
        {
            int t = T5(tier);
            Vector3 spot = McSpots(targetIds)[0];
            int n = 2 + (t >= 4 ? 1 : 0);
            for (int i = 0; i < n; i++)
            {
                int idx = i; bool back = i % 2 == 1;
                double dz = idx == 2 ? 0 : (back ? 0.85 : -0.85);
                Timeline.After(idx * 0.140, () =>
                {
                    Vector3 target = McSpots(targetIds)[0];
                    McMeleeStrike(new MeleeCfg
                    {
                        Model = "swordbot", Scale = 0.95 + t * 0.06, Hex = hex, Tier = t, Big = idx == n - 1,
                        FaceBack = back, Yaw = back ? -CreatureYaw : CreatureYaw,
                        From = V(target.x + (back ? 1.9 : -1.9), 1.3, target.z + dz),
                        To = V(target.x + (back ? 0.88 : -0.88), 0, target.z + dz),
                        InMs = 150, SwingMs = 130, OutMs = 240, Hold = 70,
                        OnImpact = (hp, a) => { McHit(hp, hex, t, idx == n - 1); Sfx.SlashArc(idx, t); },
                    });
                });
            }
        }

        // ── ② 회오리 베기(ring) — 표창 10개가 영웅을 감아 돌다 날아간다 ──
        public void McShurikenStorm(List<int> targetIds, int hex, int tier)
        {
            int t = T5(tier); const int n = 10;
            List<Vector3> spots = McSpots(targetIds);
            Vector3 hero = Hero;
            Sfx.SlashArc(0, t);
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                SkillActor a = Actor("shuriken", 0.9 + t * 0.06, V(hero.x, 1.05, hero.z), 0);
                if (a == null) return;
                Vector3 tgt = spots[idx % spots.Count];
                double a0 = (idx / (double)n) * Math.PI * 2;
                double orbit = 0.75 + (idx % 3) * 0.18;
                Vector3 dest = V(tgt.x + R(-0.2, 0.2), 0.65 + R(-0.2, 0.35), tgt.z + R(-0.2, 0.2));
                a.SetGRot(-0.35, a.GRot[1], a.GRot[2]);
                Timeline.Add(0.17, k =>
                {
                    double ang = a0 + k * 5.2;
                    a.SetPos(hero.x + Math.Cos(ang) * orbit, 0.85 + k * 0.55, hero.z + Math.Sin(ang) * orbit * 0.7);
                    a.AddGRot(1, 1.1);
                }, () =>
                {
                    Vector3 st = a.Pos;
                    Timeline.Add((110 + idx * 22) / 1000.0, k => { a.SetPos(Vector3.Lerp(st, dest, (float)k)); a.AddGRot(1, 1.4); },
                        () => { McHit(dest, hex, t, idx == n - 1); Free(a); });
                });
            }
        }

        // ── 표창 난무(shurikenrun) — 화면 왼쪽 밖에서 포물선으로 날아와 꽂힌다(단일) ──
        public void McShurikenBarrage(List<int> targetIds, int hex, int tier)
        {
            int t = T5(tier);
            Vector3 spot0 = McSpots(targetIds)[0];
            int n = 5 + (t >= 3 ? 2 : t >= 1 ? 1 : 0);
            Sfx.SlashArc(0, t);
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                Timeline.After(idx * 0.130, () =>
                {
                    Vector3 tgt = McSpots(targetIds)[0];
                    Vector3 from = V(SkillOffscreenX + R(-1, 1), R(1.4, 2.6), tgt.z + R(-1.3, 1.3));
                    SkillActor a = Actor("shuriken", 1.25 + t * 0.06, from, 0);
                    if (a == null) return;
                    Vector3 dest = V(tgt.x - 0.1, 0.62 + R(-0.15, 0.4), tgt.z + R(-0.25, 0.25));
                    double arcH = 1.6 + R(-0.2, 0.5);
                    double spin = 0.55 + R(0, 0.15);
                    int fc = 0;
                    Timeline.Add(1.5, k =>
                    {
                        Vector3 p = Vector3.Lerp(from, dest, (float)k);
                        p.y += (float)(Math.Sin(k * Math.PI) * arcH);
                        a.SetPos(p);
                        a.AddGRot(2, spin);
                        if ((fc++ % 2) == 0) FxTrailCube(a.Pos, hex, 0.15 + t * 0.02, 0.3);
                    }, () => { McHit(dest, hex, t, idx == n - 1); Free(a); });
                });
            }
        }

        // ── 화살비(arrowrain) — 화살이 왼쪽 밖에서 포물선으로 쏟아진다(광역) ──
        public void McArrowRain(List<int> targetIds, int hex, int tier)
        {
            int t = T5(tier);
            int volley = 7 + t * 2;
            Sfx.SlashArc(0, t);
            for (int i = 0; i < volley; i++)
            {
                int idx = i;
                Timeline.After(idx * 0.110, () =>
                {
                    List<Vector3> spots = McSpots(targetIds);
                    Vector3 c = spots[idx % spots.Count];
                    Vector3 from = V(SkillOffscreenX + R(-1, 1), R(1.6, 2.8), c.z + R(-1.2, 1.2));
                    Vector3 to = V(c.x + R(-0.25, 0.25), 0.6 + R(0, 0.5), c.z + R(-0.3, 0.3));
                    ProjectileBolt(from, to, hex, t, 1.3, 2.2 + R(-0.3, 0.5), true);
                });
            }
            Timeline.After((Math.Min(6, volley) * 110 + 1200) / 1000.0, () => stage.Shake(0.2 + t * 0.05));
        }

        // ── 땅벌레(burrowworm) — 적 발밑에서 지렁이가 꿈틀대며 솟아 문다(단일) ──
        static readonly string[] WormSegs = { "seg0", "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7", "seg8", "seg9", "seg10", "seg11", "seg12", "seg13" };
        public void McBurrowWorm(List<int> targetIds, int hex, int tier)
        {
            int t = T5(tier);
            Vector3 spot = McSpots(targetIds)[0];
            Vector3 at = V(spot.x, 0.1, spot.z);
            SkillActor a = Actor("worm", 1.0 + t * 0.06, V(at.x, -5.2, at.z), CreatureYaw);
            if (a == null) return;
            double ph = 0;
            Action<double> wriggle = amp =>
            {
                ph += 0.3;
                for (int i = 0; i < WormSegs.Length; i++) a.RotZ(WormSegs[i], amp * Math.Sin(ph - i * 0.45));
                a.RotZ("head", amp * 0.6 * Math.Sin(ph - WormSegs.Length * 0.45));
            };
            Sfx.MawRoar(Math.Max(0, t - 1));
            McDust(at, 16);
            Timeline.Add(0.6, k =>
            {
                double e = Ease3(k);
                a.SetY(-5.2 + e * 5.3);
                wriggle(0.10 + 0.18 * e);
                a.RotX("head", -1.0 * e); a.RotX("jaw", 0.55 * e);
            }, () =>
            {
                McDust(at, 8);
                Timeline.Add(0.45, k =>
                {
                    wriggle(0.34);
                    a.RotX("head", -1.0 + 0.22 * Math.Sin(k * Math.PI * 2)); a.RotY("head", 0.4 * Math.Sin(k * Math.PI * 2));
                    a.RotX("jaw", 0.55 + 0.15 * Math.Sin(k * Math.PI * 3));
                }, () =>
                {
                    Timeline.Add(0.22, k =>
                    {
                        double e = k * k;
                        wriggle(0.30 * (1 - e));
                        a.RotX("head", -1.0 + 1.85 * e); a.RotX("jaw", 0.55 - 1.05 * e);
                        a.SetY(0.1 + 0.5 * Math.Sin(k * Math.PI));
                    }, () =>
                    {
                        McHit(V(at.x, 1.0, at.z), hex, t, true);
                        Sfx.SlashArc(0, t);
                        Timeline.After(0.150, () =>
                        {
                            double y0 = a.Pos.y;
                            Timeline.Add(0.55, k => { wriggle(0.36 * (1 - k * 0.4)); a.SetY(y0 - k * 6.0); a.RotX("head", 0.85 - k * 0.9); }, () => Free(a));
                        });
                    });
                });
            });
        }

        // ── ③ 응급 처치(firstaid) — 의무 정령 ──
        public void McMedicSprite(int hex, int tier)
        {
            int t = T5(tier);
            Vector3 hero = Hero;
            Vector3 at = V(hero.x + SupportStageX, 1.02, hero.z - 0.3);
            Vector3 from = V(at.x + 0.7, 1.9, at.z - 0.6);
            SkillActor a = Actor("medic", 1.0 + t * 0.05, from, CreatureYaw);
            if (a == null) return;
            Sfx.HealDescend(t);
            double ph = 0;
            Timeline.Add(0.24, k =>
            {
                double e = Ease3(k);
                a.SetPos(Vector3.Lerp(from, at, (float)e));
                McFlap(a, ph += 0.9, 0.75);
                a.RotX("armR", -0.4 * e);
            }, () =>
            {
                McBlockStream(at, V(hero.x, 1.12, hero.z), 8, 0xf4f0e6, 420, 0.13, 0.16);
                McBlockStream(at, V(hero.x, 1.12, hero.z), 5, hex, 460, 0.11, 0.22);
                Timeline.Add(0.5, k =>
                {
                    McFlap(a, ph += 0.7, 0.6);
                    a.RotX("armR", -0.4 - Math.Sin(k * Math.PI * 3) * 0.5);
                    a.SetY(at.y + Math.Sin(k * Math.PI * 2) * 0.07);
                }, () =>
                {
                    Timeline.Add(0.36, k =>
                    {
                        McFlap(a, ph += 1.1, 0.9);
                        a.SetPos(at.x + k * 1.2, at.y + k * 1.7, at.z - k * 0.8);
                        a.SetScale((1 + t * 0.05) * (1 - 0.8 * k * k));
                    }, () => Free(a));
                });
            });
        }

        // ── ④ 화염구(explode) — 임프가 불덩이 블록을 던진다 ──
        public void McImpFireball(List<int> targetIds, int hex, int tier)
        {
            int t = T5(tier);
            Vector3 hero = Hero;
            Vector3 at = V(hero.x + SupportStageX - 0.35, 1.15, hero.z - 0.6);
            SkillActor a = Actor("imp", 1.0 + t * 0.07, V(at.x, at.y + 1.6, at.z - 0.6), CreatureYaw);
            if (a == null) return;
            double ph = 0;
            Sfx.MawRoar(Math.Max(0, t - 2));
            Timeline.Add(0.16, k =>
            {
                double e = Ease3(k);
                a.SetPos(at.x, at.y + 1.6 * (1 - e), at.z - 0.6 * (1 - e));
                McFlap(a, ph += 1.3, 0.9);
                a.RotX("armR", -2.2 * e);
            }, () =>
            {
                Timeline.Add(0.12, k => { McFlap(a, ph += 1.3, 0.9); a.RotX("armR", -2.2 + 3.0 * k * k); }, () =>
                {
                    List<Vector3> spots = McSpots(targetIds);
                    for (int si = 0; si < spots.Count && si < 3; si++)
                    {
                        int s = si; Vector3 spot = spots[si];
                        Timeline.After(s * 0.090, () =>
                        {
                            SkillActor b = Actor("fireblock", 0.9 + t * 0.1, V(at.x + 0.45, at.y + 0.15, at.z), 0);
                            if (b == null) return;
                            Vector3 p0 = b.Pos, p1 = V(spot.x, 0.62, spot.z);
                            Timeline.Add(0.17, k =>
                            {
                                Vector3 p = Vector3.Lerp(p0, p1, (float)k); p.y += (float)(Math.Sin(k * Math.PI) * 0.55);
                                b.SetPos(p); b.SetGRot(k * 6, k * 4, k * 3);
                            }, () =>
                            {
                                Free(b);
                                McHit(p1, hex, t, s == 0);
                                for (int i = 0; i < 4; i++)
                                {
                                    SkillActor c = Actor("fireblock", 0.42, p1, 0);
                                    if (c == null) break;
                                    Vector3 dir = V(R(-1, 1), 0, R(-0.7, 0.7)).normalized * (float)(0.8 + t * 0.1);
                                    Vector3 q0 = p1;
                                    Timeline.Add(0.42, k =>
                                    {
                                        c.SetPos(q0.x + dir.x * k, Math.Max(0.12, q0.y + Math.Sin(k * Math.PI) * 0.5 - k * 0.35), q0.z + dir.z * k);
                                        c.SetGRot(k * 7, k * 5, 0);
                                        c.SetScale(0.42 * (1 - 0.6 * k));
                                    }, () => Free(c));
                                }
                                Sfx.StormStrike(s);
                            });
                        });
                    }
                    Timeline.Add(0.4, k =>
                    {
                        McFlap(a, ph += 1.5, 1.0);
                        a.SetPos(at.x - k * 0.5, at.y + k * 1.9, at.z - k * 0.5);
                        a.SetScale((1 + t * 0.07) * (1 - 0.8 * k * k));
                    }, () => Free(a));
                });
            });
        }

        // ── ⑤ 화살 세례(beam) — 궁수 자동인형 3기 연사 ──
        public void McArcherLine(List<int> targetIds, int hex, int tier)
        {
            int t = T5(tier);
            int n = 3 + Math.Min(4, t);
            double gap = ArrowGapMs(t) / 1000.0;
            Vector3 hero = Hero;
            var bots = new List<SkillActor>();
            for (int i = 0; i < 3; i++)
            {
                Vector3 at = V(hero.x + SupportStageX - 0.2, 0, hero.z + (i - 1) * 0.85 - 0.1);
                SkillActor a = Actor("archer", 0.95, V(at.x, -1.8, at.z), CreatureYaw);
                if (a == null) break;
                bots.Add(a);
                Vector3 at2 = at;
                Timeline.Add(0.16, k => { a.SetY(-1.8 + Ease3(k) * 1.8); a.RotX("armR", -1.45 * k); a.RotX("armL", -1.2 * k); }, () => McDust(at2, 4));
            }
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                Timeline.After(idx * gap, () =>
                {
                    List<Vector3> live = LiveSpots(targetIds);
                    if (live.Count == 0) return;
                    Vector3 m = live[idx % live.Count];
                    bool last = idx == n - 1;
                    SkillActor b = bots.Count > 0 ? bots[idx % bots.Count] : null;
                    double j = (idx == 0 || last) ? 0 : 0.34;
                    Vector3 from = b != null ? b.Pos + V(0.45, 1.15, 0) : Hero + V(0.4, 1, 0);
                    Vector3 to = m + V(R(-j, j), 0.6 + R(-j, j) * 0.6, R(-j, j) * 0.5);
                    ProjectileBolt(from, to, hex, last ? Math.Min(5, t + 2) : t);
                    if (b != null && b.Has("armR")) { b.RotX("armR", -1.75); Timeline.After(0.070, () => { if (!b.Freed) b.RotX("armR", -1.45); }); }
                    if (last) McHit(to, hex, t, true);
                    Sfx.ArrowShot(idx, t);
                });
            }
            Timeline.After(n * gap + 0.260, () =>
            {
                foreach (SkillActor a in bots) Timeline.Add(0.3, k => { a.SetY(-1.8 * k * k); a.RotX("armR", -1.45 * (1 - k)); }, () => Free(a));
            });
        }

        // ── ⑥ 전투의 함성(warcry) — 오크 대장이 뿔피리를 분다 ──
        public void McOrcHorn(int hex, int tier)
        {
            int t = T5(tier);
            Vector3 hero = Hero;
            Vector3 at = V(hero.x + SupportStageX, 0, hero.z - 0.35);
            SkillActor a = Actor("orcchief", 1.0 + t * 0.05, V(at.x, 2.2, at.z), CreatureYaw);
            if (a == null) return;
            Timeline.Add(0.17, k => { a.SetY(2.2 * (1 - k * k)); a.RotX("armR", -1.1 * k); a.RotX("head", -0.2 * k); }, () =>
            {
                McDust(at, 7); stage.Shake(0.14 + t * 0.03);
                Sfx.AuraRise(t);
                Vector3 mouth = V(at.x + 0.55, 1.35, at.z);
                for (int i = 0; i < 3; i++)
                {
                    int idx = i;
                    Timeline.After((60 + idx * 190) / 1000.0, () =>
                    {
                        McBlockStream(mouth, V(hero.x + 2.4 + idx * 0.5, 1.2, hero.z), 7 + t, hex, 300, 0.16, 0.1);
                        a.RotX("head", -0.45);
                        Timeline.After(0.110, () => { if (!a.Freed) a.RotX("head", -0.2); });
                        stage.Shake(0.1 + t * 0.02);
                        Sfx.MawRoar(t);
                    });
                }
                Timeline.After(0.320, () => McBlockStream(V(at.x, 1.4, at.z), V(hero.x, 1.1, hero.z), 8, hex, 420, 0.14));
                Timeline.After(0.760, () => Timeline.Add(0.32, k => { a.SetY(-k * k * 2.4); a.RotX("armR", -1.1 * (1 - k)); }, () => Free(a)));
            });
        }

        // ── ⑦ 메테오(meteor) — 바위 골렘 낙하 · 무게는 마지막 착탄 콜백이 직접 ──
        public void McRockGolemFall(List<int> targetIds, int hex, int tier, bool withWeight)
        {
            int t = T5(tier);
            List<Vector3> baseSpots = McSpots(targetIds);
            int n = 2 + (t >= 2 ? 1 : 0) + (t >= 4 ? 1 : 0);
            Sfx.StormRumble(0.5);
            for (int i = 0; i < n; i++)
            {
                int idx = i; bool last = idx == n - 1;
                Vector3 c = baseSpots[idx % baseSpots.Count];
                double edge = last || idx == 0 ? 0 : 0.9 + t * 0.12;
                Vector3 spot = V(c.x + R(-edge, edge), 0, c.z + R(-edge, edge) * 0.55);
                Timeline.After(idx * 0.190, () =>
                {
                    McMeleeStrike(new MeleeCfg
                    {
                        Model = "rockgolem", Scale = (0.9 + t * 0.06) * (last ? 1.35 : 1), Hex = hex, Tier = t, Big = last,
                        From = V(spot.x - 0.4, 3.4, spot.z), To = V(spot.x - 0.8, 0, spot.z),
                        Drop = true, InMs = 300, SwingMs = 120, OutMs = 300, Hold = 120, Wind = -2.5, Swing = 1.15, Lunge = 0.2,
                        OnImpact = (hp, a) =>
                        {
                            McHit(hp, hex, t, last);
                            McDust(spot, 10);
                            stage.Sparks(V(spot.x, 0.3, spot.z), (int)Math.Round(10 + t * 4.0), 0x8d8a83, 1.3, 1.15);
                            Sfx.StormStrike(idx);
                            if (last && withWeight) SkillImpactWeight("meteor", hex, targetIds, t, new List<Vector3> { V(spot.x, 0, spot.z) });
                        },
                    });
                });
            }
        }

        // ── ⑧ 낙뢰(bolt) — 번개새 선회(예고 · 1박) → 급강하 ──
        public sealed class ThunderHandle { public SkillActor A; public Vector3 Spot; public double Ph, Ang, Age; public bool Stop; }
        public ThunderHandle McThunderTell(List<int> targetIds, int hex, int tier)
        {
            int t = T5(tier);
            Vector3 spot = McSpots(targetIds)[0];
            SkillActor a = Actor("thunderbird", 1.0 + t * 0.1, V(spot.x, 3.4, spot.z), CreatureYaw);
            if (a == null) return null;
            var h = new ThunderHandle { A = a, Spot = spot };
            Sfx.StormRumble(0.3);
            Action circle = null;
            circle = () =>
            {
                if (h.Stop || a.Freed) return;
                if ((h.Age += 0.22) > ThunderTellMaxAge) { h.Stop = true; Free(a); return; }
                Timeline.Add(0.22, k =>
                {
                    h.Ang += 0.16; h.Ph += 1.0;
                    a.SetPos(spot.x + Math.Cos(h.Ang) * 1.25, 3.3 + Math.Sin(h.Ang * 2) * 0.18, spot.z + Math.Sin(h.Ang) * 0.9);
                    a.SetYaw(-h.Ang + Math.PI / 2);
                    McFlap(a, h.Ph, 0.55);
                }, circle);
            };
            circle();
            return h;
        }
        public void McThunderStrike(ThunderHandle handle, List<int> targetIds, int hex, int tier)
        {
            int t = T5(tier);
            List<Vector3> spots = McSpots(targetIds);
            ThunderHandle h = (handle != null && handle.A != null && !handle.A.Freed) ? handle : McThunderTell(targetIds, hex, t);
            if (h == null) return;
            h.Stop = true;
            SkillActor a = h.A;
            int n = 2 + Math.Min(2, t / 2);
            Action<int> dive = null;
            dive = i =>
            {
                Vector3 spot = spots[i % spots.Count];
                Vector3 top = V(spot.x + 0.2, 3.5, spot.z - 0.2);
                Vector3 low = V(spot.x, 1.0, spot.z);
                Vector3 p0 = a.Pos;
                Timeline.Add(0.13, k =>
                {
                    a.SetPos(Vector3.Lerp(p0, low, (float)(k * k)));
                    a.SetYaw(Math.PI / 2);
                    McFlap(a, 0, 0.05 + (1 - k) * 0.3);
                    a.RotX("head", 0.4 * k);
                }, () =>
                {
                    McBlockStream(V(spot.x, 4.2, spot.z), V(spot.x, 0.15, spot.z), 9, 0xfff07a, 130, 0.19, 0);
                    McHit(V(spot.x, 0.7, spot.z), hex, t, i == n - 1);
                    Sfx.StormStrike(i);
                    Timeline.Add(0.15, k => { a.SetPos(Vector3.Lerp(low, top, (float)k)); McFlap(a, k * 9, 0.8); a.RotX("head", 0.4 * (1 - k)); }, () =>
                    {
                        if (i + 1 < n) dive(i + 1);
                        else Timeline.Add(0.34, k =>
                        {
                            a.SetPos(top.x - k * 1.4, top.y + k * 2.2, top.z - k * 0.8);
                            McFlap(a, k * 14, 0.9);
                            a.SetScale((1 + t * 0.1) * (1 - 0.7 * k * k));
                        }, () => Free(a));
                    });
                });
            };
            dive(0);
        }

        // ── ⑨ 축복(heal) — 치유 천사 2체 ──
        public void McAngelBless(int hex, int tier)
        {
            int t = T5(tier);
            Vector3 hero = Hero;
            Sfx.HealDescend(t);
            for (int i = 0; i < 2; i++)
            {
                double s = i == 1 ? 1 : -1;
                Vector3 at = V(hero.x + SupportStageX + i * 0.5, 1.55, hero.z + s * 0.55);
                SkillActor a = Actor("angel", 1.0 + t * 0.05, V(at.x, at.y + 2.6, at.z), -s * CreatureYaw);
                if (a == null) return;
                double ph = i * 2;
                Timeline.Add(0.26, k =>
                {
                    double e = Ease3(k);
                    a.SetY(at.y + 2.6 * (1 - e));
                    McFlap(a, ph += 0.8, 0.6);
                    a.RotX("armL", -0.7 * e); a.RotX("armR", -0.7 * e);
                }, () =>
                {
                    McBlockStream(at, V(hero.x, 1.0, hero.z), 9, hex, 520, 0.15, 0.25);
                    McBlockStream(at, V(hero.x, 1.0, hero.z), 5, 0xffffff, 560, 0.11, 0.3);
                    Timeline.Add(0.55, k => { McFlap(a, ph += 0.7, 0.55); a.SetY(at.y + Math.Sin(k * Math.PI * 2) * 0.09); },
                        () => Timeline.Add(0.34, k =>
                        {
                            McFlap(a, ph += 1.1, 0.9);
                            a.SetY(at.y + k * 2.4);
                            a.SetScale((1 + t * 0.05) * (1 - 0.8 * k * k));
                        }, () => Free(a)));
                });
            }
        }

        // ── ⑩ 용의 아가리(breath) — 와이번이 땅을 뚫고 솟아 문다 ──
        public void McWyvernBite(List<int> targetIds, int hex, int tier)
        {
            int t = T5(tier);
            List<Vector3> spots = McSpots(targetIds);
            for (int si = 0; si < spots.Count && si < 2; si++)
            {
                int s = si; Vector3 spot = spots[si];
                Timeline.After(s * 0.150, () =>
                {
                    McDust(spot, 9);
                    Sfx.StormRumble(0.26);
                    SkillActor a = Actor("wyvern", 1.5 + t * 0.12, V(spot.x - 0.5, -2.4, spot.z - 0.15), CreatureYaw);
                    if (a == null) return;
                    Sfx.MawRoar(t);
                    Timeline.Add(0.26, k =>
                    {
                        double e = Ease3(k);
                        a.SetY(-2.4 + e * 2.5);
                        a.RotX("jaw", 0.95 * Math.Min(1, k / 0.7));
                        a.RotX("head", -0.5 * e);
                        McFlap(a, k * 7, 0.45);
                    }, () =>
                    {
                        stage.Shake(0.26 + t * 0.05);
                        Timeline.Add(0.14, k =>
                        {
                            double c = k * k;
                            a.RotX("jaw", 0.95 * (1 - c)); a.RotX("head", -0.5 + 0.7 * c);
                            a.SetX(spot.x - 0.5 + c * 0.35);
                        }, () =>
                        {
                            McHit(V(spot.x, 0.8, spot.z), hex, t, s == 0);
                            Sfx.MawBite(t);
                            Timeline.Add(0.36, k => { a.SetY(0.1 - k * k * 2.6); a.RotX("head", 0.2 - k * 0.5); }, () => { Free(a); McDust(spot, 5); });
                        });
                    });
                });
            }
        }

        // ── ⑪ 처형(guillotine) — 처형인 강림 · 절단 시각 430ms(선고 300 + 낙하 130) ──
        public void McExecutioner(List<int> targetIds, int hex, int tier)
        {
            int t = T5(tier);
            Vector3 spot = McSpots(targetIds)[0];
            Sfx.StormRumble(0.32);
            McMeleeStrike(new MeleeCfg
            {
                Model = "executioner", Scale = 1.15 + t * 0.07, Hex = hex, Tier = t, Big = true,
                From = V(spot.x + 1.25, 2.9, spot.z + 0.35), To = V(spot.x + 0.92, 0, spot.z + 0.35),
                FaceBack = true, Yaw = -CreatureYaw, Drop = true,
                InMs = 300, SwingMs = 130, OutMs = 320, Hold = 140, Wind = -2.6, Swing = 1.25, Lunge = 0.34,
                OnImpact = (hp, a) =>
                {
                    McHit(hp, hex, t, true);
                    stage.Sparks(V(spot.x, 0.25, spot.z), 14 + t * 3, 0xb9bcc2, 1.4, 1.2);
                    McDust(spot, 8);
                    Sfx.AnvilHit(true);
                },
            });
        }

        // ── ⑫ 성역(aura) — 수호 석상 4기가 땅에서 솟는다 ──
        public void McGuardianStatues(int hex, int tier)
        {
            int t = T5(tier);
            Vector3 hero = Hero;
            Sfx.AuraRise(t);
            double cx = hero.x + SupportStageX + 0.5;
            for (int i = 0; i < 4; i++)
            {
                double ang = Math.PI / 4 + i * Math.PI / 2;
                Vector3 at = V(cx + Math.Cos(ang) * 0.85, 0, hero.z + Math.Sin(ang) * 0.8);
                Timeline.After(i * 0.070, () =>
                {
                    SkillActor a = Actor("statue", 0.95 + t * 0.05, V(at.x, -1.9, at.z), Math.Atan2(hero.x - at.x, hero.z - at.z));
                    if (a == null) return;
                    Timeline.Add(0.22, k => { double e = Ease3(k); a.SetY(-1.9 + e * 1.9); a.RotX("armL", -1.5 * e); a.RotX("armR", -1.5 * e); }, () =>
                    {
                        McDust(at, 5);
                        McBlockStream(V(at.x, 1.1, at.z), V(hero.x, 1.0, hero.z), 6, hex, 640, 0.13);
                        Timeline.Add(0.62, k => a.SetY(Math.Sin(k * Math.PI * 2) * 0.04),
                            () => Timeline.Add(0.3, k => a.SetY(-k * k * 2.0), () => Free(a)));
                    });
                });
            }
        }

        // ── ⑬ 초신성(nova) — 성좌 로봇 강림·응축·폭발 · 폭발 시각 560ms(강림 200 + 응축 360) ──
        public void McStarBotNova(List<int> targetIds, int hex, int tier)
        {
            int t = T5(tier);
            Vector3 spot = McSpots(targetIds)[0];
            Vector3 at = V(spot.x - 0.2, 0, spot.z);
            SkillActor a = Actor("starbot", 1.2 + t * 0.08, V(at.x, 3.3, at.z), CreatureYaw);
            if (a == null) return;
            double s0 = 1.2 + t * 0.08;
            Sfx.StormRumble(0.4);
            Timeline.Add(0.2, k => { a.SetY(3.3 * (1 - k * k)); a.RotX("armL", -0.4 * k); a.RotX("armR", -0.4 * k); }, () =>
            {
                McDust(at, 10); stage.Shake(0.2 + t * 0.04);
                Timeline.Add(0.36, k =>
                {
                    double e = k * k;
                    a.SetScale3(s0 * (1 + 0.1 * e), s0 * (1 - 0.22 * e), s0 * (1 + 0.1 * e));
                    a.RotX("armL", -0.4 - 2.0 * e); a.RotX("armR", -0.4 - 2.0 * e);
                    a.RotX("legL", 0.5 * e); a.RotX("legR", -0.5 * e);
                    a.PartScale("core", 1 + e * 1.6);
                    a.RotX("head", 0.35 * e);
                }, () =>
                {
                    McHit(V(at.x, 1.1, at.z), hex, t, true);
                    stage.Sparks(V(at.x, 1.2, at.z), 30 + t * 6, 0xffd98a, 2.2 + t * 0.2, 1.4);
                    stage.Shake(0.4 + t * 0.06);
                    for (int i = 0; i < 6; i++)
                    {
                        SkillActor b = Actor("fireblock", 0.55, V(at.x, 1.2, at.z), 0);
                        if (b == null) break;
                        Vector3 dir = V(Math.Cos(i * 1.05), R(0.3, 1.0), Math.Sin(i * 1.05) * 0.7).normalized * (float)(2.4 + t * 0.2);
                        Vector3 q0 = b.Pos;
                        Timeline.Add(0.5, k =>
                        {
                            b.SetPos(q0.x + dir.x * k, Math.Max(0.15, q0.y + dir.y * k - k * k * 2.2), q0.z + dir.z * k);
                            b.SetGRot(k * 8, k * 6, k * 4);
                            b.SetScale(0.55 * (1 - 0.7 * k));
                        }, () => Free(b));
                    }
                    Timeline.Add(0.22, k =>
                    {
                        a.SetScale(s0 * (1 + k * 0.5) * (1 - k));
                        a.SetY(k * 0.4);
                        a.RotX("armL", -2.4 + 3.0 * k); a.RotX("armR", -2.4 + 3.0 * k);
                    }, () => Free(a));
                });
            });
        }

        // ── ⑭ 공허의 창(voidrift) — 공허 기사 · 관통 시각 540ms(융기 300 + 겨눔 140 + 찌르기 100) ──
        public void McVoidKnight(List<int> targetIds, int hex, int tier)
        {
            int t = T5(tier);
            Vector3 spot = McSpots(targetIds)[0];
            Vector3 at = V(spot.x + 1.35, 0, spot.z + 0.25);
            Sfx.VoidTear(t);
            stage.Sparks(V(at.x, 0.2, at.z), 12 + t * 2, 0x2a1f44, 1.0, 1.2);
            SkillActor a = Actor("voidknight", 1.05 + t * 0.06, V(at.x, -2.2, at.z), -CreatureYaw);
            if (a == null) return;
            Timeline.Add(0.3, k => { double e = Ease3(k); a.SetY(-2.2 + e * 2.2); a.RotX("armR", -0.9 * e); }, () =>
            {
                McDust(at, 6);
                Timeline.Add(0.14, k => { a.RotX("armR", -0.9 - 0.55 * k); a.SetX(at.x + k * 0.18); }, () =>
                {
                    Timeline.Add(0.1, k => { double e = k * k; a.RotX("armR", -1.45 + 1.35 * e); a.SetX(at.x + 0.18 - e * 0.95); }, () =>
                    {
                        Vector3 hp = V(spot.x, 0.85, spot.z);
                        McHit(hp, hex, t, true);
                        Sfx.VoidPierce(t);
                        stage.Sparks(hp, 16 + t * 3, 0xb98cff, 1.6, 1.2);
                        Timeline.After(0.160, () => Timeline.Add(0.34, k => { a.SetPos(at.x - 0.77 + k * 0.5, -k * k * 2.4, a.Pos.z); }, () => { Free(a); Sfx.VoidSnap(); }));
                    });
                });
            });
        }

        // ── ⑮ 시간 왜곡(timewarp) — 태엽 로봇이 열쇠를 감고 기어 블록이 역행한다 ──
        public void McClockBot(int hex, int tier)
        {
            int t = T5(tier);
            Vector3 hero = Hero;
            Vector3 at = V(hero.x + SupportStageX + 0.3, 0, hero.z + 0.2);
            SkillActor a = Actor("clockbot", 1.0 + t * 0.05, V(at.x, 2.4, at.z), CreatureYaw);
            if (a == null) return;
            Sfx.AuraRise(t);
            Timeline.Add(0.18, k => a.SetY(2.4 * (1 - k * k)), () =>
            {
                McDust(at, 5);
                var gears = new List<SkillActor>(); var a0 = new List<double>();
                for (int i = 0; i < 5; i++)
                {
                    SkillActor g = Actor("shuriken", 0.85, V(at.x, 1.1, at.z), 0);
                    if (g == null) break;
                    gears.Add(g); a0.Add((i / 5.0) * Math.PI * 2);
                }
                Timeline.Add(0.9, k =>
                {
                    a.RotZ("weapon", -k * 22);
                    a.RotX("armR", -1.2 - Math.Sin(k * Math.PI * 4) * 0.3);
                    a.RotY("head", Math.Sin(k * Math.PI * 2) * 0.3);
                    for (int i = 0; i < gears.Count; i++)
                    {
                        double ang = a0[i] - k * 6.0;
                        gears[i].SetPos(at.x + Math.Cos(ang) * 0.8, 0.6 + ((a0[i] / 6.28) * 1.2), at.z + Math.Sin(ang) * 0.65);
                        gears[i].SetGRot(1.35, -k * 9, 0);
                        gears[i].SetScale(0.85 * (1 - 0.35 * k));
                    }
                }, () =>
                {
                    foreach (var g in gears) Free(g);
                    Timeline.Add(0.3, k => a.SetY(-k * k * 2.4), () => Free(a));
                });
            });
        }

        // ── ⑯ 종말의 화룡(dragonfire) — 첫 착탄 820ms(강림 340 + 예비 240 + 브레스 240) ──
        public void McFireDragon(List<int> targetIds, int hex, int tier)
        {
            int t = T5(tier);
            List<Vector3> spots = McSpots(targetIds);
            Vector3 hero = Hero;
            Vector3 at = V(hero.x + SupportStageX + 1.2, 2.5, hero.z - 0.4);
            SkillActor a = Actor("firedragon", 1.25 + t * 0.05, V(at.x + 4.0, 3.4, at.z - 2.0), CreatureYaw);
            if (a == null) return;
            double ph = 0;
            Sfx.MawRoar(t);
            Timeline.Add(0.34, k =>
            {
                double e = Ease3(k);
                a.SetPos(at.x + 4.0 * (1 - e), 3.4 - 1.0 * e, at.z - 2.0 * (1 - e));
                McFlap(a, ph += 0.9, 0.62);
                a.RotY("tail", Math.Sin(ph * 0.6) * 0.3);
            }, () =>
            {
                stage.Shake(0.22 + t * 0.04);
                Timeline.Add(0.24, k =>
                {
                    McFlap(a, ph += 0.7, 0.5);
                    a.RotX("head", -0.7 * k); a.RotX("jaw", 0.2 * k);
                    a.SetY(2.5 + Math.Sin(k * Math.PI) * 0.18);
                }, () =>
                {
                    Sfx.MawBite(t);
                    Vector3 mouth = V(at.x + 1.1, 2.75, at.z + 0.55);
                    Timeline.Add(0.46, k =>
                    {
                        McFlap(a, ph += 0.5, 0.35);
                        a.RotX("head", -0.7 + 1.15 * Math.Min(1, k * 2.2)); a.RotX("jaw", 0.2 + 0.85 * Math.Min(1, k * 2.2));
                    }, () => { a.RotX("jaw", 0.2); a.RotX("head", 0); });
                    for (int si = 0; si < spots.Count && si < 4; si++)
                    {
                        int s = si; Vector3 spot = spots[si];
                        Timeline.After((180 + s * 110) / 1000.0, () =>
                        {
                            SkillActor b = Actor("fireblock", 1.0 + t * 0.08, mouth, 0);
                            if (b == null) return;
                            Vector3 p1 = V(spot.x, 0.7, spot.z);
                            Timeline.Add(0.2, k =>
                            {
                                b.SetPos(Vector3.Lerp(mouth, p1, (float)k));
                                b.SetGRot(k * 7, k * 5, k * 3);
                                b.SetScale((1 + t * 0.08) * (1 + k * 0.5));
                            }, () =>
                            {
                                Free(b);
                                McHit(p1, hex, t, s == 0);
                                stage.Sparks(p1, 14 + t * 3, 0xff8a2b, 1.5, 1.2);
                                Sfx.StormStrike(s);
                            });
                        });
                    }
                    Timeline.After(0.640, () => Timeline.Add(0.42, k =>
                    {
                        McFlap(a, ph += 1.2, 0.85);
                        a.SetPos(at.x + k * 3.2, 2.5 + k * 2.4, at.z - k * 1.6);
                        a.SetScale((1.25 + t * 0.05) * (1 - 0.55 * k * k));
                    }, () => Free(a)));
                });
            });
        }

        // ── ⑰ 신의 창(spear) — 천상 기사 강하 · 착탄 500ms(개천 300 + 낙하 200) ──
        public void McSpearKnight(List<int> targetIds, int hex, int tier)
        {
            int t = T5(tier);
            Vector3 spot = McSpots(targetIds)[0];
            Vector3 at = V(spot.x + 1.15, 0, spot.z + 0.2);
            SkillActor a = Actor("spearknight", 1.1 + t * 0.06, V(at.x + 0.5, 3.6, at.z - 0.4), -CreatureYaw);
            if (a == null) return;
            double ph = 0;
            Sfx.HealDescend(t);
            Timeline.Add(0.3, k => { a.SetY(3.6 - k * 0.9); McFlap(a, ph += 0.8, 0.7); a.RotX("armR", -0.6 - 0.8 * k); }, () =>
            {
                Timeline.Add(0.2, k =>
                {
                    double e = k * k;
                    a.SetPos(at.x + 0.5 * (1 - e), 2.7 * (1 - e), at.z - 0.4 * (1 - e));
                    McFlap(a, ph += 0.4, 0.25);
                    a.RotX("armR", -1.4 + 1.9 * e);
                }, () =>
                {
                    Vector3 hp = V(spot.x, 0.8, spot.z);
                    McHit(hp, hex, t, true);
                    stage.Sparks(hp, 20 + t * 4, 0xf0c33c, 1.9, 1.35);
                    McDust(at, 10);
                    stage.Shake(0.34 + t * 0.05);
                    Sfx.AnvilHit(true);
                    Timeline.After(0.220, () => Timeline.Add(0.4, k =>
                    {
                        McFlap(a, ph += 1.3, 0.95);
                        a.SetPos(at.x + k * 0.6, k * k * 4.0, at.z - k * 0.5);
                        a.SetScale((1.1 + t * 0.06) * (1 - 0.7 * k * k));
                        a.RotX("armR", 0.5 - k * 1.2);
                    }, () => Free(a)));
                });
            });
        }

        // ── ⑱ 신성한 가호(wardshield) — 방패 골렘이 방패를 세운다 ──
        public void McShieldGolem(int hex, int tier)
        {
            int t = T5(tier);
            Vector3 hero = Hero;
            Vector3 at = V(hero.x + SupportStageX - 0.35, 0, hero.z + 0.15);
            SkillActor a = Actor("shieldgolem", 1.05 + t * 0.05, V(at.x, -2.4, at.z), CreatureYaw);
            if (a == null) return;
            Sfx.AuraRise(t);
            Timeline.Add(0.24, k => { double e = Ease3(k); a.SetY(-2.4 + e * 2.4); a.RotX("armL", -0.5 * e); }, () =>
            {
                McDust(at, 8);
                Timeline.Add(0.14, k => { a.RotX("armL", -0.5 + 0.5 * k * k); a.RotX("armR", -0.8 * k); }, () =>
                {
                    stage.Shake(0.2 + t * 0.03);
                    McDust(at, 6);
                    Sfx.AnvilHit(false);
                    McBlockStream(V(at.x, 1.2, at.z), V(hero.x, 1.05, hero.z), 8, 0xf0c33c, 560, 0.15);
                    Timeline.Add(0.7, k => a.SetY(Math.Sin(k * Math.PI * 3) * 0.03),
                        () => Timeline.Add(0.34, k => a.SetY(-k * k * 2.6), () => Free(a)));
                });
            });
        }
    }
}
