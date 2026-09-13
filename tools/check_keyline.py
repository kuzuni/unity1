#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T109 — 정본 키라인(-webkit-text-stroke) 규칙 ↔ 클론 키라인 호출 대조 자.

정본 `web/css/style.css` 에서 `-webkit-text-stroke` 를 주는 규칙(선택자 · 폭)을 전부 걷고,
클론 `Assets/Scripts/Game/**/*.cs` 에서 그 자리(파일 · 요소 이름 · 도우미 메서드)에
키라인 호출(`UiKit.Outline` · `UiKit.OutlinePx` · `PetSkillKit.Stroked` · `PopupKit.Ring`)이 있는지 센다.

왜 이 자가 있나(T28 22회차 실측): 정본은 52규칙인데 클론은 «이미 부르는 자리» 만 T104 가 고친다 —
정본이 키라인을 주는데 클론이 **호출 자체를 안 한** 자리는 어떤 자도 안 막았다(리그 상대 전투력이 민주황).

선택자 ↔ 클론 자리 짝은 이 파일의 TABLE 하나가 쥔다(새 화면·새 규칙이 늘면 여기 줄을 더한다).
  "Ui/File.cs"           파일 안 어디든 키라인 호출이 하나 있으면 됨(정본 규칙이 파일 전체에 상속되는 꼴)
  "Ui/File.cs#name"      그 이름으로 만든 글자(`.Text/.Label/.Bold/.IconTextRow(parent, "name", …)`)에 키라인이 걸려야 함
                         (`Stroked(…, "name", …)` 로 만들었으면 그 자체가 키라인)
  "Ui/File.cs@Method"    도우미 메서드 본문 안에 키라인 호출이 있어야 함(버튼·알약·게이지처럼 한 곳이 여럿을 세우는 자리)
  "—<이유>"              클론에 해당 자리가 없거나 정본이 «끄는» 규칙(폭 0) — 대조하지 않는다

빠진 자리 중 «임자가 정해진 것» 은 KNOWN 에 두어 rc 0 으로 지나가게 한다(T89 `check_text_glyphs` 의 KNOWN 과 같은 규약).
T109 ⓑ(호출 붙이기 · T104 2회차 `text_keyline_px` 뒤)가 한 자리를 붙일 때마다 KNOWN 에서 그 줄을 지운다 —
붙였는데 안 지우면 «KNOWN 인데 이제 있다» 로 알린다(rc 는 그대로).

