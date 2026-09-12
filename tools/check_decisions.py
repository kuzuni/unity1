#!/usr/bin/env python3
"""«워커 결정 기록» 번호가 겹치는지 보는 자 (T131).

왜 있나 — 워커 여럿이 30분 안에 같은 파일을 미는데, 번호는 각자 «지금 제일 큰 것 + 1» 로 고른다.
그래서 fetch 한 뒤 남이 먼저 밀면 같은 번호가 둘이 된다(2026-09-07 하루에만 341·342·344·345·346 이
각각 둘씩 · 전례 결정 207~210 · 336). 이 기록은 **번호로 서로를 가리키는 문서**라 가리키는 곳이 둘이면
«결정 344 대로» 가 무슨 뜻인지 알 수 없게 된다.

쓰는 법 (커밋 «직전» 에 · ROUTINE §3 게이트 목록):
  python3 tools/check_decisions.py          # 겹치면 1 로 끝난다(겹친 번호와 줄을 찍는다)
  python3 tools/check_decisions.py --next   # 다음에 쓸 번호 하나만 찍는다
        ⚑ 2026-09-10 부터 **문서와 커밋 이력 중 큰 쪽 + 1** 이다(T415 가 작업 번호에서 산 것과 같은 꼴 · 결정 1186·1192).
          문서의 최대는 줄어들 수 있고 이력은 안 줄어든다 — 아래 `issued_in_history` 주석에 실제 사고가 적혀 있다.

규약(ROUTINE §1) — 겹쳤을 때 **늦게 push 한 쪽이 옮긴다**. 누가 늦었는지는
  git log -1 --format=%cI -S"<번호>. **<그 줄 첫 낱말>" -- docs/PROGRESS.md
로 각 줄이 처음 실린 커밋 시각을 재서 가린다. 옮길 때는 본문뿐 아니라 그 번호를 가리키는
docs/·Assets/ 의 «결정 N» 참조도 같이 옮긴다(이 자는 참조까지는 못 센다 · 사람이 본다).
"""
import re
import subprocess
import sys

DOC = "docs/PROGRESS.md"
HEAD = "## 워커 결정 기록"
NUM = re.compile(r"^(\d+)\. \*\*")

# 동결선 — 이 번호 «미만» 의 겹침은 세기만 하고 실패로 보지 않는다(T131 · 결정 356).
# 왜: 자를 처음 대 보니 80~336 에 40쌍 넘는 옛 겹침이 있었다. 그것들은 이미 수십 개의
# 커밋 메시지·코드 주석이 그 번호로 가리키고 있어(예: 어느 커밋이든 게이트 줄의 «결정 143»)
# 지금 와서 옮기면 그 참조가 전부 틀린 곳을 가리키게 된다 — 고치는 것이 더 나쁘다.
# 그래서 옛것은 «맥락으로 읽는다» 로 두고, 이 선부터는 겹치지 않게 지킨다.
FROZEN_BELOW = 1


def entries(path=DOC):
    """(번호, 줄번호, 줄 앞머리) 목록 — «워커 결정 기록» 절 안의 «N. **…» 줄만."""
    out = []
    inside = False
    with open(path, encoding="utf-8") as f:
        for i, line in enumerate(f, 1):
            if line.startswith("## "):
                inside = line.startswith(HEAD)
                continue
            if not inside:
                continue
            m = NUM.match(line)
            if m:
                out.append((int(m.group(1)), i, line[:90].rstrip()))
    return out



def issued_in_history(limit=4000):
    """커밋 **제목·본문**에 «결정 N» 으로 실린 번호 중 가장 큰 것 — 문서가 아니라 <b>이력</b>에서 읽는다.

    왜 있나 (T415 가 작업 번호에서 산 것과 같은 자리 · 결정 1186) — 문서의 최대는 **줄어들 수 있다**.
    실제로 한 번 줄었다: `0f3c0e40` 이 `b615d22a` 가 적은 «1149. **같은 «투사체» …»» 한 줄을 지웠고
    (충돌 정리 사고로 보인다) 그 뒤 `--next` 가 **1149 를 다시 내줬다** — 다른 워커가 그 번호로
    다른 결정을 적었고, 워커 O 가 산 사실은 문서에서 사라졌다(검수 Q 가 결정 1191 로 되살렸다).
    커밋 이력은 **append-only 라 안 줄어든다**. 그래서 둘 중 큰 쪽을 쓴다.

    **일부러 그 사고 시점에 대 봤다**(검수 Q · T249 «자가 정말 무는지 부러뜨려 본다») —
    `0f3c0e40` 에서 문서 최대는 **1148** 인데 이력 최대는 **1150** 이다.
      옛 규칙(문서만) → **1149** = 이미 쓰인 번호를 다시 준다(그날 실제로 그랬다)
      새 규칙(둘 중 큰 쪽) → **1151** = 안 겹친다
    ⚠ 이력에 **아직 안 실린** 번호(= 문서에만 있는 새 줄)는 이 함수가 못 본다 —
      그것은 «지워진 뒤 재발급» 을 막는 자이지 «동시 발급» 을 막는 자가 아니다.

    ⚠ 이 수는 «지금» 의 답이다 — 같은 순간 남도 같은 답을 얻는다. 동시에 뽑는 갈래는 이 자가 못 막고,
      그것을 가르는 것은 규약의 push 순서다(늦게 민 쪽이 옮긴다).

    ⚠⚑ **«결정» 뒤에 오는 것이 다 번호는 아니다**(T420 · 2026-09-10 실측) — 이 레포는 본문에
      «(테마 팔레트 색은 주인 **결정 2026-09-05** «등급·버튼 색은 …»)» 처럼 쓴다(`5b279052`).
      거기 «결정» 은 번호를 가리키는 말이 아니라 «정했다» 는 **낱말**이고 뒤따르는 것은 **날짜**다.
      옛 정규식은 그 **연도 2026** 을 번호로 집었고, 그래서 `--next` 가 **2027** 을 내놓았다 —
      첫 줄만 읽는 손이 그것을 적으면 기록에 번호 구멍 830개가 나고 그 뒤로 계속 올라간다(T281).
      ⇒ **상한을 짐작해 막지 않는다**(«2000 넘으면 버린다» 는 짐작한 만큼만 맞다 · T330).
        **꼴로 가른다**: 숫자 뒤에 `-` 나 숫자가 이어지면 그것은 날짜(또는 더 긴 수)이지 번호가 아니다.
    """
    try:
        out = subprocess.run(["git", "log", "--format=%s%n%b", "-%d" % limit],
                             stdout=subprocess.PIPE, stderr=subprocess.DEVNULL,
                             text=True, timeout=60).stdout
    except (OSError, subprocess.SubprocessError):
        return 0
    return max_issued(out)


