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
빨강이면 같은 브랜치의 `playmode-red.txt` 에서 `FAIL`/`RED` 줄을 뽑아 **무엇이 빨간지 이름까지** 보여 주고,
**그 빨강이 누구 몫인지**까지 가린다 — 빠진 테스트의 픽스처 이름(`…PlayMode.AgePatternTests.…`)으로 그 파일
(`AgePatternTests.cs`)을 찾고, `docs/PROGRESS.md` 의 «범위» 열이 그 파일을 적은 작업을 임자로 삼는다(T125).
  ⚠ **커밋 제목으로 가리지 않는다.** main 은 워커 열여섯이 같이 미는 가지라 런 머리 커밋은 «마지막에 민
    사람» 일 뿐이다 — 1회차(T123)는 그렇게 가려서 런 250 의 `AgePatternTests`(T124)를 머리 커밋 제목만 보고
    **T109 의 것**으로 찍었다(실측 2026-09-13 21:2x). 임자를 틀리면 ⓐ 엉뚱한 lock 을 믿고 **진짜 임자 없는
    빨강을 지나치거나** ⓑ 그 lock 이 죽어 있으면 «이것이 네 일이다» 로 **남이 지금 고치는 자리**로 워커를
    보낸다. 범위 열로 못 가린 자리는 **«못 가렸다» 고 말하고** 커밋을 민 워커는 참고로만 준다.

의존성 0(순수 파이썬 + git). `--fetch` 를 주면 먼저 `git fetch origin screens` 한다.

