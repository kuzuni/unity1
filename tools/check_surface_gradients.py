#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T178 — 정본 «표면 겹»(`linear/radial/conic-gradient` 선언) ↔ 클론 «구운 겹» 대조 자.

정본 `web/css/style.css` 는 거의 모든 면에 같은 문법을 깐다 — 위쪽 1px 흰 줄(`linear-gradient(180deg, rgba(255,255,255,.6) 0 1px, …)`)
+ 45° 미세 빗금(`repeating-linear-gradient(45deg, … 0 2px, … 2px 9px)`) + 세로 명암. 선언 176(선택자 137 · `background` 83 ·
`background-image` 60 · `mask-image` 30 · `--af-pat` 3). 클론 공용 표면(`UiKit.Panel`·`Rounded`)은 색 한 칸짜리 `Image` 라
그 겹이 **조용히 사라진다** — 화면이 «플라스틱 단색» 으로 읽히는 까닭이다(T33 16회차 실측).

무엇을 «겹이 있다» 로 세나: 그 자리가 **굽는 길**(결정 223 — 이 레포에서 모양·명암은 전부 구운 스프라이트로 온다)을 지나가는가 —
`CraftFxPoly.Bake*`(정지점 그라디언트) · `AgePattern.Tile`(T124 시대 무늬) · `BattleOverlay.RadialSprite/VigSprite`(T173 방사형) ·
`BootLoading.GradSprite` · `CraftCardArt.Sheen` · 앞으로 설 공용 도우미 `UiKit.Surface*`/`SurfaceArt.*`(T178 ⓑ) · 제 손으로 굽는 `Sprite.Create`.

선택자 ↔ 클론 자리 짝은 TABLE 하나가 쥔다(T109 `check_keyline` · T159 `check_clip_paths` 의 꼴 그대로):
  "Ui/File.cs"           파일 안 어디든 굽는 호출이 하나 있으면 됨
  "Ui/File.cs#name"      그 이름으로 만든 오브젝트가 굽는 공장에서 나왔거나, 그 변수에 굽는 호출이 닿아야 함
  "Ui/File.cs@Method"    도우미 메서드 본문 안에 굽는 호출이 있어야 함
  "—<이유>"              클론에 그 자리가 없거나 정본이 그 겹을 끄는 규칙 — 대조하지 않는다

⚠ `check_clip_paths` 와 다른 한 가지 — **표에 없는 정본 선택자는 «미정» 으로 세고 막지 않는다**(rc 그대로). 선택자가 137 이라
한 회차에 다 짝지을 수 없고, 짝이 없는 자리는 «아직 안 옮긴 자리» 지 «표가 틀린 자리» 가 아니다. 회차마다 TABLE 이 자라고
«미정» 이 줄어드는 것이 이 자의 진행 수치다. 막는 것은 둘 — 표에 적힌 자리에 겹이 없는데 KNOWN 도 아닌 것 · 표에는 있는데
정본에서 사라진 선택자.

빈자리 중 «임자가 정해진 것» 은 KNOWN 에 두어 rc 0 으로 지나간다(T89·T109·T159 의 KNOWN 규약). 닫았는데 안 지우면 «이제 있다» 로 알린다.

