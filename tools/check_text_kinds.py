#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T391 — 정본이 **하한 위**(1.22~1.5rem ≈ 44~55px) 크기로 못박은 **문자 자리** ↔ 클론 그 자리의 글자 종류(`catalog.json textKinds`) px 가 ±12% 안인가.

왜 이 자가 있나(T33 36회차 · docs/parity.md): §1 하한(보조 36 · 본문 40 · 버튼 44 · 제목 60)은 정본 ≤1.0rem 자리를 **일부러** 키운다 —
그 축은 결함이 아니다. 그런데 하한 **위** 자리는 하한이 안 건드리는데도 클론이 `Title` 60 이나 `Sub` 36 으로 찍어 ±27~30% 벌어진 넷이
있었다(`.shop-gem-amt` 49 → 36 · `.rates-head h3` 47 → 60 · `.af-title` 46 → 60 · …). 종류표에 ≈48px(1.3rem) 단이 비어 있어서다.
이 자는 «정본 px ↔ 클론 종류 px» 를 자리마다 대조해 그 구멍을 센다 — 단이 생기고 배선되면 KNOWN 이 비고 초록이 된다.

무엇을 세나: 주석을 걷은(줄 번호 보존) `style.css` 에서 `@keyframes`·`@media` 밖의 정적 `font-size: <rem>rem` 선언 중 **1.22 ≤ rem ≤ 1.5**
(쉼표 목록은 하나씩 · 같은 선택자는 **뒤 규칙이 앞을 덮는다** · 특이도는 안 본다). px = rem × (`layout.rem_h` × `reference.h`) = rem × 36.4.

선택자 ↔ 클론 자리 짝은 TABLE 하나가 쥔다(T109 `check_keyline` · T377 `check_pinned_colors` 꼴):
  "Ui/File.cs#name"            «"name"» 으로 만드는 글자 호출(`UiKit.Text`·`PopupKit.Label`·`Bold`·`Stroked`·`IconTextRow`·`Btn`)의 `TextKind.X`
  "Ui/File.cs#outer/inner"     «"outer"» 상자 뒤에 오는 «"inner"» 글자 호출의 종류(이름이 흔한 안쪽 글자를 바깥 이름으로 고정한다)
  "Ui/File.cs$token"           «"token"» 문자열 뒤에 처음 오는 `TextKind.X`(`case "dmg-kill": kind = TextKind.Button` 꼴)
  "Ui/File.cs@Method#name"     위 셋을 메서드 본문 안에서만 찾는다
  "…|res:<Name>:<key>"         종류가 아니라 **표의 rem**(`Assets/Forge/Resources/<Name>.json` 안 어디든 "key": <rem>)로 px 를 받는 자리
SKIP 은 «이모지·아이콘 글리프 크기»(클론은 아틀라스 아이콘을 자리 크기로 세운다 — 글자 크기가 아니다) 같은 «대조하지 않는 자리».
KNOWN 은 «어긋난 것을 알지만 파일이 남의 lock 이라 지금 못 고치는 자리»(임자 번호를 적는다) — 고치면 여기서 지운다.
표에 없는 선택자 = **미정**(막지 않는다 · `--list` 로 본다).

