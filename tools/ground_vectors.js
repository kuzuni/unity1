#!/usr/bin/env node
'use strict';
// ============================================================================
// ground_vectors.js — T34 지면 소재 굽기 대조 벡터.
// ----------------------------------------------------------------------------
// 정본 `scene3d.js` 의 `makeGroundTexture(biome)`(알베도 512) · `makeGroundNormalMap(biome)`(노멀 256) ·
// `makeCrackTexture()`(용암 발광 256) · `buildTerrain` 의 포석 줄눈 데칼 블록(1024×256) 을 **그대로 실행**한다.
// 브라우저 캔버스가 없으므로 아래 `Canvas2D` 가 캔버스 2D 의 부분집합을 **순수 픽셀 버퍼**로 흉내낸다 —
// 정본 그리기 코드는 한 줄도 바꾸지 않고, 래스터 규칙만 우리 것이다(C# `GroundTexCanvas` 가 같은 규칙).
//
// 래스터 규칙(둘이 같아야 한다 — 바꾸면 양쪽을 같이 바꾼다):
//   · 픽셀당 2×2 표본(0.25/0.75) — 표본이 도형 안이면 1/4 씩 덮음. 도형 안/밖 판정은 정확한 기하(타원·다각형 짝홀·
//     선분 거리). 안티에일리어싱은 이 4단계뿐이다.
//   · 색은 프리멀티플라이드 double(0~1) · source-over / destination-out 만. 8비트 양자화는 get/putImageData 와 최종 추출
//     에서만(정본도 그 자리에서 Uint8ClampedArray 로 깎인다 · 반올림 = 짝수로).
//   · 그라디언트는 fill 시점 CTM 의 사용자 좌표에서 평가(정본 사막 범프의 «중심이 어긋난 그라디언트» 까지 그대로) ·
//     정지점 사이 보간은 프리멀티플라이드 선형.
//   · 선: lineCap butt|round · lineJoin 은 항상 round(정본 miter 를 근사 — 굵기 1.4~3px 에서 차이 없음).
//   · 변환은 translate·scale 만(정본이 그것만 쓴다).
// 난수 = xorshift32(정본 tools/lib-seed.js · T2 표본과 같은 시드 0x2f6e2b1) — 텍스처마다 되감는다.
//
// 사용:
//   node tools/ground_vectors.js                 # → Assets/Tests/EditMode/Vectors/t34-ground.json (+ --png 로 그림)
//   node tools/ground_vectors.js --check         # 다시 뽑아 같은지만 (rc 1 = 다르다)
//   node tools/ground_vectors.js --png <dir>     # 눈으로 볼 PNG 도 쓴다(레포에 안 넣는다)
// ============================================================================
const fs = require('fs');
const path = require('path');
const vm = require('vm');
const zlib = require('zlib');

const ROOT = path.resolve(__dirname, '..');
const DEFAULT_SRC = path.join(ROOT, '.wwwww-src');
const DEFAULT_OUT = path.join(ROOT, 'Assets', 'Tests', 'EditMode', 'Vectors', 't34-ground.json');
const SEED = 0x2f6e2b1;
const LOAD = ['bignum.js', 'util.js', 'balance-data.js', 'gamedata.js', 'voxel.js', 'mobs.js', 'mobdata.js',
    'mobs-pets.js', 'mobs-mounts.js', 'mobs-enemies.js', 'mobs-skillfx.js', 'mobs-props.js', 'prochar.js', 'scene3d.js'];

// ---------------------------------------------------------------- 수 규칙(C# 과 같게)
function xorshift32(seed) {
    let s = seed >>> 0;
    return () => { s ^= s << 13; s >>>= 0; s ^= s >>> 17; s ^= s << 5; s >>>= 0; return s / 4294967296; };
}
// Uint8ClampedArray 대입 규칙: 0~255 로 자르고 반올림은 짝수로(ToUint8Clamp).
function clamp8(x) {
    if (!(x > 0)) return 0;
    if (x >= 255) return 255;
    const f = Math.floor(x), r = x - f;
    if (r < 0.5) return f;
    if (r > 0.5) return f + 1;
    return (f & 1) ? f + 1 : f;
}
// CSS 색 문자열 → [r,g,b,a](0~1). 정본이 쓰는 꼴만: '#rgb' · '#rrggbb' · 'rgb(r,g,b)' · 'rgba(r,g,b,a)'.
function parseColor(s) {
    if (Array.isArray(s)) return s;
    s = String(s).trim();
    if (s[0] === '#') {
        const h = s.slice(1);
        if (h.length === 3) return [parseInt(h[0] + h[0], 16) / 255, parseInt(h[1] + h[1], 16) / 255, parseInt(h[2] + h[2], 16) / 255, 1];
        return [parseInt(h.slice(0, 2), 16) / 255, parseInt(h.slice(2, 4), 16) / 255, parseInt(h.slice(4, 6), 16) / 255, 1];
    }
    const m = /^rgba?\(([^)]*)\)$/.exec(s);
    if (!m) throw new Error('색을 모른다: ' + s);
    const p = m[1].split(',').map(t => parseFloat(t));
    return [p[0] / 255, p[1] / 255, p[2] / 255, p.length > 3 ? p[3] : 1];
}

