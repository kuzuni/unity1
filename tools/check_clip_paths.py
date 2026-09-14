#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T159 — 정본 도형(`clip-path: polygon(...)`) 규칙 ↔ 클론 «구운 다각형» 대조 자.

정본 `web/css/style.css` 에서 `clip-path` 로 **모양을 깎는** 규칙(선택자 · 다각형)을 전부 걷고,
클론 `Assets/Scripts/Game/**/*.cs` 의 그 자리가 **다각형을 구워** 그리는지 본다.

왜 이 자가 있나(T159 실측): 클론은 uGUI 라 «모양» 이 저절로 안 온다 — `UiKit.Panel`·`UiKit.Rounded` 는
네모/둥근 네모 한 장이다. 그래서 V 홈·꼭짓점·제비꼬리가 **조용히 사라진다**. T156(`.cmp-ribbon`)은 그 한
자리를 PNG 8배로 눈으로 잡은 것이고, 같은 병의 나머지는 아무 자도 안 봤다(`check_keyline`(T109)은
`-webkit-text-stroke` 만 본다). 이 자는 «모양» 쪽 빈자리를 센다.

무엇을 «도형이 있다» 로 세나: 그 자리가 **굽는 길**(T87 6회차 `CraftFxPoly` · T102 `PetHatchCone` ·
제 손으로 굽는 `Sprite.Create`)을 지나가는가. 결정 223 — 이 레포에서 맨 `Graphic` 은 한 픽셀도 안 칠해지므로
모양은 전부 구운 스프라이트로 온다. 그래서 «굽는 호출이 그 자리에 닿는가» 가 곧 «모양이 왔는가» 다.

선택자 ↔ 클론 자리 짝은 이 파일의 TABLE 하나가 쥔다(T109 `check_keyline` 의 꼴 그대로).
  "Ui/File.cs"           파일 안 어디든 굽는 호출이 하나 있으면 됨
  "Ui/File.cs#name"      그 이름으로 만든 오브젝트가 구운 도형이거나, 그 변수에 굽는 호출이 닿아야 함
  "Ui/File.cs@Method"    도우미 메서드 본문 안에 굽는 호출이 있어야 함
  "—<이유>"              정본이 모양을 끄는 규칙(`none`)이거나 클론에 그 자리가 없다 — 대조하지 않는다

빈자리 중 «임자가 정해진 것» 은 KNOWN 에 두어 rc 0 으로 지나간다(T89·T109 의 KNOWN 규약).
T159 ⓑ 가 한 자리를 닫을 때마다 KNOWN 에서 그 줄을 지운다 — 닫았는데 안 지우면 «이제 있다» 로 알린다(rc 는 그대로).

