#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T349 — 촬영·측정 카메라가 **제 대상에 맞게** 후처리를 켜는가.

왜 있나(실측 · 런 562): T349 3회차가 «`CopyFrom` 은 URP 추가 데이터를 안 옮긴다» 는 옳은 고침을
**꼴이 같다는 이유로** 촬영 자 열셋에 한 번에 붙였다. 그런데 그중 **열하나는 `cullingMask` 가 UI 층 하나**로
3D 를 한 화소도 안 그린다 — 거기서 후처리는 얻는 것이 0이고 **UI 만 톤맵·색 보정에 물든다**.
`ForgeUiTests.접지_그림자와_순백_코어…` 가 «순백 코어 0픽셀 · 가장 밝은 화소 rgb 215,215,214» 로 넘어졌다
(ACES 가 흰색을 내린 값이다). 정본은 `filter` 를 `#game3d` 에만 걸고 HUD·패널·팝업에는 안 건다(T357).

규칙 하나: **UI 층만 그리는 카메라에서는 `renderPostProcessing` 이 켜져 있으면 안 된다.**
  · `ShotCam.CopyUrp(...)` 를 그냥 부르면 게임 카메라의 값(켜짐)을 그대로 받으므로 **빨강**이다.
  · `ShotCam.CopyUrp(...).renderPostProcessing = false` 는 괜찮다(워커 F 의 꼴 · 다른 URP 값은 받는다).
  · 아예 안 부르는 것도 괜찮다(3회차를 되돌린 꼴).
3D 를 그리는 카메라(`cullingMask |=` 로 세계를 남기거나 마스크를 아예 안 바꾼 것)는 **켜는 것이 맞다** — 안 세운다.

