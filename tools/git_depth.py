#!/usr/bin/env python3
"""얕은 clone 에서 «로컬 git 이력» 에 기대는 자들이 **조용히 거짓을 내지 않게** — 이력 깊이를 한 회차에 한 번 벌린다 (T491 · T489 자손).

§0-1 이 적어 둔 정상 시작 경로가 `git clone --depth 1` 이고, 그 clone 은 **커밋을 하나만** 가진다.
T489 는 그 조건이 `screens` **가지 추적**을 깬 것을 고쳤다. 같은 조건이 **커밋 이력**을 쓰는 자리들도 깬다:

  · `git merge-base --is-ancestor <sha> origin/main` — 그 객체가 없으면 git 이 **rc 128**(«Not a valid object name»)을 낸다.
    rc 1(진짜 남의 갈래)과 같이 읽으면 **멀쩡한 초록을 «force push 가 있었다 · 판정을 믿지 말고» 로 부정한다**(T491 증상 ①).
  · `git log -1 --format=%ct <sha>` — 없는 객체는 나이가 `None` 이고, 그러면 T424 의 굶주림 경고 갈래가
    **구조상 한 번도 안 들어간다**(증상 ②③).
  · `git log --grep='^T<번호>'` — 커밋이 하나뿐이라 한 줄도 못 찾고, 자는 그것을 «안 움직였다» 로 **단정한다**(증상 ④).

그래서 이 자리의 규칙은 둘이다 — **ⓐ 한 번 벌려 본다** · **ⓑ 그래도 모르면 «모른다» 고 말한다**(거짓 단정 금지).

🚨 **그 sha 만 따로 받는 것으로는 조상 판정이 안 고쳐진다**(T491 실측): `git fetch --depth=1 origin <sha>` 뒤엔
rc 가 128 → 1 로 바뀔 뿐 답은 그대로 틀리다(얕은 graft 라 받은 커밋이 제 뿌리로 서서 main 과 안 이어진다).
**나이**(`%ct`)는 그 객체 하나만 받아도 고쳐진다 — 그래서 `fetch_one` 과 `deepen` 을 따로 둔다.

사용:  python3 tools/git_depth.py [--self-test] [--deepen]
"""

import os
import subprocess
import sys

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
DEPTH = 200          # 한 번 벌릴 깊이 — 두 달 멈춤 뒤에도 «그 번호의 마지막 커밋» 이 드는 길이
_deepened = {}       # (remote, branch, root) → (벌렸나, 한 줄) · **한 회차에 한 번만** 부른다
_fetched = set()     # 낱개로 받아 본 sha — 두 번 안 받는다


def _git(args, root=ROOT):
    """git 을 돌려 (rc, stdout) — 실패해도 예외를 안 던진다(자는 판정을 내야 한다)."""
    try:
        p = subprocess.run(['git'] + args, cwd=root, stdout=subprocess.PIPE,
                           stderr=subprocess.PIPE, text=True)
        return p.returncode, p.stdout
    except OSError as e:
        return 127, str(e)


# ── 순수 함수(자기 검사가 네트워크 없이 재는 자리) ───────────────────────────────

def shallow_from_output(rc, out):
    """`git rev-parse --is-shallow-repository` 의 (rc, stdout) → True/False/None(모름)."""
    if rc != 0:
        return None
    s = (out or '').strip()
    if s == 'true':
        return True
    if s == 'false':
        return False
    return None


def ancestor_from_rc(rc):
    """`git merge-base --is-ancestor` 의 rc → True(조상) / False(아니다) / **None(모름)**.

    ⚠ 이것이 T491 증상 ① 의 고침이다 — 0·1 **밖의 rc**(없는 객체 128 · git 없음 127)는 «아니다» 가 아니라 «모른다» 다.
    """
    if rc == 0:
        return True
    if rc == 1:
        return False
    return None