// ---------------------------------------------------------------- Canvas 2D 시밍
class Gradient {
    constructor(kind, a) { this.kind = kind; this.a = a; this.stops = []; }
    addColorStop(t, col) { this.stops.push({ t, c: parseColor(col) }); }
    // 사용자 좌표 (ux,uy) 에서 프리멀티플라이드 색 [r,g,b,a]
    at(ux, uy) {
        let t;
        if (this.kind === 'radial') {
            const [x0, y0, r0, x1, y1, r1] = this.a;
            const d = Math.sqrt((ux - x1) * (ux - x1) + (uy - y1) * (uy - y1));
            t = r1 > r0 ? (d - r0) / (r1 - r0) : 1;
        } else {
            const [x0, y0, x1, y1] = this.a;
            const dx = x1 - x0, dy = y1 - y0, L = dx * dx + dy * dy;
            t = L > 0 ? ((ux - x0) * dx + (uy - y0) * dy) / L : 0;
        }
        if (!(t > 0)) t = 0; else if (t > 1) t = 1;
        const st = this.stops;
        if (st.length === 0) return [0, 0, 0, 0];
        if (t <= st[0].t) return pm(st[0].c);
        for (let i = 1; i < st.length; i++) {
            if (t <= st[i].t) {
                const a = st[i - 1], b = st[i], u = b.t > a.t ? (t - a.t) / (b.t - a.t) : 0;
                const pa = pm(a.c), pb = pm(b.c);
                return [pa[0] + (pb[0] - pa[0]) * u, pa[1] + (pb[1] - pa[1]) * u, pa[2] + (pb[2] - pa[2]) * u, pa[3] + (pb[3] - pa[3]) * u];
            }
        }
        return pm(st[st.length - 1].c);
    }
}
const pm = c => [c[0] * c[3], c[1] * c[3], c[2] * c[3], c[3]];

class ImageDataShim {
    constructor(w, h, data) { this.width = w; this.height = h; this.data = data || new Uint8ClampedArray(w * h * 4); }
}

