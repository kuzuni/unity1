#!/usr/bin/env node
// ============================================================================
// voxel_vectors.js — 정본 `Voxel`(voxel.js) · `Mobs.build`(mobs.js) 를 **실물 three r128** 위에서 돌려
// C# 이식(T4 `VoxelGeometry`·`MobBuilder`)이 대조할 벡터를 뽑는다.
// ----------------------------------------------------------------------------
// 왜 실물 three 인가 — `Voxel.build` 의 정점 색은 `THREE.Color.setHex → .r/.g/.b`, `vivid` 는
// `Color.getHSL/setHSL` 을 쓴다. 스텁으로 흉내 내면 «내가 짠 것끼리 맞다» 는 것만 확인하게 된다.
// wwwww `web/lib/three.min.js`(r128) 를 vm 컨텍스트에 그대로 올린다(브라우저 API 는 안 쓴다).
//
// 출력(`Assets/Tests/EditMode/Vectors/t4-voxel.json`):
//   faces   : 모양 이름 → Voxel.faces 결과(n · corners · ao · c · v)          — 면 제거·AO 대조
//   jitter  : [x,y,z,amt, 값]                                                  — 좌표 해시 대조
//   aoShade : [level, strength, 값]
//   vivid   : [hex, vivid, 결과 hex]                                             — HSL 보정 대조
//   paint   : 표 4개 × 종 × 파츠 → 칸 색 체크섬(순서 = box 의 x→y→z 중첩 순서)   — paint/expand 대조
//   build   : 표 4개 × 종 → { cell, parts:[{pid, parent, pivot, pos, rot, matKey, mat, tri, sum…}], legs… }
//             `pos` = 그 노드의 로컬 위치(three 좌표 · cell 곱한 뒤) · `tri` = 정점 수(면×6) ·
//             `psum`/`csum` = 정점 위치·색 성분 합 · `pw` = Σ(x·1 + y·7 + z·13)·(i%5+1) 가중합(순서까지 잰다)
//   full    : 파츠 세 개의 정점 위치·색 배열 전부(작은 것만 · 순서·flip 규칙까지 그대로인지)
//
// 사용:  node tools/voxel_vectors.js [--src .wwwww-src] [--out Assets/Tests/EditMode/Vectors/t4-voxel.json]
// ============================================================================
'use strict';
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const ROOT = path.resolve(__dirname, '..');
let SRC = path.join(ROOT, '.wwwww-src');
let OUT = path.join(ROOT, 'Assets', 'Tests', 'EditMode', 'Vectors', 't4-voxel.json');
for (let i = 2; i < process.argv.length; i++) {
    if (process.argv[i] === '--src') SRC = path.resolve(process.argv[++i]);
    else if (process.argv[i] === '--out') OUT = path.resolve(process.argv[++i]);
}
const WEB = path.join(SRC, 'web');

const ctx = vm.createContext({ console, Math, Number, String, Object, Array, JSON, Float32Array, Uint32Array, Uint16Array, Int32Array, Uint8Array, Float64Array, ArrayBuffer, DataView, Map, Set, Promise, setTimeout, clearTimeout });
ctx.globalThis = ctx;
ctx.self = ctx;
function load(rel) {
    const file = path.join(WEB, rel);
    vm.runInContext(fs.readFileSync(file, 'utf8'), ctx, { filename: rel });
}
load('lib/three.min.js');
['js/voxel.js', 'js/mobs.js', 'js/mobdata.js', 'js/mobs-pets.js', 'js/mobs-mounts.js', 'js/mobs-enemies.js', 'js/mobs-skillfx.js'].forEach(load);
const THREE = ctx.THREE, Voxel = ctx.Voxel, Mobs = ctx.Mobs;
if (!THREE || !Voxel || !Mobs) throw new Error('THREE/Voxel/Mobs 로드 실패');
const TABLES = { pets: ctx.PET_MODELS, mounts: ctx.MOUNT_MODELS, enemies: ctx.ENEMY_MODELS, skillfx: ctx.SKILLFX_MODELS };
for (const k in TABLES) if (!TABLES[k]) throw new Error('표가 없다: ' + k);

const r6 = v => Math.round(v * 1e6) / 1e6;

// ── faces ───────────────────────────────────────────────────────────────────
const shapes = {
    single: [{ x: 0, y: 0, z: 0 }],
    pair: [{ x: 0, y: 0, z: 0 }, { x: 1, y: 0, z: 0 }],
    box222: Voxel.box(2, 2, 2, 0xff0000),
    box333: Voxel.box(3, 3, 3, 0x336699),
    L: [{ x: 0, y: 0, z: 0 }, { x: 1, y: 0, z: 0 }, { x: 0, y: 1, z: 0 }],
    cup: [{ x: 0, y: 0, z: 0 }, { x: 1, y: 0, z: 1 }, { x: 0, y: 1, z: 1 }],
    bar411: Voxel.box(4, 1, 1, 0x808080),
    slab313: Voxel.box(3, 1, 3, 0x123456),
    stair: [{ x: 0, y: 0, z: 0, c: 1 }, { x: 1, y: 0, z: 0, c: 2 }, { x: 1, y: 1, z: 0, c: 3 }, { x: 2, y: 1, z: 0, c: 4 }, { x: 2, y: 1, z: 1, c: 5 }, { x: 1, y: 0, z: 1, c: 6 }, { x: 0, y: -1, z: 1, c: 7 }],
};
const faces = {};
for (const name in shapes) {
    faces[name] = Voxel.faces(shapes[name], { color: 0xabcdef }).map(f => ({ n: f.n, corners: f.corners, ao: f.ao, c: f.c, v: [f.vx, f.vy, f.vz] }));
}

