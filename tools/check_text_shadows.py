#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T333 — 정본 `text-shadow` 선언 ↔ 클론 글자 그림자(`UiKit.TextShadow` · TMP 언더레이) 대조 자.

정본 `web/css/style.css` 의 `text-shadow` 는 54 선언 — 그중 `none` 4(끄는 규칙) · `check_keyline` TABLE 의 선택자(T104·T109 가 키라인으로 옮긴 자리 · 4/8방향 hard 링 등) 10 은
그쪽 몫이라 빼고 센다. 남은 자리는 «표(TABLE)에 짝을 적은 것만» 대조한다 — 표에 없는 정본 선택자는 «미정» 으로 세기만 하고 막지 않는다(T178 `check_surface_gradients` 규약 ·
선택자가 많아 회차마다 표가 자란다 · `--list` 로 본다). 막는 것은 둘 — 표에 적힌 자리에 그림자 호출이 없는데 KNOWN 도 아닌 것 · 표에는 있는데 정본에서 사라진 선택자.

무엇을 «그림자가 있다» 로 세나: 그 자리가 `UiKit.TextShadow(` 를 지나가는가(TMP `_Underlay*` 를 직접 만지는 것은 안 센다 — 식·표를 거치지 않으면 §1 «수치는 표로» 를 샌다).

선택자 ↔ 클론 자리 짝은 TABLE 하나가 쥔다(T109·T159·T178 의 꼴 그대로):
  "Ui/File.cs"           파일 안 어디든 그림자 호출이 하나 있으면 됨
  "Ui/File.cs#name"      그 이름으로 만든 글자에 그림자 호출이 닿아야 함(변수 대입 꼬리를 좇는다)
  "Ui/File.cs@Method"    도우미 메서드 본문 안에 그림자 호출이 있어야 함
  "—<이유>"              대조하지 않는다(클론에 자리 없음 등)
  "ring:<위 꼴>"         그 자리에 **링**(`UiKit.Outline`/`OutlinePx` · SDF 스트로크)이 닿아야 함 — 정본이 4/8방향 hard 링으로 흉내 낸
                         text-shadow 는 TMP 언더레이 한 겹으로 못 낸다(T333 3회차 · 등재 절 ⓒ · `.chat-name` 20겹 = 변당 2px 순검정).

빈자리 중 «임자가 정해진 것» 은 KNOWN 에 두어 rc 0 으로 지나간다. 닫았는데 안 지우면 «이제 있다» 로 알린다.

