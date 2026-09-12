#!/usr/bin/env node
// tools/webgl_smoke.js — WebGL 배포 스모크 (ROUTINE T26)
//
// 구운 WebGL(build/WebGL/web 또는 https://kuzuni.github.io/unity1/) 을 headless Chromium 으로 열어
//   ① 로더가 풀려 <html data-forge-ready="1"> 이 찍히는가(Assets/WebGLTemplates/Forge/index.html 의 표식)
//   ② 콘솔 빨강 0 (console.error · pageerror · Build/·StreamingAssets/ 요청 실패)
//   ③ --require 로 준 신호(예: battle = T8 이 forgeSignal('battle') 로 찍는 «전투 진입»)가 도착하는가
// 를 본다. rc 0 = 초록 · 1 = 빨강 · 2 = 인자 오류.
//
//   node tools/webgl_smoke.js --serve build/WebGL/web --require unity-ready --shot ui-screens/webgl_smoke.png
//   node tools/webgl_smoke.js https://kuzuni.github.io/unity1/ --require unity-ready,battle
//   node tools/webgl_smoke.js --self-test        # 템플릿을 가짜 로더로 실제로 열어 이 스크립트와 템플릿을 함께 검사
//
// ⚠ 컨테이너(클라우드 세션)에서는 kuzuni.github.io 가 프록시에 막힌다 — Pages 검사는 CI 러너에서 돈다(ci.yml build-webgl).
//    --self-test 는 네트워크 없이 돈다(로컬 정적 서버 + Chromium). Playwright 는 로컬 node_modules → 전역(npm root -g) 순으로 찾는다.
'use strict';
const fs = require('fs');
const path = require('path');
const http = require('http');
const os = require('os');
const { execSync } = require('child_process');

const REPO = path.resolve(__dirname, '..');
const TEMPLATE = path.join(REPO, 'Assets', 'WebGLTemplates', 'Forge', 'index.html');

// ---------- 인자 ----------
function parseArgs(argv) {
  const o = { url: null, serve: null, require: ['unity-ready'], timeout: 120000, signalTimeout: 60000, settle: 3000, shot: null, selfTest: false, width: 540, height: 960, quiet: false };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    const next = () => { if (i + 1 >= argv.length) throw new Error(`${a} 뒤에 값이 없다`); return argv[++i]; };
    if (a === '--serve') o.serve = next();
    else if (a === '--require') o.require = next().split(',').map(s => s.trim()).filter(Boolean);
    else if (a === '--timeout') o.timeout = Number(next());
    else if (a === '--signal-timeout') o.signalTimeout = Number(next());
    else if (a === '--settle') o.settle = Number(next());
    else if (a === '--shot') o.shot = next();
    else if (a === '--viewport') { const [w, h] = next().split('x').map(Number); o.width = w; o.height = h; }
    else if (a === '--self-test') o.selfTest = true;
    else if (a === '--quiet') o.quiet = true;
    else if (a.startsWith('--')) throw new Error(`모르는 인자: ${a}`);
    else o.url = a;
  }
  return o;
}

// ---------- Playwright 찾기 ----------
function loadPlaywright() {
  try { return require('playwright'); } catch (_) { /* 아래로 */ }
  try {
    const root = execSync('npm root -g', { encoding: 'utf8' }).trim();
    return require(path.join(root, 'playwright'));
  } catch (e) {
    throw new Error('playwright 를 못 찾았다 — `npm i --no-save playwright && npx playwright install --with-deps chromium`');
  }
}

