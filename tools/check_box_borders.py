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
폭 단도 견준다(2회차): 호출의 폭 인자(`PopupKit.Line`/`Line3` · `PetSkillKit.Line2/Line3` · `line_px`·`line2_px`·`line3_px`·`line4_px`·`line1_px` · `line`/`line3` 변수)를
단으로 읽어 정본 단과 맞춘다(`cellb` = ol3 내림 → ol3 · 클론 line_px 2 = ol1 · line2_px 4 = ol2 · line3_px 6 = ol3 · T33 29회차). 도우미 매개변수(`line`·`linePx`·`borderPx`·`lineW`)로
넘겨받은 폭은 «못 읽음» 으로 두고 판정하지 않는다. `UiKit.Rounded` 는 **짝**일 때만 테다 — 바깥 `Rounded(p, "line"|…)` 뒤에 `Rounded(…, r - <폭>)` 안쪽 면이 따라오면 그 «폭» 이 단이다.
`@Method` 자리는 본문 안의 단을 모두 모아 정본 단이 그중에 있으면 맞다(한 메서드가 여럿을 세운다).

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
    # T33 29회차가 실물로 연 셋(SkillSummonResult BuildCell/BuildFoot Framed line1_px + 같은 색)
    '.sr-qty': ['Ui/SkillSummonResult.cs#sr-qty'],
    '.sr-dup': ['Ui/SkillSummonResult.cs#sr-dup'],
    '.sr-hint': ['Ui/SkillSummonResult.cs#sr-hint'],
    '.sr-new': ['Ui/SkillSummonResult.cs#sr-new'],
    # 한 줄 테 — UiKit.Line(p, "line", …)
    '#topbar|bottom': ['Ui/Hud.cs@Build'],
    '#tabbar|top': ['Ui/TabBar.cs@Build'],
    '#equip-sheet|top': ['Ui/UiRoot.cs@Build'],
    '#chat-preview|top': ['Ui/UiRoot.cs@Build'],
    '.panel|top': ['Ui/TabBar.cs@Build'],
    '.league-foot|top': ['Ui/LeagueSheet.cs@RenderBoard'],
    '.hatchery|top': ['Ui/PetPanel.cs@BuildHatchery'],
    '.tech-branch-head|bottom': ['Ui/TechPanel.cs@BranchCard'],
    '.tb-row|bottom': ['Ui/TechPopups.cs@OpenBonuses'],
    # 공용 도우미(Popups.cs) — 카드·버튼·토스트·토글·아바타
    '.modal-card': ['Ui/Popups.cs@Card'],
    '.modal-card .btn': ['Ui/Popups.cs@Btn'],
    '.panel .btn': ['Ui/Popups.cs@Btn'],
    '#equip-sheet .btn': ['Ui/Popups.cs@Btn'],
    '.btn': ['Ui/Popups.cs@Btn'],
    '.btn.silver': ['Ui/Popups.cs@Btn'],
    '.league-back-btn': ['Ui/Popups.cs@BackButton'],
    '.toast': ['Ui/Popups.cs@Toast'],
    '.settings-toggle': ['Ui/Popups.cs@Toggle'],
    '.profile-avatar-big': ['Ui/Popups.cs@Avatar'],
    # 상단바 카드·아바타·채팅 배지(Hud) · 채팅 화면(ChatScreen)
    '.profile-card': ['Ui/Hud.cs@Build'],
    '.profile-card .avatar': ['Ui/Hud.cs@Build'],
    '.chat-preview-badge': ['Ui/Hud.cs@BuildChat'],
    '.chat-input-bar|top': ['Ui/ChatScreen.cs@Open'],
    '.chat-input-bar input': ['Ui/ChatScreen.cs@Open'],
    '.chat-input-bar .btn.danger.round': ['Ui/ChatScreen.cs@Open'],
    # 카드 면 — PopupKit.Outlined(p, "face", …)
    '.league-row': ['Ui/LeagueSheet.cs@Row'],
    '.league-collect-pill': ['Ui/LeagueSheet.cs@RenderRewards'],
    '.league-reward-table': ['Ui/LeagueSheet.cs@RenderRewards'],
    '.pass-banner': ['Ui/PassPopup.cs@Render'],
    '.pass-cell': ['Ui/PassPopup.cs@Cell'],
    '.shop-banner': ['Ui/ShopSheet.cs@Banner'],
    '.shop-deal-card': ['Ui/ShopSheet.cs@Render'],
    '.shop-gem-card': ['Ui/ShopSheet.cs@Render'],
    '.qst-row': ['Ui/QuestSheet.cs@Render'],
    # 프로필 팝업
    '.profile-field': ['Ui/ProfilePopup.cs@Field'],
    '.profile-edit-btn': ['Ui/ProfilePopup.cs@EditButton'],
    '.profile-tabs': ['Ui/ProfilePopup.cs@RenderProfile'],
    '.avatar-pick-btn': ['Ui/ProfilePopup.cs@RenderProfile'],
    '.settings-act': ['Ui/ProfilePopup.cs@ActRow'],
    # 펫·스킬 타일(PetSkillKit.Framed/Orb)
    '.pet-tile .tile-face': ['Ui/PetPanel.cs@TileFace'],
    '.sk-orb': ['Ui/SkillPanel.cs@BuildGrid'],
    '.equipped-row': ['Ui/SkillPanel.cs@BuildEquippedRow', 'Ui/PetPanel.cs@BuildEquippedRow'],
    '.equipped-label': ['Ui/SkillPanel.cs@EquippedLabel'],
    '.sk-mini small': ['Ui/SkillPanel.cs@MiniLv'],
    '.petup-panel': ['Ui/PetUpgradePopup.cs@Render'],
    '.rate-bar': ['Ui/SkillRatesPopup.cs@Render'],
    # 기술 트리(DungeonPopups.Bordered)
    '.tech-branch-card': ['Ui/TechPanel.cs@BranchCard'],
    '.tech-tier-tag': ['Ui/TechPanel.cs@RenderBranch'],
    '.tech-prog': ['Ui/TechPopups.cs@RenderAction'],
    '.modal-card .tech-prog': ['Ui/TechPopups.cs@RenderAction'],
    '.asc-focus': ['Ui/AscendPopup.cs@Open'],
    '.dgc-cell': ['Ui/DungeonClearPopup.cs@Show'],
    '.modal-card.sheet .dg-banner': ['Ui/DungeonSheet.cs@Banner'],
    # T365 ⓐ — 펫 카드 왼쪽 등급색 띠(1664 border-left var(--ol4)) · 클론에 pet-card 이름 0 · PetPanel.cs 는 T331·T333 lock
    '.pet-card|left': ['Ui/PetPanel.cs#pet-card'],
}

