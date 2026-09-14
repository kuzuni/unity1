#!/usr/bin/env python3
"""`screens` 이어받기 고르기 — T347.

왜 — 배포 스텝은 지난 `screens` 의 그림·글자 자국을 이어받아 올린다. 그런데 이어받기가
**«이 런이 PNG 를 한 장도 못 냈을 때»만** 돌았다(`ci.yml` 의 바깥 조건 `[ "$n" = "0" ]` · T170).
그래서 **몇 장이라도 낸 런**(부분 실패)이 나머지를 통째로 지웠다 — 런 494 실물 `shots: 17 · carried: 0`
(직전 런 463 은 63장) 에서 `screen_forge-list`·`screen_player-info`·`t167-itemfaces.txt`·`perf-*.txt` 등
**46장 + 글자 자국**이 사라졌고, §1 «실제 화면을 본다» 의 판정 근거가 그 회차에 통째로 없어졌다.
고리 **안쪽**에 이미 «이 런이 쓴 것은 안 덮는다» 가 있으니 바깥 조건은 없어도 안전하고, 있으면 해롭다.

그런데 «늘 이어받는다» 는 새 함정을 하나 데려온다 — **언제 찍힌 그림인지 모르게 된다.**
그래서 이 자는 고르기와 함께 `carried.txt` 를 남긴다: 이어받은 파일마다 «어느 런 것인가» 한 줄.
눈으로 확인하는 사람이 지난 런의 그림을 이번 런 것으로 읽는 일을 막는다.

  plan --dir DIR [--names 파일|-] [--from-meta META] [--run N] [--carried-out PATH]
      stdin/파일에서 «지난 screens 의 파일 이름» 목록을 받아 **이어받을 것만** 한 줄씩 찍는다.
        · `.png`·`.txt` 만 (장부 `runs.jsonl`·`meta.json` 은 배포 스텝이 따로 쥔다)
        · 이 런이 이미 쓴 이름은 뺀다(덮지 않는다)
        · `carried.txt` 자신은 안 받는다(지난 런의 목록이 남으면 거짓말이 된다)
      그리고 `carried.txt`(기본 <dir>/carried.txt)에 머리 한 줄 + 이름들을 쓴다.
      이어받을 것이 하나도 없으면 `carried.txt` 를 만들지 않는다(빈 파일을 올리지 않는다).
  --self-test   고장 주입 자기 검사.

순수 파이썬 · 네트워크 없음 · 표준 라이브러리만.
"""
import argparse
import json
import os
import shutil
import sys
import tempfile

CARRIED = 'carried.txt'
KEEP_EXT = ('.png', '.txt')


def wanted(names, have, skip=(CARRIED,)):
    """[이어받을 이름] — 확장자로 거르고 · 이 런이 쓴 것을 빼고 · 목록 자신을 빼고 · 들어온 순서 그대로(중복 제거)."""
    out, seen = [], set()
    for n in names:
        n = (n or '').strip()
        if not n or n in seen:
            continue
        seen.add(n)
        if not n.lower().endswith(KEEP_EXT):
            continue
        if n in skip or os.path.basename(n) in skip:
            continue
        if n in have:
            continue
        out.append(n)
    return out


def from_run(meta_text):
    """받은 meta.json 의 런 번호 — 못 읽으면 None(머리 줄에 «모르는 런» 으로 적는다)."""
    try:
        d = json.loads(meta_text or '')
    except ValueError:
        return None
    if not isinstance(d, dict):
        return None
    r = d.get('run')
    return r if isinstance(r, (int, float)) and r > 0 else None


def header(run, src):
    return ('# 이어받은 파일 — 이 런(#%s)이 **안 찍은** 것이라 그림·수는 런 #%s 의 것이다(T347).\n'
            '#   이 목록에 있는 이름을 «이번 런의 화면» 으로 읽지 마라 — 눈 확인(§1)은 이 런이 찍은 것으로 한다.\n'
            % (run if run is not None else '?', src if src is not None else '?'))


def plan(dir_, names, meta_text, run, carried_out, stats_out=None):
    """(이어받을 이름 목록) — `carried.txt` 를 쓰고, stats_out 이 있으면 «후보 수 · 받을 수» 도 적는다.

    **후보 수를 따로 세는 까닭(T347 2회차)**: `carried: 0` 한 수로는 «받을 것이 없었다» 와 «지난 screens 를 아예 못 받았다» 가
    똑같이 보인다 — 앞의 것은 정상이고 뒤의 것은 이어받기가 통째로 죽은 것이다. 런 535 실물이 `shots 69 · carried 0` 이었고
    그 둘을 가르려면 «지난 screens 가 몇 개를 내놓았나» 가 있어야 했다.
    """
    have = set(os.listdir(dir_)) if os.path.isdir(dir_) else set()
    cand = wanted(names, set())          # 확장자·목록 자신만 거른 «지난 screens 가 내놓은 것»
    take = wanted(names, have)           # 그중 이 런이 아직 안 쓴 것
    if take:
        with open(carried_out, 'w', encoding='utf-8') as f:
            f.write(header(run, from_run(meta_text)))
            for n in take:
                f.write(n + '\n')
    if stats_out:
        with open(stats_out, 'w', encoding='utf-8') as f:
            f.write('candidates=%d\ntake=%d\n' % (len(cand), len(take)))
    return take


def _read(path):
    if path in (None, ''):
        return ''
    try:
        with open(path, encoding='utf-8') as f:
            return f.read()
    except (IOError, OSError):
        return ''


