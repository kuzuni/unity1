#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""T371 — 정본 `color-mix(in srgb, A p%, B)` ↔ 클론 표(`ColorMixUi.json`) ↔ 그 자리의 호출을 세 겹으로 견준다.

정본은 등급색(`--rc`)·가지색(`--bc`)에서 면·테·그림자를 **정해진 비율로 섞어** 만든다(24 선언 · 키프레임 1 제외 23).
클론은 그 비율을 코드에 박거나(§1 위반) 아예 안 섞었다 — 이 자는 ⓐ 표가 정본 값을 그대로 쥐는가 ⓑ 그 자리가 표 키를 부르는가를 본다.

  `Ui/File.cs$키`   그 파일이 `"키"` 를 부른다(표값 = 정본 비율·상대색이어야 한다)
  KNOWN            임자가 정해진 빈자리(남의 산 lock 뒤) — 배선되면 여기서 지운다

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

# 정본 선택자+속성 → 클론 자리(파일$표키). 한 선택자에 선언이 둘이면(면·테) 순서대로 적는다.
TABLE = {
    ('.equip-cell', 'background'): ['Ui/ForgeUi.cs$cell_face'],
    ('.equip-cell', 'border'): ['Ui/ForgeUi.cs$cell_line'],
    ('.anvil-btn.held-slot', 'background'): ['Ui/ForgeSheet.cs$held_face'],
    ('.anvil-btn.held-slot', 'border'): ['Ui/ForgeSheet.cs$held_line'],
    ('.anvil-btn.held-slot.deck', '--dedge'): ['Ui/ForgeSheet.cs$held_deck_edge'],
    ('.anvil-btn.held-slot.deck', '--dgap'): ['Ui/ForgeSheet.cs$held_deck_gap'],
    ('.anvil-btn.held-slot.deck::before', 'background'): ['Ui/ForgeSheet.cs$held_deck_before'],
    ('.auto-drop-card', 'background'): ['Ui/ForgeAutoDrop.cs$drop_card_face'],
    ('.auto-drop-card', 'border'): ['Ui/ForgeAutoDrop.cs$drop_card_line'],
    ('.craft-batch .cb-card', 'background'): ['Ui/ForgeCraftPopup.cs$batch_card_face'],
    ('.craft-batch .cb-card', 'border'): ['Ui/ForgeCraftPopup.cs$batch_card_line'],
    ('.cmp-img', 'background'): ['Ui/ForgeCraftPopup.cs$cmp_img_face'],
    ('.tech-branch-icon::before', 'background'): ['Ui/TechPanel.cs$tech_branch_face'],
    ('.tech-branch-icon::before', 'border'): ['Ui/TechPanel.cs$tech_branch_line'],
    ('#forge-item-modal .idet-icon', 'background'): ['Ui/ForgeInfoPopup.cs$idet_icon_face'],
    ('#forge-item-modal .idet-icon', 'border-color'): ['Ui/ForgeInfoPopup.cs$idet_icon_line'],
    ('.pet-tile .tile-face', 'background'): ['Ui/PetPanel.cs$pet_tile_face'],
    ('.pet-tile .tile-face', 'box-shadow'): ['Ui/PetPanel.cs$pet_tile_shadow', 'Ui/PetPanel.cs$pet_tile_shadow_2'],
    ('.petd-wrap .petd-tile', '--petd-face'): ['Ui/PetDetailPopup.cs$petd_face'],
    ('.petd-wrap .petd-tile', 'border'): ['Ui/PetDetailPopup.cs$petd_line'],
    ('.equip-cell:not(.egg-cell)', 'box-shadow'): ['Ui/ForgeUi.cs$cell_shadow_1', 'Ui/ForgeUi.cs$cell_shadow_2'],
}