# ── 임자가 정해진 빈자리(자리 → 이유) — 닫을 때마다 지운다 ───────────────────────────────
KNOWN = {
    'Ui/PetPanel.cs#pet-card': 'T365 ⓐ — 정본 1664 `.pet-card { border-left: var(--ol4) solid var(--rc) }` 등급색 띠 · 카탈로그 line4_px(8)는 1회차에 더했고 자리는 PetPanel.cs(T331·T333 lock) 뒤',
    # ── 2회차가 찾은 폭 단 어긋남 13(정본 단 ↔ 클론 단) — 파일 lock 이 풀리는 회차에 키 하나씩 바꾼다(T365 3회차 이후) ──
    'Ui/Popups.cs@Btn': 'T365 2회차 — 바닥 버튼(`.btn` 664 ol1 #444c56 · HUD·오프라인·리그 뒤로)만 얇은 회색인데 공용 Btn 은 Line3 하나다(모달·패널·시트 안 `.btn` 3543 ol3 는 맞다) · Popups.cs T331·T333 lock — 바닥 버튼에 keyline 폭 인자를 주는 길',
    'Ui/Popups.cs@Toggle': 'T365 2회차 — `.settings-toggle` 3108 ol2 ↔ 클론 Toggle 은 `h*0.5f - Line`(ol1) · Popups.cs lock 뒤 Line2 로',
    'Ui/Popups.cs@Avatar': 'T365 2회차 — `.profile-avatar-big` 3002 ol3 ↔ 클론 Avatar 는 `radius - Line`(ol1) · Popups.cs lock 뒤 Line3 로(작은 아바타 .avatar 들은 ol2 — 호출부가 폭을 넘기는 길)',
    'Ui/Hud.cs@BuildChat': 'T365 2회차 — `.chat-preview-badge` 3249 ol15(1.5px ≈ 캔버스 3px) ↔ 클론 line_px(2) · ol15 단 키가 카탈로그에 없다(line15_px 3) · Hud.cs T331 lock',
    'Ui/ChatScreen.cs@Open': 'T365 2회차 — `.chat-input-bar` 위 테 3444 ol2 · 입력칸 3450 ol2 · 위험 둥근 버튼 3284 ol2 ↔ 클론 Open 은 Line(ol1)·Line3(ol3) 둘뿐(ol2 0) · ChatScreen.cs T333·T354 lock',
    'Ui/LeagueSheet.cs@Row': 'T365 2회차 — `.league-row` 2524 ol2 ↔ 클론 Row 는 PopupKit.Line(ol1) · LeagueSheet.cs T333 lock',
    'Ui/LeagueSheet.cs@RenderRewards': 'T365 2회차 — `.league-reward-table` 2537 ol2 ↔ 클론 table 은 PopupKit.Line(ol1)(수집 알약 ol3 는 맞다) · LeagueSheet.cs T333 lock',
    'Ui/ProfilePopup.cs@Field': 'T365 2회차 — `.profile-field` 3048 ol2 ↔ 클론 Field 는 `- PopupKit.Line`(ol1) · ProfilePopup.cs 는 산 lock 없음 → 3회차에 고친다',
    'Ui/ProfilePopup.cs@RenderProfile': 'T365 2회차 — `.profile-tabs` 3070 ol3 · `.avatar-pick-btn` 3063 ol2 ↔ 클론 RenderProfile 은 ol1 뿐 · ProfilePopup.cs 산 lock 없음 → 3회차',
    'Ui/ProfilePopup.cs@ActRow': 'T365 2회차 — `.settings-act` 3123 ol2 ↔ 클론 ActRow `- PopupKit.Line`(ol1) · ProfilePopup.cs 산 lock 없음 → 3회차',
}

