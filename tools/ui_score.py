#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""T28 — 원작 화면 ↔ 클론 화면 비율 대조 자.

ROUTINE §2 T28: «화면마다 «요소 · x% · y% · w% · h%» 표를 원작 샷에서 5% 격자로 판독 →
우리 PNG 와 ±3%p 대조 → 점수. 8.0 미만이면 그 화면의 UI 작업을 «다음 고칠 것» 으로 재등재».

하는 일 셋:
  --gen    원작 샷(`.wwwww-src/web/ref/` 아래 · 보통 `screens/shot-*.png`)을 판독해 `docs/ref-layout.md` 판독표를 만든다.
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
# 짝 표의 «원작 파일» 칸은 **이 뿌리에서 잰 상대 경로**다(`screens/shot-042120.png` · `shots/ascend-entry-rows.png`).
# T393 3회차까지 이 값은 `ref/screens` 였고 칸에는 파일 이름만 들어가 **`ref/` 아래 다른 폴더를 못 가리켰다** —
# 정본이 승천 화면의 시트를 `ref/shots/` 에 갖고 있는데 이 레포가 한 번도 못 본 까닭이 그것이다.
REF_DIR = os.path.join(REPO, ".wwwww-src", "web", "ref")
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


# ─────────────────────────── 판독 원리 (T444 · 결정 778) ───────────────────────────
# 🚫 **값 문턱을 다 걷어냈다** — 옛 자(행 얼룩 MAD 6.0 · 골짜기 `med×k`·ε 0.5 · 바탕 ±12 · 블록 8%)는 화소를
#    ±1 만 밀어도 35장 중 24장이 조각 수를 바꿨고(T437 · `--jitter`), 여섯 자리를 하나씩 고쳐도 안 줄었다 —
#    문턱이 열댓 곳이고 그 대부분이 칼날 위였다(흔들림 0.5 ↔ 여유 0.5). 옛 코드는 git 이력(T437 6회차까지)에 있다.
# 지금 자는 셋으로 선다(T444 1회차 실측 35장 · 가장자리 25~35/35 · 다중 척도 17~27/35 ↔ 이것 3~4/35):
#   ① **건너뜀 수**(정수): 한 행에서 «EDGE_STEP 칸 떨어진 화소와 밝기가 EDGE_T 넘게 다른 자리» 의 수.
#      2칸 차인 까닭 — 체커판 ±1 은 한 칸 걸러 밀므로 이웃 차는 ±1 움직이지만 2칸 차는 같은 쪽끼리라 안 움직인다.
#      전체 ±1 은 포화(흰 255 · 검정 0 은 안 밀린다)에서만 흔들고 그것은 흔들기 자체의 한계다(1회차 `main`·`skill-detail`).
#   ② **그릇 수준**: 구간 안에서 FLOOR_RUN 행 이상 이어지는 값 중 최솟값 = 그 구간을 감싼 테두리 몫(목록 격자 ≈10).
#      최솟값을 쓰면 두 행짜리 우연한 0 이 그릇이 돼 아무것도 안 갈린다(1회차 실측 `forge-list` 14~84% 한 밴드).
#   ③ **상대 잣대**: 그릇보다 ROW_EXTRA(열은 COL_EXTRA) 많은 행·열만 «내용» — 문턱이 값이 아니라 «그릇 + 정수» 다.
# 굵기는 옛 자와 같게 뒀다: 밴드 틈 H/100 잇기 · BAND_MAX%%H 넘는 밴드만 재귀(깊이 3) · 블록 틈 W/50 잇기 · 블록이 둘 미만이면 줄 없음.
EDGE_STEP = 2      # 건너뜀을 재는 화소 간격(체커판 ±1 을 안 타는 최소 간격)
EDGE_T = 24        # 이만큼 넘게 다르면 «건너뜀»(0~255 · 글자·테두리 모서리는 100 안팎 · 그라디언트 한 칸은 0~3)
FLOOR_RUN = 3      # «그릇 수준» 은 이 행 수 이상 이어져야 그릇이다
ROW_EXTRA = 2      # 그릇보다 이만큼 많은 행이 내용이다(테두리 한 쌍 = 2)
COL_EXTRA = 2      # 열도 같다
BAND_MAX = 18      # 이보다 높은(%) 밴드는 그릇 수준으로 더 가른다


def _row_edges(img, y0, y1, x0, x1):
    """[y0,y1) 각 행의 «건너뜀 수» — EDGE_STEP 칸 떨어진 화소와 EDGE_T 넘게 다른 자리(정수)."""
    g, w, st, T = img.gray(), img.w, EDGE_STEP, EDGE_T
    out = []
    for y in range(y0, y1):
        base = y * w
        c = 0
        for x in range(x0 + st, x1):
            d = g[base + x] - g[base + x - st]
            if d >= T or d <= -T:
                c += 1
        out.append(c)
    return out


def _col_edges(img, y0, y1, x0, x1):
    """[x0,x1) 각 열의 «건너뜀 수» — 그 열에 걸린 가로 건너뜀(글자·테두리의 세로 모서리) + 세로 건너뜀(가로 모서리).

    세로 줄무늬처럼 한 열 안이 위아래로 한 값인 덩어리는 세로 건너뜀이 0 이라, 가로 건너뜀을 열에 얹어야 잡힌다."""
    g, w, st, T = img.gray(), img.w, EDGE_STEP, EDGE_T
    out = [0] * (x1 - x0)
    for y in range(y0, y1):
        base = y * w
        for x in range(x0 + st, x1):
            d = g[base + x] - g[base + x - st]
            if d >= T or d <= -T:
                out[x - x0] += 1
    for y in range(y0 + st, y1):
        base, up = y * w, (y - st) * w
        for x in range(x0, x1):
            d = g[base + x] - g[up + x]
            if d >= T or d <= -T:
                out[x - x0] += 1
    return out


def _floor(seg, run=FLOOR_RUN):
    """«그릇 수준» — run 칸 이상 이어지는 값 무리 중 가장 낮은 것(무리의 최댓값). 그런 무리가 없으면 최솟값."""
    best = None
    i, n = 0, len(seg)
    while i < n:
        j = i
        while j < n and seg[j] <= seg[i]:
            j += 1
        if j - i >= run:
            v = max(seg[i:j])
            if best is None or v < best:
                best = v
        i = j
    return best if best is not None else (min(seg) if seg else 0)


def _flags(flags, min_len, gap):
    """참인 자리의 연속 구간 — gap 이하로 떨어진 것은 잇고 min_len 미만은 버린다."""
    runs = _merge(_runs([1 if f else 0 for f in flags], 0, 1), gap)
    return [(a, b) for a, b in runs if b - a >= min_len]


