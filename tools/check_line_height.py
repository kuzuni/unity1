#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T354 — 정본 `line-height` 선언 ↔ 표(`Assets/Forge/Resources/LineHeightUi.json`) **왕복 대조** 자.

왜 왕복인가: 이 표는 만들 때 자리마다 «정본 파일·줄·선택자·원값» 을 `_정본` 칸에 그대로 적어 뒀다.
그래서 선택자를 **닮은꼴로 맞출 필요가 없다** — 정본에서 다시 뽑아 그 기록과 **한 자리씩** 맞춰 보면 된다.
정본이 한 줄이라도 움직이면(값·줄 번호·선택자) 이 자가 먼저 운다.

세 겹:
  ① 개수 — 정본 선언 수 == 표 자리 수
  ② 자리마다 `_정본` 이 대는 (파일·줄·선택자·원값) 이 지금 정본과 같다
  ③ 키 꼬리가 원값의 단위와 맞고, 표에 적힌 수가 그 원값의 수다
     (`Nrem` → `…_lh_rem` · `calc(var(--app-w) * k)` → `…_lh_w` · 그 밖 배수 → `…_lh`)

곁들여 알린다(문제로 안 센다): 표를 **읽는 곳**이 몇 군데인가. 배선(2회차) 전에는 0 이고,
배선 뒤에 0 이면 «표만 있고 아무도 안 읽는다» 는 뜻이라 눈에 띄어야 한다.

사용:  python3 tools/check_line_height.py [--css <style.css>] [--ui <ui.js>] [--table <LineHeightUi.json>]
                                          [--game <Assets/Scripts>] [--list] [--self-test]