HELPERS = ('PopupKit.Outlined', 'UiKit.Line', 'PetSkillKit.Framed', 'PetSkillKit.Orb', 'DungeonPopups.Bordered', 'DungeonPopups.BorderedCircle', 'UiKit.Rounded')
# 도우미별 폭 인자 자리(0부터 · 이름 인자는 1) — Rounded 는 짝(안쪽 면의 «r - 폭»)에서 읽는다
WIDTH_ARG = {'PopupKit.Outlined': 4, 'UiKit.Line': 3, 'PetSkillKit.Framed': 4, 'PetSkillKit.Orb': 3, 'DungeonPopups.Bordered': 4, 'DungeonPopups.BorderedCircle': 3}
CALL_RE = re.compile(r'\b(' + '|'.join(re.escape(h) for h in HELPERS) + r')\s*\(')
TIER_PATTERNS = [
    ('ol4', re.compile(r'line4_px')),
    ('ol3', re.compile(r'\bLine3\b|line3_px|\bline3\b')),
    ('ol2', re.compile(r'\bLine2\b|line2_px|\bline2\b')),
    ('ol1', re.compile(r'\bLine\b|line_px|line1_px|\bline\b')),
]
PARAM_RE = re.compile(r'^\s*(?:line|linePx|lineW|borderPx|width|w)\s*$')
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


def _brace_body(src, start):
    """`{` 위치에서 짝 `}` 까지 — 문자열·문자·주석 속 중괄호는 세지 않는다(«{0}» 같은 포맷 문자열이 본문을 자르던 것 · T365 2회차)."""
    depth, i, n = 0, start, len(src)
    while i < n:
        c = src[i]
        if c == '"':
            verbatim = i > 0 and src[i - 1] == '@'
            i += 1
            while i < n:
                if src[i] == '\\' and not verbatim:
                    i += 2; continue
                if src[i] == '"':
                    if verbatim and i + 1 < n and src[i + 1] == '"':
                        i += 2; continue
                    break
                i += 1
        elif c == "'":
            i += 1
            while i < n and src[i] != "'":
                i += 2 if src[i] == '\\' else 1
        elif c == '/' and i + 1 < n and src[i + 1] == '/':
            while i < n and src[i] != '\n':
                i += 1
            continue
        elif c == '/' and i + 1 < n and src[i + 1] == '*':
            j = src.find('*/', i + 2)
            i = n if j < 0 else j + 2
            continue
        elif c == '{':
            depth += 1
        elif c == '}':
            depth -= 1
            if depth == 0:
                return src[start:i + 1]
        i += 1
    return src[start:]


