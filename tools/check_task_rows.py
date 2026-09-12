#!/usr/bin/env python3
# -*- coding: utf-8 -*-
r"""docs/PROGRESS.md 의 «같은 작업이 두 줄에 있고 상태가 어긋나는» 것을 잡는다 (T149·T150·T151·T165 사고 · 결정 455).

무엇이 문제였나
  표가 길어져 워커들이 아래쪽에 **새 표를 하나 더** 만들었고, 같은 작업이 두 줄에 남았다.
  위 줄은 «✅ 완료(확인 끝)» 인데 아래 줄은 «⬜ 대기 — 선점 안 됨» 이라, 아래 줄만 본 워커가
  **이미 끝난 일을 다시 잡는다**. 실제로 워커 J 가 T149·T151 을 선점했다가 코드를 읽고서야
  05:41 커밋(`1b215aa4` · 워커 C)이 셋을 이미 다 고쳐 놓은 것을 알았다 — 한 회차가 그냥 샜다.

무엇을 잡나
  ⓐ 같은 ID 가 여러 줄에 있고 ⓑ 한 줄은 «대기(⬜)» 인데 다른 줄은 «진행(🔄)» 이거나 «완료(✅)» 면 **실패**.
  이미 «✂»·«♻»(중복 행 표시)로 접어 둔 줄은 세지 않는다 — 그것이 이 사고를 막는 올바른 꼴이다.
  ID 는 표 첫 칸 글자 그대로 본다(«T63» 과 «T63-lobby» 는 다른 작업이다 · 꼬리의 ✅ 같은 표시는 떼고 본다).

이 자가 «보는 칸» (T198 · 결정 510 — 자를 놓을 때는 사각지대를 적어 둔다)
  표 한 줄은 여섯 칸(ID · 설명 · 상태 · 워커 · 범위 · 비고)인데 이 자가 읽는 것은 **ID · 설명 · 상태 셋**뿐이다.
  갈래 ⓒ·ⓓ 는 «닫혔다/하는 중» 을 **설명 칸과 상태 칸 둘 다**에서 찾는다 — 종결 문장을 설명 칸에 쓴 줄
  (T95·T114·T107 · 워커 A)이 상태 칸만 보던 옛 자를 그대로 지나갔기 때문이다.
  **나머지 세 칸(워커·범위·비고)은 안 본다** — 거기에 «✅ 종결» 을 적으면 이 자는 조용하다.
  그리고 «몇 번째 칸» 이라는 셈 자체가 어긋나 있으면 위 갈래가 전부 헛돈다 — 그래서 갈래 ⓔ(T201)가
  칸 수부터 본다. 칸을 가르는 것은 **escape 안 된 파이프**뿐이다(`\|` 는 글자).

쓰기: python3 tools/check_task_rows.py [--list]
  --list = 겹치는 줄을 전부 (실패가 아니어도) 보여 준다.
"""
import io
import os
import re
import sys

DOC = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "docs", "PROGRESS.md")

ROWHEAD = re.compile(r"^\|\s*T\d")
# ⚠ 칸을 가르는 것은 «escape 안 된» 파이프뿐이다 — 본문의 `\|`(iPhone\|iPad · `부위\|등급`)는 글자다.
#   옛 자는 `[^|]+` 로 끊어 그 파이프에서 칸을 갈랐고, 그래서 상태 칸을 «앞토막만» 읽고 있었다(T201).
CELL = re.compile(r"(?<!\\)\|")
FOLDED = ("✂", "♻")          # 이미 «중복 행» 이라고 접어 둔 줄


def folded(st):
    """접힘인가 — **칸 머리에서만** 읽는다(T249 · `task_state._fold` 와 같은 규약).

    이 자 자신이 안내하는 꼴이 «상태 칸을 «✂ 중복 행 — 살아 있는 기록은 N행이다» 로 바꾼다» 라
    접힘은 늘 **칸 맨 앞**에 온다. «칸 어디에든» 으로 읽으면 본문에 ✂ 를 인용한 **살아 있는 줄**이
    통째로 접힘 처리된다(T172 가 그랬다 — 머리는 ✅ 인데 2366번째 글자의 이력 문장 때문에
    두 자 모두에서 사라졌고, 그 바람에 §2 제목이 마커 없이 «열린 일» 로 남았다).
    """
    return st.lstrip("*_ ").startswith(FOLDED)
