using System;
using UnityEngine;
using Forge.Core.Battle;
using Forge.Core.BattleFx;
using Forge.Core.Data;
using Forge.Game.Hero;
using Forge.Game.Voxel;

namespace Forge.Game.Battle
{
    /// <summary>
    /// 전투 씬의 영웅(T8) — T6 <see cref="HeroRig"/>(리그·클립·파지·스윙 시계) 위에 원작 `heroAttack` 의 돌진(12060~) · `heroHit`(13324) · `heroDown`(13367) · `heroRevive`(13456) ·
    /// `update` 의 걷기/대기 전환·행군(18556~18595) · 머리 위 HP 바(4749~) 를 얹는다. 위치는 three 로 계산해 <see cref="ThreeSpace"/> 로 넣는다.
    /// </summary>
    public sealed class HeroView
    {
        public readonly HeroRig Rig;
        public readonly HpBar Bar;
        public bool Dead { get; private set; }
        public bool Attacking { get { return Rig.Attacking; } }
        public double ReviveT { get; private set; }
        public double X { get { return hx; } }
        public double Y { get { return hy; } }

        double hx, hy, hz;
        readonly double[] gRot = { 0, EnemyGait.HeroYaw, 0 };
        // 돌진
        double dashFrom, dashTo;
        // 피격 넉백
        double knockT = -1, knockOx, knockKb, knockRoll;
        // 사망 먼지
        double downT = -1; bool dustKnee, dustBody;
        readonly BattleScene scene;

        public HeroView(BattleScene scene, Transform parent, string wtypeId, WeaponType def)
        {
            this.scene = scene;
            Rig = HeroRig.Create(parent, "Hero");
            Rig.ManualStep = true;
            Rig.Equip(wtypeId, def);
            hx = BattleRules.HeroX + scene.WorldX; hy = 0; hz = 0;
            Apply();
            Bar = new HpBar(parent, "HeroHpBar", false, 0, 1);
            Bar.SetPosition(ThreeSpace.Pos(hx, hy + HitRules.HpBar.HeroY, hz));
        }

        void Apply()
        {
            Rig.transform.localPosition = ThreeSpace.Pos(hx, hy, hz);
            ThreeSpace.Apply(Rig.transform, gRot);
        }

        /// <summary>`heroAttack(targetId)`: 클립 스윙(T6) + 돌진(`dash`). targetX = 표적의 월드 x(three) · 없으면 null.</summary>
        public void Attack(double? targetX)
        {
            if (Dead) return;
            double W = scene.WorldX;
            dashTo = Math.Min(targetX.HasValue ? targetX.Value - HitRules.DashStop : BattleRules.MeleeX + W, HitRules.DashMaxX + W);
            dashFrom = BattleRules.HeroX + W;
            Rig.Attack(() =>
            {
                hx = BattleRules.HeroX + scene.WorldX; hy = 0;
                gRot[1] = EnemyGait.HeroYaw; gRot[2] = 0;
            });
        }

        /// <summary>`heroHit(sev, dmg)`.</summary>
        public void Hit(double sev, string dmgText)
        {
            sev = Math.Max(0, Math.Min(1, sev != 0 ? sev : HitRules.HeroSevDefault));
            Rig.Hit(sev);
            knockOx = hx; knockKb = HitRules.HeroKnockback(sev); knockRoll = HitRules.HeroKnockRoll(sev); knockT = 0;
            FxCatalog.Play(BattleScene.FxHeroHit, ThreeSpace.Pos(hx, hy + 0.9, hz), 0.8f);
            Bar.Hit(sev);
            scene.Shake(HitRules.HeroShake(sev));
            if (dmgText != null)
            {
                double barY = hy + HitRules.HpBar.HeroY;
                scene.Numbers.Spawn(new Vector3((float)(hx + HitRules.HeroDmgX), (float)(barY + HitRules.HpBar.BgH * HitRules.HeroDmgYK), (float)hz), dmgText, "dmg-hero",
                    -(HitRules.HeroDmgDxMin + UnityEngine.Random.value * (HitRules.HeroDmgDxMax - HitRules.HeroDmgDxMin)), HitRules.HeroDmgRise(sev), HitRules.HeroDmgPop(sev));
            }
        }

