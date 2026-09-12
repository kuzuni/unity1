#!/usr/bin/env node
// ============================================================================
// export_data.js — 정본(kuzuni/wwwww `web/`)의 JS 표를 **평가된 값** 그대로 JSON 으로 뽑는다 (T2).
// ----------------------------------------------------------------------------
// 왜 «파싱» 이 아니라 «평가» 인가 — 조형 표는 `quad()`·`eyes()`·`tail()` 같은 도우미가 로드 시점에
// 배열을 만들어 넣는다(mobdata.js). 소스를 정규식으로 읽으면 그 도우미가 만든 파츠가 빠진다.
// 그래서 원작 파일을 index.html 과 같은 순서로 `vm` 컨텍스트(= globalThis 루트)에 로드하고,
// 다 만들어진 값을 꺼낸다. 의존성 0 · node 22.
//
// 출력(기본 `Assets/StreamingAssets/data/`):
//   balance.json      balance-data.js 의 최상위 const 전부 (대장간 확률·비용·알 드랍·부화·펫 스탯·소환·탈것)
//   gamedata.json     gamedata.js 의 최상위 const 중 값(함수 제외) 전부 (시대·등급·장비명·스킬·펫·테마)
//   mobs-pets.json    PET_MODELS     — 종 이름 → {cell, parts:[…]}   (펫 25)
//   mobs-mounts.json  MOUNT_MODELS   — (탈것 29)
//   mobs-enemies.json ENEMY_MODELS   — (적 7)
//   mobs-skillfx.json SKILLFX_MODELS — (스킬 오브젝트)
//   tech.json         TechTree·Ascension 의 표 칸(TECH_FIELDS · T24) — 분기·노드·보너스·비용 배수 · 승천 라인·별 배율
//   mobs-props.json   Props          — 소품은 표가 아니라 **생성 함수**(`Props.pine(s,o)` … · Math.random)라
//                     함수 이름 목록(kinds) + 결정론 시드로 뽑은 **표본**(samples) 을 낸다. T9 가 생성기를
//                     C# 으로 옮길 때 같은 시드·같은 인자로 같은 칸 목록이 나오는지 대조하는 고정 표본이다.
//   state.json       state.js 의 최상위 const(세이브 키 · 오프라인 수급률·캡 · 난이도 티어 이름 · 사이클/챕터 길이 ·
//                     형태 보정 키 목록) + `DEFAULT_STATE` = `defaultState()` 를 **평가한 값**(시각 칸 createdAt·lastSeen·
//                     lastOfflineClaim 은 `U.now` 를 0 으로 두고 뽑는다 — 결정론 · 유니티가 만들 때 현재 시각을 넣는다)
//                     + `MODULE_CAPS` = state.js 가 다른 모듈에서 읽는 상한(`Pets.MAX_ACTIVE` · `Skills.MAX_ACTIVE` ·
//                     `Mounts.MAX_ACTIVE_MOUNTS` · `Forge.MAX_LEVEL`) — 키가 곧 원작 식이다. T13 세이브가 쓴다.
//                     ⚠ state.js 는 `window` 를 만지므로(캡처 하네스용 접근자) **별도 컨텍스트**(window = 자기 자신)에 올린다.
//
// 값 규약: 색은 원작 그대로 0xRRGGBB 정수(JSON 에서는 십진 정수). `paint` 규칙의 음수 인덱스·`mx` 거울은
// 풀지 않고 그대로 둔다 — 그것을 푸는 것은 원작 `Mobs.build` 의 몫이고 유니티에서는 T4 `VoxelMob` 이 한다.
// 표 안에 함수·undefined·NaN·Infinity 가 있으면 조용히 떨구지 않고 **경로를 찍고 실패**한다(정본이 바뀌면
// 여기서 먼저 걸리게).
//
// 사용:
//   node tools/export_data.js                       # .wwwww-src → Assets/StreamingAssets/data/
//   node tools/export_data.js --src <wwwww 체크아웃> --out <폴더>
//   node tools/export_data.js --self-test           # 쓰지 않고 개수·합·결정론만 검사 (게이트 §3)
// ============================================================================
'use strict';
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const ROOT = path.resolve(__dirname, '..');
const DEFAULT_SRC = path.join(ROOT, '.wwwww-src');
const DEFAULT_OUT = path.join(ROOT, 'Assets', 'StreamingAssets', 'data');

