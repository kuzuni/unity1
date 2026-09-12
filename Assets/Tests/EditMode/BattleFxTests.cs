using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core.BattleFx;
using Forge.Core.Data;
using Forge.Core.Hero;

namespace Forge.Tests
{
    /// <summary>
    /// T8 — Core `EnemyGait`·`HitRules` 가 정본 `scene3d.js` 를 실제로 돌린 벡터(`Vectors/t8-battlefx.json` · `tools/battlefx_vectors.js`)와 같은 값을 내는가.
    /// 표(ENEMY_GAIT·KIND_COLOR·상수) · gaitOf 보스 변환 · hopCurve/gaitSquash 격자 · easeBack · 종 선택 · 파편색 씨앗 · 돌진 곡선 · 플린치 가중치 · 피격 수치.
    /// </summary>
    public class BattleFxTests
    {
        static JsonObject _doc;
        static JsonObject Doc
        {
            get
            {
                if (_doc != null) return _doc;
                string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
                string file = Path.Combine(root, "Assets", "Tests", "EditMode", "Vectors", "t8-battlefx.json");
                if (!File.Exists(file)) throw new FileNotFoundException("T8 벡터가 없다 — node tools/battlefx_vectors.js 로 뽑는다: " + file);
                _doc = MiniJson.ParseObject(File.ReadAllText(file));
                return _doc;
            }
        }

        const double Eps = 1e-9;

        static void AssertGait(string what, JsonObject e, GaitProfile g)
        {
            Assert.AreEqual(J.Num(e["rate"]), g.Rate, Eps, what + " rate");
            Assert.AreEqual(J.Num(e["bob"]), g.Bob, Eps, what + " bob");
            Assert.AreEqual(J.Num(e["bobPow"]), g.BobPow, Eps, what + " bobPow");
            Assert.AreEqual(J.Num(e["roll"]), g.Roll, Eps, what + " roll");
            Assert.AreEqual(J.Num(e["hip"]), g.Hip, Eps, what + " hip");
            Assert.AreEqual(J.Num(e["arm"]), g.Arm, Eps, what + " arm");
            Assert.AreEqual(J.Num(e["lean"]), g.Lean, Eps, what + " lean");
            Assert.AreEqual(J.Num(e["sq"]), g.Sq, Eps, what + " sq");
            Assert.AreEqual(J.Num(e["legRate"]), g.LegRate, Eps, what + " legRate");
            Assert.AreEqual(J.Num(e["tailRate"]), g.TailRate, Eps, what + " tailRate");
            Assert.AreEqual(J.Num(e["pitch"]), g.Pitch, Eps, what + " pitch");
            Assert.AreEqual(J.Num(e["hover"]), g.Hover, Eps, what + " hover");
            Assert.AreEqual(J.Num(e["wingRate"]), g.WingRate, Eps, what + " wingRate");
            Assert.AreEqual(J.Num(e["capTilt"]), g.CapTilt, Eps, what + " capTilt");
            Assert.AreEqual(J.Num(e["capAmp"]), g.CapAmp, Eps, what + " capAmp");
            Assert.AreEqual(J.Num(e["jellyAmp"]), g.JellyAmp, Eps, what + " jellyAmp");
        }

        [Test]
        public void 상수와_표가_정본과_같다()
        {
            var c = J.Obj(Doc["consts"]);
            Assert.AreEqual(J.Num(c["ENEMY_VS"]), EnemyGait.EnemyVs, Eps);
            Assert.AreEqual(J.Num(c["BOSS_SCALE"]), EnemyGait.BossScale, Eps);
            Assert.AreEqual(J.Num(c["BOSS_IMPACT"]), Forge.Core.Battle.BattleRules.BossImpact, Eps, "T7 BattleRules.BossImpact");
            Assert.AreEqual(J.Num(c["BOSS_SPAWN_X"]), Forge.Core.Battle.BattleRules.BossSpawnX, Eps);
            var kc = J.Obj(c["KIND_COLOR"]);
            Assert.AreEqual(kc.Count, EnemyGait.KindColor.Count);
            foreach (var k in kc.Keys) Assert.AreEqual(J.Int(kc[k]), EnemyGait.KindColor[k], "KIND_COLOR " + k);
            var gt = J.Obj(c["ENEMY_GAIT"]);
            foreach (var k in gt.Keys) AssertGait("ENEMY_GAIT " + k, J.Obj(gt[k]), EnemyGait.Table(k));
            CollectionAssert.AreEqual(new[] { "slime", "golem", "goblin", "bat", "mushroom", "wolf", "imp" }, EnemyGait.Kinds);
        }