사용:  python3 tools/check_keyline.py [--css <style.css 경로>] [--game <Assets/Scripts/Game>] [--self-test]
rc:    0 = 정본 규칙 전부가 표에 있고 · 표의 자리 전부가 (키라인이 있거나 KNOWN) · 1 = 아니다 · 2 = 정본 CSS 를 못 읽었다
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
    '.ob-zzz i': ['Ui/OfflinePopup.cs#zzz'],
    '.offline-card': ['Ui/OfflinePopup.cs#coins', 'Ui/OfflinePopup.cs#hammers'],
    '.offline-total': ['Ui/OfflinePopup.cs#coins'],
    '.float-dmg': ['Battle/DamageNumbers.cs'],
    '.float-dmg.dmg-kill': ['Battle/DamageNumbers.cs'],
    '.float-dmg.dmg-hero': ['Battle/DamageNumbers.cs'],
    '.skill-btn .sk-lv': ['Ui/SkillBar.cs#sk-lv'],
    '.skill-btn.auto.on': ['Ui/SkillBar.cs#t'],
    '.equip-cell .cell-lv': ['Ui/PlayerInfoPopup.cs#t'],
    '.anvil-btn small': ['Ui/ForgeSheet.cs#count'],
    '.league-row.me .league-server': ['Ui/LeagueSheet.cs#server'],
    '.league-reward-banner': ['Ui/LeagueSheet.cs#text'],
    '.league-tier-rank.text': ['Ui/LeagueSheet.cs#label'],
    '.lgr-rank-n': ['Ui/LeagueSheet.cs#rank'],
    '.league-challenge-name small': ['Ui/LeagueSheet.cs#cp'],
    '.pass-card': ['Ui/PassPopup.cs#amt', 'Ui/PassPopup.cs#text'],
    '.shop-deal-tag': ['Ui/ShopSheet.cs#name'],
    '.shop-gem-amt': ['Ui/ShopSheet.cs#amt'],
    '.profile-tabs button.on': ['Ui/ProfilePopup.cs#label'],
    '.chat-bubble, .chat-time': ['Ui/ChatScreen.cs#text', 'Ui/ChatScreen.cs#time'],
    '.chat-share-side small:last-child': ['Ui/ChatScreen.cs#cp'],
    '.chat-share-label': ['Ui/ChatScreen.cs#label'],
    '#summon-subtabs.subtab-strip button.active': ['Ui/SkillPetSheet.cs#label'],
    ('h2.sheet-title, h3.sheet-title, h3.fi-title, h3.af-title, h3.tb-title, h3.sellwarn-title, '
     '.rates-head h3, .offline-title, .profile-title, .asc-focus-title'): [
        'Ui/SkillPanel.cs#sheet-title', 'Ui/PetPanel.cs#sheet-title', 'Ui/MountSheet.cs#sheet-title',
        'Ui/ForgeInfoPopup.cs#title', 'Ui/ForgeAutoPopup.cs#af-title', 'Ui/TechPanel.cs#title',
        'Ui/ForgeCraftPopup.cs#title', 'Ui/SkillRatesPopup.cs#rates-h3', 'Ui/OfflinePopup.cs#title',
        'Ui/ProfilePopup.cs#title', 'Ui/AscendPopup.cs#title'],
    '.summon-sub .sheet-head .cur-pill': ['Ui/PetSkillKit.cs@Pill'],
    '#panel-skills .sk-grid .sk-lv': ['Ui/SkillPanel.cs#sk-lv'],
    '.mount-pill-row .cur-pill.winder': ['Ui/PetSkillKit.cs@Pill'],
    '.hatch-cell .hatch-time': ['Ui/PetPanel.cs#hatch-time'],
    '.slot-buy-label': ['Ui/PetPanel.cs#slot-buy-label'],
    '.rates-prog span': ['Ui/PetSkillKit.cs@Gauge'],
    '.af-start': ['Ui/Popups.cs@Btn'],
    '.af-title': ['Ui/ForgeAutoPopup.cs#af-title'],
    '.fi-card .fi-skip': ['Ui/Popups.cs@Btn'],
    '.dgd-reward-pill': ['Ui/DungeonDetailPopup.cs'],
    '.dgd-keys': ['Ui/DungeonDetailPopup.cs'],
    '.dgd-btn.silver': ['Ui/Popups.cs@Btn'],
    '.petd-wrap .petd-btn': ['Ui/PetSkillKit.cs@PaperButton'],
    '.petd-wrap .petd-name': ['Ui/PetPanel.cs#petd-name', 'Ui/MountSheet.cs#petd-name'],
    '.petd-wrap .petd-tile .sk-lv': ['Ui/PetPanel.cs#sk-lv'],
    '.petup-xpbar span': ['Ui/PetSkillKit.cs@Gauge'],
    '.petup-selrow .btn.silver': ['Ui/PetSkillKit.cs@PaperButton'],
    '.petup-selrow .btn.silver.disabled': '—정본이 끄는 규칙(폭 0)',
    '.petup-panel .idet-name': ['Ui/PetUpgradePopup.cs#idet-name'],
    '.rw-amt': ['Ui/DungeonSheet.cs#rw-amt'],
    '.rw-tick': ['Ui/DungeonSheet.cs#rw-tick'],
    '#player-info-modal .pinfo-id-text .cp': ['Ui/PlayerInfoPopup.cs#cp'],
    '.league-row .league-name, .league-row .league-rank': ['Ui/LeagueSheet.cs#name', 'Ui/LeagueSheet.cs#rank'],
    '.league-row .league-name small': ['Ui/LeagueSheet.cs#server'],
    '.btn.btn.summon-btn.summon-btn:not(.ascend-ready)': ['Ui/PetSkillKit.cs@PaperButton'],
    ('.btn.btn.primary.primary, .btn.btn.on.on, .btn.btn.equip.equip, .btn.btn.danger.danger, '
     '.btn.btn.sell.sell'): ['Ui/Popups.cs@Btn'],
    ('.btn.btn.primary.primary.disabled, .btn.btn.on.on.disabled, .btn.btn.equip.equip.disabled, '
     '.btn.btn.danger.danger.disabled, .btn.btn.sell.sell.disabled'): '—정본이 끄는 규칙(폭 0)',
}

