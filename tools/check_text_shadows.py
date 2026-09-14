#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T333 — 정본 `text-shadow` 선언 ↔ 클론 글자 그림자(`UiKit.TextShadow` · TMP 언더레이) 대조 자.

정본 `web/css/style.css` 의 `text-shadow` 는 54 선언 — 그중 `none` 4(끄는 규칙) · `check_keyline` TABLE 의 선택자(T104·T109 가 키라인으로 옮긴 자리 · 4/8방향 hard 링 등) 10 은
그쪽 몫이라 빼고 센다. 남은 자리는 «표(TABLE)에 짝을 적은 것만» 대조한다 — 표에 없는 정본 선택자는 «미정» 으로 세기만 하고 막지 않는다(T178 `check_surface_gradients` 규약 ·
선택자가 많아 회차마다 표가 자란다 · `--list` 로 본다). 막는 것은 둘 — 표에 적힌 자리에 그림자 호출이 없는데 KNOWN 도 아닌 것 · 표에는 있는데 정본에서 사라진 선택자.

무엇을 «그림자가 있다» 로 세나: 그 자리가 `UiKit.TextShadow(` 를 지나가는가(TMP `_Underlay*` 를 직접 만지는 것은 안 센다 — 식·표를 거치지 않으면 §1 «수치는 표로» 를 샌다).

선택자 ↔ 클론 자리 짝은 TABLE 하나가 쥔다(T109·T159·T178 의 꼴 그대로):
  "Ui/File.cs"           파일 안 어디든 그림자 호출이 하나 있으면 됨
  "Ui/File.cs#name"      그 이름으로 만든 글자에 그림자 호출이 닿아야 함(변수 대입 꼬리를 좇는다)
  "Ui/File.cs@Method"    도우미 메서드 본문 안에 그림자 호출이 있어야 함
  "—<이유>"              대조하지 않는다(클론에 자리 없음 등)
  "ring:<위 꼴>"         그 자리에 **링**(`UiKit.Outline`/`OutlinePx` · SDF 스트로크)이 닿아야 함 — 정본이 4/8방향 hard 링으로 흉내 낸
                         text-shadow 는 TMP 언더레이 한 겹으로 못 낸다(T333 3회차 · 등재 절 ⓒ · `.chat-name` 20겹 = 변당 2px 순검정).

빈자리 중 «임자가 정해진 것» 은 KNOWN 에 두어 rc 0 으로 지나간다. 닫았는데 안 지우면 «이제 있다» 로 알린다.