// index.html 의 <script> 순서 그대로(조형 표는 mobs.js → mobdata.js 뒤에 와야 MobParts 가 있다).
const LOAD_ORDER = [
    'balance-data.js', 'gamedata.js',
    'voxel.js', 'mobs.js', 'mobdata.js',
    'mobs-pets.js', 'mobs-mounts.js', 'mobs-enemies.js', 'mobs-skillfx.js', 'mobs-props.js',
    // T24: 기술 트리·승천은 객체 리터럴(`const TechTree = {…}`) 안에 표와 메서드가 섞여 있다 — 표 칸만 뽑는다(TECH_FIELDS).
    'techtree.js', 'ascension.js',
];

// T24 — techtree.js / ascension.js 에서 «표» 인 칸만(메서드·캐시 제외). 순서 = 원문 순서.
// 여기 없는 칸(rows·parentsOf·cost·time·*Mult …)은 규칙(코드)이라 C# `TechTree`(Core/Tech)가 그대로 옮긴다.
const TECH_FIELDS = {
    TechTree: ['TIERS', 'MAX_LEVEL', 'BRANCHES', 'NODES', 'BONUS', 'LEGACY_MAP',
        'LV_MULT', 'TIER_MULT', 'TIME_BASE', 'TIME_LV_MULT', 'TIME_TIER_MULT', 'ROMAN'],
    Ascension: ['STAR_MULT', 'LINES', 'LINE_KR', 'LINE_ICON', 'FORGE_LEVEL'],
};

// 원작 tools/lib-seed.js 와 같은 xorshift32 · 같은 시드 — 소품 표본은 이 스트림에서 나온다.
const SEED = 0x2f6e2b1;

// 소품 표본 호출 목록 — scene3d.js 가 실제로 부르는 인자 꼴(s=1 · 바이옴 변주는 인자로).
// 이름은 «함수(인자)» 그대로 적어 T9 가 무엇을 재현해야 하는지 읽을 수 있게 한다.
const PROP_SAMPLES = [
    ['pine(1,{})', 'pine', [1, {}]],
    ['pine(1,{snow:true})', 'pine', [1, { snow: true }]],
    ['broadleaf(1,{species:"oak"})', 'broadleaf', [1, { species: 'oak' }]],
    ['broadleaf(1,{species:"birch"})', 'broadleaf', [1, { species: 'birch' }]],
    ['broadleaf(1,{species:"jungle"})', 'broadleaf', [1, { species: 'jungle' }]],
    ['deadTree(1)', 'deadTree', [1]],
    ['cactus(1)', 'cactus', [1]],
    ['boulder(1,false,false)', 'boulder', [1, false, false]],
    ['boulder(1,true,false)', 'boulder', [1, true, false]],
    ['boulder(1,false,true)', 'boulder', [1, false, true]],
    ['slab(1)', 'slab', [1]],
    ['strata(1)', 'strata', [1]],
    ['spire(1)', 'spire', [1]],
    ['volcanic(1)', 'volcanic', [1]],
    ['dryShrub(1)', 'dryShrub', [1]],
    ['bones(1)', 'bones', [1]],
    ['bamboo(1)', 'bamboo', [1]],
    ['mushroom(1)', 'mushroom', [1]],
    ['bush(1)', 'bush', [1]],
    ['pebble(1,false,"stone")', 'pebble', [1, false, 'stone']],
    ['pebble(1,true,"stone")', 'pebble', [1, true, 'stone']],
    ['flowers()', 'flowers', []],
    ['fern()', 'fern', []],
];
// Props 안의 조립 도우미 — 생성기가 아니다(kinds 에서 뺀다).
const PROP_HELPERS = new Set(['bx', 'cbx', 'octa', 'capLayer', 'sub']);

// ── 로드 ────────────────────────────────────────────────────────────────────
function makeContext() {
    const sandbox = { console };
    // 원작 파일은 `typeof window !== 'undefined' ? window : globalThis` 로 루트를 고른다 — window 를 두지 않는다.
    vm.createContext(sandbox);
    return sandbox;
}