rc:    0 = 세 겹이 다 맞다 · 1 = 어긋난 자리가 있다 · 2 = 정본을 못 읽었다
"""
import json
import os
import re
import sys

CSS_DEFAULT = os.path.join('.wwwww-src', 'web', 'css', 'style.css')
UI_DEFAULT = os.path.join('.wwwww-src', 'web', 'js', 'ui.js')
TABLE_DEFAULT = os.path.join('Assets', 'Forge', 'Resources', 'LineHeightUi.json')
GAME_DEFAULT = os.path.join('Assets', 'Scripts')

RATIO_SUFFIX = '_lh'
REM_SUFFIX = '_lh_rem'
APPW_SUFFIX = '_lh_w'

RULE = re.compile(r'([^{}]+)\{([^{}]*)\}')
DECL = re.compile(r'(?<![-\w])line-height:\s*([^;}]+)')
COMMENT = re.compile(r'/\*.*?\*/', re.S)
INLINE = re.compile(r'line-height:\s*([0-9.]+)')


def blank_comments(text):
    """주석을 **같은 길이의 공백**으로 지운다(줄 바꿈은 남긴다) — 줄 번호가 안 어긋난다.

    왜 통째로 먼저 지우나: 주석 안에 `{`·`}` 가 들어 있으면 규칙 쪼개기(`[^{}]+`)가 **주석 한가운데서**
    시작해 여는 `/*` 를 잃는다 — 그러면 선택자에 주석 꼬리가 그대로 붙는다(실측: 정본 5151·7067 두 자리)."""
    out = []
    i = 0
    while i < len(text):
        j = text.find('/*', i)
        if j < 0:
            out.append(text[i:]); break
        out.append(text[i:j])
        k = text.find('*/', j + 2)
        k = len(text) if k < 0 else k + 2
        chunk = text[j:k]
        out.append(''.join('\n' if c == '\n' else ' ' for c in chunk))
        i = k
    return ''.join(out)


def declarations(css_text, ui_text):
    """정본 → [(파일, 줄, 선택자, 원값)] — 표를 만든 것과 **같은 파서**여야 왕복이 성립한다."""
    out = []
    css_text = blank_comments(css_text)
    for m in RULE.finditer(css_text):
        sel = ' '.join(m.group(1).split())
        for d in DECL.finditer(m.group(2)):
            line = css_text[:m.start(2) + d.start()].count('\n') + 1
            out.append(('style.css', line, sel, d.group(1).strip()))
    for m in INLINE.finditer(ui_text or ''):
        out.append(('ui.js', (ui_text[:m.start()].count('\n') + 1), '(ui.js 인라인) 데미지 숫자 조각', m.group(1)))
    return out


def unit_of(raw):
    """원값 → (단위 꼬리, 수). 못 읽으면 (None, None)."""
    raw = raw.strip()
    if raw.endswith('rem'):
        try:
            return REM_SUFFIX, float(raw[:-3])
        except ValueError:
            return None, None
    if raw.startswith('calc('):
        m = re.search(r'\*\s*([0-9.]+)', raw)
        return (APPW_SUFFIX, float(m.group(1))) if m else (None, None)
    try:
        return RATIO_SUFFIX, float(raw)
    except ValueError:
        return None, None


def note(entry):
    """`_정본` 한 줄 → (파일, 줄, 선택자, 원값). 꼴은 «파일:줄  선택자  → 원값»."""
    m = re.match(r'^(\S+?):(\d+)\s\s(.*?)\s\s→\s(.*)$', entry)
    if not m:
        return None
    return (m.group(1), int(m.group(2)), m.group(3), m.group(4))


def compare(decls, table, src):
    """세 겹 → (문제 줄 목록, 맞은 자리 수). 순수 함수 — 자기 검사가 여기를 잰다."""
    bad = []
    keys = [k for k in table if not k.startswith('_')]
    if len(keys) != len(decls):
        bad.append('개수가 다르다 — 정본 선언 %d · 표 자리 %d (정본이 늘거나 줄었다)' % (len(decls), len(keys)))

    # 정본 쪽을 (파일·줄) 로 색인한다 — 줄이 곧 자리의 이름이다.
    by_line = {}
    for f, ln, sel, raw in decls:
        by_line[(f, ln)] = (sel, raw)

    ok = 0
    for k in sorted(keys):
        got = note(src.get(k, ''))
        if got is None:
            bad.append('%s — `_정본` 자취가 없거나 꼴이 깨졌다' % k)
            continue
        f, ln, sel, raw = got
        now = by_line.get((f, ln))
        if now is None:
            bad.append('%s — 정본 %s:%d 에 이제 `line-height` 선언이 없다(정본이 움직였다)' % (k, f, ln))
            continue
        if now[0] != sel:
            bad.append('%s — 정본 %s:%d 의 선택자가 달라졌다: «%s» → «%s»' % (k, f, ln, sel, now[0]))
            continue
        if now[1] != raw:
            bad.append('%s — 정본 %s:%d 의 값이 달라졌다: «%s» → «%s»' % (k, f, ln, raw, now[1]))
            continue
        suf, num = unit_of(raw)
        if suf is None:
            bad.append('%s — 정본 원값 «%s» 의 단위를 못 읽었다' % (k, raw))
            continue
        if not k.endswith(suf) or (suf == RATIO_SUFFIX and (k.endswith(REM_SUFFIX) or k.endswith(APPW_SUFFIX))):
            bad.append('%s — 키 꼬리가 원값 «%s» 의 단위(%s)와 다르다' % (k, raw, suf))
            continue
        if abs(float(table[k]) - num) > 1e-9:
            bad.append('%s — 표값 %s ↔ 정본 %s (%s:%d)' % (k, table[k], num, f, ln))
            continue
        ok += 1
    return bad, ok


def readers(game_dir):
    """표를 읽는 코드 파일 — 배선됐는지 알리기만 한다."""
    out = []
    for root, _dirs, files in os.walk(game_dir):
        for fn in files:
            if not fn.endswith('.cs'):
                continue
            p = os.path.join(root, fn)
            try:
                with open(p, encoding='utf-8', errors='replace') as fh:
                    t = fh.read()
            except OSError:
                continue
            if 'LineHeightUi' in t or 'LineHeightTable' in t:
                out.append(os.path.relpath(p))
    return sorted(out)


def self_test():
    fails = []

    def eq(name, got, want):
        if got != want:
            fails.append('%s: 얻은 값 %r ≠ 바란 값 %r' % (name, got, want))

    CSS = '.a { line-height: 1.25; }\n.b { line-height: 1.15rem; }\n.c { line-height: calc(var(--app-w) * .0351); }\n'
    decls = declarations(CSS, '')
    eq('ⓐ 선언 셋을 읽는다', [(d[0], d[2], d[3]) for d in decls],
       [('style.css', '.a', '1.25'), ('style.css', '.b', '1.15rem'), ('style.css', '.c', 'calc(var(--app-w) * .0351)')])
    eq('ⓐ 줄 번호', [d[1] for d in decls], [1, 2, 3])
    eq('ⓐ 인라인도 읽는다', len(declarations('', 'x line-height:1.25 y')), 1)
    # `line-height` 를 품은 다른 속성(가짜 짝)을 안 문다
    eq('ⓐ 꼬리 붙은 속성은 안 문다', declarations('.z { --my-line-height: 2; }\n', ''), [])
    # ⚠ 주석이 중괄호를 품으면 규칙 쪼개기가 주석 한가운데서 시작한다 — 먼저 공백으로 지워야 선택자가 성하다.
    eq('ⓐ 중괄호를 품은 주석 뒤 선택자가 성하다',
       [d[2] for d in declarations('/* 보기: a { b } */\n.q { line-height: 1; }\n', '')], ['.q'])
    eq('ⓐ 주석을 지워도 줄 번호가 안 밀린다',
       [d[1] for d in declarations('/* 한 줄\n두 줄 { } */\n.q { line-height: 1; }\n', '')], [3])

    eq('ⓑ 단위 배수', unit_of('1.25'), (RATIO_SUFFIX, 1.25))
    eq('ⓑ 단위 rem', unit_of('1.15rem'), (REM_SUFFIX, 1.15))
    eq('ⓑ 단위 app-w', unit_of('calc(var(--app-w) * .0351)'), (APPW_SUFFIX, 0.0351))
    eq('ⓑ 못 읽는 값', unit_of('normal'), (None, None))

    eq('ⓒ 자취 한 줄을 푼다', note('style.css:12  .a b  → 1.25'), ('style.css', 12, '.a b', '1.25'))
    eq('ⓒ 깨진 자취', note('엉터리'), None)

    T = {'a_lh': 1.25, 'b_lh_rem': 1.15, 'c_lh_w': 0.0351}
    S = {'a_lh': 'style.css:1  .a  → 1.25',
         'b_lh_rem': 'style.css:2  .b  → 1.15rem',
         'c_lh_w': 'style.css:3  .c  → calc(var(--app-w) * .0351)'}
    eq('ⓓ 맞으면 조용하다', compare(decls, T, S), ([], 3))
    eq('ⓓ 표값이 다르면 운다',
       compare(decls, dict(T, a_lh=1.3), S)[0][0].startswith('a_lh — 표값'), True)
    eq('ⓓ 자취가 없으면 운다', compare(decls, T, dict(S, a_lh=''))[0][0].startswith('a_lh — `_정본`'), True)
    eq('ⓓ 정본 줄이 움직이면 운다',
       compare(decls, T, dict(S, a_lh='style.css:99  .a  → 1.25'))[0][0].find('이제 `line-height` 선언이 없다') > 0, True)
    eq('ⓓ 정본 값이 바뀌면 운다',
       compare(decls, T, dict(S, a_lh='style.css:1  .a  → 1.4'))[0][0].find('값이 달라졌다') > 0, True)
    eq('ⓓ 선택자가 바뀌면 운다',
       compare(decls, T, dict(S, a_lh='style.css:1  .옛이름  → 1.25'))[0][0].find('선택자가 달라졌다') > 0, True)
    eq('ⓓ 키 꼬리가 단위와 다르면 운다',
       compare(decls, {'a_lh_rem': 1.25, 'b_lh_rem': 1.15, 'c_lh_w': 0.0351},
               dict(S, a_lh_rem='style.css:1  .a  → 1.25'))[0][0].find('키 꼬리가') > 0, True)
    eq('ⓓ 개수가 다르면 운다', compare(decls, {'a_lh': 1.25}, S)[0][0].startswith('개수가 다르다'), True)

    if fails:
        print('✗ check_line_height --self-test 실패 %d' % len(fails))
        for f in fails:
            print('  · ' + f)
        return 1
    print('✓ check_line_height --self-test %d칸 통과' % 19)
    return 0


def main(argv):
    css_path, ui_path, table_path, game_dir = CSS_DEFAULT, UI_DEFAULT, TABLE_DEFAULT, GAME_DEFAULT
    show = False
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--self-test':
            return self_test()
        if a == '--list':
            show = True; i += 1; continue
        if a == '--css' and i + 1 < len(argv):
            css_path = argv[i + 1]; i += 2; continue
        if a == '--ui' and i + 1 < len(argv):
            ui_path = argv[i + 1]; i += 2; continue
        if a == '--table' and i + 1 < len(argv):
            table_path = argv[i + 1]; i += 2; continue
        if a == '--game' and i + 1 < len(argv):
            game_dir = argv[i + 1]; i += 2; continue
        i += 1

    try:
        with open(css_path, encoding='utf-8') as fh:
            css = fh.read()
    except OSError:
        print('✗ 정본 CSS 를 못 읽었다: %s '
              '(git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)' % css_path)
        return 2
    try:
        with open(ui_path, encoding='utf-8') as fh:
            ui = fh.read()
    except OSError:
        ui = ''

    try:
        with open(table_path, encoding='utf-8') as fh:
            root = json.load(fh)
    except (OSError, ValueError) as e:
        print('✗ 표를 못 읽었다: %s (%s)' % (table_path, e))
        return 1

    src = root.get('_정본') or {}
    table = dict((k, v) for k, v in root.items() if not k.startswith('_'))
    decls = declarations(css, ui)
    bad, ok = compare(decls, table, src)

    if show:
        for f, ln, sel, raw in decls:
            print('  %s:%d  %s  → %s' % (f, ln, sel[:70], raw))

    who = readers(game_dir)
    for line in bad:
        print('  · ' + line)
    if bad:
        print('✗ check_line_height: 정본 선언 %d ↔ 표 자리 %d — 어긋난 자리 %d' % (len(decls), len(table), len(bad)))
        return 1
    tail = ('읽는 곳 %d(%s)' % (len(who), ' · '.join(who[:3]))) if who else \
           '아직 **읽는 곳이 없다** — 배선(2회차) 전이라 정상이다'
    print('✓ check_line_height: 정본 선언 %d 이 표 자리 %d 과 한 자리씩 맞다(값·줄·선택자·단위) · %s' % (len(decls), ok, tail))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
