#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T168 — 정본 자간(`letter-spacing`) 선언 ↔ 클론이 그 자리에 자간을 주는가 대조 자.

정본 `web/css/style.css` 의 `letter-spacing` 선언을 전부 걷고(애니 키프레임 안은 뺀다),
클론 `Assets/Scripts/Game/**/*.cs` 의 그 자리가 **표에서 읽은 자간**을 주는지 본다.

왜 이 자가 있나(T168 실측): 정본 선언 열넷인데 클론에서 자간을 주는 곳은 **넷뿐**이었다.
보스 경고의 한글 부제는 정본이 `.55em` 으로 한 글자씩 벌려 놓은 줄인데 클론은 0 이라 다닥다닥 붙어 있고,
소환 결과 팝업 다섯 줄·던전 클리어 제목도 0 이다. 자간은 PNG 에서 «글자가 좀 붙었네» 로만 보여 채점에 안 걸리고,
`check_keyline`(T109)은 테만 · `check_clip_paths`(T159)는 모양만 보므로 **아무 자도 이 결을 안 봤다**.

«자간을 준다» 의 뜻(규약): 그 자리가 **자간 표 키를 부른다**. 키 이름은 `_ls_em` 으로 끝난다
(정본 값이 em 이고 TMP `characterSpacing` 은 **1/100 em** 이라 환산이 한 군데에만 있어야 한다 — `PetSkillKit` 이 그 꼴이다).
그래서 이 자는 «그 파일이 그 키를 부르는가» 를 본다 — 수치가 코드에 박히면(§1) 아래 «박힌 숫자» 갈래가 따로 잡는다.

  "Ui/File.cs"           파일 안 어디든 자간 증거가 있으면 됨
  "Ui/File.cs@Method"    그 메서드 본문 안에 자간 증거
  "Ui/File.cs$key_ls_em" 그 파일이 자간 표 키 `"key_ls_em"` 을 부른다
  "—<이유>"              정본이 0(자간 없음)이거나 클론에 그 자리가 아직 없다 — 대조하지 않는다

