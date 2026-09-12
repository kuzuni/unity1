#!/usr/bin/env node
'use strict';
// T9 — 정본 scene3d.js 의 `setTheme(t)`·`voxelGroundGeo()` 를 **실물 three r128 위에서 실제로 실행**해 벡터를 뽑는다.
//   node tools/world_vectors.js [--src <wwwww>] [--out Assets/Tests/EditMode/Vectors/t9-world.json]
// 씬 전체(WebGL·DOM)는 못 세우므로 `this` 를 setTheme 이 만지는 칸(재질·광원·안개·렌더러)만 있는 가짜 객체로 준다 —
// 메서드는 `Object.create(Scene3D)` 로 정본 것을 그대로 쓰고(soilOf·roadOf·leafSet·stoneFrom·bkin…), 캔버스가 필요한
// `buildProps`(지면 텍스처)·`makeCrackTexture`·`paintSky`·`syncVxMats`·`ensureBlobRes` 만 빈 함수로 막는다.
// 결과: 챕터 25 × {안개색·배경·지면색·노면 배율·눈·태양·반구광·림·안개 near/far·노출·돌색·지면 발광} + 지면 격자 체크섬.
// C# `WorldGrade`(Core/World)·`GroundGrid` 가 같은 입력 → 같은 값을 내는지 EditMode `WorldGradeTests` 가 잰다.
const fs = require('fs');
const path = require('path');
const vm = require('vm');
const ROOT = path.resolve(__dirname, '..');
const ex = require('./export_data.js');

function args() {
    const a = process.argv.slice(2);
    const o = { src: path.join(ROOT, '.wwwww-src'), out: path.join(ROOT, 'Assets', 'Tests', 'EditMode', 'Vectors', 't9-world.json') };
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
    return { THREE: sb.THREE, Scene3D: g('Scene3D'), CHAPTER_THEMES: g('CHAPTER_THEMES'), U: g('U') };
}

const col = (c) => ({ r: c.r, g: c.g, b: c.b, hex: c.getHex() });
const v3 = (v) => [v.x, v.y, v.z];

function fakeScene(THREE, Scene3D) {
    const f = Object.create(Scene3D);
    f.renderer = { setClearColor() {}, toneMappingExposure: 1 };
    f.scene = { fog: new THREE.Fog(0xa8d8ea, 12, 30), background: new THREE.Color(0xa8d8ea) };
    f.terrainMat = new THREE.MeshPhongMaterial({ color: 0x8a7350, vertexColors: true });
    f.terrainMat.userData = {};
    f.TERRAIN = Object.assign({}, Scene3D.TERRAIN);
    f.mountainMat = new THREE.MeshBasicMaterial({ color: 0x558b2f });
    f.hillMat = new THREE.MeshBasicMaterial({ color: 0x6d9150 });
    f.farHillMat = new THREE.MeshBasicMaterial({ color: 0x9db98d });
    f.foliageMat = new THREE.MeshPhongMaterial({ color: 0x33691e });
    f.foliageMatDark = f.foliageMat.clone();
    f.foliageMatLight = f.foliageMat.clone();
    f.foliageMats = [f.foliageMat, f.foliageMatDark, f.foliageMatLight];
    f.bushMat = new THREE.MeshPhongMaterial({ color: 0x4a7c2f });
    f.mossMat = new THREE.MeshPhongMaterial({ color: 0x4f8578 });
    f.stoneMat = new THREE.MeshPhongMaterial({ color: 0x9a9083 });
    f.crystalMat = new THREE.MeshPhongMaterial({ color: 0x9575cd, emissive: 0x6a3fb5, emissiveIntensity: 0.5 });
    f._crystalDefault = { color: 0x9575cd, emissive: 0x6a3fb5, intensity: 0.5, halo: 0x4dd9e8, glow: 0x26c6da };
    f.blobShadowMat = new THREE.MeshBasicMaterial({ color: 0 });
    f.blobShadowFlyMat = new THREE.MeshBasicMaterial({ color: 0 });
    f.blobShadowFoeMat = new THREE.MeshBasicMaterial({ color: 0 });
    f.hemi = new THREE.HemisphereLight(0xbddcff, 0x6e7a60, 0.54);
    f.sun = new THREE.DirectionalLight(0xfff3d6, 1.2);
    f.sun.userData = {};
    f.rim = new THREE.DirectionalLight(0xcfe4ff, 0.5);
    f.pathMesh = null;
    f.sunDisc = { visible: true }; f.moonDisc = { visible: false }; f.stars = { visible: false }; f.aurora = null;
    f._shadeUniforms = [];
    f._biome = null;
    f.ensureBlobRes = function () {};
    f.paintSky = function () {};
    f.syncVxMats = function () {};
    f.buildProps = function (b) { this._biome = b; };          // 캔버스(지면 텍스처)를 만지는 자리 — 바이옴만 기억한다
    f.makeCrackTexture = function () { return { isCrackTex: true }; };
    f.setTerrainUniform = function (k, v) { this.TERRAIN[k] = v; };
    // gC(값 그레이드 지면색)는 로컬 변수라 밖으로 안 나온다 — soilOf 의 첫 인자가 그것이다. 가로채 기록한다.
    f.soilOf = function (gC, biome) { this._rec.gC = gC.clone(); return Scene3D.soilOf.call(this, gC, biome); };
    return f;
}

