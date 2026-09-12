using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Forge.Core.Battle;
using Forge.Core.BattleFx;
using Forge.Core.Data;
using Forge.Core.Voxel;
using Forge.Game.Voxel;

namespace Forge.Game.Battle
{
    /// <summary>
    /// 적 한 마리의 시각(T8) — 원작 `monsterMesh`(11615 · 표 → 관절 배선 어댑터) · `spawnEnemy`(11858) · `update` 의 보행/대기/스쿼시/바 추적(18359~18535) · `enemyAttack`(12341) ·
    /// `hitEnemy`(12974 · 넉백·펀치·플린치) · `driveFlinch`/`undoFlinch`(18295) · `driveJelly`/`driveCapFlap`(1132) · `killEnemy`(13100 · 쓰러짐).
    /// 조형은 T4 <see cref="VoxelMob"/> 가 표(JSON) 그대로 세우고, 여기는 **관절 id 규약**(hipL/kneeL · shL/elbowL · glegL · legFL/kneeFL · tag wing/tail · cap/capDome/capTop · body+jelly)으로 노드를 찾아
    /// 매 프레임 각을 넣는다(§1 «관절 id 가 곧 애니 계약»). 규칙 숫자는 전부 Core <see cref="EnemyGait"/>·<see cref="HitRules"/>. 좌표는 three 로 계산하고 <see cref="ThreeSpace"/> 로 넣는다(결정 4).
    /// 규칙(HP·위치·처치)은 Core <see cref="Enemy"/> 가 쥔다 — 씬은 그것을 읽어 그린다.
    /// </summary>
    public sealed class EnemyView
    {
        public readonly int Id;
        public readonly string Kind;
        public readonly bool IsBoss;
        public readonly Enemy E;
        public readonly VoxelMobRig Rig;
        public readonly Transform G;
        public readonly HpBar Bar;
        public readonly double TopY, BaseScale, BarY, Cell;
        public readonly int ShardC;
        public double HalfW { get; private set; }
        public bool Dead { get; private set; }
        public bool Removed { get; private set; }
        public bool Landed = true;
        public readonly GaitProfile Gait;
        public readonly bool Fly, Hop, IsWolf, IsSlime;

        // three 좌표계의 그룹 위치·회전
        double px, py, pz;
        readonly double[] gRot = { 0, EnemyGait.EnemyYaw, 0 };
        readonly Dictionary<Transform, double[]> rot = new Dictionary<Transform, double[]>();

        // 관절 배선(정본 anim)
        sealed class Leg { public Transform Hip, Knee; }
        sealed class Arm { public Transform Sh, Elbow; }
        sealed class Knee4 { public Transform Node; public double Rx0; public bool Front; }
        readonly List<Leg> bleg = new List<Leg>();
        readonly List<Arm> barm = new List<Arm>();
        readonly List<Transform> gleg = new List<Transform>();
        readonly List<Transform> legs4 = new List<Transform>();
        readonly List<Knee4> knees4 = new List<Knee4>();
        readonly List<VoxelLimb> wings = new List<VoxelLimb>();
        Transform tail, cap, armR, armL;
        Arm armRJ;
        // 젤리 · 갓 플랩
        Mesh jellyMesh; Vector3[] jellyBase; float jellyCy, jellyH; readonly List<KeyValuePair<Transform, Vector3>> jellyFollow = new List<KeyValuePair<Transform, Vector3>>();
        Mesh capMesh; Vector3[] capBase; float capRMax; Transform capTop; float capTopY;
        // 블롭
        MeshRenderer blob; Material blobMat; float blobBase; double blobBaseO;
        // 상태(정본 m.*)
        double flinchT, flinchDur, flinchAmp;
        double punchT, punchDur, punchHold, punchAmp;
        double atkSq, gaitSq;
        bool scaleLocked;
        readonly List<KeyValuePair<Transform, double[]>> flinchOff = new List<KeyValuePair<Transform, double[]>>();
        bool bodyFlinched; double[] bodyFlinch = { 0, 0, 0 };
        // 연출 시계
        double knockT = -1, knockDur, knockOx, knockKb, knockRoll;
        double lungeT = -1, lungeOx;
        double spawnT = -1; float spawnTarget;
        double dieT = -1, dieDur, dieOx, dieBaseY, dieSy0; bool dieDusted;
        readonly BattleScene scene;

        static readonly int[] rotIndex = { 0, 1, 2 };

