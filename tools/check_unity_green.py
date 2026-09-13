#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""«main 의 유니티 잡이 마지막으로 실제로 돌았을 때 초록이었는가» (ROUTINE T123 막이 · §0-6 의 눈을 대신한다).

왜 필요한가 — `ci.yml` 의 `gate` 잡은 **문서만 바뀐 push** 면 유니티 잡을 건너뛴다(옳다 · 30분을 아낀다).
그런데 **건너뛴 잡은 실패가 아니라 skipped** 라 그 런은 통째로 `success` 로 끝난다. 그래서 빨간 유니티 런
위에 초록 문서 런이 몇 개만 쌓이면, §0-6 이 워커에게 묻는 «main 의 마지막 CI 런이 빨간가» 의 답이
**«초록»(오답)** 이 된다 — 실측 2026-09-13: 런 230(`34fbfbb`) PlayMode 빨강 1 위에 런 231·235 가 초록으로 얹혔다.
T67(런 안에서 모드 XML 이 빠졌는가)·T81(잡 결과가 스텝에 갇히는가)은 **잡이 아예 안 돈 런**에는 안 켜진다.
구멍은 «런 안» 이 아니라 «런 사이» 에 있다.

무엇을 보는가 — 네트워크 없이 `screens` 브랜치의 `meta.json` 하나다.
  `ci.yml` 의 «screens 브랜치용 meta.json» 스텝이 `{"sha","run","tests","shots","carried","missing_modes"}` 를
  **유니티 잡이 실제로 돈 main 런마다**(빨간 런이어도 · `!cancelled()`) 쓴다. 즉 그 파일이 곧
  «유니티가 마지막으로 본 커밋과 그 판정» 이다. 잡이 skipped 인 런은 그 스텝 자체가 없으니 파일을 안 건드린다.

판정(rc):
  0 — `tests == "success"`. 그 뒤에 main 커밋이 쌓여 있으면 «아직 유니티가 안 본 커밋 N개» 를 알림으로 적는다(막지 않는다).
  1 — `tests` 가 success 가 아니다(빨강·취소 등) · `missing_modes` 가 비어 있지 않다(테스트 0개인 모드 · §1 «빨간 테스트보다 나쁘다»)
      · meta.json 이 없거나 못 읽는다(판정 근거가 사라진 것도 초록이 아니다).
빨강이면 같은 브랜치의 `playmode-red.txt` 에서 `FAIL`/`RED` 줄을 뽑아 **무엇이 빨간지 이름까지** 보여 준다.

의존성 0(순수 파이썬 + git). `--fetch` 를 주면 먼저 `git fetch origin screens` 한다.