# 임자가 정해진 빈자리(자리 → 이유) — 배선될 때마다 지운다. 1회차는 **배선이 0 이라 전부 여기 있다**.
KNOWN = {
    'Ui/ForgeUi.cs$cell_face': 'T371 1회차 — 표·셈만 세웠다(배선 전). `ForgeUi.cs` 는 T332 산 lock 뒤',
    'Ui/ForgeUi.cs$cell_line': 'T371 1회차 — 같은 파일(T332 lock)',
    'Ui/ForgeUi.cs$cell_shadow_1': 'T371 1회차 — 그림자 두 겹은 T331(box-shadow 축) 과 겹치는 자리다 — 그 lock 뒤',
    'Ui/ForgeUi.cs$cell_shadow_2': 'T371 1회차 — 같은 자리(뒤 규칙 62%)',
    'Ui/ForgeSheet.cs$held_face': 'T371 1회차 — 배선 전(값은 이미 맞고 비율이 코드에 박혀 있다)',
    'Ui/ForgeSheet.cs$held_line': 'T371 1회차 — 배선 전',
    'Ui/ForgeSheet.cs$held_deck_edge': 'T371 1회차 — 배선 전(클론 바탕색 #DEE3ED 가 정본 #dfe4ec 와 채널마다 1 다르다)',
    'Ui/ForgeSheet.cs$held_deck_gap': 'T371 1회차 — 클론에 그 자리(장 사이 틈 색)가 아직 없다',
    'Ui/ForgeSheet.cs$held_deck_before': 'T371 1회차 — 배선 전',
    'Ui/ForgeAutoDrop.cs$drop_card_face': 'T371 1회차 — 클론 파일 이름이 다를 수 있다(자동 제련 낙하 카드) · 2회차에 자리부터 가린다',
    'Ui/ForgeAutoDrop.cs$drop_card_line': 'T371 1회차 — 같은 자리',
    'Ui/ForgeCraftPopup.cs$batch_card_face': 'T371 1회차 — 배선 전',
    'Ui/ForgeCraftPopup.cs$batch_card_line': 'T371 1회차 — 배선 전',
    'Ui/ForgeCraftPopup.cs$cmp_img_face': 'T371 1회차 — 배선 전',
    'Ui/TechPanel.cs$tech_branch_face': 'T371 1회차 — **아예 안 섞는 자리**(등급색 원색). T178 9회차가 겹 둘을 깐 그 원판이다',
    'Ui/TechPanel.cs$tech_branch_line': 'T371 1회차 — 같은 원판의 테(클론은 공용 pp_line)',
    'Ui/ForgeInfoPopup.cs$idet_icon_face': 'T371 1회차 — `ForgeInfoPopup.cs` 는 T28·T332 산 lock 뒤',
    'Ui/ForgeInfoPopup.cs$idet_icon_line': 'T371 1회차 — 같은 파일',
    'Ui/PetPanel.cs$pet_tile_face': 'T371 1회차 — 클론은 이미 표(`PetSkillUi.json` `tile_face_mix_f`)로 읽는다 — 2회차에 **표 하나로 합칠지**(키 옮김) 정한다',
    'Ui/PetPanel.cs$pet_tile_shadow': 'T371 1회차 — 그림자(알파를 만드는 섞기) · T331 축과 겹친다',
    'Ui/PetPanel.cs$pet_tile_shadow_2': 'T371 1회차 — 같은 자리(뒤 규칙)',
    'Ui/PetDetailPopup.cs$petd_face': 'T371 1회차 — 배선 전(정본은 이 값을 다음 줄 테에 다시 쓴다 · 사슬)',
    'Ui/PetDetailPopup.cs$petd_line': 'T371 1회차 — 앞 색이 --rc 가 아니라 바로 위 --petd-face 다 — 셈을 잇는 자리',
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
    seen = {}
    for line, sel, prop, _a, frac, other in rules:
        targets = table_map.get((sel, prop))
        if targets is None:
            pending.append((line, sel, prop, frac, other))
            continue
        # 같은 (선택자, 속성)에 선언이 여럿이면(뒤 규칙이 앞을 덮는 자리) **나온 순서대로** 자리를 집는다.
        i = seen.get((sel, prop), 0)
        seen[(sel, prop)] = i + 1
        target = targets[min(i, len(targets) - 1)]
        state, why = check_target(game_dir, target, table, frac, other)
        if state == 'ok':
            green += 1
            continue
        if target in known:
            out('· KNOWN(%s)  %s  ← style.css %s %s { %s: … %d%% , %s }  — %s'
                % ('배선 전' if state == 'missing' else state, target, line, sel, prop, round(frac * 100), other, known[target]))
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
