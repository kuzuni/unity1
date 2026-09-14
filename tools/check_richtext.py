#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""T33 13회차 — 정본 `U.escapeHtml` 이 막던 자리를 클론에서는 «TMP 리치텍스트 끄기» 하나가 막는다. 그 하나를 지킨다.

정본은 플레이어가 고치는 글(닉네임 `S.nickname` · 채팅 문구·태그 · 리그 이름)을 DOM 에 넣기 전에
`U.escapeHtml`(util.js 27)로 **꺾쇠를 죽인다**(ui.js 1296·4740·4859·5038·5194·5259…). 클론에는 그 함수가 없고
없는 것이 맞다 — HTML 이 아니니까. 대신 같은 구멍이 TMP 에 있다: `richText` 가 켜져 있으면 `<color=red>` 나 `<b>` 가
**글자가 아니라 태그로 먹혀** 이름이 사라지거나 색이 바뀐다(정본이 escapeHtml 로 막는 바로 그 일).

지금 클론은 글자를 만드는 자리가 **딱 하나**(`UiKit.Text` · Assets/Scripts/Game/Ui/UiKit.cs)이고 거기서 `richText = false` 를
박아 둔다 — 그래서 구멍이 없다. 이 자는 그 두 가지가 유지되는지만 본다:
  ⓐ `UiKit.Text` 안에 `richText = false` 가 있다.
  ⓑ TMP 글자 부품을 **UiKit.cs 밖에서** 새로 만들지 않는다(`AddComponent<TextMeshProUGUI>` · `<TextMeshPro>`).
  ⓒ 어디서도 `richText = true` 로 되돌리지 않는다.
셋 중 하나라도 깨지면 rc 1. (깨야 할 까닭이 생기면 — 예: 정본이 한 줄 안에서 색을 갈아 끼우는 자리 — 그 자리에서
플레이어 글이 들어오지 않는다는 근거를 완료 기록에 적고 이 자의 ALLOW 에 한 줄 넣는다.)
"""
import os, re, sys

FACTORY = os.path.join('Assets', 'Scripts', 'Game', 'Ui', 'UiKit.cs')
ROOT = os.path.join('Assets', 'Scripts')
# 만들어도 되는 자리(파일 경로 · 까닭을 여기 적는다)
ALLOW = {
    FACTORY: '글자 공장 하나 — 여기서 richText 를 끈다',
}
RE_NEW = re.compile(r'AddComponent<\s*(TextMeshProUGUI|TextMeshPro)\s*>')
RE_ON = re.compile(r'richText\s*=\s*true')
RE_OFF = re.compile(r'richText\s*=\s*false')


def scan(text):
    """(새로 만든 자리 줄번호, richText 켠 줄번호) — 문자열 하나에 대해."""
    made, on = [], []
    for i, line in enumerate(text.splitlines(), 1):
        if RE_NEW.search(line):
            made.append(i)
        if RE_ON.search(line):
            on.append(i)
    return made, on


def self_test():
    ok = True
    cases = [
        ("TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();", ([1], [])),
        ("var t = go.AddComponent< TextMeshPro >();", ([1], [])),
        ("t.richText = true;", ([], [1])),
        ("t.richText = false;", ([], [])),
        ("// AddComponent<TextMeshProUGUI> 는 여기 말고", ([1], [])),   # 주석도 센다(일부러 — 되살리기 쉬운 자리다)
        ("label.text = \"<b>굵게</b>\";", ([], [])),
    ]
    for src, want in cases:
        got = scan(src)
        if got != want:
            print('✗ 자기 검사: %r → %r (기대 %r)' % (src, got, want))
            ok = False
    if RE_OFF.search('t.richText = false;') is None:
        print('✗ 자기 검사: 끄는 줄을 못 읽는다')
        ok = False
    print('%s check_richtext --self-test: %d칸' % ('✓' if ok else '✗', len(cases) + 1))
    return 0 if ok else 1


def main():
    if '--self-test' in sys.argv:
        return self_test()
    if not os.path.isdir(ROOT):
        print('✗ check_richtext: %s 가 없다 — 저장소 뿌리에서 돌려라' % ROOT)
        return 1
    bad = []
    # ⓐ 공장이 끄고 있는가
    try:
        with open(FACTORY, encoding='utf-8') as f:
            fac = f.read()
    except OSError:
        print('✗ check_richtext: 글자 공장 %s 를 못 읽는다' % FACTORY)
        return 1
    if not RE_OFF.search(fac):
        bad.append('%s: `richText = false` 가 없다 — 플레이어 닉네임·채팅 글의 꺾쇠가 태그로 먹힌다'
                   '(정본 `U.escapeHtml` 이 막던 자리)' % FACTORY)
    # ⓑⓒ 나머지 전부
    for root, dirs, files in os.walk(ROOT):
        for fn in sorted(files):
            if not fn.endswith('.cs'):
                continue
            p = os.path.join(root, fn)
            with open(p, encoding='utf-8', errors='ignore') as f:
                made, on = scan(f.read())
            if made and p not in ALLOW:
                bad.append('%s:%s — TMP 글자를 여기서 만든다. `UiKit.Text` 를 쓰거나, 못 쓰면 이 자의 ALLOW 에 까닭과 함께 넣고 그 자리에서 `richText = false` 를 박아라'
                           % (p, ','.join(str(i) for i in made)))
            for i in on:
                bad.append('%s:%d — `richText = true`. 플레이어가 고치는 글(닉네임·채팅·리그 이름)이 이 글자를 지나가면 '
                           '`<color=…>` 이 태그로 먹힌다 — 정본은 그 자리를 `U.escapeHtml` 로 막는다' % (p, i))
    if bad:
        print('✗ check_richtext: %d곳' % len(bad))
        for b in bad:
            print('  · ' + b)
        return 1
    print('✓ check_richtext: 글자 공장 하나(%s)가 richText 를 끄고, 그 밖에서 TMP 를 만들거나 되켜는 자리 0' % FACTORY)
    return 0


if __name__ == '__main__':
    sys.exit(main())
