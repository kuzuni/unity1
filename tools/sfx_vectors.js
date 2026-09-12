#!/usr/bin/env node
// ============================================================================
// sfx_vectors.js — 정본 `web/js/sfx.js` 를 node vm 에 **그대로** 올리고, WebAudio 대신 «기록하는 가짜 AudioContext» 를
// 물려 효과음 24종·음악 4모드가 만드는 합성 그래프(오실레이터/노이즈 · 주파수·게인 자동화 · 필터 · 센드 · 시작/정지 시각)를
// 벡터로 찍는다 (T30). C# `SfxRecipes`·`MusicSequencer`(Assets/Scripts/Core/Audio) 가 같은 시드로 같은 그래프를 내는지
// `AudioTests` 가 이 파일과 대조한다 — «비슷하게 새로 만들기» 가 아니라 «그대로 옮기기» 의 증거.
//
//   node tools/sfx_vectors.js                 # → Assets/Tests/EditMode/Vectors/t30-sfx.json
//   node tools/sfx_vectors.js --self-test     # 쓰지 않고 개수·결정론만 검사
//
// 난수: 원작은 `Math.random` 으로 지터(tone/thump)·노이즈 버퍼(noiseBurst/_musicHat)를 만든다 → 컨텍스트의 Math.random 을
// mulberry32(C# `Forge.Core.Data.Mulberry32` 와 같은 식)로 바꾸고 사례마다 시드를 되감는다. IR(`_makeIR`)은 `ensure()` 한 번에
// 만들어지므로 되감기 전에 끝난다. 노이즈 버퍼는 샘플 수 n 과 합(sum)으로 남긴다 — C# 이 같은 순서로 같은 난수를 쓰면 합이 같다.
// ============================================================================
'use strict';
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const ROOT = path.resolve(__dirname, '..');
const DEFAULT_SRC = path.join(ROOT, '.wwwww-src');
const DEFAULT_OUT = path.join(ROOT, 'Assets', 'Tests', 'EditMode', 'Vectors', 't30-sfx.json');
const SAMPLE_RATE = 44100;

// 효과음 사례 — 원작 호출부(scene3d.js · scene3d-skillfx.js · ui.js)가 실제로 넘기는 인자 꼴. tier 는 0~5(scene3d-skillfx 가 clamp).
const SFX_CASES = [
    ['hit', [false]], ['hit', [true]],
    ['bossSiren', []],
    ['anvilHit', [false]], ['anvilHit', [true]],
    ['stormRumble', []], ['stormRumble', [0.5]], ['stormRumble', [0.28]],
    ['stormCrackle', []],
    ['stormStrike', [0]], ['stormStrike', [1]], ['stormStrike', [2]], ['stormStrike', [3]], ['stormStrike', [4]], ['stormStrike', [6]],
    ['slashArc', [0, 0]], ['slashArc', [2, 3]], ['slashArc', [4, 5]],
    ['arrowShot', [0, 0]], ['arrowShot', [3, 2]], ['arrowShot', [7, 5]],
    ['mawRoar', [0]], ['mawRoar', [5]],
    ['mawBite', [0]], ['mawBite', [5]],
    ['healDescend', [0]], ['healDescend', [5]],
    ['auraRise', [0]], ['auraRise', [5]],
    ['voidTear', [0]], ['voidTear', [5]],
    ['voidPierce', [0]], ['voidPierce', [5]],
    ['voidSnap', []],
    ['equipToss', []], ['equipSnap', []], ['equipDrop', []],
    ['craft', []],
    ['craftReveal', [0]], ['craftReveal', [4]], ['craftReveal', [9]],
    ['levelUp', []],
    ['gacha', ['common']], ['gacha', ['legendary']],
    ['summonCharge', ['common']], ['summonCharge', ['mythic']],
    ['summonReveal', ['common']], ['summonReveal', ['legendary']], ['summonReveal', ['mythic']],
];
const EFFECT_NAMES = ['hit', 'bossSiren', 'anvilHit', 'stormRumble', 'stormCrackle', 'stormStrike', 'slashArc', 'arrowShot',
    'mawRoar', 'mawBite', 'healDescend', 'auraRise', 'voidTear', 'voidPierce', 'voidSnap', 'equipToss', 'equipSnap', 'equipDrop',
    'craft', 'craftReveal', 'levelUp', 'gacha', 'summonCharge', 'summonReveal'];
const MODES = ['normal', 'boss', 'dungeon', 'shop'];

