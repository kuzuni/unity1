#!/usr/bin/env node
'use strict';
// export_icons.js — 정본(kuzuni/wwwww web/js/icongen.js + avatars.js)의 캔버스 아이콘을 **headless Chromium 이 실제로 그려**
// 아틀라스 PNG 몇 장 + atlas.json(키 → 장·rect) 으로 낸다 (ROUTINE T31).
//
//   node tools/export_icons.js [--src .wwwww-src] [--out Assets/Forge/Icons/Resources/Icons] [--chromium <실행 파일>]
//   node tools/export_icons.js --compare <dir>     # 정본으로 다시 뽑아 <dir> 의 atlas.json·PNG 와 대조 (check_icons_sync.sh 가 부른다)
//   node tools/export_icons.js --self-test         # 이 자가 실제로 뽑는가 (키 전부 그려짐 · 빈 캔버스 0 · 결정론 · PNG 왕복 · 총량 상한)
//
// 원칙(T2 와 같다): 정본이 단독으로 쥔다 — 7,500줄 캔버스 코드를 C# 으로 다시 그리지 않고, 정본 `IconGen.url()` 이 만드는
// **최종 캔버스**(슈퍼샘플 → 키라인 → 블록화/축소까지 끝난 것)를 그대로 받아 아틀라스에 붙인다. 키 이름은 정본 문자열 그대로
// (`IconGen.draw` 의 키 · 아바타는 `avatar_<코드포인트16진>` · tint 변형은 정본 캐시 키와 같은 `name|#rrggbb`).
//
// 왜 Playwright 가 아니라 Chromium 바이너리 직접인가: 페이지 안이 전부 동기(캔버스 2D · 글꼴 0 · Math.random 0)라
// `--dump-dom` 한 번이면 끝난다 — npm 의존성 0 · CI 러너(google-chrome 내장)와 컨테이너(/opt/pw-browsers/chromium) 어디서나 같은 길.
// PNG 바이트는 Chromium 인코더가 아니라 여기 node 인코더로 다시 싸서(픽셀 → 같은 바이트) 커밋 바이트가 브라우저 판에 안 흔들린다.
//
// tint 변형은 정본 UI 코드가 **실제로 부르는 조합만** 뽑는다(ui.js 의 `{ tint: '#…' }` 리터럴 · TRI_BLUE 같은 상수 ·
// 등급색 알 `RARITY_CSS` · 기술 아이콘 `TECH_ICON_TINT`). 새 조합을 원작이 쓰기 시작하면 여기서 자동으로 따라간다.

const fs = require('fs');
const path = require('path');
const os = require('os');
const zlib = require('zlib');
const { spawnSync } = require('child_process');

const HERE = path.resolve(__dirname, '..');
const DEFAULT_SRC = '.wwwww-src';
const DEFAULT_OUT = 'Assets/Forge/Icons/Resources/Icons';
const ATLAS_W = 2048;          // 유니티 maxTextureSize 2048(gen_meta.py 의 .png 메타)
const ATLAS_MAX_H = 2048;
const PAD = 2;                 // 칸 사이 여백(px) — 바이리니어 축소 때 이웃 칸이 번지지 않게
const PNG_BUDGET = 3 * 1024 * 1024;   // ROUTINE T31: PNG 총량 상한 3MB
const CMP_CHANNEL_TOL = 8;     // --compare: 채널 차 8 이하는 같은 픽셀로 본다(브라우저 판 사이 AA 반올림)
const CMP_PIXEL_RATIO = 0.002; // --compare: 다른 픽셀이 0.2% 를 넘으면 정본과 다르다

function parseArgs(argv) {
  const o = { src: DEFAULT_SRC, out: DEFAULT_OUT, chromium: null, selfTest: false, compare: null, quiet: false, keep: false };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i], next = () => argv[++i];
    if (a === '--src') o.src = next();
    else if (a === '--out') o.out = next();
    else if (a === '--chromium') o.chromium = next();
    else if (a === '--compare') o.compare = next();
    else if (a === '--self-test') o.selfTest = true;
    else if (a === '--quiet') o.quiet = true;
    else if (a === '--keep') o.keep = true;
    else if (a === '-h' || a === '--help') { console.log(fs.readFileSync(__filename, 'utf8').split('\n').slice(1, 8).join('\n')); process.exit(0); }
    else throw new Error('모르는 인자: ' + a);
  }
  return o;
}

