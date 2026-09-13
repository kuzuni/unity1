#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""T28 — 원작 화면 ↔ 클론 화면 비율 대조 자.

ROUTINE §2 T28: «화면마다 «요소 · x% · y% · w% · h%» 표를 원작 샷에서 5% 격자로 판독 →
우리 PNG 와 ±3%p 대조 → 점수. 8.0 미만이면 그 화면의 UI 작업을 «다음 고칠 것» 으로 재등재».

하는 일 셋:
  --gen    원작 샷(`.wwwww-src/web/ref/screens/shot-*.png`)을 판독해 `docs/ref-layout.md` 판독표를 만든다.
  --score  클론 샷(`screens` 브랜치의 `ui-screens/screen_*.png`)을 같은 자로 재고 판독표와 ±3%p 대조해 점수를 낸다.
  --self-test  자기 검사.

의존성 0 — PNG 해독·부호화까지 표준 라이브러리(zlib)로 한다(컨테이너·CI 에 PIL·numpy 가 없다).

판독 규칙(둘 다 같은 자를 쓴다 — 그것이 대조가 성립하는 근거다):
  ⓐ 행 얼룩 rowVar[y] = 그 행 픽셀의 밝기가 그 행 평균에서 얼마나 떨어졌는가(평균 절대편차).
     배경·여백 행은 0 에 가깝고 글자·카드가 있는 행은 크다.
  ⓑ rowVar 가 문턱(6/255)을 넘는 **연속 구간** = «밴드»(상단바 · 카드 한 장 · 탭바 …).
     한 밴드가 화면의 18%% 보다 높으면 **골짜기**(여백 행)에서 더 가른다 — 딤 모달처럼 모든 행이
     문턱을 넘는 화면이 통째로 한 밴드가 되는 것을 막는다.
  ⓒ 밴드 안에서 «바탕이 아닌» 픽셀 비율 colInk[x] 가 문턱을 넘는 구간 → «블록»(밴드 안 좌/우 덩어리).
  ⓓ 좌표는 그림 크기로 나눈 백분율. 5% 격자는 **사람 눈의 검산 단위**로 «격자» 칸을 같이 적는다
     (판독값 자체를 5% 로 양자화하면 오차 2.5%p 가 허용 오차 3%p 를 먹는다 · 결정 기록 참조).