// ── 기록하는 가짜 AudioContext ──────────────────────────────────────────────
function makeFakeCtx(sr) {
    const nodes = [];
    function param(init) {
        const p = { _v: init, ev: [] };
        Object.defineProperty(p, 'value', { get() { return p._v; }, set(x) { p._v = x; p.ev.push(['v', x]); } });
        p.setValueAtTime = (v, t) => { p.ev.push(['s', v, t]); };
        p.exponentialRampToValueAtTime = (v, t) => { p.ev.push(['e', v, t]); };
        p.linearRampToValueAtTime = (v, t) => { p.ev.push(['l', v, t]); };
        return p;
    }
    function node(kind, extra) {
        const n = Object.assign({ kind, out: [], connect(d) { n.out.push(d); return d; } }, extra);
        n.id = nodes.length;
        nodes.push(n);
        return n;
    }
    const ctx = {
        currentTime: 0, sampleRate: sr, state: 'running',
        destination: node('dest', {}),
        createGain() { return node('gain', { gain: param(1) }); },
        createOscillator() { return node('osc', { type: 'sine', frequency: param(440), t0: null, t1: null, start(t) { this.t0 = t; }, stop(t) { this.t1 = t; } }); },
        createBiquadFilter() { return node('biquad', { type: 'lowpass', frequency: param(350), Q: param(1) }); },
        createBuffer(ch, n, rate) { const d = new Float32Array(n); return { length: n, sampleRate: rate, numberOfChannels: ch, getChannelData() { return d; } }; },
        createBufferSource() { return node('bufsrc', { buffer: null, t0: null, start(t) { this.t0 = t; } }); },
        createDynamicsCompressor() { return node('comp', { threshold: param(-24), knee: param(30), ratio: param(12), attack: param(0.003), release: param(0.25) }); },
        createConvolver() { return node('conv', { buffer: null }); },
        createDelay(max) { return node('delay', { maxDelay: max, delayTime: param(0) }); },
        resume() {},
    };
    return { ctx, nodes };
}

// ── 정본 로드 ─────────────────────────────────────────────────────────────
function load(src) {
    const webjs = path.join(src, 'web', 'js');
    if (!fs.existsSync(path.join(webjs, 'sfx.js'))) throw new Error(`정본이 없다: ${webjs}/sfx.js (git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)`);
    const sandbox = { console };
    sandbox.window = sandbox;
    vm.createContext(sandbox);
    // C# Mulberry32 와 같은 식 — 컨텍스트 안에 심어 sfx.js 의 Math.random 호출이 이것을 쓰게 한다.
    vm.runInContext(`
        var __seed = 1;
        Math.random = function () {
            __seed = (__seed + 0x6D2B79F5) | 0;
            var t = Math.imul(__seed ^ (__seed >>> 15), 1 | __seed);
            t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
            return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
        };
        function __reseed(s) { __seed = s | 0; }
    `, sandbox, { filename: 'rng.js' });
    for (const f of ['balance-data.js', 'gamedata.js', 'sfx.js']) {
        vm.runInContext(fs.readFileSync(path.join(webjs, f), 'utf8'), sandbox, { filename: f });
    }
    vm.runInContext('var S = { sfxOn: true, musicOn: true }; var saveGame = function () {};', sandbox, { filename: 'stubs.js' });
    const fake = makeFakeCtx(SAMPLE_RATE);
    sandbox.window.AudioContext = function () { return fake.ctx; };
    const SFX = vm.runInContext('SFX', sandbox);
    SFX.ensure();                       // 버스·리미터·IR(난수 소비)을 여기서 끝낸다
    if (!SFX.ctx) throw new Error('SFX.ensure() 가 컨텍스트를 못 만들었다');
    // 음악 버스도 startMusic 이 만드는 그대로(타이머만 빼고) — _musicScheduleStep 이 센드 노드를 찾는다
    vm.runInContext(`
        (function () {
            const ctx = SFX.ctx;
            SFX.musicGain = ctx.createGain(); SFX.musicGain.gain.value = 0.16; SFX.musicGain.connect(SFX.comp);
            SFX._musicDelay = ctx.createDelay(1.5);
            const fb = ctx.createGain(); fb.gain.value = 0.3;
            const fbLp = ctx.createBiquadFilter(); fbLp.type = 'lowpass'; fbLp.frequency.value = 2400;
            SFX._musicDelay.connect(fb); fb.connect(fbLp); fbLp.connect(SFX._musicDelay);
            const wet = ctx.createGain(); wet.gain.value = 0.4;
            SFX._musicDelay.connect(wet); wet.connect(SFX.musicGain);
            SFX._musicDelaySend = ctx.createGain(); SFX._musicDelaySend.gain.value = 1; SFX._musicDelaySend.connect(SFX._musicDelay);
            SFX._musicRvbSend = ctx.createGain(); SFX._musicRvbSend.gain.value = 1; SFX._musicRvbSend.connect(SFX._rvbIn);
            SFX._shopOpen = function () { return false; };
        })();
    `, sandbox, { filename: 'music-bus.js' });
    const reseed = (s) => vm.runInContext(`__reseed(${s})`, sandbox);
    return { sandbox, SFX, fake, reseed };
}