// ---------- 정적 서버 (WebGL 빌드 폴더용) ----------
const MIME = {
  '.html': 'text/html; charset=utf-8', '.js': 'application/javascript', '.mjs': 'application/javascript', '.css': 'text/css',
  '.json': 'application/json', '.wasm': 'application/wasm', '.data': 'application/octet-stream', '.unityweb': 'application/octet-stream',
  '.png': 'image/png', '.jpg': 'image/jpeg', '.ico': 'image/x-icon', '.svg': 'image/svg+xml', '.txt': 'text/plain; charset=utf-8', '.symbols': 'application/octet-stream',
};
function serveDir(dir) {
  const root = path.resolve(dir);
  if (!fs.existsSync(root)) throw new Error(`--serve 폴더가 없다: ${root}`);
  const server = http.createServer((req, res) => {
    let p = decodeURIComponent(new URL(req.url, 'http://x').pathname);
    if (p.endsWith('/')) p += 'index.html';
    const file = path.normalize(path.join(root, p));
    if (!file.startsWith(root) || !fs.existsSync(file) || fs.statSync(file).isDirectory()) { res.writeHead(404); res.end('not found'); return; }
    // 유니티가 .gz/.br 로 구웠는데 폴백을 안 켠 빌드도 열리게 — 실제 호스트(gh-pages)는 이 헤더를 못 주므로
    // 프로젝트는 «압축 폴백(.unityweb)» 을 켜 둔다(ProjectSettings webGLDecompressionFallback 1).
    let ext = path.extname(file).toLowerCase();
    const headers = { 'Cache-Control': 'no-store' };
    if (ext === '.gz' || ext === '.br') {
      headers['Content-Encoding'] = ext === '.gz' ? 'gzip' : 'br';
      ext = path.extname(file.slice(0, -ext.length)).toLowerCase();
    }
    headers['Content-Type'] = MIME[ext] || 'application/octet-stream';
    res.writeHead(200, headers);
    fs.createReadStream(file).pipe(res);
  });
  return new Promise(resolve => server.listen(0, '127.0.0.1', () => resolve({ server, url: `http://127.0.0.1:${server.address().port}/` })));
}

// ---------- 스모크 본체 ----------
async function runSmoke(opts, log) {
  const pw = loadPlaywright();
  const reds = [];   // 빨강 — 하나라도 있으면 실패
  const warns = [];  // 노랑 — 보고만
  const t0 = Date.now();
  let served = null;
  let url = opts.url;
  if (opts.serve) { served = await serveDir(opts.serve); url = served.url; }
  if (!url) throw new Error('URL 또는 --serve <폴더> 가 필요하다');

  const browser = await pw.chromium.launch({
    headless: true,
    args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--no-sandbox', '--disable-dev-shm-usage'],
  });
  const result = { url, ok: false, reds, warns, signals: [], appBox: null, ready: false, ms: 0 };
  try {
    const page = await browser.newPage({ viewport: { width: opts.width, height: opts.height }, deviceScaleFactor: 1 });
    page.on('console', msg => {
      const text = msg.text();
      if (msg.type() === 'error') reds.push(`console.error: ${text}`);
      else if (msg.type() === 'warning') warns.push(text);
      if (!opts.quiet && (msg.type() === 'error' || msg.type() === 'warning')) log(`  [${msg.type()}] ${text.slice(0, 300)}`);
    });
    page.on('pageerror', err => { reds.push(`pageerror: ${err.message}`); if (!opts.quiet) log(`  [pageerror] ${err.message}`); });
    const isBuildAsset = u => /\/(Build|StreamingAssets)\//.test(u) || /\.(loader\.js|framework\.js|wasm|data|unityweb)(\?|$)/.test(u);
    page.on('requestfailed', req => { if (isBuildAsset(req.url())) reds.push(`요청 실패: ${req.url()} (${req.failure() && req.failure().errorText})`); });
    page.on('response', res => { if (res.status() >= 400 && (isBuildAsset(res.url()) || res.request().isNavigationRequest())) reds.push(`HTTP ${res.status()}: ${res.url()}`); });

    log(`열기: ${url} (${opts.width}×${opts.height})`);
    await page.goto(url, { waitUntil: 'domcontentloaded', timeout: opts.timeout });

    // ① 로딩 완료 표식 (또는 로더 실패 표식)
    try {
      await page.waitForFunction(() => document.documentElement.hasAttribute('data-forge-ready') || document.documentElement.hasAttribute('data-forge-error'), null, { timeout: opts.timeout });
    } catch (_) {
      reds.push(`로딩 완료 표식(data-forge-ready)이 ${opts.timeout}ms 안에 안 찍혔다`);
    }
    const err = await page.evaluate(() => document.documentElement.getAttribute('data-forge-error'));
    if (err) reds.push(`로더 실패 표식: ${err}`);
    result.ready = await page.evaluate(() => document.documentElement.hasAttribute('data-forge-ready'));

    // ③ 요구 신호 (forgeSignal(name))
    if (result.ready && opts.require.length) {
      try {
        await page.waitForFunction(need => need.every(n => (window.__forgeSignals || []).includes(n)), opts.require, { timeout: opts.signalTimeout });
      } catch (_) {
        const have = await page.evaluate(() => window.__forgeSignals || []);
        reds.push(`요구 신호 미도착: ${opts.require.filter(n => !have.includes(n)).join(',')} (도착: ${have.join(',') || '없음'})`);
      }
    }
    // 첫 프레임들에서 터지는 런타임 오류를 잡으려고 조금 더 머문다.
    if (result.ready && opts.settle > 0) await page.waitForTimeout(opts.settle);

    result.signals = await page.evaluate(() => window.__forgeSignals || []);
    result.appBox = await page.evaluate(() => { const a = document.getElementById('app'); return a ? { w: a.clientWidth, h: a.clientHeight } : null; });
    if (result.appBox) {
      const ratio = result.appBox.w / result.appBox.h;
      if (Math.abs(ratio - 9 / 16) > 0.01) reds.push(`앱 상자가 9:16 이 아니다: ${result.appBox.w}×${result.appBox.h}`);
    } else reds.push('#app 상자가 없다 — 템플릿이 Forge 가 아니다(ProjectSettings webGLTemplate)');

    if (opts.shot) {
      fs.mkdirSync(path.dirname(path.resolve(opts.shot)), { recursive: true });
      await page.screenshot({ path: opts.shot });
      log(`촬영: ${opts.shot}`);
    }
  } finally {
    await browser.close();
    if (served) served.server.close();
  }
  result.ms = Date.now() - t0;
  result.ok = reds.length === 0;
  return result;
}

