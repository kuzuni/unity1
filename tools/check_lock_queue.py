#!/usr/bin/env python3
"""산 lock 하나가 **몇 작업을 세우고 있는가** — 그리고 그중 몇이 «임자가 오래 안 건드린 파일» 때문인가 (T127).

규약(`docs/claims/README.md`)의 «두 작업이 같은 파일을 만져야 하면 뒤 번호가 기다린다» 는 옳다.
문제는 **한 작업이 공용 파일을 오래 쥘 때**다 — 2026-09-13 실측: T87 이 05:44~21:5x 동안
`Ui/Forge*`·`Assets/Forge/catalog.json`·`ForgeUiTests.cs` 를 쥐어 열린 작업 여덟이 회차마다
게이트만 돌리고 물러났다(워커 O 16:4x 보고 · 워커 H 다섯 회차 연속).

규약에는 이미 둘째 길이 있다 — «범위에 없는 파일을 열게 되면 표의 «범위» 칸을 먼저 고쳐 push»,
뒤집으면 **더는 안 여는 파일은 범위에서 빼도 된다**. 그 판단에 필요한 사실을 이 자가 띄운다:
  ⓐ 이 lock 뒤에 선 열린 작업이 누구누구인가
  ⓑ 어느 파일 때문인가
  ⓒ 그 파일을 **이 lock 의 작업이 마지막으로 만진 게 언제**인가(오래됐으면 범위에서 뺄 자리다)
  ⓓ 90분이 지난 죽은 lock

🚩 **판정은 안 바꾼다(rc 0 고정 · 보고 전용)**. 남의 선점 사정으로 모든 워커의 게이트를 빨갛게 만들면
T82 가 산 교훈(«남의 선점 몇 분 동안 모두가 빨개진다»)을 되풀이한다. 범위를 줄일지는 lock 임자가 정한다.

사용:  python3 tools/check_lock_queue.py [--self-test] [--hours N]  (N = «오래됐다» 의 문턱 · 기본 3)
"""
import io, os, re, subprocess, sys, time, fnmatch

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
CLAIMS = 'docs/claims'
PROGRESS = 'docs/PROGRESS.md'
DEAD_MIN = 90            # 규약: 90분 지난 lock 은 죽은 것
STALE_HOURS = 3.0        # «오래 안 건드렸다» 의 기본 문턱

OPEN = '⬜'
# 겹쳐도 «막는» 것이 아닌 자리: 문서는 누구나 매 회차 덧붙이고 rebase 로 푼다(규약도 문서 충돌을 막지 않는다).
IGNORE = ('docs/',)
# 한 패턴이 이만큼 넘는 파일에 걸리면 «폴더 자리»(= 새 파일을 둘 자리)로 본다 — 파일 선점이 아니다.
FOLDER_AT = 12
# 범위 칸은 «Ui/ForgeUi.cs» 처럼 폴더를 적기도 하고 «ForgeUi.cs» 처럼 파일명만 적기도 한다(T124 가 그렇다).
# 파일명만 적힌 것도 세지 않으면 줄 길이를 **적게** 센다 — 확장자로 가려 받아들인다(T127 2회차).
FILE_EXT = ('.cs', '.json', '.py', '.sh', '.js', '.yml', '.yaml', '.shader', '.asmdef', '.txt', '.ttf', '.unity', '.asset')
RE_LOCK = re.compile(r'^(\d{4}-\d\d-\d\dT\d\d:\d\d:\d\dZ)\s+(\S+)')
RE_TICK = re.compile(r'`([^`]+)`')


def parse_lock(text):
    """lock 파일 한 줄 → (UTC epoch, SID) · 못 읽으면 (None, None)."""
    m = RE_LOCK.match(text.strip())
    if not m:
        return None, None
    t = time.strptime(m.group(1), '%Y-%m-%dT%H:%M:%SZ')
    import calendar
    return calendar.timegm(t), m.group(2)


def scope_patterns(cell):
    """PROGRESS «범위» 칸 → 파일·글로브 목록(백틱 안에서 `/` 가 있는 것만 · 괄호 설명은 버린다)."""
    out = []
    for tok in RE_TICK.findall(cell):
        tok = tok.split('(')[0].strip().strip('·').strip().rstrip(',')
        if not tok:
            continue
        if '/' not in tok and not tok.endswith(FILE_EXT):
            continue                      # 파일 이야기가 아니다(`Big` · `Names` 같은 이름)
        if tok.endswith('/'):
            tok += '*'
        if tok not in out:
            out.append(tok)
    return out