function captureTheme(THREE, Scene3D, t, index) {
    const f = fakeScene(THREE, Scene3D);
    f._rec = {};
    Scene3D.setTheme.call(f, t);
    const road = f.TERRAIN.road;
    return {
        index, biome: t.biome || 'forest', kin: f.bkin(t.biome || 'forest'), celestial: t.celestial || 'sun',
        night: (t.celestial || 'sun') === 'moon',
        theme: { sky: t.sky, fog: t.fog, ground: t.ground },
        fog: { color: col(f.scene.fog.color), near: f.scene.fog.near, far: f.scene.fog.far },
        background: col(f.scene.background),
        gC: col(f._rec.gC),
        terrain: {
            color: col(f.terrainMat.color), road: road ? [road.x, road.y, road.z] : [1, 1, 1], snow: f.TERRAIN.snow,
            emissive: col(f.terrainMat.emissive), emissiveIntensity: f.terrainMat.emissiveIntensity, crackMap: !!f.terrainMat.emissiveMap,
        },
        sun: { pos: v3(f.sun.position), intensity: f.sun.intensity, color: col(f.sun.color) },
        hemi: { intensity: f.hemi.intensity, color: col(f.hemi.color), groundColor: col(f.hemi.groundColor) },
        rim: { intensity: f.rim.intensity, color: col(f.rim.color) },
        exposure: f.renderer.toneMappingExposure,
        stone: col(f.stoneMat.color),
        moss: col(f.mossMat.color),
        blobShade: col(f.blobShadowMat.color),
        foliage: f.foliageMats.map(m => col(m.color)),
        bush: col(f.bushMat.color),
        ridge: [col(f.mountainMat.color), col(f.hillMat.color), col(f.farHillMat.color)],
    };
}

function captureGround(THREE, Scene3D) {
    const f = fakeScene(THREE, Scene3D);
    const geo = Scene3D.voxelGroundGeo.call(f);
    const P = geo.attributes.position.array, N = geo.attributes.normal.array, UV = geo.attributes.uv.array, C = geo.attributes.color.array;
    const sum = (a, k, m) => { let s = 0; for (let i = k; i < a.length; i += m) s += a[i]; return s; };
    const BS = Scene3D.VOXG.cell, NX = Math.round(60 / BS), NZ = Math.round(60 / BS);
    // 셀 (ix, iz) 의 윗면 첫 정점 색 — 정본은 셀마다 윗면 쿼드(정점 6)를 먼저 낸다(SIMPLE_BG 는 벽이 없다 = 셀당 정확히 6 정점).
    const cells = [];
    for (let ix = 0; ix < NX; ix += 5) for (let iz = 0; iz < NZ; iz += 3) {
        const v = (ix * NZ + iz) * 6;
        cells.push({ ix, iz, x: P[v * 3], y: P[v * 3 + 1], z: P[v * 3 + 2], u: UV[v * 2], v: UV[v * 2 + 1], r: C[v * 3], g: C[v * 3 + 1], b: C[v * 3 + 2] });
    }
    return {
        cell: BS, x0: -30, z0: -45, nx: NX, nz: NZ, vertexCount: P.length / 3,
        sum: { x: sum(P, 0, 3), y: sum(P, 1, 3), z: sum(P, 2, 3), nx: sum(N, 0, 3), ny: sum(N, 1, 3), nz: sum(N, 2, 3), u: sum(UV, 0, 2), v: sum(UV, 1, 2), r: sum(C, 0, 3), g: sum(C, 1, 3), b: sum(C, 2, 3) },
        cells,
    };
}

function main() {
    const o = args();
    const { THREE, Scene3D, CHAPTER_THEMES } = load(o.src);
    const themes = CHAPTER_THEMES.map((t, i) => captureTheme(THREE, Scene3D, t, i));
    const ground = captureGround(THREE, Scene3D);
    const doc = {
        meta: { note: '정본 scene3d.js setTheme(t)·voxelGroundGeo() 를 실물 three r128 위에서 가짜 this 로 실제 실행한 결과 (T9). 색은 three Color 의 r/g/b(0..1 double) + getHex.', three: THREE.REVISION, simpleBg: Scene3D.SIMPLE_BG, themes: themes.length },
        themes, ground,
    };
    fs.mkdirSync(path.dirname(o.out), { recursive: true });
    fs.writeFileSync(o.out, ex.serialize(doc), 'utf8');
    console.log(`✓ world_vectors: 테마 ${themes.length} · 지면 정점 ${ground.vertexCount} · 표본 셀 ${ground.cells.length} → ${path.relative(ROOT, o.out)}`);
    return 0;
}
process.exit(main());
