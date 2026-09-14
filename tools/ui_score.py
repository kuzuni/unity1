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
BASELINE = os.path.join(REPO, "docs", "ui-score-baseline.json")  # T28 회차 사이 점수(회귀 탐지 · 워커 M 7회차)
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


# ⚠ 행 얼룩을 창(3·5·9)으로 **고르게 펴 보는 길은 이미 재 봤고 버렸다**(T28 24회차 실측 · 런 214↔219):
#   회차 사이 |Δ| 합은 6.5 → 6.0 → 3.9 → 3.4 로 줄지만 평균이 4.70 → 4.34 → 4.14 → **3.80** 으로 같이 무너진다
#   (밴드가 뭉개져 자가 재는 것이 줄어든다 · 창 3 은 최대 |Δ| 가 되레 1.3 → 2.3 으로 커졌다).
#   경계가 걸려 점수만 튀는 회차는 «자를 무디게» 가 아니라 **지문**(안/뒤 차)으로 가른다.
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


# ── 원작 샷이 «지금 정본» 과 다른 자리 (T28 14회차 · 워커 M) ─────────────
# `web/ref/screens/shot-*.png` 30장은 **찍힌 시점의 원작**이다. 그 뒤 주인 지시로 정본이 바뀐 자리가 있고,
# 그런 자리는 클론이 «정본대로» 여도 점수가 안 오른다 — 회차마다 그것을 결함으로 다시 진단하는 일을 막는다.
STALE_REF_NOTES = [
    (u"3D 세계 — 원작 샷엔 나무·흙길, 정본은 `SIMPLE_BG`(주인 지시) 단색 지면 — `main`·`offline`·`gear-detail` 세계 밴드는 T35 전까지 못 맞춘다",
    u"3D 세계: 원작 샷에는 나무·흙길·능선이 있다. 배포 정본은 `SIMPLE_BG: true`(주인 지시)라 단색 지면이다 "
    u"— `main`·`offline`·`gear-detail` 의 세계 밴드는 T35(배경 복원) 전까지 못 맞춘다(결정 134)."),
    (u"모달 딤 — 원작 샷은 딤 α .988, 정본은 주인 지시 .5 — 팝업 화면 천장이 5점대다 · T94 뒤 클론 딤이 더 어두워져 **원작 쪽으로 왔는데 점수는 내려갔다**(대비가 줄면 밴드가 붙는다)",
    u"모달 딤: 원작 샷은 딤 α ≈ .988 시절이라 팝업 뒤가 새카맣다. 지금 정본은 주인 지시 «투명도 50%» 의 .5 다 "
    u"— 팝업 화면에서 상단바·탭바가 «보이는» 것은 결함이 아니다(T93 오진 · 결정 214). "
    u"실측(T28 18회차): 그 어두운 자리에 원작 요소의 **33~43%** 가 있다(`forge-detail` 43 · `autoforge` 38 · "
    u"`settings` 35 · `forge-list` 33) — 이 팝업 화면들의 점수 천장은 6점대가 아니라 **5점대**다. 회차 사이 «변화» 로 읽어라. "
     u"**T28 34회차 실측**: T94(딤을 정본 지각 밝기로)가 든 런 299 에서 팝업 뒤 상단바가 25.6 → 13.3(원작 1.1)로 **원작에 가까워졌는데** `offline` −3.1 · `dungeon-detail` −2.7 처럼 점수는 내려갔다 — 뒤가 어두워지면 행 얼룩이 문턱 아래로 내려가 밴드가 붙기 때문이다. 되돌리지 마라."),
    (u"제작 비교 버튼 — 원작은 «판매»·«장착» 한 단어, 정본은 `<small>` 로 금액·«기존 교체» — 클론의 두 줄 버튼이 정본대로",
    u"제작 비교 버튼: 원작 샷(`shot-043224`)의 버튼은 «판매»·«장착» 한 단어인데 지금 정본은 `<small>` 로 "
    u"판매액(코인+금액)과 «기존 교체»/«다시 장착» 을 단다(`ui.js` 3266·3269 — 그 코드 주석이 그 샷을 대놓고 "
    u"«타이틀 줄 없음, 버튼 라벨은 판매/장착만» 이라 적었다). 클론의 두 줄 버튼은 **정본대로**다(T28 15회차 실측)."),
    (u"시대 이름 — 원작 «신성한» ↔ 정본 `AGE_KR.divine = '천상'` — 클론이 맞다(이름표는 data/*.json 이라 손대기 금지)",
    u"시대 이름 «천상»: 원작 샷(`shot-042950`)은 «신성한» 인데 지금 정본 `gamedata.js` 16~19 의 "
    u"`AGE_KR.divine` 은 **«천상»** 이다(그 아래 색 주석에는 옛 이름이 남아 있다) — 클론이 정본대로다. "
    u"이름표는 `data/*.json` 이 쥐고 있고 **손으로 고치는 것이 금지**라, 이 차이를 결함으로 읽지 마라(T28 30회차)."),
    (u"기술 트리 행 — 원작은 2노드 행부터, 정본은 단계마다 «첫 타입 단독 → 2개씩»(주인 재지시) — 클론의 맨 위 단독 노드가 맞다",
    u"기술 트리 행 모양: 원작 샷(`shot-042546`)은 **2노드 행 셋 → 단독 행** 꼴인데 지금 정본은 단계마다 "
    u"**«첫 타입 단독 → 나머지 2개씩»**(`techtree.js` 264~286 · 2026-08-17 주인 재지시 «패턴이 단계마다 같아야 "
    u"연결선이 세로 레일이 된다»)이다 — 클론이 맨 위 단독 노드로 시작하는 것은 **정본대로**다(T28 29회차). "
    u"`style.css` 2118~2135 주석의 원작 픽셀 좌표((171,127)(324,127)…)도 그 옛 배치를 적어 둔 것이라 "
    u"지금 화면과 안 맞는다 — 노드 자리로 점수를 쫓지 마라."),
    (u"아이콘 블록화 — 원작 샷의 매끈한 벡터는 블록화 이전 — 지금 정본은 주인 지시 `ui-icon-blockify` 로 칸 다운샘플+최근접 확대. 던전 배너 «뭉갬» 을 다시 그리지 마라",
    u"아이콘·던전 배너가 **뭉툭한 픽셀 블록**인 것: 원작 샷(`shot-042304`·`shot-042251`)의 매끈한 벡터 그림"
    u"(마을 실루엣·구름·쥔 망치)은 **블록화 이전 시절**이다. 지금 정본은 주인 지시 `ui-icon-blockify`"
    u"(«게임 전반 UI 아이콘을 네모네모(마크/픽셀 블록) 느낌으로»)로 `IconGen.url` 출력 직전에 칸 다운샘플 →"
    u"최근접 확대를 한다(`icongen.js` 120~132 · 690~712). 클론 아틀라스가 8px 칸으로 각진 것은 **정본대로**다"
    u"(T28 28회차: `dg_hammer` 슬라이스를 직접 잘라 확인). 던전 배너가 «뭉갰다» 고 다시 그리지 마라."),
    (u"장비 상세 ✕ — 정본이 2026-08-18 에 더한 ✕ 라 클론이 맞다 — 다만 같은 화면의 카드 자리·폭은 진짜 결함이었다(T111 ✅)",
    u"장비 상세의 빨간 ✕: 원작 샷(`shot-043244`)에는 없고 클론에는 있다 — 정본이 2026-08-18 에 **더한 것**이다"
    u"(`ui.js` 3289 주석 «이 팝업만 ✕ 가 없어서 화면에 보이는 닫는 길이 하나도 없었다» · `style.css` 1733). "
    u"클론의 ✕ 가 카드 아래로 반쯤 걸치는 것도 정본대로다(T28 23회차). 다만 같은 화면의 **카드 자리·폭**은 "
    u"진짜로 어긋나 있다(바닥 −17.8%p · 폭 +4.4%p → T111)."),
    (u"펫 업그레이드 선택 줄 — 원작의 «선택 알 하나 + 밑줄 5칸» 은 개수 제한 시절 · 정본은 등급별 일괄 선택 칩 — 클론의 칩 줄이 맞다",
     u"펫 업그레이드 «합칠 펫» 자리: 원작 샷(`shot-042503`)은 선택 슬롯 다섯 칸인데 지금 정본은 "
     u"`.petup-bulkrow`(`style.css` 4365 «등급별 일괄 선택 버튼 행 — 5칸 선택 슬롯 대체 (사용자 지시: **개수 제한 철폐**)»)다 "
     u"— 클론이 칩 줄을 그리는 것은 **정본대로**다(T28 33회차)."),
    (u"장비 시대 상세 카드 — T177 로 정본 치수에 들어갔는데 점수는 2.2 → 1.1 로 **떨어졌다**. 점수로 되돌리지 마라",
     u"T177(워커 S · 런 422)이 `forge-detail` 카드를 정본 `style.css` 3649 `width: 68.9%` 와 3714 주석의 "
     u"«카드 25.20~72.76%H» 에 맞췄다 — 46회차 실측으로 카드 폭 73.7 → **67.8%W**, 높이 53.5 → **48.3%H** 로 "
     u"원작(68.5 · 47.6)에 ±1%p 안에 들어왔다. 그런데 밴드 점수는 **2.2 → 1.1** 로 내려갔다. 까닭은 딤이다: "
     u"원작은 α .988 이라 카드 바깥이 순검정이고 판독기가 카드 속을 **밴드 하나**(y 25.0 h 47.8)로 읽는데, "
     u"클론은 주인 지시 α .5 라 같은 자리를 밴드 13개로 쪼갠다. 즉 이 화면의 점수는 카드 치수가 아니라 **딤**을 "
     u"재고 있다. 카드가 맞는지는 «팝업 카드 가로 상자» 줄(딤과 무관한 자)로 보라 — 거기서 벗어나지 않으면 맞다.",
     ),
    (u"리그 상대 이름 — 원작은 흰 글자+키라인, 정본은 민글자 — 클론이 맞다. 같은 행 전투력 숫자의 키라인은 진짜 결함(T109)",
    u"리그 «상대 선택» 상대 이름: 원작 샷(`shot-042228`)의 이름은 **흰 글자 + 검정 키라인**인데 지금 정본 "
    u"`.league-challenge-name` 은 `color: var(--pp-ink)`(#17181a) 민글자다 — 클론의 어두운 민글자가 "
    u"**정본대로**다(T28 22회차 6배 확대 실측). 같은 행에서 **전투력 숫자의 검정 키라인은 정본에도 있다**"
    u"(`style.css` 2635 `-webkit-text-stroke: 2px`) — 그쪽은 진짜 결함이라 T109 로 뗐다."),
]


