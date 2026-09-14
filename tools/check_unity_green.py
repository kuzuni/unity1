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

ⓔ **«런 사이» 로 임자를 한 번 더 가린다(T148)** — 범위 열·산 lock·이력(ⓐ~ⓓ)은 전부 «그 테스트 파일» 단위라, 한 커밋이
  **남의** 자 여럿을 깨뜨리면(실측 2026-09-14 런 331: T142 4회차의 `MetaHost.Awake` 가 부팅 오버레이를 조건 없이 띄워
  `ForgeUiTests`·`PetUiTests`·`ShopUiTests`·`TextSizeGateTests` 열넷이 빨강) 어느 칸에도 안 걸려 «산 lock 이 하나도 없다 →
  네 일이다» 로 찍힌다. 그래서 «직전 **유니티가 실제로 돈 초록 런** 의 sha ↔ 이번 sha 사이의 **코드 커밋**»(`[skip ci]`·문서 전용
  제외 · 코드 = ci.yml gate 와 같은 `Assets/|Packages/|ProjectSettings/`)을 뽑아 그 제목의 `T<번호>` 중 **산 lock 이 있는 것**을
  «이 런에 새로 들어온 후보» 로 먼저 말한다. 그 커밋이 고친 테스트 파일이 이번 런에서 빨간지(«제 자도 빨강» 이면 앞에)·초록인지를 **참고로만** 적는다 —
  제 자가 초록이라고 후보에서 빼지 않는다(2회차 · 런 341: T135 카드 팝은 제 자가 초록인 채 남의 폭 자 둘을 깼다). 직전 초록 sha 는 `screens` 브랜치의 **`runs.jsonl`**(ci.yml 이 유니티 잡이 돈 런마다 한 줄 덧붙인다 ·
  screens 는 고아 커밋 하나를 force push 하므로 `meta.json` 이력은 없다 — 장부가 그 자리다)에서 읽는다. 장부가 없거나 초록이
  없으면 그렇다고 말하고 이번 sha 에서 거슬러 최근 코드 커밋 몇 개를 참고로만 보인다.

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


# T150 — 빨강 본문이 **이름까지 댄 파일**. 콘솔 에러(`RED`)로 넘어진 자는 제 파일과 무관한데,
# 사다리(ⓐ~ⓔ)는 전부 «그 테스트 파일» 단위라 엉뚱한 사람에게 간다(실측 런 343: 셰이더 에러로
# 넘어진 `AgePatternTests` 를 «T124 — lock 없다 → 네 일이다» 로 찍었다 · 진짜는 T147 의 셰이더).
ERR_PATH = re.compile(r'(Assets/[0-9A-Za-z_./\-]+\.(?:cs|shader|hlsl|cginc|mat|json|asset|unity|asmdef))')


def red_text(ref=REF):
    """`playmode-red.txt` 전문(없으면 빈 글자) — 머리줄만 보는 <see cref="red_lines"/> 와 달리 **본문**이 필요하다."""
    rc, out = _git(['show', ref + ':playmode-red.txt'])
    return out if rc == 0 else ''


def error_paths(text):
    """{픽스처: [Assets/… 경로]} — 빨강 본문이 댄 **제 것이 아닌** 파일.

    `Assets/Tests/…` 는 뺀다(그것은 넘어진 자 자신이고, 스택 줄에 늘 찍힌다).
    갈래를 안 가리고 본문 전체를 훑는 까닭: 유니티는 같은 에러를 `RED … | …` 와 `FAIL … msg …` 두 꼴로
    적는데 둘 다 경로를 그대로 담고 있어, 어느 쪽을 잡아도 같은 답이 나온다."""
    out = {}
    cur = None
    for ln in text.split('\n'):
        m = re.search(r'Forge\.Tests\.[A-Za-z]+\.([A-Za-z0-9_]+)\.', ln)
        if m:
            cur = m.group(1)
        if cur is None:
            continue
        st = ln.strip()
        if st.startswith('at ') or st.startswith('at\t'):
            continue          # 스택 줄 — 넘어진 자 자신의 파일이다
        for path in ERR_PATH.findall(ln):
            if path.startswith('Assets/Tests/'):
                continue
            out.setdefault(cur, [])
            if path not in out[cur]:
                out[cur].append(path)
    return out