def _method_body(src, name):
    """메서드 본문(들) — 줄머리의 «(한정자) 반환형 이름(» 만 잡고(호출은 안 잡는다), 오버로드가 여럿이면 본문을 전부 잇는다. 없으면 None."""
    bodies = []
    for m in re.finditer(r'^[ \t]*(?:(?:public|private|internal|protected|static|override|readonly)\s+)*[\w<>\[\],.]+\s+' + re.escape(name) + r'\s*\(', src, flags=re.M):
        _, close = _args(src, m.end() - 1)
        head = src[close:close + 200]
        arrow = re.match(r'\s*=>', head)
        if arrow:
            end = src.find(';', close)
            bodies.append(src[close:end + 1 if end >= 0 else None])
            continue
        b = src.find('{', close)
        if b < 0:
            continue
        bodies.append(_brace_body(src, b))
    return '\n'.join(bodies) if bodies else None


def _args(src, open_paren):
    """여는 괄호 뒤의 인자 목록(괄호 깊이를 센다) — (인자들, 닫는 괄호 위치)."""
    depth, i, start, args = 0, open_paren, open_paren + 1, []
    while i < len(src):
        c = src[i]
        if c == '(':
            depth += 1
        elif c == ')':
            depth -= 1
            if depth == 0:
                args.append(src[start:i].strip()); return args, i
        elif c == ',' and depth == 1:
            args.append(src[start:i].strip()); start = i + 1
        i += 1
    return args, len(src)


def tier_of_expr(expr):
    """폭 식 → 단('ol1'..'ol4') · 'param'(도우미 매개변수 — 판정 안 함) · None(모름)."""
    if expr is None:
        return None
    if PARAM_RE.match(expr):
        return 'param'
    for tier, rx in TIER_PATTERNS:
        if rx.search(expr):
            return tier
    return None


def border_calls(src):
    """[(이름, 단, 시작)] — 파일(또는 메서드 본문) 안의 테 호출 전부. Rounded 는 안쪽 면 «r - 폭» 짝이 따라올 때만."""
    out = []
    for m in CALL_RE.finditer(src):
        helper = m.group(1)
        args, close = _args(src, m.end() - 1)
        if len(args) < 2 or not args[1].startswith('"'):
            continue
        name = args[1].strip('"')
        if helper == 'UiKit.Rounded':
            # 짝 판정: 바깥 고리(이름 line/outline/ring/avatar/…) 뒤 420자 안에 안쪽 면 Rounded 가 따라와야 테다.
            # 안쪽 반지름이 «r - 폭» 이면 그 폭이 단 · 반지름을 따로 준 «입술(lip)» 꼴이면 단은 못 읽는다(None).
            look = src[close:close + 420]
            n = re.search(r'UiKit\.Rounded\s*\(', look)
            if not n:
                continue
            inner, _ = _args(look, n.end() - 1)
            if len(inner) < 4 or not inner[1].startswith('"'):
                continue
            iname = inner[1].strip('"')
            if iname not in ('face', 'bg', 'lip', 'ground', 'fill'):
                continue
            tail = inner[3].split('-', 1)[1] if '-' in inner[3] else None
            out.append((name, tier_of_expr(tail), m.start()))
        else:
            idx = WIDTH_ARG[helper]
            out.append((name, tier_of_expr(args[idx]) if len(args) > idx else None, m.start()))
    return out


def css_tier(value):
    t = width_tier(value)
    return 'ol3' if t == 'cellb' else t