사용:  python3 tools/check_letter_spacing.py [--css <style.css>] [--game <Assets/Scripts/Game>] [--self-test]
rc:    0 = 정본 선언 전부가 표에 있고 · 표의 자리 전부가 (자간이 있거나 KNOWN) · 박힌 숫자 0 · 1 = 아니다 · 2 = 정본 CSS 를 못 읽었다
"""
import os
import re
import sys
import tempfile

CSS_DEFAULT = os.path.join('.wwwww-src', 'web', 'css', 'style.css')
GAME_DEFAULT = os.path.join('Assets', 'Scripts', 'Game')

# ── 정본 선택자 ↔ 클론 자리 ──────────────────────────────────────────────────────────────
TABLE = {
    '.bw-track span': ['Ui/BattleOverlay.cs$bw_track_ls_em'],
    '.bw-sub': ['Ui/BattleOverlay.cs$bw_sub_ls_em'],
    '.equip-cell .cell-lv': ['Ui/ForgeUi.cs$equip_cell_lv_ls_em'],
    '#skill-cutin': ['Ui/SkillCutin.cs$skill_cutin_ls_em'],   # T384 — 컷인 콜아웃(표 SkillCutinUi.json layout)
    '.chat-input-bar input::placeholder': ['Ui/ChatScreen.cs$chat_placeholder_ls_em'],
    '#forge-item-modal .idet-subs .substat-row': ['Ui/ForgeInfoPopup.cs$substat_row_ls_em'],
    '.petup-selrow .btn.silver': ['Ui/PetUpgradePopup.cs$petup_sel_btn_ls_em'],
    '.af-age-star': '—정본이 0(자간 없음)',
    '.dgclear-title': ['Ui/DungeonClearPopup.cs$dgclear_title_ls_em'],
    '.sr-again': ['Ui/SkillSummonResult.cs$sr_again_ls_em'],
    '.sr-title': ['Ui/SkillSummonResult.cs$sr_title_ls_em'],
    '.sr-new': ['Ui/SkillSummonResult.cs$sr_new_ls_em'],
    '.sr-sub': ['Ui/SkillSummonResult.cs$sr_sub_ls_em'],
    '.sr-ok': ['Ui/SkillSummonResult.cs$sr_ok_ls_em'],
    # 정본이 CSS 가 아니라 **JS 인라인**으로 주는 자리(사망 배너) · index.html 의 <style> 안 규칙(부팅 로딩)
    'scene3d.js title.style': ['Ui/BattleOverlay.cs$death_title_ls_em'],
    'scene3d.js subEl.style': ['Ui/BattleOverlay.cs$death_sub_ls_em'],
    '#boot-loading .bl-title': ['Ui/BootLoading.cs'],
}

# ── 임자가 정해진 빈자리(자리 → 이유) — T168 ⓑ 가 붙일 때마다 지운다 ──────────────────────────
KNOWN = {
}

# ── 코드에 박힌 자간(§1 «수치를 코드에 박지 않는다») — 자리 → 임자 ──────────────────────────
HARD_KNOWN = {
}

SPACING_SET = re.compile(r'([\w\[\]\.]*?)\.?characterSpacing\s*=\s*([^;]+);')
EVIDENCE = re.compile(r'characterSpacing\s*=|letterSpacingEm|"[a-z][a-z0-9_]*_ls_em"')
NUM_ONLY = re.compile(r'^[\s\d\.\-\+\*/()f]+$')       # 식별자가 하나도 없으면 «박힌 숫자»
SPACING_DECL = re.compile(r'(?<![\w-])letter-spacing\s*:\s*([^;]+)')


# ── 정본 CSS ──────────────────────────────────────────────────────────────────────────────
def _blank_comments(css):
    def rep(m):
        return ''.join('\n' if ch == '\n' else ' ' for ch in m.group(0))
    return re.sub(r'/\*.*?\*/', rep, css, flags=re.S)


def _blank_keyframes(css):
    """`@keyframes … { … { … } … }` 몸을 같은 길이의 공백으로 — 애니 칸(`0%`·`16%`)은 «자리» 가 아니다."""
    out = list(css)
    for m in re.finditer(r'@(?:-\w+-)?keyframes\b', css):
        i = css.find('{', m.end())
        if i < 0:
            continue
        depth = 0
        for j in range(i, len(css)):
            if css[j] == '{':
                depth += 1
            elif css[j] == '}':
                depth -= 1
                if depth == 0:
                    break
        else:
            j = len(css) - 1
        for k in range(m.start(), j + 1):
            if out[k] != '\n':
                out[k] = ' '
    return ''.join(out)


def parse_rules(css_text):
    """[(줄, 선택자, 값)] — letter-spacing 선언 전부(키프레임 안은 뺀다 · 값 0 도 남긴다)."""
    css = _blank_keyframes(_blank_comments(css_text))
    out = []
    for m in re.finditer(r'([^{}]+)\{([^{}]*)\}', css):
        d = SPACING_DECL.search(m.group(2))
        if not d:
            continue
        sel = ' '.join(m.group(1).split())
        lead = len(m.group(1)) - len(m.group(1).lstrip())
        line = css[:m.start(1) + lead].count('\n') + 1
        out.append((line, sel, ' '.join(d.group(1).split())))
    return out


INLINE_STYLE = re.compile(r"(\w+)\.style\.cssText\s*=\s*'([^']*)'")


def parse_inline(js_text, label):
    """[(줄, «<파일> <변수>.style», 값)] — 정본이 **JS 인라인**으로 주는 자간(사망 배너가 그렇다).

    style.css 만 보면 이 자리들은 «정본에 규칙이 없다» 로 보여 **자가 통째로 지나간다** — T168 1회차가 그래서
    `BattleOverlay` 의 `32f` 를 «근거 없는 수» 로 잘못 적었다(실제 근거는 `scene3d.js` 의 `letter-spacing:.32em`).
    """
    out = []
    for m in INLINE_STYLE.finditer(js_text):
        d = SPACING_DECL.search(m.group(2))
        if not d:
            continue
        line = js_text[:m.start()].count('\n') + 1
        out.append((line, '%s %s.style' % (label, m.group(1)), ' '.join(d.group(1).split())))
    return out


def parse_all(css_path):
    """정본 자간 선언 전부 — `style.css` + `index.html` 의 <style> + `js/*.js` 의 인라인."""
    rules = [('style.css', l, s, v) for l, s, v in parse_rules(_read_text(css_path))]
    web = os.path.dirname(os.path.dirname(os.path.abspath(css_path)))
    html = os.path.join(web, 'index.html')
    if os.path.isfile(html):
        rules += [('index.html', l, s, v) for l, s, v in parse_rules(_read_text(html))]
    js_dir = os.path.join(web, 'js')
    if os.path.isdir(js_dir):
        for fn in sorted(os.listdir(js_dir)):
            if fn.endswith('.js'):
                rules += [(fn, l, s, v) for l, s, v in parse_inline(_read_text(os.path.join(js_dir, fn)), fn)]
    return rules


def _read_text(path):
    with open(path, encoding='utf-8') as f:
        return f.read()


# ── 클론 ──────────────────────────────────────────────────────────────────────────────────
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
    """(상태, 설명) — 'ok' | 'missing'(자리는 있는데 자간 0) | 'absent'(파일·메서드 없음)."""
    m = re.match(r'([^#@$]+)([@$]?)(.*)', target)
    file_part, sep, tail = m.groups()
    path = os.path.join(game_dir, file_part)
    if not os.path.isfile(path):
        return 'absent', '파일 없음 ' + file_part
    src = _read(path)
    if sep == '':
        return ('ok' if EVIDENCE.search(src) else 'missing'), '파일 전체'
    if sep == '@':
        body = _method_body(src, tail)
        if body is None:
            return 'absent', '메서드 없음 ' + tail + '('
        return ('ok' if EVIDENCE.search(body) else 'missing'), '메서드 ' + tail + '( 본문'
    if not tail.endswith('_ls_em'):
        return 'absent', '표 키는 «_ls_em» 으로 끝나야 한다(규약): ' + tail
    if ('"' + tail + '"') in src:
        return 'ok', '자간 표 키 "%s" 를 부른다' % tail
    return 'missing', '자간 표 키 "%s" 를 아무 데서도 안 부른다 — 그 줄은 자간 0 이다' % tail


def hard_coded(game_dir, known, out):
    """코드에 박힌 자간(§1) — 오른쪽이 숫자뿐인 `characterSpacing =` 를 센다."""
    bad = 0
    for root, _dirs, files in os.walk(game_dir):
        for fn in sorted(files):
            if not fn.endswith('.cs'):
                continue
            path = os.path.join(root, fn)
            rel = os.path.relpath(path, game_dir).replace(os.sep, '/')
            src = _read(path)
            for m in SPACING_SET.finditer(src):
                rhs = m.group(2).strip()
                if not NUM_ONLY.match(rhs):
                    continue
                spot = rel + ':' + (m.group(1) + '.' if m.group(1) else '') + 'characterSpacing'
                line = src[:m.start()].count('\n') + 1
                if spot in known:
                    out('· KNOWN(박힌 숫자)  %s  %s:%d = %s  — %s' % (spot, rel, line, rhs, known[spot]))
                else:
                    bad += 1
                    out('✗ 박힌 숫자  %s:%d  characterSpacing = %s  → 자간 값은 표(`*_ls_em`)에서 읽어라(§1)' % (rel, line, rhs))
    return bad


# ── 대조 ──────────────────────────────────────────────────────────────────────────────────
def run(css_path, game_dir, table, known, hard_known, out=print):
    if not os.path.isfile(css_path):
        out('✗ 정본 CSS 를 못 읽었다: %s (git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)' % css_path)
        return 2
    rules = parse_all(css_path)
    problems = 0
    seen = set()
    n_off = n_ok = n_known = 0
    known_now_ok = []
    for src_name, line, sel, val in rules:
        seen.add(sel)
        if sel not in table:
            problems += 1
            out('✗ 표에 없는 정본 자간  %s %d  %s  { letter-spacing: %s }  → TABLE 에 짝을 더해라' % (src_name, line, sel, val))
            continue
        targets = table[sel]
        if isinstance(targets, str):
            n_off += 1
            continue
        for t in targets:
            state, why = check_target(game_dir, t)
            if state == 'ok':
                n_ok += 1
                if t in known:
                    known_now_ok.append(t)
                continue
            tag = '자간 없음' if state == 'missing' else '자리 없음'
            if t in known:
                n_known += 1
                out('· KNOWN(%s)  %s  ← %s %d %s { %s }  — %s' % (tag, t, src_name, line, sel, val, known[t]))
            else:
                problems += 1
                out('✗ %s  %s  ← %s %d  %s  { %s }  — %s' % (tag, t, src_name, line, sel, val, why))
    for sel in table:
        if sel not in seen:
            problems += 1
            out('✗ 표에는 있는데 정본에 없는 선택자: %s  → 정본이 바뀌었다 · TABLE 에서 지우거나 고쳐라' % sel)
    for t in sorted(set(known_now_ok)):
        out('· KNOWN 인데 이제 자간이 있다: %s  → KNOWN 에서 지워라' % t)
    problems += hard_coded(game_dir, hard_known, out)
    out('%s check_letter_spacing: 정본 자간 %d(대조 안 함 %d) · 자리 초록 %d · KNOWN 빈자리 %d · 문제 %d'
        % ('✓' if problems == 0 else '✗', len(rules), n_off, n_ok, n_known, problems))
    return 0 if problems == 0 else 1


# ── 자기 검사(고장 주입) ────────────────────────────────────────────────────────────────────
def self_test():
    css = """
