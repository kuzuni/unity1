#!/usr/bin/env python3
"""ROUTINE §7 «완결 정의 · 원작 ↔ 작업 대조표» ↔ PROGRESS 표 상태 대조 (T49).

§7 은 주인이 정한 «다 옮겨졌다» 의 **기준**이고 T33(완주 대조)이 마지막에 그것으로 판정한다.
그런데 그 표를 보는 자가 없어 끝난 작업이 §7 에서 🔄·⬜ 로 남는다 — 2026-09-12 21:36 실측 4칸
(scene3d 줄 «T10 🔄 · T12 🔄»(같은 칸 뒤에 «T12 ✅» 가 또 있다) · pets 줄 «T43 ⬜» · 품질 줄 «T45 ✅» = PROGRESS 는 🔄).
§2 제목 ↔ PROGRESS 는 `task_state --check` 가 보지만 §7 은 아무도 안 봤다. 그 자리를 메운다.

읽는 꼴(§7 «상태» 칸):
  ⓐ `✅`                      → «작업» 칸의 번호 전부가 그 표시
  ⓑ `✅ (T7 · T8)`            → 괄호 안 번호들이 그 표시
  ⓒ `✅ (Core/Audio · …)`     → 괄호에 번호가 없으면 ⓐ 와 같다(괄호는 설명이다)
  ⓓ `T16 ✅ · T20 🔄 · …`     → 번호마다 제 표시(괄호 안 설명에 섞인 `T39 ✅` 도 그 작업의 표시로 센다)
  ⓔ `해당 없음` · `—`         → 작업이 없는 줄 — 건너뛴다

사용:  python3 tools/check_final_table.py [--self-test]
       (어긋나면 ✗ 와 rc 1 · CI dotnet 잡은 이것을 **보고만** 한다 — 배포가 죽는 갈래가 아니다)
   ⚠ «열림 ↔ 열림»(⬜ ↔ 🔄)은 **알리기만** 한다(T82) — 선점 커밋이 PROGRESS 를 🔄 로 바꾸는 순간부터
     §7 을 고칠 때까지 몇 분 동안 모든 워커의 게이트가 빨개졌다(2026-09-12 T61 · 2026-09-13 T81 실측).
     T33 의 «다 옮겨졌다» 판정은 ✅ 만 본다 — ⬜ 인지 🔄 인지는 그 판정을 바꾸지 않는다.
     **닫힌 표시(✅⛔✂)가 한쪽에라도 끼면 그대로 rc 1** 이다(끝난 것을 안 옮겼거나, 안 끝난 것을 끝났다고 적은 갈래).
"""
import io, os, re, sys

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
ROUTINE = 'docs/ROUTINE.md'
PROGRESS = 'docs/PROGRESS.md'

GLYPHS = '✅🔄⬜⛔✂'
NONE_CELLS = ('해당 없음', '—', '-', '')

RE_PAIR = re.compile(r'T(\d+)\s*[·)\]]?\s*([' + GLYPHS + r'])')
RE_TID = re.compile(r'T(\d+)')
RE_LEAD = re.compile(r'^\s*([' + GLYPHS + r'])\s*(?:\((.*)\))?\s*$', re.S)


def progress_states(text):
    """PROGRESS 표 → {번호: 표시}. 같은 번호가 두 행이면 «닫힌 쪽»(✅⛔✂)을 쥔다(표는 접힌 행을 남긴다)."""
    out = {}
    for line in text.splitlines():
        if not line.startswith('| T'):
            continue
        cells = [c.strip() for c in line.strip().strip('|').split('|')]
        if len(cells) < 3:
            continue
        m = re.fullmatch(r'T(\d+)', cells[0])
        if not m:
            continue
        g = next((ch for ch in cells[2] if ch in GLYPHS), None)
        if g is None:
            continue
        tid = 'T' + m.group(1)
        if tid in out and out[tid] in '✅⛔✂':
            continue
        out[tid] = g
    return out


def final_rows(text):
    """ROUTINE §7 의 표 줄 → [(줄번호, 작업 칸, 상태 칸)] (머리·구분선 뺀 것)."""
    lines = text.splitlines()
    start = next((i for i, l in enumerate(lines) if l.startswith('## 7')), None)
    if start is None:
        return []
    rows = []
    for i in range(start + 1, len(lines)):
        l = lines[i]
        if l.startswith('## '):
            break
        if not l.startswith('|'):
            continue
        cells = [c.strip() for c in l.strip().strip('|').split('|')]
        if len(cells) < 4 or set(cells[0]) <= set('-: '):
            continue
        if cells[0].startswith('원작'):
            continue
        rows.append((i + 1, cells[2], cells[3]))
    return rows


