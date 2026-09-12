#!/usr/bin/env node
// ============================================================================
// mount_vectors.js — 정본 `web/js/mounts.js` 를 **그대로** 돌려 대조 벡터를 뽑는다 (T40 · T16 `pet_vectors.js` 방식).
// ----------------------------------------------------------------------------
// Core `MountSystem`(C#)이 원작 `Mounts` 와 «같은 시드 → 같은 결과» 인지 EditMode `MountTests` 가 이 파일의 출력
// (`Assets/Tests/EditMode/Vectors/mount_vectors.json`)과 대조한다. 원작 파일(util·bignum·gamedata·balance-data·
// forge·ascension·mounts)을 vm 컨텍스트에 그대로 싣고, 원작이 밖에서 받는 것(S · TechTree · SFX · Quests ·
// Combat · saveGame · Math.random)만 stub 으로 꽂는다 — 규칙은 한 줄도 다시 쓰지 않는다.
//
// 난수 = mulberry32(원작 `web/tools/shot-summon-result.js` 가 Math.random 에 심는 식 · Core `Mulberry32` 와 같다).
//
// 사용:
//   node tools/mount_vectors.js                 # .wwwww-src → Assets/Tests/EditMode/Vectors/mount_vectors.json
//   node tools/mount_vectors.js --check         # 다시 뽑아 현재 파일과 같은지만 본다 (rc 1 = 다르다)
//   node tools/mount_vectors.js --src <wwwww 체크아웃> --out <파일>
// ============================================================================
'use strict';
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const ROOT = path.resolve(__dirname, '..');
const DEFAULT_SRC = path.join(ROOT, '.wwwww-src');
const DEFAULT_OUT = path.join(ROOT, 'Assets', 'Tests', 'EditMode', 'Vectors', 'mount_vectors.json');
const LOAD_ORDER = ['util.js', 'bignum.js', 'gamedata.js', 'balance-data.js', 'forge.js', 'ascension.js', 'mounts.js'];

function mulberry32(seed) {
    let a = seed >>> 0;
    return function () {
        a |= 0; a = a + 0x6D2B79F5 | 0;
        let t = Math.imul(a ^ a >>> 15, 1 | a);
        t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
        return ((t ^ t >>> 14) >>> 0) / 4294967296;
    };
}

function makeWorld(src) {
    const sandbox = { console };
    sandbox.window = sandbox;
    sandbox.Math = Object.create(Math);            // Math.random 만 바꿔 심는다 — 나머지는 원본 Math
    sandbox.Date = Date;
    vm.createContext(sandbox);
    for (const f of LOAD_ORDER) {
        const p = path.join(src, 'web', 'js', f);
        vm.runInContext(fs.readFileSync(p, 'utf8'), sandbox, { filename: f });
    }
    const w = Object.assign(sandbox, vm.runInContext('({ U, Mounts, Forge, Ascension, RARITIES, mountSummonRates, mountNames, WINDERS_PER_SUMMON, Big })', sandbox));
    w.log = { gacha: [], quests: [], saves: 0, recalc: 0 };
    w.SFX = { gacha: r => w.log.gacha.push(r) };
    w.Quests = { bump: (k, n) => w.log.quests.push(n === undefined ? k : k + ':' + n) };
    w.Combat = { recalcHero: () => { w.log.recalc++; } };
    w.saveGame = () => { w.log.saves++; };
    w.tech = { mountDmg: 0, mountHp: 0, mountCost: 0, extraMount: 0 };   // 기술트리 pct(%) 를 직접 준다
    w.TechTree = {
        pct: k => w.tech[k] || 0,
        mountDmgMult() { return 1 + this.pct('mountDmg') / 100; },
        mountHpMult() { return 1 + this.pct('mountHp') / 100; },
        mountCostMult() { return Math.max(0.1, 1 - this.pct('mountCost') / 100); },
        extraMountChance() { return this.pct('extraMount') / 100; },
    };
    w.seed = s => { w.Math.random = mulberry32(s); };
    w.reset = (over) => {
        w.S = Object.assign({ winders: 0, mountOpens: 0, mounts: [], activeMounts: [], lineAscend: { forge: 0, skill: 0, pet: 0, mount: 0 } }, over || {});
        w.log.gacha.length = 0; w.log.quests.length = 0; w.log.saves = 0; w.log.recalc = 0;
        w.tech = { mountDmg: 0, mountHp: 0, mountCost: 0, extraMount: 0 };
    };
    return w;
}

