#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""T331 — 정본 `box-shadow` 의 **바깥 그림자**가 클론에 서 있는가.

무엇이 아팠나
-------------
정본 `style.css` 의 `box-shadow` 선언은 **168개**다(주석을 지우고 센 수 — 등재 글의 168과 같다).
갈래로 가르면 `none` 40 · **안쪽(`inset`)만 67** · **테두리(`0 0 0 1px`)만 3** · **이 축이 보는 것 58** 이다.
그 바깥 그림자가 이 UI 의 «떠 있음» 을 통째로 만든다 — 카드의 딱딱한 아래턱(`.modal-card
0 .5rem 0`), 패널의 위턱(`.panel 0 -.4rem 0`), 상단바·자동 제련 카드의 흐린 그늘, 스킬 버튼의 발광.
**클론에는 그 갈래가 0이다**(흐림을 굽는 도우미 자체가 없다) — 그래서 모든 화면이 정본보다 «납작하게»
읽힌다. 안쪽 띠(`inset 0 -Nrem 0`)는 이미 `PopupKit.BottomShade`·`btn_lip` 관용구로 서 있다(T163 계열).

이 자가 하는 일
--------------
ⓐ 정본 CSS 를 읽어 **바깥 그림자를 쓰는 선택자**를 전수한다(주석은 지우고 센다 — 주석 안의
   `box-shadow: none` 같은 글이 수를 흐린다).
ⓑ 아래 `SPOTS` 표의 자리마다 **클론 코드에 그 그림자를 실제로 거는 호출이 있는가** 를 본다.
   호출은 `UiShadow.Drop(<무엇>, "<키>")` 한 꼴이다 — 주석이 아니라 **코드**를 센다(결정 505).
ⓒ 표에도 `KNOWN` 에도 없는 새 정본 자리가 생기면 알린다(정본이 자라면 이 축이 조용히 낡는다).