// ── 노드 그래프 → 보이스 기록 ───────────────────────────────────────────────
const r6 = (x) => (typeof x === 'number' ? Number(x.toPrecision(9)) : x);
const evs = (p) => p.ev.map(e => e.length === 3 ? [e[0], r6(e[1]), r6(e[2])] : [e[0], r6(e[1])]);

function collect(SFX, nodes, from) {
    const voices = [];
    for (let i = from; i < nodes.length; i++) {
        const n = nodes[i];
        if (n.kind !== 'osc' && n.kind !== 'bufsrc') continue;
        let head = n, filt = null;
        if (head.out.length !== 1) throw new Error(`${n.kind}#${n.id}: 출력이 ${head.out.length}개`);
        if (head.out[0].kind === 'biquad') { filt = head.out[0]; head = filt; }
        const env = head.out[0];
        if (!env || env.kind !== 'gain') throw new Error(`${n.kind}#${n.id}: 엔벨로프 게인이 없다`);
        let bus = null, rvb = 0, dly = 0;
        for (const d of env.out) {
            if (d === SFX.master) bus = 'sfx';
            else if (d === SFX.musicGain) bus = 'music';
            else if (d.kind === 'gain' && d.out.length === 1) {
                const tgt = d.out[0];
                if (tgt === SFX._rvbIn || tgt === SFX._musicRvbSend) rvb = d.gain.value;
                else if (tgt === SFX._musicDelaySend) dly = d.gain.value;
                else throw new Error(`${n.kind}#${n.id}: 모르는 센드 대상 ${tgt.kind}#${tgt.id}`);
            } else throw new Error(`${n.kind}#${n.id}: 모르는 출력 ${d.kind}#${d.id}`);
        }
        if (!bus) throw new Error(`${n.kind}#${n.id}: 버스에 안 닿는다`);
        const v = { k: n.kind === 'osc' ? 'osc' : 'noise', bus, t0: r6(n.t0) };
        if (n.kind === 'osc') {
            v.type = n.type; v.t1 = r6(n.t1); v.f = evs(n.frequency);
            if (filt) { if (filt.type !== 'lowpass') throw new Error('osc 필터는 lowpass 뿐'); v.lp = r6(filt.frequency.value); }
        } else {
            const d = n.buffer.getChannelData(0);
            let sum = 0;
            for (let j = 0; j < d.length; j++) sum += d[j];
            v.n = d.length; v.sum = Number(sum.toFixed(6)); v.t1 = r6(n.t0 + d.length / SAMPLE_RATE);
            if (!filt) throw new Error('noise 는 항상 필터를 지난다');
            v.filt = { type: filt.type, f: evs(filt.frequency), Q: r6(filt.Q.value) };
        }
        v.g = evs(env.gain);
        if (rvb) v.rvb = r6(rvb);
        if (dly) v.dly = r6(dly);
        voices.push(v);
    }
    return voices;
}

function extract(src) {
    const { sandbox, SFX, fake, reseed } = load(src);
    const out = { sampleRate: SAMPLE_RATE, effects: EFFECT_NAMES, sfx: [], music: [] };
    SFX_CASES.forEach(([name, args], idx) => {
        const seed = 1000 + idx;
        reseed(seed);
        const from = fake.nodes.length;
        SFX[name](...args);
        out.sfx.push({ name, args, seed, voices: collect(SFX, fake.nodes, from) });
    });
    const LOOP = vm.runInContext('MUSIC_LOOP_STEPS', sandbox);
    MODES.forEach((mode, mi) => {
        const seed = 5000 + mi;
        reseed(seed);
        SFX.musicMode = mode; SFX._combatMode = mode;
        SFX._applyModeTiming(mode);
        const stepDur = SFX._musicStepDur;
        const from = fake.nodes.length;
        for (let step = 0; step < LOOP; step++) SFX._musicScheduleStep(step, step * stepDur);
        out.music.push({ mode, seed, stepDur: r6(stepDur), steps: LOOP, voices: collect(SFX, fake.nodes, from) });
    });
    return out;
}

