#!/usr/bin/env node
// ============================================================================
// export_gear_meshes.js — 장비 3D 외형 캡처 (T37)
// 정본 `web/js/scene3d.js` 의 `makeWeapon`(무기 53종 · 시대·등급 재질) · `makeHelmet`(시대 × 이름 5) ·
// `dressMcRig`→`mcArmorParts`(시대 × 이름 5 · 옷 한 벌 = 몸통·소매 2·바지 2 → 관절 본) · `gradeHeroGearValue`(영웅 명도 그레이딩) ·
// `applyAscendDecor`(승천 데코 5층) 을 **실물 three r128 위에서 그대로 실행**해 메시를 JSON 으로 뽑는다.
// «비슷하게 새로 만들기» 가 아니라 «그대로 옮기기» — 유니티(`Assets/Scripts/Game/Hero/GearMeshes.cs`)는 이 파일을 세울 뿐 조형을 모른다.
//
// 파츠 표기(메시 하나 = 파츠 하나 · `mat` 은 모델 루트 기준 4×4 열우선 · three 좌표계 그대로 — 유니티가 z 를 뒤집는다(결정 4)):
//   { k:'vox', size, color, jitter, ao, center, cs, verts }  ← `Voxel.build` 호출을 가로채 **칸 목록**(cellsets[cs])으로(T4 `VoxelGeometry` 가 같은 규약으로 다시 굽는다 · verts 로 대조)
//   { k:'box', size:[w,h,d], verts:24 }                      ← THREE.BoxGeometry
//   { k:'ring', ir, or, seg, verts }                         ← THREE.RingGeometry(승천 데코 L3 룬 링)
//   { k:'geo', geo, pos:[…], idx?:[…], col?:[…], verts }     ← 그 밖 정점 그대로(지금 정본에는 없다 · 정본이 바뀌면 여기로 떨어진다)
// 전역 표(중복 제거): mats[]  = 재질 { t:'std'|'lam'|'basic', c, e, ei, met, rough, op, tr, side, flat, vc, map, env, dw }
//                   cellsets[] = { pal:[hex…], c:[x,y,z,pal 인덱스, …](평탄 · 4칸씩) } — 같은 칸 목록은 한 번만(승천 가시 6·젬 4·왕관 뿔 5 …)
// 등급은 재질(색·발광)과 함께 **젬·트림 파츠 유무**도 바꾼다(club 은 rare 부터 젬) → 지오메트리를 서명으로 묶어 `geoms[]` 에 한 번씩 두고
// 등급마다 `byRarity[rarity] = { g: geoms 인덱스, m:[mats 인덱스 · 파츠 순], hm:[…] }` (hm = gradeHeroGearValue 뒤 = 영웅에 실제로 붙는 값).
// 승천 데코(`applyAscendDecor` 5단계 · 층 = `layer` 1~5 · 별 → 층은 `ascendTier` = stars % 6)는 모델 경계 상자에서 나오므로 지오메트리 변형마다 → geoms[g].decor / dm / dhm.
//
// 출력: Assets/StreamingAssets/data/gear-meshes.json  (check_data_sync.sh 가 다시 뽑아 cmp 한다)
// 사용:  node tools/export_gear_meshes.js [--src .wwwww-src] [--out <file>] [--check] [--self-test]
//   --check     : 다시 뽑아 기존 파일과 같은지만 본다(정본이 바뀌면 rc 1)
//   --self-test : 쓰지 않고 개수·꼴·결정론만 검사
// ============================================================================
'use strict';
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const ROOT = path.resolve(__dirname, '..');
const DEFAULT_SRC = path.join(ROOT, '.wwwww-src');
const DEFAULT_OUT = path.join(ROOT, 'Assets', 'StreamingAssets', 'data', 'gear-meshes.json');

// 정본 index.html 순서(scene3d.js 는 앞 파일들의 전역을 본다).
const LOAD = ['bignum.js', 'util.js', 'balance-data.js', 'gamedata.js', 'voxel.js', 'mobs.js', 'mobdata.js',
    'mobs-pets.js', 'mobs-mounts.js', 'mobs-enemies.js', 'mobs-skillfx.js', 'mobs-props.js', 'prochar.js', 'scene3d.js'];
