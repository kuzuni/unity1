#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T383 — 정본 `js/ui.js` 가 `<br>` 로 **줄 수를 손으로 못박은 자리**(22) ↔ 클론의 같은 자리 대조 자.

왜 있나: 클론의 글자 하한(§1 · Sub 36px ≈ .988rem)은 주인 지시라 못 내린다. 그래서 잡을 것은 «하한 때문에
정본이 못박은 **모양**이 깨지는 자리» 고, 그 자리는 셀 수 있다 — 정본이 `<br>` 을 박은 곳이다(런 666: 패스 안내문
「…보상을 받<br>으세요!」 두 줄이 클론에서 세 줄).

이 자가 보는 것(코드 글자 · 정적):
  ① 정본 — 표의 각 줄 번호에 아직 `<br` 이 있고 표가 적은 선택자·글이 그 줄에 있다(정본이 움직이면 먼저 운다) ·
     정본 전체 `<br` 수 == 표의 `<br` 합.
  ② 클론 — 표가 대는 파일에 그 이름이 있고, 줄 수를 **코드가 안다**: 한 상자면 문자열에 `\\n` 이 (줄 수 − 1)개(표의 조각들이
     파일에 있다) · 상자 여럿이면 그 이름들이 다 있다 · 가변(join('<br>'))이면 접두 이름이 있다.
  ③ KNOWN — 코드는 알지만 **화면이 다르다고 확인된** 자리(임자 lock 뒤) · 알리되 막지 않는다.
화면에서 실제 몇 줄로 서는가(`textInfo.lineCount`)는 PlayMode 자 몫이다(2회차) — 이 자는 그 자가 어디를 봐야 하는지 표로 준다.

