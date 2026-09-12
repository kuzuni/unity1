#!/usr/bin/env node
// ============================================================================
// hero_vectors.js — 정본 영웅 리그(`web/js/prochar.js` ProChar · `scene3d.js` 의 applyWeaponGrip / heroAttack 스윙 시계)를
// **실물 three r128** 위에서 실제로 돌려 C# 이식(T6 `Forge.Core.Hero`)이 대조할 벡터를 뽑는다.
// ----------------------------------------------------------------------------
// «비슷하게 새로 만들기» 가 아니라 «그대로 옮기기» 다 — 클립 키프레임 표는 정본 코드 상수라 JSON(T2)에 없다.
// 그래서 C# 에 옮겨 적은 표·이징·포즈 합성·파지 계산을 여기서 뽑은 정본 실행 결과와 맞춘다(T4·T16 과 같은 방식).
//
// 출력(`Assets/Tests/EditMode/Vectors/t6-hero.json`):
//   consts : MC_CARRY_X · MC_GRIP_PULL · RANGED_SHAPES · SWING_SPD · CONTACT · BODY_SCALE · GROUND_Y
//   rig    : createKnight 를 실제로 돌린 박스 리그 — mc 치수 · 본 계층(부모·base 포즈) · root/outer · 박스·디캘 목록
//   clips  : ProChar.CLIPS 원문(dur · loop · once · groundPose · tracks · capeFlat) — C# 표와 1:1 대조
//   alias  : CLIP_MAP(heroAttack) 후보 → resolveClip 결과
//   ease   : 이징 6종 샘플
//   grip   : WEAPON_TYPES 전 종 + 기본(club)에 applyWeaponGrip 을 돌린 결과(부착 본 · 위치 · 오일러 · 쿼터니언 · restPose · restX · elbowFix · bladeRoll)
//   swing  : 동작 8종의 스윙 시계(atkT · G0/G1/G2 · k 마다 blendGrip 쿼터니언)
//   update : 장면 여러 개(클립 · restX · restPose · ridePose · 피격) 를 dt 로 밟으며 본 전부의 rotation/position/scale
//
// 사용:  node tools/hero_vectors.js [--src .wwwww-src] [--out Assets/Tests/EditMode/Vectors/t6-hero.json] [--check]
//   --check : 다시 뽑아 기존 파일과 같은지만 본다(정본이 바뀌면 rc 1).
// ============================================================================
'use strict';
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const ROOT = path.resolve(__dirname, '..');
let SRC = path.join(ROOT, '.wwwww-src');
let OUT = path.join(ROOT, 'Assets', 'Tests', 'EditMode', 'Vectors', 't6-hero.json');
let CHECK = false;
for (let i = 2; i < process.argv.length; i++) {
    if (process.argv[i] === '--src') SRC = path.resolve(process.argv[++i]);
    else if (process.argv[i] === '--out') OUT = path.resolve(process.argv[++i]);
    else if (process.argv[i] === '--check') CHECK = true;
}
const WEB = path.join(SRC, 'web');

// ── 브라우저 흉내 — 캔버스 텍스처는 «그림» 이라 리그 좌표에 영향이 없다. 2D 컨텍스트는 무엇을 불러도 받아 주는 프록시.
function fakeCtx() {
    const h = { get: (t, k) => (k === 'canvas' ? null : (typeof k === 'string' ? (() => new Proxy({}, h)) : undefined)), set: () => true };
    return new Proxy({}, h);
}
function fakeCanvas() { return { width: 0, height: 0, getContext: () => fakeCtx(), style: {} }; }
const document = { createElement: (tag) => (tag === 'canvas' ? fakeCanvas() : { style: {}, appendChild() { } }) };

