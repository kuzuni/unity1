#!/usr/bin/env node
// ============================================================================
// skill_vectors.js — 정본 `web/js/skills.js` 를 **그대로** 돌려 대조 벡터를 뽑는다 (T17).
// ----------------------------------------------------------------------------
// Core `SkillSystem`(C#)이 원작 `Skills` 와 «같은 시드 → 같은 결과» 인지 EditMode `SkillTests` 가 이 파일의 출력
// (`Assets/Tests/EditMode/Vectors/skill_vectors.json`)과 대조한다. T16 `pet_vectors.js` 와 같은 방식 — 원작 파일
// (util·bignum·gamedata·balance-data·ascension·skills)을 vm 컨텍스트에 그대로 싣고, 원작이 밖에서 받는 것
// (S · TechTree · UI · SFX · Quests · Combat · saveGame · Math.random)만 stub 으로 꽂는다. 규칙은 한 줄도 다시 쓰지 않는다.
//
// 난수 = mulberry32(Core `Mulberry32` 와 같은 식). `U.weightedPick`(등급) → `U.choice`(종) 순서로 난수 둘.
//
// 사용:
//   node tools/skill_vectors.js                 # .wwwww-src → Assets/Tests/EditMode/Vectors/skill_vectors.json
//   node tools/skill_vectors.js --check         # 다시 뽑아 현재 파일과 같은지만 본다 (rc 1 = 다르다)
//   node tools/skill_vectors.js --src <wwwww 체크아웃> --out <파일>
// ============================================================================
'use strict';
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const ROOT = path.resolve(__dirname, '..');
const DEFAULT_SRC = path.join(ROOT, '.wwwww-src');
const DEFAULT_OUT = path.join(ROOT, 'Assets', 'Tests', 'EditMode', 'Vectors', 'skill_vectors.json');
const LOAD_ORDER = ['util.js', 'bignum.js', 'gamedata.js', 'balance-data.js', 'ascension.js', 'skills.js'];

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
    sandbox.Math = Object.create(Math);            // Math.random 만 바꿔 심는다
    sandbox.Date = Date;
    vm.createContext(sandbox);
    for (const f of LOAD_ORDER) {
        const p = path.join(src, 'web', 'js', f);
        vm.runInContext(fs.readFileSync(p, 'utf8'), sandbox, { filename: f });
    }
    const w = Object.assign(sandbox, vm.runInContext('({ U, Skills, Ascension, RARITIES, SKILL_DEFS, Big })', sandbox));
    w.log = { gacha: [], quests: [], saves: 0, recalc: 0, renders: 0 };
    w.UI = { renderSkillBar() { w.log.renders++; } };
    w.SFX = { gacha: r => w.log.gacha.push(r) };
    w.Quests = { bump: (k, n) => w.log.quests.push([k, n]) };
    w.Combat = { recalcHero: () => { w.log.recalc++; } };
    w.saveGame = () => { w.log.saves++; };
    w.tech = { skillSummonCost: 0, skillPassiveDmg: 0, skillPassiveHp: 0 };   // 기술트리 pct(%) 를 직접 준다(원작 techtree.js 의 식 그대로)
    w.TechTree = {
        pct: k => w.tech[k] || 0,
        skillSummonCostMult() { return Math.max(0.1, 1 - this.pct('skillSummonCost') / 100); },
        skillPassiveDmgMult() { return 1 + this.pct('skillPassiveDmg') / 100; },
        skillPassiveHpMult() { return 1 + this.pct('skillPassiveHp') / 100; },
    };
    w.seed = s => { w.Math.random = mulberry32(s); };
    w.reset = (over) => {
        w.S = Object.assign({ gems: 0, tickets: 0, summonCount: 0, skills: {}, equippedSkills: [], lineAscend: { forge: 0, skill: 0, pet: 0, mount: 0 } }, over || {});
        w.log.gacha.length = 0; w.log.quests.length = 0; w.log.saves = 0; w.log.recalc = 0; w.log.renders = 0;
        w.tech = { skillSummonCost: 0, skillPassiveDmg: 0, skillPassiveHp: 0 };
    };
    return w;
}

const big = b => ({ m: b.m, e: b.e });
// S.skills 는 삽입 순서가 곧 upgradeAll·quickEquip·ownedPassive 의 순회 순서다 — 배열로 남긴다.
const skillsOf = S => Object.entries(S.skills).map(([id, v]) => [id, { level: v.level, dupes: v.dupes, stars: v.stars || 0 }]);
const snap = S => ({ skills: skillsOf(S), equipped: S.equippedSkills.slice(), summonCount: S.summonCount || 0, gems: S.gems, tickets: S.tickets });
const owned = (list) => { const o = {}; for (const [id, lv, du, st] of list) o[id] = { level: lv, dupes: du, stars: st || 0 }; return o; };

