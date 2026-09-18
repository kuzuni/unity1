#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""T371 — 정본 `color-mix(in srgb, A p%, B)` ↔ 클론 표(`ColorMixUi.json`) ↔ 그 자리의 호출을 세 겹으로 견준다.

정본은 등급색(`--rc`)·가지색(`--bc`)에서 면·테·그림자를 **정해진 비율로 섞어** 만든다(24 선언 · 키프레임 1 제외 23).
클론은 그 비율을 코드에 박거나(§1 위반) 아예 안 섞었다 — 이 자는 ⓐ 표가 정본 값을 그대로 쥐는가 ⓑ 그 자리가 표 키를 부르는가를 본다.

  `Ui/File.cs$키`   그 파일이 `"키"` 를 부른다(표값 = 정본 비율·상대색이어야 한다)
  KNOWN            임자가 정해진 빈자리(남의 산 lock 뒤) — 배선되면 여기서 지운다

**캐스케이드(8회차)**: 같은 (선택자, 속성)에 선언이 여럿이면 화면에 서는 것은 **마지막 것 하나**다
(`background`·`border`·`box-shadow` 는 전부 «덮어쓰기» 속성이라 앞 선언이 통째로 진다). 그러니 자리도 **하나**다 —
앞 선언에 표 키를 따로 두면 «정본이 안 그리는 겹» 을 표가 쥐고 있게 되고, 배선하는 사람이 그것을 보고 겹을 하나 더 그린다.
같은 병을 `tools/check_border_radius.py` 가 14회차에 고쳤다(`.modal-card` 1rem@1752 ↔ 1.1rem@3515 — 뒤가 이긴다).