const ctx = vm.createContext({
    console, Math, Number, String, Object, Array, JSON, RegExp, Float32Array, Uint32Array, Uint16Array, Int32Array, Uint8Array, Float64Array,
    ArrayBuffer, DataView, Map, Set, Promise, setTimeout, clearTimeout, document, navigator: { userAgent: 'node' },
});
ctx.globalThis = ctx;
ctx.self = ctx;
ctx.window = ctx;
function load(rel) {
    const file = path.join(WEB, rel);
    vm.runInContext(fs.readFileSync(file, 'utf8'), ctx, { filename: rel });
}
load('lib/three.min.js');
['js/voxel.js', 'js/gamedata.js', 'js/prochar.js'].forEach(load);
// 정본 시대 파일은 top-level `const` 라 globalThis 에 안 실린다 — 같은 컨텍스트의 스크립트 스코프에서 꺼낸다.
const G = vm.runInContext('({ THREE, ProChar, WEAPON_TYPES, weaponShape })', ctx);
const THREE = G.THREE, ProChar = G.ProChar, WEAPON_TYPES = G.WEAPON_TYPES, weaponShape = G.weaponShape;
if (!THREE || !ProChar || !WEAPON_TYPES) throw new Error('THREE/ProChar/WEAPON_TYPES 로드 실패');
// 텍스처는 그림이라 리그 좌표와 무관 — 실물 캔버스가 없으니 빈 텍스처로 막는다(three 는 렌더러 없이는 image 를 읽지 않는다).
ProChar.canvasTex = function (key) { return (this._texCache[key] = this._texCache[key] || new THREE.Texture()); };
ProChar.envMap = function () { return (this._envMap = this._envMap || new THREE.CubeTexture([])); };

// ── scene3d.js 에서 파지·스윙 시계 부분만 잘라 온다(파일 전체는 DOM 위에서만 산다) ──────────────────────
const scene3dSrc = fs.readFileSync(path.join(WEB, 'js', 'scene3d.js'), 'utf8');
function slice(from, to, label) {
    const a = scene3dSrc.indexOf(from);
    if (a < 0) throw new Error('scene3d.js 에서 못 찾음: ' + label + ' (' + from.slice(0, 40) + ')');
    const b = scene3dSrc.indexOf(to, a);
    if (b < 0) throw new Error('scene3d.js 끝 표식 못 찾음: ' + label);
    return scene3dSrc.slice(a, b);
}
const gripTable = slice('    WEAPON_GRIP: {', '    // C자 랩 주먹', 'WEAPON_GRIP');
const gripOfSrc = slice('    gripOf(wtypeId) {', '    // 방패를 등에 메기', 'gripOf');
const applySrc = slice('    applyWeaponGrip() {', '    refreshHeroEquip(', 'applyWeaponGrip');
const constSrc = slice('    MC_CARRY_X:', '    RIDE_STAND_BULK', 'MC_CARRY_X');
const ridePoseSrc = slice('    RIDE_STAND_POSE: {', '    MOUNT_FORMS: {', 'RIDE_STAND_POSE');
const swingSrc = slice('            const CLIP_MAP = {', '            const endAtk = () => {', 'swing clock');
const hostSrc = 'const Host = {\n' + gripTable + gripOfSrc + applySrc + constSrc + ridePoseSrc
    + '    freeHandReach() { return null; },\n    slingShield() {},\n    makeGripWrap() { return new THREE.Group(); },\n};\nHost';
const Host = vm.runInContext(hostSrc, ctx, { filename: 'scene3d-host.js' });
if (!Host.WEAPON_GRIP || !Host.applyWeaponGrip || typeof Host.MC_CARRY_X !== 'number') throw new Error('scene3d 파지 절 추출 실패');
// heroAttack 의 스윙 시계 — this.* 를 host.* 로 바꿔 순수 함수로 감싼다. 클립 재생·트레일은 값에 영향이 없는 부수효과라 무시.
const swingBody = swingSrc.replace(/this\./g, 'host.');
const swingFn = vm.runInContext('(function (motion, host, ProChar, THREE) {\n' + swingBody
    + '\n    return { atkT, CONTACT, G0, G1, G2, GW, SWING_SPD, blendGrip, clipName }; })', ctx, { filename: 'scene3d-swing.js' });

