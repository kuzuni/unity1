#!/usr/bin/env node
'use strict';
// ===== T7 원작 전투 시뮬레이터 — 정본 combat.js 를 «그대로» 돌려 판 단위 기대값을 뽑는다 =====
//
// 왜: 유니티 Core `Battle`(Assets/Scripts/Core/Battle/Battle.cs)은 원작 `web/js/combat.js` 의 이식이다.
//     «비슷하게» 가 아니라 «같은 시드 → 같은 처치 시각·같은 드랍» 을 EditMode `BattleTests` 가 단언한다.
//     그 기대값을 여기서 뽑는다 — 정본 파일을 node `vm` 에 올리고(수정 0) 바깥 모듈(Scene3D·UI·SFX·Forge·Skills·
//     Dungeons·TechTree·saveGame)만 스텁으로 갈아 끼운다. 스텁은 «호출됐다» 를 이벤트로 남긴다.
//
// 사용: node tools/sim/sim_combat.js [<wwwww 체크아웃>=.wwwww-src] [--out tools/sim/expected]
//       node tools/sim/sim_combat.js --self-test
//
// 출력(시나리오마다 tools/sim/expected/combat_<이름>.json):
//   scenario  — 해석이 끝난 입력(영웅 스탯 · 무기 · 스킬 값 · 진행 · 던전 · 시드). C# 은 이것만 읽는다(공식 중복 없음).
//   ticks     — 돌린 틱 수. rounds — 판(스테이지 클리어 + 사망) 수.
//   prefix    — 앞 PREFIX_TICKS 틱의 이벤트 줄 전부(`t|kind|id|v|n|flag|tag`) — 어긋나면 «어디서» 를 바로 읽는다.
//   windows   — 100틱 창마다 {t0, n, h(FNV-1a 32 · 이벤트 줄 연결), hp(영웅 hp 양자화)} — 전 구간을 값싸게 대조.
//   kills     — 처치마다 {t, id, boss}. drops — 재화가 바뀐 틱마다 [t, Δ코인, Δ해머](처치 드랍 + 첫 클리어 보너스). outcomes — 판 결과 {t, kind, key, label}.
//   final     — 끝난 시점의 진행·재화·hp.
//
// 결정론: Math.random 을 mulberry32(시드)로 갈아 끼운다(C# `Rng.Mulberry` 와 같은 식) · U.now 는 틱 시각(ms) 을 준다.
// 양자화: Big 은 `floor(m*1e6+0.5)e<e>` · 작은 수(|v|<1e9)는 `floor(v*1e6+0.5)` · 큰 수는 Big 으로 — 양쪽 언어의 pow 가
//         마지막 비트에서 갈릴 수 있어 «정확히 같은 문자열» 대신 6자리로 비교한다(처치 시각·순서는 정수라 그대로).
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const PREFIX_TICKS = 600;   // 60초
const WINDOW = 100;         // 10초

// 정본 파일 로드 순서(index.html 순서 중 전투 엔진이 기대는 것만). skills/forge/dungeons/techtree 는 스텁.
const LOAD_ORDER = ['util.js', 'bignum.js', 'gamedata.js', 'state.js', 'combat.js'];

