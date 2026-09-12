#!/usr/bin/env node
// ============================================================================
// gear_vectors.js — 정본 forge.js 의 **장비 절**(itemValue · itemPower · isMatchingGear · sellPrice · equip · sell · autoResolve ·
// allSubsBag · heroStats)을 node vm 에서 실제로 실행해 C#(T15 `Core/Gear`)이 대조할 벡터를 뽑는다.
// ----------------------------------------------------------------------------
// 틀은 tools/pet_vectors.js 와 같다: util·bignum·gamedata·balance-data·forge·ascension 을 순서대로 싣고, 밖에서 받는 것
// (S · TechTree · Quests · SFX · UI · Combat · Pets · Mounts · Skills · Scene3D · saveGame)은 스텁으로 꽂아 **값을 이쪽이 정한다** —
// 같은 값을 C# 쪽 IGearHost 에 주면 같은 결과가 나와야 한다. 난수는 mulberry32(원작 shot-summon-result.js 식 = Core Mulberry32).
//
// 출력: Assets/Tests/EditMode/Vectors/t15-gear.json
//   consts   : 맨몸 기본치·상한(heroStats 의 코드 상수) · RARITY_MULT · SLOT_MAIN · SUBSTATS 키 순서
//   items    : 대장간 레벨 3 × 승천 별 3 × 8개 = 72개 — 아이템 원문 + itemValue · itemPower · sellPrice(배율 1 / 1.3) · 앞 것과 isMatchingGear
//   damaged  : 반쪽 아이템의 sellPrice(NaN) · sell(0 · 코인 불변) · itemValue(null → 0)
//   equip    : 장착 흐름 12회(prev · 장착표 · 퀘스트 · recalc · refresh 호출) · 판매 5회(코인 누적) · autoResolve 2회
//   stats    : 6장면 — allSubsBag + heroStats(펫/탈것/스킬/버프/기술트리/별 조합)
//
// 사용: node tools/gear_vectors.js [--src .wwwww-src] [--out …] [--check]
// ============================================================================
'use strict';
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const ROOT = path.resolve(__dirname, '..');
let SRC = path.join(ROOT, '.wwwww-src');
let OUT = path.join(ROOT, 'Assets', 'Tests', 'EditMode', 'Vectors', 't15-gear.json');
let CHECK = false;
for (let i = 2; i < process.argv.length; i++) {
    if (process.argv[i] === '--src') SRC = path.resolve(process.argv[++i]);
    else if (process.argv[i] === '--out') OUT = path.resolve(process.argv[++i]);
    else if (process.argv[i] === '--check') CHECK = true;
}
const LOAD_ORDER = ['util.js', 'bignum.js', 'gamedata.js', 'balance-data.js', 'forge.js', 'ascension.js'];

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
    const sandbox = { console: { error: (...a) => sandbox.log.errors.push(a.join(' ')), log() {}, warn() {} } };
    sandbox.window = sandbox;
    sandbox.Math = Object.create(Math);
    sandbox.Date = Date;
    vm.createContext(sandbox);
    for (const f of LOAD_ORDER) vm.runInContext(fs.readFileSync(path.join(src, 'web', 'js', f), 'utf8'), sandbox, { filename: f });
    const w = Object.assign(sandbox, vm.runInContext('({ U, Forge, Ascension, RARITIES, RARITY_MULT, SLOTS, SLOT_MAIN, SUBSTATS, AGES, Big, itemStyleOf, itemNameOf, AGE_COLORS, RARITY_HEX, WEAPON_TYPES })', sandbox));
    w.log = { errors: [], quests: [], refresh: [], recalc: 0, saves: 0, sfx: [] };
    w.tech = { sellPrice: 0, weaponMastery: 0, armorMastery: 0, freeForge: 0, gearMaxLevel: 0 };
    w.TechTree = {
        pct: k => w.tech[k] || 0,
        sellPriceMult() { return 1 + this.pct('sellPrice') / 100; },
        gearAtkMult() { return 1 + this.pct('weaponMastery') / 100; },
        gearHpMult() { return 1 + this.pct('armorMastery') / 100; },
        freeForgeChance() { return this.pct('freeForge') / 100; },
        gearMaxLevelBonus() { return this.pct('gearMaxLevel'); },
        forgeCostMult() { return 1; }, forgeTimeMult() { return 1; },
    };
    w.Quests = { bump: (k, n) => w.log.quests.push(n === undefined ? k : k + ':' + n) };
    w.SFX = { craft: () => w.log.sfx.push('craft'), levelUp: () => w.log.sfx.push('levelUp') };
    w.UI = { toast() {}, renderEquipSheet() {}, renderForgeInfo() {}, renderAutoForge() {}, els: { forgeInfoModal: { classList: { contains: () => true } }, autoForgeModal: { classList: { contains: () => true } } } };
    w.Combat = { recalcHero: () => { w.log.recalc++; }, buffs: [] };
    w.Scene3D = { refreshHeroEquip: f => w.log.refresh.push(!!f) };
    w.saveGame = () => { w.log.saves++; };
    w.bonus = { pets: { atk: 0, hp: 0, subs: [] }, mounts: { atk: 0, hp: 0, subs: [] }, skills: { atk: 0, hp: 0 } };
    w.Pets = { activeBonus: () => ({ atk: w.Big.of(w.bonus.pets.atk), hp: w.Big.of(w.bonus.pets.hp), subs: w.bonus.pets.subs }) };
    w.Mounts = { activeBonus: () => ({ atk: w.Big.of(w.bonus.mounts.atk), hp: w.Big.of(w.bonus.mounts.hp), subs: w.bonus.mounts.subs }) };
    w.Skills = { ownedPassive: () => ({ atk: w.Big.of(w.bonus.skills.atk), hp: w.Big.of(w.bonus.skills.hp) }) };
    w.now = 1_700_000_000_000;
    w.U.now = () => w.now;
    w.seed = s => { w.Math.random = mulberry32(s); };
    w.reset = over => {
        w.S = Object.assign({ coins: 0, gems: 0, hammers: 0, totalCrafts: 0, forgeLevel: 1, rollLevel: {}, equipment: {}, lineAscend: { forge: 0, skill: 0, pet: 0, mount: 0 } }, over || {});
        w.log.errors.length = 0; w.log.quests.length = 0; w.log.refresh.length = 0; w.log.recalc = 0; w.log.saves = 0; w.log.sfx.length = 0;
        w.tech = { sellPrice: 0, weaponMastery: 0, armorMastery: 0, freeForge: 0, gearMaxLevel: 0 };
        w.Combat.buffs = [];
        w.bonus = { pets: { atk: 0, hp: 0, subs: [] }, mounts: { atk: 0, hp: 0, subs: [] }, skills: { atk: 0, hp: 0 } };
    };
    return w;
}