        public EnemyView(BattleScene scene, Enemy e, MobModel model, int chapter, Transform parent)
        {
            this.scene = scene;
            E = e; Id = e.Id; IsBoss = e.IsBoss;
            Kind = EnemyGait.KindOf(e.Id, chapter);
            Cell = model.Cell > 0 ? model.Cell : EnemyGait.EnemyVs;
            Rig = VoxelMob.Build(model, Cell, BattleScene.EnemyVivid, parent, "Enemy " + e.Id + " " + Kind);
            G = Rig.Root.transform;
            Fly = model.Fly; Hop = model.Hop; IsSlime = model.Jelly; IsWolf = Kind == "wolf";
            Gait = EnemyGait.GaitOf(Kind, IsBoss);
            foreach (var kv in Rig.PlanOf) rot[kv.Key] = new[] { kv.Value.Rot[0], kv.Value.Rot[1], kv.Value.Rot[2] };
            Wire(model);
            double maxY = 0;
            foreach (var p in model.Parts) { double t = p.At[1] + p.Box[1] / 2.0; if (t > maxY) maxY = t; }
            TopY = maxY * Cell + EnemyGait.TopYPad + (Fly ? EnemyGait.Table(Kind).Hover : 0);
            ShardC = HitRules.VolumeWeightedColor(model.Parts);
            BaseScale = IsBoss ? EnemyGait.BossScale : 1;
            G.localScale = Vector3.one * (float)BaseScale;
            BarY = TopY + (IsBoss ? EnemyGait.BarGapBoss : EnemyGait.BarGap) / BaseScale;
            px = e.X + scene.WorldX; py = 0; pz = e.Z;
            ApplyGroup();
            Bar = new HpBar(parent, "HpBar " + e.Id, true, BarY, BaseScale);
            MeasureHalfW();
            SetupShadowAndBlob(parent);
            if (IsBoss)
            {
                // 보스 등장(spawnEnemy): 0.42 배율에서 0.42초 오버슈트 스케일 인 · 등장 중 스케일 합성 잠금
                spawnTarget = (float)BaseScale;
                G.localScale = Vector3.one * (spawnTarget * 0.42f);
                scaleLocked = true; spawnT = 0;
                scene.Fx.Sparks(new Vector3((float)px, 0.3f, (float)pz), 18, 0xd7ccc8, 1.6);
                FxCatalog.Play(BattleScene.FxBossLand, ThreeSpace.Pos(px, 0, pz), (float)(1.8 / 1.0));
            }
            Bar.SetPosition(ThreeSpace.Pos(px, py, pz));
        }

        // ── 관절 배선(monsterMesh) ──
        Transform P(string id) { Transform t; return Rig.Parts.TryGetValue(id, out t) ? t : null; }

        void Wire(MobModel model)
        {
            foreach (string s in new[] { "L", "R" })
            {
                if (P("hip" + s) != null && P("knee" + s) != null) bleg.Add(new Leg { Hip = P("hip" + s), Knee = P("knee" + s) });
                if (P("sh" + s) != null && P("elbow" + s) != null) barm.Add(new Arm { Sh = P("sh" + s), Elbow = P("elbow" + s) });
                if (P("gleg" + s) != null) gleg.Add(P("gleg" + s));
            }
            if (barm.Count > 0)
            {
                armR = P("shR"); armL = P("shL");
                if (P("shR") != null && P("elbowR") != null) armRJ = new Arm { Sh = P("shR"), Elbow = P("elbowR") };
            }
            foreach (string k in new[] { "FL", "FR", "BL", "BR" })
            {
                var lg = P("leg" + k);
                if (lg == null) continue;
                legs4.Add(lg);
                var kn = P("knee" + k);
                if (kn == null) continue;
                knees4.Add(new Knee4 { Node = kn, Rx0 = rot[kn][0], Front = k[0] == 'F' });
            }
            wings.AddRange(Rig.Wings);
            tail = Rig.Tail;
            cap = P("cap");
            var capDome = P("capDome");
            if (capDome != null)
            {
                var mf = MeshOf(capDome);
                if (mf != null)
                {
                    capMesh = mf.sharedMesh; capBase = capMesh.vertices;
                    for (int i = 0; i < capBase.Length; i++) { float r = Mathf.Sqrt(capBase[i].x * capBase[i].x + capBase[i].z * capBase[i].z); if (r > capRMax) capRMax = r; }
                    if (capRMax <= 0) capRMax = 1;
                    capTop = P("capTop");
                    if (capTop != null) capTopY = capTop.localPosition.y;
                }
            }
            var body = P("body");
            if (model.Jelly && body != null)
            {
                var mf = MeshOf(body);
                if (mf != null)
                {
                    jellyMesh = mf.sharedMesh; jellyBase = jellyMesh.vertices;
                    jellyCy = mf.transform.localPosition.y;
                    jellyH = 0;
                    for (int i = 0; i < jellyBase.Length; i++) { float h = jellyCy + jellyBase[i].y; if (h > jellyH) jellyH = h; }
                    if (jellyH <= 0) jellyH = 1;
                    for (int i = 0; i < G.childCount; i++)
                    {
                        var ch = G.GetChild(i);
                        if (ch == body) continue;
                        jellyFollow.Add(new KeyValuePair<Transform, Vector3>(ch, ch.localPosition));
                    }
                }
            }
        }

