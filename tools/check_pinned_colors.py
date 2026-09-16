#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T377 — 정본이 **선택자에만 리터럴 hex 로 못박은 면 색**(전역 토큰과 *다른* 값) ↔ 클론의 그 자리가 **전용 키**로 같은 값을 쓰는가.

왜 이 자가 있나(검수 Q · 런 641 실측): 정본 `style.css` 8692 는 «전역 `--pp-red`/`--pp-red-dk` 는 안 건드린다 — 화면마다 빨강이
다르니 토큰을 옮기지 말 것 … 그래서 **버튼 규칙에만 리터럴**로 준다» 고 적어 뒀는데, 클론은 판매 버튼·채팅 뒤로 버튼을 전역
토큰 `pp_red`(#e8362f)로 찍어 정본 `#ff1017` 과 어긋났다. 리터럴 자리는 «토큰으로 뭉개지 말라» 는 뜻이므로 자리마다
**전용 키**(`catalog.json` 또는 곁 표 `Resources/PinnedColorUi.json`)로 같은 값을 준다.

무엇을 세나: 주석을 걷은 `style.css` 에서 `background`/`background-color` 가 **리터럴 hex 로 시작하는** 선언(면 색) 전부 —
그중 `--토큰: #hex` 로 정의된 값과 **같은** 것은 뺀다(토큰을 쓰는 게 맞는 자리). 남는 것이 «못박은 면 색» 이다.

선택자 ↔ 클론 자리 짝은 TABLE 하나가 쥔다(T109 `check_keyline` · T365 `check_box_borders` 꼴):
  "Ui/File.cs#name|<source>:<key>"   그 파일에 «"name"» 이 있고 «"key"» 호출이 있으며, <source> 의 key 값이 정본 hex 와 같다
  "Ui/File.cs@Method|<source>:<key>" 도우미 메서드 본문에 «"key"» 호출이 있고 값이 같다
  "Ui/File.cs|<source>:<key>"        파일 어디든 «"key"» 호출이 있고 값이 같다
  "—<이유>"                           대조하지 않는다(정본이 그 자리를 안 그리는 등)
  <source> = catalog(`Assets/Forge/catalog.json` 의 colors) · res:<Name>(`Assets/Forge/Resources/<Name>.json` 안 어디든 "key": "#hex")
표에 없는 선택자 = **미정**(막지 않는다 · `--list` 로 본다 · 회차마다 표에 더한다). 빈자리 중 임자가 정해진 것은 KNOWN 에 둔다.

사용:  python3 tools/check_pinned_colors.py [--css <style.css>] [--game <Assets/Scripts/Game>] [--list] [--self-test]
rc:    0 = 표의 선택자가 전부 정본에 있고 · 표의 자리 전부가 (호출 + 같은 값 이거나 KNOWN) · 1 = 아니다 · 2 = 정본 CSS 를 못 읽었다
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

# ── 정본 선택자 ↔ 클론 자리 ──────────────────────────────────────────────────────────────────────
TABLE = {
    # T377 1회차 — 채팅 뒤로 버튼(정본 3283 `#ff1017` · 클론은 pp_red 였다)
    '.chat-input-bar .btn.danger.round': ['Ui/ChatScreen.cs#face|res:PinnedColorUi:chat_back_face'],
    # 이미 맞던 자리 둘(등재문이 «같이 깨뜨리지 마라» 고 적은 것) — 자가 그 값을 지킨다
    '.league-reward-banner': ['Ui/LeagueSheet.cs|catalog:lgr_ribbon'],
    '.petd-wrap .petd-btn.danger': ['Ui/PetSkillKit.cs@PaperButton|res:PetSkillUi:pp_red'],   # 5435 · PetSkillUi pp_red = #f2191d(전역 pp_red 와 다른 값 — 이미 맞다)
    # T377 3회차 — 판매 버튼 둘(비교 팝업 · 판매 경고)의 면(8686 `#ff1017` · 턱 `#4e0507` 은 box-shadow 라 이 자의 «면» 목록엔 없다 — PlayMode 자가 잰다)
    '.btn.btn.danger.danger': ['Ui/ForgeCraftPopup.cs@PinSell|res:PinnedColorUi:sell_btn_face'],
    '.btn.btn.sell.sell': ['Ui/ForgeCraftPopup.cs@PinSell|res:PinnedColorUi:sell_btn_face'],
    # T377 4회차 — 산 lock 밖 자리 셋. 하나는 토큰으로 뭉개져 있었고(모루 «보류» 배지) 둘은 이미 제 값이라 자가 지키기만 한다.
    '.anvil-btn.held-slot .held-tag': ['Ui/ForgeSheet.cs|res:PinnedColorUi:held_tag_face'],   # 정본 999 #f0a020 ↔ 클론은 전역 coin(#ffd54f) 이었다
    '.skill-btn.auto': ['Ui/SkillBar.cs|res:PetSkillUi:sb_auto_bg'],                          # 정본 624 #2f3a33 ↔ PetSkillUi sb_auto_bg 같은 값(이미 맞다)
    '.skill-btn.auto.on': ['Ui/SkillBar.cs|res:PetSkillUi:sb_auto_on_bg'],                    # 정본 629 #2e7d32 ↔ sb_auto_on_bg 같은 값(이미 맞다)
}
# 임자가 정해진 빈자리(파일 lock 뒤) — 붙이면 여기서 지운다
KNOWN = {
}

# ── T396 — «못박은 **잉크** 색»(`color`)의 선택자 ↔ 클론 자리. 자리 꼴은 위 TABLE 과 **같다**.
#    1회차는 **자와 셈만** 세운다: 배선할 파일(`DamageNumbers.cs` · `ChatScreen.cs` · 탭·부화·소환 …)이
#    그때그때 남의 산 lock 이라, 자리를 하나씩 붙이는 것은 그 lock 이 풀리는 회차의 몫이다(결정 아래).
TABLE_INK = {
    # T396 2회차 — 전투 숫자 다섯(정본 509 «크리 위계는 '크기'가 아니라 **색·펀치**로 준다»).
    #   클론은 크리를 `cp`(#ff8a65), 나머지를 `stage_ink`(흰색)로 찍고 있었다 — 스킬·막음은 파랑·하늘색인데 흰색이었다.
    '.float-dmg.dmg-crit': ['Battle/DamageNumbers.cs@Style|res:PinnedColorUi:dmg_crit_ink'],
    '.float-dmg.dmg-kill': ['Battle/DamageNumbers.cs@Style|res:PinnedColorUi:dmg_kill_ink'],
    '.float-dmg.dmg-skill': ['Battle/DamageNumbers.cs@Style|res:PinnedColorUi:dmg_skill_ink'],
    '.float-dmg.dmg-hero': ['Battle/DamageNumbers.cs@Style|res:PinnedColorUi:dmg_hero_ink'],
    '.float-dmg.block': ['Battle/DamageNumbers.cs@Style|res:PinnedColorUi:dmg_block_ink'],
    # 값이 이미 같아 토큰을 써도 되는 자리(자가 그 값을 지킨다 · T377 결정 636 과 같은 셈)
    '.float-dmg.heal': ['Battle/DamageNumbers.cs@Style|catalog:pip_done'],
    # T396 3회차 — 산 lock 이 없는 파일 셋. 둘은 이미 맞았고(자가 그 값을 지킨다) 하나는 근사였다.
    '.bw-sub': ['Ui/BattleOverlay.cs|catalog:coin'],                        # 정본 409 #ffd54f ↔ 카탈로그 coin 같은 값
    '.waypoint-time': ['Ui/Waypoints.cs|res:WaypointsUi:time_ink'],         # 정본 326 #ffd54f ↔ WaypointsUi.time_ink 같은 값
    '.offline-sub': ['Ui/OfflinePopup.cs|res:PinnedColorUi:offline_sub_ink'],   # 정본 267 #ccc ↔ 클론은 pp_gray(#c4c4c4) 였다
}
KNOWN_INK = {
}

HEX = re.compile(r'#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})\b')
# `@keyframes` 단계 — `0%` · `12.5%` · `from` · `to` · 쉼표로 묶인 것(`0%, 40%`). 선택자가 아니다(T396 3회차).
STEP_SEL = re.compile(r'^(?:\d+(?:\.\d+)?%|from|to)(?:\s*,\s*(?:\d+(?:\.\d+)?%|from|to))*$')


def norm_hex(h):
    h = h.lower()
    if len(h) == 4:
        return '#' + ''.join(c * 2 for c in h[1:])
    return h[:7]


def strip_comments(css):
    return re.sub(r'/\*.*?\*/', lambda m: '\n' * m.group(0).count('\n'), css, flags=re.S)


# 걷는 속성 — 면(T377)과 **잉크**(T396). 잉크를 면과 **한 목록에 섞지 않는다**: 같은 선택자가 둘 다
# 못박을 수 있고(예: 알약이 바탕과 글자를 같이 준다) 그러면 뒤가 앞을 덮어 한 자리가 통째로 사라진다.
FACE_PROPS = r'(?:background|background-color)'
INK_PROPS = r'color'


def pinned_decls(css_text, props):
    """정본에서 «리터럴 hex · 토큰과 다른 값» 선언을 걷는다 → {selector: (line, hex)} (뒤 규칙이 앞을 덮는다).

    `props` 는 속성 이름 정규식이다 — 선언 **머리**에서만 맞춘다(`\s*<props>\s*:`). 그래서
    `background-color` 는 잉크(`color`)로 안 새고 `border-color`·`-webkit-text-stroke` 도 안 걸린다.
    """
    css = strip_comments(css_text)
    tokens = set()
    for m in re.finditer(r'--[a-z0-9-]+\s*:\s*(#[0-9a-fA-F]{3,6})\b', css):
        tokens.add(norm_hex(m.group(1)))
    rx = re.compile(r'\s*' + props + r'\s*:\s*(#[0-9a-fA-F]{3,6})\b')
    out = {}
    for m in re.finditer(r'([^{}]+)\{([^{}]*)\}', css):
        sel_raw = ' '.join(m.group(1).split())
        if sel_raw.startswith('@') or 'keyframes' in sel_raw:
            continue
        # ⚠ `@keyframes` 의 **단계**(`0%` · `from` · `to`)는 선택자가 아니다(T396 3회차).
        #    바깥 블록 `@keyframes name { … }` 은 위에서 걸러지지만, 이 정규식은 **안쪽 블록**을 따로 물어
        #    `0% { color: #ff8a1e }` 를 «선택자 0%» 로 셌다 — 그것은 «못박은 색» 이 아니라 **연출 중간값**(T173·T334 축)이고,
        #    고칠 자리가 없어 «미정» 에 영원히 남는다(실측: 잉크 105 중 둘이 `@keyframes dmgcrit` 의 0%·9% 였다).
        if STEP_SEL.match(sel_raw):
            continue
        line = css[:m.start()].count('\n') + 1
        for d in m.group(2).split(';'):
            mm = rx.match(d)
            if not mm:
                continue
            h = norm_hex(mm.group(1))
            if h in tokens:
                continue
            for sel in [s.strip() for s in sel_raw.split(',') if s.strip()]:
                out[sel] = (line, h)
    return out


def pinned_faces(css_text):
    """«못박은 면 색»(T377) — `background`/`background-color` 리터럴."""
    return pinned_decls(css_text, FACE_PROPS)


def pinned_inks(css_text):
    """«못박은 잉크 색»(T396) — `color` 리터럴. 전투 숫자 다섯·하위 탭 켜짐 글자·채팅 미리보기 … 가 여기 있다."""
    return pinned_decls(css_text, INK_PROPS)


def _find_hex(obj, key):
    """JSON 안 어디든 "key": "#hex" — 첫 것."""
    if isinstance(obj, dict):
        v = obj.get(key)
        if isinstance(v, str) and HEX.match(v):
            return norm_hex(v)
        for x in obj.values():
            r = _find_hex(x, key)
            if r:
                return r
    elif isinstance(obj, list):
        for x in obj:
            if isinstance(x, dict) and x.get('key') == key and isinstance(x.get('hex'), str):
                return norm_hex(x['hex'])
            r = _find_hex(x, key)
            if r:
                return r
    return None


def source_hex(source, key, catalog, resdir):
    if source == 'catalog':
        try:
            with open(catalog, encoding='utf-8') as f:
                return _find_hex(json.load(f), key)
        except (OSError, ValueError):
            return None
    if source.startswith('res:'):
        p = os.path.join(resdir, source[4:] + '.json')
        try:
            with open(p, encoding='utf-8') as f:
                return _find_hex(json.load(f), key)
        except (OSError, ValueError):
            return None
    return None


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


def check_site(site, game, catalog, resdir):
    """(ok, 문구)"""
    if site.startswith('—'):
        return True, site
    if '|' not in site:
        return False, '%s — 자리 표기에 «|source:key» 가 없다' % site
    place, src = site.split('|', 1)
    if ':' not in src:
        return False, '%s — «source:key» 꼴이 아니다' % site
    source, key = src.rsplit(':', 1)
    if place.count('#') == 1:
        path, name = place.split('#'); kind = '#'
    elif place.count('@') == 1:
        path, name = place.split('@'); kind = '@'
    else:
        path, name, kind = place, None, ''
    full = os.path.join(game, path)
    try:
        with open(full, encoding='utf-8') as f:
            text = f.read()
    except OSError:
        return False, '%s — 파일이 없다' % full
    scope = text
    if kind == '@':
        scope = method_body(text, name)
        if scope is None:
            return False, '%s — 메서드 %s 를 못 찾았다' % (path, name)
    elif kind == '#' and ('"%s"' % name) not in text:
        return False, '%s — 이름 «%s» 으로 만든 자리가 없다' % (path, name)
    if ('"%s"' % key) not in scope:
        return False, '%s — «"%s"» 호출이 없다(%s)' % (path, key, '메서드 ' + name if kind == '@' else '파일 어디든')
    return True, None


def _judge(kind, table, known, decls, game, catalog, resdir, bad, list_all, out):
    """한 갈래(면·잉크)를 대조한다 — (자리 초록, KNOWN, 미정 수). `bad` 에 어긋난 것을 쌓는다(순수하지 않은 것은 출력뿐)."""
    ok_n = known_n = 0
    for sel, sites in table.items():
        if sel not in decls:
            bad.append('표의 선택자 «%s» 가 정본의 «못박은 %s» 목록에 없다(리터럴이 아니거나 토큰과 같은 값이 됐다) — 표를 고쳐라' % (sel, kind))
            continue
        line, want = decls[sel]
        for site in sites:
            ok, why = check_site(site, game, catalog, resdir)
            if not ok:
                bad.append('%s(%d) → %s' % (sel, line, why)); continue
            if site.startswith('—'):
                ok_n += 1; continue
            source, key = site.split('|', 1)[1].rsplit(':', 1)
            got = source_hex(source, key, catalog, resdir)
            if got is None:
                bad.append('%s(%d) → %s 에 «%s» 이 없다' % (sel, line, source, key))
            elif got != want:
                bad.append('%s(%d) → «%s» = %s 인데 정본은 %s — 전용 키의 값이 다르다' % (sel, line, key, got, want))
            else:
                ok_n += 1
    for sel in known:
        if sel in decls:
            known_n += 1
        else:
            bad.append('KNOWN 의 «%s» 가 정본 %s 목록에 없다 — 줄을 지워라' % (sel, kind))
    undecided = [s for s in decls if s not in table and s not in known]
    if list_all:
        for s in undecided:
            out('  미정 %s %5d %-52s %s' % (kind[:2], decls[s][0], s[:52], decls[s][1]))
    return ok_n, known_n, len(undecided)


def run(css_path, game, catalog, resdir, list_all=False, out=print):
    try:
        with open(css_path, encoding='utf-8') as f:
            css_text = f.read()
        faces = pinned_faces(css_text)
        inks = pinned_inks(css_text)
    except OSError:
        out('✗ check_pinned_colors: 정본 CSS 를 못 읽었다 — %s' % css_path)
        return 2
    bad = []
    ink_ok, ink_known, ink_undec = _judge('잉크 색', TABLE_INK, KNOWN_INK, inks, game, catalog, resdir, bad, list_all, out)
    ok_n, known_n = 0, 0
    for sel, sites in TABLE.items():
        if sel not in faces:
            bad.append('표의 선택자 «%s» 가 정본의 «못박은 면 색» 목록에 없다(리터럴이 아니거나 토큰과 같은 값이 됐다) — 표를 고쳐라' % sel)
            continue
        line, want = faces[sel]
        for site in sites:
            ok, why = check_site(site, game, catalog, resdir)
            if not ok:
                bad.append('%s(%d) → %s' % (sel, line, why)); continue
            if site.startswith('—'):
                ok_n += 1; continue
            source, key = site.split('|', 1)[1].rsplit(':', 1)
            got = source_hex(source, key, catalog, resdir)
            if got is None:
                bad.append('%s(%d) → %s 에 «%s» 이 없다' % (sel, line, source, key))
            elif got != want:
                bad.append('%s(%d) → «%s» = %s 인데 정본은 %s — 전용 키의 값이 다르다' % (sel, line, key, got, want))
            else:
                ok_n += 1
    for sel in KNOWN:
        if sel in faces:
            known_n += 1
        else:
            bad.append('KNOWN 의 «%s» 가 정본 목록에 없다 — 줄을 지워라' % sel)
    undecided = [s for s in faces if s not in TABLE and s not in KNOWN]
    if list_all:
        for s in undecided:
            out('  미정 면 %5d %-52s %s' % (faces[s][0], s[:52], faces[s][1]))
    if bad:
        out('✗ check_pinned_colors: %d곳' % len(bad))
        for b in bad:
            out('  · ' + b)
        return 1
    out('✓ check_pinned_colors: 정본 «못박은 면 색» %d 선택자 · 자리 초록 %d · KNOWN %d · 미정 %d'
        ' ‖ «못박은 잉크 색» %d 선택자(색 %d) · 자리 초록 %d · KNOWN %d · 미정 %d  (--list)'
        % (len(faces), ok_n, known_n, len(undecided),
           len(inks), len(set(v[1] for v in inks.values())), ink_ok, ink_known, ink_undec))
    return 0


def self_test():
    fails = []
    def eq(name, got, want):
        if got != want:
            fails.append('%s: %r ≠ %r' % (name, got, want))
    css = ''':root { --pp-red: #e8362f; --x: #fff; }
/* 주석 안 .ghost { background: #123456 } */
.tok { background: var(--pp-red); }
.same { background-color: #E8362F; }
.short { background: #fff; }
.pin, .pin2 { background: #ff1017; box-shadow: none; }
.pin { background: #ff1017 url(x) }
.over { background: #111; }
.over { background: #222222; }
'''
    faces = pinned_faces(css)
    eq('ⓐ 토큰과 같은 값은 뺀다', '.same' in faces, False)
    eq('ⓑ 3자리 hex 도 토큰과 같으면 뺀다', '.short' in faces, False)
    eq('ⓒ 쉼표 목록은 하나씩', ('.pin' in faces, '.pin2' in faces), (True, True))
    eq('ⓓ 값 정규화', faces['.pin'][1], '#ff1017')
    eq('ⓔ 뒤 규칙이 앞을 덮는다', faces['.over'][1], '#222222')
    eq('ⓕ 주석 안은 안 센다', '.ghost' in faces, False)

    # ── T396 ⓖ~ⓙ — 잉크(`color`) 갈래. **면과 안 섞인다**: 같은 선택자가 둘 다 못박을 수 있어
    #    한 목록에 담으면 뒤가 앞을 덮어 한 자리가 통째로 사라진다(그래서 목록을 둘로 둔다).
    ink_css = ''':root { --ink: #eceff1; --pp-red: #e8362f; }
.both { background: #ff1017; color: #7ee2a8; }
.bgonly { background-color: #123456; }
.tokink { color: var(--ink); }
.sameink { color: #ECEFF1; }
.borderc { border-color: #abcdef; }
.stroke { -webkit-text-stroke: 2px #abcdef; }
.crit { color: #ff8a1e }
'''
    inks = pinned_inks(ink_css)
    ifaces = pinned_faces(ink_css)
    eq('ⓖ `background-color` 는 잉크로 안 샌다', '.bgonly' in inks, False)
    eq('ⓖ `border-color`·`-webkit-text-stroke` 도 안 샌다', ('.borderc' in inks, '.stroke' in inks), (False, False))
    eq('ⓗ 토큰과 같은 잉크는 뺀다', ('.tokink' in inks, '.sameink' in inks), (False, False))
    eq('ⓘ 한 선택자가 면·잉크를 둘 다 못박으면 **양쪽에** 선다',
       (inks.get('.both', (0, None))[1], ifaces.get('.both', (0, None))[1]), ('#7ee2a8', '#ff1017'))
    eq('ⓘ 잉크만 있는 자리는 면 목록에 없다', '.crit' in ifaces, False)
    eq('ⓙ 값 정규화·줄 번호', (inks['.crit'][1], inks['.crit'][0] > 0), ('#ff8a1e', True))

    # ⓚ T396 3회차 — `@keyframes` 의 **단계**는 선택자가 아니다. 바깥 블록은 이미 걸러지지만
    #    안쪽 블록(`0% { … }`)이 따로 물려 «선택자 0%» 로 세어지던 자리다(실측: `@keyframes dmgcrit` 의 0%·9%).
    kf_css = '''@keyframes dmgcrit {
  0%   { color: #ff8a1e; }
  12.5% { background: #123456; }
  from, to { color: #abcdef; }
}
.real { color: #ff8a1e; }
'''
    kf_ink = pinned_inks(kf_css); kf_face = pinned_faces(kf_css)
    eq('ⓚ 키프레임 단계는 잉크로 안 센다', sorted(kf_ink), ['.real'])
    eq('ⓚ 키프레임 단계는 면으로도 안 센다', sorted(kf_face), [])
    eq('ⓚ `from, to` 묶음도 안 센다', 'from, to' in kf_ink, False)
    eq('ⓚ 진짜 선택자는 그대로 선다', kf_ink['.real'][1], '#ff8a1e')
    with tempfile.TemporaryDirectory() as d:
        cssp = os.path.join(d, 'style.css'); open(cssp, 'w', encoding='utf-8').write(css)
        game = os.path.join(d, 'game'); os.makedirs(os.path.join(game, 'Ui'))
        res = os.path.join(d, 'res'); os.makedirs(res)
        cat = os.path.join(d, 'catalog.json')
        json.dump({'colors': [{'key': 'ok_key', 'hex': '#FF1017'}, {'key': 'bad_key', 'hex': '#e8362f'}]}, open(cat, 'w'))
        json.dump({'colors': {'pin2_face': '#ff1017'}}, open(os.path.join(res, 'PinnedColorUi.json'), 'w'))
        open(os.path.join(game, 'Ui', 'A.cs'), 'w', encoding='utf-8').write(
            'class A { void Build() { var f = Rounded(p, "face", "pp_red", 1); f.color = PinnedColorUi.C("pin2_face"); }\n'
            ' void Other() { X("ok_key"); Y("bad_key"); } }\n')
        global TABLE, KNOWN, TABLE_INK, KNOWN_INK
        # ⚠ **잉크 표도 같이 치운다**(T396 2회차): 안 치우면 임시 CSS 에 없는 실물 선택자(`.float-dmg…`)가
        #    «표의 선택자가 정본 목록에 없다» 로 빨개져 **자기 검사가 제 손으로 깨진다**(실제로 그랬다).
        saved = (TABLE, KNOWN, TABLE_INK, KNOWN_INK)
        TABLE_INK, KNOWN_INK = {}, {}
        try:
            lines = []
            TABLE = {'.pin': ['Ui/A.cs|catalog:ok_key'], '.pin2': ['Ui/A.cs#face|res:PinnedColorUi:pin2_face']}; KNOWN = {}
            eq('ⓖ 맞는 자리 둘 → rc 0', run(cssp, game, cat, res, out=lines.append), 0)
            TABLE = {'.pin': ['Ui/A.cs|catalog:bad_key']}
            eq('ⓗ 값이 다르면 rc 1', run(cssp, game, cat, res, out=lines.append), 1)
            eq('ⓗ 문구', any('전용 키의 값이 다르다' in l for l in lines), True)
            TABLE = {'.pin': ['Ui/A.cs@Build|catalog:ok_key']}
            eq('ⓘ 메서드 안에 호출이 없으면 rc 1', run(cssp, game, cat, res, out=lines.append), 1)
            TABLE = {'.tok': ['Ui/A.cs|catalog:ok_key']}
            eq('ⓙ 표의 선택자가 목록에 없으면 rc 1', run(cssp, game, cat, res, out=lines.append), 1)
            TABLE = {}; KNOWN = {'.pin': '남의 lock'}
            eq('ⓚ KNOWN 은 지나간다', run(cssp, game, cat, res, out=lines.append), 0)
            KNOWN = {'.nope': '없는 선택자'}
            eq('ⓛ KNOWN 이 목록에 없으면 rc 1', run(cssp, game, cat, res, out=lines.append), 1)
            TABLE = {}; KNOWN = {}
            eq('ⓜ CSS 없음 → rc 2', run(os.path.join(d, 'no.css'), game, cat, res, out=lines.append), 2)
        finally:
            TABLE, KNOWN, TABLE_INK, KNOWN_INK = saved
    n = 24   # T377 14 + T396 잉크 갈래 6 + 키프레임 단계 막이 4
    if fails:
        print('✗ check_pinned_colors --self-test 실패 %d' % len(fails))
        for f in fails:
            print('  · ' + f)
        return 1
    print('✓ check_pinned_colors --self-test %d칸 통과' % n)
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
        elif a == '--css' and i + 1 < len(argv):
            css = argv[i + 1]; i += 1
        elif a == '--game' and i + 1 < len(argv):
            game = argv[i + 1]; i += 1
        else:
            print('사용: check_pinned_colors.py [--css <style.css>] [--game <Assets/Scripts/Game>] [--list] [--self-test]')
            return 2
        i += 1
    return run(css, game, catalog, res, list_all)


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
