#!/usr/bin/env python3
# -*- coding: utf-8 -*-
r"""«이미 끝난 일을 또 잡는» 헛구덩이를 막는 자 (T193).

왜 있나 — 오늘만 세 번 샜다.
  · 워커 J 가 **T149·T151** 을 선점했다가 05:41 커밋이 이미 다 고쳐 놓은 것을 코드에서 알았다(결정 455).
  · 내가 **T188** 을 선점했더니 31분 전에 워커 L 이 끝내 놓았다(결정 479).
  · 내가 **T161** 을 잡으려다 `git log -- <그 파일>` 한 줄로 `2ad1aeaf`(워커 L)를 봤다 — 이미 다 들어 있었다.

**뿌리는 «두 문서가 어긋난다» 는 것이다.** 선점은 `docs/ROUTINE.md` §2 **제목 줄**을 보고 하는데
(§0 4항: «선점 가능한 «가장 앞» 작업»), «끝났다» 는 사실은 `docs/PROGRESS.md` **상태 칸**에 적힌다.
제목의 ✅ 는 사람이 손으로 다는 것이라 자주 빠진다 — T161 은 PROGRESS 가 «✅ 완료 · CI 확인 끝» 인데
ROUTINE 제목에는 ✅ 가 없었다. 그러면 **열린 일로 보인다**. lock 이 없는 것도 신호가 못 된다
(끝내면 반납하므로 «없음» 이 «안 했음» 과 «다 했음» 을 못 가른다).

`check_task_rows.py`(결정 455)는 **PROGRESS 표 안에서** 같은 ID 가 두 줄로 갈라진 것을 잡는다.
이 자는 그 옆칸을 본다 — **ROUTINE 제목 ↔ PROGRESS 상태**가 어긋나는 자리.

**T205 로 하나 더 본다 — «한 번호가 두 작업을 가리킨다».** `docs/claims/README.md` 가
«한 번호는 한 작업만 가리킨다 · 번호 재사용 금지» 라고 규칙은 적어 뒀는데 **자가 없었다**.
2026-09-07 하루에 세 번 났다: T189(→T192) · T190 둘 · T204 둘. 그때마다 사람이 손으로 찾아
보고 커밋을 쓰거나(워커 A 17:11) 선점을 반납했다(워커 A 17:09). 번호가 갈라지면
`T204.lock` 이 «어느 일» 인지 못 가르고 두 워커가 같은 파일을 반대로 민다.
곁들여 «제목은 있는데 PROGRESS 행이 없는» 작업도 **알리기만** 한다(등재 중인 자리가 정상적으로 그 꼴이라
실패로 세지 않는다 — 다만 그 사이 `check_task_rows` 가 그 작업을 못 본다).

**T210 으로 «닫힌 꼴» 을 둘로 넓혔다** — ✅ 완료 말고 **⛔ 폐기·흡수**도 닫힌 자리다.
실측에서 넷(T25→T37 · T27→T38 · T30→T43 · T32→T42)이 표에는 ⛔ 인데 §2 제목엔 아무 표시가 없어
**열린 일로 보였다**. 폐기에는 ✅ 가 아니라 **⛔** 를 단다(✅ 를 달면 «했다» 는 뜻이 된다).
같은 회차에 파서도 고쳤다 — 칸을 `[^|]*` 로 자르면 본문의 **`\|`(escape 된 파이프)** 앞에서 끊겨
상태를 엉뚱한 조각에서 읽는다(내 T196 행이 «iPad\» 를 상태로 읽고 있었다 · `check_task_rows` 는 이미 고쳐져 있었다).

쓰는 법
  python3 tools/task_state.py --check     # 게이트: 번호 중복·제목↔상태 어긋남이 있으면 1 (ROUTINE §3 목록)
  python3 tools/task_state.py T161        # 선점 «직전» 한 줄 — 잡아도 되는지 판정 (0 = 잡아도 된다)
  python3 tools/task_state.py --list      # 전체 표
  python3 tools/task_state.py --new-id    # 등재 «직전» — 다음 작업 번호(표·이력 둘 다 보여 준다)
  python3 tools/task_state.py --self-test # 이 자가 실제로 잡는지
  (T29) «코드 자취» 는 코드 식별자·파일명·폴더명만 센다 — 주석·문자열 리터럴·문서 내용은 뺀다.

**T415 로 «표 밖» 을 하나 더 본다 — 행을 지우면 번호가 되살아난다.**
위 T205 몫(«한 번호가 두 작업»)은 **표 안에서** 같은 ID 가 둘인 것을 본다. 그런데 2026-09-10 에
워커 L 이 T409 를 접으며 **행을 지웠고**(최대가 409 → 408 로 내려갔다) 30분 뒤 워커 C 가
규칙대로 «가장 큰 것 +1» 을 따라 **다시 409** 를 발급했다 — 표에는 행이 **하나**라 중복 자는 초록이다.
깨진 것은 표 안이 아니라 **표와 이력 사이**다. `--new-id` 는 발급을 이력(append-only)에서 뽑고,
`--check` 는 «이력에는 있는데 표에 행이 없는» 번호를 **알리기만** 한다(rc 는 안 건드린다 · 결정 493).
"""
import io
import os
import re
import subprocess
import sys
import datetime

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ROUTINE = os.path.join(ROOT, "docs", "ROUTINE.md")
PROGRESS = os.path.join(ROOT, "docs", "PROGRESS.md")
CLAIMS = os.path.join(ROOT, "docs", "claims")

# «### T161 — …» · «### T188 ✅ — …» — 꼬리에 -gear 같은 갈래가 붙는 ID 는 이 표의 대상이 아니다(별개 작업).
HEAD = re.compile(r"^###\s+(T\d+)(?![\w-])(.*)$")
# ⚠ 칸을 «`|` 로 자르기» 는 그냥은 안 된다 — 본문에 **`\|` 로 escape 된 파이프**가 있다
# (내 T196 행의 `iPhone\|iPad\|iPod\|Android` 가 그것이다). `[^|]*` 는 그 앞에서 끊겨
# **상태 칸을 엉뚱한 조각에서 읽는다**(그 행은 «iPad\» 를 상태로 읽고 있었다 · T210 실측 · T201 과 같은 갈래).
# 그래서 «앞이 역슬래시가 아닌 `|`» 로만 자른다.
SPLIT = re.compile(r"(?<!\\)\|")
ID_IN_CELL = re.compile(r"^\s*(T\d+)(?![\w-])")


def cells(line):
    """표 한 줄 → (ID, 작업 칸, 상태 칸) · 표 줄이 아니거나 칸이 모자라면 None."""
    if not line.lstrip().startswith("|"):
        return None
    parts = SPLIT.split(line.rstrip("\n"))
    if len(parts) < 4:
        return None
    m = ID_IN_CELL.match(parts[1])
    return (m.group(1), parts[2], parts[3]) if m else None
STALE_MIN = 90          # docs/claims/README.md 의 «90분» 규약과 같은 값


def _lead(s):
    """칸의 «머리» — 앞의 굵게·기울임 표시를 벗긴 첫 글자 자리(§4 규약: 표시는 칸 맨 앞에 적는다)."""
    return (s or "").strip().lstrip("*_ ").strip()


def _fold(s):
    """접어 둔 중복 행 표시(✂·♻)는 상태로 안 센다 — check_task_rows.py 와 같은 규약.

    ⚑ **칸 «머리» 에서만 읽는다**(T249). 규약이 접힘을 칸 맨 앞에 적기 때문이다
    («✂ 중복 행 — 살아 있는 기록은 N행이다» · `check_task_rows` 의 안내문 그대로).
    «칸 어디에든» 으로 읽으면 **본문에 ✂ 를 인용한 살아 있는 줄**이 통째로 접힘 처리된다 —
    T172 가 그랬다: 머리는 «✅» 인데 2366번째 글자의 «✂ 주인 13:2X 로 취소(→ T192)» 때문에
    상태 지도에서 빠졌고, 그래서 **제목↔상태 대조가 그 작업을 아예 안 봤다**(§2 제목은 마커 없이
    열린 채로 남고 자는 초록). 빠뜨림은 이 자에게 가장 나쁜 고장이다 — 존재 이유가 그것이므로.
    바로 아래 상태 마커 읽기와 **같은 꼴**로 읽는다(굵게 표시를 벗기고 머리를 본다).
    """
    return _lead(s).startswith(("✂", "♻"))


def row_ids_all(path=PROGRESS):
    """표에 **한 줄이라도** 있는 ID 전부(접힌 줄 포함) — «행이 없다» 와 «접힌 행만 있다» 를 가르는 데 쓴다(T206)."""
    out = set()
    with io.open(path, encoding="utf-8") as f:
        for line in f:
            c = cells(line)
            if c:
                out.add(c[0])
    return out


def routine_heads(path=ROUTINE, dups=None):
    """ID → (줄번호, 제목에 ✅ 가 있나, 제목 원문).

    `dups` 에 dict 를 주면 **번호가 두 번 이상 붙은 자리**를 ID → [(줄번호, 제목), …] 로 채운다(T205).
    """
    out, seen = {}, {}
    with io.open(path, encoding="utf-8") as f:
        for n, line in enumerate(f, 1):
            m = HEAD.match(line.rstrip("\n"))
            if not m:
                continue
            tid, rest = m.group(1), m.group(2)
            # 제목의 «맨 앞 토막»(첫 — 앞)에 있는 ✅ 만 «이 작업이 끝났다» 는 표시다.
            # 본문 쪽 «✅ 완료(코드 …)» 는 꼬리에 덧붙인 진행 기록이라 제목 표시와 구별한다.
            head_part = rest.split("—")[0]
            # 두 번째 칸 = 제목이 단 «닫힘 표시»(✅ 완료 · ⛔ 폐기·흡수) · 없으면 "" (T210 전에는 bool 이었다 · 참·거짓 쓰임은 그대로 산다)
            closed = "✅" if "✅" in head_part else ("⛔" if "⛔" in head_part else "")
            out.setdefault(tid, (n, closed, rest.strip()))
            seen.setdefault(tid, []).append((n, rest.strip()))
    if dups is not None:
        dups.clear()
        dups.update({t: v for t, v in seen.items() if len(v) > 1})
    return out


def progress_rows(path=PROGRESS):
    """ID → (줄번호, 상태 칸). 같은 ID 가 여러 줄이면 «가장 앞선 상태» 를 쓴다(✅ > 🔄 > ⬜)."""
    # ⛔ = «폐기 · 다른 번호에 흡수»(T210) — ✅ 와 마찬가지로 **닫힌** 꼴이라 선점 대상이 아니다.
    rank = {"✅": 4, "⛔": 3, "🔄": 2, "⬜": 1}
    out = {}
    with io.open(path, encoding="utf-8") as f:
        for n, line in enumerate(f, 1):
            c = cells(line)
            if not c:
                continue
            tid, work, state = c
            if _fold(work) or _fold(state):
                continue
            # ⚠ 칸 «어디에든» ✅ 가 있으면 완료로 세면 안 된다 — «🔄 코드 push … 로컬 게이트 전부 초록 ✅» 같은
            # 진행 기록에도 ✅ 가 흔하고, «**비평 회차 3 = 9.5 ✅**» 처럼 점수 표시로 쓰인 자리도 있다.
            # 상태는 칸 **맨 앞**에 적는 것이 이 표의 규약이라(§4) 앞의 굵게 표시만 벗기고 첫 글자를 본다.
            lead = state.strip().lstrip("*").strip()
            mark = lead[0] if lead[:1] in ("✅", "⛔", "🔄", "⬜") else ""
            prev = out.get(tid)
            if prev is None or rank.get(mark, 0) > rank.get(prev[1], 0):
                out[tid] = (n, mark, state.strip())
    return out


def lock_of(tid):
    """살아 있는 lock 이면 (SID, 나이(분)), 90분을 넘겼으면 (SID, 나이) + stale, 없으면 None."""
    p = os.path.join(CLAIMS, tid + ".lock")
    if not os.path.exists(p):
        return None
    try:
        txt = io.open(p, encoding="utf-8").read().strip().split()
        when = datetime.datetime.strptime(txt[0], "%Y-%m-%dT%H:%M:%SZ").replace(tzinfo=datetime.timezone.utc)
        sid = txt[1] if len(txt) > 1 else "(SID 없음)"
    except (OSError, ValueError, IndexError):
        return ("(못 읽음)", 0.0)
    age = (datetime.datetime.now(datetime.timezone.utc) - when).total_seconds() / 60.0
    return (sid, age)


SHALLOW_MIN = 50        # 이만큼도 안 되는 역사면 «그 SID 가 최근에 커밋했나» 를 판단하지 않는다(아래 T329)


def stale_but_alive(lock_age_min, sid_commit_age_min):
    """
    T329 — «시각으로는 죽었는데 임자는 살아 있는» lock 인가.

    90분 규약은 «시각» 하나만 보는데, 워커가 **일은 계속하면서 lock 파일 갱신만 잊는** 일이 잦다.
    그때 규약대로 «지났으니 뺏는다» 로 가면 **살아 있는 워커의 파일을 헤집는다** —
    `docs/claims/README.md` 가 «실사고 2건» 으로 적어 둔 그 꼴이다.

    «살아 있다» 의 잣대는 **90분 규약과 같은 자**를 쓴다: 그 SID 가 90분 안에 커밋했으면 살아 있다.
    lock 보다 최근이기만 하면 되는 것이 아니다 — lock 이 다섯 시간 전이고 커밋이 네 시간 전이면 둘 다 죽은 것이다.

    `sid_commit_age_min` 이 None 이면 **판단하지 않는다**(거짓으로 «죽었다» 고 말하지 않는다).
    """
    if lock_age_min is None or lock_age_min <= STALE_MIN:
        return False
    if sid_commit_age_min is None:
        return False
    return sid_commit_age_min <= STALE_MIN


def sid_commit_age(sid):
    """그 SID 가 **마지막으로 커밋한 지 몇 분**인가 — 모르면 `None`(T447).

    T329 가 세운 `stale_but_alive` 의 재료를 내는 자리다. 그때는 **요약 갈래 안에만** 있었고,
    그래서 «낡은 lock 인데 임자는 살아 있다» 를 **요약은 아는데 단일 조회(`task_state.py <ID>`)는 몰랐다** —
    그런데 워커가 **선점 직전에 실제로 부르는 것은 단일 조회**다(규약 절차 ①). 그 둘이 다른 말을 하고 있었다.

    ⚠ **모르면 `None`** — 얕은 클론(CI `fetch-depth` 없음)에서는 «그 SID 가 최근에 커밋했나» 를 물을 수가 없다.
    그때 «죽었다» 로 새면 매 런 거짓 경고가 난다(T329 가 적어 둔 그 자리).
    """
    try:
        cnt = int((_git(["rev-list", "--count", "HEAD"]).strip() or "0"))
    except ValueError:
        return None
    if cnt < SHALLOW_MIN:
        return None
    when, _subj = sid_last_commit(sid)
    if not when:
        return None
    try:
        t = datetime.datetime.fromisoformat(when)
    except ValueError:
        return None
    return (datetime.datetime.now(datetime.timezone.utc) - t).total_seconds() / 60.0