사용:  python3 tools/check_surface_gradients.py [--css <style.css 경로>] [--game <Assets/Scripts/Game>] [--list] [--self-test]
rc:    0 = 표의 자리 전부가 (겹이 있거나 KNOWN) 이고 표의 선택자가 전부 정본에 있다 · 1 = 아니다 · 2 = 정본 CSS 를 못 읽었다
"""
import os
import re
import sys
import tempfile

CSS_DEFAULT = os.path.join('.wwwww-src', 'web', 'css', 'style.css')
GAME_DEFAULT = os.path.join('Assets', 'Scripts', 'Game')

# ── 정본 선택자 ↔ 클론 자리 ──────────────────────────────────────────────────────────────
# 선택자는 style.css 의 것을 공백 하나로 정규화한 그대로(한 규칙에 선택자가 여럿이면 쉼표 목록 통째로). 표에 없는 선택자 = 미정.
TABLE = {
    # T173 — 보스 경고 두 겹(방사형 딤 · 가산 플래시)은 BattleOverlay.Radial 공장이 RadialSprite 로 굽는다.
    '.bw-dim': ['Ui/BattleOverlay.cs#bw-dim'],
    '.bw-flash': ['Ui/BattleOverlay.cs#bw-flash'],
    # T135 — 피격 붉은 비네트(정본 #dmg-flash 의 radial + mask) → VigSprite.
    '#dmg-flash': ['Ui/BattleOverlay.cs@VigSprite'],
    # T124 — 시대 무늬 셋(--af-pat 사용자 속성)은 AgePattern.Tile 이 굽는다.
    '.af-age-bar[data-age="interstellar"], .fi-age-bar[data-age="interstellar"], .equip-cell[data-age="interstellar"]': ['Ui/AgePattern.cs@Tile'],
    '.af-age-bar[data-age="multiverse"], .fi-age-bar[data-age="multiverse"], .equip-cell[data-age="multiverse"]': ['Ui/AgePattern.cs@Tile'],
    '.af-age-bar[data-age="quantum"], .fi-age-bar[data-age="quantum"], .equip-cell[data-age="quantum"]': ['Ui/AgePattern.cs@Tile'],
    # T87 28회차 — 결과 카드 광택 띠(crsheen) 는 CraftCardArt.Sheen 이 굽는다.
    '.auto-drop-card.craft-reveal::after': ['Ui/CraftCardArt.cs@Sheen'],
    # T178 2회차 — 던전 배너의 비스듬한 바탕과 «왼쪽 제목 자리 스크림» 은 공용 굽기 SurfaceArt 가 표(SurfaceUi.json)대로 굽는다.
    '.dg-banner': ['Ui/DungeonSheet.cs#bg-grad'],
    '.dg-banner::before': ['Ui/DungeonSheet.cs#scrim'],
    # T178 3회차 — 둥근 면 위는 SurfaceArt.FillMasked(면에 Mask) 로 얹는다(모서리 밖으로 안 샌다).
    '.shop-banner': ['Ui/ShopSheet.cs#shop-banner-grad'],
    '.league-collect-pill': ['Ui/LeagueSheet.cs#collect-grad'],
    '.pinfo-preview': ['Ui/PlayerInfoPopup.cs#preview-grad'],
    # T178 4회차 — 하단 탭바 밴드(겹 둘 · 아래 1px 림은 표의 `unit: "px"`) · 스킬 확률 막대(에나멜 하이라이트 + 위 1px 림 · 둥근 면이라 FillMasked).
    # T178 6회차 — 퀘스트 진행 막대 채움의 두 겹(세로 띠 + 위 1px 광택). 상태는 띠 키로만 가른다(정본 주석).
    '.qst-bar i': ['Ui/QuestSheet.cs#qst-fill-grad', 'Ui/QuestSheet.cs#qst-fill-rim'],
    '.qst-row.done .qst-bar i': ['Ui/QuestSheet.cs#qst-fill-grad', 'Ui/QuestSheet.cs#qst-fill-rim'],
    '#tabbar': ['Ui/TabBar.cs#tabbar-grad', 'Ui/TabBar.cs#tabbar-rim'],
    # T178 5회차 — 켜진 칸(그리고 ✕ 칸)의 노란 방사형 둘. `SurfaceArt` 가 방사형을 굽고 `RefreshTabX` 가 켠다.
    '#tabbar button.active, #tabbar button.tab-x': ['Ui/TabBar.cs#tab-glow', 'Ui/TabBar.cs#tab-footglow'],
    '.rate-bar': ['Ui/SkillRatesPopup.cs#rate-enamel', 'Ui/SkillRatesPopup.cs#rate-rim'],
}

# ── 임자가 정해진 빈자리(자리 → 이유) — 닫을 때마다 지운다 ────────────────────────────────
KNOWN = {
}

# 굽는 길(결정 223)
BAKE = (r'CraftFxPoly\.Bake\w*|AgePattern\.Tile|RadialSprite|VigSprite|GradSprite|CraftCardArt\.Sheen|Sheen'
        r'|SurfaceArt\.\w+|UiKit\.Surface\w*|Sprite\.Create|Texture2D')   # `new Texture2D(` = 제 손으로 굽는 자리
BAKE_CALL = re.compile(r'\b(?:' + BAKE + r')\s*\(')
# 겹을 **제 손으로** 굽는 공장(이 호출 자체가 곧 겹이다) — `Radial(warnRoot, "bw-dim", …)` · `GradFace(root, "bg", …)`
FACTORY = r'Radial|GradFace|SurfaceArt\.\w+|UiKit\.Surface\w*'
SHAPE_FACTORY = re.compile(r'^(?:' + FACTORY + r')$')
CREATE_CALL = re.compile(r'([\w.]+)\s*\(\s*[^,()]+,\s*"([^"]+)"')
ASSIGN_TAIL = re.compile(r'([\w\[\]\.]+)\s*=\s*(?:[\w!.()\[\]]+\s*\?\s*)?[\w.]*$')
GRAD_PROP = re.compile(r'(?<![\w-])(background(?:-image)?|(?:-webkit-)?mask-image|--[\w-]+)\s*:\s*([^;]+)')
GRAD_KIND = re.compile(r'(repeating-linear-gradient|repeating-radial-gradient|linear-gradient|radial-gradient|conic-gradient)\(')
KIND_CODE = {'linear-gradient': 'L', 'radial-gradient': 'R', 'repeating-linear-gradient': 'rL',
             'repeating-radial-gradient': 'rR', 'conic-gradient': 'C'}


def bake_on(var):
    """그 변수에 굽는 호출이 닿는가 — 첫 인자로(`Bake(tl, …)`) 또는 그 변수의 그림 칸에 대입으로(`tl.sprite = Bake(…)` · `tl.texture = AgePattern.Tile(…)`)."""
    v = re.escape(var)
    return re.compile(r'\b(?:' + BAKE + r')\s*\(\s*' + v + r'\s*[,)]'
                      r'|' + v + r'\s*\.\s*(?:sprite|texture|overrideSprite|material)\s*=\s*(?:[\w.]+\s*\?\s*)?(?:' + BAKE + r')\s*\(')


# ── 정본 CSS ──────────────────────────────────────────────────────────────────────────────
def _blank_comments(css):
    def rep(m):
        return ''.join('\n' if ch == '\n' else ' ' for ch in m.group(0))
    return re.sub(r'/\*.*?\*/', rep, css, flags=re.S)


def parse_rules(css_text):
    """[(줄, 선택자, 속성, 겹 코드)] — 값에 `…gradient(` 가 든 선언 전부(background · background-image · mask-image · 사용자 속성).
    겹 코드 = 겹마다 한 글자(L 선형 · R 방사 · rL/rR 반복 · C 원뿔) 를 이어 붙인 것 — «몇 겹인가» 가 한눈에 보인다."""
    css = _blank_comments(css_text)
    out = []
    for m in re.finditer(r'([^{}]+)\{([^{}]*)\}', css):
        sel = ' '.join(m.group(1).split())
        lead = len(m.group(1)) - len(m.group(1).lstrip())
        line = css[:m.start(1) + lead].count('\n') + 1
        for d in GRAD_PROP.finditer(m.group(2)):
            val = d.group(2)
            if 'gradient(' not in val:
                continue
            kinds = ''.join(KIND_CODE[k] for k in GRAD_KIND.findall(val))
            out.append((line, sel, d.group(1), kinds))
    return out


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
    """(상태, 설명) — 'ok' | 'missing'(자리는 있는데 색 한 칸) | 'absent'(자리·메서드·파일 없음)."""
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
    return 'missing', '"%s" 는 굽는 호출이 안 닿는다 — 색 한 칸짜리 Image 다' % tail


# ── 대조 ──────────────────────────────────────────────────────────────────────────────────
def run(css_path, game_dir, table, known, out=print, list_pending=False):
    if not os.path.isfile(css_path):
        out('✗ 정본 CSS 를 못 읽었다: %s (git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)' % css_path)
        return 2
    rules = parse_rules(_read(css_path))
    problems = 0
    seen = set()
    n_off = n_ok = n_known = 0
    pending = []          # (줄, 선택자, 속성, 겹) — 표에 없는 정본 선언
    known_now_ok = []
    checked = set()       # (선택자, 자리) — 같은 선택자의 선언이 여럿이어도 자리는 한 번만 센다
    for line, sel, prop, kinds in rules:
        seen.add(sel)
        if sel not in table:
            pending.append((line, sel, prop, kinds))
            continue
        targets = table[sel]
        if isinstance(targets, str):
            if (sel, targets) not in checked:
                checked.add((sel, targets)); n_off += 1
            continue
        for t in targets:
            if (sel, t) in checked:
                continue
            checked.add((sel, t))
            state, why = check_target(game_dir, t)
            if state == 'ok':
                n_ok += 1
                if t in known:
                    known_now_ok.append(t)
                continue
            tag = '겹 없음' if state == 'missing' else '자리 없음'
            if t in known:
                n_known += 1
                out('· KNOWN(%s)  %s  ← style.css %d %s { %s %s }  — %s' % (tag, t, line, sel, prop, kinds, known[t]))
            else:
                problems += 1
                out('✗ %s  %s  ← style.css %d  %s  { %s %s }  — %s' % (tag, t, line, sel, prop, kinds, why))
    for sel in table:
        if sel not in seen:
            problems += 1
            out('✗ 표에는 있는데 정본에 없는 선택자: %s  → 정본이 바뀌었다 · TABLE 에서 지우거나 고쳐라' % sel)
    for t in sorted(set(known_now_ok)):
        out('· KNOWN 인데 이제 겹이 있다: %s  → KNOWN 에서 지워라' % t)
    pend_sel = sorted(set(p[1] for p in pending))
    if list_pending:
        for line, sel, prop, kinds in pending:
            out('· 미정  style.css %5d  %-64s %-18s %s' % (line, sel[:64], prop, kinds))
    out('%s check_surface_gradients: 정본 겹 선언 %d(선택자 %d) · 끄는 규칙 %d · 자리 초록 %d · KNOWN 빈자리 %d · 미정 선택자 %d(선언 %d · --list 로 본다) · 문제 %d'
        % ('✓' if problems == 0 else '✗', len(rules), len(set(r[1] for r in rules)), n_off, n_ok, n_known, len(pend_sel), len(pending), problems))
    return 0 if problems == 0 else 1


# ── 자기 검사(고장 주입) ────────────────────────────────────────────────────────────────────
def self_test():
    fails = []
    tmp = tempfile.mkdtemp(prefix='sgrad_')
    css = os.path.join(tmp, 'style.css')
    game = os.path.join(tmp, 'Game')
    os.makedirs(os.path.join(game, 'Ui'))

    def w(path, text):
        with open(path, 'w', encoding='utf-8') as f:
            f.write(text)

    def cs(text):
        w(os.path.join(game, 'Ui', 'Face.cs'), text)

    def go(table, known, css_text=None, list_pending=False):
        if css_text is not None:
            w(css, css_text)
        lines = []
        rc = run(css, game, table, known, out=lines.append, list_pending=list_pending)
        return rc, '\n'.join(lines)

    def expect(name, cond, detail=''):
        if not cond:
            fails.append(name + (' — ' + detail if detail else ''))

    base_css = ('/* .fake-comment { background: linear-gradient(red, blue) } */\n'
                '.g-a { background: linear-gradient(180deg, rgba(255,255,255,.6) 0 1px, transparent 1px), '
                'repeating-linear-gradient(45deg, #000 0 2px, #fff 2px 9px); }\n'
                '.g-b { background-image: radial-gradient(circle, #fff, #000); }\n'
                '.g-c::before { -webkit-mask-image: linear-gradient(#000, transparent); mask-image: linear-gradient(#000, transparent); }\n'
                '.g-d { --af-pat: repeating-radial-gradient(circle, #000 0 1px, #fff 1px 3px); }\n'
                '.g-e { background: #fff; }\n'
                '.g-f, .g-g { background: conic-gradient(from 0deg, #000, #fff); }\n')
    # ⓐ 파싱 — 주석 속 선언은 빼고 · 속성 넷(background · background-image · mask-image 둘 · --af-pat) · 겹 코드
    rules = parse_rules(base_css)
    expect('ⓐ 선언 수', len(rules) == 6, str(len(rules)))
    expect('ⓐ 주석 제외', all(r[1] != '.fake-comment' for r in rules))
    expect('ⓐ 겹 코드', [r[3] for r in rules] == ['LrL', 'R', 'L', 'L', 'rR', 'C'], str([r[3] for r in rules]))
    expect('ⓐ 쉼표 선택자 통째', any(r[1] == '.g-f, .g-g' for r in rules))
    expect('ⓐ 줄 번호', rules[0][0] == 2, str(rules[0][0]))
    # ⓑ 자리에 겹이 있다 — #이름(공장) · #이름(변수에 닿음) · @메서드 · 파일 전체
    cs('class Face { static void B(Transform p){ Image a = Radial(p, "g-a", "k", null); '
       'Image b = UiKit.Panel(p, "g-b", "c"); b.sprite = CraftFxPoly.Bake("x", pts); '
       'RawImage c = UiKit.Raw(p, "g-c", "c"); c.texture = AgePattern.Tile(age, 0); }\n'
       ' static Sprite M(){ return Sprite.Create(t, r, v); } }')
    table = {'.g-a': ['Ui/Face.cs#g-a'], '.g-b': ['Ui/Face.cs#g-b'], '.g-c::before': ['Ui/Face.cs#g-c'],
             '.g-d': ['Ui/Face.cs@M'], '.g-f, .g-g': ['Ui/Face.cs']}
    rc, out = go(table, {}, base_css)
    expect('ⓑ 겹 있는 자리 rc 0', rc == 0, out)
    expect('ⓑ 자리 초록 5', '자리 초록 5' in out, out)
    expect('ⓑ 미정 0', '미정 선택자 0' in out, out)
    # ⓒ 표에 없는 정본 선택자 = 미정(막지 않는다 · --list 로 보인다)
    rc, out = go({'.g-a': ['Ui/Face.cs#g-a']}, {}, list_pending=True)
    expect('ⓒ 미정은 rc 0', rc == 0, out)
    expect('ⓒ 미정 선택자 4', '미정 선택자 4(선언 5' in out, out)
    expect('ⓒ --list 줄', '· 미정  style.css' in out and '.g-d' in out, out)
    # ⓓ 표의 자리에 겹이 없다(색 한 칸) → rc 1 · KNOWN 이면 rc 0 · KNOWN 인데 닫혔으면 알림
    cs('class Face { static void B(Transform p){ Image a = UiKit.Panel(p, "g-a", "c"); } }')
    rc, out = go({'.g-a': ['Ui/Face.cs#g-a']}, {})
    expect('ⓓ 겹 없음 rc 1', rc == 1 and '겹 없음' in out, out)
    rc, out = go({'.g-a': ['Ui/Face.cs#g-a']}, {'Ui/Face.cs#g-a': 'T999 몫'})
    expect('ⓓ KNOWN rc 0', rc == 0 and 'KNOWN(겹 없음)' in out, out)
    cs('class Face { static void B(Transform p){ Image a = Radial(p, "g-a", "k", null); } }')
    rc, out = go({'.g-a': ['Ui/Face.cs#g-a']}, {'Ui/Face.cs#g-a': 'T999 몫'})
    expect('ⓓ KNOWN 닫힘 알림', rc == 0 and 'KNOWN 인데 이제 겹이 있다' in out, out)
    # ⓔ 자리 없음(이름·메서드·파일) → rc 1
    rc, out = go({'.g-a': ['Ui/Face.cs#nope'], '.g-b': ['Ui/Face.cs@Nope'], '.g-d': ['Ui/Gone.cs']}, {})
    expect('ⓔ 자리 없음 rc 1', rc == 1 and out.count('자리 없음') == 3, out)
    # ⓕ 표에는 있는데 정본에 없는 선택자 → rc 1
    rc, out = go({'.g-a': ['Ui/Face.cs#g-a'], '.g-zzz': ['Ui/Face.cs#g-a']}, {})
    expect('ⓕ 정본에 없는 선택자 rc 1', rc == 1 and '정본에 없는 선택자' in out, out)
    # ⓖ 끄는 규칙('—') 은 대조하지 않는다 · 같은 선택자의 선언이 여럿이어도 자리는 한 번만 센다
    rc, out = go({'.g-a': ['Ui/Face.cs#g-a'], '.g-c::before': '—클론에 자리 없음'}, {})
    expect('ⓖ 끄는 규칙', rc == 0 and '끄는 규칙 1' in out and '자리 초록 1' in out, out)
    # ⓗ CSS 없음 → rc 2
    rc = run(os.path.join(tmp, 'none.css'), game, {}, {}, out=lambda s: None)
    expect('ⓗ CSS 없음 rc 2', rc == 2)
    # ⓘ 진짜 표(TABLE·KNOWN)의 자리 문법이 전부 유효한가
    for sel, ts in TABLE.items():
        if isinstance(ts, str):
            expect('ⓘ 끄는 규칙 문법 ' + sel, ts.startswith('—'))
            continue
        for t in ts:
            expect('ⓘ 자리 문법 ' + t, re.match(r'^[\w/]+\.cs([#@][\w-]+)?$', t) is not None)
    if fails:
        print('✗ check_surface_gradients --self-test 실패 %d' % len(fails))
        for f in fails:
            print('  · ' + f[:400])
        return 1
    print('✓ check_surface_gradients --self-test 18칸 통과')
    return 0


def main(argv):
    css, game, list_pending = CSS_DEFAULT, GAME_DEFAULT, False
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--self-test':
            return self_test()
        if a == '--list':
            list_pending = True; i += 1; continue
        if a == '--css' and i + 1 < len(argv):
            css = argv[i + 1]; i += 2; continue
        if a == '--game' and i + 1 < len(argv):
            game = argv[i + 1]; i += 2; continue
        print('사용: check_surface_gradients.py [--css <style.css>] [--game <Assets/Scripts/Game>] [--list] [--self-test]')
        return 2
    return run(css, game, TABLE, KNOWN, list_pending=list_pending)


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