# ── 임자가 정해진 빈자리(자리 → 이유) — T109 ⓑ 가 붙일 때마다 지운다 ──────────────────────────
KNOWN = {
    'Ui/OfflinePopup.cs#zzz': 'T109 ⓑ — 잠자는 z 글자 자리가 클론에 없다(T68 이 머리를 세울 때 안 옮김) · 세우면서 키라인',
    'Ui/OfflinePopup.cs#coins': 'T109 ⓑ — .offline-card .11em / .offline-total .2em',
    'Ui/OfflinePopup.cs#hammers': 'T109 ⓑ — .offline-card .11em',
    'Battle/DamageNumbers.cs': 'T109 ⓑ — .float-dmg .6px / kill 1px / hero .55px(색 셋) · 데미지 숫자는 Ui 밖 Battle 에 산다',
    'Ui/SkillBar.cs#sk-lv': 'T109 ⓑ — 슬롯 Lv 라벨(.skill-btn .sk-lv 2px)이 클론에 없다(SkillBar 에 lv 이름 글자 0) · 세우면서 키라인',
    'Ui/PlayerInfoPopup.cs#t': 'T109 ⓑ — 장비 칸 Lv(.equip-cell .cell-lv .3px #fff)',
    'Ui/LeagueSheet.cs#server': 'T109 ⓑ — .league-row.me .league-server max(1.2px,.1em) · .league-name small max(1.4px,.1em)',
    'Ui/LeagueSheet.cs#rank': 'T109 ⓑ — .lgr-rank-n .16rem · .league-row .league-rank 2px',
    'Ui/LeagueSheet.cs#name': 'T109 ⓑ — .league-row .league-name 2px',
    'Ui/LeagueSheet.cs#cp': 'T109 ⓑ — .league-challenge-name small 2px(T28 22회차가 잡은 바로 그 자리 · 민주황)',
    'Ui/ChatScreen.cs#text': 'T109 ⓑ — .chat-bubble .5px currentColor(T99 lock 뒤)',
    'Ui/ChatScreen.cs#time': 'T109 ⓑ — .chat-time .5px currentColor(T99 lock 뒤)',
    'Ui/ChatScreen.cs#cp': 'T109 ⓑ — .chat-share-side small:last-child var(--ol2)(T99 lock 뒤)',
    'Ui/ChatScreen.cs#label': 'T109 ⓑ — .chat-share-label var(--ol2)(T99 lock 뒤)',
    'Ui/ForgeInfoPopup.cs#title': 'T109 ⓑ — h3.fi-title .11em(T99 lock 뒤)',
    'Ui/ForgeAutoPopup.cs#af-title': 'T109 ⓑ — h3.af-title .11em · .af-title 4px #fff',
    'Ui/TechPanel.cs#title': 'T109 ⓑ — h3.tb-title .11em(DungeonPopups.Bold 은 키라인을 안 건다)',
    'Ui/ForgeCraftPopup.cs#title': 'T109 ⓑ — h3.sellwarn-title .11em(T99 lock 뒤)',
    'Ui/AscendPopup.cs#title': 'T109 ⓑ — .asc-focus-title .11em(DungeonPopups.Bold 은 키라인을 안 건다)',
    'Ui/Popups.cs@Btn': 'T109 ⓑ — .btn.btn.primary… var(--ol2) · .af-start 4px · .fi-skip 4px · .dgd-btn.silver 2px: PopupKit.Btn 라벨이 민글자',
    'Ui/PetPanel.cs#sk-lv': 'T109 ⓑ — .petd-tile .sk-lv 2.5px: 펫 미니 타일의 Lv 글자가 클론에 없다(PetPanel 에 lv 이름 글자 0) · 세우면서 키라인',
    'Ui/DungeonSheet.cs#rw-amt': 'T109 ⓑ — .rw-amt 4px #2a2018: 보상 날림의 획득량 글자가 클론에 없다(DungeonSheet 는 아이콘만 날린다) · 세우면서 키라인',
    'Ui/DungeonSheet.cs#rw-tick': 'T109 ⓑ — .rw-tick 3.5px #2a2018: 보상 날림의 체크 글자가 클론에 없다 · 세우면서 키라인',
    'Ui/PlayerInfoPopup.cs#cp': 'T109 ⓑ — #player-info-modal .pinfo-id-text .cp 2px(IconTextRow 글자 칸)',
}