def sid_last_task(sid):
    """그 SID 의 **마지막 커밋 제목이 가리키는 작업 번호** — 못 찾으면 빈 글자 (T465).

    «임자가 살아 있다» 를 재는 자(<see :func:`sid_commit_age`>)는 그 SID 의 커밋을 **절 가림 없이** 본다.
    주인 자리 로컬 세션처럼 **한 세션이 여러 절을 잇달아 미는** 꼴에서는 그 커밋이 **딴 절**의 것일 수 있고,
    그러면 «살아 있다» 가 «이 자리로 돌아온다» 를 뜻하지 않는다(워커 P 의 물음 · 결정 1295).

    ⚠ **잣대를 바꾸지 않는다** — 그것까지 자가 가르려면 «그 세션이 이 절을 마쳤나» 를 알아야 하는데 그 앎이 없다.
    모르는 것을 가른 척하는 쪽이 안 뺏는 쪽보다 비싸다. 그래서 **재료만** 내고 가름은 사람에게 넘긴다.
    """
    _when, subj = sid_last_commit(sid)
    m = re.search(r"\bT(\d+)\b", subj)
    return ("T" + m.group(1)) if m else ""


def sid_last_commit(sid, limit=200, log=None):
    """그 SID 가 **제 손으로 민** 마지막 커밋 — `(시각, 제목)` · 없으면 `(None, "")` (T481).

    ⚑ **왜 `--grep` 을 안 쓰나** — `git log --grep` 은 제목이 아니라 커밋 **온몸**을 본다.
    그래서 «남이 그 SID 를 제 커밋 글에 적은 것» 까지 «그 세션이 커밋했다» 로 세어졌다.

    ⚠⚑ **하필 가장 나쁜 자리에서 틀렸다 — 스스로를 굳히는 꼴이다.** lock 이 낡아 갈수록
      그것을 **이야기하는 글**(진단 · 조율 · «저 절은 누구 것인가»)이 늘고 그 글마다 그 SID 가 적힌다
      ⇒ **낡을수록 «살아 있다» 로 보인다.** 2026-09-11 20:4X 실측: `T473`·`T475` 가
      «lock 101분 · 그러나 임자의 마지막 커밋이 84분 전 = 뺏지 마라» 를 찍었는데,
      그 84분짜리는 **남(워커 A)의 커밋**이고 그 SID 는 그 몸통의 «`T471.lock`(… · 살아 있다)» 한 줄이었다.
      임자의 진짜 마지막 커밋은 97분 전이라 90분 규약으로는 죽어 있었다.

    세는 법 — 우리 규약이 커밋 **제목** 끝에 `(sess-… · 워커 X)` 를 붙이게 한다(ROUTINE §1 6항)
      ⇒ «그 SID 가 민 커밋» = **제목에 그 SID 가 있는 커밋** 가운데 가장 최근 것.

    ⚠ **남는 한계** — 제목에 **남의 SID** 를 적는 커밋이 나오면 이 눈도 속는다(표본 0). 그리고
      최근 `limit` 개 밖은 «모름»(`None`)이다 — 90분 규약이 재는 창은 그 안에 든다.
    """
    if log is None:
        log = _git(["log", "-%d" % limit, "--format=%cI\t%s"])
    for line in (log or "").split("\n"):
        parts = line.split("\t", 1)
        if len(parts) != 2:
            continue
        when, subj = parts[0].strip(), parts[1]
        if sid and sid in subj:
            return when, subj
    return None, ""


def _git(args):
    try:
        return subprocess.run(["git"] + args, cwd=ROOT, stdout=subprocess.PIPE,
                              stderr=subprocess.DEVNULL, text=True, timeout=60).stdout
    except (OSError, subprocess.SubprocessError):
        return ""


# «작업ID 처럼 생긴 것» — 커밋 제목에서 T번호를 집는다. 꼬리에 글자가 붙은 갈래(T69-gear)는
# 이 표의 대상이 아니므로 위 HEAD 와 같은 잣대로 뺀다.
ID_IN_TEXT = re.compile(r"\bT\d{1,4}(?![\w-])")


def history_ids():
    """**커밋 제목**에 한 번이라도 나온 작업 ID 전부 (T415).

    왜 이력을 보나 — 번호 발급 규칙(«이미 쓰인 번호 중 가장 큰 것 +1»)이 **표**를 읽으면,
    누가 작업을 접으며 행을 지우는 순간 최대가 **내려가고** 다음 사람이 같은 번호를 다시 뽑는다.
    2026-09-10 에 실제로 났다: 워커 L 이 T409(라이선스)를 접으며 행을 지워 최대가 409 → 408 이 됐고,
    30분 뒤 워커 C 가 규칙 그대로 **다시 409** 를 발급했다(결정 1180 ⑥).
    **커밋 제목은 append-only 라 절대 안 줄어든다** — 그래서 여기를 읽는다.

    ⚠ 이 집합은 «발급된 번호» 보다 **넓다**(«T278 의 자로» 같은 참조도 들어온다).
      넓은 쪽으로 틀리는 것이 안전하다 — 번호를 **건너뛰는** 것은 값이 0 이고,
      번호가 **겹치는** 것은 두 워커가 같은 lock 을 서로 다른 일로 잡는 사고다.
    """
    out = _git(["log", "--format=%s"])
    return set(ID_IN_TEXT.findall(out))


def next_id(rows_all=None, hist=None):
    """다음 작업 번호 → (다음, 표 최대, 이력 최대). git 이 없으면 이력 최대는 None."""
    rows_all = row_ids_all() if rows_all is None else rows_all
    hist = history_ids() if hist is None else hist
    n = lambda s: max((int(t[1:]) for t in s), default=0)
    tmax, hmax = n(rows_all), (n(hist) if hist else None)
    return max(tmax, hmax or 0) + 1, tmax, hmax


def buried_ids(rows_all=None, hist=None):
    """**이력에는 있는데 표에 행이 없는** ID — 곧 «행이 지워졌다 = 그 번호가 되살아났다» (T415).

    이것이 발급 사고의 **유일한 이른 신호**다. 번호가 지워진 뒤 **다시 발급되기 전**에만 보이고,
    다시 발급되고 나면 표에는 행이 하나뿐이라 «번호 중복» 자에도 안 걸린다(T205 몫이 못 보는 칸).
    ⚠ «빠진 번호(구멍)를 센다» 로는 못 잡는다 — 재 봤다: 지금 표의 구멍 여덟(T92 · T333~T339)은
      **한 번도 발급된 적이 없고**(이력에 없다), 정작 T409 는 곧바로 재발급돼 구멍이 아니었다.
    """
    rows_all = row_ids_all() if rows_all is None else rows_all
    hist = history_ids() if hist is None else hist
    return sorted(hist - rows_all, key=lambda t: int(t[1:]))


def cmd_new_id():
    """선점 «직전» 한 줄 — 다음 번호를 규칙대로 뽑아 준다(T415).

    규칙에 **재는 길을 같이 준다**: «표에서 가장 큰 것을 눈으로 찾아라» 는 지시는
    행이 지워진 날 조용히 어긋난다(결정 1166 «조건이 차면 하라» 는 그 조건을 재는 길이 있을 때만 지시다).
    """
    rows_all = row_ids_all()
    hist = history_ids()
    nxt, tmax, hmax = next_id(rows_all, hist)
    print("다음 작업 번호 = **T%d**" % nxt)
    print("  · 표(docs/PROGRESS.md) 최대 = T%d" % tmax)
    print("  · 이력(커밋 제목) 최대 = %s" % ("T%d" % hmax if hmax is not None else "(이력에 번호가 없다 — git 이 없거나 아직 T번호 커밋이 없다)"))
    buried = buried_ids(rows_all, hist)
    if buried:
        print("⚠ 이력에는 있는데 **표에 행이 없는** 번호 %d개 — 행이 지워졌다(= 그 번호가 되살아난다):" % len(buried))
        print("   %s" % " ".join(buried))
        print("   접을 때는 행을 지우지 말고 **✂ 로 남겨 번호를 태운다**(T284·T296 의 꼴).")
    print("⚠ 이 수는 «지금» 의 답이다 — 같은 순간 남도 같은 답을 얻는다.")
    print("   같은 번호를 동시에 뽑는 갈래는 이 자가 못 막는다(2026-09-10 T414 가 그랬다) —")
    print("   그것을 가르는 것은 규약의 **push 순서**다(«push 가 먼저 성공한 쪽이 이긴다» · 늦게 민 쪽이 옮긴다).")
    return 0


# ── «코드 자취» 에서 주석·문자열을 벗긴다 (T29) ─────────────────────────────────────────
# 왜: `Rng.cs` 의 `/// T7 전투가 쓴다` · `Hud.cs` 의 `// T13 세이브` 같은 **앞으로 가리키는 주석**과,
#     `check_claim_scope.py` 의 `"T25.lock"` · `check_docs_intact.py` 의 `"| T2 | 무엇 |"` 같은 **자기 검사 픽스처 문자열**이
#     `git grep` 에 걸려 «코드가 이 번호를 가리킨다 — 잡지 마라» 를 냈다(2026-09-12 T2·T7·T9·T13·T14·T25 실측).
#     규약대로면 아무도 못 잡는다. 자취로 세는 것은 **코드 식별자 · 파일명 · 폴더명** 뿐이어야 한다.
# 어떻게: 파일을 통째로 읽어 언어별 주석·문자열 리터럴을 지운 «코드만 남은 글» 에서 번호를 찾는다.
#     경로(파일명·폴더명)에 번호가 있으면 내용과 무관하게 자취다.
#     문서·데이터 파일(.md · .json · .txt · .csv)의 **내용**은 코드가 아니라 세지 않는다(경로만 본다).
_C_LIKE = {".cs", ".js", ".mjs", ".cjs", ".ts", ".c", ".h", ".cpp", ".java", ".shader", ".cginc", ".hlsl", ".glsl", ".uxml", ".uss"}
_HASH_LIKE = {".py", ".sh", ".bash", ".yml", ".yaml", ".toml", ".cfg", ".ini", ".gitignore"}
_TEXT_ONLY = {".md", ".json", ".txt", ".csv", ".html", ".xml", ".svg", ".asmdef", ".meta", ".asset", ".unity", ".prefab", ".mat", ".uxml"}


def strip_noncode(text, ext):
    """주석·문자열 리터럴을 빈칸으로 바꾼 글을 돌려준다(줄 수는 그대로 — 줄바꿈은 남긴다).
    C 계열: `//` · `/* */` · " ' ` 문자열. 해시 계열(py·sh·yml): `#` · " ' 문자열 · 파이썬 삼중따옴표.
    모르는 확장자는 손대지 않는다(전부 코드로 본다)."""
    ext = ext.lower()
    if ext in _C_LIKE:
        line_c, block_c, quotes, triple = "//", ("/*", "*/"), "\"'`", False
    elif ext in _HASH_LIKE:
        line_c, block_c, quotes, triple = "#", None, "\"'", ext == ".py"
    else:
        return text
    out = []
    i, n = 0, len(text)
    while i < n:
        ch = text[i]
        two = text[i:i + 2]
        three = text[i:i + 3]
        if two == line_c or (line_c == "#" and ch == "#"):
            j = text.find("\n", i)
            if j < 0:
                j = n
            i = j                       # 줄바꿈은 아래 일반 갈래가 남긴다
            continue
        if block_c and two == block_c[0]:
            j = text.find(block_c[1], i + 2)
            j = n if j < 0 else j + 2
            out.append("\n" * text.count("\n", i, j))
            i = j
            continue
        if triple and three in ('"""', "'''"):
            j = text.find(three, i + 3)
            j = n if j < 0 else j + 3
            out.append("\n" * text.count("\n", i, j))
            i = j
            continue
        if ch in quotes:
            j = i + 1
            while j < n and text[j] != ch:
                if text[j] == "\\":
                    j += 1
                elif text[j] == "\n" and ch != "`":
                    break               # 닫히지 않은 따옴표(주석 안의 아포스트로피 등) — 줄 끝에서 끊는다
                j += 1
            j = min(j + 1, n)
            out.append("\n" * text.count("\n", i, j))
            i = j
            continue
        out.append(ch)
        i += 1
    return "".join(out)


def code_mentions(path, text, tid):
    """이 파일이 그 번호를 **코드로** 가리키는가 — 경로에 있으면 참 · 내용은 주석·문자열을 벗기고 본다."""
    pat = re.compile(r"(?<![A-Za-z0-9])" + re.escape(tid) + r"(?![0-9])")
    if pat.search(path.rsplit("/", 1)[-1]) or pat.search(path):
        return True
    ext = os.path.splitext(path)[1].lower()
    if ext in _TEXT_ONLY:
        return False
    return bool(pat.search(strip_noncode(text, ext)))


def footprint(tid):
    """그 작업 번호가 **코드에** 남긴 자취 — 파일 목록과 «제목이 그 번호로 시작하는» 커밋들.
    파일은 «코드 식별자·파일명·폴더명» 으로 가리키는 것만 센다 — 주석·문자열 리터럴·문서 내용은 뺀다(T29)."""
    # 우리가 쓴 것만 본다 — `Assets/` 통째로 훑으면 에셋 팩의 **이진 파일**(.psd 등)이 우연히 걸린다(실측).
    out = _git(["grep", "-l", "-I", "-E", tid + r"([^0-9]|$)", "--",
                "Assets/Scripts", "Assets/Tests", "tools", "docs/ref"])
    files = []
    for rel in (l for l in out.split("\n") if l):
        try:
            with io.open(os.path.join(ROOT, rel), encoding="utf-8", errors="replace") as f:
                text = f.read()
        except OSError:
            text = ""
        if code_mentions(rel, text, tid):
            files.append(rel)
    commits = []
    log = _git(["log", "--format=%h\t%cI\t%s", "-200"])
    pat = re.compile(r"^" + tid + r"(?![\w-])")
    for line in log.split("\n"):
        parts = line.split("\t")
        if len(parts) == 3 and pat.match(parts[2]):
            commits.append(parts)
    return files, commits


# «남이 이 절에 진단만 놓고 갔다» 를 알아보는 낱말 — 실제로 쓰인 제목에서 뽑았다(T446 실측).
#   «런 1070 빨강 진단 → T443 에 놓고 간다» · «런 1062 빨강의 뿌리를 T437 행에 적었다» · «T418 에 답만 놓는다».
HANDOVER_WORDS = ("진단", "뿌리를", "놓고 간다", "놓고 감", "답만 놓는다")
SID_RE = re.compile(r"sess-\d{3,4}-\w+")