        /// <summary>`heroDown()` — Death 클립 한 번 · 자동 전환 잠금 · 2단 접지 먼지(무릎 0.55s · 몸통 1.25s).</summary>
        public void Down()
        {
            if (Dead) return;
            Dead = true;
            Rig.Play("Death", true);
            scene.Shake(HitRules.HeroDownShake);
            downT = 0; dustKnee = dustBody = false;
        }

        /// <summary>`heroRevive()` — Revive 클립 · 잠금 0.85초 · 초록 불티.</summary>
        public void Revive()
        {
            if (!Dead) return;
            Dead = false;
            ReviveT = BattleRules.DeathRiseMs / 1000;
            Rig.Play("Revive", true);
            Vector3 p = BoneThree("pelvis"); p.y = (float)hy;
            scene.Fx.Sparks(p + new Vector3(0, 0.35f, 0), 12, HitRules.ReviveSpark, 1.4);
            FxCatalog.Play(BattleScene.FxRevive, ThreeSpace.Pos(p.x, p.y + 0.3, p.z), 1f);
        }

        Vector3 BoneThree(string name)
        {
            var b = Rig.Bone(name);
            if (b == null) return new Vector3((float)hx, (float)hy, (float)hz);
            Vector3 w = b.position;
            return new Vector3(w.x, w.y, -w.z);
        }

        public void Step(float dt, bool walking, double worldX, double hpRatio)
        {
            // 행군 중 x 는 씬(worldX)이 민다 · 돌진/넉백이 x 를 쥐면 그쪽이 이긴다
            bool dashing = Rig.Attacking;
            if (!dashing && knockT < 0) hx = BattleRules.HeroX + worldX;
            Rig.Step(dt);
            if (dashing)
            {
                double k = Rig.AttackTime > 0 ? Math.Min(1, Rig.AttackElapsed / Rig.AttackTime) : 1;
                double lunge = HitRules.Lunge01(k);
                hx = dashFrom + (dashTo - dashFrom) * lunge * HitRules.DashK;
                hy = Math.Sin(k * Math.PI) * HitRules.DashJump;
                gRot[1] = EnemyGait.HeroYaw + Math.Sin(k * Math.PI) * HitRules.DashTwist;
                gRot[2] = -Math.Sin(k * Math.PI) * HitRules.DashLean;
            }
            if (knockT >= 0)
            {
                knockT += dt;
                double k = Math.Min(1, knockT / HitRules.HeroKnockDur);
                double p = Math.Sin(k * Math.PI);
                hx = knockOx - p * knockKb;
                gRot[2] = p * knockRoll;
                if (k >= 1) { knockT = -1; hx = BattleRules.HeroX + worldX; gRot[2] = 0; }
            }
            if (ReviveT > 0) ReviveT -= dt;
            if (!Rig.Attacking && !Dead && !(ReviveT > 0))
            {
                Rig.Play(walking ? "Walking" : "Idle");
                hy = 0;
            }
            if (downT >= 0)
            {
                downT += dt;
                if (!dustKnee && downT >= 0.55) { dustKnee = true; Vector3 p = BoneThree("kneeL"); p.y = (float)hy + 0.06f; scene.Fx.Sparks(p + new Vector3(0, 0.08f, 0), 7, HitRules.DustColor, 0.8); scene.Shake(HitRules.HeroKneeShake); }
                if (!dustBody && downT >= 1.25)
                {
                    dustBody = true;
                    Vector3 sh = BoneThree("shoulderL"), pv = BoneThree("pelvis"); sh.y = pv.y = (float)hy + 0.06f;
                    scene.Fx.Sparks(sh + new Vector3(0, 0.1f, 0), 11, HitRules.DustColor, 1.25);
                    scene.Fx.Sparks(pv + new Vector3(0, 0.1f, 0), 9, HitRules.DustColor, 1.05);
                    scene.Shake(HitRules.HeroBodyShake);
                    downT = -1;
                }
            }
            Apply();
            Bar.SetVisible(!Dead);
            Bar.Drive(hpRatio, dt);
            Bar.SetPosition(ThreeSpace.Pos(hx, hy + HitRules.HpBar.HeroY, hz));
        }
    }
}