const big = b => b.toString();
const item = it => it ? ({ name: it.name, slot: it.slot, age: it.age, ageIdx: it.ageIdx, rarity: it.rarity, level: it.level, main: it.main, value: it.value, subs: it.subs.map(s => ({ key: s.key, label: s.label, value: s.value })), wtype: it.wtype, nameIdx: it.nameIdx, stars: it.stars }) : null;
const num = v => Number.isFinite(v) ? v : (Number.isNaN(v) ? 'NaN' : String(v));

function build(src) {
    const w = makeWorld(src);
    const { Forge, U, SLOTS, SUBSTATS, RARITY_MULT, SLOT_MAIN } = w;
    const out = { note: '정본 forge.js 장비 절을 mulberry32 시드로 돌린 대조 벡터 — tools/gear_vectors.js 가 만든다 · 손으로 고치지 않는다' };
    out.consts = { STAR_MULT: w.Ascension.STAR_MULT, RARITY_MULT, SLOT_MAIN, SLOTS, SUBSTAT_KEYS: SUBSTATS.map(s => s[0]) };

    // ---- ① 아이템 72개: itemValue · itemPower · sellPrice · isMatchingGear ----
    out.items = [];
    let seedNo = 0, prev = null;
    for (const fl of [1, 12, 35]) for (const stars of [0, 1, 3]) {
        w.reset({ forgeLevel: fl, lineAscend: { forge: stars, skill: 0, pet: 0, mount: 0 } });
        w.seed(5000 + seedNo++);
        for (let k = 0; k < 8; k++) {
            const it = Forge.rollItem();
            w.tech.sellPrice = 0; const sp1 = Forge.sellPrice(it);
            w.tech.sellPrice = 30; const sp2 = Forge.sellPrice(it);
            out.items.push({ forgeLevel: fl, stars, item: item(it), itemValue: big(Forge.itemValue(it)), itemPower: big(Forge.itemPower(it)), sellPrice: sp1, sellPrice130: sp2, matchPrev: Forge.isMatchingGear(prev, it), style: w.itemStyleOf(it), nameOf: w.itemNameOf(it), nameOfNoName: w.itemNameOf(Object.assign({}, it, { name: '' })) });
            prev = it;
        }
    }
    // 같은 slot·rarity·name 짝(isMatchingGear true) 하나를 일부러 만든다
    {
        const a = out.items[0].item, b = Object.assign({}, a, { level: 99, value: 1 });
        w.reset();
        out.matchSame = { a: a, b: item(Object.assign({ subs: [] }, b)), match: Forge.isMatchingGear(a, b), selfNull: Forge.isMatchingGear(null, a) };
    }

    // ---- ② 반쪽 아이템 ----
    w.reset({ coins: 777 });
    const half = { slot: 'ring' };
    out.damaged = { sellPrice: num(Forge.sellPrice(half)), sell: Forge.sell(half), coinsAfter: w.S.coins, errors: w.log.errors.length, itemValueNull: big(Forge.itemValue(null)), itemPowerNull: big(Forge.itemPower(null)), sellPriceNoRarity: num(Forge.sellPrice({ slot: 'ring', level: 5 })) };

    // ---- ③ 장착·판매·autoResolve 흐름 ----
    w.reset({ forgeLevel: 20, coins: 100, lineAscend: { forge: 2, skill: 0, pet: 0, mount: 0 } });
    w.seed(777);
    const pool = []; for (let k = 0; k < 14; k++) pool.push(Forge.rollItem());
    const flow = { pool: pool.map(item), steps: [] };
    for (let k = 0; k < 12; k++) {
        const it = pool[k];
        const p = Forge.equip(it);
        flow.steps.push({ op: 'equip', i: k, prev: p ? pool.indexOf(p) : null, quests: w.log.quests.slice(), refresh: w.log.refresh.slice(), recalc: w.log.recalc, equipment: Object.fromEntries(SLOTS.map(s => [s, w.S.equipment[s] ? pool.indexOf(w.S.equipment[s]) : null])) });
    }
    w.tech.sellPrice = 15;
    for (let k = 0; k < 5; k++) {
        const price = Forge.sell(pool[k]);
        flow.steps.push({ op: 'sell', i: k, price, coins: w.S.coins, quests: w.log.quests.length });
    }
    for (let k = 12; k < 14; k++) {
        const r = Forge.autoResolve(pool[k]);
        flow.steps.push({ op: 'autoResolve', i: k, equipped: r.equipped, gained: r.gained, coins: w.S.coins, quests: w.log.quests.length });
    }
    out.equip = flow;

    // ---- ④ allSubsBag · heroStats 6장면 ----
    const scenes = [];

    // refreshHeroEquip 의 표 계산 부분(무기 종 · 투구 스타일/이름 · 갑옷 색·발광·문장) — 원작 6줄을 같은 식으로 옮긴 것(Scene3D 는 vm 에 못 올린다)
    const lookOf = () => {
        const wq = w.S.equipment.weapon, h = w.S.equipment.helmet, a = w.S.equipment.armor;
        const c = a ? w.AGE_COLORS[a.age] : 0xb0bec5;
        const aIdx = a ? w.RARITIES.indexOf(a.rarity) : 0;
        const ec = a ? w.RARITY_HEX[a.rarity] : 0x78909c;
        return {
            wtypeId: wq ? (wq.wtype || 'sword') : 'club',
            helmetStyle: h ? w.itemStyleOf(h) : null, helmetName: h ? w.itemNameOf(h) : null,
            armorColor: c, armorEmissive: aIdx >= 4 ? w.RARITY_HEX[a.rarity] : 0, armorEmissiveIntensity: aIdx >= 4 ? 0.18 : 0,
            emblemColor: ec, emblemEmissive: a ? ec : 0, emblemEmissiveIntensity: a ? 0.6 : 0,
            armorStyle: a ? w.itemStyleOf(a) : 'plate', armorName: a ? w.itemNameOf(a) : null,
        };
    };
    const statsOf = (name, setup) => {
        w.reset({ forgeLevel: 25, lineAscend: { forge: 0, skill: 0, pet: 0, mount: 0 } });
        setup();
        const bag = Forge.allSubsBag();
        const h = Forge.heroStats();
        scenes.push({
            name,
            equipment: Object.fromEntries(SLOTS.map(s => [s, item(w.S.equipment[s])])),
            bonus: JSON.parse(JSON.stringify(w.bonus)),
            buffs: w.Combat.buffs.map(b => b.buff && b.buff.atkFlat ? big(w.Big.of(b.buff.atkFlat)) : null),
            tech: Object.assign({}, w.tech),
            bag,
            look: lookOf(),
            stats: { atk: big(h.atk), hp: big(h.hp), critCh: h.critCh, critDmg: h.critDmg, attacksPerSec: h.attacksPerSec, dblAtk: h.dblAtk, block: h.block, hpRegen: h.hpRegen, lifesteal: h.lifesteal, meleeDmg: h.meleeDmg, rangedDmg: h.rangedDmg, skillDmg: h.skillDmg, skillCd: h.skillCd },
        });
    };
    const fillAll = (seed, stars) => {
        w.S.lineAscend.forge = stars; w.seed(seed);
        // 8부위 전부 채울 때까지 뽑는다(최대 200회)
        for (let k = 0; k < 200 && SLOTS.some(s => !w.S.equipment[s]); k++) { const it = Forge.rollItem(); if (!w.S.equipment[it.slot]) w.S.equipment[it.slot] = it; }
    };
    statsOf('맨몸', () => {});
    statsOf('장비 8부위', () => { fillAll(11, 0); });
    statsOf('장비+펫', () => { fillAll(12, 0); w.bonus.pets = { atk: 1234, hp: 56789, subs: [{ key: 'critCh', value: 3.5 }, { key: 'dmgPct', value: 9.9 }, { key: 'skillCd', value: 2 }] }; });
    statsOf('장비+탈것+스킬', () => { fillAll(13, 1); w.bonus.mounts = { atk: 40, hp: 900, subs: [{ key: 'hpPct', value: 12 }, { key: 'block', value: 4.4 }] }; w.bonus.skills = { atk: 3e9, hp: 7e10 }; });
    statsOf('버프+기술트리', () => { fillAll(14, 2); w.tech.weaponMastery = 25; w.tech.armorMastery = 40; w.Combat.buffs = [{ buff: { atkFlat: 5000 } }, { buff: {} }, {}, { buff: { atkFlat: w.Big.of('7e12') } }]; });
    statsOf('상한 걸림', () => {
        fillAll(15, 0);
        w.bonus.pets = { atk: 1, hp: 1, subs: [{ key: 'critCh', value: 90 }, { key: 'dblAtk', value: 70 }, { key: 'block', value: 99 }, { key: 'skillCd', value: 95 }, { key: 'atkSpd', value: 50 }, { key: 'unknownKey', value: 3 }] };
        w.bonus.mounts = { atk: 0, hp: 0, subs: [{ key: 'critCh', value: 5 }] };
    });
    statsOf('무기만', () => { w.seed(16); for (let k = 0; k < 200 && !w.S.equipment.weapon; k++) { const it = Forge.rollItem(); if (it.slot === 'weapon') w.S.equipment.weapon = it; } });
    out.stats = scenes;

    // 반쪽 장비(subs 없음)가 allSubsBag 을 끊지 않는다
    w.reset(); w.S.equipment.ring = { slot: 'ring', main: 'atk', value: 5, level: 1, rarity: 'common', stars: 0 };
    out.noSubsBag = Forge.allSubsBag();
    out.noSubsAtk = big(Forge.heroStats().atk);
    return out;
}

const doc = build(SRC);
const text = JSON.stringify(doc);
if (CHECK) {
    const cur = fs.existsSync(OUT) ? fs.readFileSync(OUT, 'utf8') : '';
    if (cur !== text) { console.error('✗ gear_vectors --check: 정본이 바뀌었거나 벡터가 낡았다 — node tools/gear_vectors.js 로 다시 뽑는다'); process.exit(1); }
    console.log('✓ gear_vectors --check: 벡터가 정본과 같다'); process.exit(0);
}
fs.mkdirSync(path.dirname(OUT), { recursive: true });
fs.writeFileSync(OUT, text);
console.log('✓ gear_vectors: ' + OUT + ' · items ' + doc.items.length + ' · equip steps ' + doc.equip.steps.length + ' · stats ' + doc.stats.length + ' · ' + (text.length / 1024).toFixed(0) + 'KB');