def handovers(tid, holder=None, limit=200):
    """**남이 이 절에 놓고 간 진단** 커밋들 — [(해시, 시각, SID, 제목)] (T446).

    왜 있나 (2026-09-11 06:4X 실측) — 최근 200커밋에 이 꼴이 **5건**인데 그중 **네 건이 두 쌍**이었다:
    런 1062 를 워커 G(02:47)와 워커 L(02:51)이 **4분 차**로, 런 1037 을 두 사람이 **9분 차**로
    따로 진단했다. 까닭은 규약이다 — §1 이 «빨강이 그 회차의 첫 일» 이라 하고,
    lock 은 **작업**을 잡지 «남의 작업을 **진단하는** 일» 은 못 잡는다. 그래서
    «선점할 것이 없다» 회차의 워커가 **모두 같은 빨강을 본다**.

    ⚠ **겹침이 늘 낭비인 것은 아니다** — 워커 L 의 둘째 진단은 첫째에 없던 근거를 보탰다.
      그래서 이 자는 **막지 않고 알려만 준다**(결정 493): «이미 하나 있다» 를 둘째가 알고 나면
      보탤지 그만둘지는 그가 정한다. 지금 없는 것은 **아는 길**뿐이다.

    세는 법은 <see cref="handover_subject"/> 한 곳에 있다(제목 한 줄만 보는 순수 함수 · T468).

    ⚠⚑ **이 셈은 완전하지 않다 — 못 세는 꼴을 알고 쓴다**(T446 실측 · 자기 것에서 걸렸다).
      진단이 **다른 절의 커밋에 얹혀 오면** 이 눈에 안 잡힌다: 2026-09-11 06:4X 에 워커 A 가
      T443 행에 남긴 진단은 제목이 «**T444** ✅ — 인계 글이 …» 라 여기서 0 으로 센다.
      그래서 부르는 쪽이 <see cref="row_handover_hint"/> 로 **행 글도 같이** 본다 —
      한쪽만 믿으면 «없다» 를 «안 세어졌다» 와 못 가른다(결정 1251 의 그 자리다).
    """
    out = []
    log = _git(["log", "--format=%h\t%cI\t%s", "-%d" % limit])
    for line in log.split("\n"):
        parts = line.split("\t")
        if len(parts) != 3:
            continue
        h, when, subj = parts
        sid = handover_subject(subj, tid, holder)
        if sid is None:
            continue
        out.append((h, when[:16], sid, subj))
    return out


def handover_subject(subj, tid, holder=None):
    """커밋 **제목 한 줄**이 «남이 이 절에 놓고 간 진단» 인가 — 맞으면 SID 문자열, 아니면 `None` (T468).

    세는 법 — 제목에 ⓐ 그 번호가 들어 있고 ⓑ <see cref="HANDOVER_WORDS"/> 가 하나라도 있고
      ⓒ (`holder` 를 주면) **그 SID 가 아닌** 것.

    ⚑ **«제목이 그 번호로 시작하면 임자 것» 은 어림이고, 실측(SID)이 있으면 어림을 안 쓴다**(T468).
      원래 이 자리는 «`T462 …` 로 시작하면 임자 제 회차 커밋이니 뺀다» 였다. 그런데
      **놓고 가는 사람도 제목을 그렇게 쓴다** — 2026-09-11 18:4X 실측: 임자가 실제로 받아
      2회차를 민 진단(`e861f232` → `32b12b4c`)이 «놓고 간 진단» 목록에서 **통째로 빠져 있었다**.
      가릴 재료는 바로 아래 줄에 이미 있다(SID). **어림과 실측이 같은 자리에 있으면 실측이 이긴다.**

    ⚠ **넓힌 것은 «임자가 아님을 SID 로 아는» 자리 하나뿐이다** — `holder` 를 모르거나
      제목에 SID 가 아예 없으면 가릴 재료가 없으므로 **종전대로** 뺀다(그 꼴은 임자 제 회차
      커밋이 압도적이고, 여기서 잘못 세면 다음 사람이 «이미 누가 봤다» 로 읽고 안 본다).
    """
    if not re.search(r"\b" + tid + r"(?![\w-])", subj):
        return None
    if not any(w in subj for w in HANDOVER_WORDS):
        return None
    m = SID_RE.search(subj)
    sid = m.group(0) if m else None
    if holder and sid == holder:
        return None
    starts_with_tid = re.match(r"^" + tid + r"(?![\w-])", subj) is not None
    known_other = bool(holder) and bool(sid) and sid != holder
    if starts_with_tid and not known_other:
        return None
    return sid or "(SID 없음)"


def row_handover_hint(rowtext, holder=None):
    """그 **행 글 안에** 남이 놓고 간 진단이 몇 군데로 보이는가 — 거친 셈이다 (T446).

    <see cref="handovers"/> 가 커밋 제목만 보아 «다른 절 커밋에 얹혀 온 진단» 을 못 세므로,
    행 글에서 «진단» 이라는 낱말이 **SID·워커 표시와 같은 조각 안에** 있는 자리를 센다.
    ⚠ **거친 셈이라고 적어 두고 쓴다** — 임자 자신이 «진단» 이라 쓴 자리도 걸릴 수 있다.
      그래서 «N군데로 **보인다**» 로 찍지 «N개 있다» 로 안 찍는다(수를 단정하면 다음 사람이 그것을 믿는다).
    """
    n = 0
    for piece in re.split(r"‖|▸", rowtext or ""):
        if "진단" not in piece:
            continue
        sids = set(SID_RE.findall(piece))
        if sids:
            # SID 가 적혀 있으면 그것으로 가린다 — 임자 것뿐이면 남이 놓고 간 것이 아니다.
            if holder:
                sids.discard(holder)
            if sids:
                n += 1
            continue
        # ⚑ SID 가 **아예 없는** 조각에서만 «워커 X» 를 본다 — 워커 A 가 «[워커 A · 코드 0줄]» 로
        #   SID 없이 적은 실측 꼴을 놓치지 않으려는 갈래다(자기 검사에 그 줄이 있다).
        if re.search(r"워커\s*[A-Q]", piece):
            n += 1
    return n


CODE_DIRS = ("Assets/", "tools/")   # T160 — 이 아래를 건드린 커밋만 «코드를 만졌다» 로 센다(docs/ 만 만진 등재·판정 커밋은 안내다)


def commit_paths(h):
    """커밋 하나가 만진 경로 목록(T160)."""
    return [l for l in _git(["show", "--name-only", "--format=", h]).split("\n") if l]


def docs_only_verdict(pmark, lk, files, commits, paths_of):
    """T160 — «⬜ 행 · lock 없음 · 코드 자취 0 · 그 번호의 커밋이 전부 문서뿐» 이면 (True, 까닭). 아니면 (None, None) 로 다음 갈래에 넘긴다.
    순수 판정이라 자기 검사가 가짜 `paths_of` 로 잰다."""
    if pmark != "⬜" or lk or files or not commits:
        return None, None
    for h, when, subj in commits:
        if any(p.startswith(CODE_DIRS) for p in paths_of(h)):
            return None, None
    h, when, subj = commits[0]
    return True, "등재·문서 커밋뿐(코드 0곳 · 커밋 %d개) — 선점해도 된다 · 먼저 읽어라: %s(%s) «%s»" % (
        len(commits), h, when[:16], subj[:60])


def verdict(tid, heads, rows):
    """(잡아도 되나, 한 줄 판정). «잡아도 되나» 가 거짓이면 그 회차에 그 번호를 선점하지 않는다."""
    lk = lock_of(tid)
    if lk and lk[1] < STALE_MIN:
        return False, "남의 lock 이 살아 있다 — %s · %d분 전(90분 규약)" % (lk[0], lk[1])
    hn, hdone, _ = heads.get(tid, (0, False, ""))
    pn, pmark, ptext = rows.get(tid, (0, "", ""))
    if hdone or pmark == "✅":
        where = "ROUTINE 제목이" if hdone else "PROGRESS 상태가"
        return False, "**끝난 일이다**(%s ✅) — 잡지 마라" % where
    # T465 — **«뺏지 마라» 를 발자취보다 먼저 묻는다.**
    #   T447 이 이 갈래를 세울 때 아래 발자취 갈래 **뒤**에 놓았다. 그런데 **인수가 나는 절은 예외 없이 발자취가 있다**
    #   (누가 일을 하다 멈춘 자리라야 인수할 것이 있다) ⇒ 그 갈래는 **정작 필요한 자리에서 한 번도 안 섰다.**
    #   ⚠ 두 갈래 다 «잡지 마라»(False)라 **허락이 샌 적은 없다** — 샌 것은 **까닭**이다.
    #   그리고 사람은 까닭을 보고 움직인다: «먼저 읽어라» 는 읽고 나면 잡으라는 말로 읽히고, «뺏지 마라» 는 아니다.
    #   2026-09-11 한 시간에 인수 둘이 그 자리에서 났다(워커 P 의 T460 · 결정 1295 ⑥ / 내 T462 · 결정 1300 ⑧).
    #   ⚑ 이 자의 자기 검사(ⓛ)는 «발자취가 있으면 더 앞선 갈래가 먼저 막으므로 발자취 0 으로 재야 한다» 고
    #     **적어 두고 그 꼴을 피해 갔다** — 피해 갈 것이 아니라 그것이 결함이었다.
    if lk:   # 여기 오는 lk 는 전부 90분 초과다(살아 있는 lock 은 맨 위에서 돌아갔다)
        age = sid_commit_age(lk[0])
        if stale_but_alive(lk[1], age):
            # T465 — **어느 절을 밀고 있는지도 같이 말한다**(워커 P 가 결정 1295 에서 남긴 물음).
            #   한 세션이 여러 절을 잇달아 밀면 «그 SID 의 마지막 커밋» 은 **딴 절**의 것일 수 있고,
            #   그때 «살아 있다» 는 «이 절을 하고 있다» 와 다른 말이다. 잣대는 안 바꾼다(모르면 안 뺏는 쪽이 싸다) —
            #   대신 **읽는 사람이 스스로 가를 재료**를 준다. 자가 못 가르는 것을 가른 척하지 않는다(결정 1106 ②).
            other = sid_last_task(lk[0])
            where = ("" if not other or other == tid
                     else " ⚠ 다만 그 커밋은 **%s** 것이다 — 그 절을 읽고 «이 자리로 돌아올 사람인가» 는 네가 가른다" % other)
            return False, ("lock 시각은 %d분 전이라 죽었지만 **임자 %s 의 마지막 커밋이 %d분 전**이다 — "
                           "살아 있다 · 뺏지 마라(T329·T447·T465 · 그가 lock 갱신만 잊은 것이다)%s" % (lk[1], lk[0], age, where))
    files, commits = footprint(tid)
    # T160 — **등재만 된 ⬜ 작업은 «손댄 흔적» 이 아니다.** 남이 §2·PROGRESS 에 절과 행을 적어 넣은 «등재» 커밋은
    #   제목이 그 번호로 시작하므로 여기서 «이미 손댄 흔적 → 잡지 마라» 로 찍혔다(2026-09-14 10:4x T156 실측 · 코드 0곳 · lock 없음 · ⬜).
    #   지시서 3 이 «선점 직전 이 자가 0» 을 요구하니, 그 절은 규약상 **영원히 아무도 못 잡는** 자리가 됐다.
    #   문서만 만진 커밋(Assets/·tools/ 를 한 줄도 안 건드린 것)은 발자취가 아니라 안내다 — ⬜ 행 + lock 없음 + 코드 자취 0 이면 잡아도 된다.
    #   🔄 행(누가 하다 멈춘 자리)은 그대로 «먼저 읽어라» 다 — 인수 규약(T447·T465)은 안 건드린다.
    ok, why = docs_only_verdict(pmark, lk, files, commits, commit_paths)
    if ok is not None:
        return ok, why
    if commits:
        h, when, subj = commits[0]
        return False, "⚠ **이미 손댄 흔적** — 커밋 %s(%s) «%s» · 코드 %d곳. 먼저 읽어라" % (
            h, when[:16], subj[:60], len(files))
    if files:
        return False, "⚠ **코드가 이 번호를 %d곳에서 가리킨다** — 먼저 읽어라: %s" % (
            len(files), ", ".join(files[:3]))
    if lk:
        return True, "lock 이 %d분 지났다(90분 초과) — 인계해서 잡아도 된다" % lk[1]
    return True, "깨끗하다 — 선점해도 된다"


def mismatches(heads, rows):
    """«PROGRESS 는 닫혔는데 ROUTINE 제목은 열려 보인다» = 선점 덫. T161·T188 이 빠진 구덩이다.

    **닫힌 꼴은 둘이다(T210)** — ✅ 완료 · ⛔ 폐기·흡수. 제목에 필요한 표시도 각각 그것이다
    (폐기된 일에 ✅ 를 달면 «했다» 는 뜻이 되므로 ✅ 로 대신하지 않는다 · 이미 ✅ 면 그대로 둔다).
    """
    bad = []
    for tid, (pn, mark, ptext) in sorted(rows.items(), key=lambda kv: int(kv[0][1:])):
        if mark not in ("✅", "⛔"):
            continue
        h = heads.get(tid)
        if h is None:
            continue        # ROUTINE §2 에 없는 작업(옛 표만 있는 것)은 선점 대상이 아니다
        if h[1] == "":
            bad.append((tid, h[0], pn, mark, ptext[:70]))
    return bad


# 행 글이 «나는 그 lock 을 쥐고 있다» 고 말하는 꼴 — 실제로 쓰인 말에서 뽑았다(T453 실측 둘 다 «쥔 채»).
HOLD_WORDS = ("쥔 채", "쥐고 있다", "lock 유지")


def says_holds_lock(tid, rowtext):
    """그 행이 «이 작업의 lock 을 지금 쥐고 있다» 고 말하는가 (T453).

    ⚑ **칸 «머리» 만 본다**(T444 가 세운 규약 — 지금 상태는 칸 머리에 적는다). 행 전체를 보면
      뒤에 붙은 **지난 회차 기록**의 낱말을 물어 눈이 먼다 — 실제로 첫 판이 그랬다: T452 의 행은
      머리에 «… `T452.lock` 쥔 채» 인데 뒤쪽 이력에 «반납» 이 있어 이 자가 **자기 표본을 놓쳤다**.
    «반납» 이 **머리에** 있으면 아니다 — «쥔 채 … 뒤에 반납» 은 «그때 쥐고 있었다» 이지 «지금» 이 아니다.
    닫힌 줄(✅·⛔)은 부르는 쪽이 먼저 거른다.
    """
    head = re.split(r"‖|▸", rowtext or "")[0]
    txt = head
    if "반납" in txt:
        return False
    if any(w in txt for w in HOLD_WORDS):
        return True
    return ("`%s.lock`" % tid) in txt