// ── jitter · aoShade ────────────────────────────────────────────────────────
const jitter = [];
for (let x = -3; x < 9; x++) for (let y = -2; y < 6; y++) for (let z = -2; z < 4; z++) jitter.push([x, y, z, 0.022, r6(Voxel.jitter(x, y, z, 0.022))]);
[[3, 7, 11, 0.06], [4, 7, 11, 0.06], [100, 200, 300, 0.1], [0, 0, 0, 0.05], [-7, 13, -21, 0.022], [1000, -1000, 5, 0.022]].forEach(a => jitter.push([a[0], a[1], a[2], a[3], r6(Voxel.jitter(a[0], a[1], a[2], a[3]))]));
const aoShade = [];
for (let l = 0; l <= 3; l++) for (const s of [0, 0.5, 0.85, 1]) aoShade.push([l, s, r6(Voxel.aoShade(l, s))]);

// ── vivid (Mobs.build 의 HSL 보정을 그대로) ─────────────────────────────────
function vividHex(col, vivid) {
    const _c = new THREE.Color(col), _h = {};
    _c.getHSL(_h);
    if (_h.s > 0.06) _c.setHSL(_h.h, Math.min(1, _h.s + vivid), Math.min(0.94, _h.l + vivid * 0.22));
    return _c.getHex();
}
const vivid = [];
const vividCols = new Set([0xe8d2ae, 0x9c6e48, 0x3c3c3c, 0xffffff, 0x000000, 0x808080, 0xff0000, 0x00ff00, 0x0000ff, 0xf0e68c, 0x2a9d8f, 0x123456, 0xfefefe, 0x010203]);
for (const k in TABLES) for (const n in TABLES[k]) for (const p of TABLES[k][n].parts) { if (p && !p.off) vividCols.add(p.c); if (p && p.paint) for (const r of p.paint) if (r) vividCols.add(r.c); }
for (const c of vividCols) for (const v of [0.14, 0.16, 0.2]) vivid.push([c, v, vividHex(c, v)]);

