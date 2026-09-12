#!/usr/bin/env node
'use strict';
// T8 — 정본 scene3d.js 의 적 보행·피격 «순수 규칙» 을 실제로 실행해 벡터를 뽑는다.
//   node tools/battlefx_vectors.js [--src <wwwww>] [--out Assets/Tests/EditMode/Vectors/t8-battlefx.json] [--check]
// 실행하는 것: ENEMY_GAIT·KIND_COLOR·ENEMY_VS·BOSS_SCALE 표 · gaitOf(종 7 × 보스) · hopCurve/gaitSquash 격자 · ProChar.easeBack 격자 ·
// enemyAttack 의 돌진 클로저(addAnim 을 가로채 k 격자에서 dx·_atkSq·관절각을 읽는다) · driveFlinch 가중치(가짜 anim 으로 rotation 가산분을 읽는다) ·
// hitEnemy 의 넉백·펀치·플린치 수치(addAnim/파티클 함수를 막고 m 레코드에 남는 값을 읽는다). C# `EnemyGait`·`HitRules`(Core/BattleFx) 가 같은 값을 내는지 EditMode `BattleFxTests` 가 잰다.
const fs = require('fs');
const path = require('path');
const vm = require('vm');
const ROOT = path.resolve(__dirname, '..');

function args() {
    const a = process.argv.slice(2);
    const o = { src: path.join(ROOT, '.wwwww-src'), out: path.join(ROOT, 'Assets', 'Tests', 'EditMode', 'Vectors', 't8-battlefx.json'), check: false };
    for (let i = 0; i < a.length; i++) {
        if (a[i] === '--src') o.src = a[++i];
        else if (a[i] === '--out') o.out = a[++i];
        else if (a[i] === '--check') o.check = true;
    }
    return o;
}

const LOAD = ['bignum.js', 'util.js', 'balance-data.js', 'gamedata.js', 'voxel.js', 'mobs.js', 'mobdata.js',
    'mobs-pets.js', 'mobs-mounts.js', 'mobs-enemies.js', 'mobs-skillfx.js', 'mobs-props.js', 'prochar.js', 'scene3d.js'];

function load(src) {
    const web = path.join(src, 'web');
    const sb = { console };
    sb.self = sb;
    vm.createContext(sb);
    vm.runInContext(fs.readFileSync(path.join(web, 'lib', 'three.min.js'), 'utf8'), sb, { filename: 'three.min.js' });
    for (const f of LOAD) vm.runInContext(fs.readFileSync(path.join(web, 'js', f), 'utf8'), sb, { filename: f });
    const g = (expr) => vm.runInContext(expr, sb);
    return { THREE: sb.THREE, Scene3D: g('Scene3D'), ProChar: g('ProChar'), ENEMY_MODELS: g('ENEMY_MODELS'), U: g('U'), Big: g('Big') };
}

const grid = (n, a, b) => { const r = []; for (let i = 0; i <= n; i++) r.push(a + (b - a) * i / n); return r; };
const KINDS = ['slime', 'golem', 'goblin', 'bat', 'mushroom', 'wolf', 'imp'];

// enemyAttack 의 돌진 클로저 — addAnim 을 가로채 fn(k) 를 격자에서 돈다. 관절 리그(armRJ)와 폴백(armR) 둘 다.
function captureLunge(THREE, Scene3D) {
    const out = { armRJ: [], fallback: [] };
    for (const mode of ['armRJ', 'fallback']) {
        const f = Object.create(Scene3D);
        const g = new THREE.Group(); g.position.x = 2;
        const sh = new THREE.Object3D(), elbow = new THREE.Object3D(), armR = new THREE.Object3D();
        const m = { g, anim: mode === 'armRJ' ? { armRJ: { sh, elbow } } : {}, armR: mode === 'armRJ' ? null : armR };
        f.enemyMap = new Map([[7, m]]);
        let fn = null, dur = 0;
        f.addAnim = (d, fx) => { dur = d; fn = fx; };
        Scene3D.enemyAttack.call(f, 7);
        for (const k of grid(50, 0, 1)) {
            fn(k);
            out[mode].push({ k, dx: g.position.x - 2, atkSq: m._atkSq, sh: sh.rotation.x, elbow: elbow.rotation.x, rz: g.rotation.z, armR: armR.rotation.x });
        }
        out.dur = dur;
    }
    return out;
}