def print_stale_notes(full=False):
    """«낡은 원작 샷» 목록 — 회차마다 찍히므로 기본은 **한 줄씩**이다.

    여덟 항목의 본문을 매 회차 통째로 찍으니 출력이 벽이 돼 아무도 안 읽었다(T28 16회차에
    «다음 고칠 것» 29줄을 다섯 줄로 줄인 것과 같은 이유). 자세한 근거는 `--notes` 로 본다."""
    print(u"")
    print(u"· 원작 샷이 지금 정본과 다른 자리 %d개(클론 결함이 아니다 — 점수로 쫓지 마라%s):"
          % (len(STALE_REF_NOTES), u"" if full else u" · 자세히는 `--notes`"))
    for short, body in STALE_REF_NOTES:
        print(u"    – " + short)
        if full:
            print(u"        " + body)


# ── 회차 사이 점수 기준선 (T28 7회차 · 워커 M) ─────────────────────────────
# 문턱 0.5 의 근거(T28 22회차 실측 · 런 208 30장 × 흔들림 4가지 = 120회): UI 를 안 바꾸는
# 흔들림(밝기 ±1 · 세로/가로 1px 밀림)만 주면 |Δ| 중앙값 0.00 · 90분위 0.11 · **최대 0.49** 다
# — 즉 0.5 는 판독 잡음 바로 위다. 이 문턱 자체는 낮추지도 올리지도 않는다.
DROP_MARK = 0.5   # 이만큼 움직이면 사람이 봐야 한다(판독 잡음 실측: 90분위 0.11 · 최대 0.49)

# ── 한 런이 통째로 이상한가 (T28 38회차 · 워커 M) ──────────────────────────
# 런 341 에서 팝업 여섯 화면이 **카드 팝(.25s scale .7→1) 도중**에 찍혀 반투명으로 남았다
# (지문 «팝업 안» 차가 29~64 · 밴드 21→14). 한 화면이 그런 것은 게임 상태지만 **여러 화면이
# 한꺼번에** 그러면 촬영이 어긋난 것이다 — 그 런을 기준선으로 삼으면 다음 회차가 유령을 쫓는다.
FRAME_BIG_DIFF = 20.0   # 지문 «팝업 안» 차가 이보다 크면 «그 화면은 통째로 달라졌다»
FRAME_BIG_MIN = 3       # 그런 화면이 이만큼이면 런을 의심한다

# ── «내려간 화면» 을 회귀로 부르기 전에 (T28 22회차 · 워커 M) ────────────────
# 판독 잡음은 0.5 아래지만, **팝업 뒤 배경**(살아 있는 3D 세계 · 상단바 숫자 · 뒤 목록)은
# 회차마다 다르고 그것만으로 점수가 크게 움직인다. 실측(런 208 · 팝업 상자 바깥을 통째로
# 단색 30/90/150 으로 바꿔 재측정): 팝업 화면 20장 중 **19장이 0.5 이상**, 중앙값 ≈1.4.
# 아래 수는 그 **상한**이다(실제 회차 간 배경 변화는 이보다 작다) — 그래서 이 수보다 작은
# 하락은 «깬 사람» 이 아니라 먼저 **두 PNG 의 팝업 뒤**를 견주라고 따로 찍는다.
# 되돌리려면 PROGRESS 22회차 기록. 값은 손으로 고치지 말고 같은 실측을 다시 해서 고친다.
BG_SHAKY = {
    "shop": 4.3, "settings": 3.3, "pet-upgrade": 3.1, "profile": 2.5, "player-info": 2.3,
    "forge-list": 2.2, "gear-detail": 1.7, "craft-compare": 1.7, "league": 1.7,
    "summon-rates": 1.5, "league-challenge": 1.5, "chat": 1.4, "forge-info": 1.1,
    "autoforge": 1.1, "offline": 1.1, "pass": 0.7, "forge-detail": 0.7,
    "autoforge-filter": 0.6, "league-rewards": 0.5,
}