KEYLINE_CALL = re.compile(r'\b(?:UiKit\.Outline|UiKit\.OutlinePx|PetSkillKit\.Stroked|PopupKit\.Ring|Stroked|Ring)\s*\(')
CREATE_CALL = re.compile(r'\.(Text|Label|Bold|Stroked|IconTextRow)\s*\(\s*[^,()]+,\s*"([^"]+)"')
ASSIGN_TAIL = re.compile(r'([\w\[\]\.]+)\s*=\s*(?:[\w!.()\[\]]+\s*\?\s*)?[\w.]*$')  # «nm = UiKit» · «st = on ? PetSkillKit» 꼬리
STROKE_DECL = re.compile(r'-webkit-text-stroke(?:-width)?\s*:\s*([^;]+);')


# ── 정본 CSS ──────────────────────────────────────────────────────────────────────────────
def _blank_comments(css):
    """주석을 같은 길이의 공백/줄바꿈으로 바꿔 줄 번호를 지킨다."""
    def rep(m):
        s = m.group(0)
        return ''.join('\n' if ch == '\n' else ' ' for ch in s)
    return re.sub(r'/\*.*?\*/', rep, css, flags=re.S)


def parse_rules(css_text):
    """[(줄, 선택자, 폭)] — -webkit-text-stroke 를 주는 규칙 전부(폭 0 = 끄는 규칙도 포함)."""
    css = _blank_comments(css_text)
    out = []
    for m in re.finditer(r'([^{}]+)\{([^{}]*)\}', css):
        w = STROKE_DECL.search(m.group(2))
        if not w:
            continue
        sel = ' '.join(m.group(1).split())
        # 선택자 첫 글자의 줄 번호
        lead = len(m.group(1)) - len(m.group(1).lstrip())
        line = css[:m.start(1) + lead].count('\n') + 1
        out.append((line, sel, w.group(1).strip()))
    return out


# ── 클론 ──────────────────────────────────────────────────────────────────────────────────
def _read(path):
    with open(path, encoding='utf-8') as f:
        return f.read()