def path_owners(path, progress_text):
    """그 **경로의 파일 이름**을 «범위» 열에 적은 작업 번호들 — `scope_owners` 의 확장자 일반판(T150)."""
    base = path.rsplit('/', 1)[-1]
    needle = re.compile(r'(?<![0-9A-Za-z_])' + re.escape(base) + r'\b')
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


# ---- ⓔ «런 사이» (T148) -----------------------------------------------------------------------------------------
CODE_RE = re.compile(r'^(Assets/|Packages/|ProjectSettings/)')   # ci.yml gate 의 «유니티가 읽는 나무» 와 같은 식
RUNS_FILE = 'runs.jsonl'
RECENT_CODE = 6   # 초록 런을 못 찾았을 때 참고로 보이는 최근 코드 커밋 수


def read_runs(ref=REF):
    """screens 브랜치의 `runs.jsonl`(런마다 한 줄 `{"sha","run","tests","missing_modes"}` · 오래된 것부터) → 목록. 없으면 None."""
    rc, out = _git(['show', ref + ':' + RUNS_FILE])
    if rc != 0:
        return None
    return parse_runs(out)


def parse_runs(text):
    """순수 — 깨진 줄은 건너뛴다(장부 한 줄이 깨졌다고 판정을 잃지 않는다)."""
    runs = []
    for ln in (text or '').split('\n'):
        ln = ln.strip()
        if not ln:
            continue
        try:
            d = json.loads(ln)
        except ValueError:
            continue
        if isinstance(d, dict) and d.get('sha'):
            runs.append(d)
    return runs


def last_green(runs, cur_sha):
    """장부에서 **이번 런 앞의 마지막 초록**(tests success · 모드 XML 다 있음) → (sha, run). 없으면 (None, None)."""
    if not runs:
        return None, None
    seen_cur = False
    for d in reversed(runs):
        if str(d.get('sha', '')) == str(cur_sha):
            seen_cur = True
            continue
        if not seen_cur and cur_sha:
            # 장부 꼬리에 이번 런이 아직 없을 수 있다(deploy 순서) — 그래도 이번 sha 와 같은 줄만 빼고 앞쪽을 본다
            pass
        if str(d.get('tests', '')) == 'success' and not str(d.get('missing_modes', '') or ''):
            return str(d['sha']), d.get('run')
    return None, None


def is_code_commit(title, files):
    """«유니티가 읽는» 코드 커밋인가 — `[skip ci]` 제목·문서 전용은 아니다(gate 와 같은 규칙)."""
    if '[skip ci]' in (title or ''):
        return False
    return any(CODE_RE.match(f) for f in (files or []))


def code_commits(since_sha, until_sha, limit=None):
    """`since..until` 의 코드 커밋(새 것부터) → [(sha, 제목, 파일 목록)]. since 가 없으면 until 에서 거슬러 limit 개."""
    rng = ('%s..%s' % (since_sha, until_sha)) if since_sha else until_sha
    args = ['log', '--format=%H%x09%s']
    if not since_sha:
        args.append('-%d' % ((limit or RECENT_CODE) * 6))   # lock 커밋이 코드 커밋의 몇 배라 넉넉히 본다
    rc, out = _git(args + [rng])
    if rc != 0:
        return []
    got = []
    for ln in out.split('\n'):
        if '\t' not in ln:
            continue
        sha, title = ln.split('\t', 1)
        rc2, files = _git(['diff-tree', '--no-commit-id', '--name-only', '-r', sha])
        flist = [f for f in files.split('\n') if f] if rc2 == 0 else []
        if is_code_commit(title, flist):
            got.append((sha, title.strip(), flist))
            if limit and len(got) >= limit:
                break
    return got


def test_files_of(files):
    """그 커밋이 고친 테스트 픽스처 이름들(`Assets/Tests/**/XTests.cs` → `XTests`)."""
    out = []
    for f in files or []:
        if f.startswith('Assets/Tests/') and f.endswith('.cs'):
            name = os.path.basename(f)[:-3]
            if name not in out:
                out.append(name)
    return out


