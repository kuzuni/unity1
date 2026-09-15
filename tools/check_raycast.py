#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T33 24회차 — 정본 `pointer-events` ↔ 클론 `raycastTarget` 지킴이.

정본 `style.css` 의 `pointer-events` 선언은 **68**이고 그중 **67 이 `none`** 이다 — 겹쳐 그리는 장식(연출 판·후광·비네트·테·리본·
그림자·마스크)이 **밑의 단추를 먹지 않게** 못 박은 것이다. 남은 하나(`.craft-batch` 1121 `auto`)만 «이 덮개는 클릭을 받는다» 다.

유니티는 **부호가 반대**다: `Graphic`(Image·RawImage·TMP)은 만들면 `raycastTarget = true` 라서 **아무 장식이나 단추를 먹는다**.
클론은 그 뒤집힘을 알고 `UiKit` 공장(`Panel`·`Icon`·`Text`)에서 false 로 낮춰 두었다 — 그래서 «UiKit 을 지나는 길» 은 안전하다.
새는 곳은 **공장 밖에서 `AddComponent<Image>()` 로 직접 만드는 자리**(연출·굽기 코드가 그렇게 만든다)다. 그 자리는 기본값 true 로 태어나
아무도 손대지 않으면 **조용히** 밑을 막는다 — 화면은 멀쩡해 보이고 누르면 안 눌린다. 자가 없으면 이것은 다음 회차에 또 난다.

이 자가 막는 것 하나: **`UiKit`·`Popups` 공장 밖에서 만든 `Image`/`RawImage` 는 그 자리에서 `raycastTarget` 을 명시해야 한다**
(true = «이 덮개는 클릭을 받는다» · false = 정본의 `pointer-events: none`). 무엇을 골랐는지는 안 따진다 — **정하지 않은 것**만 막는다.

일부러 기본값(true)으로 두는 자리는 ALLOW 에 까닭과 함께 적는다(예: 스크롤 뷰포트의 투명 hit 판).