/* 주석 { } */
.a-key { letter-spacing: .55em; }
.b-file { letter-spacing: .14em; }
.c-method { letter-spacing: .06em; }
.d-zero { letter-spacing: 0; }
@keyframes bwsub {
    0%  { opacity: 0; letter-spacing: 1.4em; }
    16% { opacity: 1; letter-spacing: .55em; }
}
.e-miss { letter-spacing: .04em; }
"""
    cs = """
namespace X {
    class Sheet {
        void Build(Transform p) {
            TextMeshProUGUI a = UiKit.Text(p, "a", TextKind.Sub, "x", "ink");
            UiText.Track(a, "bw_sub_ls_em");
            TextMeshProUGUI m = UiKit.Text(p, "m", TextKind.Sub, "y", "ink");
            m.characterSpacing = Style.Px("mq_ls_em") * 100f;
        }
        void Bare(Transform p) {
            TextMeshProUGUI b = UiKit.Text(p, "b", TextKind.Sub, "z", "ink");
        }
        void Hard(Transform p) {
            title.characterSpacing = 32f;
        }
    }
}
"""
    fails = []

    def expect(name, table, known, hard, want, css_text=css, cs_text=cs):
        with tempfile.TemporaryDirectory() as d:
            os.makedirs(os.path.join(d, 'Ui'))
            with open(os.path.join(d, 'style.css'), 'w', encoding='utf-8') as f:
                f.write(css_text)
            with open(os.path.join(d, 'Ui', 'Sheet.cs'), 'w', encoding='utf-8') as f:
                f.write(cs_text)
            lines = []
            rc = run(os.path.join(d, 'style.css'), d, table, known, hard, out=lines.append)
            if rc != want:
                fails.append('%s: rc %d ≠ %d\n  ' % (name, rc, want) + '\n  '.join(lines))
            return lines

    base = {
        '.a-key': ['Ui/Sheet.cs$bw_sub_ls_em'], '.b-file': ['Ui/Sheet.cs'],
        '.c-method': ['Ui/Sheet.cs@Build'], '.d-zero': '—정본이 0',
        '.e-miss': ['Ui/Sheet.cs$nope_ls_em'],
    }
    hard = {'Ui/Sheet.cs:title.characterSpacing': '임자'}
    # 1 다 있으면 0 (없는 키는 KNOWN · 박힌 숫자도 KNOWN)
    expect('전부 초록', base, {'Ui/Sheet.cs$nope_ls_em': '임자'}, hard, 0)
    # 2 KNOWN 을 지우면 «자간 없음» → 1
    lines = expect('자간 없음 → 1', base, {}, hard, 1)
    if not any('그 줄은 자간 0 이다' in l for l in lines):
        fails.append('«자간 0» 이유가 안 나온다')
    # 3 박힌 숫자를 KNOWN 에서 빼면 → 1
    lines = expect('박힌 숫자 → 1', base, {'Ui/Sheet.cs$nope_ls_em': '임자'}, {}, 1)
    if not any('박힌 숫자' in l and '32f' in l for l in lines):
        fails.append('박힌 숫자를 못 잡는다')
    # 4 표에 없는 정본 선언 → 1
    expect('표에 없는 선언 → 1', base, {'Ui/Sheet.cs$nope_ls_em': '임자'}, hard, 1,
           css_text=css + '.z-new { letter-spacing: .1em; }\n')
    # 5 표에는 있는데 정본에 없다 → 1
    t = dict(base); t['.gone'] = ['Ui/Sheet.cs']
    expect('정본에 없는 선택자 → 1', t, {'Ui/Sheet.cs$nope_ls_em': '임자'}, hard, 1)
    # 6 메서드 본문에 자간이 없다 → 1
    t = dict(base); t['.c-method'] = ['Ui/Sheet.cs@Bare']
    expect('메서드 민글자 → 1', t, {'Ui/Sheet.cs$nope_ls_em': '임자'}, hard, 1)
    # 7 고장 주입: 표 키 호출을 지우면 1
    broken = cs.replace('UiText.Track(a, "bw_sub_ls_em");', '')
    expect('표 키 호출 지움 → 1', base, {'Ui/Sheet.cs$nope_ls_em': '임자'}, hard, 1, cs_text=broken)
    # 8 키 이름 규약(_ls_em) 을 안 지키면 «자리 없음»
    t = dict(base); t['.a-key'] = ['Ui/Sheet.cs$bw_sub']
    lines = expect('키 규약 어김 → 1', t, {'Ui/Sheet.cs$nope_ls_em': '임자'}, hard, 1)
    if not any('_ls_em' in l and '규약' in l for l in lines):
        fails.append('키 규약 갈래가 안 나온다')
    # 9 KNOWN 인데 이제 있다 → 알리기만(rc 0)
    lines = expect('KNOWN 해소 알림', base, {'Ui/Sheet.cs$nope_ls_em': '임자', 'Ui/Sheet.cs$bw_sub_ls_em': '옛 임자'}, hard, 0)
    if not any('KNOWN 인데 이제 자간이 있다' in l for l in lines):
        fails.append('KNOWN 해소 알림이 안 나온다')
    # 10 파서: 키프레임 안(0%·16%)은 안 센다 · 값 0 도 남긴다 · 줄 번호를 지킨다
    rules = parse_rules(css)
    sels = [r[1] for r in rules]
    if sels != ['.a-key', '.b-file', '.c-method', '.d-zero', '.e-miss']:
        fails.append('파서 선택자(키프레임이 섞였다): %r' % sels)
    if rules[0][0] != 3 or rules[3][2] != '0':
        fails.append('파서 줄번호/0 값: %r' % (rules[:4],))
    # 11 «표에서 읽는 대입» 은 박힌 숫자가 아니다
    if hard_coded(os.path.dirname(__file__) or '.', {}, lambda s: None) < 0:
        fails.append('hard_coded 가 음수를 낸다')
    # 12 정본 CSS 가 없으면 2
    if run('/nonexistent/style.css', '.', base, {}, {}, out=lambda s: None) != 2:
        fails.append('정본 없음 rc 2')
    if fails:
        print('✗ check_letter_spacing --self-test 실패 %d' % len(fails))
        for f in fails:
            print('  - ' + f)
        return 1
    print('✓ check_letter_spacing --self-test 14칸 통과')
    return 0


def main(argv):
    css, game = CSS_DEFAULT, GAME_DEFAULT
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--self-test':
            return self_test()
        if a == '--css' and i + 1 < len(argv):
            css = argv[i + 1]; i += 2; continue
        if a == '--game' and i + 1 < len(argv):
            game = argv[i + 1]; i += 2; continue
        print('사용: check_letter_spacing.py [--css <style.css>] [--game <Assets/Scripts/Game>] [--self-test]')
        return 2
    return run(css, game, TABLE, KNOWN, HARD_KNOWN)


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