const DECOR_TIERS = 5;

// ── 브라우저 흉내 — 캔버스 텍스처는 «그림» 이라 메시 좌표·색에 영향이 없다(재질에는 map 유무만 남긴다).
function fakeCtx() {
    const h = { get: (t, k) => (k === 'canvas' ? null : (typeof k === 'string' ? (() => new Proxy({}, h)) : undefined)), set: () => true };
    return new Proxy({}, h);
}
function fakeCanvas() { return { width: 0, height: 0, getContext: () => fakeCtx(), style: {}, toDataURL: () => '' }; }

function loadWorld(src) {
    const web = path.join(src, 'web');
    if (!fs.existsSync(path.join(web, 'js', 'scene3d.js'))) throw new Error(`정본이 없다: ${web}/js/scene3d.js (git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)`);
    const document = { createElement: (tag) => (tag === 'canvas' ? fakeCanvas() : { style: {}, appendChild() { } }), getElementById: () => null, body: { appendChild() { } } };
    const sb = { console, document, navigator: { userAgent: 'node' }, setTimeout, clearTimeout, performance: { now: () => 0 }, requestAnimationFrame: () => 0 };
    sb.self = sb; sb.window = sb; sb.globalThis = sb;
    vm.createContext(sb);
    const load = (rel) => vm.runInContext(fs.readFileSync(path.join(web, rel), 'utf8'), sb, { filename: rel });
    load('lib/three.min.js');
    for (const f of LOAD) load('js/' + f);
    const G = vm.runInContext('({ THREE, Scene3D, ProChar, Voxel, AGES, AGE_WEAPONS, ITEM_NAMES, HELMET_STYLES, ARMOR_STYLES, RARITIES, WEAPON_TYPES })', sb);
    for (const k of ['THREE', 'Scene3D', 'ProChar', 'Voxel', 'AGES', 'AGE_WEAPONS', 'ITEM_NAMES', 'HELMET_STYLES', 'ARMOR_STYLES', 'RARITIES', 'WEAPON_TYPES'])
        if (!G[k]) throw new Error(`정본 전역 ${k} 를 못 찾았다 — 등록 이름이 바뀌었다`);
    const { THREE, ProChar, Voxel } = G;
    // 텍스처는 그림 — 실물 캔버스가 없으니 빈 텍스처(T6 hero_vectors 와 같은 길).
    ProChar.canvasTex = function (key) { return (this._texCache[key] = this._texCache[key] || new THREE.Texture()); };
    ProChar.envMap = function () { return (this._envMap = this._envMap || new THREE.CubeTexture([])); };
    // `Voxel.build` 를 가로채 칸 목록·옵션을 메시에 남긴다 — 메시 대신 칸을 내보내면 파일이 20배 작고 T4 빌더와 대조된다.
    const origBuild = Voxel.build;
    Voxel.build = function (voxels, opts) {
        const m = origBuild.call(this, voxels, opts);
        opts = opts || {};
        m.userData.voxSrc = {
            size: opts.size === undefined ? 0.1 : opts.size,
            color: opts.color === undefined ? 0xffffff : opts.color,
            jitter: opts.jitter === undefined ? 0.06 : opts.jitter,
            ao: opts.ao === undefined ? 1 : opts.ao,
            center: opts.center !== false,
            cells: voxels.map(v => (v.c === undefined ? [v.x, v.y, v.z] : [v.x, v.y, v.z, v.c])),
        };
        return m;
    };
    return G;
}

const r5 = (v) => Math.round(v * 1e5) / 1e5;
const r4 = (v) => Math.round(v * 1e4) / 1e4;

// Scene3D 메서드를 돌릴 가짜 this — 재질 캐시만 제 것으로(호출마다 새로 · 모델 사이 공유 없음).
function fakeScene(G) {
    const f = Object.create(G.Scene3D);
    f._voxMatCache = {}; f._mcMatCache = {}; f._thumbCache = {}; f.heroRig = null;
    return f;
}