// C# Forge.Core.Data.Mulberry32 와 같은 식(원작 tools/shot-summon-result.js).
function mulberry32(seed) {
    let a = seed >>> 0;
    return function () {
        a = (a + 0x6D2B79F5) | 0;
        let t = Math.imul(a ^ (a >>> 15), 1 | a);
        t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
        return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
}

function fnv1a(str, h = 0x811c9dc5) {
    const bytes = Buffer.from(str, 'utf8');
    for (let i = 0; i < bytes.length; i++) { h ^= bytes[i]; h = Math.imul(h, 0x01000193) >>> 0; }
    return h >>> 0;
}

// ── 시나리오 ────────────────────────────────────────────────────────────────
// 영웅 스탯은 원작 Forge.heroStats() 의 «버프 제외» 결과 꼴(atk·hp 는 Big 이지만 여기선 수로 적고 양쪽이 Big.of 한다).
// 스킬 값은 원작 Skills.dmg/healAmt/buffAtk 의 식(등급 기준치 × mult × 레벨 배율 · 승천 0)으로 여기서 «해석» 해 JSON 에 넣는다.
const SCENARIOS = [
    {
        name: 'melee_ch1', seed: 1,
        hero: { atk: 100, hp: 1000, critCh: 15, critDmg: 150, attacksPerSec: 1.1, dblAtk: 10, block: 5, hpRegen: 0.5, lifesteal: 2, meleeDmg: 10, rangedDmg: 0, skillDmg: 20, skillCd: 10 },
        weapon: 'sword', skills: [['powerStrike', 1], ['whirlwind', 2], ['warCry', 1], ['blessing', 1]],
        techSkillDmgMult: 1, progress: { chapter: 1, stage: 1, difficulty: 0 }, dungeon: null,
        rounds: 100, maxTicks: 40000,
    },
    {
        name: 'ranged_ch3', seed: 2,
        hero: { atk: 900, hp: 6000, critCh: 30, critDmg: 220, attacksPerSec: 1.32, dblAtk: 25, block: 12, hpRegen: 1.5, lifesteal: 4, meleeDmg: 0, rangedDmg: 18, skillDmg: 35, skillCd: 25 },
        weapon: 'bow', skills: [['fireball', 3], ['lightning', 2], ['sanctuary', 1], ['timeWarp', 1]],
        techSkillDmgMult: 1.15, progress: { chapter: 3, stage: 4, difficulty: 0 }, dungeon: null,
        rounds: 100, maxTicks: 40000,
    },
    {
        name: 'dungeon_tier', seed: 3,
        hero: { atk: 3e20, hp: 2e21, critCh: 45, critDmg: 300, attacksPerSec: 1.54, dblAtk: 40, block: 20, hpRegen: 2.5, lifesteal: 6, meleeDmg: 30, rangedDmg: 0, skillDmg: 50, skillCd: 40 },
        weapon: 'stoneSpear', skills: [['execution', 5], ['apocalypse', 2], ['divineShield', 1], ['warCry', 4]],
        techSkillDmgMult: 1.3, progress: { chapter: 24, stage: 8, difficulty: 0 },
        // 던전 «침공» 3단계 · 3웨이브 — Dungeons.monsterHp 는 해금 챕터 6 기준(원작 식 55·5.6^(u-1)·1.35^(s-1)).
        dungeon: { id: 'invasion', stage: 3, waves: 3, unlockChapter: 6, theme: 'dungeon_invasion' },
        rounds: 100, maxTicks: 40000,
    },
];

function resolveSkills(ctx, list) {
    const SKILL_DEFS = vm.runInContext('SKILL_DEFS', ctx);
    const BD = vm.runInContext('SKILL_BASE_DMG', ctx), BH = vm.runInContext('SKILL_BASE_HEAL', ctx), BB = vm.runInContext('SKILL_BASE_BUFF_ATK', ctx);
    const out = {};
    for (const [id, level] of list) {
        const d = SKILL_DEFS.find(x => x.id === id);
        if (!d) throw new Error('없는 스킬 ' + id);
        const lm = 1 + 0.15 * (level - 1);
        out[id] = { level, dmg: BD[d.rarity] * (d.mult || 0) * lm, heal: BH[d.rarity] * (d.mult || 0) * lm, buff: BB[d.rarity] * (d.mult || 0) * lm };
    }
    return out;
}

// ── 컨텍스트 ────────────────────────────────────────────────────────────────
function makeContext(src) {
    const webjs = path.join(src, 'web', 'js');
    if (!fs.existsSync(webjs)) throw new Error(`정본이 없다: ${webjs} (git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)`);
    const sandbox = { console, DEFAULT_AVATAR: '🛡️', localStorage: { getItem() { return null; }, setItem() {}, removeItem() {} } };
    sandbox.window = sandbox;
    vm.createContext(sandbox);
    for (const f of LOAD_ORDER) vm.runInContext(fs.readFileSync(path.join(webjs, f), 'utf8'), sandbox, { filename: f });
    return sandbox;
}

function run(src, sc, dump) {
    const ctx = makeContext(src);
    const rnd = mulberry32(sc.seed);
    vm.runInContext('Math.random = __rnd', Object.assign(ctx, { __rnd: rnd }));
    const Big = vm.runInContext('Big', ctx);
    const U = vm.runInContext('U', ctx);
    const Combat = vm.runInContext('Combat', ctx);
    const SKILL_DEFS = vm.runInContext('SKILL_DEFS', ctx);
    const skillVals = resolveSkills(ctx, sc.skills);

    // 시각 — 틱 시각(ms). start() 는 0.
    let nowMs = 0;
    U.now = () => nowMs;

    // 이벤트
    let tick = 0;
    const events = [];
    const q = v => {
        if (v && typeof v === 'object' && 'm' in v) return `${Math.floor(v.m * 1e6 + 0.5)}e${v.e}`;
        v = Number(v);
        if (Math.abs(v) < 1e9) return String(Math.floor(v * 1e6 + 0.5));
        return q(Big.of(v));
    };
    const emit = (kind, id = 0, v = null, n = 0, flag = false, tag = '') => {
        events.push({ t: tick, kind, id: id | 0, v: v === null ? '' : q(v), n: q(n), flag: !!flag, tag: String(tag) });
    };

    // 상태 — 원작 defaultState() 그대로 세우고 시나리오 칸만 덮는다.
    vm.runInContext('S = defaultState()', ctx);
    const S = vm.runInContext('S', ctx);
    S.chapter = sc.progress.chapter; S.stage = sc.progress.stage; S.difficulty = sc.progress.difficulty;
    S.bestChapter = S.chapter; S.bestStage = S.stage; S.bestDifficulty = S.difficulty;
    S.equippedSkills = sc.skills.map(x => x[0]);
    S.skills = {}; for (const [id, level] of sc.skills) S.skills[id] = { level, dupes: 0, stars: 0 };
    S.autoCast = true;
    S.equipment.weapon = { slot: 'weapon', wtype: sc.weapon };
    let dungeon = null;
    if (sc.dungeon) {
        dungeon = Object.assign({}, sc.dungeon, { monsterHp: 55 * Math.pow(5.6, sc.dungeon.unlockChapter - 1) * Math.pow(1.35, sc.dungeon.stage - 1) });
        S.dungeonRun = { id: dungeon.id, stage: dungeon.stage, waves: dungeon.waves };
    }

    // 스텁 — 바깥 모듈. 호출 흔적만 남긴다(원작 함수 시그니처 그대로).
    const stubs = {
        Forge: {
            heroStats() {
                let buff = Big.ZERO;
                for (const b of Combat.buffs) if (b.buff && b.buff.atkFlat) buff = buff.add(b.buff.atkFlat);
                const h = sc.hero;
                return Object.assign({}, h, { atk: Big.of(h.atk).add(buff), hp: Big.of(h.hp) });
            },
        },
        Skills: {
            def(id) { return SKILL_DEFS.find(d => d.id === id); },
            dmg(id) { return Big.of(skillVals[id].dmg); },
            healAmt(id) { return Big.of(skillVals[id].heal); },
            buffAtk(id) { return Big.of(skillVals[id].buff); },
        },
        TechTree: { skillDmgMult() { return sc.techSkillDmgMult; } },
        Dungeons: {
            get run() { return S.dungeonRun || null; },
            DEFAULT_WAVES: 3,
            def() { return { theme: dungeon ? dungeon.theme : 'none' }; },
            monsterHp() { return dungeon.monsterHp; },
            onClear() { S.dungeonRun = null; emit('dungeonClear'); },
            onFail() { S.dungeonRun = null; emit('dungeonFail'); },
        },
        Scene3D: {
            walking: false, scene: {}, BOSS_IMPACT: 1.55, BOSS_SPAWN_X: 1.75, enemyMap: new Map(),
            enemyHalfW() { return 0.5; },
            heroRevive() { emit('heroRevive'); },
            clearEnemies() { emit('clearEnemies'); },
            setTheme(t) { emit('theme', 0, null, 0, false, t); },
            setChapterTheme(ch) { emit('theme', 0, null, 0, false, 'ch:' + ch); },
            bossEntrance() { emit('bossEntrance'); },
            spawnEnemy(e) { emit('spawn', e.id, e.hp, e.x, e.isBoss); },
            enemyAttack(id) { emit('enemyAttack', id); },
            heroAttack(id) { emit('heroAttack', id); },
            skillEffect(fx, color, ids) { emit('skillEffect', 0, null, ids.length, false, fx); },
            hitEnemy(id, dmg, crit, kind, kill) { emit('hit', id, dmg, kill ? 1 : 0, crit, kind || ''); },
            killEnemy(id, isBoss) { emit('kill', id, null, 0, isBoss); },
            shake(a) { emit('shake', 0, null, a); },
            heroHit(ratio, dmg) { emit('heroHit', 0, dmg, ratio); },
            heroDown() { emit('heroDown'); },
            deathFade(msg) { emit('deathFade', 0, null, 0, false, msg); return true; },
            deathWipeEnemies() { emit('deathWipe'); },
            sceneCut(fn) { emit('sceneCut'); fn(); },
        },
        UI: {
            els: { passModal: { classList: { contains() { return true; } } } },
            renderMenu() {}, updateStageLabel() { emit('stageLabel', 0, null, 0, false, vm.runInContext('stageName()', ctx)); },
            updateWavePips(w) { emit('wavePips', 0, null, w); }, updateSkillBar() {}, renderTopBar() {}, renderPass() {},
            floatTextAtHero(text, cls) { emit('float', 0, null, 0, false, text); },
            floatLoot(text) { emit('loot', 0, null, 0, false, text); },
            toast(text, lane) { emit('toast', 0, null, 0, false, text); },
            skillCutin(d) { emit('skillCutin', 0, null, 0, false, d.id); },
            skillFlash(color) { emit('skillFlash', 0, null, 0, false, color); },
        },
        SFX: { hit() {}, setMusicMode(m) { emit('music', 0, null, 0, false, m); } },
        saveGame() { emit('save'); },
    };
    Object.assign(ctx, stubs);

    // ── 돌리기 ──
    const kills = [], outcomes = [];
    let rounds = 0, finishAt = -1;
    const origOnKill = Combat.onKill.bind(Combat);
    Combat.onKill = function (e) { origOnKill(e); kills.push({ t: tick, id: e.id, boss: !!e.isBoss }); };
    const drops = []; // 틱 단위 재화 차분 [t, Δ코인, Δ해머] — 처치 코인·해머 + 첫 클리어 보너스가 한 틱에 섞인다
    const origStageClear = Combat.stageClear.bind(Combat);
    Combat.stageClear = function () { const key = vm.runInContext('stageKey()', ctx), inDungeon = !!S.dungeonRun; origStageClear(); rounds++; outcomes.push({ t: tick, kind: inDungeon ? 'dungeonClear' : 'clear', key, label: vm.runInContext('stageName()', ctx) }); };
    const origOnDefeat = Combat.onDefeat.bind(Combat);
    Combat.onDefeat = function () { const key = vm.runInContext('stageKey()', ctx); origOnDefeat(); rounds++; outcomes.push({ t: tick, kind: 'defeat', key, label: vm.runInContext('stageName()', ctx) }); };

    Combat.start();
    const startEvents = events.length;
    while (tick < sc.maxTicks && rounds < sc.rounds) {
        tick++;
        nowMs = tick * 100;
        const c0 = S.coins, h0 = S.hammers;
        Combat.tick(0.1);
        if (S.coins !== c0 || S.hammers !== h0) drops.push([tick, S.coins - c0, S.hammers - h0]);
        // 던전 클리어 팝업 [보상 수령] — UI 가 10틱(1초) 뒤에 누른다고 본다(C# 테스트도 같은 틱의 Tick() 뒤에 FinishDungeonClear).
        if (finishAt === tick) { Combat.finishDungeonClear(); finishAt = -1; }
        if (Combat.phase === 'dungeonClear' && finishAt < 0) finishAt = tick + 10;
    }

    // ── 정리 ──
    const HASH_TAGLESS = new Set(['float', 'loot', 'toast']);
    const line = e => `${e.t}|${e.kind}|${e.id}|${e.v}|${e.n}|${e.flag ? 1 : 0}|${e.tag}`;
    const hline = e => `${e.t}|${e.kind}|${e.id}|${e.v}|${e.n}|${e.flag ? 1 : 0}|${HASH_TAGLESS.has(e.kind) ? '' : e.tag}`;
    if (dump) { for (const e of events) if (e.t >= dump[1] && e.t < dump[2]) console.log(line(e)); }
    const prefix = events.filter(e => e.t <= PREFIX_TICKS).map(line);
    const windows = [];
    for (let t0 = 0; t0 <= tick; t0 += WINDOW) {
        const ws = events.filter(e => e.t >= t0 && e.t < t0 + WINDOW);
        let h = 0x811c9dc5;
        for (const e of ws) h = fnv1a(hline(e) + '\n', h);
        windows.push({ t0, n: ws.length, h: h.toString(16).padStart(8, '0') });
    }
    const final = {
        chapter: S.chapter, stage: S.stage, difficulty: S.difficulty, bestChapter: S.bestChapter, bestStage: S.bestStage, bestDifficulty: S.bestDifficulty,
        kills: S.kills, coins: S.coins, hammers: S.hammers, heroHp: q(Combat.hero.hp), heroMaxHp: q(Combat.hero.maxHp), phase: Combat.phase, wave: Combat.wave,
        combatPower: q(Combat.combatPower()), clearedBosses: Object.keys(S.clearedBosses).length,
    };
    return {
        scenario: { name: sc.name, seed: sc.seed, hero: sc.hero, weapon: sc.weapon, skills: sc.skills.map(x => x[0]), skillVals, techSkillDmgMult: sc.techSkillDmgMult, progress: sc.progress, dungeon, rounds: sc.rounds, maxTicks: sc.maxTicks, prefixTicks: PREFIX_TICKS, window: WINDOW, finishDelayTicks: 10 },
        ticks: tick, rounds, startEvents, eventCount: events.length, prefix, windows, kills, drops, outcomes, final,
    };
}

function selfTest(src) {
    const a = run(src, SCENARIOS[0]);
    const b = run(src, SCENARIOS[0]);
    const same = JSON.stringify(a) === JSON.stringify(b);
    if (!same) throw new Error('같은 시드 두 번이 다르다 — 결정론이 깨졌다');
    if (a.kills.length < 10) throw new Error('처치가 너무 적다: ' + a.kills.length);
    if (a.outcomes.length < 3) throw new Error('판 결과가 너무 적다: ' + a.outcomes.length);
    if (!a.kills.some(k => k.boss)) throw new Error('보스 처치가 없다');
    const c = run(src, Object.assign({}, SCENARIOS[0], { seed: 99 }));
    if (JSON.stringify(c.windows) === JSON.stringify(a.windows)) throw new Error('시드를 바꿨는데 같다');
    const d = run(src, SCENARIOS[2]);
    if (!d.prefix.some(l => l.includes('|dungeonClear|')) && !d.prefix.some(l => l.includes('|dungeonFail|'))) throw new Error('던전 시나리오에 던전 결과가 없다');
    if (!d.outcomes.some(o => o.key.startsWith('d1:'))) throw new Error('티어 상승(d1:) 이 없다 — 24-8 에서 100판이면 헬 전 티어에 닿아야 한다');
    console.log(`self-test ✓ 결정론 · 처치 ${a.kills.length} · 판 ${a.outcomes.length} · 던전 · 티어 상승`);
}

function main() {
    const args = process.argv.slice(2);
    let src = '.wwwww-src', out = path.join(__dirname, 'expected'), self = false, dump = null;
    for (let i = 0; i < args.length; i++) {
        if (args[i] === '--out') out = args[++i];
        else if (args[i] === '--self-test') self = true;
        else if (args[i] === '--dump') dump = [args[++i], +args[++i], +args[++i]];
        else src = args[i];
    }
    if (self) { selfTest(src); return; }
    if (dump) {
        const sc = SCENARIOS.find(x => x.name === dump[0]);
        if (!sc) throw new Error('없는 시나리오 ' + dump[0]);
        const r = run(src, sc, dump);
        return;
    }
    fs.mkdirSync(out, { recursive: true });
    for (const sc of SCENARIOS) {
        const r = run(src, sc);
        const p = path.join(out, `combat_${sc.name}.json`);
        fs.writeFileSync(p, JSON.stringify(r, null, 0) + '\n');
        console.log(`${sc.name}: 틱 ${r.ticks} · 판 ${r.rounds} · 처치 ${r.kills.length} · 이벤트 ${r.eventCount} · 끝 ${r.final.chapter}-${r.final.stage} d${r.final.difficulty} → ${path.relative(process.cwd(), p)} (${(fs.statSync(p).size / 1024).toFixed(0)}KB)`);
    }
}

if (require.main === module) main();
module.exports = { run, SCENARIOS, mulberry32, fnv1a };