WAITING = "⬜"
LIVE = ("✅", "🔄")
CLOSED = re.compile(r"✅\s*\*{0,2}\s*(종결|완료|눈 확인까지 끝)")   # 본문에 적힌 «닫았다» 표시 (ⓓ)
# ⓕ — «lock 을 반납했다» 는 말. ⓓ 의 문구를 안 쓰고 닫는 워커가 있어서(T182: «✅ 3단계까지 전부 끝 · lock 반납»)
#     낱말을 하나 더 본다. **이것만으로는 못 쓴다** — 본문에 «lock 잡음»·«저 lock 이 닫히기 전에는» 처럼
#     남의/제 살아 있는 lock 을 말하는 줄이 많다. 그래서 `docs/claims/` 의 **실제 파일 유무**와 함께 본다(아래 참조).
# ⚑ T351 — **낱말을 넓히려다 재 보고 되돌렸다**(2026-09-09 18:0X · 워커 L · 결정 982).
#   드러난 자리: T344 가 «✅ 확인 끝 · 죽은 lock 인수 · 반납» 이라 적고 lock 도 놓았는데
#   ⓓ 의 낱말(«✅ 종결/완료»)도 ⓕ 의 낱말(«lock 을 반납»)도 안 맞아 **두 갈래 다 조용**했고,
#   머리가 🔄 로 남아 표에서 «남이 하는 중» 으로 보였다(그 행은 손으로 닫았다).
#   그래서 ⓕ 를 «반납» 한 낱말로 넓혀 봤다 — **그리고 세 번 거짓이 났다**:
#     ⓐ «`T300-a` 반납» = 쪼갠 lock 하나를 놓은 말(그 절은 하는 중) — 이력 스냅숏 14장에서 둘.
#     ⓑ «`T312` 반납으로 열려서» = **남의 작업** 이야기.
#     ⓒ «내 lock 둘 다 반납(…)» = **지난 회차의 이력**(지금 회차는 그 뒤에 새로 섰다).
#   ⇒ **낱말로는 못 가른다.** 까닭이 구조에 있다: 워커들은 머리를 고치는 대신 **본문에 회차를 덧붙인다**.
#   그래서 상태 칸은 «지금» 과 «지난 회차» 가 섞인 글이고, 어느 낱말이든 이력에서 먼저 걸린다.
#   («머리만 본다» 도 안 된다 — T344 는 머리가 «확인 전 · lock 쥔 채» 이고 **뒤에 붙은 회차**가 닫았다.)
#   ⇒ **넓히지 않았다.** 이 자를 정말 넓히려면 낱말이 아니라 **신호**가 하나 더 있어야 한다
#     (예: 그 작업의 **마지막 커밋**이 무엇을 말하는가 — 표 밖의 자리다). 지금은 그 자리가 비어 있고,
#     비어 있다는 사실을 여기 적어 둔다. **못 잡는 것을 잡는 척하는 자보다 못 잡는다고 적힌 자가 낫다.**
#   대신 **자기 검사**를 놓았다(`--selftest`) — 낱말에 기대는 자는 그 낱말 목록이 자로 지켜져야 한다.
RELEASED = re.compile(r"lock\s*(을|를)?\s*반납|반납했다|반납한다")
def says_closed(text):
    """이 칸이 «이 작업은 닫혔다» 고 말하는가 — 인용·남의 작업 이야기는 빼고 본다 (ⓓ).

    ⚑ T351 — `main()` 안의 중첩 함수였던 것을 **바깥으로 꺼냈다**(움직임은 한 줄도 없다).
      까닭은 자기 검사에서 부를 수 있어야 하기 때문이고, 짝인 `says_released` 와 나란히 두는 편이
      «이 자는 낱말 두 벌에 기댄다» 는 사실을 읽는 사람에게 바로 보여 준다.
    """
    for m in CLOSED.finditer(text):
        pre = text[:m.start()]
        if pre[-1:] in ("«", "(", "“"):        # 인용·괄호 안 = 남의 이야기
            continue
        if re.search(r"T\d+[^\s]*\s*$", pre):        # «T156 ✅ 종결» = 다른 작업 이야기
            continue
        return True
    return False