// 전역 중복 제거 표 — 재질 서술 · 칸 목록. 인덱스는 처음 나온 순서(= 정본 순서)라 결정론이다.
function makeTables() {
    return { mats: [], matIdx: new Map(), cellsets: [], csIdx: new Map() };
}
function internMat(T, desc) {
    const key = JSON.stringify(desc);
    let i = T.matIdx.get(key);
    if (i === undefined) { i = T.mats.length; T.mats.push(desc); T.matIdx.set(key, i); }
    return i;
}
function internCells(T, cells) {
    const pal = [], pi = new Map(), flat = [];
    for (const v of cells) {
        const c = v.length > 3 ? v[3] : -1;
        let p = pi.get(c);
        if (p === undefined) { p = pal.length; pal.push(c); pi.set(c, p); }
        flat.push(v[0], v[1], v[2], p);
    }
    const set = { pal, c: flat };
    const key = JSON.stringify(set);
    let i = T.csIdx.get(key);
    if (i === undefined) { i = T.cellsets.length; T.cellsets.push(set); T.csIdx.set(key, i); }
    return i;
}

function matDesc(m) {
    return {
        t: m.isMeshBasicMaterial ? 'basic' : m.isMeshLambertMaterial ? 'lam' : 'std',
        c: m.color ? m.color.getHex() : 0xffffff,
        e: m.emissive ? m.emissive.getHex() : 0,
        ei: r5(m.emissiveIntensity === undefined ? 0 : m.emissiveIntensity),
        met: r5(m.metalness === undefined ? 0 : m.metalness),
        rough: r5(m.roughness === undefined ? 1 : m.roughness),
        op: r5(m.opacity === undefined ? 1 : m.opacity),
        tr: !!m.transparent, side: m.side | 0, flat: !!m.flatShading, vc: !!m.vertexColors, map: !!m.map,
        env: r5(m.envMapIntensity === undefined ? 1 : m.envMapIntensity), dw: m.depthWrite !== false,
    };
}

// 루트 아래 메시 전부 → 파츠(지오메트리 + `rel` 기준 행렬) · 재질 서술. `filter` 로 데코만 고를 수 있다.
function capParts(G, root, rel, filter) {
    const { THREE } = G;
    rel.updateMatrixWorld(true);
    const inv = new THREE.Matrix4().copy(rel.matrixWorld).invert();
    const parts = [], mats = [];
    root.traverse(o => {
        if (!o.isMesh || (filter && !filter(o))) return;
        const M = new THREE.Matrix4().multiplyMatrices(inv, o.matrixWorld);
        const g = o.geometry;
        let part;
        const p = g.parameters || {};
        if (o.userData.voxSrc) {
            const v = o.userData.voxSrc;
            part = { k: 'vox', size: r5(v.size), color: v.color, jitter: r5(v.jitter), ao: r5(v.ao), center: v.center, cs: internCells(G.T, v.cells), verts: g.attributes.position.count };
        } else if (g.type === 'BoxGeometry' && p.widthSegments === 1 && p.heightSegments === 1 && p.depthSegments === 1) {
            part = { k: 'box', size: [r5(p.width), r5(p.height), r5(p.depth)], verts: 24 };
        } else if (g.type === 'RingGeometry' && p.phiSegments === 1 && p.thetaStart === 0 && Math.abs(p.thetaLength - Math.PI * 2) < 1e-9) {
            part = { k: 'ring', ir: r5(p.innerRadius), or: r5(p.outerRadius), seg: p.thetaSegments, verts: g.attributes.position.count };
        } else {
            const P = g.attributes.position.array;
            part = { k: 'geo', geo: g.type, pos: Array.from(P, r4), verts: g.attributes.position.count };
            if (g.index) part.idx = Array.from(g.index.array);
            if (g.attributes.color) part.col = Array.from(g.attributes.color.array, r4);
        }
        part.mat = M.elements.map(r4);
        if (o.userData.ascendDecor) part.layer = o.userData.ascendDecor;
        parts.push(part);
        mats.push(internMat(G.T, matDesc(o.material)));
    });
    return { parts, mats };
}

// 지오메트리 서명 — 등급이 지오메트리를 안 바꾸는지 대조(파츠 꼴·행렬·칸 · 재질 제외).
const sig = (parts) => JSON.stringify(parts);