const r6 = v => Math.round(v * 1e6) / 1e6;
const r6a = a => a.map(r6);
const eul = o => [r6(o.rotation.x), r6(o.rotation.y), r6(o.rotation.z)];
const pos = o => [r6(o.position.x), r6(o.position.y), r6(o.position.z)];
const scl = o => [r6(o.scale.x), r6(o.scale.y), r6(o.scale.z)];
const quat = o => [r6(o.quaternion.x), r6(o.quaternion.y), r6(o.quaternion.z), r6(o.quaternion.w)];

// ── rig ─────────────────────────────────────────────────────────────────────
const R = ProChar.createKnight();
if (!R.mc || !R._simple) throw new Error('createKnight 가 박스 리그(mc)를 만들지 않았다');
const boneName = new Map();
for (const k in R.bones) boneName.set(R.bones[k], k);
boneName.set(R.root, 'root');
function parentBone(o) {
    for (let p = o.parent; p; p = p.parent) if (boneName.has(p)) return boneName.get(p);
    return null;
}
const bones = {};
for (const k in R.bones) {
    const b = R.bones[k];
    bones[k] = { parent: parentBone(b), base: R.base[k], pos: pos(b), rot: eul(b), scale: scl(b) };
}
const boxes = [], decals = [];
R.group.traverse(o => {
    if (!o.isMesh || !(o.userData && o.userData.simpleBox)) return;
    const g = o.geometry, p = g.parameters || {};
    const rec = { parent: parentBone(o), pos: pos(o), color: o.material.color.getHex() };
    if (g.type === 'BoxGeometry') boxes.push(Object.assign(rec, { size: r6a([p.width, p.height, p.depth]), basic: !!o.material.isMeshBasicMaterial, rough: o.material.roughness, flat: !!o.material.flatShading }));
    else if (g.type === 'PlaneGeometry') decals.push(Object.assign(rec, { size: r6a([p.width, p.height]), basic: !!o.material.isMeshBasicMaterial, toneMapped: o.material.toneMapped }));
    else throw new Error('모르는 박스 지오메트리: ' + g.type);
});
const rig = {
    mc: R.mc, bodyScale: ProChar.BODY_SCALE, groundY: ProChar.GROUND_Y,
    outerScale: scl(R.group), root: { base: R.base.root, pos: pos(R.root) },
    bones, boxes, decals, simple: R._simple,
    handL: parentBone(R.handL), handR: parentBone(R.handR),
};

// ── clips · alias · ease ───────────────────────────────────────────────────
const clips = {};
for (const name in ProChar.CLIPS) {
    const c = ProChar.CLIPS[name];
    clips[name] = { dur: c.dur, loop: !!c.loop, once: !!c.once, groundPose: !!c.groundPose, tracks: c.tracks, capeFlat: c.capeFlat || null };
}
const CLIP_MAP = {
    slash: ['1H_Melee_Attack_Slice_Diagonal', '1H_Melee_Attack_Slice_Horizontal'],
    chop: ['1H_Melee_Attack_Chop'], thrust: ['1H_Melee_Attack_Stab'],
    slam: ['2H_Melee_Attack_Chop', '2H_Melee_Attack_Slice'], double: ['Dualwield_Melee_Attack_Slice', 'Dualwield_Melee_Attack_Chop'],
    bow: ['2H_Ranged_Shoot', '2H_Ranged_Shooting'], gun: ['1H_Ranged_Shoot', '1H_Ranged_Shooting'],
    cast: ['Spellcast_Shoot', 'Spellcast_Raise', 'Spellcasting', '2H_Ranged_Shoot'], throw: ['Throw', 'Spellcast_Shoot', '1H_Melee_Attack_Chop'],
    death: ['Death_A'], revive: ['Revive'], walk: ['Walking'], run: ['Running'], idle: ['Idle'], none: ['Nope', 'Zzz'],
};
const alias = {};
for (const k in CLIP_MAP) alias[k] = { cands: CLIP_MAP[k], clip: ProChar.resolveClip(CLIP_MAP[k]) };
const ease = {};
for (const fn of ['ease', 'easeOut', 'easeIn', 'easeBack', 'easeBounce', 'easeSnap']) {
    ease[fn] = [];
    for (let i = 0; i <= 40; i++) { const t = i / 40; ease[fn].push([r6(t), r6(ProChar[fn](t))]); }
}
const sample = [];
for (const name of ['slash', 'Walking', 'Idle', 'Death']) {
    const tr = ProChar.CLIPS[name].tracks;
    for (const key in tr) for (let i = 0; i <= 10; i++) { const t = i / 10; sample.push([name, key, r6(t), r6(ProChar.sample(tr[key], t))]); }
}

