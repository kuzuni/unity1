#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T365 — 정본 상자 테(`border` · `border-top/-bottom/-left/-right`) ↔ 클론 테 호출 대조 자(T109 `check_keyline` · T331 `check_box_shadows` 꼴).

정본 `style.css` 의 테 선언을 전부 걷어 **선택자×변** 으로 접는다 — 쉼표 목록은 선택자마다 하나씩, 같은 (선택자, 변)의 뒤 규칙이
앞을 덮는다(계단 · T33 29회차가 다섯을 세었다). 값은 폭 단(`--ol1`~`--ol4` = 1·2·3·4 CSS px · `--cellb` = ol3 내림 · `none`/`0` ·
직접값 px/rem)과 색(`--pp-line` · `--rc` · hex · rgba)으로 가른다.

클론 자리 짝은 TABLE 하나가 쥔다(선택자 → 자리 목록):
  "Ui/File.cs#name"     그 이름으로 테를 세우는 호출이 있어야 함 — `PopupKit.Outlined(p, "name", …)` · `UiKit.Line(p, "name", …)` ·
                        `PetSkillKit.Framed(p, "name", …)` · `PetSkillKit.Orb(p, "name", …)` · `UiKit.Rounded(p, "name", …)`
  "Ui/File.cs@Method"   도우미 메서드 본문 안에 테 호출이 있어야 함(버튼·토스트처럼 한 곳이 여럿을 세우는 자리)
  "Ui/File.cs"          파일 안 어디든 테 호출 하나
  "—<이유>"             대조하지 않는다(정본이 «끄는» 규칙 none · 클론에 그 자리 자체가 없음)
표에 없는 선택자 = 미정(막지 않는다 · `--list` 로 본다 · 회차마다 표에 더한다). 빈자리 중 임자가 정해진 것은 KNOWN 에 두어 rc 0.
폭 단이 맞는가(호출의 px 인자 ↔ 단)는 2회차 몫이다 — 이 회차는 «자리가 있는가» 와 «정본 186 의 요약» 까지.