def matches(pattern, path):
    """짧은 꼴(`Ui/Forge*`)도 · 폴더 없이 적힌 파일명(`ForgeUi.cs`)도 긴 경로에 맞는다.

    ⚠ 파일명만 적힌 꼴을 흘리면 줄 길이를 **적게** 센다 — T124 의 남은 자리(`ForgeAutoPopup.cs` 등)가
    T87 범위 `Ui/Forge*` 에 드는데도 안 찍혔다(T127 2회차 실측).
    """
    path = path.replace('\\', '/')
    pattern = pattern.replace('\\', '/')
    return fnmatch.fnmatch(path, pattern) or fnmatch.fnmatch(path, '*/' + pattern)


def expand(patterns, files, folder_at=FOLDER_AT):
    """패턴 목록 → 실제로 있는 파일 집합.

    빼는 것 셋: ⓐ 아직 없는 파일(새로 만들 파일이라 겹침이 아니다) ⓑ `docs/…`(문서는 매 회차 모두가 덧붙인다)
    ⓒ `.meta`(제 애셋을 따라간다) ⓓ 한 패턴이 `folder_at` 개를 넘겨 거는 **폴더 자리**(`Assets/Tests/PlayMode/` 처럼
    «새 파일을 여기 둔다» 는 뜻이지 그 폴더의 모든 파일을 쥔다는 뜻이 아니다)."""
    out = set()
    for p in patterns:
        if p.startswith(IGNORE):
            continue
        hit = set(f for f in files if matches(p, f) and not f.endswith('.meta') and not f.startswith(IGNORE))
        if len(hit) > folder_at:
            continue
        out |= hit
    return out


def progress_rows(text):
    """PROGRESS 표 → {번호: (표시, 범위 칸)} (같은 번호가 두 줄이면 «닫힌 쪽» 이 아니라 **열린 쪽**을 쥔다 — 기다리는 줄이 중요하다)."""
    out = {}
    for line in text.splitlines():
        if not line.startswith('| T'):
            continue
        cells = [c.strip() for c in line.strip().strip('|').split('|')]
        if len(cells) < 5:
            continue
        m = re.fullmatch(r'T(\d+)', cells[0])
        if not m:
            continue
        tid = 'T' + m.group(1)
        glyph = next((ch for ch in cells[2] if ch in '✅🔄⬜⛔✂'), None)
        if glyph is None:
            continue
        if tid in out and out[tid][0] == OPEN:
            continue
        out[tid] = (glyph, cells[4])
    return out


def queue(locks, rows, files):
    """→ [(lock 번호, [(기다리는 작업, [겹친 파일…])…])] · 산 lock 만 · 자기 자신은 뺀다."""
    res = []
    for tid in sorted(locks, key=lambda t: int(t[1:])):
        mine = expand(scope_patterns(rows.get(tid, ('', ''))[1]), files)
        if not mine:
            continue
        waiting = []
        for other, (glyph, cell) in sorted(rows.items(), key=lambda kv: int(kv[0][1:])):
            if other == tid or glyph != OPEN or other in locks:
                continue
            shared = sorted(mine & expand(scope_patterns(cell), files))
            if shared:
                waiting.append((other, shared))
        if waiting:
            res.append((tid, waiting))
    return res


def last_touch(tid, path):
    """그 작업의 커밋이 이 파일을 마지막으로 만진 시각(epoch) · 없으면 None."""
    try:
        out = subprocess.check_output(
            ['git', 'log', '-1', '--format=%ct', '-E', '--grep=^' + tid + r'([^0-9]|$)', '--', path],
            cwd=ROOT, stderr=subprocess.DEVNULL).decode().strip()
    except Exception:
        return None
    return int(out) if out else None


RE_SID = re.compile(r'sess-\d{4}-\d+')