// ── grip ────────────────────────────────────────────────────────────────────
function gripFor(wtypeId) {
    const host = Object.create(Host);
    host.heroRig = R; host.weaponG = new THREE.Group(); host.ridePose = null;
    host.wtypeId = wtypeId;
    // refreshHeroEquip 의 순서 그대로: restX = WEAPON_TYPES.restX + 0.25 → applyWeaponGrip
    const wtDef = WEAPON_TYPES[wtypeId];
    host.armRest = wtDef ? wtDef.restX : -0.25;
    R.restX = host.armRest + 0.25;
    R.restPose = null; R.ridePose = null;
    if (R.shield) R.shield.visible = true;
    host.applyWeaponGrip();
    return {
        shape: weaponShape(wtypeId), parent: boneName.get(host.weaponG.parent) || null,
        pos: pos(host.weaponG), rot: eul(host.weaponG), quat: quat(host.weaponG), scale: scl(host.weaponG),
        restX: r6(R.restX), restPose: R.restPose, elbowFix: r6(host._gripElbowFix || 0), bladeRoll: r6(host._bladeRoll || 0),
        gripRot: r6a(host._gripRot), gripPos: r6a(host._gripPos), shieldVisible: R.shield ? R.shield.visible : null,
        melee: !Host.RANGED_SHAPES[weaponShape(wtypeId)],
    };
}
const grip = {};
for (const id of Object.keys(WEAPON_TYPES)) grip[id] = gripFor(id);
grip.club = grip.club || gripFor('club');
grip.__unknown__ = gripFor('noSuchWeapon');   // gripOf 폴백 = sword

// ── swing (heroAttack 의 스윙 시계) ──────────────────────────────────────────
const swing = {};
for (const motion of ['slash', 'chop', 'thrust', 'slam', 'double', 'bow', 'gun', 'cast', 'throw']) {
    // 검(slash) 파지에서 출발한 상태 — _gripRot/_bladeRoll 은 그 무기의 applyWeaponGrip 결과
    const wt = { slash: 'sword', chop: 'axe', thrust: 'spear', slam: 'hammer', double: 'boneDagger', bow: 'bow', gun: 'gun', cast: 'staff', throw: 'sling' }[motion];
    const g = grip[wt] || grip.sword;
    const host = { heroPlay() { }, weaponG: new THREE.Group(), _gripRot: g.gripRot, _bladeRoll: g.bladeRoll };
    const s = swingFn(motion, host, ProChar, THREE);
    const w = [];
    for (let i = 0; i <= 50; i++) { const k = i / 50; s.blendGrip(k); w.push([r6(k), quat(host.weaponG)]); }
    swing[motion] = { wtype: wt, clip: s.clipName, atkT: r6(s.atkT), contact: s.CONTACT, G0: r6(s.G0), G1: r6(s.G1), G2: r6(s.G2), GW: r6(s.GW), spd: s.SWING_SPD, q: w };
}