// 한 모델(무기·투구)을 등급 6 × (원재질·영웅 그레이딩) 으로 캡처 — 지오메트리 변형은 서명으로 묶고, 변형마다 승천 데코 5층.
function capModel(G, make, label) {
    const { THREE, Scene3D, RARITIES } = G;
    const out = { geoms: [], byRarity: {} };
    const sigs = [];
    for (const r of RARITIES) {
        const f = fakeScene(G);
        const m = make(f, r);
        const holder = new THREE.Group(); holder.add(m);           // rel = 부모 → 모델 자신의 변환까지 행렬에 든다
        const raw = capParts(G, m, holder);
        if (!raw.parts.length) throw new Error(`${label}: 메시 0개`);
        const s = sig(raw.parts);
        let g = sigs.indexOf(s);
        if (g < 0) {
            g = sigs.length; sigs.push(s);
            // 승천 데코 — 원작 순서 = makeX → applyAscendDecor → gradeHeroGearValue (데코 재질도 그레이딩을 받는다)
            const f2 = fakeScene(G);
            const m2 = make(f2, r);
            const holder2 = new THREE.Group(); holder2.add(m2);
            Scene3D.applyAscendDecor.call(f2, m2, DECOR_TIERS, 'equip');
            const isDeco = o => !!o.userData.ascendDecor;
            const d = capParts(G, m2, holder2, isDeco);
            if (!d.parts.length) throw new Error(`${label}: 승천 데코 0개`);
            Scene3D.gradeHeroGearValue.call(f2, m2);
            out.geoms.push({ parts: raw.parts, decor: d.parts, dm: d.mats, dhm: capParts(G, m2, holder2, isDeco).mats });
        }
        Scene3D.gradeHeroGearValue.call(f, m);
        out.byRarity[r] = { g, m: raw.mats, hm: capParts(G, m, holder).mats };
    }
    return out;
}

function boneNameMap(R) {
    const map = new Map();
    for (const k in R.bones) map.set(R.bones[k], k);
    map.set(R.root, 'root');
    return map;
}
function parentBone(map, o) {
    for (let p = o.parent; p; p = p.parent) if (map.has(p)) return { name: map.get(p), obj: p };
    return null;
}

// 갑옷 = dressMcRig 로 리그 본에 붙는 옷 조각들 — 조각마다 본 이름 · 본 기준 로컬 변환 · 조각 자신 기준 파츠.
function capArmor(G, R, map, age, nameIdx) {
    const { THREE, Scene3D, RARITIES } = G;
    const out = { age, nameIdx, style: (G.ARMOR_STYLES[age] || [])[nameIdx] || 'plate', name: G.ITEM_NAMES[age].armor[nameIdx], geoms: [], byRarity: {} };
    const f = fakeScene(G);
    let prev = null; const sigs = [];
    for (const r of RARITIES) {
        prev = Scene3D.dressMcRig.call(f, R, { age, nameIdx, rarity: r }, prev);
        if (!prev.length) throw new Error(`갑옷 ${age}/${nameIdx}: 조각 0개`);
        const pieces = [], mats = [];
        for (const o of prev) {
            const pb = parentBone(map, o);
            if (!pb) throw new Error(`갑옷 ${age}/${nameIdx}: 본을 못 찾은 조각`);
            const c = capParts(G, o, o);
            pieces.push({
                bone: pb.name,
                pos: [r5(o.position.x), r5(o.position.y), r5(o.position.z)],
                rot: [r5(o.rotation.x), r5(o.rotation.y), r5(o.rotation.z)],
                scale: [r5(o.scale.x), r5(o.scale.y), r5(o.scale.z)],
                parts: c.parts,
            });
            mats.push(c.mats);
        }
        const s = sig(pieces);
        let g = sigs.indexOf(s);
        if (g < 0) { g = sigs.length; sigs.push(s); out.geoms.push({ pieces }); }
        out.byRarity[r] = { g, m: mats };
    }
    Scene3D.dressMcRig.call(f, R, null, prev);   // 벗긴다(다음 갑옷)
    return out;
}