def between_lines(commits, fails_fixtures, green, lock=None, now=None, no_ledger=False):
    """순수 — «런 사이» 문구(T148). commits = [(sha, 제목, 파일)] 새 것부터 · green = (sha, run) 또는 (None, None).

    갈래 셋: 초록 런을 못 찾았다 / 사이에 코드 커밋이 없다 / 하나·여럿(산 lock 이 있는 것을 먼저 · 제 자가 초록인 갈래는 아니라고)."""
    lock = lock or lock_state
    gsha, grun = green
    if not gsha:
        head = ('  · «런 사이»: 직전 초록 유니티 런을 **못 찾았다**(%s) — 이번 sha 에서 거슬러 최근 코드 커밋을 참고로만 보인다'
                % ('screens 의 `runs.jsonl` 장부가 아직 없다 — 다음 유니티 런부터 쌓인다' if no_ledger else '장부에 초록 런이 없다'))
    else:
        head = '  · «런 사이»: 직전 초록 유니티 런 #%s(%s) 뒤 **이 런에 새로 들어온 코드 커밋**' % (grun if grun is not None else '?', gsha[:7])
    if not commits:
        if gsha:
            return [head + ' — **없다**. 사이가 전부 `[skip ci]`·문서 커밋이라, 이 빨강은 새 코드가 아니라 러너·환경 갈래거나 지난 런부터 있던 것이다.']
        return [head, '    (코드 커밋을 하나도 못 읽었다 — git 이력이 얕거나 sha 가 main 의 조상이 아니다.)']
    rows = []
    cands = []        # 산 lock 을 쥔 코드 커밋 전부 — «제 자가 초록» 이라도 뺀다(아래 ⚠)
    hot = []          # 그중 제 자도 빨간 것 — 먼저 말한다
    for sha, title, files in commits:
        m = re.match(r'^T(\d+)\b', title)
        tid = ('T' + m.group(1)) if m else None
        own_tests = test_files_of(files)
        own_red = [t for t in own_tests if t in fails_fixtures]
        own_green = [t for t in own_tests if t not in fails_fixtures]
        if tid:
            alive, age = lock(tid, now)
            word = _lock_word(alive, age)
        else:
            alive, word = False, '제목이 T 로 안 시작한다'
        note = ''
        if own_red:
            note = ' · 제 자(%s)도 빨강' % '·'.join(own_red)
        elif own_green:
            # ⚠ T148 2회차 — «제 자가 초록 → 이 갈래는 아니다» 로 **빼지 않는다**. 런 341 실측: T135 2회차(카드 팝 배율)는
            #    제 자 CardPopTests 가 초록인 채 남의 폭 자 둘(T113·T111)을 깼다 — 남의 자를 깨뜨리는 커밋은 원래 제 자로는 안 잡힌다.
            note = ' · 제 자(%s)는 초록(참고 — 남의 자를 깨뜨린 커밋은 제 자로 안 잡힌다)' % '·'.join(own_green)
        rows.append('    - %s %s(%s)%s — %s' % (sha[:7], tid or '?', word, note, title[:60]))
        if tid and alive:
            cands.append(tid)
            if own_red:
                hot.append(tid)
    out = [head + ':'] + rows
    if len(cands) == 1:
        out.append('    → 범위 열로 못 가린 빨강은 **먼저 %s 의 것으로 본다**(사이 코드 커밋 중 산 lock 은 그 하나). '
                   '그의 몫이니 건드리지 말고 네 작업을 잡는다 — 임자가 아니라고 판단되면 그때 §0-6 이다.' % cands[0])
    elif len(cands) > 1:
        order = hot + [c for c in cands if c not in hot]
        out.append('    → 산 lock 을 쥔 후보 %s 를 **나란히** 둔다%s — 빨간 자가 무엇을 세우는지(부팅·오버레이·글자 하한·카드 배율…)로 눈으로 가른다.'
                   % (' · '.join(order), '(제 자도 빨간 %s 가 앞)' % ' · '.join(hot) if hot else ''))
    else:
        out.append('    → 산 lock 을 쥔 코드 커밋이 없다 — 이 칸으로도 임자가 안 나온다(§0-6 대로 네 일일 수 있다).')
    return out


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


