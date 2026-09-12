#!/usr/bin/env node
// ============================================================================
// dungeon_vectors.js — 정본 `web/js/dungeons.js` 를 **그대로** 돌려 대조 벡터를 뽑는다 (T23).
// ----------------------------------------------------------------------------
// Core `Dungeons`(C#)가 원작 `Dungeons` 와 «같은 입력·같은 시드 → 같은 상태·같은 호출» 인지 EditMode `DungeonTests` 가
// 이 파일의 출력(`Assets/Tests/EditMode/Vectors/t23-dungeons.json`)과 대조한다. 원작 파일(util·bignum·dungeons)을 vm 컨텍스트에
// 그대로 싣고, 원작이 밖에서 받는 것(S · UI · TechTree · Quests · Combat · saveGame · bestRank · Date.now · Math.random)만
// stub 으로 꽂는다 — 규칙은 한 줄도 다시 쓰지 않는다. 난수 = mulberry32(Core `Mulberry32` 와 같다) · 달력 = UTC(TZ 고정).
//
// 사용:
//   node tools/dungeon_vectors.js                 # .wwwww-src → Assets/Tests/EditMode/Vectors/t23-dungeons.json
//   node tools/dungeon_vectors.js --check         # 다시 뽑아 현재 파일과 같은지만 본다 (rc 1 = 다르다)
//   node tools/dungeon_vectors.js --src <wwwww 체크아웃> --out <파일>
// ============================================================================
'use strict';
process.env.TZ = 'UTC';   // toDateString 이 로컬 달력을 쓴다 — 벡터는 UTC 로 고정(C# 테스트도 UTC 를 준다)
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const ROOT = path.resolve(__dirname, '..');
const DEFAULT_SRC = path.join(ROOT, '.wwwww-src');
const DEFAULT_OUT = path.join(ROOT, 'Assets', 'Tests', 'EditMode', 'Vectors', 't23-dungeons.json');
const LOAD_ORDER = ['util.js', 'bignum.js', 'dungeons.js'];

function mulberry32(seed) {
    let a = seed >>> 0;
    return function () {
        a |= 0; a = a + 0x6D2B79F5 | 0;
        let t = Math.imul(a ^ a >>> 15, 1 | a);
        t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
        return ((t ^ t >>> 14) >>> 0) / 4294967296;
    };
}

// 원작을 올린 컨텍스트 하나 — 스텁은 모두 sandbox 프로퍼티(스크립트에서 자유 식별자로 보인다).
function makeWorld(src) {
    const sandbox = { console };
    sandbox.window = sandbox;
    sandbox.Math = Object.create(Math);
    sandbox.clock = { now: 0 };
    sandbox.Date = class extends Date { static now() { return sandbox.clock.now; } };
    vm.createContext(sandbox);
    for (const f of LOAD_ORDER) {
        const p = path.join(src, 'web', 'js', f);
        vm.runInContext(fs.readFileSync(p, 'utf8'), sandbox, { filename: f });
    }
    const w = Object.assign(sandbox, vm.runInContext('({ U, Dungeons, Big })', sandbox));
    w.log = fresh();
    w.S = null;
    w.host = { bestRank: 0, tech: { thiefHammer: 0, thiefCoin: 0, dungeonTicket: 0, dungeonPotion: 0 } };
    w.bestRank = () => w.host.bestRank;
    w.TechTree = {
        pct: k => w.host.tech[k] || 0,
        thiefHammerMult() { return 1 + this.pct('thiefHammer') / 100; },
        thiefCoinMult() { return 1 + this.pct('thiefCoin') / 100; },
        dungeonTicketMult() { return 1 + this.pct('dungeonTicket') / 100; },
        dungeonPotionMult() { return 1 + this.pct('dungeonPotion') / 100; },
    };
    w.Quests = { bump: k => w.log.quests.push(k) };
    w.saveGame = () => { w.log.saves++; };
    w.Combat = { hero: { hp: 1, maxHp: 10 }, setupStage() { w.log.setup++; w.log.heroHp = this.hero.hp === this.hero.maxHp; } };
    // UI: 원작은 팝업이 «열려 있을 때만» 다시 그린다 — 여기서는 열려 있다고 보고 호출을 이벤트로 남긴다(C# 은 항상 알리고 UI 가 가른다).
    const open = { classList: { contains: () => false } };
    w.UI = {
        els: { dungeonModal: open, dungeonDetailModal: open },
        toast: m => w.log.toasts.push(m),
        openDungeons() { w.log.ui.push('openDungeons'); },
        renderDungeonDetail() { w.log.ui.push('renderDungeonDetail'); },
        rewardBurst(r) { w.log.ui.push('rewardBurst:' + JSON.stringify(r)); },
        renderTopBar() { w.log.ui.push('renderTopBar'); },
        renderPets() { w.log.ui.push('renderPets'); },
        showDungeonClear(def, stage, r) { w.log.ui.push('showDungeonClear:' + def.id + ':' + stage + ':' + JSON.stringify(r)); },
    };
    return w;
}
function fresh() { return { toasts: [], quests: [], ui: [], saves: 0, setup: 0, heroHp: false }; }