def self_test():
    fails = []
    tmp = tempfile.mkdtemp(prefix='screens_carry_')
    d = os.path.join(tmp, 'ui-screens')
    os.makedirs(d)

    def expect(name, cond, detail=''):
        if not cond:
            fails.append(name + (' — ' + detail if detail else ''))

    prev = ['screen_main.png', 'screen_forge-list.png', 'perf-frames.txt', 'runs.jsonl', 'meta.json', CARRIED, 'shot.PNG']
    # ⓐ 아무것도 안 찍은 런 — png·txt 만 받고 장부·목록은 뺀다(대소문자 확장자도 받는다)
    take = plan(d, prev, '{"run": 463}', 494, os.path.join(d, CARRIED))
    expect('ⓐ 셋만', take == ['screen_main.png', 'screen_forge-list.png', 'perf-frames.txt', 'shot.PNG'], str(take))
    expect('ⓐ carried.txt 머리', '런 #463' in _read(os.path.join(d, CARRIED)) and '#494' in _read(os.path.join(d, CARRIED)))
    expect('ⓐ carried.txt 이름', 'perf-frames.txt' in _read(os.path.join(d, CARRIED)))
    # ⓑ **부분 실패 런** — 제가 찍은 것은 안 덮고 나머지는 받는다(T347 의 병)
    os.remove(os.path.join(d, CARRIED))
    open(os.path.join(d, 'screen_main.png'), 'w').close()
    take = plan(d, prev, '{"run": 463}', 494, os.path.join(d, CARRIED))
    expect('ⓑ 제 그림은 안 덮는다', 'screen_main.png' not in take, str(take))
    expect('ⓑ 나머지는 받는다', 'screen_forge-list.png' in take and 'perf-frames.txt' in take, str(take))
    # ⓒ 이 런이 전부 찍었으면 받을 것이 없고 carried.txt 도 안 만든다
    for n in ('screen_forge-list.png', 'perf-frames.txt', 'shot.PNG'):
        open(os.path.join(d, n), 'w').close()
    os.remove(os.path.join(d, CARRIED))
    take = plan(d, prev, '{"run": 463}', 494, os.path.join(d, CARRIED))
    expect('ⓒ 받을 것 0', take == [], str(take))
    expect('ⓒ 빈 목록을 안 만든다', not os.path.exists(os.path.join(d, CARRIED)))
    # ⓓ 지난 런의 carried.txt 는 절대 안 받는다(거짓말이 된다)
    expect('ⓓ 목록 자신 제외', CARRIED not in wanted([CARRIED, 'a.png'], set()))
    # ⓔ meta.json 을 못 읽어도 멎지 않는다 — 머리 줄에 «?»
    d2 = os.path.join(tmp, 'b')
    os.makedirs(d2)
    take = plan(d2, ['x.png'], 'not json', None, os.path.join(d2, CARRIED))
    expect('ⓔ 깨진 meta 도 지나간다', take == ['x.png'] and '#?' in _read(os.path.join(d2, CARRIED)))
    expect('ⓔ from_run', from_run('{"run": 7}') == 7 and from_run('{}') is None and from_run('[]') is None)
    # ⓕ 중복·빈 줄·앞뒤 공백
    expect('ⓕ 중복 한 번', wanted([' a.png ', 'a.png', '', 'b.txt'], set()) == ['a.png', 'b.txt'])
    # ⓖ 디렉터리가 아직 없어도 돈다
    take = plan(os.path.join(tmp, 'nope'), ['a.png'], '', 1, os.path.join(tmp, 'c.txt'))
    expect('ⓖ 없는 디렉터리', take == ['a.png'])
    # ⓗ 후보 수 — «받을 것이 없었다»(후보 3 · 받을 것 0) ↔ «아예 못 받았다»(후보 0)를 가른다(T347 2회차 · 런 535)
    d3 = os.path.join(tmp, 'd')
    os.makedirs(d3)
    for n in ('a.png', 'b.txt'):
        open(os.path.join(d3, n), 'w').close()
    st = os.path.join(tmp, 'stats.txt')
    take = plan(d3, ['a.png', 'b.txt', 'runs.jsonl'], '', 9, os.path.join(d3, CARRIED), st)
    expect('ⓗ 받을 것 0', take == [], str(take))
    expect('ⓗ 후보는 2', _read(st) == 'candidates=2\ntake=0\n', repr(_read(st)))
    take = plan(d3, [], '', 9, os.path.join(d3, CARRIED), st)
    expect('ⓗ 못 받으면 후보 0', _read(st) == 'candidates=0\ntake=0\n', repr(_read(st)))
    shutil.rmtree(tmp, ignore_errors=True)
    if fails:
        print('✗ screens_carry --self-test 실패 %d' % len(fails))
        for f in fails:
            print('  · ' + f[:300])
        return 1
    print('✓ screens_carry --self-test 15칸 통과')
    return 0


def main(argv):
    ap = argparse.ArgumentParser(add_help=True)
    ap.add_argument('cmd', nargs='?', default='plan')
    ap.add_argument('--dir', default='ui-screens')
    ap.add_argument('--names', default='-')
    ap.add_argument('--from-meta', default='')
    ap.add_argument('--run', default='')
    ap.add_argument('--carried-out', default='')
    ap.add_argument('--stats-out', default='')
    ap.add_argument('--self-test', action='store_true')
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    if a.cmd != 'plan':
        print('사용: screens_carry.py plan --dir ui-screens [--names -] [--from-meta META] [--run N] | --self-test')
        return 2
    text = sys.stdin.read() if a.names == '-' else _read(a.names)
    out = a.carried_out or os.path.join(a.dir, CARRIED)
    run = None
    try:
        run = int(a.run)
    except (TypeError, ValueError):
        run = None
    for n in plan(a.dir, text.split('\n'), _read(a.from_meta), run, out, a.stats_out or None):
        print(n)
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
