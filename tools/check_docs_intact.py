#!/usr/bin/env python3
"""문서가 «통째로 깨진» 것만 잡는 자 (T330).

왜 있나 — 2026-09-09 하루에 main 에 실제로 실린 사고 둘:
  · `docs/PROGRESS.md` **3168줄 소실**(`0180d5c8` · 복구 `a7c35c3e`) — 충돌 해소 스크립트가
    «`>>>>>>>` 를 만나야 버퍼를 내보내는» 루프였는데 덩어리 뒤로 파일이 끝나 버퍼가 버려졌다.
    push 까지 됐고 **한 회차 동안 main 의 기록이 반쪽**이었다.
  · **풀다 만 충돌 표식 세 줄이 커밋됨**(`c63abd91` 이 치웠다).
둘 다 «게이트가 울 수 있었는데 안 들렸다» — 복구 기록이 그렇게 적었다: `check_decisions` 가
«N. ** 줄을 못 찾았다» 고 울었지만 그 출력이 `rebase --continue` 와 **같은 줄에 이어 붙어**
지나갔고, CI 에서 그 자는 `continue-on-error`(결정 493)라 조용했다.
⇒ **자료 손상만 따로 떼어** 제 이름의 단계로, **막는 자**로 세운다.

⚠ 결정 493 을 어기는 것이 아니다 — 잣대가 다른 자리다. 493 의 물음은
  «이 자가 빨간 동안 **배포된 게임이 못 쓰게 되는가**» 이고 번호 겹침·표 행 어긋남은 아니라서
  보고만 한다. **자료가 사라진 것은 그 물음의 대상이 아니다** — 물어야 할 것은
  «**되돌릴 수 있는가**» 이고, 아무도 못 알아챈 채 회차가 몇 번 지나가면 되돌릴 자리가 묻힌다.

⚠ 그래서 **오진할 수 없는 것만** 잡는다. 특히 **줄 수·백분율 문턱은 안 쓴다** —
  접는 회차(✂·♻)는 문서를 정상적으로 줄이는 자리라 문턱은 곧 거짓 경고가 되고,
  거짓 경고를 내는 «막는 자» 는 하루 만에 `continue-on-error` 로 내려간다.

⚠ **역사(git)를 안 본다.** `actions/checkout@v4` 가 `fetch-depth` 없이 도는 잡에는 커밋이
  한 판뿐이고, **T329 4항이 «그 때문에 `fetch-depth: 0` 을 켜지 마라» 를 이미 못 박았다.**
  아래 넷은 전부 **파일 하나만 읽고** 판정된다.

쓰는 법 (커밋 «직전» 에 · 그리고 CI 의 제 단계에서):
  python3 tools/check_docs_intact.py             # 깨졌으면 1 로 끝난다
  python3 tools/check_docs_intact.py --self-test # 자기 검사
"""
import re
import sys

PROGRESS = "docs/PROGRESS.md"
ROUTINE = "docs/ROUTINE.md"

DEC_HEAD = "## 워커 결정 기록"
DEC_LINE = re.compile(r"^(\d+)\. \*\*")
ROW = re.compile(r"^\| T\d")          # PROGRESS 표 행
TITLE = re.compile(r"^### T\d")       # ROUTINE §2 제목

# ⓒ 의 잣대 — 이 기록의 **가장 오래된 번호**(실측 2026-09-09: 31 · 그 아래 1~30 은 다른 절에 있다).
# 기록은 **위에만 쌓이므로** 이 수는 움직이지 않는다. 아래쪽이 날아가면 «가장 작은 번호» 가
# 수백 번대로 훌쩍 뛴다 — 그것이 이 자가 잡는 사고다.
# ⚠ 처음에 «1 근처면 성하다» 로 썼다가 **실제 파일에서 곧바로 거짓 경고가 났다**(min = 31).
#   짐작한 문턱은 짐작한 만큼만 맞는다 — 잣대는 **파일에서 재서** 박는다.
# ⚠ 옛 기록을 일부러 접는 날에는 이 수를 같이 올린다. 그것은 **일부러 하는 일**이라
#   자가 한 번 막아 세우는 것이 맞다(사고와 갈리는 유일한 자리가 «사람이 뜻했는가» 다).
TAIL_OLDEST = 31