"""

import argparse
import os
import re
import struct
import sys
import zlib

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REF_DIR = os.path.join(REPO, ".wwwww-src", "web", "ref", "screens")
TABLE = os.path.join(REPO, "docs", "ref-layout.md")
SHOTS_JS = os.path.join(REPO, ".wwwww-src", "web", "tools", "shot-screens.js")
SHOTS_CS = os.path.join(REPO, "Assets", "Tests", "PlayMode", "UiShotsTests.cs")

TOL = 3.0          # ±3%p (ROUTINE §2 T28)
PASS_MARK = 8.0    # 이 아래면 그 화면을 «다음 고칠 것» 으로 재등재
GRID = 5.0         # 사람 눈 검산 격자


# ─────────────────────────── PNG (해독 · 부호화) ───────────────────────────

class Img(object):
    """8비트 RGB 그림. `px` 는 w*h*3 바이트(알파는 흰 바탕에 합성해 없앤다)."""

    def __init__(self, w, h, px):
        self.w = w
        self.h = h
        self.px = px

    def gray(self):
        """밝기 배열(w*h · 0~255) — 한 번 계산해 둔다."""
        g = getattr(self, "_g", None)
        if g is not None:
            return g
        px = self.px
        g = bytearray(self.w * self.h)
        for i in range(self.w * self.h):
            j = i * 3
            # BT.601 휘도를 정수로 (2*R + 5*G + 1*B) / 8 — 부동소수 없이 결정론.
            g[i] = (2 * px[j] + 5 * px[j + 1] + px[j + 2]) >> 3
        self._g = g
        return g


def png_read(path):
    with open(path, "rb") as f:
        data = f.read()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("PNG 가 아니다: " + path)
    pos, idat, w, h, depth, ctype, inter, pal, trns = 8, [], 0, 0, 0, 0, 0, None, None
    while pos < len(data):
        (ln,) = struct.unpack(">I", data[pos:pos + 4])
        typ = data[pos + 4:pos + 8]
        body = data[pos + 8:pos + 8 + ln]
        if typ == b"IHDR":
            w, h, depth, ctype, _, _, inter = struct.unpack(">IIBBBBB", body)
        elif typ == b"PLTE":
            pal = body
        elif typ == b"tRNS":
            trns = body
        elif typ == b"IDAT":
            idat.append(body)
        elif typ == b"IEND":
            break
        pos += 12 + ln
    if depth != 8:
        raise ValueError("8비트 PNG 만 읽는다(이 그림은 %d비트): %s" % (depth, path))
    if inter != 0:
        raise ValueError("인터레이스 PNG 는 안 읽는다: " + path)
    chans = {0: 1, 2: 3, 3: 1, 4: 2, 6: 4}.get(ctype)
    if chans is None:
        raise ValueError("모르는 색 종류 %d: %s" % (ctype, path))
    raw = zlib.decompress(b"".join(idat))
    stride = w * chans
    out = bytearray(stride * h)
    prev = bytearray(stride)
    p = 0
    for y in range(h):
        ft = raw[p]
        p += 1
        line = bytearray(raw[p:p + stride])
        p += stride
        if ft == 1:
            for i in range(chans, stride):
                line[i] = (line[i] + line[i - chans]) & 0xFF
        elif ft == 2:
            for i in range(stride):
                line[i] = (line[i] + prev[i]) & 0xFF
        elif ft == 3:
            for i in range(stride):
                a = line[i - chans] if i >= chans else 0
                line[i] = (line[i] + ((a + prev[i]) >> 1)) & 0xFF
        elif ft == 4:
            for i in range(stride):
                a = line[i - chans] if i >= chans else 0
                c = prev[i - chans] if i >= chans else 0
                b = prev[i]
                pa, pb, pc = abs(b - c), abs(a - c), abs(a + b - 2 * c)
                pr = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                line[i] = (line[i] + pr) & 0xFF
        elif ft != 0:
            raise ValueError("모르는 필터 %d: %s" % (ft, path))
        out[y * stride:(y + 1) * stride] = line
        prev = line
    return Img(w, h, _to_rgb(out, w, h, ctype, chans, pal, trns))


def _to_rgb(buf, w, h, ctype, chans, pal, trns):
    """색 종류별로 RGB 로 편다 — 알파는 흰 바탕에 합성한다(캡처 배경이 흰 창이다)."""
    px = bytearray(w * h * 3)
    n = w * h
    for i in range(n):
        s = i * chans
        d = i * 3
        if ctype == 0:
            v = buf[s]
            px[d] = px[d + 1] = px[d + 2] = v
        elif ctype == 4:
            v, a = buf[s], buf[s + 1]
            v = (v * a + 255 * (255 - a)) // 255
            px[d] = px[d + 1] = px[d + 2] = v
        elif ctype == 2:
            px[d:d + 3] = buf[s:s + 3]
        elif ctype == 6:
            a = buf[s + 3]
            if a == 255:
                px[d:d + 3] = buf[s:s + 3]
            else:
                for k in range(3):
                    px[d + k] = (buf[s + k] * a + 255 * (255 - a)) // 255
        elif ctype == 3:
            idx = buf[s]
            px[d:d + 3] = pal[idx * 3:idx * 3 + 3]
            if trns is not None and idx < len(trns):
                a = trns[idx]
                for k in range(3):
                    px[d + k] = (px[d + k] * a + 255 * (255 - a)) // 255
    return px


def png_write(path_or_none, img):
    """자기 검사용 최소 부호화기(RGB8 · 필터 0). path 가 None 이면 바이트를 돌려준다."""
    raw = bytearray()
    for y in range(img.h):
        raw.append(0)
        raw += img.px[y * img.w * 3:(y + 1) * img.w * 3]

    def chunk(typ, body):
        return (struct.pack(">I", len(body)) + typ + body
                + struct.pack(">I", zlib.crc32(typ + body) & 0xFFFFFFFF))

    out = (b"\x89PNG\r\n\x1a\n"
           + chunk(b"IHDR", struct.pack(">IIBBBBB", img.w, img.h, 8, 2, 0, 0, 0))
           + chunk(b"IDAT", zlib.compress(bytes(raw), 6))
           + chunk(b"IEND", b""))
    if path_or_none is None:
        return out
    with open(path_or_none, "wb") as f:
        f.write(out)
    return out


# ─────────────────────────── 판독 (밴드 · 블록) ───────────────────────────

class Rect(object):
    __slots__ = ("name", "x", "y", "w", "h")

    def __init__(self, name, x, y, w, h):
        self.name = name
        self.x = round(x, 1)
        self.y = round(y, 1)
        self.w = round(w, 1)
        self.h = round(h, 1)

    def vals(self):
        return (self.x, self.y, self.w, self.h)

    def grid(self):
        """사람 눈 검산용 5% 격자 칸 — «가로 g0~g1 · 세로 g2~g3»."""
        f = lambda v: int(round(v / GRID))
        return "%d~%d,%d~%d" % (f(self.x), f(self.x + self.w), f(self.y), f(self.y + self.h))


def _runs(vals, thresh, min_len):
    """문턱을 넘는 연속 구간 [(시작, 끝배타), …]."""
    runs, s = [], None
    for i, v in enumerate(vals):
        if v > thresh:
            if s is None:
                s = i
        elif s is not None:
            if i - s >= min_len:
                runs.append((s, i))
            s = None
    if s is not None and len(vals) - s >= min_len:
        runs.append((s, len(vals)))
    return runs


def _merge(runs, gap):
    """gap 보다 가까운 구간은 한 덩어리로 — 글자 줄 사이 1px 틈이 밴드를 잘게 쪼개는 것을 막는다."""
    if not runs:
        return []
    out = [list(runs[0])]
    for a, b in runs[1:]:
        if a - out[-1][1] <= gap:
            out[-1][1] = b
        else:
            out.append([a, b])
    return [(a, b) for a, b in out]


def _row_var(img, y0, y1, x0, x1):
    """[y0,y1) 각 행의 밝기 평균 절대편차."""
    g, w = img.gray(), img.w
    out = []
    n = x1 - x0
    for y in range(y0, y1):
        base = y * w
        tot = 0
        for x in range(x0, x1):
            tot += g[base + x]
        mean = tot // n
        dev = 0
        for x in range(x0, x1):
            d = g[base + x] - mean
            dev += d if d >= 0 else -d
        out.append(dev / float(n))
    return out


def _band_bg(img, y0, y1, x0, x1):
    """밴드의 «바탕» 밝기 — 8단위로 뭉친 밝기의 최빈값. 여백·판 색이 잡힌다."""
    g, w = img.gray(), img.w
    hist = {}
    for y in range(y0, y1):
        base = y * w
        for x in range(x0, x1):
            k = g[base + x] >> 3
            hist[k] = hist.get(k, 0) + 1
    k = max(hist.items(), key=lambda kv: (kv[1], -kv[0]))[0]
    return (k << 3) + 4


def _col_ink(img, y0, y1, x0, x1):
    """[x0,x1) 각 열에서 «바탕이 아닌» 픽셀 비율(0~100) — 밴드 안의 덩어리를 가른다.

    열끼리 비교할 때 행 평균을 쓰면 내용이 행 평균을 끌어당겨 여백이 도리어 «내용» 으로 뒤집힌다
    (자기 검사 ②가 그것을 잡는다) — 바탕색과의 차이로 본다."""
    g, w = img.gray(), img.w
    bg = _band_bg(img, y0, y1, x0, x1)
    out = []
    n = y1 - y0
    for x in range(x0, x1):
        c = 0
        for y in range(y0, y1):
            d = g[y * w + x] - bg
            if (d if d >= 0 else -d) > 12:
                c += 1
        out.append(100.0 * c / n)
    return out


BAND_INK = 6.0     # 행 얼룩이 이보다 크면 «내용 있는 행»(255 중 · 여백·단색 바는 0~3)
BAND_MAX = 18      # 이보다 높은(%) 밴드는 골짜기에서 더 가른다


def _valleys(rv, y0, y1, H, k):
    """[y0,y1) 안에서 «골짜기» 행 — 창(±H/60) 안 최솟값이고 그 구간 중앙값의 k 배 이하.

    UI 는 요소 사이를 여백(=행 얼룩의 골짜기)으로 가른다. 문턱만 쓰면 어두운 딤 위에 뜬 모달처럼
    **모든 행이 문턱을 넘는** 화면이 통째로 한 밴드가 돼 대조할 것이 4값뿐이 된다(실측:
    `shot-042905` 대장간 목록 · `shot-042744` 설정) — 골짜기로 가르면 13밴드가 선다."""
    seg = rv[y0:y1]
    n = len(seg)
    med = sorted(seg)[n // 2]
    win = max(3, H // 60)
    cuts, i = [], win
    while i < n - win:
        if seg[i] <= med * k and seg[i] == min(seg[i - win:i + win + 1]):
            cuts.append(i)
            i += win
        else:
            i += 1
    return cuts


def _split(rv, y0, y1, H, depth=0):
    if y1 - y0 <= max(4, H * BAND_MAX // 100) or depth >= 3:
        return [(y0, y1)]
    cuts = _valleys(rv, y0, y1, H, 0.75)
    if not cuts:
        cuts = _valleys(rv, y0, y1, H, 0.92)   # 얼룩이 고른 밴드의 얕은 골짜기
    if not cuts:
        # 골짜기가 없다 = 밴드 안이 «고른 바닥 + 봉우리» 꼴이다(딤 모달). 그 밴드의 중앙값을
        # 바닥으로 보고 그것을 넘는 구간만 다시 집는다 — 위 단계가 화면 전체에 한 것과 같은 규칙.
        seg = rv[y0:y1]
        med = sorted(seg)[len(seg) // 2]
        runs = _merge(_runs(seg, med * 1.15, max(2, H // 100)), max(2, H // 100))
        if len(runs) < 2:
            return [(y0, y1)]
        out = []
        for a, b in runs:
            out += _split(rv, y0 + a, y0 + b, H, depth + 1)
        return out
    bounds = [y0] + [y0 + c for c in cuts] + [y1]
    out = []
    for a, b in zip(bounds, bounds[1:]):
        if b - a >= max(3, H // 100):
            out += _split(rv, a, b, H, depth + 1)
    return out


def read_layout(img, name="화면"):
    """그림 하나를 판독해 Rect 목록(밴드 + 밴드 안 블록)을 낸다 — 원작·클론에 같은 자를 쓴다."""
    W, H = img.w, img.h
    rv = _row_var(img, 0, H, 0, W)
    bands = []
    for a, b in _merge(_runs(rv, BAND_INK, max(2, H // 100)), max(2, H // 100)):
        bands += _split(rv, a, b, H)
    rects = []
    for bi, (y0, y1) in enumerate(bands):
        rects.append(Rect("밴드%d" % (bi + 1), 0.0, 100.0 * y0 / H, 100.0, 100.0 * (y1 - y0) / H))
        ci_ = _col_ink(img, y0, y1, 0, W)
        blocks = _merge(_runs(ci_, 8.0, max(2, W // 100)), max(3, W // 50))
        if len(blocks) < 2:
            continue                      # 밴드 전체가 한 덩어리면 블록 줄은 군더더기다
        for ci, (x0, x1) in enumerate(blocks):
            rects.append(Rect("밴드%d·블록%d" % (bi + 1, ci + 1),
                              100.0 * x0 / W, 100.0 * y0 / H,
                              100.0 * (x1 - x0) / W, 100.0 * (y1 - y0) / H))
    return rects


# ─────────────────────────── 짝 표 (원작 ↔ 클론) ───────────────────────────

def pairs():
    """[(화면 이름, 원작 파일 또는 None)] — 정본 `shot-screens.js` 의 SCREENS 순서 그대로.

    정본이 옆에 없으면 T27 의 `UiShotsTests.cs` 에서 같은 표를 읽는다(둘은 같아야 한다)."""
    if os.path.exists(SHOTS_JS):
        src = open(SHOTS_JS, encoding="utf-8").read()
        i = src.find("const SCREENS = [")
        if i >= 0:
            out = []
            for m in re.finditer(r"^\s*\['([a-z0-9-]+)',\s*(?:'(\d+)'|null)",
                                 src[i:], re.M):
                out.append((m.group(1), ("shot-%s.png" % m.group(2)) if m.group(2) else None))
            if out:
                return out
    if os.path.exists(SHOTS_CS):
        src = open(SHOTS_CS, encoding="utf-8").read()
        out = []
        for m in re.finditer(r'Name = "([a-z0-9-]+)", Ref = (?:"(\d+)"|null)', src):
            out.append((m.group(1), ("shot-%s.png" % m.group(2)) if m.group(2) else None))
        return out
    return []


# ─────────────────────────── 판독표 (읽기 · 쓰기) ───────────────────────────

HEAD = u"""# 원작 ↔ 클론 화면 비율 판독표 (T28)