사용:  python3 tools/check_box_borders.py [--css <style.css>] [--game <Assets/Scripts/Game>] [--list] [--self-test]
rc:    0 = 표의 선택자가 전부 정본에 있고 · 표의 자리 전부가 (테 호출이 있거나 KNOWN) · 1 = 아니다 · 2 = 정본 CSS 를 못 읽었다
"""
import os
import re
import sys
import tempfile

CSS_DEFAULT = os.path.join('.wwwww-src', 'web', 'css', 'style.css')
GAME_DEFAULT = os.path.join('Assets', 'Scripts', 'Game')

# ── 정본 선택자 ↔ 클론 자리 ───────────────────────────────────────────────────
# 선택자는 style.css 의 것을 공백 하나로 정규화한 그대로(쉼표 목록은 갈라 하나씩). «변» 이 여럿이면 «선택자|변» 꼴(변 = top/bottom/left/right · 없으면 border 통째).
TABLE = {
    # T33 29회차가 실물로 연 셋(SkillSummonResult 500·509·590 Framed line1_px + 같은 색)
    '.sr-qty': ['Ui/SkillSummonResult.cs#sr-qty'],
    '.sr-dup': ['Ui/SkillSummonResult.cs#sr-dup'],
    '.sr-hint': ['Ui/SkillSummonResult.cs#sr-hint'],
    # 상단바·탭바·시트·채팅 미리보기의 한 줄 테 — UiKit.Line(p, "line", …)
    '#topbar|bottom': ['Ui/Hud.cs#line'],
    '#equip-sheet|top': ['Ui/UiRoot.cs#line'],
    '#chat-preview|top': ['Ui/UiRoot.cs#line'],
    # 카드 면 — PopupKit.Outlined(p, "face", …)
    '.league-row': ['Ui/LeagueSheet.cs#face'],
    '.pass-banner': ['Ui/PassPopup.cs#face'],
    '.shop-banner': ['Ui/ShopSheet.cs#face'],
    # 펫·스킬 타일 면 — PetSkillKit.Framed(p, "tile-face", …)
    '.pet-tile .tile-face': ['Ui/PetPanel.cs#tile-face'],
    # T365 ⓐ — 펫 카드 왼쪽 등급색 띠(1162 border-left var(--ol4)) · 클론에 pet-card 이름 0 · PetPanel.cs 는 T331 lock
    '.pet-card|left': ['Ui/PetPanel.cs#pet-card'],
}

# ── 임자가 정해진 빈자리(자리 → 이유) — 닫을 때마다 지운다 ───────────────────────────────
KNOWN = {
    'Ui/PetPanel.cs#pet-card': 'T365 ⓐ — 정본 1162 `.pet-card { border-left: var(--ol4) solid var(--rc) }` 등급색 띠 · 카탈로그 line4_px(8)는 1회차에 더했고 자리는 PetPanel.cs(T331 lock) 뒤 2회차',
}

BORDER_CALL = re.compile(r'\b(?:PopupKit\.Outlined|UiKit\.Line|UiKit\.Rounded|PetSkillKit\.Framed|PetSkillKit\.Orb)\s*\(')
CREATE_CALL = re.compile(r'\b(?:PopupKit\.Outlined|UiKit\.Line|UiKit\.Rounded|PetSkillKit\.Framed|PetSkillKit\.Orb)\s*\(\s*[^,()]+,\s*"([^"]+)"')
DECL = re.compile(r'(?<![-\w])border(-top|-bottom|-left|-right)?\s*:\s*([^;}]+)')
SIDES = ('top', 'bottom', 'left', 'right')


def _blank_comments(css):
    def rep(m):
        return ''.join('\n' if ch == '\n' else ' ' for ch in m.group(0))
    return re.sub(r'/\*.*?\*/', rep, css, flags=re.S)


def width_tier(value):
    """값 → 폭 단: 'ol1'..'ol4' · 'ol15' · 'cellb' · 'none' · 'px:<수단위>'(직접값)."""
    v = value.strip()
    m = re.search(r'var\(--(ol\d+|cellb)\b', v)
    if m:
        return m.group(1)
    if re.match(r'^(none|0)(\s|$)', v) or v == '0':
        return 'none'
    m = re.match(r'^([0-9.]+(?:px|rem|em))\b', v)
    if m:
        return 'px:' + m.group(1)
    return 'px:' + v.split()[0]


def color_of(value):
    """값 → 색 표기(정본 낱말 그대로 · 없으면 '')."""
    v = value.strip()
    if re.search(r'color-mix\(', v):
        return 'color-mix'
    m = re.search(r'var\(--(rc|pp-line|c)\b[^)]*\)', v)
    if m:
        return '--' + m.group(1)
    m = re.search(r'(#[0-9a-fA-F]{3,8}|rgba?\([^)]*\))', v)
    return m.group(1) if m else ''


def parse_rules(css_text):
    """[(줄, 선택자, 변, 원값)] — 정본 순서 · 쉼표 목록은 선택자마다."""
    css = _blank_comments(css_text)
    out = []
    for m in re.finditer(r'([^{}]+)\{([^{}]*)\}', css):
        body = m.group(2)
        decls = list(DECL.finditer(body))
        if not decls:
            continue
        sels = [' '.join(s.split()) for s in m.group(1).split(',')]
        sels = [s for s in sels if s]
        for d in decls:
            line = css.count('\n', 0, m.start(2) + d.start()) + 1
            side = (d.group(1) or '').lstrip('-')
            for s in sels:
                out.append((line, s, side, d.group(2).strip()))
    return out


def collapse(rows):
    """(선택자, 변) → 마지막 선언(뒤 규칙이 앞을 덮는다). 돌려주는 값: [(선택자, 변, 줄, 원값)] 정본 순 · 덮인 수."""
    final = {}
    order = []
    overridden = 0
    for line, sel, side, val in rows:
        k = (sel, side)
        if k in final:
            overridden += 1
        else:
            order.append(k)
        final[k] = (line, val)
    return [(sel, side, final[(sel, side)][0], final[(sel, side)][1]) for sel, side in order], overridden


def table_key(sel, side):
    return sel + ('|' + side if side else '')


# ── 클론 ─────────────────────────────────────────────────────────────
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
    """(상태, 설명) — 'ok' | 'missing'(자리는 있는데 테 호출 없음) | 'absent'(요소·메서드·파일 없음) | 'skip'."""
    if target.startswith('—'):
        return 'skip', target[1:]
    file_part, sep, tail = re.match(r'([^#@]+)([#@]?)(.*)', target).groups()
    path = os.path.join(game_dir, file_part)
    if not os.path.isfile(path):
        return 'absent', '파일 없음 ' + file_part
    src = _read(path)
    if sep == '':
        return ('ok' if BORDER_CALL.search(src) else 'missing'), '파일 전체'
    if sep == '@':
        body = _method_body(src, tail)
        if body is None:
            return 'absent', '메서드 없음 ' + tail + '('
        return ('ok' if BORDER_CALL.search(body) else 'missing'), '메서드 ' + tail + '( 본문'
    for m in CREATE_CALL.finditer(src):
        if m.group(1) == tail:
            return 'ok', '"%s" 를 테 호출로 세운다' % tail
    return 'absent', '"%s" 이름으로 테를 세우는 호출이 없다' % tail


# ── 대조 ─────────────────────────────────────────────────────────────
def run(css_text, game_dir, table, known, out=print, list_pending=False):
    rows = parse_rules(css_text)
    final, overridden = collapse(rows)
    tiers = {}
    for sel, side, line, val in final:
        t = width_tier(val)
        t = 'direct' if t.startswith('px:') else t
        tiers[t] = tiers.get(t, 0) + 1
    problems = 0
    n_ok = n_known = n_skip = 0
    known_now_ok = []
    keys_in_css = {table_key(sel, side) for sel, side, _, _ in final}
    pending = [(sel, side, line, val) for sel, side, line, val in final if table_key(sel, side) not in table]
    for key, targets in table.items():
        if key not in keys_in_css:
            out('  ✗ 표의 선택자가 정본에 없다: %s' % key)
            problems += 1
            continue
        for t in targets:
            state, why = check_target(game_dir, t)
            if state == 'ok':
                n_ok += 1
                if t in known:
                    known_now_ok.append(t)
            elif state == 'skip':
                n_skip += 1
            elif t in known:
                n_known += 1
            else:
                out('  ✗ %s → %s: %s(%s)' % (key, t, '테 없음' if state == 'missing' else '자리 없음', why))
                problems += 1
    for t in known_now_ok:
        out('  · KNOWN 인데 이제 테가 있다: %s → KNOWN 에서 지워라' % t)
    if list_pending:
        for sel, side, line, val in pending:
            out('· 미정  style.css %5d  %-52s %-6s  %-8s %s' % (line, sel[:52], side or 'all', width_tier(val), color_of(val)))
    tier_txt = ' · '.join('%s %d' % (k, tiers[k]) for k in ('ol3', 'ol2', 'none', 'ol1', 'cellb', 'ol4', 'ol15', 'direct') if k in tiers)
    out('%s check_box_borders: 정본 테 선언 %d → 선택자×변 %d(덮인 %d) · 단 %s · 표 자리 초록 %d · KNOWN %d · 건너뜀 %d · 미정 %d(--list) · 문제 %d'
        % ('✓' if problems == 0 else '✗', len(rows), len(final), overridden, tier_txt, n_ok, n_known, n_skip, len(pending), problems))
    return 1 if problems else 0


def self_test():
    css = """
