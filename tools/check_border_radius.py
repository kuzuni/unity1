#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T345 — 정본 `border-radius` 선언 ↔ 클론 둥근 모서리 대조 자.

정본 `web/css/style.css` 의 `border-radius` 선언을 전부 걷고(주석 밖 · 애니 키프레임 밖 · 20회차 파서),
표에 짝이 적힌 자리마다 **세 겹**을 본다:
  ① 표(`Assets/Forge/Resources/RadiusUi.json`)에 그 키가 있다
  ② 표값이 정본 값과 같다(단위는 키 꼬리 — `_r_rem` ↔ `Nrem` · `_r_w` ↔ `calc(var(--app-w) * k)`)
  ③ 그 자리 파일이 그 키를 부른다(`"키"` 가 파일 안에 있다) — 수치가 코드에 박혀 있으면 ③ 이 빈다

왜 이 자가 있나(T33 20회차 ⓡ 실측): 표값을 읽는 자리 50여 곳은 정본과 하나도 안 어긋났는데, `rem * 0.6f` 꼴 **리터럴** 자리 열 곳이
어긋나 있었다(채팅 말풍선 .42→.6 · 공유 카드 0→.6 · 뒤로 버튼 .28→.45 …). 값이 코드에 박히면 자가 못 보므로(§1) 표로 옮기고 이 자가 지킨다.

TABLE 값의 꼴:
  ['Ui/File.cs$key_r_rem']   그 파일이 표 키를 부르고 · 표값 = 정본 값
  ['Ui/File.cs$key_r_rem@PetSkillUi.json']  키가 다른 곁 표(같은 Resources 폴더)에 있는 자리 — 이미 표값을 읽는 자리를 짝만 적을 때
  ['Ui/File.cs@Method']      그 메서드 본문에 둥근 모서리 증거(`Circle(`·`RadiusUi.`·표 키) — 정본 `50%`(원) 자리에 쓴다
  ['Ui/File.cs']             파일 어디든 증거
  '✓<까닭>'                  20회차 ⓡ 가 눈으로 견줘 맞는 자리(표값 자리 · 알약 동치(결정 543) · 맞는 리터럴) — 대조하지 않는다
  '—<까닭>'                  죽은 CSS(정본 렌더 줄 0) · 클론에 그 자리가 아직 없다 — 대조하지 않는다
TABLE 에 없는 선택자는 «미정» — 알리기만 한다(`--list` 로 본다) · 문제로 안 센다.