function extract(src) {
    const G = loadWorld(src);
    G.T = makeTables();
    const { THREE, Scene3D, ProChar, AGES, AGE_WEAPONS, ITEM_NAMES, HELMET_STYLES, RARITIES } = G;
    const weapons = {};
    for (const age of AGES) {
        const ageIdx = AGES.indexOf(age);
        for (const wt of AGE_WEAPONS[age] || []) {
            if (weapons[wt]) throw new Error(`무기 ${wt} 가 두 시대에 있다 — 키를 시대까지 넓혀야 한다`);
            weapons[wt] = Object.assign({ age, ageIdx }, capModel(G, (f, r) => Scene3D.makeWeapon.call(f, wt, ageIdx, r), `무기 ${wt}`));
        }
    }
    const helmets = {};
    for (const age of AGES) {
        const names = ITEM_NAMES[age].helmet, styles = HELMET_STYLES[age] || [];
        for (let i = 0; i < names.length; i++) {
            const style = styles[i] || 'plume', name = names[i];
            helmets[`${age}/${i}`] = Object.assign({ age, nameIdx: i, style, name }, capModel(G, (f, r) => Scene3D.makeHelmet.call(f, age, r, style, name), `투구 ${age}/${i}`));
        }
    }
    const R = ProChar.createKnight();
    if (!R.mc || !R.bones || !R.headMount) throw new Error('createKnight 가 박스 리그(mc·bones·headMount)를 만들지 않았다');
    const map = boneNameMap(R);
    const armors = {};
    for (const age of AGES) {
        const names = ITEM_NAMES[age].armor;
        for (let i = 0; i < names.length; i++) armors[`${age}/${i}`] = capArmor(G, R, map, age, i);
    }
    // 투구 부착점: 원작 helmetG 는 rig.headMount(머리 중심 기준 · y 0.08) 아래 — 그 본과 본 기준 위치.
    R.group.updateMatrixWorld(true);
    const hb = parentBone(map, R.headMount);
    const rel = new THREE.Matrix4().copy(hb.obj.matrixWorld).invert().multiply(R.headMount.matrixWorld);
    const hp = new THREE.Vector3().setFromMatrixPosition(rel);
    const meta = {
        note: '정본 scene3d.js makeWeapon/makeHelmet/dressMcRig(+gradeHeroGearValue·applyAscendDecor) 를 실물 three 로 돌린 캡처 (T37 · tools/export_gear_meshes.js). 좌표는 three 그대로 — 유니티가 z 를 뒤집는다.',
        three: THREE.REVISION, decorTiers: DECOR_TIERS, rarities: RARITIES.slice(),
        mc: R.mc, helmetMount: { bone: hb.name, pos: [r5(hp.x), r5(hp.y), r5(hp.z)] },
        counts: { weapons: Object.keys(weapons).length, helmets: Object.keys(helmets).length, armors: Object.keys(armors).length, mats: G.T.mats.length, cellsets: G.T.cellsets.length },
    };
    return { meta, mats: G.T.mats, cellsets: G.T.cellsets, weapons, helmets, armors };
}

// 결정론 직렬화 — 배열은 한 줄, 객체는 키 순서 그대로(생성 순서 = 정본 순서).
function fmt(v, ind) {
    if (v === null || typeof v !== 'object') return JSON.stringify(v);
    if (Array.isArray(v)) return '[' + v.map(x => fmt(x, ind)).join(',') + ']';
    const keys = Object.keys(v);
    if (!keys.length) return '{}';
    const pad = '  '.repeat(ind + 1);
    return '{\n' + keys.map(k => pad + JSON.stringify(k) + ': ' + fmt(v[k], ind + 1)).join(',\n') + '\n' + '  '.repeat(ind) + '}';
}
const serialize = (data) => fmt(data, 0) + '\n';

