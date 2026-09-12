#!/usr/bin/env node
'use strict';
// T10 — 정본 scene3d.js 의 펫 출전 규칙을 **실물 three r128 위에서 실제로 실행**해 벡터를 뽑는다.
//   node tools/pet_scene_vectors.js [--src <wwwww>] [--out Assets/Tests/EditMode/Vectors/t10-pets.json]
// ⓐ `Scene3D.formationSpot(i, arc, row0)` 를 그대로 부른다 — 펫(비탑승·탑승 = refreshPets 가 만드는 arc/row0) · 탈것 호(MOUNT_ARC · row0 null) · zBase 없는 호(같은 함수 · 합성 입력).
// ⓑ 몸통 자세·관절 각은 `update` 의 펫 블록(18598~18645)이 씬 전체(카메라·영웅 리그)에 묶여 있어 통째로 못 돌린다 — 그 블록의 식을 **글자 그대로** 옮겨
//    실물 `PET_MOTION`·`CREATURE_YAW`·`RARITIES` 와 실물 `Mobs.build(PET_MODELS[name], {vivid:0.2}).joints`(T4 가 대조한 관절 서술) 위에서 계산한다.
// C# `PetFormation`·`PetSceneRules`·`PetPose`(Core/Pets) 가 같은 입력 → 같은 값을 내는지 EditMode `PetSceneRulesTests` 가 잰다.
const fs = require('fs');
const path = require('path');
const vm = require('vm');
const ROOT = path.resolve(__dirname, '..');
const ex = require('./export_data.js');

