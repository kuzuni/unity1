#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T378 — «표값 × 박힌 상수» 자: `UiKit.L/H("키")` 뒤에 상수 곱(·합)이 붙은 자리를 전수하고
허용 목록(`docs/table-scale-allow.md`)과 맞춘다.

왜 있나(결정 629 · T28 65회차): 표(`catalog.json`)에는 정본값이 맞게 들어 있는데 코드가 거기에 `* 1.1f`
같은 곱을 얹어 화면만 어긋난다. 훑는 눈에는 «표를 읽으니 §1 지킴» 으로 보이고, 박힌 수를 찾는 grep 에도
`1.1f` 는 «배율» 로 보여 아무도 못 봤다. 화면 실측(런 644 리그 행 피치 8.13%H ↔ 표값 7.48%H)이 드러냈다.

무엇을 세나: `UiKit.L("k")` · `UiKit.H("k")` 바로 뒤(사이에 `* w` 같은 변수 곱은 허용)에
`* <수>f` · `+ <수>f` · `- <수>f` 가 붙은 자리. 주석(`//`) 뒤는 안 센다. 갈래는 `Assets/Scripts/Game` 만.

허용 목록 두 칸:
  기하 — 영구. `* 2f`(양쪽 패딩) · `* 0.5f`(반) 처럼 뜻이 있는 기하. 표로 옮길 필요가 없다.
  임시 — 임자 있음. 자리 파일이 남의 lock 이라 아직 못 고친 자리. **알리되 막지 않는다** — 그 lock 이 풀리면
         정본 CSS 값을 표 키로 옮기고 곱을 없앤다. 끝난 자리는 목록에서 **사라져야** 한다(남아 있으면 이 자가 운다).
목록은 «파일 · 키 · 연산 상수» 로 맞춘다 — 줄 번호는 남이 파일을 고칠 때마다 밀리므로 열쇠가 아니다.