        static MeshFilter MeshOf(Transform node)
        {
            var mf = node.GetComponent<MeshFilter>();
            if (mf != null) return mf;
            for (int i = 0; i < node.childCount; i++) { mf = node.GetChild(i).GetComponent<MeshFilter>(); if (mf != null) return mf; }
            return null;
        }

        // ── 실측(spawnEnemy) ──
        void MeasureHalfW()
        {
            Bounds b; if (!WorldBounds(out b)) { HalfW = BattleRules.DefaultEnemyHalfW; return; }
            HalfW = b.size.x > 0 ? Math.Max(EnemyGait.HalfWMin, b.size.x / 2) : BattleRules.DefaultEnemyHalfW;
        }

        bool WorldBounds(out Bounds b)
        {
            b = new Bounds();
            bool any = false;
            foreach (var r in Rig.Renderers)
            {
                if (r == null) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return any;
        }

        double FootprintRadius(double minR)
        {
            Bounds b; if (!WorldBounds(out b)) return 1;
            return Col_Clamp(Math.Max(b.size.x, b.size.z) * 0.5, minR, EnemyGait.FootprintMax);
        }
        static double Col_Clamp(double v, double lo, double hi) { return v < lo ? lo : v > hi ? hi : v; }

        void SetupShadowAndBlob(Transform parent)
        {
            double foot = FootprintRadius(EnemyGait.FootprintMin);
            bool noSun = Fly || foot < EnemyGait.NoSunShadowFootprint;
            if (noSun) foreach (var r in Rig.Renderers) r.shadowCastingMode = ShadowCastingMode.Off;
            blobBaseO = Fly ? EnemyGait.BlobOpacityFly : EnemyGait.BlobOpacityFoe;
            blob = BlobShadow.Create(parent, "Blob " + Id, blobBaseO);
            blobMat = blob.sharedMaterial;
            blobBase = (float)(Fly ? EnemyGait.BlobFly : Col_Clamp(foot * EnemyGait.BlobK, EnemyGait.BlobMin, EnemyGait.BlobMax));
            blob.transform.localScale = Vector3.one * blobBase;
            blob.transform.position = ThreeSpace.Pos(px, BlobShadow.Y, pz);
        }

        // ── 회전 도우미(three 오일러 · 축 0=x 1=y 2=z) ──
        void Set(Transform t, int axis, double v) { double[] r; if (t != null && rot.TryGetValue(t, out r)) { r[axis] = v; ThreeSpace.Apply(t, r); } }
        double Get(Transform t, int axis) { double[] r; return t != null && rot.TryGetValue(t, out r) ? r[axis] : 0; }
        void Mul(Transform t, int axis, double k) { Set(t, axis, Get(t, axis) * k); }
        void ApplyGroup()
        {
            G.localPosition = ThreeSpace.Pos(px, py, pz);
            ThreeSpace.Apply(G, gRot);
        }

        // ── 이벤트 ──
        /// <summary>`enemyAttack(id)`: 0.3초 코일 → 스냅 → 여운.</summary>
        public void Attack()
        {
            if (Dead) return;
            lungeT = 0; lungeOx = px;
        }

        /// <summary>`hitEnemy(id, dmg, crit, kind, kill)` 의 몸 반응(넉백·펀치·플린치·바·숫자·파편·카메라). sev = 최대 HP 대비 피해 비중.</summary>
        public void Hit(double sev, bool crit, string kind, bool kill, string dmgText)
        {
            if (Removed) return;
            double eh = TopY * BaseScale;
            Vector3 hitPt = new Vector3((float)(px + HitRules.HitPtX * BaseScale), (float)(py + eh * HitRules.HitPtY), (float)(pz + HitRules.HitPtZ));
            FxCatalog.Play(crit ? BattleScene.FxCrit : BattleScene.FxHit, ThreeSpace.Pos(hitPt.x, hitPt.y, hitPt.z), (float)(eh * 0.55 / 0.6));
            scene.Fx.Shards(hitPt, HitRules.HitShards(sev, crit), crit ? HitRules.CritShardColor : HitRules.HitShardColor, HitRules.HitShardDir, HitRules.HitShardSpread, crit ? 1.35 : 1, crit ? 1.25 : 1);
            scene.Fx.Sparks(hitPt, HitRules.HitSparks(sev, crit), crit ? HitRules.CritSparkColor : HitRules.HitSparkColor, crit ? 1.9 : 1.4);
            // ③ 넉백 + 움찔 — 임팩트 프레임에 이미 밀려 있어야 한다
            knockOx = px; knockKb = HitRules.Knockback(sev, crit); knockRoll = HitRules.KnockRoll(sev, crit); knockDur = HitRules.KnockDur(crit); knockT = 0;
            px = knockOx + knockKb;
            ApplyGroup();
            Bar.SetPosition(ThreeSpace.Pos(px, py, pz));
            // ④ 히트축 스쿼시 · ④-b 플린치
            punchT = punchDur = HitRules.PunchDur(crit); punchHold = HitRules.PunchHold(crit); punchAmp = HitRules.PunchAmp(sev, crit);
            flinchT = flinchDur = HitRules.FlinchDur(crit); flinchAmp = HitRules.FlinchAmp(sev, crit);
            if (crit) { scene.Shake(HitRules.CritShake); scene.FovPunch(HitRules.CritFov, HitRules.CritFovDur); }
            // ⑤ HP 바 · ⑥ 데미지 숫자(바 위 · 오른쪽 위로 비켜)
            Bar.Hit(sev);
            double numY = py + BarY * BaseScale + HitRules.DmgYAboveBar(BaseScale, crit, kill) + UnityEngine.Random.value * HitRules.DmgYJitter;
            scene.Numbers.Spawn(new Vector3((float)(px + HitRules.DmgX), (float)numY, (float)pz), dmgText, HitRules.DmgClass(kill, kind, crit),
                HitRules.DmgDxMin + UnityEngine.Random.value * (HitRules.DmgDxMax - HitRules.DmgDxMin), HitRules.DmgRise(sev, crit), HitRules.DmgPop(sev));
        }

        /// <summary>`killEnemy(id, isBoss)`: 파편 버스트 + 바 드레인 + 쓰러짐(피격 반동 → 붕괴 → 홀드 → 소멸).</summary>
        public void Kill()
        {
            if (Dead) return;
            Dead = true;
            UndoFlinch(); flinchT = 0;
            double eh = TopY * BaseScale;
            var burst = new Vector3((float)px, (float)(py + eh * HitRules.BurstY), (float)pz);
            FxCatalog.Play(IsBoss ? BattleScene.FxBossKill : BattleScene.FxKill, ThreeSpace.Pos(burst.x, burst.y, burst.z), (float)(IsBoss ? 1.6 : 1));
            scene.Fx.Shards(burst, HitRules.KillShards(IsBoss), HitRules.ShardColor(ShardC), 0, Math.PI, HitRules.KillShardSpeed(IsBoss), HitRules.KillShardScale(IsBoss));
            scene.Fx.Sparks(burst, IsBoss ? 30 : 14, HitRules.KillSparkColor, 2.3);
            scene.Fx.Sparks(burst, IsBoss ? 14 : 6, HitRules.WhiteSpark, 1.7, 1.35);
            scene.Shake(IsBoss ? HitRules.BossKillShake : HitRules.KillShake);
            scene.FovPunch(HitRules.KillFov, HitRules.KillFovDur);
            Bar.BeginDying();
            punchT = 0; atkSq = 0; gaitSq = 0;
            G.localScale = Vector3.one * (float)BaseScale;
            dieBaseY = py; dieSy0 = BaseScale; dieOx = px; dieDur = HitRules.KillDur(IsBoss); dieT = 0; dieDusted = false;
        }

        // ── 프레임 ──
        public void Step(float dt, double clk, double worldX)
        {
            if (Removed) return;
            if (Dead) { StepDying(dt); return; }
            UndoFlinch();
            // 위치 추종(논리 x + 월드 오프셋 · 깊이 레인) — 넉백/돌진 연출이 x 를 쥐고 있으면 그쪽이 이긴다
            bool xOwned = knockT >= 0 || lungeT >= 0;
            double k12 = Math.Min(1, dt * EnemyGait.FollowLerp);
            if (!xOwned) px += ((E.X + worldX) - px) * k12;
            pz += (E.Z - pz) * k12;
            bool walking = E.X > scene.StopXOf(E) + EnemyGait.WalkEps;
            gaitSq = 0;
            if (Landed)
            {
                GaitProfile g = Gait;
                double hopU = EnemyGait.HopU(clk, g.Rate, Id);
                if (Fly)
                {
                    py = EnemyGait.FlyY(g, clk, Id);
                    foreach (var w in wings) Set(w.Node, 2, EnemyGait.FlyWing(w.Value, clk, g.WingRate, Id));
                    gaitSq = EnemyGait.FlySq(clk, g.WingRate, g.Sq, Id);
                }
                else if (walking)
                {
                    if (IsWolf)
                    {
                        py = EnemyGait.HopCurve(hopU, g.BobPow) * g.Bob;
                        gaitSq = EnemyGait.GaitSquash(hopU, g.BobPow, g.Sq);
                        for (int j = 0; j < legs4.Count; j++)
                        {
                            double lp = clk * g.LegRate + Id + EnemyGait.WolfLegPhase[Math.Min(j, 3)];
                            Set(legs4[j], 0, EnemyGait.WolfLeg(lp));
                            if (j < knees4.Count) Set(knees4[j].Node, 0, EnemyGait.WolfKnee(knees4[j].Rx0, knees4[j].Front, lp));
                        }
                        gRot[0] = Math.Sin(clk * g.Rate + Id) * g.Pitch;
                        if (tail != null) Set(tail, 2, EnemyGait.WolfTail(clk, g.TailRate, Id));
                    }
                    else if (Hop)
                    {
                        py = EnemyGait.HopCurve(hopU, g.BobPow) * g.Bob;
                        gaitSq = EnemyGait.GaitSquash(hopU, g.BobPow, g.Sq);
                        if (cap != null) Set(cap, 2, EnemyGait.CapTilt(clk, g.Rate, g.CapTilt, Id));
                        DriveCapFlap(clk, g.CapAmp);
                    }
                    else if (IsSlime)
                    {
                        py = EnemyGait.HopCurve(hopU, g.BobPow) * g.Bob;
                        DriveJelly(clk, g.JellyAmp);
                    }
                    else
                    {
                        double ph = clk * g.Rate + Id;
                        py = EnemyGait.HopCurve(hopU, g.BobPow) * g.Bob;
                        gaitSq = EnemyGait.GaitSquash(hopU, g.BobPow, g.Sq);
                        gRot[2] = Math.Sin(ph) * g.Roll;
                        gRot[0] = g.Lean;
                        for (int j = 0; j < gleg.Count; j++) Set(gleg[j], 0, Math.Sin(ph + j * Math.PI) * g.Hip);
                        for (int j = 0; j < bleg.Count; j++)
                        {
                            double lp = ph + j * Math.PI;
                            Set(bleg[j].Hip, 0, EnemyGait.BipedHip(lp, g.Hip));
                            Set(bleg[j].Knee, 0, EnemyGait.BipedKnee(lp));
                        }
                        if (barm.Count > 0)
                            for (int j = 0; j < barm.Count; j++)
                            {
                                double ap = ph + j * Math.PI + Math.PI;
                                Set(barm[j].Sh, 0, EnemyGait.BipedShoulder(ap, g.Arm));
                                Set(barm[j].Elbow, 0, EnemyGait.BipedElbow(ap));
                            }
                        else if (armR != null)
                        {
                            Set(armR, 0, EnemyGait.FallbackArm(ph));
                            if (armL != null) Set(armL, 0, -EnemyGait.FallbackArm(ph));
                        }
                    }
                }
                else
                {
                    double iu = (clk * EnemyGait.IdleRate + Id) / Math.PI;
                    py = EnemyGait.HopCurve(iu, EnemyGait.IdleBobPow) * EnemyGait.IdleBob;
                    gaitSq = EnemyGait.GaitSquash(iu, EnemyGait.IdleBobPow, g.Sq * EnemyGait.IdleSqK);
                    double sway = Math.Sin(clk * EnemyGait.SwayRate + Id * EnemyGait.SwayIdK) * EnemyGait.SwayAmp;
                    gRot[2] += (sway - gRot[2]) * Math.Min(1, dt * EnemyGait.SwayFollow);
                    gRot[0] *= EnemyGait.IdleDampSlow;
                    if (jellyMesh != null) DriveJelly(clk, EnemyGait.IdleJellyAmp);
                    if (capMesh != null) DriveCapFlap(clk, EnemyGait.IdleCapFlap);
                    if (armL != null) Mul(armL, 0, EnemyGait.IdleDampSlow);
                    foreach (var L in bleg) { Mul(L.Hip, 0, EnemyGait.IdleDamp); Mul(L.Knee, 0, EnemyGait.IdleDamp); }
                    foreach (var A in barm) { Mul(A.Sh, 0, EnemyGait.IdleDamp); Set(A.Elbow, 0, Get(A.Elbow, 0) + (EnemyGait.IdleElbow - Get(A.Elbow, 0)) * EnemyGait.IdleElbowFollow); }
                    foreach (var lg in gleg) Mul(lg, 0, EnemyGait.IdleDamp);
                    foreach (var kn in knees4) Set(kn.Node, 0, Get(kn.Node, 0) + (kn.Rx0 - Get(kn.Node, 0)) * EnemyGait.IdleKneeFollow);
                    if (IsWolf) foreach (var lg in legs4) Mul(lg, 0, EnemyGait.IdleDamp);
                }
            }
            StepKnock(dt);
            StepLunge(dt, worldX);
            StepSpawn(dt);
            DriveFlinch(dt);
            // 블롭 섀도우 추적
            if (blob != null)
            {
                double by = Math.Max(0, py);
                blob.transform.position = ThreeSpace.Pos(px, BlobShadow.Y, pz);
                if (Fly)
                {
                    blob.transform.localScale = Vector3.one * (float)(blobBase * EnemyGait.FlyBlobScale(by));
                    var c = FxMaterials.GetColor(blobMat); c.a = (float)(blobBaseO * EnemyGait.FlyBlobOpacity(by)); FxMaterials.SetColor(blobMat, c);
                }
                else blob.transform.localScale = Vector3.one * (float)(blobBase * EnemyGait.GroundBlobScale(by));
            }
            // 히트축 스쿼시 × 보행/공격 스쿼시
            double punchA = 0;
            if (punchT > 0 && Landed)
            {
                punchT = Math.Max(0, punchT - dt);
                double el = punchDur - punchT;
                double a = HitRules.Punch(el, punchDur, punchHold, punchAmp);
                if (punchT > 0) punchA = a;
            }
            if (Landed && !scaleLocked)
            {
                double s = atkSq != 0 ? atkSq : gaitSq, sx, sy, sz;
                EnemyGait.BodyScale(BaseScale, punchA, s, out sx, out sy, out sz);
                G.localScale = new Vector3((float)sx, (float)sy, (float)sz);
            }
            ApplyGroup();
            double ratio = E.MaxHp.IsZero ? 0 : Col_Clamp(E.Hp.RatioTo(E.MaxHp), 0, 1);
            Bar.Drive(ratio, dt);
            Bar.SetPosition(ThreeSpace.Pos(px, py, pz));
        }

        void StepKnock(float dt)
        {
            if (knockT < 0) return;
            knockT += dt;
            double k = Math.Min(1, knockT / knockDur);
            double p = HitRules.KnockReturn(k);
            px = knockOx + p * knockKb;
            gRot[2] = -p * knockRoll;
            if (k >= 1) { knockT = -1; gRot[2] = 0; }
        }

        void StepLunge(float dt, double worldX)
        {
            if (lungeT < 0) return;
            lungeT += dt;
            double k = Math.Min(1, lungeT / HitRules.EnemyAttackDur);
            double dx, sq;
            HitRules.Lunge(k, out dx, out sq);
            px = lungeOx + dx; atkSq = sq;
            if (armRJ != null)
            {
                double sh, el, rz;
                HitRules.LungeArm(k, out sh, out el, out rz);
                Set(armRJ.Sh, 0, sh); Set(armRJ.Elbow, 0, el); gRot[2] = rz;
            }
            else if (armR != null) Set(armR, 0, HitRules.LungeFallbackArm(k));
            if (k >= 1)
            {
                lungeT = -1; atkSq = 0;
                px = E.X + worldX;
                if (armR != null) Set(armR, 0, 0);
                if (armRJ != null) { Set(armRJ.Elbow, 0, EnemyGait.IdleElbow); gRot[2] = 0; }
            }
        }

        void StepSpawn(float dt)
        {
            if (spawnT < 0) return;
            spawnT += dt;
            double k = Math.Min(1, spawnT / 0.42);
            double e2 = 1 - Math.Pow(1 - k, 3);
            double over = Math.Sin(Math.PI * Math.Min(1, k / 0.85)) * 0.08;
            G.localScale = Vector3.one * (float)(spawnTarget * (0.42 + 0.58 * e2 + over));
            if (k >= 1) { spawnT = -1; G.localScale = Vector3.one * spawnTarget; scaleLocked = false; }
        }

        // ── 플린치(driveFlinch/undoFlinch) ──
        void AddF(Transform t, int axis, double v)
        {
            if (t == null || v == 0) return;
            double[] r; if (!rot.TryGetValue(t, out r)) return;
            r[axis] += v; ThreeSpace.Apply(t, r);
            var off = new double[3]; off[axis] = v;
            flinchOff.Add(new KeyValuePair<Transform, double[]>(t, off));
        }

        void DriveFlinch(float dt)
        {
            if (!(flinchT > 0) || Dead) return;
            flinchT = Math.Max(0, flinchT - dt);
            double v = 1 - flinchT / flinchDur;
            double a = flinchAmp * HitRules.FlinchWeight(v);
            if (a == 0) return;
            bodyFlinch[0] = a * HitRules.FlBodyX; bodyFlinch[1] = a * HitRules.FlBodyY;
            gRot[0] += bodyFlinch[0]; gRot[1] += bodyFlinch[1]; bodyFlinched = true;
            ThreeSpace.Apply(G, gRot);
            if (barm.Count > 0)
                for (int j = 0; j < barm.Count; j++)
                {
                    AddF(barm[j].Sh, 0, a * HitRules.FlArmSh);
                    AddF(barm[j].Sh, 2, (j == 0 ? 1 : -1) * a * HitRules.FlArmShZ);
                    AddF(barm[j].Elbow, 0, a * HitRules.FlArmElbow);
                }
            else { AddF(armR, 0, a * HitRules.FlArmFallback); AddF(armL, 0, a * HitRules.FlArmFallback); }
            if (armRJ != null && barm.Count == 0) { AddF(armRJ.Sh, 0, a * HitRules.FlArmFallback); AddF(armRJ.Elbow, 0, a * HitRules.FlArmElbow); }
            foreach (var L in bleg) { AddF(L.Hip, 0, a * HitRules.FlLegHip); AddF(L.Knee, 0, -Math.Abs(a) * HitRules.FlLegKnee); }
            foreach (var lg in gleg) AddF(lg, 0, a * HitRules.FlGleg);
            if (IsWolf) foreach (var lg in legs4) AddF(lg, 0, a * HitRules.FlWolfLeg);
            if (tail != null) AddF(tail, 2, a * HitRules.FlTail);
            foreach (var w in wings) AddF(w.Node, 2, (w.Value != 0 ? w.Value : 1) * a * HitRules.FlWing);
            if (cap != null) AddF(cap, 2, a * HitRules.FlCap);
        }

        void UndoFlinch()
        {
            if (bodyFlinched) { gRot[0] -= bodyFlinch[0]; gRot[1] -= bodyFlinch[1]; bodyFlinched = false; }
            for (int i = 0; i < flinchOff.Count; i++)
            {
                var kv = flinchOff[i];
                double[] r; if (!rot.TryGetValue(kv.Key, out r)) continue;
                for (int a = 0; a < 3; a++) r[a] -= kv.Value[a];
                ThreeSpace.Apply(kv.Key, r);
            }
            flinchOff.Clear();
        }

        // ── 젤리 · 갓 플랩(정점 웨이브) ──
        void DriveJelly(double clk, double amp)
        {
            if (jellyMesh == null) return;
            double ph = clk * EnemyGait.JellyRate + Id;
            var v = new Vector3[jellyBase.Length];
            for (int i = 0; i < jellyBase.Length; i++)
            {
                float h = jellyCy + jellyBase[i].y;
                double sy, sr; EnemyGait.JellyAt(ph, h / jellyH, amp, out sy, out sr);
                v[i] = new Vector3((float)(jellyBase[i].x * sr), (float)(h * sy) - jellyCy, (float)(jellyBase[i].z * sr));
            }
            jellyMesh.vertices = v;
            jellyMesh.RecalculateNormals();
            jellyMesh.RecalculateBounds();
            foreach (var f in jellyFollow)
            {
                double sy, sr; EnemyGait.JellyAt(ph, f.Value.y / jellyH, amp, out sy, out sr);
                f.Key.localPosition = new Vector3((float)(f.Value.x * sr), (float)(f.Value.y * sy), (float)(f.Value.z * sr));
            }
        }

        void DriveCapFlap(double clk, double amp)
        {
            if (capMesh == null) return;
            double ph = clk * EnemyGait.CapFlapRate + Id;
            var v = new Vector3[capBase.Length];
            for (int i = 0; i < capBase.Length; i++)
            {
                float t = Mathf.Sqrt(capBase[i].x * capBase[i].x + capBase[i].z * capBase[i].z) / capRMax;
                v[i] = new Vector3(capBase[i].x, capBase[i].y + (float)EnemyGait.CapFlapAt(ph, t, amp), capBase[i].z);
            }
            capMesh.vertices = v;
            capMesh.RecalculateNormals();
            capMesh.RecalculateBounds();
            if (capTop != null) { var p = capTop.localPosition; p.y = capTopY + (float)EnemyGait.CapFlapAt(ph, EnemyGait.CapTopT, amp); capTop.localPosition = p; }
        }

        // ── 쓰러짐(killEnemy 의 addAnim) ──
        void StepDying(float dt)
        {
            dieT += dt;
            double k = Math.Min(1, dieT / dieDur);
            double kHit = HitRules.KillHitS / dieDur, kDown = HitRules.KillDownS(IsBoss) / dieDur;
            if (k < kHit) px = dieOx + (k / kHit) * HitRules.KillHitDx;
            else if (k < kDown)
            {
                double f = (k - kHit) / (kDown - kHit);
                double fe = HitRules.CollapseEase(f);
                if (bleg.Count > 0) foreach (var L in bleg) { Set(L.Knee, 0, HitRules.KillKneeBase - fe * HitRules.KillKneeBend); Set(L.Hip, 0, fe * HitRules.KillHipBend); }
                else { var s = G.localScale; s.y = (float)(dieSy0 * (1 - HitRules.KillNoLegSquash * fe)); G.localScale = s; }
                px = dieOx + HitRules.KillHitDx + f * HitRules.KillFallDx;
                gRot[2] = -fe * HitRules.KillRoll;
                py = dieBaseY * (1 - f);
            }
            else
            {
                if (!dieDusted)
                {
                    dieDusted = true;
                    scene.Fx.Sparks(new Vector3((float)(px + 0.25), 0.05f, (float)pz), IsBoss ? 12 : 7, HitRules.DustColor, 0.9);
                    foreach (var r in Rig.Renderers) r.shadowCastingMode = ShadowCastingMode.Off;
                }
                gRot[2] = -HitRules.KillRoll;
                py = 0;
                px = dieOx + HitRules.KillRestDx;
                double kHold = kDown + HitRules.KillHoldS / dieDur;
                double f = k < kHold ? 0 : Col_Clamp((k - kHold) * dieDur / HitRules.KillDissolveS, 0, 1);
                // 사망 디졸브(노이즈 알파 클립 셰이더)는 없다 — 같은 구간에 시체를 줄여 보내고 블롭도 같이 줄인다(결정 기록).
                G.localScale = Vector3.one * (float)(dieSy0 * (1 - f));
                if (blob != null) blob.transform.localScale = Vector3.one * (float)(blobBase * 0.95 * (1 - f));
            }
            ApplyGroup();
            Bar.Drive(0, dt);
            Bar.SetPosition(ThreeSpace.Pos(px, py, pz));
            if (k >= 1) Remove();
        }

        public void Remove()
        {
            if (Removed) return;
            Removed = true;
            Rig.Destroy();
            Bar.Destroy();
            if (blob != null) { UnityEngine.Object.Destroy(blob.gameObject); UnityEngine.Object.Destroy(blobMat); }
        }
    }
}