쓰기: python3 tools/check_color_mix.py [--list] [--self-test]
"""
import os
import re
import sys
import json

HERE = os.path.dirname(os.path.abspath(__file__))
CSS_DEFAULT = os.path.join('.wwwww-src', 'web', 'css', 'style.css')
GAME_DEFAULT = os.path.join('Assets', 'Scripts', 'Game')
TABLE_DEFAULT = os.path.join('Assets', 'Forge', 'Resources', 'ColorMixUi.json')

# 정본 선택자+속성 → 클론 자리(파일$표키). **한 (선택자, 속성)에 자리는 하나**다 — 선언이 여럿이면 캐스케이드가 마지막만 남긴다(8회차).
TABLE = {
    ('.equip-cell', 'background'): ['Ui/ForgeUi.cs$cell_face'],
    ('.equip-cell', 'border'): ['Ui/ForgeUi.cs$cell_line'],
    ('.anvil-btn.held-slot', 'background'): ['Ui/ForgeSheet.cs$held_face'],
    ('.anvil-btn.held-slot', 'border'): ['Ui/ForgeSheet.cs$held_line'],
    ('.anvil-btn.held-slot.deck', '--dedge'): ['Ui/ForgeSheet.cs$held_deck_edge'],
    ('.anvil-btn.held-slot.deck', '--dgap'): ['Ui/ForgeSheet.cs$held_deck_gap'],
    # 정본 `.deck::before` 는 **덱의 앞면**이고 값이 `.held-slot` 면(30%, #17181a)과 같다 — 클론도 같은 한 장이라 같은 키를 가리킨다(T371 4회차).
    ('.anvil-btn.held-slot.deck::before', 'background'): ['Ui/ForgeSheet.cs$held_face'],
    ('.auto-drop-card', 'background'): ['Ui/ForgeCraftPopup.cs$drop_card_face'],
    ('.auto-drop-card', 'border'): ['Ui/ForgeCraftPopup.cs$drop_card_line'],
    ('.craft-batch .cb-card', 'background'): ['Ui/ForgeCraftPopup.cs$batch_card_face'],
    ('.craft-batch .cb-card', 'border'): ['Ui/ForgeCraftPopup.cs$batch_card_line'],
    ('.cmp-img', 'background'): ['Ui/ForgeUi.cs$cmp_img_face'],
    ('.tech-branch-icon::before', 'background'): ['Ui/TechPanel.cs$tech_branch_face'],
    ('.tech-branch-icon::before', 'border'): ['Ui/TechPanel.cs$tech_branch_line'],
    ('#forge-item-modal .idet-icon', 'background'): ['Ui/ForgeInfoPopup.cs$idet_icon_face'],
    ('#forge-item-modal .idet-icon', 'border-color'): ['Ui/ForgeInfoPopup.cs$idet_icon_line'],
    ('.pet-tile .tile-face', 'background'): ['Ui/PetPanel.cs$pet_tile_face'],
    # 정본이 4288 과 8124 에서 두 번 말한다(값은 둘 다 60%) — **8124 가 이긴다**. 4288 쪽 키(`pet_tile_shadow`)는 8회차에 걷었다.
    ('.pet-tile .tile-face', 'box-shadow'): ['Ui/PetPanel.cs$pet_tile_shadow_2'],
    ('.petd-wrap .petd-tile', '--petd-face'): ['Ui/PetPanel.cs$petd_face'],
    ('.petd-wrap .petd-tile', 'border'): ['Ui/PetPanel.cs$petd_line'],
    # 정본 7730(55%) ↔ 8538(**62%**) — `box-shadow` 는 덮어쓰기라 7730 의 시대색 광은 **안 그려진다**. 8회차에 `cell_shadow_1` 을 걷었다.
    ('.equip-cell:not(.egg-cell)', 'box-shadow'): ['Ui/ForgeUi.cs$cell_shadow_2'],
}

# 임자가 정해진 빈자리(자리 → 이유) — 배선될 때마다 지운다. 1회차는 **배선이 0 이라 전부 여기 있다**.
KNOWN = {
    # ⚑ T371 11회차가 이유를 **바로잡았다** — 종전 문구(«T331 lock 뒤» · «T331 축과 겹친다»)는 «그 lock 만 풀리면 된다» 로 읽힌다.
    #   실측(2026-09-17 10:2x): T331 lock 은 **1019분으로 죽어 있었고 파일도 열려 있었는데** 여기 배선할 자리가 없었다 —
    #   `check_box_shadows` 의 «아직 안 본 드리운 그림자 15개» 목록에 **그 두 선택자가 그대로 들어 있다**
    #   (`.pet-tile .tile-face`(8131) · `.equip-cell:not(.egg-cell)`(8539)). 곧 **겹 자체가 클론에 아직 없다.**
    #   이 자가 쥐는 것은 «그 겹의 색을 몇 %로 섞나» 이므로, 겹이 서기 전에는 **걸 자리가 없다**(표값 0.60·0.62 는 이미 있다).
    #   ⇒ 막는 것은 lock 이 아니라 **일**이다: T331 이 그 두 자리에 바깥 광을 세우면 이 둘은 «색을 표에서 받는 한 줄» 로 닫힌다.
    # ⚑ T371 12회차(2026-09-18 21:2x)가 한 번 더 바로잡았다 — «T331 이 세운 뒤» 도 헛걸음이다.
    #   T331 은 36회차(8539)·38회차(8131)에 **드리운 그림자**(치우침 있는 검정 겹 · 표 equipcell_drop·pettile_drop)만 세우고,
    #   첫 겹(치우침 0 의 등급색 **광** — 바로 이 자가 쥐는 color-mix 겹)은 «빛 갈래 · T419 뒤로 읽어라» 로 넘겼다.
    #   그런데 T419 는 ✅ 닫힌 절이고 그 범위는 `mix-blend-mode: screen` 셋뿐 · check_box_shadows 는 «빛 갈래» 를 ELSEWHERE(아무 목록도 아님)로 센다.
    #   곧 **그 두 광 겹은 임자가 없다** → 결정 803: 이 절(T371)이 13회차에 스스로 세운다(색 = 등급색 × 표 비율 · 모양 = UiShadow 흐림 굽기 · T331 lock 뒤).
    'Ui/ForgeUi.cs$cell_shadow_2': 'T371 1회차 — 장비 셀 시대색 광(정본 8538 62% · 7730 의 55% 는 뒤 규칙이 덮어 안 그려진다) · **이 절이 13회차에 그 광 겹을 세운다**(12회차 실측: T331 36회차는 드리운 그림자 equipcell_drop 만 세우고 첫 겹 등급색 광은 «T419» 로 넘겼는데 T419 는 닫힌 절이라 임자가 없다 · 결정 803 · T331 lock 뒤)',
    'Ui/PetPanel.cs$pet_tile_shadow_2': 'T371 1회차 — 펫 타일 그림자(정본 8124 가 4288 을 덮는다 · 값은 둘 다 60%) · **이 절이 13회차에 그 광 겹을 세운다**(12회차 실측: T331 38회차는 드리운 그림자 pettile_drop 만 세우고 첫 겹 등급색 광은 «T419» 로 넘겼는데 T419 는 닫힌 절이라 임자가 없다 · 결정 803 · T331 lock 뒤)',
}

MIX = 'color-mix('


def _blank_comments(css):
    return re.sub(r'/\*.*?\*/', lambda m: ''.join('\n' if ch == '\n' else ' ' for ch in m.group(0)), css, flags=re.S)


def _blank_keyframes(css):
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


def split_top(s):
    out, depth, cur = [], 0, ''
    for ch in s:
        if ch == '(':
            depth += 1
        elif ch == ')':
            depth -= 1
        if ch == ',' and depth == 0:
            out.append(cur)
            cur = ''
        else:
            cur += ch
    out.append(cur)
    return [x.strip() for x in out]


def find_mixes(text):
    out, i = [], 0
    while True:
        i = text.find(MIX, i)
        if i < 0:
            return out
        j, depth = i + len(MIX), 1
        while j < len(text) and depth > 0:
            if text[j] == '(':
                depth += 1
            elif text[j] == ')':
                depth -= 1
            j += 1
        out.append(text[i + len(MIX):j - 1])
        i = j


def parse_rules(css_text, keep_keyframes=False):
    """[(줄, 선택자, 속성, 앞색, 비율0~1, 뒤색)] — 키프레임 안은 뺀다(연출 축)."""
    css = _blank_comments(css_text)
    if not keep_keyframes:
        css = _blank_keyframes(css)
    out = []
    for m in re.finditer(r'([^{}]+)\{([^{}]*)\}', css):
        body = m.group(2)
        if MIX not in body:
            continue
        sel = ' '.join(m.group(1).split())
        line = css[:m.start(2)].count('\n') + 1
        for decl in body.split(';'):
            if MIX not in decl:
                continue
            prop = decl.split(':')[0].strip()
            for inner in find_mixes(decl):
                parts = split_top(inner)
                if len(parts) != 3 or parts[0].replace(' ', '') != 'insrgb':
                    continue
                m2 = re.match(r'(.*?)\s+(\d+(?:\.\d+)?)%$', parts[1])
                if m2 is None:
                    continue
                out.append((line, sel, prop, m2.group(1).strip(), float(m2.group(2)) / 100.0, parts[2].strip()))
    return out


def norm_color(tok):
    """정본 색 표기를 견줄 수 있는 꼴로 — `#fff` → `#ffffff` · `transparent` 그대로."""
    t = tok.strip().lower()
    if t == 'transparent':
        return t
    if t.startswith('#'):
        h = t[1:]
        if len(h) == 3:
            h = h[0] * 2 + h[1] * 2 + h[2] * 2
        return '#' + h
    return t


def load_table(path):
    if not os.path.isfile(path):
        return None
    try:
        with open(path, encoding='utf-8') as f:
            root = json.load(f)
    except ValueError:
        return None
    if not isinstance(root, dict):
        return None
    return dict((k, v) for k, v in root.items() if not k.startswith('_') and isinstance(v, dict))


def check_target(game_dir, target, table, frac, other):
    """(상태, 설명) — 'ok' | 'missing'(표는 맞는데 자리가 안 부른다) | 'value'(표값이 정본과 다르다·없다) | 'absent'(파일 없음)."""
    file_part, _, key = target.partition('$')
    if not key:
        return 'absent', '자리 표기는 «파일$표키» 여야 한다: ' + target
    if table is None:
        return 'value', '표(ColorMixUi.json)를 못 읽었다'
    if key not in table:
        return 'value', '표에 «%s» 이 없다' % key
    row = table[key]
    tf = row.get('mix_f')
    tw = row.get('with')
    if not isinstance(tf, (int, float)) or isinstance(tf, bool):
        return 'value', '«%s» 의 mix_f 가 수가 아니다: %r' % (key, tf)
    if abs(float(tf) - frac) > 1e-9:
        return 'value', '«%s» 의 mix_f = %s 인데 정본은 %s 다 → 표를 정본에 맞춰라' % (key, tf, frac)
    if norm_color(str(tw)) != norm_color(other):
        return 'value', '«%s» 의 with = %s 인데 정본은 %s 다' % (key, tw, other)
    path = os.path.join(game_dir, file_part)
    if not os.path.isfile(path):
        return 'missing', '파일이 아직 없다(자리부터 가려야 한다): ' + file_part
    with open(path, encoding='utf-8') as f:
        src = f.read()
    if ('"' + key + '"') in src:
        return 'ok', '표 키 "%s" 를 부른다' % key
    return 'missing', '표 키 "%s" 를 안 부른다 — 그 자리는 섞지 않거나 비율이 코드에 박혀 있다(§1)' % key


def run(css_path, game_dir, table_path, table_map, known, out=print, list_pending=False):
    if not os.path.isfile(css_path):
        out('✗ 정본 CSS 를 못 읽었다: %s (git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)' % css_path)
        return 2
    with open(css_path, encoding='utf-8') as f:
        css_text = f.read()
    rules = parse_rules(css_text)
    n_all = len(parse_rules(css_text, keep_keyframes=True))
    table = load_table(table_path)
    problems = 0
    green = 0
    pending = []
    # **캐스케이드(8회차)**: 같은 (선택자, 속성)의 선언이 여럿이면 화면에 서는 것은 **마지막 것**이다
    #   (`background`·`border`·`box-shadow`·사용자 변수 전부 «덮어쓰기» 속성이다 — 앞 선언은 통째로 진다).
    #   앞 선언까지 견주면 «정본이 안 그리는 겹» 을 표가 쥐게 되고, 배선하는 사람이 그것을 보고 겹을 하나 더 그린다.
    last = {}
    for r in rules:
        last[(r[1], r[2])] = r
    checked = set()
    for line, sel, prop, _a, frac, other in rules:
        targets = table_map.get((sel, prop))
        if targets is None:
            pending.append((line, sel, prop, frac, other))
            continue
        if (sel, prop) in checked:
            continue
        checked.add((sel, prop))
        line, _s, _p, _a, frac, other = last[(sel, prop)]   # 뒤 규칙이 이긴다
        target = targets[0]
        state, why = check_target(game_dir, target, table, frac, other)
        if state == 'ok':
            green += 1
            continue
        # **KNOWN 은 «배선 전» 만 덮는다(8회차)** — 표값이 정본과 다른 것까지 덮으면 틀린 값이 KNOWN 뒤에 숨고,
        #   그 lock 이 풀려 배선하는 사람이 **틀린 값을 그대로 그린다**. 같은 가름을 `check_border_radius` 가 18회차에 냈다.
        if state == 'missing' and target in known:
            out('· KNOWN(배선 전)  %s  ← style.css %s %s { %s: … %d%% , %s }  — %s'
                % (target, line, sel, prop, round(frac * 100), other, known[target]))
            continue
        out('✗ %s  %s  ← style.css %s %s { %s } — %s' % (state, target, line, sel, prop, why))
        problems += 1
    if list_pending:
        for line, sel, prop, frac, other in pending:
            out('· 미정  style.css %5d  %-46s %-12s %d%% , %s' % (line, sel[:46], prop[:12], round(frac * 100), other))
    mark = '✓' if problems == 0 else '✗'
    out('%s check_color_mix: 정본 color-mix %d(키프레임 %d 제외) · 표 자리 %d · 자리 초록 %d · KNOWN 빈자리 %d · 미정 %d · 문제 %d'
        % (mark, len(rules), n_all - len(rules), len(table or {}), green, len(known), len(pending), problems))
    return 0 if problems == 0 else 1


def self_test():
    """자기 검사 — 자가 «무엇을 잡는가» 를 칸으로 박아 둔다."""
    import tempfile
    ok = [0, 0]

    def expect(name, got, want):
        ok[1] += 1
        if got == want:
            ok[0] += 1
        else:
            print('  ✗ %s: %r 이 나와야 하는데 %r' % (name, want, got))

    css = """
    .a { background: color-mix(in srgb, var(--rc, #6b3538) 58%, #17181a); }
    .b { box-shadow: 0 0 1px color-mix(in srgb, var(--rc) 72%, transparent); }
    @keyframes k { 0% { box-shadow: 0 0 1px color-mix(in srgb, var(--rc) 40%, transparent); } }
    """
    rules = parse_rules(css)
    expect('키프레임은 안 센다', len(rules), 2)
    expect('비율은 0~1', rules[0][4], 0.58)
    expect('뒤 색', rules[1][5], 'transparent')
    expect('#fff 를 여섯 자리로', norm_color('#fff'), '#ffffff')
    expect('transparent 는 그대로', norm_color(' TRANSPARENT '), 'transparent')

    d = tempfile.mkdtemp()
    os.makedirs(os.path.join(d, 'Ui'))
    with open(os.path.join(d, 'Ui', 'X.cs'), 'w', encoding='utf-8') as f:
        f.write('var c = ColorMixUi.Mix("a_face", rc);')
    tbl = {'a_face': {'mix_f': 0.58, 'with': '#17181a'}, 'b_bad': {'mix_f': 0.5, 'with': '#000'}}
    expect('부르면 초록', check_target(d, 'Ui/X.cs$a_face', tbl, 0.58, '#17181a')[0], 'ok')
    expect('안 부르면 missing', check_target(d, 'Ui/X.cs$b_bad', tbl, 0.5, '#000')[0], 'missing')
    expect('표값이 다르면 value', check_target(d, 'Ui/X.cs$a_face', tbl, 0.6, '#17181a')[0], 'value')
    expect('상대색이 다르면 value', check_target(d, 'Ui/X.cs$a_face', tbl, 0.58, '#000')[0], 'value')
    expect('표에 없으면 value', check_target(d, 'Ui/X.cs$nope', tbl, 0.5, '#000')[0], 'value')
    expect('파일이 없으면 missing', check_target(d, 'Ui/None.cs$a_face', tbl, 0.58, '#17181a')[0], 'missing')
    expect('$ 가 없으면 absent', check_target(d, 'Ui/X.cs', tbl, 0.58, '#17181a')[0], 'absent')
    expect('#fff 와 #ffffff 는 같다', check_target(d, 'Ui/X.cs$a_face', {'a_face': {'mix_f': 0.58, 'with': '#FFF'}}, 0.58, '#ffffff')[0], 'ok')

    # ── 캐스케이드(8회차) — 같은 (선택자, 속성) 선언이 둘이면 **뒤 것**만 견준다 ──────────────
    #   전에는 «나온 순서대로» 자리를 집어, 앞 선언(정본이 안 그리는 겹)에도 표 키를 두어야 초록이었다.
    css2 = """
    .c { box-shadow: 0 0 1px color-mix(in srgb, var(--rc) 55%, transparent); }
    .c { box-shadow: 0 0 1px color-mix(in srgb, var(--rc) 62%, transparent); }
    """
    cd = tempfile.mkdtemp()
    os.makedirs(os.path.join(cd, 'Ui'))
    with open(os.path.join(cd, 'style.css'), 'w', encoding='utf-8') as f:
        f.write(css2)
    with open(os.path.join(cd, 'Ui', 'C.cs'), 'w', encoding='utf-8') as f:
        f.write('var c = ColorMixUi.Mix("c_shadow", rc);')
    tp = os.path.join(cd, 'ColorMixUi.json')

    def cascade(mix_f):
        with open(tp, 'w', encoding='utf-8') as f:
            json.dump({'c_shadow': {'mix_f': mix_f, 'with': 'transparent'}}, f)
        lines = []
        rc = run(os.path.join(cd, 'style.css'), cd, tp,
                 {('.c', 'box-shadow'): ['Ui/C.cs$c_shadow']}, {}, out=lines.append)
        return rc, '\n'.join(lines)

    rc62, out62 = cascade(0.62)
    expect('뒤 선언(62%)이 이긴다 → 초록', rc62, 0)
    expect('선언 둘이어도 자리는 하나로 센다', '자리 초록 1' in out62, True)
    rc55, out55 = cascade(0.55)
    expect('앞 선언(55%)을 쥐면 빨강', rc55, 1)
    expect('정본 값으로 대는 것은 뒤엣것(0.62)이다', '정본은 0.62' in out55, True)
    expect('가리키는 줄도 뒤 선언(3줄)이다', 'style.css 3 ' in out55, True)

    # KNOWN 은 «배선 전» 만 덮는다 — 표값이 틀린 것은 KNOWN 이어도 빨강이다(8회차)
    with open(tp, 'w', encoding='utf-8') as f:
        json.dump({'c_shadow': {'mix_f': 0.55, 'with': 'transparent'}}, f)
    lines = []
    rcK = run(os.path.join(cd, 'style.css'), cd, tp, {('.c', 'box-shadow'): ['Ui/C.cs$c_shadow']},
              {'Ui/C.cs$c_shadow': '임자'}, out=lines.append)
    expect('KNOWN 이어도 틀린 표값은 못 덮는다', rcK, 1)
    with open(os.path.join(cd, 'Ui', 'C.cs'), 'w', encoding='utf-8') as f:
        f.write('// 아직 안 부른다')
    with open(tp, 'w', encoding='utf-8') as f:
        json.dump({'c_shadow': {'mix_f': 0.62, 'with': 'transparent'}}, f)
    lines = []
    rcK2 = run(os.path.join(cd, 'style.css'), cd, tp, {('.c', 'box-shadow'): ['Ui/C.cs$c_shadow']},
               {'Ui/C.cs$c_shadow': '임자'}, out=lines.append)
    expect('KNOWN 은 «배선 전» 을 덮는다', rcK2, 0)
    expect('그때는 KNOWN 줄로 알린다', any('KNOWN(배선 전)' in l for l in lines), True)

    print('%s check_color_mix --self-test %d칸 %s' % ('✓' if ok[0] == ok[1] else '✗', ok[1], '통과' if ok[0] == ok[1] else '실패'))
    return 0 if ok[0] == ok[1] else 1


def main(argv):
    if '--self-test' in argv:
        return self_test()
    root = os.path.dirname(HERE)
    css = os.path.join(root, CSS_DEFAULT)
    game = os.path.join(root, GAME_DEFAULT)
    table = os.path.join(root, TABLE_DEFAULT)
    return run(css, game, table, TABLE, KNOWN, list_pending='--list' in argv)


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