def ancestor_from_compare(payload):
    """GitHub `/compare/<sha>...main` 응답 → True/False/None (순수 · T491 ⓒ).

    `status` 는 **뒤쪽(main) 기준**이다 — `identical`·`ahead`(main 이 앞섰다 · `behind_by` 0)면 그 sha 는 main 의 조상이다.
    `behind`·`diverged` 면 아니다. 그 밖(응답이 없거나 꼴이 다르면)은 모른다.
    """
    if not isinstance(payload, dict):
        return None
    st = str(payload.get('status') or '')
    if st == 'identical':
        return True
    if st == 'ahead':
        return None if payload.get('behind_by') else True
    if st in ('behind', 'diverged'):
        return False
    return None


def deepen_note(before, after, depth=DEPTH):
    """깊이를 벌린 결과를 한 줄로 (순수) — 안 늘었으면 ''."""
    if before is None or after is None or after <= before:
        return ''
    return ('`git fetch --deepen=%d` 로 이력을 벌렸다(커밋 %d → %d) — 얕은 clone 이라 '
            '«조상인가 · 몇 분 전인가 · 그 번호가 움직였나» 를 **로컬 이력으로 못 보던 자리**다(T491).'
            % (depth, before, after))


def needs_deepen(shallow, count, depth=DEPTH):
    """벌려야 하는가 (순수) — 얕고(True) **그러면서 이력이 짧을 때만** 참.

    🚨 `--depth=N` 이 아니라 `--deepen=N` 을 쓰는 까닭이 여기 있다: git 의 `--depth` 는 «깊이를 N 으로 **맞춘다**» 라
    이미 N 보다 깊은 clone 에서는 **이력을 잘라낸다**(이 컨테이너 실측 — 얕은 표식은 True 인데 커밋이 3179개다.
    `--depth=200` 을 쳤으면 3179 → 200 으로 **줄었다**). `--deepen` 은 **지금에서 더** 벌리기만 한다.
    """
    if shallow is not True:
        return False
    if count is None:
        return True
    return count < depth


# ── git 을 실제로 부르는 자리 ───────────────────────────────────────────────────

def count_commits(root=ROOT):
    rc, out = _git(['rev-list', '--count', 'HEAD'], root)
    if rc != 0:
        return None
    try:
        return int(out.strip())
    except ValueError:
        return None


def is_shallow(root=ROOT):
    return shallow_from_output(*_git(['rev-parse', '--is-shallow-repository'], root))


def deepen(remote='origin', branch='main', depth=DEPTH, root=ROOT):
    """얕으면 이력을 **한 번** 벌린다 → (벌렸나, 한 줄 설명). 얕지 않거나 못 벌리면 (False, '').

    같은 (remote, branch, root) 로 두 번째부터는 **첫 결과를 그대로** 돌려준다 — 한 회차에 한 번이다.
    """
    key = (remote, branch, root)
    if key in _deepened:
        return _deepened[key]
    before = count_commits(root)
    if not needs_deepen(is_shallow(root), before, depth):
        res = (False, '')
    else:
        _git(['fetch', '--deepen=%d' % depth, remote, branch], root)
        after = count_commits(root)
        note = deepen_note(before, after, depth)
        res = (bool(note), note)
    _deepened[key] = res
    return res


def fetch_one(sha, remote='origin', root=ROOT):
    """그 커밋 **하나**만 받아 본다(나이를 세려면 이것으로 된다 · 조상 판정은 안 된다 · 한 sha 에 한 번)."""
    if not sha or sha in _fetched:
        return False
    _fetched.add(sha)
    rc, _ = _git(['fetch', '--depth=1', remote, sha], root)
    return rc == 0