def lock_claim_gap(rows):
    """**행은 «쥔 채» 라는데 그 lock 파일이 없다** — [(작업, 상태 앞머리)] (T453).

    왜 있나 (2026-09-11 실측 · 표본 둘 · 서로 다른 워커) — «1회차 push» 커밋이 제 lock 파일을
    **같이 지우면서** 같은 커밋의 행에는 «`TNNN.lock` 쥔 채» 를 적었다:
      `7a113203`(09:33 · T448) · `be534acf`(12:31 · T452).
    그 줄이 **🔄** 라 어긋남이 조용히 산다 — 행만 읽는 워커는 «임자가 있다» 로 비켜 가고,
    `docs/claims/` 만 보는 워커는 «비었다» 로 읽는다. 게다가 §1(결정 429)은 반납을
    «CI 가 그 커밋을 한 번은 돈 뒤» 로 못 박았으니 **«확인 전 + lock 없음» 은 규약대로면 없어야 할 상태**다.

    ⚠ **막지 않는다**(결정 493) — 이것은 조율 결함이지 빌드 결함이 아니다. 부르는 쪽도 rc 를 안 바꾼다.
    ⚠ **닫힌 줄(✅·⛔)은 안 본다** — 거기 남은 «쥔 채» 는 회차 이력이지 «지금» 이 아니다.
    ⚠ **lock 을 대신 만들어 주지 않는다** — 남의 상태를 제3자가 정하는 일이 된다. 알리고 만다.
    """
    out = []
    for tid, (pn, mark, ptext) in sorted(rows.items(), key=lambda kv: int(kv[0][1:])):
        if mark != "🔄":
            continue
        if not says_holds_lock(tid, ptext):
            continue
        if lock_of(tid) is None:
            out.append((tid, ptext[:70]))
    return out


def task_commit_age(tid, log=None):
    """그 번호로 시작하는 **마지막 커밋이 몇 분 전**인가 — 없거나 얕은 클론이면 `None`(T164 · `sid_commit_age` 와 같은 조심).
    커밋 «제목» 만 본다(`footprint` 와 같은 자) — 남이 제 커밋 글에 그 번호를 적은 것은 안 센다."""
    if log is None:
        try:
            cnt = int((_git(["rev-list", "--count", "HEAD"]).strip() or "0"))
        except ValueError:
            return None
        if cnt < SHALLOW_MIN:
            return None
        log = _git(["log", "--format=%h\t%cI\t%s", "-400"])
    pat = re.compile(r"^" + tid + r"(?![\w-])")
    for line in log.split("\n"):
        parts = line.split("\t")
        if len(parts) == 3 and pat.match(parts[2]):
            try:
                t = datetime.datetime.fromisoformat(parts[1])
            except ValueError:
                return None
            return (datetime.datetime.now(datetime.timezone.utc) - t).total_seconds() / 60.0
    return None


def cmd_check(heads, rows, dups=None, commit_age=None):
    rc = 0
    # 마지막 «✓ 요약» 줄에 한 번 더 실을 참고 사항들 — T231.
    #    까닭: 이 자의 «(참고 · 실패 아님)» 줄은 실패가 아니라서 종료 코드에 안 잡히고,
    #    워커들은 회차마다 출력을 `| tail -1` 로 자르거나 `>/dev/null` 로 버리고 종료 코드만 본다.
    #    그래서 갈래 ⓒ 가 **T223 을 이름으로 찍고 있었는데 아무도 못 봤고**, 닫힌 그 작업이
    #    표에서 «열린 급한 일» 로 읽혀 한 워커가 회차를 통째로 썼다(결정 640).
    #    막는 자로 올리지는 않는다 — 조율 결함은 알리기만 한다(결정 493). 대신 **끝줄에도 실어** 눈에 걸리게 한다.
    notes = []
    # ⓐ 한 번호가 두 작업을 가리키는가 (T205) — `docs/claims/README.md` 의 «한 번호는 한 작업만» 규칙.
    #    이것이 남으면 `T204.lock` 이 «어느 일» 인지 못 가르고, 두 워커가 같은 파일을 반대로 민다.
    if dups:
        rc = 1
        print("⛔ **한 번호가 여러 작업을 가리킨다** — `docs/claims/README.md`: «한 번호는 한 작업만 가리킨다».")
        print("   그대로 두면 그 번호의 lock 이 어느 일인지 못 가르고, 선점이 겹친다(오늘만 T189·T190·T204 세 번).")
        print("   고침: **늦게 등재된 쪽**을 다음 빈 번호로 옮긴다(제목·PROGRESS 행·본문 참조 함께).")
        for tid, places in sorted(dups.items(), key=lambda kv: int(kv[0][1:])):
            print("  · %s 가 %d곳:" % (tid, len(places)))
            for n, title in places:
                print("      ROUTINE.md:%d  «%s»" % (n, title[:80]))

    # ⓑ 제목은 있는데 PROGRESS 행이 없다 — **실패로 세지 않는다**(등재가 진행 중인 자리가 정상적으로 이 꼴이다).
    #    다만 그 사이에는 `check_task_rows` 가 아무것도 못 보므로 알려는 둔다(워커 A 의 17:11 보고가 그 자리다).
    #    ⚠ «행이 없다» 와 «접힌 행만 있다» 는 다른 일이다(T206) — 접힌 줄(✂·♻)은 «다른 번호로 옮겼다 · 취소됐다» 는
    #    **정상적으로 끝난 꼴**이라 채울 것이 없고, 진짜로 채워야 하는 것은 «행이 아예 없는» 쪽이다.
    #    둘을 한 목록에 섞으면 다음 워커가 이미 닫힌 T172·T189 를 «등재 중» 으로 읽고 손대러 간다.
    orphan = sorted(set(heads) - set(rows), key=lambda t: int(t[1:]))
    seen = row_ids_all()
    missing = [t for t in orphan if t not in seen]
    folded = [t for t in orphan if t in seen]
    if missing:
        print("· (참고 · 실패 아님) ROUTINE §2 제목은 있는데 PROGRESS 표에 **행이 아예 없는** 작업: %s" % " ".join(missing))
        notes.append("표에 행이 없는 작업 %s" % " ".join(missing))
        print("  등재 중이면 곧 채워진다. 오래 남아 있으면 그 사이 `check_task_rows` 가 그 작업을 못 본다(T96 이 그 꼴이었다).")
    if folded:
        print("· (참고 · 손댈 것 없음) 표에 **접힌 행(✂·♻)만** 있는 작업: %s — 다른 번호로 옮겼거나 취소된 자리다." % " ".join(folded))
        notes.append("접힌 행만 있는 작업 %s(손댈 것 없음)" % " ".join(folded))

    # ⓒ 상태 칸이 네 표시(✅ ⛔ 🔄 ⬜) 중 무엇으로도 **시작하지 않는** 행 — 어떤 자도 그 작업의 상태를 못 읽는다.
    #    실패로는 안 센다(표 규약을 어긴 것이지 일이 잘못된 것은 아니다) — 다만 그 행은 이 자와 check_task_rows 의 눈 밖이다.
    blind = sorted([t for t, (n, m, s) in rows.items() if m == ""], key=lambda t: int(t[1:]))
    if blind:
        print("· (참고 · 실패 아님) PROGRESS 상태 칸이 ✅·⛔·🔄·⬜ 중 무엇으로도 시작하지 않는 작업: %s" % " ".join(blind))
        notes.append("상태 칸이 표시로 시작 안 하는 작업 %s" % " ".join(blind))
        print("  그 행은 이 자도 `check_task_rows` 도 상태를 못 읽는다 — 칸 맨 앞에 표시를 하나 붙여 주면 된다(§4 규약).")

    # ⓙ **ⓑ 의 반대 방향 — «표에 행은 있는데 ROUTINE §2 에 제목이 없다»** (T466 · 검수 Q · 2026-09-11 실측).
    #    ⓑ 는 «제목은 있는데 행이 없다» 를 본다. 그 반대는 여태 **어느 자도 안 봤고**, `task_state <ID>` 만
    #    «(ROUTINE §2 에 없다)» 로 혼잣말을 한다 — 곧 그 ID 를 **이미 아는 사람**에게만 보인다.
    #    왜 값이 있나: **§2 는 일감을 고르는 워커가 훑는 자리**다(T419 — «표에만 적으면 §2 를 보고 일을 고르는
    #    사람이 못 찾는다»). 행만 있고 제목이 없으면 그 절은 **아무도 안 고른 채** 놓인다.
    #    ⚠ **열린 것(⬜·🔄)만 센다** — 실측(2026-09-11 17:0X): 제목 없는 행 **22개 중 20개가 이미 닫힌 것**이고
    #    닫힌 절의 제목은 아무도 안 찾으므로 세면 노이즈 20 에 신호 2 가 된다. 열린 둘(T463·T464)은 **그 회차에 난 것**이었다
    #    — 곧 이 구멍은 «쌓인 빚» 이 아니라 **갓 등재한 절에서 매번 새로 나는 꼴**이다.
    #    ⚠ **막지 않는다** — 조율 결함은 알리기만 한다(결정 493·627). notes 에 실어 끝줄에도 남긴다(T231 · 결정 641).
    headless = sorted([t for t, (n, m, s) in rows.items() if m in ("⬜", "🔄") and t not in heads],
                      key=lambda t: int(t[1:]))
    if headless:
        print("· (참고 · 실패 아님) PROGRESS 에 **열린 행**은 있는데 ROUTINE §2 에 제목이 없는 작업: %s" % " ".join(headless))
        notes.append("§2 제목이 없는 열린 작업 %s" % " ".join(headless))
        print("  §2 를 훑어 일감을 고르는 워커에게 그 절은 **없는 것**이다(T419). 고침: `docs/ROUTINE.md` §2 에 «### %s — …» 한 절을 세운다."
              % headless[0])

    # ⓖ **«⬜ 대기» 인데 그 번호의 lock 이 살아 있다** — 오늘 실제로 났다(T238 · 결정 653):
    #    `T233` 행이 «⬜ 대기 — 선점 안 됨» 인 채로 `T233.lock`(10:37)이 살아 있었고 **고침은 이미 push** 돼 있었다.
    #    이 꼴이 다른 어긋남보다 비싼 까닭은 하나다 — **⬜ 는 일감을 고르는 워커가 «정확히 그것만» 훑는 표시**라
    #    «남이 하는 중» 으로 읽혀 지나칠 여지가 없고 **곧장 중복 착수**로 간다(결정 506 이 값을 치른 그 사고).
    #    ⚠ **죽은 lock(90분 초과)은 안 찍는다** — 그 자리는 규약상 «잡아도 되는» 자리라 찍으면 거짓 경고가 된다.
    #    ⚠ **막지 않는다** — 조율 결함은 알리기만 한다(결정 493·627). 대신 notes 에 실어 끝줄에도 남긴다(T231).
    trap = []
    for tid, (n_, mark, _txt) in rows.items():
        if mark != "⬜":
            continue
        lk = lock_of(tid)
        if lk and lk[1] < STALE_MIN:
            trap.append((tid, n_, lk[0], lk[1]))
    if trap:
        trap.sort(key=lambda t: int(t[0][1:]))
        print("· (참고 · 실패 아님) **«⬜ 대기» 인데 lock 이 살아 있는 작업** — 잡으면 남의 일을 두 번 한다:")
        for tid, n_, sid, age in trap:
            print("  · %-5s PROGRESS.md:%d  ↔  docs/claims/%s.lock  %s · %d분 전" % (tid, n_, tid, sid, age))
        print("  고침: 임자가 상태 칸을 🔄 로 올린다(또는 일을 접었으면 lock 을 지운다).")
        notes.append("⬜ 인데 lock 살아 있음 %s" % " ".join(t[0] for t in trap))

    # ⓚ **«🔄 진행» 인데 lock 이 없고 그 번호의 마지막 커밋이 90분보다 오래됐다** (T164 · T33 12회차 실측).
    #    세션이 1회차만 하고 죽으면 행은 🔄 인 채 남고, `task_state <ID>` 는 🔄 를 «먼저 읽어라» 로 거르므로 그 절은
    #    워커들의 «선점할 것 없음» 목록에 **후보로도 안 오른다** — T132 가 그렇게 11시간 30분을 숨었다.
    #    ⓖ 가 «⬜ 인데 lock 있음» 을 보니 그 반대 «🔄 인데 lock 없음 + 오래됨» 을 여기 짝으로 둔다.
    #    ⚠ 막지 않는다(결정 493) — 그 행을 이어 잡을지는 사람이 정한다. 방금 반납한 행(커밋 90분 안)은 안 찍는다.
    #    ⚠ 얕은 클론(커밋 나이를 못 잰다 · None)에서는 판단하지 않는다 — 거짓 경고로 매 런 울지 않게(sid_commit_age 와 같은 조심).
    age_of = commit_age or task_commit_age
    idle = []
    for tid, (n_, mark, _txt) in rows.items():
        if mark != "🔄" or lock_of(tid):
            continue
        age = age_of(tid)
        if age is not None and age > STALE_MIN:
            idle.append((tid, n_, age))
    if idle:
        idle.sort(key=lambda t: int(t[0][1:]))
        print("· (참고 · 실패 아님) **«🔄 진행» 인데 lock 이 없고 마지막 커밋이 90분을 넘긴 작업** — 임자가 떠난 자리일 수 있다(T164):")
        for tid, n_, age in idle:
            print("  · %-5s PROGRESS.md:%d  lock 없음 · 그 번호의 마지막 커밋 %d분 전" % (tid, n_, age))
        print("  그 절의 마지막 회차 기록을 읽고 남은 몫이 있으면 이어 잡는다(`task_state <ID>` 의 «먼저 읽어라» 가 그 뜻이다) ·"
              " 끝난 것이면 임자가 ✅ 를 단다.")
        notes.append("🔄 인데 lock 없음·오래됨 %s" % " ".join(t[0] for t in idle))

    # ⓗ-2 **그 반대 방향** — «🔄 이고 행은 «쥔 채» 라는데 lock 파일이 없다» (T453 · 2026-09-11 실측 둘).
    #     ⓗ 가 «안 잡았다는데 lock 이 있다» 를 보니, 짝이 되는 «쥐었다는데 lock 이 없다» 를 여기 같이 둔다.
    #     같은 자리에 두는 까닭: 둘 다 «행 ↔ claims 어긋남» 한 가지이고, 읽는 사람이 한 곳에서 본다.
    gap = lock_claim_gap(rows)
    if gap:
        print("· (참고 · 실패 아님) **행은 «lock 쥔 채» 라는데 그 lock 파일이 없는 작업** — 행과 `docs/claims/` 가 어긋난다:")
        for tid, head in gap:
            print("  · %-5s  «%s…»" % (tid, head))
        print("  실측(T453): «1회차 push» 커밋이 제 lock 을 **같이 지우면서** 행에는 «쥔 채» 를 적은 자리가 오늘 둘이다"
              "(`7a113203` T448 · `be534acf` T452 · 서로 다른 워커).")
        print("  고침: 임자가 고른다 — **확인이 남았으면 lock 을 다시 잡고**(반납은 CI 가 그 커밋을 한 번 돈 뒤다 · 결정 429),"
              " 일부러 반납한 것이면 **행 글의 «쥔 채» 를 지운다**.")
        notes.append("«쥔 채» 인데 lock 없음 %s" % " ".join(t[0] for t in gap))

    # ⓗ **lock 시각이 «미래» 다** — 90분 규약의 셈이 통째로 밀린다 (T294 · 검수 Q · 2026-09-09 실측).
    #    실측: `T288-9.lock` 이 07:35 인데 그 파일을 담은 커밋은 **07:03** 이고(+31분),
    #          `T291.lock` 이 07:28 인데 커밋은 **06:57** 이다(+31분). 두 세션이 같은 폭으로 어긋났으니
    #          컨테이너 시계가 아니라 **적는 방식**이 그런 것이다(커밋 시각들은 서로 맞물린다).
    #    왜 나쁜가 — 90분은 «죽은 세션의 자리를 되찾는» 유일한 장치인데, 시각이 앞서 적히면
    #      ⓐ 그 lock 은 실제로 **90 + N 분**을 산다(되찾기가 그만큼 늦다)
    #      ⓑ «90분 지났나» 를 재는 쪽은 **음수 나이**를 받고, 그것은 어떤 셈에서도 «방금 잡았다» 로 읽힌다
    #    ⚠ 막지 않는다(결정 493·627 · 조율 결함) — notes 에 실어 끝줄에만 남긴다.
    #    ⚠ 2분은 봐준다 — 컨테이너마다 시계가 조금씩 다르고, 그 폭으로는 위 둘 중 어느 것도 안 일어난다.
    future = []
    if os.path.isdir(CLAIMS):
        now = datetime.datetime.now(datetime.timezone.utc)
        for name in sorted(os.listdir(CLAIMS)):
            if not name.endswith(".lock"):
                continue
            lk = lock_of(name[:-5])
            if lk and lk[1] < -2:
                future.append((name[:-5], lk[0], -lk[1]))
    if future:
        print("· (참고 · 실패 아님) **lock 시각이 «미래» 로 적힌 작업** — 90분 규약의 셈이 그만큼 밀린다:")
        for tid, sid, ahead in future:
            print("  · %-7s docs/claims/%s.lock  %s · **%d분 뒤** 시각이 적혀 있다" % (tid, tid, sid, ahead))
        print("  왜 나쁜가: ⓐ 그 lock 이 90분이 아니라 90+N 분을 산다(죽은 자리 되찾기가 늦다)")
        print("             ⓑ «90분 지났나» 를 재는 쪽은 음수 나이를 받고, 그것은 늘 «방금 잡았다» 로 읽힌다")
        print("  고침: 갱신할 때 `date -u +%Y-%m-%dT%H:%M:%SZ` 가 준 값을 그대로 적는다(앞당겨 적지 않는다).")
        notes.append("lock 시각이 미래 %s" % " ".join(t[0] for t in future))

    # ⓘ **«90분 지났다» 를 «잡아도 된다» 로 읽으면 안 되는 자리** — 임자가 lock 갱신만 잊고 일은 하고 있다 (T329 · 2026-09-09 14:2X 실측).
    #    실측(그 순간 동시에 둘): `T320-b.lock` 12:38(106분 전)인데 그 SID 의 마지막 커밋은 **13:43**(41분 전) ·
    #                            `T322.lock`   12:27(117분 전)인데 마지막 커밋은 **13:28**(56분 전).
    #    ⚠ 이 자의 ⓖ 갈래는 «죽은 lock 은 안 찍는다 — 규약상 «잡아도 되는» 자리라 찍으면 거짓 경고» 라고 **일부러** 적혀 있는데,
    #      지금 그 전제가 두 자리에서 거짓이다. ⓗ(«미래로 적힌 lock»)의 **대칭**이다 —
    #      그쪽은 시각이 앞서 적혀 lock 이 **너무 오래 살고**, 이쪽은 갱신을 잊어 **너무 일찍 죽은 것으로 읽힌다**.
    #    ⚠ **남의 lock 시각을 자가 고쳐 주지 않는다** — 그것은 «살아 있음» 을 위조하는 것이다. 사람에게 알리기만 한다.
    #    ⚠ **CI 에서는 조용하다** — `actions/checkout@v4` 가 `fetch-depth` 없이 도는 잡은 역사가 한 판뿐이라
    #      «그 SID 가 최근에 커밋했나» 를 물을 수가 없다. 그때는 아무 말도 안 한다(모르면 «죽었다» 고 하지 않는다).
    #      ⇒ 이 갈래가 값을 하는 자리는 **lock 을 뺏을지 말지 정하는 워커의 터미널**이고, 거기서는 역사가 있다.
    #      («CI 에서도 보이게» 하려고 fetch-depth 0 을 켜지 마라 — 이 참고 한 줄 값이 매 런 전체 클론 값보다 싸지 않다.)
    #    ⚠ 막지 않는다(결정 493·627 · 조율 결함) — notes 에 실어 끝줄에만 남긴다.
    alive = []
    if os.path.isdir(CLAIMS) and _git(["rev-list", "--count", "HEAD"]).strip().isdigit() \
       and int(_git(["rev-list", "--count", "HEAD"]).strip() or 0) >= SHALLOW_MIN:
        for name in sorted(os.listdir(CLAIMS)):
            if not name.endswith(".lock"):
                continue
            lk = lock_of(name[:-5])
            if not lk or lk[1] <= STALE_MIN:
                continue
            when = _git(["log", "-1", "--format=%cI", "--fixed-strings", "--grep", lk[0]]).strip()
            if not when:
                continue
            try:
                t = datetime.datetime.fromisoformat(when)
            except ValueError:
                continue
            age = (datetime.datetime.now(datetime.timezone.utc) - t).total_seconds() / 60.0
            if stale_but_alive(lk[1], age):
                alive.append((name[:-5], lk[0], lk[1], age))
    if alive:
        print("· (참고 · 실패 아님) **lock 시각은 90분을 넘겼는데 임자가 살아 있는 작업** — 규약대로 뺏으면 남의 일을 헤집는다:")
        for tid, sid, lock_age, commit_age in alive:
            print("  · %-8s docs/claims/%s.lock 은 %d분 전인데  %s 의 마지막 커밋은 **%d분 전**이다"
                  % (tid, tid, lock_age, sid, commit_age))
        print("  잡기 전에: 그 SID 의 최근 커밋을 읽어라(`git log --grep <SID>`) — 그 절을 아직 밀고 있으면 다른 일을 잡는다.")
        print("  임자가 할 것: 90분 전에 `date -u +%Y-%m-%dT%H:%M:%SZ` 로 갱신해 push — 그것이 «살아 있다» 는 유일한 신호다(README).")
        notes.append("lock 은 낡았는데 임자는 살아 있음 %s" % " ".join(t[0] for t in alive))

    # T415 — «이력에는 있는데 표에 행이 없는» 번호 = 행이 지워졌다 = 그 번호가 되살아난다.
    # ⚠ **알리기만 한다(rc 를 안 건드린다)** — 이 자는 막는 게이트이고, 여기 쓰이는 재료는
    #   «커밋 제목의 낱말» 이라 남의 push 를 막을 만큼 단단하지 않다(결정 493 의 자리).
    #   그리고 지금 이 수는 0 이라, 울면 그때가 진짜다.
    buried = buried_ids(rows_all=row_ids_all())
    if buried:
        print("· (참고 · 실패 아님) **이력에는 있는데 표에 행이 없는 번호** — 행이 지워져 그 번호가 «안 쓰인 것» 이 됐다:")
        print("  · %s" % " ".join(buried))
        print("  왜 나쁜가: 발급 규칙이 «가장 큰 것 +1» 이라 다음 사람이 **같은 번호를 다시 뽑는다**"
              " — 그러면 `T<번호>.lock` 이 «어느 일» 인지 못 가른다(2026-09-10 T409 가 그랬다 · 결정 1180 ⑥).")
        print("  고침: 접을 때 행을 지우지 말고 **✂ 로 남겨 번호를 태운다**(T284·T296 의 꼴). 발급은 `--new-id` 로.")
        notes.append("이력에만 있고 표에 행이 없는 번호 %s" % " ".join(buried))

    bad = mismatches(heads, rows)
    if not bad:
        if rc == 0:
            # T231 — 참고 사항을 **끝줄에도** 싣는다(`tail -1` 만 봐도 보이게). 종료 코드는 그대로 0 이다.
            print("✓ task_state: 번호 중복 0 · ROUTINE §2 제목과 PROGRESS 상태가 어긋나는 작업 0개 (제목 %d · 표 %d)%s"
                  % (len(heads), len(rows), (" · ⚠ 참고 %d건 — %s(위 줄에 자세히)" % (len(notes), " · ".join(notes))) if notes else ""))
        return rc
    if notes:
        # 실패로 끝나는 길에서도 참고 사항이 tail 에 남게 한다(빨강만 보고 나가는 회차가 더 흔하다).
        print("⚠ 참고 %d건 — %s" % (len(notes), " · ".join(notes)))
    print("⛔ **선점 덫** — PROGRESS 는 닫혔는데(✅ 완료 · ⛔ 폐기·흡수) ROUTINE §2 제목에는 표시가 없다.")
    print("   다음 워커는 이것을 «열린 일» 로 읽고 한 회차를 통째로 버린다(T161·T188 이 그랬다).")
    print("   고침: `docs/ROUTINE.md` 그 제목 줄의 ID 뒤에 **표에 적힌 그 표시**를 붙인다(✅ 는 ✅ · ⛔ 는 ⛔).")
    for tid, hn, pn, mark, ptext in bad:
        print("  · %-5s %s  ROUTINE.md:%d  ↔  PROGRESS.md:%d  «%s»" % (tid, mark, hn, pn, ptext))
    return 1