빨강이 되는 때
-------------
· 표의 자리가 «섰다» 인데 그 호출이 클론에 없다(되돌아간 자리).
· `KNOWN` 에 적힌 자리가 실제로는 서 있다(적어 두고 안 지운 자리).
지금은 클론에 도우미가 없어 표의 자리가 전부 `KNOWN` 이다 — **그 목록이 곧 할 일 목록**이다.
"""
import os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CSS = os.path.join(ROOT, '.wwwww-src', 'web', 'css', 'style.css')
GAME = os.path.join(ROOT, 'Assets', 'Scripts', 'Game')

# 클론이 그림자를 거는 유일한 길(2회차가 세운다). 주석이 아니라 이 호출을 센다.
CALL = re.compile(r'UiShadow\.Drop\s*\(\s*[^,]+,\s*"([^"]+)"')

# 정본 자리 ↔ 클론 키. T33 17회차가 전수해 등재한 열둘(딱딱한 아래턱 다섯 · 흐린 일곱).
SPOTS = [
    # (키, 정본 선택자, 정본 줄, 꼴)
    ('card_lip',      '.modal-card',                   3518, 'hard'),
    ('panel_lip',     '.panel',                        3572, 'hard'),
    ('qstrow_lip',    '.qst-row',                      2026, 'hard'),
    ('dgbanner_lip',  '.modal-card.sheet .dg-banner',  3875, 'hard'),
    ('equipped_lip',  '.equipped-row',                 4128, 'hard'),
    ('topbar_drop',   '#topbar',                       7905, 'blur'),
    ('afcard_drop',   '.af-card',                      5030, 'blur'),
    ('autodrop_drop', '.auto-drop-card',               1073, 'blur'),
    ('cbcard_drop',   '.craft-batch .cb-card',         1140, 'blur'),
    ('passcard_drop', '.pass-card',                    None, 'blur'),
    ('leaguefoot_up', '.league-foot',                  2376, 'blur'),
    ('eqswfly_drop',  '.eqsw-fly-box',                 None, 'blur'),
]

# 아직 안 선 자리 — 까닭과 «누가/언제» 를 같이 적는다. 서면 이 줄을 지운다(안 지우면 자가 알린다).
KNOWN = {
    'panel_lip':     'T331 2회차 — 같음(UiKit.cs 는 T178·T335 산 lock)',
    'passcard_drop': 'T331 4회차 — 굽는 길은 섰다 · PassPopup.cs 가 T352 산 lock',
}


def strip_comments(text):
    """주석을 **같은 길이의 공백**으로 지운다 — 줄 번호가 안 밀린다."""
    return re.sub(r'/\*.*?\*/', lambda m: re.sub(r'[^\n]', ' ', m.group(0)), text, flags=re.S)


def parts_of(value):
    """`0 .5rem 0 rgba(...), inset 0 -.2rem 0 #000` → 쉼표로 자른 조각(괄호 안 쉼표는 안 자른다)."""
    out, cur, d = [], '', 0
    for ch in value:
        if ch == '(': d += 1
        elif ch == ')': d -= 1
        if ch == ',' and d == 0:
            out.append(cur.strip()); cur = ''
        else:
            cur += ch
    if cur.strip(): out.append(cur.strip())
    return out


LEN = re.compile(r'^(?:-?[\d.]+(?:rem|px|em)?|calc\([^)]*\)|0)$')


def geom_of(part):
    """조각에서 «x y 흐림 퍼짐» 네 자리를 뽑는다(색·`inset` 은 건너뛴다). 못 읽으면 None."""
    toks, cur, d = [], '', 0
    for ch in part.strip():
        if ch == '(': d += 1
        elif ch == ')': d -= 1
        if ch.isspace() and d == 0:
            if cur: toks.append(cur); cur = ''
        else:
            cur += ch
    if cur: toks.append(cur)
    nums = [t for t in toks if LEN.match(t)]
    if len(nums) < 2: return None
    while len(nums) < 4: nums.append('0')
    return nums[:4]


def zero(tok):
    return tok == '0' or bool(re.match(r'^-?0(?:rem|px|em)$', tok or ''))


def kind_of(part):
    """조각 하나의 갈래.

    · `inset` — 안쪽(앞이든 **뒤든**). 이 축이 안 본다(`PopupKit.BottomShade`·`btn_lip` 이 이미 쥔다).
    · `ring`  — x·y·흐림 0 인데 **퍼짐만** 있다(`0 0 0 1px …`). 그건 그림자가 아니라 **테두리**고
                이 레포에선 **T109 `check_keyline`** 축이 이미 센다 — 여기서 또 세면 두 번 센다.
    · `glow`  — x·y 0 인데 흐림이 있다(`0 0 8px …`). 뒤로 지는 그늘이 아니라 **둘레가 빛나는 것**이다.
    · `hard`  — 치우침이 있고 흐림 0(정본의 «딱딱한 턱» · `.modal-card 0 .5rem 0`).
    · `drop`  — 치우침도 흐림도 있다(보통의 드리운 그림자).
    """
    p = part.strip()
    if p.startswith('inset') or p.endswith('inset'): return 'inset'
    g = geom_of(p)
    if g is None: return 'hard'
    x, y, blur, spread = g
    off = not (zero(x) and zero(y))
    if not off and zero(blur):
        return 'ring' if not zero(spread) else 'hard'
    if not off: return 'glow'
    return 'hard' if zero(blur) else 'drop'


# 키프레임 선택자(`0%`·`from`·`to`…)는 **자리가 아니다** — 그 애니메이션이 걸리는 요소가 따로 있다.
KEYFRAME_SEL = re.compile(r'^\s*(?:from|to|[\d.]+%)(?:\s*,\s*(?:from|to|[\d.]+%))*\s*$')


def outer_decls(css_path=CSS):
    """정본에서 «바깥 그림자를 쓰는» 선언을 (줄, 선택자, 값, 갈래들) 로 뽑는다."""
    if not os.path.exists(css_path): return None
    lines = strip_comments(open(css_path, encoding='utf-8').read()).split('\n')
    out = []
    for i, l in enumerate(lines):
        m = re.search(r'box-shadow\s*:\s*([^;]*)', l)
        if not m: continue
        val = m.group(1).strip()
        if not val or val.split()[0] == 'none': continue
        ps = parts_of(val)
        kinds = [kind_of(p) for p in ps]
        # 안쪽(inset)과 테두리(ring)만인 선언은 **이 축이 아니다** — 앞은 `PopupKit.BottomShade`,
        # 뒤는 T109 `check_keyline` 이 이미 센다. 둘 다 세면 한 자리를 두 자가 판정한다.
        if all(k in ('inset', 'ring') for k in kinds): continue
        j = i
        while j >= 0 and '{' not in lines[j]: j -= 1
        sel = lines[j].split('{')[0].strip() if j >= 0 else '?'
        out.append((i + 1, sel, val, kinds))
    return out


def clone_keys(game_dir=GAME):
    """클론 코드가 실제로 거는 그림자 키(주석 아님 · 호출만)."""
    keys = {}
    for base, _, files in os.walk(game_dir):
        for fn in files:
            if not fn.endswith('.cs'): continue
            path = os.path.join(base, fn)
            for n, line in enumerate(open(path, encoding='utf-8', errors='replace'), 1):
                s = line.split('//')[0]
                for k in CALL.findall(s):
                    keys.setdefault(k, []).append('%s:%d' % (os.path.relpath(path, ROOT), n))
    return keys


def run():
    decls = outer_decls()
    if decls is None:
        print('⚠ check_box_shadows: 정본이 옆에 없다(%s) — 알리기만 한다(rc 0).' % os.path.relpath(CSS, ROOT))
        print('  · ROUTINE §0 의 방법으로 `git clone --depth 1 … .wwwww-src` 한 뒤 다시 돌린다.')
        return 0
    live = clone_keys()
    bad, stale, standing = [], [], 0
    for key, sel, ln, kind in SPOTS:
        where = live.get(key)
        if where and key in KNOWN:
            stale.append((key, sel, where[0]))
        elif where:
            standing += 1
        elif key not in KNOWN:
            bad.append((key, sel, ln, kind))

    # 표에도 KNOWN 에도 없는 **새 정본 자리** — 막지는 않고 알린다(정본이 자라면 이 축이 낡는다).
    known_sels = set(s for _, s, _, _ in SPOTS)
    rest = [(n, s, v, k) for n, s, v, k in decls if s not in known_sels]
    frames = [r for r in rest if KEYFRAME_SEL.match(r[1])]
    unseen = [r for r in rest if not KEYFRAME_SEL.match(r[1])]
    by_kind = {}
    for n, s, v, k in unseen:
        main = 'drop' if 'drop' in k else ('hard' if 'hard' in k else 'glow')
        by_kind.setdefault(main, []).append((n, s))

    print('· 정본 바깥 그림자 선언 %d개(전체 box-shadow 중 안쪽만인 것은 뺐다) · 표의 자리 %d개 · 클론에 선 자리 %d개'
          % (len(decls), len(SPOTS), standing))
    if frames:
        print('  · 키프레임 선택자 %d개는 **자리가 아니다**(`0%%`·`from`·`to` — 그 애니메이션이 걸리는 요소가 따로 있다) → 안 센다'
              % len(frames))
    for main in ('hard', 'drop', 'glow'):
        rows = by_kind.get(main)
        if not rows: continue
        label = {'hard': '딱딱한 턱', 'drop': '드리운 그림자', 'glow': '둘레 발광'}[main]
        print('⚠ 표에 없는 %s %d개(알림 — 다음 회차가 표에 담는다): %s%s'
              % (label, len(rows), ', '.join('%s(%d)' % (s, n) for n, s in rows[:5]),
                 ' …' if len(rows) > 5 else ''))
    for key, sel, where in stale:
        print('✗ KNOWN 에 «아직 안 섰다» 로 적힌 %s(%s)가 실제로는 서 있다 — %s · 그 줄을 지워라' % (key, sel, where))
    for key, sel, ln, kind in bad:
        print('✗ %s(%s%s · %s)가 클론에 없다 — `UiShadow.Drop(…, "%s")` 호출이 0이다'
              % (key, sel, (' · style.css %d' % ln) if ln else '', kind, key))
    if bad or stale:
        print('  고치는 법: 그 자리를 세우거나, 아직이면 KNOWN 에 **까닭과 다음 회차**를 적는다(빈 줄은 안 된다).')
        return 1
    print('✓ check_box_shadows: 어긋난 자리 0 · 아직 안 선 자리 %d(KNOWN — 그 목록이 할 일이다)' % len(KNOWN))
    return 0


def self_test():
    ok, fails = 0, []

    def chk(name, cond):
        nonlocal ok
        if cond: ok += 1
        else: fails.append(name)

    # ⓐ 조각 자르기 — 괄호 안 쉼표에 안 속는다
    chk('rgba 안 쉼표로 안 자른다', parts_of('0 .5rem 0 rgba(0,0,0,.25)') == ['0 .5rem 0 rgba(0,0,0,.25)'])
    chk('두 조각을 자른다', len(parts_of('0 .5rem 0 rgba(0,0,0,.25), inset 0 -.2rem 0 #000')) == 2)
    chk('빈 값은 0조각', parts_of('') == [])

    # ⓑ 갈래 가르기
    chk('inset 을 안쪽으로', kind_of('inset 0 -.3rem 0 rgba(0,0,0,.22)') == 'inset')
    chk('뒤에 붙은 inset 도 안쪽으로', kind_of('0 0 0 2px #69f0ae inset') == 'inset')
    chk('흐림 0 은 hard', kind_of('0 .5rem 0 rgba(0,0,0,.25)') == 'hard')
    chk('흐림 있으면 드리운 그림자', kind_of('0 .3rem .6rem rgba(0,0,0,.45)') == 'drop')
    chk('음수 y 도 읽는다', kind_of('0 -.14rem .34rem rgba(0,0,0,.40)') == 'drop')
    chk('치우침 없이 흐리면 발광', kind_of('0 0 8px rgba(105,240,174,.45)') == 'glow')
    chk('퍼짐만 있으면 테두리(T109 축이라 여기선 안 본다)', kind_of('0 0 0 1px rgba(0,0,0,.45)') == 'ring')
    chk('치우침 + 흐림은 드리운 그림자', kind_of('0 .3rem .6rem rgba(0,0,0,.45)') == 'drop')
    chk('음수 퍼짐 발광도 발광', kind_of('0 0 .5rem -.14rem var(--rc, #ccc)') == 'glow')
    chk('calc 치우침도 읽는다', kind_of('calc(var(--dg) * 1 - 1px) 0 0 0 var(--dedge)') == 'hard')
    chk('키프레임 선택자를 가린다', bool(KEYFRAME_SEL.match('0%, 100%')) and bool(KEYFRAME_SEL.match('from')))
    chk('보통 선택자는 키프레임이 아니다', not KEYFRAME_SEL.match('.modal-card'))

    # ⓒ 주석 지우기 — 줄 번호가 안 밀린다
    src = 'a{}\n/* box-shadow: 0 1rem 0 #000;\n   두 줄 주석 */\n.b{ box-shadow: 0 .5rem 0 #000; }\n'
    cl = strip_comments(src)
    chk('주석을 지운다', 'box-shadow: 0 1rem 0' not in cl)
    chk('줄 수가 그대로다', cl.count('\n') == src.count('\n'))
    chk('주석 밖 선언은 남는다', 'box-shadow: 0 .5rem 0 #000' in cl)

    # ⓓ 정본에서 실제로 뽑는다
    decls = outer_decls()
    if decls is None:
        chk('정본이 옆에 없으면 건너뛴다(rc 0 갈래)', True)
    else:
        sels = set(s for _, s, _, _ in decls)
        chk('.modal-card 아래턱을 뽑는다', '.modal-card' in sels)
        chk('.panel 위턱을 뽑는다', '.panel' in sels)
        chk('안쪽만인 선언은 안 뽑는다(.tech-tree-node.locked)', '.tech-tree-node.locked' not in sels)
        chk('이 축이 보는 선언이 쉰은 넘는다(실측 58)', len(decls) > 50)
        chk('선언 총계는 168(등재 글과 같다)', sum(1 for _ in re.finditer(r'box-shadow\s*:', strip_comments(open(CSS, encoding='utf-8').read()))) == 168)

    # ⓔ 클론 호출 세기 — 주석은 안 센다
    import tempfile
    d = tempfile.mkdtemp()
    open(os.path.join(d, 'A.cs'), 'w', encoding='utf-8').write(
        'class A{ void F(){ UiShadow.Drop(rt, "card_lip"); }\n'
        '  // UiShadow.Drop(rt, "panel_lip");  주석은 호출이 아니다\n'
        '  void G(){ var x = 1; } }\n')
    keys = clone_keys(d)
    chk('호출을 센다', 'card_lip' in keys)
    chk('주석은 안 센다', 'panel_lip' not in keys)
    chk('어느 줄인지 적는다', keys['card_lip'][0].endswith(':1'))

    # ⓕ 판정 갈래 — 고장 주입
    saved = dict(KNOWN)
    try:
        # 키를 박아 두면 그 자리가 서는 순간(KNOWN 에서 빠지는 순간) 자기 검사가 KeyError 로 죽는다 — 런 632 가 그렇게 막혔다(10회차가 card_lip 을
        # 세우며 KNOWN 에서 뺐다). KNOWN 은 «표에 있는데 아직 안 선 자리» 의 목록이니 그중 아무 키나 뽑으면 같은 갈래가 된다.
        hole = next(iter(KNOWN), None)
        if hole is None:
            chk('안 선 자리를 잡는다 (KNOWN 이 비어 주입할 자리가 없다 · 전부 섰다 — 건너뜀)', True)
        else:
            KNOWN.pop(hole)                        # 표에 있고 KNOWN 에 없고 클론에도 없다 → 빨강
            chk('안 선 자리를 잡는다 (%s)' % hole, run() == 1)
            KNOWN.update(saved)
        chk('지금 그대로면 초록', run() == 0)
    finally:
        KNOWN.clear(); KNOWN.update(saved)

    print('%s check_box_shadows --self-test: %d칸%s'
          % ('✓' if not fails else '✗', ok, '' if not fails else ' · 어긋남: ' + ', '.join(fails)))
    return 0 if not fails else 1


if __name__ == '__main__':
    sys.exit(self_test() if '--self-test' in sys.argv else run())