function loadAll(src) {
    const webjs = path.join(src, 'web', 'js');
    if (!fs.existsSync(webjs)) throw new Error(`정본이 없다: ${webjs} (git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)`);
    const ctx = makeContext();
    const sources = {};
    for (const f of LOAD_ORDER) {
        const p = path.join(webjs, f);
        const code = fs.readFileSync(p, 'utf8');
        sources[f] = code;
        vm.runInContext(code, ctx, { filename: f });
    }
    return { ctx, sources };
}

// state.json 용 두 번째 컨텍스트 — index.html 순서 중 state.js 와 그것이 로드 시점·defaultState() 에서 읽는 파일만.
// (icongen.js 는 DEFAULT_AVATAR · gamedata.js 는 CHAPTER_THEMES · 뒤 넷은 MODULE_CAPS 의 상한 값)
const STATE_LOAD_ORDER = [
    'bignum.js', 'util.js', 'icongen.js', 'balance-data.js', 'gamedata.js',
    'state.js', 'forge.js', 'pets.js', 'skills.js', 'mounts.js',
];
// state.js 가 다른 모듈에서 읽는 상한 — 키는 원작 식 그대로(어디서 온 수인지 JSON 만 보고 알게).
const MODULE_CAPS = ['Pets.MAX_ACTIVE', 'Skills.MAX_ACTIVE', 'Mounts.MAX_ACTIVE_MOUNTS', 'Forge.MAX_LEVEL'];

function loadState(src) {
    const webjs = path.join(src, 'web', 'js');
    const sandbox = { console };
    sandbox.window = sandbox;   // state.js: Object.defineProperty(window, 'S', …)
    vm.createContext(sandbox);
    let stateCode = '';
    for (const f of STATE_LOAD_ORDER) {
        const code = fs.readFileSync(path.join(webjs, f), 'utf8');
        if (f === 'state.js') stateCode = code;
        vm.runInContext(code, sandbox, { filename: f });
    }
    return { ctx: sandbox, stateCode };
}

// state.json — 최상위 const(함수·`S` 제외) + DEFAULT_STATE + MODULE_CAPS.
function extractState(src) {
    const { ctx, stateCode } = loadState(src);
    const out = pickTopLevel(ctx, stateCode, 'state.js');
    delete out.S;   // `let S = null` — 살아 있는 상태 자리표지 표가 아니다
    // 시각 칸을 0 으로 고정해 뽑는다(U.now 는 Date.now). 유니티 SaveDefs.DefaultState(now) 가 그 셋을 채운다.
    out.DEFAULT_STATE = vm.runInContext(
        '(() => { const realNow = U.now; U.now = () => 0; try { return defaultState(); } finally { U.now = realNow; } })()',
        ctx, { filename: 'state.js#defaultState' });
    const caps = {};
    for (const expr of MODULE_CAPS) {
        const v = vm.runInContext(expr, ctx, { filename: 'state.js#caps' });
        if (typeof v !== 'number') throw new Error(`MODULE_CAPS ${expr}: 수가 아니다(${typeof v}) — 정본의 상한 이름이 바뀌었다`);
        caps[expr] = v;
    }
    out.MODULE_CAPS = caps;
    return clean(out, 'state');
}

// 최상위 `const|let|var NAME =` 이름 — 같은 컨텍스트의 스크립트 스코프에 살아 있어 이름으로 꺼낼 수 있다.
function topLevelNames(code) {
    const out = [];
    const re = /^(?:const|let|var)\s+([A-Za-z_$][\w$]*)\s*=/gm;
    let m;
    while ((m = re.exec(code))) out.push(m[1]);
    return out;
}

function pickTopLevel(ctx, code, filename) {
    const names = topLevelNames(code);
    const expr = '({' + names.map(n => `${JSON.stringify(n)}: (typeof ${n} === 'function' ? undefined : ${n})`).join(',') + '})';
    const bag = vm.runInContext(expr, ctx, { filename: filename + '#pick' });
    const out = {};
    for (const n of names) if (bag[n] !== undefined) out[n] = bag[n];
    return out;
}

