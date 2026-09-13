#!/usr/bin/env python3
"""화면에 나가는 글자가 **글꼴에 다 있는가** (ROUTINE T89 막이).

왜 필요한가 — 런 148 의 실제 화면에서 이모지 30종이 전부 두부(□)였는데 유니티 잡은 초록이었다.
`TextSizeGateTests` 는 한글 구간(U+AC00~D7A3)만 봐서 이모지·기호를 놓쳤고, 화면을 눈으로 열어 본 회차에만
드러났다. 이 자는 **코드에 박힌 화면 문구**를 훑어 «글꼴에 없는 글자» 를 찍는다.

무엇을 보는가:
  ⓐ `Assets/Scripts/Game` 의 **모든 문자열 리터럴**(주석 줄 제외) + `StreamingAssets/data/*.json`·`Assets/Forge/Resources/*.json`
     의 **문자열 값**을 본다. 부름 인자만 보던 T89 판은 구멍이 둘이었다(T100 · 검수 Q 실측): «변수에 담은 문구»
     (`string autoLabel = "자동 ↻…"`)와 «데이터에서 오는 글자»(정본 `chat.js` 의 `😭`·`ㅠ`)를 통째로 놓쳤다 —
     셋 다 **전부 초록인 런**의 PNG 에서 □ 로 보였다. 식별자·키는 ASCII 라 넓혀도 오탐이 안 는다
     (이 자는 «글꼴에 없는 비 ASCII» 만 센다).
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
    # T100 — 넓힌 뒤 드러난 셋. 임자가 정해져 있고 이 회차에 못 고치는 자리다(고칠 파일이 남의 lock · 글꼴 재추출 필요).
    '↻': 'ForgeSheet 자동 제련 버튼 «자동 ↻» — 정본은 글자가 아니라 아이콘(ui.js 1551 IconGen.img(autoloop)) · T100 ⓐ(T87 lock 이 풀린 뒤)',
    '😭': '채팅 문구(정본 chat.js 11행) — data/*.json 으로 들어온다 · 이모지 글리프는 주인 글꼴 밖(폴백 글꼴 대기) · T100 ⓑ',
    'ㅠ': '채팅 문구의 한글 자모 «ㅠ»(정본 chat.js 16행) — 서브셋이 완성형만 담았다(U+3130~318F 를 더하면 낫는다) · T100 ⓑ',
    'ㅋ': '채팅 문구의 한글 자모 «ㅋ»(정본 chat.js · meta.json /chat/LINES/14) — ㅠ 와 같은 서브셋 갈래 · T100 ⓑ',
    '🐴': '펫·탈것 토스트와 얼굴 폴백(PetSkillUi.json /text/toast_mount_full · PetSkillKit 324행) — 정본은 `IconGen.img(MOUNT_ICONS[…])` 아이콘이다 · T20 자리라 T100 ⓐ 와 같이 넘긴다',
    '🐾': '같은 갈래(펫 보관함·출전 토스트 셋 + 얼굴 폴백) — 정본은 아이콘 · T100 ⓐ',
    '🛡': 'PlayerInfoUi.json /text/shield 라벨(T65 자리) — 정본은 방어 아이콘 · T100 ⓐ 와 같이 넘긴다',
}

LITERAL = re.compile(r'"((?:[^"\\]|\\.)*)"')
DATA_DIRS = [
    os.path.join(ROOT, 'Assets', 'StreamingAssets', 'data'),
    os.path.join(ROOT, 'Assets', 'Forge', 'Resources'),
]
# 데이터 안의 이모지가 전부 «화면 글자» 인 것은 아니다 — 정본은 이모지를 **아이콘 키**로도 쓴다
# (`PET_ICONS`·`MOUNT_ICONS`·`SKILL_ICONS`·`AGE_ICON`·`AVATAR_POOL`·기술트리 `icon`). 그 값은 `IconGen.img(…)`
# ·아바타 아틀라스(T31)로 그려지므로 글꼴과 무관하다. 키 경로로 가른다 — 값으로 가르면 같은 이모지가
# 글자로 쓰인 자리까지 놓친다.
ICON_KEY = re.compile(r'(?i)(icon|emoji|avatar)')


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
    """`root` 아래 C# 의 **모든 문자열 리터럴** → [(파일:줄, 문자열)] (주석 줄은 뺀다 · T100 으로 넓혔다)."""
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
                for lit in LITERAL.findall(line):
                    out.append(('%s:%d' % (rel, i), lit))
    return out


def data_strings(dirs=None):
    """데이터가 쥔 화면 글자 → [(파일:키경로, 문자열)]. 채팅 문구처럼 **소스에 없는 글자**가 여기로 온다(T100 ⓑ)."""
    out = []
    for d in (dirs if dirs is not None else DATA_DIRS):
        if not os.path.isdir(d):
            continue
        for f in sorted(os.listdir(d)):
            if not f.endswith('.json'):
                continue
            p = os.path.join(d, f)
            rel = os.path.relpath(p, ROOT)
            try:
                doc = json.load(open(p, encoding='utf-8'))
            except ValueError:
                continue

            def walk(v, path):
                if isinstance(v, str):
                    if not ICON_KEY.search(path):          # 아이콘·아바타 «키» 는 글자가 아니다(아래 주석)
                        out.append(('%s:%s' % (rel, path or '/'), v))
                elif isinstance(v, dict):
                    for k, vv in v.items():
                        walk(vv, path + '/' + str(k))
                elif isinstance(v, list):
                    for i, vv in enumerate(v):
                        walk(vv, path + '/' + str(i))
            walk(doc, '')
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
    strings = screen_strings(SCAN_DIR) + data_strings()
    miss = missing(strings, font, skip)
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
    print('✓ check_text_glyphs: 문구 %d줄(코드 %d + 데이터 %d) · 글꼴에 없는 글자 %d 종(전부 KNOWN · 임자 있음)'
          % (len(strings), len(screen_strings(SCAN_DIR)), len(data_strings()), len(miss)))
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
        ('string s = "⏹";', 1),          # T100 — 변수 대입도 센다(ForgeSheet 의 «자동 ↻» 를 놓친 구멍)
    ]
    # 데이터: 글자 값은 세고 아이콘 «키» 는 안 센다(T100 · 정본이 이모지를 아이콘 이름으로도 쓴다)
    import json as _json
    import tempfile as _tmp
    for doc, want, note in [
        ({'text': {'a': '⏹ 끝'}}, 1, '글자 값'),
        ({'PET_ICONS': {'Cat': '🐱'}}, 0, '아이콘 키'),
        ({'avatars': {'AVATAR_POOL': ['🐱']}}, 0, '아바타 키'),
        ({'chat': {'LINES': ['⏹']}}, 1, '채팅 문구'),
    ]:
        with _tmp.TemporaryDirectory() as d:
            _json.dump(doc, open(os.path.join(d, 'x.json'), 'w', encoding='utf-8'))
            got = len(missing(data_strings([d]), font, skip))
            if got != want:
                print('✗ 데이터 스캔 «%s»: 기대 %d · 받은 %d' % (note, want, got)); ok = False
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