function selfTest(src) {
    const fails = [];
    const ok = (label, cond, got) => { console.log(`  ${cond ? '✓' : '✗'} ${label}${got === undefined ? '' : ' = ' + got}`); if (!cond) fails.push(label); };
    const data = extract(src);
    const G = loadWorld(src);
    const cnt = o => Object.keys(o).length;
    const nWt = cnt(G.WEAPON_TYPES);
    ok(`무기 = WEAPON_TYPES 전부(${nWt})`, cnt(data.weapons) === nWt && Object.keys(G.WEAPON_TYPES).every(w => data.weapons[w]), cnt(data.weapons));
    const nHelm = G.AGES.reduce((a, age) => a + G.ITEM_NAMES[age].helmet.length, 0), nArm = G.AGES.reduce((a, age) => a + G.ITEM_NAMES[age].armor.length, 0);
    ok(`투구 = ITEM_NAMES 투구 이름 전부(${nHelm})`, cnt(data.helmets) === nHelm, cnt(data.helmets));
    ok(`갑옷 = ITEM_NAMES 갑옷 이름 전부(${nArm})`, cnt(data.armors) === nArm, cnt(data.armors));
    const kinds = new Set(); let bad = 0, vox = 0, verts = 0;
    const checkParts = (parts, label) => {
        for (const p of parts) {
            kinds.add(p.k); verts += p.verts;
            if (!(p.verts > 0) || !Array.isArray(p.mat) || p.mat.length !== 16) { bad++; console.log(`    ✗ ${label}: 파츠 꼴 이상 ${p.k}`); }
            if (p.k === 'vox') {
                vox++;
                const cs = data.cellsets[p.cs];
                if (!cs || !cs.c.length || cs.c.length % 4 !== 0 || !(p.size > 0)) { bad++; console.log(`    ✗ ${label}: 칸 0 / size`); }
                // Voxel.build 정점 수 = 노출 면 × 6 — 칸 수 × 36 이하
                else if (p.verts > (cs.c.length / 4) * 36) { bad++; console.log(`    ✗ ${label}: 정점 수가 칸 수와 안 맞는다`); }
            }
            if (p.k === 'ring' && !(p.ir > 0 && p.or > p.ir && p.seg >= 3)) { bad++; console.log(`    ✗ ${label}: 링 인자`); }
        }
    };
    let geomVariants = 0;
    const checkModel = (m, label) => {
        geomVariants += m.geoms.length;
        for (const g of m.geoms) {
            checkParts(g.parts, label); checkParts(g.decor, label + ' decor');
            if (g.dm.length !== g.decor.length || g.dhm.length !== g.decor.length) { bad++; console.log(`    ✗ ${label}: 데코 재질 수`); }
            const layers = new Set(g.decor.map(p => p.layer));
            if (![1, 2, 3, 4, 5].every(l => layers.has(l))) { bad++; console.log(`    ✗ ${label}: 데코 층 1~5 중 빠짐 ${[...layers]}`); }
        }
        for (const r of data.meta.rarities) {
            const b = m.byRarity[r];
            const n = b && m.geoms[b.g] ? m.geoms[b.g].parts.length : -1;
            if (!b || !b.m || b.m.length !== n || !b.hm || b.hm.length !== n || b.m.some(i => !data.mats[i]) || b.hm.some(i => !data.mats[i])) { bad++; console.log(`    ✗ ${label}: 등급 ${r} 재질 수 ≠ 파츠 수`); }
        }
    };
    for (const k in data.weapons) checkModel(data.weapons[k], '무기 ' + k);
    for (const k in data.helmets) checkModel(data.helmets[k], '투구 ' + k);
    for (const k in data.armors) {
        const a = data.armors[k];
        geomVariants += a.geoms.length;
        for (const g of a.geoms) {
            if (!(g.pieces.length >= 3)) { bad++; console.log(`    ✗ 갑옷 ${k}: 조각 ${g.pieces.length}`); }
            for (const pc of g.pieces) { checkParts(pc.parts, '갑옷 ' + k); if (!['spine', 'shoulderL', 'shoulderR', 'hipL', 'hipR'].includes(pc.bone)) { bad++; console.log(`    ✗ 갑옷 ${k}: 본 ${pc.bone}`); } }
        }
        for (const r of data.meta.rarities) { const b = a.byRarity[r]; if (!b || !b.m || b.m.length !== a.geoms[b.g].pieces.length || b.m.some((pm, i) => pm.length !== a.geoms[b.g].pieces[i].parts.length)) { bad++; console.log(`    ✗ 갑옷 ${k}: 등급 ${r} 재질`); } }
    }
    ok('파츠 꼴(verts>0 · 행렬 16 · 칸>0) · 등급별 재질 수 = 파츠 수 · 데코 층 1~5 · 갑옷 조각 본', bad === 0, `파츠 정점 합 ${verts} · vox 파츠 ${vox} · 지오메트리 변형 ${geomVariants} · 꼴 ${[...kinds].join(',')}`);
    ok('파츠 꼴은 vox·box·ring 뿐(geo 는 정본이 바뀐 것)', [...kinds].every(k => ['vox', 'box', 'ring'].includes(k)));
    ok('전역 표: 재질 · 칸 목록', data.mats.length > 0 && data.cellsets.length > 0 && data.mats.every(m => typeof m.c === 'number' && ['std', 'lam', 'basic'].includes(m.t)), `재질 ${data.mats.length} · 칸 목록 ${data.cellsets.length}`);
    ok('helmetMount 본 = head · mc 치수 있음', data.meta.helmetMount.bone === 'head' && data.meta.mc && data.meta.mc.torW > 0, JSON.stringify(data.meta.helmetMount));
    const sword = data.weapons.sword;
    ok('sword: 등급이 재질을 바꾼다(common ≠ mythic) · 그레이딩이 밝은 재질을 어둡게', sword && JSON.stringify(sword.byRarity.common.m) !== JSON.stringify(sword.byRarity.mythic.m)
        && sword.byRarity.common.hm.some((mi, i) => data.mats[mi].c !== data.mats[sword.byRarity.common.m[i]].c));
    ok('club: 등급별 지오메트리 변형 ≥ 2(rare 부터 젬)', data.weapons.club && data.weapons.club.geoms.length >= 2, data.weapons.club && data.weapons.club.geoms.length);
    console.log('[결정론]');
    const t1 = serialize(data), t2 = serialize(extract(src));
    ok('두 번 뽑아도 바이트 동일', t1 === t2);
    ok('JSON 으로 다시 읽힘', (() => { try { JSON.parse(t1); return true; } catch (e) { return false; } })());
    console.log(`gear-meshes.json ${(Buffer.byteLength(t1) / 1024).toFixed(0)} KB`);
    if (fails.length) { console.log(`✗ export_gear_meshes self-test 실패 ${fails.length}건: ${fails.join(' / ')}`); return 1; }
    console.log('✓ export_gear_meshes self-test 통과');
    return 0;
}