// 객체 리터럴(`const NAME = {…}`)에서 이름 붙은 칸만 — 없는 칸은 정본이 바뀐 것이라 실패.
function pickFields(ctx, spec) {
    const out = {};
    for (const objName of Object.keys(spec)) {
        // 최상위 `const` 는 컨텍스트 객체가 아니라 스크립트 스코프에 산다 — 이름으로 평가해 꺼낸다(pickTopLevel 과 같은 길).
        let obj = null;
        try { obj = vm.runInContext(`(typeof ${objName} === 'object' ? ${objName} : null)`, ctx, { filename: objName + '#pick' }); } catch (e) { obj = null; }
        if (!obj) throw new Error(`${objName} 이 없다 — 정본의 등록 이름이 바뀌었다`);
        const bag = {};
        for (const f of spec[objName]) {
            if (!(f in obj)) throw new Error(`${objName}.${f} 이 없다 — 정본의 표 칸 이름이 바뀌었다`);
            bag[f] = clean(obj[f], `${objName}.${f}`);
        }
        out[objName] = bag;
    }
    return out;
}

// ── 값 검증 + 복제 (함수·undefined·NaN 은 경로를 찍고 실패) ────────────────────
function clean(v, where) {
    const t = typeof v;
    if (v === null) return null;
    if (t === 'number') {
        if (!Number.isFinite(v)) throw new Error(`${where}: 유한하지 않은 수 ${v}`);
        return v;
    }
    if (t === 'string' || t === 'boolean') return v;
    if (t === 'undefined') throw new Error(`${where}: undefined`);
    if (t === 'function') throw new Error(`${where}: 함수 값 — 표에 함수가 들어 있다(정본에서 고칠 것으로 보고)`);
    if (Array.isArray(v)) {
        const out = new Array(v.length);
        for (let i = 0; i < v.length; i++) {
            if (v[i] === undefined) throw new Error(`${where}[${i}]: undefined(구멍)`);
            out[i] = clean(v[i], `${where}[${i}]`);
        }
        return out;
    }
    if (t === 'object') {
        const out = {};
        for (const k of Object.keys(v)) {
            if (v[k] === undefined) continue;          // 원작이 `rot: o.headRot` 처럼 비운 칸 — JSON.stringify 와 같은 규약으로 뺀다
            out[k] = clean(v[k], `${where}.${k}`);
        }
        return out;
    }
    throw new Error(`${where}: 다룰 수 없는 형 ${t}`);
}

// ── 소품 표본 ──────────────────────────────────────────────────────────────
function xorshift32(seed) {
    let s = seed >>> 0;
    return () => {
        s ^= s << 13; s >>>= 0;
        s ^= s >>> 17;
        s ^= s << 5; s >>>= 0;
        return s / 4294967296;
    };
}

function sampleProps(ctx) {
    const Props = ctx.Props;
    if (!Props) throw new Error('Props 가 없다(mobs-props.js 가 root.Props 를 안 냈다)');
    const kinds = Object.keys(Props).filter(k => typeof Props[k] === 'function' && !PROP_HELPERS.has(k));
    // 컨텍스트 안의 Math.random 을 갈아 끼운다 — 원작 판정기(lib-seed.js)와 같은 방식.
    const M = vm.runInContext('Math', ctx);
    const orig = M.random;
    const samples = {};
    try {
        for (const [label, kind, args] of PROP_SAMPLES) {
            if (typeof Props[kind] !== 'function') throw new Error(`Props.${kind} 이 없다 — 정본이 바뀌었다 (PROP_SAMPLES 를 맞출 것)`);
            M.random = xorshift32(SEED);                 // 표본마다 스트림을 원점으로 되감는다
            const r = Props[kind].apply(Props, args);
            if (!r || !Array.isArray(r.parts)) throw new Error(`Props.${kind}: {u, parts} 꼴이 아니다`);
            const parts = r.parts.map((p, i) => {
                if (!Array.isArray(p.v)) throw new Error(`Props.${kind}.parts[${i}].v 가 배열이 아니다`);
                const v = p.v.map((c, j) => {
                    for (const key of ['x', 'y', 'z']) {
                        if (!Number.isFinite(c[key])) throw new Error(`Props.${kind}.parts[${i}].v[${j}].${key} 가 수가 아니다`);
                    }
                    return c.c === undefined ? [c.x, c.y, c.z] : [c.x, c.y, c.z, c.c];
                });
                return { m: p.m, n: v.length, v };
            });
            const s = { kind, seed: SEED, u: r.u, parts };
            if (r.sway !== undefined) s.sway = r.sway;
            samples[label] = clean(s, `props.${label}`);
        }
    } finally {
        M.random = orig;
    }
    return {
        meta: {
            note: '소품은 정본에서 생성 함수(mobs-props.js Props.*)다. samples 는 Math.random 을 xorshift32(seed) 로 갈아 끼우고 표본마다 되감아 뽑은 결정론 표본 — T9 이식 대조용. v 의 원소 = [x,y,z] 또는 [x,y,z,c]; c 가 없으면 재질(m) 색.',
            rng: 'xorshift32 (wwwww web/tools/lib-seed.js 와 같은 식·같은 시드)',
            seed: SEED,
        },
        kinds,
        samples,
    };
}