        [Test]
        public void gaitOf_보스_변환이_정본과_같다()
        {
            var g = J.Obj(Doc["gaitOf"]);
            int n = 0;
            foreach (var k in g.Keys)
            {
                var pair = J.Obj(g[k]);
                AssertGait(k + " normal", J.Obj(pair["normal"]), EnemyGait.GaitOf(k, false));
                AssertGait(k + " boss", J.Obj(pair["boss"]), EnemyGait.GaitOf(k, true));
                n++;
            }
            Assert.AreEqual(9, n, "종 7 + _default + 없는 종");
        }

        [Test]
        public void hopCurve_gaitSquash_격자가_정본과_같다()
        {
            var rows = J.Arr(Doc["hop"]);
            Assert.Greater(rows.Count, 200);
            foreach (var r0 in rows)
            {
                var r = J.Obj(r0);
                double u = J.Num(r["u"]), bp = J.Num(r["bobPow"]);
                Assert.AreEqual(J.Num(r["hop"]), EnemyGait.HopCurve(u, bp), Eps, "hopCurve u=" + u + " bobPow=" + bp);
                Assert.AreEqual(J.Num(r["sq"]), EnemyGait.GaitSquash(u, bp, 0.1), Eps, "gaitSquash u=" + u + " bobPow=" + bp);
            }
            Assert.AreEqual(0, EnemyGait.GaitSquash(0.5, 1, 0), "amp 0 → 0");
        }

        [Test]
        public void easeBack_종선택_파편색이_정본과_같다()
        {
            foreach (var r0 in J.Arr(Doc["back"]))
            {
                var r = J.Obj(r0);
                Assert.AreEqual(J.Num(r["v"]), HeroEase.Back(J.Num(r["t"])), Eps, "easeBack t=" + r["t"]);
            }
            foreach (var r0 in J.Arr(Doc["kind"]))
            {
                var r = J.Obj(r0);
                Assert.AreEqual(J.Str(r["kind"]), EnemyGait.KindOf(J.Int(r["id"]), J.Int(r["chapter"])), "id " + r["id"] + " ch " + r["chapter"]);
            }
            var data = GameData.LoadDirectory(DataDir.Path);
            var shard = J.Obj(Doc["shard"]);
            foreach (var k in shard.Keys)
            {
                var s = J.Obj(shard[k]);
                MobModel model;
                Assert.IsTrue(data.Enemies.Models.TryGet(k, out model), "적 표에 " + k);
                int seed = HitRules.VolumeWeightedColor(model.Parts);
                Assert.AreEqual(J.Int(s["shardC"]), seed, k + " 부피 가중 평균색");
                Assert.AreEqual(J.Int(s["killC"]), HitRules.ShardColor(seed), k + " 처치 파편색");
                double cell = model.Cell > 0 ? model.Cell : EnemyGait.EnemyVs;
                double maxY = 0;
                foreach (var p in model.Parts) { double t = p.At[1] + p.Box[1] / 2.0; if (t > maxY) maxY = t; }
                var g = EnemyGait.Table(k);
                double topY = maxY * cell + EnemyGait.TopYPad + (model.Fly ? g.Hover : 0);
                Assert.AreEqual(J.Num(s["topY"]), topY, Eps, k + " topY");
                Assert.AreEqual(J.Bool(s["fly"]), model.Fly, k + " fly");
                Assert.AreEqual(J.Bool(s["hop"]), model.Hop, k + " hop");
                Assert.AreEqual(J.Bool(s["jelly"]), model.Jelly, k + " jelly");
            }
        }