def cmd_list(heads, rows):
    for tid in sorted(set(heads) | set(rows), key=lambda t: int(t[1:])):
        ok, why = verdict(tid, heads, rows)
        print("%-5s %s %s" % (tid, "잡아도 됨" if ok else "잡지 마라 ", why))
    return 0


def cmd_one(tid, heads, rows):
    hn, hdone, htext = heads.get(tid, (0, False, "(ROUTINE §2 에 없다)"))
    pn, pmark, ptext = rows.get(tid, (0, "", "(PROGRESS 표에 없다)"))
    lk = lock_of(tid)
    files, commits = footprint(tid)
    print("== %s ==" % tid)
    print("  ROUTINE §2  : %s" % (htext[:100] or "(없다)"))      # 제목 원문에 이미 ✅ 가 들어 있다
    print("  PROGRESS 상태: %s" % (ptext[:100] or "(없다)"))
    # T447 — «90분 초과» 뒤에 **임자가 살아 있는지**까지 붙인다. 이 줄만 읽고 뺏는 손이 있다.
    if lk and lk[1] >= STALE_MIN:
        _age = sid_commit_age(lk[0])
        _tail = (" (90분 초과 — 그러나 **임자의 마지막 커밋이 %d분 전**이다 = 살아 있다 · 뺏지 마라 · T329·T447)" % _age
                 if stale_but_alive(lk[1], _age)
                 else " (90분 초과 = 죽은 lock)")
    else:
        _tail = ""
    print("  lock        : %s" % ("%s · %d분 전%s" % (lk[0], lk[1], _tail) if lk else "없음"))
    print("  코드 자취    : %d곳%s" % (len(files), (" — " + ", ".join(files[:5])) if files else ""))
    for h, when, subj in commits[:3]:
        print("  커밋        : %s %s %s" % (h, when[:16], subj[:70]))
    # T446 — **남이 이미 놓고 간 진단**. 이 줄이 없어서 최근 빨강 두 건을 워커 둘이 4분·9분 차로
    #   따로 진단했다. 막지 않고 알려만 준다 — 읽고 보탤지 그만둘지는 보는 사람이 정한다(결정 493).
    # T453 — 행이 «쥔 채» 라는데 lock 파일이 없으면 **선점 직전에** 알려 준다(§0.4 가 이 자를 돌리라 한 자리다).
    #   막지 않는다 — 잡아도 되는지의 판정은 아래 `verdict` 가 그대로 낸다(결정 493).
    if pmark == "🔄" and lk is None and says_holds_lock(tid, ptext):
        print("  ⚠ 행은 «lock 쥔 채» 라는데 **그 lock 파일이 없다** — 행과 `docs/claims/` 가 어긋난다(T453).")
        print("      임자가 고른다: 확인이 남았으면 lock 을 다시 잡고(결정 429), 일부러 반납한 것이면 행의 «쥔 채» 를 지운다.")
    holder = lk[0] if lk else None
    hos = handovers(tid, holder=holder)
    hint = row_handover_hint(ptext, holder)
    if hos or hint:
        print("  ⚠ 놓고 간 진단: 커밋 **%d개**%s — 여기에 또 내기 전에 먼저 읽어라(T446)"
              % (len(hos), (" · 행 글에도 **%d군데**로 보인다(거친 셈)" % hint) if hint else ""))
        for h, when, sid, subj in hos[:3]:
            print("      · %s %s %s %s" % (h, when, sid, subj[:62]))
        if hint and not hos:
            # ⚠ 단정하지 않는다 — 이 거친 셈은 **자기 절에서 먼저 헛짚었다**(T446 실측: 등재 글의 «워커 L» 을 물었다).
            print("      (커밋 제목으로는 0 이다 — 남의 절 커밋에 **얹혀 왔거나**, 이 거친 셈이 **헛짚었을** 수 있다 · 행을 읽어 가려라)")
        print("      보탤 것이 있으면 내고, 없으면 내지 않는다 — 겹친 진단이 값진 적도 있다(결정 1253 ②).")
    ok, why = verdict(tid, heads, rows)
    print("  → %s: %s" % ("잡아도 된다" if ok else "잡지 마라", why))
    return 0 if ok else 1


