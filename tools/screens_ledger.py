#!/usr/bin/env python3
"""screens 브랜치 장부(`runs.jsonl`)·`meta.json` 합치기 — T336.

왜 — `screens` 는 고아 커밋 하나를 **힘으로** 미는 브랜치라, 유니티 런 둘이 겹쳐 돌면 «늦게 끝난 쪽» 이
«먼저 끝난 쪽» 을 통째로 지운다(런 463 실측: 배포 success 인데 장부에 463 이 없고 meta.json 은 459 것).
`check_unity_green` 은 장부를 옳게 읽는다 — 새는 곳은 **쓰는 쪽**이라 배포 스텝이 밀기 직전 다시 받아 여기로 합친다.

  merge --theirs A --mine B --out C [--tail 400]
      두 장부의 **합집합**을 run 번호 오름차순(오래된 것부터 · 소비자 규약)으로 C 에 쓴다.
      같은 run 번호는 한 줄만(--mine 쪽 우선). 깨진 줄은 버린다. run 이 없는 줄은 본 순서대로 맨 앞.
      A·B 가 없어도 된다(없는 쪽은 빈 장부).
  newer --theirs A.meta --mine B.meta
      받은 meta.json 의 run 이 내 run 보다 **크면 rc 0**(«그쪽이 더 새 런 — 그쪽 그림·meta 를 둔다»), 아니면 rc 1.
      못 읽거나 run 이 없으면 rc 1(내 것을 민다 — 판정 근거가 없으면 지금 런이 곧 최신이다).
  --self-test   고장 주입 자기 검사.

순수 파이썬 · 네트워크 없음 · 표준 라이브러리만.
"""
import argparse
import json
import os
import sys
import tempfile

DEFAULT_TAIL = 400


def parse_lines(text):
    """장부 본문 → 줄(dict) 목록. 깨진 줄·sha 없는 줄은 건너뛴다(check_unity_green.parse_runs 와 같은 관용)."""
    out = []
    for ln in (text or '').split('\n'):
        ln = ln.strip()
        if not ln:
            continue
        try:
            d = json.loads(ln)
        except ValueError:
            continue
        if isinstance(d, dict) and d.get('sha'):
            out.append(d)
    return out


def merge_runs(theirs, mine, tail=DEFAULT_TAIL):
    """순수 — 두 목록의 합집합. run 번호로 하나씩(mine 우선) · 오름차순 · 꼬리 tail 줄."""
    by_run = {}
    no_run = []
    for src in (theirs, mine):          # mine 이 뒤라 같은 run 은 mine 이 덮는다
        for d in src:
            r = d.get('run')
            if isinstance(r, bool) or not isinstance(r, (int, float)):
                no_run.append(d)
                continue
            by_run[int(r)] = d
    rows = no_run + [by_run[k] for k in sorted(by_run)]
    if tail and tail > 0:
        rows = rows[-tail:]
    return rows


def dump_lines(rows):
    return ''.join(json.dumps(d, ensure_ascii=False, separators=(',', ':')) + '\n' for d in rows)


def read_text(path):
    if not path or not os.path.exists(path):
        return ''
    with open(path, encoding='utf-8', errors='replace') as f:
        return f.read()


def meta_run(path):
    """meta.json 의 run(int) — 없거나 못 읽으면 None."""
    try:
        d = json.loads(read_text(path) or 'null')
    except ValueError:
        return None
    if not isinstance(d, dict):
        return None
    r = d.get('run')
    if isinstance(r, bool) or not isinstance(r, (int, float)):
        return None
    return int(r)


def theirs_newer(theirs_meta, mine_meta):
    """순수 — 받은 run 이 내 run 보다 크면 True. 어느 쪽이든 못 읽으면 False(내 것을 민다)."""
    t, m = meta_run(theirs_meta), meta_run(mine_meta)
    if t is None or m is None:
        return False
    return t > m


# ---- 자기 검사 ------------------------------------------------------------------------------------------------------