function build(src) {
    const w = makeWorld(src);
    const { Skills, RARITIES, SKILL_DEFS } = w;
    const out = { note: '정본 skills.js 를 mulberry32 시드로 돌린 대조 벡터 — tools/skill_vectors.js 가 만든다 · 손으로 고치지 않는다', consts: {} };

    // ---- 상수(원작 skills.js 코드 상수 · Core SkillRules 가 같은 값이어야 한다) ----
    for (const k of ['SUMMON_TICKET_COST', 'SUMMON_GEM_COST', 'MAX_LEVEL', 'MAX_ACTIVE']) out.consts[k] = Skills[k];
    out.consts.STAR_MULT = w.Ascension.STAR_MULT;
    out.consts.skillIds = SKILL_DEFS.map(d => d.id);

    // ---- ① 티켓 비용(기술트리 %) ----
    out.ticketCost = [];
    for (const pct of [0, 1, 37, 99, 200]) for (const count of [1, 5, 25, 75]) { w.reset(); w.tech.skillSummonCost = pct; out.ticketCost.push({ count, pct, cost: Skills.ticketCost(count) }); }
    w.reset(); out.ticketCostDefault = Skills.ticketCost();

    // ---- ② 소환 레벨 · 확률표 ----
    out.summonLevel = [0, 1, 4, 5, 9, 10, 49, 50, 494, 495, 499, 500, 1000].map(c => { w.reset({ summonCount: c }); return { summonCount: c, level: Skills.summonLevel() }; });
    out.rates = [1, 2, 3, 4, 20, 21, 44, 50, 61, 80, 81, 99, 100, 0, 150].map(l => { w.reset(); const r = Skills.rates(l); return { level: l, rates: RARITIES.map(k => r[k]) }; });
    w.reset({ summonCount: 123 }); out.ratesDefault = { summonCount: 123, rates: RARITIES.map(k => Skills.rates()[k]) };

    // ---- ③ canSummon ----
    out.canSummon = [];
    for (const c of [{ gems: 199, tickets: 31, useGems: true, count: 1 }, { gems: 200, tickets: 0, useGems: true, count: 1 }, { gems: 999, tickets: 0, useGems: true, count: 5 }, { gems: 1000, tickets: 0, useGems: true, count: 5 },
        { gems: 0, tickets: 31, useGems: false, count: 1 }, { gems: 0, tickets: 32, useGems: false, count: 1 }, { gems: 0, tickets: 159, useGems: false, count: 5, pct: 1 }, { gems: 0, tickets: 158, useGems: false, count: 5, pct: 1 }]) {
        w.reset({ gems: c.gems, tickets: c.tickets }); w.tech.skillSummonCost = c.pct || 0;
        out.canSummon.push({ ...c, pct: c.pct || 0, can: Skills.canSummon(c.useGems, c.count) });
    }

    // ---- ④ 소환(배치) — 등급 추첨 → 종 선택 · 신규는 장착 슬롯이 비면 자동 장착 · 중복은 조각 ----
    out.summon = [];
    const cases = [
        { name: 'x5_tickets_fresh', seed: 14, useGems: false, count: 5, tickets: 1000, gems: 0, summonCount: 0, skills: [], equipped: [], ascendSkill: 0 },
        { name: 'x25_gems_lv100_stars2', seed: 3, useGems: true, count: 25, tickets: 0, gems: 100000, summonCount: 495, skills: [], equipped: [], ascendSkill: 2 },
        { name: 'x5_with_owned_and_full_slots', seed: 9, useGems: false, count: 5, tickets: 10000, gems: 0, summonCount: 100, skills: [['fireball', 3, 1, 0], ['powerStrike', 1, 0, 0], ['meteor', 7, 2, 1]], equipped: ['fireball', 'powerStrike', 'meteor'], ascendSkill: 1 },
        { name: 'x1_ticket_short', seed: 5, useGems: false, count: 1, tickets: 31, gems: 0, summonCount: 0, skills: [], equipped: [], ascendSkill: 0 },
        { name: 'x5_gems_short', seed: 6, useGems: true, count: 5, tickets: 0, gems: 999, summonCount: 0, skills: [], equipped: [], ascendSkill: 0 },
        { name: 'x75_tickets_pct37', seed: 21, useGems: false, count: 75, tickets: 100000, gems: 0, summonCount: 200, skills: [['warCry', 2, 0, 0]], equipped: ['warCry'], ascendSkill: 0, pct: 37 },
        { name: 'x1_exact_ticket', seed: 22, useGems: false, count: 1, tickets: 32, gems: 0, summonCount: 4, skills: [], equipped: [], ascendSkill: 0 },
        { name: 'x5_two_equipped_one_slot_left', seed: 23, useGems: false, count: 5, tickets: 500, gems: 5, summonCount: 40, skills: [['blessing', 1, 0, 0], ['bolt_unknown_ignored', 1, 0, 0]], equipped: ['blessing'], ascendSkill: 0 },
    ];
    for (const c of cases) {
        w.reset({ gems: c.gems, tickets: c.tickets, summonCount: c.summonCount, skills: owned(c.skills), equippedSkills: c.equipped.slice(), lineAscend: { forge: 0, skill: c.ascendSkill, pet: 0, mount: 0 } });
        w.tech.skillSummonCost = c.pct || 0; w.seed(c.seed);
        const pre = { level: Skills.summonLevel(), can: Skills.canSummon(c.useGems, c.count), cost: c.useGems ? Skills.SUMMON_GEM_COST * c.count : Skills.ticketCost(c.count) };
        const r = Skills.summon(c.useGems, c.count);
        out.summon.push({ ...c, pct: c.pct || 0, pre, result: r && { results: r.results.map(x => ({ id: x.def.id, isNew: x.isNew, level: x.level })), count: r.count }, after: snap(w.S), gacha: w.log.gacha.slice(), quests: w.log.quests.slice(), saves: w.log.saves, recalc: w.log.recalc });
    }

    // ---- ⑤ 레벨 배율 · 피해 · 회복 · 버프 (등급별 대표 + 레벨 + 별) ----
    out.values = [];
    const reps = ['powerStrike', 'whirlwind', 'fireball', 'warCry', 'meteor', 'blessing', 'dragonBreath', 'execution', 'sanctuary', 'supernova', 'voidLance', 'timeWarp', 'apocalypse', 'godspear', 'divineShield'];
    for (const id of reps) for (const [level, stars] of [[1, 0], [2, 0], [50, 0], [100, 0], [1, 1], [37, 3], [100, 40]]) {
        w.reset({ skills: owned([[id, level, 0, stars]]) });
        out.values.push({ id, level, stars, levelMult: Skills.levelMult(id), dmg: big(Skills.dmg(id)), heal: big(Skills.healAmt(id)), buff: big(Skills.buffAtk(id)) });
    }
    w.reset(); out.valuesUnowned = { id: 'lightning', levelMult: Skills.levelMult('lightning'), dmg: big(Skills.dmg('lightning')), heal: big(Skills.healAmt('lightning')), buff: big(Skills.buffAtk('lightning')), level: Skills.level('lightning'), defNull: Skills.def('nope') === undefined };

    // ---- ⑥ 조각 · 업그레이드 ----
    out.shards = [1, 2, 9, 10, 11, 29, 30, 31, 59, 60, 61, 99, 100].map(l => ({ level: l, n: Skills.shardsRequired(l) }));
    out.upgrade = [];
    for (const c of [{ name: 'lv1_dupes20_twelve_tries', start: ['fireball', 1, 20, 0], tries: 12 }, { name: 'lv99_caps_at_100', start: ['godspear', 99, 100, 5], tries: 3 }, { name: 'lv29_needs3_has2', start: ['meteor', 29, 2, 0], tries: 2 }]) {
        const id = c.start[0];
        w.reset({ skills: owned([c.start]) });
        const steps = [];
        for (let i = 0; i < c.tries; i++) { const can = Skills.canUpgrade(id); const ok = Skills.upgrade(id); steps.push({ can, ok, level: w.S.skills[id].level, dupes: w.S.skills[id].dupes }); }
        out.upgrade.push({ ...c, id, steps, saves: w.log.saves, recalc: w.log.recalc });
    }
    w.reset(); out.upgradeUnknown = { can: Skills.canUpgrade('nope'), ok: Skills.upgrade('nope') };
    {
        const before = [['fireball', 1, 7, 0], ['meteor', 9, 5, 0], ['whirlwind', 30, 13, 1], ['godspear', 100, 50, 0], ['warCry', 59, 8, 0]];
        w.reset({ skills: owned(before) });
        const count = Skills.upgradeAll();
        out.upgradeAll = { before, count, after: skillsOf(w.S), saves: w.log.saves, recalc: w.log.recalc };
    }

    // ---- ⑦ 빠른 장착(등급 내림 · 레벨 내림 · 동률은 삽입 순서) · 토글 ----
    {
        const skills = [['powerStrike', 40, 0, 0], ['blessing', 2, 0, 0], ['fireball', 9, 0, 0], ['godspear', 1, 0, 0], ['meteor', 2, 0, 0], ['pierceShot', 9, 0, 0], ['apocalypse', 1, 0, 0], ['warCry', 9, 0, 0]];
        w.reset({ skills: owned(skills), equippedSkills: ['powerStrike'] });
        Skills.quickEquip();
        out.quickEquip = { skills, before: ['powerStrike'], after: w.S.equippedSkills.slice(), renders: w.log.renders, recalc: w.log.recalc, saves: w.log.saves };
        w.reset({ skills: owned([['fireball', 1, 0, 0]]) }); Skills.quickEquip();
        out.quickEquipOne = { after: w.S.equippedSkills.slice() };
    }
    {
        const skills = [['powerStrike', 1, 0, 0], ['fireball', 1, 0, 0], ['meteor', 1, 0, 0], ['godspear', 1, 0, 0]];
        w.reset({ skills: owned(skills) });
        const steps = [];
        for (const id of ['powerStrike', 'fireball', 'meteor', 'godspear', 'fireball', 'godspear', 'powerStrike', 'meteor']) steps.push({ id, ok: Skills.toggleEquip(id), equipped: w.S.equippedSkills.slice() });
        out.toggle = { skills, steps, renders: w.log.renders, recalc: w.log.recalc, saves: w.log.saves };
    }

    // ---- ⑧ 보유 패시브(장착 무관) ----
    out.passive = [];
    for (const c of [{ id: 'powerStrike', level: 1, stars: 0, dmgPct: 0, hpPct: 0 }, { id: 'fireball', level: 17, stars: 0, dmgPct: 0, hpPct: 0 }, { id: 'meteor', level: 100, stars: 0, dmgPct: 25, hpPct: 10 }, { id: 'godspear', level: 1, stars: 1, dmgPct: 0, hpPct: 0 }, { id: 'apocalypse', level: 50, stars: 3, dmgPct: 100, hpPct: 33 }, { id: 'voidLance', level: 2, stars: 40, dmgPct: 0, hpPct: 0 }]) {
        w.reset({ skills: owned([[c.id, c.level, 0, c.stars]]) }); w.tech.skillPassiveDmg = c.dmgPct; w.tech.skillPassiveHp = c.hpPct;
        const p = Skills.passiveOf(c.id);
        out.passive.push({ ...c, atk: big(p.atk), hp: big(p.hp) });
    }
    w.reset(); { const p = Skills.passiveOf('nope'); out.passiveUnknown = { atk: big(p.atk), hp: big(p.hp) }; }
    {
        const skills = [['powerStrike', 3, 0, 0], ['godspear', 1, 1, 0], ['fireball', 20, 0, 0], ['blessing', 5, 0, 2]];
        w.reset({ skills: owned(skills), equippedSkills: ['fireball'] }); w.tech.skillPassiveDmg = 12; w.tech.skillPassiveHp = 7;
        const p = Skills.ownedPassive();
        out.ownedPassive = { skills, dmgPct: 12, hpPct: 7, atk: big(p.atk), hp: big(p.hp) };
        w.reset(); const z = Skills.ownedPassive(); out.ownedPassiveEmpty = { atk: big(z.atk), hp: big(z.hp) };
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
    if (!fs.existsSync(path.join(src, 'web', 'js', 'skills.js'))) { console.error('정본이 없다: ' + src + ' (git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)'); return 2; }
    const text = JSON.stringify(build(src), null, 1) + '\n';
    if (check) {
        const cur = fs.existsSync(out) ? fs.readFileSync(out, 'utf8') : '';
        if (cur !== text) { console.error('✗ skill_vectors: ' + path.relative(ROOT, out) + ' 이 정본과 다르다 — node tools/skill_vectors.js 로 다시 뽑아라'); return 1; }
        console.log('✓ skill_vectors: ' + path.relative(ROOT, out) + ' 이 정본과 같다');
        return 0;
    }
    fs.mkdirSync(path.dirname(out), { recursive: true });
    fs.writeFileSync(out, text);
    console.log('✓ skill_vectors → ' + path.relative(ROOT, out) + ' (' + text.length + ' bytes)');
    return 0;
}

if (require.main === module) process.exit(main(process.argv.slice(2)));
module.exports = { build, mulberry32 };