def task_last_commit(tid):
    """그 번호로 나간 **마지막 커밋**의 (시각 epoch, SID) · 없거나 SID 를 못 읽으면 (None, None).

    T418 — lock 은 90분에 죽지만 **그 일은 다른 세션으로 이어지고 있을 수 있다** — 그것을 보려면 이력을 읽어야 한다.
    """
    try:
        out = subprocess.check_output(
            ['git', 'log', '-1', '--format=%ct%x09%s', '-E', '--grep=^' + tid + r'([^0-9]|$)'],
            cwd=ROOT, stderr=subprocess.DEVNULL).decode().strip()
    except Exception:
        return None, None
    if not out:
        return None, None
    ct, _tab, subj = out.partition('\t')
    m = RE_SID.search(subj)
    try:
        return int(ct), (m.group(0) if m else None)
    except ValueError:
        return None, None


def split_line(tid, lock_sid, commit_sid, commit_age_min):
    """죽은 lock 줄에 덧붙일 한 줄 · 덧붙일 것이 없으면 None (T418 · 순수 함수).

    세 갈래다 — ⓐ 그 번호가 90분 밖에도 안 움직였으면 조용히(임자가 정말 떠난 것이다)
    ⓑ 같은 SID 가 90분 안에 움직였으면 «제 세션이 갱신만 잊은 것»
    ⓒ **다른 SID** 가 90분 안에 움직였으면 «세션 갈림 의심» — 임자가 슬롯을 갈아타면서 새 SID 로 lock 을 안 덮은 꼴이다.
    판정은 안 바꾼다(rc 0) — «뺏을 수 있다» 는 그대로고, **뺏는 사람이 부딪힐 것을 미리 보게** 한다.
    """
    if commit_sid is None or commit_age_min is None or commit_age_min >= DEAD_MIN:
        return None
    if commit_sid == lock_sid:
        return ('    ↳ ⚠ 네 lock 이 아직 일하는 중이다 — 같은 SID(%s)가 %.0f분 전에 움직였다. '
                '**타임스탬프를 갱신해 push 해라** — 그것이 «살아 있다» 의 유일한 신호다(claims/README 12행).'
                % (commit_sid, commit_age_min))
    return ('    ↳ ⚠ **세션 갈림 의심** — %s 는 %.0f분 전에 `%s`(lock 의 SID `%s` 가 아니다)로 움직였다. '
            '뺏기 전에 그 커밋을 보고, 뺏거든 **그 사람의 진행 중인 파일을 깨지 마라**(T418).'
            % (tid, commit_age_min, commit_sid, lock_sid))



def tracked_files():
    try:
        out = subprocess.check_output(['git', 'ls-files'], cwd=ROOT, stderr=subprocess.DEVNULL).decode()
    except Exception:
        return []
    return [l for l in out.split('\n') if l]


def hours(sec):
    return sec / 3600.0


def main(argv):
    stale = STALE_HOURS
    if '--hours' in argv:
        stale = float(argv[argv.index('--hours') + 1])
    now = time.time()
    locks = {}
    d = os.path.join(ROOT, CLAIMS)
    for f in sorted(os.listdir(d)) if os.path.isdir(d) else []:
        if not f.endswith('.lock'):
            continue
        ts, sid = parse_lock(io.open(os.path.join(d, f), encoding='utf-8').read())
        if ts is None:
            print('⚠ %s — 한 줄 꼴(«UTC ISO8601 SID»)이 아니다' % f)
            continue
        locks[f[:-5]] = (ts, sid)
    dead = [(t, (now - ts) / 60.0) for t, (ts, _s) in locks.items() if (now - ts) / 60.0 > DEAD_MIN]
    rows = progress_rows(io.open(os.path.join(ROOT, PROGRESS), encoding='utf-8').read())
    files = tracked_files()
    q = queue(set(locks), rows, files)

    if not q and not dead:
        print('✓ check_lock_queue: 산 lock %d개 · 그 뒤에 선 열린 작업 0 · 죽은 lock 0' % len(locks))
        return 0
    for tid, waiting in q:
        ts = locks[tid][0]
        print('· %s (%.1f시간째 · %s) 뒤에 선 열린 작업 %d개' % (tid, hours(now - ts), locks[tid][1], len(waiting)))
        idle = {}
        for other, shared in waiting:
            head = ' · '.join(shared[:5]) + ('' if len(shared) <= 5 else ' … 외 %d개' % (len(shared) - 5))
            print('    %-6s ← %s' % (other, head))
            for f in shared:
                if f not in idle:
                    idle[f] = last_touch(tid, f)
        old = [(f, lt) for f, lt in idle.items() if lt is None or hours(now - lt) >= stale]
        old.sort(key=lambda t: (t[1] is not None, t[1]))
        for f, lt in old[:8]:
            when = '이 작업이 한 번도 안 만졌다' if lt is None else '%.1f시간째 안 건드림' % hours(now - lt)
            print('    ⌛ %s — %s' % (f, when))
        if len(old) > 8:
            print('    ⌛ … 외 %d개' % (len(old) - 8))
        if old:
            print('    → 더 안 열 파일이면 PROGRESS «범위» 칸에서 빼고 push 하면 그 작업들이 바로 풀린다(claims/README 둘째 길).')
    for tid, mins in sorted(dead):
        print('⚠ %s.lock 이 %.0f분 됐다 — 규약상 죽은 lock(뺏을 수 있다 · 뺏을 땐 자기 SID 로 덮어 커밋·push)' % (tid, mins))
        ct, csid = task_last_commit(tid)
        hint = split_line(tid, locks[tid][1], csid, None if ct is None else (now - ct) / 60.0)
        if hint:
            print(hint)
    print('· (보고 전용 — rc 는 늘 0이다. 범위를 줄일지는 그 lock 임자가 정한다.)')
    return 0