사용:  python3 tools/check_table_scale.py [--game Assets/Scripts/Game] [--allow docs/table-scale-allow.md] [--list] [--self-test]
rc:    0 = 허용 밖 0 · 목록에만 있는 것 0   ·   1 = 어긋남   ·   2 = 파일을 못 읽었다
"""
import os
import re
import sys

GAME_DEFAULT = os.path.join('Assets', 'Scripts', 'Game')
ALLOW_DEFAULT = os.path.join('docs', 'table-scale-allow.md')

# UiKit.L("k") 또는 UiKit.H("k") · 그 뒤 변수 곱 0개 이상(`* w` · `* UiKit.RefW`) · 그 뒤 상수 곱·합
SITE = re.compile(
    r'UiKit\.(?P<fn>[LH])\("(?P<key>[a-z0-9_]+)"\)'
    r'(?P<vars>(?:\s*\*\s*[A-Za-z_][A-Za-z0-9_.]*)*)'
    r'\s*(?P<op>[*+\-])\s*(?P<num>[0-9]*\.?[0-9]+)f\b')
ROW = re.compile(r'^\|\s*(?P<file>[^|]+?)\s*\|\s*(?P<key>[^|]+?)\s*\|\s*(?P<op>[×+\-])\s*(?P<num>[0-9.]+)\s*\|\s*(?P<kind>기하|임시)\s*\|\s*(?P<why>[^|]*?)\s*\|\s*$')
KINDS = ('기하', '임시')


def norm_num(s):
    """'1.10' 과 '1.1' · '2' 와 '2.0' 을 같은 열쇠로."""
    try:
        v = float(s)
    except ValueError:
        return s
    return ('%g' % v)


def strip_comment(line):
    i = line.find('//')
    return line if i < 0 else line[:i]


def scan_text(text, fname):
    out = []
    for ln, raw in enumerate(text.split('\n'), 1):
        line = strip_comment(raw)
        for m in SITE.finditer(line):
            op = '×' if m.group('op') == '*' else m.group('op')
            out.append({'file': fname, 'line': ln, 'fn': m.group('fn'), 'key': m.group('key'),
                        'op': op, 'num': norm_num(m.group('num')), 'src': raw.strip()})
    return out


def scan_dir(root):
    sites = []
    for d, _, files in os.walk(root):
        for f in sorted(files):
            if not f.endswith('.cs'):
                continue
            p = os.path.join(d, f)
            with open(p, encoding='utf-8') as fh:
                sites.extend(scan_text(fh.read(), os.path.relpath(p, root).replace(os.sep, '/')))
    return sites


def parse_allow(text):
    rows = []
    for ln, line in enumerate(text.split('\n'), 1):
        m = ROW.match(line.strip())
        if not m:
            continue
        rows.append({'file': m.group('file').strip('`'), 'key': m.group('key').strip('`'), 'op': m.group('op'),
                     'num': norm_num(m.group('num')), 'kind': m.group('kind'), 'why': m.group('why'), 'line': ln})
    return rows


def site_id(s):
    return (s['file'], s['key'], s['op'], s['num'])


def judge(sites, allow):
    """(허용 밖, 목록에만 있음, 기하로 허용된 자리, 임시로 허용된 자리)"""
    table = {}
    for r in allow:
        table.setdefault(site_id(r), r)
    seen = set()
    outside, geo, temp = [], [], []
    for s in sites:
        r = table.get(site_id(s))
        if r is None:
            outside.append(s)
            continue
        seen.add(site_id(s))
        (geo if r['kind'] == '기하' else temp).append((s, r))
    stale = [r for k, r in table.items() if k not in seen]
    return outside, stale, geo, temp


def fmt(s):
    return '%s:%d  UiKit.%s("%s") %s%s' % (s['file'], s['line'], s['fn'], s['key'], s['op'], s['num'])


def run(game, allow_path, list_all):
    if not os.path.isdir(game):
        print('✗ check_table_scale: 갈래를 못 찾았다: ' + game); return 2
    if not os.path.isfile(allow_path):
        print('✗ check_table_scale: 허용 목록이 없다: ' + allow_path); return 2
    sites = scan_dir(game)
    with open(allow_path, encoding='utf-8') as fh:
        allow = parse_allow(fh.read())
    outside, stale, geo, temp = judge(sites, allow)
    bad = [r for r in allow if r['kind'] not in KINDS]
    if list_all:
        for s in sites:
            print('  ' + fmt(s))
    for s in outside:
        print('  ✗ 허용 밖(새 곱 — 표 키로 옮기든지 목록의 «기하» 에 까닭을 적어라): ' + fmt(s))
    for r in stale:
        print('  ✗ 목록에만 있다(고쳐졌으면 줄을 지워라 · 파일·키·상수가 바뀌었으면 맞춰라): %s · %s · %s%s (목록 %d줄)' % (r['file'], r['key'], r['op'], r['num'], r['line']))
    files = sorted(set(s['file'] for s in sites))
    owners = {}
    for s, r in temp:
        m = re.search(r'T\d+', r['why'])
        who = m.group(0) if m else '?'
        owners[who] = owners.get(who, 0) + 1
    summary = '전수 %d(파일 %d) · 기하 %d · 임시(임자 있음 · 안 막음) %d · 허용 밖 %d · 목록에만 %d' % (
        len(sites), len(files), len(geo), len(temp), len(outside), len(stale))
    if outside or stale or bad:
        print('✗ check_table_scale: ' + summary)
        return 1
    print('✓ check_table_scale: ' + summary + (' · 임시 임자: ' + ' · '.join('%s %d' % kv for kv in sorted(owners.items())) if owners else ''))
    return 0


# ---------------------------------------------------------------- 자기 검사
SAMPLE_CS = '''
float a = UiKit.H("row_h") * 1.1f;            // 곱
float b = UiKit.L("bar_w") * w * 1.25f;       // 사이에 변수 곱
float c = UiKit.H("pad") * 2f;                // 기하
float d = UiKit.H("x") + 3f - UiKit.L("y") * 0.5f;
float e = UiKit.H("plain");                   // 상수 없음 — 안 센다
// float f = UiKit.H("in_comment") * 9f;      // 주석 — 안 센다
float g = UiKit.H("row_h") * 1.10f;           // 같은 자리 둘째 — 열쇠는 같다(1.1)
'''
SAMPLE_ALLOW = '''
| 파일 | 키 | 곱 | 갈래 | 왜 / 임자 |
|---|---|---|---|---|
| `A.cs` | `row_h` | ×1.1 | 임시 | T331 lock 뒤 · 정본 2320 |
| `A.cs` | `bar_w` | ×1.25 | 임시 | T331 lock 뒤 |
| `A.cs` | `pad` | ×2 | 기하 | 양쪽 패딩 |
| `A.cs` | `x` | +3 | 기하 | 시험 |
| `A.cs` | `y` | ×0.5 | 기하 | 반 |
'''


def self_test():
    n = [0]

    def ok(cond, what):
        n[0] += 1
        if not cond:
            print('  ✗ 자기 검사 %d: %s' % (n[0], what)); return False
        print('  ✓ 자기 검사 %d: %s' % (n[0], what)); return True

    good = True
    sites = scan_text(SAMPLE_CS, 'A.cs')
    ids = [site_id(s) for s in sites]
    good &= ok(len(sites) == 6, '전수 6(곱 · 변수 곱 · ×2 · +3 · ×0.5 · 둘째 row_h) — 상수 없음·주석은 안 센다 (실제 %d)' % len(sites))
    good &= ok(('A.cs', 'bar_w', '×', '1.25') in ids, '사이에 `* w` 가 있어도 잡는다')
    good &= ok(('A.cs', 'x', '+', '3') in ids and ('A.cs', 'y', '×', '0.5') in ids, '합(+3)과 반(×0.5)을 잡는다')
    good &= ok(all(s['key'] != 'in_comment' for s in sites), '`//` 뒤는 안 센다')
    good &= ok(ids.count(('A.cs', 'row_h', '×', '1.1')) == 2, '`1.10f` 와 `1.1f` 는 같은 열쇠(1.1)')
    allow = parse_allow(SAMPLE_ALLOW)
    good &= ok(len(allow) == 5, '허용 목록 5줄을 읽는다(머리·구분선은 건너뛴다) (실제 %d)' % len(allow))
    outside, stale, geo, temp = judge(sites, allow)
    good &= ok(not outside and not stale, '전부 목록에 있으면 허용 밖 0 · 목록에만 0')
    good &= ok(len(geo) == 3 and len(temp) == 3, '기하 3(pad·x·y) · 임시 3(row_h 둘·bar_w) (실제 %d·%d)' % (len(geo), len(temp)))
    # 새 곱이 생기면 빨강
    s2 = scan_text(SAMPLE_CS + '\nfloat z = UiKit.H("new_key") * 1.3f;\n', 'A.cs')
    o2, st2, _, _ = judge(s2, allow)
    good &= ok(len(o2) == 1 and o2[0]['key'] == 'new_key', '목록에 없는 새 곱 → 허용 밖 1')
    # 고쳐진 자리가 목록에 남으면 빨강
    s3 = scan_text(SAMPLE_CS.replace('UiKit.L("bar_w") * w * 1.25f', 'UiKit.L("bar_w") * w'), 'A.cs')
    o3, st3, _, _ = judge(s3, allow)
    good &= ok(not o3 and len(st3) == 1 and st3[0]['key'] == 'bar_w', '곱을 걷었는데 목록에 남으면 «목록에만 있다» 1')
    # 열쇠는 줄 번호가 아니다
    s4 = scan_text('\n\n\n' + SAMPLE_CS, 'A.cs')
    o4, st4, _, _ = judge(s4, allow)
    good &= ok(not o4 and not st4, '줄이 밀려도(줄 번호는 열쇠가 아니다) 판정이 같다')
    # 실물: 지금 갈래·목록이 읽힌다
    if os.path.isdir(GAME_DEFAULT) and os.path.isfile(ALLOW_DEFAULT):
        real = scan_dir(GAME_DEFAULT)
        good &= ok(len(real) >= 1, '실물 갈래에서 자리를 읽는다 (%d)' % len(real))
    print(('✓' if good else '✗') + ' check_table_scale --self-test: %d칸' % n[0])
    return 0 if good else 1


def main(argv):
    game, allow, list_all = GAME_DEFAULT, ALLOW_DEFAULT, False
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--self-test':
            return self_test()
        if a == '--list':
            list_all = True
        elif a == '--game':
            i += 1; game = argv[i]
        elif a == '--allow':
            i += 1; allow = argv[i]
        else:
            print('모르는 인자: ' + a); return 2
        i += 1
    return run(game, allow, list_all)


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
