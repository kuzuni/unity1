#!/usr/bin/env node
// ============================================================================
// pet_vectors.js — 정본 `web/js/pets.js` 를 **그대로** 돌려 대조 벡터를 뽑는다 (T16).
// ----------------------------------------------------------------------------
// Core `PetSystem`(C#)이 원작 `Pets` 와 «같은 시드 → 같은 결과» 인지 EditMode `PetTests` 가 이 파일의 출력
// (`Assets/Tests/EditMode/pet_vectors.json`)과 대조한다. 원작 파일(util·bignum·gamedata·balance-data·
// forge·ascension·pets)을 vm 컨텍스트에 그대로 싣고, 원작이 밖에서 받는 것(S · TechTree · UI · SFX ·
// Quests · Combat · saveGame · U.now · Math.random)만 stub 으로 꽂는다 — 규칙은 한 줄도 다시 쓰지 않는다.
//
// 난수 = mulberry32(원작 `web/tools/shot-summon-result.js` 가 Math.random 에 심는 식 · Core `Mulberry32` 와 같다).
//
// 사용:
//   node tools/pet_vectors.js                 # .wwwww-src → Assets/Tests/EditMode/pet_vectors.json
//   node tools/pet_vectors.js --check         # 다시 뽑아 현재 파일과 같은지만 본다 (rc 1 = 다르다)
//   node tools/pet_vectors.js --src <wwwww 체크아웃> --out <파일>
// ============================================================================
'use strict';
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const ROOT = path.resolve(__dirname, '..');
const DEFAULT_SRC = path.join(ROOT, '.wwwww-src');
const DEFAULT_OUT = path.join(ROOT, 'Assets', 'Tests', 'EditMode', 'pet_vectors.json');
const LOAD_ORDER = ['util.js', 'bignum.js', 'gamedata.js', 'balance-data.js', 'forge.js', 'ascension.js', 'pets.js'];

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
    // 최상위 `const U`·`const Pets` 는 컨텍스트의 렉시컬 전역이라 sandbox 프로퍼티가 아니다 — 식으로 꺼낸다.
    const w = Object.assign(sandbox, vm.runInContext('({ U, Pets, Forge, Ascension, RARITIES, eggDropRates, Big })', sandbox));
    // ---- 원작이 밖에서 받는 것(전역 프로퍼티 = 스크립트에서 자유 식별자로 보인다) ----
    w.log = { toasts: [], quests: [], gacha: [], saves: 0, recalc: 0 };
    w.UI = { toast: m => w.log.toasts.push(m), renderPets() {}, renderEquipSheet() {} };
    w.SFX = { gacha: r => w.log.gacha.push(r) };
    w.Quests = { bump: k => w.log.quests.push(k) };
    w.Combat = { recalcHero: () => { w.log.recalc++; } };
    w.saveGame = () => { w.log.saves++; };
    w.tech = { petDmg: 0, petHp: 0, hatchTimer: 0, extraEgg: 0 };   // 기술트리 pct(%) 를 직접 준다
    w.TechTree = {
        pct: k => w.tech[k] || 0,
        petDmgMult() { return 1 + this.pct('petDmg') / 100; },
        petHpMult() { return 1 + this.pct('petHp') / 100; },
        hatchSpeedMult() { return 1 / (1 + this.pct('hatchTimer') / 100); },
        extraEggChance() { return this.pct('extraEgg') / 100; },
    };
    w.now = 1_700_000_000_000;
    w.U.now = () => w.now;
    w.seed = s => { w.Math.random = mulberry32(s); };
    w.reset = (over) => {
        w.S = Object.assign({
            gems: 0, eggCurrency: 0, petSummonCount: 0, hatchSlotBonus: 0,
            eggs: [], hatching: [], pets: [], activePets: [], lineAscend: { forge: 0, skill: 0, pet: 0, mount: 0 },
        }, over || {});
        w.log.toasts.length = 0; w.log.quests.length = 0; w.log.gacha.length = 0; w.log.saves = 0; w.log.recalc = 0;
        w.tech = { petDmg: 0, petHp: 0, hatchTimer: 0, extraEgg: 0 };
    };
    return w;
}

const big = b => ({ m: b.m, e: b.e });
const pet = p => ({ name: p.name, rarity: p.rarity, level: p.level, dupes: p.dupes || 0, xp: p.xp || 0, stars: p.stars || 0, subs: (p.subs || []).map(s => ({ key: s.key, value: s.value })) });
const snap = S => ({ eggs: S.eggs.map(e => e.rarity), hatching: S.hatching.map(h => ({ rarity: h.rarity, endsAt: h.endsAt })), pets: S.pets.map(pet), activePets: S.activePets.slice(), gems: S.gems, eggCurrency: S.eggCurrency, petSummonCount: S.petSummonCount, hatchSlotBonus: S.hatchSlotBonus });