        [Test]
        public void 적_돌진_곡선이_정본과_같다()
        {
            var L = J.Obj(Doc["lunge"]);
            Assert.AreEqual(J.Num(L["dur"]), HitRules.EnemyAttackDur, Eps);
            foreach (var r0 in J.Arr(L["armRJ"]))
            {
                var r = J.Obj(r0);
                double k = J.Num(r["k"]), dx, sq, sh, el, rz;
                HitRules.Lunge(k, out dx, out sq);
                HitRules.LungeArm(k, out sh, out el, out rz);
                Assert.AreEqual(J.Num(r["dx"]), dx, 1e-9, "dx k=" + k);
                Assert.AreEqual(J.Num(r["atkSq"]), sq, 1e-9, "atkSq k=" + k);
                Assert.AreEqual(J.Num(r["sh"]), sh, 1e-9, "sh k=" + k);
                Assert.AreEqual(J.Num(r["elbow"]), el, 1e-9, "elbow k=" + k);
                Assert.AreEqual(J.Num(r["rz"]), rz, 1e-9, "rz k=" + k);
            }
            foreach (var r0 in J.Arr(L["fallback"]))
            {
                var r = J.Obj(r0);
                double k = J.Num(r["k"]);
                Assert.AreEqual(J.Num(r["armR"]), HitRules.LungeFallbackArm(k), 1e-9, "armR k=" + k);
            }
        }

        [Test]
        public void 플린치_가중치가_정본과_같다()
        {
            var F = J.Obj(Doc["flinch"]);
            foreach (var r0 in J.Arr(F["rows"]))
            {
                var r = J.Obj(r0);
                double v = J.Num(r["v"]);
                // v = 1 은 flinchT 가 0 인 프레임 — driveFlinch 가 `!(flinchT > 0)` 로 일찍 돌아가 가산 0 (씬도 같은 문을 지킨다).
                double a = v >= 1 ? 0 : 1 * HitRules.FlinchWeight(v);
                Assert.AreEqual(J.Num(r["gx"]), a * HitRules.FlBodyX, 1e-9, "몸 rx v=" + v);
                Assert.AreEqual(J.Num(r["gy"]), a * HitRules.FlBodyY, 1e-9, "몸 ry");
                Assert.AreEqual(J.Num(r["sh0x"]), a * HitRules.FlArmSh, 1e-9, "어깨 rx");
                Assert.AreEqual(J.Num(r["sh0z"]), a * HitRules.FlArmShZ, 1e-9, "어깨 L rz");
                Assert.AreEqual(J.Num(r["sh1z"]), -a * HitRules.FlArmShZ, 1e-9, "어깨 R rz");
                Assert.AreEqual(J.Num(r["el"]), a * HitRules.FlArmElbow, 1e-9, "팔꿈치");
                Assert.AreEqual(J.Num(r["hip"]), a * HitRules.FlLegHip, 1e-9, "고관절");
                Assert.AreEqual(J.Num(r["knee"]), -Math.Abs(a) * HitRules.FlLegKnee, 1e-9, "무릎");
                Assert.AreEqual(J.Num(r["gleg"]), a * HitRules.FlGleg, 1e-9, "기둥 다리");
                Assert.AreEqual(J.Num(r["tail"]), a * HitRules.FlTail, 1e-9, "꼬리");
                Assert.AreEqual(J.Num(r["wing"]), -1 * a * HitRules.FlWing, 1e-9, "날개 s=-1");
                Assert.AreEqual(J.Num(r["cap"]), a * HitRules.FlCap, 1e-9, "갓");
                Assert.AreEqual(J.Num(r["leg"]), a * HitRules.FlWolfLeg, 1e-9, "늑대 다리");
            }
            Assert.AreEqual(J.Num(F["fallbackArmAtHalf"]), HitRules.FlinchWeight(0.5) * HitRules.FlArmFallback, 1e-9, "폴백 팔");
        }