// 키 정렬 직렬화 — C# 쪽도 같은 규약으로 만들어 글자 비교한다.
function canon(v) {
    if (v === null || typeof v !== 'object') return JSON.stringify(v === undefined ? null : v);
    if (Array.isArray(v)) return '[' + v.map(canon).join(',') + ']';
    return '{' + Object.keys(v).sort().map(k => JSON.stringify(k) + ':' + canon(v[k])).join(',') + '}';
}

const IDS = ['hammer', 'ghost', 'invasion', 'zombie'];
const T0 = Date.UTC(2026, 8, 12, 10, 0, 0);            // 2026-09-12T10:00:00Z → 키 «Sat Sep 12 2026»
const defaultS = () => ({ hammers: 80, coins: 500, gems: 0, tickets: 40, potions: 0, eggCurrency: 0, dungeonRun: null });

function build(src) {
    const w = makeWorld(src);
    const D = w.Dungeons;
    const out = { defs: D.DEFS, consts: { MAX_KEYS: D.MAX_KEYS, MIN_WAVES: D.MIN_WAVES, MAX_WAVES: D.MAX_WAVES, DEFAULT_WAVES: D.DEFAULT_WAVES, BASE_REWARD: D.BASE_REWARD, PER_STAGE: D.PER_STAGE } };

    // ── 순수 계산 ──
    out.rewardAmount = [-3, 0, 1, 2, 3, 10, 17, 50, 100, 999].map(s => [s, D.rewardAmount(s)]);
    const TECH = [[0, 0, 0, 0], [25, 10, 15, 40], [7, 3, 33, 99], [100, 250, 1, 0]];
    out.rewards = [];
    for (const t of TECH) {
        w.host.tech = { thiefHammer: t[0], thiefCoin: t[1], dungeonTicket: t[2], dungeonPotion: t[3] };
        for (const id of IDS) for (const st of [1, 2, 3, 5, 10, 17, 33, 50, 120])
            out.rewards.push({ id, stage: st, tech: t, r: D.rewards(id, st), text: D.rewardText(id, st), textSp: D.rewardText(id, st, ' ') });
    }
    w.host.tech = { thiefHammer: 0, thiefCoin: 0, dungeonTicket: 0, dungeonPotion: 0 };
    out.monsterHp = [];
    for (const id of IDS) for (let st = 1; st <= 12; st++) out.monsterHp.push([id, st, D.monsterHp(id, st)]);
    out.unlocked = [];
    for (const rank of [0, 100, 207, 208, 209, 210, 211, 300, 301, 302, 400, 401, 2510, 2600 * 100]) {
        w.host.bestRank = rank;
        const u = {}; for (const id of IDS) u[id] = D.unlocked(id);
        out.unlocked.push([rank, u]);
    }
    out.dateKey = [];
    for (const ms of [0, 1, 9 * 3600 * 1000 - 1, 9 * 3600 * 1000, T0, Date.UTC(2026, 8, 13, 8, 59, 59, 999), Date.UTC(2026, 8, 13, 9, 0, 0), Date.UTC(2026, 11, 31, 23, 59, 59), Date.UTC(2027, 0, 1, 8, 59, 59), Date.UTC(2027, 0, 1, 9, 0, 0), Date.UTC(2028, 1, 29, 12, 0, 0), 1800000000000.7]) {
        w.clock.now = ms; out.dateKey.push([ms, D.resetDateKey()]);
    }

    // ── 흐름(시드 · 시각 · 최고 기록 · 배율 · 초기 S · 연산 나열) ──
    const flows = [
        { name: '기본 입장·클리어·열쇠 소진', seed: 1, bestRank: 301, ops: [['ensure'], ['enter', 'hammer'], ['onClear'], ['enter', 'hammer', 5], ['onClear'], ['enter', 'hammer'], ['enter', 'hammer', 1], ['sweep', 'hammer']] },
        { name: '실패는 열쇠를 안 쓴다', seed: 2, bestRank: 301, ops: [['enter', 'ghost'], ['onFail'], ['enter', 'ghost', 3], ['onFail'], ['enter', 'ghost'], ['onClear'], ['enter', 'ghost', 2], ['onClear'], ['enter', 'ghost']] },
        { name: '해금 좌표', seed: 3, bestRank: 209, ops: [['enter', 'hammer'], ['enter', 'ghost'], ['enter', 'invasion'], ['sweep', 'zombie'], ['onFail'], ['bestRank', 210], ['enter', 'hammer'], ['enter', 'zombie'], ['onClear'], ['bestRank', 401], ['enter', 'zombie'], ['onClear'], ['enter', 'invasion'], ['onClear']] },
        { name: '소탕', seed: 4, bestRank: 2510, ops: [['sweep', 'invasion'], ['enter', 'invasion'], ['sweep', 'invasion'], ['onClear'], ['sweep', 'invasion'], ['sweep', 'invasion'], ['enter', 'invasion'], ['enter', 'zombie'], ['onClear'], ['sweep', 'zombie'], ['sweep', 'zombie']] },
        { name: '기술트리 배율', seed: 5, bestRank: 2510, tech: { thiefHammer: 25, thiefCoin: 10, dungeonTicket: 15, dungeonPotion: 40 }, ops: [['enter', 'hammer'], ['onClear'], ['enter', 'ghost'], ['onClear'], ['enter', 'invasion'], ['onClear'], ['enter', 'zombie'], ['onClear'], ['sweep', 'hammer'], ['sweep', 'ghost'], ['sweep', 'invasion'], ['sweep', 'zombie']] },
        { name: '09:00 리셋', seed: 6, bestRank: 2510, ops: [['ensure'], ['enter', 'hammer'], ['onClear'], ['sweep', 'hammer'], ['now', Date.UTC(2026, 8, 13, 8, 59, 59)], ['ensure'], ['sweep', 'hammer'], ['now', Date.UTC(2026, 8, 13, 9, 0, 0)], ['ensure'], ['sweep', 'hammer'], ['ensure'], ['sweep', 'hammer']] },
        { name: '복원 — 정상', seed: 7, bestRank: 2510, S: { dungeons: { keys: { invasion: 1 }, best: { invasion: 1 }, lastReset: 'Sat Sep 12 2026' }, dungeonRun: { id: 'invasion', stage: 2, waves: 2 } }, ops: [['restoreRun'], ['onClear'], ['restoreRun']] },
        { name: '복원 — 단계가 최고+1 초과', seed: 8, bestRank: 2510, S: { dungeons: { best: { invasion: 1 } }, dungeonRun: { id: 'invasion', stage: 3, waves: 2 } }, ops: [['restoreRun'], ['enter', 'invasion']] },
        { name: '복원 — 문자열 단계는 수로 읽힌다', seed: 9, bestRank: 2510, S: { dungeons: { best: { ghost: 4 } }, dungeonRun: { id: 'ghost', stage: '2', waves: 'x' } }, ops: [['restoreRun']] },
        { name: '복원 — 웨이브 범위·소수', seed: 10, bestRank: 2510, S: { dungeons: { best: { zombie: 0 } }, dungeonRun: { id: 'zombie', stage: 1.9, waves: 7 } }, ops: [['restoreRun'], ['onFail'], ['put', 'dungeonRun', { id: 'zombie', stage: 1, waves: 2.7 }], ['restoreRun'], ['onFail'], ['put', 'dungeonRun', { id: 'zombie', stage: 1, waves: 0 }], ['restoreRun'], ['onFail'], ['put', 'dungeonRun', { id: 'zombie', stage: 1 }], ['restoreRun']] },
        { name: '복원 — 손상 형태', seed: 11, bestRank: 2510, ops: [['put', 'dungeonRun', [1, 2]], ['restoreRun'], ['put', 'dungeonRun', 5], ['restoreRun'], ['put', 'dungeonRun', 'x'], ['restoreRun'], ['put', 'dungeonRun', { id: 'nope', stage: 1 }], ['restoreRun'], ['put', 'dungeonRun', { id: 'hammer' }], ['restoreRun'], ['put', 'dungeonRun', { id: 'hammer', stage: 0 }], ['restoreRun'], ['put', 'dungeonRun', { id: 'hammer', stage: null }], ['restoreRun'], ['put', 'dungeonRun', {}], ['restoreRun'], ['put', 'dungeonRun', false], ['restoreRun']] },
        { name: 'ensure — 손상 슬롯', seed: 12, bestRank: 2510, S: { dungeons: [] }, ops: [['ensure'], ['put', 'dungeons', null], ['ensure'], ['put', 'dungeons', 'x'], ['ensure'], ['put', 'dungeons', { keys: null, best: [1], lastReset: 5 }], ['ensure'], ['put', 'dungeons', { keys: { ghost: 1, extra: 9 }, best: { hammer: 3 }, lastReset: 'Sat Sep 12 2026' }], ['ensure'], ['put', 'dungeons', { keys: { ghost: 1 }, best: { hammer: 3 }, lastReset: 'Fri Sep 11 2026' }], ['ensure']] },
        { name: 'ensure — 물약 칸', seed: 13, bestRank: 2510, S: { potions: undefined }, ops: [['ensure'], ['put', 'potions', 7], ['ensure']] },
        { name: '진행 중 입장·소탕 거부', seed: 14, bestRank: 2510, ops: [['enter', 'zombie'], ['enter', 'hammer'], ['sweep', 'zombie'], ['restoreRun'], ['onClear'], ['enter', 'zombie', 9]] },
    ];
    out.flows = [];
    for (const f of flows) {
        w.Math.random = mulberry32(f.seed);
        w.clock.now = f.now || T0;
        w.host.bestRank = f.bestRank;
        w.host.tech = Object.assign({ thiefHammer: 0, thiefCoin: 0, dungeonTicket: 0, dungeonPotion: 0 }, f.tech || {});
        const S0 = Object.assign(defaultS(), f.S || {});
        if (f.S && 'potions' in f.S && f.S.potions === undefined) delete S0.potions;
        w.S = JSON.parse(JSON.stringify(S0));
        w.Combat.hero.hp = 1;
        const steps = [];
        for (const op of f.ops) {
            w.log = fresh();
            let ret;
            switch (op[0]) {
                case 'ensure': ret = D.ensure(); break;
                case 'enter': ret = D.enter(op[1], op[2]); break;
                case 'sweep': ret = D.sweep(op[1]); break;
                case 'onClear': ret = D.onClear(); break;
                case 'onFail': ret = D.onFail(); break;
                case 'restoreRun': ret = D.restoreRun(); break;
                case 'now': w.clock.now = op[1]; break;
                case 'bestRank': w.host.bestRank = op[1]; break;
                case 'put': w.S[op[1]] = JSON.parse(JSON.stringify(op[2])); break;   // 깊은 복제 — ensure 가 제자리에서 고치므로 op 기록이 «넣은 값» 으로 남게
                default: throw new Error('op? ' + op[0]);
            }
            if (op[0] === 'onClear' || op[0] === 'onFail') w.Combat.hero.hp = 1;   // 다음 입장이 hp 를 채우는지 다시 본다
            steps.push({ op, ret: ret === undefined ? null : ret, S: canon(w.S), log: w.log });
        }
        out.flows.push({ name: f.name, seed: f.seed, now: f.now || T0, bestRank: f.bestRank, tech: w.host.tech, S0: canon(S0), steps });
    }
    return out;
}