// ── paint 체크섬 · build ─────────────────────────────────────────────────────
function cellsHash(cells) {
    let h = 7;
    for (let i = 0; i < cells.length; i++) h = (h * 31 + (cells[i].c >>> 0)) % 1000000007;
    return h;
}
function matKey(o) {
    if (!o) return 'std';
    return (o.basic ? 'b' : 's') + '|' + (o.opacity === undefined ? 1 : o.opacity) +
        '|' + (o.emissive === undefined ? '' : o.emissive) + '|' + (o.emissiveIntensity || 0) +
        '|' + (o.rough === undefined ? '' : o.rough);
}
const paint = {}, build = {}, full = {};
const FULL = new Set(['pets/Snail/head', 'enemies/imp/wingL', 'skillfx/' + Object.keys(TABLES.skillfx)[0] + '/#0']);
for (const k in TABLES) {
    paint[k] = {}; build[k] = {};
    for (const name in TABLES[k]) {
        const model = TABLES[k][name];
        const cell = model.cell || 0.03;
        // paint — 파츠별 칸 색 체크섬(off 파츠는 건너뛴다 · 인덱스는 표 순서)
        const ph = [];
        model.parts.forEach((p, i) => {
            if (!p || p.off) { ph.push(null); return; }
            let cells = Voxel.box(p.box[0], p.box[1], p.box[2], p.c);
            if (p.paint) cells = Mobs.paint(cells, p.box[0], p.box[1], p.box[2], expand(p.paint, p.box[0]));
            ph.push([cells.length, cellsHash(cells)]);
        });
        paint[k][name] = ph;
        // build — 실물 three 로 조립하고 노드 위치·회전·정점 합을 찍는다
        const out = Mobs.build(model, { cell: cell, vivid: 0 });
        const parts = [];
        model.parts.forEach((p, i) => {
            if (!p || p.off) return;
            const pid = p.id || p.tag || ('part' + i);
            // 메시를 찾는다 — pid 를 가진 Mesh(pivot 이 있으면 Group 이 같은 pid 를 갖는다)
            let mesh = null;
            out.group.traverse(o => { if (!mesh && o.isMesh && o.userData.pid === pid && !mesh) mesh = o; });
            // 같은 pid 가 여럿(id 없는 parent 붙은 파츠 · tag 만 있는 파츠)일 수 있으니 i 번째 파츠의 메시를 순서로 잡는다
            const rec = { i: i, pid: pid, parent: p.parent || null, hasPivot: !!p.pivot, matKey: matKey(p.mat) };
            parts.push(rec);
        });
        // 순서대로 메시를 다시 뽑는다: traverse 순서는 add 순서(= 파츠 순서)라 pid 중복도 갈린다
        const meshes = [];
        out.group.traverse(o => { if (o.isMesh) meshes.push(o); });
        if (meshes.length !== parts.length) throw new Error(k + '/' + name + ': 메시 수 ' + meshes.length + ' ≠ 파츠 수 ' + parts.length);
        // traverse 는 깊이 우선이라 add 순서와 다를 수 있다 — 파츠 순서로 다시 맞춘다(메시에 인덱스 표식이 없어 위치·pid 로 식별)
        // 안전하게: build 를 다시 돌리며 파츠마다 메시를 잡는 대신, 메시의 geometry 정점 수와 pid 로 매칭한다.
        const used = new Set();
        parts.forEach((rec, idx) => {
            const p = model.parts[rec.i];
            const cand = meshes.filter((m, mi) => !used.has(mi) && m.userData.pid === rec.pid);
            if (!cand.length) throw new Error(k + '/' + name + ': 메시 못 찾음 ' + rec.pid);
            const m = cand[0]; used.add(meshes.indexOf(m));
            const node = p.pivot ? m.parent : m;
            const pos = m.geometry.getAttribute('position').array, col = m.geometry.getAttribute('color').array;
            let ps = [0, 0, 0], cs = [0, 0, 0], pw = 0;
            for (let j = 0; j < pos.length; j += 3) {
                ps[0] += pos[j]; ps[1] += pos[j + 1]; ps[2] += pos[j + 2];
                cs[0] += col[j]; cs[1] += col[j + 1]; cs[2] += col[j + 2];
                pw += (pos[j] * 1 + pos[j + 1] * 7 + pos[j + 2] * 13) * ((j / 3) % 5 + 1);
            }
            rec.tri = pos.length / 3;
            rec.psum = ps.map(r6); rec.csum = cs.map(v => Math.round(v * 1e4) / 1e4); rec.pw = r6(pw);
            rec.meshPos = [m.position.x, m.position.y, m.position.z].map(r6);
            if (p.pivot) rec.pivotPos = [node.position.x, node.position.y, node.position.z].map(r6);
            rec.rot = [node.rotation.x, node.rotation.y, node.rotation.z];
            rec.parentPid = node.parent === out.group ? null : node.parent.userData.pid;
            rec.isHead = node.userData.part === 'head';
            const key = k + '/' + name + '/' + (p.id || ('#' + rec.i));
            if (FULL.has(key)) {
                full[key] = { pos: Array.from(pos).map(r6), col: Array.from(col).map(v => Math.round(v * 1e5) / 1e5) };
            }
        });
        build[k][name] = {
            cell: cell,
            parts: parts,
            head: out.head ? out.head.userData.pid : null,
            tail: out.tail ? out.tail.userData.pid : null,
            legs: out.legs.map(o => [o.userData.pid, o.userData.gait]),
            wings: out.wings.map(o => [o.userData.pid, o.userData.s]),
            wheels: out.wheels.map(o => o.userData.pid),
            spinners: out.spinners.map(o => o.userData.pid),
            glow: out.glow.map(o => o.userData.pid),
            claws: out.claws.map(o => [o.userData.pid, o.userData.s]),
            joints: out.joints.map(j => [j.o.userData.pid, j.axis, r6(j.base), j.amp, j.ph, j.f, j.gain, j.abs, j.spin]),
            mats: Object.keys(groupMats(model)).length,
        };
    }
}
function expand(rules, w) {   // mobs.js 의 expand 는 내보내지 않는다 — 같은 규칙을 여기 둔다(span 은 Mobs.span)
    if (!rules) return null;
    const out = [];
    for (const R of rules) {
        out.push(R);
        if (R && R.mx) { const xs = Mobs.span(R.x, w); out.push({ c: R.c, x: [w - 1 - xs[1], w - 1 - xs[0]], y: R.y, z: R.z }); }
    }
    return out;
}
function groupMats(model) {
    const m = {};
    for (const p of model.parts) if (p && !p.off) m[matKey(p.mat)] = true;
    return m;
}

const doc = { meta: { three: THREE.REVISION, src: 'kuzuni/wwwww web/js/{voxel,mobs}.js', tables: Object.keys(TABLES).map(k => k + ':' + Object.keys(TABLES[k]).length).join(' ') }, faces, jitter, aoShade, vivid, paint, build, full };
fs.mkdirSync(path.dirname(OUT), { recursive: true });
fs.writeFileSync(OUT, JSON.stringify(doc));
const nParts = Object.values(build).reduce((a, t) => a + Object.values(t).reduce((b, m) => b + m.parts.length, 0), 0);
console.log('✓ voxel_vectors: ' + OUT + ' · faces ' + Object.keys(faces).length + '모양 · jitter ' + jitter.length + ' · vivid ' + vivid.length + ' · 파츠 ' + nParts + ' · full ' + Object.keys(full).length + ' · ' + (fs.statSync(OUT).size / 1024).toFixed(0) + 'KB');