사용:  python3 tools/check_raycast.py [--game <Assets/Scripts/Game>] [--list] [--self-test]
rc:    0 = 명시 안 한 자리가 없다(또는 전부 ALLOW) · 1 = 있다
"""
import os
import re
import sys
import tempfile

GAME_DEFAULT = os.path.join('Assets', 'Scripts', 'Game')
# 기본값(true)으로 두는 자리 — «파일:이름» → 까닭
ALLOW = {
    'Ui/TechPanel.cs:vhit': '스크롤 뷰포트의 투명 hit 판 — 끌기를 받아야 한다(기본값 true 가 옳다)',
    'Ui/TechPopups.cs:vhit': '같은 뷰포트 hit 판(기술 상세 팝업)',
}
MAKE = re.compile(r'AddComponent<\s*(?:UnityEngine\.UI\.)?(Image|RawImage)\s*>')
VAR = re.compile(r'(?:Image|RawImage)\s+(\w+)\s*=|(\w+(?:\.\w+)*)\s*=\s*[^=].*AddComponent')
LOOK = 14   # 만든 줄부터 몇 줄 안에서 raycastTarget 을 찾나


def var_of(line):
    m = VAR.search(line)
    if not m:
        return '?'
    return (m.group(1) or m.group(2) or '?').split('.')[-1]


def scan_text(text):
    """[(줄번호, 이름, 명시했나)] — 한 파일 본문에 대해."""
    out = []
    lines = text.split('\n')
    for i, l in enumerate(lines):
        if not MAKE.search(l):
            continue
        win = '\n'.join(lines[i:i + LOOK])
        out.append((i + 1, var_of(l), 'raycastTarget' in win))
    return out


def run(game_dir, out=print, list_all=False, allow=None):
    if allow is None:
        allow = ALLOW
    total = named = 0
    bad = []
    allowed = []
    for dp, _, fs in os.walk(game_dir):
        for f in sorted(fs):
            if not f.endswith('.cs'):
                continue
            path = os.path.join(dp, f)
            rel = os.path.relpath(path, game_dir).replace(os.sep, '/')
            with open(path, encoding='utf-8') as fh:
                text = fh.read()
            for line, name, said in scan_text(text):
                total += 1
                key = rel + ':' + name
                if said:
                    named += 1
                    if key in allow:
                        allowed.append(key)
                    if list_all:
                        out('· 명시  %s:%d  %s' % (rel, line, name))
                elif key in allow:
                    allowed.append(key)
                    out('· ALLOW  %s:%d  %s  — %s' % (rel, line, name, allow[key]))
                else:
                    bad.append((rel, line, name))
    for rel, line, name in bad:
        out('✗ %s:%d — `%s` 를 UiKit 공장 밖에서 만들고 `raycastTarget` 을 안 정했다. 유니티 기본값은 **true** 라 이 장식이 밑의 단추를 먹는다 '
            '(정본은 그런 자리에 `pointer-events: none` 을 못 박는다 · 68 중 67). 한 줄로 정해라 — 덮개가 클릭을 받아야 하면 true, 아니면 false. '
            '기본값이 옳은 자리면 이 자의 ALLOW 에 까닭과 함께 넣어라.' % (rel, line, name))
    out('%s check_raycast: 공장 밖 Image/RawImage %d 자리 · raycastTarget 을 정한 자리 %d · ALLOW %d · 안 정한 자리 %d'
        % ('✓' if not bad else '✗', total, named, len(set(allowed)), len(bad)))
    return 0 if not bad else 1


def self_test():
    fails = []
    tmp = tempfile.mkdtemp(prefix='raycast_')
    d = os.path.join(tmp, 'Ui')
    os.makedirs(d)

    def w(name, text):
        with open(os.path.join(d, name), 'w', encoding='utf-8') as f:
            f.write(text)

    def go(allow=None):
        lines = []
        rc = run(tmp, out=lines.append, allow=allow if allow is not None else {})
        return rc, '\n'.join(lines)

    def expect(n, cond, detail=''):
        if not cond:
            fails.append(n + (' — ' + detail if detail else ''))

    # ⓐ 정한 자리는 초록
    w('A.cs', 'class A { void M(){ Image a = rt.gameObject.AddComponent<Image>();\n a.raycastTarget = false; } }')
    rc, out = go()
    expect('ⓐ 정한 자리 rc 0', rc == 0 and '정한 자리 1' in out, out)
    # ⓑ 안 정하면 빨강 · 문구에 이름과 줄
    w('A.cs', 'class A { void M(){ Image a = rt.gameObject.AddComponent<Image>();\n a.color = Color.white; } }')
    rc, out = go()
    expect('ⓑ 안 정하면 rc 1', rc == 1 and 'Ui/A.cs:1' in out and '`a`' in out, out)
    # ⓒ ALLOW 면 rc 0 이고 까닭이 보인다
    rc, out = go({'Ui/A.cs:a': '뷰포트 hit 판'})
    expect('ⓒ ALLOW rc 0', rc == 0 and 'ALLOW' in out and '뷰포트 hit 판' in out, out)
    # ⓓ 창(14줄) 밖에 있으면 «안 정함» — 멀리 떨어진 줄은 그 자리의 결정이 아니다
    w('A.cs', 'class A { void M(){ Image a = rt.gameObject.AddComponent<Image>();\n' + '\n' * 20 + ' a.raycastTarget = false; } }')
    rc, out = go()
    expect('ⓓ 창 밖은 안 정함', rc == 1, out)
    # ⓔ RawImage 도 센다 · true 로 정한 것도 «정함»
    w('A.cs', 'class A { void M(){ var a = go.AddComponent<RawImage>();\n a.raycastTarget = true; } }')
    rc, out = go()
    expect('ⓔ RawImage·true', rc == 0 and '공장 밖 Image/RawImage 1' in out, out)
    # ⓕ 만드는 자리가 없으면 0 자리 · rc 0
    w('A.cs', 'class A { void M(){ int x = 1; } }')
    rc, out = go()
    expect('ⓕ 없으면 rc 0', rc == 0 and '공장 밖 Image/RawImage 0' in out, out)
    # ⓖ 한 줄에 만들고 바로 정하는 꼴
    w('A.cs', 'class A { void M(){ Image a = go.AddComponent<Image>(); a.raycastTarget = false; } }')
    rc, out = go()
    expect('ⓖ 한 줄 꼴', rc == 0, out)
    # ⓗ 이름이 필드(this.x)여도 마지막 조각으로 읽는다
    w('A.cs', 'class A { void M(){ fx.raysImg = fx.rays.gameObject.AddComponent<Image>();\n fx.raysImg.raycastTarget = false; } }')
    rc, out = go()
    expect('ⓗ 필드 이름', rc == 0, out)
    # ⓘ 진짜 ALLOW 문법
    for k in ALLOW:
        expect('ⓘ ALLOW 문법 ' + k, re.match(r'^[\w/]+\.cs:\w+$', k) is not None)
    if fails:
        print('✗ check_raycast --self-test 실패 %d' % len(fails))
        for f in fails:
            print('  · ' + f[:300])
        return 1
    print('✓ check_raycast --self-test 10칸 통과')
    return 0


def main(argv):
    game, list_all = GAME_DEFAULT, False
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--self-test':
            return self_test()
        if a == '--list':
            list_all = True; i += 1; continue
        if a == '--game' and i + 1 < len(argv):
            game = argv[i + 1]; i += 2; continue
        print('사용: check_raycast.py [--game <Assets/Scripts/Game>] [--list] [--self-test]')
        return 2
    return run(game, list_all=list_all)


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