// ── update 장면 ─────────────────────────────────────────────────────────────
// 위치·배율은 base 와 다를 때만 싣는다(대부분의 본은 회전만 움직인다 — 파일 크기).
function snapshot() {
    const out = {};
    const one = (o, base) => {
        const rec = { r: eul(o) };
        const p = pos(o), s = scl(o);
        if (p[0] !== r6(base.px) || p[1] !== r6(base.py) || p[2] !== r6(base.pz)) rec.p = p;
        if (s[0] !== r6(base.sx) || s[1] !== r6(base.sy) || s[2] !== r6(base.sz)) rec.s = s;
        return rec;
    };
    for (const k in R.bones) out[k] = one(R.bones[k], R.base[k]);
    out.root = one(R.root, R.base.root);
    return out;
}
function scene(name, setup, steps) {
    R.restX = 0; R.restPose = null; R.ridePose = null; R.hitT = 0; R.hitDur = 0; R.hitAmp = 0;
    R._clip = null; R._t = 0; R._once = false; R._speed = 1; R._onDone = null; R.state = '';
    const rec = { name, frames: [], state: [], done: 0 };
    setup(rec);
    for (const dt of steps) {
        ProChar.update(R, dt);
        rec.frames.push({ dt, t: R._t, state: R.state, pose: snapshot() });   // dt·t 는 반올림 없이(누적 오차가 포즈 대조를 흔든다)
    }
    return rec;
}
const fixed = n => Array.from({ length: n }, () => 1 / 60);
const scenes = [];
scenes.push(scene('idle', () => ProChar.play(R, ['Idle']), fixed(24)));
scenes.push(scene('walk', () => ProChar.play(R, ['Walking']), [0.011, 0.019, 0.03, 0.02, 0.016, 0.1, 0.25, 0.4, 0.05]));
scenes.push(scene('walk-restX', () => { R.restX = 0.35; ProChar.play(R, ['Walking', 'Idle']); }, fixed(12)));
scenes.push(scene('slash-sword', (rec) => {
    R.restX = grip.sword.restX; R.restPose = grip.sword.restPose;
    ProChar.play(R, CLIP_MAP.slash, true, 1.25, () => { rec.done++; });
}, fixed(30)));
scenes.push(scene('chop-axe-then-idle', (rec) => {
    R.restX = grip.axe.restX; R.restPose = grip.axe.restPose;
    ProChar.play(R, CLIP_MAP.chop, true, 1.25, () => { rec.done++; ProChar.play(R, ['Idle']); });
}, fixed(32)));
scenes.push(scene('thrust-spear', () => { R.restX = grip.spear.restX; R.restPose = grip.spear.restPose; ProChar.play(R, CLIP_MAP.thrust, true, 1.25); }, fixed(26)));
scenes.push(scene('slam-hammer', () => { R.restX = grip.hammer.restX; R.restPose = grip.hammer.restPose; ProChar.play(R, CLIP_MAP.slam, true, 1.25); }, fixed(32)));
scenes.push(scene('double-dagger', () => { R.restX = grip.boneDagger.restX; R.restPose = grip.boneDagger.restPose; ProChar.play(R, CLIP_MAP.double, true, 1.25); }, fixed(30)));
scenes.push(scene('bow', () => { R.restX = grip.bow.restX; R.restPose = grip.bow.restPose; ProChar.play(R, CLIP_MAP.bow, true, 1.25); }, fixed(34)));
scenes.push(scene('gun', () => { R.restX = grip.gun.restX; R.restPose = grip.gun.restPose; ProChar.play(R, CLIP_MAP.gun, true, 1.25); }, fixed(22)));
scenes.push(scene('cast-throw', (rec) => { R.restX = grip.staff.restX; R.restPose = grip.staff.restPose; ProChar.play(R, CLIP_MAP.cast, true, 1.25, () => { rec.done++; ProChar.play(R, CLIP_MAP.throw, true, 1.25, () => { rec.done++; }); }); }, fixed(64)));
scenes.push(scene('death-revive', (rec) => { ProChar.play(R, ['Death_A'], true, 1, () => { rec.done++; ProChar.play(R, ['Revive'], true, 1, () => { rec.done++; }); }); }, fixed(150)));
scenes.push(scene('ride-idle', () => { R.restX = grip.sword.restX; R.restPose = Object.assign({}, grip.sword.restPose, Host.RIDE_STAND_POSE); R.ridePose = Host.RIDE_STAND_POSE; ProChar.play(R, ['Idle']); }, fixed(8)));
scenes.push(scene('ride-slash', () => { R.restX = grip.sword.restX; R.restPose = Object.assign({}, grip.sword.restPose, Host.RIDE_STAND_POSE); R.ridePose = Host.RIDE_STAND_POSE; ProChar.play(R, CLIP_MAP.slash, true, 1.25); }, fixed(28)));
scenes.push(scene('hit-idle', () => { ProChar.play(R, ['Idle']); ProChar.hit(R, 0.2); }, fixed(24)));
scenes.push(scene('hit-during-slash', () => { R.restX = grip.sword.restX; R.restPose = grip.sword.restPose; ProChar.play(R, CLIP_MAP.slash, true, 1.25); ProChar.hit(R, 0.6); }, fixed(20)));
scenes.push(scene('hit-during-death', () => { ProChar.play(R, ['Death_A'], true); ProChar.hit(R, 0.9); }, fixed(10)));
scenes.push(scene('loop-restart-ignored', () => { ProChar.play(R, ['Idle']); ProChar.update(R, 0.5); ProChar.play(R, ['Idle']); }, fixed(4)));
scenes.push(scene('unknown-clip', () => { ProChar.play(R, ['Idle']); ProChar.play(R, ['Nope']); }, fixed(3)));
scenes.push(scene('big-dt-wrap', () => ProChar.play(R, ['Walking']), [1.0, 2.31, 0.66, 5.5]));