// driveFlinch — 가짜 anim 으로 가산분을 읽는다(관절팔 리그 · 폴백 팔 · 늑대 · 꼬리/날개/갓).
function captureFlinch(THREE, Scene3D) {
    const mk = () => new THREE.Object3D();
    const rows = [];
    for (const v of grid(40, 0, 1)) {
        const f = Object.create(Scene3D);
        const g = mk(), shL = mk(), elL = mk(), shR = mk(), elR = mk(), hip = mk(), knee = mk(), gleg = mk(), tail = mk(), wing = mk(), cap = mk(), leg = mk(), armR = mk();
        wing.userData.s = -1;
        const m = { g, flinchT: (1 - v) * 0.26, flinchDur: 0.26, flinchAmp: 1, anim: { kind: 'wolf', barm: [{ sh: shL, elbow: elL }, { sh: shR, elbow: elR }], bleg: [{ hip, knee }], gleg: [gleg], tail, wings: [wing], cap, legs: [leg] }, armR };
        Scene3D.driveFlinch.call(f, m, 0);   // dt 0 → v 그대로
        rows.push({ v, gx: g.rotation.x, gy: g.rotation.y, sh0x: shL.rotation.x, sh0z: shL.rotation.z, sh1z: shR.rotation.z, el: elL.rotation.x, hip: hip.rotation.x, knee: knee.rotation.x, gleg: gleg.rotation.x, tail: tail.rotation.z, wing: wing.rotation.z, cap: cap.rotation.z, leg: leg.rotation.x });
    }
    // 폴백(관절 없음) 팔
    const f2 = Object.create(Scene3D);
    const g2 = mk(), armR2 = mk(), armL2 = mk();
    const m2 = { g: g2, flinchT: 0.26 * (1 - 0.5), flinchDur: 0.26, flinchAmp: 1, anim: {}, armR: armR2, armL: armL2 };
    Scene3D.driveFlinch.call(f2, m2, 0);
    return { rows, fallbackArmAtHalf: armR2.rotation.x };
}

// hitEnemy — 연출 함수를 전부 막고 m 레코드에 남는 수치(넉백·펀치·플린치·숫자 인자)만 읽는다.
function captureHit(THREE, Scene3D, Big, U) {
    const rows = [];
    const cases = [[0.02, false, '', false], [0.15, false, '', false], [0.3, true, '', false], [0.6, true, 'skill', false], [1, false, '', true], [0.5, true, '', true]];
    for (const [sev, crit, kind, kill] of cases) {
        const f = Object.create(Scene3D);
        const g = new THREE.Group(); g.position.set(1.5, 0, 0);
        const m = { g, topY: 1.1, baseScale: 1, anim: {} };
        f.enemyMap = new Map([[3, m]]);
        const rec = { anims: [], shake: 0, fov: null, num: null, shards: [], sparks: [] };
        f.addAnim = (d, fn, done) => rec.anims.push(d);
        f.flashMesh = () => {}; f.trailImpact = () => {}; f.rimFlash = () => {}; f.impactFlare = () => {}; f.impactSpikes = () => {}; f.impactRing = () => {};
        f.spawnShards = (p, n, c, o) => rec.shards.push({ n, c, o }); f.spawnSparks = (p, n, c, o) => rec.sparks.push({ n, c, o });
        f.shake = (v) => { rec.shake = Math.max(rec.shake, v); }; f.fovPunch = (a, d) => { rec.fov = [a, d]; }; f.hitHpBar = () => {};
        f.damageNumber = (p, text, cls, opt) => { rec.num = { text, cls, rise: opt.rise, scale: opt.scale, y: p.y - g.position.y - 0 }; };
        f.project = () => ({ x: 0, y: 0 });
        const maxHp = 100, dmg = sev * maxHp;
        // Combat.enemies 를 가짜로 — hitEnemy 가 sev 를 maxHp 로 계산한다
        vm.runInContext('globalThis.Combat = globalThis.Combat || {}; Combat.enemies = [{ id: 3, maxHp: 100 }];', vmCtx);
        const rnd = U.rand; U.rand = (a, b) => a;   // 지터 0 (dx · 숫자 y)
        Scene3D.hitEnemy.call(f, 3, dmg, crit, kind, kill);
        U.rand = rnd;
        rows.push({ sev, crit, kind, kill, kb: g.position.x - 1.5, punchDur: m.punchDur, punchHold: m.punchHold, punchAmp: m.punchAmp, flinchDur: m.flinchDur, flinchAmp: m.flinchAmp, knockDur: rec.anims[0], shake: rec.shake, fov: rec.fov, num: rec.num, shards: rec.shards[0], sparks: rec.sparks[0] });
    }
    return rows;
}
let vmCtx = null;