        [Test]
        public void 피격_수치가_정본과_같다()
        {
            foreach (var r0 in J.Arr(Doc["hit"]))
            {
                var r = J.Obj(r0);
                double sev = J.Num(r["sev"]);
                bool crit = J.Bool(r["crit"]), kill = J.Bool(r["kill"]);
                string kind = J.Str(r["kind"]);
                string tag = " sev=" + sev + " crit=" + crit + " kill=" + kill;
                Assert.AreEqual(J.Num(r["kb"]), HitRules.Knockback(sev, crit), 1e-9, "넉백" + tag);
                Assert.AreEqual(J.Num(r["knockDur"]), HitRules.KnockDur(crit), Eps, "넉백 길이" + tag);
                Assert.AreEqual(J.Num(r["punchDur"]), HitRules.PunchDur(crit), Eps, "펀치 길이" + tag);
                Assert.AreEqual(J.Num(r["punchHold"]), HitRules.PunchHold(crit), Eps, "펀치 유지" + tag);
                Assert.AreEqual(J.Num(r["punchAmp"]), HitRules.PunchAmp(sev, crit), 1e-9, "펀치 진폭" + tag);
                Assert.AreEqual(J.Num(r["flinchDur"]), HitRules.FlinchDur(crit), Eps, "플린치 길이" + tag);
                Assert.AreEqual(J.Num(r["flinchAmp"]), HitRules.FlinchAmp(sev, crit), 1e-9, "플린치 진폭" + tag);
                Assert.AreEqual(J.Num(r["shake"]), crit ? HitRules.CritShake : 0, Eps, "셰이크" + tag);
                if (crit) { var f = J.Arr(r["fov"]); Assert.AreEqual(J.Num(f[0]), HitRules.CritFov, Eps); Assert.AreEqual(J.Num(f[1]), HitRules.CritFovDur, Eps); }
                var num = J.Obj(r["num"]);
                Assert.AreEqual(J.Str(num["cls"]), HitRules.DmgClass(kill, kind, crit), "숫자 등급" + tag);
                Assert.AreEqual(J.Num(num["rise"]), HitRules.DmgRise(sev, crit), 1e-9, "숫자 상승" + tag);
                Assert.AreEqual(J.Num(num["scale"]), HitRules.DmgPop(sev), 1e-9, "숫자 크기" + tag);
                Assert.AreEqual(J.Num(num["y"]), HitRules.DmgYFallback(1.1, 1, crit), 1e-9, "숫자 y(바 없음 갈래)" + tag);
                var sh = J.Obj(r["shards"]);
                Assert.AreEqual(J.Int(sh["n"]), HitRules.HitShards(sev, crit), "파편 수" + tag);
                Assert.AreEqual(J.Int(sh["c"]), crit ? HitRules.CritShardColor : HitRules.HitShardColor, "파편 색" + tag);
                var sp = J.Obj(r["sparks"]);
                Assert.AreEqual(J.Int(sp["n"]), HitRules.HitSparks(sev, crit), "불티 수" + tag);
                Assert.AreEqual(J.Int(sp["c"]), crit ? HitRules.CritSparkColor : HitRules.HitSparkColor, "불티 색" + tag);
            }
        }

        [Test]
        public void 펀치_감쇠와_셰이크_감쇠_형태()
        {
            Assert.AreEqual(0.1, HitRules.Punch(0.01, 0.24, 0.055, 0.1), Eps, "유지 구간은 최대 압축");
            double a1 = HitRules.Punch(0.06, 0.24, 0.055, 0.1), a2 = HitRules.Punch(0.2, 0.24, 0.055, 0.1);
            Assert.Greater(a1, a2, "감쇠");
            Assert.Less(HitRules.Punch(0.055 + (0.24 - 0.055) * 0.31, 0.24, 0.055, 0.1), 0, "반대쪽 오버슈트");
            Assert.AreEqual(0.3 * Math.Pow(0.001, 0.5), HitRules.ShakeDecay(0.3, 0.5), Eps);
            Assert.AreEqual(62 * (1 - 0.026), HitRules.FovPunch(62, 0.026, 0), Eps);
            Assert.AreEqual(62, HitRules.FovPunch(62, 0.026, 1), Eps);
            double sx, sy, sz;
            EnemyGait.BodyScale(1.9, 0.1, 0.05, out sx, out sy, out sz);
            Assert.AreEqual(1.9 * 0.9 * 0.975, sx, Eps);
            Assert.AreEqual(1.9 * 1.092 * 1.05, sy, Eps);
            Assert.AreEqual(1.9 * 1.014 * 0.975, sz, Eps);
            Assert.AreEqual(HitRules.HpBar.FoeFillLow, HitRules.HpBar.FillColor(true, 0.1));
            Assert.AreEqual(HitRules.HpBar.HeroFillMid, HitRules.HpBar.FillColor(false, 0.4));
        }
    }
}