사용:  python3 tools/check_text_shadows.py [--css <style.css 경로>] [--game <Assets/Scripts/Game>] [--list] [--self-test]
rc:    0 = 표의 자리 전부가 (그림자가 있거나 KNOWN) 이고 표의 선택자가 전부 정본에 있다 · 1 = 아니다 · 2 = 정본 CSS 를 못 읽었다
"""
import os
import re
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
CSS_DEFAULT = os.path.join('.wwwww-src', 'web', 'css', 'style.css')
GAME_DEFAULT = os.path.join('Assets', 'Scripts', 'Game')

# ── 정본 선택자 ↔ 클론 자리 ────────────────────────────────────────────────────
# 선택자는 style.css 의 것을 공백 하나로 정규화한 그대로(쉼표 목록 통째로). 표에 없는 선택자 = 미정.
TABLE = {
    # T333 1회차 — 색 버튼 라벨(8336·8504 같은 선택자 · 뒤 규칙 .62 가 이긴다) · 비활성 흰 엠보스(8356) → 공용 PopupKit.Btn
    '.btn.btn:not(.silver):not(.ascend-ready), .modal-card .btn.btn:not(.silver):not(.ascend-ready), .panel .btn.btn:not(.silver):not(.ascend-ready), #equip-sheet .btn.btn:not(.silver):not(.ascend-ready)': ['Ui/Popups.cs@Btn'],
    '.btn.btn.disabled, .modal-card .btn.btn.disabled, .panel .btn.btn.disabled, #equip-sheet .btn.btn.disabled': ['Ui/Popups.cs@Btn'],
    # T333 2회차 — 8381 한 벌 «밝은 종이 위 글자는 흰 엠보스»: 시트 제목 다섯 자리(나머지 선택자는 KNOWN 에 임자와 함께)
    '.sheet-title, .modal-card h3, .modal-card .idet-name, .stat-grid div, .substat-list div, .prob-box div, .shop-reward-pill, .qst-row .qst-name': [
        'Ui/MountSheet.cs#sheet-title', 'Ui/PetPanel.cs#sheet-title', 'Ui/SkillPanel.cs#sheet-title',
        'Ui/ShopSheet.cs#title', 'Ui/AscendPopup.cs#title',
        'Ui/ForgeInfoPopup.cs#idet-name', 'Ui/QuestSheet.cs#qst-name',
    ],
    # T333 3회차 ⓒ — 정본이 «4/8방향 hard 링» 으로 흉내 낸 것: 언더레이 한 겹으로는 못 내니 SDF 스트로크(`ring:`)로 센다
    '#stage-label': ['ring:Ui/Hud.cs#stage-label'],
    '.chat-name, .chat-tag': ['ring:Ui/ChatScreen.cs#name'],
    '.dgd-title': ['ring:Ui/DungeonDetailPopup.cs#title'],
    # T333 4회차 조사 — 정본이 «삼각 글리프» 에 두른 8방향 1px 링(정본 주석 3281 «삼각 글리프의 검정 외곽선도 여기서 준다»).
    #   클론의 그 삼각은 글자가 아니라 **아틀라스 아이콘**(`PopupKit.Tri` → `UiKit.Icon("tri_left")`)이라 언더레이도 SDF 스트로크도 안 닿는다 → KNOWN.
    '.chat-input-bar .btn.danger.round': ['ring:Ui/ChatScreen.cs@Open'],
    # T333 3회차 — 8392 두 겹 중 첫 겹(읽히게 만드는 아래 1px 드롭 · 둘째 겹 글로우는 근사로 뺀다 · 표 `league_row` 주석)
    '.league-row .league-name, .league-row .league-rank, .league-score, .equipped-label + * , .chat-preview-name': [
        'Ui/LeagueSheet.cs#rank', 'Ui/LeagueSheet.cs#name',
        # `.league-score` 는 아이콘+수 두 조각(`UiKit.RowTexts`)이라 «이름 → 변수» 꼬리로는 못 좇는다 — 그 행을 세우는 메서드로 센다
        'Ui/LeagueSheet.cs@Row',
    ],
    # T333 8회차 — 장비 칸 이름표 두 자리(빈 칸 라벨): 알 칸은 클래스 셋이라 8030(둘)을 특이도로 이긴다 — 표 키가 둘로 갈린다
    '.equip-cell .slot-name': ['Ui/PlayerInfoPopup.cs@EquipCell', 'Ui/ForgeSheet.cs@EquipCell'],
    '.equip-cell.egg-cell .slot-name': ['Ui/PlayerInfoPopup.cs@MountWide', 'Ui/ForgeSheet.cs@MountCell'],
    # T333 5회차 — 산 lock 밖 세 자리(T335 반납으로 열린 던전 클리어 제목 · 보스 워닝 마퀴·부제): 여러 겹 중 «읽히게 만드는 한 겹»(league_row 갈래)
    # T333 9회차 — 판매 코인 금액(7426 `.coin-amt` · 정본 주석 «이너=노랑 · 아웃라인=검정»): 8방향 링 → SDF 스트로크(`ring:`) · 값은 CoinBurstUi.json(amt_ring_* · 결정 655)
    '.coin-amt': ['ring:Ui/CoinBurst.cs@Amount'],
    # T333 11회차 — «이미 굽고 있는데 자가 못 보던» 세 자리(게임 코드 0줄 · 자의 눈만 넓혔다 · 결정 668)
    #   ⓔ 스킬 컷인 이름줄: 정본 1897 `0 0 10px currentColor` 를 T384 가 이미 언더레이로 굽는다(색 = 그 스킬 색 · 표 SkillCutinUi.json `glow.text_blur_px` 10).
    #      둘째 겹(0 2px 5px 검정)은 언더레이 한 겹 규약대로 뺀 자리다.
    '#skill-cutin': ['glow:Ui/SkillCutin.cs@Show'],
    #   ⓒ 별 배지 둘(정본이 4/8방향 hard 링으로 낸 자리): 링은 `PopupKit.Ring`(= UiKit.Outline 래퍼)로 이미 서 있다.
    #      ⚠ 폭은 아직 **옛 비율 0.25**(TMP 여백 배수)이고 정본은 `--ol`·1px 다 — 그 px 화는 `ForgeUi.cs`·`ForgeInfoPopup.cs` 가
    #      T332 산 lock 이라 다음 회차 몫이다(9회차 `.coin-amt` 와 같은 길 · 절의 «남은 것» 에 적었다).
    '.equip-cell .cell-star': ['ring:Ui/ForgeUi.cs@StarBadge'],
    '.fl-face[data-asc]:not([data-asc=""])::after': ['ring:Ui/ForgeInfoPopup.cs#asc'],
    # T333 10회차 ⓔ — 데미지 숫자 글로우(크리·처치): 정본은 겹을 여럿 쌓고 `@keyframes` 가 **태어나는 프레임**에 다른 겹을 둔다.
    #   TMP 언더레이는 한 겹이라 «읽히게 만드는 한 겹»만 옮기고(표 `DmgGlowUi.json`), 시간에 따른 갈아 끼움은 공유 재질 교체로 낸다(`glow:`).
    '.float-dmg.dmg-crit': ['glow:Battle/DamageNumbers.cs@OutlineMaterial'],
    '@keyframes dmgcrit 0%': ['glow:Battle/DamageNumbers.cs@OutlineMaterial'],
    '@keyframes dmgcrit 9%': ['glow:Battle/DamageNumbers.cs@OutlineMaterial'],
    '@keyframes dmgkill 0%': ['glow:Battle/DamageNumbers.cs@OutlineMaterial'],
    '@keyframes dmgkill 7%': ['glow:Battle/DamageNumbers.cs@OutlineMaterial'],
    '.dgclear-title': ['Ui/DungeonClearPopup.cs#title'],
    '.bw-track span': ['Ui/BattleOverlay.cs#text'],
    '.bw-sub': ['Ui/BattleOverlay.cs#bw-sub'],
}

# ── 임자가 정해진 빈자리(자리 → 이유) — 닫을 때마다 지운다 ────────────────────────────────
KNOWN = {
    'Ui/ForgeInfoPopup.cs#idet-name': 'T332·T339 의 산 lock 이 쥔 파일 — 정본 8381 `.modal-card .idet-name`(장비 상세 이름) 은 그 lock 뒤',
    'Ui/QuestSheet.cs#qst-name': 'T331 산 lock + 클론에 그 이름 자리가 아직 없다 — 정본 8381 `.qst-row .qst-name`',
    'ring:Ui/ChatScreen.cs@Open': 'T333 4회차 — 클론의 «◀» 는 글자가 아니라 아틀라스 아이콘(tri_left)이라 링을 글자에 못 두른다: 키운 삼각을 뒤에 깔아야 하고 아틀라스엔 검정 틴트 변형이 없다(정본이 안 부른다) · 길 둘 = Image.color 곱하기 / 도형 굽기 · 다음 회차',
}

SHADOW = r'UiKit\.TextShadow'
# ⓒ 4/8방향 hard 링(정본이 text-shadow 로 흉내 낸 키라인) — 언더레이 한 겹으로는 못 내니 SDF 스트로크로 낸다(T104 `UiKit.Outline`/`OutlinePx`).
# 도우미로 한 번 감싼 호출도 같은 것으로 센다 — `PopupKit.Ring` 은 `UiKit.Outline`/`OutlinePx` 한 줄 래퍼다(T333 11회차).
RING = r'(?:UiKit\.Outline(?:Px)?|PopupKit\.Ring)'
# ⓔ 데미지 숫자 글로우(T333 10회차) — 초당 수십 개라 글자마다 재질 인스턴스를 만들 수 없어 `UiKit.TextShadow`(fontMaterial) 가 아니라
#   공유 재질에 굽는 갈래(`DmgGlowUi.Apply` · 표 DmgGlowUi.json)로 낸다.
#   컷인은 제 파일 안에서 같은 일을 하는 도우미(`SkillCutin.ApplyTextGlow`)를 부른다 — 이름으로 같이 센다(T333 11회차).
GLOW = r'(?:DmgGlowUi\.Apply|ApplyTextGlow)'
SHADOW_CALL = re.compile(r'\b' + SHADOW + r'\s*\(')
RING_CALL = re.compile(r'\b' + RING + r'\s*\(')
GLOW_CALL = re.compile(r'\b' + GLOW + r'\s*\(')
CREATE_CALL = re.compile(r'\.(Text|Label|Bold|Stroked|IconTextRow|Btn)\s*\(\s*[^,()]+,\s*"([^"]+)"')
ASSIGN_TAIL = re.compile(r'([\w\[\]\.]+)\s*=\s*(?:[\w!.()\[\]]+\s*\?\s*)?[\w.]*$')
DECL = re.compile(r'(?<![\w-])text-shadow\s*:\s*([^;}]+)')


def shadow_on(var, call=SHADOW):
    v = re.escape(var)
    return re.compile(r'\b' + call + r'\s*\(\s*' + v + r'\s*,')


def keyline_selectors():
    """check_keyline 의 TABLE 선택자 — 키라인으로 옮긴 자리는 이 자의 몫이 아니다."""
    try:
        sys.path.insert(0, HERE)
        import check_keyline
        return set(check_keyline.TABLE.keys())
    except Exception:
        return set()


# ── 정본 CSS ────────────────────────────────────────────────────────────
def _blank_comments(css):
    def rep(m):
        return ''.join('\n' if ch == '\n' else ' ' for ch in m.group(0))
    return re.sub(r'/\*.*?\*/', rep, css, flags=re.S)


def _keyframe_ranges(css):
    """[(시작, 끝, 이름)] — `@keyframes <이름> { … }` 의 바깥 중괄호 범위(T333 10회차).

    키프레임 안의 `0%`·`9%` 같은 «선택자» 는 그것만으로 어느 애니메이션인지 모른다(`dmgcrit` 0% ↔ `dmgkill` 0%).
    표 키가 가리키는 자리를 사람이 알아보게 `@keyframes <이름> 0%` 로 붙여 센다.
    """
    out = []
    for m in re.finditer(r'@keyframes\s+([\w-]+)\s*\{', css):
        depth = 0
        end = len(css)
        for j in range(m.end() - 1, len(css)):
            if css[j] == '{':
                depth += 1
            elif css[j] == '}':
                depth -= 1
                if depth == 0:
                    end = j
                    break
        out.append((m.start(), end, m.group(1)))
    return out


def parse_rules(css_text):
    """[(줄, 선택자, 값)] — `text-shadow` 선언 전부(주석 속은 뺀다 · `none` 포함 · 값은 공백 하나로).

    `@keyframes` 안의 선언은 선택자 앞에 `@keyframes <이름> ` 을 붙인다(같은 `0%` 가 애니메이션마다 다르다 · T333 10회차).
    """
    css = _blank_comments(css_text)
    frames = _keyframe_ranges(css)
    out = []
    for m in re.finditer(r'([^{}]+)\{([^{}]*)\}', css):
        sel = ' '.join(m.group(1).split())
        lead = len(m.group(1)) - len(m.group(1).lstrip())
        pos = m.start(1) + lead
        line = css[:pos].count('\n') + 1
        for a, b, name in frames:
            if a <= pos <= b:
                sel = '@keyframes ' + name + ' ' + sel
                break
        for d in DECL.finditer(m.group(2)):
            out.append((line, sel, ' '.join(d.group(1).split())))
    return out


# ── 클론 ──────────────────────────────────────────────────────────
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
    """(상태, 설명) — 'ok' | 'missing'(자리는 있는데 그림자 호출 없음) | 'absent'(자리·메서드·파일 없음).

    `ring:` 접두가 붙으면 «언더레이» 가 아니라 «SDF 스트로크»(`UiKit.Outline`/`OutlinePx`)를 센다 — 정본이 4/8방향 hard 링으로
    흉내 낸 text-shadow 는 TMP 언더레이 한 겹으로 못 내기 때문이다(T333 3회차 · 등재 절 ⓒ).
    """
    ring = target.startswith('ring:')
    glow = target.startswith('glow:')
    if ring:
        target = target[len('ring:'):]
    elif glow:
        target = target[len('glow:'):]
    if ring:
        call, call_re, word = RING, RING_CALL, '링'
    elif glow:
        call, call_re, word = GLOW, GLOW_CALL, '글로우'
    else:
        call, call_re, word = SHADOW, SHADOW_CALL, '그림자'
    file_part, sep, tail = re.match(r'([^#@]+)([#@]?)(.*)', target).groups()
    path = os.path.join(game_dir, file_part)
    if not os.path.isfile(path):
        return 'absent', '파일 없음 ' + file_part
    src = _read(path)
    if sep == '':
        return ('ok' if call_re.search(src) else 'missing'), '파일 전체'
    if sep == '@':
        body = _method_body(src, tail)
        if body is None:
            return 'absent', '메서드 없음 ' + tail + '('
        return ('ok' if call_re.search(body) else 'missing'), '메서드 ' + tail + '( 본문'
    found = False
    for m in CREATE_CALL.finditer(src):
        if m.group(2) != tail:
            continue
        found = True
        head = src[max(0, m.start() - 160):m.start()].replace('\n', ' ')
        a = ASSIGN_TAIL.search(head)
        if a and shadow_on(a.group(1), call).search(src):
            return 'ok', '"%s" → %s 에 %s 호출이 닿는다' % (tail, a.group(1), word)
    if not found:
        return 'absent', '"%s" 이름으로 만드는 자리가 없다' % tail
    return 'missing', '"%s" 에 %s 호출이 안 닿는다' % (tail, word)


# ── 대조 ──────────────────────────────────────────────────────────
def run(css_path, game_dir, table, known, out=print, list_pending=False, keyline=None):
    if not os.path.isfile(css_path):
        out('✗ 정본 CSS 를 못 읽었다: %s (git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)' % css_path)
        return 2
    if keyline is None:
        keyline = keyline_selectors()
    rules = parse_rules(_read(css_path))
    problems = 0
    seen = set()
    n_none = n_key = n_off = n_ok = n_known = 0
    pending = []
    known_now_ok = []
    checked = set()
    for line, sel, val in rules:
        seen.add(sel)
        if val == 'none':
            n_none += 1
            continue
        if sel in keyline:
            n_key += 1
            continue
        if sel not in table:
            pending.append((line, sel, val))
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
            tag = ('링 없음' if t.startswith('ring:') else '글로우 없음' if t.startswith('glow:') else '그림자 없음') if state == 'missing' else '자리 없음'
            if t in known:
                n_known += 1
                out('· KNOWN(%s)  %s  ← style.css %d %s { text-shadow: %s }  — %s' % (tag, t, line, sel[:70], val[:60], known[t]))
            else:
                problems += 1
                out('✗ %s  %s  ← style.css %d  %s  { text-shadow: %s }  — %s' % (tag, t, line, sel[:70], val[:60], why))
    for sel in table:
        if sel not in seen:
            problems += 1
            out('✗ 표에는 있는데 정본에 없는 선택자: %s  → 정본이 바뀌었다 · TABLE 에서 지우거나 고쳐라' % sel[:90])
    for t in sorted(set(known_now_ok)):
        out('· KNOWN 인데 이제 그림자가 있다: %s  → KNOWN 에서 지워라' % t)
    pend_sel = sorted(set(p[1] for p in pending))
    if list_pending:
        for line, sel, val in pending:
            out('· 미정  style.css %5d  %-64s %s' % (line, sel[:64], val[:80]))
    out('%s check_text_shadows: 정본 text-shadow 선언 %d · none %d · 키라인 몫 %d · 끄는 규칙 %d · 자리 초록 %d · KNOWN 빈자리 %d · 미정 선택자 %d(선언 %d · --list 로 본다) · 문제 %d'
        % ('✓' if problems == 0 else '✗', len(rules), n_none, n_key, n_off, n_ok, n_known, len(pend_sel), len(pending), problems))
    return 0 if problems == 0 else 1


# ── 자기 검사(고장 주입) ──────────────────────────────────────────────────
def self_test():
    fails = []
    tmp = tempfile.mkdtemp(prefix='tshadow_')
    css = os.path.join(tmp, 'style.css')
    game = os.path.join(tmp, 'Game')
    os.makedirs(os.path.join(game, 'Ui'))

    def w(path, text):
        with open(path, 'w', encoding='utf-8') as f:
            f.write(text)

    def cs(text):
        w(os.path.join(game, 'Ui', 'Face.cs'), text)

    def go(table, known, css_text=None, list_pending=False, keyline=frozenset()):
        if css_text is not None:
            w(css, css_text)
        lines = []
        rc = run(css, game, table, known, out=lines.append, list_pending=list_pending, keyline=keyline)
        return rc, '\n'.join(lines)

    def expect(name, cond, detail=''):
        if not cond:
            fails.append(name + (' — ' + detail if detail else ''))

    base_css = ('/* .fake { text-shadow: 0 1px 0 #000; } */\n'
                '.s-a { color: #fff; text-shadow: 0 1px 1px rgba(4,18,52,.62); }\n'
                '.s-b, .s-c { text-shadow: 0 1px 0 rgba(255,255,255,.92); }\n'
                '.s-d { text-shadow: none; }\n'
                '.s-e { text-shadow: -1px 0 #000, 1px 0 #000; }\n'
                '.s-f { box-shadow: 0 1px 0 #000; }\n')
    # ⓐ 파싱 — 주석 속 제외 · none 포함 · box-shadow 제외 · 쉼표 선택자 통째 · 줄 번호
    rules = parse_rules(base_css)
    expect('ⓐ 선언 수', len(rules) == 4, str(len(rules)))
    expect('ⓐ 주석 제외', all(r[1] != '.fake' for r in rules))
    expect('ⓐ box-shadow 제외', all(r[1] != '.s-f' for r in rules))
    expect('ⓐ 쉼표 선택자 통째', any(r[1] == '.s-b, .s-c' for r in rules))
    expect('ⓐ 줄 번호', rules[0][0] == 2, str(rules[0][0]))
    expect('ⓐ 값 정규화', rules[0][2] == '0 1px 1px rgba(4,18,52,.62)', rules[0][2])
    # ⓑ 자리에 그림자가 있다 — @메서드 · #이름(변수에 닿음) · 파일 전체
    cs('class Face { static void B(Transform p){ TextMeshProUGUI a = UiKit.Text(p, "s-b", TextKind.Body, "x"); UiKit.TextShadow(a, "k"); }\n'
       ' static Button Btn(Transform p){ var t = UiKit.Text(p, "label", TextKind.Button, "y"); UiKit.TextShadow(t, "btn_label"); return null; } }')
    table = {'.s-a': ['Ui/Face.cs@Btn'], '.s-b, .s-c': ['Ui/Face.cs#s-b'], '.s-e': ['Ui/Face.cs']}
    rc, out = go(table, {}, base_css)
    expect('ⓑ rc 0', rc == 0, out)
    expect('ⓑ 자리 초록 3', '자리 초록 3' in out, out)
    expect('ⓑ none 1', '· none 1 ·' in out, out)
    # ⓒ 키라인 몫은 뺀다 · 표에 없는 선택자는 미정(막지 않는다 · --list)
    rc, out = go({'.s-a': ['Ui/Face.cs@Btn']}, {}, list_pending=True, keyline=frozenset(['.s-e']))
    expect('ⓒ 미정 rc 0', rc == 0, out)
    expect('ⓒ 키라인 몫 1', '키라인 몫 1' in out, out)
    expect('ⓒ 미정 선택자 1', '미정 선택자 1(선언 1' in out, out)
    expect('ⓒ --list 줄', '· 미정  style.css' in out and '.s-b, .s-c' in out, out)
    # ⓓ 표의 자리에 그림자가 없다 → rc 1 · KNOWN 이면 rc 0 · KNOWN 닫힘 알림
    cs('class Face { static Button Btn(Transform p){ var t = UiKit.Text(p, "label", TextKind.Button, "y"); return null; } }')
    rc, out = go({'.s-a': ['Ui/Face.cs@Btn']}, {})
    expect('ⓓ 그림자 없음 rc 1', rc == 1 and '그림자 없음' in out, out)
    rc, out = go({'.s-a': ['Ui/Face.cs@Btn']}, {'Ui/Face.cs@Btn': 'T999 몫'})
    expect('ⓓ KNOWN rc 0', rc == 0 and 'KNOWN(그림자 없음)' in out, out)
    cs('class Face { static Button Btn(Transform p){ var t = UiKit.Text(p, "label", TextKind.Button, "y"); UiKit.TextShadow(t, "k"); return null; } }')
    rc, out = go({'.s-a': ['Ui/Face.cs@Btn']}, {'Ui/Face.cs@Btn': 'T999 몫'})
    expect('ⓓ KNOWN 닫힘 알림', rc == 0 and 'KNOWN 인데 이제 그림자가 있다' in out, out)
    # ⓔ 자리 없음(이름·메서드·파일) → rc 1
    rc, out = go({'.s-a': ['Ui/Face.cs#nope'], '.s-b, .s-c': ['Ui/Face.cs@Nope'], '.s-e': ['Ui/Gone.cs']}, {})
    expect('ⓔ 자리 없음 rc 1', rc == 1 and out.count('자리 없음') == 3, out)
    # ⓕ 표에는 있는데 정본에 없는 선택자 → rc 1
    rc, out = go({'.s-a': ['Ui/Face.cs@Btn'], '.s-zzz': ['Ui/Face.cs@Btn']}, {})
    expect('ⓕ 정본에 없는 선택자 rc 1', rc == 1 and '정본에 없는 선택자' in out, out)
    # ⓖ 끄는 규칙('—') 은 대조하지 않는다
    rc, out = go({'.s-a': ['Ui/Face.cs@Btn'], '.s-e': '—클론에 자리 없음'}, {})
    expect('ⓖ 끄는 규칙', rc == 0 and '끄는 규칙 1' in out and '자리 초록 1' in out, out)
    # ⓗ CSS 없음 → rc 2
    rc = run(os.path.join(tmp, 'none.css'), game, {}, {}, out=lambda s: None, keyline=frozenset())
    expect('ⓗ CSS 없음 rc 2', rc == 2)
    # ⓘ 진짜 표의 자리 문법 · 진짜 키라인 표를 읽는다
    for sel, ts in TABLE.items():
        if isinstance(ts, str):
            expect('ⓘ 끄는 규칙 문법 ' + sel, ts.startswith('—'))
            continue
        for t in ts:
            expect('ⓘ 자리 문법 ' + t, re.match(r'^(ring:|glow:)?[\w/]+\.cs([#@][\w-]+)?$', t) is not None)
    expect('ⓘ check_keyline TABLE 을 읽는다', len(keyline_selectors()) >= 40, str(len(keyline_selectors())))
    # ⓙ ring: 갈래 — 링이 있으면 초록 · 언더레이만 있으면 «링 없음» · 링만 있는 자리를 언더레이로 재면 «그림자 없음»
    cs('class Face { static void R(Transform p){ var a = UiKit.Text(p, "s-e", TextKind.Body, "x"); UiKit.OutlinePx(a, "pp_line", 4f); }\n'
       ' static void S(Transform p){ var b = UiKit.Text(p, "s-b", TextKind.Body, "y"); UiKit.TextShadow(b, "k"); } }')
    rc, out = go({'.s-e': ['ring:Ui/Face.cs#s-e']}, {}, base_css)
    expect('ⓙ 링 초록', rc == 0 and '자리 초록 1' in out, out)
    rc, out = go({'.s-b, .s-c': ['ring:Ui/Face.cs#s-b']}, {})
    expect('ⓙ 언더레이만 있으면 링 없음', rc == 1 and '링 없음' in out, out)
    rc, out = go({'.s-e': ['Ui/Face.cs#s-e']}, {})
    expect('ⓙ 링만 있으면 그림자 없음', rc == 1 and '그림자 없음' in out, out)
    # ⓚ 옛 갈래 `UiKit.Outline(width01)` 도 링으로 센다(Hud 의 #stage-label 이 그것을 쓴다)
    cs('class Face { static void R(Transform p){ var a = UiKit.Text(p, "s-e", TextKind.Body, "x"); UiKit.Outline(a, "stage_outline", .25f); } }')
    rc, out = go({'.s-e': ['ring:Ui/Face.cs#s-e']}, {})
    expect('ⓚ 옛 Outline 갈래', rc == 0 and '자리 초록 1' in out, out)
    # ⓝ 도우미 래퍼도 센다(T333 11회차) — PopupKit.Ring 은 UiKit.Outline 한 줄 래퍼다
    cs('class Face { static void W(Transform p){ var a = UiKit.Text(p, "s-e", TextKind.Body, "x"); PopupKit.Ring(a, "pp_line", 0.25f); } }')
    rc, out = go({'.s-e': ['ring:Ui/Face.cs#s-e']}, {}, base_css)
    expect('ⓝ 래퍼 링 초록', rc == 0 and '자리 초록 1' in out, out)
    rc, out = go({'.s-e': ['Ui/Face.cs#s-e']}, {})
    expect('ⓝ 래퍼 링을 그림자로 재면 빨강', rc == 1 and '그림자 없음' in out, out)
    # ⓛ glow: 갈래(T333 10회차) — 공유 재질에 굽는 `DmgGlowUi.Apply` 를 센다 · 언더레이 호출만 있으면 «글로우 없음»
    os.makedirs(os.path.join(game, 'Battle'), exist_ok=True)
    w(os.path.join(game, 'Battle', 'Num.cs'),
      'class Num { static Material M(TMP_Text t, string k, string g){ var m = new Material(t.fontSharedMaterial);\n'
      '   if (g != null) DmgGlowUi.Apply(m, t.font, t.fontSize, g); return m; }\n'
      ' static Material N(TMP_Text t){ var m = new Material(t.fontSharedMaterial); return m; } }')
    rc, out = go({'.s-a': ['glow:Battle/Num.cs@M']}, {}, base_css)
    expect('ⓛ 글로우 초록', rc == 0 and '자리 초록 1' in out, out)
    rc, out = go({'.s-a': ['glow:Battle/Num.cs@N']}, {})
    expect('ⓛ 글로우 없는 메서드', rc == 1 and '글로우 없음' in out, out)
    rc, out = go({'.s-a': ['Battle/Num.cs@M']}, {})
    expect('ⓛ 글로우만 있으면 그림자 없음', rc == 1 and '그림자 없음' in out, out)
    # ⓜ @keyframes 안의 선언은 애니메이션 이름을 달고 센다(같은 `0%` 가 여럿이다)
    kf_css = ('@keyframes ani-a { 0% { text-shadow: 0 0 3px #fff; } 9% { text-shadow: 0 0 9px #f60; } }\n'
              '@keyframes ani-b { 0% { transform: none; text-shadow: 0 0 4px #fff; } }\n'
              '.s-a { text-shadow: 0 1px 1px #000; }\n')
    kf = parse_rules(kf_css)
    sels = [r[1] for r in kf]
    expect('ⓜ 키프레임 이름', sels[:3] == ['@keyframes ani-a 0%', '@keyframes ani-a 9%', '@keyframes ani-b 0%'], str(sels))
    expect('ⓜ 바깥 규칙은 그대로', sels[-1] == '.s-a', str(sels))
    rc, out = go({'@keyframes ani-a 0%': ['glow:Battle/Num.cs@M'], '@keyframes ani-a 9%': ['glow:Battle/Num.cs@M'],
                  '@keyframes ani-b 0%': ['glow:Battle/Num.cs@M'], '.s-a': ['glow:Battle/Num.cs@M']}, {}, kf_css)
    expect('ⓜ 키프레임 자리도 잰다', rc == 0 and '미정 선택자 0' in out, out)
    if fails:
        print('✗ check_text_shadows --self-test 실패 %d' % len(fails))
        for f in fails:
            print('  · ' + f[:400])
        return 1
    print('✓ check_text_shadows --self-test 31칸 통과')
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
        print('사용: check_text_shadows.py [--css <style.css>] [--game <Assets/Scripts/Game>] [--list] [--self-test]')
        return 2
    return run(css, game, TABLE, KNOWN, list_pending=list_pending)


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