사용:  python3 tools/check_clip_paths.py [--css <style.css 경로>] [--game <Assets/Scripts/Game>] [--self-test]
rc:    0 = 정본 규칙 전부가 표에 있고 · 표의 자리 전부가 (도형이 있거나 KNOWN) · 1 = 아니다 · 2 = 정본 CSS 를 못 읽었다
"""
import os
import re
import sys
import tempfile

CSS_DEFAULT = os.path.join('.wwwww-src', 'web', 'css', 'style.css')
GAME_DEFAULT = os.path.join('Assets', 'Scripts', 'Game')

# ── 정본 선택자 ↔ 클론 자리 ──────────────────────────────────────────────────────────────
# 선택자는 style.css 의 것을 공백 하나로 정규화한 그대로. 순서는 style.css 등장 순.
TABLE = {
    '.cmp-ribbon': ['Ui/ForgeUi.cs@Ribbon'],
    # 리본 꼬리는 «바깥 변에서 절반 깊이로 파고드는 V 홈» 이다(정본 2502 주석) — 안쪽 변은 본체에 붙는 직선.
    '.league-reward-banner::before': ['Ui/LeagueSheet.cs#tail-l'],
    '.league-reward-banner::after': ['Ui/LeagueSheet.cs#tail-r'],
    '.pass-banner::before': ['Ui/PassPopup.cs#tail-l'],
    '.pass-banner::after': ['Ui/PassPopup.cs#tail-r'],
    # 가격 페넌트는 두 층(검정 바깥 · 3px 안쪽 주황)이 **같은 clip** 을 쓴다 — 자리는 하나다.
    '.pass-price': ['Ui/PassPopup.cs#price'],
    '.pass-price::before': ['Ui/PassPopup.cs#price'],
    # 패스 칸 아래에 붙는 꼬리 삼각형 넷(무료·프리미엄 × 검정 층·면 층) — 클론의 칸을 세우는 곳은 Cell( 하나다.
    '.pass-cell.free::before': ['Ui/PassPopup.cs@Cell'],
    '.pass-cell.free::after': ['Ui/PassPopup.cs@Cell'],
    '.pass-cell.premium::before': ['Ui/PassPopup.cs@Cell'],
    '.pass-cell.premium::after': ['Ui/PassPopup.cs@Cell'],
    '.shop-deal-tag': ['Ui/ShopSheet.cs#tag'],
    '.equipped-label': ['Ui/SkillPanel.cs@EquippedLabel'],
    '.hatch-cone': ['Ui/PetPanel.cs#hatch-cone'],
    '.rw-pop': ['Ui/RewardBurst.cs@StarSprite'],
}

# ── 임자가 정해진 빈자리(자리 → 이유) — T159 ⓑ 가 닫을 때마다 지운다 ──────────────────────────
KNOWN = {
    'Ui/ForgeUi.cs@Ribbon': 'T156 — .cmp-ribbon 의 «<» 노치(그 절이 임자 · ForgeUi.cs 는 T122 lock 뒤)',
    'Ui/PassPopup.cs#price': 'T159 ⓑ — .pass-price 페넌트(아래 꼭짓점 · 두 층이 같은 clip)',
    'Ui/PassPopup.cs@Cell': 'T159 ⓑ — .pass-cell 꼬리 삼각형 넷(칸 아래 · 검정 층 + 면 층)',
    'Ui/ShopSheet.cs#tag': 'T159 ⓑ — .shop-deal-tag 제비꼬리 홈',
}

# 굽는 길(결정 223 · T87 6회차 CraftFxPoly · T102 PetHatchCone · 제 손으로 굽는 Sprite.Create)
BAKE = r'CraftFxPoly\.Bake\w*|PetHatchCone\.Add|ClipShape\.\w+|Sprite\.Create'
BAKE_CALL = re.compile(r'\b(?:' + BAKE + r')\s*\(')
# 같은 길인데 «그 변수에 닿는가» 를 보는 꼴 — 첫 인자가 그 자리다.
def bake_on(var):
    return re.compile(r'\b(?:' + BAKE + r')\s*\(\s*' + re.escape(var) + r'\s*[,)]')

# 도형을 **제 손으로** 만드는 공장(이 호출 자체가 곧 모양이다) — `PetHatchCone.Add(cell, "hatch-cone", …)`
SHAPE_FACTORY = re.compile(r'^(?:' + BAKE + r')$')
CREATE_CALL = re.compile(r'([\w.]+)\s*\(\s*[^,()]+,\s*"([^"]+)"')
ASSIGN_TAIL = re.compile(r'([\w\[\]\.]+)\s*=\s*(?:[\w!.()\[\]]+\s*\?\s*)?[\w.]*$')  # «tl = UiKit» 꼬리
CLIP_DECL = re.compile(r'(?<![\w-])clip-path\s*:\s*([^;]+)')
VAR_DECL = re.compile(r'(--[\w-]+)\s*:\s*(polygon\([^;]*\))\s*;')
VAR_USE = re.compile(r'var\(\s*(--[\w-]+)\s*\)\s*$')


# ── 정본 CSS ──────────────────────────────────────────────────────────────────────────────
def _blank_comments(css):
    """주석을 같은 길이의 공백/줄바꿈으로 바꿔 줄 번호를 지킨다."""
    def rep(m):
        return ''.join('\n' if ch == '\n' else ' ' for ch in m.group(0))
    return re.sub(r'/\*.*?\*/', rep, css, flags=re.S)


def parse_rules(css_text):
    """[(줄, 선택자, 다각형)] — clip-path 로 모양을 깎는 규칙 전부.

    `clip-path: var(--pen)` 은 그 이름의 `polygon(...)` 선언으로 편다(이름은 정본에서 유일하므로 한 표로 충분하다).
    `none` 처럼 다각형이 아닌 값은 «끄는 규칙» 으로 남긴다(TABLE 이 '—' 로 받는다).
    """
    css = _blank_comments(css_text)
    varmap = dict((m.group(1), m.group(2)) for m in VAR_DECL.finditer(css))
    out = []
    for m in re.finditer(r'([^{}]+)\{([^{}]*)\}', css):
        d = CLIP_DECL.search(m.group(2))
        if not d:
            continue
        val = ' '.join(d.group(1).split())
        u = VAR_USE.match(val)
        if u:
            val = varmap.get(u.group(1), val)
        sel = ' '.join(m.group(1).split())
        lead = len(m.group(1)) - len(m.group(1).lstrip())
        line = css[:m.start(1) + lead].count('\n') + 1
        out.append((line, sel, val))
    return out


# ── 클론 ──────────────────────────────────────────────────────────────────────────────────
def _read(path):
    with open(path, encoding='utf-8') as f:
        return f.read()


def _method_body(src, name):
    """`name(` 의 **정의** 본문 — 호출부(`x = name(`)는 앞 글자가 `=`·`.` 라 안 걸린다."""
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
    """(상태, 설명) — 'ok' | 'missing'(자리는 있는데 네모 한 장) | 'absent'(자리·메서드·파일 없음)."""
    file_part, sep, tail = re.match(r'([^#@]+)([#@]?)(.*)', target).groups()
    path = os.path.join(game_dir, file_part)
    if not os.path.isfile(path):
        return 'absent', '파일 없음 ' + file_part
    src = _read(path)
    if sep == '':
        return ('ok' if BAKE_CALL.search(src) else 'missing'), '파일 전체'
    if sep == '@':
        body = _method_body(src, tail)
        if body is None:
            return 'absent', '메서드 없음 ' + tail + '('
        return ('ok' if BAKE_CALL.search(body) else 'missing'), '메서드 ' + tail + '( 본문'
    found = False
    for m in CREATE_CALL.finditer(src):
        if m.group(2) != tail:
            continue
        found = True
        if SHAPE_FACTORY.match(m.group(1)):
            return 'ok', '"%s" 를 %s 로 굽는다' % (tail, m.group(1))
        head = src[max(0, m.start() - 160):m.start()].replace('\n', ' ')
        a = ASSIGN_TAIL.search(head)
        if a and bake_on(a.group(1)).search(src):
            return 'ok', '"%s" → %s 에 굽는 호출이 닿는다' % (tail, a.group(1))
    if not found:
        return 'absent', '"%s" 이름으로 만드는 자리가 없다' % tail
    return 'missing', '"%s" 는 굽는 호출이 안 닿는다 — 네모/둥근 네모 한 장이다' % tail


# ── 대조 ──────────────────────────────────────────────────────────────────────────────────
def run(css_path, game_dir, table, known, out=print):
    if not os.path.isfile(css_path):
        out('✗ 정본 CSS 를 못 읽었다: %s (git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)' % css_path)
        return 2
    rules = parse_rules(_read(css_path))
    problems = 0
    seen = set()
    n_off = n_ok = n_known = 0
    known_now_ok = []
    for line, sel, poly in rules:
        seen.add(sel)
        if sel not in table:
            problems += 1
            out('✗ 표에 없는 정본 도형  style.css %d  %s  { %s }  → TABLE 에 짝을 더해라' % (line, sel, poly))
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
            tag = '도형 없음' if state == 'missing' else '자리 없음'
            if t in known:
                n_known += 1
                out('· KNOWN(%s)  %s  ← style.css %d %s { %s }  — %s' % (tag, t, line, sel, poly, known[t]))
            else:
                problems += 1
                out('✗ %s  %s  ← style.css %d  %s  { %s }  — %s' % (tag, t, line, sel, poly, why))
    for sel in table:
        if sel not in seen:
            problems += 1
            out('✗ 표에는 있는데 정본에 없는 선택자: %s  → 정본이 바뀌었다 · TABLE 에서 지우거나 고쳐라' % sel)
    for t in sorted(set(known_now_ok)):
        out('· KNOWN 인데 이제 도형이 있다: %s  → KNOWN 에서 지워라' % t)
    out('%s check_clip_paths: 정본 도형 %d(끄는 규칙 %d) · 자리 초록 %d · KNOWN 빈자리 %d · 문제 %d'
        % ('✓' if problems == 0 else '✗', len(rules), n_off, n_ok, n_known, problems))
    return 0 if problems == 0 else 1


# ── 자기 검사(고장 주입) ────────────────────────────────────────────────────────────────────
def self_test():
    css = """