def max_issued(text):
    """글 안의 «결정 N» 중 가장 큰 번호(없으면 0) — 날짜꼴(`결정 2026-09-05`)은 안 센다(T420)."""
    best = 0
    for m in re.finditer(r"결정\s*(\d{2,5})(?![\d-])", text):
        v = int(m.group(1))
        if v > best:
            best = v
    return best


def self_test():
    """T420 — 이 눈이 «번호» 와 «날짜» 를 실제로 가르는지 본다."""
    ok = True

    def want(label, got, exp):
        nonlocal ok
        if got != exp:
            ok = False
            print("✗ %s — 기대 %s / 실제 %s" % (label, exp, got))

    want("번호를 읽는다", max_issued("… (sess-1 · 워커 G · 결정 1197)"), 1197)
    want("사이 공백이 없어도 읽는다", max_issued("결정1150 을 적었다"), 1150)
    want("여럿이면 큰 쪽", max_issued("결정 990 · 결정 1152 · 결정 77"), 1152)
    # ⚑ 이 셋이 이 자가 생긴 까닭이다 — 실제 커밋 `5b279052` 의 문장 그대로
    want("날짜는 안 읽는다", max_issued("테마 팔레트 색은 주인 결정 2026-09-05 «등급·버튼 색은»"), 0)
    want("날짜 + 번호가 같이 있으면 번호만", max_issued("주인 결정 2026-09-05 … (결정 1152)"), 1152)
    want("더 긴 수도 안 읽는다", max_issued("결정 123456 은 번호가 아니다"), 0)
    want("아무것도 없으면 0", max_issued("결정을 적지 않은 커밋"), 0)

    print("✓ check_decisions 자기 검사 통과" if ok else "✗ check_decisions 자기 검사 실패")
    return 0 if ok else 1

def main(argv):
    if "--self-test" in argv:
        return self_test()
    try:
        rows = entries()
    except OSError as e:
        print("읽기 실패: %s" % e)
        return 2
    if not rows:
        print("«%s» 절에서 «N. **…» 줄을 하나도 못 찾았다 — 형식이 바뀌었는지 본다" % HEAD)
        return 2

    if "--next" in argv:
        doc = max(n for n, _, _ in rows)
        hist = issued_in_history()
        print(max(doc, hist) + 1)
        if hist > doc:
            print("  ⚠ 문서 최대 %d < 이력 최대 %d — 지워졌거나 아직 안 실린 번호가 있다(이력을 따랐다)"
                  % (doc, hist), file=sys.stderr)
        return 0

    seen = {}
    for n, ln, head in rows:
        seen.setdefault(n, []).append((ln, head))
    dup = {n: v for n, v in seen.items() if len(v) > 1}
    old = sorted(n for n in dup if n < FROZEN_BELOW)
    new = sorted(n for n in dup if n >= FROZEN_BELOW)
    nxt = max(seen) + 1
    if old:
        print("· 옛 겹침 %d쌍(%d 미만 · 동결 · 맥락으로 읽는다): %s"
              % (len(old), FROZEN_BELOW, ", ".join(str(n) for n in old)))
    if new:
        print("결정 번호가 겹친다 — 늦게 push 한 쪽이 옮긴다(ROUTINE §1 · 참조 «결정 N» 도 같이):")
        for n in new:
            print("  %d 이 %d 곳:" % (n, len(dup[n])))
            for ln, head in dup[n]:
                print("    %s:%d  %s" % (DOC, ln, head))
        # T281 — **마지막 줄은 판정이어야 한다.** 여기 있던 «다음에 쓸 번호: N» 은 초록일 때와
        #   똑같이 생겨서, 꼬리 한 줄로 읽는 워커가 **빨강을 초록으로 읽는다**(2026-09-08 실측 사고).
        #   그 수는 버리지 않고 판정 줄 안으로 옮겼다.
        print("✗ check_decisions: %d 이상 번호 겹침 %d건 — %s · 다음에 쓸 번호: %d (늦게 push 한 쪽이 옮긴다 · 참조 «결정 N» 도 같이)"
              % (FROZEN_BELOW, len(new), ", ".join(str(n) for n in new), nxt))
        return 1
    print("✓ check_decisions: 결정 %d개 · %d 이상 번호 겹침 0 (다음 번호 %d)" % (len(rows), FROZEN_BELOW, nxt))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