const big = b => ({ m: b.m, e: b.e });
const mount = m => ({ name: m.name, rarity: m.rarity, level: m.level, xp: m.xp || 0, stars: m.stars || 0, subs: (m.subs || []).map(s => ({ key: s.key, value: s.value })) });
const snap = S => ({ mounts: (Array.isArray(S.mounts) ? S.mounts : []).map(mount), activeMounts: (S.activeMounts || []).slice(), winders: S.winders, mountOpens: S.mountOpens });
function mk(name, rarity, level, over) { return Object.assign({ name, rarity, level: level || 1, xp: 0, stars: 0, subs: [] }, over || {}); }

function build(src) {
    const w = makeWorld(src);
    const { Mounts, RARITIES, mountSummonRates, mountNames } = w;
    const out = { note: '정본 mounts.js 를 mulberry32 시드로 돌린 대조 벡터 — tools/mount_vectors.js 가 만든다 · 손으로 고치지 않는다', consts: {} };

    // ---- 상수(원작 mounts.js 의 코드 상수 · Core MountRules 가 같은 값이어야 한다) ----
    for (const k of ['MAX_LEVEL', 'INDIV_MAX_LEVEL', 'INV_CAP', 'MAX_ACTIVE_MOUNTS']) out.consts[k] = Mounts[k];
    out.consts.LEGACY_SPECIES = Object.assign({}, Mounts.LEGACY_SPECIES);
    out.consts.WINDERS_PER_SUMMON = w.WINDERS_PER_SUMMON;
    out.consts.STAR_MULT = w.Ascension.STAR_MULT;
    out.consts.forge = { TIER_BASE_ATK: w.Forge.TIER_BASE_ATK, TIER_BASE_HP: w.Forge.TIER_BASE_HP, TIER_STEP: w.Forge.TIER_STEP, ATK_SLOTS: w.Forge.ATK_SLOTS, HP_SLOTS: w.Forge.HP_SLOTS, LEVEL_STEP: w.Forge.LEVEL_STEP };

    // ---- ① 소환 레벨 · needed · prevNeeded — 표의 문턱마다 −1/0/+1 ----
    out.level = [];
    const opens = new Set([0, 1]);
    for (let l = 1; l < Mounts.MAX_LEVEL; l++) { const n = mountSummonRates[l].needed; if (n !== 'MAX') { opens.add(n - 1); opens.add(n); opens.add(n + 1); } }
    opens.add(1e6); opens.add(1e9);
    for (const c of [...opens].sort((a, b) => a - b)) {
        w.reset({ mountOpens: c });
        out.level.push({ mountOpens: c, level: Mounts.level(), nextNeeded: Mounts.nextNeeded(), prevNeeded: Mounts.prevNeeded() });
    }

    // ---- ② 등급 확률표(needed 제외 · 키 순서) ----
    out.rates = [];
    for (const c of [0, 2, 5, 100, 1000, 1e6]) { w.reset({ mountOpens: c }); const r = Mounts.rates(); out.rates.push({ mountOpens: c, level: Mounts.level(), keys: Object.keys(r), values: Object.values(r) }); }

    // ---- ③ 태엽 비용 · canSummon ----
    out.winderCost = [];
    for (const pct of [0, 1, 20, 50, 90, 100, 150]) for (const count of [1, 5, 25, 75, 0.5, 2.7]) { w.reset(); w.tech.mountCost = pct; out.winderCost.push({ mountCost: pct, count, cost: Mounts.winderCost(count) }); }
    out.canSummon = [];
    for (const c of [{ winders: 0, count: 1 }, { winders: 49, count: 1 }, { winders: 50, count: 1 }, { winders: 250, count: 5 }, { winders: 249, count: 5 }, { winders: 3750, count: 75 }]) { w.reset({ winders: c.winders }); out.canSummon.push({ ...c, can: Mounts.canSummon(c.count) }); }

    // ---- ④ 소환(배치) — 비용 · 보관함 클램프 · 보너스 · 자동 장착은 빈 슬롯일 때만 · 레벨이 오르며 확률표가 바뀐다 ----
    out.summon = [];
    const summonCases = [
        { seed: 14, winders: 1000, mounts: 0, active: [], mountOpens: 0, extraMount: 0, mountCost: 0, count: 5, ascendMount: 0 },
        { seed: 3, winders: 100000, mounts: 0, active: [], mountOpens: 300, extraMount: 50, mountCost: 0, count: 75, ascendMount: 0 },
        { seed: 9, winders: 100000, mounts: 245, active: [2], mountOpens: 100, extraMount: 2, mountCost: 20, count: 25, ascendMount: 1 },
        { seed: 5, winders: 100, mounts: 0, active: [], mountOpens: 20, extraMount: 0, mountCost: 0, count: 5, ascendMount: 0 },
        { seed: 7, winders: 100000, mounts: 250, active: [0], mountOpens: 0, extraMount: 0, mountCost: 0, count: 1, ascendMount: 0 },
        { seed: 21, winders: 10000, mounts: 248, active: [], mountOpens: 2000, extraMount: 100, mountCost: 90, count: 3, ascendMount: 3 },
        { seed: 22, winders: 10000, mounts: 0, active: [], mountOpens: 0, extraMount: 0, mountCost: 0, count: 0, ascendMount: 0 },
        { seed: 23, winders: 10000, mounts: 3, active: [], mountOpens: 0, extraMount: 0, mountCost: 0, count: 2.7, ascendMount: 0 },
        { seed: 24, winders: 10000, mounts: 1, active: [0], mountOpens: 1, extraMount: 0, mountCost: 0, count: 1, ascendMount: 0 },
    ];
    for (const c of summonCases) {
        w.reset({ winders: c.winders, mountOpens: c.mountOpens, mounts: Array.from({ length: c.mounts }, (_, i) => mk('Pony', 'common', 1)), activeMounts: c.active.slice(), lineAscend: { forge: 0, skill: 0, pet: 0, mount: c.ascendMount } });
        w.tech.extraMount = c.extraMount; w.tech.mountCost = c.mountCost; w.seed(c.seed);
        const pre = { summonCount: Mounts.summonCount(c.count), winderCost: Mounts.winderCost(Mounts.summonCount(c.count)), canSummon: Mounts.canSummon(Mounts.summonCount(c.count)), space: Mounts.space() };
        const r = Mounts.summon(c.count);
        out.summon.push({ ...c, pre, result: r && { results: r.results.map(x => ({ name: x.name, rarity: x.rarity, isNew: x.isNew, level: x.level })) }, after: snap(w.S), gacha: w.log.gacha.slice(), quests: w.log.quests.slice(), saves: w.log.saves, recalc: w.log.recalc });
    }

    // ---- ⑤ 장착 1마리 — equip 토글·교체 · setRidden · isActive · ridden/riddenIdx ----
    {
        w.reset({ mounts: [mk('Pony', 'common'), mk('Donkey', 'common'), mk('Sheep', 'rare'), mk('Pony', 'common')] });
        out.equip = [];
        const step = (op, i) => {
            const ok = op === 'equip' ? Mounts.equip(i) : Mounts.setRidden(i);
            out.equip.push({ op, idx: i, ok, active: w.S.activeMounts.slice(), riddenIdx: Mounts.riddenIdx(), ridden: Mounts.ridden(), isActive: [0, 1, 2, 3].map(k => Mounts.isActive(k)), recalc: w.log.recalc, saves: w.log.saves });
        };
        step('equip', 0); step('equip', 2); step('equip', 2); step('setRidden', 3); step('setRidden', 3); step('equip', 9); step('setRidden', -1); step('equip', 1);
        out.equipEmpty = (() => { w.reset(); return { riddenIdx: Mounts.riddenIdx(), riddenInst: Mounts.riddenInst(), ridden: Mounts.ridden(), isActive0: Mounts.isActive(0), count: Mounts.count(), space: Mounts.space() }; })();
    }

    // ---- ⑥ ensure — 장착 목록 정리(정수 아님 · 없는 개체 · 중복 · 2마리 이상 → 1) · 기본값 채움 ----
    out.ensure = [];
    for (const c of [
        { name: 'dup_and_ghost', mounts: 3, active: [1, 1, 7, 'x', 2.5, 0] },
        { name: 'two_to_one', mounts: 2, active: [1, 0] },
        { name: 'not_array', mounts: 2, active: 'Pony' },
        { name: 'undefined_fields', mounts: 1, active: [0], winders: undefined, mountOpens: undefined },
    ]) {
        w.reset({ mounts: Array.from({ length: c.mounts }, (_, i) => mk('M' + i, 'common')), activeMounts: c.active });
        if ('winders' in c) { delete w.S.winders; delete w.S.mountOpens; }
        Mounts.ensure();
        out.ensure.push({ name: c.name, after: snap(w.S) });
    }

    // ---- ⑦ 경험치 · 레벨 · 흡수(내림차순 · 인덱스 보정) ----
    out.xpNeeded = [1, 2, 3, 5, 10, 17, 33, 50, 64, 99, 100].map(l => ({ level: l, xp: Mounts.xpNeeded(l) }));
    out.xpValue = RARITIES.map(r => ({ rarity: r, xp: Mounts.xpValue(r) }));
    out.addXp = [];
    for (const c of [{ level: 1, xp: 0, add: 500 }, { level: 1, xp: 79, add: 1 }, { level: 10, xp: 0, add: 100000 }, { level: 99, xp: 0, add: 1e9 }, { level: 100, xp: 0, add: 5 }, { level: 1, xp: 0, add: 0.5 }]) {
        w.reset({ mounts: [mk('Pony', 'common', c.level, { xp: c.xp })] });
        Mounts.addXp(0, c.add);
        out.addXp.push({ ...c, level_after: w.S.mounts[0].level, xp_after: w.S.mounts[0].xp });
        Mounts.addXp(9, 1);   // 없는 인덱스는 무시
    }
    out.absorb = [];
    for (const c of [
        { name: 'target3_mats_0_1_5_dupes_self_ghost', mounts: [['A', 'common', 1], ['B', 'rare', 5], ['C', 'epic', 1], ['D', 'mythic', 30], ['E', 'common', 2], ['F', 'legendary', 3]], active: [3], target: 3, mats: [0, 1, 5, 1, 3, 9, 'x', 2.5] },
        { name: 'target0_mats_after', mounts: [['A', 'common', 1], ['B', 'rare', 5], ['C', 'epic', 1]], active: [2], target: 0, mats: [2, 1] },
        { name: 'active_removed', mounts: [['A', 'common', 1], ['B', 'rare', 1]], active: [1], target: 0, mats: [1] },
        { name: 'ghost_target', mounts: [['A', 'common', 1]], active: [0], target: 5, mats: [0] },
        { name: 'no_mats', mounts: [['A', 'common', 1]], active: [0], target: 0, mats: [] },
    ]) {
        w.reset({ mounts: c.mounts.map(m => mk(m[0], m[1], m[2])), activeMounts: c.active.slice() });
        const ok = Mounts.absorbMaterials(c.target, c.mats);
        out.absorb.push({ name: c.name, ok, after: snap(w.S), quests: w.log.quests.slice(), recalc: w.log.recalc, saves: w.log.saves });
    }

    // ---- ⑧ 기여: 등급 기준치 · 레벨 · 별 · 기술트리 · activeBonus ----
    out.baseStat = RARITIES.map(r => { w.reset(); const b = Mounts.baseStat(r); return { rarity: r, atk: b.atk, hp: b.hp }; });
    out.mountPower = [];
    for (const c of [{ rarity: 'common', level: 1, stars: 0, mountDmg: 0, mountHp: 0 }, { rarity: 'rare', level: 17, stars: 0, mountDmg: 0, mountHp: 0 }, { rarity: 'mythic', level: 100, stars: 0, mountDmg: 0, mountHp: 0 }, { rarity: 'epic', level: 1, stars: 1, mountDmg: 0, mountHp: 0 }, { rarity: 'legendary', level: 50, stars: 3, mountDmg: 25, mountHp: 10 }, { rarity: 'ultimate', level: 2, stars: 40, mountDmg: 100, mountHp: 0 }]) {
        w.reset(); w.tech.mountDmg = c.mountDmg; w.tech.mountHp = c.mountHp;
        const p = Mounts.mountPower(mk('X', c.rarity, c.level, { stars: c.stars }));
        out.mountPower.push({ ...c, atk: big(p.atk), hp: big(p.hp), levelMult: Mounts.levelMult({ level: c.level }) });
    }
    {
        const subs = [{ key: 'critCh', label: '치명타 확률', value: 3.5 }, { key: 'hpPct', label: '체력', value: 12 }];
        w.reset({ mounts: [mk('Pony', 'common', 1, { subs }), mk('Dragon', 'mythic', 10, { stars: 1, subs: [{ key: 'atkSpd', label: '공격 속도', value: 7.7 }] })], activeMounts: [1] });
        const b = Mounts.activeBonus();
        out.activeBonus = { atk: big(b.atk), hp: big(b.hp), subs: b.subs.map(s => ({ key: s.key, value: s.value })) };
        w.S.activeMounts = [0]; const b0 = Mounts.activeBonus();
        out.activeBonus0 = { atk: big(b0.atk), hp: big(b0.hp), subs: b0.subs.map(s => ({ key: s.key, value: s.value })) };
        w.S.activeMounts = [7]; const bg = Mounts.activeBonus();
        out.activeBonusGhost = { atk: big(bg.atk), hp: big(bg.hp), subs: bg.subs.length };
        w.reset(); const z = Mounts.activeBonus();
        out.activeBonusEmpty = { atk: big(z.atk), hp: big(z.hp), subs: z.subs.length };
    }

    // ---- ⑨ 구세이브 이관 — 맵 → 개체 배열(dupes 펼침 · 원본 앞 · 250 자르기 · 이름 장착 → 인덱스) · 폐기 종 이름 ----
    out.migrate = [];
    {
        w.reset({ mounts: { 'Pony': { rarity: 'common', level: 3, dupes: 2, stars: 1, xp: 40, subs: [{ key: 'block', label: '블록 확률', value: 2.5 }] }, 'Brown Leaf': { rarity: 'common', dupes: 1 }, 'Dragon': { rarity: 'mythic', level: 7 } }, activeMounts: ['Dragon', 'Pony', 'Nope', 3, 1.5] });
        w.seed(51);
        Mounts.ensure();
        out.migrate.push({ name: 'legacy_map_dupes_active_names', seed: 51, after: snap(w.S) });
    }
    {
        const legacy = {};
        for (let i = 0; i < 130; i++) legacy['M' + i] = { rarity: 'rare', level: 2, dupes: 1 };
        w.reset({ mounts: legacy, activeMounts: ['M129'] });
        w.seed(52);
        Mounts.ensure();
        out.migrate.push({ name: 'legacy_overflow_keeps_originals', seed: 52, len: w.S.mounts.length, names: w.S.mounts.map(m => m.name), levels: w.S.mounts.map(m => m.level), active: w.S.activeMounts.slice() });
    }
    {
        w.reset({ mounts: [mk('Lily Pad', 'common'), mk('Pony', 'common'), mk('Log Raft', 'common', 4, { stars: 2 }), mk('Oak Leaf', 'rare')], activeMounts: [2] });
        const moved = Mounts.migrateSpecies();
        out.migrate.push({ name: 'legacy_species_rename', moved, after: snap(w.S) });
        w.reset({ mounts: Array.from({ length: 260 }, (_, i) => mk('M' + i, 'common')), activeMounts: [255, 3] });
        Mounts.ensure();
        out.migrate.push({ name: 'array_clamped_250_active_pruned', len: w.S.mounts.length, active: w.S.activeMounts.slice() });
        w.reset({ mounts: undefined, activeMounts: undefined });
        delete w.S.mounts; delete w.S.activeMounts;
        Mounts.ensure();
        out.migrate.push({ name: 'missing_fields', after: snap(w.S) });
    }
    return out;
}