// ───────────────────────────── Chromium 찾기 ─────────────────────────────
function which(name) {
  for (const dir of (process.env.PATH || '').split(path.delimiter)) {
    const p = path.join(dir, name);
    try { fs.accessSync(p, fs.constants.X_OK); return p; } catch (_) { /* 다음 */ }
  }
  return null;
}
function findChromium(explicit) {
  const cands = [];
  if (explicit) cands.push(explicit);
  for (const k of ['CHROMIUM', 'CHROME_BIN', 'PUPPETEER_EXECUTABLE_PATH']) if (process.env[k]) cands.push(process.env[k]);
  cands.push('/opt/pw-browsers/chromium');
  for (const mod of ['playwright', 'playwright-core']) {
    try { const p = require(mod).chromium.executablePath(); if (p) cands.push(p); } catch (_) { /* 없음 */ }
  }
  for (const n of ['google-chrome', 'google-chrome-stable', 'chromium', 'chromium-browser', 'chrome']) { const p = which(n); if (p) cands.push(p); }
  for (const c of cands) { try { fs.accessSync(c, fs.constants.X_OK); return c; } catch (_) { /* 다음 */ } }
  throw new Error('Chromium 을 못 찾았다 — --chromium <경로> 또는 CHROMIUM 환경변수 (컨테이너: /opt/pw-browsers/chromium · CI: google-chrome)');
}