def says_released(text):
    """이 칸이 «lock 을 반납했다» 고 말하는가 (ⓕ · 낱말은 위 주석의 좁은 셋 그대로).

    ⚑ 넓히지 않았다 — 위 ⚑ T351 주석에 «넓혀 보고 거짓이 셋 났다» 를 적어 뒀다.
      이름을 붙여 둔 까닭은 자기 검사가 이 낱말 목록을 붙들어 두기 위해서다.
    """
    return bool(RELEASED.search(text))
COLS = 6                     # ID · 설명 · 상태 · 워커 · 범위 · 비고 (ⓔ)


def cells(line):
    """표 한 줄의 칸 목록(앞뒤 빈 칸 제외). escape 된 파이프에서는 안 가른다."""
    parts = CELL.split(line.rstrip("\n"))
    if parts and parts[0].strip() == "":
        parts = parts[1:]
    if parts and parts[-1].strip() == "":
        parts = parts[:-1]
    return parts


def rows(path):
    out, shape = [], []
    with io.open(path, encoding="utf-8") as f:
        for n, line in enumerate(f, 1):
            if not ROWHEAD.match(line):
                continue
            c = cells(line)
            tid = c[0].strip().replace("✅", "").replace("🔄", "").replace("⬜", "").strip()
            if not re.match(r"^T\d+[A-Za-z0-9\-·ⓐ-ⓩ]*$", tid):
                continue
            if len(c) != COLS:
                shape.append((n, tid, len(c)))
                continue
            out.append((n, tid, c[2].strip(), c[1].strip()))
    return out, shape


CLAIMS = os.path.join(os.path.dirname(DOC), "claims")


def live_lock_ids(claims_dir=None):
    """`docs/claims/*.lock` 이 실제로 있는 작업 ID 들 — ⓕ 가 «반납했다는 말» 과 함께 본다.

    ⚑ **쪼갠 lock 도 그 작업의 lock 이다**(T288 · 이 자보다 나중에 생긴 규약) — 한 절이 자 여럿을
       «자 하나씩» 나눠 잡을 때 이름이 `T288-4.lock` 처럼 «작업 ID + `-` + 번호» 가 된다.
       그 꼴을 모르면 **T288 절이 살아 있는데도 «아무도 안 잡는 줄»** 로 읽혀 ⓕ 가 거짓 경고를 낸다
       (2026-09-09 실측 · 결정 837). 그래서 `T288-4` 는 `T288-4` 와 **`T288` 둘 다**로 센다.
    """
    d = claims_dir or CLAIMS
    try:
        names = {f[:-5] for f in os.listdir(d) if f.endswith(".lock")}
    except OSError:
        return set()
    out = set(names)
    for n in names:
        head = n.split("-", 1)[0]
        if head != n and head:
            out.add(head)          # «T288-4» → 「T288」 도 잡혀 있는 것으로 본다
    return out