사용:  python3 tools/check_border_radius.py [--css <style.css>] [--game <Assets/Scripts/Game>] [--table <RadiusUi.json>] [--list] [--self-test]
rc:    0 = 표의 자리 전부가 (세 겹이 맞거나 KNOWN) · 정본에 없는 선택자 0 · 1 = 아니다 · 2 = 정본 CSS 를 못 읽었다
"""
import json
import os
import re
import sys
import tempfile

CSS_DEFAULT = os.path.join('.wwwww-src', 'web', 'css', 'style.css')
GAME_DEFAULT = os.path.join('Assets', 'Scripts', 'Game')
TABLE_DEFAULT = os.path.join('Assets', 'Forge', 'Resources', 'RadiusUi.json')

# ── 정본 선택자 ↔ 클론 자리 ──────────────────────────────────────────────────────────────
TABLE = {
    # 어긋난 리터럴 열 자리(20회차 ⓡ) — 표로 옮긴다(T345 ⓑ · 각 파일의 산 lock 뒤)
    '.chat-bubble': ['Ui/ChatScreen.cs$chat_bubble_r_rem'],
    '.chat-share-card': ['Ui/ChatScreen.cs$chat_share_card_r_rem'],
    '.chat-input-bar input': ['Ui/ChatScreen.cs$chat_input_r_rem'],
    '.chat-input-bar .btn.round': ['Ui/ChatScreen.cs$chat_round_btn_r_rem'],
    '.chat-share-side .icon-circle.sm': ['Ui/ChatScreen.cs$chat_share_avatar_r_rem'],
    '.idet-subs': ['Ui/ForgeInfoPopup.cs$idet_subs_r_rem'],
    '.upg-progress': ['Ui/ForgeInfoPopup.cs$upg_progress_r_rem'],
    '.af-spinner': ['Ui/ForgeAutoPopup.cs$af_spinner_r_rem'],
    '.af-dd-list': ['Ui/ForgeAutoPopup.cs$af_dd_list_r_rem'],
    '.btn.back-btn': ['Ui/Popups.cs$back_btn_r_w'],
    '.back-btn': '✓`.btn.back-btn` 5189 가 덮는다(정본 뒤로 버튼은 늘 .btn 이다) — 위 줄이 그 자리',
    '.league-back-btn': '✓LeagueSheet 뒤로 버튼 .45rem(20회차 ⓡ 리그 8 자리 ✓)',
    # 판(면) 자체가 없는 후보 3 — 화면 PNG 로 가른 뒤 등재 또는 수리(T345 ⓒ)
    # ⓒ 판 없음 후보 — 2회차 판정: 정본 렌더 줄(ui.js 2232)은 `.idet-subs` 안뿐이고 3708 `.idet-subs .substat-row { border: none; background: none }` 이 판을 걷는다 → 클론(ForgeInfoPopup 의 민글자 행)이 맞다
    '.substat-row': '✓정본 3708 `.idet-subs .substat-row` 가 배경·테를 걷는다(렌더 줄 ui.js 2232 는 .idet-subs 안뿐) — 클론 ForgeInfoPopup 민글자 행 그대로',
    # ⓒ 판 없음 후보 — 2회차 판정: 클론엔 이미 `MountUpgradePopup` 재료 칩이 `PetSkillKit.Framed(… mtup_chip_r_rem …)` 로 서 있다(정본 .5rem)
    '.mat-chip': ['Ui/MountUpgradePopup.cs$mtup_chip_r_rem@PetSkillUi.json'],
    '.tech-tree-label .tech-tree-node-time': '—판 없음 후보(정본 pp-ink 위 초록 글자 .6rem · ui.js 5420) — ⓒ PNG 판정 뒤 · TechPanel.cs 는 T335·T342 lock',
    # 죽은 CSS(정본 ui.js·index.html 에 자취 0 · 20회차 ⓡ)
    '.stat-grid': '—죽은 CSS(정본 렌더 줄 0)',
    '.hatch-slot': '—죽은 CSS(정본 렌더 줄 0)',
    '.egg-chip': '—죽은 CSS(정본 렌더 줄 0)',
}

# ── 임자가 정해진 빈자리(자리 → 이유) — T345 ⓑ 가 붙일 때마다 지운다 ──────────────────────────
KNOWN = {
    'Ui/ForgeInfoPopup.cs$idet_subs_r_rem': 'T339·T332 가 ForgeInfoPopup.cs 를 쥐었다 — 그 lock 뒤 T345 ⓑ(지금은 rem*0.6)',
    'Ui/ForgeInfoPopup.cs$upg_progress_r_rem': 'T339·T332 lock 뒤 T345 ⓑ(지금은 rem*0.5)',
    'Ui/ForgeAutoPopup.cs$af_spinner_r_rem': 'T339 가 ForgeAutoPopup.cs 를 쥐었다 — 그 lock 뒤 T345 ⓑ(지금은 rem*0.3)',
    'Ui/ForgeAutoPopup.cs$af_dd_list_r_rem': 'T339 lock 뒤 T345 ⓑ(지금은 rem*0.3)',
    'Ui/Popups.cs$back_btn_r_w': 'T331·T333·T335 가 Popups.cs 를 쥐었다 — 그 lock 뒤 T345 ⓑ(지금은 Rem*0.45 = 리그 뒤로 버튼 값)',
}

RADIUS_DECL = re.compile(r'(?<![\w-])border-radius\s*:\s*([^;}]+)')
EVIDENCE = re.compile(r'\bCircle\s*\(|\bRadiusUi\s*\.\s*(?:Px|Rounded)\s*\(|"[a-z][a-z0-9_]*_r_(?:rem|w)"')
REM = re.compile(r'^(-?\d*\.?\d+)rem$')
APPW = re.compile(r'^calc\(\s*var\(\s*--app-w\s*\)\s*\*\s*(-?\d*\.?\d+)\s*\)$')
PX = re.compile(r'^(-?\d*\.?\d+)px$')


# ── 정본 CSS ──────────────────────────────────────────────────────────────────────────────
def _blank_comments(css):
    def rep(m):
        return ''.join('\n' if ch == '\n' else ' ' for ch in m.group(0))
    return re.sub(r'/\*.*?\*/', rep, css, flags=re.S)


def _blank_keyframes(css):
    """`@keyframes … { … }` 몸을 같은 길이의 공백으로 — 애니 칸은 «자리» 가 아니다."""
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
    """[(줄, 선택자, 값)] — border-radius 선언 전부(키프레임 안은 뺀다 · 한 규칙에 둘이면 둘 다)."""
    css = _blank_keyframes(_blank_comments(css_text))
    out = []
    for m in re.finditer(r'([^{}]+)\{([^{}]*)\}', css):
        sel = ' '.join(m.group(1).split())
        lead = len(m.group(1)) - len(m.group(1).lstrip())
        line = css[:m.start(1) + lead].count('\n') + 1
        for d in RADIUS_DECL.finditer(m.group(2)):
            out.append((line, sel, ' '.join(d.group(1).split())))
    return out


def css_value(val):
    """정본 값 → (단위, 수). 단위: 'rem' · 'w'(앱 폭 비율) · 'px' · 'circle'(50%) · 'inherit' · 'other'(네 모서리 따로 등)."""
    v = val.strip()
    if v == '50%':
        return 'circle', None
    if v == 'inherit':
        return 'inherit', None
    if v == '0':
        return 'rem', 0.0
    m = REM.match(v)
    if m:
        return 'rem', float(m.group(1))
    m = APPW.match(v)
    if m:
        return 'w', float(m.group(1))
    m = PX.match(v)
    if m:
        return 'px', float(m.group(1))
    return 'other', None


def _read_text(path):
    with open(path, encoding='utf-8') as f:
        return f.read()


# ── 표 ────────────────────────────────────────────────────────────────────────────────────
def load_table(path):
    """RadiusUi.json → {키: 수}(`_` 설명 칸은 뺀다). 못 읽으면 None."""
    if not os.path.isfile(path):
        return None
    try:
        root = json.loads(_read_text(path))
    except ValueError:
        return None
    if not isinstance(root, dict):
        return None
    out = {}
    for k, v in root.items():
        if k.startswith('_'):
            continue
        if isinstance(v, dict):                      # 곁 표의 절(`layout`·`colors`…) — 한 단계 안의 키도 자리다(PetSkillUi 꼴)
            for k2, v2 in v.items():
                if not k2.startswith('_') and not isinstance(v2, dict):
                    out.setdefault(k2, v2)
        else:
            out[k] = v
    return out


def key_unit(key):
    if key.endswith('_r_rem'):
        return 'rem'
    if key.endswith('_r_w'):
        return 'w'
    return None


# ── 클론 ──────────────────────────────────────────────────────────────────────────────────
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


def check_target(game_dir, target, table, unit, num, res_dir=None):
    """(상태, 설명) — 'ok' | 'missing'(자리가 표를 안 부른다) | 'value'(표값이 정본과 다르다 · 표에 키가 없다) | 'absent'(파일·메서드 없음 · 규약 어김)."""
    m = re.match(r'([^#@$]+)([@$]?)(.*)', target)
    file_part, sep, tail = m.groups()
    other = None
    if sep == '$' and '@' in tail:
        tail, other = tail.split('@', 1)          # 키가 다른 곁 표에 있는 자리
        table = load_table(os.path.join(res_dir or '.', other))
        if table is None:
            return 'value', '곁 표 %s 를 못 읽었다(같은 Resources 폴더에 있어야 한다)' % other
    path = os.path.join(game_dir, file_part)
    if not os.path.isfile(path):
        return 'absent', '파일 없음 ' + file_part
    src = _read_text(path)
    if sep == '':
        return ('ok' if EVIDENCE.search(src) else 'missing'), '파일 전체에 둥근 모서리 증거가 없다'
    if sep == '@':
        body = _method_body(src, tail)
        if body is None:
            return 'absent', '메서드 없음 ' + tail + '('
        return ('ok' if EVIDENCE.search(body) else 'missing'), '메서드 ' + tail + '( 본문에 둥근 모서리 증거가 없다'
    ku = key_unit(tail)
    if ku is None:
        return 'absent', '표 키는 «_r_rem» 이나 «_r_w» 로 끝나야 한다(규약): ' + tail
    if unit == 'circle':
        return 'absent', '정본이 50%(원)인 자리는 표 키가 아니라 `Circle(` 증거(@Method)로 본다: ' + tail
    if unit not in ('rem', 'w'):
        return 'absent', '자가 아직 못 견주는 정본 값(%s) — 표 키를 걸 수 없다: %s' % (unit, tail)
    if ku != unit:
        return 'value', '단위가 다르다 — 정본은 %s 인데 키 꼬리는 %s 다(%s)' % (unit, ku, tail)
    if table is None or tail not in table:
        return 'value', '표(%s)에 «%s» 이 없다' % (other or 'RadiusUi.json', tail)
    tv = table[tail]
    if not isinstance(tv, (int, float)) or isinstance(tv, bool):
        return 'value', '표값 «%s» 이 수가 아니다: %r' % (tail, tv)
    if abs(float(tv) - num) > 1e-6:
        return 'value', '표값 «%s» = %s 인데 정본은 %s 다 → 표를 정본에 맞춰라' % (tail, tv, num)
    if ('"' + tail + '"') in src:
        return 'ok', '표 키 "%s" 를 부른다%s' % (tail, (' (' + other + ')') if other else '')
    return 'missing', '표 키 "%s" 를 아무 데서도 안 부른다 — 그 자리는 코드에 박힌 반지름이다(§1)' % tail


# ── 대조 ──────────────────────────────────────────────────────────────────────────────────
def run(css_path, game_dir, table_path, table_map, known, out=print, list_undecided=False):
    if not os.path.isfile(css_path):
        out('✗ 정본 CSS 를 못 읽었다: %s (git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)' % css_path)
        return 2
    rules = parse_rules(_read_text(css_path))
    table = load_table(table_path)
    res_dir = os.path.dirname(os.path.abspath(table_path))
    problems = 0
    if table is None:
        problems += 1
        out('✗ 표를 못 읽었다: %s (RadiusUi.json · 최상위가 상자여야 한다)' % table_path)
    seen = set()
    n_ok = n_known = n_verified = n_off = 0
    undecided = []
    known_now_ok = []
    for line, sel, val in rules:
        seen.add(sel)
        unit, num = css_value(val)
        if sel not in table_map:
            undecided.append((line, sel, val))
            continue
        targets = table_map[sel]
        if isinstance(targets, str):
            if targets.startswith('✓'):
                n_verified += 1
            else:
                n_off += 1
            continue
        for t in targets:
            state, why = check_target(game_dir, t, table, unit, num, res_dir)
            if state == 'ok':
                n_ok += 1
                if t in known:
                    known_now_ok.append(t)
                continue
            tag = {'missing': '표를 안 부른다', 'value': '표값이 다르다', 'absent': '자리 없음'}[state]
            if state == 'missing' and t in known:
                n_known += 1
                out('· KNOWN(%s)  %s  ← style.css %d %s { border-radius: %s }  — %s' % (tag, t, line, sel, val, known[t]))
            else:
                problems += 1
                out('✗ %s  %s  ← style.css %d  %s  { border-radius: %s }  — %s' % (tag, t, line, sel, val, why))
    for sel in table_map:
        if sel not in seen:
            problems += 1
            out('✗ 표에는 있는데 정본에 없는 선택자: %s  → 정본이 바뀌었다 · TABLE 에서 지우거나 고쳐라' % sel)
    for t in sorted(set(known_now_ok)):
        out('· KNOWN 인데 이제 표를 부른다: %s  → KNOWN 에서 지워라' % t)
    if table is not None:
        for k in sorted(table):
            if key_unit(k) is None:
                problems += 1
                out('✗ 표 키 규약 어김  %s  → «_r_rem» 이나 «_r_w» 로 끝나야 한다' % k)
    if list_undecided:
        for line, sel, val in undecided:
            out('· 미정  style.css %5d  %-64s %s' % (line, sel[:64], val))
    out('%s check_border_radius: 정본 border-radius %d(선택자 %d) · 자리 초록 %d · 20회차 ✓ %d · 대조 안 함 %d · KNOWN 빈자리 %d · 미정 %d(--list 로 본다) · 문제 %d'
        % ('✓' if problems == 0 else '✗', len(rules), len({r[1] for r in rules}), n_ok, n_verified, n_off, n_known, len(undecided), problems))
    return 0 if problems == 0 else 1


# ── 자기 검사(고장 주입) ────────────────────────────────────────────────────────────────────
def self_test():
    css = """