const doc = {
    consts: {
        MC_CARRY_X: Host.MC_CARRY_X, MC_GRIP_PULL: Host.MC_GRIP_PULL, RANGED_SHAPES: Host.RANGED_SHAPES, RIDE_STAND_POSE: Host.RIDE_STAND_POSE,
        RIDE_STAND_BULK: Host.RIDE_STAND_BULK, BODY_SCALE: ProChar.BODY_SCALE, GROUND_Y: ProChar.GROUND_Y, EASES: ProChar.EASES,
        CLAMP: { rxMin: -2.95, rxMax: 2.35, rzMin: -0.5, rzMax: 0.6 }, HIT: { dur: 0.30, rise: 0.06 },
        WEAPON_GRIP: Host.WEAPON_GRIP, CLIP_ALIAS: ProChar.CLIP_ALIAS.map(([re, n]) => [re.source, n]),
        WEAPON_TYPES: Object.fromEntries(Object.entries(WEAPON_TYPES).map(([k, v]) => [k, { motion: v.motion, restX: v.restX, shape: v.shape, kind: v.kind }])),
    },
    rig, clips, alias, ease, sample, grip, swing, update: scenes,
};
const text = JSON.stringify(doc);
if (CHECK) {
    const old = fs.existsSync(OUT) ? fs.readFileSync(OUT, 'utf8') : '';
    if (old === text) { console.log('✓ hero_vectors: 정본과 같다 (' + OUT + ')'); process.exit(0); }
    console.error('✗ hero_vectors: 정본이 바뀌었다 — node tools/hero_vectors.js 로 다시 뽑고 C# 표를 맞춘다');
    process.exit(1);
}
fs.mkdirSync(path.dirname(OUT), { recursive: true });
fs.writeFileSync(OUT, text);
console.log('hero_vectors → ' + OUT + ' (' + (text.length / 1024).toFixed(0) + 'KB · clips ' + Object.keys(clips).length + ' · grip ' + Object.keys(grip).length
    + ' · swing ' + Object.keys(swing).length + ' · scenes ' + scenes.length + ' · boxes ' + boxes.length + ' · decals ' + decals.length + ')');