사용:  python3 tools/check_unity_green.py [--fetch] [--ref origin/screens] [--self-test]
"""
import datetime
import io
import json
import os
import re
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


LOCK_MIN = 90   # 규약 `docs/claims/README.md` — 90분 지난 lock 은 죽은 것이다


def lock_state(tid, now=None):
    """그 작업 lock → (살아 있는가, 몇 분 됐는가). lock 파일이 없으면 (False, None)."""
    path = os.path.join(ROOT, 'docs', 'claims', tid + '.lock')
    try:
        with io.open(path, encoding='utf-8') as f:
            stamp = f.read().split()[0]
    except (IOError, OSError, IndexError):
        return False, None
    try:
        t0 = datetime.datetime.strptime(stamp, '%Y-%m-%dT%H:%M:%SZ')
    except ValueError:
        return True, None       # 읽을 수 없는 타임스탬프는 «살아 있다» 쪽으로 닫는다
    age = int(((now or datetime.datetime.utcnow()) - t0).total_seconds() // 60)
    return age < LOCK_MIN, age


def pusher(sha):
    """그 커밋을 민 워커의 작업 번호 — **임자가 아니다**(main 은 여럿이 미는 가지다 · T125).
    범위로 임자를 못 가렸을 때만 «누가 밀었나» 로 물러나는 자리에 쓴다."""
    if not sha:
        return None
    rc, out = _git(['log', '-1', '--format=%s', sha])
    if rc != 0:
        return None
    m = re.match(r'^T(\d+)\b', out.strip())
    return ('T' + m.group(1)) if m else None


def fixtures(fails):
    """FAIL 줄 → 빠진 테스트의 **픽스처 이름** 목록(`Forge.Tests.PlayMode.AgePatternTests.…` → `AgePatternTests`).

    테스트 이름에는 점이 없다(한글·밑줄) — 그래서 «마지막 점 앞» 이 픽스처다."""
    out = []
    for ln in fails:
        head = ln.split(' · ')[0].strip()
        parts = head.split()
        if len(parts) < 2:
            continue
        dotted = parts[1].split('.')
        if len(dotted) < 2:
            continue
        name = dotted[-2]
        if name and name not in out:
            out.append(name)
    return out


def scope_owners(fixture, progress_text):
    """그 픽스처 파일(`<이름>.cs`)을 «범위» 열에 적은 작업 번호들 — 이것이 **진짜 임자**다(T125).

    왜 커밋 제목이 아닌가: main 은 워커 열여섯이 같이 미는 가지라 런 머리 커밋은 «마지막에 민 사람»
    일 뿐이다(실측 2026-09-13 런 250: 빨강은 `AgePatternTests`(T124)인데 머리 커밋은 T109 의 것이었다).
    범위 열은 규약이 «그 작업이 여는 파일» 을 적게 한 자리라(`check_claim_scope` 가 지킨다) 여기서 읽는다."""
    # ⚠ 그냥 «담겼는가» 로 보면 짧은 이름이 긴 파일에 걸린다(`UiTests` ↔ `ForgeUiTests.cs` · 자기 검사 ⓚ).
    #   앞에 낱말 글자가 없어야 한다 — 경로 구분자 `/`·따옴표·공백 뒤라야 그 파일이다.
    needle = re.compile(r'(?<![0-9A-Za-z_])' + re.escape(fixture) + r'\.cs\b')
    out = []
    for line in progress_text.split('\n'):
        if not line.startswith('| T'):
            continue
        cells = [c.strip() for c in line.strip().strip('|').split('|')]
        if len(cells) < 5:
            continue
        m = re.fullmatch(r'T(\d+)', cells[0])
        if not m or not needle.search(cells[4]):
            continue
        tid = 'T' + m.group(1)
        if tid not in out:
            out.append(tid)
    return out


def history_owners(fixture, log=None):
    """«범위» 열이 그 파일을 안 적었을 때의 **보조 증거** — 그 파일을 고쳐 온 커밋 제목의 `T<번호>`(최근 순 · T145).

    왜 필요한가(실측 2026-09-14 런 313): 빨강이 `CoinBurstTests` 인데 그 파일을 «범위» 열에 적은 작업이 없어
    자가 «못 가렸다 → 네가 고친다» 로 보냈다. 그런데 그 자리는 **T117 이 lock 을 쥔 채 그 회차에 쓰던 단언**이었다
    (2회차 제목에 «실판매 단언» 이 적혀 있다). 범위 열을 안 적은 것은 임자의 실수지만, 자가 그 실수를
    «주인 없는 자리» 로 뒤집으면 **남의 살아 있는 작업을 건드리게 된다** — 그 갈래를 여기서 막는다.

    이력은 «누가 마지막으로 밀었나»(<see cref="pusher"/>)와 다르다: 그 **파일**을 고친 커밋만 본다."""
    if log is None:
        rc, out = _git(['log', '-12', '--format=%s', '--', '*/%s.cs' % fixture])
        if rc != 0:
            return []
        log = out
    seen = []
    for line in log.split('\n'):
        m = re.match(r'^T(\d+)\b', line.strip())
        if m and ('T' + m.group(1)) not in seen:
            seen.append('T' + m.group(1))
    return seen


def read_progress():
    try:
        return io.open(os.path.join(ROOT, 'docs', 'PROGRESS.md'), encoding='utf-8').read()
    except (IOError, OSError):
        return ''


def own_line(tid, alive, age):
    """임자 한 줄 — judge 가 그대로 찍는다(자기 검사가 이 줄만 따로 잰다)."""
    if tid is None:
        return ('  · 빨강의 임자: 그 커밋 제목이 `T<번호> …` 가 아니라 작업을 못 가렸다 — '
                'lock 을 눈으로 확인하고, 임자가 없으면 §0-6 대로 **네가 고친다**.')
    if alive:
        return ('  · 빨강의 임자: **%s** — lock 이 살아 있다(%s). 그의 몫이니 건드리지 말고 네 작업을 잡는다.'
                % (tid, ('%d분 전' % age) if age is not None else '시각을 못 읽었다'))
    return ('  · 빨강의 임자: **%s** — lock 이 없다%s. §0-6 대로 **이것이 네 일이다**.'
            % (tid, (' (마지막 갱신 %d분 전 · 90분 규약으로 죽었다)' % age) if age is not None else ''))


def _lock_word(alive, age):
    """lock 한 낱말 — «살았다(N분 전)» · «죽었다(N분 전 · 90분 규약)» · «없다» 를 **가른다**(T125 2회차)."""
    if alive:
        return 'lock %s' % (('%d분 전' % age) if age is not None else '살아 있다')
    if age is None:
        return 'lock 없다'
    return 'lock %d분 전 — 90분 규약으로 **죽었다**(뺏을 수 있다)' % age


def own_lines(fails, progress_text, sha, now=None, hist=None, lock=None):
    """빨강마다 임자 한 줄 — **빠진 테스트 파일 ↔ PROGRESS 범위 열** 로 가린다(T125).

    갈래 넷:
      ⓐ 범위에 그 파일을 적은 작업이 하나  → 그 작업이 임자다(lock 살았나 죽었나까지 말한다)
      ⓑ 여럿인데 **살아 있는 lock 이 하나** → 그 하나를 임자로(나머지는 곁들여 적는다)
      ⓒ 여럿이고 다 죽었거나 다 살았다     → 다 적고 눈으로 고르게 한다
      ⓓ 아무도 그 파일을 안 적었다         → **그 파일을 고쳐 온 커밋**(<see cref="history_owners"/>)에 산 lock 이 있으면 그의 몫(T145) ·
                                            없으면 «못 가렸다» 고 말하고 커밋을 민 워커·이력 후보를 참고로만 준다
    """
    hist = hist or history_owners
    lock = lock or lock_state
    names = fixtures(fails)
    if not names:
        who = pusher(sha)
        return [('  · 빨강의 임자: 빠진 테스트 이름을 못 읽었다 — 그 커밋(%s)을 민 워커는 %s 다. '
                 'lock 을 눈으로 확인한다.' % (sha[:7] or '?', who or '못 가렸다'))]
    out = []
    for name in names:
        cands = scope_owners(name, progress_text)
        if not cands:
            # T145 — 범위 열이 비었어도 **그 파일을 고쳐 온 작업**에 산 lock 이 있으면 그의 몫이다(남의 진행 중인 자리를 뺏지 않는다).
            hcands = hist(name) or []
            hstates = [(h,) + lock(h, now) for h in hcands]
            hlive = [st for st in hstates if st[1]]
            if hlive:
                tid, _alive, age = hlive[0]
                out.append('  · `%s` 의 임자: **%s** — «범위» 열엔 없지만 **그 파일을 고쳐 온 커밋**이 그 작업이고 %s. '
                           '그의 몫이니 건드리지 말고 네 작업을 잡는다. (임자는 «범위» 열에 `%s.cs` 를 적어라 — `check_claim_scope` 가 보는 자리다.)'
                           % (name, tid, _lock_word(True, age), name))
                continue
            who = pusher(sha)
            tail = ''
            if hstates:
                tail = ' · 그 파일을 고쳐 온 작업: %s' % ' '.join('%s(%s)' % (h, _lock_word(a, g)) for h, a, g in hstates)
            out.append('  · `%s` 의 임자: **못 가렸다** — 그 파일(`%s.cs`)을 «범위» 열에 적은 작업이 없다. '
                       '(그 커밋을 민 워커는 %s 지만 main 은 여럿이 미는 가지라 임자가 아니다.)%s '
                       'lock 을 눈으로 확인하고, 임자가 없으면 §0-6 대로 **네가 고친다**.'
                       % (name, name, who or '못 가렸다', tail))
            continue
        states = [(c,) + lock_state(c, now) for c in cands]
        live = [s for s in states if s[1]]
        if len(cands) == 1:
            out.append('  · `%s` 의 임자: ' % name + own_line(*states[0]).split(': ', 1)[1])
        elif len(live) == 1:
            rest = ' · 같은 파일을 적은 다른 작업: %s' % ' '.join(c for c, a, _g in states if not a)
            out.append('  · `%s` 의 임자: ' % name + own_line(*live[0]).split(': ', 1)[1] + rest)
        else:
            # ⚠ «lock 이 죽었다» 와 «lock 이 아예 없다» 를 한 낱말로 뭉개면 안 된다 — 앞은 §0-6 의
            #    «뺏어도 되는 자리» 이고 뒤는 «아직 아무도 안 잡은 자리» 다(실측 2026-09-14: T132 의
            #    lock 이 98분이라 죽었는데 «없다» 로 찍혀 몇 분이 지났는지도 안 보였다).
            who = ' '.join('%s(%s)' % (c, _lock_word(a, g)) for c, a, g in states)
            live_note = '' if live else ' · **산 lock 이 하나도 없다 → §0-6 대로 네 일이다**'
            out.append('  · `%s` 의 임자 후보 여럿: %s — 눈으로 고른다(살아 있는 lock 이 있으면 그의 몫)%s.'
                       % (name, who, live_note))
    return out


def judge(meta, anc=None, n_after=None, fails=(), own=None):
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
        if own:
            out.extend([own] if isinstance(own, str) else list(own))
        else:
            out.append('  · 빨강의 임자: 그 커밋(%s)을 민 워커다 — lock 을 눈으로 확인한다.' % (sha[:7] or '?'))
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

    # ⓗ 임자 줄 — lock 이 살아 있으면 «그의 몫», 없으면 «네 일»
    eq('ⓗ 산 lock 은 그의 몫', '그의 몫' in own_line('T121', True, 2), True)
    eq('ⓗ 산 lock 에 분이 보인다', '2분 전' in own_line('T121', True, 2), True)
    eq('ⓗ 없는 lock 은 네 일', '네 일이다' in own_line('T77', False, None), True)
    eq('ⓗ 죽은 lock 도 네 일', '90분 규약으로 죽었다' in own_line('T77', False, 130), True)
    eq('ⓗ 번호를 못 가리면 눈으로', '눈으로 확인' in own_line(None, False, None), True)
    # ⓘ 임자 줄이 주어지면 judge 가 그것을 그대로 쓴다(기본 문구 대신 · 한 줄도 여러 줄도)
    _, out = judge({'sha': 'f' * 40, 'run': 105, 'tests': 'failure', 'missing_modes': ''}, True, 1,
                   (), own_line('T121', True, 2))
    eq('ⓘ judge 가 임자 줄을 쓴다', any('**T121**' in l for l in out), True)
    _, out = judge({'sha': 'f' * 40, 'run': 105, 'tests': 'failure', 'missing_modes': ''}, True, 1,
                   (), ['  · 첫 줄', '  · 둘째 줄'])
    eq('ⓘ 여러 줄도 쓴다', sum(1 for l in out if '째 줄' in l or '첫 줄' in l), 2)

    # ⓙ 픽스처 읽기(T125) — 테스트 이름에는 점이 없다(한글·밑줄)
    eq('ⓙ 픽스처', fixtures(['FAIL Forge.Tests.PlayMode.AgePatternTests.장착_셀은_흐림_55 · Failed']),
       ['AgePatternTests'])
    eq('ⓙ 같은 픽스처는 한 번', fixtures(['FAIL A.B.XTests.하나 · Failed', 'FAIL A.B.XTests.둘 · Failed']),
       ['XTests'])
    eq('ⓙ 못 읽는 줄은 건너뛴다', fixtures(['FAIL 이상한줄', 'RED ']), [])

    # ⓚ **이번 회차의 실측**(T125 의 뿌리) — 런 250 의 빨강은 AgePatternTests 인데
    #    머리 커밋 20db536 의 제목은 「T109 …」 였다. 범위 열로 보면 임자는 T124 다.
    P = ('| ID | 작업 | 상태 | SID | 범위 | 메모 |\n|---|---|---|---|---|---|\n'
         '| T109 | 키라인 | 🔄 | s1 | `tools/check_keyline.py` · `Ui/LeagueSheet.cs` | — |\n'
         '| T124 | 시대 무늬 | 🔄 | s2 | `Ui/AgePattern.cs` · `Assets/Tests/PlayMode/AgePatternTests.cs` | — |\n')
    eq('ⓚ 범위로 임자를 가린다', scope_owners('AgePatternTests', P), ['T124'])
    eq('ⓚ 남의 파일은 안 집는다', scope_owners('ForgeUiTests', P), [])
    # 짧은 이름이 긴 파일에 걸리면 안 된다 — `UiTests` 는 `ForgeUiTests.cs` 가 아니다
    P3 = (P + '| T131 | 대장간 | 🔄 | s4 | `Assets/Tests/PlayMode/ForgeUiTests.cs` | — |\n')
    eq('ⓚ 짧은 이름이 긴 파일에 안 걸린다', scope_owners('UiTests', P3), [])
    eq('ⓚ 제 이름은 걸린다', scope_owners('ForgeUiTests', P3), ['T131'])
    eq('ⓚ 테스트 아닌 파일도 같은 규칙', scope_owners('AgePattern', P), ['T124'])

    # ⓛ 고장 주입 — 아무도 안 적은 픽스처면 «못 가렸다» 고 **말한다**(엉뚱한 임자를 찍지 않는다)
    lines = own_lines(['FAIL A.B.NobodysTests.무엇 · Failed'], P, '')
    eq('ⓛ 못 가렸다고 말한다', any('못 가렸다' in l for l in lines), True)
    eq('ⓛ 네가 고친다로 보낸다', any('네가 고친다' in l for l in lines), True)

    # ⓝ 죽은 lock 과 없는 lock 을 가른다(T125 2회차 · 실측: T132 의 98분 lock 이 «없다» 로 뭉개졌다)
    eq('ⓝ 산 lock', _lock_word(True, 12), 'lock 12분 전')
    eq('ⓝ 죽은 lock 은 분과 규약을 말한다', '죽었다' in _lock_word(False, 98) and '98분' in _lock_word(False, 98), True)
    eq('ⓝ 없는 lock 은 그냥 없다', _lock_word(False, None), 'lock 없다')

    # ⓞ T145 — 범위 열이 비었어도 «그 파일을 고쳐 온 작업» 에 산 lock 이 있으면 그의 몫이다
    #    (실측 2026-09-14 런 313: `CoinBurstTests` 를 아무도 범위에 안 적어 자가 «네가 고친다» 로 보냈는데
    #     그 자리는 T117 이 lock 을 쥔 채 그 회차에 쓰던 단언이었다).
    eq('ⓞ 이력에서 번호를 최근 순으로', history_owners('X', 'T117 2회차: …\nT109 6회차: …\nT117 1회차: …'), ['T117', 'T109'])
    eq('ⓞ 제목이 T 로 안 시작하면 안 센다', history_owners('X', '보고함: 무엇\nMerge branch'), [])
    live = lambda tid, now=None: (True, 7)
    dead = lambda tid, now=None: (False, 130)
    lines = own_lines(['FAIL A.B.NobodysTests.무엇 · Failed'], P, '', hist=lambda n: ['T117'], lock=live)
    eq('ⓞ 산 lock 이면 그의 몫', any('**T117**' in l and '그의 몫' in l for l in lines), True)
    eq('ⓞ 범위에 적으라고 이른다', any('«범위» 열에 `NobodysTests.cs` 를 적어라' in l for l in lines), True)
    lines = own_lines(['FAIL A.B.NobodysTests.무엇 · Failed'], P, '', hist=lambda n: ['T117'], lock=dead)
    eq('ⓞ 죽은 lock 이면 종전대로 네 일', any('못 가렸다' in l and '네가 고친다' in l for l in lines), True)
    eq('ⓞ 죽은 lock 도 이력을 참고로 보인다', any('그 파일을 고쳐 온 작업: T117' in l for l in lines), True)
    lines = own_lines(['FAIL A.B.NobodysTests.무엇 · Failed'], P, '', hist=lambda n: [], lock=dead)
    eq('ⓞ 이력도 없으면 문구가 예전 그대로', any('못 가렸다' in l and '그 파일을 고쳐 온 작업' not in l for l in lines), True)

    # ⓜ 여럿이 같은 파일을 적었으면 살아 있는 lock 쪽을 고르고, 다 죽었으면 둘 다 적는다
    P2 = (P + '| T130 | 딴것 | 🔄 | s3 | `Assets/Tests/PlayMode/AgePatternTests.cs` | — |\n')
    eq('ⓜ 후보 둘', scope_owners('AgePatternTests', P2), ['T124', 'T130'])

    if fails:
        print('✗ check_unity_green --self-test 실패 %d' % len(fails))
        for f in fails:
            print('  · ' + f)
        return 1
    print('✓ check_unity_green --self-test 42칸 통과')
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
    red = bool(meta) and str(meta.get('tests')) != 'success'
    fails = red_lines(ref) if red else []
    own = own_lines(fails, read_progress(), str(meta.get('sha', ''))) if red else None
    rc, out = judge(meta, anc, n_after, fails, own)
    for ln in out:
        print(ln)
    return rc


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
