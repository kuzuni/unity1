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
import time

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


SKIPPED_TAIL = re.compile(r'\u00b7\s*(?:Skipped|Inconclusive)\s*$')   # T416 — 옛 장부의 «FAIL … · Skipped» 꼬리


def red_lines(ref=REF, limit=8):
    """playmode-red.txt 에서 FAIL·RED 줄만 뽑는다(없으면 빈 목록).

    T416 — **«· Skipped»·«· Inconclusive» 로 끝나는 줄은 빼고 센다.** 쓰는 쪽(`RedLog.cs`)은 이제
    그런 자리를 `SKIP ` 으로 적지만, 이미 밀린 옛 런의 장부에는 `FAIL … · Skipped` 가 남아 있다 —
    그 줄을 빨강으로 세면 §0-6 이 «고칠 것 없는 자리» 를 임자까지 붙여 첫 일로 올린다(머리는 «건너뜀 N» 으로
    옳게 세고 사유까지 적는데 본문만 샜다 · T386 이 머리에 세운 갈래와 짝이다).
    """
    rc, out = _git(['show', ref + ':playmode-red.txt'])
    if rc != 0:
        return []
    hits = []
    for ln in out.split('\n'):
        if ln.startswith('FAIL ') or ln.startswith('RED '):
            if SKIPPED_TAIL.search(ln):
                continue
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


# T386 — 장부 머리의 «건너뜀» 칸. CI 가 `#   건너뜀 <이름> :: <사유>` 로 한 줄씩 박는다(ci.yml ⓑ).
#        사유가 `KNOWN T<번호>` 로 시작하면 «남의 산 lock 뒤라 지금 못 고치는 자리» 다(§1) — rc 는 안 바꾸고
#        이름과 번호를 판정 줄 옆에 남긴다. 그 번호가 **이미 닫혀 있으면**(✅·⛔·✂) 접어 둘 까닭이 사라진 것이라 rc 1 로 운다.
SKIP_LINE = re.compile(r'^#\s*건너뜀\s+(\S+)\s*::\s*(.*)$')
KNOWN_ID = re.compile(r'T(\d+)')
# 닫힌 행 — `DONE_MARKS`(T340 · ✅ 하나)와 달리 여기선 ⛔·✂ 도 «접어 둘 까닭이 사라진» 쪽이다.
CLOSED_MARKS = ('✅', '⛔', '✂')


def skip_name(full):
    """`Forge.Tests.PlayMode.BrLinesTests.승천_효과_글줄…` → `BrLinesTests.승천_효과_글줄…`(자.칸 두 토막)."""
    parts = (full or '').split('.')
    return '.'.join(parts[-2:]) if len(parts) >= 2 else (full or '?')


def skips(text):
    """`playmode-red.txt` 머리의 «건너뜀» 줄 → [(이름, 사유)]. 없으면 빈 목록."""
    out = []
    for ln in (text or '').split('\n'):
        m = SKIP_LINE.match(ln.strip())
        if m:
            out.append((m.group(1).strip(), m.group(2).strip()))
    return out


def skip_note(sk, status=None):
    """T386 — (줄 목록, 이제 켜야 할 번호 목록).

    «KNOWN T…» 사유만 «남은 일» 로 센다 — 환경 사유(그래픽 장치 없음 따위)는 남은 일이 아니다.
    `status` 는 <see cref="row_status"/> 의 표(번호 → 상태). 닫힌 번호를 대고 있는 건너뜀은 **되살릴 때**다."""
    known, envs, stale, partial = [], [], [], []
    for name, why in sk or []:
        if why.startswith('KNOWN'):
            ids = ['T' + n for n in KNOWN_ID.findall(why)]
            known.append((name, why, ids))
            # T412 — «이제 켜라» 는 그 자리가 댄 번호가 **전부** 닫혔을 때만이다(any 가 아니라 all).
            #   한 번호라도 열려 있으면 접어 둘 까닭이 남은 것이다 — 런 848 실측: 문구 «KNOWN T411·T117·T134·T333» 에서
            #   T411 이 열려 있는데(lock 23분) T117·T134 가 닫혔다고 울었고, 임자는 문구에서 닫힌 번호를 지우는 우회를 했다
            #   (그러면 «한때 그 번호가 막았다» 는 기록이 사라진다). 닫힌 것과 열린 것이 섞이면 «아직 열린 번호» 를 적어 준다.
            if status and ids:
                closed = [t for t in ids if any(mk in status.get(t, '') for mk in CLOSED_MARKS)]
                still = [t for t in ids if t not in closed]
                if closed and not still:
                    for t in closed:
                        if t not in stale:
                            stale.append(t)
                elif closed and still:
                    partial.append((name, closed, still))
        else:
            envs.append((name, why))
    lines = []
    if known:
        ids = []
        for _, _, xs in known:
            for t in xs:
                if t not in ids:
                    ids.append(t)
        lines.append('  · **남은 일 %d** — %s lock 뒤라 접어 둔 자리다(§1 · 빨강이 아니다):'
                     % (len(known), ' · '.join(ids) or '번호 없음'))
        for name, why, _ in known:
            lines.append('    - %s — %s' % (skip_name(name), why))
    if envs:
        lines.append('  · 건너뜀 %d 은 환경 사유다(남은 일이 아니다): %s'
                     % (len(envs), ' · '.join(skip_name(n) for n, _ in envs)))
    for name, closed, still in partial:
        lines.append('  · 접힌 채 둔다 — %s: %s 는 닫혔지만 **%s 이(가) 아직 열려 있다**(접어 둘 까닭이 남았다 ·'
                     ' 댄 번호가 전부 닫히면 «이제 켜라» · 문구에서 닫힌 번호를 지우지 마라 — 한때 막았던 기록이다 · T412)'
                     % (skip_name(name), ' · '.join(closed), ' · '.join(still)))
    if stale:
        lines.append('  ⚑ **이제 켜라** — %s 는 이미 닫힌 행인데(✅·⛔·✂) 그 번호를 대고 접혀 있는 자가 있다.'
                     ' 접어 둘 까닭이 사라졌으니 `Assert.Ignore` 를 걷고 그 자리를 세운다(T386 ⓒ · 댄 번호가 **전부** 닫혔다 · T412).' % ' · '.join(stale))
    return lines, stale


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