def _method_body(src, name):
    m = re.search(r'\bstatic\s+[\w<>\[\],\s]+\s' + re.escape(name) + r'\s*\(', src)
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
    """(상태, 설명) — 상태: 'ok' | 'missing'(요소는 있는데 키라인 없음) | 'absent'(요소·메서드·파일 없음)."""
    file_part, sep, tail = re.match(r'([^#@]+)([#@]?)(.*)', target).groups()
    path = os.path.join(game_dir, file_part)
    if not os.path.isfile(path):
        return 'absent', '파일 없음 ' + file_part
    src = _read(path)
    if sep == '':
        return ('ok' if KEYLINE_CALL.search(src) else 'missing'), '파일 전체'
    if sep == '@':
        body = _method_body(src, tail)
        if body is None:
            return 'absent', '메서드 없음 ' + tail + '('
        return ('ok' if KEYLINE_CALL.search(body) else 'missing'), '메서드 ' + tail + '( 본문'
    # '#name' — 그 이름으로 만든 글자
    found = False
    for m in CREATE_CALL.finditer(src):
        if m.group(2) != tail:
            continue
        found = True
        if m.group(1) == 'Stroked':
            return 'ok', '"%s" 를 Stroked 로 만든다' % tail
        head = src[max(0, m.start() - 160):m.start()].replace('\n', ' ')
        a = ASSIGN_TAIL.search(head)
        if a:
            var = a.group(1)
            if re.search(r'\b(?:Outline|OutlinePx|Ring)\s*\(\s*' + re.escape(var) + r'\s*[,)]', src):  # labels[i] 처럼 ] 로 끝나는 변수도
                return 'ok', '"%s" → %s 에 키라인 호출' % (tail, var)
    if not found:
        return 'absent', '"%s" 이름으로 만드는 글자가 없다' % tail
    return 'missing', '"%s" 글자에 Outline/OutlinePx/Ring 이 안 걸린다' % tail


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
    for line, sel, width in rules:
        seen.add(sel)
        if sel not in table:
            problems += 1
            out('✗ 표에 없는 정본 규칙  style.css %d  %s  { %s }  → TABLE 에 짝을 더해라' % (line, sel, width))
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
            tag = '호출 없음' if state == 'missing' else '자리 없음'
            if t in known:
                n_known += 1
                out('· KNOWN(%s)  %s  ← style.css %d %s { %s }  — %s' % (tag, t, line, sel, width, known[t]))
            else:
                problems += 1
                out('✗ %s  %s  ← style.css %d  %s  { %s }  — %s' % (tag, t, line, sel, width, why))
    for sel in table:
        if sel not in seen:
            problems += 1
            out('✗ 표에는 있는데 정본에 없는 선택자: %s  → 정본이 바뀌었다 · TABLE 에서 지우거나 고쳐라' % sel)
    for t in sorted(set(known_now_ok)):
        out('· KNOWN 인데 이제 키라인이 있다: %s  → KNOWN 에서 지워라' % t)
    out('%s check_keyline: 정본 규칙 %d(끄는 규칙 %d) · 자리 초록 %d · KNOWN 빈자리 %d · 문제 %d'
        % ('✓' if problems == 0 else '✗', len(rules), n_off, n_ok, n_known, problems))
    return 0 if problems == 0 else 1