def main():
    show_all = "--list" in sys.argv
    live_locks = live_lock_ids()
    if not os.path.exists(DOC):
        print("PROGRESS.md 가 없다: " + DOC)
        return 1
    by_id = {}
    desc_of = {}
    table_rows, shape = rows(DOC)

    # ⓔ 칸 수가 어긋난 줄 — 이 자도, `task_state` 도, §5 채점도 «몇 번째 칸» 으로 상태를 읽는다.
    #    칸이 밀리면 워커 칸을 상태로 읽거나(T182 · 상태 칸이 아예 없었다) 상태를 앞토막만 읽는다
    #    (본문의 `|` 가 칸을 갈랐다 · T69·T114·T196 …). 그런 줄은 **모든 자의 눈 밖**이라 사고가 조용히 산다:
    #    실제로 T125 의 «✅ 눈 확인 끝 → 종결» 은 칸 밖으로 새어 나가 갈래 ⓓ 가 못 보고 있었다(T201 이 접자 그날로 잡혔다).
    #    고치는 법은 셋 — 본문 파이프는 `\|` 로 escape · 없는 칸은 만든다 · 칸 밖으로 샌 줄은 `<br>` 로 접는다(글자는 안 지운다).
    if shape:
        print("표 행의 «칸 수» 가 어긋난다 — 자들이 상태를 엉뚱한 칸에서 읽는다(T201):")
        for n, tid, k in shape:
            print("  " + tid + " — " + str(n) + "행: 칸 " + str(k) + "개(있어야 할 수 " + str(COLS) + ")")
        print("고치는 법 ⓐ 본문에 쓴 «|» 는 `\\|` 로 escape 한다(표·코드 조각을 칸 안에 쓸 때) ·")
        print("            ⓑ 빠진 칸은 만든다(모르면 «(기록 없음)») · ⓒ 줄이 «|» 로 안 끝나 다음 줄들이 새어 나갔으면 `<br>` 로 한 칸에 접는다.")
        return 1

    for n, tid, status, desc in table_rows:
        by_id.setdefault(tid, []).append((n, status))
        desc_of[(tid, n)] = desc

    # ⓒ 한 줄 안에서 어긋난 것 — 머리는 «⬜ 대기» 인데 본문에 «코드 push»·✅·🔄 가 있다.
    #    워커들이 상태 칸 «뒤» 에 회차 기록을 덧붙이면서 머리를 안 고쳐 생긴다(T170 실측 · 결정 500).
    #    표를 훑는 워커는 머리만 본다 — 그래서 끝난 일을 «선점 안 됨» 으로 읽고 다시 잡는다(T149·T151 사고와 같은 결).
    inner = []
    for tid, items in by_id.items():
        for n, status in items:
            if status.startswith(WAITING) and re.search(r"코드 push|✅|🔄", status):
                inner.append((tid, n, status))

    # ⓓ 머리는 «🔄 진행» 인데 본문에 «✅ 종결»·«✅ 완료» 가 적혀 있다 (T197 · 결정 500 의 짝).
    #    ⓒ 가 «⬜ → 실은 하는 중» 을 잡는다면 이 자는 «🔄 → 실은 끝났다» 를 잡는다. 사고는 한 번 더 나쁘다 —
    #    «🔄» 는 «남이 하는 중» 으로 읽히므로 다음 워커는 «남은 일 = 확인뿐» 만 보고 **이미 끝난 확인을 다시 한다**
    #    (실측: T159·T160·T177 은 워커 B 가 11:2X 에 PNG 로 닫았는데 개별 세 줄의 머리가 🔄 로 남아
    #     워커 F 가 15:3X 에 lock 을 셋 잡고 같은 확인을 통째로 되풀이했다 · 한 회차가 그냥 샜다).
    #    인용(«✅ 종결» 기록은…)과 남의 작업 이야기(«T156 ✅ 종결»)는 세지 않는다 — 앞 글자로 가른다.
    closed = []
    for tid, items in by_id.items():
        for n, status in items:
            if not status.lstrip("*_ ").startswith("🔄"):
                continue
            # ⚠ 상태 칸만 보면 놓친다 — 닫은 워커가 종결 문장을 «설명 칸» 앞에 쓰는 일이 실제로 있었다
            #    (T95·T114·T107 · 워커 A · T198 실측). 머리는 🔄 로 남고 자는 조용해 사고가 그대로 산다.
            #    그래서 두 칸을 다 본다 — 어느 칸에 적혔든 «닫혔다» 는 말은 머리와 어긋난다.
            if says_closed(status) or says_closed(desc_of.get((tid, n), "")):
                closed.append((tid, n, status))

    # ⓕ 머리는 «🔄 진행» 인데 본문이 «lock 을 반납했다» 고 말하고 **실제로 그 lock 파일이 없다** (T218).
    #    ⓓ 는 «✅ 종결»·«✅ 완료» 라는 **문구**를 찾는데, 그 문구를 안 쓰고 닫는 워커가 있다 —
    #    T182 는 «✅ 3단계까지 전부 끝 · lock 반납» 이라고 적었고 ⓓ 는 조용했다(내가 T198 로 그 자를 넓힌 바로 다음 자리다).
    #    ⚠ **낱말만으로는 거짓 경고가 난다** — T207(«lock 잡음»)·T215(«T182.lock 이 닫히기 전에는 잡지 마라»)는
    #    둘 다 살아 있는 lock 안이다. 그래서 두 신호를 함께 본다: **본문이 반납을 말한다 + 그 작업의 lock 파일이 실제로 없다.**
    #    (lock 이 살아 있으면 그 줄은 «하는 중» 이 맞으므로 이 자는 아무 말도 안 한다.)
    released = []
    for tid, items in by_id.items():
        if tid in live_locks:
            continue
        for n, status in items:
            if not status.lstrip("*_ ").startswith("🔄"):
                continue
            if (tid, n) in {(t, ln) for t, ln, _ in closed}:
                continue                                   # ⓓ 가 이미 잡은 줄은 두 번 안 센다
            if says_released(status) or says_released(desc_of.get((tid, n), "")):
                released.append((tid, n, status))

    # ⓖ 부모 행은 «🔄 진행» 인데 하위 행(«T63-lobby» 처럼 «부모-꼬리»)이 **전부 닫혀 있다** (T220 · 알리기만).
    #    T63 이 그랬다 — 하위 열셋이 다 ✅ 이고 부모가 «2단계의 마지막» 으로 적어 둔 `TextAudit.ClipStrict` 도 이미 true 인데
    #    부모 머리만 🔄 로 남아 표에서 «남이 하는 중» 으로 보였다. 워커 A 가 T96 을 같은 꼴로 손수 닫은 전례가 있다.
    #    ⚠ **실패로 세지 않는다** — 하위가 다 끝나도 부모에게 제 몫이 남아 있을 수 있다(«마지막에 strict 를 켠다» 같은 것).
    #    그것은 사람이 그 줄을 읽어야 알고, 자가 단정하면 멀쩡한 부모를 닫게 만든다(결정 493 의 기준: 조율 결함은 막지 않고 알린다).
    parent_done = []
    for tid, items in by_id.items():
        kids = [(k, v) for k, v in by_id.items() if k.startswith(tid + "-")]
        if not kids:
            continue
        def shut(st):
            h = st.lstrip("*_ ")
            return h.startswith("✅") or h.startswith("⛔") or folded(st)
        if not all(shut(st) for _, vs in kids for _, st in vs):
            continue
        for n, status in items:
            if status.lstrip("*_ ").startswith("🔄"):
                parent_done.append((tid, n, len(kids)))

    dups = {k: v for k, v in by_id.items() if len(v) > 1}
    bad = []
    for tid, items in dups.items():
        live = [it for it in items if not folded(it[1])]
        waiting = [it for it in live if it[1].startswith(WAITING)]
        moving = [it for it in live if it[1].lstrip("*_ ").startswith(LIVE)]
        if waiting and moving:
            bad.append((tid, waiting, moving))

    if show_all:
        for tid in sorted(dups, key=lambda s: (len(s), s)):
            print("· " + tid + ": " + " · ".join(str(n) + "행 " + s[:24] for n, s in dups[tid]))

    if inner:
        print("한 줄 안에서 상태가 어긋난다 — 머리는 «⬜ 대기» 인데 본문에 «코드 push»·✅·🔄 가 있다(결정 500):")
        for tid, n, status in sorted(inner, key=lambda x: x[1]):
            print("  " + tid + " — " + str(n) + "행: " + status[:70].replace("\n", " ") + " …")
        print("고치는 법: 상태 칸 **머리**를 실제 상태(✅·🔄)로 바꾼다 — 뒤에 붙인 회차 기록은 그대로 둔다(이력이다).")
        return 1

    if closed:
        print("머리는 «🔄 진행» 인데 본문에는 «✅ 종결/완료» 가 적혀 있다 — 다음 워커가 «남은 일 = 확인뿐» 으로 읽고 끝난 확인을 되풀이한다(T197):")
        for tid, n, status in sorted(closed, key=lambda x: x[1]):
            print("  " + tid + " — " + str(n) + "행: " + status[:70].replace("\n", " ") + " …")
        print("고치는 법: 상태 칸 **머리**를 ✅ 로 바꾸고 «(이력)» 뒤에 옛 머리를 그대로 남긴다 —")
        print("            «닫았다» 는 본문 «뒤» 가 아니라 **머리**에 있어야 한다(표를 훑는 워커는 머리만 본다).")
        print("            정말로 아직 하는 중이면 본문의 그 «✅ 종결» 이 남의 작업 이야기인지 보고, 그렇다면 «T160 ✅ 종결» 처럼 작업 번호를 앞에 적는다.")
        return 1

    if released:
        print("머리는 «🔄 진행» 인데 본문은 «lock 을 반납했다» 고 말하고 그 lock 파일도 실제로 없다 — 아무도 안 잡는 줄이 된다(T218):")
        for tid, n, status in sorted(released, key=lambda x: x[1]):
            print("  " + tid + " — " + str(n) + "행: " + status[:70].replace("\n", " ") + " …")
        print("고치는 법: 갈래 ⓓ 와 같다 — 상태 칸 **머리**를 ✅(또는 아직 남은 일이 있으면 그 상태)로 바꾸고 옛 머리는 «(이력)» 뒤에 남긴다.")
        print("            **정말로 하는 중인데 lock 이 없으면 그것이 문제다** — 반납한 채로 일하면 남이 같은 자리를 잡는다(§3). lock 을 다시 잡아라.")
        return 1

    # ⓖ 는 «알리기만» 이라 여기서 끝내지 않는다(위 갈래들이 다 통과했을 때만 이 줄이 보인다).
    if parent_done:
        print("· (참고 · 실패 아님) 부모 행이 «🔄» 인데 하위 행이 전부 닫혀 있다(T220) — 표를 훑는 워커에게 «하는 중» 으로 보인다:")
        for tid, n, k in sorted(parent_done, key=lambda x: x[1]):
            print("    " + tid + " — " + str(n) + "행 · 하위 " + str(k) + "개가 전부 ✅/⛔")
        print("    부모에게 제 몫이 남았으면 그대로 두고, 남은 것이 없으면 머리를 ✅ 로 올린다(본문은 안 지운다).")

    if bad:
        print("같은 작업이 두 줄에 있고 상태가 어긋난다 — «대기» 줄만 본 워커가 끝난 일을 다시 잡는다:")
        for tid, waiting, moving in sorted(bad, key=lambda x: x[0]):
            print("  " + tid + " — 대기 줄 " + ", ".join(str(n) + "행" for n, _ in waiting)
                  + " / 살아 있는 줄 " + ", ".join(str(n) + "행" for n, _ in moving))
        print("고치는 법 ⓐ 두 줄이 «같은 작업» 이면: 낡은 줄의 상태 칸을 «✂ 중복 행 — 살아 있는 기록은 N행이다» 로 바꾼다(지우지 않는다 · 이력이다).")
        print("고치는 법 ⓑ 두 줄이 «다른 작업인데 번호만 같으면» 접지 말고 **번호를 옮긴다** — 주인이 부른 쪽이 번호를 갖고 워커가 등재한 쪽이 다음 빈 번호로 간다")
        print("            (규약 «한 번호는 한 작업» · 결정 435 = T182 전례 · 결정 494 = T190 을 접었다가 되살린 사고). **먼저 두 줄의 제목을 읽고 ⓐ·ⓑ 를 가른다.**")
        return 1

    print("✓ check_task_rows: 표 행 " + str(sum(len(v) for v in by_id.values()))
          + "개 · 작업 " + str(len(by_id)) + "개 · «대기 ↔ 진행/완료» 어긋난 중복 0"
          + (" (중복 ID " + str(len(dups)) + "개는 전부 접혀 있거나 상태가 같다)" if dups else ""))
    return 0


