#!/usr/bin/env python3
"""화면에 나가는 글자가 **글꼴에 다 있는가** (ROUTINE T89 막이).

왜 필요한가 — 런 148 의 실제 화면에서 이모지 30종이 전부 두부(□)였는데 유니티 잡은 초록이었다.
`TextSizeGateTests` 는 한글 구간(U+AC00~D7A3)만 봐서 이모지·기호를 놓쳤고, 화면을 눈으로 열어 본 회차에만
드러났다. 이 자는 **코드에 박힌 화면 문구**를 훑어 «글꼴에 없는 글자» 를 찍는다.

무엇을 보는가:
  ⓐ 화면에 글자를 세우는 부름(`UiKit.Text`·`Label`·`Bold`·`Btn`·`Toast`·`Stroked`…)의 **문자열 인자**만 본다
     (주석·코드 식별자는 안 본다 — 주석의 이모지는 화면에 안 나간다).
  ⓑ 정본 `TOAST_ICON`(`Assets/StreamingAssets/data/ui-text.json`)에 있는 이모지는 **아이콘으로 치환**되므로 뺀다
     (T89 의 `IconText`/`UiKit.IconTextRow` 가 하는 일 · 이형 선택자 U+FE0F 포함).
  ⓒ 남은 글자를 주인 글꼴(`Assets/Fonts/NotoSansKR-Forge.ttf`)의 cmap 과 맞춰 없는 것을 찍는다.

`KNOWN` 은 «지금 알고 있고 임자가 정해진» 자리다 — 그것만 통과시키고 **새로 생긴 두부는 rc 1** 로 막는다.
의존성 0(순수 파이썬 · fonttools 없이 cmap 4/12 형식을 직접 읽는다).

사용:  python3 tools/check_text_glyphs.py [--self-test]
"""
import json
import os
import re
import struct
import sys

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
FONT = os.path.join(ROOT, 'Assets', 'Fonts', 'NotoSansKR-Forge.ttf')
TABLE = os.path.join(ROOT, 'Assets', 'StreamingAssets', 'data', 'ui-text.json')
SCAN_DIR = os.path.join(ROOT, 'Assets', 'Scripts', 'Game')

# 임자가 정해진 «아는 두부» — 정본도 이 글자를 문자열로 쓴다(ui.js 2272 ⏹ · 대장간 업그레이드 버튼 ⏱).
# 한글 글꼴에 이모지 글리프가 없어 □ 로 남는다: 주인 조치(이모지 폴백 글꼴)나 정본이 TOAST_ICON 에
# 그 두 글자를 넣어 주기 전에는 못 고친다. 여기 적은 것만 통과하고 **새 두부는 막는다**.
KNOWN = {
    '⏱': 'ForgeInfoPopup 업그레이드 버튼(정본도 같은 글자) — 주인 글꼴/정본 표 대기',
    '⏹': 'ForgeHost 자동 제련 종료 토스트(정본 ui.js 2272 그대로) — 주인 글꼴/정본 표 대기',
}

CALL = re.compile(r'\b(?:Text|Label|Bold|Btn|Toast|Stroked|SetProfile|Placeholder)\s*\(')
LITERAL = re.compile(r'"((?:[^"\\]|\\.)*)"')


def font_chars(path):
    """TTF cmap(형식 4·12) → 코드포인트 집합. (순수 함수 — 자기 검사가 이것을 본다.)"""
    d = open(path, 'rb').read()
    num = struct.unpack('>H', d[4:6])[0]
    tabs = {}
    for i in range(num):
        off = 12 + 16 * i
        tag = d[off:off + 4].decode('latin1')
        o, ln = struct.unpack('>II', d[off + 8:off + 16])
        tabs[tag] = (o, ln)
    if 'cmap' not in tabs:
        raise SystemExit('✗ check_text_glyphs: %s 에 cmap 표가 없다' % path)
    o = tabs['cmap'][0]
    n = struct.unpack('>H', d[o + 2:o + 4])[0]
    chosen = None
    for i in range(n):
        _pid, _eid, so = struct.unpack('>HHI', d[o + 4 + 8 * i:o + 12 + 8 * i])
        fmt = struct.unpack('>H', d[o + so:o + so + 2])[0]
        if fmt in (4, 12):
            chosen = (fmt, o + so)          # 형식 12 가 있으면 그것이 이긴다(BMP 밖까지 덮는다)
    if chosen is None:
        raise SystemExit('✗ check_text_glyphs: 읽을 수 있는 cmap 부표(형식 4·12)가 없다')
    fmt, base = chosen
    chars = set()
    if fmt == 4:
        segx2 = struct.unpack('>H', d[base + 6:base + 8])[0]
        seg = segx2 // 2
        ends = [struct.unpack('>H', d[base + 14 + 2 * i:base + 16 + 2 * i])[0] for i in range(seg)]
        sbase = base + 16 + segx2
        starts = [struct.unpack('>H', d[sbase + 2 * i:sbase + 2 * i + 2])[0] for i in range(seg)]
        for s, e in zip(starts, ends):
            if s == 0xFFFF:
                continue
            chars.update(range(s, min(e, 0xFFFE) + 1))
    else:
        groups = struct.unpack('>I', d[base + 12:base + 16])[0]
        for i in range(groups):
            s, e, _g = struct.unpack('>III', d[base + 16 + 12 * i:base + 28 + 12 * i])
            chars.update(range(s, min(e, s + 0xFFFF) + 1))
    return chars