def load_baseline(path):
    """지난 회차 점수. 없으면 빈 dict — 첫 회차에도 조용히 돈다."""
    if not path or not os.path.exists(path):
        return {}
    try:
        with open(path, encoding="utf-8") as f:
            raw = f.read()
    except OSError:
        return {}
    out = {}
    # 의존성 0 규칙(PIL·numpy 없음과 같은 이유로 json 도 표준 라이브러리만 쓴다 — json 은 표준이라 그대로 쓴다)
    try:
        import json
        d = json.loads(raw)
    except Exception:
        return {}
    for k, v in (d.get("screens") or {}).items():
        try:
            out[k] = float(v)
        except (TypeError, ValueError):
            pass
    if d.get("avg") is not None:
        try:
            out["_avg"] = float(d["avg"])
        except (TypeError, ValueError):
            pass
    if d.get("run") is not None:
        out["_run"] = d["run"]
    hist = []
    for h in (d.get("history") or []):
        try:
            hist.append((h.get("run"), dict((k, float(v)) for k, v in (h.get("screens") or {}).items())))
        except (TypeError, ValueError):
            pass
    out["_hist"] = hist
    fps = {}
    for k, v in (d.get("fingerprints") or {}).items():
        u_ = fp_unpack(v)
        if u_:
            fps[k] = u_
    out["_fp"] = fps
    bd = {}
    for k, v in (d.get("bands") or {}).items():
        try:
            bd[k] = int(v)
        except (TypeError, ValueError):
            pass
    out["_bands"] = bd
    return out