def self_test():
    fails = []

    def eq(name, got, want):
        if got != want:
            fails.append('%s: got %r want %r' % (name, got, want))

    L = lambda run, sha='s%d' % 0: {'sha': sha, 'run': run, 'tests': 'success', 'missing_modes': ''}
    # ⓐ 겹친 런: 459 가 463 뒤에 끝나도 둘 다 남고 오름차순이다 (런 463 실측 재현)
    theirs = parse_lines(dump_lines([L(457), L(458), L(463)]))
    mine = parse_lines(dump_lines([L(457), L(458), L(459)]))
    got = [d['run'] for d in merge_runs(theirs, mine)]
    eq('ⓐ 합집합·오름차순', got, [457, 458, 459, 463])
    # ⓑ 같은 run 은 한 줄 · mine 우선
    a = [{'sha': 'old', 'run': 5}]
    b = [{'sha': 'new', 'run': 5}]
    eq('ⓑ 같은 run 은 mine', [d['sha'] for d in merge_runs(a, b)], ['new'])
    # ⓒ 꼬리 tail 줄만 · 가장 오래된 것부터 잘린다
    many = [L(i) for i in range(1, 11)]
    eq('ⓒ 꼬리', [d['run'] for d in merge_runs(many, [], tail=3)], [8, 9, 10])
    # ⓓ 깨진 줄·sha 없는 줄은 버린다 · run 없는 줄은 맨 앞
    txt = '{"sha":"x","run":2}\n{broken\n{"run":9}\n{"sha":"norun"}\n{"sha":"y","run":1}\n'
    eq('ⓓ 깨진 줄', [d.get('run') for d in merge_runs(parse_lines(txt), [])], [None, 1, 2])
    # ⓔ 빈 장부 — 어느 쪽이 비어도 다른 쪽 그대로
    eq('ⓔ 빈 theirs', [d['run'] for d in merge_runs([], [L(3)])], [3])
    eq('ⓔ 빈 mine', [d['run'] for d in merge_runs([L(3)], [])], [3])
    # ⓕ 왕복: dump → parse 가 같다
    rows = merge_runs(theirs, mine)
    eq('ⓕ 왕복', parse_lines(dump_lines(rows)), rows)
    # ⓖ newer — 파일로
    with tempfile.TemporaryDirectory() as td:
        def p(name, obj):
            path = os.path.join(td, name)
            with open(path, 'w') as f:
                f.write(obj if isinstance(obj, str) else json.dumps(obj))
            return path
        t463 = p('t.json', {'sha': 'a', 'run': 463})
        m459 = p('m.json', {'sha': 'b', 'run': 459})
        eq('ⓖ 463 > 459', theirs_newer(t463, m459), True)
        eq('ⓖ 459 > 463 아님', theirs_newer(m459, t463), False)
        eq('ⓖ 같은 run 은 내 것', theirs_newer(t463, t463), False)
        eq('ⓖ 못 읽으면 내 것', theirs_newer(os.path.join(td, 'none.json'), m459), False)
        eq('ⓖ 깨진 meta 는 내 것', theirs_newer(p('bad.json', '{nope'), m459), False)
        eq('ⓖ run 없는 meta 는 내 것', theirs_newer(p('norun.json', {'sha': 'z'}), m459), False)
        # ⓗ merge 명령을 파일로 — theirs 가 없어도 된다
        out = os.path.join(td, 'out.jsonl')
        p('mine.jsonl', dump_lines([L(7), L(8)]))
        rc = cmd_merge(argparse.Namespace(theirs=os.path.join(td, 'nope.jsonl'), mine=os.path.join(td, 'mine.jsonl'), out=out, tail=DEFAULT_TAIL), quiet=True)
        eq('ⓗ merge rc', rc, 0)
        eq('ⓗ merge 결과', [d['run'] for d in parse_lines(read_text(out))], [7, 8])
    # ⓘ 고장 주입: 합치기를 «덮어쓰기» 로 바꾸면 ⓐ 가 잡는가
    eq('ⓘ 고장 주입(덮어쓰기)', [d['run'] for d in mine] == [457, 458, 459, 463], False)

    if fails:
        for f in fails:
            print('✗ screens_ledger 자기 검사 — ' + f)
        return 1
    print('✓ screens_ledger 자기 검사 — 합집합·오름차순·꼬리·깨진 줄·newer 판정 · 검사 %d개' % 20)
    return 0


# ---- 명령 ----------------------------------------------------------------------------------------------------------

def cmd_merge(a, quiet=False):
    rows = merge_runs(parse_lines(read_text(a.theirs)), parse_lines(read_text(a.mine)), tail=a.tail)
    tmp = a.out + '.tmp'
    with open(tmp, 'w', encoding='utf-8') as f:
        f.write(dump_lines(rows))
    os.replace(tmp, a.out)
    runs = [d.get('run') for d in rows]
    if not quiet:
        print('screens_ledger merge — %d줄 (run %s … %s)' % (len(rows), runs[0] if runs else '-', runs[-1] if runs else '-'))
    return 0


def cmd_newer(a):
    t, m = meta_run(a.theirs), meta_run(a.mine)
    newer = theirs_newer(a.theirs, a.mine)
    print('screens_ledger newer — 받은 run %s · 내 run %s → %s' % (t, m, '그쪽이 더 새 런(둔다)' if newer else '내 것을 민다'))
    return 0 if newer else 1


def main(argv):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--self-test', action='store_true')
    sub = ap.add_subparsers(dest='cmd')
    m = sub.add_parser('merge')
    m.add_argument('--theirs', required=True)
    m.add_argument('--mine', required=True)
    m.add_argument('--out', required=True)
    m.add_argument('--tail', type=int, default=DEFAULT_TAIL)
    n = sub.add_parser('newer')
    n.add_argument('--theirs', required=True)
    n.add_argument('--mine', required=True)
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    if a.cmd == 'merge':
        return cmd_merge(a)
    if a.cmd == 'newer':
        return cmd_newer(a)
    ap.print_help()
    return 2


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
