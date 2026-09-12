#!/usr/bin/env node
// ============================================================================
// gallery_vectors.js — 정본 `Mobs.build` 를 실물 three r128 위에서 돌려 **종마다 경계 상자**를 뽑는다(T5 몹 도감 대조용).
// ----------------------------------------------------------------------------
// 원작 시트(`web/tools/mobsheet.html renderMob`)와 같은 규약으로 세운다: 탈것은 `cell = 0.44 / model.seat`(안장 계약),
// 나머지는 표의 `cell`(없으면 0.03) · vivid 0. `Box3.setFromObject(group)` = 파츠마다 «로컬 AABB 를 월드 행렬로 옮긴 상자»의 합집합 —
// 유니티 `Renderer.bounds` 와 같은 정의라 PlayMode `MobGalleryTests` 가 z 부호만 뒤집어(결정 4) 그대로 대조한다.
//
// 출력(`Assets/Tests/PlayMode/Vectors/t5-bounds.json`):
//   { pets|mounts|enemies|skillfx: { 종: { cell, parts(off 제외 파츠 수), min:[x,y,z], max:[x,y,z] } } }   — three 좌표
//
// 사용:  node tools/gallery_vectors.js [--src .wwwww-src] [--out Assets/Tests/PlayMode/Vectors/t5-bounds.json] [--check]
//   --check : 다시 뽑아 기존 파일과 같은지 본다(rc 1 이면 정본이 바뀌었다 → 다시 뽑아 커밋).
// ============================================================================
'use strict';
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const ROOT = path.resolve(__dirname, '..');
let SRC = path.join(ROOT, '.wwwww-src');
let OUT = path.join(ROOT, 'Assets', 'Tests', 'PlayMode', 'Vectors', 't5-bounds.json');
let CHECK = false;
for (let i = 2; i < process.argv.length; i++) {
    if (process.argv[i] === '--src') SRC = path.resolve(process.argv[++i]);
    else if (process.argv[i] === '--out') OUT = path.resolve(process.argv[++i]);
    else if (process.argv[i] === '--check') CHECK = true;
}
const WEB = path.join(SRC, 'web');

const ctx = vm.createContext({ console, Math, Number, String, Object, Array, JSON, Float32Array, Uint32Array, Uint16Array, Int32Array, Uint8Array, Float64Array, ArrayBuffer, DataView, Map, Set, Promise, setTimeout, clearTimeout });
ctx.globalThis = ctx;
ctx.self = ctx;
function load(rel) {
    vm.runInContext(fs.readFileSync(path.join(WEB, rel), 'utf8'), ctx, { filename: rel });
}
load('lib/three.min.js');
['js/voxel.js', 'js/mobs.js', 'js/mobdata.js', 'js/mobs-pets.js', 'js/mobs-mounts.js', 'js/mobs-enemies.js', 'js/mobs-skillfx.js'].forEach(load);
const THREE = ctx.THREE, Mobs = ctx.Mobs;
if (!THREE || !Mobs) throw new Error('THREE/Mobs 로드 실패');
const TABLES = { pets: ctx.PET_MODELS, mounts: ctx.MOUNT_MODELS, enemies: ctx.ENEMY_MODELS, skillfx: ctx.SKILLFX_MODELS };
for (const k in TABLES) if (!TABLES[k]) throw new Error('표가 없다: ' + k);

const r6 = v => Math.round(v * 1e6) / 1e6;
const out = {};
for (const kind in TABLES) {
    out[kind] = {};
    const table = TABLES[kind];
    for (const name of Object.keys(table)) {
        const model = table[name];
        // 원작 시트(mobsheet.html)와 같은 칸 크기 규약
        const opts = model.seat ? { cell: 0.44 / model.seat } : {};
        const built = Mobs.build(model, opts);
        const g = built.group;
        g.updateMatrixWorld(true);
        const box = new THREE.Box3().setFromObject(g);
        let parts = 0;
        g.traverse(o => { if (o.isMesh) parts++; });
        out[kind][name] = {
            cell: r6(opts.cell || model.cell || 0.03),
            parts,
            min: [r6(box.min.x), r6(box.min.y), r6(box.min.z)],
            max: [r6(box.max.x), r6(box.max.y), r6(box.max.z)],
        };
    }
}
const text = JSON.stringify(out, null, 1) + '\n';
if (CHECK) {
    const old = fs.existsSync(OUT) ? fs.readFileSync(OUT, 'utf8') : '';
    if (old !== text) { console.error('⛔ gallery_vectors --check: ' + path.relative(ROOT, OUT) + ' 가 정본과 다르다 — `node tools/gallery_vectors.js` 로 다시 뽑아 커밋'); process.exit(1); }
    console.log('✓ gallery_vectors --check: ' + path.relative(ROOT, OUT) + ' = 정본');
    process.exit(0);
}
fs.mkdirSync(path.dirname(OUT), { recursive: true });
fs.writeFileSync(OUT, text);
const counts = Object.keys(out).map(k => k + ' ' + Object.keys(out[k]).length).join(' · ');
console.log('→ ' + path.relative(ROOT, OUT) + ' · ' + counts);
