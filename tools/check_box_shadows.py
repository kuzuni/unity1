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
ⓓ 그 알림 중 **«이 축이 볼 자리가 아니다» 거나 «다른 길로 이미 선»** 것은 `ELSEWHERE` 에 까닭과 함께 적어 둔다(26회차).
   그래야 남은 ⚠ 목록이 곧 **할 일 목록**이 된다 — 적어 둔 줄이 정본에서 사라지면 그것도 알린다(낡음 막기).

빨강이 되는 때
-------------
· 표의 자리가 «섰다» 인데 그 호출이 클론에 없다(되돌아간 자리).
· `KNOWN` 에 적힌 자리가 실제로는 서 있다(적어 두고 안 지운 자리).
25회차에 표의 열두 자리가 **전부 섰다**(`KNOWN` 이 비었다) — 할 일 목록은 이제 맨 아래 ⚠ «아직 안 본 자리» 줄이다(26회차 기준 16).
"""
import os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CSS = os.path.join(ROOT, '.wwwww-src', 'web', 'css', 'style.css')
GAME = os.path.join(ROOT, 'Assets', 'Scripts', 'Game')

# 클론이 그림자를 거는 유일한 길(2회차가 세운다). 주석이 아니라 이 호출을 센다.
# 34회차 — 거는 길이 둘이다: 바로 거는 `Drop` 과 **크기가 잡히는 첫 프레임까지 미루는** `DropWhenSized`.
# 뒤엣것을 안 세면 «호출이 0» 이라고 거짓말을 한다(공용 카드 공장이 그 길을 쓴다).
CALL = re.compile(r'UiShadow\.Drop(?:WhenSized)?\s*\(\s*[^,]+,\s*"([^"]+)"')

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
    ('passcard_drop', '.modal-card.pass-card',         8602, 'blur'),
    ('leaguefoot_up', '.league-foot',                  2376, 'blur'),
    ('eqswfly_drop',  '.eqsw-fly-box',                 None, 'blur'),
    ('equipcell_drop', '.equip-cell:not(.egg-cell)',   8539, 'blur'),   # 36회차 — 같은 선언의 첫 바깥 겹(0 0 .46rem -.1rem color-mix 62%)은 빛 갈래(T419) · T371 KNOWN 의 «T331 뒤» 는 그 겹을 뜻한다(7738 의 앞 선언은 8539 가 덮는다)
    ('pettile_drop',   '.pet-tile .tile-face',           8131, 'blur'),   # 38회차 — 첫 바깥 겹(0 0 .5rem -.08rem color-mix 60%)은 빛 갈래(T419) · 4262·4288 의 앞 선언은 8124 가 덮는다
    ('mountcell_drop', '.equip-cell.egg-cell',           8081, 'blur'),   # 39회차 — 탄 탈것 갈래만(빈 칸은 8548 .equip-cell.empty 가 inset 뿐으로 덮는다)
    ('afstart_drop',   '.af-start',                     5019, 'blur'),   # 40회차 — 남색 rgba(20,60,140,.4) · inset 둘은 턱·림라이트(PopupKit.Btn lip · T178 면)
    ('afagebar_lip',   '.af-age-bar',                   4828, 'hard'),   # 40회차 — 같은 선언의 바깥 두 겹 중 첫(딱딱한 턱) · 둘째는 afagebar_drop
    ('afagebar_drop',  '.af-age-bar',                   4828, 'blur'),   # 40회차 — 둘째 겹(앰비언트) · 막대 뿌리의 형제(무늬 층 = 1)를 안 흔들려고 둘 다 첫 자식 «bar» 틀 안에
    ('ratebar_drop',   '.rate-bar',                     8584, 'blur'),   # 41회차 — 확률 팝업 등급 막대(SkillRatesPopup · Framed 상자 맨 뒤)
    ('shopcard_drop',  '.shop-deal-card, .shop-gem-card', 8230, 'blur'),   # 41회차 — 특가 카드 + 보석 카드(ShopSheet 두 공장 · NEED 2)
    ('srok_drop',      '.sr-ok',                        7144, 'blur'),   # 43회차 — 소환 결과 [확인] 버튼(SkillSummonResult · 뿌리 맨 뒤)
    # 26회차 — 자동 제련 팝업 한 파일에 몰린 둘(손잡이 `.af-toggle .knob` 은 공장이 `Popups.cs` 라 남의 lock 뒤다)
    ('afspinner_lip', '.af-spinner',                   5007, 'hard'),
    ('afsubrow_drop', '.af-sub-row',                   4999, 'blur'),
    ('rwanchor_drop', '.rw-anchor',                   7540, 'blur'),   # 같은 선언의 첫 겹은 빛 갈래다(26회차)
    ('modalcard_cast', '.modal-card:not(.sheet):not(.pass-card)', 8596, 'blur'),   # 34회차 — 32회차가 찾아낸 여러 줄 선언
    # 29회차 — 남은 자리의 **값을 먼저 재 뒀다**(배선은 그 파일의 lock 이 풀리는 회차가 한 줄로 건다 · KNOWN 참고)
    ('fiagebar_lip',  '.fi-age-bar',                   5132, 'hard'),
    ('techbranch_drop', '.tech-branch-icon::before',   2112, 'blur'),
    ('skribbon_drop', '.sk-ribbon',                    4079, 'blur'),
    ('afknob_drop',   '.af-toggle .knob',              4991, 'blur'),
    ('sragain_drop',  '.sr-again',                     5796, 'blur'),
    ('srnew_drop',    '.sr-new',                       6985, 'blur'),
    ('srqty_drop',    '.sr-qty',                       7008, 'blur'),
    ('srdup_drop',    '.sr-dup',                       7029, 'blur'),
    ('srrk_drop',     '.sr-sub .sr-rk',                7075, 'blur'),
    ('srchip_drop',   '.sr-chip',                      7128, 'blur'),
    ('infobtn_drop',  '.info-btn, #equip-sheet .anvil-side .info-btn', 8174, 'blur'),
]

# 아직 안 선 자리 — 까닭과 «누가/언제» 를 같이 적는다. 서면 이 줄을 지운다(안 지우면 자가 알린다).
# 30회차 — **클론 공장이 여럿인 자리**: 정본 선택자 하나를 클론이 두 곳에서 만들면 한 곳만 걸어도
# «호출이 있다» 가 된다(그러면 화면의 절반은 그늘이 없는데 자가 조용하다). 걸려야 하는 곳 수를 적어 둔다.
NEED = {
    'infobtn_drop': 2,   # `ForgeUi.InfoButton`(대장간·장비 시트) + `DungeonPopups.InfoButton`(던전 팝업)
    'equipcell_drop': 3,   # 36·37회차 — 셋째(목록 타일)는 ForgeUi.ItemTile(T415 lock)을 안 열고 부르는 쪽 ForgeInfoPopup.Cell 에서 건다(37회차) · — `ForgeSheet.EquipCell`(장비 시트) + `PlayerInfoPopup.EquipCell`(플레이어 정보 격자) + `ForgeUi.ItemTile`(목록 타일 `.fl-face.equip-cell` · ui.js 2090 «타일은 .equip-cell CSS 를 그대로 입는다»)
    'pettile_drop': 3,   # 38회차 — 정본 `.pet-tile` 격자 셋(ui.js 3938 펫 · 4152 업그레이드 재료 · 5646 탈것) = 클론 `PetPanel.PetTileAt`·`PetUpgradePopup` 재료 격자·`MountSheet` 격자(공장 TileFace 는 상세 타일도 만들어 부르는 쪽에서 건다)
    'mountcell_drop': 2,   # 39회차 — `ForgeSheet.MountCell`(장비 시트) + `PlayerInfoPopup.MountWide`(플레이어 정보 와이드 칸 · 같은 egg-cell 마크업)
    'shopcard_drop': 2,   # 41회차 — `ShopSheet` 특가 카드(deal-*/card) + 보석 카드(gem-*) — 정본이 한 선언에 두 선택자를 적었다
    'fiagebar_lip': 2,   # 42회차 — `ForgeInfoPopup` 확률 정보 행(age-*) + 목록 머리(head · fl-head) — 공장 ForgeUi.AgeBar 밖 부르는 쪽 둘
}

KNOWN = {
    # 29회차 — **값은 재 뒀고 배선만 남은** 자리들. 까닭은 전부 같다: 그 파일을 «범위» 로 쥔 **산 lock** 이 있다.
    # 그 lock 이 풀리는 회차가 `UiShadow.Drop(<상자>, "<키>", <반지름>)` 한 줄을 걸고 여기서 그 줄을 지운다.
}

# 표 밖 자리의 **장부**(26회차) — «이 축이 볼 자리가 아니다» 거나 «다른 길로 이미 섰다» 는 것을
# 까닭과 함께 적어 둔다. 여기 적힌 선택자는 아래 ⚠ «아직 안 본 자리» 목록에서 빠진다.
#
# ⚑ 26회차가 눈으로 가른 것: 자는 여태 **발광(빛)과 그림자를 한 자루에 담아** 세고 있었다.
#   `0 0 <반지름> <색>`(치우침 0 · 색이 검정이 아니다)은 «떠 있음» 이 아니라 **빛나는 것**이라
#   T331(«바깥 그림자가 클론에 0»)의 축이 아니다 — 그 갈래는 발광·합성 축(T419)이 본다.
#   그리고 `<x> 0 0 0 <색>`(흐림도 번짐도 0 · 가로 치우침만)은 그림자가 아니라 **쌓인 더미의 테두리**다.
ELSEWHERE = {
    # ⓐ 빛이지 그림자가 아니다(치우침 0 · 색이 검정이 아니다) — 발광 축
    '#app': '앱 틀 바깥 halo(0 0 3rem) — 빛 갈래',
    '.pip.now': '웨이브 핍 발광(0 0 6px #fff) — 빛 갈래',
    '.pip.boss.now': '보스 핍 발광(0 0 8px #ff5252) — 빛 갈래',
    '.pinfo-preview .pip.now': '플레이어 정보의 같은 핍 — 빛 갈래',
    '.bw-banner': '보스 경고 띠 발광(0 0 2.2rem) — 빛 갈래',
    '.skill-btn.ready': '준비된 스킬 버튼 발광(0 0 8px var(--sc)) — 빛 갈래',
    '.skill-btn.auto.on': '자동 켜진 스킬 버튼 발광 — 빛 갈래',
    '.sk-mini': '작은 스킬 칸의 등급 발광(0 0 .5rem -.14rem) — 빛 갈래',
    '.af-toggle.on': '켜진 토글의 초록 발광 — 빛 갈래(같은 줄의 inset 은 T163 관용구)',
    '.hatch-lamp::after': '부화장 등불 발광 두 겹 — 빛 갈래',
    # ⓑ 그림자가 아니라 «쌓인 더미의 테두리»(흐림 0 · 번짐 0 · 가로 치우침만) — T178 축
    '.anvil-btn.held-slot.deck-2::before': '더미 2장째 테두리(calc(--dg) 0 0 0) — 그림자가 아니라 겹친 카드 변 · T178 축',
    '.anvil-btn.held-slot.deck-3::before': '더미 3장째 테두리 — T178 축',
    '.anvil-btn.held-slot.deck-4::before': '더미 4장째 테두리 — T178 축',
    '.anvil-btn.held-slot.deck-5::before': '더미 5장째 테두리 — T178 축',
    # ⓒ 소환 연출 — 발광이 **구운 판 안에** 들어 있다(T334 가 세운 겹 · UiShadow 를 안 쓴다)
    '.sr-streaks i': '수렴 빛줄기의 발광은 BakeStreak 의 glow 스톱에 구워져 있다(T334 22회차)',
    '.sr-shock': '예고 충격파의 발광은 구운 단계 판에 들어 있다(T334 19회차)',
    '.sr-shock.echo': '잔파도 같은 판 갈래(T334 19회차)',
    '.sr-tierflash': '챕터 링의 발광 꼬리는 BakeTierRing 의 glowF(T334 17회차)',
    '.sr-idle i': '끝난 뒤 고리의 halo 도 같은 굽기(T334 18회차)',
    '.sr-orbwrap': '광원 둘레 발광은 등급색 광원 판에 구워져 있다(T334 3회차 충전 3종)',
    '.sr-canopy::before': '바닥 스필 — 지금 T419 가 쥔 자리(mix-blend-mode: screen 갈래)',
    # 29회차 — 같은 그늘에 **등급 발광만 더한** 변형(그늘은 `.sr-chip` 키 하나로 선다)
    # 38회차 — 같은 그늘에 **선택 링만 더한** 변형(링 `0 0 0 .16rem var(--pp-blue)` 은 번짐만 있는 테두리라 그림자가 아니다)
    '.pet-tile.selected .tile-face': '`.pet-tile .tile-face` 와 같은 그늘 `0 .16rem .3rem rgba(0,0,0,.22)` + 파란 선택 링 — 그늘은 pettile_drop 이 쥔다 · 링은 선택 표시 갈래',
    # 41회차 — 그림자가 아닌 것 둘.
    '.sr-spark': '소환 결과 스파클 — 흐림 0 · 번짐 음수 · 흰/등급색 점을 여덟 방향 오프셋으로 찍은 **점 무리 그림**(6477 · mix-blend-mode: screen)이지 그늘이 아니다 · T334 소환 연출 갈래',
    '.modal-card:not(.sheet):not(.pass-card):not(.lgr-card)': '7783 의 `0 .5rem 0 rgba(0,0,0,.25)` 는 3518 `.modal-card` 의 턱을 같은 값으로 다시 적은 것(나머지는 inset) — 그늘은 card_lip 이 쥔다 · 8596 과 같은 갈래(결정 719)',
    # 43회차 — 소환 결과의 나머지 넷.
    '.sr-orb': '구슬 아래 드리운 그림자 `0 .5rem .8rem -.15rem rgba(0,0,0,.7)` 는 클론이 **구운 타원**(`sr-orbwrap/shadow` · PetSkillKit.Disc 검정 .5 · 폭 74%·높이 13%)으로 이미 그린다 — 다른 길로 선 자리 · 그 타원의 수(.74·.13·.02)가 코드에 박혀 있는 것은 §1 몫(표로 옮기는 회차가 따로)',
    '.sr-cell[data-mat="metal"] .sr-orb': '같은 그늘 값(6559) — `.sr-orb` 와 같은 타원이 쥔다',
    '.sr-cell[data-mat="glass"] .sr-orb': '같은 그늘 값(6597) — `.sr-orb` 와 같은 타원이 쥔다',
    '.sr-floor::before': '소환진 아래 스필 `0 1.2rem 6rem 2rem var(--floor-fill, rgba(96,146,255,.2))` — 치우침은 있지만 색이 검정이 아니라 **바닥 채움색**(밝은 파랑 .2 · 6rem 번짐 · 2rem 퍼짐)이라 빛·스필 갈래(T419 · `.sr-canopy::before` 와 같은 자리)',
    '.sr-chip[data-tier="4"]': '`.sr-chip` 과 같은 두 겹 + 등급 발광 `0 0 .5rem var(--cb)` — 그늘은 srchip_drop 이 쥔다',
    '.sr-chip[data-tier="5"]': '`.sr-chip` 과 같은 두 겹 + 등급 발광 `0 0 .7rem var(--cb)` — 그늘은 srchip_drop 이 쥔다',
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
        # 32회차 — 정본은 겹이 많은 자리를 **여러 줄로** 적는다(`box-shadow:` 다음 줄부터 값이 온다).
        #   한 줄만 읽으면 그 선언이 통째로 사라진다 — `;` 까지 이어 읽는다.
        if ';' not in l:
            j = i + 1
            while j < len(lines) and ';' not in lines[j]:
                val += ' ' + lines[j].strip(); j += 1
            if j < len(lines): val += ' ' + lines[j].split(';')[0].strip()
        val = val.strip()
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
    bad, stale, half, standing = [], [], [], 0
    for key, sel, ln, kind in SPOTS:
        where = live.get(key)
        n, need = len(where or []), NEED.get(key, 1)
        if n >= need and key in KNOWN:
            stale.append((key, sel, where[0]))
        elif n >= need:
            standing += 1
        elif n > 0 and key in KNOWN:
            half.append((key, sel, n, need))          # 적어 둔 대로 아직 안 선 자리인데 **절반은 섰다**
        elif n > 0:
            bad.append((key, sel, ln, kind + ' · %d/%d 만 걸렸다(공장이 %d 곳이다)' % (n, need, need)))
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
    filed = sum(1 for _, s2, _, _ in unseen if s2 in ELSEWHERE)
    if filed:
        print('  · 표 밖이지만 **까닭을 적어 둔 자리** %d개는 안 센다(`ELSEWHERE` — 빛 갈래·더미 테두리·구운 판)' % filed)
    for main in ('hard', 'drop', 'glow'):
        rows = [r for r in by_kind.get(main, []) if r[1] not in ELSEWHERE]
        if not rows: continue
        label = {'hard': '딱딱한 턱', 'drop': '드리운 그림자', 'glow': '둘레 발광'}[main]
        print('⚠ 아직 안 본 %s %d개(알림 — 다음 회차가 하나씩 본다): %s'
              % (label, len(rows), ', '.join('%s(%d)' % (s2, n) for n, s2 in rows)))
    gone = [k for k in ELSEWHERE if k not in set(s2 for _, s2, _, _ in unseen)]
    for k in gone:
        print('⚠ `ELSEWHERE` 에 적힌 %s 가 정본에 없다 — 표로 옮겼거나 정본이 지웠다(그 줄을 손봐라)' % k)
    for key, sel, n, need in half:
        print('  · **절반만 선 자리**: %s(%s) %d/%d — 나머지 공장이 열리는 회차가 마저 건다(`KNOWN` 참고)' % (key, sel, n, need))
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
    open(os.path.join(d, 'B.cs'), 'w', encoding='utf-8').write(
        'class B{ void F(){ UiShadow.DropWhenSized(rt, "modalcard_cast", r); } }\n')
    chk('미루는 꼴도 센다(34회차)', 'modalcard_cast' in clone_keys(d))
    chk('주석은 안 센다', 'panel_lip' not in keys)
    chk('어느 줄인지 적는다', keys['card_lip'][0].endswith(':1'))

    # ⓔ-2 표 밖 장부(26회차) — 적어 둔 자리는 ⚠ 에서 빠지고, 낡으면 알린다
    import io as _io, contextlib as _ctx
    def _out():
        b = _io.StringIO()
        with _ctx.redirect_stdout(b): run()
        return b.getvalue()
    before = _out()
    chk('장부에 적은 자리는 ⚠ 목록에 안 뜬다', '.skill-btn.ready' not in before and '#app' not in before)
    chk('장부에 적힌 수를 찍는다', '까닭을 적어 둔 자리' in before)
    saved_el = dict(ELSEWHERE)
    try:
        ELSEWHERE.pop('.skill-btn.ready')
        chk('장부에서 빼면 다시 ⚠ 에 뜬다', '.skill-btn.ready' in _out())
        ELSEWHERE.update(saved_el)
        ELSEWHERE['.없는-선택자'] = '정본에 없다'
        chk('정본에 없는 줄을 알린다', '`ELSEWHERE` 에 적힌 .없는-선택자' in _out())
    finally:
        ELSEWHERE.clear(); ELSEWHERE.update(saved_el)
    chk('되돌린 뒤엔 다시 조용하다', '.없는-선택자' not in _out())

    # ⓔ-2.5 여러 줄로 적은 선언(32회차) — `box-shadow:` 다음 줄부터 값이 오는 꼴을 통째로 읽는가
    import tempfile as _tf, os as _os
    _d = _tf.mkdtemp()
    _css = _os.path.join(_d, 'x.css')
    open(_css, 'w', encoding='utf-8').write(
        '.one { box-shadow: 0 .5rem 0 rgba(0,0,0,.25); }\n'
        '.many {\n'
        '    box-shadow:\n'
        '        inset 0 .09rem 0 rgba(255,255,255,.34),\n'
        '        0 .12rem 0 rgba(0,0,0,.3), 0 .2rem .4rem rgba(0,0,0,.24);\n'
        '}\n')
    _got = outer_decls(_css)
    _sels = [r[1] for r in _got]
    chk('한 줄 선언을 읽는다', '.one' in _sels)
    chk('여러 줄 선언도 읽는다(32회차 구멍)', '.many' in _sels)
    _many = [r for r in _got if r[1] == '.many'][0]
    chk('여러 줄의 갈래를 다 센다', _many[3].count('inset') == 1 and 'hard' in _many[3] and 'drop' in _many[3])

    # ⓔ-3 공장이 여럿인 자리(30회차) — 한 곳만 걸리면 «절반» 이고, 적어 두지 않았으면 빨강이다
    #   ⚠ §0-6 보탬(2026-09-19 · 워커 O · 런 1232·1233 datasync 빨강): 이 칸은 «실물 `infobtn_drop` 이 절반 상태» 를 전제로 했는데
    #   42회차가 나머지 절반(ForgeUi.InfoButton)을 걸어 2/2 가 되자 전제가 깨졌다 — 실물에 기대지 말고 **절반을 주입**한다
    #   (NEED 를 실제보다 크게 · KNOWN 에 까닭을 적어) — 그러면 «절반만 선 자리» 줄이 언제나 난다.
    saved_need, saved_known = dict(NEED), dict(KNOWN)
    try:
        NEED['infobtn_drop'] = 99                 # 다 걸린 자리를 «99곳 필요 · 적어 둠» 으로 속여 «절반» 을 만든다
        KNOWN['infobtn_drop'] = '(자기 검사 ⓔ-3 · 절반 주입)'
        out = _out()
        chk('절반만 선 자리를 찍는다', '절반만 선 자리' in out and 'infobtn_drop' in out)
        chk('절반이면 KNOWN 에 남는다(rc 0)', run() == 0)
        KNOWN.pop('infobtn_drop')
        NEED['card_lip'] = 99                     # 다 걸린 자리를 «99곳 필요» 로 속여 본다(KNOWN 엔 없다)
        chk('모자라고 KNOWN 에도 없으면 빨강', run() == 1)
    finally:
        NEED.clear(); NEED.update(saved_need)
        KNOWN.clear(); KNOWN.update(saved_known)
    chk('되돌리면 다시 초록', run() == 0)

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