def selftest():
    """자기 검사 (T351) — **이 자가 기대는 것은 낱말이고, 낱말은 워커마다 다르게 쓴다.**

    T344 가 그것을 보여 줬다: 확인도 반납도 끝났는데 ⓓ 의 낱말(«✅ 종결/완료»)도 ⓕ 의 낱말
    («lock 을 반납»)도 안 맞아 **두 갈래 다 조용**했고, 머리는 🔄 로 남아 «남이 하는 중» 으로 보였다.
    그래서 낱말 갈래를 여기에 못 박는다 — 다음에 누가 넓히거나 좁히면 이 칸들이 먼저 운다.
    """
    ok = True

    # ⓕ 낱말 — **지금 이 자가 실제로 세는 것**을 그대로 못 박는다(바라는 것이 아니다).
    #   ⚠ 아래 «사각지대» 표시는 **고장이 아니라 알려진 한계**다. T351 이 넓혀 보고 되돌린 자리이고
    #      (까닭은 위 ⚑ 주석), 여기 적어 두는 까닭은 **다음 사람이 그것을 모르고 넓히지 않게** 하기 위해서다.
    #      정말 고치려면 낱말이 아니라 «표 밖의 신호» 를 하나 더 가져와야 한다.
    for text, want, note in [
        ("lock 을 반납", True, ""),
        ("lock 를 반납", True, ""),
        ("반납했다", True, ""),
        ("반납한다", True, ""),
        ("lock 쥔 채", False, ""),
        ("아직 안 잡았다", False, ""),
        # ↓ 사각지대 셋 — 알고 두는 것이다.
        ("«죽은 lock 인수 · 반납)»", False, "사각지대: T344 가 쓴 꼴 — «lock» 이 «반납» 바로 앞에 없어 못 본다"),
        ("`T305.lock` 반납", False, "사각지대: 백틱이 사이에 있으면 못 본다"),
        ("lock 을 반납하지 않는다", True, "사각지대(거짓): 부정형인데 센다 — 다행히 ⓕ 는 lock 파일이 없을 때만 보므로 거의 안 터진다"),
    ]:
        if says_released(text) != want:
            print("✗ ⓕ 낱말: " + repr(text) + " — 기대 " + str(want) + ("  (" + note + ")" if note else "")); ok = False

    # ⓓ 낱말 — 인용·남의 작업 이야기는 안 센다(옛 계약 그대로).
    for text, want in [("✅ 종결", True), ("✅ **완료(sess-…)**", True),
                       ("(«✅ 종결» 기록은…)", False), ("T156 ✅ 종결", False)]:
        if says_closed(text) != want:
            print("✗ ⓓ 낱말: " + repr(text) + " — 기대 " + str(want)); ok = False

    # 쪼갠 lock 은 부모도 «잡혀 있다» 로 센다(결정 837 · ⓕ 가 거짓 경고를 내던 자리).
    import tempfile
    with tempfile.TemporaryDirectory() as d:
        io.open(os.path.join(d, "T288-4.lock"), "w", encoding="utf-8").write("x y\n")
        got = live_lock_ids(d)
        if not ({"T288", "T288-4"} <= got):
            print("✗ 쪼갠 lock: " + repr(got)); ok = False

    # 접힘은 «칸 머리» 에서만 읽는다(T172 가 통째로 사라졌던 자리).
    if not folded("✂ 중복 행 — 살아 있는 기록은 168행이다") or folded("✅ 완료 — 옛 줄은 ✂ 로 접었다"):
        print("✗ 접힘 판정"); ok = False

    print(("✓" if ok else "✗") + " check_task_rows 자기 검사 4묶음")
    return 0 if ok else 1


if __name__ == "__main__":
    if "--selftest" in sys.argv or "--self-test" in sys.argv:
        sys.exit(selftest())
    # T281 — **어느 갈래로 나가든 마지막 줄이 판정이다**(T239 가 실패 목록에 세운 계약 · 결정 678 과 같은 것).
    #   이 자는 갈래마다 «고치는 법» 을 마지막에 찍어서, 꼬리 한 줄로 읽으면 **빨강인지 초록인지 알 수 없었다**.
    #   초록 갈래는 이미 «✓ …» 로 끝나므로 빨간 갈래에만 한 줄 더 붙인다(위 설명은 그대로 둔다 — 고치는 법이 먼저다).
    _rc = main()
    if _rc:
        print("✗ check_task_rows: 어긋난 행이 있다 — 바로 위 «고치는 법» 을 그대로 따르면 된다(rc=%d)" % _rc)
    sys.exit(_rc)