// ── 결정론 직렬화 — 객체는 줄마다 키 하나, 원시값만 든 배열은 한 줄 ─────────────
function isPrimitive(v) { return v === null || typeof v !== 'object'; }
function fmt(v, indent) {
    if (isPrimitive(v)) return JSON.stringify(v);
    const pad = '  '.repeat(indent + 1), padEnd = '  '.repeat(indent);
    if (Array.isArray(v)) {
        if (v.length === 0) return '[]';
        if (v.every(isPrimitive)) return '[' + v.map(x => JSON.stringify(x)).join(', ') + ']';
        return '[\n' + v.map(x => pad + fmt(x, indent + 1)).join(',\n') + '\n' + padEnd + ']';
    }
    const keys = Object.keys(v);
    if (keys.length === 0) return '{}';
    // 원시값(또는 원시값 배열)만 든 작은 객체 — 칠 규칙 `{c, x, y, z}` · 관절 `{axis, amp, f}` 같은 것 — 은 한 줄로.
    if (keys.length <= 8 && keys.every(k => isPrimitive(v[k]) || (Array.isArray(v[k]) && v[k].length <= 8 && v[k].every(isPrimitive)))) {
        return '{ ' + keys.map(k => JSON.stringify(k) + ': ' + fmt(v[k], 0)).join(', ') + ' }';
    }
    return '{\n' + keys.map(k => pad + JSON.stringify(k) + ': ' + fmt(v[k], indent + 1)).join(',\n') + '\n' + padEnd + '}';
}
function serialize(v) { return fmt(v, 0) + '\n'; }

// ── 전체 추출 ──────────────────────────────────────────────────────────────
function extract(src) {
    const { ctx, sources } = loadAll(src);
    const need = (name) => {
        const v = ctx[name];
        if (!v || typeof v !== 'object') throw new Error(`${name} 이 없다 — 정본의 등록 이름이 바뀌었다`);
        return v;
    };
    const files = {
        'balance.json': clean(pickTopLevel(ctx, sources['balance-data.js'], 'balance-data.js'), 'balance'),
        'gamedata.json': clean(pickTopLevel(ctx, sources['gamedata.js'], 'gamedata.js'), 'gamedata'),
        'mobs-pets.json': clean(need('PET_MODELS'), 'PET_MODELS'),
        'mobs-mounts.json': clean(need('MOUNT_MODELS'), 'MOUNT_MODELS'),
        'mobs-enemies.json': clean(need('ENEMY_MODELS'), 'ENEMY_MODELS'),
        'mobs-skillfx.json': clean(need('SKILLFX_MODELS'), 'SKILLFX_MODELS'),
        'mobs-props.json': sampleProps(ctx),
        'state.json': extractState(src),
        'tech.json': pickFields(ctx, TECH_FIELDS),
    };
    const text = {};
    for (const k of Object.keys(files)) text[k] = serialize(files[k]);
    return { files, text };
}

function writeOut(text, out) {
    fs.mkdirSync(out, { recursive: true });
    for (const k of Object.keys(text)) fs.writeFileSync(path.join(out, k), text[k], 'utf8');
}