# ── 자기 검사(고장 주입) ────────────────────────────────────────────────────────────────────
def self_test():
    css = """
/* 주석 {  } */
.a-name { color: #fff; -webkit-text-stroke: 2px var(--pp-line); paint-order: stroke fill; }
.b-plain { -webkit-text-stroke: .11em #000; }
.c-btn { -webkit-text-stroke-width: 0; }
.d-str { -webkit-text-stroke: 3px #000; }
.e-file { -webkit-text-stroke: 1px #000; }
.f-method { -webkit-text-stroke: 4px #000; }
"""
    cs = """
namespace X {
    public static class Popups {
        public static Button Btn(Transform p, string name) {
            TextMeshProUGUI t = UiKit.Text(p, "label", TextKind.Sub, name, "ink");
            PopupKit.Ring(t);
            return null;
        }
        public static Button Bare(Transform p) {
            TextMeshProUGUI t = UiKit.Text(p, "bare", TextKind.Sub, "x", "ink");
            return null;
        }
    }
    class Sheet {
        void Build(Transform p, bool on) {
            TextMeshProUGUI nm = UiKit.Text(p, "name", TextKind.Sub, "n", "ink", TextAlignmentOptions.Left);
            PopupKit.Ring(nm, "pp_line", 0.2f);
            TextMeshProUGUI pl = UiKit.Text(p, "plain", TextKind.Sub, "x", "ink");
            TextMeshProUGUI st = on
                ? PetSkillKit.Stroked(p, "str", TextKind.Sub, "s", Color.white, 0.3f)
                : PetSkillKit.Text(p, "str", TextKind.Sub, "s", Color.white);
            labels[i] = PetSkillKit.Text(p, "arr", TextKind.Sub, "a", Color.white);
            if (on) UiKit.Outline(labels[i], "pp_line", 0.25f);
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
        '.a-name': ['Ui/Sheet.cs#name'], '.b-plain': ['Ui/Sheet.cs#plain'], '.c-btn': '—끄는 규칙',
        '.d-str': ['Ui/Sheet.cs#str'], '.e-file': ['Ui/Sheet.cs'], '.f-method': ['Ui/Sheet.cs@Btn'],
    }
    # 1 다 있으면 0 (plain 은 KNOWN)
    expect('전부 초록', base, {'Ui/Sheet.cs#plain': '임자'}, 0)
    # 2 KNOWN 을 지우면 호출 없음 → 1
    expect('호출 없음 → 1', base, {}, 1)
    # 3 정본에 규칙이 더 있는데 표에 없다 → 1
    expect('표에 없는 규칙 → 1', base, {'Ui/Sheet.cs#plain': '임자'}, 1, css_text=css + '.z-new { -webkit-text-stroke: 1px #000; }\n')
    # 4 표에는 있는데 정본에 없다 → 1
    t = dict(base); t['.gone'] = ['Ui/Sheet.cs']
    expect('정본에 없는 선택자 → 1', t, {'Ui/Sheet.cs#plain': '임자'}, 1)
    # 5 요소 자체가 없다 → 1 · KNOWN 이면 0
    t = dict(base); t['.a-name'] = ['Ui/Sheet.cs#nowhere']
    expect('자리 없음 → 1', t, {'Ui/Sheet.cs#plain': '임자'}, 1)
    expect('자리 없음 KNOWN → 0', t, {'Ui/Sheet.cs#plain': '임자', 'Ui/Sheet.cs#nowhere': '임자'}, 0)
    # 6 메서드 본문에 키라인이 없다 → 1
    t = dict(base); t['.f-method'] = ['Ui/Sheet.cs@Bare']
    expect('메서드 민글자 → 1', t, {'Ui/Sheet.cs#plain': '임자'}, 1)
    # 7 고장 주입: 리그 이름의 Ring 을 지우면 1
    broken = cs.replace('PopupKit.Ring(nm, "pp_line", 0.2f);', '')
    expect('Ring 지움 → 1', base, {'Ui/Sheet.cs#plain': '임자'}, 1, cs_text=broken)
    # 8 배열 원소 변수(labels[i]) 도 잡는다 · 삼항 Stroked 도 잡는다
    t = dict(base); t['.e-file'] = ['Ui/Sheet.cs#arr']
    expect('배열 변수 Outline → 0', t, {'Ui/Sheet.cs#plain': '임자'}, 0)
    # 9 KNOWN 인데 이제 있다 → 알리기만(rc 0)
    lines = expect('KNOWN 해소 알림', base, {'Ui/Sheet.cs#plain': '임자', 'Ui/Sheet.cs#name': '옛 임자'}, 0)
    if not any('KNOWN 인데 이제 키라인이 있다' in l for l in lines):
        fails.append('KNOWN 해소 알림이 안 나온다')
    # 10 파서: 주석 속 중괄호를 무시하고 줄 번호를 지킨다
    rules = parse_rules(css)
    if [r[1] for r in rules] != ['.a-name', '.b-plain', '.c-btn', '.d-str', '.e-file', '.f-method'] or rules[0][0] != 3:
        fails.append('파서: %r' % rules)
    # 11 정본 CSS 가 없으면 2
    if run('/nonexistent/style.css', '.', base, {}, out=lambda s: None) != 2:
        fails.append('정본 없음 rc 2')
    if fails:
        print('✗ check_keyline --self-test 실패 %d' % len(fails))
        for f in fails:
            print('  - ' + f)
        return 1
    print('✓ check_keyline --self-test 11칸 통과')
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
        print('사용: check_keyline.py [--css <style.css>] [--game <Assets/Scripts/Game>] [--self-test]')
        return 2
    return run(css, game, TABLE, KNOWN)


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