function main(argv) {
    let src = DEFAULT_SRC, out = DEFAULT_OUT, check = false;
    for (let i = 0; i < argv.length; i++) {
        if (argv[i] === '--src') src = path.resolve(argv[++i]);
        else if (argv[i] === '--out') out = path.resolve(argv[++i]);
        else if (argv[i] === '--check') check = true;
        else { console.error('모르는 인자: ' + argv[i]); return 2; }
    }
    if (!fs.existsSync(path.join(src, 'web', 'js', 'mounts.js'))) { console.error('정본이 없다: ' + src + ' (git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)'); return 2; }
    const text = JSON.stringify(build(src), null, 1) + '\n';
    if (check) {
        const cur = fs.existsSync(out) ? fs.readFileSync(out, 'utf8') : '';
        if (cur !== text) { console.error('✗ mount_vectors: ' + path.relative(ROOT, out) + ' 이 정본과 다르다 — node tools/mount_vectors.js 로 다시 뽑아라'); return 1; }
        console.log('✓ mount_vectors: ' + path.relative(ROOT, out) + ' 이 정본과 같다');
        return 0;
    }
    fs.mkdirSync(path.dirname(out), { recursive: true });
    fs.writeFileSync(out, text);
    console.log('✓ mount_vectors → ' + path.relative(ROOT, out) + ' (' + text.length + ' bytes)');
    return 0;
}

if (require.main === module) process.exit(main(process.argv.slice(2)));
module.exports = { build, mulberry32 };