function serialize(o) {
    // 보이스 한 줄씩 — 사람이 diff 로 읽을 수 있게
    const lines = ['{', `  "sampleRate": ${o.sampleRate},`, `  "effects": ${JSON.stringify(o.effects)},`, '  "sfx": ['];
    o.sfx.forEach((c, i) => {
        lines.push(`    { "name": ${JSON.stringify(c.name)}, "args": ${JSON.stringify(c.args)}, "seed": ${c.seed}, "voices": [`);
        c.voices.forEach((v, j) => lines.push('      ' + JSON.stringify(v) + (j < c.voices.length - 1 ? ',' : '')));
        lines.push('    ] }' + (i < o.sfx.length - 1 ? ',' : ''));
    });
    lines.push('  ],', '  "music": [');
    o.music.forEach((c, i) => {
        lines.push(`    { "mode": ${JSON.stringify(c.mode)}, "seed": ${c.seed}, "stepDur": ${c.stepDur}, "steps": ${c.steps}, "voices": [`);
        c.voices.forEach((v, j) => lines.push('      ' + JSON.stringify(v) + (j < c.voices.length - 1 ? ',' : '')));
        lines.push('    ] }' + (i < o.music.length - 1 ? ',' : ''));
    });
    lines.push('  ]', '}', '');
    return lines.join('\n');
}

function selfTest(src) {
    const a = extract(src), b = extract(src);
    const fails = [];
    const ok = (label, cond, got) => { console.log(`  ${cond ? '✓' : '✗'} ${label}${got === undefined ? '' : ' = ' + got}`); if (!cond) fails.push(label); };
    ok('효과음 사례 ' + SFX_CASES.length + ' · 이름 24종 전부 한 번 이상', a.sfx.length === SFX_CASES.length && EFFECT_NAMES.every(n => a.sfx.some(c => c.name === n)), a.sfx.length);
    ok('사례마다 보이스 ≥ 1', a.sfx.every(c => c.voices.length >= 1));
    ok('모든 보이스가 sfx 버스', a.sfx.every(c => c.voices.every(v => v.bus === 'sfx')));
    ok('음악 4모드 × 128스텝 · 보이스 ≥ 100', a.music.length === 4 && a.music.every(m => m.steps === 128 && m.voices.length >= 100), a.music.map(m => m.voices.length).join('/'));
    ok('음악 보이스 전부 music 버스 · 킥은 boss/dungeon 에만', a.music.every(m => m.voices.every(v => v.bus === 'music')) &&
        a.music.filter(m => m.voices.some(v => v.k === 'osc' && v.f.some(e => e[0] === 'e') && v.t1 - v.t0 === 0.32)).map(m => m.mode).join(',') === 'boss,dungeon');
    ok('결정론(두 번 같음)', serialize(a) === serialize(b));
    ok('노이즈 합이 0 이 아님(난수가 실제로 돌았다)', a.sfx.some(c => c.voices.some(v => v.k === 'noise' && v.sum !== 0)));
    if (fails.length) { console.log(`✗ sfx_vectors self-test 실패 ${fails.length}건`); return 1; }
    console.log('✓ sfx_vectors self-test 통과');
    return 0;
}

function main(argv) {
    let src = DEFAULT_SRC, out = DEFAULT_OUT, test = false;
    for (let i = 0; i < argv.length; i++) {
        const a = argv[i];
        if (a === '--src') src = path.resolve(argv[++i]);
        else if (a === '--out') out = path.resolve(argv[++i]);
        else if (a === '--self-test') test = true;
        else { console.error(`모르는 인자: ${a}`); return 2; }
    }
    if (test) return selfTest(src);
    const text = serialize(extract(src));
    fs.mkdirSync(path.dirname(out), { recursive: true });
    fs.writeFileSync(out, text, 'utf8');
    console.log(`✓ ${path.relative(ROOT, out)}  ${(Buffer.byteLength(text) / 1024).toFixed(1)} KB`);
    return 0;
}

if (require.main === module) process.exit(main(process.argv.slice(2)));
module.exports = { extract, serialize, SFX_CASES, EFFECT_NAMES, MODES, SAMPLE_RATE };