class Context2D {
    constructor(canvas) {
        this.canvas = canvas;
        const n = canvas.width * canvas.height;
        this.buf = new Float64Array(n * 4);          // 프리멀티플라이드 r,g,b,a (0~1)
        this.fillStyle = '#000'; this.strokeStyle = '#000';
        this.lineWidth = 1; this.lineCap = 'butt'; this.lineJoin = 'miter';
        this.globalCompositeOperation = 'source-over';
        this.sx = 1; this.sy = 1; this.tx = 0; this.ty = 0;
        this.stack = [];
        this.path = [];
    }
    save() { this.stack.push({ fillStyle: this.fillStyle, strokeStyle: this.strokeStyle, lineWidth: this.lineWidth, lineCap: this.lineCap, lineJoin: this.lineJoin, gco: this.globalCompositeOperation, sx: this.sx, sy: this.sy, tx: this.tx, ty: this.ty }); }
    restore() {
        const s = this.stack.pop(); if (!s) return;
        this.fillStyle = s.fillStyle; this.strokeStyle = s.strokeStyle; this.lineWidth = s.lineWidth; this.lineCap = s.lineCap; this.lineJoin = s.lineJoin;
        this.globalCompositeOperation = s.gco; this.sx = s.sx; this.sy = s.sy; this.tx = s.tx; this.ty = s.ty;
    }
    translate(x, y) { this.tx += x * this.sx; this.ty += y * this.sy; }
    scale(x, y) { this.sx *= x; this.sy *= y; }
    T(x, y) { return [x * this.sx + this.tx, y * this.sy + this.ty]; }
    ctm() { return { sx: this.sx, sy: this.sy, tx: this.tx, ty: this.ty }; }
    beginPath() { this.path = []; }
    closePath() { const s = this.path[this.path.length - 1]; if (s && s.pts) s.closed = true; }
    moveTo(x, y) { this.path.push({ pts: [this.T(x, y)], closed: false }); }
    lineTo(x, y) { const s = this.path[this.path.length - 1]; if (!s || !s.pts) this.moveTo(x, y); else s.pts.push(this.T(x, y)); }
    ellipse(x, y, rx, ry, rot) { this.path.push({ ell: { x, y, rx, ry, rot, ctm: this.ctm() } }); }
    arc(x, y, r) { this.ellipse(x, y, r, r, 0); }
    createRadialGradient(x0, y0, r0, x1, y1, r1) { return new Gradient('radial', [x0, y0, r0, x1, y1, r1]); }
    createLinearGradient(x0, y0, x1, y1) { return new Gradient('linear', [x0, y0, x1, y1]); }
    createImageData(w, h) { return new ImageDataShim(w, h); }
    clearRect(x, y, w, h) {
        const [x0, y0] = this.T(x, y), [x1, y1] = this.T(x + w, y + h);
        const W = this.canvas.width, H = this.canvas.height;
        for (let py = Math.max(0, Math.floor(Math.min(y0, y1))); py < Math.min(H, Math.ceil(Math.max(y0, y1))); py++)
            for (let px = Math.max(0, Math.floor(Math.min(x0, x1))); px < Math.min(W, Math.ceil(Math.max(x0, x1))); px++) {
                const i = (py * W + px) * 4; this.buf[i] = this.buf[i + 1] = this.buf[i + 2] = this.buf[i + 3] = 0;
            }
    }
    // ---- 합성: bbox 안 픽셀마다 4표본 ----
    paint(bbox, inside, style) {
        const W = this.canvas.width, H = this.canvas.height;
        const x0 = Math.max(0, Math.floor(bbox[0])), y0 = Math.max(0, Math.floor(bbox[1]));
        const x1 = Math.min(W, Math.ceil(bbox[2])), y1 = Math.min(H, Math.ceil(bbox[3]));
        if (x1 <= x0 || y1 <= y0) return;
        const grad = style instanceof Gradient ? style : null;
        const solid = grad ? null : pm(parseColor(style));
        const out = this.globalCompositeOperation === 'destination-out';
        const ctm = this.ctm();
        const buf = this.buf;
        for (let py = y0; py < y1; py++) for (let px = x0; px < x1; px++) {
            let r = 0, g = 0, b = 0, a = 0;
            for (let k = 0; k < 4; k++) {
                const sxp = px + (k & 1 ? 0.75 : 0.25), syp = py + (k & 2 ? 0.75 : 0.25);
                if (!inside(sxp, syp)) continue;
                let c = solid;
                if (grad) c = grad.at((sxp - ctm.tx) / ctm.sx, (syp - ctm.ty) / ctm.sy);
                r += c[0]; g += c[1]; b += c[2]; a += c[3];
            }
            if (a === 0) continue;
            r *= 0.25; g *= 0.25; b *= 0.25; a *= 0.25;
            const i = (py * W + px) * 4;
            if (out) { const k = 1 - a; buf[i] *= k; buf[i + 1] *= k; buf[i + 2] *= k; buf[i + 3] *= k; }
            else { const k = 1 - a; buf[i] = r + buf[i] * k; buf[i + 1] = g + buf[i + 1] * k; buf[i + 2] = b + buf[i + 2] * k; buf[i + 3] = a + buf[i + 3] * k; }
        }
    }
    fillRect(x, y, w, h) {
        const [ax, ay] = this.T(x, y), [bx, by] = this.T(x + w, y + h);
        const x0 = Math.min(ax, bx), x1 = Math.max(ax, bx), y0 = Math.min(ay, by), y1 = Math.max(ay, by);
        this.paint([x0, y0, x1, y1], (px, py) => px >= x0 && px < x1 && py >= y0 && py < y1, this.fillStyle);
    }
    fill() {
        const shapes = this.path; if (!shapes.length) return;
        let bx0 = Infinity, by0 = Infinity, bx1 = -Infinity, by1 = -Infinity;
        const tests = [];
        for (const s of shapes) {
            if (s.ell) {
                const e = s.ell, c = e.ctm;
                const R = Math.max(e.rx, e.ry);
                const cx = e.x * c.sx + c.tx, cy = e.y * c.sy + c.ty;
                bx0 = Math.min(bx0, cx - R * Math.abs(c.sx)); bx1 = Math.max(bx1, cx + R * Math.abs(c.sx));
                by0 = Math.min(by0, cy - R * Math.abs(c.sy)); by1 = Math.max(by1, cy + R * Math.abs(c.sy));
                const cr = Math.cos(e.rot), sr = Math.sin(e.rot);
                tests.push((px, py) => {
                    const ux = (px - c.tx) / c.sx - e.x, uy = (py - c.ty) / c.sy - e.y;
                    const lx = ux * cr + uy * sr, ly = -ux * sr + uy * cr;
                    const qx = lx / e.rx, qy = ly / e.ry;
                    return qx * qx + qy * qy <= 1;
                });
            } else if (s.pts.length >= 3) {
                const P = s.pts;
                for (const [x, y] of P) { bx0 = Math.min(bx0, x); bx1 = Math.max(bx1, x); by0 = Math.min(by0, y); by1 = Math.max(by1, y); }
                tests.push((px, py) => {
                    let inside = false;
                    for (let i = 0, j = P.length - 1; i < P.length; j = i++) {
                        const xi = P[i][0], yi = P[i][1], xj = P[j][0], yj = P[j][1];
                        if ((yi > py) !== (yj > py) && px < (xj - xi) * (py - yi) / (yj - yi) + xi) inside = !inside;
                    }
                    return inside;
                });
            }
        }
        if (!tests.length) return;
        this.paint([bx0, by0, bx1, by1], (px, py) => { for (const t of tests) if (t(px, py)) return true; return false; }, this.fillStyle);
    }
    stroke() {
        const hw = this.lineWidth * this.sx / 2;
        const round = this.lineCap === 'round';
        let bx0 = Infinity, by0 = Infinity, bx1 = -Infinity, by1 = -Infinity;
        const segs = [], dots = [];
        for (const s of this.path) {
            if (!s.pts || s.pts.length === 0) continue;
            const P = s.pts;
            for (const [x, y] of P) { bx0 = Math.min(bx0, x - hw); bx1 = Math.max(bx1, x + hw); by0 = Math.min(by0, y - hw); by1 = Math.max(by1, y + hw); }
            if (P.length === 1) { if (round) dots.push(P[0]); continue; }
            for (let i = 1; i < P.length; i++) segs.push([P[i - 1], P[i], round || false]);
            for (let i = 1; i < P.length - 1; i++) dots.push(P[i]);            // 꺾이는 곳은 언제나 둥근 이음
            if (s.closed && P.length >= 2) { segs.push([P[P.length - 1], P[0], round || false]); dots.push(P[0]); dots.push(P[P.length - 1]); }   // closePath 한 경로는 마지막→처음 변도 긋는다
        }
        if (!segs.length && !dots.length) return;
        const hw2 = hw * hw;
        const inside = (px, py) => {
            for (const [a, b, rnd] of segs) {
                const dx = b[0] - a[0], dy = b[1] - a[1], L = dx * dx + dy * dy;
                if (L === 0) { if (rnd && (px - a[0]) ** 2 + (py - a[1]) ** 2 <= hw2) return true; continue; }
                let t = ((px - a[0]) * dx + (py - a[1]) * dy) / L;
                if (rnd) { if (t < 0) t = 0; else if (t > 1) t = 1; }
                else if (t < 0 || t > 1) continue;
                const qx = a[0] + dx * t - px, qy = a[1] + dy * t - py;
                if (qx * qx + qy * qy <= hw2) return true;
            }
            for (const d of dots) if ((px - d[0]) ** 2 + (py - d[1]) ** 2 <= hw2) return true;
            return false;
        };
        this.paint([bx0, by0, bx1, by1], inside, this.strokeStyle);
    }
    // ---- 8비트 왕복 ----
    getImageData(x, y, w, h) {
        const W = this.canvas.width, img = new ImageDataShim(w, h);
        for (let j = 0; j < h; j++) for (let i = 0; i < w; i++) {
            const s = ((y + j) * W + (x + i)) * 4, d = (j * w + i) * 4, a = this.buf[s + 3];
            img.data[d + 3] = clamp8(a * 255);
            if (a > 0) { img.data[d] = clamp8(this.buf[s] / a * 255); img.data[d + 1] = clamp8(this.buf[s + 1] / a * 255); img.data[d + 2] = clamp8(this.buf[s + 2] / a * 255); }
            else img.data[d] = img.data[d + 1] = img.data[d + 2] = 0;
        }
        return img;
    }
    putImageData(img, x, y) {
        const W = this.canvas.width, w = img.width, h = img.height;
        for (let j = 0; j < h; j++) for (let i = 0; i < w; i++) {
            const d = ((y + j) * W + (x + i)) * 4, s = (j * w + i) * 4, a = img.data[s + 3] / 255;
            this.buf[d] = img.data[s] / 255 * a; this.buf[d + 1] = img.data[s + 1] / 255 * a; this.buf[d + 2] = img.data[s + 2] / 255 * a; this.buf[d + 3] = a;
        }
    }
}