def row_claims(work_cell, state_cell):
    """한 줄의 «상태» 칸 → [(번호, 표시)] (위 ⓐ~ⓔ · 순수 함수)."""
    state = state_cell.strip()
    if state in NONE_CELLS or not any(ch in GLYPHS for ch in state):
        return []
    lead = RE_LEAD.match(state)
    if lead:                                        # ⓐⓑⓒ — 표시 하나가 줄 전체를 덮는다
        glyph, inner = lead.group(1), lead.group(2) or ''
        ids = RE_TID.findall(inner) or RE_TID.findall(work_cell)
        return [('T' + n, glyph) for n in ids]
    return [('T' + n, g) for n, g in RE_PAIR.findall(state)]   # ⓓ — 번호마다 제 표시


def final_section(text):
    """§7 절 본문(다음 `## ` 앞까지) — 표 밖의 머리글·메모 줄도 포함한다."""
    lines = text.splitlines()
    start = next((i for i, l in enumerate(lines) if l.startswith('## 7')), None)
    if start is None:
        return ''
    end = next((i for i in range(start + 1, len(lines)) if lines[i].startswith('## ')), len(lines))
    return '\n'.join(lines[start:end])


def unlisted(routine_text, progress_text):
    """PROGRESS 에는 있는데 §7 절 **어디에도 이름이 없는** 작업 번호 [(번호, 표시)].

    왜 이것이 필요한가: `check()` 는 §7 에 **적혀 있는** 번호만 본다 — 새 작업을 §7 에 안 적으면
    그 작업은 열린 채로 T33 의 «§7 전 줄 ✅ = 다 옮겨졌다» 판정을 **통과한다**(2026-09-12 실측 17개).
    원작 모듈에 안 붙는 도구·게이트·CI 작업도 §7 의 «원작 밖» 줄에 적는다 — 코드 안 예외 목록은
    낡아서 또 같은 구멍을 낸다."""
    sec = final_section(routine_text)
    named = set('T' + n for n in RE_TID.findall(sec))
    out = [(tid, g) for tid, g in progress_states(progress_text).items() if tid not in named]
    return sorted(out, key=lambda t: int(t[0][1:]))


def check(routine_text, progress_text):
    """→ (어긋난 것 목록, 본 칸 수). 어긋남 = (줄번호, 번호, §7 표시, PROGRESS 표시 또는 사유)."""
    states = progress_states(progress_text)
    bad, seen = [], 0
    for lineno, work, state in final_rows(routine_text):
        claims = row_claims(work, state)
        first = {}
        for tid, glyph in claims:
            seen += 1
            if tid in first and first[tid] != glyph:
                bad.append((lineno, tid, glyph, '같은 칸에서 앞서 «%s» 로도 적혔다' % first[tid]))
                continue
            first.setdefault(tid, glyph)
            want = states.get(tid)
            if want is None:
                bad.append((lineno, tid, glyph, 'PROGRESS 표에 그 번호 행이 없다'))
            elif want != glyph:
                bad.append((lineno, tid, glyph, want))
    return bad, seen


OPEN = '⬜🔄'


def is_soft(glyph, want):
    """열림 ↔ 열림(⬜ ↔ 🔄) 어긋남인가 — T33 판정을 안 바꾸므로 막지 않고 알리기만 한다(T82)."""
    return want in GLYPHS and glyph in OPEN and want in OPEN