def own_lines(fails, progress_text, sha, now=None, hist=None, lock=None, err=None):
    """빨강마다 임자 한 줄 — **빠진 테스트 파일 ↔ PROGRESS 범위 열** 로 가린다(T125).

    갈래 넷:
      ⓐ 범위에 그 파일을 적은 작업이 하나  → 그 작업이 임자다(lock 살았나 죽었나까지 말한다)
      ⓑ 여럿인데 **살아 있는 lock 이 하나** → 그 하나를 임자로(나머지는 곁들여 적는다)
      ⓒ 여럿이고 다 죽었거나 다 살았다     → 다 적고 눈으로 고르게 한다
      ⓓ 아무도 그 파일을 안 적었다         → **그 파일을 고쳐 온 커밋**(<see cref="history_owners"/>)에 산 lock 이 있으면 그의 몫(T145) ·
                                            없으면 «못 가렸다» 고 말하고 커밋을 민 워커·이력 후보를 참고로만 준다

    ⓟ **본문이 남의 파일 이름을 대면 그것이 먼저다(T150)** — 콘솔 에러로 넘어진 자는 제 파일과 무관하다.
       위 넷보다 앞에 «이 빨강의 뿌리는 <경로>(임자 …)» 를 적고, 테스트 파일 임자는 «넘어진 자» 로 뒤에 남긴다.
    """
    hist = hist or history_owners
    lock = lock or lock_state
    names = fixtures(fails)
    if not names:
        who = pusher(sha)
        return [('  · 빨강의 임자: 빠진 테스트 이름을 못 읽었다 — 그 커밋(%s)을 민 워커는 %s 다. '
                 'lock 을 눈으로 확인한다.' % (sha[:7] or '?', who or '못 가렸다'))]
    err = err or {}
    out = []
    for name in names:
        # ⓟ T150 — 빨강 본문이 «제 것이 아닌 파일» 을 댔으면 그 파일의 임자를 **먼저** 찍는다.
        for path in err.get(name, [])[:2]:
            powner = path_owners(path, progress_text)
            if not powner:
                out.append('  · `%s` 의 빨강은 **제 파일 이야기가 아니다** — 콘솔 에러가 `%s` 를 댄다(그 파일을 «범위» 로 적은 작업이 없다 · 이력으로 찾아라).'
                           % (name, path))
                continue
            pstates = [(t,) + lock(t, now) for t in powner]
            plive = [st for st in pstates if st[1]]
            pick = plive[0] if plive else pstates[0]
            out.append('  · `%s` 의 빨강은 **제 파일 이야기가 아니다** — 콘솔 에러가 `%s` 를 댄다: 그 파일의 임자 **%s**(%s)%s'
                       % (name, path, pick[0], _lock_word(pick[1], pick[2]),
                          ' — 그의 몫이니 건드리지 말고 네 작업을 잡는다.' if pick[1]
                          else ' — lock 이 없으니 §0-6 대로 **이것이 네 일이다**(넘어진 자가 아니라 **이 파일**을 고친다).'))
            out.append('    ↳ 넘어진 자(`%s`)의 임자는 아래에 그대로 남긴다 — 그 사람 몫이 아닐 수 있다.' % name)
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