사용:  python3 tools/check_text_kinds.py [--css <style.css>] [--game <Assets/Scripts/Game>] [--catalog <catalog.json>] [--res <Resources>] [--list] [--self-test]
rc:    0 = 표의 선택자가 전부 정본 목록에 있고 · 표의 자리 전부가 ±12% 안(또는 KNOWN·SKIP) · 1 = 아니다 · 2 = 정본 CSS 나 카탈로그를 못 읽었다
"""
import json
import os
import re
import sys
import tempfile

CSS_DEFAULT = os.path.join('.wwwww-src', 'web', 'css', 'style.css')
GAME_DEFAULT = os.path.join('Assets', 'Scripts', 'Game')
CATALOG_DEFAULT = os.path.join('Assets', 'Forge', 'catalog.json')
RES_DEFAULT = os.path.join('Assets', 'Forge', 'Resources')
LO, HI = 1.22, 1.5          # 축: 하한 위 · 제목 아래(T33 36회차)
TOL = 0.12                  # ±12%(등재문 ⓒ)

# ── 정본 선택자 ↔ 클론 자리 ──────────────────────────────────────────────────────────────────────
TABLE = {
    # T391 1회차 — 맞는 자리 다섯(정본 px ↔ 종류 px ±12%)
    '.bw-track span': ['Ui/BattleOverlay.cs#text'],                       # 402 1.5rem 54.6 ↔ Title 60(+10%) · 보스 경고 마퀴
    '.float-dmg.dmg-kill': ['Battle/DamageNumbers.cs$dmg-kill'],          # 524 1.3rem 47.3 ↔ Button 44(−7%)
    '.profile-title': ['Ui/ProfilePopup.cs#title'],                       # 2991 1.5rem 54.6 ↔ Title 60(+10%)
    '.af-start': ['Ui/ForgeAutoPopup.cs#af-start'],                       # 4816 1.3rem 47.3 ↔ Button 44(−7%)
    '.dgd-keys': ['Ui/DungeonDetailPopup.cs#keys'],                       # 5341 1.5rem 54.6 ↔ Title 60(+10%)
    '.rw-amt': ['Ui/RewardBurst.cs#rw-amt|res:RewardBurstUi:amt_font_rem'],   # 7494 1.32rem — 표 amt_font_rem 1.32 그대로
    # T391 2회차 — ⓐ Head 48 이 서서 ⓑ 로 옮긴 일곱(산 lock 밖 파일)
    '.tb-title': ['Ui/TechPanel.cs#title'],                               # 2242 1.25rem 45.5 ↔ Button 44(−3%)
    '.league-tier-rank': ['Ui/LeagueSheet.cs#table/label'],               # 2556 1.3rem 47.3 ↔ Head 48(+1%) · 첫 호출이 4위 아래 글자(Head)
    '.league-tier-rank.text': ['Ui/LeagueSheet.cs#table/label'],          # 2563 1.48rem 53.9 ↔ Head 48(−11%)
    '.lgr-rank-n': ['Ui/LeagueSheet.cs#table/label'],                     # 2573 1.22rem 44.4 ↔ Head 48(+8% · 코드는 1~3위에 Button 44 를 준다 — 자는 첫 호출만 읽는다)
    '.shop-gem-amt': ['Ui/ShopSheet.cs#amt-row/amt'],                     # 2983 1.35rem 49.1 ↔ Head 48(−2%)
    '#craft-modal .row .btn': ['Ui/ForgeCraftPopup.cs#sell', 'Ui/ForgeCraftPopup.cs#equip'],   # 3566 1.22rem 44.4 ↔ Button 44(−1%)
    '.sheet-title': ['Ui/ShopSheet.cs#title', 'Ui/DungeonSheet.cs#title', 'Ui/LeagueSheet.cs#title'],   # 3805 1.35rem 49.1 ↔ Head 48(−2%)
    # T391 4회차 — 파일이 열린 둘
    '.rates-head h3': ['Ui/SkillRatesPopup.cs#rates-h3'],                # 4580 1.3rem 47.3 ↔ Head 48(+1%)
    '.af-title': ['Ui/ForgeAutoPopup.cs#af-title'],                      # 5033 1.26rem 45.9 ↔ Button 44(−4%)
}
# 대조하지 않는 자리 — 이모지·아이콘 글리프 크기(클론은 아틀라스 아이콘을 자리 크기로 세운다)
SKIP = {
    '.offline-rate-icon': '아이콘 글리프(오프라인 보상 줄 · 클론 UiKit.Icon)',
    '.waypoint-icon': '아이콘 글리프(이정표)',
    '#waypoint-pass .waypoint-icon': '아이콘 글리프(패스 이정표)',
    '.skill-btn .sk-icon': '아이콘 글리프(스킬 버튼)',
    '.fl-face': '아이콘 글리프(대장간 목록 얼굴 · ItemFaces)',
    '.equip-cell.egg-cell.empty .mount-sil': '이모지 실루엣(빈 탈것 칸 · T342 가 걷은 자리)',
    '.pet-card .icon-circle': '아이콘 글리프(펫 카드)',
    '.cell-img.emoji': '이모지 폴백(장비 칸)',
    '.league-avatar': '아바타 글리프(리그 행)',
    '.league-challenge-avatar': '아바타 글리프(도전 행)',
    '.art-emblem': '문장 글리프(승천)',
    '.avatar-pick-btn': '아바타 고르기 버튼의 글리프',
    '.pinfo-id .avatar': '아바타 글리프(플레이어 정보)',
    '.chat-share-side .icon-circle.sm': '아이콘 글리프(채팅 공유 카드)',
    '.sk-orb': '구슬 글리프(스킬 오브)',
    '.pet-tile .tile-face': '펫 얼굴 글리프(PetFaces)',
    '.pet-tile .tile-check': '체크 글리프(펫 타일)',
    '.petd-tile': '펫 얼굴 글리프(펫 상세)',
    '.sr-grid.dense .sr-orb': '구슬 글리프(소환 결과 · 촘촘한 격자)',
}
# 어긋난 줄 알지만 파일이 남의 lock 이라 지금 못 고치는 자리 — 임자 번호 · 고치면 지운다
KNOWN = {
    # T391 1회차(자만) 실측 — ⓐ 종류표에 ≈48px 단이 서고 ⓑ 자리마다 그 단으로 바꾸면 여기서 TABLE 로 옮긴다(자리 표기는 아래 그대로 쓰면 된다)
    '.sr-grid.one .sr-name': 'T391 ⓑ · Ui/SkillSummonResult.cs#sr-name/t Sub 36 ↔ 45.5px(−21% · x1 소환만) · T334 lock',
}

CREATE_CALL = re.compile(r'\.(Text|Label|Bold|Stroked|IconTextRow|Btn)\s*\(\s*[^,()]+,\s*"([^"]+)"')
KIND = re.compile(r'TextKind\s*\.\s*(\w+)')
FONT_SIZE = re.compile(r'(?<![\w-])font-size\s*:\s*([\d.]+)rem\b')


# ── 정본 CSS ──────────────────────────────────────────────────────────────────────────────
def _blank_comments(css):
    """주석을 같은 길이 공백으로 — **줄바꿈은 남긴다**(줄 번호가 정본과 같아야 한다)."""
    return re.sub(r'/\*.*?\*/', lambda m: re.sub(r'[^\n]', ' ', m.group(0)), css, flags=re.S)


def _excluded_ranges(css):
    """`@keyframes …{…}` · `@media …{…}` 블록의 (시작, 끝) 오프셋 — 그 안의 선언은 정적이 아니다."""
    out = []
    for m in re.finditer(r'@(?:-webkit-)?(?:keyframes|media)\b[^{]*\{', css):
        i = m.end(); depth = 1
        while i < len(css) and depth:
            if css[i] == '{': depth += 1
            elif css[i] == '}': depth -= 1
            i += 1
        out.append((m.start(), i))
    return out


def sized_text(css_text, lo=LO, hi=HI):
    """{선택자: (줄, rem)} — 정적 `font-size: <rem>rem` 이 lo~hi 인 선택자. 뒤 규칙이 앞을 덮는다(같은 선택자가 범위 밖 값으로 덮으면 빠진다)."""
    css = _blank_comments(css_text)
    excl = _excluded_ranges(css)
    found = {}
    for m in re.finditer(r'([^{}]+)\{([^{}]*)\}', css):
        if any(a <= m.start() < b for a, b in excl):
            continue
        sels = [s.strip() for s in m.group(1).split(',') if s.strip()]
        body = m.group(2)
        vals = FONT_SIZE.findall(body)
        if not vals or not sels:
            continue
        rem = float(vals[-1])
        line = css.count('\n', 0, m.start() + len(m.group(1)) - len(m.group(1).lstrip())) + 1
        for s in sels:
            s = ' '.join(s.split())
            if s.startswith('@'):
                continue
            if lo <= rem <= hi:
                found[s] = (line, rem)
            elif s in found:
                del found[s]           # 뒤 규칙이 범위 밖 값으로 덮었다
    return found


# ── 클론 ────────────────────────────────────────────────────────────────────────────────
def method_body(text, name):
    m = re.search(r'\b' + re.escape(name) + r'\s*\([^)]*\)\s*(?:where[^{]*)?\{', text)
    if not m:
        return None
    i = m.end(); depth = 1
    while i < len(text) and depth:
        if text[i] == '{': depth += 1
        elif text[i] == '}': depth -= 1
        i += 1
    return text[m.end():i]


def _kind_after(text, start, limit=None):
    """start 뒤 첫 `TextKind.X` — 같은 문장 안(`;` 전)에서만."""
    end = text.find(';', start)
    if end < 0: end = len(text)
    if limit is not None: end = min(end, start + limit)
    m = KIND.search(text, start, end)
    return m.group(1) if m else None


def find_kind(text, spot):
    """(종류 이름 또는 None, 문구)."""
    if spot.startswith('$'):
        i = text.find('"%s"' % spot[1:])
        if i < 0: return None, '«%s» 문자열이 없다' % spot[1:]
        k = _kind_after(text, i, 400)
        return k, (None if k else '«%s» 뒤 400자 안에 TextKind 가 없다' % spot[1:])
    names = spot.lstrip('#').split('/')
    pos = 0
    for n in names[:-1]:
        j = text.find('"%s"' % n, pos)
        if j < 0: return None, '«%s» 상자가 없다' % n
        pos = j
    inner = names[-1]
    for m in CREATE_CALL.finditer(text, pos):
        if m.group(2) == inner:
            k = _kind_after(text, m.end())
            return k, (None if k else '«%s» 호출 문장에 TextKind 가 없다' % inner)
    return None, '«%s» 이름으로 만드는 글자 호출이 없다' % inner


def _find_num(obj, key):
    if isinstance(obj, dict):
        if key in obj and isinstance(obj[key], (int, float)):
            return float(obj[key])
        for v in obj.values():
            r = _find_num(v, key)
            if r is not None: return r
    elif isinstance(obj, list):
        for v in obj:
            r = _find_num(v, key)
            if r is not None: return r
    return None


def load_catalog(path):
    """(종류 → px, rem_px)"""
    with open(path, encoding='utf-8') as f:
        cat = json.load(f)
    kinds = {e['kind']: float(e['size']) for e in cat['textKinds']}
    rem_h = None
    for e in cat['layout']:
        if e.get('key') == 'rem_h': rem_h = float(e['value'])
    if rem_h is None:
        raise KeyError('layout.rem_h 가 없다')
    return kinds, rem_h * float(cat['reference']['h'])


def check_site(site, game, kinds, rem_px, resdir):
    """(px 또는 None, 문구)"""
    src = None
    if '|' in site:
        site, src = site.split('|', 1)
    path, spot = site, None
    m = re.match(r'([^#@$]+)(?:@(\w+))?([#$].*)?$', site)
    if not m:
        return None, '%s — 자리 표기를 못 읽는다' % site
    path, meth, spot = m.group(1), m.group(2), m.group(3)
    full = os.path.join(game, path)
    try:
        with open(full, encoding='utf-8') as f:
            text = f.read()
    except OSError:
        return None, '%s — 파일이 없다' % path
    if meth:
        text = method_body(text, meth)
        if text is None:
            return None, '%s — 메서드 %s 를 못 찾았다' % (path, meth)
    if src is not None:
        if not spot or ('"%s"' % spot.lstrip('#$').split('/')[-1]) not in text:
            return None, '%s — 이름 «%s» 자리가 없다' % (path, spot)
        if not src.startswith('res:') or src.count(':') != 2:
            return None, '%s — «res:<Name>:<key>» 꼴이 아니다' % site
        _, name, key = src.split(':')
        try:
            with open(os.path.join(resdir, name + '.json'), encoding='utf-8') as f:
                rem = _find_num(json.load(f), key)
        except (OSError, ValueError):
            return None, '%s — 표 %s.json 을 못 읽었다' % (path, name)
        if rem is None:
            return None, '%s — 표 %s.json 에 «%s» 이 없다' % (path, name, key)
        return rem * rem_px, '표 %s.%s = %.2frem' % (name, key, rem)
    if not spot:
        return None, '%s — 자리(#이름·$문자열)가 없다' % path
    k, why = find_kind(text, spot)
    if k is None:
        return None, '%s — %s' % (path, why)
    if k not in kinds:
        return None, '%s — 종류 «%s» 가 카탈로그에 없다' % (path, k)
    return kinds[k], '%s %g' % (k, kinds[k])


def run(css_path, game, catalog, resdir, list_all=False, out=print):
    try:
        with open(css_path, encoding='utf-8') as f:
            sites = sized_text(f.read())
    except OSError:
        out('✗ check_text_kinds: 정본 CSS 를 못 읽었다 — %s' % css_path)
        return 2
    try:
        kinds, rem_px = load_catalog(catalog)
    except (OSError, ValueError, KeyError) as e:
        out('✗ check_text_kinds: 카탈로그를 못 읽었다 — %s (%s)' % (catalog, e))
        return 2
    bad, ok_n, skip_n, known_n = [], 0, 0, 0
    for sel, places in TABLE.items():
        if sel not in sites:
            bad.append('표의 선택자 «%s» 가 정본의 %.2f~%.2frem 문자 목록에 없다 — 표를 고쳐라' % (sel, LO, HI)); continue
        line, rem = sites[sel]
        want = rem * rem_px
        for site in places:
            px, why = check_site(site, game, kinds, rem_px, resdir)
            if px is None:
                bad.append('%s(%d) → %s' % (sel, line, why)); continue
            dev = px / want - 1.0
            if abs(dev) > TOL:
                bad.append('%s(%d · %.2frem = %.1fpx) → %s = %.1fpx (%+.0f%% · 허용 ±%d%%)' % (sel, line, rem, want, why, px, dev * 100, TOL * 100))
            else:
                ok_n += 1
    for sel in SKIP:
        if sel in sites: skip_n += 1
        else: bad.append('SKIP 의 «%s» 가 정본 목록에 없다 — 줄을 지워라' % sel)
    for sel in KNOWN:
        if sel in sites: known_n += 1
        else: bad.append('KNOWN 의 «%s» 가 정본 목록에 없다 — 줄을 지워라' % sel)
    undecided = [s for s in sites if s not in TABLE and s not in SKIP and s not in KNOWN]
    if list_all:
        for s in sorted(undecided, key=lambda x: sites[x][0]):
            line, rem = sites[s]
            out('  미정 %5d %-52s %.2frem = %5.1fpx' % (line, s[:52], rem, rem * rem_px))
    if bad:
        out('✗ check_text_kinds: %d곳' % len(bad))
        for b in bad: out('  · ' + b)
        return 1
    out('✓ check_text_kinds: 정본 %.2f~%.2frem 글자 %d 선택자(1rem = %.1fpx) · 자리 초록 %d · 글리프 자리(SKIP) %d · KNOWN %d · 미정 %d(--list)'
        % (LO, HI, len(sites), rem_px, ok_n, skip_n, known_n, len(undecided)))
    return 0


def self_test():
    fails = []
    def eq(name, got, want):
        if got != want: fails.append('%s: %r ≠ %r' % (name, got, want))
    css = '''.a { font-size: 1.3rem; }
/* 주석
   .ghost { font-size: 1.3rem } */
.b, .c { font-size: 1.5rem }
.d { font-size: 1.2rem }
.e { font-size: 1.3rem } .e { font-size: .8rem }
.f { font-size: .8rem } .f { font-size: 1.22rem }
@keyframes k { 0% { font-size: 1.4rem } }
@media (max-width: 1px) { .m { font-size: 1.4rem } }
.g { font-size: 1.32rem }
.h { font-size: 1.25rem }
'''
    s = sized_text(css)
    eq('ⓐ 범위 안만', ('.a' in s, '.d' in s), (True, False))
    eq('ⓑ 주석 안은 안 센다 · 줄 번호는 정본 그대로', ('.ghost' in s, s['.b'][0]), (False, 4))
    eq('ⓒ 쉼표 목록은 하나씩', '.c' in s, True)
    eq('ⓓ 뒤 규칙이 범위 밖 값으로 덮으면 빠진다', '.e' in s, False)
    eq('ⓔ 뒤 규칙이 범위 안으로 올리면 든다', s.get('.f', (0, 0))[1], 1.22)
    eq('ⓕ @keyframes·@media 안은 정적이 아니다', ('k' in ' '.join(s), '.m' in s), (False, False))
    with tempfile.TemporaryDirectory() as d:
        cssp = os.path.join(d, 'style.css'); open(cssp, 'w', encoding='utf-8').write(css)
        game = os.path.join(d, 'game'); os.makedirs(os.path.join(game, 'Ui'))
        res = os.path.join(d, 'res'); os.makedirs(res)
        cat = os.path.join(d, 'catalog.json')
        json.dump({'reference': {'w': 1080, 'h': 1920},
                   'textKinds': [{'kind': 'Title', 'size': 60}, {'kind': 'Button', 'size': 44}, {'kind': 'Sub', 'size': 36}],
                   'layout': [{'key': 'rem_h', 'value': 0.018957}]}, open(cat, 'w'))
        json.dump({'x': {'amt_font_rem': 1.32}}, open(os.path.join(res, 'Tbl.json'), 'w'))
        open(os.path.join(game, 'Ui', 'A.cs'), 'w', encoding='utf-8').write(
            'class A { void Build(Transform p) { var t = UiKit.Text(p, "big", TextKind.Title, "x", "ink");\n'
            '  var b = PopupKit.Btn(p, "go", "y", "pp_blue", "pp_blue_dk", () => {}, 1f, 2f, "ink", TextKind.Button);\n'
            '  RectTransform box = UiKit.Box(p, "wrap"); var t2 = PetSkillKit.Text(box, "t", TextKind.Sub, "z", c);\n'
            '  RectTransform row = UiKit.Box(p, "amt-row"); }\n'
            ' void Style(string cls) { switch (cls) { case "dmg-kill": kind = TextKind.Button; break; } }\n'
            ' void Other(Transform p) { var u = UiKit.Text(p, "big", TextKind.Sub, "q", "ink"); } }\n')
        kinds, rem_px = load_catalog(cat)
        eq('ⓖ 1rem 은 rem_h × 기준 높이', round(rem_px, 1), 36.4)
        global TABLE, SKIP, KNOWN
        saved = (TABLE, SKIP, KNOWN)
        try:
            lines = []
            TABLE = {'.b': ['Ui/A.cs#big'], '.h': ['Ui/A.cs#go'], '.a': ['Ui/A.cs#wrap/t|res:Tbl:amt_font_rem'], '.g': ['Ui/A.cs$dmg-kill']}
            SKIP = {}; KNOWN = {}
            eq('ⓗ 맞는 자리 넷(#이름 · Btn 의 뒤쪽 TextKind · #바깥/안쪽 + 표 rem · $문자열) → rc 0', run(cssp, game, cat, res, out=lines.append), 0)
            TABLE = {'.a': ['Ui/A.cs#wrap/t']}
            eq('ⓘ 1.3rem(47px) 자리를 Sub 36 으로 → rc 1', run(cssp, game, cat, res, out=lines.append), 1)
            eq('ⓘ 문구에 편차', any('-24%' in l for l in lines), True)
            TABLE = {'.b': ['Ui/A.cs@Other#big']}
            eq('ⓙ 메서드 안에서 찾으면 그 메서드의 종류(Sub → 1.5rem 55px 에 −34%) → rc 1', run(cssp, game, cat, res, out=lines.append), 1)
            TABLE = {'.b': ['Ui/A.cs#none']}
            eq('ⓚ 이름이 없으면 rc 1', run(cssp, game, cat, res, out=lines.append), 1)
            TABLE = {'.d': ['Ui/A.cs#big']}
            eq('ⓛ 표의 선택자가 목록에 없으면 rc 1', run(cssp, game, cat, res, out=lines.append), 1)
            TABLE = {}; KNOWN = {'.a': 'T391 ⓑ'}; SKIP = {'.b': '글리프'}
            eq('ⓜ KNOWN·SKIP 은 지나간다', run(cssp, game, cat, res, out=lines.append), 0)
            KNOWN = {'.nope': 'x'}; SKIP = {}
            eq('ⓝ KNOWN 이 목록에 없으면 rc 1', run(cssp, game, cat, res, out=lines.append), 1)
            KNOWN = {}
            lines = []
            run(cssp, game, cat, res, list_all=True, out=lines.append)
            eq('ⓞ --list 는 미정을 줄 번호 차례로', [l.split()[1] for l in lines if l.startswith('  미정')][:2], ['1', '4'])
            eq('ⓟ CSS 없음 → rc 2', run(os.path.join(d, 'no.css'), game, cat, res, out=lines.append), 2)
            eq('ⓠ 카탈로그 없음 → rc 2', run(cssp, game, os.path.join(d, 'no.json'), res, out=lines.append), 2)
        finally:
            TABLE, SKIP, KNOWN = saved
    n = 20
    if fails:
        print('✗ check_text_kinds --self-test 실패 %d' % len(fails))
        for f in fails: print('  · ' + f)
        return 1
    print('✓ check_text_kinds --self-test %d칸 통과' % n)
    return 0


def main(argv):
    css, game, catalog, res, list_all = CSS_DEFAULT, GAME_DEFAULT, CATALOG_DEFAULT, RES_DEFAULT, False
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--self-test':
            return self_test()
        if a == '--list':
            list_all = True
        elif a in ('--css', '--game', '--catalog', '--res') and i + 1 < len(argv):
            v = argv[i + 1]; i += 1
            if a == '--css': css = v
            elif a == '--game': game = v
            elif a == '--catalog': catalog = v
            else: res = v
        else:
            print('사용: check_text_kinds.py [--css <style.css>] [--game <Assets/Scripts/Game>] [--catalog <catalog.json>] [--res <Resources>] [--list] [--self-test]')
            return 2
        i += 1
    return run(css, game, catalog, res, list_all)


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