def conflict_markers(text):
    """git 충돌 표식이 남은 줄 번호. `<<<<<<< `·`>>>>>>> ` 만 본다 — `=======` 은 안 본다."""
    # ⚠ `=======` 한 줄만으로 판정하면 안 된다: 마크다운에서 그것은 **제목 밑줄(setext H1)** 이고
    #    표·구분선에도 흔하다. 반면 `<<<<<<< ` / `>>>>>>> `(일곱 글자 + 공백 + 이름표)는
    #    문서에 우연히 나올 수 없다. 표식은 늘 세 줄이 한 벌이라 이 둘만 봐도 하나도 안 놓친다.
    out = []
    for i, line in enumerate(text.splitlines(), 1):
        if line.startswith("<<<<<<< ") or line.startswith(">>>>>>> "):
            out.append((i, line[:60].rstrip()))
    return out


def decisions(text):
    """«워커 결정 기록» 절 안의 번호 목록. 절 자체가 없으면 None(= 이 자가 판정할 것이 없다)."""
    found_head = False
    inside = False
    out = []
    for line in text.splitlines():
        if line.startswith("## "):
            inside = line.startswith(DEC_HEAD)
            found_head = found_head or inside
            continue
        if not inside:
            continue
        m = DEC_LINE.match(line)
        if m:
            out.append(int(m.group(1)))
    return out if found_head else None


def check_text(name, text):
    """한 파일의 판정 — 사고 목록(빈 목록 = 성하다)."""
    bad = []
    for ln, s in conflict_markers(text):
        bad.append("%s:%d 충돌 표식이 남아 있다 — «%s»" % (name, ln, s))

    if name.endswith("PROGRESS.md"):
        dec = decisions(text)
        if dec is not None:
            if not dec:
                bad.append("%s «%s» 절이 있는데 그 아래 «N. **…» 줄이 0개다 — 기록이 통째로 날아간 꼴이다"
                           % (name, DEC_HEAD))
            elif min(dec) > TAIL_OLDEST:
                bad.append("%s 결정 꼬리가 잘렸다 — 가장 작은 번호가 %d 다(가장 오래된 것은 %d 여야 한다 · 남은 것 %d개)"
                           % (name, min(dec), TAIL_OLDEST, len(dec)))
        rows = sum(1 for l in text.splitlines() if ROW.match(l))
        if rows == 0:
            bad.append("%s 작업 표 행(«| T…»)이 0개다" % name)

    if name.endswith("ROUTINE.md"):
        titles = sum(1 for l in text.splitlines() if TITLE.match(l))
        if titles == 0:
            bad.append("%s §2 작업 제목(«### T…»)이 0개다" % name)
    return bad


# ─────────────────────────────── 자기 검사 ───────────────────────────────
GOOD_PROGRESS = "\n".join([
    "# 진행",
    "| T1 | 무엇 | ✅ 됨 | — | — | — |",
    "| T2 | 무엇 | ⬜ 대기 | — | — | — |",
    "",
    DEC_HEAD,
    "",
    "3. **셋째** — 어쩌고.",
    "2. **둘째** — 어쩌고.",
    "1. **첫째** — 어쩌고.",
    "",
    "## 그 뒤 절",
    "끝.",
])
GOOD_ROUTINE = "\n".join(["# 지시서", "### T1 ✅ — 무엇", "### T2 — 무엇"])