function main() {
    const o = args();
    const web = path.join(o.src, 'web');
    const sb = { console }; sb.self = sb; vm.createContext(sb); vmCtx = sb;
    vm.runInContext(fs.readFileSync(path.join(web, 'lib', 'three.min.js'), 'utf8'), sb, { filename: 'three.min.js' });
    for (const f of LOAD) vm.runInContext(fs.readFileSync(path.join(web, 'js', f), 'utf8'), sb, { filename: f });
    const g = (e) => vm.runInContext(e, sb);
    const THREE = sb.THREE, Scene3D = g('Scene3D'), ProChar = g('ProChar'), ENEMY_MODELS = g('ENEMY_MODELS'), U = g('U'), Big = g('Big');

    const gaitOf = {};
    for (const k of KINDS.concat(['_default', 'nope'])) gaitOf[k] = { normal: Scene3D.gaitOf(k, false), boss: Scene3D.gaitOf(k, true) };
    const hop = [];
    for (const bp of [0.7, 1.0, 1.35, 1.9, 2.25]) for (const u of grid(60, -0.5, 1.7)) hop.push({ u, bobPow: bp, hop: Scene3D.hopCurve(u, bp), sq: Scene3D.gaitSquash(u, bp, 0.1) });
    const back = grid(20, 0, 1).map(t => ({ t, v: ProChar.easeBack(t) }));
    const kind = [];
    for (let id = 1; id <= 14; id++) for (const ch of [1, 2, 3, 7]) kind.push({ id, chapter: ch, kind: KINDS[(id + ch * 2) % KINDS.length] });
    // 종별 파편색 씨앗(부피 가중 평균) · topY 계산
    const shard = {};
    for (const k of KINDS) {
        const model = ENEMY_MODELS[k];
        const acc = new THREE.Color(0, 0, 0), tmp = new THREE.Color();
        let vol = 0, maxY = 0;
        for (const p of model.parts) {
            if (!p || p.off) continue;
            const v = p.box[0] * p.box[1] * p.box[2];
            acc.add(tmp.setHex(p.c).multiplyScalar(v)); vol += v;
            const t = p.at[1] + p.box[1] / 2; if (t > maxY) maxY = t;
        }
        const shardC = acc.multiplyScalar(1 / vol).getHex();
        const G0 = Scene3D.ENEMY_GAIT[k] || {};
        shard[k] = { shardC, killC: new THREE.Color(shardC).lerp(new THREE.Color(0xff7043), 0.45).getHex(), maxY, cell: model.cell || Scene3D.ENEMY_VS, topY: maxY * (model.cell || Scene3D.ENEMY_VS) + 0.14 + (model.fly ? (G0.hover || 0) : 0), fly: !!model.fly, hop: !!model.hop, jelly: !!model.jelly };
    }
    const doc = {
        meta: { note: '정본 scene3d.js 적 보행·피격 규칙을 실제로 실행한 벡터(T8). three ' + THREE.REVISION, simpleBg: Scene3D.SIMPLE_BG },
        consts: { ENEMY_VS: Scene3D.ENEMY_VS, BOSS_SCALE: Scene3D.BOSS_SCALE, BOSS_IMPACT: Scene3D.BOSS_IMPACT, BOSS_SPAWN_X: Scene3D.BOSS_SPAWN_X, REVIVE_DUR: Scene3D.REVIVE_DUR, KIND_COLOR: Scene3D.KIND_COLOR, ENEMY_GAIT: Scene3D.ENEMY_GAIT, ENEMY_RIM: Scene3D.ENEMY_RIM },
        gaitOf, hop, back, kind, shard,
        lunge: captureLunge(THREE, Scene3D),
        flinch: captureFlinch(THREE, Scene3D),
        hit: captureHit(THREE, Scene3D, Big, U),
    };
    const text = JSON.stringify(doc, null, 1) + '\n';
    if (o.check) {
        const cur = fs.existsSync(o.out) ? fs.readFileSync(o.out, 'utf8') : '';
        if (cur !== text) { console.error('✗ battlefx_vectors: ' + o.out + ' 가 정본과 다르다 — 다시 뽑는다'); process.exit(1); }
        console.log('✓ battlefx_vectors: 벡터가 정본과 같다'); return;
    }
    fs.mkdirSync(path.dirname(o.out), { recursive: true });
    fs.writeFileSync(o.out, text);
    console.log('✓ battlefx_vectors → ' + path.relative(ROOT, o.out) + ' (' + (text.length / 1024).toFixed(0) + ' KB · gaitOf ' + Object.keys(gaitOf).length + ' · hop ' + hop.length + ' · lunge ' + doc.lunge.armRJ.length + ' · flinch ' + doc.flinch.rows.length + ' · hit ' + doc.hit.length + ')');
}
main();