/* 주석 { border-radius: 9rem; } */
.a-key { border-radius: .42rem; }
.b-zero { border-radius: 0; }
.c-w { border-radius: calc(var(--app-w) * .0094); }
.d-circle { border-radius: 50%; }
@keyframes pop {
    0%  { border-radius: 1rem; }
    50% { border-radius: 2rem; }
}
.e-miss { border-radius: .8rem; }
.f-ok { border-radius: .5rem; }
.g-dead { border-radius: 3px; }
"""
    cs = """
namespace X {
    class Sheet {
        void Build(Transform p) {
            Image a = RadiusUi.Rounded(p, "a", "ink", "a_r_rem");
            Image z = UiKit.Rounded(p, "z", "ink", RadiusUi.Px("b_zero_r_rem"));
            float r = RadiusUi.Px("c_r_w");
        }
        void Round(Transform p) {
            Image c = UiKit.Circle(p, "c", "ink");
        }
        void Bare(Transform p) {
            Image e = UiKit.Rounded(p, "e", "ink", rem * 0.6f);
        }
    }
}
"""
    table = {'_': '설명', 'a_r_rem': 0.42, 'b_zero_r_rem': 0, 'c_r_w': 0.0094, 'e_r_rem': 0.8, 'f_r_rem': 0.5}
    fails = []

    def expect(name, tmap, known, want, css_text=css, cs_text=cs, tbl=table, want_line=None):
        with tempfile.TemporaryDirectory() as d:
            os.makedirs(os.path.join(d, 'Ui'))
            with open(os.path.join(d, 'style.css'), 'w', encoding='utf-8') as f:
                f.write(css_text)
            with open(os.path.join(d, 'Ui', 'Sheet.cs'), 'w', encoding='utf-8') as f:
                f.write(cs_text)
            tp = os.path.join(d, 'RadiusUi.json')
            if tbl is not None:
                with open(tp, 'w', encoding='utf-8') as f:
                    f.write(tbl if isinstance(tbl, str) else json.dumps(tbl))
            lines = []
            rc = run(os.path.join(d, 'style.css'), d, tp, tmap, known, out=lines.append)
            if rc != want:
                fails.append('%s: rc %d ≠ %d\n  ' % (name, rc, want) + '\n  '.join(lines))
            if want_line and not any(want_line in l for l in lines):
                fails.append('%s: «%s» 줄이 안 나온다\n  ' % (name, want_line) + '\n  '.join(lines))
            return lines

    base = {
        '.a-key': ['Ui/Sheet.cs$a_r_rem'], '.b-zero': ['Ui/Sheet.cs$b_zero_r_rem'],
        '.c-w': ['Ui/Sheet.cs$c_r_w'], '.d-circle': ['Ui/Sheet.cs@Round'],
        '.e-miss': ['Ui/Sheet.cs$e_r_rem'], '.f-ok': '✓맞는 리터럴', '.g-dead': '—죽은 CSS',
    }
    kn = {'Ui/Sheet.cs$e_r_rem': '임자'}
    # 1 다 있으면 0(안 부르는 자리는 KNOWN)
    expect('전부 초록', base, kn, 0)
    # 2 KNOWN 을 지우면 «표를 안 부른다» → 1
    expect('표를 안 부른다 → 1', base, {}, 1, want_line='코드에 박힌 반지름')
    # 3 표값이 정본과 다르다 → 1(KNOWN 이어도 값은 봐야 한다)
    t2 = dict(table); t2['a_r_rem'] = 0.6
    expect('표값 다름 → 1', base, kn, 1, tbl=t2, want_line='표를 정본에 맞춰라')
    # 4 표에 키가 없다 → 1
    t3 = dict(table); del t3['c_r_w']
    expect('표에 키 없음 → 1', base, kn, 1, tbl=t3, want_line='이 없다')
    # 5 단위 꼬리가 정본과 다르다(app-w 자리에 rem 키) → 1
    t4 = dict(table); t4['c_r_rem'] = 0.0094
    m4 = dict(base); m4['.c-w'] = ['Ui/Sheet.cs$c_r_rem']
    expect('단위 다름 → 1', m4, kn, 1, tbl=t4, want_line='단위가 다르다')
    # 6 표에 없는 정본 선언은 «미정» — 알리기만(rc 0)
    lines = expect('미정은 알리기만', base, kn, 0, css_text=css + '.z-new { border-radius: .1rem; }\n')
    if not any('미정 1' in l for l in lines):
        fails.append('미정 수가 안 나온다: ' + (lines[-1] if lines else ''))
    # 7 표에는 있는데 정본에 없다 → 1
    m7 = dict(base); m7['.gone'] = ['Ui/Sheet.cs']
    expect('정본에 없는 선택자 → 1', m7, kn, 1)
    # 8 원(50%) 자리를 표 키로 걸면 «자리 없음»
    m8 = dict(base); m8['.d-circle'] = ['Ui/Sheet.cs$d_r_rem']
    expect('원 자리에 표 키 → 1', m8, kn, 1, want_line='Circle(')
    # 9 메서드 본문에 증거가 없다 → 1
    m9 = dict(base); m9['.d-circle'] = ['Ui/Sheet.cs@Bare']
    expect('메서드 민글자 → 1', m9, kn, 1)
    # 10 고장 주입: 호출을 리터럴로 되돌리면 1
    broken = cs.replace('RadiusUi.Rounded(p, "a", "ink", "a_r_rem")', 'UiKit.Rounded(p, "a", "ink", rem * 0.6f)')
    expect('호출을 리터럴로 → 1', base, kn, 1, cs_text=broken)
    # 11 키 규약(_r_rem·_r_w)을 안 지키면 → 1(TABLE 쪽 · 표 쪽 둘 다)
    m11 = dict(base); m11['.a-key'] = ['Ui/Sheet.cs$a_radius']
    expect('TABLE 키 규약 어김 → 1', m11, kn, 1, want_line='규약')
    t11 = dict(table); t11['loose'] = 1
    expect('표 키 규약 어김 → 1', base, kn, 1, tbl=t11, want_line='표 키 규약 어김')
    # 12 KNOWN 인데 이제 부른다 → 알리기만(rc 0)
    expect('KNOWN 해소 알림', base, {'Ui/Sheet.cs$e_r_rem': '임자', 'Ui/Sheet.cs$a_r_rem': '옛 임자'}, 0,
           want_line='KNOWN 인데 이제 표를 부른다')
    # 13 표 파일이 깨졌다/없다 → 1
    expect('표 깨짐 → 1', base, kn, 1, tbl='{"a_r_rem": 0.42,', want_line='표를 못 읽었다')
    expect('표 없음 → 1', base, kn, 1, tbl=None)
    # 13b 곁 표(@Other.json) — 키가 거기 있고 파일이 부르면 0 · 곁 표에 키가 없으면 1 · 곁 표 자체가 없으면 1
    def expect_other(name, other_tbl, cs_text, want, want_line=None):
        with tempfile.TemporaryDirectory() as d:
            os.makedirs(os.path.join(d, 'Ui'))
            with open(os.path.join(d, 'style.css'), 'w', encoding='utf-8') as f:
                f.write('.o-chip { border-radius: .5rem; }\n')
            with open(os.path.join(d, 'Ui', 'Sheet.cs'), 'w', encoding='utf-8') as f:
                f.write(cs_text)
            tp = os.path.join(d, 'RadiusUi.json')
            with open(tp, 'w', encoding='utf-8') as f:
                f.write(json.dumps({'z_r_rem': 1}))
            if other_tbl is not None:
                with open(os.path.join(d, 'Other.json'), 'w', encoding='utf-8') as f:
                    f.write(json.dumps(other_tbl))
            lines = []
            rc = run(os.path.join(d, 'style.css'), d, tp, {'.o-chip': ['Ui/Sheet.cs$chip_r_rem@Other.json']}, {}, out=lines.append)
            if rc != want:
                fails.append('%s: rc %d ≠ %d\n  ' % (name, rc, want) + '\n  '.join(lines))
            if want_line and not any(want_line in l for l in lines):
                fails.append('%s: «%s» 줄이 안 나온다\n  ' % (name, want_line) + '\n  '.join(lines))
    cs_o = 'class S { void B(Transform p) { var x = K.Framed(p, "skin", C("bg"), Style.Px("chip_r_rem"), 2); } }'
    expect_other('곁 표 초록', {'_': 'x', 'chip_r_rem': 0.5}, cs_o, 0)
    expect_other('곁 표 절 안의 키도 자리다', {'_': 'x', 'layout': {'chip_r_rem': 0.5}}, cs_o, 0)
    expect_other('곁 표 값 다름 → 1', {'chip_r_rem': 0.4}, cs_o, 1, want_line='표를 정본에 맞춰라')
    expect_other('곁 표에 키 없음 → 1', {'other_r_rem': 0.5}, cs_o, 1, want_line='표(Other.json)에')
    expect_other('곁 표 없음 → 1', None, cs_o, 1, want_line='곁 표 Other.json 를 못 읽었다')
    expect_other('곁 표는 있는데 안 부른다 → 1', {'chip_r_rem': 0.5}, 'class S { }', 1, want_line='코드에 박힌 반지름')
    # 14 파서: 주석·키프레임 안은 안 센다 · 값 0 · 줄 번호
    rules = parse_rules(css)
    sels = [r[1] for r in rules]
    if sels != ['.a-key', '.b-zero', '.c-w', '.d-circle', '.e-miss', '.f-ok', '.g-dead']:
        fails.append('파서 선택자(주석·키프레임이 섞였다): %r' % sels)
    if rules[0][0] != 3 or rules[1][2] != '0':
        fails.append('파서 줄번호/0 값: %r' % (rules[:2],))
    # 15 값 읽기
    for v, want in (('.42rem', ('rem', 0.42)), ('0', ('rem', 0.0)), ('50%', ('circle', None)),
                    ('calc(var(--app-w) * .0094)', ('w', 0.0094)), ('3px', ('px', 3.0)),
                    ('1rem 1rem 0 0', ('other', None)), ('inherit', ('inherit', None))):
        if css_value(v) != want:
            fails.append('값 읽기 %r → %r ≠ %r' % (v, css_value(v), want))
    # 16 정본 CSS 가 없으면 2
    if run('/nonexistent/style.css', '.', '/nonexistent.json', base, {}, out=lambda s: None) != 2:
        fails.append('정본 없음 rc 2')
    if fails:
        print('✗ check_border_radius --self-test 실패 %d' % len(fails))
        for f in fails:
            print('  - ' + f)
        return 1
    print('✓ check_border_radius --self-test 24칸 통과')
    return 0


def main(argv):
    css, game, table = CSS_DEFAULT, GAME_DEFAULT, TABLE_DEFAULT
    list_undecided = False
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--self-test':
            return self_test()
        if a == '--list':
            list_undecided = True; i += 1; continue
        if a == '--css' and i + 1 < len(argv):
            css = argv[i + 1]; i += 2; continue
        if a == '--game' and i + 1 < len(argv):
            game = argv[i + 1]; i += 2; continue
        if a == '--table' and i + 1 < len(argv):
            table = argv[i + 1]; i += 2; continue
        print('사용: check_border_radius.py [--css <style.css>] [--game <Assets/Scripts/Game>] [--table <RadiusUi.json>] [--list] [--self-test]')
        return 2
    return run(css, game, table, TABLE, KNOWN, list_undecided=list_undecided)


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