def judge(meta, anc=None, n_after=None, fails=(), own=None, between=None):
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
        if between:
            out.extend(list(between))
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

    # ⓠ T150 — 빨강 본문이 **남의 파일 이름**을 대면 그 임자가 먼저다(실측 런 343: 셰이더 에러로 넘어진
    #    `AgePatternTests` 를 «T124 — lock 없다 → 네 일» 로 보냈는데 진짜는 T147 의 `EdgeOutline.shader` 였다).
    RED343 = ("\u2500\u2500 Forge.Tests.PlayMode.AgePatternTests.\ubb34\uc5c7\n"
              "RED  Error  [Forge.Tests.PlayMode.AgePatternTests.\ubb34\uc5c7]\n"
              "  |    Shader error in 'Forge/EdgeOutline': Couldn't open include file ... at Assets/Shaders/EdgeOutline.shader(21)\n"
              "FAIL Forge.Tests.PlayMode.AgePatternTests.\ubb34\uc5c7 \u00b7 Failed\n"
              "  at   at Forge.Tests.PlayMode.AgePatternTests ... in /github/workspace/Assets/Tests/PlayMode/AgePatternTests.cs:80\n")
    eq('ⓠ 본문이 댄 남의 파일을 뽑는다', error_paths(RED343), {'AgePatternTests': ['Assets/Shaders/EdgeOutline.shader']})
    eq('ⓠ 스택 줄의 제 테스트 파일은 안 센다',
       any('Assets/Tests/' in p_ for p_ in error_paths(RED343).get('AgePatternTests', [])), False)
    P4 = (P + '| T147 | 윤곽선 | 🔄 | s5 | `Assets/Shaders/EdgeOutline.shader` · `Ui/EdgeOutlineHost.cs` | — |\n')
    eq('ⓠ 경로의 임자를 가린다', path_owners('Assets/Shaders/EdgeOutline.shader', P4), ['T147'])
    lines = own_lines(['FAIL A.B.AgePatternTests.\ubb34\uc5c7 · Failed'], P4, '', lock=live,
                      err=error_paths(RED343))
    eq('ⓠ 뿌리 파일의 임자를 먼저 찍는다',
       lines and '제 파일 이야기가 아니다' in lines[0] and '**T147**' in lines[0], True)
    eq('ⓠ 넘어진 자도 남긴다', any('의 임자: **T124**' in l for l in lines), True)
    lines = own_lines(['FAIL A.B.AgePatternTests.\ubb34\uc5c7 · Failed'], P4, '', lock=live, err={})
    eq('ⓠ 본문 경로가 없으면 종전 그대로', any('제 파일 이야기가 아니다' in l for l in lines), False)

    # ⓟ T148 — 장부 읽기 · 마지막 초록
    runs = parse_runs('{"sha":"aaa","run":329,"tests":"success","missing_modes":""}\n깨진 줄\n'
                      '{"sha":"bbb","run":330,"tests":"cancelled","missing_modes":""}\n'
                      '{"sha":"ccc","run":331,"tests":"failure","missing_modes":""}\n')
    eq('ⓟ 깨진 줄은 건너뛴다', [d['run'] for d in runs], [329, 330, 331])
    eq('ⓟ 이번 런 앞의 마지막 초록', last_green(runs, 'ccc'), ('aaa', 329))
    eq('ⓟ 모드 XML 이 빠진 초록은 초록이 아니다',
       last_green(parse_runs('{"sha":"a","run":1,"tests":"success","missing_modes":"playmode-results.xml"}\n{"sha":"c","run":2,"tests":"failure"}'), 'c'),
       (None, None))
    eq('ⓟ 장부가 비면 없다', last_green([], 'c'), (None, None))
    eq('ⓟ 장부가 None 이어도 없다', last_green(None, 'c'), (None, None))
    # ⓠ 코드 커밋 판별 — gate 와 같은 규칙
    eq('ⓠ [skip ci] 는 아니다', is_code_commit('T1 lock [skip ci]', ['Assets/a.cs']), False)
    eq('ⓠ 문서 전용은 아니다', is_code_commit('T1 문서', ['docs/PROGRESS.md', 'tools/x.py']), False)
    eq('ⓠ Assets 를 만지면 코드', is_code_commit('T142 4회차', ['docs/PROGRESS.md', 'Assets/Scripts/Game/Ui/MetaHost.cs']), True)
    eq('ⓠ ProjectSettings 도 코드', is_code_commit('T1', ['ProjectSettings/GraphicsSettings.asset']), True)
    eq('ⓠ 테스트 픽스처 이름', test_files_of(['Assets/Tests/PlayMode/AudioSmokeTests.cs', 'Assets/Scripts/Game/HostSfx.cs']), ['AudioSmokeTests'])
    # ⓡ 고장 주입 — 런 331 그대로: 초록 329 뒤 코드 커밋은 T120(제 자 AudioSmokeTests 초록)·T142(산 lock) → T142 를 먼저 말한다
    C331 = [('42a3f8e' + '0' * 33, 'T142 4회차: 배선 — 진행률을 «부름» 이 아니라', ['Assets/Scripts/Core/Ui/BootLoadingRules.cs', 'Assets/Scripts/Game/Ui/MetaHost.cs', 'docs/PROGRESS.md']),
            ('15d2922' + '0' * 33, 'T120 3회차 ⓐ: 모루 두들김 셋째만 강타', ['Assets/Scripts/Game/HostSfx.cs', 'Assets/Tests/PlayMode/AudioSmokeTests.cs'])]
    F331 = ['ForgeUiTests', 'PetUiTests', 'ShopUiTests', 'TextSizeGateTests', 'BootLoadingTests']
    both_live = lambda tid, now=None: (True, 30)
    lines = between_lines(C331, F331, ('da87587' + '0' * 33, 329), lock=both_live)
    eq('ⓡ 초록 런 번호·sha 가 보인다', '#329' in lines[0] and 'da87587' in lines[0], True)
    eq('ⓡ 산 lock 둘은 나란히 · 제 자도 빨간 T142 가 앞', any('나란히' in l and l.index('T142') < l.index('T120') for l in lines if '나란히' in l), True)
    eq('ⓡ T120 은 제 자가 초록이라고만 적는다(빼지 않는다 · 2회차)', any('T120' in l and 'AudioSmokeTests' in l and '참고' in l for l in lines), True)
    eq('ⓡ «이 갈래는 아니다» 는 이제 없다', any('이 갈래는 아니다' in l for l in lines), False)
    # ⓦ T148 2회차 — 런 341 그대로: T135(카드 팝 · 제 자 CardPopTests 초록)와 T109(제 자 KeylineSpotsTests 초록)가 산 lock → 둘 다 후보
    C341 = [('7741af2' + '0' * 33, 'T109 11회차: KNOWN 빈자리', ['Assets/Scripts/Game/Ui/DungeonPopups.cs', 'Assets/Tests/PlayMode/KeylineSpotsTests.cs']),
            ('c45974f' + '0' * 33, 'T135 2회차 ⓑ: 모달 열림 카드 팝', ['Assets/Scripts/Game/Ui/CardPop.cs', 'Assets/Tests/PlayMode/CardPopTests.cs'])]
    lines = between_lines(C341, ['CraftComparePopupTests', 'GearDetailTests'], ('816f96f' + '0' * 33, 339), lock=both_live)
    eq('ⓦ T135 가 후보에 남는다', any('나란히' in l and 'T135' in l and 'T109' in l for l in lines), True)
    eq('ⓦ 제 자 초록은 참고 표시', any('T135' in l and 'CardPopTests' in l and '참고' in l for l in lines), True)
    # ⓢ 사이에 코드 커밋이 없다
    lines = between_lines([], F331, ('da87587' + '0' * 33, 329), lock=both_live)
    eq('ⓢ 없다고 말한다', any('**없다**' in l for l in lines), True)
    # ⓣ 여럿이 다 산 lock 이고 제 자로 못 가르면 나란히
    C2 = [('a' * 40, 'T142 4회차', ['Assets/x.cs']), ('b' * 40, 'T120 3회차', ['Assets/y.cs'])]
    lines = between_lines(C2, F331, ('c' * 40, 329), lock=both_live)
    eq('ⓣ 나란히 둔다', any('나란히' in l and 'T142' in l and 'T120' in l for l in lines), True)
    # 산 lock 이 하나도 없으면 «네 일일 수 있다»
    lines = between_lines(C2, F331, ('c' * 40, 329), lock=lambda tid, now=None: (False, 130))
    eq('ⓣ 죽은 lock 뿐이면 네 일일 수 있다', any('네 일일 수 있다' in l for l in lines), True)
    # ⓤ 초록 런을 못 찾았다 — 장부 없음 / 장부에 초록 없음 을 가른다 · 참고 후보는 보인다
    lines = between_lines(C2, F331, (None, None), lock=both_live, no_ledger=True)
    eq('ⓤ 장부가 없다고 말한다', any('장부가 아직 없다' in l for l in lines), True)
    eq('ⓤ 참고 후보는 보인다', any('T142' in l for l in lines), True)
    lines = between_lines(C2, F331, (None, None), lock=both_live, no_ledger=False)
    eq('ⓤ 장부에 초록이 없다고 말한다', any('장부에 초록 런이 없다' in l for l in lines), True)
    # ⓥ judge 가 between 줄을 own 줄 뒤에 그대로 붙인다(초록이면 안 붙인다)
    _, out = judge({'sha': 'f' * 40, 'run': 331, 'tests': 'failure', 'missing_modes': ''}, True, 1, (), ['  · 임자'], ['  · «런 사이»'])
    eq('ⓥ 빨강이면 붙는다', out.index('  · «런 사이»') > out.index('  · 임자'), True)
    _, out = judge({'sha': 'f' * 40, 'run': 332, 'tests': 'success', 'missing_modes': ''}, True, 0, (), None, ['  · «런 사이»'])
    eq('ⓥ 초록이면 안 붙는다', any('런 사이' in l for l in out), False)

    if fails:
        print('✗ check_unity_green --self-test 실패 %d' % len(fails))
        for f in fails:
            print('  · ' + f)
        return 1
    print('✓ check_unity_green --self-test 69칸 통과')
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
    own = own_lines(fails, read_progress(), str(meta.get('sha', '')), err=error_paths(red_text(ref))) if red else None
    between = None
    if red:
        cur = str(meta.get('sha', ''))
        runs = read_runs(ref)
        gsha, grun = last_green(runs, cur)
        commits = code_commits(gsha, cur) if gsha else code_commits(None, cur, RECENT_CODE)
        between = between_lines(commits, fixtures(fails), (gsha, grun), no_ledger=(runs is None))
    rc, out = judge(meta, anc, n_after, fails, own, between)
    for ln in out:
        print(ln)
    return rc


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