// ───────────────────────────── tint 변형 수집(정본 UI 코드가 실제로 부르는 조합) ─────────────────────────────
function collectVariants(src) {
  const js = path.join(src, 'web', 'js');
  const ui = ['ui.js', 'league.js', 'chat.js', 'main.js'].map(f => fs.existsSync(path.join(js, f)) ? fs.readFileSync(path.join(js, f), 'utf8') : '').join('\n');
  const gd = fs.readFileSync(path.join(js, 'gamedata.js'), 'utf8');
  const seen = new Set(), out = [];
  const add = (name, tint, why) => {
    tint = tint.toLowerCase();
    if (!/^#[0-9a-f]{6}$/.test(tint)) throw new Error('tint 꼴이 낯설다: ' + name + ' ' + tint + ' (' + why + ')');
    const k = name + '|' + tint;
    if (seen.has(k)) return;
    seen.add(k); out.push({ name, tint, why });
  };
  // ⓐ 리터럴: IconGen.img('name', <cls>, { tint: '#rrggbb' })
  for (const m of ui.matchAll(/IconGen\.img\(\s*['"]([A-Za-z0-9_]+)['"]\s*,\s*(?:null|['"][^'"]*['"])\s*,\s*\{\s*tint:\s*['"](#[0-9a-fA-F]{6})['"]\s*\}/g)) add(m[1], m[2], '리터럴');
  // ⓑ 상수: IconGen.img("tri_left", null, TRI_BLUE) → const TRI_BLUE = { tint: '#…' }
  for (const m of ui.matchAll(/IconGen\.img\(\s*['"]([A-Za-z0-9_]+)['"]\s*,\s*(?:null|['"][^'"]*['"])\s*,\s*([A-Z][A-Z0-9_]+)\s*\)/g)) {
    const c = ui.match(new RegExp('const\\s+' + m[2] + '\\s*=\\s*\\{\\s*tint:\\s*[\'"](#[0-9a-fA-F]{6})[\'"]'));
    if (!c) throw new Error('상수 ' + m[2] + ' 의 tint 를 못 읽었다');
    add(m[1], c[1], '상수 ' + m[2]);
  }
  // ⓒ 등급색 알: IconGen.img('egg', …, { tint: RARITY_CSS[…] }) → gamedata.js 의 RARITY_CSS 값 전부
  for (const m of ui.matchAll(/IconGen\.img\(\s*['"]([A-Za-z0-9_]+)['"][^)]*\{\s*tint:\s*RARITY_CSS\[/g)) {
    const rc = gd.match(/const\s+RARITY_CSS\s*=\s*\{([^}]*)\}/);
    if (!rc) throw new Error('gamedata.js 에서 RARITY_CSS 를 못 읽었다');
    for (const e of rc[1].matchAll(/['"]?(\w+)['"]?\s*:\s*['"](#[0-9a-fA-F]{6})['"]/g)) add(m[1], e[2], 'RARITY_CSS.' + e[1]);
  }
  // ⓓ 기술 아이콘: TECH_ICON_TINT { type: '#…' } 의 type 을 TECH_ICON { type: 'name' } 으로 풀어 (name, tint)
  const tt = ui.match(/TECH_ICON_TINT\s*:\s*\{([^}]*)\}/), ti = ui.match(/TECH_ICON\s*:\s*\{([^}]*)\}/);
  if (tt && ti) {
    const icons = {};
    for (const e of ti[1].matchAll(/(\w+)\s*:\s*['"]([A-Za-z0-9_]+)['"]/g)) icons[e[1]] = e[2];
    for (const e of tt[1].matchAll(/(\w+)\s*:\s*['"](#[0-9a-fA-F]{6})['"]/g)) {
      if (!icons[e[1]]) throw new Error('TECH_ICON_TINT.' + e[1] + ' 에 맞는 TECH_ICON 이 없다');
      add(icons[e[1]], e[2], 'TECH_ICON_TINT.' + e[1]);
    }
  }
  return out;
}

// ───────────────────────────── 페이지 ─────────────────────────────
function buildPage(src, variants) {
  const js = path.join(src, 'web', 'js');
  const files = ['icongen.js', 'avatars.js'].map(f => {
    const p = path.join(js, f);
    if (!fs.existsSync(p)) throw new Error('정본이 없다: ' + p);
    const t = fs.readFileSync(p, 'utf8');
    if (/<\/script/i.test(t)) throw new Error(f + ' 안에 </script> 가 있어 인라인할 수 없다');
    return t;
  });
  const driver = `
(function () {
  const VARIANTS = ${JSON.stringify(variants.map(v => ({ name: v.name, tint: v.tint })))};
  const W = ${ATLAS_W}, MAXH = ${ATLAS_MAX_H}, PAD = ${PAD};
  const out = { error: null, atlases: [], icons: [] };
  try {
    let captured = null;
    const origToDataURL = HTMLCanvasElement.prototype.toDataURL;
    // IconGen.url() 은 마지막에 최종 캔버스의 toDataURL 을 한 번 부른다 — 그 캔버스를 낚아채 픽셀 그대로 쓴다(dataURL 재디코드 없음).
    HTMLCanvasElement.prototype.toDataURL = function () { captured = this; return 'captured'; };
    const jobs = Object.keys(IconGen.draw).map(n => ({ name: n, tint: null }));
    for (const v of VARIANTS) jobs.push(v);
    const items = [];
    for (const j of jobs) {
      captured = null; IconGen.cache = {};
      const key = j.name + (j.tint ? '|' + j.tint : '');
      let err = null;
      try { IconGen.url(j.name, j.tint ? { tint: j.tint } : undefined); } catch (e) { err = String(e && e.message || e); }
      if (!captured) { items.push({ key, name: j.name, tint: j.tint, missing: err || 'toDataURL 이 안 불렸다' }); continue; }
      const c = captured;
      const d = c.getContext('2d').getImageData(0, 0, c.width, c.height).data;
      let opaque = 0;
      for (let i = 3; i < d.length; i += 4) if (d[i]) opaque++;
      items.push({ key, name: j.name, tint: j.tint, w: c.width, h: c.height, size: IconGen._sizeOf(j.name), opaque, canvas: c });
    }
    HTMLCanvasElement.prototype.toDataURL = origToDataURL;
    // 선반 채우기(높이 내림차순 · 폭 내림차순 · 키) — 정렬이 전순서라 같은 입력이면 같은 자리.
    const order = items.filter(i => !i.missing).slice().sort((a, b) => b.h - a.h || b.w - a.w || (a.key < b.key ? -1 : a.key > b.key ? 1 : 0));
    const sheets = [];
    let cur = null;
    const open = () => { cur = { x: PAD, y: PAD, rowH: 0, items: [] }; sheets.push(cur); };
    open();
    for (const it of order) {
      if (it.w + 2 * PAD > W || it.h + 2 * PAD > MAXH) throw new Error('아틀라스보다 큰 아이콘: ' + it.key + ' ' + it.w + 'x' + it.h);
      if (cur.x + it.w + PAD > W) { cur.x = PAD; cur.y += cur.rowH + PAD; cur.rowH = 0; }
      if (cur.y + it.h + PAD > MAXH) open();
      it.a = sheets.length - 1; it.x = cur.x; it.y = cur.y;
      cur.x += it.w + PAD; cur.rowH = Math.max(cur.rowH, it.h); cur.items.push(it);
    }
    sheets.forEach(sh => {
      const h = Math.min(MAXH, Math.ceil((sh.y + sh.rowH + PAD) / 4) * 4);
      const cv = document.createElement('canvas');
      cv.width = W; cv.height = h;
      const g = cv.getContext('2d');
      for (const it of sh.items) g.drawImage(it.canvas, it.x, it.y);
      out.atlases.push({ w: W, h, png: cv.toDataURL('image/png') });
    });
    for (const it of items) out.icons.push(it.missing ? { key: it.key, name: it.name, tint: it.tint, missing: it.missing }
      : { key: it.key, name: it.name, tint: it.tint, a: it.a, x: it.x, y: it.y, w: it.w, h: it.h, size: it.size, opaque: it.opaque });
  } catch (e) { out.error = String(e && e.stack || e); }
  document.getElementById('__out').textContent = JSON.stringify(out);
})();`;
  return '<!doctype html><html><head><meta charset="utf-8"><title>export_icons</title></head><body><pre id="__out"></pre>\n' +
    '<script>' + files[0] + '\n</script>\n<script>' + files[1] + '\n</script>\n<script>' + driver + '\n</script></body></html>\n';
}

function runChromium(bin, pagePath, quiet) {
  const prof = fs.mkdtempSync(path.join(os.tmpdir(), 'export-icons-prof-'));
  const args = ['--headless=new', '--no-sandbox', '--disable-gpu', '--disable-dev-shm-usage', '--hide-scrollbars', '--no-first-run',
    '--disable-extensions', '--disable-background-networking', '--user-data-dir=' + prof, '--dump-dom', 'file://' + pagePath];
  const r = spawnSync(bin, args, { encoding: 'utf8', maxBuffer: 1024 * 1024 * 1024, timeout: 300000 });
  try { fs.rmSync(prof, { recursive: true, force: true }); } catch (_) { /* 무시 */ }
  if (r.error) throw new Error('Chromium 실행 실패: ' + r.error.message);
  const m = r.stdout.match(/<pre id="__out">([\s\S]*?)<\/pre>/);
  if (!m) throw new Error('Chromium 출력에서 결과를 못 찾았다 (rc ' + r.status + ')\n' + (r.stderr || '').slice(-2000));
  const txt = m[1].replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&quot;/g, '"').replace(/&#39;/g, "'").replace(/&amp;/g, '&');
  const out = JSON.parse(txt);
  if (out.error) throw new Error('페이지 안에서 실패: ' + out.error);
  return out;
}

// ───────────────────────────── PNG (RGBA8 · 비인터레이스) ─────────────────────────────
const CRC_TABLE = (() => { const t = new Int32Array(256); for (let n = 0; n < 256; n++) { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; t[n] = c; } return t; })();
function crc32(buf) { let c = -1; for (let i = 0; i < buf.length; i++) c = CRC_TABLE[(c ^ buf[i]) & 0xff] ^ (c >>> 8); return (c ^ -1) >>> 0; }
function chunk(type, data) {
  const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
  const td = Buffer.concat([Buffer.from(type, 'latin1'), data]);
  const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(td));
  return Buffer.concat([len, td, crc]);
}
function paeth(a, b, c) { const p = a + b - c, pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c); return pa <= pb && pa <= pc ? a : pb <= pc ? b : c; }
/** RGBA 픽셀 → PNG 바이트. 줄마다 필터 5종 중 절대합이 가장 작은 것(표준 휴리스틱) · deflate 9 — 같은 픽셀이면 같은 바이트. */
function encodePng(w, h, rgba) {
  const bpp = 4, stride = w * bpp;
  const raw = Buffer.alloc((stride + 1) * h);
  const cand = [0, 1, 2, 3, 4].map(() => Buffer.alloc(stride));
  let prev = Buffer.alloc(stride);
  for (let y = 0; y < h; y++) {
    const row = rgba.subarray(y * stride, (y + 1) * stride);
    let best = 0, bestSum = Infinity;
    for (let f = 0; f < 5; f++) {
      const o = cand[f]; let sum = 0;
      for (let i = 0; i < stride; i++) {
        const a = i >= bpp ? row[i - bpp] : 0, b = prev[i], c = i >= bpp ? prev[i - bpp] : 0, x = row[i];
        let v;
        if (f === 0) v = x; else if (f === 1) v = x - a; else if (f === 2) v = x - b; else if (f === 3) v = x - ((a + b) >> 1); else v = x - paeth(a, b, c);
        v &= 0xff; o[i] = v; sum += v < 128 ? v : 256 - v;
      }
      if (sum < bestSum) { bestSum = sum; best = f; }
    }
    raw[y * (stride + 1)] = best;
    cand[best].copy(raw, y * (stride + 1) + 1);
    prev = Buffer.from(row);
  }
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = 6; ihdr[10] = 0; ihdr[11] = 0; ihdr[12] = 0;
  return Buffer.concat([Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]), chunk('IHDR', ihdr),
    chunk('IDAT', zlib.deflateSync(raw, { level: 9 })), chunk('IEND', Buffer.alloc(0))]);
}
/** PNG 바이트 → {w,h,rgba}. 8비트 RGB/RGBA 비인터레이스만(Chromium toDataURL 과 위 인코더가 내는 꼴). */
function decodePng(buf) {
  if (buf.length < 8 || buf.readUInt32BE(0) !== 0x89504e47) throw new Error('PNG 가 아니다');
  let p = 8, w = 0, h = 0, ct = 0, idat = [];
  while (p < buf.length) {
    const len = buf.readUInt32BE(p), type = buf.toString('latin1', p + 4, p + 8), data = buf.subarray(p + 8, p + 8 + len);
    if (type === 'IHDR') {
      w = data.readUInt32BE(0); h = data.readUInt32BE(4);
      if (data[8] !== 8 || (data[9] !== 6 && data[9] !== 2) || data[12] !== 0) throw new Error('지원 밖 PNG(8비트 RGB/RGBA 비인터레이스만): depth ' + data[8] + ' color ' + data[9] + ' interlace ' + data[12]);
      ct = data[9];
    } else if (type === 'IDAT') idat.push(data);
    else if (type === 'IEND') break;
    p += 12 + len;
  }
  const bpp = ct === 6 ? 4 : 3, stride = w * bpp;
  const raw = zlib.inflateSync(Buffer.concat(idat));
  const px = Buffer.alloc(stride * h);
  let prev = Buffer.alloc(stride);
  for (let y = 0; y < h; y++) {
    const f = raw[y * (stride + 1)], line = raw.subarray(y * (stride + 1) + 1, (y + 1) * (stride + 1));
    const cur = Buffer.alloc(stride);
    for (let i = 0; i < stride; i++) {
      const a = i >= bpp ? cur[i - bpp] : 0, b = prev[i], c = i >= bpp ? prev[i - bpp] : 0, x = line[i];
      let v;
      if (f === 0) v = x; else if (f === 1) v = x + a; else if (f === 2) v = x + b; else if (f === 3) v = x + ((a + b) >> 1); else if (f === 4) v = x + paeth(a, b, c);
      else throw new Error('PNG 필터 ' + f);
      cur[i] = v & 0xff;
    }
    cur.copy(px, y * stride); prev = cur;
  }
  if (bpp === 3) {
    const rgba = Buffer.alloc(w * h * 4);
    for (let i = 0, j = 0; i < px.length; i += 3, j += 4) { rgba[j] = px[i]; rgba[j + 1] = px[i + 1]; rgba[j + 2] = px[i + 2]; rgba[j + 3] = 255; }
    return { w, h, rgba };
  }
  return { w, h, rgba: px };
}
function dataUrlPng(u) {
  const m = /^data:image\/png;base64,(.*)$/.exec(u);
  if (!m) throw new Error('PNG dataURL 이 아니다');
  return Buffer.from(m[1], 'base64');
}

// ───────────────────────────── 추출 ─────────────────────────────
function exportIcons(opt) {
  const src = path.resolve(HERE, opt.src);
  const bin = findChromium(opt.chromium);
  const variants = collectVariants(src);
  const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'export-icons-'));
  const page = path.join(tmp, 'page.html');
  fs.writeFileSync(page, buildPage(src, variants));
  let out;
  try { out = runChromium(bin, page, opt.quiet); } finally { if (!opt.keep) fs.rmSync(tmp, { recursive: true, force: true }); }
  const missing = out.icons.filter(i => i.missing);
  if (missing.length) throw new Error('못 그린 키 ' + missing.length + ': ' + missing.slice(0, 8).map(i => i.key + '(' + i.missing + ')').join(', '));
  const sheets = out.atlases.map((a, i) => {
    const dec = decodePng(dataUrlPng(a.png));
    if (dec.w !== a.w || dec.h !== a.h) throw new Error('아틀라스 ' + i + ' 크기가 안 맞다');
    return { file: 'atlas-' + i + '.png', w: a.w, h: a.h, rgba: dec.rgba, png: encodePng(dec.w, dec.h, dec.rgba) };
  });
  const json = {
    _: '원작 icongen.js·avatars.js 아이콘 아틀라스 (ROUTINE T31 · tools/export_icons.js 가 정본을 headless Chromium 으로 그려 낸다 — 손으로 고치지 않는다 · 검사 tools/check_icons_sync.sh). key = 정본 IconGen 키(tint 변형은 name|#rrggbb · 아바타는 avatar_<코드포인트16진>) · x,y 는 PNG 좌상단 기준 · size = 정본 굽기 해상도(S).',
    pad: PAD,
    atlases: sheets.map(s => ({ file: s.file, w: s.w, h: s.h })),
    icons: out.icons.map(i => ({ key: i.key, name: i.name, tint: i.tint, atlas: i.a, x: i.x, y: i.y, w: i.w, h: i.h, size: i.size })),
  };
  return { json, sheets, variants, bin, opaque: out.icons.map(i => ({ key: i.key, opaque: i.opaque })) };
}

function writeOut(res, outDir) {
  fs.mkdirSync(outDir, { recursive: true });
  for (const f of fs.readdirSync(outDir)) if (/^atlas-\d+\.png$/.test(f) && !res.sheets.some(s => s.file === f)) fs.unlinkSync(path.join(outDir, f));
  for (const s of res.sheets) fs.writeFileSync(path.join(outDir, s.file), s.png);
  fs.writeFileSync(path.join(outDir, 'atlas.json'), JSON.stringify(res.json, null, 1) + '\n');
}
function totalPng(res) { return res.sheets.reduce((n, s) => n + s.png.length, 0); }

// ───────────────────────────── 대조(--compare) ─────────────────────────────
function stripJson(j) { return { pad: j.pad, atlases: j.atlases, icons: j.icons }; }
function compareWith(res, dir) {
  const problems = [];
  const jp = path.join(dir, 'atlas.json');
  if (!fs.existsSync(jp)) return ['atlas.json 이 없다: ' + jp];
  const have = JSON.parse(fs.readFileSync(jp, 'utf8'));
  if (JSON.stringify(stripJson(have)) !== JSON.stringify(stripJson(res.json))) {
    const hk = new Set(have.icons.map(i => i.key)), nk = new Set(res.json.icons.map(i => i.key));
    const added = [...nk].filter(k => !hk.has(k)), gone = [...hk].filter(k => !nk.has(k));
    problems.push('atlas.json 이 다르다' + (added.length ? ' · 정본에 새로 생긴 키 ' + added.slice(0, 6).join(',') : '') + (gone.length ? ' · 정본에서 사라진 키 ' + gone.slice(0, 6).join(',') : '') +
      (!added.length && !gone.length ? ' · 자리/크기가 움직였다' : ''));
  }
  res.sheets.forEach((s, i) => {
    const fp = path.join(dir, s.file);
    if (!fs.existsSync(fp)) { problems.push(s.file + ' 이 없다'); return; }
    let dec;
    try { dec = decodePng(fs.readFileSync(fp)); } catch (e) { problems.push(s.file + ' 을 못 읽는다: ' + e.message); return; }
    if (dec.w !== s.w || dec.h !== s.h) { problems.push(s.file + ' 크기 ' + dec.w + 'x' + dec.h + ' ≠ ' + s.w + 'x' + s.h); return; }
    let diff = 0;
    const a = dec.rgba, b = s.rgba, n = s.w * s.h;
    for (let p = 0; p < n; p++) {
      const o = p * 4;
      if (Math.abs(a[o] - b[o]) > CMP_CHANNEL_TOL || Math.abs(a[o + 1] - b[o + 1]) > CMP_CHANNEL_TOL || Math.abs(a[o + 2] - b[o + 2]) > CMP_CHANNEL_TOL || Math.abs(a[o + 3] - b[o + 3]) > CMP_CHANNEL_TOL) diff++;
    }
    const ratio = diff / n;
    if (ratio > CMP_PIXEL_RATIO) problems.push(s.file + ' 픽셀이 다르다: ' + diff + '/' + n + ' (' + (ratio * 100).toFixed(3) + '% > ' + (CMP_PIXEL_RATIO * 100) + '%)');
  });
  return problems;
}

// ───────────────────────────── 자기 검사 ─────────────────────────────
function selfTest(opt) {
  const fails = [];
  const ok = (cond, msg) => { if (!cond) fails.push(msg); };
  // ⓐ PNG 왕복(필터 5종 · 알파 · 반투명)
  {
    const w = 37, h = 11, px = Buffer.alloc(w * h * 4);
    for (let i = 0; i < px.length; i++) px[i] = (i * 7919 + (i >> 3) * 31) & 0xff;
    for (let y = 0; y < 3; y++) for (let x = 0; x < w * 4; x++) px[y * w * 4 + x] = 200;   // 평평한 줄(필터 1·2 가 이긴다)
    const dec = decodePng(encodePng(w, h, px));
    ok(dec.w === w && dec.h === h && Buffer.compare(dec.rgba, px) === 0, 'PNG 인코드→디코드 왕복이 안 맞는다');
    ok(Buffer.compare(encodePng(w, h, px), encodePng(w, h, px)) === 0, 'PNG 인코더가 결정론이 아니다');
  }
  // ⓑ 실제 추출 두 번 — 키 전부 · 빈 캔버스 0 · rect 안쪽 · 결정론 · 총량
  let r1, r2;
  try { r1 = exportIcons(opt); r2 = exportIcons(opt); } catch (e) { fails.push('추출 실패: ' + e.message); }
  if (r1 && r2) {
    const keys = r1.json.icons.map(i => i.key);
    ok(keys.length >= 160, '키가 160 미만: ' + keys.length + ' (정본 IconGen.draw 아이콘 136 + 아바타 24)');
    ok(new Set(keys).size === keys.length, '키가 겹친다');
    ok(keys.filter(k => k.indexOf('avatar_') === 0).length >= 24, '아바타 24종이 안 나왔다');
    ok(r1.variants.length >= 10, 'tint 변형이 10 미만: ' + r1.variants.length + ' (알×등급 6 · check · tri×2 · paw)');
    ok(r1.json.icons.some(i => i.key === 'egg|#1cafff'), '등급색 알(egg|#1cafff) 이 없다');
    ok(r1.json.icons.some(i => i.key === 'tri_left|#005dff'), 'TRI_BLUE 삼각형이 없다');
    const empty = r1.opaque.filter(o => !o.opaque).map(o => o.key);
    ok(empty.length === 0, '빈 캔버스: ' + empty.slice(0, 8).join(','));
    for (const i of r1.json.icons) {
      const s = r1.json.atlases[i.atlas];
      if (!s || i.x < 0 || i.y < 0 || i.x + i.w > s.w || i.y + i.h > s.h) { fails.push('rect 가 아틀라스 밖: ' + i.key); break; }
      if (i.w <= 0 || i.h <= 0) { fails.push('빈 rect: ' + i.key); break; }
    }
    const coin = r1.json.icons.find(i => i.key === 'coin');
    ok(coin && coin.w === 160 && coin.h === 160, '코인은 블록화 20칸×8px = 160 이어야 한다: ' + (coin && coin.w + 'x' + coin.h));
    const av = r1.json.icons.find(i => i.key === 'avatar_1f6e1_fe0f');
    ok(av && av.w === 160 && av.h === 160, '아바타(🛡️)는 160px 도트: ' + (av && av.w + 'x' + av.h));
    ok(r1.json.atlases.length <= 4, '아틀라스가 너무 많다: ' + r1.json.atlases.length);
    ok(totalPng(r1) <= PNG_BUDGET, 'PNG 총량 ' + totalPng(r1) + ' > 상한 ' + PNG_BUDGET);
    ok(JSON.stringify(r1.json) === JSON.stringify(r2.json), '두 번 뽑은 atlas.json 이 다르다(결정론 깨짐)');
    ok(r1.sheets.length === r2.sheets.length && r1.sheets.every((s, i) => Buffer.compare(s.png, r2.sheets[i].png) === 0), '두 번 뽑은 PNG 바이트가 다르다(결정론 깨짐)');
    // ⓒ 대조 자 — 자기 자신과는 같고, 픽셀을 흔들면 다르다고 해야 한다
    const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'export-icons-st-'));
    try {
      writeOut(r1, tmp);
      ok(compareWith(r2, tmp).length === 0, '--compare 가 같은 것을 다르다고 한다');
      const bad = decodePng(fs.readFileSync(path.join(tmp, 'atlas-0.png')));
      for (let p = 0; p < bad.rgba.length; p += 4) if ((p / 4) % 50 === 0) { bad.rgba[p] = 255 - bad.rgba[p]; bad.rgba[p + 3] = 255; }
      fs.writeFileSync(path.join(tmp, 'atlas-0.png'), encodePng(bad.w, bad.h, bad.rgba));
      ok(compareWith(r2, tmp).some(m => /픽셀/.test(m)), '--compare 가 흔든 픽셀(2%)을 못 잡는다');
    } finally { fs.rmSync(tmp, { recursive: true, force: true }); }
    if (!opt.quiet) console.log('  키 ' + keys.length + ' (변형 ' + r1.variants.length + ') · 아틀라스 ' + r1.json.atlases.map(a => a.w + 'x' + a.h).join(', ') + ' · PNG ' + totalPng(r1) + 'B · Chromium ' + r1.bin);
  }
  if (fails.length) { console.log('✗ export_icons --self-test: ' + fails.length + '건\n  · ' + fails.join('\n  · ')); return 1; }
  console.log('✓ export_icons --self-test: PNG 왕복 · 키 전부 · 빈 캔버스 0 · rect · 결정론 · 총량 · --compare 판정 통과');
  return 0;
}

function main() {
  const opt = parseArgs(process.argv.slice(2));
  if (opt.selfTest) return selfTest(opt);
  const res = exportIcons(opt);
  if (opt.compare) {
    const dir = path.resolve(HERE, opt.compare);
    const problems = compareWith(res, dir);
    if (problems.length) { console.log('✗ export_icons --compare: ' + problems.join(' · ')); return 1; }
    console.log('✓ export_icons --compare: ' + dir + ' 이 정본과 같다 (키 ' + res.json.icons.length + ' · 아틀라스 ' + res.sheets.length + ')');
    return 0;
  }
  const outDir = path.resolve(HERE, opt.out);
  if (totalPng(res) > PNG_BUDGET) throw new Error('PNG 총량 ' + totalPng(res) + 'B 가 상한 ' + PNG_BUDGET + 'B 를 넘는다 — 해상도 단을 낮춰야 한다(ROUTINE T31)');
  writeOut(res, outDir);
  if (!opt.quiet) console.log('✓ export_icons: ' + outDir + ' ← 키 ' + res.json.icons.length + ' (tint 변형 ' + res.variants.length + ') · 아틀라스 ' + res.sheets.map(s => s.file + ' ' + s.w + 'x' + s.h + ' ' + s.png.length + 'B').join(', ') + ' · Chromium ' + res.bin);
  return 0;
}

if (require.main === module) {
  try { process.exit(main()); } catch (e) { console.error('✗ export_icons: ' + (e && e.message || e)); process.exit(1); }
}
module.exports = { encodePng, decodePng, collectVariants, exportIcons, compareWith };