사용:  python3 tools/check_text_shadows.py [--css <style.css 경로>] [--game <Assets/Scripts/Game>] [--list] [--self-test]
rc:    0 = 표의 자리 전부가 (그림자가 있거나 KNOWN) 이고 표의 선택자가 전부 정본에 있다 · 1 = 아니다 · 2 = 정본 CSS 를 못 읽었다
"""
import os
import re
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
CSS_DEFAULT = os.path.join('.wwwww-src', 'web', 'css', 'style.css')
GAME_DEFAULT = os.path.join('Assets', 'Scripts', 'Game')

# ── 정본 선택자 ↔ 클론 자리 ────────────────────────────────────────────────────
# 선택자는 style.css 의 것을 공백 하나로 정규화한 그대로(쉼표 목록 통째로). 표에 없는 선택자 = 미정.
TABLE = {
    # T333 1회차 — 색 버튼 라벨(8336·8504 같은 선택자 · 뒤 규칙 .62 가 이긴다) · 비활성 흰 엠보스(8356) → 공용 PopupKit.Btn
    '.btn.btn:not(.silver):not(.ascend-ready), .modal-card .btn.btn:not(.silver):not(.ascend-ready), .panel .btn.btn:not(.silver):not(.ascend-ready), #equip-sheet .btn.btn:not(.silver):not(.ascend-ready)': ['Ui/Popups.cs@Btn'],
    '.btn.btn.disabled, .modal-card .btn.btn.disabled, .panel .btn.btn.disabled, #equip-sheet .btn.btn.disabled': ['Ui/Popups.cs@Btn'],
    # T333 2회차 — 8381 한 벌 «밝은 종이 위 글자는 흰 엠보스»: 시트 제목 다섯 자리(나머지 선택자는 KNOWN 에 임자와 함께)
    '.sheet-title, .modal-card h3, .modal-card .idet-name, .stat-grid div, .substat-list div, .prob-box div, .shop-reward-pill, .qst-row .qst-name': [
        'Ui/MountSheet.cs#sheet-title', 'Ui/PetPanel.cs#sheet-title', 'Ui/SkillPanel.cs#sheet-title',
        'Ui/ShopSheet.cs#title', 'Ui/AscendPopup.cs#title',
        'Ui/ForgeInfoPopup.cs#idet-name', 'Ui/QuestSheet.cs#qst-name',
    ],
    # T333 3회차 ⓒ — 정본이 «4/8방향 hard 링» 으로 흉내 낸 것: 언더레이 한 겹으로는 못 내니 SDF 스트로크(`ring:`)로 센다
    '#stage-label': ['ring:Ui/Hud.cs#stage-label'],
    '.chat-name, .chat-tag': ['ring:Ui/ChatScreen.cs#name'],
    '.dgd-title': ['ring:Ui/DungeonDetailPopup.cs#title'],
    # T333 3회차 — 8392 두 겹 중 첫 겹(읽히게 만드는 아래 1px 드롭 · 둘째 겹 글로우는 근사로 뺀다 · 표 `league_row` 주석)
    '.league-row .league-name, .league-row .league-rank, .league-score, .equipped-label + * , .chat-preview-name': [
        'Ui/LeagueSheet.cs#rank', 'Ui/LeagueSheet.cs#name',
        # `.league-score` 는 아이콘+수 두 조각(`UiKit.RowTexts`)이라 «이름 → 변수» 꼬리로는 못 좇는다 — 그 행을 세우는 메서드로 센다
        'Ui/LeagueSheet.cs@Row',
    ],
}

# ── 임자가 정해진 빈자리(자리 → 이유) — 닫을 때마다 지운다 ────────────────────────────────
KNOWN = {
    'Ui/ForgeInfoPopup.cs#idet-name': 'T332·T339 의 산 lock 이 쥔 파일 — 정본 8381 `.modal-card .idet-name`(장비 상세 이름) 은 그 lock 뒤',
    'Ui/QuestSheet.cs#qst-name': 'T331 산 lock + 클론에 그 이름 자리가 아직 없다 — 정본 8381 `.qst-row .qst-name`',
}

SHADOW = r'UiKit\.TextShadow'
# ⓒ 4/8방향 hard 링(정본이 text-shadow 로 흉내 낸 키라인) — 언더레이 한 겹으로는 못 내니 SDF 스트로크로 낸다(T104 `UiKit.Outline`/`OutlinePx`).
RING = r'UiKit\.Outline(?:Px)?'
SHADOW_CALL = re.compile(r'\b' + SHADOW + r'\s*\(')
RING_CALL = re.compile(r'\b' + RING + r'\s*\(')
CREATE_CALL = re.compile(r'\.(Text|Label|Bold|Stroked|IconTextRow|Btn)\s*\(\s*[^,()]+,\s*"([^"]+)"')
ASSIGN_TAIL = re.compile(r'([\w\[\]\.]+)\s*=\s*(?:[\w!.()\[\]]+\s*\?\s*)?[\w.]*$')
DECL = re.compile(r'(?<![\w-])text-shadow\s*:\s*([^;}]+)')


def shadow_on(var, call=SHADOW):
    v = re.escape(var)
    return re.compile(r'\b' + call + r'\s*\(\s*' + v + r'\s*,')


def keyline_selectors():
    """check_keyline 의 TABLE 선택자 — 키라인으로 옮긴 자리는 이 자의 몫이 아니다."""
    try:
        sys.path.insert(0, HERE)
        import check_keyline
        return set(check_keyline.TABLE.keys())
    except Exception:
        return set()


# ── 정본 CSS ────────────────────────────────────────────────────────────
def _blank_comments(css):
    def rep(m):
        return ''.join('\n' if ch == '\n' else ' ' for ch in m.group(0))
    return re.sub(r'/\*.*?\*/', rep, css, flags=re.S)


def parse_rules(css_text):
    """[(줄, 선택자, 값)] — `text-shadow` 선언 전부(주석 속은 뺀다 · `none` 포함 · 값은 공백 하나로)."""
    css = _blank_comments(css_text)
    out = []
    for m in re.finditer(r'([^{}]+)\{([^{}]*)\}', css):
        sel = ' '.join(m.group(1).split())
        lead = len(m.group(1)) - len(m.group(1).lstrip())
        line = css[:m.start(1) + lead].count('\n') + 1
        for d in DECL.finditer(m.group(2)):
            out.append((line, sel, ' '.join(d.group(1).split())))
    return out


# ── 클론 ──────────────────────────────────────────────────────────
def _read(path):
    with open(path, encoding='utf-8') as f:
        return f.read()


def _method_body(src, name):
    m = re.search(r'\b(?:static\s+)?[\w<>\[\],\s]+\s' + re.escape(name) + r'\s*\(', src)
    if not m:
        return None
    i = src.find('{', m.end())
    if i < 0:
        return None
    depth = 0
    for j in range(i, len(src)):
        if src[j] == '{':
            depth += 1
        elif src[j] == '}':
            depth -= 1
            if depth == 0:
                return src[i:j + 1]
    return src[i:]


def check_target(game_dir, target):
    """(상태, 설명) — 'ok' | 'missing'(자리는 있는데 그림자 호출 없음) | 'absent'(자리·메서드·파일 없음).

    `ring:` 접두가 붙으면 «언더레이» 가 아니라 «SDF 스트로크»(`UiKit.Outline`/`OutlinePx`)를 센다 — 정본이 4/8방향 hard 링으로
    흉내 낸 text-shadow 는 TMP 언더레이 한 겹으로 못 내기 때문이다(T333 3회차 · 등재 절 ⓒ).
    """
    ring = target.startswith('ring:')
    if ring:
        target = target[len('ring:'):]
    call, call_re, word = (RING, RING_CALL, '링') if ring else (SHADOW, SHADOW_CALL, '그림자')
    file_part, sep, tail = re.match(r'([^#@]+)([#@]?)(.*)', target).groups()
    path = os.path.join(game_dir, file_part)
    if not os.path.isfile(path):
        return 'absent', '파일 없음 ' + file_part
    src = _read(path)
    if sep == '':
        return ('ok' if call_re.search(src) else 'missing'), '파일 전체'
    if sep == '@':
        body = _method_body(src, tail)
        if body is None:
            return 'absent', '메서드 없음 ' + tail + '('
        return ('ok' if call_re.search(body) else 'missing'), '메서드 ' + tail + '( 본문'
    found = False
    for m in CREATE_CALL.finditer(src):
        if m.group(2) != tail:
            continue
        found = True
        head = src[max(0, m.start() - 160):m.start()].replace('\n', ' ')
        a = ASSIGN_TAIL.search(head)
        if a and shadow_on(a.group(1), call).search(src):
            return 'ok', '"%s" → %s 에 %s 호출이 닿는다' % (tail, a.group(1), word)
    if not found:
        return 'absent', '"%s" 이름으로 만드는 자리가 없다' % tail
    return 'missing', '"%s" 에 %s 호출이 안 닿는다' % (tail, word)


# ── 대조 ──────────────────────────────────────────────────────────
def run(css_path, game_dir, table, known, out=print, list_pending=False, keyline=None):
    if not os.path.isfile(css_path):
        out('✗ 정본 CSS 를 못 읽었다: %s (git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)' % css_path)
        return 2
    if keyline is None:
        keyline = keyline_selectors()
    rules = parse_rules(_read(css_path))
    problems = 0
    seen = set()
    n_none = n_key = n_off = n_ok = n_known = 0
    pending = []
    known_now_ok = []
    checked = set()
    for line, sel, val in rules:
        seen.add(sel)
        if val == 'none':
            n_none += 1
            continue
        if sel in keyline:
            n_key += 1
            continue
        if sel not in table:
            pending.append((line, sel, val))
            continue
        targets = table[sel]
        if isinstance(targets, str):
            if (sel, targets) not in checked:
                checked.add((sel, targets)); n_off += 1
            continue
        for t in targets:
            if (sel, t) in checked:
                continue
            checked.add((sel, t))
            state, why = check_target(game_dir, t)
            if state == 'ok':
                n_ok += 1
                if t in known:
                    known_now_ok.append(t)
                continue
            tag = ('링 없음' if t.startswith('ring:') else '그림자 없음') if state == 'missing' else '자리 없음'
            if t in known:
                n_known += 1
                out('· KNOWN(%s)  %s  ← style.css %d %s { text-shadow: %s }  — %s' % (tag, t, line, sel[:70], val[:60], known[t]))
            else:
                problems += 1
                out('✗ %s  %s  ← style.css %d  %s  { text-shadow: %s }  — %s' % (tag, t, line, sel[:70], val[:60], why))
    for sel in table:
        if sel not in seen:
            problems += 1
            out('✗ 표에는 있는데 정본에 없는 선택자: %s  → 정본이 바뀌었다 · TABLE 에서 지우거나 고쳐라' % sel[:90])
    for t in sorted(set(known_now_ok)):
        out('· KNOWN 인데 이제 그림자가 있다: %s  → KNOWN 에서 지워라' % t)
    pend_sel = sorted(set(p[1] for p in pending))
    if list_pending:
        for line, sel, val in pending:
            out('· 미정  style.css %5d  %-64s %s' % (line, sel[:64], val[:80]))
    out('%s check_text_shadows: 정본 text-shadow 선언 %d · none %d · 키라인 몫 %d · 끄는 규칙 %d · 자리 초록 %d · KNOWN 빈자리 %d · 미정 선택자 %d(선언 %d · --list 로 본다) · 문제 %d'
        % ('✓' if problems == 0 else '✗', len(rules), n_none, n_key, n_off, n_ok, n_known, len(pend_sel), len(pending), problems))
    return 0 if problems == 0 else 1


# ── 자기 검사(고장 주입) ──────────────────────────────────────────────────
def self_test():
    fails = []
    tmp = tempfile.mkdtemp(prefix='tshadow_')
    css = os.path.join(tmp, 'style.css')
    game = os.path.join(tmp, 'Game')
    os.makedirs(os.path.join(game, 'Ui'))

    def w(path, text):
        with open(path, 'w', encoding='utf-8') as f:
            f.write(text)

    def cs(text):
        w(os.path.join(game, 'Ui', 'Face.cs'), text)

    def go(table, known, css_text=None, list_pending=False, keyline=frozenset()):
        if css_text is not None:
            w(css, css_text)
        lines = []
        rc = run(css, game, table, known, out=lines.append, list_pending=list_pending, keyline=keyline)
        return rc, '\n'.join(lines)

    def expect(name, cond, detail=''):
        if not cond:
            fails.append(name + (' — ' + detail if detail else ''))

    base_css = ('/* .fake { text-shadow: 0 1px 0 #000; } */\n'
                '.s-a { color: #fff; text-shadow: 0 1px 1px rgba(4,18,52,.62); }\n'
                '.s-b, .s-c { text-shadow: 0 1px 0 rgba(255,255,255,.92); }\n'
                '.s-d { text-shadow: none; }\n'
                '.s-e { text-shadow: -1px 0 #000, 1px 0 #000; }\n'
                '.s-f { box-shadow: 0 1px 0 #000; }\n')
    # ⓐ 파싱 — 주석 속 제외 · none 포함 · box-shadow 제외 · 쉼표 선택자 통째 · 줄 번호
    rules = parse_rules(base_css)
    expect('ⓐ 선언 수', len(rules) == 4, str(len(rules)))
    expect('ⓐ 주석 제외', all(r[1] != '.fake' for r in rules))
    expect('ⓐ box-shadow 제외', all(r[1] != '.s-f' for r in rules))
    expect('ⓐ 쉼표 선택자 통째', any(r[1] == '.s-b, .s-c' for r in rules))
    expect('ⓐ 줄 번호', rules[0][0] == 2, str(rules[0][0]))
    expect('ⓐ 값 정규화', rules[0][2] == '0 1px 1px rgba(4,18,52,.62)', rules[0][2])
    # ⓑ 자리에 그림자가 있다 — @메서드 · #이름(변수에 닿음) · 파일 전체
    cs('class Face { static void B(Transform p){ TextMeshProUGUI a = UiKit.Text(p, "s-b", TextKind.Body, "x"); UiKit.TextShadow(a, "k"); }\n'
       ' static Button Btn(Transform p){ var t = UiKit.Text(p, "label", TextKind.Button, "y"); UiKit.TextShadow(t, "btn_label"); return null; } }')
    table = {'.s-a': ['Ui/Face.cs@Btn'], '.s-b, .s-c': ['Ui/Face.cs#s-b'], '.s-e': ['Ui/Face.cs']}
    rc, out = go(table, {}, base_css)
    expect('ⓑ rc 0', rc == 0, out)
    expect('ⓑ 자리 초록 3', '자리 초록 3' in out, out)
    expect('ⓑ none 1', '· none 1 ·' in out, out)
    # ⓒ 키라인 몫은 뺀다 · 표에 없는 선택자는 미정(막지 않는다 · --list)
    rc, out = go({'.s-a': ['Ui/Face.cs@Btn']}, {}, list_pending=True, keyline=frozenset(['.s-e']))
    expect('ⓒ 미정 rc 0', rc == 0, out)
    expect('ⓒ 키라인 몫 1', '키라인 몫 1' in out, out)
    expect('ⓒ 미정 선택자 1', '미정 선택자 1(선언 1' in out, out)
    expect('ⓒ --list 줄', '· 미정  style.css' in out and '.s-b, .s-c' in out, out)
    # ⓓ 표의 자리에 그림자가 없다 → rc 1 · KNOWN 이면 rc 0 · KNOWN 닫힘 알림
    cs('class Face { static Button Btn(Transform p){ var t = UiKit.Text(p, "label", TextKind.Button, "y"); return null; } }')
    rc, out = go({'.s-a': ['Ui/Face.cs@Btn']}, {})
    expect('ⓓ 그림자 없음 rc 1', rc == 1 and '그림자 없음' in out, out)
    rc, out = go({'.s-a': ['Ui/Face.cs@Btn']}, {'Ui/Face.cs@Btn': 'T999 몫'})
    expect('ⓓ KNOWN rc 0', rc == 0 and 'KNOWN(그림자 없음)' in out, out)
    cs('class Face { static Button Btn(Transform p){ var t = UiKit.Text(p, "label", TextKind.Button, "y"); UiKit.TextShadow(t, "k"); return null; } }')
    rc, out = go({'.s-a': ['Ui/Face.cs@Btn']}, {'Ui/Face.cs@Btn': 'T999 몫'})
    expect('ⓓ KNOWN 닫힘 알림', rc == 0 and 'KNOWN 인데 이제 그림자가 있다' in out, out)
    # ⓔ 자리 없음(이름·메서드·파일) → rc 1
    rc, out = go({'.s-a': ['Ui/Face.cs#nope'], '.s-b, .s-c': ['Ui/Face.cs@Nope'], '.s-e': ['Ui/Gone.cs']}, {})
    expect('ⓔ 자리 없음 rc 1', rc == 1 and out.count('자리 없음') == 3, out)
    # ⓕ 표에는 있는데 정본에 없는 선택자 → rc 1
    rc, out = go({'.s-a': ['Ui/Face.cs@Btn'], '.s-zzz': ['Ui/Face.cs@Btn']}, {})
    expect('ⓕ 정본에 없는 선택자 rc 1', rc == 1 and '정본에 없는 선택자' in out, out)
    # ⓖ 끄는 규칙('—') 은 대조하지 않는다
    rc, out = go({'.s-a': ['Ui/Face.cs@Btn'], '.s-e': '—클론에 자리 없음'}, {})
    expect('ⓖ 끄는 규칙', rc == 0 and '끄는 규칙 1' in out and '자리 초록 1' in out, out)
    # ⓗ CSS 없음 → rc 2
    rc = run(os.path.join(tmp, 'none.css'), game, {}, {}, out=lambda s: None, keyline=frozenset())
    expect('ⓗ CSS 없음 rc 2', rc == 2)
    # ⓘ 진짜 표의 자리 문법 · 진짜 키라인 표를 읽는다
    for sel, ts in TABLE.items():
        if isinstance(ts, str):
            expect('ⓘ 끄는 규칙 문법 ' + sel, ts.startswith('—'))
            continue
        for t in ts:
            expect('ⓘ 자리 문법 ' + t, re.match(r'^(ring:)?[\w/]+\.cs([#@][\w-]+)?$', t) is not None)
    expect('ⓘ check_keyline TABLE 을 읽는다', len(keyline_selectors()) >= 40, str(len(keyline_selectors())))
    # ⓙ ring: 갈래 — 링이 있으면 초록 · 언더레이만 있으면 «링 없음» · 링만 있는 자리를 언더레이로 재면 «그림자 없음»
    cs('class Face { static void R(Transform p){ var a = UiKit.Text(p, "s-e", TextKind.Body, "x"); UiKit.OutlinePx(a, "pp_line", 4f); }\n'
       ' static void S(Transform p){ var b = UiKit.Text(p, "s-b", TextKind.Body, "y"); UiKit.TextShadow(b, "k"); } }')
    rc, out = go({'.s-e': ['ring:Ui/Face.cs#s-e']}, {}, base_css)
    expect('ⓙ 링 초록', rc == 0 and '자리 초록 1' in out, out)
    rc, out = go({'.s-b, .s-c': ['ring:Ui/Face.cs#s-b']}, {})
    expect('ⓙ 언더레이만 있으면 링 없음', rc == 1 and '링 없음' in out, out)
    rc, out = go({'.s-e': ['Ui/Face.cs#s-e']}, {})
    expect('ⓙ 링만 있으면 그림자 없음', rc == 1 and '그림자 없음' in out, out)
    # ⓚ 옛 갈래 `UiKit.Outline(width01)` 도 링으로 센다(Hud 의 #stage-label 이 그것을 쓴다)
    cs('class Face { static void R(Transform p){ var a = UiKit.Text(p, "s-e", TextKind.Body, "x"); UiKit.Outline(a, "stage_outline", .25f); } }')
    rc, out = go({'.s-e': ['ring:Ui/Face.cs#s-e']}, {})
    expect('ⓚ 옛 Outline 갈래', rc == 0 and '자리 초록 1' in out, out)
    if fails:
        print('✗ check_text_shadows --self-test 실패 %d' % len(fails))
        for f in fails:
            print('  · ' + f[:400])
        return 1
    print('✓ check_text_shadows --self-test 24칸 통과')
    return 0


def main(argv):
    css, game, list_pending = CSS_DEFAULT, GAME_DEFAULT, False
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--self-test':
            return self_test()
        if a == '--list':
            list_pending = True; i += 1; continue
        if a == '--css' and i + 1 < len(argv):
            css = argv[i + 1]; i += 2; continue
        if a == '--game' and i + 1 < len(argv):
            game = argv[i + 1]; i += 2; continue
        print('사용: check_text_shadows.py [--css <style.css>] [--game <Assets/Scripts/Game>] [--list] [--self-test]')
        return 2
    return run(css, game, TABLE, KNOWN, list_pending=list_pending)


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