> **자가 만든다 — 손으로 고치지 않는다.** `python3 tools/ui_score.py --gen` 이 정본 샷
> (`.wwwww-src/web/ref/screens/shot-*.png`)을 판독해 이 파일을 다시 쓴다.
> 대조는 `python3 tools/ui_score.py --score` — 클론 PNG(`ui-screens/screen_<이름>.png` ·
> `screens` 브랜치)를 **같은 자**로 재서 이 표와 ±%(tol)s%%p 로 맞춰 보고 화면마다 10점 만점을 낸다.
> %(pass)s 점 미만인 화면은 그 화면의 UI 작업을 «다음 고칠 것» 으로 재등재한다(ROUTINE §2 T28).
>
> 좌표는 **그림 크기의 백분율**이다. «격자» 칸은 사람 눈 검산용 5%% 격자 칸 번호
> (`가로 시작~끝,세로 시작~끝`) — 판독값 자체는 0.1%% 단위로 둔다(5%% 로 양자화하면
> 오차 2.5%%p 가 허용 오차 3%%p 를 먹는다).
>
> 판독 규칙: 행 얼룩(그 행 밝기의 평균 절대편차)이 문턱을 넘는 연속 구간 = **밴드**,
> 밴드 안에서 열 얼룩으로 같은 규칙 = **블록**. 원작과 클론에 같은 자를 쓴다.
"""


def gen(ref_dir, out_path, only=None, quiet=False):
    rows = pairs()
    if not rows:
        print(u"✗ 짝 표를 못 찾았다 — 정본(.wwwww-src) 또는 UiShotsTests.cs 가 있어야 한다")
        return 1
    body = [HEAD % {"tol": TOL, "pass": PASS_MARK}]
    n_screen = 0
    for name, ref in rows:
        if only and name not in only:
            continue
        if ref is None:
            body.append(u"\n## %s ↔ (원작 샷 없음)\n\n> 정본 `SCREENS` 가 짝을 안 둔 화면이다 — 대조에서 뺀다.\n"
                        % name)
            continue
        path = os.path.join(ref_dir, ref)
        if not os.path.exists(path):
            body.append(u"\n## %s ↔ `%s`\n\n> ⚠ 원작 샷이 없다(`%s`) — 대조에서 뺀다.\n" % (name, ref, path))
            continue
        img = png_read(path)
        rects = read_layout(img, name)
        body.append(u"\n## %s ↔ `%s` (원작 %d×%d · 세로/가로 %.3f)\n" % (
            name, ref, img.w, img.h, img.h / float(img.w)))
        body.append(u"\n| 요소 | x% | y% | w% | h% | 격자 |\n|---|---|---|---|---|---|\n")
        for r in rects:
            body.append(u"| %s | %.1f | %.1f | %.1f | %.1f | %s |\n"
                        % (r.name, r.x, r.y, r.w, r.h, r.grid()))
        n_screen += 1
        if not quiet:
            print(u"  · %-18s %s — 요소 %d" % (name, ref, len(rects)))
    with open(out_path, "w", encoding="utf-8") as f:
        f.write(u"".join(body))
    if not quiet:
        print(u"✓ ui_score --gen: 화면 %d개 판독 → %s" % (n_screen, os.path.relpath(out_path, REPO)))
    return 0


HEAD_RE = re.compile(r"^##\s+([a-z0-9-]+)\s+↔\s+`([^`]+)`(?:\s+\(원작\s+(\d+)×(\d+)[^)]*\))?", re.M)
ROW_RE = re.compile(r"^\|\s*([^|]+?)\s*\|\s*([-\d.]+)\s*\|\s*([-\d.]+)\s*\|\s*([-\d.]+)\s*\|\s*([-\d.]+)\s*\|")


def load_table(path):
    """판독표 → {화면: {'ref':파일, 'wh':(w,h) 또는 None, 'rects':[Rect]}}"""
    if not os.path.exists(path):
        return {}
    out, cur = {}, None
    for line in open(path, encoding="utf-8"):
        m = HEAD_RE.match(line)
        if m:
            wh = (int(m.group(3)), int(m.group(4))) if m.group(3) else None
            cur = {"ref": m.group(2), "wh": wh, "rects": []}
            out[m.group(1)] = cur
            continue
        if cur is None:
            continue
        m = ROW_RE.match(line)
        if m and m.group(1) not in ("요소",) and not m.group(1).startswith("--"):
            cur["rects"].append(Rect(m.group(1), float(m.group(2)), float(m.group(3)),
                                     float(m.group(4)), float(m.group(5))))
    return out


# ─────────────────────────── 대조 · 점수 ───────────────────────────

def _overlap(a0, a1, b0, b1):
    return max(0.0, min(a1, b1) - max(a0, b0))


def match(ref_rects, got_rects):
    """원작 요소 ↔ 클론 요소 짝짓기 — 겹치는 넓이가 가장 큰 것끼리(한 번씩만)."""
    used, out = set(), []
    for r in ref_rects:
        best, best_ov = None, 0.0
        for i, g in enumerate(got_rects):
            if i in used:
                continue
            ov = (_overlap(r.y, r.y + r.h, g.y, g.y + g.h)
                  * _overlap(r.x, r.x + r.w, g.x, g.x + g.w))
            if ov > best_ov:
                best, best_ov = i, ov
        if best is not None:
            used.add(best)
            out.append((r, got_rects[best]))
        else:
            out.append((r, None))
    extra = [g for i, g in enumerate(got_rects) if i not in used]
    return out, extra


def score_screen(ref_rects, got_rects):
    """10점 만점. 짝지은 요소의 x·y·w·h 네 값마다 ±TOL 안이면 1점씩 · 짝 없는 요소는 4실점 ·
    원작에 없는데 클론에만 있는 요소는 1실점(군더더기)."""
    pairs_, extra = match(ref_rects, got_rects)
    ok = tot = 0
    worst = []
    for r, g in pairs_:
        if g is None:
            tot += 4
            worst.append((99.9, r.name, u"짝 없음"))
            continue
        dmax, dwhich = 0.0, ""
        for lab, a, b in (("x", r.x, g.x), ("y", r.y, g.y), ("w", r.w, g.w), ("h", r.h, g.h)):
            d = abs(a - b)
            tot += 1
            if d <= TOL:
                ok += 1
            if d > dmax:
                dmax, dwhich = d, lab
        if dmax > TOL:
            worst.append((dmax, r.name, u"%s %.1f%%p 어긋남" % (dwhich, dmax)))
    for g in extra:
        tot += 1
        worst.append((TOL + 0.1, g.name, u"원작에 없는 요소"))
    if tot == 0:
        return 0.0, [u"원작 요소가 0개다"]
    worst.sort(key=lambda t: -t[0])
    return 10.0 * ok / tot, [u"%s — %s" % (n, w) for _, n, w in worst[:5]]


# ── 앱 상자가 그림을 채우는가 (T28 6회차 · 워커 M) ─────────────────────────
# 촬영 프레임이 눌리면(런 108: 3D 카메라 띠 안에 UI 까지 그려 앱 상자가 위 49% 만 차지) 모든 화면의
# y%% 가 통째로 밀려 «UI 가 30군데 망가진 것» 처럼 보인다. 그것은 점수가 아니라 **틀**이 어긋난 것이다.
FILL_H_MIN = 0.90   # 세로 채움 하한 — 정상 런 실측 0.94~1.00 · 눌린 런 0.487
FILL_W_MIN = 0.80   # 가로 채움 하한 — 정상 런 실측 0.91~1.00


def content_fill(img):
    """그림에서 «바탕이 아닌 것» 이 차지하는 세로·가로 비. 바탕색은 테두리 픽셀의 최빈값."""
    g = img.gray()
    W, H = img.w, img.h
    cnt = {}
    for x in range(0, W, 3):
        for v in (g[x], g[(H - 1) * W + x]):
            cnt[v] = cnt.get(v, 0) + 1
    for y in range(0, H, 3):
        for v in (g[y * W], g[y * W + W - 1]):
            cnt[v] = cnt.get(v, 0) + 1
    bg = max(cnt.items(), key=lambda kv: kv[1])[0] if cnt else 0
    rows = [y for y in range(H)
            if sum(1 for x in range(0, W, 2) if abs(g[y * W + x] - bg) > 12) > W * 0.005]
    cols = [x for x in range(W)
            if sum(1 for y in range(0, H, 2) if abs(g[y * W + x] - bg) > 12) > H * 0.005]
    if not rows or not cols:
        return 0.0, 0.0
    return (rows[-1] - rows[0] + 1) / float(H), (cols[-1] - cols[0] + 1) / float(W)


def score(table_path, shots_dir, only=None):
    table = load_table(table_path)
    if not table:
        print(u"✗ 판독표가 없다(%s) — 먼저 `--gen` 을 돌린다" % os.path.relpath(table_path, REPO))
        return 1
    if not os.path.isdir(shots_dir):
        print(u"✗ 클론 샷 폴더가 없다: %s" % shots_dir)
        print(u"  `git fetch origin screens && git show origin/screens:screen_main.png > …` 로 받아 둔다"
              u" — T27 촬영이 CI 에서 돈 뒤에 생긴다.")
        return 2
    scores, missing, bad, skewed, unfilled = [], [], [], [], []
    for name in [n for n, _ in pairs()]:
        if only and name not in only:
            continue
        ent = table.get(name)
        if not ent or not ent["rects"]:
            continue
        path = os.path.join(shots_dir, "screen_%s.png" % name)
        if not os.path.exists(path):
            missing.append(name)
            continue
        img = png_read(path)
        if ent["wh"]:
            ra = ent["wh"][1] / float(ent["wh"][0])
            rb = img.h / float(img.w)
            if abs(ra - rb) > ra * 0.03:
                # 틀이 다르면 같은 자리도 y%% 가 밀린다 — 점수가 아니라 **틀**이 어긋난 것이다.
                print(u"  ⚠ %-18s 틀 불일치: 원작 세로/가로 %.3f ↔ 클론 %.3f (%dx%d) — 앱 상자를 9:16 으로 잘라 찍는다"
                      % (name, ra, rb, img.w, img.h))
                skewed.append(name)
        fh, fw = content_fill(img)
        if fh < FILL_H_MIN or fw < FILL_W_MIN:
            # 앱 상자가 그림을 안 채우면 모든 자리의 y%% 가 같은 비로 밀린다 — 화면마다 고칠 것이 아니라 촬영이 어긋난 것이다.
            print(u"  ⚠ %-18s 앱 상자가 그림을 안 채운다: 세로 채움 %.2f · 가로 채움 %.2f (하한 %.2f/%.2f)"
                  u" — 촬영 프레임 문제다(점수는 이 뒤에 다시 잰다)"
                  % (name, fh, fw, FILL_H_MIN, FILL_W_MIN))
            unfilled.append(name)
        s, why = score_screen(ent["rects"], read_layout(img, name))
        scores.append((name, s))
        mark = u"✓" if s >= PASS_MARK else u"✗"
        print(u"  %s %-18s %4.1f / 10   (원작 요소 %d)" % (mark, name, s, len(ent["rects"])))
        if s < PASS_MARK:
            bad.append(name)
            for w in why:
                print(u"        · " + w)
    if missing:
        print(u"  · 클론 샷이 아직 없는 화면 %d개: %s" % (len(missing), " ".join(missing)))
    if skewed:
        print(u"  · ⚠ 틀(세로/가로)이 원작과 다른 화면 %d개: %s — 점수보다 이것이 먼저다"
              % (len(skewed), " ".join(skewed)))
    if unfilled:
        print(u"  · ⚠ 앱 상자가 그림을 안 채운 화면 %d개: %s" % (len(unfilled), " ".join(unfilled)))
        print(u"    이 런의 점수는 «UI 가 그만큼 망가졌다» 가 아니다 — **촬영이 어긋난 것**이라"
              u" 화면마다 재등재하지 말고 촬영을 먼저 고친다(T27·T54 갈래).")
    if not scores:
        print(u"✗ 점수를 낸 화면이 0개다 — 클론 샷(`screen_*.png`)이 하나도 없다")
        return 2
    avg = sum(s for _, s in scores) / len(scores)
    print(u"─" * 60)
    print(u"평균 %.2f / 10 · 화면 %d개 · %s점 미만 %d개" % (avg, len(scores), PASS_MARK, len(bad)))
    if bad:
        print(u"«다음 고칠 것»(ROUTINE §2 T28 · 그 화면의 UI 작업을 재등재한다): " + " ".join(bad))
        return 1
    return 0


# ─────────────────────────── 자기 검사 ───────────────────────────

def _canvas(w, h, bg=(245, 245, 245)):
    px = bytearray(w * h * 3)
    for i in range(w * h):
        px[i * 3:i * 3 + 3] = bytes(bg)
    return Img(w, h, px)


def _fill(img, x0, y0, x1, y1, c):
    for y in range(y0, y1):
        for x in range(x0, x1):
            d = (y * img.w + x) * 3
            img.px[d:d + 3] = bytes(c)


def _stripes(img, x0, y0, x1, y1):
    """얼룩을 만드는 줄무늬 — «내용이 있는 밴드» 를 흉내낸다."""
    for y in range(y0, y1):
        for x in range(x0, x1):
            d = (y * img.w + x) * 3
            v = 20 if ((x >> 1) & 1) else 230
            img.px[d:d + 3] = bytes((v, v, v))


def self_test():
    ok = [0]
    fail = []

    def chk(cond, label):
        ok[0] += 1
        print((u"  ✓ " if cond else u"  ✗ ") + label)
        if not cond:
            fail.append(label)

    # ① PNG 부호화 → 해독 왕복
    img = _canvas(40, 30, (10, 20, 30))
    _fill(img, 5, 5, 20, 20, (200, 100, 50))
    blob = png_write(None, img)
    tmp = os.path.join(REPO, "tools", ".ui_score_selftest.png")
    with open(tmp, "wb") as f:
        f.write(blob)
    back = png_read(tmp)
    os.remove(tmp)
    same = (back.w, back.h) == (40, 30) and bytes(back.px) == bytes(img.px)
    chk(same, u"PNG 부호화 → 해독 왕복이 픽셀까지 같다")

    # ② 판독: 만든 자리의 밴드를 그 자리에서 찾는다
    a = _canvas(100, 200)
    _stripes(a, 0, 20, 100, 40)      # 밴드 y 10~20%
    _stripes(a, 10, 120, 40, 160)    # 밴드 y 60~80% 의 왼쪽 블록
    _stripes(a, 60, 120, 90, 160)    # 같은 밴드 오른쪽 블록
    ra = read_layout(a)
    bands = [r for r in ra if u"블록" not in r.name]
    chk(len(bands) == 2, u"밴드 2개를 찾는다 (찾은 수 %d)" % len(bands))
    chk(bands and abs(bands[0].y - 10.0) <= 1.0 and abs(bands[0].h - 10.0) <= 1.0,
        u"첫 밴드가 y 10% · h 10% 자리다")
    blocks = [r for r in ra if r.name.startswith(u"밴드2·블록")]
    chk(len(blocks) == 2, u"둘째 밴드 안에서 블록 2개를 가른다 (찾은 수 %d)" % len(blocks))
    chk(bool(blocks) and abs(blocks[0].x - 10.0) <= 1.5 and abs(blocks[1].x - 60.0) <= 1.5,
        u"블록의 x 가 10% · 60% 자리다")

    # ③ 격자 칸(사람 눈 검산)
    chk(Rect(u"t", 10.0, 20.0, 30.0, 10.0).grid() == "2~8,4~6", u"5% 격자 칸을 제대로 센다")

    # ④ 점수: 같은 그림이면 10점
    s, _ = score_screen(ra, read_layout(a))
    chk(abs(s - 10.0) < 1e-9, u"같은 그림이면 10.0 점 (낸 점수 %.2f)" % s)

    # ⑤ 점수: 밴드를 10%p 내리면 8점 미만
    b = _canvas(100, 200)
    _stripes(b, 0, 30, 100, 50)      # 밴드·블록을 통째로 5%p 아래로
    _stripes(b, 10, 130, 40, 170)
    _stripes(b, 60, 130, 90, 170)
    s2, why = score_screen(ra, read_layout(b))
    chk(s2 < PASS_MARK, u"밴드가 5%%p 밀리면 %s점 미만 (낸 점수 %.2f)" % (PASS_MARK, s2))
    chk(any(u"어긋남" in w for w in why), u"어긋난 값을 이유로 찍는다")

    # ⑥ 점수: 원작에 없는 밴드가 하나 늘면 감점된다
    c = _canvas(100, 200)
    _stripes(c, 0, 20, 100, 40)
    _stripes(c, 10, 120, 40, 160)
    _stripes(c, 60, 120, 90, 160)
    _stripes(c, 0, 180, 100, 195)
    s3, why3 = score_screen(ra, read_layout(c))
    chk(s3 < 10.0 and any(u"원작에 없는" in w for w in why3),
        u"클론에만 있는 요소는 감점이다 (낸 점수 %.2f)" % s3)

    # ⑦ 딤 위에 뜬 모달처럼 «모든 행이 문턱을 넘는» 화면도 골짜기에서 갈린다
    d = _canvas(100, 200, (60, 60, 60))          # 딤 바탕
    _fill(d, 12, 20, 88, 180, (250, 250, 250))   # 흰 판(화면의 80%)
    _stripes(d, 20, 40, 80, 60)                  # 판 안 내용 두 줄
    _stripes(d, 20, 120, 80, 140)
    rd = [r for r in read_layout(d) if u"블록" not in r.name]
    chk(len(rd) == 2, u"딤 모달도 통째로 한 밴드가 되지 않고 내용 줄 2개로 갈린다 (찾은 수 %d)" % len(rd))
    chk(all(r.h <= 20.0 for r in rd) and abs(rd[0].y - 20.0) <= 1.5,
        u"갈린 밴드가 내용 줄 자리(y 20%)에 맞는다")

    # ⑦ 판독표 왕복(쓰기 → 읽기)
    tmp_md = os.path.join(REPO, "tools", ".ui_score_selftest.md")
    with open(tmp_md, "w", encoding="utf-8") as f:
        f.write(u"## main ↔ `shot-042120.png` (원작 499×892 · 세로/가로 1.787)\n\n")
        f.write(u"| 요소 | x% | y% | w% | h% | 격자 |\n|---|---|---|---|---|---|\n")
        for r in ra:
            f.write(u"| %s | %.1f | %.1f | %.1f | %.1f | %s |\n" % (r.name, r.x, r.y, r.w, r.h, r.grid()))
    t = load_table(tmp_md)
    os.remove(tmp_md)
    chk("main" in t and t["main"]["ref"] == "shot-042120.png" and t["main"]["wh"] == (499, 892),
        u"판독표 머리(화면 이름 · 원작 파일 · 크기)를 읽는다")
    chk("main" in t and len(t["main"]["rects"]) == len(ra),
        u"판독표 행 수가 왕복해도 같다 (%d)" % len(ra))

    # ⑧ 짝 표가 정본·T27 어느 쪽에서든 선다
    pr = pairs()
    chk(len(pr) >= 30 and pr[0][0] == "main" and pr[0][1] == "shot-042120.png",
        u"짝 표 첫 줄이 main ↔ shot-042120.png 다 (줄 %d)" % len(pr))

    # ⑯ 앱 상자 채움: 꽉 찬 그림은 1.0 에 가깝고, 위 절반만 쓰는 그림은 하한 아래다(런 108 실측 0.487)
    full = _canvas(100, 200, (8, 8, 8))
    _fill(full, 4, 4, 96, 196, (230, 230, 230))
    fh, fw = content_fill(full)
    chk(fh >= FILL_H_MIN and fw >= FILL_W_MIN,
        u"꽉 찬 그림은 채움 하한 위다 (세로 %.2f · 가로 %.2f)" % (fh, fw))

    half = _canvas(100, 200, (8, 8, 8))
    _fill(half, 4, 4, 96, 96, (230, 230, 230))   # 위 절반만
    fh2, fw2 = content_fill(half)
    chk(fh2 < FILL_H_MIN, u"위 절반만 쓰는 그림은 «앱 상자가 안 채운다» 로 걸린다 (세로 %.2f)" % fh2)

    print(u"")
    if fail:
        print(u"✗ ui_score self-test: %d/%d 칸 실패" % (len(fail), ok[0]))
        return 1
    print(u"✓ ui_score self-test 통과 (%d칸)" % ok[0])
    return 0


# ─────────────────────────── 들머리 ───────────────────────────

def main():
    ap = argparse.ArgumentParser(description="T28 원작 ↔ 클론 화면 비율 대조")
    ap.add_argument("--gen", action="store_true", help="원작 샷을 판독해 docs/ref-layout.md 를 다시 쓴다")
    ap.add_argument("--score", action="store_true", help="클론 샷을 판독표와 대조해 점수를 낸다")
    ap.add_argument("--self-test", action="store_true", help="자기 검사")
    ap.add_argument("--read", metavar="PNG", help="PNG 하나를 판독해 표 행을 찍는다")
    ap.add_argument("--ref-dir", default=REF_DIR, help="원작 샷 폴더")
    ap.add_argument("--shots", default=os.path.join(REPO, "ui-screens"), help="클론 샷 폴더")
    ap.add_argument("--table", default=TABLE, help="판독표 경로")
    ap.add_argument("--only", nargs="*", help="이 화면 이름만")
    a = ap.parse_args()

    if a.self_test:
        return self_test()
    if a.read:
        for r in read_layout(png_read(a.read)):
            print(u"| %s | %.1f | %.1f | %.1f | %.1f | %s |" % (r.name, r.x, r.y, r.w, r.h, r.grid()))
        return 0
    if a.gen:
        return gen(a.ref_dir, a.table, a.only)
    if a.score:
        return score(a.table, a.shots, a.only)
    ap.print_help()
    return 0


if __name__ == "__main__":
    sys.exit(main())