def main():
    routine = io.open(os.path.join(ROOT, ROUTINE), encoding='utf-8').read()
    progress = io.open(os.path.join(ROOT, PROGRESS), encoding='utf-8').read()
    bad, seen = check(routine, progress)
    miss = unlisted(routine, progress)
    rows = len(final_rows(routine))
    if miss:
        print('✗ check_final_table: §7 표에 **이름이 없는** 작업 %d개 — T33 은 §7 로 «다 옮겨졌다» 를 판정한다(그 위를 지나간다)' % len(miss))
        for tid, glyph in miss:
            print('  · %s %s — §7 어느 줄에도 안 적혔다' % (tid, glyph))
        print('  고침: 그 작업을 §7 의 제 원작 줄(«작업»·«상태» 칸)에 적는다. 원작 모듈에 안 붙는')
        print('        도구·게이트·CI 작업이면 §7 의 «(원작 밖 · 도구·게이트·CI)» 줄에 적는다.')
    hard = [b for b in bad if not is_soft(b[2], b[3])]
    soft = [b for b in bad if is_soft(b[2], b[3])]
    if hard:
        print('✗ check_final_table: §7 대조표가 PROGRESS 와 어긋난다 %d건 — T33 은 이 표로 «다 옮겨졌다» 를 판정한다' % len(hard))
        for lineno, tid, glyph, want in hard:
            if want in GLYPHS:
                print('  · %s:%d  %s  §7 «%s» ↔ PROGRESS «%s»' % (ROUTINE, lineno, tid, glyph, want))
            else:
                print('  · %s:%d  %s  §7 «%s» — %s' % (ROUTINE, lineno, tid, glyph, want))
        print('  고침: 끝낸 워커가 §7 의 그 칸도 같이 바꾼다(§7 머리줄 규약). 지금 고치려면 PROGRESS 상태에 맞춰 그 글자만.')
    if soft:
        print('⚠ 열림 ↔ 열림 %d건 — 막지 않는다(T82 · 선점 직후의 몇 분이다 · T33 판정은 ✅ 만 본다)' % len(soft))
        for lineno, tid, glyph, want in soft:
            print('  · %s:%d  %s  §7 «%s» ↔ PROGRESS «%s» — 그 작업의 임자가 다음 커밋에 맞춘다' % (ROUTINE, lineno, tid, glyph, want))
    if hard or miss:
        return 1
    print('✓ check_final_table: §7 줄 %d · 상태 표시 %d개가 PROGRESS 와 같다 · 이름이 빠진 작업 0%s'
          % (rows, seen, ' · 열림↔열림 %d(알림)' % len(soft) if soft else ''))
    return 0