def self_test():
    ok = True

    def want(label, got, exp):
        nonlocal ok
        if got != exp:
            ok = False
            print("✗ %s — 기대 %s / 실제 %s" % (label, exp, got))

    # 안 잡아야 하는 것들
    want("성한 PROGRESS", check_text(PROGRESS, GOOD_PROGRESS), [])
    want("성한 ROUTINE", check_text(ROUTINE, GOOD_ROUTINE), [])
    # 접힌 행이 있어 «짧아진» 문서 — 줄 수 문턱을 안 쓰므로 조용해야 한다
    folded = GOOD_PROGRESS.replace("| T2 | 무엇 | ⬜ 대기 | — | — | — |", "| T2 | ✂ 접힘 | — | — | — | — |")
    want("접힌 행", check_text(PROGRESS, folded), [])
    # 결정이 하나뿐인 새 문서 — 꼬리가 1 이므로 성하다
    young = GOOD_PROGRESS.replace("3. **셋째** — 어쩌고.\n2. **둘째** — 어쩌고.\n", "")
    want("결정 하나뿐", check_text(PROGRESS, young), [])
    # 본문에 우연히 낀 «=======» — 마크다운 제목 밑줄이라 잡으면 안 된다
    want("우연한 =======", check_text(PROGRESS, GOOD_PROGRESS + "\n제목\n=======\n"), [])
    # PROGRESS 아닌 파일에는 결정·표 규칙을 안 댄다
    want("다른 문서", check_text("docs/그밖.md", "아무 말"), [])

    # 잡아야 하는 넷
    want("충돌 표식 <<<", len(check_text(PROGRESS, GOOD_PROGRESS + "\n<<<<<<< HEAD\n")), 1)
    want("충돌 표식 >>>", len(check_text(PROGRESS, GOOD_PROGRESS + "\n>>>>>>> origin/main\n")), 1)
    wiped = GOOD_PROGRESS.split(DEC_HEAD)[0] + DEC_HEAD + "\n"          # 절 머리 아래가 통째로 사라진 꼴(그날 그것)
    want("결정 기록이 비었다", len(check_text(PROGRESS, wiped)), 1)
    tail = GOOD_PROGRESS.replace("2. **둘째** — 어쩌고.\n1. **첫째** — 어쩌고.\n", "")
    tail = tail.replace("3. **셋째**", "930. **셋째**")   # 꼬리가 날아가 «가장 작은 번호» 가 수백 번대로 뛴 꼴
    want("결정 꼬리가 잘렸다", len(check_text(PROGRESS, tail)), 1)
    norows = "\n".join(l for l in GOOD_PROGRESS.splitlines() if not ROW.match(l))
    want("표 행 0개", len(check_text(PROGRESS, norows)), 1)
    want("§2 제목 0개", len(check_text(ROUTINE, "# 지시서\n아무 말")), 1)

    print("✓ check_docs_intact 자기 검사 통과" if ok else "✗ check_docs_intact 자기 검사 실패")
    return 0 if ok else 1


def main(argv):
    if "--self-test" in argv:
        return self_test()
    bad = []
    for path in (PROGRESS, ROUTINE):
        try:
            with open(path, encoding="utf-8") as f:
                text = f.read()
        except OSError as e:
            print("✗ check_docs_intact: 읽기 실패 %s — %s" % (path, e))
            return 2
        bad += check_text(path, text)
    if bad:
        print("✗ check_docs_intact: 문서가 깨졌다 %d건 — **커밋하기 전에 되돌린다**(직전 커밋의 파일을 살려 내 편집만 다시 얹는다)" % len(bad))
        for b in bad:
            print("  · " + b)
        print("  ⚠ 이 자는 «자료 손실» 만 잡는다(T330) — 번호 겹침·표 행 어긋남은 다른 자가 보고만 한다(결정 493).")
        return 1
    print("✓ check_docs_intact: PROGRESS·ROUTINE 성하다 (충돌 표식 0 · 결정 꼬리 이어짐 · 표 행·§2 제목 있음)")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