def self_test():
    """오늘 실제로 난 두 사고(T161 · T188)를 자가 잡는지 본다 — «늘 초록인 자» 와 «잡는 자» 를 가른다."""
    import tempfile
    import shutil
    tmp = tempfile.mkdtemp(prefix="task_state_selftest_")
    try:
        r = os.path.join(tmp, "ROUTINE.md")
        p = os.path.join(tmp, "PROGRESS.md")
        # ⓐ T161 꼴 — PROGRESS 는 ✅ 인데 제목에 ✅ 가 없다 → 잡아야 한다.
        # ⚠ «쓰인 적 없는 번호» 를 **글자 그대로** 적으면 안 된다 — 이 파일 자체가 `git grep` 에 걸려
        # «코드 자취가 있다» 로 판정되고 자기 검사가 스스로를 깬다(실제로 그랬다). 그래서 숫자로 만든다.
        free = "T%d" % 9000
        io.open(r, "w", encoding="utf-8").write(
            "### T161 — 장비 이름을 바꾼다 (주인 …)\n### %s — 아직 아무도 안 한 일\n" % free)
        io.open(p, "w", encoding="utf-8").write(
            "| ID | 작업 | 상태 | SID |\n| T161 | 장비 이름 | ✅ **완료 · CI 확인 끝** | 워커 L |\n"
            "| %s | 새 일 | ⬜ 대기 | |\n" % free)
        heads, rows = routine_heads(r), progress_rows(p)
        bad = mismatches(heads, rows)
        if [b[0] for b in bad] != ["T161"]:
            print("⛔ 자기 검사 실패 — T161 꼴(표는 ✅ · 제목은 ✅ 없음)을 못 잡았다: %s" % (bad,))
            return 1
        # ⓑ 제목에 ✅ 를 달면 조용해야 한다(거짓 경고 0).
        io.open(r, "w", encoding="utf-8").write(
            "### T161 ✅ — 장비 이름을 바꾼다 (주인 …)\n### %s — 아직 아무도 안 한 일\n" % free)
        if mismatches(routine_heads(r), rows):
            print("⛔ 자기 검사 실패 — 제목에 ✅ 를 달았는데도 걸린다(거짓 경고)")
            return 1
        # ⓒ 진짜 트리에서 «코드 자취» 판정 — T161 은 이미 손댄 흔적이 있고, 쓰인 적 없는 번호는 깨끗하다.
        # ⚠ git 을 못 쓰는 자리(내려받은 tarball 등)에서는 이 조각을 **건너뛴다** — 여기서 «실패» 로 끝내면
        # CI dotnet 잡이 빨개지고 그 사슬 끝의 gh-pages(주인 폰)까지 멈춘다(결정 493 이 정한 그 원칙).
        if not _git(["rev-parse", "HEAD"]).strip():
            print("✓ task_state --self-test: 문서 대조는 통과(git 이 없어 «코드 자취» 조각은 건너뛴다)")
            return 0
        H, R = routine_heads(), progress_rows()
        # (unity1) aaawunity 는 여기서 실제 트리의 T161 을 박아 두고 «코드가 이미 있는 번호를 잡아도 된다고 하나» 를 봤다.
        #   이 레포는 그 번호가 없으므로 **표에 있으면서 ✅ 인 번호** 가운데 하나를 골라 같은 물음을 던진다 —
        #   아직 ✅ 가 하나도 없으면(막 판 자리) 그 조각은 건너뛴다(거짓 빨강으로 CI 를 세우지 않는다 · 결정 493 의 원칙).
        done = sorted((t for t, (_, m, _) in R.items() if m == "✅"), key=lambda t: int(t[1:]))
        if done and verdict(done[0], H, R)[0]:
            print("⛔ 자기 검사 실패 — 실제 트리의 끝난 작업 %s 을 «잡아도 된다» 로 판정했다" % done[0])
            return 1
        if not verdict(free, H, R)[0]:
            print("⛔ 자기 검사 실패 — 쓰인 적 없는 %s 를 «잡지 마라» 로 판정했다(거짓 경고)" % free)
            return 1
        # ⓓ **한 번호가 두 작업을 가리키는 자리**(T205) — 오늘 T189·T190·T204 로 세 번 났고
        #    그때마다 사람이 손으로 찾아 보고했다. 가짜 문서로 «잡는가 · 하나뿐이면 조용한가» 를 본다.
        io.open(r, "w", encoding="utf-8").write(
            "### T161 ✅ — 장비 이름을 바꾼다 (주인 …)\n"
            "### %s — 소환 결과 상자\n### %s — 검은 아웃라인\n" % (free, free))
        d = {}
        routine_heads(r, dups=d)
        if list(d) != [free] or len(d[free]) != 2:
            print("⛔ 자기 검사 실패 — 같은 번호가 붙은 제목 둘을 못 잡았다: %s" % (d,))
            return 1
        io.open(r, "w", encoding="utf-8").write(
            "### T161 ✅ — 장비 이름을 바꾼다 (주인 …)\n### %s — 소환 결과 상자\n" % free)
        d = {}
        routine_heads(r, dups=d)
        if d:
            print("⛔ 자기 검사 실패 — 번호가 하나씩인데 중복이라 한다(거짓 경고): %s" % (d,))
            return 1

        # ⓔ **«행이 없다» ↔ «접힌 행만 있다»**(T206) — 접힌 줄(✂·♻)은 `progress_rows` 가 일부러 안 세므로
        #    둘이 한 목록에 섞였다. 그러면 다음 워커가 이미 옮겨졌거나 취소된 번호를 «등재 중» 으로 읽는다.
        io.open(p, "w", encoding="utf-8").write(
            "| ID | 작업 | 상태 | SID |\n"
            "| T161 | 장비 이름 | ✂ **중복 행 — 살아 있는 기록은 위다** | 워커 L |\n")
        seen = row_ids_all(p)
        if "T161" not in seen:
            print("⛔ 자기 검사 실패 — 접힌 행도 «표에 있는 ID» 로 세야 한다")
            return 1
        io.open(p, "w", encoding="utf-8").write("| ID | 작업 | 상태 | SID |\n")
        if row_ids_all(p):
            print("⛔ 자기 검사 실패 — 행이 하나도 없는 표에서 ID 를 세었다(거짓 경고)")
            return 1

        # ⓔ **폐기(⛔)도 닫힌 꼴이다**(T210) — 표가 ⛔ 인데 제목에 표시가 없으면 잡아야 하고, ⛔ 를 달면 조용해야 한다.
        io.open(r, "w", encoding="utf-8").write("### T161 — 폐기된 일\n")
        io.open(p, "w", encoding="utf-8").write(
            "| ID | 작업 | 상태 | SID |\n| T161 | 폐기 | ⛔ 폐기 → T37 | |\n")
        got = mismatches(routine_heads(r), progress_rows(p))
        if [b[0] for b in got] != ["T161"] or got[0][3] != "⛔":
            print("⛔ 자기 검사 실패 — «표는 ⛔ · 제목엔 표시 없음» 을 못 잡았다: %s" % (got,))
            return 1
        io.open(r, "w", encoding="utf-8").write("### T161 ⛔ — 폐기된 일\n")
        if mismatches(routine_heads(r), progress_rows(p)):
            print("⛔ 자기 검사 실패 — 제목에 ⛔ 를 달았는데도 걸린다(거짓 경고)")
            return 1

        # ⓕ **본문에 ✂ 를 «인용» 한 살아 있는 줄**(T249) — 접힘은 칸 **머리**에서만 읽어야 한다.
        #    «칸 어디에든» 으로 읽으면 이런 줄이 통째로 접힘 처리되어 상태 지도에서 사라지고,
        #    그러면 제목↔상태 대조가 그 작업을 **아예 안 본다**(= 닫힌 일이 «열린 일» 로 남는다).
        #    실제로 T172 가 그랬다 — 머리는 ✅ 인데 한참 뒤 이력 문장에 «✂ 주인 13:2X 로 취소» 가 있었다.
        io.open(p, "w", encoding="utf-8").write(
            "| ID | 작업 | 상태 | SID |\n"
            "| T161 | 빛살 | ✅ **완료** — 그 뒤 **✂ 주인 13:2X 로 취소(→ T192)** 라 이력으로만 읽는다 | |\n")
        got = progress_rows(p)
        if got.get("T161", (0, "", ""))[1] != "✅":
            print("⛔ 자기 검사 실패 — 본문에 ✂ 를 인용했다고 살아 있는 줄을 접힘으로 셌다: %s" % (got,))
            return 1
        io.open(r, "w", encoding="utf-8").write("### T161 — 빛살\n")
        if [b[0] for b in mismatches(routine_heads(r), progress_rows(p))] != ["T161"]:
            print("⛔ 자기 검사 실패 — 그 줄이 상태 지도에서 빠져 제목↔상태 대조를 통째로 건너뛰었다")
            return 1
        #    거꾸로 — **머리가 ✂ 인 진짜 접힌 줄**은 여전히 안 세야 한다(거짓 경고 쪽이 더 나쁘다).
        io.open(p, "w", encoding="utf-8").write(
            "| ID | 작업 | 상태 | SID |\n| T161 | 빛살 | **✂ 중복 행 — 살아 있는 기록은 위다** | |\n")
        if progress_rows(p):
            print("⛔ 자기 검사 실패 — 머리가 ✂ 인 접힌 줄을 상태로 셌다")
            return 1

        # ⓕ **escape 된 파이프**(`\|`)가 든 칸을 제대로 가르는가(T210) — 못 가르면 상태를 엉뚱한 조각에서 읽는다.
        io.open(p, "w", encoding="utf-8").write(
            "| ID | 작업 | 상태 | SID |\n| T161 | UA 가 `iPhone\\|iPad\\|iPod` 로 갈린다 | ✅ 완료 | |\n")
        got = progress_rows(p)
        if got.get("T161", (0, "", ""))[1] != "✅":
            print("⛔ 자기 검사 실패 — `\\|` 가 든 칸 때문에 상태를 못 읽었다: %s" % (got,))
            return 1

        # ⓖ T231 — «(참고 · 실패 아님)» 줄이 **마지막 요약 줄에도** 실리는가.
        #    이것이 이 회차의 고침이다: 워커가 `| tail -1` 로 잘라 읽어도 참고 사항이 눈에 걸려야 한다.
        #    (그 줄이 안 보여서 닫힌 T223 이 «열린 급한 일» 로 읽힌 사고가 실제로 났다 · 결정 640)
        io.open(r, "w", encoding="utf-8").write("### T161 ✅ — 장비 이름\n")
        io.open(p, "w", encoding="utf-8").write(
            "| ID | 작업 | 상태 | SID |\n| T161 | 장비 이름 | ✅ 완료 | |\n"
            "| %s | 표시가 없는 행 | 대기 중이라고만 적었다 | |\n" % free)
        buf = io.StringIO()
        keep = sys.stdout
        try:
            sys.stdout = buf
            rc_note = cmd_check(routine_heads(r), progress_rows(p))
        finally:
            sys.stdout = keep
        lines = [ln for ln in buf.getvalue().splitlines() if ln.strip()]
        if rc_note != 0:
            print("⛔ 자기 검사 실패 — 참고뿐인데 실패로 끝났다(조율 결함은 막지 않는다 · 결정 493): rc=%s" % rc_note)
            return 1
        if free not in lines[-1]:
            print("⛔ 자기 검사 실패 — 참고 줄이 마지막 요약에 안 실렸다(tail -1 로 못 읽는다): %r" % (lines[-1],))
            return 1

        # ⓚ **«표에 열린 행은 있는데 §2 에 제목이 없다»**(T466 · ⓑ 의 반대 방향) — 잡는가 ·
        #    그리고 **닫힌 행은 안 잡는가**. 뒤엣것을 같이 묻는 까닭이 이 갈래의 값 전부다:
        #    실측에서 제목 없는 행 22개 중 **20개가 닫힌 것**이라, 닫힌 것까지 세면 신호 2 가 노이즈 20 에 묻힌다.
        #    그 좁힘을 자로 박아 두지 않으면 다음 사람이 «왜 다 안 세지» 하며 넓혀 놓는다.
        def _check_out(routine_text, progress_text):
            io.open(r, "w", encoding="utf-8").write(routine_text)
            io.open(p, "w", encoding="utf-8").write(progress_text)
            b = io.StringIO(); k = sys.stdout
            try:
                sys.stdout = b
                rc_ = cmd_check(routine_heads(r), progress_rows(p))
            finally:
                sys.stdout = k
            return rc_, b.getvalue()

        rc_h, out_h = _check_out(
            "### T161 ✅ — 장비 이름\n",
            "| ID | 작업 | 상태 | SID |\n| T161 | 장비 이름 | ✅ 완료 | |\n"
            "| %s | 제목 없이 등재된 열린 절 | 🔄 **1회차 push** | |\n" % free)
        if "ROUTINE §2 에 제목이 없는 작업" not in out_h or free not in out_h:
            print("⛔ 자기 검사 실패 — 열린 행인데 §2 제목이 없는 자리를 안 찍었다(T466):\n%s" % out_h)
            return 1
        if rc_h != 0:
            print("⛔ 자기 검사 실패 — §2 제목 빠짐은 **막지 않는다**(결정 493): rc=%s" % rc_h)
            return 1
        if free not in [ln for ln in out_h.splitlines() if ln.strip()][-1]:
            print("⛔ 자기 검사 실패 — 그 참고가 끝줄에 안 실렸다(결정 641): %r"
                  % ([ln for ln in out_h.splitlines() if ln.strip()][-1],))
            return 1

        rc_c, out_c = _check_out(
            "### T161 ✅ — 장비 이름\n",
            "| ID | 작업 | 상태 | SID |\n| T161 | 장비 이름 | ✅ 완료 | |\n"
            "| %s | 제목 없이 닫힌 절 | ✅ **완료** | |\n" % free)
        if "ROUTINE §2 에 제목이 없는 작업" in out_c:
            print("⛔ 자기 검사 실패 — **닫힌** 행까지 셌다 — 그러면 실측 22건 중 20건이 노이즈다(T466):\n%s" % out_c)
            return 1

        rc_q, out_q = _check_out(
            "### T161 ✅ — 장비 이름\n### %s — 제목이 있는 열린 절\n" % free,
            "| ID | 작업 | 상태 | SID |\n| T161 | 장비 이름 | ✅ 완료 | |\n"
            "| %s | 제목이 있는 열린 절 | ⬜ **대기** | |\n" % free)
        if "ROUTINE §2 에 제목이 없는 작업" in out_q:
            print("⛔ 자기 검사 실패 — 제목이 **있는데도** 찍었다(거짓 경고):\n%s" % out_q)
            return 1

        # ⓗ **«⬜ 인데 살아 있는 lock»(T238 · 결정 653)** — 잡는가 · 그리고 **죽은 lock 은 안 잡는가**.
        #    두 칸을 다 묻는 까닭: 거짓 경고 쪽이 더 나쁘다(90분 지난 lock 자리는 규약상 «잡아도 되는» 자리라
        #    거기서 «잡지 마라» 를 찍으면 이 자가 오히려 일감을 막는다).
        claims = os.path.join(tmp, "claims")
        os.makedirs(claims, exist_ok=True)
        io.open(r, "w", encoding="utf-8").write("### %s — 새 일\n" % free)
        io.open(p, "w", encoding="utf-8").write(
            "| ID | 작업 | 상태 | SID |\n| %s | 새 일 | ⬜ **대기 — 선점 안 됨** | |\n" % free)
        global CLAIMS
        keep_claims = CLAIMS
        try:
            CLAIMS = claims
            now = datetime.datetime.now(datetime.timezone.utc)
            def _write(minutes):
                when = (now - datetime.timedelta(minutes=minutes)).strftime("%Y-%m-%dT%H:%M:%SZ")
                io.open(os.path.join(claims, free + ".lock"), "w", encoding="utf-8").write(when + " sess-test\n")
            def _run():
                b = io.StringIO(); k = sys.stdout
                try:
                    sys.stdout = b; rc_ = cmd_check(routine_heads(r), progress_rows(p))
                finally:
                    sys.stdout = k
                return rc_, b.getvalue()
            _write(5)                      # 살아 있는 lock
            rc_live, out_live = _run()
            if rc_live != 0:
                print("⛔ 자기 검사 실패 — ⓖ 가 막았다(조율 결함은 알리기만 · 결정 493): rc=%s" % rc_live)
                return 1
            if "lock 이 살아 있는" not in out_live or free not in out_live:
                print("⛔ 자기 검사 실패 — «⬜ + 살아 있는 lock» 을 못 잡았다:\n%s" % out_live)
                return 1
            _write(STALE_MIN + 30)         # 죽은 lock
            rc_stale, out_stale = _run()
            if "lock 이 살아 있는" in out_stale:
                print("⛔ 자기 검사 실패 — 죽은 lock(90분 초과)인데 «잡지 마라» 로 찍었다(거짓 경고):\n%s" % out_stale)
                return 1

            # ⓚ **«🔄 인데 lock 없음 + 마지막 커밋 오래됨»(T164)** — 잡는가 · 살아 있는 lock · 방금 커밋 · ✅ 는 안 잡는가.
            def _run_k(mark, minutes, age):
                io.open(r, "w", encoding="utf-8").write("### %s %s— 새 일\n" % (free, "✅ " if mark == "✅" else ""))
                io.open(p, "w", encoding="utf-8").write(
                    "| ID | 작업 | 상태 | SID |\n| %s | 새 일 | %s 진행 | sess-test |\n" % (free, mark))
                lp = os.path.join(claims, free + ".lock")
                if minutes is None:
                    if os.path.exists(lp): os.remove(lp)
                else:
                    _write(minutes)
                b = io.StringIO(); k = sys.stdout
                try:
                    sys.stdout = b; rc_ = cmd_check(routine_heads(r), progress_rows(p), commit_age=lambda t: age)
                finally:
                    sys.stdout = k
                return rc_, b.getvalue()
            for mark, minutes, age, want in (("🔄", None, 600, True), ("🔄", 5, 600, False), ("🔄", None, 10, False),
                                             ("✅", None, 600, False), ("🔄", None, None, False)):
                rc_k, out_k = _run_k(mark, minutes, age)
                if rc_k != 0:
                    print("⛔ 자기 검사 실패(T164) — ⓚ 가 막았다(알리기만 · 결정 493): rc=%s" % rc_k)
                    return 1
                if ("마지막 커밋이 90분을 넘긴" in out_k) != want:
                    print("⛔ 자기 검사 실패(T164) — %s/lock %s/커밋 %s분 → 기대 %s:\n%s" % (mark, minutes, age, want, out_k))
                    return 1
            io.open(r, "w", encoding="utf-8").write("### %s — 새 일\n" % free)
            io.open(p, "w", encoding="utf-8").write(
                "| ID | 작업 | 상태 | SID |\n| %s | 새 일 | ⬜ **대기 — 선점 안 됨** | |\n" % free)

            # ⓘ **«미래로 적힌 lock»(T294)** — 잡는가 · 그리고 **시계 차이만 한 것은 안 잡는가**.
            #    여기서도 거짓 경고 쪽이 더 나쁘다: 컨테이너마다 시계가 조금씩 다른데 1~2분마다 울면
            #    워커가 이 참고 줄 전체를 흘려 읽게 되고, 그러면 ⓖ 도 같이 묻힌다.
            _write(-30)                    # 30분 «뒤» 시각이 적힌 lock
            rc_fut, out_fut = _run()
            if rc_fut != 0:
                print("⛔ 자기 검사 실패 — ⓘ 가 막았다(조율 결함은 알리기만 · 결정 493): rc=%s" % rc_fut)
                return 1
            if "«미래»" not in out_fut:
                print("⛔ 자기 검사 실패 — 미래로 적힌 lock 을 못 잡았다:\n%s" % out_fut)
                return 1
            _write(-1)                     # 1분 차 = 시계 차이 · 봐주는 폭
            _, out_near = _run()
            if "«미래»" in out_near:
                print("⛔ 자기 검사 실패 — 1분 차(시계 차이)에 울었다(거짓 경고):\n%s" % out_near)
                return 1

            # ⓙ **«낡은 lock 인데 임자는 살아 있다»(T329)** — 순수 함수라 git 없이 그대로 잰다.
            #    네 갈래를 다 본다: 잡아야 하는 것 하나 + 안 잡아야 하는 것 셋.
            #    ⚠ «판단 못 함(None)» 이 «죽었다» 로 새면 CI(얕은 클론)에서 매 런 거짓 경고가 난다 — 그 갈래를 따로 잰다.
            cases = [
                # (lock 나이, 그 SID 의 마지막 커밋 나이, 잡아야 하나, 무엇을 재나)
                (STALE_MIN + 20, 40,             True,  "낡은 lock + 최근 커밋 = 살아 있다(실측 T320-b·T322 꼴)"),
                (STALE_MIN + 20, STALE_MIN + 10, False, "둘 다 낡았다 = 진짜로 죽은 자리(뺏어도 된다)"),
                (STALE_MIN - 10, 1,              False, "lock 이 아직 살아 있으면 ⓖ 몫이지 이 갈래가 아니다"),
                (STALE_MIN + 20, None,           False, "판단 못 함(얕은 클론 · CI) — 모르면 아무 말도 안 한다"),
            ]
            for lock_age, commit_age, want, why in cases:
                got = stale_but_alive(lock_age, commit_age)
                if got != want:
                    print("⛔ 자기 검사 실패 — stale_but_alive(%s, %s) = %s (기대 %s) · %s"
                          % (lock_age, commit_age, got, want, why))
                    return 1

            # ⓛ **T447 — 그 앎이 «선점 직전 단일 조회» 의 판정까지 닿는가.**
            #    ⓙ 는 **순수 함수**가 옳다는 것만 재고, ⓖ 의 요약 줄은 사람이 읽는 참고다.
            #    그런데 워커가 규약 절차 ①에서 실제로 부르는 것은 `verdict()` 다 —
            #    T329 이래 넉 달 동안 그 자리만 «죽은 lock = 잡아도 된다» 로 남아 있었다.
            #    ⛑ **T465 — 여기 «발자취 0 으로 재야 드러난다» 고 적어 두고 그 꼴을 피해 간 것이 결함이었다.**
            #      인수가 나는 절은 **예외 없이 발자취가 있다**(누가 하다 멈춘 자리라야 인수할 것이 있다) ⇒
            #      이 갈래는 정작 필요한 자리에서 한 번도 안 섰다. 이제 발자취 **있는** 꼴을 같이 잰다.
            _keep = (lock_of, footprint, sid_commit_age, sid_last_task)
            try:
                _FOOT = ([ "Assets/Scripts/Game/X.cs" ], [("abc1234", "2026-09-11T00:00", "T999999 1회차 — 무엇")])
                for lock_age, commit_age, foot, want_grab, want_word, why in [
                    (STALE_MIN + 30, 20,             ([], []), False, "살아 있다", "낡은 lock + 임자 살아 있음 = 뺏지 마라"),
                    (STALE_MIN + 30, STALE_MIN + 30, ([], []), True,  "",          "둘 다 낡았다 = 인계해서 잡아도 된다"),
                    (STALE_MIN + 30, None,           ([], []), True,  "",          "판단 못 함(얕은 클론) = 종전대로 잡아도 된다"),
                    # ⛑ 아래 둘이 T465 가 세운 갈래다 — 옛 차례에서는 첫째가 «이미 손댄 흔적» 으로 나와 사람을 통과시켰다.
                    (STALE_MIN + 30, 20,             _FOOT,    False, "뺏지 마라", "발자취가 있어도 «뺏지 마라» 가 먼저 선다(T465)"),
                    (STALE_MIN + 30, STALE_MIN + 30, _FOOT,    False, "손댄 흔적", "임자까지 죽었으면 그때는 «읽어라» 가 맞다(막는 것은 그대로)"),
                ]:
                    globals()["lock_of"] = lambda _t, _a=lock_age: ("sess-test-0000", _a)
                    globals()["footprint"] = lambda _t, _f=foot: _f
                    globals()["sid_commit_age"] = lambda _s, _c=commit_age: _c
                    globals()["sid_last_task"] = lambda _s: ""
                    grab, line = verdict("T999999", {}, {})
                    if grab != want_grab:
                        print("⛔ 자기 검사 실패 — verdict 의 lock 갈래가 %s (기대 %s) · %s\n   판정 줄: %s"
                              % (grab, want_grab, why, line))
                        return 1
                    if want_word and want_word not in line:
                        print("⛔ 자기 검사 실패 — 막았는데 «%s» 를 안 말한다(다음 사람이 까닭을 모른다) · %s\n   판정 줄: %s"
                              % (want_word, why, line))
                        return 1
                # T465 — «그 커밋은 딴 절 것이다» 재료가 판정 줄에 실리는가(잣대는 안 바뀐다 · 말만 는다).
                globals()["lock_of"] = lambda _t: ("sess-test-0000", STALE_MIN + 30)
                globals()["footprint"] = lambda _t: _FOOT
                globals()["sid_commit_age"] = lambda _s: 20
                globals()["sid_last_task"] = lambda _s: "T888888"
                grab, line = verdict("T999999", {}, {})
                if grab or "T888888" not in line:
                    print("⛔ 자기 검사 실패 — 임자의 마지막 커밋이 딴 절인데 그 번호를 안 말한다: %s" % line)
                    return 1
                globals()["sid_last_task"] = lambda _s: "T999999"
                _g, line_same = verdict("T999999", {}, {})
                if "다만 그 커밋은" in line_same:
                    print("⛔ 자기 검사 실패 — 같은 절인데 «딴 절» 이라 말한다: %s" % line_same)
                    return 1
            finally:
                globals()["lock_of"], globals()["footprint"], globals()["sid_commit_age"], globals()["sid_last_task"] = _keep
        finally:
            CLAIMS = keep_claims

        # ⓚ **T415 — 행을 지워도 다음 번호가 안 내려간다.** 순수 함수라 진짜 git 없이 잰다.
        #    재는 것은 «맨 위 행이 지워진 상태에서 발급이 어떻게 되는가» 하나다 — 2026-09-10 T409 가 겪은 그 상황.
        #    ⚠ 옛 규칙(표만 본다)이 무엇을 냈는지도 같이 박아 둔다 — 안 그러면 이 자가 무엇을 막는지 다음 사람이 모른다.
        rows_del = {"T1", "T2", "T414"}          # T415 의 행이 지워졌다
        hist_has = {"T1", "T2", "T414", "T415"}  # 이력에는 남아 있다(append-only)
        nxt, tmax, hmax = next_id(rows_del, hist_has)
        if (nxt, tmax, hmax) != (416, 414, 415):
            print("⛔ 자기 검사 실패 — 행을 지운 뒤 발급이 (다음 %s · 표 %s · 이력 %s) 다 (기대 416·414·415)"
                  % (nxt, tmax, hmax))
            return 1
        if max(int(t[1:]) for t in rows_del) + 1 != 415:
            print("⛔ 자기 검사 실패 — 이 판이 «옛 규칙이면 415 를 재발급한다» 를 못 보여 준다")
            return 1
        if buried_ids(rows_del, hist_has) != ["T415"]:
            print("⛔ 자기 검사 실패 — 지워진 행(T415)을 «이력에만 있는 번호» 로 못 잡았다: %s"
                  % (buried_ids(rows_del, hist_has),))
            return 1
        if buried_ids(hist_has, hist_has) != []:
            print("⛔ 자기 검사 실패 — 멀쩡한 표에서 울었다(거짓 경고)")
            return 1
        # 구멍(발급된 적 없는 빠진 번호)에는 안 운다 — 지금 표의 T92·T333~T339 가 그 꼴이다.
        if buried_ids({"T1", "T3"}, {"T1", "T3"}) != []:
            print("⛔ 자기 검사 실패 — 빠진 번호(구멍)에 울었다 — 그 여덟은 발급된 적이 없다")
            return 1
        # git 이 없는 통(얕은 클론·CI)에서는 이력이 비고, 그러면 표만 보고 답한다(아무 말도 안 지어내지 않는다).
        nxt0, tmax0, hmax0 = next_id(rows_del, set())
        if (nxt0, tmax0, hmax0) != (415, 414, None):
            print("⛔ 자기 검사 실패 — 이력이 없을 때 (다음 %s · 표 %s · 이력 %s) 다 (기대 415·414·None)"
                  % (nxt0, tmax0, hmax0))
            return 1

        # T446 — «남이 놓고 간 진단» 을 세는 두 눈. 값을 손으로 넣어 갈래를 낸다(git·표와 무관하게 순수 함수다).
        HOLDER = "sess-0000-1"
        row_cases = [
            ("남의 SID 가 적힌 진단 조각을 센다",
             "🔄 임자가 쥐고 있다 ‖ ⛑ **남이 놓고 간 진단(sess-9999-9 · 워커 G)**: 런 1 빨강은 …", 1),
            ("SID 가 없어도 «워커 X» 면 센다 — 워커 A 가 그 꼴로 적었다(실측)",
             "🔄 … ‖ ⛑ **[워커 A · 코드 0줄] 런 1070 빨강 진단은 이 절 것이다**", 1),
            ("임자 자신의 SID 는 안 센다",
             "🔄 … ‖ 진단을 내가 적었다(%s · 워커 K)" % HOLDER, 0),
            ("«진단» 이 없으면 안 센다", "🔄 임자가 쥐고 있다 ‖ 회차 기록만 있다(sess-9999-9)", 0),
            ("조각이 둘이면 둘로 센다",
             "🔄 … ‖ 진단(sess-9999-9) … ‖ 진단(sess-8888-8) …", 2),
        ]
        for why, txt, want in row_cases:
            got = row_handover_hint(txt, HOLDER)
            if got != want:
                print("⛔ 자기 검사 실패 — 행 진단 셈: %s → %d (기대 %d)" % (why, got, want))
                return 1
        # ⚠ 이 셈이 **거친** 것임을 갈래로 박아 둔다 — 자기 절 등재 글의 «워커 L» 을 물어 1 이 나온다(T446 실측).
        #   고칠 결함이 아니라 **알고 쓰는 한계**다: 그래서 출력이 «N군데로 보인다(거친 셈)» 이고
        #   커밋이 0 일 때 «얹혀 왔거나 헛짚었을 수 있다» 로 갈래를 열어 둔다.
        rough = row_handover_hint("… 워커 L 의 둘째 진단은 내 것에 없던 근거를 보탰다 …", HOLDER)
        if rough != 1:
            print("⛔ 자기 검사 실패 — 거친 셈의 «헛짚음» 갈래가 사라졌다(설명글을 고쳤으면 출력 문구도 같이 고쳐라)")
            return 1

        # T481 — «그 SID 가 민 마지막 커밋» 을 고르는 눈. 로그를 손으로 넣어 갈래를 낸다.
        #   ⚠ 이 갈래들이 재는 것은 **고르는 규칙**이다. «`--grep` 처럼 커밋 온몸을 보지 않는다» 는 성질은
        #     여기서 못 잰다 — 넣는 로그가 이미 제목뿐이기 때문이다(그 성질은 부르는 쪽이 `%s` 만 받는 데서 온다).
        #     그 성질은 실물로 한 번 쟀다: 2026-09-11 20:5X 에 T473 이 «84분(남의 커밋)» → «100분(임자 커밋)» 으로 바뀌었다.
        SID_A, SID_B = "sess-1111-1", "sess-2222-2"
        LOG = "\n".join([
            "2026-09-11T20:00:00+00:00\tT900 진단 → 놓고 간다 (%s · 워커 Z)" % SID_B,
            # ⚑ 실물에서 이 자를 속인 그 줄의 꼴 — «남의 커밋 **몸통**에 적힌 그 SID». 탭이 없으니 커밋 줄이 아니다.
            "  UiSmokeTests.cs 가 T471.lock(%s · 19:03 갱신 · 살아 있다) 범위다" % SID_A,
            "2026-09-11T19:00:00+00:00\tT901 1회차 (%s · 워커 A)" % SID_A,
            "2026-09-11T18:00:00+00:00\tT902 선점 (%s · 워커 A)" % SID_A,
        ])
        last_cases = [
            ("⚑ 남의 커밋 몸통에 적힌 그 SID 를 건너뛰고 그 SID 가 **민** 커밋을 고른다",
             SID_A, LOG, ("2026-09-11T19:00:00+00:00", "T901")),
            ("제 커밋이 맨 위면 그것을 고른다", SID_B, LOG, ("2026-09-11T20:00:00+00:00", "T900")),
            ("그 SID 의 커밋이 없으면 «모름» 이다", "sess-3333-3", LOG, (None, "")),
            ("빈 로그도 «모름» 이다", SID_A, "", (None, "")),
            ("⚑ 빈 SID 는 아무것도 안 고른다 — 빈 글자는 모든 제목에 «들어 있다»", "", LOG, (None, "")),
        ]
        for why, sid_x, log_x, want in last_cases:
            when_x, subj_x = sid_last_commit(sid_x, log=log_x)
            got = (when_x, (subj_x.split(" ")[0] if subj_x else ""))
            if got != want:
                print("⛔ 자기 검사 실패 — 임자의 마지막 커밋 고르기(T481): %s → %r (기대 %r)" % (why, got, want))
                return 1
        # 그리고 그 위에 선 `sid_last_task` 도 같은 재료를 쓰는지 — 첫 갈래의 답이 T901 이어야 한다.
        if sid_last_commit(SID_A, log=LOG)[1].split(" ")[0] != "T901":
            print("⛔ 자기 검사 실패 — `sid_last_task` 가 읽는 제목이 남의 커밋 것이다(T481)")
            return 1

        # T468 — 커밋 «제목 한 줄» 을 보는 눈. 순수 함수라 git·표와 무관하다.
        #   ⚑ 첫 갈래가 이 절이 산 것 그 자체다 — 옛 자는 «제목이 그 번호로 시작하면 임자 것» 이라는
        #     어림으로 **남이 놓고 간 진단을 뺐다**(실물: e861f232 가 T462 목록에서 통째로 빠졌다).
        subj_cases = [
            ("⚑ 번호로 시작해도 SID 가 임자가 아니면 센다(T468 이 산 자리)",
             "T900 런 1 진단 → 놓고 간다 (sess-9999-9 · 워커 G)", HOLDER, "sess-9999-9"),
            ("번호로 시작하고 SID 가 임자면 안 센다 — 제 회차 커밋이다",
             "T900 2회차 — 놓고 간다던 진단을 받아 고쳤다 (%s · 워커 K)" % HOLDER, HOLDER, None),
            ("임자를 모르면 종전대로 «번호로 시작» 을 뺀다 — 가릴 재료가 없다",
             "T900 런 1 진단 → 놓고 간다 (sess-9999-9 · 워커 G)", None, None),
            ("번호로 시작하고 제목에 SID 가 없으면 임자를 알아도 안 센다",
             "T900 런 1 빨강 진단 → 놓고 간다", HOLDER, None),
            ("번호가 가운데면 종전대로 센다 — SID 가 없어도",
             "런 1 빨강 진단 → T900 에 놓고 간다", HOLDER, "(SID 없음)"),
            ("진단 낱말이 없으면 안 센다", "T900 2회차 — 표를 고쳤다 (sess-9999-9 · 워커 G)", HOLDER, None),
            ("그 번호가 아니면 안 센다", "T901 런 1 진단 → 놓고 간다 (sess-9999-9 · 워커 G)", HOLDER, None),
        ]
        for why, subj, hold, want in subj_cases:
            got = handover_subject(subj, "T900", hold)
            if got != want:
                print("⛔ 자기 검사 실패 — 제목 눈(T468): %s → %r (기대 %r)" % (why, got, want))
                return 1

        # T453 — «행은 «쥔 채» 라는데 lock 이 없다» 를 보는 눈. 순수 함수라 표·git 과 무관하다.
        holds = [
            ("머리에 «쥔 채» 면 쥔 것이다", "T900", "🔄 **push · 확인 전(sess-1-1 · `T900.lock` 쥔 채)** — …", True),
            ("머리에 «TNNN.lock» 만 있어도 쥔 것이다", "T900", "🔄 **선점(sess-1-1 · `T900.lock`)** — …", True),
            ("머리에 «반납» 이면 아니다", "T900", "🔄 **1회차 · `T900.lock` 반납** — …", False),
            # ⚑ 이 갈래가 이 자가 첫 판에 놓친 자리다 — 행 «전체» 를 보면 뒤 이력의 «반납» 을 물어 눈이 먼다.
            ("머리는 «쥔 채» 인데 뒤 이력에 «반납» 이 있어도 쥔 것이다",
             "T900", "🔄 **push · 확인 전(`T900.lock` 쥔 채)** ‖ (이력) 1회차는 … lock 반납했다", True),
            ("아무 말도 없으면 아니다", "T900", "🔄 **push · 확인 전(sess-1-1)** — …", False),
        ]
        for why, tid, txt, want in holds:
            got = says_holds_lock(tid, txt)
            if got != want:
                print("⛔ 자기 검사 실패 — «쥔 채» 읽기: %s → %s (기대 %s)" % (why, got, want))
                return 1
        # 그리고 표시(🔄 / ✅ / ⬜)로 거르는지 — 아래 FREE 는 lock 파일이 없는 번호라 실제 파일계로 재도 안전하다.
        # ⚠ 그 번호를 **글자 그대로 쓰지 않는다** — 이 파일이 `git grep` 에 걸려 위 «쓰인 적 없는 번호» 갈래가 스스로 깨진다
        #   (바로 위 주석이 경고해 둔 함정인데, T453 회차가 **주석에 그 글자를 적었다가 그대로 걸렸다**).
        FREE = "T%d" % 9000
        gap_cases = [
            ("🔄 + 쥔 채 + lock 없음 → 잡는다", "🔄", "🔄 **push · 확인 전(`%s.lock` 쥔 채)**" % FREE, True),
            ("✅ 는 안 본다(거기 «쥔 채» 는 이력이다)", "✅", "✅ **닫음(… `%s.lock` 쥔 채였다)**" % FREE, False),
            ("⬜ 는 안 본다(그 자리는 T238 이 본다)", "⬜", "⬜ **대기(`%s.lock` 쥔 채)**" % FREE, False),
            ("🔄 인데 쥐었다는 말이 없으면 안 잡는다", "🔄", "🔄 **push · 확인 전(sess-1-1)**", False),
        ]
        for why, mk, txt, want in gap_cases:
            got = bool(lock_claim_gap({FREE: (1, mk, txt)}))
            if got != want:
                print("⛔ 자기 검사 실패 — 어긋남 셈: %s → %s (기대 %s)" % (why, got, want))
                return 1

        # ⓜ T29 — «코드 자취» 는 코드 식별자·파일명·폴더명만 센다. 주석(`//` `///` `/* */` `#` 삼중따옴표)과
        #    문자열 리터럴(자기 검사 픽스처 "T25.lock" 같은 것)·문서 내용은 자취가 아니다. 순수 함수라 git 없이 잰다.
        #    ⚠ 번호는 숫자 조립으로 만든다(위 ⓐ 와 같은 까닭 — 이 파일이 제 자기 검사에 걸리지 않게).
        tn = "T%d" % 9101
        clean_cases = [
            ("a.cs", "/// <summary>%s 전투가 쓴다</summary>\nint x = 1;\n" % tn),
            ("a.cs", "// %s 세이브가 이 값을 읽는다\nvar y = 2;\n" % tn),
            ("a.cs", "/* 여러 줄\n   %s 가 잇는다\n*/ int z;\n" % tn),
            ("a.cs", "var s = \"%s.lock\"; var t = '%s';\n" % (tn, tn)),
            ("a.js", "const note = `%s 가 대조한다`;\n" % tn),
            ("a.py", "# %s 가 쓴다\nx = 1\n" % tn),
            ("a.py", "def f():\n    \"\"\"%s 픽스처\"\"\"\n    return {\"%s\": 1}\n" % (tn, tn)),
            ("a.py", "d = {'%s': 'x'}  # 주석의 아포스트로피 don't\n" % tn),
            ("a.sh", "echo \"%s 끝\"  # %s\n" % (tn, tn)),
            ("docs/ref/layout.md", "| %s | 화면 |\n" % tn),
            ("data/x.json", "{\"%s\": 1}\n" % tn),
        ]
        for path, txt in clean_cases:
            if code_mentions(path, txt, tn):
                print("⛔ 자기 검사 실패(T29) — 주석·문자열·문서만 가리키는데 «코드 자취» 로 셌다: %s %r" % (path, txt[:50]))
                return 1
        dirty_cases = [
            ("a.cs", "class %sFixer { }\n" % tn),
            ("a.cs", "// 머리말\nint %s_count = 0;   // 꼬리 주석\n" % tn),
            ("a.py", "x = 1  # 앞 줄은 주석\n%s = 2\n" % tn),
            ("a.js", "const s = 'x'; %s();\n" % tn),
            ("Assets/Scripts/Game/%sWorld.cs" % tn, "int a;\n"),
            ("tools/%s/run.sh" % tn, "echo hi\n"),
            ("docs/ref/%s.md" % tn, "글\n"),
        ]
        for path, txt in dirty_cases:
            if not code_mentions(path, txt, tn):
                print("⛔ 자기 검사 실패(T29) — 코드 식별자·경로가 가리키는데 자취 0 으로 셌다: %s %r" % (path, txt[:50]))
                return 1
        # 실물: 이 트리에서 주석 참조만 있는 번호가 있으면(T7·T9·T13 이 그랬다) 그것이 **깨끗**해야 한다 —
        #   있고 없고는 트리에 달렸으니, 있는 경우에만 잰다(없으면 조용히 지나간다).
        # (같은 잣대: 번호 하나라도 «주석뿐인 파일» 을 files 에 남기면 위 clean_cases 가 먼저 잡는다.)

        # ⓣ T160 — «등재만 된 ⬜ 작업» 은 잡아도 된다 · 코드를 만진 커밋이 하나라도 있으면 그대로 «손댄 흔적» · 🔄 행은 그대로.
        c_doc = [("aaaaaaa", "2026-09-14T10:08:00+00:00", "T9000 등재: 새 일")]
        c_mix = c_doc + [("bbbbbbb", "2026-09-14T10:20:00+00:00", "T9000 1회차: 자")]
        pth = {"aaaaaaa": ["docs/ROUTINE.md", "docs/PROGRESS.md"], "bbbbbbb": ["docs/PROGRESS.md", "Assets/Tests/PlayMode/X.cs"]}
        t160 = [
            (("⬜", None, [], c_doc), True),
            (("⬜", None, [], c_mix), None),
            (("⬜", None, ["Assets/Scripts/Game/X.cs"], c_doc), None),
            (("🔄", None, [], c_doc), None),
            (("⬜", ("sess-0000-1", 200), [], c_doc), None),
            (("⬜", None, [], []), None),
        ]
        for (pm, lk_, fl, cm), want in t160:
            got = docs_only_verdict(pm, lk_, fl, cm, lambda h: pth[h])[0]
            if got != want:
                print("⛔ 자기 검사 실패(T160) — 등재만 된 ⬜ 판정: %s/%s/%s/%d커밋 → %r (기대 %r)" % (pm, lk_, fl, len(cm), got, want))
                return 1
        print("✓ task_state --self-test: 어긋난 짝을 잡고(T161) · **등재만 된 ⬜ 는 잡아도 되고 코드 커밋·🔄·lock 이 있으면 아니고(T160)** · **«🔄 인데 lock 없음·마지막 커밋 90분 초과» 를 참고로 찍되 산 lock·방금 커밋·✅ 는 안 찍고(T164)** · ✅ 를 달면 조용하고 · 빈 번호는 통과하고 ·"
              " 같은 번호 두 제목을 잡고 · «행 없음 ↔ 접힌 행만» 을 가르고 · ⛔ 와 `\\|` 도 읽고 ·"
              " 참고 줄이 마지막 요약에도 실리고(T231) · «⬜ + 살아 있는 lock» 을 잡되 죽은 lock 은 안 잡고(T238) · **미래로 적힌 lock 을 잡되 1분 차에는 안 울고**(T294) · **본문에 ✂ 를 인용한 살아 있는 줄을 접힘으로 안 센다**(T249) · **«낡은 lock 인데 임자는 살아 있다» 를 잡되 «둘 다 낡음»·«아직 살아 있음»·«판단 못 함» 셋에는 안 울고**(T329)"
              " · **맨 위 행을 지워도 발급이 안 내려가고(옛 규칙이면 그 번호를 재발급한다) · 지워진 번호를 잡되 멀쩡한 표·구멍·git 없음 셋에는 안 울고**(T415)"
              " · **«남이 놓고 간 진단» 을 남의 SID·SID 없는 «워커 X» 둘 다로 세되 임자 자신의 것은 안 세고, 그 셈이 «거친 것» 임을 갈래로 박아 둔다**(T446) · **«낡은 lock 인데 임자는 살아 있다» 를 요약뿐 아니라 `verdict()`(선점 직전 단일 조회)에서도 막고, «둘 다 낡음»·«판단 못 함» 둘에는 종전대로 잡게 둔다(T447)** · **그 막음이 «발자취가 있는» 절 — 곧 인수가 실제로 나는 유일한 꼴 — 에서도 판정 줄에 서고(T465), 임자의 마지막 커밋이 딴 절이면 그 번호까지 말한다**"
              " · **«행은 «lock 쥔 채» 라는데 lock 파일이 없다» 를 칸 «머리» 로만 가려 잡고(뒤 이력의 «반납» 에 안 속는다) · ✅·⬜ 표시에는 안 울고, 판정(rc)은 안 바꾼다**(T453)"
              " · **«표에 열린 행은 있는데 §2 에 제목이 없다» 를 잡되 닫힌 행·제목이 있는 행에는 안 울고, 그 참고가 끝줄에도 실리고 rc 는 0 이다**(T466)"
              " · **제목이 그 번호로 시작해도 SID 로 «임자가 아니다» 를 가렸으면 놓고 간 진단으로 세고, 임자 것·임자를 모를 때·제목에 SID 가 없을 때 셋에는 종전대로 안 센다**(T468)"
              " · **«임자가 살아 있다» 를 그 SID 가 **민** 커밋(제목)으로만 재고, 남의 커밋 몸통에 적힌 그 SID·빈 SID·빈 로그에는 안 속는다**(T481) · **«코드 자취» 는 코드 식별자·파일명·폴더명만 세고 주석(`//` `///` `/* */` `#` 삼중따옴표)·문자열 리터럴·문서 내용은 안 센다**(T29)")
        return 0
    finally:
        shutil.rmtree(tmp, ignore_errors=True)


def main(argv):
    if "--self-test" in argv:
        return self_test()
    if "--new-id" in argv:
        return cmd_new_id()
    dups = {}
    heads, rows = routine_heads(dups=dups), progress_rows()
    if "--check" in argv:
        return cmd_check(heads, rows, dups)
    if "--list" in argv:
        return cmd_list(heads, rows)
    ids = [a for a in argv if re.fullmatch(r"T\d+", a)]
    if ids:
        return max(cmd_one(t, heads, rows) for t in ids)
    return cmd_check(heads, rows, dups)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