function mkPet(name, rarity, level, over) { return Object.assign({ name, rarity, level: level || 1, dupes: 0, xp: 0, stars: 0, subs: [] }, over || {}); }

function build(src) {
    const w = makeWorld(src);
    const { Pets, RARITIES, eggDropRates, U } = w;
    const out = { note: '정본 pets.js 를 mulberry32 시드로 돌린 대조 벡터 — tools/pet_vectors.js 가 만든다 · 손으로 고치지 않는다', consts: {}, };

    // ---- 상수(원작 pets.js 의 코드 상수 · Core PetRules 가 같은 값이어야 한다) ----
    for (const k of ['BASE_HATCH_SLOTS', 'MAX_HATCH_SLOTS_CAP', 'SLOT_GEM_COST', 'INV_CAP', 'MAX_ACTIVE', 'AUTO_ACTIVE', 'POWER_DIV', 'EGG_CAP', 'MAX_LEVEL', 'SUMMON_EGG_COST'])
        out.consts[k] = Pets[k];
    out.consts.STAR_MULT = w.Ascension.STAR_MULT;
    out.consts.forge = { TIER_BASE_ATK: w.Forge.TIER_BASE_ATK, TIER_BASE_HP: w.Forge.TIER_BASE_HP, TIER_STEP: w.Forge.TIER_STEP, ATK_SLOTS: w.Forge.ATK_SLOTS, HP_SLOTS: w.Forge.HP_SLOTS, LEVEL_STEP: w.Forge.LEVEL_STEP };

    // ---- ① 알 등급 롤: 스테이지 키마다 시드 하나 · 8회 ----
    out.eggRoll = [];
    const keys = Object.keys(eggDropRates).concat(['99-99']);   // 없는 키 → 10-10 폴백
    keys.forEach((key, i) => {
        w.reset(); w.seed(1000 + i);
        const r = [];
        for (let k = 0; k < 8; k++) r.push(Pets.rollEggRarity(key));
        out.eggRoll.push({ key, seed: 1000 + i, rarities: r });
    });

    // ---- ② 소환 레벨 · 확률표 ----
    out.summonLevel = [0, 1, 4, 5, 9, 10, 49, 50, 494, 495, 499, 500, 1000].map(c => { w.reset({ petSummonCount: c }); return { petSummonCount: c, level: Pets.summonLevel() }; });
    out.rates = [1, 2, 3, 4, 20, 21, 50, 99, 100, 0, 150].map(l => { w.reset(); const r = Pets.rates(l); return { level: l, rates: RARITIES.map(k => r[k]) }; });

    // ---- ③ 소환(배치) — 비용·보관함 클램프·보너스 알 ----
    out.summon = [];
    const summonCases = [
        { seed: 14, eggCurrency: 1000, eggs: 0, petSummonCount: 0, extraEgg: 0, count: 5 },
        { seed: 3, eggCurrency: 100000, eggs: 0, petSummonCount: 495, extraEgg: 50, count: 75 },
        { seed: 9, eggCurrency: 100000, eggs: 90, petSummonCount: 100, extraEgg: 2, count: 25 },
        { seed: 5, eggCurrency: 150, eggs: 0, petSummonCount: 20, extraEgg: 0, count: 5 },
        { seed: 7, eggCurrency: 100000, eggs: 100, petSummonCount: 0, extraEgg: 0, count: 1 },
        { seed: 21, eggCurrency: 10000, eggs: 97, petSummonCount: 200, extraEgg: 100, count: 3 },
        { seed: 22, eggCurrency: 10000, eggs: 0, petSummonCount: 0, extraEgg: 0, count: 0 },
        { seed: 23, eggCurrency: 10000, eggs: 0, petSummonCount: 0, extraEgg: 0, count: 2.7 },
    ];
    for (const c of summonCases) {
        w.reset({ eggCurrency: c.eggCurrency, petSummonCount: c.petSummonCount, eggs: Array.from({ length: c.eggs }, () => ({ rarity: 'common' })) });
        w.tech.extraEgg = c.extraEgg; w.seed(c.seed);
        const pre = { summonCount: Pets.summonCount(c.count), summonCost: Pets.summonCost(c.count), canSummon: Pets.canSummon(c.count), eggSpace: Pets.eggSpace() };
        const r = Pets.summon(c.count);
        out.summon.push({ ...c, pre, result: r && { results: r.results.map(x => ({ rarity: x.rarity, extra: !!x.extra })), requested: r.requested, summoned: r.summoned, clamped: r.clamped }, after: snap(w.S), gacha: w.log.gacha.slice() });
    }

    // ---- ④ 부화 시간 · 슬롯 · 젬 스킵 ----
    out.hatchTime = [];
    for (const ht of [0, 50]) for (const r of RARITIES) { w.reset(); w.tech.hatchTimer = ht; out.hatchTime.push({ rarity: r, hatchTimer: ht, sec: Pets.hatchTimeSec(r) }); }
    out.slots = [];
    for (let bonus = 0; bonus <= 3; bonus++) { w.reset({ hatchSlotBonus: bonus, gems: 5000 }); out.slots.push({ bonus, max: Pets.maxHatchSlots(), cost: Pets.slotCost(), canBuy: Pets.canBuySlot() }); }
    w.reset({ gems: 900 }); out.buySlot = [];
    for (let i = 0; i < 4; i++) out.buySlot.push({ ok: Pets.buySlot(), gems: w.S.gems, bonus: w.S.hatchSlotBonus, max: Pets.maxHatchSlots() });
    w.reset({ eggs: [{ rarity: 'rare' }, { rarity: 'common' }, { rarity: 'mythic' }, { rarity: 'epic' }, { rarity: 'legendary' }] });
    out.startHatch = [];
    for (const idx of [1, 0, 0, 0, 5, 0]) out.startHatch.push({ idx, ok: Pets.startHatch(idx), after: snap(w.S) });
    out.gemSkipCost = [];
    for (const remainMs of [0, -5000, 1, 60000, 599999, 600000, 600001, 3600000, 115200000]) out.gemSkipCost.push({ remainMs, cost: Pets.gemSkipCost({ endsAt: w.now + remainMs }) });
    // 젬 스킵: 부화 중 둘 · 젬 부족/충분 · 스킵하면 tick 이 바로 부화시킨다
    w.reset({ gems: 10, hatching: [{ rarity: 'common', endsAt: w.now + 30 * 60000 }, { rarity: 'rare', endsAt: w.now + 120 * 60000 }] }); w.seed(31);
    out.gemSkip = [];
    for (const idx of [1, 0, 0, 2]) out.gemSkip.push({ idx, ok: Pets.gemSkip(idx), after: snap(w.S) });

    // ---- ⑤ tick — 부화 완료 · 자동 출전 · 보유 상한 되돌림 ----
    out.tick = [];
    {
        w.reset({ hatching: [{ rarity: 'common', endsAt: w.now - 1 }, { rarity: 'mythic', endsAt: w.now + 1 }, { rarity: 'rare', endsAt: w.now }, { rarity: 'epic', endsAt: w.now - 5 }, { rarity: 'legendary', endsAt: w.now - 7 }], lineAscend: { forge: 0, skill: 0, pet: 2, mount: 0 } });
        w.seed(41);
        Pets.tick();
        out.tick.push({ name: 'four_hatch_auto_active_stars2', seed: 41, after: snap(w.S), toasts: w.log.toasts.slice(), quests: w.log.quests.slice() });
        w.now += 1; Pets.tick();
        out.tick.push({ name: 'then_mythic_after_1ms', after: snap(w.S), toasts: w.log.toasts.slice(), quests: w.log.quests.slice() });
        w.now -= 1;
    }
    {
        const full = Array.from({ length: Pets.INV_CAP }, (_, i) => mkPet('Snail', 'common', 1));
        w.reset({ pets: full, activePets: [0, 1, 2], hatching: [{ rarity: 'ultimate', endsAt: w.now - 1 }, { rarity: 'rare', endsAt: w.now - 1 }], eggs: Array.from({ length: 99 }, () => ({ rarity: 'common' })) });
        w.seed(42);
        Pets.tick();
        out.tick.push({ name: 'inventory_full_returns_egg_then_drops', seed: 42, petsLen: w.S.pets.length, eggs: w.S.eggs.map(e => e.rarity), hatching: w.S.hatching.length, toasts: w.log.toasts.slice(), quests: w.log.quests.slice() });
    }
    {
        w.reset({ pets: [mkPet('Dog', 'common', 3)], activePets: [0], hatching: [{ rarity: 'common', endsAt: w.now - 1 }] });
        w.seed(43);
        Pets.tick();
        out.tick.push({ name: 'dup_name_toast', seed: 43, after: snap(w.S), toasts: w.log.toasts.slice() });
    }

    // ---- ⑥ 경험치 · 레벨 ----
    out.xpNeeded = [1, 2, 3, 5, 10, 17, 33, 50, 64, 99, 100].map(l => ({ level: l, xp: Pets.xpNeeded(l) }));
    out.xpValue = RARITIES.map(r => ({ rarity: r, xp: Pets.xpValue(r) }));
    out.addXp = [];
    for (const c of [{ level: 1, xp: 0, add: 500 }, { level: 1, xp: 79, add: 1 }, { level: 10, xp: 0, add: 100000 }, { level: 99, xp: 0, add: 1e9 }, { level: 100, xp: 0, add: 5 }, { level: 1, xp: 0, add: 0.5 }]) {
        w.reset({ pets: [mkPet('Cat', 'common', c.level, { xp: c.xp })] });
        Pets.addXp(0, c.add);
        out.addXp.push({ ...c, level_after: w.S.pets[0].level, xp_after: w.S.pets[0].xp });
        Pets.addXp(9, 1);   // 없는 인덱스는 무시
    }

    // ---- ⑦ 흡수(합성 재료 → 경험치) ----
    {
        w.reset({ pets: [mkPet('Cat', 'common', 1), mkPet('Bear', 'rare', 5), mkPet('Panda', 'epic', 1), mkPet('Genie', 'mythic', 30), mkPet('Dog', 'common', 2)], activePets: [3, 1, 4], eggs: [{ rarity: 'rare' }, { rarity: 'legendary' }, { rarity: 'common' }] });
        const S = w.S;
        const ok = Pets.absorbMaterials(S.pets[0], [S.pets[1], S.pets[3], S.pets[0]], [S.eggs[1], S.eggs[0]]);
        out.absorb = [{ name: 'target0_mats_1_3_self_eggs_1_0', ok, after: snap(S) }];
        out.absorb.push({ name: 'null_target', ok: Pets.absorbMaterials(null, [], []) });
        const ghost = mkPet('Ghost', 'rare', 1);
        out.absorb.push({ name: 'target_not_in_list', ok: Pets.absorbMaterials(ghost, [S.pets[0]], []), after: snap(S) });
    }

    // ---- ⑧ 전투 기여: 등급 기준치 · 레벨 · 별 · 기술트리 ----
    out.baseStat = RARITIES.map(r => { w.reset(); const b = Pets.baseStat(r); return { rarity: r, atk: b.atk, hp: b.hp }; });
    out.petPower = [];
    for (const c of [{ rarity: 'common', level: 1, stars: 0, petDmg: 0, petHp: 0 }, { rarity: 'rare', level: 17, stars: 0, petDmg: 0, petHp: 0 }, { rarity: 'mythic', level: 100, stars: 0, petDmg: 0, petHp: 0 }, { rarity: 'epic', level: 1, stars: 1, petDmg: 0, petHp: 0 }, { rarity: 'legendary', level: 50, stars: 3, petDmg: 25, petHp: 10 }, { rarity: 'ultimate', level: 2, stars: 40, petDmg: 100, petHp: 0 }]) {
        w.reset(); w.tech.petDmg = c.petDmg; w.tech.petHp = c.petHp;
        const p = Pets.petPower(mkPet('X', c.rarity, c.level, { stars: c.stars }));
        out.petPower.push({ ...c, atk: big(p.atk), hp: big(p.hp), levelMult: Pets.levelMult({ level: c.level }) });
    }
    {
        const subs = [{ key: 'critCh', label: '치명타 확률', value: 3.5 }, { key: 'hpPct', label: '체력', value: 12 }];
        w.reset({ pets: [mkPet('Cat', 'common', 1, { subs }), mkPet('Genie', 'mythic', 10, { stars: 1, subs: [{ key: 'atkSpd', label: '공격 속도', value: 7.7 }] }), mkPet('Bear', 'rare', 1), mkPet('Dog', 'common', 1)], activePets: [1, 0, 7] });
        const b = Pets.activeBonus();
        out.activeBonus = { atk: big(b.atk), hp: big(b.hp), subs: b.subs.map(s => ({ key: s.key, value: s.value })) };
        w.reset(); const z = Pets.activeBonus();
        out.activeBonusEmpty = { atk: big(z.atk), hp: big(z.hp), subs: z.subs.length };
    }

    // ---- ⑨ 출전 토글 ----
    {
        w.reset({ pets: [0, 1, 2, 3, 4].map(i => mkPet('P' + i, 'common', 1)) });
        out.toggle = [];
        for (const i of [0, 1, 2, 3, 1, 3, 9, 0]) out.toggle.push({ idx: i, can: Pets.canActivate(i), ok: Pets.toggleActive(i), active: w.S.activePets.slice() });
    }

    // ---- ⑩ 합성 ----
    out.merge = [];
    {
        // 여분 4 · 키운 개체 1(level 2) · 출전 1 · 별 1 → 뒤에서 3개 소모
        w.reset({ pets: [mkPet('Snail', 'common', 1), mkPet('Cat', 'common', 2), mkPet('Dog', 'common', 1), mkPet('Mouse', 'common', 1), mkPet('Bear', 'rare', 1), mkPet('Turtle', 'common', 1), mkPet('Chicken', 'common', 1, { stars: 1 }), mkPet('Snail', 'common', 1)], activePets: [4, 2, 7] });
        const pre = { can: Pets.canMerge('common'), spare: Pets.spareIdxsOfRarity('common'), dupes: Pets.dupesOfRarity('common') };
        const ok = Pets.merge('common');
        out.merge.push({ name: 'spares_from_tail_keeps_active_and_grown', pre, ok, after: snap(w.S), toasts: w.log.toasts.slice(), quests: w.log.quests.slice(), canAgain: Pets.canMerge('common') });
    }
    {
        // 구세이브 dupes 숫자 먼저 소진 · 개체는 남는다
        w.reset({ pets: [mkPet('Bear', 'rare', 1, { dupes: 2 }), mkPet('Spider', 'rare', 1), mkPet('Ostrich', 'rare', 1)] });
        const pre = { can: Pets.canMerge('rare'), dupes: Pets.dupesOfRarity('rare') };
        const ok = Pets.merge('rare');
        out.merge.push({ name: 'legacy_dupes_first', pre, ok, after: snap(w.S) });
    }
    {
        w.reset({ pets: [mkPet('Genie', 'mythic', 1), mkPet('Genie', 'mythic', 1), mkPet('Genie', 'mythic', 1)] });
        out.merge.push({ name: 'mythic_never', can: Pets.canMerge('mythic'), ok: Pets.merge('mythic'), petsLen: w.S.pets.length });
        w.reset({ pets: [mkPet('Snail', 'common', 1), mkPet('Snail', 'common', 1)] });
        out.merge.push({ name: 'two_spares_not_enough', can: Pets.canMerge('common'), ok: Pets.merge('common') });
        w.reset({ pets: [mkPet('Snail', 'common', 1), mkPet('Snail', 'common', 1), mkPet('Snail', 'common', 1)], eggs: Array.from({ length: 100 }, () => ({ rarity: 'common' })) });
        out.merge.push({ name: 'egg_cap_blocks', can: Pets.canMerge('common'), ok: Pets.merge('common'), petsLen: w.S.pets.length, toasts: w.log.toasts.slice() });
    }

    // ---- ⑪ rollSubs 자체(순서 · 소수 한 자리) ----
    out.rollSubs = [];
    for (const s of [1, 2, 3]) { w.seed(s); out.rollSubs.push({ seed: s, subs: U.rollSubs(2).map(x => ({ key: x.key, label: x.label, value: x.value })) }); }
    w.seed(4); out.rollSubs.push({ seed: 4, count: 13, subs: U.rollSubs(13).map(x => ({ key: x.key, value: x.value })) });
    w.seed(5); out.rollSubs.push({ seed: 5, count: 20, subs: U.rollSubs(20).map(x => ({ key: x.key, value: x.value })) });
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
    if (!fs.existsSync(path.join(src, 'web', 'js', 'pets.js'))) { console.error('정본이 없다: ' + src + ' (git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)'); return 2; }
    const text = JSON.stringify(build(src), null, 1) + '\n';
    if (check) {
        const cur = fs.existsSync(out) ? fs.readFileSync(out, 'utf8') : '';
        if (cur !== text) { console.error('✗ pet_vectors: ' + path.relative(ROOT, out) + ' 이 정본과 다르다 — node tools/pet_vectors.js 로 다시 뽑아라'); return 1; }
        console.log('✓ pet_vectors: ' + path.relative(ROOT, out) + ' 이 정본과 같다');
        return 0;
    }
    fs.writeFileSync(out, text);
    console.log('✓ pet_vectors → ' + path.relative(ROOT, out) + ' (' + text.length + ' bytes)');
    return 0;
}

if (require.main === module) process.exit(main(process.argv.slice(2)));
module.exports = { build, mulberry32 };