class Canvas {
    constructor() { this._w = 300; this._h = 150; this.ctx = null; }
    get width() { return this._w; } set width(v) { this._w = v; this.ctx = null; }
    get height() { return this._h; } set height(v) { this._h = v; this.ctx = null; }
    getContext() { return this.ctx || (this.ctx = new Context2D(this)); }
    bytes() { return this.getContext().getImageData(0, 0, this._w, this._h).data; }
}

// ---------------------------------------------------------------- 정본 로드
function load(src) {
    const web = path.join(src, 'web');
    const sb = { console };
    sb.self = sb;
    sb.document = { createElement: (tag) => { if (tag !== 'canvas') throw new Error('canvas 만'); return new Canvas(); } };
    vm.createContext(sb);
    vm.runInContext(fs.readFileSync(path.join(web, 'lib', 'three.min.js'), 'utf8'), sb, { filename: 'three.min.js' });
    for (const f of LOAD) vm.runInContext(fs.readFileSync(path.join(web, 'js', f), 'utf8'), sb, { filename: f });
    const g = (expr) => vm.runInContext(expr, sb);
    const scene3dSrc = fs.readFileSync(path.join(web, 'js', 'scene3d.js'), 'utf8');
    return { THREE: sb.THREE, Scene3D: g('Scene3D'), U: g('U'), M: g('Math'), g, scene3dSrc };
}