// ── 자기 검사 ──────────────────────────────────────────────────────────────
function selfTest(src) {
    const fails = [];
    const ok = (label, cond, got) => {
        console.log(`  ${cond ? '✓' : '✗'} ${label}${got === undefined ? '' : ' = ' + got}`);
        if (!cond) fails.push(label);
    };
    const { files, text } = extract(src);
    const cnt = (o) => Object.keys(o).length;

    console.log('[조형 표]');
    ok('펫 25', cnt(files['mobs-pets.json']) === 25, cnt(files['mobs-pets.json']));
    ok('탈것 29', cnt(files['mobs-mounts.json']) === 29, cnt(files['mobs-mounts.json']));
    ok('적 7', cnt(files['mobs-enemies.json']) === 7, cnt(files['mobs-enemies.json']));
    ok('스킬 오브젝트 ≥ 18(스킬 18종)', cnt(files['mobs-skillfx.json']) >= 18, cnt(files['mobs-skillfx.json']));
    // 종 머리 키 — 펫·적·스킬 오브젝트는 `cell`(칸 크기)을 들고, 탈것은 `form`(quad/fly/wheeled…)+`seat`(안장 높이)를
    // 들고 칸 크기는 씬 쪽 `MOUNT_FORMS` 가 정한다(원작 mobs-mounts.js · scene3d.js). 그 차이를 그대로 검사한다.
    let parts = 0, bad = 0;
    for (const f of ['mobs-pets.json', 'mobs-mounts.json', 'mobs-enemies.json', 'mobs-skillfx.json']) {
        for (const [name, m] of Object.entries(files[f])) {
            const headOk = f === 'mobs-mounts.json' ? (typeof m.form === 'string' && typeof m.seat === 'number') : (m.cell > 0);
            if (!headOk || !Array.isArray(m.parts) || m.parts.length === 0) { bad++; console.log(`    ✗ ${f} ${name}: ${f === 'mobs-mounts.json' ? 'form/seat' : 'cell'}/parts 없음`); continue; }
            for (const p of m.parts) {
                parts++;
                const okBox = Array.isArray(p.box) && p.box.length === 3 && Array.isArray(p.at) && p.at.length === 3;
                if (!okBox) { bad++; console.log(`    ✗ ${f} ${name}: box/at 가 3원소가 아니다 ${JSON.stringify(p).slice(0, 80)}`); }
            }
        }
    }
    ok('종 머리(펫·적·스킬fx cell>0 · 탈것 form+seat) · 모든 파츠 box[3]·at[3]', bad === 0, `파츠 ${parts}개 · 이상 ${bad}`);
    const props = files['mobs-props.json'];
    ok('소품 생성기 kinds = 표본에 쓴 종류 전부', PROP_SAMPLES.every(s => props.kinds.includes(s[1])) && props.kinds.every(k => PROP_SAMPLES.some(s => s[1] === k)), props.kinds.join(','));
    ok('소품 표본 전부 칸 ≥ 1', Object.values(props.samples).every(s => s.parts.some(p => p.n > 0)), cnt(props.samples));

    console.log('[밸런스]');
    const b = files['balance.json'];
    ok('forgeProbabilities 35행', cnt(b.forgeProbabilities) === 35, cnt(b.forgeProbabilities));
    let worst = 0;
    for (const row of Object.values(b.forgeProbabilities)) {
        const sum = Object.values(row).reduce((a, x) => a + x, 0);
        worst = Math.max(worst, Math.abs(sum - 100));
    }
    // 원작 표는 행 합이 99.95~100.05 로 어긋난 행이 13개 있다(추출원 ForgeMasterCalculator 의 반올림 · 원작은 U.weightedPick 이
    // 합으로 정규화하므로 그대로 쓴다). 여기서는 «표가 통째로 깨지지 않았는가» 만 본다 — 0.06 넘으면 정본이 바뀐 것.
    ok('forgeProbabilities 행 합 ≈ 100 (원작 자체가 ±0.05 · 0.06 넘으면 실패)', worst <= 0.06, worst.toFixed(3));
    ok('forgeUpgrades Lv2~35', cnt(b.forgeUpgrades) === 34 && b.forgeUpgrades[35] && b.forgeUpgrades[2], cnt(b.forgeUpgrades));
    ok('eggDropRates 100칸(1-1~10-10)', cnt(b.eggDropRates) === 100, cnt(b.eggDropRates));
    const petN = Object.values(b.petStats).reduce((a, x) => a + x.length, 0);
    ok('petStats 등급 6 × 합계 25 = 조형 25', cnt(b.petStats) === 6 && petN === 25 && Object.values(b.petStats).flat().every(p => files['mobs-pets.json'][p.name]), `${cnt(b.petStats)}등급 · ${petN}종`);
    ok('mountSummonRates 50레벨', cnt(b.mountSummonRates) === 50, cnt(b.mountSummonRates));
    const mn = Object.values(b.mountNames).reduce((a, x) => a + x.length, 0);
    ok('mountNames 29종 = 조형 29종', mn === 29 && Object.values(b.mountNames).flat().every(n => files['mobs-mounts.json'][n]), mn);
    ok('WINDERS_PER_SUMMON 수', typeof b.WINDERS_PER_SUMMON === 'number', b.WINDERS_PER_SUMMON);

    console.log('[게임 정의]');
    const g = files['gamedata.json'];
    ok('AGES 10', Array.isArray(g.AGES) && g.AGES.length === 10, g.AGES && g.AGES.length);
    ok('RARITIES 6', Array.isArray(g.RARITIES) && g.RARITIES.length === 6, g.RARITIES && g.RARITIES.length);
    ok('SLOTS 8', Array.isArray(g.SLOTS) && g.SLOTS.length === 8, g.SLOTS && g.SLOTS.length);
    ok('SKILL_DEFS 18', Array.isArray(g.SKILL_DEFS) && g.SKILL_DEFS.length === 18, g.SKILL_DEFS && g.SKILL_DEFS.length);
    ok('SKILL_DEFS 의 fx 가 전부 있음', g.SKILL_DEFS.every(s => typeof s.fx === 'string' && s.fx));
    ok('PET_KR 25 = 조형 25', cnt(g.PET_KR) === 25 && Object.keys(g.PET_KR).every(n => files['mobs-pets.json'][n]), cnt(g.PET_KR));
    ok('CHAPTER_THEMES ≥ 10', Array.isArray(g.CHAPTER_THEMES) && g.CHAPTER_THEMES.length >= 10, g.CHAPTER_THEMES && g.CHAPTER_THEMES.length);
    ok('함수 이름은 안 들어감', !('accNames' in g) && !('weaponsOfAge' in g));

    console.log('[세이브 정의]');
    const st = files['state.json'];
    ok('SAVE_KEY 문자열', typeof st.SAVE_KEY === 'string' && st.SAVE_KEY.length > 0, st.SAVE_KEY);
    ok('OFFLINE 셋(캡 초 · 코인/초 · 해머/분) 양수', [st.OFFLINE_CAP_SEC, st.OFFLINE_COIN_PER_SEC, st.OFFLINE_HAMMER_PER_MIN].every(x => typeof x === 'number' && x > 0), `${st.OFFLINE_CAP_SEC}/${st.OFFLINE_COIN_PER_SEC}/${st.OFFLINE_HAMMER_PER_MIN}`);
    ok('CHAPTERS_PER_CYCLE = CHAPTER_THEMES 길이', st.CHAPTERS_PER_CYCLE === g.CHAPTER_THEMES.length, st.CHAPTERS_PER_CYCLE);
    ok('DIFFICULTY_NAMES 길이 = MAX_DIFFICULTY+1 · STAGES_PER_CHAPTER 양수', Array.isArray(st.DIFFICULTY_NAMES) && st.DIFFICULTY_NAMES.length === st.MAX_DIFFICULTY + 1 && st.STAGES_PER_CHAPTER > 0, `${st.DIFFICULTY_NAMES.length}/${st.STAGES_PER_CHAPTER}`);
    ok('STATE_SHAPE_KEYS·STATE_MIN_ONE_KEYS 가 DEFAULT_STATE 의 키', [...st.STATE_SHAPE_KEYS, ...st.STATE_MIN_ONE_KEYS].every(k => k in st.DEFAULT_STATE));
    ok('DEFAULT_STATE 시각 칸 0 · version 1 · 시작 알 1개 · 시작 스킬 강타', st.DEFAULT_STATE.createdAt === 0 && st.DEFAULT_STATE.lastSeen === 0 && st.DEFAULT_STATE.lastOfflineClaim === 0 && st.DEFAULT_STATE.version === 1 && st.DEFAULT_STATE.eggs.length === 1 && st.DEFAULT_STATE.equippedSkills[0] === 'powerStrike', cnt(st.DEFAULT_STATE) + '키');
    ok('DEFAULT_STATE 에 activeMount 접근자가 안 실림(세이브와 같은 규약)', !('activeMount' in st.DEFAULT_STATE));
    ok('MODULE_CAPS 4개 수 · Forge.MAX_LEVEL = forgeProbabilities 행 수', MODULE_CAPS.every(k => typeof st.MODULE_CAPS[k] === 'number') && st.MODULE_CAPS['Forge.MAX_LEVEL'] === cnt(b.forgeProbabilities), JSON.stringify(st.MODULE_CAPS));
    console.log('[기술 트리 · 승천 (T24)]');
    const t = files['tech.json'].TechTree, a = files['tech.json'].Ascension;
    ok('TechTree 분기 3 · 타입 7+10+12 = 29 = NODES 29', t.BRANCHES.length === 3 && t.BRANCHES.map(b => b.types.length).join('+') === '7+10+12' && cnt(t.NODES) === 29, cnt(t.NODES));
    ok('분기 타입이 전부 NODES·BONUS 에 있음', t.BRANCHES.every(b => b.types.every(ty => t.NODES[ty] && t.BONUS[ty])));
    ok('TIERS 5 · MAX_LEVEL 5 · ROMAN 5 · 배수 5개 수', t.TIERS === 5 && t.MAX_LEVEL === 5 && t.ROMAN.length === 5 && [t.LV_MULT, t.TIER_MULT, t.TIME_BASE, t.TIME_LV_MULT, t.TIME_TIER_MULT].every(x => typeof x === 'number'));
    ok('NODES 전부 per·base 수', Object.values(t.NODES).every(n => typeof n.per === 'number' && typeof n.base === 'number'));
    ok('Ascension LINES 4 · LINE_KR/ICON 4 · STAR_MULT·FORGE_LEVEL 수', a.LINES.length === 4 && cnt(a.LINE_KR) === 4 && cnt(a.LINE_ICON) === 4 && typeof a.STAR_MULT === 'number' && typeof a.FORGE_LEVEL === 'number', a.LINES.join(','));

    console.log('[결정론]');
    const again = extract(src).text;
    ok('두 번 뽑아도 바이트 동일', Object.keys(text).every(k => text[k] === again[k]));
    ok('JSON 으로 다시 읽힘', Object.keys(text).every(k => { try { JSON.parse(text[k]); return true; } catch (e) { return false; } }));

    const total = Object.values(text).reduce((a, t) => a + Buffer.byteLength(t), 0);
    console.log(`총 ${Object.keys(text).length}개 파일 · ${(total / 1024).toFixed(0)} KB`);
    if (fails.length) { console.log(`✗ export_data self-test 실패 ${fails.length}건: ${fails.join(' / ')}`); return 1; }
    console.log('✓ export_data self-test 통과');
    return 0;
}

// ── CLI ────────────────────────────────────────────────────────────────────
function main(argv) {
    let src = DEFAULT_SRC, out = DEFAULT_OUT, test = false;
    for (let i = 0; i < argv.length; i++) {
        const a = argv[i];
        if (a === '--src') src = path.resolve(argv[++i]);
        else if (a === '--out') out = path.resolve(argv[++i]);
        else if (a === '--self-test') test = true;
        else if (a === '-h' || a === '--help') { console.log('node tools/export_data.js [--src <wwwww>] [--out <dir>] [--self-test]'); return 0; }
        else { console.error(`모르는 인자: ${a}`); return 2; }
    }
    if (test) return selfTest(src);
    const { text } = extract(src);
    writeOut(text, out);
    for (const k of Object.keys(text)) console.log(`  ${k}  ${(Buffer.byteLength(text[k]) / 1024).toFixed(1)} KB`);
    console.log(`✓ ${Object.keys(text).length}개 파일 → ${path.relative(ROOT, out) || '.'}`);
    return 0;
}

if (require.main === module) process.exit(main(process.argv.slice(2)));
module.exports = { extract, serialize, selfTest, LOAD_ORDER, STATE_LOAD_ORDER, SEED };