// ---------- 자기 검사: 템플릿을 가짜 로더로 연다 ----------
// 유니티 템플릿 전처리기의 부분집합: {{{ NAME }}} · {{{ JSON.stringify(NAME) }}} · #if NAME / #else / #endif (줄 단위).
function renderTemplate(src, vars) {
  const out = [];
  const stack = []; // true = 출력 중
  const on = () => stack.every(Boolean);
  for (const line of src.split('\n')) {
    const m = /^\s*#(if|else|endif)\b\s*(!?)\s*([A-Z_]*)/.exec(line);
    if (m) {
      if (m[1] === 'if') { const v = !!vars[m[3]]; stack.push(m[2] === '!' ? !v : v); }
      else if (m[1] === 'else') stack.push(!stack.pop());
      else stack.pop();
      continue;
    }
    if (!on()) continue;
    out.push(line.replace(/\{\{\{\s*JSON\.stringify\(([A-Z_]+)\)\s*\}\}\}/g, (_, k) => JSON.stringify(String(vars[k] ?? '')))
                  .replace(/\{\{\{\s*([A-Z_]+)\s*\}\}\}/g, (_, k) => String(vars[k] ?? '')));
  }
  if (stack.length) throw new Error('템플릿 #if/#endif 짝이 안 맞는다');
  return out.join('\n');
}

function fakeLoader(mode) {
  // createUnityInstance 의 가짜 — 진행률을 올리고 인스턴스 흉내를 돌려준다.
  return `
    function createUnityInstance(canvas, config, onProgress) {
      return new Promise(function (resolve, reject) {
        var p = 0;
        var iv = setInterval(function () {
          p = Math.min(1, p + 0.25); onProgress(p);
          if (p >= 1) {
            clearInterval(iv);
            if (${JSON.stringify(mode)} === 'red') console.error('가짜 런타임 오류');
            if (${JSON.stringify(mode)} === 'hang') return;
            if (${JSON.stringify(mode)} === 'reject') { reject(new Error('가짜 로더 실패')); return; }
            resolve({ SetFullscreen: function () {}, SendMessage: function () {}, Quit: function () { return Promise.resolve(); } });
            setTimeout(function () { window.forgeSignal('battle'); }, 200);
          }
        }, 30);
      });
    }`;
}