def icon_chars(path):
    """정본 TOAST_ICON 의 키에 쓰인 글자 + 이형 선택자 — 아이콘으로 바뀌므로 글꼴에 없어도 된다."""
    table = json.load(open(path, encoding='utf-8'))['TOAST_ICON']
    out = {'️'}
    for key in table:
        out.update(key)
    return out


def screen_strings(root):
    """화면에 글자를 세우는 부름의 문자열 인자 → [(파일:줄, 문자열)] (주석 줄은 뺀다)."""
    out = []
    for dirpath, _dirs, files in os.walk(root):
        for f in sorted(files):
            if not f.endswith('.cs'):
                continue
            p = os.path.join(dirpath, f)
            rel = os.path.relpath(p, ROOT)
            for i, line in enumerate(open(p, encoding='utf-8').read().split('\n'), 1):
                st = line.strip()
                if st.startswith('//') or st.startswith('*'):
                    continue
                if not CALL.search(line):
                    continue
                for lit in LITERAL.findall(line):
                    out.append(('%s:%d' % (rel, i), lit))
    return out


def missing(strings, font, skip):
    """글꼴에도 없고 아이콘 표에도 없는 글자 → {글자: [자리…]} (순수 함수)."""
    out = {}
    for where, text in strings:
        for ch in text:
            if ord(ch) < 0x20 or ch in skip or ord(ch) in font:
                continue
            out.setdefault(ch, []).append(where)
    return out


def main(argv):
    if '--self-test' in argv:
        return self_test()
    font = font_chars(FONT)
    skip = icon_chars(TABLE)
    miss = missing(screen_strings(SCAN_DIR), font, skip)
    new = {ch: w for ch, w in miss.items() if ch not in KNOWN}
    for ch, where in sorted(miss.items()):
        tag = '(아는 것) ' + KNOWN[ch] if ch in KNOWN else '**새 두부**'
        print('  %s U+%05X «%s» %d곳 — %s' % ('·' if ch in KNOWN else '✗', ord(ch), ch, len(where), tag))
        if ch not in KNOWN:
            for w in sorted(set(where))[:6]:
                print('      %s' % w)
    if new:
        print('✗ check_text_glyphs: 글꼴에 없는 글자가 화면 문구에 %d 종 새로 들어왔다 — 두부(□)로 찍힌다.' % len(new))
        print('  고침 둘: ⓐ 정본이 그 자리에 아이콘을 그리면 `UiIcons`/`UiKit.IconTextRow` 로(정본 줄을 먼저 읽는다)')
        print('          ⓑ 정본도 글자로 그리면 주인 글꼴에 그 구간이 있어야 한다 — 주인 조치이므로 KNOWN 에 임자와 함께 적는다.')
        return 1
    print('✓ check_text_glyphs: 화면 문구 %d줄 · 글꼴에 없는 글자 %d 종(전부 KNOWN · 임자 있음)'
          % (len(screen_strings(SCAN_DIR)), len(miss)))
    return 0


def self_test():
    ok = True
    font = font_chars(FONT)
    for ch, want in [('가', True), ('A', True), ('★', True), ('→', True), ('⏹', False), ('\U0001F528', False)]:
        got = ord(ch) in font
        if got != want:
            print('✗ cmap: «%s» U+%04X 기대 %s · 받은 %s' % (ch, ord(ch), want, got)); ok = False
    skip = icon_chars(TABLE)
    for ch, want in [('\U0001FA99', True), ('⚔', True), ('️', True), ('가', False)]:
        if (ch in skip) != want:
            print('✗ 아이콘 표: «%s» 기대 %s' % (ch, want)); ok = False
    # 스캔: 부름 안의 문자열만 · 주석은 뺀다
    cases = [
        ('UiKit.Text(p, "n", TextKind.Sub, "⏹ 끝", "ink");', 1),
        ('// UiKit.Text(p, "n", TextKind.Sub, "⏹ 끝");', 0),
        ('string s = "⏹";', 0),
    ]
    import tempfile
    for code, want in cases:
        with tempfile.TemporaryDirectory() as d:
            open(os.path.join(d, 'X.cs'), 'w', encoding='utf-8').write(code)
            got = len(missing(screen_strings(d), font, skip))
            if got != want:
                print('✗ 스캔 «%s»: 기대 %d · 받은 %d' % (code[:40], want, got)); ok = False
    # 진짜 코드가 이 자를 지나는가
    rc = main([])
    if rc != 0:
        print('✗ 지금 Assets/Scripts/Game 이 이 검사에 걸린다 — 위 ✗ 를 먼저 고쳐라'); ok = False
    print('✓ check_text_glyphs 자기 검사 통과' if ok else '✗ check_text_glyphs 자기 검사 실패')
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