// buildTerrain 안의 포석 줄눈 데칼 블록은 함수가 아니라 인라인이다 — 소스에서 그 블록을 잘라 함수로 만든다(정본 글자 그대로).
function cobbleFn(w) {
    const src = w.scene3dSrc;
    const a = src.indexOf("const pc = document.createElement('canvas');");
    const b = src.indexOf('this.ground.add(this.pathMesh);', a);
    if (a < 0 || b < 0) throw new Error('포석 줄눈 블록을 못 찾았다 — 정본이 바뀌었다');
    const body = src.slice(a, b + 'this.ground.add(this.pathMesh);'.length);
    return w.g('(function () { ' + body + ' return this.pathMesh.material.map.image; })');
}

// ---------------------------------------------------------------- 벡터 요약
function fnv1a64(bytes) {
    let h = 0xcbf29ce484222325n; const P = 0x100000001b3n, M = (1n << 64n) - 1n;
    for (let i = 0; i < bytes.length; i++) { h ^= BigInt(bytes[i]); h = (h * P) & M; }
    return h.toString(16).padStart(16, '0');
}
function summarize(name, w, h, bytes) {
    const rows = new Array(h);
    for (let y = 0; y < h; y++) { let s = 0; for (let x = 0; x < w; x++) { const i = (y * w + x) * 4; s += bytes[i] + bytes[i + 1] + bytes[i + 2] + bytes[i + 3]; } rows[y] = s; }
    const rnd = xorshift32(0x9e3779b9 ^ w ^ (h << 16));
    const samples = [];
    for (let k = 0; k < 200; k++) {
        const x = Math.floor(rnd() * w), y = Math.floor(rnd() * h), i = (y * w + x) * 4;
        samples.push([x, y, bytes[i], bytes[i + 1], bytes[i + 2], bytes[i + 3]]);
    }
    let sum = 0; for (let i = 0; i < bytes.length; i++) sum += bytes[i];
    return { name, w, h, hash: fnv1a64(bytes), sum, rows, samples };
}