/* 주석 { border: var(--ol4) solid #000; } */
.a, .b { border: var(--ol1) solid #444c56; }
.a { border: var(--ol3) solid var(--pp-line); }
.c { border-top: var(--ol2) solid #30363d; border-left: 2px solid #000; }
.d { border: none; }
.e { border: var(--cellb) solid color-mix(in srgb, var(--rc, #6b3538) 80%, #000); }
.f { border: .34rem solid rgba(255,255,255,.34); }
"""
    cs = """
namespace X {
    public static class Popups {
        public static void Toast(Transform p) { PopupKit.Outlined(p, "face", "pp_paper", 4f, PopupKit.Line); }
        public static void Bare(Transform p) { }
    }
    class Sheet {
        void Build(Transform p) {
            UiKit.Line(p, "line", "pp_line", PopupKit.Line3, true);
            PetSkillKit.Framed(p, "tile-face", Color.white, 3f, 2f);
        }
    }
}
"""
    rows = parse_rules(css)
    final, overridden = collapse(rows)
    checks = []
    checks.append(('선언 8(쉼표 목록은 선택자마다 · .c 는 변 둘) · 주석 속 선언은 안 센다', len(rows) == 8))
    checks.append(('같은 (선택자, 변)의 뒤 규칙이 앞을 덮는다 → 7 · 덮인 1', len(final) == 7 and overridden == 1))
    tiers = {(s, d): width_tier(v) for s, d, _, v in final}
    checks.append(('폭 단: 덮인 .a 는 ol3 · .b 는 ol1', tiers[('.a', '')] == 'ol3' and tiers[('.b', '')] == 'ol1'))
    checks.append(('변: .c 는 top(ol2) · left(직접값 2px)', tiers[('.c', 'top')] == 'ol2' and tiers[('.c', 'left')] == 'px:2px'))
    checks.append(('none · cellb · 직접값 rem', tiers[('.d', '')] == 'none' and tiers[('.e', '')] == 'cellb' and tiers[('.f', '')] == 'px:.34rem'))
    vals = {(s, d): v for s, d, _, v in final}
    checks.append(('색: --pp-line · #30363d · color-mix · rgba', color_of(vals[('.a', '')]) == '--pp-line' and color_of(vals[('.c', 'top')]) == '#30363d'
                   and color_of(vals[('.e', '')]) == 'color-mix' and color_of(vals[('.f', '')]).startswith('rgba(')))
    tmp = tempfile.mkdtemp()
    ui = os.path.join(tmp, 'Ui'); os.makedirs(ui)
    with open(os.path.join(ui, 'Face.cs'), 'w', encoding='utf-8') as f:
        f.write(cs)
    logs = []
    good = {'.a': ['Ui/Face.cs#line'], '.b': ['Ui/Face.cs@Toast'], '.c|top': ['Ui/Face.cs#tile-face'], '.c|left': ['—직접값 자리 · 클론에 없음'], '.d': ['—정본이 끄는 규칙(none)'], '.f': ['Ui/Face.cs']}
    checks.append(('맞는 표는 rc 0', run(css, tmp, good, {}, logs.append) == 0))
    checks.append(('미정은 막지 않는다(.e 가 표에 없다)', any('미정 1' in l for l in logs)))
    bad = dict(good); bad['.e'] = ['Ui/Face.cs#nothing']
    checks.append(('자리 없음은 rc 1', run(css, tmp, bad, {}, logs.append) == 1))
    checks.append(('KNOWN 이면 rc 0', run(css, tmp, bad, {'Ui/Face.cs#nothing': '임자 있음'}, logs.append) == 0))
    bad2 = dict(good); bad2['.b'] = ['Ui/Face.cs@Bare']
    checks.append(('메서드 본문에 테 호출이 없으면 rc 1', run(css, tmp, bad2, {}, logs.append) == 1))
    bad3 = dict(good); bad3['.zzz'] = ['Ui/Face.cs']
    checks.append(('표의 선택자가 정본에 없으면 rc 1', run(css, tmp, bad3, {}, logs.append) == 1))
    logs2 = []
    run(css, tmp, {'.a': ['Ui/Face.cs#line']}, {'Ui/Face.cs#line': 'x'}, logs2.append)
    checks.append(('KNOWN 인데 이제 있다 → 알린다', any('이제 테가 있다' in l for l in logs2)))
    failed = [n for n, ok in checks if not ok]
    for n, ok in checks:
        print(('  ✓ ' if ok else '  ✗ ') + n)
    print(('✓' if not failed else '✗') + ' check_box_borders --self-test %d칸 %s' % (len(checks), '통과' if not failed else '실패 %d' % len(failed)))
    return 0 if not failed else 1


def main(argv):
    css_path, game_dir, list_pending = CSS_DEFAULT, GAME_DEFAULT, False
    i = 1
    while i < len(argv):
        a = argv[i]
        if a == '--self-test':
            return self_test()
        if a == '--css': css_path = argv[i + 1]; i += 2; continue
        if a == '--game': game_dir = argv[i + 1]; i += 2; continue
        if a == '--list': list_pending = True; i += 1; continue
        print('모르는 인자: ' + a); return 2
    if not os.path.isfile(css_path):
        print('✗ 정본 CSS 를 못 읽었다: %s (git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)' % css_path)
        return 2
    return run(_read(css_path), game_dir, TABLE, KNOWN, list_pending=list_pending)


if __name__ == '__main__':
    sys.exit(main(sys.argv))