async function selfTest(log) {
  const src = fs.readFileSync(TEMPLATE, 'utf8');
  const vars = { LOADER_FILENAME: 'web.loader.js', DATA_FILENAME: 'web.data.unityweb', FRAMEWORK_FILENAME: 'web.framework.js.unityweb', CODE_FILENAME: 'web.wasm.unityweb',
    WORKER_FILENAME: '', MEMORY_FILENAME: '', SYMBOLS_FILENAME: '', BACKGROUND_FILENAME: '', USE_WASM: true, USE_THREADS: false, USE_DATA_CACHING: true,
    COMPANY_NAME: 'DefaultCompany', PRODUCT_NAME: '포지 클론', PRODUCT_VERSION: '1.0', WIDTH: 540, HEIGHT: 960 };
  const html = renderTemplate(src, vars);
  if (/\{\{\{|^\s*#(if|endif)/m.test(html)) throw new Error('템플릿에 안 풀린 자리표/전처리 줄이 남았다');
  const cases = [
    { mode: 'ok', require: ['unity-ready', 'battle'], expect: true },
    { mode: 'red', require: ['unity-ready'], expect: false, why: 'console.error' },
    { mode: 'reject', require: ['unity-ready'], expect: false, why: '로더 실패 표식' },
    { mode: 'hang', require: ['unity-ready'], expect: false, why: '로딩 완료 표식', timeout: 2500 },
    { mode: 'ok', require: ['unity-ready', 'nope'], expect: false, why: '요구 신호 미도착' },
  ];
  let fails = 0;
  for (const c of cases) {
    const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'forge-smoke-'));
    fs.mkdirSync(path.join(dir, 'Build'));
    fs.writeFileSync(path.join(dir, 'index.html'), html);
    fs.writeFileSync(path.join(dir, 'Build', vars.LOADER_FILENAME), fakeLoader(c.mode));
    const r = await runSmoke({ serve: dir, require: c.require, timeout: c.timeout || 15000, signalTimeout: 1500, settle: 300, width: 540, height: 960, quiet: true, shot: null }, () => {});
    fs.rmSync(dir, { recursive: true, force: true });
    const pass = r.ok === c.expect && (c.expect || r.reds.some(x => x.includes(c.why)));
    log(`  ${pass ? '✓' : '✗'} ${c.mode} · require=${c.require.join(',')} → ${r.ok ? '초록' : '빨강'}${r.reds.length ? ' [' + r.reds[0].slice(0, 80) + ']' : ''} · 상자 ${r.appBox && r.appBox.w + '×' + r.appBox.h} · ${r.ms}ms`);
    if (!pass) fails++;
    if (c.mode === 'ok' && c.expect) {
      if (!r.appBox || r.appBox.w !== 540 || r.appBox.h !== 960) { log(`  ✗ 540×960 창에서 앱 상자가 540×960 이어야 한다: ${JSON.stringify(r.appBox)}`); fails++; }
      if (!r.signals.includes('battle')) { log('  ✗ battle 신호가 안 쌓였다'); fails++; }
    }
  }
  // 가로 창: 높이 기준으로 9:16 (원작 fitLayout 규칙 · 932×430 → 242×430).
  {
    const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'forge-smoke-'));
    fs.mkdirSync(path.join(dir, 'Build'));
    fs.writeFileSync(path.join(dir, 'index.html'), html);
    fs.writeFileSync(path.join(dir, 'Build', vars.LOADER_FILENAME), fakeLoader('ok'));
    const r = await runSmoke({ serve: dir, require: ['unity-ready'], timeout: 15000, signalTimeout: 1500, settle: 100, width: 932, height: 430, quiet: true, shot: null }, () => {});
    fs.rmSync(dir, { recursive: true, force: true });
    const okBox = r.appBox && r.appBox.h === 430 && Math.abs(r.appBox.w - 430 * 9 / 16) <= 1;
    log(`  ${okBox && r.ok ? '✓' : '✗'} 가로 932×430 → 앱 상자 ${r.appBox && r.appBox.w + '×' + r.appBox.h} (기대 242×430)`);
    if (!(okBox && r.ok)) fails++;
  }
  return fails;
}

// ---------- main ----------
(async () => {
  let opts;
  try { opts = parseArgs(process.argv.slice(2)); } catch (e) { console.error(`인자 오류: ${e.message}`); process.exit(2); }
  const log = m => console.log(m);
  try {
    if (opts.selfTest) {
      log('webgl_smoke 자기 검사 (템플릿 + 가짜 로더)');
      const fails = await selfTest(log);
      if (fails) { console.error(`✗ webgl_smoke self-test 실패 ${fails}건`); process.exit(1); }
      log('✓ webgl_smoke self-test 통과');
      return;
    }
    const r = await runSmoke(opts, log);
    log(`신호: ${r.signals.join(',') || '없음'} · 앱 상자: ${r.appBox ? r.appBox.w + '×' + r.appBox.h : '없음'} · ${r.ms}ms · 노랑 ${r.warns.length}`);
    if (r.ok) { log(`✓ webgl_smoke 초록: ${r.url}`); process.exit(0); }
    console.error(`✗ webgl_smoke 빨강 ${r.reds.length}건:`);
    for (const x of r.reds) console.error(`  - ${x}`);
    process.exit(1);
  } catch (e) {
    console.error(`✗ webgl_smoke 오류: ${e.message}`);
    process.exit(1);
  }
})();