function selfCheck(out) {
    if (out.defs.length !== 4) throw new Error('DEFS 4 아님');
    if (out.rewards.length !== 4 * 4 * 9) throw new Error('rewards 수');
    if (out.flows.length !== 14) throw new Error('flows 수');
    const clears = out.flows.flatMap(f => f.steps).filter(s => s.op[0] === 'onClear').length;
    if (clears < 10) throw new Error('onClear 표본 부족');
    for (const f of out.flows) for (const s of f.steps) if (s.log.toasts.some(t => t.includes('undefined') || t.includes('NaN'))) throw new Error('토스트에 undefined/NaN: ' + f.name);
}

function main() {
    const argv = process.argv.slice(2);
    const get = k => { const i = argv.indexOf(k); return i >= 0 ? argv[i + 1] : null; };
    const src = get('--src') || DEFAULT_SRC;
    const outPath = get('--out') || DEFAULT_OUT;
    if (!fs.existsSync(path.join(src, 'web', 'js', 'dungeons.js'))) { console.error('정본이 없다: ' + src + ' — git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src'); process.exit(2); }
    const out = build(src);
    selfCheck(out);
    const text = JSON.stringify(out, null, 0) + '\n';
    if (argv.includes('--check')) {
        const cur = fs.existsSync(outPath) ? fs.readFileSync(outPath, 'utf8') : '';
        if (cur !== text) { console.error('✗ dungeon_vectors: ' + path.relative(ROOT, outPath) + ' 이 정본과 다르다 — node tools/dungeon_vectors.js 로 다시 뽑는다'); process.exit(1); }
        console.log('✓ dungeon_vectors: 정본과 같다'); return;
    }
    fs.mkdirSync(path.dirname(outPath), { recursive: true });
    fs.writeFileSync(outPath, text);
    console.log('✓ ' + path.relative(ROOT, outPath) + ' — flows ' + out.flows.length + ' · rewards ' + out.rewards.length + ' · ' + text.length + ' bytes');
}
main();