// PNG(비압축 zlib 아님 — 그냥 deflate) — 눈으로 보는 용도 · 레포에 넣지 않는다.
function writePng(file, w, h, bytes) {
    const raw = Buffer.alloc((w * 4 + 1) * h);
    for (let y = 0; y < h; y++) { raw[y * (w * 4 + 1)] = 0; Buffer.from(bytes.buffer, bytes.byteOffset + y * w * 4, w * 4).copy(raw, y * (w * 4 + 1) + 1); }
    const crcT = []; for (let n = 0; n < 256; n++) { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; crcT[n] = c >>> 0; }
    const crc = (b) => { let c = 0xffffffff; for (const x of b) c = crcT[(c ^ x) & 255] ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; };
    const chunk = (type, data) => { const len = Buffer.alloc(4); len.writeUInt32BE(data.length); const td = Buffer.concat([Buffer.from(type), data]); const cc = Buffer.alloc(4); cc.writeUInt32BE(crc(td)); return Buffer.concat([len, td, cc]); };
    const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = 6; ihdr[10] = 0; ihdr[11] = 0; ihdr[12] = 0;
    fs.writeFileSync(file, Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ihdr), chunk('IDAT', zlib.deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]));
}

// ---------------------------------------------------------------- 빌드
const KINS = ['forest', 'desert', 'rock', 'snow', 'lava', 'magic'];
const TINTED = ['marsh', 'salt', 'ash', 'glacier', 'obsidian', 'sanctum'];   // 신설 바이옴 표본(각 kin 하나씩 · tintGround 경로)

function build(src, pngDir) {
    const w = load(src);
    const { Scene3D, M } = w;
    const seed = (s) => { M.random = xorshift32(s); };
    const f = Object.create(Scene3D);
    f.renderer = null;
    f.ground = { add() {} };
    f._gtex = {};
    seed(SEED); f.crackNetwork();                 // 균열 그물은 한 번만 · 시드 SEED (알베도·노멀·발광이 공유)
    const out = { note: '정본 scene3d.js makeGroundTexture/makeGroundNormalMap/makeCrackTexture/포석 줄눈을 픽셀 버퍼 캔버스 시밍 위에서 실행 — tools/ground_vectors.js 가 만든다 · 손으로 고치지 않는다',
        seed: SEED, crackNet: f._crackNet.map(s => ({ depth: s.depth, pts: s.pts })), textures: [] };
    const png = (name, w_, h_, bytes) => { if (pngDir) writePng(path.join(pngDir, name + '.png'), w_, h_, bytes); };
    const add = (name, canvas) => { const b = canvas.bytes(); out.textures.push(summarize(name, canvas.width, canvas.height, b)); png(name, canvas.width, canvas.height, b); };
    for (const biome of KINS.concat(TINTED)) {
        seed(SEED); add('albedo_' + biome, f.makeGroundTexture(biome).image);
        seed(SEED); add('normal_' + biome, f.makeGroundNormalMap(biome).image);
    }
    seed(SEED); add('crack', f.makeCrackTexture().image);
    seed(SEED); add('cobble', cobbleFn(w).call(f));
    return out;
}

function main(argv) {
    let src = DEFAULT_SRC, out = DEFAULT_OUT, check = false, pngDir = null;
    for (let i = 0; i < argv.length; i++) {
        if (argv[i] === '--src') src = path.resolve(argv[++i]);
        else if (argv[i] === '--out') out = path.resolve(argv[++i]);
        else if (argv[i] === '--png') pngDir = path.resolve(argv[++i]);
        else if (argv[i] === '--check') check = true;
        else { console.error('모르는 인자: ' + argv[i]); return 2; }
    }
    if (!fs.existsSync(path.join(src, 'web', 'js', 'scene3d.js'))) { console.error('정본이 없다: ' + src); return 2; }
    if (pngDir) fs.mkdirSync(pngDir, { recursive: true });
    const t0 = Date.now();
    const text = JSON.stringify(build(src, pngDir)) + '\n';
    if (check) {
        const cur = fs.existsSync(out) ? fs.readFileSync(out, 'utf8') : '';
        if (cur !== text) { console.error('✗ ground_vectors: ' + path.relative(ROOT, out) + ' 이 정본과 다르다 — node tools/ground_vectors.js 로 다시 뽑아라'); return 1; }
        console.log('✓ ground_vectors: ' + path.relative(ROOT, out) + ' 이 정본과 같다 (' + ((Date.now() - t0) / 1000).toFixed(1) + 's)');
        return 0;
    }
    fs.mkdirSync(path.dirname(out), { recursive: true });
    fs.writeFileSync(out, text);
    console.log('✓ ground_vectors → ' + path.relative(ROOT, out) + ' (' + text.length + ' bytes · ' + ((Date.now() - t0) / 1000).toFixed(1) + 's)');
    return 0;
}

if (require.main === module) process.exit(main(process.argv.slice(2)));
module.exports = { Canvas, Context2D, clamp8, xorshift32, build };