def last_commit_age(tid, now=None):
    """T348 — 그 작업 번호로 시작하는 **마지막 커밋**이 몇 분 전인가(제목 `T<번호> …` · 어느 SID 든). git 이 없거나 커밋이 없으면 None.

    왜: lock 파일 시각은 «원래 SID» 만 갱신한다. 다른 SID 가 이어받아 코드를 밀어도 lock 은 낡아 «죽었다(뺏을 수 있다)» 로 보인다
    (실측 2026-09-14 런 501: T342 lock 103분 · 그런데 워커 K 가 다른 SID 로 36분 전에 밀었다). `task_state` 가 쓰는 잣대(T187·T329)와 같다."""
    if not tid:
        return None
    rc, out = _git(['log', '-1', '--format=%ct', '-E', '--grep=^%s( |:)' % re.escape(tid)])
    if rc != 0 or not out.strip():
        return None
    try:
        ts = int(out.strip().split()[0])
    except ValueError:
        return None
    import calendar
    nowts = calendar.timegm((now or datetime.datetime.utcnow()).timetuple())
    return max(0, int((nowts - ts) // 60))


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
        name = _fixture(ln)
        if name and name not in out:
            out.append(name)
    return out


def _fixture(ln):
    """한 줄 → 픽스처 이름(못 읽으면 빈 글자).

    꼴이 둘이다 — `FAIL <점 찍힌 이름> · …` 와 `RED  Error  [<점 찍힌 이름>]`(T188).
    뒤에서는 `parts[1]` 이 `Error` 라 점이 없어 예전엔 **그냥 버려졌다** —
    그래서 «테스트는 PASS 인데 콘솔이 빨간» 빨강에 임자 줄이 한 줄도 안 나왔다(실측 런 438)."""
    head = ln.split(' · ')[0].strip()
    for tok in head.split()[1:]:
        dotted = tok.strip('[]').split('.')
        if len(dotted) >= 2 and dotted[-2]:
            return dotted[-2]
    return ''


def red_only(fails):
    """`FAIL` 없이 `RED` 줄로만 나온 픽스처 집합(T188).

    그런 자는 **넘어지지 않았다** — 바로 다음 줄이 `PASS` 다.
    빨강의 정체는 §1 «플레이 콘솔 에러 0» 을 깨뜨린 **콘솔 줄 하나** 이므로,
    단언을 찾으러 테스트 파일을 열면 헛걸음이다."""
    reds, fail = set(), set()
    for ln in fails:
        name = _fixture(ln)
        if not name:
            continue
        (reds if ln.startswith('RED ') else fail).add(name)
    return reds - fail


def expected_reds(text):
    """**일부러 낸** 콘솔 빨강의 픽스처 집합(T189).

    `RedLog`(T46)는 `Application.logMessageReceived` 에 붙어 **모든** 콘솔 빨강을 적는다 —
    테스트가 `LogAssert.Expect` 로 미리 받아 둔 것까지 똑같이 `RED` 로 적힌다(가릴 API 가 없다).
    그런데 유니티 테스트 프레임워크는 **기대 안 한 `LogType.Error` 가 뜨면 그 테스트를 넘어뜨린다** —
    그러므로 «그 테스트가 그 뒤 `PASS` 했다» 는 것이 곧 «그 빨강은 기대된 것» 이라는 증거다.
    실측 런 438 `BootGuardTests.손상_대기품_세이브로…`: 손상 세이브를 **일부러** 먹여 정본과 같은
    `console.error` 를 내는 자리이고 `LogAssert.Expect` 도 돼 있으며 바로 다음 줄이 `PASS` 다."""
    out, pending = set(), {}
    for ln in text.split('\n'):
        if ln.startswith('RED '):
            full = _dotted(ln)
            if full:
                pending[full] = _fixture(ln)
            continue
        if ln.startswith('PASS '):
            full = _dotted(ln)
            if full in pending:
                out.add(pending.pop(full))
            continue
        if ln.startswith('FAIL '):
            pending.pop(_dotted(ln), None)
    return out


def _dotted(ln):
    """한 줄에서 **점 찍힌 전체 이름**(`Forge.Tests.PlayMode.X.무엇`)을 집는다 — 없으면 빈 글자."""
    head = ln.split(' · ')[0].strip()
    for tok in head.split()[1:]:
        tok = tok.strip('[]')
        if tok.count('.') >= 2:
            return tok
    return ''


def stack_paths(text):
    """{픽스처: [Assets/… 경로]} — **RED 덩이 안의 `at` 줄**이 댄 프로덕션 파일(T188).

    T150 의 <see cref="error_paths"/> 는 `at ` 줄을 «넘어진 자 자신의 파일» 이라며 버린다 —
    단언으로 넘어진 자림 그것이 맞다. 그러나 `Debug.LogError` 가 남긴 스택은
    **그 줄을 찍은 프로덕션 코드** 다(실측 런 438: `ForgeHost.cs:422` ← `BootGuard` ← `Boot`).
    그래서 RED 줄 다음부터 다음 판정 줄(`PASS`/`FAIL`/`RED`/`──`) 전까지만 긁는다."""
    out = {}
    cur = None
    for ln in text.split('\n'):
        st = ln.strip()
        if ln.startswith('RED '):
            cur = _fixture(ln) or None
            continue
        if ln.startswith(('PASS ', 'FAIL ', '\u2500')):
            cur = None
            continue
        if cur is None or not (st.startswith('at ') or st.startswith('at\t')):
            continue
        for path in ERR_PATH.findall(ln):
            if path.startswith('Assets/Tests/'):
                continue
            out.setdefault(cur, [])
            if path not in out[cur]:
                out[cur].append(path)
    return out


def scope_owners(fixture, progress_text, status=None):
    """그 픽스처 파일(`<이름>.cs`)을 «범위» 열에 적은 작업 번호들 — 이것이 **진짜 임자**다(T125).

    왜 커밋 제목이 아닌가: main 은 워커 열여섯이 같이 미는 가지라 런 머리 커밋은 «마지막에 민 사람»
    일 뿐이다(실측 2026-09-13 런 250: 빨강은 `AgePatternTests`(T124)인데 머리 커밋은 T109 의 것이었다).
    범위 열은 규약이 «그 작업이 여는 파일» 을 적게 한 자리라(`check_claim_scope` 가 지킨다) 여기서 읽는다."""
    # ⚠ 그냥 «담겼는가» 로 보면 짧은 이름이 긴 파일에 걸린다(`UiTests` ↔ `ForgeUiTests.cs` · 자기 검사 ⓚ).
    #   앞에 낱말 글자가 없어야 한다 — 경로 구분자 `/`·따옴표·공백 뒤라야 그 파일이다.
    needle = re.compile(r'(?<![0-9A-Za-z_])' + re.escape(fixture) + r'\.cs\b')
    out = []
    if status is None:
        status = {}
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
            status[tid] = cells[2]
    return out


DEAD_MARKS = ('✂', '⛔')   # T162 — 접힌 행·폐기된 행은 «임자» 가 아니다
DONE_MARKS = ('✅',)       # T340 — ✅ 로 닫힌 행은 «그 일이 끝났다» 는 뜻이지 «지금 이 빨강의 임자» 가 아니다


def row_status(progress_text):
    """PROGRESS 표 전 행의 상태 칸(`T<번호>` → «✅ 완료» 등) — 범위 열과 무관한 후보(이력 후보)의 ✅ 를 가릴 때 쓴다(T340)."""
    st = {}
    for line in progress_text.split('\n'):
        if not line.startswith('| T'):
            continue
        cells = [c.strip() for c in line.strip().strip('|').split('|')]
        m = re.fullmatch(r'T(\d+)', cells[0]) if cells else None
        if m and len(cells) >= 3:
            st['T' + m.group(1)] = cells[2]
    return st


def split_done(cands, status, lock, now=None):
    """T340 — 후보 중 **✅ 로 닫힌 행**을 뺀다. 단 그 번호에 **산 lock** 이 있으면(닫은 뒤 누가 이어 잡았다) 종전대로 후보다.

    실측(2026-09-14 런 479): `PerfBudgetTests` 빨강에 «임자 후보 T44·T50·T64 — 산 lock 이 하나도 없다 → 네 일이다» 를
    냈는데 셋 다 오래 전에 ✅ 로 닫힌 절이었고, 진짜 임자는 그 파일을 그 시각에 고치던 **T330(산 lock)** 이었다.
    ✅ 를 ✂·⛔ 와 같은 «죽은 행» 으로 빼면 후보가 비어 `history_owners`(T145)가 돌고 그것이 T330 을 댄다."""
    keep, done = [], []
    for c in cands:
        if any(mk in status.get(c, '') for mk in DONE_MARKS) and not lock(c, now)[0]:
            done.append(c)
        else:
            keep.append(c)
    return keep, done



COMMON_STEMS = {'Ui', 'Forge', 'Core', 'Game', 'Fx', 'Meta', 'Pet', 'Boot', 'Play', 'Test', 'Tests'}


def stem_of(fixture):
    """자 이름에서 꼬리(`Tests`)를 뗀 줄기 — `EquipSwapTests` → `EquipSwap`."""
    return fixture[:-5] if fixture.endswith('Tests') else fixture


def stem_owners(fixture, progress_text, lock=None, now=None):
    """
    T362 — **자 이름 ↔ 파일 이름 짝**으로 임자를 한 칸 더 가린다.

    왜: 범위 열은 «내가 여는 파일» 을 적는 자리라 **자 파일을 안 적는 작업이 많다**. 그런데 자 이름과
    그 작업이 여는 파일은 대개 **같은 줄기**다 — 런 578 의 `EquipSwapTests` 빨강이 그랬다: 범위엔 그 자가
    없었지만 T331 이 `EquipSwapFx.cs` 를 적고 lock 을 쥔 채 그 회차에 그 파일을 고치고 있었다.
    자는 «못 가렸다 → 네가 고친다» 로 보냈고 사람이 손으로 되짚어 T331 을 찾아냈다.

    ⚠ 줄기가 짧거나 흔한 말(`Ui`·`Forge`·`Core`…)이면 **안 쓴다** — `PressFxSitesTests` 의 `Press` 같은 것이
       남의 파일에 마구 걸린다. 그리고 **산 lock 이 있는 작업만** 후보로 본다(죽은 lock 은 이 칸의 근거가 못 된다).

    돌려주는 것: [(작업번호, 그 작업이 범위에 적은 같은 줄기 파일)] — 없으면 빈 목록.
    """
    lock = lock or lock_state
    stem = stem_of(fixture)
    if len(stem) <= 3 or stem in COMMON_STEMS:
        return []
    # 줄기로 시작하는 «파일 이름»(경로 구분자·따옴표·공백 뒤) — 자 파일 자신은 빼고 센다
    needle = re.compile(r'(?<![0-9A-Za-z_])(' + re.escape(stem) + r'[0-9A-Za-z_]*\.cs)\b')
    out = []
    for line in progress_text.split('\n'):
        if not line.startswith('| T'):
            continue
        # ⚠ **«범위» 칸만** 본다(cells[4]). 줄 전체를 훑으면 **설명 칸에 인용된 파일 이름**까지 걸린다 —
        #    실측: 이 작업(T362)의 등재문이 `EquipSwapFx.cs` 를 인용해서 제가 제 짝으로 잡혔다.
        cells = [c.strip() for c in line.strip().strip('|').split('|')]
        if len(cells) < 5:
            continue
        m = re.fullmatch(r'T(\d+)', cells[0])
        if not m:
            continue
        tid = 'T' + m.group(1)
        files = [f for f in needle.findall(cells[4]) if f != fixture + '.cs']
        if not files:
            continue
        alive, _age = lock(tid, now)
        if not alive:
            continue
        out.append((tid, files[0]))
    return out


def scope_owners_split(fixture, progress_text):
    """그 파일을 «범위» 로 적은 작업을 **살아 있는 후보 ↔ 죽은 행**으로 가른다(T162).

    죽은 행 = 상태 칸이 **✂ 접음**(번호를 태운 행) 또는 **⛔ 폐기·흡수**. 그런 행은 «임자» 가 아니다 —
    실측(2026-09-14 런 381): `OfflineCollectTests` 빨강을 «임자 T161 · lock 없다 → **네 일이다**» 로 찍었는데
    T161 은 12분 전 같은 진단으로 남이 이미 고쳐 **✂ 로 접힌 행**이었다(수리는 T155 `7c10ef9`).
    즉 **이미 끝난 일을 다시 시키는** 오답이다.
    """
    status = {}
    live, dead = [], []
    for tid in scope_owners(fixture, progress_text, status):
        (dead if any(mk in status.get(tid, '') for mk in DEAD_MARKS) else live).append(tid)
    return live, dead, status


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


PROD_RE = re.compile(r'^(Assets/(?!Tests/)|Packages/|ProjectSettings/)')   # T153 — 테스트는 «남을 깨뜨릴 수 있는 줄» 이 아니다


def prod_touch(commits):
    """창 안 커밋들을 작업 번호로 묶어 «프로덕션 파일을 건드렸는가» 를 센다(T153).

    돌려주는 것: {작업ID: (그 작업 커밋 수, 프로덕션 파일을 만진 커밋 수)}.
    제목이 `T<번호> …`(§1 규약)가 아닌 커밋은 어느 작업에도 안 넣는다.
    **왜 필요한가**: 자가 «임자 T87 · lock 산다 → 건드리지 마라» 로 막았는데 창 안 T87 커밋 둘이
    새 테스트 파일·docs·lock 뿐이라 **프로덕션 0줄**이었다(검수 Q 실측 · 런 350). 그러면 그 빨강은
    아무도 안 줍는다 — T148 의 반대 방향 오답이다.
    """
    got = {}
    for sha, title, files in commits or []:
        m = re.match(r'^T(\d+)\b', (title or '').strip())
        if not m:
            continue
        tid = 'T' + m.group(1)
        n, p = got.get(tid, (0, 0))
        got[tid] = (n + 1, p + (1 if any(PROD_RE.match(f) for f in (files or [])) else 0))
    return got


def touch_note(tid, touched):
    """임자 줄 뒤에 붙는 «창 안에서 실제로 바꿨는가» 한 줄(T153) — 바꿨거나 창을 모르면 빈 문자열."""
    if not touched or tid not in touched:
        return ''
    n, p = touched[tid]
    if p > 0:
        return ''
    return ('\n    ⚠ 다만 **이 런 창에서 %s 가 바꾼 프로덕션 줄은 0 이다** — 창 안 그 작업 커밋 %d개는 '
            '테스트·문서·lock 뿐이다(T153). «그의 몫» 으로 단정하지 말고, 아래 «런 사이» 에서 '
            '**실제로 프로덕션을 바꾼 작업**을 눈으로 가른다 — 그러지 않으면 이 빨강을 아무도 안 줍는다.' % (tid, n))


def test_files_of(files):
    """그 커밋이 고친 테스트 픽스처 이름들(`Assets/Tests/**/XTests.cs` → `XTests`)."""
    out = []
    for f in files or []:
        if f.startswith('Assets/Tests/') and f.endswith('.cs'):
            name = os.path.basename(f)[:-3]
            if name not in out:
                out.append(name)
    return out


def window_owners(commits, lock=None, now=None):
    """«런 사이» 창의 코드 커밋 중 **산 lock 을 쥔 작업 번호** — 새 것부터, 겹치면 하나만 (T363).

    §0-6: «한 커밋이 남의 자 여럿을 깨뜨리면 그 자들에 산 lock 이 없어 «네 일이다» 로 보이지만
    그것은 **그 커밋 임자의 몫**이다.» 그 문장을 <see cref="own_lines"/> 가 쓰려면 창을 알아야 한다."""
    lock = lock or lock_state
    out = []
    for _sha, title, _files in (commits or []):
        m = re.match(r'^T(\d+)\b', title or '')
        if not m:
            continue
        tid = 'T' + m.group(1)
        if tid in [t for t, _a in out]:
            continue
        alive, age = lock(tid, now)
        if alive:
            out.append((tid, age))
    return out


def between_lines(commits, fails_fixtures, green, lock=None, now=None, no_ledger=False, commit_age=None):
    """순수 — «런 사이» 문구(T148). commits = [(sha, 제목, 파일)] 새 것부터 · green = (sha, run) 또는 (None, None).

    갈래 셋: 초록 런을 못 찾았다 / 사이에 코드 커밋이 없다 / 하나·여럿(산 lock 이 있는 것을 먼저 · 제 자가 초록인 갈래는 아니라고)."""
    cage = commit_age or last_commit_age
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
            word = _lock_word(alive, age, recent=(cage(tid, now) if (not alive and age is not None) else None), tid=tid)
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


def own_line(tid, alive, age, recent=None):
    """임자 한 줄 — judge 가 그대로 찍는다(자기 검사가 이 줄만 따로 잰다). `recent` = 그 번호의 마지막 커밋 나이(분 · T348)."""
    if tid is not None and not alive and age is not None and recent is not None and recent < LOCK_MIN:
        return ('  · 빨강의 임자: **%s** — lock 은 %d분 전(90분 규약으론 죽었다)이지만 그 번호로 **%d분 전 커밋**이 있다 — '
                '**다른 SID 가 이어 하는 중일 수 있다** · 뺏지 말고 `python3 tools/task_state.py %s` 로 확인한다(T348).'
                % (tid, age, recent, tid))
    if tid is None:
        return ('  · 빨강의 임자: 그 커밋 제목이 `T<번호> …` 가 아니라 작업을 못 가렸다 — '
                'lock 을 눈으로 확인하고, 임자가 없으면 §0-6 대로 **네가 고친다**.')
    if alive:
        return ('  · 빨강의 임자: **%s** — lock 이 살아 있다(%s). 그의 몫이니 건드리지 말고 네 작업을 잡는다.'
                % (tid, ('%d분 전' % age) if age is not None else '시각을 못 읽었다'))
    return ('  · 빨강의 임자: **%s** — lock 이 없다%s. §0-6 대로 **이것이 네 일이다**.'
            % (tid, (' (마지막 갱신 %d분 전 · 90분 규약으로 죽었다)' % age) if age is not None else ''))


def _lock_word(alive, age, recent=None, tid=None):
    """lock 한 낱말 — «살았다(N분 전)» · «죽었다(N분 전 · 90분 규약)» · «없다» 를 **가른다**(T125 2회차).

    T348 — 죽은 lock 이라도 그 번호로 **90분 안의 커밋**(`recent` 분)이 있으면 «뺏을 수 있다» 라 하지 않는다:
    다른 SID 가 이어 하는 중일 수 있다(lock 파일은 원래 SID 만 갱신한다). `task_state <ID>` 로 확인하라고 보낸다."""
    if alive:
        return 'lock %s' % (('%d분 전' % age) if age is not None else '살아 있다')
    if age is None:
        return 'lock 없다'
    if recent is not None and recent < LOCK_MIN:
        return ('lock %d분 전(90분 규약으론 죽었다) — 그런데 그 번호로 **%d분 전 커밋**이 있다 · **다른 SID 가 이어 하는 중일 수 있다** — '
                '뺏지 말고 `python3 tools/task_state.py %s` 로 확인하라(T348)' % (age, recent, tid or '<ID>'))
    return 'lock %d분 전 — 90분 규약으로 **죽었다**(뺏을 수 있다)' % age


# ── T344 — «모드가 통째로 안 돈» 런: 잡 로그는 컨테이너에서 못 연다(blob 리다이렉트를 프록시가 막는다) · 답은 screens 의 <모드>-log.txt(T151) ──
MODE_LOG_CAUSE = re.compile(r'\berror CS\d+\b|Shader error|compiler errors|Exception|crash|Abort|Fatal|Killed|signal', re.I)
MODE_LOG_LICENSE = re.compile(r'no available seats|Unable to activate license|License (?:activation|is not valid|has expired)|DISPLAY_ERROR', re.I)
MODE_LOG_PATH = re.compile(r'(Assets/[\w./ -]+?\.(?:cs|asmdef|shader|json))')


def mode_log_name(mode_xml):
    """`editmode-results.xml` → `editmode-log.txt`(T151 이 screens 에 올리는 이름)."""
    m = str(mode_xml or '').strip()
    return m[:-len('-results.xml')] + '-log.txt' if m.endswith('-results.xml') else ''


def read_mode_log(ref, mode_xml):
    name = mode_log_name(mode_xml)
    if not name:
        return None
    rc, out = _git(['show', '%s:%s' % (ref, name)])
    return out if rc == 0 and out.strip() else None


def mode_log_note(text, mode_xml, progress_text, meta_run=None, lock=None, now=None, commit_age=None):
    """<모드>-log.txt(T151) 를 **자가 직접 읽어** 원인 줄과 임자를 댄다(순수 · 자기 검사용).

    ⓐ 로그가 없으면 조용(빈 목록) ⓑ 머리의 런 번호가 meta 의 런과 다르면 «다른 런의 로그» 라고 말하고 그친다(screens 는 마지막 런 것 하나뿐)
    ⓒ 첫 `error CS…` 줄(없으면 첫 원인 문구 줄)을 그대로 싣고, 그 줄이 댄 `Assets/…` 파일의 임자를 «범위» 열에서 찾는다(`path_owners`)
    ⓓ 원인 줄이 라이선스 문구면 그때만 라이선스 갈래 — «보고함» 은 로그에도 근거가 없을 때만이다."""
    if not text:
        return []
    lock = lock or lock_state
    cage = commit_age or last_commit_age

    def lw(tid, alive, age):   # T348 — 죽은 lock 이면 그 번호의 마지막 커밋 나이를 같이 본다
        return _lock_word(alive, age, recent=(cage(tid, now) if (not alive and age is not None) else None), tid=tid)
    name = mode_log_name(mode_xml) or '<모드>-log.txt'
    head = text.split('\n', 1)[0]
    m = re.search(r'런 #(\d+)', head)
    log_run = int(m.group(1)) if m else None
    if meta_run is not None and log_run is not None and int(log_run) != int(meta_run):
        return ['    · 모드 로그(`%s` · T151): 머리가 런 #%s 의 것이라 **이 런(#%s)의 로그가 아니다** — screens 는 마지막 런 것 하나뿐이니 '
                '다음 런에서 다시 본다(그 전엔 이 로그로 임자를 가리지 마라).' % (name, log_run, meta_run)]
    body = [ln for ln in text.split('\n') if not ln.startswith('#')]
    def strip_no(ln):
        return re.sub(r'^\s*\d+:', '', ln).strip()
    cs = next((strip_no(ln) for ln in body if re.search(r'\berror CS\d+\b', ln)), None)
    lic = next((strip_no(ln) for ln in body if MODE_LOG_LICENSE.search(ln)), None)
    cause = cs or next((strip_no(ln) for ln in body if MODE_LOG_CAUSE.search(ln)), None)
    out = []
    if cs or (cause and not lic):
        line = cs or cause
        out.append('    · 모드 로그(`%s` · T151 · 런 #%s): 첫 원인 줄 — `%s`' % (name, log_run or '?', line[:220]))
        pm = MODE_LOG_PATH.search(line)
        if pm:
            path = pm.group(1)
            owners = path_owners(path, progress_text)
            if owners:
                states = [(t,) + lock(t, now) for t in owners]
                live = [st for st in states if st[1]]
                pick = live[0] if live else states[0]
                out.append('      ↳ 그 파일 `%s` 의 임자 **%s**(%s) — %s' % (
                    path, pick[0], lw(pick[0], pick[1], pick[2]),
                    '그의 몫이다(§1 «유니티 패키지 타입을 새로 쓰면 asmdef references 에 넣는다» 꼴이면 그 절이 고친다).' if pick[1]
                    else 'lock 이 없으니 §0-6 대로 **이것이 네 일이다** — 잡 로그가 아니라 이 줄이 근거다.'))
            else:
                out.append('      ↳ 그 파일 `%s` 을 «범위» 로 적은 작업이 없다 — §0-6 대로 네가 고친다(근거는 이 줄이다 · 보고함이 아니다).' % path)
        return out
    if lic:
        out.append('    · 모드 로그(`%s` · T151 · 런 #%s): 원인 줄이 라이선스다 — `%s` → §1 «유니티 라이선스 좌석» 갈래'
                   '(재실행 1회 → 되풀이되면 보고함). 코드 탓이 아니다.' % (name, log_run or '?', lic[:160]))
        return out
    out.append('    · 모드 로그(`%s` · T151 · 런 #%s): 원인 문구 줄이 없다 — 머리 60·꼬리 400 을 직접 읽는다'
               '(`git show origin/screens:%s`). 그래도 근거가 없을 때만 보고함이다.' % (name, log_run or '?', name))
    return out


def ledger_note(runs, missing, look=8):
    """장부(`screens/runs.jsonl`)로 «이번이 처음인가, 계속되는가» 를 센다(T172 3회차).

    왜: §1 의 갈래가 «재실행 1회 → **되풀이되면** 보고함» 인데, «되풀이» 인지 아닌지는 지금까지
    사람이 `runs.jsonl` 을 손으로 읽어야 알 수 있었다(워커 J 가 두 회차 연속 그렇게 했다).
    **사이에 멀쩡한 런이 있으면 간헐(플레이크)** · **연달아 빠지면 서 있는 파손**이다 — 갈래가 다르다.
    """
    if not runs:
        return ''
    mode = (missing or '').split(',')[0].strip()
    if not mode:
        return ''
    recent = list(runs)[-look:]
    if len(recent) < 2:
        return ''
    hit = [str(d.get('run')) for d in recent if mode in str(d.get('missing_modes', '') or '')]
    if len(hit) <= 1:
        return ('\n    · 장부: 최근 런 %d개 중 이 모드가 빠진 것은 **이번 하나뿐**이다 — 간헐(플레이크)로 보고 '
                '**다음 코드 push 의 런**을 기다린다. 그때 또 빠지면 그것이 §1 의 «되풀이» 다.' % len(recent))
    # 연달았는가(마지막 두 개가 모두 빠졌는가)
    straight = len(recent) >= 2 and mode in str(recent[-1].get('missing_modes', '') or '') \
        and mode in str(recent[-2].get('missing_modes', '') or '')
    if straight:
        return ('\n    · 장부: 최근 런 %d개 중 %d번 빠졌고 **연달아 빠지는 중**이다(런 %s) — 간헐이 아니라 '
                '**서 있는 파손**이다. 컴파일·라이선스 중 어느 쪽인지는 **`git show origin/screens:<모드>-log.txt`**(T151)로 본다 — '
                '잡 로그는 컨테이너에서 못 연다(T344) · 아래 «모드 로그» 줄이 그것을 읽은 결과다 · 로그에도 근거가 없을 때만 보고함이다.'
                % (len(recent), len(hit), ' '.join(hit)))
    return ('\n    · 장부: 최근 런 %d개 중 %d번 빠졌지만 **사이에 멀쩡한 런이 있다**(빠진 런: %s) — '
            '서 있는 파손이 아니라 **간헐**이다. §1 의 «되풀이» 로 보고 보고함에 올릴지는 '
            '**연달아 빠질 때** 정한다(지금 주인을 부르면 헛걸음이다).' % (len(recent), len(hit), ' '.join(hit)))


def own_lines(fails, progress_text, sha, now=None, hist=None, lock=None, err=None, touched=None, missing='', runs=None,
              passed=None, expected=None, mode_logs=None, meta_run=None, commit_age=None, window=None):
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
    cage = commit_age or last_commit_age

    def lw(tid, alive, age):   # T348 — 죽은 lock 이면 그 번호의 마지막 커밋 나이를 같이 본다
        return _lock_word(alive, age, recent=(cage(tid, now) if (not alive and age is not None) else None), tid=tid)

    def rc_of(tid, alive, age):
        return cage(tid, now) if (not alive and age is not None) else None
    # T172 — 모드가 통째로 안 돈 런은 **코드 임자를 찾을 일이 아니다**(빠진 테스트 이름이 아예 없다).
    #        여기서 «그 커밋을 민 워커» 를 대면 죄 없는 사람을 가리킨다(실측 런 403: PlayMode 0개인데 T156 을 댔다).
    if missing:
        both = missing.count('.xml') >= 2
        if both:
            head = ('  · 빨강의 임자: **찾지 마라 — 두 모드가 다 안 돌았다**(«%s»). 잡이 테스트를 시작조차 못 한 것이다 — '
                    '**컴파일 파손을 먼저 본다**(실측 2026-09-14 런 410~412: T106 이 실제 TMP 에 없는 '
                    '`HasCharacter(uint,bool,bool)` 를 써 PlayMode 어셈블리가 깨졌고 `563c80f` 로 회복됐다). '
                    '⚠ `dotnet build` 의 TestsPlay(T48)가 **초록이어도 안심하면 안 된다** — 스텁(`tools/dotnet/Stubs`)에만 '
                    '있는 멤버는 로컬에서 안 걸린다(§1 «유니티 패키지 타입» 줄). **`origin/screens:editmode-log.txt`·`playmode-log.txt`**(T151)의 첫 `error CS…` 줄을 본다 — 잡 로그는 컨테이너에서 못 연다(T344). '
                    '그게 아니고 그 로그가 `no available seats`·`Unable to activate license`·좌석 반납 실패면 '
                    '§1 의 **«유니티 라이선스 좌석»** 갈래다(재실행 1회 → 되풀이되면 보고함).' % missing)
        else:
            head = ('  · 빨강의 임자: **찾지 마라 — 모드가 통째로 안 돌았다**(«%s»). 빠진 테스트 이름이 없으니 '
                    '코드 임자를 가릴 근거가 없다. 한 모드만 없으면 **§1 의 «유니티 라이선스 좌석»** 갈래를 먼저 본다: '
                    '`origin/screens:<모드>-log.txt`(T151 · 잡 로그는 못 연다 · T344)가 `no available seats`·`Unable to activate license`·좌석 반납 실패면 **코드 탓이 아니다** — '
                    '재실행 1회(권한이 없으면 **다음 코드 push 의 런**을 기다린다), 되풀이되면 «주인 콘솔 에러 보고함» 에 '
                    '«유니티 라이선스 좌석» 한 줄. 그게 아니면 그 모드의 어셈블리 컴파일을 본다'
                    '(스텁에만 있는 멤버는 `dotnet build` 가 못 잡는다).' % missing)
        tail = ledger_note(runs, missing)
        out = [head + tail] if tail else [head]
        # T344 — 자가 그 로그를 직접 읽는다(못 여는 잡 로그 대신)
        for mode_xml in [x.strip() for x in str(missing).split(',') if x.strip()]:
            out.extend(mode_log_note((mode_logs or {}).get(mode_xml), mode_xml, progress_text, meta_run=meta_run, lock=lock, now=now, commit_age=cage))
        return out
    names = fixtures(fails)
    if not names:
        who = pusher(sha)
        return [('  · 빨강의 임자: 빠진 테스트 이름을 못 읽었다 — 그 커밋(%s)을 민 워커는 %s 다. '
                 'lock 을 눈으로 확인한다.' % (sha[:7] or '?', who or '못 가렸다'))]
    err = err or {}
    passed = passed or set()
    expected = expected or set()
    window = window or []
    out = []
    for name in names:
        # ⓡ T188 — RED 만 있고 FAIL 이 없는 자는 **넘어지지 않았다**(다음 줄이 PASS 다).
        #          단언을 찾아 테스트 파일을 열면 헛걸음이다 — 빨강은 콘솔 줄 하나다.
        if name in expected:
            # ⓦ T189 — 그 자는 PASS 했다. 유니티는 **기대 안 한** 콘솔 빨강이면 테스트를 넘어뜨리므로,
            #          PASS 는 곧 «이 빨강은 `LogAssert.Expect` 된 것» 이라는 증거다 — 고칠 것이 없다.
            out.append('  · `%s` 의 `RED` 는 **일부러 낸 빨강이다 — 고칠 것이 없다**. 그 자는 그 뒤 `PASS` 했고, '
                       '유니티는 **기대 안 한 콘솔 빨강이면 테스트를 넘어뜨린다** — 그러니 이 줄은 '
                       '`LogAssert.Expect` 로 미리 받아 둔 것이다(`RedLog`(T46)는 `logMessageReceived` 에 붙어 '
                       '**기대된 것까지 똑같이** 적는다 · 가릴 API 가 없다). 이 런이 빨간 까닭은 **다른 줄**에 있다.'
                       % name)
            continue
        if name in passed:
            out.append('  · `%s` 는 **넘어지지 않았다(PASS)** — 이 빨강은 그 자가 돌 때 남은 '
                       '**콘솔 에러 한 줄**이다(§1 «플레이 콘솔 에러 0»). 테스트 파일에서 단언을 찾지 마라 — '
                       '빨강을 찍은 것은 **자취(`at …`)가 댄 코드**다.' % name)
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
                       % (name, path, pick[0], lw(pick[0], pick[1], pick[2]),
                          ' — 그의 몫이니 건드리지 말고 네 작업을 잡는다.' if pick[1]
                          else (' — 뺏지 말고 `task_state` 로 먼저 확인한다(T348).' if rc_of(*pick) is not None and rc_of(*pick) < LOCK_MIN
                                else ' — lock 이 없으니 §0-6 대로 **이것이 네 일이다**(넘어진 자가 아니라 **이 파일**을 고친다).')))
            out.append('    ↳ %s(`%s`)의 임자는 아래에 그대로 남긴다 — 그 사람 몫이 아닐 수 있다.'
                       % ('빨강을 받아 적은 자' if name in passed else '넘어진 자', name))
        cands, dead, dstat = scope_owners_split(name, progress_text)
        # T340 — ✅ 로 닫힌 행도 임자가 아니다(산 lock 이 있으면 예외). 문구는 ✂·⛔ 와 가른다.
        cands, done = split_done(cands, dstat, lock, now)
        # T162 — ✂ 접음·⛔ 폐기·흡수 행은 «임자» 가 아니다(이미 끝났거나 남에게 넘어간 번호다).
        dead_note = ''
        if dead:
            dead_note = (' · ⚠ 그 파일을 «범위» 로 적었지만 **죽은 행**이라 임자에서 뺀 것: %s'
                         ' — 접힌 번호는 그 일이 **이미 끝났거나 남에게 흡수됐다**는 뜻이다(T162).'
                         % ' '.join('%s(%s)' % (t, dstat.get(t, '?')) for t in dead))
        if done:
            dead_note += (' · ⚠ 그 파일을 «범위» 로 적었지만 **✅ 로 닫힌 행**이라 임자에서 뺀 것: %s'
                          ' — 닫힌 일이다(그 절이 세운 파일이라는 참고일 뿐) · 그 자가 지금 빨간 것은 **새로 깨진 것**이니'
                          ' 임자는 «그 파일을 고쳐 온 작업»·«런 사이» 칸에서 찾는다(T340).'
                          % ' '.join('%s(%s)' % (t, dstat.get(t, '?')) for t in done))
        if not cands:
            # T145 — 범위 열이 비었어도 **그 파일을 고쳐 온 작업**에 산 lock 이 있으면 그의 몫이다(남의 진행 중인 자리를 뺏지 않는다).
            hcands = hist(name) or []
            hstates = [(h,) + lock(h, now) for h in hcands]
            hlive = [st for st in hstates if st[1]]
            if hlive:
                tid, _alive, age = hlive[0]
                out.append('  · `%s` 의 임자: **%s** — «범위» 열엔 없지만 **그 파일을 고쳐 온 커밋**이 그 작업이고 %s. '
                           '그의 몫이니 건드리지 말고 네 작업을 잡는다. (임자는 «범위» 열에 `%s.cs` 를 적어라 — `check_claim_scope` 가 보는 자리다.)'
                           % (name, tid, _lock_word(True, age), name) + dead_note)
                continue
            # T362 — 범위에도 이력에도 없을 때 **자 이름 ↔ 파일 이름 짝**을 한 칸 더 본다.
            #        런 578 실측: `EquipSwapTests` 의 임자는 `EquipSwapFx.cs` 를 범위에 적고 lock 을 쥔 T331 이었는데
            #        자는 «못 가렸다 → 네가 고친다» 로 보냈다(사람이 손으로 되짚어 찾았다).
            spair = stem_owners(name, progress_text, lock=lock, now=now)
            if spair:
                tid, fname = spair[0]
                _a, sage = lock(tid, now)
                rest = ''
                if len(spair) > 1:
                    rest = ' · 같은 줄기의 다른 산 lock: %s' % ' '.join('%s(%s)' % (t, f) for t, f in spair[1:])
                out.append('  · `%s` 의 임자: **%s** — «범위» 열엔 그 자가 없지만 **자 이름과 같은 줄기의 파일** `%s` 를 '
                           '범위로 적고 %s. 자는 대개 제가 세우는 파일과 이름을 나눠 쓴다 — 그의 몫이니 건드리지 말고 네 작업을 잡는다. '
                           '(임자는 «범위» 열에 `%s.cs` 도 적어라 — `check_claim_scope` 가 보는 자리다.)%s%s'
                           % (name, tid, fname, _lock_word(True, sage), name, rest, dead_note))
                continue

            who = pusher(sha)
            tail = ''
            if hstates:
                # T340 — 이력 후보도 ✅ 로 닫힌 절이면 «lock 없다» 가 아니라 «닫힌 행» 으로 말한다(그 절은 임자가 아니다).
                rstat = row_status(progress_text)
                _keep, hdone = split_done([h for h, _a, _g in hstates], rstat, lock, now)
                tail = ' · 그 파일을 고쳐 온 작업: %s' % ' '.join(
                    '%s(%s)' % (h, '✅ 닫힌 행' if h in hdone else lw(h, a, g)) for h, a, g in hstates)
                if hdone and len(hdone) == len(hstates):
                    tail += (' — 전부 닫힌 절이라 지금 빨강의 임자가 아니다 · **아래 «런 사이» 칸의 산 lock 커밋**(그 창에서 코드를 민 작업)이'
                             ' 먼저다(T340 · 실측 런 479: 그것이 T330 이었다).')
            # T363 — 범위에도 이력에도 임자가 없을 때, 창에 **산 lock 커밋**이 있으면 그쪽이 먼저다(§0-6).
            close = ('lock 을 눈으로 확인하고, 임자가 없으면 §0-6 대로 **네가 고친다**.')
            if window:
                close = ('**«네 일» 이라고 하기 전에**: «런 사이» 칸에 **산 lock 을 쥔 커밋**이 있다 — %s. §0-6 은 '
                         '«한 커밋이 남의 자 여럿을 깨뜨리면 그 자들에 산 lock 이 없어 «네 일이다» 로 보이지만 '
                         '그것은 **그 커밋 임자의 몫**» 이라 말한다. **그 줄을 먼저 보고**, 거기서도 아니면 네 일이다.'
                         % ' '.join('%s(%s)' % (t, _lock_word(True, a)) for t, a in window[:4]))
            out.append('  · `%s` 의 임자: **못 가렸다** — 그 파일(`%s.cs`)을 «범위» 열에 적은 **살아 있는** 작업이 없다. '
                       '(그 커밋을 민 워커는 %s 지만 main 은 여럿이 미는 가지라 임자가 아니다.)%s %s%s'
                       % (name, name, who or '못 가렸다', tail, close, dead_note))
            continue
        states = [(c,) + lock(c, now) for c in cands]   # T153 — 주입한 lock 을 쓴다(여기만 모듈 lock_state 를 불러 자기 검사가 안 닿았다)
        live = [s for s in states if s[1]]
        if len(cands) == 1:
            out.append('  · `%s` 의 임자: ' % name + own_line(*states[0], recent=rc_of(*states[0])).split(': ', 1)[1]
                       + (touch_note(states[0][0], touched) if states[0][1] else '') + dead_note)
        elif len(live) == 1:
            rest = ' · 같은 파일을 적은 다른 작업: %s' % ' '.join(c for c, a, _g in states if not a)
            out.append('  · `%s` 의 임자: ' % name + own_line(*live[0], recent=rc_of(*live[0])).split(': ', 1)[1] + rest
                       + touch_note(live[0][0], touched))
        else:
            # ⚠ «lock 이 죽었다» 와 «lock 이 아예 없다» 를 한 낱말로 뭉개면 안 된다 — 앞은 §0-6 의
            #    «뺏어도 되는 자리» 이고 뒤는 «아직 아무도 안 잡은 자리» 다(실측 2026-09-14: T132 의
            #    lock 이 98분이라 죽었는데 «없다» 로 찍혀 몇 분이 지났는지도 안 보였다).
            who = ' '.join('%s(%s)' % (c, lw(c, a, g)) for c, a, g in states)
            live_note = '' if live else ' · **산 lock 이 하나도 없다 → §0-6 대로 네 일이다**'
            out.append('  · `%s` 의 임자 후보 여럿: %s — 눈으로 고른다(살아 있는 lock 이 있으면 그의 몫)%s.'
                       % (name, who, live_note))
    return out


# ── T338 — 잡별 판정: «런은 빨간데 내 잡은 초록» 을 한 줄로 ──────────────────────────────
# ci.yml 의 잡 넷: dotnet(도구·문서·순수 C#) · datasync(data/*.json·아이콘·자들) · gate(시크릿) · unity-test(needs: [dotnet, gate]).
# 유니티 잡만 빨가면 도구·게이트·CI·문서를 민 워커의 몫은 지난 것이다 — §1 «lock 은 CI 가 그 커밋을 한 번은 돈 뒤 반납한다» 를
# 그 워커가 한 회차 더 기다리지 않게 판정 줄 뒤에 붙인다. API 를 못 부르는 환경이면 needs 관계로 추정만 한다(ⓒ).
API_REPO = os.environ.get('CHECK_UNITY_GREEN_REPO', 'kuzuni/unity1')
API_TIMEOUT = 8
UNITY_JOB = 'Unity'          # 잡 이름에 이 낱말이 들면 유니티 잡(ci.yml `Unity EditMode·PlayMode 테스트`)
JOB_SHORT = (('dotnet', 'dotnet'), ('data/', 'datasync'), ('시크릿', 'gate'), (UNITY_JOB, 'unity'))


def _short(name):
    for key, short in JOB_SHORT:
        if key in name:
            return short
    return name[:12]


def _api_json(url):
    import urllib.request
    req = urllib.request.Request(url, headers={'Accept': 'application/vnd.github+json', 'User-Agent': 'check_unity_green'})
    tok = os.environ.get('GITHUB_TOKEN') or os.environ.get('GH_TOKEN')
    if tok:
        req.add_header('Authorization', 'Bearer ' + tok)
    with urllib.request.urlopen(req, timeout=API_TIMEOUT) as r:
        return json.loads(r.read().decode('utf-8'))


def parse_jobs(payload):
    """`/actions/runs/<id>/jobs` 응답 → [{name, short, conclusion, red_steps}] (순수 · 자기 검사용)."""
    out = []
    for j in (payload or {}).get('jobs', []) or []:
        name = str(j.get('name', ''))
        red = [str(st.get('name', '')) for st in (j.get('steps') or [])
               if st.get('conclusion') not in (None, 'success', 'skipped', 'neutral')]
        out.append({'name': name, 'short': _short(name), 'conclusion': str(j.get('conclusion') or j.get('status') or '?'),
                    'red_steps': red})
    return out


def fetch_jobs(sha):
    """그 sha 의 CI 런(워크플로 이름 «CI»)의 잡 목록. 못 부르면 None(조용히 건너뛴다 · T338 ⓒ)."""
    if not sha:
        return None
    try:
        base = 'https://api.github.com/repos/%s/actions' % API_REPO
        runs = _api_json('%s/runs?head_sha=%s&per_page=10' % (base, sha))
        cand = [r for r in runs.get('workflow_runs', []) if str(r.get('name')) == 'CI'] or runs.get('workflow_runs', [])
        if not cand:
            return None
        run = sorted(cand, key=lambda r: int(r.get('run_number', 0)))[-1]
        if str(run.get('status')) != 'completed':
            return None
        return parse_jobs(_api_json('%s/runs/%s/jobs?per_page=30' % (base, run['id'])))
    except Exception:
        return None


def unity_verdict(unity_jobs):
    """유니티 잡들의 결론 → **판정**(`'success'`/`'failure'`) 또는 None (순수 · T373).

    `skipped`·`neutral` 은 안 돈 것이고 `cancelled`·`timed_out` 은 **돌다 만 것**이다 — 둘 다 판정이 아니다.
    (실측 2026-09-15: 런 621·625·629 가 `cancelled` 인데 옛 코드는 그것을 «돌았다» 로 삼켰다.)
    한 런에 유니티 잡이 여럿이면 **빨강 하나가 판정**이다(§1 — 하나라도 빨가면 그 런은 빨갛다).
    """
    conc = [j['conclusion'] for j in (unity_jobs or [])]
    if 'failure' in conc:
        return 'failure'
    if 'success' in conc:
        return 'success'
    return None


def newer_verdict(runs):
    """장부(`screens:meta.json`)보다 **뒤**에 온 런 중 유니티가 실제로 **판정을 낸 마지막 런**(순수 · T373).

    왜 있나 — §0-6 의 첫 일이 이 자의 첫 문장 위에 선다. 그런데 장부는 유니티 잡이 **자기 것을 적을 때만**
    갱신되므로, 빨강이 이미 닫힌 뒤에도 몇 분~몇십 분 낡은 채로 남는다. 실측(2026-09-15 08:0x):
    장부는 런 620(`d7f638e` failure)인데 같은 순간 런 623(`55c4c8a`)이 유니티를 실제로 돌려 `success` 였고
    그 빨강은 이미 닫혀 있었다 — 자는 그 런을 손에 쥐고도(`fetch_runs_after` 가 받아 왔다)
    `classify_after` 의 «돈 런은 관심 밖» 갈래에서 **버렸다**. 그래서 워커들이 남이 끝낸 일을 첫 일로 받았다.

    runs: [{run_number, sha, jobs}] — `fetch_runs_after` 꼴(run_number 오름차순).
    돌려주는 것: {'run', 'sha'(7자리), 'full'(전체 sha), 'verdict'} 또는 None(판정 낸 런이 하나도 없음).
    """
    best = None
    for r in runs or []:
        jobs = r.get('jobs')
        if jobs is None:
            continue
        v = unity_verdict([j for j in jobs if UNITY_JOB in j['name']])
        if v is None:
            continue
        full = str(r.get('sha') or '')
        best = {'run': r.get('run_number'), 'sha': full[:7], 'full': full, 'verdict': v}
    return best


def _run_gt(a, b):
    """런 번호 비교(순수) — 둘 다 수로 읽힐 때만 «a 가 더 새롭다» 라고 말한다."""
    try:
        return int(a) > int(b)
    except (TypeError, ValueError):
        return False


def stale_lines(meta_run, nv, red):
    """«장부가 낡았다» 를 **맨 위에** 세우는 줄(순수 · T373). nv 가 장부보다 새롭지 않으면 빈 목록."""
    if not nv or not _run_gt(nv.get('run'), meta_run):
        return []
    if nv['verdict'] == 'success':
        out = ['⚑ **장부가 낡았다** — `screens:meta.json` 은 런 #%s 인데, 그 뒤 런 **#%s (%s)** 에서'
               ' 유니티가 **실제로 돌아 초록**이다.' % (meta_run, nv['run'], nv['sha'])]
        if red:
            out.append('   → 장부에 적힌 빨강은 **이미 닫혔다** — 그것을 «이번 회차의 첫 일» 로 내주지 않는다'
                       '(남이 끝낸 일을 다시 잡는 자리다 · T153 과 같은 갈래).')
        out.append('   (장부는 유니티 잡이 **제 것을 적을 때만** 갱신된다 — 배포가 한 발 늦으면 늘 이만큼 낡는다.)')
        return out
    out = ['⚑ **장부가 낡았다** — `screens:meta.json` 은 런 #%s 인데, 그 뒤 런 **#%s (%s)** 에서'
           ' 유니티가 **실제로 돌아 빨갛다**. 지금 빨강은 그 런 것이다.' % (meta_run, nv['run'], nv['sha'])]
    if red:
        out.append('   ⚠ 아래 빨간 자 이름·임자 줄은 **런 #%s(장부) 것**이라 지금 빨강과 다를 수 있다'
                   ' — 런 #%s 의 잡 로그를 먼저 본다.' % (meta_run, nv['run']))
    return out


def classify_after(runs):
    """
    T356 — «유니티가 실제로 돈 마지막 런» **뒤**의 런들을 갈라 본다.

    지금까지 자는 그 뒤 런이 초록으로 보이면 늘 «문서 push 라 유니티 잡이 skipped 였을 뿐» 이라고 설명했다.
    그런데 `ci.yml` 의 `unity-test` 는 `needs: [dotnet, gate]` 라 **앞 잡이 빨가도 유니티가 skipped** 다 —
    실측(런 540~545): 여섯 런이 다 **코드 push** 인데 `dotnet = failure` · `Unity = skipped` 였고,
    그동안 **아무도 PlayMode 판정을 못 받았다**. «문서 push» 로 읽으면 그 막힘이 통째로 안 보인다.

    runs: [{run_number, sha, jobs}] — jobs 는 parse_jobs 꼴(없으면 None).
    돌려주는 것: (blocked, docs_only, unknown) — 각각 [{run, sha, why}] 꼴.
      blocked   = 유니티가 안 돌았고 **앞 잡이 빨갛다**(막힌 런)
      docs_only = 유니티가 안 돌았는데 앞 잡은 초록(진짜 문서 push)
      unknown   = 잡을 못 읽었다
    """
    blocked, docs_only, unknown = [], [], []
    for r in runs or []:
        jobs = r.get('jobs')
        if jobs is None:
            unknown.append({'run': r.get('run_number'), 'sha': (r.get('sha') or '')[:7]})
            continue
        unity = [j for j in jobs if UNITY_JOB in j['name']]
        # T373 — «돌았다» 와 «판정을 냈다» 는 다르다. `cancelled`·`timed_out` 은 돌다 만 것이라
        #        판정이 아니지만, 앞 잡이 멈춰 세운 것도 아니라 «막힘»·«문서 push» 어느 칸에도 안 넣는다.
        if unity_verdict(unity) is not None or any(
                j['conclusion'] not in ('skipped', 'neutral') for j in unity):
            continue                                   # 유니티가 돌았거나 돌다 만 런은 이 자의 관심 밖이다
        reds = [j for j in jobs if UNITY_JOB not in j['name']
                and j['conclusion'] not in ('success', 'skipped', 'neutral')]
        if reds:
            why = ' · '.join('%s ✗%s' % (j['short'], (' «%s»' % j['red_steps'][0]) if j['red_steps'] else '')
                             for j in reds)
            blocked.append({'run': r.get('run_number'), 'sha': (r.get('sha') or '')[:7], 'why': why})
        else:
            docs_only.append({'run': r.get('run_number'), 'sha': (r.get('sha') or '')[:7]})
    return blocked, docs_only, unknown


def blocked_lines(blocked, docs_only, unknown, still=True):
    """
    classify_after 결과 → 출력 줄(순수). 막힌 런이 있으면 **맨 위에** 설 줄들이다.

    `still` = **마지막 런도 막혀 있는가**. 지나간 막힘(누가 이미 고쳤다)을 «지금 막혀 있다» 로 말하면
    워커가 남이 끝낸 일을 다시 잡는다 — 그래서 시제를 가른다(지나간 것은 알리기만 하고 rc 를 안 올린다).
    """
    if not blocked:
        return []
    n = len(blocked)
    if still:
        out = ['⛔ **앞 잡이 빨개서 유니티가 아예 못 돌고 있다** — 그 뒤 런 %d개가 `dotnet`·`datasync`·`gate` 에서 멈췄고'
               ' **마지막 런도 막혀 있다**(`ci.yml` 의 `unity-test` 는 `needs: [dotnet, gate]`).'
               ' 그동안 **아무도 PlayMode 판정을 못 받는다**.' % n]
    else:
        out = ['⚠ (지나간 막힘 · 이미 풀렸다) 유니티가 실제로 돈 마지막 런 뒤에 **앞 잡이 빨개서 건너뛴 런 %d개**가 있었다'
               ' — 그래서 그동안 PlayMode 판정이 안 나왔다. **마지막 런은 안 막혀 있다** — 누가 이미 고쳤으니 잡지 마라.' % n]
    for b in blocked[:6]:
        out.append('   · 런 %s (%s) — %s' % (b['run'], b['sha'], b['why']))
    if n > 6:
        out.append('   · … %d개 더' % (n - 6))
    if still:
        out.append('   ⚑ 이것이 **유니티 빨강보다 먼저**다(§1 «컴파일 파손을 남긴 채 다음 작업으로 넘어가지 않는다» ·'
                   ' 막힌 런의 빨간 스텝 임자를 찾아 고친다 — 자기 lock 이든 남의 lock 이든).')
    else:
        out.append('   → 다음 유니티 런이 곧 판정을 준다 — 그때까지 lock 을 쥔 채 기다린다(§1).')
    if docs_only:
        out.append('   (그 밖에 진짜 문서 push 라 유니티가 건너뛴 런 %d개는 따로다.)' % len(docs_only))
    if unknown:
        out.append('   (잡을 못 읽은 런 %d개는 셈에서 뺐다.)' % len(unknown))
    return out


def still_blocked(runs, blocked):
    """마지막으로 **판정이 난** 런이 막힌 런인가(순수). 잡을 못 읽은 런은 건너뛴다."""
    seen = [r for r in (runs or []) if r.get('jobs') is not None]
    if not seen or not blocked:
        return False
    return seen[-1].get('run_number') in [b['run'] for b in blocked]


def last_green_any(runs):
    """장부에서 **마지막 초록**(tests success · 모드 XML 다 있음) → (sha, run). 이번 런도 포함한다(T424 · `last_green` 은 이번 런 앞만 본다)."""
    for d in reversed(runs or []):
        if str(d.get('tests', '')) == 'success' and not str(d.get('missing_modes', '') or ''):
            return str(d['sha']), d.get('run')
    return None, None


def _age_words(minutes):
    if minutes is None:
        return '?'
    m = int(minutes)
    return ('%d시간 %d분' % (m // 60, m % 60)) if m >= 60 else ('%d분' % m)


def api_run_counts(payload_runs, limit=60):
    """순수 — API `workflow_runs` 목록(런 단위 conclusion)을 결말별로 센다(T424).

    런 단위 결말이라 «유니티 잡이 skipped 였는가» 는 못 가른다(그것은 잡 목록을 런마다 다시 불러야 안다) —
    그래서 이 자는 «취소된 런» 만 정확히 세고 나머지는 성공/실패/그 밖으로 둔다. 취소가 곧 «대기 줄에서 갈아치워진 것» 이다
    (ci.yml `cancel-in-progress: false` · 대기 중인 것만 뒤 push 가 갈아치운다).
    """
    c = {'n': 0, 'success': 0, 'failure': 0, 'cancelled': 0, 'other': 0}
    for r in (payload_runs or [])[:limit]:
        if str(r.get('status')) != 'completed':
            continue
        c['n'] += 1
        k = str(r.get('conclusion') or '')
        if k in ('success', 'failure', 'cancelled'):
            c[k] += 1
        else:
            c['other'] += 1
    return c


def starve_lines(runs, cur_sha, green_age_min, n_after=None, n_code=None, api_counts=None):
    """T424 — «유니티 판정이 굶고 있는가» 한 줄(순수 · rc 는 안 건드린다 · 보고 전용).

    runs: 장부(`runs.jsonl`) · cur_sha: 장부 꼬리(유니티가 실제로 돈 마지막 런) · green_age_min: 마지막 초록 런 커밋의 나이(분 · None 이면 모름)
    n_after/n_code: 마지막 초록 뒤 main 커밋 수 / 그중 코드 커밋 수(None 이면 못 셈) · api_counts: `api_run_counts` 꼴(None 이면 API 없음 → 그 조각만 뺀다).

    왜 있나 — 실측(2026-09-16 10:0x · 검수 Q): 런 829~928 에서 유니티 초록은 #868 하나(4시간 50분 전) · 40런 중 취소 17 · 건너뜀 14 · 판정 6 · 초록 0.
    이 자는 «⚠ 그 뒤로 main 커밋 N개» 만 찍어 **왜 N 이 크는지**(대기 줄이 갈아치워진다)도 **마지막 초록이 언제였는지**도 말하지 않았다.
    """
    gsha, grun = last_green_any(runs)
    if not gsha:
        return ['  ⚑ 굶주림(T424): 장부에 초록 유니티 런이 **하나도 없다** — §0 «내 뒤 런이 초록이면 내 확인» 을 쓸 수 있는 사람이 없다 · 내 커밋을 포함한 런의 장부에서 내 자가 PASS 면 그것이 확인이다(T338·T340).']
    after = []
    seen = False
    reds = 0
    for d in runs or []:
        if seen:
            after.append(d)
            if str(d.get('tests', '')) == 'failure':
                reds += 1
        elif str(d.get('sha', '')) == gsha:
            seen = True
    head = '  ⚑ 굶주림(T424): 마지막 초록 유니티 런 **#%s**(%s) 은 **%s 전**' % (grun, gsha[:7], _age_words(green_age_min))
    tail = []
    if n_after is not None:
        tail.append('그 뒤 main 커밋 %d%s' % (n_after, ('(코드 %d)' % n_code) if n_code is not None else ''))
    tail.append('그 사이 판정 런 %d(전부 빨강)' % len(after) if after else '그 뒤 판정 런 0')
    if api_counts and api_counts.get('n'):
        tail.append('API 최근 %d 런 결말 — 성공 %d · 실패 %d · **취소 %d**(대기 줄에서 갈아치워진 것) · 그 밖 %d'
                    % (api_counts['n'], api_counts['success'], api_counts['failure'], api_counts['cancelled'], api_counts['other']))
    ln = head + ' — ' + ' · '.join(tail) + '.'
    out = [ln]
    if green_age_min is not None and green_age_min >= 120:
        out.append('    → 초록이 오래 굶었다: «내 뒤 런이 초록이면 내 확인» 을 기다리지 말고, **내 커밋을 포함한 런**(`git merge-base --is-ancestor <내 커밋> <런 sha>`)의 장부에서 **내 자가 PASS** 인지로 확인한다(T338·T340 · §0). 잡을 언제 어떻게 돌릴지는 `ci.yml` 임자의 축이다.')
    return out


def commit_age_min(sha, now=None):
    """그 sha 의 커밋 시각이 몇 분 전인가(git · 못 읽으면 None)."""
    rc, out = _git(['log', '-1', '--format=%ct', sha])
    if rc != 0:
        return None
    try:
        return max(0, int(((now or time.time()) - int(out.strip())) / 60))
    except ValueError:
        return None


def fetch_run_counts(limit=60):
    """API 한 번으로 최근 CI 런의 결말을 센다(T424). 못 부르면 None(조용히 건너뛴다)."""
    try:
        payload = _api_json('https://api.github.com/repos/%s/actions/workflows/ci.yml/runs?per_page=%d' % (API_REPO, limit))
        return api_run_counts(payload.get('workflow_runs', []), limit)
    except Exception:
        return None


def fetch_runs_after(sha, limit=12):
    """그 sha 의 CI 런 **뒤**에 온 CI 런들의 잡 목록. 못 부르면 None(조용히 건너뛴다)."""
    try:
        base = 'https://api.github.com/repos/%s/actions' % API_REPO
        # 창을 넉넉히 잡는다 — 기준 런이 창 밖으로 밀리면 «막힘» 을 통째로 못 본다(실측: 12개로는 놓쳤다).
        payload = _api_json('%s/workflows/ci.yml/runs?per_page=%d' % (base, 60))
        runs = [r for r in payload.get('workflow_runs', []) if str(r.get('status')) == 'completed']
        runs.sort(key=lambda r: int(r.get('run_number', 0)))
        # 기준 sha 의 런 다음부터
        idx = None
        for i, r in enumerate(runs):
            if str(r.get('head_sha', ''))[:7] == (sha or '')[:7]:
                idx = i
        if idx is None:
            return None                                  # 기준 런을 창에서 못 찾았다 — 짐작하지 않는다
        after = runs[idx + 1:]
        out = []
        for r in after[-limit:]:
            try:
                jobs = parse_jobs(_api_json('%s/runs/%s/jobs?per_page=30' % (base, r['id'])))
            except Exception:
                jobs = None
            out.append({'run_number': r.get('run_number'), 'sha': r.get('head_sha'), 'jobs': jobs})
        return out
    except Exception:
        return None


def job_lines(jobs, sha, unity_red=True):
    """잡별 한 줄(T338). jobs 가 None 이면 needs 관계로 추정만(오프라인) · 유니티가 빨갈 때만 부른다."""
    s7 = (sha or '')[:7] or '?'
    if jobs is None:
        if not unity_red:
            return []
        return ['  ◦ 잡별(오프라인 추정 · API 없음): 이 런에서 유니티 잡이 돌았으므로 그 앞 잡 dotnet·gate 는 초록이었다'
                '(ci.yml `needs: [dotnet, gate]`) — datasync 는 API 로만 안다. 네 커밋이 %s 에 실렸고 네 몫이 도구·문서뿐이면'
                ' `git merge-base --is-ancestor <내 커밋> %s` 로 확인한 뒤 lock 을 반납해도 된다(§1).' % (s7, s7)]
    reds = [j for j in jobs if j['conclusion'] not in ('success', 'skipped', 'neutral')]
    mine = [j for j in reds if UNITY_JOB not in j['name']]
    unity = [j for j in jobs if UNITY_JOB in j['name']]
    summary = ' · '.join('%s %s' % (j['short'], '✓' if j['conclusion'] == 'success' else ('—' if j['conclusion'] == 'skipped' else '✗'))
                         for j in jobs)
    out = ['  ◦ 잡별: ' + summary]
    if mine:
        for j in mine:
            steps = ' '.join('«%s»' % st for st in j['red_steps']) or '(빨간 스텝 이름 없음)'
            out.append('    ✗ %s 잡이 빨갛다 — 빨간 스텝 %s — 이것은 **도구·문서·데이터 갈래의 일**이다(유니티가 아니다).'
                       % (j['short'], steps))
    elif reds and unity and all(UNITY_JOB in j['name'] for j in reds):
        out.append('    → 유니티 잡만 빨갛다 — dotnet·datasync·gate 는 초록. 네 커밋이 이 런(%s)에 실렸으면 네 도구·문서·CI 몫은 지났다:'
                   ' `git merge-base --is-ancestor <내 커밋> %s` 가 0 이면 lock 을 반납해도 된다(§1 · T338).' % (s7, s7))
    elif not reds:
        out.append('    → 잡 전부 초록.')
    return out


def judge(meta, anc=None, n_after=None, fails=(), own=None, between=None, blocked=None, blocks_now=True,
          newer=None, skipped=None):
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

    # T356 — 막힌 런이 있으면 **맨 위**다: 유니티 빨강을 고쳐 봐야 그 판정을 받을 수 없다.
    blk = list(blocked or [])
    out.extend(blk)

    # T373 — 장부보다 **뒤**에 유니티가 실제로 판정을 낸 런이 있으면 그 런이 «지금» 이다.
    #        장부의 빨강은 이미 닫혔을 수 있고, 그것을 «이번 회차의 첫 일» 로 내주면 남이 끝낸 일을 다시 잡는다.
    nv = newer if (newer and _run_gt(newer.get('run'), run)) else None
    if nv:
        out.extend(stale_lines(run, nv, bool(bad)))
        head = '유니티 잡이 **실제로 돈** 마지막 main 런 = #%s (%s · **장부 #%s 보다 새롭다**)' % (
            nv['run'], nv['sha'], run)
        if nv['verdict'] == 'success':
            bad = []                       # 장부의 빨강은 닫혔다 — 초록 갈래로 간다
        elif not bad:
            bad = ['판정 «failure»']       # 장부는 초록인데 그 뒤 런이 빨갛다

    if bad:
        out.append('✗ check_unity_green: %s — %s' % (head, ' · '.join(bad)))
        out.append('  ⚑ 이것이 이번 회차의 첫 일이다(§0-6).' if blk else
                   '  ⚑ 이것이 이번 회차의 첫 일이다(§0-6). 그 뒤 CI 런이 «success» 로 보이더라도')
        if not blk:
            # 막힌 런을 확인하지 못했을 때만 «문서 push» 로 설명한다 — 확인했는데 막혀 있으면 위 ⛔ 가 이미 말했다.
            out.append('     그 런들은 문서 push 라 유니티 잡이 **skipped** 였을 뿐일 수 있다 — 초록이 빨강을 덮은 것이다.'
                       ' (앞 잡이 빨개서 못 돈 것일 수도 있다 — API 를 부를 수 있으면 자가 위에 ⛔ 로 가른다 · T356)')
        else:
            out.append('     ⚠ 다만 **아래 빨강은 런 %s 것**이다 — 그 뒤 런들은 위 ⛔ 대로 막혀서 안 돌았다.' % (head.split('#')[-1].split()[0] if '#' in head else '?'))
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
        if blk and blocks_now:
            # 마지막 유니티 런은 초록이지만 **지금** main 이 막혀 있다 — 그것이 이번 회차의 첫 일이다(rc 1).
            out.append('  ⚑ 그래도 **지금 main 은 막혀 있다**(위 ⛔) — 그것이 이번 회차의 첫 일이다.')
            rc = 1

    # T386 — «건너뜀» 칸. 남의 산 lock 뒤라 지금 못 고치는 자리는 빨강이 아니라 **남은 일**이다(§1).
    #        판정(초록/빨강)은 안 바꾸되 이름·번호를 남기고, 그 번호가 이미 닫혔으면 rc 1 «이제 켜라» 로 운다.
    if skipped:
        sk_lines, stale = skipped
        out.extend(list(sk_lines))
        if stale:
            rc = 1

    if anc is False:
        out.append('  ⚠ 그 sha 가 %s 의 조상이 아니다 — 다른 갈래이거나 force push 가 있었다.'
                   ' 판정을 믿지 말고 다음 유니티 런을 기다린다.' % MAIN)
    elif n_after:
        out.append('  ⚠ 그 뒤로 main 커밋 %d개가 쌓였다 — 유니티가 **아직 안 본** 코드가 그만큼이다'
                   '(초록이어도 «지금 main» 의 초록은 아니다).' % n_after)
    return rc, out


def self_test():
    fails = []
    cells = [0]

    def eq(name, got, want):
        # T188 — 칸 수는 **세어서** 말한다. 손으로 적던 수는 회차마다 안 맞았다(96 → 102 → 111 을 사람이 고쳤다).
        cells[0] += 1
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

    # ⓥ T188 — «테스트는 PASS 인데 콘솔이 빨간» 빨강(실측 런 438). RED 줄이 통째로 버려져 임자 줄이 **한 줄도** 안 나왔다.
    RED438 = ("\u2500\u2500 Forge.Tests.PlayMode.BootGuardTests.\uc190\uc0c1\n"
              "RED  Error  [Forge.Tests.PlayMode.BootGuardTests.\uc190\uc0c1]\n"
              "  |    restorePendingCraft: \ubc84\ub838\ub2e4\n"
              "  at   UnityEngine.Debug:LogError (object)\n"
              "  at   Forge.Game.Ui.ForgeHost:RestorePendingCraft () (at Assets/Scripts/Game/Ui/ForgeHost.cs:422)\n"
              "  at   Forge.Tests.PlayMode.BootGuardTests:\uc190\uc0c1 () (at Assets/Tests/PlayMode/BootGuardTests.cs:40)\n"
              "PASS Forge.Tests.PlayMode.BootGuardTests.\uc190\uc0c1\n"
              "\u2500\u2500 Forge.Tests.PlayMode.OtherTests.\ubb34\uc5c7\n"
              "  at   Forge.Game.Ui.NotMine:X () (at Assets/Scripts/Game/Ui/NotMine.cs:1)\n")
    RED_LN = 'RED  Error  [Forge.Tests.PlayMode.BootGuardTests.\uc190\uc0c1]'
    eq('ⓥ RED 줄에서도 픽스처를 읽는다', fixtures([RED_LN]), ['BootGuardTests'])
    eq('ⓥ FAIL 줄은 종전 그대로', fixtures(['FAIL A.B.PetUiTests.\ubb34\uc5c7 · Failed']), ['PetUiTests'])
    eq('ⓥ FAIL 이 같이 있으면 PASS 로 안 친다',
       red_only([RED_LN, 'FAIL A.B.BootGuardTests.\uc190\uc0c1 · Failed']), set())
    eq('ⓥ RED 만 있으면 PASS 로 친다', red_only([RED_LN]), {'BootGuardTests'})
    eq('ⓥ 스택이 댄 프로덕션 파일을 뽑는다',
       stack_paths(RED438), {'BootGuardTests': ['Assets/Scripts/Game/Ui/ForgeHost.cs']})
    eq('ⓥ 스택의 제 테스트 파일은 안 센다',
       any('Assets/Tests/' in p_ for p_ in stack_paths(RED438).get('BootGuardTests', [])), False)
    eq('ⓥ RED 덩이 밖의 자취는 안 샌다', 'OtherTests' in stack_paths(RED438), False)
    eq('ⓥ 본문에 경로가 없으면 조용하다', stack_paths(''), {})
    P5 = (P + '| T19 | 대장간 | 🔄 | s6 | `Assets/Scripts/Game/Ui/ForgeHost.cs` | — |\n'
              '| T157 | 부팅 격리 | ✅ | s7 | `Assets/Tests/PlayMode/BootGuardTests.cs` | — |\n')
    lines = own_lines([RED_LN], P5, '', lock=live, err=stack_paths(RED438), passed=red_only([RED_LN]))
    eq('ⓥ 먼저 «넘어지지 않았다» 를 말한다',
       lines and '넘어지지 않았다(PASS)' in lines[0] and 'BootGuardTests' in lines[0], True)
    eq('ⓥ 그 다음 스택이 댄 파일의 임자를 찍는다', any('**T19**' in l for l in lines), True)
    eq('ⓥ 꼬리표가 «넘어진 자» 가 아니다', any('빨강을 받아 적은 자' in l for l in lines), True)
    eq('ⓥ passed 가 비면 종전 문구 그대로',
       any('넘어진 자(' in l for l in
           own_lines([RED_LN], P5, '', lock=live, err=stack_paths(RED438))), True)

    # ⓦ T189 — 그 RED 는 **일부러 낸 것**이었다(다음 줄이 PASS 다 = UTF 가 LogAssert.Expect 로 받아 넘겼다).
    #    ⓥ 만으로는 «ForgeHost.cs 를 고쳐라» 로 보내 버린다 — 잘 돌고 있는 자리를 고치러 가는 길이다.
    eq('ⓦ PASS 로 닫힌 RED 는 기대된 것', expected_reds(RED438), {'BootGuardTests'})
    eq('ⓦ FAIL 로 닫히면 기대된 것이 아니다', expected_reds(RED343), set())
    eq('ⓦ PASS 가 아예 없으면 비어 있다',
       expected_reds('RED  Error  [A.B.C.\ubb34\uc5c7]\n  |    x\n'), set())
    eq('ⓦ 다른 테스트의 PASS 로는 안 닫힌다',
       expected_reds('RED  Error  [A.B.C.\ud558\ub098]\nPASS A.B.C.\ub458\n'), set())
    eq('ⓦ 전체 이름을 집는다', _dotted(RED_LN), 'Forge.Tests.PlayMode.BootGuardTests.\uc190\uc0c1')
    eq('ⓦ 점이 모자라면 빈 글자', _dotted('RED  Error  [Nope]'), '')
    lines = own_lines([RED_LN], P5, '', lock=live, err=stack_paths(RED438),
                      passed=red_only([RED_LN]), expected=expected_reds(RED438))
    eq('ⓦ 고칠 것이 없다고 말한다',
       len(lines) == 1 and '일부러 낸 빨강이다' in lines[0], True)
    eq('ⓦ 임자 사다리를 아예 안 탄다', any('임자' in l and 'T19' in l for l in lines), False)

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

    # ⓦ T153 — «이 런 창에서 프로덕션 0줄인 작업» 을 임자로 단정하지 않는다
    W_T87 = [('a' * 40, 'T87 34회차(닫기 회차): 시작점 대조 (sess-x · 워커 G)',
              ['Assets/Tests/PlayMode/CraftRevealSeamTests.cs', 'docs/PROGRESS.md']),
             ('b' * 40, 'T87 선점 lock (sess-x · 워커 G)', ['docs/claims/T87.lock']),
             ('c' * 40, 'T147 2회차: 윤곽선 (sess-y · 워커 K)',
              ['Assets/Shaders/EdgeOutline.shader', 'Assets/Settings/UniversalRenderer.asset'])]
    touched = prod_touch(W_T87)
    eq('ⓦ T87 은 창 안 커밋 둘·프로덕션 0', touched.get('T87'), (2, 0))
    eq('ⓦ T147 은 프로덕션을 바꿨다', touched.get('T147'), (1, 1))
    eq('ⓦ 프로덕션 0 이면 경고가 붙는다', '프로덕션 줄은 0' in touch_note('T87', touched), True)
    eq('ⓦ 경고에 커밋 수가 보인다', '커밋 2개' in touch_note('T87', touched), True)
    eq('ⓦ 실제로 바꿨으면 조용하다', touch_note('T147', touched), '')
    eq('ⓦ 창을 모르면 조용하다', touch_note('T87', None), '')
    eq('ⓦ 창 밖 작업이면 조용하다', touch_note('T999', touched), '')
    # 임자 줄에 실제로 얹히는가 — 산 lock 갈래 둘(하나뿐 · 여럿 중 하나)
    PROG_ONE = '| T87 | 대장간 | 🔄 진행 | s / 워커 G | `Assets/Tests/PlayMode/ForgeUiTests.cs` | x |'
    lines = own_lines(['FAIL Forge.Tests.PlayMode.ForgeUiTests.가 · Failed'], PROG_ONE, 'a' * 40,
                      lock=both_live, touched=touched)
    eq('ⓦ 하나뿐 갈래에 경고가 붙는다', any('프로덕션 줄은 0' in l for l in lines), True)
    PROG_TWO = (PROG_ONE + '\n| T94 | 딤 | ⬜ 대기 | — | `Assets/Tests/PlayMode/ForgeUiTests.cs` | x |')
    only87 = lambda tid, now=None: ((True, 5) if tid == 'T87' else (False, None))
    lines = own_lines(['FAIL Forge.Tests.PlayMode.ForgeUiTests.가 · Failed'], PROG_TWO, 'a' * 40,
                      lock=only87, touched=touched)
    eq('ⓦ 여럿 중 산 lock 하나 갈래에도 붙는다', any('프로덕션 줄은 0' in l for l in lines), True)
    # 프로덕션을 실제로 바꾼 임자는 지금대로 조용하다(T147·T152 갈래)
    PROG_147 = '| T147 | 윤곽선 | 🔄 진행 | s / 워커 K | `Assets/Tests/PlayMode/EdgeOutlineTests.cs` | x |'
    lines = own_lines(['FAIL Forge.Tests.PlayMode.EdgeOutlineTests.가 · Failed'], PROG_147, 'c' * 40,
                      lock=both_live, touched=touched)
    eq('ⓦ 바꾼 임자는 경고 없이 지금대로', any('프로덕션 줄은 0' in l for l in lines), False)

    # ⓧ T162 — ✂ 접음·⛔ 폐기 행은 임자가 아니다
    P_DEAD = ('| T161 | 오프라인 자 | ✂ 접음 | 워커 I | `Assets/Tests/PlayMode/OfflineCollectTests.cs` | x |\n'
              '| T170 | 딴 일 | ⛔ 폐기·흡수 | 워커 Z | `Assets/Tests/PlayMode/OfflineCollectTests.cs` | x |')
    live, dead, dstat = scope_owners_split('OfflineCollectTests', P_DEAD)
    eq('ⓧ 죽은 행만 있으면 살아 있는 후보 0', live, [])
    eq('ⓧ 접힘·폐기를 둘 다 죽은 행으로', dead, ['T161', 'T170'])
    eq('ⓧ 상태 낱말을 그대로 쥔다', dstat.get('T161'), '✂ 접음')
    no_hist = lambda name, log=None: []
    lines = own_lines(['FAIL Forge.Tests.PlayMode.OfflineCollectTests.가 · Failed'], P_DEAD, 'a' * 40,
                      lock=both_live, hist=no_hist)
    eq('ⓧ «네 일이다» 로 단정하지 않는다', any('임자: **T161**' in l for l in lines), False)
    eq('ⓧ 죽은 행이었다고 말한다', any('죽은 행' in l and 'T161(✂ 접음)' in l for l in lines), True)
    eq('ⓧ 살아 있는 작업이 없다고 말한다', any('**살아 있는** 작업이 없다' in l for l in lines), True)
    # 살아 있는 행이 섞여 있으면 그쪽이 임자고, 죽은 행은 꼬리로만 붙는다
    P_MIX = P_DEAD + '\n| T171 | 산 일 | 🔄 진행 | 워커 Y | `Assets/Tests/PlayMode/OfflineCollectTests.cs` | x |'
    live2, dead2, _ = scope_owners_split('OfflineCollectTests', P_MIX)
    eq('ⓧ 섞이면 산 것만 후보', live2, ['T171'])
    lines = own_lines(['FAIL Forge.Tests.PlayMode.OfflineCollectTests.가 · Failed'], P_MIX, 'a' * 40,
                      lock=both_live, hist=no_hist)
    eq('ⓧ 산 행이 임자가 된다', any('임자: **T171**' in l for l in lines), True)
    eq('ⓧ 죽은 행은 꼬리로만', any('죽은 행' in l for l in lines), True)

    # ⓨ T172 — 모드가 통째로 안 돈 런은 코드 임자를 가리키지 않는다
    P_ANY = '| T156 | 리본 | 🔄 진행 | s / 워커 X | `Assets/Tests/PlayMode/CraftComparePopupTests.cs` | x |'
    lines = own_lines([], P_ANY, 'a' * 40, lock=both_live, missing='playmode-results.xml')
    eq('ⓨ 한 줄만 낸다', len(lines), 1)
    eq('ⓨ «찾지 마라» 로 시작', '찾지 마라 — 모드가 통째로 안 돌았다' in lines[0], True)
    eq('ⓨ 빠진 모드 이름을 싣는다', 'playmode-results.xml' in lines[0], True)
    eq('ⓨ §1 라이선스 좌석 갈래로 보낸다', '유니티 라이선스 좌석' in lines[0], True)
    eq('ⓨ 컴파일 갈래도 일러 준다', '어셈블리 컴파일' in lines[0], True)
    eq('ⓨ 죄 없는 워커를 안 가리킨다', '민 워커' in lines[0], False)
    # ⓩ T172 2회차 — **두 모드가 다 없으면** 컴파일 파손을 먼저 대라(실측 런 410~412)
    two = own_lines([], P_ANY, 'a' * 40, lock=both_live,
                    missing='editmode-results.xml,playmode-results.xml')
    eq('ⓩ 두 모드 갈래는 다른 말을 한다', '두 모드가 다 안 돌았다' in two[0], True)
    eq('ⓩ 컴파일 파손을 먼저', '컴파일 파손을 먼저 본다' in two[0], True)
    eq('ⓩ 스텁 함정을 일러 준다', '스텁' in two[0] and 'TestsPlay' in two[0], True)
    eq('ⓩ 실측 사례를 싣는다', '410~412' in two[0], True)
    eq('ⓩ 라이선스 갈래는 뒤에 남긴다', '라이선스 좌석' in two[0], True)
    eq('ⓩ 한 모드 갈래는 라이선스를 먼저', '라이선스 좌석»** 갈래를 먼저 본다' in lines[0], True)
    # 빠진 모드가 없으면 지금까지 하던 대로다(빨강 이름이 있으면 임자를 가린다)
    lines = own_lines(['FAIL Forge.Tests.PlayMode.CraftComparePopupTests.가 · Failed'], P_ANY, 'a' * 40,
                      lock=both_live, missing='')
    eq('ⓨ 평소에는 임자를 그대로 가린다', any('임자: **T156**' in l for l in lines), True)

    # ⓐⓐ T172 3회차 — 장부로 «처음 ↔ 간헐 ↔ 연달아» 를 가른다
    def L(*miss):
        return [{'run': 400 + i, 'missing_modes': m} for i, m in enumerate(miss)]
    PM = 'playmode-results.xml'
    one = ledger_note(L('', '', '', PM), PM)
    eq('ⓐⓐ 처음이면 «이번 하나뿐»', '이번 하나뿐' in one, True)
    eq('ⓐⓐ 처음이면 다음 런을 기다리라 한다', '다음 코드 push 의 런' in one, True)
    gap = ledger_note(L(PM, '', '', PM), PM)
    eq('ⓐⓐ 사이에 멀쩡한 런이 있으면 «간헐»', '간헐' in gap and '사이에 멀쩡한 런이 있다' in gap, True)
    eq('ⓐⓐ 간헐이면 주인을 아직 안 부른다', '헛걸음' in gap, True)
    run2 = ledger_note(L('', '', PM, PM), PM)
    eq('ⓐⓐ 연달아면 «서 있는 파손»', '서 있는 파손' in run2 and '연달아 빠지는 중' in run2, True)
    eq('ⓐⓐ 빠진 런 번호를 싣는다', '402' in run2 and '403' in run2, True)
    eq('ⓐⓐ 장부가 없으면 조용하다', ledger_note(None, PM), '')
    eq('ⓐⓐ 빠진 모드가 없으면 조용하다', ledger_note(L('', ''), ''), '')
    # 임자 줄에 실제로 얹힌다
    lines = own_lines([], P_ANY, 'a' * 40, lock=both_live, missing=PM, runs=L('', '', '', PM))
    eq('ⓐⓐ 임자 줄에 장부가 붙는다', '장부: 최근 런' in lines[0], True)

    # ⓐⓑ T338 — 잡별 판정
    payload = {'jobs': [
        {'name': 'dotnet 컴파일 · 순수 C# 테스트 · 문서·.meta 검사', 'conclusion': 'success', 'steps': [{'name': 'dotnet build', 'conclusion': 'success'}]},
        {'name': 'data/*.json ↔ wwwww main 동기화 검사', 'conclusion': 'success', 'steps': []},
        {'name': '유니티 시크릿 확인', 'conclusion': 'success', 'steps': []},
        {'name': 'Unity EditMode·PlayMode 테스트', 'conclusion': 'failure',
         'steps': [{'name': 'Run game-ci/unity-test-runner@v4', 'conclusion': 'failure'}, {'name': '결과 판정', 'conclusion': 'failure'}, {'name': 'Post', 'conclusion': 'skipped'}]},
    ]}
    jobs = parse_jobs(payload)
    eq('ⓐⓑ 잡 넷을 읽는다', [j['short'] for j in jobs], ['dotnet', 'datasync', 'gate', 'unity'])
    eq('ⓐⓑ 빨간 스텝만 센다(skipped 는 아니다)', jobs[3]['red_steps'], ['Run game-ci/unity-test-runner@v4', '결과 판정'])
    lines = job_lines(jobs, '28627b4' + 'f' * 33)
    eq('ⓐⓑ 유니티만 빨가면 «네 몫은 지났다»', any('유니티 잡만 빨갛다' in l and '지났다' in l for l in lines), True)
    eq('ⓐⓑ 요약 줄에 넷이 보인다', 'dotnet ✓ · datasync ✓ · gate ✓ · unity ✗' in lines[0], True)
    eq('ⓐⓑ merge-base 확인 명령을 댄다', any('merge-base --is-ancestor' in l and '28627b4' in l for l in lines), True)
    # ⓑ 내 잡(dotnet)이 빨간 런 435 꼴 — 빨간 스텝 이름을 댄다
    payload2 = {'jobs': [
        {'name': 'dotnet 컴파일 · 순수 C# 테스트 · 문서·.meta 검사', 'conclusion': 'failure',
         'steps': [{'name': 'dotnet build', 'conclusion': 'success'}, {'name': '글자 자 자기 검사 (T89)', 'conclusion': 'failure'}]},
        {'name': 'data/*.json ↔ wwwww main 동기화 검사', 'conclusion': 'success', 'steps': []},
        {'name': '유니티 시크릿 확인', 'conclusion': 'success', 'steps': []},
        {'name': 'Unity EditMode·PlayMode 테스트', 'conclusion': 'skipped', 'steps': []},
    ]}
    lines = job_lines(parse_jobs(payload2), 'b' * 40)
    eq('ⓑ 내 잡이 빨가면 그 잡 이름', any(l.strip().startswith('✗ dotnet 잡이 빨갛다') for l in lines), True)
    eq('ⓑ 빨간 스텝 이름을 댄다', any('«글자 자 자기 검사 (T89)»' in l for l in lines), True)
    eq('ⓑ 초록 스텝은 안 댄다', any('«dotnet build»' in l for l in lines), False)
    eq('ⓑ «네 몫은 지났다» 를 안 한다', any('지났다' in l for l in lines), False)
    eq('ⓑ 건너뛴 잡은 — 로', 'unity —' in lines[0], True)
    # ⓒ 둘 다 빨가면 내 잡 쪽을 먼저 말한다
    payload3 = {'jobs': [{'name': 'dotnet 컴파일', 'conclusion': 'failure', 'steps': [{'name': 'x', 'conclusion': 'failure'}]},
                         {'name': 'Unity EditMode·PlayMode 테스트', 'conclusion': 'failure', 'steps': []}]}
    lines = job_lines(parse_jobs(payload3), 'c' * 40)
    eq('ⓒ 둘 다 빨가면 내 잡 줄', any('dotnet 잡이 빨갛다' in l for l in lines), True)
    eq('ⓒ 둘 다 빨가면 «지났다» 없음', any('지났다' in l for l in lines), False)
    # ⓓ 전부 초록 · ⓔ API 없음(오프라인 추정) · ⓕ 빈 응답
    lines = job_lines(parse_jobs({'jobs': [{'name': 'dotnet 컴파일', 'conclusion': 'success', 'steps': []}]}), 'd' * 40)
    eq('ⓓ 전부 초록', any('잡 전부 초록' in l for l in lines), True)
    lines = job_lines(None, 'e' * 40)
    eq('ⓔ API 없으면 needs 추정 한 줄', len(lines) == 1 and 'needs: [dotnet, gate]' in lines[0] and 'eeeeeee' in lines[0], True)
    eq('ⓔ 유니티가 초록이면 조용하다', job_lines(None, 'e' * 40, unity_red=False), [])
    eq('ⓕ 빈 응답', parse_jobs({}), [])
    eq('ⓕ 응답이 None', parse_jobs(None), [])

    # ⓧⓧ T340 — ✅ 로 닫힌 행은 임자가 아니다(산 lock 이면 예외) · ✂⛔ 와 문구가 다르다 · 전부 닫혔으면 이력의 산 lock 이 임자
    P_DONE = ('| T44 | 성능 자 | ✅ 완료 | 워커 A | `Assets/Tests/PlayMode/PerfBudgetTests.cs` | x |\n'
              '| T50 | 성능 둘 | ✅ 완료 | 워커 B | `Assets/Tests/PlayMode/PerfBudgetTests.cs` | x |\n'
              '| T64 | 성능 셋 | ✅ 완료 | 워커 C | `Assets/Tests/PlayMode/PerfBudgetTests.cs` | x |')
    none_lock = lambda tid, now=None: (False, None)
    t330_live = lambda tid, now=None: (tid == 'T330', 21.0 if tid == 'T330' else None)
    live, dead, dstat = scope_owners_split('PerfBudgetTests', P_DONE)
    keep, done = split_done(live, dstat, none_lock)
    eq('ⓧⓧ ✅ 만 있으면 후보 0', keep, [])
    eq('ⓧⓧ 닫힌 셋을 뺀다', done, ['T44', 'T50', 'T64'])
    keep2, done2 = split_done(['T44', 'T50'], {'T44': '✅ 완료', 'T50': '✅ 완료'}, lambda t, now=None: (t == 'T44', 5.0))
    eq('ⓧⓧ 산 lock 인 ✅ 는 종전대로 후보', (keep2, done2), (['T44'], ['T50']))
    keep3, done3 = split_done(['T161'], {'T161': '✂ 접음'}, none_lock)
    eq('ⓧⓧ ✂ 는 여기 몫이 아니다(T162 가 뺀다)', (keep3, done3), (['T161'], []))
    hist_330 = lambda name, log=None: ['T330']
    lines = own_lines(['FAIL Forge.Tests.PlayMode.PerfBudgetTests.부하_장면 · Failed'], P_DONE, 'a' * 40,
                      lock=t330_live, hist=hist_330)
    eq('ⓧⓧ «네 일이다» 로 단정하지 않는다', any('네 일이다' in l for l in lines), False)
    eq('ⓧⓧ 이력의 산 lock T330 을 임자로 댄다', any('임자: **T330**' in l for l in lines), True)
    eq('ⓧⓧ 닫힌 행이었다고 말한다(✂⛔ 문구가 아니다)', any('✅ 로 닫힌 행' in l and 'T44(✅ 완료)' in l for l in lines), True)
    eq('ⓧⓧ 죽은 행 문구는 안 쓴다', any('**죽은 행**' in l for l in lines), False)
    eq('ⓧⓧ 새로 깨진 것이라고 말한다', any('새로 깨진 것' in l for l in lines), True)
    # 이력에도 산 lock 이 없으면 «못 가렸다» 로 떨어진다(종전 ⓓ) — 여전히 «네 일이다» 가 아니라 «임자 없으면» 조건부
    lines = own_lines(['FAIL Forge.Tests.PlayMode.PerfBudgetTests.부하_장면 · Failed'], P_DONE, 'a' * 40,
                      lock=none_lock, hist=lambda name, log=None: [])
    eq('ⓧⓧ 이력도 없으면 «못 가렸다»', any('못 가렸다' in l for l in lines), True)
    eq('ⓧⓧ 그때도 닫힌 행 꼬리가 붙는다', any('✅ 로 닫힌 행' in l for l in lines), True)
    # 열린 후보가 섞여 있으면 종전 갈래(그 후보가 임자) · ✅ 는 꼬리로
    P_MIX2 = P_DONE + '\n| T330 | 윤곽선 넷째 항 | 🔄 진행 | 워커 G | `Assets/Tests/PlayMode/PerfBudgetTests.cs` | x |'
    lines = own_lines(['FAIL Forge.Tests.PlayMode.PerfBudgetTests.부하_장면 · Failed'], P_MIX2, 'a' * 40,
                      lock=t330_live, hist=hist_330)
    eq('ⓧⓧ 열린 후보가 섞이면 그것이 임자', any('임자: **T330**' in l for l in lines), True)
    eq('ⓧⓧ 그때도 ✅ 셋은 후보에 안 든다', any('T44(lock 없다)' in l for l in lines), False)
    # 이력 후보가 ✅ 절뿐이면 «lock 없다» 가 아니라 «닫힌 행» 으로 말하고 «런 사이» 를 가리킨다(런 479 실물 꼴 — T330 은 그 파일을 안 고쳤다)
    lines = own_lines(['FAIL Forge.Tests.PlayMode.PerfBudgetTests.부하_장면 · Failed'], P_DONE, 'a' * 40,
                      lock=none_lock, hist=lambda name, log=None: ['T64', 'T50', 'T44'])
    eq('ⓧⓧ 이력 후보의 ✅ 를 닫힌 행으로', any('T64(✅ 닫힌 행)' in l for l in lines), True)
    eq('ⓧⓧ 이력 후보에 «lock 없다» 를 안 쓴다', any('T64(lock 없다)' in l for l in lines), False)
    eq('ⓧⓧ 전부 닫혔으면 «런 사이» 를 가리킨다', any('«런 사이» 칸의 산 lock 커밋' in l for l in lines), True)
    eq('ⓧⓧ row_status 가 상태를 쥔다', row_status(P_DONE).get('T50'), '✅ 완료')

    # ⓧⓧⓧ T344 — 모드 로그(T151)를 자가 직접 읽는다
    LOG_492 = ('# T151 — 런 #492 (abc) · playmode 의 결과 XML 이 없다 · 에디터 로그 unity-test-results/playmode.log\n'
               '# 줄 수 1234 · 마지막 수정 2026-09-14T20:10:00Z\n## 원인 문구 줄(최대 60)\n'
               "812:Assets/Tests/PlayMode/SceneGradeTests.cs(5,29): error CS0234: The type or namespace name 'Universal' does not exist in the namespace 'UnityEngine.Rendering'\n"
               '900:Scripts have compiler errors.\n## 머리 60줄\n…')
    P_341 = '| T341 | 장면 채점 | 🔄 진행 | 워커 S | `Assets/Tests/PlayMode/SceneGradeTests.cs` · `Assets/Scripts/Game/Render/SceneGrade.cs` | x |'
    live_341 = lambda tid, now=None: (tid == 'T341', 12.0 if tid == 'T341' else None)
    lines = mode_log_note(LOG_492, 'playmode-results.xml', P_341, meta_run=492, lock=live_341)
    eq('ⓧⓧⓧ 첫 error CS 줄을 싣는다', any('error CS0234' in l for l in lines), True)
    eq('ⓧⓧⓧ 그 줄의 파일 임자 T341', any('`Assets/Tests/PlayMode/SceneGradeTests.cs` 의 임자 **T341**' in l for l in lines), True)
    eq('ⓧⓧⓧ 산 lock 이면 그의 몫', any('그의 몫이다' in l for l in lines), True)
    eq('ⓧⓧⓧ 줄 번호 접두를 뗀다', any('812:' in l for l in lines), False)
    lines = mode_log_note(LOG_492, 'playmode-results.xml', P_341, meta_run=492, lock=lambda t, now=None: (False, None))
    eq('ⓧⓧⓧ lock 없으면 네 일이다', any('이것이 네 일이다' in l for l in lines), True)
    lines = mode_log_note(LOG_492, 'playmode-results.xml', '', meta_run=492, lock=live_341)
    eq('ⓧⓧⓧ 범위에 없으면 «네가 고친다» (보고함이 아니다)', any('네가 고친다' in l and '보고함이 아니다' in l for l in lines), True)
    lines = mode_log_note(LOG_492, 'playmode-results.xml', P_341, meta_run=494, lock=live_341)
    eq('ⓧⓧⓧ 다른 런의 로그면 임자를 안 가린다', len(lines) == 1 and '이 런(#494)의 로그가 아니다' in lines[0], True)
    LOG_LIC = '# T151 — 런 #348 (x) · playmode 의 결과 XML 이 없다 · 에디터 로그 y\n## 원인 문구 줄\n33:[Licensing::Client] Error: no available seats\n'
    lines = mode_log_note(LOG_LIC, 'playmode-results.xml', P_341, meta_run=348, lock=live_341)
    eq('ⓧⓧⓧ 라이선스 문구면 그 갈래', any('«유니티 라이선스 좌석» 갈래' in l and 'no available seats' in l for l in lines), True)
    eq('ⓧⓧⓧ 라이선스면 파일 임자를 안 찾는다', any('의 임자' in l for l in lines), False)
    LOG_NONE = '# T151 — 런 #500 (x) · editmode 의 결과 XML 이 없다 · 에디터 로그 y\n## 원인 문구 줄\n## 머리 60줄\nhello\n'
    lines = mode_log_note(LOG_NONE, 'editmode-results.xml', P_341, meta_run=500, lock=live_341)
    eq('ⓧⓧⓧ 원인 줄이 없으면 직접 읽으라 하고 보고함은 그 뒤', any('원인 문구 줄이 없다' in l and 'editmode-log.txt' in l for l in lines), True)
    eq('ⓧⓧⓧ 로그가 없으면 조용', mode_log_note(None, 'playmode-results.xml', P_341, meta_run=1), [])
    eq('ⓧⓧⓧ 이름 변환', mode_log_name('editmode-results.xml'), 'editmode-log.txt')
    eq('ⓧⓧⓧ 엉뚱한 이름은 빈 문자열', mode_log_name('foo.xml'), '')
    # own_lines 의 «모드 빠짐» 갈래에 실제로 얹힌다 · 옛 «잡 로그를 열고» 문구는 사라졌다
    lines = own_lines([], P_341, 'a' * 40, lock=live_341, missing='playmode-results.xml',
                      runs=[{'run': 490, 'missing_modes': 'playmode-results.xml'}, {'run': 492, 'missing_modes': 'playmode-results.xml'}],
                      mode_logs={'playmode-results.xml': LOG_492}, meta_run=492)
    eq('ⓧⓧⓧ 모드 빠짐 갈래에 모드 로그 줄이 붙는다', any('모드 로그(' in l for l in lines), True)
    eq('ⓧⓧⓧ 서 있는 파손이면 screens 로그를 대라', any('git show origin/screens:<모드>-log.txt' in l for l in lines), True)
    eq('ⓧⓧⓧ «잡 로그를 열고» 는 없다', any('잡 로그를 열고' in l for l in lines), False)
    eq('ⓧⓧⓧ 머리 문구도 screens 로그를 댄다', any('잡 로그는 컨테이너에서 못 연다' in l for l in lines), True)

    # ⓧⓧⓧⓧ T348 — 죽은 lock 이라도 그 번호로 90분 안 커밋이 있으면 «뺏을 수 있다» 라 하지 않는다
    eq('ⓧⓧⓧⓧ 죽은 lock + 36분 전 커밋 → 이어 하는 중', '다른 SID 가 이어 하는 중일 수 있다' in _lock_word(False, 103, recent=36, tid='T342'), True)
    eq('ⓧⓧⓧⓧ 그 줄은 task_state 를 대라 한다', 'task_state.py T342' in _lock_word(False, 103, recent=36, tid='T342'), True)
    eq('ⓧⓧⓧⓧ 그 줄에 «뺏을 수 있다» 는 없다', '뺏을 수 있다' in _lock_word(False, 103, recent=36, tid='T342'), False)
    eq('ⓧⓧⓧⓧ 커밋도 90분 넘으면 종전 문구', _lock_word(False, 103, recent=200, tid='T342'), 'lock 103분 전 — 90분 규약으로 **죽었다**(뺏을 수 있다)')
    eq('ⓧⓧⓧⓧ 커밋 나이 None(얕은 클론)이면 종전 문구', _lock_word(False, 103, recent=None), 'lock 103분 전 — 90분 규약으로 **죽었다**(뺏을 수 있다)')
    eq('ⓧⓧⓧⓧ 산 lock 은 그대로', _lock_word(True, 12, recent=3), 'lock 12분 전')
    eq('ⓧⓧⓧⓧ lock 없음은 그대로', _lock_word(False, None, recent=3), 'lock 없다')
    ol = own_line('T342', False, 103, recent=36)
    eq('ⓧⓧⓧⓧ own_line 도 «네 일이다» 를 안 한다', '네 일이다' in ol, False)
    eq('ⓧⓧⓧⓧ own_line 이 커밋 나이를 말한다', '36분 전 커밋' in ol and 'task_state.py T342' in ol, True)
    eq('ⓧⓧⓧⓧ own_line 종전(커밋 200분)', '네 일이다' in own_line('T342', False, 103, recent=200), True)
    # own_lines 에 실제로 얹힌다 — 런 501 꼴: UiFilter.cs 의 임자 T342 · lock 103분 · 다른 SID 커밋 36분 전
    P_342 = '| T342 | 필터 | 🔄 진행 | 워커 K | `Assets/Scripts/Game/Ui/UiFilter.cs` · `Assets/Tests/PlayMode/UiFilterTests.cs` | x |'
    dead_342 = lambda tid, now=None: (False, 103) if tid == 'T342' else (False, None)
    age_36 = lambda tid, now=None: 36 if tid == 'T342' else None
    lines = own_lines(['FAIL Forge.Tests.PlayMode.UiFilterTests.가 · Failed'], P_342, 'a' * 40, lock=dead_342, commit_age=age_36)
    eq('ⓧⓧⓧⓧ 런 501 꼴: 뺏으라 하지 않는다', any('뺏을 수 있다' in l or '네 일이다' in l for l in lines), False)
    eq('ⓧⓧⓧⓧ 런 501 꼴: 이어 하는 중 + task_state', any('이어 하는 중' in l and 'task_state.py T342' in l for l in lines), True)
    lines = own_lines(['FAIL Forge.Tests.PlayMode.UiFilterTests.가 · Failed'], P_342, 'a' * 40, lock=dead_342, commit_age=lambda t, now=None: 200)
    eq('ⓧⓧⓧⓧ 커밋도 낡았으면 종전대로 네 일', any('네 일이다' in l for l in lines), True)
    # ⓟ 경로 갈래(콘솔 에러가 남의 파일을 댄 경우)도 같은 잣대
    lines = own_lines(['FAIL Forge.Tests.PlayMode.SomeTests.가 · Failed'], P_342, 'a' * 40, lock=dead_342, commit_age=age_36,
                      err={'SomeTests': ['Assets/Scripts/Game/Ui/UiFilter.cs']}, hist=lambda name, log=None: [])
    eq('ⓧⓧⓧⓧ 경로 갈래도 뺏지 말라 한다', any('UiFilter.cs' in l and '뺏지 말고' in l for l in lines), True)
    eq('ⓧⓧⓧⓧ 경로 갈래에 «이것이 네 일이다» 없음', any('이것이 네 일이다' in l for l in lines), False)
    # between_lines 의 lock 낱말도 같은 잣대
    bl = between_lines([('abc1234', 'T342 2회차: 무엇', ['Assets/Scripts/Game/Ui/UiFilter.cs']), ], set(), ('g' * 40, 400),
                       lock=dead_342, commit_age=age_36)
    eq('ⓧⓧⓧⓧ «런 사이» 칸의 lock 낱말도 이어 하는 중', any('이어 하는 중' in l for l in bl), True)

    # ⓥ T356 — «유니티가 안 돈 뒤 런» 을 «막힌 것» 과 «진짜 문서 push» 로 가른다
    def _jobs(dotnet, unity, step=None):
        return [{'name': 'dotnet 컴파일', 'short': 'dotnet', 'conclusion': dotnet, 'red_steps': [step] if step else []},
                {'name': 'Unity EditMode·PlayMode 테스트', 'short': 'unity', 'conclusion': unity, 'red_steps': []}]

    blk, docs, unk = classify_after([
        {'run_number': 540, 'sha': 'd' * 40, 'jobs': _jobs('failure', 'skipped', 'TMP richText')},
        {'run_number': 541, 'sha': 'e' * 40, 'jobs': _jobs('failure', 'skipped', 'TMP richText')},
        {'run_number': 546, 'sha': 'f' * 40, 'jobs': _jobs('success', 'skipped')},
        {'run_number': 547, 'sha': '7' * 40, 'jobs': _jobs('success', 'failure')},
        {'run_number': 548, 'sha': '8' * 40, 'jobs': None},
    ])
    eq('ⓥ 막힌 런 수', len(blk), 2)
    eq('ⓥ 막힌 런 번호', [b['run'] for b in blk], [540, 541])
    eq('ⓥ 빨간 스텝 이름을 댄다', 'TMP richText' in blk[0]['why'], True)
    eq('ⓥ 진짜 문서 push 는 따로', [d['run'] for d in docs], [546])
    eq('ⓥ 유니티가 돈 런은 안 센다', 547 in [b['run'] for b in blk] + [d['run'] for d in docs], False)
    eq('ⓥ 잡을 못 읽은 런', [u['run'] for u in unk], [548])

    _lines356 = blocked_lines(blk, docs, unk, True)
    eq('ⓥ 맨 윗줄이 ⛔', _lines356[0].startswith('⛔'), True)
    eq('ⓥ needs 를 설명한다', any('needs' in ln for ln in _lines356), True)
    eq('ⓥ 막힌 게 없으면 한 줄도 안 낸다', blocked_lines([], docs, unk, True), [])

    # ⓥ 시제 — 지나간 막힘은 «잡지 마라» 로 말하고 ⛔ 를 안 쓴다
    _past = blocked_lines(blk, docs, unk, False)
    eq('ⓥ 지나간 막힘은 ⛔ 가 아니다', _past[0].startswith('⛔'), False)
    eq('ⓥ 지나간 막힘은 «잡지 마라»', any('잡지 마라' in ln for ln in _past), True)
    eq('ⓥ 지나간 막힘엔 «네 일이다» 가 없다', any('자기 lock 이든' in ln for ln in _past), False)

    _runs356 = [
        {'run_number': 540, 'jobs': _jobs('failure', 'skipped', 'x')},
        {'run_number': 546, 'jobs': _jobs('success', 'skipped')},
    ]
    eq('ⓥ 마지막 런이 안 막혔으면 still=False', still_blocked(_runs356, blk), False)
    eq('ⓥ 마지막 런이 막혔으면 still=True',
       still_blocked([{'run_number': 546, 'jobs': _jobs('success', 'skipped')},
                      {'run_number': 541, 'jobs': _jobs('failure', 'skipped', 'x')}], blk), True)
    eq('ⓥ 잡을 못 읽은 마지막 런은 건너뛴다',
       still_blocked(_runs356 + [{'run_number': 999, 'jobs': None}], blk), False)

    # ⓦ T356 — 막힌 런이 있으면 판정 줄 **위**에 서고, 마지막 유니티 런이 초록이어도 rc 1
    rc, out = judge({'sha': 'a' * 40, 'run': 100, 'tests': 'success', 'missing_modes': ''}, True, 0, blocked=_lines356)
    eq('ⓦ 초록인데도 rc 1', rc, 1)
    eq('ⓦ ⛔ 가 맨 위', out[0].startswith('⛔'), True)
    eq('ⓦ 초록 줄도 남는다', any(ln.startswith('✓') for ln in out), True)

    rc, out = judge({'sha': 'a' * 40, 'run': 100, 'tests': 'success', 'missing_modes': ''}, True, 0,
                    blocked=_past, blocks_now=False)
    eq('ⓦ 지나간 막힘은 rc 를 안 올린다', rc, 0)

    rc, out = judge({'sha': 'b' * 40, 'run': 101, 'tests': 'failure', 'missing_modes': ''}, True, 2, blocked=_lines356)
    eq('ⓦ 빨강 + 막힘 rc', rc, 1)
    eq('ⓦ 빨강이어도 ⛔ 가 먼저', out[0].startswith('⛔'), True)
    eq('ⓦ 막혔으면 «문서 push 라» 로 단정하지 않는다', any('문서 push 라 유니티 잡이 **skipped** 였을 뿐이다' in ln for ln in out), False)

    rc, out = judge({'sha': 'c' * 40, 'run': 102, 'tests': 'failure', 'missing_modes': ''}, True, 2)
    eq('ⓦ 막힌 것을 못 봤으면 옛 설명을 «일 수 있다» 로 남긴다',
       any('였을 뿐일 수 있다' in ln for ln in out), True)

    # ⓐⓒ T373 — 장부보다 **새로운, 유니티가 실제로 판정을 낸 런**을 손에 쥐고도 버리던 자리.
    #      실측(2026-09-15 08:0x): 장부 = 런 620 `failure` · 같은 순간 런 623 이 실제로 돌아 `success`.
    eq('ⓐⓒ success·failure 만 판정이다', [unity_verdict([{'conclusion': c} for c in cs]) for cs in
                                    (['success'], ['failure'], ['skipped'], ['neutral'], ['cancelled'], ['timed_out'], [])],
       ['success', 'failure', None, None, None, None, None])
    eq('ⓐⓒ 한 런에 빨강이 하나라도 있으면 빨강',
       unity_verdict([{'conclusion': 'success'}, {'conclusion': 'failure'}]), 'failure')

    _after373 = [
        {'run_number': 621, 'sha': '1' * 40, 'jobs': _jobs('success', 'cancelled')},
        {'run_number': 622, 'sha': '2' * 40, 'jobs': _jobs('success', 'skipped')},
        {'run_number': 623, 'sha': '3' * 40, 'jobs': _jobs('success', 'success')},
        {'run_number': 624, 'sha': '4' * 40, 'jobs': _jobs('success', 'skipped')},
    ]
    _nv = newer_verdict(_after373)
    eq('ⓐⓒ 뒤 런에서 판정을 낸 마지막 런을 집는다', (_nv['run'], _nv['verdict']), (623, 'success'))
    eq('ⓐⓒ 전체 sha 도 같이 쥔다(커밋 셈을 다시 재려고)', _nv['full'], '3' * 40)
    eq('ⓐⓒ 판정 낸 런이 없으면 None', newer_verdict(_after373[:2] + _after373[3:]), None)
    eq('ⓐⓒ 잡을 못 읽은 런은 건너뛴다', newer_verdict([{'run_number': 9, 'sha': 'z' * 40, 'jobs': None}]), None)
    eq('ⓐⓒ 빈 목록도 조용하다', newer_verdict([]), None)

    # `cancelled`·`timed_out` 은 «돌다 만 것» — 막힘도 문서 push 도 아니다(어느 칸에도 안 들어간다)
    _b373, _d373, _u373 = classify_after(_after373)
    eq('ⓐⓒ cancelled 는 막힘이 아니다', 621 in [b['run'] for b in _b373], False)
    eq('ⓐⓒ cancelled 는 문서 push 도 아니다', 621 in [d['run'] for d in _d373], False)
    eq('ⓐⓒ 진짜 문서 push 둘은 그대로', [d['run'] for d in _d373], [622, 624])

    # 낡은 장부 + 뒤 런 초록 → «첫 일» 로 안 내준다(rc 0)
    rc, out = judge({'sha': 'd' * 40, 'run': 620, 'tests': 'failure', 'missing_modes': ''}, True, 0,
                    fails=['FAIL A.B.LineHeightTests.무엇 · Failed'], newer=_nv)
    eq('ⓐⓒ 낡은 빨강은 rc 0', rc, 0)
    eq('ⓐⓒ 맨 위에 «장부가 낡았다»', out[0].startswith('⚑ **장부가 낡았다**'), True)
    eq('ⓐⓒ «이미 닫혔다» 를 말한다', any('이미 닫혔다' in ln for ln in out), True)
    eq('ⓐⓒ «⚑ 이것이 이번 회차의 첫 일이다» 를 안 찍는다',
       any(ln.strip().startswith('⚑ 이것이 이번 회차의 첫 일이다') for ln in out), False)
    eq('ⓐⓒ ✗ 줄 자체가 없다', any(ln.startswith('✗') for ln in out), False)
    eq('ⓐⓒ 새 런 번호로 머리를 고쳐 적는다', any('#623' in ln and '장부 #620 보다 새롭다' in ln for ln in out), True)

    # 낡은 장부 + 뒤 런 빨강 → 장부가 초록이어도 빨강이다
    _nvF = {'run': 623, 'sha': '3' * 7, 'full': '3' * 40, 'verdict': 'failure'}
    rc, out = judge({'sha': 'd' * 40, 'run': 620, 'tests': 'success', 'missing_modes': ''}, True, 0, newer=_nvF)
    eq('ⓐⓒ 장부 초록 + 뒤 런 빨강 = rc 1', rc, 1)
    eq('ⓐⓒ 그 빨강이 런 623 것이라고 말한다', any('#623' in ln and '빨갛다' in ln for ln in out), True)

    # 장부가 더 새롭거나 같으면 한 줄도 안 뜬다 — 종전 출력 그대로
    eq('ⓐⓒ 장부가 더 새로우면 조용하다', stale_lines(700, _nv, True), [])
    eq('ⓐⓒ 같은 런이면 조용하다', stale_lines(623, _nv, True), [])
    eq('ⓐⓒ 번호를 못 읽으면 조용하다', stale_lines('?', _nv, True), [])
    rc, out = judge({'sha': 'd' * 40, 'run': 700, 'tests': 'failure', 'missing_modes': ''}, True, 0,
                    fails=['FAIL A.B.C.무엇 · Failed'], newer=_nv)
    eq('ⓐⓒ 장부가 더 새로우면 종전대로 «첫 일»',
       any(ln.strip().startswith('⚑ 이것이 이번 회차의 첫 일이다') for ln in out), True)
    eq('ⓐⓒ 장부가 더 새로우면 rc 1 그대로', rc, 1)
    rc0, out0 = judge({'sha': 'd' * 40, 'run': 620, 'tests': 'failure', 'missing_modes': ''}, True, 0,
                      fails=['FAIL A.B.C.무엇 · Failed'])
    eq('ⓐⓒ newer 를 안 주면 옛 출력 그대로', (rc0, out0[0].startswith('✗')), (1, True))

    # ⓨ T363 — «못 가렸다» 줄이 제 «런 사이» 안내를 «네가 고친다» 로 덮어쓰던 것.
    WC = [('a' * 7, 'T331 5회차: 그림자를 공용으로', ['Assets/Scripts/Game/Ui/EquipSwapFx.cs']),
          ('b' * 7, 'T999 문서만', ['docs/PROGRESS.md']),
          ('c' * 7, '제목이 T 로 안 시작', ['x.cs']),
          ('d' * 7, 'T331 4회차: 같은 번호 두 번', ['y.cs'])]
    only331 = lambda t, n=None: (t == 'T331', 61 if t == 'T331' else None)
    eq('ⓨ 창에서 산 lock 번호만 뽑고 겹치면 하나다', window_owners(WC, lock=only331), [('T331', 61)])
    eq('ⓨ 산 lock 이 없으면 빈 목록', window_owners(WC, lock=lambda t, n=None: (False, 200)), [])
    eq('ⓨ 창이 비면 빈 목록', window_owners([], lock=only331), [])
    P_NO = P + '| T118 | 옛 작업 | ✅ | s0 | `Assets/Tests/PlayMode/EquipSwapTests.cs` | — |\n'
    plain = own_lines(['FAIL A.B.EquipSwapTests.무엇 · Failed'], P_NO, 'e' * 40, lock=dead, hist=lambda n: [])
    eq('ⓨ 창이 비면 종전 문구 그대로', any('**네가 고친다**' in l for l in plain), True)
    withw = own_lines(['FAIL A.B.EquipSwapTests.무엇 · Failed'], P_NO, 'e' * 40, lock=dead, hist=lambda n: [],
                      window=[('T331', 61)])
    eq('ⓨ 창에 산 lock 이 있으면 «네가 고친다» 를 안 쓴다', any('**네가 고친다**' in l for l in withw), False)
    eq('ⓨ 그 번호를 줄 안에서 바로 댄다', any('T331' in l and '그 커밋 임자의 몫' in l for l in withw), True)

    # ⓨ T362 — 자 이름 ↔ 파일 이름 짝
    eq('ⓨ 줄기는 꼬리를 뗀다', stem_of('EquipSwapTests'), 'EquipSwap')
    eq('ⓨ 꼬리가 없으면 그대로', stem_of('Foo'), 'Foo')

    prog362 = ('| T331 | 그림자 | 🔄 진행 | sess-x | `Assets/Scripts/Game/Ui/EquipSwapFx.cs` · `Ui/Popups.cs` | 비고 |\n'
               '| T400 | 남 | 🔄 진행 | sess-y | `Assets/Scripts/Game/Ui/OtherThing.cs` | 비고 |\n')
    live331 = lambda t, n=None: (t == 'T331', 15)
    got = stem_owners('EquipSwapTests', prog362, lock=live331)
    eq('ⓨ 같은 줄기 + 산 lock 을 집어낸다', got, [('T331', 'EquipSwapFx.cs')])
    eq('ⓨ 죽은 lock 은 안 센다', stem_owners('EquipSwapTests', prog362, lock=lambda t, n=None: (False, 200)), [])
    eq('ⓨ 줄기가 흔한 말이면 안 쓴다', stem_owners('UiTests', prog362, lock=live331), [])
    eq('ⓨ 줄기가 짧으면 안 쓴다', stem_owners('FxTests', prog362, lock=live331), [])
    eq('ⓨ 설명 칸에 인용된 이름은 안 센다(«범위» 칸만 본다)',
       stem_owners('EquipSwapTests',
                   '| T362 | 등재문이 `EquipSwapFx.cs` 를 인용한다 | 🔄 | s | `tools/check_unity_green.py` | 비고 |\n',
                   lock=live331 if False else (lambda t, n=None: (True, 5))), [])
    eq('ⓨ 제 자 파일은 짝으로 안 센다',
       stem_owners('EquipSwapTests',
                   '| T331 | x | 🔄 | s | `Assets/Tests/PlayMode/EquipSwapTests.cs` | 비고 |\n',
                   lock=live331), [])

    # 사다리에서 실제로 그 줄이 나오는가 — 범위에도 이력에도 없을 때
    lines362 = own_lines(['FAIL Forge.Tests.PlayMode.EquipSwapTests.어쩌고 · Failed'], prog362, 'f' * 40,
                         hist=lambda n: [], lock=live331)
    eq('ⓨ 사다리가 T331 을 댄다', any('**T331**' in l for l in lines362), True)
    eq('ⓨ 근거가 되는 파일 이름을 댄다', any('EquipSwapFx.cs' in l for l in lines362), True)
    eq('ⓨ «못 가렸다» 로 안 끝난다', any('못 가렸다' in l for l in lines362), False)
    eq('ⓨ «범위 열에 적어라» 를 남긴다', any('범위» 열에 `EquipSwapTests.cs` 도 적어라' in l for l in lines362), True)

    # 줄기가 흔하면 옛 갈래(못 가렸다)로 그대로 간다
    lines_common = own_lines(['FAIL Forge.Tests.PlayMode.UiTests.어쩌고 · Failed'], prog362, 'f' * 40,
                             hist=lambda n: [], lock=live331)
    eq('ⓨ 흔한 줄기는 옛 갈래 그대로', any('못 가렸다' in l for l in lines_common), True)

    # ⓩ T386 — 장부의 «건너뜀» 칸: 남의 lock 뒤라 접어 둔 자리는 **빨강이 아니라 남은 일**이고,
    #        그 번호가 닫히면 «이제 켜라» 로 운다(접어 두기가 영원히 숨는 길이 되는 것을 막는 유일한 장치).
    txt386 = ('#   playmode-results.xml: 전부 342 · 초록 340 · 빨강 0 · 건너뜀 2\n'
              '#   건너뜀 Forge.Tests.PlayMode.BrLinesTests.승천_효과_글줄은_정본대로_세_줄이다'
              ' :: KNOWN T333·T354 lock 뒤 — 하한 탓에 4줄\n'
              '#   건너뜀 Forge.Tests.PlayMode.EdgeOutlineTests.가장자리 :: 그래픽 장치가 없다\n'
              '── Forge.Tests.PlayMode.AgePatternTests.무늬\nPASS Forge.Tests.PlayMode.AgePatternTests.무늬\n')
    sk386 = skips(txt386)
    eq('ⓩ 건너뜀 두 줄을 읽는다', len(sk386), 2)
    eq('ⓩ 이름과 사유를 가른다', sk386[0][1].startswith('KNOWN T333'), True)
    eq('ⓩ FAIL·PASS 줄은 건너뜀이 아니다', [n for n, _ in sk386 if 'AgePattern' in n], [])

    open333 = {'T333': '⬜ 대기', 'T354': '🔄 진행'}
    ln386, stale386 = skip_note(sk386, open333)
    eq('ⓩ 열린 번호면 켜라고 안 한다', stale386, [])
    eq('ⓩ 남은 일 수는 KNOWN 만 센다', any('**남은 일 1**' in l for l in ln386), True)
    eq('ⓩ 번호를 둘 다 댄다', any('T333 · T354' in l for l in ln386), True)
    eq('ⓩ 환경 사유는 따로 적는다', any('환경 사유' in l for l in ln386), True)
    eq('ⓩ 이름은 자.칸 두 토막', any('BrLinesTests.승천_효과_글줄은_정본대로_세_줄이다' in l for l in ln386), True)

    green386 = {'sha': 'c' * 40, 'run': 700, 'tests': 'success', 'missing_modes': ''}
    rc386, out386 = judge(green386, True, 0, skipped=(ln386, stale386))
    eq('ⓩ 접어 둔 자리는 초록을 안 깬다', rc386, 0)
    eq('ⓩ 초록 줄 옆에 남은 일이 선다', any('남은 일 1' in l for l in out386), True)

    # T412 — 댄 번호 둘 중 하나만 닫혔으면 **안 운다**(접어 둘 까닭이 남았다) · «아직 열린 번호» 를 적어 준다
    half333 = {'T333': '✅ 완료', 'T354': '🔄 진행'}
    ln_half, half_ids = skip_note(sk386, half333)
    eq('ⓩ T412 둘 중 하나만 닫히면 켜라고 안 한다', half_ids, [])
    eq('ⓩ T412 아직 열린 번호를 적어 준다', any('T354 이(가) 아직 열려 있다' in l and 'T333 는 닫혔지만' in l for l in ln_half), True)
    eq('ⓩ T412 반쪽은 «이제 켜라» 가 아니다', any('이제 켜라' in l and l.strip().startswith('⚑') for l in ln_half), False)
    rc_half, _ = judge(green386, True, 0, skipped=(ln_half, half_ids))
    eq('ⓩ T412 반쪽 닫힘은 rc 0', rc_half, 0)
    # 댄 번호가 **전부** 닫혔을 때만 운다
    closed333 = {'T333': '✅ 완료', 'T354': '⛔ 접음'}
    ln_stale, stale_ids = skip_note(sk386, closed333)
    eq('ⓩ 전부 닫힌 번호를 집는다', stale_ids, ['T333', 'T354'])
    eq('ⓩ «이제 켜라» 를 적는다', any('이제 켜라' in l for l in ln_stale), True)
    rc_stale, out_stale = judge(green386, True, 0, skipped=(ln_stale, stale_ids))
    eq('ⓩ 전부 닫힌 번호를 대고 접혀 있으면 rc 1', rc_stale, 1)
    eq('ⓩ 그래도 판정 줄 자체는 초록', any(l.startswith('✓ check_unity_green') for l in out_stale), True)

    # 접힌 것이 환경 사유뿐이면 한 줄도 «남은 일» 로 안 센다
    env_only = skip_note([('Forge.Tests.PlayMode.EdgeOutlineTests.가', '그래픽 장치가 없다')], open333)
    # 환경 사유 줄 자체가 «남은 일이 아니다» 라고 적으므로 머리표(`**남은 일 N**`)로 가른다
    eq('ⓩ 환경 사유만이면 남은 일 0', any('**남은 일' in l for l in env_only[0]), False)
    eq('ⓩ 환경 사유만이면 rc 0', judge(green386, True, 0, skipped=env_only)[0], 0)
    eq('ⓩ 건너뜀이 없으면 종전 출력 그대로', judge(green386, True, 0, skipped=([], []))[1],
       judge(green386, True, 0)[1])

    # ── T416 — «건너뜀» 이 빨강 줄로 새지 않는다(쓰는 쪽 `RedLog.cs` 는 이제 `SKIP `, 읽는 쪽은 옛 장부의 꼬리를 뺀다)
    import tempfile as _tf
    body416 = ('# playmode-results.xml: 전부 432 · 초록 427 · 빨강 3 · 건너뜀 2\n'
               '#   건너뜀 Forge.Tests.PlayMode.CoinSellCurveTests.판매_코인_시간축 :: 환경 — 촬영 간격이 넓다\n'
               'FAIL Forge.Tests.PlayMode.CardHatchTests.가 · Failed\n'
               'FAIL Forge.Tests.PlayMode.CoinSellCurveTests.판매_코인_시간축 · Skipped\n'
               'FAIL Forge.Tests.PlayMode.DropShadowTests.나 · Failed\n'
               'SKIP Forge.Tests.PlayMode.EdgeOutlineTests.다 · Skipped\n'
               'FAIL Forge.Tests.PlayMode.DropShadowTests.라 · Failed\n'
               'FAIL Forge.Tests.PlayMode.DropShadowTests.마 · Inconclusive\n')
    kept = [ln.strip() for ln in body416.split('\n')
            if (ln.startswith('FAIL ') or ln.startswith('RED ')) and not SKIPPED_TAIL.search(ln)]
    eq('T416 ⓐ 본문의 «FAIL … · Skipped» 는 빨강으로 안 센다(머리의 «빨강 3» 과 같다)', len(kept), 3)
    eq('T416 ⓑ «· Inconclusive» 꼬리도 뺀다', any('Inconclusive' in l for l in kept), False)
    eq('T416 ⓒ 새 꼴 «SKIP » 줄은 애초에 안 걸린다', any(l.startswith('SKIP') for l in kept), False)
    eq('T416 ⓓ 진짜 빨강은 그대로 남는다', sorted(l.split('.')[-1].split(' ')[0] for l in kept), ['가', '나', '라'])

    # T424 — 굶주림 줄(순수 셈)
    _ledger424 = parse_runs('\n'.join([
        '{"sha":"%s","run":860,"tests":"failure","missing_modes":""}' % ('a' * 40),
        '{"sha":"%s","run":868,"tests":"success","missing_modes":""}' % ('b' * 40),
        '{"sha":"%s","run":885,"tests":"failure","missing_modes":""}' % ('c' * 40),
        '{"sha":"%s","run":903,"tests":"failure","missing_modes":""}' % ('d' * 40),
    ]))
    eq('T424 ⓐ 장부의 마지막 초록(이번 런 포함)', last_green_any(_ledger424), ('b' * 40, 868))
    _api424 = api_run_counts([{'status': 'completed', 'conclusion': 'cancelled'}] * 17
                             + [{'status': 'completed', 'conclusion': 'success'}] * 14
                             + [{'status': 'completed', 'conclusion': 'failure'}] * 6
                             + [{'status': 'in_progress', 'conclusion': None}] * 3)
    eq('T424 ⓐ API 런 결말 셈(도는 중은 안 센다)', (_api424['n'], _api424['cancelled'], _api424['success'], _api424['failure']), (37, 17, 14, 6))
    _st = starve_lines(_ledger424, 'd' * 40, 290, 134, 39, _api424)
    eq('T424 ⓐ 마지막 초록 런·나이·그 뒤 커밋·판정 런 수·취소 수가 한 줄에', all(x in _st[0] for x in ('#868', 'bbbbbbb', '4시간 50분', '134', '코드 39', '판정 런 2', '취소 17')), True)
    eq('T424 ⓐ 두 시간 넘게 굶으면 T338·T340 길을 덧붙인다', len(_st) == 2 and 'T338' in _st[1], True)
    _st2 = starve_lines(_ledger424, 'd' * 40, 30, 5, 2, None)
    eq('T424 ⓑ API 없으면 그 조각만 뺀다(줄은 남는다)', ('API' not in _st2[0]) and ('#868' in _st2[0]) and len(_st2) == 1, True)
    eq('T424 ⓑ 초록이 하나도 없으면 그렇게 말한다', '하나도 없다' in starve_lines(_ledger424[:1] + _ledger424[2:], 'd' * 40, None)[0], True)
    eq('T424 ⓑ 판정은 안 건드린다(순수 줄만)', judge({'sha': 'd' * 40, 'run': 903, 'tests': 'failure', 'missing_modes': ''}, True, 0, fails=['FAIL A.B.C.무엇 · Failed'])[0], 1)

    if fails:
        print('✗ check_unity_green --self-test 실패 %d' % len(fails))
        for f in fails:
            print('  · ' + f)
        return 1
    print('✓ check_unity_green --self-test %d칸 통과' % cells[0])
    return 0


def main(argv):
    ref, do_fetch, no_api = REF, False, False
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--self-test':
            return self_test()
        if a == '--fetch':
            do_fetch = True
        elif a == '--no-api':
            no_api = True
        elif a == '--ref' and i + 1 < len(argv):
            i += 1
            ref = argv[i]
        else:
            print('사용: check_unity_green.py [--fetch] [--no-api] [--ref origin/screens] [--self-test]')
            return 2
        i += 1

    if do_fetch:
        _git(['fetch', 'origin', 'screens'])

    meta = read_meta(ref)
    anc, n_after = behind(meta.get('sha') if meta else None)

    # T356 — «유니티가 실제로 돈 마지막 런» 뒤가 **막혀 있는가**(앞 잡 빨강 → unity skipped).
    #        API 를 못 부르면 조용히 건너뛴다(지금까지의 출력 그대로).
    blocked, blocks_now, nv = [], False, None
    if meta and not no_api:
        after = fetch_runs_after(str(meta.get('sha', '')))
        if after:
            blk, docs, unk = classify_after(after)
            still = still_blocked(after, blk)
            blocked = blocked_lines(blk, docs, unk, still)
            blocks_now = still
            # T373 — 그 창 안에 «유니티가 실제로 판정을 낸» 더 새로운 런이 있으면 장부가 낡은 것이다.
            nv = newer_verdict(after)
            if nv and _run_gt(nv.get('run'), meta.get('run')):
                # 쌓인 커밋 셈도 **그 런** 기준으로 다시 잰다 — 낡은 장부 기준이면 부풀려 말한다.
                a2, n2 = behind(nv.get('full'))
                if a2 is not None:
                    anc, n_after = a2, n2

    red = bool(meta) and str(meta.get('tests')) != 'success'
    fails = red_lines(ref) if red else []
    between = None
    own = None
    if red:
        cur = str(meta.get('sha', ''))
        runs = read_runs(ref)
        gsha, grun = last_green(runs, cur)
        commits = code_commits(gsha, cur) if gsha else code_commits(None, cur, RECENT_CODE)
        # T153 — 임자 줄이 «그의 몫» 으로 막기 전에, 그 작업이 이 창에서 프로덕션을 바꾸긴 했는지 먼저 센다
        text = red_text(ref)
        # T188 — 콘솔 빨강(RED)은 스택이 곧 증거다. T150 의 본문 경로에 **덧붙여** 준다(덮어쓰지 않는다).
        err = error_paths(text)
        for name, paths in stack_paths(text).items():
            for path in paths:
                if path not in err.setdefault(name, []):
                    err[name].append(path)
        miss_str = str(meta.get('missing_modes', '') or '')
        mode_logs = {x.strip(): read_mode_log(ref, x.strip()) for x in miss_str.split(',') if x.strip()}
        own = own_lines(fails, read_progress(), cur, err=err, passed=red_only(fails),
                        expected=expected_reds(text), window=window_owners(commits),
                        touched=prod_touch(commits), missing=miss_str, runs=runs,
                        mode_logs=mode_logs, meta_run=meta.get('run'))
        between = between_lines(commits, fixtures(fails), (gsha, grun), no_ledger=(runs is None))
    # T386 — 장부 머리의 «건너뜀» 줄은 빨간 런이 아니어도 읽는다(초록 런에도 «남은 일» 이 있을 수 있다).
    sk = skip_note(skips(red_text(ref)), row_status(read_progress()))
    rc, out = judge(meta, anc, n_after, fails, own, between, blocked, blocks_now, newer=nv, skipped=sk)
    for ln in out:
        print(ln)
    # T424 — 굶주림 줄(보고 전용 · rc 그대로): 마지막 초록 런의 나이 · 그 뒤 커밋 · API 로 본 최근 런 결말.
    if meta:
        ledger = read_runs(ref) or []
        g_sha, _ = last_green_any(ledger)
        g_age = commit_age_min(g_sha) if g_sha else None
        _, g_after = behind(g_sha) if g_sha else (None, None)
        g_code = len(code_commits(g_sha, MAIN)) if (g_sha and g_after) else (0 if g_sha else None)
        for ln in starve_lines(ledger, str(meta.get('sha', '')), g_age, g_after, g_code,
                               None if no_api else fetch_run_counts()):
            print(ln)
    if meta and (red or str(meta.get('missing_modes', '') or '')):
        # T338 — 남의 PlayMode 빨강이 내 dotnet·datasync·gate 초록을 덮지 않게 잡별 한 줄
        sha = str(meta.get('sha', ''))
        for ln in job_lines(None if no_api else fetch_jobs(sha), sha, unity_red=True):
            print(ln)
    return rc


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