사용:  python3 tools/check_br_lines.py [--ui <ui.js>] [--game <Assets/Scripts/Game>] [--list] [--self-test]
rc:    0 = ①② 다 맞다 · 1 = 어긋남 · 2 = 파일을 못 읽었다
"""
import os
import re
import sys

UI_DEFAULT = os.path.join('.wwwww-src', 'web', 'js', 'ui.js')
GAME_DEFAULT = os.path.join('Assets', 'Scripts', 'Game')

# 정본 줄 → 자리. kind: 'one'(한 상자 · lits = `\n` 을 품은 코드 조각들 · 줄 수 = 조각 수 + 1) ·
#                    'split'(상자 여럿 · names) · 'var'(가변 · join('<br>') · prefix)
TABLE = [
    {'ui': 1506, 'sel': '승천<br>가능', 'br': 1, 'file': 'Ui/ForgeSheet.cs', 'name': 'forge-btn', 'kind': 'one', 'lits': ['★ 승천\\n가능'], 'what': '장비 시트 대장간 버튼(승천 가능)'},
    {'ui': 1507, 'sel': '대장간<br>최고 레벨', 'br': 1, 'file': 'Ui/ForgeSheet.cs', 'name': 'forge-btn', 'kind': 'one', 'lits': ['대장간\\n최고 레벨'], 'what': '장비 시트 대장간 버튼(만렙)'},
    {'ui': 1511, 'sel': '대장간<br>레벨', 'br': 1, 'file': 'Ui/ForgeSheet.cs', 'name': 'forge-btn', 'kind': 'one', 'lits': ['대장간\\n레벨 '], 'what': '장비 시트 대장간 버튼(레벨 N)'},
    {'ui': 1551, 'sel': '자동', 'br': 1, 'file': 'Ui/ForgeSheet.cs', 'name': 'label-stack', 'kind': 'one', 'lits': ['자동\\n'], 'what': '장비 시트 자동 제련 버튼(자동 / ON·OFF·🔒)'},
    {'ui': 2036, 'sel': '승천<br><small>', 'br': 1, 'file': 'Ui/ForgeInfoPopup.cs', 'name': 'fi-upgrade', 'kind': 'one', 'lits': ['★ 승천\\n대장간 Lv.'], 'what': '대장간 정보 · 승천 버튼'},
    {'ui': 2043, 'sel': '건너뛰기<br>', 'br': 1, 'file': 'Ui/ForgeInfoPopup.cs', 'name': 'fi-skip', 'kind': 'one', 'lits': ['건너뛰기\\n'], 'what': '대장간 정보 · 건너뛰기(젬)'},
    {'ui': 2047, 'sel': '업그레이드<br><small>', 'br': 1, 'file': 'Ui/ForgeInfoPopup.cs', 'name': 'fi-upgrade', 'kind': 'one', 'lits': ['업그레이드\\n'], 'what': '대장간 정보 · 업그레이드 버튼(코인 · 시간)'},
    {'ui': 3863, 'sel': 'sellwarn-note', 'br': 1, 'file': 'Ui/ForgeCraftPopup.cs', 'name': 'note', 'kind': 'one', 'lits': ['더 최신입니다.\\n같거나'], 'what': '판매 경고 안내 두 줄'},
    {'ui': 4010, 'sel': "join('<br>')", 'br': 1, 'file': 'Ui/PetPanel.cs', 'name': 'petd-sub-', 'kind': 'var', 'what': '펫 상세 옵션 줄(가변 · 없으면 petd-subs 한 줄)'},
    {'ui': 4033, 'sel': 'petd-stats', 'br': 1, 'file': 'Ui/PetPanel.cs', 'kind': 'split', 'names': ['petd-atk', 'petd-hp'], 'what': '펫 상세 피해/체력'},
    {'ui': 4199, 'sel': 'idet-main', 'br': 1, 'file': 'Ui/PetUpgradePopup.cs', 'kind': 'split', 'names': ['idet-atk', 'idet-hp'], 'what': '펫 업그레이드 머리 피해/체력'},
    {'ui': 4678, 'sel': '이전 스테이지<br>소탕', 'br': 1, 'file': 'Ui/DungeonDetailPopup.cs', 'name': 'sweep', 'kind': 'one', 'lits': ['이전 스테이지\\n소탕'], 'what': '던전 상세 소탕 버튼'},
    {'ui': 4740, 'sel': 'league-name', 'br': 1, 'file': 'Ui/LeagueSheet.cs', 'kind': 'split', 'names': ['name', 'cp'], 'what': '리그 행 이름 / 전투력'},
    {'ui': 4812, 'sel': 'league-reward-desc', 'br': 1, 'file': 'Ui/LeagueSheet.cs', 'name': 'desc', 'kind': 'one', 'lits': ['시즌 종료 시\\n다음 보상을'], 'what': '리그 보상 안내 두 줄'},
    {'ui': 4859, 'sel': 'league-challenge-name', 'br': 1, 'file': 'Ui/LeagueSheet.cs', 'kind': 'split', 'names': ['name', 'cp-ico', 'cp'], 'what': '도전 행 이름 / 전투력'},
    {'ui': 4862, 'sel': '도전<br><small>', 'br': 1, 'file': 'Ui/LeagueSheet.cs', 'kind': 'split', 'names': ['challenge', 'label'], 'what': '도전 행 버튼(도전 / 티켓 아이콘 + 1)'},
    {'ui': 4928, 'sel': 'pass-desc', 'br': 1, 'file': 'Ui/PassPopup.cs', 'name': 'desc', 'kind': 'one', 'lits': ['보상을 받\\n으세요!'], 'what': '패스 안내 두 줄'},
    {'ui': 5689, 'sel': "join('<br>')", 'br': 1, 'file': 'Ui/MountSheet.cs', 'name': 'petd-sub-', 'kind': 'var', 'what': '탈것 상세 옵션 줄(가변)'},
    {'ui': 5704, 'sel': 'petd-stats', 'br': 1, 'file': 'Ui/MountSheet.cs', 'kind': 'split', 'names': ['petd-atk', 'petd-hp'], 'what': '탈것 상세 피해/체력'},
    {'ui': 5707, 'sel': 'petd-subs', 'br': 1, 'file': 'Ui/MountSheet.cs', 'kind': 'split', 'names': ['petd-subs', 'petd-same'], 'what': '탈것 상세 옵션 없음 / 같은 종류 보유'},
    {'ui': 5848, 'sel': '됩니다<br>', 'br': 1, 'file': 'Ui/AscendPopup.cs', 'name': 'eff', 'kind': 'one', 'lits': ['됩니다\\n· ⚠ 보유 중인 기존'], 'group': 'asc-eff', 'what': '승천 효과 글줄 1→2'},
    {'ui': 5849, 'sel': 'asc-wipe-warn', 'br': 1, 'file': 'Ui/AscendPopup.cs', 'name': 'eff', 'kind': 'one', 'lits': ['전부 사라집니다\\n· 이후 새로'], 'group': 'asc-eff', 'what': '승천 효과 글줄 2→3'},
]

# 코드는 줄 수를 알지만 화면이 다르다고 **확인된** 자리 — 알리되 막지 않는다(임자 lock 뒤).
KNOWN = {
    4928: 'T383 ⓑ · 런 666 screen_pass: 코드는 두 줄(\\n)을 아는데 하한 Sub 36 이 「보상을 받」 을 한 번 더 접어 세 줄 — PassPopup.cs 는 T332 lock 뒤',
    5848: 'T383 2회차 · 런 688 BrLinesTests: 승천 효과 글줄이 정본 세 줄 아닌 **넷** — 셋째 항목이 하한(Sub 36 ↔ 정본 .asc-focus-eff .76rem · 5635)에 밀려 접힌다 — AscendPopup.cs 는 T333·T354 lock 뒤(5849 와 한 자리)',
    5849: '5848 과 같은 자리(승천 효과 글줄 · 한 상자 3줄)',
}


def read(path):
    with open(path, encoding='utf-8') as fh:
        return fh.read()


def check_ui(ui_text, table):
    """① 정본: 줄마다 `<br` 과 선택자 글이 있는가 · 전체 <br 수 == 표 합."""
    lines = ui_text.split('\n')
    probs = []
    for t in table:
        ln = t['ui']
        line = lines[ln - 1] if 0 < ln <= len(lines) else ''
        if '<br' not in line:
            probs.append('정본 %d 줄에 <br 이 없다(정본이 움직였다 — 표의 줄 번호를 맞춰라): %s' % (ln, line.strip()[:80]))
        elif t['sel'] not in line:
            probs.append('정본 %d 줄에 «%s» 가 없다: %s' % (ln, t['sel'], line.strip()[:80]))
    total = ui_text.count('<br')
    want = sum(t['br'] for t in table)
    if total != want:
        probs.append('정본 <br 수 %d ≠ 표 합 %d — 새 자리가 생겼거나 사라졌다(--list 로 짝을 맞춰라)' % (total, want))
    return total, probs


def check_clone(game, table):
    """② 클론: 파일·이름이 있고 줄 수를 코드가 안다."""
    probs = []
    cache = {}
    for t in table:
        path = os.path.join(game, t['file'])
        if path not in cache:
            cache[path] = read(path) if os.path.isfile(path) else None
        src = cache[path]
        tag = '%s(정본 %d)' % (t['file'], t['ui'])
        if src is None:
            probs.append(tag + ': 파일이 없다'); continue
        if t['kind'] == 'one':
            if '"%s"' % t['name'] not in src:
                probs.append(tag + ': 이름 «%s» 가 없다' % t['name'])
            for lit in t['lits']:
                if lit not in src:
                    probs.append(tag + ': 줄바꿈 조각 «%s» 가 코드에 없다(정본 줄 수를 코드가 모른다)' % lit)
        elif t['kind'] == 'split':
            for n in t['names']:
                if '"%s"' % n not in src:
                    probs.append(tag + ': 상자 «%s» 가 없다' % n)
        elif t['kind'] == 'var':
            if '"%s"' % t['name'] not in src:
                probs.append(tag + ': 접두 이름 «%s» 가 없다' % t['name'])
    return probs


def lines_of(table):
    """클론 자리별 정본 줄 수(같은 group 은 합친다)."""
    out = {}
    for t in table:
        key = t.get('group') or '%s#%s@%d' % (t['file'], t.get('name') or '+'.join(t.get('names', [])), t['ui'])
        out[key] = out.get(key, 1) + t['br']
    return out


def run(ui_path, game, table, known, list_all=False, out=print):
    if not os.path.isfile(ui_path):
        out('✗ check_br_lines: 정본을 못 읽었다: ' + ui_path + ' (clone: git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)'); return 2
    if not os.path.isdir(game):
        out('✗ check_br_lines: 갈래를 못 찾았다: ' + game); return 2
    ui_text = read(ui_path)
    total, p1 = check_ui(ui_text, table)
    p2 = check_clone(game, table)
    if list_all:
        for t in table:
            k = '  %5d  %-24s → %s#%s  %s%s' % (t['ui'], t['sel'][:24], t['file'], t.get('name') or '+'.join(t.get('names', [])),
                                               t['kind'], '  ⚠ KNOWN' if t['ui'] in known else '')
            out(k)
    for p in p1 + p2:
        out('  ✗ ' + p)
    for ln, why in sorted(known.items()):
        out('  ⚠ KNOWN 정본 %d: %s' % (ln, why))
    sites = lines_of(table)
    summary = '정본 <br> %d(줄 %d) · 클론 자리 %d · 한 상자 %d · 상자 여럿 %d · 가변 %d · KNOWN(화면 어긋남 · 안 막음) %d · 문제 %d' % (
        total, len(table), len(sites), sum(1 for t in table if t['kind'] == 'one'), sum(1 for t in table if t['kind'] == 'split'),
        sum(1 for t in table if t['kind'] == 'var'), len(known), len(p1) + len(p2))
    if p1 or p2:
        out('✗ check_br_lines: ' + summary); return 1
    out('✓ check_br_lines: ' + summary + ' · 화면 줄 수는 PlayMode 자 몫')
    return 0


# ---------------------------------------------------------------- 자기 검사
def self_test():
    import tempfile
    n = [0]
    fails = []

    def ok(cond, what):
        n[0] += 1
        (print('  ✓ 자기 검사 %d: %s' % (n[0], what)) if cond else (fails.append(what), print('  ✗ 자기 검사 %d: %s' % (n[0], what))))

    ui = '\n'.join(['x'] * 9 + ['<p class="a-desc">하나<br>둘</p>', 'q', '<b class="b-name">${n}<br><small>c</small></b>', 'r = items.join(\'<br>\')'])
    table = [
        {'ui': 10, 'sel': 'a-desc', 'br': 1, 'file': 'Ui/A.cs', 'name': 'desc', 'kind': 'one', 'lits': ['하나\\n둘'], 'what': ''},
        {'ui': 12, 'sel': 'b-name', 'br': 1, 'file': 'Ui/B.cs', 'kind': 'split', 'names': ['name', 'cp'], 'what': ''},
        {'ui': 13, 'sel': "join('<br>')", 'br': 1, 'file': 'Ui/B.cs', 'name': 'sub-', 'kind': 'var', 'what': ''},
    ]
    with tempfile.TemporaryDirectory() as d:
        os.makedirs(os.path.join(d, 'Ui'))
        with open(os.path.join(d, 'Ui', 'A.cs'), 'w', encoding='utf-8') as fh:
            fh.write('var t = UiKit.Text(x, "desc", K, "하나\\n둘");')
        with open(os.path.join(d, 'Ui', 'B.cs'), 'w', encoding='utf-8') as fh:
            fh.write('Text(r, "name", ..); Text(r, "cp", ..); Text(r, "sub-" + i, ..);')
        total, p1 = check_ui(ui, table)
        ok(total == 3 and not p1, '정본 <br 3 == 표 합 · 줄마다 선택자 있음 (문제 %d)' % len(p1))
        p2 = check_clone(d, table)
        ok(not p2, '클론 한 상자(\\n 조각)·상자 여럿·가변 전부 초록 (문제 %d)' % len(p2))
        # 정본 드리프트: 줄이 밀리면 운다
        _, p1b = check_ui('\n' + ui, table)
        ok(len(p1b) >= 1, '정본 줄이 밀리면 «줄 번호를 맞춰라» 로 운다')
        # 정본에 새 <br> 이 생기면 합이 어긋나 운다
        _, p1c = check_ui(ui + '\n<i>새<br>자리</i>', table)
        ok(any('≠' in p for p in p1c), '정본에 새 <br> 이 생기면 합 불일치로 운다')
        # 클론이 \n 을 잃으면 운다
        with open(os.path.join(d, 'Ui', 'A.cs'), 'w', encoding='utf-8') as fh:
            fh.write('var t = UiKit.Text(x, "desc", K, "하나 둘");')
        p2b = check_clone(d, table)
        ok(len(p2b) == 1 and '조각' in p2b[0], '클론이 줄바꿈(\\n)을 잃으면 «코드가 모른다» 로 운다')
        # 상자 이름이 사라지면 운다(A.cs 는 먼저 되돌린다 — 앞 칸의 고장이 섞이지 않게)
        with open(os.path.join(d, 'Ui', 'A.cs'), 'w', encoding='utf-8') as fh:
            fh.write('var t = UiKit.Text(x, "desc", K, "하나\\n둘");')
        with open(os.path.join(d, 'Ui', 'B.cs'), 'w', encoding='utf-8') as fh:
            fh.write('Text(r, "name", ..); Text(r, "sub-" + i, ..);')
        p2c = check_clone(d, table)
        ok(len(p2c) == 1 and 'cp' in p2c[0], '상자 여럿 중 하나가 사라지면 운다')
        # 같은 group 은 줄 수를 합친다
        g = lines_of([{'ui': 1, 'br': 1, 'file': 'F', 'name': 'e', 'group': 'g'}, {'ui': 2, 'br': 1, 'file': 'F', 'name': 'e', 'group': 'g'}])
        ok(g == {'g': 3}, '같은 group 두 줄 = 한 자리 3줄')
        # run() 이 KNOWN 을 알리되 막지 않는다
        with open(os.path.join(d, 'Ui', 'A.cs'), 'w', encoding='utf-8') as fh:
            fh.write('var t = UiKit.Text(x, "desc", K, "하나\\n둘");')
        with open(os.path.join(d, 'Ui', 'B.cs'), 'w', encoding='utf-8') as fh:
            fh.write('Text(r, "name", ..); Text(r, "cp", ..); Text(r, "sub-" + i, ..);')
        uipath = os.path.join(d, 'ui.js')
        with open(uipath, 'w', encoding='utf-8') as fh:
            fh.write(ui)
        buf = []
        rc = run(uipath, d, table, {10: '시험'}, list_all=True, out=buf.append)
        ok(rc == 0 and any('KNOWN 정본 10' in b for b in buf) and any('a-desc' in b for b in buf), 'run: KNOWN 은 알리되 rc 0 · --list 가 자리를 찍는다')
    # 실물 표: 줄 번호가 오름차순 · 파일 경로 꼴
    ok(all(TABLE[i]['ui'] < TABLE[i + 1]['ui'] for i in range(len(TABLE) - 1)), '실물 표는 정본 줄 오름차순')
    ok(all(t['file'].startswith('Ui/') and t['file'].endswith('.cs') for t in TABLE), '실물 표의 파일은 전부 Ui/*.cs')
    print(('✓' if not fails else '✗') + ' check_br_lines --self-test: %d칸' % n[0] + ('' if not fails else ' · 실패 %d' % len(fails)))
    return 0 if not fails else 1


def main(argv):
    ui, game, list_all = UI_DEFAULT, GAME_DEFAULT, False
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--self-test':
            return self_test()
        if a == '--list':
            list_all = True
        elif a == '--ui':
            i += 1; ui = argv[i]
        elif a == '--game':
            i += 1; game = argv[i]
        else:
            print('모르는 인자: ' + a); return 2
        i += 1
    return run(ui, game, TABLE, KNOWN, list_all)


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