def _split_rows(t, y0, y1, H, depth=0):
    """BAND_MAX%%H 를 넘는 밴드를 «그릇 + ROW_EXTRA» 넘는 행의 구간으로 가른다(틈은 잇지 않는다 — 1행 구분선에서도 갈린다)."""
    if y1 - y0 <= max(4, H * BAND_MAX // 100) or depth >= 3:
        return [(y0, y1)]
    seg = t[y0:y1]
    b = _floor(seg)
    subs = _flags([v >= b + ROW_EXTRA for v in seg], max(3, H // 100), 0)
    if len(subs) < 2:
        return [(y0, y1)]
    out = []
    for a, c in subs:
        out += _split_rows(t, y0 + a, y0 + c, H, depth + 1)
    return out


# ─────────────────────────── 판독기 자국 ───────────────────────────
# 🚨 **판독표(`docs/ref-layout.md`)는 이 자로 구운 것이다.** 판독기를 고치고 `--gen` 을 안 돌리면
#    원작 쪽은 **옛 자**, 클론 쪽은 **새 자**로 재게 된다 — `read_layout` 이 제 머리에 «원작과 클론에
#    같은 자를 쓴다» 고 적어 둔 것이 조용히 깨진다. T437 2·3회차가 실제로 그랬다(101·102회차 두 회차 ·
#    T28 103회차 실측: 다시 구우니 표가 1,366 → 1,452줄로 바뀌었다). 그래서 자국을 표 머리에 박고
#    자기 검사가 **막는다**.
STAMP_FUNCS = ("app_box", "_runs", "_merge", "_row_edges", "_col_edges", "_floor", "_flags",
               "_split_rows", "read_layout")
STAMP_CONSTS = ("EDGE_STEP", "EDGE_T", "FLOOR_RUN", "ROW_EXTRA", "COL_EXTRA", "BAND_MAX", "GRID")
_DOC_Q = ('"""', "'''")


def _strip_py(src):
    """주석·따옴표 세 개 덩어리를 걷어낸 코드 줄만 — 설명만 고쳤다고 자국이 바뀌면 안 된다."""
    out, quote = [], None
    for line in src.split("\n"):
        t = line.strip()
        if quote is not None:
            if quote in t:
                quote = None
            continue
        if not t or t.startswith("#"):
            continue
        for q in _DOC_Q:
            if t.startswith(q):
                if t.count(q) == 1:
                    quote = q
                t = ""
                break
        if t:
            out.append(" ".join(t.split()))
    return "\n".join(out)


def reader_stamp():
    """판독을 정하는 함수·상수의 **코드**만 뭉친 해시 12자리."""
    import hashlib
    import inspect
    parts = [_strip_py(inspect.getsource(globals()[n])) for n in STAMP_FUNCS]
    parts += ["%s=%r" % (n, globals()[n]) for n in STAMP_CONSTS]
    return hashlib.sha256(u"\n".join(parts).encode("utf-8")).hexdigest()[:12]


STAMP_RE = re.compile(r"판독기 자국:\s*`([0-9a-f]{12})`")


def table_stamp(path):
    """판독표 머리에 박힌 자국(없으면 None)."""
    if not os.path.exists(path):
        return None
    with open(path, encoding="utf-8") as f:
        head = f.read(4000)
    m = STAMP_RE.search(head)
    return m.group(1) if m else None


def read_layout(img, name="화면"):
    """그림 하나를 판독해 Rect 목록(밴드 + 밴드 안 블록)을 낸다 — 원작·클론에 같은 자를 쓴다.

    🚨 **앱 상자 기준이다**(T28 69회차). 원작 샷 30장은 앱 크기가 아니라 레터박스가 섞여 있어
    (세로/가로 1.6112~1.8238 · 9:16 인 것이 하나도 없다 · 68회차 전수) 그림 전체를 100%% 로 잡으면
    원작 쪽 세로 좌표가 최대 1.26%%p 치우친다. `app_box()` 로 앱만 떼어 그 안에서만 판독하고
    좌표도 앱 기준으로 낸다. 클론 샷은 540x960 = 정확히 9:16 이라 **아무것도 달라지지 않는다**.

    판독은 위 «판독 원리» 세 줄이 전부다(T444): 건너뜀이 있는 행의 연속 = 밴드(틈 H/100 잇기) ·
    BAND_MAX 를 넘는 밴드는 그릇 수준으로 재귀 · 밴드 안 열은 «그릇 + COL_EXTRA» 넘는 연속 = 블록.
    """
    ax, ay, W, H = app_box(img)
    t = _row_edges(img, ay, ay + H, ax, ax + W)      # 값은 앱 기준 0..H-1 로 돌아온다
    G = max(2, H // 100)
    bands = []
    for a, b in _flags([v >= 1 for v in t], G, G):
        bands += _split_rows(t, a, b, H)
    rects = []
    for bi, (y0, y1) in enumerate(bands):
        rects.append(Rect("밴드%d" % (bi + 1), 0.0, 100.0 * y0 / H, 100.0, 100.0 * (y1 - y0) / H))
        ce = _col_edges(img, ay + y0, ay + y1, ax, ax + W)
        blocks = _flags([v >= _floor(ce) + COL_EXTRA for v in ce], max(2, W // 100), max(3, W // 50))
        if len(blocks) < 2:
            continue                      # 밴드 전체가 한 덩어리면 블록 줄은 군더더기다
        for ci, (x0, x1) in enumerate(blocks):
            rects.append(Rect("밴드%d·블록%d" % (bi + 1, ci + 1),
                              100.0 * x0 / W, 100.0 * y0 / H,
                              100.0 * (x1 - x0) / W, 100.0 * (y1 - y0) / H))
    return rects


# ─────────────────────────── 짝 표 (원작 ↔ 클론) ───────────────────────────

def ref_rel(raw):
    """짝 표 «원작 파일» 칸 한 값 → `REF_DIR` 에서 잰 상대 경로(또는 None).

    숫자만이면 여태 쓰던 `screens/shot-<번호>.png` 다. 슬래시나 `.png` 가 들어 있으면
    **`ref/` 아래 아무 폴더나** 가리키는 것으로 본다(`shots/ascend-entry-rows.png` · T393 3회차)."""
    if not raw:
        return None
    raw = raw.strip().strip("/")
    if re.match(r"^\d+$", raw):
        return "screens/shot-%s.png" % raw
    if raw.endswith(".png"):
        return raw if "/" in raw else "screens/" + raw
    return None


def pairs():
    """[(화면 이름, 원작 파일 또는 None)] — 정본 `shot-screens.js` 의 SCREENS 순서 그대로.

    «원작 파일» 은 `REF_DIR`(= `.wwwww-src/web/ref`)에서 잰 **상대 경로**다.
    정본이 옆에 없으면 T27 의 `UiShotsTests.cs` 에서 같은 표를 읽는다(둘은 같아야 한다).
    정본에 있는 화면이 먼저 서고, **클론에만 있는 화면**(승천처럼 정본 `SCREENS` 에 줄이 없는 것)은
    그 뒤에 `UiShotsTests.cs` 순서대로 덧댄다 — 안 덧대면 그 화면은 촬영은 되는데 채점에서 통째로 빠진다."""
    out = []
    if os.path.exists(SHOTS_JS):
        src = open(SHOTS_JS, encoding="utf-8").read()
        i = src.find("const SCREENS = [")
        if i >= 0:
            for m in re.finditer(r"^\s*\['([a-z0-9-]+)',\s*(?:'([^']*)'|null)",
                                 src[i:], re.M):
                out.append((m.group(1), ref_rel(m.group(2))))
    cs = clone_pairs()
    if not out:
        return cs
    have = set(n for n, _ in out)
    for n, r in cs:
        if n not in have:
            out.append((n, r))
    return out


def clone_pairs():
    """T27 `UiShotsTests.cs` 의 촬영 목록 — [(이름, 원작 파일 또는 None)]."""
    if not os.path.exists(SHOTS_CS):
        return []
    src = open(SHOTS_CS, encoding="utf-8").read()
    out = []
    for m in re.finditer(r'Name = "([a-z0-9-]+)"(?:\s*,\s*Ref = (?:"([^"]*)"|null))?', src):
        out.append((m.group(1), ref_rel(m.group(2))))
    return out


# ─────────────────────────── 판독표 (읽기 · 쓰기) ───────────────────────────

HEAD = u"""# 원작 ↔ 클론 화면 비율 판독표 (T28)

> **자가 만든다 — 손으로 고치지 않는다.** `python3 tools/ui_score.py --gen` 이 정본 샷
> (`.wwwww-src/web/ref/` 아래 — `screens/shot-*.png` 가 대부분이고 `shots/*.png` 도 짝이 될 수 있다)을
> 판독해 이 파일을 다시 쓴다.
> 대조는 `python3 tools/ui_score.py --score` — 클론 PNG(`ui-screens/screen_<이름>.png` ·
> `screens` 브랜치)를 **같은 자**로 재서 이 표와 ±%(tol)s%%p 로 맞춰 보고 화면마다 10점 만점을 낸다.
> %(pass)s 점 미만인 화면은 그 화면의 UI 작업을 «다음 고칠 것» 으로 재등재한다(ROUTINE §2 T28).
>
> 좌표는 **앱 상자(9:16) 크기의 백분율**이다 — 원작 샷 30장은 앱 크기가 아니라 레터박스가 섞여 있어
> (세로/가로 1.6112~1.8238 · 9:16 인 것이 하나도 없다) 그림 전체를 100%% 로 잡으면 세로가 최대 1.26%%p
> 치우친다(T28 68·69회차 · `app_box()`). 클론 샷은 540x960 = 정확히 9:16 이라 달라지는 것이 없다. «격자» 칸은 사람 눈 검산용 5%% 격자 칸 번호
> (`가로 시작~끝,세로 시작~끝`) — 판독값 자체는 0.1%% 단위로 둔다(5%% 로 양자화하면
> 오차 2.5%%p 가 허용 오차 3%%p 를 먹는다).
>
> 판독 규칙(T444): 행의 «건너뜀 수»(2칸 떨어진 화소와 24 넘게 다른 자리의 수 · 정수)가 있는 연속 구간 = **밴드**
> (18%% 를 넘는 밴드는 «그릇 수준 + 2» 넘는 행으로 다시 가른다), 밴드 안에서 열의 건너뜀 수로 같은 규칙 = **블록**.
> 값 문턱이 없어 화소 ±1 에 조각 수가 안 흔들린다(옛 자 24/35 → 3~4/35). 원작과 클론에 같은 자를 쓴다.
>
> 판독기 자국: `%(stamp)s` — 이 표를 구운 자의 코드 해시다. 판독기를 고치면 이 값이 달라지고
> `--self-test` 가 **막는다**(T28 103회차). 그때는 `--gen` 으로 표를 다시 구워야 원작·클론이
> 같은 자로 재진다.
"""


def gen(ref_dir, out_path, only=None, quiet=False):
    rows = pairs()
    if not rows:
        print(u"✗ 짝 표를 못 찾았다 — 정본(.wwwww-src) 또는 UiShotsTests.cs 가 있어야 한다")
        return 1
    body = [HEAD % {"tol": TOL, "pass": PASS_MARK, "stamp": reader_stamp()}]
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


def ceil_median(past, cur):
    """천장은 **자취 중앙값**으로 읽는다(T28 87회차 · 결정 722).

    한 회차의 천장은 **밴드 쐅개짐이 바뀜면 통째로 흔들린다** — 런 859~898 전수에서
    31 화면 중 **11 이 0.5 넘게** 움직였고 최악은 `offline` **4.89 → 2.75** · `profile` **4.30 → 6.23** 이다.
    그 흔들림이 «다음 볼 화면» 의 맨 위를 뒤집어 놓으므로(87회차 실측) 자취가 셋 이상이면 중앙값을 쓴다.
    셋 미만이면 이번 회차 값을 그대로 둔다(새 화면은 자취가 없다).
    """
    vs = [v for v in (past or []) if v]
    if cur:
        vs = vs + [cur]
    if len(vs) < 3:
        return cur
    vs = sorted(vs)
    return vs[len(vs) // 2]


def score_ceiling(ref_rects, got_rects):
    """이 화면에서 **닿을 수 있는 최고점**(T28 79회차 · 결정 686).

    `score_screen` 의 분모에는 클론 배치와 **무관한** 두 덩어리가 들어 있다 —
    «짝 없음»(원작에만 있는 요소 · 4실점씩)과 «군더더기»(클론에만 있는 요소 · 1실점씩).
    둘 다 **원작 샷의 딤이 α .988 이라 카드 밖이 통짜 한 색**인 데서 주로 온다(정본 딤은 주인 지시 .5 라
    뒤 화면이 비쳐 자가 훨씬 잘게 쪼갠다 — `STALE_REF_NOTES` 둘째 항목). 그래서 짝지은 요소의
    x·y·w·h 가 **전부 맞아도** 그 실점은 남는다. 그 값이 이 함수다.

    🚫 되풀이하지 마라: 62~78회차는 «다음 볼 화면» 을 **절대 점수가 낮은 순**으로 골랐는데,
    그 줄의 맨 위(`craft-compare` 2.79 · `pet-upgrade` 2.03 · `forge-detail` 3.01)는
    천장 자체가 3.6~3.8 인 화면들이라 **이미 78·53·79% 를 채운** 자리였다. 진짜 뒤처진 자리는
    달성률(점수/천장)이 낮은 쪽이다(런 802 실측: `dungeon-detail` 3.28/6.25 = **52%** — 3점이 비어 있다).
    """
    pairs_, extra = match(ref_rects, got_rects)
    m = sum(1 for _r, g in pairs_ if g is not None)
    unmatched = len(pairs_) - m
    tot = m * 4 + unmatched * 4 + len(extra)
    return (10.0 * m * 4 / tot) if tot else 0.0


# ── 원작 샷이 «지금 정본» 과 다른 자리 (T28 14회차 · 워커 M) ─────────────
# `web/ref/screens/shot-*.png` 30장은 **찍힌 시점의 원작**이다. 그 뒤 주인 지시로 정본이 바뀐 자리가 있고,
# 그런 자리는 클론이 «정본대로» 여도 점수가 안 오른다 — 회차마다 그것을 결함으로 다시 진단하는 일을 막는다.
STALE_REF_NOTES = [
    (u'리그 도전 행 피치 — 정본이 쓰는 «높이 15.68%W + margin .5rem» 이 **정본 자신이 적어 둔 원본 실측 피치보다 행마다 1.4px 짧다** · 클론은 그 식을 글자 그대로 옮겼다(9.77%H)',
    u'리그 도전 행 피치: `style.css` 2607~2612 `.league-challenge-row { height: calc(var(--app-w) * .1568); margin-bottom: .5rem }` — 그런데 **같은 자리의 정본 주석이 원본 실측을 직접 적어 뒀다**: «원본 행 높이 21.56%CW(=78px)·피치 **23.72%CW**». 카드 폭 370px 에 대면 23.72%CW = **87.8px** 인데 식이 내는 값은 78 + .5rem(8.38px) = **86.4px** 라 **행마다 1.4px(= .085rem) 짧다**. 실측이 그대로 그것이다(T28 124회차 · 원작 `shot-042228` 잉크 피치 **87.7px = 9.93%H** · 다섯 행 차 9.95·9.96·9.84·9.95 로 고르다). 클론 `LeagueSheet.cs` 341·377 은 표 `lc_row_h` **0.1568** + `rem * 0.5f` 로 그 식을 그대로 옮겼고 그 값이 **9.77%H** 다 — 원작·클론 앱 상자의 세로/가로가 1.779 ↔ 1.778 로 같으니 남는 0.16%p 는 화면비 탓도 아니다. **곧 정본 ↔ 원작 차이지 클론 결함이 아니다** — T28 121·122회차가 «T463 을 고친 뒤에도 남으면 따로 등재한다» 로 미뤄 둔 그 수(−0.14 × 4 = −0.56)이고, 124회차가 재서 **등재하지 않고 여기 적는다**. 고치려면 정본을 고쳐야 하는데 정본은 읽기 전용이다(§1).'),
    (u"기술 노드 카드 머리 인셋·청동 원 테 — **정본이 제 셈을 틀렸다**: 머리 `margin-left: -3.2%` 를 «패딩 24.6px» 에서 냈는데 정본 제 패딩은 **22px** 이라, 정본대로 선 클론은 원작보다 **0.8%W 더 왼쪽**이다 · 원 테도 원작은 검정 고리가 한 겹 더 있다",
    u"기술 노드 머리 인셋: 정본 **4642** `.item-detail[data-tech-node] .idet-head { margin-left: -3.2% }` 의 −3.2% 는 바로 위 **4640** 주석의 셈에서 나왔다 — "
    u"«원본 아이콘 인셋 **14px** · 클론은 카드 패딩(**24.6px**)에 붙어 +2.15%p 오른쪽» ⇒ 14 − 24.6 = −10.6px = −3.2% × 331.8. "
    u"그런데 **정본 자신이 다른 자리에 제 패딩을 적어 뒀다** — **4335** «`%` 패딩은 컨테이닝 블록(.idet-wrap ≈ 앱폭) 기준이다 — **4.4% 는 22px**». "
    u"곧 4640 의 24.6 은 낡은 수고, 22px 로 셈하면 22 − 10.6 = **11.4px** 라 정본이 겨눈 14px 에 **2.6px 못 미친다**. "
    u"실측이 그대로다(T28 143회차 · 런 1213 ↔ `shot-042605`): 원작 머리 좌 인셋 **3.05%W**(15px) ↔ 정본 계약대로 선 클론 **2.22%W**(12px) — **−0.83%p**. "
    u"T477 이 정본 식을 그대로 옮긴 결과지 옮기기를 잘못한 것이 아니다 — 고치려면 정본을 고쳐야 하는데 정본은 읽기 전용이다(§1). "
    u"같은 자리의 작은 짝 하나 더 — **청동 원 테**: 원작은 #000000 2px 바깥 고리 **+** 청동 어둠 4px 두 겹인데 정본 **3689** `.idet-icon.tn-bronze { border-color: #7a4a22 }` 는 한 겹뿐이고 클론이 정본대로다. "
    u"⚑ 그리고 **T477 을 점수로 되돌리지 마라** — 기하는 정본에 붙었는데 `tech-node` 점수는 런 1209 0.9 ↔ 런 1213 0.9 로 **한 치도 안 움직였다**."),
    (u"기술 노드 «연구 중» 카드 — 원작 샷엔 [취소]·[건너뛰기] **두** 버튼인데 정본은 주인 지시로 [취소] 를 없애 **하나**다 · 클론이 정본대로",
    u"기술 노드 연구 중(`shot-042605`): 원작은 아래 버튼이 **둘**([취소] · [건너뛰기])인데 정본 `js/ui.js` **5582** 가 "
    u"«🚫 [취소] 는 없앴다 — 사용자 지시 2026-08-19 '연구 시작하면 취소 불가'» 로 그 버튼을 **지웠고** 대신 안내줄에 "
    u"`<small class=\"muted\">(취소 불가)</small>` 를 붙였다(5584). 진입점도 폐기했다(`ui.js` **5623** «onTechCancel 은 폐기했다»). "
    u"CSS 가 그 뒤를 따라 `.tech-btns .btn:only-child { flex: 0 1 60% }`(**4616**)로 «혼자 남은 버튼» 을 넓힌다 — 곧 **버튼이 하나인 것이 정본 계약**이다. "
    u"클론 `TechPopups.cs` 도 하나만 그린다. ⇒ 원작 샷과의 이 차이는 **클론 결함이 아니다**(T28 141회차 · 런 1205). "
    u"같은 화면의 **파란 채움 0%%** 도 결함이 아니다 — 남은 시간이 정하는 **상태**고 클론도 그린다(`TechPopups.cs` 279~281 · 정본 `ui.js` 5585 와 같은 식)."),
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
     u"— 클론이 칩 줄을 그리는 것은 **정본대로**다(T28 33회차). "
     u"**T28 81회차 실측 — 이 차이가 이 화면 점수의 거의 전부다**: 붉은 선택 표시가 원작은 "
     u"x **46.2~53.0%W 가운데 한 덧어리**(35×33px · 선택 알)인데 클론은 x **17.2~28.3%W 왼쪽**(61×35px · `.petup-bulk.on` 칩)이다 "
     u"— 같은 줄이 아니라 **다른 물건**이라 자가 짝을 수가 없다. 런 827 전수: 짝 22 · 짝없음 3 · **군더더기 68** 이고 "
     u"어긋난 칸의 큰 수가 전부 그 띄(원작 22.6~33.2%H)에서 나온다(w −92.4 · −90.0 · −89.8%p … = 자리가 아니라 **엉뜬 것끼리 짝지어졌다**는 뜻). "
     u"그 띄 밖 토막은 전부 −5px 안이다(카드 여백 −3.7 · 회색 판1 −2.4 · 어두운 줄 −3.1 · 회색 퍀2 −5.0). "
     u"⇒ **`forge-list` 와 같이 이 화면의 자리는 T28 의 손 밖**이다 — 달성률로도 쪼지 마라."),
    (u"탭바 금속 밴드 — 원작 샷은 통짜 `(14,17,27)`, 정본은 위가 밝고 아래가 어두운 그라데이션. 클론의 램프가 맞다",
     u"정본 `style.css` **8317~8325** 가 `#tabbar` 에 `background-image: linear-gradient(0deg, rgba(255,255,255,.12) 0 1px, …), "
     u"linear-gradient(180deg, rgba(255,255,255,.16) 0, rgba(255,255,255,.03) 30%, rgba(0,0,0,.16) 64%, rgba(0,0,0,.38) 100%)` "
     u"와 inset 그림자 둘을 준다 — 1693 의 `background: #0e111b` 는 **바탕색일 뿐**이고 그 위에 «금속 밴드» 가 얹힌다. "
     u"원작 샷 30장은 그 겹이 붙기 전이라 통짜다: `shot-042120` 탭바 가로줄 **최빈색**이 y 91.5·94.0·97.0% 에서 **전부 `(14,17,27)`**. "
     u"클론은 런 492 까지 통짜 `(13,13,28)`(원작과 같아 보였다) → T357 ✅ 뒤 런 571 에서 "
     u"**`(39,41,50)` → `(25,27,37)` → `(11,14,22)`** 램프가 섰다 — **정본대로 온 것**이다. "
     u"⚠ 한 x 만 찍어 보면 활성 탭 글자(`#ffd54f` 계열 · 예: `(126,105,49)`)를 바탕으로 착각한다 — **가로줄 최빈색**으로 재라. "
     u"T28 53·54회차에 내가 이 램프를 «포스트가 물들였다» 로 두 번 짚었는데 뿌리는 포스트가 아니라 "
     u"**CSS 알파 겹을 선형 공간에서 섞던 것**이었다(워커 G 가 수로 부정 · T357).",
     ),
    (u"하단 네비 칸 수 — 원작 샷은 **다섯 칸**, 정본은 **여섯 칸**(6번째 🐞 디버그 · 주인 지시 «되돌리지 말 것»). 클론의 여섯 칸이 맞다",
     u"T169(워커 H · 런 459~463)가 정본 `index.html` 165 + `main.js` 127~131 의 🚨 «디버그 탭은 기본 노출이다 · "
     u"**되돌리지 말 것** — 다시 숨기려면 사용자 지시가 한 번 더 있어야 한다» 대로 여섯째 칸을 되살렸다. "
     u"원작 샷 30장은 그 지시(2026-08-18) 전에 찍혀 **다섯 칸**이라, 칸이 여섯이 되면 가로 중심이 통째로 옮겨 간다 — "
     u"실측(런 449 → 463 · `screen_dungeons.png` 하단 밴드): 블록 **5개 x 6.9·26.3·46.1·65.4·86.5 → 6개 x 5.2·21.3·37.8·53.7·71.5·87.0** "
     u"(원작은 5개 x 4.8·24.2·45.2·64.5·86.1). 그 한 고침으로 **화면 19개가 내려가 합 −5.2점 · 평균 −0.17** 이 빠졌다"
     u"(`dungeons` 9.3 → 8.6 · `settings` −1.5 · `main` −0.5 …). **점수로 되돌리지 마라** — 되돌리려면 주인의 말이 한 번 더 있어야 한다.",
     ),
    (u"장비 시대 상세 카드 — T177 로 정본 치수에 들어갔는데 점수는 2.2 → 1.1 로 **떨어졌다**. 점수로 되돌리지 마라",
     u"T177(워커 S · 런 422)이 `forge-detail` 카드를 정본 `style.css` 3649 `width: 68.9%` 와 3714 주석의 "
     u"«카드 25.20~72.76%H» 에 맞췄다 — 46회차 실측으로 카드 폭 73.7 → **67.8%W**, 높이 53.5 → **48.3%H** 로 "
     u"원작(68.5 · 47.6)에 ±1%p 안에 들어왔다. 그런데 밴드 점수는 **2.2 → 1.1** 로 내려갔다. 까닭은 딤이다: "
     u"원작은 α .988 이라 카드 바깥이 순검정이고 판독기가 카드 속을 **밴드 하나**(y 25.0 h 47.8)로 읽는데, "
     u"클론은 주인 지시 α .5 라 같은 자리를 밴드 13개로 쪼갠다. 즉 이 화면의 점수는 카드 치수가 아니라 **딤**을 "
     u"재고 있다. 카드가 맞는지는 «팝업 카드 가로 상자» 줄(딤과 무관한 자)로 보라 — 거기서 벗어나지 않으면 맞다.",
     ),
    (u"패스 마일스톤 필 — T375 2회차가 필 높이를 17 → 21px(원작 20px)로 **고쳤는데** 점수는 5.1 → 4.2 로 떨어졌다. 점수로 되돌리지 마라",
     u"패스 마일스톤 필 높이: T375 1회차가 정본 주석의 «≈18.3px» 을 **바깥 상자**로 잘못 읽어 필이 17px 로 나왔고, "
     u"2회차가 `pass_label_h` 를 0.0375 → **0.041**(원작 `shot-042705` 실측 20px/488W)로 고쳤다. "
     u"**T28 67회차 실측(런 653 → 666)**: 필 바깥 상자 **17 → 21px**(기대 22.1 = 원작 20 × 540/488) · 행 피치 114 → **116px**(기대 115.1) "
     u"— 둘 다 원작 쪽으로 왔다. 그런데 화면 점수는 **5.1 → 4.2**(−0.9)이고 지문 «안» 이 3.9 로 뛰어 "
     u"이 자가 «진짜 회귀» 칸에 올렸다. 까닭은 필이 제 높이를 찾으면서 **밴드가 16 → 15 로 붙었기** 때문이다"
     u"(T94 딤·T177 카드와 같은 결). 두 런의 다른 앵커(288~322 · 359~361)는 화소까지 같아 **바뀐 것은 필 높이 하나뿐**임을 확인했다. "
     u"🚫 이 −0.9 를 되돌리지 마라 — 되돌리면 필이 다시 2px 짧아진다."),
    (u"`forge-list` 의 «나란히 −1.9%p» — 원작 샷과 클론의 **시대 행 수가 다르다**(원작 7 ↔ 클론 8). 카드가 내용 높이를 따라가는 화면이라 행 하나가 카드를 통째로 민다 — 자리로 읽지 마라",
     u"「모든 장비의 목록」(`forge-list` ↔ `shot-042905`): 이 카드는 높이가 **내용을 따라간다**. "
     u"**T28 70회차 실측**: 행 피치는 원작 67px ↔ 클론 76px(기대 75.2)로 **맞는데**, 목록의 시대 행이 "
     u"원작 **7개** ↔ 클론 **8개**다(밝은 판 자리: 클론 246·322·398·474·550·626·702·778 ↔ 원작 271·338·405·472·526~645·676). "
     u"행 하나(≈76px)가 더 있으니 카드가 그만큼 커지고, 가운데 정렬이라 **카드 위끝이 통째로 올라간다** "
     u"— 클론 9.17%H ↔ 원작 15.79%H(앱 기준). `--rows` 가 내는 «나란히 −1.9%p ×3 · −1.6%p ×3» 이 그것이다. "
     u"**코드 결함이 아니라 게임 상태(해금한 시대 수) 차이**다 — 정본도 클론도 시대 행을 같은 규칙으로 세이브에서 그리므로 행 수는 두 샷을 찍은 진행도가 정한다. 자리로 쫓지 마라. "
     u"다만 같은 재기에서 **행 안의 밝은 판이 클론 31px ↔ 원작 39~40px(기대 43.8)** 로 30%% 짧게 나왔다 — "
     u"그것은 **71회차에 자의 문턱 탓으로 판명**됐다 — 내용 띠로 다시 재니 셀 줄 원작 47px ↔ 클론 49px(기대 52.8) · 라벨 8 ↔ 7(기대 9.0) · 행 피치 67 ↔ 76(기대 75.2)로 **토막이 전부 ±4px 안**이다. 🔑 **정본이 이 화면을 두고 직접 적어 뒀다**(style.css 715~721): «패널 높이·막대 절대 위치는 **원본 23종 vs 클론 30종**이라 **판정 대상이 아니다**» — 판정할 수 있는 것은 **시대 섹션 사이 간격 하나**(`.forge-age-list` gap = `calc(var(--app-h) * .0492)` = 원본 실측 43px = 4.92%%H · 정본이 `probe-fl-section-gap.js` 로 그 간격만 잰다)뿐이다. 두 샷이 보여 주는 섹션 수가 달라 PNG 로는 그 하나도 못 견준다 — **이 화면의 자리는 T28 의 손 밖이다**. 남은 결함은 검수 Q 의 T372(칸 아래 «0.0000%%» 가 칸 피치보다 넓다)로 본다."),
    (u"리그 상대 이름 — 원작은 흰 글자+키라인, 정본은 민글자 — 클론이 맞다. 같은 행 전투력 숫자의 키라인은 진짜 결함(T109)",
    u"리그 «상대 선택» 상대 이름: 원작 샷(`shot-042228`)의 이름은 **흰 글자 + 검정 키라인**인데 지금 정본 "
    u"`.league-challenge-name` 은 `color: var(--pp-ink)`(#17181a) 민글자다 — 클론의 어두운 민글자가 "
    u"**정본대로**다(T28 22회차 6배 확대 실측). 같은 행에서 **전투력 숫자의 검정 키라인은 정본에도 있다**"
    u"(`style.css` 2635 `-webkit-text-stroke: 2px`) — 그쪽은 진짜 결함이라 T109 로 뗐다."),
    (u"`player-info` 의 점수 흔들림 — 팝업 안은 그대로고 **딤 뒤 하늘이 런마다 다르다**(런 754 초록 ↔ 769 청록) — 점수로 쫓지 마라",
    u"`player-info`: 런 754 → 769 에서 4.0 → 2.9 로 떨어졌는데 두 PNG 를 가로줄로 견주면 "
    u"**카드 안은 그대로**고 달라진 것은 딤 뒤에 비치는 세계 하늘색 하나다 "
    u"(6.5~42.5%H 전폭 · 754 는 `4f635a`(초록 `a0c8b5` 의 딤) · 769 는 `15323e`(청록 `2a647c` 의 딤) — "
    u"같은 런의 `screen_main.png` 은 둘 다 초록이다). 팝업을 여는 순간 뒤에 서 있던 화면/시대가 달라서이고 "
    u"첫 번째 항목(3D 세계 — 원작 샷 ↔ 정본 `SIMPLE_BG`)과 같은 계열이다 — **클론 결함이 아니다**(T28 76회차 실측). 이 화면은 중앙값으로 본다. "
    u"⚑ **«팝업 안은 그대로» 는 76회차의 그 두 런에서만 참이다(188회차 실측 · 런 1363 → 1375)**: 이 회차에 자가 «뒤가 달라졌다»로 올린 자리는 지문 차가 **안 6.2 · 뒤 7.4** 로 **안도 같이 움직였고**, PNG 를 열어 보니 까닭이 분명하다 — 이 카드는 **제 안에 살아 있는 3D 장비 미리보기 띠**를 쥔다(주인공이 무기를 휘두르는 프레임 + 바닥 · 카드 위 ≈25~37%H). 그 띠는 런마다 **다른 프레임**을 찍으므로 «안» 지문도 흔들린다. ⇒ 이 화면의 안·뒤가 **둘 다** 움직여도 그것만으로 «깨졌다»가 아니다 — 글자 겹침·빈 칸·잘림·분홍 머티리얼을 PNG 에서 눈으로 보고 가른다(188회차는 그렇게 보고 **결함 0** 으로 닫았다)."),
    (u"굵은 글자 자간 — T352 `boldSpacing 0` 이 든 런 872 에서 `offline` −2.4 · `league-challenge` −1.2 로 **점수는 내려갔는데 글자 폭은 원작에 붙었다**. 되돌리지 마라",
    u"굵은 글자 자간(T352 5회차 `boldSpacing 0`): 런 859 → 872 에서 자가 `offline` 4.0 → **1.6**(−2.4) · "
    u"`league-challenge` 4.2 → **3.0**(−1.2)을 «그림이 실제로 달라졌다» 로 올렸다. 그런데 **글자 폭을 원작과 재면 그 반대**다 "
    u"(T28 85회차 실측 · 앱 기준 잉크 폭): `offline` 제목 원작 **22.60%W** ↔ 클론 859 **33.15%W**(+10.6%p) → 872 **21.48%W**(−1.1%p) · "
    u"`league-challenge` 부제 원작 **45.67%W** ↔ 859 **56.48%W** → 872 **51.48%W**. 둘 다 **원작 쪽으로 왔고** `offline` 제목은 화소까지 맞았다 "
    u"(자리도 28.33 → 27.40%H 로 원작 27.11 에 붙었다). 굵은 글자가 좁아지면 잉크 띠가 붙거나 갈라져 밴드 수가 바뀌고(12 → 14 · 13 → 9) 점수만 빠진다 "
    u"— T375 의 패스 필·T177 의 장비 카드와 **같은 계열**이다. 까닭도 분명하다: **정본(브라우저)에는 «굵게» 에 자간을 더하는 기능이 없고** "
    u"TMP 의 `boldSpacing` 이 클론 쪽에만 있던 군더더기라, 0 으로 둔 것이 정본을 따른 것이다. 점수로 되돌리지 마라."),
    (u"리그 행 피치 — T378 10회차가 `×1.1` 을 걷어 **원작 피치에 맞췄는데** 점수는 4.4 → 3.6 이다. 되돌리지 마라",
    u"리그 시트 행 피치(T378 10회차 · `LeagueSheet.cs:96` 의 `UiKit.H(\"league_row_h\") * 1.1f` 를 걷었다): "
    u"자가 런 898 → 903 에서 `league` 4.4 → **3.6** 을 «그림이 실제로 달라졌다» 로 올렸는데 **행 피치는 원작에 맞았다** "
    u"(T28 88회차 실측 · 앱 기준): 원작 **67~68px**(정본 2320·2326 = 행 6.36%H + 간격 1.12%H = 7.48%H) · 기대 **73.2px** · "
    u"클론 런 898 **78px**(+4.8) → 런 903 **72px**(−1.2). 행이 제 높이를 찾으며 밴드가 11 → 12 로 갈려 점수만 빠졌다 "
    u"— T375 의 패스 필·T177 의 장비 카드·굵은 글자 자간과 **같은 계열**이다. 같은 화면에 남은 «목록 위끝 −1.8%p»(원작 21.81 ↔ 클론 20.00%H)는 **다른 자리**이고 T421 로 이미 등재돼 있다."),
    (u"던전 상세 **아래 절반** — 원작이 정본보다 **25.1px 더 길다**(스테이지 줄 아래 여백 · 버튼 높이). 점수로 쫓지 마라",
    u"`dungeon-detail` 의 알약·열쇠·버튼이 원작보다 위에 서는 것(T28 89회차가 −11.9~−15.8px 로 올린 자리)은 "
    u"**대부분 클론 결함이 아니다** — 95회차가 카드 안 열두 토막을 전수로 세어 셈을 닫았다(960 기준). "
    u"**정본↔원작 −25.1**: ⓐ 스테이지 줄 아래 여백 — 원작 **23.1px** 인데 정본 5304 는 `.dgd-stage-row { margin-bottom: **.5rem** }`(9.10) 이다(**−14.0**) "
    u"ⓑ 버튼 — 원작 상자 **74.8px** 인데 정본 5355 `.dgd-btns .btn { min-height: 3.4rem }` + `.dgd-btn { .92rem · line-height 1.25 · padding .6rem }` 는 두 줄에서 **3.5rem = 63.7**(**−11.1**). "
    u"그 위(배너 15.4%H · 스테이지 줄 42.5 · 배너 마진 .6rem+.3rem)와 그 아래(버튼 아래 2.45rem = 원작 60.8 · 카드 높이 49.6%H = 원작 49.72%H)는 **정본이 원작과 맞는다**. "
    u"**클론↔정본 −11.7** 은 따로 있고 그쪽이 고칠 자리다(T433 · 인라인 아이콘 줄 상자). 카드는 둘 다 `min-height` 로 바닥을 치므로 이 차는 전부 "
    u"`margin-top: auto` 두 곳으로 흘러 **열쇠 위·아래 여백**으로 나타난다 — 원작 25/26px ↔ 클론 47/46px(T409 판정값)이고 그 차 ≈ **38 ≈ 25.1 + 11.7(36.8)** 로 셈이 1.2px 안에서 닫힌다. "
    u"곧 «알약이 −12px 위» 를 클론 배치로 지우려 하면 **정본을 벗어나게 된다**(§1)."),
    (u"프로필 **제목 아래 → 아바타** 틈 — 원작이 정본보다 **≈20px 더 길다**. 점수로 쫓지 마라",
    u"`profile` 의 아바타·이름칸·성별칸·랭킹 줄이 원작보다 위에 서는 것(94회차가 −19~24px 로 잰 자리)은 "
    u"**클론 결함이 아니다**. 99회차가 **정본이 제 주석에 적어 둔 원작 수**로 닫았다 — `css/style.css` 2997 "
    u"«원본 `shot-042724` **x97..163 = 65px · 세로 y211..274 = 64px**»(= 13.20%W). 그 `y211` 을 앱 상자로 옮기면 "
    u"카드 위끝에서 **78.3** 인데(내가 잰 79.4 와 1.1px 차), 정본 CSS 를 쌓으면 카드 패딩 `1.1rem`(20.0) + "
    u"제목 줄 상자(`1.5rem` × ≈1.25 = 34.1) + `.profile-title { margin: 0 0 **.2rem** }`(3.6) = **57.7** 이고 "
    u"**클론이 58.0** 이다(런 1012 실측). 곧 클론은 **정본을 정확히 따르고 있고 원작만 ≈20px 아래**다. "
    u"아바타 **크기**는 맞다 — 원작 65px → 기대 **72.9** ↔ 클론 **≈72**(표 `profile_avatar` 0.132 = 정본 13.2%W). "
    u"같은 화면의 «성별 칸 → 서버 랭킹» −8.8 도 표값·기준이 다 맞고 **글자 하한**(정본 `.82rem` 29.8 ↔ `Sub` 36) 몫이다. "
    u"이 화면에서 실제로 고칠 것은 **랭킹 버튼 폭(T432)** 하나였다(94회차)."),
    (u"상점 머리 부제 틈 — 원작은 «배너~부제 넓다 : 부제~카드 좁다»(33:21px)인데 정본 `.shop-sub { margin: .34rem 0 1.42rem }` 는 **반대**(좁다:넓다)다 · 클론이 정본대로라 부제 위끝만 −2.6%p 떠 있다",
    u"상점 부제 틈: 정본 `css/style.css` **2898** `.shop-sub { … margin: **.34rem 0 1.42rem** }` — **위 틈이 좁고 아래 틈이 넓다**. "
    u"실측(검수 Q 47회차 · 원작 `screens/shot-042632.png` 500×878 ↔ 클론 런 1343 `screen_shop.png` 540×960): "
    u"금색 배너 위끝 7.06 ↔ 6.35%H(−0.71) · 배너 높이 3.99 ↔ 4.06 · **부제 위끝 14.81 ↔ 12.19%H(−2.62%p)** · 부제 높이 1.48 ↔ 1.56 · 첫 카드 흰 면 위끝 18.68 ↔ 18.12(−0.56). "
    u"곧 **위도 아래도 ±0.7%p 안인데 그 사이 부제 하나만 2.6%p(≈25px@960) 떠 있다** — 결함처럼 보이는 꼴이다. 그런데 틈의 **방향**을 보면 클론이 맞다: "
    u"클론 배너 아래~부제 ≈17px · 부제~첫 카드 ≈42px(= 정본 .34 : 1.42) ↔ **원작 ≈33px : ≈21px(반대)**. "
    u"곧 **원작 샷과 지금 정본이 서로 다른 자리**고 §1 대로 클론은 정본을 따른 것이다 — **고치면 정본에서 멀어진다**. "
    u"⚑ `shop` 은 달성 81% 라 «다음 볼 화면» 에 잘 안 들어와 **보는 사람마다 처음부터 다시 판다**(검수 Q 47회차도 재다가 정본 CSS 를 보고서야 돌아섰다 · T496). **점수로 쫓지 마라.**"),
    (u"특가 카드 보상 알약 수 — 원작 셋(셋째가 젬) ↔ 정본 둘 · 정본이 «젬 보급 전면 제거» 로 **필드 자체를 뺐다**(클론의 둘이 맞다)",
    u"특가 카드 알약: 정본 `js/shop.js` **4~10행** 이 제 주석에 까닭을 적어 뒀다 — «수량은 원본(`shot-042632`) 표기 그대로. 단 **원본 3번째 줄의 젬은 제거** — "
    u"\u2018젬 보급 전면 제거\u2019(TODO 재화 개편) · UI-SPEC 재화 표 \u2018젬 = 보급 없음, 상점 더미\u2019 에 따라 젬은 어떤 경로로도 지급되지 않는다. "
    u"**reward 가 카드 pill 표기의 원본이므로 필드 자체를 뺀다**». 그래서 세 카드의 `reward` 가 둘씩이고(`potions+hammers` · `eggCurrency+hammers` · `winders+hammers`) 클론도 둘이다 — "
    u"원작 샷의 **세 번째 알약을 클론 결함으로 읽지 마라**(T496 · 같은 화면 · 같은 갈래)."),
    (u"설정 토글 색 — 원작은 켜짐 트랙이 **초록**(4,255,0)이고 손잡이가 **두 상태 다 파랑**인데, 정본은 켜짐 `--pp-blue` **#005dff** · 꺼짐 `--pp-gray` **#c4c4c4** + **흰** 손잡이다 · 클론이 정본과 화소까지 같다",
    u"설정 토글: 정본 `css/style.css` **3107~3116** 이 못박아 뒀다 — `.settings-toggle { background: var(--pp-gray) }` · "
    u"`::after { background: #fff; left: .08rem }`(꺼짐 = 회색 트랙 + 흰 손잡이가 **왼쪽**) · `.settings-toggle.on { background: var(--pp-blue) }` · "
    u"`.on::after { left: 1.18rem }`(켜짐 = 파란 트랙 + 흰 손잡이가 **오른쪽**) · **3507** `--pp-blue: #005dff` · **3510** `--pp-gray: #c4c4c4`. "
    u"화소로 재니 **클론이 정본과 바이트까지 같다**(런 1359 `screen_settings.png` 540×960): 켜진 줄 y=254 트랙 **(0,93,255)** x382~421 · 흰 손잡이 x406~418(**오른쪽**) · "
    u"꺼진 줄 y=437 트랙 **(196,196,196)** · 흰 손잡이 x385~397(**왼쪽**). "
    u"그런데 원작 `screens/shot-042744.png`(489×884)은 **아주 다르다**: 토글 여섯의 손잡이가 **전부 파랑 (0,93,255)**(폭 ≈23px)이고 트랙은 꺼짐이 **어두운 (12,18,32)** · "
    u"켜진 줄(y≈353~371 «채팅 표시») 하나만 트랙이 **초록 (4,255,0)** 에 손잡이가 오른쪽(x374~396)이다. "
    u"⇒ **원작 샷과 지금 정본이 서로 다른 자리**고 §1 대로 클론은 정본을 따른 것이 맞다 — **고치면 정본에서 멀어진다**. "
    u"⚑ `settings` 는 달성 **70%대**라 «다음 볼 화면» 다섯에 거의 안 들어와 **보는 사람마다 처음부터 다시 판다**(검수 Q 51회차가 −1.2 를 파다 여기까지 왔다 · T508). "
    u"게다가 **색은 «한눈에 틀려 보이는»** 갈래라 자리·치수보다 헛걸음을 부르기 쉽다 — **점수·색으로 쫓지 마라.**"),
]


# ── 그 25줄의 «정본 근거» 를 기계가 읽는 칸 (T512 1회차 · 걸음 ⓐ · 워커 J) ─────────
# 위 목록은 **사람이 적은 글**이고, 그 안의 «정본 1833 `.cmp-card { … }`» 꼴 근거는 정본이 바뀌면
# **조용히 거짓**이 된다(아무 자도 안 본다 · 검수 Q 52회차가 세 회차째 같은 자리를 다시 판 뿌리).
# 그래서 **근거를 데이터로** 따로 둔다 — 키는 그 줄의 머리(`' — '` 앞), 값은 `(정본 파일, 줄, 바늘)` 들.
#   · `정본 파일` = `.wwwww-src/web/` 아래 상대 경로 · `줄` = 그 글이 **인용한** 줄 번호(없으면 0)
#   · `바늘` = 그 선언에서 **그 자리에서만 나오는 글자**(정본 전체에 한 번만 있어야 한다 — 자기 검사가 센다)
# 자기 검사는 ⓐ 모든 키가 위 목록의 머리와 1:1 인지 ⓑ 바늘이 **없어지면 잡는지**(일부러 고친 사본으로)
# ⓒ 정본 사본이 옆에 있으면 **실물에서 바늘 전부가 아직 있는지** 를 본다(사본이 없으면 그 칸만 건너뛴다 —
# `gate.sh`·CI 의 자기 검사 잡에는 정본 clone 이 없다).
STALE_REF_EVIDENCE = {
    u"리그 도전 행 피치": (
        (u"css/style.css", 2606, u".league-challenge-row {"),
        (u"css/style.css", 2612, u"height: calc(var(--app-w) * .1568)"),
        (u"css/style.css", 2609, u"border-radius: .7rem; padding: 0 .6rem; margin-bottom: .5rem;"),
    ),
    u"기술 노드 카드 머리 인셋·청동 원 테": (
        (u"css/style.css", 4642, u".item-detail[data-tech-node] .idet-head { margin-left: -3.2%; }"),
        (u"css/style.css", 3689, u".idet-icon.tn-bronze {"),
        (u"css/style.css", 3689, u"border-color: #7a4a22"),
    ),
    u"기술 노드 «연구 중» 카드": (
        (u"css/style.css", 4616, u".tech-btns .btn:only-child { flex: 0 1 60%; }"),
        (u"js/ui.js", 5582, u"[취소] 는 없앴다"),
        (u"js/ui.js", 5584, u"(취소 불가)"),
    ),
    u"3D 세계": (
        (u"js/scene3d.js", 0, u"SIMPLE_BG: true,"),
    ),
    u"모달 딤": (
        (u"css/style.css", 0, u"background: rgba(0,0,0,.5); z-index: 20;"),
    ),
    u"제작 비교 버튼": (
        (u"js/ui.js", 3266, u"UI.resolveCraft('sell')"),
        (u"js/ui.js", 3269, u"'기존 교체'"),
    ),
    u"시대 이름": (
        (u"js/gamedata.js", 18, u"divine: '천상'"),
    ),
    u"기술 트리 행": (
        (u"js/techtree.js", 264, u"'첫 타입 단독 → 나머지 2개씩'"),
    ),
    u"아이콘 블록화": (
        (u"js/icongen.js", 120, u"블록 화법 (slug `ui-icon-blockify`"),
    ),
    u"장비 상세 ✕": (
        (u"css/style.css", 1733, u"geardetail-no-close"),
    ),
    u"펫 업그레이드 선택 줄": (
        (u"css/style.css", 4365, u"개수 제한 철폐"),
        (u"css/style.css", 4366, u".petup-bulkrow {"),
    ),
    u"탭바 금속 밴드": (
        (u"css/style.css", 8317, u"linear-gradient(0deg, rgba(255,255,255,.12) 0 1px, rgba(255,255,255,0) 1px),"),
    ),
    u"하단 네비 칸 수": (
        (u"js/main.js", 127, u"nav-6th-keep-debug"),
    ),
    u"장비 시대 상세 카드": (
        (u"css/style.css", 3649, u"#forge-item-modal .modal-card.item-detail { width: 68.9%; }"),
    ),
    u"리그 상대 이름": (
        (u"css/style.css", 2635, u".league-challenge-name small { color: #ff880f;"),
    ),
    u"상점 머리 부제 틈": (
        (u"css/style.css", 2898, u".shop-sub { text-align: center;"),
        (u"css/style.css", 2898, u"margin: .34rem 0 1.42rem;"),
    ),
    u"특가 카드 보상 알약 수": (
        (u"js/shop.js", 7, u"필드 자체를 뺀다"),
    ),
    u"설정 토글 색": (
        (u"css/style.css", 3115, u".settings-toggle.on { background: var(--pp-blue); }"),
        (u"css/style.css", 3507, u"--pp-blue: #005dff;  --pp-blue-dk: #001c4e;"),
    ),
}

#: 인용한 줄 번호와 실제 줄이 이만큼 넘게 벌어지면 «글의 줄 번호가 낡았다» 고 알린다(막지는 않는다 —
#: 정본이 자라면 아래쪽 줄 번호는 다 밀리고, 그것은 근거가 **사라진 것과 다른 일**이다).
NOTE_LINE_DRIFT = 40


def note_head(short):
    """그 줄의 머리 — `STALE_REF_EVIDENCE` 의 키다(`' — '` 앞)."""
    return short.split(u" — ")[0].strip()


def evidence_problems(reader, table=None):
    """근거 바늘이 정본에 아직 있는가 — 문제 줄의 목록을 돌려준다(빈 목록 = 다 있다).

    `reader(상대경로)` 는 그 파일의 글 전체 또는 None(못 읽었다)을 내야 한다 —
    자기 검사가 **일부러 고친 사본**을 먹이는 자리가 여기다."""
    table = STALE_REF_EVIDENCE if table is None else table
    bad = []
    for head in sorted(table):
        for path, line, needle in table[head]:
            text = reader(path)
            if text is None:
                bad.append(u"%s · %s 를 못 읽었다" % (head, path))
                continue
            lines = text.split(u"\n")
            at = [i + 1 for i, l in enumerate(lines) if needle in l]
            if not at:
                bad.append(u"%s · %s 에서 근거가 **사라졌다** — 바늘 «%s»"
                           % (head, path, needle))
            elif line and min(abs(a - line) for a in at) > NOTE_LINE_DRIFT:
                bad.append(u"⚠ %s · %s 의 줄 번호가 낡았다 — 글은 %d, 실제 %s(근거 자체는 있다)"
                           % (head, path, line, at[0] if len(at) == 1 else at))
    return bad


# ── 원작 샷 ↔ 정본이 못박은 값을 한 줄로 (T512 2회차 · 걸음 ⓑ · 워커 J) ─────────
# 검수 Q 가 47·51·52회차에 **손으로** 한 일이 이것이다: ⓐ 정본 CSS 를 grep 해 그 선택자의 선언을 찾고
# ⓑ 클론 샷에서 그 값이 그려진 자리를 찾고 ⓒ **원작 샷의 같은 자리** 화소를 떠서 견준다.
# 그 셋이 `--vs <화면> <선택자>` 한 줄이다. 자리를 «선택자 → 좌표» 로 아는 길은 없으므로(그러려면
# 브라우저가 필요하다) **색이 그려진 자리를 클론에서 찾아** 그 자리를 원작으로 옮긴다 — 클론은 정본을
# 따르는 것이 계약이라(§1) «정본이 못박은 색이 클론에 있는 자리» 가 곧 그 선택자의 자리다.
#
# 길이는 자리를 안 찾는다(결정 874) — 값만 **앱 상자 비율**로 환산해 찍는다. 정본 `main.js` 17 이
# `fontSize = h / 844 * 16` 이라 1rem 은 해상도와 무관하게 앱 높이의 16/844 다.
REM_PCT_H = 16.0 / 844.0 * 100.0          # 1rem = 1.8957 %H (정본 main.js 17)
REM_PCT_W = REM_PCT_H * 16.0 / 9.0        # 앱은 늘 9:16 이므로 %W 로는 그 16/9 배다
VS_TOL = 6                                 # 화소 맞추기 허용 오차(채널당) — 먼저 0 으로 보고, 없으면 이만큼
VS_MIN_BLOB = 24                           # 이보다 작은 덩어리는 글자 테·압축 잡음이라 안 센다
VS_MAX_ROWS = 8                            # 찍는 덩어리 줄 수 상한(**다른 것부터** 찍는다)
VS_BLOB_NOISE = 12                         # 덩어리가 이보다 많으면 «그 색은 화면에 널려 있다» — 자리 가리기가 안 된다
# T517 — **클론 쪽**도 ±N 으로 본다. `scan_ref` 는 원작 쪽을 `VS_TOL` 6 으로 보면서 클론 화소는 «바이트가 딱 맞을 때만»
#   표에 맞췄다 — 유니티는 못박은 색을 1~2 어긋나게 그린다(`#00897b` → (0,137,**124**) · `#c62828` → (198,**38**,**38**)).
#   검수 Q 54회차 실측(런 1375 · 31화면 전수): 못박은 색 **109개 중 17개**가 «≤2 로만 찍히고 딱 맞게는 한 화소도 없다» —
#   `pets` 한 장에서 `.cur-pill.egg`(딱맞음 629)는 올라오고 바로 옆 `.cur-pill.gem`(딱맞음 **0** · ≤2 577)은 안 올라왔다.
#   ⚠ 넓히는 것은 «클론 화소 → 표» 열쇠 **하나**다 — `VS_TOL`·`VS_MIN_BLOB`·`VS_BLOB_NOISE` 는 그대로다.
VS_CLONE_TOL = 2                           # 클론 화소를 표 색으로 읽을 때의 채널당 허용 오차(T517 · 실측 ±2 면 17개가 다 들어온다)
CHROMA_HUE = 40                            # 채널 최대−최소가 이 이상이면 «유채색» — 색 갈래와 어둠·판 갈래를 가르는 금


def css_decls_for(css_text, selector):
    """정본에서 **그 선택자 그대로**인 규칙의 선언 전부 → [(줄, 속성, 값)] (뒤 규칙이 뒤에 온다)."""
    try:
        sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
        import check_pinned_colors as cpc
        strip, step_sel = cpc.strip_comments, cpc.STEP_SEL
    except Exception:
        strip = lambda t: re.sub(r"/\*.*?\*/", lambda m: "\n" * m.group(0).count("\n"), t, flags=re.S)
        step_sel = re.compile(r"^(?:\d+(?:\.\d+)?%|from|to)(?:\s*,\s*(?:\d+(?:\.\d+)?%|from|to))*$")
    css = strip(css_text)
    out = []
    for m in re.finditer(r"([^{}]+)\{([^{}]*)\}", css):
        sel_raw = " ".join(m.group(1).split())
        if sel_raw.startswith("@") or "keyframes" in sel_raw:
            continue
        # `@keyframes` 의 **단계**(`0%`·`from`·`to`)는 선택자가 아니다 — 바깥 블록은 위에서 걸리지만
        # 이 정규식은 **안쪽 블록**을 따로 물어 `0% { … }` 를 «선택자 0%» 로 센다(T396 3회차가 만난 그 자리).
        if step_sel.match(sel_raw):
            continue
        if selector not in [s.strip() for s in sel_raw.split(",")]:
            continue
        # ⚠ `m.start()` 는 **앞 규칙의 `}` 바로 뒤**라 줄이 하나 앞으로 밀린다 — 선택자 글자가 선 자리로 센다.
        head = m.start(1) + (len(m.group(1)) - len(m.group(1).lstrip()))
        line = css[:head].count("\n") + 1
        for d in m.group(2).split(";"):
            if ":" not in d:
                continue
            prop, val = d.split(":", 1)
            prop, val = prop.strip(), val.strip()
            if prop and val:
                out.append((line, prop, val))
    return out


def css_tokens(css_text):
    """`--pp-blue: #005dff` 꼴 토큰 표 → {이름: 값}."""
    out = {}
    for m in re.finditer(r"(--[a-z0-9-]+)\s*:\s*([^;}]+)", css_text):
        out.setdefault(m.group(1), m.group(2).strip())
    return out


def resolve_css_value(val, tokens, depth=0):
    """`var(--pp-blue, #fff)` 를 토큰 표로 푼다(없으면 대체값 · 더 없으면 그대로)."""
    if depth > 4:
        return val
    def one(m):
        name, alt = m.group(1), (m.group(2) or "").strip()
        got = tokens.get(name)
        return got if got else (alt if alt else m.group(0))
    out = re.sub(r"var\(\s*(--[a-z0-9-]+)\s*(?:,([^()]*))?\)", one, val)
    return resolve_css_value(out, tokens, depth + 1) if out != val else out


def css_color_rgb(val):
    """CSS 색 값 → `(r, g, b)` 또는 None(색이 아니다)."""
    v = val.strip().lower()
    m = re.match(r"^#([0-9a-f]{3})$", v)
    if m:
        return tuple(int(c * 2, 16) for c in m.group(1))
    m = re.match(r"^#([0-9a-f]{6})", v)
    if m:
        h = m.group(1)
        return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16))
    m = re.match(r"^rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)", v)
    if m:
        return (int(m.group(1)), int(m.group(2)), int(m.group(3)))
    return None


def css_rem_lengths(val):
    """그 값 안의 `N rem` 전부 → [(글자, 수)] (0 은 길이가 아니라 안 센다)."""
    return [(m.group(0), float(m.group(1))) for m in re.finditer(r"(-?[\d.]+)rem", val)]


def _px_at(img, x, y):
    i = ((y * img.w) + x) * 3
    return (img.px[i], img.px[i + 1], img.px[i + 2])


def _find_color(img, rgb, tol):
    """그 색인 화소의 자리들 → [(x, y)] (앱 상자 안만)."""
    ax, ay, aw, ah = app_box(img)
    hit = []
    for y in range(ay, ay + ah):
        row = (y * img.w) * 3
        for x in range(ax, ax + aw):
            i = row + x * 3
            if (abs(img.px[i] - rgb[0]) <= tol and abs(img.px[i + 1] - rgb[1]) <= tol
                    and abs(img.px[i + 2] - rgb[2]) <= tol):
                hit.append((x, y))
    return hit


def _median_rgb(vals):
    if not vals:
        return None
    return tuple(sorted(v[c] for v in vals)[len(vals) // 2] for c in range(3))


def vs_report(screen, selector, shots_dir, ref_dir=None, css_path=None):
    """`--vs <화면> <선택자>` — 원작 샷의 그 자리와 정본이 못박은 값을 **나란히** 찍는다(T512 걸음 ⓑ)."""
    ref_dir = REF_DIR if ref_dir is None else ref_dir
    css_path = os.path.join(REPO, ".wwwww-src", "web", "css", "style.css") if css_path is None else css_path
    if not os.path.exists(css_path):
        print(u"✗ 정본 CSS 를 못 읽었다: %s" % css_path)
        print(u"  · ROUTINE §0 의 `git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src` 뒤에 다시 돌려라")
        return 1
    css = open(css_path, encoding="utf-8").read()
    decls = css_decls_for(css, selector)
    if not decls:
        print(u"✗ 정본에 «%s» 그대로인 규칙이 없다 — 선택자를 정본 글자 그대로 줘라"
              u"(조상이 붙은 꼴 `.modal-card %s` 은 **다른 선택자**다)" % (selector, selector))
        return 1
    tokens = css_tokens(css)

    ref_rel_path = dict(pairs()).get(screen)
    ref_img = clone_img = None
    ref_name = clone_name = u"—"
    cp = os.path.join(shots_dir, "screen_%s.png" % screen)
    if os.path.exists(cp):
        clone_img, clone_name = png_read(cp), os.path.basename(cp)
    if ref_rel_path:
        rp = os.path.join(ref_dir, ref_rel_path)
        if os.path.exists(rp):
            ref_img, ref_name = png_read(rp), os.path.basename(rp)

    print(u"")
    print(u"· 정본 선언 — %s (`css/style.css` %s) · 선언 %d개"
          % (selector, u"·".join(str(l) for l in sorted(set(d[0] for d in decls))), len(decls)))
    print(u"  클론 샷 %s · 원작 샷 %s" % (clone_name, ref_name))
    for line, prop, raw in decls:
        got = resolve_css_value(raw, tokens)
        rgb = css_color_rgb(got)
        if rgb is not None:
            tail = u" → #%02x%02x%02x" % rgb if got.strip().lower() != u"#%02x%02x%02x" % rgb else u""
            print(u"    %s: %s%s%s" % (prop, raw, (u"  →  " + got) if got != raw else u"", tail))
            _vs_color_line(rgb, clone_img, clone_name, ref_img, ref_name)
            continue
        rems = css_rem_lengths(got)
        if rems:
            print(u"    %s: %s%s" % (prop, raw, (u"  →  " + got) if got != raw else u""))
            for txt, n in rems:
                print(u"        %-8s = %.2f%%H · %.2f%%W (정본 main.js 17 `fontSize = h/844*16`)"
                      % (txt, n * REM_PCT_H, n * REM_PCT_W))
            continue
        print(u"    %s: %s%s   (색도 rem 도 아니다 — 눈으로 봐라)"
              % (prop, raw, (u"  →  " + got) if got != raw else u""))
    return 0


def _clusters(hit, gap=2):
    """맞은 화소들 → **덩어리**별 자리 목록. 자리 하나(`gap` 안)로 이어진 것을 한 덩어리로 묶는다.

    ⚑ 왜 덩어리로 가르나(2회차 실측): `--vs settings .settings-toggle.on` 을 **통째 중앙값**으로 재면
    «같다» 가 나온다 — 정본이 못박은 `#005dff` 가 그 화면에 **여섯 토글의 손잡이까지** 깔려 있고
    원작도 손잡이가 같은 파랑이라 중앙값이 파랑으로 묻힌다. 검수 Q 51회차가 손으로 본 것은
    **켜진 줄의 트랙**(원작은 거기만 초록 `(4,255,0)`)이었다 — 곧 **한 선택자의 자리가 여럿**이고
    그중 몇만 다르다. 덩어리로 갈라야 그 몇이 보인다."""
    todo = set(hit)
    out = []
    while todo:
        seed = todo.pop()
        comp = [seed]
        frontier = [seed]
        while frontier:
            cx, cy = frontier.pop()
            for dx in range(-gap, gap + 1):
                for dy in range(-gap, gap + 1):
                    q = (cx + dx, cy + dy)
                    if q in todo:
                        todo.discard(q)
                        comp.append(q)
                        frontier.append(q)
        out.append(comp)
    return out


def _vs_color_line(rgb, clone_img, clone_name, ref_img, ref_name):
    """정본이 못박은 색 하나 — 클론에서 그 색 덩어리를 찾고 원작의 **같은 자리**를 떠서 견준다."""
    if clone_img is None:
        print(u"        \u00b7 클론 샷이 없어 자리를 못 찾는다(촬영 뒤에 다시 돌려라)")
        return
    tol = 0
    hit = _find_color(clone_img, rgb, tol)
    if not hit:
        tol = VS_TOL
        hit = _find_color(clone_img, rgb, tol)
    if not hit:
        print(u"        \u00b7 클론 %s 에 그 색이 **없다**(\u00b1%d 까지 봤다) — 클론이 정본을 안 따르는 자리일 수 있다"
              % (clone_name, VS_TOL))
        return
    comps = [c for c in _clusters(hit) if len(c) >= VS_MIN_BLOB]
    print(u"        \u00b7 클론 %s: 그 색 화소 %d개%s \u00b7 덩어리 %d개(%d화소 미만은 뺐다)"
          % (clone_name, len(hit), u"" if tol == 0 else u"(\u00b1%d)" % tol, len(comps), VS_MIN_BLOB))
    if ref_img is None:
        print(u"        \u00b7 원작 짝이 없어 견줄 자리가 없다")
        return
    rax, ray, raw_, rah = app_box(ref_img)
    rows = []
    for comp in comps:
        vals = []
        step = max(1, len(comp) // 200)
        for x, y in comp[::step]:
            px, py = app_rel(clone_img, x, y)
            rx = int(round(rax + px / 100.0 * raw_))
            ry = int(round(ray + py / 100.0 * rah))
            if 0 <= rx < ref_img.w and 0 <= ry < ref_img.h:
                vals.append(_px_at(ref_img, rx, ry))
        med = _median_rgb(vals)
        if med is None:
            continue
        xs, ys = [q[0] for q in comp], [q[1] for q in comp]
        x0p, y0p = app_rel(clone_img, min(xs), min(ys))
        x1p, y1p = app_rel(clone_img, max(xs), max(ys))
        rows.append((max(abs(med[c] - rgb[c]) for c in range(3)), len(comp), med,
                     (x0p, x1p, y0p, y1p), len(vals)))
    rows.sort(key=lambda t: (-t[0], -t[1]))          # **다른 것부터** — 찾는 것이 그것이다
    same = sum(1 for r in rows if r[0] <= VS_TOL)
    if len(rows) > VS_BLOB_NOISE:
        # 2회차 실측 — `.shop-sub { color: #fff }` 는 덩어리 **91개**가 나온다(화면의 흰 글자 전부).
        # 이 길은 «정본이 못박은 색이 클론에 있는 자리» 로 자리를 찾으므로, 그 색이 **그 선택자만 쓰는 색**이
        # 아니면 가릴 수가 없다. 조용히 그럴듯한 줄을 찍으면 그게 더 나쁘다(결정 873).
        print(u"        \u26a0 그 색은 이 화면에 **널려 있다**(덩어리 %d개) — 아래 줄은 그 선택자만의 자리가 아니다."
              u" 색 갈래는 «그 선택자만 쓰는 색»(면\u00b7트랙 따위)에서만 쓸모가 있다" % len(rows))
    for d, n, med, box, ns in rows[:VS_MAX_ROWS]:
        print(u"          %s x %.1f~%.1f%%W \u00b7 y %.1f~%.1f%%H (%d화소) \u2014 원작 (%d,%d,%d) #%02x%02x%02x \u21d2 **%s**(차 %d \u00b7 표본 %d)"
              % ((u"\u2716" if d > VS_TOL else u"\u2713", box[0], box[1], box[2], box[3], n)
                 + med + med + (u"다르다" if d > VS_TOL else u"같다", d, ns)))
    if len(rows) > VS_MAX_ROWS:
        print(u"          \u00b7 … 덩어리 %d개 더(차가 작은 쪽)" % (len(rows) - VS_MAX_ROWS))
    print(u"        \u21d2 덩어리 %d개 중 원작과 **다른 것 %d개** \u00b7 같은 것 %d개 (정본 #%02x%02x%02x)"
          % ((len(rows), len(rows) - same, same) + rgb))



def face_color_table(css_text):
    """정본이 **면**(`background`)에 못박은 색 → [(선택자, 줄)] (토큰도 풀어서 센다 · T512 걸음 ⓒ).

    `check_pinned_colors.pinned_decls` 는 «토큰과 **다른** 리터럴» 만 걷는다(그 자의 본업이 그것이다).
    여기는 반대로 **색이면 다 걷는다** — 판정의 그 자리(`.settings-toggle.on { background: var(--pp-blue) }`)가
    바로 토큰 갈래라, 토큰을 빼면 찾으려는 것을 못 찾는다.
    """
    try:
        sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
        import check_pinned_colors as cpc
        decls, props = cpc.all_decls, cpc.FACE_PROPS
    except Exception:
        return {}
    tokens = css_tokens(css_text)
    out = {}
    for sel, line, raw in decls(css_text, props):
        rgb = css_color_rgb(resolve_css_value(raw, tokens))
        if rgb is not None:
            out.setdefault(rgb, []).append((sel, line))
    return out


def _chroma(rgb):
    return max(rgb) - min(rgb)


def clone_lut(table, tol=None):
    """T517 — 표 색마다 ±<paramref name="tol"/> 안의 모든 바이트 열쇠를 미리 펼친 조회표: 열쇠 → (표 색, 어긋남).

    한 화소마다 109개 색과 거리를 재면 100만 화소에서 못 돈다 — 그래서 표 쪽을 펼친다(109 × 5³ ≈ 1만 3천 칸).
    두 표 색이 같은 열쇠를 다투면 **가까운 쪽**이 가져가고 거리가 같으면 먼저 온 쪽이 쥔다(그 다툼 수는 부르는 쪽이 찍는다).
    """
    tol = VS_CLONE_TOL if tol is None else tol
    lut, clash = {}, 0
    rng = range(-tol, tol + 1)
    for rgb in table:
        for dr in rng:
            r = rgb[0] + dr
            if r < 0 or r > 255:
                continue
            for dg in rng:
                g = rgb[1] + dg
                if g < 0 or g > 255:
                    continue
                for db in rng:
                    b = rgb[2] + db
                    if b < 0 or b > 255:
                        continue
                    d = max(abs(dr), abs(dg), abs(db))
                    cur = lut.get((r, g, b))
                    if cur is None:
                        lut[(r, g, b)] = (rgb, d)
                    elif d < cur[1]:
                        lut[(r, g, b)] = (rgb, d)
                        clash += 1
                    elif cur[0] != rgb:
                        clash += 1
    return lut, clash


def scan_ref(shots_dir, ref_dir=None, css_path=None, only=None, per_screen=3):
    """정본이 못박은 **면 색**을 화면마다 훑어 «정본 ≠ 원작» 자리를 **기계가 먼저** 목록에 올린다(T512 걸음 ⓒ).

    사람이 하던 일의 순서를 뒤집는 것이 이 명령의 전부다 — 지금까지는 **사람이 눈으로 결함을 찾고**
    정본 CSS 를 열어서야 «클론이 맞았다» 를 알았다(검수 Q 47·51·52회차 · 다섯 자리 **전부** 그랬다).
    여기서는 **기계가 먼저** «원작과 다른 면 색» 을 세어 내고 사람은 그 목록을 **검산만** 한다.

    한계는 걸음 ⓑ 와 같다(결정 873) — 색이 그 선택자만 쓰는 색이 아니면 자리를 못 가린다.
    그래서 이 자는 «그 색이 어느 선택자의 자리인가» 를 **주장하지 않고**, 색과 자리만 찍고
    그 색을 못박은 선택자 후보를 같이 적는다. 고르는 것은 사람이다.
    """
    ref_dir = REF_DIR if ref_dir is None else ref_dir
    css_path = os.path.join(REPO, ".wwwww-src", "web", "css", "style.css") if css_path is None else css_path
    if not os.path.exists(css_path):
        print(u"✗ 정본 CSS 를 못 읽었다: %s" % css_path)
        return 1
    css = open(css_path, encoding="utf-8").read()
    table = face_color_table(css)
    if not table:
        print(u"✗ 정본 면 색 표를 못 세웠다(`tools/check_pinned_colors.py` 가 옆에 있어야 한다)")
        return 1
    print(u"· 정본이 면에 못박은 색 %d개(선언 %d개 · 토큰 푼 것 포함)를 화면마다 훑는다"
          % (len(table), sum(len(v) for v in table.values())))
    # T517 — 클론 화소를 **±VS_CLONE_TOL** 로 읽는다(종전엔 «바이트가 딱 맞을 때만» 이라 17개가 통째로 안 보였다).
    lut, clash = clone_lut(table)
    print(u"  · 클론 화소는 **±%d** 로 읽는다(T517 — 유니티가 못박은 색을 1~2 어긋나게 그린다) · 펼친 열쇠 %d칸%s"
          % (VS_CLONE_TOL, len(lut), u"" if not clash else u" · 두 색이 다툰 열쇠 %d칸(가까운 쪽이 쥔다)" % clash))
    hot, cold, flat, seen = [], [], [], 0
    for screen, ref_rel_path in pairs():
        if only and screen not in only:
            continue
        cp = os.path.join(shots_dir, "screen_%s.png" % screen)
        if not (ref_rel_path and os.path.exists(cp)):
            continue
        rp = os.path.join(ref_dir, ref_rel_path)
        if not os.path.exists(rp):
            continue
        clone, ref = png_read(cp), png_read(rp)
        seen += 1
        ax, ay, aw, ah = app_box(clone)
        buckets, offs = {}, {}
        for y in range(ay, ay + ah):
            row = y * clone.w * 3
            for x in range(ax, ax + aw):
                i = row + x * 3
                got = lut.get((clone.px[i], clone.px[i + 1], clone.px[i + 2]))
                if got is not None:
                    buckets.setdefault(got[0], []).append((x, y))
                    # T517 — «어느 바이트로 맞았는가» 를 같이 쥔다(0 = 딱맞음 · 그 수가 0 이면 종전 자는 이 색을 **못 봤다**).
                    o = offs.setdefault(got[0], [0, 0])
                    o[0] += 1 if got[1] == 0 else 0
                    if got[1] > o[1]:
                        o[1] = got[1]
        rax, ray, raw_, rah = app_box(ref)
        rows, spread = [], []
        for rgb, hit in buckets.items():
            if len(hit) < VS_MIN_BLOB:
                continue
            comps = [c for c in _clusters(hit) if len(c) >= VS_MIN_BLOB]
            # ⚑ 3회차 실측 — 이 금이 없으면 목록이 `#000000` 으로 덮인다(정본이 그 색을 **14 선택자**에
            #   못박았고 테·글자 외곽·딤이 전부 그 색이다 · `settings` 한 화면에 덩어리 수십 개).
            #   그 색은 «어느 선택자의 자리» 를 못 가리므로(결정 873) 세기만 하고 목록에서 내린다.
            if len(comps) > VS_BLOB_NOISE:
                spread.append((rgb, len(comps), len(hit)))
                continue
            for comp in comps:
                step = max(1, len(comp) // 120)
                vals = []
                for x, y in comp[::step]:
                    px, py = app_rel(clone, x, y)
                    rx, ry = int(round(rax + px / 100.0 * raw_)), int(round(ray + py / 100.0 * rah))
                    if 0 <= rx < ref.w and 0 <= ry < ref.h:
                        vals.append(_px_at(ref, rx, ry))
                med = _median_rgb(vals)
                if med is None:
                    continue
                d = max(abs(med[c] - rgb[c]) for c in range(3))
                if d <= VS_TOL:
                    continue
                xs, ys = [q[0] for q in comp], [q[1] for q in comp]
                x0p, y0p = app_rel(clone, min(xs), min(ys))
                x1p, y1p = app_rel(clone, max(xs), max(ys))
                ex = offs.get(rgb) or [0, 0]
                rows.append((d, len(comp), rgb, med, (x0p, x1p, y0p, y1p), ex[0], ex[1]))
        # 갈래를 **먼저** 가르고 갈래마다 상한을 둔다 — 안 그러면 넓은 어둠 자리가 색 갈래를 밀어낸다
        # (3회차에 실제로 그랬다: 판정의 토글 자리는 482화소라 `#000000` 수천 화소에 가려 안 보였다).
        h = [r for r in rows if _chroma(r[2]) >= CHROMA_HUE or _chroma(r[3]) >= CHROMA_HUE]
        c = [r for r in rows if r not in h]
        h.sort(key=lambda t: (-t[1], -t[0]))
        c.sort(key=lambda t: (-t[1], -t[0]))
        hot.extend((screen,) + r for r in h[:per_screen])
        cold.extend((screen,) + r for r in c[:per_screen])
        if spread:
            spread.sort(key=lambda t: -t[2])
            flat.append((screen, spread))
    print(u"· 훑은 화면 %d개 · **색 갈래**(둘 중 하나가 유채색) %d자리 · 어둠·판 갈래 %d자리"
          % (seen, len(hot), len(cold)))
    # T517 ⓑ — **훑은 화면이 0 이면 운다.** 종전엔 «훑은 화면 0개 · (없다)» 를 **rc 0** 으로 내서
    #   `--shots` 를 안 준 회차(기본값 `ui-screens/` 는 저장소에 없다)가 «깨끗하다» 로 읽혔다 — 전수가 0 인 것과 깨끗한 것은 다른 말이다.
    if seen == 0:
        print(u"")
        print(u"⛔ **훑은 화면이 0 개다 — 이것은 «깨끗하다» 가 아니라 «아무것도 안 봤다» 다**(T517).")
        print(u"   까닭 후보: ⓐ `--shots <자리>` 를 안 줬다(기본값 `%s` 는 저장소에 없다 — `origin/screens` 를 내려 쓴다)"
              % os.path.relpath(shots_dir, REPO))
        print(u"   ⓑ 그 자리에 `screen_*.png` 가 없다 ⓒ `--only` 가 짝 없는 이름이다 ⓓ 원작 샷(`%s`)이 없다."
              % os.path.relpath(ref_dir, REPO))
        return 1
    print(u"")
    print(u"  ── 색 갈래 — 먼저 봐라(원작과 **색이 다른** 면) ──")
    _scan_rows(hot, table)
    print(u"")
    print(u"  ── 어둠·판 갈래 — 대부분 **딤 .988 ↔ .5**(`--notes` 다섯째 줄)이 뿌리다 ──")
    _scan_rows(cold, table)
    print(u"")
    print(u"  ── 화면에 널린 색 — 자리를 못 가린다(결정 873 · 세기만 한다) ──")
    if not flat:
        print(u"    (없다)")
    for screen, spread in flat[:6]:
        print(u"    %-16s %s" % (screen, u" · ".join(
            u"#%02x%02x%02x 덩어리 %d" % (r[0][0], r[0][1], r[0][2], r[1]) for r in spread[:4])))
    print(u"")
    print(u"  ⚑ 이 목록은 **후보**다 — 자리 하나하나는 `--vs <화면> <선택자>` 로 다시 보고,"
          u" «정본 ≠ 원작» 이 확인되면 `--notes` 에 한 줄(근거 칸까지)을 더해라(걸음 ⓐ).")
    print(u"  ⚑ **후보의 많은 수는 «색이 다르다» 가 아니라 «자리가 밀렸다» 다**(3회차 실측 · 결정 877):"
          u" 그 겹이 원작에도 있는데 클론에서 조금 옮겨 서면, 내가 옮긴 상자가 원작의 **옆 것**을 짚는다"
          u"(예: `profile` 의 `#005dff` ↔ 원작 흰색 = T432 가 이미 아는 랭킹 버튼 폭). 가르는 길 둘 —"
          u" ⓐ 원작 샷을 **눈으로** 그 %W/%H 자리에서 보고 ⓑ 그 색이 원작 **어딘가에는** 있는지 본다"
          u"(아예 없으면 «색이 다르다» 가 거의 확실하다).")
    return 0


#: T519 — «차가 이만큼 크면 열 줄에 못 들어도 한 줄로는 반드시 찍는다»(검수 Q 55회차: 차 255 가 아홉 · 200~249 가 열둘).
SCAN_LOUD_DIFF = 200


def _note_needles():
    """`--notes` 근거 칸의 바늘 — [(줄 번호(1부터), 바늘 소문자)] (T519 ⓑ)."""
    out = []
    for i, (short, _body) in enumerate(STALE_REF_NOTES, 1):
        for _path, _line, needle in STALE_REF_EVIDENCE.get(note_head(short), ()):
            out.append((i, (needle or u"").lower()))
    return out


def _note_hit(sels, needles=None):
    """못박은 선택자 중 하나가 `--notes` 근거 바늘 안에 들어 있으면 그 줄 번호 · 없으면 None.

    T519 ⓑ — 이미 적어 둔 자리는 **매 회차 같은 칸을 먹지 않게** 뒤로 민다. 바늘은 정본 원문
    조각(`.settings-toggle.on { background: var(--pp-blue); }`)이라 선택자가 그 안에 든다."""
    needles = _note_needles() if needles is None else needles
    for sel in sels:
        key = (sel or u"").strip().lower()
        if len(key) < 3:
            continue
        for i, needle in needles:
            if key in needle:
                return i
    return None


def _scan_pick(rows, table, limit=10):
    """찍을 줄을 고른다 — **한 잣대가 아니라 둘**(T519 ⓐ).

    종전엔 `sorted(key=(-화소, -차))[:10]` 이라 «얼마나 넓은가» 하나로 잘랐고, 그래서 **차 255 짜리
    아홉이 한 줄도 안 찍히는** 일이 났다(검수 Q 55회차 실측 · 런 1382 · `settings` 토글은 화소순 33위
    ↔ 차순 2위). 넓이순 절반 · 차순 절반을 뽑아 합치고(겹치면 한 번), 어느 잣대로 뽑혔는지 적는다.
    ⚠ 차순으로 **갈아타지** 않는다 — 차 255 중에도 아는 «자리가 밀렸다»(결정 877)가 있어서,
    한 잣대로 바꾸면 반대쪽이 똑같이 잘린다.

    돌려주는 것: [(row, 뽑힌 까닭, --notes 줄 번호 또는 None)] · 그리고 못 찍은 수(fresh/noted)."""
    needles = _note_needles()
    marked = [(r, _note_hit([s for s, _l in table.get(r[3], [])], needles)) for r in rows]
    fresh = [t for t in marked if t[1] is None]
    noted = [t for t in marked if t[1] is not None]
    half = max(1, limit // 2)
    chosen, why = [], {}

    def take(pool, key, label, want):
        got = 0
        for t in sorted(pool, key=key):
            if got >= want:
                break
            k = id(t[0])
            if k in why:
                why[k] = why[k] + u"·" + label        # 두 잣대 다 뽑았다
                continue
            why[k] = label
            chosen.append(t)
            got += 1

    by_px = lambda t: (-t[0][2], -t[0][1])
    by_diff = lambda t: (-t[0][1], -t[0][2])
    take(fresh, by_px, u"넓이", half)
    take(fresh, by_diff, u"차", half)
    if len(chosen) < limit:                              # 남으면 아직 안 적힌 자리부터
        take([t for t in fresh if id(t[0]) not in why], by_px, u"남은자리", limit - len(chosen))
    if len(chosen) < limit:                              # 그래도 남으면 이미 적힌 자리
        take(noted, by_px, u"남은자리", limit - len(chosen))
    chosen.sort(key=lambda t: (t[1] is not None, -t[0][1]))
    rest_fresh = len([t for t in fresh if id(t[0]) not in why])
    rest_noted = len([t for t in noted if id(t[0]) not in why])
    return [(t[0], why[id(t[0])], t[1]) for t in chosen], rest_fresh, rest_noted


def _scan_rows(rows, table, limit=10):
    if not rows:
        print(u"    (없다)")
        return
    picked, rest_fresh, rest_noted = _scan_pick(rows, table, limit)
    for (screen, d, n, rgb, med, box, exact, off), why, note in picked:
        sels = [s for s, _l in table.get(rgb, [])]
        # T517 — «어느 바이트로 맞았는지» 를 같이 찍는다: 딱맞음 0 이면 **종전 자는 이 자리를 아예 못 봤다**.
        how = (u"딱맞음 %d" % exact) if exact else (u"**딱맞음 0** · ≤%d 로만 찍혔다(T517 앞에는 안 보였다)" % off)
        tail = u"%s · 뽑힘 %s" % (how, why)
        if note is not None:
            tail += u" · **이미 적힌 자리**(`--notes` %d번째 — 다시 파지 마라)" % note
        print(u"    %-16s #%02x%02x%02x ↔ 원작 (%d,%d,%d) · 차 %3d · %5d화소 · x %.1f~%.1f%%W y %.1f~%.1f%%H · %s"
              % ((screen,) + rgb + med + (d, n, box[0], box[1], box[2], box[3], tail)))
        print(u"        그 색을 못박은 정본 선택자 %d개: %s"
              % (len(sels), u" · ".join(sels[:4]) + (u" …" if len(sels) > 4 else u"")))
    # T519 — **접은 자리를 통째로 묻지 않는다.** 열 줄은 «아직 안 적힌» 자리에 주되,
    #   ⓐ 차가 큰데 못 든 자리와 ⓑ 이미 적힌 자리는 **한 줄짜리 꼴**로 반드시 선다
    #   (등재문 판정: «차 255 짜리 아홉이 한 줄도 안 빠지고 어딘가에 선다» · «`settings` 토글 자리가 다시 찍힌다»).
    shown = set(id(r) for r, _w, _n in picked)
    needles = _note_needles()
    def brief(r, note):
        screen, d, n, rgb, med = r[0], r[1], r[2], r[3], r[4]
        return u"%s #%02x%02x%02x↔(%d,%d,%d) 차 %d · %d화소%s" % (
            (screen,) + rgb + med + (d, n, u" [`--notes` %d]" % note if note else u""))
    hit = dict((id(r), _note_hit([x for x, _l in table.get(r[3], [])], needles)) for r in rows)
    # 아직 안 적힌 자리만 여기 싣는다 — 적힌 자리는 바로 아래 제 줄에 선다(한 자리가 두 줄에 겹치지 않게).
    loud = [r for r in rows if id(r) not in shown and not hit[id(r)] and r[1] >= SCAN_LOUD_DIFF]
    loud.sort(key=lambda t: (-t[1], -t[2]))
    if loud:
        print(u"    · **차 %d 이상인데 위 열 줄에 못 든 자리 %d개**(한 줄도 묻지 않는다 · T519): %s"
              % (SCAN_LOUD_DIFF, len(loud),
                 u" | ".join(brief(r, None) for r in loud[:12]) + (u" …" if len(loud) > 12 else u"")))
    noted_rest = [r for r in rows if id(r) not in shown and hit[id(r)]]
    noted_rest.sort(key=lambda t: (-t[1], -t[2]))
    if noted_rest:
        print(u"    · 이미 적힌 자리 %d개(칸을 안 먹지만 **사라지지도 않는다** · 다시 파지 마라): %s"
              % (len(noted_rest),
                 u" | ".join(brief(r, hit[id(r)]) for r in noted_rest[:8])
                 + (u" …" if len(noted_rest) > 8 else u"")))
    if rest_fresh or rest_noted:
        print(u"    · … %d자리 더(그중 %d 은 `--notes` 에 이미 적힌 자리다)" % (rest_fresh + rest_noted, rest_noted))


def verify_stale_notes(web_dir=None):
    """정본 실물에서 근거를 검산한다 — 정본 사본이 없으면 **건너뛴다**(rc 0)."""
    web_dir = os.path.join(REPO, ".wwwww-src", "web") if web_dir is None else web_dir
    if not os.path.isdir(web_dir):
        print(u"· 근거 검산: 정본 사본(.wwwww-src)이 없어 건너뛴다"
              u" — ROUTINE §0 의 `git clone --depth 1 … .wwwww-src` 뒤에 다시 돌려라")
        return 0

    def reader(path):
        p = os.path.join(web_dir, path)
        try:
            return open(p, encoding="utf-8").read()
        except Exception:
            return None
    bad = evidence_problems(reader)
    hard = [b for b in bad if not b.startswith(u"⚠")]
    n = sum(len(v) for v in STALE_REF_EVIDENCE.values())
    if not bad:
        print(u"✓ 근거 검산: %d줄 중 %d줄에 기계가 읽는 근거 %d개 · 정본에 **전부 살아 있다**"
              % (len(STALE_REF_NOTES), len(STALE_REF_EVIDENCE), n))
        return 0
    for b in bad:
        print(u"  %s %s" % (u" " if b.startswith(u"⚠") else u"✗", b))
    print(u"%s 근거 검산: 근거 %d개 중 사라진 것 %d · 줄 번호만 낡은 것 %d"
          % (u"✗" if hard else u"·", n, len(hard), len(bad) - len(hard)))
    return 1 if hard else 0


# 자리를 **견줄 수 없는** 화면 — 원작 샷과 정본이 그 화면에서 서로 다른 물건을 그린다(T28 71·81회차 전수 실측).
# «다음 볼 화면» 에서 **지우지 않고 맨 뒤로 밀며 꼬리표를 달아 둔다** — 지우면 진짜 결함이 생겨도 안 보이고,
# 그대로 두면 달성률이 낮다는 이유로 회차마다 같은 자리를 다시 파게 된다(78·81회차가 실제로 그러였다).
# ── «여기까지는 이미 재 봤다» (T28 131회차 · 워커 M) ────────────────────────────
# «다음 볼 화면» 은 달성률만 보고 줄을 세우므로, **이미 상자를 다 재서 오차가 자 안에 든 화면**도
# 계속 맨 앞에 선다. 그러면 워커마다 같은 상자를 다시 재고 «등재할 것이 없다» 로 끝난다.
# 그래서 «무엇을 어디까지 쟀고 얼마가 남았나» 를 화면별로 한 줄 적는다 — **뒤로 미는 것이 아니다**
# (`STALE_SCREENS` 와 다르다). 새로 재서 다른 수가 나오면 그 줄을 고치면 된다.
MEASURED_NOTES = {
    u"offline": u"카드는 맞다(T28 137회차 · 런 1192): 정본이 제 CSS 에 원작 실측을 적어 뒀고(**255** `.offline-card { height: calc(var(--app-h) * .5504) }` «원본 카드 바깥 높이 **55.04%H**(22.59~77.63)» · **256** «원본 카드 상단 **22.93%H**») 클론이 **위끝 22.50 · 높이 54.69** 로 그 값이다. 어둠↔흰 경계 46.23 ↔ 46.35 · 흰 절 아래끝 77.39 ↔ 77.08 도 ±0.4 안. 🚫 **원작 샷에서 «어두운 판» 을 색으로 집지 마라** — 이 샷의 딤은 α .988 이라 카드 **밖**도 거의 새카맣다. 그 자로 재면 카드 위끝이 14.96%H 로 나와 «−7.4%H» 라는 없는 결함이 생긴다(137회차가 그렇게 헛짚고 버렸다)",
    u"tech-node": u"가로 상자는 **다 쟀고 T477 로 닫혔다**(등재 T28 141회차 · 검산 143회차 · 런 1213): 진행바 폭 원작 65.2 ↔ 클론 **65.93%W**(고치기 전 69.63) · "
                  u"진행바 좌 인셋 5.91 ↔ **5.19%W**(전 3.33) · 청동 원 지름 9.98 ↔ **10.00%W**(전 11.30) · 카드 폭 76.30 불변. **남은 것은 머리 좌 인셋 −0.83%p 하나**고 그것은 "
                  u"**정본이 제 셈을 틀린 자리**다(위 «원작 샷이 다른 자리»). 🚫 **점수로 되돌리지 마라** — 기하는 정본에 붙었는데 점수는 0.9 ↔ 0.9 로 안 움직였다. "
                  u"🚫 **[취소] 버튼이 없는 것과 파란 채움 0%% 를 결함으로 읽지 마라** — 앞은 정본이 일부러 없앤 것이고 뒤는 남은 시간이 정하는 **상태**다",
    u"skill-detail": u"셋을 쟀고 **T483 으로 등재했다**(T28 146회차 · 런 1224): 카드 **흰 면** 폭 원작 74.95 ↔ 클론 **73.70%W**(정본이 «원본 실측 371/495» 로 적은 그 수를 클론이 **테 포함** 상자에 먹인다 · T410 과 같은 병) · "
                      u"버튼 바깥 상자 원작 **7.95%H**(4.20rem) ↔ 클론 **5.10**(2.69rem · **−36%**) · 카드 높이 38.30 ↔ **37.19%H**(148회차 `card_white_box` · 옛 36.25 는 모서리에 잘린 수 · T483 뒤에도 같다). "
                      u"버튼은 [업그레이드]·[장착] **둘 다** 같은 수다(세로 단면 원작 70px ↔ 클론 49px). "
                      u"⚠ 버튼의 −2.85%p 중 **−0.7%p 만 클론 몫**이다 — 정본 계약(`1.02rem × 줄높이 + .68 × 2 + ol3 × 2` ≈ 3.07rem)과 원작(4.20rem) 사이 −2.1%p 는 정본이 이 버튼 높이를 원작에 안 맞춰 본 몫이다. "
                      u"🚫 [업그레이드] 글자가 흐린 것은 **상태**(올릴 수 없는 레벨)지 결함이 아니다",
    u"dungeon-detail": u"버튼은 **T481 로 닫혔다**(등재 T28 145회차 · 검산 147회차 · 런 1228): 버튼 바깥 상자 원작 7.80 ↔ 클론 **7.19%H**(고치기 전 6.35) · 라벨 줄 피치 원작 1.162 ↔ **1.181rem**(전 1.484). "
                        u"카드 폭 69.28 ↔ **69.63%W** · 위끝 40.79 ↔ **40.94%H** · 보상 알약 54.42 ↔ **54.07%W** — 다 맞다. "
                        u"**남은 것은 흰 면 높이 −0.81%p 하나**(원작 34.35 ↔ 클론 33.54 · 148회차가 `card_white_box` 로 다시 쟀다 — 옛 33.02 는 모서리에 잘린 수다)고, 두 카드가 다 `min-height`(정본 5279 `.dgd-card { calc(var(--app-h) * .496) }`)에 붙어 있으니 그 차는 **그 위 배너(hero) 높이** 몫이다 — T358(✂) 갈래. "
                        u"🚫 열쇠 줄의 «0/2 ↔ 2/2» 는 **상태**지 결함이 아니다",
    u"ascend": u"카드 상자는 **다 쟀고 맞다**(T28 142회차 · 런 1209 · 앱 상자 기준): 카드 위끝 28.93 ↔ 28.75%H · 높이 39.01 ↔ 39.27 · 폭 73.49 ↔ 73.70%W · "
               u"줄 넷 높이 3.40 ↔ 3.49 · 피치 5.10 ↔ 5.21 · 안내 두 줄·닫기 버튼·카드 아래끝 전부 **±0.5%p 안**이다. 제목 글자도 T478 로 닫혔다(144회차 검산 · 런 1216): "
               u"«승천» 잉크 폭 원작 6.98 ↔ **6.85%W** · 높이 1.83 ↔ **1.88%H**(고치기 전 7.04 / 1.98 — 세로 +8.0%%가 +2.3%%로). 남은 +2.3%%는 아는 글자 단 근사다(1.15rem ↔ Button 1.209rem). "
               u"🚫 **이 화면 원작 짝은 `ref/shots/` 기기 샷이다(430×860 · 위아래 레터박스 48px)** — `app_box` 를 끝점으로 읽으면 밑변이 6.7%% 작아져 «카드가 −2.35%%H 짧다» 는 없는 결함이 난다. 반드시 `app_rel()` 로 재라. "
               u"🚫 **제목 글자를 «첫 어두운 덩어리» 로 잡지 마라** — 클론 ⭐ 아이콘 테에 `#080808` 이 있어 글자로 잡힌다(T478 등재문의 수가 그래서 틀렸다 · `ink_census` → `glyph_box(ink=…)` 로 재라)",
    u"main": u"장비 시트 상자는 다 쟀다(T28 131회차 · 런 1171): 패널 위끝 55.47 ↔ 55.52 · 칸 폭 13.43 ↔ 13.33%W · "
             u"칸 높이 7.78 ↔ 7.40 · 줄 틈 0.90 ↔ 1.15(**클론이 정본 `.6rem` 그대로**고 원작이 더 좁다) · 피치 8.68 ↔ 8.54 — "
             u"**남은 것은 다 0.4%H 아래**다. 이 화면 점수의 대부분은 **3D 세계 밴드**(위 «원작 샷이 다른 자리» 첫 항목)라 T35 전엔 안 움직인다",
    u"gear-detail": u"카드 상자는 T471 로 닫혔다(T28 130·131회차 · 런 1171): 위끝 59.84 ↔ 59.69 · 높이 17.65 ↔ 17.60 · "
                   u"아래끝 22.40 ↔ 22.60 · 리본 내밈 정본 043244 «8px(1.61%W)» ↔ 클론 **≈8px@540(1.5%W)**(T485 2회차가 리본 표의 옛 보정 −2.0 → −1.52rem 을 걷은 뒤 · 런 1254 · 종전 −0.37%W 는 안으로 들어가 있던 자리). 카드 **안**도 물건 덩어리 64.8~74.1 ↔ 64.4~74.1 로 **+0.4%H 안**이다 — "
                   u"남은 점수 차는 **뒤 3D 세계와 딤**이다. "
                   u"🚫 **`card_white_box` 로 이 카드를 재지 마라**(T28 149회차) — «장착됨» 리본이 왼쪽 벽에 걸쳐 원작은 리본 행을 **밖**으로, "
                   u"클론은 리본의 흰 테 때문에 **안**으로 센다(흰 면 위끝 63.35 ↔ 60.52%H 는 없는 결함) · 카드 위끝은 `card_top` 으로",
    u"craft-compare": u"버튼 줄은 **T485 로 닫혔다**(등재 T28 149회차 · 판정 런 1254): 버튼 바깥 폭 원작 27.9 ↔ 클론 **27.6%W**(고치기 전 24.6 · 정본 셈 27.3) · "
                      u"카드 흰 벽 → 회색 패널 원작 7px ↔ 클론 **4px**(전 14 · 정본 .25rem) · 패널 → 버튼 원작 11px ↔ 클론 **17px**(전 24 · 정본 .96rem) · 리본 내밈 ≈8px(정본 7) — "
                      u"카드 층 `Column` 이 `.cmp-wrap` 의 위 마진 .5rem 을 좌우에도 주던 것과 회색 패널의 .4rem 패딩(정본에 없음)을 걷었다. "
                      u"틈 1.3rem · 버튼 높이 6.73 ↔ 6.77%H · 카드 위끝 44.06%H(자 30/30 안)는 맞다. "
                      u"🚫 점수 4.0 → 1.8(런 1243)은 T415 18회차(버튼 모서리 .55 → .85rem · 런 1243 = 그 커밋)가 든 뒤 밴드가 다르게 쪼개진 것(18 → 13 · 6회차 폭 2.0 자리) — "
                      u"**점수로 되돌리지 마라** · 두 줄 버튼·«🪙 +N» 은 정본이 원작과 다른 자리다. "
                      u"🚫 `card_white_box` 로 이 카드 높이를 재지 마라 — «장착됨» 깃발이 왼쪽 벽에 걸쳐 원작은 깃발 행을 밖으로 센다(위끝 48.09 ↔ 44.27%H 는 없는 결함)",
}


STALE_SCREENS = {
    u"forge-list": u"원작 7행 ↔ 클론 8행(진행도 차) · 정본이 직접 «판정 대상이 아니다» 라 적었다(T28 70·71회차)",
    u"pet-upgrade": u"원작 «선택 알 + 밑줄 5칸» ↔ 정본 «등급별 칩 줄» · 원작 22.6~33.2%H 가 통째로 다른 물건이다(T28 81회차)",
}


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
            # T512 1회차 — 그 줄이 댄 정본 근거를 **기계가 읽는 칸**에서 나란히 찍는다(`--verify-notes` 가 검산하는 그 칸).
            for path, line, needle in STALE_REF_EVIDENCE.get(note_head(short), ()):
                print(u"        · 근거: %s%s  `%s`"
                      % (path, (u":%d" % line) if line else u"", needle))
    if full:
        print(u"")
        print(u"  (근거 칸이 붙은 줄 %d/%d · 바늘 %d개 — `--verify-notes` 가 정본에서 그것이 아직 산지 본다)"
              % (len(STALE_REF_EVIDENCE), len(STALE_REF_NOTES),
                 sum(len(v) for v in STALE_REF_EVIDENCE.values())))


# ── 회차 사이 점수 기준선 (T28 7회차 · 워커 M) ─────────────────────────────
# 문턱 0.5 의 근거(T28 22회차 실측 · 런 208 30장 × 흔들림 4가지 = 120회): UI 를 안 바꾸는
# 흔들림(밝기 ±1 · 세로/가로 1px 밀림)만 주면 |Δ| 중앙값 0.00 · 90분위 0.11 · 최대 0.49 다
# — 즉 0.5 는 판독 잡음 바로 위다. 이 문턱 자체는 낮추지도 올리지도 않는다.
# ⚠ **«최대 0.49» 는 인공 흔들림에서만 맞다**(T28 48회차 정정). 진짜 런 넷(413·425·438·449)에서
# «지문이 거의 같은»(안·뒤 둘 다 < FP_SAME) 짝 **79개**를 재니 중앙값 0.00 · 90분위 0.1 로 위 값과 같은데
# **최대는 3.1** 이고 0.5 를 넘은 짝이 셋이다 — 그 셋이 **전부 `settings` 한 화면**이다
# (밴드가 13↔16 으로 갈려 점수가 3.6 ↔ 6.8 을 오간다 · 그림은 그대로). 꼬리가 긴 분포이고
# 그 꼬리는 «모든 화면이 조금씩 흔들린다» 가 아니라 **한 화면이 크게 흔들린다** 이다.
# 그래서 문턱을 올리는 대신(그러면 진짜 회귀를 놓친다) 아래 SHAKY_SPREAD 로 그 화면을 **이름 대어 알린다**.
DROP_MARK = 0.5   # 이만큼 움직이면 사람이 봐야 한다(인공 잡음 90분위 0.11 · 실제 런 꼬리는 SHAKY 줄이 짚는다)
# 자취(최근 HIST_KEEP 회차)의 최대−최소가 이만큼이면 «회차마다 크게 흔들리는 화면» 으로 이름을 댄다.
# 실측(런 449 기준 자취 6회차): `settings` 3.2(3.6~6.8) 가 유일하게 넘는다.
#
# 🚫 **판독기 쪽에서 고치려다 두 갈래를 버렸다 (T28 51회차 · 런 492 자료 · 되풀이하지 마라)**
#   흔들림의 자리는 `_split` 의 문턱이다 — 골짜기는 «그 구간 중앙값의 **0.75배** 이하» 여야 쪼갠다.
#   `settings` 에는 y 26% 에 비 **0.754** 인 골짜기가 있다(문턱에서 0.004 떨어져 있다).
#   ⓐ **문턱까지의 거리를 재서 «칼날 위» 를 알린다** → 버렸다. 거리로 줄을 세우면 `player-info` **0.0003**
#      이 `settings` **0.0036** 보다 앞서는데 `player-info` 는 안 흔들린다 — 가르지 못한다.
#   ⓑ **문턱을 ±0.01 흔들어 점수가 얼마나 움직이나 본다**(30장 전수) → 버렸다. 움직인 것은
#      `player-info` 1.0 하나뿐이고 `settings` 는 **0.1** 이다 — 그림 한 장에서는 문턱이 **안정적**이다.
#   즉 흔들림은 «상수가 칼날 위» 여서가 아니라 **회차마다 그림이 조금씩 달라져 골짜기가 문턱을 넘나드는 것**이다.
#   어떤 상수를 골라도 그 넘나듦은 남고, 문턱을 바꾸면 30장의 점수와 자취가 통째로 무효가 된다.
#   그래서 판독기는 그대로 두고 **자취로 이름을 대는 것(SHAKY)** 이 이 문제의 답이다.
SHAKY_SPREAD = 1.5


def _flaps(vals):
    """자취가 «오르내리는가» — 한쪽으로만 내려간 **계단**은 흔들림이 아니다 (T28 54회차).

    48회차의 첫 판은 «자취 폭» 하나만 봤다. 그런데 런 508 에서 촬영 포스트 한 겹이 얹히자
    `dungeons`·`pets`·`tech-overview` 가 **한 번에 내려가 그대로 머물렀고**(방향 바뀜 0),
    폭만 보는 자는 그 셋을 «흔들린다» 로 불러 목록을 넷으로 불렸다 — 계단은 임자를 찾아야 할
    **진짜 변화**이고, 흔들림은 그 반대다. 그래서 **의미 있는 차(±DROP_MARK 밖)의 방향이
    한 번이라도 뒤집혀야** 흔들림으로 본다. 실측(런 528 자취 6회차):
    `settings` [6.0 6.0 4.5 4.7 3.7 6.8] 방향 바뀜 **1** → 흔들림 ·
    `dungeons` [9.3 9.3 8.6 8.6 8.6 5.8] 방향 바뀜 **0** → 계단(T341 포스트 한 겹)."""
    sign = []
    for i in range(len(vals) - 1):
        d = vals[i + 1] - vals[i]
        if d >= DROP_MARK:
            sign.append(1)
        elif d <= -DROP_MARK:
            sign.append(-1)
    return any(sign[i] != sign[i + 1] for i in range(len(sign) - 1))

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
    el = {}
    for k, v in (d.get("elems") or {}).items():
        try:
            el[k] = int(v)
        except (TypeError, ValueError):
            pass
    out["_elems"] = el
    # T502 — 앞 런 PNG 의 **바이트** 지문. 없으면 빈 dict(낡은 기준선과 섞여도 안 깨진다).
    sh = {}
    for k, v in (d.get("shot_sha") or {}).items():
        if isinstance(v, str) and len(v) == 64:
            sh[k] = v.lower()
    out["_sha"] = sh
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


# ── PNG **바이트** 지문 (T502) ────────────────────────────────────────────
# ⚠ 아래 `fingerprints`(fp_pack)와 **다른 것**이다. 그쪽은 이 자가 그림을 눌러 만든 **지각 지문**이고,
#   지금 의심받는 자(T437·T444: «판독기가 같은 그림을 회차마다 다르게 쪼갠다»)의 눈금이다.
#   이쪽은 CI 가 `sha256sum` 으로 뜬 **파일 바이트**라 자가 끼어들 자리가 없다 — 그래서 «그림이 달라졌나» 를
#   가르는 자리에서는 이쪽이 판결이고 지각 지문은 참고다.
SHA_LIST = "shots-sha256.txt"


def parse_sha_list(text):
    """`<sha256>  screen_<이름>.png` 줄들 → {화면 이름: sha}. 꼴이 틀린 줄·PNG 아닌 줄은 조용히 넘긴다."""
    out = {}
    for ln in (text or "").splitlines():
        ln = ln.strip()
        if not ln or ln.startswith("#"):
            continue
        parts = ln.split()
        if len(parts) < 2:
            continue
        sha, fn = parts[0], parts[-1]
        if len(sha) != 64 or not all(c in "0123456789abcdefABCDEF" for c in sha):
            continue
        fn = os.path.basename(fn.lstrip("*"))          # `sha256sum -b` 는 이름 앞에 `*` 를 붙인다
        if not fn.lower().endswith(".png") or not fn.startswith("screen_"):
            continue
        out[fn[len("screen_"):-len(".png")]] = sha.lower()
    return out


def sha_split(old_sha, new_sha, names):
    """(바이트 같은 화면, 다른 화면, 한쪽이라도 모르는 화면) — 이름 오름차순. 어느 쪽이 비면 전부 «모른다»."""
    same, diff, unknown = [], [], []
    for n in sorted(names):
        a, b = (old_sha or {}).get(n), (new_sha or {}).get(n)
        if not a or not b:
            unknown.append(n)
        elif a == b:
            same.append(n)
        else:
            diff.append(n)
    return same, diff, unknown


def fp_quiet_moved(base, cur, old_fp, new_fp, mark=None, same=None):
    """T514 — «**지각 지문**은 그대로인데 점수가 움직인 화면» [(이름, 전, 후, 안, 뒤)] — 점수 변화 큰 것부터.

    T502 의 바이트 장부로는 이 증거가 **안 잡힌다**(채점 화면은 실측 0/31 로 런마다 바이트가 다르다 — 3D 하늘·연출·글자 AA).
    그래서 같은 뜻을 **쓸 수 있는** 잣대로 센다: 지문 차가 안·뒤 **둘 다** `FP_SAME` 아래면 «그 자리는 안 달라졌다» 이고,
    그때 점수가 `DROP_MARK` 넘게 움직였으면 범인은 클론이 아니라 **자**다(T437·T444 가 찾던 자리).
    """
    mark = DROP_MARK if mark is None else mark
    same = FP_SAME if same is None else same
    out = []
    names = set(cur) & set(n for n in (base or {}) if not n.startswith("_"))
    for n in sorted(names):
        din, dout = fp_diff((old_fp or {}).get(n), (new_fp or {}).get(n))
        if din is None or dout is None or din >= same or dout >= same:
            continue
        if abs(cur[n] - base[n]) >= mark:
            out.append((n, base[n], cur[n], din, dout))
    out.sort(key=lambda t: -abs(t[2] - t[1]))
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


def drop_note(din, dout, med, hist_n, cur_v, prev_v, bw=None, bg=0.0):
    """내려간 화면 한 줄의 «왜» — `None` 을 돌려주면 **hard**(진짜 회귀)다.

    ⚠ **차례가 뜻을 정한다**(T28 90회차). 옛 자는 «최근 N회차 중앙값 자리다» 를 «그림이 거의
    같다» 보다 **먼저** 봤다. 둘 다 참일 때 사람이 받는 말은 «지난 회차가 튀었던 것이다» 하나뿐이고,
    그 말은 **자가 흔들렸다는 사실을 감춘다**. 실측 — 런 921 → 935(그림은 930)에서 `profile` 은
    화소 차 **0.64%** · 지문 차 **안 0.15 · 뒤 0.10**(서른한 장 중 **가장 작다**)인데 점수가
    4.3 → 3.1 로 **−1.2** 떨어졌고 밴드가 **18 → 9** 로 갈렸다. 그런데 자가 찍은 문장은
    «중앙값 3.2 자리다» 였다 — 지문이 이미 «그림은 그대로다» 라고 말하고 있었는데도.
    그래서 지문 쪽을 먼저 보고, 중앙값도 참이면 **뒤에 덧붙인다**(둘 다 말한다).

    ⚠ 이 손질은 **soft/hard 판정을 안 바꾼다** — 옛 차례에서 중앙값이 잡던 칸은 전부 지문 갈래로
    옮겨가도 여전히 soft 다. 바뀌는 것은 **사람이 읽는 문장**뿐이다(점수식은 안 건드린다 · 결정 686).
    """
    if bw is not None:
        # 씬 대역이 통째로 붉으면 밴드가 녹아 붙는다 — 그림이 달라진 것은 맞지만 **UI 가 아니다**.
        return u"보스 경고 연출을 물고 찍혔다(씬 대역 %.0f%%) — 촬영이 물었다(T176)" % (bw * 100)
    on_med = med is not None and cur_v > med - DROP_MARK
    if din is not None and din < FP_SAME and dout < FP_SAME:
        # 그림은 사실상 같은데 점수만 움직였다 = 밴드 경계 하나가 걸린 것(자의 흔들림).
        why = u"그림이 거의 같다(안 %.1f · 뒤 %.1f) — 밴드 경계가 걸린 자의 흔들림이다" % (din, dout)
        if on_med:
            why += u" · 게다가 최근 %d회차 중앙값 %.1f 자리다" % (hist_n, med)
        return why
    if on_med:
        # 자취가 3회차 이상이면 «지난 회차» 가 아니라 **중앙값**과도 견준다 — 지난 회차 하나가
        # 튄 것을 «회귀» 로 부르지 않는다(T28 22회차 · forge-detail 1.9 가 그 꼴이었다).
        return u"최근 %d회차 중앙값 %.1f 자리다" % (hist_n, med)
    if din is not None and din < FP_SAME and dout > din:
        # 지문이 «팝업 안은 그대로 · 뒤만 달라졌다» 고 말하면 코드가 아닐 공산이 크다.
        return u"그림 안쪽은 그대로다(안 %.1f · 뒤 %.1f)" % (din, dout)
    if din is None and prev_v is not None and abs(cur_v - prev_v) <= bg:
        # 지문이 없는 첫 회차에만 쓰는 물러섬(실측 상한 표)
        return u"배경만으로도 ±%.1f 움직이는 화면(지문 없음)" % bg
    return None


def save_baseline_file(path, scores, avg, run=None, fps=None, bands=None, carried=False, ceilings=None, elems=None, shot_sha=None):
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
    # T500 — 같은 런을 두 번 적지 않으려는 거르기인데, `run` 이 None 이면 **번호 없는 옛 칸이 통째로 사라진다**.
    #   번호가 있을 때만 돌린다(번호 없는 칸끼리는 서로 안 지운다).
    if run is not None:
        hist = [h for h in hist if h.get("run") != run]
    ent = {"run": run, "avg": round(avg, 2), "screens": cur}
    if ceilings:
        # T28 87회차(결정 722) — 천장도 자취에 남긴다. 회차 사이에 **밴드 쪼개짐이 바뀌면 천장이 통째로 흔들려서**
        #   (런 859~898 전수: 31 화면 중 11 이 0.5 넘게 · `offline` 4.89 → 2.75 · `profile` 4.30 → 6.23)
        #   «한 회차의 천장» 으로 고르면 그 흔들림이 목록 맨 위를 뒤집는다.
        ent["ceilings"] = dict((k, round(v, 2)) for k, v in ceilings.items())
    if carried:
        # 이 런은 제 PNG 를 안 냈다 — 그림은 지난 런 것이다(T28 44회차). 자취에 그대로 남긴다.
        ent["carried"] = True
    hist.append(ent)
    d = {"run": run, "avg": round(avg, 2), "screens": cur, "history": hist[-HIST_KEEP:]}
    if ceilings:
        d["ceilings"] = dict((k, round(v, 2)) for k, v in ceilings.items())
    if carried:
        d["carried"] = True
    if fps:
        d["fingerprints"] = dict((k, fp_pack(v)) for k, v in fps.items())
    if bands:
        d["bands"] = dict((k, int(v)) for k, v in bands.items())
    if elems:
        # T28 98회차 — 짝짓기에 들어간 **요소 수**. 회차 사이에 이 수가 달라진 화면만 점수가 움직인다
        #   (아래 «판독기 흔들림» 알림의 근거 · 968 ↔ 1005 전수에서 예외 0).
        d["elems"] = dict((k, int(v)) for k, v in elems.items())
    if shot_sha:
        # T502 — CI 가 `shots-sha256.txt` 로 올린 **파일 바이트** 지문. 다음 회차가 이것과 대면
        #   «그림이 달라졌나(런 흔들림) ↔ 자가 달리 읽었나(판독기 결함)» 가 **운이 아니라 늘** 갈린다.
        d["shot_sha"] = dict((k, v) for k, v in shot_sha.items() if isinstance(v, str) and len(v) == 64)
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
    """그림에서 가장 큰 «밝은 판»(팝업 카드)의 **가로** 상자 — (x, w) %% · 못 찾으면 None.

    🚫 **이 값을 «카드의 좌우 변» 으로 그대로 믿지 마라**(T28 130회차 실측). 고르는 행이 «밝은 화소가 가장 많은
    한 행» 이라, 클론 딤이 α .5 인 탓에 **뒤 화면의 밝은 것**(장비 칸·버튼)이 그 행에서 카드 밖까지 이어지면
    상자가 통째로 넓어진다 — `gear-detail` 런 1171 에서 실제로 그랬다(이 함수 x 12.04 w 72.22 ↔ 카드 실물
    x 15.93 w 67.59 · 폭이 **+4.6%%p** 부풀었다). 카드 좌우를 재려면 **카드 세로 구간 안의 여러 행**에서
    «밝은 연속 구간» 을 잡아 **가장 자주 나오는 좌우 변**을 쓰고, 글자·✕ 가 행을 쪼개는 것을 함께 다뤄라.
    (`card_top` 이 세로에서 같은 함정을 다루는 방식과 짝이다.) 이 함수의 값은 «카드 안쪽을 훑을 x 범위» 로는
    충분하다 — 넓게 잡혀도 안쪽 셈이 크게 안 흔들리기 때문이고, 그 용도로만 쓴다.
    """
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


CARD_TOP_TOL = 1.5      # 카드 위끝 허용 차(%p · T28 114회차 실측: 30장 중 27장이 |Δ| ≤ 1.5 · 중앙값 0.56)
CARD_TOP_FRAC = 0.80    # 카드 가로 상자 안에서 이만큼이 «밝은 판» 이면 그 행은 카드다


def card_top(img):
    """팝업 카드의 **위끝**(앱 상자 기준 %%H) — 딤과 무관한 자. 못 찾으면 None.

    왜 «위끝» 만인가(T28 114회차 실측): 카드의 **아래끝·높이는 못 믿는다** — 밝은 행의 가장 긴
    구간으로 잡으면 카드 **안**의 어두운 띠(배너·목록·미리보기)에서 끊겨 `profile` −10.3 ·
    `player-info` −17.2 · `tech-node` +14.6 처럼 엉뚱한 높이가 나온다. 반면 **위끝**은 30장 중
    27장이 |Δ| ≤ 1.5%%p(중앙값 0.56)로 두 그림이 같은 값을 낸다.

    🚫 «그림 전체에서 밝은 화소를 세는» 꼴도 못 쓴다 — 클론 딤은 α .5 라 **뒤 화면이 비쳐**
    카드 위에서도 밝은 행이 나온다(`league-rewards` 가 그 탓에 −7.4 로 나왔다). 그래서
    `card_box()` 가 잡은 **카드 가로 상자 안**에서만 세고, 그 폭의 80% 이상이 밝아야 카드 행으로 본다.
    """
    b = card_box(img)
    if b is None:
        return None
    px, W, H = img.px, img.w, img.h
    L = int(W * b[0] / 100.0)
    R = int(W * (b[0] + b[1]) / 100.0)
    if R - L < 8:
        return None
    need = (R - L) * CARD_TOP_FRAC
    ax, ay, aw, ah = app_box(img)
    for y in range(H):
        n = 0
        base = y * W * 3
        for x in range(L, R):
            i = base + x * 3
            if px[i] >= CARD_THR and px[i + 1] >= CARD_THR and px[i + 2] >= CARD_THR:
                n += 1
        if n >= need:
            return (y - ay) * 100.0 / ah
    return None


# ── «이 띠가 뒤 화면이 비치는 것인가» (T28 57회차 · 워커 M · T358 ✂ 에서 배운 것) ─────
# 클론 딤은 주인 지시 α .5 라 팝업 화면에 **뒤 화면이 절반 밝기로 비친다**. 그것을 «팝업이 그린 것»
# 으로 잘못 읽으면 없는 결함을 등재하게 된다 — 56회차에 내가 그렇게 T358 을 냈고 워커 J 가 접었다.
# 그때 내가 한 검산은 «카드 밖 한 점(x 3%)이 통짜 딤이다» 였는데, 그 자리는 **뒤 화면의 빈 여백**이라
# 딤만 남는 자리였다. 한 점의 **부재**로는 아무것도 못 가린다.
# 옳은 검산은 **뒤로 의심되는 화면의 PNG 와 픽셀로 견주는 것**이다 — 워커 J 가 그렇게 갈랐다:
# 위 띠는 `screen_dungeons.png` 대비 밝기비 **0.467~0.489**(= .5 딤) · 아래 띠는 **2.17**(상관없음).
# 그래서 그 검산을 자에 넣는다: `--behind <앞> <뒤>` 가 가로줄마다 밝기비 중앙값을 찍는다.
BEHIND_LO, BEHIND_HI = 0.42, 0.58   # 이 사이면 «뒤 화면이 α .5 딤으로 비친다»


def behind_ratio(front, back, rows=24):
    """두 그림의 «가로줄 밝기비» 중앙값 목록 — (y%%, 비, 표본수). 비가 ≈0.5 면 뒤가 비치는 것이다."""
    out = []
    W, H = front.w, front.h
    if back.w != W or back.h != H:
        return out
    fg, bg = front.gray(), back.gray()
    for k in range(rows):
        y = int((k + 0.5) * H / rows)
        base = y * W
        rs = []
        for x in range(0, W, 4):
            b = bg[base + x]
            if b >= 24:                      # 뒤가 캄캄한 자리는 비가 의미 없다
                rs.append(fg[base + x] / float(b))
        if len(rs) >= 8:
            rs.sort()
            out.append((y * 100.0 / H, rs[len(rs) // 2], len(rs)))
    return out


def behind(shots_dir, front_name, back_name):
    """`--behind 앞 뒤` — 앞 화면의 각 가로줄이 뒤 화면을 딤으로 비친 것인지 수로 가른다."""
    fp = os.path.join(shots_dir, "screen_%s.png" % front_name)
    bp = os.path.join(shots_dir, "screen_%s.png" % back_name)
    for q in (fp, bp):
        if not os.path.exists(q):
            print(u"✗ 그림이 없다: %s" % q)
            return 2
    rows = behind_ratio(png_read(fp), png_read(bp))
    if not rows:
        print(u"✗ 두 그림의 크기가 다르다 — 같은 런의 같은 앱 상자여야 한다")
        return 2
    print(u"«%s» 의 가로줄이 «%s» 를 딤으로 비치는가 — 비가 %.2f~%.2f 면 그렇다(클론 딤 α .5)"
          % (front_name, back_name, BEHIND_LO, BEHIND_HI))
    for y, r, n in rows:
        mark = u"← 뒤가 비친다" if BEHIND_LO <= r <= BEHIND_HI else u""
        print(u"   y %5.1f%%  비 %5.2f  (표본 %d) %s" % (y, r, n, mark))
    hit = [t for t in rows if BEHIND_LO <= t[1] <= BEHIND_HI]
    print(u"— 뒤가 비치는 줄 %d / %d. **그 줄에 보이는 것은 앞 화면이 그린 것이 아니다**"
          u" — 등재하기 전에 이것부터 보라(T358 ✂)." % (len(hit), len(rows)))
    return 0


# ── 구분선 줄 대조 (T28 59회차 · 워커 M) ─────────────────────────────────
# 밴드 점수는 팝업·목록 화면에서 딤과 글자 모양에 눌려 «자리가 맞는데도» 3점대에 머문다.
# 그런데 목록 화면이 정말 어긋났는지는 **가로로 거의 한 색인 줄**(행 사이 여백·구분선)의 자리만
# 견주면 곧장 나온다 — 딤이 밝기를 반씩 깎아도 «한 색인가» 는 안 바뀌기 때문이다.
# 실측(런 578 `league-challenge`): 목록 다섯 줄의 구분선이 원작 40.5·50.2·60.1·70.0·79.8 ↔
# 클론 40.8·50.6·60.3·70.1·79.9 로 **Δ ≤ 0.6%p** 다 — 점수는 3.9 지만 **줄 자리는 맞다**.
# 그래서 «점수가 낮다» 를 «자리가 틀렸다» 로 읽지 않게 이 자를 따로 둔다.
SEP_X0, SEP_X1 = 0.16, 0.84   # 카드 안쪽만 본다(바깥은 딤)
SEP_SAME = 0.92               # 그 줄의 표본 중 이만큼이 같은 색이면 «구분선 줄»
SEP_MIN = 3                   # 이만큼 이어져야 한 줄로 센다(px)
SEP_TOL = 1.5                 # 원작 ↔ 클론 허용 어긋남(%p)
SHIFT_MAX = 6.0               # 이보다 멀면 «밀린 것» 이 아니라 아예 없는 줄로 본다
SHIFT_BIN = 0.5               # 밀린 크기를 이 단위로 묶어 «나란히» 를 찾는다
SHIFT_MIN = 2                 # 같은 크기로 이만큼 밀려 있으면 등재감으로 부른다


def app_box(img):
    """샷 안에서 **앱**(9:16)이 차지하는 상자 — `(x0, y0, w, h)`.

    🚨 원작 샷 30장은 **앱 크기가 아니다**(T28 68회차 전수): 세로/가로가 1.6112~1.8238 로 흩어져 있고
    9:16(1.7778)인 것은 하나도 없다. 정본 `main.js` `fitLayout()` 이
    `h = vh; w = h*9/16; if (w > vw) { w = vw; h = w*16/9 }` 라 **앱은 언제나 9:16 이고 나머지는 레터박스**다.
    그래서 `y / 샷높이` 로 재면 원작 쪽에만 최대 **2.5%p** 의 치우침이 들어간다 — 그것은 내가
    62~67회차에 «나란히 밀린 줄» 로 등재해 온 값(1.5~2.5%p)과 **같은 크기**다. 반드시 앱 상자로 재라.

    실측 치우침 큰 것부터: `chat` −10.34%H · `pass` +2.52 · `forge-list` +2.16 · `autoforge` +1.98 ·
    `pet-detail` +1.68 · `tech-branch` −1.67 · `settings` +1.66 … `craft-compare` +0.06(가장 작다).
    클론 샷은 540×960 = 정확히 9:16 이라 이 함수가 그림 전체를 돌려준다(달라지는 것이 없다).
    """
    W, H = img.w, img.h
    if H * 9 >= W * 16:                  # 세로가 길다 → 폭이 앱 폭 · 위아래 레터박스
        aw, ah = W, min(H, int(round(W * 16.0 / 9.0)))
    else:                                # 가로가 넓다 → 높이가 앱 높이 · 좌우 레터박스
        ah, aw = H, min(W, int(round(H * 9.0 / 16.0)))
    return ((W - aw) // 2, (H - ah) // 2, aw, ah)


def ink_census(img, x0, x1, y0, y1, n=6):
    """창 안 화소 색을 많은 차례로 `n` 개 — `[((r, g, b), 개수), …]`.

    **글자를 재기 전에 이것부터 쳐라.** 창 안에 무엇이 들었는지 모르고 «어두운 것 = 글자» 로
    잡으면 아이콘을 글자로 센다(아래 `glyph_box` 의 🚨 참고).
    """
    c = {}
    px, w = img.px, img.w
    for y in range(y0, y1):
        for x in range(x0, x1):
            i = (y * w + x) * 3
            k = (px[i], px[i + 1], px[i + 2])
            c[k] = c.get(k, 0) + 1
    return sorted(c.items(), key=lambda t: -t[1])[:n]


def glyph_box(img, x0, x1, y0, y1, ink=None, tol=12, ink_max=120, gray_tol=26):
    """창 안에서 **글자 잉크만**의 상자 `(L, R, T, B)` — 없으면 `None`.

    `ink=(r, g, b)` 를 주면 **그 색에서 `tol` 안**인 화소만 센다(권장 · 기본 `tol` 12 는 좁다 —
    이 자리의 글자 `#16161c` 와 아이콘 테 `#080808` 이 14 밖에 안 떨어져 있어 넓히면 다시 섞인다). 안 주면
    «어둡고(`ink_max` 아래) 무채색에 가까운(`max-min <= gray_tol`)» 화소를 센다.

    🚨 **«가장 왼쪽 어두운 덩어리» 를 글자로 삼지 마라 — 아이콘 테가 글자처럼 어둡다.**
    T28 142회차가 그렇게 승천 팝업 제목을 쟀다가 **원작의 «승» 글자 ↔ 클론의 ⭐ 아이콘**을
    나란히 놓고 «가로 +25% · 세로 +20%» 라는 수를 등재문에 적었다(T478). 144회차에 색을 세어
    보니 그 덩어리는 `#ffdf80`·`#804000`·**`#080808`**(별 아이콘과 그 테)이었고, 같은 자리의
    진짜 글자(`#16161c`)로 다시 재니 **가로 +2.7% · 세로 +11.5%** 였다. 결론(키라인이 잘못
    걸렸다)은 정본 선택자 목록에서 나온 것이라 살았지만 **수는 통째로 다시 적어야 했다**.

    ⚑ **무채색 걸러내기만으로는 못 막는다** — 그 별 테에는 `#080808`(무채색 검정)도 있다.
    그래서 규약은 둘이다: ⓐ 먼저 `ink_census` 로 창 안을 세고 ⓑ 글자 색을 골라 `ink=` 로 준다.
    ⚑ 원작 쪽 별은 `#fdd835`(순노랑)라 저절로 빠지는데 클론 별은 안 빠진다 — **한쪽만 걸리는
    덫**이라 두 수를 나란히 놓기 전엔 안 보인다.
    """
    px, w = img.px, img.w
    L = R = T = B = None
    for y in range(y0, y1):
        for x in range(x0, x1):
            i = (y * w + x) * 3
            r, g, b = px[i], px[i + 1], px[i + 2]
            if ink is not None:
                if abs(r - ink[0]) > tol or abs(g - ink[1]) > tol or abs(b - ink[2]) > tol:
                    continue
            else:
                if r > ink_max or g > ink_max or b > ink_max:
                    continue
                if max(r, g, b) - min(r, g, b) > gray_tol:
                    continue          # 색이 있다 = 아이콘 테·그림 — 글자가 아니다
            if T is None:
                T = y
            B = y
            if L is None or x < L:
                L = x
            if R is None or x > R:
                R = x
    return None if T is None else (L, R + 1, T, B + 1)


def card_white_box(img, cx0, cx1, th=200, r_rem=1.3):
    """카드 **흰 면**의 세로 구간 `(y0, y1)` — 둥근 모서리에 안 속는 자. 없으면 `None`.

    규칙 하나로 센다: **그 행의 흰 화소가 카드 왼쪽 벽에서 `r_rem` 안에서 시작하고 오른쪽 벽에서
    `r_rem` 안에서 끝나면 그 행은 카드 안이다.** 가운데를 가로지르는 내용(알약·버튼·글줄)은 상관없다 —
    카드 패딩이 좌우 끝을 늘 희게 남겨 두기 때문이다. 모서리 행도 호만큼 들어와 있을 뿐 양끝이 다 희다.

    🚨 **«테에서 몇 px 안쪽 한 열» 로 카드 높이를 재지 마라.** 그 열은 둥근 모서리에 잘린다 —
    반지름 r 인 모서리에서 벽에서 d 만큼 들어온 열은 위아래로 각각 `r − sqrt(r² − (r−d)²)` 씩 짧게 나온다.
    T28 145·146회차가 `cx1 − 6` 한 열로 재서 **클론 쪽만** −0.9%p 짧게 읽었고(원작 샷은 같은 자로도
    같은 값이 나와 **한쪽만 걸리는 덫**이었다) 그 수로 T481·T483 의 판정 창을 그었다. 148회차에 바로잡았다:
    skill-detail 카드 높이 클론 **36.25 → 37.19%H**(원작 38.30 그대로) · dungeon-detail **33.02 → 33.54**(원작 34.35).

    ⚑ 148회차가 **먼저 시도해서 버린 자 셋**(다시 하지 마라):
      ⓐ «가운데 22~78% 열의 최빈 흰 달리기» — 가운데는 내용이 가로질러 조각난다(15%H 가 나온다).
      ⓑ «행마다 흰 달리기가 카드 폭의 40% 이상» — 내용이 카드를 가로지르는 행에서 끊긴다.
      ⓒ «벽에서 3~45px 열들의 최대 달리기» — 값은 맞게 나오지만 **어느 열이 이겼는지에 기대는** 자라
         내용이 깊이 들어온 화면에서 조용히 모서리 값으로 돌아간다(이 함수와 같은 수를 내지만 근거가 약하다).

    🚫 **«장착됨» 깃발·리본이 카드 왼쪽 벽에 걸친 화면(gear-detail · craft-compare)에서는 이 자도 한쪽만 걸린다**(T28 149회차):
      깃발 행의 첫 흰 화소가 벽에서 r 밖이라 원작은 그 행을 **밖**으로 세는데, 클론은 깃발의 흰 테(`--pp-paper` 바탕)가
      벽 안에 있어 **안**으로 센다 — 실측 위끝 gear-detail 63.35 ↔ 60.52%H · craft-compare 48.09 ↔ 44.27%H, 둘 다 없는 결함이다.
      그런 화면은 `card_top` 으로 위끝을 재라. 자를 새로 세우면 짝 표의 화면 전부에 한 번 돌려 어느 화면이 걸리는지 먼저 적어라.
    """
    ax, ay, aw, ah = app_box(img)
    px, w = img.px, img.w
    r = int(r_rem * 0.018957 * ah)
    segs, s = [], None
    for y in range(ay, ay + ah):
        L = R = None
        for x in range(max(0, cx0 - 2), min(w, cx1 + 2)):
            i = (y * w + x) * 3
            if px[i] > th and px[i + 1] > th and px[i + 2] > th:
                if L is None:
                    L = x
                R = x
        inside = L is not None and L <= cx0 + r and R >= cx1 - 1 - r
        if inside and s is None:
            s = y
        elif not inside and s is not None:
            segs.append((s, y))
            s = None
    if s is not None:
        segs.append((s, ay + ah))
    return max(segs, key=lambda t: t[1] - t[0]) if segs else None


def app_rel(img, x=None, y=None):
    """픽셀 자리 → **앱 상자** 기준 백분율 `(%W, %H)`. 둘 중 하나만 주면 그쪽만 돌려준다.

    🚨 **`app_box` 는 `(x0, y0, w, h)` 를 돌려준다 — `(x0, y0, x1, y1)` 이 아니다.**
    이 둘을 헷갈리면 세로 자가 통째로 어긋난다. T28 142회차에 실제로 그랬다: `ascend` 의 원작 짝은
    **기기 샷**(430×860 · 위아래 레터박스 48px)이라 `app_box` 가 `(0, 48, 430, 764)` 인데, 끝점으로
    읽어 앱 높이를 `764 − 48 = 716` 으로 잡았다. 밑변이 **6.7% 작아지니** 원작 %H 가 전부 부풀어
    «카드가 −2.35%H 짧다 · 첫 행이 −3.12%p 높다» 는 **없는 결함**이 생겼다. 밑변을 바로잡으니
    같은 자리가 −0.18 · −0.47 로 전부 ±0.5%p 안이었다 — 등재 직전에 잡았다.

    ⚑ 원작 짝이 `ref/screens/shot-*.png`(앱 통짜 캡처)면 레터박스가 거의 없어 틀려도 안 드러난다.
    드러나는 것은 `ref/shots/*.png`(기기 화면 녹화 프레임)뿐이다 — 그래서 **늘 이 함수를 써라**.
    """
    ax, ay, aw, ah = app_box(img)
    if x is not None and y is not None:
        return ((x - ax) * 100.0 / aw, (y - ay) * 100.0 / ah)
    if x is not None:
        return (x - ax) * 100.0 / aw
    return (y - ay) * 100.0 / ah


def app_ratio(w, h):
    """그림 크기 `(w, h)` → **앱 상자**의 세로/가로. `app_box` 와 같은 셈이라 PNG 를 안 열어도 된다.

    틀을 볼 때 그림 통짜 비를 쓰면 안 된다(T393 3회차): 점수는 `app_box` 위에서 나는데 통짜 비는
    레터박스·기기 크롬까지 세므로 둘이 어긋난다. 실제로 `chat`(원작 1.611)과 `ascend`(정본 `ref/shots`
    기기 샷 2.000)가 통짜 비로는 «틀 불일치» 로 울었지만 앱 상자로는 각각 1.7788·1.7767 로 9:16 이다."""
    ax, ay, aw, ah = app_box(_WH(w, h))
    return ah / float(aw)


class _WH(object):
    """`app_box` 는 `w`·`h` 만 본다 — 크기만 들고 가는 자리표."""
    __slots__ = ("w", "h")
    def __init__(self, w, h): self.w, self.h = w, h


def sep_rows(img, y0=0.13, y1=0.86):
    """가로로 거의 한 색인 줄의 묶음 — [(위 %, 아래 %)]. 행 사이 여백·구분선이 잡힌다.

    ⚠ % 는 **앱 상자 기준**이다(68회차) — 원작 샷의 레터박스를 빼지 않으면 최대 2.5%p 가 치우친다.
    """
    px, W, H = img.px, img.w, img.h
    ax, ay, aw, ah = app_box(img)
    a, b = ax + int(aw * SEP_X0), ax + int(aw * SEP_X1)
    n = len(range(a, b, 2))
    ys = []
    for y in range(ay + int(ah * y0), ay + int(ah * y1)):
        base = y * W * 3
        cnt = {}
        for x in range(a, b, 2):
            i = base + x * 3
            k = (px[i] // 8, px[i + 1] // 8, px[i + 2] // 8)
            cnt[k] = cnt.get(k, 0) + 1
        if cnt and max(cnt.values()) >= n * SEP_SAME:
            ys.append(y)
    out, st, prev = [], None, None
    for y in ys:
        if st is None:
            st = prev = y
        elif y == prev + 1:
            prev = y
        else:
            if prev - st + 1 >= SEP_MIN:
                out.append(((st - ay) * 100.0 / ah, (prev + 1 - ay) * 100.0 / ah))
            st = prev = y
    if st is not None and prev - st + 1 >= SEP_MIN:
        out.append(((st - ay) * 100.0 / ah, (prev + 1 - ay) * 100.0 / ah))
    return out


def _align(o, c):
    """원작 줄 ↔ 클론 줄을 **차례를 지켜** 짝짓는다 — [(원작 i, 클론 j 또는 -1)].

    🚫 되풀이하지 마라: 62회차까지 여기는 «제일 가까운 클론 줄» 을 하나씩 집어 가는
    탐욕 짝짓기였다. 줄이 통째로 조금 밀린 화면에서 이것은 **차례를 어겨** 원작 n 번
    줄을 클론 n+1 번 줄에 붙이고, 남은 클론 첫 줄을 «원작에 없는 줄» 로 버린다.
    그래서 실제로는 −1.4%p 인 밀림이 «나란히 −4.0%p 세 줄» 이라는 가짜 등재감으로
    나왔다(`skills` 3줄 · `skill-detail` 2줄 · 62회차 실측). 자가 진단(⑯-b)이 그 짝을
    그대로 들고 있으니, 탐욕으로 되돌리면 빨갛게 된다.

    값은 둘 다 위에서 아래로 정렬돼 있으므로 차례를 지키는 최소비용 짝짓기(DP)면 된다.
    `SHIFT_MAX` 보다 먼 짝은 아예 짝으로 치지 않고, 건너뛰기 삯도 같은 값으로 둔다 —
    그래야 «가까운 짝이 있으면 붙이고, 없으면 버린다» 가 된다.
    """
    n, m = len(o), len(c)
    INF = float("inf")
    # d[i][j] = 원작 i개 · 클론 j개까지 맞춘 최소 삯
    d = [[INF] * (m + 1) for _ in range(n + 1)]
    bk = [[None] * (m + 1) for _ in range(n + 1)]
    d[0][0] = 0.0
    for i in range(n + 1):
        for j in range(m + 1):
            if d[i][j] == INF:
                continue
            if i < n and d[i][j] + SHIFT_MAX < d[i + 1][j]:      # 원작 줄 버리기
                d[i + 1][j] = d[i][j] + SHIFT_MAX
                bk[i + 1][j] = (i, j, -1)
            if j < m and d[i][j] + SHIFT_MAX < d[i][j + 1]:      # 클론 줄 버리기
                d[i][j + 1] = d[i][j] + SHIFT_MAX
                bk[i][j + 1] = (i, j, -2)
            if i < n and j < m:
                gap = abs(c[j][0] - o[i][0])
                if gap < SHIFT_MAX and d[i][j] + gap < d[i + 1][j + 1]:
                    d[i + 1][j + 1] = d[i][j] + gap
                    bk[i + 1][j + 1] = (i, j, j)
    out, i, j = [], n, m
    while i or j:
        pi, pj, tag = bk[i][j]
        if tag >= 0:
            out.append((pi, tag))
        elif tag == -1:
            out.append((pi, -1))
        i, j = pi, pj
    out.reverse()
    return out


def rows_hit(shots_dir, name, ref_dir=REF_DIR):
    """`--rows` 가 세는 «맞은 줄 / 원작 줄» 만 돌려준다(못 재면 `None`).

    왜 필요한가(T28 97회차): `--rows` 의 결과가 **그 화면을 따로 칠 때만** 보여서, 「다음 볼 화면」을
    따라간 사람이 «어디부터 볼까» 를 매번 다시 알아낸다. 그 한 줄을 목록에 같이 찍는다.

    🚫 **«줄이 맞으니 배치는 다 맞다» 로 읽지 마라 — 97회차에 내가 그렇게 적었다가 바로 걷었다.**
    `sep_rows` 는 **굵은 가로 구분선**만 본다. 상자 높이가 틀려도 그 차가 `margin: auto` 같은
    데로 흡수되면 구분선은 제자리에 남는다 — 실측: `dungeon-detail` 은 줄이 **6/7** 맞는데도
    95회차가 인라인 아이콘 줄 상자에서 **−11.4px** 을 찾아 T433 으로 등재했다. 그래서 이 표시는
    «자리가 맞다» 가 아니라 **«61회차의 «나란히 밀린 줄» 갈래는 아니다 → 상자 높이·여백을 봐라»**
    는 뜻이다. 실제로 이 표시가 붙은 화면 넷 중 셋(`dungeon-detail`·`tech-node`·`offline`)에
    이미 등재된 결함이 있다.
    """
    refs = dict((n, r) for n, r in pairs() if r)
    rf = refs.get(name)
    cp = os.path.join(shots_dir, "screen_%s.png" % name)
    rp = os.path.join(ref_dir, rf) if rf else None
    if not rf or not os.path.exists(rp) or not os.path.exists(cp):
        return None
    try:
        o, c = sep_rows(png_read(rp)), sep_rows(png_read(cp))
    except Exception:
        return None
    if not o:
        return None
    ok = 0
    for oi, bi in _align(o, c):
        if bi >= 0 and abs(c[bi][0] - o[oi][0]) <= SEP_TOL:
            ok += 1
    return (ok, len(o))


ROWS_OK_MIN = 0.75   # 원작 줄의 이만큼이 맞으면 «밀린 줄 갈래가 아니다» 로 적는다(97회차 실측 8/9 = .89 · 3/8 = .38 은 아니다)


def rows_cmp(shots_dir, name, ref_dir=REF_DIR):
    """`--rows <화면>` — 원작 ↔ 클론의 구분선 줄 자리를 견준다(딤과 무관)."""
    refs = dict((n, r) for n, r in pairs() if r)
    rf = refs.get(name)
    cp = os.path.join(shots_dir, "screen_%s.png" % name)
    rp = os.path.join(ref_dir, rf) if rf else None
    if not rf or not os.path.exists(rp):
        print(u"✗ 원작 짝을 못 찾았다: %s" % name)
        return 2
    if not os.path.exists(cp):
        print(u"✗ 클론 샷이 없다: %s" % cp)
        return 2
    o, c = sep_rows(png_read(rp)), sep_rows(png_read(cp))
    print(u"«%s» 구분선 줄 — 원작 %d개 · 클론 %d개 (허용 ±%.1f%%p · 딤과 무관)"
          % (name, len(o), len(c), SEP_TOL))
    used, off, shifted = set(), 0, []
    for oi, bi in _align(o, c):          # 차례를 지켜 짝짓는다(62회차 — 탐욕 금지)
        a0, a1 = o[oi]
        if bi >= 0:
            used.add(bi)
            gap = c[bi][0] - a0
        if bi >= 0 and abs(gap) <= SEP_TOL:
            print(u"   ✓ 원작 %5.1f~%5.1f ↔ 클론 %5.1f~%5.1f  (Δ %+.1f)"
                  % (a0, a1, c[bi][0], c[bi][1], gap))
        else:
            off += 1
            if bi >= 0:
                shifted.append((a0, gap))
            near = u"" if bi < 0 else u" · 짝은 클론 줄 %.1f(Δ %+.1f)" % (c[bi][0], gap)
            print(u"   ✗ 원작 %5.1f~%5.1f 에 맞는 클론 줄이 없다%s" % (a0, a1, near))
    # ── 나란히 밀린 줄 (T28 61회차) ──────────────────────────────────────
    # 안 맞은 줄이 흩어져 있으면 얇은 선·글자 잡음이고, **여러 줄이 같은 크기로 밀려 있으면**
    # 그 위 어딘가가 짧거나 길다는 뜻이다 — 그것이 진짜 등재감이다(실측: `main` 의 −1.9·−1.8 짝).
    if shifted:
        groups = {}
        for a0, d in shifted:
            groups.setdefault(round(d / SHIFT_BIN) * SHIFT_BIN, []).append((a0, d))
        for k in sorted(groups, key=lambda k: -len(groups[k])):
            g = groups[k]
            if len(g) >= SHIFT_MIN:
                print(u"   ⇒ **나란히 밀린 줄 %d개** — 전부 %+.1f%%p 언저리다(%s). "
                      u"흩어진 어긋남과 달리 이것은 «그 위 어딘가가 짧다/길다» 는 뜻이라 **등재감**이다."
                      % (len(g), k, " · ".join(u"원작 %.1f(%+.1f)" % t for t in g)))
    extra = [c[i] for i in range(len(c)) if i not in used]
    if extra:
        print(u"   · 원작에 없는 클론 줄 %d개: %s"
              % (len(extra), " ".join(u"%.1f" % t[0] for t in extra)))
    print(u"— 맞은 줄 %d / %d. 여기가 맞으면 **낮은 점수는 자리가 아니라 딤·글자 모양 탓**이다."
          % (len(o) - off, len(o)))
    print(u"  ⚠ 안 맞은 줄을 바로 결함으로 읽지 마라 — 두 갈래가 섞인다(T28 60회차 전수 실측):")
    print(u"    ⓐ **팝업 화면**은 원작 딤이 α .988 이라 카드 밖이 통째로 «한 색» 이고, 클론은 α .5 라"
          u" 뒤 화면이 비쳐 줄이 쪼개진다 — 안 맞는 수가 부풀어 보인다(`craft-compare` 2/8 · `gear-detail` 4/9).")
    print(u"    ⓑ **3D 세계가 든 화면**(`main`·`offline`)의 위쪽 줄은 나무·흙길 대 `SIMPLE_BG` 라 처음부터 못 맞춘다.")
    print(u"    그 둘을 뺀 **전면 UI 화면**의 어긋남이 진짜 후보다 — 특히 같은 크기로 **여러 줄이 나란히 밀린** 것.")
    return 0


def jitter_span(shots_dir, name, ref_rects):
    """이 화면의 점수가 **±1 흔들기**에 얼마나 움직이나 — (최소, 최대) 또는 None.

    🚨 «다음 볼 화면» 의 순서는 점수로 매기는데, 자가 ±1 에 흔들리는 화면은 그 순서가 **자의 딸**이다
    (T437 6회차 실측: 35장 중 23장이 ±1 에 조각 수를 바꾼다 · `--jitter`). 그래서 맨 위 다섯에만
    이 폭을 같이 찍는다 — 폭이 크면 «그 화면이 정말 뒤처졌나» 를 점수로 단정하지 말라는 뜻이다.
    다섯 화면 × 네 번이라 값이 큰 `--score` 전체는 안 느려진다.
    """
    f = os.path.join(shots_dir, "screen_%s.png" % name)
    if not os.path.exists(f):
        return None
    img = png_read(f)
    vals = []
    for amp, ch in JITTER_CASES:
        v = score_screen(ref_rects, read_layout(_jitter(img, amp, ch), name))
        vals.append(v[0] if isinstance(v, tuple) else v)
    return (min(vals), max(vals))


def next_screens(cand, rate, shaky, take=5):
    """«다음 볼 화면» 차례 — **안 흔들리는 화면을 앞에**, 그 안에서는 달성률이 낮은 것부터.

    🚫 흔들리는 화면을 **빼지 않는다**(낡은 샷 화면과 같은 규칙 · T28 81회차): 빼면 진짜 결함이
    숨는다. 뒤로 밀 뿐이다. 왜 미나 — 자가 ±1 에 흔드는 화면은 **점수의 순서 자체가 자의 딸**이라
    «여기가 제일 뒤처졌다» 를 못 읽는다(T437 ✂ · 런 1058 에서는 낮은 다섯이 전부 흔들렸다).
    """
    return sorted(cand, key=lambda t: (shaky(t[0]), rate(t)))[:take]


def score(table_path, shots_dir, only=None, baseline_path=BASELINE, save_baseline=False, notes_full=False, run_override=None):
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
    tops = []
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
    # T502 — CI 가 PNG 옆에 올린 **바이트** 지문 장부. 없으면 빈 dict — 낡은 `screens` 나 긁개 폴더로 재도 조용히 넘어간다.
    new_sha = {}
    _sp = os.path.join(shots_dir, SHA_LIST)
    if os.path.exists(_sp):
        try:
            with open(_sp, encoding="utf-8") as _f:
                new_sha = parse_sha_list(_f.read())
        except OSError:
            new_sha = {}
    fps, bands, ceilings, elems = {}, {}, {}, {}
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
            # **앱 상자** 비로 본다 — 점수가 나는 자리와 같은 자다(T393 3회차). 그림 통짜 비로 보면
            # 레터박스(원작 30장)와 기기 크롬(`ref/shots` 시트)까지 세어 멀쩡한 화면이 운다.
            ra = app_ratio(ent["wh"][0], ent["wh"][1])
            rb = app_ratio(img.w, img.h)
            if abs(ra - rb) > ra * 0.03:
                # 틀이 다르면 같은 자리도 y%% 가 밀린다 — 점수가 아니라 **틀**이 어긋난 것이다.
                print(u"  ⚠ %-18s 틀 불일치: 원작 앱 상자 세로/가로 %.3f ↔ 클론 %.3f (그림 %dx%d) — 앱 상자를 9:16 으로 잘라 찍는다"
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
                try:
                    a_top, b_top = card_top(png_read(rp)), card_top(img)
                except (OSError, ValueError):
                    a_top = b_top = None
                if a_top is not None and b_top is not None:
                    tops.append((name, a_top, b_top))
        got = read_layout(img, name)
        s, why = score_screen(ent["rects"], got)
        ceil = score_ceiling(ent["rects"], got)
        ceilings[name] = ceil
        scores.append((name, s))
        fps[name] = fingerprint(img)
        # 밴드를 몇 개로 쪼갰나 — 회차 사이에 이 수가 달라지면 «요소가 어긋났다» 가 아니라
        # **자가 화면을 다르게 쪼갠 것**이다(T28 24·27회차 실측: 잉크가 한 겹 두꺼워지면 밴드가 붙는다).
        bands[name] = sum(1 for r in got if u"블록" not in r.name)
        # T28 98회차 — **요소 수**(블록까지 센 전부)도 남긴다. 밴드 수는 큰 갈래만 세는데,
        #   점수를 실제로 흔드는 것은 **짝짓기에 들어가는 요소의 수**다(아래 «판독기 흔들림» 알림).
        elems[name] = len(got)
        mark = u"✓" if s >= PASS_MARK else u"✗"
        print(u"  %s %-18s %4.1f / 10   (천장 %.1f · 달성 %.0f%%  · 원작 요소 %d)"
              % (mark, name, s, ceil, (100.0 * s / ceil) if ceil else 0.0, len(ent["rects"])))
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
    if tops:
        offt = [(n, a, b) for n, a, b in tops if abs(b - a) > CARD_TOP_TOL]
        print(u"  · 팝업 카드 **위끝**(딤과 무관한 자 · 원작 ↔ 클론 · ±%.1f%%p): 잰 화면 %d개 · 벗어난 화면 %d개"
              % (CARD_TOP_TOL, len(tops), len(offt)))
        for n, a, b in sorted(offt, key=lambda t: -abs(t[2] - t[1])):
            # 낡은 샷 화면은 **꼬리표를 달아 둔다**(지우지 않는다 · T28 81회차와 같은 규칙) — 안 달면
            # 매 회차 같은 줄을 보고 같은 자리를 다시 판다(T28 120회차에 내가 그렇게 한 번 팠다).
            note = (u"  ⚠ 낡은 샷 — %s" % STALE_SCREENS[n]) if n in STALE_SCREENS else u""
            print(u"    ✗ %-18s 원작 %.2f%%H → 클론 %.2f%%H  (Δ %+.2f%%p)%s" % (n, a, b, b - a, note))
        if offt:
            print(u"    ⓘ **카드가 통째로 밀린 것**이라 그 안을 아무리 재도 안 맞는다 — 먼저 이 값을 맞춰라."
                  u" 다만 원작 샷과 클론의 **내용 줄 수가 다른 화면**(`forge-list` 등)은 카드가 내용을 따라가므로"
                  u" 자리로 읽지 마라(T28 70·71회차).")
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
        # ── 표의 30장을 한 장도 못 찾은 회차 (T28 52회차 · 런 501 실측) ─────────
        # 폴더에 PNG 가 아예 없는 것과, **진단용 PNG 는 있는데 대조할 30장만 없는 것**은 다른 일이다.
        # 런 501 이 뒤쪽이었다: `UiShotsTests` 가 던져 죽어 `screen_<이름>.png` 30장이 한 장도 안 나왔는데
        # 진단 샷 9장(`screen_boot-loading`·`screen_t147-*`·`screen_t330-*` …)은 남아 `meta.json` 이
        # `shots: 17 · carried: 0` 이 됐다 — **이어받기가 «PNG 0장» 에만 걸려 있어 안 돌았고**(`ci.yml` 431)
        # `screens` 에서 30장이 통째로 사라졌다. 그때 «샷이 하나도 없다» 고만 찍으면 읽는 사람이 헛다리를 짚는다.
        other = 0
        try:
            other = len([f for f in os.listdir(shots_dir) if f.endswith(".png")])
        except OSError:
            pass
        if other:
            print(u"✗ 대조표의 화면을 **한 장도 못 찾았다** — 그런데 폴더엔 PNG 가 %d장 있다"
                  u"(진단 샷·시트). 촬영(`UiShotsTests`)이 30장을 내기 전에 선 것이다." % other)
        else:
            print(u"✗ 점수를 낸 화면이 0개다 — 클론 샷(`screen_*.png`)이 하나도 없다")
        if meta:
            print(u"    이 런 meta: shots %s · carried %s · 없는 모드 «%s»"
                  % (meta.get("shots", "?"), meta.get("carried", "?"), meta.get("missing_modes") or u"없음"))
            if meta.get("shots") and not meta.get("carried"):
                print(u"    ⚠ `shots` 가 0 이 아니라서 **이어받기가 안 돌았다** — 지난 런의 30장까지 같이 사라졌다(T347 · 워커 I 등재).")
        base0 = load_baseline(baseline_path)
        gone0 = sorted(n for n in base0 if not n.startswith("_"))
        if gone0:
            print(u"    지난 회차(런 %s)에 있던 화면 %d개가 통째로 빠졌다: %s"
                  % (base0.get("_run", "?"), len(gone0),
                     " · ".join(u"%s %.1f" % (n, base0[n]) for n in gone0[:6]) +
                     (u" …" if len(gone0) > 6 else u"")))
            print(u"    **기준선은 그대로 둔다** — 이 회차는 «점수가 내려갔다» 가 아니라 «촬영이 섰다» 이다.")
        return 2
    avg = sum(s for _, s in scores) / len(scores)
    print(u"─" * 60)
    print(u"평균 %.2f / 10 · 화면 %d개 · %s점 미만 %d개" % (avg, len(scores), PASS_MARK, len(bad)))
    if ceilings:
        cav = sum(ceilings.get(n, 0.0) for n, _v in scores) / len(scores)
        rate = 100.0 * sum((v / ceilings[n]) for n, v in scores if ceilings.get(n)) / len(scores)
        print(u"· **자의 천장 평균 %.2f / 10 · 달성률 %.0f%%** — 10 점은 닿을 수 없는 수다"
              u"(짝 없음·군더더기 실점은 원작 샷의 딤 α .988 탓이라 클론 배치로 안 줄어든다 · 결정 686)."
              % (cav, rate))
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
                print(u"  ⚠ **두 런 사이에 코드가 바뀌었는지 먼저 확인해라**(`git merge-base --is-ancestor <고침> <런 sha>`) — 런이 다르면 «흔들린다» 고 읽기 쉬운데 실은 **고침이 그 사이에 들어간 것**일 수 있다(T28 140회차가 139회차의 «새 흔들림» 을 그렇게 잘못 읽었다). 흔들림은 **같은 코드의 두 런**을 나란히 놓아야 보인다.")
        hist = base.get("_hist") or []
        obase = base.get("_fp") or {}
        oband = base.get("_bands") or {}
        oelem = base.get("_elems") or {}

        def band_note(n):
            a, b = oband.get(n), bands.get(n)
            return u"" if a is None or b is None or a == b else u" · 밴드 %d → %d(자가 다르게 쪼갰다)" % (a, b)

        # ── 전역 손질 알림(T28 86회차 · 결정 712) ──────────────────────────
        # 자는 «내려간 화면» 을 «깬 사람을 찾으라» 로 올린다. 그런데 글자 자간·색 토큰처럼
        # **모든 화면을 한꺼번에** 바꾸는 손질이 들면 그 말이 사람을 엉뚱한 데로 보낸다 —
        # 85회차가 실제로 그랬다: `offline` −2.4 · `league-challenge` −1.2 를 «깨졌다» 로 올렸는데
        # 두 화면 다 **글자 폭이 원작에 붙은** 옳은 고침이었다(T352 `boldSpacing 0`).
        # 86회차 전수: 그 런에서 **35 화면 전부**가 달라졌다(11~88% 줄). 그래서 먼저 그 사실을 찍는다.
        moved = [n for n, _v in scores
                 if obase.get(n) and fps.get(n) and max(fp_diff(obase.get(n), fps.get(n))) >= 0.5]
        if scores and len(moved) >= max(3, len(scores) * 0.5):
            print(u"⚠ **이번 런은 전역 손질이다** — 화면 %d/%d 의 그림이 움직였다(지문 차 0.5 이상). "
                  u"내려간 화면을 곧장 «깬 사람» 으로 읽지 마라: 글자 자간·색 토큰·테 두께처럼 "
                  u"**모든 화면을 한꺼번에 바꾸는 값**이 움직였는지 먼저 본다(T28 85·86회차 · 결정 712)."
                  % (len(moved), len(scores)))

        # ── 바이트가 달라졌나 (T502 · T514 가 읽는 법을 고쳤다) ──────────────
        # 183회차는 이 갈림을 «잴 수 없었다» 로 적고, 184회차는 **앞 런 PNG 를 덮히기 전에 내려** 운 좋게 쟀다.
        # T502 가 CI 에 지문 장부를 세워 그 수가 매 회차 공짜로 나게 했다 — 그런데 **T502 가 약속한 갈래는 안 켜진다**:
        #   T28 187회차 실측(기준선 런 1356 `shot_sha` 58칸 ↔ 런 1363 장부 58칸) — 바이트가 같은 화면은 **여섯**이고
        #   그 여섯은 **전부 자 모의 샷**(`t147-edge-*`·`t330-id-*` = 코드로 그린 작은 판)이다. **게임 화면 52장은 하나도 안 같고**
        #   채점에 드는 31장은 **0/31** 이다. 그 52 에는 그 사이 어느 커밋도 안 건드린 화면(`tech-node`·`profile`·`chat`·`boot-loading`)이
        #   다 들어 있다 — 곧 **PNG 는 런마다 바이트가 다르다**(3D 하늘·시간 축 연출·글자 AA).
        # ⇒ ⓐ «같은 화면 0» 은 **흔한 일**이고 «31장이 다 달라졌다» 가 **아니다** — 그 말을 그 자리에서 해 준다.
        #   ⓑ «그림이 그대로인데 점수가 움직였다» 의 **쓸 수 있는** 잣대는 바이트가 아니라 **지각 지문**(`FP_SAME`)이다.
        #   바이트 장부의 값은 남는다 — 그림이 **진짜** 안 바뀐 자리(자 모의 샷·이어받은 샷)를 공짜로 가린다.
        osha = base.get("_sha") or {}
        if new_sha or osha:
            same, diff, unk = sha_split(osha, new_sha, set(cur) | set(n for n in base if not n.startswith("_")))
            print(u"· **바이트 지문**(T502) — 같은 화면 %d · 다른 화면 %d · 모르는 화면 %d"
                  % (len(same), len(diff), len(unk)))
            if not osha:
                print(u"    (지난 회차 기준선에 지문 칸이 없다 — 이 회차가 처음 적는다. 다음 회차부터 갈린다.)")
            elif not new_sha:
                print(u"    (이 `--shots` 자리에 `%s` 가 없다 — 낡은 `screens` 이거나 긁개 폴더다. "
                      u"«안 달라졌다» 가 **아니다**.)" % SHA_LIST)
            # T514 — «같은 화면 0» 을 본 사람이 «클론이 통째로 움직였다» 로 읽지 않게 **까닭을 같이 찍는다**.
            elif not same and diff:
                print(u"    (같은 화면이 0인 것은 **흔한 일이다** — 런마다 3D 하늘·시간 축 연출·글자 AA 가 바이트를 바꾼다. "
                      u"실측 1356↔1363: 게임 화면 **52/52 가 달랐고** 같은 여섯은 **전부 자 모의 샷**이었다. "
                      u"이 칸으로 «그림이 달라졌나» 를 가르지 마라 — 아래 **지문** 수(안/뒤)가 그 자다 · T514)")
            # 값: «그림이 그대로인데 점수가 움직인 화면» — 그것이 자 결함(T437·T444)의 **증거**다.
            # ⚠ T514 — **바이트로는 그 증거가 안 잡힌다**(채점 31 화면은 실측 0/31 로 늘 다르다). 이 줄은 사실상
            #   **자 모의 샷에만** 걸리는 자리다 — 그래서 남겨 두되(거기선 참이다) 같은 뜻의 쓸 수 있는 잣대를 **그 자리에 함께** 찍는다.
            quiet_moved = [(n, base[n], cur[n]) for n in same
                           if n in base and n in cur and abs(cur[n] - base[n]) >= DROP_MARK]
            if quiet_moved:
                print(u"    ⚠ **바이트가 같은데 점수가 움직인 화면 %d개 — 범인은 클론이 아니라 자다**(T437·T444): %s"
                      % (len(quiet_moved),
                         u" · ".join(u"%s %.1f→%.1f" % t for t in sorted(quiet_moved, key=lambda t: -abs(t[2] - t[1])))))
            # T514 — **쓸 수 있는 갈래**: 지각 지문 차가 `FP_SAME` 아래(안·뒤 둘 다)인데 점수가 `DROP_MARK` 넘게 움직인 화면.
            #   바이트와 달리 이 수는 채점 화면에서 실제로 난다(187회차: `main` 안 0.8 · 뒤 0.1 에 −0.6). 그것이 T437·T444 가 찾던 증거다.
            fp_moved = fp_quiet_moved(base, cur, obase, fps)
            if fp_moved:
                print(u"    ⚠ **지문은 그대로인데 점수가 움직인 화면 %d개 — 범인은 클론이 아니라 자다**(T437·T444 · 바이트가 아니라 이 수로 본다 · T514):"
                      % len(fp_moved))
                print(u"        %s" % u" · ".join(u"%s %.1f→%.1f(지문 안 %.1f · 뒤 %.1f)" % t for t in fp_moved))
            loud = [n for n in diff if n in base and n in cur and abs(cur[n] - base[n]) >= DROP_MARK]
            if loud:
                print(u"    · 바이트가 다르고 점수도 움직인 화면 %d개 — **런 흔들림이거나 진짜 고침**이다(코드가 그 사이에 바뀌었는지 먼저 본다): %s"
                      % (len(loud), u" ".join(sorted(loud))))

        # ── 판독기 흔들림 (T28 98회차 · 결정 746) ────────────────────────────
        # 90·97회차가 «점수가 그림과 어긋난다» 를 두 얼굴로 봤는데, 98회차가 그 **한 뿌리**를 찾았다:
        # 점수를 흔드는 것은 짝짓기도 점수식도 아니고 **`read_layout` 이 화면을 몇 조각으로 읽었나**다.
        # 런 968 ↔ 1005 전수(31장): 요소 수가 **같은 17장은 점수가 ±0.00 으로 똑같고**,
        # 달라진 14장만 점수가 움직였다(예외 0). 그 14 중 **아홉은 지문이 1.0 아래**(그림이 그대로)다.
        # 가장 큰 자리 — `offline` 43 → 45 개에서 원작 «밴드3»과 그 블록 넷의 짝이 통째로 갈려
        # 짝 11 → 7 · 짝없음 4 → 8 로 **−2.17점**. 그래서 이 수를 회차마다 이름 대어 찍는다.
        churn = []
        for n in sorted(cur):
            oe, ne = oelem.get(n), elems.get(n)
            if oe and ne and oe != ne:
                din, _d = fp_diff(obase.get(n), fps.get(n))
                churn.append((n, oe, ne, din))
        if churn:
            quiet = [t for t in churn if t[3] is not None and t[3] < FP_SAME]
            print(u"⚠ **판독기가 화면을 다르게 읽은 화면 %d개**(요소 수가 달라졌다) — 그 중 **%d개는 지문이 그대로**다. "
                  u"점수가 움직인 화면은 거의 전부 이 목록 안에 있다(T28 98회차 전수: 요소 수가 같으면 점수도 같다 · 예외 0). "
                  u"**고칠 곳을 찾기 전에 이 줄부터 봐라**:" % (len(churn), len(quiet)))
            print(u"    %s" % u" · ".join(u"%s %d→%d%s" % (n, a, b, u"" if d is None else u"(지문 %.1f)" % d)
                                          for n, a, b, d in sorted(churn, key=lambda t: -abs(t[2] - t[1]))))

        # ── 자와 그림이 어긋난 회차 (T28 90회차 · 결정 729) ──────────────────
        # 위 «전역 손질» 은 **여러 화면이 한꺼번에 움직였을 때**를 짚는다. 그런데 그 반대꼴이 있다 —
        # **점수가 움직인 화면과 그림이 달라진 화면이 서로 다른 집합**인 회차다. 실측(런 921 → 935 ·
        # 그림은 930 · 31장 전수): 점수가 ±0.5 넘게 움직인 **다섯**(settings −1.3 · profile −1.2 ·
        # ascend +0.9 · player-info −0.9 · pass −0.5)은 지문 «안» 이 전부 **0.76 아래**였고,
        # 지문 «안» 이 1.0 넘게 달라진 **넷**(shop 2.64 · craft-compare 1.51 · main 1.43 ·
        # gear-detail 1.25)은 전부 **±0.2 안**이었다 — **두 집합이 하나도 안 겹쳤다**.
        # 화소로도 같다: 움직인 셋은 화소 차 0.64~0.88%인데 shop 은 8.31% 를 바꾸고도 0.0 이다.
        # 그런 회차의 «내려간 화면» 은 **고칠 곳이 아니라 밴드 쪼개짐**이다 — 그 말을 먼저 한다.
        mv, cg = [], []
        for n in sorted(cur):
            if n not in base:
                continue
            din, _dout = fp_diff(obase.get(n), fps.get(n))
            if abs(cur[n] - base[n]) >= DROP_MARK:
                mv.append((n, cur[n] - base[n], din))
            if din is not None and din >= FP_SAME:
                cg.append((n, din, cur[n] - base[n]))
        if (len(mv) >= 3 and len(cg) >= 3
                and all(d is not None and d < FP_SAME for _n, _v, d in mv)
                and all(abs(v) < DROP_MARK for _n, _d, v in cg)):
            print(u"⚠ **이번 런은 자와 그림이 어긋났다** — 점수가 ±%.1f 넘게 움직인 화면 %d개는 "
                  u"**전부 지문이 그대로**(안 최대 %.2f)이고, 지문 «안» 이 %.1f 넘게 달라진 화면 %d개는 "
                  u"**전부 ±%.1f 안**이다(두 집합이 안 겹친다). 곧 이 회차의 점수 변화는 «무엇이 달라졌나» 가 "
                  u"아니라 **밴드 쪼개짐**을 재고 있다 — 내려간 화면을 고칠 곳으로 읽지 마라(T28 90회차)."
                  % (DROP_MARK, len(mv), max(d for _n, _v, d in mv), FP_SAME, len(cg), DROP_MARK))
            print(u"    움직인 화면: %s" % u" · ".join(u"%s %+.1f(지문 %.2f)" % (n, v, d)
                                                  for n, v, d in sorted(mv, key=lambda t: -abs(t[1]))))
            print(u"    달라진 화면: %s" % u" · ".join(u"%s 지문 %.2f(%+.1f)" % (n, d, v)
                                                  for n, d, v in sorted(cg, key=lambda t: -t[1])))

        hard, soft = [], []
        bwset = dict((t[0], t[1]) for t in bw)
        for t in drops:
            n = t[0]
            din, dout = fp_diff(obase.get(n), fps.get(n))
            med = median_of(hist, n) if len(hist) >= 3 else None
            why = drop_note(din, dout, med, len(hist), t[2], t[1],
                            bw=bwset.get(n), bg=BG_SHAKY.get(n, 0.0))
            if why is None:
                hard.append((n, t[1], t[2], din, dout))
            elif din is None:
                soft.append((n, t[1], t[2], why, None, None))
            else:
                soft.append((n, t[1], t[2], why, din, dout))
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
        # ── 회차마다 크게 흔들리는 화면 (T28 48회차 · 워커 M) ──────────────────
        # 한 회차의 수를 근거로 삼으면 안 되는 화면이 있다 — 그림이 그대로인데 밴드가 갈려
        # 점수가 3점 넘게 오간다(실측: `settings` 13↔16 밴드 · 3.6 ↔ 6.8). 이름을 대어 알린다.
        shaky = []
        for n in sorted(cur):
            vals = [h[1][n] for h in hist if n in h[1]]
            if len(vals) >= 3 and max(vals) - min(vals) >= SHAKY_SPREAD and _flaps(vals):
                shaky.append((n, min(vals), max(vals), median_of(hist, n)))
        if shaky:
            print(u"· ⚠ 회차마다 크게 흔들리는 화면 %d개 — **한 회차의 수를 근거로 삼지 마라**(중앙값을 봐라):"
                  % len(shaky))
            for n, lo, hi, med in shaky:
                print(u"    %-18s 최근 %d회차 %.1f~%.1f(폭 %.1f) · 중앙값 %.1f · 이번 %.1f"
                      % (n, len(hist), lo, hi, hi - lo, med, cur[n]))
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
    verify_stale_notes()   # T512 1회차 — 위 목록이 댄 정본 근거가 아직 산지 **매 회차** 본다(정본 사본이 없으면 건너뛴다)
    if save_baseline and suspect[0]:
        print(u"· 기준선을 **안 적었다** — 위 «촬영이 어긋났을 수 있다» 경고 때문이다."
              u" 멀쩡한 런에서 다시 `--save-baseline` 하면 된다(결정 195·199 의 규칙 그대로).")
        save_baseline = False
    # 런 번호는 CI 가 screens 에 같이 올린 meta.json 에서 읽는다 · 없으면 `--run` 으로 댄 것을 쓴다(T500).
    run = meta.get("run")
    if run is None:
        run = run_override
    if save_baseline and run is None:
        # T500 — **번호를 못 읽으면 적지 않는다.** 바로 위 «촬영이 어긋났을 수 있다» 와 같은 꼴로 접는다.
        #   번호 없는 칸은 «이름이 빈 칸» 으로 끝나지 않는다 — 다음 저장의 거르기가 그 줄을 **지운다**(1159행).
        #   이력 여섯 칸이 T28 의 «회귀냐 흔들림이냐» 를 가르는 근거 전부라 한 칸이 빠지면 중앙값이 움직인다.
        print(u"· 기준선을 **안 적었다** — 이 `--shots` 자리에 `meta.json` 의 `run` 이 없다."
              u" `screens` 를 받아 다시 재거나 `--run <번호>` 로 대라(T500).")
        save_baseline = False
    if save_baseline:
        save_baseline_file(baseline_path, scores, avg, run, fps, bands, carried, ceilings, elems, new_sha)
        print(u"· 기준선을 %s 에 적었다(다음 회차가 이것과 견준다)" % os.path.relpath(baseline_path, REPO))
    if bad:
        # T28 16회차(워커 M): 29개를 줄줄이 찍으면 아무도 안 읽는다 — **다섯**만 준다.
        # T28 79회차: 고르는 자를 **절대 점수 → 달성률(점수/천장)** 로 바꿨다(결정 686).
        #   절대 점수 순서는 천장이 3점대인 화면만 계속 집어 줄다 — 그것은 자의 딸이지 클론의 딸이 아니다.
        def _ceil_med(n):
            """이 화면의 **천장 중앙값**(자취 3회차 이상이면) — 한 회차의 천장은 밴드 쪼개짐이 바뀌면 통째로 흔들린다(결정 722)."""
            past = [h["ceilings"][n] for h in (base.get("_hist") or [])
                    if isinstance(h, dict) and h.get("ceilings", {}).get(n)]
            return ceil_median(past, ceilings.get(n, 0.0))

        def _rate(t):
            c = _ceil_med(t[0])
            # 낡은 샷 화면은 **맨 뒤로** 밀린다(지우지는 않는다 · T28 81회차).
            base = (t[1] / c) if c else 1.0
            return (base + 10.0) if t[0] in STALE_SCREENS else base
        cand = sorted(((n, v) for n, v in scores if v < PASS_MARK), key=_rate)[:8]
        # T28 108회차 — **자가 흔드는 화면은 뒤로 민다(지우지는 않는다)**. 런 1058 에서는 달성률 낮은
        #   다섯이 **전부** ±1 에 흔들려, 줄이 «먼저 볼 곳» 노릇을 못 했다. 그렇다고 지우면 진짜 결함이
        #   숨으므로 낡은 샷 화면과 같은 규칙(81회차)을 쓴다 — **여덟을 재서 안 흔들리는 것을 앞에 세운다.**
        spans = {}
        for n, _v in cand:
            spans[n] = jitter_span(shots_dir, n, table[n]["rects"]) if n in table else None
        def _shaky(n):
            sp = spans.get(n)
            return 1 if (sp and (sp[1] - sp[0]) >= JITTER_WARN) else 0
        low = next_screens(cand, _rate, _shaky)
        print(u"«다음 볼 화면»(ROUTINE §2 T28 · **달성률(점수/천장)이 낮은 것부터** · 원작 PNG 와 나란히 보고 정본 코드로 확인한 뒤 등재):")
        if any(_shaky(n) for n, _v in cand):
            print(u"    ⓘ **자가 ±1 에 흔드는 화면은 뒤로 밀었다**(지운 것이 아니다 · T437 ✂) — 여덟을 재서 "
                  u"**안 흔들리는 것을 앞에** 세운다. 흔들리는 화면은 점수가 아니라 **원작 PNG 로** 판단해라.")
        for n, v in low:
            c = _ceil_med(n); cnow = ceilings.get(n, 0.0)
            tag = (u"  ⚠ 낡은 샷 — %s" % STALE_SCREENS[n]) if n in STALE_SCREENS else u""
            note = u"" if abs(cnow - c) < 0.3 else (u"(이번 %.1f)" % cnow)
            sp = spans.get(n)
            if sp and (sp[1] - sp[0]) >= JITTER_WARN:
                tag += (u"  · ⚠ 이 점수는 **자가 흔든다** — 화소를 ±1 만 밀면 %.1f~%.1f 로 움직인다"
                        u"(T437 ✂ · `--jitter`). 순서를 믿기 전에 원작 PNG 를 봐라" % sp)
            if n in MEASURED_NOTES:
                tag += u"  · ⓘ %s" % MEASURED_NOTES[n]
            hit = rows_hit(shots_dir, n)
            if hit and hit[1] and float(hit[0]) / hit[1] >= ROWS_OK_MIN:
                tag += (u"  · ⓘ 구분선 줄은 **%d/%d 맞다** — «나란히 밀린 줄» 갈래가 아니니 "
                        u"**상자 높이·여백**부터 봐라(T28 97회차)" % hit)
            print(u"    %-18s %4.1f / 천장 %4.1f%s  — 달성 %3.0f%%%s"
                  % (n, v, c, note, (100.0 * v / c) if c else 0.0, tag))
        if len(bad) > len(low):
            print(u"    (%s점 미만 %d개 중 다섯만 적었다 — **달성률이 낮은 순**이다 · 절대 점수가 아니다)" % (PASS_MARK, len(bad)))
        st = [n for n, _v in scores if n in STALE_SCREENS]
        if st:
            print(u"    ⚠ 맨 뒤로 밀어 둔 화면 %d개(낮아도 쪼지 마라 · 원작 샷과 정본이 서로 다른 물건을 그린다):" % len(st))
            for n in st:
                c = _ceil_med(n)
                v = dict(scores)[n]
                print(u"        %-18s %4.1f / 천장 %4.1f  — 달성 %3.0f%%  · %s"
                      % (n, v, c, (100.0 * v / c) if c else 0.0, STALE_SCREENS[n]))
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
    # 캔버스는 **9:16**(112x199)이다 — 69회차에 `read_layout` 이 앱 상자 기준으로 바뀌어,
    # 9:16 이 아닌 캔버스를 쓰면 이 칸들이 «레터박스» 를 재게 된다(그건 ⑮-b 가 따로 잰다).
    a = _canvas(112, 199)
    _stripes(a, 0, 20, 112, 40)      # 밴드 y 10~20%
    _stripes(a, 11, 120, 45, 160)    # 밴드 y 60~80% 의 왼쪽 블록
    _stripes(a, 67, 120, 101, 160)   # 같은 밴드 오른쪽 블록
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
    b = _canvas(112, 199)
    _stripes(b, 0, 30, 112, 50)      # 밴드·블록을 통째로 5%p 아래로
    _stripes(b, 11, 130, 45, 170)
    _stripes(b, 67, 130, 101, 170)
    s2, why = score_screen(ra, read_layout(b))
    chk(s2 < PASS_MARK, u"밴드가 5%%p 밀리면 %s점 미만 (낸 점수 %.2f)" % (PASS_MARK, s2))
    chk(any(u"어긋남" in w for w in why), u"어긋난 값을 이유로 찍는다")

    # ⑥ 점수: 원작에 없는 밴드가 하나 늘면 감점된다
    c = _canvas(112, 199)
    _stripes(c, 0, 20, 112, 40)
    _stripes(c, 11, 120, 45, 160)
    _stripes(c, 67, 120, 101, 160)
    _stripes(c, 0, 180, 112, 195)
    s3, why3 = score_screen(ra, read_layout(c))
    chk(s3 < 10.0 and any(u"원작에 없는" in w for w in why3),
        u"클론에만 있는 요소는 감점이다 (낸 점수 %.2f)" % s3)

    # ⑥-b 천장: 같은 그림은 10 이고, 군더더기가 생기면 천장이 **점수와 같이** 내려간다
    #   (달성률은 100%% 그대로 — 군더더기는 배치가 틀린 것이 아니다 · 결정 686)
    chk(abs(score_ceiling(ra, read_layout(a)) - 10.0) < 1e-9,
        u"천장: 같은 그림은 10.0")
    cc = score_ceiling(ra, read_layout(c))
    chk(cc < 10.0 and abs(cc - s3) < 1e-9,
        u"천장: 군더더기만 늘어난 화면은 천장 = 점수다(달성률 100%%) — 천장 %.2f · 점수 %.2f" % (cc, s3))
    chk(score_ceiling(ra, read_layout(b)) > s2,
        u"천장: 자리가 밀린 화면은 천장이 점수보다 높다(고치면 오른다)")

    # ⑦ 딤 위에 뜬 모달처럼 «모든 행이 문턱을 넘는» 화면도 골짜기에서 갈린다
    d = _canvas(112, 199, (60, 60, 60))          # 딤 바탕
    _fill(d, 13, 20, 99, 180, (250, 250, 250))   # 흰 판(화면의 80%)
    _stripes(d, 22, 40, 90, 60)                  # 판 안 내용 두 줄
    _stripes(d, 22, 120, 90, 140)
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
    chk(len(pr) >= 30 and pr[0][0] == "main" and pr[0][1] == "screens/shot-042120.png",
        u"짝 표 첫 줄이 main ↔ screens/shot-042120.png 다 (줄 %d)" % len(pr))

    # ⑧-2 «원작 파일» 칸은 `ref/` 에서 잰 상대 경로다 — 숫자만이면 여태 꼴, 경로면 그 폴더 (T393 3회차)
    chk(ref_rel("042120") == "screens/shot-042120.png",
        u"숫자 칸은 screens/shot-<번호>.png 로 푼다")
    chk(ref_rel("shots/ascend-entry-rows.png") == "shots/ascend-entry-rows.png",
        u"경로 칸은 ref/ 아래 다른 폴더를 그대로 가리킨다")
    chk(ref_rel("shot-042120.png") == "screens/shot-042120.png",
        u"폴더 없는 파일 이름은 여태처럼 screens/ 로 본다")
    chk(ref_rel(None) is None and ref_rel("") is None and ref_rel("아무거나") is None,
        u"빈 칸·모르는 꼴은 짝이 없다(None)")

    # ⑧-4 틀 검사는 **앱 상자** 비로 본다 — 통짜 비로 보면 레터박스·기기 크롬이 멀쩡한 화면을 울린다
    chk(abs(app_ratio(540, 960) - 16.0 / 9.0) < 0.001,
        u"9:16 그림은 앱 상자도 9:16 (%.4f)" % app_ratio(540, 960))
    chk(abs(app_ratio(430, 860) - 16.0 / 9.0) < 0.01 and abs(860 / 430.0 - 16.0 / 9.0) > 0.2,
        u"기기 샷 430x860 은 통짜로는 2.000 인데 앱 상자는 9:16 이다 (%.4f)" % app_ratio(430, 860))
    chk(abs(app_ratio(499, 804) - 16.0 / 9.0) < 0.01,
        u"레터박스 낀 원작 샷도 앱 상자는 9:16 이다 (%.4f)" % app_ratio(499, 804))

    # ⑧-3 정본 SCREENS 에 줄이 없는 **클론 전용 화면**도 짝 표에 선다 —
    #      안 덧대면 촬영은 되는데 채점에서 통째로 빠진다(승천이 그랬다 · T393)
    cs = clone_pairs()
    if cs:
        names = set(n for n, _ in pr)
        chk(all(n in names for n, _ in cs),
            u"클론 촬영 %d개가 전부 짝 표에 있다" % len(cs))
        asc = dict(pr).get("ascend")
        chk(asc == "shots/ascend-entry-rows.png",
            u"승천은 정본 ref/shots 시트를 짝으로 쥔다 (%s)" % asc)

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

    chk(all(isinstance(k, type(u"")) and len(v) > 40 and (u"%H" in v or u"%W" in v)
            for k, v in MEASURED_NOTES.items()) and set(MEASURED_NOTES) & set(dict(pairs())),
        u"«여기까지 재 봤다» 표(MEASURED_NOTES) %d칸이 다 «잰 수» 를 들고 있고 짝 표에 있는 화면이다" % len(MEASURED_NOTES))
    chk(not (set(MEASURED_NOTES) & set(STALE_SCREENS)),
        u"MEASURED_NOTES 와 STALE_SCREENS 가 안 겹친다(하나는 «재 봤다» · 하나는 «뒤로 민다» — 뜻이 다르다)")
    # T28 141회차 — 기술 노드 화면에서 워커 둘이 헛짚기 쉬운 자리 둘(없는 [취소] 버튼 · 파란 채움 0%)을
    #   각각 «원작 샷이 다른 자리»(STALE_REF_NOTES)와 «여기까지 재 봤다»(MEASURED_NOTES)에 적어 뒀다.
    #   둘 중 하나라도 지워지면 다음 워커가 같은 헛다리를 다시 판다 — 그래서 자로 묶어 둔다.
    # T28 142회차 — `app_box` 의 돌려주는 꼴을 `(x0,y0,x1,y1)` 로 잘못 읽어 «없는 결함» 을 등재할 뻔했다.
    #   레터박스가 큰 기기 샷(ascend 원작 430x860)에서만 드러나므로 자로 못박는다.
    # T28 144회차 — 글자 자가 아이콘 테를 글자로 집는 덫(T478 등재문의 수가 그래서 틀렸다).
    _g = _canvas(40, 20, (255, 255, 255))
    _fill(_g, 2, 4, 10, 16, (255, 223, 128))      # 별 아이콘 면(노랑) — x 2~10 · y 4~16
    _fill(_g, 2, 4, 4, 16, (128, 64, 0))          # 그 왼쪽 테(갈색 · 어둡지만 색이 있다)
    _fill(_g, 20, 6, 30, 15, (22, 22, 28))        # 진짜 글자(무채색 어둠) — x 20~30 · y 6~15
    _fill(_g, 12, 4, 14, 16, (8, 8, 8))           # 그 아이콘의 **무채색 검정 테** — 색 거르기로는 안 빠진다
    chk(glyph_box(_g, 0, 40, 0, 20) == (12, 30, 4, 16),
        u"glyph_box 의 색 거르기만으로는 **아이콘의 무채색 테를 못 뺀다**(별 테 #080808 이 상자에 든다 — T478 이 그래서 틀렸다)")
    chk(glyph_box(_g, 0, 40, 0, 20, ink=(22, 22, 28)) == (20, 30, 6, 15),
        u"glyph_box 에 **글자 색을 주면** 아이콘 테를 다 버리고 글자만 잰다(규약: ink_census 로 세고 ink= 로 준다)")
    chk([c for c, n in ink_census(_g, 0, 40, 0, 20)].count((255, 223, 128)) == 1,
        u"ink_census 는 창 안 색을 많은 차례로 돌려준다(글자를 재기 전에 치는 자)")
    chk(glyph_box(_canvas(8, 8, (255, 255, 255)), 0, 8, 0, 8) is None,
        u"glyph_box 는 글자가 없으면 None 을 낸다(빈 상자를 0 으로 안 센다)")

    # T28 148회차 — 둥근 모서리에 안 속는 카드 높이 자(145·146회차가 «테에서 6px 한 열» 로 재서 클론만 짧게 읽었다).
    _cw = _canvas(200, 160, (20, 20, 20))
    for _yy in range(30, 130):                      # 흰 카드 x 40..160 · y 30..130 · 모서리 반지름 12 흉내
        _inset = 0
        if _yy < 42:
            _inset = 12 - int((144 - (42 - _yy) ** 2) ** 0.5) if (42 - _yy) <= 12 else 12
        elif _yy > 118:
            _inset = 12 - int((144 - (_yy - 118) ** 2) ** 0.5) if (_yy - 118) <= 12 else 12
        _fill(_cw, 40 + _inset, _yy, 160 - _inset, _yy + 1, (255, 255, 255))
    _fill(_cw, 45, 70, 155, 90, (30, 30, 30))       # 카드를 가로지르는 내용(버튼)
    _cb = card_white_box(_cw, 40, 160)
    chk(_cb is not None and _cb[1] - _cb[0] >= 88,
        u"card_white_box 는 카드를 가로지르는 내용이 있어도 흰 면을 한 덩어리로 낸다(둥근 모서리 자)")
    _col = [1 if _cw.px[((_y * 200) + (160 - 6)) * 3] > 200 else 0 for _y in range(160)]
    _runs, _s = [], None
    for _i, _v in enumerate(_col + [0]):
        if _v and _s is None:
            _s = _i
        elif not _v and _s is not None:
            _runs.append(_i - _s)
            _s = None
    chk(max(_runs) < _cb[1] - _cb[0],
        u"«테에서 6px 한 열» 옛 자는 같은 카드를 **더 짧게** 읽는다(둥근 모서리 몫 — T28 145·146회차가 그 덫에 걸렸다)")
    chk(card_white_box(_canvas(40, 40, (20, 20, 20)), 5, 35) is None,
        u"card_white_box 는 카드가 없으면 None 을 낸다")

    # ── T512 1회차 — «낡은 원작 샷» 25줄의 정본 근거를 기계가 검산한다(걸음 ⓐ) ──────
    _heads = [note_head(s) for s, _b in STALE_REF_NOTES]
    chk(len(set(_heads)) == len(_heads),
        u"«낡은 원작 샷» %d줄의 머리(`' — '` 앞)가 서로 다르다 — 근거 표의 키가 될 수 있다" % len(_heads))
    chk(set(STALE_REF_EVIDENCE) <= set(_heads),
        u"근거 표의 키 %d개가 전부 그 목록의 머리다(오타가 있으면 그 근거는 **아무 줄에도 안 붙는다**)"
        % len(STALE_REF_EVIDENCE))
    chk(all(isinstance(e, tuple) and len(e) == 3 and e[0] and isinstance(e[1], int) and e[2]
            for v in STALE_REF_EVIDENCE.values() for e in v)
        and all(e[0].startswith(u"css/") or e[0].startswith(u"js/")
                for v in STALE_REF_EVIDENCE.values() for e in v),
        u"근거 %d개가 (`web/` 아래 경로, 줄, 바늘) 꼴이다"
        % sum(len(v) for v in STALE_REF_EVIDENCE.values()))

    #   ⓐ 바늘이 있으면 조용하다 · ⓑ **일부러 고친 사본**에서는 «사라졌다» 를 잡는다(판정의 그 칸).
    _ev1 = {u"자기 검사": ((u"css/style.css", 10, u".settings-toggle.on { background: var(--pp-blue); }"),)}
    _good = u"\n".join([u"/* 머리 */"] * 9 + [u".settings-toggle.on { background: var(--pp-blue); }"])
    _bad = _good.replace(u"var(--pp-blue)", u"#00c853")          # 정본이 색을 바꾼 흉내
    chk(evidence_problems(lambda _p: _good, _ev1) == [],
        u"근거 검산은 바늘이 그대로면 조용하다")
    _p = evidence_problems(lambda _p: _bad, _ev1)
    chk(len(_p) == 1 and u"사라졌다" in _p[0] and u"settings-toggle" in _p[0],
        u"근거 검산은 **일부러 고친 사본**에서 «사라졌다» 를 잡는다(이 칸이 T512 판정 ⓐ 다)")
    chk(len(evidence_problems(lambda _p: None, _ev1)) == 1,
        u"근거 검산은 파일을 못 읽어도 조용히 지나가지 않는다")
    #   ⓒ 줄 번호만 밀린 것은 **알리기만** 한다 — 근거가 사라진 것과 다른 일이다(NOTE_LINE_DRIFT).
    _far = u"\n".join([u"/* 머리 */"] * (10 + NOTE_LINE_DRIFT + 5)
                      + [u".settings-toggle.on { background: var(--pp-blue); }"])
    _p2 = evidence_problems(lambda _p: _far, _ev1)
    chk(len(_p2) == 1 and _p2[0].startswith(u"⚠") and u"줄 번호가 낡았다" in _p2[0],
        u"줄 번호만 %d줄 넘게 밀린 것은 ⚠ 로 알리기만 한다(막지 않는다)" % NOTE_LINE_DRIFT)
    #   ⓓ 바늘은 그 자리에서만 나와야 한다 — 정본 사본이 옆에 있을 때만 센다(CI 자기 검사 잡엔 없다).
    _web = os.path.join(REPO, ".wwwww-src", "web")
    if os.path.isdir(_web):
        _dup = []
        for _h in sorted(STALE_REF_EVIDENCE):
            for _path, _line, _needle in STALE_REF_EVIDENCE[_h]:
                try:
                    _t = open(os.path.join(_web, _path), encoding="utf-8").read()
                except Exception:
                    continue
                _n = sum(1 for _l in _t.split(u"\n") if _needle in _l)
                if _n != 1:
                    _dup.append(u"%s · %s · %d번" % (_h, _needle[:40], _n))
        chk(not _dup, u"근거 바늘이 정본에서 **한 번만** 나온다(여러 번이면 인용한 줄이 어느 것인지 모른다): %s"
            % u" / ".join(_dup[:3]))
        chk(verify_stale_notes(_web) == 0,
            u"정본 실물에서 근거 %d개가 전부 살아 있다" % sum(len(v) for v in STALE_REF_EVIDENCE.values()))
    else:
        print(u"  · (근거 실물 검산 두 칸은 정본 사본이 없어 건너뛴다 — `--verify-notes` 로 따로 돈다)")

    # ── T512 2회차 — `--vs` 의 조각들(걸음 ⓑ · 그림 없이 재는 것만) ─────────────────
    _css = u"\n".join([
        u":root { --pp-blue: #005dff; --pp-gray: #c4c4c4; }",
        u"/* 주석 줄 — 선택자가 아니다 */",
        u".settings-toggle { background: var(--pp-gray); }",
        u".settings-toggle.on { background: var(--pp-blue); }",
        u".modal-card .settings-toggle.on { background: #111; }",
        u".shop-sub { text-align: center; font-size: .8rem; margin: .34rem 0 1.42rem; }",
        u"@keyframes blink { 0% { background: #fff; } }",
    ])
    _d = css_decls_for(_css, u".settings-toggle.on")
    chk(len(_d) == 1 and _d[0][0] == 4 and _d[0][1] == u"background" and _d[0][2] == u"var(--pp-blue)",
        u"css_decls_for 는 **그 선택자 그대로**인 규칙만 집는다(조상이 붙은 `.modal-card …` 은 다른 선택자다 · 줄 번호는 선택자가 선 줄)")
    chk(css_decls_for(_css, u"0%") == [] and css_decls_for(_css, u"blink") == [],
        u"css_decls_for 는 `@keyframes` 와 그 단계를 선택자로 세지 않는다")
    _tk = css_tokens(_css)
    chk(_tk.get(u"--pp-blue") == u"#005dff",
        u"css_tokens 가 `--pp-blue: #005dff` 를 읽는다")
    chk(resolve_css_value(u"var(--pp-blue)", _tk) == u"#005dff"
        and resolve_css_value(u"var(--nope, #abcdef)", _tk) == u"#abcdef"
        and resolve_css_value(u"var(--rc, #ffd54f)", {}) == u"#ffd54f",
        u"resolve_css_value 가 토큰을 풀고, 없으면 **대체값**을 쓴다(`var(--rc, #ffd54f)` 꼴)")
    chk(css_color_rgb(u"#005dff") == (0, 93, 255) and css_color_rgb(u"#fff") == (255, 255, 255)
        and css_color_rgb(u"rgba(4,255,0,.5)") == (4, 255, 0) and css_color_rgb(u".8rem") is None,
        u"css_color_rgb 는 `#rrggbb`·`#rgb`·`rgba()` 를 풀고 길이에는 None 을 낸다")
    chk([n for _t, n in css_rem_lengths(u".34rem 0 1.42rem")] == [0.34, 1.42],
        u"css_rem_lengths 는 `0`(단위 없음)을 길이로 세지 않는다")
    chk(abs(REM_PCT_H - 1.8957) < 1e-3 and abs(REM_PCT_W - 3.3703) < 1e-3,
        u"1rem = 1.90%H = 3.37%W (정본 `main.js` 17 `fontSize = h/844*16` · 앱은 늘 9:16)")
    #   덩어리 가르기 — **이 칸이 2회차의 교훈이다**: 통째 중앙값이면 «같다» 로 묻힌다.
    _blob = [(x, y) for x in range(10, 20) for y in range(10, 20)] \
        + [(x, y) for x in range(60, 70) for y in range(60, 70)] \
        + [(100, 100)]
    _cs = sorted((len(c) for c in _clusters(_blob)), reverse=True)
    chk(_cs == [100, 100, 1],
        u"_clusters 는 떨어진 덩어리를 가른다(붙은 100 + 100 + 외톨이 1)")
    chk(len(_clusters([(0, 0), (2, 0)])) == 1 and len(_clusters([(0, 0), (5, 0)])) == 1 + 1,
        u"_clusters 의 이음 거리는 2 자리다(3 이상 벌어지면 다른 덩어리)")
    chk(_median_rgb([(0, 0, 0), (4, 255, 0), (255, 255, 255)]) == (4, 255, 0)
        and _median_rgb([]) is None,
        u"_median_rgb 는 채널마다 중앙값을 내고 빈 표본엔 None 을 낸다")

    # ── T512 3회차 — `--scan-ref` 의 조각들(걸음 ⓒ) ────────────────────────────────
    _fc = face_color_table(u"\n".join([
        u":root { --pp-blue: #005dff; }",
        u".settings-toggle.on { background: var(--pp-blue); }",
        u".btn.equip { background: var(--pp-blue); }",
        u".x { color: #005dff; }",                      # 잉크는 면이 아니다
        u".y { background: linear-gradient(#fff, #000); }",   # 겹은 색 한 값이 아니다
    ]))
    chk(sorted(s for s, _l in _fc.get((0, 93, 255), [])) == [u".btn.equip", u".settings-toggle.on"],
        u"face_color_table 은 **토큰도 풀어** 면 색을 모은다(판정의 그 자리가 토큰 갈래다)")
    chk((0, 0, 0) not in _fc and (255, 255, 255) not in _fc,
        u"face_color_table 은 `color:`(잉크)와 `linear-gradient`(겹)를 면 색으로 안 센다")
    chk(_chroma((0, 93, 255)) == 255 and _chroma((4, 238, 0)) == 238 and _chroma((35, 35, 35)) == 0,
        u"_chroma 는 유채색을 가른다(색 갈래 ↔ 어둠·판 갈래의 금 = CHROMA_HUE %d)" % CHROMA_HUE)

    _lb = _WH(430, 860)
    _bx, _by, _bw, _bh = app_box(_lb)
    chk((_bx, _by, _bw, _bh) == (0, 48, 430, 764),
        u"app_box 는 (x0, y0, **폭**, **높이**) 를 돌려준다 — 기기 샷 430x860 → (0, 48, 430, 764)")
    chk(abs(app_rel(_lb, y=48) - 0.0) < 1e-9
        and abs(app_rel(_lb, y=48 + 764) - 100.0) < 1e-9
        and abs(app_rel(_lb, x=215) - 50.0) < 1e-9,
        u"app_rel 은 앱 상자 위끝을 0%H · 아래끝을 100%H 로 낸다(레터박스를 뺀 밑변)")
    chk(u"tech-node" in MEASURED_NOTES
        and u"상태" in MEASURED_NOTES[u"tech-node"]
        and any(u"취소" in t[0] and u"only-child" in t[1] for t in STALE_REF_NOTES),
        u"기술 노드의 헛다리 둘(없는 [취소] 버튼 · 파란 채움 0%)이 둘 다 적혀 있다")
    chk(len(STALE_REF_NOTES) >= 2
        and all(len(t) == 2 and t[0] and u"정본" in t[1] for t in STALE_REF_NOTES),
        u"«원작 샷이 지금 정본과 다른 자리» 주석 %d개가 (한 줄, 자세히) 꼴로 살아 있다" % len(STALE_REF_NOTES))

    # ⑩-b 낡은 샷 화면 표: 이름마다 까닭이 있고, 그 화면은 «다음 볼 화면» 정렬에서 맨 뒤로 간다(T28 81회차)
    chk(bool(STALE_SCREENS) and all(isinstance(k, type(u"")) and v for k, v in STALE_SCREENS.items()),
        u"낡은 샷 화면 %d개가 이름·까닭 꼴로 살아 있다" % len(STALE_SCREENS))

    # ⑩-c 전역 손질 알림: 반수 이상의 화면이 움직이면 «깨진 화면 하나» 로 읽지 않는다(결정 712)
    def _global_call(nscreens, nmoved):
        return nmoved >= max(3, nscreens * 0.5)
    chk(_global_call(30, 30) and _global_call(30, 15) and not _global_call(30, 14)
        and not _global_call(4, 2) and _global_call(6, 3),
        u"전역 손질 알림은 **반수 이상**(최소 셋)이 움직였을 때만 된다")

    # ⑩-d 천장은 자취 중앙값으로 읽는다 — 한 회차의 천장은 밴드 쪼개짐이 바뀌면 통째로 흔들린다(결정 722)
    chk(ceil_median([], 5.0) == 5.0 and ceil_median([4.0], 5.0) == 5.0,
        u"천장: 자취가 셋 미만이면 이번 회차 값을 그대로 쓴다(새 화면)")
    chk(abs(ceil_median([2.75, 2.86, 2.83], 4.89) - 2.86) < 1e-9,
        u"천장: 튄 한 회차(4.89)가 아니라 중앙값(2.86)을 쓴다 — `offline` 런 859~898 실측")
    chk(abs(ceil_median([4.30, 4.28, 4.28], 6.23) - 4.30) < 1e-9,
        u"천장: 위로 튄 회차(6.23)도 중앙값이 눌러 준다 — `profile` 실측")
    _ceil = {u"a": 5.0, u"b": 5.0}
    _sc = [(u"a", 1.0), (u"b", 4.0)]          # a 가 훨씬 낮지만 a 를 낡은 샷으로 치면
    def _r(t, stale):
        base = t[1] / _ceil[t[0]]
        return (base + 10.0) if t[0] in stale else base
    chk([n for n, _ in sorted(_sc, key=lambda t: _r(t, set()))] == [u"a", u"b"]
        and [n for n, _ in sorted(_sc, key=lambda t: _r(t, {u"a"}))] == [u"b", u"a"],
        u"낡은 샷 화면은 달성률이 꼴째여도 맨 뒤로 밀린다")

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

    # ── 요소 수 자취(T28 98회차) ───────────────────────────────────────
    import tempfile
    _fd, _bp = tempfile.mkstemp(suffix=".json")
    os.close(_fd)
    try:
        save_baseline_file(_bp, [("a", 5.0)], 5.0, run=1, elems={"a": 43})
        got = load_baseline(_bp)
        chk(got.get("_elems", {}).get("a") == 43, u"요소 수는 기준선에 남고 다시 읽힌다")
        save_baseline_file(_bp, [("a", 5.0)], 5.0, run=2)
        chk(load_baseline(_bp).get("_elems") == {}, u"안 적은 회차의 요소 수는 빈 칸이다(옛 파일도 안 넘어진다)")
    finally:
        os.remove(_bp)

    # ── rows_hit / ROWS_OK_MIN (T28 97회차) ────────────────────────────
    chk(0.0 < ROWS_OK_MIN < 1.0, u"«밀린 줄 갈래가 아니다» 문턱은 비율이다(0~1)")
    chk(8.0 / 9 >= ROWS_OK_MIN and 3.0 / 8 < ROWS_OK_MIN,
        u"97회차 실측이 그 문턱을 가른다 — `league-challenge` 8/9 는 넘고 `craft-compare` 3/8 은 못 넘는다")
    chk(6.0 / 7 >= ROWS_OK_MIN,
        u"🚫 이 표시는 «결함 없음» 이 아니다 — `dungeon-detail` 은 6/7 로 넘는데 T433(−11.4px)이 등재돼 있다")
    chk(rows_hit("/없는/자리", "league-challenge") is None,
        u"샷이 없으면 조용히 None 이다(목록을 넘어뜨리지 않는다)")
    chk(rows_hit(".", "없는화면이름") is None, u"모르는 화면 이름도 None 이다")

    # ── PNG 바이트 지문 (T502) ──────────────────────────────────────────
    # 등재문 ⓓ: «지문이 같은데 점수가 달라졌다 → 자 결함 / 지문이 다르면 런 흔들림» 을 **자가** 가른다.
    _sl = (u"%s  screen_main.png\n"
           u"%s  screen_shop.png\n"
           u"# 주석 줄\n"
           u"짧은줄\n"
           u"deadbeef  screen_bad.png\n"
           u"%s  notes.txt\n"
           u"%s *screen_forge.png\n" % ("a" * 64, "b" * 64, "c" * 64, "d" * 64))
    _p = parse_sha_list(_sl)
    chk(_p == {"main": "a" * 64, "shop": "b" * 64, "forge": "d" * 64},
        u"`sha256sum` 장부를 화면 이름으로 읽는다 — 주석·짧은 줄·짧은 sha·PNG 아닌 줄은 넘기고 `-b` 의 `*` 도 떼낸다(%s)" % sorted(_p))
    chk(parse_sha_list(u"") == {} and parse_sha_list(None) == {},
        u"장부가 없거나 비어도 조용히 빈 dict 다(낡은 screens·긁개 폴더)")
    _old = {"main": "a" * 64, "shop": "b" * 64, "chat": "e" * 64}
    _new = {"main": "a" * 64, "shop": "f" * 64}
    _sm, _df, _un = sha_split(_old, _new, {"main", "shop", "chat", "pass"})
    chk(_sm == ["main"] and _df == ["shop"] and _un == ["chat", "pass"],
        u"같다/다르다/모른다 셋으로 가른다 — 한쪽에만 있는 화면은 «다르다» 가 아니라 **모른다** 다(%s / %s / %s)"
        % (_sm, _df, _un))
    chk(sha_split({}, _new, {"main"}) == ([], [], ["main"]),
        u"앞 런 지문이 아예 없으면 전부 «모른다» — «안 달라졌다» 로 읽지 않는다")
    _b2 = os.path.join(tempfile.mkdtemp(prefix="uiscore_sha_"), "b.json")
    save_baseline_file(_b2, [("main", 4.6)], 4.6, run=1352, shot_sha={"main": "a" * 64, "bad": "zz"})
    _l2 = load_baseline(_b2)
    chk((_l2.get("_sha") or {}) == {"main": "a" * 64},
        u"기준선 왕복 — 64자 sha 만 적고 읽는다(꼴이 틀린 칸은 버린다 · %s)" % (_l2.get("_sha"),))
    chk(load_baseline(os.path.join(os.path.dirname(_b2), u"없는파일.json")) == {},
        u"기준선 파일이 없으면 빈 dict 다(지문 칸을 찾다가 멎지 않는다)")
    save_baseline_file(_b2, [("main", 4.6)], 4.6, run=1353)
    chk((load_baseline(_b2).get("_sha") or {}) == {},
        u"지문을 안 넘기면 칸을 안 만든다 — 낡은 기준선과 섞여도 «모른다» 로 떨어진다")

    # ── T514 — «지문은 그대로인데 점수가 움직였다» ──────────────────────
    # 바이트로는 이 증거가 안 잡힌다(채점 화면은 실측 0/31 로 런마다 다르다) — 그래서 지각 지문으로 센다.
    _b = {"main": 5.0, "shop": 4.0, "chat": 3.0, "pass": 2.0, "_avg": 3.5}
    _c = {"main": 4.0, "shop": 4.0, "chat": 1.0, "pass": 2.0}
    _same_fp = [10] * (FP_ROWS * FP_COLS)    # 안·뒤 둘 다 차 0 이 되게 같은 지문을 쓴다(길이가 다르면 fp_diff 가 None 이다)
    _far_fp = [200] * (FP_ROWS * FP_COLS)
    _of = {"main": _same_fp, "shop": _same_fp, "chat": _same_fp, "pass": _same_fp}
    _nf = {"main": _same_fp, "shop": _same_fp, "chat": _far_fp, "pass": _same_fp}
    _q = fp_quiet_moved(_b, _c, _of, _nf)
    chk([t[0] for t in _q] == ["main"],
        u"지문이 같고 점수가 %.1f 넘게 움직인 화면만 센다 — `shop`(안 움직임)·`chat`(지문이 달라졌다)·`pass`(둘 다 아님)는 빼고(%s)"
        % (DROP_MARK, [t[0] for t in _q]))
    chk(len(_q) == 1 and _q[0][1] == 5.0 and _q[0][2] == 4.0 and _q[0][3] < FP_SAME and _q[0][4] < FP_SAME,
        u"칸은 (이름, 전, 후, 안, 뒤)이고 지문 차 둘이 %s 아래다(%s)" % (FP_SAME, _q[0] if _q else None))
    chk(fp_quiet_moved(_b, _c, {}, _nf) == [],
        u"앞 회차 지문이 없으면 아무것도 안 센다 — «모른다» 를 «같다» 로 읽지 않는다")
    chk(fp_quiet_moved({}, _c, _of, _nf) == [] and fp_quiet_moved(_b, {}, _of, _nf) == [],
        u"한쪽 점수가 비면 조용하다")
    _q2 = fp_quiet_moved(_b, _c, _of, _nf, mark=0.0)
    chk([t[0] for t in _q2] == ["main", "pass", "shop"],
        u"문턱을 0 으로 내리면 지문이 같은 화면 전부가 든다 — 차가 큰 것부터이고 **차가 같으면 이름 차례**다"
        u"(0 인 `pass`·`shop` 은 이름 순 · %s)" % [t[0] for t in _q2])

    # ── drop_note 의 차례 (T28 90회차) ─────────────────────────────────
    # 실측 자리: `profile` 은 지문 0.15/0.10(서른한 장 중 가장 작다)인데 중앙값도 걸려서
    # 옛 자는 «중앙값 자리다» 만 말했다 — 그 문장은 자가 흔들린 것을 감춘다.
    w = drop_note(0.15, 0.10, 3.2, 6, 3.1, 4.3)
    chk(w is not None and u"그림이 거의 같다" in w and u"중앙값 3.2" in w,
        u"그림이 같고 중앙값도 걸리면 «그림이 거의 같다» 를 먼저 말하고 중앙값을 덧붙인다")
    chk(w.index(u"그림이 거의 같다") < w.index(u"중앙값 3.2"),
        u"두 까닭이 같이 참이면 지문 쪽이 앞에 선다(차례가 뜻이다)")
    w2 = drop_note(2.4, 0.1, 3.2, 6, 3.1, 4.3)
    chk(w2 is not None and u"그림이 거의 같다" not in w2 and u"중앙값" in w2,
        u"그림이 달라졌는데 중앙값 자리면 중앙값만 말한다(옛 문장 그대로)")
    chk(drop_note(0.2, 0.2, None, 0, 3.1, 4.3) is not None,
        u"자취가 없어도 지문이 같으면 soft 다")
    chk(drop_note(2.4, 2.4, 9.0, 6, 3.1, 4.3) is None,
        u"그림이 달라졌고 중앙값에서도 멀면 hard(진짜 회귀)다")
    chk(u"보스 경고" in (drop_note(2.4, 2.4, 9.0, 6, 3.1, 4.3, bw=0.6) or u""),
        u"씬 대역이 붉으면 그 까닭이 다른 모든 갈래를 이긴다")
    chk(u"배경만으로도" in (drop_note(None, None, None, 0, 3.1, 3.4, bg=0.4) or u""),
        u"지문이 없는 첫 회차에는 배경 물러섬 표를 쓴다")
    chk(drop_note(None, None, None, 0, 3.1, 4.3, bg=0.4) is None,
        u"그 물러섬은 표의 상한을 넘으면 안 먹는다")
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

    # ⑬ 흔들리는 화면 집어내기(T28 48회차) — 자취의 폭이 문턱을 넘으면 이름을 댄다
    hist_ = [(1, {"a": 3.6, "b": 5.0}), (2, {"a": 6.8, "b": 5.1}), (3, {"a": 4.8, "b": 5.0})]
    sh = [n for n in ("a", "b")
          if len([h[1][n] for h in hist_ if n in h[1]]) >= 3
          and max(h[1][n] for h in hist_) - min(h[1][n] for h in hist_) >= SHAKY_SPREAD]
    chk(sh == ["a"], u"자취 폭이 %.1f 넘는 화면만 «흔들린다» 로 집는다 (%s)" % (SHAKY_SPREAD, sh))
    chk(abs(median_of(hist_, "a") - 4.8) < 1e-9,
        u"흔들리는 화면은 중앙값으로 읽는다 (%.1f)" % median_of(hist_, "a"))
    # 계단 ↔ 흔들림 가르기(T28 54회차) — 한쪽으로만 내려간 것은 흔들림이 아니다
    chk(_flaps([6.0, 6.0, 4.5, 4.7, 3.7, 6.8]), u"오르내리는 자취는 «흔들린다»(실측 `settings`)")
    chk(not _flaps([9.3, 9.3, 8.6, 8.6, 8.6, 5.8]),
        u"한쪽으로만 내려간 계단은 «흔들린다» 가 아니다(실측 `dungeons` — 포스트 한 겹)")
    chk(not _flaps([5.0, 5.0, 5.0]), u"안 움직인 자취도 «흔들린다» 가 아니다")
    chk(not _flaps([5.0, 5.3, 5.1]), u"문턱(%.1f) 안쪽 오르내림은 안 센다" % DROP_MARK)

    # ⑭ «뒤 화면이 비치는가» 밝기비(T28 57회차 · T358 ✂ 에서 배운 것)
    bk = _canvas(120, 200, (200, 200, 200))
    _fill(bk, 10, 20, 110, 90, (120, 160, 240))
    fr = _canvas(120, 200, (100, 100, 100))          # 위 절반 = 뒤 화면의 딱 절반 밝기
    _fill(fr, 10, 20, 110, 90, (60, 80, 120))
    _fill(fr, 0, 120, 120, 200, (250, 250, 250))     # 아래 절반 = 앞 화면이 제 손으로 그린 것
    rs = behind_ratio(fr, bk, rows=8)
    up = [r for y, r, n in rs if y < 45]
    dn = [r for y, r, n in rs if y > 60]
    chk(up and all(BEHIND_LO <= r <= BEHIND_HI for r in up),
        u"뒤 화면이 α .5 로 비치는 줄은 비가 %.2f~%.2f 안이다 (%s)" % (BEHIND_LO, BEHIND_HI, [round(r, 2) for r in up]))
    chk(dn and all(r > BEHIND_HI for r in dn),
        u"앞 화면이 제 손으로 그린 줄은 그 밖이다 (%s)" % [round(r, 2) for r in dn])
    chk(behind_ratio(fr, _canvas(60, 60)) == [], u"크기가 다르면 비를 안 낸다")

    # ⑮ 구분선 줄 잡기(T28 59회차) — 가로로 한 색인 줄만 잡고, 글자가 든 줄은 안 잡는다
    # 진짜 화면처럼 카드가 재는 폭(16~84%)을 꽉 채우게 두고, 행마다 글자 덩이를 얹는다.
    # 캔버스를 **9:16**(225x400)으로 둔다 — 그래야 `app_box` 가 그림 전체를 앱으로 보고
    # 이 칸이 «레터박스» 가 아니라 «띠 잡기» 만 잰다(68회차에 sep_rows 가 앱 기준으로 바뀌었다).
    lst = _canvas(225, 400, (240, 240, 240))
    for y0 in (40, 140, 240, 340):                  # 글자가 든 행 넷 — 이 줄은 «한 색» 이 아니다
        _fill(lst, 56, y0, 169, y0 + 40, (30, 30, 30))
    rs = sep_rows(lst, 0.0, 1.0)
    tops = [round(a0) for a0, a1 in rs]
    chk(all(any(abs(t - g) <= 2 for t in tops) for g in (0, 20, 45, 70)),
        u"행 사이 여백 줄을 잡는다 (%s)" % tops)
    chk(not any(11 <= t <= 19 or 36 <= t <= 44 for t in tops),
        u"글자가 든 줄은 «구분선» 으로 안 잡는다 (%s)" % tops)

    # ⑮-b 앱 상자(T28 68회차) — 원작 샷은 9:16 이 아니라 레터박스가 섞여 있다
    chk(app_box(_canvas(540, 960)) == (0, 0, 540, 960),
        u"9:16 그림은 통째로 앱이다 (클론 샷 540x960)")
    _ab = app_box(_canvas(488, 890))                       # 실측 `pass` 원작 샷
    chk(_ab[2] == 488 and _ab[3] == 868 and _ab[1] == 11,
        u"세로로 긴 샷은 폭이 앱 폭이고 위아래가 레터박스다 (%s · pass 실측 488x890)" % (_ab,))
    _ab2 = app_box(_canvas(499, 804))                      # 실측 `chat` 원작 샷
    chk(_ab2[3] == 804 and _ab2[2] == 452 and _ab2[0] == 23,
        u"가로로 넓은 샷은 높이가 앱 높이이고 좌우가 레터박스다 (%s · chat 실측 499x804)" % (_ab2,))
    # 레터박스가 있으면 «같은 자리» 도 %H 가 달라진다 — 그것이 62~67회차의 밀림과 같은 크기다
    _lb = 100.0 * (890 - 868) / 2 / 890
    chk(1.0 < _lb < 1.5,
        u"pass 샷의 위 여백만으로 %.2f%%p 가 치우친다 — «나란히 밀린 줄» 과 같은 크기다" % _lb)

    # ⑯ 나란히 밀린 줄 묶기(T28 61회차) — 흩어진 어긋남과 «같은 크기로 밀림» 을 가른다
    def _grp(ds):
        g = {}
        for a0, d in ds:
            g.setdefault(round(d / SHIFT_BIN) * SHIFT_BIN, []).append((a0, d))
        return sorted((k for k in g if len(g[k]) >= SHIFT_MIN), key=lambda k: -len(g[k]))
    chk(_grp([(30.3, -1.8), (65.5, -1.8), (74.1, -1.8)]) == [-2.0],
        u"같은 크기로 세 줄이 밀리면 한 묶음이다 (실측 `main` −1.8)")
    chk(_grp([(10.0, -1.7), (20.0, 0.9), (30.0, 4.3)]) == [],
        u"흩어진 어긋남은 묶이지 않는다")
    chk(_grp([(10.0, 2.4), (20.0, 2.6)]) == [2.5],
        u"0.5 단위로 묶어 2.4·2.6 을 한 묶음으로 본다 (실측 `pass`)")

    # ⑯-b 차례를 지키는 짝짓기(T28 62회차) — 탐욕 짝짓기가 지어내던 가짜 «−4%p» 를 막는다
    # `skills` 실측 그대로: 원작 줄 여섯이 −1.3~−1.9%p 씩 나란히 밀려 있고 클론에 한 줄이 더 없다.
    o_ = [(16.5, 17.0), (18.8, 20.2), (26.4, 26.9), (28.7, 30.1), (36.4, 36.7), (38.5, 56.6)]
    c_ = [(14.6, 15.1), (17.5, 19.1), (24.6, 25.1), (27.5, 29.2), (34.7, 35.0), (37.8, 60.4)]
    pr_ = _align(o_, c_)
    chk(pr_ == [(0, 0), (1, 1), (2, 2), (3, 3), (4, 4), (5, 5)],
        u"나란히 밀린 줄은 차례대로 짝짓는다 (%s)" % pr_)
    ds_ = [c_[j][0] - o_[i][0] for i, j in pr_]
    chk(max(ds_) < 0 and min(ds_) > -2.0,
        u"그 짝의 어긋남은 전부 −1.3~−1.9%%p 다 — 탐욕이 지어내던 «−4.0» 이 아니다 (%s)"
        % [round(d, 1) for d in ds_])
    chk(_align([(10.0, 11.0)], [(30.0, 31.0)]) == [(0, -1)],
        u"SHIFT_MAX 보다 멀면 짝을 안 짓는다")
    chk([j for i, j in _align([(10.0, 11.0), (20.0, 21.0)],
                              [(9.5, 10.5), (14.0, 15.0), (20.5, 21.5)])] == [0, 2],
        u"가운데 낀 클론 줄은 건너뛰고 차례를 지킨다")

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

    # ㉑ 판독 원리 셋 (T444 · 결정 778) — 값 문턱 대신 «정수 건너뜀 수 + 그릇 수준 + 상대 잣대»
    #    ⓐ 2칸 차: 이웃끼리 13 씩 오르는 경사(2칸 차 26 ≥ 24)는 체커판 ±1 에 이웃 차가 12↔14 로 갈리지만 2칸 차는 그대로다.
    ramp = _canvas(40, 40, (100, 100, 100))
    for _x in range(10, 20):
        _fill(ramp, _x, 0, _x + 1, 40, (100 + 13 * (_x - 9),) * 3)
    e0 = _row_edges(ramp, 0, 40, 0, 40)
    e1 = _row_edges(_jitter(ramp, 1, True), 0, 40, 0, 40)
    chk(e0 == e1 and e0[0] > 0,
        u"건너뜀 수는 체커판 ±1 에 안 움직인다 (행마다 %d · 흔든 뒤 %d)" % (e0[0], e1[0]))
    _n1 = sum(1 for _x in range(10, 20) if abs((100 + 13 * (_x - 9)) - (100 + 13 * (_x - 10) if _x > 10 else 100)) >= 24)
    chk(_n1 == 0 and e0[0] >= 8,
        u"같은 경사를 이웃 차(13)로 재면 하나도 못 세지만 2칸 차(26)로는 센다 (이웃 %d · 2칸 %d)" % (_n1, e0[0]))
    #    ⓑ 그릇 수준: 두 행짜리 0 은 그릇이 아니다 — 목록 격자 테두리 몫(10)이 그릇이다.
    chk(_floor([0, 0] + [10] * 6 + [30, 31, 10, 10, 10, 12]) == 10,
        u"그릇 수준은 3행 이상 이어지는 값 중 최솟값이다(두 행짜리 0 은 무시)")
    chk(_floor([7, 5, 9]) == 5 and _floor([]) == 0,
        u"3행짜리 무리가 없으면 최솟값 · 빈 구간은 0")
    #    ⓒ 그릇 안 칸들: 테두리 있는 그릇(80%H) 안에 칸 넷이 있으면 그릇 한 밴드가 아니라 칸 넷으로 갈린다.
    box = _canvas(112, 199, (30, 30, 30))
    _fill(box, 6, 20, 106, 180, (200, 200, 200))        # 그릇(밝은 판)
    _fill(box, 8, 22, 104, 178, (30, 30, 30))           # 그릇 안 어두운 바닥(테두리 2px 만 남는다)
    for _k in range(4):
        _y = 30 + _k * 38
        _fill(box, 14, _y, 98, _y + 26, (200, 200, 200))  # 칸 테두리
        _fill(box, 16, _y + 2, 96, _y + 24, (30, 30, 30))
        _stripes(box, 24, _y + 8, 88, _y + 18)            # 칸 안 내용
    _bb = [r for r in read_layout(box, u"그릇") if u"블록" not in r.name]
    chk(len(_bb) == 4 and all(abs(r.h - 26.0 * 100 / 199) <= 1.5 for r in _bb),
        u"그릇(80%%H) 안 칸 넷이 칸마다 갈린다 (밴드 %d · 높이 %s)" % (len(_bb), u"·".join(u"%.1f" % r.h for r in _bb)))
    _bj = [len(read_layout(_jitter(box, _a, _c), u"그릇")) for _a, _c in JITTER_CASES]
    chk(all(_g == len(read_layout(box, u"그릇")) for _g in _bj),
        u"그 그림은 ±1 네 흔들기에 조각 수가 같다 (%s)" % u"·".join(str(_g) for _g in _bj))

    # ㉒ 판독표가 **지금 이 판독기로 구운 것**인가 (T28 103회차 · T437 4회차가 찾은 구멍)
    #    판독기를 고치고 `--gen` 을 안 돌리면 원작 쪽은 옛 자, 클론 쪽은 새 자로 재게 된다.
    st_now, st_tab = reader_stamp(), table_stamp(TABLE)
    chk(st_tab is not None,
        u"판독표 머리에 판독기 자국이 박혀 있다 (%s)" % (st_tab or u"없다 — `--gen` 을 돌려라"))
    chk(st_tab == st_now,
        u"판독표를 구운 자국이 지금 판독기와 같다 (표 %s ↔ 자 %s)%s"
        % (st_tab, st_now, u"" if st_tab == st_now else u" — `python3 tools/ui_score.py --gen` 으로 표를 다시 구워라"))
    # 자국은 «설명만 고친 것» 에는 안 움직인다 — 안 그러면 주석 한 줄에 표를 다시 굽게 된다.
    chk(_strip_py(u'def f():\n    """설명"""\n    # 주석\n    return 1\n')
        == _strip_py(u'def f():\n    """다른 설명"""\n    return 1\n'),
        u"자국은 설명·주석만 바뀐 것에는 안 움직인다")

    # ㉓ 흔들기 자가 제 노릇을 하는가 (T437 5회차 · 판정 자)
    #    🚫 이 칸은 «자가 안 흔들린다» 를 말하지 않는다 — 진짜 화면은 24/35 가 흔들린다(그것이 T437 이다).
    #    여기서 재는 것은 **자기 자신**: ±1 이 그림의 뜻을 안 바꾸고, 또렷한 그림에서는 조각 수가 버틴다.
    j = _canvas(112, 199, (20, 20, 20))
    for _y0 in (30, 70, 110, 150):
        _fill(j, 10, _y0, 100, _y0 + 20, (230, 230, 230))
        _fill(j, 20, _y0 + 5, 45, _y0 + 15, (40, 40, 40))
        _fill(j, 60, _y0 + 5, 90, _y0 + 15, (40, 40, 40))
    j_base = len(read_layout(j, u"흔들기"))
    j_got = [len(read_layout(_jitter(j, _a, _c), u"흔들기")) for _a, _c in JITTER_CASES]
    chk(all(g == j_base for g in j_got),
        u"또렷한 그림은 ±1 흔들어도 조각 수가 버틴다 (%d → %s)" % (j_base, u"·".join(str(g) for g in j_got)))
    _one = _jitter(j, 1)
    chk(bytes(_one.px) != bytes(j.px) and max(abs(a - b) for a, b in zip(_one.px, j.px)) == 1,
        u"흔들기는 모든 화소를 **딱 1 눈금**만 민다")
    chk(bytes(_jitter(j, 1, True).px) != bytes(_one.px),
        u"체커판 흔들기는 전체 흔들기와 다른 그림이다")

    # ㉔ «다음 볼 화면» 의 흔들림 폭 (T28 106회차)
    chk(jitter_span(os.path.join(REPO, "tools"), u"없는화면", []) is None,
        u"샷이 없는 화면의 흔들림 폭은 None 이다")
    _sp_dir = os.path.join(REPO, "tools", ".ui_score_span")
    os.makedirs(_sp_dir)
    try:
        _sp = _canvas(112, 199, (20, 20, 20))
        for _y0 in (30, 70, 110, 150):
            _fill(_sp, 10, _y0, 100, _y0 + 20, (230, 230, 230))
            _fill(_sp, 20, _y0 + 5, 45, _y0 + 15, (40, 40, 40))
        png_write(os.path.join(_sp_dir, "screen_틀.png"), _sp)
        _rects = read_layout(_sp, u"틀")
        _got = jitter_span(_sp_dir, u"틀", _rects)
        chk(_got is not None and _got[0] <= _got[1],
            u"흔들림 폭은 (작은 값, 큰 값) 으로 온다 (%s)" % (_got,))
        chk(_got is not None and abs(_got[1] - _got[0]) < 0.2,
            u"또렷한 그림은 폭이 0 에 가깝다 — 경고가 안 붙는다 (%.2f)" % (_got[1] - _got[0]))
    finally:
        for _f in os.listdir(_sp_dir):
            os.unlink(os.path.join(_sp_dir, _f))
        os.rmdir(_sp_dir)

    # ㉕ «다음 볼 화면» 차례 — 흔들리는 화면은 뒤로, 빼지는 않는다 (T28 108회차)
    _cand = [(u"가", 1.0), (u"나", 2.0), (u"다", 3.0), (u"라", 4.0), (u"마", 5.0), (u"바", 6.0)]
    _rate_ = lambda t: t[1]
    _shaky_ = lambda n: 1 if n in (u"가", u"나") else 0
    _ns = [n for n, _v in next_screens(_cand, _rate_, _shaky_)]
    chk(_ns[:3] == [u"다", u"라", u"마"],
        u"안 흔들리는 화면이 달성률 순으로 먼저 온다 (%s)" % u"·".join(_ns))
    _all = [n for n, _v in next_screens(_cand, _rate_, _shaky_, take=len(_cand))]
    chk(sorted(_all) == sorted(n for n, _v in _cand),
        u"흔들리는 화면도 차례에서 **안 빠진다** — 뒤로 밀 뿐이다 (%s)" % u"·".join(_all))
    chk(_all[-2:] == [u"가", u"나"],
        u"흔들리는 화면끼리도 달성률 순을 지킨다 (%s)" % u"·".join(_all[-2:]))
    chk([n for n, _v in next_screens(_cand, _rate_, lambda n: 0)][:2] == [u"가", u"나"],
        u"아무도 안 흔들리면 차례는 달성률 그대로다")

    # ㉖ 카드 **위끝** 자 (T28 114회차) — 딤이 비쳐도 안 걸린다
    def _cardimg(dim, top_y):
        """딤 밝기 dim 위에 흰 카드를 top_y 부터 그린 9:16 그림."""
        c = _canvas(112, 199, (dim, dim, dim))
        _fill(c, 20, top_y, 92, 170, (250, 250, 250))
        return c
    _dark = _cardimg(12, 40)          # 원작 꼴(딤 α .988)
    _lite = _cardimg(120, 40)         # 클론 꼴(딤 α .5 — 뒤 화면이 비친다)
    _t1, _t2 = card_top(_dark), card_top(_lite)
    chk(_t1 is not None and abs(_t1 - 100.0 * 40 / 199) < 1.0,
        u"카드 위끝을 그린 자리에서 찾는다 (%.2f%%H · 만든 자리 %.2f)" % (_t1 or -1, 100.0 * 40 / 199))
    chk(_t2 is not None and abs(_t2 - _t1) < 0.6,
        u"딤이 밝아도(뒤 화면이 비쳐도) 같은 위끝을 낸다 (%.2f ↔ %.2f)" % (_t1 or -1, _t2 or -1))
    _band = _cardimg(12, 40)
    _fill(_band, 20, 60, 92, 90, (20, 20, 20))     # 카드 **안**의 어두운 띠(배너)
    chk(abs((card_top(_band) or -99) - _t1) < 0.6,
        u"카드 안 어두운 띠는 위끝을 안 흔든다 (%.2f)" % (card_top(_band) or -99))
    chk(card_top(_canvas(112, 199, (12, 12, 12))) is None,
        u"카드가 없으면 위끝도 없다")

    # ㉗ «카드 위끝» 줄이 낡은 샷 화면에 꼬리표를 단다 (T28 120회차)
    #    🚫 지우지 않는다 — 꼬리표만 단다(낡은 샷 규칙 · T28 81회차).
    chk("forge-list" in STALE_SCREENS,
        u"낡은 샷 표에 `forge-list` 가 있다(카드 위끝 줄이 꼬리표를 달 자리)")
    chk(u"낡은 샷" in (u"  ⚠ 낡은 샷 — %s" % STALE_SCREENS["forge-list"]),
        u"꼬리표 문구가 «낡은 샷» 으로 시작한다")

    # T500 — **번호 없는 칸끼리는 서로 안 지운다.** 1159행 거르기가 `run is None` 에도 돌면
    #   이력에 있던 번호 없는 칸이 **통째로 사라진다**(이력 여섯 칸이 T28 판단의 근거 전부다).
    import json as _js
    import tempfile as _tf
    _d = _tf.mkdtemp(prefix="ui_score_t500_")
    _bl = os.path.join(_d, "bl.json")
    with open(_bl, "w", encoding="utf-8") as _f:
        _f.write(_js.dumps({"run": None, "avg": 1.0, "screens": {"a": 1.0},
                             "history": [{"run": 1, "avg": 1.0, "screens": {"a": 1.0}},
                                         {"run": None, "avg": 2.0, "screens": {"a": 2.0}}]},
                            ensure_ascii=False))
    save_baseline_file(_bl, [("a", 3.0)], 3.0, None, None, None, False, None, None)
    with open(_bl, encoding="utf-8") as _f:
        _h = _js.loads(_f.read())["history"]
    chk(len(_h) == 3 and [x.get("run") for x in _h] == [1, None, None],
        u"번호 없는 칸끼리 서로 안 지운다(T500 · 이력 %s)" % ([x.get("run") for x in _h],))
    save_baseline_file(_bl, [("a", 4.0)], 4.0, 1, None, None, False, None, None)
    with open(_bl, encoding="utf-8") as _f:
        _h2 = _js.loads(_f.read())["history"]
    chk([x.get("run") for x in _h2] == [None, None, 1],
        u"번호가 있으면 같은 번호의 옛 칸은 그대로 갈아탄다(T500 · 이력 %s)" % ([x.get("run") for x in _h2],))

    # T517 ⓐ — **±2 로 어긋난 가짜 샷**에서 자리가 올라오고 «딱맞음 0» 꼬리표가 붙는다.
    #   🚫 `clone_lut` 의 펼치기를 지우면(또는 tol 을 0 으로 박으면) 이 칸이 바로 빨강이 된다 —
    #   고장 주입이 **답을 바꾸는 자리**에 있다(결정 855). 아래 ±0 대조군이 그 증거다.
    import contextlib as _cl
    import io as _io
    _d7 = _tf.mkdtemp(prefix="ui_score_t517_")
    _sd, _rd = os.path.join(_d7, "s"), os.path.join(_d7, "r")
    os.makedirs(_sd)
    _name7, _rel7 = next(((s, r) for s, r in pairs() if r), (None, None))
    _css7 = os.path.join(_d7, "fake.css")
    with open(_css7, "w", encoding="utf-8") as _f:
        _f.write(u".t517-fake { background: #00897b; }\n")
    if _name7:
        os.makedirs(os.path.join(_rd, os.path.dirname(_rel7)) or _rd, exist_ok=True)
        _c7 = _canvas(90, 160, (13, 17, 23))
        _fill(_c7, 10, 10, 60, 60, (2, 139, 121))      # 못박은 #00897b 에서 ±2 어긋난 화소
        with open(os.path.join(_sd, "screen_%s.png" % _name7), "wb") as _f:
            _f.write(png_write(None, _c7))
        with open(os.path.join(_rd, _rel7), "wb") as _f:
            _f.write(png_write(None, _canvas(90, 160, (35, 35, 35))))

        def _scan7(tol):
            _keep = VS_CLONE_TOL
            globals()["VS_CLONE_TOL"] = tol
            _b = _io.StringIO()
            try:
                with _cl.redirect_stdout(_b):
                    _rc = scan_ref(_sd, ref_dir=_rd, css_path=_css7, only=[_name7])
            finally:
                globals()["VS_CLONE_TOL"] = _keep
            return _rc, _b.getvalue()

        _rc2, _o2 = _scan7(2)
        _rc0, _o0 = _scan7(0)
        chk(_rc2 == 0 and ".t517-fake" in _o2,
            u"±2 어긋난 가짜 샷에서 그 자리가 목록에 오른다(T517 · rc %s)" % _rc2)
        chk(u"딱맞음 0" in _o2 and u"T517 앞에는 안 보였다" in _o2,
            u"그 줄에 «딱맞음 0 · ≤2 로만 찍혔다» 꼬리표가 붙는다(T517)")
        chk(".t517-fake" not in _o0,
            u"±0 대조군에서는 그 자리가 **안** 보인다(고장 주입이 답을 바꾼다 · 결정 855)")
    else:
        chk(False, u"화면 표가 비어 ±2 가짜 샷 칸을 못 돌렸다(`pairs()` 가 0줄)")

    # T519 ⓒ — 열 줄을 **한 잣대로** 고르지 않는다: 차가 큰 **작은** 자리가 넓은 자리에 안 밀린다 ·
    #   `--notes` 에 이미 적힌 자리는 꼬리표가 붙고 **뒤로 밀린다**(그 자리가 매 회차 칸을 먹던 것).
    #   줄 꼴: (화면, 차, 화소, rgb, 원작색, 상자, 딱맞음, off)
    _row_small = (u"작은자리", 255, 100, (9, 9, 9), (4, 238, 0), (1.0, 2.0, 3.0, 4.0), 5, 0)
    _row_noted = (u"적힌자리", 254, 90, (0, 93, 255), (4, 238, 0), (1.0, 2.0, 3.0, 4.0), 5, 0)
    _row_wide = (u"넓은자리", 10, 90000, (1, 2, 3), (4, 5, 6), (1.0, 2.0, 3.0, 4.0), 7, 0)
    _tbl9 = {(0, 93, 255): [(u".settings-toggle.on", 3115)], (1, 2, 3): [(u".whatever-wide", 1)],
             (9, 9, 9): [(u".fresh-small-nowhere", 1)]}
    _b9 = _io.StringIO()
    with _cl.redirect_stdout(_b9):
        _scan_rows([_row_wide, _row_small, _row_noted], _tbl9, limit=3)
    _o9 = _b9.getvalue()
    chk(u"작은자리" in _o9 and u"뽑힘 차" in _o9,
        u"차가 큰 작은 자리가 넓은 자리에 안 밀린다(T519 ⓐ · 종전 잣대로는 화소순에서 잘렸다)")
    chk(u"이미 적힌 자리" in _o9 and u"--notes" not in _o9.split(u"작은자리")[0],
        u"`--notes` 바늘(`.settings-toggle.on`)과 겹치는 후보에 «이미 적힌 자리» 가 붙는다(T519 ⓑ)")
    # 그리고 **뒤로 민다** — 이미 적힌 자리는 아직 안 적힌 자리가 남아 있으면 그 뒤에 선다.
    _b10 = _io.StringIO()
    with _cl.redirect_stdout(_b10):
        _scan_rows([_row_noted, _row_wide], _tbl9, limit=1)
    _o10 = _b10.getvalue()
    _head10 = _o10.split(u"· 이미 적힌 자리")[0]
    chk(u"넓은자리" in _head10 and u"적힌자리" not in _head10 and u"적힌자리" in _o10,
        u"한 줄만 찍을 때 **아직 안 적힌** 자리가 먼저 서고, 적힌 자리는 꼬리 한 줄로 **남는다**(T519 ⓑ)")

    # T517 ⓑ — **빈 폴더는 «깨끗하다» 가 아니다.** 훑은 화면이 0 이면 ⛔ 와 함께 rc 1 로 떨어진다
    #   (종전엔 «0자리» 를 찍고 rc 0 으로 끝나서, `--shots` 를 빼먹은 발사가 **초록으로 보였다**).
    _b8 = _io.StringIO()
    with _cl.redirect_stdout(_b8):
        _rc8 = scan_ref(os.path.join(_d7, "empty-nope"), ref_dir=_rd, css_path=_css7)
    chk(_rc8 == 1 and u"아무것도 안 봤다" in _b8.getvalue(),
        u"샷이 0장이면 ⛔ 와 함께 rc 1 이다(T517 · rc %s)" % _rc8)

    print(u"")
    if fail:
        print(u"✗ ui_score self-test: %d/%d 칸 실패" % (len(fail), ok[0]))
        return 1
    print(u"✓ ui_score self-test 통과 (%d칸)" % ok[0])
    return 0


# ─────────────────────────── 흔들기 자 (T437 판정) ───────────────────────────
# 🚨 **두 런을 견주는 것으로는 «자의 흔들림» 을 못 잰다**(T437 5회차 실측). 회차 사이에는 그림 자체가
#    바뀐다 — «지문이 1.0 아래» 인 화면도 화소로는 3,828~95,927개가 다르고, 반대로 화소 4.1%가
#    바뀐 `autoforge` 는 조각 수가 그대로다. 곧 «얼마나 바뀌었나» 는 «자가 흔들렸나» 를 못 가른다.
#    그래서 **한 장을 ±1 만 흔들어** 본다 — 그림의 뜻은 하나도 안 바뀌는 크기다. 조각 수가 바뀌면
#    그것은 **자의 흔들림**이고, 런도 촬영도 끌어들이지 않는다.
JITTER_WARN = 0.2      # 점수 폭이 이만큼이면 «자가 흔든다» 로 본다(T28 106·108회차)
JITTER_BASE = "24/35"      # T437 5회차 실측(런 1038) — 이 수를 줄이는 것이 T437 의 판정이다


def _jitter(img, amp, checker=False):
    """모든 화소(또는 체커판 반쪽)를 amp 만큼 민 그림 — 뜻은 그대로, 값만 1 눈금."""
    px = bytearray(img.px)
    w = img.w
    for y in range(img.h):
        for x in range(w):
            if checker and ((x + y) & 1) == 0:
                continue
            d = (y * w + x) * 3
            for k in range(3):
                v = px[d + k] + amp
                px[d + k] = 0 if v < 0 else (255 if v > 255 else v)
    return Img(img.w, img.h, px)


JITTER_CASES = ((1, False), (-1, False), (1, True), (-1, True))


def jitter_report(shots_dir, only=None):
    """화면마다 ±1 흔들어 조각 수가 버티는지 찍는다 — T437 의 판정 자."""
    names = [n for (n, _r) in pairs()]
    print(u"%-18s %6s %8s %8s %8s %8s" % (u"화면", u"원래", u"전체+1", u"전체-1", u"체커+1", u"체커-1"))
    bad = tot = 0
    worst = []
    for n in names:
        if only and n not in only:
            continue
        f = os.path.join(shots_dir, "screen_%s.png" % n)
        if not os.path.exists(f):
            continue
        img = png_read(f)
        base = len(read_layout(img, n))
        got = [len(read_layout(_jitter(img, a, c), n)) for a, c in JITTER_CASES]
        tot += 1
        if any(g != base for g in got):
            bad += 1
            worst.append((max(abs(g - base) for g in got), n, base, got))
        print(u"%-18s %6d %8d %8d %8d %8d" % (n, base, got[0], got[1], got[2], got[3]))
    print(u"\n— 화소를 ±1 건드렸을 때 조각 수가 달라진 화면 **%d/%d**(T437 실측 기준선 %s)"
          % (bad, tot, JITTER_BASE))
    for d, n, base, got in sorted(worst, reverse=True)[:5]:
        print(u"    가장 크게 흔들린 곳: %-18s %d → %s (±%d)" % (n, base, "·".join(str(g) for g in got), d))
    return 0


# ─────────────────────────── 들머리 ───────────────────────────

def main():
    ap = argparse.ArgumentParser(description="T28 원작 ↔ 클론 화면 비율 대조")
    ap.add_argument("--gen", action="store_true", help="원작 샷을 판독해 docs/ref-layout.md 를 다시 쓴다")
    ap.add_argument("--score", action="store_true", help="클론 샷을 판독표와 대조해 점수를 낸다")
    ap.add_argument("--self-test", action="store_true", help="자기 검사")
    ap.add_argument("--read", metavar="PNG", help="PNG 하나를 판독해 표 행을 찍는다")
    ap.add_argument("--ref-dir", default=REF_DIR, help="원작 샷 뿌리(`ref/` · 짝 표의 칸이 이 아래 상대 경로다)")
    ap.add_argument("--shots", default=os.path.join(REPO, "ui-screens"), help="클론 샷 폴더")
    ap.add_argument("--table", default=TABLE, help="판독표 경로")
    ap.add_argument("--only", nargs="*", help="이 화면 이름만")
    ap.add_argument("--baseline", default=BASELINE, help="지난 회차 점수 파일(회귀 대조)")
    ap.add_argument("--save-baseline", action="store_true", help="이번 점수를 기준선으로 적는다")
    # T500 — `--shots` 가 가리킨 자리에 `meta.json` 이 없으면 런 번호를 못 읽는다(긁개 폴더로 재면 그렇다).
    #   사람은 번호를 아는데 파일만 모르는 자리라 **대는 길**을 둔다. 번호가 없으면 아래에서 **저장을 접는다**.
    ap.add_argument("--run", type=int, default=None, metavar="번호",
                    help="이 채점의 런 번호(meta.json 에 run 이 없을 때 손으로 댄다 · T500)")
    ap.add_argument("--jitter", action="store_true",
                    help="같은 그림을 ±1 흔들어 조각 수가 버티는지 본다(T437 판정 자)")
    ap.add_argument("--notes", action="store_true", help="«낡은 원작 샷» 항목을 근거까지 펼쳐 찍는다")
    ap.add_argument("--verify-notes", action="store_true",
                    help="그 항목들이 댄 정본 근거가 아직 정본에 있는지 검산한다(T512 ⓐ)")
    ap.add_argument("--scan-ref", action="store_true",
                    help="정본이 못박은 면 색을 화면마다 훑어 «정본 ≠ 원작» 자리를 기계가 먼저 올린다(T512 ⓒ)")
    ap.add_argument("--vs", nargs=2, metavar=("화면", "정본선택자"),
                    help="원작 샷의 그 자리와 정본이 못박은 값을 나란히 찍는다(T512 ⓑ · 예: `--vs settings .settings-toggle.on`)")
    ap.add_argument("--rows", metavar="화면", help="원작 ↔ 클론의 구분선 줄 자리를 견준다(딤과 무관)")
    ap.add_argument("--behind", nargs=2, metavar=("앞", "뒤"),
                    help="앞 화면의 가로줄이 뒤 화면을 딤으로 비치는지 밝기비로 가른다(T358 ✂)")
    a = ap.parse_args()

    if a.self_test:
        return self_test()
    if a.jitter:
        return jitter_report(a.shots, a.only)
    if a.behind:
        return behind(a.shots, a.behind[0], a.behind[1])
    if a.rows:
        return rows_cmp(a.shots, a.rows, a.ref_dir)
    if a.read:
        for r in read_layout(png_read(a.read)):
            print(u"| %s | %.1f | %.1f | %.1f | %.1f | %s |" % (r.name, r.x, r.y, r.w, r.h, r.grid()))
        return 0
    if a.verify_notes:
        return verify_stale_notes()
    if a.scan_ref:
        return scan_ref(a.shots, a.ref_dir, only=a.only)
    if a.vs:
        return vs_report(a.vs[0], a.vs[1], a.shots, a.ref_dir)
    if a.notes and not a.score:
        print_stale_notes(full=True)
        return 0
    if a.gen:
        return gen(a.ref_dir, a.table, a.only)
    if a.score:
        return score(a.table, a.shots, a.only, a.baseline, a.save_baseline, a.notes, a.run)
    ap.print_help()
    return 0


if __name__ == "__main__":
    sys.exit(main())