/* 주석 { } */
.a-tail { --pen: polygon(0 0, 100% 0, 100% 82%, 50% 100%, 0 82%); clip-path: var(--pen); }
.a-tail::before { clip-path: var(--pen); }
.b-plain { clip-path: polygon(0 0, 100% 0, 50% 100%); }
.c-off { clip-path: none; }
.d-factory { clip-path: polygon(38% 0, 62% 0, 100% 100%, 0 100%); }
.e-method { clip-path: polygon(50% 0, 100% 100%, 0 100%); }
.f-file { clip-path: polygon(0 0, 100% 0, 88% 50%, 100% 100%, 0 100%); }
"""
    cs = """
namespace X {
    class Sheet {
        void Build(Transform p) {
            RectTransform tl = UiKit.Box(p, "tail-l");
            ClipShape.Poly(tl, "bg", "pen_tail");
            RectTransform pl = UiKit.Box(p, "plain");
            UiKit.Panel(pl, "bg", "pp_red");
            PetHatchCone cone = PetHatchCone.Add(p, "hatch-cone", a, b, 1f, 0f);
        }
        void Cell(Transform p) {
            RectTransform c = UiKit.Box(p, "cell");
            CraftFxPoly.Bake("cell-tail", pts);
        }
        void Bare(Transform p) {
            RectTransform c = UiKit.Box(p, "bare-cell");
            UiKit.Rounded(c, "face", "pp_paper", 2f);
        }
    }
}
"""
    fails = []

    def expect(name, table, known, want, css_text=css, cs_text=cs):
        with tempfile.TemporaryDirectory() as d:
            os.makedirs(os.path.join(d, 'Ui'))
            with open(os.path.join(d, 'style.css'), 'w', encoding='utf-8') as f:
                f.write(css_text)
            with open(os.path.join(d, 'Ui', 'Sheet.cs'), 'w', encoding='utf-8') as f:
                f.write(cs_text)
            lines = []
            rc = run(os.path.join(d, 'style.css'), d, table, known, out=lines.append)
            if rc != want:
                fails.append('%s: rc %d ≠ %d\n  ' % (name, rc, want) + '\n  '.join(lines))
            return lines

    base = {
        '.a-tail': ['Ui/Sheet.cs#tail-l'], '.a-tail::before': ['Ui/Sheet.cs#tail-l'],
        '.b-plain': ['Ui/Sheet.cs#plain'], '.c-off': '—정본이 모양을 끄는 규칙',
        '.d-factory': ['Ui/Sheet.cs#hatch-cone'], '.e-method': ['Ui/Sheet.cs@Cell'],
        '.f-file': ['Ui/Sheet.cs'],
    }
    # 1 다 있으면 0 (네모 한 장인 .b-plain 은 KNOWN)
    expect('전부 초록', base, {'Ui/Sheet.cs#plain': '임자'}, 0)
    # 2 KNOWN 을 지우면 «도형 없음» → 1
    lines = expect('도형 없음 → 1', base, {}, 1)
    if not any('네모/둥근 네모 한 장' in l for l in lines):
        fails.append('«네모 한 장» 이유가 안 나온다')
    # 3 정본에 규칙이 더 있는데 표에 없다 → 1
    expect('표에 없는 도형 → 1', base, {'Ui/Sheet.cs#plain': '임자'}, 1,
           css_text=css + '.z-new { clip-path: polygon(0 0, 100% 0, 50% 100%); }\n')
    # 4 표에는 있는데 정본에 없다 → 1
    t = dict(base); t['.gone'] = ['Ui/Sheet.cs']
    expect('정본에 없는 선택자 → 1', t, {'Ui/Sheet.cs#plain': '임자'}, 1)
    # 5 자리 자체가 없다 → 1 · KNOWN 이면 0
    t = dict(base); t['.b-plain'] = ['Ui/Sheet.cs#nowhere']
    expect('자리 없음 → 1', t, {}, 1)
    expect('자리 없음 KNOWN → 0', t, {'Ui/Sheet.cs#nowhere': '임자'}, 0)
    # 6 메서드 본문에 굽는 호출이 없다 → 1
    t = dict(base); t['.e-method'] = ['Ui/Sheet.cs@Bare']
    expect('메서드 민네모 → 1', t, {'Ui/Sheet.cs#plain': '임자'}, 1)
    # 6b 호출부를 정의로 잘못 보지 않는다
    if _method_body('class C { void Go() { var r = Cell(null); } }', 'Cell') is not None:
        fails.append('호출부를 정의로 잘못 본다')
    # 7 고장 주입: 꼬리의 굽는 호출을 지우면 1
    broken = cs.replace('ClipShape.Poly(tl, "bg", "pen_tail");', 'UiKit.Panel(tl, "bg", "pp_red");')
    expect('굽는 호출 지움 → 1', base, {'Ui/Sheet.cs#plain': '임자'}, 1, cs_text=broken)
    # 8 공장 호출(PetHatchCone.Add(…, "hatch-cone", …))은 그 자체가 도형이다
    lines = expect('공장 호출 → 0', {'.d-factory': ['Ui/Sheet.cs#hatch-cone'], '.a-tail': '—', '.a-tail::before': '—',
                                   '.b-plain': '—', '.c-off': '—', '.e-method': '—', '.f-file': '—'}, {}, 0)
    # 9 KNOWN 인데 이제 있다 → 알리기만(rc 0)
    lines = expect('KNOWN 해소 알림', base, {'Ui/Sheet.cs#plain': '임자', 'Ui/Sheet.cs#tail-l': '옛 임자'}, 0)
    if not any('KNOWN 인데 이제 도형이 있다' in l for l in lines):
        fails.append('KNOWN 해소 알림이 안 나온다')
    # 10 파서: var(--pen) 을 펴고 · 주석 속 중괄호를 무시하고 · 줄 번호를 지킨다
    rules = parse_rules(css)
    if [r[1] for r in rules] != ['.a-tail', '.a-tail::before', '.b-plain', '.c-off', '.d-factory', '.e-method', '.f-file']:
        fails.append('파서 선택자: %r' % [r[1] for r in rules])
    if rules[0][0] != 3 or not rules[1][2].startswith('polygon(0 0, 100% 0, 100% 82%'):
        fails.append('파서 var/줄번호: %r' % (rules[:2],))
    if rules[3][2] != 'none':
        fails.append('끄는 규칙을 못 남긴다: %r' % (rules[3],))
    # 11 정본 CSS 가 없으면 2
    if run('/nonexistent/style.css', '.', base, {}, out=lambda s: None) != 2:
        fails.append('정본 없음 rc 2')
    if fails:
        print('✗ check_clip_paths --self-test 실패 %d' % len(fails))
        for f in fails:
            print('  - ' + f)
        return 1
    print('✓ check_clip_paths --self-test 13칸 통과')
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
        print('사용: check_clip_paths.py [--css <style.css>] [--game <Assets/Scripts/Game>] [--self-test]')
        return 2
    return run(css, game, TABLE, KNOWN)


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