function main(argv) {
    let src = DEFAULT_SRC, out = DEFAULT_OUT, check = false, test = false;
    for (let i = 0; i < argv.length; i++) {
        const a = argv[i];
        if (a === '--src') src = path.resolve(argv[++i]);
        else if (a === '--out') out = path.resolve(argv[++i]);
        else if (a === '--check') check = true;
        else if (a === '--self-test') test = true;
        else if (a === '-h' || a === '--help') { console.log('node tools/export_gear_meshes.js [--src <wwwww>] [--out <file>] [--check] [--self-test]'); return 0; }
        else { console.error(`모르는 인자: ${a}`); return 2; }
    }
    if (test) return selfTest(src);
    const text = serialize(extract(src));
    if (check) {
        const old = fs.existsSync(out) ? fs.readFileSync(out, 'utf8') : null;
        if (old === text) { console.log(`✓ export_gear_meshes --check: ${path.relative(ROOT, out)} 가 정본과 같다`); return 0; }
        console.log(`✗ export_gear_meshes --check: ${path.relative(ROOT, out)} 가 정본과 다르다 — node tools/export_gear_meshes.js 로 다시 뽑아 커밋(손으로 고치지 말 것)`);
        return 1;
    }
    fs.mkdirSync(path.dirname(out), { recursive: true });
    fs.writeFileSync(out, text, 'utf8');
    console.log(`✓ ${path.relative(ROOT, out)}  ${(Buffer.byteLength(text) / 1024).toFixed(0)} KB`);
    return 0;
}

if (require.main === module) process.exit(main(process.argv.slice(2)));
module.exports = { extract, serialize, selfTest };