def self_test():
    ok = True
    ts, sid = parse_lock('2026-09-13T21:59:00Z sess-2157-32053\n')
    if sid != 'sess-2157-32053' or ts is None:
        print('✗ lock 읽기: %r %r' % (ts, sid)); ok = False
    if parse_lock('쓰레기') != (None, None):
        print('✗ lock 읽기: 꼴이 아닌 것을 받아들였다'); ok = False

    cell = ('`Assets/Scripts/Game/Ui/Forge*` · `Ui/Anvil*` · `Assets/Forge/catalog.json`(연출 수치) · '
            '`docs/PROGRESS.md` · `Big` · `ForgeUi.cs`(각 lock 뒤) · `Names`')
    got = scope_patterns(cell)
    want = ['Assets/Scripts/Game/Ui/Forge*', 'Ui/Anvil*', 'Assets/Forge/catalog.json', 'docs/PROGRESS.md', 'ForgeUi.cs']
    if got != want:
        print('✗ 범위 읽기: %r' % got); ok = False
    if scope_patterns('`Assets/Scripts/Core/CraftFx/`(CssTrack)') != ['Assets/Scripts/Core/CraftFx/*']:
        print('✗ 범위 읽기: 폴더 꼴'); ok = False

    for pat, path, want in [
        ('Ui/Forge*', 'Assets/Scripts/Game/Ui/ForgeSheet.cs', True),
        ('Ui/Forge*', 'Assets/Scripts/Game/Ui/GearDetailPopup.cs', False),
        ('Assets/Forge/catalog.json', 'Assets/Forge/catalog.json', True),
        ('Ui/UiKit.cs', 'Assets/Scripts/Game/Ui/UiKit.cs', True),
        ('Ui/Forge*', 'Assets/Tests/PlayMode/ForgeUiTests.cs', False),
        ('ForgeUi.cs', 'Assets/Scripts/Game/Ui/ForgeUi.cs', True),          # 폴더 없이 적힌 파일명(T127 2회차)
        ('ForgeUi.cs', 'Assets/Scripts/Game/Ui/ForgeUiTests.cs', False),    # 이름이 겹쳐 보이는 다른 파일은 아니다
    ]:
        if matches(pat, path) != want:
            print('✗ 맞춤: %s ↔ %s = %r' % (pat, path, not want)); ok = False

    files = ['Assets/Scripts/Game/Ui/ForgeSheet.cs', 'Assets/Forge/catalog.json',
             'Assets/Scripts/Game/Ui/Popups.cs', 'Assets/Tests/PlayMode/ForgeUiTests.cs']
    rows = {
        'T87': ('🔄', '`Assets/Scripts/Game/Ui/Forge*` · `Assets/Tests/PlayMode/ForgeUiTests.cs`'),
        'T98': ('⬜', '`Assets/Scripts/Game/Ui/ForgeSheet.cs` · `Assets/Forge/catalog.json`'),
        'T94': ('⬜', '`Assets/Scripts/Game/Ui/Popups.cs` · `Assets/Tests/PlayMode/ForgeUiTests.cs`'),
        'T50': ('✅', '`Assets/Scripts/Game/Ui/ForgeSheet.cs`'),
        'T99': ('⬜', '`Assets/Scripts/Game/Ui/Popups.cs`'),
    }
    rows['T124'] = ('⬜', '`Assets/Scripts/Game/Ui/AgePattern.cs` · `ForgeUi.cs`(각 lock 뒤)')
    files = files + ['Assets/Scripts/Game/Ui/ForgeUi.cs']
    q = queue({'T87'}, rows, files)
    if [(t, [w for w, _f in ws]) for t, ws in q] != [('T87', ['T94', 'T98', 'T124'])]:
        print('✗ 줄 세기: %r' % q); ok = False
    shared = dict((w, f) for w, f in q[0][1]) if q else {}
    if shared.get('T98') != ['Assets/Scripts/Game/Ui/ForgeSheet.cs']:
        print('✗ 겹친 파일: %r' % shared.get('T98')); ok = False
    if [w for w, _f in queue({'T87', 'T94'}, rows, files)[0][1]] != ['T98', 'T124']:
        print('✗ 이미 lock 을 쥔 작업은 «기다리는 줄» 이 아니다'); ok = False
    if expand(['docs/PROGRESS.md'], ['docs/PROGRESS.md']):
        print('✗ 문서는 막는 자리가 아니다'); ok = False
    if expand(['Assets/Scripts/Game/Ui/ForgeSheet.cs'], ['Assets/Scripts/Game/Ui/ForgeSheet.cs.meta']):
        print('✗ .meta 는 제 애셋을 따라간다'); ok = False
    many = ['Assets/Tests/PlayMode/T%d.cs' % i for i in range(20)]
    if expand(['Assets/Tests/PlayMode/*'], many, folder_at=12):
        print('✗ 폴더 자리(파일 20개)는 파일 선점이 아니다'); ok = False
    if len(expand(['Assets/Tests/PlayMode/*'], many[:5], folder_at=12)) != 5:
        print('✗ 작은 폴더는 그대로 센다'); ok = False
    # ✅ 로 적어 놓고 «CI 한 바퀴» 를 기다리며 lock 을 쥔 회차가 흔하다 — 그동안에도 그 파일은 남의 것이다.
    if [t for t, _w in queue({'T50'}, rows, files)] != ['T50']:
        print('✗ ✅ 로 적힌 작업이라도 lock 을 쥐고 있으면 그 뒤에 줄이 선다'); ok = False

    # T418 — «죽은 lock 인데 그 번호는 아직 움직인다» 세 갈래(순수 함수라 git 없이 잰다).
    if split_line('T414', 'sess-0418-15809', 'sess-0418-15809', 200.0) is not None:
        print('✗ 세션 갈림: 그 번호가 90분 밖이면 덧줄이 없어야 한다'); ok = False
    if split_line('T414', 'sess-0418-15809', None, 10.0) is not None:
        print('✗ 세션 갈림: 커밋에서 SID 를 못 읽었으면 지어내지 않는다'); ok = False
    same = split_line('T414', 'sess-0418-15809', 'sess-0418-15809', 53.0)
    if same is None or '갱신' not in same or '세션 갈림' in same:
        print('✗ 세션 갈림: 같은 SID 면 «네 lock 을 갱신해라» 다 — %r' % same); ok = False
    diff = split_line('T414', 'sess-0418-15809', 'sess-0518-9931', 53.0)
    if diff is None or '세션 갈림 의심' not in diff or 'sess-0518-9931' not in diff or 'sess-0418-15809' not in diff:
        print('✗ 세션 갈림: 다른 SID 면 두 SID 를 다 대고 «세션 갈림 의심» 이어야 한다 — %r' % diff); ok = False
    # 커밋 제목에서 SID 를 읽는 자리(실물 꼴 그대로).
    if RE_SID.search('T414 2회차(T413 에서 번호 옮김): … (sess-0518-9931 · 워커 J)').group(0) != 'sess-0518-9931':
        print('✗ 커밋 제목에서 SID 읽기'); ok = False
    if RE_SID.search('T1 무언가 (워커 K)') is not None:
        print('✗ SID 가 없는 제목에서 지어내면 안 된다'); ok = False

    print('· 지금 레포:'); main([])
    print('✓ check_lock_queue 자기 검사 통과' if ok else '✗ check_lock_queue 자기 검사 실패')
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(self_test() if '--self-test' in sys.argv else main(sys.argv[1:]))