def self_test():
    ok = True

    def box(name, got, want):
        if got != want:
            print('✗ %s: %r (바라던 것 %r)' % (name, got, want))
            return False
        return True

    # T491 ⓐ — 얕은가 읽기
    ok &= box('T491 ⓐ 얕다', shallow_from_output(0, 'true\n'), True)
    ok &= box('T491 ⓐ 안 얕다', shallow_from_output(0, 'false\n'), False)
    ok &= box('T491 ⓐ git 이 없다', shallow_from_output(127, 'no git'), None)
    ok &= box('T491 ⓐ 모를 말', shallow_from_output(0, '아무거나'), None)

    # T491 ⓑ — **이것이 증상 ① 의 고침이다**: 0·1 밖의 rc 는 «아니다» 가 아니라 «모른다»
    ok &= box('T491 ⓑ rc 0 = 조상', ancestor_from_rc(0), True)
    ok &= box('T491 ⓑ rc 1 = 아니다', ancestor_from_rc(1), False)
    ok &= box('T491 ⓑ rc 128(없는 객체) = **모른다**', ancestor_from_rc(128), None)
    ok &= box('T491 ⓑ rc 127(git 없음) = 모른다', ancestor_from_rc(127), None)

    # T491 ⓒ — API 로 가르는 길
    ok &= box('T491 ⓒ ahead = 조상', ancestor_from_compare({'status': 'ahead', 'ahead_by': 8, 'behind_by': 0}), True)
    ok &= box('T491 ⓒ identical = 조상', ancestor_from_compare({'status': 'identical'}), True)
    ok &= box('T491 ⓒ diverged = 아니다', ancestor_from_compare({'status': 'diverged', 'behind_by': 2}), False)
    ok &= box('T491 ⓒ behind = 아니다', ancestor_from_compare({'status': 'behind'}), False)
    ok &= box('T491 ⓒ 응답 없음 = 모른다', ancestor_from_compare(None), None)
    ok &= box('T491 ⓒ ahead 인데 behind_by 가 있다 = 모른다', ancestor_from_compare({'status': 'ahead', 'behind_by': 3}), None)

    # T491 ⓐ — 벌린 결과 한 줄
    if '커밋 1 → 200' not in deepen_note(1, 200):
        print('✗ T491 ⓐ 벌린 줄: %r' % deepen_note(1, 200)); ok = False
    ok &= box('T491 ⓐ 안 늘었으면 말이 없다', deepen_note(200, 200), '')
    ok &= box('T491 ⓐ 못 셌으면 말이 없다', deepen_note(None, None), '')

    # T491 ⓐ — **이미 깊으면 안 벌린다**(`--depth` 였다면 잘라냈을 자리 · 이 컨테이너 실측 3179커밋)
    ok &= box('T491 ⓐ 얕고 짧다 = 벌린다', needs_deepen(True, 1), True)
    ok &= box('T491 ⓐ 얕지만 이미 깊다 = 그냥 쓴다', needs_deepen(True, 3179), False)
    ok &= box('T491 ⓐ 안 얕다 = 그냥 쓴다', needs_deepen(False, 1), False)
    ok &= box('T491 ⓐ 깊이를 못 셌다 = 벌려 본다', needs_deepen(True, None), True)

    # T491 ⓐ — 같은 회차에 두 번 벌리지 않는다(캐시)
    _deepened[('x', 'y', 'z')] = (True, '한 번 벌렸다')
    ok &= box('T491 ⓐ 두 번째는 첫 답', deepen('x', 'y', root='z'), (True, '한 번 벌렸다'))
    _deepened.pop(('x', 'y', 'z'), None)

    print('✓ git_depth --self-test 21칸 통과' if ok else '✗ git_depth --self-test 실패')
    return 0 if ok else 1


def main(argv):
    if '--self-test' in argv:
        return self_test()
    if '--deepen' in argv:
        did, note = deepen()
        print(('⚑ ' + note) if did else '· 이력이 이미 깊다(또는 벌릴 수 없다) — 그대로 쓴다.')
        return 0
    print('· 얕은 clone 인가: %r · 커밋 수: %r' % (is_shallow(), count_commits()))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