def median_of(hist, name):
    """자취에서 그 화면의 중앙값 — 한 회차가 튄 것(배경이 조용했던 런)에 속지 않는다."""
    vs = sorted(h[name] for _, h in hist if name in h)
    if not vs:
        return None
    n = len(vs)
    return vs[n // 2] if n % 2 else (vs[n // 2 - 1] + vs[n // 2]) / 2.0


HIST_KEEP = 6     # 기준선 파일이 들고 있는 회차 수(중앙값용 · 파일이 커지지 않게)

# ── 그림 지문 (T28 23회차 · 워커 M) ────────────────────────────────────────
# `screens` 는 force_orphan 이라 **지난 런 PNG 가 안 남는다**. 그래서 화면마다 굵은 격자
# 밝기(FP_ROWS×FP_COLS)를 기준선 파일에 같이 적어 둔다 — 다음 회차가 «점수가 내려갔다» 를
# 만났을 때 «그림이 어디서 달라졌나»(팝업 안 ↔ 팝업 뒤)를 수로 말할 수 있다.
# 실측(런 208 ↔ 214 두 벌을 직접 견줌): 30장 중 20장이 **픽셀 차 0.0**(촬영은 거의 결정적이다) ·
# 움직인 넷은 전부 **팝업 안**이 달라졌다(`player-info` 안 5.0 ↔ 뒤 0.6 · `league` 16.8 ↔ 0.0 ·
# `chat` 10.5 ↔ 9.2 · `league-challenge` 2.8 ↔ 0.0) — 장비 등급 색·수 자릿수 같은 **게임 상태**다.
FP_ROWS, FP_COLS = 16, 8
FP_BOX = (0.06, 0.09, 0.94, 0.86)   # 팝업이 차지하는 상자(안 ↔ 뒤 를 가르는 금)
FP_SAME = 1.0                        # 이보다 작으면 «그 자리는 안 달라졌다»


def fingerprint(img):
    """굵은 격자 밝기 — 회차 사이 «어디가 달라졌나» 를 재는 데만 쓴다(판독에는 안 쓴다)."""
    g = img.gray()
    W, H = img.w, img.h
    out = []
    for r in range(FP_ROWS):
        y0, y1 = H * r // FP_ROWS, H * (r + 1) // FP_ROWS
        for c in range(FP_COLS):
            x0, x1 = W * c // FP_COLS, W * (c + 1) // FP_COLS
            n = 0
            tot = 0
            for y in range(y0, y1, 2):
                base = y * W
                for x in range(x0, x1, 2):
                    tot += g[base + x]
                    n += 1
            out.append(int(round(tot / float(n or 1))))
    return out


def fp_pack(fp):
    """지문(0~255 칸)을 한 줄 base64 로 — 회차마다 3,840줄짜리 숫자 덩어리가 diff 를 덮지 않게."""
    import base64
    return base64.b64encode(bytes(bytearray(min(255, max(0, int(v))) for v in fp))).decode("ascii")


def fp_unpack(v):
    """base64 한 줄 → 칸 목록. 옛 꼴(숫자 목록)도 그대로 읽는다."""
    if isinstance(v, list):
        try:
            return [int(x) for x in v]
        except (TypeError, ValueError):
            return None
    if not isinstance(v, str):
        return None
    import base64
    try:
        return list(bytearray(base64.b64decode(v.encode("ascii"))))
    except Exception:
        return None


def fp_diff(a, b):
    """두 지문의 차 — (팝업 안 평균, 팝업 뒤 평균). 길이가 다르면 (None, None)."""
    if not a or not b or len(a) != len(b) or len(a) != FP_ROWS * FP_COLS:
        return None, None
    # 칸을 «가운데가 상자 안인가» 로 가르면 가로 8칸에서는 **모든 칸이 안**이 된다(칸 가운데 6.25~93.75%가
    # 상자 6~94% 안에 다 든다) — 그러면 «뒤» 가 상단바·탭바만 재게 된다(T28 26회차에 들켰다).
    # 그래서 칸이 상자와 **겹치는 넓이 비**로 안/뒤에 나눠 싣는다.
    din = dout = 0.0
    win = wout = 0.0
    for r in range(FP_ROWS):
        y0, y1 = float(r) / FP_ROWS, float(r + 1) / FP_ROWS
        oy = max(0.0, min(y1, FP_BOX[3]) - max(y0, FP_BOX[1])) / (y1 - y0)
        for c in range(FP_COLS):
            x0, x1 = float(c) / FP_COLS, float(c + 1) / FP_COLS
            ox = max(0.0, min(x1, FP_BOX[2]) - max(x0, FP_BOX[0])) / (x1 - x0)
            f = ox * oy                                   # 이 칸이 상자와 겹치는 비(0~1)
            d = abs(a[r * FP_COLS + c] - b[r * FP_COLS + c])
            din += d * f
            win += f
            dout += d * (1.0 - f)
            wout += (1.0 - f)
    return din / (win or 1.0), dout / (wout or 1.0)


def save_baseline_file(path, scores, avg, run=None, fps=None, bands=None, carried=False):
    import json
    cur = dict((n, round(v, 1)) for n, v in scores)
    hist = []
    if path and os.path.exists(path):
        try:
            with open(path, encoding="utf-8") as f:
                old = json.loads(f.read())
            hist = list(old.get("history") or [])
            if not hist and old.get("screens"):      # 자취가 없던 옛 파일 — 그 한 회차부터 이어 붙인다
                hist = [{"run": old.get("run"), "avg": old.get("avg"), "screens": old["screens"]}]
        except (OSError, ValueError):
            hist = []
    hist = [h for h in hist if h.get("run") != run]
    ent = {"run": run, "avg": round(avg, 2), "screens": cur}
    if carried:
        # 이 런은 제 PNG 를 안 냈다 — 그림은 지난 런 것이다(T28 44회차). 자취에 그대로 남긴다.
        ent["carried"] = True
    hist.append(ent)
    d = {"run": run, "avg": round(avg, 2), "screens": cur, "history": hist[-HIST_KEEP:]}
    if carried:
        d["carried"] = True
    if fps:
        d["fingerprints"] = dict((k, fp_pack(v)) for k, v in fps.items())
    if bands:
        d["bands"] = dict((k, int(v)) for k, v in bands.items())
    with open(path, "w", encoding="utf-8") as f:
        f.write(json.dumps(d, ensure_ascii=False, indent=1, sort_keys=True) + "\n")


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


# ── 보스 경고 연출을 물고 찍힌 화면 (T28 44회차 · 워커 M) ────────────────
# 정본 `style.css` 337~400: `#boss-warning` 은 **전투 씬(#game-area) 안**의 풀스크린 연출이고
# z-index 16 이라 **팝업(20/22/40/60) 아래**에 깔린다 — 그래서 팝업 화면을 찍어도 씬 대역이
# 통째로 붉게 나온다. 배너 바탕이 `linear-gradient(180deg,#1e0202,#5a0707 45%,#240303)` 이라
# 그 붉음은 **어두운 순색 적색**(#5a0707 = 90,7,7)이다.
# 실측(런 403·413 · `screen_gear-detail.png`): y 6~55% 의 가로줄이 **한 색으로 꽉 찬 (90,14,11)** 이고
# 같은 런의 이웃 `craft-compare` 는 멀쩡한 딤 세계였다.
# ⚠ **우연이 아니다**(44회차 정정): 런 403(이어받은 그림)과 런 413(제 그림 60장)은 바이트가 다른 PNG 인데
# 덮인 비가 **둘 다 84%** 이고 점수도 둘 다 3.2 다 — 촬영이 **매 런 같은 자리**에서 연출을 문다(T176).
# 그래도 **UI 결함은 아니다**: 정본 z-index 16 이라 팝업 아래에 깔리는 것 자체는 정본대로다.
# 회귀로 부르지 않고, 촬영을 고칠 일로 돌린다(T128 ⓒ 카드 팝과 같은 갈래).
BW_Y0, BW_Y1 = 0.06, 0.55   # 전투 씬 대역(정본 `#game-area`)
BW_RED_MAX = 160            # #5a0707 계열 — 밝은 순적색(#ff1c1c 등 UI 색)은 뺀다
BW_RED_MIN = 40
BW_ROW_FRAC = 0.60          # 그 대역의 가로줄 중 이만큼이 «한 색 어두운 적색» 이면 연출이다


def bw_hit(img):
    """전투 씬 대역이 보스 경고(#boss-warning)의 붉은 판으로 덮였나 — (덮인 줄 비, 대표색)."""
    W, H = img.w, img.h
    px = img.px
    y0, y1 = int(H * BW_Y0), int(H * BW_Y1)
    hit, tot, rep = 0, 0, None
    for y in range(y0, y1, 2):
        tot += 1
        i0 = (y * W) * 3
        r, g, b = px[i0], px[i0 + 1], px[i0 + 2]
        if not (BW_RED_MIN <= r <= BW_RED_MAX and r >= 3 * max(g, b)):
            continue
        same = True
        for x in range(0, W, 4):
            i = (y * W + x) * 3
            if abs(px[i] - r) > 6 or abs(px[i + 1] - g) > 6 or abs(px[i + 2] - b) > 6:
                same = False
                break
        if same:
            hit += 1
            if rep is None:
                rep = (r, g, b)
    return (hit / float(tot) if tot else 0.0), rep


# ── 팝업 카드 상자 (T28 46회차 · 워커 M) ────────────────────────────────
# 밴드 점수는 팝업 화면에서 **딤을 재고 있다**: 원작 샷은 딤 α .988 이라 카드 바깥이 순검정이고
# 판독기가 카드를 **밴드 하나**로 읽는데(`forge-detail` 원작 = 밴드 8개), 클론은 주인 지시 α .5 라
# 뒤 패널이 비쳐 같은 자리가 밴드 20개로 쪼개진다. 그래서 **카드를 정본 치수로 고쳐도 점수는 내려간다**
# (실측: T177 이 카드 폭 73.7 → 67.8%W, 높이 53.5 → 48.3%H 로 정본 실측(68.9 · 47.56)에 넣었는데
#  `forge-detail` 은 2.2 → 1.1 로 떨어졌다 · 런 413 → 425).
# 그 고침을 **점수로 되돌리지 않게** 딤과 무관한 자를 따로 둔다: 밝은 판(카드)의 **가로 상자**(x·w)를
# 원작·클론 같은 규칙으로 재서 견준다. 검산: 원작 `shot-042931` 이 x 15.9 · w **68.5** 로 나오는데
# 정본 `style.css` 3649 의 `#forge-item-modal … { width: 68.9% }` 그 값이다.
# 🚨 **세로(y·h)는 안 낸다** — 카드 안의 어두운 띠(색 막대·아이콘 줄)에서 «카드 줄» 이 끊기는 자리가
# 원작과 클론에서 다르다. 실측(46회차): `player-info` 는 잇는 틈(gap)을 4.5% → 9% 로 늘리면 클론만
# y 54.2 → 14.8 로 붙고 원작은 37.8 그대로다(원작의 어두운 띠가 더 두껍다) — 어떤 틈을 골라도 두 그림이
# 같은 규칙으로 안 잘린다. 가로는 그 영향을 안 받는다(`player-info` 는 원작·클론 둘 다 w 75.2 로 흔들림 0).
CARD_THR = 195      # 카드 종이(원작 204·252 · 클론 240 안팎)와 딤(원작 0 · 클론 48~120)을 가른다
CARD_FRAC = 0.35    # 그 줄의 밝은 픽셀이 가로의 이만큼이면 «카드 줄»
CARD_GAP = 0.045    # 카드 안의 어두운 줄(글자·아이콘)이 이만큼까지 벌어져도 한 카드로 잇는다
CARD_TOL = 3.0      # 견줌 허용치(%%p) — 밴드 점수와 같은 눈금


def card_box(img):
    """그림에서 가장 큰 «밝은 판»(팝업 카드)의 **가로** 상자 — (x, w) %% · 못 찾으면 None."""
    px, W, H = img.px, img.w, img.h
    cnt = []
    for y in range(H):
        n, base = 0, y * W * 3
        for x in range(0, W, 2):
            i = base + x * 3
            if px[i] >= CARD_THR and px[i + 1] >= CARD_THR and px[i + 2] >= CARD_THR:
                n += 1
        cnt.append(n)
    need = (W // 2) * CARD_FRAC
    rows = [y for y in range(H) if cnt[y] >= need]
    if not rows:
        return None
    g = int(H * CARD_GAP)
    runs, st, prev = [], rows[0], rows[0]
    for y in rows[1:]:
        if y - prev > g:
            runs.append((st, prev))
            st = y
        prev = y
    runs.append((st, prev))
    y0, y1 = max(runs, key=lambda r: r[1] - r[0])
    ym = max(range(y0, y1 + 1), key=lambda y: cnt[y])
    xs = [x for x in range(W)
          if px[(ym * W + x) * 3] >= CARD_THR and px[(ym * W + x) * 3 + 1] >= CARD_THR
          and px[(ym * W + x) * 3 + 2] >= CARD_THR]
    if not xs:
        return None
    return (xs[0] * 100.0 / W, (xs[-1] - xs[0] + 1) * 100.0 / W)


def score(table_path, shots_dir, only=None, baseline_path=BASELINE, save_baseline=False, notes_full=False):
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
    bw = []
    cards = []
    refs = dict((n, r) for n, r in pairs() if r)
    meta, carried = {}, False
    mp0 = os.path.join(shots_dir, "meta.json")
    if os.path.exists(mp0):
        try:
            import json as _json
            with open(mp0, encoding="utf-8") as _f:
                meta = _json.loads(_f.read()) or {}
        except (OSError, ValueError):
            meta = {}
    # ── 이어받은 그림 (T28 44회차 · 워커 M) ─────────────────────────────
    # `ci.yml` 은 이 런이 PNG 를 한 장도 못 내면(PlayMode 가 라이선스·크래시로 안 돌면) **지난 screens 의
    # PNG 를 그대로 이어받아** 올린다(`shots:0 · carried:N`). 그런데 meta.json 은 그 그림이 **어느 런 것인지**
    # 안 적는다 — 그대로 기준선에 «런 403» 이라 적으면 자취가 거짓말을 한다(실측 2026-09-14 런 403).
    if meta.get("shots") == 0 and (meta.get("carried") or 0) > 0:
        carried = True
        print(u"  ⚠ 런 %s 는 제 PNG 를 한 장도 안 냈다 — 이 그림은 지난 런에서 **이어받은 %s장**이다"
              u"(meta.json 이 어느 런 것인지 안 적는다)."
              % (meta.get("run", "?"), meta.get("carried")))
        print(u"    점수는 멀쩡하다(그림은 진짜다) — 다만 **런 번호를 이 그림에 붙이지 마라**.")
    fps, bands = {}, {}
    suspect = [False]
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
                  u" — 촬영 프레임이 눌렸거나 화면 한쪽이 통째로 비었다(세계가 안 그려짐 등)"
                  % (name, fh, fw, FILL_H_MIN, FILL_W_MIN))
            unfilled.append(name)
        frac, rep = bw_hit(img)
        if frac >= BW_ROW_FRAC:
            bw.append((name, frac, rep))
        rf = refs.get(name)
        if rf:
            rp = os.path.join(REF_DIR, rf)
            if os.path.exists(rp):
                try:
                    a_box, b_box = card_box(png_read(rp)), card_box(img)
                except (OSError, ValueError):
                    a_box = b_box = None
                if a_box and b_box:
                    cards.append((name, a_box, b_box))
        got = read_layout(img, name)
        s, why = score_screen(ent["rects"], got)
        scores.append((name, s))
        fps[name] = fingerprint(img)
        # 밴드를 몇 개로 쪼갰나 — 회차 사이에 이 수가 달라지면 «요소가 어긋났다» 가 아니라
        # **자가 화면을 다르게 쪼갠 것**이다(T28 24·27회차 실측: 잉크가 한 겹 두꺼워지면 밴드가 붙는다).
        bands[name] = sum(1 for r in got if u"블록" not in r.name)
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
    if bw:
        print(u"  · ⚠ 보스 경고 연출(#boss-warning)을 물고 찍힌 화면 %d개: %s"
              % (len(bw), " ".join(u"%s %.0f%%%s" % (n, f * 100, u"" if c is None else u" %s" % (c,)) for n, f, c in bw)))
        print(u"    정본 `style.css` 337~400 그대로 연출은 씬 안(z 16)이라 팝업 아래에 깔린다 —"
              u" 팝업 화면을 찍어도 씬 대역이 통째로 붉다."
              u" **UI 결함이 아니라 촬영이 매 런 같은 자리에서 연출을 무는 것이다**(런 403·413 둘 다 84% · T176)"
              u" — 화면을 재등재하지 말고 촬영을 고친다.")
        print(u"    이 런의 점수는 «UI 가 그만큼 망가졌다» 가 아니다 — **촬영이 어긋난 것**이라"
              u" 화면마다 재등재하지 말고 촬영을 먼저 고친다(T27·T54 갈래).")
    if cards:
        off = [(n, a, b) for n, a, b in cards
               if max(abs(b[0] - a[0]), abs(b[1] - a[1])) > CARD_TOL]
        print(u"  · 팝업 카드 **가로** 상자(딤과 무관한 자 · 원작 ↔ 클론 · ±%.0f%%p): 잰 화면 %d개 · 벗어난 화면 %d개"
              % (CARD_TOL, len(cards), len(off)))
        for n, a, b in sorted(off, key=lambda t: -max(abs(t[2][i] - t[1][i]) for i in range(2))):
            print(u"    ✗ %-18s 원작 x%.1f w%.1f → 클론 x%.1f w%.1f  (Δ x%+.1f w%+.1f)"
                  % (n, a[0], a[1], b[0], b[1], b[0] - a[0], b[1] - a[1]))
        wide = [n for n, ai, bi in off if bi[1] - ai[1] > 5.0]
        if wide:
            print(u"    ⚠ %s 는 클론 상자가 원작보다 5%%p 넘게 **넓다** — 딤 α .5 로 비치는 **뒤 시트**를"
                  u" 카드로 잡았을 공산이 크다(원작은 α .988 이라 그 시트가 안 보인다). 등재 전에 PNG 를 열어라."
                  % " ".join(wide))
        if off:
            print(u"    이 자는 딤을 안 본다 — 밴드 점수가 내려가도 이 Δ 가 0 에 가까워졌으면 **고침이 맞다**"
                  u"(T177 실측: 카드를 정본 치수에 넣었더니 점수는 2.2 → 1.1 로 떨어졌다).")
    if not scores:
        print(u"✗ 점수를 낸 화면이 0개다 — 클론 샷(`screen_*.png`)이 하나도 없다")
        return 2
    avg = sum(s for _, s in scores) / len(scores)
    print(u"─" * 60)
    print(u"평균 %.2f / 10 · 화면 %d개 · %s점 미만 %d개" % (avg, len(scores), PASS_MARK, len(bad)))
    # ── 지난 회차와 대조(T28 7회차 · 워커 M): «평균이 4.23 → 1.72» 같은 회귀를 회차마다 손으로 세지 않는다.
    base = load_baseline(baseline_path)
    if base:
        cur = dict(scores)
        drops = sorted(((n, base[n], cur[n]) for n in cur if n in base and cur[n] - base[n] <= -DROP_MARK),
                       key=lambda t: t[2] - t[1])
        ups = [n for n in cur if n in base and cur[n] - base[n] >= DROP_MARK]
        # ── 화면 집합이 바뀐 회차 (T185 · 검수 Q 등재 · T28 47회차가 고쳤다) ──────────
        # 촬영은 언제든 중간에 설 수 있다(런 432: `UiShotsTests` 가 빨강이라 두 장이 안 찍혔다).
        # 그때 빠진 화면이 평균보다 **낮은** 것들이면 남은 화면의 평균은 저절로 오르고,
        # 옛 코드는 그것을 «+0.11 올랐다» 로 찍었다 — 나아진 것이 아니라 **분모가 바뀐 것**이다.
        # 그래서 ⓐ 빠진·새로 생긴 화면을 먼저 말하고 ⓑ 평균은 **공통 집합**으로 견준다.
        names = set(n for n in base if not n.startswith("_"))
        gone = sorted(names - set(cur))
        fresh = sorted(set(cur) - names)
        both = sorted(set(cur) & names)
        if gone:
            print(u"⚠ 촬영에서 빠진 화면 %d개 — 이번 런에 없다(점수가 아니라 **촬영**이 선 것이다): %s"
                  % (len(gone), " · ".join(u"%s(지난 %.1f)" % (n, base[n]) for n in gone)))
        if fresh:
            print(u"· 새로 생긴 화면 %d개: %s" % (len(fresh), " ".join(fresh)))
        if base.get("_avg") is not None:
            if (gone or fresh) and both:
                ob = sum(base[n] for n in both) / len(both)
                nb = sum(cur[n] for n in both) / len(both)
                print(u"공통 %d장 기준 평균 %.2f → %.2f (%+.2f) — 화면 수가 %d장 → %d장 으로 바뀌어 "
                      u"**전체 평균끼리는 견주지 않는다**"
                      % (len(both), ob, nb, nb - ob, len(names), len(cur)))
                print(u"  (참고 · 분모가 다른 두 수: 지난 회차(런 %s) %d장 평균 %.2f · 이번 %d장 평균 %.2f)"
                      % (base.get("_run", "?"), len(names), base["_avg"], len(cur), avg))
            else:
                print(u"지난 회차(런 %s) 평균 %.2f → 이번 %.2f (%+.2f)"
                      % (base.get("_run", "?"), base["_avg"], avg, avg - base["_avg"]))
        hist = base.get("_hist") or []
        obase = base.get("_fp") or {}
        oband = base.get("_bands") or {}

        def band_note(n):
            a, b = oband.get(n), bands.get(n)
            return u"" if a is None or b is None or a == b else u" · 밴드 %d → %d(자가 다르게 쪼갰다)" % (a, b)

        hard, soft = [], []
        for t in drops:
            n = t[0]
            din, dout = fp_diff(obase.get(n), fps.get(n))
            med = median_of(hist, n) if len(hist) >= 3 else None
            bwset = dict((t[0], t[1]) for t in bw)
            # 자취가 3회차 이상이면 «지난 회차» 가 아니라 **중앙값**과도 견준다 — 지난 회차 하나가
            # 튄 것을 «회귀» 로 부르지 않는다(T28 22회차 · forge-detail 1.9 가 그 꼴이었다).
            if n in bwset:
                # 씬 대역이 통째로 붉으면 밴드가 녹아 붙는다 — 그림이 달라진 것은 맞지만 **UI 가 아니다**.
                soft.append((n, t[1], t[2], u"보스 경고 연출을 물고 찍혔다(씬 대역 %.0f%%) — 촬영이 물었다(T176)"
                             % (bwset[n] * 100), din, dout))
            elif med is not None and t[2] > med - DROP_MARK:
                soft.append((n, t[1], t[2], u"최근 %d회차 중앙값 %.1f 자리다" % (len(hist), med), din, dout))
            elif din is not None and din < FP_SAME and dout < FP_SAME:
                # 그림은 사실상 같은데 점수만 움직였다 = 밴드 경계 하나가 걸린 것(자의 흔들림).
                soft.append((n, t[1], t[2], u"그림이 거의 같다(안 %.1f · 뒤 %.1f) — 밴드 경계가 걸린 자의 흔들림이다" % (din, dout), din, dout))
            elif din is not None and din < FP_SAME and dout > din:
                # 지문이 «팝업 안은 그대로 · 뒤만 달라졌다» 고 말하면 코드가 아닐 공산이 크다.
                soft.append((n, t[1], t[2], u"그림 안쪽은 그대로다(안 %.1f · 뒤 %.1f)" % (din, dout), din, dout))
            elif din is None and abs(t[2] - t[1]) <= BG_SHAKY.get(n, 0.0):
                # 지문이 없는 첫 회차에만 쓰는 물러섬(실측 상한 표)
                soft.append((n, t[1], t[2], u"배경만으로도 ±%.1f 움직이는 화면(지문 없음)" % BG_SHAKY.get(n, 0.0), None, None))
            else:
                hard.append((n, t[1], t[2], din, dout))
        if hard:
            print(u"⚠ 내려간 화면 %d개 — 그림이 실제로 달라졌다(«깬 사람» 은 이 수를 보고 찾는다):" % len(hard))
            for n, b, c, din, dout in hard:
                tail = u"" if din is None else (
                    u" · 그림 차: 팝업 안 %.1f · 뒤 %.1f → %s" % (
                        din, dout,
                        u"**안쪽**이 달라졌다(게임 상태 — 장비 등급 색·수 자릿수 — 인지 코드인지 PNG 로 가른다)"
                        if din >= dout else u"**뒤**가 달라졌다"))
                print(u"    %-18s %.1f → %.1f (%+.1f)%s%s" % (n, b, c, c - b, tail, band_note(n)))
        if soft:
            print(u"· 내려갔지만 아직 회귀가 아닌 화면 %d개:" % len(soft))
            for n, b, c, why, din, dout in soft:
                print(u"    %-18s %.1f → %.1f (%+.1f · %s)%s" % (n, b, c, c - b, why, band_note(n)))
        if ups:
            print(u"· 올라간 화면 %d개: %s" % (len(ups), " ".join(sorted(ups))))
        # 여러 화면이 한꺼번에 «안쪽이 통째로 달라졌다» 면 촬영이 어긋난 런이다.
        big = []
        for n in sorted(cur):
            din, _ = fp_diff(obase.get(n), fps.get(n))
            if din is not None and din > FRAME_BIG_DIFF:
                big.append((n, din))
        if len(big) >= FRAME_BIG_MIN:
            suspect[0] = True
            print(u"⚠ 이 런의 촬영이 통째로 어긋났을 수 있다 — «팝업 안» 이 %.0f 넘게 달라진 화면이 %d개다: %s"
                  % (FRAME_BIG_DIFF, len(big), " ".join(u"%s %.0f" % t for t in big)))
            print(u"    (실측 전례: 런 341 은 카드 팝 .25s 도중에 찍혀 팝업이 반투명이었다 — T128 ⓒ)")
            print(u"    → PNG 를 한 장 열어 보고, 어긋난 런이면 **기준선을 갱신하지 마라**.")
        if not drops and not ups:
            print(u"· 지난 회차와 견줘 %.1f점 넘게 움직인 화면 없음" % DROP_MARK)
    print_stale_notes(notes_full)
    if save_baseline and suspect[0]:
        print(u"· 기준선을 **안 적었다** — 위 «촬영이 어긋났을 수 있다» 경고 때문이다."
              u" 멀쩡한 런에서 다시 `--save-baseline` 하면 된다(결정 195·199 의 규칙 그대로).")
        save_baseline = False
    if save_baseline:
        # 런 번호는 CI 가 screens 에 같이 올린 meta.json 에서 읽는다(없으면 비운다).
        run = meta.get("run")
        save_baseline_file(baseline_path, scores, avg, run, fps, bands, carried)
        print(u"· 기준선을 %s 에 적었다(다음 회차가 이것과 견준다)" % os.path.relpath(baseline_path, REPO))
    if bad:
        # T28 16회차(워커 M): 29개를 줄줄이 찍으면 아무도 안 읽는다 — **낮은 것 다섯**만 점수와 함께 준다.
        low = sorted(((n, v) for n, v in scores if v < PASS_MARK), key=lambda t: t[1])[:5]
        print(u"«다음 볼 화면»(ROUTINE §2 T28 · 낮은 것부터 · 원작 PNG 와 나란히 보고 정본 코드로 확인한 뒤 등재):")
        for n, v in low:
            print(u"    %-18s %4.1f" % (n, v))
        if len(bad) > len(low):
            print(u"    (%s점 미만 %d개 중 다섯만 적었다 — 나머지는 이 다섯이 닫힌 뒤)" % (PASS_MARK, len(bad)))
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

    # ⑱ 기준선 왕복 + 회귀 탐지 문턱
    tmpb = os.path.join(REPO, "tools", ".ui_score_baseline_test.json")
    save_baseline_file(tmpb, [("main", 4.6), ("shop", 6.1)], 5.35, run=118)
    b = load_baseline(tmpb)
    os.remove(tmpb)
    chk(abs(b.get("main", 0) - 4.6) < 1e-9 and abs(b.get("_avg", 0) - 5.35) < 1e-9 and b.get("_run") == 118,
        u"기준선 쓰기 → 읽기 왕복(화면 점수 · 평균 · 런 번호)")
    chk(load_baseline(os.path.join(REPO, "tools", ".없는파일.json")) == {},
        u"기준선 파일이 없으면 빈 것으로 조용히 지나간다(첫 회차)")

    chk(len(STALE_REF_NOTES) >= 2
        and all(len(t) == 2 and t[0] and u"정본" in t[1] for t in STALE_REF_NOTES),
        u"«원작 샷이 지금 정본과 다른 자리» 주석 %d개가 (한 줄, 자세히) 꼴로 살아 있다" % len(STALE_REF_NOTES))

    # ⑲ 뒤 배경 폭: 실측한 화면은 그 폭 안의 하락을 «회귀» 로 부르지 않는다
    chk(all(v >= DROP_MARK for v in BG_SHAKY.values()) and len(BG_SHAKY) >= 15,
        u"«뒤 배경» 폭 표가 살아 있고 모두 문턱 %.1f 이상이다 (%d화면)" % (DROP_MARK, len(BG_SHAKY)))
    chk(BG_SHAKY.get("forge-detail", 0) >= 0.7 > 0.5,
        u"여섯 회차째 1.1 인 forge-detail 은 배경만으로 ±0.7 — 0.8 하락을 바로 «깬 사람» 으로 몰지 않는다")

    # ⑳ 기준선 자취: 회차를 이어 붙이고 중앙값을 낸다(한 회차가 튄 것에 안 속는다)
    tmph = os.path.join(REPO, "tools", ".ui_score_hist_test.json")
    if os.path.exists(tmph):
        os.remove(tmph)
    save_baseline_file(tmph, [("forge-detail", 1.1)], 1.1, run=201)
    save_baseline_file(tmph, [("forge-detail", 1.2)], 1.2, run=203)
    save_baseline_file(tmph, [("forge-detail", 1.9)], 1.9, run=205)
    bh = load_baseline(tmph)
    os.remove(tmph)
    chk(len(bh.get("_hist") or []) == 3 and bh.get("_run") == 205,
        u"기준선이 회차 자취를 이어 붙인다 (%d회차)" % len(bh.get("_hist") or []))
    chk(abs((median_of(bh["_hist"], "forge-detail") or 0) - 1.2) < 1e-9,
        u"자취 중앙값은 1.2 다 — 튄 회차(1.9)가 아니라 (%s)"
        % median_of(bh["_hist"], "forge-detail"))

    # ㉑ 그림 지문: 같은 그림은 0 · 팝업 안만 바꾸면 «안» 이, 뒤만 바꾸면 «뒤» 가 커진다
    base_img = _canvas(80, 160, (120, 120, 120))
    _fill(base_img, 8, 20, 72, 140, (230, 230, 230))
    fa = fingerprint(base_img)
    chk(fp_diff(fa, fa) == (0.0, 0.0), u"같은 그림의 지문 차는 0 이다")
    inner = _canvas(80, 160, (120, 120, 120))
    _fill(inner, 8, 20, 72, 140, (230, 230, 230))
    _fill(inner, 20, 60, 60, 100, (0, 0, 0))          # 팝업 **안**만 바꾼다
    din, dout = fp_diff(fa, fingerprint(inner))
    chk(din > dout, u"팝업 안만 바뀌면 지문의 «안» 이 «뒤» 보다 크다 (안 %.1f · 뒤 %.1f)" % (din, dout))
    outer = _canvas(80, 160, (120, 120, 120))
    _fill(outer, 8, 20, 72, 140, (230, 230, 230))
    _fill(outer, 0, 150, 80, 160, (0, 0, 0))          # 팝업 **뒤**(아래 띠)만 바꾼다
    din2, dout2 = fp_diff(fa, fingerprint(outer))
    chk(dout2 > din2 and din2 < FP_SAME,
        u"뒤만 바뀌면 «뒤» 만 커지고 «안» 은 같음 문턱 아래다 (안 %.1f · 뒤 %.1f)" % (din2, dout2))

    side = _canvas(80, 160, (120, 120, 120))
    _fill(side, 8, 20, 72, 140, (230, 230, 230))
    _fill(side, 0, 40, 4, 120, (0, 0, 0))            # 왼쪽 **옆 여백**만 바꾼다(상자 밖)
    din3, dout3 = fp_diff(fa, fingerprint(side))
    chk(dout3 > din3, u"옆 여백만 바뀌어도 «뒤» 가 «안» 보다 크다 (안 %.1f · 뒤 %.1f)" % (din3, dout3))

    two = _canvas(60, 200, (250, 250, 250))
    _fill(two, 5, 20, 55, 40, (10, 10, 10))
    _fill(two, 5, 120, 55, 140, (10, 10, 10))
    nb2 = sum(1 for r in read_layout(two) if u"블록" not in r.name)
    three = _canvas(60, 200, (250, 250, 250))
    for y0 in (20, 80, 140):
        _fill(three, 5, y0, 55, y0 + 20, (10, 10, 10))
    nb3 = sum(1 for r in read_layout(three) if u"블록" not in r.name)
    chk(nb3 > nb2, u"띠가 셋인 그림은 둘인 그림보다 밴드가 많다 (%d ↔ %d) — 밴드 수는 셀 수 있다" % (nb2, nb3))

    chk(fp_unpack(fp_pack(fa)) == fa, u"지문 base64 왕복이 같다 (%d칸)" % len(fa))

    # ⑨ 보스 경고 연출 감지(T28 44회차) — 씬 대역이 통째로 #5a0707 이면 «촬영 타이밍» 이지 회귀가 아니다
    bwimg = _canvas(60, 200, (250, 250, 250))
    _fill(bwimg, 0, int(200 * 0.06), 60, int(200 * 0.55), (90, 7, 7))
    f_bw, rep = bw_hit(bwimg)
    chk(f_bw >= BW_ROW_FRAC, u"씬 대역이 #5a0707 로 덮이면 보스 경고로 잡는다 (%.2f)" % f_bw)
    chk(rep == (90, 7, 7), u"보스 경고 대표색을 그대로 돌려준다 (%s)" % (rep,))
    clean = _canvas(60, 200, (250, 250, 250))
    _fill(clean, 0, int(200 * 0.06), 60, int(200 * 0.30), (110, 180, 150))   # 멀쩡한 딤 세계
    chk(bw_hit(clean)[0] < BW_ROW_FRAC, u"붉지 않은 세계는 보스 경고로 안 잡는다")
    bright = _canvas(60, 200, (250, 250, 250))
    _fill(bright, 0, int(200 * 0.06), 60, int(200 * 0.55), (255, 28, 28))    # UI 의 ultimate 적색
    chk(bw_hit(bright)[0] < BW_ROW_FRAC, u"밝은 순적색(UI 등급색 #ff1c1c)은 연출로 안 잡는다")

    # ⑫ 화면 집합이 바뀐 회차(T185) — 낮은 화면이 빠지면 «전체 평균» 은 저절로 오른다
    b_scr = {"a": 5.0, "b": 5.0, "c": 3.0, "d": 3.5}         # 지난 회차 4장 · 평균 4.125
    c_scr = {"a": 5.0, "b": 5.0}                              # 이번 회차 2장 · 평균 5.00
    gone_ = sorted(set(b_scr) - set(c_scr))
    both_ = sorted(set(b_scr) & set(c_scr))
    ob_ = sum(b_scr[n] for n in both_) / len(both_)
    nb_ = sum(c_scr[n] for n in both_) / len(both_)
    whole = sum(c_scr.values()) / len(c_scr) - sum(b_scr.values()) / len(b_scr)
    chk(gone_ == ["c", "d"], u"빠진 화면을 이름으로 집어낸다 (%s)" % gone_)
    chk(whole > 0.5, u"낮은 둘이 빠지면 «전체 평균» 은 %+.2f 로 오른다 — 이 수를 «나아졌다» 로 읽으면 안 된다" % whole)
    chk(abs(nb_ - ob_) < 1e-9, u"공통 집합끼리 견주면 움직임이 0 이다 (%.2f → %.2f)" % (ob_, nb_))
    fresh_ = sorted(set({"a": 1.0, "e": 2.0}) - set(b_scr))
    chk(fresh_ == ["e"], u"새로 생긴 화면도 집어낸다 (%s)" % fresh_)

    # ⑪ 팝업 카드 가로 상자(T28 46회차) — 딤이 달라도 같은 값이 나와야 한다
    dimdark = _canvas(200, 300, (0, 0, 0))          # 원작 꼴: 딤 α .988
    _fill(dimdark, 30, 60, 170, 240, (250, 250, 250))
    dimlite = _canvas(200, 300, (120, 120, 120))    # 클론 꼴: 딤 α .5 (뒤 시트가 비친다)
    _fill(dimlite, 30, 60, 170, 240, (250, 250, 250))
    ba, bb = card_box(dimdark), card_box(dimlite)
    chk(ba is not None and abs(ba[0] - 15.0) < 1.0 and abs(ba[1] - 70.0) < 1.5,
        u"카드 가로 상자를 제대로 읽는다 (%s)" % (ba,))
    chk(bb == ba, u"딤 밝기가 달라도 카드 가로 상자는 같다 (%s ↔ %s)" % (ba, bb))
    barred = _canvas(200, 300, (0, 0, 0))           # 카드 안에 어두운 띠가 있어도 가로는 안 흔들린다
    _fill(barred, 30, 60, 170, 240, (250, 250, 250))
    _fill(barred, 32, 120, 168, 150, (20, 20, 20))
    bc = card_box(barred)
    chk(bc is not None and abs(bc[1] - ba[1]) < 0.6,
        u"카드 안 어두운 띠가 가로 상자를 안 흔든다 (%s ↔ %s)" % (ba, bc))
    chk(card_box(_canvas(200, 300, (0, 0, 0))) is None, u"밝은 판이 없으면 카드 상자는 없다")

    # ⑩ 이어받은 그림은 기준선 자취에 그렇게 적힌다(T28 44회차)
    import tempfile, json as _j, codecs
    fd, tmpb = tempfile.mkstemp(suffix=".json")
    os.close(fd)
    try:
        save_baseline_file(tmpb, [("a", 5.0)], 5.0, 403, None, None, True)
        got = _j.loads(codecs.open(tmpb, encoding="utf-8").read())
        chk(got.get("carried") is True and got["history"][-1].get("carried") is True,
            u"이어받은 런은 기준선과 자취 둘 다에 «carried» 로 남는다")
        save_baseline_file(tmpb, [("a", 5.0)], 5.0, 404, None, None, False)
        got2 = _j.loads(codecs.open(tmpb, encoding="utf-8").read())
        chk("carried" not in got2 and got2["history"][-1].get("carried") is None,
            u"제 그림을 낸 런에는 «carried» 를 안 적는다")
    finally:
        os.unlink(tmpb)

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
    ap.add_argument("--baseline", default=BASELINE, help="지난 회차 점수 파일(회귀 대조)")
    ap.add_argument("--save-baseline", action="store_true", help="이번 점수를 기준선으로 적는다")
    ap.add_argument("--notes", action="store_true", help="«낡은 원작 샷» 항목을 근거까지 펼쳐 찍는다")
    a = ap.parse_args()

    if a.self_test:
        return self_test()
    if a.read:
        for r in read_layout(png_read(a.read)):
            print(u"| %s | %.1f | %.1f | %.1f | %.1f | %s |" % (r.name, r.x, r.y, r.w, r.h, r.grid()))
        return 0
    if a.notes and not a.score:
        print_stale_notes(full=True)
        return 0
    if a.gen:
        return gen(a.ref_dir, a.table, a.only)
    if a.score:
        return score(a.table, a.shots, a.only, a.baseline, a.save_baseline, a.notes)
    ap.print_help()
    return 0


if __name__ == "__main__":
    sys.exit(main())