사용:  python3 tools/check_unity_green.py [--fetch] [--ref origin/screens] [--self-test]
"""
import json
import os
import subprocess
import sys

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
REF = 'origin/screens'
MAIN = 'origin/main'


def _git(args, cwd=ROOT):
    """git 을 돌려 (rc, stdout) 을 준다 — 실패해도 예외를 안 던진다(자는 판정을 내야 한다)."""
    try:
        p = subprocess.run(['git'] + args, cwd=cwd, stdout=subprocess.PIPE,
                           stderr=subprocess.PIPE, text=True)
        return p.returncode, p.stdout
    except OSError as e:
        return 127, str(e)


def read_meta(ref=REF):
    """screens 브랜치의 meta.json 을 읽어 dict 를 준다. 없으면 None."""
    rc, out = _git(['show', ref + ':meta.json'])
    if rc != 0:
        return None
    try:
        return json.loads(out)
    except ValueError:
        return None


def red_lines(ref=REF, limit=8):
    """playmode-red.txt 에서 FAIL·RED 줄만 뽑는다(없으면 빈 목록)."""
    rc, out = _git(['show', ref + ':playmode-red.txt'])
    if rc != 0:
        return []
    hits = []
    for ln in out.split('\n'):
        if ln.startswith('FAIL ') or ln.startswith('RED '):
            hits.append(ln.strip())
            if len(hits) >= limit:
                break
    return hits


def behind(sha, main=MAIN):
    """그 sha 뒤에 main 커밋이 몇 개 쌓였는가. (조상 여부, 개수) — 못 세면 (None, None)."""
    if not sha:
        return None, None
    rc, _ = _git(['merge-base', '--is-ancestor', sha, main])
    if rc == 127:
        return None, None
    anc = (rc == 0)
    if not anc:
        return False, None
    rc2, out = _git(['rev-list', '--count', sha + '..' + main])
    try:
        return True, int(out.strip())
    except ValueError:
        return True, None


def judge(meta, anc=None, n_after=None, fails=()):
    """순수 판정 — (rc, 줄 목록). 네트워크·git 없이 자기 검사할 수 있게 갈라 둔다."""
    out = []
    if meta is None:
        return 1, ['✗ check_unity_green: `%s:meta.json` 을 못 읽었다 — 유니티 판정의 근거가 없다'
                   '(먼저 `git fetch origin screens` · 그래도 없으면 유니티 잡이 한 번도 안 돈 것이다)' % REF]

    tests = str(meta.get('tests', '?'))
    run = meta.get('run', '?')
    sha = str(meta.get('sha', ''))
    miss = str(meta.get('missing_modes', '') or '')
    head = '유니티 잡이 **실제로 돈** 마지막 main 런 = #%s (%s)' % (run, sha[:7] or '?')

    bad = []
    if tests != 'success':
        bad.append('판정 «%s»' % tests)
    if miss:
        bad.append('테스트 0개인 모드 «%s»(§1 — 빨간 테스트보다 나쁘다)' % miss)

    if bad:
        out.append('✗ check_unity_green: %s — %s' % (head, ' · '.join(bad)))
        out.append('  ⚑ 이것이 이번 회차의 첫 일이다(§0-6). 그 뒤 CI 런이 «success» 로 보이더라도')
        out.append('     그 런들은 문서 push 라 유니티 잡이 **skipped** 였을 뿐이다 — 초록이 빨강을 덮은 것이다.')
        for f in fails:
            out.append('  · ' + f)
        out.append('  · 빨강의 임자: 그 커밋(%s)을 민 워커다. 그 작업의 lock 이 살아 있으면 그의 몫이고,'
                   ' lock 이 없으면 §0-6 대로 **네가 고친다**.' % (sha[:7] or '?'))
        rc = 1
    else:
        out.append('✓ check_unity_green: %s — 초록' % head)
        rc = 0

    if anc is False:
        out.append('  ⚠ 그 sha 가 %s 의 조상이 아니다 — 다른 갈래이거나 force push 가 있었다.'
                   ' 판정을 믿지 말고 다음 유니티 런을 기다린다.' % MAIN)
    elif n_after:
        out.append('  ⚠ 그 뒤로 main 커밋 %d개가 쌓였다 — 유니티가 **아직 안 본** 코드가 그만큼이다'
                   '(초록이어도 «지금 main» 의 초록은 아니다).' % n_after)
    return rc, out


def self_test():
    fails = []

    def eq(name, got, want):
        if got != want:
            fails.append('%s: 얻은 값 %r ≠ 바란 값 %r' % (name, got, want))

    # ⓐ 초록
    rc, out = judge({'sha': 'a' * 40, 'run': 100, 'tests': 'success', 'missing_modes': ''}, True, 0)
    eq('ⓐ 초록 rc', rc, 0)
    eq('ⓐ 초록 문구', out[0].startswith('✓'), True)

    # ⓑ 빨강 — 실측(런 230)
    rc, out = judge({'sha': '34fbfbb2b84e62a5fd59c4e2e4c646d4e45d88f3', 'run': 230,
                     'tests': 'failure', 'missing_modes': ''}, True, 3,
                    ['FAIL Forge.Tests.PlayMode.ForgeUiTests.불티가_… · Failed'])
    eq('ⓑ 빨강 rc', rc, 1)
    eq('ⓑ 런 번호가 보인다', '#230' in out[0], True)
    eq('ⓑ 실패 이름이 보인다', any('ForgeUiTests' in l for l in out), True)
    eq('ⓑ 쌓인 커밋 수가 보인다', any('3개' in l for l in out), True)

    # ⓒ 모드 XML 이 빠진 런은 초록이어도 빨강이다(T67)
    rc, _ = judge({'sha': 'b' * 40, 'run': 101, 'tests': 'success',
                   'missing_modes': 'playmode-results.xml'}, True, 0)
    eq('ⓒ 모드 0개 rc', rc, 1)

    # ⓓ meta 가 없으면 빨강
    rc, out = judge(None)
    eq('ⓓ meta 없음 rc', rc, 1)
    eq('ⓓ meta 없음 문구', 'meta.json' in out[0], True)

    # ⓔ 조상이 아니면 알림이 붙는다(초록이어도)
    rc, out = judge({'sha': 'c' * 40, 'run': 102, 'tests': 'success', 'missing_modes': ''}, False, None)
    eq('ⓔ 조상 아님 rc', rc, 0)
    eq('ⓔ 조상 아님 알림', any('조상이 아니다' in l for l in out), True)

    # ⓕ 초록인데 뒤에 커밋이 쌓였으면 알림만(막지 않는다)
    rc, out = judge({'sha': 'd' * 40, 'run': 103, 'tests': 'success', 'missing_modes': ''}, True, 7)
    eq('ⓕ 낡은 초록 rc', rc, 0)
    eq('ⓕ 낡은 초록 알림', any('7개' in l for l in out), True)

    # ⓖ cancelled 도 초록이 아니다
    rc, _ = judge({'sha': 'e' * 40, 'run': 104, 'tests': 'cancelled', 'missing_modes': ''}, True, 0)
    eq('ⓖ 취소 rc', rc, 1)

    if fails:
        print('✗ check_unity_green --self-test 실패 %d' % len(fails))
        for f in fails:
            print('  · ' + f)
        return 1
    print('✓ check_unity_green --self-test 14칸 통과')
    return 0


def main(argv):
    ref, do_fetch = REF, False
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--self-test':
            return self_test()
        if a == '--fetch':
            do_fetch = True
        elif a == '--ref' and i + 1 < len(argv):
            i += 1
            ref = argv[i]
        else:
            print('사용: check_unity_green.py [--fetch] [--ref origin/screens] [--self-test]')
            return 2
        i += 1

    if do_fetch:
        _git(['fetch', 'origin', 'screens'])

    meta = read_meta(ref)
    anc, n_after = behind(meta.get('sha') if meta else None)
    fails = red_lines(ref) if (meta and str(meta.get('tests')) != 'success') else []
    rc, out = judge(meta, anc, n_after, fails)
    for ln in out:
        print(ln)
    return rc


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