def check_target(game_dir, target, want_tier=None):
    """(상태, 설명) — 'ok' | 'missing'(자리는 있는데 테 호출 없음) | 'absent'(요소·메서드·파일 없음) | 'tier'(단이 정본과 다름) | 'skip'."""
    if target.startswith('—'):
        return 'skip', target[1:]
    file_part, sep, tail = re.match(r'([^#@]+)([#@]?)(.*)', target).groups()
    path = os.path.join(game_dir, file_part)
    if not os.path.isfile(path):
        return 'absent', '파일 없음 ' + file_part
    src = _read(path)
    if sep == '@':
        body = _method_body(src, tail)
        if body is None:
            return 'absent', '메서드 없음 ' + tail + '('
        calls = border_calls(body)
        where = '메서드 ' + tail + '( 본문'
    elif sep == '#':
        calls = [c for c in border_calls(src) if c[0] == tail]
        where = '"%s" 자리' % tail
    else:
        calls = border_calls(src)
        where = '파일 전체'
    if not calls:
        return ('absent' if sep == '#' else 'missing'), where + '에 테 호출이 없다'
    tiers = {t for _, t, _ in calls if t}
    if want_tier and want_tier.startswith('ol') and tiers and 'param' not in tiers and want_tier not in tiers:
        return 'tier', '%s: 정본 %s ↔ 클론 %s' % (where, want_tier, '/'.join(sorted(tiers)))
    return 'ok', where + ' · 단 ' + ('/'.join(sorted(tiers)) if tiers else '못 읽음')


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
    css_by_key = {table_key(sel, side): val for sel, side, _, val in final}
    keys_in_css = set(css_by_key)
    pending = [(sel, side, line, val) for sel, side, line, val in final if table_key(sel, side) not in table]
    for key, targets in table.items():
        if key not in keys_in_css:
            out('  ✗ 표의 선택자가 정본에 없다: %s' % key)
            problems += 1
            continue
        for t in targets:
            state, why = check_target(game_dir, t, css_tier(css_by_key[key]))
            if state == 'ok':
                n_ok += 1
                if t in known:
                    known_now_ok.append(t)
            elif state == 'skip':
                n_skip += 1
            elif t in known:
                n_known += 1
            else:
                label = {'missing': '테 없음', 'absent': '자리 없음', 'tier': '단 어긋남'}[state]
                out('  ✗ %s → %s: %s(%s)' % (key, t, label, why))
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
    # 2회차 — 폭 단: .a 는 정본 ol3 인데 Toast 는 PopupKit.Line(ol1) 이라 어긋남 · Build 의 UiKit.Line 은 Line3 이라 맞다
    bad4 = dict(good); bad4['.a'] = ['Ui/Face.cs@Toast']
    checks.append(('폭 단이 정본과 다르면 rc 1(단 어긋남)', run(css, tmp, bad4, {}, logs.append) == 1))
    checks.append(('폭 단이 맞으면 ok(.a ol3 ↔ Build 의 Line3)', check_target(tmp, 'Ui/Face.cs@Build', 'ol3')[0] == 'ok'))
    with open(os.path.join(ui, 'Ring.cs'), 'w', encoding='utf-8') as f:
        f.write('class R {\n'
                '    void Card(Transform p, float r) { UiKit.Rounded(p, "line", "pp_line", r); Image face = UiKit.Rounded(p, "face", "pp_paper", r - PopupKit.Line3); }\n'
                '    void Fill(Transform p, float r) { UiKit.Rounded(p, "bg", "pp_paper", r); }\n'
                '    void Lip(Transform p, float r) { UiKit.Rounded(p, "line", "pp_line", r); Image lip = UiKit.Rounded(p, "lip", "pp_blue", r * 0.8f); }\n'
                '}\n')
    checks.append(('Rounded 짝(line + face r - Line3)은 테 ol3', check_target(tmp, 'Ui/Ring.cs@Card', 'ol3')[0] == 'ok'))
    checks.append(('Rounded 홀로(bg 채움)는 테가 아니다', check_target(tmp, 'Ui/Ring.cs@Fill', 'ol1')[0] == 'missing'))
    checks.append(('입술 꼴(lip · 반지름 따로)은 테지만 단은 못 읽는다 → 판정 안 함', check_target(tmp, 'Ui/Ring.cs@Lip', 'ol2')[0] == 'ok'))
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