def self_test():
    ok = True
    P = ('| ID | 작업 | 상태 | SID |\n|---|---|---|---|\n'
         '| T7 | 전투 | ✅ 완료 | x |\n| T8 | 씬 | ✅ 완료 | x |\n'
         '| T20 | UI | 🔄 진행 | x |\n| T35 | 배경 | ⬜ 대기 | x |\n'
         '| T36 | 접음 | ⛔ 흡수 | x |\n')
    states = progress_states(P)
    if states != {'T7': '✅', 'T8': '✅', 'T20': '🔄', 'T35': '⬜', 'T36': '⛔'}:
        print('✗ PROGRESS 읽기: ' + repr(states)); ok = False

    # 같은 번호가 두 행이면 닫힌 쪽을 쥔다(T22 가 실제로 그렇다).
    dup = progress_states(P + '| T22 | 화면 | ✅ 완료 | x |\n| T22 | 화면 | 🔄 진행 | x |\n')
    if dup.get('T22') != '✅':
        print('✗ 두 행 갈래: ' + repr(dup.get('T22'))); ok = False

    for work, state, want, note in [
        ('T7 · T8', '✅ (T7 · T8)', [('T7', '✅'), ('T8', '✅')], 'ⓑ 묶음'),
        ('T13', '✅', [('T13', '✅')], 'ⓐ 표시 하나'),
        ('T30', '✅ (`Core/Audio` · 벡터 대조)', [('T30', '✅')], 'ⓒ 괄호가 설명이면 작업 칸을 쓴다'),
        ('T16 · T20 · T43', 'T16 ✅ · T20 🔄 · T43 ⬜', [('T16', '✅'), ('T20', '🔄'), ('T43', '⬜')], 'ⓓ 번호마다'),
        ('T8 · T39', 'T8 ✅(적 스폰 · 뺀 연출은 T39 ✅)', [('T8', '✅'), ('T39', '✅')], 'ⓓ 괄호 안 설명의 번호도 센다'),
        ('T26', '해당 없음', [], 'ⓔ 작업 없는 줄'),
        ('—', '—', [], 'ⓔ 빈 줄'),
        ('T9', 'T9 ✅(SIMPLE_BG 의 보이는 것 · 소재 T34)', [('T9', '✅')], '표시 없는 번호는 안 센다'),
    ]:
        got = row_claims(work, state)
        if got != want:
            print('✗ %s: %r → %r (기대 %r)' % (note, state, got, want)); ok = False

    R = ('## 7. 완결\n\n| 원작 | 무엇 | 작업 | 상태 |\n|---|---|---|---|\n'
         '| a.js | 전투 | T7 · T8 | ✅ (T7 · T8) |\n'
         '| b.js | UI | T20 | T20 🔄 |\n'
         '## 8. 뒤\n| c.js | 안 센다 | T99 | T99 ✅ |\n')
    bad, seen = check(R, P)
    if bad or seen != 3:
        print('✗ 깨끗한 표인데 어긋남 %r (본 표시 %d · 기대 3 — §8 줄은 안 본다)' % (bad, seen)); ok = False

    for row, want_reason, note in [
        ('| b.js | UI | T20 | T20 ✅ |', '🔄', '끝났다고 적었는데 PROGRESS 는 진행'),
        ('| b.js | 접음 | T36 | T36 ⬜ |', '⛔', '접힌 작업을 대기로'),
        ('| b.js | 없음 | T77 | T77 ✅ |', 'PROGRESS 표에 그 번호 행이 없다', '표에 없는 번호'),
        ('| b.js | 둘 | T8 | T8 🔄 · T8 ✅ |', '같은 칸에서 앞서 «🔄» 로도 적혔다', '한 칸 두 표시'),
    ]:
        bad, _ = check('## 7. x\n\n| 원작 | 무엇 | 작업 | 상태 |\n|---|---|---|---|\n' + row + '\n', P)
        if not any(b[3] == want_reason for b in bad):   # 한 줄이 두 가지로 어긋날 수 있다(«두 표시» 줄이 그렇다)
            print('✗ %s: %r' % (note, bad)); ok = False

    # T82 — 열림↔열림은 알리기만 · 닫힌 표시가 끼면 그대로 막는다
    for glyph, want, soft, note in [
        ('⬜', '🔄', True,  '선점 직후(§7 ⬜ ↔ PROGRESS 🔄)'),
        ('🔄', '⬜', True,  '반납 직후(§7 🔄 ↔ PROGRESS ⬜)'),
        ('🔄', '✅', False, '끝났는데 §7 이 진행'),
        ('✅', '🔄', False, '§7 만 끝났다고'),
        ('⬜', '⛔', False, '접힌 것을 대기로'),
        ('⬜', 'PROGRESS 표에 그 번호 행이 없다', False, '사유 문자열은 늘 막는다'),
    ]:
        if is_soft(glyph, want) != soft:
            print('✗ T82 갈래(%s): is_soft(%r, %r) = %r' % (note, glyph, want, is_soft(glyph, want))); ok = False

    # 미등재 갈래 — §7 에 이름조차 없는 번호를 잡는가
    R2 = ('## 7. 완결\n\n| 원작 | 무엇 | 작업 | 상태 |\n|---|---|---|---|\n'
          '| a.js | 전투 | T7 · T8 | ✅ (T7 · T8) |\n'
          '| b.js | UI | T20 | T20 🔄 |\n')
    miss = unlisted(R2, P)
    if [t for t, _ in miss] != ['T35', 'T36']:
        print('✗ 미등재 갈래: %r (기대 T35 · T36 — §7 에 이름이 없는 둘)' % (miss,)); ok = False
    if unlisted(R2 + '\n> 도구·게이트: T35 ⬜ · T36 ⛔\n', P):
        print('✗ 미등재 갈래: 표 밖 메모 줄에 적힌 번호도 «적힌 것»으로 봐야 한다'); ok = False
    if unlisted(R2.replace('## 7.', '## 6.'), P) == []:
        print('✗ 미등재 갈래: §7 절이 없으면 전부 미등재로 잡아야 한다'); ok = False

    # 진짜 문서로도 한 번 돌려 본다 — 다만 **여기서는 판정하지 않는다**: 남이 §7 을 어긋나게 두면
    # 그것은 «조율 결함»(보고만 하는 main() 의 몫)이지 이 자가 고장 난 것이 아니다. CI 에서
    # 자기 검사 스텝은 막고 대조 스텝은 보고만 하므로, 이 줄이 판정하면 남의 드리프트가 CI 를 막는다.
    routine = io.open(os.path.join(ROOT, ROUTINE), encoding='utf-8').read()
    progress = io.open(os.path.join(ROOT, PROGRESS), encoding='utf-8').read()
    real, seen = check(routine, progress)
    real_miss = unlisted(routine, progress)
    print('· 지금 §7: 상태 표시 %d개 · 어긋남 %d건 · 이름이 빠진 작업 %d개%s'
          % (seen, len(real), len(real_miss), '' if not (real or real_miss) else ' (자리는 --self-test 없이 돌려 본다)'))

    print('✓ check_final_table 자기 검사 통과' if ok else '✗ check_final_table 자기 검사 실패')
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(self_test() if ('--self-test' in sys.argv or '--selftest' in sys.argv) else main())