쓰기: check_shot_cams.py [--list] [--self-test]
"""
import os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TESTS = os.path.join(ROOT, 'Assets', 'Tests', 'PlayMode')

# 새 카메라를 만드는 자리 — 여기서부터 한 덩이로 읽는다
CAM_NEW = re.compile(r'(\w+)\s*=\s*\w+\.AddComponent<Camera>\(\)')
# 마스크를 «UI 층 하나로» 못 박는 꼴 (= 3D 를 안 그린다)
UI_ONLY = re.compile(r'\.cullingMask\s*=\s*1\s*<<\s*\w+')
# 마스크에 UI 를 **더하는** 꼴 (= 세계를 남긴다)
MASK_ADD = re.compile(r'\.cullingMask\s*\|=')
COPYURP = re.compile(r'ShotCam\.CopyUrp\s*\([^)]*\)')
POST_OFF = re.compile(r'ShotCam\.CopyUrp\s*\([^)]*\)\s*\.renderPostProcessing\s*=\s*false')
POST_OFF2 = re.compile(r'\.renderPostProcessing\s*=\s*false')


def blocks(text):
    """카메라를 만든 자리부터 다음 카메라(또는 끝)까지를 한 덩이로 돌려준다 — (이름, 본문)."""
    out, hits = [], list(CAM_NEW.finditer(text))
    for n, m in enumerate(hits):
        end = hits[n + 1].start() if n + 1 < len(hits) else len(text)
        out.append((m.group(1), text[m.start():end]))
    return out


def classify(body):
    """한 덩이 → (ui_only, copyurp, post_off). 순수 함수 — 자기 검사가 이것을 잰다."""
    ui_only = bool(UI_ONLY.search(body)) and not MASK_ADD.search(body)
    copyurp = bool(COPYURP.search(body))
    post_off = bool(POST_OFF.search(body)) or bool(POST_OFF2.search(body))
    return ui_only, copyurp, post_off


def verdict(ui_only, copyurp, post_off):
    """빨강인가 — UI 만 그리는데 후처리를 받아 켠 채로 두면 빨강."""
    return ui_only and copyurp and not post_off


def scan(path=TESTS):
    rows = []
    for name in sorted(os.listdir(path)):
        if not name.endswith('.cs'):
            continue
        text = open(os.path.join(path, name), encoding='utf-8').read()
        for cam, body in blocks(text):
            ui_only, copyurp, post_off = classify(body)
            rows.append({'file': name, 'cam': cam, 'ui_only': ui_only,
                         'copyurp': copyurp, 'post_off': post_off,
                         'bad': verdict(ui_only, copyurp, post_off)})
    return rows


def self_test():
    fails, cells = [], [0]

    def eq(name, got, want):
        cells[0] += 1
        if got != want:
            fails.append('%s: 얻은 값 %r ≠ 바란 값 %r' % (name, got, want))

    ui = 'cam.cullingMask = 1 << uiLayer;'
    add = 'cam.cullingMask |= 1 << canvas.gameObject.layer;'
    urp = 'ShotCam.CopyUrp(Camera.main, cam);'
    urp_off = 'ShotCam.CopyUrp(Camera.main, cam).renderPostProcessing = false;'

    eq('ⓐ UI 전용 + CopyUrp 그대로 = 빨강', verdict(*classify(ui + urp)), True)
    eq('ⓑ UI 전용 + 후처리 끔 = 초록', verdict(*classify(ui + urp_off)), False)
    eq('ⓒ UI 전용 + 안 부름 = 초록', verdict(*classify(ui)), False)
    eq('ⓓ 세계를 남긴 마스크(|=) + CopyUrp = 초록', verdict(*classify(add + urp)), False)
    eq('ⓔ 마스크를 안 바꿈 + CopyUrp = 초록', verdict(*classify(urp)), False)
    eq('ⓕ UI 전용인데 |= 도 있으면 UI 전용이 아니다', classify(ui + add + urp)[0], False)
    eq('ⓖ 따로 끈 줄도 받아 준다', verdict(*classify(ui + urp + 'd.renderPostProcessing = false;')), False)

    body = 'var c = go.AddComponent<Camera>();\n' + ui + urp
    got = blocks(body)
    eq('ⓗ 덩이 하나', len(got), 1)
    eq('ⓗ 카메라 이름', got[0][0], 'c')
    two = ('a = x.AddComponent<Camera>();\n' + ui + urp + '\n'
           'b = y.AddComponent<Camera>();\n' + add + urp)
    g2 = blocks(two)
    eq('ⓗ 덩이 둘', len(g2), 2)
    eq('ⓗ 앞 덩이만 빨강', [verdict(*classify(b)) for _, b in g2], [True, False])

    if fails:
        print('✗ check_shot_cams --self-test 실패 %d' % len(fails))
        for f in fails:
            print('  · ' + f)
        return 1
    print('✓ check_shot_cams --self-test %d칸 통과' % cells[0])
    return 0


def main(argv):
    if '--self-test' in argv:
        return self_test()
    rows = scan()
    show_all = '--list' in argv
    ui_only = [r for r in rows if r['ui_only']]
    world = [r for r in rows if not r['ui_only']]
    bad = [r for r in rows if r['bad']]

    if show_all:
        for r in rows:
            kind = 'UI 전용' if r['ui_only'] else '세계도 그린다'
            post = ('CopyUrp' + (' + 후처리 끔' if r['post_off'] else '')) if r['copyurp'] else '안 부름'
            print('  %-28s %-6s %-14s %s' % (r['file'], r['cam'], kind, post))

    for r in bad:
        print('✗ %s (%s) — **UI 층만 그리는데** `ShotCam.CopyUrp` 로 후처리를 켠 채 둔다.' % (r['file'], r['cam']))
        print('   얻는 것은 0이고 **UI 가 톤맵·색 보정에 물든다**(런 562: 순백 255 → 215 · 결정 591).')
        print('   고침 둘: `ShotCam.CopyUrp(...).renderPostProcessing = false;` 또는 아예 안 부른다.')
    if bad:
        print('✗ check_shot_cams: 카메라 %d 중 UI 전용 %d · 세계를 그리는 것 %d — **어긋난 자리 %d**'
              % (len(rows), len(ui_only), len(world), len(bad)))
        return 1
    print('✓ check_shot_cams: 카메라 %d · UI 전용 %d(후처리 안 켠다) · 세계를 그리는 것 %d · 어긋난 자리 0'
          % (len(rows), len(ui_only), len(world)))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