function args() {
    const a = process.argv.slice(2);
    const o = { src: path.join(ROOT, '.wwwww-src'), out: path.join(ROOT, 'Assets', 'Tests', 'EditMode', 'Vectors', 't10-pets.json') };
    for (let i = 0; i < a.length; i++) {
        if (a[i] === '--src') o.src = a[++i];
        else if (a[i] === '--out') o.out = a[++i];
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
    return { THREE: sb.THREE, Scene3D: g('Scene3D'), PET_MOTION: g('PET_MOTION'), RARITIES: g('RARITIES'), PET_MODELS: g('PET_MODELS'), Mobs: g('Mobs') };
}

// 시험 입력(양쪽이 같은 값을 받는다) — HERO_X 는 combat.js 상수(T7 `BattleRules.HeroX` 가 대조 완료)
const HERO_X = -1.35, WORLD_X = 7.5, TIMES = [0, 0.37, 1.9, 4.25], PHASE = 1.234, SPEED = 1.1;

function main() {
    const o = args();
    const S = load(o.src);
    const { Scene3D } = S;
    // ⓐ 대열 — refreshPets 가 만드는 그대로
    const mountedArc = { ...Scene3D.PET_ARC, rx0: Scene3D.PET_ARC.rx0 + Scene3D.PET_ARC.mountedRx, rz0: Scene3D.PET_ARC.rz0 + Scene3D.PET_ARC.mountedRz };
    const noZBase = { ...Scene3D.MOUNT_ARC }; delete noZBase.zBase;
    const spots = (n, arc, row0) => Array.from({ length: n }, (_, i) => Scene3D.formationSpot(i, arc, row0));
    const formation = {
        petUnmounted: spots(12, Scene3D.PET_ARC, Scene3D.PET_ROW0.unmounted),
        petMounted: spots(12, mountedArc, Scene3D.PET_ROW0.mounted),
        mount: spots(16, Scene3D.MOUNT_ARC, null),
        mountNoZBase: spots(8, noZBase, null),
    };
    // 등급 스케일(refreshPets 9781)
    const scale = {};
    for (const r of [...S.RARITIES, 'bogus']) scale[r] = (0.85 + S.RARITIES.indexOf(r) * 0.14) * 1.5;
    // ⓑ 자세 — update 펫 블록 18598~18645 의 식 그대로(pg = 그룹 · ud = userData)
    const pets = [];
    const names = Object.keys(S.PET_MODELS);
    names.forEach((name, i) => {
        const built = S.Mobs.build(S.PET_MODELS[name], { vivid: 0.2 });
        const spot = Scene3D.formationSpot(i % 5, Scene3D.PET_ARC, Scene3D.PET_ROW0.unmounted);
        const ud = { name, spotX: spot[0], home: { x: 0, y: 0.4 }, phase: PHASE, speed: SPEED };
        const mo = S.PET_MOTION[ud.name] || { freq: 4, amp: 0.08 };
        const frames = [];
        for (const clock of TIMES) for (const walking of [false, true]) {
            const pg = { position: { x: 0, y: 0, z: spot[1] }, rotation: { x: 0, y: Scene3D.CREATURE_YAW, z: 0 } };
            const t = clock * (ud.speed || 1) + (ud.phase || 0);
            ud.home.x = HERO_X + (ud.spotX || -0.3) + WORLD_X;
            let xJitter = 0;
            if (ud.name === 'Scorpion' || ud.name === 'Spider') xJitter = Math.sin(t * 16) * 0.03;
            if (ud.name === 'Snail') xJitter = Math.sin(t * 0.9) * 0.06;
            pg.position.x = ud.home.x + xJitter;
            const walkBoost = walking ? 1.7 : 1;
            pg.position.y = ud.home.y + (mo.hop
                ? Math.abs(Math.sin(t * mo.freq)) * mo.amp * walkBoost
                : Math.sin(t * mo.freq) * mo.amp * walkBoost);
            if (mo.sway) pg.rotation.z = Math.sin(t * mo.freq * 0.8) * mo.sway;
            if (mo.yaw) pg.rotation.y = Scene3D.CREATURE_YAW + Math.sin(t * mo.freq) * mo.yaw;
            if (mo.pitch) pg.rotation.x = Math.sin(t * mo.freq) * mo.pitch;
            const joints = built.joints.map(j => {
                const a = t * mo.freq * j.f + j.ph;
                if (j.spin) return j.base + a;
                const w = 1 + (j.gain - 1) * (walking ? 1 : 0);
                return j.base + (j.abs ? Math.abs(Math.sin(a)) : Math.sin(a)) * j.amp * w;
            });
            frames.push({ clock, walking, t, pos: [pg.position.x, pg.position.y, pg.position.z], rot: [pg.rotation.x, pg.rotation.y, pg.rotation.z], joints });
        }
        pets.push({
            name, spot, motion: S.PET_MOTION[name] || null,
            joints: built.joints.map(j => ({ pid: j.o.userData.pid, axis: j.axis, base: j.base, amp: j.amp, ph: j.ph, f: j.f, gain: j.gain, abs: j.abs, spin: j.spin })),
            frames,
        });
    });
    const doc = {
        meta: { note: '정본 scene3d.js formationSpot 실행 + refreshPets/update 펫 블록의 식(글자 그대로) 을 실물 PET_MOTION·Mobs.build 관절 위에서 계산한 결과 (T10). 각은 three 라디안 · 좌표는 three(z 반전 전).', three: S.THREE.REVISION, pets: pets.length },
        consts: { CREATURE_YAW: Scene3D.CREATURE_YAW, PET_ROW0: Scene3D.PET_ROW0, PET_ARC: Scene3D.PET_ARC, MOUNT_ARC: Scene3D.MOUNT_ARC, RARITIES: S.RARITIES, heroX: HERO_X, worldX: WORLD_X, phase: PHASE, speed: SPEED, times: TIMES },
        formation, scale, pets,
    };
    fs.mkdirSync(path.dirname(o.out), { recursive: true });
    fs.writeFileSync(o.out, ex.serialize(doc), 'utf8');
    console.log(`✓ pet_scene_vectors: 대열 ${Object.keys(formation).length}갈래 · 펫 ${pets.length}종 × 프레임 ${TIMES.length * 2} · 관절 ${pets.reduce((s, p) => s + p.joints.length, 0)} → ${path.relative(ROOT, o.out)}`);
    return 0;
}
process.exit(main());
