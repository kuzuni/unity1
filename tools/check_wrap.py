#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T361 — 정본 `white-space` 선언 ↔ 표(`Assets/Forge/Resources/WrapUi.json`) **왕복 대조** 자(T354 `check_line_height.py` 꼴).

표는 자리마다 «정본 파일·줄·선택자·원값» 을 `_정본` 칸에 그대로 적어 뒀다. 정본에서 다시 뽑아 그 기록과 한 자리씩 맞춰 본다 —
정본이 한 줄이라도 움직이면(값·줄 번호·선택자) 이 자가 먼저 운다.

세 겹:
  ① 개수 — 정본 선언 수 == 표 자리 수(41 = nowrap 40 + normal 1)
  ② 자리마다 `_정본` 이 대는 (파일·줄·선택자·원값) 이 지금 정본과 같다
  ③ 키는 선택자에서 기계로 만든 것(영숫자 밖 → `_` · 소문자 · 겹치면 `_N`)이고, `sites` 의 낱말은 원값과 같다(nowrap|normal 만)

곁들여 알린다(문제로 안 센다): 표를 **읽는 곳**(`WrapUi.Apply`/`WrapUi.Wraps`)이 몇 군데인가 — ⓐ 배선 전에는 0 이다.

사용:  python3 tools/check_wrap.py [--css <style.css>] [--table <WrapUi.json>] [--game <Assets/Scripts>] [--list] [--self-test]
rc:    0 = 세 겹이 다 맞다 · 1 = 어긋난 자리가 있다 · 2 = 정본을 못 읽었다
"""
import json
import os
import re
import sys

CSS_DEFAULT = os.path.join('.wwwww-src', 'web', 'css', 'style.css')
TABLE_DEFAULT = os.path.join('Assets', 'Forge', 'Resources', 'WrapUi.json')
GAME_DEFAULT = os.path.join('Assets', 'Scripts')
WORDS = ('nowrap', 'normal')

RULE = re.compile(r'([^{}]+)\{([^{}]*)\}')
DECL = re.compile(r'(?<![-\w])white-space\s*:\s*([^;}]+)')
SRC = re.compile(r'^(?P<file>[\w./-]+):(?P<line>\d+)\s+(?P<sel>.+?)\s+→\s+(?P<val>\S+)$')
READER = re.compile(r'\bWrapUi\.(Apply|Wraps)\s*\(')


def blank_comments(text):
    """주석을 같은 길이의 공백으로(줄 바꿈은 남긴다) — 줄 번호가 안 어긋나고 주석 속 `{}` 가 규칙을 안 깨뜨린다."""
    out = []
    i = 0
    while i < len(text):
        j = text.find('/*', i)
        if j < 0:
            out.append(text[i:]); break
        out.append(text[i:j])
        k = text.find('*/', j + 2)
        k = len(text) if k < 0 else k + 2
        out.append(''.join('\n' if c == '\n' else ' ' for c in text[j:k]))
        i = k
    return ''.join(out)


def key_of(selector):
    k = re.sub(r'[^0-9a-zA-Z]+', '_', selector).strip('_').lower()
    return re.sub(r'_+', '_', k)


def extract(css_text):
    """[(줄, 선택자, 원값)] — 정본 순서대로."""
    nc = blank_comments(css_text)
    rows = []
    for m in RULE.finditer(nc):
        sel = ' '.join(m.group(1).split())
        for d in DECL.finditer(m.group(2)):
            line = nc.count('\n', 0, m.start(2) + d.start()) + 1
            rows.append((line, sel, d.group(1).strip()))
    return rows


def keys_for(rows):
    seen = {}
    out = []
    for line, sel, val in rows:
        k = key_of(sel)
        seen[k] = seen.get(k, 0) + 1
        out.append((k if seen[k] == 1 else '%s_%d' % (k, seen[k]), line, sel, val))
    return out


def count_readers(game_dir):
    n = 0
    for root, _, files in os.walk(game_dir):
        for f in files:
            if not f.endswith('.cs'):
                continue
            try:
                with open(os.path.join(root, f), encoding='utf-8') as fh:
                    n += len(READER.findall(fh.read()))
            except OSError:
                pass
    return n


def run(css_text, table, game_dir=None, out=print, list_all=False):
    rows = extract(css_text)
    src = table.get('_정본') or {}
    sites = table.get('sites') or {}
    problems = []
    # ① 개수
    if len(rows) != len(sites):
        problems.append('① 정본 선언 %d ↔ 표 자리 %d' % (len(rows), len(sites)))
    if len(src) != len(sites):
        problems.append('① `_정본` %d ↔ `sites` %d — 자리마다 정본 기록이 하나씩 있어야 한다' % (len(src), len(sites)))
    # ② 자리마다 정본 기록 대조
    want = {k: (line, sel, val) for k, line, sel, val in keys_for(rows)}
    for k, (line, sel, val) in want.items():
        if k not in src:
            problems.append('② 정본 style.css:%d «%s» → %s 이 표에 없다(키 %s)' % (line, sel, val, k))
    for k, rec in src.items():
        m = SRC.match(rec.strip())
        if not m:
            problems.append('② `_정본` «%s» 의 꼴이 «style.css:줄  선택자  → 값» 이 아니다: %s' % (k, rec))
            continue
        if k not in want:
            problems.append('② 표 «%s» 가 대는 자리가 정본에 없다: %s' % (k, rec))
            continue
        line, sel, val = want[k]
        if int(m.group('line')) != line or m.group('sel') != sel or m.group('val') != val:
            problems.append('② «%s» 정본이 움직였다: 표 %s:%s «%s» → %s ↔ 지금 %d «%s» → %s'
                            % (k, m.group('file'), m.group('line'), m.group('sel'), m.group('val'), line, sel, val))
    # ③ 키 규칙 · 낱말
    for k, v in sites.items():
        if k != key_of(k):
            problems.append('③ 키가 규칙 밖이다(영숫자·`_`·소문자): %s' % k)
        if v not in WORDS:
            problems.append('③ «%s» 의 낱말이 정본에 없다: %s (nowrap|normal)' % (k, v))
        elif k in want and want[k][2] != v:
            problems.append('③ «%s» 의 낱말이 원값과 다르다: 표 %s ↔ 정본 %s' % (k, v, want[k][2]))
    n_nowrap = sum(1 for v in sites.values() if v == 'nowrap')
    n_normal = sum(1 for v in sites.values() if v == 'normal')
    if list_all:
        for k, line, sel, val in keys_for(rows):
            out('· style.css:%d  %-60s → %-6s  %s' % (line, sel[:60], val, k))
    readers = count_readers(game_dir) if game_dir else -1
    for p in problems:
        out('  ✗ ' + p)
    head = '✓' if not problems else '✗'
    out('%s check_wrap: 정본 white-space 선언 %d(nowrap %d · normal %d) · 표 자리 %d · 어긋남 %d%s'
        % (head, len(rows), sum(1 for r in rows if r[2] == 'nowrap'), sum(1 for r in rows if r[2] == 'normal'), len(sites), len(problems),
           (' · 표를 읽는 곳 %d(ⓐ 배선 전엔 0 이 정상)' % readers) if readers >= 0 else ''))
    return 1 if problems else 0


def self_test():
    css = ".a { white-space: nowrap; }\n/* { 주석 } */\n.b, .c { color: red; white-space: normal; }\n#x.y span { white-space: nowrap; }\n.a { white-space: nowrap; }\n"
    rows = extract(css)
    checks = []
    checks.append(('선언 넷을 뽑는다', len(rows) == 4))
    checks.append(('주석 속 중괄호가 규칙을 안 깨뜨린다(줄 번호 유지)', rows[1][0] == 3 and rows[1][1] == '.b, .c'))
    ks = keys_for(rows)
    checks.append(('키 규칙: 영숫자 밖 → _ · 소문자', ks[2][0] == 'x_y_span' and ks[1][0] == 'b_c'))
    checks.append(('같은 선택자 둘째는 _2', ks[3][0] == 'a_2'))
    good = {'_정본': {k: 'style.css:%d  %s  → %s' % (l, s, v) for k, l, s, v in ks}, 'sites': {k: v for k, l, s, v in ks}}
    logs = []
    checks.append(('맞는 표는 rc 0', run(css, good, None, logs.append) == 0))
    bad1 = json.loads(json.dumps(good, ensure_ascii=False)); bad1['sites'].pop('a_2'); bad1['_정본'].pop('a_2')
    checks.append(('자리가 빠지면 ① 로 운다', run(css, bad1, None, logs.append) == 1))
    bad2 = json.loads(json.dumps(good, ensure_ascii=False)); bad2['_정본']['a'] = 'style.css:9  .a  → nowrap'
    checks.append(('줄 번호가 움직이면 ② 로 운다', run(css, bad2, None, logs.append) == 1))
    bad3 = json.loads(json.dumps(good, ensure_ascii=False)); bad3['sites']['a'] = 'normal'
    checks.append(('낱말이 원값과 다르면 ③ 로 운다', run(css, bad3, None, logs.append) == 1))
    bad4 = json.loads(json.dumps(good, ensure_ascii=False)); bad4['sites']['a'] = 'pre'
    checks.append(('정본에 없는 낱말은 ③ 로 운다', run(css, bad4, None, logs.append) == 1))
    bad5 = json.loads(json.dumps(good, ensure_ascii=False)); bad5['sites']['B-c'] = bad5['sites'].pop('b_c'); bad5['_정본']['B-c'] = bad5['_정본'].pop('b_c')
    checks.append(('키가 규칙 밖이면 ③ 로 운다', run(css, bad5, None, logs.append) == 1))
    bad6 = json.loads(json.dumps(good, ensure_ascii=False)); bad6['_정본']['a'] = 'a nowrap'
    checks.append(('`_정본` 꼴이 깨지면 ② 로 운다', run(css, bad6, None, logs.append) == 1))
    failed = [name for name, ok in checks if not ok]
    for name, ok in checks:
        print(('  ✓ ' if ok else '  ✗ ') + name)
    print(('✓' if not failed else '✗') + ' check_wrap --self-test %d칸 %s' % (len(checks), '통과' if not failed else ('실패 %d' % len(failed))))
    return 0 if not failed else 1


def main(argv):
    css_path, table_path, game_dir = CSS_DEFAULT, TABLE_DEFAULT, GAME_DEFAULT
    list_all = False
    i = 1
    while i < len(argv):
        a = argv[i]
        if a == '--self-test':
            return self_test()
        if a == '--css': css_path = argv[i + 1]; i += 2; continue
        if a == '--table': table_path = argv[i + 1]; i += 2; continue
        if a == '--game': game_dir = argv[i + 1]; i += 2; continue
        if a == '--list': list_all = True; i += 1; continue
        print('모르는 인자: ' + a); return 2
    if not os.path.isfile(css_path):
        print('✗ 정본 CSS 를 못 읽었다: %s (git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)' % css_path)
        return 2
    try:
        with open(table_path, encoding='utf-8') as fh:
            table = json.load(fh)
    except (OSError, ValueError) as e:
        print('✗ 표를 못 읽었다: %s — %s' % (table_path, e))
        return 1
    with open(css_path, encoding='utf-8') as fh:
        css = fh.read()
    return run(css, table, game_dir if os.path.isdir(game_dir) else None, list_all=list_all)


if __name__ == '__main__':
    sys.exit(main(sys.argv))
